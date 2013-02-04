using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Gqqnbig.TrafficVolumeMonitor.Modules;
using Gqqnbig.Windows.Media;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 将多少条记录聚合为统计图的一列。
        /// </summary>
        private const int accumulateLength = 6;
        Queue<Image<Bgr, byte>> bufferImages;
        ICaptureRetriever captureRetriever;

        ILane lane;
        LaneMonitor laneMonitor;
        private CarMatch[] lastMatch;
        private LocationParameter locationParameter;
        private DispatcherTimer realtimeLoadingTimer;
        private LaneCapture lastLaneCapture;

        readonly System.Collections.ObjectModel.ObservableCollection<KeyValuePair<string, int>> chartData = new System.Collections.ObjectModel.ObservableCollection<KeyValuePair<string, int>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        public int PicId { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            locationsMenuItem.ItemsSource = Directory.GetFiles(".\\", "*.lol");
            lineChart.DataContext = chartData;

            StartLocationAnalysis(".\\xi'ao.lol");
        }

        /// <summary>
        /// 开始分析一个位置（一个位置包括很多张图片，每张图片包括很多辆车）
        /// </summary>
        /// <param name="locationSpecPath">位置设定文件的路径</param>
        private void StartLocationAnalysis(string locationSpecPath)
        {
            realtimeLoadingTimer = null;

            XmlSerializer serializer = new XmlSerializer(typeof(LocationParameter));

            //locationParameter = new LocationParameter();
            //locationParameter.AlgorithmName = "near-single";
            //locationParameter.BufferImagesCount = 1;
            //locationParameter.CarMatchParameter = new CarMatchParameter();
            //locationParameter.CarMatchParameter.AllowSamePosition = true;
            //locationParameter.CarMatchParameter.SimilarityThreshold = 0.26;
            //locationParameter.MaskFilePath = @"..\..\高伟乐街与荷兰园大马路交界\算法\mask1.bmp";
            //locationParameter.SourcePath = @"..\..\高伟乐街与荷兰园大马路交界\测试\测试图\{0}.jpg";

            //XmlTextWriter xmlWriter = new XmlTextWriter("B:\\gaohe.xml", System.Text.Encoding.UTF8);
            //xmlWriter.Formatting = Formatting.Indented;
            //serializer.Serialize(xmlWriter, locationParameter);
            //xmlWriter.Close();

            Contract.Assert(File.Exists(locationSpecPath));

            XmlTextReader xmlReader = new XmlTextReader(locationSpecPath);
            locationParameter = (LocationParameter)serializer.Deserialize(xmlReader);
            xmlReader.Close();

            if (locationParameter.SourcePath.StartsWith("http"))
            {
                //captureRetriever = new RealtimeCaptureRetriever("http://www.dsat.gov.mo/cams/cam1/AxisPic-Cam1.jpg", 5000);
                captureRetriever = new RealtimeCaptureRetriever(locationParameter.SourcePath, 5000);
            }
            else
            {
                captureRetriever = new DiskCaptureRetriever(locationParameter.SourcePath, 0);
            }



            //lane = new Lane(locationParameter.MaskFilePath);
            var ass = Assembly.LoadFrom("Algorithms\\" + locationParameter.AlgorithmName + ".dll");
            Type[] exportedTypes = ass.GetExportedTypes();
            foreach (var t in exportedTypes)
            {
                if (typeof(ILane).IsAssignableFrom(t))
                {
                    lane = (ILane)Activator.CreateInstance(t, locationParameter.MaskFilePath);
                    break;
                }
            }

            if (lane == null)
                throw new FileNotFoundException("找不到Algorithms\\" + locationParameter.AlgorithmName + ".dll");

            laneMonitor = new LaneMonitor(TrafficDirection.GoUp, lane, locationParameter.CarMatchParameter, 60);

            bufferImages = new Queue<Image<Bgr, byte>>(locationParameter.BufferImagesCount);
            for (int i = 0; i < locationParameter.BufferImagesCount; i++)
            {
                bufferImages.Enqueue(captureRetriever.GetCapture());
                System.Diagnostics.Debug.WriteLine("获得图片");
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitialView();

                //captureViewers.Clear();
                //captureViewers.Add(new CaptureViewer { Lane = lane });
                //captureViewers.Add(new CaptureViewer { Lane = lane });
                //captureViewers.Add(new CaptureViewer { Lane = lane });
                //captureViewerList.ItemsSource = captureViewers;


                //captureViewerList_SizeChanged(null, null);
                //Width++;
                //Width--;
            }));
        }

        /// <summary>
        /// InitialView方法极为耗时，不允许在Dispatcher线程上运行。
        /// </summary>
        private void InitialView()
        {
            Contract.Requires(Dispatcher.CheckAccess() == false, "InitialView方法极为耗时，不允许在Dispatcher线程上运行。");

            int index = locationParameter.BufferImagesCount / 2;

            Image<Bgr, byte> orginialImage = bufferImages.ElementAt(index);
            ICollection<Image<Bgr, byte>> samples = bufferImages.ToArray();
            LaneCapture laneCapture1 = lane.Analyze(orginialImage, samples);

            bufferImages.Dequeue();
            bufferImages.Enqueue(captureRetriever.GetCapture());
            Image<Bgr, byte> orginialImage1 = bufferImages.ElementAt(index);
            ICollection<Image<Bgr, byte>> samples1 = bufferImages.ToArray();
            LaneCapture laneCapture2 = lastLaneCapture = lane.Analyze(orginialImage1, samples1);

            var carMatches = laneMonitor.FindCarMatch(laneCapture1.Cars, laneCapture2.Cars);

            var carMove = laneMonitor.GetCarMove(carMatches, laneCapture1.Cars, laneCapture2.Cars);

            laneMonitor.AddHistory(carMove);
            chartData.Clear();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                int n = PicId / accumulateLength;
                string independentValue = string.Format("{0}-{1}", n * accumulateLength, n * accumulateLength + accumulateLength - 1);
                chartData.Add(new KeyValuePair<string, int>(independentValue, carMove.EnterToPic2));

                currentImage.Source = lastLaneCapture.OriginalImage.ToBitmap().ToBitmapImage();
            }));


            //if (captureRetriever.SuggestedInterval != 0 && realtimeLoadingTimer == null)
            //{
                realtimeLoadingTimer = new DispatcherTimer();
                realtimeLoadingTimer.Tick += (a, b) => nextButton_Click(null, new RoutedEventArgs());
                realtimeLoadingTimer.Interval = TimeSpan.FromSeconds(5); //TimeSpan.FromMilliseconds(captureRetriever.SuggestedInterval);
                realtimeLoadingTimer.Start();
            //}
        }

        //private void LoadCompleted()
        //{
        //    //if (captureRetriever.SuggestedInterval != 0 && realtimeLoadingTimer == null)
        //    //{
        //    //    realtimeLoadingTimer = new DispatcherTimer();
        //    //    realtimeLoadingTimer.Tick += (a, b) => nextButton_Click(null, new RoutedEventArgs());
        //    //    realtimeLoadingTimer.Interval = TimeSpan.FromMilliseconds(captureRetriever.SuggestedInterval);
        //    //    realtimeLoadingTimer.Start();
        //    //}


        //    lastMatch = laneMonitor.FindCarMatch(laneCapture1.Cars, laneCapture2.Cars);
        //    //LabelMatch(lastMatch);

        //    var carMove = laneMonitor.GetCarMove(lastMatch, laneCapture1.Cars, laneCapture2.Cars);

        //    //averageRunLengthRun.Text = carMove.AverageMove.ToString("f1");
        //    //leaveFromPic1Run.Text = carMove.LeaveFromPic1.ToString();
        //    //enterToPic2Run.Text = carMove.EnterToPic2.ToString();

        //    laneMonitor.AddHistory(carMove);



        //    chartData.Add(new KeyValuePair<string, int>(PicId.ToString(), carMove.EnterToPic2));
        //    //volume5Run.Text = laneMonitor.VolumeIn5seconds.ToString("f1");
        //    //volume60Run.Text = laneMonitor.VolumeIn60seconds.ToString("f1");

        //    //ThreadPool.QueueUserWorkItem(o => PreloadImage());
        //}

        /// <summary>
        /// PreloadImage方法极为耗时，不允许在Dispatcher线程上运行。
        /// </summary>
        void PreloadImage()
        {
            Contract.Requires(Dispatcher.CheckAccess() == false, "PreloadImage方法极为耗时，不允许在Dispatcher线程上运行。");


        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(lineSeries.LegendItems.Count);


            Image<Bgr, byte> orginialImage;
            ICollection<Image<Bgr, byte>> samples;
            lock (bufferImages)
            {
                bufferImages.Dequeue();
                bufferImages.Enqueue(captureRetriever.GetCapture());
                orginialImage = bufferImages.ElementAt(locationParameter.BufferImagesCount / 2);
                samples = bufferImages.ToArray();
            }

            var laneCapture = lane.Analyze(orginialImage, samples);
            System.Diagnostics.Debug.WriteLine("分析完成");

            var carMatches = laneMonitor.FindCarMatch(lastLaneCapture.Cars, laneCapture.Cars);

            var carMove = laneMonitor.GetCarMove(carMatches, lastLaneCapture.Cars, laneCapture.Cars);

            lastLaneCapture = laneCapture;
            laneMonitor.AddHistory(carMove);
            PicId++;

            int n = PicId / accumulateLength;
            string independentValue = string.Format("{0}-{1}", n * accumulateLength, n * accumulateLength + accumulateLength - 1);

            int index = chartData.Count - 1;
            var pair = chartData[index];
            if (pair.Key == independentValue)
            {
                pair = new KeyValuePair<string, int>(pair.Key, pair.Value + carMove.EnterToPic2);
                chartData[index] = pair;
            }
            else
            {
                chartData.Add(new KeyValuePair<string, int>(independentValue, carMove.EnterToPic2));
            }

            currentImage.Source = lastLaneCapture.OriginalImage.ToBitmap().ToBitmapImage();
            imageIdTextBlock.Text = PicId.ToString();

            //var originalCursor = Mouse.OverrideCursor;
            //Mouse.OverrideCursor = Cursors.Wait;

            //System.Diagnostics.Debug.WriteLine("nextButton_Click");
            ////if (captureViewers[2].CurrentPicId != PicId + 1)
            ////    captureViewers[2].View(PicId + 1);
            ////UpdateLayout();


            ////picIdTextRun1.Text = PicId.ToString();
            ////picIdTextRun2.Text = (PicId + 1).ToString();


            //TranslateTransform translateTransform = new TranslateTransform();
            ////captureViewers[0].RenderTransform = translateTransform;
            ////captureViewers[1].RenderTransform = translateTransform;
            ////captureViewers[2].RenderTransform = translateTransform;

            //DoubleAnimation animation = new DoubleAnimation(-captureViewerList.RenderSize.Height / 2, new Duration(TimeSpan.FromMilliseconds(1000)));
            //animation.FillBehavior = FillBehavior.Stop;
            //animation.Completed += (a, b) =>
            //{
            //    //captureViewers.Move(0, captureViewers.Count - 1);
            //    //if (captureRetriever.SuggestedInterval != 0 && realtimeLoadingTimer == null)
            //    //{
            //    //    realtimeLoadingTimer = new DispatcherTimer();
            //    //    realtimeLoadingTimer.Tick += (a, b) => nextButton_Click(null, new RoutedEventArgs());
            //    //    realtimeLoadingTimer.Interval = TimeSpan.FromMilliseconds(captureRetriever.SuggestedInterval);
            //    //    realtimeLoadingTimer.Start();
            //    //}


            //    lastMatch = laneMonitor.FindCarMatch(laneCapture1.Cars, laneCapture2.Cars);
            //    //LabelMatch(lastMatch);

            //    var carMove = laneMonitor.GetCarMove(lastMatch, laneCapture1.Cars, laneCapture2.Cars);

            //    //averageRunLengthRun.Text = carMove.AverageMove.ToString("f1");
            //    //leaveFromPic1Run.Text = carMove.LeaveFromPic1.ToString();
            //    //enterToPic2Run.Text = carMove.EnterToPic2.ToString();

            //    laneMonitor.AddHistory(carMove);



            //    chartData.Add(new KeyValuePair<string, int>(PicId.ToString(), carMove.EnterToPic2));
            //    //volume5Run.Text = laneMonitor.VolumeIn5seconds.ToString("f1");
            //    //volume60Run.Text = laneMonitor.VolumeIn60seconds.ToString("f1");

            //    //ThreadPool.QueueUserWorkItem(o => PreloadImage());
            //};
            //translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);

            ////Mouse.OverrideCursor = originalCursor;
        }

        private void LocationRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                RadioButton rb = item.Icon as RadioButton;
                if (rb != null)
                {
                    rb.IsChecked = true;
                }
            }

            //MenuItem item = (MenuItem)e.OriginalSource;

            ////locationsMenuItem.Items

            Task.Factory.StartNew(o => StartLocationAnalysis((string)o), item.Header);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Gqqnbig.TrafficVolumeMonitor.Modules;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Queue<Image<Bgr, byte>> bufferImages;
        ICaptureRetriever captureRetriever;

        readonly System.Collections.ObjectModel.ObservableCollection<CaptureViewer> captureViewers = new System.Collections.ObjectModel.ObservableCollection<CaptureViewer>();

        ILane lane;
        LaneMonitor laneMonitor;
        private CarMatch[] lastMatch;
        private LocationParameter locationParameter;
        private DispatcherTimer realtimeLoadingTimer;

        public MainWindow()
        {
            Title = GetType().Assembly.Location;



            InitializeComponent();
        }

        public int PicId { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            locationsMenuItem.ItemsSource = Directory.GetFiles(".\\", "*.lol");

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

            laneMonitor = new LaneMonitor(TrafficDirection.GoUp, lane, locationParameter.CarMatchParameter,60);

            bufferImages = new Queue<Image<Bgr, byte>>(locationParameter.BufferImagesCount);
            for (int i = 0; i < locationParameter.BufferImagesCount; i++)
            {
                bufferImages.Enqueue(captureRetriever.GetCapture());
                System.Diagnostics.Debug.WriteLine("获得图片");
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitialView();

                captureViewers.Clear();
                captureViewers.Add(new CaptureViewer { Lane = lane });
                captureViewers.Add(new CaptureViewer { Lane = lane });
                captureViewers.Add(new CaptureViewer { Lane = lane });
                captureViewerList.ItemsSource = captureViewers;


                captureViewerList_SizeChanged(null, null);
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
            var laneCapture1 = lane.Analyze(orginialImage, samples);

            bufferImages.Dequeue();
            bufferImages.Enqueue(captureRetriever.GetCapture());
            Image<Bgr, byte> orginialImage1 = bufferImages.ElementAt(index);
            ICollection<Image<Bgr, byte>> samples1 = bufferImages.ToArray();
            var laneCapture2 = lane.Analyze(orginialImage1, samples1);

            Dispatcher.BeginInvoke(new Action(() =>
                {
                    captureViewers[0].View(laneCapture1);
                    captureViewers[1].View(laneCapture2);

                    picIdTextRun1.Text = PicId.ToString();
                    picIdTextRun2.Text = (PicId + 1).ToString();

                    LoadCompleted();
                }));
        }

        private void LoadCompleted()
        {
            if (captureRetriever.SuggestedInterval != 0 && realtimeLoadingTimer == null)
            {
                realtimeLoadingTimer = new DispatcherTimer();
                realtimeLoadingTimer.Tick += (a, b) => nextButton_Click(null, new RoutedEventArgs());
                realtimeLoadingTimer.Interval = TimeSpan.FromMilliseconds(captureRetriever.SuggestedInterval);
                realtimeLoadingTimer.Start();
            }


            lastMatch = laneMonitor.FindCarMatch(captureViewers[0].Cars, captureViewers[1].Cars);
            LabelMatch(lastMatch);

            var carMove = laneMonitor.GetCarMove(lastMatch, captureViewers[0].Cars, captureViewers[1].Cars);

            averageRunLengthRun.Text = carMove.AverageMove.ToString("f1");
            leaveFromPic1Run.Text = carMove.LeaveFromPic1.ToString();
            enterToPic2Run.Text = carMove.EnterToPic2.ToString();

            laneMonitor.AddHistory(carMove);
            volume5Run.Text = laneMonitor.VolumeIn5seconds.ToString("f1");
            volume60Run.Text = laneMonitor.VolumeIn60seconds.ToString("f1");

            ThreadPool.QueueUserWorkItem(o => PreloadImage());
        }

        private void LabelMatch(CarMatch[] matches)
        {
            double h = 360.0 / (matches.Length + 1);
            int n = 1;

            foreach (var m in matches)
            {
                //System.Diagnostics.Debug.WriteLine(h * n);
                captureViewers[0].BoxCar(m.Car1, "match", /*Brushes.Red*/ new SolidColorBrush(Drawing.ColorConversion.HslToRgb(h * n, 0.781, 0.625).ToWpfColor()));
                captureViewers[1].BoxCar(m.Car2, "match", /*Brushes.Red*/ new SolidColorBrush(Drawing.ColorConversion.HslToRgb(h * n++, 0.781, 0.625).ToWpfColor()));
            }
        }

        private void UnlabelMatch(CarMatch[] matches)
        {
            foreach (var m in matches)
            {
                captureViewers[0].UnboxCar("match", m.Car1);
                captureViewers[1].UnboxCar("match", m.Car2);
            }
        }


        private void calculateSimilarityButton_Click(object sender, RoutedEventArgs e)
        {
            Car car1 = captureViewers[0].listView.SelectedItem as Car;

            if (car1 == null)
            {
                MessageBox.Show("请在上方的列表框中勾选一项");
                return;
            }

            Car car2 = captureViewers[1].listView.SelectedItem as Car;

            if (car2 == null)
            {
                MessageBox.Show("请在下方的列表框中勾选一项");
                return;
            }

            histSimilarityTextBox.Text = string.Format("值越大越相似\r\nR={0}\r\nG={1}\r\nB={2}\r\nHue={3}",
                            CvInvoke.cvCompareHist(car1.HistR, car2.HistR, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL),
                            CvInvoke.cvCompareHist(car1.HistG, car2.HistG, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL),
                            CvInvoke.cvCompareHist(car1.HistB, car2.HistB, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL),
                            CvInvoke.cvCompareHist(car1.HistHue, car2.HistHue, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL));
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastMatch != null)
                UnlabelMatch(lastMatch);

            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;

            System.Diagnostics.Debug.WriteLine("nextButton_Click");
            PicId++;
            //if (captureViewers[2].CurrentPicId != PicId + 1)
            //    captureViewers[2].View(PicId + 1);
            UpdateLayout();


            picIdTextRun1.Text = PicId.ToString();
            picIdTextRun2.Text = (PicId + 1).ToString();


            TranslateTransform translateTransform = new TranslateTransform();
            captureViewers[0].RenderTransform = translateTransform;
            captureViewers[1].RenderTransform = translateTransform;
            captureViewers[2].RenderTransform = translateTransform;

            DoubleAnimation animation = new DoubleAnimation(-captureViewerList.RenderSize.Height / 2, new Duration(TimeSpan.FromMilliseconds(1000)));
            animation.FillBehavior = FillBehavior.Stop;
            animation.Completed += (a, b) =>
                                       {
                                           captureViewers.Move(0, captureViewers.Count - 1);
                                           LoadCompleted();
                                       };
            translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);

            Mouse.OverrideCursor = originalCursor;
        }

        /// <summary>
        /// PreloadImage方法极为耗时，不允许在Dispatcher线程上运行。
        /// </summary>
        void PreloadImage()
        {
            Contract.Requires(Dispatcher.CheckAccess() == false, "PreloadImage方法极为耗时，不允许在Dispatcher线程上运行。");

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
            Dispatcher.BeginInvoke(new Action(() =>
                                                  {
                                                      captureViewers[2].View(laneCapture);
                                                      System.Diagnostics.Debug.WriteLine("预加载" + (PicId + 2) + "完成");
                                                  }), DispatcherPriority.Background);
        }

        private void captureViewerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var viewer in captureViewers)
            {
                viewer.Height = captureViewerList.RenderSize.Height / 2;
            }
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

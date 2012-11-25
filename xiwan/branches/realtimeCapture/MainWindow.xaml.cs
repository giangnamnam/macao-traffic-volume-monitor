﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Emgu.CV;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int m_picId;
        readonly System.Collections.ObjectModel.ObservableCollection<CaptureViewer> captureViewers = new System.Collections.ObjectModel.ObservableCollection<CaptureViewer>();

        readonly Lane lane;
        readonly LaneMonitor laneMonitor;
        private CarMatch[] lastMatch;

        bool viewOn1 = false;
        bool viewOn2 = false;


        public MainWindow()
        {
            Title = GetType().Assembly.Location;
            //Environment.CurrentDirectory = Path.GetDirectoryName(Title);

            var diskCaptureRetriever = new DiskCaptureRetriever(@"..\..\西湾测试\测试\测试图片\{0}.jpg");
            var realtimeRetriever = new RealtimeCaptureRetriever(5000, "http://www.dsat.gov.mo/cams/cam27/AxisPic-Cam27.jpg");
            realtimeRetriever.Downloaded += realtimeRetriever_Downloaded;

            lane = new Lane(realtimeRetriever, @"..\..\西湾算法\mask-Lane1.gif");
            laneMonitor = new LaneMonitor(TrafficDirection.GoUp, lane);

            InitializeComponent();



            captureViewers.Add(new CaptureViewer { Lane = lane });
            captureViewers.Add(new CaptureViewer { Lane = lane });
            captureViewers.Add(new CaptureViewer { Lane = lane });
            captureViewerList.ItemsSource = captureViewers;

            realtimeRetriever.Start();

            //realtimeRetriever.EnsureNext(6);
            //realtimeRetriever.GetCapture();
            //realtimeRetriever.GetCapture();
            //realtimeRetriever.GetCapture();
            //realtimeRetriever.EnsureNext(1);

        }

        void realtimeRetriever_Downloaded(object sender, CaptureRetrieverEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("InCacheCount={0}, view1={1}, view2={2}", e.InCacheCount, viewOn1, viewOn2);

            if (e.InCacheCount == 6)
            {
                if (viewOn1 == false)
                {
                    RealtimeCaptureRetriever retriever = (RealtimeCaptureRetriever)sender;
                    retriever.GetCapture();
                    retriever.GetCapture();
                    //retriever.GetCapture();

                    Dispatcher.Invoke(new Action(() => captureViewers[0].View()));

                    viewOn1 = true;
                }
                else if (viewOn2 == false)
                {
                    //RealtimeCaptureRetriever retriever = (RealtimeCaptureRetriever)sender;
                    //retriever.GetCapture();
                    //retriever.GetCapture();
                    ////retriever.GetCapture();

                    Dispatcher.Invoke(new Action(() =>
                                                     {
                                                         captureViewers[1].View();
                                                         LoadCompleted();
                                                     }));

                    viewOn2 = true;


                }
                else if (viewOn1 && viewOn2)
                {
                    Dispatcher.Invoke(new Action(() =>
                                                     {
                                                         captureViewers[2].View();
                                                         nextButton_Click(null, new RoutedEventArgs());
                                                     }));
                }
            }
        }

        public int PicId
        {
            get { return m_picId; }
            set
            {
                m_picId = value;
                previousButton.IsEnabled = m_picId > 0;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //try
            //{

            //PicId = 5;
            //captureViewers[0].View();
            //captureViewers[1].View();

            //picIdTextRun1.Text = PicId.ToString();
            //picIdTextRun2.Text = (PicId + 1).ToString();

            //LoadCompleted();

            //DispatcherTimer timer = new DispatcherTimer();
            //timer.Interval = TimeSpan.FromSeconds(5);
            //timer.Tick += (o, ex) => nextButton_Click(null, new RoutedEventArgs());
            //timer.Start();
            //}
            //catch (Exception ex)
            //{

            //}
        }

        private void LoadCompleted()
        {
            lastMatch = laneMonitor.FindCarMatch(captureViewers[0].Cars, captureViewers[1].Cars);
            LabelMatch(lastMatch);

            var carMove = laneMonitor.GetCarMove(lastMatch, captureViewers[0].Cars, captureViewers[1].Cars);

            averageRunLengthRun.Text = carMove.AverageMove.ToString("f1");
            leaveFromPic1Run.Text = carMove.LeaveFromPic1.ToString();
            enterToPic2Run.Text = carMove.EnterToPic2.ToString();

            laneMonitor.AddHistory(carMove);
            volume5Run.Text = laneMonitor.VolumeIn5seconds.ToString("f1");
            volume60Run.Text = laneMonitor.VolumeIn60seconds.ToString("f1");

            //PreloadImage();
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
            UnlabelMatch(lastMatch);

            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;


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
            animation.Completed += new EventHandler(animation_Completed);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);



            Mouse.OverrideCursor = originalCursor;
        }

        void animation_Completed(object sender, EventArgs e)
        {
            var n = captureViewers[0];
            captureViewers.RemoveAt(0);
            captureViewers.Add(n);

            LoadCompleted();
        }

        void PreloadImage()
        {
            Dispatcher.BeginInvoke(new Action(() =>
                                                  {
                                                      captureViewers[2].View();
                                                      System.Diagnostics.Debug.WriteLine("预加载" + (PicId + 2) + "完成");
                                                  }), System.Windows.Threading.DispatcherPriority.Background);
        }


        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            PicId--;
            //InspectCapture();
        }

        private void captureViewerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var viewer in captureViewers)
            {
                viewer.Height = captureViewerList.RenderSize.Height / 2;
            }
        }
    }
}
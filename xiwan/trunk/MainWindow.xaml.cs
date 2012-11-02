using System;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;

namespace Gqqnbig.TrafficVolumeCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string filePathPattern = @"H:\文件\毕业设计\西湾大桥氹仔端\图片\{0}.jpg";
        private int m_picId;
        readonly System.Collections.ObjectModel.ObservableCollection<CaptureViewer> captureViewers = new System.Collections.ObjectModel.ObservableCollection<CaptureViewer>();

        public MainWindow()
        {
            InitializeComponent();

            PicId = 0;

            captureViewers.Add(new CaptureViewer { FilePathPattern = filePathPattern });
            captureViewers.Add(new CaptureViewer { FilePathPattern = filePathPattern });
            captureViewers.Add(new CaptureViewer { FilePathPattern = filePathPattern });
            captureViewerList.ItemsSource = captureViewers;
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
            double h = captureViewerList.RenderSize.Height / 2;


            try
            {
                captureViewers[0].View(PicId);
                captureViewers[1].View(PicId + 1);

                picIdTextRun1.Text = PicId.ToString();
                picIdTextRun2.Text = (PicId + 1).ToString();

                PreloadImage();
            }
            catch (Exception ex)
            {

            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
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

            histSimilarityTextBox.Text = "值越大越相似\r\n";
            histSimilarityTextBox.Text += CvInvoke.cvCompareHist(car1.HistR, car2.HistR, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL).ToString() + "\r\n";
            histSimilarityTextBox.Text += CvInvoke.cvCompareHist(car1.HistG, car2.HistG, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL).ToString() + "\r\n";
            histSimilarityTextBox.Text += CvInvoke.cvCompareHist(car1.HistB, car2.HistB, Emgu.CV.CvEnum.HISTOGRAM_COMP_METHOD.CV_COMP_CORREL).ToString() + "\r\n";
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;

            PicId++;
            if (captureViewers[2].CurrentPicId != PicId + 1)
                captureViewers[2].View(PicId + 1);
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

            PreloadImage();
        }

        void PreloadImage()
        {
            Dispatcher.BeginInvoke(new Action(() =>
                                                  {
                                                      captureViewers[2].View(PicId + 2);
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

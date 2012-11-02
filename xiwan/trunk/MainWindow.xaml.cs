using System;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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

        public MainWindow()
        {
            InitializeComponent();

            PicId = 0;
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
            try
            {
                InspectCapture();
            }
            catch (Exception ex)
            {

            }
        }

        private void InspectCapture()
        {
            var originalCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;

            Image<Bgr, byte> frame1 = new Image<Bgr, byte>(string.Format(filePathPattern, PicId));
            Lane lane = new Lane();
            Image<Gray, byte> finalImage;

            var roadColor = lane.GetRoadColor(frame1);
            int sampleStart = PicId - 3;
            var samples = GetSamples(sampleStart < 0 ? 0 : sampleStart, 6);
            var backgroundImage = lane.FindBackground(samples, roadColor);
            var cars1 = lane.FindCars(frame1, backgroundImage, out finalImage);
            captureViewer1.imageBox.Source = lane.GetFocusArea(frame1).ToBitmap().ToBitmapImage();
            captureViewer1.listView.ItemsSource = cars1;
            captureViewer1.totalCarNumberTextRun.Text = cars1.Length.ToString();


            Image<Bgr, byte> frame2 = new Image<Bgr, byte>(string.Format(filePathPattern, PicId + 1));
            sampleStart = PicId - 2;
            samples = GetSamples(sampleStart < 0 ? 0 : sampleStart, 6);
            roadColor = lane.GetRoadColor(frame2);
            backgroundImage = lane.FindBackground(samples, roadColor);
            var cars2 = lane.FindCars(frame2, backgroundImage, out finalImage);
            captureViewer2.imageBox.Source = lane.GetFocusArea(frame2).ToBitmap().ToBitmapImage(); // finalImage.ToBitmap().ToBitmapImage();
            captureViewer2.listView.ItemsSource = cars2;
            captureViewer2.totalCarNumberTextRun.Text = cars2.Length.ToString();

            picIdTextRun1.Text = PicId.ToString();
            picIdTextRun2.Text = (PicId + 1).ToString();

            Mouse.OverrideCursor = originalCursor;
        }

        private Image<Bgr, byte>[] GetSamples(int sampleStart, int length)
        {
            Contract.Requires(sampleStart >= 0);
            Contract.Requires(length >= 0);

            Image<Bgr, byte>[] samples = new Image<Bgr, byte>[length];

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = new Image<Bgr, byte>(string.Format(filePathPattern, sampleStart++));
            }
            return samples;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Car car1 = captureViewer1.listView.SelectedItem as Car;

            if (car1 == null)
            {
                MessageBox.Show("请在上方的列表框中勾选一项");
                return;
            }

            Car car2 = captureViewer2.listView.SelectedItem as Car;

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
            PicId++;
            InspectCapture();
        }


        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            PicId--;
            InspectCapture();
        }
    }
}

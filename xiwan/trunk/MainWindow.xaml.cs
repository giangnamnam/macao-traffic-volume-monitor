using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Image<Bgr, byte> frame1 = new Image<Bgr, byte>(@"H:\文件\毕业设计\西湾大桥氹仔端\图片\0.jpg");
                Lane lane = new Lane();
                Image<Gray, byte> finalImage;
                var cars1 = lane.FindCars(frame1, out finalImage);
                captureViewer1.imageBox.Source = lane.GetFocusArea(frame1).ToBitmap().ToBitmapImage();
                captureViewer1.listView.ItemsSource = cars1;
                captureViewer1.totalCarNumberTextRun.Text = cars1.Length.ToString();


                Image<Bgr, byte> frame2 = new Image<Bgr, byte>(@"H:\文件\毕业设计\西湾大桥氹仔端\图片\1.jpg");
                var cars2 = lane.FindCars(frame2, out finalImage);
                captureViewer2.imageBox.Source = lane.GetFocusArea(frame2).ToBitmap().ToBitmapImage();// finalImage.ToBitmap().ToBitmapImage();
                captureViewer2.listView.ItemsSource = cars2;
                captureViewer2.totalCarNumberTextRun.Text = cars2.Length.ToString();
            }
            catch (Exception ex)
            {

            }
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



    }
}

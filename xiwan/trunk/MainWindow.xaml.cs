using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string filePathPattern = @"H:\文件\毕业设计\西湾大桥氹仔端\图片\{0}.jpg";

        readonly ListView listView1, listView2;


        public MainWindow()
        {
            InitializeComponent();

            listView1 = new ListView();
            Grid.SetColumn(listView1, 1);
            Grid.SetRow(listView1, 0);
            ScrollViewer.SetHorizontalScrollBarVisibility(listView1, ScrollBarVisibility.Auto);
            listView1.SelectionChanged += contourListBox_SelectionChanged;
            GridView gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("checkBoxTemplate") });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("picTemplate"), Header = "图片" });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("histRTemplate"), Header = "R" });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("histGTemplate"), Header = "G" });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("histBTemplate"), Header = "B" });
            listView1.View = gridView;
            grid.Children.Add(listView1);

            listView2 = new ListView();
            Grid.SetColumn(listView2, 1);
            Grid.SetRow(listView2, 1);
            ScrollViewer.SetHorizontalScrollBarVisibility(listView2, ScrollBarVisibility.Auto);
            listView2.SelectionChanged += contourListBox_SelectionChanged;
            gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("checkBoxTemplate") });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("picTemplate"), Header = "图片" });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("histRTemplate"), Header = "R" });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("histGTemplate"), Header = "G" });
            gridView.Columns.Add(new GridViewColumn { CellTemplate = (DataTemplate)FindResource("histBTemplate"), Header = "B" });
            listView2.View = gridView;
            grid.Children.Add(listView2);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Image<Bgr, byte> frame1 = new Image<Bgr, byte>(@"H:\文件\毕业设计\西湾大桥氹仔端\图片\0.jpg");
                Lane lane = new Lane();
                Image<Gray, byte> finalImage;
                var cars1 = lane.FindCars(frame1, out finalImage);
                image1.Source = lane.GetFocusArea(frame1).ToBitmap().ToBitmapImage();
                listView1.ItemsSource = cars1;
                totalCarNumberTextRun.Text = cars1.Length.ToString();


                Image<Bgr, byte> frame2 = new Image<Bgr, byte>(@"H:\文件\毕业设计\西湾大桥氹仔端\图片\1.jpg");
                var cars2 = lane.FindCars(frame2, out finalImage);
                image2.Source = lane.GetFocusArea(frame2).ToBitmap().ToBitmapImage();// finalImage.ToBitmap().ToBitmapImage();
                listView2.ItemsSource = cars2;
                totalCarNumberTextRun2.Text = cars2.Length.ToString();
            }
            catch (Exception ex)
            {

            }
        }



        private void contourListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(image1);
            Car carGroup;
            if (e.RemovedItems.Count > 0)
            {
                carGroup = (Car)e.RemovedItems[0];
                var adorners = adornerLayer.GetAdorners(image1);
                if (adorners != null)
                {
                    for (int i = 0; i < adorners.Length; i++)
                    {
                        OutBoxAdorner oba = adorners[i] as OutBoxAdorner;
                        if (oba != null && oba.Rectangle.Equals(carGroup.CarRectangle) && oba.Pen.Brush == Brushes.Orange)
                        {
                            adornerLayer.Remove(adorners[i]);
                            break;
                        }

                    }
                }
            }

            carGroup = (Car)e.AddedItems[0];
            OutBoxAdorner outBoxAdorner = new OutBoxAdorner(image1);
            outBoxAdorner.Pen = new Pen(Brushes.Orange, 2);
            outBoxAdorner.Rectangle = carGroup.CarRectangle;
            adornerLayer.Add(outBoxAdorner);

        }

        //private void contourListBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var adornerLayer = AdornerLayer.GetAdornerLayer(image2);
        //    Car carGroup;
        //    if (e.RemovedItems.Count > 0)
        //    {
        //        carGroup = (Car)e.RemovedItems[0];
        //        var adorners = adornerLayer.GetAdorners(image2);
        //        if (adorners != null)
        //        {
        //            for (int i = 0; i < adorners.Length; i++)
        //            {
        //                OutBoxAdorner oba = adorners[i] as OutBoxAdorner;
        //                if (oba != null && oba.Rectangle.Equals(carGroup.CarRectangle) && oba.Pen.Brush == Brushes.Orange)
        //                {
        //                    adornerLayer.Remove(adorners[i]);
        //                    break;
        //                }

        //            }
        //        }
        //    }

        //    carGroup = (Car)e.AddedItems[0];
        //    OutBoxAdorner outBoxAdorner = new OutBoxAdorner(image2);
        //    outBoxAdorner.Pen = new Pen(Brushes.Orange, 2);
        //    outBoxAdorner.Rectangle = carGroup.CarRectangle;
        //    adornerLayer.Add(outBoxAdorner);

        //}

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Car car = (Car)checkBox.DataContext;
            var adornerLayer = AdornerLayer.GetAdornerLayer(image1);

            //加上Adorner
            OutBoxAdorner outBoxAdorner = new OutBoxAdorner(image1);
            outBoxAdorner.Pen = new Pen(Brushes.LightGreen, 2);
            outBoxAdorner.Rectangle = car.CarRectangle;
            adornerLayer.Add(outBoxAdorner);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Car carGroup = (Car)checkBox.DataContext;
            var adornerLayer = AdornerLayer.GetAdornerLayer(image1);
            var adorners = adornerLayer.GetAdorners(image1);
            if (adorners != null)
            {
                for (int i = 0; i < adorners.Length; i++)
                {
                    OutBoxAdorner oba = adorners[i] as OutBoxAdorner;
                    if (oba != null && oba.Rectangle.Equals(carGroup.CarRectangle) && oba.Pen.Brush == Brushes.LightGreen)
                    {
                        adornerLayer.Remove(adorners[i]);
                        //break;
                    }

                }
            }
        }

        //private void CheckBox2_Checked(object sender, RoutedEventArgs e)
        //{
        //    CheckBox checkBox = (CheckBox)sender;
        //    Car car = (Car)checkBox.DataContext;
        //    var adornerLayer = AdornerLayer.GetAdornerLayer(image2);

        //    //加上Adorner
        //    OutBoxAdorner outBoxAdorner = new OutBoxAdorner(image2);
        //    outBoxAdorner.Pen = new Pen(Brushes.LightGreen, 1);
        //    outBoxAdorner.Rectangle = car.CarRectangle;
        //    adornerLayer.Add(outBoxAdorner);
        //}

        //private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    CheckBox checkBox = (CheckBox)sender;
        //    Car carGroup = (Car)checkBox.DataContext;
        //    var adornerLayer = AdornerLayer.GetAdornerLayer(image2);
        //    var adorners = adornerLayer.GetAdorners(image2);
        //    if (adorners != null)
        //    {
        //        for (int i = 0; i < adorners.Length; i++)
        //        {
        //            OutBoxAdorner oba = adorners[i] as OutBoxAdorner;
        //            if (oba != null && oba.Rectangle.Equals(carGroup.CarRectangle) && oba.Pen.Brush == Brushes.LightGreen)
        //            {
        //                adornerLayer.Remove(adorners[i]);
        //                //break;
        //            }

        //        }
        //    }
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Car car1 = listView1.SelectedItem as Car;

            if (car1 == null)
            {
                MessageBox.Show("请在上方的列表框中勾选一项");
                return;
            }

            Car car2 = listView2.SelectedItem as Car;

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

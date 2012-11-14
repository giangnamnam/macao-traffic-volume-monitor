using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    /// <summary>
    /// 用于可视化LaneCapture的控件。
    /// </summary>
    public partial class CaptureViewer : UserControl
    {



        public Car[] Cars { get; private set; }
        //public int? CurrentPicId { get; private set; }

        internal Lane Lane { get; set; }

        public CaptureViewer()
        {
            InitializeComponent();
        }

        internal ICaptureRetriever CaptureRetriever { get; set; }

        internal bool IsViewing { get; private set; }

        public void View(LaneCapture laneCapture)
        {
            //if (id.HasValue == false)
            //{
            //    imageBox.Source = null;
            //    listView.ItemsSource = null;
            //    totalCarNumberTextRun.Text = string.Empty;
            //    return;
            //}

            Cars = laneCapture.Cars;

            imageBox.Source = laneCapture.FocusedImage.ToBitmap().ToBitmapImage();
            listView.ItemsSource = Cars;
            totalCarNumberTextRun.Text = Cars.Length.ToString();

        }


        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Car car;
            if (e.RemovedItems.Count > 0)
            {
                car = (Car)e.RemovedItems[0];
                UnboxCar("select", car);
            }

            if (e.AddedItems.Count != 1)
                return;
            car = (Car)e.AddedItems[0];
            BoxCar(car, "select", Brushes.Orange);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Car car = (Car)checkBox.DataContext;
            BoxCar(car, "check", Brushes.LightGreen);
        }

        /// <summary>
        /// 给指定的车加上外框。返回此外框的id。
        /// </summary>
        /// <param name="car"></param>
        /// <param name="tag"> </param>
        /// <param name="brush"></param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        internal void BoxCar(Car car, object tag, Brush brush, double thickness = 2)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(imageBox);

            //加上Adorner
            OutBoxAdorner outBoxAdorner = new OutBoxAdorner(imageBox);
            outBoxAdorner.Tag = tag;
            outBoxAdorner.Pen = new Pen(brush, thickness);
            outBoxAdorner.Rectangle = car.CarRectangle;
            adornerLayer.Add(outBoxAdorner);
        }

        /// <summary>
        /// 删除指定车的外框。若为null，则删除所有的外框。
        /// </summary>
        /// <param name="tag"> </param>
        /// <param name="car"></param>
        internal void UnboxCar(object tag, Car car = null)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(imageBox);
            var adorners = adornerLayer.GetAdorners(imageBox);
            if (adorners != null)
            {
                for (int i = 0; i < adorners.Length; i++)
                {
                    OutBoxAdorner oba = adorners[i] as OutBoxAdorner;
                    if (car == null)
                        adornerLayer.Remove(adorners[i]);
                    else if (oba != null && oba.Rectangle.Equals(car.CarRectangle) && oba.Tag == tag)
                    {
                        adornerLayer.Remove(adorners[i]);
                        break;
                    }
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Car carGroup = (Car)checkBox.DataContext;
            UnboxCar("check", carGroup);
        }

        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage context = (BitmapImage)((MenuItem)sender).DataContext;

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".bmp";
            if (dialog.ShowDialog().GetValueOrDefault(false))
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                Stream stream = dialog.OpenFile();
                encoder.Frames.Add(BitmapFrame.Create(context));
                encoder.Save(stream);
                stream.Close();
            }
        }
    }
}

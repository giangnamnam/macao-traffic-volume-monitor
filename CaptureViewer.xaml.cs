using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gqqnbig.Windows.Media;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    /// <summary>
    /// 用于可视化LaneCapture的控件。
    /// </summary>
    public partial class CaptureViewer : UserControl
    {
        private object[] boxParameters;


        public Car[] Cars { get; private set; }
        //public int? CurrentPicId { get; private set; }

        internal ILane Lane { get; set; }

        public CaptureViewer()
        {
            InitializeComponent();
        }

        internal ICaptureRetriever CaptureRetriever { get; set; }

        internal bool IsViewing { get; private set; }

        public void View(LaneCapture laneCapture)
        {

            Cars = laneCapture.Cars;

            //objectImageBox.Source = laneCapture.ObjectImage.ToBitmap().ToBitmapImage();
            //imageBox.Source = laneCapture.FocusedImage.ToBitmap().ToBitmapImage();
            listView.ItemsSource = Cars;
            progressImagesControl.Items.Clear();
            progressImagesControl.Items.Add(laneCapture.FocusedImage.ToBitmap().ToBitmapImage());
            foreach (var image in laneCapture.ProgrssImages)
            {
                progressImagesControl.Items.Add(image.ToBitmapImage());
            }
            
            
            ;

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

            if (progressImagesControl.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                System.Diagnostics.Debug.WriteLine("progressImagesControl.ItemContainerGenerator.Status=" + progressImagesControl.ItemContainerGenerator.Status 
                    + "，加框操作被延后。");
                boxParameters = new[] { car, tag, brush, thickness };
                progressImagesControl.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
                return;
            }

            progressImagesControl.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;

            UIElement targetElement = (UIElement)progressImagesControl.ItemContainerGenerator.ContainerFromIndex(0);
            var adornerLayer = AdornerLayer.GetAdornerLayer(targetElement);

            //加上Adorner
            OutBoxAdorner outBoxAdorner = new OutBoxAdorner(targetElement);
            outBoxAdorner.Tag = tag;
            outBoxAdorner.Pen = new Pen(brush, thickness);
            outBoxAdorner.Rectangle = car.CarRectangle;
            adornerLayer.Add(outBoxAdorner);
        }

        void ItemContainerGenerator_StatusChanged(object sender, System.EventArgs e)
        {
            // ReSharper disable PossibleInvalidCastException
            BoxCar((Car)boxParameters[0], boxParameters[1], (Brush)boxParameters[2], (double)boxParameters[3]);
            // ReSharper restore PossibleInvalidCastException
        }

        /// <summary>
        /// 删除指定车的外框。若为null，则删除所有的外框。
        /// </summary>
        /// <param name="tag"> </param>
        /// <param name="car"></param>
        internal void UnboxCar(object tag, Car car = null)
        {
            UIElement targetElement = (UIElement)progressImagesControl.ItemContainerGenerator.ContainerFromIndex(0);

            var adornerLayer = AdornerLayer.GetAdornerLayer(targetElement);
            var adorners = adornerLayer.GetAdorners(targetElement);
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
                        //break;
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
            ContextMenu cm = (ContextMenu)((MenuItem)sender).Parent;

            BitmapImage context = (BitmapImage)((Image)cm.PlacementTarget).Source;

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

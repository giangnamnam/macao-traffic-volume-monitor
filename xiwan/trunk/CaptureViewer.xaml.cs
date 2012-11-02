using System.Diagnostics.Contracts;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeCalculator
{
    /// <summary>
    /// CaptureViewer.xaml 的交互逻辑
    /// </summary>
    public partial class CaptureViewer : UserControl
    {
        public int? CurrentPicId { get; private set; }
        public string FilePathPattern { get; set; }

        public CaptureViewer()
        {
            InitializeComponent();
        }



        public void View(int? id)
        {
            CurrentPicId = id;
            if(id.HasValue==false)
            {
                imageBox.Source = null;
                listView.ItemsSource = null;
                totalCarNumberTextRun.Text = string.Empty;
                return;
            }




            Lane lane = new Lane();
            Image<Gray, byte> finalImage;
            Image<Bgr, byte> frame1 = new Image<Bgr, byte>(string.Format(FilePathPattern, id));


            Bgr roadColor = lane.GetRoadColor(frame1);

            int sampleStart = id.Value - 3;

            Image<Bgr, byte>[] samples = GetSamples(sampleStart < 0 ? 0 : sampleStart, 6);

            Image<Bgra, byte> backgroundImage = lane.FindBackground(samples, roadColor);
            var cars1 = lane.FindCars(frame1, backgroundImage, out finalImage);

            imageBox.Source = lane.GetFocusArea(frame1).ToBitmap().ToBitmapImage();
            listView.ItemsSource = cars1;
            totalCarNumberTextRun.Text = cars1.Length.ToString();
        }

        private Image<Bgr, byte>[] GetSamples(int sampleStart, int length)
        {
            Contract.Requires(sampleStart >= 0);
            Contract.Requires(length >= 0);

            Image<Bgr, byte>[] samples = new Image<Bgr, byte>[length];

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = new Image<Bgr, byte>(string.Format(FilePathPattern, sampleStart++));
            }
            return samples;
        }




        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(imageBox);
            Car carGroup;
            if (e.RemovedItems.Count > 0)
            {
                carGroup = (Car)e.RemovedItems[0];
                var adorners = adornerLayer.GetAdorners(imageBox);
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

            if (e.AddedItems.Count != 1)
                return;
            carGroup = (Car)e.AddedItems[0];
            OutBoxAdorner outBoxAdorner = new OutBoxAdorner(imageBox);
            outBoxAdorner.Pen = new Pen(Brushes.Orange, 2);
            outBoxAdorner.Rectangle = carGroup.CarRectangle;
            adornerLayer.Add(outBoxAdorner);
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Car car = (Car)checkBox.DataContext;
            var adornerLayer = AdornerLayer.GetAdornerLayer(imageBox);

            //加上Adorner
            OutBoxAdorner outBoxAdorner = new OutBoxAdorner(imageBox);
            outBoxAdorner.Pen = new Pen(Brushes.LightGreen, 2);
            outBoxAdorner.Rectangle = car.CarRectangle;
            adornerLayer.Add(outBoxAdorner);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Car carGroup = (Car)checkBox.DataContext;
            var adornerLayer = AdornerLayer.GetAdornerLayer(imageBox);
            var adorners = adornerLayer.GetAdorners(imageBox);
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

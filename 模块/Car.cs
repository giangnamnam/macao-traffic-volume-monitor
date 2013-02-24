using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public class Car
    {


        /// <summary>
        /// 创造Car的实例
        /// </summary>
        /// <param name="carRectangle">车的外框</param>
        /// <param name="sourceImage">包含此车的原始图像</param>
        /// <param name="objectImage">包含此车的黑白图像</param>
        /// <returns></returns>
        public static Car CreateCar(Rectangle carRectangle, Image<Bgr, byte> sourceImage, Image<Gray, byte> objectImage)
        {
            var s = sourceImage.Copy(carRectangle);
            //CvInvoke.cvShowImage("s", s);
            var oi = objectImage.Copy(carRectangle);

            int blackCount = 0;
            for (int i = 0; i < oi.Height; i++)
            {
                for (int j = 0; j < oi.Width; j++)
                {
                    var pixel = oi[i, j];
                    if (Math.Abs(pixel.Intensity - 0) < double.Epsilon)
                        blackCount++;
                }
            }

            if (blackCount >= oi.Width * oi.Height * 0.4)
                throw new NotProperCarException();


            return new Car(carRectangle, s.Copy(oi));
        }




        public Image<Bgr, byte> Image { get; private set; }
        public ImageSource CarImage { get; private set; }

        public Rectangle CarRectangle { get; private set; }

        public DenseHistogram HistR { get; private set; }
        public DenseHistogram HistG { get; private set; }
        public DenseHistogram HistB { get; private set; }

        public DenseHistogram HistH { get; private set; }
        public DenseHistogram HistS { get; private set; }
        public DenseHistogram HistV { get; private set; }

        public DenseHistogram HistHue { get; private set; }

        private Car(Rectangle carRectangle, Image<Bgr, byte> image)
        {
            this.Image = image;
            //image.Save(@"D:\img.bmp");
            CarImage =ToBitmapImage( Image.ToBitmap());
            CarImage.Freeze();

            this.CarRectangle = carRectangle;
            
            HistR = new DenseHistogram(256, new RangeF(0, 256));
            HistR.Calculate(new[] { Image[0] }, false, null);
            HistR.MatND.ManagedArray.SetValue(0, 0);         
            HistR.Normalize(1);


            HistG = new DenseHistogram(256, new RangeF(0, 256));
            HistG.Calculate(new[] { image[1] }, false, null);
            HistG.MatND.ManagedArray.SetValue(0, 0);
            HistG.Normalize(1);

            HistB = new DenseHistogram(256, new RangeF(0, 256));
            HistB.Calculate(new[] { image[2] }, false, null);
            HistB.MatND.ManagedArray.SetValue(0, 0);
            HistB.Normalize(1);

            var hsvImage = image.Convert<Hsv, byte>();
            HistHue = new DenseHistogram(180, new RangeF(0, 180));
            HistHue.Calculate(new[] { hsvImage[0] }, false, null);
            HistHue.MatND.ManagedArray.SetValue(0, 0);
            HistHue.Normalize(1);
            //ValidateHistogram(HistHue);
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        //private static void ValidateHistogram(DenseHistogram histogram)
        //{
        //    if ((float)histogram.MatND.ManagedArray.GetValue(0) > 0.6)
        //        throw new NotProperCarException();
        //}
    }

    [Serializable]
    public class NotProperCarException : Exception
    {
        public NotProperCarException() { }
        public NotProperCarException(string message) : base(message) { }
        public NotProperCarException(string message, Exception inner) : base(message, inner) { }
        protected NotProperCarException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

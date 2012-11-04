using System;
using System.Drawing;
using System.Windows.Media;
using Emgu.CV;
using Emgu.CV.GPU;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeCalculator
{
    class Car
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
            //CvInvoke.cvShowImage("oi", oi);


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
            CarImage = Image.ToBitmap().ToBitmapImage();

            this.CarRectangle = carRectangle;

            HistR = new DenseHistogram(256, new RangeF(0, 255));
            HistR.Calculate(new[] { Image[0] }, false, null);
            HistR.MatND.ManagedArray.SetValue(0, 0);         
            HistR.Normalize(1);


            HistG = new DenseHistogram(256, new RangeF(0, 255));
            HistG.Calculate(new[] { image[1] }, false, null);
            HistG.MatND.ManagedArray.SetValue(0, 0);
            HistG.Normalize(1);

            HistB = new DenseHistogram(256, new RangeF(0, 255));
            HistB.Calculate(new[] { image[2] }, false, null);
            HistB.MatND.ManagedArray.SetValue(0, 0);
            HistB.Normalize(1);

            var hsvImage = image.Convert<Hsv, byte>();
            HistHue = new DenseHistogram(256, new RangeF(0, 179));
            HistHue.Calculate(new[] { hsvImage[0] }, false, null);
            HistHue.MatND.ManagedArray.SetValue(0, 0);
            HistHue.Normalize(1);
        }


    }
}

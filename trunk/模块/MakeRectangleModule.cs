using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor.Modules
{
    [Serializable]
    public class MakeRectangleModule
    { 
        private static Func<int, double> FindFacotrFunction(int farWidth, int nearWidth, int height)
        {
            double factor = (double)nearWidth / farWidth;

            double a = -0.00846614;
            double b = 3.125;

            b = factor;
            a = (1 - b) / height;

            return y => a * y + b;
        }
        
        
        public int FarWidth { get; set; }
        
        public Image<Bgr, byte> Run(Image<Bgr, byte> image)
        {
            Func<int, double> f = FindFacotrFunction(FarWidth, image.Width,image.Height);

            Image<Bgr, byte> newImage = new Image<Bgr, byte>(image.Size);

            double halfWidth = image.Width / 2.0;

            for (int j = 0; j < image.Height; j++)
            {
                double factor = f(j);
                for (int i = 0; i <= halfWidth; i++)
                {
                    double oldI = halfWidth - (halfWidth - i) / factor; // (-halfWidth + factor * halfWidth + i) / factor;

                    if (Math.Abs(oldI - (int)oldI) > double.Epsilon)
                    {
                        int l = (int)Math.Floor(oldI);
                        int h = (int)Math.Ceiling(oldI);

                        var cl = image[j, l];
                        var ch = image[j, h];

                        double red = cl.Red * (oldI - l) + ch.Red * (h - oldI);
                        double green = cl.Green * (oldI - l) + ch.Green * (h - oldI);
                        double blue = cl.Blue * (oldI - l) + ch.Blue * (h - oldI);
                        newImage[j, i] = new Bgr(blue, green, red);
                    }
                    else
                        newImage[j, i] = image[j, (int)oldI];
                }

                for (int i = (int)halfWidth; i < image.Width; i++)
                {
                    double oldI = halfWidth + (i - halfWidth) / factor; //(-halfWidth + factor * halfWidth + i) / factor;

                    if (Math.Abs(oldI - (int)oldI) > double.Epsilon)
                    {
                        int l = (int)Math.Floor(oldI);
                        int h = (int)Math.Ceiling(oldI);

                        var cl = image[j, l];
                        var ch = image[j, h];

                        double red = cl.Red * (oldI - l) + ch.Red * (h - oldI);
                        double green = cl.Green * (oldI - l) + ch.Green * (h - oldI);
                        double blue = cl.Blue * (oldI - l) + ch.Blue * (h - oldI);

                        newImage[j, i] = new Bgr(blue, green, red);
                    }
                    else
                        newImage[j, i] = image[j, (int)oldI];
                }
            }

            return newImage;
        }
    }
}

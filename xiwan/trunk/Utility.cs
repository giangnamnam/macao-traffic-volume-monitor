using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeCalculator
{
    static class Utility
    {
        public static List<TData> RemoveDeviatedComponent<TData>(List<TData> array, Func<TData, double> getComponent, double maxDeviation)
        {
            if (array.Count <= 1)
                return array;

            Comparison<TData> comparison = (d1, d2) =>
            {
                double c1 = getComponent(d1);
                double c2 = getComponent(d2);

                if (c1 < c2)
                    return -1;
                else if (c1 > c2)
                    return 1;
                else
                    return 0;
            };
            array.Sort(comparison);

           
            while (array.Count>=2)
            {
                if (getComponent(array[1]) - getComponent(array[0]) > maxDeviation)
                    array.RemoveAt(0);
                else
                    break;
            }

            while (array.Count >= 2)
            {
                if (getComponent(array[array.Count-1]) - getComponent(array[array.Count-2]) > maxDeviation)
                    array.RemoveAt(array.Count - 1);
                else
                    break;
            }


            //LinkedList<double> differences = new LinkedList<double>();

            //for (int i = 1; i < array.Count; i++)
            //{
            //    differences.AddLast(Math.Abs(getComponent(array[i - 1]) - getComponent(array[i])));
            //}

            //var node = differences.Last;
            //while (node != null && node.Value >= maxDeviation)
            //{
            //    node = node.Previous;
            //    array.RemoveAt(array.Count - 1);
            //}

            //node = differences.First;
            //while (node != null && node.Value >= maxDeviation)
            //{
            //    node = node.Next;
            //    array.RemoveAt(0);
            //}

            return array;
        }

        public static bool IsSimilarTo(this Bgr color, Bgr anotherColor, double tolerance)
        {
            if (Math.Abs(color.Red - anotherColor.Red) > tolerance)
                return false;
            if (Math.Abs(color.Green - anotherColor.Green) > tolerance)
                return false;
            if (Math.Abs(color.Blue - anotherColor.Blue) > tolerance)
                return false;
            return true;
        }

        public static bool IsSimilarTo(this Bgr color, Bgra anotherColor, double tolerance)
        {
            if (Math.Abs(color.Red - anotherColor.Red) > tolerance)
                return false;
            if (Math.Abs(color.Green - anotherColor.Green) > tolerance)
                return false;
            if (Math.Abs(color.Blue - anotherColor.Blue) > tolerance)
                return false;
            return true;
        }

        public static BitmapImage ToBitmapImage(this System.Drawing.Bitmap bitmap)
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

        public static Image<Bgra, byte> OverlayImages(params Image<Bgra, byte>[] images)
        {
            int width = images[0].Width;
            int height = images[0].Height;
            Image<Bgra, byte> result = new Image<Bgra, byte>(width, height);

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    Bgra[] cps = new Bgra[images.Length];
                    for (int i = 1; i < images.Length; i++)
                    {
                        cps[i] = images[i][row, column];
                    }

                    var nonTransparentColors = cps.Where(c => c.Alpha > double.Epsilon).ToArray();//获取不透明的颜色点。
                    if (nonTransparentColors.Length > 0)
                        result[row, column] = new Bgra(nonTransparentColors.Average(bgr => bgr.Blue),
                                                     nonTransparentColors.Average(bgr => bgr.Green),
                                                     nonTransparentColors.Average(bgr => bgr.Red), 255);


                    //if (Math.Abs(result[row, column].Alpha - 255) < double.Epsilon)
                    //    continue;

                    //var c1 = images[i - 1][row, column];
                    //var c2 = images[i][row, column];

                    //if (Math.Abs(c1.Alpha - 255) < double.Epsilon && Math.Abs(c2.Alpha - 255) < double.Epsilon)
                    //    result[row, column] = new Bgra((c1.Blue + c2.Blue) / 2, (c1.Green + c2.Green) / 2, (c1.Red + c2.Red) / 2, 255);
                    //else if (c1.Alpha <= double.Epsilon)//c1透明
                    //    result[row, column] = c2;
                    //else
                    //    result[row, column] = c1;
                }
            }
            return result;
        }


        public static Image<Gray, byte> RemoveSame(Image<Bgr, byte> image1, Image<Bgra, byte> image2, int tolerance)
        {
            int width = image1.Width;
            int height = image1.Height;
            Image<Gray, byte> result = new Image<Gray, byte>(width, height);

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    //System.Diagnostics.Debug.WriteLine("{0},{1}", row, column);
                    if (image1[row, column].IsSimilarTo(image2[row, column], tolerance))
                        result[row, column] = new Gray(0);
                    else
                        result[row, column] = new Gray(255);//image1[row, column];
                }
            }
            return result;
        }

        public static System.Windows.Media.Color ToWpfColor(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
        }
    }
}

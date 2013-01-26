using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
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

        public static Image<Bgr,byte> AutoCrop(Image<Bgr,byte> image)
        {
            var gray = image.Convert<Gray, byte>();
            CvInvoke.cvThreshold(gray, gray, 0, 255, THRESH.CV_THRESH_BINARY);

            var contour= gray.FindContours();
            return image.Copy(contour.BoundingRectangle);
        }
    }
}

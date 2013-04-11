using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    static class Utility
    {

        internal static Image<Gray, byte> FindSobelEdge(Image<Gray, byte> grayImage)
        {
            Image<Gray, float> sobelX = grayImage.Sobel(1, 0, 3);
            Image<Gray, float> sobelY = grayImage.Sobel(0, 1, 3);

            //Convert negative values to positive valus
            sobelX = sobelX.AbsDiff(new Gray(0));
            sobelY = sobelY.AbsDiff(new Gray(0));

            Image<Gray, float> sobel = sobelX + sobelY;
            //Find sobel min or max value
            double[] mins, maxs;
            //Find sobel min or max value position
            Point[] minLoc, maxLoc;
            sobel.MinMax(out mins, out maxs, out minLoc, out maxLoc);
            //Conversion to 8-bit image
            Image<Gray, Byte> sobelImage = sobel.ConvertScale<byte>(255 / maxs[0], 0);

            sobelX.Dispose();
            sobelY.Dispose();
            sobel.Dispose();


            return sobelImage;
        }
    }
}

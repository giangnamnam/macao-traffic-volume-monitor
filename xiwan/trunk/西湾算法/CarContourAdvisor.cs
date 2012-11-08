using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
//using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    /// <summary>
    /// 
    /// </summary>
    class CarContourAdvisor
    {
        private readonly int maxCarLength;
        private readonly int minArea;
        Image<Bgr, byte> sourceImage;
        private readonly int width;
        private readonly int height;

        public CarContourAdvisor(Image<Bgr, byte> sourceImage)
        {
            this.sourceImage = sourceImage;
            width = sourceImage.Width;
            height = sourceImage.Height;
        }

        //Image<Gray, byte> objectImage;

        public List<Contour<Point>> GetContours(Contour<Point> contour)
        {
            return GetContours(contour, 0);
        }

        private List<Contour<Point>> GetContours(Contour<Point> contour, int iteration)
        {
            //if (iteration == 0)
            //    System.Diagnostics.Debug.WriteLine(contour.Area);
            List<Contour<Point>> list = new List<Contour<Point>>();
            if (contour.Area < 40)
            {
                var diluteContour = DiluteContour(contour, iteration);
                list.Add(diluteContour);
                return list;
            }

            var erodedContours = ErodeContour(contour, 1);


            Contract.Assert(erodedContours.Length >= 1);
            if (erodedContours.Length >= 1) //从一个连通域里生出了多个连通域
            {
                foreach (var c in erodedContours)
                {
                    list.AddRange(GetContours(c, iteration + 1));
                }
            }

            return list;
        }

        Contour<Point>[] ErodeContour(Contour<Point> contour, int iteration)
        {
            Contour<Point> subContours;
            var white = new Gray(255);
            using (Image<Gray, byte> contourImage = new Image<Gray, byte>(width, height))
            {

                contourImage.Draw(contour, white, white, 0, -1);
                subContours = contourImage.Erode(iteration).FindContours();
            }

            List<Contour<Point>> list = new List<Contour<Point>>();
            while (subContours != null)
            {
                list.Add(subContours);
                subContours = subContours.HNext;
            }

            return list.ToArray();
        }

        Contour<Point> DiluteContour(Contour<Point> contour, int iteration)
        {
            if (iteration == 0)
                return contour;

            using (Image<Gray, byte> contourImage = new Image<Gray, byte>(width, height))
            {
                var white = new Gray(255);
                contourImage.Draw(contour, white, white, 0, -1);
                Contour<Point> subContours = contourImage.Dilate(iteration).FindContours();

                Contract.Assert(subContours.HNext == null);

                return subContours;
            }
        }
    }
}

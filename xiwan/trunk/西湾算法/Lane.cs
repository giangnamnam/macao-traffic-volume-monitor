﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Gqqnbig.Statistics;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public class Lane
    {
        //public TrafficDirection TrafficDirection { get; private set; }

        public double RgbSimilarityThreshold = 0.4031;

        internal DiskCaptureRetriever CaptureRetriever { get; set; }

        readonly Image<Gray, byte> mask;
        readonly Rectangle regionOfInterest;

        const int tolerance = 20;

        readonly Point[] backgroundPoints = new[]
                                   {
                                       new Point(117, 284), new Point(134, 284), new Point(154, 284), new Point(199, 284), new Point(219, 284), new Point(235, 284),
                                       new Point(101, 162), new Point(116, 162), new Point(125, 162), new Point(160, 162), new Point(170, 162), new Point(187, 162)
                                   };

        readonly int maxCarWidth = 17;
        readonly int maxCarLength = 15;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Lane(DiskCaptureRetriever captureRetriever, string maskFilePath)
        {
            CaptureRetriever = captureRetriever;
            mask = new Image<Gray, byte>(maskFilePath);
            //TrafficDirection = TrafficDirection.GoUp;

            var contours = mask.FindContours();
            regionOfInterest = contours.BoundingRectangle;
        }

        public LaneCapture Analyze(int id)
        {
            var orginialImage = CaptureRetriever.GetCapture(id);
            var focusedImage = GetFocusArea(orginialImage);

            Width = focusedImage.Width;
            Height = focusedImage.Height;

            var roadColor = GetRoadColor(orginialImage);
            var backgroundImage = GetBackground(roadColor, id);
            var car1 = Utility.RemoveSame(focusedImage, backgroundImage, tolerance);


            Image<Gray, byte> gaussianImage = car1.SmoothGaussian(3);
            Image<Gray, byte> afterThreshold = new Image<Gray, byte>(gaussianImage.Width, gaussianImage.Height);
            CvInvoke.cvThreshold(gaussianImage, afterThreshold, 0, 255, THRESH.CV_THRESH_OTSU);

            var finalImage = afterThreshold.Erode(1).Dilate(1);
            var contours = finalImage.FindContours();
            var inContourColor = new Gray(255);
            while (contours != null)
            {
                //填充连通域。有时候背景图和前景图可能颜色相似，导致车的轮廓里面有洞。
                finalImage.Draw(contours, inContourColor, inContourColor, 0, -1);
                contours = contours.HNext;
            }

            List<Car> groups = new List<Car>();
            CarContourAdvisor advisor = new CarContourAdvisor(focusedImage);
            List<Contour<Point>> contourList = new List<Contour<Point>>();
            contours = finalImage.FindContours();//第二次取连通域，这下没有洞了。
            while (contours != null)
            {
                contourList.AddRange(advisor.GetContours(contours));
                contours = contours.HNext;
            }

            foreach (var element in contourList)
            {
                var carGroup = new PossibleCarGroup(focusedImage, finalImage, element, maxCarWidth, maxCarLength, 12);
                if (carGroup.CarNumber > 0)
                {
                    var cars = carGroup.GetCars();
                    Array.ForEach(cars, c => { if (c != null)groups.Add(c); });
                }
            }
            //var inContourColor = new Gray(255);
            //while (contours != null)
            //{
            //    //填充连通域。有时候背景图和前景图可能颜色相似，导致车的轮廓里面有洞。
            //    finalImage.Draw(contours, inContourColor, inContourColor, 0, -1);

            //    var carGroup = new PossibleCarGroup(focusedImage, finalImage, contours, maxCarWidth, maxCarLength, 12);
            //    if (carGroup.CarNumber > 0)
            //    {
            //        var cars = carGroup.GetCars();
            //        Array.ForEach(cars, c => { if (c != null)groups.Add(c); });
            //    }
            //    //break;
            //    contours = contours.HNext;
            //}

            car1.Dispose();
            gaussianImage.Dispose();
            afterThreshold.Dispose();

            return new LaneCapture(orginialImage, focusedImage, backgroundImage, finalImage, groups.ToArray());
        }



        //public Image<Bgr, byte> GetFocusCapture()
        //{
        //    var capture = CaptureRetriever.GetCapture(CaptureId);
        //    return GetFocusArea(capture);
        //}

        private Image<Bgra, byte> GetBackground(Bgr roadColor, int forCapturer)
        {
            int sampleStart = forCapturer - 3;

            Image<Bgr, byte>[] samples = GetSamples(sampleStart < 0 ? 0 : sampleStart, 6);

            Parallel.For(0, samples.Length, i =>
                                                            {
                                                                samples[i] = GetFocusArea(samples[i]);
                                                            });

            int width = samples[0].Width;
            int height = samples[0].Height;
            Image<Bgra, byte> background = new Image<Bgra, byte>(width, height);

            Parallel.For(0, width, x =>
                                       {
                                           List<Bgr> colors = new List<Bgr>(samples.Length + 1);
                                           for (int y = 0; y < height; y++)
                                           {
                                               colors.AddRange(samples.Select(f => f[y, x]));
                                               //System.Diagnostics.Debug.WriteLine(x + "," + y);
                                               colors.Add(roadColor);
                                               //colors.Add(roadColor);

                                               colors = Utility.RemoveDeviatedComponent(colors, c => c.Red, 10);
                                               colors = Utility.RemoveDeviatedComponent(colors, c => c.Green, 10);
                                               colors = Utility.RemoveDeviatedComponent(colors, c => c.Blue, 10);

                                               if (colors.Any())
                                                   background[y, x] = new Bgra(colors.Select(bgr => bgr.Blue).Median(),
                                                                               colors.Select(bgr => bgr.Green).Median(),
                                                                               colors.Select(bgr => bgr.Red).Median(), 255);

                                                   //background[y, x] = new Bgra(colors.Average(bgr => bgr.Blue),
                                               //                          colors.Average(bgr => bgr.Green),
                                               //                          colors.Average(bgr => bgr.Red), 255);
                                               else
                                                   background[y, x] = new Bgra(0, 0, 0, 0);

                                               colors.Clear();
                                           }
                                       });
            //CvInvoke.cvShowImage("background", background);
            return background;
        }

        //public Car[] GetCars()
        //{
        //    var image = GetFocusCapture();
        //    Width = image.Width;
        //    Height = image.Height;

        //    var backgroundImage = GetBackground(GetRoadColor(CaptureRetriever.GetCapture(CaptureId)));
        //    var car1 = Utility.RemoveSame(image, backgroundImage, tolerance);

        //    Image<Gray, byte> gaussianImage = car1.SmoothGaussian(3);
        //    Image<Gray, byte> afterThreshold = new Image<Gray, byte>(gaussianImage.Width, gaussianImage.Height);
        //    CvInvoke.cvThreshold(gaussianImage, afterThreshold, 0, 255, THRESH.CV_THRESH_OTSU);

        //    var finalImage = afterThreshold.Erode(1).Dilate(1);
        //    //CvInvoke.cvShowImage("final", finalImage);

        //    var contours = finalImage.FindContours();

        //    List<Car> groups = new List<Car>();
        //    var inContourColor = new Gray(255);
        //    while (contours != null)
        //    {
        //        //填充连通域。有时候背景图和前景图可能颜色相似，导致车的轮廓里面有洞。
        //        finalImage.Draw(contours, inContourColor, inContourColor, 0, -1);

        //        //System.Diagnostics.Debug.WriteLine(contours.Area);
        //        //if(contours.Area>40) 进行两次腐蚀


        //        var carGroup = new PossibleCarGroup(image, finalImage, contours, maxCarWidth, maxCarLength, 12, 85);
        //        if (carGroup.CarNumber > 0)
        //        {
        //            var cars = carGroup.GetCars();
        //            Array.ForEach(cars, c => { if (c != null)groups.Add(c); });
        //        }
        //        //break;
        //        contours = contours.HNext;
        //    }

        //    car1.Dispose();
        //    gaussianImage.Dispose();
        //    afterThreshold.Dispose();

        //    return groups.ToArray();
        //}

        /// <summary>
        /// 处理原始图像，获得用于后续处理的部分。
        /// </summary>
        /// <param name="originalImage"></param>
        /// <returns></returns>
        private Image<Bgr, byte> GetFocusArea(Image<Bgr, byte> originalImage)
        {
            var image = UnDistort(originalImage);
            image = image.Copy(mask).Copy(regionOfInterest);
            return image;
        }

        private Bgr GetRoadColor(Image<Bgr, byte> frame)
        {
            var samplingBackgroundColors = new List<Bgr>();

            for (int i = 0; i < backgroundPoints.Length; i++)
            {
                samplingBackgroundColors.Add(frame[backgroundPoints[i]]);
            }

            samplingBackgroundColors = Utility.RemoveDeviatedComponent(samplingBackgroundColors, c => c.Red, 10);
            samplingBackgroundColors = Utility.RemoveDeviatedComponent(samplingBackgroundColors, c => c.Green, 10);
            samplingBackgroundColors = Utility.RemoveDeviatedComponent(samplingBackgroundColors, c => c.Blue, 10);

            var backgroundColor = new Bgr(samplingBackgroundColors.Average(bgr => bgr.Blue),
                                          samplingBackgroundColors.Average(bgr => bgr.Green),
                                          samplingBackgroundColors.Average(bgr => bgr.Red));
            return backgroundColor;
        }


        private Image<Bgr, byte> UnDistort(Image<Bgr, byte> frame)
        {
            //CvInvoke.cvShowImage("original", frame);

            Func<double, double> getCenterX = y => 0.22518 * y + 0.30778;
            Func<int, double> getLeftFactorByY = y => -0.005580 * y + 2.6072;
            Func<int, double> getRightFactorByY = y => -0.00530 * y + 2.52673;
            Func<int, double> getLeftSphereFactor = y => 1.140e-5 * (y - 153) * (y - 153) + 0.7367;
            Func<int, double> getRightSphereFactor = y => 1.304e-5 * (y - 153) * (y - 153) + 0.7367;


            var width = frame.Width;
            var height = frame.Height;
            Image<Bgr, byte> newFrame = new Image<Bgr, byte>(width, height);
            int newWidth = newFrame.Width;

            Parallel.For(32, height, y =>
              {
                  int centerX = (int)(getCenterX((double)y / height) * width);
                  int newCenterX = (int)(getCenterX((double)y / height) * newWidth);
                  double factor = getLeftFactorByY(y) * getLeftSphereFactor(y);

                  int lastNewX = -1;
                  for (int x = 0; x <= centerX; x++)
                  {
                      int horizontalDistance = centerX - x;
                      int newHorizontalDistance = (int)Math.Round(factor * horizontalDistance);

                      int newX = newCenterX - newHorizontalDistance;
                      if (newX >= 0)
                      {
                          newFrame[y, newX] = frame[y, x];

                          if (lastNewX != -1 && newX - lastNewX > 1)
                              Interplote(newFrame, lastNewX, y, newX, y);
                          lastNewX = newX;
                      }
                  }
              });

            Parallel.For(32, height, y =>
                {
                    int centerX = (int)(getCenterX((double)y / height) * width);
                    int newCenterX = (int)(getCenterX((double)y / height) * newWidth);
                    double factor = getRightFactorByY(y) * getRightSphereFactor(y);

                    int lastNewX = -1;
                    for (int x = centerX + 1; x < width; x++)
                    {
                        int horizontalDistance = x - centerX;
                        int newHorizontalDistance = (int)Math.Round(factor * horizontalDistance);

                        int newX = newCenterX + newHorizontalDistance;
                        if (newX < width)
                        {
                            newFrame[y, newX] = frame[y, x];
                            if (lastNewX != -1 && newX - lastNewX > 1)
                                Interplote(newFrame, lastNewX, y, newX, y);
                            lastNewX = newX;
                        }
                    }
                });

            //CvInvoke.cvShowImage("enlarge", newFrame);

            Matrix<double> mat = new Matrix<double>(2, 3);
            mat[0, 0] = 1;
            mat[0, 1] = -0.2349;
            mat[0, 2] = 0;
            mat[1, 0] = 0;
            mat[1, 1] = 1;
            mat[1, 2] = 0;

            var warpImage = newFrame.WarpAffine(mat, INTER.CV_INTER_LINEAR, WARP.CV_WARP_DEFAULT, new Bgr(0, 0, 0));
            //CvInvoke.cvShowImage("WarpAffine", warpImage);
            return warpImage;
        }




        /// <summary>
        /// 已知(x1,y1)、(x2,y2)的颜色，对中间的颜色进行插值。
        /// </summary>
        /// <param name="image"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        void Interplote(Image<Bgr, byte> image, int x1, int y1, int x2, int y2)
        {
            if (y1 == y2)
            {
                var c1 = image[y1, x1];
                var c2 = image[y2, x2];

                var distance = x2 - x1;
                double incR = (c2.Red - c1.Red) / distance;
                double incG = (c2.Green - c1.Green) / distance;
                double incB = (c2.Blue - c1.Blue) / distance;

                for (int i = 1; i + x1 < x2; i++)
                {
                    image[y1, x1 + i] = new Bgr(c1.Blue + incB, c1.Green + incG, c2.Red + incR);
                }
            }


        }


        private Image<Bgr, byte>[] GetSamples(int sampleStart, int length)
        {
            Contract.Requires(sampleStart >= 0);
            Contract.Requires(length >= 0);

            Image<Bgr, byte>[] samples = new Image<Bgr, byte>[length];

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = CaptureRetriever.GetCapture(sampleStart++);
            }
            return samples;
        }

    }


    public enum TrafficDirection
    {
        GoUp,
        GoDown
    }
}

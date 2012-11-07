﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Gqqnbig.Statistics;
using System.Linq;

namespace Gqqnbig.TrafficVolumeCalculator
{
    class Lane
    {
        public TrafficDirection TrafficDirection { get; private set; }

        public double RgbSimilarityThreshold = 0.4031;

        //public Lane(string maskFileName, TrafficDirection trafficDirection, double similarityThreshold)
        //{
        //    Mask = new Image<Gray, byte>(maskFileName);
        //    TrafficDirection = trafficDirection;
        //    RgbSimilarityThreshold = similarityThreshold;
        //}

        readonly Image<Gray, byte> mask;
        readonly Rectangle regionOfInterest;

        const int tolerance = 20;

        readonly Point[] backgroundPoints = new[]
                                   {
                                       new Point(117, 284), new Point(134, 284), new Point(154, 284), new Point(199, 284), new Point(219, 284), new Point(235, 284),
                                       new Point(101, 162), new Point(116, 162), new Point(125, 162), new Point(160, 162), new Point(170, 162), new Point(187, 162)
                                   };

        readonly int maxCarWidth = 15;
        readonly int maxCarLength = 15;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Lane()
        {
            mask = new Image<Gray, byte>(@"D:\文件\毕业设计\西湾大桥氹仔端\图片\mask-Lane1.gif");
            TrafficDirection = TrafficDirection.GoUp;

            var contours = mask.FindContours();
            regionOfInterest = contours.BoundingRectangle;
        }


        public Car[] FindCars(Image<Bgr, byte> image, Image<Bgra, byte> backgroundImage, out Image<Gray, byte> finalImage)
        {
            image = GetFocusArea(image);
            Width = image.Width;
            Height = image.Height;

            //CvInvoke.cvShowImage("originalImage", image);
            var car1 = Utility.RemoveSame(image, backgroundImage, tolerance);
            //CvInvoke.cvShowImage("car 1", car1);

            Image<Gray, byte> gaussianImage = car1.SmoothGaussian(3);
            //CvInvoke.cvShowImage("gaussianImage", gaussianImage);

            Image<Gray, byte> afterThreshold = new Image<Gray, byte>(gaussianImage.Width, gaussianImage.Height);
            CvInvoke.cvThreshold(gaussianImage, afterThreshold, 0, 255, THRESH.CV_THRESH_OTSU);
            //CvInvoke.cvShowImage("afterThreshold", afterThreshold);


            finalImage = afterThreshold.Erode(1).Dilate(1);
            CvInvoke.cvShowImage("final", finalImage);

            var contours = finalImage.FindContours();

            List<Car> groups = new List<Car>();
            var inContourColor = new Gray(255);
            while (contours != null)
            {
                //填充连通域。有时候背景图和前景图可能颜色相似，导致车的轮廓里面有洞。
                finalImage.Draw(contours, inContourColor, inContourColor, 0, -1);

                //System.Diagnostics.Debug.WriteLine(contours.Area);
                //if(contours.Area>40) 进行两次腐蚀


                var carGroup = new PossibleCarGroup(image, finalImage, contours, maxCarWidth, maxCarLength, 12, 85);
                if (carGroup.CarNumber > 0)
                {
                    var cars = carGroup.GetCars();
                    Array.ForEach(cars, c => { if (c != null)groups.Add(c); });
                }
                //break;
                contours = contours.HNext;
            }

            return groups.ToArray();
        }

        /// <summary>
        /// 处理原始图像，获得用于后续处理的部分。
        /// </summary>
        /// <param name="originalImage"></param>
        /// <returns></returns>
        public Image<Bgr, byte> GetFocusArea(Image<Bgr, byte> originalImage)
        {
            var image = UnDistort(originalImage);
            image = image.Copy(mask).Copy(regionOfInterest);
            return image;
        }

        public Bgr GetRoadColor(Image<Bgr, byte> frame)
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


        /// <summary>
        /// 获取背景
        /// </summary>
        /// <param name="samples">根据这些图片来获取背景</param>
        /// <param name="roadColor"> </param>
        /// <returns></returns>
        public Image<Bgra, byte> FindBackground(Image<Bgr, byte>[] samples, Bgr roadColor)
        {
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
            CvInvoke.cvShowImage("background", background);
            return background;
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




        public CarMatch[] FindCarMatch(Car[] cars1, Car[] cars2)
        {
            double similarityThreshold = 0.26;

            List<CarMatch> list = new List<CarMatch>();
            if (TrafficDirection == TrafficDirection.GoUp)
            {
                foreach (var c2 in cars2)
                {
                    foreach (var c1 in cars1)
                    {
                        if (c1.CarRectangle.Top <= c2.CarRectangle.Top)
                            break;

                        //假设车不改变车道。
                        if (Math.Abs(c1.CarRectangle.Left - c2.CarRectangle.Left) > 5)
                            continue;

                        CarMatch cm = new CarMatch(c1, c2);

                        if (cm.RS > similarityThreshold && cm.GS > similarityThreshold && cm.BS > similarityThreshold)
                            list.Add(cm);
                    }
                }
            }

            list = FindOneToOneBestMatch(list);
            if (list.Count > 2)
            {
                return RemoveDeviation(list).ToArray();
            }
            else
                return list.ToArray();
        }

        static List<CarMatch> FindOneToOneBestMatch(List<CarMatch> list)
        {
            if (list.Count == 0)
                return list;

            /*
             * 算法：
             * 1. 先从矩阵中找出相似度最高的对，
             * 2. 这个对里两个元素的其他匹配都被删除。
             * 3. 从剩余的矩阵中找最高匹配，以此类推。 
             */

            List<CarMatch> oneToOneMatch = new List<CarMatch>(list.Count);

            IComparer<CarMatch> comparer = new CarMatchComparer();
            list.Sort(comparer);

        Step1:
            var m = list[0];
            oneToOneMatch.Add(m);

            list.RemoveAt(0);
            int index = 0;
            while (index < list.Count)//有必要倒着删加快速度么？
            {
                if (list[index].Car1 == m.Car1 || list[index].Car2 == m.Car2)
                    list.RemoveAt(index); //第2步
                else
                    index++;
            }

            if (list.Count == 0)
                return oneToOneMatch;
            else
                goto Step1;
        }

        static IEnumerable<CarMatch> RemoveDeviation(List<CarMatch> list)
        {
            //用拉依达准则法。
            Tuple<CarMatch, int>[] carMoves = new Tuple<CarMatch, int>[list.Count];
            for (int i = 0; i < carMoves.Length; i++)
            {
                carMoves[i] = Tuple.Create(list[i], list[i].Car1.CarRectangle.Top - list[i].Car2.CarRectangle.Top);
            }

            var mean = carMoves.Average(m => m.Item2);
            var sd = Math.Sqrt(carMoves.Variance(mean, m => m.Item2));

            var rersult = from m in carMoves
                          where Math.Abs(m.Item1.Car1.CarRectangle.Top - m.Item1.Car2.CarRectangle.Top - mean) <= 2 * sd
                          select m.Item1;
            return rersult;

        }
    }


    enum TrafficDirection
    {
        GoUp,
        GoDown
    }
}

using System;
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

        public Lane(string maskFilePath)
        {
            mask = new Image<Gray, byte>(maskFilePath);
            //TrafficDirection = TrafficDirection.GoUp;

            var contours = mask.FindContours();
            regionOfInterest = contours.BoundingRectangle;
        }

        public LaneCapture Analyze(Image<Bgr, byte> orginialImage, ICollection<Image<Bgr, byte>> samples)
        {
            var focusedImage = GetFocusArea(orginialImage);

            Width = focusedImage.Width;
            Height = focusedImage.Height;

            var roadColor = GetRoadColor(orginialImage);
            var backgroundImage = GetBackground(roadColor, samples);
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

        private Image<Bgra, byte> GetBackground(Bgr roadColor, ICollection<Image<Bgr, byte>> samples)
        {
            Contract.Requires(samples.Count > 0);

            var focusedSamples = samples.AsParallel().Select(GetFocusArea).ToArray();

            int width = focusedSamples[0].Width;
            int height = focusedSamples[0].Height;
            Image<Bgra, byte> background = new Image<Bgra, byte>(width, height);

            Parallel.For(0, width, x =>
                                       {
                                           List<Bgr> colors = new List<Bgr>(focusedSamples.Length + 1);
                                           for (int y = 0; y < height; y++)
                                           {
                                               colors.AddRange(focusedSamples.Select(f => f[y, x]));
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


        /// <summary>
        /// 处理原始图像，获得用于后续处理的部分。
        /// </summary>
        /// <param name="originalImage"></param>
        /// <returns></returns>
        private Image<Bgr, byte> GetFocusArea(Image<Bgr, byte> originalImage)
        {
            var image = originalImage.Copy(mask).Copy(regionOfInterest);

            image = SkewTransform(image);
            image = MakeRectangle(image);
            //image = VerticalExpand(image);

            return image;
        }

        /// <summary>
        /// 切变
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Image<Bgr, byte> SkewTransform(Image<Bgr, byte> image)
        {
            int leftSpace = (int)(Math.Tan(8.2 / 180 * Math.PI) * image.Height);
            var roi = new Rectangle(new Point(leftSpace, 0), image.Size);
            var newImage = new Image<Bgr, byte>(image.Width + leftSpace, image.Height);
            newImage.ROI = roi;

            image.Copy(newImage, null);
            newImage.ROI = Rectangle.Empty;

            Matrix<double> mat = new Matrix<double>(2, 3);
            mat[0, 0] = 1;
            mat[0, 1] = -Math.Tan(8.2 / 180 * Math.PI);
            mat[0, 2] = 0;
            mat[1, 0] = 0;
            mat[1, 1] = 1;
            mat[1, 2] = 0;

            var warpImage = newImage.WarpAffine(mat, INTER.CV_INTER_LINEAR, WARP.CV_WARP_DEFAULT, new Bgr(0, 0, 0));

            return Utility.AutoCrop(warpImage);
        }

        private static Image<Bgr,byte> MakeRectangle(Image<Bgr,byte> image)
        {
            Func<int, double> f = y => -0.00409494 * y + 1.647;
            Image<Bgr, byte> newImage = new Image<Bgr, byte>(image.Size);

            double halfWidth = image.Width / 2.0;

            for (int j = 0; j < image.Height; j++)
            {
                double factor = f(j);
                for (int i = 0; i <= halfWidth; i++)
                {
                    double oldI = halfWidth - (halfWidth - i) / factor; // (-halfWidth + factor * halfWidth + i) / factor;

                    int l = (int)Math.Floor(oldI);
                    int h = (int)Math.Ceiling(oldI);

                    var cl = image[j, l];
                    var ch = image[j, h];

                    double red = cl.Red * (oldI - l) + ch.Red * (h - oldI);
                    double green = cl.Green * (oldI - l) + ch.Green * (h - oldI);
                    double blue = cl.Blue * (oldI - l) + ch.Blue * (h - oldI);
                    newImage[j, i] = new Bgr(blue, green, red);
                }

                for (int i = (int)halfWidth; i < image.Width; i++)
                {
                    double oldI = halfWidth + (i - halfWidth) / factor; //(-halfWidth + factor * halfWidth + i) / factor;

                    int l = (int)Math.Floor(oldI);
                    int h = (int)Math.Ceiling(oldI);

                    var cl = image[j, l];
                    var ch = image[j, h];

                    double red = cl.Red * (oldI - l) + ch.Red * (h - oldI);
                    double green = cl.Green * (oldI - l) + ch.Green * (h - oldI);
                    double blue = cl.Blue * (oldI - l) + ch.Blue * (h - oldI);

                    newImage[j, i] = new Bgr(blue, green, red);
                }
            }

            return newImage;
        }

        /// <summary>
        /// 进行纵向拉伸，取消近大远小。
        /// </summary>
        /// <param name="image"></param>
        private static Image<Bgr, byte> VerticalExpand(Image<Bgr, byte> image)
        {
            double a =9 ;
            double b = -6089;
            double c = 3542;

            Func<double, double> f = y => 1.0 / 18 * (6089 - Math.Sqrt(37075921 - 127512 * y));
            var newImage = new Image<Bgr, byte>(image.Width, (int)(a*image.Height*image.Height+b*image.Height+c));
            for (int x = 0; x < newImage.Width; x++)
            {
                for (int y = 0; y < newImage.Height; y++)
                {
                    double oy = f(y); //获取原图的y。

                    if (Math.Abs(oy - (int)oy) > double.Epsilon)
                    {
                        //oy一般是小数，获得oy的上下界。
                        int yl = (int)Math.Floor(oy);
                        int yh = (int)Math.Ceiling(oy);

                        var cl = image[yl, x];
                        var ch = image[yh, x];

                        double red = cl.Red * (oy - yl) + ch.Red * (yh - oy);
                        double green = cl.Green * (oy - yl) + ch.Green * (yh - oy);
                        double blue = cl.Blue * (oy - yl) + ch.Blue * (yh - oy);


                        newImage[y, x] = new Bgr(blue, green, red);
                    }
                    else
                        newImage[y, x] = image[(int)oy, x];
                }
            }

            //CvInvoke.cvShowImage("NewGetOldInterpolate " + imageWindowId++, newImage);
            return newImage;
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


        //private Image<Bgr, byte>[] GetSamples(int length)
        //{
        //    Contract.Requires(length >= 0);

        //    Image<Bgr, byte>[] samples = new Image<Bgr, byte>[length];

        //    for (int i = 0; i < length; i++)
        //    {
        //        samples[i] = CaptureRetriever.GetRelativeCapture(i - (int)Math.Ceiling(length / 2.0));
        //    }
        //    return samples;
        //}

    }


    public enum TrafficDirection
    {
        GoUp,
        GoDown
    }
}

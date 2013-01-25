using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Gqqnbig.Mathematics.Geometry;
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

        /// <summary>
        /// 获取本车道在图片中的宽度
        /// </summary>
        public int Width { get; private set; }


        /// <summary>
        /// 获取本车道在图片中的长度
        /// </summary>
        public int Height { get; private set; }

        public Lane(string maskFilePath)
        {
            //TrafficDirection = TrafficDirection.GoUp;

            mask = new Image<Gray, byte>(maskFilePath);

            for (int i = 0; i < mask.Height; i++)
            {
                for (int j = 0; j < mask.Width; j++)
                {
                    var g = mask[i, j];
                    if (g.Intensity > 0 && g.Intensity < 255)
                        throw new ArgumentException(string.Format("蒙版图像的({0},{1})处有除了黑白外的其他颜色。", j, i));
                }
            }

            var contours = mask.FindContours();
            regionOfInterest = contours.BoundingRectangle;
        }

        public LaneCapture Analyze(Image<Bgr, byte> orginialImage, ICollection<Image<Bgr, byte>> samples)
        {
            List<Image<Bgr, byte>> progressImages = new List<Image<Bgr, byte>>();

            var focusedImage = GetFocusArea(orginialImage);
            Width = focusedImage.Width;
            Height = focusedImage.Height;
            progressImages.Add(focusedImage);

            var objectImage = Utility.FindSobelEdge(focusedImage.Convert<Gray, byte>());

            //Image<Gray, Byte> modelImage = new Image<Gray, byte>(@"..\..\高伟乐街与荷兰园大马路交界\算法\arrow.bmp");
            Image<Gray, Byte> observedImage = focusedImage.Copy(new Rectangle(20, 190, 55, 85)).Convert<Gray, byte>();

            Image<Gray, byte> threshImage = new Image<Gray, byte>(observedImage.Width, observedImage.Height);

            CvInvoke.cvThreshold(observedImage, threshImage, 0, 255, THRESH.CV_THRESH_OTSU);
            //progressImages.Add(threshImage.Convert<Bgr, byte>());
            progressImages.Add(threshImage.Canny(new Gray(50), new Gray(100)).Convert<Bgr, byte>());


            LineSegment2D[] lines = threshImage.HoughLines(new Gray(50), new Gray(100), 1, System.Math.PI / 180,
                threshold: 12, minLineWidth: 10, gapBetweenLines: 10)[0];

            //image1 = grayImage.Convert<Bgr, byte>();


            Image<Bgr, byte> tmpImage = observedImage.Convert<Bgr, byte>().Copy();
            for (int i = 0; i < lines.Length; i++)
            {
                LineSegment2D m = lines[i];
                tmpImage.Draw(m, new Bgr(0, 255, 0), 1);
            }
            progressImages.Add(tmpImage.Copy());




            //CvInvoke.cvShowImage("tmpImage", tmpImage);

            if (new TolerantValue { Value = 7, Tolerance = 3.1 }.IsInRangle(lines.Length) &&
                IsArrow(lines, new TolerantValue { Value = -64, Tolerance = 14 },
                                new TolerantValue { Value = 69, Tolerance = 14 },
                                new TolerantValue { Value = 86, Tolerance = 5 }))
            {
                Debug.WriteLine("有箭头");
                return new LaneCapture(progressImages.ToArray(), new Car[0]);
            }
            else
                return new LaneCapture(progressImages.ToArray(), new[] { Car.CreateCar(new Rectangle(20, 190, 55, 85), focusedImage, objectImage) });


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
            var image = originalImage.Copy(mask);//.Copy(regionOfInterest);

            image = PerspectiveTransform(image);

            image = Utility.AutoCrop(image);
            //image = new AddSpaceModule { Right = 50 }.Run(image);
            //image = new SkewTransformModule { Angle = -19.75 }.Run(image);
            //image = new AddSpaceModule { Left = 13 }.Run(image);
            //image = new MakeRectangleModule { FarWidth = 48 }.Run(image);
            //image = VerticalExpand(image);

            return image;
        }


        private Image<Bgr, byte> PerspectiveTransform(Image<Bgr, byte> image)
        {
            PointF[] srcs = new PointF[4];
            srcs[0] = new PointF(75, 149);
            srcs[1] = new PointF(130, 149);
            srcs[2] = new PointF(0, 288);
            srcs[3] = new PointF(112, 288);


            PointF[] dsts = new PointF[4];
            dsts[0] = new PointF(0, 149);
            dsts[1] = new PointF(112, 149);
            dsts[2] = new PointF(0, 288);
            dsts[3] = new PointF(112, 288);

            HomographyMatrix mywarpmat = CameraCalibration.GetPerspectiveTransform(srcs, dsts);

            Image<Bgr, byte> newImage = image.WarpPerspective(mywarpmat, Emgu.CV.CvEnum.INTER.CV_INTER_NN,
    Emgu.CV.CvEnum.WARP.CV_WARP_FILL_OUTLIERS, new Bgr(255, 255, 255));

            return newImage;
        }


        /// <summary>
        /// 进行纵向拉伸，取消近大远小。
        /// </summary>
        /// <param name="image"></param>
        private static Image<Bgr, byte> VerticalExpand(Image<Bgr, byte> image)
        {
            double a = -11.3149, b = 12.6756, c = 0;

            Image<Bgr, byte> newImage = new Image<Bgr, byte>(image.Width, (int)((a + b + c) * image.Height));

            int h = image.Height;
            for (int y = 0; y < newImage.Height; y++)
            {
                //Console.WriteLine(y);

                double oldY = h * (-b + Math.Sqrt(b * b - 4 * a * c + 4 * a * (y + 1) / h)) / 2 / a - 1;

                for (int x = 0; x < image.Width; x++)
                {
                    if (Math.Abs(oldY - (int)oldY) > double.Epsilon)
                    {
                        //oy一般是小数，获得oy的上下界。
                        int yl = (int)Math.Floor(oldY);
                        int yh = (int)Math.Ceiling(oldY);

                        var cl = image[yl < 0 ? 0 : yl, x];
                        var ch = image[yh, x];

                        double red = cl.Red * (oldY - yl) + ch.Red * (yh - oldY);
                        double green = cl.Green * (oldY - yl) + ch.Green * (yh - oldY);
                        double blue = cl.Blue * (oldY - yl) + ch.Blue * (yh - oldY);


                        newImage[y, x] = new Bgr(blue, green, red);
                    }
                    else
                        newImage[y, x] = image[(int)oldY, x];
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="headAngle">箭头头部两条线的夹角</param>
        /// <param name="rightHeadLineAngle"> </param>
        /// <param name="verticalLineAngle"> </param>
        /// <param name="leftHeadLineAngle"> </param>
        /// <returns></returns>
        static bool IsArrow(LineSegment2D[] lines, TolerantValue leftHeadLineAngle, TolerantValue rightHeadLineAngle, TolerantValue verticalLineAngle)
        {
            int drawWidth = lines.Max(l => System.Math.Max(l.P1.X, l.P2.X)) + 10;
            int drawHeight = lines.Max(l => System.Math.Max(l.P1.Y, l.P2.Y)) + 10;

            Line leftHeadLine = null;
            Line rightHeadLine = null;

            List<LineSegment2D> lineList = new List<LineSegment2D>(lines);
            for (int i = 0; i < lineList.Count; i++)
            {
                double angleInRadian = System.Math.Atan(lineList[i].Direction.Y / lineList[i].Direction.X);
                double angleInDegree = 180 * angleInRadian / System.Math.PI;
                Debug.WriteLine(angleInDegree);
                if (leftHeadLine == null && leftHeadLineAngle.IsInRangle(angleInDegree))
                {
                    leftHeadLine = Line.FromTwoPoint(new PointD(lineList[i].P1), new PointD(lineList[i].P2));
                    lineList.RemoveAt(i--);
                }
                else if (rightHeadLine == null && rightHeadLineAngle.IsInRangle(angleInDegree))
                {
                    rightHeadLine = Line.FromTwoPoint(new PointD(lineList[i].P1), new PointD(lineList[i].P2));
                    lineList.RemoveAt(i--);
                }
            }

            if (leftHeadLine == null || rightHeadLine == null)
                return false;

            PointD headIntersection = LineCalculation.GetIntersection(leftHeadLine, rightHeadLine);

            Image<Bgr, byte> tmpImage = new Image<Bgr, byte>(drawWidth, drawHeight);
            tmpImage.Draw(new LineSegment2DF(leftHeadLine.EndPoint1.ConvertToFloat(), leftHeadLine.EndPoint2.ConvertToFloat()), new Bgr(0, 255, 0), 1);
            tmpImage.Draw(new LineSegment2DF(rightHeadLine.EndPoint1.ConvertToFloat(), rightHeadLine.EndPoint2.ConvertToFloat()), new Bgr(0, 255, 0), 1);
            tmpImage.Draw(new Ellipse(headIntersection.ConvertToFloat(), new SizeF(1, 1), 0), new Bgr(0, 0, 255), 2);


            List<Line> bodyLines = new List<Line>();

            for (int i = 0; i < lineList.Count; i++)
            {
                Line testingLine = Line.FromTwoPoint(new PointD(lineList[i].P1), new PointD(lineList[i].P2));
                if (verticalLineAngle.IsInRangle(testingLine.Angle))
                {
                    double d = LineCalculation.GetDistanceBetweenPointAndLine(headIntersection, testingLine);

                    if (d < 8)
                    {
                        bodyLines.Add(testingLine);
                        lineList.RemoveAt(i--);
                    }
                }
            }

            //已经限制了线的方向（verticalLineAngle）和位置（headIntersection），
            //找到的线多一点也没关系

            if (bodyLines.Count == 0)
                return false;

            foreach (var line in bodyLines)
            {
                tmpImage.Draw(new LineSegment2DF(line.EndPoint1.ConvertToFloat(), line.EndPoint2.ConvertToFloat()), new Bgr(0, 255, 0), 1);
            }



            for (int i = 0; i < lineList.Count; i++)
            {
                Line testingLine = Line.FromTwoPoint(new PointD(lineList[i].P1), new PointD(lineList[i].P2));

                double angle = System.Math.Abs(testingLine.Angle - bodyLines[0].Angle);

                if (angle > 80 && angle < 100)
                {
                    //tmpImage.Draw(new LineSegment2DF(testingLine.EndPoint1.ConvertToFloat(), testingLine.EndPoint2.ConvertToFloat()), new Bgr(0, 255, 0), 1);
                    //CvInvoke.cvShowImage("IsArrow", tmpImage);
                    return true;
                }

            }


            return false;
        }

    }


    public enum TrafficDirection
    {
        GoUp,
        GoDown
    }
}

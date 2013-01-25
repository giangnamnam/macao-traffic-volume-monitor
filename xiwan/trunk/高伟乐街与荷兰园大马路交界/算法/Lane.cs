using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Gqqnbig.Mathematics.Geometry;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public class Lane
    {
        readonly Image<Gray, byte> mask;

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

            //var contours = mask.FindContours();
            //regionOfInterest = contours.BoundingRectangle;
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

            Image<Bgr, byte> newImage = image.WarpPerspective(mywarpmat, INTER.CV_INTER_NN,
                                                            WARP.CV_WARP_FILL_OUTLIERS, new Bgr(255, 255, 255));

            return newImage;
        }

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
}

using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Gqqnbig.Drawing;

namespace Gqqnbig.TrafficVolumeMonitor
{
    [Obsolete]
    public class PossibleCarGroup
    {
        private readonly Image<Bgr, byte> sourceImage;
        private readonly Image<Gray, byte> objectImage;

        /// <summary>
        /// 车道的宽度。汽车宽度不能超过车道宽度。
        /// </summary>
        private readonly int laneWidth;
        private readonly int maxCarLength;
        private readonly int minArea;
        private readonly int carNumber;
        /// <summary>
        /// 水平方向上有多少车
        /// </summary>
        private int horizontalNumber;
        /// <summary>
        /// 竖直方向上有多少车
        /// </summary>
        private int verticalNumber;

        public PossibleCarGroup(Image<Bgr, byte> sourceImage, Image<Gray, byte> objectImage, Contour<Point> contour, int laneWidth, int maxCarLength, int minArea)
        {
            this.sourceImage = sourceImage;
            this.objectImage = objectImage;
            this.laneWidth = laneWidth;
            this.maxCarLength = maxCarLength;
            this.minArea = minArea;
            Contour = contour;

            carNumber = GetCarNumber();
        }


        public Contour<Point> Contour { get; private set; }

        public Rectangle BoundingRectangle
        { get { return Contour.BoundingRectangle; } }

        public int CarNumber
        {
            get { return carNumber; }
        }

        private int GetCarNumber()
        {
            horizontalNumber = (int)Math.Ceiling((double)BoundingRectangle.Width / laneWidth);
            verticalNumber = (int)Math.Ceiling(BoundingRectangle.Height / (double)maxCarLength);

            if (Contour.Area < minArea)
                return 0;


            return horizontalNumber * verticalNumber;


            //double cars = Contour.Area / maxArea;
            //if (cars <= 1)
            //    return 1;
            //else
            //    return (int)Math.Ceiling(cars);
        }

        public Car[] GetCars()
        {
            if (CarNumber == 0)
                return new Car[0];


            if (CarNumber >= 1)
            {
                try
                {
                    return new[] { Car.CreateCar(BoundingRectangle //new DRectangle(BoundingRectangle.X-1,BoundingRectangle.Y-1,BoundingRectangle.Width+2,BoundingRectangle.Height+2)
                    , sourceImage,objectImage) };
                }
                catch (NotProperCarException)
                {
                    return new Car[0];
                }
            }
            else
            {
                Rectangle[] rectangles = BoundingRectangle.DivideEqually(horizontalNumber, verticalNumber);
                Car[] cars = new Car[rectangles.Length];
                for (int i = 0; i < cars.Length; i++)
                {
                    try
                    {
                        cars[i] = Car.CreateCar(rectangles[i], sourceImage, objectImage);
                    }
                    catch (NotProperCarException) { }
                }
                return cars;
            }
        }

        ///// <summary>
        ///// 重新生成objectImage，使得contour可以放大，而且不会跟其他的contour合并。
        ///// </summary>
        ///// <param name="contour"></param>
        ///// <param name="width"></param>
        ///// <param name="height"></param>
        ///// <returns></returns>
        //private static Image<Gray, byte> CreateObjectImage(Contour<Point> contour, int width, int height)
        //{
        //    Image<Gray, byte> image = new Image<Gray, byte>(width, height);
        //    image.Draw(contour, new Gray(255), -1);
        //    //image = image.Dilate(1);
        //    return image;
        //}

    }
}

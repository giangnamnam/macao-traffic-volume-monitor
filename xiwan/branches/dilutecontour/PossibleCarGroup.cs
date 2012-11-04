using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Gqqnbig.Drawing;

namespace Gqqnbig.TrafficVolumeCalculator
{
    class PossibleCarGroup
    {
        private readonly Image<Bgr, byte> sourceImage;
        private readonly Image<Gray, byte> objectImage;

        /// <summary>
        /// 车道的宽度。汽车宽度不能超过车道宽度。
        /// </summary>
        private readonly int laneWidth;
        private readonly int maxCarLength;
        private readonly int minArea;
        private readonly int maxArea;
        private readonly int carNumber;
        /// <summary>
        /// 水平方向上有多少车
        /// </summary>
        private int horizontalNumber;
        /// <summary>
        /// 竖直方向上有多少车
        /// </summary>
        private int verticalNumber;

        public PossibleCarGroup(Image<Bgr, byte> sourceImage, Image<Gray, byte> objectImage, Contour<Point> contour, int laneWidth, int maxCarLength, int minArea, int maxArea)
        {
            this.sourceImage = sourceImage;
            this.objectImage = objectImage;
            this.laneWidth = laneWidth;
            this.maxCarLength = maxCarLength;
            this.minArea = minArea;
            this.maxArea = maxArea;
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
            else if (CarNumber == 1)
                return new[] { Car.CreateCar(BoundingRectangle //new DRectangle(BoundingRectangle.X-1,BoundingRectangle.Y-1,BoundingRectangle.Width+2,BoundingRectangle.Height+2)
                    , sourceImage,objectImage) };
            else
            {
                Rectangle[] rectangles = BoundingRectangle.DivideEqually(horizontalNumber, verticalNumber);
                Car[] cars = new Car[rectangles.Length];
                for (int i = 0; i < cars.Length; i++)
                {
                    //rectangles[i].Inflate(2, 2);
                    cars[i] = Car.CreateCar(rectangles[i], sourceImage,objectImage);
                }
                return cars;
            }
        }



    }
}

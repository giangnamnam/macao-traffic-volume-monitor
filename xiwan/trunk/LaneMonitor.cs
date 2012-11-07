using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Gqqnbig.Statistics;

namespace Gqqnbig.TrafficVolumeCalculator
{
    class LaneMonitor
    {
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


        public LaneMonitor(TrafficDirection trafficDirection, Lane lane)
        {
            Lane = lane;
            TrafficDirection = trafficDirection;
        }

        public TrafficDirection TrafficDirection { get; private set; }

        public Lane Lane { get; private set; }
        
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

        public CarMove GetCarMove(CarMatch[] matches, Car[] cars1, Car[] cars2)
        {
            double averageMove;
            if (matches.Length > 0)
                averageMove = matches.Average(m => m.Car1.CarRectangle.Top - m.Car2.CarRectangle.Top);
            else
                averageMove = Lane.Height;

            int leaveInPic1 = 0;
            foreach (var car in cars1)
            {
                if (car.CarRectangle.Top < averageMove)//这辆车会在下一幅中离开
                    leaveInPic1++;
            }

            int enterInPic2 = 0;
            foreach (var car in cars2)
            {
                int bottom = Lane.Height - car.CarRectangle.Top;
                Contract.Assert(bottom >= 0);
                if (bottom < averageMove)//这辆在第二幅图中的车是新进来的，在第一幅图中没有。
                    enterInPic2++;
            }
            return new CarMove(leaveInPic1, enterInPic2, averageMove);
        }
    }

    class CarMove
    {
        public CarMove(int leaveFromPic1, int enterToPic2, double averageMove)
        {
            AverageMove = averageMove;
            EnterToPic2 = enterToPic2;
            LeaveFromPic1 = leaveFromPic1;
        }

        public int LeaveFromPic1{get; private set;}

        public int EnterToPic2{get; private set;}

        public double AverageMove{get; private set;}
    }
}

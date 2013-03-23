using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Gqqnbig.Statistics;
using Gqqnbig.TrafficVolumeMonitor.Modules;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public class LaneMonitor
    {
        #region static

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

        #endregion

        readonly CarMatchParameter carMatchParameter;

        readonly LinkedList<int> numbersOfNewCars = new LinkedList<int>();

        /// <summary>
        /// 获取在5秒内的平均车流量（冲过底线的车的数量/5）。
        /// </summary>
        public double VolumeIn5seconds { get; private set; }

        /// <summary>
        /// 获取在60秒内的平均车流量（冲过底线的车的数量/60)。
        /// </summary>
        public double VolumeIn60seconds { get; private set; }


        public LaneMonitor(TrafficDirection trafficDirection, ILane lane, CarMatchParameter carMatchParameter, int maxHistorySize)
        {
            Lane = lane;
            TrafficDirection = trafficDirection;


            this.carMatchParameter = carMatchParameter;
            MaxHistorySize = maxHistorySize;
            //carMatchParameter = new CarMatchParameter { SimilarityThreshold = 0.26 };
            //XmlSerializer serializer = new XmlSerializer(typeof(CarMatchParameter));

            //XmlTextReader xmlReader = new XmlTextReader(carMatchParameter);
            //carMatchParameter = (CarMatchParameter)serializer.Deserialize(xmlReader);
            //xmlReader.Close();
        }

        public TrafficDirection TrafficDirection { get; private set; }

        public ILane Lane { get; private set; }

        /// <summary>
        /// 设置最多记录多少条历史。
        /// </summary>
        public int MaxHistorySize { get; private set; }

        public CarMatch[] FindCarMatch(Car[] cars1, Car[] cars2)
        {
            // ReSharper disable ConvertToConstant.Local
            double similarityThreshold = carMatchParameter.SimilarityThreshold;
            bool allowSamePosition = carMatchParameter.AllowSamePosition;
            // ReSharper restore ConvertToConstant.Local

            List<CarMatch> possibleMatches = new List<CarMatch>();
            if (TrafficDirection == TrafficDirection.GoUp)
            {
                foreach (var c2 in cars2)
                {
                    foreach (var c1 in cars1)
                    {
                        if (c1.CarRectangle.Top < c2.CarRectangle.Top)
                            break;
                        if (allowSamePosition == false && c1.CarRectangle.Top == c2.CarRectangle.Top)
                            break;

                        //假设车不改变车道。
                        if (Math.Abs(c1.CarRectangle.Left - c2.CarRectangle.Left) > 5)
                            continue;

                        CarMatch cm = new CarMatch(c1, c2);

                        if (cm.RS > similarityThreshold && cm.GS > similarityThreshold && cm.BS > similarityThreshold)
                        {
                            possibleMatches.Add(cm);
                        }
                    }
                }
            }

            possibleMatches = FindOneToOneBestMatch(possibleMatches);
            CarMatch[] bestMatches;
            if (possibleMatches.Count > 2)
            {
                bestMatches = RemoveDeviation(possibleMatches).ToArray();

            }
            else
                bestMatches= possibleMatches.ToArray();

            foreach (var m in bestMatches)
            {
                m.Car2.Id = m.Car1.Id;
            }

            return bestMatches;
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

        public void AddHistory(CarMove carMove)
        {
            VolumeIn5seconds = carMove.LeaveFromPic1 / 5.0;

            if (numbersOfNewCars.Count == MaxHistorySize)
                numbersOfNewCars.RemoveFirst();
            numbersOfNewCars.AddLast(carMove.EnterToPic2);
            //VolumeIn60seconds = numbersOfNewCars.Sum() / 5.0 / numbersOfNewCars.Count;

        }

        /// <summary>
        /// 获取在最近一段时间内，通过的车辆的数目。
        /// </summary>
        /// <param name="interval">从几条历史中求和</param>
        /// <returns></returns>
        public int GetNewCarSum(int interval)
        {
            if (interval < numbersOfNewCars.Count)
                throw new ArgumentException(string.Format("目前只有{0}条记录，因此不能访问{1}条记录。", numbersOfNewCars.Count, interval));

            var node = numbersOfNewCars.Last;
            int sum = 0;
            for (int i = 0; i < interval; i++)
            {
                sum += node.Value;
                node = node.Previous;
            }
            return sum;
        }
    }

    public class CarMove
    {
        public CarMove(int leaveFromPic1, int enterToPic2, double averageMove)
        {
            Contract.Requires(averageMove >= 0, "车辆平均移动距离不能是负值。");

            AverageMove = averageMove;
            EnterToPic2 = enterToPic2;
            LeaveFromPic1 = leaveFromPic1;
        }

        public int LeaveFromPic1 { get; private set; }

        public int EnterToPic2 { get; private set; }

        public double AverageMove { get; private set; }
    }
}

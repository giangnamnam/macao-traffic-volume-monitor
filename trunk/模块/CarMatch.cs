using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace Gqqnbig.TrafficVolumeMonitor
{
    /// <summary>
    /// 表示两辆车相似性的比较。
    /// </summary>
    public class CarMatch
    {
        public CarMatch(Car car1, Car car2)
        {
            Car2 = car2;
            Car1 = car1;

            RS = CvInvoke.cvCompareHist(car1.HistR, car2.HistR, HISTOGRAM_COMP_METHOD.CV_COMP_CORREL);
            GS = CvInvoke.cvCompareHist(car1.HistG, car2.HistG, HISTOGRAM_COMP_METHOD.CV_COMP_CORREL);
            BS = CvInvoke.cvCompareHist(car1.HistB, car2.HistB, HISTOGRAM_COMP_METHOD.CV_COMP_CORREL);
            HueS = CvInvoke.cvCompareHist(car1.HistHue, car2.HistHue, HISTOGRAM_COMP_METHOD.CV_COMP_CORREL);
        }

        public Car Car1 { get; private set; }

        public Car Car2 { get; private set; }

        public double RS { get; private set; }

        public double GS { get; private set; }

        public double BS { get; private set; }

        public double HueS { get; private set; }

        public bool IsMoreSimilar(CarMatch match)
        {
            throw new NotImplementedException();
        }

    }

    /// <summary>
    /// 一个比较器，用于比较a与b的相似度是否比c与d更高。
    /// </summary>
    public class CarMatchComparer : IComparer<CarMatch>
    {
        public int Compare(CarMatch x, CarMatch y)
        {
            double diff = (y.RS + y.GS + y.BS + y.HueS) - (x.RS + x.GS + x.BS + x.HueS);
            if (diff < 0)
                return -1;
            else if (diff > 0)
                return 1;
            else
                return 0;
        }
    }
}
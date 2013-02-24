using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gqqnbig.TrafficVolumeMonitor
{
    /// <summary>
    /// 可容差的值
    /// </summary>
    struct TolerantValue
    {
        /// <summary>
        /// 标准值
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 容差度，即最大值为Value+Tolerance，最小值为Value-Tolerance。
        /// </summary>
        public double Tolerance { get; set; }

        /// <summary>
        /// 小于最大值
        /// </summary>
        public double Maximun
        {
            get
            {
                return Value + Tolerance;
            }
        }

        /// <summary>
        /// 大于最小值
        /// </summary>
        public double Minimum
        {
            get
            {
                return Value - Tolerance;
            }
        }

        public bool IsInRangle(double n)
        {
            return n > Minimum && n < Maximun;
        }
    }

}

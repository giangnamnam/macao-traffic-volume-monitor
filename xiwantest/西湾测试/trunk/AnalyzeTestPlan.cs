using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Gqqnbig.TrafficVolumeCalculator.Testing
{
    /// <summary>
    /// 对Analyze方法的测试计划。
    /// </summary>
    public class AnalyzeTestPlan
    {
        //public AnalyzeTestExpected AnalyzeTestExpected { get; set; }

        //public AnalyzeTestAcutal AnalyzeTestAcutal { get; set; }

        /// <summary>
        /// 获取或设置样本文件的路径模式。用{0}标记序号。
        /// </summary>
        public string SampleFilePathPattern { get; set; }

        /// <summary>
        /// 获取或设置样本文件的数量。
        /// </summary>
        public int SampleLength { get; set; }

        /// <summary>
        /// 获取或设置期望数据的文件路径。该文件必须可被反序列化为AnalyzeTestExpected。
        /// </summary>
        public string ExpectedDataPath { get; set; }

        /// <summary>
        /// 获取或设置测试结果的放置路径。
        /// </summary>
        public string ActualPath { get; set; }
    }
}

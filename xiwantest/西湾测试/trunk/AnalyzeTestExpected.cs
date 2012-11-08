using System.Xml.Serialization;

namespace Gqqnbig.TrafficVolumeMonitor.Testing
{
    /// <summary>
    /// 对Analyze方法进行测试，希望得到的数据。
    /// </summary>
    public class AnalyzeTestExpected
    {
        public AnalyzeOutput[] AnalyzeOutputs { get; set; }
    }

    /// <summary>
    /// 对Analyze方法进行测试，实际得到的数据。
    /// </summary>
    class AnalyzeTestAcutal
    {
        public AnalyzeOutput[] AnalyzeOutputs { get; set; }

        public string Comment { get; set; }
    }

    public class AnalyzeOutput
    {
        [XmlAttribute]
        public int Id { get; set; }
        [XmlAttribute]
        public int CarNumber { get; set; }
    }
}

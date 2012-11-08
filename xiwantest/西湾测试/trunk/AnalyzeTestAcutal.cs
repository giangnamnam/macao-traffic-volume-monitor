namespace Gqqnbig.TrafficVolumeMonitor.Testing
{
    /// <summary>
    /// 对Analyze方法进行测试，实际得到的数据。
    /// </summary>
    public class AnalyzeTestAcutal
    {
        public string Comment { get; set; }
        
        public AnalyzeOutput[] AnalyzeOutputs { get; set; }

        public int ExpectedSum { get; set; }

        public int ActualSum { get; set; }

    }
}
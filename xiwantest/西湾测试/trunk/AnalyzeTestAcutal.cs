namespace Gqqnbig.TrafficVolumeMonitor.Testing
{
    /// <summary>
    /// ��Analyze�������в��ԣ�ʵ�ʵõ������ݡ�
    /// </summary>
    public class AnalyzeTestAcutal
    {
        public string Comment { get; set; }
        
        public AnalyzeOutput[] AnalyzeOutputs { get; set; }

        public int ExpectedSum { get; set; }

        public int ActualSum { get; set; }

    }
}
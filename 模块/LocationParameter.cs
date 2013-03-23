using System;

namespace Gqqnbig.TrafficVolumeMonitor.Modules
{
    [Serializable]
    public class LocationParameter
    {
        public string SourcePath { get; set; }

        public string MaskFilePath { get; set; }

        public CarMatchParameter CarMatchParameter { get; set; }

        public int BufferImagesCount { get; set; }

        public string AlgorithmName { get; set; }

        /// <summary>
        /// 获取或设置堵车时路面上的车的持续出现时长。单位秒。
        /// </summary>
        public int JamTime { get; set; }
    }
}

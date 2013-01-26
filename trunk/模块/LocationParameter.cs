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
    }
}

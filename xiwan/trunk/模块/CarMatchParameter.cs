using System;

namespace Gqqnbig.TrafficVolumeMonitor.Modules
{
    [Serializable]
    public class CarMatchParameter
    {
        public double SimilarityThreshold { get; set; }

        public bool AllowSamePosition { get; set; }
    }
}

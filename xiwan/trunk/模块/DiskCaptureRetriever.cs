using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public class DiskCaptureRetriever : ICaptureRetriever
    {
        private readonly string filePathPattern;
        private int id;

        public DiskCaptureRetriever(string filePathPattern, int startId = 0)
        {
            this.filePathPattern = filePathPattern;
            id = startId;
        }

        public bool IsNextAvailable
        {
            get { return System.IO.File.Exists(string.Format(filePathPattern, id + 1)); }
        }

        public Image<Bgr, byte> GetCapture()
        {
            return new Image<Bgr, byte>(string.Format(filePathPattern, id++));
        }

        public int SuggestedInterval
        { get { return 0; } }
    }
}
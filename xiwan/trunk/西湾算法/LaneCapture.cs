using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    /// <summary>
    /// 一张道路的截图
    /// </summary>
    public class LaneCapture
    {
        public LaneCapture(Image<Bgr, byte> originalImage, Image<Bgr, byte> focusedImage, Image<Bgra, byte> backgroundImage, Image<Gray, byte> objectImage, Car[] cars)
        {
            Cars = cars;
            ObjectImage = objectImage;
            BackgroundImage = backgroundImage;
            FocusedImage = focusedImage;
            OriginalImage = originalImage;
        }

        public Image<Bgr, byte> OriginalImage { get; private set; }

        public Image<Bgr, byte> FocusedImage { get; private set; }

        public Image<Bgra, byte> BackgroundImage { get; private set; }

        public Image<Gray, byte> ObjectImage { get; private set; }

        public Car[] Cars { get; private set; }

    }
}

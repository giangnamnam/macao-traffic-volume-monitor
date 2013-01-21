using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Linq;

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

        public LaneCapture(IEnumerable<Image<Bgr, byte>> progrssImages, Car[] cars)
        {
            ProgrssImages = progrssImages.Select(i=>i.ToBitmap()).ToArray();
            Cars = cars;
        }

        public Image<Bgr, byte> OriginalImage { get; private set; }

        public Image<Bgr, byte> FocusedImage { get; private set; }

        public Image<Bgra, byte> BackgroundImage { get; private set; }

        public Image<Gray, byte> ObjectImage { get; private set; }

        public Car[] Cars { get; private set; }

        public Bitmap[] ProgrssImages { get; private set; }


    }
}

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor.Modules
{
    static class Utility
    {
        public static Image<Bgr,byte> AutoCrop(Image<Bgr,byte> image)
        {
            var gray = image.Convert<Gray, byte>();
            CvInvoke.cvThreshold(gray, gray, 0, 255, THRESH.CV_THRESH_BINARY);

            var contour= gray.FindContours();
            return image.Copy(contour.BoundingRectangle);
        }
    }
}

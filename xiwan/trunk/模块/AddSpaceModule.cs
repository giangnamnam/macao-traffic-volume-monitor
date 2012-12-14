using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor.Modules
{
    [Serializable]
    public class AddSpaceModule
    {
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }


        public Image<Bgr, byte> Run(Image<Bgr, byte> image)
        {
            var roi = new Rectangle(new Point(Left, Top), image.Size);
            var newImage = new Image<Bgr, byte>(image.Width + Left + Right, image.Height + Top + Bottom);
            newImage.ROI = roi;

            image.Copy(newImage, null);
            newImage.ROI = Rectangle.Empty;
            return newImage;
        }
    }
}

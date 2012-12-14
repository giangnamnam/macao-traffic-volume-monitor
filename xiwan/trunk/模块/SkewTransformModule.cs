using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor.Modules
{
    [Serializable]
    public class SkewTransformModule
    {
        public double Angle { get; set; }

        /// <summary>
        /// 切变
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public Image<Bgr, byte> Run(Image<Bgr, byte> image)
        {
            Matrix<double> mat = new Matrix<double>(2, 3);
            mat[0, 0] = 1;
            mat[0, 1] = -Math.Tan(Angle / 180 * Math.PI);
            mat[0, 2] = 0;
            mat[1, 0] = 0;
            mat[1, 1] = 1;
            mat[1, 2] = 0;

            var warpImage = image.WarpAffine(mat, INTER.CV_INTER_LINEAR, WARP.CV_WARP_DEFAULT, new Bgr(0, 0, 0));

            return Utility.AutoCrop(warpImage);
        }
    }
}

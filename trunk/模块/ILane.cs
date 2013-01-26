using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public interface ILane
    {
        /// <summary>
        /// 获取本车道在图片中的宽度
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 获取本车道在图片中的长度
        /// </summary>
        int Height { get; }

        LaneCapture Analyze(Image<Bgr, byte> orginialImage, ICollection<Image<Bgr, byte>> samples);
    }
}
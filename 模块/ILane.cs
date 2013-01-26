using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    public interface ILane
    {
        /// <summary>
        /// ��ȡ��������ͼƬ�еĿ��
        /// </summary>
        int Width { get; }

        /// <summary>
        /// ��ȡ��������ͼƬ�еĳ���
        /// </summary>
        int Height { get; }

        LaneCapture Analyze(Image<Bgr, byte> orginialImage, ICollection<Image<Bgr, byte>> samples);
    }
}
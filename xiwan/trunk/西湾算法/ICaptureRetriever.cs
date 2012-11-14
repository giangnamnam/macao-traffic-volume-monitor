using System.IO;
using System.Net;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeMonitor
{
    /// <summary>
    /// 提供从某种资源上获取图片的方法。
    /// </summary>
    public interface ICaptureRetriever
    {
        /// <summary>
        /// 读取当前图片，并准备读取下一张图片。
        /// </summary>
        /// <returns></returns>
        Image<Bgr, byte> GetCapture();
    }

    public class RealtimeCaptureRetriever : ICaptureRetriever
    {
        private readonly string url;
        private readonly WebClient client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        public RealtimeCaptureRetriever(string url)
        {
            this.url = url;
            client = new WebClient();
        }


        public Image<Bgr, byte> GetCapture()
        {
            byte[] data = client.DownloadData(url);

            using (MemoryStream stream = new MemoryStream(data.Length))
            {
                stream.Write(data, 0, data.Length);
                return new Image<Bgr, byte>(new System.Drawing.Bitmap(stream));
            }
        }
    }

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
    }
}

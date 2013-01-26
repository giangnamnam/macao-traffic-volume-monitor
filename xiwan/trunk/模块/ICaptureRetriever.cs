using System;
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

        /// <summary>
        /// 获取建议的读取间隔（毫秒）。如果为0表示随意读取。
        /// </summary>
        int SuggestedInterval { get; }
    }

    public class RealtimeCaptureRetriever : ICaptureRetriever
    {
        private readonly string url;
        private readonly WebClient client;
        DateTime lastReadTime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="interval">每次读取图片至少间隔多少毫秒</param>
        public RealtimeCaptureRetriever(string url, int interval)
        {
            this.url = url;
            SuggestedInterval = interval;
            client = new WebClient();

            lastReadTime = DateTime.Now.AddMilliseconds(-interval);
        }


        public int SuggestedInterval { get; private set; }

#if DEBUG
        int i = 0;
#endif
        public Image<Bgr, byte> GetCapture()
        {
            var timeSpan = DateTime.Now - lastReadTime;
            double diff = SuggestedInterval - timeSpan.TotalMilliseconds;
            if (diff > 0)
            {
                System.Threading.Thread.Sleep((int)diff);
            }

            lock (client)
            {
                lastReadTime = DateTime.Now;
                byte[] data = client.DownloadData(url);

#if DEBUG
                using (FileStream fs = new FileStream("B:\\新图像\\" + i++ + ".jpg", FileMode.CreateNew))
                {
                    fs.Write(data, 0, data.Length);
                }
#endif

                using (MemoryStream stream = new MemoryStream(data.Length))
                {
                    stream.Write(data, 0, data.Length);
                    return new Image<Bgr, byte>(new System.Drawing.Bitmap(stream));
                }
            }
        }

    }
}

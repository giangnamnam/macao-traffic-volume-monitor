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

        readonly string startTime;


        private byte[] lastImageData;
        int errorCount = 0;


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

            startTime = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

            Directory.CreateDirectory("D:\\newimages\\"+startTime+"\\");
        }


        public int SuggestedInterval { get; private set; }

#if DEBUG
        int i = 0;
#endif
        public Image<Bgr, byte> GetCapture()
        {
            if (errorCount > 10)
                throw new WebException("获取图像多次失败。");
            
            var timeSpan = DateTime.Now - lastReadTime;
            double diff = SuggestedInterval - timeSpan.TotalMilliseconds;
            if (diff > 0)
            {
                System.Threading.Thread.Sleep((int)diff);
            }

            lock (client)
            {
                lastReadTime = DateTime.Now;

                try
                {
                    lastImageData = client.DownloadData(url);

                    if (errorCount > 0)
                        errorCount--;
                }
                catch (WebException)
                {
                    errorCount += 2;
                }

                if (lastImageData == null)
                    throw new WebException("无法从指定的URL中获取图像，或获取的图像大小为0。");

#if DEBUG
                using (FileStream fs = new FileStream("D:\\newimages\\" + startTime + "\\" + i++ + ".jpg", FileMode.CreateNew))
                {
                    fs.Write(lastImageData, 0, lastImageData.Length);
                }
#endif

                using (MemoryStream stream = new MemoryStream(lastImageData.Length))
                {
                    stream.Write(lastImageData, 0, lastImageData.Length);
                    return new Image<Bgr, byte>(new System.Drawing.Bitmap(stream));
                }
            }
        }

    }
}

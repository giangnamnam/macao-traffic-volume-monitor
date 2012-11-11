using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Timers;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Gqqnbig.TrafficVolumeMonitor
{
    interface ICaptureRetriever
    {
        bool CanSeek { get; }

        /// <summary>
        /// 读取当前图片，并准备读取下一张图片。
        /// </summary>
        /// <returns></returns>
        Image<Bgr, byte> GetCapture();

        /// <summary>
        /// 获取相对于当前位置的图片。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Image<Bgr, byte> GetRelativeCapture(int value);

        Image<Bgr, byte> GetCapture(int id);

        event EventHandler<CaptureRetrieverEventArgs> Downloaded;

        void Start();

        /// <summary>
        /// 确保当前位置之后有n张图片。会自动调用Start()。
        /// </summary>
        /// <param name="n"></param>
        void EnsureNext(int n);
    }

    public class RealtimeCaptureRetriever : ICaptureRetriever
    {

        int fileNumber;
        private readonly string url;
        private readonly bool saveToLocal;
        private readonly WebClient client;

        readonly LinkedList<Image<Bgr, byte>> cachedImages;
        readonly int cacheLength;
        int current;

        Image<Bgr, byte> availableImage;
        private readonly Timer timer;
        private Semaphore semaphore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interval">图片产生间隔（ms）</param>
        /// <param name="url"></param>
        /// <param name="saveToLocal"></param>
        public RealtimeCaptureRetriever(int interval, string url, bool saveToLocal = true)
        {
            int aheaadNumber = 2;
            int behindNumber = 3;
            cacheLength = 1 + aheaadNumber + behindNumber;


            cachedImages = new LinkedList<Image<Bgr, byte>>();

            fileNumber = 0;
            this.url = url;
            this.saveToLocal = saveToLocal;
            if (saveToLocal)
                Path.GetTempPath();

            client = new WebClient();

            timer = new Timer(interval);
            timer.Elapsed += DownloadImage;
        }


        public bool CanSeek { get { return false; } }

        public Image<Bgr, byte> GetCapture()
        {
            Contract.Assert(current <= 2);
            return cachedImages.ElementAt(current++);
        }

        public Image<Bgr, byte> GetRelativeCapture(int value)
        {
            int index = value + current;

            if (index < 0 || index >= cachedImages.Count)
                throw new ArgumentOutOfRangeException();

            return cachedImages.ElementAt(index);
        }

        public Image<Bgr, byte> GetCapture(int id)
        {
            throw new NotSupportedException();
        }

        public event EventHandler<CaptureRetrieverEventArgs> Downloaded;

        public void Start()
        {
            timer.Start();
        }

        public void EnsureNext(int n)
        {
            semaphore = new Semaphore(0, n);
            Start();
            for (int i = 0; i < n; i++)
            {
                semaphore.WaitOne();
            }
            semaphore.Dispose();
            semaphore = null;
        }


        private void DownloadImage(object sender, ElapsedEventArgs e)
        {
            try
            {
                byte[] data;
                lock (client)
                {
                    data = client.DownloadData(url);
                }

                using (MemoryStream stream = new MemoryStream(data.Length))
                {
                    stream.Write(data, 0, data.Length);
                    availableImage = new Image<Bgr, byte>(new System.Drawing.Bitmap(stream));

                    if (cachedImages.Count >= cacheLength)
                    {
                        cachedImages.First.Value.Dispose();
                        cachedImages.RemoveFirst();
                        current--;
                    }
                    cachedImages.AddLast(availableImage);

                }

                if (saveToLocal)
                {
                    string directoryPath = Path.Combine(Path.GetTempPath(), "macao traffic monitor");
                    Directory.CreateDirectory(directoryPath);
                    using (FileStream stream = new FileStream(Path.Combine(directoryPath, fileNumber + ".jpg"), FileMode.OpenOrCreate))
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                //System.Diagnostics.Debug.WriteLine("图片已获得");
                if (Downloaded != null)
                    Downloaded(this, new CaptureRetrieverEventArgs { InCacheCount = cachedImages.Count });

            }
            //catch (Exception ex)
            //{
            //}
            finally
            {
                fileNumber++;
                if (semaphore != null)
                    semaphore.Release();
            }
        }
    }

    public class DiskCaptureRetriever //: ICaptureRetriever
    {
        private readonly string filePathPattern;
        private int id = -1;

        public DiskCaptureRetriever(string filePathPattern)
        {
            this.filePathPattern = filePathPattern;
        }

        public bool CanSeek { get { return true; } }

        public bool IsNextAvailable
        {
            get { return System.IO.File.Exists(string.Format(filePathPattern, id + 1)); }
        }

        public Image<Bgr, byte> GetNextCapture()
        {
            return new Image<Bgr, byte>(string.Format(filePathPattern, ++id));
        }

        public Image<Bgr, byte> GetCapture(int id)
        {
            return new Image<Bgr, byte>(string.Format(filePathPattern, id));
        }

        public event EventHandler NextAvailable;
    }

    public class CaptureRetrieverEventArgs : EventArgs
    {
        public int InCacheCount { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeCalculator
{
    interface ICaptureRetriever
    {
        bool CanSeek { get; }

        bool IsNextAvailable { get; }

        Image<Bgr, byte> GetNextCapture();

        //Image<Bgr, byte> GetCapture(int id);


    }

    public class DiskCaptureRetriever : ICaptureRetriever
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Gqqnbig.TrafficVolumeCalculator
{
    class Lane
    {
        public Image<Gray, byte> Mask { get; private set; }

        public TrafficDirection TrafficDirection { get; private set; }

        public double RgbSimilarityThreshold = 0.4031;

        public Lane(string maskFileName, TrafficDirection trafficDirection, double similarityThreshold)
        {
            Mask = new Image<Gray, byte>(maskFileName);
            TrafficDirection = trafficDirection;
            RgbSimilarityThreshold = similarityThreshold;
        }


    }


    enum TrafficDirection
    {
        GoUp,
        GoDown
    }
}

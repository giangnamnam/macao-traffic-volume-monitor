using System;
using System.Globalization;
using System.Windows.Data;
using System.Linq;
using Emgu.CV;

namespace Gqqnbig.TrafficVolumeCalculator
{
    public class MultiplicationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
                              object parameter, CultureInfo culture)
        {
            var result = System.Convert.ToDouble(values[0]) * System.Convert.ToDouble(values[1]);
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
                                    object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    public class HistogramConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float[] hist = (float[])value;
            double maxHeight = System.Convert.ToDouble(parameter);

            double[] enlargedHist = new double[hist.Length];
            float maxValue = hist.Max();
            double factor = maxHeight / maxValue;

            for (int i = 0; i < hist.Length; i++)
            {
                enlargedHist[i] = hist[i] * factor;
            }

            return enlargedHist;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DenseHistogramConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DenseHistogram hist = (DenseHistogram) value;
            double maxHeight = System.Convert.ToDouble(parameter);

            float[] arr = new float[hist.BinDimension[0].Size];
            hist.MatND.ManagedArray.CopyTo(arr, 0);
            float maxValue = arr.Max();
            double factor = maxHeight / maxValue;
            double[] enlargedHist = new double[arr.Length];

            for (int i = 0; i < arr.Length; i++)
            {
                enlargedHist[i] = arr[i] * factor;
            }

            return enlargedHist;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class MaxValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float[] array = (float[])value;

            return array.Max();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
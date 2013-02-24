using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Linq;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Gqqnbig.Windows.Media;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    //public class MultiplicationConverter : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType,
    //                          object parameter, CultureInfo culture)
    //    {
    //        var result = System.Convert.ToDouble(values[0]) * System.Convert.ToDouble(values[1]);
    //        return result;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes,
    //                                object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotSupportedException("Cannot convert back");
    //    }
    //}

    public class DenseHistogramConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DenseHistogram hist = (DenseHistogram)value;
            double maxHeight = System.Convert.ToDouble(parameter);

            float[] arr = new float[hist.BinDimension[0].Size];
            hist.MatND.ManagedArray.CopyTo(arr, 0);
            float maxValue = arr.Max();
            double[] enlargedHist = new double[arr.Length];
            if (maxValue > 0)
            {
                double factor = maxHeight / maxValue;
                for (int i = 0; i < arr.Length; i++)
                {
                    enlargedHist[i] = arr[i] * factor;
                }
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

    [ValueConversion(typeof(Bitmap), typeof(BitmapImage))]
    public class BitmapConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Bitmap)value).ToBitmapImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
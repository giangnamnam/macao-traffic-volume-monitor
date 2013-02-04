using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    [ValueConversion(typeof(CultureInfo), typeof(ImageSource))]
    class StateFlagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                CultureInfo c = (CultureInfo)value;

                return new BitmapImage(new Uri("/StateFlags/" + c.Name + ".png", UriKind.Relative));
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

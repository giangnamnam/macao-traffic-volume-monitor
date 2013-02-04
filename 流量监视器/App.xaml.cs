using System;
using System.Globalization;
using System.Windows;

namespace Gqqnbig.TrafficVolumeMonitor.UI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        internal static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("zh-CN");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Localize(DefaultCulture);
        }

        internal static void Localize(CultureInfo culture)
        {
            var mergedDictionaries = Current.Resources.MergedDictionaries;
            for (int i = 0; i < mergedDictionaries.Count; i++)
            {
                Uri uri = mergedDictionaries[i].Source;
                string uriString = uri.ToString();

                if (uriString.StartsWith("Lang/" + DefaultCulture.Name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (uriString.StartsWith("Lang/", StringComparison.OrdinalIgnoreCase))
                    mergedDictionaries.RemoveAt(i--);
            }


            if (culture.Equals(DefaultCulture) == false)
            {
                ResourceDictionary langRd = FindLocalizationResource(culture);
                if (langRd != null)
                    mergedDictionaries.Add(langRd);
            }
        }

        /// <summary>
        /// 寻找存在的本地化文件。
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns>如果找不到就返回null。</returns>
        private static ResourceDictionary FindLocalizationResource(CultureInfo cultureInfo)
        {
            try
            {
                ResourceDictionary rd = new ResourceDictionary();
                rd.Source = new Uri("Lang/" + cultureInfo.Name + ".xaml", UriKind.Relative);
                return rd;
                //return LoadComponent(new Uri(@"Lang\" + cultureInfo.Name + ".xaml", UriKind.Relative)) as ResourceDictionary;
            }
            catch (Exception ex)
            {
                if (cultureInfo.Parent.IsNeutralCulture == false)//没有找到zh-CN的话，就会找zh-CHS。
                    return FindLocalizationResource(cultureInfo.Parent);
                else
                    return null;
            }
        }


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);

            System.Diagnostics.Debug.WriteLine(ex.Message + "\r\n" + ex.StackTrace);

            Environment.Exit(1);
        }
    }
}

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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            LoadLanguage();
        }

        private void LoadLanguage()
        {
            CultureInfo currentCultureInfo = CultureInfo.GetCultureInfo("en-US");

            ResourceDictionary langRd = null;

            try
            {

                langRd = FindLocalizationResource(currentCultureInfo);
            }
            catch
            {
            }

            if (langRd != null)
            {
                //if (this.Resources.MergedDictionaries.Count > 0)
                //{
                //    this.Resources.MergedDictionaries.Clear();
                //}
                this.Resources.MergedDictionaries.Add(langRd);
            }


        }

        /// <summary>
        /// 寻找存在的本地化文件。
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        private static ResourceDictionary FindLocalizationResource(CultureInfo cultureInfo)
        {
            ResourceDictionary dic= Application.LoadComponent(new Uri(@"Lang\" + cultureInfo.Name + ".xaml", UriKind.Relative)) as ResourceDictionary;

            if (dic == null && cultureInfo.Parent.IsNeutralCulture == false)//没有找到zh-CN的话，就会找zh-CHS。
                return FindLocalizationResource(cultureInfo.Parent);
            else
                return dic;
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

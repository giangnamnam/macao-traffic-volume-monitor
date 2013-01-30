using System;
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

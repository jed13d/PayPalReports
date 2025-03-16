using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace PayPalReports
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += CrashHandler;

            base.OnStartup(e);
        }

        protected void CrashHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the error.

            Debug.WriteLine($"Unhandled exception: {e.Exception}");

            //CrashInfoWindow crashWindow = new CrashInfoWindow(e.Exception);

            e.Handled = true;
        }
    }

}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PayPalReports.Contexts;
using PayPalReports.CustomEvents;
using PayPalReports.Pages;
using PayPalReports.Services;
using PayPalReports.ViewModels;
using Serilog;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace PayPalReports
{
    public partial class App : Application
    {
        private static Hashtable _commandLineArgs = [];

        private readonly IHost HOST;
        private readonly IServiceProvider SERVICE_PROVIDER;
        private readonly string LOG_FILE_PATH = "log-file.txt";

        // debug mode append "/Debug:1"
        private readonly string CLA_DEBUG_MODE_FLAG = "Debug";
        private readonly string CLA_TRUE = "1";

        public App()
        {
            // Parse command line arguments, for now debug mode or not
            if (_commandLineArgs.Count > 0 && _commandLineArgs.ContainsKey(CLA_DEBUG_MODE_FLAG) && _commandLineArgs[CLA_DEBUG_MODE_FLAG]!.Equals(CLA_TRUE))
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(LOG_FILE_PATH, rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(LOG_FILE_PATH, rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            }

            // Setup host
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();
            hostBuilder.ConfigureLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            hostBuilder.ConfigureServices(s => ConfigureServices(s));

            // initialize readonly variables 
            HOST = hostBuilder.Build();
            SERVICE_PROVIDER = HOST.Services.GetRequiredService<IServiceProvider>();
        }

        public void App_Startup(object sender, StartupEventArgs e)
        {
            // Don't bother if no command line args were passed
            // NOTE: e.Args is never null - if no command line args were passed, 
            //       the length of e.Args is 0.
            if (e.Args.Length == 0) return;

            // Parse command line args for args in the following format:
            //   /argname:argvalue /argname:argvalue /argname:argvalue ...
            //
            // Note: This sample uses regular expressions to parse the command line arguments.
            // For regular expressions, see:
            // http://msdn.microsoft.com/library/en-us/cpgenref/html/cpconRegularExpressionsLanguageElements.asp
            var pattern = @"(?<argname>/\w+):(?<argvalue>\w+)";
            foreach (var arg in e.Args)
            {
                var match = Regex.Match(arg, pattern);

                // If match not found, command line args are improperly formed.
                if (!match.Success)
                    throw new ArgumentException(
                        "The command line arguments are improperly formed. Use /argname:argvalue.");

                // Store command line arg and value
                _commandLineArgs[match.Groups["argname"].Value] = match.Groups["argvalue"].Value;
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await HOST.StopAsync();
            HOST.Dispose();

            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = SERVICE_PROVIDER.GetRequiredService<MainWindow>();
            MainWindow.Show();

            Application.Current.DispatcherUnhandledException += CrashHandler;

            base.OnStartup(e);
        }

        // customizing unhandled errors / crashes -- TODO
        protected void CrashHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the error.

            Debug.WriteLine($"Unhandled exception: {e.Exception}");

            //CrashInfoWindow crashWindow = new CrashInfoWindow(e.Exception);

            e.Handled = true;
        }

        // Setup IServiceCollection for Dependency Injection
        private void ConfigureServices(IServiceCollection services)
        {
            // custom event for updating status bar in main window
            services.AddSingleton<StatusEvent>();

            // ViewModels
            services.AddSingleton<MainViewModel>(s => new MainViewModel(s));
            services.AddSingleton<ConfigurationPageViewModel>(s => new ConfigurationPageViewModel(s));

            // contexts
            services.AddSingleton<FrameNavigationContext>();

            // services
            services.AddTransient<DataEncryptionService>(s => new DataEncryptionService(s));
            services.AddTransient<ExcelService>(s => new ExcelService(s));
            services.AddTransient<PayPalService>(s => new PayPalService(s));

            // pages
            services.AddTransient<ConfigurationPage>(s => new ConfigurationPage(s)
            {
                DataContext = s.GetRequiredService<ConfigurationPageViewModel>()
            });

            services.AddTransient<ReportsPage>(s => new ReportsPage(s));

            // Main Window
            services.AddSingleton<MainWindow>(s => new MainWindow(s)
            {
                DataContext = s.GetRequiredService<MainViewModel>()
            });

        }
    }

}

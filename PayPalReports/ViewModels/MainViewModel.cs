using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayPalReports.CustomEvents;
using PayPalReports.DataModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PayPalReports.ViewModels
{
    internal class MainViewModel : BaseViewModel, IStatusEventListener
    {
        public Page CurrentFramePage => FRAME_NAVIGATION_CONTEXT.CurrentPage;

        public ICommand? MenuItem_About_Click_Command { get; }  // TODO
        public ICommand? MenuItem_Help_Click_Command { get; }   // TODO

        private readonly ILogger<MainWindow> LOGGER;
        private readonly StatusEvent STATUS_EVENT;
        private readonly FrameNavigationContext FRAME_NAVIGATION_CONTEXT;

        private string _statusText = string.Empty;
        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<MainWindow>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
            FRAME_NAVIGATION_CONTEXT = serviceProvider.GetRequiredService<FrameNavigationContext>();

            STATUS_EVENT.RegisterListener(this);
        }

        // TODO, generate window with about information
        public void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            LOGGER.LogDebug("Sender: {@Sender} --- eventArgs: {@EventArgs}", sender, e);
        }

        // TODO, produce instructions, help file? README in repo?
        public void MenuItem_Help_Click(object sender, RoutedEventArgs e)
        {
            LOGGER.LogDebug("Sender: {@Sender} --- eventArgs: {@EventArgs}", sender, e);
        }

        /**
         * Event-Driven method for messaging the user through the UI
         * */
        public void UpdateStatusEvent(string message)
        {
            if (message != null)
            {
                StatusText = message;

            }

            LOGGER.LogInformation("Main Window status updated to: {@Message}", message);
        }
    }
}

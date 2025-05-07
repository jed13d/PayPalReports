using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayPalReports.CustomEvents;
using System.Windows.Controls;

namespace PayPalReports.Pages
{
    public partial class ConfigurationPage : Page
    {
        private readonly ILogger<ConfigurationPage>? LOGGER;
        private readonly StatusEvent? STATUS_EVENT;

        private ConfigurationPage()
        {
            InitializeComponent();
        }

        public ConfigurationPage(IServiceProvider serviceProvider) : this()
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<ConfigurationPage>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
        }

    }
}

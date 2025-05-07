using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayPalReports.CustomEvents;
using System.Windows.Controls;

namespace PayPalReports.Pages
{
    /// <summary>
    /// Interaction logic for ReportSetupPage.xaml
    /// </summary>
    public partial class ReportsPage : Page
    {
        private readonly ILogger<ReportsPage>? LOGGER;
        private readonly StatusEvent? STATUS_EVENT;

        private ReportsPage() => InitializeComponent();

        public ReportsPage(IServiceProvider serviceProvider) : this()
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<ReportsPage>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
        }

    }
}

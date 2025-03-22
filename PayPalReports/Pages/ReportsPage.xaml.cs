using PayPalReports.CustomEvents;
using PayPalReports.DataModels;
using PayPalReports.Services;
using System.Windows.Controls;

namespace PayPalReports.Pages
{
    /// <summary>
    /// Interaction logic for ReportSetupPage.xaml
    /// </summary>
    public partial class ReportSetupPage : Page
    {
        private PayPalService _payPalService = new();

        private readonly string END_OF_DAY_TIME = "23:59:59";
        private readonly string ISO_DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:sszzz";

        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }

        public ReportSetupPage()
        {
            InitializeComponent();
            ClearStatusText();
        }

        private void TestData_Click(object sender, EventArgs e)
        {
            // Disable button to prevent multiple submissions
            Submit_Button.IsEnabled = false;

            // Utilize DateTime object to convert UI form submission to ISO8601 Internet Date/Time Format
            StartDate = DateTime.Parse(dpStartDate.Text);

            string endDate = $"{dpEndDate.Text} {END_OF_DAY_TIME}";
            EndDate = DateTime.Parse(endDate);

            // Create context object for storing and passing data
            PayPalReportDetails payPalReportDetails = new PayPalReportDetails();
            payPalReportDetails.StartDate = $"{StartDate.ToString(ISO_DATE_TIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture)}";
            payPalReportDetails.EndDate = $"{EndDate.ToString(ISO_DATE_TIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture)}";

            // Begin PayPalService series of requests for data pull
            if (_payPalService.TryGetPayPalData(ref payPalReportDetails))
            {
                UpdateStatusText($"PayPalService reported success!");
            }

            // reenable button once complete
            Submit_Button.IsEnabled = true;
        }

        private void ClearStatusText()
        {
            UpdateStatusText($"");
        }

        /**
         * Method for messaging the user through the UI
         * (maybe pull this out and make event driven at bottom of window)
         * */
        private void UpdateStatusText(string message)
        {
            StatusEvent.Raise(message);
        }
    }
}

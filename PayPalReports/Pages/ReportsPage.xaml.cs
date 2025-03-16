using PayPalReports.Services;
using System.Diagnostics;
using System.Windows.Controls;

namespace PayPalReports.Pages
{
    /// <summary>
    /// Interaction logic for ReportSetupPage.xaml
    /// </summary>
    public partial class ReportSetupPage : Page
    {
        private PayPalService _payPalService = new();

        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }

        public ReportSetupPage()
        {
            InitializeComponent();
        }

        private void TestData_Click(object sender, EventArgs e)
        {
            Debug.WriteLine($"Start: {dpStartDate.Text}");

            StartDate = DateTime.Parse(dpStartDate.Text);

            Debug.WriteLine($"Start: {StartDate:O}");

            Debug.WriteLine($"End: {dpEndDate.Text}");

            string endDate = $"{dpEndDate.Text} 23:59:59";
            //string endDate = $"{dpEndDate.Text}T23:59:59.0000000Z";

            EndDate = DateTime.Parse(endDate);

            //EndDate = DateTime.Parse(dpEndDate.Text);

            Debug.WriteLine($"End: {EndDate:O}");
        }

        private async void Test(object sender, System.Windows.RoutedEventArgs e)
        {
            Debug.WriteLine($"Initializing PayPal Token Request test.");
            try
            {
                bool testPassed = await _payPalService.TestTokenRequest();
                if (testPassed)
                {
                    Debug.WriteLine($"Successfully received token with PayPal.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An exception occured while running token test. {ex}");
            }
        }
    }
}

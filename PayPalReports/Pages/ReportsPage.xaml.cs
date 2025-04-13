using Microsoft.Win32;
using PayPalReports.CustomEvents;
using PayPalReports.DataModels;
using PayPalReports.DataModels.PayPalAPI;
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
        private ExcelService _excelService = new();

        private readonly string END_OF_DAY_TIME = "23:59:59";

        private readonly string DIALOG_FILENAME = "Master-Ledger";
        private readonly string DIALOG_FILE_EXTENSION = ".xlsx";
        private readonly string FILTER = "Excel Book (.xlsx)|*.xlsx";

        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }

        public ReportSetupPage()
        {
            InitializeComponent();
            ClearStatusText();
        }

        private void Destination_Search_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.FileName = DIALOG_FILENAME;
            dialog.DefaultExt = DIALOG_FILE_EXTENSION;
            dialog.Filter = FILTER;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                DestinationPath.Text = dialog.FileName;
            }
        }

        private void TestData_Click(object sender, EventArgs e)
        {
            // Disable button to prevent multiple submissions
            Submit_Button.IsEnabled = false;

            // Utilize DateTime object to convert UI form submission to ISO8601 Internet Date/Time Format
            string endDate = $"{dpEndDate.Text} {END_OF_DAY_TIME}";

            // Create context object for storing and passing data
            PayPalReportDetails payPalReportDetails = new PayPalReportDetails();
            payPalReportDetails.StartDate = DateTime.Parse(dpStartDate.Text);
            payPalReportDetails.EndDate = DateTime.Parse(endDate);

            // Begin PayPalService series of requests for data pull
            if (_payPalService.TryGetPayPalData(ref payPalReportDetails))
            {
                if (GenerateReport(payPalReportDetails))
                {
                    UpdateStatusText($"Report generation is complete.");
                }
                else
                {
                    UpdateStatusText($"There has been an error generating your report.");
                }
            }
            else
            {
                UpdateStatusText($"There has been an error getting the data from PayPal.");
            }

            // reenable button once complete
            Submit_Button.IsEnabled = true;
        }

        private bool GenerateReport(PayPalReportDetails payPalReportDetails)
        {
            UpdateStatusText($"Generating report.");
            ExcelReportContext excelReportContext = new(payPalReportDetails, DestinationPath.Text);
            DebugOutputPayPalReportDetails(payPalReportDetails);
            return _excelService.GenerateReport(excelReportContext);
            //return true;
        }

        private void DebugOutputPayPalReportDetails(PayPalReportDetails payPalReportDetails)
        {
            Debug.WriteLine($"##### DEBUG OUTPUT DATA REPORT-DETAILS #####");

            Debug.WriteLine($"Start Date: {payPalReportDetails?.GetStartDateISO()}");
            Debug.WriteLine($"Balance: {payPalReportDetails?.PayPalStartBalanceResponse?.balances[0].total_balance.value}\n");

            Debug.WriteLine($"End Date: {payPalReportDetails?.GetEndDateISO()}");
            Debug.WriteLine($"Balance: {payPalReportDetails?.PayPalEndBalanceResponse?.balances[0].total_balance.value}\n");


            Debug.WriteLine($"Number of transactions: {payPalReportDetails?.PayPalTransactionResponse?.transaction_details.Length}");
            Debug.WriteLine($"Start date: {payPalReportDetails?.PayPalTransactionResponse?.start_date}");
            Debug.WriteLine($"End date: {payPalReportDetails?.PayPalTransactionResponse?.end_date}");
            Debug.WriteLine($"Total items: {payPalReportDetails?.PayPalTransactionResponse?.total_items}");
            Debug.WriteLine($"Total pages: {payPalReportDetails?.PayPalTransactionResponse?.total_pages}");

            Debug.WriteLine($"Account Number: {payPalReportDetails?.PayPalTransactionResponse?.account_number}");
            if (payPalReportDetails?.PayPalTransactionResponse?.transaction_details.Length > 0)
            {
                Debug.WriteLine($"Account Number: {payPalReportDetails?.PayPalTransactionResponse?.transaction_details[0]?.transaction_info.transaction_id}");
            }

            Debug.WriteLine($"##### DEBUG OUTPUT DATA REPORT-DETAILS #####");
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

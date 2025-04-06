using PayPalReports.DataModels.PayPalAPI;

namespace PayPalReports.DataModels
{
    internal class ExcelReportContext
    {
        public PayPalReportDetails PayPalReportDetails { get; set; }

        public string OutputPath { get; set; }

        public ExcelReportContext(PayPalReportDetails payPalReportDetails, string outputPath)
        {
            PayPalReportDetails = payPalReportDetails;
            OutputPath = outputPath;
        }
    }
}

using PayPalReports.DataModels.PayPalAPI.PayPalBalanceResponse;
using PayPalReports.DataModels.PayPalAPI.PayPalTransactionResponse;

namespace PayPalReports.DataModels.PayPalAPI
{
    class PayPalReportDetails
    {
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public TransactionResponse? PayPalTransactionResponse { get; set; }
        public BalanceResponse? PayPalEndBalanceResponse { get; set; }
        public BalanceResponse? PayPalStartBalanceResponse { get; set; }

        private readonly string ISO_DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:sszzz";

        public PayPalReportDetails()
        {
        }

        public string GetEndDateISO()
        {
            return EndDate.ToString(ISO_DATE_TIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
        }

        public string GetStartDateISO()
        {
            return StartDate.ToString(ISO_DATE_TIME_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

using PayPalReports.DataModels.PayPalBalanceResponse;
using PayPalReports.DataModels.PayPalTransactionResponse;

namespace PayPalReports.DataModels
{
    class PayPalReportDetails
    {
        public string EndDate { get; set; }
        public string StartDate { get; set; }
        public TransactionResponse? PayPalTransactionResponse { get; set; }
        public BalanceResponse? PayPalEndBalanceResponse { get; set; }
        public BalanceResponse? PayPalStartBalanceResponse { get; set; }

        public PayPalReportDetails()
        {
            EndDate = "";
            StartDate = "";
        }
    }
}

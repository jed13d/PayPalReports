using PayPalReports.DataModels.PayPalAPI.PayPalBalanceResponse;
using PayPalReports.DataModels.PayPalAPI.PayPalTransactionResponse;

namespace PayPalReports.DataModels.PayPalAPI
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

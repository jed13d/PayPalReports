namespace PayPalReports.DataModels
{
    class PayPalReportDetails
    {
        public string EndDate { get; set; }
        public string StartDate { get; set; }
        public PayPalTransactionResponse.PayPalTransactionResponse? PayPalTransactionResponse { get; set; }

        public PayPalReportDetails()
        {
            EndDate = "";
            StartDate = "";
        }
    }
}

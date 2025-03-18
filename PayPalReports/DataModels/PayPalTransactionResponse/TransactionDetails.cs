using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalTransactionResponse
{
    record class TransactionDetails(
        [property: JsonPropertyName("transaction_info")] TransactionInfo transaction_info,
        [property: JsonPropertyName("payer_info")] PayerInfo payer_info)
    {
    }
}

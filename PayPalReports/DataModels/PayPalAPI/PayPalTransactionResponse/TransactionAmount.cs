using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalAPI.PayPalTransactionResponse
{
    record class TransactionAmount(
        [property: JsonPropertyName("currency_code")] string currency_code,
        [property: JsonPropertyName("value")] string value)
    {
    }
}

using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalAPI.PayPalBalanceResponse
{
    record class BalanceAmount(
        [property: JsonPropertyName("currency_code")] string currency_code,
        [property: JsonPropertyName("value")] string value)
    {
    }
}

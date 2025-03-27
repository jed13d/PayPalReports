using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalAPI.PayPalBalanceResponse
{
    record class BalanceResponse(
        [property: JsonPropertyName("balances")] Balances[] balances,
        [property: JsonPropertyName("account_id")] string account_id,
        [property: JsonPropertyName("as_of_time")] string as_of_time,
        [property: JsonPropertyName("last_refresh_time")] string last_refresh_time)
    {
    }
}

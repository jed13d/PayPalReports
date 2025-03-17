using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalBalanceResponse
{
    record class Balances(
        [property: JsonPropertyName("total_balance")] BalanceAmount total_balance,
        [property: JsonPropertyName("available_balance")] BalanceAmount available_balance,
        [property: JsonPropertyName("withheld_balance")] BalanceAmount withheld_balance,
        [property: JsonPropertyName("currency")] string currency,
        [property: JsonPropertyName("primary")] bool primary)
    {
    }
}

using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalTransactionResponse
{
    record class PayerName(
        [property: JsonPropertyName("given_name")] string given_name,
        [property: JsonPropertyName("surname")] string surname,
        [property: JsonPropertyName("full_name")] string full_name,
        [property: JsonPropertyName("alternate_full_name")] string alternate_full_name)
    {
    }
}

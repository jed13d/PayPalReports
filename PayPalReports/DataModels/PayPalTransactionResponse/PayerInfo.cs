using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalTransactionResponse
{
    record class PayerInfo(
        [property: JsonPropertyName("account_id")] string account_id,
        [property: JsonPropertyName("email_address")] string email_address,
        [property: JsonPropertyName("address_status")] string address_status,
        [property: JsonPropertyName("payer_status")] string payer_status,
        [property: JsonPropertyName("payer_name")] PayerName payer_name,
        [property: JsonPropertyName("country_code")] string country_code)
    {
    }
}

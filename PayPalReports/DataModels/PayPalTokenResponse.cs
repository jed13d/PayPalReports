using System.Text.Json.Serialization;

namespace PayPalReports.DataModels
{
    record class PayPalTokenResponse(
        [property: JsonPropertyName("scope")] string scope,
        [property: JsonPropertyName("access_token")] string access_token,
        [property: JsonPropertyName("token_type")] string token_type,
        [property: JsonPropertyName("app_id")] string app_id,
        [property: JsonPropertyName("expires_in")] int expires_in,
        [property: JsonPropertyName("nonce")] string nonce)
    {
    }
}

using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.GoogleAPI
{
    record class GoogleApiCreds(
        [property: JsonPropertyName("client_id")] string client_id,
        [property: JsonPropertyName("project_id")] string project_id,
        [property: JsonPropertyName("auth_uri")] string auth_uri,
        [property: JsonPropertyName("token_uri")] string token_uri,
        [property: JsonPropertyName("auth_provider_x509_cert_url")] string auth_provider_x509_cert_url,
        [property: JsonPropertyName("client_secret")] string client_secret,
        [property: JsonPropertyName("redirect_uris")] string redirect_uris)
    {
    }
}

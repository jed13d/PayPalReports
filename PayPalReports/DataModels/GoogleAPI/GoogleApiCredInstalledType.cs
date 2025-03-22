using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.GoogleAPI
{
    record class GoogleApiCredInstalledType(
        [property: JsonPropertyName("installed")] GoogleApiCreds installed)
    {
    }
}

using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalTransactionResponse
{
    record class TransactionResponse(
        [property: JsonPropertyName("transaction_info")] TransactionInfo transaction_info,
        [property: JsonPropertyName("payer_info")] PayerInfo payer_info,
        [property: JsonPropertyName("account_number")] string account_number,
        [property: JsonPropertyName("last_refreshed_datetime")] string last_refreshed_datetime,
        [property: JsonPropertyName("end_date")] string end_date,
        [property: JsonPropertyName("start_date")] string start_date,
        [property: JsonPropertyName("page")] int page,
        [property: JsonPropertyName("total_items")] int total_items,
        [property: JsonPropertyName("total_pages")] int total_pages)
    {
    }
}

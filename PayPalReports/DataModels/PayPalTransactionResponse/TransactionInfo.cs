using System.Text.Json.Serialization;

namespace PayPalReports.DataModels.PayPalTransactionResponse
{
    record class TransactionInfo(
        [property: JsonPropertyName("paypal_account_id")] string paypal_account_id,
        [property: JsonPropertyName("transaction_id")] string transaction_id,
        [property: JsonPropertyName("paypal_reference_id")] string paypal_reference_id,
        [property: JsonPropertyName("paypal_reference_id_type")] string paypal_reference_id_type,
        [property: JsonPropertyName("transaction_event_code")] string transaction_event_code,
        [property: JsonPropertyName("transaction_status")] string transaction_status,
        [property: JsonPropertyName("transaction_subject")] string transaction_subject,
        [property: JsonPropertyName("transaction_note")] string transaction_note,
        [property: JsonPropertyName("transaction_initiation_date")] string transaction_initiation_date,
        [property: JsonPropertyName("transaction_updated_date")] string transaction_updated_date,
        [property: JsonPropertyName("transaction_amount")] TransactionAmount transaction_amount,
        [property: JsonPropertyName("fee_amount")] TransactionAmount fee_amount,
        [property: JsonPropertyName("ending_balance")] TransactionAmount ending_balance,
        [property: JsonPropertyName("available_balance")] TransactionAmount available_balance)
    {
    }
}

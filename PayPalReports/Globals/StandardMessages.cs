namespace PayPalReports.Globals
{
    class StandardMessages
    {
        // UpdateStatusText($"{StandardMessages.PAYPAL_FAILED_GETTING_TOKEN}");

        public static readonly string UNLIKELY_INTERNAL_ERROR = "An internal error, which shouldn't have happened, occurred.";

        public static readonly string PAYPAL_GETTING_DATA = "Requesting data from PayPal.";

        public static readonly string PAYPAL_GETTING_TOKEN = "Establishing secure connection with PayPal.";

        public static readonly string PAYPAL_FAILED_GETTING_TOKEN = "Failed to establish secure connection with PayPal API. Check you're able to connect with PayPal and try again.";

        public static readonly string POSSIBLE_PAYPAL_API_CHANGE = "An error occurred which may have resulted by a change to the API on PayPay's end.";
    }
}

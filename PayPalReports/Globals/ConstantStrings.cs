namespace PayPalReports.Globals
{
    static class ConstantStrings
    {
        // UpdateStatusText($"{StandardMessages.PAYPAL_FAILED_GETTING_TOKEN}");

        public static readonly string UNLIKELY_INTERNAL_ERROR = "An internal error, which shouldn't have happened, occurred.";

        // PayPal Service
        public static readonly string PAYPAL_GETTING_DATA = "Requesting data from PayPal.";

        public static readonly string PAYPAL_GETTING_TOKEN = "Establishing secure connection with PayPal.";

        public static readonly string PAYPAL_FAILED_GETTING_TOKEN = "Failed to establish secure connection with PayPal API. Check you're able to connect with PayPal and try again.";

        public static readonly string POSSIBLE_PAYPAL_API_CHANGE = "An error occurred which may have resulted by a change to the API on PayPay's end.";

        // Configuration Page
        public static readonly string DATA_EXISTS_MESSAGE = "PayPal API configuration data exists. The data / credentials have no validation. For security reasons, the data will not be displayed.";

        public static readonly string DATA_SAVED = "Configuration data saved. For security reasons, the form has been cleared and the data stored will not be shown.";

        public static readonly string DATA_NOT_SAVED = "Configuration data not saved. There seems to have been an error.";

        public static readonly string PAYPAL_DATA_FILE = "pdata.dat";

        public static readonly string DEFAULT_PAYPAL_API_URL = "https://api-m.sandbox.paypal.com https://api-m.paypal.com";

        public static readonly string DEFAULT_PAYPAL_REGION = "us";
    }
}

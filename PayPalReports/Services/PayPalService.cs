using PayPalReports.DataModels;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PayPalReports.Services
{
    class PayPalService
    {
        private string[] _apiData = new string[6];
        private PayPalTokenResponse? _tokenData;

        private readonly string PAYPAL_DATA_FILE = "pdata.dat";

        private readonly int URL = 0;
        private readonly int REGION = 1;
        private readonly int EMAIL = 2;
        private readonly int PASSWORD = 3;
        private readonly int ID = 4;
        private readonly int KEY = 5;

        private readonly string END_DATE_PARAMTER = "end_date";
        private readonly string FIELDS_PARAMETER = "fields=transaction_info,payer_info";
        private readonly string START_DATE_PARAMTER = "start_date";
        private readonly string PAGE_SIZE_PARAMETER = "page_size=500";

        private readonly string PAYPAL_TRANSACTION_HISTORY_ENDPOINT = "/v1/reporting/transactions";

        private readonly string PAYPAL_BALANCE_ENDPOINT = "/v1/reporting/balances";
        /*  requires TOKEN
         *  as_of_time              -   string [20 - 64] characters         internet date/time format  yyyy-mm-ddThh:mm:ss
         * 
         * */

        private readonly string PAYPAL_OAUTH_TOKEN_ENDPOINT = "/v1/oauth2/token";

        public PayPalService()
        {
            LoadApiInfo();
        }

        public async Task<bool> TestTokenRequest()
        {
            bool returnValue = false;

            if (_tokenData == null)     // if the token exists, no reason to get a new one
            {
                await RequestToken();
            }

            if (_tokenData != null)
            {
                returnValue = true;
            }

            return returnValue;
        }


        private void LoadApiInfo()
        {
            DataEncryptionService des = new();

            string fileData = des.RetrieveData(PAYPAL_DATA_FILE);

            _apiData = fileData.Split('\n');
        }

        // request transaction info
        // request balance info
        // actual call for all data (and store into a file -- another class? reportprocessingservice?)
        // fields in xaml for paramters needed (start / end dates, report type)
        //      remember to disable sumbit buttons upon submission / and release them upon completion

        private string GetStartDateParameter()
        {
            return "";
        }
        private async Task RequestTransactionInfo(PayPalReportDetails ppReportDetails)
        {
            if (_tokenData != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    // Setup Headers
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en_US"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_tokenData.token_type, _tokenData.access_token);

                    // Prepare Request Parameters
                    string endDate = $"{END_DATE_PARAMTER}={ppReportDetails.EndDate}";
                    string startDate = $"{START_DATE_PARAMTER}={ppReportDetails.StartDate}";

                    // Construct URL
                    string transactionURL = $"{_apiData[URL]}{PAYPAL_TRANSACTION_HISTORY_ENDPOINT}?{startDate}&{endDate}&{FIELDS_PARAMETER}";

                    // Send GET request
                    var response = await client.GetStreamAsync(transactionURL);

                    // Convert to objects through Json Deserialization
                }
            }
            /*      * required          Request Parameters
             * start_date   * (f/e)     -   string [20 - 64] characters         internet date/time format  yyyy-mm-ddThh:mm:ss
             * end_date     * (f/e)     -   ''
             * fields        (backend)  -   string  desired - "transaction_info,payer_info"
             * page_size    (be)        -   int     1-500 (default-100)
             * page         (be)        -   int     default 1
             * */
        }

        private async Task RequestToken()
        {
            using (HttpClient client = new HttpClient())
            {
                // Setup Headers
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en_US"));

                var base64encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiData[ID]}:{_apiData[KEY]}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64encoded);

                // Setup Content Data
                var keyValues = new List<KeyValuePair<string, string>>();
                keyValues.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

                // Send POST request 
                string token_uri = _apiData[URL] + PAYPAL_OAUTH_TOKEN_ENDPOINT;
                var responseMessage = await client.PostAsync(token_uri, new FormUrlEncodedContent(keyValues));

                // Stream the responseMessage through Json Deserializer, converting to usable object
                var tokenData = await JsonSerializer.DeserializeAsync<PayPalTokenResponse>(responseMessage.Content.ReadAsStream());

                if (tokenData != null)
                {
                    _tokenData = tokenData;
                }
                else
                {
                    Debug.WriteLine($"Failed to get token from {token_uri}");
                }

                /*      Sample Response for token
                    {
                      "scope": "https://uri.paypal.com/services/invoicing https://uri.paypal.com/services/disputes/read-buyer https://uri.paypal.com/services/payments/realtimepayment https://uri.paypal.com/services/disputes/update-seller https://uri.paypal.com/services/payments/payment/authcapture openid https://uri.paypal.com/services/disputes/read-seller https://uri.paypal.com/services/payments/refund https://api-m.paypal.com/v1/vault/credit-card https://api-m.paypal.com/v1/payments/.* https://uri.paypal.com/payments/payouts https://api-m.paypal.com/v1/vault/credit-card/.* https://uri.paypal.com/services/subscriptions https://uri.paypal.com/services/applications/webhooks",
                      "access_token": "A21AAFEpH4PsADK7qSS7pSRsgzfENtu-Q1ysgEDVDESseMHBYXVJYE8ovjj68elIDy8nF26AwPhfXTIeWAZHSLIsQkSYz9ifg",
                      "token_type": "Bearer",
                      "app_id": "APP-80W284485P519543T",
                      "expires_in": 31668,
                      "nonce": "2020-04-03T15:35:36ZaYZlGvEkV4yVSz8g6bAKFoGSEzuy3CQcz3ljhibkOHg"
                    }
                * */
            }
        }
    }
}

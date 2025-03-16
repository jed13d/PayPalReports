using PayPalReports.CustomEvents;
using PayPalReports.DataModels;
using PayPalReports.DataModels.PayPalTransactionResponse;
using PayPalReports.Globals;
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
        private PayPalReportDetails? _payPalReportDetails;

        private readonly string PAYPAL_DATA_FILE = "pdata.dat";

        private readonly int URL = 0;
        private readonly int ID = 4;
        private readonly int KEY = 5;

        private readonly int MAX_REQUEST_RETRYS = 5;

        private readonly string AS_OF_TIME = "as_of_time";
        private readonly string END_DATE_PARAMTER = "end_date";
        private readonly string FIELDS_PARAMETER = "fields=transaction_info,payer_info";
        private readonly string START_DATE_PARAMTER = "start_date";
        private readonly string PAGE_SIZE_PARAMETER = "page_size=500";

        private readonly string HEADER_ACCEPTS = "application/json";
        private readonly string HEADER_LANGUAGE = "en_US";
        private readonly string HEADER_TOKEN_AUTH = "Basic";
        private readonly string CONTENT_TOKEN_GRANT_KEY = "grant_type";
        private readonly string CONTENT_TOKEN_GRANT_VALUE = "client_credentials";

        private readonly string PAYPAL_TRANSACTION_HISTORY_ENDPOINT = "/v1/reporting/transactions";
        private readonly string PAYPAL_BALANCE_ENDPOINT = "/v1/reporting/balances";
        private readonly string PAYPAL_OAUTH_TOKEN_ENDPOINT = "/v1/oauth2/token";

        public PayPalService()
        {
            LoadApiInfo();
        }

        /// <summary>
        /// Calls the various PayPal API endpoints and loads the PayPalReportDetails argument with the returned data.
        /// </summary>
        /// <param name="payPalReportDetails">The PayPal data context object.</param>
        /// <returns>Boolean value reporting the success of the method.</returns>
        public bool TryGetPayPalData(ref PayPalReportDetails payPalReportDetails)
        {
            bool success = false;

            // Basic null check
            if (payPalReportDetails != null && !string.IsNullOrEmpty(payPalReportDetails.StartDate) && !string.IsNullOrEmpty(payPalReportDetails.EndDate))
            {
                _payPalReportDetails = payPalReportDetails;
                RequestReportData();
                payPalReportDetails = _payPalReportDetails;
                success = true;
            }
            else
            {
                UpdateStatusText($"{StandardMessages.UNLIKELY_INTERNAL_ERROR}");
            }

            return success;
        }

        /**
         * Outer-most private method for the various API calls to PayPal, gathering the various data required.
         * Delays are sprinkled in to throttle the API calls
         * */
        private async void RequestReportData()
        {
            // if the token is cached, no reason to get a new one
            if (_tokenData == null)
            {
                UpdateStatusText($"{StandardMessages.PAYPAL_GETTING_TOKEN}");

                await RequestToken();
                Task.Delay(100).Wait();
            }


            if (_tokenData != null)
            {
                Task transactionRequest = RequestTransactionInfo();
                Task.Delay(10).Wait();
                Task sBalanceRequest = RequestBalance(_payPalReportDetails!.StartDate);
                Task.Delay(10).Wait();
                Task eBalanceRequest = RequestBalance(_payPalReportDetails!.EndDate);

                await sBalanceRequest;
                await eBalanceRequest;
                await transactionRequest;
            }
            else
            {
                UpdateStatusText($"{StandardMessages.PAYPAL_FAILED_GETTING_TOKEN}");
            }
        }


        private void LoadApiInfo()
        {
            DataEncryptionService des = new();

            string fileData = des.RetrieveData(PAYPAL_DATA_FILE);

            _apiData = fileData.Split('\n');
        }


        private async Task RequestBalance(string atTime)
        {
            if (_tokenData != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    // Setup Headers
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HEADER_ACCEPTS));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(HEADER_LANGUAGE));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_tokenData.token_type, _tokenData.access_token);

                    // Prepare Request Parameters
                    string timeParameter = $"{AS_OF_TIME}={atTime}";

                    // Construct URL
                    string transactionURL = $"{_apiData[URL]}{PAYPAL_BALANCE_ENDPOINT}?{timeParameter}";

                    // Send GET request
                    var response = await client.GetAsync(transactionURL);

                    // Convert to objects through Json Deserialization
                    var transactionResponse = await JsonSerializer.DeserializeAsync<PayPalTransactionResponse>(response.Content.ReadAsStream());

                    if (transactionResponse != null)
                    {
                        _payPalReportDetails!.PayPalTransactionResponse = transactionResponse;
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to get balance data from {transactionURL}");
                    }
                }
            }
        }

        private async Task RequestToken()
        {
            using (HttpClient client = new HttpClient())
            {
                // Setup Headers
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HEADER_ACCEPTS));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(HEADER_LANGUAGE));

                // unlikely an error will occur here, but prepare anyway
                try
                {
                    var base64encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiData[ID]}:{_apiData[KEY]}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HEADER_TOKEN_AUTH, base64encoded);
                }
                catch (Exception ex)
                {
                    UpdateStatusText($"{StandardMessages.UNLIKELY_INTERNAL_ERROR}");
                    Debug.WriteLine(StandardMessages.UNLIKELY_INTERNAL_ERROR);
                    Debug.WriteLine(ex);
                    return;
                }

                // Setup Content Data
                List<KeyValuePair<string, string>> keyValues = [new KeyValuePair<string, string>(CONTENT_TOKEN_GRANT_KEY, CONTENT_TOKEN_GRANT_VALUE)];

                // Build URI
                string token_uri = _apiData[URL] + PAYPAL_OAUTH_TOKEN_ENDPOINT;

                // Multi-attempt request loop
                bool successfulRequest = false;
                for (int i = 0; i < MAX_REQUEST_RETRYS; i++)
                {
                    // Send POST request 
                    var response = await client.PostAsync(token_uri, new FormUrlEncodedContent(keyValues));

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        try
                        {
                            // Stream the responseMessage through Json Deserializer, converting to usable object
                            _tokenData = await JsonSerializer.DeserializeAsync<PayPalTokenResponse>(response.Content.ReadAsStream());
                            successfulRequest = true;
                        }
                        catch (Exception ex) when (ex is ArgumentNullException
                                                || ex is InvalidOperationException
                                                || ex is HttpRequestException
                                                || ex is TaskCanceledException
                                                || ex is EncoderFallbackException)
                        {
                            UpdateStatusText($"{StandardMessages.UNLIKELY_INTERNAL_ERROR}");
                            Debug.WriteLine(StandardMessages.UNLIKELY_INTERNAL_ERROR);
                            Debug.WriteLine(ex);
                        }
                        catch (Exception ex) when (ex is UriFormatException
                                                || ex is JsonException
                                                || ex is NotSupportedException)
                        {
                            UpdateStatusText($"{StandardMessages.POSSIBLE_PAYPAL_API_CHANGE}");
                            Debug.WriteLine(StandardMessages.POSSIBLE_PAYPAL_API_CHANGE);
                            Debug.WriteLine(ex);
                        }
                    }
                    if (successfulRequest)
                    {
                        break;
                    }
                }   // end for
            }   // end using
        }   // end RequestToken

        private async Task RequestTransactionInfo()
        {
            if (_tokenData != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    // Setup Headers
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HEADER_ACCEPTS));
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(HEADER_LANGUAGE));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_tokenData.token_type, _tokenData.access_token);

                    // Prepare Request Parameters
                    string endDate = $"{END_DATE_PARAMTER}={_payPalReportDetails!.EndDate}";
                    string startDate = $"{START_DATE_PARAMTER}={_payPalReportDetails!.StartDate}";

                    // Construct URL
                    string transactionURL = $"{_apiData[URL]}{PAYPAL_TRANSACTION_HISTORY_ENDPOINT}?{startDate}&{endDate}&{FIELDS_PARAMETER}";

                    // Send GET request
                    var response = await client.GetAsync(transactionURL);

                    // Convert to objects through Json Deserialization
                    var transactionResponse = await JsonSerializer.DeserializeAsync<PayPalTransactionResponse>(response.Content.ReadAsStream());

                    if (transactionResponse != null)
                    {
                        this._payPalReportDetails!.PayPalTransactionResponse = transactionResponse;
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to get transaction data from {transactionURL}");
                    }
                }
            }
        }

        /**
         * Method for messaging the user through the UI
         * (maybe pull this out and make event driven at bottom of window)
         * */
        private void UpdateStatusText(string message)
        {
            StatusEvent.Raise(message);
        }
    }
}

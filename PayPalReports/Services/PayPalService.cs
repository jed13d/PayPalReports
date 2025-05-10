using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayPalReports.CustomEvents;
using PayPalReports.DataModels.PayPalAPI;
using PayPalReports.DataModels.PayPalAPI.PayPalBalanceResponse;
using PayPalReports.DataModels.PayPalAPI.PayPalTransactionResponse;
using PayPalReports.Globals;
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
        private readonly int ID = 1;
        private readonly int KEY = 2;

        private readonly int MAX_REQUEST_RETRYS = 5;

        private readonly string AS_OF_TIME = "as_of_time";
        private readonly string END_DATE_PARAMTER = "end_date";
        private readonly string FIELDS_PARAMETER = "fields=transaction_info,payer_info";
        //private readonly string FIELDS_PARAMETER = "fields=all";
        private readonly string START_DATE_PARAMTER = "start_date";
        //private readonly string PAGE_SIZE_PARAMETER = "page_size=500";

        private readonly string HEADER_ACCEPTS = "application/json";
        private readonly string HEADER_LANGUAGE = "en_US";
        private readonly string HEADER_TOKEN_AUTH = "Basic";
        private readonly string CONTENT_TOKEN_GRANT_KEY = "grant_type";
        private readonly string CONTENT_TOKEN_GRANT_VALUE = "client_credentials";

        private readonly string PAYPAL_TRANSACTION_HISTORY_ENDPOINT = "/v1/reporting/transactions";
        private readonly string PAYPAL_BALANCE_ENDPOINT = "/v1/reporting/balances";
        private readonly string PAYPAL_OAUTH_TOKEN_ENDPOINT = "/v1/oauth2/token";

        private readonly ILogger<PayPalService> LOGGER;
        private readonly StatusEvent STATUS_EVENT;
        private readonly IServiceProvider SERVICE_PROVIDER;

        private enum BalanceDateType
        {
            Start = 0, End = 1
        }

        public PayPalService(IServiceProvider serviceProvider)
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<PayPalService>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
            SERVICE_PROVIDER = serviceProvider;
        }

        /// <summary>
        /// Calls the various PayPal API endpoints and loads the PayPalReportDetails argument with the returned data.
        /// </summary>
        /// <param name="payPalReportDetails">The PayPal data context object.</param>
        /// <returns>Boolean value reporting the success of the method.</returns>
        public bool TryGetPayPalData(ref PayPalReportDetails payPalReportDetails)
        {
            bool success = false;

            if (LoadApiInfo())
            {
                LOGGER.LogDebug("TryGetPayPalData, starting null checks.");
                // Basic null check
                if (payPalReportDetails != null)
                {
                    LOGGER.LogDebug("TryGetPayPalData, assigning local variables.");
                    _payPalReportDetails = payPalReportDetails;

                    LOGGER.LogDebug("TryGetPayPalData, calling aync task.");

                    Task.Run(RequestReportData).Wait();

                    LOGGER.LogDebug("TryGetPayPalData, reassigning reference variable data.");

                    DebugOutputPayPalReportDetails();

                    payPalReportDetails = _payPalReportDetails;
                    success = true;
                }
                else
                {
                    UpdateStatusText($"{ConstantStrings.UNLIKELY_INTERNAL_ERROR}");
                    LOGGER.LogDebug("{Error: {Message}", ConstantStrings.UNLIKELY_INTERNAL_ERROR);
                }
            }
            return success;
        }

        private void DebugOutputTokenData()
        {
            LOGGER.LogDebug("Token Data: {@Token}", _tokenData);
        }

        private void DebugOutputPayPalReportDetails()
        {
            LOGGER.LogDebug("##### DEBUG OUTPUT DATA REPORT-DETAILS #####");

            LOGGER.LogDebug("{@ReportDetails}", _payPalReportDetails);

            LOGGER.LogDebug("##### DEBUG OUTPUT DATA REPORT-DETAILS #####");
        }

        private bool LoadApiInfo()
        {
            bool success = false;

            DataEncryptionService des = SERVICE_PROVIDER.GetRequiredService<DataEncryptionService>();

            if (des.DataFileExists(PAYPAL_DATA_FILE))
            {
                string fileData = des.RetrieveData(PAYPAL_DATA_FILE);
                _apiData = fileData.Split('\n');
                success = true;
            }

            return success;
        }

        private async Task RequestBalance(string atTime, BalanceDateType balanceDateType)
        {
            if (_tokenData != null)
            {
                using HttpClient client = new();

                // Setup Headers
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HEADER_ACCEPTS));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(HEADER_LANGUAGE));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_tokenData.token_type, _tokenData.access_token);

                // Prepare Request Parameters
                string timeParameter = $"{AS_OF_TIME}={atTime}";

                // Construct URL
                string transactionURL = $"{_apiData[URL]}{PAYPAL_BALANCE_ENDPOINT}?{timeParameter}";
                LOGGER.LogDebug("RequestBalance Request URL: {Url}", transactionURL);

                // Multi-attempt request loop
                for (int i = 0; i < MAX_REQUEST_RETRYS; i++)
                {
                    // Send GET request
                    var response = await client.GetAsync(transactionURL);

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        LOGGER.LogDebug("RequestBalance Response: {Response}", response);
                        try
                        {
                            // Convert to objects through Json Deserialization
                            if (balanceDateType == BalanceDateType.Start)
                            {
                                _payPalReportDetails!.PayPalStartBalanceResponse = await JsonSerializer.DeserializeAsync<BalanceResponse>(response.Content.ReadAsStream());
                            }
                            else if (balanceDateType == BalanceDateType.End)
                            {
                                _payPalReportDetails!.PayPalEndBalanceResponse = await JsonSerializer.DeserializeAsync<BalanceResponse>(response.Content.ReadAsStream());
                            }
                            else
                            {
                                throw new ArgumentException($"Illegal argument: {balanceDateType}");
                            }
                            break;
                        }
                        catch (Exception ex) when (ex is ArgumentNullException
                                                || ex is InvalidOperationException
                                                || ex is HttpRequestException
                                                || ex is TaskCanceledException
                                                || ex is EncoderFallbackException)
                        {
                            LOGGER.LogError("Error: {Message}", ConstantStrings.UNLIKELY_INTERNAL_ERROR);
                            LOGGER.LogError("Exception: {@Exception}", ex);
                        }
                        catch (Exception ex) when (ex is UriFormatException
                                                || ex is JsonException
                                                || ex is NotSupportedException)
                        {
                            LOGGER.LogError("Error: {Message}", ConstantStrings.POSSIBLE_PAYPAL_API_CHANGE);
                            LOGGER.LogError("Exception: {@Exception}", ex);
                        }
                    }
                    else
                    {
                        LOGGER.LogError("RequestBalance Bad Response: {@Response}", response);
                    }
                }   // end for
                    // end using
            }
        }   // end RequestTransactionInfo

        /**
         * Outer-most private method for the various API calls to PayPal, gathering the various data required.
         * Delays are sprinkled in to throttle the API calls
         * */
        private async Task RequestReportData()
        {
            // if the token is cached, no reason to get a new one
            if (_tokenData == null)
            {
                LOGGER.LogDebug("Trace Message: {Message}", ConstantStrings.PAYPAL_GETTING_TOKEN);

                await RequestToken();
                Task.Delay(1000).Wait();
            }


            if (_tokenData != null)
            {
                //DebugOutputTokenData();

                LOGGER.LogDebug("Sending transaction info request");
                UpdateStatusText("Sending transaction info request");
                Task transactionRequest = RequestTransactionInfo();
                Task.Delay(100).Wait();

                LOGGER.LogDebug("Sending start balance request");
                UpdateStatusText("Sending start balance request");
                Task sBalanceRequest = RequestBalance(_payPalReportDetails!.GetStartDateISO(), BalanceDateType.Start);
                Task.Delay(100).Wait();

                LOGGER.LogDebug("Sending end balance request");
                UpdateStatusText("Sending end balance request");
                Task eBalanceRequest = RequestBalance(_payPalReportDetails!.GetEndDateISO(), BalanceDateType.End);

                LOGGER.LogDebug("Waiting on requests");
                UpdateStatusText("Waiting on requests");
                await sBalanceRequest;
                await eBalanceRequest;
                await transactionRequest;

            }
            else
            {
                LOGGER.LogError("Error: {Message}", ConstantStrings.PAYPAL_FAILED_GETTING_TOKEN);
            }
        }

        private async Task RequestToken()
        {
            using HttpClient client = new();

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
                LOGGER.LogError("Error: {Message}", ConstantStrings.UNLIKELY_INTERNAL_ERROR);
                LOGGER.LogError("Exception: {@Exception}", ex);
                return;
            }

            // Setup Content Data
            List<KeyValuePair<string, string>> keyValues = [new KeyValuePair<string, string>(CONTENT_TOKEN_GRANT_KEY, CONTENT_TOKEN_GRANT_VALUE)];

            // Build URI
            string token_uri = _apiData[URL] + PAYPAL_OAUTH_TOKEN_ENDPOINT;

            // Multi-attempt request loop
            for (int i = 0; i < MAX_REQUEST_RETRYS; i++)
            {
                // Send POST request 
                var response = await client.PostAsync(token_uri, new FormUrlEncodedContent(keyValues));

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    LOGGER.LogDebug("RequestToken Response: {Response}", response);
                    try
                    {
                        // Stream the responseMessage through Json Deserializer, converting to usable object
                        _tokenData = await JsonSerializer.DeserializeAsync<PayPalTokenResponse>(response.Content.ReadAsStream());
                        break;
                    }
                    catch (Exception ex) when (ex is ArgumentNullException
                                            || ex is InvalidOperationException
                                            || ex is HttpRequestException
                                            || ex is TaskCanceledException
                                            || ex is EncoderFallbackException)
                    {
                        LOGGER.LogError("Error: {Message}", ConstantStrings.UNLIKELY_INTERNAL_ERROR);
                        LOGGER.LogError("Exception: {@Exception}", ex);
                    }
                    catch (Exception ex) when (ex is UriFormatException
                                            || ex is JsonException
                                            || ex is NotSupportedException)
                    {
                        LOGGER.LogError("Error: {Message}", ConstantStrings.POSSIBLE_PAYPAL_API_CHANGE);
                        LOGGER.LogError("Exception: {@Exception}", ex);
                    }
                }
                else
                {
                    LOGGER.LogError("RequestToken Bad Response: {@Response}", response);
                }
            }   // end for
                // end using
        }   // end RequestToken

        private async Task RequestTransactionInfo()
        {
            if (_tokenData != null)
            {
                using HttpClient client = new();

                // Setup Headers
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HEADER_ACCEPTS));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(HEADER_LANGUAGE));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_tokenData.token_type, _tokenData.access_token);

                // Prepare Request Parameters
                string endDate = $"{END_DATE_PARAMTER}={_payPalReportDetails!.GetEndDateISO()}";
                string startDate = $"{START_DATE_PARAMTER}={_payPalReportDetails!.GetStartDateISO()}";

                // Construct URL
                string transactionURL = $"{_apiData[URL]}{PAYPAL_TRANSACTION_HISTORY_ENDPOINT}?{startDate}&{endDate}&{FIELDS_PARAMETER}";
                LOGGER.LogError("RequestTransactionInfo Request URL: {Url}", transactionURL);

                // Multi-attempt request loop
                for (int i = 0; i < MAX_REQUEST_RETRYS; i++)
                {
                    // Send GET request
                    var response = await client.GetAsync(transactionURL);

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        LOGGER.LogError("RequestTransactionInfo Response: {Response}", response);
                        try
                        {
                            // Convert to objects through Json Deserialization
                            _payPalReportDetails!.PayPalTransactionResponse = await JsonSerializer.DeserializeAsync<TransactionResponse>(response.Content.ReadAsStream());
                            break;
                        }
                        catch (Exception ex) when (ex is ArgumentNullException
                                                || ex is InvalidOperationException
                                                || ex is HttpRequestException
                                                || ex is TaskCanceledException
                                                || ex is EncoderFallbackException)
                        {
                            LOGGER.LogError("Error: {Message}", ConstantStrings.UNLIKELY_INTERNAL_ERROR);
                            LOGGER.LogError("Exception: {@Exception}", ex);
                        }
                        catch (Exception ex) when (ex is UriFormatException
                                                || ex is JsonException
                                                || ex is NotSupportedException)
                        {
                            LOGGER.LogError("Error: {Message}", ConstantStrings.POSSIBLE_PAYPAL_API_CHANGE);
                            LOGGER.LogError("Exception: {@Exception}", ex);
                        }
                    }
                    else
                    {
                        LOGGER.LogError("RequestTransactionInfo Bad Response: {@Response}", response);
                        return;
                    }
                }   // end for
                    // end using
            }
        }   // end RequestTransactionInfo

        /**
         * Method for messaging the user through the UI
         * (maybe pull this out and make event driven at bottom of window)
         * */
        private void UpdateStatusText(string message)
        {
            STATUS_EVENT.Raise(message);
        }
    }
}
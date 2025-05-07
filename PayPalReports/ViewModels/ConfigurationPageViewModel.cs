using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayPalReports.Commands;
using PayPalReports.CustomEvents;
using PayPalReports.Globals;
using PayPalReports.Services;
using System.Text;
using System.Windows.Input;

namespace PayPalReports.ViewModels
{
    internal class ConfigurationPageViewModel : BaseViewModel
    {
        public ICommand SaveConfigurationCommand { get; }

        public bool CanSaveConfiguration => HasURL && HasClientID && HasClientKey;

        private readonly ILogger<ConfigurationPageViewModel> LOGGER;
        private readonly StatusEvent STATUS_EVENT;
        private readonly IServiceProvider SERVICE_PROVIDER;

        private string _paypalURL = string.Empty;
        private string _clientID = string.Empty;
        private string _clientKey = string.Empty;

        private bool HasURL => !string.IsNullOrEmpty(PayPalURL);

        private bool HasClientID => !string.IsNullOrEmpty(ClientID);

        private bool HasClientKey => !string.IsNullOrEmpty(ClientKey);

        public string PayPalURL
        {
            get => _paypalURL;
            set
            {
                _paypalURL = value;
                OnPropertyChanged(nameof(PayPalURL));
                OnPropertyChanged(nameof(CanSaveConfiguration));
            }
        }

        public string ClientID
        {
            get => _clientID;
            set
            {
                _clientID = value;
                OnPropertyChanged(nameof(ClientID));
                OnPropertyChanged(nameof(CanSaveConfiguration));
            }
        }

        public string ClientKey
        {
            get => _clientKey;
            set
            {
                _clientKey = value;
                OnPropertyChanged(nameof(ClientKey));
                OnPropertyChanged(nameof(CanSaveConfiguration));
            }
        }

        public ConfigurationPageViewModel(IServiceProvider serviceProvider)
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<ConfigurationPageViewModel>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
            SERVICE_PROVIDER = serviceProvider;

            SaveConfigurationCommand = new SaveConfigurationCommand(this);

            PayPalURL = ConstantStrings.DEFAULT_PAYPAL_API_URL;

            TestConfigurationStatus();
        }

        public void SaveConfiguration()
        {
            UpdateStatusText("Saving configuration.");
            DataEncryptionService des = SERVICE_PROVIDER.GetRequiredService<DataEncryptionService>();

            //Calling encryption service to store data
            if (des.StoreData(CreateDataString(), ConstantStrings.PAYPAL_DATA_FILE))
            {
                UpdateStatusText(ConstantStrings.DATA_SAVED);
                ClearFormData();
            }
            else
            {
                UpdateStatusText(ConstantStrings.DATA_NOT_SAVED);
            }
        }

        private void ClearFormData()
        {
            PayPalURL = "";
            ClientID = "";
            ClientKey = "";
        }

        /**
         * Converts the data from forms to single string formatted with a '\n' delimiter
         * */
        private byte[] CreateDataString()
        {
            StringBuilder sb = new();

            sb.AppendFormat("{0}\n", PayPalURL);
            sb.AppendFormat("{0}\n", ClientID);
            sb.AppendFormat("{0}", ClientKey);

            return UnicodeEncoding.ASCII.GetBytes(sb.ToString());
        }

        /**
         * Tests for the existance of a configuration file and provides the UI with the status
         * */
        private void TestConfigurationStatus()
        {
            DataEncryptionService des = SERVICE_PROVIDER.GetRequiredService<DataEncryptionService>();

            if (des.DataFileExists(ConstantStrings.PAYPAL_DATA_FILE))
            {
                UpdateStatusText(ConstantStrings.DATA_EXISTS_MESSAGE);
            }
        }

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

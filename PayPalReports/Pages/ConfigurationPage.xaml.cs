using PayPalReports.CustomEvents;
using PayPalReports.Services;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PayPalReports.Pages
{
    public partial class ConfigurationPage : Page
    {
        private readonly string DATA_EXISTS_MESSAGE = "PayPal API configuration data exists. It's unknown whether the data is valid. For security reasons, the data will not be displayed.";
        private readonly string DATA_SAVED = "Configuration data saved. For security reasons, the form has been cleared and the data stored will not be shown.";
        private readonly string PAYPAL_DATA_FILE = "pdata.dat";

        public ConfigurationPage()
        {
            InitializeComponent();

            TestConfigurationStatus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DataEncryptionService des = new();

            // Calling encryption service to store data
            if (des.StoreData(CreateDataString(), PAYPAL_DATA_FILE))
            {
                UpdateStatusText(DATA_SAVED);
                ClearFormData();
            }
        }

        private void ClearFormData()
        {
            PayPalURL.Text = "";
            Region.Text = "";
            ClientID.Text = "";
            ClientKey.Text = "";
        }

        /**
         * Converts the data from forms to single string formatted with a '\n' delimiter
         * */
        private byte[] CreateDataString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}\n", PayPalURL.Text);
            sb.AppendFormat("{0}\n", Region.Text);
            sb.AppendFormat("{0}\n", ClientID.Text);
            sb.AppendFormat("{0}", ClientKey.Text);

            return UnicodeEncoding.ASCII.GetBytes(sb.ToString());
        }

        /**
         * Tests for the existance of a configuration file and provides the UI with the status
         * */
        private void TestConfigurationStatus()
        {
            DataEncryptionService des = new();

            if (des.DataFileExists(PAYPAL_DATA_FILE))
            {
                UpdateStatusText(DATA_EXISTS_MESSAGE);
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

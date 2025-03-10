using PayPalReports.Services;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PayPalReports.Pages
{
    public partial class PayPalConfigurationPage : Page
    {
        private readonly string DATA_EXISTS_MESSAGE = "PayPal API configuration data exists. It's unknown whether the data is valid. For security reasons, the data will not be displayed.";
        private readonly string DATA_SAVED = "Configuration data saved. For security reasons, the form has been cleared and the data stored will not be shown.";
        private readonly string PAYPAL_DATA_FILE = "pdata.dat";

        public PayPalConfigurationPage()
        {
            InitializeComponent();

            TestConfigurationStatus();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            DataEncryptionService des = new();

            string fileData = des.RetrieveData(PAYPAL_DATA_FILE);
            Debug.WriteLine($"fileData as single string:\n{fileData}");

            Debug.WriteLine($"fileData as array:");
            String[] apiData = fileData.Split('\n');
            int i = 0;
            foreach (string line in apiData)
            {
                Debug.WriteLine($"index: {i++}, value:{line}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DataEncryptionService des = new();

            Debug.WriteLine("Calling encryption service to store data.");
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
            Email.Text = "";
            Password.Text = "";
            ClientID.Text = "";
            ClientKey.Text = "";
        }

        private byte[] CreateDataString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0}\n", PayPalURL.Text);
            sb.AppendFormat("{0}\n", Region.Text);
            sb.AppendFormat("{0}\n", Email.Text);
            sb.AppendFormat("{0}\n", Password.Text);
            sb.AppendFormat("{0}\n", ClientID.Text);
            sb.AppendFormat("{0}", ClientKey.Text);

            return UnicodeEncoding.ASCII.GetBytes(sb.ToString());
        }

        private void TestConfigurationStatus()
        {
            DataEncryptionService des = new();

            if (des.DataFileExists(PAYPAL_DATA_FILE))
            {
                UpdateStatusText(DATA_EXISTS_MESSAGE);
            }
        }

        private void UpdateStatusText(string message)
        {
            ConfigStatusBlock.Text = message;
        }
    }
}

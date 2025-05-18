using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PayPalReports.Commands;
using PayPalReports.CustomEvents;
using PayPalReports.DataModels;
using PayPalReports.DataModels.PayPalAPI;
using PayPalReports.Services;
//using PayPalReports.Services.Mock;
using System.Windows.Input;

namespace PayPalReports.ViewModels
{
    internal class ReportsPageViewModel : BaseViewModel
    {
        public ICommand DestinationFolderSearchCommand { get; }
        public ICommand SubmitReportRequestCommand { get; }

        public bool CanRequestForReport => HasDestinationPath && !IsSubmitting;

        private readonly PayPalService _payPalService;
        private readonly ExcelService _excelService;

        private readonly ILogger<ReportsPageViewModel> LOGGER;
        private readonly StatusEvent STATUS_EVENT;
        private readonly IServiceProvider SERVICE_PROVIDER;

        private readonly string END_OF_DAY_TIME = "23:59:59";
        private readonly int MAX_DATE_RANGE = 31;

        private readonly string DIALOG_FILENAME = "Master-Ledger";
        private readonly string DIALOG_FILE_EXTENSION = ".xlsx";
        private readonly string FILTER = "Excel Book (.xlsx)|*.xlsx";

        private bool HasDestinationPath => !string.IsNullOrEmpty(DestinationPath);

        private string _destinationPath = string.Empty;
        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                _destinationPath = value;
                OnPropertyChanged(nameof(DestinationPath));
                OnPropertyChanged(nameof(CanRequestForReport));
            }
        }

        private DateTime _endDate = DateTime.Today;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
                OnPropertyChanged(nameof(CanRequestForReport));
            }
        }

        private bool _isSubmitting = false;
        public bool IsSubmitting
        {
            get => _isSubmitting;
            set
            {
                _isSubmitting = value;
                OnPropertyChanged(nameof(IsSubmitting));
                OnPropertyChanged(nameof(CanRequestForReport));
            }
        }

        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(CanRequestForReport));
            }
        }

        public ReportsPageViewModel(IServiceProvider serviceProvider)
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<ReportsPageViewModel>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
            SERVICE_PROVIDER = serviceProvider;

            _payPalService = serviceProvider.GetRequiredService<PayPalService>();
            _excelService = serviceProvider.GetRequiredService<ExcelService>();

            DestinationFolderSearchCommand = new DestinationFolderSearchCommand(this);
            SubmitReportRequestCommand = new SubmitReportRequestCommand(this);
        }

        /// <summary>
        /// Loads dialog to allow user to browse their local system to choose an output destination folder.
        /// </summary>
        public void DestinationFolderSearch()
        {
            var dialog = new SaveFileDialog
            {
                FileName = DIALOG_FILENAME,
                DefaultExt = DIALOG_FILE_EXTENSION,
                Filter = FILTER
            };

            if (dialog.ShowDialog() == true)
            {
                LOGGER.LogDebug("Destination Path from dialog: {Path}", dialog.FileName);
                DestinationPath = dialog.FileName;
            }
        }

        private bool DatesAreValid()
        {
            // Fix EndDate if necessary
            if (EndDate > DateTime.Now)
            {
                EndDate = DateTime.Now;
            }

            TimeSpan interval = EndDate - StartDate;

            if (StartDate.Month != EndDate.Month)
            {
                UpdateStatusText("Start Date and End Date must be in the same month.");
                return false;
            }

            if (interval.TotalDays <= 0)
            {
                UpdateStatusText("Start Date must be a date before End Date");
                return false;
            }

            if (interval.TotalDays > MAX_DATE_RANGE)
            {
                UpdateStatusText($"PayPal supports a maximum date range of {MAX_DATE_RANGE} days.");
                return false;
            }

            return true;
        }

        public void SubmitReportRequest()
        {
            // Disable button to prevent multiple submissions
            IsSubmitting = true;

            // Utilize DateTime object to convert UI form submission to ISO8601 Internet Date/Time Format
            string endDate = $"{EndDate.ToShortDateString()} {END_OF_DAY_TIME}";
            EndDate = DateTime.Parse(endDate);

            if (DatesAreValid())
            {

                // Create context object for storing and passing data
                PayPalReportDetails payPalReportDetails = new()
                {
                    StartDate = StartDate,
                    EndDate = EndDate
                };

                UpdateStatusText("Making request for data from PayPal.");

                // Begin PayPalService series of requests for data pull
                //MockPayPalService mockPayPalService = new MockPayPalService();
                //if (mockPayPalService.TryGetPayPalData(ref payPalReportDetails))
                if (_payPalService.TryGetPayPalData(ref payPalReportDetails))
                {
                    // processing delay
                    Thread.Sleep(1000);

                    //DebugOutputPayPalReportDetails(payPalReportDetails);

                    if (GenerateReport(payPalReportDetails))
                    {
                        UpdateStatusText($"Report generation is complete.");
                    }
                    else
                    {
                        UpdateStatusText($"There has been an error generating your report. Check the logs for more information.");
                    }
                }
                else
                {
                    UpdateStatusText($"There has been an error getting the data from PayPal.");
                }
            }

            // reenable button once complete
            IsSubmitting = false;
        }

        private void DebugOutputPayPalReportDetails(PayPalReportDetails payPalReportDetails)
        {
            LOGGER.LogDebug("##### DEBUG OUTPUT DATA REPORT-DETAILS START #####");

            LOGGER.LogDebug("{@ReportDetails}", payPalReportDetails);

            LOGGER.LogDebug("##### DEBUG OUTPUT DATA REPORT-DETAILS END #####");
        }

        private bool GenerateReport(PayPalReportDetails payPalReportDetails)
        {
            UpdateStatusText($"Generating report.");
            ExcelReportContext excelReportContext = new(payPalReportDetails, DestinationPath);
            DebugOutputPayPalReportDetails(payPalReportDetails);
            return _excelService.GenerateReport(excelReportContext);
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

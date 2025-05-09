using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PayPalReports.CustomEvents;
using PayPalReports.DataModels;
using PayPalReports.DataModels.PayPalAPI.PayPalTransactionResponse;
using System.Drawing;
using System.IO;

namespace PayPalReports.Services
{
    internal class ExcelService
    {
        private readonly string TEMPLATE = "Template";

        private readonly string TITLE = "MASTER LEDGER";
        private readonly string TITLE_CELL = "D1";
        private readonly string TITLE_DATE_CELL = "D2";

        private readonly string OPENING_BALANCE_HEADER = "Opening Balance: ";
        private readonly string OPENING_BALANCE_HEADER_CELL = "A4";
        private readonly string OPENING_BALANCE_CELL = "B4";

        private readonly string[] HEADER_STRINGS = ["Date", "Reference", "Account", "Explanation", "Debit (+)", "Credit (-)", "Balance"];
        private readonly string[] DROPDOWN_TABLE_VALUES_C1 = ["Types of Accounts", "Petty Cash-Revenue", "Checking", "PayPal-Expenses", "PayPal-Revenue", "Petty Cash-Expenses"];
        private readonly string[] DROPDOWN_TABLE_VALUES_C2 = ["DR/CR", "CR", "DR", "DR", "CR", "DR"];

        private readonly string DEBIT_TOTAL_CELL = "E7";
        private readonly string CREDIT_TOTAL_CELL = "F7";
        private readonly string BALANCE_TOTAL_CELL = "G7";
        private readonly string BALANCE_TOTAL_FORMULA = "=B4+E7-F7";

        private readonly string CURRENCY_FORMAT = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
        private readonly string[] ALPHA = ["A", "B", "C", "D", "E", "F", "G", "H", "I"];
        private readonly string[] MONTH_STRING = ["", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        private readonly string ACCOUNT_DROPDOWN_VALUES_CELLS_FORMULA = "$I$10:%I%14";
        private readonly int DATA_START_ROW = 8;
        private readonly string COL_DATE = "A";
        private readonly string COL_REFERENCE = "B";
        private readonly string COL_ACCOUNT = "C";
        private readonly string COL_NOTES = "D";
        private readonly string COL_CREDIT = "E";
        private readonly string COL_DEBIT = "F";
        private readonly string COL_BALANCE = "G";

        private readonly ILogger<ExcelService> LOGGER;
        private readonly StatusEvent STATUS_EVENT;
        private readonly IServiceProvider SERVICE_PROVIDER;

        public ExcelService(IServiceProvider serviceProvider)
        {
            LOGGER = serviceProvider.GetRequiredService<ILogger<ExcelService>>();
            STATUS_EVENT = serviceProvider.GetRequiredService<StatusEvent>();
            SERVICE_PROVIDER = serviceProvider;

            ExcelPackage.License.SetNonCommercialOrganization("Non-commercial Organization");
        }

        private void DebugOutputPayPalReportDetails(ExcelReportContext reportContext)
        {
            LOGGER.LogDebug("##### DEBUG OUTPUT DATA ExcelReportContext START #####");

            LOGGER.LogDebug("{@ReportDetails}", reportContext);

            LOGGER.LogDebug("##### DEBUG OUTPUT DATA ExcelReportContext END #####");
        }

        /// <summary>
        /// Checks the various pieces of the data to ensure important pieces exist
        /// </summary>
        /// <param name="reportContext"></param>
        /// <returns>test success</returns>
        public bool DataIsGood(ExcelReportContext reportContext)
        {
            // Debugging
            DebugOutputPayPalReportDetails(reportContext);

            // Context layer
            if (reportContext == null)
            {
                UpdateStatusText("Internal error: Data context in ExcelService data validation check.");
                return false;
            }

            if (string.IsNullOrEmpty(reportContext.OutputPath))
            {
                UpdateStatusText("Internal error: Output Path missing in ExcelService data validation check.");
                return false;
            }

            if (reportContext.PayPalReportDetails == null)
            {
                UpdateStatusText("Internal error: Data missing in ExcelService data validation check.");
                return false;
            }

            // Details layer
            if (reportContext.PayPalReportDetails.PayPalEndBalanceResponse == null)
            {
                UpdateStatusText("Internal error: PayPalEndBalanceResponse missing in ExcelService data validation check.");
                return false;
            }

            if (reportContext.PayPalReportDetails.PayPalStartBalanceResponse == null)
            {
                UpdateStatusText("Internal error: PayPalStartBalanceResponse missing in ExcelService data validation check.");
                return false;
            }

            if (reportContext.PayPalReportDetails.PayPalTransactionResponse == null)
            {
                UpdateStatusText("Internal error: PayPalTransactionResponse missing in ExcelService data validation check.");
                return false;
            }

            // Everything checks out, should be able to process
            return true;
        }

        public bool GenerateReport(ExcelReportContext reportContext)
        {
            bool success = DataIsGood(reportContext);

            if (success)
            {
                UpdateStatusText($"Generating report from data.");

                // Create a excel package
                using (ExcelPackage excelPackage = new(new FileInfo(reportContext.OutputPath)))
                {
                    // Get the workbook
                    ExcelWorkbook xlsWorkbook = excelPackage.Workbook;

                    // processing delay
                    Thread.Sleep(1000);

                    // create the template if it doesn't already exist
                    if (xlsWorkbook.Worksheets[TEMPLATE] == null)
                    {
                        UpdateStatusText($"Generating template worksheet.");
                        GenerateTemplate(xlsWorkbook);
                    }

                    // some variable definitions for the data assignment loop
                    int curMonth = -1;
                    int curYear = -1;
                    int curRow = -1;
                    string titleString = "";
                    ExcelWorksheet xlsWorksheet = xlsWorkbook.Worksheets[TEMPLATE];
                    foreach (TransactionDetails transactionDetails in reportContext!.PayPalReportDetails!.PayPalTransactionResponse!.transaction_details)
                    {
                        // check date is still within the current month, or move to a new sheet
                        DateTime updateDate = DateTime.Parse(transactionDetails.transaction_info.transaction_updated_date);
                        if (updateDate.Month != curMonth)
                        {
                            // if curMonth > 0, then input balance totals, closing out the sheet before creating a new one
                            if (curMonth > 0)
                            {
                                xlsWorksheet.Cells[DEBIT_TOTAL_CELL].Formula = $"=SUM(E8:E{curRow - 1};{CURRENCY_FORMAT})";
                                xlsWorksheet.Cells[CREDIT_TOTAL_CELL].Formula = $"=SUM(F8:F{curRow - 1};{CURRENCY_FORMAT})";
                            }

                            // new sheet is required

                            UpdateStatusText($"Generating worksheet for {MONTH_STRING[curMonth]}.");
                            // processing delay
                            Thread.Sleep(1000);

                            // update sheet variables
                            curMonth = updateDate.Month;
                            curYear = updateDate.Year;
                            curRow = DATA_START_ROW;
                            titleString = $"{MONTH_STRING[curMonth]} {curYear}";

                            // create a new sheet
                            CopyTemplate(xlsWorkbook, titleString);
                            xlsWorksheet = xlsWorkbook.Worksheets[titleString];

                            // Update Title "Date"
                            xlsWorksheet.Cells[TITLE_DATE_CELL].Value = titleString;
                        }

                        // assign data to various fields

                        // date
                        xlsWorksheet.Cells[$"{curRow}{COL_DATE}"].Value = updateDate.Date.ToString();

                        // reference 
                        xlsWorksheet.Cells[$"{curRow}{COL_REFERENCE}"].Value = transactionDetails.transaction_info.transaction_id;

                        // account dropdown
                        ExcelRange accountTypeCell = xlsWorksheet.Cells[$"{curRow}{COL_ACCOUNT}"];
                        var dropdown = xlsWorksheet.DataValidations.AddListValidation(accountTypeCell.Address);
                        dropdown.Formula.ExcelFormula = ACCOUNT_DROPDOWN_VALUES_CELLS_FORMULA;

                        // explanation 
                        xlsWorksheet.Cells[$"{curRow}{COL_NOTES}"].Value = transactionDetails.transaction_info.transaction_note;

                        double transactionAmount = double.Parse(transactionDetails.transaction_info.transaction_amount.value);

                        // debit        // FORMAT FOR ACCOUNTING
                        xlsWorksheet.Cells[$"{curRow}{COL_DEBIT}"].Value = transactionAmount >= 0 ? transactionAmount : 0;
                        xlsWorksheet.Cells[$"{curRow}{COL_DEBIT}"].Formula = CURRENCY_FORMAT;

                        // credit 
                        xlsWorksheet.Cells[$"{curRow}{COL_CREDIT}"].Value = transactionAmount <= 0 ? Math.Abs(transactionAmount) : 0;
                        xlsWorksheet.Cells[$"{curRow}{COL_CREDIT}"].Formula = CURRENCY_FORMAT;

                        // balance 
                        xlsWorksheet.Cells[$"{curRow}{COL_BALANCE}"].Value = transactionDetails.transaction_info.transaction_note;
                        xlsWorksheet.Cells[$"{curRow}{COL_BALANCE}"].Formula = CURRENCY_FORMAT;

                        // increment the current row
                        curRow++;

                    }   // data assignment loop

                    excelPackage.Save();
                }
            }

            return success;
        }

        private void CopyTemplate(ExcelWorkbook xlsWorkbook, string destinationWorksheetName)
        {
            xlsWorkbook.Worksheets[TEMPLATE].Name = destinationWorksheetName;
            xlsWorkbook.Worksheets.Copy(destinationWorksheetName, TEMPLATE);
        }

        private void GenerateTemplate(ExcelWorkbook xlsWorkbook)
        {
            // Create the tab / sheet
            ExcelWorksheet xlsWorksheet = xlsWorkbook.Worksheets.Add(TEMPLATE);

            // Setup Title Cells
            using (ExcelRange titleCell = xlsWorksheet.Cells[TITLE_CELL])
            {
                titleCell.Value = TITLE;
                ApplyStyleTitle(titleCell);
            }
            using (ExcelRange titleDateCell = xlsWorksheet.Cells[TITLE_DATE_CELL])
            {
                titleDateCell.Value = TEMPLATE;
                ApplyStyleTitle(titleDateCell);
            }

            // Setup Opening Balance
            using (ExcelRange openBalHeaderCell = xlsWorksheet.Cells[OPENING_BALANCE_HEADER_CELL])
            {
                openBalHeaderCell.Value = OPENING_BALANCE_HEADER;
                ApplyStyleHeader(openBalHeaderCell);
            }
            ApplyStyleOpeningBalance(xlsWorksheet.Cells[OPENING_BALANCE_CELL]);

            // Setup Read of the headers
            ApplyStyleHeader(xlsWorksheet.Cells["A6:G7"]);
            for (int column = 0; column < HEADER_STRINGS.Length; column++)
            {
                xlsWorksheet.Cells[$"{ALPHA[column]}6"].Value = HEADER_STRINGS[column];
            }
            xlsWorksheet.Cells[BALANCE_TOTAL_CELL].Formula = $"{BALANCE_TOTAL_FORMULA};{CURRENCY_FORMAT}";

            // Freeze header rows
            xlsWorksheet.View.FreezePanes(8, 1);

            // Add autofilter to a range of cells
            xlsWorksheet.Cells["A6:G6"].AutoFilter = true;


            // Setup Account dropdown table
            ApplyStyleHeader(xlsWorksheet.Cells["I9:J9"]);
            for (int i = 0, row = 9; i < DROPDOWN_TABLE_VALUES_C1.Length; i++, row++)
            {
                xlsWorksheet.Cells[row, 9].Value = DROPDOWN_TABLE_VALUES_C1[i];
                xlsWorksheet.Cells[row, 10].Value = DROPDOWN_TABLE_VALUES_C2[i];
            }


            // Set column widths
            for (int column = 1; column <= 10; column++)
            {
                xlsWorksheet.Columns[1].Width = 20.71;
                xlsWorksheet.Columns[2].Width = 13.57;
                xlsWorksheet.Columns[3].Width = 25;
                xlsWorksheet.Columns[4].Width = 35;
                xlsWorksheet.Columns[5, 8].Width = 13.57;
                xlsWorksheet.Columns[9].Width = 25;
                xlsWorksheet.Columns[10].Width = 13.57;
            }

        }

        private void ApplyStyleOpeningBalance(ExcelRange cells)
        {
            cells.Style.Border.BorderAround(ExcelBorderStyle.Double, Color.CornflowerBlue);
            cells.Style.Numberformat.Format = CURRENCY_FORMAT;
        }

        private void ApplyStyleHeader(ExcelRange cells)
        {
            cells.Style.Fill.SetBackground(Color.CornflowerBlue);
            cells.Style.Font.Color.SetColor(Color.White);
            cells.Style.Font.Size = 14;
            cells.Style.Font.Bold = true;
        }

        private void ApplyStyleTitle(ExcelRange cells)
        {
            cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cells.Style.Font.Color.SetColor(Color.CornflowerBlue);
            cells.Style.Font.Size = 24;
            cells.Style.Font.Bold = true;
        }

        private void UpdateStatusText(string message)
        {
            STATUS_EVENT.Raise(message);
        }
    }
}

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

        private readonly string HEADERS_FOR_STYLE = "A6:G7";
        private readonly string HEADERS_FOR_AUTO_FILTER = "A6:G6";
        private readonly string HEADER_LABEL_ROW = "6";
        private readonly string[] HEADER_STRINGS = ["Date", "Reference", "Account", "Explanation", "Debit (+)", "Credit (-)", "Balance"];
        private readonly string[] DROPDOWN_TABLE_VALUES_C1 = ["Types of Accounts", "PayPal-Revenue", "Petty Cash-Revenue", "Checking", "PayPal-Expenses", "Petty Cash-Expenses"];
        private readonly string[] DROPDOWN_TABLE_VALUES_C2 = ["DR/CR", "DR", "DR", "CR", "CR", "CR"];

        private readonly string DEBIT_TOTAL_CELL = "E7";
        private readonly string CREDIT_TOTAL_CELL = "F7";
        private readonly string BALANCE_TOTAL_CELL = "G7";
        private readonly string BALANCE_TOTAL_FORMULA = "SUM(B4,E7,-F7)";

        private readonly string CURRENCY_FORMAT = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
        private readonly string[] ALPHA = ["A", "B", "C", "D", "E", "F", "G", "H", "I"];
        private readonly string[] MONTH_STRING = ["", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        private readonly string ACCOUNT_DROPDOWN_VALUES_CELLS_FORMULA = "$I$10:$I$14";
        private readonly int DATA_START_ROW = 8;
        private readonly string COL_DATE = "A";
        private readonly int COL_DATE_INT = 1;
        private readonly string COL_REFERENCE = "B";
        private readonly string COL_ACCOUNT = "C";
        private readonly int COL_ACCOUNT_INT = 3;
        private readonly string COL_NOTES = "D";
        private readonly string COL_DEBIT = "E";
        private readonly int COL_DEBIT_INT = 5;
        private readonly string COL_CREDIT = "F";
        private readonly int COL_CREDIT_INT = 6;
        private readonly string COL_BALANCE = "G";
        private readonly int COL_BALANCE_INT = 7;

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
                UpdateStatusText("Data error: Data context in ExcelService data validation check.");
                return false;
            }

            if (string.IsNullOrEmpty(reportContext.OutputPath))
            {
                UpdateStatusText("Data error: Output Path missing in ExcelService data validation check.");
                return false;
            }

            if (reportContext.PayPalReportDetails == null)
            {
                UpdateStatusText("Data error: Data missing in ExcelService data validation check.");
                return false;
            }

            // Details layer
            if (reportContext.PayPalReportDetails.PayPalEndBalanceResponse == null)
            {
                UpdateStatusText("Data error: PayPalEndBalanceResponse missing in ExcelService data validation check.");
                return false;
            }

            if (reportContext.PayPalReportDetails.PayPalStartBalanceResponse == null)
            {
                UpdateStatusText("Data error: PayPalStartBalanceResponse missing in ExcelService data validation check.");
                return false;
            }

            if (reportContext.PayPalReportDetails.PayPalTransactionResponse == null)
            {
                UpdateStatusText("Data error: PayPalTransactionResponse missing in ExcelService data validation check.");
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
                    else
                    {
                        LOGGER.LogDebug("Template exists, no need to generate.");
                    }

                    // some variable definitions for the data assignment loop
                    int curMonth = -1;
                    int curYear = -1;
                    int curRow = -1;
                    string titleString = string.Empty;
                    ExcelWorksheet xlsWorksheet = xlsWorkbook.Worksheets[TEMPLATE];
                    foreach (TransactionDetails transactionDetails in reportContext!.PayPalReportDetails!.PayPalTransactionResponse!.transaction_details)
                    {// record parsing loop

                        // check date is still within the current month, or move to a new sheet
                        DateTime updateDate = DateTime.Parse(transactionDetails.transaction_info.transaction_updated_date);
                        if (updateDate.Month != curMonth)
                        {
                            #region Create New Sheet
                            // if curMonth > 0, then input balance totals, closing out the sheet before creating a new one
                            if (curRow >= DATA_START_ROW && curMonth > 0)
                            {
                                xlsWorksheet.Cells[DEBIT_TOTAL_CELL].Formula = $"=SUM(E8:E{curRow - 1};{CURRENCY_FORMAT})";
                                xlsWorksheet.Cells[CREDIT_TOTAL_CELL].Formula = $"=SUM(F8:F{curRow - 1};{CURRENCY_FORMAT})";
                            }

                            // new sheet is required

                            if (curMonth >= 1 && curMonth <= 12)
                            {
                                UpdateStatusText($"Generating worksheet for {MONTH_STRING[curMonth]}.");
                            }
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

                            // Add beginning balance -- TODO modify for multiple month
                            xlsWorksheet.Cells[OPENING_BALANCE_CELL].Formula = CURRENCY_FORMAT;
                            xlsWorksheet.Cells[OPENING_BALANCE_CELL].Value = double.Parse(reportContext!.PayPalReportDetails!.PayPalStartBalanceResponse!.balances[0].total_balance.value);

                            #endregion
                        }

                        #region Record Data Assignment

                        // date
                        xlsWorksheet.Cells[$"{COL_DATE}{curRow}"].Value = updateDate;

                        // reference 
                        xlsWorksheet.Cells[$"{COL_REFERENCE}{curRow}"].Value = transactionDetails.transaction_info.transaction_id;

                        // explanation 
                        xlsWorksheet.Cells[$"{COL_NOTES}{curRow}"].Value = $"{transactionDetails.payer_info.email_address} - {transactionDetails.payer_info.payer_name.given_name} - {transactionDetails.transaction_info.transaction_note}";


                        // transaction amount (debit or credit)
                        double transactionAmount = double.Parse(transactionDetails.transaction_info.transaction_amount.value);

                        // debit
                        xlsWorksheet.Cells[$"{COL_DEBIT}{curRow}"].Formula = CURRENCY_FORMAT;
                        xlsWorksheet.Cells[$"{COL_DEBIT}{curRow}"].Value = transactionAmount >= 0f ? transactionAmount : 0f;

                        // credit 
                        xlsWorksheet.Cells[$"{COL_CREDIT}{curRow}"].Formula = CURRENCY_FORMAT;
                        xlsWorksheet.Cells[$"{COL_CREDIT}{curRow}"].Value = transactionAmount < 0f ? Math.Abs(transactionAmount) : 0f;

                        // balance 
                        xlsWorksheet.Cells[$"{COL_BALANCE}{curRow}"].Formula = CURRENCY_FORMAT;
                        xlsWorksheet.Cells[$"{COL_BALANCE}{curRow}"].Value = double.Parse(transactionDetails.transaction_info.ending_balance.value);

                        // account dropdown
                        xlsWorksheet.Cells[$"{COL_ACCOUNT}{curRow}"].Value = transactionAmount >= 0f ? DROPDOWN_TABLE_VALUES_C1[1] : DROPDOWN_TABLE_VALUES_C1[4];

                        #endregion

                        // increment the current row
                        curRow++;

                    }// record parsing loop

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
            ApplyStyleHeader(xlsWorksheet.Cells[HEADERS_FOR_STYLE]);
            for (int column = 0; column < HEADER_STRINGS.Length; column++)
            {
                xlsWorksheet.Cells[$"{ALPHA[column]}{HEADER_LABEL_ROW}"].Value = HEADER_STRINGS[column];
            }

            // Freeze header rows
            xlsWorksheet.View.FreezePanes(8, 1);

            // Add autofilter to a range of cells
            xlsWorksheet.Cells[HEADERS_FOR_AUTO_FILTER].AutoFilter = true;


            // Setup Account dropdown table
            ApplyStyleHeader(xlsWorksheet.Cells["I9:J9"]);
            for (int i = 0, row = 9; i < DROPDOWN_TABLE_VALUES_C1.Length; i++, row++)
            {
                xlsWorksheet.Cells[row, 9].Value = DROPDOWN_TABLE_VALUES_C1[i];
                xlsWorksheet.Cells[row, 10].Value = DROPDOWN_TABLE_VALUES_C2[i];
            }

            // Some field formatting
            xlsWorksheet.Cells[DATA_START_ROW, COL_DATE_INT, xlsWorksheet.Rows.EndRow, COL_DATE_INT].Style.Numberformat.Format = "mm/dd hh:mm";
            xlsWorksheet.Cells[DATA_START_ROW, COL_DATE_INT, xlsWorksheet.Rows.EndRow, COL_DATE_INT].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            xlsWorksheet.Cells[DATA_START_ROW - 1, COL_DEBIT_INT, xlsWorksheet.Rows.EndRow, COL_BALANCE_INT].Style.Numberformat.Format = CURRENCY_FORMAT;

            xlsWorksheet.Cells[DEBIT_TOTAL_CELL].Formula = $"SUM(E8:E{xlsWorksheet.Rows.EndRow})";
            xlsWorksheet.Cells[CREDIT_TOTAL_CELL].Formula = $"SUM(F8:F{xlsWorksheet.Rows.EndRow})";
            xlsWorksheet.Cells[BALANCE_TOTAL_CELL].Formula = $"{BALANCE_TOTAL_FORMULA}";

            // Account Type Dropdowns
            ExcelRange colRange = xlsWorksheet.Cells[DATA_START_ROW, COL_ACCOUNT_INT, xlsWorksheet.Rows.EndRow, COL_ACCOUNT_INT];
            var dropdown = xlsWorksheet.DataValidations.AddListValidation(colRange.Address);
            dropdown.Formula.ExcelFormula = ACCOUNT_DROPDOWN_VALUES_CELLS_FORMULA;

            // Set column widths
            xlsWorksheet.Columns[1].Width = 21.00;
            xlsWorksheet.Columns[2].Width = 18.00;
            xlsWorksheet.Columns[3].Width = 19.00;
            xlsWorksheet.Columns[4].Width = 75.00;
            xlsWorksheet.Columns[5, 8].Width = 18.00;
            xlsWorksheet.Columns[9].Width = 25;
            xlsWorksheet.Columns[10].Width = 13.57;

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

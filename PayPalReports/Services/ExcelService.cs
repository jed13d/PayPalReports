using OfficeOpenXml;
using OfficeOpenXml.Style;
using PayPalReports.DataModels.PayPalAPI;
using System.Drawing;
using System.IO;

namespace PayPalReports.Services
{
    internal class ExcelService
    {
        private readonly string PATH_TO_OUTPUT = "reportSample.xlsx";

        private readonly string TEMPLATE = "Template";

        private readonly string TITLE = "MASTER LEDGER";
        private readonly string TITLE_CELL = "D1";
        private readonly string TITLE_DATE_CELL = "D2";

        private readonly string OPENING_BALANCE_HEADER = "Opening Balance: ";
        private readonly string OPENING_BALANCE_HEADER_CELL = "A4";
        private readonly string OPENING_BALANCE_CELL = "B4";

        private readonly string[] HEADER_STRINGS = new string[] { "Date", "Reference", "Account", "Explanation", "Credit (-)", "Debit (+)", "Balance" };
        private readonly string[] DROPDOWN_TABLE_VALUES_C1 = new string[] { "Types of Accounts", "Petty Cash-Revenue", "Checking", "PayPal-Expenses", "PayPal-Revenue", "Petty Cash-Expenses" };
        private readonly string[] DROPDOWN_TABLE_VALUES_C2 = new string[] { "DR/CR", "CR", "DR", "DR", "CR", "DR" };

        private readonly string CREDIT_TOTAL_CELL = "E7";
        private readonly string CREDIT_TOTAL_FORMULA = "=SUM(E8:E)";
        private readonly string DEBIT_TOTAL_CELL = "F7";
        private readonly string DEBIT_TOTAL_FORMULA = "=SUM(F8:F)";
        private readonly string BALANCE_TOTAL_CELL = "G7";
        private readonly string BALANCE_TOTAL_FORMULA = "=B4+E7-F7";

        private readonly string CURRENCY_FORMAT = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)";
        private readonly string ACCOUNT_TYPES_ADDRESS_NAME = "AccountTypeValues";
        private readonly string[] ALPHA = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I" };

        public ExcelService()
        {
            ExcelPackage.License.SetNonCommercialOrganization("Non-commercial Organization");
            //GenerateTemplate();
            GenReportTest();
        }

        private void GenerateReport(PayPalReportDetails reportData)
        {
            //xlsWorksheet.Cells[CREDIT_TOTAL_CELL].Formula = CREDIT_TOTAL_FORMULA;
            //xlsWorksheet.Cells[DEBIT_TOTAL_CELL].Formula = DEBIT_TOTAL_FORMULA;
            //xlsWorksheet.Cells[BALANCE_TOTAL_CELL].Formula = BALANCE_TOTAL_FORMULA;
        }

        private void GenReportTest()
        {
            // Create a excel package
            using (ExcelPackage excelPackage = new ExcelPackage(new FileInfo(PATH_TO_OUTPUT)))
            {
                // Get the workbook
                ExcelWorkbook xlsWorkbook = excelPackage.Workbook;

                if (xlsWorkbook.Worksheets.Count == 0)
                {
                    GenerateTemplate(xlsWorkbook);
                }

                xlsWorkbook.Worksheets[TEMPLATE].Name = "Jan 2025";
                xlsWorkbook.Worksheets.Copy("Jan 2025", TEMPLATE);

                excelPackage.Save();
            }
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
            ApplyStyleTitle(xlsWorksheet.Cells[TITLE_DATE_CELL]);

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
            xlsWorkbook.Names.Add(ACCOUNT_TYPES_ADDRESS_NAME, xlsWorksheet.Cells["I10:I14"]);


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

    }
}

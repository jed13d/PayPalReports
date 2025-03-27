using EasyXLS;

namespace PayPalReports.Services
{
    internal class ExcelService
    {
        public ExcelService()
        {
            SampleGenerateWorkbook();
        }

        private void SampleGenerateWorkbook()
        {
            // Create an instance of the class that creates Excel files, having multiple sheets
            ExcelDocument workbook = new ExcelDocument(2);

            // Set the sheet names
            workbook.easy_getSheetAt(0).setSheetName("First tab");
            workbook.easy_getSheetAt(1).setSheetName("Second tab");

            // Create a worksheet
            workbook.easy_addWorksheet("First tab");

            // Create Excel file
            workbook.easy_WriteXLSXFile("C:\\Samples\\Excel file.xlsx");

            // Confirm the creation of the Excel file
            String sError = workbook.easy_getError();
            if (sError.Equals(""))
                Console.Write("\nFile successfully created. Press Enter to Exit...");
            else
                Console.Write("\nError encountered: " + sError + "\nPress Enter to Exit...");

            // Dispose memory
            workbook.Dispose();
        }
    }
}

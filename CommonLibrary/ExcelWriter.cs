using OfficeOpenXml;
using System;
using System.IO;

public class ExcelWriter
{
    private readonly string _filePath;
    private readonly string[] _headerRow;
    private bool IsNewFile { get; set; }

    public ExcelWriter(string filePath, string[] headerRow)
    {
        _filePath = filePath;
        _headerRow = headerRow;
        IsNewFile = false;

        // Initialize ExcelPackage.LicenseContext (required to avoid license errors)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // Create the file if it doesn't exist
        if (!File.Exists(_filePath))
        {
            CreateNewExcelFileWithHeader();
            IsNewFile = true;
        }
    }

    /// <summary>
    /// Writes a row of strings to the specified row and starting column in the Excel file.
    /// </summary>
    /// <param name="rowData">Array of strings to write.</param>
    /// <param name="startRow">The row number where the data should be written.</param>
    /// <param name="startColumn">The column number where the data should start.</param>
    public void WriteRow(string[] rowData, int startColumn, string networkFolder)
    {
        FileInfo file = new FileInfo(_filePath);

        // Load the existing Excel file or create a new one
        using (ExcelPackage package = new ExcelPackage(file))
        {
            // Get the first worksheet (or create it if it doesn't exist)
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0] ?? package.Workbook.Worksheets.Add("Sheet1");

            // Determine the next available row
            int nextRow = worksheet.Dimension?.Rows + 1 ?? 2; // If no data, write to row 2 (row 1 is for headers)

            // Write the row of data to the specified row and starting column
            for (int i = 0; i < rowData.Length; i++)
            {
                worksheet.Cells[nextRow, startColumn + i].Value = rowData[i];
                if (i == 1)
                {
                     // Set the hyperlink to the network location
                    worksheet.Cells[nextRow, startColumn + i].Hyperlink = new Uri(networkFolder);

                    // Optionally style the link (e.g., blue color and underline)
                    //worksheet.Cells[startRow, startColumn + i].Style.Font.UnderLine = true;
                    worksheet.Cells[nextRow, startColumn + i].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                }
                
            }

            if (IsNewFile) {
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            }

            // Save changes to the Excel file
            package.Save();
        }

        Console.WriteLine("Row written successfully!");
    }

     /// <summary>
    /// Creates a new Excel file with a default worksheet and writes the header row.
    /// </summary>
    private void CreateNewExcelFileWithHeader()
    {
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // Write the header row
            for (int i = 0; i < _headerRow.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = _headerRow[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            package.SaveAs(new FileInfo(_filePath));
        }
    }
}

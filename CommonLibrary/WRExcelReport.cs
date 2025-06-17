using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic;
using Range = Microsoft.Office.Interop.Excel.Range;

public class WRExcelReport
{
    private readonly string _filePath;
    private readonly string[] _headerRow;
    private bool IsNewFile { get; set; }

    public WRExcelReport(string filePath, string[] headerRow)
    {
        _filePath = filePath;
        _headerRow = headerRow;
        IsNewFile = !File.Exists(_filePath);
    }

    public void WriteRow(string[] rowData, int startColumn, string networkFolder)
    {
        CloseExcelFileIfOpen(_filePath); // ✅ Ensure the file isn't locked

        Application excelApp = null;
        Workbook workbook = null;

        try
        {
            excelApp = new Application();
            if (excelApp == null)
                throw new NullReferenceException("Failed to start Excel.");

            // Open or create the workbook
            if (!File.Exists(_filePath))
            {
                workbook = excelApp.Workbooks.Add();
                workbook.SaveAs(_filePath);
                IsNewFile = true;
            }
            else
            {
                workbook = excelApp.Workbooks.Open(_filePath);
            }

            if (workbook == null)
                throw new NullReferenceException("Failed to open workbook.");

            // Get or create the first worksheet
            Worksheet worksheet = workbook.Sheets.Count > 0 ? (Worksheet)workbook.Sheets[1] : (Worksheet)workbook.Sheets.Add();
            if (worksheet == null)
                throw new NullReferenceException("Worksheet instance is null.");

            if (IsNewFile)
            {
                worksheet.Name = "Sheet1";
                for (int i = 0; i < _headerRow.Length; i++)
                {
                    worksheet.Cells[1, i + 1] = _headerRow[i];
                    ((Range)worksheet.Cells[1, i + 1]).Font.Bold = true;
                }
            }

            // Determine the next available row safely
            int nextRow = worksheet.UsedRange.Rows.Count + 1;

            // Write the row of data
            for (int i = 0; i < rowData.Length; i++)
            {
                worksheet.Cells[nextRow, startColumn + i] = rowData[i];

                if (i == 1)  // Add hyperlink to the second cell in the row
                {
                    worksheet.Hyperlinks.Add(
                        worksheet.Cells[nextRow, startColumn + i],
                        networkFolder,
                        Type.Missing,
                        "Network Folder",
                        rowData[i]);
                    ((Range)worksheet.Cells[nextRow, startColumn + i]).Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Blue);
                }
            }

            // Auto-size all columns
            worksheet.Columns.AutoFit();

            workbook.Save();
            Console.WriteLine("Row written successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            workbook?.Close(false);
            excelApp?.Quit();
            Marshal.ReleaseComObject(excelApp);
        }
    }

    private void CloseExcelFileIfOpen(string filePath)
    {
        Process[] excelProcesses = Process.GetProcessesByName("EXCEL");

        foreach (var process in excelProcesses)
        {
            try
            {
                if (IsFileInUseByProcess(process, filePath))
                {
                    Console.WriteLine($"Closing open Excel file: {filePath}");
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to close Excel file: {ex.Message}");
            }
        }
    }

    private bool IsFileInUseByProcess(Process process, string filePath)
    {
        try
        {
            string processTitle = process.MainWindowTitle;
            return !string.IsNullOrEmpty(processTitle) && processTitle.Contains(Path.GetFileName(filePath));
        }
        catch
        {
            return false;
        }
    }
}

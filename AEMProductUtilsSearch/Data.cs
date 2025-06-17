using DocumentFormat.OpenXml.Wordprocessing;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AEMProductUtilsSearch
{
    class Data
    {
        private List<FormData> _data = new();
        private string _excelFile { get; set; }
        public Data (string excelFile)
        {
            _excelFile = excelFile;
        }

        public void AddFormData(string form, string folder, string subForm, string search, string field, string type, string method)
        {
            // Check if the form entry already exists
            var existingForm = _data.FirstOrDefault(f => f.Form == form && f.Folder == folder);

            if (existingForm == null)
            {
                // Create new FormData and add it
                var newForm = new FormData
                {
                    Form = form,
                    Folder = folder,
                    Fields = new List<FieldData> { new FieldData { SubForm = subForm, Search = search, Field = field, Type = type, Method = method } }
                };
                _data.Add(newForm);
            }
            else
            {
                // Just add the field to the existing form
                existingForm.Fields.Add(new FieldData { SubForm = subForm, Search = search, Field = field, Type = type, Method = method });
            }
        }

        public List<FormData> GetData()
        {
            return _data;
        }

        public void WriteToExcel(string worksheetName = "Sheet1")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Required for EPPlus
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(worksheetName);

                // Headers
                worksheet.Cells[1, 1].Value = "Form";
                worksheet.Cells[1, 2].Value = "Folder";
                worksheet.Cells[1, 3].Value = "Search String";
                worksheet.Cells[1, 4].Value = "Subform/Page";
                worksheet.Cells[1, 5].Value = "Containers";
                worksheet.Cells[1, 6].Value = "Search Area";
                worksheet.Cells[1, 7].Value = "Results";

                int row = 2; // Start data from row 2

                foreach (var fileData in _data)
                {
                    string form = fileData.Form;
                    string folder = fileData.Folder;
                    var elements = fileData.Fields;

                    int startRow = row; // Keep track of where each section starts

                    foreach (var element in elements)
                    {
                        worksheet.Cells[row, 3].Value = element.Search;
                        worksheet.Cells[row, 4].Value = element.SubForm;
                        worksheet.Cells[row, 5].Value = element.Field.Trim();
                        worksheet.Cells[row, 6].Value = element.Type;

                        string method = element.Method.Trim();
                        string search = element.Search;
                        var cell = worksheet.Cells[row, 7];

                        if (!string.IsNullOrEmpty(search) && method.Contains(search, StringComparison.OrdinalIgnoreCase))
                        {
                            int matchIndex = method.IndexOf(search, StringComparison.OrdinalIgnoreCase);

                            // Add pre-match text
                            if (matchIndex > 0)
                            {
                                var pre = cell.RichText.Add(method.Substring(0, matchIndex));

                                pre.Color = System.Drawing.Color.FromArgb(0, 0, 0); // Black
                                
                            }

                            // Add matched (highlighted) text
                            var match = cell.RichText.Add(method.Substring(matchIndex, search.Length));
                            match.Bold = true;
                            match.Color = System.Drawing.Color.FromArgb(255, 0, 0); // DarkRed equivalent in RGB
                            //match.Color = Color.DarkRed;

                            // Add post-match text
                            int end = matchIndex + search.Length;
                            if (end < method.Length)
                            {
                                var post = cell.RichText.Add(method.Substring(end));
                                post.Color = System.Drawing.Color.FromArgb(0, 0, 0); // Black
                                post.Bold = false;
                            }
                        }
                        else
                        {
                            // No match — just plain text
                            cell.Value = method;
                        }

                        // Set vertical alignment
                        for (int col = 3; col <= 7; col++)
                        {
                            worksheet.Cells[row, col].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                        }
                        row++;
                    }

                    int endRow = row - 1; // Last row for this file

                    // Merge Form column if there are multiple rows
                    if (startRow < endRow)
                    {
                        worksheet.Cells[startRow, 1, endRow, 1].Merge = true;
                        worksheet.Cells[startRow, 1].Value = form;
                        worksheet.Cells[startRow, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                    }
                    else
                    {
                        worksheet.Cells[startRow, 1].Value = form;
                    }

                    // Merge Folder column if there are multiple rows
                    if (startRow < endRow)
                    {
                        worksheet.Cells[startRow, 2, endRow, 2].Merge = true;
                        worksheet.Cells[startRow, 2].Value = folder;
                        worksheet.Cells[startRow, 2].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Top;
                    }
                    else
                    {
                        worksheet.Cells[startRow, 2].Value = folder;
                    }

                }

                // AutoFit columns
                worksheet.Cells.AutoFitColumns();

                worksheet.Column(5).Width = 25; // Form
                worksheet.Column(5).Style.WrapText = true;

                worksheet.Column(7).Width = 100; // Form
                worksheet.Column(7).Style.WrapText = true;

                // Bold the headings
                using (var range = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                {
                    range.Style.Font.Bold = true;
                }

                // Add filters
                worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column].AutoFilter = true;

                // Freeze panes
                worksheet.View.FreezePanes(2,3);

                // Save Excel File
                File.WriteAllBytes(_excelFile, package.GetAsByteArray());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;

namespace CommonLibrary
{
    public class ExcelReader<T> where T : new()
    {
        private readonly string _filePath;
        private readonly Dictionary<string, string> _dataDictionary;
        private readonly int _startingRow;

        public ExcelReader(string filePath, Dictionary<string, string> dataDictionary, int startingRow)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _dataDictionary = dataDictionary ?? throw new ArgumentNullException(nameof(dataDictionary));
            _startingRow = startingRow;
        }

        public List<T> ReadRecords()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException($"Excel file not found: {_filePath}");

            var records = new List<T>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(_filePath));
            File.Copy(_filePath, tempFilePath, true); // Overwrites if exists

            using (var package = new ExcelPackage(new FileInfo(tempFilePath)))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new Exception("No worksheet found in the Excel file.");

                foreach (var kvp in _dataDictionary)
                {
                    string colLetter = kvp.Key;  // e.g., "A"
                    string propertyName = kvp.Value;

                    int colIndex = ExcelColumnToIndex(colLetter);
                    if (colIndex == -1) continue;

                    for (int row = _startingRow; row <= worksheet.Dimension.Rows; row++)
                    {
                        var record = records.ElementAtOrDefault(row) ?? new T();
                        var cellValue = worksheet.Cells[row, colIndex].Text.Trim();
                        SetProperty(record, propertyName, cellValue);

                        if (row - 2 >= records.Count) records.Add(record);
                    }
                }
            }

            // Optionally delete the temp file after use
            File.Delete(tempFilePath);

            return records;
        }

        private int ExcelColumnToIndex(string colLetter)
        {
            int colIndex = 0;
            foreach (char c in colLetter.ToUpper())
            {
                if (c < 'A' || c > 'Z') return -1;
                colIndex = colIndex * 26 + (c - 'A' + 1);
            }
            return colIndex;
        }

        private void SetProperty(T record, string propertyName, string value)
        {
            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                object convertedValue = Convert.ChangeType(value, prop.PropertyType);
                prop.SetValue(record, convertedValue);
            }
        }
    }


}

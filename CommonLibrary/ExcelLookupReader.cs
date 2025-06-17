using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ClosedXML.Excel;

namespace CommonLibrary
{
    public class ExcelLookupReader
    {
        public List<T> ReadLookup<T>(string filePath, string sheetName = null) where T : LookupBase, new()
        {
            var result = new List<T>();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = string.IsNullOrEmpty(sheetName) ? workbook.Worksheets.First() : workbook.Worksheet(sheetName);
                var headers = worksheet.Row(1).Cells().Select(c => c.Value.ToString()).ToList();

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var item = new T();
                    var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var prop in props)
                    {
                        var attr = prop.GetCustomAttribute<ExcelColumnAttribute>();
                        var columnName = attr?.ColumnName ?? prop.Name;
                        var colIndex = headers.FindIndex(h => h.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                        if (colIndex >= 0)
                        {
                            try
                            {
                                var cell = row.Cell(colIndex + 1);

                                if (!cell.IsEmpty() && prop.CanWrite)
                                {
                                    var stringValue = cell.GetValue<string>().Trim();
                                    if (!string.IsNullOrWhiteSpace(stringValue))
                                    {
                                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                                        try
                                        {
                                            var safeValue = Convert.ChangeType(stringValue, targetType);
                                            prop.SetValue(item, safeValue);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Warning: Failed to convert value '{stringValue}' for property '{prop.Name}' - {ex.Message}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Failed to process cell for property '{prop.Name}' - {ex.Message}");
                            }
                        }

                    }

                    result.Add(item);
                }

                foreach (var prop in typeof(T).GetProperties())
                {
                    var normalizeAttr = prop.GetCustomAttribute<NormalizeZerosAttribute>();
                    if (normalizeAttr != null)
                    {
                        foreach (var item in result)
                        {
                            var value = prop.GetValue(item)?.ToString()?.TrimStart('0') ?? "";
                            var padded = value.PadLeft(normalizeAttr.TotalLength, '0');
                            prop.SetValue(item, padded);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Excel file: {ex.Message}");
            }

            return result;
        }
    }

}



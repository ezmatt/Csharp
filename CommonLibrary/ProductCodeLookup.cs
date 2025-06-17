using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public class ProductCodes
    {
        public string oldProductCode { get; set; }
        public NewProductCodes newProductCode { get; set; }
    }

    public class NewProductCodes
    {
        public string HospitalCode { get; set; }
        public string ExtrasCode { get; set; }
        public string ExcessAmount { get; set; }
    }

    public class ProductCodeLookup
    {
        private readonly string _filePath;
        private string _excelFileName { get; set; }
        private string _excelOldFileName { get; set; }

        public Dictionary<string, ProductCodeMappings> LookupTable = [];
        public Dictionary<string, string> OldLookupTable = [];

        public Dictionary<string, List<string>> HospitalLookup = [];
        public Dictionary<string, ProductCodes> ProductDescriptionLookup = [];
        
        List<string> _foundHospitalCodes = [];
        List<string> _foundProductCodes = [];
        List<string> _unmatchedCodes = [];
        List<string> _matchedCodes = [];
        List<string> _foundOldHospitalCodes = [];
        List<string> _foundOldProductCodes = []; 

        public ProductCodeLookup(string filePath, string excelFileName, string excelOldFileName)
        {
            _filePath = filePath;
            _excelFileName = excelFileName;
            _excelOldFileName = excelOldFileName;
            
            LoadOldLookupTable();
            LoadLookupTable();
        }

        private void LoadOldLookupTable()
        {
            using (var workbook = new XLWorkbook(_filePath + _excelOldFileName))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();

                foreach (var row in rows.Skip(1)) // Skipping header row
                {
                    ProductCodeMappings mappings = new ProductCodeMappings();
                    mappings.Fund = row.Cell(1).GetString().Trim();
                    mappings.ProductCode = row.Cell(2).GetString().Trim();
                    mappings.ProductName = row.Cell(3).GetString().Trim();
                    mappings.ExcessAmount = row.Cell(4).GetString().Trim();
                    mappings.HospitalCode = row.Cell(10).GetString().Trim();
                    mappings.Status = row.Cell(11).GetString().Trim();
                    mappings.ProductType = row.Cell(15).GetString().Trim();


                    // Store each row in the lookup referenced by the old Product Code
                    if (!string.IsNullOrEmpty(mappings.ProductCode))
                    {
                        if (string.IsNullOrEmpty(mappings.HospitalCode)) continue;
                        OldLookupTable[mappings.ProductCode] = mappings.HospitalCode;
                    }
                }
             }
        }

        private void LoadLookupTable()
        {
         
            using (var workbook = new XLWorkbook(_filePath + _excelFileName))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();

                foreach (var row in rows.Skip(1)) // Skipping header row
                {
                    ProductCodeMappings mappings = new ProductCodeMappings();
                    mappings.Fund           = row.Cell(1).GetString().Trim();
                    mappings.ProductCode    = row.Cell(2).GetString().Trim();
                    mappings.ProductName    = row.Cell(3).GetString().Trim();
                    mappings.ExcessAmount   = row.Cell(4).GetString().Trim();
                    mappings.HospitalCode   = row.Cell(5).GetString().Trim();
                    mappings.ExtrasCode     = row.Cell(6).GetString().Trim();
                    mappings.Status         = row.Cell(7).GetString().Trim();
                    mappings.ProductType    = row.Cell(8).GetString().Trim();


                    // Store each row in the lookup referenced by the old Product Code
                    if (!string.IsNullOrEmpty(mappings.ProductCode))
                    {
                        LookupTable[mappings.ProductCode] = mappings;
                    }

                    // Match the description to an old product code and new product codes
                    if (!string.IsNullOrEmpty(mappings.ProductName))
                    {
                        var description = new ProductCodes
                        {
                            oldProductCode = mappings.ProductCode,
                            newProductCode = new NewProductCodes {
                                HospitalCode = mappings.HospitalCode,
                                ExtrasCode = mappings.ExtrasCode,
                                ExcessAmount = mappings.ExcessAmount,
                            },
                        };

                        ProductDescriptionLookup[mappings.ProductName] = description;
                    }

                    // Use the first letter of the old Product code to create a list
                    // of corresponding Hospital Codes
                    if (!string.IsNullOrEmpty(mappings.HospitalCode))
                    {
                        char firstChar = mappings.ProductCode[0];
                        // Extract the first character (old hospital code)
                        string oldHospital = mappings.ProductCode[0].ToString();
                        
                        if (!HospitalLookup.ContainsKey(oldHospital))
                        {
                            HospitalLookup[oldHospital] = new List<string>(); // Create new entry if key doesn't exist
                        }
                        // Only add the code if it is unique.
                        if (!HospitalLookup[oldHospital].Contains(mappings.HospitalCode))
                        {
                            HospitalLookup[oldHospital].Add(mappings.HospitalCode);
                        }
                    }
                }
                
                // Get the ceased codes as well
                worksheet = workbook.Worksheet(6);
                rows = worksheet.RangeUsed().RowsUsed();

                foreach (var row in rows.Skip(1)) // Skipping header row
                {
                    ProductCodeMappings mappings = new ProductCodeMappings();
                    mappings.Fund           = row.Cell(1).GetString().Trim();
                    mappings.ProductCode    = row.Cell(2).GetString().Trim();
                    mappings.ProductName    = row.Cell(3).GetString().Trim();
                    mappings.ExcessAmount   = row.Cell(4).GetString().Trim();
                    mappings.Status         = row.Cell(6).GetString().Trim();
                    mappings.ProductType    = row.Cell(7).GetString().Trim();


                    // Store each row in the lookup referenced by the old Product Code
                    if (!string.IsNullOrEmpty(mappings.ProductCode))
                    {
                        LookupTable[mappings.ProductCode] = mappings;
                    }

                    // Match the description to an old product code and new product codes
                    if (!string.IsNullOrEmpty(mappings.ProductName))
                    {
                        var description = new ProductCodes
                        {
                            oldProductCode = mappings.ProductCode,
                            newProductCode = new NewProductCodes
                            {
                                HospitalCode = "",
                                ExtrasCode = "",
                                ExcessAmount = mappings.ExcessAmount,
                            },
                        };

                        ProductDescriptionLookup[mappings.ProductName] = description;
                    }
                                        
                }
            }
        }

        public void GetNewHospitalCodes(string[] oldHospitalCodes)
        {
            _foundHospitalCodes = [];
            _foundOldHospitalCodes = [];
            _foundProductCodes = [];
            _foundOldProductCodes = [];
            _unmatchedCodes = [];
            _matchedCodes = [];
            foreach (var hospitalCode in oldHospitalCodes)
            {

                if (HospitalLookup.ContainsKey(hospitalCode))
                {
                    _matchedCodes.Add(hospitalCode);
                    foreach (string newHospitalCode in HospitalLookup[hospitalCode])
                    {
                        _foundHospitalCodes.Add(newHospitalCode);
                    }
                }
                else
                {
                    _unmatchedCodes.Add(hospitalCode);
                }

            }

        }

        public void GetNewProductCodes(string[] productCodes, bool andExtras = false)
        {
            _foundHospitalCodes = [];
            _foundOldHospitalCodes = [];
            _foundProductCodes = [];
            _unmatchedCodes = [];
            _matchedCodes = [];
            _foundOldProductCodes = [];
            foreach (var code in productCodes)
            {
                string oldCode = code;
                if (OldLookupTable.TryGetValue(code, out var updatedOldProductCode))
                {
                    _foundOldProductCodes.Add(oldCode + " -> " + updatedOldProductCode);
                    oldCode = updatedOldProductCode;
                }
                
                if (LookupTable.TryGetValue(oldCode, out var values))
                {
                    string hospitalCode = !string.IsNullOrEmpty(values.HospitalCode) ? values.HospitalCode : "NNN";
                    string extrasCode = !string.IsNullOrEmpty(values.ExtrasCode) ? values.ExtrasCode : "NNN";
                    string coverCode = (andExtras) ? hospitalCode + " " + extrasCode : hospitalCode;
                    char firstChar = oldCode[0];
                    // Extract the first character (old hospital code)
                    string oldHospitalCode = firstChar.ToString();
                    
                    _matchedCodes.Add(code);
                    if (!_foundOldHospitalCodes.Contains(oldHospitalCode))
                    {
                        _foundOldHospitalCodes.Add(oldHospitalCode);
                    }
                    if (!_foundHospitalCodes.Contains(hospitalCode)) { 
                        _foundHospitalCodes.Add(hospitalCode);
                    }
                    if (!_foundProductCodes.Contains(coverCode))
                    {
                        _foundProductCodes.Add(oldCode + " -> " +coverCode);
                    }
                }
                else
                {
                    _unmatchedCodes.Add(code);
                }
            }

            
        }

        public void GetProductDescriptionFromOldProductCodes (string[] productCodes)
        {
            _foundHospitalCodes = [];
            _foundOldHospitalCodes = [];
            _foundProductCodes = [];
            _unmatchedCodes = [];
            _matchedCodes = [];
            foreach (var code in productCodes)
            {
                if (LookupTable.TryGetValue(code, out var values))
                {
                    _matchedCodes.Add(code);
                    string coverDescription = !string.IsNullOrEmpty(values.ProductName) ? values.ProductName : "";
                    _foundHospitalCodes.Add(coverDescription);
                }
                else
                {
                    _unmatchedCodes.Add(code);
                }
            }


        }

        public void GetCodesWithHospitalDescriptions (string[] productDescriptions, bool andExtras = false)
        {
            _foundHospitalCodes = [];
            _foundOldHospitalCodes = [];
            _foundProductCodes = [];
            _unmatchedCodes = [];
            _matchedCodes = [];
            foreach (var code in productDescriptions)
            {
                if (ProductDescriptionLookup.TryGetValue(code, out var values))
                {
                    _matchedCodes.Add(code);
                    string oldProductCode = !string.IsNullOrEmpty(values.oldProductCode) ? values.oldProductCode : "";
                    _foundOldHospitalCodes.Add(oldProductCode);

                    string hospitalCode = !string.IsNullOrEmpty(values.newProductCode.HospitalCode) ? values.newProductCode.HospitalCode : "NNN";
                    string extrasCode = !string.IsNullOrEmpty(values.newProductCode.ExtrasCode) ? values.newProductCode.ExtrasCode : "NNN";
                    string coverCode = (andExtras) ? hospitalCode + " " + extrasCode : hospitalCode;
                    if (!_foundProductCodes.Contains(coverCode))
                    {
                        _foundProductCodes.Add(coverCode);
                    }
                }
                else
                {
                    _unmatchedCodes.Add(code);
                }
            }


        }

        public void WriteListtoFile(string fileName)
        {
            // Create the formatted output
            string output =
                $"Matched Codes = \n  \"{string.Join("\",\n  \"", _matchedCodes)}\"\n\n" +
                $"Unmatched Codes = \n  \"{string.Join("\",\n  \"", _unmatchedCodes)}\"\n\n" +
                $"Old Hospital Code = \n  \"{string.Join("\",\n  \"", _foundOldHospitalCodes)}\"\n\n" +
                $"New Hospital Code = \n  \"{string.Join("\",\n  \"", _foundHospitalCodes)}\"\n\n" +
                $"Updated Old Product Codes = \n  \"{string.Join("\",\n  \"", _foundOldProductCodes)}\"\n\n" +
                $"Product Expansion Product Codes = \n  \"{string.Join("\",\n  \"", _foundProductCodes)}\"";
                
            // Save output to a text file
            File.WriteAllText(_filePath + fileName, output);
        }
    }
}

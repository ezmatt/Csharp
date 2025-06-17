using OfficeOpenXml;

public class ExcelHandle {
    
    public string FilePath { get; set; }
    public string WorksheetName { get; set; }
    public string DataModel { get; set; }
    public List<FieldDefinition> DataDictionary { get; set; }
    public ExcelHandle(string filePath, string worksheetName, string dataModel)
    {
        FilePath = filePath;
        WorksheetName = worksheetName;
        DataModel = dataModel;
        DataDictionary = [];
    }

    public bool LoadExcelDataDictionary()
    {
        FileInfo fileInfo = new FileInfo(FilePath);
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[WorksheetName];

            if (worksheet.Dimension == null)
            {
                Console.WriteLine("Empty worksheet.");
                return false;
            }

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
            {
                // Order of the data dictionary columns is different for some ungoldy reason
                if ( DataModel == "AHM") 
                {
                    var fieldDefinition = new FieldDefinition
                    {
                        FieldName = worksheet.Cells[row, 1].Text,
                        Type = worksheet.Cells[row, 2].Text,
                        IsMandatory = worksheet.Cells[row, 3].Text.StartsWith("Y"),
                        PossibleValues = string.IsNullOrEmpty(worksheet.Cells[row, 4].Text) ? null : new List<string>(worksheet.Cells[row, 4].Text.Split(',')),
                        Comments = worksheet.Cells[row, 5].Text,
                        DataExample = worksheet.Cells[row, 6].Text
                    };

                    DataDictionary.Add(fieldDefinition);
                }
                else if ( DataModel == "MPL") 
                {
                    var fieldDefinition = new FieldDefinition
                    {
                        FieldName = worksheet.Cells[row, 2].Text,
                        Type = worksheet.Cells[row, 3].Text,
                        IsMandatory = worksheet.Cells[row, 4].Text.StartsWith("Man"),
                        PossibleValues = string.IsNullOrEmpty(worksheet.Cells[row, 5].Text) ? null : new List<string>(worksheet.Cells[row, 5].Text.Split(',')),
                        Comments = "",
                        DataExample = "",
                    };

                    DataDictionary.Add(fieldDefinition);
                }
                else if ( DataModel == "ALL") 
                {
                    var fieldDefinition = new FieldDefinition
                    {
                        FieldName = worksheet.Cells[row, 1].Text,
                        Type = "",
                        IsMandatory = false,
                        PossibleValues = string.IsNullOrEmpty(worksheet.Cells[row, 2].Text) ? null : new List<string>(worksheet.Cells[row, 2].Text.Split('|')),
                        Comments = worksheet.Cells[row, 4].Text,
                        DataExample = worksheet.Cells[row, 3].Text,
                    };

                    DataDictionary.Add(fieldDefinition);
                }
                else if ( DataModel == "COM") 
                {
                    var fieldDefinition = new FieldDefinition
                    {
                        FieldName = worksheet.Cells[row, 1].Text,
                        Type = worksheet.Cells[row, 5].Text, //Combination key
                        IsMandatory = false,
                        PossibleValues = string.IsNullOrEmpty(worksheet.Cells[row, 2].Text) ? null : new List<string>(worksheet.Cells[row, 2].Text.Split('|')),
                        Comments = worksheet.Cells[row, 4].Text,
                        DataExample = worksheet.Cells[row, 3].Text,
                    };

                    DataDictionary.Add(fieldDefinition);
                }
                else {
                    return false;
                }
                
            }
        }

        return true;
    }

}
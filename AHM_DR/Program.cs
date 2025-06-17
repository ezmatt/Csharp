using OfficeOpenXml;
using System.Globalization;
using System.Text.RegularExpressions;

class Program
{

    static void Main(string[] args)
    {
        // Step 1: Load Excel-based data dictionary
        string filepath = (@"\\mplfiler\Groups\Operational Delivery\Fulfilment\1. Team\Process\PO details\PO Data Formats\PO Standard Data Formats_V5_28OCT24.xlsx");
        
        // Standard ahm
        // AHM 40 data dictionary
        List<FieldDefinition> dataDictionaryAHM40 = LoadExcelDataDictionary(filepath, "ahm Standard 40 adhoc", "AHM");
        // Standard AHM data dictionary
        List<FieldDefinition> dataDictionaryAHM = LoadExcelDataDictionary(filepath, "ahm Standard", "AHM");
        
        // Standard MPL
        // PHI data dictionary - MPL 40
        List<FieldDefinition> dataDictionaryMPLPHI = LoadExcelDataDictionary(filepath, "MPL Standard 40 adhoc", "MPL");
        // PRV data dictionary - Providers
        List<FieldDefinition> dataDictionaryMPLPRV = LoadExcelDataDictionary(filepath, "MPL Provider", "MPL");
        // ASD data dictionary - Activity Statement
        List<FieldDefinition> dataDictionaryMPLASD = LoadExcelDataDictionary(filepath, "MPL Provider", "MPL");
        // Everything else - MPL Standard 10
        List<FieldDefinition> dataDictionaryMPL10 = LoadExcelDataDictionary(filepath, "MPL Standard 10 adhoc", "MPL");


        // Step 2: Load and process the tab-delimited file
        string path =(@"\\mplfiler\Groups\Operational Delivery\Fulfilment\2. ahm\1. ahm Work Requests\AWR0304 - Limit Rollover 2025\5. Test Samples\");

        string[] txtFiles = [];
        
        try {
            txtFiles = Directory.GetFiles(path, "*.txt");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Exception: {path} does not exist");
        }
        
        string logFilePath = path+"validation_log.txt"; // Log file path
        using (StreamWriter writer = new StreamWriter(logFilePath, false)) // Append to the log file
        {
            
            foreach (string filePath in txtFiles)
            {
                
                // Create Logfile object
                List<LogFile> logFile = new();

                // Validate File name.
                var file = Regex.Match(filePath, @".*\\(.+?)$");
                string filename = file.Groups[1].Value;
                Console.WriteLine($"Validating File: {filename}");
                
                // Validate file naming convention
                var match = Regex.Match(filename, @"(?i)^(.*?)_(FILE40_|POST_|EMAIL_)?(.*)?_(LIVE|TEST)_(\d\d\D\D\D\d\d)\.txt$");
                if (!match.Success) 
                {
                    LogValidation("NAMING CONVENTION", ["File does not match naming convention."], logFile, 0);

                    // Check for stuff after the date, or 
                    match = Regex.Match(filename, @"(?i)^(.*?)_(.*?_)?(.*?)_(LIVE|TEST)_(\d\d\D\D\D\d\d)(.*)?$");
                    if (match.Success) {
                        LogValidation("NAMING CONVENTION", [$"End of file has additional/incorrect characters: '{match.Groups[6]}' after the date, or is not a 'txt' file.", "    Should be _ddMMMyy.txt, eg. _12MAR24.txt, _30MAY78.txt"], logFile, 0);
                    }
                    else {
                        //Date format is wrong
                        match = Regex.Match(filename, @"(?i)^(.*?)_(.*?_)?(.*?)_(LIVE|TEST)_(.*)$");
                        if (match.Success) {
                            LogValidation("NAMING CONVENTION", [$"Date Format: '{match.Groups[5]}' is incorrect.", "    Should be ddMMMyy.txt, eg. 12MAR24, 30MAY78."], logFile, 0);
                        }
                        else {
                            //Date format is wrong
                            match = Regex.Match(filename, @"(?i)^(.*?)_(.*?_)?(.*?)_(.*?)_(.*)$");
                            if (match.Success) {
                                LogValidation("NAMING CONVENTION", [$"Processing Type: '{match.Groups[4]}' is not valid.", "    Can only be one of LIVE or TEST"], logFile, 0);
                            }
                        }
                    }
                }
                // Description cannot contain underscores.
                else if (Regex.Match(match.Groups[3].Value, @"_").Success ) {
                    LogValidation("NAMING CONVENTION", [$"Correspondence Description cannot contain underscores - '_'.", $"     '{match.Groups[3]}' is not valid.", "    eg. MPLOOCFILE"], logFile, 0);
                }
                else {
                    // Theoretically the file is good.
                }
                

                // Check to see if file is a test file
                bool test = true;
                if ( match.Groups[4] != null && match.Groups[4].Value.ToUpperInvariant() != "TEST" ) {test = false;}

                // Based on the file naming convention, we can determine if the file is a file40, standard AHM, or M2100.
                // M2100 files have 4 different formats
                if (Regex.Match(match.Groups[1].Value, @"^(M2100).*$").Success)
                {

                    // M2100PHI - OOC and Closures
                    if (Regex.Match(match.Groups[1].Value, @"^(M2100PHI).*$").Success) { 
                        try
                        {
                            ProcessTabDelimitedFile(filePath, dataDictionaryMPLPHI, logFile, test);
                        }
                        catch (Exception ex)
                        {
                            LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Standard 40 (M1200PHI)\" based on file name but uses a different data structure.", ex.Message], logFile, 0);
                        }
                    }
                    // M2100PRV - Providers
                    else if (Regex.Match(match.Groups[1].Value, @"^(M2100PRV).*$").Success)
                    {
                        try
                        {
                            ProcessTabDelimitedFile(filePath, dataDictionaryMPLPRV, logFile, test);
                        }
                        catch (Exception ex)
                        {
                            LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Provider (M1200PRV)\" based on file name but uses a different data structure.", ex.Message], logFile, 0);
                        }
                    }
                    // M2100ASD - Activity Statements
                    else if (Regex.Match(match.Groups[1].Value, @"^(M2100ASD).*$").Success)
                    {
                        try
                        {
                            ProcessTabDelimitedFile(filePath, dataDictionaryMPLPRV, logFile, test);
                        }
                        catch (Exception ex)
                        {
                            LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Activity Statements (M1200ASD)\" based on file name but uses a different data structure.", ex.Message], logFile, 0);
                        }
                    }
                    // All other M2100 files use the MPL Standard 10 adhoc structure
                    else
                    {
                        try
                        {
                            ProcessTabDelimitedFile(filePath, dataDictionaryMPL10, logFile, test);
                        }
                        catch (Exception ex)
                        {
                            LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Standard 10 (M1200)\" based on file name but uses a different data structure.", ex.Message], logFile, 0);
                        }
                    }

                }
                // AHM files
                else if ( Regex.Match(match.Groups[1].Value, @"^AHM.*$").Success) 
                {
                    // AHM40 format
                    if (match.Groups[2].Value == "FILE40_" ) {
                        try {
                            ProcessTabDelimitedFile(filePath, dataDictionaryAHM40, logFile, test);
                        }
                        catch (Exception ex)
                        {
                            LogValidation("MAJOR EXCEPTION", ["File structure is \"AHM file 40\" based on file name but uses a different data structure.", ex.Message], logFile, 0);
                        }
                        
                    }
                    // AHM standard format.
                    else 
                    {    
                        try {
                            ProcessTabDelimitedFile(filePath, dataDictionaryAHM, logFile, test);
                        }
                        catch (Exception ex)
                        {
                            LogValidation("MAJOR EXCEPTION", ["File structure is \"AHM Standard\" based on file name but uses a different data structure.", ex.Message], logFile, 0);
                        }
                        
                    }
                }
                // Something else we don't currently cater for.
                else {
                    LogValidation("MAJOR EXCEPTION", ["No data structure detected for this file."], logFile, 0);
                }
                
                
                // Step 3. Write validation errors to log if any.
                writer.WriteLine($"File: {file}");
                WriteLog(writer, logFile);
                
            }
        }

        System.Console.WriteLine("Press any key to close...");
        Console.ReadLine();
    }

    // Structure representing each field definition from the Excel data dictionary
    public class FieldDefinition
    {
        public string FieldName { get; set; }
        public string Type { get; set; }
        public bool IsMandatory { get; set; }
        public List<string> PossibleValues { get; set; }
        public string Comments { get; set; }
        public string DataExample { get; set; }
    }
    
    public class LogFile
    {
        public string logType {get; set;}
        public List<string> message {get; set;}
        public int lineNumber {get; set;}

    }

    static List<FieldDefinition> LoadExcelDataDictionary(string filePath, string worksheetName, string fieldOrderType)
    {
        var dataDictionary = new List<FieldDefinition>();

        FileInfo fileInfo = new FileInfo(filePath);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[worksheetName];

            if (worksheet.Dimension == null)
            {
                Console.WriteLine("Empty worksheet.");
                return dataDictionary;
            }

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
            {
                // Order of the data dictionary columns is different for some ungoldy reason
                if ( fieldOrderType == "AHM") 
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

                    dataDictionary.Add(fieldDefinition);
                }
                else if ( fieldOrderType == "MPL") 
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

                    dataDictionary.Add(fieldDefinition);
                }
                else {
                    throw new InvalidOperationException($"The data dictionary type \"{fieldOrderType}\" is not currently catered for.");
                }
                
            }
        }

        return dataDictionary;
    }

    // Method to process the tab-delimited file and validate/enrich it using the Excel-based data dictionary
    static void ProcessTabDelimitedFile(string file, List<FieldDefinition> dataDictionary, List<LogFile> logFile, bool test)
    {
        
        int lineNo = 2;
        using (StreamReader reader = new StreamReader(file))
        {
            string headerLine = reader.ReadLine(); // Read header row of the tab-delimited file
            string[] tabHeaders = headerLine.Split('\t'); // Split headers by tab

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] columns = line.Split('\t'); // Split the line into tab-separated columns

                // Validate channel... 
                switch (columns[30])
                {
                    case "EMAIL":
                        dataDictionary[14].IsMandatory = true;
                        dataDictionary[22].IsMandatory = false;
                        break;

                    case "SMS":
                        dataDictionary[14].IsMandatory = false;
                        dataDictionary[22].IsMandatory = true;
                        break;

                    default:
                        dataDictionary[14].IsMandatory = false;
                        dataDictionary[22].IsMandatory = false;
                        break;
                }

                for (int i = 0; i < columns.Length; i++)
                {
                    string fieldValue = columns[i];

                    
                    // Get the corresponding field definition based on the order
                    FieldDefinition fieldDefinition = dataDictionary[i];

                    // Weird issue with Double quotes around everything - JF
                    if (fieldValue.Contains("\""))
                    {
                        LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' contains \" (Double Quote) characters. ", $"Value: {fieldValue}"], logFile, lineNo);
                    }

                    // Validate mandatory fields
                    if (fieldDefinition.IsMandatory && string.IsNullOrEmpty(fieldValue))
                    {
                         LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' is mandatory but missing."], logFile, lineNo);
                    }
                    
                    // Validate type
                    ValidateFieldType(fieldDefinition, fieldValue, logFile, lineNo);
                    
                    // Other Validations

                }
                
                //Console.WriteLine(); // Blank line between rows
                
                // Increment file line number
                lineNo++;
            }

            if (test && lineNo > 61 ) { LogValidation("RECORDS", [$"File is marked as test but contains more than 60 records."], logFile, lineNo); }
        }
    }

    // Method to validate the field based on its type
    static void ValidateFieldType(FieldDefinition fieldDefinition, string fieldValue, List<LogFile> logFile, int lineNo)
    {
        // Skip validation if value is empty
        if (string.IsNullOrEmpty(fieldValue)) return;

        // Numbers
        if ( Regex.Match(fieldDefinition.Type, @"^(?i)num.*$").Success) 
        {
            if (!double.TryParse(fieldValue, out _))
                {
                    LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' should be a valid number, but got '{fieldValue}'"], logFile, lineNo);
                }
        }
        // Strings
        else if ( Regex.Match(fieldDefinition.Type, @"^(?i)string.*$").Success) 
        {
            // String needs no validation but it is a valid type. Could possiblt do some further validation here.
        }
        // Dates
        else if ( Regex.Match(fieldDefinition.Type, @"^(?i)date.*$").Success) 
        {
            if (!DateTime.TryParseExact(fieldValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' should be in the format 'yyyy-MM-dd', but got '{fieldValue}'"], logFile, lineNo);
                }
        }
        else {    
            LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' has an unsupported type: '{fieldDefinition.Type}'"], logFile, lineNo);
        }
    }

    // Method to log validation messages to a file
    static void LogValidation(string type, List<string> message, List<LogFile> logFile, int fileLineNo)
    {
        //writer.WriteLine($"Line {fileLineNo}: {message}");
        var log = new LogFile
        {
            logType = type,
            message = message,
            lineNumber = fileLineNo,
        };

        /// Channel checking, ie. need email if channel = EMAIL

        logFile.Add(log);
        Console.WriteLine($"Line: {log.lineNumber}");
        Console.WriteLine("{0}", string.Join(" - ", log.message));

    }

    static void WriteLog(StreamWriter writer, List<LogFile> logFile)
    {

        if ( logFile.Count != 0)
        {
            // Major errors first.
            // - Probably a better way to do this... but alas
            writer.WriteLine($"MAJOR ERRORS:");
            foreach (LogFile log in logFile) 
            {
                if (log.logType != "MAJOR EXCEPTION" ) {
                    continue;
                }
                foreach (string messageLine in log.message) {
                    writer.WriteLine($" - {messageLine}");
                }
            }
            
            // Naming convention errors first.
            // - Probably a better way to do this... but alas
            writer.WriteLine($"NAMING CONVENTION ERRORS:");
            foreach (LogFile log in logFile) 
            {
                if (log.logType != "NAMING CONVENTION" ) {
                    continue;
                }
                foreach (string messageLine in log.message) {
                    writer.WriteLine($" - {messageLine}");
                }
            }
            
            // Then field errors.
            writer.WriteLine($"FIELD ERRORS:");
            foreach (LogFile log in logFile) 
            {
                if (log.logType != "FIELD" ) {
                    continue;
                }
                writer.WriteLine($"Line: {log.lineNumber}");
                foreach (string messageLine in log.message) {
                    writer.WriteLine($" - {messageLine}");
                }
            }
        }
        else {
            writer.WriteLine($"*** FILE IS SWEET! ***");
        }
         writer.WriteLine("");
    }
     
}
using OfficeOpenXml;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Windows.Forms;



namespace DataValidation
{

    public partial class Form1 : Form
    {
        private const string ExcelFilesFolder = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\1. Team\Process\PO details\PO Data Formats\Current";
        public Form1()
        {
            InitializeComponent();

            // Fix for CS0165: Initialize the EPPlusLicense object properly
            EPPlusLicense excelPackage = new EPPlusLicense();

            // Fix for CS0103: Use the correct enum type EPPlusLicenseType
            // Fix for CS0200: LicenseType is read-only, so use the appropriate method to set the license
            excelPackage.SetNonCommercialOrganization("Your Organization Name");
        }

       
        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadExcelFileList();
            txtDataPath.TextChanged += TxtDataPath_TextChanged!;
            cboDataFormats.TextChanged += CboDataFormats_TextChanged!;
        }

        

        private void TxtDataPath_TextChanged(object? sender, EventArgs e)
        {
            UpdateValidateButtonState();
        }

        private void CboDataFormats_TextChanged(object? sender, EventArgs e)
        {
            UpdateValidateButtonState();
        }

        private void LoadExcelFileList()
        {
            if (Directory.Exists(ExcelFilesFolder))
            {
                var excelFiles = Directory.GetFiles(ExcelFilesFolder, "*.xls*", SearchOption.TopDirectoryOnly)
                    .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                    .Select(Path.GetFileName)
                    .ToList();

                cboDataFormats.Items.Clear();

                if (excelFiles != null && excelFiles.Count > 0)
                {
                    cboDataFormats.Items.AddRange([.. excelFiles.Cast<object>()]);
                    cboDataFormats.SelectedItem = excelFiles[0];
                }
            }
            else
            {
                MessageBox.Show($"Folder not found: {ExcelFilesFolder}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDataPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void btnValidate_Click(object sender, EventArgs e)
        {
            rtbLog.Text = "";
            Display(rtbLog, $"\nBeginning Validation...");
            string filepath = ExcelFilesFolder + "\\" + cboDataFormats.SelectedItem;
            string path = txtDataPath.Text;

            // Check if paths are valid
            if (string.IsNullOrEmpty(filepath) || string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please provide both file paths.");
                return;
            }

            // ***********************************************************
            // Load Data Dictionarys from Excel file {filepath}
            // ***********************************************************

            rtbLog.AppendText($"\nLoading Data Dictionaries...");

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
            List<FieldDefinition> dataDictionaryMPLASD = LoadExcelDataDictionary(filepath, "MPL Activity Statement", "MPL");
            // Everything else - MPL Standard 10
            List<FieldDefinition> dataDictionaryMPL10 = LoadExcelDataDictionary(filepath, "MPL Standard 10 adhoc", "MPL");
            // Add other dictionaries here...

            Display(rtbLog, $"\nReading Datafiles from {path}...");
            // ***********************************************************
            // Validate data files
            // ***********************************************************
            string[] txtFiles;
            try
            {
                txtFiles = Directory.GetFiles(path, "*.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {path} does not exist\nError: {ex}");
                return;
            }

            // Create log file
            string logFileName = "validation_log.txt";
            string logFilePath = Path.Combine(path, logFileName);
            using (StreamWriter writer = new StreamWriter(logFilePath, false))
            {
                foreach (string dataFilePath in txtFiles)
                {
                    // skip the validation_log.txt file
                    if (dataFilePath.ToLower().Contains("validation")) continue;
                    
                    // Validate and process the file
                    List<LogFile> logFile = new();
                    string filename = Path.GetFileName(dataFilePath);
                    Display(rtbLog, $"\n\nFile: - {filename}\n", true);
                    
                    // Validate file naming convention
                    var match = Regex.Match(filename, @"(?i)^(.*?)_(FILE40_|POST_|EMAIL_)?(.*)?_(LIVE|TEST)_(\d\d\D\D\D\d\d)\.txt$");
                    if (!match.Success)
                    {
                        LogValidation("NAMING CONVENTION", ["File does not match naming convention."], logFile, 0, rtbLog);

                        // Check for stuff after the date, or 
                        match = Regex.Match(filename, @"(?i)^(.*?)_(.*?_)?(.*?)_(LIVE|TEST)_(\d\d\D\D\D\d\d)(.*)?$");
                        if (match.Success)
                        {
                            LogValidation("NAMING CONVENTION", [$"End of file has additional/incorrect characters: '{match.Groups[6]}' after the date, or is not a 'txt' file.", "    Should be _ddMMMyy.txt, eg. _12MAR24.txt, _30MAY78.txt"], logFile, 0, rtbLog);
                        }
                        else
                        {
                            //Date format is wrong
                            match = Regex.Match(filename, @"(?i)^(.*?)_(.*?_)?(.*?)_(LIVE|TEST)_(.*)$");
                            if (match.Success)
                            {
                                LogValidation("NAMING CONVENTION", [$"Date Format: '{match.Groups[5]}' is incorrect.", "    Should be ddMMMyy.txt, eg. 12MAR24, 30MAY78."], logFile, 0, rtbLog);
                            }
                            else
                            {
                                //Date format is wrong
                                match = Regex.Match(filename, @"(?i)^(.*?)_(.*?_)?(.*?)_(.*?)_(.*)$");
                                if (match.Success)
                                {
                                    LogValidation("NAMING CONVENTION", [$"Processing Type: '{match.Groups[4]}' is not valid.", "    Can only be one of LIVE or TEST"], logFile, 0, rtbLog);
                                }
                            }
                        }
                    }
                    // Description cannot contain underscores.
                    //else if (Regex.Match(match.Groups[3].Value, @"_").Success)
                    //{
                    //    LogValidation("NAMING CONVENTION", [$"Correspondence Description cannot contain underscores - '_'.", $"     '{match.Groups[3]}' is not valid.", "    eg. MPLOOCFILE"], logFile, 0, rtbLog);
                    //}
                    else
                    {
                        // Theoretically the file is good.
                    }


                    // Check to see if file is a test file
                    bool test = true;
                    if (match.Groups[4] != null && match.Groups[4].Value.ToUpperInvariant() != "TEST") { test = false; }

                    // Based on the file naming convention, we can determine if the file is a file40, standard AHM, or M2100.
                    // M2100 files have 4 different formats
                    if (Regex.Match(match.Groups[1].Value, @"^(M2100).*$").Success)
                    {

                        // M2100PHI - OOC and Closures
                        if (Regex.Match(match.Groups[1].Value, @"^(M2100PHI).*$").Success)
                        {
                            try
                            {
                                ProcessTabDelimitedFile(dataFilePath, dataDictionaryMPLPHI, logFile, test, rtbLog);
                            }
                            catch (IOException ex)
                            {
                                // Check for file access being blocked because it is used by another process
                                if (ex.Message.Contains("because it is being used by another process"))
                                {
                                    Display(rtbLog, $"The file: {dataFilePath} is currently in use by another process.");
                                }
                                else
                                {
                                    // Handle other IOExceptions
                                    Display(rtbLog, $"IOException occurred: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Standard 40 (M1200PHI)\" based on file name but uses a different data structure.", "\nError: " +ex.Message], logFile, 0, rtbLog);
                            }
                        }
                        // M2100PRV - Providers
                        else if (Regex.Match(match.Groups[1].Value, @"^(M2100PRV).*$").Success)
                        {
                            try
                            {
                                ProcessTabDelimitedFile(dataFilePath, dataDictionaryMPLPRV, logFile, test, rtbLog);
                            }
                            catch (IOException ex)
                            {
                                // Check for file access being blocked because it is used by another process
                                if (ex.Message.Contains("because it is being used by another process"))
                                {
                                    Display(rtbLog, $"The file: {dataFilePath} is currently in use by another process.");
                                }
                                else
                                {
                                    // Handle other IOExceptions
                                    Display(rtbLog, $"IOException occurred: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Provider (M1200PRV)\" based on file name but uses a different data structure.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                            }
                        }
                        // M2100ASD - Activity Statements
                        else if (Regex.Match(match.Groups[1].Value, @"^(M2100ASD).*$").Success)
                        {
                            try
                            {
                                ProcessTabDelimitedFile(dataFilePath, dataDictionaryMPLPRV, logFile, test, rtbLog);
                            }
                            catch (IOException ex)
                            {
                                // Check for file access being blocked because it is used by another process
                                if (ex.Message.Contains("because it is being used by another process"))
                                {
                                    Display(rtbLog, $"The file: {dataFilePath} is currently in use by another process.");
                                }
                                else
                                {
                                    // Handle other IOExceptions
                                    Display(rtbLog, $"IOException occurred: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Activity Statements (M1200ASD)\" based on file name but uses a different data structure.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                            }
                        }
                        // All other M2100 files use the MPL Standard 10 adhoc structure
                        else
                        {
                            try
                            {
                                ProcessTabDelimitedFile(dataFilePath, dataDictionaryMPL10, logFile, test, rtbLog);
                            }
                            catch (IOException ex)
                            {
                                // Check for file access being blocked because it is used by another process
                                if (ex.Message.Contains("because it is being used by another process"))
                                {
                                    Display(rtbLog, $"The file: {dataFilePath} is currently in use by another process.");
                                }
                                else
                                {
                                    // Handle other IOExceptions
                                    Display(rtbLog, $"IOException occurred: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogValidation("MAJOR EXCEPTION", ["File structure is \"MPL Standard 10 (M1200)\" based on file name but uses a different data structure.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                            }
                        }

                    }
                    // AHM files
                    else if (Regex.Match(match.Groups[1].Value, @"^AHM.*$").Success)
                    {
                        // AHM40 format
                        if (match.Groups[2].Value == "FILE40_")
                        {
                            try
                            {
                                ProcessTabDelimitedFile(dataFilePath, dataDictionaryAHM40, logFile, test, rtbLog);
                            }
                            catch (IOException ex)
                            {
                                // Check for file access being blocked because it is used by another process
                                if (ex.Message.Contains("because it is being used by another process"))
                                {
                                    Display(rtbLog, $"The file: {dataFilePath} is currently in use by another process.");
                                }
                                else
                                {
                                    // Handle other IOExceptions
                                    Display(rtbLog, $"IOException occurred: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogValidation("MAJOR EXCEPTION", ["File structure is \"AHM file 40\" based on file name but uses a different data structure.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                            }

                        }
                        // AHM standard format.
                        else
                        {
                            try
                            {
                                ProcessTabDelimitedFile(dataFilePath, dataDictionaryAHM, logFile, test, rtbLog);
                            }
                            catch (IOException ex)
                            {
                                // Check for file access being blocked because it is used by another process
                                if (ex.Message.Contains("because it is being used by another process"))
                                {
                                    LogValidation("MAJOR EXCEPTION", [$"The file: {dataFilePath} is currently in use by another process.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                                }
                                else
                                {
                                    // Handle other IOExceptions
                                    LogValidation("MAJOR EXCEPTION", [$"IOException occurred.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogValidation("MAJOR EXCEPTION", ["File structure is \"AHM Standard\" based on file name but uses a different data structure.", "\nError: " + ex.Message], logFile, 0, rtbLog);
                            }

                        }
                    }
                    // Something else we don't currently cater for.
                    else
                    {
                        LogValidation("MAJOR EXCEPTION", ["No data structure detected for this file."], logFile, 0, rtbLog);
                    }

                    // Write log entries to RichTextBox
                    //foreach (var log in logFile)
                    //{
                    //    Display(rtbLog, $"[{log.logType}] Line {log.lineNumber}: {string.Join(", ", log.message)}\n");
                    //}

                    // Write the logs to the file
                    writer.WriteLine($"File: {filename}");
                    if (logFile.Count() == 0)
                    {
                        Display(rtbLog, $"File is good!\n", true);
                        writer.WriteLine($"File is good!\n");
                    }
                    WriteLog(writer, logFile);
                    
                }
            }
            Display(rtbLog, $"\n\nValidation file: {logFileName} has been created in the data file directory.", true);
            Display(rtbLog, $"\nPlease review...", true);
            //MessageBox.Show("Validation completed.");
        }

        //Read the data dictionarys from the Excel file
        private List<FieldDefinition> LoadExcelDataDictionary(string filePath, string worksheetName, string fieldOrderType)
        {
            var dataDictionary = new List<FieldDefinition>();
            FileInfo fileInfo = new FileInfo(filePath);
            
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[worksheetName];

                if (worksheet == null)
                {
                    Display(rtbLog, "Empty worksheet.\n");
                    return dataDictionary;
                }

                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
                {
                    if (fieldOrderType == "AHM")
                    {
                        var fieldDefinition = new FieldDefinition
                        {
                            FieldName = worksheet.Cells[row, 1].Text,
                            Type = worksheet.Cells[row, 2].Text,
                            IsMandatory = worksheet.Cells[row, 3].Text.StartsWith('Y'),
                            PossibleValues = string.IsNullOrEmpty(worksheet.Cells[row, 4].Text)
                               ? [] 
                               : [.. worksheet.Cells[row, 4].Text.Split(',')],
                            Comments = worksheet.Cells[row, 5].Text,
                            DataExample = worksheet.Cells[row, 6].Text
                        };

                        dataDictionary.Add(fieldDefinition);
                    }
                    else if (fieldOrderType == "MPL")
                    {
                        var fieldDefinition = new FieldDefinition
                        {
                            FieldName = worksheet.Cells[row, 2].Text,
                            Type = worksheet.Cells[row, 3].Text,
                            IsMandatory = worksheet.Cells[row, 4].Text.StartsWith("Man"),
                            PossibleValues = string.IsNullOrEmpty(worksheet.Cells[row, 5].Text)
                               ? []
                               : [.. worksheet.Cells[row, 5].Text.Split(',')],
                            Comments = "",
                            DataExample = "",
                        };

                        dataDictionary.Add(fieldDefinition);
                    }
                    else
                    {
                        throw new InvalidOperationException($"The data dictionary type \"{fieldOrderType}\" is not currently catered for.");
                    }
                }
            }

            return dataDictionary;
        }

        // Method to process the tab-delimited file and validate/enrich it using the Excel-based data dictionary
        static void ProcessTabDelimitedFile(string file, List<FieldDefinition> dataDictionary, List<LogFile> logFile, bool test, RichTextBox rtbLog)
        {

            int lineNo = 2;
            using (StreamReader reader = new StreamReader(file))
            {
                string headerLine = reader.ReadLine(); // Read header row of the tab-delimited file
                string[] tabHeaders = headerLine.Split('\t'); // Split headers by tab

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Ignore empty lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        LogValidation("EMPTY", [$"record is empty."], logFile, lineNo, rtbLog);
                        lineNo++;
                        continue;
                    }

                    string[] columns = line.Split('\t'); // Split the line into tab-separated columns

                    // Validate channel... 
                    // AHM Channel is field 31 - Channel
                    var channel_field = dataDictionary.FindIndex(field => field.FieldName.ToLower().Contains("channel"));
                    var email_field = dataDictionary.FindIndex(field => field.FieldName.ToLower().Contains("email"));
                    var sms_field = dataDictionary.FindIndex(field => field.FieldName.ToLower().Contains("mobile"));

                    switch (columns[channel_field])
                    {
                        case "INT":
                        case "EMAIL":
                            dataDictionary[email_field].IsMandatory = true;
                            dataDictionary[sms_field].IsMandatory = false;
                            break;

                        case "SMS":
                            dataDictionary[email_field].IsMandatory = false;
                            dataDictionary[sms_field].IsMandatory = true;
                            break;

                        default:
                            dataDictionary[email_field].IsMandatory = false;
                            dataDictionary[sms_field].IsMandatory = false;
                            break;
                    }

                    for (int i = 0; i < columns.Length; i++)
                    {
                        string fieldValue = columns[i];
                        
                        // Get the corresponding field definition based on the order
                        FieldDefinition fieldDefinition = dataDictionary[i];

                        // Weird issue with Double quotes around everything - JF
                        if (!Regex.Match(fieldDefinition.Type, @"^(?i)string.*$").Success &&  fieldValue.Contains("\""))
                        {
                            LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' contains \" (Double Quote) characters. ", $"Value: {fieldValue}"], logFile, lineNo, rtbLog);
                        }

                        // Validate mandatory fields
                        if (fieldDefinition.IsMandatory && string.IsNullOrEmpty(fieldValue))
                        {
                            LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' is mandatory but missing."], logFile, lineNo, rtbLog);
                        }

                        // Validate type
                        ValidateFieldType(fieldDefinition, fieldValue, logFile, lineNo, rtbLog);

                        // Other Validations

                    }

                    //Console.WriteLine(); // Blank line between rows

                    // Increment file line number
                    lineNo++;
                }

                if (test && lineNo > 61) { LogValidation("RECORDS", [$"File is marked as test but contains more than 60 records."], logFile, lineNo, rtbLog); }
            }
        }

        // Method to validate the field based on its type
        static void ValidateFieldType(FieldDefinition fieldDefinition, string fieldValue, List<LogFile> logFile, int lineNo, RichTextBox rtbLog)
        {
            // Skip validation if value is empty
            if (string.IsNullOrEmpty(fieldValue)) return;

            // Numbers
            if (Regex.Match(fieldDefinition.Type, @"^(?i)num.*$").Success)
            {
                if (!double.TryParse(fieldValue, out _))
                {
                    LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' should be a valid number, but got '{fieldValue}'"], logFile, lineNo, rtbLog);
                }
            }
            // Strings
            else if (Regex.Match(fieldDefinition.Type, @"^(?i)string.*$").Success)
            {
                // String needs no validation but it is a valid type. Could possiblt do some further validation here.
            }
            // Dates
            else if (Regex.Match(fieldDefinition.Type, @"^(?i)date.*$").Success)
            {
                //if (!DateTime.TryParseExact(fieldValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                if (!Regex.IsMatch(fieldValue, @"^\d{4}-\d{2}-\d{2}$"))
                {
                    LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' should be in the format 'yyyy-MM-dd', but got '{fieldValue}'"], logFile, lineNo, rtbLog);
                }
            }
            else
            {
                LogValidation("FIELD", [$"'{fieldDefinition.FieldName}' has an unsupported type: '{fieldDefinition.Type}'"], logFile, lineNo, rtbLog);
            }
        }

        static void LogValidation(string type, List<string> message, List<LogFile> logFile, int fileLineNo, RichTextBox rtbLog)
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

            Display(rtbLog, $"\nLine: {log.lineNumber}\n");
            Display(rtbLog, "\n" + string.Join("\n - ", log.message));
            Console.WriteLine($"Line: {log.lineNumber}");
            Console.WriteLine(string.Join(" - ", log.message));

        }

        private void WriteLog(StreamWriter writer, List<LogFile> logFile)
        {
            foreach (var log in logFile)
            {
                writer.WriteLine($"[{log.logType}] Line {log.lineNumber}: {string.Join(", ", log.message)}");
            }
        }

        // Other helper methods...
        // For example, validation, processing files, etc.
        static void Display(RichTextBox rtbLog, string text, bool bold = false)
        {
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionFont = new Font(rtbLog.Font, bold ? FontStyle.Bold : FontStyle.Regular);
            rtbLog.AppendText(text);
            rtbLog.ScrollToCaret();
        }
    }

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
        public string logType { get; set; }
        public List<string> message { get; set; }
        public int lineNumber { get; set; }
    }

}

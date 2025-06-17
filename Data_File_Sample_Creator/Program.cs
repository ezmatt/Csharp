using System;
using System.Collections.Generic;
using System.Globalization;
using System.Dynamic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using System.Collections;
using System.Security.Authentication.ExtendedProtection;
//using ExcelHandle;

partial class Program
{

    static void Main(string[] args)
    {
        // Get the current working directory
        string workingDirectory = Directory.GetCurrentDirectory();
        
        string filepath = (@"\\mplfiler\Groups\Operational Delivery\Fulfilment\1. Team\Process\PO details\PO Data Formats\OLD\PO Standard Data Formats_Current_DO NOT DELETE.xlsx");
        
        // Step 1: Load Excel-based data dictionary
        // AHM 40 data dictionary
        string dataModel = "ahm Standard 40 adhoc";
        ExcelHandle dataDictionaryAHM40 = new (filepath, dataModel, "AHM");
        if (!dataDictionaryAHM40.LoadExcelDataDictionary()) { throw new InvalidOperationException($"The data dictionary type: \"{dataModel}\" is not currently catered for."); }

        // Standard AHM data dictionary
        ExcelHandle dataDictionaryAHM = new (filepath, "ahm Standard", "AHM");
        if (!dataDictionaryAHM.LoadExcelDataDictionary()) { throw new InvalidOperationException($"The data dictionary type: \"{dataModel}\" is not currently catered for."); }
        
        // Standard MPL M2100 data dictionary
        ExcelHandle dataDictionaryMPL = new (filepath, "MPL Standard 40 adhoc", "MPL");
        if (!dataDictionaryMPL.LoadExcelDataDictionary()) { throw new InvalidOperationException($"The data dictionary type: \"{dataModel}\" is not currently catered for."); }
        
        
        // Step 2: Read in the excel file containing a list of fields required to create a sampling file.
        ExcelHandle uniqueFields = new(workingDirectory +  @"\Samples.xlsx", "Samples", "ALL");
        if (!uniqueFields.LoadExcelDataDictionary()) { throw new InvalidOperationException($"The data dictionary type: \"{dataModel}\" is not currently catered for."); }

        // Step 2: Read in the excel file containing a list of fields required to create a sampling file.
        ExcelHandle combinedUniqueFields = new(workingDirectory +  @"\Samples.xlsx", "Combined", "COM");
        if (!combinedUniqueFields.LoadExcelDataDictionary()) { throw new InvalidOperationException($"The data dictionary type: \"{dataModel}\" is not currently catered for."); }

        // Generate samples
        var samples = new SampleGenerator();
        samples.GenerateSamples(uniqueFields.DataDictionary);
        samples.GenerateCombinedSamples(combinedUniqueFields.DataDictionary);

        // Step 3: Load and process the tab-delimited sample data files
        string path =(@"\DataFiles\");

        string[] txtFiles = [];
        
        try {
            txtFiles = Directory.GetFiles(workingDirectory + path, "*.txt");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Exception: Directory '{path}' does not exist, or there are no data files to process. {ex}");
        }
        
        // Create Log Object
        LogFile log = new("validation_log.txt");
        
        // Create file Samples object 
        Samples fileSamples = new(workingDirectory + @"\SamplesCollected.txt", workingDirectory + @"\Samples.txt");
        
        // Process each test file to create one sample file
        foreach (string filePath in txtFiles)
        {
            // Validate File name.
            var file = Regex.Match(filePath, @".*\\(.+?)$");
            string filename = file.Groups[1].Value;
            Console.WriteLine($"Validating File: {filename}");
            
            // Validate file naming convention
            var match = Regex.Match(filename, @"^(.*?)_(FILE40_|POST_|EMAIL_)?(.*)?_(LIVE|TEST|WASHFILE)_(\d\d\D\D\D\d\d)\.(?i)txt$");
            if (!match.Success) 
            {
                log.LogError(filename, "NAMING CONVENTION", ["File does not match naming convention."], 0);
            }
            else {
                // Theoretically the file is good.
            }

            // Based on the file naming convention, we can determine if the file is a file40, standard AHM, or M2100.
            // M2100 MPL once off files are in the AHM40 format.
            if ( Regex.Match(match.Groups[1].Value, @"^(M2100).*$").Success) 
            {
                try {
                    ProcessTabDelimitedFile(filePath, dataDictionaryMPL, samples, fileSamples);
                }
                catch (Exception ex)
                {
                    log.LogError(filename, "MAJOR EXCEPTION", ["File structure is \"MPL Standard 40 (M1200)\" based on file name but uses a different data structure.", ex.Message], 0);
                }
                
            }
            // AHM files
            else if ( Regex.Match(match.Groups[1].Value, @"^AHM.*$").Success) 
            {
                // AHM40 format
                if (match.Groups[2].Value == "FILE40_" ) {
                    try {
                        ProcessTabDelimitedFile(filePath, dataDictionaryAHM40, samples, fileSamples);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(filename, "MAJOR EXCEPTION", ["File structure is \"AHM file 40\" based on file name but uses a different data structure.", ex.Message], 0);
                    }
                    
                }
                // AHM standard format.
                else 
                {    
                    try {
                        ProcessTabDelimitedFile(filePath, dataDictionaryAHM, samples, fileSamples);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(filename, "MAJOR EXCEPTION", ["File structure is \"AHM Standard\" based on file name but uses a different data structure.", ex.Message], 0);
                    }
                    
                }
            }
            // Something else we don't currently cater for.
            else {
                log.LogError(filename, "MAJOR EXCEPTION", ["No data structure detected for this file."], 0);
            }
            
            DateTime today = DateTime.Today;
            var sampleFileName = workingDirectory + @"\" + match.Groups[1].Value + "_";
            sampleFileName += (match.Groups[2].Success ) ? match.Groups[2].Value + "_": "";
            sampleFileName += "SAMPLES_TEST_";
            sampleFileName += today.ToString("ddMMMyy").ToUpper() + ".txt";
            fileSamples.SampleFileName = sampleFileName;

        }
        
        // Create the sample file
        fileSamples.WriteSampleFile();
    
        System.Console.WriteLine("Press any key to close...");
        Console.ReadLine();
    }

    // Method to process the tab-delimited file and validate/enrich it using the Excel-based data dictionary
    static void ProcessTabDelimitedFile(string file, ExcelHandle dataDictionary, SampleGenerator samples, Samples fileSamples)
    {
        
        int lineNo = 2;
        var fileRegEx = Regex.Match(file, @".*\\(.+?)$");
        string fileName = fileRegEx.Groups[1].Value;

        using (StreamReader reader = new StreamReader(file))
        {
            // Read header row of the tab-delimited file
            string headerLine = reader.ReadLine(); 
            
            // Store header line in Samples object for creating the sample file
            fileSamples.Headers.Add(file, headerLine);
            // Split headers by tab
            string[] tabHeaders = headerLine.Split('\t'); 

            List<string> records = new();

            string line;
            
            while ((line = reader.ReadLine()) != null)
            {
                string[] columns = line.Split('\t'); // Split the line into tab-separated columns
                var memberID = "";
                bool skip = false;
                List<string> combinedMatches = [];

                // This is to keep track of the sample scenarios capture for each record.
                // Only used for combined sampling
                IDictionary<string, string> capturedSamples = new Dictionary<string, string>();

                for (int i = 0; i < columns.Length; i++)
                {
                    string fieldValue = columns[i];

                    // Get the corresponding field definition based on the order
                    FieldDefinition fieldDefinition = dataDictionary.DataDictionary[i];
                    
                    if (fieldDefinition.FieldName == "MBR_ID") {memberID = fieldValue;}
                    
                    foreach (var sample in samples.Samples)
                    {
                        if ( sample.Scenario.Any(scenario => scenario.ContainsKey(fieldDefinition.FieldName)) ) {
                            var sampleRequirements = sample.Scenario
                                .First(scenario => scenario.ContainsKey(fieldDefinition.FieldName));
                            
                            // The below code is for all variations of a specific field.
                            // Create a new scenario under the current sample and add the base amount (from the original field name)
                            // if it doesn't already exist, otherwise reduce the amount by 1
                            if (sample.AllVariations) {
                                // If the field value already exists in the sampling dictionary then just add 1.
                                if (sampleRequirements.TryGetValue(fieldValue, out var something)) {
                                    Console.WriteLine($"{fieldValue}: {something}");
                                }
                                else {
                                    var newFieldVariation = new Dictionary<string, string>{
                                        {fieldDefinition.FieldName, fieldValue},
                                    };
                                    if (!sampleRequirements.ContainsKey(fieldDefinition.FieldName))
                                    {
                                        sampleRequirements.Add(fieldDefinition.FieldName, fieldValue);
                                    }

                                }
                                
                                // if (sample.Contains(fieldValue)) {
                                //     // Skip if we have the desired amount of samples for this field type
                                //     combinedMatches.Add(fieldValue);
                                //     capturedSamples.Add(fieldDefinition.FieldName, fieldValue);
                                // }
                                // else {
                                //     // Add the field value to the combined matches list.
                                //     // This keeps track of amount of matches we've had for this record so far.
                                //     combinedMatches.Add(fieldValue);
                                //     sampleRequirements.Add(fieldValue);
                                //     capturedSamples.Add(fieldDefinition.FieldName, fieldValue);
                                // }
                                
                            }
                            // // If a specific field value(s) is given then only check for those.
                            // else {
                            //     // Check to see if the field value needs to be sampled.
                            //     if (sampleRequirements.Contains(fieldValue)) {
                            //         combinedMatches.Add(fieldValue);
                            //         capturedSamples.Add(fieldDefinition.FieldName, fieldValue);
                            //     }
                            // }
                            
                            
                            // // If we have all the matches we require for this combination then add the record to the record list.
                            // if (combinedMatches.Count == samples.CombinedSamples[combinationKey].Count)
                            // {
                            //     // if ( samples.CombinedSamplesAmounts[combinationKey] > 0 ) 
                            //     // {
                            //     //     if (!skip) {
                            //     //         records.Add(line);
                            //     //     }
                            //     //     skip = true;
                            //     //     samples.CombinedSamplesAmounts[combinationKey]--;
                            //     //     foreach (var field in capturedSamples) {
                            //     //         AddRecord(fileSamples.CapturedSamples, memberID + $" - {fileName}", field.Key, field.Value);
                            //     //     }
                            //     // }
                            // }
                        }
                    }
                    // if ( samples.Samples.ContainsKey(fieldDefinition.FieldName) ) {
                    //     var sampleRequirements = samples.Samples[fieldDefinition.FieldName];
                    //     // Just grab the amount of each example you come across in the file(s) if a specific value isn't given.
                    //     if (sampleRequirements.ContainsKey("ALL")) {
                    //         int amount = sampleRequirements["ALL"];
                    //         // If the field value already exists in the sampling dictionary then just add 1.
                    //         if (sampleRequirements.TryGetValue(fieldValue, out int fieldAmount)) {
                    //             // Skip if we have the desired amount of samples for this field type
                    //             if (fieldAmount < amount) {
                    //                 sampleRequirements[fieldValue] = fieldAmount + 1;
                    //                 AddRecord(fileSamples.CapturedSamples, memberID + $" - {fileName}", fieldDefinition.FieldName, fieldValue);
                    //                 if (!skip) {
                    //                     records.Add(line);
                    //                 }
                    //                 skip = true;

                    //             }
                    //         }
                    //         else {
                    //             sampleRequirements[fieldValue] = 1;
                    //             AddRecord(fileSamples.CapturedSamples, memberID + $" - {fileName}", fieldDefinition.FieldName, fieldValue);
                    //             if (!skip) {
                    //                 records.Add(line);
                    //             }
                    //             skip = true;
                    //         }
                    //         samples.Samples[fieldDefinition.FieldName] = sampleRequirements;
                    //     }
                    //     // If a specific field value(s) is given then only check for those.
                    //     else {
                    //         // Check to see if the field value needs to be sampled.
                    //         if (sampleRequirements.ContainsKey(fieldValue)) {
                    //             if (sampleRequirements[fieldValue] > 0 ) {
                    //                 AddRecord(fileSamples.CapturedSamples, memberID + $" - {fileName}", fieldDefinition.FieldName, fieldValue);
                    //                 sampleRequirements[fieldValue]--;
                    //                 if (!skip) {
                    //                     records.Add(line);
                    //                 }
                    //                 skip = true;
                    //             }
                    //         }
                    //     }
                    // }
                }
                
                // Increment file line number
                lineNo++;
            }
            fileSamples.Records.Add(file,records);
        }

    }

    static void AddRecord(IDictionary<string, IDictionary<string, List<string>>> samples, string fileName, string header, string record)
    {
        // Ensure the fileName exists in the outer dictionary
        if (!samples.ContainsKey(fileName))
        {
            samples[fileName] = new Dictionary<string, List<string>>();
        }

        // Get the inner dictionary for the fileName
        var fileRecords = samples[fileName];

        // Ensure the header exists in the inner dictionary
        if (!fileRecords.ContainsKey(header))
        {
            fileRecords[header] = new List<string>();
        }

        // Add the record to the corresponding list
        fileRecords[header].Add(record);
    }
}

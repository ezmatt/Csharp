using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

class Program
{
    static void Main()
    {
        string excelFilePath = @"C:\\Users\\608138\\OneDrive - Medibank Private Limited\\AHM\\AHM Hospital and Extras code descriptions.xlsx"; // Change this to the correct file path

        var hospitalLookup = new Dictionary<char, string>();
        var extrasLookup = new Dictionary<char, string>();

        // Read lookup data from Excel
        using (var workbook = new XLWorkbook(excelFilePath))
        {
            var worksheet = workbook.Worksheet("Sheet1"); // Change if the sheet name is different
            var rows = worksheet.RangeUsed().RowsUsed();

            foreach (var row in rows.Skip(1)) // Skip header row
            {
                char hospitalCode = row.Cell(1).GetString()[0]; // WHICS Hosp Code (Column A)
                string hospitalDesc = row.Cell(2).GetString();   // WHICS Hospital Desc (Column B)
                char extrasCode = row.Cell(3).GetString() == "" ? ' ' : row.Cell(3).GetString()[0];    // HICS Extras Code (Column C)
                string extrasDesc = row.Cell(4).GetString();     // WHICS Extras Name (Column D)

                if (!hospitalLookup.ContainsKey(hospitalCode))
                    hospitalLookup[hospitalCode] = hospitalDesc;

                if (!extrasLookup.ContainsKey(extrasCode))
                    extrasLookup[extrasCode] = extrasDesc;
            }
        }

        string outputFilePath = @"C:\\Users\\608138\\OneDrive - Medibank Private Limited\\AHM\\Output.txt"; // Output file

        // Sample input codes
        List<string> inputCodes = new List<string> { "A51", "A54", "A53", "A5N", "GCN", "GC1", "GC2", "GC3", "GC4", "GCR", "LCN", "LC1", "LC2", "LC4", "LC3", "WCN", "WCR", "WC1", "L5B", "L5H", "WC2", "WC3", "WC4" };
        List<string> outputLines = new List<string>();

        foreach (var code in inputCodes)
        {
            if (code.Length != 3)
            {
                outputLines.Add($"Invalid code: {code}");
                continue;
            }

            char hospitalCode = code[0];
            char extrasCode = code[2];

            string hospitalDesc = hospitalLookup.ContainsKey(hospitalCode) ? hospitalLookup[hospitalCode] : "Unknown Hospital";
            string extrasDesc = extrasLookup.ContainsKey(extrasCode) ? extrasLookup[extrasCode] : "Unknown Extras";

            string output = $"Code: {code} -> {hospitalDesc} + {extrasDesc}";
            outputLines.Add(output);
        }

        // Write output to a text file
        File.WriteAllLines(outputFilePath, outputLines);

        Console.WriteLine($"Output written to {outputFilePath}");
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        string path = @"\\mplfiler\Groups\Operational Delivery\Fulfilment\3. MPL\1. MPL Work Requests\OWR001034- OOC Buderim Gastroenterology Centre\6. Prod Samples\Prebooked Matt\";
        string lookupFilePath = path + @"lookupCombined.txt";
        string[] dataFilePaths = { 
            path + @"Prebooked MPL\M2100PHI012024_MPLOOCHOSPITALEMAIL1_LIVE_24MAR25.txt", 
            path + @"Prebooked MPL OSHC\M2100PHI032024_MPLOSHCCOOCHOSPITALEMAIL1_LIVE_25MAR25.txt"
        };

        // Read Member IDs into a HashSet for fast lookup
        HashSet<string> memberIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Regex memberIdRegex = new Regex(@"\d{8}.", RegexOptions.IgnoreCase); // Example pattern: MEM12345

        foreach (var line in File.ReadLines(lookupFilePath))
        {
            var match = memberIdRegex.Match(line);
            if (match.Success)
            {
                memberIds.Add(match.Value);
            }
        }

        foreach (string dataFile in dataFilePaths)
        {
            string outputFilePath = dataFile + ".new";
            using (var reader = new StreamReader(dataFile))
            using (var writer = new StreamWriter(outputFilePath))
            {
                string line = reader.ReadLine();
                writer.WriteLine(line); //header
                while ((line = reader.ReadLine()) != null)
                {
                    string memberId = ExtractMemberIdFromLine(line); // <-- Customize this
                    if (memberIds.Contains(memberId))
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
        Console.WriteLine("Done! Matched file written.");
    }

    // Example: assuming MemberID is the first value in a CSV line
    static string ExtractMemberIdFromLine(string line)
    {
        var parts = line.Split('\t');
        return parts[3]+parts[4]; // Adjust index as needed
    }
}
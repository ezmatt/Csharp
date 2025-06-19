using System;
using System.Collections.Generic;
using System.Linq;
using XDPToolKit.XdpAnalysis;
using CommonLibrary;
using OfficeOpenXml.ConditionalFormatting;
using DocumentFormat.OpenXml.Features;
using System.Text.Json;
using XDPToolKit.Models;

class Program
{
    public static async Task Main()
    {
        string xdpDirectoryPath = @"C:\Users\608138\OneDrive - Medibank Private Limited\MedibankGithub\WHICS_Templates_PROD";

        List<string> xdpDirectories = [.. Directory.GetDirectories(xdpDirectoryPath, "*", SearchOption.AllDirectories)];
        xdpDirectories.Add(xdpDirectoryPath);

        var log = new Logger(Directory.GetCurrentDirectory() + @"\\log.txt");

        foreach (string xdpDirectory in xdpDirectories)
        {
            log.Log($"Directory: {xdpDirectory}");
            foreach (string xdpFile in Directory.GetFiles(xdpDirectory, "*.xdp"))
            {
                //#######################################################
                string[] testingLetterCodes = 
                [
                    //"HENQBX",
                    //"AMBLD1",
                ];
                if (testingLetterCodes.Length > 0 && !testingLetterCodes.Any(code => xdpFile.Contains(code))) continue;
                //#######################################################

                log.Log($"- {xdpFile}");

                // Start the XDP parsing process
                var xdp = new XdpParser(xdpFile, log);
                var model = xdp.BuildFormModel();

                // Create filename with .json extension
                string outputFileName = Path.GetFileNameWithoutExtension(xdpFile) + ".json";
                string outputPath = Path.Combine(xdpDirectory, outputFileName);
                using FileStream createStream = File.Create(outputPath);
                
                // Serialize to JSON
                await JsonSerializer.SerializeAsync(createStream, model, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                log.Log($"✓ {outputFileName} created.");
                
            }
        }
        log.Log("Complete...");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

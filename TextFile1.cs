using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using XDPToolKit;
using XDPToolKit.XdpAnalysis;
using System.Text.RegularExpressions;
using System.IO.Compression;
using OfficeOpenXml;
using System.Collections.Generic;
using AEMProductUtilsSearch;
using System.Linq;
using CommonLibrary;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;
using DocumentFormat.OpenXml.Office2010.Ink;

class Program
{


    static void Main()
    {
        List<string> searchStrings = [
            //"cheque",
            "keyno_042",
            "keyno_057",
            "keyno_071",
            "keyno_494",
            "keyno_8144",
            "keyno_8245",
            "keyno_8258",
            "prev_prod_code",
            "new_prod_code",
            //"productUtils",
        ];

        // Define each node's requirements
        Dictionary<string, XDP> nodeSearch = new Dictionary<string, XDP>
        {
            { "bind", new XDP { NamedParent = "field", Attribute = "ref" } },
            { "script", new XDP { NamedParent = "field", Attribute = "" } },
            { "value", new XDP { NamedParent = "draw", Attribute = "" } },
            { "subform", new XDP { NamedParent = "pageArea", Attribute = "usehref" } },
        };


        string filePath = Directory.GetCurrentDirectory() + @"\\WHICS_Templates";
        //filePath = Utilities.ValidateInput("Enter root (starting) directory that contains XDP (AEM) files :", filePath);
        filePath = @"C:\\Users\\608138\\DevelopmentGithub\\C#\\AEMProductUtilsSearch\\WHICS_Templates_UAT";
        filePath = @"C:\\Users\\608138\\DevelopmentGithub\\C#\\AEMProductUtilsSearch\\WHICS_Templates_PROD";
        List<string> xdpDirectories = [.. Directory.GetDirectories(filePath, "*", SearchOption.AllDirectories)];
        xdpDirectories.Add(filePath);

        string outputPath = filePath + @"\\output.xlsx"; // Change as needed
        var dataManager = new Data(outputPath);
        var log = new Logger(filePath + @"\\log.txt");

        // Search through content
        string worksheetName = "AEM_Forms";

        string[] nodeNames = { "value", "field", "script" };

        foreach (string xdpDirectory in xdpDirectories)
        {
            log.Log($"Directory: {xdpDirectory}");
            string[] xdpFiles = Directory.GetFiles(xdpDirectory, "*.xdp");

            foreach (string xdpFile in xdpFiles)
            {
                //For testing purposes. Uncomment to test a specific file
                //if (!xdpFile.Contains("TRAE01")) continue;

                // Main search logic
                try
                {
                    log.Log($"File: {RegexMe(xdpFile, $@"WHICS_Templates(?:_PROD|_UAT)?\\(.+)\\[^\\]+\.*\.xdp$")}\\{RegexMe(xdpFile, @"([^\\]+)\.xdp$")}");

                    var parser = new XdpParser(xdpFile);

                    // Search through list of items to search for
                    foreach (string searchString in searchStrings)
                    {
                        // Search the entire XDP file first to see if the search string is in there or not
                        // Make sure we aren't missing anything.
                        // skp the element nodes as it has all the fields in the data dictionary for all forms.
                        Boolean hasSearchString = false;
                        List<XElement> searchElements = parser.GetNodesWithSearchTextInEntireXDP(searchString);
                        foreach (XElement searchNode in searchElements)
                        {
                            if (searchNode.Name.LocalName == "element") continue;
                            hasSearchString = true;
                        }

                        Boolean isMatched = false;

                        foreach (string nodeName in nodeSearch.Keys)
                        {
                            //

                            List<XElement> matchingNodes = [];

                            if (nodeSearch[nodeName].Attribute != "")
                            {
                                matchingNodes = parser.FindSpecificNodesByAttribute(searchString, nodeName);
                            }
                            else
                            {
                                matchingNodes = parser.FindSpecificNodesByValue(searchString, nodeName);
                            }

                            List<string> searchPseudonyms = new List<string>();

                            foreach (XElement node in matchingNodes)
                            {

                                var subformName = GetSubFormName(parser, node);

                                //XElement parentNode = parser.FindAllElementsByTagParent(node, nodeSearch[nodeName].NamedParent);
                                XElement parentNode = parser.FindParentElementByAttribute(node, "name");
                                string containerName = (parentNode != null) ? parentNode.Attribute("name").Value : "";

                                // Get the Attribute value if the nodeSearch has a valid attribute value
                                string content = node.Value;
                                if (nodeSearch[nodeName].Attribute != "")
                                {
                                    if (node.Attribute(nodeSearch[nodeName].Attribute) != null)
                                    {
                                        content = node.Attribute(nodeSearch[nodeName].Attribute).Value;
                                    }
                                    else if (node.Attribute("name") != null)
                                    {
                                        content = node.Attribute("name").Value;
                                    }
                                }

                                // Add the binding pseudonyms to the list to search through scripts below
                                if (nodeName == "bind" && containerName != searchString)
                                {
                                    searchPseudonyms.Add(containerName);
                                }

                                // Add the field to the data manager
                                dataManager.AddFormData(
                                                    RegexMe(xdpFile, @"([^\\]+)\.xdp$"),
                                                    RegexMe(xdpFile, $@"WHICS_Templates(?:_PROD|_UAT)?\\(.+)\\[^\\]+\.*\.xdp$"),
                                                    subformName,
                                                    searchString,
                                                    containerName,
                                                    nodeName,
                                                    content);

                                log.Log($"\nFound \"{searchString}\"");
                                log.Log($"Subform: \"{subformName}\"");
                                log.Log("Container: " + containerName);
                                log.Log($"{nodeName} value: {content}\n", true);

                                isMatched = true;
                            }

                            //// Now that we have the bind pseudonyms, let's search for them in script nodes
                            foreach (string pseudonym in searchPseudonyms)
                            {
                                log.Log($"\nSearching for pseudonym: {pseudonym} in script elements");

                                matchingNodes = parser.FindSpecificNodesByValue(pseudonym, "script");

                                foreach (var node in matchingNodes)
                                {
                                    var subformName = GetSubFormName(parser, node);

                                    XElement parentNode = parser.FindParentElementByAttribute(node, "name");
                                    string containerName = (parentNode != null) ? parentNode.Attribute("name").Value : "";

                                    string content = node.Value;

                                    // Add the field to the data manager
                                    dataManager.AddFormData(
                                                        RegexMe(xdpFile, @"([^\\]+)\.xdp$"),
                                                        RegexMe(xdpFile, $@"WHICS_Templates(?:_PROD|_UAT)?\\(.+)\\[^\\]+\.*\.xdp$"),
                                                        subformName,
                                                        searchString,
                                                        containerName,
                                                        "script",
                                                        content);

                                    log.Log($"subForm: {subformName} uses: {searchString}...", true);
                                    log.Log("Container: " + containerName);
                                    log.Log($"{nodeName}: {content}", true);

                                    isMatched = true;
                                }
                            }

                        }


                        if (!isMatched && hasSearchString)
                        {
                            log.Log($"\n*************************************\n* No matches found for {searchString} in {xdpFile}\n*************************************\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        dataManager.WriteToExcel(worksheetName);
    }

    private static object GetContainerName(XdpParser parser, XElement parentNode)
    {
        throw new NotImplementedException();
    }

    private static string GetSubFormName(XdpParser parser, XElement node)
    {
        var subformParent = parser.FindAllElementsByTagParent(node, "subform");
        return (subformParent.Attribute("name") != null) ? subformParent.Attribute("name").Value : "";
    }

    public static string? RegexMe(string scriptValue, string regex)
    {
        // Regex pattern to match "productUtils.methodName("
        Match match = Regex.Match(scriptValue, regex);

        if (match.Success)
        {
            return match.Groups[1].Value; // The method name
        }

        return "";
    }

}
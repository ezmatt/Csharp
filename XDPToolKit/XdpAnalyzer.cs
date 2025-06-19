using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CommonLibrary;

namespace XDPToolKit
{
    namespace XdpAnalysis
    {
        public class XdpAnalyzer
        {
            public class XDPFormData
            {
                public string FileName { get; set; }
                public string TemplateName { get; set; }
                public string SubformName { get; set; }
                public Dictionary<string, string> Attributes { get; set; } = new();
                public Dictionary<string, string> BodyTexts { get; set; } = new(); // Stores <body> content per subform
                public List<string> FragmentsUsed { get; set; } = new();
                public List<MatchResult> Matches { get; set; } = new();
            }

            public class MatchResult
            {
                public string MatchingFileName { get; set; }
                public double SimilarityPercentage { get; set; }
            }

            public static List<XDPFormData> ProcessXDPFiles(string directoryPath)
            {
                var formDataList = new List<XDPFormData>();
                var xdpFiles = Directory.GetFiles(directoryPath, "*.xdp");

                foreach (var file in xdpFiles)
                {
                    GlobalLogger.Log($"File: {file}");
                    try
                    {
                        XdpParser parser = new XdpParser(file, null);
                        var subforms = parser.GetAllOfType("subform");
                        var bodyTexts = new Dictionary<string, string>();

                        foreach (var subform in subforms)
                        {
                            var subformName = subform.Attribute("name")?.Value ?? "UnknownSubform";
                            var bodyContent = string.Join(" ", subform.Elements("exData")
                                .Where(exData => exData.Attribute("name")?.Value == "body")
                                .Select(exData => exData.Value));

                            if (!string.IsNullOrWhiteSpace(bodyContent))
                            {
                                bodyTexts[subformName] = bodyContent;
                            }
                        }

                        var fragments = subforms
                            .Where(sf => sf.Attribute("usehref") != null)
                            .Select(sf => sf.Attribute("usehref")?.Value ?? "")
                            .ToList();

                        var formData = new XDPFormData
                        {
                            FileName = Path.GetFileName(file),
                            TemplateName = parser.FindElementByTag("template")?.Attribute("name")?.Value ?? "Unknown",
                            BodyTexts = bodyTexts,
                            FragmentsUsed = fragments
                        };

                        formDataList.Add(formData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {file}: {ex.Message}");
                    }
                }

                CompareForms(formDataList);
                WriteResultsToCsv(formDataList, Path.Combine(directoryPath, "XDPAnalysisResults.csv"));
                return formDataList;
            }

            private static void CompareForms(List<XDPFormData> forms)
            {
                for (int i = 0; i < forms.Count; i++)
                {
                    for (int j = i + 1; j < forms.Count; j++)
                    {
                        double maxSimilarity = forms[i].BodyTexts.Values.SelectMany(text1 =>
                            forms[j].BodyTexts.Values.Select(text2 => ComputeJaccardSimilarity(text1, text2)))
                            .DefaultIfEmpty(0).Max();

                        if (maxSimilarity > 60) // Only store matches above 60%
                        {
                            forms[i].Matches.Add(new MatchResult { MatchingFileName = forms[j].FileName, SimilarityPercentage = maxSimilarity });
                            forms[j].Matches.Add(new MatchResult { MatchingFileName = forms[i].FileName, SimilarityPercentage = maxSimilarity });
                        }
                    }
                }
            }

            private static double ComputeJaccardSimilarity(string text1, string text2)
            {
                if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                    return 0.0;

                var set1 = new HashSet<string>(text1.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries));
                var set2 = new HashSet<string>(text2.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries));

                double intersection = set1.Intersect(set2).Count();
                double union = set1.Union(set2).Count();

                return union == 0 ? 0.0 : (intersection / union) * 100.0;
            }

            private static void WriteResultsToCsv(List<XDPFormData> forms, string filePath)
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("FileName,TemplateName,SubformName,BodyText,FragmentsUsed,MatchingFile,SimilarityPercentage");
                    foreach (var form in forms)
                    {
                        var fragments = string.Join("; ", form.FragmentsUsed);
                        foreach (var (subform, bodyText) in form.BodyTexts)
                        {
                            foreach (var match in form.Matches)
                            {
                                writer.WriteLine($"{form.FileName},{form.TemplateName},{subform},{bodyText.Replace(",", " ")},{fragments},{match.MatchingFileName},{match.SimilarityPercentage:F2}");
                            }
                        }
                    }
                }
            }
        }
    }
}

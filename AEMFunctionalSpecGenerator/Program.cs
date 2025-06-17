using System;
using System.Collections.Generic;
using System.IO;
using XDPToolKit.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Compression;
using AEMFunctionalSpecGenerator;
using AEMFunctionalSpecGenerator.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Linq;
using System.Text.RegularExpressions;
using Style = DocumentFormat.OpenXml.Wordprocessing.Style;
using DocumentFormat.OpenXml;
using System.Dynamic;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using TopBorder = DocumentFormat.OpenXml.Wordprocessing.TopBorder;
using BottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using LeftBorder = DocumentFormat.OpenXml.Wordprocessing.LeftBorder;
using RightBorder = DocumentFormat.OpenXml.Wordprocessing.RightBorder;
using Break = DocumentFormat.OpenXml.Wordprocessing.Break;
using Color = DocumentFormat.OpenXml.Wordprocessing.Color;
using FontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
using CommonLibrary;
using AEMFunctionalSpecGenerator.DataDictionaries;

using Hyperlink = DocumentFormat.OpenXml.Wordprocessing.Hyperlink;
using Underline = DocumentFormat.OpenXml.Wordprocessing.Underline;
using UnderlineValues = DocumentFormat.OpenXml.Wordprocessing.UnderlineValues;
using DocumentFormat.OpenXml.Drawing;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using TableGrid = DocumentFormat.OpenXml.Wordprocessing.TableGrid;
using GridColumn = DocumentFormat.OpenXml.Wordprocessing.GridColumn;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using TableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using TableProperties = DocumentFormat.OpenXml.Wordprocessing.TableProperties;
using InsideHorizontalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder;
using InsideVerticalBorder = DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder;

namespace XDPFunctionalSpecGenerator
{
    class Program
    {
        // Initialize the logger
        private static Logger log = new Logger(Directory.GetCurrentDirectory() + @"\\log.txt");

        private static readonly ExcelLookupReader reader = new();

        private static List<string> hyperlinkTags = new List<string>();

        static void Main(string[] args)
        {
            log.Log("Generating Functional Specification Document...");

            string jsonDirectoryPath = @"C:\Users\608138\OneDrive - Medibank Private Limited\MedibankGithub\WHICS_Templates_PROD";

            List<string> jsonDirectories = new List<string>(Directory.GetDirectories(jsonDirectoryPath, "*", SearchOption.AllDirectories));
            jsonDirectories.Add(jsonDirectoryPath);

            foreach (string jsonDirectory in jsonDirectories)
            {
                log.Log($"Directory: {jsonDirectory}");

                List<SubformNode> subforms = new List<SubformNode>();

                foreach (string jsonFile in Directory.GetFiles(jsonDirectory, "*.json"))
                {
                    //##########################################
                    if (!jsonFile.Contains("AMBLD1")) continue;
                    //##########################################

                    log.Log($"- {jsonFile}");

                    try
                    {
                        FormJsonModel subformNode = JsonLoader.LoadSubformFromJson(jsonFile);
                        GenerateWordDocument(subformNode, jsonFile.Replace(".json", ".docx"));
                    }
                    catch (Exception ex)
                    {
                        log.Log($"Error loading JSON: {ex.Message}");
                    }
                }
            }

            log.Log($"Functional Specification generated at: {System.IO.Path.GetFullPath(jsonDirectoryPath)}");
            log.Log("\n\nPress any key to exit...");
            //Console.ReadLine();
        }

        //###################################################
        // Create Functional Spec from JSON
        //###################################################
        static void GenerateWordDocument(FormJsonModel form, string filePath)
        {
            try
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();

                    Body body = new Body();
                    mainPart.Document = new Document(body);

                    AddStyles(mainPart);

                    var rootsubform = form.RootSubform;
                    var fragments = form.Fragments;
                    var scripts = form.Scripts;
                    var fields = form.Fields;

                    // Functional Spec Heading
                    var formCode = RegexMe(filePath, @"\\([A-Za-z0-9]+)\.docx");
                    List<string> formCodes = [formCode];
                    var correspondenceProduct = "Member Comms";

                    DocumentHeading(body, rootsubform, correspondenceProduct, formCodes);
                    StartNewSection(body, PageOrientation.Portrait);

                    // Fragments
                    if (fragments.Count > 0)
                    {
                        AddFragmentsToDocument(body, fragments);
                        AddPageSetupToDocument(body, rootsubform.PageAreaFragments);
                        StartNewSection(body, PageOrientation.Portrait);
                    }
                    
                    // Scripts
                    if (scripts.Count > 0)
                    {
                        AddScriptsToDocument(body, scripts);
                        StartNewSection(body, PageOrientation.Portrait);
                    }

                    // Scripts
                    if (fields.Count > 0)
                    {
                        AddFieldsToDocument(body, fields);
                        StartNewSection(body, PageOrientation.Portrait);
                    }

                    AddCopyTextToDocument(body, rootsubform);
                    StartNewSection(body, PageOrientation.Landscape);

                }
            }
            catch (Exception ex)
            {
                log.Log($"Error generating Word document: {ex.Message}");
            }
        }

        //###################################################
        // Functional Spec Section methods
        //###################################################
        // Document Heading table
        // TODO:
        // 1. Need to add the form description to the table dynamically
        // 2. Add Background Logo
        static void DocumentHeading(Body body, SubformNode subformNode, string correspondenceProduct, List<string> formCodes)
        {
            for (int i = 0; i < 3; i++)
            {
                body.Append(PrintStyle("Headings1", ""));
            }

            body.Append(PrintStyle("Headings1", $"Communication Requirements Brief"));

            // Create the table
            Table table = new Table();

            // Append TableGrid first
            TableGrid tableGrid = new TableGrid(
                new GridColumn() { Width = DXA(5) },
                new GridColumn() { Width = DXA(13.4) }
            );
            table.AppendChild(tableGrid);

            // Then append TableProperties
            table.AppendChild(CreateTableProperties(DXA(18.4), false));

            string letterCodes = string.Join(", ", formCodes);
            string todaysDate = DateTime.Now.ToString("dd/MM/yyyy");

            var tableHeadings = new Dictionary<string, string>
            {
                { "Correspondence:", correspondenceProduct },
                { "Description:", "" },
                { "Letter Code(s):", letterCodes },
                { "ECD Contact:", "" },
                { "Marketing Contact:", "" },
                { "Date:", todaysDate },
                { "Version:", "1.0" }
            };

            foreach (var item in tableHeadings)
            {
                TableRow row = new TableRow();
                row.Append(
                    new TableCell(
                        StandardCellProperties(new CellProps(VerticalMergeType.None, 1, "centre"), ((int)Colours.AHMGrey).ToString("X6")),
                        PrintStyle("tableHeading", item.Key)
                    ),
                    new TableCell(
                        StandardCellProperties(new CellProps(VerticalMergeType.None, 1, "centre")),
                        PrintStyle("paragraph", item.Value)
                    )
                );
                table.Append(row);
            }

            body.Append(table);
        }

        // Add the Fragments to the word Doc.
        private static void AddFragmentsToDocument(Body body, Dictionary<string, Fragment> fragments)
        {
            body.Append(PrintStyle("Headings2", "Common Fragments"));

            // Create the table
            Table table = new Table();

            // ✅ Define the column widths explicitly
            TableGrid tableGrid = new TableGrid(
                new GridColumn() { Width = DXA(2.8) },
                new GridColumn() { Width = DXA(5) },
                new GridColumn() { Width = DXA(6) },
                new GridColumn() { Width = DXA(2.1) },
                new GridColumn() { Width = DXA(2.1) }
            );
            table.AppendChild(tableGrid);

            // Define table properties
            table.AppendChild(CreateTableProperties(DXA(18.4), false));


            // Header row
            TableRow headerRow = new TableRow();
            headerRow.Append(
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    new TableCellWidth { Width = DXA(2.8), Type = TableWidthUnitValues.Dxa },
                    PrintStyle("tableHeading", "Fragment ID", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    new TableCellWidth { Width = DXA(5), Type = TableWidthUnitValues.Dxa }, 
                    PrintStyle("tableHeading", "CF Name", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    new TableCellWidth { Width = DXA(6), Type = TableWidthUnitValues.Dxa }, 
                    PrintStyle("tableHeading", "Location", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    new TableCellWidth { Width = DXA(2.1), Type = TableWidthUnitValues.Dxa },
                    PrintStyle("tableHeading", "X-Pos", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    new TableCellWidth { Width = DXA(2.1), Type = TableWidthUnitValues.Dxa },
                    PrintStyle("tableHeading", "Y-Pos", true)
                )
            );

            table.Append(headerRow);

            // Data rows
            foreach (var fragment in fragments)
            {
                // Create a new table row
                TableRow row = new TableRow();

                // Create a cell for the script ID
                TableCell CFID = new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("table", fragment.Key, true)
                );

                // Create a cell for the script ID
                TableCell CFName = new TableCell(
                    StandardCellProperties(new CellProps()),
                    PrintStyle("table", fragment.Value.Name)
                );

                // Location
                TableCell location = new TableCell(
                    StandardCellProperties(new CellProps()),
                    PrintStyle("table", fragment.Value.FragmentLocation)
                );

                // Location
                TableCell xpos = new TableCell(
                    StandardCellProperties(new CellProps()),
                    PrintStyle("table", fragment.Value.PageLocation.x)
                );

                // Location
                TableCell ypos = new TableCell(
                    StandardCellProperties(new CellProps()),
                    PrintStyle("table", fragment.Value.PageLocation.y)
                );

                row.Append(CFID, CFName, location, xpos, ypos);
                table.Append(row);
            }

            body.Append(table);
        }

        //Add Master Page setups
        // TODO:
        // 1. Might add positioning
        private static void AddPageSetupToDocument(Body body, Dictionary<string, Dictionary<string, string>> pageAreaFragments)
        {
            body.Append(PrintStyle("Headings2", ""));
            body.Append(PrintStyle("Headings2", "Master Page Setup"));

            // Create the table
            Table table = new Table();

            // ✅ Define the column widths explicitly
            TableGrid tableGrid = new TableGrid(
                new GridColumn() { Width = DXA(2.5) },
                new GridColumn() { Width = DXA(2.5) },
                new GridColumn() {  }
            );
            table.AppendChild(tableGrid);

            // Define table properties
            table.AppendChild(CreateTableProperties(DXA(18.4)));


            // Header row
            TableRow headerRow = new TableRow();
            headerRow.Append(
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Master Page", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Fragment ID", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Fragment Name", true)
                )
            );

            table.Append(headerRow);

            // Data rows
            foreach (var pageArea in pageAreaFragments)
            {

                string pageName = pageArea.Key;  // e.g., "Page1"
                var fragments = pageArea.Value;  // Dictionary<string, string>
                VerticalMergeType startSectionMerge = VerticalMergeType.Restart;

                foreach (var fragment in fragments)
                {
                    string fragmentId = fragment.Key;    // e.g., "TF004"
                    string fragmentName = fragment.Value; // e.g., "Header_PHI"

                    // Create a new table row
                    TableRow row = new TableRow();

                    // Create a cell for the script ID
                    TableCell pageNameCell = new TableCell(
                        StandardCellProperties(new CellProps(startSectionMerge), ((int)Colours.AHMGrey).ToString("X6")),
                        PrintStyle("table", (string)pageName, true)
                    );

                    // Create a cell for the script ID
                    TableCell CFID = new TableCell(
                        StandardCellProperties(new CellProps()),
                        PrintStyle("table", fragmentId)
                    );

                    // Location
                    TableCell CFName = new TableCell(
                        StandardCellProperties(new CellProps()),
                        PrintStyle("table", fragmentName)
                    );

                    row.Append(pageNameCell, CFID, CFName);
                    table.Append(row);
                    startSectionMerge = VerticalMergeType.Continue;
                }
            }

            body.Append(table);
        }

        // Add the Business rules/Scripts to the word Doc.
        static void AddScriptsToDocument(Body body, Dictionary<string, FormScript> scripts)
        {
            body.Append(PrintStyle("Headings2", "Business Rules"));

            // Create the table
            Table table = new Table();
                        
            // ✅ Define the column widths explicitly
            TableGrid tableGrid = new TableGrid(
                new GridColumn() { Width = DXA(2.8) },
                new GridColumn() { Width = DXA(2.8) },
                new GridColumn() { Width = DXA(12.8) }
            );
            table.AppendChild(tableGrid);
            
            // Define table properties
            table.AppendChild(CreateTableProperties(DXA(18.4)));


            // Header row
            TableRow headerRow = new TableRow();
            headerRow.Append(
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "BR ID", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "BR Name ID", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Code", true)
                )
            );

            table.Append(headerRow);

            var JSConverter = new JsToPseudocodeConverter();

            // Data rows
            foreach (var script in scripts)
            {
                // Convert JavaScript to pseudocode
                string code = JSConverter.Convert(script.Value.Code);
                // Create a new table row
                TableRow row = new TableRow();
                
                // Create a cell for the script ID
                TableCell BRIdCell = new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("table", script.Key, true)
                );

                // Create a cell for the script ID
                TableCell BRName = new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("table", "")
                );

                // Create a cell for the code
                List<Paragraph> codeParagraphs = new List<Paragraph>();

                int indent = 0;

                foreach (string rawLine in code.Split("\r\n"))
                {
                    string line = rawLine.TrimEnd();
                    indent = (line.Contains("\t")) ? 567 : 0;
                    // Create paragraph with current indentation
                    Paragraph paragraph = new Paragraph(
                        new ParagraphProperties(
                            new Indentation { Left = (indent).ToString() },
                            new SpacingBetweenLines { After = "0" },
                            new ParagraphStyleId() { Val = "table" }
                        ),
                        new Run(new Text(line))
                    );

                    // Append paragraph to the cell content
                    codeParagraphs.Add(paragraph);
                }

                TableCell codeCell = new TableCell(
                    StandardCellProperties(new CellProps())
                );
                // Append each paragraph to the cell
                foreach (var para in codeParagraphs)
                {
                    codeCell.Append(para);
                }

                row.Append(BRIdCell, BRName, codeCell);
                table.Append(row);
            }
            
            body.Append(table);
        }

        // Add the Fields to the word Doc.
        static void AddFieldsToDocument(Body body, Dictionary<string, FormField> fields)
        {

            List<KeyFieldReference> lookups = reader.ReadLookup<KeyFieldReference>(
                @"C:\Users\608138\OneDrive - Medibank Private Limited\AHM\Key Fields Reference.xlsx",
                "Sheet1"
            );

            //log.LogObject(lookups);

            body.Append(PrintStyle("Headings2", "Data Field Mapping"));

            // Create the table
            Table table = new Table();

            // ✅ Define the column widths explicitly
            TableGrid tableGrid = new TableGrid(
                new GridColumn(), 
                new GridColumn(), 
                new GridColumn(), 
                new GridColumn(),
                new GridColumn() 
            );
            table.AppendChild(tableGrid);

            // Define table properties
            table.AppendChild(CreateTableProperties(DXA(18.4)));
            
            // Header row
            TableRow headerRow = new TableRow();
            headerRow.Append(
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.Restart), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Field ID", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.None, 2), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Data Mapping", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.Restart), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Field Description", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.Restart), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Scripts", true)
                )
            );

            table.Append(headerRow);

            TableRow headerRow2 = new TableRow();
            headerRow2.Append(
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.Continue), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Field Name", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "Binding", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.Continue), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "", true)
                ),
                new TableCell(
                    StandardCellProperties(new CellProps(VerticalMergeType.Continue), ((int)Colours.AHMGrey).ToString("X6")),
                    PrintStyle("tableHeading", "", true)
                )
            );

            table.Append(headerRow2);

            // Data rows
            int bookmarkId = 1;
            foreach (var field in fields)
            {
                // Set up Bookmarks for each field
                BookmarkStart start = new BookmarkStart() { Name = field.Key, Id = bookmarkId++.ToString() };
                BookmarkEnd end = new BookmarkEnd() { Id = bookmarkId++.ToString() };

                TableRow row = new TableRow();

                // Any scripts associated with the field
                var binding = Regex.Match(field.Value.Binding, @"\$.(.*)$").Groups[1].Value;

                string fieldName = field.Value.Name;
                
                string keyno = Regex.Match(field.Value.Binding, @".*?keyno_(\d+)$").Groups[1].Value ?? "";

                var keyDesc = lookups
                    .Where(x => x.KeyNo == keyno.TrimStart('0'))
                    .Select(x => x.ShortDesc)
                    .FirstOrDefault() ?? "";

                string scripts = string.Join("\r\n", field.Value.Scripts);

                row.Append(
                    new TableCell(
                        StandardCellProperties(new CellProps(), ((int)Colours.AHMGrey).ToString("X6")),
                        start,
                        PrintStyle("table", field.Key, true),
                        end
                    ),
                    new TableCell(
                        StandardCellProperties(new CellProps()),
                        PrintStyle("table", fieldName)
                    ),
                    new TableCell(
                        StandardCellProperties(new CellProps()),
                        PrintStyle("table", binding)
                    ),
                    new TableCell(
                        StandardCellProperties(new CellProps()),
                        PrintStyle("table", keyDesc)
                    ),
                    new TableCell(
                        StandardCellProperties(new CellProps()),
                        PrintStyle("table", scripts)
                    )
                );

                table.Append(row);
            }

            body.Append(table);
        }

        static void AddCopyTextToDocument(Body body, SubformNode root)
        {
            body.Append(PrintStyle("Headings2", "Copy Requirements"));

            var table = CreateCopyRequirementsTable();
            var contentItems = GetContentItems(root);

            var groupedItems = contentItems
                .GroupBy(item =>
                    item.SubformLayout == "row" && !string.IsNullOrEmpty(item.ParentSubformName)
                        ? $"ROWGROUP::{item.ParentSubformName}"
                        : $"SINGLE::{Guid.NewGuid()}")
                .ToList();

            string lastSection = "", lastScript = "";

            foreach (var groupedItem in groupedItems)
            {
                var first = groupedItem.First();
                bool isRowLayout = first.SubformLayout == "row";

                string sectionName = first.Path.Count > 1 ? first.Path[1] : "All";
                string sectionCellText = sectionName == lastSection ? "" : sectionName;
                var sectionMerge = sectionName == lastSection ? VerticalMergeType.Continue : VerticalMergeType.Restart;
                lastSection = sectionName;

                string ruleText = string.Join(", ", first.ScriptIDs ?? []);
                var ruleMerge = ruleText == lastScript ? VerticalMergeType.Continue : VerticalMergeType.Restart;
                lastScript = ruleText;

                string bgColour = string.IsNullOrEmpty(ruleText)
                    ? ((int)Colours.White).ToString("X6")
                    : ((int)Colours.LightRed).ToString("X6");

                TableRow row = new();
                row.Append(CreateSectionCell(sectionCellText, sectionMerge));
                row.Append(CreateRuleCell(ruleText, ruleMerge, bgColour));

                if (isRowLayout)
                {
                    var innerTable = CreateInnerTable(groupedItem.ToList());
                    var copyCell = new TableCell(StandardCellProperties(new CellProps(VerticalMergeType.None), bgColour));
                    copyCell.Append(innerTable);
                    copyCell.Append(AddEmptyParagraphWithSpacing());
                    row.Append(copyCell);
                }
                else
                {
                    string bodyCopy = ExtractCopyText(first.Item);
                    if (string.IsNullOrWhiteSpace(bodyCopy)) continue;

                    row.Append(CreateCopyCell(bodyCopy, bgColour));
                }

                table.Append(row);
            }

            body.Append(table);
        }


        //###################################################
        // Word Document markup methods
        //###################################################

        static Table CreateCopyRequirementsTable()
        {
            var table = new Table();
            var tableGrid = new TableGrid();
            tableGrid.Append(new GridColumn()); // Section
            tableGrid.Append(new GridColumn()); // Rule
            tableGrid.Append(new GridColumn() { Width = DXA(14) }); // Copy

            table.AppendChild(tableGrid);
            table.AppendChild(CreateTableProperties(DXA(26.4)));

            var headerRow = new TableRow();
            string grey = ((int)Colours.AHMGrey).ToString("X6");

            headerRow.Append(
                new TableCell(StandardCellProperties(new CellProps(), grey), PrintStyle("tableHeading", "Section(s)")),
                new TableCell(StandardCellProperties(new CellProps(), grey), PrintStyle("tableHeading", "Rule(s)", true)),
                new TableCell(StandardCellProperties(new CellProps(), grey), PrintStyle("tableHeading", "Copy", true))
            );

            table.Append(headerRow);
            return table;
        }

        static TableCell CreateSectionCell(string text, VerticalMergeType merge)
        {
            return new TableCell(
                StandardCellProperties(new CellProps(merge), ((int)Colours.AHMGrey).ToString("X6")),
                PrintStyle("table", text, true)
            );
        }

        static TableCell CreateRuleCell(string text, VerticalMergeType merge, string bgColour)
        {
            return new TableCell(
                StandardCellProperties(new CellProps(merge), bgColour),
                PrintStyle("table", text, true)
            );
        }

        static TableCell CreateCopyCell(string bodyCopy, string bgColour)
        {
            var cell = new TableCell(StandardCellProperties(new CellProps(VerticalMergeType.None), bgColour));
            foreach (var line in bodyCopy.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                cell.Append(PrintStyle("table", line));
            }
            return cell;
        }

        static string ExtractCopyText(ContentItem content)
        {
            if (content.Type == "draw" && content.Draw != null)
            {
                return content.Draw.Content.Replace("\u2029", "\r\n").TrimEnd();
            }
            else if (content.Type == "reference")
            {
                return content.FragmentID;
            }

            return "";
        }

        static Paragraph AddEmptyParagraphWithSpacing()
        {
            var para = new Paragraph();
            var spacing = new SpacingBetweenLines
            {
                After = "0",
                Line = "240",
                LineRule = LineSpacingRuleValues.Auto
            };
            para.Append(new ParagraphProperties(spacing));
            return para;
        }


        // property for a column merge
        enum VerticalMergeType
        {
            None,
            Restart,
            Continue
        }

        record CellProps(VerticalMergeType VMerge = VerticalMergeType.None, int MergeColumns = 1, string VAlign = "top");

        static TableCellProperties StandardCellProperties(CellProps props, string bgColour = null)
        {
            // Vertical Alignment
            var alignment = props.VAlign?.ToLower() switch
            {
                "bottom" => TableVerticalAlignmentValues.Bottom,
                "centre" => TableVerticalAlignmentValues.Center,
                _ => TableVerticalAlignmentValues.Top
            };

            // Margins
            // Todo: 1. Move the standard width to initialization object/file
            var cellMargins = new TableCellMargin(
                new LeftMargin { Width = "142", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "142", Type = TableWidthUnitValues.Dxa }
            );

            var verticalAlignment = new TableCellVerticalAlignment { Val = alignment };

            var properties = new TableCellProperties();

            // Optional: Add background color if provided
            if (!string.IsNullOrEmpty(bgColour))
            {
                properties.Append(new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = bgColour 
                });
            }

            if (props.VMerge != VerticalMergeType.None)
            {
                var verticalMerge = new VerticalMerge
                {
                    Val = props.VMerge == VerticalMergeType.Restart
                        ? MergedCellValues.Restart
                        : MergedCellValues.Continue
                };

                properties.Append(verticalMerge);
            }
            else
            {
                if (props.MergeColumns != 1)
                    properties.Append(new GridSpan() { Val = props.MergeColumns });
            }

            properties.Append(verticalAlignment);
            properties.Append(cellMargins);

            return properties;
        }

        static Paragraph PrintStyle(string style, string text, bool bold = false)
        {
            Regex fieldRegex = new Regex(@"(DF\d{3}|BR\d{3}|TF\d{3})");
            Paragraph para = ParagraphStyle(style);

            string line = text.TrimEnd();
            int lastIndex = 0;

            foreach (Match match in fieldRegex.Matches(line))
            {
                // Normal text before match
                if (match.Index > lastIndex)
                {
                    string beforeText = line.Substring(lastIndex, match.Index - lastIndex);
                    para.Append(PrintTextWithStyling(beforeText, ((int)Colours.Black).ToString("X6"), bold, false));
                }

                // Highlighted match
                string highlight = match.Value switch
                {
                    var s when s.StartsWith("DF") => ((int)Colours.Red).ToString("X6"),
                    var s when s.StartsWith("BR") => ((int)Colours.Green).ToString("X6"),
                    var s when s.StartsWith("TF") => ((int)Colours.Blue).ToString("X6"),
                    _ => ((int)Colours.Black).ToString("X6")
                };

                if (!hyperlinkTags.Contains(match.Value))
                {
                    para.Append(
                        new Hyperlink(
                            PrintTextWithStyling(match.Value, highlight, bold, true)
                        )
                        {
                            Anchor = match.Value // must match a BookmarkStart.Name somewhere else
                        });
                    hyperlinkTags.Add(match.Value);
                }

                lastIndex = match.Index + match.Length;
            }

            // Remainder of the line
            if (lastIndex < line.Length)
            {
                string afterText = line.Substring(lastIndex);
                para.Append(PrintTextWithStyling(afterText, ((int)Colours.Black).ToString("X6"), bold, false));
            }
            
            return para;
        }

        static Paragraph ParagraphStyle(string style)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId() { Val = style }
                )
            );
        }

        static Run PrintTextWithStyling(string text, string colour, bool bold, bool underline)
        {
            Run run = new Run();
            run.Append(
                new RunProperties(
                    // Colour
                    new Color() { Val = colour, ThemeColor = null },
                    // Underline
                    underline ? new Underline() { Val = UnderlineValues.Single } : null,
                    // Add bold if specified
                    bold ? new Bold() : null
                ),
                new Text(text)
            );
                
            return run;

        }

        enum PageOrientation
        {
            Portrait,
            Landscape
        }

        static void StartNewSection(Body body, PageOrientation orient)
        {
            UInt32Value width = (orient == PageOrientation.Portrait) ? 11906U : 16838U;
            UInt32Value height = (orient == PageOrientation.Portrait) ? 16838U : 11906U;

            var sectionProps = new SectionProperties(
                new PageSize
                {
                    Width = width,
                    Height = height,
                    Orient = (orient == PageOrientation.Portrait)
                        ? PageOrientationValues.Portrait
                        : PageOrientationValues.Landscape
                },
                new PageMargin
                {
                    Top = 720,
                    Right = 720,
                    Bottom = 720,
                    Left = 720
                }
            );

            // This paragraph ends the current section and starts a new one
            var sectionBreakParagraph = new Paragraph(new ParagraphProperties(sectionProps));
            body.Append(sectionBreakParagraph);
        }

        // ahm Styles
        private static void AddStyles(MainDocumentPart mainPart)
        {
            StyleDefinitionsPart stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = new Styles();

            // #############################
            // Heading 1
            // #############################
            Style style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Headings1",
                CustomStyle = true
            };
            style.Append(new StyleName() { Val = "Headings 1" });

            // Style formatting
            style.Append(new BasedOn() { Val = "Normal" });
            style.Append(new NextParagraphStyle() { Val = "Normal" });
            style.Append(new StyleRunProperties(
                new FontSize() { Val = "42" },//(half-points)
                new RunFonts() { Ascii = "Proxima Nova Bl" } // Font
            ));
            style.Append(
                new StyleParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "240", // 240 twips = 12pt before
                        After = "120"   // 240 twips = 12pt after
                    }
                )
            );

            // Add the style to the styles part
            stylePart.Styles.Append(style);

            // #############################
            // Heading 2
            // #############################
            style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Headings2",
                CustomStyle = true
            };

            // Heading 2
            style.Append(new StyleName() { Val = "Headings 2" });

            // Style formatting
            style.Append(new BasedOn() { Val = "Normal" });
            style.Append(new NextParagraphStyle() { Val = "Normal" });
            style.Append(new StyleRunProperties(
                new FontSize() { Val = "26" },  // (half-points)
                new RunFonts() { Ascii = "Proxima Nova Bl" } // Font
            ));
            style.Append(
                new StyleParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "120", // 240 twips = 12pt before
                        After = "120"   // 240 twips = 12pt after
                    }
                )
            );
            // Add the style to the styles part
            stylePart.Styles.Append(style);

            // #############################
            // ahm Table Headings
            // #############################
            style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "tableHeading",
                CustomStyle = true
            };

            // Style Name
            style.Append(new StyleName() { Val = "Table Heading" });

            // Style formatting
            style.Append(new BasedOn() { Val = "Normal" });
            style.Append(new NextParagraphStyle() { Val = "Normal" });
            style.Append(new StyleRunProperties(
                new FontSize() { Val = "22" },  // (half-points)
                new RunFonts() { Ascii = "Proxima Nova Bl" } // Font
            ));
            style.Append(
                new StyleParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "150", // 240 twips = 12pt before
                    }
                )
            );
            // Add the style to the styles part
            stylePart.Styles.Append(style);

            // #############################
            // ahm Table text
            // #############################
            style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "table",
                CustomStyle = true
            };

            // paragrpah
            style.Append(new StyleName() { Val = "Table" });

            // Style formatting
            style.Append(new BasedOn() { Val = "Normal" });
            style.Append(new NextParagraphStyle() { Val = "Normal" });
            style.Append(new StyleRunProperties(
                new FontSize() { Val = "22" },  // (half-points)
                new RunFonts() { Ascii = "Proxima Nova Lt" } // Font
            ));
            style.Append(
                new StyleParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "120", // 240 twips = 12pt before
                    }
                )
            );
            // Add the style to the styles part
            stylePart.Styles.Append(style);

            // #############################
            // ahm Paragrpah
            // #############################
            style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "paragraph",
                CustomStyle = true
            };

            // paragrpah
            style.Append(new StyleName() { Val = "Paragraph" });

            // Style formatting
            style.Append(new BasedOn() { Val = "Normal" });
            style.Append(new NextParagraphStyle() { Val = "Normal" });
            style.Append(new StyleRunProperties(
                new FontSize() { Val = "22" },  // (half-points)
                new RunFonts() { Ascii = "Proxima Nova Lt" } // Font
            ));
            style.Append(
                new StyleParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "120", // 240 twips = 12pt before
                    }
                )
            );
            // Add the style to the styles part
            stylePart.Styles.Append(style);

        }

        private static TableProperties CreateTableProperties(string width, bool autofit = true)
        {
            var auto = (autofit) ? TableLayoutValues.Autofit : TableLayoutValues.Fixed;

            // Define table properties
            return new TableProperties(
                new TableWidth { Width = width, Type = TableWidthUnitValues.Dxa },
                new TableLayout { Type = auto },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 12 },
                    new BottomBorder { Val = BorderValues.Single, Size = 12 },
                    new LeftBorder { Val = BorderValues.Single, Size = 12 },
                    new RightBorder { Val = BorderValues.Single, Size = 12 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                )
            );
        }

       
        private static Table CreateInnerTable(List<ContentItemWithContext> rowItems)
        {
            double totalWidth = 0;
            var table = new Table();

            // Create TableGrid
            TableGrid tableGrid = new TableGrid();
            var first = rowItems.First();
            for (int i = 0; i < first.Columns; i++)
            {
                double width = 7;
                var columnWidthStr = first.ColumnWidths[i];

                if (!string.IsNullOrWhiteSpace(columnWidthStr) && columnWidthStr.EndsWith("mm"))
                {
                    var numericPart = columnWidthStr.Replace("mm", "").Trim();
                    if (double.TryParse(numericPart, out double parsedWidth))
                    {
                        width = parsedWidth / 10;
                        totalWidth += width;
                    }
                }

                tableGrid.Append(new GridColumn() { Width = DXA(width) });
            }
            // Setup table properties
            table.AppendChild(CreateTableProperties(DXA(totalWidth), false));

            table.Append(tableGrid);
            table.Append(RenderTableRow(rowItems, totalWidth));
            return table;
        }

        private static TableRow RenderTableRow(List<ContentItemWithContext> rowItems, double totalWidth)
        {
            var tableRow = new TableRow();
            int column = 0;
            foreach (var item in rowItems)
            {
                double width = totalWidth;
                int colSpan = item?.Item?.Draw?.CellSpan ?? 1;
                for (int i = column; i < colSpan + column; i++)
                {
                    var columnWidth = item?.ColumnWidths[i];
                    var numericPart = columnWidth.Replace("mm", "").Trim();
                    if (double.TryParse(numericPart, out double parsedWidth))
                    {
                        width = parsedWidth / 10;
                    }
                }

                TableCell cell = new TableCell(StandardCellProperties(new CellProps(VerticalMergeType.None, colSpan)), new TableCellWidth { Width = DXA(width), Type = TableWidthUnitValues.Dxa });

                string content = item?.Item?.Draw?.Content;

                if (!string.IsNullOrEmpty(content))
                {
                    foreach (string rawLine in content.Split("\r\n"))
                    {
                        string line = rawLine.TrimEnd();

                        var styledElement = PrintStyle("table", SanitizeForOpenXml(line));
                        if (styledElement != null)
                        {
                            cell.Append(styledElement);
                        }
                    }
                }

                tableRow.Append(cell);
                column++;
            }

            return tableRow;
        }

        //###################################################
        // Helper Methods
        //###################################################
        enum Colours
        {
            LightGrey = 0xD9D9D9,
            AHMGrey = 0xDDDDDD,
            DarkGrey = 0xA6A6A6,
            Black = 0x000000,
            White = 0xFFFFFF,
            Blue = 0x31849B,
            Red = 0xFF0000,
            LightRed = 0xFFE5E5,
            Green = 0x00B050
        }

        record ContentItemWithContext(List<string> Path, string SubformLayout, string ParentSubformName, List<string> ScriptIDs, List<string> FragmentIDs, ContentItem Item, int? Columns = null,    List<string>? ColumnWidths = null);

        private static List<ContentItemWithContext> GetContentItems(SubformNode node, List<string> path = null, int? parentColumns = null, List<string>? parentColumnWidths = null)
        {
            path ??= new List<string>();
            var currentPath = new List<string>(path) { node.Name };

            var result = new List<ContentItemWithContext>();

            // Determine current context (use node if set, else parent)
            var columns = node.Columns > 0 ? node.Columns : parentColumns;
            // Updated line to resolve CS0173 by ensuring both sides of the conditional expression are of the same type.
            // Convert the string[] to a List<string> using .ToList() for compatibility with the List<string> type.
            var columnWidths = node.ColumnWidths?.Length > 0 ? node.ColumnWidths.ToList() : parentColumnWidths;

            foreach (var contentItem in node.ContentItems)
            {
                switch (contentItem.Type)
                {
                    case "draw":
                    case "reference":
                        result.Add(new ContentItemWithContext(
                            currentPath,
                            node.Layout,
                            node.Name,
                            node.ScriptIDs,
                            node.FragmentIDs,
                            contentItem,
                            columns,
                            columnWidths
                        ));
                        break;

                    case "subform":
                        if (contentItem.Subform != null)
                        {
                            result.AddRange(GetContentItems(
                                contentItem.Subform,
                                currentPath,
                                columns,
                                columnWidths
                            ));
                        }
                        break;
                }
            }

            return result;
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
        private static StringValue DXA(double cm)
        {
            return ((int)(cm * 567)).ToString();
        }

        public static string SanitizeForOpenXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Replace non-breaking spaces with regular spaces
            text = text.Replace('\u00A0', ' ');

            // Remove invalid XML characters
            // Valid XML chars: https://www.w3.org/TR/xml/#charsets
            text = new string(text.Where(c =>
                c == 0x9 || c == 0xA || c == 0xD || (c >= 0x20 && c <= 0xD7FF) ||
                (c >= 0xE000 && c <= 0xFFFD) || (c >= 0x10000 && c <= 0x10FFFF)).ToArray());

            return text.Trim();
        }

    }
}
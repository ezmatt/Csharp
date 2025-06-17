using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Linq;

public class WordUtilities : IDisposable
{
     private readonly Body? _body;
     private WordprocessingDocument _wordDoc;

    public WordUtilities(string wordDocumentFileName)
    {
        try {
            _wordDoc = WordprocessingDocument.Open(wordDocumentFileName, true);
            _body = _wordDoc.MainDocumentPart?.Document.Body;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open file: {wordDocumentFileName}");
            Console.WriteLine($"Error: {ex.Message}");
        }
        
    }

    public void CloseandSaveWordDocument()
    {
         if (_wordDoc != null)
        {
            _wordDoc.MainDocumentPart?.Document.Save();
            _wordDoc.Dispose();
            _wordDoc = null;
        }
    }

    public void Dispose()
    {
        CloseandSaveWordDocument(); // Ensure cleanup
    }

    public void ReplaceText(string placeholder, string replacementText)
    {
        var paragraphs = _body.Descendants<Paragraph>().ToList();

        foreach (var paragraph in paragraphs)
        {
            // Get all text elements in the paragraph
            var textElements = paragraph.Descendants<Text>().ToList();

            // Concatenate all text in the paragraph
            string fullText = string.Join("", textElements.Select(t => t.Text));

            // Check if placeholder exists
            if (fullText.Contains(placeholder))
            {
                // Perform replacement
                fullText = fullText.Replace(placeholder, replacementText);

                // Clear existing text elements
                foreach (var textElement in textElements)
                {
                    textElement.Text = string.Empty;
                }

                // Assign new text to the first text element
                if (textElements.Any())
                {
                    textElements.First().Text = fullText;
                }
            }
        }
    }


    public void ReplaceText(Dictionary<string, string> replacements)
    {
        foreach (var replacement in replacements)
        {
            ReplaceText(replacement.Key, replacement.Value);
        }
    }
  
    public void CheckBox (string checkboxTag, Boolean isChecked)
    {
        string uncheckValue = "☐"; // Unicode 2610
        string checkValue = "☒";   // Unicode 2612

        try {
            foreach (SdtElement sdt in _body.Descendants<SdtElement>())
            {
                var tagElement = sdt.Descendants<DocumentFormat.OpenXml.Wordprocessing.Tag>().FirstOrDefault();

                // ✅ Make sure we are working with the correct checkbox based on BrandCode
                if (tagElement != null && tagElement.Val == checkboxTag)
                {
                    var checkbox = sdt.Descendants<SdtContentCheckBox>().FirstOrDefault();
                    if (checkbox == null) continue; // Skip if it's not a checkbox

                    // ✅ Set the checkbox to "Checked"
                    var checkedElement = checkbox.GetFirstChild<DocumentFormat.OpenXml.Office2010.Word.Checked>();
                    if (checkedElement != null)
                    {
                        checkedElement.Val = isChecked ? OnOffValues.One : OnOffValues.Zero;
                    }
                    
                    var textElement = sdt.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                        .FirstOrDefault(t => t.Text == uncheckValue); // Make sure we're modifying ☐, not other text
                    if (textElement != null)
                    {
                        textElement.Text = checkValue;
                    }
                    else
                    {
                        // ✅ If not found directly, look inside the associated table cell
                        var tableCell = sdt.Ancestors<DocumentFormat.OpenXml.Wordprocessing.TableCell>().FirstOrDefault();
                        if (tableCell != null)
                        {
                            var tableText = tableCell.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                                .FirstOrDefault(t => t.Text == uncheckValue);

                            if (tableText != null)
                            {
                                tableText.Text = checkValue;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

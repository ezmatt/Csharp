using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net.Mime;
using System.Dynamic;
using XDPToolKit.Models;
using System.Text;
using DocumentFormat.OpenXml.Presentation;
using System.Text.RegularExpressions;

namespace XDPToolKit
{
    namespace XdpAnalysis
    {
        public class XdpParser
        {
            private readonly XDocument _xdpDocument;
            private readonly XNamespace xfaNs;
            private readonly XElement _root;

            private readonly Dictionary<string, string> _fieldHashToId = new();
            private readonly Dictionary<string, string> _scriptHashToId = new();
            private readonly Dictionary<string, string> _fragmentHashToId = new();

            private readonly Dictionary<string, FormField> _allFields = new();
            private readonly Dictionary<string, FormScript> _allScripts = new();
            private readonly Dictionary<string, Fragment> _allFragments = new();

            private readonly Dictionary<string, int> _allSubforms = new();
            
            private int _fieldCounter = 1;
            private int _scriptCounter = 1;
            private int _fragmentCounter = 1;

            public XdpParser(string filePath)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("XDP file not found", filePath);

                _xdpDocument = XDocument.Load(filePath);
                _root = _xdpDocument?.Root;
                var firstElement = _root?.Descendants().FirstOrDefault();
                xfaNs = firstElement?.GetDefaultNamespace() ?? XNamespace.None;

                //Console.WriteLine($"Namespace: {xfaNs}"); // Debugging: Should print the actual namespace
            }

            public XElement GetRootSubForm()
            {
                // Find the first subform element under the template element
                return _root.Descendants(xfaNs + "template")
                    .Elements(xfaNs + "subform")
                    .FirstOrDefault();
            }

            public IEnumerable<XElement> GetAllOfType(string elementType)
            {
                return _xdpDocument.Descendants(xfaNs + elementType);
            }

            public XElement? GetParentNode(XElement element)
            {
                return element.Parent;
            }

            public XElement? FindElementByTag(string tag)
            {
                return FindElementByTag(_root, tag);
            }

            public XElement? FindElementByTag(XElement parent, string tag)
            {
                // Check if the current node matches the tag
                if (parent.Name.LocalName == tag)
                {
                    return parent;
                }

                // Recursively search child elements
                foreach (var child in parent.Elements())
                {
                    var found = FindElementByTag(child, tag);
                    if (found != null)
                    {
                        return found; // Stop searching once found
                    }
                }

                return null; // Not found
            }

            public List<XElement> FindAllElementsByTag(string tag)
            {
                return FindAllElementsByTag(_root, tag);
            }

            public List<XElement> FindAllElementsByTag(XElement parent, string tag)
            {
                List<XElement> results = new List<XElement>();

                // Check if the current node matches the tag
                if (parent.Name.LocalName == tag)
                {
                    results.Add(parent);
                }

                // Recursively search child elements
                foreach (var child in parent.Elements())
                {
                    results.AddRange(FindAllElementsByTag(child, tag)); // ✅ Collect matches from recursion
                }

                return results;
            }

            public XElement FindAllElementsByTagParent(XElement child, string tag)
            {
                if (child == null)
                {
                    return null;
                }

                // Check if the current node matches the tag
                if (child.Name.LocalName == tag)
                {
                    return child;
                }

                // Recursively search child elements
                var found = FindAllElementsByTagParent(child.Parent, tag);
                if (found != null)
                {
                    return found; // Stop searching once found
                }

                return null; // Not found
            }

            public XElement FindParentElementByAttribute(XElement child, string attribute)
            {
                if (child == null)
                {
                    return null;
                }

                // Check if the current node has the required "attribute"
                if (child.Attribute(attribute) != null && child.Name.LocalName != "event")
                {
                    //Console.WriteLine(child.Name.LocalName);
                    return child;
                }

                // Recursively search child elements
                var found = FindParentElementByAttribute(child.Parent, attribute);
                if (found != null)
                {
                    return found; // Stop searching once found
                }

                return null; // Not found
            }

            public bool HasSearchTextInEntireXDP(string searchText)
            {
                return _root.DescendantsAndSelf() // Include root and all descendants
                .Any(node => node.Attributes().Any(attr => attr.Value.Contains(searchText)) ||
                    node.Value.Contains(searchText));

            }

            public List<XElement> GetNodesWithSearchTextInEntireXDP(string searchText)
            {
                return _root.DescendantsAndSelf() // Include root and all descendants
                .Where(node => node.Attributes().Any(attr => attr.Value.ToLower().Contains(searchText)) ||
                    node.Value.Contains(searchText)).ToList();

            }

            public List<XElement> FindSpecificNodesByAttribute(string searchText, string element = "")
            {
                return _root.DescendantsAndSelf(xfaNs + element) // Get all nodes, including root
                            .Where(node => node.Attributes().Any(attr => attr.Value.ToLower().Contains(searchText)))  // search node attribute
                            .ToList();
            }

            public List<XElement> FindSpecificNodesByValue(string searchText, string element = "")
            {

                return _root.DescendantsAndSelf(xfaNs + element) // Get all nodes, including root
                            .Where(node => node.Value.ToLower().Contains(searchText.ToLower()))  // Search node value
                            .ToList();
            }

            public List<string> GetAllContainerElementsByAttribute(XElement node, string attributeName = "")
            {
                List<string> containerNodes = new();

                if (node == null)
                {
                    return null;
                }

                XElement parent = node.Parent;

                // Recursively search parent elements
                while (parent != null)
                {
                    if (parent.Name.LocalName == "event")
                    {
                        parent = parent.Parent;
                        continue;
                    }

                    // WHICSLetter is the highest subform in the hierarchy, and we don't need that
                    if (parent.Parent != null && parent.Parent?.Attribute("name") != null && parent.Parent?.Attribute("name")?.Value == "WHICSLetter")
                    {
                        break;
                    }

                    // Check if the current node has the required "attribute"
                    if (parent.Attribute(attributeName) != null)
                    {
                        containerNodes.Add(parent?.Attribute(attributeName)?.Value);
                    }
                    // Move to the parent element
                    parent = parent.Parent;
                }

                return containerNodes;
            }

            public XElement GetHighestSubform(XElement node)
            {
                XElement highestSubForm = null;

                XElement parent = node.Parent;

                // Recursively search child elements
                while (parent != null)
                {
                    XElement grandParent = parent.Parent;
                    if (grandParent != null && grandParent.Name.LocalName == "subform" && grandParent?.Attribute("name")?.Value == "WHICSLetter")
                    {
                        highestSubForm = parent;
                    }
                    // Move to the parent element
                    parent = parent.Parent;
                }

                return highestSubForm;
            }

            public List<XElement> GetNodesByTag(string element)
            {
                return [.. _xdpDocument.Descendants(xfaNs + element)];
            }

            //##################################################################################
            //## Functions for interrogating the XDP file and extracting information
            //##################################################################################

            // Get all the scripts associated with a given element
            private List<FormScript> GetScripts(XElement element)
            {

                List<FormScript> result = [];

                foreach (var formEvent in element.Elements(xfaNs + "event"))
                {

                    FormScript formscript = new FormScript
                    {
                        Event = formEvent.Attribute("name")?.Value ?? "[Unnamed]",
                        Code = formEvent.Element(xfaNs + "script")?.Value?.Trim() ?? ""
                    };

                    result.Add(formscript);
                }

                return result;

            }

            // Get or add a script ID based on its code
            private string GetOrAddScriptId(FormScript script)
            {
                string hash = $"{script.Code}";
                if (!_scriptHashToId.TryGetValue(hash, out var id))
                {
                    id = $"BR{_scriptCounter++:D3}";
                    _scriptHashToId[hash] = id;
                    _allScripts[id] = script;
                }
                return id;
            }

            // Get all the fields and relevant bindings as well as any associated rules
            public List<FormField> GetFields(XElement subFormNode)
            {
                List<FormField> fields = [];

                foreach (XElement node in subFormNode.Elements(xfaNs + "field"))
                {
                    var name = node.Attribute("name")?.Value;
                    var fieldID = node.Attribute("id")?.Value ?? "[NoID]";

                    // Skip if the name is empty...
                    // Not sure why this would happen.
                    //if (string.IsNullOrEmpty(fieldID)) continue;

                    var bindRef = node.Element(xfaNs + "bind")?.Attribute("ref")?.Value ?? "";

                    var scripts = GetScripts(node);
                    List<string> scriptIDs = scripts.Select(script => GetOrAddScriptId(script)).ToList(); // ✅ Store IDs

                    FormField formField = new FormField
                    {
                        Name = name,
                        Binding = bindRef,
                        Scripts = scriptIDs,
                        FieldID = fieldID,
                    };
                    fields.Add(formField);
                }

                return fields;

            }

            // Get or add a field ID based on its name and binding
            private string GetOrAddFieldId(FormField field)
            {
                string hash = (!string.IsNullOrEmpty(field.Binding)) ? field.Name+"|"+field.Binding : field.FieldID;
                if (!_fieldHashToId.TryGetValue(hash, out var id))
                {
                    id = $"DF{_fieldCounter++:D3}";
                    _fieldHashToId[hash] = id;
                    _allFields[id] = field;
                }
                return id;
            }

            private string GetFieldIDfromXDPID(string fieldID)
            {
                string cleaned = fieldID.TrimStart('#');
                return _allFields.FirstOrDefault(kvp => kvp.Value.FieldID == cleaned).Key ?? string.Empty;

            }

            // Get all the fields and relevant bindings as well as any associated rules
            public List<Fragment> GetFragments(XElement subFormNode)
            {
                List<Fragment> fragments = [];

                if (subFormNode.Attribute("usehref") != null )
                { 
                    var usehref = subFormNode.Attribute("usehref")?.Value;
                    string path = usehref.Replace(@"\\", @"\").Replace("..\\", ""); // normalize
                    var matches = Regex.Match(path, @"^(.*)\\([^\\]+?)(?:\.xdp)\#.*$");

                    var name = matches.Groups[2].Value;
                    var fragmentLocation = matches.Groups[1].Value;

                    FragmentPosition positions = new FragmentPosition
                    {
                        x = (subFormNode.Attribute("x") == null) ? "0" : subFormNode.Attribute("x").Value,
                        y = (subFormNode.Attribute("y") == null) ? "0" : subFormNode.Attribute("y").Value,
                        w = (subFormNode.Attribute("w") == null) ? "0" : subFormNode.Attribute("w").Value,
                        h = (subFormNode.Attribute("h") == null) ? "0" : subFormNode.Attribute("h").Value,
                    };

                    Fragment fragment = new Fragment
                    {
                        Name = name,
                        FragmentLocation = fragmentLocation,
                        PageLocation = positions,
                    };
                    fragments.Add(fragment);
                }

                return fragments;

            }

            // Get or add a field ID based on its name and binding
            private string GetOrAddFragmentId(Fragment fragment)
            {
                string hash = $"{fragment.Name}";
                if (!_fragmentHashToId.TryGetValue(hash, out var id))
                {
                    id = $"TF{_fragmentCounter++:D3}";
                    _fragmentHashToId[hash] = id;
                    _allFragments[id] = fragment;
                }
                return id;
            }

            public DrawItem GetDrawDetailsFromElement(XElement drawElement, string parentLayout = "tb")
            {
                var name = drawElement.Attribute("name")?.Value ?? "[No Name]";
                int cellSpan = 1;

                if (parentLayout == "row" && drawElement.Attribute("colSpan") != null)
                {
                    int.TryParse(drawElement.Attribute("colSpan")?.Value, out cellSpan);
                }

                var valueElem = drawElement.Element(xfaNs + "value");
                if (valueElem == null) return null;

                string type = "text";
                string content = null;

                XNamespace xhtmlNs = "http://www.w3.org/1999/xhtml";

                // 1. Check for image
                var imageHref = valueElem.Element(xfaNs + "image")?.Attribute("href")?.Value;
                if (!string.IsNullOrWhiteSpace(imageHref))
                {
                    type = "image";
                    content = imageHref.Trim();
                }
                // 2. Check for exData text
                else if (!string.IsNullOrWhiteSpace(valueElem.Element(xfaNs + "exData")?.Value))
                {
                    var exDataElem = valueElem.Element(xfaNs + "exData");
                    var bodyElem = exDataElem?.Element(xhtmlNs + "body");
                    if (bodyElem != null)
                    {
                        content = ExtractTextFromHtml(bodyElem, xfaNs);
                    }
                }
                // 3. Fallback to raw value (plain text)
                else if (!string.IsNullOrWhiteSpace(valueElem.Value))
                {
                    content = valueElem.Value.Trim();
                }

                if (string.IsNullOrWhiteSpace(content)) return null;

                return new DrawItem
                {
                    Name = name,
                    Type = type,
                    Content = content,
                    CellSpan = cellSpan
                };
            }


            public List<DrawItem> GetDrawDetails(XElement subForm)
            {
                var layout = subForm.Attribute("layout")?.Value ?? "tb";
                return subForm.Elements(xfaNs + "draw")
                .Select(draw => GetDrawDetailsFromElement(draw, layout))
                .Where(draw => draw != null)
                .ToList();
            }

            string ExtractTextFromHtml(XElement element, XNamespace xfaNs)
            {
                var sb = new StringBuilder();
                XNamespace dataXfaNs = "http://www.xfa.org/schema/xfa-data/1.0/";

                foreach (var node in element.Nodes())
                {
                    if (node is XText textNode)
                    {
                        sb.Append(textNode.Value);
                    }
                    else if (node is XElement elem)
                    {
                        switch (elem.Name.LocalName)
                        {
                            case "br":
                                sb.AppendLine();
                                break;

                            case "p":
                                sb.AppendLine(ExtractTextFromHtml(elem, xfaNs));
                                //sb.AppendLine();
                                break;

                            case "ol":
                            case "ul":
                                sb.AppendLine(ExtractTextFromHtml(elem, xfaNs));
                                break;

                            case "li":
                                sb.Append("- "); // or use numbers if inside <ol>
                                sb.AppendLine(ExtractTextFromHtml(elem, xfaNs));
                                break;

                            case "span":
                                var embedAttr = elem.Attribute(dataXfaNs + "embed");
                                if (embedAttr != null)
                                {
                                    sb.Append($"{GetFieldIDfromXDPID(embedAttr.Value)}");
                                }
                                else
                                {
                                    sb.Append(ExtractTextFromHtml(elem, xfaNs));
                                }
                                break;

                            default:
                                sb.Append(ExtractTextFromHtml(elem, xfaNs));
                                break;
                        }
                    }
                }

                var result = sb.ToString();
                return string.IsNullOrWhiteSpace(result) ? result : result.Trim();

            }

            // Parse a subform and extract its details
            public SubformNode ParseSubform(XElement subform, string name)
            {
                if (name == null)
                {
                    name = subform.Attribute("name")?.Value ?? "[UnnamedSubform]";
                }

                var node = new SubformNode
                {
                    Name = name,
                    Layout = subform.Attribute("layout")?.Value ?? "[Standard]"
                };

                if (node.Layout == "table")
                {
                    node.ColumnWidths = subform.Attribute("columnWidths")?.Value.Split(" ");
                    node.Columns = node.ColumnWidths.Length;
                }
                
                // Deduplicated Scripts directly under this subform
                var scripts = GetScripts(subform);
                foreach (var script in scripts)
                {
                    node.ScriptIDs.Add(GetOrAddScriptId(script));
                }

                // Deduplicated Fields
                var rawFields = GetFields(subform);
                foreach (var field in rawFields)
                {
                    node.FieldIDs.Add(GetOrAddFieldId(field));
                }

                // Preserve the order of the items in the XDP
                node.ContentItems = GetContentItems(subform, node);

                // Handle fragments in pageArea
                foreach (var pageSet in subform.Elements(xfaNs + "pageSet"))
                {
                    foreach (var pageArea in pageSet.Elements(xfaNs + "pageArea"))
                    {
                        var pageAreaName = pageArea.Attribute("name")?.Value ?? "UnnamedPageArea";
                        Dictionary<string, string> fragmentIDs = [];
                        foreach (var subformInPageArea in pageArea.Elements(xfaNs + "subform"))
                        {
                            // Deduplicated Fragments
                            var fragments = GetFragments(subformInPageArea);
                            
                            foreach (var fragment in fragments)
                            {
                                var fragmentID = GetOrAddFragmentId(fragment);
                                node.FragmentIDs.Add(fragmentID);
                                fragmentIDs.TryAdd(fragmentID, fragment.Name);
                            }
                            
                        }
                        node.PageAreaFragments.Add(pageAreaName, fragmentIDs);
                    }
                }

                return node;
            }

            public FormJsonModel BuildFormModel()
            {
                // Find the root subform from the loaded XDP document
                XElement rootSubform = GetRootSubForm();

                if (rootSubform == null)
                    throw new Exception("Root subform not found.");

                // Reset de-duplication state
                _fieldHashToId.Clear();
                _scriptHashToId.Clear();
                _allFields.Clear();
                _allScripts.Clear();
                _fieldCounter = 1;
                _scriptCounter = 1;

                // Parse and build model
                var rootNode = ParseSubform(rootSubform, null);

                return new FormJsonModel
                {
                    Fields = _allFields,
                    Scripts = _allScripts,
                    Fragments = _allFragments,
                    RootSubform = rootNode
                };
            }

            public List<ContentItem> GetContentItems(XElement subForm, SubformNode node)
            {
                var contentItems = new List<ContentItem>();
                var layout = subForm.Attribute("layout")?.Value ?? "tb";

                foreach (var child in subForm.Elements())
                {
                    // Handle <draw> elements
                    if (child.Name == xfaNs + "draw")
                    {
                        var draw = GetDrawDetailsFromElement(child, layout);
                        if (draw != null)
                        {
                            contentItems.Add(new ContentItem
                            {
                                Type = "draw",
                                Name = draw.Name,
                                Draw = draw
                            });
                        }
                    }

                    // Handle <subform> elements
                    else if (child.Name == xfaNs + "subform")
                    {
                        var useHref = child.Attribute("usehref")?.Value;
                        string name = child.Attribute("name")?.Value ?? "[Unnamed]";

                        if (_allSubforms.TryGetValue(name, out int counter))
                        {
                            counter++;
                            _allSubforms[name] = counter;
                            name = $"{name}_{counter}"; // Append counter to name for uniqueness
                        }
                        else
                        {
                            _allSubforms[name] = 1;
                        }

                        if (!string.IsNullOrWhiteSpace(useHref))
                        {

                            // Deduplicated Fragments
                            var fragments = GetFragments(child);
                            var fragmentID = "";
                            foreach (var fragment in fragments)
                            {
                                fragmentID = GetOrAddFragmentId(fragment);
                            }

                            contentItems.Add(new ContentItem
                            {
                                Type = "reference",
                                Name = name,
                                FragmentID = fragmentID
                            });
                        }
                        else
                        {
                            var subNode = ParseSubform(child, name);
                            contentItems.Add(new ContentItem
                            {
                                Type = "subform",
                                Name = name,
                                Subform = subNode
                            });
                        }
                    }

                    // Optional: Handle <field> elements if needed
                    // else if (child.Name == xfaNs + "field") { ... }
                }

                return contentItems;
            }


        }
    }

}

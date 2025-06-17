using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static XDPToolKit.XdpAnalysis.XdpParser;

namespace XDPToolKit.Models
{
    // Subform Node Structure
    public class SubformNode
    {
        public string Name { get; set; }
        public string Layout { get; set; }
        public int Columns { get; set; }
        public string[] ColumnWidths { get; set; }
        public string CellSpan { get; set; }
        public List<DrawItem> drawItems { get; set; } = new();
        public List<string> FieldIDs { get; set; } = new();
        public List<string> ScriptIDs { get; set; } = new();
        public List<string> FragmentIDs { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> PageAreaFragments { get; set; } = new();
        public List<SubformNode> Children { get; set; } = new();
        public List<ContentItem> ContentItems { get; set; } = new();

    }
}

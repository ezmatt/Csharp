using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static XDPToolKit.XdpAnalysis.XdpParser;

namespace XDPToolKit.Models
{
    // Form Field Structure
    public class FormField
    {
        public string Name { get; set; }
        public string Binding { get; set; }
        public List<string> Scripts { get; set; } = new();
        public string FieldID { get; set; }
    }
}

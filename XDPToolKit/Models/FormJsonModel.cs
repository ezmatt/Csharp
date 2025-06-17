using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDPToolKit.Models
{
    public class FormJsonModel
    {
        public Dictionary<string, FormField> Fields { get; set; } = new();
        public Dictionary<string, FormScript> Scripts { get; set; } = new();
        public Dictionary<string, Fragment> Fragments { get; set; } = new();
        public SubformNode RootSubform { get; set; }
    }
}

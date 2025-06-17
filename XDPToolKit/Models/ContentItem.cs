using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDPToolKit.Models
{

    public class ContentItem
    {
        public string Type { get; set; } // "draw", "subform", "reference"
        public string Name { get; set; }
        public string FragmentID { get; set; }
        public DrawItem Draw { get; set; }
        public SubformNode Subform { get; set; }
    }

}

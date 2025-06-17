using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDPToolKit.Models
{
    // Draw Item Structure
    public class DrawItem
    {
        public string Name { get; set; }
        public string Type { get; set; } // e.g., "text", "image", "exData"
        public string Content { get; set; }
        public int CellSpan { get; set; }
    }
}

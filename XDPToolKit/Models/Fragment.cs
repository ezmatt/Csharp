using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XDPToolKit.Models
{
    public class Fragment
    {
        public string Name { get; set; }
        public string FragmentLocation { get; set; }
        public FragmentPosition PageLocation { get; set; }

    }
}

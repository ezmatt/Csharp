using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AEMProductUtilsSearch
{
    public class FormData
    {
        public string Form { get; set; }
        public string Folder { get; set; }
        public List<FieldData> Fields { get; set; } = new List<FieldData>();
    }

    public class FieldData
    {
        public string SubForm { get; set; }
        public string Search { get; set; }
        public string Field { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }
    }
}

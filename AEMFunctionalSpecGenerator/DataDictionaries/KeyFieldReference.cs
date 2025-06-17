using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLibrary;

namespace AEMFunctionalSpecGenerator.DataDictionaries
{
    public class KeyFieldReference : LookupBase
    {

        [ExcelColumn("Key Field Number")]
        [NormalizeZeros(0)]
        public string KeyNo { get; set; }

        [ExcelColumn("Short Name")]
        public string ShortDesc { get; set; }

        [ExcelColumn("Length")]
        public string FieldLength { get; set; }

        [ExcelColumn("Type")]
        public string Type { get; set; }

        [ExcelColumn("Type Detail")]
        public string TypeDetail { get; set; }

        public string? Description { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public abstract class LookupBase { }

    [AttributeUsage(AttributeTargets.Property)]
    public class NormalizeZerosAttribute : Attribute 
    {
        public int TotalLength { get; }

        public NormalizeZerosAttribute(int totalLength)
        {
            TotalLength = totalLength;
        }

    }

    public class ExcelColumnAttribute(string columnName) : Attribute
    {
        public string ColumnName { get; } = columnName;
    }

}

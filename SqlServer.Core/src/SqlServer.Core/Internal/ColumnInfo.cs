using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    public struct ColumnInfo
    {
        public DataType DataType;
        public int Length;
        public string Name;
        public bool IsNullable;
    }
}

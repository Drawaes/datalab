using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    public enum DataType : byte
    {
        Null = 0x1F,
        TinyInt = 0x30,
        Bit = 0x32,
        SmallInt = 0x34,
        Int = 0x38,
        SmallDateTime = 0x3A,
        Real = 0x3B,
        Money = 0x3C,
        DateTime = 0x3D,
        Float = 0x3E,
        SmallMoney = 0x7A,
        BigInt = 0x7F,

        Guid = 0x24,
        IntN = 0x26,
        Decimal = 0x37,
        Numeric = 0x3F,
        FloatN = 0x6D,
        MoneyN = 0x6E,
        DateTimeN = 0x6F,
        DateN = 0x28,
        TimeN = 0x29,
        DateTime2N = 0x2A,
        DateTimeOffsetN = 0x2B,
        CharType = 0x2F,
        VarChar = 0x27,
        Binary = 0x2D,
        VarBinary = 0x25,
        BigVarBin = 0xA5,
        BigVarChar = 0xA7,
        BigBinary = 0xAD,
        BigChar = 0xAF,
        NVarChar = 0xE7,
        NChar = 0xEF,
        Xml = 0xF1,
        Udt = 0xF0,
        Text = 0x23,
        Image = 0x22,
        NText = 0x63,
        SSVariant = 0x62,
    }
}

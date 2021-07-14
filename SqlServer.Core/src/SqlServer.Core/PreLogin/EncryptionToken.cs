using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.PreLogin
{
    internal enum EncryptionToken : byte
    {
        Encrypt_Off = 0x00,
        Encrypt_On = 0x01,
        Encrypt_Not_Supported = 0x02,
        Encrypt_Required = 0x03,
    }
}

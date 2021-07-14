using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.PreLogin
{
    internal enum PreLoginTokens : byte
    {
        Version = 0x00,
        Encryption = 0x01,
        InstOpt = 0x02,
        ThreadId = 0x03,
        Mars = 0x04,
        TraceId = 0x05,
        FedAuthRequired = 0x06,
        NonceOpt = 0x07,
        Terminator = 0xFF,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    [Flags]
    internal enum StatusType : byte
    {
        Normal = 0x00,
        EndOfMessage = 0x01,
        IgnoreEvent = 0x02,
        ResetConnection = 0x08,
        ResetConnectionSkipTran = 0x10,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    internal enum PacketType : byte
    {
        SqlBatch = 1,
        PreTds7Login = 2,
        Rpc = 3,
        TabularResult = 4,
        AttentionSignal = 6,
        BulkLoadData = 7,
        FederatedAuthenticationToken = 8,
        TransactionManagerRequest = 14,
        Tds7Login = 16,
        Sspi = 17,
        PreLogin = 18,
        LoginAck = 0xAD,
    }
}

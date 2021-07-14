using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core
{
    public enum SqlConnectionState
    {
        None,
        PreLoginSent,
        PreLoginReceived,
        TlsHandshake,
        HandshakeCompleted,
        LoginAckCompleted,
        LoginSent,
        QueryRunning
    }
}

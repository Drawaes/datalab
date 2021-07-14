using System;
using SqlServer.Core.Internal;

namespace SqlServer.Core
{
    internal class ServerLogin
    {
        private string _database;
        private string _language;

        public bool LoginAckReceived { get; private set; }

        public ServerLogin(ReadOnlySpan<byte> readPacket)
        {
            var originalBuffer = readPacket;

            while (readPacket.Length > 0)
            {
                switch ((DataStreamTokenTypes)readPacket[0])
                {
                    case DataStreamTokenTypes.ENVCHANGE_TOKEN:
                        ProcessExchangeToken(ref readPacket);
                        break;
                    case DataStreamTokenTypes.INFO_TOKEN:
                        ProcessInfoToken(ref readPacket);
                        break;
                    case DataStreamTokenTypes.LOGINACK_TOKEN:
                        ProcessLoginAckToken(ref readPacket);
                        LoginAckReceived = true;
                        break;
                    default:
                        return;
                        //throw new InvalidOperationException($"Unknown token {(DataStreamTokenTypes)readPacket[0]}");
                }
            }
        }

        private void ProcessLoginAckToken(ref ReadOnlySpan<byte> buffer)
        {
            buffer = buffer.Slice(1).ReadLittleEndian<ushort>(out var length);
            var data = buffer.Slice(0, length);
            buffer = buffer.Slice(length);
        }

        private void ProcessInfoToken(ref ReadOnlySpan<byte> buffer)
        {
            buffer = buffer.Slice(1).ReadLittleEndian<ushort>(out var length);
            var data = buffer.Slice(0, length);
            buffer = buffer.Slice(length);
        }

        private void ProcessExchangeToken(ref ReadOnlySpan<byte> buffer)
        {
            buffer = buffer.Slice(1).ReadLittleEndian<ushort>(out var length);
            var data = buffer.Slice(0, length);
            buffer = buffer.Slice(length);

            switch ((EnvironmentChangeType)data[0])
            {
                case EnvironmentChangeType.Database:
                    data.Slice(1).ReadBVarChar(out _database).ReadBVarChar(out var oldDatabase);
                    break;
                case EnvironmentChangeType.SqlCollation:
                case EnvironmentChangeType.PacketSize:
                    break;
                case EnvironmentChangeType.Language:
                    data.Slice(1).ReadBVarChar(out _language);
                    break;
                default:
                    throw new NotImplementedException($"Not implemented {(EnvironmentChangeType)data[0]}");
            }
        }
    }
}

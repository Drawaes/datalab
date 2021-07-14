using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlServer.Core.Internal;

namespace SqlServer.Core.PreLogin
{
    internal class ServerPreLogin
    {
        private readonly EncryptionToken _encryptionType;

        public ServerPreLogin(ReadOnlySpan<byte> readPacket)
        {
            var originalBuffer = readPacket;

            var token = TokenExtensions.ReadToken(ref readPacket);
            do
            {
                switch ((PreLoginTokens)token.TokenType)
                {
                    case PreLoginTokens.Encryption:
                        var value = (EncryptionToken)originalBuffer[token.Position - PacketReader.PacketHeaderSize];
                        break;
                    case PreLoginTokens.Version:
                        var version = originalBuffer.Slice(token.Position - PacketReader.PacketHeaderSize, token.Length).ToArray();
                        break;
                    case PreLoginTokens.InstOpt:
                        var instResult = originalBuffer[token.Position - PacketReader.PacketHeaderSize] == 0;
                        break;
                }


                token = TokenExtensions.ReadToken(ref readPacket);
            } while (token.TokenType != 0xFF);
        }
    }
}

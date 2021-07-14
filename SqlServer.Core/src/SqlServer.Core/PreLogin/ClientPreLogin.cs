using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlServer.Core.Internal;

namespace SqlServer.Core.PreLogin
{
    internal struct ClientPreLogin
    {
        private const int TokenLength = 5;
        private const int NumberOfTokens = 6;
        private const int TotalTokenSize = NumberOfTokens * TokenLength + 1;
        private static readonly byte[] s_version = new byte[] { 04, 07, 09, 0xfc, 00, 00 };

        public static void WritePreLogin(ref PipeWriter bufferToWriteTo)
        {
            var fullSpan = bufferToWriteTo.GetSpan(1000);
            var tokenSpan = fullSpan.Slice(PacketReader.PacketHeaderSize, TotalTokenSize);
            var dataSpan = fullSpan.Slice(PacketReader.PacketHeaderSize + TotalTokenSize);

            ushort currentDataOffset = TotalTokenSize;

            TokenExtensions.WriteToken((byte)PreLoginTokens.Version, ref tokenSpan, ref dataSpan, s_version, ref currentDataOffset);
            TokenExtensions.WriteToken((byte)PreLoginTokens.Encryption, ref tokenSpan, ref dataSpan, (byte)0, ref currentDataOffset);
            TokenExtensions.WriteToken((byte)PreLoginTokens.InstOpt, ref tokenSpan, ref dataSpan, (byte)0, ref currentDataOffset);
            TokenExtensions.WriteToken((byte)PreLoginTokens.ThreadId, ref tokenSpan, ref dataSpan, (uint)1, ref currentDataOffset);
            TokenExtensions.WriteToken((byte)PreLoginTokens.Mars, ref tokenSpan, ref dataSpan, (byte)0, ref currentDataOffset);
            TokenExtensions.WriteToken((byte)PreLoginTokens.FedAuthRequired, ref tokenSpan, ref dataSpan, (byte)1, ref currentDataOffset);
            tokenSpan.WriteBigEndian((byte)0xFF);

            var totalLength = (ushort)(currentDataOffset + PacketReader.PacketHeaderSize);
            PacketHeader.WriteSingleMessage(fullSpan, PacketType.PreLogin, totalLength);

            bufferToWriteTo.Advance(totalLength);
        }
    }
}

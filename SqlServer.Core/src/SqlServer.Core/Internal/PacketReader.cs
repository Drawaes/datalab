using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    public static class PacketReader
    {
        public static readonly int PacketHeaderSize = Unsafe.SizeOf<PacketHeader>();

        internal static PacketResult ReadPacket(ref ReadOnlySequence<byte> inBuffer, out ReadOnlySequence<byte> packetBuffer, out PacketHeader header)
        {
            if (inBuffer.Length < PacketHeaderSize)
            {
                packetBuffer = default;
                header = default;
                return PacketResult.Incomplete;
            }

            header = inBuffer.Read<PacketHeader>();
            if (inBuffer.Length < header.Length)
            {
                packetBuffer = default;
                return PacketResult.Incomplete;
            }

            packetBuffer = inBuffer.Slice(PacketHeaderSize, header.Length - PacketHeaderSize);
            inBuffer = inBuffer.Slice(header.Length);
            return PacketResult.Complete;
        }
    }
}

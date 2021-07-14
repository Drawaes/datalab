using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PacketHeader
    {
        public PacketType Type;
        public StatusType Status;
        private ushort _length;
        private ushort _spid;
        public byte PacketId;
        public byte Window;

        public ushort Length { get => (ushort)((_length << 8) | (_length >> 8)); set => _length = (ushort)((value << 8) | (value >> 8)); }
        public ushort Spid { get => (ushort)((_spid << 8) | (_spid >> 8)); set => _spid = (ushort)((value << 8) | (value >> 8)); }

        public static Span<byte> WriteSingleMessage(Span<byte> span, PacketType type, ushort length)
        {
            var ph = new PacketHeader()
            {
                Length = length,
                PacketId = 1,
                Spid = 0,
                Status = StatusType.EndOfMessage,
                Type = type,
                Window = 0
            };

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), ph);
            return span.Slice(Unsafe.SizeOf<PacketHeader>());
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SqlServer.Core.Internal;

namespace SqlServer.Core
{
    internal static class ClientLogin
    {
        private const int DataOffset = 0x5E;
        private const uint Version = 0x74000004;
        private static int OptionFlags = 0x1000_03E0; //CreateOptionsFlags();
        private static byte[] _extension = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x04, 0x01, 0x00, 0x00, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0xFF };
        private static Random _clientIdRandom = new Random();

        private static int CreateOptionsFlags()
        {
            var flags = 1 << 5; //UseDB
            flags |= 1 << 6; // Init DB Fatal
            flags |= 1 << 7; // Set Lang On
            flags |= 1 << 8; // Init lang fatal
            flags |= 1 << 9; // ODBC On
            return flags;
        }

        public static void WriteLogin(ref PipeWriter output, SqlConnectionDetails connectionDetails)
        {
            var length = connectionDetails.ByteSizeOfStrings();
            length += DataOffset + sizeof(uint);

            var extensionOffset = length;
            length += _extension.Length;

            var totalLength = (ushort)(length + PacketReader.PacketHeaderSize);

            var fullSpan = output.GetSpan(totalLength);
            for (var i = 0; i < fullSpan.Length; i++)
            {
                fullSpan[i] = 0;
            }
            fullSpan = PacketHeader.WriteSingleMessage(fullSpan, PacketType.Tds7Login, totalLength);
            var tokenSpan = fullSpan.Slice(0, DataOffset);

            var dataSpan = fullSpan.Slice(DataOffset);
            var currentOffset = (ushort)DataOffset;

            //Length
            tokenSpan = tokenSpan.WriteLittleEndian((uint)(length));
            //Version
            tokenSpan = tokenSpan.WriteLittleEndian(Version);
            // Packet Size
            tokenSpan = tokenSpan.WriteLittleEndian(0x0000_1000);
            // Client Program Version
            tokenSpan = tokenSpan.WriteLittleEndian(0x0600_0000);
            // Client PID
            tokenSpan = tokenSpan.WriteLittleEndian(0x0000_03FC);
            // Connection Id
            tokenSpan = tokenSpan.WriteLittleEndian(0x0000_0000);
            tokenSpan = tokenSpan.WriteLittleEndian(OptionFlags);
            // Timezone 
            tokenSpan = tokenSpan.WriteLittleEndian(0x0000_0000);
            // Client LCID
            tokenSpan = tokenSpan.WriteLittleEndian(0x0000_0000);

            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.HostName, ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.Username, ref currentOffset);
            tokenSpan = tokenSpan.WritePassword(ref dataSpan, connectionDetails.Password, ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.AppName, ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.Server, ref currentOffset);
            // Feature Extension
            tokenSpan = tokenSpan.WriteInt(ref dataSpan, extensionOffset, ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.LibraryName, ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.Language, ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.Database, ref currentOffset);

            // Client Id
            lock (_clientIdRandom)
            {
                _clientIdRandom.NextBytes(tokenSpan.Slice(0, 6));
                tokenSpan = tokenSpan.Slice(6);
            }

            //SSPI
            tokenSpan = tokenSpan.WriteString(ref dataSpan, "", ref currentOffset);
            tokenSpan = tokenSpan.WriteString(ref dataSpan, connectionDetails.AttachDBFilename, ref currentOffset);

            // Change Password
            tokenSpan = tokenSpan.WriteString(ref dataSpan, "", ref currentOffset);
            // SSPI Long
            tokenSpan = tokenSpan.WriteLittleEndian((uint)0);

            _extension.CopyTo(dataSpan);

            output.Advance(totalLength);
        }

        private static Span<byte> WritePassword(this Span<byte> self, ref Span<byte> dataSpan, string password, ref ushort offset)
        {
            var token = new ServerDataToken() { Offset = offset, Length = (ushort)(password.Length) };
            dataSpan = dataSpan.WritePassword(password);
            self = self.WriteToken(token);
            offset += (ushort)(token.Length * 2);
            return self;
        }

        private static Span<byte> WritePassword(this Span<byte> self, string password)
        {
            Utils.ObfuscatePassword(self, password, out var output);
            return self.Slice(output.Length);
        }

        private static Span<byte> WriteInt(this Span<byte> self, ref Span<byte> dataSpan, int value, ref ushort offset)
        {
            var token = new ServerDataToken() { Offset = offset, Length = (ushort)sizeof(int) };
            dataSpan = dataSpan.WriteLittleEndian(value);

            self = self.WriteToken(token);
            offset += token.Length;
            return self;
        }

        private static Span<byte> WriteString(this Span<byte> self, ref Span<byte> dataSpan, string value, ref ushort offset)
        {
            value = value ?? string.Empty;
            var token = new ServerDataToken() { Offset = offset, Length = (ushort)value.Length };

            var stringSpan = MemoryMarshal.Cast<char,byte>(value.AsSpan());
            stringSpan.CopyTo(dataSpan);
            dataSpan = dataSpan.Slice(stringSpan.Length);

            self = self.WriteToken(token);
            offset += (ushort)(stringSpan.Length);
            return self;
        }

        private static Span<byte> WriteToken(this Span<byte> self, ServerDataToken token)
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), token);
            return self.Slice(Unsafe.SizeOf<ServerDataToken>());
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ServerDataToken
        {
            private ushort _offset;
            private ushort _length;

            public ushort Offset { get => _offset; set => _offset = value; }
            public ushort Length { get => _length; set => _length = value; }
        }
    }
}

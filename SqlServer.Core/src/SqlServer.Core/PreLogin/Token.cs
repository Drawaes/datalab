using System.Runtime.InteropServices;

namespace SqlServer.Core.PreLogin
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Token
    {
        public byte TokenType;
        private ushort _position;
        private ushort _length;

        public ushort Length { get => (ushort)((_length << 8) | (_length >> 8)); set => _length = (ushort)((value << 8) | (value >> 8)); }
        public ushort Position { get => (ushort)((_position << 8) | (_position >> 8)); set => _position = (ushort)((value << 8) | (value >> 8)); }
    }
}

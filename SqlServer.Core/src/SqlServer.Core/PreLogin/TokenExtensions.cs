using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SqlServer.Core.Internal;

namespace SqlServer.Core.PreLogin
{
    internal static class TokenExtensions
    {
        private static readonly int _sizeOfSpan = Unsafe.SizeOf<Token>();
        private static readonly int _tokenSize = Unsafe.SizeOf<Token>();

        public static unsafe ushort WriteToken(this Token self, ref Span<byte> span)
        {
            fixed (byte* bytes = &MemoryMarshal.GetReference(span))
            {
                Unsafe.Write(bytes, self);
            }
            span = span.Slice(_sizeOfSpan);
            return (ushort)(self.Position + self.Length);
        }

        public static unsafe Span<byte> WriteBigEndian<T>(this Span<byte> self, T value)
            where T : struct
        {
            value = ReaderExtensions.Reverse(value);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), value);
            return self.Slice(Unsafe.SizeOf<T>());
        }
       
        public static unsafe void WriteToken<TDataType>(byte tokenType, ref Span<byte> tokenSpan, ref Span<byte> dataSpan, TDataType data, ref ushort currentOffset)
            where TDataType : struct
        {
            var newDataSpan = dataSpan.WriteBigEndian(data);
            var size = dataSpan.Length - newDataSpan.Length;
            dataSpan = newDataSpan;

            var token = new Token() { Length = (ushort)size, Position = currentOffset, TokenType = tokenType };
            currentOffset = token.WriteToken(ref tokenSpan);
        }

        public static unsafe void WriteToken(byte tokenType, ref Span<byte> tokenSpan, ref Span<byte> dataSpan, byte[] data, ref ushort currentOffset)
        {
            var size = data.Length;
            data.CopyTo(dataSpan);
            dataSpan = dataSpan.Slice(size);

            var token = new Token() { Length = (ushort)size, Position = currentOffset, TokenType = tokenType };
            currentOffset = token.WriteToken(ref tokenSpan);
        }

        //public unsafe static int WriteBigEndian<TOutput, TValue>(this TOutput output, TValue value)
        //    where TOutput : IOutput where TValue : struct
        //{
        //    var size = Unsafe.SizeOf<TValue>();
        //    output.Enlarge(size);
        //    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(output.GetSpan()), value);
        //    output.Advance(size);
        //    return size;
        //}

        public static unsafe Token ReadToken(ref ReadOnlySpan<byte> span)
        {
            if (span[0] == 0xFF)
            {
                span = span.Slice(1);
                return new Token() { TokenType = 0xFF };
            }

            var token = Unsafe.ReadUnaligned<Token>(ref MemoryMarshal.GetReference(span));
            span = span.Slice(_sizeOfSpan);
            return token;
        }
    }
}

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    internal static class ReaderExtensions
    {
        public static T Read<T>(this ReadOnlySequence<byte> buffer) where T : struct
        {
            buffer = buffer.Slice(0, Unsafe.SizeOf<T>());

            if (buffer.IsSingleSegment)
            {
                return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(buffer.First.Span));
            }

            Span<byte> span = stackalloc byte[(int)buffer.Length];
            var newSpan = span;
            foreach (var segment in buffer)
            {
                segment.Span.CopyTo(newSpan);
                newSpan = newSpan.Slice(segment.Length);
            }

            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
        }

        public static ReadOnlySpan<byte> ToSpan(this ReadOnlySequence<byte> buffer, int length)
        {
            if (buffer.IsSingleSegment) return buffer.FirstSpan.Slice(0,length);
            return buffer.ToArray().AsSpan().Slice(0,length);
        }

        public static Span<byte> WriteString(this Span<byte> self, string value)
        {
            var s = MemoryMarshal.Cast<char, byte>(value.AsSpan());
            s.CopyTo(self);
            return self.Slice(s.Length);
        }

        public static unsafe ReadOnlySpan<byte> ReadLittleEndian<T>(this ReadOnlySpan<byte> self, out T value)
        {
            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(self));
            return self.Slice(Unsafe.SizeOf<T>());
        }

        public static ReadOnlySpan<byte> ReadBVarChar(this ReadOnlySpan<byte> self, out string value)
        {
            var length = self[0] * 2;
            value = Encoding.Unicode.GetString(self.Slice(1, length));
            return self.Slice(1 + length);
        }

        public static unsafe Span<byte> WriteLittleEndian<T>(this Span<byte> self, T value)
            where T : struct
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(self), value);
            return self[Unsafe.SizeOf<T>()..];
        }

        public static string ReadBVarChar(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;
            string returnValue;
            var length = firstSpan[0] * 2;
            bufferReader.Advance(1);
            firstSpan = bufferReader.UnreadSpan;
            returnValue = firstSpan.Length >= length ? Encoding.Unicode.GetString(firstSpan.Slice(0, length)) : throw new NotImplementedException();
            bufferReader.Advance(length);
            return returnValue;
        }

        public static double GetDouble(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;

            if (firstSpan.Length >= 8)
            {
                var returnValue = BinaryPrimitives.ReadDoubleLittleEndian(firstSpan);
                bufferReader.Advance(8);
                return returnValue;
            }

            var temp = (Span<byte>)stackalloc byte[8];
            bufferReader.TryCopyTo(temp);
            return BinaryPrimitives.ReadDoubleLittleEndian(temp);
        }

        public static int ReadIntLittleEndian(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;
            if (firstSpan.Length >= 4)
            {
                var returnValue = BinaryPrimitives.ReadInt32LittleEndian(firstSpan);
                bufferReader.Advance(4);
                return returnValue;
            }
            else
            {
                var temp = (Span<byte>)stackalloc byte[4];
                bufferReader.TryCopyTo(temp);
                return BinaryPrimitives.ReadInt32LittleEndian(temp);
            }
        }

        public static uint ReadUIntLittleEndian(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;
            if (firstSpan.Length >= 4)
            {
                var returnValue = BinaryPrimitives.ReadUInt32LittleEndian(firstSpan);
                bufferReader.Advance(4);
                return returnValue;
            }

            var temp = (Span<byte>)stackalloc byte[4];
            bufferReader.TryCopyTo(temp);
            return BinaryPrimitives.ReadUInt32LittleEndian(temp);
        }

        public static short ReadShortLittleEndian(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;
            short returnValue;
            if (firstSpan.Length >= 2)
            {
                returnValue = BinaryPrimitives.ReadInt16LittleEndian(firstSpan);
                bufferReader.Advance(2);
            }
            else
            {
                returnValue = firstSpan[0];
                bufferReader.Advance(1);
                returnValue |= (short)(bufferReader.CurrentSpan[0] << 8);
                bufferReader.Advance(1);
            }
            return returnValue;
        }

        public static long ReadLongLittleEndian(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;

            if (firstSpan.Length >= 8)
            {
                var returnValue = BinaryPrimitives.ReadInt64LittleEndian(firstSpan);
                bufferReader.Advance(8);
                return returnValue;
            }

            var temp = (Span<byte>)stackalloc byte[8];
            bufferReader.TryCopyTo(temp);
            return BinaryPrimitives.ReadInt64LittleEndian(temp);
        }

        public static ushort ReadUShortLittleEndian(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;
            ushort returnValue;
            if (firstSpan.Length >= 2)
            {
                returnValue = BinaryPrimitives.ReadUInt16LittleEndian(firstSpan);
                bufferReader.Advance(2);
                return returnValue;
            }

            returnValue = firstSpan[0];
            bufferReader.Advance(1);
            returnValue |= (ushort)(bufferReader.CurrentSpan[0] << 8);
            bufferReader.Advance(1);
            return returnValue;
        }

        public static byte ReadByte(ref this SequenceReader<byte> bufferReader)
        {
            var returnValue = bufferReader.UnreadSpan[0];
            bufferReader.Advance(1);
            return returnValue;
        }

        public static Guid GetGuid(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;
            if (firstSpan.Length >= 16)
            {
                var returnValue = new Guid(firstSpan);
                bufferReader.Advance(16);
                return returnValue;
            }

            var temp = (Span<byte>)stackalloc byte[16];
            bufferReader.TryCopyTo(temp);
            return new Guid(temp);
        }

        public static float GetFloat(ref this SequenceReader<byte> bufferReader)
        {
            var firstSpan = bufferReader.UnreadSpan;

            if (firstSpan.Length >= 4)
            {
                var returnValue = BinaryPrimitives.ReadSingleLittleEndian(firstSpan);
                bufferReader.Advance(4);
                return returnValue;
            }

            var temp = (Span<byte>)stackalloc byte[4];
            bufferReader.TryCopyTo(temp);
            return BinaryPrimitives.ReadSingleLittleEndian(temp);
        }

        internal static unsafe T Reverse<T>(T value) where T : struct
        {
            // note: relying on JIT goodness here!
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                return value;
            }
            else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(short))
            {
                ushort val = 0;
                Unsafe.Write(&val, value);
                val = (ushort)((val >> 8) | (val << 8));
                return Unsafe.Read<T>(&val);
            }
            else if (typeof(T) == typeof(uint) || typeof(T) == typeof(int)
                || typeof(T) == typeof(float))
            {
                uint val = 0;
                Unsafe.Write(&val, value);
                val = (val << 24)
                    | ((val & 0xFF00) << 8)
                    | ((val & 0xFF0000) >> 8)
                    | (val >> 24);
                return Unsafe.Read<T>(&val);
            }
            else if (typeof(T) == typeof(ulong) || typeof(T) == typeof(long)
                || typeof(T) == typeof(double))
            {
                ulong val = 0;
                Unsafe.Write(&val, value);
                val = (val << 56)
                    | ((val & 0xFF00) << 40)
                    | ((val & 0xFF0000) << 24)
                    | ((val & 0xFF000000) << 8)
                    | ((val & 0xFF00000000) >> 8)
                    | ((val & 0xFF0000000000) >> 24)
                    | ((val & 0xFF000000000000) >> 40)
                    | (val >> 56);
                return Unsafe.Read<T>(&val);
            }
            else
            {
                // default implementation
                var len = Unsafe.SizeOf<T>();
                var val = stackalloc byte[len];
                Unsafe.Write(val, value);
                int to = len >> 1, dest = len - 1;
                for (var i = 0; i < to; i++)
                {
                    var tmp = val[i];
                    val[i] = val[dest];
                    val[dest--] = tmp;
                }
                return Unsafe.Read<T>(val);
            }
        }
    }
}

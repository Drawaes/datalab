using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SqlServer.Core.Internal;

namespace SqlServer.Core
{
    public ref struct RowReader
    {
        private SequenceReader<byte> _reader;
        private int _currentColumn;
        private ulong _nullMap;

        public RowReader(ReadOnlySequence<byte> buffer, ulong nullMap)
        {
            _reader = new SequenceReader<byte>(buffer);
            _nullMap = nullMap;
            _currentColumn = 0;
        }

        private bool IsColumnNull => (_nullMap & (uint)(1 << _currentColumn)) != 0;

        public short ReadSmallInt()
        {
            _currentColumn++;
            return _reader.ReadShortLittleEndian();
        }

        public int ReadInt()
        {
            _currentColumn++;
            return _reader.ReadIntLittleEndian();
        }

        public byte ReadTinyInt()
        {
            _currentColumn++;
            return _reader.ReadByte();
        }

        public long ReadBigInt()
        {
            _currentColumn++;
            return _reader.ReadLongLittleEndian();
        }

        public double ReadReal()
        {
            _currentColumn++;
            return _reader.GetDouble();
        }

        public float ReadFloat()
        {
            _currentColumn++;
            return _reader.GetFloat();
        }

        public DateTime ReadDateTime()
        {
            _currentColumn++;
            var days = _reader.ReadIntLittleEndian();
            var seconds = _reader.ReadUIntLittleEndian();
            return new DateTime(1900, 1, 1).AddDays(days).AddSeconds(seconds / 300.0);
        }

        public DateTime ReadSmallDateTime()
        {
            _currentColumn++;
            var days = _reader.ReadUShortLittleEndian();
            var minutes = _reader.ReadUShortLittleEndian();
            return new DateTime(1900, 1, 1).AddDays(days).AddMinutes(minutes);
        }

        public DateTime ReadDate()
        {
            _currentColumn++;
            var days = (_reader.ReadUShortLittleEndian() << 8) + _reader.ReadByte();
            return new DateTime(01, 01, 01).AddDays(days);
        }

        public DateTime? ReadDateNullable()
        {
            if (IsColumnNull)
            {
                _currentColumn++;
                return null;
            }
            var size = _reader.ReadByte();
            if (size == 0)
            {
                _currentColumn++;
                return null;
            }
            return ReadDate();
        }

        public Guid ReadGuid()
        {
            _currentColumn++;
            return _reader.GetGuid();
        }

        public ReadOnlySpan<char> ReadNVarChar()
        {
            _currentColumn++;
            var length = _reader.ReadShortLittleEndian() * 2;
            return MemoryMarshal.Cast<byte,char>(ReadVarCharInternal(length));
        }

        public bool ReadNVarCharNullable(out ReadOnlySpan<char> output)
        {
            if (IsColumnNull)
            {
                _currentColumn++;
                output = default;
                return false;
            }
            _currentColumn++;
            var length = _reader.ReadUShortLittleEndian();
            if (length == 0xFFFF)
            {
                output = default;
                return false;
            }
            output = MemoryMarshal.Cast<byte,char>(ReadVarCharInternal(length * 2));
            return true;
        }

        public bool ReadVarCharNullable(out ReadOnlySpan<byte> output)
        {
            if (IsColumnNull)
            {
                _currentColumn++;
                output = default;
                return false;
            }
            _currentColumn++;
            var length = _reader.ReadUShortLittleEndian();
            if (length == 0xFFFF)
            {
                output = default;
                return false;
            }
            output = ReadVarCharInternal(length);
            return true;
        }

        public ReadOnlySpan<byte> ReadVarChar()
        {
            _currentColumn++;
            var length = _reader.ReadUShortLittleEndian();
            return ReadVarCharInternal(length);
        }

        private ReadOnlySpan<byte> ReadVarCharInternal(int length)
        {
            _currentColumn++;
            if (_reader.UnreadSpan.Length < length)
            {
                var tempBuffer = new byte[length];
                _reader.TryCopyTo(tempBuffer);
                return tempBuffer.AsSpan();
            }
            else
            {
                var returnValue = _reader.UnreadSpan.Slice(0, length);
                _reader.Advance(length);
                return returnValue;
            }
        }
    }
}

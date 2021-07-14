using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using SqlServer.Core.Internal;

namespace SqlServer.Core
{
    public class DataReader
    {
        private const byte NullableFlag = 0x1;
        private const ushort NullLength = 65535;

        private Pipe _innerPipe;
        private ColumnInfo[] _columns;
        private SequencePosition _rowEnd;
        private ReadOnlySequence<byte> _currentBuffer;
        private ulong _nullMap;
        private bool _finished;

        internal DataReader(Pipe pipe) => _innerPipe = pipe;

        public ColumnInfo[] Columns => _columns;
        internal Pipe InnerPipe => _innerPipe;

        public RowReader GetRowReader() => new(_currentBuffer.Slice(0, _rowEnd), _nullMap);
        public Task ReadMetaDataAsync() => ReadRowAsync();

        public Task<bool> ReadRowAsync()
        {
            return _finished ? Task.FromResult(false) : InnerReadRow();

            async Task<bool> InnerReadRow()
            {
                while (true)
                {
                    SequencePosition consumed;
                    ReadOnlySequence<byte> buffer;
                    if (_currentBuffer.IsEmpty)
                    {
                        var result = await _innerPipe.Reader.ReadAsync();
                        buffer = result.Buffer;
                        consumed = buffer.Start;
                    }
                    else
                    {
                        _currentBuffer = _currentBuffer.Slice(_rowEnd);
                        buffer = _currentBuffer;
                        _currentBuffer = default;
                        consumed = buffer.Start;
                    }

                    if (ReadTokens(ref buffer, ref consumed))
                    {
                        return true;
                    }
                    else
                    {
                        if (_finished)
                        {
                            _innerPipe.Reader.AdvanceTo(buffer.End, buffer.End);
                            _innerPipe.Reader.Complete();
                            return false;
                        }
                        else
                        {
                            _innerPipe.Reader.AdvanceTo(consumed, buffer.End);
                        }
                    }
                }
            }
        }

        private bool IsNull(ulong map, int colId) => (map & (uint)(1 << colId)) != 0;

        private bool ReadTokens(ref ReadOnlySequence<byte> messageBuffer, ref SequencePosition position)
        {
            while (messageBuffer.Length > 3)
            {
                var tokenType = (DataStreamTokenTypes)messageBuffer.First.Span[0];
                switch (tokenType)
                {
                    case DataStreamTokenTypes.COLMETADATA_TOKEN:
                        ReadMetaData(messageBuffer, out position);
                        messageBuffer = messageBuffer.Slice(position);
                        _currentBuffer = messageBuffer;
                        _rowEnd = _currentBuffer.Start;
                        return true;
                    case DataStreamTokenTypes.ROW_TOKEN:
                        return ReadRow(messageBuffer, ref position, false);
                    case DataStreamTokenTypes.NBCROW_TOKEN:
                        return ReadRow(messageBuffer, ref position, true);
                    case DataStreamTokenTypes.DONE_TOKEN:
                        _rowEnd = _currentBuffer.End;
                        _finished = true;
                        return false;
                    default:
                        throw new InvalidOperationException();
                }
            }
            return false;
        }

        private static int GetLength(DataType type, ref SequenceReader<byte> reader)
        {
            switch (type)
            {
                case DataType.TinyInt:
                    return 1;
                case DataType.SmallInt:
                    return 2;
                case DataType.Int:
                case DataType.Real:
                    return 4;
                case DataType.BigInt:
                case DataType.Float:
                case DataType.DateTime:
                    return 8;
                case DataType.BigVarChar:
                    var length = reader.ReadUShortLittleEndian();
                    var collation = reader.ReadUIntLittleEndian();
                    var sort = reader.ReadByte();
                    return length;
                default:
                    throw new NotImplementedException();
            }
        }

        private void ReadMetaData(ReadOnlySequence<byte> readBuffer, out SequencePosition finalPosition)
        {
            readBuffer.FirstSpan.Slice(1).ReadLittleEndian<ushort>(out var numberOfCols);
            _columns = new ColumnInfo[numberOfCols];

            var reader = new SequenceReader<byte>(readBuffer.Slice(5));
            for (var i = 0; i < numberOfCols; i++)
            {
                var userType = reader.ReadUIntLittleEndian();
                var flags1 = reader.ReadByte();
                var flags2 = reader.ReadByte();
                var dataType = (DataType)reader.ReadByte();
                _columns[i].IsNullable = (flags1 & 0x1) == 0x1;
                _columns[i].DataType = dataType;
                _columns[i].Length = GetLength(dataType, ref reader);
                var name = reader.ReadBVarChar();
                _columns[i].Name = name;
            }
            finalPosition = reader.Position;
        }

        private (ulong nullMap, int nullByteCount) GetNullBitMap(ref SequenceReader<byte> reader, bool hasNullMap)
        {
            if (hasNullMap)
            {
                ulong returnValue = 0;
                var numBytes = ((_columns.Length - 1) >> 3) + 1;
                for (var i = 0; i < numBytes; i++)
                {
                    returnValue = (returnValue << 8) + reader.ReadByte();
                }
                return (returnValue, numBytes);
            }
            return (0, 0);
        }

        private bool ReadRow(ReadOnlySequence<byte> messageBuffer, ref SequencePosition position, bool hasNullMap)
        {
            var originalPosition = position;

            var reader = new SequenceReader<byte>(messageBuffer);
            var bufferSize = messageBuffer.Length;
            reader.Advance(1);
            var (nullMap, nullByteCount) = GetNullBitMap(ref reader, hasNullMap);
            for (var i = 0; i < _columns.Length; i++)
            {
                var column = _columns[i];
                if (IsNull(nullMap, i))
                {
                    continue;
                }
                switch (column.DataType)
                {
                    case DataType.Int:
                    case DataType.TinyInt:
                    case DataType.SmallInt:
                    case DataType.BigInt:
                    case DataType.DateTime:
                    case DataType.SmallDateTime:
                    case DataType.Guid:
                    case DataType.Real:
                    case DataType.Float:
                    case DataType.Money:
                    case DataType.SmallMoney:
                        if (bufferSize - reader.Consumed < column.Length)
                        {
                            _currentBuffer = default;
                            position = originalPosition;
                            return false;
                        }
                        reader.Advance(column.Length);
                        break;
                    case DataType.BigVarChar:
                        if (bufferSize - reader.Consumed < 2)
                        {
                            _currentBuffer = default;
                            position = originalPosition;
                            return false;
                        }
                        var length = reader.ReadUShortLittleEndian();

                        if (messageBuffer.Length - reader.Consumed < length)
                        {
                            _currentBuffer = default;
                            position = originalPosition;
                            return false;
                        }
                        reader.Advance(length);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            _rowEnd = reader.Position;
            _currentBuffer = messageBuffer.Slice(1 + nullByteCount);
            _nullMap = nullMap;
            return true;
        }
    }
}

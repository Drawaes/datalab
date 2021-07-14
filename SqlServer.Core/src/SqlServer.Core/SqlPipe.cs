using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SqlServer.Core.Internal;
using SqlServer.Core.Tls;

namespace SqlServer.Core
{
    public class SqlPipe
    {
        private readonly IDuplexPipe _pipeConnection;
        private readonly SqlConnectionDetails _connectionDetails;
        private TlsWrapper _wrapper;
        private SqlConnectionState _state = SqlConnectionState.None;
        private SemaphoreSlim _stateAwaiter = new(0);
        private DataReader _currentReader;
        private ServerLogin _loginDetails;

        public SqlPipe(IDuplexPipe pipeConnection, SqlConnectionDetails connectionDetails)
        {
            _connectionDetails = connectionDetails;
            _pipeConnection = pipeConnection;
        }

        public async Task ConnectAsync()
        {
            _wrapper = new TlsWrapper(_connectionDetails.PipeOptions);

            
            var writer = _pipeConnection.Output;
            PreLogin.ClientPreLogin.WritePreLogin(ref writer);
            _state = SqlConnectionState.PreLoginSent;

            _ = ReadingLoop();

            await writer.FlushAsync();
            await _stateAwaiter.WaitAsync();

            var authAsClient = TlsPipeline.AuthenticateClient(_wrapper, new TlsClientOptions());
            _state = SqlConnectionState.TlsHandshake;

            _ = _wrapper.ReaderLoop(_pipeConnection);

            var tlsConnection = await authAsClient;
            _wrapper.HandshakeCompleted = true;
            //Now we have authenticated we need to send the login message over the TLS connection
            _state = SqlConnectionState.LoginSent;
            
            ClientLogin.WriteLogin(ref writer, _connectionDetails);
            await writer.FlushAsync();

            await _stateAwaiter.WaitAsync();
        }

        private void WriteSequenceToWriter(ReadOnlySequence<byte> output, PipeWriter writer)
        {
            foreach(var seg in output)
            {
                var span = writer.GetSpan(seg.Length);
                seg.Span.CopyTo(span);
                writer.Advance(seg.Length);
            }
        }

        private async Task ReadingLoop()
        {
            while (true)
            {
                var readResult = await _pipeConnection.Input.ReadAsync();
                var buffer = readResult.Buffer;
                try
                {
                    if (buffer.IsEmpty)
                    {
                        if (readResult.IsCompleted)
                        {
                            return;
                        }
                        continue;
                    }

                    while (PacketReader.ReadPacket(ref buffer, out var packetBuffer, out var header) == PacketResult.Complete)
                    {
                        switch (_state)
                        {
                            case SqlConnectionState.TlsHandshake:
                                WriteSequenceToWriter(packetBuffer, _wrapper.InternalOutput);
                                await _wrapper.InternalOutput.FlushAsync();
                                break;
                            case SqlConnectionState.PreLoginSent when header.Type == PacketType.TabularResult:
                                var serverPreLogin = new PreLogin.ServerPreLogin(packetBuffer.First.Span);
                                _state = SqlConnectionState.PreLoginReceived;
                                _stateAwaiter.Release();
                                break;
                            case SqlConnectionState.LoginSent when header.Type == PacketType.TabularResult:
                                _loginDetails = new ServerLogin(packetBuffer.First.Span);
                                _state = SqlConnectionState.LoginAckCompleted;
                                _stateAwaiter.Release();
                                break;
                            case SqlConnectionState.QueryRunning when header.Type == PacketType.TabularResult:
                                foreach (var segment in packetBuffer)
                                {
                                    await _currentReader.InnerPipe.Writer.WriteAsync(segment);
                                }
                                if (((byte)header.Status & (byte)StatusType.EndOfMessage) > 0)
                                {
                                    _currentReader.InnerPipe.Writer.Complete();
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                finally
                {
                    _pipeConnection.Input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }

        public async Task<DataReader> ExecuteQueryAsync(string sqlQuery)
        {
            _currentReader = new DataReader(new Pipe(_connectionDetails.PipeOptions));
            var length = PacketReader.PacketHeaderSize + sqlQuery.Length * 2 + sizeof(uint) + Unsafe.SizeOf<TransactionDescriptor>();
            _state = SqlConnectionState.QueryRunning;

            var memory = _pipeConnection.Output.GetMemory(length);

            WriteMessage(sqlQuery, memory.Span, length);
            _pipeConnection.Output.Advance(length);
            await _pipeConnection.Output.FlushAsync();

            return _currentReader;

            void WriteMessage(string query, Span<byte> buffer, int l)
            {
                buffer = PacketHeader.WriteSingleMessage(buffer, PacketType.SqlBatch, (ushort)l);
                buffer = buffer.WriteLittleEndian((uint)(sizeof(uint) + Unsafe.SizeOf<TransactionDescriptor>()));
                buffer = buffer.WriteLittleEndian(new TransactionDescriptor(0));
                buffer.WriteString(query);
            }
        }
    }
}

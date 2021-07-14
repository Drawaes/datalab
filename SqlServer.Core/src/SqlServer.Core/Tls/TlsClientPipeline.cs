using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SqlServer.Core.Internal;
using SqlServer.Core.Tls.Interop;
using static SqlServer.Core.Tls.Interop.LibCrypto;
using static SqlServer.Core.Tls.Interop.OpenSsl;

namespace SqlServer.Core.Tls
{
    internal class TlsClientPipeline : IDuplexPipe, IDisposable
    {
        private SSL_CTX _context;
        private SSL _ssl;
        private readonly TlsClientOptions _clientOptions;
        private readonly TlsServerOptions _serverOptions;
        private Pipe _readInnerPipe;
        private Pipe _writeInnerPipe;
        private IDuplexPipe _innerConnection;
        private BIO _readBio;
        private BIO _writeBio;
        private TaskCompletionSource<bool> _handshakeTask;

        private static readonly CustomReadBioDescription s_ReadBio = new CustomReadBioDescription();
        private static readonly CustomWriteBioDescription s_WriteBio = new CustomWriteBioDescription();

        internal TlsClientPipeline(IDuplexPipe inputPipe, SSL_CTX context, TlsClientOptions clientOptions, PipeOptions pipeOptions)
            : this(inputPipe, context, pipeOptions)
        {
            _clientOptions = clientOptions;
            SSL_set_connect_state(_ssl);
        }

        internal TlsClientPipeline(IDuplexPipe inputPipe, SSL_CTX context, TlsServerOptions serverOptions, PipeOptions pipeOptions)
            : this(inputPipe, context, pipeOptions)
        {
            _serverOptions = serverOptions;
            SSL_set_accept_state(_ssl);
        }

        private TlsClientPipeline(IDuplexPipe inputPipe, SSL_CTX context, PipeOptions pipeOptions)
        {
            _innerConnection = inputPipe;
            _readInnerPipe = new Pipe(pipeOptions);
            _writeInnerPipe = new Pipe(pipeOptions);
            _context = context;
            _ssl = SSL_new(_context);
            _readBio = s_ReadBio.New();
            _writeBio = s_WriteBio.New();
            SSL_set0_rbio(_ssl, _readBio);
            SSL_set0_wbio(_ssl, _writeBio);
        }

        internal IDuplexPipe InnerConnection => _innerConnection;

        private async Task HandshakeLoop()
        {
            if (_clientOptions != null)
            {
                if (!string.IsNullOrEmpty(_clientOptions.CertificateFile))
                {
                    LoadCertificate(await System.IO.File.ReadAllBytesAsync(_clientOptions.CertificateFile), _clientOptions.CertificatePassword);
                }
                await ProcessHandshakeMessage(default, _innerConnection.Output);
            }
            else
            {
                if (string.IsNullOrEmpty(_serverOptions.CertificateFile))
                {
                    throw new InvalidOperationException();
                }
                LoadCertificate(await System.IO.File.ReadAllBytesAsync(_serverOptions.CertificateFile), _serverOptions.CertificatePassword);
            }

            try
            {
                while (true)
                {
                    var readResult = await _innerConnection.Input.ReadAsync();
                    var buffer = readResult.Buffer;
                    try
                    {
                        if (buffer.IsEmpty && readResult.IsCompleted)
                        {
                            _handshakeTask.SetException(new InvalidOperationException("Failed to complete handshake"));
                            return;
                        }

                        while (TryGetFrame(ref buffer, out var messageBuffer, out var frameType))
                        {
                            if (frameType != TlsFrameType.Handshake && frameType != TlsFrameType.ChangeCipherSpec)
                            {
                                _handshakeTask.SetException(new InvalidOperationException($"Received an invalid frame for the current handshake state {frameType}"));
                                return;
                            }

                            if ((await ProcessHandshakeMessage(messageBuffer, _innerConnection.Output)))
                            {
                                return;
                            }
                        }
                    }
                    finally
                    {
                        _innerConnection.Input.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            finally
            {
                _handshakeTask.TrySetResult(true);
                _ = StartReading();
                _ = StartWriting();
            }
        }

        private void LoadCertificate(byte[] certificateData, string password)
        {
            var pkcs12 = d2i_PKCS12(certificateData);
            var (key, cert) = PKCS12_parse(pkcs12, password);
            try
            {
                SSL_use_certificate(_ssl, cert);
                SSL_use_PrivateKey(_ssl, key);
            }
            finally
            {
                key.Free();
                cert.Free();
            }
        }

        private async Task StartWriting()
        {
            await _handshakeTask.Task.ConfigureAwait(false);

            var maxBlockSize = (int)Math.Pow(2, 14);
            try
            {
                while (true)
                {
                    var result = await _writeInnerPipe.Reader.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {

                        if (buffer.IsEmpty && result.IsCompleted)
                        {
                            break;
                        }
                        while (buffer.Length > 0)
                        {
                            ReadOnlySequence<byte> messageBuffer;
                            if (buffer.Length <= maxBlockSize)
                            {
                                messageBuffer = buffer;
                                buffer = buffer.Slice(buffer.End);
                            }
                            else
                            {
                                messageBuffer = buffer.Slice(0, maxBlockSize);
                                buffer = buffer.Slice(maxBlockSize);
                            }

                            await EncryptAsync(messageBuffer, _innerConnection.Output);
                        }
                    }
                    finally
                    {
                        _writeInnerPipe.Reader.AdvanceTo(buffer.End);
                    }
                }
            }
            finally
            {
                _writeInnerPipe.Reader.Complete();
            }
        }

        private async Task StartReading()
        {
            try
            {
                while (true)
                {
                    var result = await _innerConnection.Input.ReadAsync();
                    var buffer = result.Buffer;
                    try
                    {
                        if (buffer.IsEmpty && result.IsCompleted)
                        {
                            break;
                        }

                        while (TryGetFrame(ref buffer, out var messageBuffer, out var frameType))
                        {
                            if (frameType != TlsFrameType.AppData)
                            {
                                // Throw we don't support renegotiation at this point
                                throw new InvalidOperationException($"Invalid frame type {frameType} expected app data");
                            }

                            await DecryptAsync(messageBuffer, _readInnerPipe);
                        }
                    }
                    finally
                    {
                        _innerConnection.Input.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            finally
            {
                _readInnerPipe.Writer.Complete();
            }
        }

        private ValueTask<FlushResult> EncryptAsync(ReadOnlySequence<byte> unencrypted, PipeWriter writer)
        {
            var handle = GCHandle.Alloc(writer);
            try
            {
                BIO_set_data(_writeBio, handle);
                while (unencrypted.Length > 0)
                {
                    var totalWritten = SSL_write(_ssl, unencrypted.First.Span);
                    unencrypted = unencrypted.Slice(totalWritten);
                }

                return writer.FlushAsync();
            }
            finally
            {
                handle.Free();
            }
        }

        private ValueTask<FlushResult> DecryptAsync(ReadOnlySequence<byte> messageBuffer, Pipe readInnerPipe)
        {
            BIO_set_data(_readBio, ref messageBuffer);
            var result = 1;
            while (result > 0)
            {
                var decryptedData = readInnerPipe.Writer.GetSpan(1024);

                result = SSL_read(_ssl, decryptedData);
                if (result > 0)
                {
                    readInnerPipe.Writer.Advance(result);
                }
            }

            return readInnerPipe.Writer.FlushAsync();
        }

        private async Task<bool> ProcessHandshakeMessage(ReadOnlySequence<byte> readBuffer, PipeWriter writer)
        {
            var writeHandle = GCHandle.Alloc(writer);
            try
            {
                BIO_set_data(_readBio, ref readBuffer);
                BIO_set_data(_writeBio, writeHandle);

                var result = SSL_do_handshake(_ssl);
                if (result == 1)
                {
                    // handshake is complete
                    await writer.FlushAsync();
                    return true;
                }

                // Not completed, so we need to check if its an error or if we should continue
                var sslResultCode = SSL_get_error(_ssl, result);
                if (sslResultCode == SslErrorCodes.SSL_ASYNC_PAUSED)
                {
                    await writer.FlushAsync();
                    return false;
                }
                else
                {
                    return false;
                }

                // We had some other error need to fill in and figure out how to deal with it.
                throw new NotImplementedException();
            }
            finally
            {
                writeHandle.Free();
            }
        }

        private static bool TryGetFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> messageBuffer, out TlsFrameType frameType)
        {
            frameType = TlsFrameType.Incomplete;

            // The header is 5 bytes long so if it's less than that just exit
            if (buffer.Length < 5)
            {
                messageBuffer = default;
                return false;
            }

            var span = buffer.ToSpan(5);

            frameType = (TlsFrameType)span[0];

            // Check it's a valid frametype for what we are expecting

            if ((byte)frameType < 20 | (byte)frameType > 24)
            {
                // Unknown frametype, error
                throw new FormatException($"The Tls frame type was invalid, type was {frameType}");
            }

            // Get the Tls Version
            var version = span[1] << 8 | span[2];

            if (version < 0x300 || version >= 0x500)
            {
                //Unknown or unsupported message version
                messageBuffer = default;
                throw new FormatException($"The Tls frame version was invalid, the version was {version}");
            }

            var length = span[3] << 8 | span[4];
            if (buffer.Length >= (length + 5))
            {
                messageBuffer = buffer.Slice(0, length + 5);
                buffer = buffer.Slice(messageBuffer.End);
                return true;
            }

            messageBuffer = default;
            return false;
        }

        public Task AuthenticateAsync() => _handshakeTask?.Task ?? StartHandshake();

        private Task StartHandshake()
        {
            _ = HandshakeLoop();
            _handshakeTask = new TaskCompletionSource<bool>();
            return _handshakeTask.Task;
        }

        public PipeReader Input => _readInnerPipe.Reader;
        public PipeWriter Output => _writeInnerPipe.Writer;

        public void Dispose()
        {
            _context.Close();
            _ssl.Close();
        }
    }
}

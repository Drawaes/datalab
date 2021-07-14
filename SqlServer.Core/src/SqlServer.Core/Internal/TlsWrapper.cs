using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    internal class TlsWrapper : IDuplexPipe
    {
        private readonly Pipe _readerPipe;
        private readonly Pipe _writerPipe;

        public PipeReader Input => _readerPipe.Reader;
        public PipeWriter Output => _writerPipe.Writer;
        public bool HandshakeCompleted { get; set; }
        public PipeReader InternalInput => _writerPipe.Reader;
        public PipeWriter InternalOutput => _readerPipe.Writer;

        public TlsWrapper(PipeOptions pipeOptions)
        {
            _readerPipe = new Pipe(pipeOptions);
            _writerPipe = new Pipe(pipeOptions);
        }

        public async Task ReaderLoop(IDuplexPipe connection)
        {
            while(true)
            {
                var readResult = await InternalInput.ReadAsync();
                var buffer = readResult.Buffer;
                try
                {
                    if (buffer.IsEmpty)
                    {
                        continue;
                    }
                    var writer = connection.Output;
                    if (!HandshakeCompleted)
                    {
                        var memory = connection.Output.GetMemory(PacketReader.PacketHeaderSize);
                        PacketHeader.WriteSingleMessage(memory.Span, PacketType.PreLogin, (ushort)(PacketReader.PacketHeaderSize + readResult.Buffer.Length));
                        writer.Advance(PacketReader.PacketHeaderSize);
                    }

                    foreach (var seg in buffer)
                    {
                        var mem = writer.GetMemory(seg.Length);
                        seg.CopyTo(mem);
                        writer.Advance(seg.Length);
                    }

                    await writer.FlushAsync();
                }
                finally
                {
                    InternalInput.AdvanceTo(readResult.Buffer.End);
                }
            }
        }

        public void Dispose()
        {
            _readerPipe.Writer.Complete();
            _readerPipe.Reader.Complete();
            _writerPipe.Reader.Complete();
            _writerPipe.Writer.Complete();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls
{
    internal static class TlsPipeline
    {
        public static async Task<TlsClientPipeline> AuthenticateClient(IDuplexPipe inputPipe, TlsClientOptions clientOptions)
        {
            var ctx = Interop.OpenSsl.SSL_CTX_new(Interop.OpenSsl.TLS_client_method());
            var pipeline = new TlsClientPipeline(inputPipe, ctx, clientOptions, new PipeOptions(System.Buffers.MemoryPool<byte>.Shared));
            await pipeline.AuthenticateAsync();
            return pipeline;
        }

        public static async Task<TlsClientPipeline> AuthenticateAsServer(IDuplexPipe inputPipe, TlsServerOptions options)
        {
            var ctx = Interop.OpenSsl.SSL_CTX_new(Interop.OpenSsl.TLS_server_method());
            var pipeline = new TlsClientPipeline(inputPipe, ctx, options, new PipeOptions(System.Buffers.MemoryPool<byte>.Shared));
            await pipeline.AuthenticateAsync();
            return pipeline;
        }
    }
}

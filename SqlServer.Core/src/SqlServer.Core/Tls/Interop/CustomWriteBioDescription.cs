using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SqlServer.Core.Tls.Interop.LibCrypto;

namespace SqlServer.Core.Tls.Interop
{
    internal class CustomWriteBioDescription : CustomBioDescription
    {
        public CustomWriteBioDescription()
            : base(nameof(CustomWriteBioDescription))
        {
        }

        protected override int Create(BIO bio) => 1;

        protected override int Destroy(BIO bio) => 1;

        protected override int Read(BIO bio, Span<byte> output)
        {
            throw new NotImplementedException();
        }

        private const int MaxSize = 4096 - 64;

        protected override int Write(BIO bio, ReadOnlySpan<byte> input)
        {
            var data = BIO_get_data(bio);

            if (!(data.Target is PipeWriter clientPipe))
            {
                return -1;
            }

            var inputLength = input.Length;

            while (input.Length > 0)
            {
                var size = Math.Min(MaxSize, input.Length);
                var writer = clientPipe.GetSpan(size);
                input.Slice(0, size).CopyTo(writer);
                input = input.Slice(size);
                clientPipe.Advance(size);
            }
            return inputLength;
        }
    }
}

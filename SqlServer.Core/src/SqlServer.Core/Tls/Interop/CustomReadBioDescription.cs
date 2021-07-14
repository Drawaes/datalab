using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SqlServer.Core.Tls.Interop.LibCrypto;

namespace SqlServer.Core.Tls.Interop
{
    internal sealed class CustomReadBioDescription : CustomBioDescription
    {
        public CustomReadBioDescription()
            : base(nameof(CustomReadBioDescription))
        {
        }

        protected override int Create(BIO bio) => 1;

        protected override int Destroy(BIO bio) => 1;

        protected override int Read(BIO bio, Span<byte> output)
        {
            ref var data = ref BIO_get_data<ReadOnlySequence<byte>>(bio);

            if (data.Length <= 0)
            {
                return -1;
            }

            var amountToWrite = Math.Min(data.Length, output.Length);
            data.Slice(0, amountToWrite).CopyTo(output);
            data = data.Slice(amountToWrite);

            return (int)amountToWrite;
        }

        protected override int Write(BIO bio, ReadOnlySpan<byte> input)
        {
            throw new NotImplementedException();
        }
    }
}

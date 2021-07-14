using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Internal
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TransactionDescriptor
    {
        public uint HeaderLength;
        public AllHeadersType HeaderType;
        public ulong Descriptor;
        public uint OutstandingRequestCount;

        public TransactionDescriptor(ulong descriptor)
        {
            HeaderLength = 18;
            HeaderType = AllHeadersType.TransactionDescriptor;
            Descriptor = descriptor;
            OutstandingRequestCount = 1;
        }
    }
}

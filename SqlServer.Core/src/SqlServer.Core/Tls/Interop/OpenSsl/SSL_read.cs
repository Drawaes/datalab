using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class OpenSsl
    {
        [DllImport(Libraries.LibSsl, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int SSL_read(SSL ssl, void* buf, int num);

        public unsafe static int SSL_read(SSL ssl, Span<byte> input)
        {
            fixed (void* ptr = &MemoryMarshal.GetReference(input))
            {
                var result = SSL_read(ssl, ptr, input.Length);
                return result;
            }
        }
    }
}

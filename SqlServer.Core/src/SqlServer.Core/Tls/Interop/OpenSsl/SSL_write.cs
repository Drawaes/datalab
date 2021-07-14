using System;
using System.Runtime.InteropServices;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class OpenSsl
    {
        [DllImport(Libraries.LibSsl, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int SSL_write(SSL ssl, void* buf, int num);

        public unsafe static int SSL_write(SSL ssl, ReadOnlySpan<byte> output)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(output))
            {
                var result = SSL_write(ssl, ptr, output.Length);
                return result;
            }
        }
    }
}

using System.Runtime.InteropServices;
using static SqlServer.Core.Tls.Interop.LibCrypto;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class OpenSsl
    {
        [DllImport(Libraries.LibSsl, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SSL_set0_wbio(SSL ssl, BIO wbio);
    }
}

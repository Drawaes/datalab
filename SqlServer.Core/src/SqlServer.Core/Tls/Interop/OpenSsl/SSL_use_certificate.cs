using System.Runtime.InteropServices;
using static SqlServer.Core.Tls.Interop.LibCrypto;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class OpenSsl
    {
        [DllImport(Libraries.LibSsl, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(SSL_use_certificate))]
        private unsafe extern static int Internal_SSL_use_certificate(SSL ctx, X509 cert);

        public static void SSL_use_certificate(SSL ssl, X509 cert)
        {
            var result = Internal_SSL_use_certificate(ssl, cert);
            ThrowOnErrorReturnCode(result);
        }
    }
}

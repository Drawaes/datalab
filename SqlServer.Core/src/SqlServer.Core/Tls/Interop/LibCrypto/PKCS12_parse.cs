using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int PKCS12_parse(PKCS12 p12, string pass, out EVP_PKEY pkey, out X509 cert, IntPtr ca);

        public static (EVP_PKEY privateKey, X509 certificate) PKCS12_parse(PKCS12 p12, string password)
        {
            ThrowOnErrorReturnCode(PKCS12_parse(p12, password, out var privateKey, out var cert, IntPtr.Zero));
            return (privateKey, cert);
        }
    }
}

using System.Runtime.InteropServices;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class OpenSsl
    {
        [DllImport(Libraries.LibSsl, CallingConvention = CallingConvention.Cdecl)]
        public static extern SSL SSL_new(SSL_CTX ctx);
    }
}

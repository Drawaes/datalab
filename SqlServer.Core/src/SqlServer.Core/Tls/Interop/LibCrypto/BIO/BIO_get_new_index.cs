using System.Runtime.InteropServices;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int BIO_get_new_index();
    }
}

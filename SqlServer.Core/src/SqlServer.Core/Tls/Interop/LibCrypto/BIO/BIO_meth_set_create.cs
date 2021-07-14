using System.Runtime.InteropServices;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "BIO_meth_set_create")]
        private static extern int Internal_BIO_meth_set_create(BIO_METHOD biom, CreateDelegate method);

        public static void BIO_meth_set_create(BIO_METHOD biom, CreateDelegate method)
        {
            var returnCode = Internal_BIO_meth_set_create(biom, method);
            ThrowOnErrorReturnCode(returnCode);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CreateDelegate(BIO bio);
    }
}

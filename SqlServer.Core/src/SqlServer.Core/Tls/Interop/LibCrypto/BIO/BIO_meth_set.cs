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
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "BIO_meth_set_write")]
        private static extern int Internal_BIO_meth_set_write(BIO_METHOD biom, WriteDelegate method);

        public static void BIO_meth_set_write(BIO_METHOD biom, WriteDelegate method)
        {
            var returnCode = Internal_BIO_meth_set_write(biom, method);
            ThrowOnErrorReturnCode(returnCode);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int WriteDelegate(BIO bio, void* buf, int num);

        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "BIO_meth_set_read")]
        private static extern int Internal_BIO_meth_set_read(BIO_METHOD biom, ReadDelegate method);

        public static void BIO_meth_set_read(BIO_METHOD biom, ReadDelegate method)
        {
            var returnCode = Internal_BIO_meth_set_read(biom, method);
            ThrowOnErrorReturnCode(returnCode);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate int ReadDelegate(BIO bio, void* buf, int size);

        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl, EntryPoint = nameof(BIO_meth_set_ctrl))]
        private static extern int Internal_BIO_meth_set_ctrl(BIO_METHOD biom, ControlDelegate controlMethod);

        internal static void BIO_meth_set_ctrl(BIO_METHOD biom, ControlDelegate controlMethod)
        {
            var result = Internal_BIO_meth_set_ctrl(biom, controlMethod);
            ThrowOnErrorReturnCode(result);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate long ControlDelegate(BIO bio, BIO_ctrl cmd, long num, void* ptr);

        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl, EntryPoint = "BIO_meth_set_destroy")]
        private static extern int Internal_BIO_meth_set_destroy(BIO_METHOD biom, DestroyDelegate method);

        public static void BIO_meth_set_destroy(BIO_METHOD biom, DestroyDelegate method)
        {
            var returnCode = Internal_BIO_meth_set_destroy(biom, method);
            ThrowOnErrorReturnCode(returnCode);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DestroyDelegate(BIO bio);
    }
}

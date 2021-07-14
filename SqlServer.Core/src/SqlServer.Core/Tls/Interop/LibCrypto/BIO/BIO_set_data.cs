using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        [DllImport(Libraries.LibCrypto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BIO_set_data(BIO a, IntPtr ptr);

        public static void BIO_set_data(BIO bio, GCHandle handle) => BIO_set_data(bio, GCHandle.ToIntPtr(handle));

        public unsafe static void BIO_set_data<T>(BIO bio, ref T data) where T : struct
        {
            var ptr = Unsafe.AsPointer(ref data);
            BIO_set_data(bio, (IntPtr)ptr);
        }

        public static void BIO_reset_data(BIO bio) => BIO_set_data(bio, IntPtr.Zero);
    }
}

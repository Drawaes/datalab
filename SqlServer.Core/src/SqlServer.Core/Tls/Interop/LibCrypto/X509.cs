using System;
using System.Runtime.InteropServices;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct X509
        {
            private IntPtr _ptr;

            public bool IsValid => _ptr != IntPtr.Zero;

            public void Free()
            {
                if (IsValid) return;
                X509_free(this);
                _ptr = IntPtr.Zero;
            }
        }
    }
}

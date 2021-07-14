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
        public class PKCS12 : SafeHandle
        {
            private PKCS12() : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                if (IsInvalid) return false;

                PKCS12_free(handle);
                return true;
            }
        }
    }
}

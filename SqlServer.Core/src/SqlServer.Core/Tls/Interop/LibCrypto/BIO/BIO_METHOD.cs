using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class LibCrypto
    {
        public struct BIO_METHOD
        {
            private IntPtr _pointer;

            public void Free()
            {
                if (_pointer != IntPtr.Zero)
                {
                    BIO_meth_free(this);
                    _pointer = IntPtr.Zero;
                }
            }
        }
    }
}

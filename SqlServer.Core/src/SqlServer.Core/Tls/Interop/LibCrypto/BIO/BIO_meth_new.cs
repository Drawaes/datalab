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
        internal static extern BIO_METHOD BIO_meth_new(int type, string name);
    }
}

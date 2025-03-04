﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls.Interop
{
    internal static partial class OpenSsl
    {
        [DllImport(Libraries.LibSsl, CallingConvention = CallingConvention.Cdecl)]
        public static extern SslErrorCodes SSL_get_error(SSL ssl, int code);

        public enum SslErrorCodes
        {
            SSL_NOTHING = 1,
            SSL_WRITING = 2,
            SSL_READING = 3,
            SSL_X509_LOOKUP = 4,
            SSL_ASYNC_PAUSED = 5,
            SSL_ASYNC_NO_JOBS = 6,
            SSL_EARLY_WORK = 7,
        }
    }
}

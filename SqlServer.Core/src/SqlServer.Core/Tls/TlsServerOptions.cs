using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls
{
    public class TlsServerOptions
    {
        public string CertificateFile { get; set; }
        public string CertificatePassword { get; set; }
    }
}

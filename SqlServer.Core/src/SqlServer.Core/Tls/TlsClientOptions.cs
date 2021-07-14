using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core.Tls
{
    public class TlsClientOptions
    {
        public string CertificatePassword { internal get; set; }
        public string CertificateFile { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core
{
    internal class SslStreamWrapper : IDuplexPipe
    {
        public PipeReader Input => throw new NotImplementedException();

        public PipeWriter Output => throw new NotImplementedException();

        public SslStreamWrapper(IDuplexPipe pipe)
        {
            
        }

    }
}

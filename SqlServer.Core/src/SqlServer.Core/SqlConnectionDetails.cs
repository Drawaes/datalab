using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core
{
    public class SqlConnectionDetails
    {
        private PipeOptions _pipeOptions;

        public string Username { get; set; }
        public string Password { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string AttachDBFilename { get; set; }
        public string Language { get; set; }
        public PipeOptions PipeOptions { set => _pipeOptions = value; get => _pipeOptions ?? new PipeOptions(System.Buffers.MemoryPool<byte>.Shared); }

        internal string HostName { get; } = System.Net.Dns.GetHostName();
        internal string AppName { get; } = "Microsoft SQL Server Management Studio";
        internal string LibraryName { get; } = ".Net SqlClient Data Provider";

        internal int ByteSizeOfStrings() =>
            (Username.Length
            + Password.Length
            + Server.Length
            + (Database?.Length ?? 0)
            + (AttachDBFilename?.Length ?? 0)
            + (Language?.Length ?? 0)
            + HostName.Length
            + AppName.Length
            + LibraryName.Length) * 2;
    }
}

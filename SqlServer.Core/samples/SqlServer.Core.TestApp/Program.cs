using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Pipelines.Sockets.Unofficial;

namespace SqlServer.Core.TestApp
{
    class Program
    {
        private static SqlConnectionDetails _connectionDetails;

        static async Task Main(string[] args)
        {
            _connectionDetails = new SqlConnectionDetails()
            {
                Server = "127.0.0.1",
                Database = "testdb",
                Password = "testpassword2",
                Username = "testuser"
            };
            await PipeClient();
            Console.ReadLine();
        }

        static async Task PipeClient()
        {
            for (var x = 0; x < 3; x++)
            {
                var connection = await SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 1433));
                var sqlConnection = new SqlPipe(connection, _connectionDetails);
                await sqlConnection.ConnectAsync();

                var sw = Stopwatch.StartNew();

                // Now we have logged in, lets try a simple query
                var reader = await sqlConnection.ExecuteQueryAsync("SELECT * FROM [Values]");
                await reader.ReadMetaDataAsync();
                // meta data should be loaded now

                var i = 0;
                while (await reader.ReadRowAsync())
                {
                    ReadRow(reader);
                    i++;
                }
                var time = sw.ElapsedMilliseconds;
                Console.WriteLine($"{i} Total rows read time elapsed {time}");
            }
        }

        static void ReadRow(DataReader dr)
        {
            var rowReader = dr.GetRowReader();
            rowReader.ReadInt();
            var isNull = rowReader.ReadVarCharNullable(out var value);
            rowReader.ReadDateTime();
        }

        static async Task SqlClient()
        {
            for (var x = 0; x < 3; x++)
            {
                using (var connection = new SqlConnection($"Server={_connectionDetails.Server};Database={_connectionDetails.Database};User Id={_connectionDetails.Username};Password = {_connectionDetails.Password}"))
                {
                    await connection.OpenAsync();

                    var command = new SqlCommand("SELECT * FROM [Values]", connection);
                    var sw = Stopwatch.StartNew();
                    var reader = await command.ExecuteReaderAsync();

                    var i = 0;
                    while (await reader.ReadAsync())
                    {
                        reader.GetInt32(0);
                        reader.GetString(1);
                        reader.GetDateTime(2);
                        i++;
                    }
                    var time = sw.ElapsedMilliseconds;
                    reader.Dispose();
                    command.Dispose();
                    Console.WriteLine($"{i} Total rows read time elapsed {time}");
                }
            }
        }
    }
}

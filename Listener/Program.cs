using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Listener
{
    class Program
    {
        private static void Log(string message)
        {
            Console.WriteLine(message);
        }

        private static void Log(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        
        static async Task Main(string[] args)
        {
            int port = args.Length == 0 ? 8222 : int.Parse(args[0], CultureInfo.InvariantCulture);
            using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(IPAddress.Loopback, port);
            serverSocket.Bind(endpoint);
            serverSocket.Listen(1);

            Log($"Server created on port {port}");

            while (true)
            {
                try
                {
                    using var clientSocket = await serverSocket.AcceptAsync();
                    Log($"Client socket accepted");

                    var dataReceived = new List<byte>();
                    var buffer = new byte[4096];
                    while (clientSocket.Connected)
                    {
                        var dataRead = clientSocket.Receive(buffer);
                        if (dataRead == 0) continue;
                        dataReceived.AddRange(buffer.Take(dataRead));

                        var zeroIndex = dataReceived.IndexOf(0);
                        while (zeroIndex != -1)
                        {
                            ExecuteCommand(dataReceived, zeroIndex, clientSocket);
                            dataReceived = dataReceived.Skip(zeroIndex + 1).ToList();
                            zeroIndex = dataReceived.IndexOf(0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private static void ExecuteCommand(List<byte> dataReceived, int endIndex, Socket socket)
        {
            var commandSpan = CollectionsMarshal.AsSpan(dataReceived)[..endIndex];
            var command = Encoding.UTF8.GetString(commandSpan);
            var words = command.Split(' ');
            switch (words[0])
            {
                case "guid":
                    var count = int.Parse(words[1], CultureInfo.InvariantCulture);
                    Log($"Generating {count} GUIDs");
                    var guids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
                    foreach (var guid in guids)
                    {
                        var data = Encoding.UTF8.GetBytes(guid.ToString());
                        socket.Send(data);
                    }
                    Log($"{count} GUIDs sent successfully");
                    break;
                default: throw new Exception($"Unknown command: {command}");
            }
        }
    }
}
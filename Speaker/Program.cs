using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Speaker
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
        
        static int Main(string[] args)
        {
            int port = args.Length == 0 ? 8222 : int.Parse(args[0], CultureInfo.InvariantCulture);
            bool stopAtRandom = true;
            
            const int guidCount = 2000;
            var rand = new Random();
            try
            {
                while (true)
                {
                    int disconnectAfter = stopAtRandom ? rand.Next(guidCount) : guidCount;
                    Log($"Disconnecting after receiving {disconnectAfter} out of {guidCount} GUIDs");

                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var endPoint = new IPEndPoint(IPAddress.Loopback, port);

                    var command = $"guid {guidCount}\0";
                    socket.Connect(endPoint);
                    // var array = new List<byte>();
                    // for (int i = 0; i < 150; ++i)
                        // array.AddRange(Encoding.UTF8.GetBytes(command));
                    socket.SendAsync(Encoding.UTF8.GetBytes(command), 0);

                    Task.Run(() =>
                    {
                        try
                        {
                            var guids = new List<Guid>();
                            var buffer = new byte[1024];
                            var dataReceived = new List<byte>();
                            while (guids.Count < disconnectAfter)
                            {
                                var byteCount = socket.Receive(buffer);
                                dataReceived.AddRange(buffer.Take(byteCount));
                                while (dataReceived.Count >= 36)
                                {
                                    var span = CollectionsMarshal.AsSpan(dataReceived)[..36];
                                    var datum = Encoding.UTF8.GetString(span);
                                    var guid = Guid.Parse(datum);
                                    guids.Add(guid);

                                    dataReceived = dataReceived.Skip(36).ToList();
                                }
                            }
                            
                            Log("Read success");
                        }
                        catch (SocketException)
                        {
                            Log("Read terminated");
                        }
                    });
                    
                    Thread.Sleep(1);
                    
                    Log("Closing the socket");
                    socket.Close(0);
                }
            }
            catch (Exception ex)
            {
                Log(ex);
                return 1;
            }
        }
    }
}
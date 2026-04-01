using System.Net;

namespace Server;

class Program
{
    static void Main(string[] args)
    {
        Server server = new Server(IPAddress.Loopback, 12345);
        server.Start();
    }
}
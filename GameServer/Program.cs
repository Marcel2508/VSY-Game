using System;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TCPServer server = new TCPServer(9000);
            GameServerRunner gameClass = new GameServerRunner(server);

            //Starting to listen for TCL Clients...
            server.startServer();
            Console.WriteLine("ENDED");
        }
    }
}

using System;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerPacketHandler server = new ServerPacketHandler(8099);
        }
    }
}

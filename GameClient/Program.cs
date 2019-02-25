using System;

namespace GameClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ClientSocket sock = new ClientSocket("127.0.0.1",8099);
            sock.onPacket += PacketHandler;
            sock.Send(new ConnectPacket("Tester teston"));

        }

        private static void PacketHandler(object sender, OnPacketEventArgs e){
            switch(e.type){
                case PacketTypes.CONNECT_ACK:
                    Console.WriteLine("Connect packet!");
                    break;
            }
        }
    }
}

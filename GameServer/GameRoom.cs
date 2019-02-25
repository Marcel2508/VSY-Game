using System;

namespace GameServer
{
    class GameRoom
    {
        private Client client1;
        private string client1Name = "UNDEFINED";
        private Client client2;
        private string client2Name = "UNDEFINED";
        private int[,] fieldState = new int[6,7];
        public GameRoom(Client c1,string name){
            for(int y=0;y<6;y++)
                for(int x =0;x<7;x++)fieldState[y,x] = 0;
            this.client1 = c1;
            this.client1Name = name;
            Console.WriteLine("New Gameroom Created! Player 1: "+name);
        }
        public void Client2Connect(Client c2,string name){
            this.client2 = c2;
            this.client2Name = name;
            this.client1.onPacket += this.PacketListener;
            this.client2.onPacket += this.PacketListener;
            this.emitGameBegin();
        }

        private void emitGameBegin(){

        }
        public void PacketListener(object sender, OnPacketEventArgs e){

        }
    }
}

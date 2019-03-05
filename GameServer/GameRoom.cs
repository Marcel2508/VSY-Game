#define GAME_ROOM_EXIST

using System;

namespace GameServer
{
    enum RoomStates{
        WAITING=0x01,
        CLIENT1_TURN=0x02,
        CLIENT2_TURN=0x03,
        END=0x04
    }
    class GameRoom
    {
        private GameServerRunner server;
        private GameClient client1;
        private GameClient client2;
        private int[,] fieldState = new int[7,6];
        private int roomId = -1;

        private RoomStates state = RoomStates.WAITING;

        public int Roomid{
            get{return this.roomId;}
        }
        public GameClient Client1{
            get{return this.client1;}
        }
        public GameClient Client2{
            get{return this.client2;}
        }

        public bool Full{
            get{return this.client2==null?false:true;}
        }

        public GameRoom(GameServerRunner server,int roomId, GameClient c1){
            this.server = server;
            for(int x=0;x<7;x++)
                for(int y =0;y<6;y++)fieldState[x,y] = 0;
            this.client1 = c1;
            this.roomId = roomId;
            c1.RoomId=roomId;
            Console.WriteLine("New Gameroom Created! Player 1: "+c1.Name);
            //Room Sends Connect Ack
            c1.Send(new ConnectAckPacket(0,c1.Id,roomId));
            this.client2 = null;
            this.state = RoomStates.WAITING;
            c1.setIngame(this.roomId);
        }
        public void Client2Connect(GameClient c2){
            this.client2 = c2;
            this.client1.onPacket += this.PacketListener;
            this.client2.onPacket += this.PacketListener;
            c2.Send(new ConnectAckPacket(1,c2.Id,this.roomId));
            this.emitGameBegin();
            Console.WriteLine("Game Room "+this.roomId+" begins Game!");
            c2.setIngame(this.roomId);
        }

        private void emitGameBegin(){
            this.client1.Send(new GameReadyPacket(this.client2.Name,true));
            this.client2.Send(new GameReadyPacket(this.client1.Name,false));
            this.state = RoomStates.CLIENT1_TURN;
        }

        private int getSlot(int column){    
            if(column <7&&column>=0){
                for(int y=5;y>=0;y--){
                    if(this.fieldState[column,y]==0)return y;
                }
                return -1;
            }
            return -1;
        }

        private bool checkWinState(int player)
        {
            int i,j;
            for(i=0;i<7;i++)
                for(j=0;j<6-3;j++)
                    if(this.fieldState[i,j] != 0 && this.fieldState[i,j]==this.fieldState[i,j+1] && this.fieldState[i,j]==this.fieldState[i,j+2] && this.fieldState[i,j]==this.fieldState[i,j+3])
                        return true;

            //checks vertical win
            for(i=0;i<7-3;i++)
                for(j=0;j<6;j++)
                    if(this.fieldState[i,j] != 0 && this.fieldState[i,j]==this.fieldState[i+1,j] && this.fieldState[i,j]==this.fieldState[i+2,j] && this.fieldState[i,j]==this.fieldState[i+3,j])
                       return true;

            //checks rigth diagonal win
            for(i=0;i<7-3;i++)
                for(j=0;j<6-3;j++)
                    if(this.fieldState[i,j] != 0 && this.fieldState[i,j]==this.fieldState[i+1,j+1] && this.fieldState[i,j]==this.fieldState[i+2,j+2] && this.fieldState[i,j]==this.fieldState[i+3,j+3])
                        return true;

            //checks left diagonal win
            for(i=0;i<7-3;i++)
                for(j=5;j>2;j--)
                    if(this.fieldState[i,j] != 0 && this.fieldState[i,j]==this.fieldState[i+1,j-1] && this.fieldState[i,j]==this.fieldState[i+2,j-2] && this.fieldState[i,j]==this.fieldState[i+3,j-3])
                       return true;
            return false;
        }

        private void checkGameState(){
            if(this.checkWinState(1)){
                this.client1.Send(new GameEndPacket(true,new int[4]{0,0,0,0}));
                this.client2.Send(new GameEndPacket(false,new int[4]{0,0,0,0}));
                this.state = RoomStates.END;
                this.closeRoom(true);
            }
            else if(this.checkWinState(2)){
                this.client1.Send(new GameEndPacket(false,new int[4]{0,0,0,0}));
                this.client2.Send(new GameEndPacket(true,new int[4]{0,0,0,0}));
                this.state = RoomStates.END;
                this.closeRoom(true);
            }
        }

        private void handleAction(GameClient c, GameActionPacket p){
            if(c.Id==this.client1.Id){
                //Its client 1
                if(this.state==RoomStates.CLIENT1_TURN){
                    int ySlot = this.getSlot(p.Column);
                    if(ySlot!=-1){
                        this.fieldState[p.Column,ySlot]=1;
                        GameActionNotifyPacket np = new GameActionNotifyPacket(p.Column);
                        this.client1.Send(np);
                        this.client2.Send(np);
                        this.state = RoomStates.CLIENT2_TURN;
                        this.checkGameState();
                    }
                    else //Invalid Move!
                        c.Send(new GameActionNotifyPacket(p.Column,false));
                }
                else
                    c.Send(new GameErrorPacket(0x03,"Its not your Turn!"));
            }
            else if(c.Id==this.client2.Id){
                if(this.state==RoomStates.CLIENT2_TURN){
                    int ySlot = this.getSlot(p.Column);
                    if(ySlot!=-1){
                        this.fieldState[p.Column,ySlot]=2;
                        GameActionNotifyPacket np = new GameActionNotifyPacket(p.Column);
                        this.client1.Send(np);
                        this.client2.Send(np);
                        this.state = RoomStates.CLIENT1_TURN;
                        this.checkGameState();
                    }
                    else //Invalid Move!
                        c.Send(new GameActionNotifyPacket(p.Column,false));
                }
                else
                    c.Send(new GameErrorPacket(0x03,"Its not your Turn!"));
            }
        }

        private void PacketListener(object sender, OnPacketEventArgs e){
            switch(e.Type){
                case PacketTypes.GAME_ACTION:
                    this.handleAction((GameClient)sender,(GameActionPacket)e.Packet);
                    break;
            }
        }

        public void closeRoom(bool ended, string message="",GameClient disconClient = null){
            this.client1.onPacket -= this.PacketListener;
            if(this.Full){
                this.client2.onPacket -= this.PacketListener;
                if(ended==false){
                    if(this.client1 == disconClient)this.client2.Send(new GameEndPacket(true,new int[]{0,0,0,0},true));
                    else this.client1.Send(new GameEndPacket(true,new int[]{0,0,0,0},true));
                }
            }
            this.server.removeRoom(this);
            //Removing Room from list
        }
    }
}

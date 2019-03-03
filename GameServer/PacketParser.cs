using System;
using System.Collections.Generic;
using System.Threading;

namespace GameServerOld
{/*
    class OnPacketEventArgs : EventArgs{
        private PacketTypes _type;
        private BasePacket _packet;
        public PacketTypes type{
            get{return this._type; }
        }
        public BasePacket packet{
            get{return this._packet;}
        }

        public OnPacketEventArgs(PacketTypes t, BasePacket p){
            this._type = t;
            this._packet = p;
        }
    }
    class OnClientConnectionEventArgs : EventArgs{
        private string _name;
        private Client _client;

        public string Name{
            get{return _name;}
        }
        public Client Client{
            get{return _client;}
        }
        public OnClientConnectionEventArgs(string name,Client client){
            this._name=name;
            this._client = client;
        }
    }

    class OnConnectionLostEventArgs : EventArgs{
        private long lastContact;
        public long LastContact{
            get{return lastContact;}
        }

        public OnConnectionLostEventArgs(long lc){
            this.lastContact = lc;
        }
    }

    class ServerPacketHandler
    { 
        private TCPServer server;
        private List<GameRoom> rooms = new List<GameRoom>();
        private bool newRoom = true;
        public ServerPacketHandler(int serverPort){
            this.server = new TCPServer(serverPort);
            Console.WriteLine("SUB");
            this.server.onClient += this.handleClient;
            this.server.startServer();
        }

        private void connectPacketHandler(object sender, OnPacketEventArgs e){
            if(e.type == PacketTypes.CONNECT){
                Console.WriteLine("Client Connected "+((ConnectPacket)e.packet).Name);
                if(newRoom){
                    //send: Ack packet
                    this.rooms.Add(new GameRoom((Client)sender,((ConnectPacket)e.packet).Name));
                    newRoom=false;
                }
                else{
                    //Send: ack packet
                    this.rooms[rooms.Count-1].Client2Connect((Client)sender,((ConnectPacket)e.packet).Name);
                    newRoom=true;
                }
            }
            else{
                Console.WriteLine("ERR: INVALID FIRST PACKET RECEIVED!");
            }
            ((Client)sender).onPacket -= this.connectPacketHandler;
        }
        private void handleClient(object sender, ClientConnectedEventArgs e){
            //EMIT EVENT
            Client c = new Client(e.handler,e.id);
            c.onPacket += this.connectPacketHandler;
        }
    }

    class Client{
        private ClientHandler client;
        private int id;

        public event EventHandler<OnPacketEventArgs> onPacket;
        public event EventHandler<OnConnectionLostEventArgs> onConnectionLost;
        
        private long _lastContact = 0;
        private Timer pingCheckTimer;

        public Client(ClientHandler h, int id){
            this.client = h;
            this.id = id;
            this.client.onPacket += this.convertPacket;
            this.pingCheckTimer = new Timer(this.checkConnectionState,null,5000,5000);
        }

        private void checkConnectionState(Object e){
            if(DateTimeOffset.UtcNow.ToUnixTimeSeconds()-this._lastContact>10000){
                this.emitConnectionLost();
            }
        }

        protected virtual void emitPacketEvent(OnPacketEventArgs e)
        {
            EventHandler<OnPacketEventArgs> handler = onPacket;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void emitConnectionLost()
        {
            //When 4x no ping packet received: Destroy Connection an client Handler
            Console.WriteLine("Connection lost");
            this.client.CloseConnection();
            EventHandler<OnConnectionLostEventArgs> handler = onConnectionLost;
            if (handler != null)
            {
                handler(this, new OnConnectionLostEventArgs(this._lastContact));
            }
        }
        
        private void convertPacket(object sender, RawPacketReceiveEventArgs e){
            PacketTypes p = (PacketTypes)BitConverter.ToInt16(e.data,0);
            Console.WriteLine("Received Packet "+Convert.ToString(p));
            switch(p){
                case PacketTypes.PING:
                    this._lastContact = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    this.client.Send(new PongPacket());
                    break;
                case PacketTypes.CONNECT:
                    //EMIT:
                    emitPacketEvent(new OnPacketEventArgs(PacketTypes.CONNECT,new ConnectPacket(e.data)));
                    break;
                case PacketTypes.CONNECT_ACK:
                    //EMIT:
                    emitPacketEvent(new OnPacketEventArgs(PacketTypes.CONNECT_ACK,new ConnectAckPacket(e.data)));
                    break;
            }
        }
    } */
}

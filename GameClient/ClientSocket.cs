using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace GameClient
{

    class OnPacketEventArgs : EventArgs{
      private PacketTypes _type;
      private BasePacket _packet;
      public PacketTypes type{
        get{return this._type;}
      }
      public BasePacket packet{
        get{return this._packet;}
      }

      public OnPacketEventArgs(PacketTypes t, BasePacket p){
        this._type = t;
        this._packet = p;
      }
    }

    class ConnectionLostEventArgs : EventArgs{
        private long lastConnection;
        public long LastConnection{
            get{return lastConnection;}
        }

        public ConnectionLostEventArgs(long lc){
            this.lastConnection = lc;
        }
    }

    class ClientSocket
    {
        private TcpClient socket;
        private Thread socketThread;

        private int bufferSize = 0;
        private List<byte[]> parts = new List<byte[]>();
        private short state = 0;//0: Head, 1: Payload
        private int payloadLength = 0;
        private bool parseMore = true;
        private bool run = true;

        private long lastContact = 0;
        private Timer pingSendTimer;
        private Timer disconnectDetection;

        public event EventHandler<OnPacketEventArgs> onPacket;
        public event EventHandler<ConnectionLostEventArgs> onConnectionLost;

        protected virtual void EmitPacketEvent(OnPacketEventArgs e)
        {
            EventHandler<OnPacketEventArgs> handler = onPacket;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void EmitConnectionLostEvent()
        {
            Console.WriteLine("Connection Lost!");
            this.CloseConnection();
            EventHandler<ConnectionLostEventArgs> handler = onConnectionLost;
            if (handler != null)
            {
                handler(this, new ConnectionLostEventArgs(this.lastContact));
            }
        }

        public void CloseConnection(){
            this.run = false;
        }

        public ClientSocket(string ip, int port){
            this.socket = new TcpClient();
            try{
                this.socket.Connect(ip,port);
                Console.WriteLine("Client connected!");
                this.startClient();
            }
            catch(Exception ex){
                Console.WriteLine("ERROR Connection to Remote Socket!");
                Console.WriteLine(ex.Message);
            }
        }   

        public void startClient(){
            this.socketThread = new Thread(this.runListener);
            this.socketThread.Start();
            this.pingSendTimer = new Timer(this.sendPing,null,2500,2500);
            this.disconnectDetection = new Timer(this.checkConnection,null,5000,5000);
        }

        private void sendPing(object e){
            this.Send(new PingPacket());
        }

        private void checkConnection(object e){
            if(DateTimeOffset.UtcNow.ToUnixTimeSeconds()-this.lastContact>10000){
                this.EmitConnectionLostEvent();
            }
        }

        private void runListener(){
            NetworkStream stream = this.socket.GetStream();
            while(this.run){
                if(this.socket.Available>0){
                    this.bufferSize+=this.socket.Available;
                    byte[] tmp = new byte[this.socket.Available];
                    stream.Read(tmp,0,this.socket.Available);
                    this.parts.Add(tmp);
                    this.parseMore=true;
                    this.dataHandler();
                }
                System.Threading.Thread.Sleep(10);
            }
            this.socket.Close();
            this.pingSendTimer.Dispose();
            this.disconnectDetection.Dispose();
        }

        private bool dataAvailable(int count){
            if(this.bufferSize>=count)return true;
            this.parseMore=false;
            return false;
        }
        private byte[] getData(int count){
            this.bufferSize-=count;
            if(this.parts[0].Length==count){
                byte[] t = this.parts[0];
                this.parts.RemoveAt(0);
                return t;
            }
            else if(this.parts[0].Length>count){
                byte[] t = new byte[count];
                byte[] r = new byte[this.parts[0].Length-count];
                Array.Copy(this.parts[0],t,count);
                Array.Copy(this.parts[0],count,r,0,this.parts[0].Length-count);
                this.parts[0] = r;
                return t;
            }
            else{
                //Need to combine multiple ...
                byte[] res = new byte[count];
                int offset = 0;
                int l = 0;
                while(count>0){
                    l = this.parts[0].Length;
                    if(count >= l){
                        Array.Copy(this.parts[0],0,res,offset,l);
                        offset += l;
                        this.parts.RemoveAt(0);
                    }
                    else{
                        Array.Copy(this.parts[0],0,res,offset,count);
                        byte[] r = new byte[l-count];
                        Array.Copy(this.parts[0],count,r,0,l-count);
                        this.parts[0] = r;
                    }
                    count -= l;
                }
                return res;
            }
        }

        private void readHeader(){
            if(this.dataAvailable(2)){
                this.payloadLength = BitConverter.ToInt16(this.getData(2));
                this.state=1;
            }
        }
        private void readPayload(){
            if(this.dataAvailable(this.payloadLength)){
                byte[] data = this.getData(this.payloadLength);
                PacketTypes p = (PacketTypes)BitConverter.ToInt16(data,0);
                Console.WriteLine("Received packed");
                switch(p){
                    case PacketTypes.PONG:
                        this.lastContact = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        break;
                    case PacketTypes.CONNECT_ACK:
                        this.EmitPacketEvent(new OnPacketEventArgs(p,new ConnectAckPacket(data)));
                    break;
                    
                }
                this.state=0;
            }
        }

        private void dataHandler(){
            while(this.parseMore){
                if(this.state==0){
                    this.readHeader();
                }
                else{
                    this.readPayload();
                }
            }
        }

        //SENDING:
        public void Send(BasePacket packet){
            Console.WriteLine("Sending Packet");
            byte[] d = packet.getBytes();
            byte[] c = new byte[d.Length+2];
            BitConverter.GetBytes(Convert.ToInt16(d.Length)).CopyTo(c,0);
            d.CopyTo(c,2);
            try{
                this.socket.GetStream().Write(c,0,c.Length);
            }
            catch{
                this.EmitConnectionLostEvent();
            }
        }
    }
}

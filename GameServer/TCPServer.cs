using System;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Collections.Generic;

namespace GameServer
{
    class TCPServer
    {
        private TcpListener serverSocket;
        private bool run = true;

        private List<ClientHandler> clientListe = new List<ClientHandler>();

        public event EventHandler<ClientConnectedEventArgs> onClient;
        public TCPServer(int port){
            this.serverSocket = new TcpListener(IPAddress.Any,port);
        }

        public void startServer(){
            TcpClient clientSocket = default(TcpClient);
            serverSocket.Start();
            Console.WriteLine("Server started...");
            while(run){
                clientSocket = serverSocket.AcceptTcpClient();
                ClientHandler client = new ClientHandler(clientSocket);
                client.startClient();
                this.clientListe.Add(client);
                this.OnClientEmitter(new ClientConnectedEventArgs(this.clientListe.Count-1,client));
            }
            serverSocket.Stop();
        }

        protected virtual void OnClientEmitter(ClientConnectedEventArgs e){
            EventHandler<ClientConnectedEventArgs> handler = this.onClient;
            if(handler != null){
                handler(this,e);
            }
        }
    }

    class RawPacketReceiveEventArgs : EventArgs{
        private int _length;
        private byte[] _data;
        public int length{
            get{return _length;}
        }
        public byte[] data{
            get{return _data;}
        }

        public RawPacketReceiveEventArgs(int l, byte[] d){
            this._length = l;
            this._data = d;
        }
    }

    class ClientConnectedEventArgs : EventArgs{
        private int _id = 0;
        private ClientHandler _handler;
        public int id{
            get{return this._id;}
        }
        public ClientHandler handler{
            get{return this._handler;}
        }

        public ClientConnectedEventArgs(int i, ClientHandler h){
            this._id = i;
            this._handler = h;
        }
    }

    class ClientHandler
    {
        private TcpClient socket;
        private Thread socketThread;

        private int bufferSize = 0;
        private List<byte[]> parts = new List<byte[]>();
        private short state = 0;//0: Head, 1: Payload
        private int payloadLength = 0;
        private bool parseMore = true;
        private bool run = true;

        public event EventHandler<RawPacketReceiveEventArgs> onPacket;

        protected virtual void EmitPacketEvent(RawPacketReceiveEventArgs e)
        {
            EventHandler<RawPacketReceiveEventArgs> handler = onPacket;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void CloseConnection(){
            this.run = false;
        }

        public ClientHandler(TcpClient socket){
            Console.WriteLine("New Socket Connected!");
            this.socket = socket;
        }   

        public void startClient(){
            this.socketThread = new Thread(this.runListener);
            this.socketThread.Start();
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
                this.EmitPacketEvent(new RawPacketReceiveEventArgs(this.payloadLength,data));
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
            this.socket.GetStream().Write(c,0,c.Length);
        }
    }
}
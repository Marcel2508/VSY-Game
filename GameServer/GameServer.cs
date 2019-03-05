using System;
using System.Collections.Generic;

namespace GameServer
{
  enum ClientStates{
    WAITING=0x01,
    CONNECTED=0x02,
    INGAME=0x03,
    DISCONNECTED=0x04
  }
  class GameClient{

    private ClientStates state = ClientStates.WAITING;
    private int room = -1;
    private int id = -1;
    private string name = "NOT_AVAILABLE";
    private ClientHandler handler = null;

    public ClientHandler Handler{
      get{return this.handler;}
    } 
    public int Id{
      get{return this.id;}
    }
    private int uuid = -1;

    private GameServerRunner server=null;

    public event EventHandler<OnPacketEventArgs> onPacket;

    public int RoomId{
      get{return this.room;}
      set{this.room=value;}
    }
    public string Name{
      get{return this.name;}
    }

    public void close(){
      this.handler.CloseConnection();
    }
    
    public void Send(BasePacket p){
      this.handler.Send(p);
    }
    
    public GameClient(int i,GameServerRunner s,ClientHandler h){
      this.id = i;
      this.handler = h;
      this.server = s;
      this.handler.onPacket += this.handlePacketProxy;
      this.handler.onDisconnect += this.handleDisconnect;
    }

    private void handleConnectPacket(ConnectPacket p){
      if(this.state==ClientStates.WAITING){
            this.name = p.Name;
            if(p.TargetRoom==-1){
              this.server.createNewRoom(this);
              this.state = ClientStates.CONNECTED;
            }
            else{
              if(this.server.roomNum(p.TargetRoom)!=-1){
                this.server.addToRoom(this,p.TargetRoom);
                this.state = ClientStates.CONNECTED;
              }
              else
                this.Send(new GameErrorPacket(0x02,"Room full or not existing!"));
            }
          }
          else
            this.Send(new GameErrorPacket(0x01,"Invalid Client State to perform this action!",true,false));
    }

    private void handleClientEndPacket(GameClientEndPacket p){
      Console.WriteLine("Client \""+this.name+"\" gracefully closed Connection! Message: \""+p.Message+"\"");
      this.state = ClientStates.DISCONNECTED;
      this.server.closeRoom(this.room,this,"Client closed connection: "+p.Message);
      this.handler.CloseConnection();
      this.server.removeClient(this);
    }

    private void handleDisconnect(object sender, OnDisconnectEventArgs e){
      Console.WriteLine("Client \""+this.name+"\" lost Connection!");
      this.state = ClientStates.DISCONNECTED;
      this.server.closeRoom(this.room,this,"Client lost connection!");
      this.handler.CloseConnection();
      this.server.removeClient(this);
    }

    private void handlePacketProxy(object sender, OnPacketEventArgs e){
      switch(e.Type){
        case PacketTypes.GET_ROOM_LIST:
          RoomListPacket l = (RoomListPacket)this.server.GetRoomListPacket();
          RoomListPacket l2 = new RoomListPacket(l.getBytes());
          Console.WriteLine("Room Count: "+l2.RoomCount);
          for(int i=0;i<l2.RoomCount;i++){
            Console.WriteLine(l2.RoomList[i].Id+": "+l2.RoomList[i].P1Name);
          }
          this.Send(l);
          break;
        case PacketTypes.CONNECT:
          this.handleConnectPacket((ConnectPacket)e.Packet);
          break;
        case PacketTypes.GAME_ERR:
          GameErrorPacket p = (GameErrorPacket)e.Packet;
          Console.WriteLine("Received Client Error: #"+p.Code+" \""+p.Message+"\"");
          break;
        case PacketTypes.GAME_CLIENT_END:
          this.handleClientEndPacket((GameClientEndPacket)e.Packet);
          break;
        default:
          //EMIT Packet for GameRooms
          this.emitPacketEvent(e);
          break;
      }
    }

    private void emitPacketEvent(OnPacketEventArgs e){
      EventHandler<OnPacketEventArgs> handler = this.onPacket;
      if(handler != null){
          handler(this,e);
      }
    }

    public void resetClient(){
      this.room = -1;
      this.state = ClientStates.WAITING;
    }

    public void setIngame(int roomId){
      this.state = ClientStates.INGAME;
      this.room = roomId;
    }
  
  }
  class GameServerRunner
  {
    private List<GameClient> clients = new List<GameClient>();
    private List<GameRoom> rooms = new List<GameRoom>();

    int clientCounter = 0;
    int roomCounter = 0;

    private int getUUID(){
      return this.clientCounter++;
    }
    private int getRoomId(){
      return this.roomCounter++;
    }

    public GameServerRunner(TCPServer server){
      server.onClient += this.handleClient;
    }
    
    public int roomNum(int rid){
      for(int i=0;i<this.rooms.Count;i++){
        if(this.rooms[i].Roomid==rid){
          return i;
        }
      }
      return -1;
    }

    public void createNewRoom(GameClient client){
      GameRoom r = new GameRoom(this,this.getRoomId(),client);
      this.rooms.Add(r);
    }

    public void addToRoom(GameClient client, int roomId){
      if(this.roomNum(roomId)!=-1)
        this.rooms[this.roomNum(roomId)].Client2Connect(client);
    }

    public void closeRoom(int roomId,GameClient client,string message){
      //called by disconnected clients
      if(this.roomNum(roomId)!=-1)
        this.rooms[this.roomNum(roomId)].closeRoom(false,message,client);
    }

    private void handleClient(object sender, OnSocketConnectionEventArgs e){
      this.clients.Add(new GameClient(this.getUUID(),this,e.handler));
    }

    public BasePacket GetRoomListPacket(){
      return new RoomListPacket(this.rooms.ToArray());
    }

    public void removeClient(GameClient c){
      this.clients.Remove(c);
    }

    public void removeRoom(GameRoom room){
      //Reset clients and destory Room
      this.rooms.Remove(room);
      room.Client1.resetClient();
      if(room.Full)
        room.Client2.resetClient();
    }

  }
}

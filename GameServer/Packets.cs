#define GAME_ROOM_EXIST
using System;

namespace GameServer
{
    enum PacketTypes{
      BASE=-1,
      PING=0x00,//Sent regulary by Client (Slave) to check if still connected
      PONG=0x01,//Sent by Server to Client (Slave) to answer pings
      CONNECT=0x10,//Sent by Client to connect to a Server
      CONNECT_ACK=0x11,//Sent by Server to acknowledge Client Connection
      GAME_READY = 0x20, // Sent by Server when Game is Ready -> Includes the client who starts
      GAME_ACTION = 0x30, // Sent by Client. Includes the move which the client made
      GAME_ACTION_NOTIFY = 0x31, // Sent by Server to Client -> After checking the validity of the Move
      GAME_END = 0x40, // Sent by Server to Clients when a Game has ended (Gracefully)
      GAME_CLIENT_END = 0x41,
      GAME_ERR = 0xF0,// Sent by Server to Clients when any Kind of error Occurs 

      GET_ROOM_LIST = 0x50, // Fragt eine Aktuelle Auflistung aller GameRooms ab
      ROOM_LIST = 0x51, // Gibt die Auflistung der Rooms zurück

      SERVER_SYNC_REQUEST = 0xB0,
      SERVER_SYNC_RESPONSE = 0xB1,

      //Nicht implementiert:
      SERVER_MASTER= 0xA0, // Initializing Master-Slave Data-Connection
      SERVER_SLAVE = 0xA1, // Slave acknowledges the Connection
      SERVER_SYNC = 0xB0, // Master sends updated Game Data to Slave
    };

    class BasePacket
    {
        public virtual PacketTypes Type{
          get{return PacketTypes.BASE;}
        }
        public BasePacket(byte[] data){
          //Noting todo
        }
        public BasePacket(){
          //No Argument
        }

        public virtual byte[] getBytes(){
          return null;
        }
    }
    
    //Sent by Clients to ensure connectivity
    class PingPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.PING;}
      }
      private long _timestamp;
      public long Timestamp{
        get{return _timestamp;}
      }
      public PingPacket(){
        this._timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      }
      public PingPacket(byte[] data){
        this._timestamp = BitConverter.ToInt64(data,2);
      }
      public override byte[] getBytes(){
        byte[] r = new byte[10];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt64(this._timestamp)).CopyTo(r,2);
        return r;
      }
    }

    //Sent by Servers to ensure connectivity
    class PongPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.PONG;}
      }
      private long _timestamp;
      public long Timestamp{
        get{return _timestamp;}
      }
      public PongPacket(){
        this._timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      }
      public PongPacket(byte[] data){
        this._timestamp = BitConverter.ToInt64(data,2);
      }
      public override byte[] getBytes(){
        byte[] r = new byte[10];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt64(this._timestamp)).CopyTo(r,2);
        return r;
      }
    }

    //Sent by Clients on initial Connection
    class ConnectPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.CONNECT;}
      }
      private string name = "";
      private int targetRoom = -1;
      
      public string Name{
        get{return this.name;}
      }
      public int TargetRoom{
        get{return this.targetRoom;}
      }

      public ConnectPacket(byte[] data){
        int sl = BitConverter.ToInt16(data,2);
        this.name = System.Text.Encoding.UTF8.GetString(data,4,sl);
        this.targetRoom = BitConverter.ToInt32(data,4+sl);
      }
      public ConnectPacket(string name,int targetRoom=-1){
        this.name = name;
        this.targetRoom = targetRoom;
      }
      public override byte[] getBytes(){
        int sl = System.Text.Encoding.UTF8.GetByteCount(this.name);
        byte[] r = new byte[8+sl];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(sl)).CopyTo(r,2);
        System.Text.Encoding.UTF8.GetBytes(this.name).CopyTo(r,4);
        BitConverter.GetBytes(Convert.ToInt32(this.targetRoom)).CopyTo(r,4+sl);
        return r;
      }
    }
    
    // Sent by Servers to Acknowledge Connection and send required Data back to Clients
    class ConnectAckPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.CONNECT_ACK;}
      }

      private int color = 0;
      private int uuid = -1;
      private int roomId = -1;
      //More??
      
      public int Color{
        get{return this.color;}
      }
      public int UUID{
        get{return this.UUID;}
      }

      public int RoomId{
        get{return this.roomId;}
      }

      public ConnectAckPacket(byte[] data){
        this.color = BitConverter.ToInt16(data,2);
        this.uuid = BitConverter.ToInt32(data,4);
        this.roomId = BitConverter.ToInt32(data,8);
        
      }
      public ConnectAckPacket(int color, int u,int r){
        this.color = color;
        this.uuid = u;
        this.roomId = r;
      }
      public override byte[] getBytes(){
        byte[] r = new byte[12];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(this.color)).CopyTo(r,2);
        BitConverter.GetBytes(Convert.ToInt32(this.uuid)).CopyTo(r,4);
        BitConverter.GetBytes(Convert.ToInt32(this.roomId)).CopyTo(r,8);
        return r;
      }
    }

    //Sent by Server to Client once the room is full. Contains the Oponents Name and if The player starts the game
    class GameReadyPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GAME_READY;}
      }
      private string oponentName = "UNKNOWN";
      private bool myTurn = false;

      public string OponentName {
        get{return this.oponentName;}
      }
      public bool MyTurn{
        get{return this.myTurn;}
      }

      public GameReadyPacket(string opn,bool mt){
        this.oponentName = opn;
        this.myTurn = mt;
      }

      public GameReadyPacket(byte[] raw){
        int sl = BitConverter.ToInt16(raw,2);
        this.oponentName = System.Text.Encoding.UTF8.GetString(raw,4,sl);
        this.myTurn = BitConverter.ToBoolean(raw,4+sl);
      }

      public override byte[] getBytes(){
        int sl = System.Text.Encoding.UTF8.GetByteCount(this.oponentName);
        byte[] r = new byte[4+sl+1];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(sl)).CopyTo(r,2);
        System.Text.Encoding.UTF8.GetBytes(this.oponentName).CopyTo(r,4);
        BitConverter.GetBytes(this.myTurn).CopyTo(r,4+sl);
        return r;
      }
 
    }

    //Sent by Clients to the server when client clicked a column
    class GameActionPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GAME_ACTION;}
      }
      private int column = 0;
      public int Column{
        get{return this.column;}
      }

      public GameActionPacket(int c){
        this.column = c;
      }

      public GameActionPacket(byte[] raw){
        this.column = BitConverter.ToInt16(raw,2);
      }

      public override byte[] getBytes(){
        byte[] r = new byte[4];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(this.column)).CopyTo(r,2);
        return r;
      }
 
    }

    class GameActionNotifyPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GAME_ACTION_NOTIFY;}
      }
      private int column = 0;
      private bool valid = true;
      public int Column{
        get{return this.column;}
      }
      public bool Valid{
        get{return this.valid;}
      }

      public GameActionNotifyPacket(int c,bool v=true){
        this.column = c;
        this.valid = v;
      }

      public GameActionNotifyPacket(byte[] raw){
        this.column = BitConverter.ToInt16(raw,2);
        this.valid = BitConverter.ToBoolean(raw,4);
      }

      public override byte[] getBytes(){
        byte[] r = new byte[5];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(this.column)).CopyTo(r,2);
        BitConverter.GetBytes(this.valid).CopyTo(r,4);
        return r;
      }
 
    }

    class GameEndPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GAME_END;}
      }
      private bool winner = false;
      private bool connectionLost = false;
      private int[] fields = {0,0,0,0};
      public bool Winner{
        get{return this.winner;}
      }
      public int[] Fields{
        get{return this.fields;}
      }

      public bool ConnectionLost{
        get{return this.connectionLost;}
      }

      public GameEndPacket(bool w,int[] f,bool conL = false){
        this.winner = w;
        f.CopyTo(this.fields,0);
        this.connectionLost = conL;
      }

      public GameEndPacket(byte[] raw){
        this.winner = BitConverter.ToBoolean(raw,2);
        for(int i=0;i<4;i++){
          this.fields[i]=BitConverter.ToInt16(raw,3+i*2);
        }
        this.connectionLost = BitConverter.ToBoolean(raw,3+8);
      }

      public override byte[] getBytes(){
        byte[] r = new byte[12];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToBoolean(this.winner)).CopyTo(r,2);
        for(int i=0;i<4;i++){
          BitConverter.GetBytes(Convert.ToInt16(this.fields[i])).CopyTo(r,3+i*2);
        }
        BitConverter.GetBytes(this.connectionLost).CopyTo(r,3+8);
        return r;
      }
    }

    class GameClientEndPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GAME_CLIENT_END;}
      }
      private string message = "{NO REASON}";

      public string Message{
        get{return message;}
      }

      public GameClientEndPacket(string m){
        this.message = m;
      }

      public GameClientEndPacket(byte[] raw){
        int sl = BitConverter.ToInt16(raw,2);
        this.message = System.Text.Encoding.UTF8.GetString(raw,4,sl);
      }

      public override byte[] getBytes(){
        int sl = System.Text.Encoding.UTF8.GetByteCount(this.message);
        byte[] r = new byte[4+sl];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(sl)).CopyTo(r,2);
        System.Text.Encoding.UTF8.GetBytes(this.message).CopyTo(r,4);
        return r;
      }
    }

    class GameErrorPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GAME_ERR;}
      }
      private int code = -1;
      private string message = "UNKNOWN ERROR";
      
      private bool reconnect = false;
      private bool changeServer = false;


      public int Code{
        get{return this.code;}
      }

      public string Message{
        get{return this.message;}
      }

      public bool Reconnect{
        get{return this.reconnect;}
      }

      public bool ChangeServer{
        get{return this.changeServer;}
      }

      public GameErrorPacket(int c, string m,bool r = false, bool s = false){
        this.code = c;
        this.message=m;
        this.reconnect = r;
        this.changeServer = s;
      }

      public GameErrorPacket(byte[] raw){
        this.code = BitConverter.ToInt16(raw,2);
        int sl =  BitConverter.ToInt16(raw,4);
        this.message = System.Text.Encoding.UTF8.GetString(raw,6,sl);
        this.reconnect = BitConverter.ToBoolean(raw,6+sl);
        this.changeServer = BitConverter.ToBoolean(raw,7+sl);
      }

      public override byte[] getBytes(){
        int sl = System.Text.Encoding.UTF8.GetByteCount(this.message);
        byte[] r = new byte[8+sl];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(this.code)).CopyTo(r,2);
        BitConverter.GetBytes(Convert.ToInt16(sl)).CopyTo(r,4);
        System.Text.Encoding.UTF8.GetBytes(this.message).CopyTo(r,6);
        BitConverter.GetBytes(this.reconnect).CopyTo(r,6+sl);
        BitConverter.GetBytes(this.changeServer).CopyTo(r,7+sl);
        return r;
      }
    }

    class GetRoomListPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.GET_ROOM_LIST;}
      }

      public GetRoomListPacket(){
        //EMPTY
      }

      public GetRoomListPacket(byte[] raw){
        //EMPTY
      }

      public override byte[] getBytes(){
        byte[] r = new byte[2];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        return r;
      }
    }

    struct RoomDescription{
      public string P1Name;
      public string P2Name;
      public bool Full;
      public int Id;
      public RoomDescription(string p1,string p2,int id){
        this.P1Name = p1;
        this.P2Name = p2;
        if(p2==null)this.Full = false;
        else this.Full=true;
        this.Id = id;
      }

      public RoomDescription(byte[] raw){
        //First Byte = Entry Byte Size
        this.Id = BitConverter.ToInt32(raw,2);
        this.Full = BitConverter.ToBoolean(raw,6);
        int sl = BitConverter.ToInt16(raw,7);
        this.P1Name = System.Text.Encoding.UTF8.GetString(raw,9,sl);
        if(!this.Full){
          int sl2 = BitConverter.ToInt16(raw,9+sl);
          this.P2Name = System.Text.Encoding.UTF8.GetString(raw,11+sl,sl2);
        }
        else this.P2Name = null;
      }
      public RoomDescription(byte[] raw, int offset){
        //First Byte = Entry Byte Size
        this.Id = BitConverter.ToInt32(raw,2+offset);
        this.Full = BitConverter.ToBoolean(raw,6+offset);
        int sl = BitConverter.ToInt16(raw,7+offset);
        this.P1Name = System.Text.Encoding.UTF8.GetString(raw,9+offset,sl);
        if(this.Full){
          int sl2 = BitConverter.ToInt16(raw,9+sl+offset);
          this.P2Name = System.Text.Encoding.UTF8.GetString(raw,11+sl+offset,sl2);
        }
        else this.P2Name = null;
      }

      public byte[] getBytes(){
        int s1 = System.Text.Encoding.UTF8.GetByteCount(this.P1Name);
        int s2 = this.Full?System.Text.Encoding.UTF8.GetByteCount(this.P2Name):0;
        int pSize = s1+9+(this.Full?2+s2:0);
        byte[] r = new byte[pSize];
        BitConverter.GetBytes(Convert.ToInt16(pSize)).CopyTo(r,0);//Entry Size (inclusive)
        BitConverter.GetBytes(Convert.ToInt32(this.Id)).CopyTo(r,2);//Room ID
        BitConverter.GetBytes(this.Full).CopyTo(r,6);//Full ?
        BitConverter.GetBytes(Convert.ToInt16(s1)).CopyTo(r,7); //Player 1 String length
        System.Text.Encoding.UTF8.GetBytes(this.P1Name).CopyTo(r,9);//Player 1 String
        if(this.Full){ // Only Append Player 2 Name when not full
          BitConverter.GetBytes(Convert.ToInt16(s2)).CopyTo(r,9+s1);
          System.Text.Encoding.UTF8.GetBytes(this.P2Name).CopyTo(r,11+s1);
        }
        return r;
      }
    }

    class RoomListPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.ROOM_LIST;}
      }

      private RoomDescription[] roomList;

      public RoomDescription[] RoomList{
        get{return this.roomList;}
      }

      public int RoomCount{
        get{return this.roomList.Length;}
      }

      public RoomListPacket(RoomDescription[] rooms){
        this.roomList = (RoomDescription[])rooms.Clone();
      }
      //Dont include on client
      #if GAME_ROOM_EXIST
      public RoomListPacket(GameRoom[] roomObjects){
        this.roomList = new RoomDescription[roomObjects.Length];
        for(int i=0;i<roomObjects.Length;i++){
          this.roomList[i] = new RoomDescription(roomObjects[i].Client1.Name,roomObjects[i].Full?roomObjects[i].Client2.Name:null,roomObjects[i].Roomid);
        }
      }
      # endif

      public RoomListPacket(byte[] raw){
        //0 is Packet type
        int c = BitConverter.ToInt16(raw,2);
        this.roomList = new RoomDescription[c];
        int curOffset = 4;
        for(int i=0;i<c;i++){
          int ps = BitConverter.ToInt16(raw,curOffset);
          roomList[i] = new RoomDescription(raw,curOffset);
          curOffset+=ps;
        }
      }

      public override byte[] getBytes(){
        //Bad / Slow with two loops?? Better ideas?
        System.Collections.Generic.List<byte[]> entries = new System.Collections.Generic.List<byte[]>();
        int total = 4;
        for(int i=0;i<this.roomList.Length;i++){
          byte[] t = roomList[i].getBytes();
          entries.Add(t);
          total+=t.Length;
        }
        byte[] r = new byte[total];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(this.roomList.Length)).CopyTo(r,2);
        int curOffset = 4;
        for(int i=0;i<entries.Count;i++){
          entries[i].CopyTo(r,curOffset);
          curOffset+=entries[i].Length;
        }
        return r;
      }
    }


    /*class ServerSyncRequestPacket : BasePacket{
      public override PacketTypes Type{
        get{return PacketTypes.SERVER_SYNC_REQUEST;}
      }
      private int curRoomId = 0;
      private int curPlayerId = 0;

      public string Message{
        get{return message;}
      }

      public GameClientEndPacket(string m){
        this.message = m;
      }

      public GameClientEndPacket(byte[] raw){
        int sl = BitConverter.ToInt16(raw,2);
        this.message = System.Text.Encoding.UTF8.GetString(raw,4,sl);
      }

      public override byte[] getBytes(){
        int sl = System.Text.Encoding.UTF8.GetByteCount(this.message);
        byte[] r = new byte[4+sl];
        BitConverter.GetBytes(Convert.ToInt16(this.Type)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(sl)).CopyTo(r,2);
        System.Text.Encoding.UTF8.GetBytes(this.message).CopyTo(r,4);
        return r;
      }
    }*/

    //TODO: ServerMasterPacket
    //TODO: ServerSlavePacket
    //TODO: ServerSyncPacket

}

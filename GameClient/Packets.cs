using System;

namespace GameClient
{
    enum PacketTypes{
      PING=0x00,
      PONG=0x01,
      CONNECT=0x10,
      CONNECT_ACK=0x11,
      GAME_READY = 0x20,
      GAME_ACTION = 0x30,
      GAME_ACTION_ACK = 0x31,
      GAME_END = 0x40,
      GAME_ERR = 0xF0
    };
    class BasePacket
    {
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

    class PingPacket : BasePacket{
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
        BitConverter.GetBytes(Convert.ToInt16(PacketTypes.PING)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt64(this._timestamp)).CopyTo(r,2);
        return r;
      }
    }

    class PongPacket : BasePacket{
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
        BitConverter.GetBytes(Convert.ToInt16(PacketTypes.PONG)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt64(this._timestamp)).CopyTo(r,2);
        return r;
      }
    }

    class ConnectPacket : BasePacket{
      private string name = "";
      //More??
      
      public string Name{
        get{return this.name;}
      }

      public ConnectPacket(byte[] data){
        int sl = BitConverter.ToInt16(data,2);
        System.Text.Encoding.UTF8.GetString(data,4,sl);
      }
      public ConnectPacket(string name){
        this.name = name;
      }
      public override byte[] getBytes(){
        int sl = System.Text.Encoding.UTF8.GetByteCount(this.name);
        byte[] r = new byte[4+sl];
        BitConverter.GetBytes(Convert.ToInt16(PacketTypes.CONNECT)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(sl)).CopyTo(r,2);
        System.Text.Encoding.UTF8.GetBytes(this.name,0,sl).CopyTo(r,4);

        return r;
      }
    }
    class ConnectAckPacket : BasePacket{
      private int color = 0;
      //More??
      
      public int Color{
        get{return this.color;}
      }

      public ConnectAckPacket(byte[] data){
        this.color = BitConverter.ToInt16(data,2);
        
      }
      public ConnectAckPacket(int color){
        this.color = color;
      }
      public override byte[] getBytes(){
        byte[] r = new byte[4];
        BitConverter.GetBytes(Convert.ToInt16(PacketTypes.CONNECT_ACK)).CopyTo(r,0);
        BitConverter.GetBytes(Convert.ToInt16(this.color)).CopyTo(r,2);
        return r;
      }
    }
}

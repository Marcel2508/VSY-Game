const net = require("net");
const readline = require("readline");

var gsock = null;
process.stdin.setEncoding('utf8');
const rl = readline.createInterface({
  input: process.stdin,
  output:process.stdout,
  terminal:true
});

/*class Terminal{

  constructor(){
    this.rl = readline.createInterface({
      input:process.stdin,
      output:process.stdout
    });
    this.rl.
  }
}*/




const ptr = {
  "CONNECT":(_)=>{
    rl.question("Username: ",(username)=>{
      rl.question("RoomID: ",(room)=>{
        process.stdout.write("SENT CONNECT\n");
        gsock.write(connectPacket(username,room));
      });
    });
  },
  "GAME_ACTION":(_)=>{
    rl.question("Column: ",(c)=>{
      process.stdout.write("SENT GAME_ACTION\n");
      gsock.write(gameActionPacket(c));
    });
  },
  "GAME_CLIENT_END":(_)=>{
    rl.question("Reason: ",(r)=>{
      process.stdout.write("SENT GAME_CLIENT_END\n");
      gsock.write(gameClientEndPacket(r));
    });
  },
  "GET_ROOM_LIST":(_)=>{
    process.stdout.write("SENT GET_ROOM_LIST\n");
    gsock.write(getRoomListPacket())
  },
  "GAME_ERR":(_)=>{
    rl.question("Code: ",(code)=>{
      rl.question("Message: ",(msg)=>{
        rl.question("Reconnect: ",(recon)=>{
          rl.question("Change Server: ",(chs)=>{
            process.stdout.write("SENT GAME_ERR\n");
            gsock.write(gameErrorPacket(code,msg,(recon=="y"||recon=="true"||recon=="yes"||recon==1),(chs=="y"||chs=="true"||chs=="yes"||chs==1)));
            _();
          });
        });
      });
    });
  }
}

function pt(v){
  switch(v){
    case 0x00:
      return "PING";
    case 0x01:
      return "PONG";
    case 0x10:
      return "CONNECT";
    case 0x11:
      return "CONNECT_ACK";
    case 0x20:
      return "GAME_READY";
    case 0x30:
      return "GAME_ACTION";
    case 0x31:
      return "GAME_ACTION_NOTIFY";
    case 0x40:
      return "GAME_END";
    case 0x41:
      return "GAME_CLIENT_END";
    case 0x50:
      return "GET_ROOM_LIST";
    case 0x51:
      return  "ROOM_LIST";
    case 0xF0:
      return "GAME_ERR";
    default:
      return "{UNKNOWN}"
  }
} 

function displayRoom(buff,offset){
  var id = buff.readInt32LE(offset+2);
  var full = buff.readInt8(offset+6);
  var sl = buff.readInt16LE(offset+7);
  var str = buff.toString("utf8",offset+9,sl);
  if(full){
    var sl2 = buff.readInt16LE(offset+9+sl);
    var str2 = buff.toString("utf8",offset+11+sl,sl2);
  }
  process.stdout.write("ID:",id,"- FULL:",full,"- Name1:",str,"- Name2:",str2);
}

function displayRoomList(buff){
  var c = buff.readInt16LE(4);
  var offset = 6;
  process.stdout.write("ROOMS:");
  for(var i=0;i<c;i++){
    var ps = buff.readInt16LE(offset);
    displayRoom(buff,offset);
    offset+=ps;
  }
}


function pingPacket(){
  var buff = Buffer.allocUnsafe(12);
  buff.writeInt16LE(10,0);
  buff.writeInt16LE(0x00,2);
  buff.writeInt32LE(0x12,4);
  buff.writeInt32LE(0x34,8);
  return buff;
}

function connectPacket(username,lobbyId){
  var sl = Buffer.byteLength(username,"utf8");
  var buff = Buffer.allocUnsafe(10+sl);
  buff.writeInt16LE(8+sl,0);
  buff.writeInt16LE(0x10,2);
  buff.writeInt16LE(sl,4);
  buff.write(username,6,"utf8");
  buff.writeInt32LE(lobbyId,6+sl);
  return buff;
}

function gameActionPacket(column){
  var buff = Buffer.allocUnsafe(6);
  buff.writeInt16LE(4,0);
  buff.writeInt16LE(0x30,2);
  buff.writeInt16LE(column,4);
  return buff;
}

function gameClientEndPacket(reason){
  var sl = Buffer.byteLength(reason,"utf8");
  var buff = Buffer.allocUnsafe(sl+6);
  buff.writeInt16LE(4+sl,0);
  buff.writeInt16LE(0x41,2);
  buff.writeInt16LE(sl,4);
  buff.write(reason,6,"utf8");
  return buff;
}

function getRoomListPacket(){
  var buff = Buffer.allocUnsafe(4);
  buff.writeInt16LE(2,0);
  buff.writeInt16LE(0x50,2);
  return buff;
}


function gameErrorPacket(code,message,reconnect=false,changeServer=false){
  var sl = Buffer.byteLength(message,"utf8");
  var buff = Buffer.allocUnsafe(10+sl);
  buff.writeInt16LE(8+sl,0);
  buff.writeInt16LE(0xF0,2)
  buff.writeInt16LE(code,4);
  buff.writeInt16LE(sl,6);
  buff.write(message,8,message);
  buff.writeInt8(reconnect?0x01:0x00,8+sl);
  buff.writeInt8(changeServer?0x01:0x00,9+sl);
  return buff;
}

gsock = new net.Socket();

gsock.on("data",(d)=>{
  var v = d.readInt16LE(2);
  if(v!==1)
  process.stdout.write("Got "+pt(v)+" Packet of size: "+d.length+"!");
  if(v==0x51)displayRoomList(d);
});


gsock.connect(9000,()=>{
  process.stdout.write("Client Connected!"+"\n");
  var _ = ()=>{
    rl.question("Packet? ",(type)=>{
      if(ptr.hasOwnProperty(type.toUpperCase())){
        ptr[type.toUpperCase()](_);
      }
      else{
        process.stdout.write("INVALID PACKET NAME!"+"\n");
        _();
      }
    }); 
  };_();
  setInterval(()=>{
    gsock.write(pingPacket());
  },2500);
});
const net = require("net");
const readline = require("readline");

var gsock = null;
process.stdin.setEncoding('utf8');
const rl = readline.createInterface({
  input: process.stdin,
  output:process.stdout,
  terminal:false
});

const ptr = {
  "CONNECT_ACK":(_)=>{
    rl.question("Color: ",(color)=>{
      rl.question("UUID: ",(uuid)=>{
        rl.question("ROOMID: ",(room)=>{
          process.stdout.write("SENT CONNECT_ACK\n");
          gsock.write(connectAckPacket(color,uuid,room));
          _();
        });
      });
    });
  },
  "GAME_READY":(_)=>{
    rl.question("Oponent's name: ",(name)=>{
      rl.question("Your Tourn: ",(turn)=>{
        process.stdout.write("SENT GAME_READY\n");
        gsock.write(gameReadyPacket(name,(turn=="y"||turn=="true"||turn=="yes"||turn==1)));
        _();
      });
    });
  },
  "GAME_ACTION_NOTIFY":(_)=>{
    rl.question("Column: ",(column)=>{
      rl.question("Valid: ",(valid)=>{
        process.stdout.write("SENT GAME_ACTION_NOTIFY\n");
        gsock.write(gameActionNotify(parseInt(column),(valid=="y"||valid=="true"||valid=="yes"||valid==1)));
        _();
      });
    });
  },
  "GAME_END":(_)=>{
    rl.question("winner: ",(win)=>{
      rl.question("connection lost: ",(valid)=>{
        rl.question("Fields (x,x,x,x): ",(fields)=>{
          var a = fields.split(",").map(x=>parseInt(x));
          if(a.length!=4||isNaN(a[0])){
            a=[0,1,2,3];
          }
          process.stdout.write("SENT GAME_END\n");
          gsock.write(gameEndPacket((win=="y"||win=="true"||win=="yes"||win==1),a,(valid=="y"||valid=="true"||valid=="yes"||valid==1)));
          _();
        });
      });
    });
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
    case 0xF0:
      return "GAME_ERR";
    default:
      return "{UNKNOWN}"
  }
} 

function pongPacket(){
  var buff = Buffer.allocUnsafe(12);
  buff.writeInt16LE(10,0);
  buff.writeUInt16LE(0x01,2);
  buff.writeInt32LE(0,4);
  buff.writeUInt32LE(0,8);
  return buff;
}

function connectAckPacket(color,uuid,roomId){
  var buff = Buffer.allocUnsafe(14);
  buff.writeInt16LE(12,0);
  buff.writeInt16LE(0x11,2);
  buff.writeInt16LE(color,4);
  buff.writeInt32LE(uuid,6);
  buff.writeInt32LE(roomId,10);
  return buff;
}

function gameReadyPacket(oponentName,myTurn){
  var sl = Buffer.byteLength(oponentName,"utf8");
  var buff = Buffer.allocUnsafe(9+sl);
  buff.writeInt16LE(sl+7,0);
  buff.writeInt16LE(0x20,2);
  buff.writeInt16LE(sl,4);
  buff.write(oponentName,6,"utf8");
  buff.writeInt8(myTurn?0x01:0x00,6+sl);
  return buff;
}

function gameActionNotify(column,valid=true){
  var buff = Buffer.allocUnsafe(7);
  buff.writeInt16LE(5,0);
  buff.writeInt16LE(0x31,2)
  buff.writeInt16LE(column,4);
  buff.writeInt8(valid?0x01:0x00,6);
  return buff;
}

function gameEndPacket(winner,arr,conL = false){
  var buff = Buffer.allocUnsafe(16);
  buff.writeInt16LE(14,0);
  buff.writeInt16LE(0x40,2);
  buff.writeInt8(winner?0x01:0x00,4);
  buff.writeUInt16LE(arr[0],5);
  buff.writeUInt16LE(arr[1],7);
  buff.writeUInt16LE(arr[2],9);
  buff.writeUInt16LE(arr[3],11);
  
  buff.writeInt8(conL?0x01:0x00,13);
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

var srv = net.createServer(function(socket){
  socket.on("data",(data)=>{
    var l = data.readInt16LE(0);
    var t = pt(data.readUInt16LE(2));
    if(t==="PING")socket.write(pongPacket());
    process.stdout.write("Received a "+t+" of Size: "+l+"\n");
  });
  gsock = socket;
});

srv.listen(9000,()=>{
  process.stdout.write("SERVER STARTED!"+"\n");
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
});
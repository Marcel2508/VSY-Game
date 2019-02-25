
//8x8 Array anlegen und mit -1en füllen
var feld = [];
for(var i=0;i<8;i++){
  feld[i]=[[[false,[]]],[false,[]],[false,[]],[false,[]],[false,[]],[false,[]],[false,[]],[false,[]]];
}
var zugnummer = 0;
var zuege = [];
var belegt = [];
for(var i=0;i<64;i++){
  belegt[i] = [0,0,0,0,0,0,0,0,0,0];
}

var xx = 0;

function vor(x,y){
  setTimeout(()=>{vorX(x,y)},0);
}


function btnClick(xValue,yValue){
  vor(xValue,yValue);
}

function moeglich(x,y,m){
  return x>=0 && x<8 && y>=0 && y<8 && feld[x][y][0] == false && feld[x][y][1].indexOf(m)!=-1;
}
function vorX(x,y){

  feld[x][y][0] = true;
  if(moeglich(x+1,y+2,1)){
    feld[x][y][1].push(1);
    vor(x+1,y+2);
  }
  else if(moeglich(x+1,y-2,2)){
    feld[x][y][1].push(2);
    vor(x+1,y-2);
  }
  else if(moeglich(x-1,y+2,3)){
    feld[x][y][1].push(3);
    vor(x-1,y+2);
  }
  else if(moeglich(x-1,y-2,4)){
    feld[x][y][1].push(4);
    vor(x-1,y-2);
  }
  else if(moeglich(x-2,y+1,5)){
    feld[x][y][1].push(5);
    vor(x-2,y+1);
  }
  else if(moeglich(x-2,y-1,6)){
    feld[x][y][1].push(6);
    vor(x-2,y-1);
  }
  else if(moeglich(x+2,y+1,7)){
    feld[x][y][1].push(7);
    vor(x+2,y+1);
  }
  else if(moeglich(x+2,y-1,8)){
    feld[x][y][1].push(8);
    vor(x+2,y-1);
  }
  else{
    //console.log("zurück")
    zurück(x,y)
  }
}

function zurück(x,y){
  belegt[zugnummer] = [0,0,0,0,0,0,0,0,0,0];
  if(zuege[zugnummer]==1){
    feld[x][y][0]=false;
    x-=1;y-=2;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=1);
  }
  else if(zuege[zugnummer]==2){
    feld[x][y][0]=false;
    x-=1;y+=2;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=2);
  }
  else if(zuege[zugnummer]==3){
    feld[x][y]=-1;
    x+=1;y-=2;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=3);
  }
  else if(zuege[zugnummer]==4){
    feld[x][y]=-1;
    x+=1;y+=2;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=4);
  }
  else if(zuege[zugnummer]==5){
    feld[x][y]=-1;
    x+=2;y-=1;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=5);
  }

  else if(zuege[zugnummer]==6){
    feld[x][y]=-1;
    x+=2;y+=1;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=6);
  }
  else if(zuege[zugnummer]==7){
    feld[x][y]=-1;
    x-=2;y-=1;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=7);
  }
  else if(zuege[zugnummer]==7){
    feld[x][y]=-1;
    x-=2;y+=1;
    feld[x][y][1]=feld[x][y][1].filter(k=>k!=8);
  }
  vor(x,y)
}

function print(){
  var s= "";
  for(var i=0;i<8;i++){
    s = "";
    for(var j =0;j<8;j++){
      s+=feld[i][j]+"\t";
    }
    console.log(s);
    s="";
  }
}

btnClick(0,0)
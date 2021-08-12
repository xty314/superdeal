var titlea = new Array();var texta = new Array();var linka = new Array();var trgfrma = new Array();var heightarr = new Array();var cyposarr = new Array();
cyposarr[0]=0;cyposarr[1]=1;
titlea[0] = "New Face";texta[0] = "New Qpos Face has been builded";linka[0] = "c.aspx";trgfrma[0] = "_blank";titlea[1] = "EZNZ QPos 2007";texta[1] = "EZNZ Qpos Demo";linka[1] = "c.aspx";trgfrma[1] = "_blank";
var mc=2;

var inoout=false;

var tmpv;
tmpv=185-8-8-2*parseInt(1);
var cvar=0,say=0,tpos=0,enson=0,hidsay=0,hidson=0;

var psy = new Array();
divtextb ="<div id=d";
divtev1=" onmouseover=\"mdivmo(";
divtev2=")\" onmouseout =\"restime(";
divtev3=")\" onclick=\"butclick(";
divtev4=")\"";
divtexts = " style=\"position:absolute;visibility:hidden;width:"+tmpv+"; COLOR: #6699CC; left:0; top:0; FONT-FAMILY: Arial; FONT-SIZE: 8pt; FONT-STYLE: normal; FONT-WEIGHT: normal; TEXT-DECORATION: none; margin:0px; overflow-x:hidden; LINE-HEIGHT: 12pt; text-align:left;padding:0px; cursor:'default';\">";
ie6span= " style=\"position:relative; COLOR: #FF9900; width:"+tmpv+"; FONT-FAMILY: verdana,arial,helvetica; FONT-SIZE: 9pt; FONT-STYLE: normal; FONT-WEIGHT: bold; TEXT-DECORATION: none; LINE-HEIGHT: 14pt; text-align:left;padding:0px;\"";

uzun="<div id=\"enuzun\" style=\"position:absolute;left:0;top:0;\">";
var uzunobj=null;
var uzuntop=0;
var toplay=0;



function mdivmo(gnum)
{
	inoout=true;

	if((linka[gnum].length)>2)
	{
	objd=eval("d"+gnum);
	objd2=eval("hgd"+gnum);

	objd.style.color="#999999";
	objd2.style.color="#999999";

	objd.style.cursor='hand';
	objd2.style.cursor='hand';

	objd.style.textDecoration='none';objd2.style.textDecoration='none';

}
	window.status=""+linka[gnum];

}

function restime(gnum2)
{
	inoout=false;
	objd=eval("d"+gnum2);
	objd2=eval("hgd"+gnum2);

	objd.style.color="#000000";
	objd2.style.color="#414A76";

	objd.style.textDecoration='none';objd2.style.textDecoration='none';

	window.status="";

}

function butclick(gnum3)
{
if(linka[gnum3].substring(0,11)=="javascript:"){eval(""+linka[gnum3]);}else{if((linka[gnum3].length)>3){
if((trgfrma[gnum3].indexOf("_parent")>-1)){eval("parent.window.location='"+linka[gnum3]+"'");}else if((trgfrma[gnum3].indexOf("_top")>-1)){eval("top.window.location='"+linka[gnum3]+"'");}else{window.open(''+linka[gnum3],''+trgfrma[gnum3]);}}}


}

function dotrans()
{
	if(inoout==false){
	uzuntop--;
	if(uzuntop<(-1*toplay))
	{
		uzuntop=215;
	}

	enuzun.style.pixelTop=uzuntop;
}
	if(psy[(uzuntop*(-1))+4]==3)
	{
setTimeout('dotrans()',3000+35);
}
else{setTimeout('dotrans()',35);}

}

function initte2()
{
	for(i=0;i<mc;i++)
	{
		objd=eval("d"+i);
		if(parseInt(objd.offsetHeight)<=0){setTimeout('initte2()',1000);return;}
	}
	i=0;
	for(i=0;i<mc;i++)
	{
		objd=eval("d"+i);
		heightarr[i]=parseInt(objd.offsetHeight);
	}

	toplay=4;
	for(i=0;i<mc;i++)
	{
		objd=eval("d"+i);
		objd.style.visibility="visible";
		objd.style.pixelTop=toplay;
		psy[toplay]=3;
		toplay=toplay+heightarr[i]+10;

	}


	enuzun.style.left=8+"px";
	enuzun.style.height=toplay+"px";
	enuzun.style.width=tmpv+"px";
	uzuntop=215;



	dotrans();

}

function initte()
{
	i=0;
	innertxt=""+uzun;
	for(i=0;i<mc;i++)
	{
		innertxt=innertxt+""+divtextb+""+i+""+divtev1+i+divtev2+i+divtev3+i+divtev4+divtexts+"<span id=\"hgd"+i+"\""+ie6span+">"+titlea[i]+"</span><br>"+texta[i]+"</div>";
	}
	innertxt=innertxt+"</div>";

	spageie.innerHTML=""+innertxt;
	setTimeout('initte2()',500);

}




window.onload=initte;
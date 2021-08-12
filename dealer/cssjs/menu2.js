var activeHeader = null;
var activeMenu = null;
	
function setMenu(menuHeaderID,menuID)
{
var agt=navigator.userAgent.toLowerCase(); 

var version= parseInt(navigator.appVersion); 

var opera = (agt.indexOf("opera") != -1);
var opera7 = (opera && (vers >= 7));
var moz = (agt.indexOf("gecko") != -1);
var ie = ((agt.indexOf("msie") != -1) && !moz && !opera);
var ie4 = (ie && (version >= 4));
var nn = ((agt.indexOf("mozilla") != -1) && !ie && !moz && !opera);
var nav4 = (nn && (version >= 4));

// *** PLATFORM ***
var is_win  = (agt.indexOf("win")!=-1);
var is_mac  = (agt.indexOf("mac")!=-1);
var is_unix = (agt.indexOf("x11")!=-1);

	var top = 0;
	var left = 0;
	var currentEle;
	
	if (nn || nav4 || moz) {
		activeMenu = document.getElementById(menuID);
		activeHeader =  document.getElementById(menuTitleID);
	}
	else {
		activeMenu = document.all[menuID];
		activeHeader = document.all[menuTitleID];
	}
	window.alert(activeMenu);
	currentEle = activeHeader;

	while(currentEle.tagName.toLowerCase() != 'body' )
	{
		top += currentEle.offsetTop;
		left += currentEle.offsetLeft;
		currentEle = currentEle.offsetParent;
	}
	top += (activeHeader.offsetHeight);
	activeMenu.style.left = left;
	activeMenu.style.top = top;
	hideSelect();
	if(document.all)
		activeMenu.style.visibility = 'visible';
			
	event.cancelBubble = true;
	

}
function hideMenu()
{
	//if(document.all)
	{
		if(activeHeader != null && activeMenu != null)
		{
			if(!activeMenu.contains(event.toElement)) 
			{
				activeMenu.style.visibility = 'hidden';
				activeHeader = null;
				activeMenu = null;
				showSelect();
			}
		}
	}
}
function showSelect()
{
	var obj;	

	for(var i = 0; i < document.getElementsByTagName("select").length; i++)
	{
		obj = document.getElementsByTagName("select")[i];

		if(!obj || !obj.offsetParent)
			continue;
		obj.style.visibility = 'visible';
	}
}
function hideSelect()
{
	var obj;
	var currentEle;
	var top = 0;
	var left = 0;
	var menuHeight;
	var timeout;
	for(var i = 0; i < document.getElementsByTagName("select").length; i++)
	{
		obj = document.getElementsByTagName("select")[i];
		currentEle = obj;
		while(currentEle.tagName.toLowerCase() != 'body')
		{
			top += currentEle.offsetTop;
			left += currentEle.offsetLeft;
			currentEle = currentEle.offsetParent;
		}
		if(activeMenu != null)
		{
			menuHeight = (activeMenu.offsetTop + activeMenu.offsetHeight);
			
			if(top < menuHeight)
			{			
				if((left < (activeMenu.offsetLeft + activeMenu.offsetWidth)) && (left + obj.offsetWidth > activeMenu.offsetLeft)) 
					obj.style.visibility = 'hidden';
			}
		}
		top = 0;
		left = 0;
	}
}

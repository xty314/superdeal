var activeHeader = null;
var activeMenu = null;
	
var brwAgent=navigator.userAgent.toLowerCase(); 

var brwOpera = (brwAgent.indexOf("opera") != -1);
var brwOpera7 = (brwOpera && (vers >= 7));
var brwMoz = (brwAgent.indexOf("gecko") != -1);
var brwIE = ((brwAgent.indexOf("msie") != -1) && !brwOpera && !brwMoz);
var brwIE4 = (brwIE && (version >= 4));
var brwNN = ((brwAgent.indexOf("mozilla") != -1) && !brwOpera && !brwMoz && brwIE);
var brwNav4 = (brwNN && (version >= 4));


function setMenu(menuHeaderID,menuID)
{
	var top = 0;
	var left = 0;
	var currentEle;

	
	if(activeHeader != null && activeMenu != null)
	{
		if(activeMenu.style.visibility != 'hidden')
		{
			activeMenu.style.visibility = 'hidden';
			
			showSelect();
		}
	}


	if (brwNN || brwNav4 || brwMoz) 
	{
		activeMenu = document.getElementById(menuID);
		activeHeader =  document.getElementById(menuHeaderID);
	}
	else 
	{
		activeMenu = document.all(menuID);
		activeHeader = document.all(menuHeaderID);
	}

	currentEle = activeHeader;

	while(currentEle.tagName.toLowerCase() != 'body')
	{
		top += currentEle.offsetTop;
		left += currentEle.offsetLeft;
		currentEle = currentEle.offsetParent;
	}

	top += (activeHeader.offsetHeight);
	activeMenu.style.left = left;
	activeMenu.style.top = top;
	hideSelect();

	if (activeMenu != null && activeMenu.style.visibility == "hidden")
		activeMenu.style.visibility = 'visible';
	
	if (window.event && window.event.cancelBubble != null) 	{
		event.cancelBubble = true;
	}
	else{
		event.stopPropagation();
	}


}

function hideMenu()
{
		var bHide = false;	
		if(activeHeader != null && activeMenu != null)	
		{
			if(brwNN || brwNav4 || brwMoz)
			{
					
			}
			else {
				if(!activeMenu.contains(event.toElement)) 
					bHide = true;
			}

			if(bHide)
			{
				activeMenu.style.visibility = 'hidden';
				activeHeader = null;
				activeMenu = null;
				showSelect();
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


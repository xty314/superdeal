var activeHeader = null;
var activeMenu = null;
	
var brwAgent=navigator.userAgent.toLowerCase(); 

var brwOpera = (brwAgent.indexOf("opera") != -1);
var brwOpera7 = (brwOpera && (vers >= 7));
var brwMoz = (brwAgent.indexOf("gecko") != -1);
var brwIE = ((brwAgent.indexOf("msie") != -1) && !brwOpera && !brwMoz);
var brwIE4 = false;
var brwNN = ((brwAgent.indexOf("mozilla") != -1) && !brwOpera && !brwMoz && brwIE);
var brwNav4 = false;


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
	activeMenu.style.left = left + 'px';
	activeMenu.style.top = top + 'px';
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
				if(!activeMenu.contains(event.toElement)) 
					bHide = true;

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
			top += currentEle.offsetTop + 'px';
			left += currentEle.offsetLeft + 'px';
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
$(document).ready(function() {
    $("[name='selected_invoice']").click(function (event) {
		ChangeUrl();
	});
	$("[name='selected_all_invoice']").click(function (event) {
		if ($(this).attr('checked') == true) {
			$("[name='selected_invoice']").attr('checked',true);
		} else {
			$("[name='selected_invoice']").attr('checked', false);
		}
		ChangeUrl();
	});
});
function ChangeUrl(){
	var si = "";
	$("[name='selected_invoice']").each(function(index) {
		if ($(this).attr('checked') == true) {
			var i = parseInt(index);
			var v = $(this).val();
			//console.log(i);
			//console.log(v);
			if(i == 0)
				si += v;
			else
				si += "," + v;
		} 
	});
	var ci = window.location.search;
	var t = "statement.aspx" + ci + "&selected_invoice=" + encodeURI(si) +"&t=vd";
	//console.log(t);
	$('#printable-copy-s').empty();
	$('#printable-copy-s').append("<input id=\"printable-copy\" type=\"button\" class=\"b\" value=\" Printable Copy \" onclick=\"window.open('" + t +"')\"/>"); 
	//console.log("si=",si);
}


var activeTitle = null;
var activeMenu = null;

function setMenu(menuTitleID, menuID) {
	if (activeTitle != null && activeMenu != null) {
		prevActiveTitle = activeTitle;
		prevActiveMenu  = activeMenu;
	}
	if (nn || nav4 || moz) {
		activeMenu = document.getElementById(menuID);
		activeTitle =  document.getElementById(menuTitleID);
	}
	else {
		activeMenu = document.all[menuID];
		activeTitle = document.all[menuTitleID];
	}
	if (activeMenu != null && activeMenu.style.visibility == "hidden") {
		var cellPos  = getPosition(activeTitle);
		var cellSize = getSize(activeTitle);
		var menuSize = getSize(activeMenu);
		var menuPos = getHorizMenuInitPos(cellPos, cellSize, menuSize);
		activeMenu.style.left = menuPos.x;
		activeMenu.style.top  = menuPos.y;
		activeMenu.style.visibility = "visible";
		activeTitle.style.fontWeight = "normal";
	}
}

function setVertMenu(menuTitleID, menuID) {
	if (activeTitle != null && activeMenu != null) {
		prevActiveTitle = activeTitle;
		prevActiveMenu  = activeMenu;
	}
	if (nn || nav4 || moz) {
		activeMenu = document.getElementById(menuID);
		activeTitle =  document.getElementById(menuTitleID);
	}
	else {
		activeMenu = document.all[menuID];
		activeTitle = document.all[menuTitleID];
	}
	if (activeMenu != null && activeMenu.style.visibility == "hidden") {
		var cellPos  = getPosition(activeTitle);
		var cellSize = getSize(activeTitle);
		var menuSize = getSize(activeMenu);
		var menuPos = getVertMenuInitPos(cellPos, cellSize, menuSize);
		activeMenu.style.left = menuPos.x;
		activeMenu.style.top  = menuPos.y;
		activeMenu.style.visibility = "visible";
		activeTitle.style.fontWeight = "normal";
	}
}

function hideMenu(e) {
	if (activeMenu != null && activeTitle != null) {

		var toRemove = false;
		if (nn || nav4 || moz) {
			if (!containsEvent(activeTitle, e) && !containsEvent(activeMenu, e)) 
				toRemove = true;
		}
		else {
			if(!activeMenu.contains(event.toElement)) 
				toRemove = true;
		}
		if (toRemove) {
			activeMenu.style.visibility = 'hidden';
			activeTitle.style.fontWeight = "bold";
			activeHeader = null;
			activeMenu = null;
		}
	}
}

function getVertMenuInitPos(cellPos, cellSize, menuSize) {
	var initPos = {x: 0, y: 0};
	var menuHalfWidth = menuSize.width / 2;
	var cellHalfWidth = cellSize.width / 2;

	initPos.x = cellPos.x - (menuHalfWidth - cellHalfWidth);
	initPos.y = cellPos.y + cellSize.height - 1;

	return initPos;
}

function getHorizMenuInitPos(cellPos, cellSize, menuSize) {
	var viewportBounds = {top: 0, bottom: 0};
	var centre = {x: 0, y: 0};
	centre.x = cellPos.x + cellSize.width;
	centre.y = cellPos.y + cellSize.height / 2;

	if (nn || nav4 || moz) {
		viewportBounds.top    = self.scrollY;
		viewportBounds.bottom = self.scrollY + self.innerHeight;
	}
	else {
		viewportBounds.top    = self.document.body.scrollTop;
		viewportBounds.bottom = self.document.body.scrollTop + self.document.body.offsetHeight - 20;
	}

	var initPos = {x: 0, y: 0};
	initPos.x = centre.x - 50;

	var top    = centre.y - menuSize.height/2;
	var bottom = centre.y + menuSize.height/2;
	if (top < viewportBounds.top) 
		initPos.y = viewportBounds.top + 10;
	else if (bottom > viewportBounds.bottom)
		initPos.y = viewportBounds.bottom - 10 - menuSize.height;
	else
		initPos.y = top;

	return initPos;
}

function getPosition(element) {
	var coords = {x: 0, y: 0};
	while (element) {
		coords.x += element.offsetLeft;
		coords.y += element.offsetTop;
		element = element.offsetParent;
	}
	return coords;
}

function getSize(element) {
	var size = {width: 0, height: 0};
	size.width  = element.offsetWidth;
	size.height = element.offsetHeight;

	return size;
}

function containsEvent(element, event) {
	var pos  = getPosition(element);
	var size = getSize(element);
	var e = {x: 0, y: 0};

	if (nn || nav4 || moz) {
		e.x = event.pageX;
		e.y = event.pageY;
	}
	else {
		e.x = event.x;
		e.y = event.y;
	}

	if (e.x > pos.x && e.x < pos.x+size.width) {
		if (e.y > pos.y && e.y < pos.y+size.height) {
			return true;
		}
	}
	return false;
}
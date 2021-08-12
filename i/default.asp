<% @ LANGUAGE = JSCRIPT %>

<%
//Turn on buffering.  This statement must appear before the <HTML> tag.
Response.buffer = true ; 
%>
<html>

<head>
<title>
List Directory
</title>
</head>


<body>

<% RUNAT=Server
function ShowFolderFileList(folderspec)
{
  var fso, f, f1, fc, s;
  fso = new ActiveXObject("Scripting.FileSystemObject");
  f = fso.GetFolder(folderspec);
  fc = new Enumerator(f.files);
  s = "";
  for (; !fc.atEnd(); fc.moveNext())
  {
	s += "<a href=\"";
	s += fc.item().name;
	s += "\">";
	s += fc.item().name;
	s += "</a>.........";
	s += fc.item().size;
	s += " bytes<br>";
  }
  return(s);
}
%>

<% RUNAT=Server
function ShowImageFileList(folderspec)
{
  var fso, f, f1, fc, s, n;
  var rejpg = new RegExp(".jpg");
  var regif = new RegExp(".gif");

  fso = new ActiveXObject("Scripting.FileSystemObject");
  f = fso.GetFolder(folderspec);
  fc = new Enumerator(f.files);
  s = "<br><br><h2><font color=\"#008000\"><em> Images in this folder.</em></font></h2>";
  s += "<p>";
  for (; !fc.atEnd(); fc.moveNext())
  {
      n = fc.item().name;
	  if(rejpg.test(n) || regif.test(n))
	  {
		s += "<a href=\"";
		s += n;
		s += "\"><img src=\"";
		s += n;
		s += "\"";
		if(fc.item().size > 100000)
			s += " width=\"50%\" height=\"50%\" ";
		s += " align=\"center\"> ";	
		s += "</a>&nbsp;&nbsp;";
	  }
  }
  s += "</p>";
  return(s);
}
%>

<% RUNAT=Server
function MyCurrentTime() 
{ 
  	var x  
  	x = new Date();
	if(Response.IsClientConnected())
	{	
		Response.Write(x.toString()); // translated to a string.  
	}
}
%> 

<%

Response.Write("<br><h1><font color=\"#008000\"><em>Greetings! The clock is still ticking.</em></font></h1>");
Response.Write(ShowFolderFileList(Server.MapPath(".")));
Response.Write(ShowImageFileList(Server.MapPath(".")));
Response.Flush(); 

%>

</body>
</html>

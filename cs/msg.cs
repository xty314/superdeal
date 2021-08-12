<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
	PrintAdminHeader();
	PrintAdminMenu();
	if(Request.Form["msg"] != null && Request.Form["msg"] != "")
	{
		string msg = EncodeForJava(Request.Form["msg"]);
		msg += "\\r\\n\\r\\n --- " + Session["name"] + " sent from " + Session["rip"].ToString();
		msg += " " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + " --- \\r\\n";
		Application[Request.Form["sid"] + "_msg"] = msg;
		Response.Write("<br><br><center><h3>Message Sent</h3>");
//DEBUG("sid=", Request.Form["sid"]);
/*		Response.Write("<script Language=javascript");
		Response.Write(">\r\n");
		Response.Write("window.alert('" + Application[Request.Form["sid"] + "_msg"] + "')\r\n");
		Response.Write("</script");
		Response.Write(">\r\n ");
*/	
	}
	else
		PrintBody();
	PrintAdminFooter();
}

void PrintBody()
{
	Response.Write("<form action=msg.aspx method=post>");
	Response.Write("<input type=hidden name=sid value=" + Request.QueryString["sid"] + ">");
	Response.Write("<br><br><center><table width=400><tr><td>");
	Response.Write("<textarea name=msg rows=7 cols=70></textarea>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td align=right><input type=submit name=cmd value=' Send '></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

string EncodeForJava(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\r')
			ss += "\\r"; //double it for SQL query
		else if(s[i] == '\n')
			ss += "\\n";
		else
			ss += s[i];
	}
	return ss;
}
</script>

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<br><br><center><h3>EZNZ Customer Service</h3>");

	Response.Write("<form action=http://www.eznz.com/ticket.aspx method=post traget=_new>");
	Response.Write("<input type=hidden name=customer value='" + m_sCompanyName + "'>");
//	Response.Write("<input type=hidden name=password value=473987C3D573D88BBF94D16BD4149180>");
	Response.Write("<input type=hidden name=name value='" + Session["name"] + "'>");
	Response.Write("<input type=hidden name=email value='" + Session["email"] + "'>");
	Response.Write("<input type=hidden name=access_level value=" + Session[m_sCompanyName + "access_level"] + ">");

	Response.Write("<input type=submit name=cmd value='View Tickets' " + Session["button_style"] + ">");
	Response.Write("<br><br>");
	Response.Write("<input type=submit name=cmd value='Bug Report' " + Session["button_style"] + ">");
	Response.Write("<br><br>");
	Response.Write("<input type=submit name=cmd value='New Feature Request' " + Session["button_style"] + ">");
	Response.Write("<br><br>");

	Response.Write("</form>");
	PrintAdminFooter();
}
</script>

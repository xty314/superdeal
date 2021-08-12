<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd

void Page_Load(Object Src, EventArgs E ) 
{
    TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
    
	m_type = g("t");
	m_action = g("a");
	m_cmd = p("cmd");
	
	switch(m_cmd)
	{
	case "Submit":
		break;
	case "refresh":
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&a=" + m_action + "\">");
		break;
	return; //if it's a form post then do nothing else, quit here
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	PrintMainForm();
}

bool PrintMainForm()
{
	Response.Write("<br><center><h4>test</h4>");
	Response.Write("<form name=f action=? method=post>");
	Response.Write("<table width=90% cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr class=th>");
	Response.Write("<th></th>");
	Response.Write("<th></th>");
	Response.Write("</tr>");
	
	
	Response.Write("</table></form>");
	return true;
}

</script>

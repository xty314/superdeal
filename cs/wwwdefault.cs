<script runat=server>


void Page_Load(Object Src, EventArgs E ) 
{
//	if(TS_UserLoggedIn())
	{
		Response.Redirect("c.aspx");
		return;
	}

	m_bCheckLogin = false;
	TS_PageLoad(); //do common things, LogVisit etc...
//	if(!SecurityCheck("editor"))
//		return;

//	PrintHeaderAndMenu();
	Response.Write(ReadSitePage("www_default"));
//	PrintFooter();	
}

void GetQueryStrings()
{
}

</script>
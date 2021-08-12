<!-- #include file="config.cs" -->
<!-- #include file="..\cs\stat_detail.cs" -->

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["t"] != "vd")
	{
		
		 PrintAdminHeader();
		 if(Request.QueryString["p"] != "1")
		 PrintAdminMenu();

	}

	SPage_Load();

	if(Request.QueryString["t"] != "vd")
	{
		if(Request.QueryString["p"] != "1")
		LFooter.Text = m_sAdminFooter;
	}
}
</script>
<!-- #include file="config.cs" -->
<!-- #include file="..\cs\statement.cs" -->

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["t"] != "vd")
	{
		PrintAdminHeader();
		PrintAdminMenu();
	}

	SPage_Load();

	if(Request.QueryString["t"] != "vd")
		LFooter.Text = m_sAdminFooter;
}
</script>
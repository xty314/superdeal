<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\newship.cs" -->

<script runat=server>
void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	PrintAdminHeader();
	PrintAdminMenu();
	if(!SecurityCheck("manager"))
	{
		Response.Write("<br><b>Access Denied!</b>");
		return;
	}
	NPage_Load();
	PrintAdminFooter();
}

</script>
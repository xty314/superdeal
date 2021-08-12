<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\quotation.cs" -->

<script runat=server>
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	PrintAdminHeader();
	PrintAdminMenu();
	PrintHeaderAndMenu();

	if(!PrintPage())
		return;

	PrintAdminFooter();
}

</script>
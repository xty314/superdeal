<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\search.cs" -->

<script runat=server>
protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	bAdmin = true;
	bSales = false;

	PrintAdminHeader();
	PrintAdminMenu();
	PrintHeaderAndMenu();
	GetKeyWord();
	Search_Page_Load();
	PrintSearchForm();
	PrintAdminFooter();
}
</script>
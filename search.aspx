<!-- #include file="config.cs" -->
<!-- #include file="cs\search.cs" -->
<!-- #include file="cs\adminmenu.cs" -->

<script runat=server>
protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	bAdmin = false;
	bSales = false;

	GetKeyWord();
	PrintHeaderAndMenu();
	Search_Page_Load();
	PrintFooter();
}
</script>
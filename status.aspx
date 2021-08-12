<!-- #include file="config.cs" -->
<!-- #include file="cs\status.cs" -->

<script runat="server">

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
	RememberLastPage();

	PrintHeaderAndMenu();

	PrintBodyHeader();

	Status_Page_Load();

	PrintBodyFooter();

	PrintFooter();
}

</script>
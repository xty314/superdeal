<!-- #include file="config.cs" -->
<!-- #include file="..\cs\rmastatus.cs" -->

<script runat="server">
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	PrintHeaderAndMenu();
	SPage_Load();
	PrintFooter();
}
</script>
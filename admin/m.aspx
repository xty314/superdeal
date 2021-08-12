<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\m.cs" -->

<script runat=server>
void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	PrintAdminHeader();
	PrintAdminMenu();
	MPage_Load();
	PrintAdminFooter();
}

</script>
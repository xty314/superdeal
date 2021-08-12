<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\more.cs" -->

<script runat=server>
void Page_Load(Object Src, EventArgs E)
{
	PrintAdminHeader();
	PrintAdminMenu();
	MPage_Load();
	PrintAdminFooter();
}

</script>
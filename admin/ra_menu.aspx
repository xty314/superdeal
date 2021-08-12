<!-- #include file="config.cs" -->
<!-- #include file="..\cs\ra_menu.cs" -->

<script runat=server>

void InitializeData()
{
	PrintAdminHeader();
	PrintAdminMenu();
	LFooter.Text = m_sAdminFooter;
	
}
</script>
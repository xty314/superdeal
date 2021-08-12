<!-- #include file="config.cs" -->
<!-- #include file="..\cs\pk.cs" -->
<!-- #include file="..\cs\menu.cs" -->

<script runat=server>
void InitializeData()
{
	if(!SecurityCheck("editor"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	m_bAdminMenu = true;
	m_sAdminFooter1 = m_sAdminFooter;
}
</script>

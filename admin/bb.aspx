<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\bb.cs" -->

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

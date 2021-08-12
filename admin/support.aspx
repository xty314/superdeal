<!-- #include file="config.cs" -->
<!-- #include file="..\cs\support.cs" -->

<script runat=server>

void initializeData()
{

	PrintAdminHeader();
	PrintAdminMenu();
	LFooter.Text = m_sAdminFooter;

}
</script>
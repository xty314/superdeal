<!-- #include file="config.cs" -->
<!-- #include file="..\cs\repair.cs" -->

<script runat=server>
void InitializeData()
{
//	PrintAdminHeader();
//	PrintAdminMenu();
//	LFooter.Text = m_sAdminFooter;
	PrintHeaderAndMenu();
	//LFooter.Text = m_sFooter;
//	PrintFooter();
}
void InitialFooter()
{
//	PrintFooter();
	LFooter.Text = m_sFooter;
}
</script>
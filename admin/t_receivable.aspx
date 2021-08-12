<!-- #include file="config.cs" -->
<!-- #include file="..\cs\t_receivable.cs" -->
<script runat=server>
void InitializeData()
{
	PrintAdminHeader();
	if(!g_bPDA)
	{
		PrintAdminMenu();
		LFooter.Text = m_sAdminFooter;
	}
}
</script>
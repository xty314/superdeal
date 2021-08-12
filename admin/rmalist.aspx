<!-- #include file="config.cs" -->
<!-- #include file="..\cs\rmalist.cs" -->

<script runat=server>
void InitializeData()
{
	PrintAdminHeader();
	PrintAdminMenu();
	LFooter.Text = m_sAdminFooter;
}
</script>
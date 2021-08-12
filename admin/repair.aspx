<!-- #include file="config.cs" -->
<!-- #include file="..\cs\repair.cs" -->

<script runat=server>
void InitializeData()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
}
void InitialFooter()
{
	LFooter.Text = m_sAdminFooter;
}

</script>
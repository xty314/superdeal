<!-- #include file="config.cs" -->
<!-- #include file="..\cs\inv_note.cs" -->

<script runat=server>
void InitializeData()
{
	PrintAdminHeader();
	PrintAdminMenu();
	LFooter.Text = m_sAdminFooter;
}
</script>
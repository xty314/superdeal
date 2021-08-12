<!-- #include file="config.cs" -->
<!-- #include file="..\cs\viewdata.cs" -->

<script runat=server>
bool InitializeData()
{
	if(tableName == "web_log")
	{
		m_sql = "SELECT ip, name, url, query, visit FROM web_log ORDER BY visit DESC";
	}
	return true;
}
</script>

<!-- #include file="config.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\c.cs" -->

<script runat=server>

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("normal"))
		return;

	//refresh cache whatever
	TSRemoveCache(m_sCompanyName + m_sSite + "_" + m_sHeaderCacheName);

	PrintAdminHeader();
	PrintAdminMenu();
	if(!CatalogInitPage()) //if return true, then cache wrote
		CatalogDrawList(true); //true to draw administration menu
//	else
//		DEBUG("cache wrote", "");
	PrintAdminFooter();
}
</script>

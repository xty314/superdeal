<!-- #include file="config.cs" -->
<!-- #include file="..\invoice.cs" -->
<!-- #include file="..\cs\menu.cs" -->
<!-- #include file="..\cs\q.cs" -->

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	m_bSales = true;
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(Request.QueryString["search"] == "1" && Request.QueryString["ci"] == null)//	if(Request.Form["cmd"] == "Search")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		DoCustomerSearchAndList();
		return;
	}

	if(!QPage_Load())
		return;
	PrintAdminHeader();
	PrintAdminMenu();
	PrintHeaderAndMenu();
	PrintQForm();
//	PrintFooter();
	PrintSearchForm();
	PrintAdminFooter();
}

void PrintPageHeader()
{
	PrintAdminHeader();
	PrintAdminMenu();
}
</script>
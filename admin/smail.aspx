<!-- #include file="config.cs" -->
<!-- #include file="..\cs\smail.cs" -->

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("accountant"))
		return;

	PrintAdminHeader();	
	SmailPage_Load();

}
</script>
<!-- #include file="config.cs" -->
<!-- #include file="..\cs\broadmail.cs" -->

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("editor"))
		return;

	PrintAdminHeader();	
	PrintAdminMenu();
	mPage_Load();
}
</script>
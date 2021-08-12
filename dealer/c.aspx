<!-- #include file="config.cs" -->
<!-- #include file="..\cs\c.cs" -->
<link rel="stylesheet" href=".\cssjs\alex.css">
<script src=".\cssjs\alex.js"></script>
<script runat=server>

protected void Page_Load(Object Src, EventArgs E ) 
{
//	TS_PageLoad(); //do common things, LogVisit etc...
	TS_Init();
	if(CatalogInitPage())
		return;	//cache wrote, out
	CatalogDrawList(false); //false means don't draw administration menu
	PrintFooter();
}
</script>

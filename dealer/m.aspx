<!-- #include file="config.cs" -->
<!-- #include file="..\cs\m.cs" -->

<script runat=server>
void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	MPage_Load();
	PrintFooter();
}
</script>
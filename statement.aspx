<!-- #include file="config.cs" -->
<!-- #include file="cs\statement.cs" -->

<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;

	if(Request.QueryString["t"] != "vd")
		PrintHeaderAndMenu();

//	if(Request.QueryString["ci"] != Session["card_id"].ToString())
//		Response.Write("<h3>Access Denied</h3>");
//	else
		SPage_Load();

	if(Request.QueryString["t"] != "vd")
		LFooter.Text = m_sFooter;
}
</script>
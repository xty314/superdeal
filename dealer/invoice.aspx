<!-- #include file="config.cs" -->
<!-- #include file="..\invoice.cs" -->

<script runat=server>
bool m_bOrder = false;

protected void Page_Load(Object Src, EventArgs E ) 
{
	m_inv_sNumber = Request.QueryString[0];
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!TS_UserLoggedIn() || Session["email"] == null)
	{
		Response.Write("<h3>Error, please <a href=login.aspx>login</a> first</h3>");
		return;
	}
	string s = "";
	if(Request.QueryString["t"] == "order" && Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		s = BuildOrderMail(Request.QueryString["id"]);
		Response.Write(s);
		return;
	}
	else
	{
		s = BuildInvoice(Request.QueryString[0]);
		//continue followed code;
	}
	Response.Write(s);
}
</script>

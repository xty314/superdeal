<!-- #include file="config.cs" -->
<!-- #include file="cs\kit_fun.cs" -->

<script runat=server>

protected void Page_Load(Object Src, EventArgs E ) 
{
    if(!CartOnPageLoad())
		return;

	InitKit();
	
	PrintHeaderAndMenu();
	RememberLastPage();
	PrintBodyHeader();

	Response.Write("<table border=0 cellpadding=7 cellspacing=0 width=100% align=center>");
	Response.Write("<tr><center><h2 style='margin-top:20px;margin-bottom:10px;'>Shopping Cart</h2></center>");
	Response.Write("<table width=100% bgcolor=#CCCCCC cellspacing=0 cellpadding=0 border=0>");
	Response.Write("<form action=cart.aspx?r=" + DateTime.Now.ToOADate() + " method=get>");
	Response.Write("<input type=hidden size=2 maxlength=3 name=t value=update><tr><td style='border:none;'>");

	Response.Write(PrintCart(true, false)); //true to printer buttons

	Response.Write("</tr></form></table></td></tr></table>");
	PrintBodyFooter();

	PrintFooter();
}

</script>

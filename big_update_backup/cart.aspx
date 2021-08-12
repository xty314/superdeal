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

	Response.Write("<table border=0 cellpadding=7 cellspacing=0 width=100% align=center>");
	Response.Write("<tr><td><font color=#CCCCCC size=+1><b>Your Shopping Cart</b></font>");
	Response.Write("<table width=100% bgcolor=#CCCCCC cellspacing=0 cellpadding=0 border=0>");
	Response.Write("<form action=cart.aspx?r=" + DateTime.Now.ToOADate() + " method=get>");
	Response.Write("<input type=hidden size=2 maxlength=3 name=t value=update><tr><td>");

	Response.Write(PrintCart(true, false)); //true to printer buttons

	Response.Write("</td></tr></form></table></td></tr></table>");
	
	PrintFooter();
}

</script>

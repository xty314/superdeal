<script runat=server>

string r = "r=";
string m_sAdminFooter = "</td></tr></table></td></tr><tr><td><a href=default.aspx?";
string m_sMenuTables = "";
string m_sBranchName = "Mt Eden";
bool m_bShowProgress = false;

void PrintAdminMenu()
{
	BuildMenuTables();
	Response.Write(m_sMenuTables);

	Response.Write("\r\n\r\n<table width=100% cellpadding=0 cellspacing=0 BORDER=0 BGCOLOR=#666696>\r\n");
	Response.Write("<tr style='color:white;background-color:#666696;font-weight:bold;'>\r\n");
	
	Response.Write("<td width=30><a href=default.aspx class=w>&nbsp;Main&nbsp;</td>\r\n");
	Response.Write("<td bgcolor=#666696 width=10 height=20></td>\r\n");

	Response.Write("<td width=30><a href=c.aspx class=w>&nbsp;Sale&nbsp;</td>\r\n");
	Response.Write("<td bgcolor=#666696 width=10 height=20></td>\r\n");

	Response.Write("<td width=30><a href=cart.aspx class=w>&nbsp;Cart&nbsp;</td>\r\n");
	Response.Write("<td bgcolor=#666696 width=10 height=20></td>\r\n");

	Response.Write("<td width=30><a href=q.aspx class=w>&nbsp;Quotation&nbsp;</td>\r\n");
	Response.Write("<td bgcolor=#666696 width=10 height=20></td>\r\n");

	Response.Write("<td width=30><a href=order.aspx class=w>&nbsp;Order&nbsp;</td>\r\n");
	Response.Write("<td bgcolor=#666696 width=10 height=20></td>\r\n");

	Response.Write("<td align=center><b>" + m_sCompanyTitle + " - " + m_sBranchName + " Branch</b></td>");
//	Response.Write("<td id=Report onmouseover='setMenu(\"Report\", \"ReportMenu\")'><a href=default.aspx class=w>&nbsp;Report&nbsp;</td>\r\n");
//	Response.Write("<td bgcolor=#666696 width=10 height=20></td>\r\n");

	Response.Write("<td align=right><b>Sales : </b>" + Session["name"].ToString() + "</td>");
	Response.Write("</tr></table>\r\n");

	if(!m_bShowProgress)
		Response.Write("<table cellpadding=0 cellspacing=0 align=center bgcolor=#FFFFFF width=96% height=90%><tr><td valign=top><table width=100% height=100%><tr><td valign=top>");

}

void PrintAdminHeader()
{
	r += DateTime.Now.ToOADate().ToString(); //random seeds to force IE no cache
	m_sAdminFooter += r;
	m_sAdminFooter += ">Main Page</a></td></tr></table></body></html>";

	Response.Write("<html><head><title>" + m_sCompanyName + " Sales</title>");
	Response.Write("<script language=javascript src=/cssjs/menu.js></script");
	Response.Write(">");
	Response.Write("<style type=\"text/css\">");
	Response.Write("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:vardana;}");
	if(!m_bShowProgress)
		Response.Write("body{background:#666696;font:10px Verdana;}"); 
	else
		Response.Write("body{background:#FFFFFF;font:10px Verdana;}"); 
	Response.Write("a{color:#0033FF;text-decoration:none} a:hover{text-decoration:underline}");
	Response.Write(".m{TEXT-DECORATION:none;background-color:#EEEEEE;border: 1px solid #000000;}");
	Response.Write(".x{FONT-WEIGHT:300;FONT-SIZE:8PT;TEXT-DECORATION:underline;FONT-FAMILY:verdana;COLOR:#0000ff;}");
	Response.Write(".w{color:#FFFFFF;text-decoration:none} a.w:hover{color:#FF0000;text-decoration:none}");
	Response.Write(".d{color:#000000;text-decoration:none} a.d:hover{color:#FF0000;text-decoration:none}");
	Response.Write("</style></head>");
	Response.Write("<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0 ");
	Response.Write("onmouseover=hideMenu() onmouseout=hideMenu()>");
}

void PrintAdminFooter()
{
	Response.Write(m_sAdminFooter);
}

void PrintSearchForm()
{
	Response.Write(search_form);
	if(Session["search_keyword"] != null)
		Response.Write(Session["search_keyword"].ToString());
	Response.Write(search_form2);
}

void BuildMenuTables()
{
	m_sMenuTables = @"
	";
}

const string search_form = @"
<form action=search.aspx method=get>
<table border=0 width=100% bgcolor=#CCCCFF cellspacing=0 cellpadding=5>
<tr><td class=d align=center class=s>
<b>Search&nbsp;</b>
<select name=index>
<option value=blended selected>All Products
<option value=books>Cars
<option value=books>Books
<option value=computers>Computers
<option value=photo>Camera &amp; Photo
<option value=dvd>DVD
<option value=vhs>VHS
<option value=food>Food
<option value=toys>Toys
<option value=electronics>Electronics
<option value=software>Software
<option value=tools>Tools &amp; Hardware
<option value=magazines>Magazines
<option value=garden>Outdoor Living
<option value=kitchen>Kitchen
<option value=travel>Travel
<option value=wireless-phones>Cell Phones & Service
</select>
<b>&nbsp;&nbsp;for&nbsp;&nbsp;</b>
<input type=text name=kw size=15 value='";

const string search_form2 = @"
'>&nbsp;&nbsp;
<input type=image name=Go value='Go!' border=0 alt='Go!' src=/i/go.gif border=0 align=absmiddle > 
</td></tr></table>
</form>
";

</script>

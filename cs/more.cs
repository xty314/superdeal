<script runat=server>

int MaxRows = 24;
string m_brand = "";
int m_nPageSize = 50;
int m_page = 1;

DataSet ds = new DataSet();

void MPage_Load()
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	if(Request.QueryString["p"] != null)
	{
		if(IsInteger(Request.QueryString["p"]))
			m_page = int.Parse(Request.QueryString["p"]);
	}

	PrintHeaderAndMenu();
	
	if(Request.QueryString["b"] != null || Request.QueryString["c"] != null)
		PrintProductList();
	else
	{
		PrintBrandList();
		PrintSSCatList();
	}
}

void PrintBrandList()
{
	int rows = 0;
	string sc = "";
	sc = "SELECT DISTINCT c.brand FROM product_skip k JOIN code_relations c ON c.id=k.id WHERE c.supplier <> 'SS' ORDER BY c.brand";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "brand");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}

	Response.Write("<center><h3>Uncataloged Items - List by Brand</h3>");	
	if(rows <= 0)
	{
		Response.Write("<tr><td>No More :)</td></tr>");
	}
	Response.Write("<table><tr><td valign=top><table>");
	DataRow dr = null;
	int j = 0;
	bool bColumn = true;
	for(int i=0; i < rows; i++)
	{
		j++;
		if(j >= MaxRows)
		{
			j = 1;
			Response.Write("</table></td><td valign=top><table>");
		}
		dr = ds.Tables[0].Rows[i];
		string brand = dr["brand"].ToString();
		Response.Write("<tr><td><a href=more.aspx?b=" + HttpUtility.UrlEncode(brand) + ">");
		if(brand == "")
			brand = "Unknwon";
		Response.Write(brand);
		Response.Write("</a></td></tr>");
	}
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
}

void PrintSSCatList()
{
	int rows = 0;
	string sc = "";
	sc = "SELECT DISTINCT c.cat, c.s_cat FROM product_skip k JOIN code_relations c ON c.id=k.id WHERE c.supplier='SS' ORDER BY c.cat, c.s_cat";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "ss");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}

	Response.Write("<center><h3>Software Solutions - List by Category</h3>");	
	if(rows <= 0)
	{
		Response.Write("<tr><td>No More :)</td></tr>");
	}
	Response.Write("<table><tr><td valign=top><table>");
	DataRow dr = null;
	int j = 0;
	bool bColumn = true;
	string cat = "";
	string cat_old = "";
	MaxRows = 30;
	for(int i=0; i < rows; i++)
	{
		j++;
		if(j >= MaxRows)
		{
			j = 1;
			Response.Write("</table></td><td valign=top><table>");
		}
		dr = ds.Tables["ss"].Rows[i];
		cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		if(cat == "")
			cat = "Unknwon";
		if(cat != cat_old)
			Response.Write("<tr><td><b>" + cat + "</b></td></tr>");
		cat_old = cat;

		Response.Write("<tr><td>&nbsp&nbsp&nbsp&nbsp;<a href=more.aspx?c=" + HttpUtility.UrlEncode(cat) + "&s=" + HttpUtility.UrlEncode(s_cat) + ">");
		Response.Write(s_cat);
		Response.Write("</a></td></tr>");
	}
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
}

void PrintProductList()
{
	m_brand = Request.QueryString["b"];
	string cat = Request.QueryString["c"];
	string s_cat = Request.QueryString["s"];
	int rows = 0;
	string sc = "";
	sc = "SELECT c.cat, c.s_cat, c.ss_cat, c.code, c.name, k.price, k.stock ";
	sc += " FROM product_skip k JOIN code_relations c ON c.id=k.id ";
	if(cat != null && cat != "")
		sc += " WHERE c.cat='" + cat + "' AND s_cat='" + s_cat + "' ";
	else
		sc += " WHERE c.brand='" + m_brand + "' ";
	sc += " ORDER BY c.cat, c.s_cat, c.ss_cat, c.name";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "p");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}

	Response.Write("<center><h3>");
	if(cat != null && cat != "")
	{
		if(cat == "")
			Response.Write("Unknown Category");
		else
			Response.Write(cat + " - " + s_cat);
	}
	else
	{
		if(m_brand == "")
			Response.Write("Unknown Brand");
		else
			Response.Write(m_brand);
	}
	Response.Write("</h3>");	

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	PrintPageIndex();
	
	Response.Write("<tr style=\"color:white;background-color:#888888;font-weight:bold;\">\r\n");
	Response.Write("<td>CODE</td>");
	Response.Write("<td>CATALOG</td>");
	Response.Write("<td>DESC</td>");
	Response.Write("<td>STOCK</td>");
	Response.Write("<td>PRICT</td>");
//	Response.Write("<td>&nbsp;</td>");
	Response.Write("</tr>\r\n");

	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (m_page-1) * m_nPageSize;
//DEBUG("p=", startPage);
	for(int i=startPage; i<ds.Tables["p"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		dr = ds.Tables["p"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
			break;
	}

	PrintPageIndex();
	Response.Write("</table>");
}

void PrintPageIndex()
{
	Response.Write("<tr><td colspan=5 align=right>Page: ");
	int pages = ds.Tables["p"].Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != m_page)
		{
			Response.Write("<a href=more.aspx?b=" + m_brand + "&p=");
			Response.Write(i.ToString());
			Response.Write(">");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	Response.Write("</td></tr>");
}

bool DrawRow(DataRow dr, int i, bool alterColor)
{
	string code = dr["code"].ToString();
//	string details = dr["details"].ToString();

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");

	Response.Write("<td>" + code + "</td>");
	Response.Write("<td nowrap>" + dr["cat"].ToString() + " " + dr["s_cat"].ToString() + " " + dr["ss_cat"].ToString() + " </td>");
	Response.Write("<td><a href=p.aspx?" + code + ">" + dr["name"].ToString() + "</a> </td>");
	Response.Write("<td>" + dr["stock"].ToString() + " </td>");
	Response.Write("<td>" + double.Parse(dr["price"].ToString()).ToString("c") + " </td>");
//	Response.Write("<td><a href=cart.aspx?t=b&c=" + code + "><img src=b.gif border=0></td>");
	Response.Write("</tr>");
//	if(details != "")
//	{
//		details.Replace("<", "[");
//		Response.Write("<tr><td>&nbsp;</td><td colspan=5>" + details + "</td></tr>");
//	}
	return true;
}

</script>

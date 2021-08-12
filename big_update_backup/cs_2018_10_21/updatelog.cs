<script runat=server>

Boolean m_bAlterColor = false;
DataSet	dst = new DataSet();
string m_time = null;
string m_show = null;
DateTime m_dTime;

int m_nPage = 1;
int m_nPageSize = 20;
int m_cols = 6;
int m_nRows = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	GetQueryStrings();

	Response.Write("<br><center><h3>Logs of AutoUpdate</h3></center>");
	if(m_time == null)
	{
		ShowLogs();
	}
	else
	{
		if(!GetChangedItems())
			return;

		if(!DrawUpdateTable())
			return;
	}

	PrintAdminFooter();
}

bool ShowLogs()
{
	int rows = 0;
	string sc = "SELECT DISTINCT time_stamp FROM product_update_log ORDER BY time_stamp DESC"; 
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "logs");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<b>No logs available.</b>");
		return true;
	}

	Response.Write("<b>Available logs:</b>");
	Response.Write("<table>");
	for(int i=0; i<rows; i++)
	{
		DateTime d = DateTime.Parse(dst.Tables["logs"].Rows[i]["time_stamp"].ToString());
		Response.Write("<tr><td><a href=?t=" + d.Ticks + "&r=" + DateTime.Now.ToOADate() + ">");
		Response.Write("<img src=r.gif border=0> ");
		Response.Write(d.ToString("hh:mm dd/MM/yy") + "</a></td></tr>");
	}
	Response.Write("</table>");
	return true;
}

void GetQueryStrings()
{
	if(Request.QueryString["t"] != null)
		m_time = Request.QueryString["t"];
	if(m_time != null)
	{
		m_dTime = new DateTime(long.Parse(m_time));
//DEBUG("m_dTime=", m_dTime.ToString("hh:mm dd/MM/yy"));
	}
	if(Request.QueryString["p"] != null)
		m_nPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["s"] != null)
		m_show = Request.QueryString["s"];
}

bool GetChangedItems()
{
	string sc = "SELECT l.*, c.brand, c.name, c.cat, c.s_cat, c.ss_cat ";
	sc += " FROM product_update_log l JOIN code_relations c ON l.code=c.code ";
	sc += " WHERE l.time_stamp='" + m_dTime.ToString("dd-MM-yyyy") + "' ORDER BY l.code, l.price_age DESC"; 
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "product_update");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean DrawUpdateTable()
{
	Boolean bRet = true;
	
	DrawTableHeader();
	int rows = dst.Tables["product_update"].Rows.Count;
	int i = 0;
	for(; i<rows; i++)
	{
		if(!DrawRow(ref i, rows))
		{
			bRet = false;
			break;
		}
	}
	DrawTableFooter();
	return bRet;
}

string WriteURLWithoutPageNumber()
{
	string s = "?t=" + m_dTime.Ticks + "&s=" + m_show + "&r=" + DateTime.Now.ToOADate();
	return s;
}

string PrintPageIndex()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=" + m_cols + ">Page: ");
	int pages = m_nRows / m_nPageSize + 1;
//	int pages = dst.Tables["product_update"].Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != m_nPage)
		{
			sb.Append("<a href=");
			sb.Append(WriteURLWithoutPageNumber());
			sb.Append("&p=");
			sb.Append(i.ToString());
			sb.Append(">");
			sb.Append(i.ToString());
			sb.Append("</a> ");
		}
		else
		{
			sb.Append(i.ToString());
			sb.Append(" ");
		}
	}
	sb.Append("</td></tr>");
	return sb.ToString();
}

void DrawTableHeader()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<table width=100% cellspacing=0 cellpadding=0 align=Center rules=all bgcolor=white \r\n");
	sb.Append("bordercolor=#FFFFFF border=1 width=90% style=\"font-family:Verdana);font-size:8pt);\r\n");
	sb.Append("border-collapse:collapse);\">\r\n");
	sb.Append("<tr><td>");
	sb.Append(m_dTime.ToString("dd.MM.yy hh:mm"));
	sb.Append("</td><td colspan=" + (m_cols-1) + " align=right>");
	sb.Append("<a href=?t=" + m_dTime.Ticks + "&s=s&r=" + DateTime.Now.ToOADate());
	sb.Append(">Stock Changes</a> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	sb.Append("<a href=?t=" + m_dTime.Ticks + "&s=e&r=" + DateTime.Now.ToOADate());
	sb.Append(">ETA Changes</a> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	sb.Append("<a href=?t=" + m_dTime.Ticks + "&s=p&r=" + DateTime.Now.ToOADate());
	sb.Append(">Price Changes</a> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	sb.Append("<a href=?t=" + m_dTime.Ticks + "&r=" + DateTime.Now.ToOADate());
	sb.Append(">All Changes</a> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	sb.Append("<a href=?r=" + DateTime.Now.ToOADate() + ">Other Logs</a></td></tr>");
	sb.Append("<tr style=\"color:red;background-color:#CCCCCC;font-weight:bold;\">\r\n");
	sb.Append("<td>code</td>\r\n");
	sb.Append("<td>id</td>\r\n");
	sb.Append("<td>name (description)</td>\r\n");
//	sb.Append("<td>brand</td>");
//	sb.Append("<td>cat</td>");
//	sb.Append("<td>s_cat</td>");
//	sb.Append("<td>ss_cat</td>");
//	sb.Append("<td>supplier</td>");
//	sb.Append("<td>supplier_code</td>");
	sb.Append("<td>stock</td>");
	sb.Append("<td>eta</td>");
	sb.Append("<td>price</td>");
//	sb.Append("<td>price</td>");
//	sb.Append("<td>rate</td>");
//	sb.Append("<td>pdorp</td>");
//	sb.Append("<td>action</td>");
	sb.Append("</tr>\r\n");
	Response.Write(sb.ToString());
	Response.Flush();
}

void DrawTableFooter()
{
	StringBuilder sb = new StringBuilder();
	sb.Append(PrintPageIndex());
	sb.Append("</table>\r\n<br>\r\n");
	Response.Write(sb.ToString());
}

bool DrawRow(ref int i, int rows)
{
	bool bRet = true;

	DataRow dr = dst.Tables["product_update"].Rows[i];
	DataRow drp = null;
	if(i+1 < rows)
		drp = dst.Tables["product_update"].Rows[i+1];

	if(drp != null)
	{
		if(dr["id"].ToString() != drp["id"].ToString())
			drp = null;
		else
			i++;
	}
	string color="#FF7777";

	//detect changes
	string code			= dr["code"].ToString();
	string id			= dr["id"].ToString();
	string name			= dr["name"].ToString();
	string stock		= dr["stock"].ToString();
	string eta			= dr["eta"].ToString();
	string supplier_price = dr["supplier_price"].ToString();

	string id_current = "";
	string stock_current = "";
	string eta_current = "";
	string supplier_price_current = "";

//	if(!GetProduct(code, ref drp))
//		return false;

	if(drp != null)
	{
		id_current				= drp["id"].ToString();
		stock_current			= drp["stock"].ToString();
		eta_current				= drp["eta"].ToString();
		supplier_price_current	= drp["supplier_price"].ToString();
	}
	if(m_show == "p") // show price changes only
	{
		if(supplier_price == supplier_price_current)
			return true;
	}
	else if(m_show == "s") // show price changes only
	{
		if(stock == stock_current)
			return true;
	}
	else if(m_show == "e") // show price changes only
	{
		if(eta == eta_current)
			return true;
	}

	m_nRows++;

	int start = (m_nPage - 1)* m_nPageSize;
	if(m_nRows < start || m_nRows > start + m_nPageSize)
		return true;
					
	StringBuilder sb = new StringBuilder();
	sb.Append("<tr");

	if(m_bAlterColor)
		sb.Append(" bgcolor=#EEEEEE");
	m_bAlterColor = !m_bAlterColor;

	sb.Append("><td");
	if(drp == null)
	{
		sb.Append(" bgcolor=");
		sb.Append(color);
	}
	sb.Append(">");
	sb.Append(dr["code"].ToString());
	sb.Append("</td><td");
//	sb.Append(dr["brand"].ToString());
//	sb.Append("</td><td>");
//	sb.Append(dr["cat"].ToString());
//	sb.Append("&nbsp;</td><td>");
//	sb.Append(dr["s_cat"].ToString());
//	sb.Append("&nbsp;</td><td>");
//	sb.Append(dr["ss_cat"].ToString());
//	sb.Append("&nbsp;</td><td");
	if(drp != null && id != id_current)
	{
		sb.Append(" bgcolor=");
		sb.Append(color);
		sb.Append(" title=");
		sb.Append(id_current);
	}
	sb.Append(">");
	sb.Append(dr["id"].ToString());
	sb.Append("</td><td>");
	sb.Append(dr["name"].ToString());
	sb.Append("</td><td");
//	sb.Append(dr["supplier_code"].ToString());
	
//	sb.Append("</td><td");
	if(drp != null && stock != stock_current)
	{
		sb.Append(" bgcolor=");
		sb.Append(color);
		sb.Append(" title=");
		sb.Append(stock_current);
	}
	sb.Append(">");
	sb.Append(stock);

	sb.Append("&nbsp;</td><td");
	if(drp != null && eta != eta_current)
	{
		sb.Append(" bgcolor=");
		sb.Append(color);
		sb.Append(" title=");
		sb.Append(eta_current);
	}
	sb.Append(">");
	sb.Append(eta);

	sb.Append("&nbsp;</td><td");
	if(drp != null && supplier_price != supplier_price_current)
	{
		sb.Append(" bgcolor=");
		sb.Append(color);
		sb.Append(" title=");
		sb.Append(supplier_price_current);
	}
	sb.Append(">");
	sb.Append(supplier_price);

//	sb.Append("</td><td>");
//	sb.Append(dr["price"].ToString());
//	sb.Append("</td><td>");
//	sb.Append(dr["rate"].ToString());
//	sb.Append("</td><td>");
//	sb.Append(dr["price_dropped"].ToString());

	sb.Append("</td></tr>\r\n");

	Response.Write(sb.ToString());
	return bRet;
}
</script>

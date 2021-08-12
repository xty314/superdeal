<!-- #include file="price.cs" -->
<script runat=server>

string ebrand = null;
string ecat = "hardware";
string es_cat = "monitor";
string ess_cat = null;

string brand = null;
string cat = "hardware";
string s_cat = "monitor";
string ss_cat = null;

string orderby = null;
string m_type = null;
int page = 1;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	GetQueryStrings();

	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();
	
	if(m_type == "update")
	{
//DEBUG("update", page);
		string update = Request.Form["update"];
		if(UpdateAllRows())
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			string s = "<br><br>done! wait a moment......... <br>\r\n";
			s += "<meta http-equiv=\"refresh\" content=\"1; URL=";
			s += WriteURLWithoutPageNumber();
			s += "&p=";
			s += page;
			s += "\"></body></html>";
			Response.Write(s);
		}
	}
	else
	{
		if(!DoSearch())
			return;
	
		MyDrawTable();
	}
	WriteFooter();
	PrintAdminFooter();
}

string WriteURLWithoutPageNumber()
{
	StringBuilder sb = new StringBuilder();

	sb.Append("edithotskip.aspx?");
	if(brand != null)
	{
		sb.Append("b=");
		sb.Append(ebrand);
	}
	else
	{
		sb.Append("c=");
		sb.Append(ecat);
	}
	if(s_cat != null)
	{
		sb.Append("&s=");
		sb.Append(es_cat);
	}
	if(ss_cat != null)
	{
		sb.Append("&ss=");
		sb.Append(ess_cat);
	}
	sb.Append("&r=" + DateTime.Now.ToOADate());
	return sb.ToString();
}

void GetQueryStrings()
{
	brand = Request.QueryString["b"];
	cat = Request.QueryString["c"];
	s_cat = Request.QueryString["s"];
	ss_cat = Request.QueryString["ss"];

	ebrand = HttpUtility.UrlEncode(brand);
	ecat = HttpUtility.UrlEncode(cat);
	es_cat = HttpUtility.UrlEncode(s_cat);
	ess_cat = HttpUtility.UrlEncode(ss_cat);
	m_type = Request.QueryString["t"];
	string spage = Request.QueryString["p"];
	if(spage != null)
		page = int.Parse(spage);
//DEBUG("page=", page);
}

Boolean DoSearch()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT c.id, p.code, p.brand, p.name, p.cat, p.s_cat, p.ss_cat, p.stock, p.hot, c.skip FROM product p JOIN code_relations c ON p.code=c.code WHERE ");
	if(brand != null)
	{
		sb.Append(" p.brand='");
		sb.Append(brand);
		sb.Append("'");
	}
	else
	{
		sb.Append(" p.cat='");
		sb.Append(cat);
		sb.Append("'");
	}

	if(s_cat != null)
	{
		sb.Append(" AND p.s_cat='");
		sb.Append(s_cat);
		sb.Append("'");
	}
	if(ss_cat != null)
	{
		sb.Append(" AND p.ss_cat='");
		sb.Append(ss_cat);
		sb.Append("'");
	}
	sb.Append(" ORDER BY p.brand, p.s_cat, p.ss_cat, p.name, p.code");
//DEBUG("query=", sb.ToString());	
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		int rows = myAdapter.Fill(dst, "product");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

Boolean UpdateAllRows()
{
	int i = (page-1) * 10;
	string id = Request.Form["id"+i.ToString()];
	while(id != null)
	{
		if(!UpdateOneRow(i.ToString()))
			return false;;
		i++;
		id = Request.Form["id"+i.ToString()];
	}
	return true;
}

bool UpdateOneRow(string sRow)
{
	Boolean bRet = true;

	string code		= Request.Form["code"+sRow];
	string id		= Request.Form["id"+sRow];

	string stock	= Request.Form["stock"+sRow];
	if(stock == "")
		stock = "null";

	string hot		= Request.Form["hot"+sRow];
	string skip		= Request.Form["skip"+sRow];

	if(hot == null)
		hot = "0";
	else
		hot = "1";

	if(skip == null)
		skip = "0";
	else
		skip = "1";
	
	StringBuilder sb = new StringBuilder();
//DEBUG("skip=", skip);
//DEBUG("id=", id);
	if(skip=="1")
	{
		//add to skip table
		if(!AddToSkipTable(code))
			return false;

		//update code_relations
		sb.Append("UPDATE code_relations SET skip=");
		sb.Append(skip);
		sb.Append(" WHERE id='");
		sb.Append(id);
		sb.Append("'");

		try
		{
			myCommand = new SqlCommand(sb.ToString());
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sb.ToString(), e);
			return false;
		}
		
		sb.Remove(0, sb.Length);
		sb.Append("DELETE FROM product WHERE code=");
		sb.Append(code);
		sb.Append("");
	}
	else
	{
		//update product (live update)
		sb.Append("UPDATE product SET stock=");
		sb.Append(stock);
		sb.Append(", hot=");
		sb.Append(hot);
		sb.Append(" WHERE code=");
		sb.Append(code);
	}
//DEBUG("code=", code);
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

	return bRet;
}

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<br><center><h3>Select Hot Product</h3></center>");
	sb.Append("<form action=");
	sb.Append(WriteURLWithoutPageNumber());
	sb.Append("&t=update&p=");
	sb.Append(page);
	sb.Append(" method=post>\r\n");
	Response.Write(sb.ToString());
	Response.Flush();
}

void WriteFooter()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("</td></tr></table>");
	sb.Append("</form>");//</body></html>");
	Response.Write(sb.ToString());
}

Boolean MyDrawTable()
{
	Boolean bRet = true;
	DrawTableHeader();
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (page-1) * 10;
	for(int i=startPage; i<dst.Tables["product"].Rows.Count; i++)
	{
		if(i-startPage >= 10)
			break;
		dr = dst.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
		{
			bRet = false;
			break;
		}
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=6 align=right><input type=submit name=update value='Update'></td>\r\n");
	sb.Append("</tr><tr><td colspan=6 align=right>Page: ");
	int pages = dst.Tables["product"].Rows.Count / 10 + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != page)
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
		
	sb.Append("</table>\r\n");
	Response.Write(sb.ToString());

	return bRet;
}

void DrawTableHeader()
{
	StringBuilder sb = new StringBuilder();;
	sb.Append("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr height=10 style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("<td>code</td>\r\n");
	sb.Append("<td>name (description)</td>\r\n");
	sb.Append("<td>stock</td>");
	sb.Append("<td>hot</td>");
	sb.Append("<td>skip</td>");
	sb.Append("</tr>\r\n");
	
	Response.Write(sb.ToString());
	Response.Flush();
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
	string code = dr["code"].ToString();
	string id = dr["id"].ToString();
	string name = dr["name"].ToString();
	string stock = dr["stock"].ToString();
	string hot = dr["hot"].ToString();
	string skip = dr["skip"].ToString();
	string index = i.ToString();

	StringBuilder sb = new StringBuilder();;
	
	sb.Append("<input type=hidden name=id");
	sb.Append(index);
	sb.Append(" value='");
	sb.Append(id);
	sb.Append("'>");

	sb.Append("<input type=hidden name=code");
	sb.Append(index);
	sb.Append(" value='");
	sb.Append(code);
	sb.Append("'>");

	sb.Append("<tr");
	if(alterColor)
		sb.Append(" bgcolor=#EEEEEE");
	sb.Append("><td>");
	sb.Append(code);
	sb.Append("</td><td>");
	sb.Append(name);

	sb.Append("</td><td><input type=text size=3 name=stock");
	sb.Append(index);
	sb.Append(" value=");
	sb.Append(stock);

	sb.Append("></td><td><input type=checkbox name=hot");
	sb.Append(index);
	sb.Append(" value=");
	if(String.Compare(hot, "true", true) == 0)
		sb.Append("1 checked");
	else
		sb.Append("0 unchecked");

	sb.Append("></td><td><input type=checkbox name=skip");
	sb.Append(index);
	sb.Append(" value=");
	if(String.Compare(skip, "true", true) == 0)
		sb.Append("1 checked");
	else
		sb.Append("0 unchecked");

	sb.Append("></td></tr>\r\n");

	Response.Write(sb.ToString());
	Response.Flush();
	return true;
}

</script>
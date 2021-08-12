<script runat=server>
//////////////////////////////////////////////////////////////////////////////////////
//common functions ebrand.aspx, ecat.aspx, escat.aspx, esscat.aspx
string ebrand;
string ecat;
string es_cat;
string ess_cat;

string brand;
string cat;
string s_cat;
string ss_cat;

string m_type = null;
int page = 1;
const int m_nPageSize = 15; //how many rows in oen page
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

Boolean DoSearch()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("SELECT c.id, p.code, p.brand, p.name, p.cat, p.s_cat, p.ss_cat FROM product p LEFT OUTER JOIN code_relations c ON p.code=c.code WHERE ");
	sb.Append("SELECT c.id, p.* FROM product p LEFT OUTER JOIN code_relations c ON p.code=c.code WHERE ");
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

string WriteURLWithoutPageNumber()
{
	StringBuilder sb = new StringBuilder();

	sb.Append(thisurl + "?");
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

Boolean UpdateAllRows()
{
	int i = (page-1) * m_nPageSize;
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

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("<html><style type=\"text/css\">td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
//	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>");
//	sb.Append("<body bgcolor=#666696>\r\n");
	sb.Append("<table width=100% height=100% bgcolor=white align=center valign=center><tr><td valign=top>");
	sb.Append("<br><center><h3>" + tableTitle + "</h3></center>");
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
	sb.Append("</td></tr></table>");
//	sb.Append("</form></body></html>");
	Response.Write(sb.ToString());
}

Boolean MyDrawTable()
{
	Boolean bRet = true;
	DrawTableHeader();
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (page-1) * m_nPageSize;
	for(int i=startPage; i<dst.Tables["product"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
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
	sb.Append("<tr><td colspan=" + cols + " align=right><input type=submit name=update value='Update'></td></tr>");
	sb.Append("<tr><td colspan=" + cols + " align=right>Page: ");
	int pages = dst.Tables["product"].Rows.Count / m_nPageSize + 1;
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
	sb.Append("</td></tr>");
//	sb.Append("<tr><td colspan=" + cols + "><a href=default.aspx>Main Page</a>&nbsp;&nbsp;&nbsp;<a href=ec.aspx>Update Catalog</a></td></tr>");
		
	sb.Append("</table>\r\n");
	Response.Write(sb.ToString());

	return bRet;
}

bool CheckCodeRelations(string id, DataRow dr)
{
	if(id == null || id == "")
	{
		DEBUG("warning, wrong product deleted, code=" + dr["code"].ToString() + ", name=" + dr["name"].ToString(), " save into product_bak table");
		if(!BackupProduct(dr))
			return false;
		string sc;

		sc = "DELETE FROM product WHERE code=" + dr["code"].ToString();
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}
</script>
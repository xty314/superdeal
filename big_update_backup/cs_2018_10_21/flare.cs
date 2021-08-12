<!-- #include file="page_index.cs" -->
<script runat=server>
//////////////////////////////////////////////
// data grid template

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string cat = "";
string s_cat = "";
string ss_cat = "";

int total = 0;

string m_action = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("administrator"))
		return;

	if(Request.QueryString["t"] != null)
		m_action = Request.QueryString["t"];

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
	if(m_action == "bulk")
	{
		Trim(ref cat);
		Trim(ref s_cat);
		Trim(ref ss_cat);

		if(Request.Form["cmd"] == "Add All Pages")
		{
			if(DoBulkAddAll())
			{
				Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?t=bulk");
				Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
				Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
				Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
				Response.Write("\">");
				return;
			}
		}
		else if(Request.Form["cmd"] == "Add Selected")
		{
			if(DoBulkAddSelected())
			{
				Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?t=bulk");
				Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
				Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
				Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
				if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
					Response.Write("&p=" + Request.QueryString["p"]);
				if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
					Response.Write("&spb=" + Request.QueryString["spb"]);
				Response.Write("\">");
				return;
			}
		}
		PrintAdminHeader();
		PrintAdminMenu();
		PrintBulkForm();
		PrintAdminFooter();
		return;
	}
	else if(m_action == "del")
	{
		if(DoDelete())
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"\">");
		return;
	}
	else if(m_action == "print1")
	{
		PrintPriceList(1);
		return;
	}
	else if(m_action == "print2")
	{
		PrintPriceList(2);
		return;
	}
	else if(Request.Form["code"] != null && Request.Form["code"] != "")
	{
		if(DoAddOne())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=flare.aspx?code=" + Request.Form["code"] + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h3>Print Price List</h3>");
	ShowEditTable();
	PrintAdminFooter();
}

bool GetFlareTable()
{
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];

	string sc = " SELECT f.id, f.code, p.name, p.cat, p.s_cat, p.ss_cat ";
	sc += " FROM flare f JOIN product p ON p.code = f.code ";

	bool bWhereAdded = false;
	if(cat != "")
	{
		sc += " WHERE p.cat = '" + cat + "' ";
		bWhereAdded = true;
	}
	if(s_cat != "")
	{
		if(!bWhereAdded)
			sc += " WHERE ";
		else 
			sc += " AND ";
		sc += " p.s_cat = '" + s_cat + "' ";
	}
	if(ss_cat != "")
	{
		if(!bWhereAdded)
			sc += " WHERE ";
		else 
			sc += " AND ";
		sc += " p.ss_cat = '" + ss_cat + "' ";
	}
	if(Request.QueryString["code"] != null)
	{
		if(!bWhereAdded)
			sc += " WHERE ";
		else 
			sc += " AND ";
		sc += " f.code = " + Request.QueryString["code"];
	}
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "flare");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetFlareData()
{
	string sc = " SELECT p.code, p.name, p.cat, p.s_cat, p.ss_cat, p.price, c.level_rate1, c.level_rate2, c.level_rate3, c.level_rate4,c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9 ";
	sc += " FROM flare f JOIN code_relations c ON c.code = f.code ";
	sc += " JOIN product p ON p.code = f.code ";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "data");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoDelete()
{
	string sc = " DELETE FROM flare ";
	sc += " WHERE id = " + Request.QueryString["id"];
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
	return true;	
}

bool DoAddOne()
{
	if(GetProductDesc(Request.Form["code"]) == "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, product not found. code = " + Request.Form["code"] + "</h3>");
		return false;
	}

	string sc = " IF NOT EXISTS (SELECT * FROM flare WHERE code=" + Request.Form["code"] + ") ";
	sc += " INSERT INTO flare (code) VALUES(" + Request.Form["code"] + ") ";
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
	return true;	
}

/////////////////////////////////////////////////////////////////
void DoShowDealerLevel()
{
	int nDealerLevel = int.Parse(GetSiteSettings("dealer_levels", "6"));
	Response.Write("<select name=dealer_level>");
	for(int i=1; i<=nDealerLevel; i++)
	{
		Response.Write("<option value="+ i +">Dealer Level: "+ i +"</option>");
	}
	Response.Write("</select>");

}
void ShowEditTable()
{
	if(!GetFlareTable())
		return;

	string tableName = "flare";
	DataColumnCollection dc = ds.Tables[tableName].Columns;

	int cols = dc.Count;
	int i = 0;

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?";
	m_cPI.PageSize = 30;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<form action=flare.aspx method=post name=f>");
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=3><b>Code : </b><input type=text name=code size=5>");
	Response.Write("<input type=submit name=cmd value=Add " + Session["button_style"] + ">");
//	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=button name=cmd value='Bulk Add' ");
	Response.Write(" onclick=window.location=('flare.aspx?t=bulk') " + Session["button_style"] + ">");
//	Response.Write("<i>(select from catalog)</i>");

	
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	PrintCatsForDisplay();
	Response.Write("</td>");
	
	Response.Write("<td colspan=4 align=right>");
	
	Response.Write("<input type=button onclick=window.open('editpage.aspx?p=flare_header') value='Edit Header' " + Session["button_style"] + ">");
	Response.Write("&nbsp&nbsp&nbsp;Select Dealer Level:");
	DoShowDealerLevel();
	Response.Write("<input type=button onclick=\"window.open('flare.aspx?t=print1&dl='+ document.f.dealer_level.value)\" value='Print Page 1' " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=\"window.open('flare.aspx?t=print2&dl='+ document.f.dealer_level.value)\" value='Print Page 2' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(int m=1; m<cols; m++)
		Response.Write("<th><b>" + dc[m].ColumnName + "</b></th>");
	Response.Write("<th><b>Action</b></th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + GetSiteSettings("table_row_bgcolor", "#EEEEEE"));
		bAlterColor = !bAlterColor;
		Response.Write(">");

		DataRow dr = ds.Tables[tableName].Rows[i];
		string value = "";
		for(int j=1; j<cols; j++)
		{
			value = dr[j].ToString();
			Response.Write("<td>" + value + "</td>");
		}
		Response.Write("<td align=right>");
		Response.Write("<a href=liveedit.aspx?code=" + dr["code"].ToString() + " class=o target=_blank>EDIT</a> &nbsp; ");
		Response.Write("<a href=flare.aspx?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"&t=del&id=" + dr["id"].ToString() + " class=o>DEL</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=" + cols + ">" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	return;
}

bool PrintBulkForm()
{
	if(!GetProductList())
		return false;

	string tableName = "bulk";
	int i = 0;
	DataColumnCollection dc = ds.Tables[tableName].Columns;
	int cols = dc.Count;
	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	m_cPI.TotalRows = rows;
	m_cPI.URI = "?t=bulk";
	m_cPI.URI += "&cat=" + HttpUtility.UrlEncode(cat);
	m_cPI.URI += "&scat=" + HttpUtility.UrlEncode(s_cat);
	m_cPI.URI += "&sscat=" + HttpUtility.UrlEncode(ss_cat);

	m_cPI.PageSize = 20;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<br><center><h3>Price List Templates - Bulk Add</h3>");

	Response.Write("<form action=flare.aspx?t=bulk");
	Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
		Response.Write("&p=" + Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
		Response.Write("&spb=" + Request.QueryString["spb"]);
	Response.Write(" method=post>");

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=4 nowrap><b>Select Catalog : </b>");
	PrintCats();
	Response.Write("</td>");

	Response.Write("<td colspan=3 align=right>");
	Response.Write("<b>Total </b><font color=red><b>" + total + "</b></font><b> items found</b>&nbsp&nbsp;");
	Response.Write("<input type=submit name=cmd value='Add Selected' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Add All Pages' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(int m=0; m<cols; m++)
		Response.Write("<th><b>" + dc[m].ColumnName + "</b></th>");
	Response.Write("<th><b>Select</b></th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("<tr><td colspan=6 align=right>");
		Response.Write("<input type=button value='Show Template' onclick=window.location=('flare.aspx') " + Session["button_style"] + ">");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		Response.Write("</form>");
		return true;
	}

	bool bAlterColor = false;

	Response.Write("<input type=hidden name=row_start value=" + i + ">");

	for(; i < rows && i < end; i++)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + GetSiteSettings("table_row_bgcolor", "#EEEEEE"));
		bAlterColor = !bAlterColor;
		Response.Write(">");

		DataRow dr = ds.Tables[tableName].Rows[i];
		string value = "";
		for(int j=0; j<cols; j++)
		{
			value = dr[j].ToString();
			Response.Write("<td>" + value + "</td>");
		}

		Response.Write("<input type=hidden name=code" + i + " value=" + dr["code"].ToString() + ">");
		Response.Write("<td align=right><input type=checkbox name=select" + i);
		Response.Write("></td>");
		Response.Write("</tr>");
	}
	
	Response.Write("<input type=hidden name=row_end value=" + i + ">");

	Response.Write("<tr><td colspan=" + cols + ">" + sPageIndex + "</td></tr>");

	Response.Write("<tr><td colspan=6 align=right>");
	Response.Write("<input type=button value='Show Template' onclick=window.location=('flare.aspx') " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");	

	return true;
}

bool GetProductList()
{
	string sc = "";
	if(cat == "")
		cat = "just build the table, no row return please";

	sc = " SELECT p.code, p.cat, p.s_cat, p.ss_cat, p.name ";
	sc += " FROM product p ";
	sc += " JOIN stock_qty s ON s.code = p.code";
//	sc += " LEFT OUTER JOIN flare f ";
	sc += " WHERE p.cat = '" + cat + "' ";
	if(s_cat != "" && s_cat != "all")
		sc += " AND p.s_cat = '" + s_cat + "' ";
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND p.ss_cat = '" + ss_cat + "' ";
	sc += " AND p.code NOT IN (SELECT code FROM flare) ";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		total = myAdapter.Fill(ds, "bulk");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintCats()
{
	int rows = 0;
	string sc = "SELECT DISTINCT p.cat ";
	sc += " FROM product p ";
	sc += " JOIN stock_qty s ON s.code = p.code ";
	sc += " ORDER BY p.cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("<select name=cat ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
	Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Select One</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		sc = "SELECT DISTINCT p.s_cat ";
		sc += " FROM product p ";
		sc += " JOIN stock_qty s ON s.code = p.code ";
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " ORDER BY p.s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}
		Response.Write("</select>");
	}

	
	if(s_cat != "")
	{
		sc = "SELECT DISTINCT ss_cat ";
		sc += " FROM product p ";
		sc += " JOIN stock_qty s ON s.code = p.code ";
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " AND p.s_cat = '"+ s_cat +"' ";
		sc += " ORDER BY p.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=ss_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");

		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}

	return true;
}

bool PrintCatsForDisplay()
{
	int rows = 0;
	string sc = "SELECT DISTINCT p.cat ";
	sc += " FROM product p JOIN flare f ON f.code = p.code ";
	sc += " ORDER BY p.cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("<select name=cat ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?");
	Response.Write("cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>All</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		sc = "SELECT DISTINCT p.s_cat ";
		sc += " FROM product p JOIN flare f ON f.code = p.code ";
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " ORDER BY p.s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?");
		Response.Write("cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}
		Response.Write("</select>");
	}

	
	if(s_cat != "")
	{
		sc = "SELECT DISTINCT p.ss_cat ";
		sc += " FROM product p JOIN flare f ON f.code = p.code ";
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " AND p.s_cat = '"+ s_cat +"' ";
		sc += " ORDER BY p.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=ss_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?");
		Response.Write("cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");

		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}
		Response.Write("</select>");
	}
	return true;
}

bool DoBulkAddAll()
{
	if(!GetProductList())
		return false;

	string sc = "";
	int rows = ds.Tables["bulk"].Rows.Count;
	for(int i=0; i<rows; i++)
	{
		sc += " INSERT INTO flare (code) VALUES(" + ds.Tables["bulk"].Rows[i]["code"] + ") ";
	}
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
	return true;
}

bool DoBulkAddSelected()
{
	string sc = "";
	int row_start = MyIntParse(Request.Form["row_start"]);
	int row_end = MyIntParse(Request.Form["row_end"]);
	for(int i=row_start; i<row_end; i++)
	{
		if(Request.Form["select" + i] == "on")
			sc += " INSERT INTO flare (code) VALUES(" + Request.Form["code" + i] + ") ";
	}

	if(sc == "")
		return true;

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
	return true;
}

string PrintHalfPage(int start, int rows, int page_rows, ref int finished)
{
	string th1 = "<table width=100% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white";
	th1 += " style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">";

	StringBuilder sb1 = new StringBuilder();
	sb1.Append(th1);

	string s_cat = "";
	string s_cat_old = "";
	string code = "";
	string name = "";
	string price = "";
	string lr1 = "";
	double drrp = 0;
	int count = 0;

	string tableName = "data";
	string dealer_level = "1";
if(Request.QueryString["dl"] != null && Request.QueryString["dl"] != "")
	dealer_level = Request.QueryString["dl"];

if(!TSIsDigit(dealer_level))
	dealer_level = "1";
if(int.Parse(dealer_level) > int.Parse(GetSiteSettings("dealer_levels","6")))
	dealer_level = "1";

	int i = start;
	for(i=start; i<rows; i++)
	{
		DataRow dr = ds.Tables[tableName].Rows[i];

		s_cat = dr["s_cat"].ToString();
		Trim(ref s_cat);
		code = dr["code"].ToString();
		name = dr["name"].ToString();
		price = dr["price"].ToString();
//		lr1 = dr["level_rate1"].ToString();
		lr1 = dr["level_rate"+ dealer_level].ToString();

		drrp = Math.Round(MyDoubleParse(price) * MyDoubleParse(lr1), 2);

		if(count > page_rows)
			break; //print at the right half of the page with a new catalog start

		if(s_cat != s_cat_old || i == start)
		{
			if(i ==	start)
				sb1.Append("<tr bgcolor=#DDDDDD><td><b>Code</b></td><td><b>" + s_cat + "</b></td><td align=right><b>Price</b></td></tr>");
			else
				sb1.Append("<tr bgcolor=#DDDDDD><td>&nbsp;</td><td><b>" + s_cat + "</b></td><td>&nbsp;</td></tr>");
			s_cat_old = s_cat;
			count++;
			if(count > page_rows)
				break; //print at the right half of the page with a new catalog start
		}

		if(name.Length > 50)
			name = name.Substring(0, 50);
		sb1.Append("<tr>");
		sb1.Append("<td>" + code + "&nbsp&nbsp;</td>");
		sb1.Append("<td>" + name + "</td>");
		sb1.Append("<td align=right>" + drrp.ToString("c") + "</td>");
		sb1.Append("</tr>");
		count++;
	}
	sb1.Append("</table>");
	finished = i;
	return sb1.ToString();
}

bool PrintPriceList(int page)
{
	if(!GetFlareData())
		return false;

	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();

	sb.Append("<html><head>");
	sb.Append("<style type=text/css>");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:6PT;FONT-FAMILY:verdana;}");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:6PT;FONT-FAMILY:verdana;}");
	sb.Append("</style></head>");
	sb.Append("<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0 text=black>");

	sb.Append("<table align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:3px;border-style:Solid;border-collapse:collapse;fixed\">");

	if(page == 1)
	{
		sb.Append("<tr><td colspan=2>");
		string header = ReadSitePage("flare_header");
		header = header.Replace("@@date", DateTime.Now.ToString("dd/MM/yy"));
		sb.Append(header);
		sb.Append("</td></tr>");
	}

	if(rows <= 0)
	{
		sb.Append("</table>");
		return true;
	}

	int start = 0;

	StringBuilder sb1 = new StringBuilder();
	sb1.Append("<tr><td valign=top>");
	sb1.Append(PrintHalfPage(start, rows, page_rows, ref start));
	sb1.Append("</td><td valign=top>");
	sb1.Append(PrintHalfPage(start, rows, page_rows, ref start));
	sb1.Append("</td></tr>");
	
	if(page == 1)
		sb.Append(sb1.ToString());
	else if(page == 2)
	{
		sb.Append("<tr><td valign=top>");
		sb.Append(PrintHalfPage(start, rows, page_rows, ref start));
		sb.Append("</td><td valign=top>");
		sb.Append(PrintHalfPage(start, rows, page_rows, ref start));
		sb.Append("</td></tr>");
	}
	sb.Append("</td></tr>");
	sb.Append("</table>");

	Response.Write(sb.ToString());
	return true;
}

</script>

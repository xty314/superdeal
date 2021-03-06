<!-- #include file="page_index.cs" -->
<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_id = "";
string m_kw = "";

void Page_Load(Object Src, EventArgs E ) 
{
    TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
    
	m_type = g("t");
	m_action = g("a");
	m_kw = p("kw");
	if(m_kw == "")
		m_kw = g("kw");
	m_cmd = p("cmd");
	m_id = MyIntParse(g("id")).ToString();
	
	if(m_cmd == Lang("Add"))
	{
		if(DoUpdateData(""))
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("New data successfully added, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Save"))
	{
		if(DoUpdateData(m_id))
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully saved, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?kw=" + HttpUtility.UrlEncode(m_kw) + "&p=" + g("p") + "&spb=" + g("spb") + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_action == "del")
	{
		if(DoDeleteData())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully deleted, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?kw=" + HttpUtility.UrlEncode(m_kw) + "&p=" + g("p") + "&spb=" + g("spb") + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	PrintMainForm();
}

bool PrintMainForm()
{
	Response.Write("<br><center><h4>" + Lang("Edit Shelf") + "</h4></center>");
	Response.Write("<form name=f action=?id=" + m_id + "&p=" + g("p") + "&spb=" + g("spb") + " method=post>");
	Response.Write("<table width=75% align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr><td colspan=6 align=right>");
	Response.Write("<input type=text name=kw value='" + m_kw + "'>");
	Response.Write("<input type=submit class=b value='" + Lang("Search") + "'>");
	if(m_kw != "")
		Response.Write("<input type=button value='" + Lang("Show All") + "' class=b onclick=\"window.location='?';\">");
	Response.Write("</td></tr>");
 
	Response.Write("<tr class=th>");
	Response.Write("<th>ID</th>");
	Response.Write("<th>" + Lang("Area") + "</th>");
	Response.Write("<th>" + Lang("Location") + "</th>");
	Response.Write("<th>" + Lang("Section") + "</th>");
	Response.Write("<th>&nbsp; " + Lang("Level") + " &nbsp; </th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	string sc = " SELECT s.id, s.area, s.name, s.location, s.section, s.level ";
	sc += " FROM shelf s ";
	sc += " WHERE 1 = 1 ";
	if(m_kw != "")
	{
		if(IsInteger(m_kw))
			sc += " AND (s.id = " + m_kw + ") ";
		else
			sc += " AND (s.name LIKE '%" + EncodeQuote(m_kw) + "%') ";
	}
	sc += " ORDER BY s.area, s.location, s.section, s.level ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "data");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(g("p"));
	if(m_cPI.CurrentPage <= 0)
		 m_cPI.CurrentPage = 1;	
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = MyIntParse(g("spb"));
	int nRows = ds.Tables["data"].Rows.Count;
	m_cPI.TotalRows = nRows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(m_kw != null)
		m_cPI.URI += "&kw=" + HttpUtility.UrlEncode(m_kw);	
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	
	for(; i<end && i<nRows; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		string id = dr["id"].ToString();
		string area = dr["area"].ToString();
		string location = dr["location"].ToString();
		string section = dr["section"].ToString();
		string level = dr["level"].ToString();
		string name = dr["name"].ToString();
		
		Response.Write("<tr class=tn>");
		Response.Write("<td class=tb align=center>" + id + "</td>");
		if(id == m_id) //edit
		{
			Response.Write("<td class=tb><select name=area>" + PrintShelfAreaOptions(area, false) + "</select>");
			Response.Write("<input type=text size=5 name=area_new value=''></td>");
			Response.Write("<td class=tb><select name=location>" + PrintShelfLocationOptions(area,location, false) + "</select>");
			Response.Write("<input type=text size=5 name=location_new value=''></td>");
			Response.Write("<td class=tb><select name=section>" + PrintShelfSectionOptions(area,location, section,false) + "</select>");
			Response.Write("<input type=text size=5 name=section_new value=''></td>");
			Response.Write("<td class=tb><select name=level>" + PrintShelfLevelOptions(level,false) + "</select></td>");
			Response.Write("<td class=tb align=righ nowrapt>");
			Response.Write("<input type=submit name=cmd value='" + Lang("Save") + "' class=b>");
			Response.Write("<input type=button class=b value='" + Lang("Cancel") + "' onclick=\"history.go(-1);\">");
			Response.Write("<input type=button class=b value='" + Lang("Delete") + "' onclick=\"if(!window.confirm('");
			Response.Write(Lang("Are you sure to delete?") + "')){return false;}else{window.location='?id=" + id + "&a=del';}\">");
			Response.Write("</td>");
		}
		else
		{
			Response.Write("<td class=tb align=center>" + area + "</td>");
			Response.Write("<td class=tb align=center>" + location + "</td>");
			Response.Write("<td class=tb align=center>" + section + "</td>");
			Response.Write("<td class=tb align=center>" + level + "</td>");
			Response.Write("<td class=tb align=right nowrap>");
			Response.Write("<input type=button class=b value='" + Lang("Edit") + "' onclick=\"window.location='?t=e&id=" + id + "&kw=" + HttpUtility.UrlEncode(m_kw) + "&p=");
			if(g("p") == "")
				Response.Write("1");
			else
				Response.Write(g("p"));
			Response.Write("&spb=" + g("spb") + "';\">");
			Response.Write("<input type=button class=b value='" + Lang("Stock") + "' onclick=\"window.location='shelf.aspx?id=" + id + "';\">");
			Response.Write("</td>");
		}
		Response.Write("</tr>");
	}
	
	if(m_id == "0")
	{	
		Response.Write("<tr class=ts>");
		Response.Write("<td class=tb align=right>&nbsp;" + Lang("Add New") + ":</td>");
		Response.Write("<td class=tb><select name=area>" + PrintShelfAreaOptions("Eo", false) + "</select>");
		Response.Write("<input type=text size=5 name=area_new value=''></td>");
		Response.Write("<td class=tb><select name=location>" + PrintShelfLocationOptions("","", false) + "</select>");
		Response.Write("<input type=text size=5 name=location_new value=''></td>");
		Response.Write("<td class=tb><select name=section>" + PrintShelfSectionOptions("","","", false) + "</select>");
		Response.Write("<input type=text size=5 name=section_new value=''></td>");
		Response.Write("<td class=tb><select name=level>" + PrintShelfLevelOptions("", false) + "</select></td>");
		Response.Write("<td class=tb><input type=submit name=cmd value='" + Lang("Add") + "' class=b></td>");
		Response.Write("</tr>");
	}

	if(nRows > m_cPI.PageSize)
		Response.Write("<tr><td colspan=4>" + sPageIndex + "</td></tr>");	
	Response.Write("</table></form>");
	return true;
}

bool DoUpdateData(string id)
{
	string area = p("area");
	string area_new = p("area_new");
	if(area_new != "")
		area = area_new;
	string location = p("location");
	string location_new = p("location_new");
	if(location_new != "")
		location = location_new;
	string section = p("section");
	string section_new = p("section_new");
	if(section_new != "")
		section = section_new;
	string level = p("level");
	string name = area + location + section + level;
	
	string sc = "";
	if(id == "") //add new
	{
		sc = " SELECT id FROM shelf ";
		sc += " WHERE 1 = 1 ";
		sc += " AND area = N'" + EncodeQuote(area) + "' ";
		sc += " AND location = N'" + EncodeQuote(location) + "' ";
		sc += " AND section = N'" + EncodeQuote(section) + "' ";
		sc += " AND level = " + MyIntParse(level).ToString() + " ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "check") > 0)
			{
				ErrMsgAdmin("<font color=red><b>" + area + location + section + level + "</b></font> has already been used, please select another one.");
				return false;
			}
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
		
		sc = " BEGIN TRANSACTION ";
		sc += " INSERT INTO shelf (area, location, section, level, name) VALUES(";
		sc += " N'" + EncodeQuote(area) + "' ";
		sc += ", N'" + EncodeQuote(location) + "' ";
		sc += ", N'" + EncodeQuote(section) + "' ";
		sc += ", " + MyIntParse(level).ToString() + " ";
		sc += ", N'" + EncodeQuote(name) + "') ";
		sc += " SELECT IDENT_CURRENT('shelf') AS id ";
		sc += " COMMIT ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "data") <= 0)
			{
				ErrMsgAdmin("getting new id failed");
				return false;
			}
			m_id = ds.Tables["data"].Rows[0]["id"].ToString();
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
	}
	
	if(m_id == "" || m_id == "0")
	{
		ErrMsgAdmin("Invalid id");
		return false;
	}
	
	sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE shelf SET area = N'" + EncodeQuote(area) + "' ";
	sc += ", location = N'" + EncodeQuote(location) + "' ";
	sc += ", section = N'" + EncodeQuote(section) + "' ";
	sc += ", level = " + MyIntParse(level).ToString() + " ";
	sc += ", name = N'" + EncodeQuote(name) + "' ";
	sc += " WHERE id = " + m_id;
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

bool DoDeleteData()
{
	if(m_id == "" || m_id == "0")
		return true;
	
	string sc = " SELECT * FROM shelf_item WHERE shelf_id = " + m_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "data") > 0)
		{
			ErrMsgAdmin("Delete Failed. There are storage on this shelf, please remove item first before delete this shelf");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
		
	sc = " DELETE FROM shelf WHERE id = " + m_id;
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
</script>

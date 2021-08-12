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
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_action == "del")
	{
		if(DoDeleteData())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully deleted, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	PrintMainForm();
}

bool PrintMainForm()
{
	Response.Write("<br><center><h4>" + Lang("Edit Issue") + "</h4></center>");
	Response.Write("<script language=javascript src=../cssjs/calendar30en.js></script");
	Response.Write("><form name=f action=?id=" + m_id + " method=post>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr><td colspan=4 align=right>");
	Response.Write("<input type=text name=kw value='" + m_kw + "'>");
	Response.Write("<input type=submit class=b value='" + Lang("Search") + "'>");
	if(m_kw != "")
		Response.Write("<input type=button value='" + Lang("Show All") + "' class=b onclick=\"window.location='?';\">");
	Response.Write("</td></tr>");
	Response.Write("<tr class=th>");
	Response.Write("<th>ID</th>");
	Response.Write("<th>" + Lang("Issue Number") + "</th>");
	Response.Write("<th>" + Lang("Issue Date") + "</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	string sc = " SELECT i.id, i.issue_number, CONVERT(varchar(50), i.issue_date, 103) AS sdate ";
	sc += " FROM issue i ";
	sc += " WHERE 1 = 1 ";
	if(m_kw != "")
	{
		if(MyIntParse(m_kw) > 0)
			sc += " AND (i.id = " + m_kw + " OR i.issue_number LIKE '%" + EncodeQuote(m_kw) + "%') ";
		else
			sc += " AND (i.issue_number LIKE '%" + EncodeQuote(m_kw) + "%') ";
	}
	sc += " ORDER BY i.issue_date DESC ";
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
		string issue_number = dr["issue_number"].ToString();
		string issue_date = dr["sdate"].ToString();
		
		Response.Write("<tr class=tn>");
		Response.Write("<td class=tb align=center>" + id + "</td>");
		if(id == m_id) //edit
		{
			Response.Write("<td class=tb><input type=text name=issue_number value='" + issue_number + "'></td>");
			Response.Write("<td class=tb><input type=text name=issue_date value='" + issue_date + "' onclick=\"calendar();\"></td>");
			Response.Write("<td class=tb align=right>");
			Response.Write("<input type=submit name=cmd value='" + Lang("Save") + "' class=b>");
			Response.Write("<input type=button class=b value='" + Lang("Cancel") + "' onclick=\"history.go(-1);\">");
			Response.Write("<input type=button class=b value='" + Lang("Delete") + "' onclick=\"if(!window.confirm('");
			Response.Write(Lang("Are you sure to delete?") + "')){return false;}else{window.location='?id=" + id + "&a=del';}\">");
			Response.Write("</td>");
		}
		else
		{
			Response.Write("<td class=tb align=center>" + issue_number + "</td>");
			Response.Write("<td class=tb align=center>" + issue_date + "</td>");
			Response.Write("<td class=tb align=right>");
			Response.Write("<input type=button class=b value='" + Lang("Edit") + "' onclick=\"window.location='?t=e&id=" + id + "';\">");
			Response.Write("<input type=button class=b value='" + Lang("Pages") + "' onclick=\"window.location='epage.aspx?iid=" + id + "';\">");
			Response.Write("</td>");
		}
		Response.Write("</tr>");
	}
	
	if(m_id == "0")
	{	
		Response.Write("<tr class=ts>");
		Response.Write("<td class=tb align=right>&nbsp;" + Lang("Add New") + ":</td>");
		Response.Write("<td class=tb><input type=text name=issue_number></td>");
		Response.Write("<td class=tb><input type=text name=issue_date onclick=\"calendar();\"></td>");
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
	string issue_number = p("issue_number");
	string issue_date = p("issue_date");
	
	string sc = "";
	if(id == "") //add new
	{
		sc = " BEGIN TRANSACTION ";
		sc += " INSERT INTO issue (issue_number) VALUES(N'" + EncodeQuote(issue_number) + "') ";
		sc += " SELECT IDENT_CURRENT('issue') AS id ";
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
	sc += " UPDATE issue SET issue_number = N'" + EncodeQuote(issue_number) + "' ";
	sc += ", issue_date = N'" + EncodeQuote(issue_date) + "' ";
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
	
	string sc = " SELECT count(*) FROM issue_page WHERE issue_id = " + m_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "data") > 0)
		{
			ErrMsgAdmin("Delete Failed. There are pages already been set for this issue, please delete pages first before delete this issue");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
		
	sc = " DELETE FROM issue WHERE id = " + m_id;
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

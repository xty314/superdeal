<script runat=server>

DataSet dst = new DataSet();

string m_sTableName = "settings";
string[] m_aColNames = new string[16];
string[] m_aColTypes = new string[16];
string[] m_aColValues = new string[16];
int m_nColumns = 0;

DataRow[] m_dra = null; 

string m_id = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
	{
		m_sTableName = Request.QueryString["t"];
		Session["etable_table_name"] = m_sTableName;
	}
	else if(Session["etable_table_name"] != null)
		m_sTableName = Session["etable_table_name"].ToString();

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetColumnNames())
		return;
	string cmd = null;
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		cmd = Request.Form["cmd"];
	
	if(cmd == "Add")
	{
		if(DoInsertData())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?id="+ Request.QueryString["id"] +"');</script");
			Response.Write(">\r\n");
			return;
		}
	}
	else if(cmd == "Update")
	{
		if(DoUpdateData())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "\">");
		return;
	}
	if(Request.QueryString["a"] == "new" || m_id != "")
	{
		ShowData();
	}
	else if(Request.QueryString["a"] == "del")
	{
		if(DoDeleteData())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"');</script");
			Response.Write(">\r\n");
		}
		return;
	}
	else
		DoQueryData();
	PrintAdminFooter();
}

bool GetColumnNames()
{
	string sc = " SELECT * FROM " + m_sTableName;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "colnames");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int i = 0;

	//write column names
	DataColumnCollection dc = dst.Tables["colnames"].Columns;
	m_nColumns = dc.Count;
	for(i=0; i<m_nColumns; i++)
	{
		m_aColNames[i] = dc[i].ColumnName;
//DEBUG("c=", m_aColNames[i]);
	}

	//column data type
	for(i=0; i<m_nColumns; i++)
	{
		m_aColTypes[i] = dc[i].DataType.ToString().Replace("System.", "");
	}
	return true;
}

bool ShowData()
{
	if(m_id != "")
	{
		string sc = " SELECT * FROM " + m_sTableName + " WHERE id = " + m_id;
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			if(myCommand1.Fill(dst, "datas") == 1)
			{
				for(int i=0; i<m_nColumns; i++)
				{
					m_aColValues[i] = dst.Tables["datas"].Rows[0][m_aColNames[i]].ToString();
				}
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	Response.Write("<center><h5>Edit Table</h5>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");

	for(int i=0; i<m_nColumns; i++)
	{
		string colName = m_aColNames[i];
		string colType = m_aColTypes[i];
		string colValue = m_aColValues[i];

		if(colName == "id")
			Response.Write("<input type=hidden name=id value=" + colValue + ">");
		else
		{
			Response.Write("<tr><td valign=top><b> " + colName + "</b></td>");
			Response.Write("<td><textarea rows=3 cols=55 name=" + colName + ">" + colValue + "</textarea>");
			Response.Write("</td></tr>");
		}
	}

	Response.Write("<tr><td>");
	Response.Write("<input type=button name=cmd value='List Rows' ");
	Response.Write(" class=b onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");
	Response.Write("<td align=right valign=bottom>");
	if(m_id != "")
	{
		Response.Write("<input type=button name=cmd value=New ");
		Response.Write(" class=b onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");
		Response.Write("<input type=submit name=cmd value=Update class=b>");
	}
	else
	{
		Response.Write("<input type=submit name=cmd value=Add class=b>");
	}
	Response.Write("</td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	Response.Write("</form><br> ");

	return true;
}

bool DoUpdateData()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE " + m_sTableName + " SET ";
	for(int i=0; i<m_nColumns; i++)
	{
		if(m_aColNames[i] == "id")
			continue;

		string v = Request.Form[m_aColNames[i]];
		if(i > 1)
			sc += ",";
		sc += m_aColNames[i] + " = ";
		if(m_aColTypes[i] == "String")
		{
			sc += "'";
			sc += EncodeQuote(v);
			sc += "'";
		}
		else
		{
			sc += v;
		}
	}
	sc += " WHERE id = " + Request.Form["id"];
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
	if(!TSIsDigit(m_id))
		return false;

	string sc = " SET DATEFORMAT dmy ";
	sc += " DELETE FROM " + m_sTableName + " WHERE id = " + m_id;
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

bool DoQueryData()
{
	string sc = " SELECT * FROM " + m_sTableName + " ORDER BY id ";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "datas");
		if(rows <= 0)
		{
	//		Response.Write("<br><br><center><h3>Error getting order items, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<br><center><h5>Edit Table - " + m_sTableName + "</h5>");
	Response.Write("<table width=95% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	bool bAlter = false;

	int i = 0;
	if(m_id != "")
	{
		m_dra = dst.Tables["datas"].Select("id=" + m_id, "");
		int nLength = m_dra.Length;
		if(nLength == 1)
		{
			for(i=0; i<m_nColumns; i++)
			{
				m_aColValues[i] = m_dra[0][m_aColNames[i]].ToString();
			}
		}
	}

	Response.Write("<tr><td colspan=" + m_nColumns + ">");
	Response.Write("<input type=button value='Insert New' onclick=window.location=('?a=new') class=b>");
	Response.Write("</td></tr>");

	Response.Write("<tr align=left bgcolor=#b3ADE>");
	for(i=0; i<m_nColumns; i++)
	{
		Response.Write("<th>" + m_aColNames[i] + "</th>");
	}
	Response.Write("<th>ACTION</td></tr>");
	
	for(int m=0; m<rows; m++)
	{
		DataRow dr = dst.Tables["datas"].Rows[m];
		string id = "";
		for(i=0; i<m_nColumns; i++)
		{
			m_aColValues[i] = dr[m_aColNames[i]].ToString();
			if(m_aColNames[i] == "id")
				id = m_aColValues[i];
		}

		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		Response.Write(">");

		for(i=0; i<m_nColumns; i++)
		{
			Response.Write("<td>" + m_aColValues[i] + "</td>");
		}

		Response.Write("<td>");
		Response.Write("<a title='Edit' href='"+ Request.ServerVariables["URL"] +"?id="+ id +"' class=o>E</a>&nbsp;&nbsp; ");
		Response.Write("<a title='Delete' href='"+ Request.ServerVariables["URL"] +"?a=del&id="+ id +"' class=o><font color=red><b>X</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}

	Response.Write("</table>");
	return true;
}

bool DoInsertData()
{
	int i = 0;
	string sc = " INSERT INTO " + m_sTableName + "(";
	for(i=0; i<m_nColumns; i++)
	{
		if(m_aColNames[i] == "id")
			continue;
		if(i > 1)
			sc += ",";
		sc += m_aColNames[i];
	}

	sc += ") VALUES (";
	
	for(i=0; i<m_nColumns; i++)
	{
		if(m_aColNames[i] == "id")
			continue;

		string v = Request.Form[m_aColNames[i]];
		if(i > 1)
			sc += ",";
		if(m_aColTypes[i] == "String")
		{
			sc += "'";
			sc += EncodeQuote(v);
			sc += "'";
		}
		else
			sc += v;
	}
	sc += ") ";
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

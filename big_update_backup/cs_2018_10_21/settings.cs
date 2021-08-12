<script runat=server>

DataSet dst = new DataSet();
//*************************************************************************************************************************
bool b_edValue = false;
bool b_edDesc = false;
bool b_edName = false;
//bool m_bEZNZAdmin = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("manager"))
		return;

	if(Session["email"].ToString().IndexOf("eznz.com") >= 0)
		m_bEZNZAdmin = true;

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h3>Settings</h3></center>");


	bool b_edit = false;  //make sure that it need to draw the edit table, default set to "NO"
	string editRowid = "";

	if(Request.QueryString["rd"] != null)
	{
		editRowid = Request.QueryString["rd"];
		b_edit = true;
		if(editRowid == "new")
		{
			b_edDesc = true;
			b_edName = true;
			b_edValue = true;
		}
		else
		{
			if(Request.QueryString["e"] == "v")		//edit "value"
				b_edValue = true;
			if(Request.QueryString["e"] == "d")		//edit "description"
				b_edDesc = true;
			if(Request.QueryString["e"] == "n")		//edit "name"
				b_edName = true;
		}
	}

	string sfield = "";

	if(Request.Form["cmd"] != null && Request.Form["cmd"] == "Update")
	{
		if(Request.Form["sNewDesc"] != null)
		{
			sfield = " description ='" + EncodeQuote(Request.Form["sNewDesc"]) + "' ";
		}
		else if(Request.Form["sNewName"] != null)
		{
			sfield = "name ='" + EncodeQuote(Request.Form["sNewName"]) + "' ";
		}
		else if(Request.Form["sNewValue"] != null)
		{
			sfield = "value ='" + EncodeQuote(Request.Form["sNewValue"]) + "' ";
		}
		else
		{
			Response.Write("Update field Error...");
			return;
		}

//		if(Request.Form["confirm"] != "on")
//		{
//			Response.Write("<h3>Error, please tick the checkbox to confirm Updating...</h3>");
//			return;
//		}
//		else
		{
			string sRow_id = Request.Form["sid"];
			if(!DoUpdateSettings(sRow_id, sfield))
			{
				Response.Write("<h3>Error, Updating Data to Setting Talbe ERROR!!!...</h3>");
				return;
			}
			if(Request.Form["name"] != null)
				Session[Request.Form["name"]] = null;
		}
	}
	else if(Request.Form["cmd"] != null && Request.Form["cmd"] == " Add ")
	{
		/*if(Request.Form["confirm"] != "on")
		{
			Response.Write("<h3>Error, please tick the checkbox to confirm Add New...</h3>");
			return;
		}
		else*/
		{
			if(!DoAddNew())
			{
				Response.Write("<h3>Error, Inserting Data into Setting Talbe ERROR!!!...</h3>");
				return;
			}
		}
	}

	if(Request.Form["cmd"] != null && Request.Form["cmd"] == "Cancel")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=settings.aspx\">");
	}

	if(!GetSettings())
	{
		Response.Write("<br>There is no existing setting content...");
		PrintAdminFooter();
		return;
	}

	if(b_edit)
		DrawEditTable(editRowid);

	if(!BindSettingsTable())
	{
		Response.Write("<br>Error binding settings table");
		PrintAdminFooter();
		return;
	}

	PrintAdminFooter();
}

bool GetSettings()
{
	string sc = "SELECT * FROM settings ";
	if(Request.QueryString["t"] == "hidden")
		sc += " WHERE hidden = 1 ";
	else
		sc += " WHERE hidden = 0 ";
	sc += " ORDER BY cat ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "t_settings");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

//*************************************************************************************************************************
bool BindSettingsTable()
{
	DrawTableHeader();

	if(!DrawRows())
	{
		Response.Write("</table>");
		return false;
	}

	return true;
}
//*************************************************************************************************************************
void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:10pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td align=center>Name</td><td align=center>Value</td><td align=center colspan=2>Description</td></tr>\r\n");

	
}
//*************************************************************************************************************************
bool DrawRows()
{
	bool bWhite = false;
	for(int i=0; i<dst.Tables["t_settings"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["t_settings"].Rows[i];
		
		bWhite = !(bWhite);
		string colorit = "bgcolor=#EEEEFF";
		if(bWhite)
			colorit = "";
		Response.Write("<tr " +colorit+ "><td>");
		Response.Write(dr["name"].ToString()+ "</td><td>");
		Response.Write(dr["value"].ToString()+ "</td><td>");
		Response.Write(dr["description"].ToString()+ "</td>");
		Response.Write("<td align=right>");
		Response.Write("<a href=settings.aspx?rd=" +i+"&n=" + dr["id"].ToString()+ "&e=v");
		if(Request.QueryString["t"] == "hidden")
			Response.Write("&t=hidden");
		Response.Write(" class=o>Edit</a></td>");
		Response.Write("</tr>\r\n");
	}

	Response.Write("</table><br><hr size=2>");

	Response.Write("<table width=100%><tr><td align=right>");
	if(m_bEZNZAdmin)
		Response.Write("<input type=button onclick=window.location=('settings.aspx?t=hidden') value='Show Hidden' " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=window.location=('settings.aspx?rd=new') value='Add New' " + Session["button_style"] + ">");
	Response.Write("</td></tr></table>\r\n");
	return true;
//('suppay.aspx?t=payall&id=" + m_supplierID + "')>Pay All</button></td>");
}

//*************************************************************************************************************************
void DrawEditTable(string sRowid)
{	
	string DescLink = "";
	string NameLink = "";
	string ValueLink = "";

	string sDesc = "";
	string sName = "";
	string sValue = "";

	bool bHidden = false;
	if(Request.QueryString["t"] == "hidden")
		bHidden = true;

	Response.Write("<br><br><br><form action=settings.aspx");
	if(Request.QueryString["t"] == "hidden")
		Response.Write("?t=hidden");
	Response.Write(" method=post>\r\n");
	Response.Write("<table align=center width=100% align=left cellspacing=1 cellpadding=0 border=2 bordercolor=#666696 background-color=white>");
//	Response.Write("<tr style=\"color:black;background-color:#EEEEFF;font-weight:bold;\">\r\n");
//	Response.Write("<td colspan=3><b>Edit Record</b></td></tr>");
	
	DataRow dr = null;

	if(sRowid != "new")
	{
		int iRowid = int.Parse(sRowid);
		dr = dst.Tables["t_settings"].Rows[iRowid];
		
		DescLink = "<a href=settings.aspx?rd="+sRowid+"&n="+dr["id"].ToString()+"&e=d><b>Description</b></a>";
		sDesc = dr["description"].ToString();

		NameLink = "<a href=settings.aspx?rd="+sRowid+"&n="+dr["id"].ToString()+"&e=n><b>Name</b></a>";
		sName = dr["name"].ToString();

		ValueLink = "<a href=settings.aspx?rd="+sRowid+"&n="+dr["id"].ToString()+"&e=v><b>Value</b></a>";
		sValue = dr["value"].ToString();
	}
	else
	{
		DescLink = "<b>Description</b>";
		NameLink = "<b>Name</b>";
		ValueLink = "<b>Value</b>";
	}


	//Description (row)
	Response.Write("<tr><td bgcolor=#EEEEFF>" +DescLink+ "</td>");
	if(b_edDesc)
	{
		Response.Write("<td colspan=3><input type=text size=100% name=sNewDesc value='" +sDesc+ "'</td></tr>\r\n");
	}
	else
	{
		Response.Write("<td colspan=3>" +sDesc+ "</td></tr>\r\n");
	}

	//Name (row)
	Response.Write("<tr><td bgcolor=#EEEEFF>" +NameLink+ "</a></td>");
	if(b_edName)
	{
		Response.Write("<td colspan=3><input type=text size=60% name=sNewName value='" +sName+ "'</td></tr>\r\n");
	}
	else
		Response.Write("<td colspan=3>" +sName+ "</td></tr>\r\n");
	Response.Write("<input type=hidden name=name value='" + sName + "'>");

	//Value (row)
	Response.Write("<tr><td bgcolor=#EEEEFF>" +ValueLink+ "</a></td>");
	if(b_edValue)
	{
		Response.Write("<td><textarea name=sNewValue cols=70 rows=3>" + sValue + "</textarea></td>\r\n");
	}
	else
		Response.Write("<td>" +sValue+ "</td>\r\n");
	
	string sbtnName = "Update";
	if(sRowid == "new")
		sbtnName = " Add ";

	Response.Write("</tr>");

	Response.Write("<tr><td>");
	if(m_bEZNZAdmin)
	{
		Response.Write("<input type=checkbox name=is_hidden ");
		if(bHidden)
			Response.Write("checked");
		Response.Write(">Hidden");
	}

	Response.Write("</td><td>");
//	Response.Write("<td colspan=2 align=center><input type=checkbox name=confirm> tick to " +sbtnName);
	Response.Write("<input type=submit name=cmd value='" +sbtnName+ "' " + Session["button_style"] + ">");
	//if(sRowid != "new")
	Response.Write("<input type=hidden name=sid value='"+Request.QueryString["n"]+ "'>");
	Response.Write("<input type=submit name=cmd value='Cancel' " + Session["button_style"] + ">");
	Response.Write("</td></tr>\r\n");
	Response.Write("</table></form>");
}

//*************************************************************************************************************************
bool DoUpdateSettings(string Row_id, string sNew)
{
	string sHidden = "0";
	if(Request.Form["is_hidden"] == "on")
		sHidden = "1";
	string sc = " UPDATE settings SET " + sNew;
//	if(Request.Form["is_hidden"] != null)
		sc += ", hidden = " + sHidden;
	sc += " WHERE id = " + Row_id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Session[Request.Form["sNewName"]] = Request.Form["sNewValue"];
	return true;
}

//*************************************************************************************************************************

bool DoAddNew()
{
	string sc = "INSERT INTO settings (name, value, description) VALUES ";
		   sc+= "('" + Request.Form["sNewName"]+ "', '" +Request.Form["sNewValue"]+ "','" +Request.Form["sNewDesc"]+ "')";

	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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

//*************************************************************************************************************************
</script>
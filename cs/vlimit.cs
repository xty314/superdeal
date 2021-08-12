<script runat=server>
DataSet dst = new DataSet();
DataSet dsi = new DataSet();

void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	string s_cmd = "";
	if(Request.Form["cmd"] != null)
	{
		s_cmd = Request.Form["cmd"];
		Trim(ref s_cmd);
	}

	if(s_cmd == "Add")
	{
		if(DoAddNewGroup())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?" + "\">");
		return;
	}
	else if(s_cmd =="Modify")
	{
		if(UpdateGroup())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?" + "\">");
		return;
	}
	else if(s_cmd =="Delete")
	{
		if(DeleteGroup())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?" + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><hr3><center><b><font size=+1>View Limit Group</font></b></center></h3>");
	Response.Write("<table width=90%  align=center valign=top cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td width=60% align=center valign=top>");
	LoadExistingGroups();
	Response.Write("</td><td width=40% align=center valign=top>");

	PrintOneGroup();

	Response.Write("</td></tr></table>");

	Response.Write("<br><br>");
	Response.Write("<b> * Rules : Limit can only be sub categories or brands, use comma as seperator.<br>");
	Response.Write(" * Entered sub categories and brands( Plus manully entered limits on Edit Card Page) will be excluded from dealer view<br>");
	Response.Write(" * However, you can change this to only these categories and brands can be seen by add a word \"not\" into dealer_view_limit on Edit Card Page.<br>");
	Response.Write(" * Final Note, changes wont be effected until dealer's next login.<br><br>");
	Response.Write(" * Examples : <br>");
	Response.Write("<table border=1><tr><td><b>Limit</b></td><td><b>Means</b></td></tr>");
	Response.Write("<tr><td>printer</td><td>dealer won't be able to see print sub category</td></tr>");
	Response.Write("<tr><td>printer, cpu</td><td>dealer won't be able to see print and cpu sub category</td></tr>");
	Response.Write("<tr><td>printer, intel</td><td>dealer won't be able to see print sub category and all Intel products(limited by brand)</td></tr>");
	Response.Write("<tr><td>printer (*and add not to dealer_view_limit manually*)</td><td>dealer can only see print sub category(nothing else)</td></tr>");
	Response.Write("</table>");

	PrintAdminFooter();
}

bool DoAddNewGroup()
{
	string sgroup = Request.Form["sgroup"];
	string limit = Request.Form["limit"];
	if(sgroup == null || sgroup == "")
	{
		Response.Write("<br><center><h3>Error, group name cant be blank</h3>");
		return false;
	}
	if(sgroup.Length >= 49)
		sgroup = sgroup.Substring(0, 49);
	if(limit.Length >= 1023)
		limit = limit.Substring(0, 1023);

	string sc = " IF NOT EXISTS(SELECT * FROM view_limit WHERE sgroup='" + EncodeQuote(sgroup) + "') ";
	sc += " INSERT INTO view_limit (sgroup, limit) VALUES( ";
	sc += "'" + EncodeQuote(sgroup) + "' ";
	sc += ", '" + EncodeQuote(limit) + "' ";
	sc += ") ";
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

bool UpdateGroup()
{
	string id = Request.Form["id"];
	string sgroup = Request.Form["sgroup"];
	string limit = Request.Form["limit"];
	if(sgroup.Length >= 49)
		sgroup = sgroup.Substring(0, 49);
	if(limit.Length >= 1023)
		limit = limit.Substring(0, 1023);

	if(id == null || id == "")
	{
		Response.Write("<br><center><h3>Error, no ID</h3>");
		return false;
	}

	string sc = "UPDATE view_limit SET ";
	sc += " sgroup='" + EncodeQuote(sgroup) + "' ";
	sc += ", limit = '" + EncodeQuote(limit) + "' ";
	sc += " WHERE id = " + id;
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

bool DeleteGroup()
{
	string id = Request.Form["id"];
	if(id == null || id == "")
	{
		Response.Write("<br><center><H3>Error, no id");
		return false;
	}

	string sc = "DELETE FROM view_limit WHERE id = " + id;
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

void LoadExistingGroups()
{
	if(!GetExistingGroups())
		return;
	
	Response.Write("<table width=100% valign=top cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
//	Response.Write("<th width=5%>ID</th>");
	Response.Write("<th nowrap>Group</th>");
	Response.Write("<th>Limit(No Access)</th>");
	Response.Write("</tr>\r\n");

	for(int i=0; i<dst.Tables["groups"].Rows.Count;i++)
	{
		DataRow dr = dst.Tables["groups"].Rows[i];
		string id = dr["id"].ToString();
		string sgroup = dr["sgroup"].ToString();
		string limit = dr["limit"].ToString();
		Response.Write("<tr>");
		Response.Write("<td align=left><a href=?t=m&i=" + id + " class=o Title='Click To Edit'>" + sgroup + "</a></td>");
//		Response.Write("<td align=left>" + sgroup + "</td>");
		Response.Write("<td align=left>" + limit + "</td>");
		Response.Write("</tr>\r\n");
	}
	Response.Write("</table>");

	return;
}

void PrintOneGroup()
{
	string id = "";
	string sgroup = "";
	string limit = "";

	string s_TblName = "Add";

	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "")
	{
		s_TblName = "Modify";
		if(Request.QueryString["i"] != null && Request.QueryString["i"] != "")
		{
			if(!GetSelectedRow())
				return;
			id = dsi.Tables["selectedgroup"].Rows[0]["id"].ToString();
			sgroup = dsi.Tables["selectedgroup"].Rows[0]["sgroup"].ToString();
			limit = dsi.Tables["selectedgroup"].Rows[0]["limit"].ToString();
		}
	}
	Response.Write("<form name=frmAdd method=post action=vlimit.aspx>");
	Response.Write("<table width=95% valign=top align=right cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 ");
	Response.Write(" style=\"border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<td colspan=2 align=center><b>" + s_TblName + " Group</b></td></tr>");
/*	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD><b>ID:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=id value='");
	Response.Write(id + "'>");
	Response.Write("</td></tr>\r\n");
*/
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD><b>Group:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=sgroup value='");
	Response.Write(sgroup + "'></td></tr>\r\n");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.frmAdd.sgroup.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Limit(No Access):</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=limit value='");
	Response.Write(limit + "'></td></tr>\r\n");

	Response.Write("<tr><td colspan=2 bgcolor=#FFFFFF align=center><br>");
	Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' " + s_TblName + " ' "+ Session["button_style"] +">");

	Response.Write("&nbsp;&nbsp;&nbsp");
	Response.Write("<input type=button style='font-size:8pt;font-weight:bold' name=clear value=' Cancel '");
	Response.Write(" "+ Session["button_style"] +" OnClick=window.location=('vlimit.aspx')>");

	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "")
	{
		Response.Write("&nbsp;&nbsp;&nbsp");
		Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' Delete ' "+ Session["button_style"] +">");
		Response.Write("<input type=hidden name=id value='" + Request.QueryString["i"] + "'>");
	}
	
	Response.Write("</td></tr>");
	Response.Write("</table></form>");

	return;
}

bool GetSelectedRow()
{
	string sc = "SELECT * FROM view_limit WHERE id = " + Request.QueryString["i"];
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi, "selectedgroup");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows != 1)
		return false;

	return true;
}

bool GetExistingGroups()
{
	string sc = "SELECT * FROM view_limit ORDER BY id";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "groups");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>
<!-- #include file="kit_fun.cs" -->

<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_type = "cpu";
string m_parent = "";
string m_cols = "2";
string m_set = null;

void Page_Load(Object Src, EventArgs E )
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		m_type = Request.QueryString["t"];

	if(Request.QueryString["set"] != null)
	{
		m_set = Request.QueryString["set"];
	}
//DEBUG("m_set=", m_set);
	if(m_set == null)
	{
		Response.Write("Error, no parent code.");
		return;
	}
	
	if(!DoSearch())
		return;

	if(Request.QueryString["a"] == "update")
	{
		if(DoUpdate())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&set=" + m_set + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.Form["cmd"] != null)
	{
		string cmd = Request.Form["cmd"];
		if(cmd == "Add")
		{
			if(m_type == "ram" || m_type == "video")
			{
				string set = Request.Form["new_set"];
				if(set != null && set != "")
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&set=" + set + "&r=" + DateTime.Now.ToOADate() + "\">");
			}
			else if(DoAddParent(Request.Form["code"]))
			{
//				m_set = Request.Form["code"];
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&set=" + m_set + "&r=" + DateTime.Now.ToOADate() + "\">");
			}
		}
		else if(cmd == "Delete")
		{
			if(Request.Form["check_del"] == "on")
			{
				if(DoDeleteParent(m_set))
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&set=" + m_set + "&r=" + DateTime.Now.ToOADate() + "\">");
			}
			else
			{
				Response.Write("<h3>Error, tick the check box to confirm deletion</h3>");
			}
		}
		return;
	}

	TS_PageLoad(); //do common things, LogVisit etc...

	PrintAdminHeader();
	PrintAdminMenu();

	string desc = GetProductDesc(m_set);
	Response.Write("<br><center>");
	Response.Write("<a href=liveedit.aspx?code=" + m_set + " class=d target=_blank>");
	Response.Write("<font size=+1><b>" + desc + "</b></font></a></center><br>");

	BindGrid();
	PrintAddNewField();
	LFooter.Text = m_sAdminFooter;
}

bool QExists(string code, string tableName)
{
//DEBUG("tableName=", tableName);
	if(ds.Tables.Count <= 0)
		return false;
//	if(ds.Tables[tableName] == null)
//		return false;

	for(int i=0; i<ds.Tables[tableName].Rows.Count; i++)
	{
		DataRow dr = ds.Tables[tableName].Rows[i];
//		if(tableName == "cpu")
//		{
//DEBUG("code="+dr["code"].ToString(), "parent="+dr["parent"].ToString());
//			if(dr["parent"].ToString() == code && dr["code"].ToString() == m_set)
//				return true;
//		}
//		else
//		{
//DEBUG("dr.code="+dr["code"].ToString(), " dr.parent="+dr["parent"].ToString() + " code="+code + " parent="+m_set);
			if(dr["code"].ToString() == code && dr["parent"].ToString() == m_set)
				return true;
//		}
	}
	return false;
}

bool DoSearch()
{
	if(ds != null)
		ds.Clear();

	if(m_type == "cpu")
	{
		if(!GetSet("mb"))
			return false;
	}
	else if(m_type == "mb")
	{
		if(!GetSet("cpu"))
			return false;
		if(!GetSet("ram"))
			return false;
		if(!GetSet("video"))
			return false;
	}
	else if(m_type == "ram")
	{
		if(!GetSet("ram_mb"))
			return false;
	}
	else if(m_type == "video")
	{
		if(!GetSet("video_mb"))
			return false;
	}
	return true;
}

bool GetSet(string name)
{
	string sc = "";
	int rows = 0;
	if(name == "mb")
	 	sc = "SELECT DISTINCT q.code, parent=CASE q.parent WHEN " + m_set + " THEN " + m_set + " END, p.name FROM q_mb q LEFT OUTER JOIN product p ON q.code=p.code ORDER BY p.name, parent DESC";
	else if(name == "cpu")
		sc = "SELECT DISTINCT q.parent AS code, parent=CASE q.code WHEN " + m_set + " THEN " + m_set + " END, p.name FROM q_mb q LEFT OUTER JOIN product p ON q.parent=p.code ORDER BY p.name, parent DESC";
	else if(name == "ram_mb") //get mb from ram table
		sc = "SELECT DISTINCT q.code, parent = CASE r.code WHEN " + m_set + " THEN " + m_set + " END, p.name FROM q_mb q LEFT OUTER JOIN q_ram r ON r.parent = q.code LEFT OUTER JOIN product p ON q.code=p.code ORDER BY p.name, parent DESC";
	else if(name == "video_mb") //get mb from ram table
		sc = "SELECT DISTINCT q.code, parent = CASE r.code WHEN " + m_set + " THEN " + m_set + " END, p.name FROM q_mb q LEFT OUTER JOIN q_video r ON r.parent = q.code LEFT OUTER JOIN product p ON q.code=p.code ORDER BY p.name";
	else
	{
		sc = "SELECT DISTINCT q.code, parent=CASE q.parent WHEN " + m_set + " THEN " + m_set + " END, p.name ";
		sc += " FROM q_" + name + " q LEFT OUTER JOIN product p ON q.code=p.code ORDER BY p.name, parent DESC";
	}
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(m_type == "ram")
			rows = myCommand.Fill(ds, "ram");
		else if(m_type == "video")
			rows = myCommand.Fill(ds, "video");
		else
			rows = myCommand.Fill(ds, name);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintAddNewField()
{
	string key = "cpu";
	if(m_type == "cpu")
		key = "mb";
	else if(m_type == "ram")
		key = "ram";
	else if(m_type == "video")
		key = "video";
	string keys = GetNewOptions(key);
	if(keys == "error")
		return false;
	StringBuilder sb = new StringBuilder();
	sb.Append("<form action=sqmi.aspx?t=" + m_type + "&set=" + m_set + " method=post>");
	sb.Append("<table align=center><tr><td>");
	sb.Append("<input type=checkbox name=check_del> Delete This " + m_type);
	sb.Append(" <input type=submit name=cmd value='Delete' " + Session["button_style"] + "></td></tr>");
	if(m_type == "cpu")
	{
		sb.Append("<tr><td><select name=code>");
		sb.Append(keys);
		sb.Append("</select> ");
		sb.Append("<input type=submit name=cmd value='Add' " + Session["button_style"] + ">");
		sb.Append("</td></tr>");
	}
	else if(m_type == "mb")
	{
		sb.Append("<tr><td><select name=code>");
		sb.Append(keys);
		sb.Append("</select> ");
		sb.Append("<input type=submit name=cmd value='Add' " + Session["button_style"] + ">");
		sb.Append("</td></tr>");
	}
/*	else if(m_type == "ram" || m_type == "video")
	{
		sb.Append("<tr><td><select name=new_set>");
		sb.Append(keys);
		sb.Append("</select> ");
		sb.Append("<input type=submit name=cmd value='Add' " + Session["button_style"] + ">");
		sb.Append("</td></tr>");
	}
*/
	sb.Append("</table>");
	sb.Append("</form>");
	sb.Append("<center><a href=sqm.aspx class=o>Main Setup Page</a>");
	LAddNewButton.Text = sb.ToString();
	return true;
}

string GetNewOptions(string sKey)
{
	DataSet dso = new DataSet();
	int rows = 0;
	StringBuilder sb = new StringBuilder();
	
	string sc = GetNewOptionsKey(sKey);
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "error";
	}
	if(rows <= 0)
		return "";

	bool bSelected = false;
	string code = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dso.Tables[0].Rows[i];
		code = dr["code"].ToString();
//		if(m_parent == "")
//		{
			if(QOptionExists(code))
				continue;
//		}
		sb.Append("<option value='" + code + "'");
		if(!bSelected)
		{
			if(m_set != null)
			{
				if(m_set == code)
				{
					sb.Append(" selected");
					bSelected = true;
				}
			}
			else
			{
				sb.Append(" selected");
				bSelected = true;
			}
		}
		double b_rate = double.Parse(dr["rate"].ToString());
	   double level_r1 = double.Parse(dr["level_rate1"].ToString());
	   double level_pr1 = double.Parse(dr["manual_cost_nzd"].ToString());
	          b_rate = Math.Round(b_rate, 6);
			  level_r1 = Math.Round(level_r1, 6);
			  level_pr1 = Math.Round(level_pr1, 2);
 	double sqm_lprice = b_rate * level_r1 * level_pr1;
		      sqm_lprice = Math.Round(sqm_lprice, 2);
		//sb.Append(">" + dr["name"].ToString() + " $" + dr["price"].ToString() + "</option>");
		sb.Append(">" + dr["name"].ToString() + " $" + sqm_lprice + "</option>");
	}
	if(sKey == "video")
		sb.Append("<option value=-1>" + m_sNONE + "</option>");
	return sb.ToString();
}

bool QOptionExists(string code)
{
	if(ds.Tables.Count <= 0)
		return false;
	for(int i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		if(dr["code"].ToString() == code)
			return true;
	}
	return false;
}

bool DoAdd(string code, string tableName)
{
	if(QExists(code, tableName))
	{
//DEBUG("already exists, code="+code, " m_set="+m_set + " table="+tableName);
		return true;
	}
//DEBUG("added, code="+code, " m_set="+m_set + " table="+tableName);
//return true;
	string sc = "";
	if(m_type == "ram" || m_type == "video")
	{
		sc = "INSERT INTO q_" + tableName + " (code, parent) VALUES(" + m_set + ", " + code + ")";
	}
	else
	{
		if(tableName == "mb" || tableName == "ram" || tableName == "video")
			sc = "INSERT INTO q_" + tableName + " (parent, code) VALUES(" + m_set + ", " + code + ")";
		else if(tableName == "cpu")
			sc = "INSERT INTO q_mb (parent, code) VALUES(" + code + ", " + m_set + ")";
		else
			sc = "INSERT INTO q_flat (" + tableName + ") VALUES(" + code + ")";
	}
//DEBUG("sc=", sc);
//return true;
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

bool DoDelete(string code, string tableName)
{
	if(!QExists(code, tableName))
	{
//DEBUG("NO DEL not exists, code="+code, " m_set="+m_set + " table="+tableName);
		return true;
	}
//DEBUG("deleted, code="+code, " m_set="+m_set + " table="+tableName);
//return true;
	string sc = "";
	if(m_type == "ram" || m_type == "video")
	{
			sc = "DELETE FROM q_" + tableName + " WHERE code=" + m_set + " AND parent=" + code;
	}
	else
	{
		if(tableName == "mb" || tableName == "ram" || tableName == "video")
			sc = "DELETE FROM q_" + tableName + " WHERE code=" + code + " AND parent=" + m_set;
		else if(tableName == "cpu")
			sc = "DELETE FROM q_mb WHERE code=" + m_set + " AND parent=" + code;
		else
			sc = "DELETE FROM q_flat WHERE " + tableName + "=" + code;
	}
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

bool DoAddParent(string code)
{
	if(code == null || code == "")
		return true;

	string tableName = "mb";
	if(m_type == "mb")
		tableName = "cpu";
	else if(m_type == "ram")
		tableName = "ram";
	else if(m_type == "video")
		tableName = "video";
	if(QExists(code, tableName))
	{
//DEBUG("already exists, code="+code, " m_set="+m_set + " table="+tableName);
		return true;
	}
//return true;
	string sc = "";
	if(tableName == "mb" || tableName == "ram" || tableName == "video")
		sc = "INSERT INTO q_" + tableName + " (parent, code) VALUES(" + m_set + ", " + code + ")";
	else if(tableName == "cpu")
		sc = "INSERT INTO q_mb (parent, code) VALUES(" + code + ", " + m_set + ")";
//	else if(tableName == "ram")
//		sc = "INSERT INTO q_ram (parent, code) VALUES(" + code + ", " + m_set + ")";
//	else if(tableName == "video")
//		sc = "INSERT INTO q_video (parent, code) VALUES(" + code + ", " + m_set + ")";
	else
		sc = "INSERT INTO q_flat (" + tableName + ") VALUES(" + code + ")";
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

bool DoDeleteParent(string code)
{
	string sc = "";
	if(m_type == "cpu")
		return DoDeleteParent(code, "mb");
	else if(m_type == "mb")
	{
		if(!DoDeleteParent(code, "ram"))
			return false;
		if(!DoDeleteParent(code, "video"))
			return false;
		if(!DoDeleteAllChild(code, "mb"))
			return false;
	}
	else if(m_type == "ram")
	{
		if(!DoDeleteAllChild(code, "ram"))
			return false;
	}
	else if(m_type == "video")
	{
		if(!DoDeleteAllChild(code, "video"))
			return false;
	}
	return true;
}

bool DoDeleteParent(string code, string table)
{
	string sc = "";
	sc = "DELETE FROM q_" + table + " WHERE parent=" + code;
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

bool DoDeleteAllChild(string code, string table)
{
	string sc = "";
	sc = "DELETE FROM q_" + table + " WHERE code=" + code;
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

bool UpdateOneTable(string tableName)
{
	int qty = int.Parse(Request.Form["qty_" + tableName]);
	for(int i=0; i<qty; i++)
	{
		string code = Request.Form["code_" + tableName + i.ToString()];
		if(code == null || code == "")
			continue;
		bool bChecked = (Request.Form["check_" + tableName + i.ToString()] == "on");
//DEBUG("code="+m_code, "checked=" + bChecked.ToString());
		if(bChecked)
		{
			if(!DoAdd(code, tableName))
				return false;
		}
		else
		{
			if(!DoDelete(code, tableName))
				return false;
		}
	}
	return true;
}

bool DoUpdate()
{
	if(m_type == "cpu")
	{
		return UpdateOneTable("mb");
	}
	else if(m_type == "mb")
	{
		if(!UpdateOneTable("cpu"))
			return false;
		if(!UpdateOneTable("ram"))
			return false;
		if(!UpdateOneTable("video"))
			return false;
		if(Request.Form["cpus"] != "1")
			SetCpusMBNeeds(m_set, Request.Form["cpus"]);
	}
	else if(m_type == "ram")
	{
		if(!UpdateOneTable("ram"))
			return false;
	}
	else if(m_type == "video")
	{
		if(!UpdateOneTable("video"))
			return false;
	}
	return true;
}

void PrintOneTable(string tableName)
{
	bool bAlterColor = false;
//	bool bSkipOne = false; //no duplicate entries, can't make a better select statment, see Search()
	string name = "";
	string code = "";
	string code_old = "";
	int qty = ds.Tables[tableName].Rows.Count;
	
	Response.Write("<input type=hidden name=qty_" + tableName + " value=" + qty.ToString() + ">");
	Response.Write("<table align=center border=1 cellspacing=0 cellpadding=0 bordercolor=#EEEEEE ");
	Response.Write("style=border-width:1px;font-family:Verdana;font-size:8pt;border-collapse:collapse;fixed>");
	
	for(int i=0; i<qty; i++)
	{
		DataRow dr = ds.Tables[tableName].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");

		name = dr["name"].ToString();
		code = dr["code"].ToString();
		if(code == code_old)
			continue;
		code_old = code;

		Response.Write("<td><input type=hidden name=code_" + tableName + i.ToString() + " value=" + code + ">");
		Response.Write("<input type=checkbox name=check_" + tableName + i.ToString());
		if(dr["parent"].ToString() == m_set)
		{
			Response.Write(" checked");
//			bSkipOne = true;
		}
		Response.Write("></td>");
		if(name == null || name == "")
		{
			if(tableName == "cpu" || tableName == "ram")
			{
				string sc = "";
				if(tableName == "cpu")
					sc = "DELETE FROM q_mb WHERE code=" + code + " AND parent=" + m_set;
				else
					sc = "DELETE FROM q_" + tableName + " WHERE parent=" + code;
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
					return;
				}
				name = "NOT FOUND, record deleted";
			}
			else
				Response.Write("<td>" + m_sNONE + "</td><td> </td>");
		}
		else
		{
			Response.Write("<td>");
/*			if(tableName == "cpu")
			{
				Response.Write("<a href=sqmi.aspx?t=mb&set="+code+"&r="+DateTime.Now.ToOADate()+ " class=d>");
				else if(m_type == "mb")
					Response.Write("<a href=sqmi.aspx?t=cpu&set="+code+"&r="+DateTime.Now.ToOADate()+ " class=d>");
				else
					Response.Write("<a href=p.aspx?" + code + " class=d target=_blank>");
			}
*/			
			if(tableName == "cpu" || tableName == "mb" || tableName == "ram" || tableName == "video")
				Response.Write("<a href=sqmi.aspx?t=" + tableName + "&set="+code+"&r="+DateTime.Now.ToOADate()+ " class=d>");
			else
				Response.Write("<a href=p.aspx?" + code + " class=d target=_blank>");
			Response.Write(name + "</a></td>");
//			Response.Write("<td align=right>" + double.Parse(dr["price"].ToString()).ToString("c") + "</td>");
		}
		Response.Write("</tr>");
//		if(bSkipOne)
//		{
//			bSkipOne = false;
//			i++; //the next one is the same code, do skip
//		}
	}
	Response.Write("</table>");
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	Response.Write("<form action=sqmi.aspx?a=update&t=" + m_type + "&set=" + m_set + " method=post>");
	Response.Write("<table align=center border=1 cellspacing=1 cellpadding=1 bordercolor=#EEEEEE ");
	Response.Write("style=border-width:1px;font-family:Verdana;font-size:8pt;border-collapse:collapse;fixed>");
	
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
//	Response.Write("<td> </td>");
	string title = "MainBoard";
	if(m_type == "mb")
		title = "CPU";
	Response.Write("<td align=center><b>" + title + "</b></td>");
	if(m_type == "mb")
	{
		Response.Write("<td align=center><b>RAM</b></td>");
		Response.Write("<td align=center><b>VIDEO Cards</b></td>");
	}
	Response.Write("</tr>");

	Response.Write("<tr>");
	if(m_type == "mb")
	{
		Response.Write("<td valign=top>");
		PrintOneTable("cpu");
		Response.Write("</td><td valign=top>");
		PrintOneTable("ram");
		Response.Write("</td><td valign=top>");
		PrintOneTable("video");
	}
	else if(m_type == "cpu")
	{
		Response.Write("<td valign=top>");
		PrintOneTable("mb");
	}
	else if(m_type == "ram")
	{
		Response.Write("<td valign=top>");
		PrintOneTable("ram");
	}
	else if(m_type == "video")
	{
		Response.Write("<td valign=top>");
		PrintOneTable("video");
	}
	Response.Write("</td>");
	Response.Write("</tr>");
	if(m_type == "mb")
	{
		Response.Write("<tr><td colspan=3>This Motherboard needs <input type=text style=text-align:right size=1 name=cpus value=");
		Response.Write(GetCpusMBNeeds(m_set));
		Response.Write("> CPU</td></tr>");
	}
	Response.Write("<tr><td");
	if(m_type == "mb")
		Response.Write(" colspan=3");
	Response.Write(" align=right><input type=submit name=cmd value=Update " + Session["button_style"] + "></td></tr>");
	Response.Write("</table></form>");
}

bool SetCpusMBNeeds(string code, string cpus)
{
	string sc = "IF EXISTS (SELECT cpus FROM q_mb_cpus WHERE code=" + code + ") ";
	sc += " UPDATE q_mb_cpus SET cpus=" + cpus + " WHERE code=" + code;
	sc += " ELSE INSERT INTO q_mb_cpus (code, cpus) VALUES(" + code + ", " + cpus + ")";
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

<asp:Label id=LAddNewButton runat=server/>
<asp:Label id=LFooter runat=server/>
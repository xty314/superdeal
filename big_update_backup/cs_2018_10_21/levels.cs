<script runat=server>
DataSet dst = new DataSet();

string m_card_id = "";

void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
		m_card_id = Request.QueryString["ci"];
	if(m_card_id == "")
	{
		if(Request.Form["ci"] != null && Request.Form["ci"] != "")
			m_card_id = Request.Form["ci"];
	}
	if(m_card_id == "") //still no card id
	{
		PrintCardSelectionForm();
		return;
	}

	string s_cmd = "";
	if(Request.Form["cmd"] != null)
	{
		s_cmd = Request.Form["cmd"];
		Trim(ref s_cmd);
	}

	if(s_cmd == "Add")
	{
		if(DoAddNewLevel())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?ci=" + m_card_id + "\">");
		return;
	}
	else if(s_cmd =="Save")
	{
		if(UpdateLevels())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?ci=" + m_card_id + "\">");
		return;
	}
	else if(Request.QueryString["t"] =="del")
	{
		if(DeleteOneLevel())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?ci=" + m_card_id + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	ListLevels();
	PrintAdminFooter();
}

void PrintCardSelectionForm()
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h3>Category Dependent Price Levels</h3>");
	Response.Write("<form name=f action=? method=post>");
	Response.Write("<h4>Enter card id : <input type=text size=5 name=ci>");
	Response.Write("<input type=submit value=GO class=b>");
	Response.Write("</form>");
	Response.Write("<script");
	Response.Write(">document.f.ci.focus();</script");
	Response.Write(">");
	PrintAdminFooter();
}

bool ListLevels()
{
	int rows = 0;
	string sc = " SELECT * FROM dealer_levels WHERE card_id = " + m_card_id + " ORDER BY cat ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "levels");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = GetCardData(m_card_id);
	if(dr == null)
	{
		Response.Write("<h3>Invalid Card ID");
		return false;
	}

	string cname = dr["trading_name"].ToString();
	if(cname == "")
		cname = dr["company"].ToString();
	cname += " (";
	cname += dr["name"].ToString();
	cname += ")";

	string default_level = dr["dealer_level"].ToString();

	Response.Write("<form action=? method=post>");
	Response.Write("<input type=hidden name=ci value=" + m_card_id + ">");
	Response.Write("<input type=hidden name=rows value=" + rows + ">");

	Response.Write("<br><center><h4>Price Levels For <font color=blue>" + cname + "</font></h4>");
	Response.Write("<font size=+1 color=red><b>Default Level : " + default_level + "</b></font>");
	Response.Write("<table valign=top cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<th width=90 nowrap>Category</th>");
	Response.Write("<th width=55>Level</th>");
	Response.Write("<th width=99>&nbsp;</th>");
	Response.Write("</tr>\r\n");
	
	int i = 0;
	for(; i<rows; i++)
	{
		dr = dst.Tables["levels"].Rows[i];
		string id = dr["id"].ToString();
		string cat = dr["cat"].ToString();
		string level = dr["level"].ToString();

		Response.Write("<tr>");
		Response.Write("<td>" + cat + "</td>");
		Response.Write("<td>");
		Response.Write("<select name=level" + i.ToString() + ">");
		PrintLevelOptions(level);
		Response.Write("</select>");
		Response.Write("<input type=hidden name=level" + i.ToString() + "old value=" + level + ">");
		Response.Write("<input type=hidden name=id" + i.ToString() + " value=" + id + ">");
		Response.Write("</td>");
		Response.Write("<td align=right>");
		Response.Write("<a href=?ci=" + m_card_id + "&t=del&id=" + id + " class=o>Reset</a> ");
		Response.Write("</td></tr>");
	}
	
//	Response.Write("<tr>&nbsp;<td colspan=3><b>Add New</b></td></tr>");
	if(rows > 0)
		Response.Write("<tr><td colspan=3 align=right><input type=submit name=cmd value=Save class=b></td></tr>");

//	Response.Write("<tr><td colspan=3>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=3><b>Add New</b></td></tr>");
	Response.Write("<tr>");
	Response.Write("<td>");
	PrintExistsCats();
	Response.Write("</td>");
	Response.Write("<td>");
	Response.Write("<select name=level_new>");
	PrintLevelOptions("1");
	Response.Write("</select>");
	Response.Write("</td>");
	Response.Write("<td align=right><input type=submit name=cmd value=Add class=b></td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	Response.Write("</form>");

	Response.Write("<center><b>* Note : Brand levels overwrite categories.</b>");
	return true;
}

bool PrintLevelOptions(string current_level)
{
	int levels = MyIntParse(GetSiteSettings("dealer_levels", "3"));
	if(levels > 9)
		levels = 9;

	for(int i=1; i<=levels; i++)
	{
		Response.Write("<option value=" + i.ToString());
		if(current_level == i.ToString())
			Response.Write(" selected");
		Response.Write(">");
		Response.Write("Level " + i.ToString());
		Response.Write("</option>");
	}
	return true;
}

bool PrintExistsCats()
{
	int rows = 0;
	string sc = " SELECT DISTINCT cat + ' - ' + s_cat AS cat FROM catalog ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cats");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<select name=cat>");
	for(int i=0; i<rows; i++)
	{
		string cat = dst.Tables["cats"].Rows[i]["cat"].ToString();
		Response.Write("<option value='" + cat + "'>" + cat + "</option>");
	}
	Response.Write("</select>");
	return true;
}

bool DoAddNewLevel()
{
	string cat = Request.Form["cat"];
	string slevel = Request.Form["level_new"];

	string sc = " IF NOT EXISTS ";
	sc += " (SELECT * FROM dealer_levels WHERE card_id = " + m_card_id + " AND cat='" + EncodeQuote(cat) + "') ";
	sc += " INSERT INTO dealer_levels (card_id, cat, level) VALUES( ";
	sc += m_card_id;
	sc += ", '" + EncodeQuote(cat) + "' ";
	sc += ", " + slevel;
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

bool UpdateLevels()
{
	int rows = 0;
	if(Request.Form["rows"] != null && Request.Form["rows"] != "")
		rows = MyIntParse(Request.Form["rows"]);

	string sc = "";
	for(int i=0; i<rows; i++)
	{
		string id = Request.Form["id" + i];
		string level = Request.Form["level" + i];
		string level_old = Request.Form["level" + i + "old"];

		if(level == level_old)
			continue;
		if(id == null || id == "")
			continue;

		sc += " UPDATE dealer_levels SET ";
		sc += ", level = " + level;
		sc += " WHERE id = " + id;
	}
	if(sc == "")
		return true;

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

bool DeleteOneLevel()
{
	string id = Request.QueryString["id"];
	if(id == null || id == "")
	{
		Response.Write("<br><center><H3>Error, no id");
		return false;
	}

	string sc = "DELETE FROM dealer_levels WHERE id = " + id;
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
</script>
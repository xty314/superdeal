<script runat=server>
//==============================
//Created date: 31 March 2004
//Author: Vincent Tsui
//Purpose: Update site page functions, make it more efficient and dynamic 
//==============================

string m_type = "";
string m_cat = "";
string m_name = "";
string m_hcat = ""; //http encoded
string m_hname = ""; //http encoded

//save current value
string m_id = "";
string m_sid = ""; //sub_pages ID
string m_desc = "";
string m_text = "";
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
		
	m_id = g("id");
	m_sid = g("sid");
	m_type = g("t");
	m_cat = g("c");
	m_name = g("n");
	string cmd = Request.Form["cmd"];
	m_hcat = HttpUtility.UrlEncode(m_cat);
	m_hname = HttpUtility.UrlEncode(m_name);

	if(m_type == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=siteoption.aspx?c=" + m_hcat + "&n=" + m_hname + "\">");
		return;
	}
	else if(cmd == "Save")
	{
		if(DoSave())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=siteoption.aspx?sid=" + m_sid + "&c=" + m_hcat + "&n=" + m_hname + "\">");
		return;
	}
	else if(cmd == "Update")
	{
		if(DoUpdate())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=siteoption.aspx?t=edit&sid=" + m_sid + "&c=" + m_cat + "&n=" + m_name + "\">");
		return;
	}
	else if(m_type == "use")
	{
		if(DoUse())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=siteoption.aspx?sid=" + m_sid + "&c=" + m_cat + "&n=" + m_name + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(m_type == "new")
		PrintNewForm();
	else if(m_type == "edit")
		PrintEditForm();
	else
	{
		if(!GetAllSubPages())
			return;
		PrintSubPageList();
	}

	PrintAdminFooter();
}

bool GetAllSubPages()
{
	int nRows = 0;
	string sc = " SELECT s.* ";
	sc += " FROM site_sub_pages s, site_pages p ";
	sc += " WHERE s.id = p.id ";
	if(m_cat != "")
		sc += " AND p.cat = '" + EncodeQuote(m_cat) + "' ";
	if (m_name != "")
		sc += " AND p.name = '" + EncodeQuote(m_name) + "' ";
	sc += " ORDER BY s.kid ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "allpages");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 1) //only has default
	{
		if(!DoCopyDefault())
			return false;
		//query again
		dst.Tables["allpages"].Clear();
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			nRows = myAdapter.Fill(dst, "allpages");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}

bool DoCopyDefault()
{
	if(dst.Tables["allpages"] == null)
		return false;
	if(dst.Tables["allpages"].Rows.Count <= 0)
		return false;

	DataRow dr = dst.Tables["allpages"].Rows[0];
	string id = dr["id"].ToString();
	string text = dr["text"].ToString();
	string sc = " INSERT INTO site_sub_pages (id, text, description) VALUES(";
	sc += id;
	sc += ", '" + EncodeQuote(text) + "', 'customer default copy')";
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

void PrintSubPageList()
{
	Response.Write("<center><h3>Site Page Options</h3>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1  style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\" height=\"68\">");
	Response.Write("<tr><td nowrap align=left colspan=5>");
	Response.Write("<select onchange=\"window.location=('siteoption.aspx?c='+this.options[this.selectedIndex].value)\">");
	Response.Write(getCategory() +"</select>");
	Response.Write("<select onchange=\"window.location=('siteoption.aspx?c=" + HttpUtility.UrlEncode(m_cat));
	Response.Write("&n='+this.options[this.selectedIndex].value)\">");
	Response.Write(getName() +"</select></td></tr>");
	
	Response.Write("<tr style=\"color:white;background-color:#666696\">"); 
	Response.Write("<th width=\"80\">&nbsp&nbsp;</th>");
	Response.Write("<th width=\"130\">Category&nbsp&nbsp;</th>");
	Response.Write("<th width=\"190\">Name&nbsp&nbsp;</th>");
	Response.Write("<th width=\"150\">Description</th>");
	Response.Write("<th width=\"110\">ACTION &nbsp&nbsp;</th></tr>");

	for(int i=0; i<dst.Tables["allpages"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["allpages"].Rows[i];
		string sid = dr["kid"].ToString();
		string desc = dr["description"].ToString();
		string use = dr["inuse"].ToString();
		
		if(MyBooleanParse(use))
			Response.Write("<tr bgcolor=lightgreen><td><font color=red><b>" + Lang("In Use") + "</b></font></td>");
		else
			Response.Write("<tr><td>&nbsp;</td>");
		Response.Write("<td width=\"130\" height=\"14\">" + m_cat + "</td>");
		Response.Write("<td width=\"190\" height=\"14\">" + m_name + "</td>");
		Response.Write("<td width=\"150\" height=\"14\">" + desc + "</td>");

		PrintActions(sid, use, desc);
	}	

	Response.Write("</tr><tr><td colspan=5 align=right>");
	//add new button
	Response.Write("<input type=button onClick=window.location=('siteoption.aspx?t=new");
	Response.Write("&c=" + HttpUtility.UrlEncode(m_cat) + "&n=" + HttpUtility.UrlEncode(m_name));
	Response.Write("')  "+ Session["button_style"] +"  value='Add New' name=\"button\">");
	//go back edit site page button
	Response.Write("<input type=button onClick=window.location=('editpage.aspx')  "+ Session["button_style"] +"  value='Edit Page' name=\"button\">");
	Response.Write("</tr></table><br><center><b><font color=red size=+1> </font></b></center></center>");
}

void PrintActions(string sid, string use, string desc)
{
	bool bEZNZAdmin = false;
	if(Session["email"].ToString().IndexOf("@eznz") > 0)
		bEZNZAdmin = true;
	Response.Write("<td align=right nowrap>");
	//CONFIRM script begin
	Response.Write("\r\n<Script");
	Response.Write(">\r\n");
	Response.Write("function confirmdel(sid){\r\n");
	Response.Write("	answer = confirm(\"Do you really want to Delete?\")\r\n");
	Response.Write("	if (answer == 1){\r\n");
	Response.Write("		location=\"siteoption.aspx?t=del&c=" + HttpUtility.UrlEncode(m_cat) + "&n=" + HttpUtility.UrlEncode(m_name) + "&sid=\"+sid ;\r\n");
	Response.Write("	}\r\n");
	Response.Write("}\r\n");
	Response.Write("\r\n</Script");
	Response.Write(">\r\n");
	//script end
	
	//check which one is using

	if(use != "True")
	{
		if(desc != "Default" || bEZNZAdmin)
		{
			Response.Write("<a href=siteoption.aspx?t=edit&sid="+ sid +"&c=" + m_hcat);
			Response.Write("&n=" + m_hname + "&desc=" + HttpUtility.UrlEncode(desc) + " class=o> EDIT</a> "); 
		}
		else if(desc == "Default")
		{
			Response.Write("<a href=siteoption.aspx?t=edit&sid="+ sid +"&c=" + m_hcat);
			Response.Write("&n=" + m_hname + "&desc=" + HttpUtility.UrlEncode(desc) + " class=o> View</a> "); 
		}
		Response.Write("<a href=siteoption.aspx?t=use&sid=" + sid + "&c="+ m_hcat + "&n=" + m_hname + " class=o> USE</a> ");
//		Response.Write("<a href=\"#\" Onclick=\"confirmdel();\" class=o> DEL</a>"); 
		if(!MyBooleanParse(use))
			Response.Write("<a href=\"#\" Onclick=\"confirmdel("+ sid +");\" class=o> DEL</a>"); 
	}
	else
	{
		if(desc != "Default" || bEZNZAdmin)
		{
			Response.Write("<a href=siteoption.aspx?t=edit&sid=" + sid + "&c=" + m_hcat + "&n=" + m_hname);
			Response.Write("&desc=" + HttpUtility.UrlEncode(desc) + " class=o> EDIT</a> "); 
		}
		if(bEZNZAdmin && !MyBooleanParse(use))
			Response.Write("<a href=\"#\" Onclick=\"confirmdel("+ sid +");\" class=o> DEL</a>"); 
	}
}

bool DoUse()
{
	if(m_sid == "")
		return false;

	string txt = loadcontent(m_sid);

	string sc = "UPDATE site_sub_pages SET inuse=0 ";
	sc += " WHERE id = (SELECT id FROM site_sub_pages WHERE kid = " + m_sid + ") ";
	sc += " Update site_sub_pages SET inuse=1 ";
	sc += " WHERE kid = " + m_sid;
	sc += " UPDATE site_pages SET text = '" + EncodeQuote(txt) + "' ";
	sc += " WHERE id = (SELECT id FROM site_sub_pages WHERE kid = " + m_sid + ") ";
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

bool DoUpdate()
{
	string cat = Request.Form["cat"];
	string name = Request.Form["name"];
	string cat_old = Request.Form["cat_old"];
	string name_old = Request.Form["name_old"];
	string txt = Request.Form["Optxtfield"];
	string desc = Request.Form["desc"];

	string sc = " UPDATE site_sub_pages ";
	sc += " SET text = N'" + EncodeQuote(txt) + "' ";
	sc += ", description = N'" + EncodeQuote(desc) + "' ";
	sc += " WHERE kid = " + m_sid;
	sc += " IF (SELECT inuse FROM site_sub_pages WHERE kid = " + m_sid + ") = 1 ";
	sc += " BEGIN ";
	sc += " UPDATE site_pages SET text = N'" + EncodeQuote(txt) + "' WHERE id = ";
	sc += " (SELECT id FROM site_sub_pages WHERE kid = " + m_sid + ") ";
	sc += " END ";

	if(cat != cat_old || name != name_old)
	{
		sc += " UPDATE site_pages SET ";
		sc += " cat = '" + EncodeQuote(cat) + "' ";
		sc += ", name = '" + EncodeQuote(name) + "' ";
		sc += " WHERE id = (SELECT id FROM site_sub_pages WHERE kid = " + m_sid + ") ";
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

string getIDbyCAT(string name, string cat)
{
	if(dst.Tables["getID"] != null)
		dst.Tables["getID"].Clear();

	string sc = "SELECT id FROM site_pages ";
	sc += " WHERE name = '" + EncodeQuote(name) + "' ";
	sc += " AND cat = '" + EncodeQuote(cat) + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "getID") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return dst.Tables["getID"].Rows[0]["id"].ToString();
}

bool DoSave()
{
	string txt = Request.Form["Optxtfield"];
	string desc = Request.Form["desc"];
	string id = getIDbyCAT(m_name, m_cat);

	string sc = " INSERT INTO site_sub_pages (id, text, description) ";
	sc += " VALUES (" + id + ", N'" + EncodeQuote(txt) + "', N'" + EncodeQuote(desc) + "') ";
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

bool DoDelete()
{
	if(m_sid == "")
		return false;

	string sc = "DELETE FROM site_sub_pages WHERE kid = " + m_sid; 
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

string GetDefaultPage(string id)
{
	if(dst.Tables["default_page"] != null)
		dst.Tables["default_page"].Clear();

	string sc = " SELECT text FROM site_pages WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "default_page") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return dst.Tables["default_page"].Rows[0]["text"].ToString();
}

string loadcontent(string id)
{
	if(dst.Tables["content"] != null)
		dst.Tables["content"].Clear();

	string sc = " SELECT description, text FROM site_sub_pages WHERE kid = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "content") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	DataRow dr = dst.Tables["content"].Rows[0];
	m_desc = dr["description"].ToString();
	return dr["text"].ToString();
}

string getCategory()
{
	if(dst.Tables["category"] != null)
		dst.Tables["category"].Clear();

	string sc = " SELECT cat FROM site_pages sp GROUP BY sp.cat ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "category");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	string output = "<option value=>--ALL--</option>";

	for(int i=0; i < dst.Tables["category"].Rows.Count; i++)
	{
		string outcat = dst.Tables["category"].Rows[i]["cat"].ToString();
		if (outcat == m_cat)
			output += "<option value='"+outcat+"' selected>" + outcat + "</option>";
		else
			output += "<option value='"+outcat+"'>" + outcat + "</option>";
	}
	return output;
}

string getName()
{
	if(dst.Tables["name"] != null)
		dst.Tables["name"].Clear();

	string sc = " SELECT name FROM site_pages sp WHERE sp.cat = '" + EncodeQuote(m_cat) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "name");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	string output = "<option value=>--SELECT--</option>";
	output += "<option value=>--ALL--</option>";

	for(int i=0; i < dst.Tables["name"].Rows.Count; i++)
	{
		string name = dst.Tables["name"].Rows[i]["name"].ToString();
		if(name == m_name)
			output += "<option value='"+name+"' selected>"+name+"</option>";
		else
			output += "<option value='"+name+"'>"+name+"</option>";
	}
	return output;
}


void PrintNewForm()
{
	Response.Write("<form name=\"form\" method=\"post\" action=\"siteoption.aspx?c=" + m_hcat + "&n=" + m_hname + "\">");
	
	Response.Write("<center><h5>" + m_name + "</h5>");

	Response.Write("<table align=center valign=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td><b>Desc : </b><input type=text name=desc value='" + m_desc + "'></td></tr>");
	Response.Write("<tr><td><textarea id='textarea1' rows=25 cols=110 name=Optxtfield></textarea></td></tr>");
	
	Response.Write("<tr><td align=right>");
	Response.Write("<input type=submit name=cmd value=Save class=b>");
    Response.Write("<input type=button value=Back onclick=window.location=('siteoption.aspx?c=" + m_hcat + "&n=" + m_hname + "') class=b>");
    Response.Write("</td></tr></table></form>");
}

void PrintEditForm()
{
	bool bEZNZAdmin = false;
	if(Session["email"].ToString().IndexOf("@eznz") > 0)
		bEZNZAdmin = true;

	Response.Write("<form name=\"form\" method=\"post\" action=\"siteoption.aspx?t=edit&sid=" + m_sid + "&c=" + m_hcat + "&n=" + m_hname + "\">");

	Response.Write("<input type=hidden name=cat_old value='" + m_cat + "'>");
	Response.Write("<input type=hidden name=name_old value='" + m_name + "'>");

	string txt = loadcontent(m_sid);
	Response.Write("<center><h5>" + m_name + "</h5>");

	Response.Write("<table align=center valign=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>Category</b></td>");
	Response.Write("<td>");
	Response.Write("<select name=cat onchange=\"window.location=('siteoption.aspx?t=" + m_type + "&sid=" + m_sid + "&c='+this.options[this.selectedIndex].value)\">");
	Response.Write(getCategory());
	Response.Write("</select>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Name</b></td>");
	Response.Write("<td>");
	Response.Write("<select name=name>");
	Response.Write(getName());
	Response.Write("</select>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Desc</b></td><td><input type=text name=desc value='" + m_desc + "'></td></tr>");
	Response.Write("<tr><td colspan=2><textarea id='textarea1' rows=25 cols=110 name=Optxtfield >" + HttpUtility.HtmlEncode(txt) + "</textarea>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2 align=right>");
	if(m_desc == "Default" && !bEZNZAdmin)
		Response.Write("<b>(Default version, View Only)</b>");
	else
		Response.Write("<input type=submit name=cmd value=Update class=b>");
    Response.Write("<input type=button value=Back onclick=window.location=('siteoption.aspx?c=" + m_hcat + "&n=" + m_hname + "') class=b>");
    Response.Write("</td></tr></table></form>");
}

</script>
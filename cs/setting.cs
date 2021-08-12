<script runat=server>

DataSet dst = new DataSet();
//bool m_bEZNZAdmin = false;
string m_cat = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("manager"))
		return;

	if(Session["email"].ToString().IndexOf("eznz.com") >= 0)
		m_bEZNZAdmin = true;

	if(Request.QueryString["t"] == "e")
	{
		if(Request.Form["cmd"] == "Save")
		{
			if(DoSaveSetting())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=setting.aspx?t=e");
				if(Request.QueryString["c"] != null)
					Response.Write("&c=" + HttpUtility.UrlEncode(m_cat));
				if(Request.QueryString["id"] != null)
					Response.Write("&id=" + HttpUtility.UrlEncode(Request.QueryString["id"]));
				Response.Write("\">");
			}
		}
		else if(Request.Form["cmd"] == "Delete")
		{
			if(DoDeleteSetting())
			{
				PrintAdminHeader();
				Response.Write("<br><center><h5>Setting entry deleted.</h5>");
				Response.Write("<a href=close.htm class=o><f5>Close Window</f5></a>");
			}
		}
		else
		{
			PrintEditForm();
		}
		return;
	}

	if(!GetAllSettings())
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h4>Settings</h4>");

	PrintList();
	PrintAdminFooter();
}

bool GetAllSettings()
{
	string sc = " SELECT id, cat, name, value, description, hidden FROM settings ORDER BY hidden, cat, name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "settings");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintList()
{
	Response.Write("<table cellspacing=0 cellpadding=0>");

	string cat_old = "";
	bool bAlterColor = false;
	Response.Write("<tr>");
	for(int i=0; i<dst.Tables["settings"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["settings"].Rows[i];
		string id = dr["id"].ToString();
		string cat = dr["cat"].ToString();
		string name = dr["name"].ToString();
		string value = dr["value"].ToString();
		string description = dr["description"].ToString();
		bool bHidden = MyBooleanParse(dr["hidden"].ToString());
		if(bHidden && !m_bEZNZAdmin)
			continue;

		if(cat == "")
			cat = "others";
		if(cat != cat_old)
		{
			Response.Write("<tr><td colspan=3>");
			Response.Write("<a href=setting.aspx?t=e&c=" + HttpUtility.UrlEncode(cat) + " class=o title='Click to Edit' target=_blank>");
			Response.Write("<font size=+1 color=red><b>" + cat + "</b></font></td></tr>");
			cat_old = cat;
			bAlterColor = false;
		}
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td><a href=setting.aspx?t=e&id=" + id + " title='" + description + "' target=_blank>");
		if(bHidden)
			Response.Write("<font color=green>" + name + "</font>");
		else
			Response.Write(name);
		Response.Write("</a></td>");
		Response.Write("<td>");
		Response.Write(value);
		Response.Write("</td>");
		Response.Write("<td><a href=setting.aspx?t=e&id=" + id + " title='" + description + "' class=o target=_blank>Edit </a></td>"); 
		
		Response.Write("</tr>");
	}
	Response.Write("</tr></table>");
}

void PrintEditForm()
{
	string cat = "";
	string id = "";
	if(Request.QueryString["c"] != null)
		cat = Request.QueryString["c"];
	if(Request.QueryString["id"] != null)
		id = Request.QueryString["id"];

	if(cat != "")
		PrintEditCatForm(cat);
	else
		PrintEditSettingForm(id);
}

void PrintEditCatForm(string cat)
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<center><br><h3>Edit Category Name</h3>");
	Response.Write("<form action=setting.aspx?t=e&c=" + HttpUtility.UrlEncode(cat) + " method=post>");

	Response.Write("<h5>" + cat + "</h5>");
	Response.Write("<b>New Value : </b><input type=text name=cat><br>");
	Response.Write("<input type=submit name=cmd value=Save class=b>");
	Response.Write("</form>");
}

bool PrintEditSettingForm(string id)
{
	if(id == "")
		return false;

	string sc = " SELECT * FROM settings WHERE id = " + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "setting") <= 0)
		{
			Response.Write("not found");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = dst.Tables["setting"].Rows[0];
	string cat = dr["cat"].ToString();
	string name = dr["name"].ToString();
	string value = dr["value"].ToString();
	string description = dr["description"].ToString();
	bool bHidden = MyBooleanParse(dr["hidden"].ToString());
	bool bBool = MyBooleanParse(dr["bool_value"].ToString());

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<center><h4>Edit Setting</h4>");
	Response.Write("<form action=setting.aspx?t=e&id=" + id + " method=post>");

	Response.Write("<table>");
	Response.Write("<tr><td><b>Cat : </b></td><td>");
	PrintCatOptions(cat);
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Name : </b></td><td>");
	Response.Write("<input type=text name=name size=40 value='" + name + "'>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Value : </b></td><td>");
	if(bBool)
	{
		Response.Write("<input type=radio name=value value=1 ");
		if(MyBooleanParse(value))
			Response.Write(" checked");
		Response.Write(">Yes ");
		Response.Write("<input type=radio name=value value=0 ");
		if(!MyBooleanParse(value))
			Response.Write(" checked");
		Response.Write(">No");
	}
	else
	{
		Response.Write("<input type=text name=value size=40 value='" + value + "'>");
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td><b>Description : </b></td><td>");
	Response.Write("<textarea name=description rows=5 cols=40>" + description + "</textarea>");
	Response.Write("</td></tr>");

	if(m_bEZNZAdmin)
	{
		Response.Write("<tr><td><b>Boolean : </b></td><td>");
		Response.Write("<input type=checkbox name=bool_value ");
		if(bBool)
			Response.Write(" checked");
		Response.Write(">");
		Response.Write("</td></tr>");

		Response.Write("<tr><td><b>Hidden : </b></td><td>");
		Response.Write("<input type=checkbox name=hidden ");
		if(bHidden)
			Response.Write(" checked");
		Response.Write(">");
		Response.Write("</td></tr>");
	}
	
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Save class=b>");
	if(m_bEZNZAdmin)
		Response.Write("<input type=submit name=cmd value=Delete onclick=\"if(!confirm('Are you sure to delete this settings?')){return false;}\" class=b>");
	Response.Write("</td></tr>");

	Response.Write("</table></form>");
	return true;
}

bool PrintCatOptions(string current)
{
	string sc = " SELECT DISTINCT cat FROM settings ORDER BY cat ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "cats");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<select name=cat>");
	for(int i=0; i<dst.Tables["cats"].Rows.Count; i++)
	{
		string cat = dst.Tables["cats"].Rows[i]["cat"].ToString();
		Response.Write("<option value='" + cat + "'");
		if(cat == current)
			Response.Write(" selected");
		Response.Write(">" + cat + "</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=text name=cat_new size=10 value=''>(enter new value here)");
	return true;
}

bool DoSaveSetting()
{
	string cat = "";
	string id = "";
	if(Request.QueryString["c"] != null)
		cat = Request.QueryString["c"];
	if(Request.QueryString["id"] != null)
		id = Request.QueryString["id"];

	string sc = "";
	if(cat != "")
	{
		string new_cat = Request.Form["cat"];
		m_cat = new_cat;
		sc = " UPDATE settings SET cat = '" + EncodeQuote(new_cat) + "' WHERE cat = '" + EncodeQuote(cat) + "' ";
	}
	else if(id != "")
	{
		string name = Request.Form["name"];
		string new_cat = Request.Form["cat"];
		if(Request.Form["cat_new"] != "")
			new_cat = Request.Form["cat_new"];
		string value = "0";
		if(MyBooleanParse(Request.Form["bool_value"]))
		{
			if(MyBooleanParse(Request.Form["value"]))
				value = "1";
		}
		else
		{
			value = Request.Form["value"];
		}
		string description = Request.Form["description"];
		string hidden = "0";
		if(MyBooleanParse(Request.Form["hidden"]))
			hidden = "1";
		string bool_value = "0";
		if(MyBooleanParse(Request.Form["bool_value"]))
			bool_value = "1";
		sc = " UPDATE settings SET cat = '" + EncodeQuote(new_cat) + "' ";
		sc += ", name = '" + EncodeQuote(name) + "' ";
		sc += ", value = '" + EncodeQuote(value) + "' ";
		sc += ", description = '" + EncodeQuote(description) + "' ";
		if(m_bEZNZAdmin)
		{
			sc += ", hidden = " + hidden;
			sc += ", bool_value = " + bool_value;
		}
		sc += " WHERE id = " + id;
	}
	
	if(sc == "")
	{
		Response.Write("sth wrong");
		return false;
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

bool DoDeleteSetting()
{
	string id = Request.QueryString["id"];
	if(id == "")
	{
		Response.Write("no id");
		return false;
	}
	string sc = " DELETE FROM settings where id = " + id;
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
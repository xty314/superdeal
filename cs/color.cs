<script runat=server>

string m_type = "";
string[] cn = new string[64];
string[] cc = new string[64];
int m_colors = 0;

string m_id = "";
string m_name = "";
string m_note = "";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	GetColorNames();

	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		if(!GetColorSet(m_id))
			m_id = "";
	}

	if(m_type == "edit_name")
	{
		PrintEditColorNameForm();
	}
	else if(m_type == "new") //add new
	{
		PrintColorsForm();
	}
	else if(m_type == "e")
	{
		PrintColorsForm();
	}
	else if(m_type == "u")
	{
		SetSiteSettings("color_set_in_use", Request.QueryString["id"]);
		Session["color_set"] = Request.QueryString["id"];
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=color.aspx\">");
		return;
	}
	else if(Request.Form["cmd"] == "Save")
	{
		string type = Request.Form["form_type"];
	
		if(type == "edit_name")
		{
			DoSaveColorNames();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=color.aspx");
			Response.Write("\">");
			return;
		}
		else
		{
			DoSaveColorSet();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=color.aspx?id=" + Request.Form["id"] + "");
			Response.Write("&t=e");
			Response.Write("\">");
			return;
		}
	
	/*	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=color.aspx?id=" + Request.Form["id"] + "");
	
			Response.Write("&t=e");
		Response.Write("\">");
		*/
		return;
	}
	else
	{
		PrintColorSetList();
	}
	PrintAdminFooter();
}

bool GetColorNames()
{
	string sc = " SELECT * FROM color_name ORDER BY id";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "cn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int i = 0;
	for(i=0; i<dst.Tables["cn"].Rows.Count; i++)
	{
		string name = dst.Tables["cn"].Rows[i]["name"].ToString();
		if(name == "")
			break;
		cn[i] = name;
	}
	m_colors = i;
	return true;
}

bool PrintColorSetList()
{
	string sc = " SELECT * FROM color_set ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "set");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	string currentID = GetSiteSettings("color_set_in_use", "1");
	Response.Write("<center><br><h3>Color Sets</h3>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td align=right colspan=5><b>Current in Use : <font color=red>" + currentID + "</font></b></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696\">");
	Response.Write("<th>ID &nbsp&nbsp;</th>");
	Response.Write("<th>NAME &nbsp&nbsp;</th>");
	Response.Write("<th>DESC &nbsp&nbsp;</th>");
	Response.Write("<th>COLORS &nbsp&nbsp;</th>");
	Response.Write("<th>ACTION &nbsp&nbsp;</th>");
	Response.Write("</tr>");

	bool bAlterColor = false;
	for(int i=0; i<dst.Tables["set"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["set"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string note = dr["note"].ToString();
		string designer = dr["designer"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td>" + id + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td>" + note + "</td>");

		//colors
		Response.Write("<td>");
		Response.Write("<table cellspacing=0 cellpadding=0 border=0>");
		Response.Write("<tr>");
		for(int c=0; c<m_colors; c++)
		{
			string color = dr["c" + c.ToString()].ToString();
			Response.Write("<td width=10 bgcolor=" + color + ">&nbsp;</td>");
		}
		Response.Write("</tr></table>");
		Response.Write("</td>");

		Response.Write("<td>");
		Response.Write("<a href=color.aspx?t=e&id=" + id + " class=o>EDIT</a> ");
		if(id != currentID)
		{
			Response.Write("<a href=color.aspx?t=u&id=" + id + " class=o>USE</a> ");
			Response.Write("<a href=color.aspx?t=d&id=" + id + " class=o>DEL</a> ");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
	}

	Response.Write("<tr>");
	Response.Write("<td colspan=5 align=right>");
	Response.Write("<input type=button onclick=window.location=('color.aspx?t=new') " + Session["button_style"] + " value='Add New'>");
	Response.Write("</tr>");
	Response.Write("</table>");

	Response.Write("<br><center><b><font color=red size=+1> " + m_colors + " </font> Color Names <a href=color.aspx?t=edit_name class=o>Edit Color Name</a></b>");
	return true;
}

bool GetColorSet(string id)
{
	string sc = " SELECT * FROM color_set WHERE id=" + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "cs") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	DataRow dr = dst.Tables["cs"].Rows[0];
	m_name = dr["name"].ToString();
	m_note = dr["note"].ToString();
	for(int i=0; i<m_colors; i++)
	{
		cc[i] = dr["c" + i.ToString()].ToString();
	}
	return true;
}

bool PrintColorsForm()
{
	Response.Write("<form action=color.aspx method=post>");
//	Response.Write("<input type=hidden name=form_type value='edit");

	string title = "New Color Set";
	if(m_id != "")
	{
		title = "Edit Color Set";
		Response.Write("<input type=hidden name=id value=" + m_id + ">");
	}
	
	Response.Write("<center><br><h3>" + title + "</h3>");

	Response.Write("<table width=500 align=center cellspacing=0 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696\">");
	Response.Write("<th>ID</th>");
	Response.Write("<th>NAME</th>");
	Response.Write("<th>VALUE</th>");
	Response.Write("<th>REF</th>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td><b>SET_NAME</b></td>");
	Response.Write("<td><input type=text size=20 name=name value='" + m_name + "' maxlength=49></td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td><b>DESCRIPTION</b></td>");
	Response.Write("<td><input type=text size=20 name=note value='" + m_note + "' maxlength=49></td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("</tr>");

	for(int i=0; i<m_colors; i++)
	{
		Response.Write("<tr>");
		Response.Write("<td align=center><b>Color " + i.ToString() + "</b></td>");
		Response.Write("<td><b>" + cn[i] + "</b></td>");
		Response.Write("<td><input type=text size=20 name=c" + i.ToString() + " value='" + cc[i] + "'></td>");
		Response.Write("<td width=70 align=center>");
		Response.Write("<table cellspacing=0 cellpadding=0 bgcolor=" + cc[i] + "><tr><td width=50>&nbsp;</td></tr></table>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	
	Response.Write("<tr><td colspan=4 align=right>");
	
	Response.Write("<input type=submit name=cmd value='Save' " + Session["button_style"] + ">");
	Response.Write("<input type=button name=cmd value='Color Menu' " + Session["button_style"] + " onclick=\"window.location=('"+ Request.ServerVariables["URL"]+"')\">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	return true;
}

void PrintEditColorNameForm()
{
	Response.Write("<form action=color.aspx method=post>");
	Response.Write("<input type=hidden name=form_type value=edit_name>");
	
	Response.Write("<center><br><h3>Edit Color Name</h3>");

	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696\">");
	Response.Write("<th>Color_ID</th>");
	Response.Write("<th>Color_Name</th>");
//	Response.Write("<th>Color_Desc</th>");
	Response.Write("</tr>");

	for(int i=0; i<20; i++)
	{
		Response.Write("<tr>");
		Response.Write("<td align=center><b>" + i + "</b></td>");
		Response.Write("<td><input type=text size=50 name=name" + i.ToString() + " maxlength=49 value='" + cn[i] + "'></td>");
//		Response.Write("<td><input type=text name=desc" + i.ToString() + " maxlength=49 value=");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Save " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
}

bool DoSaveColorNames()
{
	string sc = "";
	for(int i=0; i<20; i++)
	{
		if(Request.Form["name" + i] == null || Request.Form["name" + i] == "")
			break;
		sc += " UPDATE color_name SET name = '" + EncodeQuote(Request.Form["name" + i]) + "' WHERE id=" + i + " ";
	}
//DEBUG("sc=", sc);
//Response.End();
	if(sc != "")
	{
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
	}
	return true;
}

bool DoSaveColorSet()
{
	string sc = "";
	string id = "";
	int i = 0;
	if(Request.Form["id"] != null)
	{
		id = Request.Form["id"];
		sc = " UPDATE color_set SET ";
		sc += " name = '" + EncodeQuote(Request.Form["name"]) + "' ";
		sc += ", note = '" + EncodeQuote(Request.Form["note"]) + "' ";
		sc += ", designer = '" + Session["name"].ToString() + "' ";
		for(i=0; i<m_colors; i++)
		{
			sc += ", c" + i.ToString() + " = '" + EncodeQuote(Request.Form["c" + i]) + "' ";
		}
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
	}
	else
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO color_set (name, note, designer ";
		for(i = 0; i<m_colors; i++)
		{
			sc += ", c" + i.ToString() + " ";
		}
		sc += " ) VALUES( ";
		sc += " '" + EncodeQuote(Request.Form["name"]) + "' ";
		sc += ", '" + EncodeQuote(Request.Form["note"]) + "' ";
		sc += ", '" + Session["name"].ToString() + "' ";
		for(i=0; i<m_colors; i++)
		{
			sc += ", '" + EncodeQuote(Request.Form["c" + i]) + "' ";
		}
		sc += ") SELECT IDENT_CURRENT('color_set') AS id ";
		sc += " COMMIT ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dst, "id") == 1)
			{
				m_id = dst.Tables["id"].Rows[0]["id"].ToString();
				return true;
			}
			return false;
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}

</script>

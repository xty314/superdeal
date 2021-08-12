<script runat=server>

DataSet ds = new DataSet();
string m_user = "";
string m_id = "";
string m_cat = "";
int m_notes = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	m_user = Session["card_id"].ToString();
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];
	if(Request.QueryString["c"] != null && Request.QueryString["c"] != "")
		m_cat = Request.QueryString["c"];

	string cmd = "";
	string action = "";
	if(Request.Form["cmd"] != null)
		cmd = Request.Form["cmd"];
	if(Request.QueryString["a"] != null && Request.QueryString["a"] != "")
		action = Request.QueryString["a"];

	if(action == "color")
	{
		if(DoChangeColor())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=notepad.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}
	if(action == "del")
	{
		if(DoDeleteNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=notepad.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}
	else if(cmd == "Save")
	{
		if(DoAddNewNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=notepad.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}
	else if(cmd == "Update")
	{
		if(DoUpdateNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=notepad.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}

	PrintAdminHeader();
	PrintAdminMenu();

	GetNotes();
	DisplayNotes();
	PrintAdminFooter();
}

string BuildParameter()
{
	return BuildParameter("");
}

string BuildParameter(string con)
{
	string s = "";
	if(m_id != "" && con != "noid")
		s += "&id=" + m_id;
	if(m_cat != "")
		s += "&c=" + HttpUtility.UrlEncode(m_cat);
	return s;
}

bool DoAddNewNote()
{
	string text = Request.Form["text"];
	string cat = Request.Form["cat"];
	if(Request.Form["cat_new"] != "")
		cat = Request.Form["cat_new"];
	string sc = " INSERT INTO notepad (card_id, cat, text) VALUES( ";
	sc += m_user;
	sc += ", '" + EncodeQuote(cat) + "' ";
	sc += ", '" + EncodeQuote(text) + "' ";
	sc += " ) ";
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

bool DoUpdateNote()
{
	string text = Request.Form["text"];
	string cat = Request.Form["cat"];
	if(Request.Form["cat_new"] != "")
		cat = Request.Form["cat_new"];
	string id = Request.Form["id"];
	string sc = " UPDATE notepad SET ";
	sc += " cat = '" + EncodeQuote(cat) + "' ";
	sc += ", text = '" + EncodeQuote(text) + "' ";
	sc += " WHERE id = " + id;
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

string GetCatOptions(string current)
{
	if(ds.Tables["cat"] != null)
		ds.Tables["cat"].Clear();

	string sc = " SELECT DISTINCT cat FROM notepad WHERE card_id = " + m_user;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	for(int i=0; i<ds.Tables["cat"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["cat"].Rows[i];
		string cat = dr["cat"].ToString();
		if(cat == "")
			cat = "Uncataloged";
		s += "<option value='" + cat + "'";
		if(cat == current)
			s += " selected";
		s += ">" + cat + "</option>";
	}
	return s;
}

bool GetNotes()
{
	string sc = "SELECT * FROM notepad ";
	sc += " WHERE card_id=" + m_user;
	if(m_cat != "deleted")
	{
		sc += " AND deleted=0 ";
		if(m_cat != "")
			sc += " AND cat='" + EncodeQuote(m_cat) + "' ";
	}
	else
		sc += " AND deleted=1 ";

	sc += " ORDER BY date DESC";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_notes = myCommand.Fill(ds, "notes");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void DisplayNotes()
{
	Response.Write("<br><center><h3>Notepad - " + Session["name"] + "</h3>");
	Response.Write("<table width=90% align=center border=1 cellspacing=1 cellpadding=1 bordercolor=#888888 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td><b>Catalog : </b>");
	Response.Write("<select name=cat onchange=\"window.location=('notepad.aspx?c=' + this.options[this.selectedIndex].value)\">");
	Response.Write("<option value=''>All</option>");
	Response.Write(GetCatOptions(m_cat));
	Response.Write("<option value=deleted");
	if(m_cat == "deleted")
		Response.Write(" selected");
	Response.Write(">Deleted</option>");
	Response.Write("</select>");
	Response.Write("</td></tr>");
	string enote = "";
	string cmd = "Save";
	string cat = "";
	for(int i=0; i<m_notes; i++)
	{
		DataRow dr = ds.Tables["notes"].Rows[i];
		string id = dr["id"].ToString();
		string color = dr["color"].ToString();
		string text = dr["text"].ToString();
		string time = DateTime.Parse(dr["date"].ToString()).ToString("dd-MMM-yy HH:mm");
		if(id == m_id)
		{
			enote = text;
			cat = dr["cat"].ToString();
			cmd = "Update";
		}
		text = text.Replace("\r\n", "\r\n<br>");

		string action = " &nbsp; <a href=notepad.aspx?id=" + id + " class=o>Edit</a>";
		action += " &nbsp; <a href=notepad.aspx?a=del&id=" + id + " class=o>Del</a>";
		action += " &nbsp; <a href=notepad.aspx?a=color&id=" + id + " class=o>Color</a>";

		Response.Write("<tr><td>");
		Response.Write("<table border=1 align=center bordercolor=" + color + ">");
		Response.Write("<tr bgcolor=" + color + ">");
		Response.Write("<td nowrap><b>" + time + "</b></td><td width=90%>" + action + "</td></tr>");

		Response.Write("<tr>");
		Response.Write("<td width=100% colspan=2>" + text + " <br>&nbsp;</td></tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");
	}

	Response.Write("<form action=notepad.aspx method=post>");
	Response.Write("<input type=hidden name=id value=" + m_id + ">");
	
	Response.Write("<tr><td colspan=2><b>Catalog : </b>");
	Response.Write("<select name=cat>");
	Response.Write(GetCatOptions(cat));
	Response.Write("</select>");
	Response.Write(" &nbsp; <b>New Catalog : </b>");
	Response.Write("<input type=text name=cat_new maxlength=50>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2><textarea name=text cols=70 rows=10>" + enote + "</textarea></td></tr>");
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value='" + cmd + "' " + Session["button_style"] + ">");
	if(enote != "")
	{
//		Response.Write("<input type=button value='New' onclick=window.location=('notepad.aspx?r=1" + BuildParameter() + "') ");
		Response.Write("<input type=button value='New' onclick=window.location=('notepad.aspx?r=1" + BuildParameter("noid") + "') ");
		Response.Write(Session["button_style"]);
		Response.Write(">");
	}
	Response.Write("</td></tr>");
	Response.Write("</form>");
	Response.Write("</table>");
}

bool DoChangeColor()
{
	if(m_id == "")
		return true;

	string[] colors = new string[16];
	int i = 0;
	colors[i++] = "#E6E6FA";//"lavender";//"tomato";//"lightcoral";//"hotpink";//"red";//"tomato";//"deeppink";//"salmon";
	colors[i++] = "#FFAAAA";//"lavender";//"tomato";//"lightcoral";//"hotpink";//"red";//"tomato";//"deeppink";//"salmon";
//	colors[i++] = "orange";
//	colors[i++] = "gold";
	colors[i++] = "#40C798";//"palegreen";//"mediumseagreen";//"limegreen";//"lightseagreen";//"lightgreen";
	colors[i++] = "paleturquoise";//"cyan";
	colors[i++] = "lightskyblue";
	colors[i++] = "khaki";//"thistle";
	colors[i++] = "antiquewhite";

	if(Session["notepad_color"] == null)
		Session["notepad_color"] = 0;
//	Random rnd = new Random();
//	string color="#" + rnd.Next(255).ToString("x") + rnd.Next(255).ToString("x") + rnd.Next(255).ToString("x");
	string color = colors[(int)Session["notepad_color"]];
//DEBUG("color=", color);
	Session["notepad_color"] = (int)Session["notepad_color"] + 1;
	if((int)Session["notepad_color"] >= 7)
		Session["notepad_color"] = 0;

	string sc = " UPDATE notepad SET color='" + color + "' ";
	sc += " WHERE id = " + m_id + " AND card_id=" + m_user;
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

bool DoDeleteNote()
{
	if(m_id == "")
		return true;

	string sc = " UPDATE notepad SET deleted=1 ";
	sc += " WHERE id = " + m_id + " AND card_id=" + m_user;
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

<script runat=server>

DataSet ds = new DataSet();
string m_user = "-1";
string m_id = "";
int m_notes = 0;

void ShowPublicNotice() 
{
//	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=default.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}
	if(action == "del")
	{
		if(DoDeleteNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=default.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}
	else if(cmd == "Post")
	{
		if(DoAddNewNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=default.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}
	else if(cmd == "Update")
	{
		if(DoUpdateNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=default.aspx?r=1" + BuildParameter() + "\">");
			return;
		}
	}

	GetNotes();
	DisplayNotes();
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
	return s;
}

bool DoAddNewNote()
{
	string text = Request.Form["text"];
	string sc = " INSERT INTO notepad (card_id, text, poster) VALUES( ";
	sc += m_user;
	sc += ", N'" + EncodeQuote(text) + "' ";
	sc += ", " + Session["card_id"].ToString();
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
	string id = Request.Form["id"];
	string sc = " UPDATE notepad SET ";
	sc += " text = N'" + EncodeQuote(text) + "' ";
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

bool GetNotes()
{
	string sc = "SELECT * FROM notepad ";
	sc += " WHERE deleted = 0 AND card_id = " + m_user;
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
//---------------------------------------------------  P NOTE ------------------------------------------------
void DisplayNotes()
{
	StringBuilder sb = new StringBuilder();
	
	sb.Append("<table width=99% align=center border=0 cellspacing=0 cellpadding=0  bgcolor=white >");
	//sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed; border-color:#6699CC\">");
	
	sb.Append ("<tr><td align=left ><img src=../i/internal.jpg width=200 height=46 border=0></td></tr>");
	sb.Append("<tr>");
	sb.Append("<td width=100% style=\" font:13px arial; border-top:1px solid; border-right:1px solid; border-left:1px solid; border-bottom:1px solid; border-color:#6699CC\">");

	sb.Append("<table width=100% align=center border=1 cellspacing=1 cellpadding=1 bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	string enote = "";
	string cmd = "Post";
	string cat = "";
	for(int i=0; i<m_notes; i++)
	{
		DataRow dr = ds.Tables["notes"].Rows[i];
		string id = dr["id"].ToString();
		string poster = dr["poster"].ToString();
		if(poster != "")
		{
			DataRow drc = GetCardData(poster);
			if(drc != null)
				poster = drc["name"].ToString();
		}
		string color = dr["color"].ToString();
		string text = dr["text"].ToString();
		string time = DateTime.Parse(dr["date"].ToString()).ToString("dd-MMM HH:mm");
		if(id == m_id)
		{
			enote = text;
			cat = dr["cat"].ToString();
			cmd = "Update";
		}
		text = text.Replace("\r\n", "\r\n<br>");

		string action = " &nbsp; <a href=default.aspx?id=" + id + " class=o>Edit</a>";
		action += " &nbsp; <a href=default.aspx?a=del&id=" + id + " class=o>Del</a>";
		action += " &nbsp; <a href=default.aspx?a=color&id=" + id + " class=o>Color</a>";

		sb.Append("<tr><td>");
		
		sb.Append("<table width=100% align=center border=0 cellspacing=1 cellpadding=1 bordercolor=" + color + " bgcolor=white");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		sb.Append("<tr bgcolor=" + color + "><td nowrap>");
		sb.Append("<b>" + time + "</b>&nbsp&nbsp;" + action);
		if(poster != "")
			sb.Append(" &nbsp&nbsp; <i>(" + poster + ")</i>");
		sb.Append("</td></tr>");
		sb.Append("<tr><td style=\"font:12px arial; color:#333333\" height=25 valign=middle>" + text + " &nbsp; ");
		sb.Append("</td></tr>");
		sb.Append("</table>");

		sb.Append("</td></tr>");
	}

	sb.Append("<form action=default.aspx method=post>");
	if(enote != "" || Request.QueryString["a"] == "new")
	{
		sb.Append("<input type=hidden name=id value=" + m_id + ">");

		sb.Append("<tr><td><textarea name=text cols=50 rows=3>" + enote + "</textarea></td></tr>");
		sb.Append("<tr><td align=right>");
		sb.Append("<input type=submit name=cmd value='" + cmd + "' " + Session["button_style"] + ">");
		sb.Append("</td></tr>");
	}
	sb.Append("</form>");

	if(Request.QueryString["a"] != "new")
	{
		sb.Append("<tr><td>");
		sb.Append("<input type=button style=\"background:url(i/newnotice.jpg); width=124; height=42; border-style:none; border-left:none\" onclick=window.location=('default.aspx?a=new&r=1" + BuildParameter("noid") + "') ");
		sb.Append(Session["button_style"] + ">");
		sb.Append("</td></tr>");
	}

	sb.Append("</table>");

	sb.Append("</td></tr>");
	sb.Append("<table>");

	Response.Write(sb.ToString());
}

bool DoChangeColor()
{
	if(m_id == "")
		return true;

//	Random rnd = new Random();
//	string color="#" + rnd.Next(255).ToString("x") + rnd.Next(255).ToString("x") + rnd.Next(255).ToString("x");

	string[] colors = new string[16];
	int i = 0;
	colors[i++] = "#bbbb77"; //default
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
	if((int)Session["notepad_color"] >= 8)
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
</script>

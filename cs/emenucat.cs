<script runat=server>

string m_id = "";
//bool m_bEZNZAdmin = false;
DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
    if(Session["email"].ToString().IndexOf("@eznz.com") >= 0)
		m_bEZNZAdmin = true;
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	if(Request.QueryString["t"] == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenucat.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.QueryString["seq"] != null && Request.QueryString["seq"] != "")
	{
		m_id = Request.QueryString["id"];
		if(DoMove())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenucat.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.Form["cmd"] == "Insert")
	{
		if(DoInsert())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenucat.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "UPDATE")
	{
		if(DoUpdate())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenucat.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetAllCatalog())
		return;

	PrintBody();

	PrintAdminFooter();
}

bool DoDelete()
{
	string sc = " DELETE FROM menu_admin_catalog WHERE id=" + m_id;
	sc += " DELETE FROM menu_admin_sub WHERE cat=" + m_id;
	if(g_bDemo && g_bOrderOnlyVersion)
	{
		sc = " UPDATE menu_admin_catalog SET orderonly=0 WHERE id=" + m_id;
		sc += " UPDATE menu_admin_sub SET orderonly=0 WHERE cat=" + m_id;
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

bool DoMove()
{
	string id = Request.QueryString["id"];
	string seq = Request.QueryString["seq"];
	
	if(id == null || seq == null || id == "" || seq == "")
	{
		Response.Write("Error, no id or seq, please follow a proper link");
		return false;
	}
	string sc = " UPDATE menu_admin_catalog SET seq=" + seq + " WHERE id=" + id;
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

bool DoInsert()
{
	string name = Request.Form["name"];
	string seq = Request.Form["seq"];
	
	Trim(ref name);
//	if(name == "")
//	{
//		Response.Write("error, name empty");
//		return false;
//	}
	name = EncodeQuote(name);
	string sc = " INSERT INTO menu_admin_catalog (name, seq) VALUES(N'" + name + "', " + seq + ") ";
	if(g_bDemo && g_bOrderOnlyVersion)
	{
		sc = " IF NOT EXISTS (SELECT * FROM menu_admin_catalog WHERE name='" + EncodeQuote(name) + "' ) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('" + name + "', " + seq + ") ";
		sc += " ELSE ";
		sc += " UPDATE menu_admin_catalog SET orderonly=1 WHERE name=N'" + EncodeQuote(name) + "' ";
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

bool DoUpdate()
{
	string id = Request.Form["update_id"];
	string name = Request.Form["name_update"];
	
	Trim(ref name);
	if(name == "")
	{
		Response.Write("Error, name empty");
		return false;
	}
	name = EncodeQuote(name);
	string sc = " UPDATE menu_admin_catalog SET name=N'" + name + "' WHERE id=" + id;
//DEBUG("sc=", sc);
//return false;
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

bool GetAllCatalog()
{
	int rows = 0;
	string sc = "SELECT * FROM menu_admin_catalog ";
	if(g_bDemo && g_bOrderOnlyVersion)
		sc += " WHERE orderonly=1 ";
	sc += " ORDER BY seq ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintBody()
{
	Response.Write("<br><center><h3>Main Menu Catalog</font></h3>");
	Response.Write("<form name=f action=emenucat.aspx method=post>");
	Response.Write("<table width=55% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>MENU</th>");
//	Response.Write("<th align=left>SEQUENCE</th>");
	Response.Write("<th>EDIT</th>");
	Response.Write("<th align=right>MOVE</th>");
	Response.Write("</tr>");

	string id = "";
	string name = "";
	double seq = 0; //current sequence
	double sequ1 = 0; //previous previous menu's sequence
	double sequ = 0; //previous menu's sequence
	double seqd = 0; //next menu's sequence
	double seqd1 = 0; //next next menu's sequence
	double seqn = 0; //new sequence number (calculated)
	DataRow dr = null;
	int rows = ds.Tables["cat"].Rows.Count;
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["cat"].Rows[i];
		if(i < rows - 1 )
			seqd = MyDoubleParse(ds.Tables["cat"].Rows[i+1]["seq"].ToString()); //next seq number 
		else
			seqd = 0;
		if(i < rows - 2 )
			seqd1 = MyDoubleParse(ds.Tables["cat"].Rows[i+2]["seq"].ToString()); //next seq number 
		else
			seqd1 = 0;

		id = dr["id"].ToString();
		name = dr["name"].ToString();
		seq = MyDoubleParse(dr["seq"].ToString());

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		if(m_id == id)
		{
			Response.Write("<td><input type=text name=name_update value=\"" + name + "\"></td>");
			Response.Write("<td align=right><input type=submit name=cmd value=UPDATE " + Session["button_style"] + "><input type=submit name=cmd value=CANCEL " + Session["button_style"] + "></td>");
			Response.Write("<input type=hidden name=update_id value=" + id + ">");
		}
		else
		{
			Trim(ref name);
			if(name == "")
				name = "<font color=green><i>&nbsp&nbsp; (seperator)</i></font>";
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td align=right>");
			Response.Write("<input type=button " + Session["button_style"]);
			Response.Write(" onclick=window.location=('emenusub.aspx?&cat=" + id + "&r=" + DateTime.Now.ToOADate() + "')");
			Response.Write(" value='SUB MENUS'>");
			Response.Write("<input type=button " + Session["button_style"]);
			Response.Write(" onclick=window.location=('emenucat.aspx?id=" + id + "&r=" + DateTime.Now.ToOADate() + "')");
			Response.Write(" value=EDIT>");
			Response.Write("<input type=button " + Session["button_style"]);
			Response.Write(" onclick=\"if(window.confirm('Are you sure to delete this menu?'))window.location='emenucat.aspx?t=del&id=" + id + "&r=" + DateTime.Now.ToOADate() + "'");
			Response.Write("\" value=DEL>");
			Response.Write("</td>");
		}
		Response.Write("<td align=right>");
		if(i > 0)
		{
			seqn = sequ - (sequ - sequ1) / 2;
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=");
			Response.Write("('emenucat.aspx?seq=" + seqn.ToString() + "&id=");
			Response.Write(id + "&r=" + DateTime.Now.ToOADate() + "') value='MOVE UP'>");
		}
		if(i<rows-1)
		{
			if(seqd1 != 0)
				seqn = seqd + (seqd1 - seqd) / 2;
			else
				seqn = seqd + 1;
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=");
			Response.Write("('emenucat.aspx?seq=" + seqn.ToString() + "&id=");
			Response.Write(id + "&r=" + DateTime.Now.ToOADate() + "') value='DOWN'>");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
		sequ1 = sequ;
		sequ = seq;
	}
  if(m_bEZNZAdmin){
	Response.Write("<tr>");
	Response.Write("<td><input type=text name=name></td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td align=right><input type=submit name=cmd value=Insert " + Session["button_style"] + "></td>");
	Response.Write("<input type=hidden name=seq value=" + (seq + 1).ToString() + ">");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=30><a href=emenuclass.aspx?r=" + DateTime.Now.ToOADate() + " class=o>Edit Access Classes </a></td></tr>");

		Response.Write("<tr><td colspan=30><a href=emenuid.aspx?r=" + DateTime.Now.ToOADate() + " class=o>Edit Menu ID</a></td></tr>");
}
	Response.Write("</table>");
	Response.Write("</form>");
}

</script>

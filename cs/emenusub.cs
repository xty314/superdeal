<script runat=server>

string m_id = "";
string m_cat = "";
//bool m_bEZNZAdmin = false;
DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
   if(Session["email"].ToString().IndexOf("@eznz.com") >= 0)
		m_bEZNZAdmin = true;
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"];

	if(m_cat == "")
	{
		Response.Write("<br><br><center><h3>Error, no catalog id, please follow a proper link");
		return;
	}

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	if(Request.QueryString["t"] == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenusub.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.QueryString["seq"] != null && Request.QueryString["seq"] != "")
	{
		m_id = Request.QueryString["id"];
		if(DoMove())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenusub.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.Form["cmd"] == "Update Access")
	{
		if(DoUpdateAccess())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenusub.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Insert")
	{
		if(DoInsert())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenusub.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetAllClass())
		return;
	if(!GetAllMenus())
		return;
	if(!GetAllSub())
		return;

	PrintBody();

	PrintAdminFooter();
}

bool GetAllClass()
{
	string sc = " SELECT * FROM menu_access_class ";
	sc += " WHERE name NOT LIKE '%no access%' AND name NOT LIKE '%administrator%' ";
	sc += " ORDER BY name";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "class");
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
	string sc = " DELETE FROM menu_admin_sub WHERE id=" + m_id;
	if(g_bDemo && g_bOrderOnlyVersion)
	{
		sc = " UPDATE menu_admin_sub SET orderonly=0 WHERE id=" + m_id;
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
	string sc = " UPDATE menu_admin_sub SET seq=" + seq + " WHERE id=" + id;
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
	string menu = Request.Form["menu"];
	string seq = Request.Form["seq"];
	if(menu == null || menu == "")
	{
		Response.Write("<h3>Error, no emnu id selected");
		return false;
	}

	string sc = " INSERT INTO menu_admin_sub (menu, cat, seq) VALUES('" + menu + "', " + m_cat + ", " + seq + ") ";
	if(g_bDemo && g_bOrderOnlyVersion)
	{
		sc = " IF NOT EXISTS (SELECT * FROM menu_admin_sub WHERE menu='" + menu + "' AND cat='" + m_cat + "' ) ";
		sc += " INSERT INTO menu_admin_sub (menu, cat, seq) VALUES('" + menu + "', " + m_cat + ", " + seq + ") ";
		sc += " ELSE ";
		sc += " UPDATE menu_admin_sub SET orderonly=1 WHERE menu='" + menu + "' AND cat='" + m_cat + "' ";
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

bool DoUpdateAccess()
{
	if(!GetAllSub())
		return false;
	if(!GetAllClass())
		return false;

	string sc = "";
	for(int i=0; i<ds.Tables["sub"].Rows.Count; i++)
	{
		string menu_id = ds.Tables["sub"].Rows[i]["menu"].ToString();

		sc += " DELETE FROM menu_admin_access WHERE menu=" + menu_id + " ";
		for(int j=0; j<ds.Tables["class"].Rows.Count; j++)
		{
			string cid = ds.Tables["class"].Rows[j]["id"].ToString();
			if(Request.Form[cid + "_" + menu_id] == "on")
				sc += " INSERT INTO menu_admin_access (menu, class) VALUES(" + menu_id + ", " + cid + ") ";
		}
	}
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

bool GetAllSub()
{
	int rows = 0;
	string sc = "SELECT s.id, s.menu, s.seq, i.name ";
	sc += " FROM menu_admin_sub s JOIN menu_admin_id i ON i.id=s.menu ";
	sc += " WHERE s.cat='" + m_cat + "' ";
	if(g_bDemo && g_bOrderOnlyVersion)
	{
		sc += " AND s.orderonly=1 ";
	}
	sc += " ORDER BY s.seq ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "sub");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetAllMenus()
{
	int rows = 0;
	string sc = "SELECT i.*, c.name AS cat_name ";
	sc += " FROM menu_admin_id i JOIN menu_admin_catalog c ON c.id=i.cat ";
	sc += " WHERE i.id NOT IN (SELECT menu FROM menu_admin_sub WHERE cat=" + m_cat;
	if(g_bDemo && g_bOrderOnlyVersion)
		sc += " AND orderonly=1 ";
	sc += ") ";
	if(g_bDemo && g_bOrderOnlyVersion)
		sc += " AND i.orderonly=1 ";
	sc += " ORDER BY c.seq, i.name ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "menu");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool CanAccess(string menu_id, string class_id)
{
	if(ds.Tables["canaccess"] != null)
		ds.Tables["canaccess"].Clear();

	string sc = " SELECT id FROM menu_admin_access WHERE menu=" + menu_id + " AND class=" + class_id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "canaccess") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return false;
}

string GetMenuOptions()
{
	string s = "";
	string cat_old = "";
	for(int i=0; i<ds.Tables["menu"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["menu"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string cat = dr["cat"].ToString();
		string cat_name = dr["cat_name"].ToString();
		if(cat_old != cat)
		{
//			if(cat_old != "")
				s += "<option value=''>---------- " + cat_name + " ----------</option>";
			cat_old = cat;
		}
		s += "<option value=" + id + ">" + name + "</option>";
	}
	return s;
}

void PrintBody()
{
	Response.Write("<br><center><h3><font color=red>" + GetMenuCatName(m_cat) + "</font> Menu </h3>");
	Response.Write("<form name=f action=emenusub.aspx?cat=" + m_cat + " method=post>");
	Response.Write("<table width=55% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><th colspan=2>MENU</td><th colspan=30>ACCESS</th></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>MENU</th>");
//	Response.Write("<th align=left>SEQUENCE</th>");
	Response.Write("<th>EDIT</th>");

	int i = 0;
	for(i=0; i<ds.Tables["class"].Rows.Count; i++)
	{
		string cname = ds.Tables["class"].Rows[i]["name"].ToString();
		string cid = ds.Tables["class"].Rows[i]["id"].ToString();
		Response.Write("<th nowrap>&nbsp; " + cname + " &nbsp;</th>");
	}
	Response.Write("</tr>");

	string id = "";
	string name = "";
	string menu_id = "";
	double seq = 0; //current sequence
	double sequ1 = 0; //previous previous menu's sequence
	double sequ = 0; //previous menu's sequence
	double seqd = 0; //next menu's sequence
	double seqd1 = 0; //next next menu's sequence
	double seqn = 0; //new sequence number (calculated)
	DataRow dr = null;
	int rows = ds.Tables["sub"].Rows.Count;
	bool bAlterColor = false;
	for(i=0; i<rows; i++)
	{
		dr = ds.Tables["sub"].Rows[i];
		if(i < rows - 1 )
			seqd = MyDoubleParse(ds.Tables["sub"].Rows[i+1]["seq"].ToString()); //next seq number 
		else
			seqd = 0;
		if(i < rows - 2 )
			seqd1 = MyDoubleParse(ds.Tables["sub"].Rows[i+2]["seq"].ToString()); //next seq number 
		else
			seqd1 = 0;

		id = dr["id"].ToString();
		menu_id = dr["menu"].ToString();
		name = dr["name"].ToString();
		seq = MyDoubleParse(dr["seq"].ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td nowrap><b>" + name + "</b></td>");
		Response.Write("<td align=right>");
		if(i > 0)
		{
			seqn = sequ - (sequ - sequ1) / 2;
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=");
			Response.Write("('emenusub.aspx?cat=" + m_cat + "&seq=" + seqn.ToString() + "&id=");
			Response.Write(id + "&r=" + DateTime.Now.ToOADate() + "') value='MOVE UP'>");
		}
		if(i<rows-1)
		{
			if(seqd1 != 0)
				seqn = seqd + (seqd1 - seqd) / 2;
			else
				seqn = seqd + 1;
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=");
			Response.Write("('emenusub.aspx?cat=" + m_cat + "&seq=" + seqn.ToString() + "&id=");
			Response.Write(id + "&r=" + DateTime.Now.ToOADate() + "') value='MOVE DOWN'>");
		}
		Response.Write("<input type=button " + Session["button_style"]);
		Response.Write(" onclick=\"if(window.confirm('Are you sure to delete this menu?'))window.location='emenusub.aspx?cat=" + m_cat + "&t=del&id=" + id + "&r=" + DateTime.Now.ToOADate() + "'");
		Response.Write("\" value=DEL>");
		Response.Write("</td>\r\n\r\n");

		for(int j=0; j<ds.Tables["class"].Rows.Count; j++)
		{
			string cname = ds.Tables["class"].Rows[j]["name"].ToString();
			string cid = ds.Tables["class"].Rows[j]["id"].ToString();
			Response.Write("<td align=center><input type=checkbox name=" + cid + "_" + menu_id);
			if(CanAccess(menu_id, cid))
				Response.Write(" checked");
			Response.Write("></td>\r\n");
		}

		Response.Write("</tr>");
		sequ1 = sequ;
		sequ = seq;
	}

	Response.Write("<tr><td colspan=2>");
	for(int j=0; j<ds.Tables["class"].Rows.Count; j++)
	{
		string cid = ds.Tables["class"].Rows[j]["id"].ToString();
		Response.Write("<td align=center><a href=emenutest.aspx?c=" + cid + " class=o target=_blank>test</a></td>");
	}
	Response.Write("</tr>");

  if(m_bEZNZAdmin){
	Response.Write("<tr>");
//	Response.Write("<td><input type=text name=name></td>");
	Response.Write("<td colspan=3><select name=menu>" + GetMenuOptions() + "</select>");
	Response.Write("<input type=submit name=cmd value=Insert " + Session["button_style"] + "></td>");
	Response.Write("<input type=hidden name=seq value=" + (seq + 1).ToString() + ">");}
	Response.Write("<td colspan=30 align=right><input type=submit name=cmd " + Session["button_style"] + " value='Update Access'></td>");
	Response.Write("</tr>");
	if(m_bEZNZAdmin){
	Response.Write("<tr><td colspan=3><a href=emenucat.aspx?r=" + DateTime.Now.ToOADate() + " class=o>Edit Main Menu Catalog</a></td></tr>");
}
	Response.Write("</table>");
	Response.Write("</form>");
}

string GetMenuCatName(string id)
{
	if(ds.Tables["getmenuname"] != null)
		ds.Tables["getmenuname"].Clear();

	string sc = " SELECT name FROM menu_admin_catalog WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "getmenuname") == 1)
			return ds.Tables["getmenuname"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return id;
}

</script>

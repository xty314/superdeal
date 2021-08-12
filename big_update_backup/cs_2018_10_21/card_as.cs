<script runat=server>

string m_id = "";
string m_cat = "";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"];

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	if(Request.QueryString["t"] == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=card_as.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.Form["cmd"] == "Update Access")
	{
		if(DoUpdateAccess())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=card_as.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Insert")
	{
		if(DoInsert())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=card_as.aspx?cat=" + m_cat + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}

	if(Request.QueryString["t"] == "test")
	{
		string cid = Request.QueryString["cid"];
		Session["customer_access_level"] = cid;
		Session["main_card_id"] = Session["card_id"];
	}
	if(Request.QueryString["t"] == "endtest")
	{
		Session["main_card_id"] = "";
		Response.Redirect("close.htm");
		return;
	}
	PrintHeaderAndMenu();

	if(!GetAllMenu())
		return;
	if(!GetAllClass())
		return;
	PrintBody();

	PrintFooter();
}

bool GetAllClass()
{
	string main_cardid = "";
	if(Session["main_card_id"] != null)
		main_cardid = Session["main_card_id"].ToString();
	if(main_cardid == null || main_cardid == "")
		main_cardid = Session["card_id"].ToString();

	int rows = 0;
	string sc = "SELECT * FROM card_access_class WHERE main_card_id = " + main_cardid + " ORDER BY id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "class");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetAllMenu()
{
	int rows = 0;
	string sc = "SELECT * FROM card_access_menu ORDER BY id ";
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

bool DoDelete()
{
	string sc = " DELETE FROM menu_admin_sub WHERE id=" + m_id;
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
	if(!GetAllMenu())
		return false;
	if(!GetAllClass())
		return false;

	string sc = "";
	for(int i=0; i<ds.Tables["menu"].Rows.Count; i++)
	{
		string menu_id = ds.Tables["menu"].Rows[i]["id"].ToString();

		sc += " DELETE FROM card_access_data WHERE main_card_id=" + Session["card_id"] + " AND no_access_menu_id = " + menu_id + " ";
		for(int j=0; j<ds.Tables["class"].Rows.Count; j++)
		{
			string cid = ds.Tables["class"].Rows[j]["class_id"].ToString();
			if(Request.Form[cid + "_" + menu_id] == "on")
			{
				sc += " INSERT INTO card_access_data (main_card_id, class_id, no_access_menu_id) ";
				sc += " VALUES(" + Session["card_id"] + ", " + cid + ", " + menu_id + ") ";
			}
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

bool NoAccess(string menu_id, string class_id)
{
	if(ds.Tables["noaccess"] != null)
		ds.Tables["noaccess"].Clear();

	string sc = " IF EXISTS (SELECT id FROM card_access_data WHERE main_card_id = " + Session["card_id"] + ") ";
	sc += " SELECT id ";
	sc += " FROM card_access_data ";
	sc += " WHERE main_card_id = " + Session["card_id"];
	sc += " AND no_access_menu_id = " + menu_id;
	sc += " AND class_id = " + class_id;
	sc += " ELSE ";
	sc += " SELECT id FROM card_access_data_default ";
	sc += " WHERE no_access_menu_id = " + menu_id + " AND class_id = " + class_id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "noaccess") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return false;
}

void PrintBody()
{
	Response.Write("<br><center><h3>Advance Access Options</h3>");
	Response.Write("<form name=f action=card_as.aspx method=post>");
	Response.Write("<table width=55% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

//	Response.Write("<tr><th colspan=2>MENU</td><th colspan=30>DENIED ACCESS</th></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>MENU</th>");

	int i = 0;
	for(i=0; i<ds.Tables["class"].Rows.Count; i++)
	{
		string name = ds.Tables["class"].Rows[i]["class_name"].ToString();
		Response.Write("<th nowrap>&nbsp; " + name + " &nbsp;</th>");
	}
	Response.Write("</tr>");

	string class_id = "";
	string class_name = "";
	string menu_id = "";
	string menu = "";
	DataRow dr = null;
	int rows = ds.Tables["menu"].Rows.Count;
	bool bAlterColor = false;
	int j = 0;
	for(i=0; i<rows; i++)
	{
		dr = ds.Tables["menu"].Rows[i];

		menu_id = dr["id"].ToString();
		menu = dr["description"].ToString();
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td nowrap><b>" + menu + "</b></td>");
		for(j=0; j<ds.Tables["class"].Rows.Count; j++)
		{
			string cname = ds.Tables["class"].Rows[j]["class_name"].ToString();
			string cid = ds.Tables["class"].Rows[j]["class_id"].ToString();
			Response.Write("<td align=center><input type=checkbox name=" + cid + "_" + menu_id);
			if(NoAccess(menu_id, cid))
				Response.Write(" checked");
			Response.Write(">NO</td>\r\n");
		}

		Response.Write("</tr>");
	}

	Response.Write("<tr><td>");
	if(Session["main_card_id"] != null && Session["main_card_id"].ToString() == Session["card_id"].ToString())
		Response.Write("<input type=button onclick=window.location=('card_as.aspx?t=endtest') value='End Test' " + Session["button_style"] + ">");
	else
		Response.Write("&nbsp;");
	Response.Write("</td>");
	for(j=0; j<ds.Tables["class"].Rows.Count; j++)
	{
		string cid = ds.Tables["class"].Rows[j]["id"].ToString();
//		Response.Write("<form action=login.aspx target=_new method=post>");
//		Response.Write("<input type=hidden name=login value=>");
		Response.Write("<td align=center><input type=button onclick=window.open('card_as.aspx?t=test&cid=" + cid + "') value=Test " + Session["button_style"] + "></td>");
//		Response.Write("</form>");
	}
	Response.Write("</tr>");

	j++; //for colspan
	if(Session["main_card_id"] != null && Session["main_card_id"].ToString() == Session["card_id"].ToString())
	{
		Response.Write("<tr><td colspan=" + (j-2) + "><font color=red>Click \"End Test\" to regain manager access</td>");
		Response.Write("<td colspan=2 align=right>");
	}
	else
		Response.Write("<tr><td colspan=" + j + " align=right>");
	Response.Write("<input type=submit name=cmd " + Session["button_style"] + " value='Update Access'>");
	Response.Write("</td>");

	Response.Write("<tr><td colspan=3><a href=register.aspx?t=logins class=o>Login List</a></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

</script>

<script runat=server>

string m_cats = "";
string m_cat = "";
string m_id = "";

int m_nMenus = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	if(Request.QueryString["t"] == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuid.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	if(Request.Form["cmd"] == "Insert")
	{
		if(DoInsert())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuid.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Update")
	{
		if(DoUpdate())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuid.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetAllMenus())
		return;
	if(!GetAllCatalog())
		return;

	PrintBody();

	PrintAdminFooter();
}

bool DoDelete()
{
	string sc = " DELETE FROM menu_admin_id WHERE id=" + m_id;
	sc += " DELETE FROM menu_admin_sub WHERE menu=" + m_id;
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
	string sisters = Request.Form["sisters"];
	string uri = Request.Form["uri"];
	string cat = Request.Form["cat"];
	
	Trim(ref name);
	Trim(ref uri);
	if(uri == "")
	{
		Response.Write("uri empty");
		return false;
	}
	name = EncodeQuote(name);
	uri = EncodeQuote(uri);
	string sc = " INSERT INTO menu_admin_id (name, uri, sisters, cat) ";
	sc += " VALUES(N'" + name + "', '" + uri + "', '" + sisters + "', " + cat + ") ";
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
	string sisters = Request.Form["sisters_update"];
	string uri = Request.Form["uri_update"];
	string cat = Request.Form["cat_update"];
	
	Trim(ref name);
	Trim(ref uri);
	Trim(ref sisters);
	if(uri == "")
	{
		Response.Write("uri empty");
		return false;
	}
	name = EncodeQuote(name);
	uri = EncodeQuote(uri);
	string sc = " UPDATE menu_admin_id SET name=N'" + name + "', uri='" + uri;
	sc += "', sisters='" + sisters + "', cat=" + cat + " WHERE id=" + id;
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

bool GetAllMenus()
{
	string sc = " SELECT i.*, c.name AS cat_name  ";
	sc += " FROM menu_admin_id i LEFT OUTER JOIN menu_admin_catalog c ON c.id=i.cat ";
	sc += " ORDER BY c.seq, i.name";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_nMenus = myCommand.Fill(ds, "menu");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string GetCatOptions(string cat)
{
	string s = "";
	for(int i=0; i<ds.Tables["cat"].Rows.Count; i++)
	{
		string cid = ds.Tables["cat"].Rows[i]["id"].ToString();
		s += "<option value=" + cid;
		if(cat == cid)
			s += " selected";
		s += ">" + ds.Tables["cat"].Rows[i]["name"].ToString();
		s += "</option>";
	}
	return s;
}

bool GetAllCatalog()
{
	int rows = 0;
	string sc = "SELECT * FROM menu_admin_catalog ORDER BY seq ";
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

	if(rows == 0)
	{
		sc = " INSERT INTO menu_admin_catalog (name, seq) VALUES('File', 1) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Sales', 2) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Purchase', 3) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Card', 4) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Stock', 5) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Account', 6) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Service', 7) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('       ', 8) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Edit', 9) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Manage', 10) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Tools', 11) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Help', 12) ";
		sc += " INSERT INTO menu_admin_catalog (name, seq) VALUES('Develop', 13) ";
		sc += " SELECT * FROM menu_admin_catalog ORDER BY seq ";
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
	}
	return true;
}

void PrintBody()
{
	Response.Write("<br><center><h3>Available Menus : <font color=red>" + m_nMenus + "</font></h3>");
	Response.Write("<form name=f action=emenuid.aspx method=post>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>ID</th>");
	Response.Write("<th align=left>CATALOG</th>");
	Response.Write("<th align=left>NAME</th>");
	Response.Write("<th align=left>URI</th>");
	Response.Write("<th align=left>SISTERS</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	string id = "";
	string name = "";
	string sisters = ""; //sister uris
	string cat = "";
	string cat_id = "";
	string uri = "";
	DataRow dr = null;
	int rows = ds.Tables["menu"].Rows.Count;
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["menu"].Rows[i];
		id = dr["id"].ToString();
		name = dr["name"].ToString();
		sisters = dr["sisters"].ToString();
		cat = dr["cat_name"].ToString();
		cat_id = dr["cat"].ToString();
		uri = dr["uri"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td>" + id + "</td>");
		if(m_id == id)
		{
			Response.Write("<td><select name=cat_update>" + GetCatOptions(dr["cat"].ToString()) + "</select></td>");
			Response.Write("<td><input type=text name=name_update value=\"" + name + "\"></td>");
			Response.Write("<td><input type=text size=40 name=uri_update value=\"" + uri + "\"></td>");
			Response.Write("<td><input type=text size=40 name=sisters_update value=\"" + sisters + "\"></td>");
			Response.Write("<td align=right><input type=submit name=cmd value=Update " + Session["button_style"] + "><input type=submit name=cmd value=CANCEL " + Session["button_style"] + "></td>");
			Response.Write("<input type=hidden name=update_id value=" + id + ">");
		}
		else
		{
			if(name == "" || name == null)
			{
				if(Session["email"].ToString().IndexOf("@eznz.com")>= 0 )
				{				
					Response.Write("<td><a href=emenusub.aspx?cat=" + cat_id + " class=o>" + cat + "</a></td>");
					Response.Write("<td>" + name + "</td>");
			//		Response.Write("<td><input style=border-width:0; type=text readonly=true name=uri" + i + " value=\"" + uri + "\"></td>");
					Response.Write("<td>" + uri + "</td>");
					Response.Write("<td>" + sisters + "</td>");
					Response.Write("<td align=right><input type=button " + Session["button_style"]);
					Response.Write(" onclick=window.location=('emenuid.aspx?id=" + id + "&r=" + DateTime.Now.ToOADate() + "')");
					Response.Write(" value=Edit>");
					Response.Write("<input type=button " + Session["button_style"]);
					Response.Write(" onclick=\"if(window.confirm('Are you sure to delete this menu?'))window.location='emenuid.aspx?t=del&id=" + id + "&r=" + DateTime.Now.ToOADate() + "'");
					Response.Write("\" value=DEL></td>");
				}
			}
			else
			{
					Response.Write("<td><a href=emenusub.aspx?cat=" + cat_id + " class=o>" + cat + "</a></td>");
					Response.Write("<td>" + name + "</td>");
			//		Response.Write("<td><input style=border-width:0; type=text readonly=true name=uri" + i + " value=\"" + uri + "\"></td>");
					Response.Write("<td>" + uri + "</td>");
					Response.Write("<td>" + sisters + "</td>");
					Response.Write("<td align=right><input type=button " + Session["button_style"]);
					Response.Write(" onclick=window.location=('emenuid.aspx?id=" + id + "&r=" + DateTime.Now.ToOADate() + "')");
					Response.Write(" value=Edit>");
					Response.Write("<input type=button " + Session["button_style"]);
					Response.Write(" onclick=\"if(window.confirm('Are you sure to delete this menu?'))window.location='emenuid.aspx?t=del&id=" + id + "&r=" + DateTime.Now.ToOADate() + "'");
					Response.Write("\" value=DEL></td>");

			}
		}
		Response.Write("</tr>");
	}

	Response.Write("<tr>");
	Response.Write("<td align=center valign=center><font color=red><b> * </b></font></td>");
	Response.Write("<td><select name=cat>" + GetCatOptions("") + "</select></td>");
	Response.Write("<td><input type=text name=name></td>");
	Response.Write("<td><input type=text size=40 name=uri></td>");
	Response.Write("<td><input type=text size=40 name=sisters></td>");
	Response.Write("<td align=right><input type=submit name=cmd value=Insert " + Session["button_style"] + "></td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	
	Response.Write("<a href=emenucat.aspx?r=" + DateTime.Now.ToOADate() + " class=o>Edit Main Menu Catalog</a>");

}

</script>

<script runat=server>

string m_id = "";

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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuclass.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	if(Request.Form["cmd"] == "Insert")
	{
		if(DoInsert())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuclass.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Update")
	{
		if(DoUpdate())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuclass.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetAllClass())
		return;

	PrintBody();

	PrintAdminFooter();
}

bool DoDelete()
{
	string sc = " DELETE FROM menu_access_class WHERE id=" + m_id;
	sc += " DELETE FROM menu_admin_access WHERE class=" + m_id;
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
	
	Trim(ref name);
	if(name == "")
	{
		Response.Write("Error name blank");
		return false;
	}
	name = EncodeQuote(name);
	string sc = " INSERT INTO menu_access_class (name) VALUES('" + name + "')";
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
		Response.Write("Erro, name blank");
		return false;
	}
	name = EncodeQuote(name);
	string sc = " UPDATE menu_access_class SET name='" + name + "' WHERE id=" + id;
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

bool GetAllClass()
{
	string sc = " SELECT * FROM menu_access_class ORDER BY name";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_nClass = myCommand.Fill(ds, "class");
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
	Response.Write("<br><center><h3>Account Classes : <font color=red>" + m_nClass + "</font></h3>");
	Response.Write("<form name=f action=emenuclass.aspx method=post>");
	Response.Write("<table width=55%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>ID</th>");
	Response.Write("<th align=left>NAME</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	string id = "";
	string name = "";
	DataRow dr = null;
	int rows = ds.Tables["class"].Rows.Count;
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["class"].Rows[i];
		id = dr["id"].ToString();
		name = dr["name"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td>" + id + "</td>");
		if(m_id == id)
		{
			Response.Write("<td><input type=text name=name_update value=\"" + name + "\"></td>");
			Response.Write("<td align=right><input type=submit name=cmd value=Update " + Session["button_style"] + "></td>");
			Response.Write("<input type=hidden name=update_id value=" + id + ">");
		}
		else
		{
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td align=right><input type=button " + Session["button_style"]);
			Response.Write(" onclick=window.location=('emenuclass.aspx?id=" + id + "&r=" + DateTime.Now.ToOADate() + "')");
			Response.Write(" value=Edit>");
			Response.Write("<input type=button " + Session["button_style"]);
			Response.Write(" onclick=\"if(window.confirm('Are you sure to delete this menu?'))window.location='emenuclass.aspx?t=del&id=" + id + "&r=" + DateTime.Now.ToOADate() + "'");
			Response.Write("\" value=DEL></td>");
		}
		Response.Write("</tr>");
	}

	Response.Write("<tr>");
	Response.Write("<td align=center valign=center><font color=red><b> * </b></font></td>");
	Response.Write("<td><input type=text name=name></td>");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.name.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<td align=right><input type=submit name=cmd value=Insert " + Session["button_style"] + "></td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</form>");
}

</script>

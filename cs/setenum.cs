<script runat=server>

string m_cats = "";
string m_cat = "";
string m_id = "";
string m_sClass = "all";
bool m_bEdit = false;
int m_nMenus = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;
GetQueryString();
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	if(Request.QueryString["t"] == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "&cl="+ Request.QueryString["cl"]+"\">");
			//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuid.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	if(Request.Form["cmd"] == "Add New Desc")
	{
		if(DoInsert())
		{
			//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=emenuid.aspx?r=" + DateTime.Now.ToOADate() + "\">");
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "&cl="+ Request.QueryString["cl"]+"\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Update")
	{
		if(DoUpdate())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?r=" + DateTime.Now.ToOADate() + "&cl="+ Request.QueryString["cl"]+"\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetAllEnums())
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

void GetQueryString()
{
	if(Request.QueryString["ed"] == "1")
		m_bEdit = true;
	if(Request.QueryString["cl"] != "" && Request.QueryString["cl"] != null)
		m_sClass = Request.QueryString["cl"];
	
}

bool DoInsert()
{
	string name = Request.Form["name"];
	string class_name = Request.Form["class"];
	string id = Request.Form["id"];
//return false;
	if(name != "")
	{
		name = EncodeQuote(name);
		string sc = " INSERT INTO enum (id, name, class) ";
		sc += " VALUES("+ id +", '" + name + "', '"+ class_name + "') ";
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
	}
	return true;
}

bool DoUpdate()
{
	string id = Request.Form["id"];
	string name = Request.Form["desc"];
	string class_name = Request.Form["class"];

	name = EncodeQuote(name);
	string sc = " UPDATE enum SET name = '"+ name +"' ";
	sc += " WHERE id = "+ id +" AND class = '"+ class_name +"'";
//DEBUG("sc = ",sc);
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
string GetAllClass()
{
	string sc = " SELECT DISTINCT class FROM enum ";
	if(m_sClass != "" && m_sClass != "all")
		
	sc += " ORDER BY class ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "eclass");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
//		return false;
	}
	Response.Write("<b>SELECT Class name: </b><select name=class_name ");
	Response.Write(" onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
	Response.Write("&cl='+ escape(this.options[this.selectedIndex].value))\" ");
	Response.Write(">");
	Response.Write("<option value='all'>All</option>");
	for(int i = 0; i<ds.Tables["eclass"].Rows.Count; i++)
	{
		string sclass = ds.Tables["eclass"].Rows[i]["class"].ToString();
		
		Response.Write("<option value='"+ sclass +"'");
		if(m_sClass == sclass )
			Response.Write(" selected ");
		Response.Write(">"+ sclass +"</option>");
	}
	Response.Write("</select>");
	return "";
}

bool GetAllEnums()
{

	string sc = " SELECT * ";
	sc += " FROM enum WHERE 1=1 ";
	if(m_sClass != "" && m_sClass != "all")
		sc += " AND class = '"+ m_sClass +"' ";
	sc += " ORDER BY class, id ";
//DEBUG(" sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_nMenus = myCommand.Fill(ds, "enum");
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



void PrintBody()
{
	Response.Write("<br><center><h3>Available Menus :</h3>");
	Response.Write("<form name=f method=post>");
	Response.Write("<table width=60%  align=center valign=center cellspacing=0 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=3>");
	GetAllClass();
	Response.Write("</tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>ID</th>");
	Response.Write("<th align=left>CLASS_NAME</th>");
	Response.Write("<th align=left>DESCRIPTION</th>");
	Response.Write("<th align=left>ACTION</th>");

	Response.Write("</tr>");
	int nrow = 4;
	string id = "";
	string name = "";
	string class_name = "";

	DataRow dr = null;
	int rows = ds.Tables["enum"].Rows.Count;
	bool bAlterColor = false;
	string uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"";
	if(m_sClass != "" && m_sClass != "all")
		uri += "&cl="+ m_sClass +"";
bool bUpdate = false;
	
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["enum"].Rows[i];
		id = dr["id"].ToString();
		name = dr["name"].ToString();
		class_name = dr["class"].ToString();
		if(m_bEdit && m_sClass != "" && m_sClass != "all" && Request.QueryString["id"] == id)
			bUpdate = true;	
	
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td>" + id + "</td>");
		Response.Write("<td>"+ class_name +"</td>");
		Response.Write("<td>");
		//if(m_bEdit && m_sClass != "" && m_sClass != "all" && Request.QueryString["id"] == id)
		if(bUpdate)
			Response.Write("<input type=text name=desc value='");
		Response.Write(name);
		//if(m_bEdit && m_sClass != "" && m_sClass != "all" && Request.QueryString["id"] == id)
		if(bUpdate)
			Response.Write("'>");
		Response.Write("</td>");
		Response.Write("<td>");
		if(bUpdate)
		{
			Response.Write("<input type=hidden name=class value='"+ class_name +"'>");
			Response.Write("<input type=hidden name=id value='"+ id +"'>");
			Response.Write("<input type=submit name=cmd value='Update' "+ Session["button_style"] +">");
			Response.Write("<input type=button value='Cancel' "+ Session["button_style"] +" Onclick=\"window.location=('"+ uri +"') \">");
		//	Response.Write("<input type=submit name=cmd value='Add New' "+ Session["button_style"] +">");
			bUpdate = false;
		}
		else  //if(!bUpdate && Request.QueryString["new"] != "1")
		{
			Response.Write("<a title='change settings' href='"+ uri +"&ed=1&id="+ id +"' class=o>");
			Response.Write("edit");
			Response.Write("</a>");
		}
		Response.Write("</td>");
		Response.Write("</tr>");

		if((i+1)==rows && Request.QueryString["new"] == "1")
		{
			Response.Write("<input type=hidden name=class value='"+ class_name +"'>");
			Response.Write("<input type=hidden name=id value='"+ (int.Parse(id) + 1).ToString() +"'>");

			Response.Write("<tr><td><input type=text name=classid value='"+ (int.Parse(id) + 1).ToString() +"' readonly></td>");
			Response.Write("<td><input type=text name=class_name value='"+ class_name +"' readonly></td>");
			Response.Write("<td><input type=text name=name></td>");
			Response.Write("<td><input type=submit name=cmd value='Add New Desc' "+ Session["button_style"] +" ");
			Response.Write(" onclick=\"if(!confirm('Processing this value...!!\\r\\nOnce Added will not allow delete')){return false;}\" >");
			Response.Write("<input type=button value='Cancel' "+ Session["button_style"] +" Onclick=\"window.location=('"+ uri +"') \">");
			
			Response.Write("</td>");
			Response.Write("</tr>");

		}
	}

	
	if(m_sClass != "" && m_sClass != "all" && Request.QueryString["new"] != "1")
	{
		Response.Write("<tr align=right>");
		Response.Write("<td colspan="+ nrow +">");
		Response.Write("<input type=button value='Add New' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('"+ uri +"&new=1')\" ");
		Response.Write(">");
		Response.Write("</td></tr>");
	}
	
	Response.Write("</table>");
	Response.Write("</form><br><br>");
	

}

</script>
<asp:Label id=LFooter runat=server/>
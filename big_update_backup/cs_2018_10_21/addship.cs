<script runat=server>
DataSet dst = new DataSet();
DataSet dsi = new DataSet();


void NPage_Load()
{

	string s_cmd = "";
	if(Request.Form["cmd"] != null)
	{
		s_cmd = Request.Form["cmd"];
		Trim(ref s_cmd);
	}
//DEBUG("cmd = ", Request.Form["cmd"]);

	if(s_cmd == "Add")
	{
		if(Request.Form["shName"] != null && Request.Form["shName"] != "")
			DoAddNewShip();
	}
	else if(s_cmd =="Modify")
	{
		if(Request.Form["shName"] != null && Request.Form["shName"] != "")
		UpdateShip();
	}
	else if(s_cmd =="Delete")
	{
		DeleteShip();
	}

	Response.Write("<br><hr3><center><b><font size=+1>Shipping Company</font></b></center></h3>");
	Response.Write("<table width=90%  align=center valign=top cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td width=60% align=center valign=top>");
	LoadExistingShipCom();

	Response.Write("</td><td width=40% align=center valign=top>");

	AddNewShipCom();

	Response.Write("</td></tr></table>");

	return;
}

bool DoAddNewShip()
{
	string sc = "INSERT INTO ship (name, phone, web)  VALUES ('" + Request.Form["shName"];
		sc += "', '" + Request.Form["shPH"] + "', '" + Request.Form["shSite"] + "')";
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
//DEBUG("here = ", 0);
	return true;
}


bool UpdateShip()
{
	string sc = "UPDATE ship SET name='" + Request.Form["shName"] + "', phone='" + Request.Form["shPH"];
		sc += "', web='" + Request.Form["shSite"] + "' WHERE id=" + Request.Form["rid"];

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
	return true;	
}

bool DeleteShip()
{
	string sc = "DELETE FROM ship WHERE id=" + Request.Form["rid"];
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
	return true;
}

void LoadExistingShipCom()
{
	if(!GetExistingShips())
		return;
	
	Response.Write("<table width=100% valign=top cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<th width=5%>ID#</th>");
	Response.Write("<th width=30%>Name</th>");
	Response.Write("<th width=25%>Phone</th>");
	Response.Write("<th width=40%>Web Site</th>");
	Response.Write("</tr>\r\n");

	for(int i=0; i<dst.Tables["shipcom"].Rows.Count;i++)
	{
		DataRow dr = dst.Tables["shipcom"].Rows[i];
		Response.Write("<tr><td align=center><b>" + dr["id"].ToString() + "</b></td>");
		Response.Write("<td align=center><b>");
		Response.Write("<a href='newship.aspx?t=m&i=" + dr["id"].ToString() + "'>");
		Response.Write(dr["name"].ToString() + "</a></b></td>");
		Response.Write("<td align=left>" + dr["phone"].ToString() + "</td>");	
		Response.Write("<td align=left>" + dr["web"].ToString() + "</td></tr>\r\n");
	}
	Response.Write("</table>");

	return;
}

void AddNewShipCom()
{

	string s_name = "";
	string s_phone = "";
	string s_web = "";

	string s_TblName = "Add";

	if(Request.QueryString["t"] == "m")
	{
		s_TblName = "Modify";
		if(Request.QueryString["i"] != null)
		{

			if(!GetSelectedRow())
				return;
			s_name = dsi.Tables["selectedship"].Rows[0]["name"].ToString();
			s_phone = dsi.Tables["selectedship"].Rows[0]["phone"].ToString();
			s_web = dsi.Tables["selectedship"].Rows[0]["web"].ToString();
		}
	}
	Response.Write("<form name=frmAdd method=post action=newship.aspx>");
	Response.Write("<table width=95% valign=top align=right cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 ");
	Response.Write(" style=\"border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<td colspan=2 align=center><b>" + s_TblName + " Record</b></td></tr>");
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD><b>Name:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=shName value='");
	Response.Write(s_name + "'></td></tr>\r\n");
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD><b>Phone #:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=shPH value='");
	Response.Write(s_phone + "'></td></tr>\r\n");
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD><b>Wed Site:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=shSite value='");
	Response.Write(s_web + "'></td></tr>\r\n");
	Response.Write("<tr><td colspan=2 bgcolor=#FFFFFF align=center><br>");
	Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' " + s_TblName + " '");
	Response.Write(" OnClick=window.location=('newship.aspx')>");

	Response.Write("&nbsp;&nbsp;&nbsp");
	Response.Write("<input type=button style='font-size:8pt;font-weight:bold' name=clear value=' Cancel '");
	Response.Write(" OnClick=window.location=('newship.aspx')>");

	if(Request.QueryString["t"] == "m")
	{
		Response.Write("&nbsp;&nbsp;&nbsp");
		Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' Delete '");
		Response.Write(" OnClick=window.location=('newship.aspx')><input type=hidden name=rid value='");
		Response.Write(Request.QueryString["i"] + "'>");
	}
	
	Response.Write("</td></tr>");
	Response.Write("</table></form>");

	return;
}

bool GetSelectedRow()
{
	string sc = "SELECT * FROM ship WHERE id = " + Request.QueryString["i"];
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi, "selectedship");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows != 1)
		return false;

	return true;
}

bool GetExistingShips()
{
	string sc = "SELECT * FROM ship";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "shipcom");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>
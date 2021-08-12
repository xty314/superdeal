<script runat=server>
DataSet dst = new DataSet();
DataSet dsi = new DataSet();
string m_sActivatedButton = "De-Activate This Branch";
bool m_bActivated = false;
//bool m_bEZNZAdmin = false;
string g_Modify = "";
string i_branch ="";

void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
	if(!SecurityCheck("administrator"))
		return;

if(Session["email"].ToString().IndexOf("@eznz.com") >= 0)
		m_bEZNZAdmin = true;
    g_Modify = Request.QueryString["t"];
	i_branch = Request.QueryString["i"];  
	
	if (g_Modify =="" || i_branch =="")
	{ 
	if(!m_bEZNZAdmin )
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=1&t=m \">");
}
    
	/////////////////////////////////////////////// C M///////////
	string s_cmd = "";
	if(Request.Form["cmd"] != null)
	{
		s_cmd = Request.Form["cmd"];
		Trim(ref s_cmd);
	}
	//if(!MyBooleanParse(GetSiteSettings("display_branch_list_setting", "0", true)) && Session["email"].ToString().IndexOf("@eznz.com") <= 0)
	//{
	//	PrintAdminHeader();
	//	PrintAdminMenu();
	//	return;
	//}
	if(s_cmd == "Add")
	{
		if(DoAddNewBranch())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + Request.QueryString["i"] +"&t="+ Request.QueryString["t"] +" \">");
		return;
	}
	else if(s_cmd =="Modify")
	{
		if(UpdateBranch())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + Request.QueryString["i"] +"&t="+ Request.QueryString["t"] +" \">");
		return;
	}
	else if(s_cmd =="Delete")
	{
		if(DeleteBranch())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + Request.QueryString["i"] +"&t="+ Request.QueryString["t"] +" \">");
		return;
	}
	else if(s_cmd == "De-Activate This Branch" || s_cmd == "Activate This Branch")
	{
		if(ActivateBranch())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + Request.QueryString["i"] +"&t="+ Request.QueryString["t"] +" \">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><hr3><center><b><font size=+1>Branch List</font></b></center></h3>");
	Response.Write("<table width=90%  align=center valign=top cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td width=60% align=center valign=top>");
	LoadExistingBranch();
	Response.Write("</td><td width=40% align=center valign=top>");

	PrintOneBranch();

	Response.Write("</td></tr></table>");

	PrintAdminFooter();
}

bool DoAddNewBranch()
{
	string id = "";
	string name = Request.Form["name"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string city = Request.Form["city"];
	string country = Request.Form["country"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];
	string postal1 = Request.Form["postal1"];
	string postal2 = Request.Form["postal2"];
	string postal3 = Request.Form["postal3"];
    string pos_header = Request.Form["pos_header"];
	
	if(name.Length >= 49)
		name = name.Substring(0, 49);
	if(address1.Length >= 49)
		address1 = address1.Substring(0, 49);
	if(address2.Length >= 49)
		address2 = address2.Substring(0, 49);
	if(address3.Length >= 49)
		address3 = address3.Substring(0, 49);
	if(city.Length >= 49)
		city = city.Substring(0, 49);
	if(country.Length >= 49)
		country = country.Substring(0, 49);
	if(phone.Length >= 49)
		phone = phone.Substring(0, 49);
	if(fax.Length >= 49)
		fax = fax.Substring(0, 49);
	if(postal1.Length >= 49)
		postal1 = postal1.Substring(0, 49);
	if(postal2.Length >= 49)
		postal2 = postal2.Substring(0, 49);
	if(postal3.Length >= 49)
		postal3 = postal3.Substring(0, 49);
		
	if(pos_header.Length >= 49)
		pos_header = pos_header.Substring(0, 49);
	string sc = " BEGIN TRANSACTION IF NOT EXISTS(SELECT * FROM branch WHERE name='" + EncodeQuote(name) + "') ";
	sc += " INSERT INTO branch (name, address1, address2, address3, city, country, phone, fax, postal1, postal2, postal3, branch_header, branch_footer, branch_pos_reciept_header) ";
	sc += " VALUES( ";
	sc += "'" + EncodeQuote(name) + "' ";
	sc += ", '" + EncodeQuote(address1) + "' ";
	sc += ", '" + EncodeQuote(address2) + "' ";
	sc += ", '" + EncodeQuote(address3) + "' ";
	sc += ", '" + EncodeQuote(city) + "' ";
	sc += ", '" + EncodeQuote(country) + "' ";
	sc += ", '" + EncodeQuote(phone) + "' ";
	sc += ", '" + EncodeQuote(fax) + "' ";
	sc += ", '" + EncodeQuote(postal1) + "' ";
	sc += ", '" + EncodeQuote(postal2) + "' ";
	sc += ", '" + EncodeQuote(postal3) + "' ";
	sc += ", '" + EncodeQuote(Request.Form["branch_header"]) + "' ";
	sc += ", '" + EncodeQuote(Request.Form["branch_footer"]) + "' ";
	sc += ", '" + EncodeQuote(pos_header) + "' ";
	sc += ") ";
	sc += " COMMIT ";
	
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
//insert all the stock to all the latest branch
	sc = " BEGIN TRANSACTION INSERT INTO stock_qty (code, branch_id, qty, supplier_price, average_cost )";
	sc += " SELECT code, (SELECT TOP 1 id FROM branch ORDER BY id DESC) , 0, supplier_price, average_cost ";
	sc += " FROM stock_qty WHERE branch_id = 1 ";
	sc += " COMMIT ";
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

bool UpdateBranch()
{
	string id = Request.Form["id"];
	string name = Request.Form["name"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string city = Request.Form["city"];
	string country = Request.Form["country"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];
	string postal1 = Request.Form["postal1"];
	string postal2 = Request.Form["postal2"];
	string postal3 = Request.Form["postal3"];
    string pos_header = Request.Form["pos_header"];
	
	if(name.Length >= 49)
		name = name.Substring(0, 49);
	if(address1.Length >= 49)
		address1 = address1.Substring(0, 49);
	if(address2.Length >= 49)
		address2 = address2.Substring(0, 49);
	if(address3.Length >= 49)
		address3 = address3.Substring(0, 49);
	if(city.Length >= 49)
		city = city.Substring(0, 49);
	if(country.Length >= 49)
		country = country.Substring(0, 49);
	if(phone.Length >= 49)
		phone = phone.Substring(0, 49);
	if(fax.Length >= 49)
		fax = fax.Substring(0, 49);
	if(postal1.Length >= 49)
		postal1 = postal1.Substring(0, 49);
	if(postal2.Length >= 49)
		postal2 = postal2.Substring(0, 49);
	if(postal3.Length >= 49)
		postal3 = postal3.Substring(0, 49);
	if(pos_header.Length >= 49)
		pos_header = pos_header.Substring(0, 49);

	if(id == null || id == "")
	{
		Response.Write("<br><center><h3>Error, no ID</h3>");
		return false;
	}

	string sc = "UPDATE branch SET ";
	sc += " name='" + EncodeQuote(name) + "' ";
	sc += ", address1 = '" + EncodeQuote(address1) + "' ";
	sc += ", address2 = '" + EncodeQuote(address2) + "' ";
	sc += ", address3 = '" + EncodeQuote(address3) + "' ";
	sc += ", city = '" + EncodeQuote(city) + "' ";
	sc += ", country = '" + EncodeQuote(country) + "' ";
	sc += ", phone = '" + EncodeQuote(phone) + "' ";
	sc += ", fax = '" + EncodeQuote(fax) + "' ";
	sc += ", postal1 = '" + EncodeQuote(postal1) + "' ";
	sc += ", postal2 = '" + EncodeQuote(postal2) + "' ";
	sc += ", postal3 = '" + EncodeQuote(postal3) + "' ";
	sc += ", branch_pos_reciept_header = '" + EncodeQuote(pos_header) + "' ";
	sc += ", branch_header = '" + EncodeQuote(Request.Form["branch_header"]) + "' ";
	sc += ", branch_footer = '" + EncodeQuote(Request.Form["branch_footer"]) + "' ";
	sc += " WHERE id = " + id;

	//update all stock qty
	sc += " INSERT INTO stock_qty (code, branch_id, qty, supplier_price, average_cost) ";
	sc += " SELECT s.code, '" + id +"', 0, s.supplier_price, s.average_cost ";
	sc += " FROM stock_qty s WHERE branch_id = 1 and code NOT IN ";
	sc += " (SELECT code FROM stock_qty WHERE branch_id = "+ id +")";
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

bool DeleteBranch()
{
	string id = Request.Form["id"];
	if(id == null || id == "")
	{
		Response.Write("<br><center><H3>Error, no id");
		return false;
	}

	string sc = "DELETE FROM branch WHERE id = " + id;
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

bool ActivateBranch()
{
	string id = Request.Form["id"];
	if(id == null || id == "")
	{
		Response.Write("<br><center><H3>Error, no id");
		return false;
	}

	string sc = " IF(SELECT activated FROM branch WHERE id =" + id +") = 0 ";
	sc += " BEGIN UPDATE branch SET activated = 1 WHERE id = " + id +" END "; 
	sc += " ELSE BEGIN UPDATE branch SET activated = 0 WHERE id = " + id +" END "; 
//DEBUG("sc =", sc);
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

void LoadExistingBranch()
{
	if(!GetExistingBranch())
	return;
	
	Response.Write("<table width=100% valign=top cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<th width=5%>ID</th>");
	Response.Write("<th nowrap>Name</th>");
	Response.Write("<th>City</th>");	
	Response.Write("<th>Country</th>");
	if(Session["email"].ToString().IndexOf("@eznz.com") >= 0)
		Response.Write("<th>Activated</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>\r\n");

	for(int i=0; i<dst.Tables["branch"].Rows.Count;i++)
	{
		DataRow dr = dst.Tables["branch"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string city = dr["city"].ToString();
		string country = dr["country"].ToString();
		string pos_header = dr["branch_pos_reciept_header"].ToString();
		string activated = dr["activated"].ToString();
		if(!bool.Parse(activated))
		{
			if(Session["email"].ToString().IndexOf("@eznz.com") < 0)
			{
				continue;			
			}
		}
		//else
		{
			Response.Write("<tr>");
			Response.Write("<td>" + id + "</td>");
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td>" + city + "</td>");
			Response.Write("<td>" + country + "</td>");
			if(Session["email"].ToString().IndexOf("@eznz.com") >= 0)
					Response.Write("<td>" + activated + "</td>");
			Response.Write("<td align=right><a href=?t=m&i=" + id + " class=o>Edit</a></td>");
			Response.Write("</tr>\r\n");
		}
		
		
	}
	Response.Write("</table>");

	return;
}

void PrintOneBranch()
{
	string id = "";
	string name = "";
	string address1 = "";
	string address2 = "";
	string address3 = "";
	string city = "";
	string country = "";
	string phone = "";
	string fax = "";
	string postal1 = "";
	string postal2 = "";
	string postal3 = "";
	string branch_header = "";
	string branch_footer = "";
    string pos_header ="";
	string s_TblName = "Add";

	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "")
	{
		s_TblName = "Modify";
		if(Request.QueryString["i"] != null && Request.QueryString["i"] != "")
		{
			if(!GetSelectedRow())
				return;
			DataRow dr = dsi.Tables["selected"].Rows[0];
			id = dr["id"].ToString();
			name = dr["name"].ToString();
			address1 = dr["address1"].ToString();
			address2 = dr["address2"].ToString();
			address3 = dr["address3"].ToString();
			city = dr["city"].ToString();
			country = dr["country"].ToString();
			phone = dr["phone"].ToString();
			fax = dr["fax"].ToString();
			postal1 = dr["postal1"].ToString();
			postal2 = dr["postal2"].ToString();
			postal3 = dr["postal3"].ToString();
			pos_header = dr["branch_pos_reciept_header"].ToString();
			branch_header = dr["branch_header"].ToString();
			branch_footer = dr["branch_footer"].ToString();
			m_bActivated = bool.Parse(dr["activated"].ToString());
			if(!m_bActivated)
				m_sActivatedButton = "Activate This Branch";
			else
				m_sActivatedButton = "De-Activate This Branch";

		}
	}
	Response.Write("<form name=frmAdd method=post action=branch.aspx?i="+ id +"&t="+ Request.QueryString["t"] +">");
	Response.Write("<table width=95% valign=top align=right cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 ");
	Response.Write(" style=\"border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	string button ="" ;
	string c_button = "";
if(!m_bEZNZAdmin){
   if(s_TblName =="Add"){
      s_TblName ="";
	  button="";
	  c_button="";
	  }else{
	 s_TblName =s_TblName;	
	  button = "<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' " + s_TblName + " ' "+ Session["button_style"] +" >";
	  c_button = "<input type=button style='font-size:8pt;font-weight:bold' name=clear value=' Cancel ' "+ Session["button_style"] +"  OnClick=window.location=('branch.aspx')>";
	  }
}
if(m_bEZNZAdmin){
  button = "<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' " + s_TblName + " ' "+ Session["button_style"] +" >";
	  c_button = "<input type=button style='font-size:8pt;font-weight:bold' name=clear value=' Cancel ' "+ Session["button_style"] +"  OnClick=window.location=('branch.aspx')>";}
	  
	Response.Write("<td colspan=2 align=center><b>" + s_TblName + " Branch</b></td></tr>");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD><b>Name:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=name value='");
	Response.Write(name + "'></td></tr>\r\n");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.frmAdd.name.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Address</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=address1 value='");
	Response.Write(address1 + "'></td></tr>\r\n");
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Address</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=address2 value='");
	Response.Write(address2 + "'></td></tr>\r\n");
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Address</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=address3 value='");
	Response.Write(address3 + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>City</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=city value='");
	Response.Write(city + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Country</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=country value='");
	Response.Write(country + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Phone</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=phone value='");
	Response.Write(phone + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Fax</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=fax value='");
	Response.Write(fax + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Postal</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=postal1 value='");
	Response.Write(postal1 + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Postal</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=postal2 value='");
	Response.Write(postal2 + "'></td></tr>\r\n");

	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Postal</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=postal3 value='");
	Response.Write(postal3 + "'></td></tr>\r\n");
	
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>POS Receipt Header</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name='pos_header' value='");
	Response.Write(pos_header + "'></td></tr>\r\n");
    


	//print branch header in sales orders or sales invoice *********
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Invoice Header<br>for Branch<br>(Please Use HTML Code)</b></td>");
	Response.Write("<td width=70% align=center><textarea name=branch_header name=branch_header rows=5 cols=50>"+ branch_header +"</textarea>");
	Response.Write("</td></tr>\r\n");

	//print branch footer in sales orders or sales invoice *********
	Response.Write("<tr><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Invoice Footer<br>for Branch<br>(Please Use HTML Code)</b></td>");
	Response.Write("<td width=70% align=center><textarea name=branch_footer name=branch_footer rows=5 cols=50>"+ branch_footer +"</textarea>");
	Response.Write("</td></tr>\r\n");

	Response.Write("<tr><td colspan=2 bgcolor=#FFFFFF align=center><br>");
	Response.Write(button);
	Response.Write(c_button);
	//Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' " + s_TblName + " ' "+ Session["button_style"] +" >");
	//Response.Write("<input type=button style='font-size:8pt;font-weight:bold' name=clear value=' Cancel '");
	//Response.Write(" "+ Session["button_style"] +"  OnClick=window.location=('branch.aspx')>");

	//if(MyIntParse(Session[m_sCompanyName + "AccessLevel"].ToString()) == 10 && Session["email"].ToString().IndexOf("@eznz.com") >= 0)
	//{
		if(id != "" && id != "1")
		Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' onclick=\"return confirm('Are you Sure to Continue!!!'); \" name=cmd value='"+ m_sActivatedButton +"' "+ Session["button_style"] +" >"); //onclick=\"window.open('"+ Request.ServerVarialbes["URL"] +"?r="+DateTime.Now.ToOADate() +"&activated="+ !m_bActivated +">");
	//}
//	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "")
//	{
//		Response.Write("&nbsp;&nbsp;&nbsp");
//		Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' Delete ' "+ Session["button_style"] +" >");
		Response.Write("<input type=hidden name=id value='" + Request.QueryString["i"] + "'>");
//	}
	
	Response.Write("</td></tr>");
	Response.Write("</table></form>");

	return;
}

bool GetSelectedRow()
{
	string sc = "SELECT * FROM branch WHERE id = " + Request.QueryString["i"];
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi, "selected");
	}
	catch(Exception e) 
	{
		string error = e.ToString();
		DEBUG("erere =", error);
		ShowExp(sc, e);
		return false;
	}

	if(rows != 1)
		return false;

	return true;
}

bool GetExistingBranch()
{
	string sc = "SELECT branch_header, branch_footer, activated, * FROM branch ORDER BY id";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "branch");
	}
	catch(Exception e) 
	{
		string err = e.ToString();
//	DEBUG("erer =", err);
		if(err.IndexOf("Invalid column name 'branch_header'")>= 0)
		{
			sc = " ALTER TABLE branch add branch_header [text], ADD activated [bit] NOT NULL DEFAULT(1) ";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myConnection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}
		if(err.IndexOf("Invalid column name 'branch_footer'")>= 0)
		{
			sc = " ALTER TABLE branch add branch_footer [text] ";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myConnection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}
		if(err.IndexOf("Invalid column name 'activated'")>= 0)
		{			
			sc = " ALTER TABLE branch add activated [bit] not  null default(1) ";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myConnection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}
		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>
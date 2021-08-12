<script runat=server>
const string tableTitle = "Bank Account";
const string thisurl = "bankacc.aspx";

string m_sClass1 = "";
string m_sClass2 = "";
string m_sClass3 = "";
string m_sClass4 = "";
string m_sClass1New = "";
string m_sClass2New = "";
string m_sClass3New = "";
string m_sClass4New = "";
string m_sName1 = "";
string m_sName2 = "";
string m_sName3 = "";
string m_sName4 = "";
string m_sName1New = "";
string m_sName2New = "";
string m_sName3New = "";
string m_sName4New = "";

double m_dOpenBal = 0;
double m_dBalance = 0;
bool m_bActive = true;
bool m_bShowBalance = true;

int page = 1;
const int m_nPageSize = 15;	//how many rows in one page

bool m_bCheckAccess = false;

DataSet dst = new DataSet();		//for creating Temp tables templated on an existing sql table
//=======================================================================================
void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("accountant"))
		return;

	m_bCheckAccess = bCheckAccessLevel(m_bCheckAccess);
//DEBUG("m_bCheckAcces = ", m_bCheckAccess.ToString());
	m_sClass1 = Request.QueryString["c"];
	m_sClass2 = Request.QueryString["c2"];
	m_sClass3 = Request.QueryString["c3"];
	m_sClass4 = Request.QueryString["c4"];
	if(m_sClass1 == null || m_sClass1 == "")
		m_sClass1 = "1";
	if(m_sClass2 == null || m_sClass2 == "")
		m_sClass2 = "1";
	if(m_sClass3 == null || m_sClass3 == "")
		m_sClass3 = "1";
	if(m_sClass4 == null || m_sClass4 == "")
		m_sClass4 = "1";

	m_sClass1New = m_sClass1;
	m_sClass2New = m_sClass2;
	m_sClass3New = m_sClass3;
	m_sClass4New = m_sClass4;
	if(Request.QueryString["new_c"] != null && Request.QueryString["new_c"] != "")
		m_sClass1New = Request.QueryString["new_c"];
	if(Request.QueryString["new_c2"] != null && Request.QueryString["new_c2"] != "")
		m_sClass2New = Request.QueryString["new_c2"];
	if(Request.QueryString["new_c3"] != null && Request.QueryString["new_c3"] != "")
		m_sClass3New = Request.QueryString["new_c3"];
	if(Request.QueryString["new_c4"] != null && Request.QueryString["new_c4"] != "")
		m_sClass4New = Request.QueryString["new_c4"];

	PrintAdminHeader();
	PrintAdminMenu();

	string type = Request.QueryString["t"];
	string cmd = Request.Form["cmd"];
	string cur_acc="";

	if(Request.QueryString["luri"] != null && Request.QueryString["luri"] != "")
		Session["acc_l_uri"] = Request.QueryString["luri"];
	if(!GetCurrentAccounts(ref cur_acc))
	{
		type="e";
	}
	if(type == "m")
	{
		string ru = "account.aspx?t=e&edit=1&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4;
		ru += "&new_c=" + Request.QueryString["new_c"];
		ru += "&new_c2=" + Request.QueryString["new_c2"];
		ru += "&new_c3=" + Request.QueryString["new_c3"];
//		ru += "&new_c4=" + Request.QueryString["new_c4"];
		if(cmd == "Move")
		{
			if(DoMoveAccount())
			{
				ru = "account.aspx?t=m&c=" + m_sClass1New + "&c2=" + m_sClass2New;
				ru += "&c3=" + m_sClass3New + "&c4=" + m_sClass4New;
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + ru + "\">");
			}
		}
		else
		{
			DrawMoveTable();
		}
		return;
	}
	if(type == "e")
	{
		if(cmd == "Update")
		{
			if(DoUpdateAccount()) //false means update
			{
				string ru = "account.aspx?t=e&edit=1&c=" + m_sClass1 + "&c2=" + m_sClass2;
				ru += "&c3=" + m_sClass3 + "&c4=" + m_sClass4;
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + ru + "\">");
			}
			return;
		}
		else if(cmd == "Delete")
		{
			if(Request.Form["confirm_delete"] != "on")
			{
				Response.Write("<br><center><h3>Please tick confirm delete</h3>");
				Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
			}
			if(DoDeleteAccount()) 
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=account.aspx?c=" + m_sClass1 + "\">");
			return;
		}
		else if(cmd =="Add")
		{
			if(DoAddNewAccount())
			{
				Response.Write("<br><center><h3>New account added, please wait 1 second...</h3>");
				Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=account.aspx?c=" + m_sClass1 + "\">");
				return;
			}
		}
		else if(cmd =="Back")
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=account.aspx\">");
		}
		else
		{
			if(Request.QueryString["edit"] == "1")
				DrawEditTable();
			else
				DrawAddNewTable();
		}
		
		return;
	}
	
	DrawBrowseTable();

	//PrintAdminFooter();
	LFooter.Text = m_sAdminFooter;

}
//--------------------------------------------------------------------------------
void DrawBrowseTable()
{
	if(!GetAllTopClassAccount())
		return;
	
	string s = "";
	if(GetPartAccount(ref s))
		DrawTable(s);
	return;
}
//--------------------------------------------------------------------------------
bool GetAllTopClassAccount()
{
	string sc = "SELECT DISTINCT(class1), name1";
	sc += " FROM account ";
	sc += " ORDER BY class1, name1 ";//class2, class3, class4 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "topclass") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}
//--------------------------------------------------------------------------------
bool DrawTable(string ACCNAME)
{
	Boolean bRet = true;
	//Table Tile
	//Response.Write("<br><center><h3>" + tableTitle + "</center></h3>");
	//Response.Write("<br><img border=0 src='../../i/ba.gif'>");
	Response.Write("<br><center><h4><b>ACCOUNT LIST</b></h4></center>");
	//Class1 list
	DrawTopClassLink();
	//Account browse table starts

	DrawTableHeader(ACCNAME);
	//Account details
	DataRow dr;
	Boolean alterColor = true;
	
	string titlelvl1 = dst.Tables["partaccount"].Rows[0]["class1"].ToString() + "000" + " - ";
	titlelvl1 += dst.Tables["partaccount"].Rows[0]["name1"].ToString();
	Response.Write("<tr><td colspan=5><b>" + titlelvl1 + "</b></td></tr>");

	string class_flag = dst.Tables["partaccount"].Rows[0]["class1"].ToString();
	string id = "";
	if(!GetLevel2PartAccount(class_flag))
	{
		return false;
	}
	else
	{
		bool bAlter = true;
		for(int i=0; i<dst.Tables["lvl2PaAcc"].Rows.Count; i++)
		{
			dr = dst.Tables["lvl2PaAcc"].Rows[i];
			string class_flag2 = dr["class2"].ToString();
		
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
			bAlter = !bAlter;
			Response.Write("><td colspan=5 ");
			Response.Write("><b>&nbsp;&nbsp;&nbsp" + class_flag + class_flag2 + "00");
			Response.Write(" - " + dr["name2"].ToString() + "</b></td></tr>");

			if(!GetLevel3PartAccount(class_flag, class_flag2))
				return false;
			else
			{
				for(int j=0; j<dst.Tables["lvl3PaAcc"].Rows.Count; j++)
				{
					DataRow dr2 = dst.Tables["lvl3PaAcc"].Rows[j];
					string class_flag3 = dr2["class3"].ToString();
					Response.Write("<tr");
					if(bAlter)
						Response.Write(" bgcolor=#EEEEEe ");
					bAlter = !bAlter;
					Response.Write(" ><td colspan=5><b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp");
					Response.Write(class_flag + class_flag2 + class_flag3 + "0");
					Response.Write(" - " + dr2["name3"].ToString() + "</b></td></tr>");

					if(!GetLevel4PartAccount(class_flag, class_flag2, class_flag3))
						return false;
					else
					{
						for(int k=0; k<dst.Tables["lvl4PaAcc"].Rows.Count; k++)
						{
							DataRow dr3 = dst.Tables["lvl4PaAcc"].Rows[k];
							string acc = class_flag + class_flag2 + class_flag3 + dr3["class4"].ToString();
							id = dr3["id"].ToString();
							bool bShowBalance = MyBooleanParse(dr3["show_balance"].ToString());
							Response.Write("<tr");
							//if(bAlter)
							//	Response.Write(" bgcolor=#EEEEEE ");
							//bAlter = !bAlter;
							
							Response.Write("><td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp");
							//Response.Write("<a href="+ Request.ServerVariables["URL"] +"?t=e&a="+ class_flag +"");
							//Response.Write("&a2="+ class_flag2 +"&a3="+ class_flag3 +"&a4="+ dr3["class4"].ToString() +"");
							Response.Write("<a href="+ Request.ServerVariables["URL"] +"?t=e&edit=1&c="+ class_flag +"");
							Response.Write("&c2="+ class_flag2 +"&c3="+ class_flag3 +"&c4="+ dr3["class4"].ToString() +"");
							Response.Write(" class=o>");
							Response.Write(acc + " - " + dr3["name4"].ToString() + "</a>");
							Response.Write("&nbsp; </td><td><a title='View Transaction History' href=acc_report.aspx?id="+ id + " class=o target=_blank><font color=green>Trans_History</a>");
							Response.Write("</td>");
							
							string activity = dr3["active"].ToString();
							if(activity=="True")
								activity = "Active";
							else
								activity = "Inactive";

							string balance = double.Parse(dr3["balance"].ToString()).ToString("c");
							string open_balance = double.Parse(dr3["opening_balance"].ToString()).ToString("c");
							Response.Write("<td align=right>" + activity + "</td>");
							Response.Write("<td align=right>" + open_balance + "</td>");
							if(bShowBalance)
								Response.Write("<td align=right>" + balance + "</td></tr>");
							else
								Response.Write("<td align=right>Check Transaction History</td></tr>");
							//Response.Write("<td align=right><a title='view current balance' href='acc_report.aspx?st="+ id +"&r="+ DateTime.Now.ToOADate() +"' target=_blank class=o>View</a></td></tr>");

						}

						dst.Tables["lvl4PaAcc"].Clear();
					}
				}
		
				dst.Tables["lvl3PaAcc"].Clear();
			}
		}
		dst.Tables["lvl2PaAcc"].Clear();
	}

	
	//add new button
/*	
	Response.Write("<tr><td>");
	Response.Write("</td>");
	Response.Write("<td colspan=4 align=right>");
	Response.Write("<button onclick=\"window.location=('account.aspx?t=e&c="+ Request.QueryString["c"] +"')\" "+ Session["button_style"] +">Add New</button>");
	Response.Write("</td></tr>");
*/
	Response.Write("</table>\r\n");
	Response.Write("</form><br><br>");
	
	return true;
}
//--------------------------------------------------------------------------------

//--------------------------------------------------------------------------------
bool GetLevel2PartAccount(string sflag)
{
	string sc = "SELECT DISTINCT class1, class2, name2 FROM account WHERE class1 =" + sflag;
	sc += " ORDER BY class1, class2 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "lvl2PaAcc") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

//--------------------------------------------------------------------------------

bool GetLevel3PartAccount(string flag1, string flag2)
{
	string sc = "SELECT DISTINCT class1, class2, class3, name3 FROM account ";
	sc += " WHERE (class1 = " + flag1 +") AND (class2 = " + flag2 +")";
	sc += " ORDER BY class1, class2, class3 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "lvl3PaAcc") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

//--------------------------------------------------------------------------------
bool GetLevel4PartAccount(string flag1, string flag2, string flag3)
{
	string sc = "SELECT id, class1, class2, class3, class4, name4, active, balance, opening_balance, show_balance FROM account ";
	sc += "WHERE (class1 = " + flag1 +") AND (class2 = " + flag2 +") AND (class3 = " + flag3 + ")";
	sc += " ORDER BY class1, class2, class3, class4 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "lvl4PaAcc") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

//--------------------------------------------------------------------------------
string DrawTopClassLink()
{
	Response.Write("<form name=frm method=post>");
	Response.Write("<table width=96%  align=center valign=center cellspacing=0 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<tr>");
	Response.Write("<td>");
//	Response.Write("<input type=submit name=cmd value='New Finance Year' class=b>");
	Response.Write("<input type=button onclick=\"window.location=('account.aspx?t=e&c="+ Request.QueryString["c"] +"')\" class=b value='Add New'>");
	Response.Write("<input type=button onclick=\"window.location=('transfer.aspx')\" class=b value='Transfer Money'>");
	Response.Write("</td>");

	Response.Write("<td width=100% align=right>");
	string sname = "";
	Response.Write("<b>SELECT ACC:</b> <select name=slt_type onchange=\"window.location=('"+Request.ServerVariables["URL"]+"?");
	if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		Response.Write("t="+ Request.QueryString["t"] +"&");
	Response.Write("c='+this.options[this.selectedIndex].value)\" >\r\n");
	//if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		Response.Write(" <option value='all'>ALL</option>");
	for(int i=0; i<dst.Tables["topclass"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["topclass"].Rows[i];
		Response.Write("<option value=" + dr["class1"].ToString() + "");
		if(Request.QueryString["c"] == dr["class1"].ToString())
		{
			Response.Write(" selected ");
			sname = dr["name1"].ToString();
		}
		Response.Write(">" + dr["name1"].ToString() + "</option>");
		//Response.Write("<a href=account.aspx?c=" + dr["class1"].ToString() + ">" + dr["name1"].ToString() + "</a>&nbsp;&nbsp;");
		
	
//DEBUG("Rows=", i);	
	}
	Response.Write("</select>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	//Response.Write("</form>");
	return sname;
}

string DrawSecondClassLink()
{
	string class2 = "";
	string class1 = "";
	if(Request.QueryString["c"] != "" && Request.QueryString["c"] != null)
		class1 = Request.QueryString["c"];
	if(Request.QueryString["c2"] != "" && Request.QueryString["c2"] != null)
		class2 = Request.QueryString["c2"];

	string sc = "SELECT DISTINCT(class2), name2";
	sc += " FROM account ";
	if(class1 != "" && class1 != "all")
		sc += " WHERE class1 = "+ class1 +" ";
	//if(class2 != "" && class2 != "all")
	//	sc += " AND class2 = "+ class2 +" ";
	sc += " ORDER BY class2, name2";
//DEBUG(" sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "secondclass");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		//return;
	}

	string sname = "";
	Response.Write("<form name=frm method=post>");
	//Response.Write("<table width=96%  align=center valign=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	//Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	//Response.Write("<tr><td>");
	Response.Write("<div align=right>");
	Response.Write("<select name=slt_type onchange=\"window.location=('"+Request.ServerVariables["URL"]+"?");
	if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		Response.Write("t="+ Request.QueryString["t"] +"&");
	if(Request.QueryString["c"] != "" && Request.QueryString["c"] != null)
		Response.Write("c="+ Request.QueryString["c"] +"&");
	Response.Write("c2='+this.options[this.selectedIndex].value)\" >\r\n");
	Response.Write(" <option value='all'>ALL</option>");
	for(int i=0; i<dst.Tables["secondclass"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["secondclass"].Rows[i];
		Response.Write("<option value=" + dr["class2"].ToString() + "");
		if(Request.QueryString["c2"] == dr["class2"].ToString())
		{
			Response.Write(" selected ");
			sname = dr["name2"].ToString();
		}
		Response.Write(">" + dr["name2"].ToString() + "</option>");

	}
	Response.Write("</select>");
	Response.Write("</div>");
	//Response.Write("</td></tr>");
	//Response.Write("</table>");
	return sname;
}
string DrawThirdClassLink()
{
	string class2 = "";
	string class1 = "";
	string class3 = "";
	if(Request.QueryString["c"] != "" && Request.QueryString["c"] != null)
		class1 = Request.QueryString["c"];
	if(Request.QueryString["c2"] != "" && Request.QueryString["c2"] != null)
		class2 = Request.QueryString["c2"];
	if(Request.QueryString["c3"] != "" && Request.QueryString["c3"] != null)
		class3 = Request.QueryString["c3"];
	string sc = "SELECT DISTINCT(class3), name3";
	sc += " FROM account "; 
	if(class1 != "" && class1 != "all")
		sc += " WHERE class1 = "+ class1 +" ";
	if(class2 != "" && class2 != "all")
		sc += " AND class2 = "+ class2 +" ";
	//if(class3 != "" && class3 != "all")
	//	sc += " AND class3 = "+ class3 +" ";
	sc += " ORDER BY class3, name3";
//DEBUG(" sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "thirdclass");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		//return;
	}
	
	string sname = "";
	Response.Write("<form name=frm method=post>");
	//Response.Write("<table width=96%  align=center valign=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	//Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr><td>");
	Response.Write("<div align=right>");
	Response.Write("<select name=slt_type onchange=\"window.location=('"+Request.ServerVariables["URL"]+"?");

	if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		Response.Write("t="+ Request.QueryString["t"] +"&");
	if(Request.QueryString["edit"] != null && Request.QueryString["edit"] != "")
		Response.Write("edit=1");
	if(Request.QueryString["c"] != "")
		Response.Write("c="+ Request.QueryString["c"] +"&");
	if(Request.QueryString["c2"] != "")
		Response.Write("c2="+ Request.QueryString["c2"] +"&");
	Response.Write("c3='+this.options[this.selectedIndex].value)\" >\r\n");
	Response.Write(" <option value='all'>ALL</option>");
	for(int i=0; i<dst.Tables["thirdclass"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["thirdclass"].Rows[i];
		Response.Write("<option value=" + dr["class3"].ToString() + "");
		if(Request.QueryString["c3"] == dr["class3"].ToString())
		{
			Response.Write(" selected ");
			sname = dr["name3"].ToString();
		}
		
		Response.Write(">" + dr["name3"].ToString() + "</option>");
		
	}
	Response.Write("</select>");
	Response.Write("</div>");
	//Response.Write("</td></tr>");
	//Response.Write("</table>");
	return sname;
}

string DrawForthClassLink()
{
	string class2 = "";
	string class1 = "";
	string class3 = "";
	string class4 = "";
	string sname = "";
	if(Request.QueryString["c"] != "" && Request.QueryString["c"] != null)
		class1 = Request.QueryString["c"];
	if(Request.QueryString["c2"] != "" && Request.QueryString["c2"] != null)
		class2 = Request.QueryString["c2"];
	if(Request.QueryString["c3"] != "" && Request.QueryString["c3"] != null)
		class3 = Request.QueryString["c3"];
	if(Request.QueryString["c4"] != "" && Request.QueryString["c4"] != null)
		class4 = Request.QueryString["c4"];

	string sc = "SELECT DISTINCT(class4), name4 ";
	sc += " FROM account "; 
	if(class1 != "" && class1 != "all")
		sc += " WHERE class1 = "+ class1 +" ";
	if(class2 != "" && class2 != "all")
		sc += " AND class2 = "+ class2 +" ";
	if(class3 != null && class3 != "" && class3 != "all")
		sc += " AND class3 = "+ class3 +" ";
	//if(class4 != "" && class4 != "all")
	//	sc += " AND class4 = "+ class4 +" ";
	sc += " ORDER BY class4, name4";
//DEBUG(" sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "forthclass");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		//return;
	}

	Response.Write("<form name=frm method=post>");
	Response.Write("<select name=slt_type onchange=\"window.location=('"+Request.ServerVariables["URL"]+"?");
	if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		Response.Write("t="+ Request.QueryString["t"] +"&");
	if(Request.QueryString["c"] != "")
		Response.Write("c="+ Request.QueryString["c"] +"&");
	if(Request.QueryString["c2"] != "")
		Response.Write("c2="+ Request.QueryString["c2"] +"&");
	if(Request.QueryString["c3"] != "")
		Response.Write("c3="+ Request.QueryString["c3"] +"&");
	Response.Write("c4='+this.options[this.selectedIndex].value)\" >\r\n");
	Response.Write(" <option value='all'>ALL</option>");
	for(int i=0; i<dst.Tables["forthclass"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["forthclass"].Rows[i];
		Response.Write("<option value=" + dr["class4"].ToString() + "");
		if(Request.QueryString["c4"] == dr["class4"].ToString())
		{
			sname = dr["name4"].ToString();
			Response.Write(" selected ");
		}
		Response.Write(">" + dr["name4"].ToString() + "</option>");
		
	}
	Response.Write("</select>");
	Response.Write("</div>");
	//Response.Write("</td></tr>");
	//Response.Write("</table>");
	return sname;
}

//--------------------------------------------------------------------------------
void DrawTableHeader(string ACCNAME)
{

	Response.Write("<table width=96%  align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	//Response.Write("<tr style=\"color:white;background-color:#666698;font-weight:bold;\">\r\n");
	Response.Write("<tr><td colspan=5><hr size=1 color=black></td></tr>");
	Response.Write("<tr>");
	Response.Write("<th align=left>&nbsp;&nbsp;ACCOUNT NAME - "+ ACCNAME.ToUpper() +"</td><td></td><th align=right>STATUS</td><th align=right>OPEN BALANCE</td><th align=right>CURRENT BALANCE</td></tr>");
	Response.Write("<tr><td colspan=5><hr size=1 color=black></td></tr>");
//	DEBUG("accname=", ACCNAME);
	//Response.Write("</table>");

}

//--------------------------------------------------------------------------------
bool GetPartAccount(ref string ACCNAME)
{	//Get sub class accounts under selected 'class1' //HG 13.Aug.2002	
	string class1 = "";

	if(Request.QueryString["c"] != null)
		class1 = Request.QueryString["c"];
	string sc = "SELECT * FROM account ";
	if(class1 != "" && class1 != "all")
	{
		sc += " WHERE class1=" + class1 + " ";
		sc += " ORDER BY class2, class3, class4";
	}
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "partaccount") > 0)
		{
			ACCNAME = dst.Tables["partaccount"].Rows[0]["name1"].ToString();
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e); 
		return false;
	}
	return false;
}
//--------------------------------------------------------------------------------
bool GetCurrentAccounts(ref string cur_acc)
{
	string sc = "SELECT * FROM account ";
	sc += " WHERE 1=1 ";
	if(Request.QueryString["c"] != null && Request.QueryString["c"] != "" && Request.QueryString["c"] != "all")
		sc += " AND class1 = "+ Request.QueryString["c"];
	if(Request.QueryString["c2"] != null && Request.QueryString["c2"] != "" && Request.QueryString["c2"] != "all")
		sc += " AND class2 = "+ Request.QueryString["c2"];
	if(Request.QueryString["c3"] != null && Request.QueryString["c3"] != "" && Request.QueryString["c3"] != "all")
		sc += " AND class3 = "+ Request.QueryString["c3"];
	//if(Request.QueryString["c4"] != null && Request.QueryString["c4"] != "" && Request.QueryString["c4"] != "all")
	//	sc += " AND class4 = "+ Request.QueryString["c4"];

	sc += " GROUP BY id, class1, class2, class3, class4, name1, name2, name3, ";
	sc += " name4, active, opening_balance, balance, branch_id , show_balance ";
	sc += " ORDER BY class1, class2, class3, class4 ";
//DEBUG("sc = ",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "allaccount");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int irows = dst.Tables["allaccount"].Rows.Count;
	if(irows > 0)
	{
		cur_acc = dst.Tables["allaccount"].Rows[0]["name1"].ToString();
		return true;
	}
	else
		return false;	
}

bool CheckClass4()
{
	if(dst.Tables["checkclass4"] != null)
		dst.Tables["checkclass4"].Clear();

	int nRows = 0;
	string sc = " SELECT class4 FROM account ";
	sc += " WHERE class1 = " + m_sClass1New;
	sc += " AND class2 = " + m_sClass2New;
	sc += " AND class3 = " + m_sClass3New;
	sc += " ORDER BY class4 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "checkclass4");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	string next_number = "1";
	for(int i=1; i<=nRows; i++)
	{
		DataRow dr = dst.Tables["checkclass4"].Rows[i-1];
		int nNumber = MyIntParse(dr["class4"].ToString());
		if(nNumber > i)
		{
			next_number = (nNumber - 1).ToString();
			break;
		}
		if(i == nRows)
			next_number = (nNumber + 1).ToString();
//DEBUG("i="+i.ToString(), "number="+nNumber.ToString());
	}

	m_sClass4New = next_number;
	return true;
}

bool GetNewClassNames()
{
	if(dst.Tables["getnewclassname"] != null)
		dst.Tables["getnewclassname"].Clear();

	string sc = " SELECT TOP 1 * FROM account ";
	sc += " WHERE class1 = " + m_sClass1New;
	sc += " AND class2 = " + m_sClass2New;
	sc += " AND class3 = " + m_sClass3New;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "getnewclassname") <= 0)
		{
//DEBUG("sc=", sc);
			Response.Write("<h3>Error detination class not found, please try again.</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	DataRow dr = dst.Tables["getnewclassname"].Rows[0];
	m_sName1New = dr["name1"].ToString();
	m_sName2New = dr["name2"].ToString();
	m_sName3New = dr["name3"].ToString();

	//check duplicates for name4
	for(int i=0; i<dst.Tables["getnewclassname"].Rows.Count; i++)
	{
		dr = dst.Tables["getnewclassname"].Rows[i];
		string name = dr["name4"].ToString();
		if(name == m_sName4) //already had a class4 called this name?
		{
			Response.Write("<br><br><center><h4>Error, the destination class already have an entry called " + m_sName4 + ", please change the name first or delete this class</h4>");
			Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
			return false;
		}
	}
	return true;
}

bool DoMoveAccount()
{
	m_sClass1New = Request.Form["class1"];
	m_sClass2New = Request.Form["class2"];
	m_sClass3New = Request.Form["class3"];

	if(m_sClass1 == m_sClass1New	&& m_sClass2 == m_sClass2New	&& m_sClass3 == m_sClass3New)
		return true; //no changes

	if(!CheckClass4())
		return false;

	if(!GetNewClassNames())
		return false;

	string sc = " UPDATE account SET ";
	sc += " class1 = " + m_sClass1New;
	sc += ", class2 = " + m_sClass2New;
	sc += ", class3 = " + m_sClass3New;
	sc += ", class4 = " + m_sClass4New;
	sc += ", name1 = '" + EncodeQuote(m_sName1New) + "' ";
	sc += ", name2 = '" + EncodeQuote(m_sName2New) + "' ";
	sc += ", name3 = '" + EncodeQuote(m_sName3New) + "' ";
	sc += " WHERE class1 = " + m_sClass1;
	sc += " AND class2 = " + m_sClass2;
	sc += " AND class3 = " + m_sClass3;
	sc += " AND class4 = " + m_sClass4;
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

bool DoUpdateAccount()
{
	if(Request.Form["sName1"] != "")
	{
		if(!DoChangeClassName(1))
			return false;
	}
	if(Request.Form["sName2"] != "")
	{
		if(!DoChangeClassName(2))
			return false;
	}
	if(Request.Form["sName3"] != "")
	{
		if(!DoChangeClassName(3))
			return false;
	}
	if(Request.Form["sName4"] != "")
	{
		if(!DoChangeClassName(4))
			return false;
	}

	m_dOpenBal = MyMoneyParse(Request.Form["opening_balance"]);
	double dOpenBalOld = MyMoneyParse(Request.Form["opening_balance_old"]);
	string sActive = "1";
	if(Request.Form["active"] != "on")
		sActive = "0";
	string sShowbalance = "1";
	if(Request.Form["showbalance"] != "on")
		sShowbalance = "0";

	string sc = " UPDATE account SET ";
	sc += " opening_balance = " + m_dOpenBal;
	if(m_dOpenBal != dOpenBalOld)
		sc += ", balance = balance + " + (m_dOpenBal - dOpenBalOld).ToString();
	sc += ", active = " + sActive;
	if(m_bCheckAccess)
		sc += ", show_balance = " + sShowbalance;
	sc += " WHERE class1 = " + m_sClass1;
	sc += " AND class2 = " + m_sClass2;
	sc += " AND class3 = " + m_sClass3;
	sc += " AND class4 = " + m_sClass4;
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

bool ClassNameOK(int nClass)
{
	string sc = "";
	if(nClass == 1)
	{
		sc = " SELECT * FROM account WHERE name1 = '" + m_sName1 + "' ";
	}
	else if(nClass == 2)
	{
		sc = " SELECT * FROM account WHERE class1 = " + m_sClass1;
		sc += " AND name2 = '" + m_sName2 + "' ";
	}
	else if(nClass == 3)
	{
		sc = " SELECT * FROM account WHERE class1 = " + m_sClass1 + " AND class2 = " + m_sClass2;
		sc += " AND name3 = '" + m_sName3 + "' ";
	}
	else
	{
		sc = " SELECT * FROM account WHERE class1 = " + m_sClass1 + " AND class2 = " + m_sClass2;
		sc += " AND class3 = " + m_sClass3 + " AND name4 = '" + m_sName4 + "' ";
	}
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "classnameexits") > 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetNextClass(int nClass)
{
	if(dst.Tables["getnextclass"] != null)
		dst.Tables["getnextclass"].Clear();

	int nRows = 0;
	string next_number = "1";
	string sc = "";
	if(nClass == 1)
	{
		sc = " SELECT DISTINCT class1 AS next_number FROM account ";
		sc += " ORDER BY class1 ";
	}
	else if(nClass == 2)
	{
		sc = " SELECT DISTINCT class2 AS next_number FROM account ";
		sc += " WHERE class1 = " + m_sClass1;
		sc += " ORDER BY class2 ";
	}
	else if(nClass == 3)
	{
		sc = " SELECT DISTINCT class3 AS next_number FROM account ";
		sc += " WHERE class1 = " + m_sClass1;
		sc += " AND class2 = " + m_sClass2;
		sc += " ORDER BY class3 ";
	}
	else
	{
		sc = " SELECT DISTINCT class4 AS next_number FROM account ";
		sc += " WHERE class1 = " + m_sClass1;
		sc += " AND class2 = " + m_sClass2;
		sc += " AND class3 = " + m_sClass3;
		sc += " ORDER BY class4 ";
	}
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "getnextclass");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(nRows >= 9 && nClass != 4)
	{
		Response.Write("<br><center><h3>Error, class " + nClass.ToString() + " is full.</h3>");
		return false;
	}
	for(int i=1; i<=nRows; i++)
	{
		DataRow dr = dst.Tables["getnextclass"].Rows[i-1];
		int nNumber = MyIntParse(dr["next_number"].ToString());
		if(nNumber > i)
		{
			next_number = (nNumber - 1).ToString();
			break;
		}
		if(i == nRows)
			next_number = (nNumber + 1).ToString();
//DEBUG("i="+i.ToString(), "number="+nNumber.ToString());
	}

	switch(nClass)
	{
	case 1:
		m_sClass1 = next_number;
		break;
	case 2:
		m_sClass2 = next_number;
		break;
	case 3:
		m_sClass3 = next_number;
		break;
	case 4:
		m_sClass4 = next_number;
		break;
	default:
		break;
	}
	return true;
}

bool DoAddNewAccount()
{
	m_sClass1 = Request.Form["class1"];
	m_sClass2 = Request.Form["class2"];
	m_sClass3 = Request.Form["class3"];
	m_sClass4 = Request.Form["class4"];

	m_sName1 = EncodeQuote(Request.Form["sname1"]);
	m_sName2 = EncodeQuote(Request.Form["sname2"]);
	m_sName3 = EncodeQuote(Request.Form["sname3"]);
	m_sName4 = EncodeQuote(Request.Form["sname4"]);
	m_dOpenBal = MyMoneyParse(Request.Form["opening_balance"]);
	string sActive = "1";
	if(Request.Form["active"] != "on")
		sActive = "0";

	if(m_sClass1 == "9999" && m_sName1 == "")
	{
		Response.Write("<br><br><center><h3>Please enter 1st class name</h3>");
		Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
		return false;
	}
	if(m_sClass2 == "9999" && m_sName2 == "")
	{
		Response.Write("<br><br><center><h3>Please enter 2nd class name</h3>");
		Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
		return false;
	}
	if(m_sClass3 == "9999" && m_sName3 == "")
	{
		Response.Write("<br><br><center><h3>Please enter 3rd class name</h3>");
		Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
		return false;
	}
	if(m_sClass4 == "9999" && m_sName4 == "")
	{
		Response.Write("<br><br><center><h3>Please enter 4th class name</h3>");
		Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
		return false;
	}

	if(m_sClass1 == "9999") //new
	{
		if(ClassNameOK(1))
		{
			if(!GetNextClass(1))
				return false;
		}
		else
		{
			Response.Write("<br><center><h3>Error, this 1st class name \"" + m_sName1 + "\" already exists.</h3>");
			return false;
		}
	}
	else
	{
		m_sName1 = Request.Form["selected_name1"];
	}

	if(m_sClass2 == "9999")
	{
		if(ClassNameOK(2))
		{
			if(!GetNextClass(2))
				return false;
		}
		else
		{
			Response.Write("<br><center><h3>Error, this 2nd class name \"" + m_sName2 + "\" already exists.</h3>");
			return false;
		}
	}
	else //no new name2
	{
		m_sName2 = Request.Form["selected_name2"];
	}

	if(m_sClass3 == "9999")
	{
		if(ClassNameOK(3))
		{
			if(!GetNextClass(3))
				return false;
		}
		else
		{
			Response.Write("<br><center><h3>Error, this 3rd class name \"" + m_sName3 + "\" already exists.</h3>");
			return false;
		}
	}
	else //now new name3
	{
		m_sName3 = Request.Form["selected_name3"];
	}

	if(m_sClass4 == "9999")
	{
		if(ClassNameOK(4))
		{
			if(!GetNextClass(4))
				return false;
		}
		else
		{
			Response.Write("<br><center><h3>Error, this 4th class name \"" + m_sName4 + "\" already exists.</h3>");
			return false;
		}
	}

	string sc = "";
	sc += " INSERT INTO account (class1, class2, class3, class4 ";
	sc += ", name1, name2, name3, name4, opening_balance, balance, active) VALUES (";
	sc += m_sClass1 + ", " + m_sClass2 + ", " + m_sClass3 + ", " + m_sClass4;
	sc += ", '" + m_sName1 + "', '" + m_sName2 + "', '" + m_sName3 + "', '" + m_sName4 + "'";
	sc += ", " + m_dOpenBal + ", " + m_dOpenBal + ", " + sActive + ") ";
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

//-----------------------------------------------------------------------------------------------------
bool DoUpdateAccount_old(bool bIsNew)
{
	string sName1 = EncodeQuote(Request.Form["eName1"]);
	string sName2 = EncodeQuote(Request.Form["eName2"]);
	string sName3 = EncodeQuote(Request.Form["eName3"]);
	string sName4 = EncodeQuote(Request.Form["eName4"]);
	string sName1_old = EncodeQuote(Request.Form["eName1_old"]);
	string sName2_old = EncodeQuote(Request.Form["eName2_old"]);
	string sName3_old = EncodeQuote(Request.Form["eName3_old"]);
	string sName4_old = EncodeQuote(Request.Form["eName4_old"]);
	string sClass1 = Request.Form["eClass1"];
	string sClass2 = Request.Form["eClass2"];
	string sClass3 = Request.Form["eClass3"];
	string sClass4 = Request.Form["eClass4"];
	string sClass1_old = Request.Form["sClass1_old"];
	string sClass2_old = Request.Form["sClass2_old"];
	string sClass3_old = Request.Form["sClass3_old"];
	string sClass4_old = Request.Form["sClass4_old"];

	//string sOpenBal = Request.Form["eOpenBal"];
	string sBalance = Request.Form["eBalance"];


//	double dBalance = double.Parse(sBalance, NumberStyles.Currency, null);

	int iActivity;
	//string branch = Request.Form["branch"];

	if(Request.Form["eActive"] == "on")
		iActivity = 1;
	else
		iActivity = 0;

	bool bUpdateClassNumber = false;
	if(sClass1 != sClass1_old || sClass2 != sClass2_old || sClass3 != sClass3_old || sClass4 != sClass4_old)
		bUpdateClassNumber = true;

	string sc = "";
	if(bIsNew || bUpdateClassNumber)
	{
		if(ClassExists(sClass1, sClass2, sClass3, sClass4))
		{
			//Response.Write("<h3>ERROR, This Account <font color=red>");
			//Response.Write(sClass1 + sClass2 + sClass3 + sClass4 +"</font> ALREADY EXISTS</h3>");
			string stext = "ERROR, This Account: "+ sClass1 + sClass2 + sClass3 + sClass4 +" ALREADY EXISTS ";
			Response.Write("<script language=javascript>window.alert('"+ stext +"'); window.history.go(-1);</script");
			Response.Write(">");
			return false;
		}
		else if(sClass1 + sClass2 + sClass3 + sClass4 == "0000")
		{
			//Response.Write("<h3>ERROR, This Account <font color=red>");
			//Response.Write(sClass1 + sClass2 + sClass3 + sClass4 +"</font> IS NOT VALID!</h3>");
			string stext = "ERROR, This Account: "+ sClass1 + sClass2 + sClass3 + sClass4 +" IS NOT VALID! ";
			Response.Write("<script language=javascript>window.alert('"+ stext +"'); window.history.go(-1);</script");
			Response.Write(">");
			return false;
		}
	}
	
	if(bIsNew)
	{
		string sOpenBal = Request.Form["eOpenBal"];
		double dOpenBal = 0;

		try
		{
			dOpenBal = MyMoneyParse(sOpenBal);
		}
		catch(Exception e)
		{
			Response.Write("Error, incorrect currency format.");
			return false;
		}
		sc = "INSERT INTO account (class1, name1, class2, name2, class3, name3, class4, name4, opening_balance, active) ";
		sc += " VALUES(" + sClass1 + ",'" + sName1 + "'," + sClass2 + ",'" + sName2 + "'," + sClass3 + ",'" + sName3 + "'," + sClass4 + ",'";
		sc += sName4 + "', " + dOpenBal + ", " + iActivity +")";

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

	//update level names
	if(sName1 != sName1_old)
	{
		sc = " UPDATE account SET name1='" + EncodeQuote(sName1) + "' ";
		sc += "	WHERE class1=" + sClass1_old;
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
	}
	if(sName2 != sName2_old)
	{
		sc = " UPDATE account SET name2='" + EncodeQuote(sName2) + "' ";
		sc += "	WHERE class1=" + sClass1_old;
		sc += " AND class2=" + sClass2_old;
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
	}
	if(sName3 != sName3_old)
	{
		sc = " UPDATE account SET name3='" + EncodeQuote(sName3) + "' ";
		sc += "	WHERE class1=" + sClass1_old;
		sc += " AND class2=" + sClass2_old;
		sc += " AND class3=" + sClass3_old;
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
	}
	if(sName4 != sName4_old)
	{
		sc = " UPDATE account SET name4='" + EncodeQuote(sName4) + "' ";
		sc += "	WHERE class1=" + sClass1_old + " ";
		sc += " AND class2=" + sClass2_old;
		sc += " AND class3=" + sClass3_old;
		sc += " AND class4=" + sClass4_old;
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
	}

	//update opening balance
	if(Request.Form["eOpenBal"] != Request.Form["eOpenBal_old"])
	{
		string sOpenBal = Request.Form["eOpenBal"];
		double dOpenBal = MyMoneyParse(sOpenBal);

		sc = " UPDATE account SET opening_balance=" + dOpenBal;
		sc += "	WHERE class1=" + sClass1_old + " ";
		sc += " AND class2=" + sClass2_old;
		sc += " AND class3=" + sClass3_old;
		sc += " AND class4=" + sClass4_old;
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
	}

	//update level number
	if(bUpdateClassNumber)
	{
		sc = "UPDATE account SET ";
		sc += " class1='" + sClass1 +"'";
		sc += ", class2='" + sClass2 + "'";
		sc += ", class3='" + sClass3 +"'";
		sc += ", class4='" + sClass4 + "'";
		sc += "	WHERE class1=" + sClass1_old;
		sc += " AND class2=" + sClass2_old;
		sc += " AND class3=" + sClass3_old;
		sc += " AND class4=" + sClass4_old;
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
	}
	return true;
}

//----------------------------------------------------------------------------------------------------
bool ClassExists(string c1, string c2, string c3, string c4)
{
	if(dst.Tables["AllClass"] != null)
		dst.Tables["AllClass"].Clear();

	string sc = "SELECT class1, class2, class3, class4 FROM account";
	sc += " WHERE class1 =" + c1 + "AND class2 =" + c2 + "AND class3 =" + c3 + "AND class4 =" + c4;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "AllClass");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int iClass = dst.Tables["AllClass"].Rows.Count;
	if(iClass > 0)
		return true;
	else
		return false;
}

//----------------------------------------------------------------------------------------------------
bool DoDeleteAccount()
{
	string sc = " SELECT balance FROM account "; 
	sc += " WHERE class1 = " + m_sClass1 +" AND class2 = " + m_sClass2;
	sc += " AND class3 = " + m_sClass3 + " AND class4 = " + m_sClass4;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "checkbalance") <= 0)
			return true; //target not found, nothing to delete
		double dBalance = MyDoubleParse(dst.Tables["checkbalance"].Rows[0]["balance"].ToString());
		if(dBalance != 0)
		{
			Response.Write("<br><br><br><br><center><h4><font color=red>This account could not be deleted because current balance is not zero.</font></h4>");
			Response.Write("<h4>Current Balance : " + dBalance.ToString("c") + "</h4>");
			Response.Write("<input type=button value=Back onclick=history.go(-1) class=b>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " DELETE FROM account"; 
	sc += " WHERE class1 = " + m_sClass1 +" AND class2 = " + m_sClass2;
	sc += " AND class3 = " + m_sClass3 + " AND class4 = " + m_sClass4;
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

bool GetAllClasses()
{
	if(dst.Tables["class1"] != null)
		dst.Tables["class1"].Clear();
	if(dst.Tables["class2"] != null)
		dst.Tables["class2"].Clear();
	if(dst.Tables["class3"] != null)
		dst.Tables["class3"].Clear();
	if(dst.Tables["class4"] != null)
		dst.Tables["class4"].Clear();

	//class1
	string sc = " SELECT DISTINCT(class1), name1";
	sc += " FROM account ";
	sc += " ORDER BY class1, name1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "class1");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	// class2
	sc = " SELECT DISTINCT(class2), name2";
	sc += " FROM account ";
	sc += " WHERE class1 = " + m_sClass1New;
	sc += " ORDER BY class2, name2";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "class2");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	// class3
	// check if class2 changed, apply new class2 if true
	if(m_sClass1New != m_sClass1)
	{
		m_sClass2New = "0";
		if(dst.Tables["class2"].Rows.Count > 0)
			m_sClass2New = dst.Tables["class2"].Rows[0]["class2"].ToString(); //use the first one as new
	}
	sc = " SELECT DISTINCT(class3), name3";
	sc += " FROM account ";
	sc += " WHERE class1 = " + m_sClass1New + " AND class2 = " + m_sClass2New;
	sc += " ORDER BY class3, name3";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "class3");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	// class4
	sc = " SELECT DISTINCT(class4), name4";
	sc += " FROM account ";
	sc += " WHERE class1 = " + m_sClass1New + " AND class2 = " + m_sClass2New + " AND class3 = " + m_sClass3New;
	sc += " ORDER BY class4, name4";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "class4");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//get accoun name
	if(dst.Tables["editRecord"] != null)
		dst.Tables["editRecord"].Clear();
	sc = " SELECT * FROM account ";
	sc += " WHERE class1 = " + m_sClass1;
	sc += " AND class2 = " + m_sClass2;
	sc += " AND class3 = " + m_sClass3;
	sc += " AND class4 = " + m_sClass4;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "editRecord") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = dst.Tables["editRecord"].Rows[0];
	m_sName1 = dr["name1"].ToString();
	m_sName2 = dr["name2"].ToString();
	m_sName3 = dr["name3"].ToString();
	m_sName4 = dr["name4"].ToString();
	m_dOpenBal = MyDoubleParse(dr["opening_balance"].ToString());
	m_dBalance = MyDoubleParse(dr["balance"].ToString());
	m_bActive = MyBooleanParse(dr["active"].ToString());
	m_bShowBalance = MyBooleanParse(dr["show_balance"].ToString());
	return true;
}

bool DrawEditTable()
{
	Response.Write("<br><center><h3>Edit Account</h3>");
	if(!GetAllClasses())
		return false;

	Response.Write("<form name=frm action='account.aspx?t=e&edit=1");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("' method=post>");

	Response.Write("<br><table align=center cellspacing=1 cellpadding=5 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:hash;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th colspan=2>");
	Response.Write("<b>ACC# <font color=red>" + m_sClass1 + m_sClass2 + m_sClass3 + m_sClass4 + "</font>");
	Response.Write(" " + m_sName1 + " - " + m_sName2 +" - " + m_sName3 +" - " + m_sName4);
	Response.Write("<tr><td colspan=2><hr size=1 color=black></td></tr>");

	Response.Write("<tr><td colspan=2>");

	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:hash;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th width=50>Class</th><th width=150 nowrap>Current Name</th><th nowrap>New Name</th></tr>");
	Response.Write("<tr><td align=center>" + m_sClass1 + "</td><td>" + m_sName1 + "</td><td><input type=text name=sName1></td></tr>");
	Response.Write("<tr><td align=center>" + m_sClass2 + "</td><td>" + m_sName2 + "</td><td><input type=text name=sName2></td></tr>");
	Response.Write("<tr><td align=center>" + m_sClass3 + "</td><td>" + m_sName3 + "</td><td><input type=text name=sName3></td></tr>");
	Response.Write("<tr><td align=center>" + m_sClass4 + "</td><td>" + m_sName4 + "</td><td><input type=text name=sName4></td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2 align=center>");
	Response.Write("<table>");
	Response.Write("<tr><td>");
	Response.Write("<b>Opening Balance : </b><input type=text size=5 name=opening_balance ");
	Response.Write(" style=text-align:right value='" + m_dOpenBal + "'>");
	Response.Write("<input type=hidden name=opening_balance_old value='" + m_dOpenBal + "'>");
	Response.Write(" &nbsp;&nbsp;&nbsp; <b>Active : </b><input type=checkbox name=active ");

	if(m_bActive)
		Response.Write(" checked");
	Response.Write(">");
	//only admin to change the view balance setting..
	if(m_bCheckAccess)
	{
		Response.Write(" &nbsp;&nbsp;&nbsp; Show Balance : </b><input type=checkbox name=showbalance ");
		if(m_bShowBalance)
			Response.Write(" checked");
		Response.Write(">");
	}
	Response.Write("</td>");
	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<input type=submit name=cmd value=Update class=b>");
	Response.Write("<input type=button value=Finish class=b onclick=window.location=('account.aspx?c=" + m_sClass1 + "')>");
	Response.Write("</td><td align=right>");
	Response.Write("<input type=checkbox name=confirm_delete>Confirm delete ");
	Response.Write("<input type=submit name=cmd value=Delete class=b>");
	Response.Write("<input type=button value='Move Account' class=b onclick=window.location=('");
	Response.Write("account.aspx?t=m&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4 + "')>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool DrawMoveTable()
{
	Response.Write("<br><center><h3>Move Account</h3>");
	if(!GetAllClasses())
		return false;

	Response.Write("<form name=frm action='account.aspx?t=m");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("&new_c=" + Request.QueryString["new_c"]);
	Response.Write("&new_c2=" + Request.QueryString["new_c2"]);
	Response.Write("&new_c3=" + Request.QueryString["new_c3"]);
	Response.Write("' method=post>");

	Response.Write("<br><table align=center valign=center cellspacing=1 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:hash;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th align=left colspan=2>");
	Response.Write("<b>ACC# <font color=red>" + m_sClass1 + m_sClass2 + m_sClass3 + m_sClass4 + "</font>");
	Response.Write(" " + m_sName1 + " - " + m_sName2 +" - " + m_sName3 +" - " + m_sName4);
	Response.Write("<tr><td colspan=2><hr size=1 color=black></td></tr>");

	Response.Write("<tr><td colspan=2><b>Detination class to move to : </b></td></tr>");

	string sname = "";

	//class 1
	Response.Write("<tr>");
	Response.Write("<td align=center>");
	if(m_sClass1New == "9999" || dst.Tables["class1"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + m_sClass1New + "</b> ");
	Response.Write("</td><td>");
	Response.Write("<select name=class1 onchange=\"window.location=('account.aspx?t=m");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("&new_c2=" + Request.QueryString["new_c2"]);
	Response.Write("&new_c3=" + Request.QueryString["new_c3"]);
	Response.Write("&new_c='+this.options[this.selectedIndex].value)\" >\r\n");
	for(int i=0; i<dst.Tables["class1"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["class1"].Rows[i];
		sname = dr["name1"].ToString();
		if(sname == "")
			sname = "(no name)";
		string sel = m_sClass1;
		Response.Write("<option value=" + dr["class1"].ToString() + "");
		if(m_sClass1New == dr["class1"].ToString())
			Response.Write(" selected ");
		Response.Write(">" + sname + "</option>");
	}
	Response.Write("</select>");
	Response.Write("</tr>");

	//class 2
	string destClass2 = m_sClass2New;
	if(m_sClass1New != m_sClass1)
	{
		destClass2 = dst.Tables["class2"].Rows[0]["class2"].ToString();
	}
	Response.Write("<tr>");
	Response.Write("<td align=center>");
	if(m_sClass2New == "9999" || dst.Tables["class2"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + destClass2 + "</b> ");
	Response.Write("</td><td>");
	Response.Write("<select name=class2 onchange=\"window.location=('account.aspx?t=m");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("&new_c=" + Request.QueryString["new_c"]);
	Response.Write("&new_c3=" + Request.QueryString["new_c3"]);
	Response.Write("&new_c2='+this.options[this.selectedIndex].value)\" >\r\n");
	for(int i=0; i<dst.Tables["class2"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["class2"].Rows[i];
		sname = dr["name2"].ToString();
		Response.Write("<option value=" + dr["class2"].ToString() + "");
		if(m_sClass2New == dr["class2"].ToString())
			Response.Write(" selected ");
		Response.Write(">" + sname + "</option>");
	}
	Response.Write("</select>");
	Response.Write("</tr>");

	//class 3
	string destClass3 = m_sClass3New;
	if(m_sClass1New != m_sClass1 || m_sClass2New != m_sClass2)
	{
		if(dst.Tables["class3"].Rows.Count > 0)
			destClass3 = dst.Tables["class3"].Rows[0]["class3"].ToString();
	}
	Response.Write("<tr>");
	Response.Write("<td align=center>");
	if(m_sClass3New == "9999" || dst.Tables["class3"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + destClass3 + "</b> ");
	Response.Write("</td><td>");
	Response.Write("<select name=class3 onchange=\"window.location=('account.aspx?t=m");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("&new_c=" + Request.QueryString["new_c"]);
	Response.Write("&new_c2=" + Request.QueryString["new_c2"]);
	Response.Write("&new_c3='+this.options[this.selectedIndex].value)\" >\r\n");
	for(int i=0; i<dst.Tables["class3"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["class3"].Rows[i];
		sname = dr["name3"].ToString();
		if(sname == "")
			sname = "(no name)";
		Response.Write("<option value=" + dr["class3"].ToString() + "");
		if(m_sClass3New == dr["class3"].ToString())
			Response.Write(" selected ");
		Response.Write(">" + sname + "</option>");
	}
	Response.Write("</select>");
	Response.Write("</tr>");

	//class 4
	Response.Write("<tr>");
	Response.Write("<td align=center>");
	if(m_sClass4New == "9999" || dst.Tables["class4"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + m_sClass4New + "</b> ");
	Response.Write("</td><td>");
	Response.Write("<select name=class4 onchange=\"window.location=('account.aspx?t=m");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("&new_c2=" + Request.QueryString["new_c2"]);
	Response.Write("&new_c3=" + Request.QueryString["new_c3"]);
	Response.Write("&new_c=" + Request.QueryString["new_c"]);
	Response.Write("')\">\r\n");
	Response.Write("<option value=" + m_sName4 + ">" + m_sName4 + "</option>");
	Response.Write("</select>");
	Response.Write("</tr>");

	Response.Write("<tr><td>");
	Response.Write("<input type=button value='Edit Account' class=b onclick=window.location=('");
	Response.Write("account.aspx?t=e&edit=1&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4 + "')>");
	Response.Write("</td><td align=right>");
	Response.Write("<input type=submit name=cmd value=Move class=b>");
	Response.Write("<input type=button value=Finish class=b onclick=window.location=('account.aspx?c=" + m_sClass1 + "')>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool DrawAddNewTable()
{
	Response.Write("<br><center><h3>Add New Account</h3>");
	if(!GetAllClasses())
		return false;

	Response.Write("<form name=frm action='account.aspx?t=e' method=post>");

	Response.Write("<br><table width=55% align=center valign=center cellspacing=0 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:hash;border-collapse:collapse;fixed\">");

	Response.Write("<tr>");
	Response.Write("<td colspan=4>");
	Response.Write("<b>Select existsing class or enter new at text box</b> ");

	//class 1
	Response.Write("<tr>");
	Response.Write("<td colspan=4>");
	if(m_sClass1New == "9999" || dst.Tables["class1"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + m_sClass1New + "</b> ");
	Response.Write("<select name=class1 onchange=\"window.location=('account.aspx?t=e");
	Response.Write("&c='+this.options[this.selectedIndex].value)\" >\r\n");
	Response.Write(" <option value=9999>New(Enter class name at right)</option>");
	string sname = "";
	for(int i=0; i<dst.Tables["class1"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["class1"].Rows[i];
		string sel = m_sClass1;
		Response.Write("<option value=" + dr["class1"].ToString() + "");
		if(m_sClass1New == dr["class1"].ToString())
		{
			Response.Write(" selected ");
			sname = dr["name1"].ToString();
		}
		Response.Write(">" + dr["name1"].ToString() + "</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name=selected_name1 value='" + sname + "'>");
	Response.Write("<input type=text name=sName1 size=20>");
	Response.Write("</tr>");

	//class 2
	Response.Write("<tr>");
	Response.Write("<td colspan=4>");
	if(m_sClass2New == "9999" || dst.Tables["class2"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + m_sClass2New + "</b> ");
	Response.Write("<select name=class2 onchange=\"window.location=('account.aspx?t=e");
	Response.Write("&c=" + m_sClass1);
	Response.Write("&c2='+this.options[this.selectedIndex].value)\" >\r\n");
	Response.Write(" <option value=9999>New(Enter class name at right)</option>");
	for(int i=0; i<dst.Tables["class2"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["class2"].Rows[i];
		Response.Write("<option value=" + dr["class2"].ToString() + "");
		if(m_sClass2New == dr["class2"].ToString())
		{
			Response.Write(" selected ");
			sname = dr["name2"].ToString();
		}
		Response.Write(">" + dr["name2"].ToString() + "</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name=selected_name2 value='" + sname + "'>");
	Response.Write("<input type=text name=sName2 size=20>");
	Response.Write("</tr>");

	//class 3
	Response.Write("<tr>");
	Response.Write("<td colspan=4>");
	if(m_sClass3New == "9999" || dst.Tables["class3"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + m_sClass3New + "</b> ");
	Response.Write("<select name=class3 onchange=\"window.location=('account.aspx?t=e");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2);
	Response.Write("&c3='+this.options[this.selectedIndex].value)\" >\r\n");
	Response.Write(" <option value=9999>New(Enter class name at right)</option>");
	for(int i=0; i<dst.Tables["class3"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["class3"].Rows[i];
		Response.Write("<option value=" + dr["class3"].ToString() + "");
		if(m_sClass3New == dr["class3"].ToString())
		{
			Response.Write(" selected ");
			sname = dr["name3"].ToString();
		}
		Response.Write(">" + dr["name3"].ToString() + "</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name=selected_name3 value='" + sname + "'>");
	Response.Write("<input type=text name=sName3 size=20>");
	Response.Write("</tr>");

	//class 4
	Response.Write("<tr>");
	Response.Write("<td colspan=4>");
	if(m_sClass4New == "9999" || dst.Tables["class4"].Rows.Count <= 0)
		Response.Write("<b> &nbsp;&nbsp; </b> ");
	else
		Response.Write("<b>" + m_sClass4New + "</b> ");
	Response.Write("<select name=class4 onchange=\"window.location=('account.aspx?t=e");
	Response.Write("&c=" + m_sClass1 + "&c2=" + m_sClass2 + "&c3=" + m_sClass3 + "&c4=" + m_sClass4);
	Response.Write("&new_c2=" + Request.QueryString["new_c2"]);
	Response.Write("&new_c3=" + Request.QueryString["new_c3"]);
	Response.Write("&new_c=" + Request.QueryString["new_c"]);
	Response.Write("')\">\r\n");
	Response.Write(" <option value=9999>New(Enter class name at right)</option>");
	Response.Write("</select>");
	Response.Write("<input type=text name=sName4 size=20>");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=4>");
	Response.Write("<table>");
	Response.Write("<tr><td>");
	Response.Write("<b>Opening Balance : </b><input type=text size=5 name=opening_balance ");
	Response.Write(" style=text-align:right value=0>");
	Response.Write(" &nbsp;&nbsp;&nbsp; <b>Active : </b><input type=checkbox name=active checked>");
	Response.Write("</td>");
	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=4>");
	Response.Write("<input type=submit name=cmd value=Add class=b>");
	Response.Write("<input type=button value=Finish class=b onclick=window.location=('account.aspx?c=" + m_sClass1 + "')>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool DoChangeClassName(int nClass)
{
	string name = EncodeQuote(Request.Form["sName" + nClass.ToString()]);
	if(name == "")
	{
		Response.Write("<h3>Please enter new calss name</h3>");
		return false;
	}

	string sc = " UPDATE account SET name" + nClass.ToString() + " = '" + name + "' WHERE 1=1 ";
	switch(nClass)
	{
	case 1:
		sc += " AND class1 = " + m_sClass1;
		break;
	case 2:
		sc += " AND class1 = " + m_sClass1;
		sc += " AND class2 = " + m_sClass2;
		break;
	case 3:
		sc += " AND class1 = " + m_sClass1;
		sc += " AND class2 = " + m_sClass2;
		sc += " AND class3 = " + m_sClass3;
		break;
	case 4:
		sc += " AND class1 = " + m_sClass1;
		sc += " AND class2 = " + m_sClass2;
		sc += " AND class3 = " + m_sClass3;
		sc += " AND class4 = " + m_sClass4;
		break;
	default:
		break;
	}
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

//----------------------------------------------------------------------------------------------------
bool DrawAccountIndex()
{
	string sindex1 = "";
	string sindex2 = "";
	string sindex3 = "";
	string sindex4 = "";

	string sindex1_old = "";
	string sindex2_old = "";
	string sindex3_old = "";
	string sindex4_old = "";

	string s1 = "";
	string s2 = "";
	string s3 = "";
	string s4 = "";
	
	string select1 = "";
	string select2 = "";
	string select3 = "";
	string select4 = "";
	string bgCol = "";
	
	if(Request.QueryString["c"] != null && Request.QueryString["c"] != "" && Request.QueryString["c"] != "all")
		select1 = Request.QueryString["c"];
	if(Request.QueryString["c2"] != null && Request.QueryString["c2"] != "" && Request.QueryString["c2"] != "all")
		select2 = Request.QueryString["c2"];
	if(Request.QueryString["c3"] != null && Request.QueryString["c3"] != "" && Request.QueryString["c3"] != "all")
		select3 = Request.QueryString["c3"];
	if(Request.QueryString["c4"] != null && Request.QueryString["c4"] != "" && Request.QueryString["c4"] != "all")
		select4 = Request.QueryString["c4"];
	string ss1 = "";
	string ss2 = "";
	string ss3 = "";
	string ss4 = "";

	int irows = dst.Tables["allaccount"].Rows.Count;
	if(irows == 0)
		return false;
	bool bAlter = false;
	for(int i=0; i<irows; i++)
	{
		DataRow dr = dst.Tables["allaccount"].Rows[i];
		sindex1 = dr["class1"].ToString() + " - " + dr["name1"].ToString();
		sindex2 = dr["class2"].ToString() + " - " + dr["name2"].ToString();
		sindex3 = dr["class3"].ToString() + " - " + dr["name3"].ToString();
		sindex4 = dr["class4"].ToString() + " - " + dr["name4"].ToString();
					
		if(sindex1 != sindex1_old)
		{
			sindex1_old = sindex1;
			s1 = sindex1;
			ss1 = dr["class1"].ToString();
		}
		else
			s1 = "";

		if(sindex2 != sindex2_old)
		{
			sindex2_old = sindex2;
			s2 = sindex2;
			ss2 = dr["class2"].ToString();

		}
		else
			s2 = "";

		if(sindex3 != sindex3_old)
		{
			sindex3_old = sindex3;
			s3 = sindex3;
			ss3 = dr["class3"].ToString();
		}
		else
			s3 = "";
		if(sindex4 != sindex4_old)
		{
			sindex4_old = sindex4;
			s4 = sindex4;
			ss4 = dr["class4"].ToString();
		}
		else
			s4 = "";
		
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		Response.Write("><td ");
		if(s1 != "" && ss1 == select1)
			Response.Write(" bgcolor= "+ bgCol +" ");
		Response.Write("><b>" + s1 + "</b></td><td ");
		if(s2 != "" && ss2 == select2)
			Response.Write(" bgcolor= "+ bgCol +" ");
		Response.Write("><b>" + s2 + "</b></td>");
		Response.Write("<td ");
		if(s3 != "" && ss3 == select3)
			Response.Write(" bgcolor= "+ bgCol +" ");
		Response.Write(" ><b>" + s3 + "</b></td><td ");
		if(s4 != "" && ss4 == select4)
			Response.Write(" bgcolor= "+ bgCol +" ");
		Response.Write("><b>" + sindex4 + "</b></td></tr>");
	}
	Response.Write("</table></td></tr><br>");
	return true;
}
//----------------------------------------------------------------------------------------------------
bool GetCurAccBalance(string accNum, ref string sCurrBal)
{
	DataRow dr = null;

	if(accNum == null || accNum == "")
		return false;
	else 
	{
		if(GetAccLastTran(accNum, ref dr))
		{
			if(accNum == dr["source"].ToString())
			{
				sCurrBal = double.Parse(dr["source_balance"].ToString()).ToString("c");
			}
			else if(accNum == dr["dest"].ToString())
				{
//DEBUG("dest_bal = ", dr["dest"].ToString());
//DEBUG("dest_bal = ", dr["dest_balance"].ToString());
					sCurrBal = double.Parse(dr["dest_balance"].ToString()).ToString("c");
				}
			return true;
		}
		else 
		{
			string c1 = accNum[0].ToString();
			string c2 = accNum[1].ToString();
			string c3 = accNum[2].ToString();
			string c4 = accNum[3].ToString();
//DEBUG("accNum = ", accNum);
			string sc = "SELECT opening_balance FROM account ";
			sc += "WHERE class1 =" + c1 + " AND class2 =" + c2 + " AND class3 =" + c3 + " AND class4 =" + c4;
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				int rows = myAdapter.Fill(dst, "single_balance_record");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
			
			DataRow ds = dst.Tables["single_balance_record"].Rows[0];
			sCurrBal = double.Parse(ds["opening_balance"].ToString()).ToString("c");
			dst.Tables["single_balance_record"].Clear();
			return true;
		}
	}
	return true;
}
//----------------------------------------------------------------------------------------------------
bool GetAccLastTran(string accNum, ref DataRow dr)
{
	string sc = "SELECT TOP 1 t.trans_date, t.source, t.dest, d.source_balance, d.dest_balance ";
	sc += "FROM trans t JOIN tran_detail d ON t.id = d.id ";
	sc += "WHERE t.source =" + accNum + " OR t.dest =" + accNum;
	sc += " ORDER BY t.trans_date, t.source, t.dest, d.source_balance, d.dest_balance DESC";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "trans_rec");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int irows = dst.Tables["trans_rec"].Rows.Count;
	if(irows > 0)
	{
		dr = dst.Tables["trans_rec"].Rows[0];
		return true;
	}
	else
		return false;
}


bool bCheckAccessLevel(bool bAllow)
{
	string access_level = "1";
	string sc = " SELECT access_level FROM card ";
	sc += " WHERE id = "+ Session["card_id"];
//DEBUG("sc = ", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "access");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1)
		access_level = dst.Tables["access"].Rows[0]["access_level"].ToString();

	if(int.Parse(access_level) > 9)
		bAllow = true;
	return bAllow;
}
</script>

<asp:Label id=LFooter runat=server/>

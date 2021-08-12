<script runat=server>

string m_sInvNo = "";
string m_actionmsg = " ";
string m_customerName = "";

bool m_bSingleE = true;
bool m_bCredit = false;

string m_nqty = "";
DataSet dst = new DataSet();
bool m_bIgnor = false;
bool m_bIgnor_qty = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
/*
if(validateSN("1dsadsf123", "1000", "1"))
	Response.Write("ok!");
else
	Response.Write("oh no!");		*/

	m_sInvNo = Request.QueryString["inv"];

	if(m_sInvNo == null || m_sInvNo == "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error no invoice number");
		return;
	}

	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		string s_id = Request.QueryString["del"];
		
		string s_rid = "";
		if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
			s_rid = "&rid=" + Request.QueryString["rid"];
		if(!DeleteSN(s_id))
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=inputsn.aspx?inv=" + m_sInvNo + s_rid + "\">");
	}

	if(Request.QueryString["inv"] != null && Request.QueryString["inv"] !="")
	{
		if(IsInteger(m_sInvNo))
		{
			if(!GetItems())		//Get details of products;
				return;
		}
	}


	if(Request.Form["cmd"] == "Input" || (Request.Form["sn"] != "" && Request.Form["sn"] != null))
	{
		string s_pID = Request.Form["pcode"];
		string s_sSN = Request.Form["sn"];
		
		string s_SNnum = "";
		string s_SNchar = "";
		m_nqty = Request.Form[""+ s_pID +""];

		if(Request.Form["ignor"] == "1")
			m_bIgnor = true;
		if(Request.Form["ignor_qty"] == "1")
			m_bIgnor_qty = true;
			//DEBUG("ignor =", Session["ignor"].ToString());
		if(Request.QueryString["ft"] =="m")
		{
			if(Request.Form["sn_start"] != "")
			{
				if(validateSN(Request.Form["sn_start"], Request.Form["sn_qty"], Request.Form["inc_ratio"], ref s_SNnum, ref s_SNchar))
				{
					if(!DoMultiRecord(s_pID, s_SNnum, s_SNchar))
						return;
				}
			}
		}
		else
		{
			if(s_sSN != "")
			{
				if(!RecordSN(s_pID, s_sSN))
					return;
			}
		}
		m_bIgnor = false;
	}
	else if(Request.Form["cmd"] == "Del")
	{
		string s_SNid = Request.Form["sn_id"];

		if(!DeleteSN(s_SNid))
			return;
	}
	PrintAdminHeader();
	PrintAdminMenu();

	ListProducts();
	DrawInputTbl();
	
}

bool chkQTY(string s_inv, string s_code)
{
	bool bOverLimit = false;
	
	string sc = " SELECT invoice_number FROM sales_serial ";
	sc += " WHERE invoice_number = "+ s_inv +" ";
	sc += " AND code = "+ s_code +"";
	int rows = 0;

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "limit");
			
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	//DEBUG(" row s= ", rows);
	
	if(m_bCredit)
		m_nqty = m_nqty.Replace("-", "0");

	if(rows >= int.Parse(m_nqty))
		bOverLimit = true;

	/*if(bOverLimit)
	{
		Response.Write("<script language=javascript>window.alert('All SN# are already INPUT!!')</script");
		Response.Write(">");
	}*/
	return bOverLimit;
}

bool chkSameProduct(string s_sn, string s_code)
{
	bool bFound = false;
	string sc = " SELECT product_code FROM stock ";
	sc += " WHERE sn = '"+ s_sn +"'";
	//sc += " AND product_code = "+ s_code +"";
	string s_fd_code = "";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "sm_code") == 1)
			s_fd_code = dst.Tables["sm_code"].Rows[0]["product_code"].ToString();
			
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(s_code == s_fd_code)
		bFound = true;
	
	/*else
	{
		Response.Write("<script language=javascript>window.alert('Item is Different from the Sales Invoice')</script");
		Response.Write(">");
	}*/
	
	return bFound;
}


bool chkDuplicateSN(string s_sn, string s_inv, string s_code)
{
	bool bFound = false;
	string sc = " SELECT sn FROM sales_serial ";
	sc += " WHERE sn = '"+ s_sn +"'";
	sc += " AND invoice_number = "+ s_inv +" ";
	sc += " AND code = "+ s_code +"";
	//string sf_sn = "";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "duplicate") == 1)
			bFound = true;
			//sf_sn = dst.Tables["duplicate"].Rows[0]["sn"].ToString();
			
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	/*if(bFound)
	{
		Response.Write("<script language=javascript>window.alert('Duplicate SN# Found!!')</script");
		Response.Write(">");
	}*/
	
	return bFound;
}

bool DoMultiRecord(string pid, string snNum, string snChar)
{
	int i_qty = int.Parse(Request.Form["sn_qty"]);
	int incby = int.Parse(Request.Form["inc_ratio"]);

	string inputSN = "";
	
	
	for(int i=0; i<i_qty; i++)
	{
		inputSN = (int.Parse(snNum) + incby*i).ToString();
		while(snNum.Length > inputSN.Length)
		{
			inputSN = "0" + inputSN;
		}
		inputSN = snChar + inputSN;
//DEBUG("pid = ", pid);	
		RecordSN(pid, inputSN);
	}
	
	return true;
}

bool RecordSN(string id, string sn)
{
/*	int rows = 0;
	string scc = "SELECT sn FROM sales_serial WHERE sn='" + sn + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(scc, myConnection);
		rows = myCommand.Fill(dst, "existing_sn");
		if(rows > 0 )
		{
			m_actionmsg = "*Error! <br>&nbsp;Reason: The input SN: " + sn + " exists in Records!";
		}
	}
	catch(Exception e) 
	{
		ShowExp(scc, e);
		return false;
	}

	if(rows ==0)
	{
		string sc = "INSERT INTO sales_serial (invoice_number, sn, code) VALUES (";
			sc += m_sInvNo + ", '" + sn + "', " + id + ")"; 
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
	return true;
*/
	string sc = "";
	string stock_status = m_bCredit ? "2" : "1"; //1.sold 2.instock
	sc = " UPDATE stock SET status = " + stock_status + " WHERE sn = '" + sn + "' ";
	bool bDuplicate = chkDuplicateSN(sn, m_sInvNo, id);  // check duplication on sn
	bool bchkQty = chkQTY(m_sInvNo, id);              // check qty
//DEBUG("bchkqty = ", bchkQty.ToString());	
	bool bSameCode = false;
	if(m_bIgnor)
		bSameCode = true;
	else
		bSameCode = chkSameProduct(sn, id);		// check sn in the same product type
//DEBUG(" mbig =", m_bIgnor_qty.ToString());
	if(m_bIgnor_qty)
		bchkQty = false;

	if(!bDuplicate && !bchkQty && bSameCode )
	{
		sc += " INSERT INTO sales_serial (invoice_number, sn, code) VALUES (";
		sc += m_sInvNo + ", '" + sn + "', " + id + ") "; 
		if(stock_status == "1")
			sc += AddSerialLogString(sn, "Sold to " + m_customerName, "", m_sInvNo, "", "");
	}
	else
	{
		//string sjava = "<script language=javascript>window.alert('All SN# are already INPUT!!')</script";
		//sjava += " >";
		//if(bchkQty)
		if(bchkQty || bDuplicate || !bSameCode)
		{
			Response.Write("<script language=javascript>window.alert('Please Check the following causes:\\r\\rAll SN# already INPUT\\rOR Duplicate SN# Found\\rOR Item is Different from the INVOICE')</script");
			Response.Write(">");
		}
		/*if(bDuplicate)
		{
			Response.Write("<script language=javascript>window.alert('Duplicate SN# Found!!')</script");
			Response.Write(">");
		}
		if(!bSameCode)
		{
			Response.Write("<script language=javascript>window.alert('Item is Different from the Sales Invoice')</script");
			Response.Write(">");
		}*/
	}
	if(m_bCredit && stock_status == "2")
		sc += AddSerialLogString(sn, "Credit for " + m_customerName, "", m_sInvNo, "", "");	
	
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

bool DeleteSN(string s_id)
{
	string sn = GetSNFromStock(s_id);
	string sc = "DELETE FROM sales_serial WHERE id = " + s_id;
	if(sn != "")
		sc += AddSerialLogString(sn, "ReStocked while packing", "", m_sInvNo, "", "");
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

string GetSNFromStock(string id)
{
	if(dst.Tables["getsn"] != null)
		dst.Tables["getsn"].Clear();

	string sc = " SELECT sn FROM sales_serial WHERE id = " + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "getsn") == 1)
			return dst.Tables["getsn"].Rows[0]["sn"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return "";
}

void ListProducts()
{
	Response.Write("<br><br><center><h3><b>Serial Enter</b></h3></center><br><br>");
	Response.Write("<b>&nbsp;&nbsp;&nbsp;<font color=blue>Details: </font> Invoice No. " + m_sInvNo + "</b><br><br>");
	Response.Write("<table width=90% align=center border=1 cellspacing=3 cellpadding=2>\r\n");
	Response.Write("<tr bgcolor=E3E3E3 height=30><td width=13% align=center><b>");
	Response.Write("Product#</b></td>");
	Response.Write("<td align=center><b>Supp_code#</td>");
	Response.Write("<td align=center><b>Description</b></td>");
	Response.Write("<td align=center><b>Qty Invoiced</b></td></tr>\r\n");

	for(int i=0; i<dst.Tables["products"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["products"].Rows[i];			
		Response.Write("<tr><td align=center>" + dr["code"].ToString() + "</td>");
		Response.Write("<td align=center>" + dr["supplier_code"].ToString() + "</td>");
		Response.Write("<td><a href=inputsn.aspx?inv=" + m_sInvNo + "&rid=" + i.ToString() + " class=o>");
		Response.Write(dr["name"].ToString() + "</a></td>");
		Response.Write("<td align=center>" + dr["quantity"].ToString() + "</td></tr>\r\n");	
		
	}

	Response.Write("</table><br>\r\n");
}

void DrawInputTbl()
{
	string s_rid = "0";
	if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
		s_rid = Request.QueryString["rid"];
	string s_code = dst.Tables["products"].Rows[int.Parse(s_rid)]["code"].ToString();
	string s_desc = dst.Tables["products"].Rows[int.Parse(s_rid)]["name"].ToString();
	m_nqty = dst.Tables["products"].Rows[int.Parse(s_rid)]["quantity"].ToString();
	Response.Write("<b>&nbsp;&nbsp;&nbsp;<font color=blue>Processing: </font>");
	Response.Write("<font color=red>#" + s_code + "</font> - " + s_desc + "</b><br><br>");
	
	int icurrent = 0;
	if(!GetItemSNdetails(s_code, ref icurrent))
		return;

	string url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	Response.Write("<form name=frmInput action=" + url);
	Response.Write(" method=post>");
	
	Response.Write("<input type=hidden name='"+ s_code +"' value='"+ m_nqty +"'>");
	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=0>");
	//left table --- input serials
	Response.Write("<tr><td width=50% valign=top align=left>");
	Response.Write("<table width=60% align=left border=1 cellspacing=3 cellpadding=2>\r\n");
	Response.Write("<tr bgcolor=E3E3E3 height=30><td width=13% align=center><b>");
	Response.Write("No.</b></td>");
	Response.Write("<td colspan=2 align=center><b>Serial No.</b></td></tr>\r\n");

	for(int i=0; i<dst.Tables["serials"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["serials"].Rows[i];
		Response.Write("<tr><td align=center><b>" + (i + 1).ToString() + "</b></td>");
		Response.Write("<td>" + dr["sn"].ToString() + "</td>");
		Response.Write("<td align=center>");
		Response.Write("<a href='inputsn.aspx?inv=" + m_sInvNo + "&rid=" + Request.QueryString["rid"] + "&del=" + dr["id"].ToString() + "'>Del</a>");
		Response.Write("</td></tr>\r\n");
	}

	Response.Write("</table>\r\n");

	Response.Write("</td><td valign=top align=left width=50%>");
	
	string s_checksingle = "";
	string s_checkmulti = " checked";

	if(Request.QueryString["ft"] != null && Request.QueryString["ft"] =="m")
		m_bSingleE = false;

	if(m_bSingleE)
	{
		s_checksingle = " checked";
		s_checkmulti = "";
	}
	
	//right table --- display serials
//	string s_rid = "";
//	if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
//	{
//		s_rid = Request.QueryString["rid"];
//	}
	Response.Write("<table width=100% border=1 cellspacing=3 cellpadding=2>\r\n");
	Response.Write("<tr bgcolor=E3E3E3><td colspan=3 align=center><b>Input SN:</b></td></tr>");
	Response.Write("<tr><td colspan=3 align=left><input type=checkbox name=ignor value=1><b>Ignor Input SN# in the Same Product Type</td></tr>");
	Response.Write("<tr><td colspan=3 align=left><input type=checkbox name=ignor_qty value=1><b>Ignor Input SN# in Quantity Check</td></tr>");
	Response.Write("<tr><td bgcolor=E3E3E3 valign=top><br><input type=radio name=sglentry");
	Response.Write(s_checksingle + " onclick=\"window.location=('" + Request.ServerVariables["URL"]);
	if(s_rid == "")
		Response.Write("?" + "inv=" + m_sInvNo + "&r=" + DateTime.Now.ToOADate() + "')\""); 
	else
		Response.Write("?" + "inv=" + m_sInvNo +"&rid=" + s_rid + "&r=" + DateTime.Now.ToOADate() + "')\""); 
	Response.Write(">Single Entry :<br>");
	if(m_bCredit)
		m_nqty = m_nqty.Replace("-", "0");
	if(int.Parse(m_nqty) > 1)
	{
		Response.Write("<input type=radio name=mulentry" + s_checkmulti);
		Response.Write(" onclick=\"window.location=('" + Request.ServerVariables["URL"]);
		
		if(s_rid == "")
			Response.Write("?" + "ft=m&inv=" + m_sInvNo + "&r=" + DateTime.Now.ToOADate() + "')\"");
		else
			Response.Write("?" + "ft=m&inv=" + m_sInvNo + "&rid=" + s_rid + "&r=" + DateTime.Now.ToOADate() + "')\"");
		
		Response.Write(">Multi - Entries :</td>");
	}
	if(m_bSingleE)
	{
		Response.Write("<td align=center><input type=editbox size=35 name=sn value=''>");
		Response.Write("<script");
		Response.Write(">\r\ndocument.frmInput.sn.focus();\r\n</script");
		Response.Write(">\r\n");
		Response.Write("<input type=hidden name=pcode value='" + s_code + "'></td>");
	}
	else
	{
		
		Response.Write("<td align=right><br>Start:&nbsp;<input type=editbox size=30 name=sn_start value=''><br>");
		Response.Write("<br><b>Quantity :</b>&nbsp;<input type=editbox size=10 name=sn_qty value='1'><br><br>");
		Response.Write("<input type=hidden name=pcode value='" + s_code + "'>");
		Response.Write("<b>increase by : </b><select name=inc_ratio><option value=1 selected>1<option value=2>2");
		Response.Write("</select><br>&nbsp;</td>");
		
	}
	
	Response.Write("<td align=center><input type=submit name=cmd value='Input' "+ Session["button_style"] +"></td></tr>");
	Response.Write("<tr><td colspan=3><font color=red>" + m_actionmsg + "</font></td></tr>");
	Response.Write("<tr><td colspan=3 align=right><br><br><input type=button value='Back' "+ Session["button_style"] +" onclick=window.location=('esales.aspx?i=");
	Response.Write(m_sInvNo + "&r=" + DateTime.Now.ToOADate() + "') ><br><br></td></tr>\r\n");
	Response.Write("</table></td></tr>\r\n");

	//DW 24.12.2002
	Response.Write("<tr><td colspan=7 align=center><br>");		
	if(MyBooleanParse(GetSiteSettings("QPOS_ENABLE_SN_PRINTOUT", "0")))
	{
		Response.Write("<input type=button  value=' Print POS Receipt ' "+ Session["button_style"] +" ");
		Response.Write(" onclick=window.open('qpos.aspx?t=pr&i=" + m_sInvNo + "')  "+ Session["button_style"] +">");	
	}
	Response.Write("<input type=button  value=' Continue Process ' "+ Session["button_style"] +" ");
	Response.Write(" onclick=window.location=('esales.aspx?i=" + m_sInvNo + "&r=" + DateTime.Now.ToOADate() + "')>");
	Response.Write("<input type=button  value=' Close Window ' "+ Session["button_style"] +" ");
	Response.Write(" onclick='window.close();'  "+ Session["button_style"] +">");	
	Response.Write("<input type=button  value=' Back to Order List ' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\"window.location=('olist.aspx');\"  "+ Session["button_style"] +">");	
	Response.Write("</td></tr>");

	Response.Write("</table></form>\r\n");
}

bool GetItemSNdetails(string s_code, ref int i_current)
{
	int rows = 0;
	string sc = "SELECT	id, sn FROM sales_serial WHERE invoice_number=" + m_sInvNo;
		sc += " AND code=" + s_code;
		sc += " ORDER BY id DESC";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "serials");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return true;
}

bool GetItems()
{
	string sc = "";
	int rows = 0;

	sc = " SELECT s.code, s.name, s.quantity, s.supplier_price, s.supplier, s.supplier_code ";
	sc += ", i.type, c.trading_name, c.trading_name as cname ";
	sc += " FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id = i.card_id ";
	sc += " WHERE s.invoice_number=" + m_sInvNo;

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "products");
		if(rows <= 0)
		{
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}

	if(dst.Tables["products"].Rows[0]["type"].ToString() == "6")
		m_bCredit = true;
	m_customerName = dst.Tables["products"].Rows[0]["cname"].ToString();
	return true;
}

bool validateSN(string sn_start, string sn_qty, string incby, ref string num, ref string s_psn)
{
	int ilength = sn_start.Length;
	int iMaxLength = (Request.Form["sn_qty"].ToString()).Length + 1; 

//DEBUG("digi = ", sn_start[ilength-1].ToString());
	bool bOK = IsInteger(sn_start[ilength-1].ToString());

	if(!bOK)
	{
		m_actionmsg = "* Error!<br>&nbsp;&nbsp;Initial SN is not adequate for multiple serial entry!";
		return false;	
	}

	if(!IsInteger(sn_qty))
	{
		m_actionmsg = "* Error!<br>&nbsp;&nbsp;invalid quantity!";
		return false;
	}

	int inc_by = int.Parse(incby);

	//string num = "";
	while (bOK)
	{
		ilength = ilength - 1;
		if(IsInteger(sn_start[ilength].ToString()) && iMaxLength >= num.Length)
		{
			num = sn_start[ilength] + num;
		}
		else
		{
			while (ilength >= 0)
			{
				s_psn = sn_start[ilength] + s_psn;
				ilength = ilength - 1;
			}
//DEBUG("num = ", num);
//DEBUG("s_psn = ", s_psn);
			bOK = false;
		}
		if(ilength == 0)
			break;
	}

	string e_num = (int.Parse(num) + inc_by * int.Parse(sn_qty)).ToString();

	if(num.Length < e_num.Length)
	{
		m_actionmsg = "* Error!<br>&nbsp;&nbsp;Initial SN is not adequate for multiple serial entry!";
		return false;
	}
	return true;
}
</script>
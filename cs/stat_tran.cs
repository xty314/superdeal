<!-- #include file="page_index.cs" -->

<script runat=server>
DataSet dst = new DataSet();
string m_scredit_terms_id = "6";
string m_scredit_terms = "";
string m_timeOpt = "0";
string m_custID = "";
string m_periodindex = "1";
string m_Tbalance = "0";
string m_type = "";
string m_datePeriod = "";
string m_dateSql = "";
string m_dateSqlc = "";
string m_code = "";
DataRow[] m_dra = null;
string[] m_EachMonth = new string[16];
string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
string fromdate ="";
string todate = "";
string rangdate ="";
string emailTem ="";
int m_nPeriod = 0;
bool m_bPickTime = false;
string m_branchID = "1";
double invTotal =0;
double totalPaid =0;
double totalUnpaid =0;
double totalbal =0;
double creditTotalBalance=0;
double dNOTotalUpaid = 0;
double dNZTotalUpaid = 0;
double dNOTotalPaid = 0;
double dNZTotalPaid = 0;
//double m_dCardBalance = 0;
double m_dUnPaidTotal = 0;
string m_directory = "0";
string tableWidth = "100%";
string sysEmail = "";

void SPage_Load()
{
	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
		m_custID = Request.QueryString["ci"];
	
	if(Session["email"] != "" && Session["email"] !=null)
		sysEmail = Session["email"].ToString();
	else
		sysEmail = GetSiteSettings("System_Email", "sysmail@gpos.com", false);

	if(m_sSite != "admin")
		m_custID = Session["card_id"].ToString(); //customer can only view his own statment, of course
		
	 m_EachMonth[0] = "JAN";
	m_EachMonth[1] = "FEB";
	m_EachMonth[2] = "MAR";
	m_EachMonth[3] = "APR";
	m_EachMonth[4] = "MAY";
	m_EachMonth[5] = "JUN";
	m_EachMonth[6] = "JUL";
	m_EachMonth[7] = "AUG";
	m_EachMonth[8] = "SEP";
	m_EachMonth[9] = "OCT";
	m_EachMonth[10] = "NOV";
	m_EachMonth[11] = "DEC";
	//----

	int i = 0;
	int day = 1;
	int month = DateTime.Now.Month;
	int year = DateTime.Now.Year;
    m_type = Request.QueryString["t"];
	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.QueryString["period"] != null)
	    m_nPeriod = MyIntParse(Request.QueryString["period"]); 
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
	if(Request.Form["t"] != null)
		m_type = Request.Form["t"];

	if(Request.Form["Datepicker1_day"] != null)
	{
		m_sdFrom = Request.Form["Datepicker1_day"] + "-" + Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		m_sdTo = Request.Form["Datepicker2_day"] + "-" + Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];

		Session["report_date_from"] = m_sdFrom;
		Session["report_date_to"] = m_sdTo;
	}
	if(Request.Form["pick_month1"] != null)
	{
		m_smFrom = Request.Form["pick_month1"];
		m_smTo = Request.Form["pick_month2"];
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];
	
	}
	if(Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"].ToString();
		m_sdTo = Request.QueryString["to"].ToString();
	}
	if(Request.QueryString["pr"] != null)
		m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());
	Session["report_period"] = m_nPeriod;
	
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yestoday";
		break;
	case 6:
		m_datePeriod = "The Day Before Yesterday";
		break;
		
	case 2:
		m_datePeriod = "This Week";
		break;
	case 3:
		m_datePeriod = "This Month";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	case 5:
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_smFrom)-1] + "-"+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_smTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}
  string m_ShowFullReport = Request.Form["period"];

	if(m_timeOpt != Request.QueryString["p"])
		m_timeOpt = Request.QueryString["p"];
	 
	
	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
	{
		m_timeOpt = "4";
		if(!GetInvRecords(m_custID))
			return;
		Response.Write(PrintStatmentDetails());
		 if(Request.QueryString["a"] != "" && Request.QueryString["a"] != null)
		{
			if(Request.QueryString["a"].ToString() == "e")
			{   
				if(sendMail(emailTem, m_custID))
					return;
			}
		}		
		return;
	}

     
	 
	
	        if(Request.QueryString["d"] == null || Request.QueryString["d"] =="" )
		      PrintMainPage();
			else
				PrintStatmentDetails();
				

}

bool bCardType(ref string cardID)
{
	string stype = "1";
	bool bIsSupplier = false;
	
	int rows = 0;
	string sc= " SELECT * FROM card WHERE id = " + cardID;
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "ctype");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows == 1)
	{
		stype = dst.Tables["ctype"].Rows[0]["type"].ToString();
		if(GetEnumID("card_type", "supplier") == stype)
			bIsSupplier = true;
	}

	return bIsSupplier;
}

bool GetInvRecords(string cardID)
{

	if(cardID == null || cardID == "")
   {
	    PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<p style=\"font:bold 14px arial; color:red; text-align=center\">Sorry, No Customer has been selected!!</p><p align=center><input type=button onclick=\"window.location=('statement.aspx')\" value='Back to Statment List'>");
		PrintAdminFooter();
	    return false;
	}
		
	string sc = "";
   
    switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";
		break;
	case 6:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 2 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(week, i.commit_date, GETDATE()) = 0 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 0, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}
	
	 switch(m_nPeriod)
	{
	case 0:
		m_dateSqlc = " AND DATEDIFF(day, td.trans_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSqlc = " AND DATEDIFF(day, td.trans_date, GETDATE()) = 1 ";
		break;
	case 6:
		m_dateSqlc = " AND DATEDIFF(day, td.trans_date, GETDATE()) = 2 ";
		break;
	case 2:
		m_dateSqlc = " AND DATEDIFF(week, td.trans_date, GETDATE()) = 0 ";
		break;
	case 3:
		m_dateSqlc = " AND DATEDIFF(month, td.trans_date, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSqlc = " AND td.trans_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 0, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}
	
	
	string pickMonth = "";
	string pickYear = "";
	if(Request.Form["pickMonth"] != null && Request.Form["pickMonth"] != "")
		pickMonth = Request.Form["pickMonth"].ToString();

	
	int nTotalMonth = 0;
	string currentYear = DateTime.Now.ToString("yyyy");

	if(pickMonth != "all" && pickMonth != null && pickMonth != "")
	{		
		m_timeOpt = pickMonth;		
	}

	if(!bCardType(ref cardID))
	{
		sc = "SET dateformat dmy SELECT '0' AS pid, CONVERT(VARCHAR(50),(i.invoice_number)) AS invoice_number, i.commit_date, i.freight, i.cust_ponumber, i.total, Isnull(i.amount_paid,0) AS amount_paid, i.customer_gst ";
		sc += ", (i.total - i.amount_paid) AS cur_bal";
		sc += ",0 as creditapplied";
		sc += " FROM invoice i ";
		sc += " WHERE 1=1 ";
		sc += " AND i.card_id = " + cardID;
	    sc += m_dateSql ;

		
		//////get all the credit records as well.....////////
		sc += " UNION ";
		sc += " SELECT '0' AS pid, td.invoice_number AS invoice_number, ISNULL(td.trans_date, GETDATE()) AS commit_date, 0 AS freight, 'Credit' AS cust_ponumber,  0 AS total, c.amount AS amount_paid, '0' as customer_gst";
		sc += ", 0 - (c.amount - c.amount_applied) AS cur_bal ";
		sc += ", c.amount_applied as creditapplied";
		sc += " FROM credit c  ";
		sc += " JOIN trans t ON t.id = c.tran_id JOIN tran_detail td ON td.credit_id = c.tran_id ";		
		sc += " WHERE c.card_id = " + cardID;
		sc += m_dateSqlc ;
		///////end here //////
        //////get all the credit records as well.....////////
		sc += " UNION "; 
		sc += " SELECT '0' AS pid, '0' AS invoice_number, ISNULL(td.trans_date, GETDATE()) AS commit_date, 0 AS freight, 'Credit' AS cust_ponumber,  0 AS total, c.amount AS amount_paid, '0' as customer_gst ";
		sc += ", 0 - (c.amount - c.amount_applied) AS cur_bal ";
		sc += ", c.amount_applied as creditapplied";
		sc += " FROM credit c  ";
		sc += " JOIN trans t ON t.id = c.tran_id JOIN tran_detail td ON td.id = t.id AND td.id = c.tran_id ";			
		sc += " WHERE c.card_id = " + cardID;
		sc += " AND c.amount_applied = '0' ";
		sc += m_dateSqlc ;
		///////end here //////
		
		sc += " ORDER BY i.commit_date DESC";
	}
	
//DEBUG("SC " , sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "invoice_rec");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool sendMail(string body, string cid)
{
	
	MailMessage msgMail = new MailMessage();
	msgMail.To = GetCustName("email", cid);
	msgMail.From = sysEmail;
	msgMail.Subject = "Account Information";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = body;
	SmtpMail.Send(msgMail);
//	PrintHeaderAndMenu();
//	Response.Write("<br><br><br><br><br><br><br><br><br><br><center><b>Your password has been sent to " + m_userEmail + ". Check your email after a few minutes.<br><br><br><br><br><br><br><br><br><br>");
//	PrintFooter();
	return true;
}

string sStateRowsLeft()
{
	StringBuilder sb = new StringBuilder();
	bool bAlterColor = false;
	string s_empPOno = "";
	for(int i = 0; i < dst.Tables["invoice_rec"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["invoice_rec"].Rows[i];
		sb.Append("<tr ");
         
		 if(bAlterColor)
			sb.Append(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
				
		sb.Append("><td align=center>" + DateTime.Parse(dr["commit_date"].ToString()).ToString("dd/MM/yy") + "</td>\r\n");
	    string inv_number = dr["invoice_number"].ToString();
		//double inv_number = double.Parse(dr["invoice_number"].ToString());
        if(Math.Round(double.Parse(dr["total"].ToString()),0) == 0 )
		inv_number = inv_number +"( "+ double.Parse(dr["creditapplied"].ToString()).ToString("c")+")";
		/*
		if(inv_number == "0")
			inv_number = "";
		*/
		sb.Append("<td align=center>");
		if(dr["customer_gst"].ToString() == "0")
			sb.Append("N0");
		else
			sb.Append("NZ");
		sb.Append(inv_number + "</a></td>");
		
		//if(dr["cust_ponumber"].ToString() == "")
		//	sb.Append("<td align=center>&nbsp;</td>");
		//else
			sb.Append("<td align=center>" + dr["cust_ponumber"].ToString() + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["total"].ToString()).ToString("c")  + "</td>\r\n");
		//if(dr["amount_paid"].ToString() == "0")
		//	sb.Append("<td align=right>&nbsp;</td>\r\n");
		//else
			sb.Append("<td align=right>" + double.Parse(dr["amount_paid"].ToString()).ToString("c")  + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "</td></tr>\r\n");
		invTotal += double.Parse(dr["total"].ToString());
		totalPaid += double.Parse(dr["amount_paid"].ToString());
		totalbal += double.Parse(dr["cur_bal"].ToString());
		if(dr["cust_ponumber"].ToString() == "Credit")
		creditTotalBalance  += double.Parse(dr["cur_bal"].ToString());
		if(dr["customer_gst"].ToString() == "0")
		{
			dNOTotalUpaid += double.Parse(dr["cur_bal"].ToString());
			dNOTotalPaid  += double.Parse(dr["amount_paid"].ToString());
		}
		else
		{
			dNZTotalUpaid += double.Parse(dr["cur_bal"].ToString());
			dNZTotalPaid  += double.Parse(dr["amount_paid"].ToString());
		}
	}
	  
		
		string subTitle = "Unpaid";
		if(totalbal < 0 )
		subTitle = " Credit";
		
		sb.Append("<tr><td colspan=6 align=right>NO UNPAID: "+ dNOTotalUpaid.ToString("c")+"</td></tr>");
		sb.Append("<tr><td colspan=6 align=right>NZ UNPAID: "+  dNZTotalUpaid.ToString("c")+"</td></tr>");
		sb.Append("<tr><td colspan=6 align=right>TOTAL UNPAID BALANCE: "+  (dNZTotalUpaid+dNOTotalUpaid).ToString("c")+"</td></tr>");
		if(Request.QueryString["a"] != "e" )
		{
			sb.Append("<tr><td colspan=6 align=right><input type=button name=cmd value='Send Mail' onclick=\"window.location=('?a=e");
			if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
				sb.Append("&t="+Request.QueryString["t"].ToString());
			if(Request.QueryString["ci"] != "" && Request.QueryString["ci"] != null)
				sb.Append("&ci="+Request.QueryString["ci"].ToString());
			sb.Append("&period="+Request.Form["period"].ToString());
			sb.Append("&frm="+m_sdFrom);
			sb.Append("&to="+m_sdTo);
			sb.Append("')\">");
			sb.Append("</td></tr>");
		}
		 
		sb.Append("<tr><td colspan=6>");
		sb.Append(GetLatestTrans(Request.QueryString["ci"].ToString()));
		sb.Append("</td></tr>");
		sb.Append("<tr><td colspan=6 align=left valgin=top >");
		sb.Append("<table cellspacing=0 cellpadding=0 border=0 bordercolor=black bgcolor=white style=\"font-family:Arial;font-size:6pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\" id=\"table1\">");
		sb.Append("<tr><td >");

		sb.Append(GetCustName("note",Request.QueryString["ci"].ToString()));
		sb.Append("</td></tr></table>");
		sb.Append("</td></tr>");
		
		sb.Append("</table>");
		
    	return sb.ToString();
}

String PrintStatmentDetails()
{


	StringBuilder sb = new StringBuilder();
     if(Session["branch_id"] != null)
	 	m_branchID =  Session["branch_id"].ToString();
	string sCust_Branch = GetCustName("our_branch", m_custID);
	if(sCust_Branch == "" && sCust_Branch == null)
		sCust_Branch = m_branchID;
	//DEBUG("ID " , Session["branch_id"].ToString());
	//build up body
	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	//sb.Append(ReadSitePage("statement_header_full"));
	sb.Append(GetBrandHeader(sCust_Branch));
    sb.Append("<table width="+ tableWidth +" border=0 cellspacing=0 cellpadding=0><tr><td valign=top>");
	sb.Append("<table width=100% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><td width=73% valign=top>");
	sb.Append("<table width=100% align=center cellspacing=3 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><td valign=top>\r\n");
	//sb.Append("<table width=100% align=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	//sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><td align=left colspan=2><b>Company name</b>:&nbsp;"+ GetCustName("trading_name", m_custID)+"</td><td colspan=2></td><td align=right colspan=2><b>Date:</b>&nbsp;"+m_datePeriod+"</td>");
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td width=15% align=center><b>DATE</b></td>");
	sb.Append("<td width=18% align=center><b>Inv No.</b></td>");
	sb.Append("<td width=17% align=center><b>Order no.</b></td>");
	sb.Append("<td width=15% align=center><b>Sales Amount</b></td>");
	sb.Append("<td width=15% align=center><b>Paid</b></td>");
	sb.Append("<td width=20% align=center><b>Unpaid </b></td></tr>\r\n");
	
	//rows on left side of statement
	sb.Append(sStateRowsLeft());
	sb.Append("</table>\r\n");
    double dCreditTotal = GetTotalCredit(m_custID);
	double[] dSubBalance = new double[4];
	////get the select month issue only
	string pickMonth = "";
	string pickYear = "";
	if(Request.Form["pickMonth"] != null && Request.Form["pickMonth"] != "")
		pickMonth = Request.Form["pickMonth"].ToString();
	if(Request.Form["pickYear"] != null && Request.Form["pickYear"] != "")
		pickYear = Request.Form["pickYear"].ToString();
	
	int nTotalMonth = 0, nCurrentMonth = 0;
	int nStartCount = 0, nEndCount = 4;
	nCurrentMonth = int.Parse(DateTime.Now.ToString("MM"));
	string currentYear = DateTime.Now.ToString("yyyy");
	int nStoreID = 0;
	if(pickMonth != "all" && pickMonth != null && pickMonth != "")
	{
		nStartCount = int.Parse(pickMonth);
		nEndCount = nStartCount + 1;
		nStoreID = int.Parse(pickMonth);
	}
	
	for(int i = nStartCount; i<nEndCount; i++)
	{		
		GetSubBalance(i, m_custID, ref dSubBalance[nStoreID]);		
		nStoreID++;		
		
	}


	sb.Append("</td></tr></table>\r\n");

	sb.Append("</body></html>");
	emailTem = sb.ToString();
	return sb.ToString();
}


double GetTotalCredit(string cardID)
{
	if(dst.Tables["credit"] != null)
		dst.Tables["credit"].Clear();

	string sc = "SELECT SUM(amount - amount_applied) AS total ";
	sc += " FROM credit ";
	sc += " WHERE card_id = " + cardID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "credit");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}
	return MyDoubleParse(dst.Tables["credit"].Rows[0]["total"].ToString());
}



bool GetSubBalance(int i, string sCardID, ref double dtotal)
{
	if(dst.Tables["balance"] != null)
		dst.Tables["balance"].Clear();
	int rows = 0;
	string sc= "";	

	if(!bCardType(ref sCardID))
	{
		sc= " SELECT SUM(total - amount_paid) AS sub_total FROM invoice WHERE card_id = " + sCardID;
		if(m_scredit_terms_id == "4")  // ** 7days
		{			
			if(i == 0)
				sc += "	AND (DATEDIFF(day, commit_date, GETDATE()) >= 0 AND DATEDIFF(day, commit_date, GETDATE()) < 7)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 7 AND DATEDIFF(day, commit_date, GETDATE()) < 15)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 15 AND DATEDIFF(day, commit_date, GETDATE()) < 22)";
			else if(i == 3)
				sc += " AND DATEDIFF(day, commit_date, GETDATE()) >= 22";
			else
			{
				dtotal = 0.00;
				return true;
			}

		}
		else if(m_scredit_terms_id == "5") // ** 14days
		{
			if(i == 0)
				sc += "	AND (DATEDIFF(day, commit_date, GETDATE()) >= 0 AND DATEDIFF(day, commit_date, GETDATE()) < 14)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 14 AND DATEDIFF(day, commit_date, GETDATE()) < 29)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 29 AND DATEDIFF(day, commit_date, GETDATE()) < 59 )";
			else if(i == 3)
				sc += " AND DATEDIFF(day, commit_date, GETDATE()) >= 59";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		else // ** the rest 30days
		{
			if(i == 0)
				sc += "	AND DATEDIFF(month, commit_date, GETDATE()) = 0 ";
			else if(i == 1)
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 1 ";
			else if(i == 2)
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 2";
			else if(i == 3)
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) >= 3";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		sc += " AND paid = 0 ";
//		DEBUG("s c= ", sc);
	}
	else
	{
		sc = " SELECT SUM(total_amount - amount_paid) AS sub_total FROM purchase WHERE supplier_id = " + sCardID;
		if(m_scredit_terms_id == "4")  // ** 7days
		{
			if(i == 0)
				sc += "	AND (DATEDIFF(day, date_received, GETDATE()) >= 0 AND DATEDIFF(day, date_received, GETDATE()) < 7)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 7 AND DATEDIFF(day, date_received, GETDATE()) < 15)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 15 AND DATEDIFF(day, date_received, GETDATE()) < 22)";
			else if(i == 3)
				sc += " AND DATEDIFF(day, date_received, GETDATE()) >= 22";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
			else if(m_scredit_terms_id == "5") // ** 14days
		{
			if(i == 0)
				sc += "	AND (DATEDIFF(day, date_received, GETDATE()) >= 0 AND DATEDIFF(day, date_received, GETDATE()) < 14)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 14 AND DATEDIFF(day, date_received, GETDATE()) < 29)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 29 AND DATEDIFF(day, date_received, GETDATE()) < 59 )";
			else if(i == 3)
				sc += " AND DATEDIFF(day, date_received, GETDATE()) >= 59";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		else // ** the rest 30days
		{
			if(i == 0)
				sc += "	AND DATEDIFF(month, date_received, GETDATE()) = 0 ";
			else if(i == 1)
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 1 ";
			else if(i == 2)
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 2";
			else if(i == 3)
				sc += " AND DATEDIFF(month, date_received, GETDATE()) >= 3";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		sc += " AND amount_paid = 0 ";
	
//		sc += " AND date_received is NOT NULL ";
//DEBUG("sc = ", sc);
	}

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "balance");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(dst.Tables["balance"].Rows[0]["sub_total"].ToString() == "" || dst.Tables["balance"].Rows[0]["sub_total"].ToString() == null)
		dtotal = 0;
	else
		dtotal = double.Parse(dst.Tables["balance"].Rows[0]["sub_total"].ToString());

	return true;
}
void PrintMainPage()
{

	Response.Write("<form name=f action='stat_tran.aspx?");
    Response.Write("ci="+Request.QueryString["ci"]+"&p=0&t=vd");
	
	Response.Write("' method=post>");

	Response.Write("<br><center><h3>Select Report</h3>");
	 
	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	

	string uri = Request.ServerVariables["URL"].ToString();
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Select Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Yestoday</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=6>The Day Before Yesterday</td></tr>");
	Response.Write("<input type=hidden name=dateSelected value=1 >");
	/////allow only authorized user to access...
	//if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
		Response.Write("<tr><td colspan=6><input type=radio name=period value=2>This Week</td></tr>");
		Response.Write("<tr><td colspan=6><input type=radio name=period value=3>This Month</td></tr>");
		Response.Write("<tr><td colspan=6><input type=radio name=period value=4>Select Date Range</td></tr>");
	
		int i = 1;
		datePicker(); //call date picker function
		Response.Write("<tr><td><b> &nbsp; From Date </b>");
		
		string s_day = DateTime.Now.ToString("dd");
		string s_month = DateTime.Now.ToString("MM");
		string s_year = DateTime.Now.ToString("yyyy");
		Response.Write("<select name='Datepicker1_day' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
		for(int d=1; d<32; d++)
		{
			Response.Write("<option value="+ d +">"+d+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='Datepicker1_month' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			if(int.Parse(s_month) == m)
				Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
			else
				Response.Write("<option value="+m+">"+txtMonth+"</option>");
		}
		
		Response.Write("</select>");
		Response.Write("<select name='Datepicker1_year' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
		for(int y=2000; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");

		Response.Write("<input type=hidden name='Datepicker1'>");
		Response.Write("<input type=hidden name='Datepicker1_conv'>");
		Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker1',1)");
		Response.Write("</script ");
		Response.Write(">");

		Response.Write("<td> &nbsp; TO: ");
		Response.Write("<select name='Datepicker2_day' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
		for(int d=1; d<32; d++)
		{
			if(int.Parse(s_day) == d)
				Response.Write("<option value="+ d +" selected>"+d+"</option>");
			else
				Response.Write("<option value="+ d +">"+d+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='Datepicker2_month' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			if(int.Parse(s_month) == m)
				Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
			else
				Response.Write("<option value="+m+">"+txtMonth+"</option>");
		}
		
		Response.Write("</select>");
		Response.Write("<select name='Datepicker2_year' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
		for(int y=2000; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<input type=hidden name='Datepicker2'>");
		Response.Write("<input type=hidden name='Datepicker2_conv'>");
		Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker2',1)");
		Response.Write("</script ");
		Response.Write(">");
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td align=right colspan=6><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");

}
string GetCustName(string col, string code)
{
	//DataSet dsd = new DataSet();
	string sc = "SELECT "+ col +" from card where id="+code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	string nRet = dst.Tables[0].Rows[0][col].ToString();
	return nRet;
}

string GetBrandHeader(string bID)
{
	//DataSet dsd = new DataSet();
	int rows = 0;
	string sc = "select * From branch where id="+bID;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "branch");
		if(rows <= 0)
		{
			return "";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string nBranchHeader = dst.Tables["branch"].Rows[0]["branch_header"].ToString();
	return nBranchHeader;
}
string GetLatestTrans(string cardID)
{
   int rows =0;
   string transTable = "<table width=100% cellspacing=0 cellpadding=1 style=\"border-top:#000000 1px solid;border-right:#000000 1px solid;border-left:#000000 1px solid;border-bottom:#000000 1px solid\" >";
   transTable += "<tr bgcolor=\"#6699CC\" style=\"text-align:left\" valign=\"middle\" height=\"20\"><td style=\"font:bold 12px Arial, Helvetica, sans-serif; color:#FFFFFF \">NO</td><td style=\"font:bold 12px Arial, Helvetica, sans-serif; color:#FFFFFF \">Date</td><td style=\"font:bold 12px Arial, Helvetica, sans-serif; color:#FFFFFF\">Paid Amount </td><td style=\"font:bold 12px Arial, Helvetica, sans-serif; color:#FFFFFF\">Remark</td></tr>";
   string sc = "SELECT TOP 10 td.invoice_number, td.trans_date, t.amount ,td.note FROM trans t JOIN tran_detail td ON t.id=td.id";
   sc += " WHERE td.card_id="+cardID;
   sc += " ORDER BY t.id DESC";
   try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "latestTrans");
		if(rows <= 0)
		{
			return "";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string iv = "";
	int basic = 30;
	for(int i = 0; i < dst.Tables["latestTrans"].Rows.Count; i++)
	{
	    DataRow dr = dst.Tables["latestTrans"].Rows[i];
		string inv_number = dr["invoice_number"].ToString();
		int inv_number_l = inv_number.Length;
		int repeat = inv_number_l / 30;
		
	//DEBUG("repeat ", repeat.ToString());
		if(repeat >=2)
		{
			for(int e = 0 ; e < repeat+1 ; e ++)
			{
				//DEBUG("repeat ", repeat.ToString());
				
					int InvLenght = e*basic;
					int res = inv_number_l - InvLenght;
					   if(e != repeat )
					    iv += inv_number.Substring(InvLenght, basic) +"<Br>";
						if(res < basic)
							iv += inv_number.Substring(InvLenght);
							
				//DEBUG("res1"+e+" " , iv);		
					
		//DEBUG("InvL ", InvLenght.ToString());
		//DEBUG("res ", res.ToString());
				
			}
			//inv_number = iv;
			//DEBUG("f " , iv);
			inv_number = iv;
		}
		
		string trans_date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd/MM/yyyy");
	    string trans_amount = dr["amount"].ToString();
		string trans_note = dr["note"].ToString();
		transTable += "<tr";
		if(i % 2 == 1)
			transTable += " bgcolor=#F1F1F1" ;
		transTable += " height=20 ><td valign=middle >"+ inv_number + "</td><td valign=middle >"+ trans_date + "</td><td valign=middle >"+ double.Parse(trans_amount).ToString("c") + "</td><td valign=middle >"+ trans_note + "</td></tr>";
		
		
		
	}
	transTable += "</table>";
	return transTable;
}
</script>
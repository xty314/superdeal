<script runat=server>

int pageNo = 1;
string m_timeOpt = "0";
string m_custID = "";
DataSet dsti= new DataSet();
string m_scredit_terms_id = "6";
string m_scredit_terms = "";
double d_SumTotal = 0;
StringBuilder sbTableHeader = new StringBuilder();

bool GetSelectedCust(string cardID)
{
	if(dsti.Tables["cust_gen"] != null)
		dsti.Tables["cust_gen"].Clear();
/*
	string sc = "SELECT c.id, e.name AS credit_terms, c.credit_limit, e.id AS terms_id, c.email, c.ap_email, c.type, c.name, c.short_name, c.company, c.trading_name, c.phone, c.fax, c.balance, c.companyB, ";
	sc += "c.nameB, c.address1B, c.address2B, c.cityB, c.countryB, c.postal1, c.postal2, c.postal3, c.note FROM card c";
	*/
	string sc = "SELECT c.id, e.name AS credit_terms, c.credit_limit, e.id AS terms_id, c.*";
	sc += " FROM card c";
	sc += " JOIN enum e ON e.id = c.credit_term ";
	sc += " WHERE e.class = 'credit_terms' ";
	sc += " AND c.id = " + cardID;
//DEBUG("sc =", sc);

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsti, "cust_gen") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;

}

string  GetSelectedCustByID(string cardID, string field)
{
	if(dsti.Tables["cust_gen"] != null)
		dsti.Tables["cust_gen"].Clear();
/*
	string sc = "SELECT c.id, e.name AS credit_terms, c.credit_limit, e.id AS terms_id, c.email, c.ap_email, c.type, c.name, c.short_name, c.company, c.trading_name, c.phone, c.fax, c.balance, c.companyB, ";
	sc += "c.nameB, c.address1B, c.address2B, c.cityB, c.countryB, c.postal1, c.postal2, c.postal3, c.note FROM card c";
	*/
	string sc = "SELECT c."+field;
	sc += " FROM card c";
	sc += " JOIN enum e ON e.id = c.credit_term ";
	sc += " WHERE e.class = 'credit_terms' ";
	sc += " AND c.id = " + cardID;
//DEBUG("sc =", sc);

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsti, "cust_gen") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	return dsti.Tables["cust_gen"].Rows[0][field].ToString();

}


bool GetInvRecords(string cardID)
{
	if(dsti.Tables["invoice_rec"] != null)
		dsti.Tables["invoice_rec"].Clear();
	string sc = "";
//	DEBUG("Month =", Request.Form["pickMonth"].ToString());
///	DEBUG("Year =", Request.Form["pickYear"].ToString());
	
	string pickMonth = "";
	string pickYear = "";
	if(Request.Form["pickMonth"] != null && Request.Form["pickMonth"] != "")
		pickMonth = Request.Form["pickMonth"].ToString();
//	if(Request.Form["pickYear"] != null && Request.Form["pickYear"] != "")
//		pickYear = Request.Form["pickYear"].ToString();
	
	int nTotalMonth = 0;
	string currentYear = DateTime.Now.ToString("yyyy");
/*	if(pickMonth != "")
		nTotalMonth = (int.Parse(currentYear) - int.Parse(pickYear)) * 12;	
	*/
	if(pickMonth != "all" && pickMonth != null && pickMonth != "")
	{		
		m_timeOpt = pickMonth;		
	}
//DEBUG("m_scredit =", m_scredit_terms_id);
//	DEBUG("ntaot+", nTotalMonth);
	if(!bCardType(ref cardID))
	{
		sc = "SELECT '0' AS pid, i.invoice_number, i.commit_date, i.freight, i.cust_ponumber, i.total, Isnull(i.amount_paid,0) AS amount_paid, ";
		sc += "(i.total - i.amount_paid) AS cur_bal FROM invoice i ";
		sc += " WHERE i.paid = 0 ";
	//	sc += " AND (ROUND(i.total,2)-ROUND(i.amount_paid,2)) > 0.0099  ";
		sc += " AND i.card_id = " + cardID;
	
	/*	if(pickMonth != null && pickMonth != "" && TSIsDigit(pickMonth))
		{
			sc += "	AND (DATEDIFF(month, i.commit_date, GETDATE()) = "+ ((int.Parse(DateTime.Now.ToString("MM")) - int.Parse(pickMonth)) + nTotalMonth) +")";
		}
	*/
		if(m_scredit_terms_id == "4")  // ** 7days
		{
			if(m_timeOpt == "0")
				sc += "	AND (DATEDIFF(day, i.commit_date, GETDATE()) >= 0 AND DATEDIFF(day, i.commit_date, GETDATE()) < 7)";
			else if(m_timeOpt == "1")
				sc += " AND (DATEDIFF(day, i.commit_date, GETDATE()) >= 7 AND DATEDIFF(day, i.commit_date, GETDATE()) < 15)";
			else if(m_timeOpt == "2")
				sc += " AND (DATEDIFF(day, i.commit_date, GETDATE()) >= 15 AND DATEDIFF(day, i.commit_date, GETDATE()) < 22)";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(day, i.commit_date, GETDATE()) >= 22";

		}
		else if(m_scredit_terms_id == "5") // ** 14days
		{
			if(m_timeOpt == "0")
				sc += "	AND (DATEDIFF(day, i.commit_date, GETDATE()) >= 0 AND DATEDIFF(day, i.commit_date, GETDATE()) < 14)";
			else if(m_timeOpt == "1")
				sc += " AND (DATEDIFF(day, i.commit_date, GETDATE()) >= 14 AND DATEDIFF(day, i.commit_date, GETDATE()) < 29)";
			else if(m_timeOpt == "2")
				sc += " AND (DATEDIFF(day, i.commit_date, GETDATE()) >= 29 AND DATEDIFF(day, i.commit_date, GETDATE()) < 59 )";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(day, i.commit_date, GETDATE()) >= 59";
		}
		/*else if(m_scredit_terms_id == "7") // ** 2oth of the month
		{
			if(m_timeOpt == "0")
				sc += "	AND DATEDIFF(month, commit_date, GETDATE()) = 0 ";
			else if(m_timeOpt == "1")
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 1 ";
			else if(m_timeOpt == "2")
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 2";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) >= 3";
		}*/
		else // ** the rest 30days
		{
			if(m_timeOpt == "0")
				sc += "	AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
			else if(m_timeOpt == "1")
				sc += " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
			else if(m_timeOpt == "2")
				sc += " AND DATEDIFF(month, i.commit_date, GETDATE()) = 2";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 3";
		}		
		//////get all the credit records as well.....////////
		sc += " UNION ";
		sc += "SELECT '0' AS pid, '0' AS invoice_number, ISNULL(td.trans_date, GETDATE()) AS commit_date, 0 AS freight, 'Credit' AS cust_ponumber,  0 AS total, c.amount AS amount_paid, ";
		sc += " 0 - (c.amount - c.amount_applied) AS cur_bal ";
		sc += " FROM credit c  ";
		sc += " JOIN trans t ON t.id = c.tran_id JOIN tran_detail td ON td.id = t.id AND td.id = c.tran_id ";		
		sc += " WHERE c.card_id = " + cardID +" AND (c.amount - c.amount_applied) <> 0 ";
		///////end here //////
		//sc += " ORDER BY i.commit_date DESC";
	}
	else
	{
		sc = " SELECT id AS pid, inv_number AS invoice_number, isnull(date_invoiced, date_received) as commit_date, freight ";
		sc += " , po_number as cust_ponumber, total_amount AS total, amount_paid, ";
		sc += "(total_amount - amount_paid) AS cur_bal ";
		sc += " FROM purchase ";
		sc += " WHERE (type >= 2)  AND date_received IS NOT NULL "; //AND (total_amount - amount_paid) > 0
		sc += " AND supplier_id = " + cardID;
		if(m_scredit_terms_id == "4")
		{
			if(m_timeOpt == "0")
				sc += "	AND (DATEDIFF(day, date_received, GETDATE()) >= 0 AND DATEDIFF(day, date_received, GETDATE()) < 7)";
			else if(m_timeOpt == "1")
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 7 AND DATEDIFF(day, date_received, GETDATE()) < 15)";
			else if(m_timeOpt == "2")
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 15 AND DATEDIFF(day, date_received, GETDATE()) < 22)";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(day, date_received, GETDATE()) >= 22";

		}
		else if(m_scredit_terms_id == "5")// ** 14days
		{
				if(m_timeOpt == "0")
				sc += "	AND (DATEDIFF(day, date_received, GETDATE()) >= 0 AND DATEDIFF(day, date_received, GETDATE()) < 14)";
			else if(m_timeOpt == "1")
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 14 AND DATEDIFF(day, date_received, GETDATE()) < 29)";
			else if(m_timeOpt == "2")
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 29 AND DATEDIFF(day, date_received, GETDATE()) < 59 )";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(day, date_received, GETDATE()) >= 59";
		
		}
		/*else if(m_scredit_terms_id == "7")
		{
			if(m_timeOpt == "0")
				sc += "	AND DATEDIFF(month, date_received, GETDATE()) = 0 ";
			else if(m_timeOpt == "1")
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 1 ";
			else if(m_timeOpt == "2")
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 2";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(month, date_received, GETDATE()) >= 3";
		}*/
		else// ** the rest 30days
		{
			if(m_timeOpt == "0")
				sc += "	AND DATEDIFF(month, date_received, GETDATE()) = 0 ";
			else if(m_timeOpt == "1")
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 1 ";
			else if(m_timeOpt == "2")
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 2";
			else if(m_timeOpt == "3")
				sc += " AND DATEDIFF(month, date_received, GETDATE()) >= 3";
		}

		//sc += " ORDER BY date_received DESC";
	}
//DEBUG("me sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsti, "invoice_rec");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}


String PrintStatmentDetails()
{
	string header = ReadSitePage("statement_header");
	string header2 = ReadSitePage("statement_header2");
	string footer = ReadSitePage("statement_footer");

	
	StringBuilder sb = new StringBuilder();
	StringBuilder sbTableHeader = new StringBuilder();

	//build up body

	sb.Append(header);
    sb = sb.Replace("@@Page", "@@PPage1");
	sb.Append("<table width=730 border=0 cellspacing=0 cellpadding=0><tr><td valign=top>");

    sb.Append("<div id=\"invoicetable\">");

    /*if (dsti.Tables["invoice_rec"].Rows.Count < 25)
    {
        sb.Append("<div id=\"invoicetable\">");
    }
    else
    {
        sb.Append("<div id=\"invoicetabletwo\">");
    }*/

	sb.Append("<table width=100% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr style=\"font-family:Verdana;font-size:8pt;background-color:orange;border-color:black;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\"><td width=15% align=center><b>DATE</b></td>");
	sb.Append("<td width=18% align=center><b>Invoice no.</b></td>");
	sb.Append("<td width=17% align=center><b>Order no.</b></td>");
	sb.Append("<td width=15% align=right><b>Charges</b></td>");
	sb.Append("<td width=15% align=right><b>Payment</b></td>");
	sb.Append("<td width=20% align=right><b>Total Due</b></td></tr>\r\n");

    sb.Append(sbTableHeader);

	//rows on left side of statement
	sb.Append(sStateRowsLeft());

m_scredit_terms_id = dsti.Tables["cust_gen"].Rows[0]["terms_id"].ToString();
m_scredit_terms = dsti.Tables["cust_gen"].Rows[0]["credit_terms"].ToString();
//DEBUG("msftucret=", m_scredit_terms_id);
	/*sb.Append("<tr width=100%><td align=center><b> Current</b></td>\r\n");
	//sb.Append("<td align=center><b>30 Days</b></td>\r\n");
	if(m_scredit_terms_id == "4")
		sb.Append("<td align=center><b>7 Days</b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		sb.Append("<td align=center><b>14 Days</b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	sb.Append("<td align=center><b>20th of the Month</b></td>\r\n");
	else
		sb.Append("<td align=center><b>30 Days</b></td>\r\n");
	//sb.Append("<td align=center><b>60 Days</b></td>\r\n");
		if(m_scredit_terms_id == "4")
		sb.Append("<td align=center><b>14 Days</b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		sb.Append("<td align=center><b>30 Days</b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	sb.Append("<td align=center><b>20th of the Month</b></td>\r\n");
	else
		sb.Append("<td align=center><b>60 Days</b></td>\r\n");
	//sb.Append("<td align=center><b>90 Days+ </b></td>\r\n");
		if(m_scredit_terms_id == "4")
		sb.Append("<td align=center><b>30 Days+</b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		sb.Append("<td align=center><b>60 Days+</b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	sb.Append("<td align=center><b>20th of the Month</b></td>\r\n");
	else
		sb.Append("<td align=center><b>90 Days+</b></td>\r\n");
	sb.Append("<td align=center><b>Credits Left</b></td></tr>\r\n");*/

	double dCreditTotal = GetTotalCredit(m_custID);
//DEBUG("dCreditTotal ", dCreditTotal.ToString());
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
/*	if(pickMonth != "")
		nTotalMonth = (int.Parse(currentYear) - int.Parse(pickYear)) * 12;	
//	DEBUG("nTotalMonth =", pickMonth +" curr = "+ nCurrentMonth +"= " + nTotalMonth);
if(pickMonth != null && pickMonth != "" && TSIsDigit(pickMonth))
{
nStartCount = ((nCurrentMonth) - (int.Parse(pickMonth) + nTotalMonth));
if(nStartCount < 0 || nStartCount > 5)
nStartCount = 4;
nEndCount = nStartCount + 1;
				//DEBUG("pick moanth=",((nCurrentMonth + nTotalMonth) - int.Parse(pickMonth)));
}
*/
	int nStoreID = 0;
	if(pickMonth != "all" && pickMonth != null && pickMonth != "")
	{
		nStartCount = int.Parse(pickMonth);
		//if(pickMonth == "3")
			
		nEndCount = nStartCount + 1;
//	DEBUG("nsta =", nStartCount);
	nStoreID = int.Parse(pickMonth);
	}
	
	for(int i = nStartCount; i<nEndCount; i++)
	{		
		GetSubBalance(i, m_custID, ref dSubBalance[nStoreID]);		
		nStoreID++;		
		//if(!GetSubBalance(i, ref dSubBalance[i]))
		//	return;		
//		DEBUG(" nStoreID=", nStoreID);
	}
/*	sb.Append("<tr><td align=center><b>" + dSubBalance[0].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[1].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[2].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[3].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dCreditTotal.ToString("c") + "</b></td></tr>\r\n");*/



	//sb.Append("<td width=27% align=left>");
	//sb.Append("<table width=85%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	//sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	//sb.Append("<tr><td align=right><b>Total Amount Due:</b></td></tr>\r\n");

	double d_SumTotal = dSubBalance[0] + dSubBalance[1] + dSubBalance[2] + dSubBalance[3];
	d_SumTotal -= dCreditTotal;


	sb.Append("</table>");
	sb.Append("</table></td></tr>\r\n");
	sb.Append("</table>\r\n</td></tr></table>\r\n</div>");
	sb.Append("<table width=730 style=\"font-family:Verdana;font-size:8pt;background-color:orange;border-color:black;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\"0><tr><td align=right><b>STATEMENT TOTAL:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b>         " + d_SumTotal.ToString("c") + "</td></tr></table><br>\r\n");

	sb.Append(footer);

	sb.Append("</body></html>");




	sb = sb.Replace("@@title", "STATEMENT");
	sb = sb.Replace("@@date", DateTime.Now.ToString("dd-MMM-yyyy"));
	sb = sb.Replace("@@account_id", dsti.Tables["cust_gen"].Rows[0]["id"].ToString());
	sb = sb.Replace("@@compname", dsti.Tables["cust_gen"].Rows[0]["trading_name"].ToString());
	sb = sb.Replace("@@credit_terms", dsti.Tables["cust_gen"].Rows[0]["credit_terms"].ToString());
	sb = sb.Replace("@@credit_limit", MyDoubleParse(dsti.Tables["cust_gen"].Rows[0]["credit_limit"].ToString()).ToString("c"));
	sb = sb.Replace("@@pobox", dsti.Tables["cust_gen"].Rows[0]["postal1"].ToString());
	sb = sb.Replace("@@suburb", dsti.Tables["cust_gen"].Rows[0]["postal2"].ToString());
	sb = sb.Replace("@@city", dsti.Tables["cust_gen"].Rows[0]["postal3"].ToString());
	sb = sb.Replace("@@email", dsti.Tables["cust_gen"].Rows[0]["email"].ToString());
	sb = sb.Replace("@@phone", dsti.Tables["cust_gen"].Rows[0]["phone"].ToString());
	sb = sb.Replace("@@fax", dsti.Tables["cust_gen"].Rows[0]["fax"].ToString());

	sb = sb.Replace("@@cur_days", dSubBalance[0].ToString("c"));
	sb = sb.Replace("@@30_days", dSubBalance[1].ToString("c"));
	sb = sb.Replace("@@60_days", dSubBalance[2].ToString("c"));
	sb = sb.Replace("@@90_days", dSubBalance[3].ToString("c"));
	sb = sb.Replace("@@tot_days", d_SumTotal.ToString("c"));
	sb = sb.Replace("@@account_note", dsti.Tables["cust_gen"].Rows[0]["note"].ToString());
	sb = sb.Replace("@@amountdue", d_SumTotal.ToString("c"));

    for (int i = 0; i < pageNo; i++)
    {
        int j = i + 1;
        sb = sb.Replace("@@PPage"+j.ToString(), "" + j.ToString() + " of " + pageNo.ToString());
    }


	return sb.ToString();
}

String PrintStatmentDetailsPDF(string selected)
{
	
	double dCreditTotal = GetTotalCredit(m_custID);

	double[] dSubBalance = new double[4];
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
	d_SumTotal = dSubBalance[0] + dSubBalance[1] + dSubBalance[2] + dSubBalance[3];
	d_SumTotal -= dCreditTotal;
	switch (selected)
	{
		case "0":
		  return dSubBalance[0].ToString("c");
		  break;
		case "1":
		  return dSubBalance[1].ToString("c");
		  break;
		case "2":
		  return dSubBalance[2].ToString("c");
		  break;
		case "3":
		  return dSubBalance[3].ToString("c");
		  break;
		case "total_due":
		  return d_SumTotal.ToString("c");
		  break;
		default:
		  break;
	}
	
	
	 


	return "";
}

string sStateRowsLeft()
{
	StringBuilder sb = new StringBuilder();

	string s_empPOno = "";
	for(int i = 0; i < dsti.Tables["invoice_rec"].Rows.Count; i++)
	{
        if ((i%28==0) && (i > 0))
        {
            pageNo += 1;
            sb.Append("</table>");
	        //sb.Append("<br><br><br><br><br><br><br><br><br><br>");
	        string footer = ReadSitePage("statement_footer");
	        sb.Append(footer);
	        sb.Append("<br><br><br>");

	        string header = ReadSitePage("statement_header");

	        header = header.Replace("@@Page", "@@PPage"+pageNo.ToString());
	        sb.Append(header);
	        sb.Append("<table width=730 border=0 cellspacing=0 cellpadding=0><tr><td valign=top>");
            sb.Append("<div id=\"invoicetable2\">");
	        sb.Append("<table width=100% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	        sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	        sb.Append("<tr style=\"font-family:Verdana;font-size:8pt;background-color:orange;border-color:black;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\"><td width=15% align=center><b>DATE</b></td>");
	        sb.Append("<td width=18% align=center><b>Invoice 1 no.</b></td>");
	        sb.Append("<td width=17% align=center><b>Order no.</b></td>");
	        sb.Append("<td width=15% align=center><b>Charges</b></td>");
	        sb.Append("<td width=15% align=center><b>Payment</b></td>");
	        sb.Append("<td width=20% align=center><b>Total Due</b></td></tr>\r\n");
        }

		DataRow dr = dsti.Tables["invoice_rec"].Rows[i];
		sb.Append("<tr><td align=center>" + DateTime.Parse(dr["commit_date"].ToString()).ToString("dd/MM/yy") + "</td>\r\n");
		string inv_number = dr["invoice_number"].ToString();
		if(inv_number == "0")
			inv_number = "";
		//Response.Write(dr["invoice_number"].ToString() + "</a></td>");
		sb.Append("<td align=center>" + inv_number + "</a></td>");
		//sb.Append("<td align=center>" + dr["invoice_number"].ToString() + "</td>\r\n");
		if(dr["cust_ponumber"].ToString() == "")
			sb.Append("<td align=center>&nbsp;</td>");
		else
			sb.Append("<td align=center>" + dr["cust_ponumber"].ToString() + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["total"].ToString()).ToString("c")  + "</td>\r\n");
		if(dr["amount_paid"].ToString() == "0")
			sb.Append("<td align=center>&nbsp;</td>\r\n");
		else
			sb.Append("<td align=right>" + double.Parse(dr["amount_paid"].ToString()).ToString("c")  + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "</td></tr>\r\n");

	}

	return sb.ToString();
}

string sStateRowsRight()
{
	StringBuilder sb = new StringBuilder();
	for(int i = 0; i < dsti.Tables["invoice_rec"].Rows.Count; i++)
	{
		/*DataRow dr = dsti.Tables["invoice_rec"].Rows[i];
			string inv_number = dr["invoice_number"].ToString();
		if(inv_number == "0")
			inv_number = "";
		//Response.Write(dr["invoice_number"].ToString() + "</a></td>");
		sb.Append("<td align=right>" + inv_number + "</a></td>");
		//sb.Append("<tr><td align=right>" + dr["invoice_number"].ToString() + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "</td></tr>\r\n");*/

	}
	return sb.ToString();
}	
bool bCardType(ref string cardID)
{
	string stype = "1";
	bool bIsSupplier = false;
	if(dsti.Tables["ctype"] != null)
		dsti.Tables["ctype"].Clear();
	int rows = 0;
	string sc= " SELECT * FROM card WHERE id = " + cardID;
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsti, "ctype");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows == 1)
	{
		stype = dsti.Tables["ctype"].Rows[0]["type"].ToString();
		if(GetEnumID("card_type", "supplier") == stype)
			bIsSupplier = true;
	}

	return bIsSupplier;
}
bool GetSubBalance(int i, string sCardID, ref double dtotal)
{
	if(dsti.Tables["balance"] != null)
		dsti.Tables["balance"].Clear();
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
		/*else if(m_scredit_terms_id == "7") // ** 2oth of the month
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
		}*/
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
		/*
		if(i == 0)
			sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 0 ";
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
		*/
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
		rows = myCommand.Fill(dsti, "balance");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(dsti.Tables["balance"].Rows[0]["sub_total"].ToString() == "" || dsti.Tables["balance"].Rows[0]["sub_total"].ToString() == null)
		dtotal = 0;
	else
		dtotal = double.Parse(dsti.Tables["balance"].Rows[0]["sub_total"].ToString());

	return true;
}

double GetTotalCredit(string cardID)
{
	if(dsti.Tables["credit"] != null)
		dsti.Tables["credit"].Clear();

	string sc = "SELECT SUM(amount - amount_applied) AS total ";
	sc += " FROM credit ";
	sc += " WHERE card_id = " + cardID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsti, "credit");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}
	return MyDoubleParse(dsti.Tables["credit"].Rows[0]["total"].ToString());
}

</script>
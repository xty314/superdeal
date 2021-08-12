<script runat=server>

string m_timeOpt = "0";
string m_custID = "";
DataSet dst = new DataSet();
string m_scredit_terms_id = "6";
string m_scredit_terms = "";
bool GetSelectedCust(string cardID)
{
	if(dst.Tables["cust_gen"] != null)
		dst.Tables["cust_gen"].Clear();
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
		if(myCommand.Fill(dst, "cust_gen") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;

}

bool GetInvRecords(string cardID)
{
	if(dst.Tables["invoice_rec"] != null)
		dst.Tables["invoice_rec"].Clear();
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
		sc += " WHERE c.card_id = " + cardID;// +" AND (c.amount - c.amount_applied) <> 0 "; CH
		///////end here //////
		sc += " ORDER BY i.commit_date DESC";
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

		sc += " ORDER BY date_received DESC";
	}
//DEBUG("me sc = ", sc);
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


String PrintStatmentDetails()
{
	string header = ReadSitePage("statement_header");
	string footer = ReadSitePage("statement_footer");

	header = header.Replace("@@title", "STATEMENT");
	header = header.Replace("@@date", DateTime.Now.ToString("dd-MM-yyyy"));
	header = header.Replace("@@account_id", dst.Tables["cust_gen"].Rows[0]["id"].ToString());
	header = header.Replace("@@compname", dst.Tables["cust_gen"].Rows[0]["trading_name"].ToString());
	header = header.Replace("@@credit_terms", dst.Tables["cust_gen"].Rows[0]["credit_terms"].ToString());
	header = header.Replace("@@credit_limit", MyDoubleParse(dst.Tables["cust_gen"].Rows[0]["credit_limit"].ToString()).ToString("c"));
	header = header.Replace("@@pobox", dst.Tables["cust_gen"].Rows[0]["postal1"].ToString());
	header = header.Replace("@@suburb", dst.Tables["cust_gen"].Rows[0]["postal2"].ToString());
	header = header.Replace("@@city", dst.Tables["cust_gen"].Rows[0]["postal3"].ToString());
	header = header.Replace("@@email", dst.Tables["cust_gen"].Rows[0]["email"].ToString());
	header = header.Replace("@@phone", dst.Tables["cust_gen"].Rows[0]["phone"].ToString());
	header = header.Replace("@@fax", dst.Tables["cust_gen"].Rows[0]["fax"].ToString());
	
	StringBuilder sb = new StringBuilder();

	//build up body
	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");

	sb.Append(header);

	sb.Append("<table width=650 border=0 cellspacing=0 cellpadding=0><tr><td valign=top>");

	sb.Append("<table width=100% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	sb.Append("<tr><td width=73% valign=top>");
	sb.Append("<table width=100% align=center cellspacing=3 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><td valign=top>\r\n");
	sb.Append("<table width=100% align=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr><td width=15% align=center><b>DATE</b></td>");
	sb.Append("<td width=18% align=center><b>Invoice no.</b></td>");
	sb.Append("<td width=17% align=center><b>Order no.</b></td>");
	sb.Append("<td width=15% align=center><b>Charges</b></td>");
	sb.Append("<td width=15% align=center><b>Payment</b></td>");
	sb.Append("<td width=20% align=center><b>Total Due</b></td></tr>\r\n");

	//rows on left side of statement
	sb.Append(sStateRowsLeft());

	sb.Append("</table></td></tr>\r\n");
	sb.Append("</td></tr></table>\r\n");
	sb.Append("</td></tr></table>\r\n");

	sb.Append("</td>");		//end of left table		
	sb.Append("<td height=600 style=\"border-left:1px #000000 dashed\">&nbsp;</td>\r\n");//|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|");
	//sb.Append("<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|<br>|</td>\r\n");

	sb.Append("<td width=27% valign=top align=left>");
	sb.Append("<table width=90% align=center cellspacing=3 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr><td valign=top align=left>");
	sb.Append("<table width=95% align=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr><td width=50% align=right><b>Inv No.</b></td>");
	sb.Append("<td width=50% align=right><b>Total Due</b></td></tr>\r\n");

	//rows on right side of statement
	sb.Append(sStateRowsRight());	

	sb.Append("</table></td></tr></table>\r\n");
	sb.Append("</td></tr></table>\r\n");

	sb.Append("</td></tr>\r\n");   //frame one: close first row;

	sb.Append("<tr><td valign=top><table width=650 border=0 cellspacing=0 cellpadding=0>\r\n");
	sb.Append("<tr><td width=73%>\r\n");
	sb.Append("<table width=100%  align=center valign=center cellspacing=3 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");

m_scredit_terms_id = dst.Tables["cust_gen"].Rows[0]["terms_id"].ToString();
m_scredit_terms = dst.Tables["cust_gen"].Rows[0]["credit_terms"].ToString();
//DEBUG("msftucret=", m_scredit_terms_id);
	sb.Append("<tr width=100%><td align=center><b> Current</b></td>\r\n");
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
	sb.Append("<td align=center><b>Credits Left</b></td></tr>\r\n");

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
	sb.Append("<tr><td align=center><b>" + dSubBalance[0].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[1].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[2].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[3].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dCreditTotal.ToString("c") + "</b></td></tr>\r\n");

	sb.Append("</table>");

	sb.Append("</td><td style=\"border-left:1px #000000 dashed\">&nbsp;</td>");
	sb.Append("<td width=27% align=left>");
	sb.Append("<table width=85%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr><td align=right><b>Total Amount Due:</b></td></tr>\r\n");

	double d_SumTotal = dSubBalance[0] + dSubBalance[1] + dSubBalance[2] + dSubBalance[3];
	d_SumTotal -= dCreditTotal;

	sb.Append("<tr><td align=right><br><b>" + d_SumTotal.ToString("c") + "</b></td></tr>\r\n");
	sb.Append("</table></td></tr>\r\n");
	sb.Append("</table>\r\n</td></tr></table>\r\n");
	footer = footer.Replace("@@account_note", dst.Tables["cust_gen"].Rows[0]["note"].ToString());
	footer = footer.Replace("@@amountdue", d_SumTotal.ToString("c"));
	sb.Append(footer);

	sb.Append("</body></html>");
	return sb.ToString();
}

string sStateRowsLeft()
{
	StringBuilder sb = new StringBuilder();

	string s_empPOno = "";
	for(int i = 0; i < dst.Tables["invoice_rec"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["invoice_rec"].Rows[i];
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
			sb.Append("<td align=right>&nbsp;</td>\r\n");
		else
			sb.Append("<td align=right>" + double.Parse(dr["amount_paid"].ToString()).ToString("c")  + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "&nbsp;&nbsp;&nbsp;</td></tr>\r\n");

	}

	return sb.ToString();
}

string sStateRowsRight()
{
	StringBuilder sb = new StringBuilder();
	for(int i = 0; i < dst.Tables["invoice_rec"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["invoice_rec"].Rows[i];
			string inv_number = dr["invoice_number"].ToString();
		if(inv_number == "0")
			inv_number = "";
		//Response.Write(dr["invoice_number"].ToString() + "</a></td>");
		sb.Append("<td align=right>" + inv_number + "</a></td>");
		//sb.Append("<tr><td align=right>" + dr["invoice_number"].ToString() + "</td>\r\n");
		sb.Append("<td align=right>" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "</td></tr>\r\n");

	}
	return sb.ToString();
}	
bool bCardType(ref string cardID)
{
	string stype = "1";
	bool bIsSupplier = false;
	if(dst.Tables["ctype"] != null)
		dst.Tables["ctype"].Clear();
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

</script>
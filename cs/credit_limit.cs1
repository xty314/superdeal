<script runat="server">

bool IsStopOrdering(string card_id, ref string reason)
{
	DataRow dr = GetCardData(card_id);
	if(dr == null)
		return false;
	reason = dr["stop_order_reason"].ToString();
	return bool.Parse(dr["stop_order"].ToString());
}

bool CreditLimitOK(string card_id, double dBuy, ref bool bPutOnHold, ref string msg)
{
	if(GetSiteSettings("enable_credit_limit_check", "0") == "0")
		return true;

	if(card_id == "" || card_id == "0")
		return true;

	DataRow dr = GetCardData(card_id);
	if(dr == null)
		return true;

	string name = dr["name"].ToString();

	double limit = MyDoubleParse(dr["credit_limit"].ToString());
	double balance = MyDoubleParse(dr["balance"].ToString());
	double ava = limit - balance;
	double over = dBuy - ava;

	bPutOnHold = false;
	if(limit == 0) //0 means no limit
		return true;

	if(dBuy > ava)
	{
		if(m_sSite == "admin" && dBuy > (limit * 1.15 - balance))
		{
			msg += "<br><br><center><h3><font color=red>Warning, this dealer's credit limit is reached : </h3>";
			msg += "<table>";
			msg += "<tr><td colspan=2>";
			msg += "<table>";
			msg += "<tr><td><b>Account Name</b></td><td align=right>" + dr["trading_name"].ToString() + "</td></tr>";
			msg += "<tr><td><b>Credit Limit</b></td><td align=right>" + limit.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Total Outstanding</b></td><td align=right>" + balance.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Credit Available</b></td><td align=right>" + ava.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Order Total</b></td><td align=right>" + dBuy.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Credit Exceeded</b></td><td align=right>" + over.ToString("c") + "</td></tr>";
			msg += "<tr><td colspan=2><h4><br>Please put this order on hold</h4></td></tr>";
			msg += "<tr><td colspan=2><br><input type=button onclick=history.go(-1) value=' << Back ' " + Session["button_style"] + "></td></tr>";
			msg += "<tr><td colspan=2><br><input type=button onclick=window.location=('eorder.aspx?forceship=1&id=" + Request.QueryString["id"] + "') value=' Continue Shipping ' " + Session["button_style"] + "></td></tr>";
			msg += "</table></td></tr>";
			msg += "</table>";
		}
		else
		{
			msg += "<br><br><center><h3><font color=red>Dear " + name + ", your credit limit is reached : </h3>";
			msg += "<table>";
			msg += "<tr><td colspan=2>";
			msg += "<table>";
//			msg += "<tr><td><b>Account Name</b></td><td align=right>" + dr["trading_name"].ToString() + "</td></tr>";
			msg += "<tr><td><b>Credit Limit</b></td><td align=right>" + limit.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Total Outstanding</b></td><td align=right>" + balance.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Credit Available</b></td><td align=right>" + ava.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Order Total</b></td><td align=right>" + dBuy.ToString("c") + "</td></tr>";
			msg += "<tr><td><b>Credit Exceeded</b></td><td align=right>" + over.ToString("c") + "</td></tr>";
			msg += "</table></td></tr>";

			if(dBuy <= (limit * 1.15 - balance))
			{
				msg += "<tr><td colspan=2><font color=green><b><br>We will release this order as a special support.</b></font></td></tr>";
			}
			else
			{
				msg += "<tr><td colspan=2><font color=red><b><br>We have to put this order on hold.</b></font></td></tr>";
				bPutOnHold = true;
			}

			string footer = ReadSitePage("credit_limit_reminder_footer");
			footer = footer.Replace("\r\n", "<br>\r\n");
			msg += "<tr><td colspan=2><br>" + footer + "</td></tr>";

			msg += "</table>";

			msg += "<br><input type=button " + Session["button_style"] + " onclick=window.location=('status.aspx?r=" + DateTime.Now.ToOADate() + "') value='View Order Status'>";
		}

		return false;
	}
	return true;
}

bool CreditLimitOK(string card_id, double dBuy)
{
	double dCredit = 0;
	double dBalance = 0;
	return CreditLimitOK(card_id, dBuy, ref dCredit, ref dBalance, true);
}

bool CreditLimitOK(string card_id, double dBuy, ref double dCredit, ref double dBalance, bool bWarn)
{
	if(!MyBooleanParse(GetSiteSettings("enable_credit_limit_check", "0")))
		return true;

	if(card_id == "" || card_id == "0")
		return true;

	DataRow dr = GetCardData(card_id);
	if(dr == null)
		return true;

	double limit = MyDoubleParse(dr["credit_limit"].ToString());
	double balance = MyDoubleParse(dr["balance"].ToString());
	double ava = limit - balance;
	dCredit = limit;
	dBalance = balance;

	if(limit == 0) //0 means no limit
		return true;

	if(dBuy > ava)
	{
		if(bWarn)
		{
			Response.Write("<br><br><center><h3><font color=red>Sorry, Credit Limit Reachead.</h3>");
			Response.Write("<table>");
			Response.Write("<tr><td><b>Account Name</b></td><td align=right>" + dr["trading_name"].ToString() + "</td></tr>");
			Response.Write("<tr><td><b>Credit Limit</b></td><td align=right>" + limit.ToString("c") + "</td></tr>");
			Response.Write("<tr><td><b>Total Outstanding</b></td><td align=right>" + balance.ToString("c") + "</td></tr>");
			Response.Write("<tr><td><b>Credit Available</b></td><td align=right>" + ava.ToString("c") + "</td></tr>");
			Response.Write("<tr><td><b>Order Total</b></td><td align=right>" + dBuy.ToString("c") + "</td></tr>");

			string footer = ReadSitePage("credit_limit_reminder_footer");
			footer = footer.Replace("\r\n", "<br>\r\n");
			Response.Write("<tr><td colspan=2><br>" + footer + "</td></tr>");

			Response.Write("</table>");

			Response.Write("<br><input type=button " + Session["button_style"] + " onclick=window.location=('cart.aspx?r=" + DateTime.Now.ToOADate() + "') value='View Cart'>");
		}
		return false;
	}
	return true;
}

bool CreditTermsOK(string card_id)
{
	string term_msg = "";
	int nOverdues = 0;
	int nOverdueDays = 0;
	double dOverdueAmount = 0;
	return CreditTermsOK(card_id, ref nOverdues, ref dOverdueAmount, ref nOverdueDays, ref term_msg);
}

bool CreditTermsOK(string card_id, ref string term_msg)
{
	int nOverdues = 0;
	int nOverdueDays = 0;
	double dOverdueAmount = 0;
	return CreditTermsOK(card_id, ref nOverdues, ref dOverdueAmount, ref nOverdueDays, ref term_msg);
}

bool CreditTermsOK(string card_id, ref int nOverdues, ref double dOverdueAmount, ref int nOverdueDays, ref string msg)
{
	if(!MyBooleanParse(GetSiteSettings("enable_credit_term_check", "0")))
		return true;

	if(card_id == "" || card_id == "0")
		return true;
	
	DataRow dr = GetCardData(card_id);
	if(dr == null)
		return true;

	int dtoday = DateTime.Now.Day;
	DateTime ddead = DateTime.Now.AddDays(20 - dtoday); //set to 20th this month
	if(dtoday <= 20)
		ddead = ddead.AddMonths(-1); //set to last month
	
	bool bBadNews = false;

//	string sterm = GetEnumValue("credit_terms", dr["credit_term"].ToString());
	int nterm = MyIntParse(dr["credit_term"].ToString());
	int ndays = 0;
	if(nterm == 4)
		ndays = 7;
	else if(nterm == 5)
		ndays = 14;
	else if(nterm == 6)
		ndays = 30;
	else if(nterm == 7) //20th of the month
		ndays = Math.Abs((DateTime.Now - ddead).Days);

	//get unpaid invoices
	DataSet dstermcheck = new DataSet();
	string sc = " SELECT invoice_number, commit_date, total, amount_paid ";
	sc += ", DATEDIFF(day, commit_date, GETDATE()) - " + ndays + " AS overdue_days ";
	sc += ", total - amount_paid AS overdue_amount ";
	sc += " FROM invoice ";
	sc += " WHERE card_id = " + card_id;
	sc += " AND paid = 0 AND total > amount_paid ";
	sc += " AND DATEDIFF(day, commit_date, GETDATE()) > " + ndays;
	sc += " ORDER BY commit_date ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nOverdues = myAdapter.Fill(dstermcheck);
		if(nOverdues <= 0)
			return true;
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return true;
	}

	//bad news, we have outstandings
	dr = dstermcheck.Tables[0].Rows[0];
	nOverdueDays = MyIntParse(dr["overdue_days"].ToString());

	for(int i=0; i<nOverdues; i++)
	{
		dr = dstermcheck.Tables[0].Rows[i];
		double dAmount = MyDoubleParse(dr["overdue_amount"].ToString());
		dOverdueAmount += dAmount;
	}

	if(m_sSite == "admin")
	{
		msg = ReadSitePage("credit_term_check_no_good_msg_admin");
		msg = msg.Replace("@@overdues", nOverdues.ToString());
		msg = msg.Replace("@@overdue_amount", dOverdueAmount.ToString("c"));
		msg = msg.Replace("@@overdue_days", nOverdueDays.ToString());
	}
	else
	{
		if(nOverdueDays > 3)
			msg = ReadSitePage("credit_term_check_on_hold_msg");
		else
			msg = ReadSitePage("credit_term_check_no_good_msg");
		msg = msg.Replace("@@overdues", nOverdues.ToString());
		msg = msg.Replace("@@overdue_amount", dOverdueAmount.ToString("c"));
		msg = msg.Replace("@@overdue_days", nOverdueDays.ToString());
	}
	return false;
}

double GetCreditBalance(string card_id)
{	
	double dCreditBalance = 0;
	//get unpaid invoices
	DataSet dstermcheck = new DataSet();

	if(dstermcheck.Tables["creditsBL"] != null)
		dstermcheck.Tables["creditsBL"].Clear();
	string sc = " SELECT ISNULL(ROUND(SUM(amount - amount_applied),2),0) AS balance ";
	sc += " FROM credit ";
	sc += " WHERE card_id = " + card_id;
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dstermcheck, "creditsBL");		
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return 0;
	}

	return dCreditBalance = MyDoubleParse(dstermcheck.Tables["creditsBL"].Rows[0]["balance"].ToString());
	
}
</script>

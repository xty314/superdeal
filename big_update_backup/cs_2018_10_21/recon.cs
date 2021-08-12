<script runat=server>

string m_id = ""; //for restore
string m_type = "0";
string m_tableTitle = "Account Reconciliation";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
string m_ssid = "";

DataRow[] m_dra = null;

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = -1;
string m_account = "1111";
string m_accountID = "1"; //account.id, respect to m_account
string m_accName = "";
double m_dOpeningBalance = 0;
double m_dExpectingBalance = 0;

bool m_bMoving = false; //moving back or forth to search payment records

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

int m_nRows = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	//session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID
		PrepareNewRecon();
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("recon.aspx" + par);
		return;
	}

	if(Request.QueryString["t"] == "new")
	{
		Session["recon_period" + m_ssid] = null;
		Session["recon_date_from" + m_ssid] = null;
		Session["recon_date_to" + m_ssid] = null;
		PrepareNewRecon();
	}
	else if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		if(!DoRestoreRecon())
			return;
	}

	if(IsPostBack)
	{
	}

	if(Request.Form["amount"] != null) //edit amount
	{
		double dAmount = MyMoneyParse(Request.Form["amount"]);
		string id = Request.Form["mark_id"];
		if(MyBooleanParse(Request.Form["is_minus"]))
			dAmount = 0 - dAmount;

		Session["recon_amount_" + id + m_ssid] = dAmount;
	}

	if(Request.Form["cmd"] == "Reset")
	{
		PrepareNewRecon();
	}

	if(Request.Form["opening_balance"] != null)
	{
		m_dOpeningBalance = MyMoneyParse(Request.Form["opening_balance"]);
		Session["recon_opening_balance" + m_ssid] = m_dOpeningBalance;
	}
	else if(Session["recon_opening_balance" + m_ssid] != null)
		m_dOpeningBalance = (double)Session["recon_opening_balance" + m_ssid];

	if(Request.Form["closing_balance"] != null)
		Session["recon_closing_balance" + m_ssid] = Request.Form["closing_balance"];

	if(Request.Form["expecting_balance"] != null)
	{	
		m_dExpectingBalance = MyMoneyParse(Request.Form["expecting_balance"]);
		Session["recon_expecting_balance" + m_ssid] = m_dExpectingBalance;
	}
	else if(Session["recon_expecting_balance" + m_ssid] != null)
		m_dExpectingBalance = (double)Session["recon_expecting_balance" + m_ssid];

	if(Request.Form["acc"] != null && Request.Form["acc"] != "")
		m_account = Request.Form["acc"];
	else if(Request.QueryString["acc"] != null && Request.QueryString["acc"] != "")
		m_account = Request.QueryString["acc"];
	m_accName = GetAccountName(m_account);

	if(Request.Form["period"] != null)
	{
		m_nPeriod = MyIntParse(Request.Form["period"]);
		Session["recon_period" + m_ssid] = m_nPeriod;
	}
	else if(Session["recon_period" + m_ssid] != null)
	{
		m_nPeriod = (int)Session["recon_period" + m_ssid];
	}

	if(Request.Form["day_from"] != null)
	{
		string day = Request.Form["day_from"];
		string monthYear = Request.Form["month_from"];
		ValidateMonthDay(monthYear, ref day);
		m_sdFrom = day + "-" + monthYear;

		day = Request.Form["day_to"];
		monthYear = Request.Form["month_to"];
		ValidateMonthDay(monthYear, ref day);
		m_sdTo = day + "-" + monthYear;

		Session["recon_date_from" + m_ssid] = m_sdFrom;
		Session["recon_date_to" + m_ssid] = m_sdTo;
	}
	else if(Session["recon_date_from" + m_ssid] != null)
	{
		if(Session["recon_date_from" + m_ssid] != null)
			m_sdFrom = Session["recon_date_from" + m_ssid].ToString();
		if(Session["recon_date_to" + m_ssid] != null)
			m_sdTo = Session["recon_date_to" + m_ssid].ToString();
	}
	if(Request.QueryString["t"] == "move")
	{
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		if(Request.QueryString["dir"] != "stop" && Session["recon_display_period" + m_ssid] == null)
		{
			//save current working sheet date information
			Session["recon_display_period" + m_ssid] = 0;
			Session["recon_working_period" + m_ssid] = m_nPeriod;
			Session["recon_working_date_from" + m_ssid] = m_sdFrom;
			Session["recon_working_date_to" + m_ssid] = m_sdTo;
//DEBUG("saved", 1);
		}

		if(Request.QueryString["dir"] == "back")
		{
			DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
			m_sdFrom = dFrom.AddMonths(-1).ToString("dd-MM-yyyy");
			m_sdTo = dFrom.AddDays(1).ToString("dd-MM-yyyy");
			Session["recon_period" + m_ssid] = 4; 
			Session["recon_date_from" + m_ssid] = m_sdFrom;
			Session["recon_date_to" + m_ssid] = m_sdTo;
		}
		else if(Request.QueryString["dir"] == "next")
		{
			DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
			m_sdTo = dTo.AddMonths(1).ToString("dd-MM-yyyy");
			m_sdFrom = dTo.AddDays(-1).ToString("dd-MM-yyyy");
			Session["recon_period" + m_ssid] = 4;
			Session["recon_date_from" + m_ssid] = m_sdFrom;
			Session["recon_date_to" + m_ssid] = m_sdTo;
		}
		else if(Request.QueryString["dir"] == "stop")
		{
//DEBUG("restored", 1);
			Session["recon_display_period" + m_ssid] = null;
			if(Session["recon_working_period" + m_ssid] != null)
			{
				Session["recon_period" + m_ssid] = (int)Session["recon_working_period" + m_ssid];
				Session["recon_date_from" + m_ssid] = Session["recon_working_date_from" + m_ssid].ToString();
				Session["recon_date_to" + m_ssid] = Session["recon_working_date_to" + m_ssid].ToString();
				m_nPeriod = (int)Session["recon_period" + m_ssid];
				m_sdFrom = Session["recon_date_from" + m_ssid].ToString();
				m_sdTo = Session["recon_date_to" + m_ssid].ToString();
			}
		}
	}
	if(Session["recon_display_period" + m_ssid] != null)
		m_bMoving = true;

	if(Request.QueryString["t"] == "new")
	{
		Session["recon_period" + m_ssid] = null;
	}
	else if(Request.Form["period"] == null) //
	{
		if(Session["recon_period" + m_ssid] != null)
			m_nPeriod = (int)Session["recon_period" + m_ssid];
		if(Session["recon_date_from" + m_ssid] != null)
			m_sdFrom = Session["recon_date_from" + m_ssid].ToString();
		if(Session["recon_date_to" + m_ssid] != null)
			m_sdTo = Session["recon_date_to" + m_ssid].ToString();
	}

	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Continue";
		break;
	case 1:
		m_datePeriod = "Current Month";
		break;
	case 2:
		m_datePeriod = "Last Month";
		break;
	case 3:
		m_datePeriod = "Last Three Month";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	default:
		break;
	}

	if(Request.Form["cmd"] == "Done")
	{
		if(DoSaveRecon())
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Reconciliation History Saved, please wait a second...");
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=recon.aspx?t=new&ssid=" + m_ssid + "\">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Delete")
	{
		if(DoDeleteRecon())
		{
			PrepareNewRecon();
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Reconciliation deleted</h3><br>");
			Response.Write("<input type=button value='New Reconciliation' ");
			Response.Write(" onclick=window.location=('recon.aspx?t=new&ssid=" + m_ssid + "') class=b>");
			PrintAdminFooter();
		}
		return;
	}

	if(Request.QueryString["t"] == "new" || Request.QueryString["ss"] != null)
	{
		PrintMainPage();
		if(Request.QueryString["ss"] == "1")
			PrintReconList();
		PrintAdminFooter();
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	PrintStatement();

	PrintAdminFooter();
}

void PrepareNewRecon()
{
	Session["recon_opening_balance" + m_ssid] = null;
	Session["recon_expecting_balance" + m_ssid] = null;
}

void ValidateMonthDay(string monthYear, ref string day)
{
	string month = "";
	string year = "";
	for(int i=0; i<monthYear.Length; i++)
	{
		if(monthYear[i] == '-')
		{
			month = year;
			year = "";
			continue; //skip dash
		}
		year += monthYear[i];
	}

	int dMax = DateTime.DaysInMonth(MyIntParse(year), MyIntParse(month));
	int d = MyIntParse(day);
	if(d > dMax)
		d = dMax;
	day = d.ToString();
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<form name=f action=recon.aspx?ssid=" + m_ssid + " method=post>");

	Response.Write("<br><center><h3>Reconciliation</h3>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Select Account </b></td></tr>");
	Response.Write("<tr><td colspan=2>");
	PrintAccountList();
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>Continue</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>Last Month To Now</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>Last Three Months To Now</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=4>Select Date Range</td></tr>");

	int i = 1;

	//from date
	Response.Write("<tr><td colspan=2><b> &nbsp; From Date </b>");
	Response.Write("<select name=day_from onchange=\"document.f.period.value=3;\">");
	for(; i<=31; i++)
	{
		Response.Write("<option value=" + i);
		Response.Write(">" + i + "</option>");
	}
	Response.Write("</select>");

	Response.Write("&nbsp&nbsp; <select name=month_from>");
	DateTime dstep = DateTime.Parse("01/01/2003");
	DateTime dend = DateTime.Now;
	while((dstep - dend).Days <= 0)
	{
		string value = dstep.ToString("MM-yyyy");
		string name = dstep.ToString("MMM yyyy");
		Response.Write("<option value='" + value + "'>" + name + "</option>");
		dstep = dstep.AddMonths(1);
	}
	Response.Write("</select>");

	//to date
	Response.Write("&nbsp; <b>To Date</b> &nbsp; ");
	Response.Write("<select name=day_to>");
	for(i=1; i<=31; i++)
	{
		Response.Write("<option value=" + i);
		if(i == DateTime.Now.Day)
			Response.Write(" selected");
		Response.Write(">" + i + "</option>");
	}
	Response.Write("</select>");

	Response.Write("&nbsp&nbsp; <select name=month_to>");
	dstep = DateTime.Parse("01/01/2003");
	while((dstep - dend).Days <= 0)
	{
		string value = dstep.ToString("MM-yyyy");
		string name = dstep.ToString("MMM yyyy");
		Response.Write("<option value='" + value + "' ");
		if(dstep.Month == dend.Month && dstep.Year == dend.Year)
			Response.Write(" selected");
		Response.Write(">" + name + "</option>");
		dstep = dstep.AddMonths(1);
	}
	Response.Write("</select>");

	Response.Write("</td></tr>");
	
	Response.Write("<tr><td>");
	if(Request.QueryString["ss"] == "1")
		Response.Write("<input type=button value='Hide History' onclick=window.location=('recon.aspx?t=new&ss=0&ssid=" + m_ssid + "') class=b>");
	else
		Response.Write("<input type=button value='Show History' onclick=window.location=('recon.aspx?t=new&ss=1&ssid=" + m_ssid + "') class=b>");
	Response.Write("</td>");
	Response.Write("<td align=right><input type=submit name=cmd value='Reconcilate' class=b></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
}

bool PrintReconList()
{
	Response.Write("<br><center><h3>Reconciliations History</h3>");
	int rows = 0;
	string sc = " SELECT c.*, STR(a.class1) + '-' + CONVERT(varchar, a.class2*100 +a.class3*10 + a.class4) + ' ' ";
	sc += " + a.name4 AS account_name ";
	sc += " FROM accrecon c JOIN account a ON a.id = c.account_id ";
	sc += " WHERE c.type='balance' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "recon");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<h4><font color=red>No saved reconciliation</font></h4>");
		return true;
	}

	Response.Write("<table width=80% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Account</th>");
	Response.Write("<th>From_Date</th>");
	Response.Write("<th>To_Date</th>");
	Response.Write("<th>Opening_Balance</th>");
	Response.Write("<th>Closing_Balance</th>");
	Response.Write("<th>Expecting_Balance</th>");
	Response.Write("<th>Out_Of_Balance</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	string bgc = GetSiteSettings("table_row_bgcolor", "#EEEEEE");
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["recon"].Rows[i];
		string id = dr["id"].ToString();
		string account = dr["account_name"].ToString();
		string open_date = DateTime.Parse(dr["open_date"].ToString()).ToString("dd-MMM-yyyy");
		string close_date = DateTime.Parse(dr["close_date"].ToString()).ToString("dd-MMM-yyyy");
		double dob = MyDoubleParse(dr["open_balance"].ToString());
		double dcb = MyDoubleParse(dr["close_balance"].ToString());
		double deb = MyDoubleParse(dr["expect_balance"].ToString());
		double dout = deb - dcb;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + bgc);
		Response.Write(">");

		Response.Write("<td>" + account + "</td>");
		Response.Write("<td>" + open_date + "</td>");
		Response.Write("<td>" + close_date + "</td>");
		Response.Write("<td>" + dob.ToString("c") + "</td>");
		Response.Write("<td>" + dcb.ToString("c") + "</td>");
		Response.Write("<td>" + deb.ToString("c") + "</td>");
		Response.Write("<td>" + dout.ToString("c") + "</td>");
		Response.Write("<td align=right>");
		Response.Write("<input type=button onclick=window.location=('recon.aspx?id=" + id + "&ssid=" + m_ssid + "') value=View class=b>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;	
}

string GetLastReconDate()
{
	string close_balance = "0";
	string sdFrom = "01-01-2003";
	string sc = " SELECT TOP 1 * FROM accrecon WHERE type='balance' ORDER BY close_date DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "lastrecon") == 1)
		{
			DataRow dr = ds.Tables["lastrecon"].Rows[0];
			sdFrom = dr["close_date"].ToString();
			close_balance = dr["close_balance"].ToString();
			m_dOpeningBalance = MyDoubleParse(close_balance); //use last close balance as new opening balance
			Session["recon_opening_balance" + m_ssid] = m_dOpeningBalance;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return sdFrom;
	}
//DEBUG("from=", sdFrom);	
//	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
//	DateTime dFrom = DateTime.Parse(sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
	DateTime dFrom = DateTime.Parse(sdFrom);
	dFrom = dFrom.AddDays(1); //include the last day

	sdFrom = dFrom.ToString("dd-MM-yyyy");
	Session["recon_date_from" + m_ssid] = sdFrom;
	return sdFrom;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Monthly Profit Chart
bool PrintStatement()
{
	string sqlLoss = "";
	string sqlExpense = "";
	string sqlTrans = "";
	string sqlPurchase = "";
	string sqlTax = "";
	string sqlOld = "";
	int i = 0;
	switch(m_nPeriod)
	{
	case 0:
		if(Request.Form["period"] != null || Session["recon_date_from" + m_ssid] == null)
			m_sdFrom = GetLastReconDate();
		else
			m_sdFrom = Session["recon_date_from" + m_ssid].ToString();
		sqlTrans = " p.deposit_date >= '" + m_sdFrom + "' ";
		sqlPurchase = " d.trans_date >= '" + m_sdFrom + "' ";
		sqlExpense = " e.payment_date >= '" + m_sdFrom + "' ";
		sqlTax = " t.recorded_date >= '" + m_sdFrom + "' ";
		sqlOld = " (r.tran_date < '" + m_sdFrom + "' OR (r.ticked=1 AND r.tick_date>='" + m_sdFrom + "')) ";
		break;
	case 1:
		sqlTrans = " DATEDIFF(month, p.deposit_date, GETDATE()) = 0 ";
		sqlPurchase = " DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) = 0 ";
		sqlTax = " DATEDIFF(month, t.recorded_date, GETDATE()) = 0 ";
		m_sdFrom = "01" + DateTime.Now.ToString("-MM-yyyy");
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		sqlOld = " DATEDIFF(month, r.tran_date, GETDATE()) > 0 ";
		break;
	case 2:
		sqlTrans = " DATEDIFF(month, p.deposit_date, GETDATE()) <= 1 ";
		sqlPurchase = " DATEDIFF(month, d.trans_date, GETDATE()) <= 1 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) <= 1 ";
		sqlTax = " DATEDIFF(month, t.recorded_date, GETDATE()) <= 1 ";
		m_sdFrom = DateTime.Parse("01" + DateTime.Now.ToString("-MM-yyyy")).AddMonths(-1).ToString("dd-MM-yyyy");
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		sqlOld = " DATEDIFF(month, r.tran_date, GETDATE()) > 1 ";
		break;
	case 3: //last 3 months
		sqlTrans = " DATEDIFF(month, p.deposit_date, GETDATE()) <= 3 ";
		sqlPurchase = " DATEDIFF(month, d.trans_date, GETDATE()) <= 3 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
		sqlTax = " DATEDIFF(month, t.recorded_date, GETDATE()) <= 3 ";
		m_sdFrom = DateTime.Parse("01" + DateTime.Now.ToString("-MM-yyyy")).AddMonths(-3).ToString("dd-MM-yyyy");
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		sqlOld = " DATEDIFF(month, r.tran_date, GETDATE()) > 3 ";
		break;
	case 4:
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		dTo = dTo.AddDays(1); //include the last day
		string from = dFrom.ToString("dd-MM-yyyy");
		string to = dTo.ToString("dd-MM-yyyy");
		sqlTrans = " p.deposit_date >= '" + from + "' AND p.deposit_date <= '" + to + "' ";
		sqlPurchase = " d.trans_date >= '" + from + "' AND d.trans_date <= '" + to + "' ";
		sqlExpense = " e.payment_date >= '" + from + "' AND e.payment_date <= '" + to + "' ";
		sqlTax = " t.recorded_date >= '" + from + "' AND t.recorded_date <= '" + to + "' ";
		sqlOld = " (r.tran_date < '" + from + "' OR (r.ticked=1 AND r.tick_date >= '" + m_sdFrom + "')) ";
		break;
	default:
		break;
	}

	if(sqlOld == "") //probably a 
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=recon.aspx?t=new\">");
		return false;
	}

	ds.Clear();
	int rows = 0;

	string sc = "";
	//get all old unticked
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT '' AS tid, r.rid AS recon_id, r.tran_date AS recon_date, r.tran_amount, r.tran_ref AS ref, r.type AS recon_type ";
	sc += " FROM accrecon r ";
	sc += " WHERE r.rid IS NOT NULL AND (r.ticked=0 OR (r.ticked=1 AND r.tick_date >= '" + m_sdFrom + "')) ";
	sc += " AND " + sqlOld;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	string idNotInPurchase = "";
	string idNotInDeposit = "";
	string idNotInExpense = "";
	string idNotInOthers = "";

	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["trans"].Rows[i];
		string rid = dr["recon_id"].ToString();
		string type = dr["recon_type"].ToString();
		if(type == "purchase")
		{
			if(idNotInPurchase != "")
				idNotInPurchase += ",";
			idNotInPurchase += rid;
		}
		else if(type == "deposit")
		{
			if(idNotInDeposit != "")
				idNotInDeposit += ",";
			idNotInDeposit += rid;
		}
		else if(type == "expense")
		{
			if(idNotInExpense != "")
				idNotInExpense += ",";
			idNotInExpense += rid;
		}
		else
		{
			if(idNotInOthers != "")
				idNotInOthers += ",";
			idNotInOthers += rid;
		}
	}

	//get income
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id AS tid, pi.id AS recon_id, p.deposit_date AS recon_date, SUM(t.amount) AS tran_amount ";
	sc += ", '' AS ref, 'deposit' AS recon_type ";
	sc += " FROM trans t JOIN tran_deposit_id pi on pi.tran_id = t.id ";
	sc += " JOIN tran_deposit p on p.id = pi.id ";
	sc += " WHERE (t.dest = " + m_account + " OR t.source = " + m_account + ") ";
//	sc += " WHERE ((t.banked = 1 AND dest = " + m_accoutn + ") OR t.source = " + m_account + ") ";
	if(sqlTrans != "")
		sc += " AND " + sqlTrans;
	if(idNotInDeposit != "")
		sc += " AND pi.id NOT IN(" + idNotInDeposit + ") ";
	sc += " GROUP BY t.id, pi.id, p.deposit_date ";
//	sc += " ORDER BY deposit_id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//get purchase
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id AS tid, t.id AS recon_id, d.trans_date AS recon_date, t.amount AS tran_amount ";
	sc += ", d.payment_ref AS ref, 'purchase' AS recon_type ";
	sc += " FROM trans t JOIN tran_detail d ON d.id=t.id ";
	sc += " JOIN tran_invoice i ON i.tran_id = t.id ";
	sc += " WHERE i.purchase=1 AND t.source = " + m_account;
	if(sqlPurchase != "")
		sc += " AND " + sqlPurchase;
	if(idNotInPurchase != "")
		sc += " AND t.id NOT IN (" + idNotInPurchase + ") ";
	sc += " GROUP BY t.id, d.trans_date, d.payment_ref, t.amount ";
	sc += " ORDER BY t.id ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	//expense
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT '' AS tid, e.id AS recon_id, e.payment_date AS recon_date ";
	sc += ", tran_amount = CASE e.total WHEN 0 THEN e.tax ELSE e.total END ";//, (a.class1 * 1000 + a.class2 * 100 + a.class3 * 10 + a.class4) AS source ";
	sc += ", '' AS ref, 'expense' AS recon_type ";
	sc += " FROM expense e ";//JOIN account a ON a.id = e.from_account ";
	sc += " WHERE e.from_account=" + m_accountID;
	if(sqlTrans != "")
		sc += " AND " + sqlExpense;
	if(idNotInExpense != "")
		sc += " AND e.id NOT IN(" + idNotInExpense + ") ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	string tran_id_exclusive = "(-1";
	for(i=0; i<ds.Tables["trans"].Rows.Count; i++)
	{
		string tid = ds.Tables["trans"].Rows[i]["tid"].ToString();
		if(tid != "")
			tran_id_exclusive += "," + tid;
	}
	tran_id_exclusive += ")";

	//get other expense (transfer between acc etc.)
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id AS recon_id, d.trans_date AS recon_date, t.amount AS tran_amount ";
	sc += ", d.payment_ref AS ref, 'others' AS recon_type ";
	sc += " FROM trans t JOIN tran_detail d ON d.id=t.id ";
	sc += " LEFT OUTER JOIN tran_invoice i ON i.tran_id = t.id ";
	sc += " WHERE t.source = " + m_account;// + " AND t.dest IS NOT NULL ";
//	sc += " AND (i.purchase <> 1 OR i.purchase is null) ";
	sc += " AND t.id NOT IN " + tran_id_exclusive;
	if(sqlPurchase != "")
		sc += " AND " + sqlPurchase;
	if(idNotInOthers != "")
		sc += " AND t.id NOT IN(" + idNotInOthers + ") ";
	sc += " GROUP BY t.id, d.trans_date, d.payment_ref, t.amount ";
	sc += " ORDER BY t.id ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//get others debit(transfer between acc etc.)
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id AS recon_id, d.trans_date AS recon_date, (0 - t.amount) AS tran_amount ";
	sc += ", d.payment_ref AS ref, 'others' AS recon_type ";
	sc += " FROM trans t JOIN tran_detail d ON d.id=t.id ";
	sc += " LEFT OUTER JOIN tran_invoice i ON i.tran_id = t.id ";
	sc += " LEFT OUTER JOIN tran_deposit p on p.id = t.id ";
	sc += " WHERE t.dest = " + m_account;// + " AND t.dest IS NOT NULL ";
//	sc += " AND (i.purchase <> 1 OR i.purchase is null) ";
//	sc += " AND p.id is null ";
	sc += " AND t.id NOT IN " + tran_id_exclusive;
	if(sqlPurchase != "")
		sc += " AND " + sqlPurchase;
	if(idNotInOthers != "")
		sc += " AND t.id NOT IN(" + idNotInOthers + ") ";
	sc += " GROUP BY t.id, d.trans_date, d.payment_ref, t.amount ";
	sc += " ORDER BY t.id ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//tax payment
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id AS recon_id, t.recorded_date AS recon_date, (0 - t.total_gst) AS tran_amount ";
	sc += ", t.payment_ref AS ref, 'tax_payment' AS recon_type ";
	sc += " FROM custom_tax t ";
	sc += " WHERE t.from_acc = " + m_accountID;
	sc += " AND " + sqlTax;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	//equity
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id AS recon_id, t.recorded_date AS recon_date ";
	sc += ", (0 - t.total) AS tran_amount, t.payment_ref AS ref, 'owner_draw' AS recon_type ";
	sc += " FROM acc_equity t ";
	sc += " WHERE t.account_type = " + m_accountID;
	sc += " AND " + sqlTax;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "trans");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
		

	Response.Write("<form name=f action=recon.aspx?ssid=" + m_ssid + " method=post>");
	Response.Write("<input type=hidden name=acc value=" + m_account + ">");

	Response.Write("<br><table width=85%  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=3 align=center><font size=+1><b>" + m_tableTitle + "</b></font></td></tr>");
	Response.Write("<tr><td colspan=3 align=center><b>" + m_accName + " - " + m_account + "</b></td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	Response.Write("<tr><td colspan=2 nowrap>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b> &nbsp&nbsp; ");
	Response.Write("<input type=button value='New Expense' ");
	Response.Write(" onclick=window.open('expense.aspx?t=new') class=b>");
	Response.Write("</td>");

	Response.Write("<td align=right>");
	Response.Write("<input type=submit name=cmd value=Refresh class=b>");
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=3>");

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Type</th>");
	Response.Write("<th>Ref.</th>");
	Response.Write("<th>Credit</th>");
	Response.Write("<th>Debit</th>");
	Response.Write("<th>Balance</th>");
	Response.Write("</tr>");

	int cols = 9;

	Response.Write("<tr bgcolor=yellow>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td>" + m_sdFrom + "</td>");
	Response.Write("<td colspan=4 align=right><font color=green><b>Opening Balance</b></font></td>");
	Response.Write("<td align=right>");
//	if(Request.QueryString["co"] != null)
	if(Request.Form["cmd"] == "Edit")
	{
		Response.Write("<input type=text size=15 style=text-align:right;font-size:8pt; name=opening_balance value=" + m_dOpeningBalance + ">");
	}
	else
	{
		Response.Write("<input type=submit name=cmd value=Edit class=b title='Click to change'> " + m_dOpeningBalance.ToString("c") + "</a>");
//		Response.Write("<a href=recon.aspx?acc=" + m_account + "&co=1&ssid=" + m_ssid + " class=o title='Click to change'>" + m_dOpeningBalance.ToString("c") + "</a>");
		Response.Write("<input type=hidden name=opening_balance value=" + m_dOpeningBalance + ">");
	}
	Response.Write("</td>");
	Response.Write("</tr>");

	DataRow[] dra = ds.Tables["trans"].Select("", "recon_date");
	double dBalance = m_dOpeningBalance;

	bool bAlterColor = false;
	int nEditRow = -1;
	if(Request.QueryString["row"] != null)
		nEditRow = MyIntParse(Request.QueryString["row"]);
	int nDateRow = -1;
	if(Request.QueryString["drow"] != null)
		nDateRow = MyIntParse(Request.QueryString["drow"]);

	string tran_type_old = "";
	string deposit_id_old = "";

	m_nRows = dra.Length;

	for(i=0; i<dra.Length; i++)
	{
		bool bMarked = false; //price changed
		DataRow dr = dra[i];
		string id = dr["recon_id"].ToString();
//		string date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy");
		string deposit_date = dr["recon_date"].ToString();
		if(deposit_date != "")
			deposit_date = DateTime.Parse(deposit_date).ToString("dd-MM-yyyy");
		double dAmount = MyDoubleParse(dr["tran_amount"].ToString());
		bool bIsMinus = false; //used for marking price
		if(dAmount < 0)
			bIsMinus = true;
		double dAmountOrg = dAmount;
		dAmountOrg = Math.Abs(dAmountOrg);
		string sref = dr["ref"].ToString();
		string tran_type = dr["recon_type"].ToString();
		bool bPurchase = false;//MyBooleanParse(dr["purchase"].ToString()); 
		if(tran_type == "purchase")
			bPurchase = true;
		string markid = tran_type + id;

		if(tran_type == "others")
		{
			if(IsAssetsPayment(id))
				tran_type = "assets";
			else
				tran_type = "transfer";
		}

		Response.Write("<input type=hidden name=tran_type" + i + " value='" + tran_type + "'>");
		Response.Write("<input type=hidden name=tran_id" + i + " value='" + id + "'>");
		Response.Write("<input type=hidden name=tran_date" + i + " value='" + deposit_date + "'>");
		Response.Write("<input type=hidden name=tran_ref" + i + " value='" + sref + "'>");

		//marked amount check
		if(Session["recon_amount_" + markid + m_ssid] != null)
		{
			if(Request.Form["cmd"] == "Reset")
			{
				Session["recon_amount_" + markid + m_ssid] = null;
			}
			else if(Session["recon_amount_" + markid + m_ssid] != null)
			{
				dAmount = (double)Session["recon_amount_" + markid + m_ssid];
				bMarked = true;
			}
		}
		bool bTicked = false;
		if(Request.Form["tran_type" + i] != null)
		{
			if(Request.Form["tick" + i] == "on")
			{
				Session["recon_ticked_" + markid + m_ssid] = true;
			}
			else// if(Request.Form["tick" + i] == "")
			{
				Session["recon_ticked_" + markid + m_ssid] = null;
			}
		}
		else if(Session["recon_ticked_" + markid + m_ssid] != null)
		{
			bTicked = true;
		}
		if(Session["recon_ticked_" + markid + m_ssid] != null)
		{
			if(Request.Form["cmd"] == "Reset")
				Session["recon_ticked_" + markid + m_ssid] = null;
			else
				bTicked = true;
		}

		if(bPurchase || tran_type.ToLower() == "expense" || tran_type.ToLower() == "transfer" || tran_type.ToLower() == "assets")
			dAmount = 0 - dAmount;

		if(bTicked)
			dBalance += dAmount;

		Response.Write("<tr");
		if(bTicked)
		{
//			if(bAlterColor)
				Response.Write(" bgcolor=#86B685");
//			else
//				Response.Write(" bgcolor=#900090");
		}
		else
		{
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
		}
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td><input type=checkbox name=tick" + i);
		if(bTicked)
			Response.Write(" checked");
		Response.Write(" onclick='CalcBalance();'></td>");
//		Response.Write("<td>" + id + "</td>");
		Response.Write("<td>" + deposit_date + "</td>");

		Response.Write("<td>");

		string vlink = "";
		switch(tran_type)
		{
		case "deposit":
			vlink = "<a href=banking.aspx?id=" + id + " title='Click to view details' class=o target=_blank>";
			break;
		case "purchase":
			vlink = "<a href=payhistory.aspx?t=p&id=" + id + " title='Click to view details' class=o target=_blank>";
			break;
		case "expense":
			vlink = "<a href=expense.aspx?id=" + id + " title='Click to view details' class=o target=_blank>";
			break;
		case "assets":
			vlink = "<a href=asstlist.aspx?t=ph&tran_id=" + id + " title='Click to view details' class=o target=_blank>";
			break;
		default:
			break;
		}

		Response.Write(vlink);
		Response.Write("<font color=");
		if(bPurchase)
			Response.Write("red");
		else if(tran_type.ToLower() == "expense")
			Response.Write("blue");
		else
			Response.Write("black");
		Response.Write(">");
		Response.Write(tran_type);
		Response.Write("</font>");
		if(vlink != "")
			Response.Write("</a>");
		Response.Write("</td>");
//		Response.Write("<td>" + payment + "</td>");
		Response.Write("<td>" + sref + "</td>");
		
		bool bIsIncome = true;
		if(dAmount >= 0)
		{
			Response.Write("<td>&nbsp;</td>");
		}
		else
		{
			dAmount = 0 - dAmount;
			bIsIncome = false;
		}

		Response.Write("<input type=hidden name=income" + i + " value=");
		if(bIsIncome)
			Response.Write("1");
		else
			Response.Write("0");
		Response.Write(">");

		Response.Write("<input type=hidden name=amount" + i + " value=" + dAmount + ">");

		if(i == nEditRow)
		{
			Response.Write("<td align=right><input type=text size=5 style=\"text-align:right;font-size:8pt;\" ");
			Response.Write(" name=amount value=" + dAmount + ">");
			Response.Write("<input type=submit name=cmd value='OK' class=b></td>");
			Response.Write("<input type=hidden name=mark_id value=" + markid + ">");
			Response.Write("<input type=hidden name=is_minus value='" + bIsMinus.ToString() + "'>");
		}
		else
		{
			if(bMarked)
				Response.Write("<td align=right><a href=recon.aspx?acc=" + m_account + "&row=" + i + "&ssid=" + m_ssid + " class=o title='Original Amount : " + dAmountOrg.ToString("c") + ", click to edit'><font color=red>" + dAmount.ToString("c") + "</font></a></td>");
			else
				Response.Write("<td align=right><a href=recon.aspx?acc=" + m_account + "&row=" + i + "&ssid=" + m_ssid + " class=o title='Mark Amount'>" + dAmount.ToString("c") + "</a></td>");
		}
		
		if(!bIsIncome)
			Response.Write("<td>&nbsp;</td>");

		Response.Write("<td align=right><input type=text size=10 name=ongoing_balance" + i + " readonly=true style=\"border:0;text-align:right;\" value=" + dBalance.ToString() + "></td>");
//		Response.Write("<td align=right><a href=" + edituri + " class=o target=_blank>Date</a></td>");
		Response.Write("</tr>");
	}

	if(m_bMoving) //no saving deleting stuff
	{
		Response.Write("</table></td></tr></table></form>");
		return true;
	}

	Response.Write("<tr bgcolor=yellow>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td>" + m_sdTo + "</td>");
	Response.Write("<td colspan=4 align=right><font color=green><b>Closing Balance</b></font></td>");
	Response.Write("<td align=right><input type=text size=10 name=closing_balance readonly=true style=\"border:0;text-align:right;\" value=" + dBalance.ToString("") + "></td>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td>" + m_sdTo + "</td>");
	Response.Write("<td colspan=4 align=right><font color=green><b>Expecting Balance</b></font></td>");
	Response.Write("<td align=right>");
	if(Request.Form["cmd"] == "Edit")
	{
		Response.Write("<input type=text size=15 style=text-align:right;font-size:8pt; name=expecting_balance value=" + m_dExpectingBalance + ">");
	}
	else
	{
		Response.Write("<input type=submit name=cmd value=Edit class=b title='Click to change'> " + m_dExpectingBalance.ToString("c") + "</a>");
		Response.Write("<input type=hidden name=expecting_balance value=" + m_dExpectingBalance + ">");
	}
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td colspan=4 align=right><font color=green><b>Out Of Balance</b></font></td>");
	Response.Write("<td align=right><input type=text name=out_of_balance readonly=true style=\"border:0;text-align:right\" value=" + (m_dExpectingBalance - dBalance).ToString("") + "></td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<input type=submit name=cmd value='Reset' class=b>");
	Response.Write("<input type=button onclick=window.location=('recon.aspx?t=new&ssid=" + m_ssid + "') value='New' class=b>");
	Response.Write("</td>");
	Response.Write("<td colspan=2 align=right nowrap>");
	Response.Write("<input type=checkbox name=confirm_delete>Confirm Delete ");
	Response.Write("<input type=submit name=cmd value=Delete class=b>");
	Response.Write("<input type=submit name=cmd value=Done class=b>");
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</form>");

	PrintJavaCode();

	return true;
}

string GetAccountName(string number)
{
	string class1 = number.Substring(0, 1);
	string class2 = number.Substring(1, 1);
	string class3 = number.Substring(2, 1);
	string class4 = number.Substring(3, number.Length-3);

	DataSet dsname = new DataSet();
	string sc = " SELECT id, name1, name2, name3, name4 ";
	sc += " FROM account ";
	sc += " WHERE class1 = " + class1 + " AND class2 = " + class2 + " AND class3 = " + class3 + " AND class4 = " + class4;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsname, "acc") == 1)
		{
			DataRow dr = dsname.Tables["acc"].Rows[0];
			m_accountID = dr["id"].ToString();
			return dr["name1"].ToString() + " " + dr["name4"].ToString();
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}

bool DoSaveRecon()
{
	string ob = "0";
	if(Session["recon_opening_balance" + m_ssid] != null)
		ob = Session["recon_opening_balance" + m_ssid].ToString();
	string cb = "0";
	if(Session["recon_closing_balance" + m_ssid] != null)
		cb = Session["recon_closing_balance" + m_ssid].ToString();
	string eb = "0";
	if(Session["recon_expecting_balance" + m_ssid] != null)
		eb = Session["recon_expecting_balance" + m_ssid].ToString();

	double dob = MyDoubleParse(ob);
	double dcb = MyDoubleParse(cb);
	double deb = MyDoubleParse(eb);
	double dOut = deb - dcb;

	if(Session["recon_date_from" + m_ssid] != null)
		m_sdFrom = Session["recon_date_from" + m_ssid].ToString();
	if(Session["recon_date_to" + m_ssid] != null)
		m_sdTo = Session["recon_date_to" + m_ssid].ToString();

	string sc = " SET DATEFORMAT dmy ";

	//save balance
	sc += " IF NOT EXISTS(SELECT * FROM accrecon WHERE type='balance' AND open_date='" + m_sdFrom + "' AND close_date='" + m_sdTo + "') ";
	sc += " BEGIN ";
		sc += " INSERT INTO accrecon (type, account_id, open_date, open_balance, close_date, close_balance, expect_balance, out_of_balance) ";
		sc += " VALUES('balance', " + m_accountID + ", '" + m_sdFrom + "', " + dob;
		sc += ", '" + m_sdTo + "', " + dcb + ", " + deb + ", " + dOut + ") ";
	sc += " END ";
	sc += " ELSE ";
	sc += " BEGIN ";
		sc += " UPDATE accrecon SET open_balance = " + dob + ", close_balance = " + dcb + ", expect_balance = " + deb;
		sc += ", out_of_balance = " + dOut;
		sc += " WHERE type='balance' AND open_date='" + m_sdFrom + "' AND close_date='" + m_sdTo + "' ";
	sc += " END ";
	
	int i = -1;
	while(i++ < 10000)
	{
		if(Request.Form["tran_type" + i] == null)
			break;
		string type = Request.Form["tran_type" + i];
		string rid = Request.Form["tran_id" + i];
		string markid = type + rid;
		string ticked = "0";
		if(Request.Form["tick" + i] == "on")
			ticked = "1";
		string tran_amount = Request.Form["amount" + i];
		string tran_date = Request.Form["tran_date" + i];
		string tran_ref = Request.Form["tran_ref" + i];
//		if(Request.Form["tick" + i] == "on")
		{
//			ticked = "1";
			sc += " IF NOT EXISTS(SELECT * FROM accrecon WHERE type='" + type + "' AND rid=" + rid + ") ";
			sc += " BEGIN ";
				sc += " INSERT INTO accrecon (account_id, type, rid, ticked, tick_date, tran_date, tran_amount, tran_ref) ";
				sc += " VALUES(" + m_accountID + ", '" + type + "', " + rid + ", " + ticked + ", '" + m_sdFrom + "' ";
				sc += ", '" + EncodeQuote(tran_date) + "', " + tran_amount + ", '" + EncodeQuote(tran_ref) + "') ";
			sc += " END ";
			sc += " ELSE ";
			sc += " BEGIN ";
				sc += " UPDATE accrecon SET ticked=" + ticked + ", tick_date = '" + m_sdFrom + "' ";
				sc += " WHERE type = '" + type + "' AND rid = " + rid + " ";
			sc += " END ";
		}
//		else
//		{
//			sc += " UPDATE accrecon SET ticked=0 WHERE type = '" + type + "' AND rid = " + rid + " ";
//		}
		
		if(Session["recon_amount_" + markid + m_ssid] != null)
		{
			double dAmount = (double)Session["recon_amount_" + markid + m_ssid];
			sc += " IF NOT EXISTS(SELECT * FROM accrecon WHERE type='" + type + "' AND rid=" + rid + ") ";
			sc += " BEGIN ";
				sc += " INSERT INTO accrecon (account_id, type, rid, mark_amount, ticked, tran_date, tran_amount, tran_ref) ";
				sc += " VALUES(" + m_accountID + ", '" + type + "', " + rid + ", " + dAmount + ", " + ticked;
				sc += ", '" + EncodeQuote(tran_date) + "', " + tran_amount + ", '" + EncodeQuote(tran_ref) + "') ";
			sc += " END ";
			sc += " ELSE ";
			sc += " BEGIN ";
				sc += " UPDATE accrecon SET mark_amount = " + dAmount + ", ticked=" + ticked;
				sc += " WHERE type = '" + type + "' AND rid = " + rid + " ";
			sc += " END ";
		}
	}
//DEBUG("sc=", sc);
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

bool DoRestoreRecon()
{
	int rows = 0;
	string sc = " SELECT * FROM accrecon WHERE id=" + m_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "recon_balance");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<h4><font color=red>Error. Reconciliation not found</font></h4>");
		return true;
	}

	DataRow dr = ds.Tables["recon_balance"].Rows[0];
	m_accountID = dr["account_id"].ToString();
	m_account = GetAccountClass(m_accountID);
	m_nPeriod = 4;
	DateTime dTo = DateTime.Parse(dr["close_date"].ToString());
	m_sdFrom = DateTime.Parse(dr["open_date"].ToString()).ToString("dd-MM-yyyy");
	m_sdTo = dTo.ToString("dd-MM-yyyy");
	Session["recon_period" + m_ssid] = m_nPeriod;
	Session["recon_date_from" + m_ssid] = m_sdFrom;
	Session["recon_date_to" + m_ssid] = m_sdTo;
	Session["recon_opening_balance" + m_ssid] = MyDoubleParse(dr["open_balance"].ToString());
	Session["recon_expecting_balance" + m_ssid] = MyDoubleParse(dr["expect_balance"].ToString());

	ds.Clear();
	sc = " SELECT * FROM accrecon WHERE rid IS NOT NULL "; //get them all
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "recon_adjust");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["recon_adjust"].Rows[i];
		string type = dr["type"].ToString();
		string rid = dr["rid"].ToString();
		string amount = dr["mark_amount"].ToString();
		bool bTicked = MyBooleanParse(dr["ticked"].ToString());
		if(bTicked)
		{
			DateTime tick_date = DateTime.Parse(dr["tick_date"].ToString());
			if(tick_date > dTo)
				bTicked = false;
		}

		string markid = type + rid;
		if(amount != "")
			Session["recon_amount_" + markid + m_ssid] = MyDoubleParse(amount);
		if(bTicked)
			Session["recon_ticked_" + markid + m_ssid] = true;
		else
			Session["recon_ticked_" + markid + m_ssid] = null;
	}
	ds.Clear();
	return true;
}

bool DoDeleteRecon()
{
	if(Request.Form["confirm_delete"] != "on")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3><font color=red>Please tick confirm to delete</font></h3><br>");
		Response.Write("<input type=button onclick=history.go(-1) value=Back class=b>");
		PrintAdminFooter();
		return false;
	}
	string sc = " SET DATEFORMAT dmy ";
	sc += " DELETE FROM accrecon WHERE type='balance' AND open_date='" + m_sdFrom + "' AND close_date='" + m_sdTo + "' ";
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

bool PrintAccountList()
{
	string account = "1111";

	if(Request.QueryString["fa"] != null && Request.QueryString["fa"] != "")
		m_account = Request.QueryString["fa"];

	int rows = 0;
	string sc = "SELECT * FROM account WHERE class1=1 OR class1=2 ORDER BY class1, class2, class3, class4";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "account");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<table width=100%>");
	Response.Write("<tr><td><b>Account : </b>");
	Response.Write("<select name=acc>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["account"].Rows[i];
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		Response.Write("<option value=" + number);
		if(number == m_account)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString()+ " $" +dr["balance"].ToString());		
	}
	Response.Write("</select></td></tr></table>");
	return true;
}

void PrintJavaCode()
{
	StringBuilder sb = new StringBuilder();

	sb.Append("\r\n<script language=JavaScript");
	sb.Append(">\r\n");
	sb.Append("function CalcBalance()	\r\n");
	sb.Append("{						\r\n");
	sb.Append("		var opening_balance = Number(document.f.opening_balance.value);		\r\n");
	sb.Append("		var closing_balance = opening_balance;		\r\n");
	sb.Append("		var expecting_balance = Number(document.f.expecting_balance.value);		\r\n");
	sb.Append("		for(var i=0; i<" + m_nRows.ToString() + "; i++)	\r\n");
	sb.Append("		{						\r\n");
	sb.Append("			if(eval(\"document.f.tick\" + i + \".checked\"))	\r\n");
	sb.Append("			{													\r\n");
	sb.Append("				isincome = eval(\"document.f.income\" + i + \".value\");\r\n");
	sb.Append("				amount = Number(eval(\"document.f.amount\" + i + \".value\"));\r\n");
	sb.Append("				if(isincome == '1')\r\n");
	sb.Append("					closing_balance += amount;	\r\n");
	sb.Append("				else					\r\n");
	sb.Append("					closing_balance -= amount;	\r\n");
	sb.Append("			}													\r\n");
	sb.Append("			eval(\"document.f.ongoing_balance\" + i + \".value= closing_balance\");	\r\n");
	sb.Append("		}							\r\n");
	sb.Append("		var out_of_balance = expecting_balance - closing_balance; ");
	sb.Append("		document.f.closing_balance.value = closing_balance;		\r\n");
	sb.Append("		document.f.out_of_balance.value = out_of_balance;		\r\n");
	sb.Append("}			\r\n</script");
	sb.Append(">\r\n");

	Response.Write(sb.ToString());
}

bool IsAssetsPayment(string tran_id)
{
	string sc = " SELECT * FROM assets_payment WHERE tran_id = " + tran_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "recon") > 0)
			return true;
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

</script>

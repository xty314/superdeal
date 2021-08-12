<script runat=server>

string m_id = ""; //for restore
string m_type = "0";
string m_tableTitle = "Account Reconciliation";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

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

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["t"] == "new")
	{
		Session["accrecon_period"] = null;
		Session["accrecon_date_from"] = null;
		Session["accrecon_date_to"] = null;
		PrepareNewRecon();
	}
	else if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		if(!DoRestoreRecon())
			return;
	}

	if(Request.Form["cmd"] == "Reset")
	{
		PrepareNewRecon();
	}
	else if(Request.Form["cmd"] == "OK")
	{
		double dAmount = MyMoneyParse(Request.Form["amount"]);
		string id = Request.Form["mark_id"];
		if(MyBooleanParse(Request.Form["is_minus"]))
			dAmount = 0 - dAmount;

		Session["accrecon_amount_" + id] = dAmount;
	}
	else if(Request.Form["cmd"] == "Change") //change date
	{
		if(!DoChangeDate())
			return;
	}

	if(Request.Form["opening_balance"] != null)
	{
		m_dOpeningBalance = MyMoneyParse(Request.Form["opening_balance"]);
		Session["accrecon_opening_balance"] = m_dOpeningBalance;
	}
	else if(Session["accrecon_opening_balance"] != null)
		m_dOpeningBalance = (double)Session["accrecon_opening_balance"];

	if(Request.Form["closing_balance"] != null)
		Session["accrecon_closing_balance"] = Request.Form["closing_balance"];

	if(Request.Form["expecting_balance"] != null)
	{	
		m_dExpectingBalance = MyMoneyParse(Request.Form["expecting_balance"]);
		Session["accrecon_expecting_balance"] = m_dExpectingBalance;
	}
	else if(Session["accrecon_expecting_balance"] != null)
		m_dExpectingBalance = (double)Session["accrecon_expecting_balance"];

	if(Request.Form["acc"] != null && Request.Form["acc"] != "")
		m_account = Request.Form["acc"];
	else if(Request.QueryString["acc"] != null && Request.QueryString["acc"] != "")
		m_account = Request.QueryString["acc"];
	m_accName = GetAccountName(m_account);

	if(Request.Form["period"] != null)
	{
		m_nPeriod = MyIntParse(Request.Form["period"]);
		Session["accrecon_period"] = m_nPeriod;
	}
	else if(Session["accrecon_period"] != null)
	{
		m_nPeriod = (int)Session["accrecon_period"];
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

		Session["accrecon_date_from"] = m_sdFrom;
		Session["accrecon_date_to"] = m_sdTo;
	}
	else if(Session["accrecon_date_from"] != null)
	{
		if(Session["accrecon_date_from"] != null)
			m_sdFrom = Session["accrecon_date_from"].ToString();
		if(Session["accrecon_date_to"] != null)
			m_sdTo = Session["accrecon_date_to"].ToString();
	}

	if(Request.QueryString["t"] == "move")
	{
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		if(Request.QueryString["dir"] != "stop" && Session["accrecon_display_period"] == null)
		{
			//save current working sheet date information
			Session["accrecon_display_period"] = 0;
			Session["accrecon_working_period"] = m_nPeriod;
			Session["accrecon_working_date_from"] = m_sdFrom;
			Session["accrecon_working_date_to"] = m_sdTo;
//DEBUG("saved", 1);
		}

		if(Request.QueryString["dir"] == "back")
		{
			DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
			m_sdFrom = dFrom.AddMonths(-1).ToString("dd-MM-yyyy");
			m_sdTo = dFrom.AddDays(1).ToString("dd-MM-yyyy");
			Session["accrecon_period"] = 4; 
			Session["accrecon_date_from"] = m_sdFrom;
			Session["accrecon_date_to"] = m_sdTo;
		}
		else if(Request.QueryString["dir"] == "next")
		{
			DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
			m_sdTo = dTo.AddMonths(1).ToString("dd-MM-yyyy");
			m_sdFrom = dTo.AddDays(-1).ToString("dd-MM-yyyy");
			Session["accrecon_period"] = 4;
			Session["accrecon_date_from"] = m_sdFrom;
			Session["accrecon_date_to"] = m_sdTo;
		}
		else if(Request.QueryString["dir"] == "stop")
		{
//DEBUG("restored", 1);
			Session["accrecon_display_period"] = null;
			if(Session["accrecon_working_period"] != null)
			{
				Session["accrecon_period"] = (int)Session["accrecon_working_period"];
				Session["accrecon_date_from"] = Session["accrecon_working_date_from"].ToString();
				Session["accrecon_date_to"] = Session["accrecon_working_date_to"].ToString();
				m_nPeriod = (int)Session["accrecon_period"];
				m_sdFrom = Session["accrecon_date_from"].ToString();
				m_sdTo = Session["accrecon_date_to"].ToString();
			}
		}
	}
	if(Session["accrecon_display_period"] != null)
		m_bMoving = true;

	if(Request.QueryString["t"] == "new")
	{
		Session["accrecon_period"] = null;
	}
	else if(Request.Form["period"] == null) //
	{
		if(Session["accrecon_period"] != null)
			m_nPeriod = (int)Session["accrecon_period"];
		if(Session["accrecon_date_from"] != null)
			m_sdFrom = Session["accrecon_date_from"].ToString();
		if(Session["accrecon_date_to"] != null)
			m_sdTo = Session["accrecon_date_to"].ToString();
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
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=accrecon.aspx?t=new\">");
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
			Response.Write(" onclick=window.location=('accrecon.aspx?t=new') class=b>");
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
	Session["accrecon_opening_balance"] = null;
	Session["accrecon_expecting_balance"] = null;
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
	
	Response.Write("<form name=f action=accrecon.aspx method=post>");

	Response.Write("<br><center><h3>Account Reconciliation</h3>");

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
		Response.Write("<input type=button value='Hide History' onclick=window.location=('accrecon.aspx?t=new&ss=0') class=b>");
	else
		Response.Write("<input type=button value='Show History' onclick=window.location=('accrecon.aspx?t=new&ss=1') class=b>");
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
		Response.Write("<input type=button onclick=window.location=('accrecon.aspx?id=" + id + "') value=View class=b>");
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
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return sdFrom;
	}
	
	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	DateTime dFrom = DateTime.Parse(sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
	dFrom = dFrom.AddDays(1); //include the last day

	sdFrom = dFrom.ToString("dd-MM-yyyy");
	return sdFrom;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Monthly Profit Chart
bool PrintStatement()
{
	string sqlLoss = "";
	string sqlExpense = "";
	string sqlTrans = "";
	int i = 0;
	switch(m_nPeriod)
	{
	case 0:
		m_sdFrom = GetLastReconDate();
		sqlTrans = " d.trans_date >= '" + m_sdFrom + "' ";
		sqlExpense = " e.payment_date >= '" + m_sdFrom + "' ";
		break;
	case 1:
		sqlTrans = " DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) = 0 ";
		m_sdFrom = "01" + DateTime.Now.ToString("-MM-yyyy");
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		break;
	case 2:
		sqlTrans = " DATEDIFF(month, d.trans_date, GETDATE()) <= 1 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) <= 1 ";
		m_sdFrom = DateTime.Parse("01" + DateTime.Now.ToString("-MM-yyyy")).AddMonths(-1).ToString("dd-MM-yyyy");
//		m_sdTo = DateTime.Parse(m_sdFrom).AddMonth(1).ToString("dd-MM-yyyy");
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		break;
	case 3: //last 3 months
//		sqlTrans = " DATEDIFF(month, d.trans_date, GETDATE()) >= 1 AND DATEDIFF(month, commit_date, GETDATE()) <= 3 ";
//		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) >= 1 AND DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
		sqlTrans = " DATEDIFF(month, d.trans_date, GETDATE()) <= 3 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
		m_sdFrom = DateTime.Parse("01" + DateTime.Now.ToString("-MM-yyyy")).AddMonths(-3).ToString("dd-MM-yyyy");
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		break;
	case 4:
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		dTo = dTo.AddDays(1); //include the last day
		string from = dFrom.ToString("dd-MM-yyyy");
		string to = dTo.ToString("dd-MM-yyyy");
		sqlTrans = " d.trans_date >= '" + from + "' AND d.trans_date <= '" + to + "' ";
//		sqlLoss = " log_time >= '" + from + "' AND log_time <= '" + to + "' ";
		sqlExpense = " e.payment_date >= '" + from + "' AND e.payment_date <= '" + to + "' ";
		break;
	default:
		break;
	}
	ds.Clear();
	int rows = 0;
	
	//get income
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT t.id, t.amount, t.source, t.dest, i.purchase, 'sales' AS tran_type ";
	sc += ", d.trans_date, e.name AS payment, c.trading_name, c.company, c.name ";
	sc += " FROM trans t JOIN tran_detail d ON d.id=t.id ";
	sc += " JOIN tran_invoice i ON i.tran_id = t.id ";
	sc += " LEFT OUTER JOIN enum e ON e.id = d.payment_method AND e.class='payment_method' ";
	sc += " LEFT OUTER JOIN card c ON c.id = d.card_id ";
	sc += " WHERE ( t.banked = 1 OR t.source = " + m_account + " ) ";
	sc += " AND " + sqlTrans;
//	sc += " GROUP BY t.id, t.amount, t.source, t.dest, d.trans_date, e.name, c.trading_name, c.company, c.name ";
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
	//get expense
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT e.id, e.total AS amount, (a.class1 * 1000 + a.class2 * 100 + a.class3 * 10 + a.class4) AS source ";
	sc += ", '6000' AS dest, 0 AS purchase, 'expense' AS tran_type ";
	sc += ", e.date_recorded AS trans_date, enum.name AS payment, c.trading_name, c.company, c.name ";
	sc += " FROM expense e JOIN account a ON a.id = e.from_account ";
	sc += " LEFT OUTER JOIN enum ON enum.id = e.payment_type AND enum.class='payment_method' ";
	sc += " LEFT OUTER JOIN card c ON c.id = e.card_id ";
	sc += " WHERE e.from_account=" + m_accountID + " AND " + sqlExpense;
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
	Response.Write("<form action=accrecon.aspx method=post>");
	Response.Write("<input type=hidden name=acc value=" + m_account + ">");

	Response.Write("<br><table width=85%  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

//	Response.Write("<tr><td colspan=2><h5><i>" + m_sCompanyTitle + "</i></h5></td></tr>");
	Response.Write("<tr><td colspan=3 align=center><font size=+1><b>" + m_tableTitle + "</b></font></td></tr>");
	Response.Write("<tr><td colspan=3 align=center><b>" + m_accName + " - " + m_account + "</b></td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	Response.Write("<tr><td colspan=2 nowrap>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b> &nbsp&nbsp; ");
	Response.Write("<input type=button value='<< Search Back' ");
	Response.Write(" onclick=window.location=('accrecon.aspx?acc=" + m_account + "&t=move&dir=back') class=b>");
	Response.Write("<input type=button value=' Working Sheet ' ");
	Response.Write(" onclick=window.location=('accrecon.aspx?acc=" + m_account + "&t=move&dir=stop') class=b>");
	Response.Write("<input type=button value='Search Next >>' ");
	Response.Write(" onclick=window.location=('accrecon.aspx?acc=" + m_account + "&t=move&dir=next') class=b>");
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
	Response.Write("<th>Payment</th>");
	Response.Write("<th>Ref.</th>");
	Response.Write("<th>Credit</th>");
	Response.Write("<th>Debit</th>");
	Response.Write("<th>Balance</th>");
//	Response.Write("<th></th>");
	Response.Write("</tr>");

	if(!m_bMoving)
	{
		Response.Write("<tr bgcolor=yellow>");
		Response.Write("<td>&nbsp;</td>");
		Response.Write("<td>" + m_sdFrom + "</td>");
		Response.Write("<td colspan=5 align=right><font color=green><b>Opening Balance</b></font></td>");
		Response.Write("<td align=right>");
		if(Request.QueryString["co"] != null)
			Response.Write("<input type=text size=15 style=text-align:right;font-size:8pt; name=opening_balance value=" + m_dOpeningBalance + ">");
		else
			Response.Write("<a href=accrecon.aspx?acc=" + m_account + "&co=1 class=o title='Click to change'>" + m_dOpeningBalance.ToString("c") + "</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	DataRow[] dra = ds.Tables["trans"].Select("", "trans_date");
	double dBalance = m_dOpeningBalance;

	bool bAlterColor = false;
	int nEditRow = -1;
	if(Request.QueryString["row"] != null)
		nEditRow = MyIntParse(Request.QueryString["row"]);
	int nDateRow = -1;
	if(Request.QueryString["drow"] != null)
		nDateRow = MyIntParse(Request.QueryString["drow"]);
	for(i=0; i<dra.Length; i++)
	{
		bool bMarked = false; //price changed
		DataRow dr = dra[i];
		string id = dr["id"].ToString();
		string date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy");
		double dAmount = MyDoubleParse(dr["amount"].ToString());
		bool bIsMinus = false; //used for marking price
		if(dAmount < 0)
			bIsMinus = true;
		double dAmountOrg = dAmount;
		dAmountOrg = Math.Abs(dAmountOrg);
		string customer = dr["trading_name"].ToString();
		if(customer == "")
			customer = dr["company"].ToString();
		if(customer == "")
			customer = dr["name"].ToString();
		string tran_type = dr["tran_type"].ToString();
		bool bPurchase = MyBooleanParse(dr["purchase"].ToString()); 
		if(bPurchase)
			tran_type = "purchase";
		string markid = tran_type + id;

//		Response.Write("<input type=hidden name=opening_balance");
//		Response.Write("<input type=hidden name=tran_type" + i + " value=" + tran_type + ">");
//		Response.Write("<input type=hidden name=tran_id" + i + " value=" + id + ">");

		//marked amount check
		if(Session["accrecon_amount_" + markid] != null)
		{
			if(Request.Form["cmd"] == "Reset")
			{
				Session["accrecon_amount_" + markid] = null;
			}
			else
			{
				dAmount = (double)Session["accrecon_amount_" + tran_type + id];
				bMarked = true;
			}
		}
//DEBUG("drtran_type=", tran_type);
		//ticked or not
//DEBUG("markid=", markid);
//DEBUG("tran_type=", Request.Form["tran_type" + i]);
		bool bTicked = false;
		if(Request.Form["tran_type" + i] != null)
		{
//DEBUG("ticked=", Request.Form["tick" + i]);
			if(Request.Form["tick" + i] == "on")
			{
				Session["accrecon_ticked_" + markid] = true;
			}
			else// if(Request.Form["tick" + i] == "")
			{
				Session["accrecon_ticked_" + markid] = null;
			}
		}
		else if(Session["accrecon_ticked_" + markid] != null)
		{
			bTicked = true;
		}
		if(Session["accrecon_ticked_" + markid] != null)
		{
			if(Request.Form["cmd"] == "Reset")
				Session["accrecon_ticked_" + markid] = null;
			else
				bTicked = true;
		}

		if(bPurchase || tran_type.ToLower() == "expense")
			dAmount = 0 - dAmount;

		dBalance += dAmount;

		string payment = dr["payment"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td><input type=checkbox name=tick" + i);
		if(bTicked)
			Response.Write(" checked");
		Response.Write("></td>");
		Response.Write("<td>");
		if(nDateRow == i)
		{
			Response.Write("<input type=hidden name=tran_type value='" + tran_type + "'>");
			Response.Write("<input type=hidden name=tran_id value='" + id + "'>");
			Response.Write("<input type=hidden name=old_date value='" + date + "'>");
			Response.Write("<input type=text size=7 name=new_date value='" + date + "'>");
			Response.Write("<input type=submit name=cmd value=Change class=b>");
		}
		else
		{
			Response.Write("<a href=accrecon.aspx?acc=" + m_account + "&drow=" + i + " class=o title='Click to change date'>" + date + "</a>");
			Response.Write("<input type=hidden name=tran_type" + i + " value='" + tran_type + "'>");
			Response.Write("<input type=hidden name=tran_id" + i + " value='" + id + "'>");
		}
		Response.Write("</td>");
		Response.Write("<td><font color=");
		if(bPurchase)
			Response.Write("red");
		else if(tran_type.ToLower() == "expense")
			Response.Write("blue");
		else
			Response.Write("black");
		Response.Write(">");
		Response.Write(tran_type);
		Response.Write("</font></td>");
		Response.Write("<td>" + payment + "</td>");
		Response.Write("<td>" + customer + "</td>");
		
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
				Response.Write("<td align=right><a href=accrecon.aspx?acc=" + m_account + "&row=" + i + " class=o title='Original Amount : " + dAmountOrg.ToString("c") + ", click to edit'><font color=red>" + dAmount.ToString("c") + "</font></a></td>");
			else
				Response.Write("<td align=right><a href=accrecon.aspx?acc=" + m_account + "&row=" + i + " class=o title='Mark Amount'>" + dAmount.ToString("c") + "</a></td>");
		}
		
		if(!bIsIncome)
			Response.Write("<td>&nbsp;</td>");

		Response.Write("<td align=right>" + dBalance.ToString("c") + "</td>");
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
	Response.Write("<td colspan=5 align=right><font color=green><b>Closing Balance</b></font></td>");
	Response.Write("<td align=right>" + dBalance.ToString("c") + "</td>");
	Response.Write("</tr>");

	Response.Write("<input type=hidden name=closing_balance value=" + dBalance + ">");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td>" + m_sdTo + "</td>");
	Response.Write("<td colspan=5 align=right><font color=green><b>Expecting Balance</b></font></td>");
	Response.Write("<td align=right>");
	if(Request.QueryString["ce"] != null)
		Response.Write("<input type=text size=15 style=text-align:right;font-size:8pt; name=expecting_balance value=" + m_dExpectingBalance + ">");
	else
		Response.Write("<a href=accrecon.aspx?acc=" + m_account + "&ce=1 class=o title='Click to change'>" + m_dExpectingBalance.ToString("c") + "</a>");
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<td colspan=5 align=right><font color=green><b>Out Of Balance</b></font></td>");
	Response.Write("<td align=right>" + (m_dExpectingBalance - dBalance).ToString("c") + "</td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<input type=submit name=cmd value='Reset' class=b>");
	Response.Write("<input type=button onclick=window.location=('accrecon.aspx?t=new') value='New' class=b>");
	Response.Write("</td>");
	Response.Write("<td colspan=2 align=right nowrap>");
	Response.Write("<input type=checkbox name=confirm_delete>Confirm Delete ");
	Response.Write("<input type=submit name=cmd value=Delete class=b>");
	Response.Write("<input type=submit name=cmd value=Done class=b>");
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</form>");

	return true;
}

string GetAccountName(string number)
{
	DataSet dsname = new DataSet();
	string sc = " SELECT id, name1, name2, name3, name4 ";
	sc += " FROM account ";
	sc += " WHERE class1 * 1000 + class2 * 100 + class3 * 10 + class4 = " + number;
//DEBUG("sc=", sc);
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

string GetAccountClass(string id)
{
	DataSet dsname = new DataSet();
	string sc = " SELECT class1 * 1000 + class2 * 100 + class3 * 10 + class4 AS account_class ";
	sc += " FROM account ";
	sc += " WHERE id = " + id;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsname, "acc") == 1)
		{
			DataRow dr = dsname.Tables["acc"].Rows[0];
			return dr["account_class"].ToString();
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
	if(Session["accrecon_opening_balance"] != null)
		ob = Session["accrecon_opening_balance"].ToString();
	string cb = "0";
	if(Session["accrecon_closing_balance"] != null)
		cb = Session["accrecon_closing_balance"].ToString();
	string eb = "0";
	if(Session["accrecon_expecting_balance"] != null)
		eb = Session["accrecon_expecting_balance"].ToString();

	double dob = MyDoubleParse(ob);
	double dcb = MyDoubleParse(cb);
	double deb = MyDoubleParse(eb);
	double dOut = deb - dcb;

	if(Session["accrecon_date_from"] != null)
		m_sdFrom = Session["accrecon_date_from"].ToString();
	if(Session["accrecon_date_to"] != null)
		m_sdTo = Session["accrecon_date_to"].ToString();

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
		string ticked = "null";
		if(Request.Form["tick" + i] == "on")
			ticked = "1";
		if(Session["accrecon_amount_" + markid] != null)
		{
			double dAmount = (double)Session["accrecon_amount_" + markid];
			sc += " IF NOT EXISTS(SELECT * FROM accrecon WHERE type='" + type + "' AND rid=" + rid + ") ";
			sc += " BEGIN ";
				sc += " INSERT INTO accrecon (account_id, type, rid, mark_amount, ticked) VALUES(" + m_accountID + ", '" + type + "', " + rid + ", " + dAmount + ", " + ticked + ") ";
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
	m_sdFrom = DateTime.Parse(dr["open_date"].ToString()).ToString("dd-MM-yyyy");;
	m_sdTo = DateTime.Parse(dr["close_date"].ToString()).ToString("dd-MM-yyyy");
	Session["accrecon_period"] = m_nPeriod;
	Session["accrecon_date_from"] = m_sdFrom;
	Session["accrecon_date_to"] = m_sdTo;
	Session["accrecon_opening_balance"] = MyDoubleParse(dr["open_balance"].ToString());
	Session["accrecon_expecting_balance"] = MyDoubleParse(dr["expect_balance"].ToString());

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
		string ticked = dr["ticked"].ToString();

		string markid = type + rid;
		if(amount != "")
			Session["accrecon_amount_" + markid] = MyDoubleParse(amount);
		if(ticked != "")
			Session["accrecon_ticked_" + markid] = true;
		else
			Session["accrecon_ticked_" + markid] = null;
	}
	ds.Clear();
	return true;
}

bool DoChangeDate()
{
	string new_date = Request.Form["new_date"];
	if(new_date == Request.Form["old_date"])
		return true;
	Trim(ref new_date);
	if(new_date == "")
		return true;

	try
	{
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dNew = DateTime.Parse(new_date, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
	}
	catch(Exception e)
	{
		Response.Write("<br><center><h3>Error, input date format is incorrect, the right format is date-month-year, please try again");
		return true;
	}

	string id = Request.Form["tran_id"];
	string sc = " SET DATEFORMAT dmy ";
	string type = Request.Form["tran_type"];
	if(type == "expense")
		sc += " UPDATE expense SET date_recorded = '" + new_date + "' WHERE id = " + id;
	else
		sc += " UPDATE tran_detail SET trans_date = '" + new_date + "' WHERE id = " + id;
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

</script>

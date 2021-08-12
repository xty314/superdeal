<script runat=server>

string m_tableTitle = "Payment Report";
string m_datePeriod = "";
string m_dateSql = "";
string m_sqlDateTrans = "";
string m_sqlDateOut = "";

string[] m_EachMonth = new string[13];

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = 0;

DataSet ds = new DataSet();
double m_dInvoiceTotal = 0;
double m_dPaidTotal = 0;
double m_dUnpaidTotal = 0;
double m_dOtherTotal = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
		//monthly name
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

	if(Request.Form["cmd"] == null && Request.QueryString["continue"] != "1")
	{
		if(Request.QueryString["type"] == null || Request.QueryString["type"] == "")
		{
			PrintMainPage();
			return;
		}
		if(Request.QueryString["np"] != null && Request.QueryString["np"] != "")
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	}
	
	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.Form["Datepicker1_day"] != null)
	{
		//string day = Request.Form["day_from"];
		//string monthYear = Request.Form["month_from"];
		//ValidateMonthDay(monthYear, ref day);
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		//day = Request.Form["day_to"];
		//monthYear = Request.Form["month_to"];
		//ValidateMonthDay(monthYear, ref day);
		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}

	if(Request.QueryString["continue"] == "1")
	{
		if(Session["rbalance_date_period"] != null && Session["rbalance_date_period"] != "")
			m_nPeriod = (int)Session["rbalance_date_period"];
		if(Session["rbalance_date_from"] != null && Session["rbalance_date_from"] != "")
			m_sdFrom = (string)Session["rbalance_date_from"];
		if(Session["rbalance_date_to"] != null && Session["rbalance_date_to"] != "")
			m_sdTo = (string)Session["rbalance_date_to"];
	}
	else
	{
		Session["rbalance_date_period"] = m_nPeriod;
		Session["rbalance_date_from"] = m_sdFrom;
		Session["rbalance_date_to"] = m_sdTo;
	}

//DEBUG("m_nperido =", m_nPeriod);
//DEBUG("mtype =", m_type);
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yesterday";
		break;
	case 2:
		m_datePeriod = "This week";
		break;
	case 3:
		m_datePeriod = "This Month";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	default:
		break;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	
	DoBalanceReport();
	PrintAdminFooter();
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
	
	Response.Write("<form name=f action=rbalance.aspx method=post>");

	Response.Write("<br><center>");
	Response.Write("<font size=+1><b>" + m_tableTitle + "</b></font>");
	Response.Write("<h5>Select Date Period</h5>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Yesterday</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>This Week</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=4>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function from common.cs
		string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td>");
	Response.Write("<b>Select : </b> From Date ");
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		//if(int.Parse(s_day) == d)
		//	Response.Write("<option value="+ d +" selected>"+d+"</option>");
		//else
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

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
	Response.Write("<select name='Datepicker1_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
	for(int y=1997; y<int.Parse(s_year)+1; y++)
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
	Response.Write("</td>");
		//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
	Response.Write("<select name='Datepicker2_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

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
	Response.Write("<select name='Datepicker2_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int y=1997; y<int.Parse(s_year)+1; y++)
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
		
	Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}

bool DoBalanceReport()
{
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		m_sqlDateTrans = " AND DATEDIFF(day, d.trans_date, GETDATE()) = 0 ";
		m_sqlDateOut = " AND DATEDIFF(day, i.commit_date, GETDATE()) > 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";
		m_sqlDateTrans = " AND DATEDIFF(day, d.trans_date, GETDATE()) = 1 ";
		m_sqlDateOut = " AND DATEDIFF(day, i.commit_date, GETDATE()) <> 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(week, i.commit_date, GETDATE()) = 0 ";
		m_sqlDateTrans = " AND DATEDIFF(week, d.trans_date, GETDATE()) = 0 ";
		m_sqlDateOut = " AND DATEDIFF(week, i.commit_date, GETDATE()) > 0 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		m_sqlDateTrans = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		m_sqlDateOut = " AND DATEDIFF(month, i.commit_date, GETDATE()) > 0 ";
		break;
	case 4:
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59 "+"' ";
		m_sqlDateTrans = " AND d.trans_date >= '" + m_sdFrom + "' AND d.trans_date <= '" + m_sdTo + " 23:59 "+"' ";
		m_sqlDateOut = " AND (i.commit_date < '" + m_sdFrom + "' OR i.commit_date > '" + m_sdTo + " 23:59 "+"') ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	//get invoice total
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT SUM(i.total) AS invoice_total, SUM(i.amount_paid) AS paid_total, SUM(i.total - i.amount_paid) AS unpaid_total ";
	sc += " FROM invoice i WHERE 1=1 ";
	sc += m_dateSql;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "inv_total");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = ds.Tables["inv_total"].Rows[0];
	m_dInvoiceTotal = MyDoubleParse(dr["invoice_total"].ToString());
	m_dPaidTotal = MyDoubleParse(dr["paid_total"].ToString());
	m_dUnpaidTotal = MyDoubleParse(dr["unpaid_total"].ToString());

	//get transaction total
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT SUM(t.amount) AS trans_total ";
	sc += " FROM trans t JOIN tran_detail d ON d.id = t.id ";
	sc += " WHERE t.dest IS NOT NULL "; //null means paid out not received
	sc += m_sqlDateTrans;
//DEBUG("sc1=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "trans_total");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	dr = ds.Tables["trans_total"].Rows[0];
	m_dPaidTotal = MyDoubleParse(dr["trans_total"].ToString());

	//get other payment total
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT SUM(ti.amount_applied) AS other_total ";
	sc += " FROM tran_invoice ti JOIN tran_detail d ON d.id = ti.tran_id ";
	sc += " JOIN trans t ON t.id = d.id ";
	sc += " JOIN invoice i ON i.invoice_number = ti.invoice_number ";
	sc += " WHERE ti.purchase = 0 AND t.dest IS NOT NULL ";
	sc += m_sqlDateTrans;
	sc += m_sqlDateOut;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "other_total");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	dr = ds.Tables["other_total"].Rows[0];
	m_dOtherTotal = MyDoubleParse(dr["other_total"].ToString());

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
	
	double dBalance = 0;
	dBalance = m_dInvoiceTotal - m_dPaidTotal - m_dUnpaidTotal + m_dOtherTotal; 

	//print report
	Response.Write("<table align=center valign=center cellspacing=10 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td align=right><b>Invoice Total : </b></td><td align=right>" + m_dInvoiceTotal.ToString("c") + "</td></tr>");
	Response.Write("<tr><td align=right><a href=rbalance.aspx?continue=1&showpayment=1 class=o title='Click to see details'>");
	Response.Write("<b>Total Received : </b></a></td><td align=right>" + m_dPaidTotal.ToString("c") + "</td></tr>");
	if(Request.QueryString["showpayment"] == "1")
	{
		Response.Write("<tr><td colspan=2>");
		ShowPaymentList();
		Response.Write("</td></tr>");
	}
	Response.Write("<tr><td align=right><a href=rbalance.aspx?continue=1&showunpaidlist=1 class=o title='Click to show unpaid invoices'>");
	Response.Write("<b>Unpaid Total : </b></a></td><td align=right>" + m_dUnpaidTotal.ToString("c") + "</td></tr>");
	if(Request.QueryString["showunpaidlist"] == "1")
	{
		Response.Write("<tr><td colspan=2>");
		ShowUnpaidList();
		Response.Write("</td></tr>");
	}
	Response.Write("<tr><td align=right><a href=rbalance.aspx?continue=1&showothers=1 class=o title='Click to show all other payment received within this period'>");
	Response.Write("<b>Other payment Total : </b></a></td><td align=right>" + m_dOtherTotal.ToString("c") + "</td></tr>");
	if(Request.QueryString["showothers"] == "1")
	{
		Response.Write("<tr><td colspan=2>");
		ShowOtherPayment();
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td align=right><b>Out of balance : </b></td><td align=right>" + dBalance.ToString("c") + "</td></tr>");
	Response.Write("</table>");

	Response.Write("<center>");
	Response.Write("<input type=button onclick=window.location=('rbalance.aspx?continue=1') value='Hide Details' class=b>");
	Response.Write("<input type=button onclick=window.location=('rbalance.aspx') value='Reset' class=b>");

	return true;
}

bool ShowPaymentList()
{
	int rows = 0;

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT e.name AS payment_method, SUM(t.amount) AS total ";
	sc += " FROM trans t JOIN tran_detail d ON d.id = t.id ";
	sc += " JOIN enum e ON e.class='payment_method' AND e.id = d.payment_method ";
	sc += " WHERE t.dest IS NOT NULL ";
	sc += m_sqlDateTrans;
	sc += " GROUP BY e.name ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "group_total");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<table align=center valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr style=\"color:white;background-color:#888888;font-weight:bold;\">");
	Response.Write("<th> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Payment Type</th>");
	Response.Write("<th> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Total</th>");
	Response.Write("</tr>");

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["group_total"].Rows[i];
		string method = dr["payment_method"].ToString();
		double dAmount = MyMoneyParse(dr["total"].ToString());

		string method_id = GetEnumID("payment_method", method);

		Response.Write("<tr><td align=right>");
		Response.Write("<a href=rbalance.aspx?continue=1&showpayment=1&showdetail=" + method_id);
		Response.Write(" class=o title='Click to see details'>" + Capital(method) + "</a></td>");
		Response.Write("<td align=right>" + dAmount.ToString("c") + "</td></tr>");

		if(Request.QueryString["showdetail"] == method_id)
		{
			Response.Write("<tr><td colspan=2 align=right>");
			ShowPaymentDetail(method_id);
			Response.Write("</td></tr>");
		}
	}
	Response.Write("</table>");
	return true;
}

bool ShowPaymentDetail(string payment_method)
{
	int rows = 0;

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT t.id, t.amount ";
	sc += " FROM trans t JOIN tran_detail d ON d.id = t.id ";
	sc += " WHERE t.dest IS NOT NULL AND d.payment_method = " + payment_method;
	sc += m_sqlDateTrans;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "payment_detail");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<table align=right cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr style=\"color:white;background-color:#888888;font-weight:bold;\">");
	Response.Write("<th> &nbsp;&nbsp; Transactin ID</th>");
	Response.Write("<th> &nbsp;&nbsp; Total</th>");
	Response.Write("</tr>");

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["payment_detail"].Rows[i];
		string id = dr["id"].ToString();
		double dAmount = MyMoneyParse(dr["amount"].ToString());

		Response.Write("<tr><td align=center>");
		Response.Write("<a href=payhistory.aspx?t=p&id=" + id + " class=o target=_blank title='Click to trace'>");
		Response.Write(id + "</a></td>");
		Response.Write("<td align=right>" + dAmount.ToString("c") + "</td></tr>");
	}
	Response.Write("</table>");
	return true;
}

bool ShowUnpaidList()
{
	int rows = 0;

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.invoice_number, i.total, i.amount_paid, c1.trading_name AS customer, c1.company, c1.name ";//, c2.name AS sales ";
	sc += " FROM invoice i JOIN card c1 ON c1.id = i.card_id ";
//	sc += " JOIN card c2 ON c2.id = i.
	sc += " WHERE total <> amount_paid AND paid = 0 ";
	sc += m_dateSql;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "unpaid_list");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<table align=center valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr style=\"color:white;background-color:#888888;font-weight:bold;\">");
	Response.Write("<th> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Invoice Number</th>");
	Response.Write("<th> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Customer</th>");
	Response.Write("<th>Invoice Total</th>");
	Response.Write("<th>Amount Paid</th>");
	Response.Write("<th>Balance owed</th>");
	Response.Write("</tr>");

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["unpaid_list"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string customer = dr["customer"].ToString();
		if(customer == "")
			customer = dr["company"].ToString();
		if(customer == "")
			customer = dr["name"].ToString();
		double dTotal = MyMoneyParse(dr["total"].ToString());
		double dAmountPaid = MyMoneyParse(dr["amount_paid"].ToString());
		double dBalance = dTotal - dAmountPaid;

		Response.Write("<tr>");
		Response.Write("<td><a href=invoice.aspx?" + invoice_number + " class=o target=_blank>");
		Response.Write(invoice_number + "</td>");
		Response.Write("<td>" + customer + "</td>");
		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dAmountPaid.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dBalance.ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

bool ShowOtherPayment()
{
	int rows = 0;

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.invoice_number, i.total, i.amount_paid, ti.amount_applied, e.name AS payment_method ";
	sc += ", c1.trading_name AS customer, c1.company, c1.name ";//, c2.name AS sales ";
	sc += " FROM invoice i JOIN card c1 ON c1.id = i.card_id ";
	sc += " JOIN tran_invoice ti ON ti.invoice_number = i.invoice_number ";
	sc += " JOIN tran_detail d ON d.id = ti.tran_id ";
	sc += " JOIN enum e ON e.id = d.payment_method AND e.class='payment_method' ";
	sc += " WHERE ti.amount_applied <> 0 AND ti.purchase = 0 ";
	sc += m_sqlDateTrans;
	sc += m_sqlDateOut;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "other_payment");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<table align=center valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr style=\"color:white;background-color:#888888;font-weight:bold;\">");
	Response.Write("<th> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Invoice Number</th>");
	Response.Write("<th> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Customer</th>");
	Response.Write("<th>Amount Received</th>");
	Response.Write("<th>Payment Type</th>");
	Response.Write("</tr>");

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["other_payment"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string customer = dr["customer"].ToString();
		if(customer == "")
			customer = dr["company"].ToString();
		if(customer == "")
			customer = dr["name"].ToString();
		double dAmount = MyMoneyParse(dr["amount_applied"].ToString());
		string payment_type = dr["payment_method"].ToString();

		Response.Write("<tr>");
		Response.Write("<td><a href=invoice.aspx?" + invoice_number + " class=o target=_blank>");
		Response.Write(invoice_number + "</td>");
		Response.Write("<td>" + customer + "</td>");
		Response.Write("<td align=right>" + dAmount.ToString("c") + "</td>");
		Response.Write("<td align=right>" + Capital(payment_type) + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

</script>

<!-- #include file="page_index.cs" -->
<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet dst = new DataSet();

bool m_bCanRollBack = true;
string m_action = "";
string m_id = "";
string m_creditID = "";

string m_sdFrom = DateTime.Now.ToString("dd-MM-yyyy");
string m_sdTo = DateTime.Now.AddDays(1).ToString("dd-MM-yyyy");
int m_nPeriod = 3;
string m_tableTitle = "Payment Trace";
string m_datePeriod = "";
string m_dateSql = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("accountant"))
		return;

	m_action = Request.QueryString["t"];

	if(Request.QueryString["t"] == "search")
	{
		LoadCustomerList();
		return;
	}
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];
	if(Request.QueryString["card"] != "" && Request.QueryString["card"] != null && TSIsDigit(Request.QueryString["card"].ToString()))
		GetCardDetails(Request.QueryString["card"]);
	else
	{
		Session["ss_customer_id"] = null;
		Session["ss_customer_name"] = null;
	}
	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	//if(Request.Form["day_from"] != null)
	if(Request.Form["Datepicker1_day"] != null)
	{
		//string day = Request.Form["day_from"];
		//string monthYear = Request.Form["month_from"];
		
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		//ValidateMonthDay(monthYear, ref day);
		m_sdFrom = day + "-" + monthYear;

		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
		//day = Request.Form["day_to"];
		//monthYear = Request.Form["month_to"];
		//ValidateMonthDay(monthYear, ref day);
//DEBUG("dto=", m_sdTo);
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		m_sdTo = dTo.AddDays(1).ToString("dd-MM-yyyy");
	}

	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Current Month";
		break;
	case 1:
		m_datePeriod = "Last Month";
		break;
	case 2:
		m_datePeriod = "Last Three Month";
		break;
	case 3:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		//DEBUG(" ms_dform = ", m_sdFrom);
		//DEBUG(" ms_dto = ", m_sdTo);
		//System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		//DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		//DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		//dTo = dTo.AddDays(1);
		//m_sdFrom = dFrom.ToString("dd-MM-yyyy");
		//m_sdTo = dTo.ToString("dd-MM-yyyy");
		break;
	default:
		break;
	}

	if(!IsPostBack)
	{
		if(m_action == "p")
		{
			PrintAdminHeader();
			PrintAdminMenu();
			ListCustPayDetails();			
		}
		else if(m_action == "rollback")
		{
			if(Request.QueryString["confirmed"] == "1")
			{
				if(DoRollBack())
				{
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=payhistory.aspx?t=rollback&finished=1\">");
				}
			}
			else if(Request.QueryString["finished"] == "1")
			{
				PrintAdminHeader();
				PrintAdminMenu();
				Response.Write("<br><center><h3>Done !</h3>");
				Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location='");
				Response.Write("payhistory.aspx' value='Trace Another Transaction'>");
			}
			else
			{
				Response.Write("<script Language=javascript");
				Response.Write(">");
				Response.Write("if(window.confirm('Dear " + Session["name"]);
				Response.Write("\\r\\n\\r\\nAre you sure you want to roll back this transaction?         ");
				Response.Write("\\r\\nThis action cannot be undone.\\r\\n");
				Response.Write("\\r\\nClick OK to continue.\\r\\n");
				Response.Write("'))");
				Response.Write("window.location='payhistory.aspx?t=rollback&id=" + m_id + "&confirmed=1';\r\n");
				Response.Write("else window.location='payhistory.aspx?t=p&id=" + m_id + "';\r\n");
				Response.Write("</script");
				Response.Write(">");
			}
			return;
		}
		else
		{
			if(!DoSearch())
				return;
			PrintAdminHeader();
			PrintAdminMenu();
			string card = "";
			if(Request.QueryString["card"] != "" && Request.QueryString["card"] != null)
				card = Request.QueryString["card"].ToString();
			Response.Write("<form name=f action=payhistory.aspx?card="+card+" method=post>");
			BindGrid();
			PrintDateForm();
			Response.Write("</form>");
		}		
	}	
	if(m_action != "p")
		LFooter.Text = m_sAdminFooter;
//	PrintAdminFooter();
}

bool GetCardDetails(string card_id)
{
	string sc = " SELECT TOP 1 company, trading_name, id FROM card WHERE id = "+ card_id +"";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "cardfound") == 1)
		{
			Session["ss_customer_id"] = dst.Tables["cardfound"].Rows[0]["id"].ToString();
			Session["ss_customer_name"] = dst.Tables["cardfound"].Rows[0]["company"].ToString();
			if(Session["ss_customer_name"] == "" || Session["ss_customer_name"] == null)
				Session["ss_customer_name"] = dst.Tables["cardfound"].Rows[0]["trading_name"].ToString();
		}
	
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}
void PrintDateForm()
{
	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=5><b>Search : <select name=search_type >");
	
	Response.Write("<option value=1");
	if(Session["search_type"] != null && Session["search_type"] != "")
		if(Session["search_type"].ToString() == "1")
			Response.Write(" selected ");
	Response.Write(">Invoice#</option>");
	Response.Write("<option value=2");
	if(Session["search_type"] != null && Session["search_type"] != "")
		if(Session["search_type"].ToString() == "2")
			Response.Write(" selected ");
	Response.Write(">Cheque#</option>");
	Response.Write("</select>");
	Response.Write(" </b>");
	Response.Write("<input type=text name=invoice_number value='" + Session["payhistory_invoice_number"] + "'>");

//	Response.Write("<tr><td colspan=5><b>Invoice# : </b>");
//	Response.Write("<input type=text name=invoice_number value='" + Session["payhistory_invoice_number"] + "'>");
	Response.Write("<input type=submit name=cmd value='Enquiry' " + Session["button_style"] + "></td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Last Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function from common.cs

	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td>");
	Response.Write("<b>Select : </b> From Date ");
	Response.Write("<select name='Datepicker1_day' onChange=\"document.f.period[3].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		//if(int.Parse(s_day) == d)
		//	Response.Write("<option value="+ d +" selected>"+d+"</option>");
		//else
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"document.f.period[3].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		if(m == 1)
			txtMonth = "JAN";
		if(m == 2)
			txtMonth = "FEB";
		if(m == 3)
			txtMonth = "MAR";
		if(m == 4)
			txtMonth = "APR";
		if(m == 5)
			txtMonth = "MAY";
		if(m == 6)
			txtMonth = "JUN";
		if(m == 7)
			txtMonth = "JUL";
		if(m == 8)
			txtMonth = "AUG";
		if(m == 9)
			txtMonth = "SEP";
		if(m == 10)
			txtMonth = "OCT";
		if(m == 11)
			txtMonth = "NOV";
		if(m == 12)
			txtMonth = "DEC";
		if(int.Parse(s_month) == m)
			Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
		else
			Response.Write("<option value="+m+">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_year' onChange=\"document.f.period[3].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
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
	Response.Write("<select name='Datepicker2_day' onChange=\"document.f.period[3].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"document.f.period[3].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		if(m == 1)
			txtMonth = "JAN";
		if(m == 2)
			txtMonth = "FEB";
		if(m == 3)
			txtMonth = "MAR";
		if(m == 4)
			txtMonth = "APR";
		if(m == 5)
			txtMonth = "MAY";
		if(m == 6)
			txtMonth = "JUN";
		if(m == 7)
			txtMonth = "JUL";
		if(m == 8)
			txtMonth = "AUG";
		if(m == 9)
			txtMonth = "SEP";
		if(m == 10)
			txtMonth = "OCT";
		if(m == 11)
			txtMonth = "NOV";
		if(m == 12)
			txtMonth = "DEC";
		if(int.Parse(s_month) == m)
			Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
		else
			Response.Write("<option value="+m+">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_year' onChange=\"document.f.period[3].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
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
	Response.Write("</td>");
	Response.Write("</tr>");
		
//------ END second display date -----------
	/*
	//from date
	Response.Write("<tr><td><b> &nbsp; From Date </b>");
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
	*/
	
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='Trace' " + Session["button_style"] + "></td></tr>");
	
	Response.Write("</table>");
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

void ListCustPayDetails()
{
//	int day = (int)Session["payquery_day"];
//	int month = (int)Session["payquery_month"];
//	int year = (int)Session["payquery_year"];

//	DateTime tstart = new DateTime(year, month, day);
//	DateTime tend = tstart.AddDays(1);

	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT d.id, c.trading_name, i.invoice_number ";
	sc += ", ISNULL(i.amount_applied, 0) AS amount_applied, i.purchase, d.trans_date, d.note, ";
	sc += " d.payment_method, d.payment_ref, t.amount, c.name, c1.name AS accountant ";
	sc += ", d.bank, d.branch, d.paid_by, d.invoice_number, ISNULL(inv.total, 0) AS sales_total, ISNULL(p.total_amount, 0) AS purchase_total ";
	sc += ", p.po_number ";
	sc += " FROM tran_detail d LEFT OUTER JOIN tran_invoice i ON d.id=i.tran_id ";
	sc += " JOIN trans t ON t.id = d.id ";
	sc += " LEFT OUTER JOIN invoice inv ON inv.invoice_number=i.invoice_number ";
	sc += " LEFT OUTER JOIN purchase p ON p.id=i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id=d.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id=d.staff_id ";
	sc += " WHERE d.id=" + m_id;
	sc += " ORDER BY d.trans_date DESC";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "custpaydetails");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
	
	DataRow dr = null;
	string payment_method = null;
	int i = 0;
	Response.Write("<br><br><table align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	int nCols = 7;
	//title
	Response.Write("<tr><td colspan=" + nCols + " align=center><font size=+1><b>Transaction Detail</b></font></td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	if(dst.Tables["custpaydetails"].Rows.Count > 0)
	{
		dr = dst.Tables["custpaydetails"].Rows[0];
		payment_method = GetEnumValue("payment_method", dr["payment_method"].ToString()).ToUpper();

		Response.Write("<tr><td colspan=" + nCols + ">");
		Response.Write("<table>");

		Response.Write("<tr><td><b>Transaction# : </b></td><td>");
		Response.Write(dr["id"].ToString());
		Response.Write("</td></tr>");

		Response.Write("<tr><td><b>Company : </b></td><td>");
		Response.Write(dr["trading_name"].ToString());
		Response.Write("</td>");

		Response.Write("<tr><td><b>Accountant : </b></td><td>");
		Response.Write(dr["accountant"].ToString());
		Response.Write("</td>");

		Response.Write("<tr><td><b>Payment Date : </b></td><td>");
		Response.Write(DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy HH:mm"));
		Response.Write("</td></tr>");

		Response.Write("</table>");

		if(payment_method.ToLower() == "cheque")
		{
			Response.Write("<tr><td>");
		}

		Response.Write("<tr><td colspan=" + nCols + " align=center><hr></td></tr>");
	}

	Response.Write("<tr><th width=100 align=left>Date</th>");
	Response.Write("<th width=150>Invoice# &nbsp&nbsp;</th>");
	Response.Write("<th align=center nowrap>Invoice Total &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp; </th>");
	Response.Write("<th align=center nowrap>Amount Paid</th>");
	Response.Write("<th width=90>Method</th>");
	Response.Write("<th width=90>Reference</th>");
	Response.Write("<th>Note</th>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=" + nCols + " align=center><hr></td></tr>");
	int NumInv = 0;
	string sInvNo = "";
	string sInvoices = "";
	string sTotalPaid = "";
	string sNote = "";
	for(i=0; i<dst.Tables["custpaydetails"].Rows.Count; i++)
	{
		dr = dst.Tables["custpaydetails"].Rows[i];
		string purchase = dr["purchase"].ToString();
		bool bPurchase = false;
		if(purchase != "")
			bPurchase = bool.Parse(purchase);

		string total = "0";
		Response.Write("<tr>");
		Response.Write("<td>" + DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy") + "</td>");
		Response.Write("<td align=left>");
		
		if(!bPurchase) //this is a sales payment
		{
			Response.Write("<a href='invoice.aspx?"+ dr["invoice_number"].ToString() + "&r=" + DateTime.Now.ToOADate());
			Response.Write("' target=_blank class=o>" + dr["invoice_number"].ToString() + "</a></td>");
			total = dr["sales_total"].ToString();
		}
		else
		{
			Response.Write("<a href='purchase.aspx?t=pp&n="+ dr["invoice_number"].ToString() + "&r=" + DateTime.Now.ToOADate());
			Response.Write("' target=_blank class=o>Purchase Order# " + dr["po_number"].ToString() + "</a></td>");
			total = dr["purchase_total"].ToString();
		}

		double dTotal = 0;
		if(total != "")
			dTotal = Math.Round(double.Parse(total), 2);
		double dPaid = Math.Round(double.Parse(dr["amount_applied"].ToString()), 2);
		Response.Write("<td align=left>" + dTotal.ToString("c") + "</td>");
		if(dPaid > dTotal)
			Response.Write("<td align=left><font color=green>" + dPaid.ToString("c") + "</font></td>");
		else if(dPaid < dTotal)
			Response.Write("<td align=left><font color=red>" + dPaid.ToString("c") + "</font></td>");
		else
			Response.Write("<td align=left>" + dPaid.ToString("c") + "</td>");
			
		Response.Write("<td align=center>" + payment_method + "</td>");
		Response.Write("<td align=center>" + dr["payment_ref"].ToString() + "</td>");
		Response.Write("<td>" + dr["note"].ToString() + "</td></tr>");
		
		sTotalPaid = dr["amount"].ToString();
	}
	if(sTotalPaid == null || sTotalPaid == "")
		sTotalPaid = "0";

	Response.Write("<tr><td colspan=" + nCols + " align=center><hr></td></tr>");
	Response.Write("<tr><td colspan=" + nCols + " align=right><b>Transaction Total: <font color=blue>");
	Response.Write(MyDoubleParse(sTotalPaid).ToString("c")+ "</font></b></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=" + (nCols-3).ToString() + " >");

	//check if it has generated any credit
	CheckCreditApplied();

	if(m_bCanRollBack)
	{
		Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location='");
		Response.Write("payhistory.aspx?t=rollback&id=" + m_id + "' value='Roll Back Transaction'>");
	}

	Response.Write("</td><td colspan=" + (nCols-3).ToString() + " align=right>");

	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location='");
	Response.Write("remit.aspx?id=" + m_id + "' value='Remittance'>");
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location='");
	Response.Write("payhistory.aspx?r=" + DateTime.Now.ToOADate() + "' value=' Back '>");
	Response.Write("</td></tr>");

	if(!m_bCanRollBack)
	{
		Response.Write("<tr><td colspan=" + (nCols-3).ToString() + ">");
		PrintCreditTrans();
		Response.Write("</td></tr>");
	}
	return;
}

bool CheckCreditApplied()
{
	string sc = " SELECT * FROM credit WHERE tran_id = " + m_id + " AND amount_applied <> 0 ";
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "rollBackCheck");
		if(rows > 0)
		{
			m_bCanRollBack = false;
			m_creditID = ds.Tables["rollBackCheck"].Rows[0]["id"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintCreditTrans()
{
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT d.id, c.trading_name, i.invoice_number ";
	sc += ", ISNULL(i.amount_applied, 0) AS amount_applied, i.purchase, d.trans_date, d.note, ";
	sc += " d.payment_method, d.payment_ref, t.amount, c.name, c1.name AS accountant ";
	sc += ", d.bank, d.branch, d.paid_by, d.invoice_number, ISNULL(inv.total, 0) AS sales_total, ISNULL(p.total_amount, 0) AS purchase_total ";
	sc += ", p.po_number ";
	sc += " FROM tran_detail d LEFT OUTER JOIN tran_invoice i ON d.id=i.tran_id ";
	sc += " JOIN trans t ON t.id = d.id ";
	sc += " LEFT OUTER JOIN invoice inv ON inv.invoice_number=i.invoice_number ";
	sc += " LEFT OUTER JOIN purchase p ON p.id=i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id=d.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id=d.staff_id ";
	sc += " WHERE d.credit_id=" + m_creditID;
	sc += " ORDER BY d.trans_date DESC";
//DEBUG("s c= ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "credit");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = null;
	string payment_method = null;
	int i = 0;

	Response.Write("<br><h5><font color=red>This transaction cannot rollback since its credit has been applied<br>");
	Response.Write("Please rollback the following transactions first.</font></h5>");

	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><th width=100 align=left>Date</th>");
	Response.Write("<th align=left>Tran# &nbsp&nbsp;</th>");
	Response.Write("<th align=left>Invoice# &nbsp&nbsp;</th>");
	Response.Write("<th align=center nowrap>Invoice Total &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp; </th>");
	Response.Write("<th align=center nowrap>Amount Paid</th>");
	Response.Write("<th width=120>Method</th>");
	Response.Write("<th width=120>Reference</th></tr><tr><td colspan=7 align=center><hr></td></tr>");
	int NumInv = 0;
	string sInvNo = "";
	string sInvoices = "";
	string sTotalPaid = "";
	for(i=0; i<dst.Tables["credit"].Rows.Count; i++)
	{
		dr = dst.Tables["credit"].Rows[i];
		payment_method = GetEnumValue("payment_method", dr["payment_method"].ToString()).ToUpper();
		string id = dr["id"].ToString();
		string purchase = dr["purchase"].ToString();
		bool bPurchase = false;
		if(purchase != "")
			bPurchase = bool.Parse(purchase);

		string total = "0";
		Response.Write("<tr>");
		Response.Write("<td>" + DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy") + "</td>");
		Response.Write("<td align=center><a href=payhistory.aspx?t=p&id=" + id + " class=o># " + id + "</a></td>");
		Response.Write("<td align=left>");
		
		if(!bPurchase) //this is a sales payment
		{
			Response.Write("<a href='invoice.aspx?"+ dr["invoice_number"].ToString() + "&r=" + DateTime.Now.ToOADate());
			Response.Write("' target=_blank class=o>" + dr["invoice_number"].ToString() + "</a></td>");
			total = dr["sales_total"].ToString();
		}
		else
		{
			Response.Write("<a href='purchase.aspx?t=pp&n="+ dr["invoice_number"].ToString() + "&r=" + DateTime.Now.ToOADate());
			Response.Write("' target=_blank class=o>Purchase Order# " + dr["po_number"].ToString() + "</a></td>");
			total = dr["purchase_total"].ToString();
		}

		double dTotal = 0;
		if(total != "")
			dTotal = Math.Round(double.Parse(total), 2);
		double dPaid = Math.Round(double.Parse(dr["amount_applied"].ToString()), 2);
		Response.Write("<td align=left>" + dTotal.ToString("c") + "</td>");
		if(dPaid > dTotal)
			Response.Write("<td align=left><font color=green>" + dPaid.ToString("c") + "</font></td>");
		else if(dPaid < dTotal)
			Response.Write("<td align=left><font color=red>" + dPaid.ToString("c") + "</font></td>");
		else
			Response.Write("<td align=left>" + dPaid.ToString("c") + "</td>");
			
		Response.Write("<td align=center>" + payment_method + "</td>");
		Response.Write("<td align=right>" + dr["payment_ref"].ToString() + "</td></tr>");
		
		sTotalPaid = dr["amount"].ToString();
	}
	if(sTotalPaid == null || sTotalPaid == "")
		sTotalPaid = "0";

	Response.Write("<tr><td colspan=7 align=center><hr></td></tr>");
	Response.Write("<tr><td colspan=7 align=right><b>Transaction Total: <font color=blue>");
	Response.Write(MyDoubleParse(sTotalPaid).ToString("c")+ "</font></b></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("</table>");
	return true;
}

bool DoSearch()
{
	string date_sql1 = "";
	string date_sql2 = "";
	string date_sql3 = "";
	string date_sqlCustom = "";
		
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		
	date_sql1 = " AND DATEDIFF(month, e.recorded_date, GETDATE()) = 0 ";
	date_sql2 = " AND DATEDIFF(month, e.payment_date, GETDATE()) = 0 ";
	date_sql3 = " AND DATEDIFF(month, t.payment_date, GETDATE()) = 0 ";
	date_sqlCustom = " AND DATEDIFF(month, e.statement_date, GETDATE()) = 0 ";
	
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 1 ";
	date_sql1 = " AND DATEDIFF(month, e.recorded_date, GETDATE()) = 1 ";
	date_sql2 = " AND DATEDIFF(month, e.payment_date, GETDATE()) = 1 ";
	date_sql3 = " AND DATEDIFF(month, t.payment_date, GETDATE()) = 1 ";
	date_sqlCustom = " AND DATEDIFF(month, e.statement_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) >= 1 AND DATEDIFF(month, d.trans_date, GETDATE()) <= 3 ";
	date_sql1 = " AND DATEDIFF(month, e.recorded_date, GETDATE()) >= 1 AND DATEDIFF(month, e.recorded_date, GETDATE()) <= 3 ";
	date_sql2 = " AND DATEDIFF(month, e.payment_date, GETDATE()) >= 1 AND DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
	date_sql3 = " AND DATEDIFF(month, t.payment_date, GETDATE()) >= 1 AND DATEDIFF(month, t.payment_date, GETDATE()) <= 3 ";
	date_sqlCustom = " AND DATEDIFF(month, e.statement_date, GETDATE()) >= 1 AND DATEDIFF(month, e.statement_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND d.trans_date >= '" + m_sdFrom + "' AND d.trans_date <= '" + m_sdTo + "' ";
	date_sql1 = " AND e.recorded_date >= '" + m_sdFrom + "' AND e.recorded_date <= '" + m_sdTo + "' ";
	date_sql2 = " AND e.payment_date >= '" + m_sdFrom + "' AND e.payment_date <= '" + m_sdTo + "' ";
	date_sql3 = " AND t.payment_date >= '" + m_sdFrom + "' AND t.payment_date <= '" + m_sdTo + "' ";
	date_sqlCustom = " AND e.statement_date >= '" + m_sdFrom + "' AND e.statement_date <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

string search_word = Request.Form["invoice_number"];
string search_type = Request.Form["search_type"];
Session["search_type"] = Request.Form["search_type"];

//*************money receive********************
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT e.name, d.card_id, d.trans_date, c.trading_name, c.company, c.name, d.payment_method, d.payment_ref ";
	sc += ", t.id, t.amount, d.id, d.invoice_number, i.purchase ";
	sc += " , 'payhistory.aspx?t=p&id='+ CONVERT(varchar(50),d.id) AS url ";
	sc += " FROM tran_detail d JOIN trans t ON t.id=d.id ";
	sc += " LEFT OUTER JOIN tran_invoice i ON i.tran_id=t.id AND d .id = i.tran_id ";
//	sc += " LEFT OUTER JOIN purchase p ON p.id = i.id ";
	sc += " JOIN enum e ON e.class = 'payment_method' AND e.id = d .payment_method ";
	sc += " JOIN card c ON c.id=d.card_id ";
	//sc += "  LEFT OUTER JOIN  credit ct ON ct.tran_id = t .id ";
	sc += " WHERE 1=1 ";
	if(search_word != null && search_word != "")
	{
		if(search_type == "1")
			sc += " AND d.invoice_number LIKE '%" + EncodeQuote(search_word) + "%' ";
		if(search_type == "2")
			sc += " AND d.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}
//	if(Request.Form["invoice_number"] != null && Request.Form["invoice_number"] != "")
//		sc += " WHERE d.invoice_number LIKE '%" + Request.Form["invoice_number"] + "%' ";
	else if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND d.card_id=" + Request.QueryString["card"];
	else
		sc += m_dateSql;
	sc += " AND ISNULL(i.purchase,0) = 0 ";
	
//	sc += " AND t.amount>0 ";
//	sc += " AND d.source_balance IS NULL AND d.dest_balance IS NOT NULL";
	sc += " ORDER BY d.payment_method, t.id ";
//DEBUG("sc =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
/*	
	//equity
	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT c.id AS card_id, c.trading_name, c.company, c.name, payment_type AS payment_method "; 
	sc += ", e.id, '0' AS invoice_number, 0 As purchase  ";
	sc += ", e.total AS amount, e.recorded_date AS trans_date, e.payment_ref ";
	sc += " , 'acc_owner.aspx?vw=rp&id='+ convert(varchar(50),e.id) AS url ";
	sc += " FROM acc_equity e ";
	sc += " LEFT OUTER JOIN card c ON c.id = e.trans_card_id ";
	sc += " WHERE (1=1) ";
	if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND e.trans_card_id = " + Request.QueryString["card"];
	if(search_word != null && search_word != "")
	{
		if(search_type == "2")
			sc += " AND e.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}
	sc += date_sql1;
//DEBUG(" sc equity = ", sc );
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	*/

//*************payment transaction********************
	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT d.card_id, d.trans_date, c.trading_name, c.company, c.name, d.payment_method, d.payment_ref ";
	sc += ", t.id, t.amount, d.id, d.invoice_number, i.purchase ";
	sc += ", 'payhistory.aspx?t=p&id=' + CONVERT(varchar, d.id) AS url ";
	sc += " FROM tran_detail d JOIN trans t ON t.id=d.id ";	
	sc += " LEFT OUTER JOIN tran_invoice i ON i.tran_id=t.id ";
	sc += " JOIN purchase p ON p.id = i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id=d.card_id ";
	sc += " WHERE 1=1 ";
	if(search_word != null && search_word != "")
	{
		if(search_type == "1")
			sc += " AND (p.po_number LIKE '%" + EncodeQuote(search_word) + "%' OR p.inv_number LIKE '%" + EncodeQuote(search_word) + "%' )";
			//sc += " AND d.invoice_number LIKE '%" + EncodeQuote(search_word) + "%' ";
		if(search_type == "2")
			sc += " AND d.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}
	else if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND d.card_id=" + Request.QueryString["card"];
	else
		sc += m_dateSql;
	if(search_word == null || search_word == "")
		sc += m_dateSql;
	sc += " AND i.purchase = 1 ";
	sc += " ORDER BY d.payment_method, t.id ";
//DEBUG("sc1 =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "pur_payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc += " SELECT d.card_id, d.trans_date, c.trading_name, c.company, c.name, d.payment_method, d.payment_ref ";
	sc += ", t.amount, d.id, d.invoice_number, i.purchase ";

	//pay assets
	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT c.trading_name, c.company, c.name, t.payment_type AS payment_method, 1 AS purchase ";
	sc += ", t.id ";
	sc += ", t.total AS amount, t.payment_date AS trans_date, t.payment_ref ";
	sc += ", 'asstlist.aspx?r="+ DateTime.Now.ToOADate() +"&tid='+ CONVERT(varchar(50), t.id) AS url ";
	sc += " FROM assets t JOIN assets_payment tp ON tp.assets_id = t.id ";
	sc += " JOIN assets_item ai ON ai.id = t.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = t.card_id ";
	sc += " WHERE 1=1 ";
	if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND t.card_id = " + Request.QueryString["card"];
	if(search_word != null && search_word != "")
	{
		if(search_type == "1")
			sc += " AND ai.invoice_number LIKE '%" + EncodeQuote(search_word) + "%' ";
		if(search_type == "2")
			sc += " AND t.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}
//	sc += " AND t.total - t.amount_paid <= 0 ";
	sc += date_sql3;
//DEBUG("sc paysaseet +", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "pur_payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//expense
	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT c.trading_name, c.company, c.name, e.payment_type AS payment_method, 1 AS purchase ";
	sc += ", e.id, e.card_id ";
	sc += ", CASE ei.total WHEN 0 THEN ei.tax ELSE ei.total END AS amount ";
	sc += ", e.payment_date AS trans_date, e.payment_ref ";
	sc += ", 'expense.aspx?id=' + CONVERT(varchar, e.id) AS url ";
//	sc += ", 'explist.aspx?tid=' + CONVERT(varchar, e.id) AS url ";
	sc += " FROM expense e JOIN expense_item ei ON ei.id = e.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = e.card_id ";
	sc += " WHERE 1=1 ";
	if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND e.card_id = " + Request.QueryString["card"];
	if(search_word != null && search_word != "")
	{
		if(search_type == "1")
			sc += " AND ei.invoice_number LIKE '%" + EncodeQuote(search_word) + "%' ";
		if(search_type == "2")
			sc += " AND e.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}
	sc += date_sql2;
//DEBUG("expens = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "pur_payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//custom GST
	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT c.trading_name, c.company, c.name, e.payment_type AS payment_method, 1 AS purchase ";
	sc += ", e.id , e.payee AS card_id ";
	sc += ", e.total_gst AS amount, e.statement_date AS trans_date, e.payment_ref ";
	sc += ", 'custax_rp.aspx?r="+ DateTime.Now.ToOADate()+"&type=1' AS url ";
	sc += " FROM custom_tax e ";
	sc += " LEFT OUTER JOIN card c ON c.id = e.payee ";
	sc += " WHERE 1=1 ";
	if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND e.payee = " + Request.QueryString["card"];
	if(search_word != null && search_word != "")
	{
		if(search_type == "1")
			sc += " AND e.clientcode LIKE '%" + EncodeQuote(search_word) + "%' ";
		if(search_type == "2")
			sc += " AND e.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}
	sc += date_sqlCustom;
//DEBUG("sc custom ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "pur_payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//invoice Refund
	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT c.trading_name, c.company, c.name, e.payment_type AS payment_method, 1 AS purchase ";
	sc += ", e.id, e.card_id ";
	sc += ", e.total AS amount, e.recorded_date AS trans_date, e.payment_ref";
	sc += ", 'ref_report.aspx?r="+ DateTime.Now.ToOADate() +"&rp='+ CONVERT(varchar(50),e.id) AS url ";
	sc += " FROM acc_refund e ";
	sc += " LEFT OUTER JOIN card c ON c.id = e.card_id ";
	sc += " WHERE 1=1 ";
	if(Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND e.card_id = " + Request.QueryString["card"];
	if(search_word != null && search_word != "")
	{		
		if(search_type == "2")
			sc += " AND e.payment_ref LIKE '%" + EncodeQuote(search_word) + "%' ";
	}	
	sc += date_sql1;
//DEBUG("refund =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "pur_payments");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

/////////////////////////////////////////////////////////////////

string TidyInvoice(string invoice_number)
{
	int nLength = invoice_number.Length;
	string s_text = "";
	//DEBUG("nLength =", nLength);
	char[] c_single = invoice_number.ToCharArray();
	for(int i = 0; i<nLength; i++)
	{
		s_text += c_single[i].ToString();
		if((i >= 50 && i <= 56 && c_single[i].ToString() == ",") || (i >= 110 && i<=115 && c_single[i].ToString() == ",")
			|| ( i >= 170 && i<=175 && c_single[i].ToString() == ","))
			s_text += " ";
	}
	
	return s_text;

}

void BindGrid()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["payments"] != null)
		rows = ds.Tables["payments"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	StringBuilder sb = new StringBuilder();

	Response.Write("<br><table align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	if(m_action == "p") //print
	{
		//address
		Response.Write("<tr><td colspan=4><b>" + m_sCompanyTitle + "</b><br>");
//		Response.Write("<i>Unit 26 761 Great South Road</i><br>");
//		Response.Write("<i>Penrose Auckland</i>");
		Response.Write("</td></tr>");
	}

	//title
	Response.Write("<tr><td colspan=4 align=center><font size=+1><b>Payment Trace</b></font></td></tr>");

	Response.Write("<tr><td colspan=4 align=center><h5>" + m_datePeriod + "</h5></td></tr>");

//	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	
	Response.Write("<tr valign=bottom>");
	Response.Write("<th align=left>Payment Method &nbsp&nbsp&nbsp&nbsp;</th>");
	Response.Write("<th align=left>Date &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;</th>");
//	Response.Write("<th align=left><a href=payhistory.aspx?t=search&r=" + DateTime.Now.ToOADate() + " class=o>Customer</a> &nbsp&nbsp&nbsp&nbsp;</th>");
	Response.Write("<th align=left>");
	Response.Write(" CUSTOMER: <select name=slt_customer onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&t=search')\"><option value='all'>All</option>");
	if(Session["ss_customer_id"] != null && Session["ss_customer_id"] != "")
		Response.Write("<option value='"+ Session["ss_customer_id"] +"' selected>"+ Session["ss_customer_name"] +"</option>");
	Response.Write("</select>");
	if(Session["ss_customer_id"] != null && Session["ss_customer_id"] != "")	
		Response.Write(" <a title='view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["ss_customer_id"] + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>?</a>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp;</th>");
	Response.Write("<th> Invoice# &nbsp&nbsp&nbsp&nbsp;</th>");
	Response.Write("<th align=right> &nbsp&nbsp&nbsp&nbsp; Amount</th>");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=5><hr></td></tr>");

	if(rows <= 0 && ds.Tables["pur_payments"].Rows.Count <= 0)
	{
		Response.Write("</table>");
		Response.Write(sb.ToString());
		return;
	}
	double dSubTotal = 0;
	double dGrandTotal = 0;
	string payment_method_old = "-1";
	double dTotalPurchase = 0;
	double dTotalreceived_text = 0;
	string tran_id_old = "";
	bool bAlterColor = true;
	if(rows > 0)
	{
		Response.Write("<tr><td colspan=5><font size=2><b>RECEIVED MONEY HISTORY</b></font></td></tr>");
	//	for(; i < rows && i < end; i++)
		for(; i < rows; i++)
		{
			DataRow dr = ds.Tables["payments"].Rows[i];
			string tran_id = dr["id"].ToString();
			if(tran_id == tran_id_old)
				continue;
			tran_id_old = tran_id;
			
			string card_id = dr["card_id"].ToString();
			string date = dr["trans_date"].ToString();
			string pm = dr["payment_method"].ToString();
			string invoice_number = dr["invoice_number"].ToString();
		
			if((invoice_number != null && invoice_number != "") || (invoice_number.Length > 1))
				invoice_number = TidyInvoice(invoice_number);
		
			string customer = dr["trading_name"].ToString();
			if(customer == "")
				customer = dr["name"].ToString();
			if(customer == "")
				customer = dr["company"].ToString();
			
			string total = dr["amount"].ToString();
			string purchase = dr["purchase"].ToString();

			bool bPurchase = false;
			if(purchase != "")
				bPurchase = bool.Parse(purchase);

			DateTime dd = DateTime.Parse(date);
			double dTotal = MyMoneyParse(total);
			if(bPurchase)
				dTotalPurchase += dTotal;
			else
				dTotalreceived_text += dTotal;
		

			string payment = GetEnumValue("payment_method", pm);

			//Response.Write("<tr><td colspan=2>&nbsp;</td><td colspan=3>"+customer+"</td></tr>");
			
			if(pm != payment_method_old)
			{
				if(dSubTotal != 0)
				{
					Response.Write("<tr><td>&nbsp;</td></tr>");
					Response.Write("<tr><td colspan=4 align=right><b>Deposit Total:</b></td>");
					Response.Write("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
					dSubTotal = 0;
				}

				Response.Write("<tr><td colspan=4><font size=+1><b>" + payment[0].ToString().ToUpper() + payment.Substring(1, payment.Length-1) + "</b></font></td></tr>");
				bAlterColor = true;
			}
			payment_method_old = pm;
		

			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");

  			if(payment.ToLower() == "cheque")
				Response.Write("<td>" + dr["payment_ref"].ToString() + "</td>");
			else
				Response.Write("<td>&nbsp;</td>");
			Response.Write("<td>" + dd.ToString("dd-MM-yy") + "</td>");
			Response.Write("<td>");//<font color=red>" + customer + "</font> ");
			Response.Write("<a title='view details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + card_id + "','', ' width=350,height=350');viewcard_window.focus();\" >" + customer + "</a>");
			//Response.Write("<input fgcolor=blue type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			//Response.Write("id=" + card_id + "','', ' width=350,height=350');\" value='" + customer + "' " + Session["button_style"] + ">");
			Response.Write(" &nbsp; </td>");
	//		Response.Write("<td><a href=invoice.aspx?" + invoice_number + "&r=" + DateTime.Now.ToOADate() + " class=o>" + invoice_number + "</a></td>");
			if(bPurchase)
			{
				Response.Write("<td>" + GetPoNumbers(invoice_number) + " &nbsp; </td>");
			}
			else
			{
				int nINCREASED = 12;
				string sinvoice = "";
				int nFound = 0;
				for(int k=0; k<invoice_number.Length; k++)
				{
					sinvoice += invoice_number[k].ToString();
					if(invoice_number[k].ToString() == ",")
						nFound++ ;
					
					if(nFound == nINCREASED)
					{
						nINCREASED += 12;
						sinvoice += "<br>";
					}
					
				}
				invoice_number = sinvoice;
				Response.Write("<td width=45%>" + invoice_number + " </td>");
			}
			string uri = dr["url"].ToString();
			//Response.Write("<td align=right><a href='payhistory.aspx?t=p&id=" + tran_id + "' class=o>");
			Response.Write("<td align=right><a title='view transactions' href="+ uri +" class=o target=blank>");
			Response.Write(dTotal.ToString("c") + "</td>");
			Response.Write("</tr>");
						
			dSubTotal += dTotal;
			if(payment.ToLower() != "credit apply")
			dGrandTotal += dTotal;
			
		}
		
		if(dSubTotal != 0)
		{
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td colspan=4 align=right><b>Deposit Total:</b></td>");
			Response.Write("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
		}

		Response.Write("<tr><td colspan=4 align=right><b>Grand Total:</b></td>");
		Response.Write("<td align=right><b>" + dGrandTotal.ToString("c") + "</b></td>");
		Response.Write("</tr>");
	}
	if(ds.Tables["pur_payments"].Rows.Count > 0)
	{
		dGrandTotal = 0;
		dSubTotal = 0;
		Response.Write("<tr><td colspan=5><font size=2><b>PAY PURCHASE/EXPENSE HISTORY</b></font></td></tr>");
		for(i=0; i<ds.Tables["pur_payments"].Rows.Count; i++)
		{
			DataRow dr = ds.Tables["pur_payments"].Rows[i];
			string tran_id = dr["id"].ToString();
			if(tran_id == tran_id_old)
				continue;
			tran_id_old = tran_id;
			
			string card_id = dr["card_id"].ToString();
			string date = dr["trans_date"].ToString();
			string pm = dr["payment_method"].ToString();
			string invoice_number = dr["invoice_number"].ToString();
		
			if((invoice_number != null && invoice_number != "") || (invoice_number.Length > 1))
				invoice_number = TidyInvoice(invoice_number);
		
			string customer = dr["trading_name"].ToString();
			if(customer == "")
				customer = dr["name"].ToString();
			if(customer == "")
				customer = dr["company"].ToString();
			
			string total = dr["amount"].ToString();
			string purchase	= dr["purchase"].ToString();

			bool bPurchase = false;
			if(purchase != "")
				bPurchase = bool.Parse(purchase);

			DateTime dd = DateTime.Parse(date);
			double dTotal = MyMoneyParse(total);
			if(bPurchase)
				dTotalPurchase += dTotal;
			else
				dTotalreceived_text += dTotal;
		

			string payment = GetEnumValue("payment_method", pm);

			//Response.Write("<tr><td colspan=2>&nbsp;</td><td colspan=3>"+customer+"</td></tr>");
			if(pm != payment_method_old)
			{
				if(dSubTotal != 0)
				{
					Response.Write("<tr><td>&nbsp;</td></tr>");
					Response.Write("<tr><td colspan=4 align=right><b>Deposit Total:</b></td>");
					Response.Write("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
					dSubTotal = 0;
				}
				
				Response.Write("<tr><td colspan=4><font size=+1><b>" + payment[0].ToString().ToUpper() + payment.Substring(1, payment.Length-1) + "</b></font></td></tr>");
				bAlterColor = true;
			}
			payment_method_old = pm;
		

			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");

  			if(payment.ToLower() == "cheque")
				Response.Write("<td>" + dr["payment_ref"].ToString() + "</td>");
			else
				Response.Write("<td>&nbsp;</td>");
			Response.Write("<td>" + dd.ToString("dd-MM-yy") + "</td>");
			Response.Write("<td>");//<font color=red>" + customer + "</font> ");
			Response.Write("<a title='view details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + card_id + "','', ' width=350,height=400, resizable=yes'); viewcard_window.focus();\" >" + customer + "</a>");
		//	Response.Write("<input fgcolor=blue type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
		//	Response.Write("id=" + card_id + "','', ' width=350,height=350');\" value='" + customer + "' " + Session["button_style"] + ">");
			Response.Write(" &nbsp; </td>");
	//		Response.Write("<td><a href=invoice.aspx?" + invoice_number + "&r=" + DateTime.Now.ToOADate() + " class=o>" + invoice_number + "</a></td>");
			if(bPurchase)
			{
				Response.Write("<td>" + GetPoNumbers(invoice_number) + " &nbsp; </td>");
			}
			else
			{
				int nINCREASED = 12;
				string sinvoice = "";
				int nFound = 0;
				for(int k=0; k<invoice_number.Length; k++)
				{
					sinvoice += invoice_number[k].ToString();
					if(invoice_number[k].ToString() == ",")
						nFound++ ;
					
					if(nFound == nINCREASED)
					{
						nINCREASED += 12;
						sinvoice += "<br>";
					}
					
				}
				invoice_number = sinvoice;
				Response.Write("<td  width=45%>" + invoice_number + " </td>");
			}
			string uri = dr["url"].ToString();
			//Response.Write("<td align=right><a href='payhistory.aspx?t=p&id=" + tran_id + "' class=o>");
			Response.Write("<td align=right><a title='view transaction' href="+ uri +" class=o target=blank>");
			Response.Write(dTotal.ToString("c") + "</td>");
			Response.Write("</tr>");

			dSubTotal += dTotal;
			dGrandTotal += dTotal;
		}
		
		if(dSubTotal != 0)
		{
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td colspan=4 align=right><b>Pay Purchase Total:</b></td>");
			Response.Write("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
		}

		Response.Write("<tr><td colspan=4 align=right><b>Grand Total:</b></td>");
		Response.Write("<td align=right><b>" + dGrandTotal.ToString("c") + "</b></td>");
		Response.Write("</tr>");
	}
	
/*
	Response.Write("<tr><td colspan=5><b>Search : <select name=search_type >");
	
	Response.Write("<option value=1");
	if(Session["search_type"] != null && Session["search_type"] != "")
		if(Session["search_type"].ToString() == "1")
			Response.Write(" selected ");
	Response.Write(">Invoice#</option>");
	Response.Write("<option value=2");
	if(Session["search_type"] != null && Session["search_type"] != "")
		if(Session["search_type"].ToString() == "2")
			Response.Write(" selected ");
	Response.Write(">Cheaque#</option>");
	Response.Write("</select>");
	Response.Write(" </b>");
	Response.Write("<input type=text name=invoice_number value='" + Session["payhistory_invoice_number"] + "'>");

//	Response.Write("<tr><td colspan=5><b>Invoice# : </b>");
//	Response.Write("<input type=text name=invoice_number value='" + Session["payhistory_invoice_number"] + "'>");
	Response.Write("<input type=submit name=cmd value='Enquiry' " + Session["button_style"] + "></td></tr>");
*/
	Response.Write("</table>");

	Response.Write("<br>");

}

string GetPoNumbers(string inv)
{
	string s = "";
	string[] invs = new string[1024];
	int m = 0;
	int i = 0;
	for(i=0; i<inv.Length; i++)
	{
		if(inv[i] != ',')
		{
			invs[m] += inv[i];
		}
		else
		{
			Trim(ref invs[m]);
			m++; // new invoice
		}
		if(m > 1023)
			break;
	}
	for(i=0; i<=m; i++)
	{
		string po = "";
		Trim(ref invs[i]);
		if(invs[i] != null && invs[i] != "")
		{
			po = GetPoNumber(invs[i]);
			if(po != "")
				s += "<a href=purchase.aspx?t=pp&n=" + invs[i] + " class=o>P" + po + "</a> ";
		}
	}
	return s;
}

void LoadCustomerList()
{
	string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h3><font size=+1><b>Customer List</b></font></h3>");

	if(m_sSite == "admin")
	{
		Response.Write("<form name=f id=search action=" + uri + " method=post>");
		Response.Write("<table width=100/%><tr><td><b>Customer :</b></td><td>");
		Response.Write("<input type=text size=20 name=ckw></td><td>");
		
		Response.Write("<script");
		Response.Write(">");
		Response.Write("document.f.ckw.focus();");
		Response.Write("</script");
		Response.Write(">");
	
		Response.Write("<input type=submit name=cmd value=Search "+ Session["button_style"] +"" );
		Response.Write(" onClick=window.location=('payhistory.aspx?id=0&r=" + DateTime.Now.ToOADate() + "')>");
		Response.Write("<input type=button name=cmd value='Cancel' "+ Session["button_style"] +"");
		Response.Write(" onClick=window.location=('payhistory.aspx?r=" + DateTime.Now.ToOADate() + "')>");
		Response.Write("</td></tr></table></form>\r\n");

		if(!DoCustomerSearchAndList())
			return;
	}
}

bool DoCustomerSearchAndList()
{
	string uri = Request.ServerVariables["URL"] + "?";	// + Request.ServerVariables["QUERY_STRING"];
	int rows = 0;
	string kw = "'%" + EncodeQuote(Request.Form["ckw"]) + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%%'";
	string s_random = DateTime.Now.ToOADate().ToString();
	string sc = "SELECT *, '" + uri + "' + 'card='+ LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + "' AS uri FROM card ";
	sc += " WHERE (";
	if(IsInteger(Request.Form["ckw"]))
		sc += " id=" + Request.Form["ckw"] + " OR ";
	sc += " name LIKE " + kw + " OR email LIKE " + kw + " OR trading_name LIKE " + kw + ")";
//	sc += " AND type<>3";		//type 3: supplier;
	sc += " ORDER BY name";
//DEBUG("sc=", sc);
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card_search");
		if(rows == 1)
		{
			Response.Write("<script language=javascript>window.location=('"+ uri +"card="+ dst.Tables["card_search"].Rows[0]["id"].ToString() +"");
			Response.Write("&r="+ DateTime.Now.ToOADate() + "')");
			Response.Write("</script");
			Response.Write(">");
			return false;
		}
		
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	BindCustomerGrid();

	return true;
}

void BindCustomerGrid()
{
	DataView source = new DataView(dst.Tables["card_search"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
	frmCalendar.Visible = true;
}

void MyDataGrid_PageA(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindCustomerGrid();
}

bool DoRollBack()
{
	//do transaction
	SqlCommand myCommand = new SqlCommand("eznz_payment_rollback", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;
	myCommand.Parameters.Add("@tran_id", SqlDbType.Int).Value = MyIntParse(m_id);
	myCommand.Parameters.Add("@return_status", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoCustomerPayment", e);
		return false;
	}
	string return_status = myCommand.Parameters["@return_status"].Value.ToString();
	if(return_status != "0")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, " + return_status);
		LFooter.Text = m_sAdminFooter;
		return false;
	}
	return true;
}

</script>

<table width=100%>
<tr><td><asp:Label id=LTable runat=server/></td></tr>

<tr><td>

<form id=frmCalendar runat=server visible=false>
<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=false
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=20
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_PageA
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=ACC_NUMBER
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=id/>
		<asp:HyperLinkColumn
			 HeaderText=PURCHASER
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=CUSTOMER
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=trading_name/>
		<asp:BoundColumn HeaderText=BALANCE DataField=balance DataFormatString="{0:c}"/>
	</Columns>
</asp:DataGrid>

</form>

</td></tr>

<tr><td>
<asp:Label id=LFooter runat=server/>
</td></tr>

</table>
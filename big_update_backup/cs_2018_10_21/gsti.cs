<!-- #include file="page_index.cs" -->
<script runat=server>

string m_branchID = "1";
string m_type = "";

double m_dTotal = 0;
double m_dTotalGst = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_sdFrom = "";
string m_sdTo = "";

string from_d = "";
string from_m = "";
string to_d = "";
string to_m = "";
string m_sCurrencyName="NZD";
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
    m_sCurrencyName=GetSiteSettings("default_currency_name", "NZD");
	m_type = Request.QueryString["t"];

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}

	PrintAdminHeader();
	PrintAdminMenu();

	//if(Request.Form["day_from"] != null)
	if(Request.Form["Datepicker1_day"] != null)
	{
		//from_d = Request.Form["day_from"];
		//from_m = Request.Form["month_from"];
		//to_d = Request.Form["day_to"];
		//to_m = Request.Form["month_to"];
		from_d = Request.Form["Datepicker1_day"];
		from_m = Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		to_d = Request.Form["Datepicker2_day"];
		to_m = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"]; 
	}
	else
	{
		//from_d = "1";
		from_d = DateTime.Now.ToString("dd");
		from_m = DateTime.Now.ToString("MM-yyyy");
		to_d = DateTime.Now.ToString("dd");
		to_m = DateTime.Now.ToString("MM-yyyy");
	}

//	Session["report_date_from_d"] = from_d;
//	Session["report_date_from_m"] = from_m;
//	Session["report_date_to_d"] = to_d;
//	Session["report_date_to_m"] = to_m;

	m_sdFrom = from_d + "-" + from_m;
	m_sdTo = to_d + "-" + to_m;

	if(!DoSearch())
		return;
	
	BindGrid();
//	LFooter.Text = m_sAdminFooter;
	PrintAdminFooter();
}

void PrintDatePicker()
{
	int i = 1;

	datePicker(); //call date picker function from common.cs
	//from date
	/*
	Response.Write("<b>Select : </b> From Date ");
	Response.Write("<select name=day_from>");
	for(; i<=31; i++)
	{
		Response.Write("<option value=" + i);
		if(MyIntParse(from_d) == i)
			Response.Write(" selected");
		Response.Write(">" + i + "</option>");
	}
	Response.Write("</select>");

	Response.Write("&nbsp&nbsp; <select name=month_from>");
	DateTime dstep = DateTime.Parse("01-01-2003");
	DateTime dend = DateTime.Now;
	while((dstep - dend).Days <= 0)
	{
		string value = dstep.ToString("MM-yyyy");
		string name = dstep.ToString("MMM yyyy");
		Response.Write("<option value='" + value + "' ");
		if(from_m == value)
			Response.Write(" selected");
		Response.Write(">" + name + "</option>");
		dstep = dstep.AddMonths(1);
	}
	Response.Write("</select>");
	
	//to date
	Response.Write("&nbsp;To &nbsp; ");
	Response.Write("<select name=day_to>");
	for(i=1; i<=31; i++)
	{
		Response.Write("<option value=" + i);
		if(MyIntParse(to_d) == i)
			Response.Write(" selected");
		Response.Write(">" + i + "</option>");
	}
	Response.Write("</select>");

	Response.Write("&nbsp&nbsp; <select name=month_to>");
	dstep = DateTime.Parse("01-01-2003");
	while((dstep - dend).Days <= 0)
	{
		string value = dstep.ToString("MM-yyyy");
		string name = dstep.ToString("MMM yyyy");
		Response.Write("<option value='" + value + "' ");
		if(to_m == value)
			Response.Write(" selected");
		Response.Write(">" + name + "</option>");
		dstep = dstep.AddMonths(1);
	}
	Response.Write("</select> ");
	*/

	//Response.Write("<form name=f action=report.aspx method=post>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	string s_day = "";
	string s_month = "";
	string s_year = "";
	string s_day1 = "";
	string s_month1 = "";
	string s_year1 = "";
	if(Request.Form["Datepicker1_day"] != null && Request.Form["Datepicker1_day"] != "")
		s_day = Request.Form["Datepicker1_day"].ToString();
	else
		s_day = DateTime.Now.ToString("dd");
		//s_day = "1";
	
	if(Request.Form["Datepicker1_month"] != null && Request.Form["Datepicker1_month"] != "")
		s_month = Request.Form["Datepicker1_month"].ToString();
	else
		s_month = DateTime.Now.ToString("MM");
	if(Request.Form["Datepicker1_year"] != null && Request.Form["Datepicker1_year"] != "")
		s_year = Request.Form["Datepicker1_year"].ToString();
	else
		s_year = DateTime.Now.ToString("yyyy");
	if(Request.Form["Datepicker2_day"] != null && Request.Form["Datepicker2_day"] != "")
		s_day1 = Request.Form["Datepicker2_day"].ToString();
	else
		s_day1 = DateTime.Now.ToString("dd");
	if(Request.Form["Datepicker2_month"] != null && Request.Form["Datepicker2_month"] != "")
		s_month1 = Request.Form["Datepicker2_month"].ToString();
	else
		s_month1 = DateTime.Now.ToString("MM");
	if(Request.Form["Datepicker2_year"] != null && Request.Form["Datepicker2_year"] != "")
		s_year1 = Request.Form["Datepicker2_year"].ToString();
	else
		s_year1 = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<b>Branch : </b>");
		PrintBranchNameOptions(m_branchID);
	}

	Response.Write(" &nbsp; <b>Date : </b> From ");
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

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
	Response.Write("<select name='Datepicker1_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
	for(int y=2000; y<int.Parse(DateTime.Now.ToString("yyyy"))+1; y++)
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
	Response.Write("<td> &nbsp; To ");
	Response.Write("<select name='Datepicker2_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day1) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

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
		if(int.Parse(s_month1) == m)
			Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
		else
			Response.Write("<option value="+m+">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int y=2000; y<int.Parse(DateTime.Now.ToString("yyyy"))+1; y++)
	{
		if(int.Parse(s_year1) == y)
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
	//Response.Write("</td></tr>");
		
//------ END second display date -----------
	Response.Write("<td align=right><input type=submit name=cmd value='Query Total' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Show All Invoices' " + Session["button_style"] + ">");
	Response.Write(" <input type=checkbox name=audit_payment>Audit Payment");
	
	//Response.Write(" &nbsp;<input type=button name=cmd value='Show All Invoices' " + Session["button_style"] + "");
	//Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?t="+ m_type +"&to="+m_sdTo+"&fr="+m_sdFrom+"')\" >");
	Response.Write("</td></tr>");

}

Boolean DoSearch()
{
	string sales_type_online = GetEnumID("sales_type", "online");
	ds.Clear();
/*	if(Request.QueryString["to"] != null && Request.QueryString["to"] != "")
	{
		m_sdFrom = Request.QueryString["fr"];
		m_sdTo = Request.QueryString["to"];
	}
*/		
	string sc = "SET DATEFORMAT dmy ";
	if(m_type == "e") //expense
	{
		sc += " SELECT '' as sales, '' AS agent, p.tax_date, p.id, p.po_number AS invoice_number, c.trading_name, c.gst_rate, p.exchange_rate ";
		sc += ", p.date_received AS commit_date, p.total / p.exchange_rate AS total, p.tax / p.exchange_rate AS tax, p.total_amount / p.exchange_rate AS total_amount ";
		sc += " FROM purchase p LEFT OUTER JOIN card c ON c.id=p.supplier_id ";
		sc += " WHERE p.tax_date>='" + m_sdFrom + "' AND p.tax_date<='" + m_sdTo + " 23:59" + "' ";
		sc += " AND p.date_received IS NOT NULL ";
		if(Session["branch_support"] != null)
			sc += " AND p.branch_id = " + m_branchID;
		//sc += " ORDER BY p.po_number";
		sc += " ORDER BY p.tax_date";
	}
	else
	{
		sc += " SELECT i.sales, c2.name AS agent, i.invoice_number, c.trading_name, c.gst_rate, i.commit_date, i.total, i.tax, i.cust_ponumber ";
		sc += " FROM invoice i LEFT OUTER JOIN card c ON c.id=i.card_id ";
		sc += " LEFT OUTER JOIN card c2 ON c2.id = i.agent ";
		sc += " WHERE i.commit_date>='" + m_sdFrom + "' AND i.commit_date<='" + m_sdTo + " 23:59" + "' ";
	//	sc += " AND paid=1 AND refunded=0 AND sales_type=" + sales_type_online;
		if(Session["branch_support"] != null)
			sc += " AND i.branch = " + m_branchID;
		//sc += " ORDER BY i.invoice_number";
		sc += " ORDER BY i.commit_date ";

	}
//DEBUG("sc=", sc);

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool getOtherGSTExp()
{
	//if(Request.Form["cmd"] == "Show All Invoices")
	//{
	//	m_sdFrom = Request.QueryString["fr"];
	//	m_sdTo = Request.QueryString["to"];
	//}
	string sc = " SET DATEFORMAT dmy ";
//	sc += " SELECT ei.id, ei.invoice_number, e.payment_date AS invoice_date, ei.tax, ei.total, c.name AS customer, c.trading_name, c.company AS Expr3, c1.name AS accountant ";
	sc += " SELECT ei.id, ei.invoice_number, ei.invoice_date, ei.tax, ei.total, c.name AS customer, c.trading_name, c.company AS Expr3, c1.name AS accountant ";
	sc += ", a.name4 AS expense_type, e.total AS total_amount ";
	sc += " FROM expense e LEFT OUTER JOIN ";
    sc += " expense_item ei ON ei.id = e.id LEFT OUTER JOIN ";
    sc += " card c ON c.id = e.card_id LEFT OUTER JOIN ";
	sc += " card c1 ON c1.id = e.recorded_by INNER JOIN ";
	sc += " account a ON a.id = e.to_account ";
	sc += " WHERE ei.invoice_date >= '" + m_sdFrom +"' AND ei.invoice_date <= '" + m_sdTo +" 23:59" + "' ";
//	sc += " WHERE e.payment_date >= '" + m_sdFrom +"' AND e.payment_date <= '" + m_sdTo +" 23:59" + "' ";
	if(Session["branch_support"] != null)
		sc += " AND e.branch = " + m_branchID;
//	sc += " AND ei.tax > 0 ";
	sc += " ORDER BY ei.invoice_date ";
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "other_exp");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(!getCustTax())
		return false;
	
	if(!getAssetsTax())
		return false;

	return true;
}

bool getCustTax()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ct.id, '' AS invoice_number, ct.statement_date AS invoice_date ";
	sc += ", ct.total_gst AS tax, 0 AS total, c.name AS customer ";
	sc += ", c.trading_name, c.company AS Expr3, c1.name AS accountant ";
	sc += ", 'CustomTax' AS expense_type, ct.total_gst AS total_amount ";
	sc += " FROM custom_tax ct LEFT OUTER JOIN ";
    sc += " card c ON c.id = ct.payee LEFT OUTER JOIN ";
	sc += " card c1 ON c1.id = ct.recorded_by ";
	sc += " WHERE ct.statement_date >= '" + m_sdFrom +"' AND ct.statement_date <= '" + m_sdTo +" 23:59" + "' ";
	if(Session["branch_support"] != null)
		sc += " AND ct.branch = " + m_branchID;
	sc += " ORDER BY ct.statement_date ";
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "cust_tax");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool getAssetsTax()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT a.id, '' AS invoice_number, a.date_recorded AS invoice_date ";
	sc += ", a.tax, a.total AS total, c.name AS customer ";
	sc += ", c.trading_name, c.company AS Expr3, c1.name AS accountant ";
	sc += ", 'AssetsTax' AS expense_type, a.total AS total_amount ";
	sc += " FROM assets a LEFT OUTER JOIN ";
    sc += " card c ON c.id = a.card_id LEFT OUTER JOIN ";
	sc += " card c1 ON c1.id = a.recorded_by ";
	sc += " WHERE a.date_recorded >= '" + m_sdFrom +"' AND a.date_recorded <= '" + m_sdTo +" 23:59" + "' ";
	if(Session["branch_support"] != null)
		sc += " AND a.branch = " + m_branchID;
	sc += " ORDER BY a.date_recorded ";
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "assets_tax");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 25;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["invoice"] != null)
		rows = ds.Tables["invoice"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<form action=gsti.aspx?t=" + m_type + " method=post>");
	if(m_type == "e") //expense
	{
		Response.Write("<br><center><h3>GST Detail - Expense"+"("+m_sCurrencyName+")"+"</h3></center>");
	}
	else
	{
		Response.Write("<br><center><h3>GST Detail - Income</h3></center>");
	}

	bool bAuditPayment = false;
	if(Request.Form["audit_payment"] == "on")
		bAuditPayment = true;

	Response.Write("<center>");
	Response.Write("<b>GST Date From : </b><font color=green> ");
	Response.Write(m_sdFrom);
	Response.Write(" </font>&nbsp&nbsp; to &nbsp&nbsp; <font color=red>");
	Response.Write(m_sdTo + "</font>&nbsp&nbsp&nbsp&nbsp;");

	PrintDatePicker();
	Response.Write("</form>");

	Response.Write("<table width=98%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	//Response.Write("<tr><td colspan=2>" + sPageIndex + "</td></tr>");

	if(m_type == "e") //expense
	{
		//query other gst 
		if(!getOtherGSTExp())
			return;

		Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
		//Response.Write("<th>Inv.Date</th>");
		Response.Write("<th>TAX_DATE</th>");
		Response.Write("<th>P.O.No.</th>");
		Response.Write("<th>SUPPLIER</th>");
		Response.Write("<th>Ex. RATE</th>");
//		Response.Write("<th>GST_RATE</th>");
		Response.Write("<th>AMOUNT</th>");
		Response.Write("<th>GST</th>");
		Response.Write("<th>AMOUNT(GST 0.00%)</th>");
		if(bAuditPayment)
			Response.Write("<th>PAYMENT AUDIT</th>");
		Response.Write("</tr>");
	}
	else
	{
		Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
		Response.Write("<th>INVOICE_DATE</th>");
		Response.Write("<th>INVOICE#</th>");
		
		Response.Write("<th>CUSTOMER</th>");
		Response.Write("<th>SALES PERSON</th>");
		Response.Write("<th align=left>AGENT</th>");
		Response.Write("<th>P.O.No.</th>");
//		Response.Write("<th>GST_RATE</th>");
		Response.Write("<th align=right>INV.AMOUNT(inc.GST)</th>");
		Response.Write("<th align=right>GST</th>");
		Response.Write("<th align=right>INV.AMOUNT(GST 0.00%)</th>");
		if(bAuditPayment)
			Response.Write("<th>PAYMENT AUDIT</th>");
		Response.Write("</tr>");
	}

	if(m_type == "e")
	{
		if(rows <= 0 && ds.Tables["other_exp"].Rows.Count <=0 && ds.Tables["cust_tax"].Rows.Count <= 0)
		{
			Response.Write("</table>");
			return;
		}
	}
	else
	{
		if(rows <= 0)
		{
			Response.Write("</table>");
			return;
		}
	}
	DataRow dr;
	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	//for(; i < rows && i < end; i++)
	for(; i < rows; i++)
	{
		dr = ds.Tables["invoice"].Rows[i];
		string date = "";
		if(m_type == "e")
			date = dr["tax_date"].ToString();
		else
			date = dr["commit_date"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string po_id = "";
		if(m_type == "e")
			po_id = dr["id"].ToString();
		string customer = dr["trading_name"].ToString();
		string customer_ponumber = "";
		if(m_type != "e")
			customer_ponumber = dr["cust_ponumber"].ToString();
		string total = dr["total"].ToString();
		string tax = dr["tax"].ToString();
		string gst_rate = dr["gst_rate"].ToString();
		string ex_rate = "";//dr["exchange_rate"].ToString();
		string agent = dr["agent"].ToString();
		string sales = dr["sales"].ToString();

		if(m_type == "e")
			ex_rate = dr["exchange_rate"].ToString();
		double dExRate = MyDoubleParse(ex_rate);

		DateTime dd = DateTime.Parse(date);
		//DEBUG("tostal = ", total);
		double dTotal = 0;
		if(total != "")
			dTotal = MyMoneyParse(total);
		//double dTotal = MyMoneyParse(total);
		if(m_type == "e")
			dTotal = MyMoneyParse(dr["total_amount"].ToString());
		double dTax = 0;
		if(tax != "")
			dTax = MyMoneyParse(tax);
		//double dTax = MyMoneyParse(tax);
	
		dTotal = Math.Round(dTotal, 2);
		dTax = Math.Round(dTax, 2);
		//if(Request.QueryString["to"] != null && Request.QueryString["to"] != "")
		double dRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;						//30.06.2003 XW
		if(gst_rate != "")
			dRate = MyDoubleParse(gst_rate);
		if(dRate == 0)
			dTotalNoGST += dTotal;
		else
		{
			dTotalWithGST += dTotal;
			dTotalTax += dTax;
		}
		if(Request.Form["cmd"] == "Show All Invoices")
		{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + dd.ToString("dd-MM-yyyy") + "</td>");
		if(m_type == "e")
			Response.Write("<td><a href=purchase.aspx?t=pp&n=" + dr["id"].ToString() + "&r=" + DateTime.Now.ToOADate() + " target=_blank class=o>" + invoice_number + "</a></td>");
		else
			Response.Write("<td><a href=invoice.aspx?" + invoice_number + "&r=" + DateTime.Now.ToOADate() + " target=_blank class=o>" + invoice_number + "</a></td>");
		Response.Write("<td>" + customer + "</td>");
			
		if(m_type != "e") //expense
		{
			Response.Write("<td>" + sales + "</td>");
			Response.Write("<td>" + agent + "</td>");
			Response.Write("<td>" + customer_ponumber + "</td>");
		}
//		double dRate = 0.125;
		
		if(m_type == "e")
			Response.Write("<td align=right>" + ex_rate + "</td>");

		if(dRate == 0)
		{
		//	dTotalNoGST += dTotal;
//gsitrate disable			Response.Write("<td align=right><font color=red><b>0.00%</b></font></td>");
			Response.Write("<td align=right>&nbsp;</td>");
			Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
			Response.Write("<td align=right><font color=red><b>" + dTotal.ToString("c") + "</b></font></td>");
		}
		else
		{
			//dTotalWithGST += dTotal;
			//dTotalTax += dTax;
//gsitrate disable			Response.Write("<td align=right>" + gst_rate + "</td>");
			Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
			Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
			Response.Write("<td align=right>&nbsp;</td>");
		}
		if(bAuditPayment)
		{
			Response.Write("<td align=right nowrap>" + AuditPayment(invoice_number, po_id, Math.Round(dTotal, 2)) + "</td>");
		}
			Response.Write("</tr>");
		}
	}
	if(m_type == "e") //expense
		Response.Write("<tr><td colspan=5><b><font color=black>TOTAL PURCHASES: <b></td><th align=right>"+dTotalWithGST.ToString("c")+"</td><th align=right>"+ dTotalTax.ToString("c") +"</td><th align=right>"+dTotalNoGST.ToString("c")+"</td> </tr>");
	
	Response.Write("<tr></tr>");
	double dtotal_exp = 0;
	double dtotal_exp_gst = 0;
	double dtotal_exp_no_gst = 0;
	double dtotal_cust = 0; //custom gst
	double dtotal_cust_gst = 0;
	double dtotal_cust_no_gst = 0;
	double dtotal_assets = 0; //custom gst
	double dtotal_assets_gst = 0;
	double dtotal_assets_no_gst = 0;

	if(m_type == "e") //expense
	{
			//for other expense type
		if(ds.Tables["other_exp"].Rows.Count > 0)
		{
			if(m_sdTo != "")
			{
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr><td colspan=9><font color=green><b>OTHER EXPENSES</b></td></tr>");
				Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
				//Response.Write("<th>Tax Date</th>");
				Response.Write("<th>Inv.Date</th>");
				Response.Write("<th>Invoice#</th>");
				Response.Write("<th colspan=2>Expense Type:</th>");
				Response.Write("<th>Pay To:</th>");
				//Response.Write("<th>Pay Type:</th>");
				Response.Write("<th>Amount</th>");
				Response.Write("<th>GST</th>");
				Response.Write("<th>Amount(GST 0.00%)</th>");
				if(bAuditPayment)
					Response.Write("<td align=right nowrap>&nbsp;</td>");
					
				Response.Write("</tr>");
			}
			
			for(int j=0; j<ds.Tables["other_exp"].Rows.Count; j++)
			{
				dr = ds.Tables["other_exp"].Rows[j];
				string inv_date = "";
				
				if(m_type == "e")
					inv_date = dr["invoice_date"].ToString();
				string inv_number = dr["invoice_number"].ToString();
				string payto = dr["customer"].ToString();
				string amount = dr["total"].ToString();
				string gst = dr["tax"].ToString();
				string expense_type = dr["expense_type"].ToString();			
				string ep_id = dr["id"].ToString();
				DateTime dd = DateTime.Now;
				if(inv_date != "")
					dd = DateTime.Parse(inv_date);
				double dTotal = 0;
				if(amount != "")
					dTotal = MyMoneyParse(amount);
				//if(m_type == "e")
				//	dTotal = MyMoneyParse(dr["total_amount"].ToString());
				double dTax = 0;
				if(gst != "")
					dTax = MyMoneyParse(gst);
				dTotal = Math.Round(dTotal, 2);
				dTax = Math.Round(dTax, 2);
				double dRate = 0.125;
				string gst_rate = "0.125";
				
				if(gst_rate != "")
					dRate = MyDoubleParse(gst_rate);
				if(dTax == 0)
				{
					dTotalNoGST += dTotal;
					dtotal_exp_no_gst += dTotal;
				}
				else
				{
					dTotalWithGST += dTotal;
					dTotalTax += dTax;
					dtotal_exp += dTotal;
					dtotal_exp_gst += dTax;
				}
				if(m_sdTo != "")
				{
					if(Request.Form["cmd"] == "Show All Invoices")
					{
					Response.Write("<tr");
					if(bAlterColor)
						Response.Write(" bgcolor=#EEEEEE");
					bAlterColor = !bAlterColor;
					Response.Write(">");
					Response.Write("<td>" + dd.ToString("dd-MM-yyyy") + "</td>");
					if(m_type == "e")
						Response.Write("<td><a href=expense.aspx?id="+ ep_id +"&r=" + DateTime.Now.ToOADate() + " target=_blank class=o>" + inv_number + "</a></td>");
					Response.Write("<td colspan=2>"+ expense_type +"</td>");
					Response.Write("<td>" + payto + "</td>");
					
					if(dTax == 0)
					{
//						Response.Write("<td align=right><font color=red><b>0.00%</b></font></td>");
						Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
//						Response.Write("<td align=right>&nbsp;</td>");
						Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
						Response.Write("<td align=right><font color=red><b>" + dTotal.ToString("c") + "</b></font></td>");
					}
					else
					{
						
						Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
						Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
						Response.Write("<td align=right> &nbsp; </td>");
//						Response.Write("<td align=right>&nbsp;</td>");
					}
					if(bAuditPayment)
						Response.Write("<td align=right nowrap>&nbsp;</td>");
					
					Response.Write("</tr>");
					}
				}
			}
		}

		if(m_type == "e")
		{
			Response.Write("<tr><td><b>TOTAL OTHER EXPENSES: </b></td>");
			Response.Write("<td colspan=5 align=right><b>" + dtotal_exp.ToString("c") + "</b></td>");
			Response.Write("<td align=right><b>" + dtotal_exp_gst.ToString("c") + "</b></td>");
			Response.Write("<td align=right><b>" + dtotal_exp_no_gst.ToString("c") + "</b></td>");
		}

		//for customer gst type
		if(ds.Tables["cust_tax"].Rows.Count > 0)
		{
			if(m_sdTo != "")
			{
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr><td colspan=9><font color=green><b>CUSTOM GST</b></td></tr>");
				Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
				//Response.Write("<th>Tax Date</th>");
				Response.Write("<th>Inv.Date</th>");
				Response.Write("<th>Invoice#</th>");
				Response.Write("<th colspan=2>Expense Type:</th>");
				Response.Write("<th>Pay To:</th>");
				//Response.Write("<th>Pay Type:</th>");
				Response.Write("<th>Amount</th>");
				Response.Write("<th>GST</th>");
				Response.Write("<th>Amount(GST 0.00%)</th>");
				if(bAuditPayment)
					Response.Write("<td align=right nowrap>&nbsp;</td>");
					
				Response.Write("</tr>");
			}
			
			for(int j=0; j<ds.Tables["cust_tax"].Rows.Count; j++)
			{
				dr = ds.Tables["cust_tax"].Rows[j];
				string inv_date = "";
				
				if(m_type == "e")
					inv_date = dr["invoice_date"].ToString();
				string inv_number = dr["invoice_number"].ToString();
				string payto = dr["customer"].ToString();
				string amount = dr["total"].ToString();
				string gst = dr["tax"].ToString();
				string expense_type = dr["expense_type"].ToString();			
				string ep_id = dr["id"].ToString();
				DateTime dd = DateTime.Now;
				if(inv_date != "")
					dd = DateTime.Parse(inv_date);
				double dTotal = 0;
				if(amount != "")
					dTotal = MyMoneyParse(amount);
				//if(m_type == "e")
				//	dTotal = MyMoneyParse(dr["total_amount"].ToString());
				double dTax = 0;
				if(gst != "")
					dTax = MyMoneyParse(gst);
				dTotal = Math.Round(dTotal, 2);
				dTax = Math.Round(dTax, 2);
				double dRate = 0.125;
				string gst_rate = "0.125";
				
				if(gst_rate != "")
					dRate = MyDoubleParse(gst_rate);
				if(dTax == 0)
				{
					dTotalNoGST += dTotal;
					dtotal_cust_no_gst += dTotal;
				}
				else
				{
					dTotalWithGST += dTotal;
					dTotalTax += dTax;
					dtotal_cust += dTotal;
					dtotal_cust_gst += dTax;
				}
				if(m_sdTo != "")
				{
					if(Request.Form["cmd"] == "Show All Invoices")
					{
					Response.Write("<tr");
					if(bAlterColor)
						Response.Write(" bgcolor=#EEEEEE");
					bAlterColor = !bAlterColor;
					Response.Write(">");
					Response.Write("<td>" + dd.ToString("dd-MM-yyyy") + "</td>");
					if(m_type == "e")
						Response.Write("<td><a href=expense.aspx?id="+ ep_id +"&r=" + DateTime.Now.ToOADate() + " target=_blank class=o>" + inv_number + "</a></td>");
					Response.Write("<td colspan=2>"+ expense_type +"</td>");
					Response.Write("<td>" + payto + "</td>");
					
					if(dTax == 0)
					{
//						Response.Write("<td align=right><font color=red><b>0.00%</b></font></td>");
						Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
//						Response.Write("<td align=right>&nbsp;</td>");
						Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
						Response.Write("<td align=right><font color=red><b>" + dTotal.ToString("c") + "</b></font></td>");
					}
					else
					{
						
						Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
						Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
						Response.Write("<td align=right> &nbsp; </td>");
//						Response.Write("<td align=right>&nbsp;</td>");
					}
					if(bAuditPayment)
						Response.Write("<td align=right nowrap>&nbsp;</td>");
					
					Response.Write("</tr>");
					}
				}
			}
		}
		if(m_type == "e")
		{
			Response.Write("<tr><td><b>TOTAL CUSTOM GST: </b></td>");
			Response.Write("<td colspan=5 align=right><b>" + dtotal_cust.ToString("c") + "</b></td>");
			Response.Write("<td align=right><b>" + dtotal_cust_gst.ToString("c") + "</b></td>");
			Response.Write("<td align=right><b>" + dtotal_cust_no_gst.ToString("c") + "</b></td>");
		}

		//for assets gst type
		if(ds.Tables["assets_tax"].Rows.Count > 0)
		{
			if(m_sdTo != "")
			{
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr><td colspan=9><font color=green><b>ASSETS GST</b></td></tr>");
				Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
				//Response.Write("<th>Tax Date</th>");
				Response.Write("<th>DateRecorded</th>");
				Response.Write("<th>Description</th>");
				Response.Write("<th colspan=2>Expense Type:</th>");
				Response.Write("<th>Pay To:</th>");
				//Response.Write("<th>Pay Type:</th>");
				Response.Write("<th>Amount</th>");
				Response.Write("<th>GST</th>");
				Response.Write("<th>Amount(GST 0.00%)</th>");
				if(bAuditPayment)
					Response.Write("<td align=right nowrap>&nbsp;</td>");
					
				Response.Write("</tr>");
			}
			
			for(int j=0; j<ds.Tables["assets_tax"].Rows.Count; j++)
			{
				dr = ds.Tables["assets_tax"].Rows[j];
				string assets_id = dr["id"].ToString();
				string names = GetAssetsItemNames(assets_id);
				string inv_date = "";
				
				if(m_type == "e")
					inv_date = dr["invoice_date"].ToString();
				string inv_number = dr["invoice_number"].ToString();
				string payto = dr["customer"].ToString();
				string amount = dr["total"].ToString();
				string gst = dr["tax"].ToString();
				string expense_type = dr["expense_type"].ToString();			
				DateTime dd = DateTime.Now;
				if(inv_date != "")
					dd = DateTime.Parse(inv_date);
				double dTotal = 0;
				if(amount != "")
					dTotal = MyMoneyParse(amount);
				//if(m_type == "e")
				//	dTotal = MyMoneyParse(dr["total_amount"].ToString());
				double dTax = 0;
				if(gst != "")
					dTax = MyMoneyParse(gst);
				dTotal = Math.Round(dTotal, 2);
				dTax = Math.Round(dTax, 2);
				double dRate = 0.125;
				string gst_rate = "0.125";
				
				if(gst_rate != "")
					dRate = MyDoubleParse(gst_rate);
				if(dTax == 0)
				{
					dTotalNoGST += dTotal;
					dtotal_assets_no_gst += dTotal;
				}
				else
				{
					dTotalWithGST += dTotal;
					dTotalTax += dTax;
					dtotal_assets += dTotal;
					dtotal_assets_gst += dTax;
				}
				if(m_sdTo != "")
				{
					if(Request.Form["cmd"] == "Show All Invoices")
					{
					Response.Write("<tr");
					if(bAlterColor)
						Response.Write(" bgcolor=#EEEEEE");
					bAlterColor = !bAlterColor;
					Response.Write(">");
					Response.Write("<td>" + dd.ToString("dd-MM-yyyy") + "</td>");
					if(m_type == "e")
						Response.Write("<td><a href=assets.aspx?id=" + assets_id + " target=_blank class=o>" + names + "</a></td>");
					Response.Write("<td colspan=2>"+ expense_type +"</td>");
					Response.Write("<td>" + payto + "</td>");
					
					if(dTax == 0)
					{
//						Response.Write("<td align=right><font color=red><b>0.00%</b></font></td>");
						Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
//						Response.Write("<td align=right>&nbsp;</td>");
						Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
						Response.Write("<td align=right><font color=red><b>" + dTotal.ToString("c") + "</b></font></td>");
					}
					else
					{
						
						Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
						Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
						Response.Write("<td align=right> &nbsp; </td>");
//						Response.Write("<td align=right>&nbsp;</td>");
					}
					if(bAuditPayment)
						Response.Write("<td align=right nowrap>&nbsp;</td>");
					
					Response.Write("</tr>");
					}
				}
			}
		}
		if(m_type == "e")
		{
			Response.Write("<tr><td><b>TOTAL ASSETS GST: </b></td>");
			Response.Write("<td colspan=5 align=right><b>" + dtotal_assets.ToString("c") + "</b></td>");
			Response.Write("<td align=right><b>" + dtotal_assets_gst.ToString("c") + "</b></td>");
			Response.Write("<td align=right><b>" + dtotal_assets_no_gst.ToString("c") + "</b></td>");
		}
	}

	Response.Write("<tr><td ");
	if(m_type != "e")
		Response.Write(" colspan=2 ");
	Response.Write("><b>");
	
	Response.Write(" SUB TOTAL: ");
	Response.Write("</b></td>");
	
	if(m_type == "e")
		Response.Write("<td colspan=4 align=right><b>" + dTotalWithGST.ToString("c") + "</b></td>");
	else
		Response.Write("<td colspan=5 align=right><b>" + dTotalWithGST.ToString("c") + "</b></td>");

	Response.Write("<td align=right><b>" + dTotalTax.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dTotalNoGST.ToString("c") + "</b></td>");
	if(bAuditPayment)
		Response.Write("<td></td>");

	Response.Write("</tr>");
	
	Response.Write("</table>");

	Response.Write("<br>");

	Response.Write("<b>GST Date From : </b><font color=green> ");
	Response.Write(m_sdFrom);
	Response.Write(" </font>&nbsp&nbsp; to &nbsp&nbsp; <font color=red>");
	Response.Write(m_sdTo + "</font>&nbsp&nbsp&nbsp&nbsp;");

	Response.Write("</b></center>");
}

string AuditPayment(string invoice_number, string po_id, double dTotal)
{
	DataSet dsa = new DataSet();
	string sc = " SET DATEFORMAT dmy ";

	if(po_id != "") //purchase
	{
		sc += " SELECT tran_id, amount_applied FROM tran_invoice WHERE purchase=1 AND invoice_number = " + po_id;
	}
	else
	{
		sc += " SELECT tran_id, amount_applied FROM tran_invoice WHERE purchase=0 AND invoice_number = " + invoice_number;
	}
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsa);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "System Error";
	}

	string sRet = "";
	double dPaid = 0;
	string trace = "";
	for(int i=0; i<dsa.Tables[0].Rows.Count; i++)
	{
		string tran_id = dsa.Tables[0].Rows[i]["tran_id"].ToString();
		trace += " <a href=payhistory.aspx?t=p&id=" + tran_id + " class=o target=_blank>" + tran_id + "</a>";
		double dAmount = MyDoubleParse(dsa.Tables[0].Rows[i]["amount_applied"].ToString());
		dPaid += dAmount;
	}
	double dBalance = Math.Round(dPaid - dTotal, 2);
	if(dBalance == 0)
		sRet += "<font color=green><b>No Problem Found</b></font>";
	else if(dPaid == 0)
		sRet += "<b><i>(Unpaid)</i></b>";
	else
		sRet += "<font color=red><b>Amount Paid : </b></font><b>" + dPaid.ToString("c") + "</b>";

	if(trace != "")
		sRet += " Trace : " + trace;
//DEBUG("balance=", dBalance.ToString());
	return sRet;
}

string GetAssetsItemNames(string assets_id)
{
	if(ds.Tables["item_names"] != null)
		ds.Tables["item_names"].Clear();

	int nRows = 0;
	string names = "";
	string sc = " SELECT name FROM assets_item WHERE id = " + assets_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "item_names");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	for(int i=0; i<nRows; i++)
	{
		names += ds.Tables["item_names"].Rows[i]["name"].ToString();
		if(i < nRows - 1)
			names += ",";
	}
	return names;
}

</script>

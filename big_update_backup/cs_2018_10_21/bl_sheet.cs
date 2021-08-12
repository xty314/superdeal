<script runat=server>

string m_type = "0";
string m_tableTitle = "BALANCE SHEET";
string m_datePeriod = "";
string m_dateSql1 = "";
string m_dateSql2 = "";
string m_dateSql3 = "";
string m_dateSql4 = "";
string m_dateSql5 = "";
string m_dateSql6 = "";
string m_dateSql7 = "";
string m_dateSql8 = "";
string m_dateSql9 = "";
string m_code = "";

DataRow[] m_dra = null;
string m_smFrom = "";
string m_smTo = "";
string m_sdFrom = "";
string m_sdTo = "";
string m_pickdate = "";

int m_nPeriod = 0;

bool m_bPickTime = false;
bool m_bShowPic = true;

string m_ExpenseType = "";
string m_AccountID = "";
DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string[] m_EachMonth = new string[13];
string m_salesID = "0";
string m_salesName = "";
string m_inv = "";

bool m_bCompair = false;
bool m_bAuto = false;
bool m_bBranchSupport = false;
bool m_bDebug = false;

double  m_dEquity = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["debug"] == "1")
		m_bDebug = true;
	else if(Request.Form["debug"] == "on")
		m_bDebug = true;

	if(Request.QueryString["autolist"] == "1")
    {
      	m_bAuto = true;
      	PrintAdminHeader();
      	PrintAdminMenu();
      	DoAccountList();
      	PrintAdminFooter();
      	return;
    }
	
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

	if(Request.QueryString["s"] != null)
		m_salesID = Request.QueryString["s"];
	else
		m_salesID = Session["card_id"].ToString();
	
	DataRow dr = GetCardData(m_salesID);
	m_salesName = dr["name"].ToString();
	
	
	if(Request.Form["cmd"] == "Update" || Request.Form["equity"] != null && Request.Form["equity"] != "")
	{
		string sc = " UPDATE settings SET value = '"+ Request.Form["equity"] +"'";
		sc += " WHERE name = 'company_equity' ";
	//DEBUG("sc =", sc);
	//return;
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return;
		}

		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	string sequity = GetSiteSettings("company_equity", "0");
	
	bool bIsValid = false;
//	sequity = "0.1";
	if(sequity != "")
	{
		if(TSIsDigit(sequity))
		{
			bIsValid = true;
			m_dEquity = MyDoubleParse(sequity);
		}
	}	
	
//	if(GetSiteSettings("company_equity", "0") == "0")
	if(!bIsValid || m_dEquity <=0)	
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<form name=frm method=post><br><center><h4>First Time to Process the Balance Sheet, Please Provide the Registered Balance</h4>");
		//Response.Write("<i>(you can change the registered equity again at site setting)</i> <br><br>");
		Response.Write("Company Registered Equity: <input type=text name=equity value='"+ sequity +"'>");
		Response.Write("<input type=submit name=cmd value='Update' "+ Session["button_style"] +" Onclick=\"window.alert('Correct Amount!!');\">");
		Response.Write("</form>");
		PrintAdminFooter();
		return;
	}

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_AccountID = Request.QueryString["id"].ToString();
		m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
		m_datePeriod = "Current Month";
		DoAccountList();
		
		return;
	}

	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["type"] == null || Request.QueryString["type"] == "")
		{
			PrintMainPage();
			PrintAdminFooter();
			return;
		}
		if(Request.QueryString["np"] != null && Request.QueryString["np"] != "")
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
		
		m_code = Request.QueryString["code"];
		
	}

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
	if(Request.Form["account"] != null && Request.Form["account"] != "")
		m_AccountID = Request.Form["account"].ToString();

	if(Request.Form["Datepicker1_day"] != null)
	{
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	string sShow_date = DateTime.Now.ToString("dd-MMM-yyyy");
	if(m_nPeriod == 2)
	{
		string day = Request.Form["Datepicker3_day"];
		string monthYear = Request.Form["Datepicker3_month"] + "-" +Request.Form["Datepicker3_year"];
		m_sdFrom = day + "-" + monthYear;
		sShow_date = m_sdFrom;
		m_sdFrom = (DateTime.Parse(m_sdFrom)).ToString("yyyyMMdd");
	}

	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Till Now";
		break;
	case 1:
		m_datePeriod = "Till <font color=green>" + sShow_date + "</font>";
		break;
	default:
		break;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	
	DoAccountList();

	PrintAdminFooter();
}


void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h4>BALANCE SHEET</h4></center>");
	Response.Write("<form name=f action=bl_sheet.aspx?s=");
	if(m_salesID != "")
		Response.Write(m_salesID);
	if(m_bDebug)
		Response.Write("&debug=1");
	Response.Write(" method=post>");

	Response.Write("<table width=55% align=center border=1 cellspacing=1 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td aling=center><b><font size=2>SELECT DATE RANGE</font></b></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td><b>Date Range</b></td></tr>");

	Response.Write("<tr><td><input type=radio name=period value=0 checked>Till Now</td></tr>");
	Response.Write("<tr><td><input type=radio name=period value=1>");

	int i = 1;
	datePicker(); //call date picker function from common.cs
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");

//	Response.Write("<tr><td>");
	Response.Write("<b>Select : </b> Till Date &nbsp;&nbsp; ");
	Response.Write("<select name='Datepicker3_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker3',1);\">");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker3_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker3',1);\" style=''>");

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
	Response.Write("<select name='Datepicker3_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker3',1);\" style=''>");
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
	Response.Write("</td></tr>");

	Response.Write("<input type=hidden name='Datepicker3'>");
	Response.Write("<input type=hidden name='Datepicker3_conv'>");
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker3',1)");
	Response.Write("</script ");
	Response.Write(">");
	Response.Write("<tr><td align=right>");
	Response.Write("<input type=checkbox name=debug> Show Details ");
	Response.Write("<input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//
bool DoAccountList()
{
	string m_report_date = "deposit_date";

	switch(m_nPeriod)
	{
	case 0:
	
		m_dateSql1 = " AND deposit_date <= GETDATE() ";
		m_dateSql2 = " AND i.commit_date <= GETDATE() ";
		m_dateSql3 = " AND date_received <= GETDATE() ";
		m_dateSql4 = " AND date_recorded <= GETDATE() ";
		m_dateSql5 = " AND recorded_date <= GETDATE() ";
		m_dateSql6 = " AND t.trans_date <= "+ int.Parse(DateTime.Now.ToString("yyyyMMdd")) +"";
		m_dateSql7 = " AND payment_date <= GETDATE() ";
		m_dateSql8 = " AND i.log_time <= GETDATE() ";
		m_dateSql9 = " AND statement_date <= GETDATE() ";
		break;
	case 1:
		m_dateSql1 = " AND deposit_date <= '"+ m_sdFrom +"' ";
		m_dateSql2 = " AND i.commit_date <= '"+ m_sdFrom +"' ";
		m_dateSql3 = " AND date_received <= '"+ m_sdFrom +"' ";
		m_dateSql4 = " AND date_recorded <= '"+ m_sdFrom +"' ";
		m_dateSql5 = " AND recorded_date <= '"+ m_sdFrom +"' ";
		m_dateSql6 = " AND t.trans_date <= "+ m_sdFrom +"";
		//m_dateSql6 = " AND t.trans_date <= "+ DateTime.Parse(m_sdFrom).ToString("yyyyMMdd") +"";
		m_dateSql7 = " AND payment_date <= '"+ m_sdFrom +"' ";
		m_dateSql8 = " AND i.log_time <= '"+ m_sdFrom +"' ";
		m_dateSql9 = " AND statement_date <= '" + m_sdFrom + "' ";
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = "";
	sc = " SET DATEFORMAT dmy ";
//current assets - Bank Account

/*	sc = "SELECT sum(amount_applied) ";
	sc += ", ((SELECT ISNULL(SUM(ti.amount_applied),0) ";
	sc += " FROM tran_invoice ti JOIN trans t ON t.id = ti.tran_id ";
	sc += " WHERE 1=1 AND ti.purchase = 0 "+ m_dateSql6 +") ";
	sc += " - (SELECT ISNULL(SUM(ti.amount_applied),0) ";  // minus total purchase
	sc += " FROM tran_invoice ti JOIN trans t ON t.id = ti.tran_id ";
	sc += " WHERE 1=1 AND ti.purchase = 1 "+ m_dateSql6 +")";
//	sc += " - (SELECT ISNULL(sum(e.total), 0) ";    // minus total expense
//	sc += " FROM expense e WHERE 1=1 "+ m_dateSql7 +")";  
	sc += ") AS total ";
	sc += " FROM tran_invoice ";

//DEBUG("bank assetsc1=", sc);
*/	
	sc = " SELECT SUM(balance) AS total FROM account ";
	sc += " WHERE class1 = 1 AND class2 = 1 AND class3 = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "bank_assets");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//expense total
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(sum(e.total), 0) AS total_paid_expense ";    // minus total expense
	sc += " FROM expense e WHERE ispaid=1 "+ m_dateSql7 +""; 
//DEBUG(" sc expense =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "total_paid_expense");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

//current assets - Trade Detors
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(i.total-i.amount_paid),0) AS total FROM invoice i ";
	sc += " WHERE 1=1 ";
	sc += " AND i.paid = 0 ";
	sc += m_dateSql2;
//	DEBUG("sc2=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "debtor_assets");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//purchase gst
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(tax), 0) AS tax FROM purchase WHERE 1=1 ";
	sc += m_dateSql3;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "purchase_gst");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//custom GST
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(total_gst), 0) AS tax FROM custom_tax WHERE 1=1 ";
	sc += m_dateSql9;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "custom_gst");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//expense GST
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(tax), 0) AS tax FROM expense WHERE 1=1 ";
	sc += m_dateSql4; //use date_recorded
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "expense_gst");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//assets GST
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(tax), 0) AS tax FROM assets WHERE 1=1 ";
	sc += m_dateSql4; //use date_recorded
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "assets_gst");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//sales/invoice gst
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(i.tax), 0) AS tax FROM invoice i WHERE 1=1 ";
	sc += m_dateSql2;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "sales_gst");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

/*
//current assets - GST Receivable
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT (ISNULL(sum(tax),0) "; //- ";
	//sc += ") AS ttotal ";
	//sc += " - (SELECT ISNULL(sum(i.tax), 0) FROM invoice i WHERE 1=1 "+ m_dateSql2 +") AS s_gst ";
	sc += " - (SELECT ISNULL(sum(i.tax), 0) FROM invoice i WHERE 1=1 "+ m_dateSql2 +") ";
	//sc += " + (SELECT ISNULL(sum(total),0) FROM expense e JOIN account a ON a.id = e.to_account WHERE a.id=28 "+ m_dateSql7 +" ) AS total ";
	sc += " + (SELECT ISNULL(sum(total),0) FROM expense e JOIN account a ON a.id = e.to_account WHERE a.id=28 "+ m_dateSql7 +" ) ";
	sc += ") AS total ";
	sc += " FROM purchase  ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql3;
//DEBUG("gst=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "gst_receive");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
*/

//current assets - Stock On Hand
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT UPPER(b.name) as name, b.id AS branch_id ";
//	sc += " , (SELECT ISNULL(sum(p.total), 0) AS ptotal ";
	sc += " , (SELECT ISNULL(sum(pi.qty * pi.price), 0) AS ptotal ";
	sc += " FROM purchase p JOIN purchase_item pi ON pi.id = p.id AND p.branch_id = b.id ";
	sc += " JOIN code_relations c ON c.code = pi.code ";
//	sc += " FROM purchase p ";
	sc += " WHERE 1=1 "+m_dateSql3;
	sc += " AND c.cat <> 'ServiceItem' ";
	if(!m_bBranchSupport)
		sc += " AND b.id <= 1 "; //only get main branch
	sc += " ) AS ptotal ";

	sc += " , (SELECT ISNULL(sum(i.price - i.amount_paid),0) AS stotal FROM invoice i ";
	//sc += " , (SELECT ISNULL(sum(s.quantity * s.supplier_price),0) AS stotal FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number AND i.branch = b.id ";
//	sc += " WHERE i.paid = 1 "+ m_dateSql2;
	sc += " WHERE 1=1 "+ m_dateSql2;
	sc += "  AND i.branch = b.id ";
	sc += ") AS stotal ";
	//sc += " , (SELECT ISNULL(sum(s.quantity * s.commit_price),0) AS stotal FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number AND i.branch = b.id ";
	sc += " , (SELECT ISNULL(sum(c.cost * c.qty),0) AS sstotal ";
	sc += " FROM sales_cost c JOIN invoice i ON c.invoice_number = i.invoice_number AND i.branch = b.id ";
	sc += " JOIN code_relations cr ON c.code = c.code ";
//	sc += " WHERE i.paid = 1 "+ m_dateSql2;
	sc += " WHERE 1 = 1 "+ m_dateSql2;
	sc += " AND cr.cat <> 'ServiceItem' ";
	sc += ") AS sstotal ";
	sc += " FROM branch b WHERE 1=1 ";
	if(!m_bBranchSupport)
		sc += " AND b.id <= 1 ";
	sc += " ORDER BY b.id ";

//DEBUG("sc4stock=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "stock");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

//****************profit and loss  *****************
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";
	//income
	sc += " ( SELECT Isnull(SUM(i.price),0) FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number WHERE 1=1 " + m_dateSql2 + ") ";
	sc += " AS sales_income ";
	//freight
	sc += ", ( SELECT Isnull(SUM(i.freight),0) FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number WHERE 1=1 " + m_dateSql2 + ") ";
	sc += " AS freight ";
	//cost
	sc += ", ( SELECT Isnull(SUM(c.cost * c.qty),0) FROM sales_cost c JOIN invoice i ON c.invoice_number = i.invoice_number WHERE 1=1 " + m_dateSql2 + ") ";
	sc += " AS sales_cost ";
	//stock loss
	sc += ", ( SELECT Isnull(SUM(c.cost * c.qty),0) FROM stock_loss c JOIN stock_adj_log i ON c.id = i.id WHERE 1=1 " + m_dateSql8 + ") ";
	sc += " AS stock_loss ";
	//total expense
	sc += ", ( SELECT Isnull(SUM(total - tax),0) FROM expense WHERE 1=1 AND total <> 0 " + m_dateSql4 + ") ";
	sc += " AS total_expense ";
//DEBUG("stock loss=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "profitloss");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

//************end profit and loss *****************
//less sales assets 
/*	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(s.quantity * s.supplier_price),0) AS total, isnull(b.name, ''), Isnull(b.id, 1) AS branch ";
	sc += " FROM invoice i LEFT OUTER JOIN branch b ON b.id = i.branch ";
	sc += " JOIN sales s ON s.invoice_number = i.invoice_number ";
	sc += " LEFT OUTER JOIN sales_cost ss ON ss.code = s.code AND ss.invoice_number = s.invoice_number AND ss.invoice_number = i.invoice_number ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql2;
	sc += " GROUP BY b.name, b.id ";
	sc += " ORDER BY b.id ";
DEBUG("sc5=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "less_stock");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
*/

//////////////////////////////////////////////////////////////////////////////////////////
//Trade Creditors
	//puchase payable
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(total_amount - amount_paid),0) AS total FROM purchase  ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql3;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "purchase_payable");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//recorded expense
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(total),0) AS total FROM expense  ";
	sc += " WHERE ispaid = 0 ";
	sc += m_dateSql4; //use date_recorded
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "recorded_expense");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	//recorded assets
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(total - amount_paid),0) AS total FROM assets  ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql4; //use date_recorded
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "recorded_assets");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}


//current Liabilities - Trade Creditors
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(sum(i.amount_paid - i.total),0) AS total FROM invoice i ";
	//sc += " SELECT ISNULL(sum(total - amount_paid),0) AS total FROM invoice  ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql2;
	sc += " HAVING sum(i.amount_paid - i.total) > 0 ";
//DEBUG("sc6=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "creditors");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
//current Liabilities - Supplier Purchase
	sc = " SET DATEFORMAT dmy ";
//	sc += " SELECT ISNULL(sum(amount_paid - total_amount),0) AS total FROM purchase  ";
	sc += " SELECT ISNULL(sum(total_amount - amount_paid),0) AS total FROM purchase  ";
	sc += " WHERE 1=1 ";
//	sc += " AND date_received <> null AND date_received <> '' ";
	sc += m_dateSql3;
//DEBUG("sc7=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "supplier_credit");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
/*
//account GST payable
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(sum(tax),0) - ";
	sc += " (SELECT ISNULL(sum(total),0) FROM expense e JOIN account a ON a.id = e.to_account WHERE a.id=27 "+ m_dateSql7 +" ) AS total ";
	sc += " FROM invoice i ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql2;
//DEBUG("sc3=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "gst_payable");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
*/
//current Liabilities - Fixed Assets
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(total-tax),0) AS total FROM assets  ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql4;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "fix_assets");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

//Shareholder's current A/Cs
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(total),0) AS t ";
	sc += ", ((SELECT ISNULL(sum(total),0) AS t1 FROM acc_equity ae WHERE 1=1 AND type=2 "+ m_dateSql5 +") ";
	sc += " - (SELECT ISNULL(sum(total),0) AS t1 FROM acc_equity ae WHERE 1=1 AND type=1 "+ m_dateSql5 +")";
	sc +=") AS total ";
	sc += " FROM acc_equity  ";
	//sc += " WHERE 1=1 ";
	//sc += m_dateSql5;
//DEBUG("shard holder=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "shareholders");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
//equity
/*	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(sum(balance),0) AS total FROM account  ";
	sc += " WHERE 1=1 ";
	sc += " AND name1='equity' AND class2=1 AND class3=1 ";
	
DEBUG("sc10=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "equity");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
*/
	Response.Write("<br><center><h3><b>"+ m_sCompanyName.ToUpper() +"</b></h3>");
	Response.Write("<center><h3><b>BALANCE SHEET</b></h4>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
	
	BindExpenseList();
/*	if(m_bShowPic)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + "></center>");

	}
*/	
	return true;
}

/////////////////////////////////////////////////////////////////
void BindExpenseList()
{

	int nrows = 7;
	
	Response.Write("<table  width=35% align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

//Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");


	Response.Write("</tr>");
	Response.Write("<tr><td colspan="+ nrows +"><hr size=1></tr>");

	string gst = GetSiteSettings("gst_rate_percent", "0.125");
//DEBUG(" gst = ", gst);
	
	bool bAlterColor = false;
	double dtotal_cost = 0;
	double dtotal_loss = 0;
	double dtotal_freight = 0;
	double dtotal_sales = 0;
	double dtotal_expense = 0;
	double dtotal_sales_profit = 0;
	double dgross_profit = 0;
	if(ds.Tables["profitloss"].Rows.Count >= 1)
	{
		dtotal_sales = double.Parse(ds.Tables["profitloss"].Rows[0]["sales_income"].ToString());
		dtotal_freight = double.Parse(ds.Tables["profitloss"].Rows[0]["freight"].ToString());
		dtotal_loss = double.Parse(ds.Tables["profitloss"].Rows[0]["stock_loss"].ToString());
		dtotal_cost = double.Parse(ds.Tables["profitloss"].Rows[0]["sales_cost"].ToString());
		dtotal_expense = double.Parse(ds.Tables["profitloss"].Rows[0]["total_expense"].ToString());
//	DEBUG("dtotalcos t= ", dtotal_cost.ToString());

//	DEBUG("dtotalfrieght t= ", dtotal_freight.ToString());

		dgross_profit = (dtotal_sales + dtotal_freight) - dtotal_cost;
		dtotal_sales_profit = dgross_profit - dtotal_expense - dtotal_loss;
//	DEBUG("dtotalprofit= ", dtotal_sales_profit.ToString());
//	DEBUG("dtotallossst= ", dtotal_loss.ToString());
	}
	//currenty assets
	double dtotal_bank = 0;
	if(ds.Tables["bank_assets"].Rows.Count == 1)
		dtotal_bank = double.Parse(ds.Tables["bank_assets"].Rows[0]["total"].ToString());
//DEBUG("dtAotl_anbk =",dtotal_bank.ToString());
	double dtotal_debtor = 0;
	if(ds.Tables["debtor_assets"].Rows.Count == 1)
		dtotal_debtor = double.Parse(ds.Tables["debtor_assets"].Rows[0]["total"].ToString());

	//total gst receivable
//	double dtotal_gst = 0;
//	if(ds.Tables["gst_receive"].Rows.Count == 1)
//		dtotal_gst = double.Parse(ds.Tables["gst_receive"].Rows[0]["total"].ToString());

	//total gst receivable
	double dpurchase_tax = MyDoubleParse(ds.Tables["purchase_gst"].Rows[0]["tax"].ToString());
	double dcustom_tax = MyDoubleParse(ds.Tables["custom_gst"].Rows[0]["tax"].ToString());
	double dexpense_tax = MyDoubleParse(ds.Tables["expense_gst"].Rows[0]["tax"].ToString());
	double dassets_tax = MyDoubleParse(ds.Tables["assets_gst"].Rows[0]["tax"].ToString());
	double dsales_tax = MyDoubleParse(ds.Tables["sales_gst"].Rows[0]["tax"].ToString());
	double dtotal_gst = dpurchase_tax + dcustom_tax + dexpense_tax + dassets_tax - dsales_tax;

	//total gst payable
	double dtotal_gst_payable = 0;
//	dtotal_gst_payable = dtotal_debtor * (double.Parse(gst) / 100);
//	if(ds.Tables["gst_payable"].Rows.Count == 1)
//		dtotal_gst_payable = double.Parse(ds.Tables["gst_payable"].Rows[0]["total"].ToString());

	double dtotal_stock = 0;
	double dtotal_branch1 = 0;
	double dtotal_branch2 = 0;
	double dtotal_branch3 = 0;
	double dtotal_branch4 = 0;
	double dtotal_branch5 = 0;
	double dtotal_branch6 = 0;
	string name1 = "";
	string name2 = "";
	string name3 = "";
	string name4 = "";
	string name5 = "";
	string name6 = "";
	

	//current liability
	double dtotal_creditor = 0;
	if(ds.Tables["creditors"].Rows.Count == 1)
		dtotal_creditor = double.Parse(ds.Tables["creditors"].Rows[0]["total"].ToString());
	double dtotal_supp_credit = 0;
	if(ds.Tables["supplier_credit"].Rows.Count == 1)
		dtotal_supp_credit = double.Parse(ds.Tables["supplier_credit"].Rows[0]["total"].ToString());

	//total expense 
	double dtotal_paid_expense = 0;
	if(ds.Tables["total_paid_expense"].Rows.Count >= 1)
		dtotal_paid_expense = double.Parse(ds.Tables["total_paid_expense"].Rows[0]["total_paid_expense"].ToString());
	
	//fix assets
	double dtotal_fix_asset = 0;
	if(ds.Tables["fix_assets"].Rows.Count == 1)
		dtotal_fix_asset = double.Parse(ds.Tables["fix_assets"].Rows[0]["total"].ToString());

//Represeted by:
	double dtotal_share = 0;
	if(ds.Tables["shareholders"].Rows.Count > 0)
	{
		//double dtotal = 0;
		dtotal_share = double.Parse(ds.Tables["shareholders"].Rows[0]["total"].ToString());
//	DEBUG(" dtotal = ", dtotal.ToString());
	
	}

	//adding shareholders' assets to banking
//	dtotal_bank += dtotal_share - dtotal_paid_expense;

	double dtotal_equity = 0;
	dtotal_equity = m_dEquity;
//	if(ds.Tables["equity"].Rows.Count == 1)
//		dtotal_equity = double.Parse(ds.Tables["equity"].Rows[0]["total"].ToString());
	
	Response.Write("<tr><td colspan=2><b>CURRENT ASSETS</b></td></tr>");
	
	Response.Write("<tr><td>&nbsp;Bank Account </td><td align=right>");
	if(dtotal_bank < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_bank.ToString("c") +"</td>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;Trade Debtors</td><td align=right>");
	if(dtotal_debtor < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_debtor.ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;GST Recievable</td><td align=right>");
//	dtotal_gst = dtotal_supp_credit * (double.Parse(gst) / 100);
	if(dtotal_gst < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_gst.ToString("c") +"</td>");
	Response.Write("</tr>");

	if(m_bDebug)
	{
		Response.Write("<tr><td colspan=2 align=right>");
		Response.Write("<table border=1 cellspacing=0 cellpadding=0>");
		Response.Write("<tr><td>Purchase GST</td><td align=right>" + dpurchase_tax.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Custom GST</td><td align=right>" + dcustom_tax.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Expense GST</td><td align=right>" + dexpense_tax.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Assets GST</td><td align=right>" + dassets_tax.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Sales GST</td><td align=right>" + dsales_tax.ToString("c") + "</td></tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
	}

	
	Response.Write("<tr>");
	//Response.Write("<td>Stock On Hand</td><td align=right>"+ dtotal_stock.ToString("c") +"</td>");
	Response.Write("<tr><td>&nbsp;Stock On Hand</td>");
	Response.Write("<td align=right>");
	if(!m_bBranchSupport)
	{
		DataRow dr = ds.Tables["stock"].Rows[0];
		string ptotal = dr["ptotal"].ToString();
		string stotal = dr["stotal"].ToString();
		string stotal_cost = dr["sstotal"].ToString();
		dtotal_branch1 = MyDoubleParse(ptotal) - (MyDoubleParse(stotal_cost));
		Response.Write(dtotal_branch1.ToString("c"));
	}
	Response.Write("</td></tr>");

	if(m_bBranchSupport)
	{
		for(int i=0; i<ds.Tables["stock"].Rows.Count; i++)
		{
			//int nLessStock = ds.Tables["less_stock"].Rows.Count;
			//int nStock = ds.Tables["stock"].Rows.Count;
			//int nDifferent = 0;
			//double dless_total_stock = 0;
		//	if(nStock > nLessStock)
		//		nDifferent = nStock - nLessStock;
			DataRow dr = ds.Tables["stock"].Rows[i];
			string branch_id = "";
			string lp_branch_id = "";
			string lp_branch_name = "";
			branch_id = dr["branch_id"].ToString();
			name1 = dr["name"].ToString();
			string ptotal = dr["ptotal"].ToString();
			string stotal = dr["stotal"].ToString();
			string stotal_cost = dr["sstotal"].ToString();
			//dtotal_branch1 = double.Parse(ptotal) - double.Parse(stotal);
			dtotal_branch1 = double.Parse(ptotal) - (double.Parse(stotal_cost));
			dtotal_stock += dtotal_branch1;
			/*	for(int i=0; i<nStock; i++)
			{
				branch_id = ds.Tables["stock"].Rows[i]["branch"].ToString();
				dtotal_branch1 = double.Parse(ds.Tables["stock"].Rows[i]["total"].ToString());
				name1 = ds.Tables["stock"].Rows[i]["name"].ToString();
				
			if(i <= nLessStock - 1)
				{
					lp_branch_id = ds.Tables["less_stock"].Rows[i]["branch"].ToString();
					if(lp_branch_id == "" && lp_branch_id == null)
						lp_branch_id = "1";
					dless_total_stock = double.Parse(ds.Tables["less_stock"].Rows[i]["total"].ToString());
			DEBUG("dlesstotalstoc = ", dless_total_stock.ToString());
			DEBUG("dlessbranch = ", dtotal_branch1.ToString());

					if(lp_branch_id == branch_id)
						dtotal_stock += dtotal_branch1 - dless_total_stock;
					//else
					//	dtotal_stock += dtotal_branch1;

				}
				
				Response.Write("<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-"+ name1+"</td><td align=right>"+ dtotal_branch1.ToString("c") +"</td></tr>");
				
			}*/
			Response.Write("<tr><td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-"+ name1+"</td><td align=right>");
			if(dtotal_branch1 < 0)
				Response.Write("<font color=red>");
			Response.Write(""+ dtotal_branch1.ToString("c") +"</td></tr>");
		}
	}

	double dtotalAssets = dtotal_bank + dtotal_debtor + dtotal_gst + dtotal_stock;
	double dtotalEquity = dtotal_equity + dtotal_share;

	Response.Write("<tr align=right>");
	Response.Write("<td>&nbsp;</td><td>____________</td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;<b>Total Assets:</b></td><td align=right>");
	if(dtotalAssets < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotalAssets.ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=2><b>CURRENT LIABILITIES</b></td></tr>");

//Trade Creditors
	double dpurchase_payable = MyDoubleParse(ds.Tables["purchase_payable"].Rows[0]["total"].ToString());
	double drecorded_expense = MyDoubleParse(ds.Tables["recorded_expense"].Rows[0]["total"].ToString());
	double drecorded_assets = MyDoubleParse(ds.Tables["recorded_assets"].Rows[0]["total"].ToString());
	double dTradeCreditors = dpurchase_payable + drecorded_expense + drecorded_assets;

//	double dtotalLiability = dtotal_creditor + dtotal_supp_credit + dtotal_gst_payable;
//	double dtotalNetWorkingCapital = dtotalAssets - dtotalLiability;
	double dtotalNetWorkingCapital = dtotalAssets - dTradeCreditors;
	double dtotalNetAssets = dtotalNetWorkingCapital + dtotal_fix_asset;

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;Trade Creditors</td><td align=right>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
//	if((dtotal_creditor + dtotal_supp_credit) < 0)
	if(dTradeCreditors < 0)
		Response.Write("<font color=red>");
//	Response.Write(""+ (dtotal_creditor + dtotal_supp_credit).ToString("c") +"</td>");
	Response.Write(dTradeCreditors.ToString("c") + "</td>");
	Response.Write("</tr>");

	if(m_bDebug)
	{
		Response.Write("<tr><td colspan=2 align=right>");
		Response.Write("<table border=1 cellspacing=0 cellpadding=0>");
		Response.Write("<tr><td>Purchase Payable</td><td align=right>" + dpurchase_payable.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Recorded Expense</td><td align=right>" + drecorded_expense.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Recorded Assets</td><td align=right>" + drecorded_assets.ToString("c") + "</td></tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
	}

	Response.Write("<tr>");
	Response.Write("<td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b><i>Net Working Capital</td><td align=right>");
	if(dtotalNetWorkingCapital < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotalNetWorkingCapital.ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("<tr align=right><td>&nbsp;</td><td></td></tr>");
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;<b>Fixed Assets</b></td><td align=right><u>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	if(dtotal_fix_asset < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_fix_asset.ToString("c") +"</td>");
	Response.Write("</tr>");
//	Response.Write("<tr align=right>");
//Response.Write("<td>&nbsp;</td><td>______________</td>");
//Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;<b><u>NET ASSETS</b></u></td><td align=right>");
	if(dtotalNetAssets < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotalNetAssets.ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("<tr align=right>");
	Response.Write("<td>&nbsp;</td><td>===========</td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td><b><i>REPRESENTED BY:</b></i></td><td>&nbsp;</td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;Current A/Cs of Shareholder(s)</td><td align=right>");
	if(dtotal_share < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_share.ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<th align=left>Equity</td></tr>");
	Response.Write("<tr>");
	Response.Write("<td> &nbsp;- Registered Equity</td><td align=right>");
	if(dtotal_equity < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_equity.ToString("c") +"</td>");
	Response.Write("</tr>");

//Sales Profit	
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;- Sales Profit:</td><td align=right>");
	if(dtotal_sales_profit < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ dtotal_sales_profit.ToString("c") +"</td>");
	Response.Write("</tr>");

	if(m_bDebug)
	{
		Response.Write("<tr><td colspan=2 align=right>");
		Response.Write("<table border=1 cellspacing=0 cellpadding=0>");
		Response.Write("<tr><td> &nbsp;&nbsp; Total Sales</td><td align=right>" + dtotal_sales.ToString("c") + "</td></tr>");
		Response.Write("<tr><td> &nbsp;&nbsp; Total Freight</td><td align=right>" + dtotal_freight.ToString("c") + "</td></tr>");
		Response.Write("<tr><td> &nbsp;&nbsp; Total Cost</td><td align=right>" + dtotal_cost.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Gross Profit</td><td align=right>" + dgross_profit.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Total Expense</td><td align=right>" + dtotal_expense.ToString("c") + "</td></tr>");
		Response.Write("<tr><td>Stock Loss</td><td align=right>" + dtotal_loss.ToString("c") + "</td></tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
	}
/*
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;- Stock Loss:</td><td align=right>");
	if(dtotal_loss < 0)
		Response.Write("<font color=red>");
	Response.Write("<u>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"+ dtotal_loss.ToString("c") +"</td>");
	Response.Write("</tr>");
*/
	dtotal_equity += dtotal_sales_profit - dtotal_loss + dtotal_share;// - dtotal_paid_expense;

	Response.Write("<tr>");
	Response.Write("<td></td><td align=right>");
	if(dtotal_equity < 0)
		Response.Write("<font color=red>");
	Response.Write(""+ (dtotal_equity).ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("</tr>");
	Response.Write("<tr align=right>");
	Response.Write("<td>&nbsp;</td><td>===========</td>");
	Response.Write("</tr>");
	Response.Write("</table><br><br><br><br><br>");

}

/////////////////////////////////////////////////////////////////

</script>

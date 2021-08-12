<!-- #include file="page_index.cs" -->

<script runat=server>

string m_accountID = "";
string m_accClassString = "";
DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] m_EachMonth = new string[13];

string m_sdFrom = "";
string m_sdTo = "";
string m_syFrom = DateTime.Now.ToString("yyyy");
string m_syTo = DateTime.Now.ToString("yyyy");
string m_datePeriod = "";
int m_nPeriod = 0;
string m_dateSql = "";
string m_dateSql_trans = "";
string m_dateSql_trans2 = "";
string m_dateSql_exp = "";
string m_dateSql_eqt = "";
string m_dateSql4 = "";

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

	PrintAdminHeader();
	PrintAdminMenu();
	
	if(Request.QueryString["luri"] != null && Request.QueryString["luri"] != "")
		Session["luri"] = Request.QueryString["luri"];

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.QueryString["type"] != null && Request.QueryString["type"] != "")
		m_nPeriod = MyIntParse(Request.QueryString["type"].ToString());
	if(Request.Form["Datepicker1_day"] != null)
	{
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	if(m_nPeriod == 4) //select range
	{
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];

		m_sdFrom = Request.Form["pick_month1"]; 
		m_sdTo = Request.Form["pick_month2"];
	
	}
	if(Request.QueryString["type"] != null && Request.QueryString["type"] != "")
		m_nPeriod = 0;
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_nPeriod = 0;
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
		break;
	default:
		break;
	}
//DEBUG("m_period=", m_nPeriod);

	if(Request.Form["acc"] != null && Request.Form["acc"] != "")
		m_accountID = Request.Form["acc"].ToString();
//DEBUG("m_accoutdi = ", m_accountID);

	if(Request.Form["cmd"] == "View Opening Balance Log")
	{
		m_accClassString = GetAccClassString(m_accountID);
		DoQueryAccountAdjustLog(m_accClassString);
		return;
	}
	if(Request.QueryString["accid"] != null && Request.QueryString["accid"] != "")
	{
		m_accountID = Request.QueryString["accid"].ToString();
		m_accClassString = m_accountID;
		DoQueryAccountAdjustLog(m_accountID);
		return;
	}

	if(Request.Form["cmd"] == "View Report")
	{
		m_accClassString = GetAccClassString(m_accountID);
		DoAccountList();
		return;
	}

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_accountID = Request.QueryString["id"].ToString();
		m_accClassString = GetAccClassString(m_accountID);
		DoAccountList();
		return;
	}
	
	PrintMainPage();
		
//	else
//		Response.Write("<br><br><center><h3>Error, no account id</h3>");
	PrintAdminFooter();
}



void PrintMainPage()
{
//	PrintAdminHeader();
//	PrintAdminMenu();
	Response.Write("<br><center><h4><b>Account Transaction Report</b></h4></center>");
	Response.Write("<form name=f action=acc_report.aspx?r="+ DateTime.Now.ToOADate() +"");
//	if(m_accountID != "")
//		Response.Write("?id=" + Request.Form["acc"]);
	Response.Write(" method=post>");

	Response.Write("<table  border=0 align=center cellspacing=1 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=2 bgcolor=#666696><b>SELECT ACCOUNT TYPE: </b>");
	Response.Write("</td><tr><tr><td colspan=2>");
	if(!PrintAccountList())
		return;
	Response.Write("</td></tr>");	
	
	Response.Write("<tr><td colspan=2 aling=center><b><font size=1>Select Date Range</font></b></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Last Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function from common.cs
	Response.Write("<tr><td>");
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
	//Response.Write("</td>");
		//------ start second display date -----------
	Response.Write(" &nbsp; TO: ");
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
	Response.Write("</td>");
	Response.Write("</tr>");
		
//------ END second display date -----------
		
	Response.Write("<tr><td align=right>");
	if(Session["luri"] != null)
	{
		Response.Write("<input type=button value='<< back Account List' "+ Session["button_style"] +" onclick=\"");
		Response.Write("window.location=('"+ Session["luri"] +"'); \"");
		Response.Write(">");
	}
	
	//Response.Write("<input type=submit name=cmd value='View Opening Balance Log' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
//	PrintAdminFooter();
}


string GetAccClassString(string accountID)
{
	string sc = " SELECT class1*1000 + class2*100 + class3*10 + class4 AS cs FROM account WHERE id = " + accountID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "classstring") <= 0)
			return "";
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	return ds.Tables["classstring"].Rows[0]["cs"].ToString();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
bool DoAccountList()
{
	ds.Clear();

	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, p.deposit_date, GETDATE()) = 0 ";
		m_dateSql_trans = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		m_dateSql_exp = " AND DATEDIFF(month, e.payment_date, GETDATE()) = 0 ";
		m_dateSql_eqt = " AND DATEDIFF(month, e.recorded_date, GETDATE()) = 0 ";
		m_dateSql4 = " AND DATEDIFF(month, t.recorded_date, GETDATE()) = 0 ";
		m_dateSql_trans2 = " AND DATEDIFF(month, d.record_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, p.deposit_date, GETDATE()) = 1 ";
		m_dateSql_trans = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 1 ";
		m_dateSql_exp = " AND DATEDIFF(month, e.payment_date, GETDATE()) = 1 ";
		m_dateSql_eqt = " AND DATEDIFF(month, e.recorded_date, GETDATE()) = 1 ";
		m_dateSql4 = " AND DATEDIFF(month, t.recorded_date, GETDATE()) = 1 ";
		m_dateSql_trans2 = " AND DATEDIFF(month, d.record_date, GETDATE()) = 1 ";

		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, p.deposit_date, GETDATE()) >= 1 AND DATEDIFF(month, p.deposit_date, GETDATE()) <= 3 ";
		m_dateSql_trans = " AND DATEDIFF(month, d.trans_date, GETDATE()) >= 1 AND DATEDIFF(month, d.trans_date, GETDATE()) <= 3 ";
		m_dateSql_exp = " AND DATEDIFF(month, e.payment_date, GETDATE()) >= 1 AND DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
		m_dateSql_eqt = " AND DATEDIFF(month, e.recorded_date, GETDATE()) >= 1 AND DATEDIFF(month, e.recorded_date, GETDATE()) <= 3 ";
		m_dateSql4 = " AND DATEDIFF(month, t.recorded_date, GETDATE()) >= 1 AND DATEDIFF(month, t.recorded_date, GETDATE()) <= 3 ";
		m_dateSql_trans2 = " AND DATEDIFF(month, d.record_date, GETDATE()) >= 1 AND DATEDIFF(month, d.record_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND p.deposit_date >= '" + m_sdFrom + "' AND p.deposit_date <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql_trans = " AND d.trans_date >= '" + m_sdFrom + "' AND d.trans_date <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql_exp = " AND e.payment_date >= '" + m_sdFrom + "' AND e.payment_date <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql_eqt = " AND e.recorded_date >= '" + m_sdFrom + "' AND e.recorded_date <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql4 = " AND t.recorded_date >= '" + m_sdFrom + "' AND t.recorded_date <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql_trans2 = " AND d.record_date >= '" + m_sdFrom + "' AND d.record_date <= '" + m_sdTo + " 23:59"+"' ";
					
		break;

	default:
		break;
	}
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT 'deposit' AS record_type ";
	sc += ", p.id, '' AS name, '' AS trading_name, c1.name AS accountant ";
	sc += ", '' AS source_acc ";
	sc += ", '' AS dest_acc ";
//	sc += ", p.total AS total ";
	sc += ", sum(t.amount) AS total ";
	sc += ", p.deposit_date AS date, p.ref AS ref ";
	sc += ", 'banking.aspx?id=' + CONVERT(varchar, p.id) AS url ";
	sc += ", '' AS source_acc_id, '' AS dest_acc_id ";
	sc += " FROM tran_deposit p ";
	sc += " JOIN tran_deposit_id ti ON ti.id = p.id JOIN trans t ON t.id = ti.tran_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = p.staff ";
	sc += " WHERE p.account_id = " + m_accountID;
	sc += m_dateSql;
	sc += " GROUP BY p.id, c1.name, p.deposit_date, p.ref";	



	sc += " UNION ";
	sc += " SELECT 'Transaction' AS record_type ";
	sc += ", t.id, c.name, c.trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' ' + a1.name1 AS source_acc ";
	sc += ", a2.name4 + ' ' + a2.name1 AS dest_acc ";
	sc += ", t.amount AS total, d.trans_date AS date, d.payment_ref AS ref ";
	sc += ", 'payhistory.aspx?t=p&id=' + CONVERT(varchar, t.id) AS url ";
	sc += ", t.source AS source_acc_id, t.dest AS dest_acc_id ";
	sc += " FROM trans t ";
	sc += " JOIN tran_detail d ON d.id = t.id ";
	sc += " LEFT OUTER JOIN account a1 ON t.source=(a1.class1*1000 + a1.class2*100 + a1.class3*10 + a1.class4) ";
	sc += " LEFT OUTER JOIN account a2 ON t.dest = (a2.class1*1000 + a2.class2*100 + a2.class3*10 + a2.class4) ";
	sc += " LEFT OUTER JOIN card c ON c.id = d.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = d.staff_id ";
	sc += " WHERE (t.source = " + m_accClassString + " OR t.dest = " + m_accClassString +" )";
	sc += m_dateSql_trans;
//	sc += " ORDER BY d.trans_date DESC ";

	//expense
	sc += " UNION ";
	sc += " SELECT '<font color=red>Expense</font>' AS record_type ";
	sc += ", e.id, c.name, c.trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' ' + a1.name1 AS source_acc ";
	sc += ", a2.name4 + ' ' + a2.name1 AS dest_acc ";
	sc += ", CASE e.total WHEN 0 THEN e.tax ELSE e.total END AS total ";
	sc += ", e.payment_date AS date, e.payment_ref AS ref ";
	sc += ", 'expense.aspx?id=' + CONVERT(varchar, e.id) AS url ";
	sc += ", e.from_account AS source_acc_id, e.to_account AS dest_acc_id ";
	sc += " FROM expense e ";
	sc += " LEFT OUTER JOIN account a1 ON e.from_account = a1.id ";
	sc += " LEFT OUTER JOIN account a2 ON e.to_account = a2.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = e.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = e.recorded_by ";
	sc += " WHERE e.ispaid=1 AND (e.from_account = " + m_accountID + " OR e.to_account = " + m_accountID + ") ";
	sc += m_dateSql_exp;

	//equity
	sc += " UNION ";
	sc += " SELECT convert(varchar, e.type) AS record_type "; //'<font color=green>Owner Draw</font>' AS record_type ";
	sc += ", e.id, '' AS name, '' AS trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' ' + a1.name1 AS source_acc ";
	sc += ", '' dest_acc ";
	sc += ", e.total AS total, e.recorded_date AS date, e.payment_ref AS ref ";
	sc += ", 'acc_owner.aspx?vw=rp' AS url ";
	sc += ", e.account_type AS source_acc_id, '' AS dest_acc_id ";
	sc += " FROM acc_equity e ";
	sc += " LEFT OUTER JOIN account a1 ON e.account_type = a1.id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = e.recorded_by ";
	//added to view 2 account 5-11-04
	sc += " WHERE (e.account_type = " + m_accountID +" OR e.join_account = "+ m_accountID +" )";
	//sc += " WHERE e.account_type = " + m_accountID;
	sc += m_dateSql_eqt;

	//custom GST
	sc += " UNION ";
	sc += " SELECT '<font color=blue>TAX Payment</font>' AS record_type ";
	sc += ", t.id, c.name, c.trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' ' + a1.name1 AS source_acc ";
	sc += ", '' dest_acc ";
	sc += ", t.total_gst AS total, t.recorded_date AS date, t.payment_ref AS ref ";
	sc += ", 'custax_rp.aspx?r="+ DateTime.Now.ToOADate()+"&type=1' AS url ";
	sc += ", t.from_acc AS source_acc_id, '' AS dest_acc_id ";
	sc += " FROM custom_tax t ";
	sc += " LEFT OUTER JOIN account a1 ON t.from_acc = a1.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = t.payee ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = t.recorded_by ";
	
	//sc += " WHERE t.from_acc = " + m_accountID;
	//added to view 2 account 5-11-04
	sc += " WHERE (t.from_acc = " + m_accountID +" OR t.location_acc = "+ m_accountID +" )";
	sc += m_dateSql4;

	//invoice Refund
	sc += " UNION ";
	sc += " SELECT '<font color=purple>Customer Refund</font>' AS record_type ";
	sc += ", t.id, c.name, c.trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' - ' + a1.name1 AS source_acc ";
	sc += ", '' as dest_acc ";
	sc += ", t.total AS total, t.recorded_date AS date, t.payment_ref AS ref ";
	sc += ", 'ref_report.aspx?r="+ DateTime.Now.ToOADate() +"&rp=";
	sc += "'+ CONVERT(varchar(50),t.id) ";
	sc += " AS url ";
//	sc += ", 'invoice.aspx?r="+ DateTime.Now.ToOADate() +"&id='(select invoice_number from acc_refund_sub as where as.id = t.id) AS url ";
	sc += ", t.from_account AS source_acc_id, '' AS dest_acc_id ";
	sc += " FROM acc_refund t ";
	sc += " LEFT OUTER JOIN account a1 ON t.from_account = a1.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = t.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = t.recorded_by ";
	sc += " WHERE t.from_account = " + m_accountID + " "; // OR t.to_account = "+ m_accountID +" )";
	sc += m_dateSql4;
	
	sc += " UNION ";
	sc += " SELECT 'Transaction' AS record_type ";
	sc += ", t.id, c.name, c.trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' ' + a1.name1 AS source_acc ";
	sc += ", a2.name4 + ' ' + a2.name1 AS dest_acc ";
	sc += ", t.amount AS total, d.trans_date AS date, d.payment_ref AS ref ";
	sc += ", 'payhistory.aspx?t=p&id=' + CONVERT(varchar, t.id) AS url ";
	sc += ", t.source AS source_acc_id, t.dest AS dest_acc_id ";
	sc += " FROM trans t ";
	sc += " JOIN tran_detail d ON d.id = t.id ";
	sc += " JOIN trans_other tr ON d.id = tr.id AND tr.id = t.id ";
	sc += " LEFT OUTER JOIN account a1 ON t.source=(a1.class1*1000 + a1.class2*100 + a1.class3*10 + a1.class4) ";
	sc += " LEFT OUTER JOIN account a2 ON t.dest = (a2.class1*1000 + a2.class2*100 + a2.class3*10 + a2.class4) ";
	sc += " LEFT OUTER JOIN card c ON c.id = d.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = d.staff_id ";
	sc += " WHERE tr.location_acc = "+ m_accountID +" "; //(t.source = " + m_accClassString + " OR t.dest = " + m_accClassString +" )";
	sc += m_dateSql_trans;

//	sc += " ORDER BY d.trans_date DESC ";

/*	//Assets
	sc += " UNION ";
	sc += " SELECT '<font color=purple>Fix Assets</font>' AS record_type ";
	sc += ", t.id, c.name, c.trading_name, c1.name AS accountant ";
	sc += ", a1.name4 + ' - ' + a1.name1 AS source_acc ";
	sc += ", '' as dest_acc ";
	sc += ", tp.amount_applied AS total, tp.record_date AS date, t.payment_ref AS ref ";
	sc += ", 'ref_report.aspx' AS url ";
	sc += ", t.from_account AS source_acc_id, '' AS dest_acc_id ";
	sc += " FROM assets t JOIN assets_payment tp ON tp.assets_id = t.id ";
	sc += " LEFT OUTER JOIN account a1 ON t.to_account = a1.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = t.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = t.recorded_by ";
	sc += " WHERE t.from_account = " + m_accountID;
*/
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "report");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<center><br><font size=+1><b>ACCOUNT REPORT<b></font>");
//	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
	
	BindList();
	return true;
}

void PrintDescHeader(int nrows)
{
	string spagebreak = "";
	
	spagebreak += "<P>";
//	spagebreak += " <div style='page-break-before:auto'> ";
	spagebreak +="<tr>";
	spagebreak +="<th align=left nowrap>DATE</th>";
	spagebreak +="<th align=left nowrap>REF. NO.</th>";
	spagebreak +="<th align=left nowrap>TYPE</th>";
	spagebreak +="<th align=left>PAYEE</th>";
	spagebreak +="<th align=left nowrap>RECORDED BY</th>";
	
	spagebreak +="<th align=right>DEBIT</th>";
	spagebreak +="<th align=right>CREDIT</th>";
	spagebreak +="</tr>";
	spagebreak +="<tr><td colspan="+ nrows +"><hr size=1></tr>";
//	spagebreak +="</P>";
//	spagebreak +="	</div> ";
	Response.Write(spagebreak);
}
/////////////////////////////////////////////////////////////////
void BindList()
{
	int i = 0;
	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	if(rows <= 0)
	{
		//Response.Write("<script language=javascript>window.alert('No Transactions!!!');window.close();</script");
		//Response.Write(">");
		Response.Write("<center><h4>No Transactions");
		Response.Write("<br><br><a title='back' href='acc_report.aspx?r="+ DateTime.Now.ToOADate() +"' class=o> << Back </a>");	
	
		return;
	}
	
	DataRow dr = ds.Tables["report"].Rows[0];
	string accName = dr["source_acc"].ToString();
	if(accName == "")
		accName = dr["dest_acc"].ToString();

	Response.Write("<center><b>" + m_accClassString + " - " + accName + "</b><br>Date Period: "+ m_datePeriod +"<br>");
	
	int nrows = 7;
	Response.Write("<table width=99%  align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
/*	
	Response.Write("<tr><td colspan=7>");
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	Response.Write("<a title='search in account report' href='acc_report.aspx?st="+ m_accountID +"&r="+ DateTime.Now.ToOADate() +"' class=o target=new onclick='window.close();'>Search in Account Reprot</a>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan="+ nrows +"><hr size=1 ></tr>");
*/
	string stylesheet = "<STYLE> P {page-break-before: always}</STYLE>"; 
	Response.Write(stylesheet);

	Response.Write("<tr colspan="+ nrows +">Enquiry time: "+ DateTime.Now.ToString("dd/MM/yyyy  HH:mm")+"</td></tr>");
	Response.Write("<tr>");
	Response.Write("<th align=left nowrap>DATE</th>");
	Response.Write("<th align=left nowrap>REF. NO.</th>");
	Response.Write("<th align=left nowrap>TYPE</th>");
	Response.Write("<th align=left>PAYEE</th>");
	Response.Write("<th align=left nowrap>RECORDED BY</th>");
	
	Response.Write("<th align=right>DEBIT</th>");
	Response.Write("<th align=right>CREDIT</th>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan="+ nrows +"><hr size=1></tr>");
	
	double dSubTotal = 0;
	double dTotalExpense = 0;
	double dTotalDeposit = 0;
	double dSubTotalExp = 0;
	double dSubTotalDep = 0;
	double dTotalDiffer = 0;
	double dTotalExpEachMonth = 0;
	double dTotalDepEachMonth = 0;

	bool bAlterColor = false;

	DataRow[] dra = ds.Tables["report"].Select("", "date DESC");

	PageIndex m_cPI = new PageIndex(); //page index class
	
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	m_cPI.TotalRows = rows;
	m_cPI.URI = "?id=" + m_accountID;
	m_cPI.PageSize = 50;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

//	for(; i<rows && i<end; i++)
	int m=0;
	
	for(i=0; i<rows; i++)
	{
		dr = dra[i];
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		string record_type = dr["record_type"].ToString();
		string id = dr["id"].ToString();
		string url = dr["url"].ToString();
		string payee = dr["trading_name"].ToString();
		if(payee == "")
			payee = dr["name"].ToString();
		double dTotal = MyDoubleParse(dr["total"].ToString());
		string payment_date = DateTime.Parse(dr["date"].ToString()).ToString("dd-MM-yyyy");
		string recorded_by = dr["accountant"].ToString();
		string list_type = "1"; // 2=debit, 1=credit
		string account = dr["source_acc"].ToString();
		string reference = dr["ref"].ToString();
		if(record_type == "Transaction")
		{
			if(account == "")
			{
				account = dr["dest_acc"].ToString();
				list_type = "2";
			}
			else
			{
				string da = dr["dest_acc_id"].ToString();
				if(da != "")
				{
					if(da == m_accClassString)
					{
						account = dr["dest_acc"].ToString();
						list_type = "2";
					}
				}
			}
		}
		else if(record_type == "1") //owner withdraw			
		{
			record_type = "<font color=orange>Owner Draw</font>";
			list_type = "1";
		}
		else if(record_type == "2") //owner deposit
		{
			record_type = "<font color=green>Owner Deposit</font>";
			list_type = "2";
		}
		else if(record_type == "deposit")
		{
			list_type = "2";
			if(dTotal < 0)
			{
				dTotal = 0 - dTotal;
				list_type = "1";
			}
			record_type = "<font color=green>Deposit</font>";
		}
		dSubTotal += dTotal;
		
		if(list_type == "1")
		{
			dTotalExpense += dTotal;
		}
		if(list_type == "2")
		{
			dTotalDeposit += dTotal;
		}
		Response.Write("</font></td>");
		Response.Write("<td>" + payment_date + "</td>");
		Response.Write("<td>" + reference + "</td>");
		Response.Write("<td>" + record_type + "</td>");
		Response.Write("<td>" + payee + "</td>");
		Response.Write("<td>" + recorded_by + "</a></td>");
		
		if(list_type == "2")
			Response.Write("<td align=right><a title='view details' href=" + url + " class=o target=blank>" + dTotal.ToString("c") + "</a></td>");	
		else
			Response.Write("<td align=right>&nbsp;</td>");
		if(list_type == "1")
			Response.Write("<td align=right><a title='view details' href=" + url + " class=o target=blank>" + dTotal.ToString("c") + "</a></td>");	
		else 
			Response.Write("<td align=right>&nbsp;</td>");
		Response.Write("</tr>");
		
		m++;

		if(m == 50)
		{
			PrintDescHeader(nrows);
			m=-2;
		}

	}


/*	//total
	Response.Write("<tr style=\"color:white;background-color:#84EEDD;\">");  //#27ABD1
	Response.Write("<td colspan=");
	dTotalDiffer = dTotalDeposit - dTotalExpense;
	Response.Write(" 4 ");
	Response.Write(" align=right style=\"font-size:12\"><b>Total Credit :</b></td>");
	Response.Write("<td colspan=1 align=right style=\"font-size:12\">" + dTotalExpense.ToString("c") + "</td><td>&nbsp;</td>");
	Response.Write("</tr><tr  style=\"font-size:12;color:white;background-color:#ADDEff;\">");
	Response.Write("<td colspan=4 align=right><b>Total Debit :</b></td><td>&nbsp;</td><td  align=right> " + dTotalDeposit.ToString("c") + "</td>");
	Response.Write("</tr><tr  style=\"font-size:12;color:white;background-color:#ADDEff;\">");
	Response.Write("<td colspan=4 align=right><b>Total Differ :</b></td><td align=right colspan=2>" + dTotalDiffer.ToString("c") + "</td></tr>");
	Response.Write("</tr>");
*/
	Response.Write("<tr align=right rowspan=1 valign=bottom bgcolor=#eeeee3><td colspan=5 align=left><h5>SUB TOTAL:</td><td><h5>");
	Response.Write("<u><font color=red>&nbsp;&nbsp;&nbsp;&nbsp;"+ dTotalDeposit.ToString("c") +"</u></td><td><h5><u><font color=green>&nbsp;&nbsp;&nbsp;&nbsp;"+ dTotalExpense.ToString("c") +"");
	Response.Write("</td></tr>");

	Response.Write("</table>");

//	Response.Write(sPageIndex);
}


bool PrintAccountList()
{
	string account = "1";

	int rows = 0;
	string sc = "SELECT * FROM account  ORDER BY class1, class2, class3, class4";
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
		string id = dr["id"].ToString();
		string disnumber = dr["class1"].ToString() + " - " + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string acc_type = dr["name4"].ToString() + " - " +dr["name1"].ToString().ToUpper();
		Response.Write("<option value=" + id);
		if(id == m_accountID)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + acc_type +" $" +dr["balance"].ToString());		
	}
	Response.Write("</select></td></tr></table>");
	return true;
}

bool DoQueryAccountAdjustLog(string accountid)
{

	string sc = "SELECT al.*, a.name1, a.name2, a.name3, a.name4, a.opening_balance, a.balance, c.name AS staff ";
	sc += "FROM account_adjust_log al JOIN account a ON al.account_id = (a.class1 * 1000) + (a.class2 * 100) + (a.class3 * 10) + (a.class4) ";
	sc += " JOIN card c ON c.id = al.record_by ";
	sc += " WHERE al.account_id = "+ accountid;
	sc += " ORDER BY al.record_date DESC ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "account_log");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

Response.Write("<center><br><font size=+1><b>Opening Balance Adjustment Report<b></font>");
	BindLog();
	return true;

}

void BindLog()
{

	int i = 0;
	int rows = 0;
	if(ds.Tables["account_log"] != null)
		rows = ds.Tables["account_log"].Rows.Count;
	if(rows <= 0)
	{
		//Response.Write("<script language=javascript>window.alert('No Transactions!!!');window.close();</script");
		//Response.Write(">");
		Response.Write("<center><h4>No Transactions");
		Response.Write("<br><br><a title='back' href='acc_report.aspx?r="+ DateTime.Now.ToOADate() +"' class=o> << Back </a>");	
	
		return;
	}
	DataRow dr = ds.Tables["account_log"].Rows[0];
	string accName = dr["name1"].ToString().ToUpper() +" - "+ dr["name4"].ToString();
	
	Response.Write("<center><p><b>" + m_accClassString + " - " + accName + "</b>"); //<br>Date Period: "+ m_datePeriod +"<br>");
	
	int nrows = 7;
	Response.Write("<p><table width=1024 align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
//	Response.Write("<tr colspan="+ nrows +">Enquiry time: "+ DateTime.Now.ToString("dd/MM/yyyy  HH:mm")+"</td></tr>");
//	Response.Write("<tr>");
	Response.Write("<th align=left nowrap>RECORDED_DATE</th>");
	Response.Write("<th align=left nowrap>RECORDED_BY</th>");
	Response.Write("<th align=left nowrap>LAST_OPENING_BALANCE</th>");
	Response.Write("<th align=left>NEW_OPENING_BALANCE</th>");
	Response.Write("<th align=left>BALANCE</th>");
	//Response.Write("<th align=left nowrap>ACCOUNT ID</th>");
	
	Response.Write("</tr>");
//	Response.Write("<tr><td colspan="+ nrows +"><hr size=1></tr>");
	
	bool bAlterColor = false;

	PageIndex m_cPI = new PageIndex(); //page index class
	
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	m_cPI.TotalRows = rows;
	m_cPI.URI = "?id=" + m_accountID;
	m_cPI.PageSize = 50;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

//	for(; i<rows && i<end; i++)
	int m=0;
	
	for(i=0; i<rows; i++)
	{	
		dr = ds.Tables["account_log"].Rows[i];
		string staff = dr["staff"].ToString();
		string r_date = dr["record_date"].ToString();
		string old_open_balance = dr["last_amount"].ToString();
		string new_open_balance = dr["new_amount"].ToString();
		string balance = dr["balance"].ToString();
		//string staff = dr["staff"].ToString();
		Response.Write("<tr bgcolor=");
		if(bAlterColor)
			Response.Write("#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>"+ r_date +"</td>");
		Response.Write("<td>"+ staff +"</td>");
		Response.Write("<td>"+ old_open_balance +"</td>");
		Response.Write("<td>"+ new_open_balance +"</td>");
		Response.Write("<td>"+ balance +"</td>");



	}

	Response.Write("</table>");

}

</script>

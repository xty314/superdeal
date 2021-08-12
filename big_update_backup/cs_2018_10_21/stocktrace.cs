<!-- #include file="page_index.cs" -->
<script runat=server>

string m_branchID = "1";
string m_tableTitle = "Stock Trace ";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
string settle_time = "";

DataRow[] m_dra = null;

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = 0;

int m_nAllocated = 0;
int m_nStock = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}

	if(Request.QueryString["c"] != null)
		m_code = Request.QueryString["c"];

	if(Request.QueryString["p"] != null)
		m_nPeriod = MyIntParse(Request.QueryString["p"]);
	else if(Session["report_period"] != null)
	{
		m_nPeriod = (int)(Session["report_period"]);
		if(m_nPeriod == 3) //select range
		{
			if(Session["report_date_from"] != null)
				m_sdFrom = Session["report_date_from"].ToString();
			if(Session["report_date_to"] != null)
				m_sdTo = Session["report_date_to"].ToString();
		}
	}

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);

	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];

	if(Request.Form["Datepicker1_day"] != null)
	{
		//string day = Request.Form["day_from"];
		//string monthYear = Request.Form["month_from"];
		//ValidateMonthDay(monthYear, ref day);
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		//ValidateMonthDay(monthYear, ref day);
		m_sdFrom = day + "-" + monthYear;

		//day = Request.Form["day_to"];
		//monthYear = Request.Form["month_to"];
		//ValidateMonthDay(monthYear, ref day);
		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;

//		m_sdFrom = Request.Form["day_from"] + "/" + Request.Form["month_from"];
//		m_sdTo = Request.Form["day_to"] + "/" + Request.Form["month_to"];
		Session["report_date_from"] = m_sdFrom;
		Session["report_date_to"] = m_sdTo;
	}

//Response.Write("ksfljlsdf" + m_code);
//Response.Write("slfj;lskjf;lsdfjl;j" + Session["Settle_time" + m_code]);

	Session["report_period"] = m_nPeriod;
	settle_time = GetSettleTime(m_code);
/******
	if(	Session["Settle_time" + m_code] != null)
		settle_time = Session["Settle_time" + m_code].ToString();
	else
		settle_time ="2900-12-12 23:56:59";
*/
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Current Month";
//		m_dateSql = " AND p.date_received >= '" + settle_time + "' AND p.date_received <= '" +  DateTime.Now.ToString("dd-MM-yyyy") +"' ";
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

	PrintAdminHeader();
	PrintAdminMenu();

	if(m_code != "")
		DoStockTrace();
	else
		Response.Write("<br><center><h3>" + Lang("Stock Trace") + "</h3></center>");
	PrintMainPage();
	PrintAdminFooter();
}

string GetSettleTime(string code)
{
	if(ds.Tables["settledate"] != null)
		ds.Tables["settledate"].Clear();
	int i=0;
	string settledate = "";
	string sc = "";
//	sc = " SET DATEFORMAT ymd ";
	sc = " Select top 1 CONVERT(varchar(99), settle_time, 120) as settle_time from stock_settle where code=" + code;
	sc += " and branch_id =" + m_branchID;
//	sc = " Select top 1  settle_time as settle_time from stock_settle where code=" + code;
	sc += " order by id desc ";
//DEBUG("sc888=",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "settledate");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	DataRow dr = ds.Tables["settledate"].Rows[0];
	settledate = dr["settle_time"].ToString();

		return settledate;
//	string staff = dr["t_staff"].ToString();
//	settledate= ds.Tables["settledate"].Rows[0];

}

void PrintMainPage()
{
	Response.Write("<form action=stocktrace.aspx method=post>");

	Response.Write("<table align=center cellspacing=1  cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td align=right ><b>" + Lang("Item Code") + " : </b><input type=text size=10 name=code value=" + m_code + "> ");
	if(Session["branch_support"] != null)
	{
		Response.Write("<b>" + Lang("Branch") + " : </b>");
		PrintBranchNameOptions(m_branchID);
	}
	Response.Write("</td>");
	


/*
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>" + Lang("Date Range") + "</b></td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>" + Lang("This Month") + "</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>" + Lang("Last Month") + "</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>" + Lang("Last Three Months") + "</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>" + Lang("Select Date Range") + "</td></tr>");
	int i = 1;
	datePicker(); //call date picker function from common.cs
	Response.Write("<tr><td>");
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td>");
	Response.Write("<b>" + Lang("Select") + " : </b> " + Lang("From Date"));
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[1].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		//if(int.Parse(s_day) == d)
		//	Response.Write("<option value="+ d +" selected>"+d+"</option>");
		//else
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"tg_mm_setdays('document.forms[1].Datepicker1',1);\" style=''>");

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
			Response.Write("<option value="+m+" selected>" + Lang(txtMonth) + "</option>");
		else
			Response.Write("<option value="+m+">" + Lang(txtMonth) + "</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_year' onChange=\"tg_mm_setdays('document.forms[1].Datepicker1',1);\" style=''>");
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
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[1].Datepicker1',1)");
	Response.Write("</script ");
	Response.Write(">");
	Response.Write("</td>");
		//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
	Response.Write("<select name='Datepicker2_day' onChange=\"tg_mm_setdays('document.forms[1].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"tg_mm_setdays('document.forms[1].Datepicker2',1);\" style=''>");

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
			Response.Write("<option value="+m+" selected>" + Lang(txtMonth) + "</option>");
		else
			Response.Write("<option value="+m+">" + Lang(txtMonth) + "</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_year' onChange=\"tg_mm_setdays('document.forms[1].Datepicker2',1);\" style=''>");
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
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[1].Datepicker2',1)");
	Response.Write("</script ");
	Response.Write(">");
	Response.Write("</td>");
	Response.Write("</tr>");
*/


		
	Response.Write("<td colspan=2 align=right>&nbsp;<input type=submit name=cmd value='" + Lang("Trace") + "' class=b ></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
}

bool DoStockTrace()
{
	switch(m_nPeriod)
	{
	case 0:
//		m_dateSql = " AND DATEDIFF(month, p.date_received, GETDATE()) = 0 ";
//		m_dateSql = " AND p.date_received >= '" + settle_time + "' AND p.date_received <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql = " AND p.date_received >= '" + settle_time + "' AND p.date_received <= '" +  DateTime.Now.ToString("yyyy-MM-dd") + " 23:59"+"' ";
		break;
	case 1:
//		m_dateSql = " AND DATEDIFF(month, p.date_received, GETDATE()) = 1 ";
		m_dateSql = " AND p.date_received >= '" + settle_time + "' AND p.date_received <= '" +  DateTime.Now.ToString("dd-MM-yyyy") + " 23:59"+"' ";
		break;
	case 2:
//		m_dateSql = " AND DATEDIFF(month, p.date_received, GETDATE()) >= 1 AND DATEDIFF(month, p.date_received, GETDATE()) <= 3 ";
		m_dateSql = " AND p.date_received >= '" + settle_time + "' AND p.date_received <= '" +  DateTime.Now.ToString("dd-MM-yyyy") + " 23:59"+"' ";
		break;
	case 3:
//		m_dateSql = " AND p.date_received >= '" + m_sdFrom + "' AND p.date_received <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql = " AND p.date_received >= '" + settle_time + "' AND p.date_received <= '" +  DateTime.Now.ToString("dd-MM-yyyy") + " 23:59"+"' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT ymd ";
	sc += " SELECT i.qty AS changed, d.date_received AS date, c.name AS t_staff, cs.trading_name AS t_supplier ";
	sc += ", '<font color=green>Purchase</font>' AS t_type ";
	sc += ", '<a href=purchase.aspx?t=pp&n=' + CONVERT(varchar(50), p.id) + ' class=o>Purchase Order #' + STR(p.po_number) + '</a>' AS description ";
	sc += " FROM purchase_item i JOIN purchase p ON i.id = p.id ";
	sc += " JOIN dispatch d ON d.code = i.code AND d.id = i.kid ";
	sc += " LEFT OUTER JOIN card c ON c.id = p.staff_id ";
	sc += " LEFT OUTER JOIN card cs ON cs.id = p.supplier_id ";
	sc += " WHERE i.code = " + m_code + " AND p.status = " + GetEnumID("purchase_order_status", "received");
	sc += m_dateSql.Replace("p.date_received", "d.date_received");
	if(Session["branch_support"] != null)
		sc += " AND d.branch = " + m_branchID;
//DEBUG("sc11213=", sc);
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

	//adjustment
	switch(m_nPeriod)
	{
	case 0:
	//	m_dateSql = " AND DATEDIFF(month, s.log_time, GETDATE()) = 0 ";
		m_dateSql = " AND  s.log_time >= '" + settle_time + "' AND  s.log_time <= '" +  DateTime.Now.ToString("yyyy-MM-dd")+ " 23:59:59"+"' ";
		break;
	case 1:
//		m_dateSql = " AND DATEDIFF(month, s.log_time, GETDATE()) = 1 ";
		m_dateSql = " AND  s.log_time >= '" + settle_time + "' AND  s.log_time <= '" +  DateTime.Now.ToString("dd-MM-yyyy")+ " 23:59"+"' ";
		break;
	case 2:
//		m_dateSql = " AND DATEDIFF(month, s.log_time, GETDATE()) >= 1 AND DATEDIFF(month, s.log_time, GETDATE()) <= 3 ";
		m_dateSql = " AND  s.log_time >= '" + settle_time + "' AND  s.log_time <= '" +  DateTime.Now.ToString("dd-MM-yyyy")+ " 23:59"+"' ";
		break;
	case 3:
//		m_dateSql = " AND s.log_time >= '" + m_sdFrom + "' AND s.log_time <= '" + m_sdTo + " 23:59"+"' ";
		m_dateSql = " AND  s.log_time >= '" + settle_time + "' AND  s.log_time <= '" +  DateTime.Now.ToString("dd-MM-yyyy")+ " 23:59"+"' ";
		break;
	default:
		break;
	}

	sc = " SET DATEFORMAT ymd ";
	sc += " SELECT s.qty as changed, s.log_time AS date, c.name AS t_staff ";
	sc += ", '<font color=red>Adjustment</font>' AS t_type ";
	sc += ", s.note AS description ";
	sc += " FROM stock_adj_log s LEFT OUTER JOIN card c ON c.id = s.staff ";
	sc += " WHERE s.code = " + m_code + m_dateSql;
	if(Session["branch_support"] != null)
		sc += " AND s.branch_id = " + m_branchID;
//DEBUG("sc2=", sc);
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

		//rma stock
	switch(m_nPeriod)
	{
	case 0:
	//	m_dateSql = " AND DATEDIFF(month, rs.replaced_date, GETDATE()) = 0 ";
		m_dateSql = " AND rs.replaced_date >= '" + settle_time + "' AND rs.replaced_date <= '" +  DateTime.Now.ToString("yyyy-MM-dd")+ " 23:59"+"' ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, rs.replaced_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, rs.replaced_date, GETDATE()) >= 1 AND DATEDIFF(month, rs.replaced_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND rs.replaced_date >= '" + m_sdFrom + "' AND rs.replaced_date <= '" + m_sdTo + " 23:59"+"' ";
		break;
	default:
		break;
	}
	sc = " SET DATEFORMAT ymd ";
	sc += " SELECT 1 AS changed, rs.replaced_date AS date, c.name AS t_staff, 'RMA Transfered' AS t_type ";
	sc += ", '<a href=rma_rp.aspx?ra=' + CONVERT(varchar(50), rs.repair_id) + ' class=o> RMA#' + STR(rs.repair_id) + ' </a>' AS description ";
	sc += " FROM return_sn rs ";
	sc += " JOIN card c ON c.id = rs.staff ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = rs.code ";
	sc += " WHERE cr.code = " + m_code + m_dateSql;
	if(Session["branch_support"] != null)
		sc += " AND rs.branch = " + m_branchID;
//DEBUG("sc =", sc );
//DEBUG("sc3=", sc);
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


	//stock borrow
	switch(m_nPeriod)
	{
	case 0:
//		m_dateSql = " AND DATEDIFF(month, b.borrow_date, GETDATE()) = 0 ";
		m_dateSql = " AND b.borrow_date >= '" + settle_time + "' AND b.borrow_date <= '" +  DateTime.Now.ToString("yyyy-MM-dd")+ " 23:59"+"' ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, b.borrow_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, b.borrow_date, GETDATE()) >= 1 AND DATEDIFF(month, b.borrow_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND b.borrow_date >= '" + m_sdFrom + "' AND b.borrow_date <= '" + m_sdTo + " 23:59"+"' ";
		break;
	default:
		break;
	}
	sc = " SET DATEFORMAT ymd ";
	sc += " SELECT b.return_qty + ISNULL(b.replace_qty,0) - b.approved_qty AS changed, b.borrow_date AS date, c.name AS t_staff, '<font color=purple>Stock Borrow</font>' AS t_type ";
	sc += ", '<a href=stk_borrow.aspx?pr=lt&bid='+ CONVERT(varchar(10),b.borrow_id) +'&rp=1 class=o target=new> '+ c2.name +' BORROW#' + STR(b.borrow_id) + ' </a>' AS description ";
	sc += " FROM stock_borrow b ";
	sc += " JOIN card c ON c.id = b.approved_by ";
	sc += " JOIN card c2 ON c2.id = b.borrower_id ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = b.code ";
	sc += " WHERE cr.code = " + m_code + m_dateSql;
	sc += " AND b.approved = 1 ";
//	sc += " AND b.return_qty + ISNULL(b.replace_qty,0) - b.approved_qty <> 0 ";
	if(Session["branch_support"] != null)
		sc += " AND b.branch = " + m_branchID;
//DEBUG("sc4 =", sc );
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


	//sales
	switch(m_nPeriod)
	{
	case 0:
//		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		m_dateSql = " AND i.commit_date >= '" + settle_time + "' AND i.commit_date <= '" +  DateTime.Now.ToString("yyyy-MM-dd")+ " 23:59"+"' ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59"+"' ";
		break;
	default:
		break;
	}
	sc = " SET DATEFORMAT ymd ";
	sc += " SELECT 0 - s.quantity AS changed, i.commit_date AS date, c.name AS t_staff, 'Sales' AS t_type ";
	sc += ", '<a href=invoice.aspx?' + CONVERT(varchar(50), i.invoice_number) + ' class=o>' + ISNULL(cc.trading_name, 'Cash Sales ') + ' INV#' + STR(i.invoice_number) + '</a>' AS description ";
	//sc +=  ", '' AS description ";
	sc += " FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id = o.sales ";
	sc += " LEFT OUTER JOIN card cc ON cc.id = i.card_id ";
	sc += " WHERE s.code = " + m_code + m_dateSql;
	if(Session["branch_support"] != null)
		sc += " AND o.branch = " + m_branchID;
//DEBUG("sc5 =" , sc);
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



	//sort and display
	m_dra = ds.Tables["report"].Select("", "date");
	PrintStockTrace();
	





	//allocated trace
	switch(m_nPeriod)
	{
	case 0:
//		m_dateSql = " AND DATEDIFF(month, o.record_date, GETDATE()) = 0 ";
		m_dateSql = " AND o.record_date >= '" + settle_time + "' AND o.record_date <= '" +  DateTime.Now.ToString("yyyy-MM-dd")+ " 23:59"+"' ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, o.record_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, o.record_date, GETDATE()) >= 1 AND DATEDIFF(month, o.record_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND o.record_date >= '" + m_sdFrom + "' AND o.record_date <= '" + m_sdTo + " 23:59"+"' ";
		break;
	default:
		break;
	}
	sc = " SET DATEFORMAT ymd ";
	sc += " SELECT o.record_date AS date, i.quantity AS changed, c.name AS t_staff, 'Allocated' AS t_type ";
	sc += ", '<a href=pos.aspx?n=' + STR(o.id) + ' class=o>' + cc.trading_name + ' ORDER# ' + STR(o.number) + '</a>' AS description ";
	sc += " FROM order_item i JOIN orders o ON i.id = o.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = o.sales ";
	sc += " LEFT OUTER JOIN card cc ON cc.id = o.card_id ";
	sc += " WHERE i.code = " + m_code + " AND o.status <> 2 AND o.status <> 3 AND o.status <> 6 ";
	if(Session["branch_support"] != null)
		sc += " AND o.branch = " + m_branchID;
//	sc += m_dateSql; //dispaly all allocated regardless date selection
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "allocated");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	//sort and display
//	m_dra = ds.Tables["allocated"].Select("", "date");
//	PrintAllocatedTrace();

	return true;
}



/////////////////////////////////////////////////////////////////
void PrintStockTrace()
{
	string code = "";
	int i = 0;
	DataRow drp = null;
	GetProduct(m_code, ref drp);
	
	string sc = " SELECT q.qty AS stock, q.allocated_stock, c.code, c.supplier_code, c.name,ss.stock as settle, CONVERT(varchar(99), ss.settle_time, 120) as settle_time ";
	sc += " FROM stock_qty q JOIN code_relations c ON c.code = q.code ";
	sc += " join stock_settle ss on ss.code = c.code ";
	sc += " WHERE q.code = " + m_code;
	if(Session["branch_support"] != null)
		sc += " AND q.branch_id = " + m_branchID;
//DEBUG("sc111 ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "retail_stock") > 0)
			drp = ds.Tables["retail_stock"].Rows[0];
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return;
	}

	if(drp == null)
	{
		Response.Write("<br><h3>Error, product not found");
		return;
	}

	m_nStock = MyIntParse(drp["stock"].ToString());
	m_nAllocated = MyIntParse(drp["allocated_stock"].ToString());
	code = drp["code"].ToString();	

//DEBUG("code=",code);

	Response.Write("<form action=stocktrace.aspx?p=" + m_nPeriod + "&c=" + m_code + " method=post>");
	Response.Write("<br><center><h3>" + Lang("Stock Trace") + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b> &nbsp; ");
	if(Session["branch_support"] != null)
	{
		Response.Write("<font size=+1>" + Lang("Branch") + " : </font>");
		PrintBranchNameOptions();
		Response.Write(" <input type=submit name=cmd value='" + Lang("Trace") + "' class=b></center><br>");
	}
	Response.Write("</form>");

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=6><font size=+1>#" + m_code + " " + drp["supplier_code"].ToString() + " &nbsp&nbsp; " + drp["name"].ToString() + "</font></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=6 align=right>" + Lang("Current Stock") + " : <font color=blue><b>" + m_nStock);


//	Response.Write("</b></font> Allocated : <font color=red>" + m_nAllocated + "</b></font></td></tr>");
	
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>" + Lang("Date") + "</th>");
	Response.Write("<th>" + Lang("Staff") + "</th>");
	Response.Write("<th>" + Lang("Description") + "</th>");
	Response.Write("<th>" + Lang("Type") + "</th>");
	Response.Write("<th align=right>" + Lang("In/Out") + "</th>");
	Response.Write("<th align=right>" + Lang("Balance") + "</th>");
	Response.Write("</tr>");

	int rows = m_dra.Length;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	int nAfter = 0;
	for(i = 0; i < rows; i++)
	{
		nAfter += MyIntParse(m_dra[i]["changed"].ToString());
	}

	int obalance = MyIntParse(drp["settle"].ToString());// m_nStock - nAfter;
//	settle_time = DateTime.Parse(drp["settle_time"].ToString()).ToString("dd-MM-yyyy hh:mm:ss");
	settle_time = drp["settle_time"].ToString();

	Session["Settle_time" + code] = settle_time;

	nAfter = obalance;

	string start_date = DateTime.Parse(m_dra[0]["date"].ToString()).ToString("dd-MM-yyyy");

	Response.Write("<tr style=\"color:black;background-color:yellow;font-weight:bold;\">");
//	Response.Write("<td>" + start_date + "</td><td>&nbsp;</td><td>");
	Response.Write("<td>" + settle_time + "</td><td>&nbsp;</td><td>");
	Response.Write("<font color=green><b>" + Lang("Opening Balance") + "</b></font></td><td colspan=3 align=right>" + obalance + "</td></tr>");

	bool bAlterColor = true;
	string date = "";
	for(i = 0; i < rows; i++)
	{
		DataRow dr = m_dra[i];
		date = DateTime.Parse(dr["date"].ToString()).ToString("yyyy-MM-dd");
		string staff = dr["t_staff"].ToString();
		string desc = dr["description"].ToString();
		string changed = dr["changed"].ToString();
		string type = dr["t_type"].ToString();

		nAfter += MyIntParse(changed);

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + staff + "</td>");
		Response.Write("<td>" + desc + "</td>");
		Response.Write("<td align=center>" + type + "</td>");
		Response.Write("<td align=right>" + changed + "</td>");
		Response.Write("<td align=right>" + nAfter.ToString() + "</td>");
		Response.Write("</tr>");
	}
	
	Response.Write("<tr style=\"color:black;background-color:yellow;font-weight:bold;\">");
	Response.Write("<td>" + date + "</td><td>&nbsp;</td><td>");
//	Response.Write("<font color=green><b>" + Lang("Closing Balance") + "</b></font></td><td colspan=3 align=right>" + m_nStock + "</td></tr>");
	Response.Write("<font color=green><b>" + Lang("Closing Balance") + "</b></font></td><td colspan=3 align=right>" + nAfter + "</td></tr>");

	Response.Write("<tr><td colspan=6 align=right><b>" + Lang("Current Stock") + " : " + m_nStock + "</b></td></tr>");
	Response.Write("</table>");
}

/////////////////////////////////////////////////////////////////
void PrintAllocatedTrace()
{
	int i = 0;
	
	Response.Write("<br><center><h3>" + Lang("Allocated Trace") + "</h3>");
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>" + Lang("Date") + "</th>");
	Response.Write("<th>" + Lang("Staff") + "</th>");
	Response.Write("<th>" + Lang("Description") + "</th>");
	Response.Write("<th>" + Lang("Type") + "</th>");
	Response.Write("<th align=right>" + Lang("In/Out") + "</th>");
	Response.Write("<th align=right>" + Lang("Balance") + "</th>");
	Response.Write("</tr>");

	int rows = m_dra.Length;
	if(rows <= 0)
	{
		Response.Write("<tr><td colspan=6 align=right><b>" + Lang("Current Allocated Stock") + " : " + m_nAllocated + "</b></td></tr>");
		Response.Write("</table>");
		return;
	}

	int nAfter = 0;
	for(i = 0; i < rows; i++)
	{
		nAfter += MyIntParse(m_dra[i]["changed"].ToString());
	}

	int obalance = m_nAllocated - nAfter;
	nAfter = obalance;

	string start_date = DateTime.Parse(m_dra[0]["date"].ToString()).ToString("dd-MM-yyyy");
	Response.Write("<tr style=\"color:black;background-color:yellow;font-weight:bold;\">");
	Response.Write("<td>" + start_date + "</td><td>&nbsp;</td><td>");
	Response.Write("<font color=green><b>" + Lang("Opening Balance") + "</b></font></td><td colspan=3 align=right>" + obalance + "</td></tr>");

	bool bAlterColor = true;
	string date = "";
	for(i = 0; i < rows; i++)
	{
		DataRow dr = m_dra[i];
		date = DateTime.Parse(dr["date"].ToString()).ToString("dd-MM-yyyy");
		string staff = dr["t_staff"].ToString();
		string desc = dr["description"].ToString();
		string changed = dr["changed"].ToString();
		string type = dr["t_type"].ToString();

		nAfter += MyIntParse(changed);

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + staff + "</td>");
		Response.Write("<td>" + desc + "</td>");
		Response.Write("<td align=center>" + type + "</td>");
		Response.Write("<td align=right>" + changed + "</td>");
		Response.Write("<td align=right>" + nAfter.ToString() + "</td>");
		Response.Write("</tr>");
	}
	
	Response.Write("<tr style=\"color:black;background-color:yellow;font-weight:bold;\">");
	Response.Write("<td>" + date + "</td><td>&nbsp;</td><td>");
	Response.Write("<font color=green><b>" + Lang("Closing Balance") + "</b></font></td><td colspan=3 align=right>" + m_nAllocated + "</td></tr>");

	Response.Write("<tr><td colspan=6 align=right><b>" + Lang("Current Allocated Stock") + " : " + m_nAllocated + "</b></td></tr>");
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
</script>

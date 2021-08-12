<!-- #include file="page_index.cs" -->
<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>
<script runat=server>

string m_branchID = "";
string m_type = "";
string m_tableTitle = "Transaction Summary";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
DataRow[] m_dra = null;
string[] m_EachMonth = new string[16];

string m_sorted = "DESC";

string m_path = "";
string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
int m_nPeriod = 0;

bool m_bDealerLevel = false;
bool m_bPickTime = false;
bool m_bSltBoth = false; //item details and customer are selected
bool m_bPrint = false;
string m_paymentType = "";
string m_customerID = "all";
string m_customerName = "";
string m_customerEmail = "";
string m_customerPhone = "";
string tableWidth = "80%";
string m_export = "";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["export"] != null)
		m_export = Request.QueryString["export"];

	if(int.Parse(GetSiteSettings("dealer_levels", "0")) > 0)
		m_bDealerLevel = true;

	string strPath = Server.MapPath("backup/");
	string lname = Session["name"].ToString();
	int bpos = lname.IndexOf(" ");
	if(bpos > 0)
		lname = lname.Substring(0, bpos);
	lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	m_path = strPath + lname;
//export done 
	if(m_export == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Export Invoice Report Done</h3></center>");

		if(Directory.Exists(m_path))
		{
			Response.Write("<table align=center cellspacing=7 cellpadding=3 border=0 bordercolor#EEEEEE");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
			Response.Write("<tr><td colspan=4><b>Backup files ready to download</b></td></tr>");

			Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
			Response.Write("<td><b>FILE</b></td>");
			Response.Write("<td><b>SIZE</b></td>");
			Response.Write("<td><b>FILE DATE</b></td>");
			Response.Write("<td><b>DOWNLOAD</b></td>");
			Response.Write("</tr>");

			DirectoryInfo di = new DirectoryInfo(m_path);
			foreach (FileInfo f in di.GetFiles("*.*")) 
			{
				string s = f.FullName;
				string file = s.Substring(m_path.Length+1, s.Length-m_path.Length-1);
//				string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
				Response.Write("<tr><td><a href=backup/" + lname + "/" + file + ">" + file);
				Response.Write("</a></td>");
				Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
				Response.Write("<td>" + f.LastWriteTime.ToString("dd-MM-yyyy HH:mm") + "</td>");
				Response.Write("<td align=right><a href=backup/" + lname + "/" + file + " class=o>download");
				Response.Write("</a></td>");
				Response.Write("</tr>");
			}
			Response.Write("</table>");
			if(!m_bPrint)
			{
				LFooter.Text = "<br><br><center><a href="+ Request.ServerVariables["URL"] +"";
				LFooter.Text += " class=o>New Report</a>";
			}
		}
		return;
	}


	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
		if(m_branchID == "0")
			m_branchID = "";
	}

	if(Request.Form["pm"] != null)
		m_paymentType = Request.Form["pm"];
	else if(Request.QueryString["pm"] != null)
		m_paymentType = Request.QueryString["pm"];

//DEBUG("m_brahc =-", m_branchID);
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

	int i = 0;
	
	int day = 1;
	int month = DateTime.Now.Month;
	int year = DateTime.Now.Year;
	
	if(Request.Form["hCustomerID"] != null && Request.Form["hCustomerID"] != "")
		m_customerID = Request.Form["hCustomerID"];
	if(Request.Form["cmdSearch"] == "Search Customer" || (Request.Form["customer"] != null && Request.Form["customer"] != ""))
	{
		doSearchCustomer(Request.Form["customer"].ToString());
	}
//DEBUG("m_customerID =", m_customerID);
//	m_type = Request.QueryString["t"];
	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["print"] == null || Request.QueryString["print"] == "")
		{
			PrintMainPage();
			LFooter.Text = m_sAdminFooter;
			return;
		}
//		m_type = Request.QueryString["t"];
		if(Session["report_period"] != null)
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
	}

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
	if(Request.Form["t"] != null)
		m_type = Request.Form["t"];
	//if(Request.Form["day_from"] != null)
	if(Request.Form["Datepicker1_day"] != null)
	{
		m_sdFrom = Request.Form["Datepicker1_day"] + "-" + Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		m_sdTo = Request.Form["Datepicker2_day"] + "-" + Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
//DEBUG("m_sdFrom= ", m_sdFrom);
		//m_sdFrom = Request.Form["day_from"] + "/" + Request.Form["month_from"];
		//m_sdTo = Request.Form["day_to"] + "/" + Request.Form["month_to"];
		Session["report_date_from"] = m_sdFrom;
		Session["report_date_to"] = m_sdTo;
	}
	if(Request.Form["pick_month1"] != null)
	{
		m_smFrom = Request.Form["pick_month1"];
		m_smTo = Request.Form["pick_month2"];
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];
	
	}
	if(Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"].ToString();
		m_sdTo = Request.QueryString["to"].ToString();
	}
	if(Request.QueryString["pr"] != null)
		m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());

	Session["report_period"] = m_nPeriod;
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yestoday";
		break;
		
	case 2:
		m_datePeriod = "This Week";
		break;
	case 3:
		m_datePeriod = "This Month";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	case 5:
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_smFrom)-1] + "-"+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_smTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}
	
	if(Request.QueryString["print"] != null)
		m_bPrint = true;

	
	PrintAdminHeader();
	if(!m_bPrint)
		PrintAdminMenu();
	DoTransactionSummary("");

	if(!m_bPrint)
	{
		LFooter.Text = "<br><br><center><a href="+ Request.ServerVariables["URL"] +"";
		LFooter.Text += " class=o>New Report</a>";
	}
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
	Response.Write("' method=post>");

	Response.Write("<br><center><h3>Transaction Summary</h3>");

	if(Session["branch_support"] != null)
	{
		int nal = MyIntParse(Session[m_sCompanyName + "AccessLevel"].ToString());
		if(nal >= 7) //manager
		{
			Response.Write("<b>Branch : </b>");
			PrintBranchNameOptions(m_branchID, "", true);
		}
		else
		{
			Response.Write("<input type=hidden name=branch value=" + Session["branch_id"].ToString() + ">");
		}
	}

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
string uri = Request.ServerVariables["URL"].ToString();
/*	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	Response.Write("<b>Select Payment Type</b></td></tr>");
	
	Response.Write("<tr><td colspan=6>");
	Response.Write("<select name=pm><option value=''>All</option>");
	Response.Write("<option value=1>Cash</option>");
	Response.Write("<option value=2>Cheque</option>");
	Response.Write("<option value=3>Credit Card</option>");
	Response.Write("<option value=6>Eftpos</option>");
	Response.Write("</select>\r\n");
	
	
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	Response.Write("<b>Search Customer</b></td></tr>");
	
	Response.Write("<tr><td colspan=6>");
	//Response.Write("<textarea name=customer rows=1 cols=25>"+ m_customerName +"</textarea>");
	if(m_customerEmail != null && m_customerEmail != "")
		Response.Write(" <b>Found Customer:</b>");
	Response.Write("<input type=text name=customer size=15% value=\""+ m_customerName +"\">");
	if(m_customerEmail != null && m_customerEmail != "" && m_customerID != "all" && m_customerID != null && m_customerID != "")
		Response.Write("<input type=hidden name=hCustomerID value="+ m_customerID +">");

	Response.Write("<input type=submit name=cmdSearch value='Search Customer' "+ Session["button_style"] +">");
*/
/*	Response.Write("<select name=id>");
	Response.Write("<option value='all' ");
	if(m_customerID == "all")
		Response.Write(" selected ");
	Response.Write(">All</option>");
	Response.Write(""+ GetCardValue("", m_customerID) +"</select></td>");
	*/
		Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Select Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Yestoday</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=2>This Week</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=3>This Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=4>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function
	Response.Write("<tr><td><b> &nbsp; From Date </b>");
	
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<select name='Datepicker1_day' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

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
	Response.Write("<select name='Datepicker1_year' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
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
//------ END first display date -----------

	//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
	Response.Write("<select name='Datepicker2_day' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

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
	Response.Write("<select name='Datepicker2_year' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
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
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right colspan=6>");
//	Response.Write("<input type=submit name=cmd value='Export Report' " + Session["button_style"] + " onclick=\"return confirm('Export invoice report!!!');\">");
	Response.Write("<input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	LFooter.Text = m_sAdminFooter;
}

bool DoTransactionSummary(string status)
{
	m_tableTitle = "Payment Summary";
	string trandsSQL = "";
	string inSQL = "";
	string dpSQL = "";
	string poSQL = "";
	string ppoSQL = "";
	string depSQL = "";
	switch(m_nPeriod)
	{
	case 0:
		//m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		m_dateSql = " AND DATEDIFF(day, td.trans_date, GETDATE()) = 0 ";		
		inSQL = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";		
		poSQL = " AND DATEDIFF(day, ISNULL(p.date_invoiced, p.date_create), GETDATE()) = 0 ";				
		ppoSQL = " AND DATEDIFF(day, ISNULL(pp.date_invoiced, pp.date_create), GETDATE()) = 0 ";				
		depSQL = " AND DATEDIFF(day, td.deposit_date, GETDATE()) = 0 ";		
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, td.trans_date, GETDATE()) = 1 ";
		inSQL = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";		
		poSQL = " AND DATEDIFF(day, ISNULL(p.date_invoiced, p.date_create), GETDATE()) = 1 ";				
		ppoSQL = " AND DATEDIFF(day, ISNULL(pp.date_invoiced, pp.date_create), GETDATE()) = 1 ";	
		depSQL = " AND DATEDIFF(day, td.deposit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(week, td.trans_date, GETDATE()) = 0 ";
		inSQL = " AND DATEDIFF(week, i.commit_date, GETDATE()) = 0 ";		
		poSQL = " AND DATEDIFF(week, ISNULL(p.date_invoiced, p.date_create), GETDATE()) = 0 ";
		ppoSQL = " AND DATEDIFF(week, ISNULL(pp.date_invoiced, pp.date_create), GETDATE()) = 0 ";
		depSQL = " AND DATEDIFF(week, td.deposit_date, GETDATE()) = 0 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, td.trans_date, GETDATE()) = 0 ";	
		inSQL = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";	
		poSQL = " AND DATEDIFF(month, ISNULL(p.date_invoiced, p.date_create), GETDATE()) = 0 ";	
		ppoSQL = " AND DATEDIFF(month, ISNULL(pp.date_invoiced, pp.date_create), GETDATE()) = 0 ";	
		depSQL = " AND DATEDIFF(month, td.deposit_date, GETDATE()) = 0 ";	
		break;
	case 4:
		m_dateSql = " AND td.trans_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";		
		inSQL = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";		
		poSQL = " AND ISNULL(p.date_invoiced, p.date_create) BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";		
		ppoSQL = " AND ISNULL(pp.date_invoiced, pp.date_create) BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";		
		depSQL = " AND td.deposit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";		
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = "";
//////sales summary report
	sc = "SET DATEFORMAT dmy SELECT ISNULL(SUM(total-tax),0) AS 'Total No Gst', ISNULL(SUM(tax),0) AS 'Total GST', ISNULL(SUM(total),0) AS 'Total Sales' ";
	sc += " FROM invoice i WHERE i.commit_date IS NOT NULL ";
	sc += inSQL;
	if(m_branchID != "" && m_branchID != null && TSIsDigit(m_branchID))
	{
		sc += " AND i.branch = "+ m_branchID;
	}

//DEBUG("sales=", sc);	
	try
	{  
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "sales_summary");

	}
	catch(Exception e)
	{
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
		ShowExp(sc, e);
		return false;
	}
	PrintSalesSummary();
/////query Deposit Summary///

/*	sc = " SET DATEFORMAT dmy SELECT td.trans_date AS 'Deposit Date', c.name AS 'Staff Name', e.name AS 'Payment Method', td.payment_ref AS 'Payment Reference', ISNULL(ti.amount_applied,0) AS Amount ";
	sc += " FROM trans t JOIN tran_detail td ON td.id = t.id ";
	sc += " JOIN tran_invoice ti ON ti.tran_id = t.id ";
	sc += " JOIN card c ON c.id = td.staff_id ";
	sc += " JOIN enum e ON e.id = td.payment_method AND Lower(e.class) = 'payment_method' ";
	sc += "WHERE ti.purchase = 0 ";
	sc += m_dateSql;
	if(m_branchID != "" && m_branchID != null && TSIsDigit(m_branchID))
	{
		sc += " AND t.branch = "+ m_branchID;
	}
	sc += " ORDER BY td.trans_date ";
	*/
	sc = " SET DATEFORMAT dmy SELECT td.deposit_date AS 'Deposit Date', c.name AS 'Staff Name', 'Deposit' AS 'Payment Method', td.ref AS 'Payment Reference', ISNULL(td.total,0) AS Amount ";
	sc += " FROM tran_deposit td ";	
	sc += " LEFT OUTER JOIN card c ON c.id = td.staff ";	
	sc += "WHERE 1=1 ";
	sc += depSQL;
//DEBUG("deposit=", sc);	
	try
	{  
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "deposit_summary");

	}
	catch(Exception e)
	{		
		ShowExp(sc, e);
		return false;
	}
	PrintDepositSummary();

	/////query purchase summary////
	sc = " SET DATEFORMAT dmy SELECT p.gst_rate AS 'GST Rate', ISNULL(sum(p.total_amount * p.exchange_rate),0) AS 'Total Purchase' ";
	sc += ", ISNULL(sum(p.tax * p.exchange_rate),0) AS 'Total GST', ISNULL((SELECT SUM(pp.total_amount * pp.exchange_rate) from purchase pp WHERE pp.gst_rate = p.gst_rate AND type=4 AND status = 2 "+ ppoSQL +" ),0) AS 'Billed' ";
	sc += " , ISNULL((SELECT SUM(pp.total_amount * pp.exchange_rate) from purchase pp WHERE pp.gst_rate = p.gst_rate AND type=2 AND status = 2 "+ ppoSQL +" ),0) AS 'Unbilled' ";
	sc += " FROM purchase p ";
	sc += "WHERE p.type in (2,4) AND status = 2 ";
	sc += poSQL;
	if(m_branchID != "" && m_branchID != null && TSIsDigit(m_branchID))
	{
		sc += " AND p.branch_id = "+ m_branchID;
	}
	sc += " GROUP BY p.gst_rate ";
//DEBUG("purchase=", sc);	
	try
	{  
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "purchase_summary");

	}
	catch(Exception e)
	{		
		ShowExp(sc, e);
		return false;
	}
	PrintPurchaseSummary();

	////Payment Summary///

	sc = " SET DATEFORMAT dmy SELECT td.trans_date AS 'Payment Date', c.company AS 'Supplier', e.name AS 'Payment Method', td.payment_ref AS 'Payment Reference', ISNULL(ti.amount_applied,0) AS Amount";
	sc += " FROM trans t JOIN tran_detail td ON td.id = t.id ";
	sc += " JOIN tran_invoice ti ON ti.tran_id = t.id ";
	sc += " JOIN purchase p ON p.id = ti.invoice_number ";
	sc += " JOIN card c ON c.id = p.supplier_id ";
	sc += " JOIN enum e ON e.id = td.payment_method AND Lower(e.class) = 'payment_method' ";
	sc += "WHERE ti.purchase = 1 ";
	sc += m_dateSql;
	if(m_branchID != "" && m_branchID != null && TSIsDigit(m_branchID))
	{
		sc += " AND t.branch = "+ m_branchID;
	}
//DEBUG("payment=", sc);	
	try
	{  
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "payment_summary");

	}
	catch(Exception e)
	{		
		ShowExp(sc, e);
		return false;
	}
	PrintPaymentSummary();


	////Stock Value ////

	sc = " SET DATEFORMAT dmy SELECT getdate() AS 'Current Date', ISNULL(SUM(c.average_cost * sq.qty),0) AS 'Stock Value' ";
	sc += " FROM stock_qty sq JOIN code_relations c ON c.code = sq.code ";
	if(m_branchID != "" && m_branchID != null && TSIsDigit(m_branchID))
	{
		sc += " WHERE sq.branch_id = "+ m_branchID;
	}
//DEBUG("stock=", sc);	
	try
	{  
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "stock_summary");

	}
	catch(Exception e)
	{		
		ShowExp(sc, e);
		return false;
	}
	PrintStockSummary();

	if(Request.Form["cmd"] == "Export Report")
	{
		bool bRet = true;
		bRet = EmptyBackupFolder();
		bRet = WriteCSVFile(ds);
			
		if(bRet)
		{
			Response.Write("<br><h4>Zipping data files, please wait...");
			Response.Flush();
			bRet = ZipDir(m_path, "invoice_data_" + DateTime.Now.ToString("dd_MM_yy_HH_mm") + ".zip");
			Response.Write("done.</h4>\r\n");
		}
		//clean up csv files
		if(Directory.Exists(m_path))
		{
			string[] files = Directory.GetFiles(m_path, "*.csv");
			for(int i=0; i<files.Length; i++)
			{
				if(File.Exists(files[i]))
					File.Delete(files[i]);
			}
		}

		if(bRet)
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"]+ "?export=done\">");
			//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
			return true;
		}
		return true;
	}
//	else
//		PrintSummary();
	return true;
}

string GetBranchName(string id)
{
	if(ds.Tables["branch_name"] != null)
		ds.Tables["branch_name"].Clear();
if(id != "" && id != null)
{
	string sc = " SELECT name FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "branch_name") == 1)
			return ds.Tables["branch_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
}
	return "";

}

void PrintSummary()
{
	string sBranchName = GetBranchName(m_branchID);
	if(sBranchName == "")
		sBranchName = "ALL";
	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
	if(m_type != "" && m_type != null)
		Response.Write("&t="+ m_type);
	//if(m_nPeriod != null && m_nPeriod != "")
		Response.Write("&pr="+ m_nPeriod);
	
	Response.Write("' method=post>");
	Response.Write("<input type=hidden name=type value="+ m_type +">");
	Response.Write("<input type=hidden name=hCustomerID value="+ m_customerID +">");
	Response.Write("<center><table width='"+ tableWidth +"'>");
	Response.Write("<tr><td><font size=+1><b>" + m_sCompanyName.ToUpper() + "</b></font>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b><i>" + sBranchName.ToUpper() + " Branch</i></b><br>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");
	Response.Write("<tr><td align=center><font size=+1>");
	
	Response.Write("Transaction Summary</font>");
	if(m_paymentType != "")
		Response.Write("&nbsp;&nbsp;<b> -- " + GetEnumValue("payment_method", m_paymentType).ToUpper() + "</b>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Date Period : " + m_datePeriod + "</b></td></tr>");
//	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");

	int rows = ds.Tables["report"].Rows.Count;

	Response.Write("<tr><td width='"+ tableWidth +"'>");
	Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	if(m_bDealerLevel)
		Response.Write("<th align=left>Company</th>");
	else
		Response.Write("<th align=left>Customer</th>");
	Response.Write("<th>Invoice#</th>");
	Response.Write("<th>Invoiced Date</th>");
	Response.Write("<th>Received Date</th>");
//	Response.Write("<th align=left>Customer</th>");
	Response.Write("<th align=right>InvoiceTotal</th>");
	
//	Response.Write("<th align=right>Credit</th>");
	Response.Write("<th align=right>Cash</th>");
	Response.Write("<th align=right>DirectDebit</th>");
	Response.Write("<th align=right>CreditCard</th>");
	Response.Write("<th align=right>Eftpos</th>");
	Response.Write("<th align=right>CashOut</th>");
	Response.Write("<th align=right>Cheque</th>");
	
	Response.Write("<th align=right>PaymentTotal</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	double dTotal = 0;
	double dCash = 0;
	double dCashOut = 0;
	double dEftpos = 0;
	double dCC = 0;
	double dCredit = 0;
	double dCheque = 0;
	double dDirectDebit = 0;
	double dCashTotal = 0;
	double dCashOutTotal = 0;
	double dEftposTotal = 0;
	double dCCTotal = 0;
	double dChequeTotal = 0;
	double dSum = 0;
	double dPaymentTotal = 0;
	double dCreditTotal = 0;
	double dDirectDebitTotal = 0;
	string inv = "";
	string sdate = "";
	string inv_old = "";
	string customer = "";
	string customer_id = "";
	string received_date = "";
	string last_customer = "";
	string last_card_id = "";

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		inv = dr["invoice_number"].ToString();
		customer = dr["Customer"].ToString();

		if(customer == "" || customer == null)
			customer = dr["company"].ToString();
		else if(customer == "" || customer == null)
			customer = dr["custID"].ToString();
		if(m_bDealerLevel)
		{
			customer = dr["company"].ToString();
			if(customer == "" || customer == null)
				customer = dr["Customer"].ToString();
			else if(customer == "" || customer == null)
				customer = dr["custID"].ToString();
		}
	//	customer = dr["customer"].ToString();
		customer_id = dr["custID"].ToString();
		
		if(inv != inv_old)
		{
			if(inv_old != "")
			{
				dPaymentTotal = dCash + dEftpos + dCC + dCheque - dCashOut + dDirectDebit;
				Response.Write("<tr");
				if(Math.Round(dPaymentTotal - dTotal, 2) != 0)
					Response.Write(" bgcolor=#F5362E");
				else if(bAlterColor)
					Response.Write(" bgcolor=#EEEEEE");
				Response.Write(">");
				bAlterColor = !bAlterColor;				
		
			Response.Write("<td wrap><a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + last_card_id + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>" + last_customer + "</a></td>");
				//Response.Write("<td wrap>" + customer + "</td>");
				Response.Write("<td><a href=invoice.aspx?id=" + inv_old + " class=o target=new>" + inv_old + "</a></td>");
				Response.Write("<td wrap>" + sdate + "</td>");
				Response.Write("<td wrap>" + received_date + "</td>");
//				Response.Write("<td>" + customer + "</td>");
				Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
				
//				Response.Write("<td align=right>" + (dCredit==0 ? "" : dCredit.ToString("c")) + "</td>");
				Response.Write("<td align=right>" + (dCash==0 ? "" : dCash.ToString("c")) + "</td>");
				Response.Write("<td align=right>" + (dDirectDebit==0 ? "" : dDirectDebit.ToString("c")) + "</td>");
				Response.Write("<td align=right>" + (dCC==0 ? "" : dCC.ToString("c")) + "</td>");
				Response.Write("<td align=right>" + (dEftpos==0 ? "" : dEftpos.ToString("c")) + "</td>");
				Response.Write("<td align=right>" + (dCashOut==0 ? "" : dCashOut.ToString("c")) + "</td>");
				Response.Write("<td align=right>" + (dCheque==0 ? "" : dCheque.ToString("c")) + "</td>");
	
				Response.Write("<td align=right>" + dPaymentTotal.ToString("c") + "</td>");
				Response.Write("</tr>");
			}
			
			dCashTotal += dCash;
			dCashOutTotal += dCashOut;
			dEftposTotal += dEftpos;
			dCCTotal += dCC;
			dChequeTotal += dCheque;
			dDirectDebitTotal += dDirectDebit;
			dCreditTotal += dCredit;
			dSum += dTotal;

			dCash = 0;
			dCashOut = 0;
			dEftpos = 0;
			dCC = 0;
			dCheque = 0;
			dCredit = 0;
			dDirectDebit = 0;
			inv_old = inv;
			if(last_customer != customer)
			{
				last_customer = customer;
				last_card_id = customer_id;
			}
		}

		sdate = DateTime.Parse(dr["invoice_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		received_date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string payment_type = dr["name"].ToString().ToUpper();
		dTotal = MyDoubleParse(dr["total"].ToString());
		double dAmount = MyDoubleParse(dr["amount"].ToString());
		string AmountInv = dr["amount2"].ToString();
//	DEBUG("AmountInv = ",AmountInv.ToString());
		if(AmountInv != null && AmountInv != "")
			dAmount = MyDoubleParse(AmountInv);
		
		if(inv == "0")
		{
			dCredit += dAmount;
			dTotal = dCredit;
		}
			
		if(payment_type == "CASH")
		{
			if(dAmount < 0 && dTotal >= 0)
				dCashOut -= dAmount;
			else
				dCash += dAmount;
		}
	//	else if(payment_type == "DIRECT DEBIT")
		else if(payment_type == "DEPOSIT")
		{
			dDirectDebit += dAmount;
		//	dTotal += dTotal;
		}
		else if(payment_type == "EFTPOS")
			dEftpos += dAmount;
		else if(payment_type == "CREDIT CARD")
			dCC += dAmount;
		else if(payment_type == "CHEQUE")
			dCheque += dAmount;
//		else if(payment_type == "CREDIT")
//			dCreditTotal += dCredit;
//DEBUG("payemt +", dCash);			
//	DEBUG(" tyep =", payment_type);
	}

	//the last row
	dPaymentTotal = dCash + dEftpos + dCC + dCheque - dCashOut + dDirectDebit; // - dCredit;
	Response.Write("<tr");
	if(Math.Round(dPaymentTotal - dTotal, 2) != 0)
		Response.Write(" bgcolor=#F5362E");
	else if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");
	bAlterColor = !bAlterColor;
	Response.Write("<td wrap><a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + last_card_id + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>" + last_customer + "</a></td>");
	Response.Write("<td><a href=invoice.aspx?id=" + inv_old + " class=o target=new>" + inv_old + "</a></td>");
	Response.Write("<td nowrap>" + sdate + "</td>");
	Response.Write("<td nowrap>" + received_date + "</td>");

//	Response.Write("<td align=right></td>");
	Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
	
//	Response.Write("<td align=right>" + (dCredit==0 ? "" : dCredit.ToString("c")) + "</td>");
	Response.Write("<td align=right>" + (dCash==0 ? "" : dCash.ToString("c")) + "</td>");
	Response.Write("<td align=right>" + (dDirectDebit==0 ? "" : dDirectDebit.ToString("c")) + "</td>");
	Response.Write("<td align=right>" + (dCC==0 ? "" : dCC.ToString("c")) + "</td>");
	Response.Write("<td align=right>" + (dEftpos==0 ? "" : dEftpos.ToString("c")) + "</td>");
	Response.Write("<td align=right>" + (dCashOut==0 ? "" : dCashOut.ToString("c")) + "</td>");
	Response.Write("<td align=right>" + (dCheque==0 ? "" : dCheque.ToString("c")) + "</td>");
	
	Response.Write("<td align=right>" + dPaymentTotal.ToString("c") + "</td>");
	Response.Write("</tr>");

	dCashTotal += dCash;
	dCashOutTotal += dCashOut;
	dEftposTotal += dEftpos;
	dCCTotal += dCC;
	dChequeTotal += dCheque;
//	dCreditTotal += dCredit;
	dDirectDebitTotal += dDirectDebit;
	dSum += dTotal;

	Response.Write("<tr>");
	Response.Write("<td colspan=4 align=right><b>Total : </b></td>");
//	Response.Write("<td align=right><b></b></td>");
	Response.Write("<td align=right><b>" + dSum.ToString("c") + "</b></td>");
	
//	Response.Write("<td align=right><b>" + dCreditTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dCashTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dDirectDebitTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dCCTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dEftposTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dCashOutTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dChequeTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + (dCashTotal + dEftposTotal + dCCTotal + dChequeTotal - dCashOutTotal).ToString("c") + "</b></td>");
	Response.Write("</tr>");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("<th>&nbsp;</th>");
//	Response.Write("<th align=right></th>");
	Response.Write("<th align=right>InvoiceTotal</th>");
	
//	Response.Write("<th align=right>Credit</th>");
	Response.Write("<th align=right>Cash</th>");
	Response.Write("<th align=right>DirectDebit</th>");
	Response.Write("<th align=right>CreditCard</th>");
	Response.Write("<th align=right>Eftpos</th>");
	Response.Write("<th align=right>CashOut</th>");
	Response.Write("<th align=right>Cheque</th>");
	Response.Write("<th align=right>PaymentTotal</th>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</td></tr></table>");

	if(!m_bPrint)
	{
		Response.Write("<input type=submit name=cmd value='Export Report' " + Session["button_style"] + " onclick=\"return confirm('Export invoice report!!!');\">");
		Response.Write("<input type=button value='Printable Version'  "+ Session["button_style"] +"  ");
		Response.Write(" onclick=window.open('"+ Request.ServerVariables["URL"] +"?pm=" + m_paymentType + "&print=1&pr="+ m_nPeriod +"");
		if(m_sdFrom != null)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&branch="+ m_branchID +"");
		Response.Write("')>");
	}
	Response.Write("</form>");
}

bool WriteCSVFile(DataSet ds)
{
	Response.Write("Getting data from <b> Invoice </b> table ...");
	Response.Flush();
	
	StringBuilder sb = new StringBuilder();

	int i = 0;

	//write column names
	DataColumnCollection dc = ds.Tables[0].Columns;
	int cols = dc.Count;
	for(i=0; i<cols; i++)
	{
		if(i > 0)
			sb.Append(",");
		sb.Append(dc[i].ColumnName);
	}
	sb.Append("\r\n");

	//column data type
	for(i=0; i<cols; i++)
	{
		if(i > 0)
			sb.Append(",");
		sb.Append(dc[i].DataType.ToString().Replace("System.", ""));
	}
	sb.Append("\r\n");
	
	DataRow dr = null;

	for(i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		dr = ds.Tables[0].Rows[i];
		for(int j=0; j<cols; j++)
		{
			if(j > 0)
				sb.Append(",");
			string sValue = dr[j].ToString().Replace("\r\n", "@@eznz_return"); //encode line return in site_pages, kit...
			sValue = sValue.Replace("\r", "@@eznz_return"); //encode single return
			sValue = sValue.Replace("\n", "@@eznz_return"); //encode single return
			sValue = sValue.Replace("@@eznz_return", "\\r\\n");
			//if(sTableName == "site_pages" || sTableName == "site_sub_pages")
			//	sValue = sValue.Replace("?/", "</"); //strange error
			sb.Append("\"" + EncodeDoubleQuote(sValue) + "\"");
		}
		sb.Append("\r\n");
		MonitorProcess(10);
	}

	string strPath = m_path + "\\invoice.csv";

	Encoding enc = Encoding.GetEncoding("iso-8859-1");
	byte[] Buffer = enc.GetBytes(sb.ToString());

	//create file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();

	Response.Write("done<br>\r\n");
	Response.Flush();
	return true;
}

bool ZipDir(string dirName, string zipFileName)
{
	string[] filenames = Directory.GetFiles(dirName);
	
	Crc32 crc = new Crc32();
	ZipOutputStream s = new ZipOutputStream(File.Create(m_path + "\\" + zipFileName));
	
	s.SetLevel(9); // 0 - store only to 9 - means best compression
	
	long maxLength = 2048000; //2mb file
	long len = 0;
	int files = 1;
	foreach (string file in filenames) 
	{
		if(s.Length >= maxLength)
		{
			s.Finish();
			s.Close();
			s = new ZipOutputStream(File.Create(m_path + "\\" + zipFileName.Replace(".zip", "") + "_" + files.ToString() + ".zip"));
			s.SetLevel(9); // 0 - store only to 9 - means best compression
			files++;
			len = 0;
		}
//		string file = Server.MapPath("./download/" + m_fileName);
		FileStream fs = File.OpenRead(file);
		byte[] buffer = new byte[fs.Length];
		fs.Read(buffer, 0, buffer.Length);
		ZipEntry entry = new ZipEntry(file);
		
		entry.DateTime = DateTime.Now;
		
		// set Size and the crc, because the information
		// about the size and crc should be stored in the header
		// if it is not set it is automatically written in the footer.
		// (in this case size == crc == -1 in the header)
		// Some ZIP programs have problems with zip files that don't store
		// the size and crc in the header.
		entry.Size = fs.Length;
		fs.Close();
		
		crc.Reset();
		crc.Update(buffer);
		
		entry.Crc  = crc.Value;
		
		s.PutNextEntry(entry);
		
		s.Write(buffer, 0, buffer.Length);
		len = buffer.Length; //total length
MonitorProcess(1);
	}
	
	s.Finish();
	s.Close();
	return true;
}
bool EmptyBackupFolder()
{
	if(Directory.Exists(m_path))
		Directory.Delete(m_path, true);

	Directory.CreateDirectory(m_path);
	return true;
}

bool doSearchCustomer(string searchValue)
{	
	string sc = " SELECT * FROM card ";
	sc += " WHERE 1=1 ";
	if(TSIsDigit(searchValue))
		sc += " AND id = "+ searchValue;
	else
		sc += " AND (name = '"+ EncodeQuote(searchValue) +"' OR phone = '"+ EncodeQuote(searchValue) +"'  OR company = '"+ EncodeQuote(searchValue) +"'  OR fax = '"+ EncodeQuote(searchValue) +"'  OR email = '"+ EncodeQuote(searchValue) +"' )";
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "customerList");				
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
	{
		m_customerID = ds.Tables["customerList"].Rows[0]["id"].ToString();
		m_customerName = ds.Tables["customerList"].Rows[0]["name"].ToString();
		m_customerEmail = ds.Tables["customerList"].Rows[0]["email"].ToString();
		m_customerPhone = ds.Tables["customerList"].Rows[0]["phone"].ToString();
	}
	
		
	return true;
}

void PrintSalesSummary()
{
	string sBranchName = GetBranchName(m_branchID);
	if(sBranchName == "")
		sBranchName = "ALL";
//	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
//	if(m_type != "" && m_type != null)
//		Response.Write("&t="+ m_type);
	//if(m_nPeriod != null && m_nPeriod != "")
//		Response.Write("&pr="+ m_nPeriod);
	
//	Response.Write("' method=post>");
	Response.Write("<input type=hidden name=type value="+ m_type +">");
	Response.Write("<input type=hidden name=hCustomerID value="+ m_customerID +">");
	Response.Write("<center><table width='"+ tableWidth +"' border=0>");
	Response.Write("<tr><td><font size=+1><b>" + m_sCompanyName.ToUpper() + "</b></font>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b><i>" + sBranchName.ToUpper() + " Branch</i></b><br>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>");
//	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b></td></tr>");
	Response.Write("<tr><td><hr width='100%'></td></tr>");
	Response.Write("<tr><td align=left><font size=+1>");	
	Response.Write("Sales Summary</font>");
	if(m_paymentType != "")
		Response.Write("&nbsp;&nbsp;<b> -- " + GetEnumValue("payment_method", m_paymentType).ToUpper() + "</b>");
	Response.Write("</td></tr>");
	
//	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");
	int rows = ds.Tables["sales_summary"].Rows.Count;

	Response.Write("<tr><td width='100%'>");
	Response.Write("<table width='100%'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	Response.Write("<th align=right width='20%'>Total Sales NO GST</th>");
	Response.Write("<th align=right width='20%'>Total GST</th>");
	Response.Write("<th align=right>Total Sales</th>");		
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		Response.Write("</td></tr></table>");
		return;
	}

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["sales_summary"].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[0].ToString()).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[1].ToString()).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[2].ToString()).ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	
	Response.Write("</td></tr></table>");
	

}

void PrintDepositSummary()
{
	string sBranchName = GetBranchName(m_branchID);
	if(sBranchName == "")
		sBranchName = "ALL";

	Response.Write("<center><table width='"+ tableWidth +"' border=0>");	
	Response.Write("<tr><td><hr width='100%'></td></tr>");
	Response.Write("<tr><td align=left><font size=+1>");	
	Response.Write("Deposit Summary</font>");		
//	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");
	int rows = ds.Tables["deposit_summary"].Rows.Count;

	Response.Write("<tr><td width='100%'>");
	Response.Write("<table width='100%'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	Response.Write("<th align=right width='20%'>Deposit Date</th>");
	Response.Write("<th align=right width='20%'>Staff Name</th>");
//	Response.Write("<th align=right width='20%'>Deposit Method</th>");
	Response.Write("<th align=right width='40%'>Deposit Reference</th>");					
	Response.Write("<th align=right width='20%'>Deposit Amount</th>");		
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		Response.Write("</td></tr></table>");
		return;
	}

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["deposit_summary"].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;
		Response.Write("<td align=right>" + dr[0].ToString() + "</td>");
		Response.Write("<td align=right>" + dr[1].ToString() + "</td>");
		//Response.Write("<td align=right>" + dr[2].ToString() + "</td>");
		Response.Write("<td align=right>" + dr[3].ToString() + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[4].ToString()).ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr></table>");

}

void PrintPurchaseSummary()
{
	string sBranchName = GetBranchName(m_branchID);
	if(sBranchName == "")
		sBranchName = "ALL";

	Response.Write("<center><table width='"+ tableWidth +"' border=0>");	
	Response.Write("<tr><td><hr width='100%'></td></tr>");
	Response.Write("<tr><td align=left><font size=+1>");	
	Response.Write("Purchase Summary</font>");
	if(m_paymentType != "")
		Response.Write("&nbsp;&nbsp;<b> -- " + GetEnumValue("payment_method", m_paymentType).ToUpper() + "</b>");
	Response.Write("</td></tr>");	
//	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");
	int rows = ds.Tables["purchase_summary"].Rows.Count;

	Response.Write("<tr><td width='100%'>");
	Response.Write("<table width='100%'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	Response.Write("<th align=right width='20%'>GST Rate</th>");
	Response.Write("<th align=right width='20%'>Total Purchase</th>");
	Response.Write("<th align=right width='20%'>Total GST</th>");
	Response.Write("<th align=right width='20%'>Billed</th>");		
	Response.Write("<th align=right width='20%'>Unbilled</th>");				
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		Response.Write("</td></tr></table>");
		return;
	}

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["purchase_summary"].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[0].ToString()).ToString("p") + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[1].ToString()).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[2].ToString()).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[3].ToString()).ToString("c") + "</td>");		
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[4].ToString()).ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr></table>");

}

void PrintPaymentSummary()
{
	string sBranchName = GetBranchName(m_branchID);
	if(sBranchName == "")
		sBranchName = "ALL";

	Response.Write("<center><table width='"+ tableWidth +"' border=0>");	
	Response.Write("<tr><td><hr width='100%'></td></tr>");
	Response.Write("<tr><td align=left><font size=+1>");	
	Response.Write("Payment Summary</font>");
	if(m_paymentType != "")
		Response.Write("&nbsp;&nbsp;<b> -- " + GetEnumValue("payment_method", m_paymentType).ToUpper() + "</b>");
	Response.Write("</td></tr>");	
//	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");
	int rows = ds.Tables["payment_summary"].Rows.Count;

	Response.Write("<tr><td width='100%'>");
	Response.Write("<table width='100%'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	Response.Write("<th align=right width='20%'>Payment Date</th>");
	Response.Write("<th align=right width='20%'>Supplier</th>");
	Response.Write("<th align=right width='20%'>Payment Method</th>");
	Response.Write("<th align=right width='20%'>Payment Reference</th>");
	Response.Write("<th align=right width='20%'>Amount</th>");	
	
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		Response.Write("</td></tr></table>");
		return;
	}

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["payment_summary"].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;
		Response.Write("<td align=right>" + dr[0].ToString() + "</td>");
		Response.Write("<td align=right>" + dr[1].ToString() + "</td>");
		Response.Write("<td align=right>" + dr[2].ToString() + "</td>");
		Response.Write("<td align=right>" + dr[3].ToString() + "</td>");		
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[4].ToString()).ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr></table>");

}
void PrintStockSummary()
{
	string sBranchName = GetBranchName(m_branchID);
	if(sBranchName == "")
		sBranchName = "ALL";

	Response.Write("<center><table width='"+ tableWidth +"' border=0>");	
	Response.Write("<tr><td><hr width='100%'></td></tr>");
	Response.Write("<tr><td align=left><font size=+1>");	
	Response.Write("Stock Value Summary</font>");
	if(m_paymentType != "")
		Response.Write("&nbsp;&nbsp;<b> -- " + GetEnumValue("payment_method", m_paymentType).ToUpper() + "</b>");
	Response.Write("</td></tr>");	
//	Response.Write("<tr><td><hr width='"+ tableWidth +"'></td></tr>");
	int rows = ds.Tables["stock_summary"].Rows.Count;

	Response.Write("<tr><td width='100%'>");
	Response.Write("<table width='100%'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#6699CC;font-weight:bold;\">");
	Response.Write("<th align=right width='20%'>Current Date</th>");
	Response.Write("<th align=right>Stock Value</th>");
	//Response.Write("<th align=right>Payment Reference</th>");
	//Response.Write("<th align=right>Amount</th>");	
	
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		Response.Write("</td></tr></table>");
		return;
	}

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["stock_summary"].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;
		Response.Write("<td align=right >" + dr[0].ToString() + "</td>");
		Response.Write("<td align=right>" + MyCurrencyPrice(dr[1].ToString()).ToString("c") + "</td>");
	//	Response.Write("<td align=right>" + dr[2].ToString() + "</td>");
	//	Response.Write("<td align=right>" + dr[3].ToString() + "</td>");		
		//Response.Write("<td align=right>" + dr[4].ToString() + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr></table>");

}

</script>

<asp:Label id=LFooter runat=server/>
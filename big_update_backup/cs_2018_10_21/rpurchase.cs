<!-- #include file="page_index.cs" -->
<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>
<script runat=server>

string m_branchID = "";
string m_type = "";
string m_tableTitle = "Agent Commission Summary";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
DataRow[] m_dra = null;
string[] m_EachMonth = new string[16];

string m_sorted = "DESC";

string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
int m_nPeriod = 0;

string m_agent_id = "";
string m_agent = "";

bool m_bPickTime = false;
bool m_bSltBoth = false; //item details and customer are selected
bool m_bPrint = false;
bool m_bIsAgent = true;

string m_path = "";
string m_export = "";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Request.QueryString["sales"] == "1")
		m_bIsAgent = false;

	if(Request.QueryString["export"] != null)
		m_export = Request.QueryString["export"];

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
		Response.Write("<br><center><h3>Export Purchase Invoice Report Done</h3></center>");

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
			LFooter.Text = "<br><br><center><a href="+ Request.ServerVariables["URL"] +"";
			if(!m_bIsAgent)
				LFooter.Text += "?sales=1 ";
			LFooter.Text += " class=o>New Report</a>";
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
	}
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

//	if(Request.Form["cmd"] == "View Report")
//	{
//		if(!doShowAllPurchase())
//			return;
///			
//	}
	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		m_type = Request.QueryString["t"];

	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["print"] == null || Request.QueryString["print"] == "")
		{
			PrintMainPage();
			LFooter.Text = m_sAdminFooter;
			return;
		}
	/*	if(Request.QueryString["t"] == null || Request.QueryString["t"] == "")
		{
			PrintMainPage();
			LFooter.Text = m_sAdminFooter;
			return;
		}
		*/
		if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
			m_type = Request.QueryString["t"];
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
	if(Request.Form["type"] != null && Request.Form["type"] != "")
		m_type = Request.Form["t"];
//DEBUG(" type =", m_type);	
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
//		m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());

	Session["report_period"] = m_nPeriod;
	if(Request.Form["type"] != null && Request.Form["type"] != "")
		m_type = Request.Form["type"];
//DEBUG(" mteyp +", m_type);
//DEBUG("m_period =", m_nPeriod);
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yesterday";
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
	
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		m_agent_id = Request.QueryString["sid"];

	if(Request.QueryString["print"] != null)
		m_bPrint = true;

	PrintAdminHeader();
	if(!m_bPrint)
		PrintAdminMenu();
	DoPurchaseInvoiceSummary("");

	if(!m_bPrint)
	{
		LFooter.Text = "<br><br><center><a href="+ Request.ServerVariables["URL"] +"";
		if(!m_bIsAgent)
			LFooter.Text += "?sales=1 ";
		LFooter.Text += " class=o>New Report</a>";
	}
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
	if(!m_bIsAgent)
		Response.Write("&sales=1&t=as");
	
	Response.Write("' method=post>");

	Response.Write("<br><center><h3>Select Purchase Report</h3>");
	

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
//	if(m_bIsAgent)
//		Response.Write("<b>Select Travel Agent</b></td></tr>");
//	else
//		Response.Write("<b>Select Sales Person</b></td></tr>");

	string uri = Request.ServerVariables["URL"].ToString();
	
	Response.Write("<tr><th align=left >");

	if(Session["branch_support"] != null)
	{
		Response.Write("<b>Branch : </b></td><td>");
		PrintBranchNameOptions(m_branchID, "", true);
	//	PrintBranchNameOptions(m_branchID, ""+ Request.ServerVariables["URL"] +"?branch=");
	}
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left >");
	Response.Write("Purchase Invoice Type: </td><td><select name=type><option value=''>all</option>");
	Response.Write("<option value=1>Created</option>");
//	Response.Write("<option value=2>Received</option>");
	Response.Write("<option value=4>Billed</option>");	
//	Response.Write("<option value=5>Open Billed</option>");
//	Response.Write("<option value=3>Back Order</option>");
	Response.Write("<option value=6>Deleted</option>");
	//Response.Write(GetEnumOptions("purchase_order_status", m_type));
	Response.Write("</select>\r\n");
	
	
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Select Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Yesterday</td></tr>");
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
	Response.Write("<input type=submit name=cmd value='Export Report' " + Session["button_style"] + " onclick=\"return confirm('Export purchase invoice report!!!');\">");
	Response.Write("<input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	LFooter.Text = m_sAdminFooter;
}

bool DoPurchaseInvoiceSummary(string status)
{
//	if(m_bIsAgent)
///		m_tableTitle = "Travel Agent Commission Summary";
//	else
		m_tableTitle = "Purchase Invoice Summary";

	switch(m_nPeriod)
	{
	case 0:
		if(m_type == "3")
			m_dateSql = " AND DATEDIFF(day, i.date_recieved, GETDATE()) = 0 ";
		else
			m_dateSql = " AND DATEDIFF(day, i.date_create, GETDATE()) = 0 ";
		break;
	case 1:
		if(m_type == "3")
			m_dateSql = " AND DATEDIFF(day, i.date_invoiced, GETDATE()) = 1 ";
		else
			m_dateSql = " AND DATEDIFF(day, i.date_create, GETDATE()) = 1 ";
		break;
	case 2:
		if(m_type == "3")
			m_dateSql = " AND DATEDIFF(week, i.date_invoiced, GETDATE()) = 0 ";
		else
			m_dateSql = " AND DATEDIFF(week, i.date_create, GETDATE()) = 0 ";
		break;
	case 3:
		if(m_type == "3")
			m_dateSql = " AND DATEDIFF(month, i.date_invoiced, GETDATE()) = 0 ";
		else
			m_dateSql = " AND DATEDIFF(month, i.date_create, GETDATE()) = 0 ";

		break;
	case 4:
		if(m_type == "3")
			m_dateSql = " AND i.date_invoiced BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		else
			m_dateSql = " AND i.date_create BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = " ";

//	if(m_type == "as")
	{
		sc = " SET DATEFORMAT dmy ";
		sc += " SELECT i.type, i.status, i.po_number, i.inv_number ";
		if(m_type == "4")
			sc += " , i.date_invoiced ";
		else
			sc += ", i.date_create ";
		sc += " AS invoice_date ";
		sc += " ,  c.trading_name, c.id AS supplier_id, i.total_amount, i.exchange_rate ";
		sc += " FROM purchase i ";
		
		sc += " JOIN card c ON c.id = i.supplier_id ";
		sc += " WHERE 1=1 " + m_dateSql;
		if(Session["branch_support"] != null && m_branchID != "0")
			sc += " AND i.branch_id = " + m_branchID;
		if(m_type == "1" )
			sc += " AND( i.type = 2 OR i.type = 1 ) AND (i.status = 2 OR i.status = 1) ";
		else if(m_type == "6" )
			sc += " AND i.type = 2 AND i.status = 4 ";
		else if(m_type == "4")
			sc += " AND i.type = 4 AND i.status = 2 ";
		//else
		//	sc += " AND( i.type = 2 OR i.type = 1 ) AND (i.status = 2 OR i.status = 1) ";
		
		sc += " ORDER BY ";
		sc += " i.id ";		
//		if(m_type == "4")
//			sc += " i.date_invoiced  ";
//		else
//			sc += " i.date_create ";
	}

	
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

	if(Request.Form["cmd"] == "Export Report")
	{
		bool bRet = true;
		bRet = EmptyBackupFolder();
		bRet = WriteCSVFile(ds);
			
		if(bRet)
		{
			Response.Write("<br><h4>Zipping data files, please wait...");
			Response.Flush();
			bRet = ZipDir(m_path, "purchase_data_" + DateTime.Now.ToString("dd_MM_yy_HH_mm") + ".zip");
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
		}
		return true;
	}
	else		
		PrintPurchaseSummary(status);
	return true;
}
/////////////////////////////////////////////////////////////////
void PrintPurchaseSummary(string status)
{
	m_dra = ds.Tables["report"].Select("", "po_number DESC");

	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
	if(m_type != "" && m_type != null)
		Response.Write("&t="+ m_type);
	//if(m_nPeriod != null && m_nPeriod != "")
		Response.Write("&pr="+ m_nPeriod);
	
	Response.Write("' method=post>");
	Response.Write("<input type=hidden name=type value="+ m_type +">");
	Response.Write("<br><center><h4>");
//DEBUG("m_type =", m_type);	
	Response.Write("Purchase Invoice");
	Response.Write(" Report</h4>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	int i = 0;
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = m_dra.Length;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(m_type != null)
		m_cPI.URI += "&type="+ m_type;	
	m_cPI.URI += "&pr="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=95%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>SUPPLIER</th>");
	Response.Write("<th>SYS_PONUMBER</th>");
	Response.Write("<th>INV_NUMBER</th>");
	Response.Write("<th>DATE</th>");
	Response.Write("<th>STUTUS</th>");
	//Response.Write("<th>TOTAL ORDERS</th>");
	Response.Write("<th align=right>TOTAL AMOUNT</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	double dTotalQTY = 0;
	string lastSupp = "";
	if(m_bPrint)
		i=0;
	for(; i<rows && i<end; i++)
	{
		DataRow dr = m_dra[i];
		Response.Write("<tr align=left ");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		string supplier_id = dr["supplier_id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string po_number = dr["po_number"].ToString();
		string inv_number = dr["inv_number"].ToString();
		string inv_date = dr["invoice_date"].ToString();
		string total_amount = dr["total_amount"].ToString();
		string exRate = dr["exchange_rate"].ToString();
		double amount = 0;
		double dExRate = MyDoubleParse(exRate);
		if(dExRate == 0)
			amount = double.Parse(total_amount);
		else
			amount = double.Parse(total_amount) / dExRate;
			
		//string amount = dr["amount"].ToString();
		dTotalNoGST += amount;		
		string type = dr["type"].ToString();
		string sstatus = dr["status"].ToString();
		Response.Write("<td>&nbsp;&nbsp;<a title='click me to view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id="+supplier_id+"','', 'width=350,height=350'); viewcard_window.focus();\" class=o>");
		
		//if(lastSupp != trading_name)
			Response.Write(trading_name);
		Response.Write("</a></td>");
		Response.Write("<td align=left>");
		if(m_type == "4")
			Response.Write("<a title='click me to view purchase invoice' href=\"javascript:purchase_window=window.open('purchase.aspx?t=pp&n="+ po_number+"','', 'scrollbar=1, resizeable=1'); viewcard_window.focus();\" class=o>");
		Response.Write("" + po_number + "</a></td>");
		Response.Write("<td align=left>" + inv_number + "</td>");
		Response.Write("<td align=left>" + DateTime.Parse(inv_date).ToString("dd/MM/yyyy HH:MM ") + "</td>");
//	DEBUG("type ="+ type, "status ="+ sstatus);	
		
		if(type == "2" && sstatus == "4")
			Response.Write("<td align=left><font color=red>Deleted</font></td>");
		else if(type == "4" && sstatus == "2")
			Response.Write("<td align=left><font color=green>Billed</font></td>");
		else
			//if(type == "1" && sstatus == "1") )
			Response.Write("<td align=left><font color=blue>Created/Received</font></td>");
		
		Response.Write("<td align=right>" + (amount).ToString("c") + "</td>");
		Response.Write("</tr>");
		lastSupp = trading_name;
	}

	
	Response.Write("<tr align=right><td colspan=5><b>SUB Total:</b></td><td> <font color=Green size=2>"+ (dTotalNoGST).ToString("c") +"</font></td><tr>");
	/*Response.Write("<br><center>");
	Response.Write("<b> Total QTY: </b><font color=red size=2>"+ dTotalQTY.ToString() +"</font>");
	Response.Write("&nbsp;&nbsp;<b>Total AMOUNT:</b> <font color=Green size=2>"+ dTotalNoGST.ToString("c") +"</font>");
	Response.Write(" <b>SUB Total:</b> <font color=Green size=2>"+ (dTotalNoGST).ToString("c") +"</font>");
	Response.Write("</center>");
	*/
	if(!m_bPrint)
		Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

	if(!m_bPrint)
	{
		Response.Write("<input type=submit name=cmd value='Export Report' " + Session["button_style"] + " onclick=\"return confirm('Export purchase invoice report!!!');\">");
		Response.Write("<input type=button value='Printable Version' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.open('rpurchase.aspx?print=1&pr="+ m_nPeriod +"&t="+ m_type +"&pr="+ m_nPeriod +"");
		if(m_sdFrom != null)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&branch="+ m_branchID +"");
		Response.Write("')\">");
	}
	Response.Write("</form>");

}
bool doShowAllPurchase()
{
	return doShowAllPurchase(false);
}
bool doShowAllPurchase(bool bIsAgent)
{
	string sc = " SELECT name, trading_name, company, contact, phone, id, sales ";
	sc += " FROM card WHERE 1=1 ";
	if(bIsAgent)
		sc += " AND type = 2 ";
	else
		sc += " AND type = 3 ";
	sc += " ORDER BY name, trading_name";
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "agents"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool WriteCSVFile(DataSet ds)
{
	Response.Write("Getting data from <b> Purchase </b> table ...");
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

	string strPath = m_path + "\\purchase.csv";

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


</script>

<asp:Label id=LFooter runat=server/>
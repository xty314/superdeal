<!-- #include file="page_index.cs" -->
<script runat=server>


DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_branch = "1";
string m_sReceiptPort = "LTP1";
string m_sdate = "0";
string m_branchName = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
		m_branch = Request.QueryString["b"];
	
		PrintAdminHeader();
	if(Request.QueryString["branchname"] != null && Request.QueryString["branchname"] != "")
		m_branchName = Request.QueryString["branchname"].ToString();
	if(Request.Form["sdate"] != null && Request.Form["sdate"] != "")
		m_sdate = Request.Form["sdate"];
	else if(Request.QueryString["sdate"] != null)
		m_sdate = Request.QueryString["sdate"];
	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");

//	PrintAdminMenu();
	
	setBranchOptions();
	if(Request.QueryString["s"] == "print" && Request.QueryString["b"] != null && Request.QueryString["b"] != "")
	{
		doPrintDailySummary();
		Response.Write("<script language=javascript>window.close();</script");
		Response.Write(">");
		return;
	}
	
//	Response.Write("<script language=javascript>if(confirm('Print Today Sales Summary')){ window.location=('"+ Request.ServerVariables["URL"]+"?s=print&r="+DateTime.Now.ToOADate() +"');}");
//	Response.Write("</script");
//	Response.Write(">");

	Response.Write("<br><br><br>");
//	if(m_branch != "" && m_branch != null)
		Response.Write("<input type=button value='Print Receipt' "+ Session["button_style"] +" onclick=\"window.open('"+ Request.ServerVariables["URL"]+"?s=print&b="+ m_branch +"&branchname="+ HttpUtility.UrlEncode(m_branchName) +"&sdate="+ m_sdate +"&r="+DateTime.Now.ToOADate() +"');\">");
	Response.Write("<input type=button value='Close Window' "+ Session["button_style"] +" onclick='window.close()'>");
	return;
	

}

bool setBranchOptions()
{
	string sc = " SELECT * FROM branch ";
	sc += " WHERE 1=1 AND activated = 1 ";

//	if(Session[m_sCompanyName + "AccessLevel"].ToString() != "10")
	if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
		m_branch = Session["branch_id"].ToString();
		if(m_branch != "")
		{
			if(TSIsDigit(m_branch))
				sc += " AND id ="+ m_branch +" ";
		}
	}

	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "branch");
		if(rows <= 0)
		{
//			Response.Write("<br><br><center><h3>ERROR, Order Not Found</h3>");
			return false;
		}
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

	Response.Write("<form action=grsum_rp.aspx?b="+ m_branch +"&sdate="+ m_sdate +" method=post>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr align=center><td>");
//	Response.Write("Tick View Daily Summary Only<input type=checkbox name=view>");
//DEBUG("m branch =", m_branch);	
	if(Session["branch_support"] != null && Session["branch_support"] != "")
	{
	Response.Write("Branch : <select name=branch onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?sdate="+ m_sdate +"&b='+ this.options[this.selectedIndex].value)\"");
	Response.Write(">");
//	if(Session[m_sCompanyName + "AccessLevel"].ToString() == "10")
	if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	Response.Write("<option value=all>All</option>");
	for(int i=0; i<rows; ++i)
	{
		string branch = dst.Tables["branch"].Rows[i]["id"].ToString();
		Response.Write("<option value=" + branch);
		if(branch == m_branch)
		{
			Response.Write(" selected");
			m_branchName = dst.Tables["branch"].Rows[i]["name"].ToString();
		}
		Response.Write(">"+ dst.Tables["branch"].Rows[i]["name"].ToString() +"</option>");
	}
	Response.Write("</select>");
	}
	else
	{
		Response.Write("<input type=hidden name=branch value=1>");
	}
	Response.Write("<select name=sdate onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?b="+ m_branch +"&sdate='+ this.options[this.selectedIndex].value)\"");	
	Response.Write("><option value=0 ");
	if(m_sdate == "0")
		Response.Write(" selected ");
	Response.Write(">Today</option>");
	Response.Write("<option value=1");
	if(m_sdate == "1")
		Response.Write(" selected ");
	Response.Write(">Yesterday</option></select>");
	Response.Write("<br><br><center><input type=submit value='Get Summary' "+ Session["button_style"] + ">");
//	Response.Write(" <input type=checkbox name=view value=1>View Only (no print out)");
	Response.Write("</td></tr>");
	
	Response.Write("</table>");
	Response.Write("</form>");

	Response.Write("<br><br>");
	string s = doBuildDailySummary();
	s = s.Replace("\r\n", "<br>\r\n");
	s = s.Replace("[/b]", "");
	Response.Write(s);
//	doPrintDailySummary();
	return true;
}

string doBuildDailySummary()
{
	if(dst.Tables["details"] != null)
		dst.Tables["details"].Clear();
	if(dst.Tables["summary"] != null)
		dst.Tables["summary"].Clear();

	int rows = 0;
	string sdate = "0";
	if(Request.Form["sdate"] != null)
		sdate = Request.Form["sdate"];
	else if(Request.QueryString["sdate"] != null && Request.QueryString["sdate"] != "")
		sdate = Request.QueryString["sdate"];
	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
		m_branch = Request.QueryString["b"];
	m_sdate = sdate;
	string s_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = " + sdate;
	
	string sc = "SET dateformat dmy ";
	sc += " SELECT i.invoice_number, sum(s.normal_price * s.quantity) AS gross ";
//	sc += ", sum(s.commit_price * s.quantity) AS total_sales ";
	sc += ", i.total AS total_sales, i.amount_paid AS total_paid ";

//	sc += ", sum(s.normal_price * s.quantity) - sum(s.commit_price * s.quantity) AS markdown ";
	sc += ", sum(s.normal_price * s.quantity) AS markdown ";
	sc += ", (SELECT DISTINCT sum(i.total) FROM sales ss WHERE ss.invoice_number = i.invoice_number AND refunded=1 ) AS total_return ";
	
	sc += " FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " WHERE 1=1 ";
//	sc += " AND i.paid = 1 ";
	sc += s_dateSql;
	if(m_branch != "")
	{
		if(TSIsDigit(m_branch))
		sc += " AND i.branch = "+ m_branch;
	}
//	sc += " AND i.paid = 1 ";
	sc += " GROUP BY i.invoice_number, i.total, i.paid, i.refunded, i.amount_paid ";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "summary");
		if(rows <= 0)
		{
//			Response.Write("<br><br><center><h3>ERROR, Order Not Found</h3>");
			return "No Sales";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "SQL Error";
	}

	int n = 0;
	int len = 0;
	string s = "";
	double dGross = 0;
	double dTMarkDown = 0;
	double dTReturn = 0;
	double dNetSales = 0;
	double dTGST = 0;
	double dNetNoGST = 0;
	double dTSales = 0;
	double dTAmountPaid = 0;
	string gst = GetSiteSettings("gst_rate_percent", "1.125");
if(gst != "")
	if(MyDoubleParse(gst) > 1.125)
		gst = (1 + (MyDoubleParse(gst) / 100)).ToString();
///DEBUG("gst =", gst);
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["summary"].Rows[i];
		string gross = dr["gross"].ToString();
		string markdown = dr["markdown"].ToString();
		
		string t_return = dr["total_return"].ToString();
		string t_sales = dr["total_sales"].ToString();
		string t_paid = dr["total_paid"].ToString();
		string inv = dr["invoice_number"].ToString();
//		string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 2).ToString("c");
		if(MyDoubleParse(markdown) == 0)
			markdown = t_sales;
		markdown = (MyDoubleParse(markdown) - MyDoubleParse(t_sales)).ToString();
		dGross += MyDoubleParse(gross);
		dTMarkDown += MyDoubleParse(markdown);
		dTReturn += MyDoubleParse(t_return);
		dTSales += MyDoubleParse(t_sales);
		dTAmountPaid += MyDoubleParse(t_paid);
	}
	
	dGross = dGross * MyDoubleParse(gst);
	dTMarkDown = dTMarkDown * MyDoubleParse(gst);
	dTReturn = dTReturn * MyDoubleParse(gst);
	dNetNoGST = dTSales / MyDoubleParse(gst);
//	dTSales = dTSales * MyDoubleParse(gst);
	s += "\r\n------------------------------------------\r\n";
	s += "TRADING TOTALS\r\n";
	s += "------------------------------------------\r\n\r\n";
//	s += "Gross Sales\t\t\t"+ dGross.ToString("c") +"\r\n";
	s += "Gross Sales\t\t\t "+ (dTSales).ToString("c") +"\r\n";
//	s += "Gross Sales\t\t\t"+ (dTSales + dTMarkDown).ToString("c") +"\r\n";
	s += "\t- Returns\t\t "+ dTReturn.ToString("c") +"\r\n";
//	s += "\t- Markdowns\t\t"+ dTMarkDown.ToString("c") +"\r\n";
	s += "\t\t\t\t----------\r\n\r\n";
	//dNetSales = dGross - dTReturn - dTMarkDown;
	dNetSales = dTSales;
//	dNetNoGST = dNetSales / MyDoubleParse(gst);
	dTGST = dNetSales - dNetNoGST;
	
//	s += "Total Merchandise Sales\t\t "+ dGross.ToString("c") +"\r\n";
//	s += "Net Total Sales\t\t\t "+ dGross.ToString("c") +"\r\n\r\n";
//	s += "\t\t\t\t----------\r\n\r\n";
	s += "Net Taxable Excluding GST\t"+ dNetNoGST.ToString("c") +"\r\n";
	s += "Net GST Collected\t\t "+ dTGST.ToString("c") +"\r\n";
	s += "\t\t\t\t==========\r\n\r\n";
//	s += "Trading Total\t\t\t"+ (dNetNoGST + dTGST).ToString("c") +"\r\n\r\n";
	s += "Trading Total\t\t\t "+ dNetSales.ToString("c") +"\r\n\r\n";
	s += "Total Paid\t\t\t "+ dTAmountPaid.ToString("c") +"\r\n\r\n";
	s += "Total UnPaid\t\t\t "+ (dNetSales-dTAmountPaid).ToString("c") +"\r\n\r\n";

	
	//***************** print media details *********************

	s += "------------------------------------------\r\n";
	s += "MEDIA TOTALS\r\n";
	s += "------------------------------------------\r\n";

	sc = " SET dateformat dmy SELECT sum(i.total) AS total, e.name, COUNT(i.invoice_number) AS total_invoice ";
	sc += " FROM invoice i INNER JOIN enum e ON e.id = i.payment_type AND e.class = 'payment_method' ";
	sc += s_dateSql;
	if(m_branch != "")
	{
		if(TSIsDigit(m_branch))
		sc += " AND i.branch = "+ m_branch;
	}
//	sc += " AND i.paid = 1 ";
	sc += "group by e.name  ";
	sc += " ORDER BY e.name ";

	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT sum(t.amount) AS total, e.name, COUNT(t.id) AS total_trans ";
	sc += " FROM trans t JOIN tran_detail d ON d.id = t.id ";
//	sc += " LEFT OUTER JOIN tran_invoice ti ON ti.tran_id = d.id and ti.tran_id = t.id "; //AND ti.purchase = 0 ";
	sc += " JOIN enum e ON e.id = d.payment_method AND e.class = 'payment_method' ";
	sc += " WHERE DATEDIFF(day, d.trans_date, GETDATE()) = " + sdate;
//	sc += " AND (ISNULL(ti.purchase, 0) = 0) ";
	if(m_branch != "")
	{
		if(TSIsDigit(m_branch))
		sc += " AND t.branch = "+ m_branch;
	}
	sc += " GROUP BY e.name ";
	sc += " ORDER BY e.name ";
//DEBUG("sc medai = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "details");
		if(rows <= 0)
		{
			s += "No Payment";
			return s;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return s + "Error Getting Payment";
	}
	double dTCash = 0;
	double dTotal = 0;
	double dNetTotal = 0;
	string last_type = "1";
	int nTotalTrans = 0;
//DEBUG("rows =", rows);
	for(int i=0; i<dst.Tables["details"].Rows.Count; ++i)
	{
		DataRow dr = dst.Tables["details"].Rows[i];
		string payment_type = dr["name"].ToString();
		int nTrans = MyIntParse(dr["total_trans"].ToString());
		//string payment_type_id = dr["type_id"].ToString();
		//string invoice = dr["invoice_number"].ToString();
		string total = dr["total"].ToString();
		
		dNetTotal += MyDoubleParse(total);
		payment_type = payment_type.ToUpper();
		if(payment_type == "CASH")
			payment_type += "(INC CASHOUT)";
		s += payment_type;
		if(payment_type.Length >= 16)
		s += "\t";
		if(payment_type.Length >=2 && payment_type.Length <9)
		s += "\t\t\t";
		if(payment_type.Length >=9 && payment_type.Length <16)
		s += "\t\t";
		s += nTrans +"\t ";
		s += MyDoubleParse(total).ToString("c") +"\r\n";
//s += payment_type +"\t\t\t"+ MyDoubleParse(total).ToString("c") +"\r\n";
		s += "\t\t\t\t----------\r\n";
		//s += invoice +"\t\t\t\t"+ MyDoubleParse(total).ToString("c") +"\r\n";
		nTotalTrans += nTrans;
	}

	
	s += "[/b]Media Total[/b]\t\t ";
	s += nTotalTrans.ToString();
	s += "\t"+ dNetTotal.ToString("c")+"";
	s += "\r\n\t\t\t\t==========\r\n";
//	s += "[/b]Invoice Total[/b]\t\t\t"+ total_invoice +"";
	s += "\r\n";
	//*****************end here*********************


//DEBUG("s =", s);
	return s;
}

bool doPrintDailySummary()
{
	string title = "Today\\'s";
	//if(Request.Form["sdate"] == "1")
	if(m_sdate == "1")
		title = "Yesterday\\'s";
	string sReceiptBody = doBuildDailySummary(); //get m_dInvoiceTotal first before record payment
//DEBUG("sReceiptBody =", sReceiptBody);
		//print receipt
/*	byte[] bf = {0x1b, 0x21, 0x20, 0x0};//new char[4];
	byte[] sf = {0x1b, 0x21, 0x02, 0x0};//new char[4];
//	byte[] cut = {0x1d, 0x56, 0x01, 0x00};//new char[4];
	byte[] cut = {0x1B, 0x6D, 0x01, 0x00};//new char[4]; //original cut code idp3210 model
	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
//	byte[] kick = {0x1B, 0x70, 0x30, 0x37, 0x79};//, 0x0a, 0x0};//new char[6]; //original kick drawer for idp3210 model
*/	
	//print receipt
	byte[] bf = {0x1b, 0x21, 0x20, 0x0};//new char[4];
	byte[] sf = {0x1b, 0x21, 0x02, 0x0};//new char[4];
	byte[] cut = {0x1d, 0x56, 0x01, 0x00};//new char[4];
	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
	
	ASCIIEncoding encoding = new ASCIIEncoding( );
    string bigfont = encoding.GetString(bf);	
    string smallfont = encoding.GetString(sf);
    string scut = encoding.GetString(cut);
    string kickout = encoding.GetString(kick);
    
	string header = "";
	header += "[b]" + title + "[/b]\r\n[b]Sales Summary[/b]\r\n";
	header += "Branch: "+ m_branchName +"\r\n";
//	header += DateTime.Now.ToString();
	string sbody = sReceiptBody;
	string sdate = "\r\n"+ DateTime.Now.ToString("dd/MM/yyyy");
	string stime = "  "+ DateTime.Now.ToString("HH:mm");

	string footer = "\r\n\t  *******[/b]End of Summary[/b]*******\r\n\r\n\r\n\r\n\r\n";
//	string sprint = "\r\ntesting me\r\n";
//	sprint += scut;
	string sprint = "\r\n" + header + sdate + stime + sbody + footer + scut;

//	if(Request.Form["pm"] == "cash")
		sprint += "\\r\\n\\r\\n" + kickout;
	sprint = sprint.Replace("\r\n", "\\r\\n");
	sprint = sprint.Replace("[/b]", smallfont);
	sprint = sprint.Replace("[b]", bigfont);
	sprint = sprint.Replace("[cut]", scut.ToString());
	sprint = sprint.Replace("[date]", sdate);
	sprint = sprint.Replace("[time]", stime);
///	sprint = sprint.Replace("[inv_num]", m_invoiceNumber);
//DEBUG("sprint = ", sprint);
	PrintAdminHeader();

	//AsPrint ActiveX Control
	Response.Write("\r\n<object classid=\"clsid:B816E029-CCCB-11D2-B6ED-444553540000\" ");
	Response.Write(" CODEBASE=\"..\\cs\\asprint.ocx\" ");
	Response.Write(" id=\"AsPrint1\">\r\n");
	Response.Write("<param name=\"_Version\" value=\"65536\">\r\n");
	Response.Write("<param name=\"_ExtentX\" value=\"2646\">\r\n");
	Response.Write("<param name=\"_ExtentY\" value=\"1323\">\r\n");
	Response.Write("<param name=\"_StockProps\" value=\"0\">\r\n");
	Response.Write("<param name=\"HideWinErrorMsg\" value=\"1\">\r\n");
	Response.Write("</object>\r\n");

/*	//	s += "	document.AsPrint1.Open('LPT1')\r\n";
	s += "	document.AsPrint1.Open('COM1')\r\n";
	s += "	document.AsPrint1.PrintString('" + sprint + "');\r\n";
	s += "	document.AsPrint1.Close();\r\n";

	s += "</script";
	s += ">";
*/
	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");
//DEBUG(" sdfeor =", m_sReceiptPort);
	string s = "";
	s = "\r\n<script language=javascript>\r\n";
	
	s += " var printer_port = '" + m_sReceiptPort + "';\r\n";
	s += @"
	var sport = '';
	fn = 'c:/qpos/p_port.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
	{
		tf = fso.OpenTextFile(fn, 1, false); 
		
		try
		{
			sport = tf.ReadAll();
		}
		catch(err)
		{
		}
		tf.Close(); 
	}
	if(sport != '')
		printer_port = sport;

	fn = 'c:/qpos/receipt.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
		fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -2);
//	window.alert(sport);
	";
//	s += "tf.Write('" + sprint_nokick + "'); \r\n";
	s += "tf.Close();\r\n";
//	s += "	document.AsPrint1.Open(document.f.printer_port.value);\r\n";
	s += "	document.AsPrint1.Open(printer_port);\r\n";
	
//	if(bkick)
//		s += "	document.AsPrint1.PrintString('" + kickout + "');\r\n";
//	else
		s += "	document.AsPrint1.PrintString('" + sprint + "');\r\n";
	s += "	document.AsPrint1.PrintString('" + scut + "');\r\n";
	s += "	document.AsPrint1.PrintString('" + kickout + "');\r\n";
	s += "	document.AsPrint1.Close();\r\n";
	s += "</script";
	s += ">";

	Response.Write(s);

//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpos.aspx?t=new\">");
	return true;
}
</script>

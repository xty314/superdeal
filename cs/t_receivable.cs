<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@ Import Namespace="ASPNet_Drawing" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>
<!-- #include file="page_index.cs" -->

<script runat=server>

string m_branchID = "1";
string m_click = "1";
string listed = "";
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_directory = "0";
string m_credit_terms = "0";
string m_path = "";
string m_sFileName = "total_receivable";
string m_export = "";
string m_period = "";
bool m_bPrint = false;

void Page_Load(Object Src, EventArgs E) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("accountant"))
		return;

		//geting server path name 
	string strPath = Server.MapPath("backup/");
	string lname = Session["name"].ToString();
	int bpos = lname.IndexOf(" ");
	if(bpos > 0)
		lname = lname.Substring(0, bpos);
	lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	m_path = strPath + lname;

	if(Request.QueryString["export"] != null)
		m_export = Request.QueryString["export"];

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

	if(Request.QueryString["dir"] != null && Request.QueryString["dir"] != "")
		m_directory = Request.QueryString["dir"];
	if(Request.QueryString["terms"] != null && Request.QueryString["terms"] != "")
		m_credit_terms = Request.QueryString["terms"];
	if(Request.QueryString["period"] != null && Request.QueryString["period"] != "" && Request.QueryString["period"] != "all")
		m_period = Request.QueryString["period"];
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
//DEBUG("m_branchid =", m_branchID);	
	if(!getTotalDue())
		return;
	if(Request.QueryString["export"] == "1")
	{		
		return;
	}
	if(Request.QueryString["print"] == "sum")
	{
		printSummary();
	}
	else
	{
		InitializeData();
		BindTotalDueGrid();
	}
	PrintAdminFooter();
}

void printSummary()
{
//	Response.Write("<hmtl><body onload=\"window.print(); return confirm('Close this windows!!');\">");
	Response.Write("<hmtl><body onload=\"window.print(); \">");
	
	Response.Write("<table width=99% align=center cellspacing=1 cellpadding=1 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	if(Request.QueryString["listed"] == "")
		listed = "ALL";
	else
		listed = Request.QueryString["listed"];
	Response.Write("<tr><th><h4>Receivable Summary for "+(listed).ToUpper()+" </h4></tr>");
	Response.Write("<tr><td>" + m_sCompanyTitle +"</td></tr>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<tr><td>Branch: ");
		PrintBranchNameOptions(m_branchID, "", true);
	}
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Requested By: "+ Session["name"].ToString() + "</td></tr>");
	Response.Write("<tr><td>Created Date: "+ DateTime.Now.ToString("dd/MMM/yyyy") + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<table width=99% align=center cellspacing=1 cellpadding=1 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr align=left bgcolor=#CDDDDF><th>CUSTID#</th>");
	//Response.Write("<th>COMPANY</th>");
	Response.Write("<th align=left>TRADING NAME</th>");
	Response.Write("<th align=left nowrap>PHONE#</th>");	
	Response.Write("<th align=left>CREDIT TERM</th>");
	Response.Write("<th align=left>SALES MANAGER</th>");	
	Response.Write("<th align=right>TOTAL DUE</th>");
	Response.Write("<th align=right>CURRENT</th>");
	Response.Write("<th align=right>7/14/30 Days</th>");
	Response.Write("<th align=right>14/30/60 Days</th>");
	Response.Write("<th align=right>30/60/90 Days+</th><th align=right>TOTAL CREDITS</th></tr>");

	int rows = dst.Tables["total_due"].Rows.Count;

	bool bAlter = true;
	DataRow dr ;
	double dSub_total_due = 0;
	double dSub_current_due = 0;
	double dSub_30days = 0;
	double dSub_60days = 0;
	double dSub_90days = 0;
	double dSub_total_credit = 0;
	Response.Write("<tr><td colspan=12><hr size=1 color=black</td></tr>");
	for(int m=0; m<rows; m++)
	{
		
		dr = dst.Tables["total_due"].Rows[m];
		string credit_term = dr["credit_term"].ToString();
		string company = dr["company"].ToString();
		if(company == "")
			company = dr["name"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string sales = dr["sales_manager"].ToString();
		string phone = dr["phone"].ToString();
		string card_id = dr["id"].ToString();
		string total_due = dr["totaldue"].ToString();
		string days30 = dr["Days30"].ToString();
		string days60 = dr["Days60"].ToString();
		string days90 = dr["Days90"].ToString();
		string current_due = dr["current_due"].ToString();
		string total_credit = dr["total_credit"].ToString();

		if(total_credit == null || total_credit == "")
			total_credit = "0";
		if(total_due == null || total_due == "")
			total_due = "0";
		if(days30 == null || days30 == "")
			days30 = "0";
		if(days60 == null || days60 == "")
			days60 = "0";
		if(days90 == null || days90 == "")
			days90 = "0";
		if(current_due == null || current_due == "")
			current_due = "0";
		
		Response.Write("<tr ");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");

		string stotal_due = "";
		string scurrent_due = "";
		string sdays30 = "";
		string sdays60 = "";
		string sdays90 = "";
		string stotal_credit = "";

		double dtotal_due = MyDoubleParse(total_due);
		double dcurrent_due = Math.Round(MyDoubleParse(current_due), 2);
		double ddays30 = MyDoubleParse(days30);
		double ddays60 = MyDoubleParse(days60);
		double ddays90 = MyDoubleParse(days90);
		double dtotal_credit = MyDoubleParse(total_credit);
		if(dtotal_due != 0)
			stotal_due = dtotal_due.ToString("c");
		if(dcurrent_due != 0)
			scurrent_due = dcurrent_due.ToString("c");
		if(ddays30 != 0)
			sdays30 = ddays30.ToString("c");
		if(ddays60 != 0)
			sdays60 = ddays60.ToString("c");
		if(ddays90 != 0)
			sdays90 = ddays90.ToString("c");
		if(dtotal_credit != 0)
			stotal_credit = dtotal_credit.ToString("c");

		bAlter = !bAlter;
		Response.Write(">");
		Response.Write("<td>" + card_id +"</td>"); 
		//Response.Write("<th align=left>" + company +"</th>");
		Response.Write("<td align=left>" + trading_name +"</th>");
		Response.Write("<td align=left>" + phone +"</th>");		
		Response.Write("<td align=left>" + credit_term +"</th>");		
		Response.Write("<td align=left>" + sales +"</th>");
		
		Response.Write("<td align=right>" + stotal_due +"</td>");
		Response.Write("<td align=right>" + scurrent_due +"</td>");
		Response.Write("<td align=right>" + sdays30 +"</td>");
		Response.Write("<td align=right>" + sdays60 +"</td>");
		Response.Write("<td align=right>" + sdays90 +"</td>");
		Response.Write("<td align=right>" + stotal_credit +"</td>");
		Response.Write("</tr>");
		dSub_total_due += double.Parse(total_due);
		dSub_current_due += double.Parse(current_due);
		dSub_30days += double.Parse(days30);
		dSub_60days += double.Parse(days60);
		dSub_90days += double.Parse(days90);
		dSub_total_credit += double.Parse(total_credit);
	}
	
	Response.Write("<tr><td colspan=12>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=12><hr size=1 color=black</td></tr>");
	Response.Write("<tr><th colspan=5 align=right>Sub Total:</th><th align=right> "+dSub_total_due.ToString("c")+"</th>");
	Response.Write("<th align=right> "+dSub_current_due.ToString("c")+"</th>");
	Response.Write("<th align=right> "+dSub_30days.ToString("c")+"</th>");
	Response.Write("<th align=right> "+dSub_60days.ToString("c")+"</th>");
	Response.Write("<th align=right> "+dSub_90days.ToString("c")+"</th>");
	Response.Write("<th align=right> "+dSub_total_credit.ToString("c")+"</th></tr>");
	
	Response.Write("<tr><th align=right colspan=5>Ageing Percent:</th><th align=left> &nbsp;</th>");
	Response.Write("<th align=right> "+((dSub_current_due / dSub_total_due)).ToString("p")+"</th>");
	Response.Write("<th align=right> "+((dSub_30days / dSub_total_due) ).ToString("p")+"</th>");
	Response.Write("<th align=right> "+((dSub_60days / dSub_total_due) ).ToString("p")+"</th>");
	Response.Write("<th align=right> "+((dSub_90days / dSub_total_due) ).ToString("p")+"</th>");
	Response.Write("<th align=right> "+((dSub_total_credit / dSub_total_due) ).ToString("p")+"</th></tr>");
	

	Response.Write("<tr><td colspan=10>&nbsp;</td>");
	Response.Write("</tr>");
	Response.Write("</body></html>");
	Response.Write("</table>");
}

void BindTotalDueGrid()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["total_due"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 40;
	string sorted = "";
	if(Request.QueryString["sorted"] != null && Request.QueryString["sorted"] != "")
		sorted = Request.QueryString["sorted"].ToString();
	if(Request.QueryString["listed"] != "" && Request.QueryString["listed"] != null)
		listed = Request.QueryString["listed"].ToString();

	m_cPI.URI ="?listed="+ listed +"&sorted="+ sorted +"";
	m_cPI.URI += "&dir="+ m_directory +"&terms="+ m_credit_terms +"&period="+ m_period;
		int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<form name=f method=post>");
//	Response.Write("<br>");
	string search = "";
	if(Request.Form["search"] != "" && Request.Form["search"] != null)
		search = Request.Form["search"].ToString();
	string URI = "t_receivable.aspx?p="+ m_cPI.CurrentPage +"";
	if(m_branchID != "")
		URI += "&branch="+ m_branchID;
//	URI += "&dir="+ m_directory +"&terms="+ m_credit_terms;
	URI += "&sorted=";
	string sURI = URI;
//	sURI = sURI.Replace("&dir=", "&nd=");
//	sURI = sURI.Replace("&terms=", "&nt=");
	Response.Write("<table width=99% align=center cellspacing=0 cellpadding=2 border=0 bordercolor=white bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	if(!g_bPDA)
	{
		Response.Write("<tr><td><b>Receivable Summary</b> &nbsp; </td></tr>");
		Response.Write("<tr><td>");
		if(Session["branch_support"] != null)
		{
			//URI += "&branch=";
			Response.Write("<font size=2>Branch : </font>");
			PrintBranchNameOptions(m_branchID, "t_receivable.aspx?p="+ m_cPI.CurrentPage +"&branch=", true);
		}
		Response.Write("</tr>");
		Response.Write("<tr><th align=left>" + m_sCompanyTitle +"</th></tr>");
		Response.Write("<tr><td>Requested By: "+ Session["name"].ToString() + "</td></tr>");
		Response.Write("<tr><td>Created Date: "+ DateTime.Now.ToString("dd/MMM/yyyy") + "</td></tr>");
		Response.Write("</table>");
		Response.Write("<table width=99% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=12><input type=text name=search value='"+ search +"'><input type=submit value='Search Customer' "+Session["button_style"] +">");	
		Response.Write(" Type:<select name=listed onchange=\"window.location=('" + URI + sorted +"&click="+ m_click +"&period="+ m_period+"&terms="+ m_credit_terms+"&dir="+ Request.QueryString["dir"] +"&listed='+this.options[this.selectedIndex].value)\">");
		Response.Write("<option value=all >All</option>");
		Response.Write("<option value='dealer'");
		if(Request.QueryString["listed"] == "dealer")
			Response.Write(" selected ");
		Response.Write(">Dealer</option>");
		Response.Write("<option value='customer'");
		if(Request.QueryString["listed"] == "customer")
			Response.Write(" selected ");
		Response.Write(">Customer</option>");	
		Response.Write("</select>");

		Response.Write("Directory/Group:");
		Response.Write("<select name=directory onchange=\"window.location=('" + URI + sorted +"&click="+ m_click +"&period="+ m_period+"&terms="+ m_credit_terms+"&listed="+ Request.QueryString["listed"] +"&dir='+this.options[this.selectedIndex].value)\">");
		Response.Write("<option value=0>All</option>");
		Response.Write(GetEnumOptions("card_dir", m_directory));
		Response.Write("</select>");
		Response.Write("Credit Terms:");
		Response.Write("<select name=terms onchange=\"window.location=('" + URI + sorted +"&click="+ m_click +"&period="+ m_period+"&listed="+ Request.QueryString["listed"] +"&dir="+ Request.QueryString["dir"] +"&terms='+this.options[this.selectedIndex].value)\">");
		Response.Write("<option value=0>All</option>");
		Response.Write(GetEnumOptions("credit_terms", m_credit_terms, false, true,"", false,"0"));
		Response.Write("</select>");

		Response.Write("Period:<select name='pickPeriod'  onchange=\"window.location=('" + URI + sorted +"&click="+ m_click +"&listed="+ Request.QueryString["listed"] +"&dir="+ Request.QueryString["dir"] +"&terms="+ Request.QueryString["terms"] +"&period='+this.options[this.selectedIndex].value)\"><option value=all>All</option>");
		Response.Write("<option value=0 ");
		if(m_period == "0")
			Response.Write(" selected ");
		Response.Write(">Current</option>");
		Response.Write("<option value=1 ");
		if(m_period == "1")
			Response.Write(" selected ");
		Response.Write(">Last Month</option>");
		Response.Write("<option value=2 ");
		if(m_period == "2")
			Response.Write(" selected ");
		Response.Write(">2 Months Ago</option>");
		Response.Write("<option value=3 ");
		if(m_period == "3")
			Response.Write(" selected ");
		Response.Write(">Over 3 Months</option>");
		Response.Write("</select>");

		Response.Write("</td>");

		Response.Write("<td colspan=5 align=right>");
		Response.Write("<input type=button value='Export Report' "+Session["button_style"] +" ");
		Response.Write(" onclick=\"javascript:summary_window=window.open('t_receivable.aspx?export=1&period="+ m_period+"&listed="+Request.QueryString["listed"]+"&sorted=" + Request.QueryString["sorted"] + "&branch="+ m_branchID +"&dir="+ m_directory+"&terms="+ m_credit_terms+"');\" '', ''>");
		Response.Write("<input type=button value='Print Summary' "+Session["button_style"] +" ");
		Response.Write(" onclick=\"javascript:summary_window=window.open('t_receivable.aspx?print=sum&period="+ m_period+"&listed="+Request.QueryString["listed"]+"&sorted=" + Request.QueryString["sorted"] + "&branch="+ m_branchID +"&dir="+ m_directory+"&terms="+ m_credit_terms+"');\" '', ''></td>");
		Response.Write("</tr>");
	}
	else
	{
		Response.Write("<tr><td colspan=3><b>Receivable Summary : " + DateTime.Now.ToString("dd/MMM/yyyy") + "</b></td></tr>");
	}
	
	if(!g_bPDA)
	{
		Response.Write("<tr bgcolor=#CDDDDF><th align=left><a title='click to sort by card id' href='"+URI+"i.card_id&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory+"&terms="+ m_credit_terms+"' class=o>CID#</a></th>");
		Response.Write("<th align=left><a title='click to sort by trading name' href='"+URI+"c.company&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>TRADING NAME</a></th>");
		Response.Write("<th align=left><a title='click to sort by phone' href='"+URI+"c.phone&click="+m_click+"&period="+ m_period+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>PHONE</a></th>");
		Response.Write("<th><a title='click to sort by credit term' href='"+URI+"credit_term&click="+m_click+"&period="+ m_period+"&listed="+listed+"' class=o>CREDIT TERM</a></th>");
		Response.Write("<th align=left><a title='click to sort by sales manager' href='"+URI+"c.sales&click="+m_click+"&period="+ m_period+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>SALES MANAGER</a></th>");
		Response.Write("<th colspan=2><a title='click to sort by total due' href='"+URI+"totaldue&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>TOTAL DUE</a></th>");
		Response.Write("<th colspan=2><a title='click to sort by current due' href='"+URI+"current_due&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>CURRENT</a></th>");
		Response.Write("<th colspan=2><a title='click to sort by 30 days' href='"+URI+"Days30&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>7/14/30 Days</a></th>");
		Response.Write("<th colspan=2><a title='click to sort by 60 days' href='"+URI+"Days60&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>14/30/60 Days</a></th>");
		Response.Write("<th colspan=2><a title='click to sort by 90 days' href='"+URI+"Days90&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>30/60/90 Days+</a></th>");
		Response.Write("<th ><a title='click to sort by total credit' href='"+URI+"total_credit&period="+ m_period+"&click="+m_click+"&listed="+listed+"&dir="+ m_directory +"&terms="+ m_credit_terms+"' class=o>TOTAL CREDIT</a></th>");
	}
	else
	{
		Response.Write("<tr bgcolor=#CDDDDF><th align=left>CID#</th>");
		Response.Write("<th align=left>NAME</th>");
		Response.Write("<th colspan=2>DUE</th>");
	}
	Response.Write("</tr>");
	
	bool bAlter = true;
	DataRow dr ;
	double dSub_total_due = 0;
	double dSub_current_due = 0;
	double dSub_30days = 0;
	double dSub_60days = 0;
	double dSub_90days = 0;
	double dSub_total_credit = 0;

	double d_total_due = 0;
	double d_current_due = 0;
	double d_30days = 0;
	double d_60days = 0;
	double d_90days = 0;
	double d_total_credit = 0;

	for(int j=0; j<rows; j++)
	{
		dr = dst.Tables["total_due"].Rows[j];
		dSub_total_due += MyDoubleParse(dr["totaldue"].ToString());
		dSub_30days += MyDoubleParse(dr["Days30"].ToString());
		dSub_60days += MyDoubleParse(dr["Days60"].ToString());
		dSub_90days += MyDoubleParse(dr["Days90"].ToString());
		dSub_current_due += MyDoubleParse(dr["current_due"].ToString());
		dSub_total_credit += MyDoubleParse(dr["total_credit"].ToString());
	}
	dSub_current_due -= dSub_total_credit; //DW
	dSub_total_due -= dSub_total_credit; //DW
	
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["total_due"].Rows[i];
		string company = dr["company"].ToString();
		if(company == "")
			company = dr["name"].ToString();
		string credit_term = dr["credit_term"].ToString();
		string card_id = dr["id"].ToString();
		string total_due = dr["totaldue"].ToString();
		string days30 = dr["Days30"].ToString();
		string days60 = dr["Days60"].ToString();
		string days90 = dr["Days90"].ToString();
		string current_due = dr["current_due"].ToString();
		string total_credit = dr["total_credit"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string phone = dr["phone"].ToString();
		string sales = dr["sales_manager"].ToString();
		if(total_credit == null || total_credit == "")
			total_credit = "0";
		if(total_due == null || total_due == "")
			total_due = "0";
		if(days30 == null || days30 == "")
			days30 = "0";
		if(days60 == null || days60 == "")
			days60 = "0";
		if(days90 == null || days90 == "")
			days90 = "0";
		if(current_due == null || current_due == "")
			current_due = "0";
		
		string stotal_due = "";
		string scurrent_due = "";
		string sdays30 = "";
		string sdays60 = "";
		string sdays90 = "";
		string stotal_credit = "";

		double dtotal_due = MyDoubleParse(total_due);
		double dcurrent_due = Math.Round(MyDoubleParse(current_due), 2);
		double ddays30 = MyDoubleParse(days30);
		double ddays60 = MyDoubleParse(days60);
		double ddays90 = MyDoubleParse(days90);
		double dtotal_credit = MyDoubleParse(total_credit);
		dtotal_due -= dtotal_credit; //24.08.05 DW 
		dcurrent_due -= dtotal_credit; //24.08.05 DW 
		if(dtotal_due != 0)
			stotal_due = dtotal_due.ToString("c");
		if(dcurrent_due != 0)
			scurrent_due = dcurrent_due.ToString("c");
		if(ddays30 != 0)
			sdays30 = ddays30.ToString("c");
		if(ddays60 != 0)
			sdays60 = ddays60.ToString("c");
		if(ddays90 != 0)
			sdays90 = ddays90.ToString("c");
		if(dtotal_credit != 0)
			stotal_credit = dtotal_credit.ToString("c");

		Response.Write("<tr ");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		
		bAlter = !bAlter;
		string statement = "";
		if(dtotal_due > 0)
			statement = "statement.aspx?ci="+card_id+"&t=vd&p=4";
		if(dcurrent_due > 0)
			statement = "statement.aspx?ci="+card_id+"&t=vd&p=0";
		if(ddays30 > 0)
			statement = "statement.aspx?ci="+card_id+"&t=vd&p=1";
		if(ddays60 > 0)
			statement = "statement.aspx?ci="+card_id+"&t=vd&p=2";
		if(ddays90 > 0)
			statement = "statement.aspx?ci="+card_id+"&t=vd&p=3";

		Response.Write(">");
		Response.Write("<td><a title='click to edit card list' href='ecard.aspx?id="+card_id+"' target=blank class=o>" + card_id +"</a></td>"); 
		Response.Write("<td>");
		if(!g_bPDA)
		{
			Response.Write(" <input type=button title='view customer details' onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + card_id + "','',' width=350,height=350');\" style='font-size: 7pt' value='who?' " + Session["button_style"] + ">&nbsp;");
		}
		Response.Write(trading_name);		
		if(!g_bPDA)
		{
			Response.Write("<td>" + phone + "</td>");
			Response.Write("</td><td>" + credit_term + "</td>");
			Response.Write("<td>" + sales + "</td>");
		}
		Response.Write("<td align=right >" + stotal_due +"</td>");
		if(!g_bPDA)
		{
			if(double.Parse(total_due) > 0)
			{
				Response.Write("<td ><a title='print total due statement' href='"+statement+"' class=o target=blank>S</a>");
				Response.Write("&nbsp;<a title='Email to Customer' href='broadmail.aspx?ci="+card_id+"&t=m' class=o target=blank>E</a></td>");
			}
			else
				Response.Write("<td>&nbsp;</td>");
			Response.Write("<td align=right >" + scurrent_due +"</td>");
			if(double.Parse(current_due) > 0)
				Response.Write("<td><a title='print current due statement' href='"+statement+"' class=o target=blank>S</a>");
			else
				Response.Write("<td>&nbsp;</td>");
			Response.Write("<td align=right >" + sdays30 +"</td>");
			if(double.Parse(days30) > 0)
				Response.Write("<td><a title='print 30 days statement' href='"+statement+"' class=o target=blank>S</a>");
			else
				Response.Write("<td>&nbsp;</td>");
			Response.Write("<td align=right >" + sdays60 +"</td>");
			if(double.Parse(days60) > 0)
				Response.Write("<td><a title='print 60 days statement' href='"+statement+"' class=o target=blank>S</a>");
			else
				Response.Write("<td>&nbsp;</td>");
			Response.Write("<td align=right >" + sdays90 +"</td>");
			if(double.Parse(days90) > 0)
				Response.Write("<td><a title='print 90 days statement' href='"+statement+"' class=o target=blank>S</a>");
			else
				Response.Write("<td>&nbsp;</td>");

			if(dtotal_credit > 0 && dtotal_due > 0)
				Response.Write("<td align=right ><font color=Green><a title='click to apply credit' href='custpay.aspx?id="+card_id+"' class=o target=blank>"); // + double.Parse(total_credit).ToString("c") +"</font></td>");
			else
				Response.Write("<td align=right >");
			Response.Write(""+ stotal_credit +"</a></td>");
		}
		Response.Write("</tr>");

		d_total_due += dtotal_due;
		d_current_due += dcurrent_due;
		d_30days += ddays30;
		d_60days += ddays60;
		d_90days += ddays90;
		d_total_credit += dtotal_credit;
	
	}

	if(g_bPDA)
	{
		Response.Write("<tr><th colspan=2 align=right> TOTAL:</th><th align=right> "+d_total_due.ToString("c")+"</th>");
		Response.Write("</tr>");
		Response.Write("</table></form>");
		return;
	}
		
	Response.Write("<tr><th colspan=5 align=right> TOTAL:</th><th align=right> "+d_total_due.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+(d_current_due).ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+d_30days.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+d_60days.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+d_90days.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+d_total_credit.ToString("c")+"</th></tr>");

	//Response.Write("<tr><td colspan=14><hr size=1 color=#eeeeee></td></tr>");
	Response.Write("<tr bgcolor=#EEEEE><th colspan=5 align=right>GRAND TOTAL:</th><th align=right> "+dSub_total_due.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+dSub_current_due.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+dSub_30days.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+dSub_60days.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+dSub_90days.ToString("c")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+dSub_total_credit.ToString("c")+"</th></tr>");

	Response.Write("<tr bgcolor=#EEEEE><th colspan=5 align=right>AGEING (%):</th><th align=left> &nbsp;</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+((dSub_current_due / dSub_total_due) ).ToString("p")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+((dSub_30days / dSub_total_due) ).ToString("p")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+((dSub_60days / dSub_total_due) ).ToString("p")+"</th>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+((dSub_90days / dSub_total_due) ).ToString("p")+"</th>");
//	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > "+((dSub_total_credit / dSub_total_due) ).ToString("p")+"</th></tr>");
	Response.Write("<td>&nbsp;</td><th colspan=1 align=right > &nbsp; </th></tr>");

	Response.Write("<tr><td colspan=12>");
	Response.Write(sPageIndex);
	Response.Write("</td>");
	
	Response.Write("<td colspan=5 align=right>");
	Response.Write("<input type=button value='Export Report' "+Session["button_style"] +" ");
	Response.Write(" onclick=\"javascript:summary_window=window.open('t_receivable.aspx?export=1&listed="+Request.QueryString["listed"]+"&sorted=" + Request.QueryString["sorted"] + "&branch="+ m_branchID +"&dir="+ m_directory+"&terms="+ m_credit_terms+"');\" '', ''>");
	Response.Write("<input type=button value='Print Summary' "+Session["button_style"] +" ");
	Response.Write(" onclick=\"javascript:summary_window=window.open('t_receivable.aspx?print=sum&listed="+Request.QueryString["listed"]+"&branch="+ m_branchID +"&dir="+ m_directory+"&terms="+ m_credit_terms+"');\" '', ''></td>");
	Response.Write("</tr>");
	Response.Write("</form>");
	Response.Write("</table>");
}

bool getTotalDue()
{

	string sc = "SET DATEFORMAT dmy SELECT c.id, i.card_id, c.trading_name, c.phone, c.sales, c.name, c.company, ";
	sc += " (SELECT cc.name FROM card cc WHERE cc.id = c.sales) AS sales_manager ";
	sc += " , (SELECT name FROM enum WHERE class='credit_terms' AND id = (SELECT credit_term FROM card WHERE id = i.card_id OR id = t.card_id) ) AS credit_term, ";
	sc += " (SELECT  SUM(amount-amount_applied) AS totalcredit FROM credit ";
//    sc += " WHERE card_id = i.card_id) AS total_credit, ";
	sc += " WHERE card_id = t.card_id) AS total_credit, ";
	sc += "  (SELECT ISNULL((SUM(total-amount_paid)),0) AS  totalall ";
	sc += " FROM invoice  ";
	sc += "  WHERE paid = 0 AND card_id = i.card_id ";
	if(m_period == "0")
		sc += " AND DATEDIFF(month, commit_date, getdate()) = 0 ";
	else if(m_period == "1")
		sc += " AND DATEDIFF(month, commit_date, getdate()) = 1 ";
	else if(m_period == "2")
		sc += " AND DATEDIFF(month, commit_date, getdate()) = 2 ";
	else if(m_period == "3")
		sc += " AND DATEDIFF(month, commit_date, getdate()) >= 3 ";
	

	sc += " ) AS 'totaldue', ";

/*	sc += "  (SELECT ISNULL(SUM(total-amount_paid),0) AS totalcur FROM invoice ";
	sc += "  WHERE card_id = i.card_id AND datediff(month, commit_date, getdate()) = 0) AS 'current_due', ";
	sc += "  (SELECT ISNULL(SUM(total-amount_paid),0) AS total30 FROM invoice ";
	sc += "  WHERE card_id = i.card_id AND datediff(month, commit_date, getdate()) = 1) AS 'Days30', ";
	sc += "  (SELECT ISNULL(SUM(total-amount_paid),0) AS total60 FROM invoice ";
	sc += "  WHERE card_id = i.card_id AND datediff(month, commit_date, getdate()) = 2) AS 'Days60', ";
	sc += "  (SELECT ISNULL(SUM(total-amount_paid),0) AS total90 FROM invoice ";
	sc += "  WHERE card_id = i.card_id AND datediff(month, commit_date, getdate()) >= 3) AS 'Days90' ";
*/	
if(m_period == "0" || m_period == "")
{		
	sc += @"	'current_due' = 
CASE c.credit_term 
WHEN '4' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id and DATEDIFF(day, commit_date, GETDATE()) >=0 AND DATEDIFF(day, commit_date, GETDATE()) < 7) 
WHEN '5' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(day, commit_date, GETDATE()) >=0 AND DATEDIFF(day, commit_date, GETDATE()) < 14) 
WHEN '6' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(month, commit_date, getdate()) = 0) 
ELSE (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND datediff(month, commit_date, getdate()) = 0)  
END
";
}
else
{
	sc += " 'current_due' = 0 ";
}
  
if(m_period == "1" || m_period == "")
{
sc += @" ,'Days30' = 
CASE c.credit_term 
WHEN '4' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(day, commit_date, GETDATE()) >= 7 AND DATEDIFF(day, commit_date, GETDATE()) < 15) 
WHEN '5' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND (DATEDIFF(day, commit_date, GETDATE()) >= 14 AND DATEDIFF(day, commit_date, GETDATE()) < 29)) 
WHEN '6' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(month, commit_date, GETDATE()) = 1)  
ELSE (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(month, commit_date, GETDATE()) = 1)  
END ";
}
else
	sc += " , 'Days30' = 0 ";

if(m_period == "2" || m_period == "")
{
sc += @" ,'Days60' = 
CASE c.credit_term 
WHEN '4' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(day, commit_date, GETDATE()) >= 15 AND DATEDIFF(day, commit_date, GETDATE()) < 22) 
WHEN '5' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND (DATEDIFF(day, commit_date, GETDATE()) >= 29 AND DATEDIFF(day, commit_date, GETDATE()) < 59))
WHEN '6' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(month, commit_date, getdate()) = 2) 
ELSE (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND datediff(month, commit_date, getdate()) = 2)
END ";
}
else
	sc += " , 0 AS 'Days60'";

if(m_period == "3" || m_period == "")
{
sc += @" , 'Days90' = 
CASE c.credit_term 
WHEN '4' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND card_id = i.card_id AND DATEDIFF(day, commit_date, GETDATE()) >= 22) 
WHEN '5' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(day, commit_date, GETDATE()) >= 59) 
WHEN '6' THEN (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND DATEDIFF(month, commit_date, getdate()) >= 3)  
ELSE (SELECT sum(total- amount_paid) from invoice where paid = 0 AND  card_id = i.card_id AND datediff(month, commit_date, getdate()) >= 3) 	
END
";
}
else
{
	sc += " , 0 AS 'Days90' ";
}

	sc += " FROM invoice i INNER JOIN card c ON c.id = i.card_id ";
	//sc += " FROM card c LEFT OUTER JOIN invoice i ON c.id = i.card_id ";
	sc += " LEFT OUTER JOIN credit t ON t.card_id = c.id ";
//	sc += " WHERE (i.card_id IS NOT NULL) ";
//	sc += " AND (total IS NOT NULL) ";
	sc += " WHERE 1=1 ";
	if(Request.Form["search"] != "" && Request.Form["search"] != null)
	{
		if(TSIsDigit(Request.Form["search"]))
			sc += " AND c.id = " + Request.Form["search"].ToString() + " ";
		//	sc += " AND i.card_id = " + Request.Form["search"].ToString() + " ";
		else
		{
			sc += " AND (Lower(c.name) LIKE '%"+ (EncodeQuote(Request.Form["search"].ToString())).ToLower() + "%' ";
			sc += " OR Lower(c.company) LIKE '%"+ (EncodeQuote(Request.Form["search"].ToString())).ToLower() + "%'";
			sc += " OR LOWER(c.trading_name) LIKE '%"+ (EncodeQuote(Request.Form["search"].ToString())).ToLower() + "%') ";
		}
	}
	if(Request.QueryString["listed"] != "" && Request.QueryString["listed"] != null)
	{
		if(Request.QueryString["listed"] == "customer")
			sc += " AND c.type = 1 ";
		else if(Request.QueryString["listed"]== "dealer")
			sc += " AND c.type = 2 ";
		else
			sc += "";
	}	
	if(m_directory != "0")
		sc += " AND c.directory = "+ m_directory;
	if(m_credit_terms != "0")
		sc += " AND c.credit_term = "+ m_credit_terms;
	if(Session["branch_support"] != null && m_branchID != "0")
		sc += " AND i.branch = "+ m_branchID;
	
	if(m_period == "0")
		sc += " AND  paid= 0 AND DATEDIFF(month, i.commit_date, getdate()) = 0 ";
	else if(m_period == "1")
		sc += " AND paid= 0 AND  DATEDIFF(month, i.commit_date, getdate()) = 1 ";
	else if(m_period == "2")
		sc += " AND  paid= 0 AND DATEDIFF(month, i.commit_date, getdate()) = 2 ";
	else if(m_period == "3")
		sc += " AND paid= 0 AND DATEDIFF(month, i.commit_date, getdate()) >= 3 ";

	sc += " GROUP BY t.card_id, c.id, i.card_id, c.name, c.company, c.trading_name,  c.credit_term, c.phone, c.sales ";
//	sc += " HAVING (SUM(i.total - i.amount_paid) > 0) ";
	sc += " HAVING (SUM(i.total - i.amount_paid) > 0 OR SUM(i.total - i.amount_paid) < 0) OR (SUM(t.amount - t.amount_applied) > 0) ";
	if(!g_bPDA)
	{
		if(Request.QueryString["sorted"] != null && Request.QueryString["sorted"] != "")
		{	
			if(Request.QueryString["click"] == "1")
			{
				sc += "ORDER BY "+Request.QueryString["sorted"].ToString()+ " ASC ";
				m_click = "0";
			}
			else
			{
				sc += "ORDER BY "+Request.QueryString["sorted"].ToString()+ " DESC ";
				m_click = "1";
			}
		}
		else
			sc += " ORDER BY totaldue, total_credit DESC ";
	}
	else
	{
		sc += " ORDER BY totaldue DESC ";
	}
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "total_due");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(Request.QueryString["export"] != null && Request.QueryString["export"] != "" && Request.QueryString["export"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		bool bRet = true;
		bRet = EmptyBackupFolder();
		bRet = WriteCSVFile(dst);
	
		if(bRet)
		{
			Response.Write("<br><h4>Zipping data files, please wait...");
			Response.Flush();
			bRet = ZipDir(m_path, "report_data_" + DateTime.Now.ToString("dd_MM_yy_HH_mm") + ".zip");
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
			Response.Write("<center><br><h2>Exporting data, please wait a second...</h2></center>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL="+ Request.ServerVariables["URL"]+ "?export=done\">");
			//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
			return true;
		}
		return true;
	
	}
	return true;

}

bool EmptyBackupFolder()
{
	if(Directory.Exists(m_path))
		Directory.Delete(m_path, true);

	Directory.CreateDirectory(m_path);
	return true;
}
bool WriteCSVFile(DataSet ds)
{
	Response.Write("Getting data from <b> Invoice </b> table ...");
	Response.Flush();
	
	StringBuilder sb = new StringBuilder();

	int i = 0;

	//write column names
	DataColumnCollection dc = ds.Tables["total_due"].Columns;
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

	for(i=0; i<ds.Tables["total_due"].Rows.Count; i++)
	{
		dr = ds.Tables["total_due"].Rows[i];
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

	string strPath = m_path + "\\"+ m_sFileName +".csv";

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
		//	s = new ZipOutputStream(File.Create(m_path + "\\" + zipFileName.Replace(".zip", "") + "_" + files.ToString() + ".zip"));
			s = new ZipOutputStream(File.Create(zipFileName.Replace(".zip", "") + "_" + files.ToString() + ".zip"));
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


</script>

<asp:Label id=LFooter runat=server/>

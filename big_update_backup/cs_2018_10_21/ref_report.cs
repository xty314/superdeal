
<!-- #include file="page_index.cs" -->
<script runat=server>

string m_type = "0";
string m_tableTitle = "Profit & Loss";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sales_person = "";
string m_sales_id = "";

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = 0;

bool m_bShowPic = true;
bool m_bPickTime = false;

string m_search = "";
string m_command = "";

string m_payee_name = "";

StringBuilder m_sb = new StringBuilder();  //xml data for 3d chart

string[] m_EachMonth = new string[13];

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] sct = new string[16];
int cts = 0;
int m_ct = 1;
bool m_bCompair = false;

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

	int i = 0;
	sct[i++] = "Bar Graph 2D";
	sct[i++] = "Bar Graph 3D"; 
	sct[i++] = "Blocks Chart 2D"; 
	sct[i++] = "Blocks Chart 3D"; 
	sct[i++] = "Pie Chart 2D"; 
	sct[i++] = "Pie Chart 3D"; 
	sct[i++] = "Stacked Bar 2D"; 
	sct[i++] = "Stacked Bar 3D"; 
	sct[i++] = "Line Graph 2D"; 
	sct[i++] = "Line Area Graph"; 
	sct[i++] = "Point Chart"; 
	sct[i++] = "Spine Graph 2D"; 
	sct[i++] = "Spine Area Graph"; 
	cts = i;

	if(Request.Form["cmd"] != null)
		m_command = Request.Form["cmd"];

	if(Request.Form["search"] != null && Request.Form["search"] !="" || m_command == "SEARCH PAYEE" 
		|| Request.QueryString["cid"] != null && Request.QueryString["cid"] != "" )
	{
			PrintAdminHeader();
		PrintAdminMenu();
		m_search = Request.Form["search"];
		if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "" && Request.QueryString["cid"] != "all")
			m_search = Request.QueryString["cid"];
		GetInvoiceRefundCusotmer();
		PrintAdminFooter();
		return;
	}

	if(Session["payee_name"] != null && Session["payee_name"] != "")
		m_payee_name = Session["payee_name"].ToString();

	if(m_command == "View Report")
	{
		PrintAdminHeader();
		PrintAdminMenu();

		DoQueryRefund();
		
		PrintAdminFooter();
		return;
	}

	m_type = Request.QueryString["t"];
	if(Request.Form["type"] != null)
		m_type = Request.Form["type"];
	
	if(Request.QueryString["type"] != "" && Request.QueryString["type"] != null)
	{
	
		m_type = Request.QueryString["type"];
		if(Request.QueryString["pr"] != null)
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());

	}
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["type"] == null || Request.QueryString["type"] == "")
		{
			PrintMainPage();
			return;
		}

//		if(Request.QueryString["np"] != null && Request.QueryString["np"] != "")
//			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
		m_type = Request.QueryString["type"];
//		m_code = Request.QueryString["code"];
	}

//	if(Request.Form["period"] != null)
//		m_nPeriod = MyIntParse(Request.Form["period"]);

	if(Request.Form["type"] != null)
		m_type = Request.Form["type"];

	if(Request.Form["Datepicker1_day"] != null)
	{
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
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
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	default:
		break;
	}

/*	PrintAdminHeader();
	PrintAdminMenu();

	DoQueryRefund();
	
	PrintAdminFooter();
	*/
}


void PrintMainPage()
{

	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"]+"' method=post>");
	Response.Write("<br><center><h3>Select Report</h3>");
	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>SELECT PAYEE</b></td></tr>");
	Response.Write("<tr>");

	Response.Write("<td colspan=2><b>SEARCH PAYEE: </b>&nbsp; ");
	if(m_payee_name != "")
		Response.Write("<select name=slt_card onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?cid=all'); \"><option value="+ Session["payee_id"].ToString() +">"+ m_payee_name +"</select> ");
	else
	{
		Response.Write("<input type=text ");
		Response.Write(" name=search value='"+ m_payee_name +"' onclick=\"document.f.search.value='';\">");
	}
	Response.Write(" <input type=submit name=cmd value='SEARCH PAYEE' "+ Session["button_style"] +"></td></tr>");
	Response.Write("<script");
		Response.Write(">\r\ndocument.f.search.select();\r\n</script");
		Response.Write(">\r\n");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>SELECT DATE RANGE</b></td></tr>");

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
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>Select : </b> From Date ");
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
		Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}


bool GetInvoiceRefundCusotmer()
{
	string sc = " SELECT c.name, c.company, c.email, c.phone, c.fax, c.id ";
	sc += " FROM card c "; //JOIN acc_refund ar ON ar.card_id = c.id  ";
	sc += " WHERE 1 = 1 ";
	if(m_search != "")
	{
		sc += " AND ";
		if(TSIsDigit(m_search))
			sc += " c.id = "+ m_search +" ";
		else
		{
			m_search = EncodeQuote(m_search);
			m_search = "%"+ m_search +"%";
			sc += " c.name LIKE '"+ m_search +"' OR c.company LIKE '"+ m_search +"' ";
			sc += " OR c.phone LIKE '"+ m_search +"' OR c.email LIKE '"+ m_search +"' ";
		}
	}
	sc += " GROUP BY c.name, c.company, c.email, c.phone, c.fax, c.id ";

//DEBUG("sc = ",sc);

	int rows =0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "refund_cust");
		if(rows == 1)
		{
			Session["payee_name"] = ds.Tables["refund_cust"].Rows[0]["name"].ToString();
			if(Session["payee_name"] == null || Session["payee_name"] == "")
				Session["payee_name"] = ds.Tables["refund_cust"].Rows[0]["company"].ToString();
			Session["payee_id"] = ds.Tables["refund_cust"].Rows[0]["id"].ToString();
			
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"');</script");
			Response.Write(">");
			return false;
		}
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG("rows =", rows);	
	if(rows <= 0)
 {
		Response.Write("<script language=javascript>window.alert('Currently No Transaction Found');window.history.go(-1);</script");
		Response.Write(">");
		return false;
	}
	double dTotalRefund = 0;

	if(rows > 0)
	{
		//paging class
		PageIndex m_cPI = new PageIndex(); //page index class
		if(Request.QueryString["p"] != null)
			m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
		if(Request.QueryString["spb"] != null)
			m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
			m_cPI.TotalRows = rows;
		m_cPI.PageSize = 25;
		m_cPI.URI = "?";
		
		string uri = Request.ServerVariables["URL"] +"?";
		if(m_cPI.CurrentPage.ToString() != "" && m_cPI.CurrentPage.ToString() != null)
			uri += "p="+ m_cPI.CurrentPage.ToString() +"&";
		if(m_cPI.StartPageButton.ToString() != "" && m_cPI.StartPageButton.ToString() != null)
			uri += "spb="+ m_cPI.StartPageButton.ToString() +"&";
		if(m_command != "")
			m_cPI.URI += "cmd="+m_command;
		int i = m_cPI.GetStartRow();
		int end = i + m_cPI.PageSize;
		string sPageIndex = m_cPI.Print();

		Response.Write("<form name=frm method=post>");
		Response.Write("<table width=90% align=center cellspacing=2 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=3>");

		Response.Write("<b>SEARCH:</b> <input type=text name=search value='"+ m_payee_name +"' onclick=\"this.value=''; this.select();\"><input type=submit name=cmd value='SEARCH PAYEE' "+ Session["button_style"] +">");
		Response.Write("<input type=button name=cmd value='<< Back' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"]+"')\">");
		Response.Write("</td><td align=right colspan=3>");
		Response.Write(sPageIndex);
		Response.Write("</td></tr>");
		Response.Write("<tr style='color:white;background-color:#666696;font-weight:bold;' align=left><th>CARD ID</th><th>NAME</th><th>COMPANY</th><th>PHONE</th><th>EMAIL</th></tr>");
		bool bAlter = false;
		for(; i<rows && i<end; i++)
		{
			DataRow dr = ds.Tables["refund_cust"].Rows[i];
			string id = dr["id"].ToString();
			string name = dr["name"].ToString();
			string company = dr["company"].ToString();
			string email = dr["email"].ToString();
			string phone = dr["phone"].ToString();
				
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEE ");
			Response.Write(">");
			bAlter = !bAlter;
			Response.Write("<td><a title='select this "+name+"' href='"+ uri +"cid="+ id +"' class=o>");
			Response.Write(id);
			Response.Write("</a></td>");
			Response.Write("<td><a title='select this "+name+"' href='"+ uri +"cid="+ id +"' class=o>");
			Response.Write(name);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(company);
			Response.Write("</td>");
			Response.Write("<td><a title='select this "+name+"' href='"+ uri +"cid="+ id +"' class=o>");
			Response.Write(email);
			Response.Write("</td>");Response.Write("<td>");
			Response.Write(phone);
			Response.Write("</td>");
	
			Response.Write("</tr>");
		}
		Response.Write("</table>");
		Response.Write("</form>");
	}
	return true;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////

bool DoQueryRefund()
{
	m_tableTitle = "REFUND REPORT";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, r.recorded_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, r.recorded_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, r.recorded_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		//m_dateSql = " AND i.commit_date >=  '" + m_sdFrom + "' AND  i.commit_date <= DATEADD(day, 1, '" + m_sdTo + " 23:59"+"') ";
		//m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND  DATEADD(day, 1, '" + m_sdTo + " 23:59"+"') ";
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59"+"'  ";
		break;
	default:
		break;
	}

//DEBUG(" m_dateSql=", m_dateSql);
	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT r.total, c.name AS staff, r.recorded_date, rs.amount_refund, rs.amount_owe, r.from_account, a.name1 +' '+ a.name2 +' '+ a.name4 AS account_name ";
	sc += " , c1.name AS cust, c1.company, r.card_id, r.payment_ref, r.note, rs.amount_owe, rs.amount_refund, rs.invoice_number ";
	sc += " FROM acc_refund r JOIN acc_refund_sub rs ON rs.id = r.id ";
	sc += " JOIN invoice i ON i.invoice_number = rs.invoice_number ";
	sc += " JOIN card c ON c.id = r.recorded_by ";
	sc += " JOIN card c1 ON c1.id = r.card_id ";
	sc += " JOIN account a ON a.id = r.from_account ";

	sc += " WHERE 1=1 ";
	sc += m_dateSql;

	if(Request.Form["slt_card"] != null && Request.Form["slt_card"] != "")
		sc += " AND r.card_id = "+ Request.Form["slt_card"] +"";
	
	//sc += " WHERE sales_qty > 0 ";
	sc += " ORDER BY r.recorded_date DESC ";	
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
	if(ds.Tables["report"].Rows.Count <= 0 )
	{
		Session["payee_id"] = null;
		Session["payee_name"] = null;
		Response.Write("<script language=javascript> window.alert('No Records for : \\r\\n\\t"+ m_payee_name.ToUpper() +"');window.location=('"+ Request.ServerVariables["URL"] +"');</script");
		Response.Write(">");
		return false;
	}

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

BindRefundReport();

	return true;
}

/////////////////////////////////////////////////////////////////
void BindRefundReport()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	if(m_sdTo != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	m_cPI.PageSize = 50;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	//DEBUG(" rows = ", rows );
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6>");
	Response.Write("<input type=button value='NEW QUERY' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");
	Response.Write("<input type=button value='PRINT REPORT' "+ Session["button_style"] +" onclick=\"window.print()\">");
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\" align=left>");
	//Response.Write("<th>Date</th>");
	Response.Write("<th>INV#</th>");
	Response.Write("<th>PAYEE</th>");
	Response.Write("<th>FROM ACC</th>");
	Response.Write("<th>PAYMENT REF</th>");
	Response.Write("<th>NOTE</th>");
	Response.Write("<th>RECORDED BY</th>");
	Response.Write("<th align=right>AMOUNT OWE</th>");
	Response.Write("<th align=right>AMOUNT REFUND</th>");
	Response.Write("<th align=right>TOTAL OWE</th>");
	Response.Write("</tr>");

	double dTotalOwe = 0;
	double dTotal = 0;
	double dTotalRefund = 0;

	bool bAlterColor = false;

	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
 		string total = dr["total"].ToString();
		string recorded_by = dr["staff"].ToString();
		string recorded_date = dr["recorded_date"].ToString();
		string from_account = dr["from_account"].ToString();
		string account = dr["account_name"].ToString();
		string cust = dr["cust"].ToString();
		if(cust == "" || cust == null)
			cust = dr["company"].ToString();
		string card_id = dr["card_id"].ToString();
		string payment_ref = dr["payment_ref"].ToString();
		string note = dr["note"].ToString();
		string amount_owe = dr["amount_owe"].ToString();
		string amount_refund = dr["amount_refund"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		if(amount_owe == null || amount_owe == "")
			amount_owe = "0";
		if(amount_refund == null || amount_refund == "")
			amount_refund = "0";
		if(total == null || total == "")
			total = "0";
		
		dTotalOwe += double.Parse(amount_owe);
		dTotalRefund += double.Parse(amount_refund);
		//dTotal += double.Parse(total);
		dTotal = dTotalOwe - dTotalRefund;
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		//Response.Write("<td>" + date + "</td>");
		Response.Write("<td><a title='view invoice' href='invoice.aspx?id=" + invoice_number + "' class=o>" + invoice_number + "</td>");
		Response.Write("<td>" + cust + "</td>");
		Response.Write("<td>" + account + "</td>");
		Response.Write("<td align=center>" + payment_ref + "</a></td>");
		Response.Write("<td align=center>" + note + "</a></td>");
		Response.Write("<td align=center>" + recorded_by + "</a></td>");
		Response.Write("<td align=right>" + double.Parse(amount_owe).ToString("c") + "</td>");
		Response.Write("<td align=right>" + double.Parse(amount_refund).ToString("c") + "</td>");
		//Response.Write("<td align=right>" + total + "</td>");
		Response.Write("<td align=right>" + (double.Parse(amount_owe) - double.Parse(amount_refund)).ToString("c") + "</td>");
		
		Response.Write("</tr>");

	}

	//total
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=6 align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:16\">" + dTotalOwe.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:16\">" + dTotalRefund.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:16\">" + dTotal.ToString("c") + "</td>");
	Response.Write("</tr>");
	
	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");

	Response.Write("</table>");
	Response.Write("<br>");

	
}





</script>


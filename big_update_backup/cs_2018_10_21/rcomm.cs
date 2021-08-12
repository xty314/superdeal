<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

<!-- #include file="page_index.cs" -->
<script runat=server>

string m_type = "0";
string m_tableTitle = "Sales & Commission Report";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = 0;

bool m_bShowPic = true;
bool m_bPickTime = false;
bool m_bGBSetShowPicOnReport = true;

StringBuilder m_sb = new StringBuilder();  //xml data for 3d chart
string m_picFile = "";
double m_nMaxY = 0;
double m_nMinY = 0;
double m_nMaxX = 0;
bool m_bHasLegends = true;
string m_xLabel = "";
string m_yLabel = "";
string[] m_IslandTitle = new string[64];
int m_nIsland = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] sct = new string[16];
string[] m_EachMonth = new string[13];
int cts = 0;
int m_ct = 1;

string m_salesID = "";
string m_salesName = "";
string m_inv = "";
string m_branchID = "";
string m_branchName = "";

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

	m_bGBSetShowPicOnReport = MyBooleanParse(GetSiteSettings("set_display_chart_on_report", "1", true));
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
	{
		m_salesID = Request.QueryString["s"];
		m_type = Request.QueryString["type"];
		if(Request.QueryString["pr"] != null)
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	}
	else if(Request.Form["cmd"] == "View Report")
	{
		m_salesID = Request.Form["employee"].ToString();
		if(!doShowEmployee())
			return;
		if(Request.Form["employee"] == "")
			m_salesName = "All";

	}
//	else if(m_branchID != "" && m_branchID != "0" && m_branchID != "all")
//		m_salesID = Session["card_id"].ToString();

	if(m_salesID != "" && m_salesID != "all")
	{
		DataRow dr = GetCardData(m_salesID);
		if(dr != null)
			m_salesName = dr["name"].ToString();
	}

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

	if(Request.Form["chart_type"] != null)
		m_ct = MyIntParse(Request.Form["chart_type"]);

	
	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["type"] == null || Request.QueryString["type"] == "")
		{
			PrintMainPage();
			return;
		}
		if(Request.QueryString["np"] != null && Request.QueryString["np"] != "")
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
		m_type = Request.QueryString["type"];
		m_code = Request.QueryString["code"];
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
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		//day = Request.Form["day_to"];
		//monthYear = Request.Form["month_to"];
		//ValidateMonthDay(monthYear, ref day);
		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
	if(Request.QueryString["type"] != null && Request.QueryString["type"] != "")
		m_type = Request.QueryString["type"].ToString();

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

	PrintAdminHeader();
	PrintAdminMenu();
	
	DoSRItem();

	PrintAdminFooter();
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

bool doShowEmployee()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id, sales ";
	sc += " FROM card ";
	sc += " WHERE type = 4";
	if(Session["branch_support"] != null && m_branchID != "" && m_branchID != "0" && m_branchID != "all")
	{
		sc += " AND our_branch = " + m_branchID;
	}
	if(m_salesID != null && m_salesID != "" && m_salesID != "" && m_salesID != "0" && m_salesID != "all")
		sc += " AND id = "+ m_salesID + "";
//DEBUG("sc=", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "employee"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1)
	{
		m_salesName = ds.Tables["employee"].Rows[0]["name"].ToString();
	}
	
	return true;
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<form name=f action=?");
	if(m_salesID != "")
		Response.Write("s=" + m_salesID);
	Response.Write(" method=post>");

	Response.Write("<center><h3>Commission Report");
	if(m_salesName != "")
		Response.Write(" - " + m_salesName);
	Response.Write("</h3>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");


	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Report Option</b></td></tr>");
	Response.Write("<tr><td><table>");
	Response.Write("<tr><td><b>Branch : </b></td><td>");
	PrintBranchNameOptions(m_branchID, "?branch=", true);
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Sales : </b></td><td>");
	Response.Write("<select name=employee><option value='all'>All");
	if(!doShowEmployee())
		return;
	
	int numrow = ds.Tables["employee"].Rows.Count;
	string[] sales = new string[numrow];
	for(int ii=0; ii < numrow; ii++)
	{
		DataRow dr = ds.Tables["employee"].Rows[ii];
		
		string name = dr["name"].ToString();
		string id = dr["id"].ToString();
		sales[ii] =	dr["sales"].ToString();

		Response.Write("\r\n <option value='"+id+"'>"+ name +"</option>");
	}
	Response.Write("</select>\r\n");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");

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
		txtMonth = m_EachMonth[m-1];
		
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
		txtMonth = m_EachMonth[m-1];
		
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
	Response.Write("</td></tr>");
		
	Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}


///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool DoSRItem()
{
	if(m_branchID != "" && m_branchID != "all" && m_branchID != "0")
		m_branchName = GetBranchNameByID(m_branchID);
	else
		m_branchName = "All";
	if(m_salesID != "-1" && m_salesID != "" && m_salesID != "all")
		m_salesName = GetCardNameByID(m_salesID);

	m_tableTitle = "Commission Report";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59 "+"' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT  s.code, s.supplier, s.supplier_code, s.name, SUM(s.quantity) AS qty ";
	sc += ", c.commission_rate ";
	sc += ", ISNULL(s.supplier_price, s.commit_price) AS supplier_price, s.commit_price  ";
	sc += " FROM sales s ";
	sc += " JOIN orders o ON o.invoice_number = s.invoice_number "; 
	sc += " JOIN code_relations c ON c.code = s.code ";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " LEFT OUTER JOIN card ca ON ca.id = o.card_id ";
	sc += " WHERE 1 = 1 ";
	if(m_branchID != "" && m_branchID != "all" && m_branchID != "0")
		sc += " AND o.branch = " + m_branchID;
	if(m_salesID != "-1" && m_salesID != "" && m_salesID != "all")
		sc += " AND o.sales = " + m_salesID;
	if(m_salesID == "-1")
		sc += " AND o.sales IS NULL ";
	sc += m_dateSql;
	sc += " GROUP BY s.code, s.supplier, s.supplier_code, s.name, c.commission_rate ";
	sc += ", s.supplier_price, s.commit_price ";
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

	Response.Write("<br><center><font size=+1><b>" + m_tableTitle + "</b></font><br>");
	Response.Write("<b>Branch : " + m_branchName + "</b><br>");
	if(m_salesName != "")
		Response.Write("<b>Sales : " + m_salesName + "</b><br>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	BindSRItem();
	/*
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	

		Response.Write("<form action=? method=post>");
		Response.Write("<input type=hidden name=period value=" + m_nPeriod + ">");
		Response.Write("<input type=hidden name=type value=" + m_type + ">");
		Response.Write("<input type=hidden name=day_from value=" + Request.Form["day_from"] + ">");
		Response.Write("<input type=hidden name=month_from value=" + Request.Form["month_from"] + ">");
		Response.Write("<input type=hidden name=day_to value=" + Request.Form["day_to"] + ">");
		Response.Write("<input type=hidden name=month_to value=" + Request.Form["month_to"] + ">");

		Response.Write("<b>Chart Type : </b>");
		Response.Write("<select name=chart_type>");
		for(int i=0; i<cts; i++)
		{
			Response.Write("<option value=" + i.ToString());
			if(m_ct == i)
				Response.Write(" selected");
			Response.Write(">" + sct[i] + "</option>");
		}
		Response.Write("</select>");

		Response.Write(" <input type=submit name=cmd value=Redraw " + Session["button_style"] + ">");
		Response.Write("</form>");
	}*/
	return true;
}

/////////////////////////////////////////////////////////////////
void BindSRItem()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 99999;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	if(m_salesID != "" && m_salesID != null)
		m_cPI.URI += "&s="+ m_salesID +"";
	//m_cPI.URI += "&s="+ m_salesID +"&type="+ m_type +"&pr="+ m_nPeriod +"";
	if(m_sdFrom != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	//if(m_nPeriod == 3)
	//	m_cPI.URI += "&to="+ m_sdTo +"&frm="+ m_sdFrom +"";
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Code</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>Name</th>");
	Response.Write("<th>SalesPrice</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>CommRate</th>");
	Response.Write("<th>Commission</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		m_bShowPic = false;
		Response.Write("</table>");
		return;
	}

	//for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";
	int margins = 0;

	double dSalesTotal = 0;
	double dCostTotal = 0;
	double dProfitTotal = 0;
	double dMarginTotal = 0;
	double dTotalQTY = 0;
	double dTotalFreight = 0;
	double dAllTotal = 0;
    double dCommissionRate = 0;
	double dCommission = 0;
	double dTotalCommission = 0;

	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	StringBuilder sb3 = new StringBuilder();
	int n_count = 0;
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sales_qty = dr["qty"].ToString();
		string cost = dr["supplier_price"].ToString();
		double dSalesPrice = MyDoubleParse(dr["commit_price"].ToString());
		dCommissionRate = MyDoubleParse(dr["commission_rate"].ToString());
		dCommission = 0;
	    dCommission = dSalesPrice * MyDoubleParse(sales_qty) * dCommissionRate / 100;
		dTotalCommission += dCommission;

		if(MyIntParse(sales_qty) == 0)
			continue;

		margins++;

		dSalesTotal += dSalesPrice * MyDoubleParse(sales_qty);
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=right>" + dSalesPrice.ToString("c") + "</td>");
		Response.Write("<td align=center>" + sales_qty + "</a></td>");
		Response.Write("<td align=right>" + dCommissionRate);
		Response.Write("%");

		Response.Write("</td>");
		Response.Write("<td align=right>" + dCommission.ToString("c") + "</td>");
		Response.Write("</tr>");
	}


	//total
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3 style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:13\">&nbsp;</td>");
	Response.Write("<td align=right style=\"font-size:13\"></td>");
	Response.Write("<td align=right style=\"font-size:13\"></td>");
	Response.Write("<td align=right nowrap style=\"font-size:13\">" + dTotalCommission.ToString("c") + "</td>");
	Response.Write("</tr>");

	Response.Write("</center></h4>");
}
string GetBranchNameByID(string id)
{
	if(MyIntParse(id) <= 0)
		return "";
	if(dstcom.Tables["get_branch_name"] != null)
		dstcom.Tables["get_branch_name"].Clear();

	string sc = " SELECT name FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "get_branch_name") > 0)
			return dstcom.Tables["get_branch_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return "";
}
string GetCardNameByID(string id)
{
	if(MyIntParse(id) <= 0)
		return "";
	if(dstcom.Tables["get_card_name"] != null)
		dstcom.Tables["get_card_name"].Clear();

	string sc = " SELECT name FROM card WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "get_card_name") > 0)
			return dstcom.Tables["get_card_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return "";
}
</script>

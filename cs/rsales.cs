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

string m_salesID = "0";
string m_salesName = "";
string m_inv = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
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

	if(Request.QueryString["i"] != null)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		m_inv = Request.QueryString["i"];
		ShowDetails();
		PrintAdminFooter();
		return;
	}

	if(Request.QueryString["s"] != null)
	{
		m_salesID = Request.QueryString["s"];
		m_type = Request.QueryString["type"];
		if(Request.QueryString["pr"] != null)
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	}
	else
		m_salesID = Session["card_id"].ToString();

	DataRow dr = GetCardData(m_salesID);
	if(dr != null)
		m_salesName = dr["name"].ToString();

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

//	if(Request.Form["legends"] != "on")
//		m_bHasLegends = false;

	m_type = Request.QueryString["t"];
	if(Request.Form["type"] != null)
		m_type = Request.Form["type"];
	
	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["type"] == null || Request.QueryString["type"] == "")
		{
			PrintMainPage();
			return;
		}
		if(Request.QueryString["np"] != null && Request.QueryString["np"] != "")
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	//DEBUG("m_nPeriod=", m_nPeriod);	
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

//DEBUG("m_nperido =", m_nPeriod);
//DEBUG("mtype =", m_type);
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
	
//DEBUG("mtype = ", m_type);
	switch(MyIntParse(m_type))
	{
	case 1:
		DoSRInvoice();
		break;
	case 0:
		DoSRItem();
		break;
	default:
		break;
	}

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

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<form name=f action=rsales.aspx");
	if(m_salesID != "")
		Response.Write("?s=" + m_salesID);
	Response.Write(" method=post>");

	Response.Write("<center><h3>Select Report");
	if(m_salesName != "")
		Response.Write(" - " + m_salesName);
	Response.Write("</h3>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");


	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Report Type</b></td></tr>");
	//Response.Write("<tr><td colspan=2><input type=radio name=type value=0 checked>Invoice Based</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=type value=0 checked>Item Based</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=type value=1>Invoice Based</td></tr>");
	
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
		
//------ END second display date -----------

	/*
	//from date
	Response.Write("<tr><td><b> &nbsp; From Date </b>");
	Response.Write("<select name=day_from onchange=\"document.f.period.value=3;\">");
	for(; i<=31; i++)
	{
		Response.Write("<option value=" + i);
		Response.Write(">" + i + "</option>");
	}
	Response.Write("</select>");

	Response.Write("&nbsp&nbsp; <select name=month_from>");
	DateTime dstep = DateTime.Parse("01/01/2003");
	DateTime dend = DateTime.Now;
	while((dstep - dend).Days <= 0)
	{
		string value = dstep.ToString("MM-yyyy");
		string name = dstep.ToString("MMM yyyy");
		Response.Write("<option value='" + value + "'>" + name + "</option>");
		dstep = dstep.AddMonths(1);
	}
	Response.Write("</select>");

	//to date
	Response.Write("&nbsp; <b>To Date</b> &nbsp; ");
	Response.Write("<select name=day_to>");
	for(i=1; i<=31; i++)
	{
		Response.Write("<option value=" + i);
		if(i == DateTime.Now.Day)
			Response.Write(" selected");
		Response.Write(">" + i + "</option>");
	}
	Response.Write("</select>");

	Response.Write("&nbsp&nbsp; <select name=month_to>");
	dstep = DateTime.Parse("01/01/2003");
	while((dstep - dend).Days <= 0)
	{
		string value = dstep.ToString("MM-yyyy");
		string name = dstep.ToString("MMM yyyy");
		Response.Write("<option value='" + value + "' ");
		if(dstep.Month == dend.Month && dstep.Year == dend.Year)
			Response.Write(" selected");
		Response.Write(">" + name + "</option>");
		dstep = dstep.AddMonths(1);
	}
	Response.Write("</select>");

	Response.Write("</td></tr>");
	
	Response.Write("<tr><td>&nbsp;</td></tr>");
	*/
	Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}


void DrawChart()
{
	Stream chartFile = null;
	XmlDocument obXmlDoc = null;
	string uname = EncodeUserName();
	m_picFile = "./ri/" + uname + DateTime.Now.ToString("ddMMyyyyHHmmss") + "_datachart.jpg";
	
	doDeleteAllPicFiles(); //delete pic files

	string strFileName = m_picFile;
	string strDataXmlFile = "./ri/" + uname + "_chartdata.xml";
	try
	{
		chartFile = new FileStream(Server.MapPath(strFileName),
				FileMode.Create, FileAccess.ReadWrite);
		obXmlDoc = new XmlDocument();
		obXmlDoc.Load(Server.MapPath(strDataXmlFile));
		Graph dc = new BarGraph3D();
		switch(m_ct)
		{
		case 0:
			dc = new BarGraph2D();
			break;
		case 2:
			dc = new BlocksChart2D();
			break;
		case 3:
			dc = new BlocksChart3D();
			break;
		case 4:
			dc = new PieGraph2D();
			break;
		case 5:
			dc = new PieGraph3D();
			break;
		case 6:
			dc = new StackBarGraph2D();
			break;
		case 7:
			dc = new StackBarGraph3D();
			break;
		case 8:
			dc = new LineGraph2D();
			break;
		case 9:
			dc = new LineGraph3D();
			break;
		case 10:
			dc = new PointGraph();
			break;
		case 11:
			dc = new SplineGraph();
			break;
		case 12:
			dc = new SplineAreaGraph();
			break;
		default :
			break;
		}

		dc.SetGraphData (obXmlDoc);
		dc.Title = "Profit & Loss";
		for(int n=0; n<m_nIsland; n++)
			dc.IslandTitle[n] = m_IslandTitle[n];
		dc.HasTickers = false;
		dc.HasGridLines = true;
		dc.HasLegends = m_bHasLegends;
		dc.Width = 600;
		dc.Height = 500;
		dc.MinXValue = 0;
//		dc.XTickSpacing = 100;
		dc.MaxXValue = (int)m_nMaxX;
		dc.MinYValue = (int)m_nMinY;
		dc.MaxYValue = (int)m_nMaxY;
		dc.YAxisLabel = m_yLabel;
		dc.XAxisLabel = m_xLabel;
		dc.AxisPaddingTop = 100;
		dc.AxisPaddingBottom = 100;
		dc.YTickSpacing = (int)((m_nMaxY - m_nMinY)/ 10);
		dc.XTickSpacing = 1000;
//		dc.TickStep = 100;
		dc.TitlePaddingFromTop = 30;
//		dc.XAxisLabelColor = Color.Red;
		dc.DrawGraph(ImageFormat.Jpeg, ref chartFile);
		chartFile.Close();

		chartFile = null;
	}
	catch(Exception ex)
	{
		ShowExp("draw chart error", ex);
	}
	finally
	{
		if(null != chartFile)
		{
			chartFile.Close();
		}
	}
}

void WriteXMLFile()
{
	string s = "";
	s += "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n";
	s += "<chartdatalist>\r\n";
//	s += "<chartdataisland>\r\n";

	s += m_sb.ToString();

//	s += "</chartdataisland>\r\n";
	s += "</chartdatalist>\r\n";

	string uname = EncodeUserName();
	string strPath = Server.MapPath("./ri");
	strPath += "\\" + uname + "_chartdata.xml";
	
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	TextWriter tw = new StreamWriter(newFile);
	
	// Write data to the file
	tw.Write(s.ToCharArray());
	tw.Flush();
	
	newFile.Close();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool DoSRItem()
{
	m_tableTitle = "Sales & Commission - <font color=red>" + m_salesName + "</font> Item Based";
	
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
	sc += " SELECT  s.code, i.freight, s.supplier, s.supplier_code, s.name, s.quantity, ISNULL(s.supplier_price, s.commit_price) AS supplier_price  ";
	sc += ", i.price AS sales_total, s.commit_price ";
	sc += ", (SELECT SUM(s1.commit_price * s1.quantity) ";
	sc += " FROM sales s1 ";
	sc += " WHERE s1.invoice_number = s.invoice_number AND s1.invoice_number = i.invoice_number ";
	sc += m_dateSql;
	sc += " ) AS total_commit ";
	
	//sc += ", SUM(s.commit_price * s.quantity) AS total ";
	//sc += ", SUM(s.supplier_price * s.quantity) AS cost ";
	//sc += ", SUM((s.commit_price - s.supplier_price) * s.quantity) as profit ";
	sc += " FROM sales s JOIN orders o ON o.invoice_number = s.invoice_number "; 
	//sc += " LEFT OUTER JOIN sales_cost sc  ON sc.invoice_number = s.invoice_number AND sc.code = s.code";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
//	sc += " LEFT OUTER JOIN sales_cost sc ON sc.invoice_number = s.invoice_number AND sc.code = s.code ";

	if(m_salesID != "-1")
		sc += " WHERE o.sales = " + m_salesID;
	if(m_salesID == "-1")
		sc += " WHERE o.sales IS NULL ";
	sc += m_dateSql;
	//sc += " GROUP BY s.code, s.name, s.supplier, s.supplier_code, s.system, i.price, s.commit_price ";
	//sc += " ORDER BY profit ";

	/*sc += " SELECT s.system,  s.code, s.supplier, s.supplier_code, s.name, SUM(s.quantity) AS Quantity ";
	//sc += ", SUM(s.commit_price * s.quantity) AS sales_total ";
	sc += ", i.price AS sales_total ";
	sc += ", SUM(s.supplier_price * s.quantity) AS cost ";
	sc += ", SUM((s.commit_price - s.supplier_price) * s.quantity) as profit ";
	sc += " FROM sales s JOIN orders o ON o.invoice_number = s.invoice_number "; 
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " WHERE o.sales = " + m_salesID;
	sc += " GROUP BY s.code, s.name, s.supplier, s.supplier_code, s.system, i.price ";
	sc += " ORDER BY profit ";
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

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	BindSRItem();
	
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	

		Response.Write("<form action=rsales.aspx method=post>");
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
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindSRItem()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 60;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	//m_cPI.URI = "?branch="+ m_branchID +"&np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	m_cPI.URI = "&np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate(); //************************sean
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
	Response.Write("<th>Supplier</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>Name</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>Sales</th>");
	Response.Write("<th>Cost</th>");
	Response.Write("<th>Profit</th>");
	Response.Write("<th>Margin</th>");
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
		string sales_qty = dr["quantity"].ToString();
		string sales_amount = dr["sales_total"].ToString();
		double dSales = (MyDoubleParse(sales_amount));
		//string cost = dr["cost"].ToString();
		//double dCost = MyDoubleParse(cost);
		//string profit = dr["profit"].ToString();
		//double dProfit = MyDoubleParse(profit);
		string sfreight = dr["freight"].ToString();
		//string cost = dr["cost"].ToString();
		//if(cost == "0")
		string cost = dr["supplier_price"].ToString();
		double dCost = MyDoubleParse(cost) * MyDoubleParse(sales_qty);
		double dProfit = 0;
		string commit_price = dr["commit_price"].ToString();
		double dTotalCommit = MyDoubleParse(dr["total_commit"].ToString());
		dTotalQTY += MyDoubleParse(sales_qty);
		double dPercent = 1;

//DEBUG("sales =", dSales.ToString());
//DEBUG("total commit =", dTotalCommit.ToString());
//DEBUG(" ocmmit price = ", commit_price);
		double dSalesPrice = MyDoubleParse(commit_price);
		if(dSales < dTotalCommit)
		{
			dPercent -= ((dTotalCommit - dSales) / dTotalCommit);
			dSalesPrice = ((dSalesPrice * dPercent));
		}
		dSales = dSalesPrice;
		dSales *= MyDoubleParse(sales_qty);
		dProfit = dSales - dCost;
//	DEBUG(" selling price = ", dSalesPrice.ToString("c"));
//	DEBUG(" profit = ", dProfit.ToString("c"));

		double dMargin = 0;
		if(dSales >= dCost)
			dMargin = (dSales - dCost) / dSales;
		dMargin = Math.Round(dMargin, 4);

		if(MyIntParse(sales_qty) == 0)
			continue;

		margins++;

		dSalesTotal += dSales;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;
		dTotalFreight += MyDoubleParse(sfreight);
		dAllTotal += dSales + MyDoubleParse(sfreight);
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center>" + sales_qty + "</a></td>");
		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dCost.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dMargin.ToString("p") + "</td>");
		Response.Write("</tr>");

		if(dSales > m_nMaxY)
			m_nMaxY = dSales;

		if(dSales < 0 && dSales < m_nMinY)
			m_nMinY = dSales;

		//xml chart data
		x = (i).ToString();
		m_nMaxX = i;

		y = dProfit.ToString();
		name = XMLDecoding(name);
		legend = name.Replace("&", " ");
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");

		y = dCost.ToString();
		legend = name.Replace("&", " ");
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");

		y = dSales.ToString();
		legend = name.Replace("&", " ");
		sb3.Append("<chartdata>\r\n");
		sb3.Append("<x legend='" + legend + "'");
		sb3.Append(">" + x + "</x>\r\n");
		sb3.Append("<y>" + y + "</y>\r\n");
		sb3.Append("</chartdata>\r\n");
	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb3.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Gross Profit";
	m_IslandTitle[1] = "--Gross Cost";
	m_IslandTitle[2] = "--Total Sales";
	m_nIsland = 3;

	double dMarginAve = 0;
	if(margins > 0)
		dMarginAve = Math.Round(dMarginTotal / margins, 4);

	//total
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=5  style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:13\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:13\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:13\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:13\">" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");

	dSalesTotal = 0;
		dCostTotal = 0;
		dProfitTotal = 0;
		dMarginTotal = 0;
		dTotalFreight = 0;
		dAllTotal = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string sales_qty = dr["quantity"].ToString();
		string sales_amount = dr["sales_total"].ToString();
		double dSales = (MyDoubleParse(sales_amount));
		string sfreight = dr["freight"].ToString();
		string cost = dr["supplier_price"].ToString();
		double dCost = MyDoubleParse(cost) * MyDoubleParse(sales_qty);
		double dProfit = 0;
		string commit_price = dr["commit_price"].ToString();
		double dTotalCommit = MyDoubleParse(dr["total_commit"].ToString());
		dTotalQTY += MyDoubleParse(sales_qty);
		double dPercent = 1;
		double dSalesPrice = MyDoubleParse(commit_price);
		if(dSales < dTotalCommit)
		{
			dPercent -= ((dTotalCommit - dSales) / dTotalCommit);
			dSalesPrice = ((dSalesPrice * dPercent));
		}
		dSales = dSalesPrice;
		dSales *= MyDoubleParse(sales_qty);
		dProfit = dSales - dCost;
		double dMargin = 0;
		if(dSales >= dCost)
			dMargin = (dSales - dCost) / dSales;
		dMargin = Math.Round(dMargin, 4);

		if(MyIntParse(sales_qty) == 0)
			continue;

		dSalesTotal += dSales;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;
		dTotalFreight += MyDoubleParse(sfreight);
		dAllTotal += dSales + MyDoubleParse(sfreight);
		}

		Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=5 style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<br><center><h4>");
	Response.Write("<b>Total Freight : </b><font color=red>" + dTotalFreight.ToString("c") + "</font>&nbsp&nbsp;");
	Response.Write("<b>Total Sales : </b><font color=red>" + dSalesTotal.ToString("c") + "</font>&nbsp;");
	Response.Write("<b>Sub Total : </b><font color=red>" + dAllTotal.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</center></h4>");
	//write xml data file for chart image
	if(m_bShowPic && m_bGBSetShowPicOnReport)
		WriteXMLFile();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//
bool DoSRInvoice()
{
	m_tableTitle = "Sales & Commission - <font color=red>" + m_salesName + "</font> Invoice Based";
	
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
	
/*	string sc = " SET DATEFORMAT dmy ";
	sc += "	SELECT s.invoice_number,i.commit_date ";
	sc += ", SUM((s.supplier_price) * s.quantity) as profit ";
	//sc += ", SUM((s.commit_price - s.supplier_price) * s.quantity) as profit ";
	//sc += ", SUM(s.commit_price * s.quantity) as sales_total ";
	sc += ", i.price AS sales_total ";
	sc += "	FROM sales s JOIN orders o ON o.invoice_number = s.invoice_number "; 
	sc += "	JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " WHERE 1=1 ";
	
	sc += m_dateSql;
	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
	{
		if(TSIsDigit(Request.QueryString["code"].ToString()))
		{
			sc += " AND i.invoice_number = (SELECT ss.invoice_number FROM sales ss WHERE ss.code = "+ Request.QueryString["code"] +" ";
			sc += " AND ss.invoice_number = i.invoice_number AND ss.invoice_number = s.invoice_number )";
		}
	}
	else
	{
		if(m_salesID != "-1")
			sc += "	AND o.sales = " + m_salesID;
	}
	if(m_salesID == "-1")
		sc += "	AND o.sales IS NULL ";
	sc += "	GROUP BY s.invoice_number, i.commit_date , i.price";
	sc += "	ORDER BY profit ";
*/

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.invoice_number, i.commit_date, i.price AS sales_total ";
//	sc += ", i.price - SUM(ISNULL(s.supplier_price, s.commit_price) * s.quantity) AS profit ";
	sc += ", SUM(ISNULL(s.commit_price - s.supplier_price, s.commit_price) * s.quantity) AS profit ";
//	sc += ", i.price - SUM(ISNULL(sc.cost, s.commit_price) * s.quantity) AS profit ";
//	sc += ", SUM(ISNULL(s.commit_price - s.supplier_price, s.commit_price - s.commit_price) * s.quantity) AS profit ";
	sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " JOIN sales s ON s.invoice_number = i.invoice_number ";
	//sc += " LEFT OUTER JOIN sales_cost sc ON sc.invoice_number = s.invoice_number AND sc.code = s.code ";
	if(m_salesID != "-1")
		sc += "	WHERE o.sales = " + m_salesID;
	if(m_salesID == "-1")
		sc += "	WHERE o.sales IS NULL ";
	sc += m_dateSql;
	sc += "	GROUP BY i.invoice_number, i.commit_date , i.price";
	sc += "	ORDER BY profit ";

//DEBUG(" sc += ", sc);
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

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	BindSRInvoice();

	m_xLabel = "Invoice";
	m_yLabel = "Profit";
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=rsales.aspx method=post>");
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
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindSRInvoice()
{
	int i = 0;
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 60;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	//m_cPI.URI = "?branch="+ m_branchID +"&r=" + DateTime.Now.ToOADate();
	m_cPI.URI = "&r=" + DateTime.Now.ToOADate(); //**************************sean
	if(m_salesID != "" && m_salesID != null)
		m_cPI.URI += "&s="+ m_salesID +"&type="+ m_type +"&np="+ m_nPeriod +"";
	if(m_sdFrom != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Invoice_Number</th>");
	Response.Write("<th>Sales</th>");
	Response.Write("<th>Profit</th>");
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

	double dSalesTotal = 0;
	double dProfitTotal = 0;

	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string sdate = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string sales = dr["sales_total"].ToString();
		double dSales = MyDoubleParse(sales);
		string profit = dr["profit"].ToString();
		double dProfit = MyDoubleParse(profit);

		dSalesTotal += dSales;
		dProfitTotal += dProfit;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + sdate + "</td>");
		Response.Write("<td nowrap>");
		Response.Write("<a href=rsales.aspx?i=" + invoice_number + " class=o target=_blank title='Show Details'>Inv#" + invoice_number + "</a> &nbsp&nbsp;");
		Response.Write("<a href=invoice.aspx?id=" + invoice_number + " class=o target=_blank title='View Invoice'>View Invoice</a>");
		Response.Write("</td>");
		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		Response.Write("</tr>");

		if(dSales > m_nMaxY)
			m_nMaxY = dSales;

		if(dSales < 0 && dSales < m_nMinY)
			m_nMinY = dSales;

		//xml chart data
		x = (i*10).ToString();
		m_nMaxX = i*10;

		y = dProfit.ToString();

		invoice_number = XMLDecoding(invoice_number);
		legend = invoice_number;
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");

		x = (MyIntParse(x) + 5).ToString();
		y = dSales.ToString();
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x");
		if(m_bHasLegends)
			sb2.Append(" legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
//		sb2.Append("<z>" + 100 + "</z>\r\n");
		sb2.Append("</chartdata>\r\n");

	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Profit";
	m_IslandTitle[1] = "--Sales";
	m_nIsland = 2;

/*
	//xml chart data
	x = (i * 10).ToString();
	y = dProfitTotal.ToString();
//		color = m_c[i];
	legend = "Total";
	m_sb.Append("<chartdata>\r\n");
	m_sb.Append("<x legend='" + legend + "'>" + x + "</x>\r\n");
	m_sb.Append("<y>" + y + "</y>\r\n");
//		m_sb.Append("<color>" + color + "</color>\r\n");
	m_sb.Append("</chartdata>\r\n");
*/
	//total
	Response.Write("<tr></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
	Response.Write("<td colspan=2 style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:13\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:13\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("</tr>");
	dSalesTotal =0;
		dProfitTotal =0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string sales = dr["sales_total"].ToString();
		double dSales = MyDoubleParse(sales);
		string profit = dr["profit"].ToString();
		double dProfit = MyDoubleParse(profit);

		dSalesTotal += dSales;
		dProfitTotal += dProfit;
	}

	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\">");
	Response.Write("<td colspan=2  style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=3>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

	//write xml data file for chart image
	if(m_bShowPic && m_bGBSetShowPicOnReport)
		WriteXMLFile();
}

bool ShowDetails()
{
	int rows = 0;
	string sc = " SELECT s.invoice_number, s.code, s.name, s.commit_price, s.supplier_price, s.quantity ";
	sc += ", (s.commit_price - isnull(s.supplier_price, s.commit_price)) * s.quantity as profit ";
	sc += " FROM sales s ";
	sc += " WHERE s.invoice_number = " + m_inv;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "report");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<br><center><h3>Invoice #" + m_inv + "</h3>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Code</th>");
	Response.Write("<th>Name</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>Commit_Price</th>");
	Response.Write("<th>Cost</th>");
	Response.Write("<th>Profit</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		m_bShowPic = false;
		Response.Write("</table>");
		return true;
	}

	//for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";

	double dSalesTotal = 0;
	double dCostTotal = 0;
	double dProfitTotal = 0;

	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string sales = dr["commit_price"].ToString();
		double dSales = MyDoubleParse(sales);
		string cost = dr["supplier_price"].ToString();
		double dCost = MyDoubleParse(cost);
		string qty = dr["quantity"].ToString();
		string profit = dr["profit"].ToString();
		double dProfit = MyDoubleParse(profit);

		dSalesTotal += dSales;
		dCostTotal += dCost;
		dProfitTotal += dProfit;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center>" + qty + "</td>");
		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dCost.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	//total
	Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
	Response.Write("<td colspan=3 style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:13\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:13\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:13\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("</tr>");
		dSalesTotal = 0;
		dCostTotal = 0;
		dProfitTotal = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string sales = dr["commit_price"].ToString();
		double dSales = MyDoubleParse(sales);
		string cost = dr["supplier_price"].ToString();
		double dCost = MyDoubleParse(cost);
		string qty = dr["quantity"].ToString();
		string profit = dr["profit"].ToString();
		double dProfit = MyDoubleParse(profit);

		dSalesTotal += dSales;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
	}

	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\">");
	Response.Write("<td colspan=3 style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("<br><br><center><a href=close.htm class=o><font size=+1><b>Close Window</b></a>");
	
	return true;
}
</script>

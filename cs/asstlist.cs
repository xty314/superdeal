<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

<script runat=server>

string m_type = "0";
string m_tableTitle = "Fixed Assets List";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = 0;

bool m_bPickTime = false;

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

DataSet ds = new DataSet();
DataSet dst = new DataSet();
string[] sct = new string[16];
int cts = 0;
int m_ct = 1;

string m_salesID = "0";
string m_salesName = "";
string m_inv = "";

bool m_bAuto = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["t"] == "ph") //show payment history
	{
		PrintAdminHeader();
		PrintAdminMenu();
		PrintPaymentHistoryForm();
		return;
	}

	if(Request.QueryString["autolist"] == "1")
	{
		m_bAuto = true;
		PrintAdminHeader();
		PrintAdminMenu();
		DoAssetsList();
		PrintAdminFooter();
		return;
	}

	if(Request.QueryString["s"] != null)
		m_salesID = Request.QueryString["s"];
	else
		m_salesID = Session["card_id"].ToString();

	DataRow dr = GetCardData(m_salesID);
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
			PrintAdminFooter();
			return;
		}
		m_type = Request.QueryString["type"];
		m_code = Request.QueryString["code"];
	}

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
	//if(Request.Form["day_from"] != null)
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
	
	string cmd = Request.Form["cmd"];
	if(cmd == "Payment History")
		PrintPaymentHistoryForm();
	else
		DoAssetsList();

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
	
	Response.Write("<form name=f action=asstlist.aspx");
	if(m_salesID != "")
		Response.Write("?s=" + m_salesID);
	Response.Write(" method=post>");

	Response.Write("<br><center><h3>Select Date Range</h3>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

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
	Response.Write("</td>");
		//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
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
		
//------ END second display date -----------

	//from date
	/*Response.Write("<tr><td><b> &nbsp; From Date </b>");
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
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value='List Assets' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Payment History' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

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
		dc.Title = "Fixed Assets";
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
bool DoAssetsList()
{
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, e.date_recorded, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, e.date_recorded, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, e.date_recorded, GETDATE()) >= 1 AND DATEDIFF(month, e.date_recorded, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND e.date_recorded >= '" + m_sdFrom + "' AND e.date_recorded <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT e.*, c.name, c.trading_name, c.company, c1.name AS accountant ";
	sc += ", a.name4 + a.name1 AS assets_type ";
	if(m_bAuto)
		sc += ", enum.name AS frequency, ae.next_payment_date ";
	sc += " FROM assets e LEFT OUTER JOIN card c ON c.id = e.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = e.recorded_by ";
	sc += " JOIN account a ON a.id = e.to_account ";
	if(m_bAuto)
	{
		sc += " JOIN auto_assets ae ON ae.id = e.id ";
		sc += " LEFT OUTER JOIN enum ON enum.id = ae.frequency AND enum.class='autopayment_frequency' ";
	}
	else
	{
		sc += " WHERE 1=1 ";
		sc += m_dateSql;
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

	if(m_bAuto)
		m_tableTitle = "Auto Payment List";
	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");

	if(!m_bAuto)
		Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	BindList();

	if(m_bAuto)
		return true;

	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT SUM(e.total) AS total, a.name4 + ' ' + a.name1 AS type ";
	sc += " FROM assets e JOIN account a ON a.id = e.to_account ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql;
	sc += " GROUP BY a.name4, a.name1 ";
	sc += " ORDER BY total desc ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "total");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb1 = new StringBuilder();

	string x = "";
	string y = "";
	string legend = "";

	for(int i=0; i<ds.Tables["total"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["total"].Rows[i];
		double dTotal = MyDoubleParse(dr["total"].ToString());
		string type = dr["type"].ToString();

		if(dTotal > m_nMaxY)
			m_nMaxY = dTotal;

		if(dTotal < 0 && dTotal < m_nMinY)
			m_nMinY = dTotal;

		//xml chart data
		x = (i).ToString();
		m_nMaxX = i;

		y = dTotal.ToString();
		legend = type.Replace("&", " ");
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");
	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Assets";
	m_nIsland = 1;

	//write xml data file for chart image
	WriteXMLFile();
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=asstlilst.aspx method=post>");
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
	return true;
}

/////////////////////////////////////////////////////////////////
void BindList()
{
	int i = 0;
	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>ID</th>");
//	if(!m_bAuto)
//		Response.Write("<th nowrap>Payment Date</th>");
	Response.Write("<th nowrap>Assets Type</th>");
	Response.Write("<th>Payee</th>");
	Response.Write("<th nowrap>Recorded By</th>");
	Response.Write("<th>Tax</th>");
	Response.Write("<th>Amount</th>");
	Response.Write("<th>Amount Paid</th>");
	Response.Write("<th>Amount Owed</th>");
	if(m_bAuto)
	{
		Response.Write("<th>Frequency</th>");
		Response.Write("<th>NextPayment_Date</th>");
	}
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	double dSubTax = 0;
	double dSubTotal = 0;
	double dPaidTotal = 0;

	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	for(; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string id = dr["id"].ToString();
		string card_id = dr["card_id"].ToString();
		string payee = dr["trading_name"].ToString();
		if(payee == "")
			payee = dr["company"].ToString();
		if(payee == "")
			payee = dr["name"].ToString();
		double dTax = MyDoubleParse(dr["tax"].ToString());
		double dTotal = MyDoubleParse(dr["total"].ToString());
		double dPaid = MyDoubleParse(dr["amount_paid"].ToString());
		double dOwed = dTotal - dPaid;
		string sOwed = "";
		if(dOwed != 0)
			sOwed = dOwed.ToString("c");
//		string payment_date = DateTime.Parse(dr["payment_date"].ToString()).ToString("dd-MM-yyyy");
		string recorded_by = dr["accountant"].ToString();
		string assets_type = dr["assets_type"].ToString();

		dSubTax += dTax;
		dSubTotal += dTotal;
		dPaidTotal += dPaid;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + id);
		Response.Write(" <a href=assets.aspx?id=" + id + " class=o>Edit</a>");
//		Response.Write(" <a href=autopay.aspx?id=" + id + " class=o>Auto</a>");
		Response.Write("</td>");
//		if(!m_bAuto)
//			Response.Write("<td>" + payment_date + "</td>");
		Response.Write("<td>" + assets_type + "</td>");
		Response.Write("<td>" + payee + "</td>");
		Response.Write("<td>" + recorded_by + "</a></td>");
		Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		Response.Write("<td align=right><a href=asstlist.aspx?t=ph&id=" + id);
		Response.Write(" class=o traget=_blank title='Payment History'>" + dPaid.ToString("c") + "</a></td>");
		Response.Write("<td align=right>" + sOwed + "</td>");
		if(m_bAuto)
		{
			Response.Write("<td align=right>" + Capital(dr["frequency"].ToString()) + "</td>");
			Response.Write("<td align=right>" + DateTime.Parse(dr["next_payment_date"].ToString()).ToString("dd-MM-yyyy") + "</td>");
		}
		Response.Write("</tr>");

	}

	//total
	if(!m_bAuto)
	{
		Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
		Response.Write(">");
		Response.Write("<td colspan=4 align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
		Response.Write("<td align=right style=\"font-size:16\">" + dSubTax.ToString("c") + "</td>");
		Response.Write("<td align=right style=\"font-size:16\">" + dSubTotal.ToString("c") + "</td>");
		Response.Write("<td align=right style=\"font-size:16\">" + dPaidTotal.ToString("c") + "</td>");
		Response.Write("<td align=right style=\"font-size:16\">" + (dSubTotal - dPaidTotal).ToString("c") + "</td>");
		Response.Write("</tr>");
	}

	Response.Write("</table>");
}

bool PrintPaymentHistoryForm()
{
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) >= 1 AND DATEDIFF(month, d.trans_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND d.trans_date >= '" + m_sdFrom + "' AND d.trans_date <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

	string assets_id = Request.QueryString["id"];
	string tran_id = Request.QueryString["tran_id"]; //this parameter comes from reconciliation
	if(tran_id != null && tran_id != "")
	{
		assets_id = GetAssetsIDFromTranID(tran_id);
	}

	string sc = " SELECT a.id, acc.name4 AS type, p.amount_applied, t.source AS from_account ";
	sc += ", d.trans_date, e.name AS payment_type, c.name AS staff ";
	sc += " FROM assets a JOIN assets_payment p ON p.assets_id = a.id ";
	sc += " JOIN trans t ON t.id = p.tran_id ";
	sc += " JOIN tran_detail d ON d.id = p.tran_id ";
	sc += " LEFT OUTER JOIN account acc ON acc.id = a.to_account ";
	sc += " LEFT OUTER JOIN enum e ON e.class='payment_method' AND e.id = d.payment_method ";
	sc += " LEFT OUTER JOIN card c ON c.id = d.staff_id ";
	sc += " WHERE 1=1 ";
	if(assets_id != null && assets_id != "")
		sc += " AND a.id = " + assets_id;
	if(m_dateSql != "")
		sc += m_dateSql;
	sc += " ORDER BY d.trans_date DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "ph");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<center><h4>Assets Payment History</h4>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th nowrap>AssetsType</th>");
	Response.Write("<th nowrap>AssetsDesc</th>");
	Response.Write("<th nowrap>FromAccount</th>");
	Response.Write("<th>PaymentDate</th>");
	Response.Write("<th>PaymentType</th>");
	Response.Write("<th>Staff</th>");
	Response.Write("<th>Amount</th>");
	Response.Write("</tr>");

	for(int i=0; i<ds.Tables["ph"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["ph"].Rows[i];
		string id = dr["id"].ToString();
		string type = dr["type"].ToString();
		string names = GetAssetsItemNames(id);
		string from_account = dr["from_account"].ToString();
		string payment_date = dr["trans_date"].ToString();
		string payment_type = dr["payment_type"].ToString();
		string staff = dr["staff"].ToString();
		string amount = dr["amount_applied"].ToString();

		Response.Write("<tr>");
		Response.Write("<td>" + type + "</td>");
		Response.Write("<td><a href=assets.aspx?id=" + id + " class=o target=_blank>" + names + "</a></td>");
		Response.Write("<td><a href=recon.aspx?t=new&fa=" + from_account + " class=o target=_blank>" + from_account + "</a></td>");
		Response.Write("<td>" + DateTime.Parse(payment_date).ToString("dd-MM-yyyy") + "</td>");
		Response.Write("<td>" + payment_type + "</td>");
		Response.Write("<td>" + staff + "</td>");
		Response.Write("<td>" + MyDoubleParse(amount).ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

string GetAssetsItemNames(string assets_id)
{
	if(dst.Tables["item_names"] != null)
		dst.Tables["item_names"].Clear();

	int nRows = 0;
	string names = "";
	string sc = " SELECT name FROM assets_item WHERE id = " + assets_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "item_names");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	for(int i=0; i<nRows; i++)
	{
		names += dst.Tables["item_names"].Rows[i]["name"].ToString();
		if(i < nRows - 1)
			names += ",";
	}
	return names;
}

string GetAssetsIDFromTranID(string tran_id)
{
	if(dst.Tables["getid"] != null)
		dst.Tables["getid"].Clear();
	string sc = " SELECT assets_id FROM assets_payment WHERE tran_id = " + tran_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "getid") > 0)
			return dst.Tables["getid"].Rows[0]["assets_id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}
</script>

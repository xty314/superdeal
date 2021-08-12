<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

<script runat=server>

string m_type = "0";
string m_tableTitle = "Expense List";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sdFrom = "";
string m_sdTo = "";
string m_syFrom = DateTime.Now.ToString("yyyy");
string m_syTo = DateTime.Now.ToString("yyyy");

int m_nPeriod = 0;

bool m_bPickTime = false;
bool m_bShowPic = true;

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

string m_ExpenseType = "";
string m_toAccount = "";
DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] sct = new string[16];
int cts = 0;
int m_ct = 1;

string[] m_EachMonth = new string[13];
string m_salesID = "0";
string m_salesName = "";
string m_inv = "";

bool m_bCompair = false;
bool m_bAuto = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Request.QueryString["autolist"] == "1")
    {
      	m_bAuto = true;
      	PrintAdminHeader();
      	PrintAdminMenu();
      	DoExpenseList();
      	PrintAdminFooter();
      	return;
    }
	
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
		m_salesID = Request.QueryString["s"];
	else
		m_salesID = Session["card_id"].ToString();
	
	DataRow dr = GetCardData(m_salesID);
	m_salesName = dr["name"].ToString();
	
	if(Request.Form["to_account"] != null && Request.Form["to_account"] != "")
		m_toAccount = Request.Form["to_account"];
	
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
	
//	if(Request.Form["cmd"] == "View Report")
//	{
//		if(!PrintToAccountList())
//			return;
		//DEBUG("m_expensetype = ", m_ExpenseType);
//	}
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
	if(m_nPeriod == 4) //select range
	{
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];

		m_sdFrom = Request.Form["pick_month1"]; 
		m_sdTo = Request.Form["pick_month2"];
	
	}
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_nPeriod = 0;

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
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_sdFrom)-1] + " - "+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_sdTo)-1] + " - "+ m_syTo +"</font>";
		break;
	default:
		break;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	
	DoExpenseList();

	PrintAdminFooter();
}

bool PrintToAccountType()
{
	int rows = 0;
//	string sc = "SELECT * FROM account WHERE class1 = 6 ";
//	if(m_toAccount != "" && m_toAccount != "all")
//		sc += " AND id = "+ m_toAccount;
//	sc += " ORDER BY name4";//class1, class2, class3, class4";
	
	string sc = "SELECT class1, name1 AS type ";
	sc += " FROM account ";
	sc += " WHERE class1 NOT IN(1)";
	sc += " GROUP BY class1, name1";
	sc += " ORDER BY type ";//class1, class2, class3, class4";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "toaccounttype");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1 || m_toAccount == "all")
	{
		if(m_toAccount == "all")
			m_ExpenseType = "ALL";
		if(rows == 1)
		{
			//m_ExpenseType = ds.Tables["toaccounttype"].Rows[0]["name4"].ToString();
			m_ExpenseType += ds.Tables["toaccounttype"].Rows[0]["name1"].ToString();
		}
		return true;
	}
	
	string getEclassNo = Request.QueryString["cid"];
	Response.Write("<select name=to_account_type onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate());
	Response.Write(" &cid='+this.options[this.selectedIndex].value)\">");
	Response.Write("<option value='all'> ALL </option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["toaccounttype"].Rows[i];
	//	string id = dr["id"].ToString();
		//string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
	//	string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
	    string eclassNo = dr["class1"].ToString();
		//double dAccBalance = double.Parse(dr["balance"].ToString());
		Response.Write("<option value=" + eclassNo);
		if(eclassNo == getEclassNo)
		{
			Response.Write(" selected");
//			m_sAccBalance = dAccBalance.ToString("c");
		}
		//Response.Write(">" + dr["name4"].ToString());
		Response.Write("> " +dr["type"].ToString());
		Response.Write("</option>");
		
//		Response.Write(" " + dAccBalance.ToString("c"));		
	}
	Response.Write("</select>");
	
	return true;
}
	
bool PrintToAccountList()
{
	int rows = 0;
//	string sc = "SELECT * FROM account WHERE class1 = 6 ";
//	if(m_toAccount != "" && m_toAccount != "all")
//		sc += " AND id = "+ m_toAccount;
//	sc += " ORDER BY name4";//class1, class2, class3, class4";
	
	string sc = "SELECT *, name4+' ' +name1 AS type ";
	sc += " FROM account ";
	sc += " WHERE class1 = 6 OR class1 = 2 ";
	if(m_toAccount != "" && m_toAccount != "all")
		sc += " AND id = "+ m_toAccount;
	sc += " ORDER BY type ";//class1, class2, class3, class4";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "toaccount");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1 || m_toAccount == "all")
	{
		if(m_toAccount == "all")
			m_ExpenseType = "ALL";
		if(rows == 1)
		{
			m_ExpenseType = ds.Tables["toaccount"].Rows[0]["name4"].ToString();
			m_ExpenseType += ds.Tables["toaccount"].Rows[0]["name1"].ToString();
		}
		return true;
	}
	
	
	Response.Write("<select name=to_account>");
	Response.Write("<option value='all'> ALL </option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["toaccount"].Rows[i];
		string id = dr["id"].ToString();
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		double dAccBalance = double.Parse(dr["balance"].ToString());
		Response.Write("<option value=" + id);
		if(id == m_toAccount)
		{
			Response.Write(" selected");
//			m_sAccBalance = dAccBalance.ToString("c");
		}
		Response.Write(">" + dr["name4"].ToString());
		//Response.Write(" " +dr["name1"].ToString());
		Response.Write("</option>");
		
//		Response.Write(" " + dAccBalance.ToString("c"));		
	}
	Response.Write("</select>");
	
	return true;
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
	Response.Write("<br><center><h4><b>Expenses Report</b></h4></center>");
	Response.Write("<form name=f action=explist.aspx");
	if(m_salesID != "")
		Response.Write("?s=" + m_salesID);
	Response.Write("&sid="+Request.QueryString["sid"]);
	Response.Write(" method=post>");

	Response.Write("<table  border=0 align=center cellspacing=1 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	
	Response.Write("<tr><td colspan=2><b>Type&nbsp;&nbsp; : </b>");
	if(!PrintToAccountType())
	    return;
	if(!PrintToAccountList())
		return;
	Response.Write("</td></tr>");	
	Response.Write("<tr><td colspan=2><b>Branch:&nbsp;</b>");
	Response.Write(GetBranchName());
	Response.Write("</td></tr>");
	//Response.Write("</table>");

	//Response.Write("<table width=60% align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	//Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
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
	//string s_year = DateTime.Now.ToString("yyyy");
	//if(sValue == "1")
	{
		Response.Write("<tr><td colspan=1><input type=radio name=period value=4 onclick=''>Compare Monthly Report");//</tr>");
		Response.Write(" &nbsp;<b>Selct From:</b> <select name='pick_month1'>");
		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			Response.Write("<option value="+m+"");
			if(int.Parse(s_month) == m)
				Response.Write(" selected ");
			//Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
			Response.Write(">"+txtMonth+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='pick_year1'>");
		for(int y=2000; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");

		Response.Write(" <b>To:</b> <select name='pick_month2'>");
		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			
			Response.Write("<option value="+m+"");
			if(int.Parse(s_month) == m)
				Response.Write(" selected ");
			//Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
			Response.Write(">"+txtMonth+"</option>");
			
		}
		Response.Write("</select>");
		Response.Write("<select name='pick_year2'>");
		for(int y=2000; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");

		Response.Write("</td></tr>");
	}	
    
	Response.Write("<tr><td align=center><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	//PrintAdminFooter();
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
		dc.Title = "General Expense";
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
bool DoExpenseList()
{
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, e.payment_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, e.payment_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, e.payment_date, GETDATE()) >= 1 AND DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND e.payment_date >= '" + m_sdFrom + "' AND e.payment_date <= '" + m_sdTo + "' ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(e.payment_date) >= '" + m_sdFrom + "' ";
		m_dateSql += " AND YEAR(e.payment_date) = '"+ m_syFrom +"' ";
		m_dateSql +=" AND MONTH(e.payment_date) <= '" + m_sdTo + "' ";
		m_dateSql += " AND YEAR(e.payment_date) = '"+ m_syTo +"' ";
		break;
	default:
		break;
	}

	ds.Clear();
	string eBranchId = Request.Form["branch"];
	string sc = " SET DATEFORMAT dmy ";
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_toAccount = Request.QueryString["id"];
	if(m_bCompair)
	{
		int nDifferent = 0;
		if(int.Parse(m_syFrom) < int.Parse(m_syTo))
			nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
		
		m_sdTo = (int.Parse(m_sdTo) + nDifferent).ToString();
		sc += " SELECT sum(e.total) ";
		int nPlus =0;
		for(int ii=int.Parse(m_sdFrom); ii<=int.Parse(m_sdTo); ii++)
		{
			int nMonth = ii;
			int nYear = int.Parse(m_syFrom);
			int nn = 0;
			if(nMonth > 12)
			{
				string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
				nn = int.Parse(snn[0].ToString());
			
				nPlus++;
				if(nPlus == 13)
					nPlus = 1;
				nMonth = nPlus;
				nYear = (int.Parse(m_syFrom) + nn);
			}

			sc += ",(SELECT sum(e1.total) FROM expense e1 ";
			sc += " JOIN account a ON a.id = e1.to_account ";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(DateTime.Now.ToString("yyyy"))) +"'";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(m_syFrom)) +"'";
			sc += " WHERE MONTH(e1.payment_date) = '" + nMonth + "' ";
			sc += " AND YEAR(e1.payment_date) = "+ nYear +"";
			if(m_toAccount != "" && m_toAccount != "all")
				sc += " AND a.id = "+ m_toAccount +"";
			sc += " ) AS 'total"+ ii +"' ";
			sc += ",(SELECT sum(e1.tax) FROM expense e1 ";
			sc += " JOIN account a ON a.id = e1.to_account ";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(DateTime.Now.ToString("yyyy"))) +"'";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(m_syFrom)) +"'";
			sc += " WHERE MONTH(e1.payment_date) = '" + nMonth + "' ";
			sc += " AND YEAR(e1.payment_date) = "+ nYear +"";
			if(m_toAccount != "" && m_toAccount != "all")
				sc += " AND a.id = "+ m_toAccount +"";
			sc += " ) AS 'tax"+ ii +"' ";
		}
		sc += " FROM expense e ";
		sc += " WHERE 1=1 ";
		if(eBranchId !="0")
		sc += " AND branch="+eBranchId ;
		//sc += m_dateSql;
		
	}
	else
	{
		sc += " SELECT e.*, c.name, c.trading_name, c.company, c1.name AS accountant ";
		sc += ", a.name4 + a.name1 AS expense_type, a.class1 ";
		if(m_bAuto)
			sc += ", enum.name AS frequency, ae.next_payment_date ";
		sc += " FROM expense e LEFT OUTER JOIN card c ON c.id = e.card_id ";
		sc += " LEFT OUTER JOIN card c1 ON c1.id = e.recorded_by ";
		sc += " JOIN account a ON a.id = e.to_account ";
		if(m_bAuto)
		{
			sc += " JOIN auto_expense ae ON ae.id = e.id ";
			sc += " LEFT OUTER JOIN enum ON enum.id = ae.frequency AND enum.class = 'autopayment_frequency' ";
		}
		else
		{
			sc += " WHERE 1=1 ";
			sc += m_dateSql;
		}
		if(m_toAccount != "" && m_toAccount != "all")
			sc += " AND a.id = "+ m_toAccount +"";
		if(eBranchId !="0")
		sc += " AND branch="+eBranchId ;
//		sc += " AND e.total > 0 ";
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
	Response.Write("<br><center><h3>" + m_tableTitle + "");
	if(m_ExpenseType != "")
	{
		Response.Write(" for");
		Response.Write("<font color=Green> ");
		Response.Write(m_ExpenseType);
//		Response.Write(" AutoPayment");
		Response.Write(" </font>");
	}
	Response.Write("</h3>");
	if(!m_bAuto)
		Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
	
	BindList();
	if(m_bAuto)
	{
		Response.Write("<br><input type=button value='New AutoPayment' onclick=window.location=('expense.aspx?t=new&auto=1') class=b>");
		Response.Write("<br><br><h5>For new auto payments, record expense first, then set 'Auto Repeat'</h5>");
		return true;
	}

	sc = " SET DATEFORMAT dmy ";
	
	if(m_bCompair)
	{
		sc += " SELECT sum(e.total) ";
		int nPlus = 0;
		for(int ii=int.Parse(m_sdFrom); ii<=int.Parse(m_sdTo); ii++)
		{
			int nMonth = ii;
			int nYear = int.Parse(m_syFrom);
			int nn = 0;
			if(nMonth > 12)
			{
				string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
				nn = int.Parse(snn[0].ToString());
			
				nPlus++;
				if(nPlus == 13)
					nPlus = 1;
				nMonth = nPlus;
				nYear = (int.Parse(m_syFrom) + nn);
			}
			sc += ",(SELECT sum(e1.total) FROM expense e1 ";
			sc += " JOIN account a ON a.id = e1.to_account ";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(DateTime.Now.ToString("yyyy"))) +"'";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(m_syFrom)) +"'";
			sc += " WHERE MONTH(e1.payment_date) = '" + nMonth + "' ";
			sc += " AND YEAR(e1.payment_date) = "+ nYear +"";
			if(m_toAccount != "" && m_toAccount != "all")
				sc += " AND a.id = "+ m_toAccount +"";
			sc += " ) AS 'total"+ ii +"' ";
			sc += ",(SELECT sum(e1.tax) FROM expense e1 ";
			sc += " JOIN account a ON a.id = e1.to_account ";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(DateTime.Now.ToString("yyyy"))) +"'";
			//sc += " WHERE MONTH(e1.payment_date) + YEAR(e1.payment_date) = '"+ (ii + int.Parse(m_syFrom)) +"'";
			sc += " WHERE MONTH(e1.payment_date) = '" + nMonth + "' ";
			sc += " AND YEAR(e1.payment_date) = "+ nYear +"";
			if(m_toAccount != "" && m_toAccount != "all")
				sc += " AND a.id = "+ m_toAccount +"";
			sc += " ) AS 'tax"+ ii +"' ";
	
		}
		sc += " FROM expense e ";
		sc += " WHERE 1=1 ";
	    if(eBranchId !="0")
		sc += " AND branch="+eBranchId ;
		//sc += m_dateSql;
		
	}
	else
	{
		sc += " SELECT SUM(e.total) AS total, a.name4 + ' ' + a.name1 AS type ";
		sc += " FROM expense e JOIN account a ON a.id = e.to_account ";
		sc += " WHERE 1=1 ";
		sc += m_dateSql;
		if(m_toAccount != "" && m_toAccount != "all")
			sc += " AND a.id = "+ m_toAccount +"";
		
		if(eBranchId !="0")
		sc += " AND branch="+eBranchId ;
		sc += " GROUP BY a.name4, a.name1 ";
		sc += " ORDER BY total desc ";
	}
	
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
	StringBuilder sb2 = new StringBuilder();

	string x = "";
	string y = "";
	string legend = "";

	for(int i=0; i<ds.Tables["total"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["total"].Rows[i];
		if(!m_bCompair)
		{
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
		else
		{	
			int nPlus =0;
			for(int ii=int.Parse(m_sdFrom); ii<=int.Parse(m_sdTo); ii++)
			{
				int nMonth = ii;
				int nn = 0;
							
				if(nMonth > 12)
				{	
					string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
					nn = int.Parse(snn[0].ToString());
					nPlus++;
					if(nPlus == 13)
						nPlus = 1;
					nMonth = nPlus;
				}

				double dTotal = MyDoubleParse(dr["total"+ ii +""].ToString());
				double dTax = MyDoubleParse(dr["tax"+ ii +""].ToString());
				if(dTotal > m_nMaxY)
					m_nMaxY = dTotal;

				if(dTotal < 0 && dTotal < m_nMinY)
					m_nMinY = dTotal;
			
				//xml chart data
				x = (i*10).ToString();
				m_nMaxX = i*10;
				y = dTotal.ToString();
				
				if(nPlus > 0)
					legend = m_EachMonth[nPlus-1].Replace("&", " ");
				else
					legend = m_EachMonth[ii-1].Replace("&", " ");
				
					
				sb1.Append("<chartdata>\r\n");
				sb1.Append("<x");
				if(m_bHasLegends)
					sb1.Append(" legend='" + legend + "'");
				sb1.Append(">" + x + "</x>\r\n");
				sb1.Append("<y>" + y + "</y>\r\n");
				sb1.Append("</chartdata>\r\n");

				x = (MyIntParse(x) + 5).ToString();
				y = dTax.ToString();
				sb2.Append("<chartdata>\r\n");
				sb2.Append("<x");
				if(m_bHasLegends)
					sb2.Append(" legend='" + legend + "'");
				sb2.Append(">" + x + "</x>\r\n");
				sb2.Append("<y>" + y + "</y>\r\n");
				sb2.Append("</chartdata>\r\n");
			}
		}
		
	}

	if(m_bCompair)
	{
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb2.ToString());
		m_sb.Append("</chartdataisland>\r\n");
		m_nIsland = 2;
	
		m_IslandTitle[0] = "--TAX";
		m_IslandTitle[1] = "--Expense";
	}
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	if(!m_bCompair)
	{
		m_nIsland = 1;
		m_IslandTitle[0] = "--Expense";
	}
	
	
	if(m_bShowPic)
	{
	//write xml data file for chart image
	WriteXMLFile();
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=explilst.aspx method=post>");
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
void BindList()
{
	int i = 0;
	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;

	Response.Write("<table width=98%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(!m_bCompair)
	{
		Response.Write("<th>ID</th>");
		if(!m_bAuto)
			Response.Write("<th nowrap>Payment Date</th>");
		if(m_bAuto)
		{
			Response.Write("<th>Frequency</th>");
			Response.Write("<th>NextPayment_Date</th>");
		}	
		Response.Write("<th nowrap>Expense Type</th>");
		Response.Write("<th>Payee</th>");
		Response.Write("<th nowrap>Recorded By</th>");
		Response.Write("<th>Tax</th>");
		Response.Write("<th>Amount</th>");
	}
	else
	{
//		DEBUG("m_sdTo=", m_sdTo);
		int nPlus =0;
		for(int ii=int.Parse(m_sdFrom); ii<=int.Parse(m_sdTo); ii++)
		{
				int nMonth = ii;
			int nYear = int.Parse(m_syFrom);
			int nn = 0;
			if(nMonth > 12)
			{
				string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
				nn = int.Parse(snn[0].ToString());
				
				nPlus++;
				if(nPlus == 13)
					nPlus = 1;
				nMonth = nPlus;
				nYear = int.Parse(m_syFrom) + nn;
		
			}
		
			string s_month = m_EachMonth[nMonth-1] +"-"+ nYear ;
			Response.Write("<th>"+ s_month +"</th>");
			
		}

	}
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		m_bShowPic = false;
		return;
	}

	double dSubTax = 0;
	double dSubTotal = 0;

	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	for(; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		if(!m_bCompair)
		{
			string id = dr["id"].ToString();
			string classId = dr["class1"].ToString();
			string cardId = dr["card_id"].ToString();
			string card_id = dr["card_id"].ToString();
			string payee = dr["trading_name"].ToString();
			if(payee == "")
				payee = dr["company"].ToString();
			if(payee == "")
				payee = dr["name"].ToString();
			double dTax = MyDoubleParse(dr["tax"].ToString());
			double dTotal = MyDoubleParse(dr["total"].ToString());
			string payment_date = DateTime.Parse(dr["payment_date"].ToString()).ToString("dd-MM-yyyy");
			string recorded_by = dr["accountant"].ToString();
			string expense_type = dr["expense_type"].ToString();

			dSubTax += dTax;
			dSubTotal += dTotal;

			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");
			
			if(m_bAuto)
				Response.Write("<td>" + id + " <a href=editae.aspx?id=" + id + "&sid="+classId+"&pid="+cardId +" class=o>Edit</a></td>");
			else
				Response.Write("<td>" + id + " <a href=expense.aspx?id=" + id + "&sid="+classId+"&pid="+cardId +" class=o>Edit</a></td>");
			if(!m_bAuto)
				Response.Write("<td>" + payment_date + "</td>");
			if(m_bAuto)
			{
				Response.Write("<td align=center>" + Capital(dr["frequency"].ToString()) +"</td>");
				Response.Write("<td align=center>" + DateTime.Parse(dr["next_payment_date"].ToString()).ToString("dd-MM-yy") +"</td>");
			}
			//Response.Write("<td>" + payment_date + "</td>");
			Response.Write("<td>" + expense_type + "</td>");
			Response.Write("<td>" + payee + "</td>");
			Response.Write("<td>" + recorded_by + "</a></td>");
			Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
			Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		}
		else
		{
			double dTotalEachMonth = 0;
			//double dRefundEachMonth = 0;
			double dEachMonthTax = 0;
	//		DEBUG("msdform =", m_sdFrom);
	//		DEBUG("msdto =", m_sdTo);
			for(int ii=int.Parse(m_sdFrom); ii<=int.Parse(m_sdTo); ii++)
			{
				string tax = dr["tax"+ ii +""].ToString();
				string total = dr["tax"+ ii +""].ToString();
//			DEBUG("total "+ ii +" = ", total);
				if(tax != null && tax != "")
					dEachMonthTax = double.Parse(dr["tax"+ ii +""].ToString());
				else
					dEachMonthTax = 0;
				if(total != null && total != "")
					dTotalEachMonth = double.Parse(dr["total"+ ii +""].ToString());
				else
					dTotalEachMonth = 0;
				dSubTax += dEachMonthTax;
				dSubTotal += dTotalEachMonth;
				Response.Write("<td align=center>"+ dTotalEachMonth.ToString("c") +"</th>");
				
			}
		}
		Response.Write("</tr>");

	}

	//total
	if(!m_bAuto)
	{
		Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
		Response.Write(">");
		Response.Write("<td colspan=");
		if(m_bCompair)
		{
			string srows = (int.Parse(m_sdTo) - int.Parse(m_sdFrom)).ToString();
			Response.Write(" "+ (int.Parse(srows)+1) +"");
			//Response.Write(" "+ srows +"");
			Response.Write(" align=right style=\"font-size:16\"><table><tr>");
			Response.Write("<td align=right style=\"font-size:16\">TAX:" + dSubTax.ToString("c") + "&nbsp;&nbsp;</td>");
			Response.Write("<td align=right style=\"font-size:16\">Total:" + dSubTotal.ToString("c") + "&nbsp;&nbsp;</td>");
			Response.Write("<td align=right style=\"font-size:16\">Sub Total:" + (dSubTotal+dSubTax).ToString("c") + "</td>");
			Response.Write("</tr></table></td>");
		}
		else
		{
			Response.Write(" 5 ");
			Response.Write(" align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
			Response.Write("<td align=right style=\"font-size:16\">" + dSubTax.ToString("c") + "</td>");
			Response.Write("<td align=right style=\"font-size:16\">" + dSubTotal.ToString("c") + "</td>");
		}
		Response.Write("</tr>");
	}
	
	Response.Write("</table>");

}
string GetBranchName()
{
    if(ds.Tables["branch_name"] != null)
	ds.Tables["branch_name"].Clear();
	int rows = 0;
	string sc = " SELECT id, name FROM branch";
	sc += " WHERE activated =1";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	//string getEclassNo = Request.QueryString["cid"];
	Response.Write("<select name=branch >");
	Response.Write("<option value='0'> ALL </option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["branch"].Rows[i];
	
		string BranchId = dr["id"].ToString();
	    string BranchName = dr["name"].ToString();
		Response.Write("<option value=" + BranchId);
		Response.Write("> " +dr["name"].ToString());
		Response.Write("</option>");	
	}
	Response.Write("</select>");
	return "";

}
	


</script>

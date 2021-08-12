<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

<script runat=server>

string m_type = "0";
string m_tableTitle = "Custom Tax List";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
int m_start_year = 2000;
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
	if(int.Parse(DateTime.Now.ToString("yyyy")) - m_start_year > 7)
		m_start_year = int.Parse(DateTime.Now.ToString("yyyy")) - 7;

	
	if(Request.QueryString["del"] == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		ShowCustomGSTDone();
		PrintAdminFooter();
		return;
	}

	if(Request.QueryString["did"] != null && Request.QueryString["did"] != "")
	{
		if(DoDeleteTrans())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?del=done\">");
			return;
		}
	}
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

bool DoDeleteTrans()
{
	string del_id = Request.QueryString["did"];
	if(del_id != "")
	{
				//update account balance
		string sc = " UPDATE account SET balance = balance + (SELECT total_gst FROM custom_tax WHERE id = "+ del_id +") WHERE id = (SELECT from_acc FROM custom_tax WHERE id = "+ del_id +") ";
		sc += " UPDATE account SET balance = balance - (SELECT total_gst FROM custom_tax WHERE id = "+ del_id +") WHERE id = (SELECT location_acc FROM custom_tax WHERE id = "+ del_id +") ";
		//update delete record
		sc += " INSERT INTO custom_tax_log (id, update_by, update_date, old_total, new_total, note) ";
		sc += " SELECT id, "+ Session["card_id"] +", getdate(), total_gst, total_gst, 'delete from custom gst' ";
		sc += " FROM custom_tax WHERE id = "+ del_id +" ";

		sc += " DELETE FrOM custom_tax WHERE id = "+ del_id +" ";
//	DEBUG("sc = ", sc);
		
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

	}
	return true;
}

void ShowCustomGSTDone()
{
	Response.Write("<center><br><h4>Record Done...");
	Response.Write("<br><br><a title='go to custom gst report' href='custax_rp.aspx?r="+ DateTime.Now.ToOADate() +"' class=o>Go to Custom GST List</a>");
	Response.Write("<br><br><a title='new custom gst transaction' href='custax.aspx?r="+ DateTime.Now.ToOADate() +"' class=o>Pay New Custom GST</a>");
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
	sc += " WHERE class1 = 2 ";
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
		Response.Write(" " +dr["name1"].ToString());
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
	Response.Write("<br><center><h4><b>Custom GST Report</b></h4></center>");
	Response.Write("<form name=f action="+Request.ServerVariables["URL"] +"");
//	if(m_salesID != "")
//		Response.Write("?s=" + m_salesID);
	Response.Write(" method=post>");

	Response.Write("<table  border=0 align=center cellspacing=1 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
/*	Response.Write("<tr><td colspan=2><b>Select Expense Type : </b>");
	if(!PrintToAccountList())
		return;
	Response.Write("</td></tr>");	
*/
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
	for(int y=m_start_year; y<int.Parse(s_year)+1; y++)
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
	for(int y=m_start_year; y<int.Parse(s_year)+1; y++)
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

	//string s_year = DateTime.Now.ToString("yyyy");
	bool bNoCompair = true;
	if(!bNoCompair)
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
		for(int y=m_start_year; y<int.Parse(s_year)+1; y++)
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
		for(int y=m_start_year; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");

		Response.Write("</td></tr>");
	}	

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
		m_dateSql = " AND DATEDIFF(month, t.statement_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, t.statement_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, t.statement_date, GETDATE()) >= 1 AND DATEDIFF(month, t.statement_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND t.statement_date >= '" + m_sdFrom + "' AND t.statement_date <= '" + m_sdTo + "' ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(t.statement_date) >= '" + m_sdFrom + "' ";
		m_dateSql += " AND YEAR(t.statement_date) = '"+ m_syFrom +"' ";
		m_dateSql +=" AND MONTH(t.statement_date) <= '" + m_sdTo + "' ";
		m_dateSql += " AND YEAR(t.statement_date) = '"+ m_syTo +"' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
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
		//sc += m_dateSql;
		
	}
	else
	{
		sc += " SELECT t.*, c1.name, c1.trading_name, c1.company, c.name AS accountant ";
		sc += ", a.name4 + ' ' + a.name1 AS location_account ";
		sc += ", a1.name4 + ' ' + a1.name1 AS from_account ";
		sc += ", e.name AS payment_method ";
		sc += " FROM custom_tax t JOIN card c ON c.id = t.recorded_by ";
		sc += " JOIN card c1 ON c1.id = t.payee ";
		sc += " JOIN account a ON a.id = t.location_acc ";
		sc += " JOIN account a1 ON a1.id = t.from_acc ";
		sc += " JOIN enum e ON e.class='payment_method' AND e.id = t.payment_type ";
		sc += " WHERE 1=1 ";
		sc += m_dateSql;
		sc += " ORDER BY t.statement_date ";
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
	return true;

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
	
	return false;
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

	Response.Write("<table width=98%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666966;font-weight:bold;\">");
	if(!m_bCompair)
	{
		Response.Write("<th>&nbsp;</th>");
		Response.Write("<th>TAX_ID#</th>");
		Response.Write("<th nowrap>STATEMENT Date</th>");
		Response.Write("<th nowrap>Date Payment Due</th>");
		Response.Write("<th nowrap>FROM ACC</th>");
		Response.Write("<th>PAYEE</th>");
		Response.Write("<th nowrap>RECORDED BY</th>");
		Response.Write("<th nowrap>PAYMENT TYPE</th>");
		Response.Write("<th nowrap>PAYMENT REF</th>");
		Response.Write("<th nowrap>NOTE</th>");
		Response.Write("<th>AMOUNT</th>");
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
			string client_code = dr["clientcode"].ToString();
			string card_id = dr["recorded_by"].ToString();
			string payee = dr["trading_name"].ToString();
			if(payee == "")
				payee = dr["company"].ToString();
			if(payee == "")
				payee = dr["name"].ToString();
			
			double dTotal = MyDoubleParse(dr["total_gst"].ToString());
			string statement_date = DateTime.Parse(dr["statement_date"].ToString()).ToString("dd-MM-yyyy");
			string date_due = DateTime.Parse(dr["date_payment_due"].ToString()).ToString("dd-MM-yyyy");
			string recorded_by = dr["accountant"].ToString();
			string from_account = dr["from_account"].ToString();
			string location_acc = dr["location_account"].ToString();
			string payment_ref = dr["payment_ref"].ToString();
			string payment_type = dr["payment_method"].ToString();
			string note = dr["note"].ToString();
			dSubTotal += dTotal;
				
			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");
			Response.Write("<td><a href=custax_rp.aspx?did=" + id + " onclick=\"");
			Response.Write("if(!confirm('Do you want to Delete This Record??\\r\\n')){return false;}\"");
			Response.Write("class=o><font color=red>DELETE</a></td>");
			Response.Write("<td>" + client_code + "</td>");
			Response.Write("<td>" + statement_date + "</td>");
			Response.Write("<td>" + date_due + "</td>");
			Response.Write("<td>" + location_acc + "</td>");
			Response.Write("<td>" + payee + "</td>");
			Response.Write("<td>" + recorded_by + "</a></td>");
			Response.Write("<td>" + payment_type + "</a></td>");
			Response.Write("<td>" + payment_ref + "</a></td>");
			Response.Write("<td>" + note + "</a></td>");
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
		Response.Write("<tr style=\"color:black;background-color:#EEEEE;\" ");
		Response.Write(">");
		Response.Write("<td colspan=");
		if(m_bCompair)
		{
			string srows = (int.Parse(m_sdTo) - int.Parse(m_sdFrom)).ToString();
			Response.Write(" "+ (int.Parse(srows)+1) +"");
			
			Response.Write(" align=right style=\"font-size:16\"><table><tr>");
			Response.Write("<td align=right style=\"font-size:16\">Sub Total:" + (dSubTotal+dSubTax).ToString("c") + "</td>");
			Response.Write("</tr></table></td>");
		}
		else
		{
			Response.Write(" 10 ");
			Response.Write(" align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
			Response.Write("<td align=right style=\"font-size:16\">" + dSubTotal.ToString("c") + "</td>");
		}
		Response.Write("</tr>");
	}
	
	Response.Write("</table>");

}

</script>

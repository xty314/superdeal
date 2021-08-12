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
string m_uri = ""; 
string m_type = "0";
string m_tableTitle = "Profit & Loss";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sales_person = "";
string m_sales_id = "";

string m_sType_Value = "0";

string m_sdFrom = "";
string m_sdTo = "";
string m_syFrom = DateTime.Now.ToString("yyyy");
string m_syTo = DateTime.Now.ToString("yyyy");
int m_nPeriod = 0;

bool m_bGBSetShowPicOnReport = false;
bool m_bShowPic = true;
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
string[] m_EachMonth = new string[13];
int m_nIsland = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] sct = new string[16];
int cts = 0;
int m_ct = 1;
bool m_bCompair = false;

string m_brand = "";
string m_cat = "";
string m_scat = "";
string m_sscat = "";
string m_export = "";
string m_path = "";
bool m_bPrint = false;
string m_sFileName = "ItemBased";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	m_uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate();
	m_bGBSetShowPicOnReport = MyBooleanParse(GetSiteSettings("set_display_chart_on_report", "1", true));

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

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
	
	if(Request.QueryString["sltype"] != "" && Request.QueryString["sltype"] != null)
		m_sType_Value = Request.QueryString["sltype"];
	string cat = "", s_cat = "", ss_cat = "", brand = "";
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"].ToString();
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
		m_scat = Request.QueryString["s_cat"].ToString();
	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
		m_sscat = Request.QueryString["ss_cat"].ToString();
	if(Request.QueryString["brand"] != null && Request.QueryString["brand"] != "")
		m_brand = Request.QueryString["brand"].ToString();
//	if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
//		 m_branchID = Request.QueryString["branch"].ToString();

	if(Request.Form["cmd"] != null)
	{
	m_brand = Request.Form["brand"];
	m_cat = Request.Form["cat"];
	m_scat = Request.Form["scat"];
	m_sscat = Request.Form["sscat"];
	m_branchID = Request.Form["branch"];
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

	if(Request.Form["cmd"] == "View Report")
	{
		m_sales_id = Request.Form["employee"].ToString();
		if(!doShowEmployee())
			return;
		if(Request.Form["employee"] == "all")
			m_sales_person = "all";
	}
	if(Request.QueryString["type"] != "" && Request.QueryString["type"] != null)
	{
		m_type = Request.QueryString["type"];
		if(Request.QueryString["np"] != null)
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	}
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
//DEBUG("mtye =", m_type);
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
	if(Request.Form["type"] != null)
		m_type = Request.Form["type"];
	//if(Request.Form["day_from"] != null)
	if(Request.Form["Datepicker1_day"] != null)
	{
		//string day = Request.Form["day_from"];
		//string monthYear = Request.Form["month_from"];
		//ValidateMonthDay(monthYear, ref day);
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;
	
		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	if(m_nPeriod == 4) //select range
	{
		m_sdFrom = Request.Form["pick_month1"];
		m_sdTo = Request.Form["pick_month2"];
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];
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
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_sdFrom)-1] + "-"+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_sdTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	switch(MyIntParse(m_type))
	{
	case 0:
		DoPRItem();
		break;
	case 1:
		DoPRSales();
		break;
	case 2:
		DoPRMonthly();
		break;
	case 3:
		DoProfitStatement();
		break;
	case 4:
		DoItemCatagorySummary();
		break;
	case 5:
		DoReceivedPaySummary();
		break;

	default:
		break;
	}

	PrintAdminFooter();
}

/*void ValidateMonthDay(string monthYear, ref string day)
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
*/
bool doShowEmployee()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id ";
	sc += " FROM card ";
	sc += " WHERE type = 4";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND our_branch = " + m_branchID;
	}
	if(m_sales_id != null && m_sales_id != "" && m_sales_id != "all")
		sc += " AND id = "+ m_sales_id +"";
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
		m_sales_person = ds.Tables["employee"].Rows[0]["name"].ToString();
	}
	
	return true;
}


bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT brand FROM product p  ORDER BY brand ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "brand");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
		return true;
	Response.Write("Brand Selection: <select name=brand ");
	Response.Write(" onchange=\"window.location=('" + m_uri + "&sltype="+ m_sType_Value +"");
	if(m_cat != "" && m_cat != null)
		Response.Write("&cat="+ HttpUtility.UrlEncode(m_cat) +"");
	if(m_scat != "" && m_scat != null)
		Response.Write("&s_cat="+ HttpUtility.UrlEncode(m_scat) +"");
	if(m_sscat != "" && m_sscat != null)
		Response.Write("&ss_cat="+ HttpUtility.UrlEncode(m_sscat) +"");
	if(m_branchID != "")
		Response.Write("&branch="+ m_branchID +"");
	Response.Write("&brand='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["brand"].Rows[i];
		string s = dr["brand"].ToString();
		if(m_brand.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");
	}

	Response.Write("</select>");

	sc = "SELECT DISTINCT cat FROM product p  ORDER BY cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "cat");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
		return true;
	Response.Write(" Catalog Select: <select name=cat ");
	Response.Write(" onchange=\"window.location=('" + m_uri + "&sltype="+ m_sType_Value +"");
	if(m_brand != "")
		Response.Write("&brand="+ m_brand +"");
	if(m_branchID != "")
		Response.Write("&branch="+ m_branchID +"");
	Response.Write("&r=" + DateTime.Now.ToOADate() + "&cat='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
//	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
//		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
	
		if(m_cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");
	}

	Response.Write("</select>");
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
	{
		//m_cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT s_cat FROM product  WHERE cat = '"+ m_cat +"' ";
		sc += " ORDER BY s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(ds, "s_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=scat ");
		Response.Write(" onchange=\"window.location=('" + m_uri + "&sltype="+ m_sType_Value +"");
		if(m_brand != "")
			Response.Write("&brand="+ m_brand +"");
		if(m_branchID != "")
		Response.Write("&branch="+ m_branchID +"");
		Response.Write("&cat="+ HttpUtility.UrlEncode(m_cat) +"&r=" + DateTime.Now.ToOADate() + "&s_cat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
//		if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
//			s_cat = Request.QueryString["s_cat"].ToString();
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
		
			//DEBUG(" scat = ", s_cat);
			if(m_scat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
		}

		Response.Write("</select>");
	}

	if(Request.QueryString["m_scat"] != null && Request.QueryString["m_scat"] != "")
	{
		//m_s_cat = Request.QueryString["s_cat"].ToString();
//		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM product p WHERE cat = '"+ m_cat +"' ";
		sc += " AND s_cat = '"+ m_scat +"' ";
		sc += " ORDER BY ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(ds, "ss_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		if(rows <= 0)
			return true;
		Response.Write("<select name=sscat ");
		Response.Write(" onchange=\"window.location=('" + m_uri + "&sltype="+ m_sType_Value +"");
		if(m_brand != "")
			Response.Write("&brand="+ m_brand +"");
		if(m_branchID != "")
		Response.Write("&branch="+ m_branchID +"");
		Response.Write("&cat="+ HttpUtility.UrlEncode(m_cat) +"&r=" + DateTime.Now.ToOADate() + "&s_cat="+ HttpUtility.UrlEncode(m_scat) +"&m_sscat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
//		if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
//			ss_cat = Request.QueryString["ss_cat"].ToString();
		
		
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(m_sscat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}
	return true;
}

void PrintMainPage()
{

	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<form name=f action=rprofit.aspx method=post>");
	Response.Write("<br><center><h3>Select Report</h3>");
	string uri = m_uri.ToString();

	if(m_branchID != "")
		uri += "&branch="+ m_branchID;	
	if(Session["branch_support"] != null)
	{
		string b_uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate();
		if(m_sType_Value != "")
			b_uri += "&sltype="+ m_sType_Value;
		b_uri += "&branch=";
		Response.Write("<b>Branch : </b>");
//		PrintBranchNameOptions(m_branchID, "rprofit.aspx?branch=");
		PrintBranchNameOptions(m_branchID, b_uri, false);
	}
	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Report Type</b></td></tr>");
	
	
/*	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ uri +"&sltype=0');\" value=0 ");
	if(m_sType_Value == "0")
		Response.Write(" checked");
*/	
	Response.Write("<tr><td colspan=2><input type=radio name=type checked value=0 onclick=\"OnTypeChange(this.value);\"");
	Response.Write(" >Item Based ");
	Response.Write(" &nbsp;&nbsp;&nbsp;<b>Select Item Code : </b><select name=code ");
	Response.Write(" onclick=\"window.location=('slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"')\" ");
	Response.Write(" >");
	Response.Write("<option value='all'>All");
	if(Session["slt_code"] != null)
		Response.Write("<option value='"+ Session["slt_code"] +"' selected> "+Session["slt_code"] +" </option>");
	Response.Write("</select>\r\n");
	Response.Write("</td></tr>");

	//display report in brand, catagory
	Response.Write("<tr><td colspan=2><input type=radio name=type value=4 onclick=\"OnTypeChange(this.value);\"");
/*	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ uri +"&sltype=4');\" value=4 ");
	if(m_sType_Value == "4")
		Response.Write(" checked ");
*/
	//Response.Write(">Sorted by: ");
	Response.Write(">");
	DoItemOption();
	Response.Write("</td>");
	Response.Write("</tr>");

Response.Write("<tr><td colspan=2><input type=radio name=type value=1 onclick=\"OnTypeChange(this.value);\" ");
/*	Response.Write("<tr><td colspan=2><input type=radio name=type value=1 onclick=\"window.location=('"+ uri +"&sltype=1');\" ");
	if(m_sType_Value == "1")
		Response.Write(" checked ");
*/
	Response.Write(" >Sales Person Based &nbsp;");
	Response.Write("<select name=employee> <option value='all'>all");
	if(!doShowEmployee())
		return;
	for(int ii=0; ii<ds.Tables["employee"].Rows.Count; ii++)
	{
		DataRow dr = ds.Tables["employee"].Rows[ii];
		string name = dr["name"].ToString();
		
		string id = dr["id"].ToString();
		Response.Write("<option value='"+id+"'>"+ name +"</option>");
	}
	Response.Write("</select>");
	Response.Write("</td></tr>");

//	Response.Write("<tr><td colspan=2><input type=radio name=type value=2 onclick=\"OnTypeChange(this.value);\" ");
/*	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ uri +"&sltype=2');\" value=2 ");
	if(m_sType_Value == "2")
		Response.Write(" checked ");
*/
//	Response.Write(">Gross Profit Chart <i>(monthly report)</i></td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=type value=3 onclick=\"OnTypeChange(this.value);\" ");
/*
	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ uri +"&sltype=3');\" value=3");
	if(m_sType_Value == "3")
		Response.Write(" checked");
*/
	Response.Write(">Profit Statement</td></tr>");
	//received Money and Pay Purchase summary 

/*	Response.Write("<tr><td colspan=2><input type=radio name=type value=5 onclick=\"OnTypeChange(this.value);\" ");
	Response.Write(">");
	Response.Write("<select name=pr_option>");
	Response.Write("<option value='all'>Both</option>");
	Response.Write("<option value='1'>Pay Purchase Summary</option>");
	Response.Write("<option value='0'>Receive Money Summary</option>");
	Response.Write("</select>");
	Response.Write(" | Receive Money/Pay Purchase Summary</td></tr>");
	
//	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ m_uri +"?sltype=6');\" value=5");
//	if(m_sType_Value == "6")
//		Response.Write(" checked");
//	Response.Write(">Pay Purchase Summary</td></tr>");
	//Response.Write("<tr><td>&nbsp;</td></tr>");
*/
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Last Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>Select Date Range</td></tr>");

	int i = 1;
	PrintJavaFunction(); //call visivility object
	datePicker(); //call date picker function from common.cs
	Response.Write("<tr><td>");
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td>");
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>Select : </b> From Date ");
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
	//Response.Write("</td>");
		//------ start second display date -----------
	Response.Write(" &nbsp; TO: ");
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
	Response.Write("</td>");
	Response.Write("</tr>");
	
	//if(m_sType_Value == "1" || m_sType_Value == "4")
	//if(m_sType_Value == "1")
		{
	Response.Write("<tr><td>");
	Response.Write("<table id=tblCompare");
	if(m_type != "1") //pickup
		Response.Write(" style='visibility:hidden' ");
	Response.Write(">");

		Response.Write("<tr><td><input type=radio name=period value=4 onclick=''>Compare Monthly Report");//</tr>");
		Response.Write(" &nbsp;<b>Select From:</b> <select name='pick_month1'>");
		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			Response.Write("<option value="+m+"");
			if(int.Parse(s_month) == m)
				Response.Write(" selected ");
			Response.Write(">"+txtMonth+"</option>");
			//Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
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
			Response.Write(">"+txtMonth+"</option>");
			//Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
			
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
		Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right>");
	Response.Write("<input type=submit name=cmd value='Export Report' " + Session["button_style"] + " onclick=\"return confirm('Export Report!!!');\">");
	Response.Write("<input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}
/*
public string XAxisLabel
public string YAxisLabel
public bool UseGradientColors
public bool HasGridLines
public bool HasSubGridLines
public bool HasLegends
public bool HasBorder
public bool HasTickers
public int Width
public int Height
public double TitlePaddingFromTop
public int AxisPaddingLeft
public int AxisPaddingRight
public int AxisPaddingBottom
public int AxisPaddingTop
public int MinXValue
public int MaxXValue
public int MinYValue
public int MaxYValue
public int XTickSpacing
public int YTickSpacing
public Font TitleFont
public Font XAxisLabelFont
public Font YAxisLabelFont
public Font XTickFont
public int TickStep
public Font YTickFont
public Font LegendFont
public Color BackgroundColor
public Color GridLineColor
public Color HatchColor
public Color TitleColor
public Color XAxisLabelColor
public Color YAxisLabelColor
*/

void PrintJavaFunction()
{
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function OnTypeChange(mtype)");
	Response.Write("{		");
	Response.Write("	var m = mtype;\r\n");
	Response.Write("	if(m == 1){document.all('tblCompare').style.visibility='visible';}\r\n");
	Response.Write("	else{document.all('tblCompare').style.visibility='hidden';}\r\n");
	Response.Write("}\r\n");
	Response.Write("</script");
	Response.Write(">");
}

void DrawChart()
{
	Stream chartFile = null;
	XmlDocument obXmlDoc = null;
	string uname = EncodeUserName();
	m_picFile = "./ri/" + uname + DateTime.Now.ToString("ddMMyyyyHHmmss") + "_datachart.jpg";
	
	doDeleteAllPicFiles(); // delete all pic files before creating a new one

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
bool DoPRItem()
{
	m_tableTitle = "Profit & Loss - Item Based";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			m_tableTitle += " <br><font size=2>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=2>branch: ALL</font>";
	}
//DEBUG("m_tableTitle +", m_tableTitle);
	
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
	if(Request.Form["cmd"] == "Export Report")
	{
		sc += " SELECT s.code ";	
		sc += " , s.supplier_code, s.name, sum(s.commit_price * s.quantity) AS commit_price  ";
		sc += " , c.average_cost AS last_cost, c.manual_cost_frd AS manaul_cost , (c.manual_cost_frd * c.manual_exchange_rate * rate) AS bottom_price ";
		for(int j=1; j<=int.Parse(GetSiteSettings("dealer_levels", "1")); j++)
			sc += " , (c.manual_cost_frd * c.manual_exchange_rate * rate * level_rate"+ j +") AS dealer_price"+ j +"  ";
		int nCount = 6;
		int nMonthDiffer = 6;
		while(nCount > 0)
		{
				sc += ", ISNULL((SELECT SUM(ss.commit_price * ss.quantity) "; 											
				sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number JOIN orders oo ON oo.invoice_number = ii.invoice_number ";        
				sc += " WHERE ss.code = s.code ";						
				sc += " AND ss.supplier_code = s.supplier_code ";
				sc += " AND DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (nMonthDiffer - nCount).ToString() +" ),'') AS 'TotalSalesItemPrice"+ (nMonthDiffer - nCount).ToString() +"' ";

				sc += ", ISNULL((SELECT SUM(ss.quantity)  "; 									
				sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number JOIN orders oo ON oo.invoice_number = ii.invoice_number ";        
				sc += " WHERE ss.code = s.code ";						
				sc += " AND ss.supplier_code = s.supplier_code ";
				sc += " AND DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (nMonthDiffer - nCount).ToString() +" ),'') AS 'TotalSalesQTY"+ (nMonthDiffer - nCount).ToString() +"' ";

				nCount--;
		}
			  
		sc += ", sum(c.average_cost * s.quantity) AS cost, sum(s.quantity) AS sales_qty";
		sc += " , (sum(s.commit_price * s.quantity)) AS gross_sales ";		
		sc += " , (sum(s.commit_price * s.quantity) - sum(c.average_cost * s.quantity)) AS gross_profit ";		
		sc += " FROM sales s LEFT OUTER JOIN code_relations c ON c.code = s.code ";
		sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
		sc += " WHERE 1=1 ";
		if(Session["slt_code"] != null && Session["slt_code"] != "")
			sc += " AND s.code = "+ Session["slt_code"];
		if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
			if(TSIsDigit(Request.QueryString["code"].ToString()))
				sc += " AND s.code = "+ Request.QueryString["code"];
		if(Session["branch_support"] != null)
		{
			if(TSIsDigit(m_branchID) && m_branchID != "all")
				sc += " AND i.branch = " + m_branchID;
		}
		sc += m_dateSql;

		sc += " GROUP BY s.code, s.supplier_code, s.name, c.supplier_price, c.manual_cost_frd, c.manual_exchange_rate, c.rate ";
		for(int j=1; j<=int.Parse(GetSiteSettings("dealer_levels", "1")); j++)
			sc += ", level_rate"+ j +"";
		sc += " ORDER BY sum(s.quantity) DESC ";
	}
	else
	{
	    sc += " SELECT s.code, s.supplier_code, s.name, sum(s.commit_price * s.quantity) AS commit_price  ";
	    sc += ", sum(c.average_cost * s.quantity) AS cost, sum(s.quantity) AS sales_qty";
	    sc += " , (sum(s.commit_price * s.quantity)) AS gross_sales ";
//	sc += " , (sum(s.supplier_price * s.quantity)) AS gross_sales ";
	    sc += " , (sum(s.commit_price * s.quantity) - sum(c.average_cost * s.quantity)) AS gross_profit ";
	    sc += " FROM sales s ";
	    sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
        sc += " JOIN code_relations c ON c.code = s.code ";
	    sc += " WHERE 1=1 ";
	    if(Session["slt_code"] != null && Session["slt_code"] != "")
		    sc += " AND s.code = "+ Session["slt_code"];
	    if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
        {
		    if(TSIsDigit(Request.QueryString["code"].ToString()))
			    sc += " AND s.code = "+ Request.QueryString["code"];
        }
	    if(Session["branch_support"] != null)
	    {
		    if(TSIsDigit(m_branchID) && m_branchID != "all")
			    sc += " AND i.branch = " + m_branchID;
	    }
	    sc += m_dateSql;

	    sc += " GROUP BY s.code, s.supplier_code, s.name ";
	    sc += " ORDER BY sum(s.quantity) DESC ";

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
	m_sFileName = "ItemBased";
	if(Request.Form["cmd"] == "Export Report")
	{
		bool bRet = true;
		bRet = EmptyBackupFolder();
		bRet = WriteCSVFile(ds);
			
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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"]+ "?export=done\">");
			//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
			return true;
		}
		return true;
		
	}

		Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
		Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
		if(ds.Tables["report"].Rows.Count <= 0 )
		{
			Response.Write("<br><center><h4>No Records...");
			Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><br>");
			return false;
		}
		
		BindPRItem();
		if(m_bShowPic && m_bGBSetShowPicOnReport)
		{
			DrawChart();

			string uname = EncodeUserName();

			Response.Write("<img src=" + m_picFile + ">");

			Response.Write("<form action=rprofit.aspx method=post>");
			Response.Write("<input type=hidden name=period value=" + m_nPeriod + ">");
			Response.Write("<input type=hidden name=type value=" + m_type + ">");
			Response.Write("<input type=hidden name=day_from value=" + Request.Form["day_from"] + ">");
			Response.Write("<input type=hidden name=month_from value=" + Request.Form["month_from"] + ">");
			Response.Write("<input type=hidden name=day_to value=" + Request.Form["day_to"] + ">");
			Response.Write("<input type=hidden name=month_to value=" + Request.Form["month_to"] + ">");

		/*	Response.Write("<b>Chart Type : </b>");
			Response.Write("<select name=chart_type>");
			for(int i=0; i<cts; i++)
			{
				Response.Write("<option value=" + i.ToString());
				if(m_ct == i)
					Response.Write(" selected");
				Response.Write(">" + sct[i] + "</option>");
			}
			Response.Write("</select>");
		*/

		//	Response.Write(" <input type=checkbox name=legends");
		//	if(m_bHasLegends)
		//		Response.Write(" checked");
		//	Response.Write("><b>Legends</b> ");
			
		//	Response.Write(" <input type=submit name=cmd value=Redraw " + Session["button_style"] + ">");
			Response.Write("</form>");
		}
		Session["slt_code"] = null;
	
	return true;
}

/////////////////////////////////////////////////////////////////
void BindPRItem()
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
	m_cPI.URI = "?branch="+m_branchID+"&np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	if(m_sdTo != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	m_cPI.PageSize = 50;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	//DEBUG(" rows = ", rows );
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<th>Date</th>");
	Response.Write("<th>CODE</th>");
//	Response.Write("<th>BARCODE</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>DESCRIPTION</th>");
	Response.Write("<th>SALES QTY</th>");
	Response.Write("<th align=right>GROSS SALES</th>");
	Response.Write("<th align=right>GROSS COST</th>");
	Response.Write("<th align=right>PROFIT</th>");
	Response.Write("<th align=right>GP(%)</th>");
//	Response.Write("<th align=right>MARGIN</th>");
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

	double dQtyTotal = 0;
	double dSalesTotal = 0;
	double dCostTotal = 0;
	double dProfitTotal = 0;
	double dMarginTotal = 0;
	double dGPTotal = 0;
	int margins = 0;

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	StringBuilder sb3 = new StringBuilder();
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		//string date = dr["price_age"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sales_qty = dr["sales_qty"].ToString();
//		string sales_amount = dr["sales_amount"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string cost = dr["cost"].ToString();
		//get the sum of total sales price which is different from invoice's price
//		string total_commit = dr["total_commit"].ToString(); 
		
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dCost = MyDoubleParse(cost);
		double dProfit = dCommitPrice - dCost;

/*		double dQTY = MyDoubleParse(sales_qty);
		double dSales = MyDoubleParse(sales_amount) * dQTY;
		double dCommitPrice = MyDoubleParse(commit_price)* dQTY;
		double dTotalCommit = MyDoubleParse(total_commit);
		dSales = dSales;
		double dCost = MyDoubleParse(cost) * dQTY;
		double dPercent = 1;
	
		if(dSales < dTotalCommit)
		{
			dPercent -=  ((dTotalCommit - dSales ) / dTotalCommit);
			dCommitPrice = (dCommitPrice * dPercent);
		}
//		DEBUG(" qty = ", dQTY.ToString());
//DEBUG(" sales = ", dSales.ToString());
		dSales = dCommitPrice;
		dSales = dSales;
		double dProfit = dSales - dCost;
*/
		/*string gross_cost = dr["rought_cost"].ToString();
		double dCost = MyDoubleParse(gross_cost);
		string profit = dr["rough_profit"].ToString();
		double dProfit = MyDoubleParse(profit);
		*/
		
		double dMargin = 1;
		//if(dSales >= dCost)
		//	dMargin = (dSales - dCost) / dSales;
		if(dCost != 0)
			dMargin = dProfit / dCost;
		dMargin = Math.Round(dMargin, 4);
		double dGP = 1;
		if(dCommitPrice != 0)
			dGP = dProfit / dCommitPrice;
//		if(MyDoubleParse(sales_qty) == 0)
//			continue;
		//DEBUG("dsales =", dSales.ToString());
		//DEBUG("dcost =", dCost.ToString());
		//DEBUG("dmargin =", ((dSales-dCost)/dSales).ToString("p"));
		//DEBUG("dsales =", dSales.ToString());
		margins++;

		dQtyTotal += dQTY;
//		dSalesTotal += dSales;
		dSalesTotal += dCommitPrice;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;
		dGPTotal += dGP;
		//DEBUG("dmargin = ", dMargin.ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		//Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center><a href='report.aspx?type=1&code=" + code + "&period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("' class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=center><a href=rsales.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=center><a href=rprofit.aspx?type=0&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dCommitPrice.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dCost.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		Response.Write("<td align=right>" + (dGP).ToString("p") + "</td>");
//		Response.Write("<td align=right>" + dMargin.ToString("p") + "</td>");
		//Response.Write("<td align=right>" + dMargin.ToString("p") + "</td>");
		Response.Write("</tr>");

		if(dCommitPrice > m_nMaxY)
			m_nMaxY = dCommitPrice;

		if(dCommitPrice < 0 && dCommitPrice < m_nMinY)
			m_nMinY = dCommitPrice;

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

		y = dCommitPrice.ToString();
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
		dMarginAve = dProfitTotal / dCostTotal;
	//DEBUG("dmarginave = ", dMarginAve.ToString());
	//dMarginAve = Math.Round(dMarginTotal / margins, 4);
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
	Response.Write("<tr><td colspan=8>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3 ><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=middle >" + dQtyTotal.ToString() + "</td>");
	Response.Write("<td align=right >" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dProfitTotal.ToString("c") + "</td>");	
	Response.Write("<td align=right nowrap>" + (dProfitTotal/dSalesTotal).ToString("p") + "</td>");
//	Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	dQtyTotal = 0;
		dSalesTotal = 0;
		dCostTotal = 0;
		dProfitTotal = 0;
		dMarginTotal = 0;
		dGPTotal = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		
		string sales_qty = dr["sales_qty"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string cost = dr["cost"].ToString();
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dCost = MyDoubleParse(cost);
		double dProfit = dCommitPrice - dCost;
		double dMargin = 0;

		dMargin = 1;
		if(dCost != 0)
			dMargin = dProfit / dCost;
		double dGP = 1;
		if(dCommitPrice != 0)
			dGP = dProfit / dCommitPrice;

		dMargin = Math.Round(dMargin, 4);
		dQtyTotal += dQTY;
		dSalesTotal += dCommitPrice;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;
		dGPTotal += dProfitTotal / dSalesTotal;
	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=middle style=\"font-size:14\">" + dQtyTotal.ToString() + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + (dProfitTotal/dSalesTotal).ToString("p") + "</td>");
//	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<br>");

	//write xml data file for chart image
	if(m_bShowPic && m_bGBSetShowPicOnReport)
		WriteXMLFile();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item Catagory Summary
bool DoItemCatagorySummary()
{

	m_tableTitle = "ITEM SUMMARY for ";
	if(m_brand != "" && m_brand != null && m_brand != "all")
		m_tableTitle += m_brand.ToUpper();
	if(m_cat != "" && m_cat != null &&  m_cat != "all")
		m_tableTitle += " Cat: "+ m_cat.ToUpper();
	if(m_scat != "" && m_scat != null && m_scat != "all")
		m_tableTitle += " Sub Cat: "+ m_scat.ToUpper();
	if(m_sscat != "" && m_sscat != null && m_sscat != "all")
		m_tableTitle += " Sub Sub Cat: "+ m_sscat.ToUpper();
	if(m_brand == "" && (m_cat == "all" || m_cat == ""))
		m_tableTitle += "ALL ";
//DEBUG("m_branc =", m_brand );
//DEBUG("m_branc =",  m_cat);
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			m_tableTitle += " <br><font size=2>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=2>branch: All</font>";
	}
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
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59"+"'  ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_sdFrom + "' AND MONTH(i.commit_date) <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

//DEBUG(" m_dateSql=", m_dateSql);
	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
//	if(!m_bCompair)

	if(Request.Form["cmd"] == "Export Report")
	{
		sc += " SELECT  sum(c.average_cost * s.quantity) AS gross_cost, sum(s.commit_price * s.quantity) AS gross_sales, sum(s.quantity) AS sales_qty ";
		sc += " , sum(s.commit_price * s.quantity) - sum(c.average_cost * s.quantity) AS profit ";		
		sc += " , '0' AS margin ";
		sc += ", c.brand, c.cat, c.s_cat, c.ss_cat ";
		sc += " , c.average_cost AS last_cost, c.manual_cost_frd AS manaul_cost , (c.manual_cost_frd * c.manual_exchange_rate * c.rate) AS bottom_price ";
		for(int j=1; j<=int.Parse(GetSiteSettings("dealer_levels", "1")); j++)
			sc += " , (c.manual_cost_frd * c.manual_exchange_rate * c.rate * c.level_rate"+ j +") AS dealer_price"+ j +"  ";
		int nCount = 6;
		int nMonthDiffer = 6;
		while(nCount > 0)
		{
				sc += ", ISNULL((SELECT SUM(ss.commit_price * ss.quantity) "; 											
				sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number JOIN orders oo ON oo.invoice_number = ii.invoice_number ";        
				sc += " WHERE ss.code = s.code ";						
				sc += " AND ss.supplier_code = s.supplier_code ";
				sc += " AND DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (nMonthDiffer - nCount).ToString() +" ),'') AS 'TotalSalesItemPrice"+ (nMonthDiffer - nCount).ToString() +"' ";

				sc += ", ISNULL((SELECT SUM(ss.quantity)  "; 									
				sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number JOIN orders oo ON oo.invoice_number = ii.invoice_number ";        
				sc += " WHERE ss.code = s.code ";						
				sc += " AND ss.supplier_code = s.supplier_code ";
				sc += " AND DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (nMonthDiffer - nCount).ToString() +" ),'') AS 'TotalSalesQTY"+ (nMonthDiffer - nCount).ToString() +"' ";

				nCount--;
		}
		sc += " FROM sales s ";
		sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
		sc += " JOIN code_relations c ON s.code = c.code  "; //AND s.supplier_code = c.supplier_code ";
		sc += " WHERE 1=1 ";
		if(Session["branch_support"] != null)
		{
			if(TSIsDigit(m_branchID) && m_branchID != "all")
				sc += " AND i.branch = " + m_branchID;
		}
		if(m_brand != "" && m_brand != null && m_brand != "all")
			sc += " AND c.brand = '"+ m_brand +"'";
		if(m_cat != "" && m_cat != null &&  m_cat != "all")
			sc += " AND c.cat = '"+ m_cat +"'";
		if(m_scat != "" && m_scat != null && m_scat != "all")
			sc += " AND c.s_cat = '"+ m_scat +"'";
		if(m_sscat != "" && m_sscat != null && m_sscat != "all")
			sc += " AND c.ss_cat = '"+ m_sscat +"'";

		if(Session["slt_code"] != null && Session["slt_code"] != "")
			sc += " AND p.code = "+ Session["slt_code"];
		sc += m_dateSql;

		sc += " GROUP BY c.brand, c.cat ,c.s_cat ,c.ss_cat, c.supplier_price, s.code, s.supplier_code, c.manual_cost_frd, c.manual_exchange_rate , c.rate ";
		for(int j=1; j<=int.Parse(GetSiteSettings("dealer_levels", "1")); j++)
			sc += " , c.level_rate"+ j +"  ";
		sc += " ORDER BY c.brand, c.cat, c.s_cat, c.ss_cat ";	
	}
	else
	{
		
		sc += " SELECT  sum(c.average_cost * s.quantity) AS gross_cost, sum(s.commit_price * s.quantity) AS gross_sales, sum(s.quantity) AS sales_qty ";
		sc += " , sum(s.commit_price * s.quantity) - sum(c.average_cost * s.quantity) AS profit ";
		//sc += " , ((sum(s.commit_price * s.quantity) - sum(s.supplier_price* s.quantity))/ sum(s.commit_price * s.quantity)) * 100 AS margin ";
		sc += " , '0' AS margin ";
		sc += ", c.brand, c.cat, c.s_cat,c.ss_cat ";
		sc += " FROM sales s ";
		sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
		sc += " JOIN code_relations c ON s.code = c.code  "; //AND s.supplier_code = c.supplier_code ";
		sc += " WHERE 1=1 ";
		if(Session["branch_support"] != null)
		{
			if(TSIsDigit(m_branchID) && m_branchID != "all")
				sc += " AND i.branch = " + m_branchID;
		}
		if(m_brand != "" && m_brand != null && m_brand != "all")
			sc += " AND c.brand = '"+ m_brand +"'";
		if(m_cat != "" && m_cat != null &&  m_cat != "all")
			sc += " AND c.cat = '"+ m_cat +"'";
		if(m_scat != "" && m_scat != null && m_scat != "all")
			sc += " AND c.s_cat = '"+ m_scat +"'";
		if(m_sscat != "" && m_sscat != null && m_sscat != "all")
			sc += " AND c.ss_cat = '"+ m_sscat +"'";

		if(Session["slt_code"] != null && Session["slt_code"] != "")
			sc += " AND p.code = "+ Session["slt_code"];
		sc += m_dateSql;

		sc += " GROUP BY c.brand, c.cat ,c.s_cat ,c.ss_cat ";
	//	sc += " HAVING SUM(s.commit_price * s.quantity) > 0 ";
	//	sc += " HAVING sum(s.quantity) >0 ";
		sc += " ORDER BY c.brand, c.cat, c.s_cat, c.ss_cat ";	
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

m_sFileName = "BrandBased";
	if(Request.Form["cmd"] == "Export Report")
	{
		bool bRet = true;
		bRet = EmptyBackupFolder();
		bRet = WriteCSVFile(ds);
			
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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"]+ "?export=done\">");
			//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
			return true;
		}
		return true;
		
	}

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	if(ds.Tables["report"].Rows.Count <= 0 )
	{
		Response.Write("<br><center><h4>No Records...");
		Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><br>");
		return false;
	}
	BindCatagorySummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=rprofit.aspx method=post>");
	Response.Write("<input type=hidden name=period value=" + m_nPeriod + ">");
	Response.Write("<input type=hidden name=type value=" + m_type + ">");
	Response.Write("<input type=hidden name=day_from value=" + Request.Form["day_from"] + ">");
	Response.Write("<input type=hidden name=month_from value=" + Request.Form["month_from"] + ">");
	Response.Write("<input type=hidden name=day_to value=" + Request.Form["day_to"] + ">");
	Response.Write("<input type=hidden name=month_to value=" + Request.Form["month_to"] + ">");

/*	Response.Write("<b>Chart Type : </b>");
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
*/
	Response.Write("</form><br><br><br>");
	}
	return true;
}

void BindCatagorySummary()
{
		int i = 0;

	PageIndex m_cPI = new PageIndex(); //page index class
	
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
//DEBUG(" rows = ", rows);
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?branch="+m_branchID+"&np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	if(m_sdTo != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	if(m_brand != "" && m_brand != null)
		m_cPI.URI += "&brand="+ m_brand +"";
	if(m_cat != "" && m_cat != null)
		m_cPI.URI += "&cat="+ m_cat +"";
	if(m_scat != "" && m_scat != null)
		m_cPI.URI += "&scat="+ m_scat +"";
	if(m_sscat != "" && m_sscat != null)
		m_cPI.URI += "&sscat="+ m_sscat +"";

	m_cPI.PageSize = 25;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	//DEBUG(" rows = ", rows );
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<th>Date</th>");
	Response.Write("<th>BRAND</th>");
	Response.Write("<th>CAT</th>");
	Response.Write("<th>SUB_CAT</th>");
	Response.Write("<th>SUB_SUB_CAT</th>");
	Response.Write("<th align=right >SALES QTY</th>");
	Response.Write("<th align=right >GROSS SALES</th>");
	Response.Write("<th align=right >GROSS COST</th>");
	Response.Write("<th align=right >PROFIT</th>");
	Response.Write("<th align=right >GP(%)</th>");
//	Response.Write("<th align=right >MARGIN</th>");
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
	double dCostTotal = 0;
	double dProfitTotal = 0;
	double dMarginTotal = 0;
	int margins = 0;
	
	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	double dTotalQTY = 0;
	double dGPTotal = 0;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	StringBuilder sb3 = new StringBuilder();
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
	
		string brand = dr["brand"].ToString();
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		string sales_qty = dr["sales_qty"].ToString();
		string total_sales = dr["gross_sales"].ToString();
		string total_cost = dr["gross_cost"].ToString();
		string margin = dr["margin"].ToString();
		double dtsales = MyDoubleParse(total_sales);
		double dtcost = MyDoubleParse(total_cost);
		if(dtcost != dtsales && dtcost != 0)
			margin = (((dtsales - dtcost) / dtcost ) * 100).ToString();
		string sGPPercent = (((dtsales - dtcost) / dtsales ) * 100).ToString();
		string profit = dr["profit"].ToString();
//	DEBUG("sales qty =", sales_qty);
		//if(MyIntParse(sales_qty) == 0)
		
		if(MyDoubleParse(sales_qty) == 0)
			continue;
		sales_qty = (Math.Round(MyDoubleParse(sales_qty),0)).ToString();
		margins++;
		
		dSalesTotal += double.Parse(total_sales);
		dCostTotal += double.Parse(total_cost);
		dProfitTotal += double.Parse(profit);
		dMarginTotal += double.Parse(margin);
		dGPTotal += double.Parse(sGPPercent);
		dTotalQTY += double.Parse(sales_qty);
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td>" + brand + "</td>");
		Response.Write("<td>" + cat + "</td>");
		Response.Write("<td>" + s_cat + "</td>");
		Response.Write("<td>" + ss_cat + "</td>");
		//Response.Write("<td align=center><a href=rprofit.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=center><a href=report.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
		Response.Write("<td align=right>" + sales_qty + "</td>");
		Response.Write("<td align=right>" + double.Parse(total_sales).ToString("c") + "</td>");
		Response.Write("<td align=right>" + double.Parse(total_cost).ToString("c") + "</td>");
		Response.Write("<td align=right>" + double.Parse(profit).ToString("c") + "</td>");
		Response.Write("<td align=right>" + (double.Parse(profit)/double.Parse(total_sales)).ToString("p") + "</td>");
//		Response.Write("<td align=right>" + (double.Parse(margin)/100).ToString("p") + "</td>");
		Response.Write("</tr>");

		if(double.Parse(total_sales) > m_nMaxY)
			m_nMaxY = double.Parse(total_sales);

		if(double.Parse(total_sales) < 0 && double.Parse(total_sales) < m_nMinY)
			m_nMinY = double.Parse(total_sales);

		//xml chart data
		x = (i).ToString();
		m_nMaxX = i;

		y = profit;
				
		legend = brand.Replace("&", " ");
	
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");

		y = total_cost;
		legend = brand.Replace("&", " ");
	
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");

		y = double.Parse(total_sales).ToString();
		legend = brand.Replace("&", " ");
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

	
	double dMarginAve = dProfitTotal / dCostTotal;

	//total
	Response.Write("<tr><td colspan=8>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=4 ><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=right>" + dTotalQTY.ToString() + "</td>");
	Response.Write("<td align=right>" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap>" + (dProfitTotal/dSalesTotal).ToString("p") + "</td>");
//	Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	dSalesTotal = 0;
		dCostTotal = 0;
		dProfitTotal = 0;
		dMarginTotal = 0;
		dTotalQTY = 0;
		dGPTotal = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string sales_qty = dr["sales_qty"].ToString();
		string total_sales = dr["gross_sales"].ToString();
		string total_cost = dr["gross_cost"].ToString();
		string margin = dr["margin"].ToString();
		string profit = dr["profit"].ToString();
		if(MyDoubleParse(sales_qty) == 0)
			continue;
		sales_qty = (Math.Round(MyDoubleParse(sales_qty),0)).ToString();
		
		dSalesTotal += double.Parse(total_sales);
		dCostTotal += double.Parse(total_cost);
		dProfitTotal += double.Parse(profit);
		if(dSalesTotal != dCostTotal && dCostTotal != 0)
			margin = (((dSalesTotal - dCostTotal) / dCostTotal ) *100).ToString();
		string sGPPercent = (((dSalesTotal - dCostTotal) / dSalesTotal ) *100).ToString();
		dMarginTotal += double.Parse(margin);
		dGPTotal += double.Parse(sGPPercent);
		dTotalQTY += double.Parse(sales_qty);
	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=4 style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dTotalQTY.ToString() + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + (dProfitTotal / dSalesTotal).ToString("p") + "</td>");
//	Response.Write("<td align=right nowrap style=\"font-size:14\">" + (dProfitTotal / dCostTotal).ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	//Response.Write("<br>");
	//write xml data file for chart image
	if(m_bShowPic && m_bGBSetShowPicOnReport)
		WriteXMLFile();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Sales Person based
bool DoPRSales()
{
	m_tableTitle = "Profit & Loss - Sales Person Based for ";
	m_tableTitle += "(<font color=Green><b>";
	if(m_sales_id != null && m_sales_id != "" && m_sales_id != "all")
		m_tableTitle += m_sales_person;
	else
		m_tableTitle += "ALL";
	m_tableTitle += "</b></font>)";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			m_tableTitle += " <br><font size=2>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=2>branch: All</font>";
	}
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
		//m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND  DATEADD(day, 1, '" + m_sdTo + "') ";
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59"+"'  ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_sdFrom + "' AND MONTH(i.commit_date) <= '" + m_sdTo + "' ";
		//m_dateSql = " AND MONTH(i.commit_date) >= '" + m_sdFrom + "' AND MONTH(i.commit_date) <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

	if(m_sdFrom == "" || m_sdFrom == null)
	{
		Response.Write("<script language=javascript>window.history.back()</script");
		Response.Write(">");
		return false;
	}

	ds.Clear();
	string sc = "";
//DEBUG("m_sales_id = ", m_sales_id);
	sc = " SET DATEFORMAT dmy ";
	if(m_bCompair)
	{
		int nDifferent = 0;
		if(int.Parse(m_syFrom) < int.Parse(m_syTo))
			nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
		m_sdTo = (int.Parse(m_sdTo) + nDifferent).ToString();

		sc += " SELECT SUM(i.price) AS amount ";
		int nPlus = 0;
	
		for(int ii=int.Parse(m_sdFrom); ii<=int.Parse(m_sdTo); ii++)
		{
			int nMonth = ii;
			int nYear = int.Parse(m_syFrom);
			
			int nn = 0;
			if(nMonth > 12)
			{
			//nn = int.Parse(Math.Round((double.Parse(nMonth.ToString()) / 16.2),0).ToString());
				string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
				nn = int.Parse(snn[0].ToString());
			
				nPlus++;
				if(nPlus == 13)
					nPlus = 1;
				nMonth = nPlus;
				nYear = (int.Parse(m_syFrom) + nn);
			}
			
			sc += ", ISNULL(( ";
			{
				sc += " SELECT SUM(ii.price) ";
				sc += " FROM invoice ii JOIN orders oo ON oo.invoice_number = ii.invoice_number ";
				//sc += " WHERE MONTH(ii.commit_date) = '" + ii + "' ";
				sc += " WHERE MONTH(ii.commit_date) = '" + nMonth + "' ";
				sc += " AND YEAR(ii.commit_date) = "+ nYear +"";
				if(m_sales_id != "all" && m_sales_id != "")
					sc += " AND oo.sales = "+ m_sales_id;
			}
			sc += " ), 0) AS '"+ nYear + nMonth +"' ";
			sc += ", ISNULL(( ";
			{
				sc += " SELECT SUM(ii.freight) ";
				sc += " FROM invoice ii JOIN orders oo ON oo.invoice_number = ii.invoice_number ";
				//sc += " WHERE MONTH(ii.commit_date) = '" + ii + "' ";
				sc += " WHERE MONTH(ii.commit_date) = '" + nMonth + "' ";
				sc += " AND YEAR(ii.commit_date) = "+ nYear +"";
				if(m_sales_id != "all" && m_sales_id != "")
					sc += " AND oo.sales = "+ m_sales_id;
			}
			sc += " ), 0) AS freight"+ nYear + nMonth +" ";
		
		}
		sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
			sc += " AND o.sales = "+ m_sales_id;
		if(Session["branch_support"] != null)
		{
			if(TSIsDigit(m_branchID) && m_branchID != "all")
				sc += " AND i.branch = " + m_branchID;
		}
	}
	else
	{
		if(Request.Form["cmd"] == "Export Report")
		{			
			sc += " SELECT SUM(i.price) AS sales_amount, SUM(i.freight) AS freight ";
			sc += ",(  ";
			sc += "SELECT SUM(ISNULL(co.average_cost, s.commit_price) * s.quantity) ";
			sc += " FROM sales s INNER JOIN ";
			sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
			sc += " orders oo ON oo.invoice_number = s.invoice_number ";
            sc += " JOIN code_relations co ON co.code = s.code ";
			sc += " WHERE ISNULL(oo.sales,'') = ISNULL(o.sales,'') ";
			sc += m_dateSql;
			sc += " AND i.branch = " + m_branchID + " ) AS gross_profit ";
			sc += ",(  ";
			sc += "SELECT SUM(s.quantity) ";
			sc += " FROM sales s INNER JOIN ";
			sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
			sc += " orders oo ON oo.invoice_number = s.invoice_number ";
            sc += " WHERE ISNULL(oo.sales,'') = ISNULL(o.sales,'') ";
			sc += m_dateSql;
			sc += " AND i.branch = " + m_branchID + " ) AS total_qty ";
			int nCount = 6;
			int nMonthDiffer = 6;
			while(nCount > 0)
			{
					sc += ", ISNULL((SELECT SUM(ss.commit_price * ss.quantity) "; 											
					sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number JOIN orders oo ON oo.invoice_number = ii.invoice_number ";        
					sc += " WHERE ISNULL(oo.sales,'') = ISNULL(o.sales,'') AND ii.branch = " + m_branchID + " AND DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (nMonthDiffer - nCount).ToString() +" ),'') AS 'TotalSalesPrice"+ (nMonthDiffer - nCount).ToString() +"' ";

					sc += ", ISNULL((SELECT SUM(ss.quantity)  "; 									
					sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number JOIN orders oo ON oo.invoice_number = ii.invoice_number ";        
					sc += " WHERE  ISNULL(oo.sales,'') = ISNULL(o.sales,'') AND  ii.branch = " + m_branchID + " AND  DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (nMonthDiffer - nCount).ToString() +" ),'') AS 'TotalSalesQTY"+ (nMonthDiffer - nCount).ToString() +"' ";

					nCount--;
			}


			sc += " , isnull(c.name,'Online Orders') AS name, c.type, ISNULL(o.sales, '-1') AS id ";
			sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " LEFT OUTER JOIN card c ON c.id = o.sales ";
			sc += " WHERE 1=1 ";
			if(m_sales_id != "all" && m_sales_id != "")
				sc += " AND o.sales = "+ m_sales_id;
			if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
			{
				sc += " AND (i.branch = " + m_branchID + " OR o.sales IS NULL ) ";
			}
			sc += m_dateSql;

			sc += " GROUP BY o.sales, c.name, c.type, c.id ";
			sc += " ORDER BY o.sales ";
		}
		else
		{
		
		    sc += " SELECT SUM(i.price) AS sales_amount, SUM(i.freight) AS freight ";
		//sc += ", (SELECT SUM(s.supplier_price * s.quantity) ";
		    sc += ",(  ";
		    sc += "SELECT SUM(ISNULL(co.average_cost, s.commit_price) * s.quantity) ";
		    sc += " FROM sales s INNER JOIN ";
		    sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		    sc += " orders oo ON oo.invoice_number = s.invoice_number ";
            sc += " JOIN code_relations co ON co.code = s.code ";
//		sc += " LEFT OUTER JOIN sales_cost sc ON sc.invoice_number = s.invoice_number AND sc.code = s.code ";
		    sc += " WHERE ISNULL(oo.sales,'') = ISNULL(o.sales,'') ";
/*	
		sc += ", (SELECT SUM(s.cost * s.qty) ";
		sc += " FROM sales_cost s INNER JOIN ";
		sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		sc += " orders oo ON oo.invoice_number = s.invoice_number ";
		sc += " WHERE oo.sales = o.sales ";
*/
		    sc += m_dateSql;
		    sc += " AND i.branch = " + m_branchID + " ) AS gross_profit ";
		//sc += ", (SELECT SUM(s.supplier_price * s.quantity) ";
			sc += ",(  ";
		    sc += "SELECT SUM(s.quantity) ";
		    sc += " FROM sales s INNER JOIN ";
		    sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		    sc += " orders oo ON oo.invoice_number = s.invoice_number ";
		    sc += " WHERE ISNULL(oo.sales,'') = ISNULL(o.sales,'') ";
		    sc += m_dateSql;
		    sc += " AND i.branch = " + m_branchID + " ) AS total_qty ";
/*		sc += " (SELECT SUM(ISNULL(s.supplier_price, s.commit_price) * s.quantity) ";
//		sc += ", (SELECT SUM(ISNULL(sc.cost, s.supplier_price) * s.quantity) ";
		sc += " FROM sales s INNER JOIN ";
		sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		sc += " orders oo ON oo.invoice_number = s.invoice_number ";
//		sc += " LEFT OUTER JOIN sales_cost sc ON sc.invoice_number = s.invoice_number AND sc.code = s.code ";
		sc += " WHERE ISNULL(oo.sales, '') = ISNULL(o.sales, '') ";
		sc += m_dateSql;
		sc += "  AND i.branch = " + m_branchID + " ) AS gross_profit1 ";
*/	

		    sc += " , isnull(c.name,'Online Orders') AS name, c.type, ISNULL(o.sales, '-1') AS id ";
		    sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		    sc += " LEFT OUTER JOIN card c ON c.id = o.sales ";
		    sc += " WHERE 1=1 ";
		    if(m_sales_id != "all" && m_sales_id != "")
			    sc += " AND o.sales = "+ m_sales_id;
		    if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
		    {
//			if(m_branchID == "1") //main branch, list online orders too
//				sc += " AND (c.our_branch = " + m_branchID + " OR o.sales IS NULL ) ";
			    sc += " AND (i.branch = " + m_branchID + " OR o.sales IS NULL ) ";
//			else
//				sc += " AND c.our_branch = " + m_branchID;
		    }
		    sc += m_dateSql;
//		if(m_branchID == "1")
//			sc += " OR o.sales IS NULL ";
		    sc += " GROUP BY o.sales, c.name, c.type, c.id ";
		    sc += " ORDER BY o.sales ";
		}
//DEBUG("sc =", sc);
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
	
		sc = " SET DATEFORMAT dmy ";
//		sc += "	SELECT SUM(s.cost * is.qty) AS gross_profit, ISNULL(oo.sales, '-1') AS id ";
		sc += "SELECT SUM(ISNULL(co.average_cost, s.commit_price) * s.quantity) AS gross_profit, ISNULL(oo.sales, '-1') AS id ";
		//-- last old --
		//sc += "SELECT SUM(s.supplier_price * s.quantity) AS gross_profit, ISNULL(oo.sales, '-1') AS id ";
		sc += " FROM invoice i INNER JOIN ";
		sc += " orders oo ON i.invoice_number = oo.invoice_number ";
		sc += " LEFT OUTER JOIN sales s ON s.invoice_number = i.invoice_number AND s.invoice_number = oo.invoice_number ";
		sc += " LEFT OUTER JOIN card c ON c.id = oo.sales ";
        sc += " LEFT OUTER JOIN code_relations co ON co.code = s.code ";
//		sc += " LEFT OUTER JOIN sales_cost sc ON sc.invoice_number = s.invoice_number AND sc.code = s.code ";
//		sc += " sales_cost s ON s.invoice_number = i.invoice_number AND s.invoice_number = oo.invoice_number ";
		
		sc += " WHERE  1=1 ";
		if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
		{
			sc += " AND c.our_branch = " + m_branchID;
		}
		sc += m_dateSql;
		sc += " GROUP BY oo.sales ";
		sc += " ORDER BY oo.sales ";
	
//DEBUG("sc1 =", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(ds, "gross_profit");  

		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
	}

//DEBUG("sc =", sc);
	if(m_bCompair)
	{
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
	}

	m_sFileName = "SalesBased";
	if(Request.Form["cmd"] == "Export Report")
	{
		bool bRet = true;
		bRet = EmptyBackupFolder();
		bRet = WriteCSVFile(ds);
			
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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"]+ "?export=done\">");
			//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
			return true;
		}
		return true;
		
	}


	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	if(ds.Tables["report"].Rows.Count <= 0 )
	{
		Response.Write("<br><center><h4>No Records...");
		Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><br>");
		return false;
	}

	BindPRSales();

	m_xLabel = "Sales Person";
	m_yLabel = "Profit";
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=rprofit.aspx method=post>");
	Response.Write("<input type=hidden name=period value=" + m_nPeriod + ">");
	Response.Write("<input type=hidden name=type value=" + m_type + ">");
	Response.Write("<input type=hidden name=day_from value=" + Request.Form["day_from"] + ">");
	Response.Write("<input type=hidden name=month_from value=" + Request.Form["month_from"] + ">");
	Response.Write("<input type=hidden name=day_to value=" + Request.Form["day_to"] + ">");
	Response.Write("<input type=hidden name=month_to value=" + Request.Form["month_to"] + ">");

/*	Response.Write("<b>Chart Type : </b>");
	Response.Write("<select name=chart_type>");
	for(int i=0; i<cts; i++)
	{
		Response.Write("<option value=" + i.ToString());
		if(m_ct == i)
			Response.Write(" selected");
		Response.Write(">" + sct[i] + "</option>");
	}
	Response.Write("</select>");
*/
//	Response.Write(" <input type=checkbox name=legends");
//	if(m_bHasLegends)
//		Response.Write(" checked");
//	Response.Write("><b>Legends</b> ");
	
//	Response.Write(" <input type=submit name=cmd value=Redraw " + Session["button_style"] + ">");
	Response.Write("</form>");
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindPRSales()
{
	int i = 0;
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(m_bCompair)
	{
		int nDifferent = 0;
		if(int.Parse(m_syFrom) < int.Parse(m_syTo))
			nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
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
				nYear = int.Parse(m_syFrom) + nn;
		
			}
		
			string s_month = m_EachMonth[nMonth-1] +"-"+ nYear ;
			Response.Write("<th>"+ s_month +"</th>");
			
		}
	}
	else
	{
		Response.Write("<th>Sales</th>");
		Response.Write("<th>Sales_Amount</th>");
		Response.Write("<th>Profit_Generated</th>");
		
	}
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
	double dFreightTotal = 0;
	double dAllTotal = 0;
	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalQTY = 0;

	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();

	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		if(!m_bCompair)
		{
			string sales_id = dr["id"].ToString();
			string sales = dr["name"].ToString();
			string sales_amount = dr["sales_amount"].ToString();
			double dSales = MyDoubleParse(sales_amount);
			//string profit = dr["gross_profit"].ToString();
			string profit = "0";
			profit = dr["gross_profit"].ToString();
			double dprofit_temp = 0;
			if(profit != null && profit != "")
				dprofit_temp = MyDoubleParse(profit);
			if(profit == null || profit == "" || dprofit_temp <0 )
				profit = ds.Tables["gross_profit"].Rows[i]["gross_profit"].ToString();
//DEBUG("profit = ", profit);
	///		if(profit == null || profit == "" || dprofit_temp <0)
	//			profit = dr["gross_profit1"].ToString();
			
	//		DEBUG("profit = ", profit);
			double dProfit = MyDoubleParse(profit);
//			DEBUG("sales =", sales_amount);
//DEBUG("profit =", profit);
			dProfit = dSales - (dProfit);	
			if(dSales == 0)
				continue;
			dFreightTotal += MyDoubleParse(dr["freight"].ToString());
			dSalesTotal += dSales;
			dProfitTotal += dProfit;
			dAllTotal += dSales + MyDoubleParse(dr["freight"].ToString());
			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");
			Response.Write("<form action='rsales.aspx?s=" + sales_id + "");
			if(m_nPeriod == 3)
				Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"\r\n");
			Response.Write("' ");
			Response.Write(" method=post target=new>");
			Response.Write("<td>");
			Response.Write("<input type=hidden name=period value=" + m_nPeriod + ">");
			Response.Write("<input type=hidden name=type value=" + m_type + ">");
			Response.Write("<input type=hidden name=day_from value=" + Request.Form["day_from"] + ">");
			Response.Write("<input type=hidden name=month_from value=" + Request.Form["month_from"] + ">");
			Response.Write("<input type=hidden name=day_to value=" + Request.Form["day_to"] + ">");
			Response.Write("<input type=hidden name=month_to value=" + Request.Form["month_to"] + ">");
			Response.Write("<input type=submit name=cmd value='" + sales + "' title='View Details' " + Session["button_style"] + ">");
			Response.Write("</td>");
			Response.Write("</form>");
	//			<a href=rsales.aspx?s=" + sales_id + " class=o title='View Details'>" + sales + "</a></td>");
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
			sales = XMLDecoding(sales);
			legend = sales.Replace("&", " ");
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
		else
		{
			double dTotalEachMonth = 0;
			double dEachMonthFreight = 0;

			int nDifferent = 0;
			if(int.Parse(m_syFrom) < int.Parse(m_syTo))
				nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
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
					nYear = (int.Parse(m_syFrom) + nn);
					nMonth = nPlus;
				}
//DEBUG("nMonth =", nMonth);
				dEachMonthFreight = double.Parse(dr["freight"+ nYear + nMonth  +""].ToString());
				dTotalEachMonth = double.Parse(dr[""+ nYear + nMonth +""].ToString());

				Response.Write("<td align=center>"+ dTotalEachMonth.ToString("c") +"</th>");
				if(dTotalEachMonth > m_nMaxY)
					m_nMaxY = dTotalEachMonth;

				if(dTotalEachMonth < 0 && dTotalEachMonth < m_nMinY)
					m_nMinY = dTotalEachMonth;
				dFreightTotal += dEachMonthFreight;
				dTotalNoGST += dTotalEachMonth;
				dSalesTotal += dTotalEachMonth;
				dAllTotal += dTotalEachMonth + dEachMonthFreight;
				//xml chart data
				x = (i*10).ToString();
				m_nMaxX = i*10;
				y = dTotalEachMonth.ToString();
				legend = m_EachMonth[nMonth-1].Replace("&", " ");
				//legend = "test";
				sb1.Append("<chartdata>\r\n");
				sb1.Append("<x");
				if(m_bHasLegends)
					sb1.Append(" legend='" + legend + "'");
				sb1.Append(">" + x + "</x>\r\n");
				sb1.Append("<y>" + y + "</y>\r\n");
				sb1.Append("</chartdata>\r\n");
			}
			Response.Write("</tr>");
		}
	}
	if(m_bCompair)
	{
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb1.ToString());
		m_sb.Append("</chartdataisland>\r\n");
		m_IslandTitle[0] = "--Amount";
		m_nIsland = 1;
	}
	else
	{
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb1.ToString());
		m_sb.Append("</chartdataisland>\r\n");
		
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb2.ToString());
		m_sb.Append("</chartdataisland>\r\n");

		m_IslandTitle[0] = "--Profit";
		m_IslandTitle[1] = "--Sales";
		m_nIsland = 2;

		//total
		Response.Write("<tr></tr>");
		Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
		Response.Write("<td style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
		Response.Write("<td align=right style=\"font-size:13\">" + dSalesTotal.ToString("c") + "</td>");
		Response.Write("<td align=right nowrap style=\"font-size:13\">" + dProfitTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
		dSalesTotal =0;
			dProfitTotal = 0;
		for(i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["report"].Rows[i];
				
			string sales_amount = dr["sales_amount"].ToString();
			double dSales = MyDoubleParse(sales_amount);
					string profit = "0";
			profit = dr["gross_profit"].ToString();
			double dprofit_temp = 0;
			if(profit != null && profit != "")
				dprofit_temp = MyDoubleParse(profit);
			if(profit == null || profit == "" || dprofit_temp <0 )
				profit = ds.Tables["gross_profit"].Rows[i]["gross_profit"].ToString();
		//	if(profit == null || profit == "" || dprofit_temp <0)
		//		profit = dr["gross_profit1"].ToString();
			double dProfit = MyDoubleParse(profit);
			dProfit = dSales - (dProfit);	
			if(dSales == 0)
				continue;
			dSalesTotal += dSales;
			dProfitTotal += dProfit;
			
		}
		Response.Write("<tr style=\"color:black;background-color:#EEE54E;\">");
		Response.Write("<td style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
		Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
		Response.Write("<td align=right nowrap style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	}
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
	/*Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
	Response.Write("<td align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:16\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:16\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("</tr>");
	*/
	Response.Write("<tr><td colspan=3>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

	Response.Write("<br><center><h4>");
	
	Response.Write("<b>Total Freight : </b><font color=red>" + dFreightTotal.ToString("c") + "</font>&nbsp&nbsp;");
	Response.Write("<b>Total Sales : </b><font color=red>" + dSalesTotal.ToString("c") + "</font>&nbsp;");
	Response.Write("<b>Sub Total : </b><font color=red>" + dAllTotal.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</center></h4>");
	//write xml data file for chart image
	if(m_bShowPic && m_bGBSetShowPicOnReport)
		WriteXMLFile();
}


///////////////////////////////////////////////////////////////////////////////////////////////////////
//Monthly Profit Chart
bool DoPRMonthly()
{
	m_tableTitle = "Profit & Loss - Monthly Profit";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			m_tableTitle += " <br><font size=2>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=2>branch: All</font>";
	}
	string[] colName = new String[64];
	string[] dateSql = new String[64];
	int months = 0;

	int i = 0;
	DateTime dNow = DateTime.Now.AddMonths(1);
	if(m_nPeriod == 0)
	{
		months = 1;
		dNow = DateTime.Now.AddMonths(0);
	}
	if(m_nPeriod == 1)
	{
		months = 1;
		dNow = DateTime.Now.AddMonths(-1);
	}
	if(m_nPeriod == 2)
	{
		months = 3;
		dNow = DateTime.Now.AddMonths(-3);
	}
	switch(m_nPeriod)
	{
	case 0:
	case 1:
	case 2: //last 4 months
		//months = 3;
		//DateTime dNow = DateTime.Now.AddMonths(-3);
		for(i=0; i<months; i++)
		{
			colName[i] = dNow.AddMonths(i).ToString("MMM");
			dateSql[i] = " WHERE i.commit_date >= '01-" + dNow.AddMonths(i).ToString("MM-yyyy") + "' ";
			dateSql[i] += " AND i.commit_date <= '01-" + dNow.AddMonths(i+1).ToString("MM-yyyy") + "' ";
		}
		break;
	case 3:
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		DateTime d = dFrom;
		TimeSpan ts = new TimeSpan(dTo.Ticks - dFrom.Ticks);
		months = 12; //maximam 12 months supported; //ts.Months;
		for(i=0; i<months; i++)
		{
			if(d.AddMonths(i) > dTo)
				break;
			colName[i] = d.AddMonths(i).ToString("MMM");
			dateSql[i] = " WHERE i.commit_date >= '01-" + d.AddMonths(i).ToString("MM-yyyy") + "' ";
			dateSql[i] += " AND i.commit_date <= '01-" + d.AddMonths(i+1).ToString("MM-yyyy") + "' ";
		}
		months = i;
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";
	for(i=0; i<months; i++)
	{
		//sc += " SELECT SUM((s.commit_price - s.supplier_price) * s.quantity) ";
		if(i > 0)
			sc += ", ";
		sc += " ISNULL(( ";
		sc += " SELECT SUM(i.price) ";
		sc += " FROM invoice i ";
		sc += dateSql[i];
		sc += ") - (";
		//sc += " SELECT SUM(s.cost * s.qty) ";
		//sc += " FROM sales_cost s ";
		sc += " SELECT SUM(co.average_cost * s.quantity) ";
		sc += " FROM sales s ";
		sc += " JOIN invoice i ON s.invoice_number=i.invoice_number ";
        sc += " LEFT OUTER JOIN code_relations co ON co.code = s.code ";
		if(Session["branch_support"] != null)
		{
			if(TSIsDigit(m_branchID) && m_branchID != "all")
				sc += " AND i.branch = " + m_branchID;
		}
		sc += dateSql[i];
		sc += "), 0) AS ";
		sc += "'" + colName[i] + "' ";
	}
	sc += " UNION SELECT ";
	for(i=0; i<months; i++)
	{
		if(i > 0)
			sc += ", ";
		sc += " ISNULL(( ";
		sc += " SELECT SUM(i.price) ";
		//sc += " FROM sales s ";
		//sc += " JOIN invoice i ON s.invoice_number=i.invoice_number ";
		sc += " FROM invoice i ";
		sc += dateSql[i];
		if(Session["branch_support"] != null)
		{
			if(TSIsDigit(m_branchID) && m_branchID != "all")
				sc += " AND i.branch = " + m_branchID;
		}
		sc += "), 0) AS ";
		sc += "'" + colName[i] + "' ";
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

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	BindPRMonthly();

	m_xLabel = "Month";
	m_yLabel = "Profit";
	
if(m_bShowPic && m_bGBSetShowPicOnReport)
{
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=rprofit.aspx method=post>");
	Response.Write("<input type=hidden name=period value=" + m_nPeriod + ">");
	Response.Write("<input type=hidden name=type value=" + m_type + ">");
	Response.Write("<input type=hidden name=day_from value=" + Request.Form["day_from"] + ">");
	Response.Write("<input type=hidden name=month_from value=" + Request.Form["month_from"] + ">");
	Response.Write("<input type=hidden name=day_to value=" + Request.Form["day_to"] + ">");
	Response.Write("<input type=hidden name=month_to value=" + Request.Form["month_to"] + ">");

/*	Response.Write("<b>Chart Type : </b>");
	Response.Write("<select name=chart_type>");
	for(i=0; i<cts; i++)
	{
		Response.Write("<option value=" + i.ToString());
		if(m_ct == i)
			Response.Write(" selected");
		Response.Write(">" + sct[i] + "</option>");
	}
	Response.Write("</select>");
*/
//	Response.Write(" <input type=checkbox name=legends");
//	if(m_bHasLegends)
//		Response.Write(" checked");
//	Response.Write("><b>Legends</b> ");
	
//	Response.Write(" <input type=submit name=cmd value=Redraw " + Session["button_style"] + ">");
	Response.Write("</form>");
}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindPRMonthly()
{
	int i = 0;
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>&nbsp;</th>");
	DataColumnCollection dc = ds.Tables["report"].Columns;
	for(i=0; i<dc.Count; i++)
	{
		Response.Write("<th>" + dc[i].ColumnName + "</th>");
	}
	Response.Write("</tr>");

	//for xml chart data
	string x = "";
	string y = "";
	string legend = "";

	Response.Write("<tr><td><b>Profit</b></td>");

	if(ds.Tables["report"].Rows.Count <=1)
	{
		Response.Write("<br><center><h4>No Records...");
		Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><br>");
		return;
	}
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();

	DataRow dr = ds.Tables["report"].Rows[0];
	DataRow dr1 = ds.Tables["report"].Rows[1];
	for(i=0; i<dc.Count; i++)
	{
		string month = dc[i].ColumnName;
		double dProfit = MyDoubleParse(dr[i].ToString());
		double dSales = MyDoubleParse(dr1[i].ToString());
		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		
		//xml chart data
		x = (i).ToString();
		m_nMaxX = i;

		y = dProfit.ToString();
		legend = month;
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");
	}

	Response.Write("<tr><td><b>Sales</b></td>");
	for(i=0; i<dc.Count; i++)
	{
		string month = dc[i].ColumnName;
		double dSales = MyDoubleParse(dr1[i].ToString());
		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");

		//xml chart data
		x = (i).ToString();

		if(dSales > m_nMaxY)
			m_nMaxY = dSales;

		if(dSales < 0 && dSales < m_nMinY)
			m_nMinY = dSales;

		y = dSales.ToString();
		legend = month;
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x");
		if(m_bHasLegends)
			sb2.Append(" legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");
	}
	Response.Write("</tr>");

	Response.Write("</table>");

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Profit";
	m_IslandTitle[1] = "--Sales";
	m_nIsland = 2;

	
	//write xml data file for chart image
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
	//	DEBUG("m_bShowPic=", m_bShowPic.ToString());
	//	DEBUG("m_bGBSetShowPicOnReport =", m_bGBSetShowPicOnReport.ToString());
		WriteXMLFile();
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Monthly Profit Chart
bool DoProfitStatement()
{
	m_tableTitle = "Profit & Loss Statement ";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			m_tableTitle += " <br><font size=2>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=2>branch: All</font>";
	}
	string sqlSales = "";
	string sqlLoss = "";
	string sqlExpense = "";
	string sqlStockCost = "";
	int i = 0;
	switch(m_nPeriod)
	{
	case 0:
		sqlSales = " DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		sqlLoss = " DATEDIFF(month, log_time, GETDATE()) = 0 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) = 0 ";
		sqlStockCost = " DATEDIFF(month, i.date_received, GETDATE()) = 0 ";
		break;
	case 1:
		sqlSales = " DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		sqlLoss = " DATEDIFF(month, log_time, GETDATE()) = 1 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) = 1 ";
		sqlStockCost = " DATEDIFF(month, i.date_received, GETDATE()) = 1 ";
		break;
	case 2: //last 3 months
		sqlStockCost = " DATEDIFF(month, i.date_received, GETDATE()) >= 1 AND DATEDIFF(month, i.date_received, GETDATE()) <= 3 ";
		sqlSales = " DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		sqlLoss = " DATEDIFF(month, log_time, GETDATE()) >= 1 AND DATEDIFF(month, log_time, GETDATE()) <= 3 ";
		sqlExpense = " DATEDIFF(month, e.payment_date, GETDATE()) >= 1 AND DATEDIFF(month, e.payment_date, GETDATE()) <= 3 ";
		break;
	case 3:
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dFrom = DateTime.Parse(m_sdFrom, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		string from = dFrom.ToString("dd-MM-yyyy");
		string to = dTo.ToString("dd-MM-yyyy");
		sqlStockCost = " i.date_received >= '" + from + "' AND i.date_received <= '" + to + " 23:59 "+"' ";
		sqlSales = " i.commit_date >= '" + from + "' AND i.commit_date <= '" + to + " 23:59 "+"' ";
		sqlLoss = " log_time >= '" + from + "' AND log_time <= '" + to + "' ";
		sqlExpense = " e.payment_date >= '" + from + "' AND e.payment_date <= '" + to + "' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";

	//income
	sc += " ( SELECT SUM(i.price) FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " WHERE " + sqlSales;
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND i.branch = " + m_branchID;
	}
	sc += " ) ";
	sc += " AS sales_income ";
	
	sc += ", (SELECT isnull(balance,0) AS other_income ";
	sc += " FROM account ";
	sc += " WHERE class1 = 4 AND (LOWER(name4) = 'other incomes' OR  LOWER(name4) = 'other income')) AS other_income ";

	//freight
	sc += ", ( SELECT SUM(i.freight) FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " WHERE " + sqlSales;
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND i.branch = " + m_branchID;
	}
	sc += ") ";
	sc += " AS freight ";

	//cost
	sc += ", ( SELECT SUM(co.average_cost * c.quantity) FROM sales c JOIN invoice i ON c.invoice_number = i.invoice_number ";
    sc += "  LEFT OUTER JOIN code_relations co ON co.code = c.code WHERE " + sqlSales + " ";
//	sc += ", ( SELECT SUM(c.cost * c.qty) FROM sales_cost c JOIN invoice i ON c.invoice_number = i.invoice_number ";
//	sc += " WHERE " + sqlSales;
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND i.branch = " + m_branchID;
	}
	sc += ") ";
	sc += " AS sales_cost ";
	
/*	//stock cost
	sc += ", ( SELECT SUM(i.total) FROM purchase i WHERE " + sqlStockCost + " ";
	if(Session["branch_support"] != null)
	{
		sc += " AND i.branch_id = " + m_branchID;
	}
	sc += ") ";
	sc += " AS stock_cost ";
*/
	//purchase freight
	sc += ", ( SELECT SUM(i.freight) FROM purchase i WHERE " + sqlStockCost + " ";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND i.branch_id = " + m_branchID;
	}
	sc += ") ";
	sc += " AS pfreight ";

	//stock loss
	sc += ", ( SELECT SUM(c.cost * c.qty) FROM stock_loss c JOIN stock_adj_log i ON c.id = i.id ";
	sc += " WHERE " + sqlLoss;
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND i.branch_id = " + m_branchID;
	}
	sc += ") ";
	
	sc += " AS stock_loss ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "sales");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(SUM(e.total-e.tax),0) AS total, a.name4 + ' ' + a.name1 AS type ";
	sc += " FROM expense e JOIN account a ON a.id = e.to_account ";
	sc += " WHERE " + sqlExpense;
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND e.branch = "+ m_branchID;
	}
	sc += " AND e.tax >= 0 ";
	sc += " GROUP BY a.name4, a.name1 ";
	sc += " ORDER BY total desc ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "expense");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}


	DataRow dr = ds.Tables["sales"].Rows[0];
	double dSales = MyDoubleParse(dr["sales_income"].ToString());
	double dCost = MyDoubleParse(dr["sales_cost"].ToString());
	double dStockLoss = MyDoubleParse(dr["stock_loss"].ToString());
	double dFreight = MyDoubleParse(dr["freight"].ToString());
//	double dStockcost = MyDoubleParse(dr["stock_cost"].ToString());
	double dPfreight = MyDoubleParse(dr["pfreight"].ToString());
	double dOthers = MyDoubleParse(dr["other_income"].ToString());	
	double dGrossProfit = dSales + dFreight + dOthers - dCost - dStockLoss ;
	

	Response.Write("<br><table width=400  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=2><h5><i>" + m_sCompanyTitle + "</i></h5></td></tr>");
	Response.Write("<tr><td colspan=2 align=center><h3>" + m_tableTitle + "</h3></td></tr>");
	Response.Write("<tr><td colspan=2 align=center><b>Date Period : " + m_datePeriod + "</b></td></tr>");


	Response.Write("<tr><td>" + DateTime.Now.ToString("dd-MM-yy HH:mm") + "</td></tr>");
	Response.Write("<tr><td colspan=2><hr></td></tr>");
	
/*	Response.Write("<tr><td colspan=2><b>PURCHASE</b></td></tr>");
	Response.Write("<tr><td><blockquote><p>Stock Cost </td><td align=right>" + dStockcost.ToString("c") + " &nbsp&nbsp;</p></blockquote></td></tr>");
	Response.Write("<tr><td><blockquote><p>Freight </td><td align=right>" + dPfreight.ToString("c") + " &nbsp&nbsp;</p></blockquote></td></tr>");
	Response.Write("<tr><td><b>Total Stock On Hand </b></td><td align=right>" + (dStockcost + dPfreight).ToString("c") + "</td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	*/
	Response.Write("<tr><td colspan=2><b>INCOME</b></td></tr>");
	Response.Write("<tr><td><blockquote><p>Sales Income </td><td align=right>" + dSales.ToString("c") + " &nbsp&nbsp;</p></blockquote></td></tr>");
	Response.Write("<tr><td><blockquote><p>Other Income </td><td align=right>" + dOthers.ToString("c") + " &nbsp&nbsp;</p></blockquote></td></tr>");
	Response.Write("<tr><td><blockquote><p>Freight </td><td align=right>" + dFreight.ToString("c") + " &nbsp&nbsp;</p></blockquote></td></tr>");
	Response.Write("<tr><td><b>Total Income </b></td><td align=right>" + (dSales + dFreight + dOthers).ToString("c") + "</td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2><b>COST of SALES</b></td></tr>");
	Response.Write("<tr><td><blockquote><p>Cost of Sales </td><td align=right>" + dCost.ToString("c") + " &nbsp&nbsp;</p></blockquote> </td></tr>");
	Response.Write("<tr><td><blockquote><p>Stock Loss </td><td align=right>" + dStockLoss.ToString("c") + " &nbsp&nbsp;</p></blockquote> </td></tr>");
	Response.Write("<tr><td><b>Total Cost of Sales </b></td><td align=right>" + (dCost + dStockLoss).ToString("c") + "</td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td><b>GROSS PROFIT </b></td><td align=right>" + dGrossProfit.ToString("c") + "</td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2><b>EXPENSE</b></td></tr>");

	double dTotalExpense = 0;
	for(i=0; i<ds.Tables["expense"].Rows.Count; i++)
	{
		dr = ds.Tables["expense"].Rows[i];
		double dTotal = MyDoubleParse(dr["total"].ToString());
		string type = dr["type"].ToString();
//DEBUG("type = ", type);
		if(type.IndexOf("GST") >= 0)
			continue;
		dTotalExpense += dTotal;

		Response.Write("<tr><td><blockquote><p>" + type + " </td><td align=right>" + dTotal.ToString("c") + "</p></blockquote> </td></tr>");
	}
	Response.Write("<tr><td><blockquote><p>Other Expenses</td><td align=right></p></blockquote> </td></tr>");
	Response.Write("<tr><td><b>Total Expense </b></td><td align=right>" + dTotalExpense.ToString("c") + "</td></tr>");

	double dProfit = (dSales + dFreight + dOthers )- dCost - dStockLoss - dTotalExpense;

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td><b>Operating Profit </b></td><td align=right>" + dProfit.ToString("c") + "</td></tr>");
	
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
//	Response.Write("<tr><td><b>Other Income </b></td><td align=right>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
//	Response.Write("<tr><td><b>Other Expense </b></td><td align=right>&nbsp;</td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td><b>Net Profit / (Loss) </b></td><td align=right>" + dProfit.ToString("c") + "</td></tr>");
	Response.Write("</table>");

	return true;
}

bool DoReceivedPaySummary()
{

	string IsPurchase = "1";
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, td.trans_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, td.trans_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, td.trans_date, GETDATE()) >= 1 AND DATEDIFF(month, td.trans_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND td.trans_date >= '" + m_sdFrom + "' AND td.trans_date <= '" + m_sdTo + " 23:59"+"'  ";
		break;
	default:
		break;
	}

/*	string sc = " SET DATEFORMAT dmy  SELECT isnull(t.amount_applied, ts.amount) as total, ISNULL(td.currency_loss,0) AS currency_loss, ISNULL(td.finance,0) AS finance, ISNULL(td.credit, 0) AS credit, t.tran_id";
	sc += " , ISNULL(t.invoice_number,0) AS invoice_number, c.name AS staff, c1.company, c1.id AS card_id ";
	sc += ", e.name AS payment_method ";
	sc += ", td.trans_date, Isnull(t.purchase,0) As purchase ";
	sc += " FROM trans ts JOIN tran_detail td ON ts.id = td.id ";
	sc += " LEFT OUTER JOIN tran_invoice t ON t.tran_id = td.id AND ts.id = t.tran_id ";
	sc += " JOIN card c ON c.id = td.staff_id ";
	sc += " JOIN card c1 ON c1.id = td.card_id ";
	sc += " JOIN enum e ON e.class='payment_method' AND e.id = td.payment_method ";
	sc += " WHERE 1= 1";
	sc += m_dateSql;
	if(Request.Form["pr_option"] != null && Request.Form["pr_option"] != "" && Request.Form["pr_option"] != "all")
		sc += " AND t.purchase = "+ Request.Form["pr_option"];
//	sc += " GROUP BY t.tran_id, t.invoice_number, c.name, c1.company, e.name, td.trans_date, c1.id , t.purchase  ";
	sc += " ORDER BY td.trans_date ";
	*/

	//string sc = " SET DATEFORMAT dmy SELECT e.name AS payment_method, ISNULL(t.amount_applied, ts.amount) AS total ";
	string sc = " SET DATEFORMAT dmy SELECT e.name AS payment_method, ISNULL(t.amount_applied, 0) AS total ";
	sc += " , ISNULL(td.currency_loss,0) AS currency_loss, ISNULL(td.finance,0) AS finance";
	sc += ", ISNULL(td.credit, 0) AS credit ";
	sc += ", ISNULL(ct.amount_applied, 0) AS credit_applied, t.tran_id";
	sc += ", td.trans_date, ISNULL(t.purchase,0) As purchase ";
	sc += " , ISNULL(t.invoice_number,0) AS invoice_number, c.name AS staff, c1.company, c1.id AS card_id ";
	sc += " FROM trans ts JOIN  tran_detail td ON ts.id = td.id LEFT OUTER JOIN ";
	//sc += " FROM trans ts JOIN  tran_detail td ON ts.id = td.id JOIN ";
	sc += " tran_invoice t ON t.tran_id = td.id AND ts.id = t.tran_id ";
	sc += " JOIN enum e ON e.class='payment_method' AND e.id = td.payment_method ";
	sc += " JOIN card c ON c.id = td.staff_id ";
	sc += " JOIN card c1 ON c1.id = td.card_id ";
	sc += " LEFT OUTER JOIN credit ct ON ct.tran_id = td.id ";
	sc += " WHERE 1=1 ";
	
//DEBUG("pr otpion =", Request.Form["pr_option"].ToString());
	if(Request.Form["pr_option"] != null && Request.Form["pr_option"] != "" && Request.Form["pr_option"] != "all")
	{
		sc += " AND (t.purchase = "+ Request.Form["pr_option"];
		if(Request.Form["pr_option"].ToString() == "0")
			sc += " OR t.purchase IS NULL ";
		sc += ")"; // OR t.purchase = "+ Request.Form["pr_option"] +") ";
	}
	
	sc += m_dateSql;
//	sc += " GROUP BY e.name "; // , t.purchase ";
	sc += " ORDER BY td.trans_date ";
//DEBUG("sc = ", sc);
	int rows = 0;
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
//DEBUG("rows = ",rows);
	Response.Write("<br><center><h4>PAY PURCHASE/RECEIVED MONEY REPORT</h4></center>");
	if(rows <=0)
	{
		Response.Write("<br><center><h4>No Records...");
		Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a>");
		return false;
	}
	if(rows > 0)
	{

		Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	//	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//	Response.Write("<th>&nbsp;</th>");
		//	Response.Write("</tr>");
		Response.Write("<tr  style=\"color:white;background-color:#666696;font-weight:bold;\" align=left>");
		Response.Write("<th>RECORD DATE</td><th>STAFF</td><th>COMPANY</td><th>PAY PURCHASE/RECEIVED MONEY</td><th>INVOICE#/PURCHASE#</td><th>PAYMENT METHOD</td>");
		Response.Write("<th>CREDIT</th><th>CREDIT-APPLIED</th><th>CURRENCY-LOSS</th><th>FINANCE</th>");
		Response.Write("<th>CR</td><th>DR</td></tr>");
		//for xml chart data
		string x = "";
		string y = "";
		string legend = "";

		StringBuilder sb1 = new StringBuilder();
		bool bAlter = false;
		double dTotal = 0;
		double dTotalPurchase = 0;
		double dTotalInvoice = 0;
		double dTotalCredit = 0;
		double dTotalFinance = 0;
		double dTotalCLoss = 0;
		double dTotalCredit_Applied = 0;
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["report"].Rows[i];	
			string staff = dr["staff"].ToString();
			string company = dr["company"].ToString();
			string invoice = dr["invoice_number"].ToString();
			string tran_id = dr["tran_id"].ToString();
			string total = dr["total"].ToString();
			string r_date = dr["trans_date"].ToString();
			string payment_method = dr["payment_method"].ToString();
			bool bIspurchase = bool.Parse(dr["purchase"].ToString());
			string card_id = dr["card_id"].ToString();
			string credit = dr["credit"].ToString();
			string credit_applied = dr["credit_applied"].ToString();
		
			total = (MyDoubleParse(total) + (MyDoubleParse(credit) - MyDoubleParse(credit_applied))).ToString();
		//	DEBUG("credit -applied = ", credit_applied);
			
			string finance = dr["finance"].ToString();
			string currency_loss = dr["currency_loss"].ToString();
			dTotalFinance += double.Parse(finance);
			dTotalCredit += double.Parse(credit);
			dTotalCredit_Applied += double.Parse(credit_applied);
			dTotalCLoss += double.Parse(currency_loss);
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
			Response.Write(">");
			bAlter = !bAlter;
			
			string stype = "";
			stype = "<font color=green>INVOICE</font>";
			if(bIspurchase) 
			{
				dTotalPurchase += MyDoubleParse(total);
				stype = "<font color=red>PURCHASE</font>";
			}
			else
				dTotalInvoice += MyDoubleParse(total); // - double.Parse(credit_applied);
			

			dTotal = dTotalInvoice - dTotalPurchase;
			
			Response.Write("<td>"+ DateTime.Parse(r_date).ToString("dd-MM-yyyy HH:mm") +" </td>");
			Response.Write("<td>"+ staff +" </td>");
			Response.Write("<td><a title='view supplier' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + card_id + "','', ' width=350,height=350'); viewcard_window.focus()\" class=o >");
			Response.Write(""+ company +" </td>");
			Response.Write("<td>"+ stype +"</td>");
			Response.Write("<td>");
			Response.Write("<a ");
			if(bIspurchase)
				Response.Write(" title='view purchase details' href='purchase.aspx?t=pp&n="+ invoice +"'");
			else
				Response.Write(" title='view invoice details' href='invoice.aspx?id="+ invoice +"'");
			Response.Write(" class=o target=new>");
			Response.Write(""+ invoice +" </a></td>");
			Response.Write("<td>"+ payment_method +" </td>");
			Response.Write("<td>"+ (MyDoubleParse(credit)).ToString("c") +" </td>");
			Response.Write("<td>"+ (MyDoubleParse(credit_applied)).ToString("c") +" </td>");
			Response.Write("<td>"+ (MyDoubleParse(currency_loss)).ToString("c") +" </td>");
			Response.Write("<td>"+ (MyDoubleParse(finance)).ToString("c") +" </td>");
			if(bIspurchase)
				Response.Write("<td>&nbsp;</td><td>"+ (MyDoubleParse(total)).ToString("c") +" </td>");
			else
				Response.Write("<td>"+ (MyDoubleParse(total)).ToString("c") +" </td><td>&nbsp;</td>");
				//Response.Write("<td>"+ (MyDoubleParse(total) - double.Parse(credit_applied)).ToString("c")  +" </td><td>&nbsp;</td>");
			Response.Write("</tr>");
			
		/*	//xml chart data
			x = (i).ToString();

			if(dSales > m_nMaxY)
				m_nMaxY = dSales;

			if(dSales < 0 && dSales < m_nMinY)
				m_nMinY = dSales;

			y = dSales.ToString();
			legend = month;
			sb2.Append("<chartdata>\r\n");
			sb2.Append("<x");
			if(m_bHasLegends)
				sb2.Append(" legend='" + legend + "'");
			sb2.Append(">" + x + "</x>\r\n");
			sb2.Append("<y>" + y + "</y>\r\n");
			sb2.Append("</chartdata>\r\n");
		*/
		}
					
		Response.Write("<tr align=left bgcolor=#EEEEE><th colspan=10>TOTAL RECEIEVED MONEY:</td><th colspan=2>"+ dTotalInvoice.ToString("c") +"</td></tr>");
		Response.Write("<tr align=left bgcolor=#EEEEE><th colspan=10>TOTAL PAY PURCHASE:</td><th align=right colspan=2>"+ dTotalPurchase.ToString("c") +"</td></tr>");
		Response.Write("<tr align=left bgcolor=#EEEEE><th colspan=6>SUB TOTAL:</td><th>"+ dTotalCredit.ToString("c") +"</th><th>"+ dTotalCredit_Applied.ToString("c") +"</th><th>"+ dTotalCLoss.ToString("c") +"</th><th>"+ dTotalFinance.ToString("c") +"</th><th colspan=2>"+ dTotal.ToString("c") +"</td></tr>");
		Response.Write("<tr align=left><td colspan=10><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a></td></tr>");
		Response.Write("</table>");
		Response.Write("<br><br>");
	/*
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb1.ToString());
		m_sb.Append("</chartdataisland>\r\n");
		
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb2.ToString());
		m_sb.Append("</chartdataisland>\r\n");

		m_IslandTitle[0] = "--Profit";
		m_IslandTitle[1] = "--Sales";
		m_nIsland = 2;
	*/
		
		//write xml data file for chart image
	//	WriteXMLFile();
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
	DataColumnCollection dc = ds.Tables["report"].Columns;
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

	for(i=0; i<ds.Tables["report"].Rows.Count; i++)
	{
		dr = ds.Tables["report"].Rows[i];
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

<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

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
string m_sPickMonthFrom = "";
string m_sPickMonthTo = "";
string m_sPickYearFrom = "";
string m_sPickYearTo = "";
int m_nMonthDiffer = 6;
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

string m_SortBy = "ASC";
string m_SortName = "s.code";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	m_uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate();
	m_bGBSetShowPicOnReport = MyBooleanParse(GetSiteSettings("set_display_chart_on_report", "1", true));

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
	if(Request.QueryString["sortby"] != null)
		m_SortBy = Request.QueryString["sortby"];
	//if(m_SortBy.ToLower() != "asc" || m_SortBy.ToLower() != "desc")
	//	m_SortBy = "ASC";	
	if(Request.QueryString["sortname"] != null)
		m_SortName = Request.QueryString["sortname"];
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
	string sYear = (DateTime.Now.Year).ToString();
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

		m_sPickMonthTo = Request.Form["Datepicker2_month"];
        m_sPickYearTo = Request.Form["Datepicker2_year"];
        m_sPickMonthFrom = Request.Form["Datepicker1_month"];
        m_sPickYearFrom = Request.Form["Datepicker1_year"];
	}
	if(Request.QueryString["monthdiffer"] != null && Request.QueryString["monthdiffer"] != "")
	{
		m_nMonthDiffer = int.Parse(Request.QueryString["monthdiffer"].ToString());
		m_sPickMonthTo = Request.QueryString["mto"];
		m_sPickYearTo = Request.QueryString["yto"];
		m_sPickMonthFrom = Request.QueryString["mfrm"];
		m_sPickYearFrom = Request.QueryString["yfrm"];
	}
	 m_nMonthDiffer = ((int.Parse(sYear) - int.Parse(m_sPickYearTo)) * 12) ;
    if(m_nMonthDiffer > 0)
          m_nMonthDiffer = (m_nMonthDiffer - int.Parse(m_sPickMonthTo) ) + int.Parse((DateTime.Now.Month).ToString()) + 6;        
    else
        m_nMonthDiffer = 6;
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
		DoSalesItemRP();
		break;
	case 1:
		DoPRSales();
		break;	
	case 3:		
		DoPRCustomers();
		break;
	case 4:
		DoItemCatagorySummary();
		break;	

	default:
		break;
	}

	PrintAdminFooter();
}

bool doShowEmployee()
{
	string sc = " SELECT DISTINCT name, trading_name, company, contact, phone, id ";
	sc += " FROM card ";
	sc += " WHERE type = 4";
	
	if(Session["branch_support"] != null)
	{
		//if(TSIsDigit(m_branchID) && m_branchID != "all")
		if(TSIsDigit(m_branchID) && m_branchID != "all"  && m_branchID != "0")
			sc += " AND our_branch = " + m_branchID;
	}
	if(m_sales_id != null && m_sales_id != "" && m_sales_id != "all")
		sc += " AND id = "+ m_sales_id +"";
	sc += " AND id IN (SELECT cc.sales FROM card cc WHERE type = 1 or type=2) ";
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
	
	Response.Write("<form name=f action="+ Request.ServerVariables["URL"] +" method=post>");
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
//		PrintBranchNameOptions(m_branchID, ""+ Request.ServerVariables["URL"] +"?branch=");
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
	Response.Write(" >Item Based: ");
	Response.Write(" &nbsp;&nbsp;&nbsp;<b>Select Item Code : </b><select name=code ");
	Response.Write(" onclick=\"window.location=('slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"')\" ");
	Response.Write(" >");
	Response.Write("<option value='all'>All");
	if(Session["slt_code"] != null)
		Response.Write("<option value='"+ Session["slt_code"] +"' selected> "+Session["slt_code"] +" </option>");
	Response.Write("</select>\r\n");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=type value=3 onclick=\"OnTypeChange(this.value);\"");
	Response.Write(" >Customer Based: ");	
	Response.Write("</td></tr>");

	//display report in brand, catagory
/*	Response.Write("<tr><td colspan=2><input type=radio name=type value=4 onclick=\"OnTypeChange(this.value);\"");
	Response.Write(">");
	DoItemOption();
	Response.Write("</td>");
	Response.Write("</tr>");
*/
Response.Write("<tr><td colspan=2><input type=radio name=type value=1 onclick=\"OnTypeChange(this.value);\" ");
/*	Response.Write("<tr><td colspan=2><input type=radio name=type value=1 onclick=\"window.location=('"+ uri +"&sltype=1');\" ");
	if(m_sType_Value == "1")
		Response.Write(" checked ");
*/
	Response.Write(" >Sales Manager Based: &nbsp;");
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

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>This Month</td></tr>");
//	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Last Month</td></tr>");
//	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>Last Three Months</td></tr>");
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
	
	
	//Response.Write("</td></tr>");

	Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");
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
bool DoSalesItemRP()
{
	m_tableTitle = "Item Based Report";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			m_tableTitle += " <br><font size=2>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=2>branch: ALL</font>";
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

	sc += " SELECT s.code, s.supplier_code, s.name, sum(s.commit_price * s.quantity) AS commit_price, s.commit_price AS sales_price  ";
	sc += ", sum(s.supplier_price * s.quantity) AS supplier_price, sum(s.quantity) AS sales_qty";
	
    int nCount = 6;
    while(nCount > 0)
	{
        sc += ", ISNULL((SELECT ";
        //sc += " SUM(ss.commit_price * ss.quantity) "; 
        sc += " SUM(ss.quantity)  ";        
        sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number ";        
		sc += " WHERE DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (m_nMonthDiffer - nCount).ToString() +"  ";
		sc += " AND ss.code = s.code AND ss.commit_price = s.commit_price ";	
		sc += " ),'0') AS '"+ (m_nMonthDiffer - nCount).ToString() +"' ";
        nCount--;

	}
	sc += " FROM sales s ";
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

	sc += " GROUP BY s.commit_price, s.code, s.supplier_code, s.name  ";
	sc += " ORDER BY "+ EncodeQuote(m_SortName) +" "+ EncodeQuote(m_SortBy) +"";
	//sc += " ORDER BY sum(s.quantity) DESC ";
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
	if(ds.Tables["report"].Rows.Count <= 0 )
	{
		Response.Write("<br><center><h4>No Records...");
		Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><br>");
		return false;
	}
	
	BindPRItem();
	
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
	m_cPI.URI = "?np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	if(m_sdTo != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	m_cPI.URI += "&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom;
	if(m_branchID != null && m_branchID != "")
		m_cPI.URI += "&branch="+ m_branchID;
	if(m_SortBy.ToLower() == "desc")
		m_cPI.URI += "&sortby=asc";
	else
		m_cPI.URI += "&sortby=desc";
m_cPI.URI += "&monthdiffer="+ m_nMonthDiffer;
	m_cPI.PageSize = 50;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	//DEBUG(" rows = ", rows );
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<th>Date</th>");
	Response.Write("<th align=left><a title='sort by code' href='"+ m_cPI.URI +"&sortname=s.code");	
	Response.Write("'><font color=white>CODE</a></th>");
//	Response.Write("<th>BARCODE</th>");
	Response.Write("<th align=left><a title='sort by supplier code' href='"+ m_cPI.URI +"&sortname=s.supplier_code'><font color=white>M_PN</a></th>");
	Response.Write("<th align=left><a title='sort by name' href='"+ m_cPI.URI +"&sortname=s.name'><font color=white>DESCRIPTION</a></th>");
	Response.Write("<th align=left><a title='sort by sales price' href='"+ m_cPI.URI +"&sortname=s.commit_price'><font color=white>SALES PRICE</a></th>");
	string sMonth = (DateTime.Now.Month).ToString();
	string sYear = (DateTime.Now.Year).ToString();
	if(m_nPeriod == 3)
	{
		sMonth = m_sPickMonthTo;
	}
	int nSwapMonth = int.Parse(sMonth);        
       nSwapMonth--;
	for(int j =5 ;j>=0; j--)
	{
         if(nSwapMonth < 0)
            nSwapMonth = 11;

		Response.Write("<th width='3%'align=center> "+ m_EachMonth[nSwapMonth] +"</th>");
        nSwapMonth--;
	}
	Response.Write("<th>TOTAL SALES QTY</th>");
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
	int[] nEachSubTotalQTY = new int[7];
	for(; i<rows && i<end; i++)
	{
		int nEachTotalQTY = 0;

		DataRow dr = ds.Tables["report"].Rows[i];
		//string date = dr["price_age"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sales_price = dr["sales_price"].ToString();
		string sales_qty = dr["sales_qty"].ToString();
//		string sales_amount = dr["sales_amount"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();
		//get the sum of total sales price which is different from invoice's price
//		string total_commit = dr["total_commit"].ToString(); 
		
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dCost = MyDoubleParse(cost);
		double dProfit = dCommitPrice - dCost;

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

//		dQtyTotal += dQTY;
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
		Response.Write("<td><a title='View Sales Item Details' href='rpsum_d.aspx?period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom);			
		Response.Write("&monthdiffer="+ m_nMonthDiffer);
		if(m_branchID != null && m_branchID != "")
			Response.Write("&branch="+ m_branchID);
		Response.Write("&code="+ code +"");
		Response.Write("' target=_blank class=o >" + code + "</a></td>");
		//Response.Write("" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td><a title='View Sales Item Details' href='rpsum_d.aspx?period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom);			
		Response.Write("&monthdiffer="+ m_nMonthDiffer);
		Response.Write("&code="+ code +"");
		Response.Write("'  target=_blank  class=o >" + name + "</td>");
		Response.Write("<td>" + double.Parse(sales_price).ToString("c") + "</td>");
		int nCount = 6;
		while(nCount > 0 )
		{
            //Response.Write("<td width='2%'align=center>"+  double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString()).ToString("c") +"</td>");
            Response.Write("<td width='2%'align=center>"+ dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString() +"</td>");            
			nEachTotalQTY += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nEachSubTotalQTY[nCount] += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nCount--;
		}
		sales_qty = nEachTotalQTY.ToString();
		dQtyTotal += nEachTotalQTY;
		/*Response.Write("<td align=center><a href='report.aspx?type=1&code=" + code + "&period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("' class=o title='Click to view details'>" + sales_qty + "</a></td>");
		*/
		Response.Write("<td align=center>" + sales_qty + "</td>");
//		Response.Write("<td align=center><a href=rpsum_c.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=center><a href="+ Request.ServerVariables["URL"] +"?type=0&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
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
	Response.Write("<td></td>");
	int nCounter = 6;
	while(nCounter > 0 )
	{
		Response.Write("<td align=center>"+ nEachSubTotalQTY[nCounter] +"</td>");
		nEachSubTotalQTY[nCounter] = 0;
		nCounter--;
	}
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
		string cost = dr["supplier_price"].ToString();
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
		int nCount = 6;
		int nEachTotalQTY = 0;
		while(nCount > 0 )
		{			
			nEachTotalQTY += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nEachSubTotalQTY[nCount] += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());			
			nCount--;
		}
		dQtyTotal += nEachTotalQTY;
		dMargin = Math.Round(dMargin, 4);
	//	dQtyTotal += dQTY;
		dSalesTotal += dCommitPrice;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;
		dGPTotal += dProfitTotal / dSalesTotal;
	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td></td>");
	nCounter = 6;
	while(nCounter > 0 )
	{
		Response.Write("<td align=center>"+ nEachSubTotalQTY[nCounter] +"</td>");
		nEachSubTotalQTY[nCounter] = 0;
		nCounter--;
	}
	Response.Write("<td align=middle style=\"font-size:14\">" + dQtyTotal.ToString() + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + (dProfitTotal/dSalesTotal).ToString("p") + "</td>");
//	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");
	Response.Write("<tr><td colspan=7><a href='"+ Request.ServerVariables["URL"] +"'>New Report</a></td></tr>");
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
	{
	sc += " SELECT  sum(s.supplier_price * s.quantity) AS gross_cost, sum(s.commit_price * s.quantity) AS gross_sales, sum(s.quantity) AS sales_qty ";
	sc += " , sum(s.commit_price * s.quantity) - sum(s.supplier_price * s.quantity) AS profit ";
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

	Response.Write("<form action="+ Request.ServerVariables["URL"] +" method=post>");
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
	m_cPI.URI = "?np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
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
	if(m_branchID != null && m_branchID != "")
		m_cPI.URI += "&branch="+ m_branchID;

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
		//Response.Write("<td align=center><a href="+ Request.ServerVariables["URL"] +"?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
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

}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Sales Person based
bool DoPRSales()
{
	m_tableTitle = "Sales Manger Report for ";
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


	sc += " SELECT SUM(i.price) AS sales_amount, SUM(i.freight) AS freight ";
	sc += ",(  ";
	sc += "SELECT SUM(ISNULL(s.supplier_price, s.commit_price) * s.quantity) ";
	sc += " FROM sales s INNER JOIN ";
	sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
	sc += " orders oo ON oo.invoice_number = s.invoice_number ";
	//sc += " WHERE ISNULL(oo.sales_manager,oo.sales) = ISNULL(o.sales_manager,o.sales) ";
	sc += " WHERE oo.sales_manager = o.sales_manager ";

	sc += m_dateSql;
	sc += " AND i.branch = " + m_branchID + " ) AS gross_profit ";	
	//sc += " , isnull(c.name,'Online Orders') AS name,  ISNULL(o.sales_manager, ISNULL(o.sales, 0)) AS id ";
	sc += " , isnull(c.name,'Online Orders') AS name,  o.sales_manager AS id ";
	int nCount = 6;
    while(nCount > 0)
	{
        sc += ", ISNULL((SELECT ";
        sc += " SUM(ss.commit_price * ss.quantity) ";              
        sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number ";        
		sc += " JOIN orders oo ON oo.invoice_number = ii.invoice_number ";
		sc += " WHERE DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (m_nMonthDiffer - nCount).ToString() +"  ";
		//sc += " AND ISNULL(oo.sales_manager, ISNULL(oo.sales, 0)) = ISNULL(o.sales_manager, ISNULL(o.sales, 0))  ";	
		sc += " AND oo.sales_manager = o.sales_manager  ";	
		sc += " ),'0') AS '"+ (m_nMonthDiffer - nCount).ToString() +"' ";
        nCount--;
	}

	sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
	//sc += " LEFT OUTER JOIN card c ON c.id = ISNULL(o.sales_manager, o.sales) ";
	sc += " LEFT OUTER JOIN card c ON c.id = o.sales_manager ";
//	sc += " WHERE 1=1 ";
	sc += " WHERE o.sales_manager IS NOT NULL ";
	if(m_sales_id != "all" && m_sales_id != "")
		sc += " AND o.sales_manager = "+ m_sales_id;
	if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
	{
		sc += " AND (i.branch = " + m_branchID + " ) ";
	}
	sc += m_dateSql;

//	sc += " GROUP BY o.sales_manager, c.name, c.id , o.sales ";
	sc += " GROUP BY o.sales_manager, c.name ";
	sc += " ORDER BY o.sales_manager ";
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

		
//DEBUG("sc =", sc);

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

	return true;
}

/////////////////////////////////////////////////////////////////
void BindPRSales()
{
	int i = 0;
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 2000;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	m_cPI.URI += "&np="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
	m_cPI.URI += "&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom;
	if(m_SortBy.ToLower() == "desc")
		m_cPI.URI += "&sortby=asc";
	else
		m_cPI.URI += "&sortby=desc";
	if(m_branchID != null && m_branchID != "")
		m_cPI.URI += "&branch="+ m_branchID;
	m_cPI.URI += "&monthdiffer="+ m_nMonthDiffer;

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>SALES MANAGER</th>");
//	Response.Write("<th align=right><font color=white>"+ m_datePeriod +"</th>");
	string sMonth = (DateTime.Now.Month).ToString();
	string sYear = (DateTime.Now.Year).ToString();
	if(m_nPeriod == 3)
	{
		sMonth = m_sPickMonthTo;
	}
	int nSwapMonth = int.Parse(sMonth);        
       nSwapMonth--;
	for(int j =5 ;j>=0; j--)
	{
         if(nSwapMonth < 0)
            nSwapMonth = 11;

		Response.Write("<th width='10%'align=right> "+ m_EachMonth[nSwapMonth] +"</th>");
        nSwapMonth--;
	}
	Response.Write("<th align=right>TOTAL SALES AMOUNT</th>");
//	Response.Write("<th>PROFIT GENERATED</th>");
	
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

double [] dEachSubTotalPrice  = new double[7];
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		
		string sales_id = dr["id"].ToString();
		string sales = dr["name"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		double dSales = MyDoubleParse(sales_amount);
		//string profit = dr["gross_profit"].ToString();
		string profit = "0";
		profit = dr["gross_profit"].ToString();
	
//DEBUG("profit = ", profit);
		
//		DEBUG("profit = ", profit);
		double dProfit = MyDoubleParse(profit);
//			DEBUG("sales =", sales_amount);
//DEBUG("profit =", profit);
		dProfit = dSales - (dProfit);	
//		if(dSales == 0)
//			continue;
		dFreightTotal += MyDoubleParse(dr["freight"].ToString());
	//	dSalesTotal += dSales;
		dProfitTotal += dProfit;
		dAllTotal += dSales + MyDoubleParse(dr["freight"].ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
	/*	Response.Write("<form action='rpsum_c.aspx?s=" + sales_id + "");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"\r\n");
		Response.Write("' ");
		Response.Write(" method=post target=new>");
	*/	
		Response.Write("<td><a title='View Customer with This Sales Manager Report' href='rpsum_c.aspx?custgrp=1&period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom);			
		Response.Write("&monthdiffer="+ m_nMonthDiffer);
		if(m_branchID != null && m_branchID != "")
			Response.Write("&branch="+ m_branchID);
		Response.Write("&salesid="+ sales_id +"");
		Response.Write("' target=_blank class=o ><font color=green><b>VC</b></font></a>  &nbsp;&nbsp;");	

		Response.Write("<a title='View Details Report' href='rpsum_c.aspx?period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom);			
		Response.Write("&monthdiffer="+ m_nMonthDiffer);
		if(m_branchID != null && m_branchID != "")
			Response.Write("&branch="+ m_branchID);
		Response.Write("&salesid="+ sales_id +"");
		Response.Write("' target=_blank class=o >" + sales + "</a></td>");					
		
		int nCount = 6;
		double dEachSubTotal = 0;
	//	Response.Write("<td align=right>"+ dSales.ToString("c") +"</td>");
		while(nCount > 0 )
		{            
            Response.Write("<td width='10%'align=right>"+ double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString()).ToString("c") +"</td>");            
			dEachSubTotal += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			dEachSubTotalPrice[nCount] += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			
			nCount--;
		}
		dSales = dEachSubTotal;
		dSalesTotal += dSales;
//			<a href=rpsum_c.aspx?s=" + sales_id + " class=o title='View Details'>" + sales + "</a></td>");
		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
//		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		Response.Write("</tr>");		
	}
			//total
		Response.Write("<tr></tr>");
		Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
		Response.Write("<td style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
		int nCounter = 6;
		while(nCounter > 0 )
		{
			Response.Write("<td align=right>"+ (dEachSubTotalPrice[nCounter]).ToString("c") +"</td>");
			//dSalesTotal += dEachSubTotalPrice[nCounter];
			dEachSubTotalPrice[nCounter] = 0;
			nCounter--;
		}		
		Response.Write("<td align=right style=\"font-size:13\">" + dSalesTotal.ToString("c") + "</td>");
//		Response.Write("<td align=right nowrap style=\"font-size:13\">" + dProfitTotal.ToString("c") + "</td>");
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
		
			double dProfit = MyDoubleParse(profit);
			dProfit = dSales - (dProfit);	
			if(dSales == 0)
				continue;
			int nCount = 6;
			double dEachSubTotal = 0;
			while(nCount > 0 )
			{	
				dEachSubTotal += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
				dEachSubTotalPrice[nCount] += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());			
				nCount--;
			}
			dSalesTotal += dEachSubTotal;

			//dSalesTotal += dSales;
			dProfitTotal += dProfit;
			
		}
		Response.Write("<tr align=right style=\"color:black;background-color:#EEE54E;\">");
		Response.Write("<td align=left style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
		nCounter = 6;
		while(nCounter > 0 )
		{
			Response.Write("<td align=right>"+ (dEachSubTotalPrice[nCounter]).ToString("c") +"</td>");
			dEachSubTotalPrice[nCounter] = 0;
			nCounter--;
		}
		Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	//	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	
	//total
	
	Response.Write("<tr><td colspan=3>" + sPageIndex + "</td></tr>");
	Response.Write("<tr><td colspan=3><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><td></tr>");

	Response.Write("</table>");

/*	Response.Write("<br><center><h4>");
	
	Response.Write("<b>Total Freight : </b><font color=red>" + dFreightTotal.ToString("c") + "</font>&nbsp&nbsp;");
	Response.Write("<b>Total Sales : </b><font color=red>" + dSalesTotal.ToString("c") + "</font>&nbsp;");
	Response.Write("<b>Sub Total : </b><font color=red>" + (dSalesTotal + dFreightTotal).ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</center></h4>");
*/
	//write xml data file for chart image
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Customers based
bool DoPRCustomers()
{
	m_tableTitle = "Customer Purchase Report ";
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
	sc += " SELECT 0 AS sales_amount, SUM(i.freight) AS freight ";
	sc += ",(  ";
	/*sc += "SELECT SUM(ISNULL(s.supplier_price, s.commit_price) * s.quantity) ";
	sc += " FROM sales s INNER JOIN ";
	sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
	sc += " orders oo ON oo.invoice_number = s.invoice_number ";
	sc += " WHERE i.card_id = oo.card_id ";

	sc += m_dateSql;
	sc += " AND i.branch = " + m_branchID + " ) AS gross_profit ";	
	*/
	sc += " 0 ) AS gross_profit ";
	sc += " , isnull(c.name,'Online Orders') AS name, c.company, c.trading_name,  i.card_id AS id ";
	int nCount = 6;
    while(nCount > 0)
	{
        sc += ", ISNULL((SELECT ";
        sc += " SUM(ss.commit_price * ss.quantity) ";              
        sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number ";        
		sc += " JOIN orders oo ON oo.invoice_number = ii.invoice_number ";
		sc += " WHERE DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (m_nMonthDiffer - nCount).ToString() +"  ";
		sc += " AND ii.card_id = i.card_id ";	
		sc += " ),'0') AS '"+ (m_nMonthDiffer - nCount).ToString() +"' ";
        nCount--;
	}

	sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id = i.card_id ";
	//sc += " WHERE o.sales_manager IS NOT NULL ";	
	sc += " WHERE 1=1 ";	
	if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
	{
		sc += " AND (i.branch = " + m_branchID + " ) ";
	}
	sc += m_dateSql;

	sc += " GROUP BY i.card_id, c.name, c.id , c.trading_name, c.company ";
	if(m_SortName == "s.code")
		m_SortName = "i.card_id ";
	sc += " ORDER BY "+ m_SortName +" "+ m_SortBy +" ";
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

		
//DEBUG("sc =", sc);

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	if(ds.Tables["report"].Rows.Count <= 0 )
	{
		Response.Write("<br><center><h4>No Records...");
		Response.Write("<br><h4><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><br>");
		return false;
	}

	BindPRCustomers();

	return true;
}

/////////////////////////////////////////////////////////////////
void BindPRCustomers()
{
	int i = 0;
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 2000;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	m_cPI.URI += "&np="+ m_nPeriod +"&type="+ m_type +"";
	m_cPI.URI += "&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom;
	if(m_SortBy.ToLower() == "desc")
		m_cPI.URI += "&sortby=asc";
	else
		m_cPI.URI += "&sortby=desc";
	m_cPI.URI += "&monthdiffer="+ m_nMonthDiffer;
	if(m_branchID != null && m_branchID != "")
		m_cPI.URI += "&branch="+ m_branchID;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=98%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<th align=left><a title='sort by name' href='"+ m_cPI.URI +"&sortname=c.name'><font color=white>NAME</a></th>");
	//Response.Write("<th align=left><a title='sort by trading name' href='"+ m_cPI.URI +"&sortname=c.trading_name'><font color=white>TRADING NAME</a></th>");
	Response.Write("<th align=left>NAME</a></th>");
	Response.Write("<th align=left>TRADING NAME</a></th>");
//	Response.Write("<th align=right><font color=white>"+ m_datePeriod +"</th>");
	string sMonth = (DateTime.Now.Month).ToString();
	string sYear = (DateTime.Now.Year).ToString();
	if(m_nPeriod == 3)
	{
		sMonth = m_sPickMonthTo;
	}
	int nSwapMonth = int.Parse(sMonth);        
       nSwapMonth--;
	for(int j =5 ;j>=0; j--)
	{
         if(nSwapMonth < 0)
            nSwapMonth = 11;

		Response.Write("<th width='10%'align=right> "+ m_EachMonth[nSwapMonth] +"</th>");
        nSwapMonth--;
	}
	Response.Write("<th align=right>TOTAL SALES AMOUNT</th>");
//	Response.Write("<th>PROFIT GENERATED</th>");
	
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

double [] dEachSubTotalPrice  = new double[7];
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		
		string sales_id = dr["id"].ToString();
		string company = dr["name"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		double dSales = MyDoubleParse(sales_amount);
		//string profit = dr["gross_profit"].ToString();
		string profit = "0";
		profit = dr["gross_profit"].ToString();
	
//DEBUG("profit = ", profit);
		
//		DEBUG("profit = ", profit);
		double dProfit = MyDoubleParse(profit);
//			DEBUG("sales =", sales_amount);
//DEBUG("profit =", profit);
		dProfit = dSales - (dProfit);	
//		if(dSales == 0)
//			continue;
		dFreightTotal += MyDoubleParse(dr["freight"].ToString());
	//	dSalesTotal += dSales;
		dProfitTotal += dProfit;
		dAllTotal += dSales + MyDoubleParse(dr["freight"].ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
	/*	Response.Write("<form action='rpsum_c.aspx?s=" + sales_id + "");
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
		Response.Write("<input type=submit name=cmd value='" + company + "' title='View Details' " + Session["button_style"] + ">");
		Response.Write("</td>");
		Response.Write("</form>")
		*/
		Response.Write("<td><a title='View Details Report' href='rpsum_c.aspx?period="+ m_nPeriod +"");
		if(m_nPeriod == 3)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom);			
		Response.Write("&monthdiffer="+ m_nMonthDiffer);
		if(m_branchID != null && m_branchID != "")
			Response.Write("&branch="+ m_branchID);
		Response.Write("&custid="+ sales_id +"");
		Response.Write("' target=_blank class=o >" + company + "</a></td>");
		//Response.Write("<td>");
		Response.Write("<td align=left><a title='view customer details' ");
		Response.Write(" href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + sales_id + "', '',");
		Response.Write("'width=450, height=450'); viewcard_window.focus()\" class=o>" + trading_name + "</a></td>");
		int nCount = 6;
		double dEachSubTotal = 0;
	//	Response.Write("<td align=right>"+ dSales.ToString("c") +"</td>");
		while(nCount > 0 )
		{            
            Response.Write("<td width='10%'align=right>"+ double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString()).ToString("c") +"</td>");            
			dEachSubTotal += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			dEachSubTotalPrice[nCount] += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			
			nCount--;
		}
		dSales = dEachSubTotal;
		dSalesTotal += dSales;
//			<a href=rpsum_c.aspx?s=" + sales_id + " class=o title='View Details'>" + sales + "</a></td>");
		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
//		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
		Response.Write("</tr>");		
		
	}
	

		//total
		Response.Write("<tr></tr>");
		Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
		Response.Write("<td colspan=2 style=\"font-size:13\"><b>SUB Total : &nbsp; </b></td>");
		int nCounter = 6;
		while(nCounter > 0 )
		{
			Response.Write("<td align=right>"+ (dEachSubTotalPrice[nCounter]).ToString("c") +"</td>");
			//dSalesTotal += dEachSubTotalPrice[nCounter];
			dEachSubTotalPrice[nCounter] = 0;
			nCounter--;
		}		
		Response.Write("<td align=right style=\"font-size:13\">" + dSalesTotal.ToString("c") + "</td>");
//		Response.Write("<td align=right nowrap style=\"font-size:13\">" + dProfitTotal.ToString("c") + "</td>");
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
		
			double dProfit = MyDoubleParse(profit);
			dProfit = dSales - (dProfit);	
			if(dSales == 0)
				continue;
			int nCount = 6;
			double dEachSubTotal = 0;
			while(nCount > 0 )
			{	
				dEachSubTotal += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
				dEachSubTotalPrice[nCount] += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());			
				nCount--;
			}
			dSalesTotal += dEachSubTotal;

			//dSalesTotal += dSales;
			dProfitTotal += dProfit;
			
		}
		Response.Write("<tr align=right style=\"color:black;background-color:#EEE54E;\">");
		Response.Write("<td align=left colspan=2  style=\"font-size:14\"><b>GRAND TOTAL : &nbsp; </b></td>");
		nCounter = 6;
		while(nCounter > 0 )
		{
			Response.Write("<td align=right>"+ (dEachSubTotalPrice[nCounter]).ToString("c") +"</td>");
			dEachSubTotalPrice[nCounter] = 0;
			nCounter--;
		}
		Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	//	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	
	//total
	
	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
	Response.Write("<tr><td colspan=6><a title='new report' href='"+ Request.ServerVariables["URL"] +"' class=o>New Report</a><td></tr>");

	Response.Write("</table>");

/*	Response.Write("<br><center><h4>");
	
	Response.Write("<b>Total Freight : </b><font color=red>" + dFreightTotal.ToString("c") + "</font>&nbsp&nbsp;");
	Response.Write("<b>Total Sales : </b><font color=red>" + dSalesTotal.ToString("c") + "</font>&nbsp;");
	Response.Write("<b>Sub Total : </b><font color=red>" + (dSalesTotal + dFreightTotal).ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</center></h4>");
	*/
	//write xml data file for chart image
}
string GetBranchName(string id)
{
	if(ds.Tables["branch_name"] != null)
		ds.Tables["branch_name"].Clear();

	string sc = " SELECT name FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "branch_name") == 1)
			return ds.Tables["branch_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}	


</script>

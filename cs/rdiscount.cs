<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

<!-- #include file="page_index.cs" -->
<script runat=server>

string m_branchID = "-1";
string m_uri = ""; 
string m_type = "0";
string m_tableTitle = "Discount Report";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sales_person = "";
string m_sales_id = "-1";

string m_sType_Value = "0";

string m_sdFrom = "";
string m_sdTo = "";
string m_syFrom = DateTime.Now.ToString("yyyy");
string m_syTo = DateTime.Now.ToString("yyyy");
int m_nPeriod = 0;

bool m_bShowPic = false;
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

string m_cols = "7";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	m_uri = Request.ServerVariables["URL"];

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
	m_brand = Request.Form["brand"];
	m_cat = Request.Form["cat"];
	m_scat = Request.Form["scat"];
	m_sscat = Request.Form["sscat"];

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
		{
			m_sales_id = "-1";
			m_sales_person = "all";
		}
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
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "This month";
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
//	case 1:
//		DoPRSales();
//		break;
//	case 2:
//		DoPRMonthly();
//		break;
//	case 3:
//		DoProfitStatement();
//		break;
//	case 4:
//		DoItemCatagorySummary();
//		break;
//	case 5:
//		DoReceivedPaySummary();
//		break;

	default:
		break;
	}

	PrintAdminFooter();
}

bool doShowEmployee()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id ";
	sc += " FROM card ";
	sc += " WHERE type = 4";
	if(Session["branch_support"] != null)
	{
		if(m_branchID != "-1")
			sc += " AND our_branch = " + m_branchID;
	}
	if(m_sales_id != null && m_sales_id != "" && m_sales_id != "-1" && m_sales_id != "all")
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
	string cat = "", s_cat = "", ss_cat = "", brand = "";
		if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"].ToString();
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
		s_cat = Request.QueryString["s_cat"].ToString();
	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
		ss_cat = Request.QueryString["ss_cat"].ToString();
	if(Request.QueryString["brand"] != null && Request.QueryString["brand"] != "")
		brand = Request.QueryString["brand"].ToString();

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
	Response.Write(" onchange=\"window.location=('" + m_uri + "?sltype="+ m_sType_Value +"");
	if(cat != "" && cat != null)
		Response.Write("&cat="+ HttpUtility.UrlEncode(cat) +"");
	if(s_cat != "" && s_cat != null)
		Response.Write("&s_cat="+ HttpUtility.UrlEncode(s_cat) +"");
	if(ss_cat != "" && ss_cat != null)
		Response.Write("&ss_cat="+ HttpUtility.UrlEncode(ss_cat) +"");
	Response.Write("&brand='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["brand"].Rows[i];
		string s = dr["brand"].ToString();
		if(brand.ToUpper() == s.ToUpper())
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
	Response.Write(" onchange=\"window.location=('" + m_uri + "?sltype="+ m_sType_Value +"");
	if(brand != "")
		Response.Write("&brand="+ brand +"");
	Response.Write("&r=" + DateTime.Now.ToOADate() + "&cat='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
//	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
//		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
	
		if(cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");
	}

	Response.Write("</select>");
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
	{
		cat = Request.QueryString["cat"].ToString();
	
		sc = "SELECT DISTINCT s_cat FROM product  WHERE cat = '"+ cat +"' ";
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
		Response.Write(" onchange=\"window.location=('" + m_uri + "?sltype="+ m_sType_Value +"");
		if(brand != "")
			Response.Write("&brand="+ brand +"");
		Response.Write("&cat="+ HttpUtility.UrlEncode(cat) +"&r=" + DateTime.Now.ToOADate() + "&s_cat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
//		if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
//			s_cat = Request.QueryString["s_cat"].ToString();
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
		
			//DEBUG(" scat = ", s_cat);
			if(s_cat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
		}

		Response.Write("</select>");
	}

	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
	{
		s_cat = Request.QueryString["s_cat"].ToString();
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM product p WHERE cat = '"+ cat +"' ";
		sc += " AND s_cat = '"+ s_cat +"' ";
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
		Response.Write(" onchange=\"window.location=('" + m_uri + "?sltype="+ m_sType_Value +"");
		if(brand != "")
			Response.Write("&brand="+ brand +"");
		Response.Write("&cat="+ HttpUtility.UrlEncode(cat) +"&r=" + DateTime.Now.ToOADate() + "&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
//		if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
//			ss_cat = Request.QueryString["ss_cat"].ToString();
		
		
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(ss_cat.ToUpper() == s.ToUpper())
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
	
	Response.Write("<form name=f action=rdiscount.aspx method=post>");
	Response.Write("<br><center><h3>Select Report</h3>");
	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
//	Response.Write("<td colspan=2><b>Report Type</b></td></tr>");
	
	string uri = m_uri.ToString();
/*	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ m_uri +"?sltype=0');\" value=0 ");
	if(m_sType_Value == "0")
		Response.Write(" checked");
	
	Response.Write(" >Item Based ");
	Response.Write(" &nbsp;&nbsp;&nbsp;<b>Select Item Code : </b><select name=code ");
	Response.Write(" onclick=\"window.location=('slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"')\" ");
	Response.Write(" >");
	if(Session["slt_code"] != null)
		Response.Write("<option value='"+ Session["slt_code"] +"'> "+Session["slt_code"] +" </option>");
	Response.Write("</select>\r\n");
	Response.Write("</td></tr>");

	//display report in brand, catagory
	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ m_uri +"?sltype=4');\" value=4 ");
	if(m_sType_Value == "4")
		Response.Write(" checked ");
	//Response.Write(">Sorted by: ");
	Response.Write(">");
	DoItemOption();
	Response.Write("</td>");
	Response.Write("</tr>");
*/
	//Response.Write("<tr><td colspan=2><input type=radio name=type value=1 >Sales Person Based &nbsp;");
	Response.Write("<tr><td colspan=2>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<b>Branch : </b>");
		PrintBranchNameOptionsWithAll(m_branchID, "rdiscount.aspx?branch=");
	}
	Response.Write("<input type=hidden name=type value=0 onclick=\"window.location=('"+ m_uri +"?sltype=1');\" ");
//	if(Request.QueryString["sltype"] == "1")
//		Response.Write(" checked ");
	Response.Write(" >Sales Person &nbsp;");
	Response.Write("<select name=employee> <option value='-1'>All");
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
/*	
	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ m_uri +"?sltype=2');\" value=2 ");
	if(m_sType_Value == "2")
		Response.Write(" checked ");
	Response.Write(">Gross Profit Chart</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ m_uri +"?sltype=3');\" value=3");
	if(m_sType_Value == "3")
		Response.Write(" checked");
	Response.Write(">Profit Statement</td></tr>");
	//received Money and Pay Purchase summary 
	Response.Write("<tr><td colspan=2><input type=radio name=type onclick=\"window.location=('"+ m_uri +"?sltype=5');\" value=5");
	if(m_sType_Value == "5")
		Response.Write(" checked");
	Response.Write(">");
	Response.Write("<select name=pr_option>");
	Response.Write("<option value='all'>Both</option>");
	Response.Write("<option value='1'>Pay Purchase Summary</option>");
	Response.Write("<option value='0'>Receive Money Summary</option>");
	Response.Write("</select>");
	Response.Write(" | Receive Money/Pay Purchase Summary</td></tr>");
	
*/
	//Response.Write("<tr><td>&nbsp;</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>This Month</td></tr>");
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
	if(m_sType_Value == "1")
	{
		Response.Write("<tr><td colspan=1><input type=radio name=period value=4 onclick=''>Compare Monthly Report");//</tr>");
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
//	m_tableTitle = "Profit & Loss - Item Based";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59"+"'  ";
		break;
	default:
		break;
	}

//DEBUG(" m_dateSql=", m_dateSql);
	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.invoice_number, s.code, s.supplier_code, s.name, s.commit_price  ";
	sc += ", s.normal_price, s.quantity ";
	sc += " FROM sales s ";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " WHERE 1=1 ";
	sc += " AND s.commit_price <> s.normal_price ";
	if(Session["slt_code"] != null && Session["slt_code"] != "")
		sc += " AND s.code = "+ Session["slt_code"];
	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
	{
		if(TSIsDigit(Request.QueryString["code"].ToString()))
			sc += " AND s.code = "+ Request.QueryString["code"];
	}
	if(m_sales_id != "-1")
	{
		sc += " AND o.sales = " + m_sales_id;
	}
	if(Session["branch_support"] != null)
	{
		if(m_branchID != "-1")
			sc += " AND i.branch = " + m_branchID;
	}
	sc += m_dateSql;
	sc += " ORDER BY i.invoice_number ";
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
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br>");
	Response.Write("<b>Branch : " + GetBranchName(m_branchID) + " Sales : " + m_sales_person + "</b><br><br>");

	BindPRItem();
	if(m_bShowPic)
	{
		DrawChart();

		string uname = EncodeUserName();

		Response.Write("<img src=" + m_picFile + ">");

		Response.Write("<form action=rdiscount.aspx method=post>");
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
	m_cPI.PageSize = 100;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	//DEBUG(" rows = ", rows );
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Invoice#</th>");
	Response.Write("<th>Code</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th>Normal_Price</th>");
	Response.Write("<th>Sales_Price</th>");
	Response.Write("<th>Sales_QTY</th>");
	Response.Write("<th>Discount</th>");
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
	double dDiscountTotal = 0;
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
		string invoice_number = dr["invoice_number"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sales_qty = dr["quantity"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string normal_price = dr["normal_price"].ToString();
//		string cost = dr["supplier_price"].ToString();
		
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dNormalPrice = MyDoubleParse(normal_price);
		double dDiscount = (dNormalPrice - dCommitPrice) * dQTY;
		dDiscountTotal += dDiscount;
		
		//DEBUG("dmargin = ", dMargin.ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a href=invoice.aspx?id=" + invoice_number + " class=o target=_blank>" + invoice_number + "</a></td>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
//		Response.Write("<td align=center><a href=rdiscount.aspx?type=0&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=right>" + dSales.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dNormalPrice.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dCommitPrice.ToString("c") + "</td>");
		Response.Write("<td align=right>" + sales_qty + "</td>");
		Response.Write("<td align=right>" + dDiscount.ToString("c") + "</td>");
//		Response.Write("<td align=center><a href=rdiscount.aspx?type=0&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=center><a href=rsales.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=right>" + dCost.ToString("c") + "</td>");
//		Response.Write("<td align=center><a href=rdiscount.aspx?type=0&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
//		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");
//		Response.Write("<td align=right>" + ((dSales-dCost)/dSales).ToString("p") + "</td>");
//		Response.Write("<td align=right>" + dMargin.ToString("p") + "</td>");
		//Response.Write("<td align=right>" + dMargin.ToString("p") + "</td>");
		Response.Write("</tr>");
/*
		if(dNormalPrice > m_nMaxY)
			m_nMaxY = dNormalPrice;
		if(dNormalPrice < 0 && dNormalPrice < m_nMinY)
			m_nMinY = dNormalPrice;
*/
		if(dDiscount > m_nMaxY)
			m_nMaxY = dDiscount;
		if(dDiscount < 0 && dDiscount < m_nMinY)
			m_nMinY = dDiscount;

		//xml chart data
		x = (i).ToString();
		m_nMaxX = i;

		y = dDiscount.ToString();

		name = XMLDecoding(name);
		legend = name.Replace("&", " ");
		
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");
/*
		y = dNormalPrice.ToString();
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
*/
	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
/*	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb3.ToString());
	m_sb.Append("</chartdataisland>\r\n");
*/
	m_IslandTitle[0] = "--Discount";
//	m_IslandTitle[1] = "--Gross Cost";
//	m_IslandTitle[2] = "--Total Sales";
	m_nIsland = 1;

//	double dMarginAve = 0;
//	if(margins > 0)
//		dMarginAve = (dSalesTotal - dCostTotal) / dSalesTotal;

	//total
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=" + m_cols + " align=right style=\"font-size:16\"><b>Total Discount : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:16\">" + dDiscountTotal.ToString("c") + "</td>");
//	Response.Write("<td align=right style=\"font-size:16\">" + dSalesTotal.ToString("c") + "</td>");
//	Response.Write("<td align=right style=\"font-size:16\">" + dCostTotal.ToString("c") + "</td>");
//	Response.Write("<td align=right style=\"font-size:16\">" + dProfitTotal.ToString("c") + "</td>");
//	Response.Write("<td align=right nowrap style=\"font-size:16\">" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	
	Response.Write("<tr><td colspan=" + m_cols + ">" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<br>");

	//write xml data file for chart image
	if(m_bShowPic)
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
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
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
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
					sc += " AND oo.sales = "+ m_sales_id;
			}
			sc += " ), 0) AS freight"+ nYear + nMonth +" ";
		
		}
		sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
			sc += " AND o.sales = "+ m_sales_id;
		if(Session["branch_support"] != null)
		{
			sc += " AND i.branch = " + m_branchID;
		}
	}
	else
	{
		sc += " SELECT SUM(i.price) AS sales_amount, SUM(i.freight) AS freight ";
		sc += ", (SELECT SUM(s.supplier_price * s.quantity) ";
		sc += " FROM sales s INNER JOIN ";
		sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		sc += " orders oo ON oo.invoice_number = s.invoice_number ";
		sc += " WHERE oo.sales = o.sales ";
/*	
		sc += ", (SELECT SUM(s.cost * s.qty) ";
		sc += " FROM sales_cost s INNER JOIN ";
		sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		sc += " orders oo ON oo.invoice_number = s.invoice_number ";
		sc += " WHERE oo.sales = o.sales ";
*/
		sc += m_dateSql;
		sc += " ) AS gross_profit ";
		sc += ", (SELECT SUM(s.supplier_price * s.quantity) ";
		sc += " FROM sales s INNER JOIN ";
		sc += " invoice i ON i.invoice_number = s.invoice_number JOIN ";
		sc += " orders oo ON oo.invoice_number = s.invoice_number ";
		sc += " WHERE oo.sales = o.sales ";
		if(Session["branch_support"] != null)
		{
			sc += " AND i.branch = " + m_branchID;
		}
		sc += m_dateSql;
		sc += " ) AS gross_profit1 ";
		
		sc += " , isnull(c.name,'Online Orders') AS name, c.type, ISNULL(o.sales, '-1') AS id ";
		sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		sc += " LEFT OUTER JOIN card c ON c.id = o.sales ";
		sc += " WHERE 1=1 ";
		if(Session["branch_support"] != null)
		{
			sc += " AND i.branch = " + m_branchID;
		}
		sc += m_dateSql;
		sc += " GROUP BY o.sales, c.name, c.type, c.id ";
		sc += " ORDER BY o.sales ";
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
//		sc += "	SELECT SUM(s.cost * s.qty) AS gross_profit, ISNULL(oo.sales, '-1') AS id ";
		sc += "	SELECT SUM(s.supplier_price * s.quantity) AS gross_profit, ISNULL(oo.sales, '-1') AS id ";
		sc += " FROM invoice i INNER JOIN ";
		sc += " orders oo ON i.invoice_number = oo.invoice_number LEFT OUTER JOIN ";
		sc += " sales s ON s.invoice_number = i.invoice_number AND s.invoice_number = oo.invoice_number ";
//		sc += " sales_cost s ON s.invoice_number = i.invoice_number AND s.invoice_number = oo.invoice_number ";
		
		sc += " WHERE  1=1 ";
		if(Session["branch_support"] != null)
		{
			sc += " AND i.branch = " + m_branchID;
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

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	BindPRSales();

	m_xLabel = "Sales Person";
	m_yLabel = "Profit";
	if(m_bShowPic)
	{
	DrawChart();

	string uname = EncodeUserName();

	Response.Write("<img src=" + m_picFile + ">");

	Response.Write("<form action=rdiscount.aspx method=post>");
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

//	Response.Write(" <input type=checkbox name=legends");
//	if(m_bHasLegends)
//		Response.Write(" checked");
//	Response.Write("><b>Legends</b> ");
	
	Response.Write(" <input type=submit name=cmd value=Redraw " + Session["button_style"] + ">");
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

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
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
			if(profit == null || profit == "" || dprofit_temp <0)
				profit = ds.Tables["gross_profit"].Rows[i]["gross_profit"].ToString();
//DEBUG("profit = ", profit);
			if(profit == null || profit == "" || dprofit_temp <0)
				profit = dr["gross_profit1"].ToString();
			
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
		Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
		Response.Write("<td align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
		Response.Write("<td align=right style=\"font-size:16\">" + dSalesTotal.ToString("c") + "</td>");
		Response.Write("<td align=right nowrap style=\"font-size:16\">" + dProfitTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=3>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

	Response.Write("<br><center><h4>");
	
	Response.Write("<b>Total Freight : </b><font color=red>" + dFreightTotal.ToString("c") + "</font>&nbsp&nbsp;");
	Response.Write("<b>Total Sales : </b><font color=red>" + dSalesTotal.ToString("c") + "</font>&nbsp;");
	Response.Write("<b>Sub Total : </b><font color=red>" + dAllTotal.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</center></h4>");
	//write xml data file for chart image
	if(m_bShowPic)
		WriteXMLFile();
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

bool PrintBranchNameOptionsWithAll(string current_branch, string onchange_url)
{
	DataSet dsBranch = new DataSet();
	int rows = 0;

	//do search
	string sc = "SELECT id, name FROM branch ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsBranch, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=branch");
	if(onchange_url != "")
	{
		Response.Write(" onchange=\"window.location=('");
		Response.Write(onchange_url + "'+ this.options[this.selectedIndex].value ) \" ");
	}
	Response.Write(">");
	Response.Write("<option value='-1'>All</option>");
	for(int i=0; i<rows; i++)
	{
		string bname = dsBranch.Tables["branch"].Rows[i]["name"].ToString();
		string bid = dsBranch.Tables["branch"].Rows[i]["id"].ToString();
		Response.Write("<option value='" + bid + "' ");
		if(bid == current_branch)
			Response.Write("selected");
		Response.Write(">" + bname + "</option>");
	}
	if(rows == 0)
		Response.Write("<option value=1>Branch 1</option>");
	Response.Write("</select>");
	return true;
}

</script>

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
string m_filter1 = "1";
string m_filter2 = "8";
string m_lastFilterIndex = "8";
string m_code_from = "";
string m_code_to = "";
string m_customer_id_from = "";
string m_customer_id_to = "";
string m_cust_directory = "";
string [] m_filter_name1 = new string [7];
string [] m_filter_name2 = new string [9];
string m_sortedby = "DESC";
string m_sorted = "sum(s.quantity)";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("accountant"))
		return;
	m_uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate();
	//m_bGBSetShowPicOnReport = MyBooleanParse(GetSiteSettings("set_display_chart_on_report", "1", true));

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

	m_filter_name1 [1] = "Item/Stock Code";
	m_filter_name1 [2] = "Item/Stock Category";
	m_filter_name1 [3] = "Customer/Debtor";
	m_filter_name1 [4] = "Customer/Debtor Category";
	m_filter_name1 [5] = "Sales Person";
	m_filter_name1 [6] = "Sales Manager";

	m_filter_name2 [1] = "Item/Stock Code";
	m_filter_name2 [2] = "Item/Stock Category";
	m_filter_name2 [3] = "Customer/Debtor";
	m_filter_name2 [4] = "Customer/Debtor Category";
	m_filter_name2 [5] = "Sales Person";
	m_filter_name2 [6] = "Invoice";
	m_filter_name2 [7] = "Sales Manager";
	m_filter_name2 [8] = "Not Required";
	
	if(Request.Form["cmd"] != null)
	{
		m_brand = Request.Form["brand"];
		m_cat = Request.Form["cat"];
		m_scat = Request.Form["scat"];
		m_sscat = Request.Form["sscat"];
		m_branchID = Request.Form["branch"];
		if(Request.Form["code1"] != null && Request.Form["code1"] != "")
			m_code_from = Request.Form["code1"].ToString();
		if(Request.Form["code2"] != null && Request.Form["code2"] != "")
			m_code_to = Request.Form["code2"].ToString();
		if(Request.Form["customer1"] != null && Request.Form["customer1"] != "")
			m_customer_id_from = Request.Form["customer1"].ToString();
		if(Request.Form["customer2"] != null && Request.Form["customer2"] != "")
			m_customer_id_to = Request.Form["customer2"].ToString();
		if(Request.Form["directory"] != null && Request.Form["directory"] != "")
			m_cust_directory = Request.Form["directory"].ToString();
		if(Request.Form["employee"] != null && Request.Form["employee"] != "")
			m_sales_id = Request.Form["employee"].ToString();
		
		m_filter1 = Request.Form["filter1"];
		m_filter2 = Request.Form["filter2"];
	}
	else
	{
		if(Request.QueryString["code1"] != null && Request.QueryString["code1"] != "")
			m_code_from = Request.QueryString["code1"].ToString();
		if(Request.QueryString["code2"] != null && Request.QueryString["code2"] != "")
			m_code_to = Request.QueryString["code2"].ToString();
		if(Request.QueryString["customer1"] != null && Request.QueryString["customer1"] != "")
			m_customer_id_from = Request.QueryString["customer1"].ToString();
		if(Request.QueryString["customer2"] != null && Request.QueryString["customer2"] != "")
			m_customer_id_to = Request.QueryString["customer2"].ToString();
		if(Request.QueryString["directory"] != null && Request.QueryString["directory"] != "")
			m_cust_directory = Request.QueryString["directory"].ToString();
		if(Request.QueryString["employee"] != null && Request.QueryString["employee"] != "")
			m_sales_id = Request.QueryString["employee"].ToString();			
		m_filter1 = Request.QueryString["filter1"];
		m_filter2 = Request.QueryString["filter2"];
		if(Request.QueryString["by"] != null && Request.QueryString["by"] != "")
			m_sortedby = Request.QueryString["by"].ToString();
		if(Request.QueryString["sorted"] != null && Request.QueryString["sorted"] != "")
			m_sorted = Request.QueryString["sorted"].ToString();
		if(m_sortedby.ToUpper() == "DESC") 
			m_sortedby = "ASC";
		else
			m_sortedby = "DESC";
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
		if(!doShowEmployee(4))
			return;
		if(Request.Form["employee"] == "all")
			m_sales_person = "all";
	}
/*	if(Request.QueryString["type"] != "" && Request.QueryString["type"] != null)
	{
		m_type = Request.QueryString["type"];
		if(Request.QueryString["np"] != null)
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	}
	*/
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
//DEBUG("mtye =", m_type);
	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["np"] == null || Request.QueryString["np"] == "")
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

	doQueryReport();
/*	switch(MyIntParse(m_type))
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
*/
	PrintAdminFooter();
}

bool doShowEmployee(int nType)  //nType for customer type
{
	if(ds.Tables["employee"] != null)
		ds.Tables["employee"].Clear();
	string sc = "";
	if(nType == 1)
		sc = " SELECT DISTINCT e.name, e.name AS directory, e.id AS type ";
	else
		sc = " SELECT DISTINCT c.name, c.trading_name, c.company, c.contact, c.phone, c.id, e.name AS directory ";
	sc += " FROM card c ";
	sc += " JOIN enum e ON e.id = c.directory AND e.class = 'card_dir' ";
	if(nType == 1)
		sc += " WHERE (c.type = "+ nType +" OR c.type = 2 ) ";
	else
		sc += " WHERE c.type = "+ nType +"";
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all" && m_branchID != "0")
			sc += " AND c.our_branch = " + m_branchID;
	}
	if(m_sales_id != null && m_sales_id != "" && m_sales_id != "all")
		sc += " AND c.id = "+ m_sales_id +"";
//DEBUG(" sc = ", sc);	
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
	if(rows == 1 && nType != 1)
	{
		m_sales_person = ds.Tables["employee"].Rows[0]["name"].ToString();
	}
	
	return true;
}


bool DoItemOption()
{
	int rows = 0;
	string sc = "";
/*	string sc = "SELECT DISTINCT brand FROM product p  ORDER BY brand ";
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
	Response.Write("<select name=brand ");
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
*/
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
	Response.Write("<select name=cat ");
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
	Response.Write("<br><center><h3>Sales Analysis Report</h3>");
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
		PrintBranchNameOptions(m_branchID, b_uri, true);
	}
	Response.Write("<table align=center cellspacing=1 cellpadding=2 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td><b>Filter 1</b></td><td><b>Filter 2</b></td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<table height=90% align=center cellspacing=1 cellpadding=5 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	//filter option 1 and 2
	int nCount1 = 1;
	
	for(int i=1; i<m_filter_name1.Length; i++)
	{
		if(i == 1)
			Response.Write("<tr>");
		Response.Write("<td>"+ i +". <input type=radio name=filter1 ");
		if(i==1)
			Response.Write(" checked ");
		Response.Write(" value="+ i +">"+ m_filter_name1[i].ToString() +"</td><td> &nbsp; </td>"); //<td>5. <input type=radio name=filter2 value=5>Item/Stock Category</td></tr>");
		
		if(nCount1 == 2)
		{
			Response.Write("</tr><tr>");
			nCount1 = 0;
		}
		nCount1++;

	}
/*	Response.Write("<tr><td>1. <input type=radio name=filter1 value=1 checked>Item/Stock Code</td><td> &nbsp; </td><td>4. <input type=radio name=filter1 value=4>Item/Stock Category</td></tr>");
	Response.Write("<tr><td>2. <input type=radio name=filter1 value=2>Customer/Debtor</td><td> &nbsp; </td><td>5. <input type=radio name=filter1 value=5>Customer/Debtor Directory</td></tr>");
	Response.Write("<tr><td>3. <input type=radio name=filter1 value=3>Sales Person</td><td> &nbsp; </td></tr>"); //<td>6. <input type=radio name=filter1 value=6>Sales Location</td></tr>");
	//Response.Write("<tr><td>7. <input type=radio name=filter1 value=7></td><td>8. <input type=radio name=filter1 value=8></td></tr>");
	*/
	Response.Write("</table>");
	Response.Write("</td><td>");
	Response.Write("<table align=center width=100% cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=2><b>Filter 1</b></td></tr>");
	int nCount2 = 1;
	for(int i=1; i<m_filter_name2.Length; i++)
	{
		if(i == 1)
			Response.Write("<tr>");
		Response.Write("<td>"+ i +". <input type=radio name=filter2 ");
		if(i==8)
			Response.Write(" checked ");
		Response.Write(" value="+ i +">"+ m_filter_name2[i].ToString() +"</td><td> &nbsp; </td>"); //<td>5. <input type=radio name=filter2 value=5>Item/Stock Category</td></tr>");
		
		if(nCount2 == 2)
		{
			Response.Write("</tr><tr>");
			nCount2 = 0;
		}
		nCount2++;

	}

	Response.Write("</table>");
	Response.Write("</td></tr>");

	//ranges option here
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=2><b>Option Range</b></td></tr>");
	Response.Write("<tr><td colspan=2>");
	Response.Write("<table height=90% width=100% align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td></td><td><b>FROM</b></td><td><b>TO</b></td></tr>");
	Response.Write("<tr><td>Item/Stock Code</td><td><input type=text name=code1 ");
	Response.Write(" onclick=\"window.open('search_code.aspx', 'code1','menubar=0,resizable=1,scrollbars=1,width=450,height=300', 'code1.focus();');\" ");
	Response.Write(" ></td><td><input type=text name=code2 onclick=\"window.open('search_code.aspx', 'code2','menubar=0,scrollbars=1,resizable=1,width=450,height=300', 'code2.focus();');\" ></td></tr>");
	Response.Write("<tr><td>Item/Stock Category</td><td colspan=2>");//<input type=text name=cat_frm></td><td><input type=text name=cat_to></td></tr>");
	DoItemOption();
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Customer/Debtor ID</td><td><input type=text name=customer1 ");
	Response.Write(" onclick=\"window.open('search_card.aspx', 'customer1','menubar=0,resizable=1,scrollbars=1,width=450,height=300', 'customer1.focus();');\" ");
	Response.Write("></td><td><input type=text name=customer2 ");
	Response.Write(" onclick=\"window.open('search_card.aspx', 'customer2','menubar=0,resizable=1,scrollbars=1,width=450,height=300', 'customer2.focus();');\" ");
	Response.Write("></td></tr>");
	Response.Write("<tr><td>Customer/Debtor Category</td><td colspan=2>"); //<input type=text name=directory_frm></td><td><input type=text name=directory_to></td></tr>");
	Response.Write("<select name=directory> <option value='all'>all");
	if(!doShowEmployee(1))
		return;
	for(int ii=0; ii<ds.Tables["employee"].Rows.Count; ii++)
	{
		DataRow dr = ds.Tables["employee"].Rows[ii];
		string directory = dr["directory"].ToString();
		
		string id = dr["type"].ToString();
		Response.Write("<option value='"+id+"'>"+ directory +"</option>");
	}
	Response.Write("</select>");
	Response.Write("</td></tr>");
//	Response.Write("<tr><td>Sales Person</td><td><input type=text name=sales_rep_frm></td><td><input type=text name=sales_rep_to></td></tr>");

	Response.Write("<tr><td>Sales Person</td><td colspan=2>");
	Response.Write("<select name=employee> <option value='all'>all");
	if(!doShowEmployee(4))
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

	Response.Write("</table>");	
	Response.Write("</td></tr>");	

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Last Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>Select Date Range</td></tr>");

//	int i = 1;
	PrintJavaFunction(); //call visivility object
	datePicker(); //call date picker function from common.cs

	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td colspan=2>");
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

	Response.Write("<tr><td align=right colspan=2><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");
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

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool doQueryReport()
{
	m_tableTitle = "Sales Analysis Report";
	m_tableTitle += " <br><font size=1>Type: "+ m_filter_name1[int.Parse(m_filter1)].ToString() +"";
	if(m_filter2 != "8")
		m_tableTitle += " - "+ m_filter_name2[int.Parse(m_filter2)].ToString();
	m_tableTitle += "</font>";
	if(m_sales_id != null && m_sales_id != "" && m_sales_id != "all")
		m_tableTitle += "<br><font size=1>Sales Person: "+ m_sales_person +"</font>";
	if(m_cat != null && m_cat != "" && m_cat != "all")
			m_tableTitle += " <br><font size=1>CAT: "+ m_cat +"";
	if(m_scat != null && m_scat != "" && m_scat != "all")
		m_tableTitle += " - "+ m_scat;
	if(m_sscat != null && m_sscat != "" && m_sscat != "all")
		m_tableTitle += " - "+ m_sscat;
	 
	if(m_code_from != "" && TSIsDigit(m_code_from) && m_code_to != "" && TSIsDigit(m_code_to))
		m_tableTitle += "<br><font size=1>Item/Stock Code : "+ m_code_from +" - "+ m_code_to +" ";

	if((m_customer_id_from != "" && TSIsDigit(m_customer_id_from)) && m_customer_id_to != "" && TSIsDigit(m_customer_id_to))
		m_tableTitle += "<br><font size=1>Customer ID : "+ m_customer_id_from +" - "+ m_customer_id_to +" ";
	
	if(m_cust_directory != "" && TSIsDigit(m_cust_directory))
		m_tableTitle += "<br><font size=1>Directory: "+ m_cust_directory +"</font>";
	if(Session["branch_support"] != null)
	{		
		//if(TSIsDigit(m_branchID) && m_branchID != "all")
		if(TSIsDigit(m_branchID) && m_branchID != "all" && m_branchID != "0")
			m_tableTitle += " <br><font size=1>branch: "+GetBranchName(m_branchID) +"</font>";
		else
			m_tableTitle += " <br><font size=1>branch: ALL</font>";
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
		default:
			break;
	}

//DEBUG(" m_dateSql=", m_dateSql);
	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";
	if(m_filter1 == "1")
	{
		sc += " s.code, s.supplier_code, c.name ";
	}
	else if(m_filter1 == "2")
	{
		//sc += " c.ss_cat AS code, ' ' AS supplier_code, c.name ";
		sc += " '' AS code, ' ' AS supplier_code, c.ss_cat AS name ";
	}
	else if(m_filter1 == "3")
	{
		sc += " i.card_id AS code, '' AS supplier_code, cd.company AS name, cd.name AS customer ";
	}
	else if(m_filter1 == "4")
	{
		sc += " e.name AS code, ' ' AS supplier_code, i.card_id AS customerID, cd.company AS name, cd.name AS customer ";
	}
	else if(m_filter1 == "5")
	{		
		sc += " ISNULL(o.sales, ' ') AS code, 'Sales REP:' AS supplier_code, cd.name AS name, '' AS customer ";		
	}
	else if(m_filter1 == "6")
	{		
		sc += " o.sales_manager AS code, 'Sales Manager:' AS supplier_code, cd.name AS name, '' AS customer ";		
	}
	sc += ", sum(s.commit_price * s.quantity) AS commit_price  ";
	sc += ", sum(s.supplier_price * s.quantity) AS supplier_price, sum(s.quantity) AS sales_qty";	
//	sc += ", sum(s.supplier_price) AS supplier_price, sum(s.quantity) AS sales_qty";	
	sc += " FROM sales s JOIN code_relations c ON c.code = s.code ";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " JOIN card cd ON cd.id = ";
	if(m_filter1 == "3")
		sc += " ISNULL(i.card_id, 0) ";
	else if(m_filter1 == "5")
		sc += " ISNULL(o.sales, ' ') ";
		//sc += " ISNULL(o.sales,0) ";
	else if(m_filter1 == "6")
		sc += " ISNULL(o.sales_manager, ' ') ";
		//sc += " ISNULL(o.sales_manager, (SELECT sales FROM card WHERE id = o.card_id)) ";
	else
		sc += " i.card_id ";
	sc += " JOIN enum e ON e.id = cd.directory AND e.class = 'card_dir' ";
	sc += " WHERE 1=1 ";
		
	if(Session["branch_support"] != null)
	{
		//if(TSIsDigit(m_branchID) && m_branchID != "all")
		if(TSIsDigit(m_branchID) && m_branchID != "all" && m_branchID != "0")
			sc += " AND i.branch = " + m_branchID;
	}
	sc += m_dateSql;

	if(m_brand != null && m_brand != "")
		sc += " AND c.brand = '" + EncodeQuote(m_brand) +"' ";
	if(m_cat != null && m_cat != "" && m_cat != "all")
		sc += " AND c.cat = '" + EncodeQuote(m_cat) +"' ";
	if(m_scat != null && m_scat != "" && m_scat != "all")
		sc += " AND c.s_cat = '" + EncodeQuote(m_scat) +"' ";
	if(m_sscat != null && m_sscat != "" && m_sscat != "all")
		sc += " AND c.ss_cat = '" + EncodeQuote(m_sscat) +"' ";
	 
	if(m_code_from != "" && TSIsDigit(m_code_from) && m_code_to != "" && TSIsDigit(m_code_to))
		sc += " AND s.code >= "+ m_code_from +" AND s.code <= "+ m_code_to +"";

	if((m_customer_id_from != "" && TSIsDigit(m_customer_id_from)) && m_customer_id_to != "" && TSIsDigit(m_customer_id_to))
		sc += " AND i.card_id >= "+ m_customer_id_from +" AND i.card_id <= "+ m_customer_id_to +"";
	
	if(m_cust_directory != "" && TSIsDigit(m_cust_directory))
		sc += " AND cd.directory = "+ m_cust_directory +" ";
	 
	if(m_sales_id  != "" && TSIsDigit(m_sales_id ))
		sc += " AND o.sales = "+ m_sales_id  +" ";
	
	if(m_filter1 == "1")
		sc += " GROUP BY s.code, s.supplier_code, c.name ";
	else if(m_filter1 == "2")
	{
		sc += " GROUP BY c.ss_cat "; // ,c.name";
//		sc += " ORDER BY c.ss_cat ";
	}
		
	else if(m_filter1 == "3")
		sc += " GROUP BY i.card_id, cd.company, cd.name ";	
		
	else if(m_filter1 == "4")
		sc += " GROUP BY e.name, cd.company, cd.name, i.card_id ";
				
	else if(m_filter1 == "5")
		sc += " GROUP BY o.sales, cd.name ";	
	else if(m_filter1 == "6")
		sc += " GROUP BY  o.sales_manager, cd.name";	

	//sc += " ORDER BY sum(s.quantity) DESC ";
//	else
		sc += " ORDER BY "+ m_sorted +" "+ m_sortedby +" ";

//DEBUG("sc filter1=", sc);

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
	
	BindAllReport();
/*	if(m_bShowPic && m_bGBSetShowPicOnReport)
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
	*/
	return true;
}

/////////////////////////////////////////////////////////////////
void BindAllReport()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	
	string stylesheet = "<STYLE> H1 {page-break-before: always}</STYLE>"; 
	Response.Write(stylesheet);

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
	m_cPI.URI += "&filter1="+ m_filter1 +"&filter2="+ m_filter2;

	if(m_brand != null && m_brand != "")
		m_cPI.URI += "&brand=" + HttpUtility.UrlEncode(m_brand) +"";
	if(m_cat != null && m_cat != "" && m_cat != "all")
		m_cPI.URI += "&cat=" + HttpUtility.UrlEncode(m_cat) +"";
	if(m_scat != null && m_scat != "" && m_scat != "all")
		m_cPI.URI += "&scat="+ HttpUtility.UrlEncode(m_scat) +"";
	if(m_sscat != null && m_sscat != "" && m_sscat != "all")
		m_cPI.URI += "&sscat=" + HttpUtility.UrlEncode(m_sscat);
	
	if(m_code_from != "" && m_code_from != null)
		m_cPI.URI += "&code1=" + m_code_from +"";
	if(m_code_to != "" && m_code_to != null)
		m_cPI.URI += "&code2=" + m_code_to +"";
	if(m_customer_id_from != "" && m_customer_id_from != null)
		m_cPI.URI += "&customer1=" + m_customer_id_from +"";
	if(m_customer_id_to != "" && m_customer_id_to != null)
		m_cPI.URI += "&customer2=" + m_customer_id_to +"";
	
	if(m_cust_directory != "" && m_cust_directory != null)
		m_cPI.URI += "&directory=" + m_cust_directory +"";

	if(m_sales_id != "" && m_sales_id != null)
		m_cPI.URI += "&employee=" + m_sales_id +"";

	m_cPI.PageSize = 70;
	if(m_filter2 != "8")	
		m_cPI.PageSize = 25;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	//DEBUG(" rows = ", rows );
	Response.Write("<table width=100%>");

	string sort_uri = m_cPI.URI;

	Response.Write("<tr><td>Requested Date: "+ DateTime.Now.ToString("D") +"</td><td align=right>Page: "+ m_cPI.CurrentPage +"</td></tr></table>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<th>Date</th>");
	Response.Write("<th></th>");
	Response.Write("<th></th>");
	Response.Write("<th></th>");
	//Response.Write("<th>DESCRIPTION</th>");
		
	Response.Write("<th width=5% align=center>");
	Response.Write("<a href='"+ sort_uri +"&sorted=sum(s.quantity)&by="+ m_sortedby +"' class=o>");		
	Response.Write("<font color=white><b>QUANTITY</a></th>");
	Response.Write("<th width=10% align=right>");
	Response.Write("<a href='"+ sort_uri +"&sorted=sum(s.commit_price * s.quantity)&by="+ m_sortedby +"' class=o>");
	Response.Write("<font color=white><b>GROSS_SALES</a></th>");
	Response.Write("<th width=10% align=right>");	
	Response.Write("<a href='"+ sort_uri +"&sorted=sum(s.supplier_price * s.quantity)&by="+ m_sortedby +"' class=o>");
	Response.Write("<font color=white><b>GROSS_COST</a></th>");	
	Response.Write("<th width=10% align=right>");
	Response.Write("<a href='"+ sort_uri +"&sorted=sum((s.commit_price - s.supplier_price) * s.quantity)&by="+ m_sortedby +"' class=o>");
	Response.Write("<font color=white><b>PROFIT</a></th>");	
	
	Response.Write("<th width=8% align=right>");
//	Response.Write("<a href='"+ sort_uri +"&sorted=sum((s.commit_price - s.supplier_price) * s.quantity) / sum(s.supplier_price * s.quantity)&by="+ m_sortedby +"' class=o>");
	Response.Write("<font color=white><b>MARGIN (%)</a></th>");
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
	int margins = 0;

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	StringBuilder sb3 = new StringBuilder();
			
	bool bNotDisplayTop = false;
	
//	if(m_filter1 == "1" && (m_filter2 != "1" && m_filter2 != "2" && m_filter2 != m_lastFilterIndex))
	if(m_filter1 == "1" && (m_filter2 != "1" && m_filter2 != m_lastFilterIndex))
		bNotDisplayTop = false;
//	else if(m_filter1 == "2" && (m_filter2 != "1" && m_filter2 != "2" && m_filter2 != m_lastFilterIndex))
	else if(m_filter1 == "2" && (m_filter2 != "2" && m_filter2 != m_lastFilterIndex))
		bNotDisplayTop = false;	
	else if(m_filter1 == "3" && (m_filter2 != "3" && m_filter2 != "4" && m_filter2 != m_lastFilterIndex))
		bNotDisplayTop = false;
	else if(m_filter1 == "4" && (m_filter2 != "3" && m_filter2 != "4" && m_filter2 != m_lastFilterIndex))
		bNotDisplayTop = false;
	else if(m_filter1 == "5" && (m_filter2 != "5" && m_filter2 != m_lastFilterIndex))
		bNotDisplayTop = false;
	else if(m_filter1 == "6" && (m_filter2 != "7" && m_filter2 != m_lastFilterIndex))
		bNotDisplayTop = false;
	else if(m_filter2 == m_lastFilterIndex )
		bNotDisplayTop = true;
	else
		bNotDisplayTop = true;
	string lastCode = "";
	for(; i<rows && i<end; i++)
	{
		string filterName = "";
		string filterData = "";

		DataRow dr = ds.Tables["report"].Rows[i];
		//string date = dr["price_age"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
	
		if(m_filter1 != "1" && m_filter1 != "2")
		{
			if(name == "" || name == null)
				name = dr["customer"].ToString();
			if(g_bRetailVersion)
			{
				name = dr["customer"].ToString();
				if(name == "" || name == null)
						name = dr["customer"].ToString();
			}
		}
		
		string sales_qty = dr["sales_qty"].ToString();
//		string sales_amount = dr["sales_amount"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();
		//get the sum of total sales price which is different from invoice's price
//		string total_commit = dr["total_commit"].ToString(); 
		
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dCost = MyDoubleParse(cost);
		//DEBUG("dqty =", dQTY);
		double dProfit = 0; //dCommitPrice - dCost;
	/*	if(dQTY < 0)
		{
			dCost = dCost * (0-dQTY);
			dProfit = dCommitPrice + dCost;
		}
		else	
		{
			dCost *= dQTY;
			dProfit = dCommitPrice - dCost;
		}
		*/
		dProfit = dCommitPrice - dCost;
		//DEBUG("dProfit = ", dProfit);
		double dMargin = 1;
		if(dCost != 0)
			dMargin = dProfit / dCost;
		else
			dMargin = 0;
		dMargin = Math.Round(dMargin, 4);

		//DEBUG("dmargin =", ((dSales-dCost)/dSales).ToString("p"));
		//DEBUG("dsales =", dSales.ToString());
		margins++;

		dQtyTotal += dQTY;
//		dSalesTotal += dSales;
		dSalesTotal += dCommitPrice;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;
		//DEBUG("dmargin = ", dMargin.ToString());
		Response.Write("<tr");
		if(!bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		if(bNotDisplayTop)
			bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td valign=top>");
		Response.Write("<a href=\"javascript:_window=window.open('");
		if(m_filter1 == "1" )
			Response.Write("p.aspx?"+ code +"");
		else if(m_filter1 == "3")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter1 == "4")
			Response.Write("ecard.aspx?id="+ dr["customerID"].ToString() +"&v=view");
		else if(m_filter1 == "5" || m_filter1 == "6")
			Response.Write("ecard.aspx?id="+ code +"&v=view");

		
		Response.Write("', '','menubar=0, scrollbars=1, resizable=1'); _window.focus();\" class=d>");
		if(lastCode != code )
		{
			if(g_bRetailVersion)
				Response.Write("" + code + "");
			Response.Write("</a></td>");
			lastCode = code;
		}
		else
			Response.Write("</td>");
		Response.Write("<td valign=top>" + supplier_code + "</td>");
		Response.Write("<td valign=top>");
			Response.Write("<a href=\"javascript:_window=window.open('");
		if(m_filter1 == "1" )
			Response.Write("p.aspx?"+ code +"");
		else if(m_filter1 == "3")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter1 == "4")
			Response.Write("ecard.aspx?id="+ dr["customerID"].ToString() +"&v=view");
		else if(m_filter1 == "5")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter1 == "6")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		
		Response.Write("', '','menubar=0, scrollbars=1, resizable=1'); _window.focus();\" class=d>");
		Response.Write("" + name + "</a></td>");
		Response.Write("<td align=center>");
		//<a href='report.aspx?type=1&code=" + code + "&period="+ m_nPeriod +"");
		//if(m_nPeriod == 3)
		//	Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		//Response.Write("' class=o title='Click to view details'>" + sales_qty + "</a></td>");

		if(bNotDisplayTop)
			Response.Write(sales_qty);
		Response.Write("</td>");
		Response.Write("<td align=right>");
		if(bNotDisplayTop)
			Response.Write(dCommitPrice.ToString("c"));
		Response.Write("</td>");
		Response.Write("<td align=right>");
		if(bNotDisplayTop)
			Response.Write(dCost.ToString("c"));
		Response.Write("</td>");
		Response.Write("<td align=right>");
		if(bNotDisplayTop)
			Response.Write(dProfit.ToString("c"));
		Response.Write("</td>");
		Response.Write("<td align=right>");
		if(bNotDisplayTop)
			Response.Write(dMargin.ToString("p"));
		Response.Write("</td>");

		Response.Write("</tr>");
		
		if(m_filter2 != m_lastFilterIndex)
		{
			//if(m_filter1 == "1" && (m_filter2 != "1" && m_filter2 != "2"))
			if(m_filter1 == "1" && (m_filter2 != "1"))
			{
				filterData = " AND c.code = '" + code +"'";				
			}
			//else if(m_filter1 == "2" && (m_filter2 != "1" && m_filter2 != "2"))
			else if(m_filter1 == "2" && (m_filter2 != "2"))
			{
				//filterData = " AND c.ss_cat = '" + code +"'";			
				filterData = " AND c.ss_cat = '" + name +"'";			
			}
			else if(m_filter1 == "3" && (m_filter2 != "3" && m_filter2 != "4"))
			{
				filterData = " AND i.card_id = '" + code +"'";			
			}
			else if(m_filter1 == "4" && (m_filter2 != "3" && m_filter2 != "4"))
			{
				filterData = " AND i.card_id = '" + dr["customerID"].ToString() +"'";
			/*	if(dr["name"].ToString() == null || dr["name"].ToString() == "")
					filterData = " AND cd.name = '" + name +"'";
				else
					filterData = " AND cd.company = '" + name +"'";
					*/
			}
			else if(m_filter1 == "5" && (m_filter2 != "5"))
			{				
				filterData = " AND o.sales = '" + code +"'";	
				if(code == "0")
					filterData = " AND o.sales IS NULL ";	
			}	
			else if(m_filter1 == "6" && (m_filter2 != "7"))
			{
				filterData = " AND o.sales_manager = '" + code +"'";			
			}	
//DEBUG("filter da = ", filterData);
			Response.Write("<tr><td colspan=8>");
			doQuerySubReport(m_dateSql, filterData);
			Response.Write("</td></tr>");
		}
		if(dCommitPrice > m_nMaxY)
			m_nMaxY = dCommitPrice;

		if(dCommitPrice < 0 && dCommitPrice < m_nMinY)
			m_nMinY = dCommitPrice;
	
	}


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
	Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	dQtyTotal = 0;
	dSalesTotal = 0;
	dCostTotal = 0;
	dProfitTotal = 0;
	dMarginTotal = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		
		string sales_qty = dr["sales_qty"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dCost = MyDoubleParse(cost);
		double dProfit = 0; //dCommitPrice - dCost;
		/*if(dQTY < 0)
		{
			dCost = dCost * (0-dQTY);
			dProfit = dCommitPrice + dCost;
		}
		else	
		{
			dCost *= dQTY;
			dProfit = dCommitPrice - dCost;
		}
		*/
		dProfit = dCommitPrice - dCost;
		//double dProfit = 0; //dCommitPrice - dCost;
		double dMargin = 0;

		dMargin = 1;
		if(dCost != 0)
			dMargin = dProfit / dCost;
		else
			dMargin = 0;			
		
		dMargin = Math.Round(dMargin, 4);
		dQtyTotal += dQTY;
		dSalesTotal += dCommitPrice;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal = (dProfitTotal)/dCostTotal;
	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=middle style=\"font-size:14\">" + dQtyTotal.ToString() + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right style=\"font-size:14\">" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:14\">" + dMarginTotal.ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");
	Response.Write("<tr><td colspan=7><a href='"+ Request.ServerVariables["URL"] +"' class=d>New Report</a></td></tr>");
	Response.Write("</table>");
	Response.Write("<br>");

	//write xml data file for chart image
//	if(m_bShowPic && m_bGBSetShowPicOnReport)
//		WriteXMLFile();
}


///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool doQuerySubReport(string sqlDate, string filterData)
{	

//DEBUG(" m_dateSql=", m_dateSql);
	DataSet dst = new DataSet();
	dst.Clear();

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";
//	if(m_filter2 == "1" && (m_filter1 != "1" && m_filter1 != "2"))	
	if(m_filter2 == "1" && (m_filter1 != "1"))	
	{
		sc += " s.code AS code, s.supplier_code, s.name, cd.company, cd.name AS customer, cd.id ";
		sc += " , e.name AS directory , c.cat, c.ss_cat, c.name AS item_name ";
	}
//	else if(m_filter2 == "2"  && (m_filter1 != "1" && m_filter1 != "2"))
	else if(m_filter2 == "2"  && (m_filter1 != "2"))
	{
		//sc += " c.cat+ '-' + c.ss_cat AS code, ' ' AS supplier_code, '' AS name ";
		sc += " ''  AS code, ' ' AS supplier_code, c.ss_cat AS name ";
	}
	else if(m_filter2 == "3"  && (m_filter1 != "3" && m_filter1 != "4"))
	{
		sc += " i.card_id AS code, 'Customer:' AS supplier_code, cd.company AS name, cd.name AS customer ";
	}
	else if(m_filter2 == "4"  && (m_filter1 != "3" && m_filter1 != "4"))
	{
		sc += " e.name AS code, ' ' AS supplier_code, cd.id AS customerID, cd.company AS name, cd.name AS customer ";
	}
	else if(m_filter2 == "5" && m_filter1 != "5")
	{
		sc += " o.sales AS code, 'Sales REP:' AS supplier_code, cd.name AS name, '' AS customer ";
	}
	else if(m_filter2 == "6")
	{
		sc += " i.invoice_number AS code, i.card_id AS supplier_code, cd.company AS name, cd.name AS customer ";
	}
	else if(m_filter2 == "7" && (m_filter1 != "6" ) )
	{
		sc += " o.sales_manager AS code, 'Sales Manager:' AS supplier_code, cd.name AS name, cd.name AS customer ";		
	}
	else
		return false;
	sc += ", sum(s.commit_price * s.quantity) AS commit_price  ";
	sc += ", sum(s.supplier_price * s.quantity) AS supplier_price, sum(s.quantity) AS sales_qty";
	//sc += ", sum(s.supplier_price) AS supplier_price, sum(s.quantity) AS sales_qty";
	sc += " FROM sales s JOIN code_relations c ON c.code = s.code ";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " JOIN card cd ON cd.id = ";
	if(m_filter2 == "5" || m_filter2 == "6")
		sc += " ISNULL(o.sales, ' ') ";
	else if(m_filter2 == "7")
		sc += " ISNULL(o.sales_manager, ' ') ";
	else
		sc += " i.card_id ";
	sc += " JOIN enum e ON e.id = cd.directory AND e.class = 'card_dir' ";
	sc += " WHERE 1=1 ";

	//if(filterName != "" && filterData != "")	
	//sc += "AND "+ filterName +" = '"+ filterData +"'";	 
	sc += filterData;

	if(Session["branch_support"] != null)
	{
		//if(TSIsDigit(m_branchID) && m_branchID != "all")
		if(TSIsDigit(m_branchID) && m_branchID != "all" && m_branchID != "0")
			sc += " AND i.branch = " + m_branchID;
	}
	sc += sqlDate;
	 
	if(m_brand != null && m_brand != "")
		sc += " AND c.brand = '" + EncodeQuote(m_brand) +"' ";
	if(m_cat != null && m_cat != "" && m_cat != "all")
		sc += " AND c.cat = '" + EncodeQuote(m_cat) +"' ";
	if(m_scat != null && m_scat != "" && m_scat != "all")
		sc += " AND c.s_cat = '" + EncodeQuote(m_scat) +"' ";
	if(m_sscat != null && m_sscat != "" && m_sscat != "all")
		sc += " AND c.ss_cat = '" + EncodeQuote(m_sscat) +"' ";

	if(m_code_from != "" && TSIsDigit(m_code_from) && m_code_to != "" && TSIsDigit(m_code_to))
		sc += " AND s.code >= "+ m_code_from +" AND s.code <= "+ m_code_to +"";

	if((m_customer_id_from != "" && TSIsDigit(m_customer_id_from)) && m_customer_id_to != "" && TSIsDigit(m_customer_id_to))
		sc += " AND i.card_id >= "+ m_customer_id_from +" AND i.card_id <= "+ m_customer_id_to +"";
	
	if(m_cust_directory != "" && TSIsDigit(m_cust_directory))
		sc += " AND cd.directory = "+ m_cust_directory +" ";
	 
	if(m_sales_id  != "" && TSIsDigit(m_sales_id ))
		sc += " AND o.sales = "+ m_sales_id  +" ";
	
//	if(m_filter2 == "1")
	//	sc += " GROUP BY s.supplier_code, s.code, s.name, cd.id, cd.company, c.cat, c.ss_cat ,c.name, e.name, i.invoice_number, cd.name ";
/*	else if(m_filter2 == "2")
		sc += " GROUP BY cd.id, cd.company ";
	else if(m_filter2 == "3")
		sc += " GROUP BY cd.id, cd.company ";
	else if(m_filter2 == "4")
		sc += " GROUP BY c.cat, c.ss_cat ,c.name";
	else if(m_filter2 == "5")
		sc += " GROUP BY cd.company, e.name ";
*/
	
	if(m_filter2 == "1")
	{
		sc += " GROUP BY s.code , s.supplier_code, s.name, cd.company, cd.name, cd.id ";
		sc += " , e.name , c.cat, c.ss_cat, c.name  ";
	}
	else if(m_filter2 == "2")
	{
		sc += " GROUP BY c.ss_cat "; // ,c.name";
//		sc += " ORDER BY c.ss_cat ";
	}
		
	else if(m_filter2 == "3")
		sc += " GROUP BY i.card_id, cd.company, cd.name ";	
		
	else if(m_filter2 == "4")
		sc += " GROUP BY e.name , cd.id , cd.company , cd.name  ";
				
	else if(m_filter2 == "5")
		sc += " GROUP BY o.sales, cd.name ";	
	else if(m_filter2 == "6")
		sc += " GROUP BY i.invoice_number , i.card_id, cd.company, cd.name ";	
	else if(m_filter2 == "7")
		sc += " GROUP BY  o.sales_manager, cd.name";
	else if(m_filter1 == "7")
		sc += " GROUP BY  o.sales_manager, cd.name";
	//sc += " ORDER BY sum(s.quantity) DESC ";
	//else
		sc += " ORDER BY "+ m_sorted +" "+ m_sortedby +" ";
//	sc += " ORDER BY "+ m_sorted +" "+ m_sortedby +" ";
	//sc += " ORDER BY sum(s.quantity) DESC ";

//DEBUG("sub sc=", sc);

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "sub_report");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	
	BindSubReport(dst);

	return true;
}


void BindSubReport(DataSet dst)
{
	int i = 0;
	int rows = dst.Tables["sub_report"].Rows.Count;
	
	Response.Write("<table width=98%  align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	double dQtyTotal = 0;
	double dSalesTotal = 0;
	double dCostTotal = 0;
	double dProfitTotal = 0;
	double dMarginTotal = 0;
	int margins = 0;

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;

	for(; i<rows; i++)
	{
		DataRow dr = dst.Tables["sub_report"].Rows[i];
		//string date = dr["price_age"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();

		if(m_filter2 != "2" && m_filter2 != "6")
		{
			if(name == "" || name == null)
				name = dr["customer"].ToString();
		}
		string sales_qty = dr["sales_qty"].ToString();
//		string sales_amount = dr["sales_amount"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();
		//get the sum of total sales price which is different from invoice's price
//		string total_commit = dr["total_commit"].ToString(); 
		
		double dQTY = MyDoubleParse(sales_qty);
		double dCommitPrice = MyDoubleParse(commit_price);
		double dCost = MyDoubleParse(cost);
		double dProfit = 0; //dCommitPrice - dCost;
	/*	if(dQTY < 0)
		{
			dCost = dCost * (0-dQTY);
			dProfit = dCommitPrice + dCost;
		}
		else	
		{
			dCost *= dQTY;
			dProfit = dCommitPrice - dCost;
		}	
		*/
		dProfit = dCommitPrice - dCost;
		double dMargin = 1;
		if(dCost != 0)
			dMargin = dProfit / dCost;
		dMargin = Math.Round(dMargin, 4);

		margins++;

		dQtyTotal += dQTY;

		dSalesTotal += dCommitPrice;
		dCostTotal += dCost;
		dProfitTotal += dProfit;
		dMarginTotal += dMargin;

		Response.Write("<tr");
	//	if(bAlterColor)
	//		Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td  width=7%>");
		Response.Write("<a href=\"javascript:_window=window.open('");
		if(m_filter2 == "1" || m_filter2 == "2")
			Response.Write("p.aspx?"+ code +"");
		else if(m_filter2 == "3")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter2 == "4")
			Response.Write("ecard.aspx?id="+ dr["customerID"].ToString() +"&v=view");
		else if(m_filter2 == "5")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter2 == "6")
			Response.Write("invoice.aspx?n="+ code +"");
		Response.Write("', '','menubar=0, scrollbars=1, resizable=1'); _window.focus();\" class=d>");
		Response.Write("" + code + "</a></td>");
		Response.Write("<td width=10%>" + supplier_code + "</td>");
		Response.Write("<td>");
		Response.Write("<a href=\"javascript:_window=window.open('");
		if(m_filter2 == "1" || m_filter2 == "2")
			Response.Write("p.aspx?"+ code +"");
		else if(m_filter2 == "3")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter2 == "4")
			Response.Write("ecard.aspx?id="+ dr["customerID"].ToString() +"&v=view");
		else if(m_filter2 == "5")
			Response.Write("ecard.aspx?id="+ code +"&v=view");
		else if(m_filter2 == "6")
			Response.Write("invoice.aspx?n="+ code +"");
		Response.Write("', '','menubar=0, scrollbars=1, resizable=1'); _window.focus();\" class=d>");
		Response.Write("" + name + "</a></td>");
		Response.Write("<td align=center width=5% >");
		//<a href='report.aspx?type=1&code=" + code + "&period="+ m_nPeriod +"");
		//if(m_nPeriod == 3)
		//	Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		//Response.Write("' class=o title='Click to view details'>" + sales_qty + "</a></td>");
		Response.Write(sales_qty + "</td>");

		Response.Write("<td align=right width=10% >" + dCommitPrice.ToString("c") + "</td>");
		Response.Write("<td align=right width=10% >" + dCost.ToString("c") + "</td>");
		Response.Write("<td align=right width=10% >" + dProfit.ToString("c") + "</td>");

		Response.Write("<td align=right width=8% >" + dMargin.ToString("p") + "</td>");

		Response.Write("</tr>");
	
	}

	double dMarginAve = 0;
	if(margins > 0)
		dMarginAve = dProfitTotal / dCostTotal;

	//total
	Response.Write("<tr><td colspan=3></td><td colspan=5><hr size=1 width=100% color=gray></td></tr>");
	Response.Write("<tr ");
	Response.Write(">");
	Response.Write("<td colspan=3 ><b> &nbsp; </b></td>");
	Response.Write("<td align=middle  width=5% >" + dQtyTotal.ToString() + "</td>");
	Response.Write("<td align=right  width=10%>" + dSalesTotal.ToString("c") + "</td>");
	Response.Write("<td align=right  width=10%>" + dCostTotal.ToString("c") + "</td>");
	Response.Write("<td align=right  width=10%>" + dProfitTotal.ToString("c") + "</td>");
	Response.Write("<td align=right  width=8% nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");	
	Response.Write("</table>");

}


</script>

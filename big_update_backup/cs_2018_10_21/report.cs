<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>
<!-- #include file="page_index.cs" -->
<!-- #include file="piecharts.cs" -->
<!-- #include file="r_items.cs" -->
<!-- #include file="r_itemd.cs" -->
<!-- #include file="r_cust.cs" -->
<!-- #include file="r_sales.cs" -->
<!-- #include file="r_itemd_cust.cs" -->
<!-- #include file="r_sinvoice.cs" -->

<script runat=server>

string m_branchID = "";
string m_type = "0";
string m_tableTitle = "Item Summary";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
string m_rtype="";
string m_itype ="";
string m_ctype = "";
DataRow[] m_dra = null;
string[] m_EachMonth = new string[16];

string m_uri = ""; 
string m_sType_Value = "0";
string m_brand = "";
string m_cat = "";
string m_scat = "";
string m_sscat = "";

string m_sorted = "DESC";

//xml draw chart
string[] sct = new string[16];

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
int m_ct = 1;
int cts = 0;
//--------

string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
int m_nPeriod = 0;

string m_sales_id = "";
string m_sales_person = "";

string m_option = "1"; //1 for sales, 2 for purchase

bool m_bShowPic = true;
bool m_bPickTime = false;
bool m_bSltBoth = false; //item details and customer are selected
bool m_bGBSetShowPicOnReport = true;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	m_bGBSetShowPicOnReport = MyBooleanParse(GetSiteSettings("set_display_chart_on_report", "1", true));
    m_rtype= Request.QueryString ["o"];
	m_itype = Request.QueryString["it"];
	m_ctype = Request.QueryString ["cu"];
	m_uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate();
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
	
	if(Request.QueryString["st"] == "ASC")
		m_sorted = "DESC";
	
	if(Request.QueryString["st"] == "DESC")
		m_sorted = "ASC";
	
	if(Request.QueryString["s"] != null && Request.QueryString["s"] == "")
		m_option = Request.QueryString["s"];

		//display sales invoice list / customer invoice list
	if((Request.QueryString["sid"] != null || Request.QueryString["sid"] == "") 
		|| (Request.QueryString["cid"] != null || Request.QueryString["cid"] == "") )
	{
		PrintAdminHeader();
		PrintAdminMenu();
		DoInvoiceList();
		return;
	}
	string type2 = "";
	if(Request.Form["cmd"] == "View Report")
	{
		if(Request.Form["type0"] != null && Request.Form["type0"] != "")
			m_type = Request.Form["type0"];
		if((Request.Form["type1"] != null && Request.Form["type1"] != "") || (Request.Form["type2"] != null && Request.Form["type2"] != ""))
		{
			if(Request.Form["type1"] != null && Request.Form["type1"] != "")
				m_type = Request.Form["type1"];
			if(Request.Form["type2"] != null && Request.Form["type2"] != "")
				type2 = Request.Form["type2"];

			if(type2 != "" && m_type == "0")
				m_type = Request.Form["type2"];
			if(type2 == "2" && m_type == "1")
				m_bSltBoth = true;

		}
		if(Request.Form["type3"] != null && Request.Form["type3"] != "")
			m_type = Request.Form["type3"];
		//vin add
		if(Request.Form["type4"] != null && Request.Form["type4"] != "")
			m_type = Request.Form["type4"];
		if(Request.Form["type5"] != null && Request.Form["type5"] != "")
			m_type = Request.Form["type5"];
	
	}
	//m_type = Request.QueryString["t"];

	int day = 1;
	int month = DateTime.Now.Month;
	int year = DateTime.Now.Year;

/*	if(m_bPickTime)
	{
		Calendar1.Visible = true;
	}
	else
	{
		Calendar1.Visible = false;
	}
*/	
    if((Request.QueryString["card"] == "all" || Request.QueryString["card"] != null && Request.QueryString["card"] != "")
	|| (Request.Form["cmd"] == "Search") || (Request.Form["search"] != null &&  Request.Form["search"] != "") )

	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(!doShowCustomer())
			return;
		return;
	}
/*	if(!IsPostBack)
	{
		Calendar1.VisibleDate = DateTime.Now;

		string startDate = month.ToString() + "/01/" + year.ToString();
		Session["sr_start_date"] = startDate;
		Session["sr_day"] = day;
		Session["sr_month"] = month;
		Session["sr_year"] = year;
	}
*/
	
	if(Request.Form["cmd"] == "View Report")
	{
		m_sales_id = Request.Form["employee"].ToString();
		if(!doShowEmployee())
			return;
		if(Request.Form["employee"] == "all")
			m_sales_person = "all";
		Session["slt_code"] = null;
	
	}
	
	if(Request.Form["cmd"] == "Cancel")
	{
		Session["customer_id"] = "all";
		Session["customer_name"] = "All";
		Response.Write(Session["custoemr_id"]);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+" \">");
	}
	
	if(Request.Form["code"] == null && Request.Form["cmd"] == null)
	{
		if(Request.QueryString["type"] == null || Request.QueryString["type"] == "")
		{
			PrintMainPage();
			LFooter.Text = m_sAdminFooter;
			return;
		}
		m_type = Request.QueryString["type"];
		if(Session["report_period"] != null)
		{
			m_nPeriod = (int)(Session["report_period"]);
			if(m_nPeriod == 3) //select range
			{
				if(Session["report_date_from"] != null)
					m_sdFrom = Session["report_date_from"].ToString();
				if(Session["report_date_to"] != null)
					m_sdTo = Session["report_date_to"].ToString();
			}
		}
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
		m_sdFrom = Request.Form["Datepicker1_day"] + "-" + Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		m_sdTo = Request.Form["Datepicker2_day"] + "-" + Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
//DEBUG("m_sdFrom= ", m_sdFrom);
		//m_sdFrom = Request.Form["day_from"] + "/" + Request.Form["month_from"];
		//m_sdTo = Request.Form["day_to"] + "/" + Request.Form["month_to"];
		Session["report_date_from"] = m_sdFrom;
		Session["report_date_to"] = m_sdTo;
	}
	if(Request.Form["pick_month1"] != null)
	{
		m_smFrom = Request.Form["pick_month1"];
		m_smTo = Request.Form["pick_month2"];
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];
	
	}
	if(Request.QueryString["period"] != null && Request.QueryString["period"] != "")
		m_nPeriod = int.Parse(Request.QueryString["period"].ToString());
	if(Request.QueryString["frm"] != null && Request.QueryString["frm"] != "")
	{
		m_sdFrom = Request.QueryString["frm"].ToString();
		m_sdTo = Request.QueryString["to"].ToString();
	}

	Session["report_period"] = m_nPeriod;
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
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_smFrom)-1] + "-"+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_smTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}
	
	PrintAdminHeader();
	if(!g_bPDA)
		PrintAdminMenu();

	/*if(Request.Form["code"] != "")
	{
		DoItemDetails();
		LFooter.Text = m_sAdminFooter;
		return;
	}*/
	
	int type = MyIntParse(m_type);
	if(m_bSltBoth)
		DoCustomerItemDetails();
	else
	{
		switch(type)
		{
		case 0:
			DoItemSummary();
			break;
		case 1:
			DoItemDetails();
			break;
		case 2:
			DoCustomerSummary();
			break;
		case 3:
			DoSalesPersonSummary("");
			break;
		//vin add
		case 4:
			DoSalesPersonSummary("Manager");
			break;
		case 5:
			DoItemCatagorySummary();
			break;
		default:
			break;
		}
	}
	
	LFooter.Text = m_sAdminFooter;
}
//add at 7-4-2004 by vin
string getSalesManager()
{
	string sc = "SELECT sales FROM card WHERE sales != '' GROUP BY sales";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "salesmanager");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	string output=" in (";
	
	for(int i=0; i < ds.Tables["salesmanager"].Rows.Count-1; i++)
	{
		output += ds.Tables["salesmanager"].Rows[i]["sales"].ToString()+",";
	}
	output += ds.Tables["salesmanager"].Rows[ds.Tables["salesmanager"].Rows.Count-1]["sales"].ToString()+")";
	
	return output;
}

bool doShowCustomer()
{
	string search = "";
	if(Request.Form["search"] != null && Request.Form["search"] != "")
		search = Request.Form["search"];
	string sc = " SELECT name, trading_name, company, contact, phone, id, email ";
	sc += " FROM card WHERE 1=1";
	if(Request.QueryString["card"] != "all" && Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		sc += " AND id = "+ Request.QueryString["card"];
	if(search != null && search != "")
	{
		if(TSIsDigit(search))
			sc += " AND id = "+ search;
		
		else
		{
			sc += " AND phone LIKE '%"+ search +"%' OR name LIKE '%"+ search +"%' OR trading_name LIKE '%"+ search +"%' ";
			sc += " OR company LIKE '%"+ search +"%' OR email LIKE '%"+ search +"%' OR contact LIKE '%"+ search +"%' ";
		}
	}
	else 
		sc += " ";
	int rows = 0;
	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows =	myAdapter.Fill(ds, "card"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG("rows = ", rows );
	if(rows == 1)
	{
		Session["customer_name"] = ds.Tables["card"].Rows[0]["name"].ToString();
		if(Session["customer_name"] == "")
			Session["customer_name"] = ds.Tables["card"].Rows[0]["company"].ToString();
		Session["customer_id"] = ds.Tables["card"].Rows[0]["id"].ToString();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?branch="+m_branchID+        "&o=1&cu=1&it="+m_itype+" \">");
	}
	
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	rows = ds.Tables["card"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;
	m_cPI.URI = "report.aspx?card=all";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	string uri = "report.aspx?card=";
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center width=80% cellspacing=1 cellpadding=1 bordercolor=#AAEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=3>"+sPageIndex+"</td><td colspan=2><input type=text name=search value='"+ search +"'>");
	Response.Write("<input type=submit name=cmd value='Search'"+Session["button_style"]+">");
	Response.Write("<input type=submit name=cmd value='Cancel'"+Session["button_style"]+"></td></tr>");

	Response.Write("<tr bgcolor=#AAEEEE ><th>Id</th><th>Name</th><th>Trading Name</th><th>Email</th><th>Company</th><th>Phone</th></tr>");
	bool bAlter = true;
	for(; i<rows && i<end; i++)
	{	
		DataRow dr = ds.Tables["card"].Rows[i];
		string name = dr["name"].ToString();
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string company = dr["company"].ToString();
		string phone = dr["phone"].ToString();
		string contact = dr["contact"].ToString();
		string email = dr["email"].ToString();
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(">");
		bAlter = !bAlter;
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"' class=o>"+id+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"' class=o>"+name+"<a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"' class=o>"+trading_name+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"' class=o>"+email+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"' class=o>"+company+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"' class=o>"+phone+"</a></td>");
		Response.Write("<tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");

	return true;
}

bool doShowEmployee()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id, sales ";
	sc += " FROM card ";
	sc += " WHERE type = 4";
	if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
	{
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
void PrintReportHeader(string sExtra)
{
	Response.Write("<br><center><h2><font color=#656565>" + m_tableTitle + "</font></h2>");
	Response.Write(sExtra);
	if(Session["branch_support"] != null)
	{
		Response.Write("<font color=#656565><b>" + Lang("Branch") + ": ");
		if(TSIsDigit(m_branchID) && m_branchID != "All" && m_branchID != "0")
			 Response.Write(GetBranchName(m_branchID));
		else
			Response.Write(Lang("ALL"));
	}
	Response.Write(" &nbsp; &nbsp; &nbsp; " + Lang("Date Period") + " : " + m_datePeriod + "</b></font><br><br>");
}
void PrintReportHeaderPDA(string sExtra)
{
	Response.Write("<font color=#656565><b>" + m_tableTitle + "</b></font> ");
	Response.Write(sExtra);
	if(Session["branch_support"] != null)
	{
		Response.Write("<font color=#656565><b>" + Lang("Branch") + ": ");
		if(TSIsDigit(m_branchID) && m_branchID != "All")
			 Response.Write(GetBranchName(m_branchID));
		else
			Response.Write(Lang("ALL"));
	}
	Response.Write(" " + Lang("Date Period") + " : " + m_datePeriod + "</b></font><br>");
}
void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
	
	Response.Write("<form name=f action=report.aspx method=post>");
	Response.Write("<center><h3>Select Report</h3>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<b>Branch : </b>");
		PrintBranchNameOptions(m_branchID, "report.aspx?branch=", true);
	}

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Report Type</b></td></tr>");
	//string uri = Request.ServerVariables["URL"].ToString(); 
	string uri = "report.aspx";
           uri += "?branch="+m_branchID+"&o=1&it=1&cu="+m_ctype+"";

	Response.Write("<tr><td colspan=6><input type=checkbox name=type5 value=5 ");
	Response.Write(" style='visibility:hidden' ");
	if (m_rtype =="5" )
		Response.Write ("checked");
	Response.Write(" onclick=\"if(this.checked){document.f.type1.checked=false; document.f.type2.checked=false; document.f.type0.checked=false;document.f.type3.checked=false;document.f.type4.checked=false;}\"");
	Response.Write(">");
	DoItemOption();
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=6><input type=checkbox name=type0 value=0 ");
	if (m_rtype =="0")
		Response.Write ("checked");
	Response.Write(" onclick=\"if(this.checked){document.f.type1.checked=false; document.f.type2.checked=false; document.f.type3.checked=false;document.f.period[4].checked=false;document.f.period[0].checked=true;document.f.type5.checked=false;document.f.type4.checked=false;}\"");
	Response.Write(" >Item Summary</td></tr>");
	
	Response.Write("<tr><td colspan=2><input type=checkbox name=type1 value=1 ");
	if (m_rtype =="1" && m_itype =="1"){
	Response.Write ("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type0.checked=false;  document.f.type3.checked=false;document.f.period[4].checked=false;document.f.period[0].checked=true;document.f.type5.checked=false;document.f.type2.checked=false;document.f.type4.checked=false;}\"");
	Response.Write(">Item Details &nbsp&nbsp&nbsp&nbsp;");
	//sponse.Write("<b>Item Code : </b><input type=text size=10 name=code></td></tr>");
	Response.Write("<b>Item Code : </b><select name=code ");
	//Response.Write(" onclick=\"window.location=('slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"?branch="+m_branchID+"&o=1&it=1&cu="+m_ctype+"')\" ");
	Response.Write(" onclick=\"window.location=('slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"')\" ");
	Response.Write(" >");
	if(Session["slt_code"] != null)
		Response.Write("<option value='"+ Session["slt_code"] +"'> "+Session["slt_code"] +" </option>");

	//Response.Write("<option value='all>ALL</option>");
	Response.Write("</select>\r\n");
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td colspan=6><input type=checkbox name=type2 value=2 ");
	if (m_rtype =="1" && m_ctype=="1"){
	Response.Write ("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type0.checked=false; document.f.type3.checked=false;document.f.period[4].checked=false;document.f.period[0].checked=true; document.f.type4.checked=false;document.f.type5.checked=false;document.f.type1.checked=false;}\"");
	Response.Write(">Customer Summary&nbsp;");
	Response.Write("<select name='card_id' ");
	//if(Session["customer_id"] != null)
	//	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?card='+ this.options[this.selectedIndex].value);\"");
	//else
	Response.Write(" onclick=\"window.location=('" + Request.ServerVariables["URL"] + "?card=all&branch="+m_branchID+"&o=1&cu=1&it="+m_itype+"')\" ");
	Response.Write(" >");
	//if(Session["customer_id"] != null)
		Response.Write("<option value='"+ Session["customer_id"] +"'> "+ Session["customer_name"] +"");

	//Response.Write("<option value='all'>All");
	Response.Write("</select></td></tr>");

	//DEBUG("customer =", Session["customer_id"].ToString());
	//Response.Write("<tr><td colspan=6><input type=radio name=type value=2>Customer Summary</td></tr>");
	//Response.Write("<tr><td colspan=6><input type=radio name=type value=3>Sales Person</td></tr>");
	Response.Write("<tr><td colspan=6><input type=checkbox name=type3 value=3 ");
	if (m_rtype =="3" ){
	Response.Write ("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type1.checked=false; document.f.type2.checked=false; document.f.type0.checked=false; document.f.type4.checked=false;document.f.type5.checked=false;}\"");
	Response.Write(">Sales Person ");

	Response.Write("<select name=employee><option value='all'>all");
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
	
	//show sales manager code at :7-4-2004 by vin
	Response.Write("<input type=checkbox name=type4 value=4 ");
	Response.Write(" onclick=\"if(this.checked){document.f.type1.checked=false; document.f.type2.checked=false; document.f.type0.checked=false;document.f.type3.checked=false;document.f.type5.checked=false;}\"");
	Response.Write("> Sales Manager");
	
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>This Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Last Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=2>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=3>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function
	//from date
	Response.Write("<tr><td><b> &nbsp; From Date </b>");
	//DateTime dstep = DateTime.Parse("01/01/2003");
	//DateTime dend = DateTime.Now;
	
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
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
//------ END first display date -----------

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
	//Response.Write("</td><td>&nbsp;<input type=submit nam=cmd value='Search' "+Session["button_style"].ToString()+"></td>");
	Response.Write("</tr>");
//------ END second display date -----------
	//Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=1><input type=radio name=period value=4 onclick=\"if(document.f.type3.checked){ this.checked=true;}else{this.checked=false;document.f.period[0].checked=true;window.alert('This Option is Not Allow');}\" >Compare Monthly Report</td>");//</tr>");
	//Response.Write("<tr><td colspan=2>");
	Response.Write("<th>From: <select name='pick_month1' onChange=\"document.f.period[4].checked=true;\">");
	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		txtMonth = m_EachMonth[m-1];
		Response.Write("<option value="+m+"");
		if(int.Parse(s_month) == m)
			Response.Write(" selected ");
		Response.Write(">"+txtMonth+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='pick_year1' onChange=\"document.f.period[4].checked=true;\">");
	for(int y=2000; y<int.Parse(s_year)+1; y++)
	{
		if(int.Parse(s_year) == y)
			Response.Write("<option value="+y+" selected>"+y+"</option>");
		else
			Response.Write("<option value="+y+">"+y+"</option>");
	}
	Response.Write("</select>");

	Response.Write(" To: <select name='pick_month2' onChange=\"document.f.period[4].checked=true;\">");
	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		txtMonth = m_EachMonth[m-1];
		Response.Write("<option value="+m+"");
		if(int.Parse(s_month) == m)
			Response.Write(" selected ");
		Response.Write(">"+txtMonth +"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='pick_year2' onChange=\"document.f.period[4].checked=true;\">");
	for(int y=2000; y<int.Parse(s_year)+1; y++)
	{
		if(int.Parse(s_year) == y)
			Response.Write("<option value="+y+" selected>"+y+"</option>");
		else
			Response.Write("<option value="+y+">"+y+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<tr><td align=right colspan=6><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	if(!g_bPDA)
		LFooter.Text = m_sAdminFooter;
}


void DrawChart()
{
	Stream chartFile = null;
	XmlDocument obXmlDoc = null;
	string uname = EncodeUserName();
	m_picFile = "./ri/" + uname + DateTime.Now.ToString("ddMMyyyyHHmmss") + "_datachart.jpg";
	string strFileName = m_picFile;
	
	doDeleteAllPicFiles();

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
		string sTitle = "";
		if(m_type == "0")
			sTitle = "Item Summary";
		if(m_type == "1")
			sTitle = "Item Details";
		if(m_type == "2")
			sTitle = "Customer Summary";
		if(m_type == "3")
			sTitle = "Sales Person Summary";
		if(m_bSltBoth)
			sTitle = "Customer Item Details";

		dc.SetGraphData (obXmlDoc);
		dc.Title = sTitle;
		for(int n=0; n<m_nIsland; n++)
			dc.IslandTitle[n] = m_IslandTitle[n];
		dc.HasTickers = false;
		dc.HasGridLines = true;
		dc.HasLegends = m_bHasLegends;
		dc.Width = 670;
		dc.Height = 500;
		dc.MinXValue = 0;
//		dc.XTickSpacing = 100;
		dc.MaxXValue = (int)m_nMaxX;
		dc.MinYValue = (int)m_nMinY;
		dc.MaxYValue = (int)m_nMaxY;
		dc.YAxisLabel = m_yLabel;
		dc.XAxisLabel = m_xLabel;
		dc.AxisPaddingTop = 190;
		dc.AxisPaddingBottom = 100;
		dc.YTickSpacing = (int)((m_nMaxY - m_nMinY)/ 10);
		dc.XTickSpacing = 1500;
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
	Response.Write(" onchange=\"window.location=('" + m_uri + "&o=5&sltype="+ m_sType_Value +"");
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
	Response.Write(" onchange=\"window.location=('" + m_uri + "&o=5&sltype="+ m_sType_Value +"");
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
		Response.Write(" onchange=\"window.location=('" + m_uri + "&o=5&sltype="+ m_sType_Value +"");
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
		Response.Write(" onchange=\"window.location=('" + m_uri + "&o=5&sltype="+ m_sType_Value +"");
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
///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item Catagory Summary
bool DoItemCatagorySummary()
{
	m_tableTitle = "Item Sales Summary For";
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
		//m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
	    m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59 "+"' ";
		break;
	case 4:
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
		break;
	default:
		break;
	}

//DEBUG("m_branc =", m_brand );
//DEBUG("m_branc =",  m_cat);
//DEBUG(" m_dateSql=", m_dateSql);

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT  sum(s.commit_price * s.quantity) AS sales_amount  ";
	sc += ", sum((s.commit_price - s.supplier_price) * s.quantity) AS rough_profit ";
	sc += ", sum(s.supplier_price * s.quantity) AS supplier_price, sum(s.quantity) AS sales_qty";
	sc += ", c.brand, c.cat, c.s_cat,c.ss_cat ";
	sc += " FROM sales s ";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN code_relations c ON s.code = c.code  ";
	sc += " WHERE 1=1 ";
	if(Session["branch_support"] != null && m_branchID != "0")
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
	sc += m_dateSql;
	sc += " GROUP BY c.brand, c.cat ,c.s_cat ,c.ss_cat ";
	
	sc += " ORDER BY sum(s.quantity) DESC ";
	
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
	
	return true;
}
void BindCatagorySummary()
{
		int i = 0;

	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
//DEBUG(" rows = ", rows);
	m_cPI.TotalRows = rows;
	
	m_cPI.URI = "?branch="+m_branchID+"&pr="+ m_nPeriod +"&type="+ m_type +"&r=" + DateTime.Now.ToOADate();
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
	Response.Write("<th align=right >SALES TOTAL QTY</th>");
	Response.Write("<th align=right >SALES TOTAL AMOUNT</th>");
	Response.Write("<th align=right >SALES TOTAL AMOUNT (Inl GST)</th>");
	Response.Write("<th align=right >ROUGH PROFIT</th>");
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
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalProfit = 0;
	int dTotalQTY = 0;
	bool bAlterColor = false;
	
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
	
		string brand = dr["brand"].ToString();
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		
		string sales_qty = dr["sales_qty"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		string profit = dr["rough_profit"].ToString();
		double salesAmountGST = MyDoubleParse(sales_amount)*1.15;
		salesAmountGST = Math.Round(salesAmountGST, 2);

		dTotalQTY += int.Parse(sales_qty);
		dTotalNoGST += MyDoubleParse(sales_amount);
		dTotalProfit += MyDoubleParse(profit);	
		
		
		
		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td>" + brand + "</td>");
		Response.Write("<td>" + cat + "</td>");
		Response.Write("<td>" + s_cat + "</td>");
		Response.Write("<td>" + ss_cat + "</td>");
		Response.Write("<td align=right>" + sales_qty + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(sales_amount).ToString("c") + "</td>");
		Response.Write("<td align=right>" + salesAmountGST.ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(profit).ToString("c")+ "</td>");

		Response.Write("</tr>");

		

		
			
	}


	Response.Write("<tr><td colspan=5></td></tr>");
	Response.Write("<tr bgcolor=#EEEEEE><th align=left colspan=4>SUB Total:</td><th>"+ dTotalQTY +"</td><th align=right>"+ dTotalNoGST.ToString("c") +"</th><th align=right>"+ (dTotalNoGST*1.15).ToString("c") +"</th><th align=right>"+ dTotalProfit.ToString("c") +"</th></tr>");

	dTotalNoGST = 0;
	dTotalProfit = 0;
	dTotalQTY = 0;
	
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string sales_qty = dr["sales_qty"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		string profit = dr["rough_profit"].ToString();

		dTotalQTY += int.Parse(sales_qty);
		dTotalNoGST += MyDoubleParse(sales_amount);
		dTotalProfit += MyDoubleParse(profit);
	}
	Response.Write("<tr bgcolor=#EEE54E><th align=left colspan=4>GRAND TOTAL:</td><th>"+ dTotalQTY +"</td><th align=right>"+ dTotalNoGST.ToString("c") +"</th><th align=right>"+ (dTotalNoGST*1.15).ToString("c") +"</th><th align=right>"+ dTotalProfit.ToString("c") +"</th></tr>");
	Response.Write("<tr><td colspan=8>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

		//write xml data file for chart image
	//WriteXMLFile();
}
</script>

<asp:Label id=LFooter runat=server/>
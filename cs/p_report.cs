<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>
<!-- #include file="page_index.cs" -->
<!-- #include file="rp_items.cs" -->
<!-- #include file="rp_itemd.cs" -->
<!-- #include file="rp_supplier.cs" -->
<!-- #include file="rp_purchase.cs" -->
<!-- #include file="rp_supp_purchase.cs" -->
<!-- #include file="rp_pinvoice.cs" -->

<script runat=server>

string m_branchID = "1";
string m_type = "0";
string m_tableTitle = "Item Summary";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
DataRow[] m_dra = null;

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
string[] m_EachMonth = new string[12];
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
//---------------
string p_iType ="";
string p_sType ="";
string p_tType ="";
//------------
string m_sales_id = "";
string m_sales_person = "";
string m_option = "1"; //1 for sales, 2 for purchase
string m_filter = "";
string m_filter_type = "";

bool m_bShowPic = true;
bool m_bPickTime = false;
bool m_bSltBoth = false; //item details and customer are selected
bool m_bCompair = false;
bool m_bGBSetShowPicOnReport = true;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	m_bGBSetShowPicOnReport = MyBooleanParse(GetSiteSettings("set_display_chart_on_report", "1", true));
    p_iType = Request.QueryString["pi"];
	p_sType = Request.QueryString["su"];
	p_tType = Request.QueryString["pt"];
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
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
	
	if(Request.QueryString["s"] != null && Request.QueryString["s"] == "")
		m_option = Request.QueryString["s"];

	string type2 = "";

		//display sales invoice list / customer invoice list
	if((Request.QueryString["spid"] != null || Request.QueryString["spid"] == ""))
	{
		PrintAdminHeader();
		PrintAdminMenu();
		DoPurchaseInvoiceList();
		return;
	}

	if(Request.Form["cmd"] == "View Report")
	{
		if(Request.Form["txt_search"] != null && Request.Form["txt_search"] != "")
		{
			m_filter = Request.Form["txt_search"];
			m_filter_type = Request.Form["slt_search"];
		}

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
		return;
	}
	
	if((Request.QueryString["card"] == "all" || Request.QueryString["card"] != null && Request.QueryString["card"] != "")
		|| (Request.Form["cmd"] == "Search") || (Request.Form["search"] != null &&  Request.Form["search"] != "") )

	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(!doShowCustomer())
			return;
		return;
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
		if((Request.QueryString["frm"] != null && Request.QueryString["frm"] != "") && Request.QueryString["pr"] != null && Request.QueryString["pr"] != "")
		{
			m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());
			m_sdFrom = Request.QueryString["frm"].ToString();
			m_sdTo = Request.QueryString["to"].ToString();
			if(Request.QueryString["s"] != "" && Request.QueryString["s"] != null)
				m_sales_person = Request.QueryString["s"].ToString();
		}
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
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_smFrom)-1] +"-"+ m_syFrom +" </font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_smTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	
	int type = MyIntParse(m_type);
	
	if(m_bSltBoth)
		DoSupplierItemDetails();
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
			DoSupplierSummary();
			break;
		case 3:
			DoPurchaseSummary();
			break;
		default:
			break;
		}
	}
	
	LFooter.Text = m_sAdminFooter;
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
	if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		sc += " AND type = "+ Request.QueryString["t"];
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
		
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+" \">");
		return true;
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
	m_cPI.URI = Request.ServerVariables["URL"]+"?card=all";
	if(Request.QueryString["t"] != "" && Request.QueryString["t"] != null)
		m_cPI.URI += "&t="+ Request.QueryString["t"];
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	string uri = Request.ServerVariables["URL"] +"?card=";
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center width=80% cellspacing=0 cellpadding=2 border=0 bordercolor=#DDAAAE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=3>"+sPageIndex+"</td><td colspan=2><input type=text name=search value='"+ search +"'>");
	Response.Write("<input type=submit name=cmd value='Search'"+Session["button_style"]+">");
	Response.Write("<input type=submit name=cmd value='Cancel'"+Session["button_style"]+"></td></tr>");

	Response.Write("<tr bgcolor=#DDAAAE ><th>Id</th><th>Name</th><th>Trading Name</th><th>Email</th><th>Company</th><th>Phone</th></tr>");
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
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+"' class=o>"+id+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+"' class=o>"+name+"<a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+"' class=o>"+trading_name+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+"' class=o>"+email+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+"' class=o>"+company+"</a></td>");
		Response.Write("<td><a title='choose me' href='"+uri+id+"&branch="+m_branchID+"&pt=1&su=1&pi="+p_iType+"' class=o>"+phone+"</a></td>");
		Response.Write("<tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");

	return true;
}

bool doShowEmployee()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id ";
	sc += " FROM card ";
	sc += " WHERE type = 4";
	if(Session["branch_support"] != null)
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

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"' method=post>");

	Response.Write("<center><h3>Select Purchase Report</h3>");

	if(Session["branch_support"] != null)
	{
		Response.Write("<b>Branch : </b>");
		PrintBranchNameOptions(m_branchID, "p_report.aspx?branch=");
	}

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Report Type</b></td></tr>");
//	string uri = Request.ServerVariables["URL"].ToString();
	string uri = "p_report.aspx";
           uri += "?branch="+m_branchID+"&pi=1&pt=1&su="+p_sType+"";
	Response.Write("<tr><td colspan=6><input type=checkbox name=type0 value=0 ");
	if(p_tType =="0"){
	Response.Write("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type1.checked=false; document.f.type2.checked=false; document.f.type3.checked=false; document.f.period[4].checked=false; document.f.period[0].checked=true;}\"");
	Response.Write(" >Item Summary</td></tr>");
	
	Response.Write("<tr><td colspan=2><input type=checkbox name=type1 value=1 ");
	if(p_tType =="1" && p_iType =="1"){
	Response.Write("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type0.checked=false;  document.f.type3.checked=false;document.f.period[4].checked=false;document.f.period[0].checked=true;}\"");
	Response.Write(">Item Details &nbsp&nbsp&nbsp&nbsp;");
	//sponse.Write("<b>Item Code : </b><input type=text size=10 name=code></td></tr>");
	Response.Write("<b>Item Code : </b><select name=code ");
	Response.Write(" onclick=\"window.location=('slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"&branch="+m_branchID+"')\" ");
	Response.Write(" >");
	if(Session["slt_code"] != null)
		Response.Write("<option value='"+ Session["slt_code"] +"'> "+Session["slt_code"] +" </option>");

	//Response.Write("<option value='all>ALL</option>");
	Response.Write("</select>\r\n");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6>");
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Filter With : <select name=slt_search>");
	Response.Write("<option value='pi.id'>Purchase ID</option>");
	Response.Write("<option value='p.po_number'>PONumber</option>");
	Response.Write("<option value='pi.name'>Product Name</option>");
	Response.Write("<input type=text name=txt_search value='"+ Request.Form["txt_search"] +"'></td></tr>");

	Response.Write("<tr><td colspan=6><input type=checkbox name=type2 value=2 ");
		if(p_tType =="1" && p_sType =="1"){
	Response.Write("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type0.checked=false; document.f.type3.checked=false;}\"");
	Response.Write(">Supplier Summary &nbsp;");
	Response.Write("<select name='card_id' ");
	//if(Session["customer_id"] != null)
	//	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?card='+ this.options[this.selectedIndex].value);\"");
	//else
	Response.Write(" onclick=\"window.location=('" + Request.ServerVariables["URL"] + "?card=all&branch="+m_branchID+"&t=3&pt=1&su=1&pi="+p_iType+"')\" ");
	Response.Write(" >");
	//if(Session["customer_id"] != null)
		Response.Write("<option value='"+ Session["customer_id"] +"'> "+ Session["customer_name"] +"");

	//Response.Write("<option value='all'>All");
	Response.Write("</select></td></tr>");

	//DEBUG("customer =", Session["customer_id"].ToString());
	//Response.Write("<tr><td colspan=6><input type=radio name=type value=2>Customer Summary</td></tr>");
	//Response.Write("<tr><td colspan=6><input type=radio name=type value=3>Sales Person</td></tr>");
	Response.Write("<tr><td colspan=6><input type=checkbox name=type3 value=3 ");
	if(p_tType =="3"){
	Response.Write("checked");
	}
	Response.Write(" onclick=\"if(this.checked){document.f.type1.checked=false; document.f.type2.checked=false; document.f.type0.checked=false;}\"");
	Response.Write(">Sales Person ");
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
	Response.Write("<tr><td colspan=1><input type=radio name=period value=4 ");
	Response.Write(" onclick=\"if(document.f.type3.checked){ this.checked=true;}else if (document.f.type2.checked && !document.f.type1.checked)");
	Response.Write("{ this.checked=true;}else{this.checked=false; document.f.period[0].checked=true;}\" >");
	Response.Write("Compare Monthly Report</td>");//</tr>");
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
		Response.Write(">"+txtMonth+"</option>");
		
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
	LFooter.Text = m_sAdminFooter;
}

void DrawChart()
{
	Stream chartFile = null;
	XmlDocument obXmlDoc = null;
	string uname = EncodeUserName();
	m_picFile = "./ri/" + uname + DateTime.Now.ToString("ddMMyyyyHHmmss") + "_datachart.jpg";
	string strFileName = m_picFile;

//delete all old pics file
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
			sTitle = "Purchase Item Summary";
		if(m_type == "1")
			sTitle = "Purchase Item Details";
		if(m_type == "2")
			sTitle = "Supplier(s) Summary";
		if(m_type == "3")
			sTitle = "Sales Person Purchase Summary";
		if(m_bSltBoth)
			sTitle = "Suppliers' Item Details";

		dc.SetGraphData (obXmlDoc);
		dc.Title = sTitle;
		for(int n=0; n<m_nIsland; n++)
			dc.IslandTitle[n] = m_IslandTitle[n];
		dc.HasTickers = false;
		dc.HasGridLines = true;
		dc.HasLegends = m_bHasLegends;
		//dc.Width = 900;
		dc.Width = 700;
		dc.Height = 450;
		dc.MinXValue = 0;
		dc.XTickSpacing = 100;
		dc.MaxXValue = (int)m_nMaxX;
		dc.MinYValue = (int)m_nMinY;
		
		dc.MaxYValue = (int)m_nMaxY;
		dc.YAxisLabel = m_yLabel;
		dc.XAxisLabel = m_xLabel;
		dc.AxisPaddingTop = 150;
		//dc.AxisPaddingTop = 220;
		dc.AxisPaddingBottom = 70;
		//dc.AxisPaddingBottom = 60;
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


/*void Selection_Change(Object sender, EventArgs e) 
{
	int day = Calendar1.SelectedDate.Day;
	int month = Calendar1.SelectedDate.Month;
	int year = Calendar1.SelectedDate.Year;

	Session["sr_day"] = day;
	Session["sr_month"] = month;
	Session["sr_year"] = year;

	string startDate = Calendar1.SelectedDate.ToShortDateString();
	Session["sr_start_date"] = startDate;
	Calendar1.SelectedDates.Clear();
}
*/

</script>

<asp:Label id=LFooter runat=server/>

<!-- #include file="page_index.cs" -->
<script runat=server>

string m_type = "0";
string m_tableTitle = "Item Search";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;
string[] m_EachMonth = new string[13];
string m_sdFrom = "";
string m_sdTo = "";
string m_search = "";
int m_nPeriod = 0;

bool m_bPickTime = false;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] sct = new string[16];

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
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
	
	if(Request.Form["search"] != "" || Request.Form["cmd"] == "View Report")
		m_search = Request.Form["search"];
	if(Session["slt_code"] != null)
		m_search = Session["slt_code"].ToString();
//DEBUG("m_search =", m_search);
	m_type = Request.QueryString["t"];
	if(Request.Form["type"] != null)
		m_type = Request.Form["type"];
	
	
	if(Request.QueryString["d"] != "" && Request.QueryString["d"] != null)
	{		
		m_sdFrom = Request.QueryString["d"];
		
		PrintAdminHeader();
		PrintAdminMenu();
		DoReturnItem();
		PrintAdminFooter();
		return;
	}

	if(Request.QueryString["ra"] != null && Request.QueryString["ra"] != "")
	{	
		m_search = Request.QueryString["ra"];
		PrintAdminHeader();
		PrintAdminMenu();
		DoReturnItem();
		PrintAdminFooter();
		return;
	}
	if(Request.Form["cmd"] == null && Request.QueryString["period"] == null)
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

	if(Request.QueryString["period"] != null && Request.QueryString["period"] != "")
		m_nPeriod = int.Parse(Request.QueryString["period"].ToString());

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
	if(Request.Form["type"] != null)
		m_type = Request.Form["type"];
	
	if(Request.Form["Datepicker1_day"] != null)
	{
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	if(Request.QueryString["dfrm"] != null)
	{
		m_sdFrom = Request.QueryString["dfrm"];
		m_sdTo = Request.QueryString["dto"];
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

	if(m_sdTo != "")
	{
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		m_sdTo = dTo.AddDays(1).ToString("dd-MM-yyyy");
	}


	//switch(MyIntParse(m_type))
	//{
	//case 0:
		PrintAdminHeader();
		PrintAdminMenu();
		DoReturnItem();
		PrintAdminFooter();
	//	break;

	//default:
	//	break;
	//}
}


void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();
	
	string uri = Request.ServerVariables["URL"];
	
	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"]+"' method=post>");

	Response.Write("<br><center><h3>Trace RMA ITEM Report</h3>");
	
	Response.Write("<table align=center cellspacing=1 cellpadding=2 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><th align=left>SEARCH RETURN ITEM: <input type=text name=search value="+ m_search +">");
	Response.Write("<a title='Get Product Code, Supplier Code, Description' href='slt_item.aspx?uri="+ uri +"?'>...</a>");
	Response.Write("</td></tr>");
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
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
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
	Response.Write("&nbsp; TO: ");
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

	Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");

}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool DoReturnItem()
{
	m_tableTitle = "RMA ITEM TRACE REPORT";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, rp.repair_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, rp.repair_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, rp.repair_date, GETDATE()) >= 1 AND DATEDIFF(month, rp.repair_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND rp.repair_date >= '" + m_sdFrom + "' AND rp.repair_date <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT  rp.code, rp.prod_desc AS name, rp.supplier_code,  c1.name AS staff_name ";
	sc += ",rs.id, rp.id AS repair_id, rp.ra_number, r.ra_id, rs.action_desc, rs.replaced_date, rs.old_sn ";
	sc += ", rp.replaced, rp.repaired, rp.finished, rp.repair_finish_date, rp.technician ";
	sc += ", rf.ship_desc ";
//	sc += " FROM return_sn rs LEFT OUTER JOIN code_relations c ON c.code = rs.code ";
	
	sc += " FROM repair rp LEFT OUTER JOIN return_sn rs ON rs.repair_id = rp.id ";
	sc += " LEFT OUTER JOIN rma r ON r.id = rs.ra_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = rs.staff ";
	sc += " LEFT OUTER JOIN ra_freight rf ON rf.repair_number = rp.id ";
//	sc += " LEFT OUTER JOIN code_relations c ON c.code = rp.code ";

/*	sc += " FROM return_sn rs ";
	sc += " JOIN card c1 ON c1.id = rs.staff ";
//	sc += " serial_trace t LEFT OUTER JOIN return_sn rs ON (rs.old_sn = t.sn OR rs.replaced_sn = t.sn ) ";
//	sc += " LEFT OUTER JOIN ra_replaced rr ON (rr.sn = t.sn OR rr.sn = rs.old_sn OR rr.sn = rs.replaced_sn )";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = rs.code ";
	sc += " LEFT OUTER JOIN rma r ON r.id = rs.ra_id ";
	sc += " LEFT OUTER JOIN repair ON repair.id = rs.ra_id ";
*/
	sc += " WHERE 1=1 ";
//DEBUG("m_sech =", m_search);
	if(m_search != "" && m_search != null)
	{
		if(TSIsDigit(m_search))
			sc += " AND (rs.code = '"+ m_search +"' OR rs.repair_id = "+ m_search +" OR rs.old_sn LIKE '%"+ EncodeQuote(m_search) +"%' OR t.sn LIKE '%"+ EncodeQuote(m_search) +"%' ) ";
		else
		{
			sc += " AND (rs.old_sn LIKE '%"+ EncodeQuote(m_search) +"%' OR rs.replaced_sn LIKE '%"+ EncodeQuote(m_search) +"%' OR t.sn LIKE '%"+ EncodeQuote(m_search) +"%' OR rr.sn LIKE '%"+ EncodeQuote(m_search) +"%' ";
			sc += " OR rr.description LIKE '%"+ EncodeQuote(m_search) +"%' OR rr.supp_code LIKE '%"+ EncodeQuote(m_search) +"%' ";
			sc += " ) ";
		}
	}
	
	if(m_sdFrom != "" && m_sdTo == "")
		sc += " AND rp.repair_date >= '"+ m_sdFrom +"'";
	else
		sc += m_dateSql;
	sc += " ORDER BY rp.ra_number, rp.code ";

//	sc += " ORDER BY rs.replaced_date  ";	
//DEBUG("sc + = ", sc );
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

	BindPRItem();


	return true;
}

/////////////////////////////////////////////////////////////////
void BindPRItem()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
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
	m_cPI.URI += "&period="+ m_nPeriod;
	if(m_nPeriod == 3 && m_sdFrom != "")
		m_cPI.URI += "&dfrm="+ HttpUtility.UrlEncode(m_sdFrom) +"&dto="+ HttpUtility.UrlEncode(m_sdTo) +"";
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	if(rows <= 0)
	{
		//Response.Write("<tr><td><input type=button value='New Search' "+ Session["button_style"] +"");
		Response.Write("<br><center><h4><font color=red>No Records Found</font></h4>");
		Response.Write("<br><input type=button value='New Search' "+ Session["button_style"] +"");
		Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\" >");
		//Response.Write("</td></tr>");
		//Response.Write("</table>");
		Session["slt_code"] = null;
		return;
	}
	Response.Write("<table width=90%  align=center valign=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

Response.Write("<tr><td colspan=10>" + sPageIndex + " ");
Response.Write(" | <a title='new trace' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"' class=o>New Search</a>");
Response.Write("</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>SN#</th>");
	Response.Write("<th>CODE</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>PRO_DESC</th>");
//	Response.Write("<th>SUPPLIER RMA#</th>");
	Response.Write("<th>RMA#</th>");
	Response.Write("<th>RP_DATE</th>");
	Response.Write("<th>RP_STAFF</th>");
	Response.Write("<th>ACTION_DESC</th>");
	Response.Write("<th>SHIPPING</th>");
	Response.Write("<th>FINISHED</th>");
//	Response.Write("<th>Margin</th>");
	Response.Write("</tr>");

	int margins = 0;

	bool bAlterColor = false;
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sn = dr["old_sn"].ToString();
//		string rp_date = dr["replaced_date"].ToString();
//		string staff = dr["staff_name"].ToString();
		string action = dr["action_desc"].ToString();
		string repair_id = dr["repair_id"].ToString();
		string ra_id = dr["ra_id"].ToString();
		string ra_number = dr["ra_number"].ToString();
		
		string s_qString1 = "techr.aspx?op=5&s=0&id="+ repair_id +"";
		string s_qString2 = "supp_rma.aspx?rma=rd&rid="+ ra_id +"";

		bool bReplaced = MyBooleanParse(dr["replaced"].ToString());
		if(bReplaced)
			action = "Replaced";
		bool bRepaired = MyBooleanParse(dr["repaired"].ToString());
		if(bRepaired)
			action = "Repaired";

		bool bFinished = MyBooleanParse(dr["finished"].ToString());
		string repair_finish_date = dr["repair_finish_date"].ToString();
		string staff = dr["technician"].ToString();
		string shipping = dr["ship_desc"].ToString();
		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a title='check sn#' href=\"javascript:sn_window=window.open('snsearch.aspx?sn="+ sn +"&r="+ DateTime.Now.ToOADate() +"', '', ''); sn_window.focus()\" class=o>" + sn + "</a></td>");
		Response.Write("<td><a href='stocktrace.aspx?p=0&c="+ code +"' class=o title='Click to trace item'>" + code + "</a></td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
//		Response.Write("<td><a href='"+ s_qString2 +"' class=o title='Click to view supplier rma details'>" + ra_id + "</a></td>");
		Response.Write("<td><a href='"+ s_qString1 +"' class=o title='Click to view rma details'>" + ra_number + "</a></td>");
		Response.Write("<td align=center>" + repair_finish_date + "</a></td>");
		Response.Write("<td>" + staff + "</td>");
		Response.Write("<td>" + action + "</td>");
		Response.Write("<td>" + shipping + "</td>");
		Response.Write("<td align=center><input type=checkbox name=finished");
		if(bFinished)
			Response.Write(" checked");
		Response.Write(" disabled></td>");
		
		//Response.Write("<td align=right>" +  + "</td>");
		Response.Write("</tr>");

	}
		
	Response.Write("<tr><td colspan=10>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Session["slt_code"] = null;

}

</script>

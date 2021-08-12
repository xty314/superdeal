<!-- #include file="page_index.cs" -->

<script runat=server>

string m_search = "";
string m_cust_query = "";
string m_supp_query = "";
string m_sdFrom = "";
string m_sdTo = "";

bool m_bFound = false;
bool bClicktoView = true;

string[] m_EachMonth = new string[13];

DataSet dst = new DataSet();
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
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
	
	if(Request.Form["cmd"] == "QUERY")
	{
		m_sdFrom = Request.Form["Datepicker1_day"] +"-"+ Request.Form["Datepicker1_month"] +"-"+ Request.Form["Datepicker1_year"];
		m_sdTo = Request.Form["Datepicker2_day"] +"-"+ Request.Form["Datepicker2_month"] +"-"+ Request.Form["Datepicker2_year"];
	}
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"].ToString();
		m_sdTo = Request.QueryString["to"].ToString();
	}
//DEBUG("msdform = ", m_sdFrom);
//DEBUG("msdto = ", m_sdTo);
	if(Request.Form["search"] != null && Request.Form["search"] != "" || Request.Form["cmd"] == "SEARCH")
		m_search = Request.Form["search"];
	if(Request.QueryString["ct"] != null && Request.QueryString["ct"] != "")
		m_cust_query = Request.QueryString["ct"];
	if(Request.QueryString["spl"] != null && Request.QueryString["spl"] != "")
		m_supp_query = Request.QueryString["spl"];
	if(!SecurityCheck("technician"))
		return;
	InitializeData();
	Response.Write("<br><center><h3><b>RMA LIST</b></h3></center>");
	RA_Menu();
	DoSearchRA();
}

bool DoSearchRA()
{
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT c.id AS cid, r.id, r.ra_number, r.repair_date, c.name, c.trading_name, c.company, 1 AS repair, r.status, e.name AS sname";
	sc += " FROM repair r JOIN card c ON c.id = r.customer_id ";
	sc += " JOIN enum e ON e.id = r.status AND e.class = 'rma_status' ";
	sc += " WHERE 1=1 ";
	if(m_search != "")
	{
		//if(TSIsDigit(m_search))
			sc += " AND ra_number = '"+ m_search +"'"; // OR r.invoice_number = "+ m_search;
		//else
			sc += " OR r.serial_number LIKE '%"+ m_search +"%' OR r.invoice_number = '"+ m_search +"' ";
	}
	else
		sc += " AND DATEDIFF(MONTH, r.repair_date, GETDATE()) <= 3 ";
	if(m_sdFrom != "" || m_cust_query != "")
	{
		if(m_sdFrom != "")
			sc += " AND r.repair_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		if(m_cust_query != "")
			sc += " AND r.status = "+ m_cust_query;
		if(m_sdFrom != "" && m_cust_query != "")
		{
			sc += " AND r.repair_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
			sc += " AND r.status = "+ m_cust_query;
		}
	}

	sc += " UNION ";
	sc += " SELECT c.id AS cid,  r.id, CONVERT(varchar(40), r.ra_id) AS ra_number, r.repair_date, c.name, c.trading_name, c.company, 2 AS repair, r.check_status AS status, '1' AS sname ";
	sc += " FROM rma r JOIN card c ON c.id = r.supplier_id ";
	sc += " LEFT OUTER JOIN enum e ON e.id = r.status AND e.class = 'rma_status' ";
	sc += " WHERE 1=1 ";
	if(m_search != "")
	{
		if(TSIsDigit(m_search))
			sc += " AND ra_id = "+ m_search +" ";// OR r.invoice_number = '"+ m_search +"'";
		else
			sc += " AND r.serial_number LIKE '%"+ m_search +"%'  OR r.invoice_number = '"+ m_search +"'";
	}
	else
		sc += " AND DATEDIFF(MONTH, r.repair_date, GETDATE()) <= 3 ";

	if(m_sdFrom != "" || m_supp_query != "")
	{
		if(m_sdFrom != "")
			sc += " AND r.repair_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		else if(m_supp_query != "")
			sc += " AND r.check_status = "+ m_supp_query;
		else if(m_sdFrom != "" && m_cust_query != "")
		{
			sc += " AND r.repair_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
			sc += " AND r.status = "+ m_cust_query;
		}
	}
	
//DEBUG(" sc +=", sc );
	sc += " ORDER BY r.repair_date DESC ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "list");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
//	if(rows <= 0)
//	{
//		Response.Write("<center><h4><font color=red>Nothing Found</font></h4></center>");
//		return false;
//	}
	
	if(rows > 0)
	{
		//paging class
		PageIndex m_cPI = new PageIndex(); //page index class
		if(Request.QueryString["p"] != null)
			m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
		if(Request.QueryString["spb"] != null)
			m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
			
		m_cPI.TotalRows = rows;
		m_cPI.PageSize = 25;
		m_cPI.URI = "?";
		if(m_sdFrom != "")
			m_cPI.URI += "frm="+ m_sdFrom+"&to="+ m_sdTo+"";
		int i = m_cPI.GetStartRow();
		int end = i + m_cPI.PageSize;
		string sPageIndex = m_cPI.Print();
		Response.Write("<table width=98% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=6>"+ sPageIndex +"</th></tr>");
		Response.Write("<tr align=left bgcolor=#DDDEEE><th>RA#</th><th>NAME</th><th>DATE</th><th>CUST/SUPP RA</th><th>STATUS</th><th></th></tr>");
		bool bAlter = false;
		for(; i<rows && i<end; i++)
		{
			DataRow dr = dst.Tables["list"].Rows[i];
			string cid = dr["cid"].ToString();
			string id = dr["id"].ToString();
			string ra = dr["ra_number"].ToString();
			string name = dr["name"].ToString();
			string status = dr["status"].ToString();
			string IsRMA = dr["repair"].ToString();
			string date = dr["repair_date"].ToString();
			string status_name = dr["sname"].ToString();
			if(name == "")
				name = dr["trading_name"].ToString();
			if(name == "")
				name = dr["company"].ToString();
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#FFFFF ");
			bAlter = !bAlter;
			Response.Write("><td>");
			Response.Write(ra);
			Response.Write("<td>");
			Response.Write("<a title='click me to view details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id="+ cid +"','', 'width=350,height=350'); viewcard_window.focus();\" class=o> ");
			Response.Write(name);
			Response.Write("</a>");
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(DateTime.Parse(date).ToString("dd-MMM-yyyy"));
			Response.Write("</td>");
			Response.Write("<td>");
			if(IsRMA == "1")
			{
				Response.Write(" CUSTOMER RA ");
				Response.Write("</td>");
				Response.Write("<td>");
				Response.Write(status_name);
			}
			if(IsRMA == "2")
			{
				Response.Write(" SUPPLIER RA ");
				Response.Write("</td>");
				Response.Write("<td>");
				
				if(status == "1")
					status_name = "RECORDED";
				if(status == "2")
					status_name = "SENT";
				if(status == "3")
					status_name = "RETURNED";
				Response.Write(status_name);
			}
			Response.Write("</td>");
			Response.Write("<td>");
			if(IsRMA == "1")
				Response.Write("<a title='process rma' href='techr.aspx?s=2&op="+ status +"&id="+ id +"' class=o>");
			if(IsRMA == "2")
			{
				if(int.Parse(status) >= 2)
					Response.Write("<a title='process rma' href='ra_process.aspx?id="+ ra +"' class=o>");
				else
					Response.Write("<a title='process rma' href='supp_rma.aspx?rma=rd&st="+ status +"&rid="+ ra +"' class=o>");
			}
			Response.Write("PROC");
			Response.Write("</a>");
			Response.Write("</td></tr>");
			
		}
	}
	Response.Write("</table>");
	Response.Write("</form><br>");
	return true;
}

void RAMenuOption()
{
	//string customer_ra_uri = "techr.aspx?s=2&op=";
	//string supplier_ra_uri = "supp_rma.aspx";
	string customer_ra_uri = ""+ Request.ServerVariables["URL"] +"?";
	if(m_sdFrom != "")
		customer_ra_uri += "frm="+ m_sdFrom +"&to="+ m_sdTo +"&";
	customer_ra_uri += "ct=";
	string supplier_ra_uri = ""+ Request.ServerVariables["URL"] +"?";
	if(m_sdFrom != "")
		supplier_ra_uri += "frm="+ m_sdFrom +"&to="+ m_sdTo +"&";
	supplier_ra_uri += "spl=";

	Response.Write("<img src='r.gif'> <select name=ra_menu1 onchange=\"window.location=(this.options[this.selectedIndex].value)\">");
	Response.Write("<option value='"+ customer_ra_uri +"'>Customer RMA</option>");
	Response.Write("<option value='"+ customer_ra_uri +"1'");
	if(m_cust_query == "1")
		Response.Write(" selected ");
	Response.Write(">Waiting For Processing</option>");
	Response.Write("<option value='"+ customer_ra_uri +"3'");
	if(m_cust_query == "3")
		Response.Write(" selected ");
	Response.Write(">Recieve/Repair</option>");
	Response.Write("<option value='"+ customer_ra_uri +"4'");
	if(m_cust_query == "4")
		Response.Write(" selected ");
	Response.Write(">Repair Done</option>");
	Response.Write("<option value='"+ customer_ra_uri +"5'");
	if(m_cust_query == "5")
		Response.Write(" selected ");
	Response.Write(">Finished</option>");
	Response.Write("</select>");
	Response.Write("&nbsp;<img src='r.gif'> <select name=ra_menu2 onchange=\"window.location=(this.options[this.selectedIndex].value)\">");
	Response.Write("<option value='"+ supplier_ra_uri +"'>Supplier RMA</option>");
	Response.Write("<option value='supp_rma.aspx'>New Supplier RMA</option>");
	Response.Write("<option value='supp_rma.aspx?cp=all'>Supplier RMA LIST</option>");
	//Response.Write("<option value='"+ supplier_ra_uri +"?rma=rd&ssp=all&st=1'>Processing</option>");
	//Response.Write("<option value='"+ supplier_ra_uri +"?rma=rd&ssp=all&st=2'>Waiting For Return</option>");
	//Response.Write("<option value='"+ supplier_ra_uri +"?rma=rd&ssp=all&st=3'>Returned RMA</option>");
	Response.Write("<option value='"+ supplier_ra_uri +"1'");
	if(m_supp_query == "1")
		Response.Write(" selected ");
	Response.Write(">Processing</option>");
	Response.Write("<option value='"+ supplier_ra_uri +"2'");
	if(m_supp_query == "2")
		Response.Write(" selected ");
	Response.Write(">Waiting For Return</option>");
	Response.Write("<option value='"+ supplier_ra_uri +"3'");
	if(m_supp_query == "3")
		Response.Write(" selected ");
	Response.Write(">Returned RMA</option>");
	
	Response.Write("</select>");
}


void RA_Menu()
{
	Response.Write("<form name=frm method=post action='"+ Request.ServerVariables["URL"] +"'>");
	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold; \">");
	
	Response.Write("<tr bgcolor=#DDDEEE><td align=right>");
	Response.Write("SEARCH RMA BY ID, SN#, INVOICE# &nbsp;<input type=text name=search value="+ Request.Form["search"] +">");
	Response.Write("<input type=submit name=cmd value='SEARCH' "+ Session["button_style"] +">");
	Response.Write("</td><td> &nbsp;&nbsp;&nbsp;&nbsp;");
	RAMenuOption();
	Response.Write("</td></tr>");

	Response.Write("\r\n<script");
	Response.Write(">\r\ndocument.frm.search.focus();\r\ndocument.frm.search.select();\r\n</script");
	Response.Write(">\r\n");

/*	Response.Write("<tr bgcolor=#DDDEEE><td align=center colspan=2>");
	
	datePicker(); //call date picker function
	//from date
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	string s_day2 = DateTime.Now.ToString("dd");
	string s_month2 = DateTime.Now.ToString("MM");
	string s_year2 = DateTime.Now.ToString("yyyy");
	if(Request.Form["cmd"] == "QUERY")
	{
		s_day = Request.Form["Datepicker1_day"];
		s_month = Request.Form["Datepicker1_month"];
		s_year = Request.Form["Datepicker1_year"];
		s_day2 = Request.Form["Datepicker2_day"];
		s_month2 = Request.Form["Datepicker2_month"];
		s_year2 = Request.Form["Datepicker2_year"];
	}
	if(m_sdFrom != "")
	{
		s_day = DateTime.Parse(m_sdFrom).ToString("dd");
		s_month = DateTime.Parse(m_sdFrom).ToString("MM");
		s_year = DateTime.Parse(m_sdFrom).ToString("yyyy");
		s_day2 = DateTime.Parse(m_sdTo).ToString("dd");
		s_month2 = DateTime.Parse(m_sdTo).ToString("MM");
		s_year2 = DateTime.Parse(m_sdTo).ToString("yyyy");
	}
	
	Response.Write("<b> SEARCH BY DATE FROM: ");
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		Response.Write("<option value="+ d +" ");
		if(int.Parse(s_day) == d)
			Response.Write(" selected ");
		
		Response.Write(">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		txtMonth = m_EachMonth[m-1];
		Response.Write("<option value="+m+" ");
		if(int.Parse(s_month) == m)
			Response.Write(" selected ");
		Response.Write(">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
	for(int y=2000; y<int.Parse(DateTime.Now.ToString("yyyy"))+1; y++)
	{
		Response.Write("<option value="+y+" ");
		if(int.Parse(s_year) == y)
			Response.Write(" selected ");
		Response.Write(" >"+y+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name='Datepicker1'>");
	Response.Write("<input type=hidden name='Datepicker1_conv'>");
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker1',1)");
	Response.Write("</script ");
	Response.Write(">");
	//------ start second display date -----------
	Response.Write(" &nbsp; TO: ");
	Response.Write("<select name='Datepicker2_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		Response.Write("<option value="+ d +" ");
		if(int.Parse(s_day2) == d)
			Response.Write(" selected ");
		
		Response.Write(">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		txtMonth = m_EachMonth[m-1];
		Response.Write("<option value="+m+" ");
		if(int.Parse(s_month2) == m)
			Response.Write(" selected ");
		Response.Write(">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int y=2000; y<=int.Parse(DateTime.Now.ToString("yyyy")); y++)
	{
		Response.Write("<option value="+y+" ");
		if(int.Parse(s_year2) == y)
			Response.Write(" selected ");
		Response.Write(" >"+y+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name='Datepicker2'>");
	Response.Write("<input type=hidden name='Datepicker2_conv'>");
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker2',1)");
	Response.Write("</script ");
	Response.Write(">");
	Response.Write("<input type=submit name=cmd value='QUERY' "+ Session["button_style"] +">");
	Response.Write("</td></tr>");
*/

}


</script>
<asp:Label id=LFooter runat=server/>
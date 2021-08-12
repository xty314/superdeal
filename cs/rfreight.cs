<!-- #include file="page_index.cs" -->
<script runat=server>

string m_type = "0";
string m_tableTitle = "Freight Report";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";

DataRow[] m_dra = null;

string m_sdFrom = "";
string m_sdTo = "";
int m_nPeriod = 0;

bool m_bPickTime = false;

DataSet ds = new DataSet();	
DataSet dsf = new DataSet();
string[] sct = new string[16];
string[] m_EachMonth = new string[13];
int cts = 0;
int m_ct = 1;

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

	if(Request.QueryString["pr"] != null)
	{
		m_nPeriod = MyIntParse(Request.QueryString["np"].ToString());
	}

	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["np"] != null && Request.QueryString["np"] != "")
			m_nPeriod = MyIntParse(Request.QueryString["np"].ToString());
		PrintMainPage();
		return;
	}
	
	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
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
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
//DEBUG("m_nperido =", m_nPeriod);
//DEBUG("mtype =", m_type);
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yesterday";
		break;
	case 2:
		m_datePeriod = "This Week";
		break;
	case 30:
		m_datePeriod = "This Month";
		break;
	case 4:
		m_datePeriod = "Last Month";
		break;
	case 5:
		m_datePeriod = "Last Three Month";
		break;
	case 6:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	default:
		break;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	DoReport();

	PrintAdminFooter();
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
	
	Response.Write("<form name=f action=rfreight.aspx method=post>");

	Response.Write("<br><center><h3>Freight Report</h3>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2><b>Date Range</b></td></tr>");

	Response.Write("<tr><td colspan=2><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=1>Yestoday</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=2>This Week</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=3>This Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=4>Last Month</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=5>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=2><input type=radio name=period value=6>Select Date Range</td></tr>");

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
	Response.Write("</td>");
		//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
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
	Response.Write("</td></tr>");
		
	Response.Write("<tr><td align=right><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	PrintAdminFooter();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//
bool DoReport()
{
//	if(!CheckShipID())
//		return false;

	m_tableTitle = "Freight Report";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) <= 7 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		break;
	case 5:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 6:
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59 "+"' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ship_name, ship_desc, COUNT(ticket) AS qty, SUM(f.price) AS total ";
	sc += " FROM invoice_freight f JOIN invoice i ON i.invoice_number = f.invoice_number ";
	sc += " WHERE 1 = 1 ";
	sc += m_dateSql;
	sc += " GROUP BY ship_name, ship_desc ";
	sc += " ORDER BY qty DESC ";
	sc += " SELECT f.ticket ";
	sc += " FROM invoice_freight f INNER JOIN invoice i ON i.invoice_number = f.invoice_number ";
//if(Session["email"].ToString().IndexOf("eznz.com") >= 0)
//DEBUG(" sc += ", sc);
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

	BindReport();

	return true;
}

/////////////////////////////////////////////////////////////////
void BindReport()
{
	int i = 0;

	//paging class
/*	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
*/
	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
/*	m_cPI.TotalRows = rows;
	m_cPI.URI = "?np="+ m_nPeriod +"";
	if(m_sdFrom != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo +"";
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
*/
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Freight Company</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th>Tickets</th>");
	Response.Write("<th>Freight</th>");
	Response.Write("</tr>\r\n");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	int nTicketTotal = 0;
	double dFreightTotal = 0;

	bool bAlterColor = false;
	for(; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string name = dr["ship_name"].ToString();
		string desc = dr["ship_desc"].ToString();
		string sQty = dr["qty"].ToString();
		int nQty = MyIntParse(sQty);
		string sTotal = dr["total"].ToString();
		double dTotal = MyDoubleParse(sTotal);
		dFreightTotal += dTotal;
		nTicketTotal += nQty;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td nowrap>" + desc + "</td>");
		Response.Write("<td align=right>" + sQty + "</td>");
		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		Response.Write("</tr>\r\n");
	}

	//total
	Response.Write("<tr style=\"color:black;background-color:lightblue;\">");
	Response.Write("<td colspan=2 align=right style=\"font-size:16\"><b>Sub Total : &nbsp; </b></td>");
	Response.Write("<td align=right style=\"font-size:16\">" + nTicketTotal.ToString() + "</td>");
	Response.Write("<td align=right nowrap style=\"font-size:16\">" + dFreightTotal.ToString("c") + "</td>");
	Response.Write("</tr>\r\n");
	
//	Response.Write("<tr><td colspan=3>" + sPageIndex + "</td></tr>\r\n");
	Response.Write("</table>");
}
/*
bool CheckShipID() //compatible with old version
{
	string sc = " SELECT * FROM ship ORDER BY prefix DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "ship");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	int rows = 0;
	sc = " SELECT f.id, f.ticket ";
	sc += " FROM invoice_freight f JOIN invoice i ON i.invoice_number = f.invoice_number ";
	sc += " WHERE f.ship_id IS NULL ";
	sc += m_dateSql;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "nullid");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["nullid"].Rows[i];
		string id = dr["id"].ToString();
		string ticket = dr["ticket"].ToString();
		
		string shipid = GetShipID(ticket);
		sc += " UPDATE invoice_freight SET ship_id = " + shipid + " WHERE id=" + id;
	}

	if(sc != "")
	{
//DEBUG("sc = ", sc);
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

string GetShipID(string ticket)
{
	DataRow dr = null;

	if(dsf.Tables["scan"] == null)
	{
		string sc = "SELECT * FROM ship ORDER BY prefix";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dsf, "scan");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}
	}

	bool bsuffixMatch = false;
	bool bPrefixMatch = false;

	string pm = "";
	string sm = "";
	string prefix = "";
	string suffix = "";
	for(int i=0; i<dsf.Tables["scan"].Rows.Count; i++)
	{
		dr = dsf.Tables["scan"].Rows[i];
		prefix = dr["prefix"].ToString();
		suffix = dr["suffix"].ToString();
		Trim(ref prefix);
		Trim(ref suffix);
		if(prefix == "")
		{
			if(suffix == "")
				continue;
			if(suffix.Length > ticket.Length)
				continue;
			//now check suffix
			if(ticket.Substring(ticket.Length - suffix.Length, suffix.Length).ToLower() == suffix.ToLower())
			{
				bsuffixMatch = true;
				sm = suffix;
				break;
			}
			continue;
		}
		if(prefix.Length > ticket.Length)
			continue;
		if(ticket.Substring(0, prefix.Length).ToLower() == prefix.ToLower())
		{
			bPrefixMatch = true;
			pm = prefix;
			break;
		}
	}
	if(!bsuffixMatch && !bPrefixMatch)
		return "";

	bool bDoubleMatch = false;
	//check double matches
	for(int i=0; i<dsf.Tables["scan"].Rows.Count; i++)
	{
		DataRow drd = dsf.Tables["scan"].Rows[i];
		prefix = drd["prefix"].ToString();
		suffix = drd["suffix"].ToString();
		Trim(ref prefix);
		Trim(ref suffix);
		if(prefix == "")
		{
			if(suffix == "")
				continue;
			if(suffix.Length > ticket.Length)
				continue;
			if(bsuffixMatch)
				continue;

			//now check suffix
			if(ticket.Substring(ticket.Length - suffix.Length, suffix.Length).ToLower() == suffix.ToLower())
			{
				bDoubleMatch = true;
				sm = suffix;
				break;
			}
		}
		if(prefix.Length > ticket.Length)
			continue;
		if(ticket.Substring(0, prefix.Length).ToLower() == prefix.ToLower())
		{
			if(bPrefixMatch)
				continue;
			bDoubleMatch = true;
			pm = prefix;
			break;
		}
	}
//	if(bDoubleMatch)
//	{
//		Response.Write("<br><br><center><h1><font color=red>Double Match Detected, ");
//		Response.Write("ticket scanned match both prefix '</font>" + pm + "<font color=red>' ");
//		Response.Write("and suffix '</font>" + sm + "<font color=red>', please check ticket settings");
//	}
	return dr["id"].ToString();
}
*/
</script>

<!-- #include file="page_index.cs" -->

<script runat=server>

string m_branchID = "";
string m_type = "";
string m_tableTitle = "Employee Time Sheet";
string m_datePeriod = "";
string m_dateSql = "";
string[] m_EachMonth = new string[16];

string m_sorted = "DESC";

string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
int m_nPeriod = 0;

string m_card_id = "";

bool m_bPrint = false;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
//DEBUG("m_brahc =-", m_branchID);
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
	
	int day = 1;
	int month = DateTime.Now.Month;
	int year = DateTime.Now.Year;

	if(Request.Form["cmd"] == "View Report")
	{
		m_card_id = Request.Form["staff"].ToString();
		if(!doShowAllStaff())
			return;
	}
	m_type = Request.QueryString["t"];
	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["t"] == null || Request.QueryString["t"] == "")
		{
			PrintMainPage();
			LFooter.Text = m_sAdminFooter;
			return;
		}
		m_type = Request.QueryString["t"];
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
	}

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.Form["t"] != null)
		m_type = Request.Form["t"];
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
	if(Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"].ToString();
		m_sdTo = Request.QueryString["to"].ToString();
	}
	if(Request.QueryString["pr"] != null)
		m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());

	Session["report_period"] = m_nPeriod;
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "This Week";
		break;
	case 1:
		m_datePeriod = "This Month";
		break;
	case 2:
		m_datePeriod = "Last Month";
		break;
	case 3:
		m_datePeriod = "This Year";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	case 5:
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_smFrom)-1] + "-"+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_smTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}
	
	if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		m_card_id = Request.QueryString["cid"];

	if(Request.QueryString["print"] != null)
		m_bPrint = true;

	PrintAdminHeader();
	if(!m_bPrint)
		PrintAdminMenu();

	if(m_type == "d")
		PrintOneStaffSummary();
	else
		DoTimeSummary("");

	if(!m_bPrint)
	{
		LFooter.Text = "<br><br><center><a href=rtime.aspx?t=d";
		LFooter.Text += " class=o>Show Details</a>";
	}
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<form name=f action='rtime.aspx' method=post>");

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<b>Branch : </b>");
		PrintBranchNameOptions(m_branchID);
	//	PrintBranchNameOptions(m_branchID, "rtime.aspx?branch=");
	}

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	Response.Write("<b>Select Staff</b></td></tr>");

	string uri = Request.ServerVariables["URL"].ToString();
	
	Response.Write("<tr><td colspan=6>");
	Response.Write("<select name=staff><option value=''>all</option>");
	if(!doShowAllStaff())
		return;
	
	int numrow = ds.Tables["staff"].Rows.Count;
	for(int ii=0; ii < numrow; ii++)
	{
		DataRow dr = ds.Tables["staff"].Rows[ii];
		
		string name = dr["name"].ToString();
		string id = dr["id"].ToString();

		Response.Write("\r\n <option value='"+id+"'>"+ name +"</option>");
	}
	Response.Write("</select>\r\n");
	
	
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Select Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>This Week</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>This Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=2>Last Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=3>This Year</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=4>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function
	Response.Write("<tr><td><b> &nbsp; From Date </b>");
	
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
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
//------ END first display date -----------

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
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right colspan=6><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	LFooter.Text = m_sAdminFooter;
}

bool DoTimeSummary(string status)
{
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(week, w.record_time, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, w.record_time, GETDATE()) = 0 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, w.record_time, GETDATE()) = 1 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(year, w.record_time, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND w.record_time BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = " ";
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT SUM(w.hours) AS total_hours, c.name AS staff_name, b.name AS branch_name, w.card_id ";
	sc += " FROM work_time w JOIN card c ON c.id = w.card_id JOIN branch b ON b.id = c.our_branch ";
	sc += " WHERE 1=1 " + m_dateSql;
	if(Session["branch_support"] != null && m_branchID != "0")
		sc += " AND c.our_branch = " + m_branchID;
	if(m_card_id != "" && m_card_id != null && m_card_id != "all")
		sc += " AND w.card_id = " + m_card_id;
	sc += " GROUP BY c.name, b.name, w.card_id ";
	sc += " ORDER BY b.name, c.name ";
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
	PrintStaffSummary(status);
	return true;
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

bool PrintOneStaffSummary()
{
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(week, w.record_time, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, w.record_time, GETDATE()) = 0 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, w.record_time, GETDATE()) = 1 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(year, w.record_time, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND w.record_time BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = " ";
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT w.record_time, w.hours, w.is_checkin, c.name AS staff_name, b.name AS branch_name, w.card_id ";
	sc += " FROM work_time w JOIN card c ON c.id = w.card_id JOIN branch b ON b.id = c.our_branch ";
	sc += " WHERE 1=1 " + m_dateSql;
	if(m_card_id != "")
		sc += " AND w.card_id = " + m_card_id;
	sc += " ORDER BY c.name, w.record_time ";
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

	string staff = "All";
	if(m_card_id != "")
		staff = TSGetUserNameByID(m_card_id);
	Response.Write("<center><br><table width=500>");
	Response.Write("<tr><td align=center><font size=+1><b>" + m_tableTitle + " - " + staff + "</b></font>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr width=100%></td></tr>");
	Response.Write("<tr><td align=center><font size=+1>");
	
	Response.Write("<tr><td><b>Date Period : " + m_datePeriod + "</b></td></tr>");

	int rows = ds.Tables["report"].Rows.Count;

	Response.Write("<tr><td width=100%>");
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#00db80;font-weight:bold;\">");
	if(m_card_id == "")
		Response.Write("<th>Name</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Checkin</th>");
	Response.Write("<th>Checkout</th>");
	Response.Write("<th align=right>Hours</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return true;
	}

	double dSum = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string name = dr["staff_name"].ToString();
		string date = DateTime.Parse(dr["record_time"].ToString()).ToString("dd-MM-yyyy");
		string time = DateTime.Parse(dr["record_time"].ToString()).ToString("HH:mm:ss");
		bool bCheckin = MyBooleanParse(dr["is_checkin"].ToString());
		string checkin = "";
		string checkout = time;
		if(bCheckin)
		{
			checkin = time;
			checkout = "";
		}
		double dHour = MyDoubleParse(dr["hours"].ToString());
		dSum += dHour;
		string sHour = dHour.ToString();
		if(dHour == 0)
			sHour = "";

		Response.Write("<tr");
		if(!bCheckin)
			Response.Write(" bgcolor=#FFFFDF");
		Response.Write(">");
		if(m_card_id == "")
			Response.Write("<td>" + name + "</td>");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td align=center>" + checkin + "</td>");
		Response.Write("<td align=center>" + checkout + "</td>");
		Response.Write("<td align=right>" + sHour + "</td>");
		Response.Write("</tr>");
	}

	if(m_card_id != "")
		Response.Write("<tr><td colspan=4 align=right><b>Total : " + dSum + "</b></td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr></table>");
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintStaffSummary(string status)
{
	Response.Write("<br><center><h4>");
	Response.Write(m_tableTitle);
	Response.Write("</h4>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	Response.Write("<table width=500 align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#0080db;font-weight:bold;\">");
	Response.Write("<th>Branch</th>");
	Response.Write("<th>Staff</th>");
	Response.Write("<th align=right>Total Hours</th>");
	Response.Write("</tr>");
	int rows = ds.Tables["report"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		string staff_name = dr["staff_name"].ToString();
		string staff_id = dr["card_id"].ToString();
		string branch_name = dr["branch_name"].ToString();
		string total_hours = dr["total_hours"].ToString();
		
		Response.Write("<td>" + branch_name + "</td>");
		Response.Write("<td>" + staff_name + "</td>");
		Response.Write("<td align=right><a href=rtime.aspx?t=d&cid=" + staff_id + " class=o title='Click to view details'>" + total_hours + "</a></td>");
		Response.Write("</tr>");
	}

	Response.Write("</table>");
}

bool doShowAllStaff()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id, sales ";
	sc += " FROM card WHERE 1=1 ";
	sc += " AND type = 4 ";
	sc += " ORDER BY name, trading_name";
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "staff"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>

<asp:Label id=LFooter runat=server/>
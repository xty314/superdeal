<!-- #include file="page_index.cs" -->

<script runat=server>

string m_branchID = "1";
string m_type = "";
string m_tableTitle = "Agent Commission Summary";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
DataRow[] m_dra = null;
string[] m_EachMonth = new string[16];

string m_sorted = "DESC";

string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
int m_nPeriod = 0;

string m_agent_id = "";
string m_agent = "";

bool m_bPickTime = false;
bool m_bSltBoth = false; //item details and customer are selected
bool m_bPrint = false;
bool m_bIsAgent = true;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Request.QueryString["sales"] == "1")
		m_bIsAgent = false;

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["login_branch_id"] != null)
			m_branchID = Session["login_branch_id"].ToString();
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
		m_agent_id = Request.Form["agent"].ToString();
		if(!doShowAllAgents())
			return;
		if(Request.Form["agent"] == "all")
			m_agent = "all";
		
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
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
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
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yesterday";
		break;
	case 6:
		m_datePeriod = "The Day Before Yesterday";
		break;
		
	case 2:
		m_datePeriod = "This Week";
		break;
	case 3:
		m_datePeriod = "This Month";
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
	
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		m_agent_id = Request.QueryString["sid"];

	if(Request.QueryString["print"] != null)
		m_bPrint = true;

	PrintAdminHeader();
	if(!m_bPrint)
		PrintAdminMenu();
	DoAgentSummary("");

	if(!m_bPrint)
	{
		LFooter.Text = "<br><br><center><a href=agent.aspx";
		if(!m_bIsAgent)
			LFooter.Text += "?sales=1 ";
		LFooter.Text += " class=o>New Report</a>";
	}
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<form name=f action='agent.aspx?r="+ DateTime.Now.ToOADate() +"");
	if(!m_bIsAgent)
		Response.Write("&sales=1&t=as");
	
	Response.Write("' method=post>");

	Response.Write("<br><center><h3>Select Report</h3>");
	if(Session["branch_support"] != null)
	{
		//int nal = MyIntParse(Session[m_sCompanyName + "AccessLevel"].ToString());
		//if(nal >= 7) //manager
		//if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
		{
			Response.Write("<b>Branch : </b>");
			PrintBranchNameOptions(m_branchID, "", true);
		}
		//else
		{
		//	Response.Write("<input type=hidden name=branch value=" + Session["branch_id"].ToString() + ">");
		}
	}

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	if(m_bIsAgent)
		Response.Write("<b>Select Travel Agent</b></td></tr>");
	else
		Response.Write("<b>Select Sales Person</b></td></tr>");

	string uri = Request.ServerVariables["URL"].ToString();
	
	Response.Write("<tr><td colspan=6>");
	Response.Write("<select name=agent><option value=''>all</option>");
	if(!doShowAllAgents(m_bIsAgent))
		return;
	
	int numrow = ds.Tables["agents"].Rows.Count;
	string[] agents = new string[numrow];
	for(int ii=0; ii < numrow; ii++)
	{
		DataRow dr = ds.Tables["agents"].Rows[ii];
		
		string name = dr["name"].ToString();
		string id = dr["id"].ToString();
		agents[ii] =	dr["id"].ToString();

		Response.Write("\r\n <option value='"+id+"'>"+ name +"</option>");
	}
	Response.Write("</select>\r\n");
	
	
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Select Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Yesterday</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=6>The Day Before Yesterday</td></tr>");
	/////allow only authorized user to access...
	if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
		Response.Write("<tr><td colspan=6><input type=radio name=period value=2>This Week</td></tr>");
		Response.Write("<tr><td colspan=6><input type=radio name=period value=3>This Month</td></tr>");
		Response.Write("<tr><td colspan=6><input type=radio name=period value=4>Select Date Range</td></tr>");
	
		int i = 1;
		datePicker(); //call date picker function
		Response.Write("<tr><td><b> &nbsp; From Date </b>");
		
		string s_day = DateTime.Now.ToString("dd");
		string s_month = DateTime.Now.ToString("MM");
		string s_year = DateTime.Now.ToString("yyyy");
		Response.Write("<select name='Datepicker1_day' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
		for(int d=1; d<32; d++)
		{
			Response.Write("<option value="+ d +">"+d+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='Datepicker1_month' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

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
		Response.Write("<select name='Datepicker1_year' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
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
		Response.Write("<select name='Datepicker2_day' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
		for(int d=1; d<32; d++)
		{
			if(int.Parse(s_day) == d)
				Response.Write("<option value="+ d +" selected>"+d+"</option>");
			else
				Response.Write("<option value="+ d +">"+d+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='Datepicker2_month' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

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
		Response.Write("<select name='Datepicker2_year' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
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
	}

	Response.Write("<tr><td align=right colspan=6><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	LFooter.Text = m_sAdminFooter;
}

bool DoAgentSummary(string status)
{
	if(m_bIsAgent)
		m_tableTitle = "Travel Agent Commission Summary";
	else
		m_tableTitle = "Invoice Summary";

	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";
		break;
	case 6:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 2 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(week, i.commit_date, GETDATE()) = 0 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = " ";

	if(m_bIsAgent)
	{
		sc = " SET DATEFORMAT dmy ";
		sc += " SELECT id, name ";
		sc += ", ISNULL(( ";
		{
			sc += " SELECT COUNT(*) ";
			sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " WHERE 1=1 " + m_dateSql;
			sc += " AND o.agent = card.id";
			if(Session["branch_support"] != null && m_branchID != "0")
			{
			//	sc += " AND i.branch = " + m_branchID;
			}
		}
		sc += " ), 0) AS orders ";
		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(i.total) ";
			sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " WHERE 1=1 " + m_dateSql;
			sc += " AND o.agent = card.id "; //AND o.sales = " + m_sales_id;
			if(Session["branch_support"] != null && m_branchID != "0")
			{
			//	sc += " AND i.branch = " + m_branchID;
			}
		}
		sc += " ), 0) AS amount ";
		sc += " FROM card ";
		sc += " WHERE type = 7 ";
		if(m_agent_id != "")
			sc += " AND id = " + m_agent_id;

		if(m_type == "as")
		{
			sc = " SET DATEFORMAT dmy ";
			sc += " SELECT i.invoice_number, i.commit_date, c.name AS agent, i.total AS total ";
	//		sc += " SELECT i.invoice_number, i.commit_date, c.name AS agent, i.price AS total ";
			sc += " FROM invoice i JOIN card c ON c.id = i.agent ";
			sc += " WHERE 1=1 " + m_dateSql;
			if(Session["branch_support"] != null && m_branchID != "0")
				sc += " AND i.branch = " + m_branchID;
//			sc += " AND i.agent = " + m_agent_id;
			sc += " AND c.id = " + m_agent_id;
		
			sc += " ORDER BY i.commit_date ";
		}
		else if(m_type == "ad")
		{
			sc = " SET DATEFORMAT dmy ";
	//		sc += " SELECT i.invoice_number, i.commit_date, c.name AS agent, i.price AS total ";
			sc += " SELECT i.invoice_number, i.commit_date, c1.name AS sales, c.name AS agent, i.total AS total ";
			sc += " , s.name, s.commit_price * 1.125 AS price, s.quantity, s.commit_price * s.quantity * 1.125 AS subtotal ";
			sc += " FROM invoice i JOIN sales s ON s.invoice_number = i.invoice_number ";
			sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " JOIN card c ON c.id = i.agent ";
			sc += " JOIN card c1 ON c1.id = o.sales ";
			sc += " WHERE 1=1 " + m_dateSql;
			if(Session["branch_support"] != null && m_branchID != "0")
				sc += " AND i.branch = " + m_branchID;
//			sc += " AND i.agent = " + m_agent_id;
			sc += " AND c.id = " + m_agent_id;
			sc += " ORDER BY i.commit_date ";
		}
	}
	else
	{
		sc = "";
		if(m_type == "as")
		{
			sc = " SET DATEFORMAT dmy ";
			sc += " SELECT i.invoice_number, i.commit_date, c.name AS sales, c1.name AS agent, i.total AS total ";
			sc += " FROM invoice i ";
			sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " JOIN card c ON c.id = o.sales ";
			sc += " LEFT OUTER JOIN card c1 ON c1.id= LTRIM(RTRIM(STR(i.card_id))) ";
			sc += " WHERE 1=1 " + m_dateSql;
			if(Session["branch_support"] != null && m_branchID != "0")
				sc += " AND i.branch = " + m_branchID;
			if(m_agent_id != "" && m_agent_id != null && m_agent_id != "all")
				sc += " AND o.sales = " + m_agent_id;
		
			sc += " ORDER BY i.commit_date ";

			if(Session["branch_support"] != null && m_branchID == "0") //do branch summary
			{
				sc = " SET DATEFORMAT dmy ";
				sc += " SELECT b.name AS branch, SUM(i.total) AS total ";
				sc += " FROM invoice i ";
				sc += " JOIN branch b ON b.id = i.branch ";
				sc += " WHERE 1=1 " + m_dateSql;
				sc += " GROUP BY b.name ";
				sc += " ORDER BY b.name ";
			}
		}
		else if(m_type == "ad")
		{
			sc = " SET DATEFORMAT dmy ";
			sc += " SELECT i.invoice_number, i.commit_date, c.name AS sales, c1.name AS Agent, i.total AS total ";
			sc += " , cr.brand + ' ' + s.name AS name, s.commit_price * 1.125 AS price, s.quantity, s.commit_price * s.quantity * 1.125 AS subtotal ";
			sc += " FROM invoice i JOIN sales s ON s.invoice_number = i.invoice_number ";
			sc += " JOIN orders O ON o.invoice_number = i.invoice_number ";
			sc += " JOIN card c ON c.id = o.sales ";
			sc += " LEFT OUTER JOIN card c1 ON c1.id = LTRIM(RTRIM(STR(i.card_id))) ";
			sc += " LEFT OUTER JOIN code_relations cr ON cr.code = s.code ";
			sc += " WHERE 1=1 " + m_dateSql;
			if(Session["branch_support"] != null && m_branchID != "0")
				sc += " AND i.branch = " + m_branchID;
			if(m_agent_id != "" && m_agent_id != null && m_agent_id != "all")
				sc += " AND o.sales = " + m_agent_id;
			sc += " ORDER BY i.commit_date ";
		}
		
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
	if(m_type == "as")
		PrintOneAgentSummary();
	else if(m_type == "ad")
		PrintOneAgentDetails();
	else
		PrintAgentSummary(status);
	return true;
}

void PrintBranchSummary()
{
	Response.Write("<center><table width=80%>");
	//Response.Write("<tr><td><font size=+1><b>" + m_sCompanyName.ToUpper() + "</b></font>");
	Response.Write("<tr><td>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	if(m_bIsAgent)
		Response.Write("<b><i>All Branches</i></b><br>");
	else
		Response.Write("<b><i>Branch Summary</i></b><br>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr width=100%></td></tr>");
	Response.Write("<tr><td align=center><font size=+1>");
	if(m_bIsAgent)
		Response.Write("Agent Summary</font></td></tr>");
	else
		Response.Write("Sales Report</font></td></tr>");
	Response.Write("<tr><td><b>Date Period : " + m_datePeriod + "</b></td></tr>");
//	Response.Write("<tr><td><hr width=100%></td></tr>");
	int rows = ds.Tables["report"].Rows.Count;

	Response.Write("<tr><td width=100%>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#8b008e;font-weight:bold;\">");
	if(m_bIsAgent)
		Response.Write("<th>Agent</th>");
	else
		Response.Write("<th>Branch</th>");
	Response.Write("<th align=right>INVOICE TOTAL</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	double dSum = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string name = "";
		if(m_bIsAgent)
			name = dr["agent"].ToString();
		else
			name = dr["branch"].ToString();
		double dTotal = MyDoubleParse(dr["total"].ToString());
		dSum += dTotal;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	}

	Response.Write("<tr><td align=right colspan=2><b>Total : " + dSum.ToString("c") + "</b></td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr></table>");
}

void PrintOneAgentSummary()
{
	if(Session["branch_support"] != null && m_branchID == "0") //do branch summary
	{
		PrintBranchSummary();
		return;
	}

	string sBranchName = GetBranchName(m_branchID);
	Response.Write("<center><table width=80%>");
	//Response.Write("<tr><td><font size=+1><b>" + m_sCompanyName.ToUpper() + "</b></font>");
	Response.Write("<tr><td>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b><i>" + sBranchName.ToUpper() + " Branch</i></b><br>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr width=100%></td></tr>");
	Response.Write("<tr><td align=center><font size=+1>");
	
	if(!m_bIsAgent)
		Response.Write("Sales Invoice");
	else
		Response.Write("Tourist");
	Response.Write(" Report</font></td></tr>");
	Response.Write("<tr><td><b>Date Period : " + m_datePeriod + "</b></td></tr>");
//	Response.Write("<tr><td><hr width=100%></td></tr>");
	int rows = ds.Tables["report"].Rows.Count;

	Response.Write("<tr><td width=100%>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#8b008e;font-weight:bold;\">");
	Response.Write("<th>INVOICE#</th>");
	Response.Write("<th>DATE</th>");
	if(!m_bIsAgent)
		Response.Write("<th>SALES</th>");
	Response.Write("<th>AGENT</th>");
	Response.Write("<th align=right>INVOICE TOTAL</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	double dAgentTotal = 0;
	double dSum = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string sales = "";
		if(!m_bIsAgent)
			sales = dr["sales"].ToString();
		string agent = dr["agent"].ToString();
		double dTotal = MyDoubleParse(dr["total"].ToString());
		if(agent != "")
			dAgentTotal += dTotal;
		dSum += dTotal;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a href=invoice.aspx?id=" + invoice_number + "&ig=1 target=new class=o>" + invoice_number + "</a></td>");
		Response.Write("<td>" + date + "</td>");
		if(!m_bIsAgent)
			Response.Write("<td>" + sales + "</td>");
		Response.Write("<td align=right>" + agent + "</td>");
		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	}

	Response.Write("<tr><td colspan=3>&nbsp;</td>");
	if(!m_bIsAgent)
		Response.Write("<td align=right><b>Agent Total : " + dAgentTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>Total : " + dSum.ToString("c") + "</b></td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr></table>");

	if(!m_bPrint)
	{
		Response.Write("<input type=button value='Show Invoice Details' class=b ");
		Response.Write(" onclick=window.location=('agent.aspx?t=ad&branch=" + m_branchID + "&sid=" + m_agent_id + "&pr="+ m_nPeriod +"");
		if(m_sdFrom != null)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		if(!m_bIsAgent)
			Response.Write("&sales=1");
		Response.Write("')>");
		if(Request.QueryString["t"] != "ad"){
		Response.Write("<input type=button value='Printable Version' class=b ");
		Response.Write(" onclick=window.open('agent.aspx?t=as&sid=" + m_agent_id + "&print=1&pr="+ m_nPeriod +"");
		if(m_sdFrom != null)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		if(!m_bIsAgent)
			Response.Write("&sales=1");
		if(m_branchID != "")
			Response.Write("&branch="+ m_branchID);
		Response.Write("')>");
	}
	}
}

void PrintOneAgentDetails()
{
	string sBranchName = GetBranchName(m_branchID);
	Response.Write("<center><table width=80%>");
	//Response.Write("<tr><td><font size=+1><b>" + m_sCompanyName.ToUpper() + "</b></font>");
	Response.Write("<tr><td>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b><i>" + sBranchName.ToUpper() + " Branch</i></b><br>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr width=100%></td></tr>");
	Response.Write("<tr><td align=center><font size=+1>");

	if(!m_bIsAgent)
		Response.Write("Sales Invoice");
	else
		Response.Write("Tourist");
	Response.Write(" Details Report</font></td></tr>");
	Response.Write("<tr><td><b>Date Period : " + m_datePeriod + "</b></td></tr>");
//	Response.Write("<tr><td><hr width=100%></td></tr>");

	int rows = ds.Tables["report"].Rows.Count;

	Response.Write("<tr><td width=100%>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#8B008E;font-weight:bold;\">");
	Response.Write("<th>INVOICE#</th>");
	Response.Write("<th>DATE</th>");
//	if(m_bIsAgent)
		Response.Write("<th>AGENT</th>");
//	else
		Response.Write("<th>SALES PERSON</th>");
	Response.Write("<th width=50% colspan=3 align=right>INVOICE TOTAL</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=6>");
	sb.Append("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><th width=50% align=left>Product Name</th><th width=20% align=left>Price</th>");
	sb.Append("<th width=10% align=left>Quantity</th><th width=20% align=left>Subtotal</th></tr>");

	string inv_old = "";
	double dSum = 0;

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string agent = dr["agent"].ToString();
		string sales = dr["sales"].ToString();
		string name = dr["name"].ToString();
		double dPrice = Math.Round(MyDoubleParse(dr["price"].ToString()), 2);
		string qty = dr["quantity"].ToString();
		double dSub = Math.Round(MyDoubleParse(dr["subtotal"].ToString()), 2);
		double dTotal = MyDoubleParse(dr["total"].ToString());

		if(invoice_number != inv_old)
		{
			dSum += dTotal;
			if(i > 0)
				Response.Write("</td></tr></table>");
			Response.Write("<tr>");
			Response.Write("<td><b>" + invoice_number + "</b></td>");
			Response.Write("<td><b>" + date + "</b></td>");
			Response.Write("<td><b>" + agent + "</b></td>");
			Response.Write("<td><b>" + sales + "</b></td>");
			Response.Write("<td colspan=3 align=right><b>" + dTotal.ToString("c") + "</b></td>");
			Response.Write("</tr>");
			Response.Write(sb.ToString());
		}
//		else
		{
			Response.Write("<tr>");
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td>" + dPrice.ToString("c") + "</td>");
			Response.Write("<td>" + qty + "</td>");
			Response.Write("<td>" + dSub.ToString("c") + "</td>");
			Response.Write("</tr>");
		}
	
		inv_old = invoice_number;
	}

//	Response.Write("<tr><td colspan=6 align=right><b>Total : " + dSum.ToString("c") + "</b></td></tr>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=7 align=right><b>Total : " + dSum.ToString("c") + "</b></td></tr>");
	Response.Write("</table><center>");

	if(!m_bPrint)
	{
		Response.Write("<input type=button value='Show Invoice Summary' class=b ");
		Response.Write(" onclick=window.location=('agent.aspx?t=as&sid=" + m_agent_id + "&pr="+ m_nPeriod +"");
		if(m_sdFrom != null)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		if(!m_bIsAgent)
			Response.Write("&sales=1&branch="+ m_branchID +"");
		Response.Write("')>");
		if(Request.QueryString["t"] != "ad"){
		Response.Write("<input type=button value='Printable Version1' class=b ");
		Response.Write(" onclick=window.open('agent.aspx?t=ad&sid=" + m_agent_id + "&print=1&pr="+ m_nPeriod +"");
		if(m_sdFrom != null)
			Response.Write("&frm="+ HttpUtility.UrlEncode(m_sdFrom) +"&to="+ HttpUtility.UrlEncode(m_sdTo) +"");
		Response.Write("&sales=1&branch="+ m_branchID +"");	
		Response.Write("')>");	
	}
}
}

/////////////////////////////////////////////////////////////////
void PrintAgentSummary(string status)
{
	m_dra = ds.Tables["report"].Select("amount > 0", "orders DESC");


	Response.Write("<br><center><h4>");
	if(!m_bIsAgent)
		Response.Write("Sales Invoice");
	else
		Response.Write("Tourist");
	Response.Write(" Report</h4>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	int i = 0;
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = m_dra.Length;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(m_type != null)
		m_cPI.URI += "&type="+ m_type;	
	m_cPI.URI += "&pr="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=95%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>AGENT</th>");
	Response.Write("<th>TOTAL ORDERS</th>");
	Response.Write("<th align=right>TOTAL AMOUNT</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	double dTotalQTY = 0;
	for(; i<rows && i<end; i++)
	{
		DataRow dr = m_dra[i];
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string orders = dr["orders"].ToString();
		string amount = dr["amount"].ToString();
		dTotalNoGST += double.Parse(amount);
		dTotalQTY += double.Parse(orders);
		
		Response.Write("<td>&nbsp;&nbsp;<a title='view sales invoice' href='agent.aspx?t=as&sid="+ id +"&pr="+ m_nPeriod +"&branch="+ m_branchID+"");
		if(m_sdFrom != "" && m_sdFrom != null)
			Response.Write("&frm="+ m_sdFrom +"&to="+ m_sdTo +"");
		Response.Write("' class=o>" + name + "</a></td>");
		Response.Write("<td align=center><a title='view sales invoice' href='agent.aspx?t=as&sid="+ id +"&pr="+ m_nPeriod +"&branch="+ m_branchID+"");
		if(m_sdFrom != "" && m_sdFrom != null)
			Response.Write("&frm="+ m_sdFrom +"&to="+ m_sdTo +"");
		if (status == "Manager")
			Response.Write("&salesmanager=1");
		Response.Write("' class=o>" + orders + "</a></td>");
		
		Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");
		Response.Write("</tr>");
	}

	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<br><center>");
	Response.Write("<b> Total QTY: </b><font color=red size=2>"+ dTotalQTY.ToString() +"</font>");
	Response.Write("&nbsp;&nbsp;<b>Total AMOUNT:</b> <font color=Green size=2>"+ dTotalNoGST.ToString("c") +"</font>");
	Response.Write(" <b>SUB Total:</b> <font color=Green size=2>"+ (dTotalNoGST).ToString("c") +"</font>");
	Response.Write("</center>");
}
bool doShowAllAgents()
{
	return doShowAllAgents(false);
}
bool doShowAllAgents(bool bIsAgent)
{
	string sc = " SELECT name, trading_name, company, contact, phone, id, sales ";
	sc += " FROM card WHERE 1=1 ";
	if(bIsAgent)
		sc += " AND type = "+ GetEnumID("card_type", "agent") +"";
	else
		sc += " AND type = 4 ";
	sc += " ORDER BY name, trading_name";
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "agents"); 
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
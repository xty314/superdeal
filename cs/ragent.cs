<!-- #include file="page_index.cs" -->

<script runat=server>

string m_branchID = "";
string m_type = "as";
string m_tableTitle = "Agent Commission Summary";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
string m_sPrinterPort = "";
string m_agent_id = "";
string m_agent = "";
string m_rdate = System.DateTime.Now.ToString();
bool m_bPickTime = false;
bool m_bSltBoth = false; //item details and customer are selected
bool m_bPrint = false;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_agent_id = Request.QueryString["id"];

	if(Request.Form["cmd"] != null || Request.Form["agent"] != null)
	{
		m_agent_id = Request.Form["agent"].ToString();
		DoAgentSummary();
		return;
	}

	PrintMainPage();
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h4>Travel Agent Summary</h4>");
	Response.Write("<form name=f action=ragent.aspx method=post>");

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#80db80;font-weight:bold;\"><td colspan=6>");
	Response.Write("<b>Select Travel Agent</b></td></tr>");

	string uri = Request.ServerVariables["URL"].ToString();
	
	Response.Write("<tr><td colspan=6>");
	Response.Write("<select name=agent><option value=''>all</option>");
	if(!doShowAllAgents())
		return;
	
	int numrow = ds.Tables["agents"].Rows.Count;
	string[] agents = new string[numrow];
	for(int ii=0; ii < numrow; ii++)
	{
		DataRow dr = ds.Tables["agents"].Rows[ii];
		
		string name = dr["name"].ToString();
		string phone = dr["phone"].ToString();
		string id = dr["id"].ToString();
		agents[ii] =	dr["id"].ToString();

		Response.Write("\r\n <option value='"+id+"'>"+ name + " " + phone +"</option>");
	}
	Response.Write("</select>\r\n");
	
	Response.Write(" OR Enter Agent Barcode : <input type=text name=barcode>");	
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right colspan=6><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("<script>document.f.barcode.focus();</script");
	Response.Write(">");
}

bool DoAgentSummary()
{
	string sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");
	string agent_barcode = Request.Form["barcode"];
	int rows = 0;
	ds.Clear();
	string sc = " ";

	m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";

	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT id, name, barcode, phone ";
/*	sc += ", ISNULL(( ";
	{
		sc += " SELECT COUNT(*) ";
		sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		sc += " WHERE i.branch = " + Session["login_branch_id"].ToString() + m_dateSql;
		sc += " AND i.agent = card.barcode ";
	}
	sc += " ), 0) AS orders ";
*/
	sc += ", ISNULL(( ";
	{
		sc += " SELECT SUM(i.total) ";
		sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		sc += " WHERE i.branch = " + Session["login_branch_id"] + m_dateSql;
		sc += " AND i.agent = card.id "; //AND o.sales = " + m_sales_id;
	}
	sc += " ), 0) AS amount ";

	
	sc += ", ISNULL(( ";
	{
		sc += " SELECT SUM(s.commit_price * s.quantity * 1.125) AS total ";
		sc += " FROM invoice i JOIN sales s ON s.invoice_number = i.invoice_number ";
		sc += " JOIN code_relations c ON c.code = s.code ";
		sc += " WHERE i.branch = " + Session["login_branch_id"] + m_dateSql;
		sc += " AND c.class = 1 ";
		sc += " AND i.agent = card.id "; //AND o.sales = " + m_sales_id;
	}
	sc += " ), 0) AS amount_a ";
	
	sc += ", ISNULL(( ";
	{
		sc += " SELECT SUM(s.commit_price * s.quantity * 1.125) AS total ";
		sc += " FROM invoice i JOIN sales s ON s.invoice_number = i.invoice_number ";
		sc += " JOIN code_relations c ON c.code = s.code ";
		sc += " WHERE i.branch = " + Session["login_branch_id"] + m_dateSql;
		sc += " AND c.class = 2 ";
		sc += " AND i.agent = card.id "; //AND o.sales = " + m_sales_id;
	}
	sc += " ), 0) AS amount_b ";
	
	sc += " FROM card ";
	sc += " WHERE type = 2 ";
	if(m_agent_id != "")
		sc += " AND id = " + m_agent_id;
	else if(agent_barcode != "")
		sc += " AND barcode = '" + agent_barcode + "' ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "report");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
//	PrintOneAgentSummary();

	if(rows <= 0)
		return true;

	DataRow dr = ds.Tables["report"].Rows[0];
	string name = dr["name"].ToString();
	string phone = dr["phone"].ToString();
	string barcode = dr["barcode"].ToString();
	string amount = MyDoubleParse(dr["amount"].ToString()).ToString("c");
	string amount_a = MyDoubleParse(dr["amount_a"].ToString()).ToString("c");
	string amount_b = MyDoubleParse(dr["amount_b"].ToString()).ToString("c");

	PrintAdminHeader();
   
	Response.Write("<br><center><h4>" + name + "</h4>");
	Response.Write("Report Date: <b>" + m_rdate   + "</b><br>");
	Response.Write ("========================================");
	Response.Write("<table>");
	Response.Write("<tr><td>Code : </td><td>" + barcode + "</td></tr>");
//	Response.Write("<tr><td>Name : </td><td>" + name + "</td></tr>");
	Response.Write("<tr><td>Phone : </td><td>" + phone + "</td></tr>");
	Response.Write("<tr><td>Total Amount Today : </td><td>" + amount + "</td></tr>");
	Response.Write("<tr><td>A Class : </td><td>" + amount_a + "</td></tr>");
	Response.Write("<tr><td>B Class : </td><td>" + amount_b + "</td></tr>");
    
	Response.Write("</table>");

	Response.Write("<br><br><br><br><br><input type=button value='Close Window' onclick='window.close();' class=b>");


	string tp = ReadSitePage("agent_summary");
	tp = tp.Replace("@@name", name);
	tp = tp.Replace("@@phone", phone);
	tp = tp.Replace("@@barcode", barcode);
	tp = tp.Replace("@@amount", amount);
	tp = tp.Replace("@@re_date",  m_rdate);
	tp = tp.Replace("\r\n", "\\r\\n");

	string s = "\r\n<object classid=\"clsid:B816E029-CCCB-11D2-B6ED-444553540000\" ";
	s += " CODEBASE=\"asprint.ocx\" id=\"AsPrint1\">\r\n";
	s += "<param name=\"_Version\" value=\"65536\">\r\n";
	s += "<param name=\"_ExtentX\" value=\"2646\">\r\n";
	s += "<param name=\"_ExtentY\" value=\"1323\">\r\n";
	s += "<param name=\"_StockProps\" value=\"0\">\r\n";
	s += "<param name=\"HideWinErrorMsg\" value=\"1\">\r\n";
	s += "</object>\r\n";

	byte[] cut = {0x1d, 0x56, 0x01, 0x00};//new char[4];
//	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
//	byte[] init_printer = {0x1b, 0x40};

	ASCIIEncoding encoding = new ASCIIEncoding( );
    string scut = encoding.GetString(cut);	

	s += "<script language=javascript>\r\n";
	s += " document.AsPrint1.Open('" + sReceiptPort + "')\r\n";
	s += " document.AsPrint1.PrintString('" + tp + "');\r\n";
	s += " document.AsPrint1.PrintString('" + scut + "');\r\n";
	s += " document.AsPrint1.Close();\r\n";
	s += "</script";
	s += ">";

	Response.Write(s);
	return true;
}

bool doShowAllAgents()
{
	string sc = " SELECT name, trading_name, company, contact, phone, id, sales ";
	sc += " FROM card WHERE 1=1 ";
	sc += " AND type = 2 ";
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

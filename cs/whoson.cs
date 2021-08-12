<!-- #include file="resolver.cs" -->
<!-- #include file="page_index.cs" -->
<script runat=server>

string tableName = "Web Session";
string dbName = "";
string m_sql = "";
string m_visitors = "0";
string m_pageHits = "0";
string m_traffic = "0";

int m_currentOnline = 0;

SqlConnection connbak = new SqlConnection("Initial Catalog=" + m_sCompanyName + "_log_bak;" + m_sDataSource + m_sSecurityString);
DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

bool m_bQuit = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	//refresh itself in 5 second
	//Response.Write("<META HTTP-EQUIV='Refresh' CONTENT=5>");

	if(Request.QueryString["cls"] == "1")
	{
		ClearSessions();
		Response.Redirect("whoson.aspx");
		return;
	}

	GetQueryString();
//	m_sql = "SELECT * FROM " + tableName;
	InitializeData(); //init functions

	if(!ShrinkDB())
		return;
	if(m_bQuit)
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	if(!DoSearch())
		return;

	WriteHeaders();

	if(!IsPostBack)
	{
		BindGrid();
		BindGridA();
	}
	LFooter.Text = m_sAdminFooter;
}

//backup logs old then 2 days, delete useless records, graphics, scripts etc 
bool ShrinkDB()
{
	if(Application["backingup_web_log"] != null)
	{
		if(Request.QueryString["clearflag"] == null)
		{
			Response.Write("Backing up in progress by : " + Application["backingup_web_log_by"] + " at " + Application["backingup_web_log_at"]);
			return true;
		}
	}

	Application["backingup_web_log"] = true;
	Application["backingup_web_log_by"] = Session["name"].ToString();
	Application["backingup_web_log_at"] = DateTime.Now.ToString("dd-MM HH:mm");

	string oldLogs = "0";
	string sc = "SELECT count(*) AS logs FROM web_uri_log WHERE DATEDIFF(day, logtime, GETDATE()) > 1";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "clean");
		oldLogs = ds.Tables["clean"].Rows[0]["logs"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Application["backingup_web_log"] = null;
		return false;
	}
//DEBUG("oldlogs=", oldLogs);
	if(oldLogs == "0")
	{
		Application["backingup_web_log"] = null;
		return true;
	}

	//calc analyze data first
	DateTime dFirst = DateTime.Now;
	DateTime dYesterday = DateTime.Now.AddDays(-1);
	sc = " SELECT MIN(logtime) AS first_day FROM web_session ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "stat");
		string first_day = ds.Tables["stat"].Rows[0]["first_day"].ToString();
		if(first_day == "")
		{
			Application["backingup_web_log"] = null;
			return true;
		}
//		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
//DEBUG("f=", first_day);
//return false;
//		dFirst = DateTime.Parse(first_day, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		dFirst = DateTime.Parse(first_day);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Application["backingup_web_log"] = null;
		return false;
	}

	while(dFirst <= dYesterday)
	{
		if(!SaveStatic(dFirst.ToString("dd-MM-yyyy")))
		{
			Application["backingup_web_log"] = null;
			return false;
		}
		dFirst = dFirst.AddDays(1);
	}
	m_bShowProgress = true;
	PrintAdminHeader();
	PrintAdminMenu();

	int rows = 0;
	ds.Tables["clean"].Clear();
	sc = "SELECT * FROM web_uri_log WHERE DATEDIFF(day, logtime, GETDATE()) > 1";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "clean");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Application["backingup_web_log"] = null;
		return false;
	}

	Response.Write("<html><body>");
	Response.Write("backing up old uri logs, please wait...<br>");
	Response.Flush();
/*
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["clean"].Rows[i];
		sc = " SET DATEFORMAT dmy ";
		sc += " INSERT INTO web_uri_log (id, logtime, target, parameters) ";
		sc += " VALUES(' ";
		sc += dr["id"].ToString() + "', '";
		sc += DateTime.Parse(dr["logtime"].ToString()).ToString("dd-MM-yyyy HH:mm") + "', '";
		sc += EncodeQuote(dr["target"].ToString()) + "', '";
		sc += EncodeQuote(dr["parameters"].ToString()) + "')";
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = connbak;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			Application["backingup_web_log"] = null;
			return false;
		}
		MonitorProcess(100);
	}
*/	
	ds.Tables["clean"].Clear();
	sc = "SELECT * FROM web_session WHERE DATEDIFF(day, logtime, GETDATE()) > 1";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "clean");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Application["backingup_web_log"] = null;
		return false;
	}

//	Response.Write("<html><body>");
	Response.Write("<br>backing up old session logs, please wait...<br>");
	Response.Flush();
/*
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["clean"].Rows[i];
		sc = "SET DATEFORMAT dmy ";
		sc += " INSERT INTO web_session (id, sid, ip, rip, host, card_id, logtime, agent, referer, activate) ";
		sc += " VALUES(";
		sc += dr["id"].ToString() + ", '";
		sc += dr["sid"].ToString() + "', '";
		sc += dr["ip"].ToString() + "', '";
		sc += dr["rip"].ToString() + "', '";
		sc += EncodeQuote(dr["host"].ToString()) + "', '";
		sc += dr["card_id"].ToString() + "', '";
		sc += DateTime.Parse(dr["logtime"].ToString()).ToString("dd-MM-yyyy HH:mm") + "', '";
		sc += EncodeQuote(dr["agent"].ToString()) + "', '";
		sc += EncodeQuote(dr["referer"].ToString()) + "', 0)";
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = connbak;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			Application["backingup_web_log"] = null;
			return false;
		}
		MonitorProcess(100);
	}
*/
	//delete old
	Response.Write("<br>Final Clean up, please wait...<br>");
	sc = " DELETE FROM web_uri_log WHERE DATEDIFF(day, LogTime, GETDATE()) > 1 ";
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
		Application["backingup_web_log"] = null;
		return false;
	}
	
	sc = " DELETE FROM web_session WHERE DATEDIFF(day, LogTime, GETDATE()) > 1 ";
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
		Application["backingup_web_log"] = null;
		return false;
	}

	Response.Write("done!<br>");
	Response.Flush();
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=whoson.aspx\">");
	Response.Write("</body></html>");
	m_bQuit = true;
	Application["backingup_web_log"] = null;
	return true;
}

void GetQueryString()
{
	if(Request.QueryString["t"] != null)
		tableName = Request.QueryString["t"];
	if(Request.QueryString["d"] != null)
	{
		dbName = Request.QueryString["d"];
		dbName = dbName.ToLower();
	}
}

Boolean DoSearch()
{
	if(ds.Tables["ip"] != null)
		ds.Tables["ip"].Clear();
	if(ds.Tables["session"] != null)
		ds.Tables["session"].Clear();

	string sc = "";
	int rows = 0;
	sc = "SELECT s.id, c.name from web_session s ";
	sc += " LEFT OUTER JOIN card c ON c.id=s.card_id WHERE s.activate=1 ORDER BY c.name DESC, agent";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "ip");
		m_currentOnline = rows;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<rows; i++)
	{
		int nr = 0;
		string id = ds.Tables["ip"].Rows[i]["id"].ToString();
		string name = ds.Tables["ip"].Rows[i]["name"].ToString();
		name = name.Replace("'", "`");
		sc = "select top 1 s.id, s.card_id, s.sid, s.IP, s.rip, '" + name + "' AS Name ";
		sc += ", SUBSTRING(s.agent, PATINDEX('%IE%', s.agent), 50) AS Agent, '" + m_sCompanyName + "' AS Site, ";
		sc += " u.target+u.parameters AS LastAction, CONVERT(varchar(50), DATEDIFF(minute, u.logtime, GETDATE())) + ' minutes' AS IdleTime";
		sc += " FROM web_uri_log u JOIN web_session s on u.id=s.id ";
		sc += " WHERE s.id=" + id + " AND DATEDIFF(minute, u.logtime, GETDATE()) < 60 ";
		sc += " ORDER BY u.logtime DESC";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			nr = myCommand.Fill(ds, "session");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		if(nr <= 0)
		{
			sc = "select id, IP, rip, '" + name + "' as Name, SUBSTRING(agent, PATINDEX('%IE%', agent), 50) AS Agent, ";
			sc += " '" + m_sCompanyName + "' AS Site, 'unknown' AS LastAction, 'unknown' AS IdleTime ";
			sc += " FROM web_session WHERE id=" + id;
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				nr = myCommand.Fill(ds, "session");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
		}
	}

	sc = "SELECT TOP 20 s.rip AS IP, c.name, u.target+u.parameters AS URL, u.logtime as TIME ";
	sc += " FROM web_uri_log u JOIN web_session s ON s.id=u.id ";
	sc += " LEFT OUTER JOIN card c ON c.id=s.card_id ";
	sc += " WHERE c.name <> 'Cash Sales' ";
	sc += " ORDER BY u.logtime DESC";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "log");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(!GetStatic())
		return false;
	return true;
}

bool SaveStatic(string sDate)
{
	string sc = "";
	int rows = 0;

	if(ds.Tables["history"] != null)
		ds.Tables["history"].Clear();
	if(ds.Tables["visitors1"] != null)
		ds.Tables["visitors1"].Clear();
	if(ds.Tables["pagehits1"] != null)
		ds.Tables["pagehits1"].Clear();

	string visitors = "0";
	string pagehits = "0";

	//get analyze data from log_analyze table
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT * FROM web_log_analyze WHERE DATEDIFF(day, dates, '" + sDate + "') = 0 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "history");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows > 0)
		return true; 

	//visitors1
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT COUNT(DISTINCT rip) AS Visitors FROM web_session ";
	sc += " WHERE DATEDIFF(day, LogTime, '" + sDate + "') = 0 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "visitors1") > 0)
			visitors = ds.Tables["visitors1"].Rows[0]["visitors"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//pagehits1
	sc = " SET DATEFORMAT dmy ";
	sc += " SELECT COUNT(target) AS PageHits FROM web_uri_log ";
	sc += " WHERE DATEDIFF(day, LogTime, '" + sDate + "') = 0 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "pagehits1") > 0)
			pagehits = ds.Tables["pagehits1"].Rows[0]["PageHits"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//save analyze data
	sc = " SET DATEFORMAT dmy ";
	sc += " INSERT INTO web_log_analyze (dates, visitors, page_hits, traffic) ";
	sc += " VALUES('" + sDate + "', '";
	sc += visitors + "', '" + pagehits + "', '0')";
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
	return true;
}

bool GetStatic()
{
	string sc = "";
	//today visitors
	sc = "SELECT COUNT(DISTINCT rip) AS Visitors FROM web_session ";
	sc += " WHERE DATEDIFF(day, LogTime, GETDATE())=0 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "visitors") > 0)
			m_visitors = ds.Tables["visitors"].Rows[0]["visitors"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	//pagehits
	sc = "SELECT COUNT(target) AS PageHits FROM web_uri_log ";
	sc += " WHERE DATEDIFF(day, LogTime, GETDATE())=0 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "pagehits") > 0)
			m_pageHits = ds.Tables["pagehits"].Rows[0]["PageHits"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//traffic
	m_traffic = "0";
	return true;
}

void WriteHeaders()
{
	Response.Write("<br><center><h3>" + dbName + " " + tableName + " </h3></center>");
	Response.Write("<table width=100%>");
	Response.Write("<tr><td align=right>");
	Response.Write("<b>Current Online : </b><font size=+1 color=red>" + m_currentOnline + "</font>");
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<b>Today's Visitors : </b><font size=+1 color=red>" + m_visitors + "</font>");
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<b>PageHits : </b><font size=+1 color=red>" + m_pageHits + "</font>");
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
//	Response.Write("<b>Traffic : </b><font size=+1 color=red>" + m_traffic + "</font>" + " MB");
	Response.Write("</td></tr>");
	Response.Write("</table>");
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["session"] != null)
		rows = ds.Tables["session"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>IP</th>");
	Response.Write("<th>NAME/AGENT</th>");
	Response.Write("<th>LAST ACTION</th>");
	Response.Write("<th>IDLE TIME</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["session"].Rows[i];
		string id = dr["id"].ToString();
		string card_id = dr["card_id"].ToString();
		string trading_name = TSGetUserCompanyByID(card_id);
		string ip = dr["ip"].ToString();
		string name = dr["name"].ToString();
		string agent = dr["agent"].ToString();
		string rip = dr["rip"].ToString();

		if(name == "Cash Sales")
		{
			name = "EZNZ ADMIN";
			continue;
		}

		CResolver rs = new CResolver();
		string host = rs.Resolve(ip);
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td title='IP:" + ip);
		if(rip != ip)
			Response.Write(" REALIP:" + rip);
		Response.Write("'>" + host);
		if(rip != ip)
			Response.Write(" <font color=red>*</font>");
		Response.Write("</td>");
		Response.Write("<td");
		if(name != "")
			Response.Write(" title='" + agent + "'");
		Response.Write("><a href=msg.aspx?sid=" + dr["sid"].ToString() + ">");
		if(name == "")
			Response.Write(agent);
		else
			Response.Write("<b>" + name + " <font color=red>" + trading_name + "</font></b>");
		Response.Write("</a></td>");
		Response.Write("<td><a href=vd.aspx?t=ilog&id=" + id + " title='Show Visit Map'>" + dr["lastaction"].ToString() + "</a></td>");
		Response.Write("<td>" + dr["idletime"].ToString() + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<a href=?cls=1 class=o>Clear List</a>");
}

void BindGridA()
{
	DataView source = new DataView(ds.Tables["log"]);
	MyDataGridA.DataSource = source ;
	MyDataGridA.DataBind();
}

void MyDataGrid_PageA(object sender, DataGridPageChangedEventArgs e) 
{
	DoSearch();
	MyDataGridA.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
	BindGridA();
}

bool ClearSessions()
{
	try
	{
		myCommand = new SqlCommand("UPDATE web_session SET activate=0 WHERE activate=1");
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		AlertAdmin(e.ToString());
		return false;
	}
	return true;
}

</script>

<form runat=server>
<asp:DataGrid id=MyDataGridA
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=20
	PagerStyle-PageButtonCount=20
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_PageA
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>
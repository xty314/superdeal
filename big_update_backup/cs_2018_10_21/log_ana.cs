<script runat=server>

string m_sTableName = "web_log_analyze";
string m_sDbName = m_sCompanyName;
string m_idays = "0";
string m_title = "Statistics";

DataTable dtla = new DataTable();

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	bool bRet = false;

//	GetQueryString();
//	InitializeData(); //init functions

	string t = Request.QueryString["t"];
	if(t == "ii")
		m_title = "Interest Item List";
	else if(t == "rl")
		m_title = "Referer List";

	if(Request.QueryString["day"] != null)
		m_idays = Request.QueryString["day"];

	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();

	if(t == "ii")
		ShowInterestItem();
	else if(t == "rl")
		PrintRefererList();
	else
	{
		if(!DoSearch())
			return;
		BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
}

void GetQueryString()
{
	if(Request.QueryString["t"] != null)
		m_sTableName = Request.QueryString["t"];
	if(Request.QueryString["d"] != null)
	{
		m_sDbName = Request.QueryString["d"];
		m_sDbName = m_sDbName.ToLower();
	}
}

Boolean DoSearch()
{
	string sc = "SELECT * FROM " + m_sTableName + " ORDER BY dates DESC";
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "hits");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void WriteHeaders()
{
	Response.Write("<br><center><h3>" + m_title + "</h3></center>");
	Response.Write("<div align=right>");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx>Statistics</a>&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx?t=ii>Interest Item List</a>&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx?t=rl>Referer List</a>&nbsp;&nbsp;");
	Response.Write("</div>");
}

bool ShowInterestItem()
{
//	return true;

	int rows = 0;
	string sc = "SELECT logtime, parameters AS code FROM web_uri_log ";
	sc += " WHERE (target LIKE '%p.aspx') AND (DATEDIFF(day, LogTime, GETDATE()) <= " + m_idays + ") ";
	sc += " ORDER BY parameters";
//DEBUG("sc=", sc);
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "interest");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

//	Response.Write("<b>Interest Products List</b> &nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx?t=ii&day=0>Today</a>&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx?t=ii&day=1>+ Yesterday</a>&nbsp;&nbsp;");

	if(rows <= 0)
		return true;
	
	dtla.Columns.Add(new DataColumn("name", typeof(String)));
	dtla.Columns.Add(new DataColumn("code", typeof(String)));
	dtla.Columns.Add(new DataColumn("hits", typeof(Int32)));
	dtla.Columns.Add(new DataColumn("time", typeof(String)));
	dtla.Columns.Add(new DataColumn("price", typeof(String)));
	dtla.Columns.Add(new DataColumn("pic", typeof(String)));

	DataRow dr = null;
	string code = "";
	string code_old = "";
	string time = "";
	int hits = 0;
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["interest"].Rows[i];
		code = dr["code"].ToString();
		Trim(ref code);
		if(!IsInteger(code))
			continue;
		if(code != code_old)
		{
			if(code_old != "")
			{
				if(!AddToTable(code_old, hits.ToString(), time))
					return false;
			}
			code_old = code;
			hits = 1;
			time = DateTime.Parse(dr["logtime"].ToString()).ToString("dd/MM/yyyy HH:mm");;
		}
		else
			hits++;
	}
	if(code_old != "")
	{
		if(!AddToTable(code_old, hits.ToString(), time))
			return false;
	}

	DataRow[] dra = dtla.Select("", "hits DESC, time");

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>CODE </td>\r\n");
	Response.Write("<td>DESC</td>\r\n");
	Response.Write("<td>&nbsp;HITS&nbsp;</td>\r\n");
	Response.Write("<td nowrap>LAST VIEWED&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>\r\n");
	Response.Write("<td>&nbsp;PHOTO</td>\r\n");
	Response.Write("</tr>\r\n");

	for(int i=0; i<dtla.Rows.Count; i++)
	{
		dr = dra[i];
		Response.Write("<tr>");
		Response.Write("<td>" + dr["code"].ToString() + "</td>");
		Response.Write("<td>" + dr["name"].ToString() + "</td>");
		Response.Write("<td>" + dr["hits"].ToString() + "</td>");
		Response.Write("<td>" + dr["time"].ToString() + "</td>");
		Response.Write("<td align=center>" + dr["pic"].ToString() + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

bool AddToTable(string code, string hits, string sTime)
{
	DataRow dr = dtla.NewRow();

	DataRow drp = null;
	if(!GetProductWithSpecialPrice(code, ref drp))
		return false;

	if(drp == null)
		return true;

	string name = drp["name"].ToString();
	Trim(ref name);
	if(name.Length >= 90)
		name = name.Substring(0, 90);
	
	dr["name"] = "<a href=p.aspx?" + code + ">" + name + "</a>";
	dr["price"] = double.Parse(drp["price"].ToString()).ToString("c");
	dr["code"] = code;
	dr["hits"] = hits;
	dr["time"] = sTime;
	dr["pic"] = "<img src=../pi/" + code + ".jpg height=24>";
	dtla.Rows.Add(dr);
	return true;
}

bool PrintRefererList()
{
/*	string sc = "DELETE FROM ref_log WHERE ref LIKE 'http://www.edenonline.co.nz%' ";
	sc += " OR ref LIKE 'http://www.edencomputers.co.nz%' ";
	sc += " OR ref LIKE 'http://www.edencomputer.co.nz%' ";
	sc += " OR ref LIKE 'http://www.eznz.co.nz%' ";
	sc += " OR ref LIKE 'http://www.eznz.com%' ";
	sc += " OR ref LIKE 'http://nz.eznz.com%' ";
	try
	{
		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
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
*/
	int rows = 0;
	string sc = "SELECT * FROM web_ref_log WHERE DATEDIFF(day, logtime, GETDATE()) <= " + m_idays + " ORDER BY ref";
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "ref");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
//	Response.Write("<br><b>Referer List</b> &nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx?t=rl&day=0>Today</a>&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif> <a href=log_ana.aspx?t=rl&day=1>+ Yesterday</a>&nbsp;&nbsp;");

	if(rows <= 0)
		return true;

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>REFERER </td>\r\n");
	Response.Write("<td>URL</td>\r\n");
	Response.Write("<td>IP</td>\r\n");
	Response.Write("<td nowrap>TIME</td>\r\n");
	Response.Write("</tr>\r\n");

	for(int i=0; i<ds.Tables["ref"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["ref"].Rows[i];
		string referer = dr["ref"].ToString();
		string refs = "";
		int nSlash = 0;
		if(referer.Length > 24)
		{
			for(int j=0; j<referer.Length; j++)
			{
				if(nSlash >= 2)
				{
					if(referer[j] == '/')
						break;
					refs += referer[j];
				}
				if(referer[j] == '/')
					nSlash++;
			}
		}
		string url = "http://" + dr["server"].ToString() + dr["target"].ToString() + "?" + dr["parameters"].ToString();
		Response.Write("<tr>");
		Response.Write("<td><a href=" + referer + ">" + refs + "</a></td>");
		Response.Write("<td><a href=" + url + ">" + dr["target"].ToString() + "</a></td>");
		Response.Write("<td nowrap>" + dr["clienthost"].ToString() + "</td>");
		Response.Write("<td nowrap>" + dr["logtime"].ToString() + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	DataView source = new DataView(ds.Tables["hits"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

</script>

<br>
<form runat=server>

<b>Page Hits</b>
<asp:DataGrid id=MyDataGrid 
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
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
</asp:DataGrid>

</form>

<asp:Label id=LFooter runat=server/>
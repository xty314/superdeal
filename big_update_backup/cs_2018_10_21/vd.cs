<script runat=server>

string m_type = "VIEW FIGHT";
string tableName = "aiprice";
string dbName = m_sCompanyName;
string m_sql = "SELECT * FROM aiprice ORDER BY log_time DESC";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	bool bRet = false;

	GetQueryString();

	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();

	if(!DoSearch())
		return;

	if(!IsPostBack)
	{
		BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
}

void GetQueryString()
{
	if(Request.QueryString["t"] != null)
	{
		m_type = Request.QueryString["t"];
		if(m_type == "fight")
		{
			m_sql = "SELECT a.code, SUBSTRING(p.name, 0, 30) as ITEM, a.cost, a.price, a.margin, a.bottom_price, a.bargain_price, c.name, a.ip, a.log_time ";
			m_sql += " FROM aiprice a JOIN product p ON p.code=a.code LEFT OUTER JOIN card c ON c.id=a.card_id ORDER BY a.log_time DESC";
		}
		else if(m_type == "adjlog")
		{
			m_sql = "SELECT l.CODE, l.qty AS 'QTY ADJUSTMENT', c.name AS STAFF, l.NOTE, l.LOG_TIME FROM stock_adj_log l JOIN card c ON c.id=l.staff ";
			m_sql += " WHERE code=" + Request.QueryString["code"];
			m_sql += " ORDER BY log_time DESC ";
			m_type = "STOCK TAKING LOG";
		}
		else if(m_type == "ilog")
		{
			string sc = "SELECT s.rip AS IP, c.name, u.target+u.parameters AS URL, u.logtime as TIME ";
			sc += " FROM web_uri_log u JOIN web_session s ON s.id=u.id JOIN card c ON c.id=s.card_id ";
			sc += " WHERE u.id=" + Request.QueryString["id"];
			sc += " ORDER BY u.logtime DESC ";
//DEBUG("sc=", sc);
			m_sql = sc;
			m_type = "VISIT LOG";
		}
	}
/*	if(Request.QueryString["d"] != null)
	{
		dbName = Request.QueryString["d"];
		dbName = dbName.ToLower();
	}
*/
}

Boolean DoSearch()
{
	string sc = m_sql;
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog="+ dbName + ";" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds);
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
	Response.Write("<br><center><h3>" + m_type + " </h3></center>");
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	DataView source = new DataView(ds.Tables[0]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

</script>

<form runat=server>
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
	PageSize=100
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
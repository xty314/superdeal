<script runat=server>
/*
W3SVC1 www.eznz.co.nz
W3SVC2 www.edenonline.co.nz
W3SVC3 nz.eznz.com
W3SVC4 www.eznz.com
W3SVC6 www.edencomputers.co.nz
*/

string tableName = "web_session";
string dbName = "eznz";
string m_sql = "";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
	if(Session["email"].ToString() != "darcy@eznz.com")
	{
		Response.Write("<h3>ACCESS DENIED</h3>");
		return;
	}

	bool bRet = false;

	GetQueryString();
	m_sql = "SELECT * FROM " + tableName;
	InitializeData(); //init functions

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
		tableName = Request.QueryString["t"];
	if(Request.QueryString["d"] != null)
	{
		dbName = Request.QueryString["d"];
		dbName = dbName.ToLower();
	}
}

Boolean DoSearch()
{
	string sc = m_sql;
	try
	{
		SqlConnection myConnection = new SqlConnection("Initial Catalog="+ dbName + ";" + m_sDataSource + m_sSecurityString);
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
	StringBuilder sb = new StringBuilder();
//	sb.Append("<html><style type=\"text/css\">td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
//	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>");
//	sb.Append("<body bgcolor=#666696>");
//	sb.Append("<table bgcolor=white width=100% height=100% align=center><tr><td valign=top>");
	sb.Append("<br><center><h3>" + dbName + " " + tableName + " </h3></center>");
	Response.Write(sb.ToString());
	Response.Flush();
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
<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h3>Quotation Manager - Recommended System</h3></center>");

	if(!DoSearch())
		return;

	if(!IsPostBack)
	{
		BindGrid();
	}

	LAddNewButton.Text = @"
<table width=100% align=center>
<tr><td>
<form action=sqm_syse.aspx target=_new method=post>
<input type=submit name=cmd value='Add New Configuration' ";

	LAddNewButton.Text += Session["button_style"];

	LAddNewButton.Text += @">
</form>
</td></tr>
</table>
";
	LFooter.Text = m_sAdminFooter;
}

Boolean DoSearch()
{
	string sc = "SELECT id, name FROM q_sys";	 
	try
	{
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
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=Edit
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="sqm_syse.aspx?t=get&rid={0}"
			 Text=Edit
			 Target=_blank/>
	</Columns>

</asp:DataGrid>
</form>

<asp:Label id=LAddNewButton runat=server/>
<asp:Label id=LFooter runat=server/>
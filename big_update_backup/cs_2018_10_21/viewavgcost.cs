<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_ItemCode = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;
//	InitializeData(); //init functions

	if(!getAVGCostLog())
		return;
	string jobno = "";
	if(Request.QueryString["code"] != null)
		jobno = Request.QueryString["code"].ToString();
	Response.Write("<br>");
	Response.Write("<h5><center>AVERAGE COST of #:"+ jobno +"</center></h5>");
	BindGrid();
	//LFooter.Text = m_sAdminFooter;
}

bool getAVGCostLog()
{
	int rows = 0;
	if(Request.QueryString["code"] != "" && Request.QueryString["code"] != null)
		m_ItemCode = Request.QueryString["code"].ToString();
	string sc = "SELECT r.code AS CODE#, r.comments AS 'COMMENTS',  r.input_date AS 'UPDATE DATE',  cr.name AS 'ITEM NAME', c.name AS STAFF";
	sc += " ,r.last_avg_cost AS 'LAST AVG COST', r.new_avg_cost AS 'NEW AVG COST' , r.purchase_id AS 'PURCHASE#' ";
	sc += " FROM avg_cost_log r JOIN card c ON c.id = r.input_by ";
	sc += " JOIN code_relations cr ON cr.code = r.code ";
	sc += " WHERE 1 = 1 ";
	if(m_ItemCode != "")
		sc += " AND r.code = '"+ m_ItemCode +"' ";
/*	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		if(TSIsDigit(Request.QueryString["code"]))
			sc += " AND r.code = "+ Request.QueryString["code"];
			*/
	sc += " ORDER BY r.input_date DESC ";
	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "avg_cost_log");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<center><h5><font color=Red>NO AVERAGE COST CHANGED</h5></font></center>");
		return false;
	}

	return true;
}
void BindGrid()
{
	DataView source = new DataView(dst.Tables[0]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}



</script>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"><html>
<head>
    <title>--- Average Cost Log ---</title> 
</head>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#E3E3E3
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=7pt 
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

	<HeaderStyle BackColor=#E3E3E3 ForeColor=black Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#E3E3E3/>

</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>
<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_status = "2"; //ready to ship
string m_paymentMethod = "";

string m_slink = "esales.aspx";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();

	if(Request.QueryString["s"] != null)
		m_status = Request.QueryString["s"];
	if(Request.QueryString["pm"] != null)
		m_paymentMethod = Request.QueryString["pm"];

	if(!DoSearch())
		return;

	if(!IsPostBack)
	{
		BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
}

Boolean DoSearch()
{
	if(m_paymentMethod != "" && m_paymentMethod != "refunded")
		m_slink = "salespay.aspx";

	string uri = m_slink + "?i=";

	string receipt_type = GetEnumID("receipt_type", "order");
	string receipt_type_draft = GetEnumID("receipt_type", "draft");
	string status_deleted = GetEnumID("general_status", "deleted");
	string sc = "SELECT DISTINCT i.invoice_number as Invoice#, '" + uri + "' + LTRIM(STR(i.invoice_number)) AS uri, ";
	sc += " c.name, c.company, ";
	if(m_paymentMethod == "refunded")
		sc += " 'Refunded' ";
	else if(m_paymentMethod == "abandoned")
		sc += " 'Abandoned' ";
	else if(m_status == "deleted")
		sc += " 'Deleted' ";
	else
		sc += " e.name ";
	sc += " AS status, i.commit_date as date ";
	sc += "FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number JOIN enum e ON (e.id=s.status AND e.class='sales_item_status') ";
	sc += " JOIN card c ON c.id=i.card_id ";
//	string sc = "SELECT DISTINCT i.invoice_number as Invoice#, i.name, i.company, s.status, i.commit_date as date ";
//	sc += "FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
	if(m_paymentMethod != "")
	{
		if(m_paymentMethod == "refunded")
			sc += " WHERE i.refunded=1 ";
		else if(m_paymentMethod == "abandoned")
			sc += " WHERE i.type=" + receipt_type_draft;
		else
			sc += " WHERE i.payment_type=" + m_paymentMethod + " AND i.type=" + receipt_type;
		sc += " AND i.paid=0 AND i.status!=" + status_deleted;
	}
	else if(m_status != "all")
	{
		if(m_status == "deleted")
			sc += " WHERE i.status=" + status_deleted;
		else
			sc += " WHERE s.status=" + m_status + " AND i.status!=" + status_deleted;
	}		
	sc += " ORDER BY i.commit_date DESC";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds);
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
	
	//sb.Append("<br><center><font size=4><b>Sales Manager</b></font></center><br>");
	sb.Append("<br><img border=0 src='/i/smt.gif'><br>");
	sb.Append("<div align=right><img src=r.gif><a href=?s=" + GetEnumID("sales_item_status", "payment confirmed") + "&r="+r+" class=d>Payment Confirmed</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?pm=" + GetEnumID("payment_method", "deposit") + "&r="+r+" class=d>Deposit</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?pm=" + GetEnumID("payment_method", "cheque") + "&r="+r+" class=d>Cheque</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?s=" + GetEnumID("sales_item_status", "on backorder") + "&r="+r+" class=d>BackOrders</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?s=" + GetEnumID("sales_item_status", "shipped") + "&r="+r+" class=d>Shipped</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?pm=abandoned&r="+r+" class=d>Abandonded</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?pm=refunded&r="+r+" class=d>Refunded</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?s=deleted&"+r+" class=d>Deleted</a>&nbsp;&nbsp;&nbsp;");
	sb.Append("<img src=r.gif><a href=?s=all&"+r+" class=d>All</a>&nbsp;&nbsp;&nbsp;</div>");
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
	AutoGenerateColumns=false
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
	PagerStyle-HorizontalAlign=Right
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:HyperLinkColumn HeaderText=INVOICE# 
			 DataNavigateUrlField=Invoice#
			 DataNavigateUrlFormatString="invoice.aspx?{0}"
			 DataTextField=Invoice#
			 Target=_blank/>
		<asp:BoundColumn HeaderText='NAME' DataField=name/>
		<asp:BoundColumn HeaderText='COMPANY' DataField=company/>
		<asp:BoundColumn HeaderText='STATUS' DataField=status/>
		<asp:BoundColumn HeaderText='DATE / TIME' DataField=date DataFormatString="{0:dd/MM/yy HH:mm}"/>

		<asp:HyperLinkColumn
			 HeaderText=
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 Text=' Process '
			 Target=_blank/>
	</Columns>

</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>
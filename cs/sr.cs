<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_profit = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	int day = 1;
	int month = DateTime.Now.Month;
	int year = DateTime.Now.Year;

	if(!IsPostBack)
	{
		Calendar1.VisibleDate = DateTime.Now;

		string startDate = month.ToString() + "/01/" + year.ToString();
		Session["sr_start_date"] = startDate;
		Session["sr_day"] = day;
		Session["sr_month"] = month;
		Session["sr_year"] = year;

		if(!DoSearch())
			return;
		BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
}

Boolean DoSearch()
{
	DateTime dStart = DateTime.Now.AddMonths(-1);
	if(Session["sr_start_date"] != null)
		dStart = DateTime.Parse(Session["sr_start_date"].ToString());

	string startDate = dStart.ToString("dd-MM-yyyy");
	string endDate = dStart.AddMonths(1).ToString("dd-MM-yyyy");

	string sales_type_online = GetEnumID("sales_type", "online");
	ds.Clear();
	
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT i.invoice_number, i.total , c.trading_name, i.commit_date, i.total ";
	sc += " FROM invoice i LEFT OUTER JOIN card c ON c.id=i.card_id ";
	sc += " WHERE i.commit_date>='" + startDate + "' AND i.commit_date<='" + endDate + "' ";
//	sc += " AND paid=1 AND refunded=0 AND sales_type=" + sales_type_online;
	sc += " ORDER BY i.invoice_number";
//DEBUG("sc=", sc);

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "SET DATEFORMAT dmy ";
	sc += " SELECT sum((s.commit_price - s.supplier_price) * s.quantity - i.freight) ";
	sc += " FROM invoice i JOIN sales s ON s.invoice_number=i.invoice_number ";
	sc += " WHERE i.commit_date>='" + startDate + "' AND i.commit_date<='" + endDate + "' ";
//	sc += " AND i.paid=1 AND i.refunded=0 AND i.sales_type=" + sales_type_online;
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "profit") > 0)
		{
			try
			{
				m_profit = double.Parse(ds.Tables["profit"].Rows[0][0].ToString()).ToString("c");
			}
			catch(Exception e)
			{
			}
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void Selection_Change(Object sender, EventArgs e) 
{
	int day = Calendar1.SelectedDate.Day;
	int month = Calendar1.SelectedDate.Month;
	int year = Calendar1.SelectedDate.Year;

	Session["sr_day"] = day;
	Session["sr_month"] = month;
	Session["sr_year"] = year;

	string startDate = Calendar1.SelectedDate.ToShortDateString();
	Session["sr_start_date"] = startDate;
//DEBUG("date=", startDate);
	DoSearch();
	Calendar1.SelectedDates.Clear();
//	Calendar1.VisibleDate = new DateTime(year, month, day);

//	Calendar1.VisibleDate = DateTime.Now;
//	Calendar1.VisibleDate = new DateTime(Calendar1.TodaysDate.Year, DropList1.SelectedIndex + 1);
	BindGrid();
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	double dTotal = 0;
	if(ds.Tables["invoice"] != null)
	{
		DataRow dr = null;
		double dPrice = 0;
		// double dTotal = 0;
		for(int i=0; i<ds.Tables["invoice"].Rows.Count; i++)
		{
			dr = ds.Tables[0].Rows[i];
			dPrice = double.Parse(dr["total"].ToString());
			dTotal += dPrice;
		}
//		DataView source = new DataView(ds.Tables["invoice"]);
//		MyDataGrid.DataSource = source ;
//		MyDataGrid.DataBind();
	}

	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	Response.Write("<br><img border=0 src='/i/sr.gif'>");

	int day = (int)Session["sr_day"];
	int month = (int)Session["sr_month"];
	int year = (int)Session["sr_year"];

	DateTime start = new DateTime(year, month, day);
	DateTime end = start.AddMonths(1);
	
	Response.Write(start.ToString("MMM.dd.yyyy"));
	Response.Write(" - ");
	Response.Write(end.ToString("MMM.dd.yyyy"));

	Response.Write("</b></center>");
	
	LTotal.Text = "<b>Sales SubTotal : " + dTotal.ToString("c") + "</b>";
	LProfit.Text = "<b>Net Profit : " + m_profit + "</b>";
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	DoSearch();
	BindGrid();
}

//change calendar setting 21aug
/*  Calendar id=Calendar1 runat=server OnSelectionChanged=Selection_Change>
	<OtherMonthDayStyle ForeColor=gray>
	</OtherMonthDayStyle>
	
	<TitleStyle BackColor=blue ForeColor=White Font-Bold=true>
	</TitleStyle>

	<DayStyle BackColor=gray>
	</DayStyle>

	<SelectedDayStyle BackColor=Red Font-Bold=True>
	</SelectedDayStyle>

*/
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
	AllowPaging=false
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
		<asp:BoundColumn HeaderText='Invoice Date' DataField=commit_date DataFormatString="{0:dd/MM/yy}"/>
		<asp:HyperLinkColumn 
			 HeaderText=INVOICE#
			 DataNavigateUrlField=invoice_number
			 DataNavigateUrlFormatString="invoice.aspx?{0}"
			 DataTextField=invoice_number
			 Target=_blank/>
		<asp:BoundColumn HeaderText='Customer' DataField=trading_name/>
		<asp:BoundColumn HeaderText='Total Inc-GST' DataField=total DataFormatString="{0:c}">
			<ItemStyle HorizontalAlign=right />
		</asp:BoundColumn>
	</Columns>

</asp:DataGrid>

<table width=100%>
<tr><td>


<asp:Calendar id=Calendar1 
        OnSelectionChanged=Selection_Change
        DayNameFormat="FirstLetter"
        ShowGridLines="true"
        BackColor="beige"
        ForeColor="darkblue"
        SelectedDayStyle-BackColor="red"
        SelectedDayStyle-ForeColor="white"
        SelectedDayStyle-Font-Bold="true"
        TitleStyle-BackColor="darkblue"
        TitleStyle-ForeColor="white"
        TitleStyle-Font-Bold="true"
        NextPrevStyle-BackColor="darkblue"
        NextPrevStyle-ForeColor="white"
        DayHeaderStyle-BackColor="Blue"
        DayHeaderStyle-ForeColor="white"
        DayHeaderStyle-Font-Bold="true"
        OtherMonthDayStyle-BackColor="white"
        OtherMonthDayStyle-ForeColor="lightblue"
        Width="256px"
        RunAt="Server">
		
</asp:Calendar>

</td>
<td valign=top align=right>

<table valign=top>
	<tr><td align=right>
	<asp:Label id=LTotal runat=server/>
	</td></tr>
	<tr><td align=right>
	<asp:Label id=LProfit runat=server/>
	</td></tr>
</table>

</td></tr></table>

</form>

<asp:Label id=LFooter runat=server/>
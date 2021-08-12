<script runat=server>

bool DoInvoiceList()
{

	if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		m_tableTitle = "CUSTOMER INVOICE DETIALS";
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		m_tableTitle = "SALES INVOICE DETIALS";
	if(Request.QueryString["pr"] != null && Request.QueryString["pr"] != "")
		m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());
	
	if(Request.QueryString["frm"] != null && Request.QueryString["frm"] != "")
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Current Month";
		break;
	case 1:
		m_datePeriod = "Last Month";
		break;
	case 2:
		m_datePeriod = "Last Three Month";
		break;
	case 3:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_smFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_smTo + "</font>";
		break;
	default:
		break;
	}

//m_tableTitle = "Sales Person Summary for (<font color=green size=4>"+ m_sales_person.ToUpper() +"</font>)";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

	bool bSalesManager = false;
	if(Request.QueryString["salesmanager"] == "1")
		bSalesManager = true;
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ISNULL(i.total,0) AS total, c.id, c.name, c.trading_name, i.invoice_number, i.price, i.tax, i.freight, i.commit_date ";
	if(bSalesManager)
		sc += ", c1.name AS sales_manager, c1.trading_name AS sales_manager_trading_name ";
	sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id = ";
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		sc += " o.sales ";
	if(bSalesManager)
		sc += " JOIN card c1 ON c1.id = o.sales_manager ";
	else if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		sc += " i.card_id ";
	sc += " WHERE 1=1 ";
	if(Session["branch_support"] != null && m_branchID != "0")
	{
		sc += " AND i.branch = " + m_branchID;
	}
	sc += m_dateSql;
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
	{
		sc += " AND o.sales";
		if(bSalesManager)
			sc += "_manager";
		if(Request.QueryString["sid"] == "-1")
			sc += " IS NULL ";
		else
			sc += " = " + Request.QueryString["sid"];
	}
	else if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		sc += " AND i.card_id = "+ Request.QueryString["cid"];
		
		
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
	
	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			Response.Write("<h5>Branch: "+GetBranchName(m_branchID) +"</h5>");
		else
			Response.Write("<h5>Branch: ALL</h5><br>");
	}
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	PrintInvoiceSummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintInvoiceSummary()
{
	bool bSalesManager = false;
	if(Request.QueryString["salesmanager"] == "1")
		bSalesManager = true;
	int i = 0;

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

//	int rows = m_dra.Length;
	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?1=1";
	if(Request.QueryString["sid"] != null)
		m_cPI.URI += "&sid="+ Request.QueryString["sid"];
	else if(Request.QueryString["cid"] != null)
		m_cPI.URI += "&cid="+ Request.QueryString["cid"];
	if(Request.QueryString["pr"] != null)
		m_cPI.URI += "&pr="+ m_nPeriod;
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
		m_cPI.URI += "&frm="+ Request.QueryString["frm"] +"&to="+ Request.QueryString["to"];
	if(Request.QueryString["salesmanager"] == "1")
		m_cPI.URI += "&salesmanager=1";

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(Request.QueryString["sid"] != "" && Request.QueryString["sid"] != null)
		Response.Write("<th align=right>SALES NAME</th>");
	if(Request.QueryString["cid"] != "" && Request.QueryString["cid"] != null)
		Response.Write("<th align=right>CUSTOMER</th>");
	if(bSalesManager)
		Response.Write("<th align=right>SALES MANAGER</th>");
	Response.Write("<th>INVOICE#</th>");
	Response.Write("<th>INVOICE DATE#</th>");
	Response.Write("<th align=right>FREIGHT</th>");
	Response.Write("<th align=right>GST</th>");
	Response.Write("<th align=right>AMOUNT</th>");
	Response.Write("<th align=right>TOTAL AMOUNT</th>");
	Response.Write("</tr>");
	if(rows <= 0)
	{
		Response.Write("</table>");
		m_bShowPic = false;
		return;
	}

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	double dTotalQTY = 0;
	double dTotalFreight = 0;
	//for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
			
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		//DataRow dr = m_dra[i];
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		string id = dr["id"].ToString();
		string freight = dr["freight"].ToString();
		string name = dr["name"].ToString();
		if(name == "")
			name = dr["trading_name"].ToString();
		string smname = "";
		if(bSalesManager)
		{
			smname = dr["sales_manager"].ToString();
			if(smname == "")
				smname = dr["sales_manager_trading_name"].ToString();
		}
		string tax = dr["tax"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string amount = dr["price"].ToString();
		string date = dr["commit_date"].ToString();
		if(date != "")
			date = DateTime.Parse(date).ToString("dd-MM-yyyy");
		dTotalFreight += double.Parse(freight);
		dTotalNoGST += double.Parse(amount);
		dTotalTax += double.Parse(tax);
		dTotalWithGST += double.Parse(dr["total"].ToString());
		Response.Write("<td align=right>" + name + "</td>");
		if(bSalesManager)
			Response.Write("<td align=right>" + smname + "</td>");
		Response.Write("<td align=center><a title='view invoice details' href='invoice.aspx?"+ invoice_number +"' class=o target=new>" + invoice_number + "</a></td>");
		Response.Write("<td align=center>" + date + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(freight).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(tax).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");
		Response.Write("<td align=right>" + (MyDoubleParse(tax) + MyDoubleParse(freight) + MyDoubleParse(amount)).ToString("c") + "</td>");

		if(double.Parse(amount) > m_nMaxY)
			m_nMaxY = double.Parse(amount);

		if(double.Parse(amount) < 0 && double.Parse(amount) < m_nMinY)
			m_nMinY = double.Parse(amount);

	//xml chart data
		x = (i*10).ToString();
		m_nMaxX = i*10;
		y = tax;
		name = StripHTMLtags(EncodeQuote(name));
		//name = XMLDecoding(name);
		legend = name.Replace("&", " ");
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");

		x = (MyIntParse(x) + 5).ToString();
		y = amount;
		
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x");
		if(m_bHasLegends)
			sb2.Append(" legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");
		
		Response.Write("</tr>");
		
	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_IslandTitle[0] = "--GST";
	m_IslandTitle[1] = "--Amount";
	m_nIsland = 2;
	

	Response.Write("<tr><td colspan=8>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3 ><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=right >" + dTotalFreight.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dTotalTax.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dTotalNoGST.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dTotalWithGST.ToString("c") + "</td>");
//	Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	
	dTotalTax = 0;
	dTotalNoGST = 0;
	dTotalWithGST =0;
	dTotalFreight = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string freight = dr["freight"].ToString();
		string tax = dr["tax"].ToString();
		string amount = dr["price"].ToString();
		dTotalFreight += double.Parse(freight);
		dTotalNoGST += double.Parse(amount);
		dTotalTax += double.Parse(tax);
		dTotalWithGST += double.Parse(dr["total"].ToString());
	
	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right>" + dTotalFreight.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dTotalTax.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dTotalNoGST.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dTotalWithGST.ToString("c") + "</td>");
	Response.Write("</tr>");
	
	Response.Write("<tr><td colspan=7>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
		//write xml data file for chart image
	WriteXMLFile();

}

</script>
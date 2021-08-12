<script runat=server>

bool DoPurchaseInvoiceList()
{

	m_tableTitle = "PURCHASE DETIALS";
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
		m_dateSql = " AND DATEDIFF(month, p.date_invoiced, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, p.date_invoiced, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, p.date_invoiced, GETDATE()) >= 1 AND DATEDIFF(month, p.date_invoiced, GETDATE()) <= 3 ";
		break;
	case 3:
//		m_dateSql = " AND p.date_invoiced BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		m_dateSql = " AND p.date_invoiced BETWEEN '" + m_sdFrom + "' AND '" + m_sdTo + "' ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(p.date_invoiced) >= '" + m_smFrom + "' AND MONTH(p.date_invoiced) <= '" + m_smTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT c2.id, c2.name, c2.trading_name, p.id AS purchase_id, p.po_number ";
	sc += " , p.inv_number, p.total AS amount, p.tax, p.freight, p.date_invoiced ";
	sc += " FROM purchase p ";
	sc += " JOIN card c2 ON c2.id = p.supplier_id ";
	sc += " WHERE 1=1 ";
	if(Session["branch_support"] != null)
	{
		sc += " AND p.branch_id = " + m_branchID;
	}
//	sc += " AND total > 0 ";
	sc += m_dateSql;

	if(Request.QueryString["spid"] != null && Request.QueryString["spid"] != "")
		sc += " AND c2.id = "+ Request.QueryString["spid"];

		
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
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	PrintPurchaseInvoiceSummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintPurchaseInvoiceSummary()
{
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
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(Request.QueryString["spid"] != null)
		m_cPI.URI += "&spid="+ Request.QueryString["spid"];
	if(Request.QueryString["pr"] != null)
		m_cPI.URI += "&pr="+ m_nPeriod;
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
		m_cPI.URI += "&frm="+ Request.QueryString["frm"] +"&to="+ Request.QueryString["to"];

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=95%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	
	Response.Write("<th>SUPPLIER</th>");
	
	Response.Write("<th>PONUMBER#</th>");
	Response.Write("<th>PURCHASE DATE#</th>");
	Response.Write("<th align=left>SUPP_INV#</th>");
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
		string tax = dr["tax"].ToString();
		string invoice_number = dr["po_number"].ToString();
		string supp_inv = dr["inv_number"].ToString();
		string purchase_id = dr["purchase_id"].ToString();
		string amount = dr["amount"].ToString();
		string date = dr["date_invoiced"].ToString();
		if(date != "")
			date = DateTime.Parse(date).ToString("dd-MM-yyyy");
		dTotalFreight += double.Parse(freight);
		dTotalNoGST += double.Parse(amount);
		dTotalTax += double.Parse(tax);
		Response.Write("<td align=center>" + name + "</td>");
		Response.Write("<td align=center><a title='view purchase details' href='purchase.aspx?t=pp&n="+ purchase_id +"' class=o target=new>" + invoice_number + "</a></td>");
		Response.Write("<td align=center>" + date + "</td>");
		Response.Write("<td>" + supp_inv + "</td>");
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
	
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<br><center>");
	Response.Write("<b> Total QTY: </b><font color=red size=2>"+ dTotalQTY.ToString() +"</font>");
	Response.Write("&nbsp;<b>Total Freight:</b> <font color=Green size=2>"+ dTotalFreight.ToString("c") +"</font>");
	Response.Write("&nbsp;&nbsp;<b>Total AMOUNT:</b> <font color=Green size=2>"+ dTotalNoGST.ToString("c") +"</font>");
	Response.Write(" <b>SUB Total:</b> <font color=Green size=2>"+ (dTotalFreight + dTotalNoGST).ToString("c") +"</font>");
	Response.Write("</center>");
		//write xml data file for chart image
	WriteXMLFile();

}

</script>
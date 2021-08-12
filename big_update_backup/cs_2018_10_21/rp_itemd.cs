<script runat=server>

bool DoItemDetails()
{
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
		m_dateSql = " AND p.date_invoiced BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		//m_dateSql = " AND p.date_invoiced >= '" + m_sdFrom + "' AND p.date_invoiced <= '" + m_sdTo + "' ";
		break;
	case 4:
		//m_dateSql = " AND MONTH(p.date_invoiced) >= '" + m_smFrom + "' AND MONTH(p.date_invoiced) <= '" + m_smTo + "' ";
		m_dateSql = " AND MONTH(p.date_invoiced) + YEAR(p.date_invoiced) >= '" + (int.Parse(m_smFrom) + int.Parse(DateTime.Now.ToString("yyyy"))) + "' AND MONTH(p.date_invoiced) + YEAR(p.date_invoiced) <= '" + (int.Parse(m_smTo) + int.Parse(DateTime.Now.ToString("yyyy"))) + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

	int rows = 0;
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT p.supplier_id, c.company, p.po_number, p.freight, c.trading_name, p.inv_number, p.id, p.date_invoiced ";
	sc += " , pi.price, pi.qty, pi.code, pi.supplier_code, pi.name, p.tax, p.total, p.total_amount ,p.gst_rate, p.exchange_rate ";
	sc += " FROM purchase p JOIN purchase_item pi ON pi.id = p.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = p.supplier_id ";
	sc += " WHERE 1=1 ";
	if(Session["branch_support"] != null)
	{
		sc += " AND p.branch_id = " + m_branchID;
	}
	if(IsInteger(m_code))
		sc += " AND pi.code = " + m_code + m_dateSql;
	if(m_code == "" )
		sc += m_dateSql;
	//else
	//	sc += " WHERE pi.supplier_code = '" + m_code + "' "+ m_dateSql;

	if(m_filter != "")
	{
		if(m_filter_type != "pi.id")
			sc += " AND "+ m_filter_type +" ";
			
		if(m_filter_type == "pi.id")
		{
			if(TSIsDigit(m_filter))
			{
				sc += " AND "+ m_filter_type +" ";
				sc += " = "+ m_filter;
			}
		}
		else if(m_filter_type == "pi.name")
		{
			sc += "LIKE '%"+ m_filter +"%' ";
		}
		else
			sc += " = "+ m_filter;
	}
	sc += " ORDER BY p.date_invoiced DESC ";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "report");
		if(rows > 0)
			m_code = ds.Tables["report"].Rows[0]["code"].ToString();
		else
		{
			Response.Write("<br><br><center><h3>Item not found : <font color=red>" + m_code + "</font></h3>");
			return false;
		}

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	PrintItemDetails();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
		
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintItemDetails()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(m_sdFrom != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to=" + m_sdTo +"";
	if(m_type != "")
		m_cPI.URI += "&type="+ m_type +"";
	if(Session["report_period"] != "" && Session["report_period"] != null)
		m_cPI.URI += "&pr="+ Session["report_period"].ToString() +"";
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	DataRow drp = null;
	GetProduct(m_code, ref drp);
	
	if(drp == null)
	{
		Response.Write("<br><h3>Error, product not found");
		m_bShowPic = false;
		return;
	}

	Response.Write("<br><center><h3>Item Purchase Details</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b></center><br>");

	Response.Write("<font size=+1 color=red>#" + m_code + " " + drp["supplier_code"].ToString() + " &nbsp&nbsp; " + drp["name"].ToString() + "</font>");
	
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Supplier</th>");
	Response.Write("<th>SuppINV#</th>");
	Response.Write("<th>PONUMBER#</th>");
	Response.Write("<th>INV Date</th>");
	Response.Write("<th>QTY</th>");
//	Response.Write("<th>Freight</th>");
	Response.Write("<th align=right>EXCHANGE-RATE</th>");
//	Response.Write("<th align=right>GST</th>");
//	Response.Write("<th align=right>Cost</th>");
	Response.Write("<th align=right>Total Cost</th>");
	//Response.Write("<th align=right>Ave_Cost</th>");
	//Response.Write("<th align=right>Total_Profit</th>");
//	Response.Write("<th align=right>Status</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	//for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	StringBuilder sb3 = new StringBuilder();

	int totalQty = 0;
	double dTotalAmount = 0;
	//double dTotalProfit = 0;
	double dTotalFreight = 0;
	bool bAlterColor = false;
	double dTotalGST = 0;
	double dGrandCost = 0;
	double dGrandGST = 0;
	double dGrandFreight = 0;
	double dGrandQTY = 0;

	for(; i < rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string supplier = dr["trading_name"].ToString();
		if(supplier == "")
			supplier = dr["company"].ToString();
	
		string invoice = dr["inv_number"].ToString();
		string po_number = dr["po_number"].ToString();
		string puchase_id = dr["id"].ToString();
		string date = dr["date_invoiced"].ToString();
		if(date != "" && date != null)
			date = DateTime.Parse(date).ToString("dd-MM-yyyy");
		string quantity = dr["qty"].ToString();
	//	string tax = dr["tax"].ToString();
		//string price = dr["price"].ToString();
		string cost = dr["price"].ToString();
		string freight = dr["freight"].ToString();
		string supplier_id = dr["supplier_id"].ToString();
		string purchase_id = dr["id"].ToString();
		int qty = MyIntParse(quantity);
		//double dPrice = MyDoubleParse(price);
		double dCost = MyDoubleParse(cost);
	//	if(tax == "" && tax == null)
	//		tax = "0";
		//DEBUG("gst_rate_percent = ", GetSiteSettings("gst_rate_percent", "12.5"));
	//	if(freight == "" && freight == null)
	//		freight = "0";
	//	double dFreight = MyDoubleParse(freight);
		
		//double dTax = (dCost + dFreight) * (double.Parse(dr["gst_rate"].ToString()));
	//	double dTax = (dCost) * (double.Parse(dr["gst_rate"].ToString()));
	
		dGrandCost += dCost;
		dGrandQTY += qty;
		dCost *= qty;
	//	dGrandGST += dTax;
	//	dGrandFreight += dFreight;
	//	string gstrate = "1";
	//if(dr["gst_rate"].ToString() != "0")
	//	gstrate = (1 + double.Parse(dr["gst_rate"].ToString())).ToString();

	//	dCost = (dCost * qty * double.Parse(gstrate)) + (dFreight * double.Parse(gstrate))  ;
		
		dTotalAmount += dCost;
	
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a title='view supplier' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
		Response.Write("id=" + supplier_id + "','', ' width=350,height=350'); viewcard_window.focus()\" class=o >"+ supplier +"</a>");
		Response.Write("</td>");
		Response.Write("<th><a href=purchase.aspx?t=pp&n=" + purchase_id + " class=o target=_new>" + invoice + "</a></td>");
		Response.Write("<th><a href=purchase.aspx?t=pp&n=" + purchase_id + " class=o target=_new>" + po_number + "</a></td>");
		Response.Write("<td align=center>" + date + "</td>");
		Response.Write("<td align=center>" + quantity + "</td>");
	//	Response.Write("<td align=right>" + dFreight.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dr["exchange_rate"].ToString() + "</td>");
	//	Response.Write("<td align=right>" + dTax.ToString("c") + "</td>");
//		Response.Write("<td align=right>" + double.Parse(cost).ToString("c") + "</td>");
		Response.Write("<td align=right>");
		//Response.Write("<a href=tp.aspx?n=sales_cost&inv=" + invoice + "&code=" + m_code + " class=o target=_blank>");
		Response.Write(dCost.ToString("c") + "</a></td>");

		Response.Write("</tr>");
		if(dCost > m_nMaxY)
			m_nMaxY = dCost;

		if(dCost < 0 && dCost < m_nMinY)
			m_nMinY = dCost;

		//xml chart data
		x = (i*10).ToString();
		m_nMaxX = i*10;

		y = qty.ToString();
		
		supplier = StripHTMLtags(EncodeQuote(supplier));
		//supplier = XMLDecoding(supplier);

		legend = supplier.Replace("&", " ");
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");

		x = (MyIntParse(x) + 5).ToString();
		y = dCost.ToString();
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x");
		if(m_bHasLegends)
			sb2.Append(" legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");

		/*x = (MyIntParse(x) + 5).ToString();
		y = dPrice.ToString();
		sb3.Append("<chartdata>\r\n");
		sb3.Append("<x");
		if(m_bHasLegends)
			sb3.Append(" legend='" + legend + "'");
		sb3.Append(">" + x + "</x>\r\n");
		sb3.Append("<y>" + y + "</y>\r\n");
		sb3.Append("</chartdata>\r\n");
		*/
	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	/*m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb3.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	*/
	m_IslandTitle[0] = "--Profit";
	m_IslandTitle[1] = "--Cost";
	//m_IslandTitle[2] = "--Sales Price";
	m_nIsland = 3;
	
	Response.Write("<tr valign=bottom bgcolor=#EEEEEE><td colspan=4><br><font size=2><b>SUB Total:</td><th>"+ dGrandQTY +"</td>");
	//Response.Write("<th align=right>"+ dGrandFreight.ToString("C") +"</td><td>&nbsp;</td><th align=right>"+ dGrandGST.ToString("C") +"</td>");
//	Response.Write("<td>&nbsp;</td><th align=right>"+ dGrandCost.ToString("C") +"</td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<th align=right>"+ dTotalAmount.ToString("C") +"</td>");
	Response.Write("</tr>");
	dGrandCost = 0;
		dGrandQTY = 0;
		dGrandGST = 0;
		dGrandFreight = 0;
		dTotalAmount = 0;
	for(i=0; i <rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		//string tax = dr["tax"].ToString();
		string cost = dr["price"].ToString();
	//	string freight = dr["freight"].ToString();
		string quantity = dr["qty"].ToString();
		int qty = MyIntParse(quantity);
		double dCost = MyDoubleParse(cost);
		//if(tax == "" && tax == null)
		//	tax = "0";
		//if(freight == "" && freight == null)
		//	freight = "0";
		//double dFreight = MyDoubleParse(freight);
		//double dTax = (dCost + dFreight) * (double.Parse(dr["gst_rate"].ToString()));
		dGrandCost += dCost;
		dGrandQTY += qty;
		//dGrandGST += dTax;
		//dGrandFreight += dFreight;
		//string gstrate = "1";
	//if(dr["gst_rate"].ToString() != "0")
	//	gstrate = (1 + double.Parse(dr["gst_rate"].ToString())).ToString();
	//	dCost = (dCost * qty * double.Parse(gstrate)) + (dFreight * double.Parse(gstrate))  ;
		dCost *=qty;
		dTotalAmount += dCost;
	
	}

	Response.Write("<tr valign=bottom bgcolor=#EEEEEE><td colspan=4><font size=2><b>GRAND TOTAL:</td><th>"+ dGrandQTY +"</td>");
	//Response.Write("<th align=right>"+ dGrandFreight.ToString("C") +"</td><td>&nbsp;</td><th align=right>"+ dGrandGST.ToString("C") +"</td>");
//	Response.Write("<td>&nbsp;</td><th align=right>"+ dGrandCost.ToString("C") +"</td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("<th align=right>"+ dTotalAmount.ToString("C") +"</td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	Response.Write("<br><center><h4>");
/*	Response.Write("<b>Total Qty : </b><font color=red>" + totalQty + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b>Total Freight : </b><font color=red>" + dTotalFreight.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b>Total GST : </b><font color=red>" + dTotalGST.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b>Total Amount : </b><font color=red>" + dTotalAmount.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	//Response.Write("<b>Total Profit : </b><font color=red>" + dTotalProfit.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</h4><br>");
	*/
	//write xml data file for chart image
	WriteXMLFile();

}

</script>

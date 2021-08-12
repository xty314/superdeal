<script runat=server>

bool DoItemDetails()
{
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
		//m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59"+"' ";
		break;
	case 4:
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

	int rows = 0;
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT c.trading_name, c.name, c.company, i.invoice_number, i.commit_date, i.paid ";
	sc += ", s.quantity, s.code, s.commit_price, s.supplier_price, s.discount_percent";
	sc += " FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN card c ON c.id = i.card_id ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql;
	if(m_code != "")
	{
	if(IsInteger(m_code))
		sc += " AND s.code = " + m_code;
	else
		sc += " AND s.supplier_code = '" + m_code + "' ";
	}
	if(Session["branch_support"] != null && m_branchID != "0")
	{
		sc += " AND i.branch = " + m_branchID;
	}
	sc += " ORDER BY i.commit_date DESC ";
//DEBUG("sc = ",sc);
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
	{
		if(m_bGBSetShowPicOnReport)
		{
			DrawChart();
			string uname = EncodeUserName();
			Response.Write("<img src=" + m_picFile + ">");
		}
	}	
	/*piecharts pc = new piecharts();
	Bitmap objBitmap = pc.GetPieChart(ds.Tables["report"].Rows, "quantity", "invoice_number", "", 400);
	if(objBitmap == null)
		return true;

	string fn = DateTime.Now.ToOADate().ToString();
	fn = fn.Replace(".", "_") + ".jpg";
	objBitmap.Save(Server.MapPath(".") + "\\" + fn);
	Response.Write("<center><img src=" + fn + ">");
	*/
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
	if(m_type != null)
		m_cPI.URI += "&type="+ m_type;	
	m_cPI.URI += "&pr="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;

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

	Response.Write("<br><center><h3>Item Sales Detail</h3>");
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			Response.Write("<h5>Branch: "+GetBranchName(m_branchID) +"</h5>");
		else
			Response.Write("<h5>Branch: ALL</h5><br>");
	}
	Response.Write("<b>Date Period : " + m_datePeriod + "</b></center>");

	Response.Write("<font size=+1 color=red>#" + m_code + " " + drp["supplier_code"].ToString() + " &nbsp&nbsp; " + drp["name"].ToString() + "</font>");
	
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Customer</th>");
	Response.Write("<th>Invoice #</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th align=right>Sales_Price</th>");
	Response.Write("<th align=right>Ave_Cost</th>");
	Response.Write("<th align=right>Total_Profit</th>");
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

	double totalQty = 0;
	double dTotalAmount = 0;
	double dTotalProfit = 0;
	double dTotalCost = 0;
	bool bAlterColor = false;
	for(; i < rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string customer = dr["trading_name"].ToString();
		if(customer == "")
			customer = dr["company"].ToString();
		if(customer == "")
			customer = dr["name"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy");
		string quantity = dr["quantity"].ToString();
		string price = dr["commit_price"].ToString();
		string d_price = dr["discount_percent"].ToString();
		string cost = dr["supplier_price"].ToString();

		double qty = MyDoubleParse(quantity);
		double dPrice = MyDoubleParse(price);
		double d_dprice = MyDoubleParse(d_price);
		double dCost = MyDoubleParse(cost);
		if(d_dprice != 0)
		   dPrice = dPrice * (1-d_dprice/100);
		else
		   dPrice = dPrice;
		double dProfit = (dPrice - dCost) * qty;
		totalQty += qty;
		dTotalAmount += dPrice * qty;
		dTotalProfit += dProfit;
		dTotalCost += dCost;
//		string status = dr["paid"].ToString();
//		if(status == "1")
//			status = "Closed";
//		else
//			status = "Open";


		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + customer + "</td>");
		Response.Write("<td><a href=invoice.aspx?" + invoice + " class=o>" + invoice + "</a></td>");
		Response.Write("<td align=center>" + date + "</td>");
		Response.Write("<td align=center>" + quantity + "</td>");
		Response.Write("<td align=right>" + dPrice.ToString("c") + "</td>");
		Response.Write("<td align=right>");
		//Response.Write("<a href=tp.aspx?n=sales_cost&inv=" + invoice + "&code=" + m_code + " class=o target=_blank>");
		//Response.Write(dCost.ToString("c") + "</a></td>");
		Response.Write(dCost.ToString("c") + "</td>");

		//profit
		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");

		Response.Write("</tr>");
		if(dPrice > m_nMaxY)
			m_nMaxY = dPrice;

		if(dPrice < 0 && dPrice < m_nMinY)
			m_nMinY = dPrice;

		//xml chart data
		x = (i*10).ToString();
		m_nMaxX = i*10;
	
		y = dProfit.ToString();
		customer = StripHTMLtags(EncodeQuote(customer));
		legend = customer.Replace("&", " ");
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

		x = (MyIntParse(x) + 5).ToString();
		y = dPrice.ToString();
		sb3.Append("<chartdata>\r\n");
		sb3.Append("<x");
		if(m_bHasLegends)
			sb3.Append(" legend='" + legend + "'");
		sb3.Append(">" + x + "</x>\r\n");
		sb3.Append("<y>" + y + "</y>\r\n");
		sb3.Append("</chartdata>\r\n");

	}

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb3.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Profit";
	m_IslandTitle[1] = "--Cost";
	m_IslandTitle[2] = "--Sales Price";
	m_nIsland = 3;
	
	Response.Write("<tr><td colspan=8>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3 ><b>SUB Total : &nbsp; </b></td>");
	Response.Write("<td align=middle >" + totalQty.ToString() + "</td>");
	Response.Write("<td align=right >" + dTotalAmount.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dTotalCost.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dTotalProfit.ToString("c") + "</td>");
//	Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	
	totalQty = 0;
	dTotalAmount = 0;
	dTotalProfit =0;
	dTotalCost = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string quantity = dr["quantity"].ToString();
		string price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();
		string d_price = dr["discount_percent"].ToString();
		double d_dprice = MyDoubleParse(d_price);
		double qty = MyDoubleParse(quantity);
		double dPrice = MyDoubleParse(price);
		double dCost = MyDoubleParse(cost);
		if(d_dprice != 0)
		   dPrice = dPrice * (1-d_dprice/100);
		else
		   dPrice = dPrice;
		   
		double dProfit = (dPrice - dCost) * qty;
		
		totalQty += qty;
		dTotalAmount += dPrice * qty;
		dTotalProfit += dProfit;
		dTotalCost += dCost;

	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=3><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=middle>" + totalQty.ToString() + "</td>");
	Response.Write("<td align=right>" + dTotalAmount.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dTotalCost.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dTotalProfit.ToString("c") + "</td>");
	//Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("</table>");

	Response.Write("<br><center><h4>");
/*	Response.Write("<b>Total Sales Qty : </b><font color=red>" + totalQty + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b>Total Sales Amount : </b><font color=red>" + dTotalAmount.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<b>Total Profit : </b><font color=red>" + dTotalProfit.ToString("c") + "</font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("</h4><br>");
*/	
	//write xml data file for chart image
	WriteXMLFile();

}

</script>

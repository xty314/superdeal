<script runat=server>

bool DoItemSummary()
{
	m_tableTitle = "Item Sales Summary";
	
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
		//m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + "' ";
		break;
	case 4:
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();
	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT s.code, s.supplier_code, c.cat, c.s_cat, c.ss_cat, s.name, sum(s.commit_price * s.quantity) AS sales_amount  ";
	sc += ", sum((s.commit_price - s.supplier_price) * s.quantity) AS rough_profit ";
	sc += ", sum(s.supplier_price * s.quantity) AS supplier_price, sum(s.quantity) AS sales_qty";
	sc += " FROM sales s ";
	sc += " JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN code_relations c ON c.code = s.code  ";
	sc += " WHERE 1=1 ";
	if(Session["slt_code"] != null && Session["slt_code"] != "")
		sc += " AND s.code = "+ Session["slt_code"];
	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		if(TSIsDigit(Request.QueryString["code"].ToString()))
			sc += " AND s.code = "+ Request.QueryString["code"];
	if(Session["branch_support"] != null && m_branchID != "0")
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			sc += " AND i.branch = " + m_branchID;
	}
	if(m_brand != "" && m_brand != null && m_brand != "all")
		sc += " AND c.brand = '"+ m_brand +"'";
	if(m_cat != "" && m_cat != null &&  m_cat != "all")
		sc += " AND c.cat = '"+ m_cat +"'";
	if(m_scat != "" && m_scat != null && m_scat != "all")
		sc += " AND c.s_cat = '"+ m_scat +"'";
	if(m_sscat != "" && m_sscat != null && m_sscat != "all")
		sc += " AND c.ss_cat = '"+ m_sscat +"'";
	sc += m_dateSql;
	sc += " GROUP BY s.code, s.supplier_code, s.name, c.cat, c.s_cat, c.ss_cat ";
	sc += " ORDER BY sum(s.quantity) DESC ";
//	sc += " ORDER BY c.cat, c.s_cat, c.ss_cat, s.name ";
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
	
	if(g_bPDA)
	{
		PrintReportHeaderPDA("");
		PrintItemSummaryPDA();
		return true;
	}
	
	PrintReportHeader("");
	PrintItemSummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
	/*piecharts pc = new piecharts();
	string uname = EncodeUserName();
	Response.Write("<img src=" + m_picFile + ">");

	Bitmap objBitmap = pc.GetPieChart(ds.Tables["report"].Rows, "sales_qty", "supplier_code", "", 400);
	if(objBitmap == null)
		return true;
	// Since we are outputting a Jpeg, set the ContentType appropriately
//	Response.ContentType = "image/jpeg";

	// Save the image to the OutputStream
//	objBitmap.Save(Response.OutputStream, ImageFormat.Jpeg);
//	objBitmap.Save(Server.MapPath(".") + "\\report.jpg");
//	Response.Write("<img src=report.jpg>");
	string fn = DateTime.Now.ToOADate().ToString();
	fn = fn.Replace(".", "_") + ".jpg";
	objBitmap.Save(Server.MapPath(".") + "\\" + fn);
	Response.Write("<center><img src=" + fn + ">");
	*/
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintItemSummary()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 500;
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
	if(m_brand != "" && m_brand != null)
		m_cPI.URI += "&brand="+ m_brand +"";
	if(m_cat != "" && m_cat != null)
		m_cPI.URI += "&cat="+ m_cat +"";
	if(m_scat != "" && m_scat != null)
		m_cPI.URI += "&scat="+ m_scat +"";
	if(m_sscat != "" && m_sscat != null)
		m_cPI.URI += "&sscat="+ m_sscat +"";

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>CAT</th>");
	Response.Write("<th align=left>SCAT</th>");
	Response.Write("<th align=left>SSCAT</th>");
	Response.Write("<th align=left>CODE</th>");
	Response.Write("<th align=left>M_PN</th>");
	Response.Write("<th align=left>DESCRIPTION</th>");
	Response.Write("<th>SALES TOTAL QTY</th>");
	Response.Write("<th align=right>SALES TOTAL AMOUNT</th>");
	Response.Write("<th align=right>SALES TOTAL AMOUNT (Inl GST)</th>");
	Response.Write("<th align=right>ROUGH PROFIT</th>");
//	Response.Write("<th>GST</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		m_bShowPic = false;
		return;
	}
	//----for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	//----
	
	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalProfit = 0;
	int dTotalQTY = 0;
	bool bAlterColor = false;
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sales_qty = dr["sales_qty"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		string profit = dr["rough_profit"].ToString();
		double salesAmountGST = MyDoubleParse(sales_amount)*1.15;
		salesAmountGST = Math.Round(salesAmountGST, 2);

		dTotalQTY += int.Parse(sales_qty);
		dTotalNoGST += MyDoubleParse(sales_amount);
		dTotalProfit += MyDoubleParse(profit);	
//		name = StripHTMLtags(EncodeQuote(name));		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + cat + "</td>");
		Response.Write("<td>" + s_cat + "</td>");
		Response.Write("<td>" + ss_cat + "</td>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center><a href=report.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
		Response.Write("<td align=right>" + MyDoubleParse(sales_amount).ToString("c") + "</td>");
		Response.Write("<td align=right>" + salesAmountGST.ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(profit).ToString("c") + "</td>");
		Response.Write("</tr>");

		if(double.Parse(sales_amount) > m_nMaxY)
			m_nMaxY = double.Parse(sales_amount);

		if(double.Parse(sales_amount) < 0 && double.Parse(sales_amount) < m_nMinY)
			m_nMinY = double.Parse(sales_amount);

		//xml chart data
		x = (i*10).ToString();
		m_nMaxX = i*10;
		y = sales_amount;
		name = StripHTMLtags(name);	
//		name = XMLDecoding(name);	
		name = name.Replace("'", " ");				
		legend = name.Replace("&", " ");		
//		legend = name.Replace(";", " ");
								
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");
		
		x = (MyIntParse(x) + 5).ToString();
		y = profit;
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");
			
	}
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Total Profit";
	m_IslandTitle[1] = "--Total Sales";
	m_nIsland = 2;

	Response.Write("<tr><td colspan=8></td></tr>");
	Response.Write("<tr bgcolor=#EEEEEE><th align=left colspan=3>SUB Total:</td><th>"+ dTotalQTY +"</td><th align=right>"+ dTotalNoGST.ToString("c") +"</th><th align=right>"+ (dTotalNoGST*1.15).ToString("c") +"</th><th align=right>"+ dTotalProfit.ToString("c") +"</th></tr>");

	dTotalNoGST = 0;
	dTotalProfit = 0;
	dTotalQTY = 0;
	
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string sales_qty = dr["sales_qty"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		string profit = dr["rough_profit"].ToString();

		dTotalQTY += int.Parse(sales_qty);
		dTotalNoGST += MyDoubleParse(sales_amount);
		dTotalProfit += MyDoubleParse(profit);
	}
	Response.Write("<tr bgcolor=#EEE54E><th align=left colspan=6>GRAND TOTAL:</td><th>"+ dTotalQTY +"</td><th align=right>"+ dTotalNoGST.ToString("c") +"</th><th align=right>"+ (dTotalNoGST*1.15).ToString("c") +"</th><th align=right>"+ dTotalProfit.ToString("c") +"</th></tr>");
	Response.Write("<tr><td colspan=9>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

		//write xml data file for chart image
	WriteXMLFile();
		
}
void PrintItemSummaryPDA()
{
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>CODE</th>");
	Response.Write("<th align=left>DESC</th>");
	Response.Write("<th>QTY</th>");
	Response.Write("<th align=right>AMOUNT</th>");
	Response.Write("<th align=right>PROFIT</th>");
	Response.Write("</tr>");

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		m_bShowPic = false;
		return;
	}
	
	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalProfit = 0;
	int dTotalQTY = 0;
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string sales_qty = dr["sales_qty"].ToString();
		string sales_amount = dr["sales_amount"].ToString();
		string profit = dr["rough_profit"].ToString();
		double salesAmountGST = MyDoubleParse(sales_amount)*1.15;
		salesAmountGST = Math.Round(salesAmountGST, 2);

		dTotalQTY += int.Parse(sales_qty);
		dTotalNoGST += MyDoubleParse(sales_amount);
		dTotalProfit += MyDoubleParse(profit);	
//		name = StripHTMLtags(EncodeQuote(name));		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center><a href=report.aspx?type=1&code=" + code + " class=o title='Click to view details'>" + sales_qty + "</a></td>");
		Response.Write("<td align=right>" + MyDoubleParse(sales_amount).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(profit).ToString("c") + "</td>");
		Response.Write("</tr>");
	}

	Response.Write("<tr bgcolor=#EEEEEE><th align=left colspan=2>SUB Total:</td><th>"+ dTotalQTY +"</td><th align=right>"+ dTotalNoGST.ToString("c") +"</th><th align=right>"+ dTotalProfit.ToString("c") +"</th></tr>");
	Response.Write("</table>");
}
</script>

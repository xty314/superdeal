<script runat=server>

bool DoItemSummary()
{
	m_tableTitle = "Item Purchase Summary";
	
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
	string sc = "";
	sc += " SET DATEFORMAT dmy ";
	sc += " SELECT pi.code, pi.supplier_code, pi.name ";
	sc += ", ISNULL(SUM(pi.qty), 0) AS purchase_qty ";
	sc += ", ISNULL(SUM(pi.price * pi.qty ),0) AS purchase_amount ";
	
	sc += " FROM purchase p JOIN purchase_item pi ON pi.id = p.id ";
//	sc += " JOIN code_relations c ON c.code = pi.code ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql;
	if(Session["branch_support"] != null)
	{
		sc += " AND p.branch_id = " + m_branchID;
	}
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
	sc += " GROUP BY pi.code, pi.supplier_code, pi.name ";
//	sc += " HAVING sum(pi.qty) > 0 ";
	sc += " ORDER BY purchase_qty DESC ";
/*	sc += " SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT code, supplier_code, name ";
	sc += ", ISNULL(( ";
	{
		sc += " SELECT SUM(pi.qty) ";
		sc += " FROM purchase_item pi JOIN purchase p ON p.id = pi.id ";
		sc += " WHERE pi.code = pd.code AND pi.supplier_code = pd.supplier_code ";
		if(Session["branch_support"] != null)
		{
			sc += " AND p.branch_id = " + m_branchID;
		}
		sc += m_dateSql;
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
	}
	sc += " ), 0) AS purchase_qty ";

	sc += ", ISNULL(( ";
	{
		//sc += " SELECT SUM(pi.price * pi.qty + (pi.price * 0.125)) ";
		sc += " SELECT SUM(pi.price * pi.qty ) ";
		sc += " FROM purchase_item pi JOIN purchase p ON p.id = pi.id ";
		sc += " WHERE pi.code = pd.code ";
		if(Session["branch_support"] != null)
		{
			sc += " AND p.branch_id = " + m_branchID;
		}
		sc += m_dateSql;
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
	}
	sc += " ), 0) AS purchase_amount ";
	
	sc += " FROM code_relations pd ";
	sc += " WHERE 1=1 ";
	sc += " AND (SELECT SUM(pi.qty) ";
	sc += " FROM purchase_item pi JOIN purchase p ON pi.id = p.id ";
	sc += " WHERE pi.code = pd.code AND pi.supplier_code = pd.supplier_code " + m_dateSql +"";
	if(Session["branch_support"] != null)
	{
		sc += " AND p.branch_id = " + m_branchID;
	}
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

	sc += ") > 0 ";	
	sc += " ORDER BY purchase_qty DESC ";	
	*/
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
	
	PrintItemSummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintItemSummary()
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

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>CODE</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>DESCRIPTION</th>");
	Response.Write("<th>TOTAL QTY</th>");
	Response.Write("<th align=right>Total COST</th>");
	//Response.Write("<th>Rough_Profit</th>");
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
	StringBuilder sb3 = new StringBuilder();
	//----
	
	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalQTY = 0;
	bool bAlterColor = false;
	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		if(name != null && name != "")
			name = StripHTMLtags(EncodeQuote(name));
		string purchase_qty = dr["purchase_qty"].ToString();
		string purchase_amount = dr["purchase_amount"].ToString();
		//string profit = dr["rough_profit"].ToString();
		dTotalQTY += double.Parse(purchase_qty);
		dTotalWithGST += double.Parse(purchase_amount) ;
		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center><a href='"+ Request.ServerVariables["URL"]+"?type=1&code=" + code + "' class=o title='Click to view details'>" + purchase_qty + "</a></td>");
		Response.Write("<td align=right>" + MyDoubleParse(purchase_amount).ToString("c") + "</td>");
		//Response.Write("<td align=right>" + MyDoubleParse(profit).ToString("c") + "</td>");
		Response.Write("</tr>");

		if(double.Parse(purchase_amount) > m_nMaxY)
			m_nMaxY = double.Parse(purchase_amount);

		if(double.Parse(purchase_amount) < 0 && double.Parse(purchase_amount) < m_nMinY)
			m_nMinY = double.Parse(purchase_amount);

		//xml chart data
		x = (i*10).ToString();
		m_nMaxX = i*10;
		y = purchase_qty;
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
		y = purchase_amount;
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");

	}
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Total QTY";
	m_IslandTitle[1] = "--Total Amount";
	m_nIsland = 2;
	Response.Write("<tr valign=bottom bgcolor=#EEEEEE><td colspan=3><br><font size=2><b>SUB Total:</td><th align=center>"+ dTotalQTY.ToString() +"</td>");
	Response.Write("<th align=right>"+ dTotalWithGST.ToString("c") +"</td>");
	Response.Write("</tr>");
	Response.Write("<tr valign=bottom bgcolor=#EEEEEE><td colspan=5><hr size=1></td></tr>");
	dTotalQTY = 0;
	dTotalWithGST = 0;
	for(i=0; i <rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string purchase_qty = dr["purchase_qty"].ToString();
		string purchase_amount = dr["purchase_amount"].ToString();
		
		dTotalWithGST += double.Parse(purchase_amount);
		dTotalQTY += double.Parse(purchase_qty);
			
	}
	Response.Write("<tr valign=bottom bgcolor=#EEEEEE><td colspan=3><font size=2><b>GRAND TOTAL:</td><th align=center>"+ dTotalQTY.ToString() +"</td>");
	Response.Write("<th align=right>"+ dTotalWithGST.ToString("c") +"</td>");
	Response.Write("</tr>");

	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	
		//write xml data file for chart image
	WriteXMLFile();
		
}

</script>

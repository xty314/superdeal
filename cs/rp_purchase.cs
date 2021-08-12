<script runat=server>
//bool m_bCompair = false;

bool DoPurchaseSummary()
{
	m_tableTitle = "Sales Person Purchase Summary for (<font color=green size=4>"+ m_sales_person.ToUpper() +"</font>)";
	
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
		//m_dateSql = " AND p.date_invoiced >= '" + m_sdFrom + "' AND p.date_invoiced <= '" + m_sdTo + " 23:59 "+"' ";
		break;
	case 4:
		m_bCompair = true;
		//m_dateSql = " AND MONTH(p.date_invoiced) >= '" + m_smFrom + "' AND MONTH(p.date_invoiced) <= '" + m_smTo + "' ";
		m_dateSql = " AND MONTH(p.date_invoiced) + YEAR(p.date_invoiced) >= '" + (int.Parse(m_smFrom) + int.Parse(DateTime.Now.ToString("yyyy"))) + "' AND MONTH(p.date_invoiced) + YEAR(p.date_invoiced) <= '" + (int.Parse(m_smTo) + int.Parse(DateTime.Now.ToString("yyyy"))) + "' ";		break;
	default:
		break;
	}
	
	ds.Clear();

	string sc = " SET DATEFORMAT dmy ";
	if(m_bCompair)
	{
		int nDifferent = 0;
		if(int.Parse(m_syFrom) < int.Parse(m_syTo))
			nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
				
		m_smTo = (int.Parse(m_smTo) + nDifferent).ToString();
		int nPlus = 0;

		sc += " SELECT SUM(total) AS amount ";
		for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
		{
			int nMonth = ii;
			int nYear = int.Parse(m_syFrom);
			int nn = 0;

			if(nMonth > 12)
			{
				string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
				nn = int.Parse(snn[0].ToString());
			
				nPlus++;
				if(nPlus == 13)
					nPlus = 1;
				nMonth = nPlus;
				nYear = (int.Parse(m_syFrom) + nn);
			}
			sc += ", ISNULL(( ";
			{
				sc += " SELECT SUM(p1.total) ";
				sc += " FROM purchase p1 ";
				sc += " WHERE MONTH(p1.date_invoiced) = '" + nMonth + "' ";
				sc += " AND YEAR(p1.date_invoiced) = '" + nYear + "' ";
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
					sc += " AND p1.staff_id = "+ m_sales_id;
				
			}
			sc += " ), 0) AS '"+ nYear + nMonth +"' ";
		}
		sc += " FROM purchase ";
		if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
			sc += " WHERE staff_id = "+ m_sales_id;
		if(Session["branch_support"] != null)
		{
			sc += " AND p.branch_id = " + m_branchID;
		}
	}
	else
	{
		sc += " SELECT c1.id AS staff_id, c1.name AS staff, c2.id AS supplier_id, c2.name AS supplier, p.id AS purchase_id, p.po_number ";
		sc += " , p.inv_number, p.exchange_rate, p.date_invoiced, sum(p.total) AS amount, sum(p.tax) AS tax, sum(p.freight) AS freight, sum(total_amount) AS total_amount  ";
		sc += " FROM purchase p JOIN card c1 ON c1.id = p.staff_id ";
		sc += " JOIN card c2 ON c2.id = p.supplier_id ";
		sc += " WHERE c1.type = 4 ";
		if(Session["branch_support"] != null)
		{
			sc += " AND p.branch_id = " + m_branchID;
		}
		sc += m_dateSql;
		if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
			sc += " AND c1.id = "+ m_sales_id;
		sc += " GROUP BY c1.id, c1.name, c2.id, c2.name , p.id, p.po_number ";
		sc += " , p.inv_number, p.exchange_rate, p.date_invoiced ";
		sc += " ORDER BY date_invoiced DESC ";	
	}
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
	
//	m_dra = ds.Tables["report"].Select("amount > 0", "amount DESC");

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
		
	PrintPurchaseSummary();
	
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintPurchaseSummary()
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

//	int rows = m_dra.Length;
	int rows = ds.Tables["report"].Rows.Count;
//	if(ds.Tables["report"] != null)
//		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;

	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(m_sdFrom != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to=" + m_sdTo +"";
	if(m_type != "")
		m_cPI.URI += "&type="+ m_type +"";
	if(Session["report_period"] != "" && Session["report_period"] != null)
		m_cPI.URI += "&pr="+ Session["report_period"].ToString() +"";
	if(m_sales_person != "")
		m_cPI.URI += "&s="+ HttpUtility.UrlEncode(m_sales_person);
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(!m_bCompair)
	{
		Response.Write("<th align=left>SALES NAME</th>");
		Response.Write("<th align=left>SUPPLIER</th>");
		Response.Write("<th align=left>PO_NUMBER</th>");
		Response.Write("<th align=left>SUPP_INV#</th>");
		Response.Write("<th align=left>INVOICE DATE</th>");
		Response.Write("<th align=right>EXCHANGE-RATE</th>");
		Response.Write("<th align=right>GST</th>");
		Response.Write("<th align=right>FREIGHT</th>");
		Response.Write("<th align=right>COST</th>");
		Response.Write("<th align=right>TOTAL COST(inc. GST)</th>");
		//Response.Write("<th>Total_Orders</th>");
		//Response.Write("<th align=right>Total Purchase Amount</th>");
	}
	else
	{
		int nDifferent = 0;
		if(int.Parse(m_syFrom) < int.Parse(m_syTo))
			nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
		int nPlus = 0;
		for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
		{
			int nMonth = ii;
			int nYear = int.Parse(m_syFrom);
			int nn = 0;
			if(nMonth > 12)
			{
				string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
				nn = int.Parse(snn[0].ToString());
				
				nPlus++;
				if(nPlus == 13)
					nPlus = 1;
				nMonth = nPlus;
				nYear = int.Parse(m_syFrom) + nn;
		
			}
			string s_month = m_EachMonth[nMonth-1] +"-"+ nYear;
			Response.Write("<th>"+ s_month +"</th>");
		
		}
	
	}
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
	double dTotalFreight = 0;
	bool bAlterColor = false;
	double dTotalQTY = 0;
	//for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	StringBuilder sb11 = new StringBuilder();

	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
//		DataRow dr = m_dra[i];
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		if(!m_bCompair)
		{
			string supplier_id = dr["supplier_id"].ToString();
			string staff = dr["staff"].ToString();
			string supplier = dr["supplier"].ToString();
			string staff_id = dr["staff_id"].ToString();
			string purchase_id = dr["purchase_id"].ToString();
			string po_number = dr["po_number"].ToString();
			string inv_number = dr["inv_number"].ToString();
			string inv_date = dr["date_invoiced"].ToString();
			string total = dr["amount"].ToString();
			string tax = dr["tax"].ToString();
			string freight = dr["freight"].ToString();
			string exchange_rate = dr["exchange_rate"].ToString();
			dTotalNoGST += double.Parse(total);
			dTotalTax += double.Parse(tax); 
			dTotalFreight += double.Parse(freight); 
			dTotalWithGST += double.Parse(total) + double.Parse(tax) + double.Parse(freight);
			Response.Write("<td>" + staff + "</td>");
			Response.Write("<td>" + supplier + "</td>");
			Response.Write("<td><a title='View Purchase Details' href='purchase.aspx?t=pp&n="+ purchase_id +"' class=o target=new>" + po_number + "</a></td>");
			Response.Write("<td>" + inv_number + "</td>");
			Response.Write("<td>" + DateTime.Parse(inv_date).ToString("dd-MMM-yyyy") + "</td>");
			Response.Write("<td align=right>" + exchange_rate + "</td>");
			Response.Write("<td align=right>" + MyDoubleParse(tax).ToString("c") + "</td>");
			Response.Write("<td align=right>" + MyDoubleParse(freight).ToString("c") + "</td>");
			Response.Write("<td align=right>" + MyDoubleParse(total).ToString("c") + "</td>");
			Response.Write("<td align=right>" + (MyDoubleParse(total) + double.Parse(tax) + double.Parse(freight)).ToString("c") + "</td>");
			//Response.Write("<td>" + name + "</td>");
			//Response.Write("<td align=center>" + orders + "</td>");
			//Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");

			if(double.Parse(total) > m_nMaxY)
				m_nMaxY = double.Parse(total);

			if(double.Parse(total) < 0 && double.Parse(total) < m_nMinY)
				m_nMinY = double.Parse(total);

		//xml chart data
			x = (i*10).ToString();
			m_nMaxX = i*10;
			y = tax;

			staff = StripHTMLtags(EncodeQuote(staff));
			//staff = XMLDecoding(staff);
			legend = staff.Replace("&", " ");
			sb1.Append("<chartdata>\r\n");
			sb1.Append("<x");
			if(m_bHasLegends)
				sb1.Append(" legend='" + legend + "'");
			sb1.Append(">" + x + "</x>\r\n");
			sb1.Append("<y>" + y + "</y>\r\n");
			sb1.Append("</chartdata>\r\n");
			
			x = (MyIntParse(x) + 5).ToString();
			y = total;
			sb2.Append("<chartdata>\r\n");
			sb2.Append("<x");
			if(m_bHasLegends)
				sb2.Append(" legend='" + legend + "'");
			sb2.Append(">" + x + "</x>\r\n");
			sb2.Append("<y>" + y + "</y>\r\n");
			sb2.Append("</chartdata>\r\n");
		
		}
		else
		{

			double dTotalEachMonth = 0;

			int nDifferent = 0;
			if(int.Parse(m_syFrom) < int.Parse(m_syTo))
				nDifferent = (int.Parse(m_syTo) - int.Parse(m_syFrom)) * 12;
			int nPlus = 0;

			for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
			{
				int nMonth = ii;
				int nYear = int.Parse(m_syFrom);
				int nn = 0;
				if(nMonth > 12)
				{
					string snn = Math.Abs(double.Parse(nMonth.ToString()) / 12.1).ToString();
					nn = int.Parse(snn[0].ToString());
					
					nPlus++;
					if(nPlus == 13)
						nPlus = 1;
					nMonth = nPlus;
					nYear = int.Parse(m_syFrom) + nn;
			
				}
				dTotalEachMonth = double.Parse(dr[""+nYear + nMonth+""].ToString());
				Response.Write("<td align=center>"+ dTotalEachMonth.ToString("c") +"</th>");
				if(dTotalEachMonth > m_nMaxY)
					m_nMaxY = dTotalEachMonth;

				if(dTotalEachMonth < 0 && dTotalEachMonth < m_nMinY)
					m_nMinY = dTotalEachMonth;
				
			//xml chart data
				x = (nMonth*10).ToString();
				m_nMaxX = nMonth*10;
				dTotalWithGST += dTotalEachMonth;
				legend = (m_EachMonth[nMonth-1] +"-"+ nYear.ToString()).Replace("&", " ");
				y = dTotalEachMonth.ToString();
						
				sb11.Append("<chartdata>\r\n");
				sb11.Append("<x");
				if(m_bHasLegends)
					sb11.Append(" legend='" + legend + "'");
				sb11.Append(">" + x + "</x>\r\n");
				sb11.Append("<y>" + y + "</y>\r\n");
				sb11.Append("</chartdata>\r\n");
	
			}
		}
		Response.Write("</tr>");
		
	}
	
	if(m_bCompair)
	{
	
		int k = 1;
		m_sb.Append("<chartdataisland>\r\n");
				
		m_sb.Append(sb11.ToString());
		m_sb.Append("</chartdataisland>\r\n");
		
		
		m_IslandTitle[0] = "--Total Amount";
		m_nIsland = 1;
	
	}
	else
	{
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb1.ToString());
		m_sb.Append("</chartdataisland>\r\n");

		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb2.ToString());
		m_sb.Append("</chartdataisland>\r\n");

		m_IslandTitle[0] = "--Orders";	
		m_IslandTitle[1] = "--Amount";
		m_nIsland = 2;

	}
	if(!m_bCompair)
	{
		Response.Write("<tr valign=bottom bgcolor=#EEEEEE><td colspan=6><br><font size=2><b>SUB Total:</td><th align=right>"+ dTotalTax.ToString("c") +"</td><th align=right>"+ dTotalFreight.ToString("C") +"</td><th align=right>"+ dTotalNoGST.ToString("C") +"</td><th align=right>"+ dTotalWithGST.ToString("C") +"</td></tr>");
		
		dTotalNoGST = 0;
		dTotalTax = 0; 
		dTotalFreight = 0; 
		dTotalWithGST = 0;
		for(i=0; i <rows; i++)
		{
			DataRow dr = ds.Tables["report"].Rows[i];
			string total = dr["amount"].ToString();
			string tax = dr["tax"].ToString();
			string freight = dr["freight"].ToString();
			dTotalNoGST += double.Parse(total);
			dTotalTax += double.Parse(tax); 
			dTotalFreight += double.Parse(freight); 
			dTotalWithGST += double.Parse(total) + double.Parse(tax) + double.Parse(freight);
		}

		Response.Write("<tr valign=bottom bgcolor=#EEEAAA><td colspan=6><font size=2><b>GRAND TOTAL:</td><th align=right>"+ dTotalTax.ToString("c") +"</td><th align=right>"+ dTotalFreight.ToString("C") +"</td><th align=right>"+ dTotalNoGST.ToString("C") +"</td><th align=right>"+ dTotalWithGST.ToString("C") +"</td></tr>");
	Response.Write("<tr><td colspan=10>" + sPageIndex + "</td></tr>");	
	}
	
	Response.Write("</table>");
	Response.Write("<br><center>");
	
	
		//write xml data file for chart image
	WriteXMLFile();
}

</script>

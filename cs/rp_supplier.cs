<script runat=server>


bool DoSupplierSummary()
{
	m_bCompair = false;
	m_tableTitle = "Purchase Summary from <font color=lighblue>";
	
//	if(Session["customer_id"] == "" || Session["customer_id"] == null)
//		m_tableTitle += "(ALL Suppliers)";
	if(Session["customer_name"] != "" && Session["customer_name"] != null)
		m_tableTitle += "("+ (Session["customer_name"].ToString()).ToUpper() + ")";
	else
		m_tableTitle += "(ALL Suppliers)";
	m_tableTitle += "</font>";
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
		break;
	case 4:
		m_bCompair = true;
		break;
	default:
		break;
	}

	ds.Clear();
	string sc = "";
	sc = " SET DATEFORMAT dmy ";
	
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
				
				if(Session["customer_id"] != "" && Session["customer_id"] != null && Session["customer_id"] != "all")
					sc += " AND p1.supplier_id = "+ Session["customer_id"];
			}
			sc += " ), 0) AS '"+ nYear + nMonth +"' ";
		}
		sc += " FROM purchase WHERE 1=1 ";
		if(Session["customer_id"] != "" && Session["customer_id"] != null && Session["customer_id"] != "all")
			sc += " AND supplier_id = "+ Session["customer_id"];
		if(Session["branch_support"] != null)
					sc += " AND branch_id = " + m_branchID;
	}
	else
	{
		sc += " SELECT c.id, c.trading_name, name, c.company";
		sc += ", ISNULL(( ";
		{
			sc += " SELECT COUNT(*) ";
			sc += " FROM purchase p ";
			sc += " WHERE p.supplier_id = c.id ";
			if(Session["branch_support"] != null)
				sc += " AND branch_id = " + m_branchID;
			sc += m_dateSql;
		}
		sc += " ), 0) AS orders ";
		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(p.total) ";
			sc += " FROM purchase p ";
			sc += " WHERE p.supplier_id = c.id ";
			if(Session["branch_support"] != null)
				sc += " AND branch_id = " + m_branchID;
			sc += m_dateSql;
		}
		sc += " ), 0) AS amount ";

		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(p.tax) ";
			sc += " FROM purchase p ";
			sc += " WHERE p.supplier_id = c.id ";
			if(Session["branch_support"] != null)
				sc += " AND branch_id = " + m_branchID;
			sc += m_dateSql;
		}
		sc += " ), 0) AS tax ";

		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(p.freight) ";
			sc += " FROM purchase p ";
			sc += " WHERE p.supplier_id = c.id ";
			if(Session["branch_support"] != null)
				sc += " AND branch_id = " + m_branchID;
			sc += m_dateSql;
		}
		sc += " ), 0) AS freight ";
 
		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(p.total_amount) ";
			sc += " FROM purchase p ";
			sc += " WHERE p.supplier_id = c.id ";
			if(Session["branch_support"] != null)
				sc += " AND branch_id = " + m_branchID;
			sc += m_dateSql;
		}
		sc += " ), 0) AS total_amount ";

		sc += " FROM card c WHERE 1=1 ";
		if(Session["customer_id"] != null && Session["customer_id"] != "" && Session["customer_id"] != "all")
			sc += " AND c.id = " + Session["customer_id"];
		if(Session["customer_id"] == "all")
			sc += " ";
		sc += " AND (SELECT SUM(p.total) ";
		sc += " FROM purchase p ";
		sc += " WHERE p.supplier_id = c.id ";
		if(Session["branch_support"] != null)
				sc += " AND branch_id = " + m_branchID;
		sc += m_dateSql +" ) > 0 ";
		
		sc += " ORDER BY amount DESC ";
		
	
		/*sc += " SELECT c2.id AS supplier_id, c2.trading_name AS supplier, c2.name, c2.company, p.id AS purchase_id, p.po_number ";
		sc += " , p.inv_number, p.total AS amount, p.tax, p.date_invoiced ";
		sc += " FROM purchase p "; //JOIN card c1 ON c1.id = p.staff_id ";
		sc += " JOIN card c2 ON c2.id = p.supplier_id ";
		sc += " WHERE c2.type = 3 ";  //3 for supplier
		sc += " AND total > 0 ";
		sc += m_dateSql;
		if(Session["customer_id"] != null && Session["customer_id"] != "" && Session["customer_id"] != "all")
			sc += " AND c2.id = " + Session["customer_id"];
		if(Session["customer_id"] == "all")
			sc += " ";
		sc += " ORDER BY date_invoiced DESC ";	
		*/

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

	PrintCustomerSummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}
	Session["customer_name"] = null;
	Session["customer_id"] = null;
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintCustomerSummary()
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
	if(m_sdFrom != "")
		m_cPI.URI += "&frm="+ m_sdFrom +"&to=" + m_sdTo +"";
	if(m_type != "")
		m_cPI.URI += "&type="+ m_type +"";
	if(Session["report_period"] != "" && Session["report_period"] != null)
		m_cPI.URI += "&pr="+ Session["report_period"].ToString() +"";
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(!m_bCompair)
	{
		Response.Write("<th>SUPPLIER</th>");
		Response.Write("<th>TOTAL ORDERS</th>");
		Response.Write("<th align=right>TOTAL FREIGHT</th>");
		Response.Write("<th align=right>TOTAL TAX</th>");
		Response.Write("<th align=right>AMOUNT</th>");
		Response.Write("<th align=right>TOTAL AMOUNT</th>");
		/*Response.Write("<th>SALES NAME</th>");
		Response.Write("<th>SUPPLIER</th>");
		Response.Write("<th>PO_NUMBER</th>");
		Response.Write("<th>SUPP_INV#</th>");
		Response.Write("<th>INVOICE DATE</th>");
		Response.Write("<th align=right>GST</th>");
		Response.Write("<th align=right>Total</th>");
		*/
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
		//	string s_month = "";
		//	s_month = m_EachMonth[ii-1];
			string s_month = m_EachMonth[nMonth-1] +"-"+ nYear;
			Response.Write("<th>Total "+ s_month +"</th>");
		}
	}
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
			m_bShowPic = false;
		return;
	}

	//for xml chart data
	string x = "";
	string y = "";
	string color = "";
	string legend = "";
	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();

	StringBuilder sb11 = new StringBuilder();

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalFreight = 0;
	double dTotalQTY = 0;
	bool bAlterColor = false;

	for(; i<rows && i<end; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
//		DataRow dr = m_dra[i];
		string id = "";
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		if(!m_bCompair)
		{
		
			string supplier = dr["trading_name"].ToString();
			string total = dr["amount"].ToString();
			string freight = dr["freight"].ToString();
			string tax = dr["tax"].ToString();
			string orders = dr["orders"].ToString();
			id = dr["id"].ToString();
			
			if(supplier == "")
				supplier = dr["name"].ToString();
			string amount = total;
			string name = supplier;
			dTotalNoGST += double.Parse(total);
			dTotalQTY += double.Parse(orders); 

			dTotalTax += double.Parse(tax);
			dTotalFreight += double.Parse(freight); 
			dTotalWithGST += MyDoubleParse(dr["total_amount"].ToString());
			Response.Write("<td><a title='view supplier Detials' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + id + "', '', 'width=350, height=350, resizable=1'); viewcard_window.focus()\" class=o>"+ name +"</a></td>");
			//Response.Write("<td><a title='view all purchase details' href='rp_purinv.aspx?id="+ id +"' class=o>" + name.ToUpper() + "</a></td>");
			//Response.Write("<td align=center><a title='view all purchase details' href='p_report.aspx?spid="+ id +"&pr="+ m_nPeriod +"' class=o>" + orders + "</a></td>");
			Response.Write("<td align=center><a title='view all purchase details' href='p_report.aspx?spid="+ id +"&pr="+ m_nPeriod +"");
			if(m_sdFrom != "" && m_sdFrom != null)
				Response.Write("&frm="+ m_sdFrom +"&to="+ m_sdTo +"");
			Response.Write("' class=o>" + orders + "</a></td>");
			Response.Write("<td align=right>" + MyDoubleParse(freight).ToString("c") + "</td>");
			Response.Write("<td align=right>" + MyDoubleParse(tax).ToString("c") + "</td>");
			
			Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");
			Response.Write("<td align=right>" + (MyDoubleParse(amount) + MyDoubleParse(tax) + MyDoubleParse(freight)).ToString("c")+ "</td>");

			if(double.Parse(total) > m_nMaxY)
				m_nMaxY = double.Parse(total);

			if(double.Parse(total) < 0 && double.Parse(total) < m_nMinY)
				m_nMinY = double.Parse(total);
			
			//xml chart data
			x = (i*10).ToString();
			m_nMaxX = i*10;
			y = orders;
			supplier = StripHTMLtags(EncodeQuote(supplier));
		//	supplier= XMLDecoding(supplier);
			legend = supplier.Replace("&", " ");
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
				string total = dr[""+ nYear + nMonth +""].ToString();
				if(total == "" || total == null)
					total = "0";
				dTotalEachMonth = double.Parse(total);
				dTotalNoGST += dTotalEachMonth;
				Response.Write("<th align=center>"+ dTotalEachMonth.ToString("c") +"</th>");
				if(dTotalEachMonth > m_nMaxY)
					m_nMaxY = dTotalEachMonth;
				if(dTotalEachMonth < 0 && dTotalEachMonth < m_nMinY)
					m_nMinY = dTotalEachMonth;
				
					//xml chart data
				x = (nMonth*10).ToString();
				m_nMaxX = ii*10;
				y = dTotalEachMonth.ToString();
				legend = (m_EachMonth[nMonth-1] +"-"+ nYear.ToString()).Replace("&", " ");
				//if(ii == 1)
				{
				sb11.Append("<chartdata>\r\n");
				sb11.Append("<x");
				if(m_bHasLegends)
					sb11.Append(" legend='" + legend + "'");
				sb11.Append(">" + x + "</x>\r\n");
				sb11.Append("<y>" + y + "</y>\r\n");
				sb11.Append("</chartdata>\r\n");
				}
	
			}

		}
		Response.Write("</tr>");
		
	}
//	dTotalWithGST = dTotalTax + dTotalFreight + dTotalNoGST;
	if(!m_bCompair)
	{
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb1.ToString());
		m_sb.Append("</chartdataisland>\r\n");
		
		m_sb.Append("<chartdataisland>\r\n");
		m_sb.Append(sb2.ToString());
		m_sb.Append("</chartdataisland>\r\n");

		m_IslandTitle[1] = "--Total Amount";
		m_IslandTitle[0] = "--Total Orders";
		m_nIsland = 2;
		
		//Response.Write("<tr align=right><th colspan=1>SUB Total:</td><th align=center>"+ dTotalQTY.ToString() +"</td>");
		//Response.Write("<th>"+ dTotalWithGST.ToString("c") +"</td>");
		//Response.Write("</tr>");
		
	}
	else
	{
		//for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
		{
			int k = 0;
			m_sb.Append("<chartdataisland>\r\n");
			//if(ii == 1)
			{
				//k += ii - int.Parse(m_smFrom);
				m_IslandTitle[k] = "--Total Purchase";
				m_sb.Append(sb11.ToString());
			}
									
			m_sb.Append("</chartdataisland>\r\n");
		}
		
		//int j = 3 + int.Parse(m_smTo) - int.Parse(m_smFrom);
		//m_nIsland = j;
		m_nIsland = 1;
	}
	
	if(!m_bCompair)
	{
		Response.Write("<tr align=right><td colspan=5 align=left>&nbsp;</td></tr>");

		Response.Write("<tr align=right bgcolor=#EEEEE><th align=left>Sub Total:</td><th align=center>"+ dTotalQTY.ToString() +"</td><th><u>"+dTotalFreight.ToString("c") +"</u>");
		Response.Write("<th><u>"+dTotalTax.ToString("c") +"");
		Response.Write("<th><u>"+dTotalNoGST.ToString("c") +"</td>");
		Response.Write("<th><u>"+dTotalWithGST.ToString("c") +"</td>");
		Response.Write(" </tr>");
			dTotalNoGST = 0;
			dTotalQTY = 0; 
			dTotalTax = 0;
			dTotalFreight = 0; 
			dTotalWithGST = 0;
		for(int m=0; m<rows; m++)
		{
			DataRow dr = ds.Tables["report"].Rows[m];
			string total = dr["amount"].ToString();
			string freight = dr["freight"].ToString();
			string tax = dr["tax"].ToString();
			string orders = dr["orders"].ToString();
			string amount = total;
			
			dTotalNoGST += double.Parse(total);
			dTotalQTY += double.Parse(orders); 
			dTotalTax += double.Parse(tax);
			dTotalFreight += double.Parse(freight); 
			dTotalWithGST += MyDoubleParse(dr["total_amount"].ToString());
		}
		
		Response.Write("<tr align=right  bgcolor=#EEEAAA><th align=left>GRAND TOTAL:</td><th align=center>"+ dTotalQTY.ToString() +"</td><th><u>"+dTotalFreight.ToString("c") +"</u>");
		Response.Write("<th><u>"+dTotalTax.ToString("c") +"");
		Response.Write("<th><u>"+dTotalNoGST.ToString("c") +"</td>");
		Response.Write("<th><u>"+dTotalWithGST.ToString("c") +"</td>");
		Response.Write(" </tr>");
	}

	Response.Write("</table>");
	Response.Write("<br><center>");
//	if(!m_bCompair)
//		Response.Write(" <b>Total GST: </b><font color=red size=2>"+ dTotalQTY.ToString() +"</font>");
	
	if(m_bCompair)
		Response.Write("&nbsp;&nbsp;<b>Total AMOUNT:</b> <font color=Green size=2>"+ dTotalNoGST.ToString("c") +"</font>");
	Response.Write("</center>");
		//write xml data file for chart image
	WriteXMLFile();
}

</script>

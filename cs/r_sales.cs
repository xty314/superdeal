<script runat=server>
bool m_bCompair = false;

bool DoSalesPersonSummary(string status)
{
	string manager="";
	if (status == "Manager")
	{
//		manager = getSalesManager();
		manager = Request.Form["employee"];
		if(manager == null || manager == "" || manager == "all")
			manager = " = card.id ";
		else
			manager = " = " + manager;
//DEBUG("man",manager);
		m_tableTitle = "Sales Person Summary for (<font color=green size=4>Sales Manager</font>)";
	}
	else
		m_tableTitle = "Sales Person Summary for (<font color=green size=4>"+ m_sales_person.ToUpper() +"</font>)";
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
		//m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59 "+"' ";
		break;
	case 4:
		m_bCompair = true;
		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
		break;
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

		sc += " SELECT SUM(i.price) AS amount ";
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
				sc += " SELECT SUM(ii.price) ";
				sc += " FROM invoice ii JOIN orders oo ON oo.invoice_number = ii.invoice_number ";
				sc += " WHERE MONTH(ii.commit_date) = '" + nMonth + "' AND YEAR(ii.commit_date) = "+ nYear +"";
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
					sc += " AND oo.sales = "+ m_sales_id;
			}
			sc += " ), 0) AS '"+ nYear + nMonth +"' ";
			sc += ", ISNULL(( ";
			{
				sc += " SELECT SUM(ii.freight) ";
				sc += " FROM invoice ii JOIN orders oo ON oo.invoice_number = ii.invoice_number ";
				sc += " WHERE MONTH(ii.commit_date) = '" + nMonth + "' ";
				sc += " AND YEAR(ii.commit_date) = "+ nYear +"";
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
					sc += " AND oo.sales = "+ m_sales_id;
			}
			sc += " ), 0) AS freight"+ nYear + nMonth +" ";
						
		}
		sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
		if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "")
			sc += " AND o.sales = "+ m_sales_id;
		if(Session["branch_support"] != null && m_branchID != "0")
		{
			sc += " AND i.branch = " + m_branchID;
		}
	}
	else
	{
		sc += " SELECT id, name ";
		sc += ", ISNULL(( ";
		{
			sc += " SELECT COUNT(*) ";
			sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " WHERE 1=1 " + m_dateSql;
			if(status == "Manager")
				sc += " AND o.sales_manager " + manager;
			else if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
				sc += " AND o.sales = " + m_sales_id;
			else
				sc += " AND o.sales = card.id ";
//			if(Session["branch_support"] != null)
//			{
//				sc += " AND i.branch = " + m_branchID;
//			}
			
		}
		sc += " ), 0) AS orders ";
		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(i.price) ";
			sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " WHERE 1=1 " + m_dateSql;
			//sc += " AND i.card_id = card.id ";
			if (status == "Manager")
				sc += " AND o.sales_manager " + manager;
			else if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
				sc += " AND o.sales = card.id "; //AND o.sales = " + m_sales_id;
//			if(Session["branch_support"] != null)
//			{
//				sc += " AND i.branch = " + m_branchID;
//			}
			sc += " AND o.sales = card.id ";
		
		}
		sc += " ), 0) AS amount ";
		
		sc += ", ISNULL(( ";
		{
			sc += " SELECT SUM(i.freight) ";
			sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
			sc += " WHERE 1=1 " + m_dateSql;
			
			if (status == "Manager")
				sc += " AND o.sales_manager " + manager;
			else if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
				sc += " AND o.sales = card.id ";// AND o.sales = "+ m_sales_id;
//			if(Session["branch_support"] != null)
//			{
//				sc += " AND i.branch = " + m_branchID;
//			}
			sc += " AND o.sales = card.id ";
		
		}
		sc += " ), 0) AS freight ";
		sc += " FROM card ";
		sc += " WHERE 1=1 ";
		if (status == "Manager")
		{
			if(manager != " = card.id ")
				sc += " AND id " + manager;
			else
				sc += " AND (type = 4 OR type <= 0) ";
		}
		else if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
			sc += " AND id = "+ m_sales_id;
		else
			sc += " AND (type = 4 OR type <= 0) ";
		if(Session["branch_support"] != null && m_branchID != "all" && m_branchID != "0")
			sc += " AND our_branch = " + m_branchID;

		if(status != "Manager")
		{
			sc += " UNION ";
			sc += " SELECT     - 1 AS id, 'Online Orders' AS name ";
			sc += ", ISNULL(( ";
			{
				sc += " SELECT COUNT(*) ";
				sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
				sc += " WHERE o.sales IS NULL " + m_dateSql;
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
					sc += " AND o.sales = "+ m_sales_id;
				else if (status == "Manager")
					sc += " AND o.sales_manager " + manager;
//				if(Session["branch_support"] != null)
//				{
//					sc += " AND i.branch = " + m_branchID;
//				}
			}
			sc += " ), 0) AS orders ";
			sc += ", ISNULL(( ";
			{
				sc += " SELECT SUM(i.price) ";
				sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
				sc += " WHERE o.sales IS NULL " + m_dateSql;
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
					sc += " AND o.sales = "+ m_sales_id;
				else if (status == "Manager")
					sc += " AND o.sales_manager " + manager;
//				if(Session["branch_support"] != null)
//				{
//					sc += " AND i.branch = " + m_branchID;
//				}
			}
			sc += " ), 0) AS amount ";
			
			sc += ", ISNULL(( ";
			{ 
				sc += " SELECT SUM(i.freight) ";
				sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
				sc += " WHERE o.sales IS NULL " + m_dateSql;
				if(m_sales_id != "all" && m_sales_id != null && m_sales_id != "" && status == "")
					sc += " AND o.sales = "+ m_sales_id;
				else if (status == "Manager")
					sc += " AND o.sales_manager " + manager;
//				if(Session["branch_support"] != null)
//				{
//					sc += " AND i.branch = " + m_branchID;
//				}
			}
			sc += " ), 0) AS freight ";
		}
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
	
	if(m_bCompair)
		m_dra = ds.Tables["report"].Select("amount > 0 OR amount < 0 ", "");
	else
		m_dra = ds.Tables["report"].Select("amount > 0 OR amount < 0 ", "orders DESC");



	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			Response.Write("<h5>Branch: "+GetBranchName(m_branchID) +"</h5>");
		else
			Response.Write("<h5>Branch: ALL</h5><br>");
	}
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	PrintSalesPersonSummary(status);
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
	DrawChart();
	string uname = EncodeUserName();
	Response.Write("<img src=" + m_picFile + ">");
	}
	/*piecharts pc = new piecharts();
	Bitmap objBitmap = pc.GetPieChart(ds.Tables["report"].Rows, "amount", "name", "", 400);
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
void PrintSalesPersonSummary(string status)
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

	int rows = m_dra.Length;
//	if(ds.Tables["report"] != null)
//		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?branch="+m_branchID+"&r=" + DateTime.Now.ToOADate();
	if(m_type != null)
		m_cPI.URI += "&type="+ m_type;	
	if(Request.QueryString["salesmanager"] == "1")
		m_cPI.URI += "&salesmanager=1";
	m_cPI.URI += "&pr="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(m_bCompair)
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
	else
	{
		Response.Write("<th align=left>SALES NAME</th>");
		Response.Write("<th>TOTAL ORDERS</th>");
		Response.Write("<th>TOTAL FRIEGHT</th>");
		Response.Write("<th align=right>TOTAL AMOUNT</th>");
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
	StringBuilder sb3 = new StringBuilder();
	StringBuilder sb11 = new StringBuilder();
	for(; i<rows && i<end; i++)
	{
//		DataRow dr = ds.Tables["report"].Rows[i];
		DataRow dr = m_dra[i];
		int nOrders = 0;
		if(!m_bCompair)
		{
			nOrders = MyIntParse(dr["orders"].ToString());
			if(nOrders <= 0)
				continue;
		}
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		if(!m_bCompair)
		{
			string id = dr["id"].ToString();
			string freight = dr["freight"].ToString();
			string name = dr["name"].ToString();
			string orders = dr["orders"].ToString();
//DEBUG("orders",orders);
			string amount = dr["amount"].ToString();
			//string refunded = dr["refunded"].ToString();
			//amount = double.Parse(amount);
			//DEBUG("refund = ", refunded);
			dTotalFreight += double.Parse(freight);
			dTotalNoGST += double.Parse(amount);
			dTotalQTY += double.Parse(orders);
			
			Response.Write("<td>&nbsp;&nbsp;<a title='view sales invoice' href='report.aspx?branch="+ m_branchID +"&sid="+ id +"&pr="+ m_nPeriod +"");
			if(m_sdFrom != "" && m_sdFrom != null)
				Response.Write("&frm="+ m_sdFrom +"&to="+ m_sdTo +"");
			Response.Write("' class=o>" + name + "</a></td>");
			Response.Write("<td align=center><a title='view sales invoice' href='report.aspx?branch="+ m_branchID +"&sid="+ id +"&pr="+ m_nPeriod +"");
			if(m_sdFrom != "" && m_sdFrom != null)
				Response.Write("&frm="+ m_sdFrom +"&to="+ m_sdTo +"");
			if (status == "Manager")
				Response.Write("&salesmanager=1");
			Response.Write("' class=o>" + orders + "</a></td>");
			
			Response.Write("<td align=right>" + MyDoubleParse(freight).ToString("c") + "</td>");
			Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");
			if(double.Parse(amount) > m_nMaxY)
				m_nMaxY = double.Parse(amount);

			if(double.Parse(amount) < 0 && double.Parse(amount) < m_nMinY)
				m_nMinY = double.Parse(amount);

		//xml chart data
			x = (i*10).ToString();
			m_nMaxX = i*10;
			y = orders;
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
		}
		else
		{
			double dTotalEachMonth = 0;
			double dRefundEachMonth = 0;
			double dFreightEachMonth = 0;
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
					nYear = (int.Parse(m_syFrom) + nn);
					nMonth = nPlus;
				}	
				
				dTotalEachMonth = double.Parse(dr[""+ nYear + nMonth  +""].ToString());
				dFreightEachMonth = double.Parse(dr["freight"+ nYear + nMonth  +""].ToString());
				
				Response.Write("<td align=center>"+ dTotalEachMonth.ToString("c") +"</th>");
				if(dTotalEachMonth > m_nMaxY)
					m_nMaxY = dTotalEachMonth;

				if(dTotalEachMonth < 0 && dTotalEachMonth < m_nMinY)
					m_nMinY = dTotalEachMonth;
				dTotalNoGST += dTotalEachMonth;
				dTotalFreight += dFreightEachMonth;
				//xml chart data
				x = (i*10).ToString();
				m_nMaxX = i*10;
				y = dTotalEachMonth.ToString();

				m_EachMonth[nMonth-1] = XMLDecoding(m_EachMonth[nMonth-1]);
				legend = m_EachMonth[nMonth-1].Replace("&", " ");
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
				/*if(ii == 2)
				{
					sb12.Append("<chartdata>\r\n");
					sb12.Append("<x");
					if(m_bHasLegends)
						sb12.Append(" legend='" + legend + "'");
					sb12.Append(">" + x + "</x>\r\n");
					sb12.Append("<y>" + y + "</y>\r\n");
					sb12.Append("</chartdata>\r\n");
				}
				if(ii == 3)
				{
					sb13.Append("<chartdata>\r\n");
					sb13.Append("<x");
					if(m_bHasLegends)
						sb13.Append(" legend='" + legend + "'");
					sb13.Append(">" + x + "</x>\r\n");
					sb13.Append("<y>" + y + "</y>\r\n");
					sb13.Append("</chartdata>\r\n");
				}
				if(ii == 4)
				{
					sb14.Append("<chartdata>\r\n");
					sb14.Append("<x");
					if(m_bHasLegends)
						sb14.Append(" legend='" + legend + "'");
					sb14.Append(">" + x + "</x>\r\n");
					sb14.Append("<y>" + y + "</y>\r\n");
					sb14.Append("</chartdata>\r\n");
				}
				if(ii == 5)
				{
					sb15.Append("<chartdata>\r\n");
					sb15.Append("<x");
					if(m_bHasLegends)
						sb15.Append(" legend='" + legend + "'");
					sb15.Append(">" + x + "</x>\r\n");
					sb15.Append("<y>" + y + "</y>\r\n");
					sb15.Append("</chartdata>\r\n");
				}
				if(ii == 6)
				{
					sb16.Append("<chartdata>\r\n");
					sb16.Append("<x");
					if(m_bHasLegends)
						sb16.Append(" legend='" + legend + "'");
					sb16.Append(">" + x + "</x>\r\n");
					sb16.Append("<y>" + y + "</y>\r\n");
					sb16.Append("</chartdata>\r\n");
				}
				if(ii == 7)
				{
					sb17.Append("<chartdata>\r\n");
					sb17.Append("<x");
					if(m_bHasLegends)
						sb17.Append(" legend='" + legend + "'");
					sb17.Append(">" + x + "</x>\r\n");
					sb17.Append("<y>" + y + "</y>\r\n");
					sb17.Append("</chartdata>\r\n");
				}
				if(ii == 8)
				{
					sb18.Append("<chartdata>\r\n");
					sb18.Append("<x");
					if(m_bHasLegends)
						sb18.Append(" legend='" + legend + "'");
					sb18.Append(">" + x + "</x>\r\n");
					sb18.Append("<y>" + y + "</y>\r\n");
					sb18.Append("</chartdata>\r\n");
				}
				if(ii == 9)
				{
					sb19.Append("<chartdata>\r\n");
					sb19.Append("<x");
					if(m_bHasLegends)
						sb19.Append(" legend='" + legend + "'");
					sb19.Append(">" + x + "</x>\r\n");
					sb19.Append("<y>" + y + "</y>\r\n");
					sb19.Append("</chartdata>\r\n");
				}
				if(ii == 110)
				{
					sb110.Append("<chartdata>\r\n");
					sb110.Append("<x");
					if(m_bHasLegends)
						sb110.Append(" legend='" + legend + "'");
					sb110.Append(">" + x + "</x>\r\n");
					sb110.Append("<y>" + y + "</y>\r\n");
					sb110.Append("</chartdata>\r\n");
				}
				if(ii == 111)
				{
					sb111.Append("<chartdata>\r\n");
					sb111.Append("<x");
					if(m_bHasLegends)
						sb111.Append(" legend='" + legend + "'");
					sb111.Append(">" + x + "</x>\r\n");
					sb111.Append("<y>" + y + "</y>\r\n");
					sb111.Append("</chartdata>\r\n");
				}
				if(ii == 112)
				{
					sb112.Append("<chartdata>\r\n");
					sb112.Append("<x");
					if(m_bHasLegends)
						sb112.Append(" legend='" + legend + "'");
					sb112.Append(">" + x + "</x>\r\n");
					sb112.Append("<y>" + y + "</y>\r\n");
					sb112.Append("</chartdata>\r\n");
				}*/
			}
		}
		Response.Write("</tr>");
		
	}

	if(m_bCompair)
	{
		//for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
		//{
			m_sb.Append("<chartdataisland>\r\n");
		//int j = 1;		
		//if(ii == 1)
			m_sb.Append(sb11.ToString());
		/*(if(ii == 2)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--FEB";
			m_sb.Append(sb12.ToString());
		}
		if(ii == 3)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--MAR";
			m_sb.Append(sb13.ToString());
		}
		if(ii == 4)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--APR";
			m_sb.Append(sb14.ToString());
		}
		if(ii == 5)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--MAY";
			m_sb.Append(sb15.ToString());
		}
		if(ii == 6)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--JUN";
			m_sb.Append(sb16.ToString());
		}
		if(ii == 7)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--JUL";
			m_sb.Append(sb17.ToString());
		}
		if(ii == 8)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--AUG";
			m_sb.Append(sb18.ToString());
		}
		if(ii == 9)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--SEP";
			m_sb.Append(sb19.ToString());
		}
		if(ii == 10)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--OCT";
			m_sb.Append(sb110.ToString());
		}
		if(ii == 11)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--NOV";
			m_sb.Append(sb111.ToString());
		}
		if(ii == 12)
		{
			j += ii - int.Parse(m_smFrom);
			m_IslandTitle[j] = "--DEC";
			m_sb.Append(sb112.ToString());
		}*/

			m_sb.Append("</chartdataisland>\r\n");
		//}
	
		//int k = 3 + int.Parse(m_smTo) - int.Parse(m_smFrom);
		m_IslandTitle[0] = "--Amount";
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
		Response.Write("<tr align=right><td colspan=3 align=left>&nbsp;</td></tr>");

		Response.Write("<tr align=right bgcolor=#EEEEE><th align=left>SUb Total:</td><th align=center>"+ dTotalQTY +"</td><th><u>"+dTotalFreight.ToString("c") +"</u>");
		Response.Write("<th><u>"+dTotalNoGST.ToString("c") +"</td>");

		dTotalNoGST = 0;
		dTotalTax = 0;
		dTotalFreight = 0;
		dTotalQTY = 0;
		for(i=0; i<rows; i++)
		{
			DataRow dr = m_dra[i];
			string orders = dr["orders"].ToString();
			string amount = dr["amount"].ToString();
			string freight = dr["freight"].ToString();
			dTotalQTY += int.Parse(orders);
			dTotalNoGST += MyDoubleParse(amount);
			dTotalFreight += MyDoubleParse(freight);

		}

		Response.Write("<tr align=right bgcolor=#EEEAAA><th align=left>GRAND TOTAL:</td><th align=center>"+ dTotalQTY +"</td><th><u>"+dTotalFreight.ToString("c") +"</u>");
		Response.Write("<th><u>"+dTotalNoGST.ToString("c") +"</td>");
	
	}
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("<br><center>");
	Response.Write("</center>");
		//write xml data file for chart image
	WriteXMLFile();
}

</script>
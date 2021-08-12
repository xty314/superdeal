<script runat=server>

string m_title_name = "amount";

bool DoCustomerSummary()
{
	m_bCompair = false;
	m_tableTitle = "Customer Purchase Summary for ";
	if(Request.QueryString["tl"] != null && Request.QueryString["tl"] != "")
		m_title_name = Request.QueryString["tl"];

	if(Session["customer_id"] == "" || Session["customer_id"] == null)
		m_tableTitle += "(ALL)";
	else
		m_tableTitle += "("+ Session["customer_name"] + ")";
	
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
		m_bCompair = true;
		//m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT c.id, c.trading_name, c.name, c.company";
	
/*	sc += ", ISNULL(( ";
	{
		sc += " SELECT SUM(i.price) ";
		sc += " FROM invoice i ";
		sc += " WHERE i.card_id = card.id AND type = 6 " + m_dateSql;
	}
	sc += " ), 0) AS refunded ";
*/
	sc += ", ISNULL(COUNT(i.invoice_number), 0) AS orders ";
	sc += ", ISNULL(SUM(i.price), 0) AS amount ";
	sc += ", ISNULL(SUM(i.tax), 0) AS t_tax ";
	sc += ", ISNULL(SUM(i.freight), 0) AS t_freight ";
	if(m_bCompair)
	{
		for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
		{
			/* sc += ", ISNULL(( ";
			{
				sc += " SELECT SUM(i.price) ";
				sc += " FROM invoice i ";
				sc += " WHERE i.card_id = card.id  AND MONTH(i.commit_date) = '" + ii + "' ";
			}
			sc += " ), 0) AS '"+ ii +"' ";
			*/
			sc += ", ISNULL(SUM(i.price), 0) AS '"+ ii +"' ";

		}
	}
	sc += " FROM invoice i JOIN card c ON i.card_id = c.id ";

	sc += " WHERE 1=1 " + m_dateSql;
	if(Session["customer_id"] != null && Session["customer_id"] != "" && Session["customer_id"] != "all")
		sc += " AND c.id = " + Session["customer_id"];
	//if(Session["customer_id"] == "all")
	//	sc += " ";
	if(Session["branch_support"] != null && m_branchID != "0")
	{
	//	sc += " AND c.our_branch = " + m_branchID;
		sc += " AND i.branch = "+ m_branchID;
	}
	sc += " GROUP BY  c.id, c.trading_name, c.name, c.company ";
	
	sc += " ORDER BY amount ";	
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

	//m_dra = ds.Tables["report"].Select("amount > 0", ""+ m_title_name +" "+ m_sorted +"");
	m_dra = ds.Tables["report"].Select("", ""+ m_title_name +" "+ m_sorted +"");

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			Response.Write("<h5>Branch: "+GetBranchName(m_branchID) +"</h5>");
		else
			Response.Write("<h5>Branch: ALL</h5><br>");
	}
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	PrintCustomerSummary();
	if(m_bShowPic && m_bGBSetShowPicOnReport)
	{
		DrawChart();
		string uname = EncodeUserName();
		Response.Write("<img src=" + m_picFile + ">");
	}

	return true;
}

/////////////////////////////////////////////////////////////////
void PrintCustomerSummary()
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
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	if(m_type != null)
		m_cPI.URI += "&type="+ m_type;	
	m_cPI.URI += "&pr="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:#FFFFFF;background-color:#666696;\">");
	
	Response.Write("<th align=left>");
	Response.Write(" <a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=company");
	Response.Write("' class=o>");
	Response.Write("<font color=white>CUSTOMER");
	
	//<img border=0  src='/i/");
	//if(m_sorted == "DESC")
	//	Response.Write("dw");
	//else
	//	Response.Write("up");
	//Response.Write(".gif'></a>");
	Response.Write("</a>");
	Response.Write("</th>");
	Response.Write("<th>");
	Response.Write(" <a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=orders");
	Response.Write("' class=o>");
	Response.Write("<font color=white>TOTAL SALES");
	//Response.Write(" <a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=orders");
	//Response.Write("'><img border=0  src='/i/");
	//if(m_sorted == "DESC")
	//	Response.Write("dw");
	//else
	//	Response.Write("up");
	//Response.Write(".gif'></a>");
	Response.Write("</a>");
	Response.Write("</th>");
	
	Response.Write("<th align=right>");
	Response.Write("<a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=t_freight");
	Response.Write("'  class=o>");
	Response.Write("<font color=white>TOTAL FREIGHT");
	Response.Write("</a>");
	Response.Write("</th>");

	Response.Write("<th align=right>");
	Response.Write("<a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=t_tax");
	Response.Write("'  class=o>");
	Response.Write("<font color=white>TOTAL GST");
	Response.Write("</a>");
	Response.Write("</th>");

	Response.Write("<th align=right>");
	Response.Write("<a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=amount");
	Response.Write("'  class=o>");
	Response.Write("<font color=white>TOTAL AMOUNT");
	Response.Write("</a>");
	//Response.Write("'><img border=0  src='/i/up.gif'></a>");

	Response.Write("</th>");

	Response.Write("<th align=right>");
	Response.Write("<a title='Click to Sorted by "+ m_sorted +"' href='"+ m_cPI.URI +"&st="+ m_sorted +"&tl=amount");
	Response.Write("'  class=o>");
	Response.Write("<font color=white>TOTAL AMOUNT(inc. GST)");
	Response.Write("</a>");
	//Response.Write("'><img border=0  src='/i/up.gif'></a>");

	Response.Write("</th>");
	if(m_bCompair)
	{
		for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
		{
			string s_month = "";
			s_month = m_EachMonth[ii-1];
			Response.Write("<th>Total AMT in "+ s_month +"</th>");
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
	StringBuilder sb3 = new StringBuilder();

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	double dTotalFreight = 0;
	int dTotalSales = 0;
	bool bAlterColor = false;
	for(; i<rows && i<end; i++)
	{
//		DataRow dr = ds.Tables["report"].Rows[i];
		DataRow dr = m_dra[i];
		string id = dr["id"].ToString();
	
		string name = dr["trading_name"].ToString();
		if(name == "")
			name = dr["name"].ToString();
		if(name == "")
			name = dr["company"].ToString();
		string orders = dr["orders"].ToString();
		string amount = dr["amount"].ToString();
		string freight = dr["t_freight"].ToString();
		string tax = dr["t_tax"].ToString();
	//	string refunded = dr["refunded"].ToString();
	//	refunded = "0";
		//amount = (double.Parse(amount) + (double.Parse(refunded))).ToString();
		dTotalTax += MyDoubleParse(tax);
		dTotalNoGST += MyDoubleParse(amount);
		dTotalFreight += MyDoubleParse(freight);
		dTotalWithGST += MyDoubleParse(tax) + MyDoubleParse(freight) + MyDoubleParse(amount);
		dTotalSales += int.Parse(orders);
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>&nbsp;&nbsp;<a title='view customer Detials' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + id + "', '', 'width=350, height=350, resizable=1'); viewcard_window.focus()\" class=o>"+ name +"123s</a></td>");
		
		Response.Write("<td align=center><a title='view customer invoice' href='report.aspx?cid="+ id +"&pr="+ m_nPeriod +"");
		if(m_sdFrom != "" && m_sdFrom != null)
			Response.Write("&frm="+ m_sdFrom +"&to="+ m_sdTo +"");
		Response.Write("' class=o>" + orders + "</a></td>");
		Response.Write("<td align=right>" + MyDoubleParse(freight).ToString("c") + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(tax).ToString("c") + "</td>");
		
		Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");
		Response.Write("<td align=right>" + (MyDoubleParse(amount) + MyDoubleParse(tax) + MyDoubleParse(freight)).ToString("c") + "</td>");
		
		if(m_bCompair)
		{
			double dTotalEachMonth = 0;
			double dRefundEachMonth = 0;
			for(int ii=int.Parse(m_smFrom); ii<=int.Parse(m_smTo); ii++)
			{
				dTotalEachMonth = double.Parse(dr[""+ii+""].ToString());
				dRefundEachMonth = double.Parse(dr["refunded"+ii+""].ToString());
			//	dTotalEachMonth -= dRefundEachMonth;
				//DEBUG("refund = ", dRefundEachMonth.ToString());
				Response.Write("<th align=right>"+ dTotalEachMonth.ToString("c") +"</th>");
			}
		}
		Response.Write("</tr>");
		if(double.Parse(amount) > m_nMaxY)
		m_nMaxY = double.Parse(amount);

		if(double.Parse(amount) < 0 && double.Parse(amount) < m_nMinY)
			m_nMinY = double.Parse(amount);
	//xml chart data
		x = (i*10).ToString();
		//m_nMaxX = i*10;
		y = orders;
		name = name.Replace("'", "");
		name = StripHTMLtags(EncodeQuote(name));
//		name = XMLDecoding(name);
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
//dTotalWithGST += dTotalTax + dTotalNoGST + dTotalFreight;
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[1] = "--Total Amount";
	m_IslandTitle[0] = "--Total Orders";
	m_nIsland = 2;

	if(!m_bCompair)
	{
	Response.Write("<tr align=right><td colspan=5 align=left>&nbsp;</td></tr>");

	Response.Write("<tr align=right bgcolor=#EEEEE><th align=left>SUb Total:</td><th align=center>"+ dTotalSales +"</td><th><u>"+dTotalFreight.ToString("c") +"</u>");
	Response.Write("<th><u>"+dTotalTax.ToString("c") +"");
	Response.Write("<th><u>"+dTotalNoGST.ToString("c") +"</td>");
	Response.Write("<th><u>"+ dTotalWithGST.ToString("c") +"</td> </tr>");
	dTotalWithGST = 0;
	dTotalNoGST = 0;
	dTotalTax = 0;
	dTotalFreight = 0;
	dTotalSales = 0;
	for(i=0; i<rows; i++)
	{
		//DataRow dr = ds.Tables["report"].Rows[i];
		DataRow dr = m_dra[i];
		string orders = dr["orders"].ToString();
		string amount = dr["amount"].ToString();
		string freight = dr["t_freight"].ToString();
		string tax = dr["t_tax"].ToString();
		dTotalSales += int.Parse(orders);
		dTotalTax += MyDoubleParse(tax);
		dTotalNoGST += MyDoubleParse(amount);
		dTotalFreight += MyDoubleParse(freight);
		dTotalWithGST += MyDoubleParse(tax) + MyDoubleParse(freight) + MyDoubleParse(amount);
	}

	Response.Write("<tr align=right bgcolor=#EEEAAA><th align=left>GRAND TOTAL:</td><th align=center>"+ dTotalSales +"</td><th><u>"+dTotalFreight.ToString("c") +"</u>");
	Response.Write("<th><u>"+dTotalTax.ToString("c") +"");
	Response.Write("<th><u>"+dTotalNoGST.ToString("c") +"</td>");
	Response.Write("<th><u>"+ dTotalWithGST.ToString("c") +"</td> </tr>");
	}
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table><br>");

		//write xml data file for chart image
	WriteXMLFile();
}

</script>

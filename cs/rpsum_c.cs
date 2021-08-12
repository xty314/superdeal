<!-- #include file="page_index.cs" -->

<script runat=server>

string m_SortBy = "asc";
string m_SortName = "s.code ";
string m_type = "1";
string m_sdFrom = "";
string m_sdTo = "";
string m_syFrom = DateTime.Now.ToString("yyyy");
string m_syTo = DateTime.Now.ToString("yyyy");
string m_sPickMonthFrom = "";
string m_sPickMonthTo = "";
string m_sPickYearFrom = "";
string m_sPickYearTo = "";

int m_nMonthDiffer = 6;
int m_nPeriod = 0;

string m_branchID = "1";
string m_dateSql = "";
string m_card_id = "";
string m_datePeriod = "";
string[] m_EachMonth = new string[13];
DataSet ds = new DataSet();
string m_titleName = "";
bool m_bCustomer = false;
bool m_bShowCustomerDetails = false;
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

		//monthly name
	m_EachMonth[0] = "JAN";
	m_EachMonth[1] = "FEB";
	m_EachMonth[2] = "MAR";
	m_EachMonth[3] = "APR";
	m_EachMonth[4] = "MAY";
	m_EachMonth[5] = "JUN";
	m_EachMonth[6] = "JUL";
	m_EachMonth[7] = "AUG";
	m_EachMonth[8] = "SEP";
	m_EachMonth[9] = "OCT";
	m_EachMonth[10] = "NOV";
	m_EachMonth[11] = "DEC";
	//----
	if(Request.QueryString["custid"] != null )
	{
		m_card_id = Request.QueryString["custid"];
		m_bCustomer = true;		
	}
	else if(Request.QueryString["salesid"] != null )
	{
		m_card_id = Request.QueryString["salesid"];
		m_bCustomer = false;
		if(Request.QueryString["custgrp"] != null && Request.QueryString["custgrp"] != "" && Request.QueryString["custgrp"].ToString() == "1")
			m_bShowCustomerDetails = true;
	}
//DEBUG("s =", m_card_id);
	if(m_card_id == null || m_card_id == "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		return;
	}

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
	if(Request.QueryString["sortby"] != null)
		m_SortBy = Request.QueryString["sortby"];
	
	if(Request.QueryString["sortname"] != null)
		m_SortName = Request.QueryString["sortname"];
	if(Request.QueryString["type"] != "" && Request.QueryString["type"] != null)
	{
		m_type = Request.QueryString["type"];
		if(Request.QueryString["np"] != null)
			m_nPeriod = int.Parse(Request.QueryString["np"].ToString());
	}
	if(Request.QueryString["period"] != "" && Request.QueryString["period"] != null)
		m_nPeriod = int.Parse(Request.QueryString["period"].ToString());
	if(Request.QueryString["frm"] != "" && Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"];
		m_sdTo = Request.QueryString["to"];
	}
	string sYear = (DateTime.Now.Year).ToString();
	if(Request.QueryString["monthdiffer"] != null && Request.QueryString["monthdiffer"] != "")
	{
		m_nMonthDiffer = int.Parse(Request.QueryString["monthdiffer"].ToString());
		m_sPickMonthTo = Request.QueryString["mto"];
		m_sPickYearTo = Request.QueryString["yto"];
		m_sPickMonthFrom = Request.QueryString["mfrm"];
		m_sPickYearFrom = Request.QueryString["yfrm"];
	}
	 m_nMonthDiffer = ((int.Parse(sYear) - int.Parse(m_sPickYearTo)) * 12) ;
    if(m_nMonthDiffer > 0)
          m_nMonthDiffer = (m_nMonthDiffer - int.Parse(m_sPickMonthTo) ) + int.Parse((DateTime.Now.Month).ToString()) + 6;        
    else
        m_nMonthDiffer = 6;
//	DEBUG("m_nPeriod =", m_nPeriod);
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

	default:
		break;
	}
	PrintAdminHeader();
	PrintAdminMenu();
	if(m_bShowCustomerDetails)
		DoShowCustomerDetails();
	else
		DoItemDetails();

}
bool DoShowCustomerDetails()
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
//	case 4:
//		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
//		break;
	default:
		break;
	}

	ds.Clear();

	int rows = 0;
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.card_id, c.trading_name, c.name, c.company  "; //, i.invoice_number, i.commit_date, i.paid ";
	//sc += ", s.code ";
		
    int nCount = 6;
    while(nCount > 0)
	{
        sc += ", ISNULL((SELECT ";
        sc += " SUM(ss.commit_price * ss.quantity) "; 
        //sc += " SUM(ss.quantity)  ";        
        sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number ";        
		sc += " WHERE DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (m_nMonthDiffer - nCount).ToString() +"  ";
		sc += " AND ii.card_id = i.card_id  ";	
		sc += " ),'0') AS '"+ (m_nMonthDiffer - nCount).ToString() +"' ";
        nCount--;
	}
	sc += " FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " JOIN card c ON c.id = ";
	sc += " i.card_id ";		
	sc += " WHERE 1=1 ";
	sc += m_dateSql;
	if(m_card_id != "")
	{
		if(IsInteger(m_card_id))
		{			
			sc += " AND o.sales_manager = " + m_card_id +" AND o.sales_manager IS NOT NULL ";
		}
	}
	if(Session["branch_support"] != null && m_branchID != "0")
	{
		sc += " AND i.branch = " + m_branchID;
	}
//	sc += "GROUP BY c.trading_name, c.name, c.company, s.quantity, s.name, s.supplier_code, s.code, s.commit_price, s.supplier_price, i.card_id ";
	sc += "GROUP BY c.trading_name, c.name, c.company, i.card_id ";
	sc += " ORDER BY "+ m_SortName +" "+ m_SortBy +"";
//	sc += " ORDER BY i.commit_date DESC ";

//DEBUG("sc custeomr group = ",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "report");
	/*	if(rows > 0)
			m_card_id = ds.Tables["report"].Rows[0]["code"].ToString();
		else
	*/	if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>No Data Found for Card ID : <font color=red>" + m_card_id + "</font></h3>");
			return false;
		}

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	PrintCustomerDetails();

	return true;
}

/////////////////////////////////////////////////////////////////
void PrintCustomerDetails()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 4000;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
//	if(m_type != null)
//		m_cPI.URI += "&type="+ m_type;	
	m_cPI.URI += "&period="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;
	m_cPI.URI += "&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom +"&code="+ m_card_id;
	if(m_SortBy.ToLower() == "desc")
		m_cPI.URI += "&sortby=asc";
	else
		m_cPI.URI += "&sortby=desc";
	m_cPI.URI += "&monthdiffer="+ m_nMonthDiffer;
	m_cPI.URI += "&branch="+ m_branchID;
	if(m_bShowCustomerDetails)
		m_cPI.URI += "&custgrp=1";
	if(m_bCustomer)
		m_cPI.URI += "&custid="+ m_card_id;
	else
		m_cPI.URI += "&salesid="+ m_card_id;

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	DataRow drp = GetCardData(m_card_id);
		
	if(drp == null)
	{
		Response.Write("<br><h3>Error, product not found");
		return;
	}

//	Response.Write("<br><center><h3>Sales Item Detail Report</h3>");
	Response.Write("<br><center><h3>Sales Manager Item Detail Report</h3>");

	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			Response.Write("<h5>Branch: "+GetBranchName(m_branchID) +"</h5>");
		else
			Response.Write("<h5>Branch: ALL</h5><br>");
	}
	Response.Write("<b>Date Period : " + m_datePeriod + "</b></center>");

	//Response.Write("<font size=+1 color=GREEN><b><u>#" + m_card_id + " " + drp["supplier_code"].ToString() + " &nbsp&nbsp; " + drp["name"].ToString() + "</b></font>");
	Response.Write("<font size=+1 color=GREEN><b><u>Sales Manager: #" + m_card_id + " " + drp["name"].ToString() + " &nbsp&nbsp; </b></font>");
	
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr align=right style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<th align=left ><a title='sort by code' href='"+ m_cPI.URI +"&sortname=s.code'><font color=white>CODE#</a></th>");
	//Response.Write("<th align=left ><a title='sort by supplier code' href='"+ m_cPI.URI +"&sortname=s.supplier_code'><font color=white>SUPPLIER CODE</a></th>");
	Response.Write("<th align=left ><font color=white>NAME</a></th>");
	Response.Write("<th align=left ><a title='sort by name' href='"+ m_cPI.URI +"&sortname=c.trading_name'><font color=white>TRADING NAME</a></th>");
	
//	Response.Write("<th align=left >INVOICE#</th>");
//	Response.Write("<th align=left >DATE</th>");	
//	Response.Write("<th align=right><a title='sort by sales price' href='"+ m_cPI.URI +"&sortname=s.commit_price'><font color=white>SALES PRICE</a></th>");
//	Response.Write("<th align=right>AVG COST</th>");
	string sMonth = (DateTime.Now.Month).ToString();
	string sYear = (DateTime.Now.Year).ToString();
	if(m_nPeriod == 3)
	{
		sMonth = m_sPickMonthTo;
	}
	int nSwapMonth = int.Parse(sMonth);        
       nSwapMonth--;
	for(int j =5 ;j>=0; j--)
	{
         if(nSwapMonth < 0)
            nSwapMonth = 11;

		Response.Write("<th width='6%'align=center> "+ m_EachMonth[nSwapMonth] +"</th>");
        nSwapMonth--;
	}

	//Response.Write("<th align=center>TOTAL QTY</th>");
	
//	Response.Write("<th align=right>TOTAL PROFIT</th>");
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
	double [] dEachSubTotalPrice = new double [7];
	for(; i < rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
	//	string code = dr["code"].ToString();
	//	string supplier_code = dr["supplier_code"].ToString();
	///	string item_name = dr["item_name"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
	//	Response.Write("<td>" + code + "</td>");
	//	Response.Write("<td>" + supplier_code + "</td>");
	//	Response.Write("<td>" + item_name + "</td>");

		Response.Write("<td> "+ dr["name"].ToString() + "</td>");
		Response.Write("<td> "+ dr["trading_name"].ToString() + "</td>");
		
	//	Response.Write("<td><a href=invoice.aspx?" + invoice + " class=o>" + invoice + "</a></td>");
	//	Response.Write("<td align=center>" + date + "</td>");
		
		//Response.Write("<td align=right>" + dPrice.ToString("c") + "</td>");
		//Response.Write("<td align=right>");
		//Response.Write("<a href=tp.aspx?n=sales_cost&inv=" + invoice + "&code=" + m_card_id + " class=o target=_blank>");
		//Response.Write(dCost.ToString("c") + "</a></td>");
		//Response.Write(dCost.ToString("c") + "</td>");
		int nCount = 6;
		int nEachTotalQTY = 0;
		while(nCount > 0 )
		{            
            Response.Write("<td width='6%'align=center>"+ double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString()).ToString("c") +"</td>");            
		
			dEachSubTotalPrice[nCount] += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nCount--;
		}
			
		Response.Write("</tr>");	

	}
	
	Response.Write("<tr><td colspan=8>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=2 ><b>SUB TOTAL : &nbsp; </b></td>");
	//Response.Write("<td align=right >" + dTotalAmount.ToString("c") + "</td>");
	//Response.Write("<td align=right >" + dTotalCost.ToString("c") + "</td>");
	int nCounter = 6;
	while(nCounter > 0 )
	{
		Response.Write("<td align=center>"+ dEachSubTotalPrice[nCounter] +"</td>");
		dEachSubTotalPrice[nCounter] = 0;
		nCounter--;
	}

	Response.Write("</tr>");
	
	totalQty = 0;
	dTotalAmount = 0;
	dTotalProfit =0;
	dTotalCost = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
//		string quantity = "0"; // dr["quantity"].ToString();
//		string price = dr["commit_price"].ToString();
//		string cost = dr["supplier_price"].ToString();
//		double qty = MyDoubleParse(quantity);
///		double dPrice = MyDoubleParse(price);
//		double dCost = MyDoubleParse(cost);
//		double dProfit = (dPrice - dCost) * qty;
		int nCount = 6;
		int nEachTotalQTY = 0;
		while(nCount > 0 )
		{			
//			nEachTotalQTY += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			dEachSubTotalPrice[nCount] += double.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());			
			nCount--;
		}
	//	totalQty += nEachTotalQTY;
		//totalQty += qty;
	//	dTotalAmount += dPrice;
	//	dTotalProfit += dProfit;
	//	dTotalCost += dCost;

	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=2><b>GRAND TOTAL : &nbsp; </b></td>");
//	Response.Write("<td align=right>" + dTotalAmount.ToString("c") + "</td>");
//	Response.Write("<td align=right>" + dTotalCost.ToString("c") + "</td>");
	nCounter = 6;
	while(nCounter > 0 )
	{
		Response.Write("<td align=center>"+ dEachSubTotalPrice[nCounter].ToString("c") +"</td>");
		dEachSubTotalPrice[nCounter] = 0;
		nCounter--;
	}
//	Response.Write("<td align=middle>" + totalQty.ToString() + "</td>");
	
//	Response.Write("<td align=right>" + dTotalProfit.ToString("c") + "</td>");
	//Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
	Response.Write("<tr><td colspan=6><a title='close window' href=\"javascript:window.close();\" class=o>Close Report</a><td></tr>");
	Response.Write("</table>");

	Response.Write("<br><center><h4>");

	//write xml data file for chart image
	//WriteXMLFile();

}


////////////////////////////
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
//	case 4:
//		m_dateSql = " AND MONTH(i.commit_date) >= '" + m_smFrom + "' AND MONTH(i.commit_date) <= '" + m_smTo + "' ";
//		break;
	default:
		break;
	}

	ds.Clear();

	int rows = 0;
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.card_id, c.trading_name, c.name, c.company, s.name AS item_name, s.supplier_code "; //, i.invoice_number, i.commit_date, i.paid ";
	sc += ", s.code , s.commit_price, s.supplier_price ";
	
    int nCount = 6;
    while(nCount > 0)
	{
        sc += ", ISNULL((SELECT ";
        //sc += " SUM(ss.commit_price * ss.quantity) "; 
        sc += " SUM(ss.quantity)  ";        
        sc += " FROM sales ss JOIN invoice ii ON ii.invoice_number = ss.invoice_number ";        
		sc += " WHERE DATEDIFF(month, ii.commit_date, GETDATE()) = "+ (m_nMonthDiffer - nCount).ToString() +"  ";
		sc += " AND ii.card_id = i.card_id AND ss.code = s.code AND ss.commit_price = s.commit_price ";	
		sc += " ),'0') AS '"+ (m_nMonthDiffer - nCount).ToString() +"' ";
        nCount--;

	}
	sc += " FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " JOIN card c ON c.id = ";
	if(m_bCustomer)
		sc += "i.card_id ";
	else
		sc += " o.sales_manager ";
	sc += " WHERE 1=1 ";
	sc += m_dateSql;
	if(m_card_id != "")
	{
		if(IsInteger(m_card_id))
		{
			if(m_bCustomer)
				sc += " AND i.card_id = " + m_card_id;
			else
				sc += " AND o.sales_manager = " + m_card_id +" AND o.sales_manager IS NOT NULL ";
		}
	}
	if(Session["branch_support"] != null && m_branchID != "0")
	{
		sc += " AND i.branch = " + m_branchID;
	}
//	sc += "GROUP BY c.trading_name, c.name, c.company, s.quantity, s.name, s.supplier_code, s.code, s.commit_price, s.supplier_price, i.card_id ";
	sc += "GROUP BY c.trading_name, c.name, c.company, s.name, s.supplier_code, s.code, s.supplier_price, s.commit_price, i.card_id ";
	sc += " ORDER BY "+ m_SortName +" "+ m_SortBy +"";
//	sc += " ORDER BY i.commit_date DESC ";
//DEBUG("sc = ",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "report");
	/*	if(rows > 0)
			m_card_id = ds.Tables["report"].Rows[0]["code"].ToString();
		else
	*/	if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>No Data Found for Card ID : <font color=red>" + m_card_id + "</font></h3>");
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
	/*	if(m_bGBSetShowPicOnReport)
		{
			DrawChart();
			string uname = EncodeUserName();
			Response.Write("<img src=" + m_picFile + ">");
		}
		*/
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
	m_cPI.PageSize = 4000;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["report"] != null)
		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
//	if(m_type != null)
//		m_cPI.URI += "&type="+ m_type;	
	m_cPI.URI += "&period="+ m_nPeriod;
	if(m_sdFrom != "" && m_sdFrom != null)
		m_cPI.URI += "&frm="+ m_sdFrom +"&to="+ m_sdTo;
	m_cPI.URI += "&mto=" + m_sPickMonthTo +"&mfrm="+ m_sPickMonthFrom +"&yto=" + m_sPickYearTo +"&yfrm="+ m_sPickYearFrom +"&code="+ m_card_id;
	if(m_SortBy.ToLower() == "desc")
		m_cPI.URI += "&sortby=asc";
	else
		m_cPI.URI += "&sortby=desc";
	m_cPI.URI += "&monthdiffer="+ m_nMonthDiffer;
	m_cPI.URI += "&branch="+ m_branchID;
	if(m_bCustomer)
		m_cPI.URI += "&custid="+ m_card_id;
	else
		m_cPI.URI += "&salesid="+ m_card_id;

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	DataRow drp = GetCardData(m_card_id);
		
	if(drp == null)
	{
		Response.Write("<br><h3>Error, product not found");
		return;
	}

//	Response.Write("<br><center><h3>Sales Item Detail Report</h3>");
	if(m_bCustomer)
		Response.Write("<br><center><h3>Customer Item Detail Report</h3>");
	else
		Response.Write("<br><center><h3>Sales Manager Item Detail Report</h3>");

	if(Session["branch_support"] != null)
	{
		if(TSIsDigit(m_branchID) && m_branchID != "all")
			Response.Write("<h5>Branch: "+GetBranchName(m_branchID) +"</h5>");
		else
			Response.Write("<h5>Branch: ALL</h5><br>");
	}
	Response.Write("<b>Date Period : " + m_datePeriod + "</b></center>");

	//Response.Write("<font size=+1 color=GREEN><b><u>#" + m_card_id + " " + drp["supplier_code"].ToString() + " &nbsp&nbsp; " + drp["name"].ToString() + "</b></font>");
	if(m_bCustomer)
		Response.Write("<font size=+1 color=GREEN><b><u>Customer: #" + m_card_id + " " + drp["trading_name"].ToString() + " &nbsp&nbsp; " + drp["name"].ToString() + "</b></font>");
	else
		Response.Write("<font size=+1 color=GREEN><b><u>Sales Manager: #" + m_card_id + " " + drp["name"].ToString() + " &nbsp&nbsp; </b></font>");
	
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr align=right style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left ><a title='sort by code' href='"+ m_cPI.URI +"&sortname=s.code'><font color=white>CODE#</a></th>");
	Response.Write("<th align=left ><a title='sort by supplier code' href='"+ m_cPI.URI +"&sortname=s.supplier_code'><font color=white>SUPPLIER CODE</a></th>");
	Response.Write("<th align=left ><a title='sort by product name' href='"+ m_cPI.URI +"&sortname=s.name'><font color=white>NAME</a></th>");
if(m_bCustomer)
	Response.Write("<th align=left >SALES MANAGER</th>");	
else
		Response.Write("<th align=left >COMPANY</th>");	
//	Response.Write("<th align=left >INVOICE#</th>");
//	Response.Write("<th align=left >DATE</th>");	
	Response.Write("<th align=right><a title='sort by sales price' href='"+ m_cPI.URI +"&sortname=s.commit_price'><font color=white>SALES PRICE</a></th>");
	Response.Write("<th align=right>AVG COST</th>");
		string sMonth = (DateTime.Now.Month).ToString();
	string sYear = (DateTime.Now.Year).ToString();
	if(m_nPeriod == 3)
	{
		sMonth = m_sPickMonthTo;
	}
	int nSwapMonth = int.Parse(sMonth);        
       nSwapMonth--;
	for(int j =5 ;j>=0; j--)
	{
         if(nSwapMonth < 0)
            nSwapMonth = 11;

		Response.Write("<th width='6%'align=center> "+ m_EachMonth[nSwapMonth] +"</th>");
        nSwapMonth--;
	}

	Response.Write("<th align=center>TOTAL QTY</th>");
	
//	Response.Write("<th align=right>TOTAL PROFIT</th>");
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
	int [] nEachSubTotalQTY = new int [7];
	for(; i < rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string item_name = dr["item_name"].ToString();
	
//		string invoice = dr["invoice_number"].ToString();
//		string date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy");
		string quantity = "0";
//		string quantity = dr["quantity"].ToString();
		string price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();

		double qty = MyDoubleParse(quantity);
		double dPrice = MyDoubleParse(price);
		double dCost = MyDoubleParse(cost);
		double dProfit = (dPrice - dCost) * qty;
		
	//	totalQty += qty;
//		dTotalAmount += dPrice * qty;
		dTotalAmount += dPrice;
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
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + item_name + "</td>");
if(m_bCustomer)
Response.Write("<td> "+ dr["name"].ToString() + "</td>");
else
Response.Write("<td> "+ dr["trading_name"].ToString() + "</td>");

		
	//	Response.Write("<td><a href=invoice.aspx?" + invoice + " class=o>" + invoice + "</a></td>");
	//	Response.Write("<td align=center>" + date + "</td>");
		
		Response.Write("<td align=right>" + dPrice.ToString("c") + "</td>");
		Response.Write("<td align=right>");
		//Response.Write("<a href=tp.aspx?n=sales_cost&inv=" + invoice + "&code=" + m_card_id + " class=o target=_blank>");
		//Response.Write(dCost.ToString("c") + "</a></td>");
		Response.Write(dCost.ToString("c") + "</td>");
		int nCount = 6;
		int nEachTotalQTY = 0;
		while(nCount > 0 )
		{            
            Response.Write("<td width='6%'align=center>"+ dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString() +"</td>");            
			nEachTotalQTY += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nEachSubTotalQTY[nCount] += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nCount--;
		}
		quantity = nEachTotalQTY.ToString();
		totalQty += nEachTotalQTY;

		Response.Write("<td align=center>" + quantity + "</td>");
		

		//profit
//		Response.Write("<td align=right>" + dProfit.ToString("c") + "</td>");

		Response.Write("</tr>");
	

	}

	
	Response.Write("<tr><td colspan=8>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:black;background-color:lightblue;\" ");
	Response.Write(">");
	Response.Write("<td colspan=4 ><b>SUB TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right >" + dTotalAmount.ToString("c") + "</td>");
	Response.Write("<td align=right >" + dTotalCost.ToString("c") + "</td>");
	int nCounter = 6;
	while(nCounter > 0 )
	{
		Response.Write("<td align=center>"+ nEachSubTotalQTY[nCounter] +"</td>");
		nEachSubTotalQTY[nCounter] = 0;
		nCounter--;
	}
	Response.Write("<td align=middle >" + totalQty.ToString() + "</td>");
	
//	Response.Write("<td align=right >" + dTotalProfit.ToString("c") + "</td>");
//	Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	
	totalQty = 0;
	dTotalAmount = 0;
	dTotalProfit =0;
	dTotalCost = 0;
	for(i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string quantity = "0"; // dr["quantity"].ToString();
		string price = dr["commit_price"].ToString();
		string cost = dr["supplier_price"].ToString();
		double qty = MyDoubleParse(quantity);
		double dPrice = MyDoubleParse(price);
		double dCost = MyDoubleParse(cost);
		double dProfit = (dPrice - dCost) * qty;
		int nCount = 6;
		int nEachTotalQTY = 0;
		while(nCount > 0 )
		{			
			nEachTotalQTY += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());
			nEachSubTotalQTY[nCount] += int.Parse(dr[""+ (m_nMonthDiffer - nCount).ToString() +""].ToString());			
			nCount--;
		}
		totalQty += nEachTotalQTY;
		//totalQty += qty;
		dTotalAmount += dPrice;
		dTotalProfit += dProfit;
		dTotalCost += dCost;

	}
	Response.Write("<tr style=\"color:black;background-color:#EEE54E;\" ");
	Response.Write(">");
	Response.Write("<td colspan=4><b>GRAND TOTAL : &nbsp; </b></td>");
	Response.Write("<td align=right>" + dTotalAmount.ToString("c") + "</td>");
	Response.Write("<td align=right>" + dTotalCost.ToString("c") + "</td>");
	nCounter = 6;
	while(nCounter > 0 )
	{
		Response.Write("<td align=center>"+ nEachSubTotalQTY[nCounter] +"</td>");
		nEachSubTotalQTY[nCounter] = 0;
		nCounter--;
	}
	Response.Write("<td align=middle>" + totalQty.ToString() + "</td>");
	
//	Response.Write("<td align=right>" + dTotalProfit.ToString("c") + "</td>");
	//Response.Write("<td align=right nowrap>" + dMarginAve.ToString("p") + "</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
	Response.Write("<tr><td colspan=6><a title='close window' href=\"javascript:window.close();\" class=o>Close Report</a><td></tr>");
	Response.Write("</table>");

	Response.Write("<br><center><h4>");

	//write xml data file for chart image
	//WriteXMLFile();

}

string GetBranchName(string id)
{
	if(ds.Tables["branch_name"] != null)
		ds.Tables["branch_name"].Clear();

	string sc = " SELECT name FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "branch_name") == 1)
			return ds.Tables["branch_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}	

</script>

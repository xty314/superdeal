<!-- #include file ="page_index.cs" -->
<!-- #include file ="fifo_f.cs" -->

<script runat=server>
DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table

//
int m_page = 1;
int m_nPageSize = 20;
int m_nQtyReturn = 0;

double m_dTotal = 0;

string cat = "";
string s_cat = "";
string ss_cat = "";
string m_sCurrencyName="NZD";
string m_id = "";
string m_sBranchID = "0";
string tableWidth = "97%";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	m_sCurrencyName=GetSiteSettings("default_currency_name", "NZD");
	if(!SecurityCheck("sales"))
		return;

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_sBranchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_sBranchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_sBranchID = Session["branch_id"].ToString();
	}

	if(Request.QueryString["t"] == "detail")
	{
		ShowItemDetails();
		return;
	}

	if(Request.QueryString["page"] != null)
	{
		if(IsInteger(Request.QueryString["page"]))
			m_page = int.Parse(Request.QueryString["page"]);
	}

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];

	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	
	if(!GetStockQty())
		return;
	if(Request.QueryString["print"] == "report")
		PrintReport();	
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
		BindStockQty();	

		LFooter.Text = m_sAdminFooter;
	}
}

bool GetStockQty()
{
	string kw = "";
	if(Request.Form["kw"] != null)
	{
		kw = Request.Form["kw"];
		Session["stock_value_search"] = kw;
	}
	kw = EncodeQuote(kw);

	bool bWhereAdded = false;
	string sc = "SELECT DISTINCT p.code, p.supplier, p.supplier_code, p.name , p.supplier_price,p.average_cost ";//, p.supplier_price";
	//string sc = "SELECT DISTINCT p.code, p.supplier, p.supplier_code, p.name , sq.supplier_price, sq.average_cost ";
	sc += ", sq.qty AS stock, sq.allocated_stock, sq.branch_id ";
	if(Session["branch_support"] != null)
	{
		//if(m_sBranchID != "0")
		sc += ", b.name AS branch ";
	}
	else
		sc += ", '' AS branch ";
//	sc += " FROM product p  ";
	sc += " FROM code_relations p  ";
	sc += " JOIN stock_qty sq ON p.code=sq.code";
	sc += " JOIN branch b ON b.id = sq.branch_id AND activated = 1 ";
	sc += " WHERE 1=1 ";
	if(cat != "ServiceItem")
		sc += " AND p.cat <> 'ServiceItem' ";
if(Session["branch_support"] != null)
	{
	if(m_sBranchID != "0")
		sc += " AND sq.branch_id = " + m_sBranchID;
}
else
	sc += " AND sq.branch_id = 1 ";
	if(kw != "")
	{
		sc += " AND ";
		//DEBUG(" isInteger = ", IsInteger(kw).ToString());
		if(IsInteger(kw))
			sc += " p.code = " + kw;
		else
			sc += " (p.supplier_code LIKE N'%" + kw + "%' OR p.name LIKE N'%" + kw + "%' ) ";
	}
	else
	{
		if(cat != "" && cat != "all")
		{
			sc += " AND ";
			sc += " p.cat = N'"+ cat +"' ";
		}
		if(s_cat != "" && s_cat != "all")
		{
			sc += " AND ";
			sc += " p.s_cat = N'"+ s_cat +"' ";
		}
		if(ss_cat != "" && ss_cat != "all")
		{
			sc += " AND ";
			sc += " p.ss_cat = N'"+ ss_cat +"' ";
		}
	}
//	if(cat == "" && s_cat == "" && ss_cat == "")
//		sc += " AND 1=1 ";// do not show the whole stock, too slow

	if(Request.QueryString["cat"] == "all" || (Request.QueryString["cat"] != null && Request.QueryString["scat"] == "all" )
		|| (Request.QueryString["cat"] != null && Request.QueryString["cat"] != null && Request.QueryString["sscat"] == "all"))
	{
		sc += " ORDER BY p.name ";	
	}
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nQtyReturn = myAdapter.Fill(dst, "stock_qty");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;

}

void PrintReport()
{
	Response.Write("<form name=frmQtyAdjust method=post action=stock_value.aspx?update=success");
	if(cat != "")
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	if(s_cat != "")
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	if(ss_cat != "")
		Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	Response.Write(">");
	int rows = 0;
	if(dst.Tables["stock_qty"] != null)
		rows = dst.Tables["stock_qty"].Rows.Count;


	Response.Write("<br><center><h3>Stock Value Detail Report</h3>");
	Response.Write("<table width=95% cellspacing=1 cellpadding=0 border=0 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	//Response.Write("<tr><td colspan=6><table><tr><td>");
	//Response.Write("<b>Quick Search : </b><input type=text name=kw value='" + Session["stock_value_search"] + "'>");
	//Response.Write("</td><td valign=center><input type=submit name=cmd value=Search " + Session["button_style"] + ">");

	//Response.Write("</td><td colspan=4 align=right>");

	//Response.Write("</td><td><input type=button value='Print Report' "+ Session["button_style"] +" ");
	//Response.Write(" onclick=\"javascript:stock_window=window.open('stock_value.aspx?print=report', '','');\" >");
	//Response.Write("</td></tr></table>");
	//Response.Write("</td></tr>");
//	Response.Write("<tr><td colspan=6>"+ sPageIndex +"</td></tr>");
	Response.Write("<tr bgcolor=#8BB7DD>");
	if(m_sBranchID == "0")
		Response.Write("<th align=center>Branch</th>");
	Response.Write("<th align=center>Code</th>");
	Response.Write("<th align=center nowrap>M_PN / StockTrace</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th nowrap>Last_Cost"+"("+m_sCurrencyName+")"+"</th>");
	Response.Write("<th nowrap>Average_Cost"+"("+m_sCurrencyName+")"+"</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>Total LastCost</th>"); // Value</th>");
	Response.Write("<th>Total AverageCost</th>");
//	Response.Write("<th align=right>New Quantity</th> "); //<td align=center>Adjustment</td></tr>");
	Response.Write("</tr>");

	bool bAlt = true;
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	//int startPage = (m_page-1) * m_nPageSize;
//DEBUG("p=", startPage);
	m_dTotal = 0;
	double dAvgCost = 0;
	//for(int i=startPage; i<dst.Tables["stock_qty"].Rows.Count; i++)	
	for(int i=0; i<rows; i++)
	{
		
		bool bDisplay = true;
		//if(i-startPage >= m_nPageSize)
		//if(i-end >= m_nPageSize)
		//	bDisplay = false;
		dr = dst.Tables["stock_qty"].Rows[i];
		//alterColor = !alterColor;
		//if(!DrawRow(dr, i, alterColor, bDisplay))
		//	break;
		string branchname = dr["branch"].ToString();
		string s_qty = dr["stock"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		string average_cost = dr["average_cost"].ToString();
		double dSupplierPrice = MyDoubleParse(supplier_price);
		double dAverageCost = MyDoubleParse(average_cost);
		double dQty = MyDoubleParse(s_qty);

		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		
		string s_productdesc = dr["name"].ToString();

		double dValue = 0;
		
//		GetItemValue(code, m_sBranchID, dQty, ref dValue);
//		if(dValue == 0)
			dValue = dSupplierPrice * dQty;
		m_dTotal += dValue;
		dAvgCost += (dAverageCost * dQty);

		if(!bDisplay)
			return; //only calculate total value

		
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		alterColor = !alterColor;
		if(m_sBranchID == "0")
			Response.Write("<td>"+ branchname +"</td>");
		Response.Write("<td align=center><b>"+code+"</b></td>");
		Response.Write("<td align=center><b>" + supplier_code + "</b></td>");
		Response.Write("<td> "+s_productdesc+"</td>");
		
		Response.Write("<td> " + dSupplierPrice.ToString("c") + "</td>");

		Response.Write("<td> " + dAverageCost.ToString("c") + "</td>");

		Response.Write("<td nowrap>");
		Response.Write("<b><font color=");
		if(MyDoubleParse(s_qty) == 0)
			Response.Write("Green>");
		else if(MyDoubleParse(s_qty) < 0)
			Response.Write("Red>");
		else if(MyDoubleParse(s_qty) > 0)
			Response.Write("Black>");

		Response.Write(s_qty);
		Response.Write("</font></b></td>");
		Response.Write("<td align=right>" + dValue.ToString("c") + "</td>");
		Response.Write("<td align=right>" + (dAverageCost * dQty).ToString("c") + "</td>");		
		Response.Write("</tr>");
	
	}
int nCols = 6;
if(m_sBranchID == "0")
	nCols = 7;
	Response.Write("<tr><td colspan="+ nCols +" align=right><font color=red><b>Sub Total : </b></font></td>");
	Response.Write("<td align=right><b>" + m_dTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dAvgCost.ToString("c") + "</b></td></tr>");
	Response.Write("</table>");
	Response.Write("</form></center>");
}


void BindStockQty()
{
	Response.Write("<form name=frmQtyAdjust method=post action=stock_value.aspx?update=success");
	if(cat != "")
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	if(s_cat != "")
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	if(ss_cat != "")
		Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	Response.Write(">");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(dst.Tables["stock_qty"] != null)
		rows = dst.Tables["stock_qty"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 9000;
	m_cPI.URI = "?&cat=" + HttpUtility.UrlEncode(cat) +"&scat=" + HttpUtility.UrlEncode(s_cat) +"&sscat=" + HttpUtility.UrlEncode(ss_cat);
	
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
		
	m_nPageSize = m_cPI.PageSize;

	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>Stock Value Detail Report</font></font>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
	
	Response.Write("<table border=1 align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=11><br></td></tr>");

	/*
	Response.Write("<br><center><h3>Stock Value Detail Report</h3>");
	Response.Write("<table width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
*/
	Response.Write("<tr><td colspan=9><table border=0><tr><td>");
	Response.Write("<b>Quick Search : </b><input type=text name=kw value='" + Session["stock_value_search"] + "'>");
	Response.Write("</td><td valign=center><input type=submit name=cmd value=Search " + Session["button_style"] + ">");

	Response.Write("</td><td colspan=4 align=right nowrap>");
	if(!DoItemOption())
		return;
	Response.Write("</td><td nowrap>");

	if(Session["branch_support"] != null)
	{
		Response.Write(" <b>Branch : </b>");
		PrintBranchNameOptions(m_sBranchID, "", true);
		Response.Write("<input type=submit name=cmd value='View Report'  "+ Session["button_style"] +">");
	}

	Response.Write("<input type=button value='Print Report' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\"javascript:stock_window=window.open('stock_value.aspx?print=report', '','');\" >");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6>"+ sPageIndex +"</td></tr>");
	Response.Write("<tr bgcolor=#8BB7DD>");
	if(m_sBranchID == "0")
		Response.Write("<th align=left>Branch</th>");
	Response.Write("<th align=center>Code</th>");
	Response.Write("<th align=center nowrap>M_PN / StockTrace</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th nowrap>Last_Cost"+"("+m_sCurrencyName+")"+"</th>");
	Response.Write("<th nowrap>Average_Cost"+"("+m_sCurrencyName+")"+"</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>Total LastCost</th>"); //Value</th>");
	Response.Write("<th>Total AverageCost</th>"); //Value</th>");
//	Response.Write("<th align=right>New Quantity</th> "); //<td align=center>Adjustment</td></tr>");
	Response.Write("</tr>");

	bool bAlt = true;
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	//int startPage = (m_page-1) * m_nPageSize;
//DEBUG("p=", startPage);
	m_dTotal = 0;
	//for(int i=startPage; i<dst.Tables["stock_qty"].Rows.Count; i++)
	double dAvgCost = 0;
//	for(; i < rows && i < end; i++)
	for(i=0; i<dst.Tables["stock_qty"].Rows.Count; i++)
	{		
		bool bDisplay = true;
		//if(i-startPage >= m_nPageSize)
		if(i-end >= m_nPageSize)
			bDisplay = false;
		dr = dst.Tables["stock_qty"].Rows[i];
		//alterColor = !alterColor;
		//if(!DrawRow(dr, i, alterColor, bDisplay))
		//	break;

		string s_qty = dr["stock"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		string average_cost = dr["average_cost"].ToString();
		double dAverageCost = MyDoubleParse(average_cost);
		double dSupplierPrice = MyDoubleParse(supplier_price);

		double dQty = MyDoubleParse(s_qty);
//		double dValue = dSupplierPrice * nQty;
//		m_dTotal += dValue;
		string branchname = dr["branch"].ToString();
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		
		string s_productdesc = dr["name"].ToString();
		string swap = "";
		if(s_productdesc.Length >=60)
		{
			for(int j=0; j<60; j++)
				swap += s_productdesc[j].ToString();
		}
		else
			swap = s_productdesc;
		s_productdesc = swap;

		double dValue = 0;
		
//		GetItemValue(code, m_sBranchID, dQty, ref dValue);
//		if(dValue == 0)
			dValue = dSupplierPrice * dQty;
		m_dTotal += dValue;
		dAvgCost += (dAverageCost * dQty);

		if(!bDisplay)
			continue; //only calculate total value

		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		alterColor = !alterColor;
		Response.Write(">");
		if(m_sBranchID == "0")
			Response.Write("<td >" + branchname + "</td>");
		Response.Write("<td align=center><a href=stock_value.aspx?t=detail&code=" + code);
		Response.Write(" class=o target=_blank title='click to view details'><b>"+code+"</b></a></td>");
		Response.Write("<td align=center><b>" + supplier_code + "</b></td>");
		
		Response.Write("<td> "+s_productdesc+"</td>");
		
	//	Response.Write("<input type=hidden name=code" + i + " value='" + code + "'>");
		Response.Write("<td> " + dSupplierPrice.ToString("c") + "</td>");
		Response.Write("<td>" + dAverageCost.ToString("c") + "</td>");
		Response.Write("<td nowrap>");
		Response.Write("<b><font color=");
		if(dQty == 0)
			Response.Write("Green>");
		else if(dQty < 0)
			Response.Write("Red>");
		else if(dQty > 0)
			Response.Write("Black>");

		Response.Write(s_qty);
		Response.Write("</font></b></td>");

		Response.Write("<td align=right>" + dValue.ToString("c") + "</td>");
		Response.Write("<td align=right>" + (dAverageCost * dQty).ToString("c") + "</td>");
		Response.Write("</tr>");
	
	}
		
	//PrintPageIndex();
	int nCols = 6;
if(m_sBranchID == "0")
	nCols = 7;
	Response.Write("<tr><td colspan="+ nCols +" align=right><font color=red><b>Sub Total : </b></font></td>");
	Response.Write("<td align=right><b>" + m_dTotal.ToString("c") + "</b></td>");
	Response.Write("<td align=right><b>" + dAvgCost.ToString("c") + "</b></td></tr>");
	Response.Write("</table>");
	Response.Write("</form></center>");
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT p.cat ";
	sc += " FROM product p ";
//	sc += " JOIN stock_qty s ON s.code=p.code "; //retail version
	sc += " ORDER BY p.cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;
	Response.Write("<b>Catalog Select : </b><select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&cat='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
		else
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");

	}
	Response.Write("</select>");
	
	if(cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
	
		sc = "SELECT DISTINCT s_cat ";
		sc += " FROM product p ";
//		sc += " JOIN stock_qty s ON s.code = p.code ";// retail version
		sc += " WHERE p.cat = N'"+ cat +"' ";
		sc += " ORDER BY p.s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?cat="+HttpUtility.UrlEncode(cat));
		Response.Write("&r=" + DateTime.Now.ToOADate() + "&scat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
			
		}
		Response.Write("</select>");
	}

	
	if(s_cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat ";
		sc += " FROM product p ";
//		sc += " JOIN stock_qty s ON s.code = p.code "; //retail version
		sc += " WHERE p.cat = N'"+ cat +"' ";
		sc += " AND p.s_cat = N'"+ s_cat +"' ";
		sc += " ORDER BY p.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&r=" + DateTime.Now.ToOADate() + "&scat=" + HttpUtility.UrlEncode(s_cat) + "&sscat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
		}
		Response.Write("</select>");
	}
	return true;
}

bool GetItemValue(string code, string branch, double dStock, ref double dValue)
{
	return GetItemValue(code, branch, MyIntParse(Math.Round(dStock, 0).ToString()), ref dValue);
}

bool GetItemValue(string code, string branch, int nStock, ref double dValue)
{
	if(dsfifo.Tables["get_item_value"] != null)
		dsfifo.Tables["get_item_value"].Clear();

	string sc = " SELECT SUM(cost) AS item_value FROM stock_cost ";
	sc += " WHERE instock = 1 ";
	if(branch != "0")
		sc += " AND branch = " + branch;
	sc += " AND code = " + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsfifo, "get_item_value");
		dValue = MyDoubleParse(dsfifo.Tables["get_item_value"].Rows[0]["item_value"].ToString());
		return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return false;
}

int GetStockQuantity(string code)
{
	if(dst.Tables["getstock"] != null)
		dst.Tables["getstock"].Clear();
   
	string sc = " SELECT DISTINCT p.code, p.supplier, p.supplier_code, p.supplier_price, p.name ";
	sc += ", sq.qty AS stock, sq.allocated_stock, sq.branch_id, b.name ";
	sc += " FROM product p  ";
	sc += " JOIN stock_qty sq ON p.code=sq.code JOIN branch b ON b.id = sq.branch_id";
	sc += " WHERE p.code=" + code ;
	if(Session["branch_support"] != null)
	sc += " AND sq.branch_id="+Session["branch_id"];
	
	 
	//sc += " SELECT sum(qty) as total_stock from stock_qty WHERE code = "+code;
	
//DEBUG("Sc" , sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "getstock") > 0)
			return MyIntParse(dst.Tables["getstock"].Rows[0]["stock"].ToString());
			
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return 0;
	}
	return 0;
}

bool ShowItemDetails()
{  
   
	string code = Request.QueryString["code"];
	double dValue = 0;
	double dAveCost = 0;
	int nStock = GetStockQuantity(code);
	
	int nQty = nStock; 
   
	PrintAdminHeader();
	Response.Write("<br><center><h3>Item Stock Value Details</h3>");
	Response.Write("<b>Current Branch Stock : " + nStock.ToString() +"</b><br>");
	
    
	if(nStock <= 0) //out of stock
	{
//		if(!fifo_getLastCost(code, m_sBranchID, ref dAveCost))
//			return false;

//		dValue = dAveCost * nStock;
		Response.Write("<h4>Out of stock");//, last purchase cost : " + dAveCost.ToString("c"));
		return true;
	}

	Response.Write("<table width=95% cellspacing=1 cellpadding=0 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#8BB7DD>");
	Response.Write("<th align=center>Purchase#</th>");
	Response.Write("<th align=center nowrap>StockDate</th>");
	Response.Write("<th align=center nowrap>PurchaseQty</th>");
	Response.Write("<th nowrap>Cost"+"("+m_sCurrencyName+")"+"</th>");
	Response.Write("<th>AppliedQty</th>");
	Response.Write("<th>Value</th>");
	Response.Write("</tr>");

	double dCost = 0;
	int loop = 0;
	int q = 0; //total quantity
	int q_applied = 0; //total applied quantity
	int q_left = nQty;
	string dTime = "";
	if(dsfifo.Tables["purchase"] != null)
		dsfifo.Tables["purchase"].Clear();
	while(loop < 999) //protection
	{
		loop++;
		int rows = 0;

		string sc = " SET DATEFORMAT dmy ";
		sc += " SELECT TOP 10 p.id, p.type, i.qty, i.price, p.date_received ";
		sc += " FROM purchase_item i JOIN purchase p ON p.id = i.id";
		sc += " WHERE i.code = " + code;
		sc += " AND p.status = 2 "; //status received
		sc += " AND p.date_received IS NOT NULL "; //protection
		if(dTime != "")
			sc += " AND p.date_received < '" + dTime + "' ";
		sc += " ORDER BY p.date_received DESC ";
//DEBUG("SC ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dsfifo, "purchase");
			if(rows <= 0)
			{
//				if(!fifo_getLastCost(code, m_sBranchID, ref dAveCost))
//					return false;

//				dValue = dAveCost * nStock;

				Response.Write("</table><h4>No Purchase Record");//, use last cost(" + dAveCost.ToString("c") + "), Total Value = " + dValue.ToString("c") + "<br>");
				return false; //didn't find
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		for(int i=0; i<rows; i++)
		{
			DataRow dr = dsfifo.Tables["purchase"].Rows[i];

			int pq = MyIntParse(dsfifo.Tables["purchase"].Rows[i]["qty"].ToString()); //qty from this purchase
			q += pq; //total qty add up
//DEBUG("q=", q);
			if(q >= nStock) //all current stock from this purchase and later purchase, here we begin
			{
				string purchase_id = dr["id"].ToString();
				int n_qty = MyIntParse(dr["qty"].ToString());
				string date_received = DateTime.Parse(dr["date_received"].ToString()).ToString("dd-MM-yyyy");
				double d_cost = MyDoubleParse(dr["price"].ToString());

				dCost = MyDoubleParse(dsfifo.Tables["purchase"].Rows[i]["price"].ToString());
				int sq = nStock - (q - pq); //stock qty in this cost
				int aq = q_left; //applied qty in this cost
				if(sq < q_left) //qty in this cost not enough for this sale
					aq = sq; //apply all stock in this cost

				Response.Write("<tr>");
				Response.Write("<td><a href=purchase.aspx?t=pp&n=" + purchase_id + " target=_blank class=o title='click to view purchase'>");
				Response.Write(purchase_id + "</a></td>");
				Response.Write("<td>" + date_received + "</td>");
				Response.Write("<td align=center>" + n_qty.ToString() + "</td>");
				Response.Write("<td>" + d_cost.ToString("c") + "</td>");
				Response.Write("<td align=center>" + aq.ToString() + "</td>");
				Response.Write("<td align=right>" + (d_cost * aq).ToString("c") + "</td>");
				Response.Write("</tr>");

				q_applied += aq;
				q_left -= aq; //qty left to get cost

				if(q_left <= 0) //all in this cost, out
				{
					dValue = dCost * aq;
					loop = 99999999; //break out while
					break;
				}
				dValue += dCost * aq; //save for late calculation

				i--; //next purchase
				for(int j=i; j>=0; j--)
				{
					dr = dsfifo.Tables["purchase"].Rows[j];
					purchase_id = dr["id"].ToString();
					n_qty = MyIntParse(dr["qty"].ToString());
					date_received = DateTime.Parse(dr["date_received"].ToString()).ToString("dd-MM-yyyy");
					d_cost = MyDoubleParse(dr["price"].ToString());

					pq = MyIntParse(dsfifo.Tables["purchase"].Rows[j]["qty"].ToString()); //qty from this purchase
					dCost = MyDoubleParse(dsfifo.Tables["purchase"].Rows[j]["price"].ToString());
					aq = q_left; //applied qty in this cost
					if(pq < q_left) //qty in this cost not enough for this sale
						aq = pq; //apply all stock in this cost
//					else if(j == 0) //last purchase
//						aq = pq; //apply all qty
					dValue += dCost * aq; //save for late calculation

					Response.Write("<tr>");
					Response.Write("<td><a href=purchase.aspx?t=pp&n=" + purchase_id + " target=_blank class=o title='click to view purchase'>");
					Response.Write(purchase_id + "</a></td>");
					Response.Write("<td>" + date_received + "</td>");
					Response.Write("<td align=center>" + n_qty.ToString() + "</td>");
					Response.Write("<td>" + d_cost.ToString("c") + "</td>");
					Response.Write("<td align=center>" + aq.ToString() + "</td>");
					Response.Write("<td align=right>" + (d_cost * aq).ToString("c") + "</td>");
					Response.Write("</tr>");

					q_applied += aq;
					q_left -= aq;
					if(q_left <= 0) //all in this cost, out
					{
						loop = 99999999; //break out while
						i = rows; //break out for(i)
						break;
					}
				}
//DEBUG("left=", q_left);
//				dValue += dCost * q_left;
				break;
			}
			dTime = DateTime.Parse(dsfifo.Tables["purchase"].Rows[i]["date_received"].ToString()).ToString("dd/MM/yyyy HH:mm");
		}
	}

	Response.Write("<tr><td align=center><b>Total Value</b></td>");
	Response.Write("<td colspan=3>&nbsp;</td>");
	Response.Write("<td align=center>" + q_applied + "</td>");
	Response.Write("<td align=right>" + dValue.ToString("c") + "</td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	return true;
}

</script>

<asp:Label id=LFooter runat=server/>

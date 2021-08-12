<!-- #include file="page_index.cs" -->
<!-- #include file="isdate.cs" -->
<script runat="server">

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_serial = "";
string m_supplier = "";
string m_invoice = "";
string m_purchase_date = "";
string m_finish_date = "";
string m_prod_desc = "";
string m_status = "";
string m_product_code = "";
string m_cost ="";
string m_branch_id = "";
string m_supplier_code = "";
string m_id = "";

string m_lastSearchText = "";

string m_sn = "";// things to search

int m_nPageSize = 25;
int m_page = 1;

bool m_IwayOldSN = false;
bool m_bMultiItemFound = true;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(Request.QueryString["p"] != null)
	{
		if(IsInteger(Request.QueryString["p"]))
			m_page = int.Parse(Request.QueryString["p"]);
	}
	//DEBUG("m_page= ", m_page);
	if(!SecurityCheck("technician"))
		return;
				
	PrintAdminHeader();
	PrintAdminMenu();
	if(Request.QueryString["sn"] != null && Request.QueryString["sn"] != "")
	{
		m_lastSearchText = Request.QueryString["sn"].ToString();
		m_sn = Request.QueryString["sn"].ToString();
	}
	Response.Write("<br><center><h4>Serial Number Trace</h4>");

		//add link search from replace product to customer -Tee-
	DisplaySearchForm();

	if(m_sn != "" && m_sn != null)
		DoSerialSearch();

		//Response.Write("<input type=button value='<<back' "+Session["button_style"]+" ");
		//Response.Write(" >");
	
//	DisplaySearchForm();
	if(Request.Form["txtSearch"] != null || Request.Form["cmd"] == "Search" ) 
	{
		if(Request.Form["txtSearch"] == null || Request.Form["txtSearch"] == "")
		{
			Response.Write("<br><center><h3>Nothing to search</h3>");
			return;
		}
		
		m_lastSearchText = Request.Form["txtSearch"];
		m_sn = Request.Form["txtSearch"];

		DoSerialSearch(); //DW
	}

	if(Request.QueryString["edit"] != null)
	{
		if(!GetProduct())
			return;
		//m_id = Request.QueryString["id"].ToString();
		
	}
	if(Request.QueryString["success"] == "y" || Request.Form["cmd"] == "Update Item")
	{
		if(UpdateProduct())
		{
			Response.Write("<br><br><center><h3>Done! Please wait a moment");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=snsearch.aspx?edit=" + m_serial + "&id=" + m_id + "\">");
		}
		return;
	}

	if(Request.QueryString["del"] != null)
	{
		if(!DeleteProduct())
			return;
	}
	
	LFooter.Text = m_sAdminFooter;
}

bool DeleteProduct()
{
	string sserial = Request.QueryString["del"].ToString();

	string sc = "DELETE  FROM stock ";
	sc += " WHERE sn = '"+sserial+"'";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

bool UpdateProduct()
{
	m_id = Request.QueryString["id"].ToString();
	m_serial = Request.Form["txtserial"];
	m_supplier = Request.Form["txtsupp"];
	m_invoice = Request.Form["txtinv"];
	m_purchase_date = Request.Form["txtpdate"];
	m_finish_date = Request.Form["txtfdate"];
	m_prod_desc = Request.Form["txtpdesc"];
	m_status = Request.Form["status"];
	m_product_code = Request.Form["txtpcode"];
	m_supplier_code = Request.Form["txtsuppcode"];
	m_branch_id= Request.Form["branch"];
	m_cost = Request.Form["txtcost"].ToString();

	//string sc = " set DATEFORMAT dmy ";
	string sc = " UPDATE stock  ";
	sc += " SET sn='"+ m_serial+"', purchase_order_id='"+m_invoice+"', supplier='"+ m_supplier+"', ";
	sc += " purchase_date= '"+m_purchase_date+"', prod_desc='"+m_prod_desc+"', status="+m_status+", ";
	sc += " product_code='"+ m_product_code +"', warranty='"+m_finish_date+"', ";
	sc += " supplier_code ='"+m_supplier_code+"', branch_id = "+m_branch_id+", cost="+m_cost+"";
	sc += "  WHERE id=" + m_id + " ";

	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

bool GetProduct()
{
	string sid = Request.QueryString["id"].ToString();

	string sc = "SELECT * FROM stock ";
	sc += " WHERE id = '"+sid+"'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "editstock");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	if(Request.QueryString["id"] != null)
		m_id = Request.QueryString["id"].ToString();
	Response.Write("<center><h4>Edit Serial Status </h4></center>");
	Response.Write("<form name=frmEdit method=post action='snsearch.aspx?success=y&id="+m_id+"'>");
	Response.Write("<hr size=1 color=black width=600>");
	Response.Write("<br><table align=center cellspacing=2 cellpadding=1 border=1 bordercolor=#EEEEEE background-color=white");

	if(dst.Tables["editstock"].Rows.Count == 1 )
	{
		DataRow dr = dst.Tables["editstock"].Rows[0];
		m_id = dr["id"].ToString();
		m_serial = dr["sn"].ToString();
		m_supplier = dr["supplier"].ToString();
		m_invoice = dr["purchase_order_id"].ToString();
		m_purchase_date = dr["purchase_date"].ToString();
		m_finish_date = dr["warranty"].ToString();
		m_prod_desc = dr["prod_desc"].ToString();
		m_status = dr["status"].ToString();
		m_product_code = dr["product_code"].ToString();
		m_branch_id = dr["branch_id"].ToString();
		m_cost = dr["cost"].ToString();
		m_supplier_code = dr["supplier_code"].ToString();
	
		Response.Write("<tr style=\"color:Black;background-color:#E3E3E3;font-weight:bold;\">");
		Response.Write("<tr><td>Serial NO</td>");
		Response.Write("<td><input type=text name=txtserial size=50 value='" +m_serial+ "'></td></tr>");

		Response.Write("<tr><td>Supplier</td>");
		Response.Write("<td><input type=text name=txtsupp size=50 value='" +m_supplier+ "'></td></tr>");
		Response.Write("<tr><td>Supplier Code</td>");
		Response.Write("<td><input type=text name=txtsuppcode size=50 value='" +m_supplier_code+ "'></td></tr>");
		Response.Write("<tr><td>Invoice NO</td>");
		Response.Write("<td><input type=text name=txtinv size=50 value='" +m_invoice+ "'></td></tr>");
		Response.Write("<tr><td>Purchase Date</td>");
		Response.Write("<td><input type=text name=txtpdate size=50 value='" +m_purchase_date+ "'></td></tr>");
		Response.Write("<tr><td>Warranty(yr)</td>");
		Response.Write("<td><input type=text name=txtfdate size=50 value='" +m_finish_date+ "'></td></tr>");
		Response.Write("<tr><td>Product Description</td>");
		Response.Write("<td><input type=text name=txtpdesc size=50 value='" +m_prod_desc+ "'></td></tr>");
		Response.Write("<tr><td>Status</td>");
		Response.Write("<td><select name=status>");
		Response.Write(GetEnumOptions("stock_status", m_status));
		Response.Write("</select></td></tr>");
		Response.Write("<tr><td>Product Code</td>");
		Response.Write("<td><input type=text name=txtpcode size=50 value='" +m_product_code+ "'></td></tr>");
		
		Response.Write("<tr><td>Cost</td>");
		Response.Write("<td><input type=text name=txtcost size=50 value='" +m_cost+ "'></td></tr>");

		//branch
		Response.Write("<tr><td>Branch</td>");
		Session["branch_id"] = MyIntParse(m_branch_id);
		Response.Write("<td>");
		PrintBranchNameOptions();
		Response.Write("</td></tr>");

		Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='Update Item' " + Session["button_style"] + "></td></tr>");
	}

	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("<hr size=1 color=black width=600>");
	return true;
}

void DisplaySearchForm()
{
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	const string jav = @"
	function checksearch() 
	{	
		
		if(document.frmSearch.txtSearch.value=='') {
		window.alert ('Please Supply a Search Value!');
		document.frmSearch.txtSearch.select();
		document.frmSearch.txtSearch.focus();
			return false;
		}
		if(document.frmSearch.txtSearch.value.length < 2) {
		window.alert ('Please Value must be greater than 2 characters!');
		document.frmSearch.txtSearch.select();
		document.frmSearch.txtSearch.focus();
		
		return false;
		}
		
	}	
	";
	Response.Write("--> "); 
	Response.Write(jav);
	Response.Write("</script");
	Response.Write("> ");
	Response.Write("<form name=frmSearch method=post onsubmit='return checksearch()' action='snsearch.aspx'><br><table >");
	Response.Write("<tr><td colspan=2 ><b>Enter Serial : </b></td>");
	if(Request.Form["txtSearch"] != null)
		m_lastSearchText = Request.Form["txtSearch"].ToString();
	m_lastSearchText = m_sn;
	Response.Write("<td><input type=text name=txtSearch value='"+m_lastSearchText+"'> ");
	Response.Write("<script");
	Response.Write(">\r\n\r\ndocument.frmSearch.txtSearch.select();document.frmSearch.txtSearch.focus();\r\n</script");
	Response.Write(">\r\n");
	Response.Write("<input type=submit name=cmd value='Search' onclick='return checksearch()' " + Session["button_style"] + "></td></tr>");
	Response.Write("</table></form>");
}

bool TryNoPurchaseSearch()
{
	if(dst.Tables["SearchResult"] != null)
		dst.Tables["SearchResult"].Clear();
	
	string SearchSN = Request.Form["txtSearch"];

	string sc = "SELECT null AS id, ss.sn, null AS warranty, p.name AS prod_desc, null AS po_inv ";
	sc += ", null AS po_id, null AS location, p.code AS product_code ";
	sc += ", null AS purchase_date, p.supplier, p.supplier_code, ss.invoice_number, i.commit_date AS sales_date ";
	sc += ", c.id AS customer_id, c.trading_name, r.id AS rma_id, r.repair_date, null AS po_id ";
	sc += " FROM sales_serial ss JOIN product p ON p.code=ss.code ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number=ss.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id=i.card_id ";
	sc += " LEFT OUTER JOIN repair r ON r.serial_number=ss.sn ";
	sc += " WHERE ss.sn LIKE '%" + SearchSN + "%' ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "SearchResult");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

void BindIwayOldSNStatusTable()
{
	string tableName = "SearchResult";

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	m_cPI.PageSize = 25;
	int rows = 0;
	if(dst.Tables[tableName] != null)
		rows = dst.Tables[tableName].Rows.Count;

	m_cPI.TotalRows = rows;
	m_cPI.URI = "?sn="+m_sn+"";

	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100% cellspacing=0 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr style=\"color:white;background-color:#EEECCC;font-weight:bold;\">");
	Response.Write("<tr bgcolor=#CCCDDD>");
	Response.Write("<th>S/N</th>");
	Response.Write("<th>PRODUCT DESCRIPTION</th>");

	Response.Write("<th align=left>SUPPLIER</th>");
	Response.Write("<th align=left>SUPP_CODE#</th>");
	Response.Write("<th align=left>PURCHASE DATE</th>");
	Response.Write("<th align=left>PURCHASE#</th>");
	Response.Write("<th align=left>SOLD_DATE</th>");
	Response.Write("<th align=left>CUSTOMER</th>");
	Response.Write("<th align=left>SOLD_INV</th>");
	Response.Write("<th align=left>DATE_RMA</th>");
//		Response.Write("<th align=left>LOCATION</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	string sn_old = "";
	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = dst.Tables[tableName].Rows[i];
		string sn = dr["sn"].ToString();
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string date_invoiced = dr["date_invoiced"].ToString();
		string customer = dr["customer"].ToString();
		string customer_inv = dr["cu_inv"].ToString();
		string sold_date = dr["date_out"].ToString();
		string rma_date = dr["date_ra"].ToString();
		//string inv_number = dr["inv_number"].ToString();
		string inv_number = dr["inv_num1"].ToString();
		
		string desc = dr["name"].ToString();

		desc = desc.Replace("[", "<");
		desc = desc.Replace("]", ">");
		desc = StripHTMLtags(desc);
		string desc_title = code + " : " + desc;
		string status = dr["location"].ToString().ToUpper();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td><a title='Check on this SN#' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&sn=" + HttpUtility.UrlEncode(sn) + "' class=o>" + sn + "</a></td>");
		Response.Write("<td title='" + desc_title + "'>" + desc + "</td>");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + date_invoiced + "</td>");
		Response.Write("<td>" + inv_number + "</td>");
		Response.Write("<td>" + sold_date + "</td>");
		Response.Write("<td>" + customer + "</td>");
		Response.Write("<td>" + customer_inv + "</td>");
		Response.Write("<td>" + rma_date + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

}

void BindStatusTable()
{
	string tableName = "SearchResult";

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	m_cPI.PageSize = 25;
	int rows = 0;
	if(dst.Tables[tableName] != null)
		rows = dst.Tables[tableName].Rows.Count;

	m_cPI.TotalRows = rows;
	m_cPI.URI = "?sn="+m_sn+"";
//	m_cPI.URI = "?r="+ DateTime.Now.ToOADate();
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100% cellspacing=0 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr style=\"color:white;background-color:#EEECCC;font-weight:bold;\">");
	Response.Write("<tr bgcolor=#CCCDDD>");
	Response.Write("<th>S/N</th>");
	
//	Response.Write("<th>INV#</th>");
//	Response.Write("<th>Date</th>");
//	Response.Write("<th>WARR</th>");
	Response.Write("<th>PRODUCT DESCRIPTION</th>");
	
	Response.Write("<th align=left>SUPPLIER</th>");
	Response.Write("<th align=left>SUPP_CODE#</th>");
	Response.Write("<th align=left>PURCHASE DATE</th>");
	Response.Write("<th align=left>SUPP_INV</th>");
//	Response.Write("<th>Code</th>");
	Response.Write("<th align=left>LOCATION</th>");
	
//	Response.Write("<th>Date</th>");
//	Response.Write("<th>Customer</th>");
//	Response.Write("<th>RMA#</th>");
//	Response.Write("<th>Date</th>");
//	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

//string sc = "SELECT s.id, s.sn, s.warranty, s.prod_desc, s.po_number, e.name, s.product_code ";
//sc += ", s.purchase_date, s.supplier, s.supplier_code, ss.invoice_number ";
	bool bAlterColor = false;
	string sn_old = "";

	for(; i < rows && i < end; i++)
	{
		DataRow dr = dst.Tables[tableName].Rows[i];
//		string id = dr["id"].ToString();
		string sn = dr["sn"].ToString();
		if(sn == sn_old)
			continue;
		sn_old = sn;
//		string supplier = dr["supplier"].ToString();
//		string po_inv = dr["po_inv"].ToString();
//		string po_id = dr["po_id"].ToString();
//		string p_date = "";
//		if(dr["purchase_date"].ToString() != "")
//			p_date = DateTime.Parse(dr["purchase_date"].ToString()).ToString("dd-MM-yyyy");
//		string warranty = dr["warranty"].ToString();
/*		string code = dr["product_code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string date_invoiced = dr["date_invoiced"].ToString();
		string inv_number = dr["inv_number"].ToString();
		string desc = dr["prod_desc"].ToString();
		string desc_title = code + " : " + desc;
		if(desc.Length > 50)
			desc = desc.Substring(0, 50);
		string status = dr["location"].ToString().ToUpper();
*/
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string date_invoiced = dr["date_invoiced"].ToString();
		string inv_number = dr["inv_number"].ToString();
		string desc = dr["name"].ToString();
		desc = desc.Replace("[", "<");
		desc = desc.Replace("]", ">");
		desc = StripHTMLtags(desc);
		string desc_title = code + " : " + desc;
		string status = dr["location"].ToString().ToUpper();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td><a title='Check on this SN#' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&sn=" + HttpUtility.UrlEncode(sn) + "' class=o>" + sn + "</a></td>");

		Response.Write("<td title='" + desc_title + "'><a href=p.aspx?" + code + ">" + desc + "<a></td>");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + supplier_code + "</td>");
		Response.Write("<td>" + date_invoiced + "</td>");
		Response.Write("<td>" + inv_number + "</td>");
			
		//status locations
		Response.Write("<td><font color=");
		if(status == "LOST")
			Response.Write("red");
		else if(status == "SOLD")
			Response.Write("green");
		else if(status == "RMA")
			Response.Write("orange");
		else
			Response.Write("black");
		Response.Write("><b>" + status + "</b></font></td>");
		
//		Response.Write("<td><a href=invoice.aspx?n=" + sales_inv + " class=o>" + sales_inv + "</a></td>");
//		Response.Write("<td>" + sales_date + "</td>");
//		Response.Write("<td><a href=ecard.aspx?id=" + customer_id + " class=o>" + customer + "</a></td>");
//		Response.Write("<td><a href=rmastatus.aspx?id=" + rma + " class=o>" + rma + "</a></td>");
//		Response.Write("<td>" + rma_date + "</td>");
		
//		if(id != "")
//		{
//			Response.Write("<td><a href='snsearch.aspx?del=" + sn + "' class=o>Del</a> ");
//			Response.Write("<a href='snsearch.aspx?edit=" + sn + "&id=" + id + "' class=o>Edit</a></td>");
//		}
//		else
//			Response.Write("<td>&nbsp;</td>");

		Response.Write("</tr>");
	}
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
}


void BindStockGrid()
{
	DataView source = new DataView(dst.Tables["SearchResult"]);
	//string path = Request.ServerVariables["URL"].ToString();
	MyDataGrid.DataSource = source;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindStockGrid();
}

bool DoSerialSearch()
{
	int rows = 0;

	//stock search
	string sc = "";

/*	sc = " SELECT t.*, c.name AS staff_name "; 
	sc += ", s.warranty, s.prod_desc, e.name AS location, s.product_code ";
	sc += ", s.purchase_date, s.supplier, s.supplier_code , ps.inv_number, ps.po_number, ps.date_invoiced , i.commit_date";
	sc += ", ss.code AS product_code2, cr.name AS prod_desc2, cr.supplier_code AS supplier_code2, ss.invoice_number AS invoice_number2 ";
*/
	/*sc += " FROM serial_trace t ";
	sc += " LEFT OUTER JOIN stock s ON s.sn = t.sn "; 
	sc += " LEFT OUTER JOIN sales_serial ss ON ss.sn = s.sn AND ss.sn = t.sn ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number = t.invoice_number ";
	//sc += " LEFT OUTER JOIN purchase ps ON t.po_id = ps.id ";
	sc += " LEFT OUTER JOIN purchase ps ON s.purchase_order_id = ps.id ";
	sc += " LEFT OUTER JOIN enum e ON (e.id = s.status AND e.class='stock_status') ";
	sc += " LEFT OUTER JOIN card c ON c.id = t.staff ";
	*/
/*	sc += " FROM  serial_trace t LEFT OUTER JOIN ";
	sc += " stock s ON s.sn = t.sn LEFT OUTER JOIN ";
    sc += " sales_serial ss ON ss.sn = t.sn OR s.sn = ss.sn LEFT OUTER JOIN ";
	sc += " rma r ON r.serial_number = t.sn OR s.sn = r.serial_number LEFT OUTER JOIN ";
	sc += " purchase ps ON t.po_id = ps.id OR ps.id = s.purchase_order_id OR ps.id = r.po_id LEFT OUTER JOIN ";
	sc += " invoice  i ON i.invoice_number = ss.invoice_number OR t.invoice_number = i.invoice_number LEFT OUTER JOIN ";
	sc += " code_relations cr ON cr.code = ss.code OR cr.code = s.product_code LEFT OUTER JOIN ";
	sc += " enum e ON e.id = s.status AND e.class = 'stock_status' LEFT OUTER JOIN ";
	sc += " card c ON c.id = t.staff ";

	sc += " WHERE t.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
	//sc += " ORDER BY t.sn ";
	sc += " ORDER BY t.id DESC ";
*/
/*	sc = " SELECT DISTINCT c.name, c.code, c.supplier_code, c2.company AS supplier, e.name AS location, t.sn, s.update_time, p.id, p.date_invoiced ";
	sc += " , p.po_number, p.inv_number, ss.invoice_number, i.commit_date ";
	sc += " FROM serial_trace t LEFT OUTER JOIN stock s ON t.sn = s.sn ";
	sc += " LEFT OUTER JOIN sales_serial ss ON s.sn = ss.sn OR t.sn = ss.sn ";
	sc += " LEFT OUTER JOIN purchase p ON p.id = s.purchase_order_id ";
	sc += " LEFT OUTER JOIN purchase_item pi ON pi.id = s.purchase_order_id AND p.id = pi.id ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number = ss.invoice_number ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = pi.code OR ss.code = c.code ";
	sc += " LEFT OUTER JOIN enum e ON e.id = s.status AND e.class = 'stock_status' ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = p.supplier_id ";
	sc += " WHERE t.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
	sc += " OR ss.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
	//sc += " OR ss.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";

//	sc += " ORDER BY t.sn DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "SearchResult");

	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	
*/	

	//serial search
	sc = " SELECT DISTINCT TOP 1000 st.sn, st.prod_desc AS name, st.product_code AS code, st.supplier_code, st.supplier, st.update_time AS date_invoiced ";
	sc += " , e.name AS location , st.purchase_order_id AS inv_number ";
	sc += " FROM stock st JOIN enum e ON e.class = 'stock_status' AND e.id = st.status ";
	sc += " WHERE 1=1 ";
	
	if(Request.QueryString["sn"] != null && Request.QueryString["sn"] != "" && !m_bMultiItemFound )
		sc += " AND st.sn = '" + EncodeQuote(m_sn) + "' ";
	else
		sc += " AND st.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
//	sc += " ORDER BY st.sn DESC ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "SearchResult");
		if(rows == 1)
			m_bMultiItemFound = false;

	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	if(rows <= 0 && m_sCompanyName.ToLower() == "iway")
	{
		sc = " SELECT TOP 1000 model AS supplier_code, bar_code AS sn, description AS name ";
		sc += " ,date_in AS date_invoiced ";
		sc += " ,supplier, customer  "; 
		sc += ", su_inv AS inv_num1 ";
		//sc += " ,supplier, su_inv AS inv_number, customer  "; 
		sc += " ,date_ra, qty, comment, bar_no,  cu_inv ";
		sc += " ,date_out, cur_no";
		sc += " FROM sn_old ";
		sc += " WHERE 1=1 ";

		if(Request.QueryString["sn"] != null && Request.QueryString["sn"] != "" && !m_bMultiItemFound )
			sc += " AND bar_code = '" + EncodeQuote(m_sn) + "' ";
		else
			sc += " AND bar_code LIKE '%" + EncodeQuote(m_sn) + "%' ";
		sc += " ORDER BY bar_code ";
//DEBUG("sc = ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "SearchResult");
			if(rows > 0)
				m_IwayOldSN = true;
			if(rows == 1)
				m_bMultiItemFound = false;
						
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
	}

	if(m_IwayOldSN)
	{
		BindIwayOldSNStatusTable();
		return false;
	}
	//serial trace search
	sc = " SELECT TOP 1000 t.*, c.name AS staff_name "; //, e.name AS location "; 
	sc += " FROM serial_trace t LEFT OUTER JOIN stock s ON s.sn = t.sn ";
	sc += " LEFT OUTER JOIN card c ON c.id = t.staff ";
//	sc += " LEFT OUTER JOIN enum e ON e.id = s.status AND e.class = 'stock_status'  ";
	sc += " WHERE t.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
	sc += " ORDER BY t.id DESC ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "TraceResult");

	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}


	bool bSingleSNFound = false;
	if(rows == 1)
		bSingleSNFound = true;

	
	if(bSingleSNFound)
	{
		//sc = " SELECT DISTINCT st.sn, pi.name, pi.code, pi.supplier_code, e.name AS location, p.po_number, p.inv_number AS supplier_invoice_number, p.date_received, p.supplier_id ";
		sc = " SELECT DISTINCT p.po_number, p.inv_number AS supplier_invoice_number, p.date_received, p.supplier_id ";
		sc += ", st.purchase_order_id ";
		sc += ", c.company AS supplier_company ";
		sc += " FROM stock st JOIN purchase p ON p.id = st.purchase_order_id ";
		sc += " JOIN purchase_item pi ON pi.id = p.id AND pi.id = st.purchase_order_id ";
		sc += " JOIN enum e ON e.id = st.status AND e.class = 'stock_status' ";
		sc += " JOIN card c ON c.id = p.supplier_id ";
//		sc += " WHERE st.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
		sc += " WHERE st.sn = '" + EncodeQuote(m_sn) + "' ";
//		sc += " ORDER BY st.sn DESC ";
//		DEBUG("purchase =", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "Purchase");

		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}

		sc = " SELECT DISTINCT i.invoice_number, i.commit_date, i.card_id, c.name AS customer ";
		sc += ", c.company AS customer_company ";
		sc += " FROM sales_serial ss JOIN invoice i ON i.invoice_number = ss.invoice_number ";
		sc += " JOIN card c ON c.id = i.card_id ";
//		sc += " WHERE ss.sn LIKE '%" + EncodeQuote(m_sn) + "%' ";
		sc += " WHERE ss.sn = '" + EncodeQuote(m_sn) + "' ";
//		sc += " ORDER BY ss.sn DESC ";
//		DEBUG("sales =", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "Sales");

		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
	
	}
	
	if(rows <= 0 && dst.Tables["TraceResult"].Rows.Count <=0)
	{
		Response.Write("<br><center><h3><font color=red>No Record Found</font></h3>");
		return false;
	}
	if(rows <= 0 && dst.Tables["TraceResult"].Rows.Count > 0)
	{
		SearchTraceResult();
		return true;
	}

/*	string sn = dst.Tables["SearchResult"].Rows[0]["sn"].ToString();
	for(int i=1; i<rows; i++)
	{
		string sn1 = dst.Tables["SearchResult"].Rows[i]["sn"].ToString();
		if(sn1 != sn) //multiple sn found
		{
			BindStatusTable();
			return true;
		}
	}
*/

	if(rows > 1)
	{
		BindStatusTable();
		return true;
	}

	if(rows == 1)
		PrintTraceResult();
	
	return true;
}

void PrintTraceResult()
{
/*	DataRow dr = dst.Tables["SearchResult"].Rows[0];

	string sn = dst.Tables["SearchResult"].Rows[0]["sn"].ToString();
	string location = dst.Tables["SearchResult"].Rows[0]["location"].ToString();
	string desc = dst.Tables["SearchResult"].Rows[0]["name"].ToString();
	string code = dst.Tables["SearchResult"].Rows[0]["code"].ToString();
	string supp_code = dst.Tables["SearchResult"].Rows[0]["supplier_code"].ToString();
	string inv_date = dst.Tables["SearchResult"].Rows[0]["commit_date"].ToString();
	string supp_inv_date = dst.Tables["SearchResult"].Rows[0]["date_invoiced"].ToString();
	string inv = dst.Tables["SearchResult"].Rows[0]["invoice_number"].ToString();
	string supp_inv = dst.Tables["SearchResult"].Rows[0]["inv_number"].ToString();
	string po_number = dst.Tables["SearchResult"].Rows[0]["id"].ToString();
	string po = dst.Tables["SearchResult"].Rows[0]["po_number"].ToString();

	
	if(location == "")
	{
		if(inv != "")
			location = "Sold";
	}
*/	
	
//	Response.Write("<h5><font color=red>" + m_sn + "</font></h5>");
/*esponse.Write("<table align=center border=0 cellspacing=1 cellpadding=4 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6>");
	Response.Write("<table align=center border=0 cellspacing=1 cellpadding=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=0><b>PRODUCT DESCRIPTION : </b></th><td colspan=2><font color=green>" + desc + "</font></td></tr>");
	Response.Write("<tr><th colspan=0 align=left>PRODUCT CODE : </th><td colspan=2><a title='click to view product information' href='p.aspx?p="+code+"' class=o target=_new>"+code+"</td></tr>");
	Response.Write("<tr><th colspan=0 align=left>M_PN :</th><td colspan=2>"+ supp_code +"</td></tr>");
	Response.Write("<tr>");
	Response.Write("<td><b>S/N# : </b></td><td>" + sn.ToUpper() + "</td>");
	Response.Write("<tr><td ><b>CURRENT LOCATION : </b></td><td><font color=red><b>" + location.ToUpper() + "</b></font></td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	Response.Write("</td></tr>");

		Response.Write("<tr bgcolor=#E3E3DE><th>"+ m_sCompanyName.ToUpper() +" PURCHASE#</th><th>Supp_INV#</th><th>Supplier INV DATE</th><th>Customer INV#</th><th>INV# Date</th><th>Pack#</th></tr>");
		Response.Write("<tr><th><a title='click to view purchase detail' href='purchase.aspx?t=pp&n=" + po_number + "' class=o target=_new>"+po+" </a></th>");
		Response.Write("<th><a title='click to view purchase detail' href='purchase.aspx?t=pp&n=" + po_number + "' class=o target=_new>"+supp_inv+" </a></th>");

	if(supp_inv_date == null || supp_inv_date == "")
		supp_inv_date = dst.Tables["SearchResult"].Rows[0]["update_time"].ToString();
	Response.Write("<th>"+ supp_inv_date +"</th>");
	Response.Write("<th><a title='click to view invoice' href='invoice.aspx?"+ inv +"' class=o target=_new>"+ inv +"</a></th>");
	Response.Write("<th>"+ inv_date +"</th>");
	Response.Write("<th><a title='click to packing slip' href='pack.aspx?i="+ inv +"' class=o target=_new>"+inv+"</a></th></tr>");

	Response.Write("</table>");
*/
	
	//item seasrch result
	DataRow dr = dst.Tables["SearchResult"].Rows[0];
	string sn = dst.Tables["SearchResult"].Rows[0]["sn"].ToString();
	string location = dst.Tables["SearchResult"].Rows[0]["location"].ToString();
	string desc = dst.Tables["SearchResult"].Rows[0]["name"].ToString();
	string code = dst.Tables["SearchResult"].Rows[0]["code"].ToString();
	string supp_code = dst.Tables["SearchResult"].Rows[0]["supplier_code"].ToString();
//	string purchase_date = dst.Tables["SearchResult"].Rows[0]["date_invoiced"].ToString();
//	string supplier = dst.Tables["SearchResult"].Rows[0]["supplier"].ToString();
	if(desc != null && desc != "")
	{
		desc = desc.Replace("[", "<");
		desc = desc.Replace("]", ">");
		desc = StripHTMLtags(desc);
	}
	Response.Write("<table align=center border=0 cellspacing=1 cellpadding=4 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>PRODUCT DESCRIPTION : </b></th><td ><font color=green>" + desc + "</font></td></tr>");
	Response.Write("<tr><td><b>PRODUCT CODE : </th><td><a title='click to view product information' href='p.aspx?p="+code+"' class=o target=_new>"+code+"</td></tr>");
	Response.Write("<tr><td><b>M_PN :</th><td >"+ supp_code +"</td></tr>");
	Response.Write("<tr><td><b>S/N# : </b></td><td><a title='view the sn details' href='"+ Request.ServerVariables["URL"] +"?sn="+ HttpUtility.UrlEncode(sn) +"&r="+ DateTime.Now.ToOADate() +"' class=o>" + sn.ToUpper() + "</a></td>");
//	Response.Write("<tr><td><b>SUPPLIER : </b></td><td>" + supplier.ToUpper() + "</td>");
	Response.Write("<tr><td ><b>CURRENT LOCATION : </b></td><td><font color=red><b>" + location.ToUpper() + "</b></font></td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	
	//purchase result
	if(dst.Tables["Purchase"].Rows.Count == 1)
	{
		string stable = "Purchase";
		string purchase_number = dst.Tables[stable].Rows[0]["supplier_invoice_number"].ToString();
		string po_number = dst.Tables[stable].Rows[0]["po_number"].ToString();
		string purchase_date = dst.Tables[stable].Rows[0]["date_received"].ToString();
		string supplier_name = dst.Tables[stable].Rows[0]["supplier_company"].ToString();
		string pid = dst.Tables[stable].Rows[0]["purchase_order_id"].ToString();
		string supplier_id = dst.Tables[stable].Rows[0]["supplier_id"].ToString();
		if(IsDate(purchase_date))
			purchase_date = (DateTime.Parse(purchase_date)).ToString("dd-MM-yyyy");
		Response.Write("<table align=center width=60%  border=1 cellspacing=1 cellpadding=2 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#666696 ><th colspan=4><font color=white>PURCHASE RESULT</td></tr>");
		Response.Write("<tr bgcolor=#E3E3E3><td>"+ m_sCompanyName.ToUpper() +" PURCHASE#</td><td>SUPP_INV#</td><td>PURCHASE_DATE</td><td>SUPPLIER</td></tr>");
	
		Response.Write("<tr><td><a title='click to view purchase detail' href='purchase.aspx?t=pp&n=" + pid + "' class=o target=_new>"+ po_number +"</a></td><td>"+ purchase_number +"</td>");
		Response.Write("<td>"+ purchase_date +"</td>");
		Response.Write("<td><a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + supplier_id + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>"+ supplier_name +"</a></td></tr>");
		Response.Write("</table>");
	}

	//sales result
	if(dst.Tables["Sales"].Rows.Count == 1)
	{
		string stable = "Sales";
		string invoice = dst.Tables[stable].Rows[0]["invoice_number"].ToString();
		string invoice_date = dst.Tables[stable].Rows[0]["commit_date"].ToString();
		
		string customer = dst.Tables[stable].Rows[0]["customer"].ToString();
		if(customer == "")
			customer = dst.Tables[stable].Rows[0]["customer_company"].ToString();
		if(!g_bRetailVersion)
			customer = dst.Tables[stable].Rows[0]["customer_company"].ToString();
		
		string card_id = dst.Tables[stable].Rows[0]["card_id"].ToString();
		if(IsDate(invoice_date))
			invoice_date = (DateTime.Parse(invoice_date)).ToString("dd-MM-yyyy");
		Response.Write("<table align=center width=60% border=0 cellspacing=1 cellpadding=2 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#666696 ><th colspan=4><font color=white>SALES RESULT</td></tr>");
		Response.Write("<tr bgcolor=#E3E3E3><td>INV#</td><td>INV DATE#</td><td>PACKING#</td><td>CUSTOMER</td></tr>");
		Response.Write("<tr><td><a title='click to view invoice' href='invoice.aspx?"+ invoice +"' class=o target=_new>"+ invoice +"</a></td><td>"+ invoice_date +"</td>");
		Response.Write("<td><a title='click to packing slip' href='pack.aspx?i="+ invoice +"' class=o target=_new>"+ invoice +"</a></td>");
		Response.Write("<td><a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + card_id + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>"+ customer +"</a></td></tr>");
		
		Response.Write("</table>");
	}

	//trace result
	SearchTraceResult();

}

void SearchTraceResult()
{
	Response.Write("<br>");
	Response.Write("<table align=center width=90% cellspacing=0 cellpadding=2 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4><b>FOUND TRACE RESULT</td></tr>");
	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
	
	Response.Write("<th>Date</th>");
	Response.Write("<th>SN#</th>");
	Response.Write("<th>Staff</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th>Reference</th>");
	Response.Write("</tr>");
	

	bool bAlterColor = false;
	//	DataRow[] dra = dst.Tables["SearchResult"].Select("", "logtime");
	//for(int i=0; i<dra.Length; i++)
	for(int i=0; i<dst.Tables["TraceResult"].Rows.Count; i++)
	{
		//dr = dra[i];
		DataRow dr = dst.Tables["TraceResult"].Rows[i];
		string date = DateTime.Parse(dr["logtime"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string staff = dr["staff_name"].ToString();
		string action = dr["action_desc"].ToString();
		string reference = "";
		string po_id = dr["po_id"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string dealer_rma_id = dr["dealer_rma_id"].ToString();
		string supplier_rma_id = dr["supplier_rma_id"].ToString();
		string sn = dr["sn"].ToString();
		//string inv_date = dr["commit_date"].ToString();
		//string supp_inv_date = dr["date_invoiced"].ToString();
				
		if(po_id != "0")
			reference += " <a href=purchase.aspx?t=pp&n=" + po_id + " class=o target=_new>Purchase #" + GetPoNumber(po_id) + "</a>";
		if(invoice_number != "0")
			reference += " <a href=invoice.aspx?" + invoice_number + " class=o target=_new>Invoice #" + invoice_number + "</a>";
		if(dealer_rma_id != "0")
			reference += " <a href='techr.aspx?op=5&s=2&src=" + dealer_rma_id + "' class=o target=_new>RMA Number #" + HttpUtility.UrlEncode(dealer_rma_id) + "</a>";
		if(supplier_rma_id != "0")
			reference += " <a href='supp_rma.aspx?rma=rd&spp=all&st=3&rid=" + supplier_rma_id + "' class=o target=_new>RMA Number #" + HttpUtility.UrlEncode(supplier_rma_id) + "</a>";
		

		Response.Write("<tr ");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td><a title='Select me to view SN tracing details' href='"+ Request.ServerVariables["URL"] +"?sn="+ HttpUtility.UrlEncode(sn) +"&r="+ DateTime.Now.ToOADate() +"' class=o>" + sn.ToUpper() + "</a></td>");
		Response.Write("<td>" + staff + "</td>");
		Response.Write("<td><font color=green>" + action + "</font></td>");
		Response.Write("<td>" + reference + "</td>");
		//Response.Write("<td><a title='click to view purchase detail' href='purchase.aspx?t=pp&n=" + po_id + "' class=o target=_new>"+ GetPoNumber(po_id) +"</td>");
		//Response.Write("<td>"+ supp_inv_date +"</td>");
		//Response.Write("<td><a title='click to view invoice' href='invoice.aspx?"+ invoice_number +"' class=o target=_new>"+ invoice_number +" </td>");
		//Response.Write("<td>"+ inv_date +"</td>");
		//Response.Write("<td><a title='click to view packing slip' href='pack.aspx?i="+ invoice_number +"' class=o target=_new>"+ invoice_number +" </td>");	

		Response.Write("</tr>");
	}
	Response.Write("</table><br><br>");
}

</script>

<form runat=server>

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#E3E3E3
	CellPadding=2
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=left
	AllowPaging=True
	PageSize=20
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=left
    OnPageIndexChanged=MyDataGrid_Page
	>
	<HeaderStyle BackColor=#E3E3E3 ForeColor=black Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=""
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="snsearch.aspx?edit=true&id={0}"
			 Text=EDIT
			 />
	</Columns>

</asp:DataGrid>

</form>
<asp:Label id=LFooter runat=server/>


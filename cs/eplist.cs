<!-- #include file="page_index.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table
string m_id = "";
string m_name = "";
string m_sBranch = "";
string m_last_search = "";
string cat = "";
string s_cat = "";
string ss_cat = "";
string sSystem = "";
string sOption = "";
string ra_id = "";
string ra_code = "";

int m_nPageSize = 20;
int m_page = 1;
int m_nPageSize1 = 15;
int m_page1 = 1;
int m_pageQty = 1;

int m_RowsReturn = 0;
int m_SerialReturn = 0;

//current edit products
string m_sn = "";
string m_product_code = "";
string m_cost = "";
string m_purchase_date = "";
string m_branch_id = "";
string m_po_number = "";
string m_supplier = "";
string m_supplier_code = "";
string m_status = "";

string m_snQuery= "";
string m_prodQuery = "";

string m_sort = "";
bool m_bDesc = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["sort"] != null)
		m_sort = Request.QueryString["sort"];
	if(Request.QueryString["desc"] == "1")
		m_bDesc = true;

	//getting catalog and sub catalog 
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"].ToString();
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
		s_cat = Request.QueryString["s_cat"].ToString();
	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
		ss_cat = Request.QueryString["ss_cat"].ToString();

	if(Request.QueryString["ra"] != null && Request.QueryString["ra"] != "")
		ra_code = Request.QueryString["ra"].ToString();
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		ra_id = Request.QueryString["id"].ToString();
	if(Request.QueryString["op"] != null && Request.QueryString["op"] != "")
		sOption = Request.QueryString["op"].ToString();
	if(Request.QueryString["s"] != null && Request.QueryString["s"] != "")
		sSystem = Request.QueryString["s"].ToString();

	if(Request.QueryString["p"] != null)
	{
		if(IsInteger(Request.QueryString["p"]))
			m_page = int.Parse(Request.QueryString["p"]);
	}
	if(Request.QueryString["sp"] != null)
	{
		if(IsInteger(Request.QueryString["sp"]))
			m_page1 = int.Parse(Request.QueryString["sp"]);
	}
	if(Request.QueryString["qtyp"] != null)
	{
		if(IsInteger(Request.QueryString["qtyp"]))
			m_pageQty = int.Parse(Request.QueryString["qtyp"]);
	}
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><h3><center>STOCK LIST</h3></center>");

	GetSearch();

	if(Request.Form["cmdUpdate"] == "Search Product" || Request.QueryString["search"] != null)
	{
		string sSearch = "", sSearchSN = "";
		if(Request.Form["txtSearch"] != null)
			sSearch = Request.Form["txtSearch"].ToString();

		if(Request.Form["txtSearchSN"] != null)
			sSearchSN = Request.Form["txtSearchSN"].ToString();
			
		if(Request.QueryString["sp"] != null && Request.QueryString["search"] != null)
		{
			m_last_search=Request.QueryString["search"].ToString();
			if(!SearchProduct(m_last_search))
				return;
		}
		else
		{
			if(sSearch != "")
			{
				if(!SearchProduct(sSearch))
					return;
				m_last_search = sSearch;
			}
			if(sSearchSN != "")
			{			
				if(!SearchSNQty(sSearchSN))
					return;
			}
		}
		
		if(sSearch != "")
		{
			if(m_RowsReturn <=0 && dst.Tables["searchQty"].Rows.Count <= 0)
			{
				Response.Write("<script language=javascript>");
				Response.Write("window.alert('No Item Found')\r\n");
				Response.Write("</script");
				Response.Write(">");
			}
			else
				BindSearchProduct();
		}
		else if(sSearchSN != "")
		{
			if(dst.Tables["snQty"].Rows.Count <= 0)
			{
				Response.Write("<script language=javascript>");
				Response.Write("window.alert('No Item Found')\r\n");
				Response.Write("</script");
				Response.Write(">");
			}
			else
				BindSearchSNProduct();
		}
	}
	if(!GetStockQty())
			return;
	BindStockQty();
	LFooter.Text = m_sAdminFooter;
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT cat FROM product p  ORDER BY cat";
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
	string qtyp = "";
	if(Request.QueryString["qtyp"] != "" && Request.QueryString["qtyp"] != null)
		qtyp = Request.QueryString["qtyp"].ToString();
	if(rows <= 0)
		return true;
	Response.Write("Catalog Select: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&s="+sSystem+"&op="+sOption+"&id="+ra_id+"&ra="+ra_code+"&qtyp="+qtyp+"&cat='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		if(cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
	{
		cat = Request.QueryString["cat"].ToString();
	
		sc = "SELECT DISTINCT s_cat FROM product  WHERE cat = '"+ cat +"' ";
		sc += " ORDER BY s_cat";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?ra="+ra_code+"&s="+sSystem+"&op="+sOption+"&id="+ra_id+"&cat="+cat+"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
		if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
			s_cat = Request.QueryString["s_cat"].ToString();
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			//DEBUG(" s = ", s);
			//DEBUG(" scat = ", s_cat);
			if(s_cat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
		}

		Response.Write("</select>");
	}

	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
	{
		s_cat = Request.QueryString["s_cat"].ToString();
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM product p WHERE cat = '"+ cat +"' ";
		sc += " AND s_cat = '"+ s_cat +"' ";
		sc += " ORDER BY ss_cat";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?ra="+ra_code+"&s="+sSystem+"&op="+sOption+"&id="+ra_id+"&cat="+cat+"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat="+s_cat+"&ss_cat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
			ss_cat = Request.QueryString["ss_cat"].ToString();
		
		
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(ss_cat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}
	return true;
}


bool GetStockQty()
{
	string sc = " SELECT c.expire_date, p.stock, p.code, CONVERT(varchar(60), p.name) AS name ";
	sc += ", ISNULL((SELECT SUM(s.quantity) FROM sales s WHERE s.code = p.code), 0) AS sales ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
	if(ra_code != "" && ra_code != null)
	{
		sc += " JOIN stock_borrow b ON b.code = p.code ";
	}
	sc += " WHERE 1=1 ";
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "all" && Request.QueryString["cat"] != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc += " AND p.cat = '"+ cat +"' ";
	}
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "all" && Request.QueryString["s_cat"] != "")
	{
		s_cat = Request.QueryString["s_cat"].ToString();
		sc += " AND p.s_cat = '"+ s_cat +"' ";
	}

	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "all" && Request.QueryString["ss_cat"] != "")
	{
		ss_cat = Request.QueryString["ss_cat"].ToString();
		sc += " AND p.ss_cat = '"+ ss_cat +"' ";
	}
//	if(Request.QueryString["ra"] == "code")
//	{
//		sc += " AND p.stock > 0 ";
//	}
	if(m_sort != "")
	{
		sc += " ORDER BY " + Request.QueryString["sort"];
		if(m_bDesc)
			sc += " DESC ";
	}
	else if(Request.QueryString["cat"] == "all" || (Request.QueryString["cat"] != null && Request.QueryString["s_cat"] == "all" )
		|| (Request.QueryString["cat"] != null && Request.QueryString["cat"] != null && Request.QueryString["ss_cat"] == "all"))
	{
		sc += " ORDER BY c.expire_date, p.cat, p.s_cat, p.ss_cat, p.brand, p.name, p.code ";	
	}
	else
	{
		sc += " ORDER BY c.expire_date DESC ";
	}
DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "stock_qty");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	if(ra_code != "" && ra_code != null)
	{
		if(dst.Tables["stock_qty"].Rows.Count < 0)
		{
			Response.Write("<script Language=javascript");
			Response.Write(">\r\n");
			Response.Write("window.alert('Sorry NO Item to Replace for RMA, Please Go to Borrow Item from Stock!!')\r\n");
			Response.Write("</script");
			Response.Write(">\r\n ");
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stk_borrow.aspx\">");
			return false;
		}
	}
	return true;
}

void GetSearch()
{
	Response.Write("<script language=javascript>");
	Response.Write("<!-- hide from old browser");
	string s = @"
		function checkform()
		{
			//if(document.frmSearchProduct.txtSearch.value !='' || document.frmSearchProduct.txtSearchSN.value != ''){
			if(document.frmSearchProduct.txtSearch.value == '' && document.frmSearchProduct.txtSearchSN.value == ''){

				window.alert('Please Input Product Code or Serial number for search!! ');
				document.frmSearchProduct.txtSearch.focus();
				//document.frmSearchProduct.cmdUpdate.disabled=false;
				return false;
			}
			if(!IsNumberic(document.frmSearchProduct.txtSearch.value)){
				//window.alert('Please Enter Number Only!!');
				document.frmSearchProduct.txtSearch.focus();
				document.frmSearchProduct.txtSearch.select();
				return false;
			}
			return true;			
		}
		function queryitem()
		{
				if(document.frmSearchProduct.txtQuery.value !='')
					document.frmSearchProduct.cmdQuery.disabled=false;
				else
					document.frmSearchProduct.cmdQuery.disabled=true;
		}
		
		
		function IsNumberic(sText)
		{
		   var ValidChars = '0123456789';
		   var IsNumber=true;
		   var Char;
		   for (i = 0; i < sText.length && IsNumber == true; i++) 
		   { 
			  Char = sText.charAt(i); 
			  if (ValidChars.indexOf(Char) == -1) 
						 IsNumber = false;
		   }
		   return IsNumber;
   		 }
	";
	Response.Write("//-->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	
	Response.Write("<form name=frmSearchProduct method=post action=stock.aspx?search="+m_last_search+">");
	//Response.Write("<table border=2>");
	Response.Write("<table cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td>Search by Product Code:</td><td><input type=text name=txtSearch value="+ Request.Form["txtSearch"] +"></td>");
	
	//Response.Write("<input type=submit name=cmd value='Search Product Code' OnClick='return checkform();'></td></tr>");
	
	//Response.Write("<td>&nbsp;</td><td>Query Items in Stock by Product Code:<input type=text name=txtQuery value='' onchange='queryitem();'></td>");
	//Response.Write("<td><input type=submit name=cmdQuery value='Query Item' disabled></td></tr>");
	Response.Write("<tr><td>Search by Serial Number:</td>");
	Response.Write("<td><input type=text name=txtSearchSN value="+ Request.Form["txtSearchSN"] +">");
	//Response.Write("<td><input type=text name=txtSearchSN  onchange='checkform();'>");
	
	Response.Write("<input type=submit name=cmdUpdate value='Search Product' "+ Session["button_style"] +" ");
	Response.Write(" onclick='return checkform();'></td></tr>");

	Response.Write("\r\n<script");
	Response.Write(">\r\n document.frmSearchProduct.txtSearch.focus()\r\n</script");
	Response.Write(">\r\n ");
	
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

//copy from bb.c to decode the message with special char
string msgEncode(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += "\'\'"; //double it for SQL query
		else if(s[i] == '<')
			ss += '[';
		else if(s[i] == '>')
			ss += ']';
//		else if(s[i] == '\n')
//			ss += s[i] + "<br>";
		else
			ss += s[i];
	}
	return ss;
}

bool SearchSNQty(string sSearchSN)
{
	string sc = "";
	sSearchSN = msgEncode(sSearchSN);
	//DEBUG(" sSEarch = ", sSearchSN);
	/*if(g_bRetailVersion)
	{
		sc = " SELECT sq.id, sq.code, sq.qty, CONVERT(varchar(50),pd.name) AS name, pd.price ";
		sc += " FROM stock_qty sq INNER JOIN product pd ON sq.code = pd.code";
		sc += " WHERE (sq.code = ";
		sc += " (SELECT product_code ";
		sc += " FROM stock ";
		sc += " WHERE (sn = '"+sSearchSN+"'))) "; //OR sn like '%"+sSearchSN+"%')) ";
	}
	else
	*/
	//{
		sc = " SELECT DISTINCT st.sn, pi.code,  CONVERT(varchar(60), pi.name) AS name, pd.stock AS qty, pd.price ";
		//sc += " FROM serial_trace st LEFT OUTER JOIN ";
		sc += " FROM stock st JOIN ";
		sc += " purchase p ON p.id = st.purchase_order_id JOIN ";
		sc += " purchase_item pi ON st.purchase_order_id = pi.id AND pi.id = p.id JOIN ";
		sc += " product pd ON pd.code = pi.code AND st.product_code = pi.code AND st.product_code = pd.code ";
		sc += " WHERE st.sn = '"+sSearchSN+"' ";
		//sc = " SELECT supplier+supplier_code AS ID, code, stock AS qty, CONVERT(varchar(50),name) AS name, price ";
		//sc += " FROM product ";
		//if(sSearch != "")
		//	sc += " WHERE code = "+sSearch+"";
		sc += " ORDER BY st.sn DESC ";
	//}
	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int i = myAdapter.Fill(dst,"snQty");
		//DEBUG( "i = ", i );


	}
	catch(Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	
	
	return true;
}


bool SearchProduct(string sSearch)
{	
		
/*	string sc = "SELECT * FROM stock s INNER JOIN enum e";
	sc += " ON e.id = s.status ";
	sc += " WHERE e.class = 'stock_status' ";
	sc += " AND e.name = 'In Stock' ";
	if(sSearch != "" || sSearch != null)
		sc += " AND product_code = '"+sSearch+"'";
	
*/
	string sc = " SELECT supplier+supplier_code AS ID, code, stock AS qty, CONVERT(varchar(50),name) AS name, price ";
	sc += " FROM product ";
	if(sSearch != "")
		sc += " WHERE code = "+sSearch+"";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		//m_RowsReturn=myAdapter.Fill(dst,"search");
		m_RowsReturn=myAdapter.Fill(dst,"searchQty");
		
	/*	if(m_RowsReturn <=0)
		{
			sc = " SELECT sq.id, sq.code, sq.qty, pd.name, pd.price ";
			sc += " FROM stock_qty sq INNER JOIN product pd ";
			sc += " ON sq.code = pd.code ";
			if(sSearch != "")
				sc += " AND pd.code = "+sSearch+"";
	
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				myAdapter.Fill(dst,"searchQty");
			}
			catch(Exception e)
			{
				ShowExp(sc,e);
				return false;
			}
		}
	*/
	}
	catch(Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	
	
	return true;

}

void BindSearchSNProduct()
{
	Response.Write("<table width=100% cellspacing=2 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td colspan=4><b>Search Found:</b></td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3><th>Product Code</th> ");
	Response.Write("<th align=left>Description</th> ");
	Response.Write("<th align=left>Quantity</th> ");
	Response.Write("<th align=left>Price</th> ");
	Response.Write("</tr>");
	for(int i=0; i<dst.Tables["snQty"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["snQty"].Rows[i];
		string s_product_code = dr["code"].ToString();
		string s_quantity = dr["qty"].ToString();
		string s_prod_desc = dr["name"].ToString();
		string price = dr["price"].ToString();
		double dPrice = 0;
		if(price != null && price != "")
			dPrice = double.Parse(price);
		else
			dPrice = 0;
		Response.Write("<tr>");
		//Response.Write("<td align=center>"+s_product_code+"</td>");
		Response.Write("<th><a title='click here to view the sales reference' href='salesref.aspx?code="+ s_product_code +"' target=_blank class=o>"+s_product_code+"</a>");
		Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
		Response.Write("code=" + s_product_code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
		Response.Write("<td>"+s_prod_desc+"</td>");
		if(int.Parse(s_quantity) == 0)
			Response.Write("<td><b>"+s_quantity+"</b></td>");
		else if(int.Parse(s_quantity) > 0)
			Response.Write("<td><b><font color=Green>"+s_quantity+"</font></b></td>");
		else if(int.Parse(s_quantity) < 0)
			Response.Write("<td><b><font color=Red>"+s_quantity+"</font></b></td>");
		Response.Write("<td>"+ dPrice.ToString("c") +"</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

void BindSearchProduct()
{

	Response.Write("<table width=100% cellspacing=2 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td colspan=7><b>Search Found:</b></td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th>Product Code</th> ");
	Response.Write("<th align=left>Description</th> ");
	Response.Write("<th align=left>Quantity</th> ");
	Response.Write("<th align=left>Price</th> ");
	
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (m_page1-1) * m_nPageSize1;
	for(int i=startPage; i<dst.Tables["searchQty"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize1)
			break;
		dr = dst.Tables["searchQty"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRowSearch(dr, i, alterColor))
			break;
	}
		
	//PrintPageIndexSearch();
	
	Response.Write("</table>");
	Response.Write("<hr size=1 color=black>");

}

bool DrawRowSearch(DataRow dr, int i, bool alterColor)
{
	string s_prodcode = dr["code"].ToString();
	string s_cost = dr["price"].ToString();
	string s_desc = dr["name"].ToString();
	string s_qty = dr["qty"].ToString();
	string s_sid = dr["id"].ToString();

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");
	
	Response.Write("<tr>");
	//Response.Write("<td>"+s_prodcode+"</td>");
	Response.Write("<th><a title='click here to view the sales reference' href='salesref.aspx?code="+ s_prodcode +"' target=_blank class=o>"+s_prodcode+"</a>");
	Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
	Response.Write("code=" + s_prodcode + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
	Response.Write("<td>"+s_desc+"</td>");
	if(int.Parse(s_qty) == 0 )
		Response.Write("<td><font color=black>"+s_qty+"</font></td>");
	else if(int.Parse(s_qty) <0 )
		Response.Write("<td><font color=red>"+s_qty+"</font></td>");
	else if(int.Parse(s_qty) >0 )
		Response.Write("<td><font color=green><b>"+s_qty+"</b></font></td>");
	
	Response.Write("<td>"+double.Parse(s_cost).ToString("c")+"</td>");
	return true;
}

void PrintPageIndexSearch()
{
	Response.Write("<tr><td colspan=4>Page: ");
	//int pages = dst.Tables["search"].Rows.Count / m_nPageSize1 + 1;
	int pages = dst.Tables["searchQty"].Rows.Count / m_nPageSize1 + 1;
	//int pages = dst.Tables["stock"].Rows.Count / m_nPageSize;
	for(int i=1; i<=pages; i++)
	{
		if(i != m_page1)
		{
			Response.Write("<a href='stock.aspx?search="+m_last_search+"&sp=");
			Response.Write(i.ToString());
			Response.Write("'>");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	Response.Write("</td></tr>");
}

void BindStockQty()
{
	string uri = "?cat=" + HttpUtility.UrlEncode(cat) + "&s_cat=" + HttpUtility.UrlEncode(s_cat)+ "&ss_cat=" + HttpUtility.UrlEncode(ss_cat);
	if(ra_code != "")
	{
		uri += "&ra="+ ra_code;
		uri += "&id="+ ra_id;
	}
	Response.Write("<table width=100% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["stock_qty"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 20;
	m_cPI.URI = "?ra="+ ra_code +"&s="+sSystem+"&op="+sOption+"&id="+ra_id+"&b="+ m_branch_id +"&cat="+ HttpUtility.UrlEncode(cat) +"&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat="+ HttpUtility.UrlEncode(ss_cat);
	//m_cPI.URI = "?stock.aspx?b="+ m_branch_id +"&cat="+ HttpUtility.UrlEncode(cat) +"&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat="+ HttpUtility.UrlEncode(ss_cat);
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<tr><td colspan=3>");
	Response.Write(sPageIndex);
	
	Response.Write("</td><td align=right colspan=3>");
	if(!DoItemOption())
		return;
	Response.Write("</td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th><a href=" + uri + "&sort=p.code");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by code' class=o>CODE</a></th>");
	Response.Write("<th><a href=" + uri + "&sort=name");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by Description' class=o>DESCRIPTION</a></th>");

	Response.Write("<th><a href=" + uri + "&sort=ep");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by Expire Date' class=o>Expire_Date</a></th>");

	Response.Write("<th><a href=" + uri + "&sort=stock");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by stock' class=o>STOCK</a></th>");
	Response.Write("<th align=right><a href=" + uri + "&sort=sales");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by sales' class=o>SALES</a></th>");
	if(Request.QueryString["ra"] == "code")
		Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");
	
	DataRow dr;
	Boolean alterColor = true;

	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["stock_qty"].Rows[i];
	
		string s_product_code = dr["code"].ToString();
		string s_qty = dr["stock"].ToString();
		string s_description = dr["name"].ToString();
		string sales = dr["sales"].ToString();
		string expire_date = dr["expire_date"].ToString();
		if(expire_date != "")
			expire_date = DateTime.Parse(expire_date).ToString("dd-MM-yyyy");
		
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		alterColor = !alterColor;
		string code = s_product_code;
		Response.Write("<td align=center nowrap><a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
		Response.Write(code);
		Response.Write("</a> ");
	//	if(CheckPatent("viewsales"))
		{
			Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
			Response.Write("code=" + code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
		}
		Response.Write("</td>\r\n");

		Response.Write("<td>" + s_description + "</td>");
		Response.Write("<td>" + expire_date + "</td>");
		
		Response.Write("<td align=center><font color=");
		if(s_qty == "")
			s_qty = "999999";
		int q = MyIntParse(s_qty);
		if(q == 0 )
			Response.Write("black");
		else if(q <0 )
			Response.Write("red");
		else if(q >0 )
			Response.Write("green");
		Response.Write(">" + s_qty + "</font></td>");
		
		Response.Write("<td align=right>" + sales + "</td>");
		if(Request.QueryString["ra"] == "code")
		{
			Response.Write("<td align=right><input type=button name=button value='Add To RA' "+ Session["button_style"] +"");
			Response.Write(" onclick=window.location=('techr.aspx?op="+sOption+"&s="+sSystem+"&id="+ra_id+"&sltcode="+ s_product_code +"') ");
			Response.Write(" ></td>");
		}
		Response.Write("</tr>");
	}
	
	//PrintQtyPageIndex();
	
	//Response.Write("</tr> "); //<tr><td>Page: <a href=stock.aspx?link='"+i+"'>"+(i+1)+"</a></td></tr>");
	Response.Write("</table>");
	
}


</script>

<asp:Label id=LFooter runat=server/>




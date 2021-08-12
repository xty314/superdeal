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
string m_borrow = "0";

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
string m_search = "";
string m_sort = "";
bool m_bDesc = false;
bool m_IsBorrow = false;
bool m_bNoStockBorrowAction = false;
string m_session_uri = "";

string m_last_url = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;
	m_bNoStockBorrowAction = MyBooleanParse(GetSiteSettings("no_stock_borrow_action", "false", true));
	if(Request.QueryString["sort"] != null)
		m_sort = Request.QueryString["sort"];
	if(Request.QueryString["desc"] == "1")
		m_bDesc = true;
	if(Request.QueryString["br"] != null && Request.QueryString["br"] != "")
		m_borrow = Request.QueryString["br"].ToString();

	if(Request.QueryString["op"] != null && Request.QueryString["op"] != "")
		sOption = Request.QueryString["op"].ToString();
	if(Request.QueryString["s"] != null && Request.QueryString["s"] != "")
		sSystem = Request.QueryString["s"].ToString();
	
	if(Request.QueryString["uri"] != null)
	{
		Session["last_uri"] = Request.QueryString["uri"];
	//	DEBUG("last uri = ", Session["last_uri"].ToString());
		if(Request.QueryString["id"] != null && Request.QueryString["ss"] != null)
		{
			Session["cid"] = Request.QueryString["id"];
			Session["nss"] = Request.QueryString["ss"];
		}
	}
	if(Request.Form["cmd"] == "Cancel")
	{
		Session["slt_code"] = null;
		Response.Write("<script language=javascript");
		Response.Write("> window.location=('"+ Session["last_uri"] +"')\r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		return;
	}

	if(m_borrow == "1" && Request.QueryString["code"] != null && Request.QueryString["code"] != "" )
	{
		if(DoAddItemToSession())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?");
			if(m_borrow == "1")
			{
				Response.Write("br=1&");
				if(m_sort != "")
					Response.Write("sort="+m_sort+"&");
			//if(sSystem != "")
			//	Response.Write("&s="+sSystem+"");
			if(Request.QueryString["cat"] != "" && Request.QueryString["cat"] != null)
				Response.Write("cat="+ HttpUtility.UrlEncode(Request.QueryString["cat"].ToString()) +"&");
			if(Request.QueryString["s_cat"] != "" && Request.QueryString["s_cat"] != null)
				Response.Write("s_cat="+ HttpUtility.UrlEncode(Request.QueryString["s_cat"].ToString()) +"&");
			if(Request.QueryString["ss_cat"] != "" && Request.QueryString["ss_cat"] != null)
				Response.Write("ss_cat="+ HttpUtility.UrlEncode(Request.QueryString["ss_cat"].ToString()) +"&");
			}
			Response.Write("')");
			Response.Write("</script");
			Response.Write(">");
			
			return;
		}
	}

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "" && m_borrow != "1")
	{
		
		Session["slt_code"] = Request.QueryString["code"];
		Session["slt_supplier_code"] = Request.QueryString["supp_code"].ToString();
		Session["slt_name"] = Request.QueryString["desc"].ToString();
		//check borrow id is true
		if(Request.QueryString["brid"] != null && Request.QueryString["brid"] != "")
			Session["slt_borrow_id"] = Request.QueryString["brid"].ToString();
		if(Request.QueryString["rbrid"] != null && Request.QueryString["rbrid"] != "")
			Session["slt_rborrow_id"] = Request.QueryString["rbrid"].ToString();
		if(Session["cid"] != null && Session["cid"] != "")
		{
			string cid = Session["cid"].ToString();
			Session["slt_code"+ cid] = Session["slt_code"];
		}
		
		Response.Write("<script language=javascript");
		Response.Write("> window.location=('"+ Session["last_uri"] + "");
		if(Session["nss"] != null)
			Response.Write("&ss="+ Session["nss"] +"");
		Response.Write("')\r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		
		return;
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	if(Session["last_uri"] != null && Session["last_uri"] != "")
		m_session_uri = Session["last_uri"].ToString();

	Response.Write("<br><h3><center>Product Item List</h3></center>");

	GetSearch();
	
	if(Request.Form["cmd"] == "Search Product" || Request.Form["txtSearch"] != null)
	{
		if(Request.Form["txtSearch"] != "" && Request.Form["txtSearch"] != null)
			m_search = Request.Form["txtSearch"].ToString();
	}
	if(!GetStockQty())
		return;
	BindStockQty();
	LFooter.Text = m_sAdminFooter;
}


bool DoAddItemToSession()
{
	string code = Request.QueryString["code"].ToString();

	int nSession = 0;
	bool IsDuplicate = false;
//DEBUG(" code = ", code);	
	if(Session["slt_count"] == null)
	{
		Session["slt_count"] = 1;
		Session["slt_code1"] = code;
		nSession = (int)Session["slt_count"];
//DEBUG("code = ", Session["slt_code1"].ToString());
	}
	else
	{
		nSession = (int)Session["slt_count"] + 1;		
		for(int i=1; i<=nSession; i++)
		{	
			if(Session["slt_code"+ i] != null)
			{
				if(code == Session["slt_code"+ i].ToString())
					IsDuplicate = true;
			}
		}
		if(!IsDuplicate)
		{
			Session["slt_count"] = nSession;
			Session["slt_code"+ nSession] = code;
		//	DEBUG("code = ", Session["slt_code"+ nSession].ToString());	
		//	for(int i=1; i<=nSession; i++)
		//		DEBUG("code = ", Session["slt_code"+ i].ToString());	
		}
		
	//	DEBUG("nSession=", nSession);
	}

	//***********add display message to user
	Response.Write("<script launguage=javascript>window.alert('You Have Borrowed Product_Code: "+ code +"\\n\\nClick OK to Continue.');</script");
	Response.Write(">");
	//****************8
	return true;
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
	Response.Write("Select Catalog: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&s="+sSystem+"&op="+sOption+"&sort="+m_sort+"&id="+ra_id+"&ra="+ra_code+"&qtyp="+qtyp+"");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_borrow == "1")
		Response.Write("&br=1");
	Response.Write("&cat='+this.options[this.selectedIndex].value)\"");
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?ra="+ra_code+"&s="+sSystem+"&op="+sOption+"&sort="+ m_sort +"");
		if(!m_bDesc)
			Response.Write("&desc=1");
		if(m_borrow != "0")
			Response.Write("&br=1");
		Response.Write("&id="+ra_id+"&cat="+cat+"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat='+this.options[this.selectedIndex].value)\"");
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?ra="+ra_code+"&s="+sSystem+"&op="+sOption+"&sort="+ m_sort +"");
		if(!m_bDesc)
			Response.Write("&desc=1");
		if(m_borrow != "0")
			Response.Write("&br=1");
		Response.Write("&id="+ra_id+"&cat="+cat+"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat="+s_cat+"&ss_cat='+this.options[this.selectedIndex].value)\"");
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
	string sc = "";
	//sc += "";
/*	if(g_bRetailVersion)
	{
		sc += " SELECT DISTINCT c.code, CONVERT(varchar(50),c.name) AS name ";
		if(m_session_uri.IndexOf("ra=rp") >= 0 && !m_bNoStockBorrowAction)
			sc += ", sb.qty - sb.return_qty - sb.replace_qty AS qty, sb.borrow_id ";
		else
			sc += ", sq.qty ";
		sc += " , c.supplier_code ";
		sc += " FROM code_relations c ";
		sc += " LEFT OUTER JOIN stock_qty sq ON c.code = sq.code ";
		sc += " LEFT OUTER JOIN stock s ON s.product_code = sq.code AND s.product_code = c.code ";
		
	}
	else
*/
	{
		sc += " SELECT DISTINCT c.code, c.name ";
		if(m_session_uri.IndexOf("ra=rp") >= 0 && !m_bNoStockBorrowAction)
				sc += ", sb.qty - sb.return_qty AS qty, sb.borrow_id ";
		else
		{
			sc += ", (SELECT ISNULL(sum(qty),0) FROM stock_qty WHERE c.code=code AND sq.code = code ";
			if(Session["branch_support"] != null)
				sc += " AND sq.branch_id = "+ Session["branch_id"].ToString() +"";
			sc += " ) AS qty ";
		}
		sc += " , c.supplier_code ";
		sc += " FROM code_relations c JOIN stock_qty sq ON sq.code = c.code ";
	}
//DEBUG("m_session_uri=", m_session_uri);
	if(m_session_uri.IndexOf("ra=rp") >= 0 && !m_bNoStockBorrowAction)
			sc += " JOIN stock_borrow sb ON sb.code = c.code and sb.supplier_code = c.supplier_code ";

	sc += " WHERE 1 = 1 "; 
	if(m_session_uri.IndexOf("ra=rp") >= 0 && !m_bNoStockBorrowAction)
	{
		sc += " AND sb.qty - sb.return_qty - sb.replace_qty > 0 AND sb.approved = 1 ";
		sc += " AND sb.borrower_id = "+ Session["card_id"] + "";
	}
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "all" && Request.QueryString["cat"] != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc += " AND c.cat = '"+ cat +"' ";
	}
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "all" && Request.QueryString["s_cat"] != "")
	{
		s_cat = Request.QueryString["s_cat"].ToString();
		sc += " AND c.s_cat = '"+ s_cat +"' ";
	}

	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "all" && Request.QueryString["ss_cat"] != "")
	{
		ss_cat = Request.QueryString["ss_cat"].ToString();
		sc += " AND c.ss_cat = '"+ ss_cat +"' ";
	}
	if(Session["branch_support"] != null)
				sc += " AND sq.branch_id = "+ Session["branch_id"].ToString() +"";
	if(Request.QueryString["br"] == "1")
	{
	//	if(g_bRetailVersion)
			sc += " AND sq.qty > 0 ";
	//	else
	//		sc += " AND p.stock > 0 ";
	}

	if(m_search != "")
	{
		m_search = EncodeQuote(m_search).ToLower();
		if(TSIsDigit(m_search))
		{
			sc += " AND (c.code = "+ m_search +" OR c.barcode = '"+ m_search +"' OR c.supplier_code = '"+ m_search +"' )";
		}
		else
		{
			sc += " AND (c.name LIKE '%"+ m_search +"%' OR c.supplier_code LIKE '%" + m_search +"%' )"; // OR c.barcode = '"+ m_search +"')";					
		}
/*		if(Request.Form["filter"] == "barcode")
		{
			sc += " AND (c.barcode = '"+ m_search +"') ";
		}
		else
		{
			if(TSIsDigit(m_search))
				sc += " AND (c.code = "+ m_search +") "; // OR c.barcode = '"+ m_search +"')";
			else
			{
				m_search = EncodeQuote(m_search);			
				sc += " AND (c.name LIKE '%"+ m_search +"%' OR c.supplier_code LIKE '%" + m_search +"%' )"; // OR c.barcode = '"+ m_search +"')";					
			}	
		}
*/
	}
	if(m_sort != "")
	{
		sc += " ORDER BY " + Request.QueryString["sort"];
		if(m_bDesc)
			sc += " DESC ";
	}
	int rows = 0;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "stock_qty");
		if(TSIsDigit(m_search) && rows <=0)
		{
			sc = " SELECT DISTINCT c.code, CONVERT(varchar(50),c.name) AS name , rs.id AS rborrow_id, rs.id AS borrow_id ";
			sc += " , c.supplier_code, (select count(code) from rma_stock rs1 WHERE rs1.id = rs.id ) AS qty  ";
			sc += " FROM code_relations c JOIN rma_stock rs ON rs.code = c.code ";
			
				sc += " WHERE (c.code = '"+ m_search +"') "; 
//DEBUG(" sc 2= ", sc);
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				rows = myAdapter.Fill(dst, "stock_qty");
			}
			catch(Exception e)
			{
				ShowExp(sc, e);
				return false;
			}
		}

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoDeleteProduct(string s_id)
{
	return false; //no delete here DW.
	
	string sc = " DELETE FROM stock ";
	sc += " WHERE id = "+ s_id +"";
	
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	return true;
}


void GetSearch()
{

	//Response.Write("<form name=frmSearchProduct method=post action="+ Request.ServerVariables["URL"] +"?br=1&b="+m_branch_id+"&search="+m_last_search+"&uri="+ HttpUtility.UrlEncode(Request.QueryString["uri"]) +">");
	Response.Write("<form name=frmSearchProduct method=post >");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td>Search Item:</td><td><input type=text name=txtSearch >");
	//Response.Write("<select name=filter><option value=code>All</option><option value=barcode>Barcode</opiton></select></td>");
	Response.Write("</td>");
	
	//Response.Write("<tr><td>Search by Serial Number:</td>");
	//Response.Write("<td><input type=text name=txtSearchSN >");
	//Response.Write("<td><input type=text name=txtSearchSN  onchange='checkform();'>");
	
	//Response.Write("<td><input type=submit name=cmd value='Search Product' "+Session["button_style"] +" onclick='return checkform();'></td></tr>");
	Response.Write("<td><input type=submit name=cmd value='Search Product' "+Session["button_style"] +" >");
//	if(m_session_uri.IndexOf("ra=rp") >= 0)
	Response.Write("<td><input type=submit name=cmd value='Cancel' "+Session["button_style"] +" >");
	if(m_borrow == "1")
	{
		Response.Write("<input type=button value='View Borrow List' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('stk_borrow.aspx')\" ");
		Response.Write(" >");
	}
	Response.Write("</td></tr>");
	
	//Response.Write("<tr><td>Branch :</td><td>");
	//PrintBranchNameOptionsWithOnChange();
	Response.Write("</td></tr>");

	Response.Write("\r\n<script");
	Response.Write(">\r\n document.frmSearchProduct.txtSearch.focus()\r\n</script");
	Response.Write(">\r\n ");
	
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	//Response.Write("</table>");
	Response.Write("</form>");
}

void BindStockQty()
{
	string cat = "", s_cat = "", ss_cat = "";
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"].ToString();
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
		s_cat = Request.QueryString["s_cat"].ToString();
	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
		ss_cat = Request.QueryString["ss_cat"].ToString();
	
	Response.Write("<form method=post name=frm>");
	string uri = "?cat=" + HttpUtility.UrlEncode(cat) + "&s_cat=" + HttpUtility.UrlEncode(s_cat) +"&ss_cat=" + HttpUtility.UrlEncode(ss_cat);
	if(m_borrow != "" && m_borrow != null)
		uri += "&br="+ m_borrow;
	Response.Write("<table align=center width=90% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<br><hr size=1 color=black>");
	
		//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["stock_qty"].Rows.Count;
	if(rows == 0)
	{
		Response.Write("<script Language=javascript");
		Response.Write(">\r\n");
		Response.Write("window.alert('Nothing Found!!')\r\n");
		
		/*Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?"); //\">");
		if(m_sort != null && m_sort != "")
			Response.Write("&sort="+ m_sort +"");
		if(m_borrow != null && m_borrow != "")
			Response.Write("&br="+ m_borrow +"");
		if(sOption != null && sOption != "")
			Response.Write("&op="+ sOption +"");
		if(sSystem != null && sSystem != "")
			Response.Write("&s="+ sSystem +"");
		
		Response.Write("\">");
		*/
		Response.Write("</script");
		Response.Write(">\r\n ");
		Session["last_uri"] = null;
		if(m_session_uri.IndexOf("ra=rp") >= 0)
		{
			Response.Write("<center><input type=button value='Borrow Item from Stock'"+ Session["button_style"] +" ");
			Response.Write(" onclick=\"window.location=('stk_borrow.aspx?r="+ DateTime.Now.ToOADate() +"')\" >");
		}

		return;
	}
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 20;
	
	m_cPI.URI = "?r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&ra="+ ra_code +"&s="+sSystem+"&op="+sOption+"&sort="+ m_sort +"&id="+ra_id+"&b="+ m_branch_id +"&cat="+ HttpUtility.UrlEncode(cat) +"&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat="+ HttpUtility.UrlEncode(ss_cat);
	if(!m_bDesc)
		m_cPI.URI +="&desc=1";
	if(m_borrow == "1")
		m_cPI.URI += "&br=1";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	
	Response.Write("<tr><td colspan=2>");
	Response.Write(sPageIndex);
	Response.Write("</td><td align=right colspan=5>");
	if(!DoItemOption())
		return;
	Response.Write("</td></tr>");
	
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th>");
//	DEBUG("last uri =", Session["last_uri"].ToString());
	
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=");
	Response.Write("c.code");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by code' class=o>");
	}
	
	Response.Write("CODE</a></th>");
	
	Response.Write("<th>");
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=");
	Response.Write("c.supplier_code");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by supplier code' class=o>");
	}
	
	Response.Write("SUPP_CODE</a></th>");

	if(m_session_uri.IndexOf("ra=rp") >= 0)
	Response.Write("<th align=left>BORROW#</th>");
	
	Response.Write("<th>");
	
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=c.name");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by Description' class=o>");
	}
	
	Response.Write("DESCRIPTION</a></th>");
	Response.Write("<th>");
	
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=sq.qty");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by stock' class=o>");
	}
	
	Response.Write("STOCK</a></th>");
	//Response.Write("<th><a href=" + uri + "&b="+ m_branch_id + "&sort=sq.allocated_stock");
	//if(!m_bDesc)
	//	Response.Write("&desc=1");
	//Response.Write(" title='Click to sort by allocated stock' class=o>Allocated</a></th>");
	//Response.Write("<th align=right><a href=" + uri + "&sort=sales");
	/*Response.Write("<th align=right><a href=" + uri + "&b="+ m_branch_id + "&sort=sales");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by sales' class=o>Sales</a></th>");
	Response.Write("<th align=right><a href=" + uri + "&b="+ m_branch_id + "&sort=cost");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by cost' class=o>Cost</a></th>");
	*/
	Response.Write("<th>&nbsp;</th>");

	Response.Write("</tr>");
	
	bool bAlt = true;

	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	
	//for(int i=startPage; i<dst.Tables["stock_qty"].Rows.Count; i++)
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["stock_qty"].Rows[i];
		//string s_allocated_stock = dr["allocated_stock"].ToString();
		string s_product_code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string s_qty = dr["qty"].ToString();
		string s_description = dr["name"].ToString();
		string borrow_id = "";
		string rborrow_id = "";
		if(m_session_uri.IndexOf("ra=rp") >= 0 && !m_bNoStockBorrowAction)
			borrow_id = dr["borrow_id"].ToString();
		if(borrow_id == "0")
			rborrow_id = dr["rborrow_id"].ToString();
		//string cost = dr["cost"].ToString();
		//string sales = dr["sales"].ToString();
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		alterColor = !alterColor;
		Response.Write("<td align=center><b>"+s_product_code+"</b></td></td>");
		Response.Write("<td align=left><b>"+supplier_code+"</b></td></td>");
		if(m_session_uri.IndexOf("ra=rp") >= 0)
			Response.Write("<td>"+ borrow_id+"</td>");
		Response.Write("<td>"+s_description+"</td>");
//	DEBUG(" sqty = ", s_qty);	
		Response.Write("<td align=center><font color=");
	
		if(s_qty == "")
			s_qty = "0";
		int q = MyIntParse(s_qty);
		if(q == 0 )
			Response.Write("black");
		else if(q <0 )
			Response.Write("red");
		else if(q >0 )
			Response.Write("green");
		Response.Write(">" + s_qty + "</font></td>");
		//Response.Write("<td align=center>");
		//Response.Write(s_allocated_stock + "</td>");
		//Response.Write("<td align=right>" + sales + "</td>");
		//Response.Write("<td align=right>"+(double.Parse(cost)).ToString("c")+"</td>");
		/*if(Request.QueryString["br"] == "1")
		{
			Response.Write("<td align=right><input type=submit name=cmd value='Check This Item' "+ Session["button_style"] +"");
			Response.Write(" ></td>");
		}
		else
		*/
		//{
			Response.Write("<td align=right>");
			Response.Write("<input type=button name=button value='");
			if(m_borrow == "1")
				Response.Write("BORROW ");
			else
				Response.Write("Select ");
			Response.Write("This' "+ Session["button_style"] +" ");
			Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?code="+ HttpUtility.UrlEncode(s_product_code) +"");
			if(m_borrow == "1")
			{
				Response.Write("&br=1");
				if(sOption != "")
					Response.Write("&op="+sOption+"");
				if(m_sort != "")
					Response.Write("&sort="+m_sort+"");
				if(sSystem != "")
					Response.Write("&s="+sSystem+"");
				if(Request.QueryString["cat"] != "" && Request.QueryString["cat"] != null)
					Response.Write("&cat="+ HttpUtility.UrlEncode(Request.QueryString["cat"].ToString()) +"");
				if(Request.QueryString["s_cat"] != "" && Request.QueryString["s_cat"] != null)
					Response.Write("&s_cat="+ HttpUtility.UrlEncode(Request.QueryString["s_cat"].ToString()) +"");
				if(Request.QueryString["ss_cat"] != "" && Request.QueryString["ss_cat"] != null)
					Response.Write("&ss_cat="+ HttpUtility.UrlEncode(Request.QueryString["ss_cat"].ToString()) +"");
			}
			//if(m_session_uri.IndexOf("ra=rp") >= 0)
			if(borrow_id != "")
				Response.Write("&brid="+ borrow_id +"");
			if(rborrow_id != "")
				Response.Write("&rbrid="+ rborrow_id +"");
			Response.Write("&supp_code="+ HttpUtility.UrlEncode(supplier_code) +"&desc="+ HttpUtility.UrlEncode(s_description) +"");
			Response.Write("')\"");
			
			Response.Write(" ></td>");
		//}
		
		Response.Write("</tr>");
	}
	
	Response.Write("</table>");
	Response.Write("</form>");

}


</script>

<asp:Label id=LFooter runat=server/>




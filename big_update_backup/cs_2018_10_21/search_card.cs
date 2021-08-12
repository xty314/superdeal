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
	
	if(Request.Form["cmd"] == "close")
	{	
		Response.Write("<script language=javascript");
		Response.Write("> window.close(); \r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		return;
	}
	
	Response.Write("<br><h3><center>Card List</h3></center>");

	GetSearch();
	
	if(Request.Form["cmd"] == "Search Card" || Request.Form["txtSearch"] != null)
	{
		if(Request.Form["txtSearch"] != "" && Request.Form["txtSearch"] != null)
			m_search = Request.Form["txtSearch"].ToString();
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
	Response.Write("Select Catalog: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&s="+sSystem+"&op="+sOption+"&sort="+m_sort+"&id="+ra_id+"&ra="+ra_code+"&qtyp="+qtyp+"");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_borrow == "1")
		Response.Write("&br=1");
	Response.Write("&cat='+ escape(this.options[this.selectedIndex].value))\"");
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
		Response.Write("&id="+ra_id+"&cat="+ HttpUtility.UrlEncode(cat) +"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat='+ escape(this.options[this.selectedIndex].value))\"");
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
		Response.Write("&id="+ra_id+"&cat="+ HttpUtility.UrlEncode(cat) +"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat='+escape(this.options[this.selectedIndex].value))\"");
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
	sc += " SELECT * FROM card c ";

	if(m_search != "")
	{
		m_search = EncodeQuote(m_search).ToLower();
		if(TSIsDigit(m_search))
		{
			if(m_search.Length <=12)
				sc += " AND (c.id = "+ m_search +" OR c.barcode = '"+ m_search +"' ) ";
			else
				sc += " AND ( c.barcode = '"+ m_search +"' )";
		}
		else
		{
			sc += " AND (c.name LIKE '%"+ m_search +"%' OR c.trading_name LIKE '%" + m_search +"%' OR c.company LIKE '%" + m_search +"%' OR c.phone LIKE '%" + m_search +"%' OR c.email LIKE '%" + m_search +"%')"; // OR c.barcode = '"+ m_search +"')";					
		}

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
		rows = myAdapter.Fill(dst, "search_card");		

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
	Response.Write("<tr><td>Search Card (name/trading name/id/phone):</td><td><input type=text name=txtSearch >");
	//Response.Write("<select name=filter><option value=code>All</option><option value=barcode>Barcode</opiton></select></td>");
	Response.Write("</td>");
	
	//Response.Write("<tr><td>Search by Serial Number:</td>");
	//Response.Write("<td><input type=text name=txtSearchSN >");
	//Response.Write("<td><input type=text name=txtSearchSN  onchange='checkform();'>");
	
	//Response.Write("<td><input type=submit name=cmd value='Search Card' "+Session["button_style"] +" onclick='return checkform();'></td></tr>");
	Response.Write("<td><input type=submit name=cmd value='Search Card' "+Session["button_style"] +" >");
//	if(m_session_uri.IndexOf("ra=rp") >= 0)
	Response.Write("<td><input type=submit name=cmd value=' X ' "+Session["button_style"] +" onclick=\" window.close(); \" >");

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

	Response.Write("<form method=post name=frm cellspacing=0 cellpadding=0>");
	string uri = "?cat=" + HttpUtility.UrlEncode(cat) + "&s_cat=" + HttpUtility.UrlEncode(s_cat) +"&ss_cat=" + HttpUtility.UrlEncode(ss_cat);
	if(m_borrow != "" && m_borrow != null)
		uri += "&br="+ m_borrow;
	Response.Write("<table align=center width=60% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<br><hr size=1 color=black>");
	
		//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["search_card"].Rows.Count;
	if(rows == 0)
	{
		Response.Write("<script Language=javascript");
		Response.Write(">\r\n");
		Response.Write("window.alert('Nothing Found!!')\r\n");
		
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
//	if(!DoItemOption())
//		return;
	Response.Write("</td></tr>");
	
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th>");
//	DEBUG("last uri =", Session["last_uri"].ToString());
	
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=");
	Response.Write("c.id");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by id' class=o>");
	}
	
	Response.Write("ID#</a></th>");
	
	Response.Write("<th align=left>");
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=");
	Response.Write("c.trading_name");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by trading name' class=o>");
	}
	
	Response.Write("TRADING NAME</a></th>");

	Response.Write("<th align=left>");
	
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=c.name");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by name' class=o>");
	}
	
	Response.Write("NAME</a></th>");
/*	Response.Write("<th>");
	
	if(m_session_uri.IndexOf("ra=rp") < 0)
	{
	Response.Write("<a href=" + uri + "&b="+ m_branch_id + "&sort=sq.qty");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by stock' class=o>");
	}
	
	Response.Write("STOCK</a></th>");
*/
	Response.Write("<th>&nbsp;</th>");

	Response.Write("</tr>");
	
	bool bAlt = true;

	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	
	//for(int i=startPage; i<dst.Tables["search_card"].Rows.Count; i++)
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["search_card"].Rows[i];
		//string s_allocated_stock = dr["allocated_stock"].ToString();
		string card_id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		
		string name = dr["name"].ToString();
		
		
		//string cost = dr["cost"].ToString();
		//string sales = dr["sales"].ToString();
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		alterColor = !alterColor;
		Response.Write("<td align=center><b>"+card_id+"</b></td></td>");
		Response.Write("<td align=left><b>"+trading_name+"</b></td></td>");		
		Response.Write("<td>"+name+"</td>");

			Response.Write("<td align=right>");
			Response.Write("<input type=button name=button value='");			
			Response.Write("Select This'");
			Response.Write(" "+ Session["button_style"] +" ");
			Response.Write(" onclick=\" getProductCode("+ card_id +"); \" ");
			Response.Write(" ></td>");
		//}
		
		Response.Write("</tr>");
	}

	Response.Write("<script language='javascript' type='text/javascript' >");
	
	Response.Write(" function getProductCode(code){  ");
	Response.Write(" var fieldName = window.name; ");
	//Response.Write(" window.alert(fieldName); ");
	//Response.Write(" window.alert(eval(\"opener.document.f.\" + fieldName + \".value\")); ");
	Response.Write(" eval(\"opener.document.f.\" + fieldName + \".value=code\"); window.close(); ");
	Response.Write(" } ");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("</table>");
	Response.Write("</form>");

}


</script>

<asp:Label id=LFooter runat=server/>




	

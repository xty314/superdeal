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

string m_branchid = "1";
string m_sort = "";
bool m_bDesc = false;
bool m_ballbranchs = false;
string tableWidth = "97%";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Session["branch_support"] != null)
	{
		if(Request.QueryString["b"] != null && Request.QueryString["b"] !="")
		{
			m_branchid = Request.QueryString["b"];
			if(m_branchid == "all")
				m_ballbranchs = true;
		}
		else if(Session["branch_id"] != null)
		{
			m_branchid = Session["branch_id"].ToString();
		}
		if(Request.Form["branch"] != null && Request.Form["branch"] != "")
			m_branchid = Request.Form["branch"];
	}
	
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

//	Response.Write("<h3><center>STOCK LIST</h3></center>");
	Response.Write("<form name=frmSearchProduct method=post action=stock.aspx?search="+m_last_search+">");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>Stock List</font></font>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");		

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
			else if(sSearchSN != "")
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
	
	if(Request.Form["cmd"] == "Update Product")
	{	
		if(Request.Form["txtSN"] != null)
			m_sn = Request.Form["txtSN"].ToString();
		if(Request.Form["txtProd"] != null)
			m_product_code = Request.Form["txtProd"].ToString();
		if(Request.Form["txtCost"] != null)
			m_cost = Request.Form["txtCost"].ToString();
		if(Request.Form["txtProd"] != null)
			m_purchase_date = Request.Form["txtPurDate"].ToString();
		if(Request.Form["txtProd"] != null)
			m_branch_id = Request.Form["txtBranchID"].ToString();
		if(Request.Form["txtProd"] != null)
			m_po_number = Request.Form["txtPOnum"].ToString();
		if(Request.Form["txtProd"] != null)
			m_supplier = Request.Form["txtSupp"].ToString();
		if(Request.Form["txtProd"] != null)
			m_supplier_code = Request.Form["txtSuppCode"].ToString();
		if(Request.Form["txtProd"] != null)
			m_status = Request.Form["txtStatus"].ToString();
	
		if(!UpdateProduct())
			return;
	}
	if(Request.QueryString["del"] != null)
	{
		string s_id = Request.QueryString["del"].ToString();
		if(!DoDeleteProduct(s_id))
			return;
	}
		
	/*if(Request.QueryString["edit"] != null || Request.QueryString["id"] != null)
	{
		if(Request.QueryString["i"] != null)
			m_snQuery = Request.QueryString["i"].ToString();
		
		if(!GetEditProduct(m_snQuery))
			return;
		DisplayEditProduct();
	}
	*/
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
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&s="+sSystem+"&op="+sOption+"");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write("&id="+ra_id+"&ra="+ra_code+"&qtyp="+qtyp+"&cat='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		if(cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
		else
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");

	}

	Response.Write("</select>");
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
	{
		cat = Request.QueryString["cat"].ToString();
	
		sc = "SELECT DISTINCT s_cat FROM product  WHERE cat = N'"+ cat +"' ";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?ra="+ra_code+"&s="+sSystem+"");
		if(m_branchid != "")
			Response.Write("&b="+ m_branchid +"");
		Response.Write("&op="+sOption+"&id="+ra_id+"&cat="+HttpUtility.UrlEncode(cat)+"&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat='+this.options[this.selectedIndex].value)\"");
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
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
		}

		Response.Write("</select>");
	}

	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "")
	{
		s_cat = Request.QueryString["s_cat"].ToString();
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM product p WHERE cat = N'" + cat + "' ";
		sc += " AND s_cat = N'" + s_cat + "' ";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?ra="+ra_code+"");
		if(m_branchid != "")
			Response.Write("&b="+ m_branchid +"");
		Response.Write("&s="+sSystem+"&op="+sOption+"&id="+ra_id+"&cat=");
		Response.Write(HttpUtility.UrlEncode(cat) + "&r=" + DateTime.Now.ToOADate() + "&qtyp="+qtyp+"&s_cat=");
		Response.Write(HttpUtility.UrlEncode(s_cat) + "&ss_cat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "")
			ss_cat = Request.QueryString["ss_cat"].ToString();
		
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(ss_cat.ToUpper() == s.ToUpper())
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
		}

		Response.Write("</select>");
	}
	return true;
}


bool GetStockQty()
{
	string sc = " SELECT p.supplier_code, p.code, CONVERT(varchar(60), p.name) AS name, p.barcode ";
	sc += ", ISNULL((SELECT SUM(s.quantity) FROM sales s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " WHERE s.code = p.code AND sq.branch_id = i.branch ";	
	sc += "), 0) AS sales ";

//	if(g_bRetailVersion)
		sc += ", ISNULL(sq.qty,0) AS stock , br.name AS branch_name ";
//	else
//		sc += ", p.stock ";
//	sc += " FROM product p ";
	sc += " FROM code_relations p ";
//	if(g_bRetailVersion)
	{
		sc += " JOIN stock_qty sq ON sq.code = p.code ";
		sc += " JOIN branch br ON br.id = sq.branch_id AND br.activated = 1 ";
	}
	
	if(ra_code != "" && ra_code != null)
	{
		sc += " JOIN stock_borrow b ON b.code = p.code ";
	}
	sc += " WHERE 1=1 ";
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "all" && Request.QueryString["cat"] != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc += " AND p.cat = N'" + cat + "' ";
	}
	if(Request.QueryString["s_cat"] != null && Request.QueryString["s_cat"] != "all" && Request.QueryString["s_cat"] != "")
	{
		s_cat = Request.QueryString["s_cat"].ToString();
		sc += " AND p.s_cat = N'" + s_cat + "' ";
	}

	if(Request.QueryString["ss_cat"] != null && Request.QueryString["ss_cat"] != "all" && Request.QueryString["ss_cat"] != "")
	{
		ss_cat = Request.QueryString["ss_cat"].ToString();
		sc += " AND p.ss_cat = N'" + ss_cat + "' ";
	}
//	if(Request.QueryString["ra"] == "code")
//	{
//		sc += " AND p.stock > 0 ";
//	}
	if(m_branchid != "" && m_branchid != "all")
	{
//		if(g_bRetailVersion)
			sc += " AND sq.branch_id = "+ m_branchid +" ";
	}
	sc += " ORDER BY ";
	
	if(m_sort != "")
	{
		//sc += " ORDER BY " + Request.QueryString["sort"];
//		if(Request.QueryString["sort"] == "barcode" )
//			sc += " CAST( ";
		sc += Request.QueryString["sort"];
//		if(Request.QueryString["sort"] == "barcode" )
//			sc += " AS float(16)) ";
		if(m_bDesc)
			sc += " DESC ";
	}
	else if(Request.QueryString["cat"] == "all" || (Request.QueryString["cat"] != null && Request.QueryString["s_cat"] == "all" )
		|| (Request.QueryString["cat"] != null && Request.QueryString["cat"] != null && Request.QueryString["ss_cat"] == "all"))
	{
		sc += " p.cat, p.s_cat, p.ss_cat, p.brand, p.name, p.code ";	
	}
	else
	{
//		sc += " CAST(p.barcode AS float(16)) ";
		sc += " p.barcode ";
	}
//DEBUG("sc=", sc);
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
			/*if(!IsNumberic(document.frmSearchProduct.txtSearch.value)){
				//window.alert('Please Enter Number Only!!');
				document.frmSearchProduct.txtSearch.focus();
				document.frmSearchProduct.txtSearch.select();
				return false;
			}
			*/
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
	
	//Response.Write("<table border=2>");
//	Response.Write("<table cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<table border=0 align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=2><br></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td width=15%>Search by BarCode/Code/MPN:</td><td><input type=text name=txtSearch value='"+ Request.Form["txtSearch"] +"'></td>");
	
	//Response.Write("<input type=submit name=cmd value='Search Product Code' OnClick='return checkform();'></td></tr>");
	
	//Response.Write("<td>&nbsp;</td><td>Query Items in Stock by Product Code:<input type=text name=txtQuery value='' onchange='queryitem();'></td>");
	//Response.Write("<td><input type=submit name=cmdQuery value='Query Item' disabled></td></tr>");
	Response.Write("<tr><td>Search by Serial Number:</td>");
	Response.Write("<td><input type=text name=txtSearchSN value="+ Request.Form["txtSearchSN"] +">");
	//Response.Write("<td><input type=text name=txtSearchSN  onchange='checkform();'>");
	
	Response.Write("<input type=submit name=cmdUpdate value='Search Product' "+ Session["button_style"] +" ");
	Response.Write(" onclick='return checkform();'></td></tr>");

	//branch option
	Response.Write("<tr>");

	if(Session["branch_support"] != null)
	{
		Response.Write("<td><b>Branch :</b></td><td align=left>");
		PrintBranchNameOptionsWithOnChange();
		Response.Write(" <input type=button  value='Print Product List' "+ Session["button_style"] +" onclick=\"window.location=('stocktake.aspx?pr=3&br='+document.frmSearchProduct.branch.selectedIndex)\"> ");
		Response.Write("</td>");
	}
	Response.Write("</tr>");
	Response.Write("\r\n<script");
	Response.Write(">\r\n document.frmSearchProduct.txtSearch.focus()\r\n</script");
	Response.Write(">\r\n ");
	
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

private bool PrintBranchNameOptionsWithOnChange()	//Herman: 21/03/03
{
	DataSet dsBranch = new DataSet();
	string sBranchID = "1";
	int rows = 0;
/*	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
	{
		sBranchID = Request.QueryString["b"];
		if(sBranchID != "all")
			Session["branch_id"] = MyIntParse(sBranchID); //Session["branch_id"] is integer
	}
	else if(Session["branch_id"] != null)
	{
		sBranchID = Session["branch_id"].ToString();
	}
*/
	//do search
	string sc = "SELECT id, name FROM branch WHERE activated = 1 ";
    if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	    sc += " AND id ="+ Session["branch_id"].ToString() +" ";
    sc += " ORDER BY id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsBranch, "branch");
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=branch");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?b=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
    if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
    {
	    Response.Write("<option value='all'");
	    if(m_ballbranchs)
		    Response.Write("selected");
			
	    Response.Write("> All Branches</option>");
    }
	for(int i=0; i<rows; i++)
	{
		string bname = dsBranch.Tables["branch"].Rows[i]["name"].ToString();
		int bid = int.Parse(dsBranch.Tables["branch"].Rows[i]["id"].ToString());
		Response.Write("<option value='" + bid + "' ");
		if(IsInteger(m_branchid))
		{
			if(bid == int.Parse(m_branchid))
				Response.Write("selected");
		}
		Response.Write(">" + bname + "</option>");
	}

	if(rows == 0)
		Response.Write("<option value=1>Branch 1</option>");
	Response.Write("</select>");
	return true;
}


bool GetEditProduct(string s_id)
{
	string sc = " SELECT * FROM stock ";
	sc += " WHERE id = '"+ s_id + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "editProduct");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;

}

bool UpdateProduct()
{
	m_snQuery = Request.QueryString["i"].ToString();
	
	string sc = "set DATEFORMAT dmy ";
	sc += " UPDATE stock  ";
	sc += " SET sn = '"+ m_sn +"', product_code = '"+m_product_code+"', branch_id = "+ m_branch_id+", ";
	sc += " cost = "+ m_cost +", purchase_date = '"+ m_purchase_date +"', po_number = '"+ m_po_number +"', ";
	sc += " supplier = '"+ m_supplier +"', supplier_code = '"+ m_supplier_code +"', status = "+m_status+"";
	sc += " WHERE id = "+m_snQuery+"";
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

void DisplayEditProduct()
{
	Response.Write("<form name=frmEditProduct method=post action='stock.aspx?success=d&i="+m_snQuery+"' >");
	Response.Write("<table width=100%  align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td colspan=2 align=center><font size=1><b>Edit Current Product</b></font></td><td>&nbsp;</td></tr>");
	for(int i=0; i<dst.Tables["editProduct"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["editProduct"].Rows[i];

		m_sn = dr["sn"].ToString();
		m_product_code = dr["product_code"].ToString();
		m_cost = dr["cost"].ToString();
		m_purchase_date = dr["purchase_date"].ToString();
		m_branch_id = dr["branch_id"].ToString();
		m_po_number = dr["po_number"].ToString();
		m_supplier = dr["supplier"].ToString();
		m_supplier_code = dr["supplier_code"].ToString();
		m_status = dr["status"].ToString();
		
	}
	Response.Write("<tr><td align=right>Serial Number :</td><td><input type=text name=txtSN value='"+m_sn+"'></td></tr>");
	Response.Write("<tr><td align=right>Product Code :</td><td><input type=text name=txtProd value='"+m_product_code+"'></td></tr>");
	Response.Write("<tr><td align=right>Cost :</td><td><input type=text name=txtCost value='"+m_cost+"'></td></tr>");
	Response.Write("<tr><td align=right>Purchase Date :</td><td><input type=text name=txtPurDate value='"+m_purchase_date+"'></td></tr>");
	Response.Write("<tr><td align=right>Branch ID :</td><td><input type=text name=txtBranchID value='"+m_branch_id+"'></td></tr>");
	Response.Write("<tr><td align=right>PO Number :</td><td><input type=text name=txtPOnum value='"+m_po_number+"'></td></tr>");
	Response.Write("<tr><td align=right>Supplier :</td><td><input type=text name=txtSupp value='"+m_supplier+"'></td></tr>");
	Response.Write("<tr><td align=right>Supplier Code :</td><td><input type=text name=txtSuppCode value='"+m_supplier_code+"'></td></tr>");
	Response.Write("<tr><td align=right>Status :</td> ");
	Response.Write("<td><select name=txtStatus> ");
	if(m_status == "1")
	{
		Response.Write("<option value=1 selected>Sold</optiokn><option value=2>In Stock</option> ");
		Response.Write("<option value=3>Lost</option><option value=4>RMA</option></select></td></tr>");
	}
	else if(m_status == "2")
	{
		Response.Write("<option value=1>Sold</optiokn><option value=2 selected>In Stock</option> ");
		Response.Write("<option value=3>Lost</option><option value=4>RMA</option></select></td></tr>");
	}
	else if(m_status == "3")
	{
		Response.Write("<option value=1>Sold</optiokn><option value=2>In Stock</option> ");
		Response.Write("<option value=3 selected>Lost</option><option value=4>RMA</option></select></td></tr>");
	}
	else if(m_status == "4")
	{
		Response.Write("<option value=1 selected>Sold</optiokn><option value=2>In Stock</option> ");
		Response.Write("<option value=3>Lost</option><option value=4 selected>RMA</option></select></td></tr>");
	}
	else
	{
		Response.Write("<option value=1 selected>Sold</optiokn><option value=2>In Stock</option> ");
		Response.Write("<option value=3>Lost</option><option value=4 selected>RMA</option></select></td></tr>");
	}
	//Response.Write("<td><input type=text name=txtStatus value='"+m_status+"'></td></tr>");
	
	Response.Write("<tr><td align=right><input type=submit name=cmd value='Update Product'></td></tr> ");
	Response.Write("</table></form>");
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
//DEBUG("g_bRetailVersion =", g_bRetailVersion.ToString());
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
		sc = " SELECT DISTINCT st.sn, pi.code, c.supplier_code,  CONVERT(varchar(60), pi.name) AS name ";
		if(g_bRetailVersion)
			sc += ", ISNULL(sq.qty,0) AS qty, br.name AS branch_name ";
		else
			sc += ", pd.stock AS qty ";
		sc += ", c.price1 AS price ";
		//sc += " FROM serial_trace st LEFT OUTER JOIN ";
		sc += " FROM stock st ";
		sc += " JOIN code_relations c ON c.code = st.product_code ";
		sc += " JOIN purchase p ON p.id = st.purchase_order_id ";
		sc += " JOIN purchase_item pi ON st.purchase_order_id = pi.id AND pi.id = p.id ";
		sc += " JOIN product pd ON pd.code = pi.code AND st.product_code = pi.code AND st.product_code = pd.code ";
//		sc += " JOIN code_relations c ON c.code = pd.code AND c.code = pi.code AND c.code = st.product_code ";
//		if(g_bRetailVersion)
		{
			sc += " JOIN stock_qty sq ON sq.code = pd.code AND pi.code = sq.code ";
			sc += " JOIN branch br ON br.id = sq.branch_id AND br.activated = 1 ";
		}
		sc += " WHERE (st.sn = '"+sSearchSN+"' ) ";
		
		if(m_branchid != "" && m_branchid != "all")
		{
			if(g_bRetailVersion)
				sc += " AND sq.branch_id = "+ m_branchid +" ";
		}
		
		sc += " ORDER BY st.sn DESC ";
	//}
//DEBUG("sc = ", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int i = myAdapter.Fill(dst,"snQty");
		//DEBUG( "i = ", i );

	}
	catch(Exception e)
	{
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
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
	bool bIsInt = false;
	try
	{
		string stemp = int.Parse(sSearch).ToString();
		bIsInt = true;
	}
	catch(Exception e)
	{

	}
	
	string sc = " SELECT p.supplier + p.supplier_code AS ID, p.supplier_code, p.code, c.barcode ";
	if(g_bRetailVersion)
		sc += ", ISNULL(sq.qty,0) AS qty , br.name AS branch_name ";
	else
		sc += " , p.stock AS qty ";
	sc += " , CONVERT(varchar(50),p.name) AS name, c.price1 AS price ";
	sc += " FROM product p JOIN code_relations c ON c.code = p.code ";
//	if(g_bRetailVersion)
	{
		sc += " JOIN stock_qty sq ON sq.code = p.code ";
		sc += " JOIN branch br ON br.id = sq.branch_id AND br.activated = 1 ";
        if(m_branchid != "" && m_branchid != "all")
            sc += " AND br.id = "+ m_branchid +" ";
	}
	sc += " LEFT OUTER JOIN barcode b ON b.item_code = p.code";

	if(sSearch != "")
	{
//		sc += " WHERE p.code = "+sSearch+"";
		//if(TSIsDigit(sSearch))
		if(bIsInt)
		{
			sc += " WHERE (c.barcode = '" + sSearch + "' OR c.supplier_code = '"+ sSearch +"' ";
			sc += " OR b.barcode = '"+ sSearch + "'";
			if(sSearch.Length < 9)
				 sc += " OR c.code = '"+ sSearch +"' ";
			sc += " ) ";				
		}
		else
		{
			sc += " WHERE (c.supplier_code = '" + sSearch + "' OR c.barcode = '" + sSearch + "' OR b.barcode = '"+ sSearch + "'";
			sc += "	OR c.name LIKE N'%"+sSearch+"%' ) ";
		}
	}
	
	if(m_branchid != "" && m_branchid != "all")
	{
		if(g_bRetailVersion)
			sc += " AND sq.branch_id = "+ m_branchid +" ";
	}
//DEBUG("sc = ", sc);
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
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
		ShowExp(sc,e);
		return false;
	}
	
	
	return true;

}

bool GetItemName()
{
	/*string sc = " SET DATEFORMAT dmy ";
	sc += "SELECT s.id, s.sn, s.product_code, s.branch_id, s.cost, s.purchase_date, s.supplier, s.supplier_code, s.po_number, e.name 'Status'";
	sc += " FROM stock s INNER JOIN enum e ";
	sc += " ON e.id = s.status ";
	sc += " WHERE e.class = 'stock_status'";
	sc += " ORDER BY purchase_date DESC ";
	*/
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT COUNT(s.product_code) AS Quantity, s.product_code, s.cost,  p.name 'p_desc', ";
	sc += " e.name 'Status' ";
	sc += " FROM stock s INNER JOIN product p ";
	sc += " ON p.code = s.product_code  INNER JOIN enum e";
	sc += " ON e.id = s.status ";
	sc += " WHERE e.class = 'stock_status'";
	sc += " AND s.status = 2 ";
	sc += " GROUP BY s.product_code, s.cost,  s.prod_desc, e.name, p.name";
	//sc += " HAVING COUNT(product_code) > 0 ";
	//sc += " ORDER BY purchase_date DESC ";
	
	//string sc = "SELECT * FROM product ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_RowsReturn = myAdapter.Fill(dst, "stock");
//DEBUG("rows=", rows);
		if(m_RowsReturn <= 0)
		{
			Response.Write("<h3>Item Not Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	//m_name = dst.Tables["item"].Rows[0]["name"].ToString();
	return true;
}


bool DoUpdate()
{
	string branch = Request.Form["branch"];
	string sn = Request.Form["sn"];
	string cost = Request.Form["cost"];
	string sc = "INSERT INTO stock (id, sn, cost, branch, age) VALUES('" + m_id + "', '" + sn;
	sc += "', " + cost + ", " + branch + ", GETDATE())";
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

bool DrawEditTable()
{
//	Response.Write("<br><center><h3>");
//	Response.Write("Stock List</h3>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>Stock List</font></font>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
	
	Response.Write("<table border=1 align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=11><br></td></tr>");


	DrawTableHeader();

	Response.Write("<form action=purchase.aspx?t=add method=post>");
	Response.Write("<input type=hidden name=id_old value='" + m_id + "'>");
	Response.Write("<tr><td>");
	Response.Write("<input type=text name=id value='" + m_id + "'>");
	Response.Write("</td><td>");
	Response.Write(m_name);
	Response.Write("</td><td>");
	if(!PrintBranchNameOptions())
		return false;
	Response.Write("</td><td>");
	Response.Write("<input type=text name=cost>");
	Response.Write("</td><td>");
	Response.Write("<input type=text name=sn>");
	Response.Write("</td><td>");
	Response.Write("<input type=text name=sn_end value=''>");
	Response.Write("</td><td>");
	Response.Write("<input type=submit name=cmd value=Save>");
	Response.Write("</td></tr>");

//	Response.Write("<tr><td colspan=5 align=right>");
//	Response.Write("<input type=submit name=cmd value=SAVE>");
//	Response.Write("</td></tr>");

	Response.Write("</form>");

	Response.Write("</table>");
	return true;
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");

	Response.Write("<td>ID</td>");
	Response.Write("<td>NAME</td>");
	Response.Write("<td>BRANCH</td>");
	Response.Write("<td>Price</td>");
	Response.Write("<td>S/N</td>");
	Response.Write("<td>S/N END</td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("</tr>\r\n");
}

void BindSearchSNProduct()
{
	Response.Write("<table width=100% cellspacing=2 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td colspan=4><b>Search Found:</b></td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th>Product Code</th> ");
	Response.Write("<th>Supplier Code</th> ");
	Response.Write("<th align=left>Description</th> ");
	Response.Write("<th align=left>Branch</th> ");
	Response.Write("<th align=left>Quantity</th> ");
	Response.Write("<th align=left>Price</th> ");
	Response.Write("</tr>");
	for(int i=0; i<dst.Tables["snQty"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["snQty"].Rows[i];
		string s_product_code = dr["code"].ToString();
		string s_supplier_code = dr["supplier_code"].ToString();
		string s_barcode = dr["barcode"].ToString();
		string s_quantity = dr["qty"].ToString();
		string s_prod_desc = dr["name"].ToString();
		string s_branch_name = "";
		if(g_bRetailVersion)
			s_branch_name = dr["branch_name"].ToString();
		string price = dr["price"].ToString();
		double dPrice = 0;
		if(price != null && price != "")
			dPrice = double.Parse(price);
		else
			dPrice = 0;
		Response.Write("<tr>");
		//Response.Write("<td align=center>"+s_product_code+"</td>");
		Response.Write("<th><a title='click here to view the sales reference' href='salesref.aspx?code="+ s_product_code +"' target=_blank class=o>"+s_barcode+"</a>");
		Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
		Response.Write("code=" + s_product_code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
		Response.Write("<th>"+s_supplier_code +"</th>");
		Response.Write("<td>"+s_prod_desc+"</td>");
		if(g_bRetailVersion)
			Response.Write("<td>"+s_branch_name+"</td>");
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
	Response.Write("<th>Product Barcode</th> ");
	Response.Write("<th>Supplier Code</th> ");
	Response.Write("<th align=left>Description</th> ");
	Response.Write("<th align=left>Branch</th> ");
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
	string s_supplier_code = dr["supplier_code"].ToString();
	string s_barcode = dr["barcode"].ToString();
	string s_cost = dr["price"].ToString();
	string s_desc = dr["name"].ToString();
	string s_qty = dr["qty"].ToString();
	string s_sid = dr["id"].ToString();	
	string s_branch_name = "";
	if(g_bRetailVersion)
		s_branch_name = dr["branch_name"].ToString();
	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");
	
	Response.Write("<tr>");
	//Response.Write("<td>"+s_prodcode+"</td>");
	Response.Write("<th><a title='click here to view the sales reference' href='salesref.aspx?code="+ s_prodcode +"' target=_blank class=o>"+s_barcode+"</a> ");	
	Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
	Response.Write("code=" + s_prodcode + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
	Response.Write("</th><th>"+s_supplier_code +"</th> ");
	Response.Write("<td>"+s_desc+"</td>");
	if(g_bRetailVersion)
		Response.Write("<td>"+s_branch_name+"</td>");
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
	
	Response.Write("<table width='"+ tableWidth +"' align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	//Response.Write("<tr bgcolor=#E3E3E3><td align=center colspan=2>Stock Quantity Table</td></tr>");
	//Response.Write("<br><hr size=1 color=black>");
	//Response.Write("<tr><td>Stock Quantity</td></tr>");
			//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
		
	string uri = "?cat=" + HttpUtility.UrlEncode(cat) + "&s_cat=" + HttpUtility.UrlEncode(s_cat)+ "&ss_cat=" + HttpUtility.UrlEncode(ss_cat);
	if(Request.QueryString["p"] != null && Request.QueryString["spb"] != null)
	uri += "&p="+ Request.QueryString["p"].ToString() +"&spb="+ Request.QueryString["spb"].ToString();
		
	if(ra_code != "")
	{
		uri += "&ra="+ ra_code;
		uri += "&id="+ ra_id;
	}

	int rows = dst.Tables["stock_qty"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 30;
	m_cPI.URI = "?ra="+ ra_code +"";
	if(m_branchid != "")
		m_cPI.URI += "&b="+ m_branchid;
	m_cPI.URI += "&s="+sSystem+"&op="+sOption+"&id="+ra_id+"&cat="+ HttpUtility.UrlEncode(cat) +"&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat="+ HttpUtility.UrlEncode(ss_cat);
	if(m_sort != null && m_sort != "")
		m_cPI.URI += "&sort="+ HttpUtility.UrlEncode(m_sort) +"";
	if(m_bDesc)
		m_cPI.URI += "&desc=1";

	//m_cPI.URI = "?stock.aspx?b="+ m_branch_id +"&cat="+ HttpUtility.UrlEncode(cat) +"&s_cat="+ HttpUtility.UrlEncode(s_cat) +"&ss_cat="+ HttpUtility.UrlEncode(ss_cat);
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<tr><td colspan=4>");
	Response.Write(sPageIndex);
	
	Response.Write("</td><td align=right colspan=3>");
	if(!DoItemOption())
		return;
	Response.Write("</td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3>");
	/*Response.Write("<th><a href=" + uri + "&sort=p.code");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write(" title='Click to sort by code' class=o>CODE</a></th>");
	*/
	//**new for baisheng for displaying Barcode NO
	Response.Write("<th align=left><a href=" + uri + "&sort=barcode");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write(" title='Click to sort by Barcode' class=o>BARCODE</a></th>");
	Response.Write("<th align=left>");
	Response.Write("PRODUCT CODE</th>");
	Response.Write("<th align=left>");
	Response.Write("SUPPLIER CODE</th>");

	//************//

	Response.Write("<th align=left><a href=" + uri + "&sort=name");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write(" title='Click to sort by Description' class=o>DESCRIPTION</a></th>");
	
	if(g_bRetailVersion)
	{
		Response.Write("<th align=left>BRANCH</th>");
	}

	Response.Write("<th><a href=" + uri + "&sort=stock");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write(" title='Click to sort by stock' class=o>STOCK</a></th>");
	Response.Write("<th align=right><a href=" + uri + "&sort=sales");
	if(!m_bDesc)
		Response.Write("&desc=1");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
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
		string s_supplier_code = dr["supplier_code"].ToString();
		string s_qty = dr["stock"].ToString();
		string s_description = dr["name"].ToString();
		string barcode = dr["barcode"].ToString();
		string sales = dr["sales"].ToString();
		string branch_name = "";
		if(g_bRetailVersion)
			branch_name = dr["branch_name"].ToString();
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		alterColor = !alterColor;
		string code = s_product_code;
		/*Response.Write("<td align=center nowrap>");
		
		Response.Write("<table border=0><tr>");
		Response.Write("<td><a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
		Response.Write(code);
		Response.Write("</a> ");
		Response.Write("</td>");
		
		Response.Write("<td width=50%>");
	//	if(CheckPatent("viewsales"))
		{
			Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
			Response.Write("code=" + code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
		}
		Response.Write("</td></tr></table>");
		Response.Write("</td>\r\n");
		*/
		Response.Write("<td>");
		Response.Write("<table border=0><tr>");
		Response.Write("<td><a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
		Response.Write(""+ barcode +"");
		Response.Write("</a> ");
		Response.Write("</td>");		
		Response.Write("<td width=11%>");
		Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
		Response.Write("code=" + code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
		Response.Write("</td></tr></table>");
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(""+ s_product_code +"");
		Response.Write("");
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(""+ s_supplier_code +"");
		Response.Write("");
		Response.Write("</td>");
		Response.Write("<td width=40%>"+s_description+"</td>");

		if(g_bRetailVersion)
			Response.Write("<td>"+ branch_name +"</td>");
		Response.Write("<td align=center><font color=");
		if(s_qty == "")
			s_qty = "999999";
		double q = MyDoubleParse(s_qty);
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




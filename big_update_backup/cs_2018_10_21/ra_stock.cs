<!-- #include file="fifo_f.cs" -->

<script runat=server>
DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table

//
int m_page = 1;
int m_nPageSize = 20;
int m_nQtyReturn = 0;
int m_nIndexCount = 10;

bool b_Allbranches = false;

string cat = "";
string s_cat = "";
string ss_cat = "";
string m_branchid = "1";		//branch id, 21/03/03 herman
string m_id = "";
string m_status = "";

string m_code = "";
bool m_bBarcode = false;

string m_querystring = "";

void Page_Load(Object Src, EventArgs E ) 
{
	
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != null)
		m_querystring = Request.QueryString["code"];

	if(Request.QueryString["st"] != null && Request.QueryString["st"] != null)
		m_status = Request.QueryString["st"];

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


	PrintAdminHeader();
	PrintAdminMenu();
	if(Request.Form["cmd"] == "ReSell Item...")
	{
	
		if(DoProcessReSellItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=liveedit.aspx?code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\">");
			return;
		}
	}

	if(Request.QueryString["rsid"] != null && Request.QueryString["rsid"] != "")
	{
		GetResellItem();
		return;
	}
	if(!GetStockQty())
		return;

	BindStockQty();	
	
	
	LFooter.Text = m_sAdminFooter;
}

void GetResellItem()
{
	if(!DoSearchAllItems())
		return;
	Response.Write("<form name=frm method=post>");
	Response.Write("<br><center><h4>Process Resell Item</h4>");
	Response.Write("<table align=center  cellspacing=0 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	for(int i=0; i<1; i++)
	{
		DataRow dr = dst.Tables["all_items"].Rows[0];
		string cat = dr["cat"].ToString();
		string brand = dr["brand"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat= dr["ss_cat"].ToString();
		string supplier = dr["supplier"].ToString();
		
		string repair_id = dr["repair_id"].ToString();
		string supp_rma_id = dr["supp_rma_id"].ToString();
		string status = dr["status"].ToString();
		string item_status = dr["item_location"].ToString();
		string id = dr["id"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["prod_name"].ToString();
		
		Response.Write("<input type=hidden name=hide_id value='"+ id +"' >");
		Response.Write("<tr");
		//Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(">");
		Response.Write("<td bgcolor=#EEEEEE >BRAND:</td>");
		Response.Write("<td><input size=45% type=text name=brand value='"+ brand +"' readonly></td>");
		Response.Write("</tr>");
		Response.Write("<td bgcolor=#EEEEEE >CAT:</td>");
		Response.Write("<td><select name=cat>");
		Response.Write("<option value='" + cat +"'>"+ cat +"</opiton>");
		Response.Write("<option value='" + GetSiteSettings("new_catalog_for_rma_stock", "2nd Items") +"'>"+ GetSiteSettings("new_catalogory_for_rma_stock", "2nd Items") +"</opiton>");
		Response.Write("</select>");
		Response.Write(" <i>(** to change the new catalog, please go to site settings and look for 'New Catalog for RMA stock **)</i>");
		//Response.Write("<td><input size=30% type=text name=brand value='"+ cat +"' readonly>");
		//Response.Write("&nbsp;&nbsp;NEW_CAT: <input type=text name=new_cat value='"+ GetSiteSettings("new_catalogory_for_rma_stock", "2nd Items") +"' readonly></td>");
		Response.Write("</td></tr>");
		Response.Write("<td bgcolor=#EEEEEE >S_CAT:</td>");
		Response.Write("<td><input size=40% type=text name=s_cat value='"+ s_cat +"' readonly></td>");
		Response.Write("</tr>");
		Response.Write("<td bgcolor=#EEEEEE >SS_CAT:</td>");
		Response.Write("<td><input size=55% type=text name=ss_cat value='"+ ss_cat +"' readonly></td>");
		Response.Write("</tr>");
		Response.Write("<td bgcolor=#EEEEEE >M_PN#:</td>");
		Response.Write("<td><input size=50% type=text name=supplier_code value='RMA_"+ supplier_code +"' readonly></td>");
		Response.Write("</tr>");
		Response.Write("<td bgcolor=#EEEEEE >DESCRIPTION:</td>");
		Response.Write("<td><input size=77% type=text name=name value='"+ name +" (rma item)'></td>");
		Response.Write("</tr>");
		Response.Write("<td bgcolor=#EEEEEE >SUPPLIER:</td>");
		Response.Write("<td><input type=text name=supplier value='"+ supplier +"'></td>");
		Response.Write("</tr>");
		Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='ReSell Item...' "+ Session["button_style"] +"");
		Response.Write(" onclick=\"if(!confirm('resell this item...')){return false;}\" >");
		Response.Write("</td></tr>");

	}
	Response.Write("</table>");
	Response.Write("</form>");
}

void ShowAllItemDetails(bool alterColor)
{
	if(!DoSearchAllItems())
		return;
	Response.Write("<table align=center cellspacing=1 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr align=left bgcolor=#EEEEE>");
	Response.Write("<th>RA_ID#</th><th>SN#</th><th>CODE#</th><th>SUPP_CODE#</th><th>DESCRIPTION</th><th>STATUS</th><th>&nbsp;</th><th>SELLING CODE#</th><th>SOLD QTY</th><th>&nbsp;</th>");
	Response.Write("</tr>");
	for(int i=0; i<dst.Tables["all_items"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["all_items"].Rows[i];
		string repair_id = dr["repair_id"].ToString();
		string supp_rma_id = dr["supp_rma_id"].ToString();
		string status = dr["status"].ToString();
		string item_status = dr["item_location"].ToString();
		string id = dr["id"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["prod_name"].ToString();
		string sn = dr["sn"].ToString();
		string resell_code = dr["resell_code"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string sold = dr["sold"].ToString();
		string sitem = item_status;
		if(item_status == "3")
			item_status = "RMA STOCK";
		if(item_status == "6")
			item_status = "RESELLING ";
		if(item_status == "5")
			item_status = "RETURNED FAULTY ITEM";
		if(item_status == "1")
			item_status = "REPLACEMENT";
		if(item_status == "7")
			item_status = "CREDIT";
		if(item_status == "4")
			item_status = "OUT OF WARRANTY";
		if(item_status == "2")
			item_status = "REPAIRED";

		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE ");
		alterColor = !alterColor;
		Response.Write(">");
		//if(status == "1")
		if(repair_id != "0")
			Response.Write("<td><a title='view repair item' href='techr.aspx?id="+ repair_id +"&r="+ DateTime.Now.ToOADate() +"' class=o target=new>"+ repair_id +"</a></td>");
		if(supp_rma_id != "0")
			Response.Write("<td><a title='view supplier rma' href='supp_rma.aspx?rma=rd&id="+ supp_rma_id +"&r="+ DateTime.Now.ToOADate() +"' class=o target=new>"+ supp_rma_id +"</a></td>");
		Response.Write("<td><a title='check sn#' href='snsearch.aspx?sn="+ HttpUtility.UrlEncode(sn) +"' class=o>"+ sn +"</a></td>");
		Response.Write("<td>"+ code +"</td>");
		Response.Write("<td>"+ supplier_code +"</td>");
		Response.Write("<td>"+ name +"</td>");
		if(repair_id != "0")
			status = "CUSTOMER RMA";
		if(supp_rma_id != "0")
			status = "SUPPLIER RMA";
		Response.Write("<td>"+ item_status +"</td>");
		Response.Write("<td>"+ status +"</td>");
		
		Response.Write("<td><a title='view product details' href='p.aspx?"+ resell_code +"&r="+ DateTime.Now.ToOADate() +"' class=o>"+ resell_code +"</a></td>");
		Response.Write("<td>");
		if(invoice != "" && invoice != null)
			Response.Write("<a title='view invoice' href='invoice.aspx?i="+ invoice +"&r="+ DateTime.Now.ToOADate() +"' class=o target=_blank>");
		Response.Write(""+ sold +" </a></td>");
		Response.Write("<td>");
		if(repair_id != "0")
			Response.Write("<a title='send to supplier' href='ra_supplier.aspx?id="+ repair_id +"&r="+ DateTime.Now.ToOADate() +"' class=o >STS</a>  ");
		if(sitem != "6")
			Response.Write(" <a title='resell items' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&code="+ code +"&rsid="+ id +"' class=o>RESELL</a>&nbsp;");
	//	Response.Write("<a title='resell items' href='"+ Request.ServerVariables["URL"] +"' class=o>RESELL</a>&nbsp;|&nbsp;");
		Response.Write("</td>");
		Response.Write("</tr>");

	}
	Response.Write("</table>");
}
bool DoProcessReSellItem()
{
	int nCode = GetNextCode();
	if(nCode <= 0)
	{
		Response.Write("Error generating code for new product");
		return false;
	}
	m_code = nCode.ToString();
	
	Boolean bRet = true;

	string supplier	= Request.Form["supplier"];
	string supplier_code = Request.Form["supplier_code"];
	string supplier_price = "0";//Request.Form["supplier_price"];
	if(supplier == "" || supplier == null)
		supplier = m_sCompanyName[0].ToString() + m_sCompanyName[1].ToString();
	//else if(supplier.Length > 3)
	//	supplier = supplier[0] +

	string id		= supplier + supplier_code;

	if(id.Length > 100)
	{
		Response.Write("<center><br><br><h3>M_PN is too long, maxmum 50 characters.</h3>");
		return false;
	}

	string name		= EncodeQuote(Request.Form["name"]);
	string brand	= EncodeQuote(Request.Form["brand"]);
	string cat		= EncodeQuote(Request.Form["cat"]);
	string s_cat	= EncodeQuote(Request.Form["s_cat"]);
	string ss_cat	= EncodeQuote(Request.Form["ss_cat"]);
	string rate		= "1.10";//Request.Form["rate"];
	string price	= "0";//Request.Form["price"];
	string stock	= "1";//Request.Form["stock"];
	string currency	= "NZD";
	currency = GetEnumID("currency", currency);

	double level_rate1 = 1.08;//MyDoubleParse(Request.Form["level_rate1"]);
	double level_rate2 = 1.06;//MyDoubleParse(Request.Form["level_rate2"]);
	int qty_break1 = 2;//MyIntParse(Request.Form["qty_break1"]);
	int qty_break2 = 5;//MyIntParse(Request.Form["qty_break2"]);
	int qty_break3 = 10;//MyIntParse(Request.Form["qty_break3"]);
	int qty_break4 = 50;//MyIntParse(Request.Form["qty_break4"]);

	string exchange_rate = "1";//Request.Form["exrate"];
	string freight = "0";//Request.Form["freight"];
	string raw_supplier_price = "0";//Request.Form["raw_supplier_price"];

	//update exchange rate if changed
	string rate_usd = "USD";
	string rate_aud = "AUD";
//	string rate_usd_old = Request.Form["rate_usd_old"];
//	string rate_aud_old = Request.Form["rate_aud_old"];
	//if(rate_usd != rate_usd_old)
		SetSiteSettings("exchange_rate_usd", rate_usd);
	//if(rate_aud != rate_aud_old)
		SetSiteSettings("exchange_rate_aud", rate_aud);

	if(stock == "")
		stock = "1"; //will display 'YES' instead of stock numbers if value is NULL in database
	string hot = "0";
	if(Request.Form["hot"] == "on")
		hot = "1";
	string skip		= "1"; //default to skip, 
	string special = "0";
	if(Request.Form["special"] == "on")
		special = "1";
	string isnew	= "0";

	Trim(ref name);
	Trim(ref brand);
	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	if(name != null && name.Length > 255)
		name = name.Substring(0, 255);

/*	if(Request.Form["cat_new"] != null && Request.Form["cat_new"] != "")
		cat = EncodeQuote(Request.Form["cat_new"]);
	if(Request.Form["s_cat_new"] != null && Request.Form["s_cat_new"] != "")
		s_cat = EncodeQuote(Request.Form["s_cat_new"]);
	if(Request.Form["ss_cat_new"] != null && Request.Form["ss_cat_new"] != "")
		ss_cat = EncodeQuote(Request.Form["ss_cat_new"]);
	if(Request.Form["brand_new"] != null && Request.Form["brand_new"] != "")
		brand = EncodeQuote(Request.Form["brand_new"]);
*/
	if(name == null || name == "")
	{
		Response.Write("Error, no product name");
		return false;
	}
	if(supplier_price == null || supplier_price == "" || TSIsDigit(supplier_price) == false)
	{
		Response.Write("Error, invalid supplier_price");
		return false;
	}
	if(price == null || price == "" || TSIsDigit(price) == false)
	{
		Response.Write("Error, invalid price");
		return false;
	}
	if(stock == "")
		stock = "0";
	
	double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dPrice = double.Parse(price, NumberStyles.Currency, null);
	
	//check for duplicate product ID
	string sc = "SELECT code, name FROM code_relations WHERE id='" + id + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "id_check");
		if(rows > 0)
		{
			string ic_code = dst.Tables["id_check"].Rows[0]["code"].ToString();
			string ic_name = dst.Tables["id_check"].Rows[0]["name"].ToString();
			Response.Write("<br><br><center><h3>Error, dupplicat Product ID (supplier + manualfacture_PN) found. Please Change Supplier to different 2-letter Supplier Name:</h3>");
			Response.Write("<b>Code : </b><a href=liveedit.aspx?code=" + ic_code + " class=o>" + ic_code + "</a><br>");
			Response.Write("<b>Desc : </b>" + ic_name);
			Response.Write("<br><br><h4>Check manualfacture_PN or use a different supplier.</h4>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, code, name, brand, ";
	sc += " cat, s_cat, ss_cat, hot, skip, rate, currency, exchange_rate, foreign_supplier_price, nzd_freight ";
	sc += " , level_rate1, level_rate2, qty_break1, qty_break2, qty_break3, qty_break4 ";
	sc += ") VALUES('";
	sc += id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += dSupplier_price;
	sc += ", ";
	sc += m_code;
	sc += ", '";
	sc += name;
	sc += "', '";
	sc += brand;
	sc += "', '";
	sc += cat;
	sc += "', '";
	sc += s_cat; 
	sc += "', '";
	sc += ss_cat;
	sc += "', ";
	sc += "1";
	sc += ", ";
	sc += skip;
	sc += ", ";
	sc += rate;
	sc += ", '";
	sc += currency;
	sc += "', ";
	sc += exchange_rate;
	sc += ", ";
	sc += raw_supplier_price;
	sc += ", ";
	sc += freight;
	sc += ", ";
	sc += level_rate1;
	sc += ", ";
	sc += level_rate2;
	sc += ", ";
	sc += qty_break1;
	sc += ", ";
	sc += qty_break2;
	sc += ", ";
	sc += qty_break3;
	sc += ", ";
	sc += qty_break4;
	sc += ")";

//return false;
	sc += " UPDATE rma_stock SET item_location = 6 , resell_code = "+ m_code +" WHERE id ="+ Request.Form["hide_id"];
//	DEBUG("sc=", sc);
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

	sc = "INSERT INTO product_skip (id, stock, supplier_price, price) ";
	sc += " VALUES('" + id + "', 0, 0, 0)";
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
/*	if(bRet)
	{
		Session["addp_brand"] = brand;
		Session["addp_cat"] = cat;
		Session["addp_s_cat"] = s_cat;
		Session["addp_ss_cat"] = ss_cat;
	}
*/
	return true;

}


bool GetStockQty()
{
	if(Request.QueryString["b"] != null && Request.QueryString["b"] !="")
	{
		m_branchid = Request.QueryString["b"];
		if(m_branchid == "all")
			b_Allbranches = true;
	}
	string kw = "";
	if(Request.Form["kw"] != null)
	{
		kw = Request.Form["kw"];
		Session["stock_adj_search"] = kw;
	}
	kw = EncodeQuote(kw);

//	string sc = " SELECT rs.code, rs.supplier_code, rs.prod_name AS name, p.supplier ";
	string sc = " SELECT DISTINCT rs.code, rs.supplier_code, p.name , p.supplier ";
	sc += " , (SELECT count(code) From rma_stock rs1 WHERE rs1.code = rs.code AND rs1.supplier_code = rs.supplier_code ";
	sc += ") AS stock, 0 AS allocated_stock ";
//	sc += " AND rs.prod_name = rs1.prod_name) AS stock, 0 AS allocated_stock ";
	sc += " , rs.branch_id ";
	sc += ", 0 AS adjusted  ";
	if(kw != "")
		sc += " , rs.sn ";

	sc += " FROM rma_stock rs JOIN code_relations p ON p.code = rs.code ";
	sc += " WHERE 1=1 ";
	if(cat != "" && cat != "all")
		sc += " AND p.cat = '"+ cat +"' ";
	
	if(s_cat != "" && s_cat != "all")
		sc += " AND p.s_cat = '"+ s_cat +"' ";
	
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND p.ss_cat = '"+ ss_cat +"' ";
	
	if(kw != "")
	{		
		if(IsInteger(kw))
			sc += " AND (p.code = " + kw +" OR rs.sn = '" + kw +"') ";
		else
			sc += " AND (rs.sn LIKE '%" + kw + "%' OR p.supplier_code LIKE '%" + kw + "%' OR p.name LIKE '%" + kw + "%') ";
	}
	if(m_branchid != "")
		if(TSIsDigit(m_branchid))
			sc += " AND rs.branch_id = "+ m_branchid;
	sc += " AND rs.item_location <= 7 ";
	if(m_querystring != "")
		sc += " AND p.code = "+ m_querystring;
	if(Request.QueryString["ty"] == "1")
		sc += " AND rs.repair_id != 0 AND rs.repair_id IS NOT NULL ";
	if(Request.QueryString["ty"] == "2")
		sc += " AND rs.supp_rma_id != 0 AND rs.supp_rma_id IS NOT NULL ";
//	if(!b_Allbranches && g_bRetailVersion)
//		sc += " AND rs.branch_id=" + m_branchid;
	sc += " ORDER BY p.name ";

//DEBUG("sc=", sc);
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
	if(kw != null && kw != "")
	{
		if(m_nQtyReturn == 1)
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?code="+ dst.Tables["stock_qty"].Rows[0]["code"].ToString() +"&sn="+ HttpUtility.UrlEncode(dst.Tables["stock_qty"].Rows[0]["sn"].ToString()) +"&r=" + DateTime.Now.ToOADate() + "\">");
			return false;
		}
	}
	return true;

}

void BindStockQty()
{

	Response.Write("<form name=frmQtyAdjust method=post action="+ Request.ServerVariables["URL"] +"?update=success&b=all");
	if(cat != "")
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	if(s_cat != "")
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	if(ss_cat != "")
		Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	Response.Write("&page=" + m_page);
	Response.Write(">");
	
	Response.Write("<center><h4>RMA Stock Take</h4>");
	Response.Write("<table width=98% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><td colspan=10><table border=0 cellpadding=1 width=95%><tr><td>");
	Response.Write("<b>Quick Search : </b></td><td><input type=text name=kw value='" + Session["stock_adj_search"] + "'>");
	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	
	
	//branch option

	Response.Write("<tr><td><b>Branch :</b></td><td align=left>");
	PrintBranchNameOptionsWithOnChange();
	Response.Write("</td>");

	Response.Write("<td align=right colspan=3>");

	//option type:::
	string uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"";
	if(cat != "")
		uri += "&cat=" + HttpUtility.UrlEncode(cat);
	if(s_cat != "")
		uri += "&scat=" + HttpUtility.UrlEncode(s_cat);
	if(ss_cat != "")
		uri += "&sscat=" + HttpUtility.UrlEncode(ss_cat);

	Response.Write("SELECT TYPE: <select name=rma_type onchange=\"window.location=('"+ uri +"&ty='+ document.frmQtyAdjust.rma_type.value)\" >");
	Response.Write("<option value=0 >ALL</option>");
	Response.Write("<option value=1 ");
	if(Request.QueryString["ty"] == "1")
		Response.Write(" selected ");
	Response.Write(">CUSTOMER RMA");
	Response.Write("<option value=2 ");
	if(Request.QueryString["ty"] == "2")
		Response.Write(" selected ");
	Response.Write(">SUPPLIER RMA");
	Response.Write("</select> &nbsp;&nbsp;| &nbsp;&nbsp;");

	if(!DoItemOption())
		return;
	Response.Write("</tr>");

	Response.Write("</table></td></tr>");

	Response.Write("<tr align=left bgcolor=#8BB7DD>");
	Response.Write("<th>CODE#</th>");
	Response.Write("<th nowrap>M_PN</th><th>STOCK TRACE</th>");
	Response.Write("<th>ITEM DESC</th>");

	Response.Write("<th>BRANCH</th>");
	Response.Write("<th>QTY</th>");
//	Response.Write("<th>&nbsp;</th>");
	Response.Write("<th>ACTION</th>");
//	Response.Write("<th>ALLOCATED</th>");
//	Response.Write("<th align=right>NEW QTY</th> "); //<td align=center>Adjustment</td></tr>");
//	Response.Write("<th>CORRECT ALLOCATED STOCK</th>");
//	if(m_bBarcode)
//		Response.Write("<th>PRINT BARCODE</th>");
	Response.Write("</tr>");

	bool bAlt = true;
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (m_page-1) * m_nPageSize;
//DEBUG("p=", startPage);
	for(int i=startPage; i<dst.Tables["stock_qty"].Rows.Count; i++)
	{
		//Response.Write("<form name=frmQtyAdjust method=post action='"+ Request.ServerVariables["URL"] +"?update=success'>");
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["stock_qty"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
			break;
	}
		
	PrintPageIndex();
	
	Response.Write("</table>");
	Response.Write("</form></center>");


}

bool PrintBranchNameOptionsWithOnChange()	//Herman: 21/03/03
{
	DataSet dsBranch = new DataSet();
	string sBranchID = "1";
	int rows = 0;
	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
	{
		sBranchID = Request.QueryString["b"];
		if(sBranchID != "all")
			Session["branch_id"] = MyIntParse(sBranchID); //Session["branch_id"] is integer
	}
	else if(Session["branch_id"] != null)
	{
		sBranchID = Session["branch_id"].ToString();
	}

	//do search
	string sc = "SELECT id, name FROM branch ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsBranch, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=branch");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?b=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	for(int i=0; i<rows; i++)
	{
		string bname = dsBranch.Tables["branch"].Rows[i]["name"].ToString();
		int bid = int.Parse(dsBranch.Tables["branch"].Rows[i]["id"].ToString());
		Response.Write("<option value='" + bid + "' ");
		if(IsInteger(sBranchID))
		{
			if(bid == int.Parse(sBranchID))
				Response.Write("selected");
		}
		Response.Write(">" + bname + "</option>");
	}
//	int iNumBranches = dsBranch.Tables["branch"].Rows.Count + 1;
	Response.Write("<option value='all'");
	if(!IsInteger(m_branchid))
	{
		Response.Write("selected");
		b_Allbranches = true;	
	}
	Response.Write("> All Branches</option>");

	if(rows == 0)
		Response.Write("<option value=1>Branch 1</option>");
	Response.Write("</select>");
	return true;
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT cat FROM product ";
	sc += " ORDER BY cat";
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
	string rma_type = Request.QueryString["ty"];

	if(rows <= 0)
		return true;
	Response.Write("Catalog Select: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?r="+ DateTime.Now.ToOADate() +"");
	if(rma_type != null && rma_type != "")
		Response.Write("&ty="+ rma_type +"");
	Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
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
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
	
		sc = "SELECT DISTINCT s_cat FROM product ";
		sc += " WHERE cat = '"+ cat +"' ";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?r="+ DateTime.Now.ToOADate() +"&cat=" + HttpUtility.UrlEncode(cat));
		if(rma_type != null && rma_type != "")
			Response.Write("&ty="+ rma_type +"");
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}

		Response.Write("</select>");
	}

	if(s_cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM product ";
		sc += " WHERE cat = '"+ cat +"' ";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
		if(rma_type != null && rma_type != "")
			Response.Write("&ty="+ rma_type +"");
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}

	return true;
}

bool DrawRow(DataRow dr, int i, bool alterColor)
{
	string adjusted = dr["adjusted"].ToString(); //if not blank then this product has been adjusted stock
	
	string s_qty = dr["stock"].ToString();
	string code = dr["code"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string s_productdesc = dr["name"].ToString();
		string swap = "";
	if(s_productdesc != "" && s_productdesc != null)
		s_productdesc = StripHTMLtags(s_productdesc);
	if(s_productdesc.Length >=60)
	{
		for(int j=0; j<60; j++)
			swap += s_productdesc[j].ToString();
	}
	else
		swap = s_productdesc;

	s_productdesc = swap;

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");

	Response.Write("<td align= nowrap>");
	//Response.Write("<table border=0><tr><td width=80%><a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
	Response.Write("<table border=0><tr><td width=80%><a title='View Product details:' href=\"javascript:product_window=window.open('p.aspx?" + code +"', '','scrollbars=1'); product_window.focus();\" class=o >");
	Response.Write(""+ code +"</a> </td><td>");
	Response.Write(" </td><td>");

	Response.Write("</td>\r\n");
	Response.Write("</tr></table></td>");
	Response.Write("<td>" + supplier_code + "</td>");
	//Response.Write("<td><a title='view log' href=\"javascript:repairlog_window=window.open('repair_log.aspx?code=" + code + "&r="+ DateTime.Now.ToOADate() +"', 'repairlog_window','scrollbars=yes,resizable=yes'); repairlog_window.focus();\" class=o>RLog</a> ");
	Response.Write("<td><a title='view repair log' href=\"javascript:repairlog_window=window.open('repair_log.aspx?code=" + code + "&r="+ DateTime.Now.ToOADate() +"', 'repairlog_window',''); repairlog_window.focus();\" class=o>RLog</a> ");
	
	Response.Write("</td>");

	Response.Write("<td> "+s_productdesc.ToUpper() +"</td>");
	
	
	string s_branchid = dr["branch_id"].ToString();
	Response.Write("<td align=left>" + s_branchid + "</td>");	
	

	Response.Write("<input type=hidden name=code" + i + " value='" + code + "'>");
	Response.Write("<td nowrap>");
	Response.Write("<b><font color=");
	if(int.Parse(s_qty) == 0)
		Response.Write("Green>");
	else if(int.Parse(s_qty) < 0)
		Response.Write("Red>");
	else if(int.Parse(s_qty) > 0)
		Response.Write("Black>");

	Response.Write(s_qty);
	
	int nAllocated = MyIntParse(dr["allocated_stock"].ToString());

	Response.Write("<input type=hidden name=qty_old" + i + " value='"+s_qty+"' >");
	string uri = "&cat=" + HttpUtility.UrlEncode(cat);
	uri += "&scat=" + HttpUtility.UrlEncode(s_cat);
	uri += "&sscat=" + HttpUtility.UrlEncode(ss_cat);
	uri += "&page=" + m_page;
	if(Request.QueryString["ty"] != null && Request.QueryString["ty"] != "")
		uri += "&ty="+ Request.QueryString["ty"];
	Response.Write("</td><td>");
	Response.Write("<a title='view all items' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() + uri +"");
	if(m_querystring == null || m_querystring == "")
		Response.Write("&code="+ code +"");
	Response.Write("' class=o>VIEW");
	if(m_querystring != null && m_querystring != "")
	Response.Write(" ALL</a>");
//	Response.Write("</td><td>");
//	Response.Write("<a title='process item' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&code="+ code +"&st=rs' class=o>RESELL</a>");
//	Response.Write("&nbsp;&nbsp;<a title='process item' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&code="+ code +"' class=o>PUT BACK to STOCK</a>");
	//Response.Write("<a title='process item' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&code="+ code +"' class=o>RESELL</a>");
	Response.Write("</td></tr>");
	if(m_querystring != "")
	{
		Response.Write("<tr><td colspan=7>");
		ShowAllItemDetails(alterColor);
		Response.Write("</td></tr>");
	}

	//Response.Write("</form>");

	return true;
}

void PrintPageIndex()
{
	Response.Write("<tr><td colspan=2>Page: ");
	int pages = dst.Tables["stock_qty"].Rows.Count / m_nPageSize + 1;
	//int pages = dst.Tables["stock"].Rows.Count / m_nPageSize;
	int start = 1; 
	if(m_page >= 2)
		start = m_page - 1;
	int end = pages;
	if(end - start > m_nIndexCount)
		end = start + m_nIndexCount;
	int i = start;
	if(i > 2)
	{
		Response.Write("<a href="+ Request.ServerVariables["URL"] +"?page=" + (start - 1).ToString());
		if(cat != "")
			Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		if(s_cat != "")
			Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		if(ss_cat != "")
			Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
		Response.Write(">...</a> ");
	}

	for(; i<=end; i++)
	{
		if(i != m_page)
		{
			Response.Write("<a href="+ Request.ServerVariables["URL"] +"?page=");
			Response.Write(i.ToString());
			if(cat != "")
				Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
			if(s_cat != "")
				Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
			if(ss_cat != "")
				Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
			Response.Write(">");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	if(end < pages)
	{
		Response.Write("<a href="+ Request.ServerVariables["URL"] +"?page=" + i.ToString());
		if(cat != "")
			Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		if(s_cat != "")
			Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		if(ss_cat != "")
			Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
		Response.Write(">...</a>");
	}

	Response.Write("</td>");
//	Response.Write("<td colspan=7 align=right>");
//	Response.Write("<b>Adjustment Note : </b><input type=text name=note> ");
//	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Update Adjustment' >");
//	Response.Write("</td>");
	Response.Write("</tr>");
}

bool DoSearchAllItems()
{
	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
	{
		if(TSIsDigit(Request.QueryString["code"].ToString()))
		{
			string sc = " SELECT p.brand, p.cat, p.s_cat, p.ss_cat, p.name , p.supplier, rs.* ";
			sc += ", (SELECT ss.invoice_number FROM sales ss WHERE ss.code = rs.resell_code) AS invoice_number ";
			sc += ", (SELECT count(code) FROM sales ss WHERE ss.code = rs.resell_code) AS sold ";
			sc += " FROM rma_stock rs JOIN code_relations p ON p.code = rs.code ";
			sc += " WHERE rs.code ="+ Request.QueryString["code"];
			if(Request.QueryString["rsid"] != null && Request.QueryString["rsid"] != "")
				sc += " AND rs.id = "+ Request.QueryString["rsid"];
			if(Request.QueryString["sn"] != "" && Request.QueryString["sn"] != null)
				sc += " AND rs.sn LIKE '%"+ Request.QueryString["sn"] +"%' ";
			sc += " AND rs.item_location <= 7 ";
			//sc += " AND rs.item_location < 6 ";
			sc += " ORDER by rs.id ";
//	DEBUG("sc = ", sc);
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				myAdapter.Fill(dst, "all_items");
		//DEBUG("rows=", rows);
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
		}
	}
	return true;
}
</script>

<asp:Label id=LFooter runat=server/>

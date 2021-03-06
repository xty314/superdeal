<!-- #include file="kit_fun.cs" -->
<!-- #include file="catalog.cs" -->
<!-- #include file="price.cs" -->

<script runat=server>

string m_sql = "";//"SELECT p.code, p.id, p.name, p.brand, p.cat, p.s_cat, p.ss_cat, p.hot, c.skip FROM product p JOIN code_relations c ON p.id=c.id WHERE p.cat='hardware' AND p.'s_cat'='case' ORDER BY p.cat, p.s_cat, p.ss_cat, p.name, p.brand";
string m_code = "";
string m_c = "hardware";
string m_s = "case";
string m_co = "-1"; //cat for options
string m_so = "-1"; //s_cat for options
string m_ck = "-1"; //cat for options
string m_sk = "-1"; //s_cat for options
string m_action = "";
string m_supplierID = "0";
string m_supplierName = "";
bool m_bUpdateCatalogTable = false;
string m_url = "liveedit.aspx?code=";

string m_sBP = "";
//bool m_bSkipped = false; //indictate this is a skipped item

DataSet dsc = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations

string m_sSupplier_code = "";
string copy_supplier = "";
string copy_supplier_code = "";
string copy_id = "";

string m_sCurrencyName = "NZD";

bool m_bSimpleInterface = false;
bool m_bFixedPrices = false;
bool m_bQPOSUseAVGCost = false;
bool m_bSecurityCheckAccessAllow = false;
bool m_bAllowChangeSupplierCode = false;
string tableWidth = "90%";
string rowWidth = "46%";
double m_GSTRate = 1;
bool bHidden = false;
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	m_bSecurityCheckAccessAllow = bSecurityAccess(Session["card_id"].ToString());
	m_bAllowChangeSupplierCode = MyBooleanParse(GetSiteSettings("allow_change_supplier_code", "0", true));

	try
	{
		m_GSTRate = 1 + (double.Parse(GetSiteSettings("gst_rate_percent", "12.5")) / 100);
	}
	catch(Exception ec)
	{
	}
	if(m_GSTRate < 1)
		m_GSTRate = 1 + m_GSTRate;

	if(MyBooleanParse(GetSiteSettings("simple_liveedit", "1", true)))
		m_bSimpleInterface = true;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		m_bFixedPrices = true;
	m_bQPOSUseAVGCost = MyBooleanParse(GetSiteSettings("set_qpos_to_use_avg_cost", "0", true));
	m_sCurrencyName = GetSiteSettings("default_currency_name", "NZD");
	InitKit();
	if(!GetQueryStrings())
		return;
    GetSupplier();

	if(m_action == "copy")
	{
		if(Request.Form["cmd"] == Lang("Copy"))
		{
			m_code = Request.QueryString["code"];
			if(DoCopyProduct())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=liveedit.aspx?code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		else
		{
			PrintAdminHeader();
			PrintAdminMenu();
			
			Response.Write("<br><br><center><h3>"+Lang("Copy Item")+"</h3>");
			Response.Write("<form action=liveedit.aspx?action=copy&code=" + m_code + " method=post>");
			Response.Write("<table><tr><td>"+Lang("Supplier")+" : </td><td>" + PrintSupplierOptionsWithShortName() + "</td></tr>");
			Response.Write("<tr><td>"+Lang("Supplier Code")+" : </td><td><input type=text name=supplier_code></td></tr>");
			Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value="+Lang("Copy")+"></td></tr>");
			Response.Write("</table></form>");
			PrintAdminFooter();
		}
		return;
	}
    if(Request.QueryString["search"] == "1")
	{
        Response.Write("<br><center><h3>Search For Supplier</h3></center>");
        DoSupplierSearch();
		return;
	}


	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();
//DEBUG("action=", m_action);	
	Boolean bRet = true;
	if(m_action == "update")
	{    
	
		string update = Request.Form["update"];
		if(update != null)
		{
			m_bUpdateCatalogTable = false;
			if(String.Compare(update, Lang("update"), true) == 0)
			{
				bRet = UpdateAllRows();
			}
//			if(m_bUpdateCatalogTable)
//				bRet = UpdateCatalogTable();
		}
		
//		else
//		{
//			bRet = DoSqlCmd(Request.Form["sSqlCmd"]);
//		}
		if(bRet)
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=liveedit.aspx?code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\"></body></html>");
			return; 
		}
	}
	else if(m_action == "add")
	{
		if(AddCross())
		{
			string s = "<br><br>"+Lang("done! wait a moment")+"......... <br>\r\n";
			s += "<meta http-equiv=\"refresh\" content=\"1; URL=liveedit.aspx?code=";
			s += m_code + "&r=" + DateTime.Now.ToOADate();
			s += "\"></body></html>";
			Response.Write(s);
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
		}
	}
	else if(m_action == "delete")
	{
		if(DeleteCross())
		{
			string s = "<br><br>"+Lang("done! wait a moment")+"......... <br>\r\n";
			s += "<meta http-equiv=\"refresh\" content=\"1; URL=liveedit.aspx?code=";
			s += m_code + "&r=" + DateTime.Now.ToOADate();
			s += "\"></body></html>";
			Response.Write(s);
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
		}
	}
	else
	{
		if(DoSearch())
		{	
			PrintJavaFunction();
			MyDrawTable();
		}
	}
	WriteFooter();
	PrintAdminFooter();
}

bool GetQueryStrings()
{
	if(Request.Form["m_sql"] != null && Request.Form["m_sql"] != "")
		m_sql = Request.Form["m_sql"];

   if(Request.QueryString["s"]  != "" && Request.QueryString["s"] != null)
		m_sSupplier_code = Request.QueryString["s"];
	m_code = Request.QueryString["code"];
	m_c = Request.QueryString["c"];
	m_s = Request.QueryString["s"];
	
	m_url += Request.QueryString["code"];
    m_url += "&ssid=" + m_ssid;

	if(Request.QueryString["co"] != null)
		m_co = Request.QueryString["co"];
	if(Request.QueryString["so"] != null)
		m_so = Request.QueryString["so"];
	if(Request.QueryString["ck"] != null)
		m_ck = Request.QueryString["ck"];
	if(Request.QueryString["sk"] != null)
		m_sk = Request.QueryString["sk"];
	m_action = Request.QueryString["action"];
	return true;
}

Boolean DoSearch()
{
	if(m_code == null || m_code == "")
	{
		if(m_sSupplier_code == "")
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx\">");
			return false;
		}
	}

	if(dst.Tables["product"] != null)
		dst.Tables["product"].Clear();

	int rows = 0;
	string id = "";
	string sc = "SELECT c.code, c.supplier_code, c.id, c.skip, c.barcode ";
	sc += " FROM code_relations c ";
	sc += " WHERE 1 = 1 ";
	if(m_code != null && m_code != "")
		sc += " AND c.code = " + m_code;
	else
	{
		sc += " AND (c.supplier_code = N'" + EncodeQuote(m_sSupplier_code) + "' ";
		sc += " OR  c.barcode =N'" + EncodeQuote(m_sSupplier_code) + "')";
	}
//DEBUG("sc=", sc);		
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "id");
		if(rows > 0)
		{
			id = dst.Tables["id"].Rows[0]["id"].ToString();
			m_code = dst.Tables["id"].Rows[0]["code"].ToString();
			m_sSupplier_code = dst.Tables["id"].Rows[0]["supplier_code"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(id == "")
	{
		Response.Write("<h3>"+Lang("Product Not Found")+"</h3><input type=button value='Add This Item' onclick=\"window.location=('addp.aspx?s="+Request.QueryString["s"].ToString()+ "')\" style=\" height=30; width=150; font:bold 20px arial\" >");
		Response.Write("<input type=button onclick=\"javascript:history.go(-1)\" value=Cancel style=\" height=30; width=150; font:bold 20px arial\" >");
		return false;
	}
	/*else if(m_sSupplier_code != "")
	{
		DEBUG("m_sSupplier_code", m_sSupplier_code);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=liveedit.aspx?code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\">");
		return false;	
	}*/

	if(dst.Tables["product"] != null)
		dst.Tables["product"].Clear();
	bool bSkipped = bool.Parse(dst.Tables["id"].Rows[0]["skip"].ToString());
	if(bSkipped)
	{
		sc = " SELECT c.*, k.price, k.stock, k.eta, 0 AS special ";
		sc += " FROM product_skip k JOIN code_relations c ON k.id=c.id ";
		sc += " WHERE c.id='" + EncodeQuote(id) + "'";
	}
	else
	{
      	sc = " SELECT TOP 1 c.*, p.price, (SELECT sum(qty) FROM stock_qty WHERE code = p.code ) AS stock, p.eta, p.hot, isnull(s.code, 0) AS special ";
		sc += " FROM product p JOIN code_relations c ON p.code=c.code LEFT OUTER JOIN specials s on p.code=s.code ";
		sc += " WHERE c.id = '" + EncodeQuote(id) + "'";
	}
	if(m_supplierString != "")
		sc += " AND c.supplier IN" + m_supplierString + " ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean UpdateAllRows()
{
	int i = 0;
	string id = Request.Form["id"+i.ToString()];
//	while(id != null && id != "")
//	{
		if(!UpdateOneRow(i.ToString()))
			return false;;
//		i++;
//		id = Request.Form["id"+i.ToString()];
//	}
	return true;
}


bool CheckBarcode(string barcode, string code)
{


//	string sc = " SELECT barcode FROM code_relations WHERE (barcode ='" + barcode +"')";
//	sc += " OR package_barcode1 = '"+ barcode +"'  OR package_barcode2 = '"+ barcode +"'  OR package_barcode2 = '"+ barcode +"' ) AND code <> " + code;
   string barcode1 = Request.Form["package_barcode1"];
	string barcode2 = Request.Form["package_barcode2"];
	string barcode3 = Request.Form["package_barcode3"];
	string old_barcode = Request.Form["old_barcode"];

            
   
             string sc = " SELECT barcode FROM code_relations WHERE (barcode ='" + barcode1 +"' OR barcode = '"+barcode2+"' OR barcode = '"+barcode3+"'";
            
            if(barcode != old_barcode){
           sc += " or barcode = '"+barcode+"'" ;
           sc +=    " AND code <> " + code;  

}           
           sc +=" or package_barcode1='"+barcode+"' or package_barcode2 ='"+barcode+"' or package_barcode3 ='"+barcode+"' ";
           sc +=" or (package_barcode1='"+barcode2+"' or package_barcode1 = '"+barcode3+"')";
           sc +=" or (package_barcode2='"+barcode1+"' or package_barcode2 = '"+barcode3+"')";
           sc +=" or (package_barcode3='"+barcode1+"' or package_barcode3 = '"+barcode2+"')";
           sc += ")";
		   sc += " AND barcode <> '' ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "id");
//DEBUG("rows=",rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG("sc =", sc);
	if(rows >= 1)
	{
		Response.Write("<center><br><h4><font color=red>BARCODE# "+ barcode +" DUPLICATE!!!<br>PLEASE TRY AGAIN");
		Response.Write("<br><br><a title='back to edit product' href='"+ Request.ServerVariables["URL"] +"?code="+ code +"&r="+ DateTime.Now.ToOADate() +"' class=o><< Back </a>");
		return false;
	}

	return true;
}
Boolean UpdateOneRow(string sRow)
{
	Boolean bRet = true;

	string code		= Request.Form["code"+sRow];
	RecordUpdatedItem(code);

	string id		= Request.Form["id"+sRow];
	string supplier	= Request.Form["supplier"+sRow];
	string supplier_code = Request.Form["supplier_code"+sRow];
	string name		= EncodeQuote(Request.Form["name"+sRow]);
	string name_cn	= EncodeQuote(Request.Form["name_cn"+sRow]);
	string brand	= EncodeQuote(Request.Form["brand"+sRow]);
	string cat		= EncodeQuote(Request.Form["cat"+sRow]);
	string s_cat	= EncodeQuote(Request.Form["s_cat"+sRow]);
	string ss_cat	= EncodeQuote(Request.Form["ss_cat"+sRow]);
//	string sku		= EncodeQuote(Request.Form["sku"+sRow]);
	string supplier_price = Request.Form["supplier_price"+sRow];
	string supplier_price_old = Request.Form["supplier_price_old"+sRow];
	string price	= Request.Form["price"+sRow];
	price = price.Replace("$", "");
	string average_cost	= Request.Form["average_cost"];
//	if(average_cost == "")
//		average_cost = supplier_price;
	string old_average_cost	= Request.Form["old_average_cost"];
	string stock	= Request.Form["stock"+sRow];
	string eta		= EncodeQuote(Request.Form["eta"+sRow]);
	string hot		= Request.Form["hot"+sRow];
	string skip		= Request.Form["skip"+sRow];
	string clearance = Request.Form["clearance"+sRow];
	string rate		= Request.Form["rate"+sRow];
	try
	{
		rate = double.Parse(rate).ToString();
	}
	catch(Exception e)
	{
		rate = "1.01";
	}
	string currency	= Request.Form["currency_name"];

//DEBUG("currrency =", currency);
	//currency = GetEnumID("currency", currency);
	currency = GetCurrencyID(currency);
	
	string exchange_rate	= Request.Form["exrate"];
	string freight	= Request.Form["freight"];
	string foreign_supplier_price	= Request.Form["raw_supplier_price"];
	string weight = Request.Form["weight"+sRow];
	double dWeight = MyDoubleParse(weight);
	string stock_location = Request.Form["stock_location"];
	stock_location = EncodeQuote(stock_location);

	string barcode = Request.Form["barcode"];
if(barcode != "" && barcode != null && barcode != code)
{
	if(!CheckBarcode(barcode, code))
		return false;
}
	string expire_date = Request.Form["expire_date"];
	
	string is_service = Request.Form["is_service"];
	if(is_service == "on")
		is_service = "1";
	else
		is_service = "0";

	if(is_service == "1") //enforce
	{
		brand = "ServiceItem";
		cat = "ServiceItem";
	}
	string rrp		= Request.Form["rrp"+sRow];
	double drrp = MyDoubleParse(rrp);

	string manual_cost_frd = "0";
	if(Request.Form["manual_cost_frd"] != null && Request.Form["manual_cost_frd"] != "")
		manual_cost_frd = Request.Form["manual_cost_frd"];
	string manual_exchange_rate = Request.Form["manual_exrate"];
	string manual_cost_nzd = Request.Form["manual_cost_nzd"];

	double[] level_rate = new double[9];
	double[] qty_discount = new double[9];
	double[] qty_price = new double[16];
	double[] level_price = new double[10];
	
	int[] qb = new int[9];

	int dls = MyIntParse(Request.Form["dls"]);
	int qbs = MyIntParse(Request.Form["qbs"]);

	int i = 0;
	for(i=0; i<dls; i++)
	{
		string ii = (i+1).ToString();
		level_rate[i] = MyDoubleParse(Request.Form["level_rate" + ii]);
		level_price[i] = MyMoneyParse(Request.Form["level_price" + ii]);
	}
	for(i=0; i<qbs; i++)
	{
		string ii = (i+1).ToString();
		qb[i] = MyIntParse(Request.Form["qty_break" + ii]);
		qty_discount[i] = MyIntParse(Request.Form["qty_break_discount" + ii]);
		qty_price[i] = MyMoneyParse(Request.Form["qty_price" + ii]);
	}

	string qpos_qty_break = Request.Form["qpos_qty_break"];
	if(qpos_qty_break == null || qpos_qty_break == "")
		qpos_qty_break = "0";
	//update exchange rate if changed
	string rate_usd = Request.Form["rate_usd"];
	string rate_aud = Request.Form["rate_aud"];
	string rate_usd_old = Request.Form["rate_usd_old"];
	string rate_aud_old = Request.Form["rate_aud_old"];
	if(rate_usd != rate_usd_old)
		SetSiteSettings("exchange_rate_usd", rate_usd);
	if(rate_aud != rate_aud_old)
		SetSiteSettings("exchange_rate_aud", rate_aud);
	
	bool bSpecial = (Request.Form["special"+sRow] == "on");
	bool bSpecial_old = (Request.Form["special_old"+sRow] == "on");

	string price_old = Request.Form["price_old"+sRow];
	double dPrice_old = MyMoneyParse(price_old);
	double dsupplier_price_old = MyMoneyParse(supplier_price_old);
	double dRate = MyDoubleParse(rate);

	if(Request.Form["brand_new"+sRow] != "")
		brand = EncodeQuote(Request.Form["brand_new"+sRow]);
	if(Request.Form["cat_new"+sRow] != "")
		cat = EncodeQuote(Request.Form["cat_new"+sRow]);
	if(Request.Form["s_cat_new"+sRow] != "")
		s_cat = EncodeQuote(Request.Form["s_cat_new"+sRow]);
	if(Request.Form["ss_cat_new"+sRow] != "")
		ss_cat = EncodeQuote(Request.Form["ss_cat_new"+sRow]);

	string old_code		= EncodeQuote(Request.Form["old_code"+sRow]);
	string old_brand	= EncodeQuote(Request.Form["old_brand"+sRow]);
	string old_cat		= EncodeQuote(Request.Form["old_cat"+sRow]);
	string old_s_cat	= EncodeQuote(Request.Form["old_s_cat"+sRow]);
	string old_ss_cat	= EncodeQuote(Request.Form["old_ss_cat"+sRow]);
	string old_hot		= EncodeQuote(Request.Form["old_hot"+sRow]);
	string old_skip		= EncodeQuote(Request.Form["old_skip"+sRow]);

	if(String.Compare(brand, old_brand, true) != 0)
	{
		m_bUpdateCatalogTable = true;
	}
	else
	{
		if(String.Compare(cat, old_cat, true) != 0)
			m_bUpdateCatalogTable = true;
		else
		{
			if(String.Compare(s_cat, old_s_cat, true) != 0)
				m_bUpdateCatalogTable = true;
			else
			{
				if(String.Compare(ss_cat, old_ss_cat, true) != 0)
					m_bUpdateCatalogTable = true;
				else
				{
					if(String.Compare(hot, old_hot, true) != 0)
						m_bUpdateCatalogTable = true;
				}
			}
		}
	}
	
	if(hot == null)
		hot = "0";
	else
		hot = "1";

	if(skip == null)
		skip = "0";
	else
		skip = "1";

	if(clearance == "on")
		clearance = "1";
	else
		clearance = "0";
		
	string new_item = p("new_item");
	if(new_item == "")
		new_item = "0";
	else
		new_item = "1";
	
	string hidden_item = p("hidden_item"); 
	if(hidden_item == "")
		hidden_item = "0";
	else
		hidden_item = "1";

	double dsupplier_price = MyMoneyParse(supplier_price);
	double dPrice = MyMoneyParse(price);

/*	if(supplier_price == "0")
	{
		dRate = 1;
		Response.Write("<font color=red><h3>Error, supplier_price is 0, please skip this product.</h3></font>");
		return false;
	}
*/
	if(dsupplier_price != dsupplier_price_old) //supplier price change
	{
		if(!WritePriceHistory(code, dPrice * level_rate[0], dPrice_old * level_rate[0])) //use level 1 price as retail price
			return false;
	}
	
	if(stock == "")
		stock = "null";
	StringBuilder sb = new StringBuilder();

	//check kit needs
	if(skip == "1")
	{
		if(Kit_NeedThisItem(code, "phased out"))
		{
			return false;
		}
	}
//DEBUG("eaet =", eta);
	string sc = "";
//DEBUG("m_bSimpleInterface=",MyParseDouble??m_bSimpleInterface);
	if(m_bSimpleInterface)
	{
		
		sc = " UPDATE code_relations SET ";
		sc += " name = N'" + name + "' ";
		sc += ", name_cn = N'" + name_cn + "' ";
		sc += ", brand = N'" + brand + "' ";
		sc += ", cat = N'" + cat + "' ";
		sc += ", s_cat = N'" + s_cat + "' ";
		sc += ", ss_cat = N'" + ss_cat + "' ";	
		//sc += ", supplier = '0' ";		
//		sc += ", price = " + dPrice;
		for(i=0; i<qbs; i++)
		{
			string ii = (i+1).ToString();
			sc += ", qty_break" + ii + " = " + qb[i];
			sc += ", qty_break_price" + ii + " = " + qty_price[i];
		}
		sc += " WHERE code=" + code;
		sc += " UPDATE product SET ";
		sc += " name = N'" + name + "' ";
		sc += ", brand = N'" + brand + "' ";
		sc += ", cat = N'" + cat + "' ";
		sc += ", s_cat = N'" + s_cat + "' ";
		sc += ", ss_cat = N'" + ss_cat + "' ";
//		sc += "', hot=" + hot;
//		sc += ", skip=" + skip + ", clearance=" + clearance;
		sc += ", eta = N'"+ EncodeQuote(eta) +"'";
		sc += ", price = " + dPrice;
	//	sc += ", qpos_qty_break = " + qpos_qty_break;
	 
		sc += " WHERE code=" + code;

		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception esc) 
		{
			ShowExp(sc, esc);
			return false;
		}
		if(bSpecial != bSpecial_old)
		{
			if(bSpecial) //do insert
				bRet = AddSpecial(code);
			else //do delete
				bRet = RemoveSpecial(code);
		}
		SetItemUpdatedFlag(code);
		return true;
	}
	else if(m_bFixedPrices)
	{
		 
		if(!UpdateBranchOfItem())
			return false;
		string price1	= Request.Form["price1"];
		string price2	= Request.Form["price2"];
		string price3	= Request.Form["price3"];
		string price4	= Request.Form["price4"];
		string price5	= Request.Form["price5"];
		string price6	= Request.Form["price6"];
		string price7	= Request.Form["price7"];
		string price8	= Request.Form["price8"];
		string price9	= Request.Form["price9"];
	
		double dPrice1 = MyMoneyParse(price1);
		double dPrice2 = MyMoneyParse(price2);
		double dPrice3 = MyMoneyParse(price3);
		double dPrice4 = MyMoneyParse(price4);
		double dPrice5 = MyMoneyParse(price5);
		double dPrice6 = MyMoneyParse(price6);
		double dPrice7 = MyMoneyParse(price7);
		double dPrice8 = MyMoneyParse(price8);
		double dPrice9 = MyMoneyParse(price9);
		
		string level_price0 = Request.Form["wholeprice"];
		
		int nQty1 = MyIntParse(Request.Form["d_qty_break1"]);
		
		skip = Request.Form["skip"];
		supplier_price = MyMoneyParse(Request.Form["supplier_price"]).ToString();
		string avg_cost = MyMoneyParse(Request.Form["average_cost"]).ToString();
		
		int low_stock = MyIntParse(Request.Form["low_stock"]);
		int moq = MyIntParse(p("moq"));
		int inner_pack = MyIntParse(p("inner_pack"));
		string sInactive = p("inactive");
		if(sInactive == "")
			sInactive = "0";

		string package_barcode1 = Request.Form["package_barcode1"];
		string package_qty1 = Request.Form["package_qty1"];
		string package_price1 = Request.Form["package_price1"];
		Trim(ref package_barcode1);
		Trim(ref package_qty1);
		Trim(ref package_price1);
		package_barcode1 = EncodeQuote(package_barcode1);
		package_qty1 = MyIntParse(package_qty1).ToString();
		package_price1 = MyMoneyParse(package_price1).ToString();

		string package_barcode2 = Request.Form["package_barcode2"];
		string package_qty2 = Request.Form["package_qty2"];
		string package_price2 = Request.Form["package_price2"];
		Trim(ref package_barcode2);
		Trim(ref package_qty2);
		Trim(ref package_price2);
		package_barcode2 = EncodeQuote(package_barcode2);
		package_qty2 = MyIntParse(package_qty2).ToString();
		package_price2 = MyMoneyParse(package_price2).ToString();

		string package_barcode3 = Request.Form["package_barcode3"];
		string package_qty3 = Request.Form["package_qty3"];
		string package_price3 = Request.Form["package_price3"];
		Trim(ref package_barcode3);
		Trim(ref package_qty3);
		Trim(ref package_price3);
		package_barcode3 = EncodeQuote(package_barcode3);
		package_qty3 = MyIntParse(package_qty3).ToString();
		package_price3 = MyMoneyParse(package_price3).ToString();
//check 3 packages barcode for existing barcodes
		if(package_barcode1 != "")
		{
			if(!CheckBarcode(package_barcode1, code))
				return false;
		}
		if(package_barcode2 != "")
		{
			if(!CheckBarcode(package_barcode2, code))
				return false;
		}
		if(package_barcode3 != "")
		{
			if(!CheckBarcode(package_barcode3, code))
				return false;
		}
		//check 
		double dSpecialPrice = MyMoneyParse(Request.Form["special_price"]);
		string dtEnd = Request.Form["special_price_end_date"];
		
		id = supplier + supplier_code;
		sc = " UPDATE code_relations SET ";
		sc += " supplier = N'" + EncodeQuote(supplier) + "' ";
		sc += ", supplier_code = N'" + EncodeQuote(supplier_code) + "' ";
		sc += ", id = N'" + EncodeQuote(id) + "' ";
		sc += ", name = N'" + name + "' ";
		sc += ", name_cn = N'" + name_cn + "' ";
		sc += ", brand = N'" + brand + "' ";
		sc += ", cat = N'" + cat + "' ";
		sc += ", s_cat = N'" + s_cat + "' ";
		sc += ", ss_cat = N'" + ss_cat + "' ";		
		sc += ", average_cost = "+ avg_cost +"";
		sc += ", price1 = " + dPrice1;
		sc += ", price2 = " + dPrice2;
		sc += ", price3 = " + dPrice3;
		sc += ", price4 = " + dPrice4;
		sc += ", price5 = " + dPrice5;
		sc += ", price6 = " + dPrice6;		
		sc += ", price7 = " + dPrice7;
		sc += ", price8 = " + dPrice8;
		sc += ", price9 = " + dPrice9;
		sc += ", level_price0 = '"+ level_price0 +"'";
		
		for(i=0; i<dls; i++)
		{
			string ii = (i+1).ToString();
			sc +=", level_price" + ii + " = " + level_price[i];
		}
		sc += ", stock_location = '" + stock_location +"'";
		sc += ", barcode = '" + EncodeQuote(barcode) + "' ";
		sc += ", low_stock = " + low_stock;
		sc += ", qty_break1 = " + nQty1.ToString();
		//always set the special price back to null
		sc += ", special_price = null ";
		if(package_barcode1 != "" && package_qty1 != "0" && package_price1 != "0")
		{
			sc += ", package_barcode1 = '" + package_barcode1 + "' ";
			sc += ", package_qty1 = " + package_qty1;
			sc += ", package_price1 = " + package_price1;
		}
		else
			sc += ", package_barcode1 = null, package_qty1 = null, package_price1 = null ";
		if(package_barcode2 != "" && package_qty2 != "0" && package_price2 != "0")
		{
			sc += ", package_barcode2 = '" + package_barcode2 + "' ";
			sc += ", package_qty2 = " + package_qty2;
			sc += ", package_price2 = " + package_price2;
		}
		else
			sc += ", package_barcode2 = null, package_qty2 = null, package_price2 = null ";
		if(package_barcode3 != "" && package_qty3 != "0" && package_price3 != "0")
		{
			sc += ", package_barcode3 = '" + package_barcode3 + "' ";
			sc += ", package_qty3 = " + package_qty3;
			sc += ", package_price3 = " + package_price3;
		}
		else
			sc += ", package_barcode3 = null, package_qty3 = null, package_price3 = null ";
/*		for(i=0; i<qbs; i++)
		{
			string ii = (i+1).ToString();
			sc += ", qty_break" + ii + " = " + qb[i];
			sc += ", qty_break_price" + ii + " = " + qty_price[i];
		}
*/		sc += ", qpos_qty_break = " + qpos_qty_break;
		sc += ", moq = '" + moq.ToString() + "' ";
		sc += ", inner_pack = " + inner_pack + " ";
		sc += ", inactive = " + sInactive + " ";
		sc += ", new_item = " + new_item;
		sc += ", hidden ='"+ hidden_item+"'";
		sc += " WHERE code=" + code;
//DEBUG("sc =", sc);
//return false;
		if(skip == "on")
		{
			sc += " delete from  product  where code = "+ code;
			sc += " UPDATE code_relations set skip =1 WHERE code = "+ code;
			sc += " IF NOT EXISTS (SELECT id FROM product_skip WHERE id = '" + id +"' ) INSERT INTO product_skip (id) VALUES('" + id + "')";
			
		}
		if(skip == "" || skip == null || skip == "0")
		{
			sc += " IF NOT EXISTS(SELECT code FROM product WHERE code ="+ code +" )";
			sc += " INSERT INTO product(code, supplier, supplier_code, name, brand, cat, s_cat, ss_cat, price, supplier_price) VALUES( ";
			sc += code + ", '" + EncodeQuote(supplier) + "', '" + EncodeQuote(supplier_code) + "' ";
			sc += ", N'" + EncodeQuote(name) + "' ";
			sc += ", N'" + EncodeQuote(brand) + "' ";
			sc += ", N'" + EncodeQuote(cat) + "' ";
			sc += ", N'" + EncodeQuote(s_cat) + "' ";
			sc += ", N'" + EncodeQuote(ss_cat) + "' ";
			sc += ", " + dPrice1 + ", " + supplier_price;
			sc += ") ";
			sc += " ELSE ";
			sc += " UPDATE product SET ";
			sc += " name = N'" + name + "' ";
			sc += ", brand = N'" + brand + "' ";
			sc += ", cat = N'" + cat + "' ";
			sc += ", s_cat = N'" + s_cat + "' ";
			sc += ", ss_cat = N'" + ss_cat + "' ";			
		    sc += ", supplier_price = " + supplier_price ;
			sc += ", supplier_code =N'"+supplier_code+"'";
			sc += ", eta = '"+ EncodeQuote(eta) +"'";
			sc += " WHERE code = " + code;
			sc += " UPDATE code_relations set skip=0 where code = "+ code;
			sc += " DELETE FROM product_skip WHERE id = '" + id + "' ";

			if(is_service == "0")
			{
				sc += " IF NOT EXISTS(SELECT code FROM stock_qty WHERE code = "+ code +" ) ";
				sc += " INSERT INTO stock_qty (code, qty, supplier_price, average_cost, branch_id)  ";
				sc += " SELECT "+ code +", 0, "+ supplier_price +", "+ supplier_price +", id  FROM branch WHERE 1=1 ";
			}
			
		}
//DEBUG("sc =",sc);		
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception esc) 
		{
			ShowExp(sc, esc);
			return false;
		}
		if(skip == "" || skip == null || skip == "0")
		{

			if(old_average_cost != average_cost)
					sc = AddAVGCostLog(code, "Updated: average cost had been changed", old_average_cost, average_cost, "");
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myConnection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception esc) 
			{				
				//ShowExp(sc, esc);				
				string err = esc.ToString().ToLower();	
				myConnection.Close(); //close it first
				if(err.IndexOf("invalid object name 'avg_cost_log'") >= 0)
				{
					myConnection.Close(); //close it first
					string ssc = @"				
					CREATE TABLE [dbo].[avg_cost_log] (
						[id] [bigint] IDENTITY (1, 1) NOT FOR REPLICATION  NOT NULL ,
						[code] [int] NOT NULL ,
						[last_avg_cost] [money] NOT NULL ,
						[new_avg_cost] [money] NOT NULL ,
						[input_by] [int] NOT NULL ,
						[input_date] [datetime] NOT NULL ,
						[purchase_id] [int] NULL ,
						[comments] [varchar] (2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
					) ON [PRIMARY]
					

					ALTER TABLE [dbo].[avg_cost_log] ADD 
						CONSTRAINT [DF_avg_cost_log_input_date] DEFAULT (getdate()) FOR [input_date]
					
					";
			
					try
					{
						myCommand = new SqlCommand(ssc);
						myCommand.Connection = myConnection;
						myCommand.Connection.Open();
						myCommand.ExecuteNonQuery();
						myCommand.Connection.Close();
					}
					catch(Exception er)
					{			
						//DEBUG("er =", er.ToString());
						//return false;
					}
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"] +" \">");
					return false;
				}
			}
		}
		if(bSpecial != bSpecial_old)
		{
			if(bSpecial) //do insert
			{
				bRet = AddSpecial(code);
				if(bRet)
					bRet = SetSpecialPrice(code, dSpecialPrice, dPrice1, dtEnd);
			}
			else //do delete
			{
				bRet = RemoveSpecial(code);
				if(bRet)
					bRet = EndSpecialPrice(code);
			}
		}
		else if(bSpecial) //update special price and end date
		{
			bRet = UpdateSpecialPriceAndDate(code, dSpecialPrice, dtEnd);
		}
		
		if(MyIntParse(GetSiteSettings("dealer_levels")) <= 0)
			return bRet;

		//update dealer_prices
		sb.Append(" SET DATEFORMAT dmy ");
		sb.Append(" UPDATE code_relations SET code=");
		sb.Append(code);
		sb.Append(", supplier_price=");
		sb.Append(dsupplier_price);
		sb.Append(", average_cost=");
		sb.Append(average_cost);
		sb.Append(", rate=");
		sb.Append(dRate);
		if(currency != "")
			sb.Append(", currency='" + currency + "'");
		if(exchange_rate != "")
			sb.Append(", exchange_rate=" + exchange_rate);
		if(foreign_supplier_price != "")
			sb.Append(", foreign_supplier_price=" + foreign_supplier_price);
		if(freight != "")
			sb.Append(", nzd_freight=" + freight);
	
		sb.Append(", is_service=" + is_service);

		for(i=0; i<dls; i++)
		{
			string ii = (i+1).ToString();
			sb.Append(", level_rate" + ii + " = " + level_rate[i]);
		}
		for(i=0; i<qbs; i++)
		{
			string ii = (i+1).ToString();
			sb.Append(", qty_break" + ii + " = " + qb[i]);
			sb.Append(", qty_break_discount" + ii + " = " + qty_discount[i]);
		}
		sb.Append(", manual_cost_frd=" + manual_cost_frd + ", manual_exchange_rate=" + manual_exchange_rate);
		sb.Append(", manual_cost_nzd=" + manual_cost_nzd);
		sb.Append(", rrp=" + drrp);
		sb.Append(", weight=" + dWeight);
		sb.Append(", stock_location='" + stock_location + "' ");
		sb.Append(", barcode='" + EncodeQuote(barcode) + "' ");
		sb.Append(", qpos_qty_break = " + qpos_qty_break +"");
	//	if(expire_date != "")
	//		sb.Append(", expire_date='" + EncodeQuote(expire_date) + "' ");
		sb.Append(" WHERE id='");
		sb.Append(id);
		sb.Append("'");
		try
		{
			myCommand = new SqlCommand(sb.ToString());
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sb.ToString(), e);
			return false;
		}
		SetItemUpdatedFlag(code);
		return true;
	}

	//update code_relations
	sb.Append(" SET DATEFORMAT dmy ");
	sb.Append(" UPDATE code_relations SET code=");
	sb.Append(code);
	sb.Append(", name=N'");
	sb.Append(name);
	sb.Append("', brand=N'");
	sb.Append(brand);
	sb.Append("', cat=N'");
	sb.Append(cat);
	sb.Append("', s_cat=N'");
	sb.Append(s_cat);
	sb.Append("', ss_cat=N'");
	sb.Append(ss_cat);
	sb.Append("', hot=");
	sb.Append(hot);
	sb.Append(", skip=" + skip + ", clearance=" + clearance);
	sb.Append(", supplier_price=");
	sb.Append(dsupplier_price);
	sb.Append(", average_cost=");
	sb.Append(average_cost);
	sb.Append(", rate=");
	sb.Append(dRate);
	if(currency != "")
		sb.Append(", currency='" + currency + "'");
	if(exchange_rate != "")
		sb.Append(", exchange_rate=" + exchange_rate);
	if(foreign_supplier_price != "")
		sb.Append(", foreign_supplier_price=" + foreign_supplier_price);
	if(freight != "")
		sb.Append(", nzd_freight=" + freight);

	sb.Append(", is_service=" + is_service);

	for(i=0; i<dls; i++)
	{
		string ii = (i+1).ToString();
		sb.Append(", level_rate" + ii + " = " + level_rate[i]);
		sb.Append(", level_price" + ii + " = " + level_price[i]);
	}
	for(i=0; i<qbs; i++)
	{
		string ii = (i+1).ToString();
		sb.Append(", qty_break" + ii + " = " + qb[i]);
		sb.Append(", qty_break_discount" + ii + " = " + qty_discount[i]);
	}
	sb.Append(", manual_cost_frd=" + manual_cost_frd + ", manual_exchange_rate=" + manual_exchange_rate);
	sb.Append(", manual_cost_nzd=" + manual_cost_nzd);
	sb.Append(", rrp=" + drrp);
	sb.Append(", weight=" + dWeight);
	sb.Append(", stock_location='" + stock_location + "' ");
	sb.Append(", barcode='" + EncodeQuote(barcode) + "' ");
//	if(expire_date != "")
//		sb.Append(", expire_date='" + EncodeQuote(expire_date) + "' ");
	sb.Append(" WHERE id='");
	sb.Append(id);
	sb.Append("'");
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

	sb.Remove(0, sb.Length);
	if(skip != old_skip)
	{
		if(skip=="1")
		{
			//add to to product_skip table
			sc = " IF NOT EXISTS (SELECT id FROM product_skip WHERE id = '"+ id +"' ) ";
			sc += " INSERT INTO product_skip (id, stock, eta, supplier_price, price) VALUES('";
			sc += id + "', " + stock + ", '" + eta + "', " + supplier_price + ", " + price + ")";
			sc += " ELSE ";
			sc += " UPDATE product_skip set stock = "+ stock +" ";		
			sc += " , eta = '"+ eta +"' ";
			sc += " , supplier_price = "+ supplier_price +" ";
			sc += " , price = "+ price +"";
			sc += " WHERE id = '"+ id +"' ";
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
			
			//delete from product table
			sb.Append("DELETE FROM product WHERE code=" + old_code);
		}
		else
		{
			//can we insert it back to product table at this time?  maybe this one was 
			//discontinued a long time ago so simply update code_relations talbe, or better
			//wait it comes back at next autoupdate? DW 27.Jun.2002
			sc = " IF NOT EXISTS (SELECT code FROM product WHERE code=" + code + " ) ";
			sc += " INSERT INTO product (supplier, supplier_code, code, name, brand, cat, s_cat, ss_cat, supplier_price, ";
			sc += "price, stock, eta, hot, price_age) ";
			sc += "VALUES('" + supplier + "', '" + supplier_code + "', " + code + ", '" + name;
			sc += "', '" + brand + "', '" + cat + "', '" + s_cat + "', '" + ss_cat;
			sc += "', " + supplier_price + ", " + price + ", " + stock + ", '" + eta + "', ";
			sc += hot + ", GETDATE())";
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

			//remove form product_skip
			sb.Append("DELETE FROM product_skip WHERE id='" + id + "'");
		}
	}
	else
	{
		if(skip == "0")
		{
			//update product (live update)
			sb.Append("UPDATE product SET code='");
			sb.Append(code);
			sb.Append("', name=N'");
			sb.Append(name);
			sb.Append("', brand=N'");
			sb.Append(brand);
			sb.Append("', cat=N'");
			sb.Append(cat);
			sb.Append("', s_cat=N'");
			sb.Append(s_cat);
			sb.Append("', ss_cat=N'");
			sb.Append(ss_cat);
			sb.Append("', supplier_price=");
			sb.Append(dsupplier_price);
			sb.Append(", price=");
			sb.Append(dPrice);
//			sb.Append(", stock=");
//			sb.Append(stock);
			sb.Append(", eta='");
			sb.Append(eta);
			sb.Append("', hot=");
			sb.Append(hot);
			sb.Append(" WHERE code=");
			sb.Append(old_code);
		}
		else
		{
			//update product_skip (live update)
			sb.Append("UPDATE product_skip SET eta='");
			sb.Append(eta);
			sb.Append("', supplier_price=");
			sb.Append(supplier_price);
			sb.Append(", price=");
			sb.Append(price);
			sb.Append(" WHERE id='");
			sb.Append(id);
			sb.Append("'");
		}
	}
//DEBUG("sc=", sb.ToString());
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

	//DO singleout check
	if(code != old_code)
	{
		bRet = SingleOut(code);
		m_code = code;
	}
	else if(skip != "1")
	{
		//check kit
		Kit_AutoUpdatePrice(code);
	}

	if(bSpecial != bSpecial_old)
	{
		if(bSpecial) //do insert
			bRet = AddSpecial(code);
		else //do delete
			bRet = RemoveSpecial(code);
	}
	SetItemUpdatedFlag(code);
	return bRet;
}

bool CrossExists(string cat, string s_cat, string ss_cat)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM cat_cross WHERE cat='");
	sb.Append(cat);
	sb.Append("' AND s_cat='");
	sb.Append(s_cat);
	sb.Append("' AND ss_cat='");
	sb.Append(ss_cat);
	sb.Append("' AND code=");
	sb.Append(m_code);
	try
	{
		DataSet dsex = new DataSet();
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		if(myAdapter.Fill(dsex) > 0)
			return true;

	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return true; //return true to stop adding
	}
	return false;
}

bool AddCross()
{
	string cat = EncodeQuote(Request.Form["cat"]);
	string s_cat = EncodeQuote(Request.Form["s_cat"]);
	string ss_cat = EncodeQuote(Request.Form["ss_cat"]);
	if(CrossExists(cat, s_cat, ss_cat))
		return true;
	StringBuilder sb = new StringBuilder();
	sb.Append("INSERT INTO cat_cross (cat, s_cat, ss_cat, code) VALUES('");
	sb.Append(cat);
	sb.Append("', '");
	sb.Append(s_cat);
	sb.Append("', '");
	sb.Append(ss_cat);
	sb.Append("', ");
	sb.Append(m_code);
	sb.Append(")");
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

bool DeleteCross()
{
	string cat = EncodeQuote(Request.QueryString["c"]);
	string s_cat = EncodeQuote(Request.QueryString["s"]);
	string ss_cat = EncodeQuote(Request.QueryString["ss"]);
	StringBuilder sb = new StringBuilder();
	sb.Append("DELETE FROM cat_cross WHERE code=" + m_code + " AND cat='");
	sb.Append(cat);
	sb.Append("' AND s_cat='");
	sb.Append(s_cat);
	sb.Append("' AND ss_cat='");
	sb.Append(ss_cat);
	sb.Append("'");
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("<br><center><h3>Edit Product</h3></center>");
//	Response.Write(sb.ToString());
	sb.Append("<table><tr><td colspan=5>");
	sb.Append("<img src=r.gif><a href=liveedit.aspx?code=" + m_code + " class=x>"+Lang("Edit Product")+"</a> ");
	sb.Append(" <img src=r.gif><a href=ep.aspx?code=" + m_code + " class=x>"+Lang("Edit Specifications")+"</a> ");
	sb.Append(" <img src=r.gif><a href=addpic.aspx?code=" + m_code + " class=x>"+Lang("Edit Photo")+"</a> ");
//	sb.Append(" <img src=r.gif><a href=stocktrace.aspx?p=0&c=" + m_code + " class=x>Trace Stock</a> ");
	
	//*** change supplier code for product in the entire database, include code_relations, product, product_skip, sales, purchase_item table

	//if(Session["email"].ToString() == "tee@eznz.com" || Session["email"].ToString() == "darcy@eznz.com" || Session["email"].ToString() == "neo@eznz.com")
	if(m_bAllowChangeSupplierCode && m_bSecurityCheckAccessAllow)
	{
	//	sb.Append(" <img src=r.gif><a title='change supplier code in the entire database' ");
	//	sb.Append(" href=\"javascript:suppchange_window=window.open('chsupcd.aspx?e=new&cd="+ m_code +"', '','scrollbars=1, menubar=0, resizable=1');suppchange_window.focus();\" class=x>"+Lang("Change Supplier Code")+"</a> " );
	}
	//sb.Append("</td></tr><tr><td>");
	sb.Append("</td></tr></table>");
	//sb.Append("<center><h3>Edit Product</h3></center>");
	Response.Write(sb.ToString());

	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>"+Lang("Edit Product")+"</b><font color=red><b>");	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");
	Response.Write("<form name=quicksearch  >");
	Response.Write(" <input type=text name=s  style=\" height=30; width=300; font:bold 20px arial\">");
	Response.Write(" <input type=button  value='Quick Search' onclick=\"window.location=('liveedit.aspx?s='+document.quicksearch.s.value)\"  style=\" height=30; width=150; font:bold 20px arial\" >");
	Response.Write("</form>");
	Response.Write("</td></tr>");
	
	Response.Flush();
}

void WriteFooter()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("</form>");
	Response.Write(sb.ToString());
}

bool GetCrossReferences()
{
	int rows = 0;
	string sc = "SELECT b.name, cb.inactvie FROM cat_cross WHERE code=" + m_code + " ORDER BY cat, s_cat, ss_cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cross");
//DEBUG("sc=", sc);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//	if(rows <= 0)
//		return true;
	if(dsAEV.Tables["cat"].Rows.Count <= 0)
		return true;

	if(m_co == "-1")
		m_co = dsAEV.Tables["cat"].Rows[0][0].ToString(); 
	sc = "cat = N'" +  EncodeQuote(m_co) + "' ORDER BY s_cat";
//DEBUG("sc=", sc);
	if(!ECATGetAllExistsValues("s_cat", sc, false))
		return false;

	if(m_so == "-1")
		m_so = dsAEV.Tables["s_cat"].Rows[0][0].ToString();
	sc = "cat = N'" + EncodeQuote(m_co) + "' AND s_cat = N'" + EncodeQuote(m_so) + "' ORDER BY ss_cat";
	if(!ECATGetAllExistsValues("ss_cat", sc, false))
		return false;

	return true;
}

string PrintCrossReferences()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<a name=cross>");
	sb.Append("<form name=form2 action=liveedit.aspx?action=add&code=" + m_code + " method=post>");
	sb.Append("<table width=100%  cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr height=10 style='color:white;background-color:#666696;font-weight:bold;'><td>CAT</td><td>S_CAT</td><td>SS_CAT</td><td>"+Lang("ACTION")+"</td></tr>");
	bool alterColor = true;
	for(int i=0; i<dst.Tables["cross"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["cross"].Rows[i];
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		alterColor = !alterColor;
		sb.Append("<tr");
		if(alterColor)
			sb.Append(" bgcolor=#EEEEEE");
		sb.Append("><td>");
		sb.Append(cat);
		sb.Append("</td><td>");
		sb.Append(s_cat);
		sb.Append("</td><td>");
		sb.Append(ss_cat);
		sb.Append("</td><td align=right><a href=liveedit.aspx?action=delete&code=" + m_code + "&c=");
		sb.Append(HttpUtility.UrlEncode(cat));
		sb.Append("&s=");
		sb.Append(HttpUtility.UrlEncode(s_cat));
		sb.Append("&ss=");
		sb.Append(HttpUtility.UrlEncode(ss_cat));
		sb.Append(">DELETE</a></td></tr>");
	}
	sb.Append("<tr><td>&nbsp;</td></tr><tr><td>");
	sb.Append(PrintSelectionRowForCross("cat", m_co));
	sb.Append("</td><td>");
	sb.Append(PrintSelectionRowForCross("s_cat", m_so));
	sb.Append("</td><td>");
	sb.Append(PrintSelectionRowForCross("ss_cat", ""));
	sb.Append("</td><td align=right><input type=submit value=' Add ' " + Session["button_style"] + "></td></tr>");
	sb.Append("</table</form>");
	return sb.ToString();
}

string BranchPrice()
{
// DEBUG("m_sBP ", m_sBP);
	return m_sBP;
}

bool MyDrawTable()
{
	if(dst.Tables["product"].Rows.Count <=0 )
	{
		Response.Write("<h3>Product Not Found</h3>");
		return true;
	}

	bool bRet = true;
	Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");
	
	//print edit table
	Response.Write("<form name=form1 action=liveedit.aspx?action=update&code=");
	Response.Write(m_code);
	Response.Write(" method=post>\r\n");
	Response.Write("<input type=hidden name=m_sql value='");
	Response.Write(m_sql);
	Response.Write("'>\r\n");
	
	Response.Write("<table width=100%>");
//	Response.Write("<tr height=10 style='color:white;background-color:#666696;font-weight:bold;'><td>name</td><td colspan=2>value</td></tr>");
	string s = "";
	DataRow dr;

//DEBUG("dst.Tables[product].Rows.Count=", dst.Tables["product"].Rows.Count);
	for(int i=0; i<dst.Tables["product"].Rows.Count; i++)
	{
		dr = dst.Tables["product"].Rows[i];
		if(!DrawRow(dr, i))
		{
			bRet = false;
			break;
		}
	}

	Response.Write("<tr bgcolor='#EEEEE'><td colspan=1><a href=ep.aspx?code=" + m_code + " class=x>" + Lang("Edit Details") + "</a> ");
	Response.Write("<a href=addpic.aspx?code=" + m_code + " class=x>"+Lang("Edit Photo")+"</a></td><td align=right>");
	Response.Write("<input type=button class=b value='"+Lang("Calculate")+"' ");
	Response.Write(" onclick=\"Calculate();\">");
	Response.Write("<input type=submit name=update title= 'Update All' value='" + Lang("Update") + "' class=b>");
	if(!MyBooleanParse(dst.Tables["product"].Rows[0]["skip"].ToString()))
		Response.Write("<input type=button value='" + Lang("Copy Item") + "' onclick=window.location=('liveedit.aspx?action=copy&code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "') class=b>");
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td>");
	Response.Write(BranchPrice());
	Response.Write("</td></tr>");
	Response.Write("<tr bgcolor='#EEEEE'><td colspan=1><a href=ep.aspx?code=" + m_code + " class=x>" + Lang("Edit Details") + "</a> ");
	Response.Write("<a href=addpic.aspx?code=" + m_code + " class=x>"+Lang("Edit Photo")+"</a></td><td align=right>");
	Response.Write("<input type=button class=b value='"+Lang("Calculate")+"' ");
	Response.Write(" onclick=\"Calculate();\">");
	Response.Write("<input type=submit name=update title= 'Update All' value='" + Lang("Update") + "' class=b>");
	if(!MyBooleanParse(dst.Tables["product"].Rows[0]["skip"].ToString()))
		Response.Write("<input type=button value='" + Lang("Copy Item") + "' onclick=window.location=('liveedit.aspx?action=copy&code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "') class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table></form>\r\n");
	
	if(m_bSimpleInterface || m_bFixedPrices)
		return true;

	Response.Write("</td></tr><tr><td><b>Cross References</b></td></tr>");
	Response.Write("<tr><td>");

	if(!GetCrossReferences())
	{
		Response.Write("</td></tr></table>");
		return false;
	}

	Response.Write(PrintCrossReferences());
	Response.Write("</td></tr>");
	Response.Write("</table>\r\n");

	

	return bRet;
}

void DrawTableHeader()
{
	StringBuilder sb = new StringBuilder();
	Response.Write(sb.ToString());
	Response.Flush();
}

bool DrawRow(DataRow dr, int row)
{
	string code = dr["code"].ToString();
//	string sku = dr["sku_code"].ToString();
	string id = dr["id"].ToString();
	string ssn = dr["supplier"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string name = dr["name"].ToString();
	string name_cn = dr["name_cn"].ToString();
	string brand = dr["brand"].ToString();
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	string ss_cat = dr["ss_cat"].ToString();
	string supplier_price = dr["supplier_price"].ToString();
	string rate = dr["rate"].ToString();
//	string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 2).ToString();
	string stock = dr["stock"].ToString();
	string eta = dr["eta"].ToString();
	string hot = dr["hot"].ToString();
	string skip = dr["skip"].ToString();
	string rrp = dr["rrp"].ToString();
	string special = dr["special"].ToString();
	string clearance = dr["clearance"].ToString();
	string inactive = dr["inactive"].ToString();
	string index = row.ToString();
	string currency = dr["currency"].ToString();
	string weight = dr["weight"].ToString();
	string stock_location = dr["stock_location"].ToString();
	string low_stock = dr["low_stock"].ToString();
	string moq = dr["moq"].ToString();
	string inner_pack = dr["inner_pack"].ToString();
	string barcode = dr["barcode"].ToString();
	string sexpire = dr["expire_date"].ToString();
	string dealerLevel1Price = dr["level_price0"].ToString();

	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	string expire_date = "";
	Trim(ref sexpire);
//	if(sexpire != "")
//		expire_date = DateTime.Parse(sexpire, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault).ToString("dd-MM-yyyy");

//	currency = GetEnumValue("currency", currency);
	bool bis_service = bool.Parse(dr["is_service"].ToString());

	supplier_price = Math.Round(MyDoubleParse(supplier_price), 2).ToString();
	string average_cost = dr["average_cost"].ToString();
	if(average_cost == "" || average_cost == "0")
		average_cost = supplier_price;
	average_cost = Math.Round(MyDoubleParse(average_cost), 2).ToString();
	
	string manual_exchange_rate = dr["manual_exchange_rate"].ToString();
	if(manual_exchange_rate == "")
		manual_exchange_rate = "1";
	string manual_cost_frd = Math.Round(MyDoubleParse(dr["manual_cost_frd"].ToString()), 2).ToString();
	string manual_cost_nzd = Math.Round(MyDoubleParse(dr["manual_cost_nzd"].ToString()), 2).ToString();

	string exchange_rate = dr["exchange_rate"].ToString();
	string foreign_supplier_price = Math.Round(MyDoubleParse(dr["foreign_supplier_price"].ToString()), 2).ToString();
	string nzd_freight = dr["nzd_freight"].ToString();

	double dmcn = MyDoubleParse(manual_cost_nzd);
	double dmer = MyDoubleParse(manual_exchange_rate);
	double dbr = MyDoubleParse(rate);
//	string price = Math.Round( dmcn / dmer * dbr + MyDoubleParse(nzd_freight), 2).ToString();
	string price = Math.Round( dmcn * dbr + MyDoubleParse(nzd_freight), 2).ToString();

	string sSkip = "0";
	if(String.Compare(skip, "true", true) == 0)
		sSkip = "1";
	string sClearance = "0";
	if(String.Compare(clearance, "true", true) == 0)
		sClearance = "1";
	string sInactive = "0";
	if(MyBooleanParse(inactive))
		sInactive = "1";
	bool bNewItem = MyBooleanParse(dr["new_item"].ToString());
	bHidden = MyBooleanParse(dr["hidden"].ToString());
//DEBUG("supplier_code=", supplier_code);
	Trim(ref name);
	Trim(ref brand);
	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	if(!ECATGetAllExistsValues("brand", "brand<>'-1' ORDER BY brand", false))
		return false ;
	if(!ECATGetAllExistsValues("cat", "cat<>'Brands' ORDER BY cat", false))
		return false;

	string sc = "";
	if(m_ck == "-1")
		sc = "cat = N'" + EncodeQuote(cat) + "' ORDER BY s_cat";
	else
		sc = "cat = N'" + EncodeQuote(m_ck) + "' ORDER BY s_cat";
	if(!ECATGetAllExistsValues("s_cat", sc, false))
		return false;

	if(m_ck == "-1")
	{
		if(m_sk == "-1")
			sc = "cat = N'" + EncodeQuote(cat) + "' AND s_cat = N'" + EncodeQuote(s_cat) + "' ORDER BY ss_cat";
		else
			sc = "cat = N'" + EncodeQuote(cat) + "' AND s_cat = N'" + EncodeQuote(m_sk) + "' ORDER BY ss_cat";
	}
	else
	{
		if(m_sk == "-1")
			sc = "cat = N'" + EncodeQuote(m_ck) + "' AND s_cat = N'" + EncodeQuote(s_cat) + "' ORDER BY ss_cat";
		else
			sc = "cat = N'" + EncodeQuote(m_ck) + "' AND s_cat = N'" + EncodeQuote(m_sk) + "' ORDER BY ss_cat";
	}
//DEBUG("sc=", sc);
	if(!ECATGetAllExistsValues("ss_cat", sc, false))
		return false;
	
	Response.Write("<input type=hidden name=old_code");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(code);
	Response.Write("'>\r\n");

	Response.Write("<input type=hidden name=old_cat");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(cat);
	Response.Write("'>\r\n");

	Response.Write("<input type=hidden name=old_s_cat");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(s_cat);
	Response.Write("'>\r\n");

	Response.Write("<input type=hidden name=old_ss_cat");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(ss_cat);
	Response.Write("'>\r\n");

	Response.Write("<input type=hidden name=old_hot");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(hot);
	Response.Write("'>\r\n");

	Response.Write("<input type=hidden name=supplier1");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(supplier);
	Response.Write("'>\r\n");

	Response.Write("<input type=hidden name=price_old");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(price);
	Response.Write("'>\r\n");
	
	Response.Write("<input type=hidden name=old_barcode");
	Response.Write(" value='");
	Response.Write(barcode);
	Response.Write("'>\r\n");

//	Response.Write("<input type=hidden name=rate" + index + " value=" + dr["rate"].ToString() + ">");
	Response.Write("<input type=hidden name=supplier_price_old" + index + " value=" + dr["supplier_price"].ToString() + ">");

	string keyEnter = "onKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"";
	int cols = 2;
	
	Response.Write("<tr bgcolor='#EEEEE'><td colspan='"+ cols +"'><font size=3><b>"+Lang("Product Details")+"</b></font></td></tr>");
	Response.Write("<tr><td colspan='"+ cols +"'>");
	Response.Write("<a class=thumbnail href='"+ GetProductImgSrc(code) +"' target=_blank><img height=64 src='"+ GetProductImgSrc(code) +"'>");
	Response.Write("<span width=100%><img src='"+ GetProductImgSrc(code) +"'></span>");
	Response.Write("</a></td></tr>");
	Response.Write("<tr><td valign=top width='"+ rowWidth +"'>");
	Response.Write("<table width='100%' align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><td><font color=red><b>CODE/SSN/M-PN</b></font></td><td>" + code + "/" + ssn + "/" + supplier_code);
	Response.Write("<input type=hidden name=code" + index + " value='" + code + "'>");
	Response.Write("<input type=hidden name=ssh" + index + " value='" + supplier + "'>");
	Response.Write("<input type=hidden name=id" + index + " value='" + id + "'> &nbsp;&nbsp; ");
//	Response.Write("<b>SKU Code : </b><input type=text name=sku" + index + " value='" + sku + "'>");
//	Response.Write("</td></tr>");
	
    Response.Write("<tr><td><b>"+Lang("SupplierName")+"</b></td><td >");
    Response.Write("<select name=supplier" + index + " onclick=window.location=('" + m_url + "&search=1')>");
	
	if(m_supplierID != "0")
	{
	    if(m_supplierID == "-2")
		    Response.Write("<option value='" + m_supplierID + "' selected><b>Other</b></option>");
		else
			Response.Write("<option value='" + m_supplierID + "' selected><b>" + m_supplierName + "</b></option>");
	}
    else
        Response.Write("<option value=0>" + showSupplierName(supplier) +"</option>");
	Response.Write("</select>");
	Response.Write("</td></tr>");
	
    if(supplier_code == "")
		supplier_code = id;
	
	
	
	Response.Write("<tr><td><B>Supplier Code</b></td><td ><input type=text size=25  name=supplier_code");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(supplier_code);
	Response.Write("'></td></tr>");

	Response.Write("<tr><td><B>"+Lang("Description")+"</b></td><td ><input type=text size=50  name=name");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(name);
	Response.Write("'></td></tr>");
	Response.Write("<tr><td><B>"+Lang("Description") + "CN</b></td><td ><input type=text size=50  name=name_cn");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(name_cn);
	Response.Write("'></td></tr>");

	//ETA
	Response.Write("<tr><td><b>"+Lang("ETA")+":</b></td><td ><input type=text size=50 maxlength=50 name=eta");
	Response.Write(index + " value=\"" + eta + "\" "+ keyEnter +">");
	Response.Write("</td></tr>");	

	//BARCODE
	Response.Write("<tr><td><b>"+Lang("Single Barcode:")+"</b></td><td ><input type=text maxlength=20 size=30 name=barcode ");
	Response.Write(" value='" + barcode + "' "+ keyEnter +">");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>MOQ</b></td><td><input type=text size=5 maxlength=5 style=\"text-align:right;background-color:#EEEEEE\" name=moq ");
	Response.Write(" value='" + moq + "'  " + keyEnter + "></td></tr>");

	Response.Write("<tr><td><b>Inner Pack</b></td><td><input type=text size=5 maxlength=5 style=\"text-align:right;background-color:#EEEEEE\" name=inner_pack ");
	Response.Write(" value='" + inner_pack + "'  " + keyEnter + "></td></tr>");

	Response.Write("<tr><td><b>"+Lang("Outer Pack")+"</b></td><td><input type=text size=5 maxlength=5 style=text-align:right name=weight" + index);
	Response.Write(" value='"+ weight +"'  "+ keyEnter +"></td></tr>");	
	
	Response.Write("</table></td>");
		
	Response.Write("<td valign=top> ");
	Response.Write("<table width='100%' align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>"+Lang("Brand")+"</b></td><td>");
//******** BRAND AND CATEGORIES ********
	Response.Write(PrintSelectionRow("brand", brand, index));

	Response.Write("</td></tr><tr><td><b>"+Lang("1st Category")+"</b></td><td>");
	Response.Write(PrintSelectionRow("cat", cat, index));

	Response.Write("</td></tr><tr><td><b>"+Lang("2nd Category")+"</b></td><td>");
	Response.Write(PrintSelectionRow("s_cat", s_cat, index));
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>"+Lang("3rd Category")+"</b></td><td>");
	Response.Write(PrintSelectionRow("ss_cat", ss_cat, index));
	Response.Write("</td></tr>");

	///status
	Response.Write("<tr><td><b>"+Lang("Status")+" </b></td><td colspan=2 nowrap>");

	Response.Write("<input type=checkbox name=skip ");
	//Response.Write("<b>Phased Out :</b> &nbsp&nbsp&nbsp&nbsp; <input type=checkbox name=skip ");
	if(sSkip == "1" || sSkip == "True")
		Response.Write(" checked");
	else
		Response.Write(" unchecked ");
	Response.Write(">" + Lang("Phased Out") + " ");
	Response.Write("<input type=hidden name=old_skip" + index.ToString() + " >");
	Response.Write("<input type=checkbox name=is_service ");
	if(bis_service)
		Response.Write(" checked");
	Response.Write(" ><font color=red><b>"+Lang("Service")+"</b></font>");

	Response.Write("<input type=checkbox name=inactive value=1 ");
	if(sInactive == "1")
		Response.Write(" checked");
	Response.Write("><b>Not Available</b> ");
	Response.Write(" &nbsp; <input type=checkbox name=new_item value=1 ");
	if(bNewItem)
		Response.Write(" checked");
	Response.Write("><b>New Item</b> ");
	Response.Write(" <input type=checkbox name=hidden_item value=1 ");
	if(bHidden)
		Response.Write(" checked");
	Response.Write(">Hidden On Public");
	Response.Write("</td></tr>");
	//location.
	Response.Write("<tr><td><b>"+Lang("Stock Location")+"</b></td><td>");
	//Response.Write(GetItemLocationOnEdit(code));
	Response.Write("<input type=text size=10 maxlength=25 name=stock_location style=text-align:right maxlength=48 value='" + stock_location + "' "+ keyEnter +">");
    Response.Write("</td>");
	//branch stock qty, otherwise will get the first branch stock
	Response.Write("<tr>");
	Response.Write("<td><b>"+Lang("Stock QTY")+"</b></td><td><input type=text size=5 maxlength=5 style=\"text-align:right;background-color:#EEEEEE\" readonly=true name=qty" + index);
	Response.Write(" value='"+ stock +"'  "+ keyEnter +">");
    Response.Write("</td></tr>");

    Response.Write("<tr><td><b><font color=green>"+Lang("Set Warning Low Stock Qty")+" </b></td><td>");
	Response.Write("<input type=text size=5 name=low_stock value=" + low_stock + " style=text-align:right;>");
	Response.Write(" (" +Lang("Individual Branch Stock Level")+")</td></tr>");

	Response.Write("</table></td></tr>");
//******** BRAND AND CATEGORIES END HERE ********
	

	//Response.Write("</table>");
	
	if(m_bSimpleInterface)
	{
		PrintSimpleInterface(dr, row);
		return true;
	}
	else if(m_bFixedPrices)
	{
		PrintFixedPricesInterface(dr, row);
		if(MyIntParse(GetSiteSettings("dealer_levels")) <= 0)
			return true;
	}
	
	Response.Write("<tr bgcolor='#EEEEE'><td colspan='"+ cols +"' align=right><input type=submit name=update title= 'Update All' value='"+Lang("Update")+"' " + Session["button_style"]);
//	Response.Write(" onclick=\"if(document.form1.barcode.value == ''){window.alert('Please enter barcode');return false;}\"");
	Response.Write(">");
	if(!bool.Parse(dst.Tables["product"].Rows[0]["skip"].ToString()))
		Response.Write("<input type=button value='"+Lang("Copy Item")+"' onclick=window.location=('liveedit.aspx?action=copy&code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "') " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("<tr bgcolor='#EEEEE'><td ><font size=3><b>"+Lang("Manual Cost Setting")+"</b></font></td><td><font size=3><b>"+Lang("Last Cost Details")+"</b></font></td></tr>");
	Response.Write("<tr><td valign=top width='"+ rowWidth +"'>");	
	
	Response.Write("<table align=left valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
/*	Response.Write("<tr><td><b>Stock</b></td><td><input type=text size=3 readonly=true style=text-align:right name=stock");
	Response.Write(index);
	Response.Write(" value='");
	Response.Write(stock);

	Response.Write("' "+ keyEnter +"> &nbsp&nbsp&nbsp&nbsp; <b>ETA : </b></td><td><input type=text size=55 name=eta");
	Response.Write(index + " value='" + eta + "' "+ keyEnter +"></td></tr>");

	Response.Write("<tr><td><b>Barcode</b></td><td colspan=2><input type=text size=50 name=barcode ");
	Response.Write(" value='" + barcode + "' "+ keyEnter +"></td></tr>");
//	Response.Write("<tr><td><b>ExpireDate</b></td><td colspan=2><input type=text size=10 name=expire_date ");
//	Response.Write(" value='" + expire_date + "'>(dd-mm-yyyy)</td></tr>");

	Response.Write("<tr><td><b>Status</b></td><td colspan=2>");
//	Response.Write("<table border=1 width=100%><tr><td>");
//	Response.Write("<b>Status : &nbsp&nbsp&nbsp&nbsp; </b>");
	Response.Write("<input type=checkbox name=hot" + index + " value=");
	if(String.Compare(hot, "true", true) == 0)
		Response.Write("1 checked");
	else
		Response.Write("0 unchecked");
	Response.Write(">New &nbsp&nbsp&nbsp&nbsp; <input type=checkbox name=skip" + index + " value=" + sSkip);
	if(sSkip == "1")
		Response.Write(" checked");
	else
		Response.Write(" unchecked");
	Response.Write(" "+ keyEnter +">Phased Out &nbsp&nbsp&nbsp&nbsp; <input type=hidden name=old_skip" + index.ToString() + " value=" + sSkip + ">");
	Response.Write("<input type=checkbox name=special" + index);
	if(special != "0")
		Response.Write(" checked");
	Response.Write(" "+ keyEnter +">Special &nbsp&nbsp&nbsp&nbsp; <input type=hidden name=special_old" + index + " value=");
	if(special != "0")
		Response.Write("on");
	Response.Write(" "+ keyEnter +"><input type=checkbox name=clearance" + index);
	if(sClearance == "1")
		Response.Write(" checked");
	else
		Response.Write(" unchecked");
	Response.Write(">Clearance &nbsp&nbsp&nbsp&nbsp; ");
	
	Response.Write("<input type=checkbox name=is_service ");
	if(bis_service)
		Response.Write(" checked");
	Response.Write(" "+ keyEnter +"><font color=red><b>Service</b></font>");

	Response.Write("</td></tr>\r\n");
*/
		
	Response.Write("<tr><td valign=top>");
	Response.Write("<table align=left valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//manual price
	Response.Write("<tr><td><b>"+Lang("Manual Cost")+"("+Lang("FRD")+") <font color=red><i>*</i></font></b></td><td>");
	Response.Write("<input type=text size=5 style=text-align:right name=manual_cost_frd");
	Response.Write(" onchange=\"CalcManualCost()\" value=" + manual_cost_frd + " "+ keyEnter +"></td></tr>");
	Response.Write("<tr><td><b>"+Lang("Manual Ex-Rate")+"</b></td><td>");
	Response.Write("<input type=text size=5 style=text-align:right name=manual_exrate");
	Response.Write(" onchange=\"CalcManualCost()\" value=" + manual_exchange_rate + " "+ keyEnter +"></td></tr>");
	Response.Write("<td><b>"+Lang("Manual COST")+""+"("+m_sCurrencyName+")"+"</b></td><td>");
	Response.Write("<input type=text size=5 style=text-align:right;background-color:#EEEEEE readonly=true name=manual_cost_nzd");
	Response.Write(" value=" + manual_cost_nzd + " "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("</table>");

	Response.Write("</td><td valign=top>");	
	//bottom rate and price
	Response.Write("<table align=left valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
	Response.Write("<tr>");
	Response.Write("<td><b>"+Lang("Bottom Rate")+"</b></td><td><input type=text size=5 style=text-align:right name=rate" + index);
	Response.Write(" onchange=\"CalcPrice()\" value=" + rate + " "+ keyEnter +"></td></tr>");
	Response.Write("<tr><td><b>"+Lang("Bottom Price")+"</b></td><td><input type=text size=5 style=text-align:right ");
	if(sClearance != "1")
		Response.Write(" readonly=true ");
	Response.Write("  name=price" + index + " value=" + price + " "+ keyEnter +"></td></tr>");
	
	Response.Write("<tr><td><b>"+Lang("RRP")+"</b></td><td><input type=text size=5 style=text-align:right ");
	Response.Write(" name=rrp" + index + " value=" + MyDoubleParse(rrp).ToString() + " "+ keyEnter +"></td>");

	Response.Write("</tr>");
	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</td><td>");
	Response.Write("<table valign=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	if(exchange_rate == "" || exchange_rate == null)
			exchange_rate = "1";
	if(foreign_supplier_price == "0")
	{	
		if(supplier_price != "0" && double.Parse(exchange_rate) > 0)
			foreign_supplier_price = (double.Parse(supplier_price) * double.Parse(exchange_rate)).ToString();
		else
			foreign_supplier_price = (double.Parse(supplier_price) / double.Parse(exchange_rate)).ToString();
	}

	//currency selection
	string rate_usd = GetSiteSettings("exchange_rate_usd", "0.49");
	string rate_aud = GetSiteSettings("exchange_rate_aud", "0.87");
	Response.Write("<input type=hidden name=rate_nzd value=1>");
	Response.Write("<input type=hidden name=rate_usd value=" + rate_usd + ">");
	Response.Write("<input type=hidden name=rate_aud value=" + rate_aud + ">");
	Response.Write("<input type=hidden name=rate_usd_old value=" + rate_usd + ">");
	Response.Write("<input type=hidden name=rate_aud_old value=" + rate_aud + ">");
	Response.Write("<input type=hidden name=currency_name value='" + currency + "'>");
	Response.Write("<tr><td valign=top>");
	Response.Write("<table align=left valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Currency & Exchange-Rate, item cost
	Response.Write("<tr><td><b>"+Lang("Last Cost")+"(FRD)</b></td><td>");
	Response.Write("<input type=text readonly size=5 style=text-align:right;");	
	Response.Write(" name=raw_supplier_price value='" + foreign_supplier_price + "' onchange=\"CalcCost()\"></td></tr>");
	Response.Write("<tr><td><b>"+Lang("Currency")+"</b></td><td>");
	Response.Write("<select name=currency onchange=\"UpdateCurrency(this.options[selectedIndex].text)\">");
	Response.Write(PrintCurrencyOptions(true, currency)); //true to use rates only
	Response.Write("</select></td></tr>");
	Response.Write("<tr><td><b>"+Lang("Exchange Rate")+"</b></td><td><input type=text size=5 style=text-align:right name=exrate value='" + exchange_rate + "' onchange=\"UpdateExRate()\"></td></tr>");
	Response.Write("</table></td><td>");
/////end the last cost table here 

	double dExchangeRate = 1;
	if(exchange_rate != null)
	{
		try{ dExchangeRate = double.Parse(exchange_rate); } catch(Exception e){}
		if(dExchangeRate == 0)
			dExchangeRate = 1;
	}	
	string sLastCost = (Math.Round(double.Parse(foreign_supplier_price) / dExchangeRate,2)).ToString();

	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>"+Lang("Last Cost")+""+"("+m_sCurrencyName+")"+"</b></td><td>");
	Response.Write("<input type=text size=5 readonly=true style=text-align:right;background-color:#EEEEEE name=supplier_price" + index);
	Response.Write(" onchange=\"CalcPrice()\" value=" + sLastCost + "></td></tr>");
	Response.Write("<tr><td><b>"+Lang("Freight")+""+"("+m_sCurrencyName+")"+"</b></td><td><input type=text size=5 style=text-align:right name=freight value='" + nzd_freight + "' onchange=\"CalcManualCost(); CalcPrice();Calculate()\"></td>");
	Response.Write("</tr>");
		//show average cost in stock
	if(MyIntParse(GetSiteSettings("dealer_levels")) > 0)
	{
		Response.Write("<tr><td><b>"+Lang("Average Cost")+"</b> ");
		Response.Write("<a title='view average changed details' href=\"javascript:viewavg_window=window.open('viewavgcost.aspx?");
		Response.Write("code=" + m_code + "','', ' width=600,height=500, resizable=yes, scrollbar=1'); viewavg_window.focus();\" ><font color=purple size=2><b>V</font></b></a>");
		Response.Write("</td><td><input type=text size=5 name=average_cost style=text-align:right; maxlength=48 value='" + average_cost + "' "+ keyEnter +"></td></tr>");
	}
	Response.Write("<input type=hidden name=old_average_cost value='"+ average_cost +"' >");
	Response.Write("</table>");	
	
	Response.Write("</td></tr>");
	Response.Write("</table>");

//	Response.Write("</td></tr></table>");

//	Response.Write("</td></tr>");

//	double level_rate3 = Math.Round(GetLevelRate(3, level_rate1, level_rate2), 4);
//	double level_rate4 = Math.Round(GetLevelRate(4, level_rate1, level_rate2), 4);
//	double level_rate5 = Math.Round(GetLevelRate(5, level_rate1, level_rate2), 4);
//	double level_rate6 = 1;

	double dPrice = MyDoubleParse(price);

	int i = 0;
	int[] qb = new int[9]; //qty breaks;
	double[] dQPrice = new double[9];
	double[] dLPrice = new double[9];
	double[] level_rate = new double[9];
	double[] dQDiscount = new double[9];

	for(i=0; i<9; i++)
	{
		string ii = (i+1).ToString();
		
		string si = dr["level_rate" + ii].ToString();
		if(si == "")
			level_rate[i] = 2;
		else
			level_rate[i] = MyDoubleParse(dr["level_rate" + ii].ToString());
		
		si = dr["qty_break" + ii].ToString();
		if(si == "")
			qb[i] = 0;
		else
			qb[i] = MyIntParse(dr["qty_break" + ii].ToString());

		si = dr["qty_break_discount" + ii].ToString();
		if(si == "")
			dQDiscount[i] = 0;
		else
			dQDiscount[i] = MyIntParse(dr["qty_break_discount" + ii].ToString());
		
		dLPrice[i] =  Math.Round(MyDoubleParse(dealerLevel1Price) * level_rate[i], 2) /100;
		dQPrice[i] = Math.Round(dLPrice[0] * (1 - dQDiscount[i]/100), 2);
	}

	int dls = MyIntParse(GetSiteSettings("dealer_levels", "3")); // how many dealer levels
	if(dls > 9)
		dls = 9;
	int qbs = MyIntParse(GetSiteSettings("quantity_breaks", "3")); // how many quantity breaks
	if(qbs > 9)
		qbs = 9;

	Response.Write("<input type=hidden name=dls value=" + dls + ">");
	Response.Write("<input type=hidden name=qbs value=" + qbs + ">");

/*	double dQDiscount1 = GetQuantityDiscount(qb[0], qb, level_rate2);
	double dQDiscount2 = GetQuantityDiscount(qb[1], qb, level_rate2);
	double dQDiscount3 = GetQuantityDiscount(qb[2], qb, level_rate2);
	double dQDiscount4 = GetQuantityDiscount(qb[3], qb, level_rate2);
*/

//	dQDiscount1 = Math.Round(1 - dQDiscount1, 4);

//calculate dealer level by given selling price
	//javascript start here
	Response.Write("<script language=javascript>");
	string s = @"
		function autoCal()
		{
			var SellingPrice, descreased, level1, level2, level3, level4, level5, level6, level7, level8, level9, BottomPrice;
			SellingPrice = document.form1.sellingprice.value;
			BottomPrice = document.form1.price0.value;
			if(SellingPrice == '')
				return false;
			if(eval(SellingPrice) < eval(BottomPrice))
				return false;
			level1 = SellingPrice / BottomPrice;
			descreased = (level1 - 1) / 6;
			level2 = level1 - (2 * descreased);
			level3 = level1 - (3 * descreased);
			level4 = level1 - (4 * descreased);
			level5 = level1 - (5 * descreased);
			level6 = level1 - (6 * descreased);
			level7 = level1 - (7 * descreased);
			level8 = level1 - (8 * descreased);
			level9 = level1 - (9 * descreased);				
			if(document.form1.level_rate1 != undefined)
				document.form1.level_rate1.value = level1;
			if(document.form1.level_rate2 != undefined)
			document.form1.level_rate2.value = level2.toFixed(2);
			if(document.form1.level_rate3 != undefined)
			document.form1.level_rate3.value = level3.toFixed(2);
			if(document.form1.level_rate4 != undefined)
			document.form1.level_rate4.value = level4.toFixed(2);
			if(document.form1.level_rate5 != undefined)
			document.form1.level_rate5.value = level5.toFixed(2);
			if(document.form1.level_rate6 != undefined)
			document.form1.level_rate6.value = level6.toFixed(2);
			if(document.form1.level_rate7 != undefined)
			document.form1.level_rate7.value = level7.toFixed(2);
			if(document.form1.level_rate8 != undefined)
			document.form1.level_rate8.value = level8.toFixed(2);
			if(document.form1.level_rate9 != undefined)
			document.form1.level_rate9.value = level9.toFixed(2);
			return true;
		}
		";
		
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	/*
	Response.Write("<tr bgcolor='#EEEEE'><td colspan=2><font size=3><b>"+Lang("Dealer Prices Setting")+"</b></font></td></tr>");	
	//if(g_bRetailVersion)
	{
		Response.Write("<tr bgcolor='#EEEEE'><td colspan='"+ cols +"'>");
		Response.Write("<b>"+Lang("Input Selling Price(exc GST) for Calculating Dealer Level Rates")+": </b><input size=5% style=text-align:right type=text name=sellingprice value='" + price +"'>");
		Response.Write("<input type=button name='calme' value='"+Lang("Calculate Dealer Level Rate")+"' "+ Session["button_style"] +"");
		Response.Write(" Onclick='autoCal(); Calculate();' >");
		Response.Write("</tr>");
	}
	
*/

	Response.Write("<tr bgcolor='#EEEEE'><td colspan=2><font size=3><b>"+Lang("Whole Sell Prices Setting")+"</b></font></td></tr>");	
	//if(g_bRetailVersion)
	{
		Response.Write("<tr bgcolor='#EEEEE'><td colspan='"+ cols +"'>");
		Response.Write("<b>"+Lang("Input Whole Sell Price(exc GST)")+": </b><input size=5% style=text-align:right type=text name=wholeprice value='" + dealerLevel1Price +"'>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan='"+ cols +"'>");
	Response.Write("<table width=100% cellspacing=0 cellpadding=5 border=1 style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td valign=top>");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0>");
	Response.Write("<tr bgcolor='#EEEEE'><td><b>"+Lang("Level")+"</b></td><td><b>"+Lang("Rate")+"</b></td><td><b>"+Lang("Price")+"</b></td></tr>");
	Response.Write("<tr><td colspan=3><hr></td></tr>");

	for(i=0; i<dls; i++)
	{
		string ii = (i + 1).ToString();
		string lname = GetEnumValue("dealer_level_name", ii);
		if(lname == "")
			lname = Lang("Level") + ii;
		Response.Write("<tr><td>" + Lang(lname) + "</td>");
		Response.Write("<td><input type=text size=5 name=level_rate" + ii + " value=" + level_rate[i] + " ");
		Response.Write(" onBlur=\"if(this.value < 1) { this.value = this.value * 100 }; var v_rate = this.value ; if (v_rate.search('%') > 0) {this.value=v_rate.replace('%','');};\"");
	//	Response.Write(
		Response.Write("> </td>");
		Response.Write("<td><input type=text size=5 name=level_price" + ii);
		Response.Write(" style=text-align:right;   ");
		Response.Write(" value='" + dLPrice[i].ToString() + "'");
		Response.Write(" onKeyUp =\"document.all.level_rate"+ii+".value = Math.round((this.value / document.all.manual_cost_frd.value),2) ;\"");
		Response.Write("></td></tr>");
	}
    Response.Write("<tr><td style=\"color:red\" colspan=3 ><b>* Level Rate Format. eg: Key in 20 instead of 20% or 0.2</b></td></tr>");
	Response.Write("</table>");

	Response.Write("</td><td valign=top>");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0>");
	Response.Write("<tr bgcolor='#EEEEE'><td><b>"+Lang("Break")+"</b></td><td><b>"+Lang("Quantity")+"</b></td><td><b>"+Lang("Discount")+"</b></td><td><b>"+Lang("Example(Level 1)")+"</b></td></tr>");
	Response.Write("<tr><td colspan=4><hr></td></tr>");

//	Response.Write("<tr><td>0</td><td>1</td>");
//	Response.Write("<td>0.00</td><td>" + dLPrice[0].ToString("c") + "</td></tr>");
	for(i=0; i<qbs; i++)
	{
		string ii = (i + 1).ToString();
		Response.Write("<tr><td>" + ii + "</td><td>");
		Response.Write("<input type=text size=5 name=qty_break" + ii + " value=" + qb[i] + "></td>");
		Response.Write("<td><input type=text size=3 name=qty_break_discount" + ii + " value=" + dQDiscount[i] + ">%</td>");
		Response.Write("<td><input type=text size=5 name=qty_price" + ii);
		Response.Write(" style=text-align:right;background-color:#EEEEEE readonly=true ");
		Response.Write(" value=" + dQPrice[i].ToString() + "></td></tr>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr>");

//	Response.Write("<tr><td colspan=2 align=right><input type=button " + Session["button_style"] + " value='Calculate' ");
//	Response.Write(" onclick=\"Calculate();\">");
	Response.Write("</table>");

	return true;
}

bool PrintSimpleInterface(DataRow dr, int row)
{
	string index = row.ToString();

	string price = dr["price"].ToString();
	double dPrice = MyDoubleParse(price);
	string special = dr["special"].ToString();

	int i = 0;
	int[] qb = new int[16]; //qty breaks;
	double[] dQPrice = new double[16];
	double[] dLPrice = new double[16];
	double[] dQDiscount = new double[16];

	int qbs = MyIntParse(GetSiteSettings("quantity_breaks", "3")); // how many quantity breaks
	for(i=0; i<qbs; i++)
	{
		string ii = (i+1).ToString();
		qb[i] = MyIntParse(dr["qty_break" + ii].ToString());
		dQPrice[i] = MyDoubleParse(dr["qty_break_price" + ii].ToString());
		if(dQPrice[i] == 0)
		{
			if(i > 0)
				dQPrice[i] = dQPrice[i-1];
			else
				dQPrice[i] = dPrice;
		}
	}

	if(qbs > 15)
		qbs = 15;

	Response.Write("<table width='100%' align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<input type=hidden name=qbs value=" + qbs + ">");

	Response.Write("<tr><td><b>Status</b> : ");
	Response.Write("<input type=checkbox name=special" + index);
	if(special != "0")
		Response.Write(" checked");
	Response.Write(">Special &nbsp&nbsp&nbsp&nbsp; <input type=hidden name=special_old" + index + " value=");
	if(special != "0")
		Response.Write("on");
	Response.Write(">");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");
	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=0>");
	Response.Write("<tr><td><b>Break</b></td><td><b>Quantity</b></td><td><b>Price</b></td></tr>");
	Response.Write("<tr><td colspan=4><hr></td></tr>");

	Response.Write("<tr bgcolor=#EEEEEE><td>0</td><td>1</td>");
	Response.Write("<td><input type=text size=5 style=text-align:right ");
	Response.Write("  name=price" + index + " value=" + price + "></td>");
	Response.Write("</td></tr>");

	bool bAlterColor = false;
	for(i=0; i<qbs; i++)
	{
		string ii = (i + 1).ToString();
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write("><td>" + ii + "</td><td>");
		Response.Write("<input type=text size=5 name=qty_break" + ii + " value=" + qb[i] + "></td>");
//		Response.Write("<td><input type=text size=3 name=qty_break_discount" + ii + " value=" + dQDiscount[i] + ">%</td>");
		Response.Write("<td><input type=text size=5 name=qty_price" + ii);
		Response.Write(" style=text-align:right;");
		Response.Write(" value=" + dQPrice[i].ToString() + "></td></tr>");
	}
	Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("</table>");
	return true;
}

bool UpdateBranchOfItem()
{
	string sc = "";
	int nBranches = MyIntParse(Request.Form["branches"]);
	for(int i=0; i<nBranches; i++)
	{
		int branch_id = MyIntParse(Request.Form["branch_id" + i]);
		if(branch_id <= 0)
			continue;
		int nBranchChecked = MyIntParse(Request.Form["branch_checked" + branch_id]);
		int nInactive = 1;
		if(nBranchChecked == 1)
			nInactive = 0;
		if(Request.Form["price1" + branch_id] == null || Request.Form["price1" + branch_id] == "")
			continue; //blank settings
		double dPrice1 = MyMoneyParse(Request.Form["price1" + branch_id]);
		double dPrice2 = MyMoneyParse(Request.Form["price2" + branch_id]);
		int nQty = MyIntParse(Request.Form["qpos_qty_break" + branch_id]);
		int nSpecial = MyIntParse(Request.Form["special" + branch_id]);
		double dSpecialPrice = MyMoneyParse(Request.Form["special_price" + branch_id]);
		string sEndDate = Request.Form["special_price_end_date" + branch_id];
		if(sEndDate == "")
			sEndDate = "NULL";
		else
			sEndDate = "'" + sEndDate + "'";
		
		sc += " IF EXISTS (SELECT id FROM code_branch WHERE code = " + m_code + " AND branch_id = " + branch_id + ") ";
		sc += " UPDATE code_branch SET ";
		sc += " inactive = " + nInactive;
		sc += ", price1 = " + dPrice1;
		sc += ", price2 = " + dPrice2;
		sc += ", qpos_qty_break = " + nQty;
		sc += ", special = " + nSpecial;
		sc += ", special_price = " + dSpecialPrice;
		sc += ", special_price_end_date = " + sEndDate;
		sc += " WHERE code = " + m_code + " AND branch_id = " + branch_id;
		sc += " ELSE ";
		sc += " INSERT INTO code_branch(inactive, code, branch_id, price1, price2, qpos_qty_break, special, special_price, special_price_end_date) VALUES(";
		sc += nInactive;
		sc += ", " + m_code;
		sc += ", " + branch_id;
		sc += ", " + dPrice1;
		sc += ", " + dPrice2;
		sc += ", " + nQty;
		sc += ", " + nSpecial;
		sc += ", " + dSpecialPrice;
		sc += ", " + sEndDate;
		sc += "); \r\n";
	}
	if(sc == "")
		return true;
	sc = " SET DATEFORMAT dmy \r\n" + sc;
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

int GetBranchOfItem(string sCode)
{
	if(dst.Tables["code_branch"] != null)
		dst.Tables["code_branch"].Clear();
	int nRet = 0;
	string sc = " SELECT b.id AS branch_id, b.name AS branch_name, cb.special ";
	sc += ", cb.branch_id AS branch_checked, cb.inactive, cb.price1, cb.price2, cb.qpos_qty_break, cb.special_price ";
	sc += ", CONVERT(varchar(100), cb.special_price_end_date, 105) AS end_date ";
	sc += " FROM branch b ";
	sc += " LEFT OUTER JOIN code_branch cb ON cb.branch_id = b.id AND cb.code = " + sCode;
	sc += " WHERE b.activated = 1 ";
	sc += " ORDER BY b.id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRet = myAdapter.Fill(dst, "code_branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}
	return nRet;
}
bool PrintFixedPricesInterface(DataRow dr, int row)
{
	int dls = MyIntParse(GetSiteSettings("dealer_levels", "3")); // how many dealer levels
	dls = 2;
	int i = 0;
	string index = row.ToString();

	string[] price = new string[9];
	double[] dPrice = new double[9];
	int[] nQty = new int[9];
	for(i=0; i<dls; i++)
	{
		price[i] = dr["price" + (i+1).ToString()].ToString();
		dPrice[i] = MyMoneyParse(price[i]);
		nQty[i] = MyIntParse(dr["qty_break" + (i+1).ToString()].ToString());
	}
	string special = dr["special"].ToString();
	string sSkip = dr["skip"].ToString();
	string cost = MyDoubleParse(dr["supplier_price"].ToString()).ToString();
	string low_stock = dr["low_stock"].ToString();

	string package_barcode1 = dr["package_barcode1"].ToString();
	string package_qty1 = dr["package_qty1"].ToString();
	string package_price1 = dr["package_price1"].ToString();
	string package_barcode2 = dr["package_barcode2"].ToString();
	string package_qty2 = dr["package_qty2"].ToString();
	string package_price2 = dr["package_price2"].ToString();
	string package_barcode3 = dr["package_barcode3"].ToString();
	string package_qty3 = dr["package_qty3"].ToString();
	string package_price3 = dr["package_price3"].ToString();
	double dNormalPrice = MyMoneyParse(dr["normal_price"].ToString());
	double dSpecialPrice = MyMoneyParse(dr["special_price"].ToString());
	string s_dtEnd = dr["special_price_end_date"].ToString();
	string avg_cost = dr["average_cost"].ToString();
	string supplier_price = dr["supplier_price"].ToString();
//	if(avg_cost == "" || avg_cost == "0")
//		avg_cost = supplier_price;
	
	
	bool bis_service = bool.Parse(dr["is_service"].ToString());

	string qpos_qty_break = "0";
	try
	{
		qpos_qty_break = dr["qpos_qty_break"].ToString();
	}
	catch(Exception e)
	{
		
		string err = e.ToString().ToLower();	
		if(err.IndexOf("column 'qpos_qty_break' does not belong") >= 0)
		{
			//myConnection.Close(); //close it first
			string ssc = @"				
			ALTER TABLE [dbo].[code_relations] ADD [qpos_qty_break] [int] DEFAULT(0) not null												
			";
//		DEBUG("ssc = ", ssc);
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
			//	ShowExp(sc, er);
				return false;
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"] +" \">");
		}				
	}
	if(s_dtEnd != "")
	{
		s_dtEnd = DateTime.Parse(dr["special_price_end_date"].ToString()).ToString("dd/MM/yyyy");
		dSpecialPrice = dPrice[0];
		dPrice[0] = MyMoneyParse(dr["normal_price"].ToString());
	}

	//branch item/price support	2009.01.19 DW
	string sc = " SET DATEFORMAT dmy ";
	sc += " IF NOT EXISTS(SELECT id FROM code_branch WHERE code = " + m_code + ") ";
	sc += " INSERT INTO code_branch (code, branch_id, price1, price2, qpos_qty_break, special_price, special_price_end_date) ";
	sc += " VALUES(" + m_code + ", 1, " + dPrice[0] + ", " + dPrice[1] + ", " + qpos_qty_break + ", " + dSpecialPrice;
	if(s_dtEnd != "")
		sc += ", '" + s_dtEnd + "' ";
	else
		sc += ", NULL";
	sc += ") ";
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

	int nBranches = GetBranchOfItem(m_code); //load data to code_branch table
	
	m_sBP += "<tr class=ta><td colspan=2><b>" + Lang("Branch") + "</b> (All price GST Inclusive)</td></tr>";
	m_sBP += "<tr><td colspan=3>";
	m_sBP += "<table align=left width='100%' valign=left cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white";
	m_sBP += " style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">";	
	m_sBP += "<tr bgcolor=#EEEFFF><td>" + Lang("Name") + "</td>";
	m_sBP += "<td>" + Lang("Sell") + "</td>";
	m_sBP += "<td>" + Lang("Price") + "</td>";
	//m_sBP += ("<td nowrap>" + Lang("Discount Price") + "</td>");
//	m_sBP += ("<td nowrap>" + Lang("Discount Qty(>=)") + "</td>");
	m_sBP += "<td nowrap>" + Lang("On Special") + "</td>";
	m_sBP += "<td nowrap>" + Lang("Special Price") + "</td>";
//	m_sBP += ("<td nowrap>" + Lang("Special End Date(DD/MM/YY)") + "</td>");
	m_sBP += "</tr>";
	
	m_sBP += "<input type=hidden name=branches value=" + nBranches + ">";
	for(i=0; i<nBranches; i++)
	{
		DataRow drc = dst.Tables["code_branch"].Rows[i];
		string branch_id = drc["branch_id"].ToString();
		string branch_name = drc["branch_name"].ToString();
		bool bBranchChecked = false;
		if(drc["branch_checked"].ToString() != "")
			bBranchChecked = true;
		bool bInactive = MyBooleanParse(drc["inactive"].ToString());
		if(bInactive)
			bBranchChecked = false;
		
		string price1 = drc["price1"].ToString();
		string price2 = drc["price2"].ToString();
		string qpos_qty_break_b = drc["qpos_qty_break"].ToString();
		bool bSpecial_b = MyBooleanParse(drc["special"].ToString());
		string special_price_b = drc["special_price"].ToString();
		string special_price_end_date_b = drc["end_date"].ToString();
		if(price1 != "")
		{
			price1 = MyMoneyParse(price1).ToString("c");
			price2 = MyMoneyParse(price2).ToString("c");
			special_price_b = MyMoneyParse(special_price_b).ToString("c");
		}
		
		if(i == 0)
		{
/*			m_sBP += ("<input type=hidden name=price1 value='" + price1 + "'>");
			m_sBP += ("<input type=hidden name=price2 value='" + price2 + "'>");
			m_sBP += ("<input type=hidden name=qpos_qty_break value='" + qpos_qty_break_b + "'>");
			m_sBP += ("<input type=hidden name=special_price value='" + special_price_b + "'>");
			m_sBP += ("<input type=hidden name=special_price_end_date value='" + special_price_end_date_b + "'>");
*/
		}
		
		m_sBP += "<input type=hidden name=branch_id" + i + " value=" + branch_id + ">";	
		m_sBP += "<tr><td>" + branch_name + "</td>";
		m_sBP += "<td><input type=checkbox value=1 name=branch_checked" + branch_id + " ";
		if(bBranchChecked)
			m_sBP += " checked";
		m_sBP += "></td>";
		m_sBP += "<td><input type=text size=10 style=text-align:right name=price1" + branch_id + " value='" + price1 + "'";
//		m_sBP += (" onkeyup=\"document.all.price1.value = this.value;\"");
		m_sBP += "></td>";
	//	m_sBP += ("<td><input type=text size=10 style=text-align:right name=price2" + branch_id + " value='" + price2 + "'></td>");
	//	m_sBP += ("<td><input type=text size=10 style=text-align:right name=qpos_qty_break" + branch_id + " value='" + qpos_qty_break_b + "'></td>");
		m_sBP += "<td><input type=checkbox name=special" + branch_id + " value=1 ";
		if(bSpecial_b)
			m_sBP += " checked";
		m_sBP += "></td>";
		m_sBP += "<td><input type=text size=10 style=text-align:right name=special_price" + branch_id + " value='" + special_price_b + "'";
//		m_sBP += (" onkeyup=\"document.all.special_price.value = this.value;\"");
		m_sBP += "></td>";
	//	m_sBP += ("<td><input type=text size=20 style=text-align:right name=special_price_end_date" + branch_id + " value='" + special_price_end_date_b + "'></td>");
		m_sBP += "</tr>";
	}
	m_sBP += "</table>";	
	m_sBP += "</td></tr>";


	///Special Price ///
	
	Response.Write("<tr bgcolor='#EEEEE'><td width='"+ rowWidth +"' colspan=2> ");
	Response.Write("<font size=3><b>" + Lang("Barcodes") + "</b></font>");
	Response.Write(" &nbsp; <input type=button value='" + Lang("Edit Barcode") + "' onclick=window.location=('ebarcode.aspx?code=" + m_code +"') class=b></font></b></td></tr>");
	Response.Write("<tr><td valign=top>");
	Response.Write("<table align=left width='100%' valign=left cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
	Response.Write("<tr><td width='50%'><b>"+Lang("Click to Enable Special Price Setting")+" </b></td><td>");
	Response.Write("<input type=checkbox name=special" + index);
	if(special != "0")
		Response.Write(" checked");
	Response.Write("> <input type=hidden name=special_old" + index + " value=");
	if(special != "0")
		Response.Write("on");
	Response.Write(">");
	Response.Write("</td></tr>");
	Response.Write("<tr><td ><b>"+Lang("Special Price")+" ("+Lang("Inc. GST")+") </b></td><td>");
	Response.Write("<input type=text size=10 name=special_price value=" + dSpecialPrice + " style=text-align:right; >");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>"+Lang("End Date")+" </b></td><td><input type=text size=10 name=special_price_end_date value='" + s_dtEnd + "'>("+Lang("dd/MM/yyyy")+")");
	Response.Write("</td></tr>");
	
//	Response.Write("<tr><td><b><font color=green>"+Lang("Set Warning Low Stock Qty")+" </b></td><td>");
//	Response.Write("<input type=text size=5 name=low_stock value=" + low_stock + " style=text-align:right;>");
//	Response.Write("("+Lang("Individual Branch Stock Level")+")</td></tr>");
	Response.Write("</table>");	
	Response.Write("</td>");
	Response.Write("<td>");	

	//package price here.
/*	Response.Write("<table align=left valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
	Response.Write("<tr bgcolor=#EEEFFF align=left><th>"+Lang("Barcode")+"</th><th>"+Lang("Qty")+"</th><th>"+Lang("Price")+"</th></tr>");
	Response.Write("<tr>");
	Response.Write("<td><input type=text name=package_barcode1 value='" + package_barcode1 + "' onKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"></td>");
	Response.Write("<td><input type=text name=package_qty1 size=5% value='" + package_qty1 + "'></td>");
	Response.Write("<td><input type=text name=package_price1 size=7%  value='" + package_price1 + "'></td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td><input type=text name=package_barcode2 value='" + package_barcode2 + "' onKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"></td>");
	Response.Write("<td><input type=text name=package_qty2 size=5%  value='" + package_qty2 + "'></td>");
	Response.Write("<td><input type=text name=package_price2 size=7%  value='" + package_price2 + "'></td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<td><input type=text name=package_barcode3 value='" + package_barcode3 + "' onKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"></td>");
	Response.Write("<td><input type=text name=package_qty3 size=5%  value='" + package_qty3 + "'></td>");
	Response.Write("<td><input type=text name=package_price3 size=7%  value='" + package_price3 + "'></td>");
	Response.Write("</tr>");
	Response.Write("</table>");
*/ 

	string sc_barcode = " SELECT barcode, item_qty ";
	sc_barcode += " FROM barcode ";
	sc_barcode += " WHERE item_code = " + m_code;
	int nRows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc_barcode, myConnection);
		nRows = myCommand.Fill(dst, "barc");
	}
	catch(Exception e) 
	{
		ShowExp(sc_barcode, e);
		return false;
	}
	if(nRows > 0)
	{
		Response.Write("<table width=100% align=left valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white class=t>");
		Response.Write("<tr class=th>");
		Response.Write("<th>Barcode</th>");
		Response.Write("<th>Qty</th>");
		Response.Write("</tr>");
		DataRow drb = null;
		for(i=0; i<nRows; i++)
		{
			drb = dst.Tables["barc"].Rows[i];
			string barcode = drb["barcode"].ToString();
			string qty = drb["item_qty"].ToString();
			Response.Write("<tr");
			if(i%2 != 0)
				Response.Write(" bgcolor=#EEEEEE"); //alter color
			Response.Write(">");
			Response.Write("<td align=center>" + barcode + "</td>");
			Response.Write("<td width=50% align=center>" + qty + "</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");
	}	

	Response.Write("</td></tr>");
	///
	
//	Response.Write("<tr><td><b>Level</b></td><td><b>Price</b></td></tr>");
//	Response.Write("<tr><td colspan=2><hr></td></tr>");

	Response.Write("<tr bgcolor='#EEEEE'><td colspan=2><font size=3><b>"+Lang("POS Prices")+"</b></font></td></tr>");	
	Response.Write("<tr><td>");
	Response.Write("<table align=left width='100%' valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
	Response.Write("<tr><td>");
	//POS Price
	Response.Write("<table align=left width='100%' valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	

	Response.Write("<tr><td wrap width='50%'><b>"+Lang("Sales Price (GST Inclusive)")+" </b></td><td>");
		
	Response.Write("<input type=text size=10 name=price1 value=" + dPrice[0] + " style='");
	if(special != "0")
		Response.Write("background-color:#EEEEEE;");
	Response.Write("text-align:right;' ");
	if(special != "0")
		Response.Write(" readonly ");
	Response.Write(">");
	Response.Write("</td></tr>");
	Response.Write("<tr><td wrap><b>"+Lang("Discount Price (GST Inclusive)")+" </b></td><td>");
	Response.Write("<input type=text size=10 name=price2 value=" + dPrice[1] + " style=text-align:right; >");
	Response.Write("</td></tr>");
	Response.Write("<tr><td wrap><b>"+Lang("Discount Qty")+"(>=) </b></td><td>");
//	Response.Write("<input type=text size=10 name=d_qty_break1 value=" + nQty[0] + " style=text-align:right; >");
	Response.Write("<input type=text size=10 name=qpos_qty_break value='" + qpos_qty_break + "' style=text-align:right; >");
	Response.Write("</td></tr>");
	
	Response.Write("</table>");
//	Response.Write("<tr><td><b>Packages</b></td></tr>");
	Response.Write("</td><td valign=top > ");

	//Approximately Profit
	Response.Write("<table align=left cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
	
	double grProfit = (dPrice[0] / m_GSTRate) - Math.Round(MyDoubleParse(avg_cost), 2);
	double dGPPercent = ((dPrice[0] / m_GSTRate) - Math.Round(MyDoubleParse(avg_cost), 2) )/ (dPrice[0] / m_GSTRate);
	if(special != "0")
	{
		grProfit = (dSpecialPrice / m_GSTRate) - Math.Round(MyDoubleParse(avg_cost), 2);
		dGPPercent = ((dSpecialPrice / m_GSTRate) - Math.Round(MyDoubleParse(avg_cost), 2) )/ (dSpecialPrice / m_GSTRate);
	}

	Response.Write("</td></tr><tr><td align=right><b>"+Lang("Gross Profit")+" ($)</b></td><td><input type=text style='text-align:right;background-color:#EEEEEE' size=5 readonly name=grossprofit value='"+ grProfit.ToString("c") +"' >");
	Response.Write("</td></tr>");
	Response.Write("<tr><td align=right><b>(%)</td><td><input type=text size=5 readonly style='text-align:right;background-color:#EEEEEE' name=grPercent value='"+ dGPPercent.ToString("p") +"' >");
	Response.Write("</td></tr>");
	Response.Write("</table>");			
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</td><td valign=top>");
	if(MyIntParse(GetSiteSettings("dealer_levels")) <= 0)
	{
		Response.Write("<table align=left cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
		Response.Write("<tr><td><b>Average Cost </b>");
		Response.Write("<a title='view average changed details' href=\"javascript:viewavg_window=window.open('viewavgcost.aspx?");
		Response.Write("code=" + m_code + "','', ' width=600,height=500, resizable=yes, scrollbar=1'); viewavg_window.focus();\" ><font color=purple size=2><b>V</font></b></a> ");
		Response.Write("</td><td><input type=text size=5 name=average_cost value=" + Math.Round(MyDoubleParse(avg_cost), 2) + " style=text-align:right;");
	//	if(MyIntParse(GetSiteSettings("dealer_levels")) > 1)
	//		Response.Write("background-color:#EEEEEE readonly ");
		Response.Write("></td></tr>");
		Response.Write("<tr><td><b>Last Cost</b></td>");
		Response.Write("<td><input type=text size=5 value=" + Math.Round(MyDoubleParse(supplier_price), 2) + " style='text-align:right;background-color:#EEEEEE'>");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		
	}	
	Response.Write("</tr>");
	//end of POS table

//	Response.Write("</table>");

//	Response.Write("</td></tr>");
//	Response.Write("</table>");
	return true;
}

Boolean UpdateCatalogTable()
{
	if(DoUpdateCatalogTable())
		return true;
	return false;
}

string PrintSelectionRow(string sName, string sValue, string index)
{
	bool bMatch = false;
	string str = "";
	StringBuilder sb = new StringBuilder();
	sb.Append("\r\n<select name=" + sName + index);
	if(sName != "brand" && sName != "ss_cat")
	{
		sb.Append(" onchange=\"window.location=('liveedit.aspx?code=" + m_code);
		sb.Append("&r=" + DateTime.Now.ToOADate());
		if(sName == "cat")
			sb.Append("&sk=-1&ck='");
		else if(sName == "s_cat")
			sb.Append("&ck="+HttpUtility.UrlEncode(m_ck) + "&sk='");
		sb.Append("+ escape(this.options[this.selectedIndex].value))\"");
	}
	sb.Append(">");
	sb.Append("<option value=''></option>");
	for(int j=0; j<dsAEV.Tables[sName].Rows.Count; j++)
	{
		str = dsAEV.Tables[sName].Rows[j][0].ToString();
		str = str.TrimStart(null);
		str = str.TrimEnd(null);
		sb.Append("<option value='" + str + "'");
		if(!bMatch)
		{
			if(sName == "brand")
			{
				if(str == sValue)
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
			else if(sName == "cat")
			{
				if(m_ck == "-1")
				{
//DEBUG("s_value=", sValue);
					if(str == sValue)
					{
						bMatch = true;
						sb.Append(" selected");
					}
				}
				else
				{
					if(str == m_ck)
					{
						bMatch = true;
						sb.Append(" selected");
					}
				}
			}
			else if(sName == "s_cat")
			{
				if(m_sk == "-1")
				{
					if(str == sValue)
					{
						bMatch = true;
						sb.Append(" selected");
					}
				}
				else
				{
//DEBUG("m_sk="+m_sk, " str="+str);
					if(str == m_sk)
					{
						bMatch = true;
						sb.Append(" selected");
					}
				}
			}
			else if(sName == "ss_cat")
			{
				if(str == sValue)
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
		}

		sb.Append(">" + str + "</option>");
	}
	if(!bMatch && m_ck == "-1" && m_sk == "-1")
		sb.Append("<option value='" + sValue + "' selected>" + sValue + "</option>");
	sb.Append("</select>");
	sb.Append("</td>");

	sb.Append("<td><input type=text size=28 maxlength=180 name=" + sName + "_new" + index + ">");
/*	if(sName == "brand")
		sb.Append("<b> New Brand</b>");
	else if(sName == "cat")
		sb.Append("<b> New 1st Category</b>");
	else if(sName == "s_cat")
		sb.Append("<b> New 2nd Category</b>");
	else if(sName == "ss_cat")
		sb.Append("<b> New 3rd Category</b>");
		*/
	sb.Append("</td></tr>");
	return sb.ToString();
}

string PrintSelectionRowForCross(string sName, string sValue)
{
	bool bMatch = false;
	string str = "";
	StringBuilder sb = new StringBuilder();
	sb.Append("\r\n<select name=" + sName);
	if(sName != "ss_cat")
	{
		sb.Append(" onchange=\"window.location=('liveedit.aspx?code=" + m_code);
		if(sName == "cat")
			sb.Append("&so=-1&co='");
		else if(sName == "s_cat")
			sb.Append("&co="+HttpUtility.UrlEncode(m_co) + "&so='");
		sb.Append("+ escape(this.options[this.selectedIndex].value)) + '#cross'\"");
	}
	sb.Append(">");
	for(int j=0; j<dsAEV.Tables[sName].Rows.Count; j++)
	{
		str = dsAEV.Tables[sName].Rows[j][0].ToString();
		sb.Append("<option value='" + str + "'");
		if(str == sValue)
		{
			bMatch = true;
			sb.Append(" selected");
		}
		if(!bMatch)
		{
			if(sName == "cat" && m_co == "-1")
			{
				bMatch = true;
				sb.Append(" selected");
			}
			else if(sName == "s_cat" && m_so == "-1")
			{
				if(str != "")
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
			else if(sName == "ss_cat")
			{
				if(str != "")
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
		}
		sb.Append(">"+str+"</option>");
	}
	if(!bMatch)
		sb.Append("<option value='" + sValue + "' selected>" + sValue + "</option></select>");
//	if(sName == "ss_cat")
//		sb.Append("<input type=text size=10 name=" + sName + "_new>");
	return sb.ToString();
}

bool DoCopyProduct()
{
	//m_code = GetNextCode();
	copy_supplier = Request.Form["supplier"];
	copy_supplier_code = Request.Form["supplier_code"];
	if(copy_supplier_code == "")
	{
		Response.Write("<br><br><h3>Error, no supplier code");
		return false;
	}
	copy_id = copy_supplier + copy_supplier_code;
	
	//check id vialation
	string sc = "SELECT * FROM code_relations WHERE id='" + copy_id + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "id_check") > 0)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Error, dupplicate supplier code found</h3><br>");
			DataRow dr = dst.Tables["id_check"].Rows[0];
			string code = dr["code"].ToString();
			Response.Write("<table>");
			Response.Write("<tr><td>code : </td><td><a href=p.aspx?" + code + " class=o>" + code + "</a></td></tr>");
			Response.Write("<tr><td>supplier : </td><td>" + dr["supplier"].ToString() + "</td></tr>");
			Response.Write("<tr><td>Supplier Code : </td><td>" + dr["supplier_code"].ToString() + "</td></tr>");
			Response.Write("<tr><td>Description : </td><td>" + dr["name"].ToString() + "</td></tr>");
			Response.Write("</table>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow drpd = null;
	DataRow drp = null;
	DataRow dr_cr = null;
	DataRow dr_cc = null;

	if(!GetNewProdDetails("product_details", ref drpd))
		return false;
	if(!GetNewProdDetails("product", ref drp))
		return false;
	if(!GetNewProdDetails("code_relations", ref dr_cr))
		return false;
	if(!GetNewProdDetails("cat_cross", ref dr_cc))
		return false;

	string code_old = m_code;
	m_code = GetNextCode().ToString();

	if(drpd != null)
	{
		if(!InsertProdDetails(drpd))
			return false;
	}
	if(drp != null)
	{
		if(!InsertProd(drp))
			return false;
	}
	if(dr_cr != null)
	{
		if(!InsertCodeRelation(dr_cr))
			return false;
	}
	if(dr_cc != null)
	{
		if(!InsertCat_Cross(dr_cc)) 
			return false;
	}
	
	string sPicFile = "";
	string vpath = GetRootPath();
	vpath += "/pi/";
	string path = vpath;
	sPicFile = path + code_old + ".gif";
	string newfile = path + m_code;
	bool bHasLocal = File.Exists(Server.MapPath(sPicFile));
	if(!bHasLocal)
	{
		sPicFile = path + code_old + ".jpg";
		bHasLocal = File.Exists(Server.MapPath(sPicFile));
		newfile += ".jpg";
	}
	else
		newfile += ".gif";

	if(bHasLocal)
	{
		if(!File.Exists(Server.MapPath(newfile)))
			File.Copy(Server.MapPath(sPicFile), Server.MapPath(newfile)); //copy image
	}
	RecordUpdatedItem(m_code);
	return true;
}

bool GetNewProdDetails(string s_table, ref DataRow dr)
{
	string s_filltable = "";
	if(s_table == "product_details")
		s_filltable = "pspec";
	else if(s_table == "product")
		s_filltable = "pdetails";
	else if(s_table == "code_relations")
		s_filltable = "p_code_relations";
	else if(s_table == "cat_cross")
		s_filltable = "p_cat_cross";
	else
	{
		Response.Write("<br><br><center><h3>Error, no table defined, s_table=" + s_table);
		return false;
	}

//	if(dst.Tables["details"] != null)
//		dst.Tables["details"].Clear();

	string sc = "SELECT * FROM " + s_table + " WHERE code = " + m_code;
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, s_filltable);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(dst.Tables[s_filltable].Rows.Count > 0 )
		dr = dst.Tables[s_filltable].Rows[0];
	else
		dr = null;
	return true;
}

bool InsertProdDetails(DataRow dr)
{
	string sc = " IF NOT EXISTS (SELECT code FROM product_details WHERE code = "+ m_code +") ";
	sc += " INSERT INTO product_details (code, highlight, spec, manufacture, pic, rev, warranty)";
	sc += "VALUES (" + m_code + ", '";
	sc += EncodeQuote(dr["highlight"].ToString()) + "', '";
	sc += EncodeQuote(dr["spec"].ToString()) + "', '";
	sc += EncodeQuote(dr["manufacture"].ToString()) + "', '";
	sc += EncodeQuote(dr["pic"].ToString()) + "', '";
	sc += EncodeQuote(dr["rev"].ToString()) + "', '";
	sc += EncodeQuote(dr["warranty"].ToString()) + "')";
	sc += " ELSE ";
	sc += " UPDATE product_details ";
	sc += " SET highlight = '"+ EncodeQuote(dr["highlight"].ToString()) + "'";
	sc += ", spec = '"+ EncodeQuote(dr["spec"].ToString()) + "'";
	sc += ", manufacture = '" + EncodeQuote(dr["manufacture"].ToString()) + "'";
	sc += ", pic = '"+ EncodeQuote(dr["pic"].ToString()) + "'";
	sc += ", rev = '"+ EncodeQuote(dr["rev"].ToString()) + "'";
	sc += ", warranty = '"+ EncodeQuote(dr["warranty"].ToString()) + "'";
	sc += " WHERE code = "+ m_code +" ";
//DEBUG("sc = ", sc);
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

bool InsertProd(DataRow dr)
{
	string sc = "INSERT INTO product_skip (id, stock, eta, supplier_price, price) ";
	sc += " VALUES ('";
	sc += copy_id + "', 0, '"; //no stock copying
	sc += dr["eta"].ToString() + "', ";
	sc += dr["supplier_price"].ToString() + ", ";
	sc += dr["price"].ToString() + ")";
//DEBUG("sc = ", sc);
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

bool InsertCodeRelation(DataRow dr)
{
	string[] lr = new string[10];
	string[] qb = new string[10];
	string[] qbd = new string[10];
	string[] lp = new string[10];

	int i = 1;
	for(i=1; i<=9; i++)
	{
		if(dr["level_rate" + i].ToString() != "")
			lr[i] = dr["level_rate" + i].ToString();
		else
			lr[i] = "null";
		if(dr["qty_break" + i].ToString() != "")
			qb[i] = dr["qty_break" + i].ToString();
		else
			qb[i] = "null";
		if(dr["qty_break_discount" + i].ToString() != "")
			qbd[i] = dr["qty_break_discount" + i].ToString();
		else
			qbd[i] = "null";
		
		if(dr["level_price"+i].ToString() != "")
			lr[i] = dr["level_price"+i].ToString();
		else
			lr[i] = "0";
	}

	string sc = "INSERT INTO code_relations (code, id, supplier, supplier_code, supplier_price, rate ";
	sc += ", name, brand, cat, s_cat, ss_cat, hot, skip, inventory_account, cost_account, income_account ";
	sc += ", foreign_supplier_price, currency, exchange_rate, nzd_freight ";
	sc += ", level_rate1, level_rate2, level_rate3, level_rate4, level_rate5, level_rate6, level_rate7, level_rate8, level_rate9 ";
	sc += ", qty_break1, qty_break2, qty_break3, qty_break4, qty_break5, qty_break6, qty_break7, qty_break8, qty_break9 ";
	sc += ", qty_break_discount1, qty_break_discount2, qty_break_discount3, qty_break_discount4, qty_break_discount5, qty_break_discount6, qty_break_discount7, qty_break_discount8, qty_break_discount9 ";
	sc += ", level_price1, level_price2, level_price3, level_price4, level_price5, level_price6, level_price7, level_price8, level_price9 ";
	sc += ", level_price0 ";
	sc += ", average_cost ";
	sc += ", manual_cost_frd, manual_exchange_rate, manual_cost_nzd ";
	sc += ") VALUES (";
	sc += m_code + ", '" + copy_id + "', '";
	sc += copy_supplier + "', '";
	sc += copy_supplier_code + "', ";
	sc += dr["supplier_price"].ToString() + ", ";
	sc += dr["rate"].ToString() + ", '";
	sc += EncodeQuote(dr["name"].ToString()) + "', '";
	sc += EncodeQuote(dr["brand"].ToString()) + "', '";
	sc += EncodeQuote(dr["cat"].ToString()) + "', '";
	sc += EncodeQuote(dr["s_cat"].ToString()) + "', '";
	sc += EncodeQuote(dr["ss_cat"].ToString()) + "', ";
	//"hot" field;
	if(dr["hot"].ToString().ToLower() == "true")
	{
		sc += "1, ";
	}
	else if(dr["hot"].ToString().ToLower() == "false")
	{
		sc += "0, ";
	}
	//"skip" field;
	sc += "1, ";
	string iva = dr["inventory_account"].ToString();
	if(iva == "")
		iva = "null";
	sc +=  iva + ", ";
	string ca = dr["inventory_account"].ToString();
	if(ca == "")
		ca = "null";
	sc +=  ca + ", ";
	string ia = dr["inventory_account"].ToString();
	if(ia == "")
		ia = "null";
	sc +=  ia + ", " + dr["foreign_supplier_price"].ToString() + ", '" + dr["currency"].ToString();
	sc += "', " + dr["exchange_rate"].ToString() + ", " + dr["nzd_freight"].ToString();
	for(i=1; i<=9; i++)
		sc += ", " + lr[i];
	for(i=1; i<=9; i++)
		sc += ", " + qb[i];
	for(i=1; i<=9; i++)
		sc += ", " + qbd[i];
	for(i=1; i<=9; i++)
		sc += ", " + lr[i];
	sc += ", " + dr["level_price0"].ToString();
	sc += ", " + dr["supplier_price"].ToString();
	sc += ", " + dr["manual_cost_frd"].ToString();
	sc += ", " + dr["manual_exchange_rate"].ToString();
	sc += ", " + dr["manual_cost_nzd"].ToString();
	sc += ")";
	//sc += dr["cost_account"].ToString() + ", ";
	//sc += dr["income_account"].ToString() + ")";

//DEBUG("sc = ", sc);
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

bool InsertCat_Cross(DataRow dr)
{
	string sc = "INSERT INTO cat_cross (code, cat, s_cat, ss_cat) VALUES (";
		sc += m_code + ", '";
		sc += dr["cat"].ToString() + "', '";
		sc += dr["s_cat"].ToString() + "', '";
		sc += dr["ss_cat"].ToString() + "')";
//DEBUG("sc = ", sc);
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

void PrintJavaFunction()
{
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function CalcCost()");
	Response.Write("{ var d = 0;\r\n");
//	Response.Write("	if(Number(document.form1.raw_supplier_price.value) <= 0){return;}\r\n");
	Response.Write("	d = Number(document.form1.raw_supplier_price.value) / Number(document.form1.exrate.value) + Number(document.form1.freight.value);\r\n");
	Response.Write("	d = Math.round(d * 100) / 100;\r\n");
	Response.Write("	document.form1.supplier_price0.value=d;\r\n");
	Response.Write("}\r\n");
	Response.Write("function UpdateCurrency(optionName)");
	Response.Write("{\r\n");
	Response.Write("	var rate = Number(document.form1.currency.value);\r\n");
	Response.Write("	document.form1.exrate.value = rate;\r\n");
	Response.Write("	document.form1.currency_name.value=optionName;\r\n ");
//	Response.Write(" window.alert(optionName); \r\n");
/*	Response.Write("	if(rate == Number(document.form1.rate_nzd.value)){document.form1.currency_name.value='nzd';\r\n}");
	Response.Write("	else if(rate == Number(document.form1.rate_usd.value))document.form1.currency_name.value='usd';\r\n");
	Response.Write("	else if(rate == Number(document.form1.rate_aud.value))document.form1.currency_name.value='aud';\r\n");
*/
	Response.Write("	CalcCost();\r\n");
	Response.Write("}\r\n");
	Response.Write("function UpdateExRate()");
	Response.Write("{\r\n");
	Response.Write("	if(document.form1.currency_name.value == 'nzd')document.form1.exrate.value=1;\r\n"); //fix NZD exchange rate to 1.00
	Response.Write("	else if(document.form1.currency_name.value == 'usd')document.form1.rate_usd.value=document.form1.exrate.value;\r\n");
	Response.Write("	else if(document.form1.currency_name.value == 'aud')document.form1.rate_aud.value=document.form1.exrate.value;\r\n");
	Response.Write("	CalcCost();");
	Response.Write("};");
	Response.Write("function CalcManualCost()");
	Response.Write("{ var d = 0;\r\n");
	Response.Write("	if(Number(document.form1.manual_cost_frd.value) <= 0){document.form1.manual_cost_frd.value=document.form1.manual_cost_nzd.value;}\r\n");
	Response.Write("	if(Number(document.form1.manual_exrate.value) <= 0){document.form1.manual_exrate.value=1;}\r\n");
//	Response.Write("	d = Number(document.form1.manual_cost_frd.value) / Number(document.form1.manual_exrate.value) + Number(document.form1.freight.value);\r\n");
	Response.Write("	d = Number(document.form1.manual_cost_frd.value) / Number(document.form1.manual_exrate.value);\r\n");
	Response.Write("	d = Math.round(d * 100) / 100;\r\n");
	Response.Write("	document.form1.manual_cost_nzd.value=d;\r\n");
	Response.Write("	CalcPrice();\r\n");
	Response.Write("}\r\n");
	Response.Write("function CalcPrice()");
	Response.Write("{ var d = 0;\r\n");
	Response.Write("	var rate = Number(document.form1.rate0.value);\r\n");
//	Response.Write("	if(rate <= 1){rate = 1.01;window.alert('Error, Bottom Rate cannot less than 1.00 !\\r\\nI will reset it to 1.01');}\r\n");
//	Response.Write("	d = rate * Number(document.form1.manual_cost_nzd.value);\r\n");
	Response.Write("	d = (rate * Number(document.form1.manual_cost_nzd.value)) + Number(document.form1.freight.value);\r\n");
	Response.Write("	d = Math.round(d * 100) / 100;\r\n");
	Response.Write("	document.form1.rate0.value=rate;\r\n");
	Response.Write("	document.form1.price0.value=d;\r\n");
	Response.Write("}\r\n");
	Response.Write("function Calculate()");
	Response.Write("{						");
	Response.Write("	var dls = Number(document.form1.dls.value);	\r\n");
	Response.Write("	for(var i=1; i<=dls; i++) ");
	Response.Write("	{	eval( \"document.form1.level_price\" + i + \".value =((Number(document.form1.level_rate\" + i + \".value) / 100) * Number(document.form1.wholeprice.value)).toFixed(3); \" ); } \r\n");
	Response.Write("	var qbs = Number(document.form1.qbs.value);	\r\n");
	Response.Write("	for(var i=1; i<=qbs; i++) ");
	Response.Write("	{	eval( \"document.form1.qty_price\" + i + \".value = Math.round( (1 - Number(document.form1.qty_break_discount\" + i + \".value)/100) * Number(document.form1.level_price1.value) * 100) / 100; \" ); } \r\n");
	Response.Write("}\r\n");
	Response.Write("</script");
	Response.Write(">");
}

bool DoSupplierSearch()
{
	string sc = "";
	string type_supplier = GetEnumID("card_type", "supplier");
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%#@#@#@#@#@#@%'";
	sc = "SELECT '<a href=" + m_url + "&si=' + convert(varchar, id) + ' class=o>' + convert(varchar, id) + '</a>' AS 'Select' ";
	sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
	sc += ", short_name AS Name, trading_name AS 'Trading Name', company AS Company  FROM card ";
	sc += " WHERE (name LIKE N" + kw + " OR trading_name LIKE N" + kw + " OR company LIKE N" + kw + " OR short_name LIKE N" + kw + ")";
	if(m_bOrder)
		sc += " AND type=" + type_supplier;
	sc += " ORDER BY name";
//DEBUG("sc=", sc);
	try
	{
        SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows == 0)
	{
		sc = "SELECT '<a href=" + m_url + "&si=' + convert(varchar, id) + ' class=o>' + convert(varchar, id) + '</a>' AS 'Select' ";
		sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
		sc += ", short_name AS Name, trading_name AS 'Trading Name', company AS Company  ";
		sc += " FROM card WHERE type=" + type_supplier + " ORDER BY name";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(dst, "card");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	BindGrid();
	LOtherSupplier.Text = "<a href=" + m_url + "&si=-2 class=o>Other Supplier</a>";

	Response.Write("<form id=search action='" + m_url + "&search=1' method=post>");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search><input type=submit name=cmd value=Cancel>");
	Response.Write("</td></tr></table></form>");
	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["card"]);
	string path = Request.ServerVariables["URL"].ToString();
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

bool GetSupplier()
{
	if(dst.Tables["card"] != null)
		dst.Tables["card"].Clear();
	//prepare customer ID
	string id = "";
	if(Request.QueryString["si"] == null)
	{
		
		if(Session["purchase_supplierid" + m_ssid] == null)
		    return true;
		id = Session["purchase_supplierid" + m_ssid].ToString();
		
	}
	else if(Request.QueryString["si"] != "")
	{
		id = Request.QueryString["si"].ToString();
		Session["purchase_supplierid" + m_ssid] = id;
	}

	if(id != "")
		m_supplierID = id;
	else
		return true; //nothing to get

	//-2 , other supplier
	if(m_supplierID == "-2")
	{
		m_supplierName = "";
		return true;
	}

	//do search
	string sc = "";
	if(id != "")
		sc = "SELECT * FROM card WHERE id=" + id;
	//else
//		sc = "SELECT * FROM card WHERE email='" + m_supplierEmail + "'";
//DEBUG("sc=", sc);
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "card") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//get supplier data
	DataRow dr = dst.Tables["card"].Rows[0];
	m_supplierName = dr["short_name"].ToString();
    if(m_supplierName == "")
		m_supplierName = dr["trading_name"].ToString();
	if(m_supplierName == "")
		m_supplierName = dr["company"].ToString();
		
//DEBUG("id=", id);	
	if(id != "")
	{
		m_supplierID = dr["id"].ToString();
		Session["purchase_supplierid" + m_ssid] = m_supplierID;
		
	}
	return true;
}

string showSupplierName(string supplierCode)
{
    if(dst.Tables["showsupplier"] != null)
		dst.Tables["showsupplier"].Clear();
    string sc = "SELECT short_name, trading_name, company FROM card WHERE id = " +  supplierCode;
    try
	{
	    SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "showsupplier") != 1)
            return "";
	}
	catch(Exception e) 
	{
	    ShowExp(sc, e);
		return "";
	}
    if(dst.Tables["showsupplier"].Rows[0]["short_name"].ToString() != "" && dst.Tables["showsupplier"].Rows[0]["short_name"].ToString() != null)
        return dst.Tables["showsupplier"].Rows[0]["short_name"].ToString();
    else if(dst.Tables["showsupplier"].Rows[0]["trading_name"].ToString() != "" && dst.Tables["showsupplier"].Rows[0]["trading_name"].ToString() != null)
        return dst.Tables["showsupplier"].Rows[0]["trading_name"].ToString();
    else if(dst.Tables["showsupplier"].Rows[0]["company"].ToString() != "" && dst.Tables["showsupplier"].Rows[0]["company"].ToString() != null)
        return dst.Tables["showsupplier"].Rows[0]["company"].ToString();
    else
        return "";
}

bool SetSpecialPrice(string code, double dSpecialPrice, double dPrice1, string dtEnd)
{

//	DEBUG("dtend =", dtEnd);
	string sc = " SET DATEFORMAT dmy UPDATE code_relations ";
	sc += " SET normal_price = " + dPrice1 + ", price1 = " + dSpecialPrice;
	sc += ", special_price = " + dSpecialPrice + " ";
	if(dtEnd != "")
		sc += ", special_price_end_date = '" + dtEnd + "' ";
	else
		sc += ", special_price_end_date =  GetDate() + 2 ";
	sc += " WHERE code = " + code;
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

bool UpdateSpecialPriceAndDate(string code, double dSpecialPrice, string dtEnd)
{
	string sc = " SET DATEFORMAT dmy UPDATE code_relations ";
	sc += " SET price1 = " + dSpecialPrice;
	sc += ", special_price = " + dSpecialPrice + ", special_price_end_date = '" + dtEnd + "' ";
	sc += " WHERE code = " + code;
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

bool EndSpecialPrice(string code)
{
	string sc = " SET DATEFORMAT dmy UPDATE code_relations ";
	sc += " SET price1 = isnull(normal_price,0), special_price_end_date = NULL ";
	sc += " WHERE code = " + code;
	sc += " UPDATE promotion SET Status = 'finished', Comments = ISNULL(Comments, '') + '\r\n Ended promotion price on :"+ DateTime.Now.ToString("dd/MM/yyyy HH:mm") +"' WHERE ItemCode = " + code +" AND Status = 'special' ";
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

bool RecordUpdatedItem(string code)
{
	int nRows = 0;
	string sc = " SELECT id FROM branch WHERE activated = 1 AND id > 1 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(dstcom, "rui_branches");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = "";
	for(int i=0; i<nRows; i++)
	{
		string branch_id = dstcom.Tables["rui_branches"].Rows[i]["id"].ToString();
		sc += " IF NOT EXISTS(SELECT id FROM updated_item WHERE branch_id = " + branch_id + " AND item_code = " + code + ") ";
		sc += " INSERT INTO updated_item (branch_id, item_code) VALUES(" + branch_id + ", " + code + ") ";
	}
	if(sc == "")
		return true;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception er)
	{
		ShowExp(sc, er);
		return false;
	}
	return true;
}
</script>
<form runat=server>

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
</asp:DataGrid>
<br>
<asp:Label id=LOtherSupplier runat=server/>

</form>
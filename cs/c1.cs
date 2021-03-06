<!-- #include file="kit_fun.cs" -->

<script runat=server>

DataSet ds = new DataSet();
string alex_sort="supplier_code";
string mainTitleIndex = "s_cat";
string subTableIndex = "brand";
int rows_return = 0;

string brand = null;
string cat = null;
string s_cat = null;
string ss_cat = null;

//decode "zzzOthers"
string dbrand = null;
string dcat = null;
string ds_cat = null;
string dss_cat = null;

//encoded space
string ebrand = null;
string ecat = null;
string es_cat = null;
string ess_cat = null;

string m_sort = "brand";

//bool m_bCacheEnabled = true; //debug
bool m_bCacheEnabled = false; //debug
bool bShowSpecial = false;
bool m_bAdmin = false;
bool m_bPaging = false;
bool bSayHot = true;
bool m_bAddToCart = false;

bool bAlterTableRowColor = false;  //table row color
bool bEnable_Allocated_Stock = false;
string scn = "";

int m_nPage = 1;
//const int m_nPageSize = 100;
int m_nPageSize = 21;

bool bIncludeGST = true;
bool bShowAllStock = true;
bool bOrder = false;

string m_sBuyType = "";

double m_levelDiscount = 0;
double m_ld1 = 1.08;
double m_ld2 = 1.04;
int m_qb1 = 2;
int m_qb2 = 5;
int m_qb3 = 10;
int m_qb4 = 50;

int m_nDealerLevel = 1;

bool m_bClearance = false;
bool m_bDiscontinued = false;
bool m_bShowLogo = false;
bool m_bDoAction = false;
string m_action = "";

bool m_bKit = false;
int m_nItemsPerRow = 1;

bool m_bNoPrice = false;

//for search
bool m_bSearching = false;
string keyword = "";
string kw = "";
string kw1 = ""; //first search step;
string kw2 = ""; //2nd search step;
string kw3 = ""; //3rd search step;
string ss = ""; //current search string;
string quick = ""; //identify if is qpurchase;

int words = 0; //how many keywords, not include in quotation marks
int uwords = 0; //how many unwanted keywords, not include in quotation marks
string[] kws = new string[64];	//wanted keywords
string[] ukws = new string[64]; //un wanted keywords

bool m_bSimpleInterface = false; //liveedit settings, if true then ignore price rate and qty breaks, go for p.price
bool m_bFixedPrices = false;
bool m_bUseLastSalesFixedPrice = false; 
bool m_bStockSayYesNo = false;
string m_sStockYesString = "YES";
string m_sStockNoString = "NO";
string[] m_aBranch = new string[64];
int m_branches = 1;
double m_dGSTRate = 1;
string m_sIMGOnOff = "";
bool m_bItemImageOn = false;
double m_dPOSPrice = 0;

bool m_bOnSalesOrderMode = false;
string m_type = "";
bool bCheck_qty = false; // cil -- stcok greater 20 show yes

bool CatalogInitPage()
{
//DEBUG("begin:", DateTime.Now.ToString());
//	if(g_bRetailVersion)
		TS_PageLoad(); //this is dealer area, check login
//	else
//		TS_Init();

	InitKit();

	// m_type = g("t");
	// m_action = g("a");
	// if(m_action == "ajaxShoppingCart")
	// {
	// 	AjaxShoppingCart();
	// 	return true;
	// }
	
	string ilurl = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	Session["item_list_url"] = ilurl;
	m_dGSTRate = MyDoubleParse(Session[m_sCompanyName +"gst_rate"].ToString());				// 30.JUN.2003 XW

	if(m_dGSTRate < 1)
		m_dGSTRate = 1+m_dGSTRate;

		//item rows pages
	m_nPageSize = int.Parse(GetSiteSettings("item_rows_paging", "50"));

	////Set show Images On/Off on site pages START HERE //////
	string queryStr = Request.ServerVariables["QUERY_STRING"];

	queryStr = queryStr.Replace("&img=off", "");
	queryStr = queryStr.Replace("&img=on", "");

	if(Session["switch_item_image_off_"+ m_sCompanyName] != null)
	{
		m_sIMGOnOff = "<a title='show images' href='"+ Request.ServerVariables["URL"] +"?"+ queryStr +"&img=on'>ON</a>";
		m_sIMGOnOff += " | ";
		m_sIMGOnOff += "<font color=gray>OFF</font>";
		m_bItemImageOn = false;
	}
	else
		m_sIMGOnOff = "<font color=gray>ON</font> | <a title='disabled images' href='"+ Request.ServerVariables["URL"] +"?"+ queryStr +"&img=off'>OFF</a>";
	if(Request.QueryString["img"] == "on"){
		m_bItemImageOn = true;
		Session["switch_item_image_off_"+ m_sCompanyName] = null;				
	}
	//// END Here ///////
	//quick = Request.QueryString["quick"].ToString();
	string sbranch = GetSiteSettings("show_stock_branch_idz", "1,2,3");
	m_aBranch[0] = "1";
	if(Session["branch_support"] != null)
	{
	if(sbranch != "")
	{
		int i = 0;
		int j = 0;
		string ones = "";

		for(i=0; i<sbranch.Length; i++)
		{
			if(sbranch[i] == ' ' || sbranch[i] == ',' || sbranch[i] == ';')
			{
				Trim(ref ones);
				if(ones != "")
				{
					if(!TSIsDigit(ones))
						break;
					m_aBranch[j++] = ones;
					ones = "";
				}
			}
			else
			{
				ones += sbranch[i];
			}
		}
		if(ones != "" && TSIsDigit(ones)) //the last one
			m_aBranch[j++] = ones;
		m_branches = j;
	}
	}
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		m_bFixedPrices = true;

	if(MyBooleanParse(GetSiteSettings("enable_dealer_level_price_in_sales_mode", "0", true)) && m_sSite != "www")
		m_bFixedPrices = false;

	if(int.Parse(GetSiteSettings("dealer_levels", "1")) <= 0)
		m_bFixedPrices = true;

	if(MyBooleanParse(GetSiteSettings("Enable_Use_Last_Sales_Fixed_Price", "0", true)))
		m_bUseLastSalesFixedPrice = true;

	if(g_bRetailVersion)
	{
		if(MyBooleanParse(GetSiteSettings("allocated_stock_public_enabled", "1", true)))
			bEnable_Allocated_Stock = true;
	}
	if(MyBooleanParse(GetSiteSettings("simple_liveedit", "1", true)))
		m_bSimpleInterface = true;

	if(!g_bRetailVersion && m_sSite == "www")
		m_bNoPrice = true;

	if(MyBooleanParse(GetSiteSettings("stock_say_yes_no", "0", false)))
		m_bStockSayYesNo = true;

	if(m_bStockSayYesNo)
	{
		m_sStockYesString = GetSiteSettings("stock_yes_string", "YES");
		m_sStockNoString = GetSiteSettings("stock_no_string", "NO");
	}

	if(Session[m_sCompanyName + "dealer_level"] == null)
		Session[m_sCompanyName + "dealer_level"] = "1";

	if(TS_UserLoggedIn())
		m_nDealerLevel = MyIntParse(Session[m_sCompanyName + "dealer_level"].ToString());

	//stop ordering status, back to normal
	if(Request.QueryString["endorder"] == "1")
	{
		Session[m_sCompanyName + "_ordering"] = null;
		Session[m_sCompanyName + "_salestype"] = null;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?\">");
		return true;
	}
	else if(Request.QueryString["startorder"] == "1")
	{
		Session[m_sCompanyName + "_ordering"] = true;	
	}

	if(Session[m_sCompanyName + "_ordering"] != null)
		m_bOnSalesOrderMode = true;

	//sales session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		m_ssid = Request.QueryString["ssid"];
	
	if(Request.QueryString["t"] == "clearance")
		m_bClearance = true;
	else if (Request.QueryString["t"] == "discontinued")
		m_bDiscontinued = true;

	string cmd = "";
	if(Request.Form["cmd"] != null)
		cmd = Request.Form["cmd"];

	if(cmd == "Action")
	{
		m_bDoAction = true;
		m_action = Request.Form["action"];
	}
	else if(cmd == "Update Price")
	{
	}
	else if(cmd == "Add To Cart")
	{
		m_bAddToCart = true;
	}
	if(m_action == "buy")
	{
		m_bAddToCart = true;
	}
	
	RememberLastPage();
	PrintHeaderAndMenu();

	if(Request.QueryString["gst"] == "e")
	{
		Session["display_include_gst"] = "false";
	}
	else if(Request.QueryString["gst"] == "i")
		Session["display_include_gst"] = "true";

	if(Session["display_include_gst"] != null)
	{
		if(Session["display_include_gst"].ToString() == "false")
			bIncludeGST = false;
	}
	else
		Session["display_include_gst"] = "true";

	//sort options
	if(Request.QueryString["asort"] != null){
		Session[m_sCompanyName + "_asort"]=Request.QueryString["asort"];
		alex_sort=Session[m_sCompanyName + "_asort"].ToString();
		m_bCacheEnabled = false; 
	}else{
		Session[m_sCompanyName + "_asort"]="supplier_code";
	}
		
		//refresh cache
	if(Request.QueryString["sort"] != null)
		m_bCacheEnabled = false; //refresh cache
	if(Request.QueryString["sort"] == "price")
		Session[m_sCompanyName + "_sort"] = "price";
	else if(Request.QueryString["sort"] == "brand")
		Session[m_sCompanyName + "_sort"] = "brand";

	if(Session[m_sCompanyName + "_sort"] != null)
		m_sort = Session[m_sCompanyName + "_sort"].ToString();

	Session["display_show_all_stock"] = "true";
	
	if(Request.QueryString["sas"] == "1")
		Session["display_show_all_stock"] = "true";
	else if(Request.QueryString["sas"] == "0")
		Session["display_show_all_stock"] = null;

	if(Session["display_show_all_stock"] == null)
		bShowAllStock = false;
	else if(Session["display_show_all_stock"].ToString() == "true")
		bShowAllStock = true;

//	if(bShowAllStock)
//		DEBUG("bShowAllStock=true", "");
//	else
//		DEBUG("bShowAllStock=false", "");

	if(Request.QueryString["p"] != null)
	{
		string sPage = Request.QueryString["p"];
		if(TSIsDigit(sPage))
			m_nPage = int.Parse(sPage);
	}

	string cn = m_sCompanyName + m_sSite + "_b_" + Request.QueryString["b"] + "_c_" + Request.QueryString["c"] + "_s_" + Request.QueryString["s"] + "_ss_" + Request.QueryString["ss"];
	if(bIncludeGST)
		cn += "_igst";
	if(bShowAllStock)
		cn += "_sas";
	if(Request.QueryString["p"] != null)
	{
		cn += "_p" + m_nPage.ToString();
	}
	scn = cn;

	if(m_bCacheEnabled && m_supplierString == "")
	{
		if(Cache[cn] != null)
		{
			Response.Write(Cache[cn]);
			PrintFooter();
			return true; //cache wrote, should not draw table again
		}
	}

        

	if(Request.QueryString["b"] == null && Request.QueryString["c"] == null 
		&& Request.QueryString["s"] == null && Request.QueryString["ss"] == null && m_type != "new")
	{
		bShowSpecial = true;
	}
	else
	{
		brand = Request.QueryString["b"];
		cat = Request.QueryString["c"];
		s_cat = Request.QueryString["s"];
		ss_cat = Request.QueryString["ss"];

		dbrand = brand;
		dcat = cat;
		ds_cat = s_cat;
		dss_cat = ss_cat;
		if(brand == "zzzOthers")
			dbrand = "";
		if(cat == "zzzOthers")
			dcat = "";
		if(s_cat == "zzzOthers")
			ds_cat = "";
		if(ss_cat == "zzzOthers")
			dss_cat = "";

		ebrand = HttpUtility.UrlEncode(dbrand);
		ecat = HttpUtility.UrlEncode(dcat);
		es_cat = HttpUtility.UrlEncode(ds_cat);
		ess_cat = HttpUtility.UrlEncode(dss_cat);
	}
	
//	if(s_cat != null && ss_cat == null && m_sSite == "admin")
	if(brand == null && s_cat != null && ss_cat == null && m_sSite != "admin")
	{
		m_bShowLogo = true;
	}

	if(cat == m_sKitTerm) //package
	{
		m_bKit = true;
		GetKit();
		return false;
	}

	string sca = "12345"; //try again if no hot product later
	string scb = "12345"; //cut "select " and " hot=1" from sc
	string sWhere = "";
	string sc = "";

	bool bHotOnly = false;
	if(m_bClearance)
	{
		sc = "SELECT c.level_price0, c.*, c.moq, c.inner_pack, c.weight, c.barcode, p.price_age, p.supplier, p.supplier_code, p.supplier_price, p.code, p.name, p.brand,  c.stock_location ,p.price";
		if(bIncludeGST)
			sc += "* "+ (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString())) +" AS price";
		if(g_bRetailVersion)
		{
			sc += ", ISNULL((select sum(qty";
			if(bEnable_Allocated_Stock)
				sc += "-allocated_stock";
			sc += ") FROM stock_qty WHERE code = p.code), 0) AS  stock ";
		}
		else if(m_bStockSayYesNo)
			sc += ", p.stock AS stock ";
		else
			sc += ", p.stock-p.allocated_stock AS stock ";
		sc += ", p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
		sc += ", c.currency, c.foreign_supplier_price ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
	//	sc += ", c.manual_cost_nzd * c.rate AS bottom_price ";
		sc += ", (c.rate * c.level_price0) + c.nzd_freight AS bottom_price ";
		sc += ", c.inactive ";
		sc += " FROM product p JOIN code_relations c ON c.id=p.supplier+p.supplier_code ";
		sc += " WHERE c.clearance = 1 ";
		if(m_sSite != "admin")
			sc += " AND c.is_service = 0 ";
		if(m_bOnSalesOrderMode)
			sc += " AND skip = 0 ";
		if(m_supplierString != "")
			sc += " AND LOWER(p.supplier) IN" + m_supplierString.ToLower() + " ";
		if(Session["cat_access_sql"] != null)
		{
			if(Session["cat_access_sql"].ToString() != "all")
			{
				sc += " AND LOWER(c.s_cat) " + (EncodeQuote(Session["cat_access_sql"].ToString())).ToLower();
				sc += " AND LOWER(c.ss_cat) " + (EncodeQuote(Session["cat_access_sql"].ToString())).ToLower();
			}
		}
		sc += "ORDER BY p.brand, p.cat, p.s_cat, p.ss_cat";
// DEBUG("sc cleanrl =", sc);
	}
	else if(m_bDiscontinued)
	{
		sc = "SELECT c.level_price0, c.moq, c.*, c.inner_pack, c.weight, c.barcode, c.code, c.name, c.brand, p.price, c.stock_location";
		if(g_bRetailVersion)
		{
			sc += ", ISNULL((select sum(qty";
			if(bEnable_Allocated_Stock)
				sc += "-allocated_stock";
			sc += ") FROM stock_qty WHERE code = c.code), 0) AS  stock ";
		}
		else
			sc += ", p.stock AS stock ";
		sc += ", c.cat, c.s_cat, c.ss_cat, ";
		sc += " c.supplier, c.supplier_code, c.supplier_price, c.clearance, '' AS eta, 0 AS price_dropped ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
		sc += ", c.currency, c.foreign_supplier_price ";
//		sc += ", c.manual_cost_nzd * c.rate AS bottom_price ";
		sc += ", (c.rate * c.level_price0) + c.nzd_freight AS bottom_price ";
		sc += ", c.inactive ";
		sc += " FROM code_relations c JOIN product_skip p ON c.id=p.id ";
		if(m_sSite != "admin")
			sc += " WHERE c.is_service = 0 ";
		if(m_bOnSalesOrderMode)
			sc += " AND skip = 0 ";
		else if(m_supplierString != "")
			sc += " WHERE c.supplier IN" + m_supplierString + " ";
		if(Session["cat_access_sql"] != null)
		{
			if(Session["cat_access_sql"].ToString() != "all")
			{
				sc += " AND c.s_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
				sc += " AND c.ss_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
			}
		}
		sc += "ORDER BY c.brand, c.cat, c.s_cat, c.ss_cat";
//DEBUG("sc discontinue =", sc);
	}
	else if(bShowSpecial)
	{
		string top = "";
		//if(!specialItem() && m_type != "all" && m_type != "new")
			top = "top 12";
		
		sc = "SELECT " + top + "c.level_price0, c.* , c.moq, c.inner_pack, c.weight, c.barcode, p.price_age, p.supplier, p.supplier_code, p.supplier_price, p.code, p.name, p.brand, c.stock_location, p.price";
		if(bIncludeGST)
			sc += "* "+ (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString())) +" AS price";
		if(g_bRetailVersion)
		{
			sc += ", ISNULL((select sum(qty";
			if(bEnable_Allocated_Stock)
				sc += "-allocated_stock";
			sc += ") FROM stock_qty WHERE code = p.code), 0) AS  stock ";
		}
		else if(m_bStockSayYesNo)
			sc += ", p.stock AS stock ";
		else
			sc += ", p.stock-p.allocated_stock AS stock ";
		sc += " , p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
		sc += ", c.currency, c.foreign_supplier_price ";
		//sc += ", c.manual_cost_nzd * c.rate AS bottom_price ";
		sc += ", (c.rate * c.level_price0) + c.nzd_freight AS bottom_price ";
		sc += ", c.inactive ";
		sc += " FROM product p ";
		//if(specialItem())
		//	sc += "JOIN specials s ON p.code=s.code ";
		sc += " JOIN code_relations c ON c.code = p.code ";
		sc += " WHERE 1 = 1 ";
		if(m_sSite != "admin")
			sc += " AND c.is_service = 0 AND c.hidden ='0' ";
		if(m_bOnSalesOrderMode)
			sc += " AND skip = 0 ";
		else if(m_supplierString != "")
			sc += " AND p.supplier IN" + m_supplierString + " ";
		if(m_type == "new")
			sc += " AND c.new_item = 1 ";
		if(Session["cat_access_sql"] != null)
		{
			if(Session["cat_access_sql"].ToString() != "all")
			{
				sc += " AND p.s_cat " + Session["cat_access_sql"].ToString();
				sc += " AND p.ss_cat " + Session["cat_access_sql"].ToString();
			}
		}
        //sc += " AND c.special_price_end_date > GetDate() ";
		// sc += "ORDER BY p.name, p.brand, p.cat, p.s_cat, p.ss_cat desc";
		 sc += "ORDER BY p.name, p.brand, p.cat, p.s_cat, p.ss_cat ";
		//   sc += "ORDER BY c.supplier_code desc";
		//  DEBUG("sc speical =", alex_sort);
	}
	else
	{
		sc = "c.level_price0, c.moq, c.inner_pack,c.* , c.weight, c.barcode, p.price_age, p.supplier, p.supplier_code, p.supplier_price, p.code, p.name, p.brand,c.stock_location , p.price ";
		if(bIncludeGST)
			sc += "* "+ (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()))  +" AS price";
		if(g_bRetailVersion)
		{
			sc += ", ISNULL((select sum(qty";
			if(bEnable_Allocated_Stock)
				sc += "-allocated_stock";
			sc += " ) FROM stock_qty WHERE code = p.code), 0) AS  stock ";
		}
		else if(m_bStockSayYesNo)
			sc += ", p.stock AS stock ";
		else
			sc += ", p.stock-p.allocated_stock AS stock ";
		sc += " , p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
		sc += ", c.currency, c.foreign_supplier_price ";
		//sc += ", c.manual_cost_nzd * c.rate AS bottom_price ";
		sc += ", (c.rate * c.level_price0) + c.nzd_freight AS bottom_price ";
		sc += ", c.inactive ";
		sc += " FROM product p";
		sc += " JOIN code_relations c ON c.code=p.code ";
		if(m_sSite != "admin")
			sc += " WHERE c.is_service = 0 AND c.hidden ='0' AND ";
		else
			sc += " WHERE ";
		if(!bShowAllStock)
			sc += " (p.stock>0 OR p.stock IS NULL) AND ";
		if(m_type == "new")
			sc += " c.new_item = 1 AND ";
		if(brand != null)
		{
			sWhere += " LOWER(p.brand)='";
			sWhere += EncodeQuote(dbrand).ToLower();
			sWhere += "'";
			mainTitleIndex = "brand";
			subTableIndex = "ss_cat";
		}
		
		if(cat != null)
		{
			if(sWhere != "")
				sWhere += " AND";
			sWhere += " LOWER(p.cat)='";
			sWhere += EncodeQuote(dcat).ToLower();
			sWhere += "'";
		}

		if(Session["cat_access_sql"] != null)
		{
			if(Session["cat_access_sql"].ToString() != "all")
			{
				string limit = Session["cat_access_sql"].ToString();
				limit = EncodeQuote(limit);
				sWhere += " AND (p.brand " + limit;
				if(limit.ToLower().IndexOf("not") >= 0)
					sWhere += " AND ";
				else
					sWhere += " OR ";
				sWhere += " p.s_cat " + limit + " AND p.ss_cat " + limit + ") ";
			}
		}
		
		if(s_cat != null)
		{
			if(sWhere != "")
				sWhere += " AND";
			sWhere += " LOWER(p.s_cat)='";
			sWhere += EncodeQuote(ds_cat).ToLower();
			sWhere += "'";
		}
		if(ss_cat != null)
		{
			if(sWhere != "")
				sWhere += " AND";
			sWhere += " LOWER(p.ss_cat)='";
			sWhere += EncodeQuote(dss_cat).ToLower();
			sWhere += "'";
		}
//		sWhere += " AND p.cat NOT IN('networking') ";

		scb = sc + sWhere;
		
		if((ss_cat == null && brand == null && ss_cat != "zzzOthers" && brand != "zzzOthers") 
			|| (s_cat == null && brand != null && s_cat != "zzzOthers" && brand != "zzzOthers"))
		{
			if(m_type != "all" && m_type != "new")
			{
				bHotOnly = true;
				if(sWhere != "")
					sWhere += " AND";
				sWhere += " c.hot=1";
			}
			if(m_type == "new")
			{
				if(sWhere != "")
					sWhere += " AND";
				sWhere += " c.new_item = 1 ";
			}
		}
		
		sc += sWhere;
		if(m_bOnSalesOrderMode)
			sc += " AND skip = 0 ";
		sc = "SELECT " + sc;
		sca = sc;

		if(m_supplierString != "")
		{
			sc += " AND p.supplier IN" + EncodeQuote(m_supplierString) + " ";
		}
		sc += " ORDER BY p.brand, p.s_cat, p.ss_cat, p.name, p.code";
		// sc += " ORDER BY c.code desc";
	// DEBUG("sc1 =", sc);
	}
//if(Session["email"].ToString().IndexOf("darcy@") >= 0)
	// DEBUG(" sc =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows_return = myCommand.Fill(ds, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return true;
	}

	string scWhere = "";
	int cross = 0;
	if(!bShowSpecial && brand == null)// && ss_cat != null)
	{
		sc = "SELECT code FROM cat_cross WHERE";
		if(cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " LOWER(cat)='";
			scWhere += EncodeQuote(dcat).ToLower();
			scWhere += "'";
		}
		if(s_cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " LOWER(s_cat)='";
			scWhere += EncodeQuote(ds_cat).ToLower();
			scWhere += "'";
		}
		if(ss_cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " LOWER(ss_cat)='";
			scWhere += EncodeQuote(dss_cat).ToLower();
			scWhere += "'";
		}
		sc += scWhere;
		if(scWhere != "")
		{
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				cross = myCommand.Fill(ds, "cross");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return true;
			}
			if(cross > 0)
			{
		sc += ", ISNULL((select sum(qty";
			if(bEnable_Allocated_Stock)
				sc += "-allocated_stock";
			sc += ") FROM stock_qty WHERE code = p.code), 0) AS  stock ";


				for(int c=0; c<cross; c++)
				{
					string ccode = ds.Tables["cross"].Rows[c]["code"].ToString();
					sc = "SELECT c.level_price0, c.moq,c.* , c.inner_pack, c.weight, c.barcode, p.price_age, p.supplier, p.supplier_code, p.supplier_price, p.code, p.name, p.brand, p.price";
					if(bIncludeGST)
						sc += "* "+ (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString())) +" AS price";
					// if(m_bStockSayYesNo)
					// 	sc += ", p.stock AS stock ";
					// else 
					// 	sc += ", p.stock-p.allocated_stock AS stock ";
						sc += ", ISNULL((select sum(qty";
			if(bEnable_Allocated_Stock)
				sc += "-allocated_stock";
			sc += ") FROM stock_qty WHERE code = p.code), 0) AS  stock ";

					sc += ", p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
					for(int j=1; j<=9; j++)
						sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
					sc += ", c.currency, c.foreign_supplier_price ";
					//sc += ", c.manual_cost_nzd * c.rate AS bottom_price ";
					sc += ", (c.rate * c.level_price0) + c.nzd_freight AS bottom_price ";
					sc += ", c.inactive ";
					sc += " FROM product p JOIN code_relations c ON c.code=p.code WHERE p.code=" + ccode;
					if(m_sSite != "admin")
						sc += " AND c.is_service = 0  ";
					if(bHotOnly)
						sc += " AND c.hot=1";
					if(m_bOnSalesOrderMode)
						sc += " AND skip = 0 ";
					if(m_type == "new")
						sc += " AND c.new_item = 1 ";
					if(m_supplierString != "")
						sc += " AND p.supplier IN" + EncodeQuote(m_supplierString) + " ";
					if(Session["cat_access_sql"] != null)
					{
						if(Session["cat_access_sql"].ToString() != "all")
						{
							sc += " AND c.s_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
							sc += " AND c.ss_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
						}
					}
                   
                    //sc += " OR p.code=" + ccode;
 //DEBUG("sc_c=", sc);
					try
					{
						SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
						rows_return += myCommand.Fill(ds, "product");
					}
					catch(Exception e) 
					{
						ShowExp(sc, e);
						return true;
					}
				}
			}
		}
		
		//kit cross
		sc = "SELECT code FROM cat_cross_kit WHERE";
		if(cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " LOWER(cat)='";
			scWhere += EncodeQuote(dcat).ToLower();
			scWhere += "'";
		}
		if(s_cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " LOWER(s_cat)='";
			scWhere += EncodeQuote(ds_cat).ToLower();
			scWhere += "'";
		}
		if(ss_cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " LOWER(ss_cat)='";
			scWhere += EncodeQuote(dss_cat).ToLower();
			scWhere += "'";
		}
		sc += scWhere;
		if(scWhere != "")
		{
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				cross = myCommand.Fill(ds, "cross_kit");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return true;
			}
			if(cross > 0)
			{
				for(int c=0; c<cross; c++)
				{
					string ccode = ds.Tables["cross_kit"].Rows[c]["code"].ToString();
					sc = " SELECT 1 AS moq, c.inner_pack, c.weight, c.* , id as barcode, '01/01/2001' AS price_age, '' AS supplier, '' AS supplier_code, 0 AS supplier_price ";
					sc += ", id AS code, '' AS brand, 0 AS stock, '" + m_sKitTerm + "' AS cat, '' AS eta ";
					sc += ", 0 AS price_dropped, 0 AS clearance, 1 AS currency, 0 AS foreign_supplier_price ";
					for(int j=1; j<=9; j++)
						sc += ", 1 AS level_rate" + j + ", 1 AS qty_break" + j + ", 0 AS qty_break_discount" + j + ", 0 AS price" + j;
					sc += ", * FROM kit ";
					sc += " WHERE id = " + ccode;
					if(Session["cat_access_sql"] != null)
					{
						if(Session["cat_access_sql"].ToString() != "all")
						{
							sc += " AND s_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
							sc += " AND ss_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
						}
					}

					try
					{
						SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
						rows_return += myCommand.Fill(ds, "product");
					}
					catch(Exception e) 
					{
						ShowExp(sc, e);
						return true;
					}
				}
			}
		}
	
	}		

	if(rows_return <= 0)
	{
		if(bHotOnly) //try display all
		{
			//sc = "SELECT TOP 10 ";
			sc = " SELECT "; // no more hot key...
			sc += scb; //sca.Substring(7, sca.Length-17);
			if(m_supplierString != "")
				sc += " AND supplier IN" + EncodeQuote(m_supplierString) + " ";
			sc += " ORDER BY p.brand, p.s_cat, p.ss_cat, p.name, p.code";
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				rows_return = myCommand.Fill(ds, "product");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return true;
			}
			if(rows_return <= 0)
			{
				//for corporate
				if(m_supplierString == "")
					return true;
			}
			bSayHot = false; //don't display word " - hot product"

		}
		else
		{
			//for corporate
			if(m_supplierString == "")
				return false;
			sc = "SELECT TOP 10 ";
			sc += "c.level_price0, c.moq, c.inner_pack,c.* , c.weight, c.barcode,  p.price_age, p.supplier, p.supplier_code, p.supplier_price, p.code, p.name, p.brand, p.price";
			if(bIncludeGST)
				sc += "* "+ (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString())) +" AS price";
			sc += ", p.stock-p.allocated_stock AS stock, p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
			for(int j=1; j<=9; j++)
				sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
			sc += ", c.currency, c.foreign_supplier_price ";
			//sc += ", c.manual_cost_nzd * c.rate AS bottom_price ";
			sc += ", (c.rate * c.level_price0) + c.nzd_freight AS bottom_price ";
			sc += ", c.inactive ";
			sc += " FROM product p JOIN code_relations c ON c.id=p.supplier+p.supplier_code WHERE p.supplier IN " + m_supplierString;
			if(m_sSite != "admin")
				sc += " c.is_service = 0 ";
			if(m_bOnSalesOrderMode)
				sc += " AND skip = 0 ";
			if(m_type == "new")
				sc += " AND c.new_item = 1 ";
			if(Session["cat_access_sql"] != null)
			{
				if(Session["cat_access_sql"].ToString() != "all")
				{
					sc += " AND c.s_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
					sc += " AND c.ss_cat " + EncodeQuote(Session["cat_access_sql"].ToString());
				}
			}
			sc += " ORDER BY price DESC";
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				rows_return = myCommand.Fill(ds, "product");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return true;
			}

			if(rows_return <= 0)
				return true;
			bSayHot = false; //don't display word " - hot product"
// DEBUG("sc_f=", sc);
		}
	}

	if(bShowSpecial)
	{
		GetSpecialKits();
	}

	return false;
}

bool specialItem()
{
	if(ds.Tables["specialItems"] != null)
		ds.Tables["specialItems"].Clear();
	string sc = " SELECT *  FROM specials ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "specialItems") <=0)
			return false;
		
	}
	catch (Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string PrintURI()
{
	StringBuilder sb = new StringBuilder();
	if(brand != null)
		sb.Append("b=" + HttpUtility.UrlEncode(ebrand) + "&");
	if(cat != null)
		sb.Append("c="+ HttpUtility.UrlEncode(ecat));
	if(s_cat != null)
		sb.Append("&s=" + HttpUtility.UrlEncode(es_cat));
	if(ss_cat != null)
		sb.Append("&ss=" + HttpUtility.UrlEncode(ess_cat));
	if(m_ssid != "")
		sb.Append("&ssid=" + m_ssid);
	return sb.ToString();
}

void CatalogDrawList(bool bAdmin) //if bAdmin then draw adminstratrion menu
{
	m_bAdmin = bAdmin;
	StringBuilder sb = new StringBuilder();

	//for search result print
	if(m_bSearching)
	{
		m_bShowLogo = false;
	}
	if(m_bShowLogo)
	{
		string sl = ShowLogo();
		if(sl != "")
		{
			sb.Append("&nbsp;");
			sb.Append(sl);
			sb.Append("</td><tr><tr><td alin=right>");
			sb.Append("<font color=red><i><b>This sub catageory default page layout is patented</b></i></font> &nbsp&nbsp; ");
			sb.Append("<a href=catlogo.aspx?s=" + es_cat + " class=o>Edit Images</a>");
			sb.Append("</td></tr></table>");
			sb.Append("</td></tr></table>");
			Response.Write(sb.ToString());
			return;
		}
	}
	if(Session[m_sCompanyName + "_ordering"] != null)
		bOrder = true;
	if(Session[m_sCompanyName + "_salestype"] != null)
		m_sBuyType = Session[m_sCompanyName + "_salestype"].ToString();
	if(bOrder)
	{
		//use current customer level for POS
		if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] != null)
			m_nDealerLevel = MyIntParse(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString());
	}
	if(Request.QueryString.Count <= 0)
	{
		Session[m_sCompanyName + "_ordering"] = null;
		bOrder = false;
	}

	//left side menu
	string sMId = m_sCompanyName + m_sSite + "cache_leftmenu_";
	if(brand != null)
	{
		sMId += "brands_";
		sMId += brand;
	}
	else
	{
		sMId += cat;
		sMId += "_";
		sMId += s_cat;
	}
	sMId = sMId.ToLower();

	string lsm_id = "left_side_menu";
	if(m_sSite == "www")
		lsm_id = "public_left_side_menu";
	string lsm = ReadSitePage(lsm_id);

	if(lsm != "")
	{
		lsm = BlockSysQuote(lsm);
		string kw = "";
		string kwCompare = "";
		if(Request.QueryString["kw"] != null && Request.QueryString["kw"] != "")
			kwCompare = Request.QueryString["kw"].ToString();
		if(Session["search_keyword"] != null)
		{
			if(Session["search_keyword"].ToString() == kwCompare)
				kw = Session["search_keyword"].ToString();
			else
				kw = kwCompare;
		}			
		lsm = lsm.Replace("@@search_keyword", kw);
      
		//*** add switch on off of item images
		lsm = lsm.Replace("@@IMAGE_ONOFF_LINK", m_sIMGOnOff);
		//*** add show best seller items here #12-12-04 tee
		if(bShowSpecial)
		lsm = lsm.Replace("@@todayspecail", "Today's Specials");
		
		if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
		{///login is true..  so allow hiding the login menu on body item and also on the menu header and cat header..
			lsm = lsm.Replace("@@HIDE_LOGIN_FRONT", "<!--");
			lsm = lsm.Replace("@@HIDE_LOGIN_END", "-->");
			lsm = lsm.Replace("@@LOGIN_NAME", Session["name"].ToString());
		}
		else
		{
			lsm = lsm.Replace("@@HIDE_LOGIN_FRONT", "");
			lsm = lsm.Replace("@@HIDE_LOGIN_END", "");
			lsm = lsm.Replace("@@LOGIN_NAME", "Guest");
		}
	}

	string tp = ReadSitePage("public_body_item_list");
	if(g_bOrderOnlyVersion)
		tp = ReadSitePage("public_body_item_list_orderonly");
	//if(!bShowSpecial)
	//    tp = ReadSitePage("public_body_item_list_orderonly");
	if(m_bDealerArea || m_sSite == "admin")
		tp = ReadSitePage("body_item_list");
	if(m_sSite == "dealer")
		tp = ReadSitePage("body_item_list_dealer");
		
	//print page index
	if(ds.Tables["product"] == null || ds.Tables["product"].Rows.Count <= 0)
		return;

	int rows = ds.Tables["product"].Rows.Count;
	string slPageFirst = "";
	string slPagePrev = "";
	string slPage = "";
	string slPageNext = "";
	string slPageLast = "";
	string keywordSearch = Request.QueryString["kw"];
	if(rows > m_nPageSize)
	{
		m_bPaging = true;
		int pages = rows / m_nPageSize + 1;
		if(m_nPage > 1)
		{
			slPagePrev = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + (m_nPage-1).ToString();
			slPageNext = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + (m_nPage).ToString();
			slPageLast = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + pages.ToString();
			slPageFirst = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=1";
		}
		else
		{
			slPagePrev = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=1";			
			slPageFirst = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=1";			
		}
		for(int p=1; p<=pages; p++)
		{
			if(p != m_nPage)
			{
				if(m_sSite != "admin")
					slPage += "<li>";
				slPage += "<a href=";
				slPage += WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + p.ToString()+"&asort="+ HttpUtility.UrlEncode(alex_sort);
				slPage += "> ";
			}
			else
			{
				if(m_sSite != "admin")
					slPage += "<li class=\"active-2\"><a href=\"#\">";
				else
					slPage += "<font size=+1><b>";
			}
			slPage += p.ToString();
			if(p != m_nPage)
			{
				if(m_sSite != "admin")
					slPage += " </a></li>";
				else
					slPage += " </a>";
			}
			else
			{
				if(m_sSite != "admin")
					slPage += "</a></li>";
				else
					slPage += "</b></font>";
			}
			slPage += " ";
		}
		if(m_nPage < pages)
		{
            pages = pages-1;
			slPageNext = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + (m_nPage+1).ToString()+"&asort="+ HttpUtility.UrlEncode(alex_sort);
			slPageLast = WriteURLWithoutPageNumber() + "&t=" + m_type + "&kw="+ HttpUtility.UrlEncode(keywordSearch) +"&p=" + pages.ToString()+"&asort="+ HttpUtility.UrlEncode(alex_sort);
		}
		tp = tp.Replace("@@PAGE_LINK_FIRST", slPageFirst);
		tp = tp.Replace("@@PAGE_LINK_PREV", slPagePrev);
		tp = tp.Replace("@@PAGE_LINK_PAGES", slPage);
		tp = tp.Replace("@@PAGE_LINK_NEXT", slPageNext);
		tp = tp.Replace("@@PAGE_LINK_LAST", slPageLast);
	}

	//parse conditions, IF_SPECIAL, IF_PURCHAE etc.
	tp = TemplateParseCommand(tp);

	bool bHasGroup = true;
	string tp_group = GetRowTemplate(ref tp, "itemgroup");
	string tp_row = GetRowTemplate(ref tp_group, "itemrow");
	if(tp_group == "")
	{
		bHasGroup = false;
		tp_group = GetRowTemplate(ref tp, "itemrow");
		tp_row = tp_group;
	}
	string tp_item = GetRowTemplate(ref tp_row, "rowitem");
	string ssmenu = "";
	if(Cache[sMId] != null)
	{
		ssmenu = Cache[sMId].ToString();
		ssmenu = ssmenu.Replace("@@ssid", m_ssid);		
	}
	
	ssmenu = ssmenu.Replace("@@tcat", s_cat);
	if(m_bSearching)
		ssmenu = "";

	ssmenu += lsm;

	if(tp.IndexOf("@@SUB_SUB_MENU") >= 0)
		tp = tp.Replace("@@SUB_SUB_MENU", ssmenu);
	else
		tp = tp.Replace("@@LEFT_SIDE_MENU", ssmenu);
	
	string sout = tp;
	if(Cache["item_categories"] != null)
		sout = sout.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	else
		sout = sout.Replace("@@HEADER_MENU_TOP_CAT", "");
/*
	if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
	{///login is true..  so allow hiding the login menu on body item and also on the menu header and cat header..
	
	    string p_logoff = @" 
	   <table width=200 cellpadding=0 cellspacing=0 border=0>
		 <tr>
		  <td align=right><a href=status.aspx? ><img src=i/status.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=status.aspx? >Order Status</a></td>
		  </tr>
			<tr>
		  <td align=right><a href=register.aspx ><img src=i/myacc.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=register.aspx >Update Details</a></td>
		  </tr>
		   <tr>
		  <td align=right><a href=setpwd.aspx ><img src=i/setpwd.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=setpwd.aspx >Change Password</a></td>
		  </tr>
		 
			<tr>
		  <td align=right><a href=login.aspx?logoff=true ><img src=i/logout.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=login.aspx?logoff=true >Logout</a></td>
		  </tr>
		  </table>";
		sout = sout.Replace("@@Public_Logout_Right", p_logoff );
		sout = sout.Replace("@@HIDE_LOGIN_FRONT", "<!--");
		sout = sout.Replace("@@HIDE_LOGIN_END", "-->");
		sout = sout.Replace("@@LOGIN_NAME", Session["name"].ToString());
	}
	else
	{
	    string p_logoff = ReadSitePage("public_login");
			
		sout = sout.Replace("@@Public_Logout_Right", p_logoff);
		sout = sout.Replace("@@HIDE_LOGIN_FRONT", "");
		sout = sout.Replace("@@HIDE_LOGIN_END", "");
		sout = sout.Replace("@@LOGIN_NAME", "Guest");
	}
 */
string lbtn_in  = "<input type=image src='images/sign_in.gif' name=go id='login_btn'   />"; //<input type="image" src=""

	
	
	string login_name ="User Name: <input type='hidden' name='use_cookie' value='true' /><input   type='text' name='name' class='login_b'  autocomplete='false'/><input type='hidden' name='name_old' value=''>";
	string login_pass = "Password:  <input  name='pass' type='password'  class='login_b'  autocomplete='false'/>";
	string login_reg ="<a href='Register.aspx'>Register Now!</a>";
	string login_time = DateTime.Now.ToString("dd-MM-yyyy");
	
	string login_block =@"
						<div id='log_acc' class='log_block'>
							<a href='sp.aspx?account'>
								<table width='52' cellpadding='0' cellspacing='0' border='0'>
									<tr><td height='55' align='center' valign='middle'><img src='images/my_acc.gif'/></td></tr>
									<tr><td height='25' align='center' valign='TOP'>MyAccount</td></tr>
								</table>
							</a>
						</div>
						<div id='log_cart' class='log_block'>
							<a href='cart.aspx'>
								<table width='52' cellpadding='0' cellspacing='0' border='0'>
									<tr><td height='55' align='center' valign='middle'><img src='images/view_c.gif'/></td></tr>
									<tr><td height='25' align='center' valign='TOP'>View Cart</td></tr>
								</table>
							</a>
						</div>
						<div id='log_sign' class='log_block'>
							<a href='login.aspx?logoff=true'>
								<table width='52' cellpadding='0' cellspacing='0' border='0'>
									<tr><td height='55' align='center' valign='middle'><img src='images/sign_out.gif'/></td></tr>
									<tr><td height='25' align='center' valign='TOP'>Sign Out</td></tr>
								</table>
							</a>
						</div>";
		string login_top =@"
					<a class='top_base' href='sp.aspx?account'>MyAccount</a> &nbsp; | &nbsp; <a href='login.aspx?logoff=true' class='top_base'>Logout</a>";

		string logout_top =@"
					<a class='top_base_in' href='login.aspx'>Login</a> &nbsp; | &nbsp; <a href='Register.aspx' class='top_base'>Register</a>";
	
	if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
	{///login is true..  so allow hiding the login menu on body item and also on the menu header and cat header..
	
	    string p_logoff = @" 
	   <table width=200 cellpadding=0 cellspacing=0 border=0>
		 <tr>
		  <td align=right><a href=status.aspx? ><img src=i/status.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=status.aspx? >Order Status</a></td>
		  </tr>
			<tr>
		  <td align=right><a href=register.aspx ><img src=i/myacc.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=register.aspx >Update Details</a></td>
		  </tr>
		   <tr>
		  <td align=right><a href=setpwd.aspx ><img src=i/setpwd.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=setpwd.aspx >Change Password</a></td>
		  </tr>
		 
			<tr>
		  <td align=right><a href=login.aspx?logoff=true ><img src=i/logout.gif border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=login.aspx?logoff=true >Logout</a></td>
		  </tr>
		  </table>";
		sout = sout.Replace("@@Public_Logout_Right", p_logoff );
		sout = sout.Replace("@@HIDE_LOGIN_FRONT", "<!--");
		sout = sout.Replace("@@HIDE_LOGIN_END", "-->");
		sout = sout.Replace("@@LOGIN_NAME", "Welcome "+Session["name"].ToString());
		sout = sout.Replace("@@login_a", "<span id='log_name'>"+Session["name"].ToString()+"</span><span> &nbsp; Welcome</span>");
		sout = sout.Replace("@@login_b", login_block);
		sout = sout.Replace("@@lbtn", "<span>"+login_time+"</span>");
		sout = sout.Replace("@@reg", ""); 
		sout = sout.Replace("@@111", login_top);

		
	}
	else
	{
	    string p_logoff = ReadSitePage("public_login");
			
		sout = sout.Replace("@@Public_Logout_Right", p_logoff);
		sout = sout.Replace("@@HIDE_LOGIN_FRONT", "");
		sout = sout.Replace("@@HIDE_LOGIN_END", "");
		sout = sout.Replace("@@LOGIN_NAME", "Guest");
		
		sout = sout.Replace("@@login_a",  login_name);
		sout = sout.Replace("@@login_b", login_pass);
		sout = sout.Replace("@@lbtn", lbtn_in);
		sout = sout.Replace("@@reg", login_reg);
		sout = sout.Replace("@@111", logout_top);
	}
	
	// title and title_desc
	string title = "";
	if(bShowSpecial)
	{
		if(m_supplierString == "")
		{
			if(m_bClearance)
				title = "Clearance Center";
			else if(m_bDiscontinued)
				title = "Discontinued Items";
			else
				title = "Today's Specials";
		}
		else
			title = m_sCompanyTitle;
	}
	else
	{
		if(ds.Tables["product"].Rows.Count > 0)
			title = ds.Tables["product"].Rows[0][mainTitleIndex].ToString();
	}

	string title_desc = "";
	if(!bShowSpecial && ds.Tables["product"].Rows.Count > 0)
	{
		if(brand != null)
		{
			if(s_cat == null)
			{
//				if(bSayHot)
//					title_desc = " - New Products";
			}
			else
				title_desc = " - " + s_cat;
		}
		else
		{
			if(s_cat == "zzzOthers")
			{
				title = " + cat + ";
				title_desc = " - All Others";
			}
			else
			{
				if(ss_cat == null)
				{
//					if(bSayHot)
//						title_desc = " - New Products";
				}
				else
					title_desc = " - " + ss_cat + "";
			}
		}
	}
      string FirstCat = cat;
	if(m_bSearching)
	{
		title = "<b>Search Result for : " + keyword + "</b>";
		title_desc = "";
	}

	sout = sout.Replace("@@ITEM_LIST_TITLE_DESC", title_desc);
	sout = sout.Replace("@@ITEM_LIST_TITLE", title);
	sout = sout.Replace("@@First_Cat", title);
	

	string form_action = "?" + Request.ServerVariables["QUERY_STRING"];
	form_action = form_action.Replace("&confirm=0", "");
	sout = sout.Replace("@@ITEM_LIST_FORM_ACTION", form_action);

	//function buttons
	string fun_buttons = "";
	if(m_sSite == "admin" && !m_bDiscontinued && bOrder)
	{
		fun_buttons = PrintFunctionButtons();
	}
	sout = sout.Replace("@@ITEM_LIST_FUNCTION_BUTTONS", fun_buttons);

	//start group
	string groups = "";

	Boolean bAlterColor = false;
	DataRow dr = null;
	string sti = "";
	string stiOld = "-1"; //in case there's a blank subTableIndex
	string code = "";
	int i = 0;
	string bgColor = "white";
	
	string sSel = "brand, s_cat, ss_cat, name, code"; 
	 if(m_sort != "brand")
		sSel = m_sort + ", brand, s_cat, ss_cat, name, code"; 
	if(m_bSearching)
		sSel = "";
//********************sort**********************
// alex_sort="level_price0";
// alex_sort="level_price0 desc";
// alex_sort="supplier_code";
// alex_sort="supplier_code desc";
alex_sort=Session[m_sCompanyName + "_asort"].ToString();
	DataRow[] drs = ds.Tables["product"].Select("",alex_sort);




//********************sort**********************
	string sFontColor = "red";
	string slSort = "";
	string sSortName = "";
	string slShowRoom = "";

	sSortName = "brand";
	if(m_sort == "brand")
		sSortName = "price";
	slSort = "?r" + PrintURI() + "&sort=" + sSortName; // + ">Click here to sort by " + sort + "</a>");
	slShowRoom = "sroom.aspx?" + PrintURI();

	if(m_bSearching)
		slShowRoom = "";

	bool bFixLevel6 = MyBooleanParse(GetSiteSettings("level_6_no_qty_discount", "0"));
	bool bAddBookMark = true;
	bool bAddSort = true;
	bool bClearance = false;
	int	nBookMarkCount = 0;
	int nStart = (m_nPage - 1) * m_nPageSize;
	int nEnd = nStart + m_nPageSize;
	i = nStart;
	
	//finals
	StringBuilder sbGroups = new StringBuilder();
	StringBuilder sbRows = new StringBuilder();
	StringBuilder sbItems = new StringBuilder();

	//template
	string sgroup = tp_group;
	string srow = tp_row;
	string sitem = tp_item;
	string sdbrand = "";

	//count
	int items_added = 0; //if greater than m_nItemsPerRow than add the row and reset this counter to zero

	bool bGroupEmpty = true;
	bool bFirstGroup = true;
	
	while(i<rows && i < nEnd)
	{
		//DEBUG("fixed "  , m_bFixedPrices.ToString());
		sti = drs[i][subTableIndex].ToString(); //sti: subTableIndex
		Trim(ref sti);
		
		if(bHasGroup && stiOld.ToLower() != sti.ToLower() && !bShowSpecial && !m_bSearching)
		{
			if(!bGroupEmpty) //add group
			{
				if(items_added > 0) //finish current row if any item already added
				{
					srow = srow.Replace("@@template_rowitem", sbItems.ToString()); //finalize this row
					sbRows.Append(srow); //add row to rows
					srow = tp_row; //reset row template, prepare next row
				
					sbItems.Remove(0, sbItems.Length); //empty items content
					items_added = 0; //reset counter
				}

				sgroup = sgroup.Replace("@@template_itemrow", sbRows.ToString()); //add group
				sgroup = RowParseCommand(sgroup, bFirstGroup); //remove show room link if not first group
				if(bFirstGroup)
				{
					sgroup = sgroup.Replace("@@ITEM_LIST_SHOWROOM_LINK", slShowRoom);
					sgroup = sgroup.Replace("@@ITEM_LIST_SORT_LINK", slSort);
					sgroup = sgroup.Replace("@@ITEM_LIST_SORT_NAME", sSortName);
					sgroup = sgroup.Replace("@@Printable",  Request.ServerVariables["QUERY_STRING"]);
					bFirstGroup = false;
				}

				string bookMark = "";
				if(bAddBookMark)
					bookMark = stiOld + "_a";

				sgroup = sgroup.Replace("@@ITEM_GROUP_TAG", bookMark);
				sgroup = sgroup.Replace("@@ITEM_GROUP_TITLE", stiOld);

				sbGroups.Append(sgroup); //add group
				sbRows.Remove(0, sbRows.Length); //reset rows content, prepare next row
				sgroup = tp_group; //reset group template
			}

			//begin a new group
			bGroupEmpty = false;

			bAlterColor = false;
		}
		
		if(!bAddBookMark)
		{
			nBookMarkCount++;
			if(nBookMarkCount > 10)
			{
				nBookMarkCount = 0;
				bAddBookMark = true;
			}
		}

		stiOld = sti;
		dr = drs[i];
		code = dr["code"].ToString();
		bool bKit = false;
		if(dr["cat"].ToString() == m_sKitTerm) //package
			bKit = true;

		if(m_bDoAction)
		{
			if(Request.Form["sel" + code] == "on")
			{
				if(DoAction(code))
				{
					if(m_action == "Phase Out")
					{
						i++;
						continue; //already phased out, don't display
					}
				}
			}
		}

		if(bAlterColor)
			bgColor ="#D2F0FF";
			//bgColor = "@@color_12";
		else
			bgColor = "@@color_11";
		bAlterColor = !bAlterColor;
		srow = srow.Replace("@@ITEM_ROW_BGCOLOR", bgColor);

		bClearance = MyBooleanParse(dr["clearance"].ToString());
		bool bInactive = MyBooleanParse(dr["inactive"].ToString());

		//first column is blank, show product pic for special page
		string src = GetProductImgSrc(code);
	
		if(m_bKit || bKit)
			src = GetKitImgSrc(code);

		src = src.Replace("na.gif", "0.gif");
		sitem = sitem.Replace("@@ITEM_PIC_LINK", src);
		if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
		{///login is true..  so allow hiding the login menu on body item and also on the menu header and cat header..
			sitem = sitem.Replace("@@HIDE_LOGIN_FRONT", "<!--");
			sitem = sitem.Replace("@@HIDE_LOGIN_END", "-->");
			sitem = sitem.Replace("@@LOGIN_NAME", Session["name"].ToString());
		}
		else
		{
			sitem = sitem.Replace("@@HIDE_LOGIN_FRONT", "");
			sitem = sitem.Replace("@@HIDE_LOGIN_END", "");		
			sitem = sitem.Replace("@@LOGIN_NAME", "Guest");
		}

		if(!m_bItemImageOn)
		{
			sitem = sitem.Replace("@@IMAGE_ONOFF_FRONT", "<!--");
			sitem = sitem.Replace("@@IMAGE_ONOFF_BACK", "-->");
		}
		else
		{
			sitem = sitem.Replace("@@IMAGE_ONOFF_FRONT", "");
			sitem = sitem.Replace("@@IMAGE_ONOFF_BACK", "");
		}
		
		string sdcode = code;
		string m_pn = dr["supplier_code"].ToString();
		string sItemName = dr["name"].ToString();
		sdbrand = dr["brand"].ToString();


		if(m_bKit || bKit)
			sdcode = m_sKitTerm + " " + code;
		if(m_bSearching)
		{
			m_pn = ShowKeywords(m_pn);
			sItemName = ShowKeywords(sItemName);
		}

		sitem = sitem.Replace("@@ITEM_CODE", sdcode);
		sitem = sitem.Replace("@@ITEM_BRAND", sdbrand);
		sitem = sitem.Replace("@@BARCODE", dr["barcode"].ToString());
		sitem = sitem.Replace("@@ITEM_MPN", m_pn);
		
		string exh = dr["stock_location"].ToString();//GetShelfQty(code, "1");
		string sto = GetShelfQty(code, "2");
		
		sitem = sitem.Replace("@@ITEM_EXH_NO", exh);
		sitem = sitem.Replace("@@ITEM_STO_NO", sto);
		

		//recently price dropped
		string sRPD = "";
		sb.Append("<td>");
		if(dr["price_dropped"].ToString() != "0")
		{
			if(CheckPriceDate(code, dr["price_age"].ToString()))
			{
				if(dr["price_dropped"].ToString() == "1")
					sRPD = "<img src=pd.gif title='Recently Price Dropped'>";
				else
					sRPD = "<img src=pu.gif title='Recently Price Raise'>";
			}
		}
		sitem = sitem.Replace("@@RECENT_PRICE_CHANGE_IMG", sRPD);
	
		//Description
		string sItemLink = "p.aspx?" + code;
		if(m_bKit || bKit) //kit
		{
			sItemLink = "pk.aspx?" + code + "&ssid=" + m_ssid;
			if(bInactive)
				sItemName += "<font color=red><b><i>( * Inactive. Edit Only)<i></b></font>";
		}

		if(bClearance)
			sItemName += " <font color=" + sFontColor + ">(*Clearance*) </font>";

		sitem = sitem.Replace("@@ITEM_LINK", sItemLink);
		sitem = sitem.Replace("@@ITEM_NAME", sItemName);

		string highlight = "";
		double dRRP = 0;
		double realRRP=0;
		DataRow drp = null;
		if(GetProduct(code, ref drp))
		{
			highlight = drp["highlight"].ToString();
			if(highlight.Length > 255)
				highlight = highlight.Substring(0, 255);
			dRRP = MyDoubleParse(drp["rrp"].ToString());
			realRRP = MyDoubleParse(drp["level_price0"].ToString());
			if(dRRP == 0)
				dRRP = MyDoubleParse(drp["manual_cost_nzd"].ToString()) * MyDoubleParse(drp["rate"].ToString()) * MyDoubleParse(drp["level_rate1"].ToString()) * 1.1;
		}
		sitem = sitem.Replace("@@ITEM_HIGHLIGHT", highlight);
		sitem = sitem.Replace("@@ITEM_PRICE_RRP", dRRP.ToString("c"));
		sitem = sitem.Replace("@@ITEM_PRICE_realRRP", realRRP.ToString("c"));
		double special_price= Math.Round(MyDoubleParse(dr["special_price"].ToString())/1.15,3);
	 	string d_special_end = dr["special_price_end_date"].ToString();
		 sitem = sitem.Replace("@@ITEM_PRICE_special_price_end_date", d_special_end);
		sitem = sitem.Replace("@@ITEM_PRICE_special_price", special_price.ToString("c"));
        double d_rrp = double.Parse(dr["level_price0"].ToString());
        sitem = sitem.Replace("@@RRP", d_rrp.ToString("c"));
		string showStatus = "";
		if(bInactive)
			showStatus = "<span style=\"color:green\"> Yes </span>";
		else
			showStatus = "<span style=\"color:red\"> No </span>";
		sitem = sitem.Replace("@@status", showStatus);

		string item_supplier = "";
		string item_supplier_price = "";
		{
			//supplier info
			string supplier = dr["supplier"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string foreignCost = dr["foreign_supplier_price"].ToString();
			double dForeignCost = MyDoubleParse(dr["supplier_price"].ToString());
			if(foreignCost != "")
				dForeignCost = MyDoubleParse(foreignCost);

			item_supplier = supplier;
			string cur = GetCurrencyName(dr["currency"].ToString()).ToUpper();
			if(cur.Length > 2)
				cur = cur.Substring(0, 2);
			item_supplier_price = cur + dForeignCost.ToString("c");

			int dls = int.Parse(GetSiteSettings("dealer_levels", "6")); // how many quantity breaks
			if(dls > 9)
				dls = 9;
			int qbs = int.Parse(GetSiteSettings("quantity_breaks", "3")); // how many quantity breaks
			if(qbs > 9)
				qbs = 9;

			double[] lr = new double[9];
			int[] qb = new int[9]; //qty breaks;
			double[] qbd = new double[9];
			double[] dprice = new double[9];

			int j = 0;
			for(j=0; j<dls; j++)
			{
				string jj = (j+1).ToString();
				lr[j] = 2;
				if(dr["level_rate" + jj].ToString() != "")
					lr[j] = double.Parse(dr["level_rate" + jj].ToString());
			}
			for(j=0; j<qbs; j++)
			{
				string jj = (j+1).ToString();
				qb[j] = 1;
				if(dr["qty_break" + jj].ToString() != "")
					qb[j] = int.Parse(dr["qty_break" + jj].ToString());

				qbd[j] = 1;
				if(dr["qty_break_discount" + jj].ToString() != "")
					qbd[j] = double.Parse(dr["qty_break_discount" + jj].ToString());
			}

			double dQtyDiscount = 0;
			double dDiscount = 0;

			string card_id = "";
			int nLevel = 1;
			if(Session["card_id"] != null)
				card_id = Session["card_id"].ToString();
			if(bOrder)
			{
				card_id = "0";
				//use current customer level for POS
				if(Session[m_sCompanyName + "_dealer_card_id" + m_ssid] != null)
					card_id = Session[m_sCompanyName + "_dealer_card_id" + m_ssid].ToString();
				else if(Session[m_sCompanyName + "customerid" + m_ssid] != null)
					card_id = Session[m_sCompanyName + "customerid" + m_ssid].ToString();
			}
			//DEBUG("fixed2 "  , m_bFixedPrices.ToString());
			if(bIsSpecialItemForQPOS(code))
				m_bFixedPrices = true;
			else
			{				
				if(Session[m_sCompanyName + "_card_type_for_pos" + m_ssid] != null)
				{
					string sCardType = Session[m_sCompanyName + "_card_type_for_pos" + m_ssid].ToString();
					if(sCardType == "4" && !bOrder && m_sSite.ToLower() == "admin")
						m_bFixedPrices = true;							
					else if(sCardType == "0" || sCardType == "1")
						m_bFixedPrices = true;				
					else
						m_bFixedPrices = false;
				//DEBUG("fixed3 "  , m_bFixedPrices.ToString());
				}
				else
				{
					if(Session[m_sCompanyName + "loggedin"] != null)
					{
						if(int.Parse(GetSiteSettings("dealer_levels", "1")) > 0 && Session["card_type"].ToString() != "1") //type = dealer then disable fixed price
							m_bFixedPrices = false;	
						else
							m_bFixedPrices = true;
					}
					else
						m_bFixedPrices = true;
				}
			}	
			if(card_id != "" && card_id != "0")
			{
				string sBrand = brand;
				if(sBrand == null || sBrand == "")
					sBrand = dr["brand"].ToString();
				nLevel = GetDealerLevelForCat(card_id, sBrand, cat + " - " + s_cat, m_nDealerLevel);
			}
			//DEBUG("nlevel",lr[nLevel-1]);
			double level_rate = lr[nLevel - 1];
			//double dPrice = MyDoubleParse(dr["level_price"+nLevel].ToString());
            double dPrice = MyDoubleParse(dr["bottom_price"].ToString());
//DEBUG("p1=", dPrice.ToString());
			double dNormalPrice = dPrice * lr[0];
			//if(!bClearance)
				dPrice *= level_rate/100;
			if(m_bFixedPrices)
				nLevel = 1;
			if(m_bSimpleInterface)
				dPrice = MyDoubleParse(dr["price"].ToString()); //this is p.price

			m_dPOSPrice = MyDoubleParse(dr["price1"].ToString());
		//	m_dPOSPrice = m_dPOSPrice / m_dGSTRate;
//DEBUG("fixed "  , m_bFixedPrices.ToString());

            string normal_price_show = "";
            DateTime check_time = DateTime.Now;
            if((m_bFixedPrices && (!m_bKit && !bKit)) || card_id == "0")
			{	
//DEBUG("fixed2 "  , m_bFixedPrices.ToString());
//DEBUG("Now= ", check_time.ToString());			
				if(bIsSpecialItemForQPOS(code))
                {
                    if(dr["special_price_end_date"].ToString() != null && dr["special_price_end_date"].ToString() != "")
                    {
                        DateTime special_end = DateTime.Parse(dr["special_price_end_date"].ToString());
                        if(check_time <= special_end)
                        {
                            dPrice = MyDoubleParse(dr["special_price"].ToString());
                            normal_price_show = "<p id='item_p'><s>";
                            normal_price_show += MyDoubleParse(dr["price" + nLevel].ToString()).ToString("c");
                            normal_price_show += "<span>&nbsp;was price</span></s></p>";
                        }
                        else if(m_sSite.ToLower() != "www" && check_time > special_end)
                            dPrice = MyDoubleParse(dr["level_price"+nLevel].ToString());
                        else
                            dPrice = MyDoubleParse(dr["price" + nLevel].ToString());
                    }
                    else 
                    {
                        if(m_sSite.ToLower() != "www")
                            dPrice = MyDoubleParse(dr["level_price"+nLevel].ToString());
                        else
                            dPrice = MyDoubleParse(dr["price" + nLevel].ToString());
                    }
                }
                else
                    dPrice = MyDoubleParse(dr["price" + nLevel].ToString());
//DEBUG("sp=", dr["price" + nLevel].ToString());
                if(dPrice != 0) 
					dPrice = dPrice / m_dGSTRate;
				//else
					//dPrice = MyDoubleParse(dr["level_price"+nLevel].ToString());
			}  //*************************************************** Price display by Sean
/*
			if(m_bUseLastSalesFixedPrice && (!m_bKit && !bKit))
			{	double dTmpPrice = GetLastSalesFixedPriceForDealer(code, "1", card_id, nLevel.ToString());
				if(dTmpPrice > 0)
					dPrice = dTmpPrice;
			}
		*/
		//	if(card_name.IndexOf("cash sales") >= 0)
		//	{
		//	}
			//DEBUG("level ",nLevel + " price  " + dPrice.ToString());	
			double dPriceOnePiece = dPrice; //price without qty discount

			//calculate price and discount on qty
			string qty = "0";
			if(Request.Form["qty" + i] != null)
				qty = Request.Form["qty" + i];
			int dqty = MyIntParse(qty);
			dDiscount = 0;
			string add_qty = g("qty");
			string add_code = g("code");
			if(add_code == code)
			{
				dqty = MyIntParse(add_qty);
				qty = add_qty;
			}
	
			if(dqty != 0)
			{
				if(dqty < 0 && !bAdmin)
				{
					//negative qty is not allowed to public user!
					Response.Write("<script Language=javascript");
					Response.Write(">");
					Response.Write("window.alert ('Error. Quantity cannot be negative !')");
					Response.Write("</script");
					Response.Write(">");
					qty = "0";
				}
				else
				{
					//get qty discount
					dQtyDiscount = GetQtyDiscount(dqty, qb, qbd);
					if(bFixLevel6 && m_nDealerLevel >= 6)
						dQtyDiscount = 0;
					if(!bClearance)
					{
						dPrice *= (1 - dQtyDiscount);
						dDiscount = dQtyDiscount;
					}
					if(m_bAddToCart)
					{
						if(m_bKit || bKit)
						{
							if(bAdmin)
							{
								DoAddKit(code, dqty);
								dPrice = dPriceOnePiece;
								dDiscount = 0;
								dqty = 0;
								qty = "0";
							}
							else
							{
								DoAddKit(code, dqty);
								dPrice = dPriceOnePiece;
								dDiscount = 0;
								dqty = 0;
								qty = "0";		
								if(Session["c_editing_order" + m_ssid] != null)
								{
									Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=corder.aspx?ssid=" + m_ssid + "\">");
									return;
								}
							}
						}
						else
						{
							if(bAdmin)
							{								
								//admin edit
								if(m_sBuyType == "purchase")
									AddToCart(code, supplier, supplier_code, qty, dForeignCost.ToString());
								else
									AddToCart(code, qty, dPrice.ToString());
								dPrice = dPriceOnePiece;
								dDiscount = 0;
								dqty = 0;
								qty = "0";
							}
							else
							{
								AddToCart(code, qty, dPrice.ToString());
								dPrice = dPriceOnePiece;
								dDiscount = 0;
								dqty = 0;
								qty = "0";		
								if(Session["c_editing_order" + m_ssid] != null)
								{
									Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=corder.aspx?ssid=" + m_ssid + "\">");
									return;
								}
							}
						}
					}
				}
			}

			dDiscount = Math.Round((dNormalPrice - dPrice)/dNormalPrice, 4);

			sitem = sitem.Replace("@@ITEM_SUPPLIER", item_supplier);
			sitem = sitem.Replace("@@ITEM_COST", item_supplier_price);

			string sstock = dr["stock"].ToString();
			string sstock_qty = dr["stock"].ToString();
			string moq = dr["moq"].ToString();
			string inner = dr["inner_pack"].ToString();
			string outer = dr["weight"].ToString();

			if(m_bStockSayYesNo && m_sSite.ToLower() != "admin" )
			{
				if(MyIntParse(sstock) == 0)
					sstock = m_sStockNoString;
				else
					sstock = m_sStockYesString;
			}
			string seta = dr["eta"].ToString();
			string sDiscountInfo = "";
			string sprice = "";

			if(m_bDiscontinued)
			{
				sstock = "&nbsp;";
				seta = "&nbsp;";
				sDiscountInfo = "&nbsp;";
			}
			if(!m_bKit && !bKit)
				sstock = GetAllBranchStock(code);
			
			/*if(!bCheck_qty)	
				sstock = "<span style=\"color:red;\">X</span>";
			else
				sstock = "<span style=\"color:green;\">&radic;</span>";*/

			//Stock
			sitem = sitem.Replace("@@ITEM_STOCK_QTY", sstock_qty);	//qty
			sitem = sitem.Replace("@@ITEM_STOCK", sstock);
			if(m_sSite != "admin" && bInactive )
				sitem = sitem.Replace("@@disable", "disabled");
			sitem = sitem.Replace("@@ITEM_MOQ", moq);
			sitem = sitem.Replace("@@ITEM_INNER", inner);
			sitem = sitem.Replace("@@ITEM_OUTER", outer);

			//ETA
			sitem = sitem.Replace("@@ITEM_ETA", seta);

			//discount
			sDiscountInfo += dDiscount.ToString("p");
			if(qbs > 0)
			{
				sDiscountInfo += " (";
				for(j=0; j<qbs; j++)
				{
					if(j != 0)
						sDiscountInfo += ",";
					sDiscountInfo += qb[j]; 
				}
				sDiscountInfo += ") ";
			}
			if(m_bDiscontinued)
			{
				sDiscountInfo = "&nbsp;";
				qty = "0";
			}

			sitem = sitem.Replace("@@ITEM_DISCOUNT", sDiscountInfo);

			//qty
			sitem = sitem.Replace("@@ITEM_FIELD_NAME_MOQ", "moq" + i.ToString());
			
			sitem = sitem.Replace("@@ITEM_FIELD_NAME_QTY", "qty" + i.ToString());
			sitem = sitem.Replace("@@ITEM_FIELD_VALUE_QTY", qty);
			sitem = sitem.Replace("@@ITEM_ROW", i.ToString());
	

			sb.Append("<td align=right>");
			if(dqty > 1 && !bClearance)
				sprice = "<font color=" + sFontColor + "><b>";
			sprice += dPrice.ToString("c");
			if(dqty > 1 && !bClearance)
				sprice += "</b></font>";
			//if(m_bNoPrice || m_bDiscontinued)
			//	sprice = "&nbsp;";
		
		//if(m_sSite == "www")
		//	 sprice = (Math.Round(dPrice * m_dGSTRate, 3)).ToString("c");

		   if(m_sSite == "www")
			sprice = (Math.Round(dPrice*1.15, 3)).ToString("c");
//DEBUG("p2=", dPrice.ToString());

		//DEBUG("pos ", m_dPOSPrice.ToString("c"));
			sitem = sitem.Replace("@@ITEM_POS_PRICE", m_dPOSPrice.ToString("c"));
	        sitem = sitem.Replace("@@normal_price", normal_price_show);
			sitem = sitem.Replace("@@ITEM_PRICE", sprice);

            
		
			sitem = sitem.Replace("@@ITEMPRICEWITHGST", (Math.Round(dPrice * m_dGSTRate, 3)).ToString("c"));	
				

			string slEdit = "";
			string slActionSel = "";
			string slBuyButton = "";

			if(bAdmin && !bOrder)
			{
				//admin edit
				if(m_bKit || bKit) //kit
					slEdit += "kit.aspx?id=";
				else
					slEdit += "liveedit.aspx?code=";
				slEdit += code;
				slActionSel += "<input type=checkbox name=sel" + code + ">";
			}
			else
			{
				slEdit = "\"javascript:alert_window=window.alert('Product Edit Not Allow While in Sale Mode');window.close();\"";
			}
			if(m_sSite.ToLower() != "admin")
			{
				if(m_bKit || bKit)
					slBuyButton = "<a title='view to buy this kit' href=pk.aspx?"+ code +"&ssid=" + m_ssid + " class=o><img title='view package details to purchase' border=0 src='i/view.gif'></a>";
				else
					slBuyButton = "<a href=cart.aspx?t=b&c="+ code +"><img title='buy this item' border=0 src='b.gif'></a>";
			}
			sitem = sitem.Replace("@@PUBLIC_BUY_LINK", slBuyButton);

			sitem = sitem.Replace("@@ITEM_EDIT_LINK", slEdit);
			sitem = sitem.Replace("@@ACTION_SELECT", slActionSel);
		}
		sb.Append("</tr>\r\n");

		//one item added
		items_added++;
		sbItems.Append(sitem); //add to row
		sitem = tp_item; //reset item template, prepare next item
		if(items_added >= m_nItemsPerRow) //row ready
		{
			srow = srow.Replace("@@template_rowitem", sbItems.ToString()); //finalize this row
			sbRows.Append(srow); //add row to rows
			srow = tp_row; //reset row template, prepare next row
			
			sbItems.Remove(0, sbItems.Length); //empty items content
			items_added = 0; //reset counter
		}

		i++;
		sitem = tp_item; //new item
		//end subTable
	}

	if(items_added > 0) //item left behind
	{
		srow = srow.Replace("@@template_rowitem", sbItems.ToString()); //finalize this row
		sbRows.Append(srow); //add row to rows
	}

	if(bHasGroup && sbRows.Length > 0) //row behinde
	{
		sgroup = sgroup.Replace("@@template_itemrow", sbRows.ToString()); //add group
		sgroup = RowParseCommand(sgroup, bFirstGroup); //remove show room link if not first group
		if(bFirstGroup)
		{
			sgroup = sgroup.Replace("@@ITEM_LIST_SHOWROOM_LINK", slShowRoom);
			sgroup = sgroup.Replace("@@ITEM_LIST_SORT_LINK", slSort);
			sgroup = sgroup.Replace("@@ITEM_LIST_SORT_NAME", sSortName);
			sgroup = sgroup.Replace("@@BRAND", sdbrand);
			sgroup = sgroup.Replace("@@Printable",  Request.ServerVariables["QUERY_STRING"]);
			bFirstGroup = false;
		}
                
		string bookMark = sti + "_a";

		if(bAddBookMark)
			bookMark = sti + "_a";

		sgroup = sgroup.Replace("@@ITEM_GROUP_TAG", bookMark);
		sgroup = sgroup.Replace("@@ITEM_GROUP_TITLE", sti);

		sbGroups.Append(sgroup); //add group
	}

	if(bHasGroup)
		sout = sout.Replace("@@template_itemgroup", sbGroups.ToString());
	else
		sout = sout.Replace("@@template_itemrow", sbRows.ToString());
	
	sout = sout.Replace("@@BRAND", sdbrand);
	sout = sout.Replace("@@ITEM_LIST_SHOWROOM_LINK", slShowRoom);
	sout = sout.Replace("@@ITEM_LIST_SORT_LINK", slSort);
	sout = sout.Replace("@@ITEM_LIST_SORT_NAME", sSortName);
	sout = sout.Replace("@@Printable",  Request.ServerVariables["QUERY_STRING"]);
	sout = sout.Replace("@@ssid_value", m_ssid);
	sout = sout.Replace("@@ssid", "&ssid="+Request.QueryString["ssid"]);

	string[] banner = GetBannerImgSrc();
	string carousel = "";
	for(int b=0; b < banner.Length; b++ ){
		carousel += "<img src=\"" + banner[b] + "\"/>";
	//DEBUG("banner=",banner[b]);
	}
	sout = sout.Replace("@@CAROUSEL", carousel);
//	if(m_bSearching)
//		sout = sout.Replace("Show Room", "");	sout = ApplyColor(sout);
	Response.Write(sout);
	return;

	TSAddCache(scn, sb.ToString());
	Response.Write(sb.ToString());
}

string WriteURLWithoutPageNumber()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("?");
	if(brand != null)
		sb.Append("b=" + ebrand + "&");
	if(cat != null)
		sb.Append("c="+ ecat);
	if(s_cat != null)
		sb.Append("&s=" + es_cat);
	if(ss_cat != null)
		sb.Append("&ss=" + ess_cat);
	if(m_ssid != "")
		sb.Append("&ssid=" + m_ssid);
	return sb.ToString();
}

string AppendParameter(string brand, string s_cat, string ss_cat, 
					   string ebrand, string ecat, string es_cat, string ess_cat, string text)
{
	StringBuilder sb = new StringBuilder();
	if(brand != null)
	{
		sb.Append("b=");
		sb.Append(ebrand);
	}
	else
	{
		sb.Append("c=");
		sb.Append(ecat);
	}
	if(s_cat != null)
	{
		sb.Append("&s=");
		sb.Append(es_cat);
	}
	if(ss_cat != null)
	{
		sb.Append("&ss=");
		sb.Append(ess_cat);
	}
	sb.Append("&r=" + DateTime.Now.ToOADate());
	sb.Append(" target=_blank>");
	sb.Append(text);
	sb.Append("</a>");
	return sb.ToString();
}

bool CheckPriceDate(string code, string age)
{
	DateTime dAge = DateTime.Parse(age);
	if( (DateTime.Now - dAge).Days > 7)
		return false;
	return true;
}

string ShowLogo()
{
	string sc = " SELECT * FROM cat_logo ";
	sc += " WHERE s_cat = '" + EncodeQuote(s_cat) + "' ";
	sc += " ORDER by seq ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "logo") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	StringBuilder sb = new StringBuilder();

	int columns = MyIntParse(GetSiteSettings("sub_cat_image_table_columns", "3"));
	if(columns > 30)
		columns = 30; //for protection

	int n = 0;
	sb.Append("<table cellspacing=10 cellpadding=10 border=0>");
	for(int i=0; i<ds.Tables["logo"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["logo"].Rows[i];
		string pic_name = dr["pic_name"].ToString();
		string uri = dr["uri"].ToString();
		int nColspan = MyIntParse(dr["colspan"].ToString());
		string title = dr["title"].ToString();
		if(n == 0)
		{
			if(i > 0)
				sb.Append("</tr>");
			sb.Append("<tr>");
		}

		if(nColspan > columns - n) //no enough columns
		{
			for(int m=columns-n; m>0; m--)
				sb.Append("<td>&nbsp;</td>");
			sb.Append("</tr><tr>");
			n = 0;
		}

		n++;
		if(n >= columns)
			n = 0;

		sb.Append("<td valign=bottom");
		if(nColspan > 1)
		{
			sb.Append(" colspan=" + nColspan);
			n += nColspan - 1;
			if(n >= columns)
				n = 0;
		}
		sb.Append(">");

		sb.Append("<table cellspacing=0 cellpadding=0 border=0 ");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		sb.Append("<tr><td>");
//	DEBUG("uri = ", uri);
		if(uri != "")
		{
			sb.Append("<a href=" + uri + "");
			if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
				sb.Append("&ssid="+ Request.QueryString["ssid"]+"");
			sb.Append(">");
		}
		sb.Append("<img src='/i/" + pic_name + "' border=0>");
		if(uri != "")
			sb.Append("</a>");
		sb.Append("</td></tr>");
		sb.Append("<tr><td><b>");
		sb.Append(title);
		sb.Append("</b></td></tr></table>");

		sb.Append("</td>");
	}
	sb.Append("</tr>");
	sb.Append("</table>");
	return sb.ToString();
}

bool DoAction(string code)
{
	if(m_action == "Phase Out")
	{
		//add to to product_skip table
		string sc = " IF EXISTS (SELECT code FROM product WHERE code=" + code + ") ";
		sc += " BEGIN ";
		sc += " INSERT INTO product_skip ";
		sc += " SELECT c.id, p.stock, p.eta, c.supplier_price, p.price, '' AS details ";
		sc += " FROM product p join code_relations c on c.code=p.code where c.code = " + code;
		sc += " UPDATE code_relations SET skip=1 WHERE code=" + code;
		sc += " DELETE FROM product WHERE code = " + code;
		sc += " END ";
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
	}
	return true;
}

string PrintFunctionButtons()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<input type=button " + Session["button_style"] + " value='View ");
	if(m_sBuyType == "purchase")
		sb.Append("Purchase Order");
		
	else if(m_sBuyType == "quote")
		sb.Append("Quote");
	else
		sb.Append(m_sBuyType);
	sb.Append("' onclick=window.location=('");
	//if(quick == "1")
		//sb.Append("qpurchase.aspx");
	if(m_sBuyType == "purchase")
		sb.Append("purchase.aspx");
	else if(m_sBuyType == "quote")
		sb.Append("q.aspx");
	else if(m_sBuyType == "sales")
		sb.Append("pos.aspx");
	else if(m_sBuyType == "quick_sales")
		sb.Append("pos_retail.aspx");
	else
		sb.Append(Session[m_sCompanyName + "_salesurl"]);
	if(m_ssid != "" && sb.ToString().IndexOf("ssid=") < 0)
		sb.Append("?ssid=" + m_ssid);
	sb.Append("') >");
	return sb.ToString();
}

bool GetKit()
{
	string sc = " SELECT id AS barcode, getdate() AS price_age, '' AS supplier, '' AS supplier_code, 0 AS supplier_price ";
	sc += ", id AS code, '' AS brand, 1 AS stock, '" + m_sKitTerm + "' AS cat, '' AS eta ";
	sc += ", 0 AS price_dropped, 0 AS clearance, 1 AS currency, 0 AS foreign_supplier_price ";
	for(int j=1; j<=9; j++)
		sc += ", 1 AS level_rate" + j + ", 1 AS qty_break" + j + ", 0 AS qty_break_discount" + j + ", 0 AS price" + j;
	sc += ", price AS bottom_price ";
	sc += ", * FROM kit ";
	sc += " WHERE 1=1 ";
	if(m_sSite != "admin")
		sc += " AND inactive=0 ";
	if(ds_cat != null && ds_cat != "")
		sc += " AND s_cat = '" + EncodeQuote(ds_cat) + "' ";
	if(dss_cat != null && dss_cat != "")
		sc += " AND ss_cat = '" + EncodeQuote(dss_cat) + "' ";
//	if(Request.QueryString["ss"] == null || Request.QueryString["ss"] == "")
//		sc += " AND hot = 1 ";	
	sc += " ORDER BY s_cat, ss_cat, name, id";
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows_return = myCommand.Fill(ds, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetSpecialKits()
{
	string sc = " SELECT  id AS barcode, '01/01/2001' AS price_age, '' AS supplier, '' AS supplier_code, 0 AS supplier_price ";
	sc += ", id AS code, '' AS brand, 1 AS stock, '" + m_sKitTerm + "' AS cat, '' AS eta ";
	sc += ", 0 AS price_dropped, 0 AS clearance, 1 AS currency, 0 AS foreign_supplier_price ";
	for(int j=1; j<=9; j++)
		sc += ", 1 AS level_rate" + j + ", 1 AS qty_break" + j + ", 0 AS qty_break_discount" + j + ", 0 AS price" + j;
	sc += ", k.price AS bottom_price ";
	sc += ", * FROM kit k ";
	sc += " JOIN specials_kit s ON k.id = s.code "; 
	sc += " WHERE 1=1 ";
	if(m_sSite != "admin")
		sc += " AND k.inactive=0 ";
	sc += " ORDER BY k.s_cat, k.ss_cat, k.name, k.id";
//DEBUG("sc sp kit =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows_return = myCommand.Fill(ds, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string TemplateParseCommand(string tp)
{
	StringBuilder sb = new StringBuilder();

	int line = 0;
	string sline = "";
	bool bRead = ReadLine(tp, line, ref sline);
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf("IF_SPECIAL") >= 0)
		{
			sline = sline.Replace("IF_SPECIAL", "");
			if(bShowSpecial)
			{
				if(sline.IndexOf("@@DEFINE") >= 0)
				{
					string snItems = GetDefineValue("ITEMS_PER_ROW", sline);
					if(snItems != "")
						m_nItemsPerRow = MyIntParse(snItems);
				}
				else
					sb.Append(sline);
			}
		}
		else if(sline.IndexOf("IF_NOT_SPECIAL") >= 0)
		{
			sline = sline.Replace("IF_NOT_SPECIAL", "");
			if(!bShowSpecial && !m_bSearching)
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_NOT_DISCONTINUED") >= 0)
		{
			sline = sline.Replace("IF_NOT_DISCONTINUED", "");
			if(!m_bDiscontinued)
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_PURCHASE") >= 0)
		{
			sline = sline.Replace("IF_PURCHASE", "");
			if(m_sBuyType == "purchase")
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_NOT_PURCHASE") >= 0)
		{
			sline = sline.Replace("IF_NOT_PURCHASE", "");
			if(m_sBuyType != "purchase")
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_ADMIN") >= 0)
		{
			sline = sline.Replace("IF_ADMIN", "");
			if(m_bAdmin)
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_NOT_ADMIN") >= 0)
		{
			sline = sline.Replace("IF_NOT_ADMIN", "");
			if(!m_bAdmin)
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_PAGING") >= 0)
		{
			sline = sline.Replace("IF_PAGING", "");
			if(m_bPaging)
				sb.Append(sline);
		}
		else if(sline.IndexOf("IF_LOGGEDIN") >= 0)
		{
			sline = sline.Replace("IF_LOGGEDIN", "");
			if(TS_UserLoggedIn())
				sb.Append(sline);
		}
		else if(sline.IndexOf("@@DEFINE") >= 0)
		{
			string snItems = GetDefineValue("ITEMS_PER_ROW", sline);
			if(snItems != "")
				m_nItemsPerRow = MyIntParse(snItems);
		}
		else if(sline.IndexOf("IF_SEARCHING") >= 0)
		{
			sline = sline.Replace("IF_SEARCHING", "");
			if(m_bSearching)
				sb.Append(sline);
		}
		else
		{
			sb.Append(sline);
		}
		line++;
		bRead = ReadLine(tp, line, ref sline);
	}
	return sb.ToString();
}

string GetDefineValue(string sDef, string sline)
{
	int p = sline.IndexOf(sDef);
	string sValue = "";
	if(p > 0)
	{
		p += sDef.Length + 1;
		for(; p<sline.Length; p++)
		{
			if(sline[p] == ' ' || sline[p] == '\r' || sline[p] == '\n')
				break;
			sValue += sline[p];
		}
	}
	return sValue;
}

string RowParseCommand(string s, bool bFirstGroup)
{
	StringBuilder sb = new StringBuilder();

	int line = 0;
	string sline = "";
	bool bRead = ReadLine(s, line, ref sline);
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf("IF_FIRST_GROUP") >= 0)
		{
			sline = sline.Replace("IF_FIRST_GROUP", "");
			if(bFirstGroup)
				sb.Append(sline);
		}
		else
		{
			sb.Append(sline);
		}
		line++;
		bRead = ReadLine(s, line, ref sline);
	}
	return sb.ToString();
}

//for search options
string ShowKeywords(string sIn)
{
	string s = sIn;
	for(int i=0; i<words; i++)
	{
		s = showkw(s, kws[i]);
	}
	return s;
}

string showkw(string sIn, string kw)
{
	if(kw.Length <= 0)
	{
		return sIn;
	}

	string s = "";
	string slow = sIn.ToLower();
	kw = kw.ToLower();
	int p = slow.IndexOf(kw);
//DEBUG("keyword="+keyword, " p=" + p.ToString());
	int start = 0;

	while(p >= 0)
	{
		s += sIn.Substring(start, p - start) + "<b>";
		s += sIn.Substring(p, kw.Length);
		s += "</b>";
		start = p + kw.Length;
//		int pp = p;
		p = sIn.IndexOf(kw, p + kw.Length);
//		if(p
	}
	if(start < sIn.Length)
	{
		s += sIn.Substring(start, sIn.Length - start);
	}
	if(s == "")
		return sIn;

	return s;
}

string GetAllBranchStock(string code)
{
	if(ds.Tables["stocks"] != null)
		ds.Tables["stocks"].Clear();

	bool bManyBranch = false;
	if(m_branches > 1)
		bManyBranch = true;

	string sc = " ";
	if(bManyBranch) //get many branch stock
	{
		sc += " SELECT s.branch_id, isnull(s.qty, 0) AS qty FROM stock_qty s ";
		sc += " JOIN branch b ON b.id = s.branch_id AND b.activated = 1 ";
		sc += " WHERE s.code = " + code; // + " AND branch_id IN (" + sbranch + ") ";
		sc += " ORDER BY branch_id ";
	}
	else //get summary stock from all branch
	{
		sc += " SELECT 1 AS branch_id, ISNULL((SELECT SUM(s.qty) FROM stock_qty s WHERE s.code = "+ code +" AND s.branch_id=1)";
		sc += " ,0) as qty ";
		sc += " FROM branch b WHERE b.id = 1 AND b.activated = 1 ";
	}

//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "stocks") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	if(ds.Tables["branchName"] == null)
	{
		sc = " SELECT id, name FROM branch b order by id "; 	
//	DEBUG("sc=", sc);
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(ds, "branchName");			
		}
		catch(Exception e) 
		{			
		}
	}
	string BranchName = "";
	string s = "";	

	if(bManyBranch)
	{
	    s = "<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#AAAAAA bgcolor=white ";
	    s += " style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">";
	    BranchName += s + "<tr>";
	
	    for(int i=0; i<ds.Tables["branchName"].Rows.Count; i++)
	    {
		    for(int m=0; m<m_branches; m++)
		    {
			    int mBranch = MyIntParse(m_aBranch[m]);
			    if(mBranch == int.Parse(ds.Tables["branchName"].Rows[i]["id"].ToString()))
			    {
				    BranchName += "<td align=center>"+ ds.Tables["branchName"].Rows[i]["name"].ToString() +"";		
				    BranchName += "</td>";
				    break;
			    }
		    }		
	    }   
	
	    BranchName += "</tr>";
	}
	string[] aStock = new string[16];
	for(int i=0; i<16; i++)
		aStock[i] = "0";

	for(int i=0; i<ds.Tables["stocks"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["stocks"].Rows[i];
		int nBranch = MyIntParse(dr["branch_id"].ToString());
		string qty = dr["qty"].ToString();

		for(int m=0; m<m_branches; m++)
		{
			int mBranch = MyIntParse(m_aBranch[m]);
			if(mBranch == nBranch)
			{
				aStock[m] = qty;
				break;
			}
		}
	}
	
	s = BranchName;
	if(bManyBranch)
		s += "<tr align=center>";
	String [] aStockString = new String [m_branches];
	for(int i=0; i<m_branches; i++)
	{
		if(m_bStockSayYesNo && m_sSite.ToLower() != "admin" )
		{
			if(double.Parse(aStock[i]) > 0)
				aStockString[i] = m_sStockYesString;
			else
				aStockString[i] = m_sStockNoString;			
		}
		else
			aStockString[i] = aStock[i];
        if (double.Parse(aStock[i]) >= 20)
            return  "<span style=\"color:green;\">&radic;</span>";
        else
            return "<span style=\"color:red;\">X</span>";
		if(bManyBranch)
			s += "<td bgcolor=#EEEEEE>";
		s += aStockString[i];
		if(bManyBranch)
			s += "</td>";
	}
	if(bManyBranch)
		s += "</tr></table>";
	return s;
}
string GetShelfQty(string code, string location_type)
{
	int nRows = 0;
	if(ds.Tables["shelf"] != null)
		ds.Tables["shelf"].Clear();
	string sc = " SELECT s.name, si.qty ";
	sc += " FROM shelf s ";
	sc += " JOIN shelf_item si ON si.shelf_id = s.id ";
	sc += " WHERE si.code = " + code;
	if(location_type == "1")
		sc += " AND s.area LIKE 'E%' ";
	else 
		sc += " AND s.area LIKE 'S%' AND si.qty > 0 ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(ds, "shelf");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["shelf"].Rows[i];
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		s += qty + "@" + name + " ";
	}
	return s;
}
string[] GetBannerImgSrc()
{
	string path = "pic/banner/images/";
	if(m_sSite != "www")
		path = "../" + path;
	int i = 0;
	int count = 0;
	string [] s = {};
	if(Directory.Exists(Server.MapPath(path)))
	{
		DirectoryInfo di = new DirectoryInfo(Server.MapPath(path));
		foreach(FileInfo f in di.GetFiles("*.*"))
		{
			if(f.Name != "")
				count++;
		}
		s = new string[count];
		foreach(FileInfo f in di.GetFiles("*.*"))
		{
			s[i] = path + f.Name;
			i++;
		}
	}
	return s;
}
bool AjaxShoppingCart()
{
	double dRowTotal = 0;
	double dOrderTotal = 0;
	double dQtyTotal = 0;
	string s = "";

	DataTable dtCartNew = dtCart.Copy();
	DataRow[] dra = dtCartNew.Select("");
	dtCart.Rows.Clear();
	for(int i=0; i<dra.Length; i++)
	{
		DataRow dr = dra[i];
		string dsite = dr["site"].ToString();
		string dkid = dr["kid"].ToString();
		string dcode = dr["code"].ToString();
		string dname = dr["name"].ToString();
		double dqty = MyMoneyParse(dr["quantity"].ToString());
		string dsystem = dr["system"].ToString();
		string dkit = dr["kit"].ToString();
		string dused = dr["used"].ToString();
		string dsupplierPrice = dr["supplierPrice"].ToString();
		double dprice = Math.Round(MyMoneyParse(dr["salesPrice"].ToString()), 2);		
		string dsupplier = dr["supplier"].ToString();
		string dsupplier_code = dr["supplier_code"].ToString();
		string ds_serialNo = dr["s_serialNo"].ToString();
		string dbarcode = dr["barcode"].ToString();
		string dpoints = dr["points"].ToString();
		string ddiscount_percent = dr["discount_percent"].ToString();
		string dpack = dr["pack"].ToString();
		string dnote = dr["note"].ToString();

		if(s != "")
			s += ",\r\n";
		s += "{\"name\" : \"" + dname.ToString() + "\",";
		s += "\"qty\" : " + dqty + ",";
		s += "\"price\" : \"" + dprice.ToString("c") + "\"}";	 

		dRowTotal = dprice * dqty * 1;
		dOrderTotal += dRowTotal;
		dQtyTotal += dqty;

		dr = dtCart.NewRow();
		dr["site"] = dsite;
		dr["kid"] = dkid;
		dr["code"] = dcode;
		dr["name"] = dname;
		dr["quantity"] = dqty;
		dr["system"] = dsystem;
		dr["kit"] = dkit;
		dr["used"] = dused;
		dr["supplierPrice"] = dsupplierPrice;
		dr["salesPrice"] = dprice;	
		dr["supplier"] = dsupplier;
		dr["supplier_code"] = dsupplier_code;
		dr["s_serialNo"] = ds_serialNo;
		dr["barcode"] = dbarcode;
		dr["points"] = dpoints;
		dr["discount_percent"] = ddiscount_percent;
		dr["pack"] = dpack;
		dr["note"] = dnote;
		dtCart.Rows.Add(dr);
		//DEBUG("dPrice=",dPrice);
		//DEBUG("dQty=",dQty.ToString());
		//DEBUG("dQtyTotal=",dRowTotal.ToString());
	}
	s = "{\"dataList\" : [" + s + "],";
	s += "\"dQtyTotal\" : " + dQtyTotal + ",";
	s += "\"dOrderTotal\" : \"" + dOrderTotal.ToString("c") + "\",";
	s += "\"dQtyTotalTax\" : \"" + (dOrderTotal*0.15).ToString("c") + "\",";
	s += "\"dQtyTotalAmount\" : \"" + (dOrderTotal*1.15).ToString("c") + "\",";
	s += "\"timeStamp\" : \"" + DateTime.Now.ToOADate().ToString() + "\"}";	 
	Response.Write(s);
	return true;
}
</script>


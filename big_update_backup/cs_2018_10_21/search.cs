<!-- #include file="c.cs" -->
<script runat=server>

int returns = 0; //search result in total;
int m_nStartPageButton = 1;
int m_nPageButtonCount = 9;
int m_cols = 6;
bool m_bShowCat = true;
bool bAdmin = false;
bool bSales = false;

/*
DataSet dss = new DataSet();
int m_nPage = 1;
int m_nPageSize = 20;

bool bIncludeGST = false;
bool bOrder = false;

bool m_bClearance = false;
bool m_bDiscontinued = false;
bool m_bAddToCart = false;

string m_sBuyType = "sales";

int m_nDealerLevel = 1;
*/
	
void GetKeyWord()
{
	keyword = Request.QueryString["kw"];
	if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
		m_nPage = MyIntParse(g("p"));
	if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
		m_nStartPageButton = MyIntParse(g("spb"));

	if(Session["display_include_gst"] != null && Session["display_include_gst"].ToString() == "true")
		bIncludeGST = true;

	m_dGSTRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "15")) / 100;				// 30.JUN.2003 XW
	if(m_dGSTRate < 1)
		m_dGSTRate = 1+m_dGSTRate;

	if(keyword == null || keyword == "")
	{
//		PrintHeaderAndMenu();
		Response.Write("<br><br><center><h3>Nothing to search</h3>");
//		PrintFooter();
		Response.End();
		return;
	}

	Trim(ref keyword);
	Session["search_keyword"] = keyword;
	keyword = EncodeQuote(keyword); //prevent sql attack
}

void Search_Page_Load()
{
	if(MyBooleanParse(GetSiteSettings("Enable_Use_Last_Sales_Fixed_Price", "0", true)))
		m_bUseLastSalesFixedPrice = true;
	
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

	if(keyword == null)
	{
		Response.Write(" <h5>Nothing to search, parameter kw missing</h5>");
		return;
	}

	RememberLastPage();

	if(TS_UserLoggedIn())
	{
		if(Session[m_sCompanyName + "dealer_level"] != null)
			m_nDealerLevel = MyIntParse(Session[m_sCompanyName + "dealer_level"].ToString());
	}

	//sales session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		m_ssid = Request.QueryString["ssid"];

	if(Session[m_sCompanyName + "_ordering"] != null)
		bOrder = true;
	if(Session[m_sCompanyName + "_salestype"] != null)
		m_sBuyType = Session[m_sCompanyName + "_salestype"].ToString();

	if(bOrder)
	{
		//use current customer level for POS
		if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] != null && Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] != "")
			m_nDealerLevel = MyIntParse(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString());
	}

	string cmd = "";
	if(Request.Form["cmd"] != null)
		cmd = Request.Form["cmd"];

	if(cmd == "Update Price")
	{
	}
	else if(cmd == "Add To Cart")
	{
		m_bAddToCart = true;
	}
	m_action = g("a");
	if(m_action == "buy")
	{
		m_bAddToCart = true;
	}
	char[] cb = keyword.ToCharArray();
	for(int i=0; i<cb.Length; i++)
	{
		if(cb[i] == '\'')
			continue;
		else
			kw += cb[i];
	}
	
	string word = "";
	bool bUnwanted = false;
	kw = kw.ToLower();
	cb = kw.ToCharArray();	
	kw = " LOWER(p.name) LIKE '%";
	bool bQuote = false;
	for(int i=0; i<cb.Length; i++)
	{
//DEBUG("here, i=", i);
		if(cb[i] == '\'')
			continue;
		if(bQuote)
		{
			if(cb[i] == '"')
			{
				bQuote = false;
				if(bUnwanted) //the end of an unwanted keyword
				{
					ukws[uwords] = word;
					uwords++;// end of space, comma, plus and miners
				}
				else
				{
					kws[words] = word;
					words++;
				}
				word = ""; //reset all
				bUnwanted = false;
				continue;
			}
			else
			{
				kw += cb[i];
				word += cb[i]; //build up word list
			}
		}
		else
		{
			if(cb[i] == '"')
			{
				bQuote = true;
				continue;
			}
			if(cb[i] == ' ' || cb[i] == ',')
			{
//DEBUG("i=", i+ " cb="+cb.Length.ToString());
				if(i < cb.Length-1)
				{
					if(cb[i+1] == ' ' || cb[i+1] == ',' || cb[i+1] == '+' || cb[i+1] == '-')
					{
						if(bUnwanted)
						{
							ukws[uwords] = word;
							uwords++;// end of space, comma, plus and miners
						}
						else
						{
//DEBUG("word1=", word);
							kws[words] = word;
							words++;// end of space, comma, plus and miners
						}
						word = "";
						bUnwanted = false;
						continue;
					}
					if(Char.IsDigit(cb[i+1]))
					{
						int j = i+1;
						string sd = "";
						sd += cb[j];
						j++;
						while(j<cb.Length && cb[j] != ' ' && cb[j] != ',')
						{
							sd += cb[j];
							j++;
						}
//DEBUG("sd=", sd);
						if(IsInteger(sd)) //if there's an integer followed a work, then don't seperated them, see 'kw += sd';
						{
							kw += ' ';
							kw += sd;
							i = j-1;
//DEBUG("kw=", kw);
							//add to word list
							if(word != "") //get word first
							{
								if(bUnwanted)
								{
									ukws[uwords] = word;
									uwords++;// end of space, comma, plus and miners
								}
								else
								{
//DEBUG("word=", word + " len="+word.Length.ToString());
									kws[words] = word;
									words++;// end of space, comma, plus and miners
								}
								word = "";
							}
							if(bUnwanted) //get the integer
							{
								ukws[uwords] = sd;
								uwords++;// end of space, comma, plus and miners
							}
							else
							{
//DEBUG("word2=", sd + " len="+sd.Length.ToString());
								kws[words] = sd;
								words++;// end of space, comma, plus and miners
							}
							word = "";
							bUnwanted = false;
							//add to word list

							continue;
						}
					}

					if(word != "")
					{
						if(bUnwanted)
						{
							ukws[uwords] = word;
							uwords++;// end of space, comma, plus and miners
						}
						else
						{
//DEBUG("word3=", word + " len="+word.Length.ToString());
							kws[words] = word;
							words++;// end of space, comma, plus and miners
						}
					}
					word = "";
					bUnwanted = false;
				}
				
				kw += "%' OR LOWER(p.name) LIKE '%";
			}
			else if(cb[i] == '-' && i > 0 && cb[i-1] == ' ')
			{
				kw += "%' AND LOWER(p.name) NOT LIKE '%";
				bUnwanted = true;
			}
			else if(cb[i] == '+')
				kw += "%' AND LOWER(p.name) LIKE '%";
			else
			{
				kw += cb[i];
				word += cb[i];
			}
		}
	}
	kw += "%' ";
	if(word != "") // get the last words
	{
		if(bUnwanted)
		{
			ukws[uwords] = word;
			uwords++;// end of space, comma, plus and miners
//DEBUG("ukw=", word);
		}
		else
		{
//DEBUG("word=", word);
			kws[words] = word;
			words++;// end of space, comma, plus and miners
//DEBUG("kw=", word);
		}
		word = "";
		bUnwanted = false;
	}
//DEBUG("words=", words);
//DEBUG("uwords=", uwords);
//DEBUG("kw=", kw);
	if(words > 1)
	{
		//first step search, search for exact matches, all words, together, in order
		kw1 = "";
		for(int i=0; i<words; i++)
		{
			kw1 += kws[i];
			if(i != words - 1)
				kw1 += " ";
		}
		ss = " LOWER(p.name) LIKE '%";
		ss += kw1.ToLower(); 
		ss += "%' OR LOWER(p.supplier_code) LIKE '%" + kw1.ToLower() + "%' ";
		if(uwords > 0)
		{
			for(int j=0; j<uwords; j++)
			{
				ss += " AND LOWER(p.name) NOT LIKE '%";
				ss += ukws[j].ToLower();
				ss += "%'";
			}
		}
//DEBUG("1st search, ss=", ss);
		if(!DoSearch()) //1st step search
			return;
		
		if(returns <= 0)
		{

		//2nd step search, search for all words, sepearated
		ss = "";
		for(int i=0; i<words; i++)
		{
			if(ss != "")
				ss += " AND LOWER(p.name) LIKE";
			else 
				ss = " LOWER(p.name) LIKE";
			ss += "'%";
			ss += kws[i].ToLower();
			ss += "%'";
		}
		if(uwords > 0)
		{
			for(int j=0; j<uwords; j++)
			{
				ss += " AND LOWER(p.name) NOT LIKE '%";
				ss += ukws[j].ToLower();
				ss += "%'";
			}
		}
		ss += " AND LOWER(p.name) NOT LIKE '%" + kw1.ToLower() + "%'";
//DEBUG("2nd search, ss=", ss);
		if(!DoSearch()) //2nd step search
			return;
		
		}
/*
		if(returns <= 0) //no match, do final search, (effert search)
		{
			ss = kw;
			if(!DoSearch()) //1st step search
				return;
		}
*/
	}

	if(keyword != "")
	{
		if(returns <= 0 && words <= 5) //no match, do final search, (effert search)
		{
			ss = kw;
//DEBUG("final search, ss=", ss + " words="+words.ToString());
			if(!DoSearch())
				return;
		}
	}
	else
	{
		Response.Write(" <h3>Nothing to search</h3>");
	}

	if(returns > 0)
		PrintResultList();
	else
	{
		Response.Write("<table><tr><td>&nbsp;&nbsp;&nbsp;</td><td>");
		Response.Write("<br>Your search - <b>" + keyword + "</b> - did not match any products.<br>");	
		if(m_sBuyType =="purchase" || m_sBuyType =="sales"){
		Response.Write ("<span style=\"color:red\">Or the item has phased out</span><Br>");
		}
		Response.Write("No product were found like \"<b>" + keyword + "</b>\".<br><br>");
		Response.Write("<b>Suggestions:</b><br>");
		Response.Write("Make sure all words are spelled correctly.<br>");
		Response.Write("Try different keywords.<br>");
		Response.Write("Try more general keywords.<br>");
		Response.Write("</td></tr></table>");
	}
}	

void PrintResultList()
{
	m_bSearching = true;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		m_bFixedPrices = true;
	
	if(MyBooleanParse(GetSiteSettings("Enable_Use_Last_Sales_Fixed_Price", "0", true)))
		m_bUseLastSalesFixedPrice = true;

	if(MyBooleanParse(GetSiteSettings("stock_say_yes_no", "0", false)))
		m_bStockSayYesNo = true;

	CatalogDrawList(bAdmin);

	return;

	m_bShowCat =  false;
	bool bIncludeGST = false;
	if(Session["display_include_gst"].ToString() == "true")
		bIncludeGST = true;

	Response.Write("<table width=100% align=center bgcolor=white>");
	Response.Write("<tr><td>");

	//draw title
	Response.Write("<br><center><font size=+1><b>Search result for ");
	Response.Write(keyword);
	Response.Write("</b></font><br></center>");

	//begin product list
	Response.Write("<table cellspacing=0 cellpadding=3 rules=all bgcolor=white ");
	Response.Write("bordercolor=White border=0 width=100% style=\"font-family:Verdana;font-size:8pt;");
	Response.Write("border-collapse:collapse;\">");

	string subTableIndex = "s_cat";
	Boolean bAlterColor = false;
	DataRow dr = null;
	string code = "";
//	DataRow[] dra = dss.Tables[0].Select("", "name");
	DataRow[] dra = ds.Tables[0].Select("");

	Response.Write("<form name=frmMain action=search.aspx?" + Request.ServerVariables["QUERY_STRING"] + " method=post>\r\n");

	if(TS_UserLoggedIn() && !m_bDiscontinued)
	{
		//back to sales button
		if(m_sSite == "admin")
		{
			Response.Write("<tr><td colspan=11 align=right>");
			if(bOrder)
			{
				Response.Write("<tr><td colspan=11 align=right>");
				Response.Write("<input type=button " + Session["button_style"] + " value='View ");
				if(m_sBuyType == "purchase")
					Response.Write("Purchase Order");
				else if(m_sBuyType == "quote")
					Response.Write("Quote");
				else
					Response.Write("Sales");
				Response.Write("' onclick=window.location=('");
				if(m_sBuyType == "purchase")
					Response.Write("purchase.aspx");
				else if(m_sBuyType == "quote")
					Response.Write("q.aspx");
				else if(m_sBuyType == "sales")
					Response.Write("pos.aspx");
				else if(m_sBuyType == "quick_sales")
					Response.Write("pos_retail.aspx");
				Response.Write("?r=" + DateTime.Now.ToOADate() + "')>");
			}
		}
		else
		{
			Response.Write("<tr><td colspan=7 align=right>");
		}

		Response.Write("<input type=submit name=cmd  value='Update Price' " + Session["button_style"]);
		Response.Write(">");
		Response.Write("<input type=submit name=cmd value='Add To Cart' " + Session["button_style"]);
		Response.Write("></td></tr>");
	}

	//subtable header
	Response.Write(ApplyColor("<tr style=\"text-align:left;font-weight:bold;color:@@color_10;background-color:@@color_9;\">"));
	Response.Write("<th width=50>Code</th>");
	Response.Write("<th width=50>M_PN</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th width=50>Stock</th>");
	Response.Write("<th width=50>ETA</th>");
	Response.Write("<th width=50>QTY</th>");
	Response.Write("<th align=right>Price</th>");

/*	if(TS_UserLoggedIn())
	{
		Response.Write("<th nowrap>Price");
		if(bIncludeGST)
			Response.Write(" Inc-GST");
		else
			Response.Write(" Ext-GST");
		Response.Write("</th><th width=40>Stock</th>");
	}
*/
//	Response.Write("<th width=70>SHOP</th>");
//	if(bAdmin || m_bShowCat)
//		Response.Write("<th>CATEGORY</th>");
	if(bAdmin)
	{
		Response.Write("<th>ID</th>");
		Response.Write("<th>COST</th>");
		Response.Write("<th width=30>&nbsp;</th>");
	}

	Response.Write("</tr>");
//	Response.Write("\r\n\r\n");
	
	int start = (m_nPage - 1)* m_nPageSize;
	int i = start;
	while(i<dra.Length)
	{
		if(i >= start + m_nPageSize)
			break;

		dr = dra[i];
		string site = dr["site"].ToString();
		string company = "";
		string m_sSiteURL = "";

		if(site == "eden")
		{
			company = "EDEN";
			m_sSiteURL = "www.edenonline.co.nz";
		}
		else if(site == "asus")
			company = "ASUS";
		else if(site == "demo")
			company = "DEMO";
		else if(site == "test")
			company = "TEST";
		else if(site == "viewsonic")
			company = "ViewSonic";
		else if(site == "phone")
		{
			company = "Senxe";
			m_sSiteURL = "www.eznz.co.nz/phone";
		}
		else
			company = site;
			
		code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");

		//first column is blank
//		Response.Write("<td> </td>");
		
		//code	
		Response.Write("<td>");
/*		Response.Write("<a href=");
		if(!bAdmin && site != m_sCompanyName)
			Response.Write("http://" + m_sSiteURL + "/");
		Response.Write("p.aspx?");
		Response.Write(code);
		Response.Write(" class=o>");
*/
		Response.Write(ShowKeywords(code));
		Response.Write("</td>");

		Response.Write("<td>");
		Response.Write(ShowKeywords(supplier_code));
		Response.Write("</td>");
		
		//Description
		Response.Write("<td><a href=");
//		if(!bAdmin && site != m_sCompanyName)
//			Response.Write("http://" + m_sSiteURL + "/");
		Response.Write("p.aspx?");
//		Response.Write("<td><a href=p.aspx?");
		Response.Write(code);
		Response.Write(" class=d>");
		Response.Write(ShowKeywords(dr["name"].ToString()));
		Response.Write("</a></td>");

		PrintCartCode(dr, i);

/*		//buy link
		if(bAdmin || m_bShowCat)
		{
			string cat = dr["cat"].ToString();
			string s_cat = dr["s_cat"].ToString();
			string ss_cat = dr["ss_cat"].ToString();
			Response.Write("<td><a href=c.aspx?c=" + HttpUtility.UrlEncode(cat));
			Response.Write("&s=" + HttpUtility.UrlEncode(s_cat) + "&ss=" + HttpUtility.UrlEncode(ss_cat) + "><font color=green>");
			Response.Write(cat + "-" + s_cat + "-" + ss_cat + "</font></a></td>");	
		}
*/
		if(bAdmin)
		{
			string id = dr["supplier"].ToString() + dr["supplier_code"].ToString();
			string cost = double.Parse(dr["supplier_price"].ToString()).ToString("c");

			Response.Write("<td>" + ShowKeywords(id) + "</td>");
		//	Response.Write("<td>" + cost + "</td>");

			//admin edit
			Response.Write("<td><a href=liveedit.aspx?code=");
			Response.Write(code);
			Response.Write(" target=_blank class=o>Edit</a>");
			Response.Write("</td>");
		}

		Response.Write("</tr>");
		i++;
	}
	
//	Response.Write("<tr><td>&nbsp;</td></tr>");
	//end subTable

	if(TS_UserLoggedIn() && !m_bDiscontinued)
	{
		//back to sales button
		if(m_sSite == "admin")
		{
			Response.Write("<tr><td colspan=11 align=right>");
			if(bOrder)
			{
				Response.Write("<input type=button " + Session["button_style"] + " value='View ");
				if(m_sBuyType == "purchase")
					Response.Write("Purchase Order");
				else if(m_sBuyType == "quote")
					Response.Write("Quote");
				else
					Response.Write("Sales");
				Response.Write("' onclick=window.location=('");
				if(m_sBuyType == "purchase")
					Response.Write("purchase.aspx");
				else if(m_sBuyType == "quote")
					Response.Write("q.aspx");
				else if(m_sBuyType == "sales")
					Response.Write("pos.aspx");
				else if(m_sBuyType == "quick_sales")
					Response.Write("pos_retail.aspx");
				Response.Write("?r=" + DateTime.Now.ToOADate() + "')>");
			}
		}
		else
		{
			Response.Write("<tr><td colspan=7 align=right>");
		}
		Response.Write("<input type=submit name=cmd  value='Update Price' " + Session["button_style"]);
		Response.Write(">");
		Response.Write("<input type=submit name=cmd value='Add To Cart' " + Session["button_style"]);
		Response.Write("></td></tr>");
	}
	Response.Write("</form>");

	Response.Write(PrintPageIndex());
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
}

string PrintPageIndex()
{	
	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=" + m_cols + ">Page: ");
	int pages = ds.Tables[0].Rows.Count / m_nPageSize + 1;
	int i=m_nStartPageButton;
	if(m_nStartPageButton > 10)
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&kw="+ HttpUtility.UrlEncode(Request.QueryString["kw"]) +"&p=");
		sb.Append((i-10).ToString());
		sb.Append("&spb=");
		sb.Append((i-10).ToString());
		sb.Append(">...</a> ");
	}
	for(;i<=m_nStartPageButton + m_nPageButtonCount; i++)
	{
		if(i > pages)
			break;
		if(i != m_nPage)
		{
			sb.Append("<a href=");
			sb.Append(WriteURLWithoutPageNumber());
			sb.Append("&kw="+ HttpUtility.UrlEncode(Request.QueryString["kw"]) +"&p=");
			sb.Append(i.ToString());
			sb.Append("&spb=" + m_nStartPageButton.ToString() + ">");
			sb.Append(i.ToString());
			sb.Append("</a> ");
		}
		else
		{
			sb.Append("<font size=+1><b>" + i.ToString() + "</b></font>");
			sb.Append(" ");
		}
	}
	if(i<pages)
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&kw="+ HttpUtility.UrlEncode(Request.QueryString["kw"]) +"&p=");
		sb.Append(i.ToString());
		sb.Append("&spb=");
		sb.Append(i.ToString());
		sb.Append(">...</a> ");
		sb.Append("</td></tr>");
	}
	return sb.ToString();
}
/*
string WriteURLWithoutPageNumber()
{
	string s = "?kw=" + HttpUtility.UrlEncode(keyword);
	return s;
}
*/
bool DoSearch()
{
//DEBUG("ss=", ss);
	int rows = 0;
	if(bAdmin)
	{
		if(!SearchDB(m_sCompanyName, "mpn", ref rows))
			return false;
		if(!SearchDB(m_sCompanyName, "product", ref rows))
			return false;
		if(!SearchDB(m_sCompanyName, "product_skip", ref rows))
			return false;
	}
	else
	{
		if(!SearchDB(m_sCompanyName, "mpn", ref rows))
			return false;
		if(!SearchDB(m_sCompanyName, "product", ref rows))
			return false;
//		if(!SearchDB(m_sCompanyName, "product_skip", ref rows))
//			return false;
	}
	return true;
}

bool SearchDB(string dbName, string tableName, ref int rows)
{
	if(MyBooleanParse(GetSiteSettings("stock_say_yes_no", "0", false)))
		m_bStockSayYesNo = true;
	int nKW = -1;
	if(IsInteger(keyword) && keyword != "")
	{
		try
		{
			nKW = int.Parse(keyword);
		}
		catch(Exception e)
		{
		}
	}
	
	SqlConnection myConnection = new SqlConnection("Initial Catalog=" + dbName + m_sDataSource + m_sSecurityString);
	
	string sc = "";
	if(tableName == "mpn")
	{
		sc = " SELECT c.inactive, c.moq, c.name_cn, c.stock_location, c.barcode, c.code, c.name, c.brand, c.supplier,c.inner_pack , c.weight , c.*";
		sc += ", p.price ";
		if(bIncludeGST)
			sc += " * 1.15 ";
		sc += " AS price ";
		sc += ", c.supplier_code, c.supplier_price, '" + dbName + "' AS site ";
		if(g_bRetailVersion)
			sc += ", (SELECT ISNULL(sum(qty-allocated_stock),0) AS stock FROM stock_qty sq WHERE sq.code = c.code) AS stock ";
		else if(m_bStockSayYesNo)
			sc += ", p.stock AS stock ";
		else
			sc += ", p.stock-p.allocated_stock AS stock ";
		sc += ", p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
		sc += ", c.currency, c.foreign_supplier_price, p.price_age ";
		sc += ", c.level_price0 AS bottom_price ";
		sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
		sc += " LEFT OUTER JOIN barcode b ON b.item_code = p.code ";
		sc += " WHERE c.supplier_code LIKE '" + keyword + "%' ";
		sc += " ORDER BY c.supplier_code ";
//		sc += " ORDER BY c.s_cat, c.ss_cat, c.brand, c.code";
	}
	else if(tableName == "product")
	{
		sc = "SELECT c.inactive, c.moq, c.name_cn, c.stock_location, c.barcode, c.code, c.name, c.brand, c.supplier,c.inner_pack, c.weight , c.*";
		sc += ", p.price  ";
		if(bIncludeGST)
			sc += " * 1.15 ";
		sc += " AS price";
		sc += ", c.supplier_code, c.supplier_price, '" + dbName + "' AS site ";
		if(g_bRetailVersion)
			sc += ", (SELECT ISNULL(sum(qty-allocated_stock),0) AS stock FROM stock_qty sq WHERE sq.code = c.code) AS stock ";
		else if(m_bStockSayYesNo)
			sc += ", p.stock AS stock ";
		else
			sc += ", p.stock-p.allocated_stock AS stock ";
		sc += ", p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped, c.clearance ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
		sc += ", c.currency, c.foreign_supplier_price, p.price_age ";
		sc += ", c.level_price0 AS bottom_price ";
		sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
		sc += " LEFT OUTER JOIN barcode b ON b.item_code = p.code ";
		sc += " WHERE 1 = 1 ";
		sc += " AND c.supplier_code NOT LIKE '" + keyword + "%' ";
		sc += " AND  ( (";
		sc += ss;
		sc += ") AND p.name <> ''  ";
		sc += " OR c.barcode = '" + keyword + "' ";	
		sc += " OR b.barcode = '" + keyword + "' ";
		sc += " OR LOWER(p.supplier_code) LIKE '%" + keyword.ToLower() + "%'";
		
		sc += ") ";
		if(m_supplierString != "")
			sc += " AND p.supplier IN" + m_supplierString;
		if(Session["cat_access_sql"] != null)
		{
			if(Session["cat_access_sql"].ToString() != "all")
			{
				sc += " AND p.brand " + Session["cat_access_sql"].ToString();
				sc += " AND p.s_cat " + Session["cat_access_sql"].ToString();
				sc += " AND p.ss_cat " + Session["cat_access_sql"].ToString();
			}
		}
		sc += " ORDER BY c.supplier_code ";
//		sc += " ORDER BY c.s_cat, c.ss_cat, c.brand, c.code";
//DEBUG("sc=", sc);		
	}
	else
	{
		ss = ss.Replace("p.", "c.");

		sc = "SELECT c.inactive, c.barcode, c.code, c.name AS name, c.brand, '" + dbName + "' AS site , c.inner_pack, c.weight, c.*";
		sc += ", p.price ";
		if(bIncludeGST)
			sc += "* 1.15 ";
		sc += " AS price ";
		sc += ", c.supplier, c.supplier_code, c.supplier_price ";
		if(g_bRetailVersion)
			sc += ", (SELECT ISNULL(sum(qty),0) AS stock FROM stock_qty sq WHERE sq.code = c.code) AS stock ";
		else
			sc += ", p.stock AS stock ";
		sc += " , c.cat, c.s_cat, c.ss_cat, p.eta, 0 AS price_dropped, c.clearance ";

		//sc += ", p.stock AS stock, c.cat, c.s_cat, c.ss_cat, p.eta, 0 AS price_dropped, c.clearance ";
		for(int j=1; j<=9; j++)
			sc += ", c.level_rate" + j + ", c.qty_break" + j + ", c.qty_break_discount" + j + ", c.price" + j;
		sc += ", c.currency, c.foreign_supplier_price, '01/01/2004' AS price_age ";
		sc += ", c.level_price0 AS bottom_price ";
		sc += " FROM code_relations c JOIN product_skip p ON c.id=p.id ";
		sc += " LEFT OUTER JOIN barcode b ON b.item_code = c.code ";
		sc += "WHERE ";
		if(m_sBuyType =="purchase" || m_sBuyType =="sales" ){
		sc +=" c.skip<>'1' And";
		}
		sc += "( (";
		sc += ss;
		sc += ") AND name!=''  ";
		sc += " OR c.barcode = '" + keyword + "' ";	
		sc += " OR b.barcode = '" + keyword + "' ";
		sc += " OR LOWER(c.supplier_code) LIKE '%" + keyword.ToLower() + "%' ";
		if(m_supplierString != "" && m_supplierString != null)
			sc += ") AND c.supplier IN" + m_supplierString;
		else 
			sc += ")";
		sc += " ORDER BY c.supplier_code ";
//		sc += " ORDER BY c.s_cat, c.ss_cat, c.brand, c.code";
	}
//DEBUG("sc=", sc);
//return true;	
//	SqlConnection myConnection = new SqlConnection("Initial Catalog=" + dbName + m_sDataSource + m_sSecurityString);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "product");
//DEBUG("table="+tableName + ", ss="+ss, " rows="+rows.ToString());
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	returns += rows;
//DEBUG("rows=", rows);
//Response.Flush();
	return true;
}

bool PrintCartCode(DataRow dr, int i)
{
	bool bAddBookMark = true;
	bool bAddSort = true;
	bool bClearance = false;
	int	nBookMarkCount = 0;
	int nStart = (m_nPage - 1) * m_nPageSize;
	int nEnd = nStart + m_nPageSize;

	string code = dr["code"].ToString();

	//supplier info
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	double dForeignCost = MyDoubleParse(dr["foreign_supplier_price"].ToString());
	if(bOrder && m_sBuyType == "purchase")
	{
		Response.Write("<td>" + supplier + "</td>");
//		Response.Write("<td>" + GetEnumValue("currency", dr["currency"].ToString()).ToUpper().Substring(0, 2) + dForeignCost.ToString("c") + "</td>");
		Response.Write("<td>" + GetCurrencyName(dr["currency"].ToString()).ToUpper().Substring(0, 2) + dForeignCost.ToString("c") + "</td>");
	}

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


	double level_rate = lr[m_nDealerLevel - 1];
	double dPrice = double.Parse(dr["price"].ToString());
	double dBottomPrice = dPrice * lr[0];
	if(!bClearance)
		dPrice *= level_rate;

	double dPriceOnePiece = dPrice; //price without qty discount

	//calculate price and discount on qty
	string qty = "0";
	if(Request.Form["qty" + i] != null)
		qty = Request.Form["qty" + i];
	
	int dqty = MyIntParse(qty);
	dDiscount = 0;
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
			if(!bClearance)
			{
				dPrice *= (1 - dQtyDiscount);
				dDiscount = dQtyDiscount;
			}
				
			if(m_bAddToCart)
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
					if(Session["c_editing_order"] != null)
					{
						Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=corder.aspx?r=" + DateTime.Now.ToOADate() + "\">");
						return false;
					}
				}
			}
		}
	}
	dDiscount = Math.Round((dBottomPrice - dPrice)/dBottomPrice, 4);

	//Stock
	string stock = dr["stock"].ToString();
	if(stock == "")
		stock = "Yes";
	stock = GetAllBranchStock(code);
	Response.Write("<td align=center>");
	Response.Write(stock);
	Response.Write("</td>");

	//ETA
	Response.Write("<td align=center><font color=red>");
	Response.Write(dr["eta"].ToString());
	Response.Write("</font></td>");
/*
	//discount
	if(m_sBuyType != "purchase")
	{
		Response.Write("<td>");
		Response.Write(dDiscount.ToString("p"));
		Response.Write("</td>");
	}
*/
	//qty
	Response.Write("<td align=right nowrap>");
	Response.Write("<input type=text size=3 autocomplete=off style='font-size:8pt;text-align:center;' name=qty" + i + " value=");
	Response.Write(qty);
	Response.Write("></td>");

	if(m_sBuyType != "purchase")
	{
		//price
		Response.Write("<td align=right>");
		if(dqty > 1 && !bClearance)
			Response.Write("<font color=red><b>");
		Response.Write(dPrice.ToString("c"));
		if(dqty > 1 && !bClearance)
			Response.Write("</b></font>");
		Response.Write("</td>");
	}

	return true;
}

</script>

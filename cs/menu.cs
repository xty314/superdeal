<!-- #include file="card_access.cs" -->

<script runat=server>

const string m_sHeader = @"";
string m_sFooter = "";

string m_cat_access = "all";

Boolean PrintHeaderAndMenu()
{
	if(Session["show_popular_only"] == null)
	{
//		Session["show_popular_only"] = true;
//		if(m_sSite != "admin")
			Session["show_popular_only"] = MyBooleanParse(GetSiteSettings("show_popular_only", "0", true));
	}

	if(Session["cat_access"] == null)// && Session["cat_access"].ToString() != "all")
		Session["cat_access"] = "";

	string str = Session["cat_access"].ToString();
	string cat_block_to_all = GetSiteSettings("main_category_block_to_all", "");
	if(cat_block_to_all != "" && m_sSite != "admin")
		str += cat_block_to_all;
	m_cat_access = BuildCatAccess(str);
	Session["cat_access_sql"] = m_cat_access;

	string kw = "";
//	if(Session["search_keyword"] != null)
//		kw = Session["search_keyword"].ToString();
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
				
	if(m_bDealerArea)
	{
		m_sFooter =ReadSitePage("dealer_foot_menu");
		string terms = ReadSitePage("terms");
		m_sFooter = m_sFooter.Replace("@@terms", terms);
	}
	else
	{
		m_sFooter = ReadSitePage("foot_menu");
		string terms = ReadSitePage("terms");
		m_sFooter = m_sFooter.Replace("@@search_keyword", kw);
		m_sFooter = m_sFooter.Replace("@@terms", terms);
	}
	string menu = "";
	string sMenuSubTables = "";
	if(!BuildMenuCache(ref menu, ref sMenuSubTables))
		return false;
//DEBUG("menu",menu);
	menu = BlockSysQuote(menu);
//DEBUG("menu",menu);
	string ssid = ""; //sales session id
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = "&ssid=" + Request.QueryString["ssid"];

	TSAddCache("item_categories", menu);

	string header = ReadSitePage("public_page_header");
	string sCatOptions = BuildCatOptions(g("cat"));
	string sSCatOptions = BuildSCatOptions(g("cat"), g("scat"));
	header = header.Replace("@@CAT_OPTIONS", sCatOptions);
	header = header.Replace("@@S_CAT_OPTIONS", sSCatOptions);
	//if(m_sRoot != "")
	 if(m_bDealerArea)
		header = ReadSitePage("dealer_page_header");
	 if(m_sSite =="admin" )
	header = ReadSitePage("admin_visual_page_header");
	header = ApplyCustomerAccessLevel(header);
	header = header.Replace("@@search_keyword", kw);
	header = header.Replace("@@HEADER_MENU_TOP_CAT", menu);
	header = header.Replace("@@public_page_title", m_sCompanyTitle);
	header = header.Replace("@@companyTitle", m_sCompanyTitle);
	header = header.Replace("@@products", GetTotalProducts());
	header = header.Replace("@@rnd", "r=" + DateTime.Now.ToOADate());
	string bodyList = ReadSitePage("body_item_list");
	string login = "<a href=login.aspx?logoff=true><span style=\"color:red\">Log off</span></a>";
    string lbutton ="<input type=button value='Log off'  onclick=\"window.location=('login.aspx?logoff=true')\" style=\"font:bold 12px arial;color:red\">";
    string sUserName = "guest";
	string frontEndLogin = "";
	frontEndLogin += "<li><a class=\"account\" href=\"register.aspx\">My Account</a></li>";
	frontEndLogin += "<li><a class=\"account\" href=\"status.aspx?t=1\">My Order</a></li>";
	frontEndLogin += "<li><a class=\"account\" href=\"#\">Welcome: @@LOGIN_NAME</a></li>";
	frontEndLogin += "<li><a class=\"btn btn-warning btn-logoff\" style='background-color: #fa7c63;border-color: #fa7c63;' href=\"login.aspx?logoff=true\">Log off</a></li>";
	if(!TS_UserLoggedIn())
	{
		login = "<a href=login.aspx >Log on</a>";
		lbutton ="<input type=submit value=LOGIN name=go>";
		frontEndLogin = "<li><a class=\"account btn btn-primary btn-login\" style=\"background-color: #4c4c4c;border-color: #333;\" href=\"login.aspx\">LOGIN</a></li>";
		frontEndLogin += "<li><a class=\"btn btn-primary btn-login\" style=\"background-color: #4c4c4c;border-color: #333;\" href=\"register.aspx?a=reseller_reg\">REGISTER</a></li>";
	}
	else
	{ 
		sUserName =  Session["name"].ToString();
	}
	//frontend start
	if(m_sSite =="www" || m_sSite == "dealer"){
		string frontEndCart = GetRowTemplate(ref header, "cart");
	//	frontEndCart = GetPageTopCart(frontEndCart);
		header = header.Replace("@@template_cart", frontEndCart.ToString());
		header = header.Replace("@@FRONT_END_LOGIN", frontEndLogin);
	}
	//frontend end
	header = header.Replace("@@LOGIN_NAME", sUserName);
	header = header.Replace("@@login", login);
	header = header.Replace("@@lbutton", lbutton);
	header = header.Replace("@@ssid_value", Request.QueryString["ssid"]);
	header = header.Replace("@@ssid", "&ssid=" + Request.QueryString["ssid"]);
	header = ApplyColor(header);
	if(Session["cart_total_no_gst"] == null)
		Session["cart_total_no_gst"] = 0;
	header = header.Replace("@@cart_total", (MyDoubleParse(Session["cart_total_no_gst"].ToString())).ToString("c"));
	if(Session["online_order_po_number"] == null)
		Session["online_order_po_number"] = "";
	header = header.Replace("@@cust_po_number", Session["online_order_po_number"].ToString());
    if(m_bDealerArea)
		header = header.Replace("@@DealerBodyList", bodyList);
	Response.Write(header);
//	Response.Flush();
	return true;
}

Boolean BuildBrandsSubMenu()
{
	DataSet dsCache = new DataSet();
	int rows = 0;
	string sc = "SELECT s_cat, ss_cat FROM catalog";
	if(m_supplierString != "")
		sc += m_catTableString;
	sc += " WHERE cat='Brands' ";
	if(m_sSite != "admin")
		sc += " AND s_cat <> 'ServiceItem' ";
	if(m_cat_access != null && m_cat_access != "" && m_cat_access != "all")
		sc += " AND s_cat " + m_cat_access + " ";
	sc += " ORDER BY s_cat, ss_cat";
//DEBUG(" sc 1= ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsCache, "brands");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
       
	     
	string tpL = ReadSitePage("left_side_menu_sub_cat");
	 if(m_sSite =="admin" || m_bDealerArea)
	       tpL = ReadSitePage("left_sub_cat_admin");
		string scat = Request.QueryString["s"];
		string tcat ="";
	    tcat = tcat.Replace("@@tcat",scat);
		Response.Write(tcat);
		
	string srowTemplateL = GetRowTemplate(ref tpL, "menurow");
	string tpO = tpL;
	string srowL = srowTemplateL;

	//apply view limit to left side sub menu
	string ssublimit = "";
	if(Session["cat_access_sql"] != null)
		ssublimit = Session["cat_access_sql"].ToString();

	StringBuilder sbSubSub = new StringBuilder();
	DataRow dr = null;

	string b = "-1";
	string c = "-1";
	string b_old = "-1";
	string c_old = "-1";
	int i = 0;
	for(;i<=rows;i++)
	{
		if(i<rows)
		{
			dr = dsCache.Tables["brands"].Rows[i];
			b = dr["s_cat"].ToString();
			c = dr["ss_cat"].ToString();
			Trim(ref b);
			Trim(ref c);
		}
		if(String.Compare(b, b_old, true) != 0 || i==rows)
		{
			if(sbSubSub.ToString() != "" && b_old != "-1") //end of subsub menu
			{
				sbSubSub.Append("</table>");
//				sbSubSub.Append("<!-- end of side menu -->\r\n\r\n");
				string sid = m_sCompanyName + m_sSite + "cache_leftmenu_brands_";
				sid += b_old;
				sid = sid.ToLower();
				tpL = tpL.Replace("@@template_menurow", "");
				TSAddCache(sid, tpL);
				tpL = tpO;

//				TSAddCache(sid, sbSubSub.ToString());
				sbSubSub.Remove(0, sbSubSub.Length);
				c_old = "-1";

				if(i == rows)
				{
//DEBUG("i==rows, end of leftmenu:", sid);
					break;
				}
			}

			b_old = b;

			//begin sub sub menu
//			sbSubSub.Append("<!-- begin side menu -->\r\n");
			sbSubSub.Append("<table class=n cellpadding=2 cellspacing=0 bgcolor='#EEEEEE' width=110>");
			sbSubSub.Append("<tr rowspan=3><td>&nbsp;</td></tr>");
			sbSubSub.Append("\r\n");
		}

		if(String.Compare(c, c_old, true) != 0)
		{
			string brandLink = b;
//			string ssLink = c;
			string ssName = c;
			if(b == "zzzOthers")
				brandLink = "";
			if(c == "zzzOthers")
			{
//				ssLink = "";
				ssName = "All Others";
			}

			string brand_link = "c.aspx?b=" + HttpUtility.UrlEncode(brandLink);
			brand_link += "&s=" + HttpUtility.UrlEncode(c);
		//	brand_link += "@@ssid";

			string sub_menu_link = brand_link;
			string sub_menu_name = ssName;
			if(ssublimit == "" || ssublimit.IndexOf(sub_menu_name.ToLower()) < 0)
			{
				srowL = srowL.Replace("@@sub_menu_link", sub_menu_link);
				srowL = srowL.Replace("@@sub_menu_name", sub_menu_name);
				tpL = tpL.Replace("@@template_menurow", srowL + "@@template_menurow");
				srowL = srowTemplateL;
			}
			StringBuilder sbb = new StringBuilder();

			sbb.Append("<tr><td>&nbsp;<img src=/i/reddot.gif width=10></td><td>");
			sbb.Append("<a href='c.aspx?r=" + DateTime.Now.ToOADate() + "@@ssid&b=");
			sbb.Append(HttpUtility.UrlEncode(brandLink));
			sbb.Append("&s=");
			sbb.Append(HttpUtility.UrlEncode(c));
			sbb.Append("' class=d>");
			sbb.Append(ssName);
			sbb.Append("</a></td></tr><tr><td>&nbsp;</td></tr>");
			sbb.Append("\r\n");
			sbSubSub.Append(sbb.ToString()); 

			c_old = c;
		}
	}
	return true;
}

Boolean BuildMenuCache(ref string sMenu, ref string sMenuSubTables)
{
	if(!BuildBrandsSubMenu())
		return false;

	DataSet dsCache = new DataSet();
	int rows = 0;
	
	string sc = "SELECT * FROM catalog ";
	sc += " WHERE 1=1 ";
	if(m_sSite != "admin")
		sc += " AND cat <> 'ServiceItem' AND cat <> 'Brands' ";
	else if(m_supplierString != "")
		sc += " AND " + m_catTableString;
	if(m_cat_access != null && m_cat_access != "" && m_cat_access != "all")
	{
		sc += " AND s_cat " + m_cat_access + " ";
		sc += " AND cat " + m_cat_access + " "; //block main category as well
	}
	sc += " ORDER BY seq, LTRIM(RTRIM(cat)), LTRIM(RTRIM(s_cat)), LTRIM(RTRIM(ss_cat))";
//DEBUG("sc =",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsCache, "catalog");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
          
		   
	string tpL = ReadSitePage("left_side_menu_sub_cat");
	//if(m_sSite =="admin" || m_bDealerArea)
	//	tpL = ReadSitePage("left_sub_cat_admin");
	string srowTemplateL = GetRowTemplate(ref tpL, "menurow");
	string tpO = tpL;
	string srowL = srowTemplateL;

	string tp = ReadSitePage("public_header_menu_top_cat");
	if(m_sSite == "admin")
		tp = ReadSitePage("header_menu_top_cat");
	else if(m_sSite == "dealer")
		tp = ReadSitePage("dealer_header_menu_top_cat");
	////////////// C.M Modifty 05/11/2007 ////
    string DealerAcc = ReadSitePage("dealer_account_info");
	if(m_sSite !="admin" && m_bDealerArea)
	{
		//tp = ReadSitePage("header_menu_top_cat");
		tp = tp.Replace("@@DealerAccInfo", DealerAcc);
	}
	else if(m_sSite == "admin" || m_bDealerArea)
	{
		//tp = ReadSitePage("header_menu_top_cat");
		tp = tp.Replace("@@DealerAccInfo",""); 
	}
/////////////// Modify End //////////
	if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
	{///login is true..  so allow hiding the login menu on body item and also on the menu header and cat header..
		tp = tp.Replace("@@HIDE_LOGIN_FRONT", "<!--");
		tp = tp.Replace("@@HIDE_LOGIN_END", "-->");
		tp = tp.Replace("@@LOGIN_NAME", Session["name"].ToString());
	
	}
	else
	{
		tp = tp.Replace("@@HIDE_LOGIN_FRONT", "");
		tp = tp.Replace("@@HIDE_LOGIN_END", "");
		tp = tp.Replace("@@LOGIN_NAME", "Guest");
	}
    string new_link = "c.aspx?r=1@@ssid&";
    new_link += "t=new";
    tp = tp.Replace("@@new_link", new_link);
	string tp_group = GetRowTemplate(ref tp, "menugroup");
	string sout = tp;
	string g = tp_group;
//DEBUG("tp_group=", tp_group);
	string srowTemplate = GetRowTemplate(ref g, "menurow");
	string srow = srowTemplate;
	string menu_id = "";
//DEBUG("osrow=", srow);

				
	//apply view limit to left side sub menu
	string ssublimit = "";
	if(Session["cat_access_sql"] != null)
		ssublimit = Session["cat_access_sql"].ToString();

	StringBuilder sbMain = new StringBuilder();
	StringBuilder sbSub = new StringBuilder();
	StringBuilder sbSubSub = new StringBuilder();

//	sbMain.Append("\r\n\r\n<!-- begin main menu -->\r\n");

	DataRow dr = null;

	string c = "-1";
	string s = "-1";
	string ss = "-1";
	string c_old = "-1";
	string s_old = "-1";
	string ss_old = "-1";
	string c_old_bak = "-1";
	string s_old_bak = "-1";
	int i = 0;
	bool bCatChanged = false;
	for(;i<=rows;i++)
	{
		bCatChanged = false;
		if(i<rows)
		{
			dr = dsCache.Tables["catalog"].Rows[i];
			c = dr["cat"].ToString();
			s = dr["s_cat"].ToString();
			ss = dr["ss_cat"].ToString();

			Trim(ref c);
			Trim(ref s);
			Trim(ref ss);

			if(String.Compare(c, c_old, true) != 0)
			{
				bCatChanged = true;
				c_old_bak = c_old;
				
				if(sbSub.ToString() != "") //end of this sub menu
				{
					if(c != null && c != "")
						menu_id = c_old;
					else
						menu_id = "_";
			//		DEBUG("msunu i d=", menu_id);
					string menu_link_top = "c.aspx?r=1@@ssid&";
					menu_link_top += "c=";
					menu_link_top += HttpUtility.UrlEncode(menu_id);
					g = g.Replace("@@menu_id", menu_id);
					g = g.Replace("@@top_menu_link", menu_link_top);
					g = g.Replace("@@template_menurow", ""); //delete the last tag
					sout = sout.Replace("@@template_menugroup", g + "@@template_menugroup");

					if(String.Compare(c_old, "brands", true) == 0)
					{
						sbSub.Append("<tr><td class=dl> <a href=m.aspx?m=brands&t=more class=d>More brands...</a></td></tr>");
					}
					sbSub.Append("</table>");
					s_old_bak = s_old;
					s_old = "-1"; 
				}

				g = tp_group;
				GetRowTemplate(ref g, "menurow"); //replace menurow with @@template_menurow
				srow = srowTemplate;

				sbMain.Append(MenuAddMain(c));
				c_old = c;
				
				//begin build sub menus
//				sbSub.Append("\r\n\r\n<!-- sub menu -->\r\n");
				sbSub.Append("<table border=0 class=m id='");
				if(c != null && c != "")
					sbSub.Append(c);
				else
					sbSub.Append("_");
				sbSub.Append("Menu' width=156 style='position:absolute;top:0;left:0;z-index:100;visibility:hidden' onmouseout='hideMenu()'>");
//				sbSub.Append("\r\n");
			}
		}
		if(String.Compare(s, s_old, true) != 0 || (i == rows && rows > 0) || bCatChanged)
		{
			if(sbSubSub.ToString() != "" && s != "" && c != "Brands") //end of subsub menu
			{
				if(c_old_bak == "-1")
					c_old_bak = c;
				if(s_old_bak == "-1")
					s_old_bak = s;
				sbSubSub.Append("</table>");
//				sbSubSub.Append("<!-- end of side menu -->\r\n\r\n");
				string sid = m_sCompanyName + m_sSite + "cache_leftmenu_";
				if(bCatChanged)
					sid += c_old_bak;
				else
					sid += c;
				sid += "_";
				if(bCatChanged)
					sid += s_old_bak;
				else
					sid += s_old;
				sid = sid.ToLower();
				tpL = tpL.Replace("@@template_menurow", "");
//DEBUG("sid=", sid);
//DEBUG("tpL=", tpL + "<br><br><br>");
				TSAddCache(sid, tpL);
				tpL = tpO;

//				TSAddCache(sid, sbSubSub.ToString());
				sbSubSub.Remove(0, sbSubSub.Length);
				ss_old = "-1";
				
				if(i == rows)
				{
					break;
				}
			}

			if(s != s_old)
			{
				sbSub.Append(MenuAddSub(c, s));
				s_old = s;

				bool bWriteMenu = true;
				if(String.Compare(c, "brands", true) == 0)
				{
					if(IsThisBrandShow(s) == false || s == "zzzOthers")
						bWriteMenu = false;
				}

				if(bWriteMenu)
				{
					string menu_link = "c.aspx?r=1@@ssid&";
					
					if(c == "Brands")
					{
						menu_link += "b=";
						menu_link += HttpUtility.UrlEncode(s);
					
					
					}
					else
					{
						menu_link += "c=";
						menu_link += HttpUtility.UrlEncode(c);
						menu_link += "&s=";
						menu_link += HttpUtility.UrlEncode(s);
					
					}
					string menu_name = s;
					if(s == "zzzOthers")
						menu_name = "All Others";

					srow = srow.Replace("@@menu_link", menu_link);
					srow = srow.Replace("@@menu_name", menu_name);
//DEBUG("fsrow=", srow);
					srow += "@@template_menurow";
					g = g.Replace("@@template_menurow", srow);
					srow = srowTemplate;
				}
			}

			//begin sub sub menu
			if(c != "Brands")
			{
				sbSubSub.Append("<table class=n cellpadding=2 cellspacing=0 bgcolor=#EEEEEE width=110>");
				sbSubSub.Append("<tr rowspan=3><td>&nbsp;</td></tr>");
				srowL = srowTemplateL;
			}
		}
      
		if(String.Compare(ss, ss_old, true) != 0)
		{
			if(c != "Brands")
			{
				sbSubSub.Append(MenuAddSubSub(c, s, ss)); 

				string sub_menu_link = "c.aspx?r=@@ssid&c=";
				sub_menu_link += HttpUtility.UrlEncode(c);
				sub_menu_link += "&s=";
				sub_menu_link += HttpUtility.UrlEncode(s);
				sub_menu_link += "&ss=";
				sub_menu_link += HttpUtility.UrlEncode(ss);
				string sub_menu_name = ss;
				if(ss == "zzzOthers")
					sub_menu_name = "All Others";
					
            
	      
			 
//if(Session["email"].ToString() == "darcy@eznz.com")
//{DEBUG("ssublimit=", ssublimit);
//DEBUG("name=", sub_menu_name);}
				if(ssublimit == "" || ssublimit.IndexOf(sub_menu_name.ToLower()) < 0)
				{
					srowL = srowL.Replace("@@sub_menu_link", sub_menu_link);
					srowL = srowL.Replace("@@sub_menu_name", sub_menu_name);
					tpL = tpL.Replace("@@template_menurow", srowL + "@@template_menurow");
					srowL = srowTemplateL;
				}
			}
			ss_old = ss;
		}
	}

	if(sbSub.ToString() != "")
	{
		sbSub.Append("</table>");
		sbSub.Append("\r\n\r\n");
	}

	sbSub.Append(AppendUsedCatalog());

//	sMenu = sbMain.ToString();// + sbSub.ToString();
//	sMenuSubTables = sbSub.ToString();

	menu_id = c;
	if(c != null && c != "")
		menu_id = c;
	else
		menu_id = "_";

//DEBUG("srow=", srow);
	string menu_link_top_o = "c.aspx?r=1@@ssid&";
	menu_link_top_o += "c=";
	menu_link_top_o += HttpUtility.UrlEncode(menu_id);
    g = g.Replace("@@menu_id", menu_id);
	g = g.Replace("@@top_menu_link", menu_link_top_o);
 //	g = g.Replace("@@template_menurow", srow);
	g = g.Replace("@@template_menurow", ""); //delete the last tag
   
	sout = sout.Replace("@@template_menugroup", g);
//DEBUG("sout=", sout);	
	sMenu = sout;
	return true;
}

string AppendUsedCatalog()
{
	DataSet dsuc = new DataSet();
	string sc = "SELECT id, cat FROM used_catalog ORDER BY cat";
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		myCommand1.Fill(dsuc, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<table border=0 class=m id='UsedParts");
	sb.Append("Menu' width=156 style='position:absolute;top:0;left:0;z-index:100;visibility:hidden' onmouseout='hideMenu()'>");
	for(int i=0; i<dsuc.Tables["cat"].Rows.Count; i++)
	{
		string cat = dsuc.Tables["cat"].Rows[i]["cat"].ToString();
		sb.Append("<tr><td> <a href=used.aspx?c=");
		sb.Append(HttpUtility.UrlEncode(cat));
		sb.Append(">");
		sb.Append(cat);
		sb.Append("</a></td></tr>");
	}
	sb.Append("</table>");
	return sb.ToString();
}

string MenuAddMain(string c)
{
	StringBuilder sb = new StringBuilder();

	string s = c;
	if(s == "zzzOthers")
		s = "All Others"; //display empty
//DEBUG("c =", c);
//DEBUG("s = ", HttpUtility.UrlEncode(c));
//	sb.Append("<td bgcolor=#EEEEEE width=10 height=20></td>");
//	sb.Append("<td class=d bgcolor=#EEEEEE width=10 height=20></td>");
//	sb.Append("\r\n");
	sb.Append("<td nowrap id='");
	sb.Append(HttpUtility.UrlEncode(s));
	sb.Append("' onmouseover='setMenu(\"");
	sb.Append(HttpUtility.UrlEncode(s));
	sb.Append("\", \"");
	sb.Append(HttpUtility.UrlEncode(s));
	sb.Append("Menu\")'>");
	sb.Append("<a href=m.aspx?m=");
	sb.Append(HttpUtility.UrlEncode(c));
	sb.Append(">&nbsp;&nbsp;");
//	sb.Append(" class=d> &nbsp;");
	sb.Append(s);
	sb.Append("</a></td>");
//	sb.Append("\r\n");
	
	return sb.ToString();	
}

string MenuAddSub(string c, string s)
{
	if(String.Compare(c, "brands", true) == 0)
		if(IsThisBrandShow(s) == false || s == "zzzOthers")
			return "";
	
	StringBuilder sb = new StringBuilder();

	sb.Append("<tr><td> <a href=c.aspx?r=" + DateTime.Now.ToOADate() + "@@ssid&");
//	sb.Append("<tr><td nowrap class=d>&nbsp;&nbsp;<a href=c.aspx?");
	if(c == "Brands")
	{
		sb.Append("b=");
		sb.Append(HttpUtility.UrlEncode(s));
	}
	else
	{
		sb.Append("c=");
		sb.Append(HttpUtility.UrlEncode(c));
		sb.Append("&s=");
		sb.Append(HttpUtility.UrlEncode(s));
	}
	sb.Append(">");
//	sb.Append(" class=d>");
	if(s == "zzzOthers")
		sb.Append("All Others");
	else
		sb.Append(s);
	sb.Append("</a></td></tr>");
//	sb.Append("\r\n");
	
	return sb.ToString();
}

string MenuAddSubSub(string c, string s, string ss)
{
	StringBuilder sb = new StringBuilder();

	sb.Append("<tr><td>&nbsp;<img src=rd.gif width=10></td><td>");
	sb.Append("<a href='c.aspx?r=" + DateTime.Now.ToOADate() + "@@ssid&c=");
	sb.Append(HttpUtility.UrlEncode(c));
	sb.Append("&s=");
	sb.Append(HttpUtility.UrlEncode(s));
	sb.Append("&ss=");
	sb.Append(HttpUtility.UrlEncode(ss));
//	sb.Append("' class=d>");
	sb.Append("'>");
	if(ss == "zzzOthers")
		sb.Append("All Others");
	else
		sb.Append(ss);
	sb.Append("</a></td></tr><tr><td>&nbsp;</td></tr>");
//	sb.Append("\r\n");

	return sb.ToString();
}

void PrintFooter()
{
	Response.Write(m_sFooter);
}

string GetTotalProducts()
{
	DataSet dscc = new DataSet();

	string sc = "SELECT count(*) AS count FROM product";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dscc, "sell");
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return "";
	}
	string sell = dscc.Tables["sell"].Rows[0]["count"].ToString();

	sc = "SELECT count(*) AS count FROM product_skip";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dscc, "skip");
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return "e";
	}
	string skip = dscc.Tables["skip"].Rows[0]["count"].ToString();

	int total = int.Parse(sell) + int.Parse(skip);
	return "<b>Total Items : " + total.ToString() + "</b>";
}

string GetRowTemplate(ref string tp, string sid)
{
	StringBuilder sb = new StringBuilder(); //for return
	StringBuilder sb1 = new StringBuilder();

	string begin = "<!-- BEGIN " + sid + " -->";
	string end = "<!-- END " + sid + " -->";
	int line = 0;
	string sline = "";
	bool bRead = ReadLine(tp, line, ref sline);
	bool bBegan = false;
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf(begin) >= 0)
		{
			bBegan = true;
			sb1.Append("@@template_" + sid);

			//skip this line
			line++; 
			bRead = ReadLine(tp, line, ref sline);
		}
		if(sline.IndexOf(end) >= 0)
			bBegan = false;
		else if(bBegan)
			sb.Append(sline);
		else
			sb1.Append(sline + "\r\n");
		line++;
		bRead = ReadLine(tp, line, ref sline);
	}
	tp = sb1.ToString(); //replace template with @@template_[sid]
	return sb.ToString();
}

//return false means no more lines
bool ReadLine(string s, int n, ref string sline)
{
	StringBuilder sb = new StringBuilder();
	int lines = 0;
	int i = 0;
	for(i=0; i<s.Length && lines <= n; i++)
	{
		if(s[i] == '\r')
			lines++;
		else if(s[i] == '\n')
			continue;

		if(n == lines)
			sb.Append(s[i]);
		if(lines > n)
			break;
	}
	sline = sb.ToString();

	if(sb.ToString() == "" && i == s.Length)
		return false;
	return true;
}
string BuildCatOptions(string sCurrent)
{
	DataSet dsCache = new DataSet();
	int nRows = 0;
	string sc = " SELECT DISTINCT cat AS v ";
	sc += " FROM catalog ";
	sc += " WHERE cat <> 'Brands' AND cat <> 'ServiceItem' ";
	sc += " ORDER BY v ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dsCache, "catop");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	string s = "";
	for(int i=0; i<nRows; i++)
	{
		string v = dsCache.Tables["catop"].Rows[i]["v"].ToString();
		s += "<option value='" + v + "'";
		if(v == sCurrent)
			s += " selected";
		s += ">" + v + "</option>";
	}
	return s;
}
string BuildSCatOptions(string cat, string sCurrent)
{
	DataSet dsCache = new DataSet();
	int nRows = 0;
	string sc = " SELECT DISTINCT s_cat AS v ";
	sc += " FROM catalog ";
	sc += " WHERE cat <> 'Brands' AND cat <> 'ServiceItem' ";
	if(cat != "")
		sc += " AND cat = N'" + EncodeQuote(cat) + "' ";
	sc += " ORDER BY v ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dsCache, "catop");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	string s = "";
	for(int i=0; i<nRows; i++)
	{
		string v = dsCache.Tables["catop"].Rows[i]["v"].ToString();
		s += "<option value='" + v + "'";
		if(v == sCurrent)
			s += " selected";
		s += ">" + v + "</option>";
	}
	return s;
}
/*string GetPageTopCart(string sout)
{
	if(m_sSite =="www" )
	{
		string cartItem = GetRowTemplate(ref sout, "itemrow");
		double dRowTotal = 0;
		double dOrderTotal = 0;
		double dQtyTotal = 0;
		string st = "";

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

			string si = cartItem;
			si = si.Replace("@@ITEM_NAME", dname.ToString());
			si = si.Replace("@@ITEM_QTY", dqty.ToString());
			si = si.Replace("@@ITEM_PRICE", dprice.ToString("c"));
			st += si;

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
		sout = sout.Replace("@@template_itemrow", st.ToString());
		sout = sout.Replace("@@QTY_TOTAL", dQtyTotal.ToString());
		sout = sout.Replace("@@ORDER_TOTAL", dOrderTotal.ToString("c"));
		sout = sout.Replace("@@ORDER_TAX", (dOrderTotal*0.15).ToString("c"));
		sout = sout.Replace("@@ORDER_AMOUNT", (dOrderTotal*1.15).ToString("c"));
		sout = sout.Replace("@@TIME_STAMP", DateTime.Now.ToOADate().ToString());
	}
	return sout;
}*/
Boolean PrintBodyHeader()
{
	string header = "<div class=\"shop_area shop_area_m_t\"><div class=\"container\">";
	Response.Write(header);
	return true;
}
Boolean PrintBodyFooter()
{
	string footer = "</div></div>";
	Response.Write(footer);
	return true;
}

</script>


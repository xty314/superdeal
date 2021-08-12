<!-- #include file="chart.cs" -->
<!-- #include file="s_item.cs" -->
<script runat=server>

bool m_bAdminMenu = false;
string m_sAdminFooter1 = "";
string code = "";
bool m_bIncludeGST = true;
int m_nDealerLevel = 0;
double sTotalQty =0;
DataSet ds = new DataSet();
string m_action = "";

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
//	TS_Init();
	InitializeData(); //init functions

	if(m_sSite == "admin" && Request.QueryString["t"] == "getdata")
	{
		if(DoGetItemDetails())
		{
			Response.Redirect("p.aspx?" + Request.QueryString[0]);
			return;
		}
	}
	if(Session["display_include_gst"].ToString() == "false")
		m_bIncludeGST = false;

	if(Request.QueryString["t"] == "b") //buy
	{
		DoBuy();
		return;
	}
	m_action = g("a");
	if(m_action == "gb")
	{
		if(Session["item_list_url"] != null)
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Session["item_list_url"].ToString() + "\">");
		else
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx\">");
		return;
	}

	RememberLastPage();
	PrintHeaderAndMenu();
	PrintBody();
	if(m_bAdminMenu)
		Response.Write(m_sAdminFooter1);
	else
		PrintFooter();
}

bool GetProductWithDetails()
{
	if(Request.QueryString.Count <= 0)
	{
		Response.Write("<h3>ERROR, NO PRODUCT CODE</h3>");
		return false;
	}
	code = Request.QueryString[0];

	if(!CheckSQLAttack(code))
		return false;

	if(!IsInteger(code))
	{
		Response.Write("<h3>ERROR, INVALID PRODUCT CODE</h3>");
		return false;
	}
/*	string sc = "SELECT p.name, p.supplier, p.supplier_code, p.price, p.supplier_price, d.highlight, ";
	sc += "d.manufacture, d.spec, d.pic, d.rev, d.warranty, c.clearance ";
	sc += ", c.level_rate1, c.level_rate2, c.qty_break1, c.qty_break2, c.qty_break3, c.qty_break4 ";
	sc += "FROM product p JOIN code_relations c on c.id=p.supplier+p.supplier_code ";
	sc += " LEFT JOIN product_details d On d.code = p.code ";
*/
	string sc = " SELECT c.*, d.*, p.eta ";
	sc += ", c.level_price0 * c.rate AS bottom_price, ISNULL(s.code, '-1') AS specials , st.qty";
	sc += " FROM code_relations c ";
	sc += " JOIN product p ON p.code = c.code ";
	sc += " LEFT OUTER JOIN product_details d On d.code = c.code ";
	sc += " LEFT OUTER JOIN specials s ON s.code = c.code ";
	/**********/
	sc += " LEFT OUTER JOIN stock_qty st ON st.code = c.code";
	/**********/
	
	sc += " WHERE c.code = " + code;
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		ds = new DataSet();
		if(myAdapter.Fill(ds) <= 0)
		{
			Response.Write("<h3>Product Not Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintBody()
{
	if(!GetProductWithDetails())
		return;

	DataRow dr = ds.Tables[0].Rows[0];

	//administrator menu
	if(m_bAdminMenu)
	{
		Response.Write("<table width=100% bgcolor=#EEEEEE>");
		Response.Write("<tr><td><img src=r.gif> ");
		Response.Write("<a href=addpic.aspx?code=");
		Response.Write(code);
		Response.Write("&name=");
		Response.Write(HttpUtility.UrlEncode(dr["name"].ToString()));
		Response.Write(" target=_blank>Upload Photo</a> ");
		
		Response.Write("<img src=r.gif> ");
		Response.Write("<a href=addpdf.aspx?code=");
		Response.Write(code);
		Response.Write("&name=");
		Response.Write(HttpUtility.UrlEncode(dr["name"].ToString()));
		Response.Write(" target=_blank>Upload PDF File</a> ");

		Response.Write("<img src=r.gif> <a href=ep.aspx?code=");
		Response.Write(code);
		Response.Write(" target=_blank>Edit Specifications</a> ");

		Response.Write("<img src=r.gif> <a href=liveedit.aspx?code=");
		Response.Write(code);
		Response.Write(" target=_blank>Edit Product Details</a> ");

		Response.Write("<img src=r.gif> <a href=p.aspx?code=" + code + "&t=getdata>Get Spec From Supplier</a>");
		Response.Write("</td></tr></table>\r\n");
	}
	//end of administrator menu

	string sImageLink = GetProductImgSrc(code);
	//string sPDFFile = GetProductPDFSrc(code);
    //needs to be replaced with something that checks if the pdf file exists...
    string supplier_code = dr["supplier_code"].ToString();
    string sPDFFile = "../pdf/" + code + ".pdf";
	string sHighLight = (dr["highlight"].ToString());
	string spec = ListText(dr["spec"].ToString());
	string manufacture = HashLink(dr["manufacture"].ToString());	
	string review = HashLink(dr["rev"].ToString());
	string warranty = HashLink(dr["warranty"].ToString());
	string brand = HashLink(dr["brand"].ToString());
	string barcode = HashLink(dr["barcode"].ToString());
	string moq = dr["moq"].ToString();
	string outer_pack = dr["weight"].ToString();
	string inner_pack = dr["inner_pack"].ToString();
	if(warranty == "")
		warranty = "<ul><li>12 months return to base</ul>";
	
	string dealer_level = "1";
	if(Session[m_sCompanyName + "dealer_level"] != null)
		dealer_level = Session[m_sCompanyName + "dealer_level"].ToString();
	double dlevel_rate = MyDoubleParse(dr["level_rate" + dealer_level].ToString());
	bool bClearance = bool.Parse(dr["clearance"].ToString());

	double dRRP = MyDoubleParse(dr["rrp"].ToString());
	double dBottomPrice = MyDoubleParse(dr["level_price0"].ToString()) * MyDoubleParse(dr["rate"].ToString()) + MyDoubleParse(dr["nzd_freight"].ToString());

	double dPriceYours = dBottomPrice;
	if(!bClearance)
		dPriceYours *= dlevel_rate;
	if(dRRP <= 0)
		dRRP = dPriceYours * MyDoubleParse(dr["level_rate1"].ToString());
	double dMargin = 0;
	if(dRRP > 0)
		dMargin = (dRRP - dPriceYours) / dPriceYours;

	//get level_one price (highest price level) for price history check, regardless clearance or not
//	double dPrice = Math.Round(dBottomPrice * MyDoubleParse(dr["level_rate1"].ToString()), 2); 
//	DEBUG(" care =", Session["card_type"].ToString());
	double dPrice = Math.Round(MyDoubleParse(dr["price1"].ToString()), 2); 

//DEBUG("dPrice =", dPrice.ToString());	
    string normal_price_show = "";
	if(dr["specials"].ToString() == "-1")
	{
		if(Session["card_id"] != null && Session["card_id"] != "")
		{
//DEBUG("cared =", Session["card_id"].ToString());
			DataRow drCard = GetCardData(Session["card_id"].ToString());
			if(drCard != null)
			{
				string sType = drCard["type"].ToString();
				if(int.Parse(GetSiteSettings("dealer_levels", "1")) > 0 && sType != "1") //type = dealer then disable fixed price
				//if(sType != "1" || Session["card_id"].ToString() != "0") //dealer level rate only
					dPrice = Math.Round(dBottomPrice * MyDoubleParse(dr["level_rate"+ Session[m_sCompanyName + "dealer_level"].ToString()].ToString())/100, 2); 
				else
					dPrice = dPrice / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
			}
		}
		else
			dPrice = dPrice / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
	}	
	else
	{		
		if(bIsSpecialItemForQPOS(code))
        {
            if(dr["special_price_end_date"].ToString() != null && dr["special_price_end_date"].ToString() != "")
            {
                DateTime check_time = DateTime.Now;
                DateTime special_end = DateTime.Parse(dr["special_price_end_date"].ToString());
                if(check_time <= special_end)
                {
                    dPrice = MyDoubleParse(dr["special_price"].ToString());
                    dPrice = dPrice / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
                    normal_price_show = "<font color=#FF0000><s>";
                    normal_price_show += MyDoubleParse(dr["price1"].ToString()).ToString("c");
                    normal_price_show += " was price</s></font><br>";
                }
                else
                    dPrice = dPrice / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
            }
            else
                dPrice = dPrice / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
        }
        //dPrice = Math.Round(MyDoubleParse(dr["special_price"].ToString()), 2); 
		
	}

	if(m_bIncludeGST)
		dPrice = dPrice * (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
		
	string p_h_s = CheckPriceHistoryChart(code, dPrice); //store bottom price
	string Left_side_menu = ReadSitePage("public_left_side_menu");
	string s = ReadSitePage("product_detail_page");

    string lbtn_in  = "<input type=image src='images/sign_in.gif' name=go id='login_btn'   />"; //<input type="image" src=""
	string login_name ="User Name: <input type='hidden' name='use_cookie' value='true' /><input   type='text' name='name' class='login_b'  autocomplete='false'/><input type='hidden' name='name_old' value=''>";
	string login_pass = "Password:  <input  name='pass' type='password'  class='login_b'  autocomplete='false'/>";
	string login_reg ="<a href='Register.aspx'>Register Now!</a>";
	string login_time = DateTime.Now.ToString("yyyy-MM-dd"); 
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
	
//DEBUG("dprice =", dPrice.ToString("c"));
	if(m_sSite == "www")
		s = ReadSitePage("product_detail_page_public");
    if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
	{
			s = s.Replace("@@login_a", "<span id='log_name'>"+Session["name"].ToString()+"</span><span> &nbsp; Welcome </span>");
			s = s.Replace("@@login_b", login_block);
			s = s.Replace("@@lbtn", "<span>"+login_time+"</span>");
			s = s.Replace("@@reg", ""); 
			s = s.Replace("@@111", login_top);
	}
	else
	{
		s = s.Replace("@@login_a",  login_name);
		s = s.Replace("@@login_b", login_pass);
		s = s.Replace("@@lbtn", lbtn_in);
		s = s.Replace("@@reg", login_reg);
		s = s.Replace("@@111", logout_top);
	}
	s = s.Replace("@@item_name", dr["name"].ToString());
	s = s.Replace("@@item_code", code);
	s = s.Replace("@@supplier_code", supplier_code);
	s = s.Replace("@@item_mpn", dr["supplier_code"].ToString());
	s = s.Replace("@@image_link", sImageLink);
	s = s.Replace("@@price_history_image_link", p_h_s);
	s = s.Replace("@@item_highlight", sHighLight);
	s = s.Replace("@@model", manufacture);
	s = s.Replace("@@BRAND", dr["brand"].ToString());
	s = s.Replace("@@item_spec", spec);
	s = s.Replace("@@barcode", barcode);
	s = s.Replace("@@item_review", review);
	s = s.Replace("@@item_warranty", warranty);
	s = s.Replace("@@pdf_link", sPDFFile);
	s = s.Replace("@@item_rrp", dRRP.ToString("c"));
	s = s.Replace("@@your_buy_price", dPrice.ToString("c"));
    s = s.Replace("@@normal_price", normal_price_show);		
	s = s.Replace("@@item_price", (dPrice*1.15).ToString("c"));	
	s = s.Replace("@@price_with_gst", (dPrice * (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()))).ToString("c"));	
	s = s.Replace("@@price_without_gst", (dPrice / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()))).ToString("c"));	
	s = s.Replace("@@item_margin", dMargin.ToString("p"));
	s = s.Replace("@@item_eta", dr["eta"].ToString());
	s = s.Replace("@@normal_price", MyDoubleParse(dr["normal_price"].ToString()).ToString("c"));
	s = s.Replace ("@@LEFT_SIDE_MENU" ,Left_side_menu);
	s = s.Replace("@@qty", dr["qty"].ToString());
	s = s.Replace("@@Inner_pack", inner_pack);
	s = s.Replace("@@outter_pack", outer_pack);
	s = s.Replace("@@MOQ", moq);
	if(Cache["item_categories"] != null)
		s = s.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	else
		s= s.Replace("@@HEADER_MENU_TOP_CAT", ""); 
		
	string stock_details = "";
	if(Session["branch_support"] != null)
		stock_details = GetStockDetails(code);
	else
		stock_details = "";
	//s = s.Replace("@@item_stock", ApplyColor(GetStockDetails(code)));
	s = s.Replace("@@item_stock", ApplyColor(stock_details));
	
	Response.Write(s);
/*
	Response.Write("<table width=100% align=center bgcolor=white>");
	Response.Write("<tr><td><table width=80% align=center>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
		Response.Write("<tr><td>");
		Response.Write("<table>");
			Response.Write("<tr><td width=30% valign=top>");

			//print pic
			Response.Write("<table height=100% width=10%>");
				Response.Write("<tr><td>");
				string sPicFile = GetProductImgSrc(code);
		//DEBUG("sPicFile=", sPicFile);
				Response.Write("<table cellpadding=0 cellspacing=0><tr><td align=center>");
				//Response.Write("<a href=");
				Response.Write("<a title='view Larger Image' href=\"javascript:image_window=window.open('");
				Response.Write(sPicFile);
				Response.Write("', 'image_window', 'width=450, height=500, scrollbars=no,resizable=yes '); image_window.focus()\" ");
				//Response.Write(sPicFile);
				Response.Write("><img src=");
				Response.Write(sPicFile);
			//	Response.Write(" title='Click For Large Image' border=0 ");
				string rp = Server.MapPath(sPicFile);
				int iWidth = 0;
				System.Drawing.Image im = null;
				if(File.Exists(rp))
				{
					try
					{
						im = System.Drawing.Image.FromFile(rp);
					}
					catch(Exception e)
					{
//						ShowExp("file : " + rp, e);
					}
				}
				if(im != null)
				{
					iWidth = im.Width;
					if(im.Width > 200)
						Response.Write(" width=200 title='Click For Large Image'");
					im.Dispose();
				}
				else
					Response.Write(" width=150");
				
				Response.Write(" border=0></a></td></tr><tr><td align=center>");
				//Response.Write("<a href=");
				Response.Write("<a title='view Larger Image' href=\"javascript:image_window=window.open('");
				Response.Write(sPicFile);
				Response.Write("', 'image_window', 'width=450, height=500, scrollbars=no,resizable=yes '); image_window.focus()\" ");
				//Response.Write(sPicFile);
				if(sPicFile.Length > 2)
				{
					if(sPicFile.Substring(0, 2) == "/i")
						Response.Write("><font size=+1 color=red><b>We are sorry, Image temporarily unavailable</b></font>");
					else
					{
						Response.Write(">");
						if(iWidth > 200)
							Response.Write("<font size=1 color=blue>Click For Large Image</font>");
					}
				}
				Response.Write("</a></td></tr>");
				Response.Write("</table>");
				Response.Write("</td></tr><tr valign=bottom><td height=200 valign=bottom>");
				//Response.Write("<a href=" + p_h_s + " class=d><img width=200 src=" + p_h_s + "><br>Price History</a></td></tr>");
				Response.Write("<a title='view Larger Image' href=\"javascript:image_window=window.open('");
				Response.Write(p_h_s);
				Response.Write("', 'image_window', 'width=450, height=500, scrollbars=no,resizable=yes '); image_window.focus()\" class=d>");
				Response.Write("<img width=200 src=" + p_h_s + "><br>Price History</a></td></tr>");
			Response.Write("</table>");

			
			//price
			Response.Write("</td><td>&nbsp;&nbsp;&nbsp;</td><td valign=top>");
			PrintPrice(dr, code);
			Response.Write("</td></tr>");
		Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr></td></tr>");

	//display pdf file
			string sPDFFile = GetProductPDFSrc(code);
		//DEBUG("sPdfFile=", sPDFFile);
				
			//	Response.Write(" title='Click For Large Image' border=0 ");
				string rpdf = Server.MapPath(sPDFFile);
				if(File.Exists(rpdf))
				{
					Response.Write("<tr><td colspan=2 valign=top>");
					Response.Write("<table cellpadding=0 cellspacing=0><tr><td align=center>");
					Response.Write("<tr><td><a title='download Acrobat Reader from Adobe' href='http://www.adobe.com/productindex/acrobat' ");
					Response.Write(" target=new class=o><img border=0 src='/i/pdf.gif'></a> View The product specification in PDF format.</td></tr>");
					Response.Write("<tr></tr><tr></tr>");
					Response.Write("<tr><td> <a title='Click to View by PDF Format' href=");
					Response.Write(sPDFFile);
					Response.Write("><img src='/i/download.gif' ");
					//Response.Write(sPDFFile);	
					Response.Write(" border=0></a></td></tr>");
					
					//Response.Write("<a href=");
					//Response.Write(sPicFile);
					Response.Write("</table>");
					Response.Write("</td></tr>");
					//Response.Write("</td></tr><tr valign=bottom><td height=200 valign=bottom>");
					//Response.Write("<a href=" + p_h_s + " class=d><img width=200 src=" + p_h_s + "><br>Price History</a></td></tr>");
				}

			//--------------end here
	//manufacture
	Response.Write("<tr><td>");
	string sManufacture = HashLink(dr["manufacture"].ToString());
	if(sManufacture != "")
	{
		Response.Write("<b>Manufacturer:</b><br>");
		Response.Write(sManufacture);
	}
	Response.Write("</td></tr>");
	//Response.Write("<tr><td>&nbsp;</td></tr>");

	//review
	Response.Write("<tr><td>");
	string sRev = HashLink(dr["rev"].ToString());
	if(sRev != "")
	{
		Response.Write("<b>Reviews:</b><br>");
		Response.Write(sRev);
	}
	Response.Write("</td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	//spec
	Response.Write("<tr><td>");
	string sSpec = ListText(dr["spec"].ToString());
	if(sSpec != "")
	{
		Response.Write("<b>Specifications:</b><br>");
		Response.Write(sSpec);
	}
	Response.Write("</td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	//warranty
	Response.Write("<tr><td>");
	string sWarranty = HashLink(dr["warranty"].ToString());
	if(sWarranty != "")
	{
		Response.Write("<b>Warranty:</b><br>");
		Response.Write(sWarranty);
	}
	else
	{
		Response.Write("<b>Warranty:</b><br>");
		Response.Write("<ul><li>12 months return to base</ul>");
	}
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td><br><b>Notice : </b><ul>");
	Response.Write("<li>Errors and omissions excepted.");
	Response.Write("<li>Photographs, pictures or graphics are indicative only.");
	Response.Write("<li>Specifications subject to change without notice.");
	Response.Write("<li>All prices exclude freight & GST and are subject to change without notice.");
	Response.Write("<li>Availabilty subject to change without notice.");
	Response.Write("</ul></td></tr>");

	Response.Write("</td></tr></table>");
	Response.Write("</table>");
*/
}

void PrintPrice(DataRow dr, string code)
{
	string dealer_level = Session[m_sCompanyName + "dealer_level"].ToString();
	double dlevel_rate = MyDoubleParse(dr["level_rate" + dealer_level].ToString());
	bool bClearance = bool.Parse(dr["clearance"].ToString());

	double dRRP = MyDoubleParse(dr["rrp"].ToString());
	double dBottomPrice = MyDoubleParse(dr["manual_cost_nzd"].ToString()) * MyDoubleParse(dr["rate"].ToString());

	double dPriceYours = dBottomPrice;
	if(!bClearance)
		dPriceYours *= dlevel_rate;

	double dMargin = 0;
	if(dRRP > 0)
		dMargin = (dRRP - dPriceYours)/dPriceYours;

	Response.Write("<table><tr><td colspan=2><font color=red><h3>");
	Response.Write(dr["name"].ToString());
	Response.Write("</h3></font></td></tr>\r\n<tr><td>&nbsp;</td></tr>\r\n<tr><td colspan=2>");
	Response.Write(ListText(dr["highlight"].ToString()));
	Response.Write("</td></tr>");
	
	Response.Write("<tr rowspan=3>");

	Response.Write("<td colspan=2>&nbsp;</td></tr>");
	
	Response.Write("<tr><td align=right><b>Item Code:</b></td><td><b>" + code + "</b>");
	Response.Write("</td></tr>");

	if(TS_UserLoggedIn())
	{
		Response.Write("<tr><td align=right><b>RRP:</b></td><td><b>");
		Response.Write(dRRP.ToString("c"));
		Response.Write("</b></td></tr>");

		Response.Write("<tr><td align=right><b>Your Buy Price</b></td><td><b>");
		Response.Write(dPriceYours.ToString("c"));
		if(bClearance)
			Response.Write("</b> <font color=red><b>(*Clearance*)</b></font>");
		Response.Write("</td></tr>");
		
//		Response.Write("<tr><td></td><td><br><b>Your Single Buy Price:&nbsp;<font color=blue>");
//		Response.Write(((dPriceYours*margin)).ToString("c")+ "</font></b></td></tr>");
		Response.Write("<tr><td align=right><b>Margin:&nbsp;<font color=blue></td><td><b>");
		Response.Write(dMargin.ToString("p"));
		Response.Write("</b></td></tr>");
//		Response.Write("<tr><td></td><td><font color=blue><i>(Further discount applies for quantity)</i></font></td></tr>");
	}
				
	Response.Write("<tr><td colspan=2 align=center>");

	//forum
	if(Session["loggedin"] != null)
	{
		Response.Write("<table>");
		Response.Write("<tr><td><font size=+1 color=red><b>&nbsp&nbsp;Any Question?&nbsp;&nbsp;</b></font></td><td>");
		Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('bb.aspx?t=nt&fi=1&q=1&dp="+ HttpUtility.UrlEncode(dr["name"].ToString())+"&cd="+ code +"') value='&nbsp&nbsp; Ask Us &nbsp;'>");
		Response.Write("</td></tr>");
		Response.Write("</table>");
	}

//	if(m_sSite == "admin")
//		Response.Write("<br><input type=button value='Get Details From Supplier Site' onclick=window.location=('p.aspx?code=" + code + "&t=getdata') " + Session["button_style"] + ">");

	Response.Write("</td></tr>");
//	}
//	Response.Write("</table>\r\n");
//	Response.Write("<button onclick=window.location=('watch.aspx?t=price&code=" + code + "')>Stock and Price Watch</button>");
}

string HashLink(string s)
{
	int len = s.Length;

	StringBuilder sb = new StringBuilder();
	StringBuilder sl = new StringBuilder();
	
	Boolean bLink = false;
	bool bAddHttp = false;
	int i = 0;
	for(i=0; i<len; i++)
	{
		if(s[i] == 'h' || s[i] == 'H')
			if(i+6 < len)
				if(s[i+1] == 't' || s[i+1] == 'T')
					if(s[i+2] == 't' || s[i+2] == 'T')
						if(s[i+3] == 'p' || s[i+3] == 'P')
							if(s[i+4] == ':' && s[i+5] == '/' && s[i+6] == '/')
								bLink = true;
		
		if(!bLink)
		{
			if(s[i] == 'w' || s[i] == 'W')
				if(i+3 < len)
					if(s[i+1] == 'w' || s[i+1] == 'W')
						if(s[i+2] == 'w' || s[i+2] == 'W')
							if(s[i+3] == '.')
							{
								bLink = true;
								bAddHttp = true;
							}
		}

//		bLink = false;
		if(!bLink)
		{
			sb.Append(s[i]);
		}
		else
		{
			if(i+1 == len || s[i] == ' ')
			{
				if(i+1 == len) //the last character
					sl.Append(s[i]);

				sb.Append("<a href=");
				if(bAddHttp)
				{
					sb.Append("http://");
					bAddHttp = false;
				}
				sb.Append(sl.ToString());
				sb.Append(" class=o target=_blank>");
				sb.Append(sl.ToString());
				sb.Append("</a>");
				bLink = false;
				sl.Remove(0, sl.Length);
			}
			else
				sl.Append(s[i]);
		}
	}
	return sb.ToString();
}

string ListText(string s)
{
	CompareInfo ci = CompareInfo.GetCompareInfo(1);
	bool bHasTable = (ci.IndexOf(s, "<table", CompareOptions.IgnoreCase) >= 0);
	if(!bHasTable)
		bHasTable = (ci.IndexOf(s, "<td>", CompareOptions.IgnoreCase) >= 0);

	if(bHasTable)
		return s;

	string sRet = "<ul><li>";
	for(int i=0; i<s.Length; i++)
	{
		if(i<s.Length-4 && s[i] == '\r' && s[i+1] == '\n')
		{
			if(i>2 && (s[i-2] == '\r' && s[i-1] == '\n') || (s[i+2] == '\r' && s[i+3] == '\n'))
				sRet += "<br>";
			else
				sRet += "<li>";
		}
		else
			sRet += s[i];
	}
	sRet += "</ul>"; 

	return sRet; 
}

string CheckPriceHistoryChart(string code, double level_one_price)
{
	DateTime dNow = DateTime.Now;
	DataSet ds;
	int rows = 0;

	string filename = code + ".jpg";
	filename = RemoveSlash(filename);
	string vpath = GetRootPath() + "/ph/";
	string fn = Server.MapPath(vpath);
	fn += filename; 

	double dPrice = level_one_price;
	string sc = "IF NOT EXISTS (SELECT price FROM price_history WHERE code=" + code + ") ";
	sc += " INSERT INTO price_history (code, price, price_date) VALUES(" + code;
	sc += ", " + dPrice + ", GETDATE() )";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return vpath + filename;
	}
	
	sc = "SELECT h.price * c.level_rate1 AS price, h.price_date ";
	sc += " FROM price_history h JOIN code_relations c ON c.code=h.code ";
	sc += " WHERE h.code='";
	sc += code + "' AND h.price_date>";
	sc += "DATEADD(Year, -1, GETDATE())"; //dNow.AddYears(-1);
	sc += " ORDER BY h.price_date DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		ds = new DataSet();
		rows = myAdapter.Fill(ds);
		if(rows <= 0)
			return vpath + filename;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	DateTime dDate = DateTime.Parse(ds.Tables[0].Rows[rows-1]["price_date"].ToString());
	// build the new chart
	dPrice = 0;
	double dPriceL = 99999999; //lowest price
	double dPriceH = -1; //highest price
	DataRow dr = null;

	//get price margin first
	for(int i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		dr = ds.Tables[0].Rows[i];
		dPrice = double.Parse(dr["price"].ToString());
		if(dPrice < dPriceL)
			dPriceL = dPrice;
		if(dPrice > dPriceH)
			dPriceH = dPrice;
	}
//DEBUG("dPriceL=", (int)dPriceL);
//DEBUG("dPriceH=", (int)dPriceH);

	//build chart
	int nYear = dNow.Year - dDate.Year;

	LineChart c = new LineChart(500, 300, Page);
	c.Title="Price History - Last 12 Months";
	c.SetBgColor(0xFFCCCC);
//	c.SetBgColor(0xFFFFFF);
	c.ScaleX=400; 
	c.Xdivs=12;
	DateTime dOrigin = dNow.AddYears(-1);	//backward one year
	dOrigin = dOrigin.AddDays(0 - dNow.Day);			//back to the beginning of the month
//	c.Xorigin = d.DayOfYear;				
	c.Xorigin = 0;

	int nStep = (int)((dPriceH - dPriceL) / 10);
	if(nStep < 1)
		nStep = 1;
	c.Yorigin = (int)(dPriceL / nStep) * nStep - nStep;
	c.ScaleY = (int)(dPriceH / nStep) * nStep + nStep - c.Yorigin;
	c.Ydivs = c.ScaleY / nStep;
//DEBUG("Yorigin=", (int)c.Yorigin);
//DEBUG("ScaleY=", (int)c.ScaleY);
//DEBUG("nStep=", nStep);
//DEBUG("Ydivs=", (int)c.Ydivs);
	DateTime dDatePrev = DateTime.Parse("01/01/1900");
	double dPricePrev = -1;
//	DataRow[] drsbp = ds.Tables[0].Select("", "price"); //sort by price
	DataRow[] drsbp = ds.Tables[0].Select("", "price_date"); //sort by price_date
	for(int i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		dr = drsbp[i];
		dDate = DateTime.Parse(dr["price_date"].ToString());
		if((dDate - dDatePrev).Days <= 1)
			continue; //skip rapid changes
		dDatePrev = dDate;
		dPrice = double.Parse(dr["price"].ToString());
//DEBUG("price=", dPrice.ToString());
		if(dPrice == dPricePrev)
			continue;
//DEBUG("price=", dPrice.ToString());
		dPricePrev = dPrice;
		nYear = dNow.Year - dDate.Year;
		c.AddValue((dDate - dOrigin).Days, (int)(dPrice));
//DEBUG("days=", (dDate-dOrigin).Days);
//DEBUG("y=", (int)dPrice);
	}
	c.AddValue((dNow - dOrigin).Days, (int)(dPrice));
	c.Draw();
//DEBUG("filename=", fn);
	c.Save(fn);
	return vpath + filename;
}

void DoBuy()
{
	if(!GetProductWithDetails())
		return;

	string sDiscount = "0";
	double dDiscount = 0;
	if(Session["loggedin"] != null)
	{
		if(Session[m_sCompanyName + "discount"] != null)
		{
			sDiscount = Session[m_sCompanyName + "discount"].ToString();
			dDiscount = double.Parse(sDiscount);
			sDiscount = ((int)dDiscount).ToString();
		}
	}

	DataRow dr = ds.Tables[0].Rows[0];
	string price = dr["price"].ToString(); 
	double dPrice = double.Parse(price);
	string supplier_price = dr["supplier_price"].ToString(); 
	double dsupplier_price = double.Parse(supplier_price);
	double cost = dsupplier_price * 0.04; //Bank charge 2.5 and DPS charg 1;
	double dProfit = dPrice - dsupplier_price - cost;
	double dPriceYours = dPrice - dProfit * dDiscount / 100;
	double dPriceCreditCard = 0;

	bool bDealer = false;
	if(Session["loggedin"] != null)
	{
		if(Session["card_type"].ToString() == GetEnumID("card_type", "dealer"))
		{
			bDealer = true;
			dPriceCreditCard = dPriceYours;
			dProfit = dPrice - dsupplier_price;
			dPriceYours = dPrice - dProfit * dDiscount / 100;
		}
	}
	
	bool bCreditCard = false;
	double dFinalPrice = dPriceCreditCard;
	if(Request.QueryString["card"] != "1")
	{
		dFinalPrice = dPriceYours;
		Session[m_sCompanyName + "no_credit_card"] = true;
	}
	
	Session["bargain_price_" + code] = dFinalPrice;
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?t=b&c=" + code + "&r=" + DateTime.Now.ToOADate() + "\">");
}

double GetBottomLevelDiscount()
{
	DataSet dsld = new DataSet();

	double dd = 2;
	int rows = 0;
	string sc = "SELECT MAX(data1) AS rate FROM discount WHERE factor='level'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsld, "ld") == 1)
			dd = MyDoubleParse(dsld.Tables["ld"].Rows[0]["rate"].ToString());
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}
	return dd;
}

bool DoGetItemDetails()
{
//	return false;

	bool bRet = true;

	if(!GetProductWithDetails())
		return false;

	DataRow dr = ds.Tables[0].Rows[0];
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();

	string serviceUrl = "";
	if(supplier.ToLower() == "dw")
		serviceUrl = "http://www.datawellonline.co.nz/service/item.asmx";
	else if(supplier.ToLower() == "tm")
		serviceUrl = "http://www.timesonline.co.nz/service/item.asmx";
	else if(supplier.ToLower() == "iw")
		serviceUrl = "http://210.55.223.208/service/item.asmx";
	else if(supplier.ToLower() == "edit")
		serviceUrl = "http://www.ed-it.co.nz/service/item.asmx";

	if(serviceUrl == "")
		return true;
	csItem ItemService = new csItem(serviceUrl);

	DataSet ds1 = ItemService.GetItemDetail(supplier_code);
	if(ds1 != null)
	{
		dr = ds1.Tables[0].Rows[0];
		string sc = "";
		sc = " IF NOT EXISTS (SELECT code FROM product_details WHERE code = " + code + ") "; 
		sc += " BEGIN ";
		sc += " INSERT INTO product_details (code, highlight, spec, manufacture, rev, warranty) ";
		sc += " VALUES( " + code;
		sc += ", '" + EncodeQuote(dr["highlight"].ToString()) + "' ";
		sc += ", '" +  EncodeQuote(dr["spec"].ToString()) + "' ";
		sc += ", '" + EncodeQuote(dr["manufacture"].ToString()) + "' ";
		sc += ", '" + EncodeQuote(dr["rev"].ToString()) + "' ";
		sc += ", '" + EncodeQuote(dr["warranty"].ToString()) + "' ";
		sc += ") ";
		sc += " END ";
		sc += " ELSE ";
		sc += " BEGIN ";
		sc += " UPDATE product_details SET ";
		sc += " highlight = '" + EncodeQuote(dr["highlight"].ToString()) + "' ";
		sc += ", spec = '" + EncodeQuote(dr["spec"].ToString()) + "' ";
		sc += ", manufacture = '" + EncodeQuote(dr["manufacture"].ToString()) + "' ";
		sc += ", rev = '" + EncodeQuote(dr["rev"].ToString()) + "' ";
		sc += ", warranty = '" + EncodeQuote(dr["warranty"].ToString()) + "' ";
		sc += " WHERE code = " + code;
		sc += " END ";
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	else
	{
		Response.Write("<br><center><h3>Error getting product details from " + supplier + ", " + serviceUrl + ", MPN: " + supplier_code + "</h3>");	
		bRet = false;
	}

	string fileType = ItemService.GetItemPhotoType(supplier_code);
	if(fileType == null || fileType == "")
		return false;
	byte[] buffer = ItemService.GetItemPhotoData(supplier_code + "." + fileType);
	if(buffer == null)
		return false;

//	string strPath = Server.MapPath("/"+ m_sCompanyName +"/pi/" + code + "." + fileType);
	string strPath = Server.MapPath("/pi/" + code + "." + fileType);
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(buffer, 0, buffer.Length);
	// Close file
	newFile.Close();
	return bRet;


}

string GetStockDetails(string code)
{
	string sStockCT = GetSiteSettings("show_stock_branch_idz", "1");
	
	DataSet dssd = new DataSet();
	string sc = " ";
	if(sStockCT != "" && sStockCT != null)
	{
		sc += " SELECT DISTINCT SUM(q.qty) as qty, SUM(q.allocated_stock) AS allocated_stock, b.name ";
		sc += " FROM stock_qty q JOIN branch b ON b.id = q.branch_id ";
		sc += " WHERE q.code=" + code;		
		sc += " AND q.branch_id in ("+ sStockCT +") ";
		sc += " GROUP BY b.name ";
	}
	else
	{
		sc += " SELECT name, ISNULL( (SELECT SUM(qty) FROM stock_qty WHERE branch_id = b.id), 0) AS qty ";
		sc += " , ISNULL( (SELECT SUM(allocated_stock) FROM stock_qty WHERE branch_id = b.id), 0) AS allocated_stock ";
		sc += " FROM branch b WHERE b.id = 1 ";
	}
	
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dssd, "stock") <= 0)
		{
			return "Error getting stock details";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	StringBuilder sb = new StringBuilder();
	string public_stock_detail_full= GetSiteSettings("public_stock_detail_full"); // Colin
//DEBUG("View branch stock", 	view_branch_stock);
    if(public_stock_detail_full =="1"){
	sb.Append("<table border=0 width=100%>");
	sb.Append("<tr bgcolor=@@color_9><td><b>Stock On Hand</b></td><td><b>Commited</b></td><td><b>Available</b></td></tr>");
	for(int i=0; i<dssd.Tables["stock"].Rows.Count; i++)
	{
		DataRow dr = dssd.Tables["stock"].Rows[i];
		string branch_name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		string allocated = dr["allocated_stock"].ToString();
		int nQty = MyIntParse(qty);
		int nAllocated = MyIntParse(allocated);

		sb.Append("<tr>");
		sb.Append("<td>" + qty + "(" + branch_name + ")</td>");
		sb.Append("<td>" + allocated + "(" + branch_name + ")</td>");
		sb.Append("<td>" + (nQty-nAllocated).ToString() + "(" + branch_name + ")</td>");
		sb.Append("</tr>");
	}
	}else{   // IF VIEW BRANCH STOCK IS FALSE, COUNT TOTAL STOCK IN DATABASE
	for(int i=0; i<dssd.Tables["stock"].Rows.Count; i++)
	{
		DataRow dr = dssd.Tables["stock"].Rows[i];
		string branch_name = dr["name"].ToString();
		string SaidYesOrNo = GetSiteSettings("stock_say_yes_no");
		string SaidYes = GetSiteSettings("stock_yes_string");
		string SaidNo = GetSiteSettings("stock_no_string");
		string qty = dr["qty"].ToString();
		string allocated = dr["allocated_stock"].ToString();
		int nQty = MyIntParse(qty);
		int nAllocated = MyIntParse(allocated);
		double CheckQty = double.Parse(qty);
		 if(CheckQty >= 1){
               if(SaidYesOrNo =="1")
			     qty = SaidYes;
				 else
				   qty = qty ;
			}else {
			   if(SaidYesOrNo =="1")
			     qty = SaidNo;
				 else
				   qty = qty ;
				 }
			   
			     
		    
		sb.Append(qty);
	}
	}
	return sb.ToString();
}

</script>

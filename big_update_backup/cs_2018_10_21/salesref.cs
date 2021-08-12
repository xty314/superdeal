<script runat=server>

int page = 1;
const int m_nPageSize = 15; //how many rows in oen page

DataSet dsc = new DataSet();
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_kw = "";
string m_ckw = "";
string m_code = "";
string m_cardID = "";
int m_nSearchReturn = 0;
double m_viewEditProduct = 0;
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
	{
		m_code = Request.QueryString["code"];
//		Session["salesref_code"] = m_code;
	}
	else if(Session["salesref_code"] != null)
	{
//		m_code = Session["salesref_code"].ToString();
	}

	if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
	{
		m_cardID = Request.QueryString["cid"];
		GetCustomerDetails();
//		Session["salesref_card"] = m_cardID;
	}
	else if(Session["salesref_card"] != null)
	{
//		m_cardID = Session["salesref_card"].ToString();
	}

	if(m_code != "")
	{
		GetItemDetails();
	}

	if(m_code != "" || m_cardID != "")
	{
		if(!DoSearch())
			return;
	}
	string s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];

	//item search
	if(Request.Form["customer_search"] != null && Request.Form["customer_search"] != "")
	{
		m_ckw = Request.Form["customer_search"];
		Session["salesref_ckw"] = m_ckw;
		DoCustomerSearchAndList();
		return;
	}
	else if(Request.Form["item_search"] != null && Request.Form["item_search"] != "")
	{
		m_kw = Request.Form["item_search"];
		Session["salesref_kw"] = m_kw;

		string s_SearchMsg = "";	
		bool bRet = DoBarcodeSearch(Request.Form["item_search"]);
		if(bRet)
		{
			//found product by barcode;
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=salesref.aspx?code=" + m_code + "\">");	
			return;
		}
		else
		{	
			if(IsInteger(Request.Form["item_search"]))
			{
				//true - means find exactly, false - mean find similar;
				if(!DoSearchItem(Request.Form["item_search"], true))
				{
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
					return;
				}
				else
					s_url = "salesref.aspx?code=" + m_code;
			}
			if(m_nSearchReturn <= 0)
			{
				if(!DoSearchItem(Request.Form["item_search"], false))	
				{
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
					return;
				}
				
				if(m_nSearchReturn <=0 )
				{
					Response.Write("<br><br><center><h3>No Item's S/N, code or description matches <b>" + Request.Form["supplier_search"] + "</b></h3>");
					Response.Write("<input type=button " + Session["button_style"] + " value=Back onclick=window.location=('salesref.aspx')>");
					return;
				}
				LFooter.Text = m_sAdminFooter;
				return;
			}
		}

		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
		return;
	}
	
	MyDrawTable();
	LFooter.Text = m_sAdminFooter;
}

bool DoBarcodeSearch(string barcode)
{
	string s_msgSN = "";
//	string sc = "SELECT sn, status, product_code, prod_desc, supplier_code, supplier, cost ";
//	       sc+= "FROM stock WHERE sn = '" + s_SN + "'";
	string sc = " SELECT code FROM code_relations WHERE barcode = '" + EncodeQuote(barcode) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nSearchReturn = myAdapter.Fill(dst, "prod_sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}

	if(m_nSearchReturn > 0)
	{
		DataRow dr = dst.Tables["prod_sn"].Rows[0];
		m_code = dr["code"].ToString();
		return true;
	}

	return false; 
}

bool DoSearchItem(string kw, bool btype)
{
	string sc = "SELECT code, supplier, supplier_code, name, supplier_price ";
	sc += " FROM code_relations WHERE ";// LIKE '"+ kw;
	if(!btype)
	{
		sc += " name LIKE '%" + kw + "%'";
	}
	else
	{
		sc += " supplier_code = '" + kw + "' ";;
	}
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nSearchReturn = myAdapter.Fill(dst, "isearch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(m_nSearchReturn == 1)
	{
		DataRow dr = dst.Tables["isearch"].Rows[0];
		m_code = dr["code"].ToString();
	}
	else if(m_nSearchReturn > 1)
	{
		Response.Write("<center><h3>Search Result For <font color=red>" + m_kw + "</h3></center>");
		if(m_nSearchReturn >= 100)
		{
			Response.Write("Top 100 rows returned, Display 1-100");
			m_nSearchReturn = 100;
		}
		else
			Response.Write("top " +(m_nSearchReturn).ToString()+ " rows returned, display 1-" + (m_nSearchReturn).ToString());
		BindISTable();
	}
	return true;
}

bool GetItemDetails()
{
	string sc = " SELECT c.*, p.price, p.stock, p.allocated_stock ";
	//sc += ", ROUND((c.manual_cost_frd / c.manual_exchange_rate),2) * rate + nzd_freight AS bottom_price ";
	//sc += ", ROUND(c.manual_cost_nzd * rate,2) + nzd_freight AS bottom_price ";
    sc += ", c.level_price0/100 AS bottom_price ";
	sc += " FROM code_relations c JOIN product p ON p.code = c.code ";
	sc += " WHERE c.code = " + m_code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "item");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetCustomerDetails()
{
	string sc = " SELECT c.*, c1.name AS sales_name ";
	sc += " FROM card c LEFT OUTER JOIN card c1 ON c1.id = c.sales ";
	sc += " WHERE c.id = " + m_cardID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "customer");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoSearch()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT c.code, c.supplier_code, c.name AS product_name, c.supplier_code, c.name ";
	sc += ", c.supplier_price, p.price, c.level_rate1 ";
	sc += ", s.commit_price, s.quantity, i.invoice_number, i.system, i.commit_date, c2.name AS sales, i.invoice_number ";
	sc += ", c1.id AS card_id, c1.trading_name, c1.company, c1.name AS customer_name, c1.dealer_level ";
	sc += " FROM code_relations c JOIN sales s ON c.code = s.code ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " LEFT OUTER JOIN orders o ON o.invoice_number = i.invoice_number ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = o.sales ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = i.card_id ";
	sc += " LEFT OUTER JOIN product p ON p.code = c.code ";
	if(m_code != "")
	{
		sc += " WHERE c.code = " + m_code;
		if(m_cardID != "")
			sc += " AND i.card_id = " + m_cardID;
	}
	else if(m_cardID != "")
	{
		sc += " WHERE i.card_id = " + m_cardID;
		if(m_code != "")
			sc += " AND c.code = " + m_code;
	}

	sc += " ORDER BY i.commit_date DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "sales");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool MyDrawTable()
{
	bool bQAdded = false;
	string bgcolor = "lightblue";//GetSiteSettings("table_row_bgcolor", "#666696");
	Response.Write("<br><center><h3>Sales Reference</h3></center>");
	Response.Write("<form name=f action=salesref.aspx");
	if(m_code != "")
	{
		Response.Write("?code=" + m_code);
		bQAdded = true;
	}
	if(m_cardID != "")
	{
		if(bQAdded)
			Response.Write("&");
		else
			Response.Write("?");
		Response.Write("cid=" + m_cardID);
	}

	Response.Write(" method=post>");
	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	if(dst.Tables["item"] != null && dst.Tables["item"].Rows.Count > 0)
	{
		Response.Write("<tr><td>");
		PrintItemDetails();
		Response.Write("</td></tr>");
	}
	
	if(dst.Tables["customer"] != null && dst.Tables["customer"].Rows.Count > 0)
	{
		Response.Write("<tr><td>");
		PrintCustomerDetails();
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td>");

	Response.Write("<table>");
	Response.Write("<tr><td><b>Item : </b></td>");
	Response.Write("<td><input type=text name=item_search size=10 value=\"" + m_kw + "\">");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.item_search.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("</td><td>");
	Response.Write("<input type=submit name=cmd " + Session["button_style"] + " value='Search'>");
	if(m_code != "" && m_cardID != "")
		Response.Write("<input type=button onclick=window.location=('salesref.aspx?cid=" + m_cardID + "') value='Show All Item' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Customer : </b></td>");
	Response.Write("<td><input type=text name=customer_search size=10 value=\"" + m_ckw + "\">");

	Response.Write("</td><td>");
	Response.Write("<input type=submit name=cmd " + Session["button_style"] + " value='Search'>");
	if(m_code != "" && m_cardID != "")
		Response.Write("<input type=button onclick=window.location=('salesref.aspx?code=" + m_code + "') value='Show All Customer' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</td></tr>");

	if(dst.Tables["sales"] == null || dst.Tables["sales"].Rows.Count <= 0)
	{
		if(m_code != "" || m_cardID != "")
			Response.Write("<tr><td><h5>&nbsp;<br><font color=red>No Sales Record. </font></h5></td></tr></table>");
		return true;
	}

	Response.Write("<tr><td>");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:" + bgcolor + ";font-weight:bold;\">");
	Response.Write("<th nowrap>Customer </th>");
	Response.Write("<th> Code </th>");
	Response.Write("<th> M_PN </th>");
	Response.Write("<th> Item </th>");
	Response.Write("<th> Date </th>");
	Response.Write("<th> Inv# </th>");
	Response.Write("<th> Qty </th>");
	Response.Write("<th> Price </th>");
	Response.Write("<th> Sales </th>");
	Response.Write("<th> System </th>");
	Response.Write("</tr>");	

	bool bAlterColor = false;
	for(int i=0; i<dst.Tables["sales"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["sales"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string date = dr["commit_date"].ToString();
		string customer_id = dr["card_id"].ToString();
		string commit_price = dr["commit_price"].ToString();
		string item = dr["product_name"].ToString();
		if(item.Length > 70)
			item = item.Substring(0, 70);
		string item_code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string qty = dr["quantity"].ToString();
		string system = "No";
		if(dr["system"].ToString() != "" && MyBooleanParse(dr["system"].ToString()))
			system = "<font color=red>Yes</font>";
		string sales = dr["sales"].ToString();
		string card_id = dr["card_id"].ToString();
		string customer = dr["trading_name"].ToString();
		if(customer == "")
			customer = dr["trading_name"].ToString();
		if(customer == "")
			customer = dr["company"].ToString();
		if(customer == "")
		{
			if(customer_id != "")
				customer = "ACC# " + customer_id;
		}

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");

		bAlterColor = !bAlterColor;
		
		string sdate = "";
		if(date != "")
			sdate = DateTime.Parse(date).ToString("dd-MM-yy");

		string shortname = customer;
		if(customer.Length > 10)
			shortname = customer.Substring(0, 10);
		Response.Write("<td nowrap title='" + customer + "'><a href=salesref.aspx?code=" + m_code + "&cid=" + card_id + ">" + shortname + "</a></td>");
		Response.Write("<td nowrap>&nbsp;" + item_code + "&nbsp;</td>");
		Response.Write("<td nowrap>" + supplier_code + "</td>");
		Response.Write("<td nowrap><a href=salesref.aspx?cid=" + m_cardID + "&code=" + item_code + ">" + item + "</a></td>");
		Response.Write("<td nowrap>" + sdate + "</td>");
		Response.Write("<td nowrap>&nbsp;<a href=invoice.aspx?id=" + invoice_number + " class=o>" + invoice_number + "</a>&nbsp;</td>");
		Response.Write("<td nowrap align=center>" + qty + "</td>");
		string s = MyDoubleParse(commit_price).ToString("c");
		s = s.Substring(1, s.Length-1);
		Response.Write("<td nowrap align=right>" + s + "</td>");
		Response.Write("<td nowrap align=right>" + sales + "</td>");
		Response.Write("<td nowrap align=right>" + system + "</td>");

		Response.Write("</tr>");
	}
	Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

void PrintItemDetails()
{
	DataRow dr = dst.Tables["item"].Rows[0];
	string code = dr["code"].ToString();
	string name = dr["name"].ToString();
	string supplier_price = dr["supplier_price"].ToString();
	string price = dr["price"].ToString();

	string bottom_price = dr["bottom_price"].ToString();

	string stock = dr["stock"].ToString();
	string allocated = dr["allocated_stock"].ToString();

	double dRRP = MyDoubleParse(dr["rrp"].ToString());

	double dPrice = 0;

	if(price != "")
	{
		dPrice = MyDoubleParse(price);
		dPrice = MyDoubleParse(bottom_price);
	}
	else
		stock = "<font size=+1 color=red><b>Discontinued</b></font>";
	double dSupplierPrice = MyDoubleParse(supplier_price);

	double[] lr = new double[10];
	int[] qb = new int[10];
	double[] qbd = new double[10];
	
	int levels = MyIntParse(GetSiteSettings("dealer_levels", "6"));
	int qbs = MyIntParse(GetSiteSettings("quantity_breaks", "3"));


	int i = 1;
	for(i=1; i<=levels; i++)
	{
		string slr = dr["level_rate" + i].ToString();
		if(slr != "")
			lr[i] = MyDoubleParse(slr);
		else
			lr[i] = 0;
	}

	double dMaxQbd = 0;
	for(i=1; i<=qbs; i++)
	{
		string sqb = dr["qty_break" + i].ToString();
		if(sqb != "")
			qb[i] = MyIntParse(sqb);
		else
			qb[i] = 0;

		string sqbd = dr["qty_break_discount" + i].ToString();
		if(sqbd != "")
		{
			double dd = MyDoubleParse(sqbd);
			if(dd > dMaxQbd)
				dMaxQbd = dd;
			qbd[i] = dd;
		}
		else
			qbd[i] = 0;
	}

	Response.Write("<table><tr><td width=550>");
	Response.Write("<font size=+1>" + name + "</font><br>");
	Response.Write("Supplier Price : <b>" + dSupplierPrice.ToString("c") + "</b>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("Bottom Price : <font color=red><b>" + dPrice.ToString("c") + "</b></font>&nbsp&nbsp&nbsp&nbsp;");
//	Response.Write("Bottom Price : <font color=red><b>" + Math.Round(dPrice * (1 - dMaxQbd/100), 2).ToString("c") + "</b></font>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("RRP : <font color=blue><b>" + dRRP.ToString("c") + "</b></font><br>");
	Response.Write("Stock : <b>" + stock + "&nbsp&nbsp&nbsp&nbsp;" + "</b>Allocated : <b>" + allocated + "</b>&nbsp&nbsp&nbsp&nbsp;");
	   m_viewEditProduct = MyDoubleParse(GetSiteSettings("allow_edit_product_sales_ref"));
    
     if(m_viewEditProduct < MyIntParse(Session[m_sCompanyName + "AccessLevel"].ToString()))
	Response.Write("<a href=liveedit.aspx?code=" + m_code + " class=o>Edit Product</a><br>");
	Response.Write("</td></tr></table>");

	Response.Write("<table valign=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=9><b>Price List</b></td></tr>");

	Response.Write("<tr style=\"color:white;background-color:lightblue;font-weight:bold;\">\r\n");
	Response.Write("<td><b> &nbsp; Quantity &nbsp; </b></td>");

	for(i=1; i<=levels; i++)
    {
        string lname = GetEnumValue("dealer_level_name", i.ToString());
		Response.Write("<th><b>&nbsp;" +  Lang(lname)  + "&nbsp;</b></th>");
    }
	Response.Write("</tr>");
	
	//one piece price
	Response.Write("<tr>");
	Response.Write("<td bgcolor=#AAAAAA align=center><font color=white><b> 1 </b></font></td>");
	for(i=1; i<=levels; i++)
	{
		Response.Write("<td>" + Math.Round(dPrice * lr[i], 2).ToString("c") + "</td>");
	}
	Response.Write("</tr>");

	bool bFixLevel6 = MyBooleanParse(GetSiteSettings("level_6_no_qty_discount", "0"));
	for(int j=1; j<=qbs; j++)
	{
		Response.Write("<tr>");
		Response.Write("<td bgcolor=#AAAAAA align=center><font color=white><b>" + qb[j].ToString() + "</b></font></td>");
		for(i=1; i<=levels; i++)
		{
			double dDiscountRate = (1 - qbd[j]/100);
			if(bFixLevel6 && i == 6)
				dDiscountRate = 1;
			Response.Write("<td>" + Math.Round(dPrice * lr[i] * dDiscountRate, 2).ToString("c") + "</td>");
		}
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

void PrintCustomerDetails()
{
	DataRow dr = dst.Tables["customer"].Rows[0];
	string trading_name = dr["trading_name"].ToString();
	string name = dr["name"].ToString();
	string SalesName = dr["sales_name"].ToString();
	string level = dr["dealer_level"].ToString();
	string balance = MyDoubleParse(dr["balance"].ToString()).ToString("c");
	string purchase = MyDoubleParse(dr["trans_total"].ToString()).ToString("c");
	bool bStopOrder = bool.Parse(dr["stop_order"].ToString());

	Response.Write("<table valign=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=7><font size=+1>" + trading_name + "</font></td></tr>");

//	Response.Write("<tr><td colspan=9><b>Customer Details</b></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:lightblue;font-weight:bold;\">\r\n");
	Response.Write("<th> Manager </th>");
	Response.Write("<th> Level </th>");
	Response.Write("<th> Sales </th>");
	Response.Write("<th> Purchase_History </th>");
	Response.Write("<th> Balance </th>");
	Response.Write("<th> Stop_Order </th>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td>" + name + "</td>");
	Response.Write("<td align=center>" + level + "</td>");
	Response.Write("<td align=right>" + SalesName + "</td>");
	Response.Write("<td align=right>" + purchase + "</td>");
	Response.Write("<td align=right>" + balance + "</td>");
	Response.Write("<td align=right>");
	if(bStopOrder)
		Response.Write("<font color=red>YES</font>");
	else
		Response.Write("No");
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("</table>");
}

bool DoCustomerSearchAndList()
{
//	string uri = Request.ServerVariables["URL"] + "?";	// + Request.ServerVariables["QUERY_STRING"];
	int rows = 0;
	string kw = "'%" + EncodeQuote(Request.Form["customer_search"]) + "%'";
//	string s_random = DateTime.Now.ToOADate().ToString();
//	string sc = "SELECT *, '" + uri + "' + 'cid='+ LTRIM(STR(id)) AS uri FROM card ";
	string sc = "SELECT c.*, c1.name AS sales_name ";
	sc += " FROM card c LEFT OUTER JOIN card c1 ON c1.id = c.sales ";
//	sc += " WHERE main_card_id is null AND type<>3 AND ("; //type 3: supplier;
	sc += " WHERE ("; //type 3: supplier;
	if(IsInteger(Request.Form["customer_search"]))
		sc += " c.id=" + Request.Form["customer_search"] + ") ";
	else
		sc += " c.name LIKE " + kw + " OR c.email LIKE " + kw + " OR c.trading_name LIKE " + kw + ")";
	sc += " ORDER BY c.trading_name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
		if(rows == 1)
		{
			string search_id = dst.Tables["card"].Rows[0]["id"].ToString();
			Trim(ref search_id);
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=salesref.aspx?cid=" + search_id);
			if(m_code != "")
				Response.Write("&code=" + m_code);
			Response.Write("\">");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<br><center><h3>Select Customer</h3>");
	BindCustomer();

	return true;
}

void BindCustomer()
{
	string bgcolor = "lightblue";//GetSiteSettings("table_row_bgcolor", "#666696");
	Response.Write("<table width=90%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:" + bgcolor +";font-weight:bold;\">\r\n");
	Response.Write("<th>ACC#</th>");
	Response.Write("<th>Company</th>\r\n");
	Response.Write("<th>Contact</th>\r\n");
	Response.Write("<th>Email</th>\r\n");
	Response.Write("<th>Phone</th>\r\n");
	Response.Write("<th>Level</th>\r\n");
	Response.Write("<th>Sales_Person</th>\r\n");
	Response.Write("<th>Purchase_History</th>\r\n");
	Response.Write("<th>Balance</td>\r\n");
	Response.Write("</tr>\r\n");

	bool bcolor = true;
	string scolor = "";

	int rows = 50;
	for(int i=0; i<rows; i++)
	{
		if(i >= dst.Tables["card"].Rows.Count)
			break;

		if(bcolor)
			scolor = " bgcolor=#EEEEEE";
		else
			scolor = "";

		bcolor = !bcolor;

		DataRow dr = dst.Tables["card"].Rows[i];
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string level = dr["dealer_level"].ToString();
		string name = dr["name"].ToString();
		string email = dr["email"].ToString();
		string purchase = MyDoubleParse(dr["trans_total"].ToString()).ToString("c");
		string balance = MyDoubleParse(dr["balance"].ToString()).ToString("c");
		string sales = dr["sales_name"].ToString();
		string phone = dr["phone"].ToString();

		Response.Write("<tr" + scolor + ">");

		Response.Write("<td><a href=salesref.aspx?cid=" + id + "&code=" + m_code + ">");
		Response.Write(id + "</a></td>\r\n");

		Response.Write("<td><a href=salesref.aspx?cid=" + id + "&code=" + m_code + ">");
		Response.Write(trading_name + "</a></td>");

		Response.Write("<td><a href=salesref.aspx?cid=" + id + "&code=" + m_code + ">");
		Response.Write(name + "</a></td>");

		Response.Write("<td><a href=salesref.aspx?cid=" + id + "&code=" + m_code + ">");
		Response.Write(email + "</a></td>");

		Response.Write("<td>" + phone + "</td>");
		Response.Write("<td align=center>" + level + "</td>");
		Response.Write("<td align=right>" + sales + "</td>");
		Response.Write("<td align=right>" + purchase + "</td>");
		Response.Write("<td align=right>" + balance + "</td>");

		Response.Write("</tr>");
	}	
	Response.Write("</table>");
}

void BindISTable()
{
	string bgcolor = "lightblue";//GetSiteSettings("table_row_bgcolor", "#666696");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:" + bgcolor +";font-weight:bold;\">\r\n");
	Response.Write("<td>Code</td>\r\n");
	Response.Write("<td>M_PN</td>\r\n");
	Response.Write("<td>Description</td>\r\n");
	Response.Write("</tr>\r\n");

	bool bcolor = true;
	string scolor = "";

	for(int i=0; i<m_nSearchReturn; i++)
	{
		if(bcolor)
			scolor = " bgcolor=#EEEEEE";
		else
			scolor = "";

		bcolor = !bcolor;

		DataRow dr = dst.Tables["isearch"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();

		Response.Write("<tr" + scolor + ">");

		Response.Write("<td><a href=salesref.aspx?cid=" + m_cardID + "&code=" + code + ">");
		Response.Write(code + "</a></td>\r\n");

		Response.Write("<td><a href=salesref.aspx?cid=" + m_cardID + "&code=" + code + ">");
		Response.Write(supplier_code + "</a></td>\r\n");

		Response.Write("<td><a href=salesref.aspx?cid=" + m_cardID + "&code=" + code + ">");
		Response.Write(name + "</a></td>\r\n");

		Response.Write("</tr>");
	}	
	Response.Write("</table>");
}
</script>

<asp:Label id=LFooter runat=server/>
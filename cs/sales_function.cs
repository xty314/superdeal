<script runat="server">

string m_orderID = "";
string m_orderNumber = "";
string m_customerID = "0";
string m_customerName = "";
string m_customerEmail = "";
string m_quoteType = "2";
string m_sales = "";
string m_sSalesID  = "";

string m_billToID = "";

bool m_bSearchButton = false;
bool m_bPrintSN = false;

double m_dGstRate = 0.15; //differ from customer

string kw_record;

string DrawCustomerTable()
{
	return DrawCustomerTable("");
}

string DrawCustomerTable(string sCustomerPONumber)
{
	//Customer Information
	
	StringBuilder sb = new StringBuilder();
	sb.Append("<b>Customer :&nbsp;</b>");
	sb.Append("<select name=customer onclick=window.location=('pos.aspx?search=1&r=" + DateTime.Now.ToOADate().ToString() + "')>");
	sb.Append("<option value=0>Cash Sales</option>");
	if(m_customerID != "0")
		sb.Append("<option value='" + m_customerID + "' selected>" + m_customerName + "</option>");
	sb.Append("</select>");
	return sb.ToString();
}

bool InvoicePaid()
{
	string sc = "SELECT paid FROM invoice WHERE invoice_number = " + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "invoice_paid") <= 0)
		{
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}	
	return (bool)dst.Tables["invoice_paid"].Rows[0]["paid"];

}

bool GetInvoiceTotal()
{
	string sc = "SELECT * FROM invoice WHERE invoice_number = " + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "invoice_total") <= 0)
		{
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

bool GetCustomer()
{
	if(dst.Tables["card"] != null)
		dst.Tables["card"].Clear();

	Trim(ref m_customerEmail);

	//prepare customer ID
	string id = "";
	if(Request.QueryString["ci"] == null)
	{
		if(m_customerEmail == "")
		{
			if(Session["sales_customerid" + m_ssid] == null)
			{
				return true;
			}
			id = Session["sales_customerid" + m_ssid].ToString();
		}
	}
	else if(Request.QueryString["ci"] != "")
	{
		id = Request.QueryString["ci"].ToString();
		Session["sales_customerid" + m_ssid] = id;
	}

	if(id != "")
		m_customerID = id;
//DEBUG("id =", id);
//DEBUG("customerEmail =", m_customerEmail);
	
	//do search
	string sc = "";
	if(id != "")
		sc = "SELECT * FROM card WHERE id=" + id;
	else
		sc = "SELECT * FROM card WHERE email='" + m_customerEmail + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "card") <= 0)
		{
			//cash sales, update price if needed
			if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString() != "1")
			{
				Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = "1";
				CheckShoppingCart();
				if(!IsCartEmpty())
					AdjustSalesPriceForAnotherCustomer();
			}
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG("RowCount = ", dst.Tables["card"].Rows.Count);
	//get customer data
	DataRow dr = dst.Tables["card"].Rows[0];

	string main_card_id = dr["main_card_id"].ToString();
	if(main_card_id != "")
	{
		DataRow drm = GetCardData(main_card_id);
		string boss = drm["trading_name"].ToString();
		bool bCanPlaceOrder = CanPlaceOrder(m_customerID);
		if(!bCanPlaceOrder)
		{
			Response.Write("<br><center><h3>Cannot Place Order</h3>");
			Response.Write("<h5>This account has no right to place order accroding to his manager setup<br>");
			Response.Write("Please contact its main account <a href=viewcard.aspx?id=" + main_card_id);
			Response.Write(" target=_blank class=o>ACC# " + main_card_id + ", " + boss + "</a><br>");
			Response.Write("</h5>");
			Response.Write("<input type=button value='Choose Another Account' onclick=window.location=('pos.aspx?search=1') " + Session["button_style"] + ">");
			return false;
		}
		else
		{
/*			Response.Write("<br><center><h3>Extra Login Account</h3>");
			Response.Write("<h5>This account is an extra login of <a href=viewcard.aspx?id=" + main_card_id);
			Response.Write(" target=_blank class=o>ACC# " + main_card_id + ", " + boss + "</a><br>");
			Response.Write("Placing Order is allowed by main account manager.<br><br>");
			Response.Write("Please use main account to place order</h5>");
			Response.Write("<input type=button value='OK, use main account' onclick=window.location=('pos.aspx?ci=" + main_card_id + "') " + Session["button_style"] + ">");
*/
			m_customerID = main_card_id;
			Session["sales_customerid" + m_ssid] = m_customerID;
			Session["sales_special_shipto" + m_ssid] = "1";
			string ssta = dr["trading_name"].ToString() + "\r\n" + dr["address1"].ToString() + "\r\n";
			if(dr["address2"].ToString() != "")
				ssta += dr["address2"].ToString() + "\r\n";
			if(dr["address3"].ToString() != "")
				ssta += dr["address3"].ToString() + "\r\n";
			if(dr["phone"].ToString() != "")
				ssta += "ph:" + dr["phone"].ToString();
				
			Session["sales_special_ship_to_addr" + m_ssid] = ssta;
			sc = " SELECT * FROM card WHERE id = " + main_card_id; 
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				if(myCommand.Fill(dst, "card_main") > 0)
				{
					dr = dst.Tables["card_main"].Rows[0];
				}
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
		}
	}

	m_customerEmail = dr["email"].ToString();
	m_customerName = dr["trading_name"].ToString();
	if(m_customerName == "")
		m_customerName = dr["name"].ToString();

	Trim(ref m_customerEmail);

	//m_dGstRate = MyDoubleParse(dr["gst_rate"].ToString()); //************************ GST Sean
	
	string dealer_level = dr["dealer_level"].ToString();
	Session["sales_customerid" + m_ssid] = m_customerID;

	Session[m_sCompanyName + "_card_type_for_pos" + m_ssid] = dr["type"].ToString();
	//session dealer level when sale place an order for customer
	Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = dealer_level;
	//session for quick sales dealer level;
	Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] = dealer_level;
	if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString() != dealer_level)
	{
		Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = dealer_level;
		CheckShoppingCart();
		if(!IsCartEmpty())
			AdjustSalesPriceForAnotherCustomer();
	}
	
	if(id == "")
	{
		m_customerID = dr["id"].ToString();
	}
	return true;
}

bool AdjustSalesPriceForAnotherCustomer()
{
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;
		
		if(dtCart.Rows[i]["kit"] == "1")
			continue;

//		DataRow drp = null;
		string code = dr["code"].ToString();
		string qty = dr["quantity"].ToString();

		double dPrice = GetSalesPriceForDealer(code, qty, Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		dr["salesPrice"] = dPrice.ToString();
//DEBUG("new price=", dPrice.ToString());
	}
	return true;
}

bool GetBillTo()
{
	if(dst.Tables["billto"] != null)
		dst.Tables["billto"].Clear();

	//prepare bill to ID
	string id = "";
	if(Request.QueryString["bi"] == null)
	{
		if(Session["sales_billtoid" + m_ssid] == null)
		{
			id = GetSiteSettings("card_id_for_purchase_bill_to");
			if(id == "")
				id = "0"; //default set to the first card avoid error
		}
		else
			id = Session["sales_billtoid" + m_ssid].ToString();
	}
	else if(Request.QueryString["bi"] != "")
	{
		id = Request.QueryString["bi"].ToString();
		Session["sales_billtoid" + m_ssid] = id;
	}
	
	//do search
	string sc = "";
	sc = "SELECT * FROM card WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "billto") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = dst.Tables["billto"].Rows[0];
	if(id == "")
	{
		m_billToID = dr["id"].ToString();
		Session["sales_billtoid" + m_ssid] = id;
	}
	return true;
}

string BuildQInvoice(string msg, string sType, bool bInvoiceOnly, bool bSystem)
{
	return BuildQInvoice(msg, sType, bInvoiceOnly, bSystem, "", "");
}

string BuildQInvoice(string msg, string sType, bool bInvoiceOnly, bool bSystem, string sCustomerPONumber, string sComment)
{
	sType = GetEnumValue("receipt_type", sType);
	StringBuilder sb = new StringBuilder();

	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
//	sb.Append("<head><center><h2>System Quotation</h2></center></head>\r\n");
	sb.Append("<body>\r\n");

	sb.Append("<b>");

	string b = msg; 
	for(int j=0; j<b.Length; j++)
	{
		if(b[j] == '\r' || b[j] == '\n')
		{
			sb.Append("<br>");
			j++;
		}
		else
			sb.Append(b[j]);
	}
//	sb.Append(Request.Form["mailbody"]);
	sb.Append("</b>");

	sb.Append(InvoicePrintHeader(sType, "", m_orderNumber, DateTime.Now.ToString("dd/MM/yyyy"), sCustomerPONumber));
//DEBUG("here = ", 0);
	//get customer
	DataRow dr = null;
	if(!GetCustomer())
		return "";
	if(dst.Tables["card"] != null && dst.Tables["card"].Rows.Count > 0)
		dr = dst.Tables["card"].Rows[0];
	sb.Append(InvoicePrintShip(dr, ""));

	sb.Append("</td></tr><tr><td>\r\n");

	sb.Append("<table width=100% cellpadding=0 cellspacing=0");
	if(bSystem)
		sb.Append(" bgcolor=#FFFFEE><tr><td><b>SYSTEM</b>");
	else
		sb.Append("><tr><td>");
	sb.Append("</td></tr><tr>");
	sb.Append("<td width=70>PART#</td>\r\n");
	sb.Append("<td>DESCRIPTION</td>\r\n");
	sb.Append("<td width=70 align=right>");
	sb.Append("PRICE");
	sb.Append("</td>\r\n");
	sb.Append("<td width=40 align=right>QTY</td>\r\n");
	sb.Append("<td width=70 align=right>");
	sb.Append("AMOUNT");
	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	double dTotal = 0;
	StringBuilder sq = new StringBuilder();
	sq.Append("q.aspx?t=b"); //query string for url link
	
	if(bSystem)
	{
		for(int i=0; i<m_qfields; i++)
		{
			string code = dtQ.Rows[0][i].ToString();
	//DEBUG("i="+i, " code="+code);
			if(code == null || code == "")
			{
				continue;
			}
			if(int.Parse(code) <= 0)
				continue;
			string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
			double dPrice = double.Parse(dtQ.Rows[0][fn[i]+"_price"].ToString(), NumberStyles.Currency, null);
			double dsPrice = dPrice * int.Parse(qty);
			string price = dPrice.ToString("c");
			dTotal += dsPrice;
			
			sq.Append("&" + fn[i] + "=" + code);

			sb.Append("<tr><td>");
			sb.Append(code);
			sb.Append("</td><td>");
			sb.Append(GetProductDesc(code));
			sb.Append("</td><td align=right>");
			sb.Append(price);
			sb.Append("</td><td align=right>");
			sb.Append(qty);
			sb.Append("</td><td align=right>"); //quantity
			sb.Append(dsPrice.ToString("c"));
			sb.Append("</td></tr>");
		}
	}

	//optionals
	if(m_bSales) //print shopping cart as optionals
	{
		sb.Append("<tr><td>&nbsp;</td></tr>");
		CheckShoppingCart();
		for(int i=0; i<dtCart.Rows.Count; i++)
		{
			if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
				continue;

			if(bSystem)
			{
				if(dtCart.Rows[i]["system"].ToString() == "1")
					continue;
			}

			DataRow drp = null;
			string code = dtCart.Rows[i]["code"].ToString();
			string qty = dtCart.Rows[i]["quantity"].ToString();
			if(!GetProductWithSpecialPrice(code, ref drp))
				return "";

			string s_salesPrice = dtCart.Rows[i]["salesPrice"].ToString();
			if(dtCart.Rows[i]["salesPrice"].ToString() == "" || dtCart.Rows[i]["salesPrice"].ToString() == null)
				s_salesPrice = "0";
			double dPrice = double.Parse(s_salesPrice, NumberStyles.Currency, null);
			dTotal += dPrice * int.Parse(qty);

			double dsTotal = dPrice * int.Parse(qty);
			double dsupplierPrice = 0;
			
			if(code != "" && code !=null &&  code!="0")
			{
				if(!GetSupplierPrice(code, ref dsupplierPrice))
				return "";
			}

			sb.Append("<tr><td>" + code + "</td>");
			
			if(code != "" && code !=null &&  code!="0")
				sb.Append("<td>" + drp["name"].ToString() + "</td>");
			else
				sb.Append("<td>" + dtCart.Rows[i]["name"].ToString() + "</td>");
			sb.Append("<td align=right>");
			sb.Append(dPrice.ToString("c"));
			sb.Append("</td>");
			sb.Append("<td align=right>" + qty + "</td>");
			sb.Append("<td align=right>");
			sb.Append(dsTotal.ToString("c"));
			sb.Append("</td></tr>");
		}
	}

	double dAmount = dTotal;
//	double dTAX = dTotal * 0.125;
//	dAmount *= 1.125;
	double dGST = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;		//Modified by NEO
	double dTAX = dTotal * dGST;														//Modified by NEO
	dAmount *= 1 + dGST;																//Modified by NEO

	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");
	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>Sub-Total:</b></td><td align=right>");
	sb.Append(dTotal.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>GST:</b></td><td align=right>");
	sb.Append(dTAX.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>TOTAL:</b></td><td align=right>");
	sb.Append(dAmount.ToString("c"));
	sb.Append("</td></tr></table>\r\n");

	sb.Append("</table>");
	sb.Append("</td></tr>");
	if(!bInvoiceOnly && !m_bSales)
		sb.Append("<tr><td>Check out details on <a href=" + sq + ">" + sq + "</a></td></tr>");
	sb.Append("</table>");

	if(m_bPrintSN)
	{
		sb.Append("<br><hr></hr>");
		sb.Append("<table width=100% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		sb.Append("<tr><td width=10%><b>Product ID</b></td><td width=90%><b>Serial Numbers:</b></td></tr>");

		DataRow dr_sn = null;
		if(!GetSNs())
			return "Error: getting product serial number error!";

		string s_preCode = "";
		string s_ProductID = "";
		string s_ProductSNs = "";

		if(dst.Tables["sales_SNs"].Rows.Count > 0)
		{
			dr_sn = dst.Tables["sales_SNs"].Rows[0];
			sb.Append("<tr><td>" + dr_sn["code"].ToString() + "</tr></td><td>" + dr_sn["sn"].ToString());// + "</td></tr>");

			s_preCode = dr_sn["code"].ToString();
			s_ProductSNs = dr_sn["sn"].ToString();
			
			if(dst.Tables["sales_SNs"].Rows.Count > 1)
			{
				for(int i = 1; i < dst.Tables["sales_SNs"].Rows.Count; i++)
				{
					dr_sn = dst.Tables["sales_SNs"].Rows[i];

					if(s_preCode != dr_sn["code"].ToString())
					{
						sb.Append("</td></tr>");
						sb.Append("<tr><td>" + dr_sn["code"].ToString() + "</tr></td><td>" + dr_sn["sn"].ToString());
						s_preCode = dr_sn["code"].ToString();
						//s_ProductSNs = dr_sn["sn"].ToString();
					}
					else
					{
						//s_preCode = dr_sn["code"].ToString();
						s_ProductSNs = dr_sn["sn"].ToString();
						sb.Append(", " + s_ProductSNs);
					}
				}
			}
			else
			{
				sb.Append("</td></tr>");
			}

		}

		sb.Append("</table><br>");
	}
	sb.Append(InvoicePrintBottom(sComment));

	sb.Append("</body></html>");
	return sb.ToString();
}

bool GetSNs()
{
	string sc = "SELECT	code, sn FROM sales_serial WHERE invoice_number = " + m_orderNumber + "Group BY code, sn";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "sales_SNs");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool PrepareNewSales()
{
	CheckShoppingCart();
	EmptyCart();
	Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = "1";
	m_branchID = Session["login_branch_id"].ToString();
	m_sSalesID = Session["login_card_id"].ToString();
	Session["pos_branch_id" + m_ssid] = Session["login_branch_id"].ToString();
	Session["pos_sales_id" + m_ssid] = Session["login_card_id"].ToString();

/*
	Session["order_created"] = null;
	Session["sales_current_order_number"] = null;
	Session["sales_current_order_id"] = null;
	Session["sales_customerid" + m_ssid] = null;
	Session["m_customer_po_number"] = null;
	Session["sales_customer_po_number"] = null;
	Session["m_sales_note"] = null;
	Session["sales_note"] = null;
	Session["SalesType"] = null;
	Session["sales_freight"] = null;
	Session["sales_type_credit"] = null;
	Session["EditingOrder"] = null;

	Session["sales_shipping_method"] = null ;
	Session["sales_special_shipto"] = null;
	Session["sales_special_ship_to_addr"] = null;
	Session["sales_pick_up_time"] = null;
	Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = "1";
*/
	return true;
}

bool DoCustomerSearch()
{
	string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%#@#@#@#@#@#@%'";
	string sc = "SELECT id, '" + uri + "' + '&ci=' + LTRIM(STR(id)) AS uri, name=CASE name WHEN '' THEN company ELSE name END, email, company FROM card ";
	sc += " WHERE (name LIKE " + kw + " OR email LIKE " + kw + " OR company LIKE " + kw + ")";
//	if(m_bOrder)
//		sc += " AND type='supplier' ";
	sc += " ORDER BY name";
//DEBUG("sc=", sc);
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	BindGrid();
	Response.Write("<form id=search action=" + uri + " method=post>");
	Response.Write("<input type=hidden name=invoice_number value=" + m_invoiceNumber + ">");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search><input type=submit name=cmd value=Cancel>");
	Response.Write("</td></tr></table></form>");
	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["card"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_PageA(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

bool DoApplyDiscountTotal(double dCustomerGSTRate, bool bManualSetGST)
{
//DEBUG("GST",Request.Form["custgst"]);

	if(Request.Form["custgst"] != null && Request.Form["custgst"] != "")
	{	
		m_dGstRate = double.Parse(Request.Form["custgst"]);	
		Session["custgst"+m_ssid] = m_dGstRate.ToString();
	}
	
	if((Request.Form["discount_total"] != null && Request.Form["discount_total"] != "") && (Request.Form["discount_total_per"] == null || Request.Form["discount_total_per"] == "") )
	{
	
		string discount = Request.Form["discount_total"];
		string discount_per = Request.Form["discount_total_per"];
		
		try
		{
			double.Parse(discount);
			
		}
		catch(Exception e)
		{
			return false;
		}
		
		string gst = Request.Form["custgst"];
		
		double GST_rate = double.Parse(gst);
		
		//DEBUG("GST_rate", GST_rate);
		
		int rows = dtCart.Rows.Count;
		int i = 0;
		for(i=rows-1; i>=0; i--)
		{
			if(dtCart.Rows[i]["system"] == "1")
			{
				Response.Write("<br><br><h3><font color=red>Error, Discount calculation not applied to system quotations");
				return false;
			}
		}
		bool bCheckBottomPrice = MyBooleanParse(GetSiteSettings("enable_bottom_price_check", "1"));
		
		if(dst.Tables["speciallist"] != null)
			dst.Tables["speciallist"].Clear();
		string sc = " Select code FROM specials WHERE 1 = 1";
		int nRows = 0;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			nRows = myCommand.Fill(dst, "speciallist");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}
		
		double total_amount = MyMoneyParse(discount);
		if(total_amount.ToString().Trim() != "")
			Session["total_amount"+m_ssid] = total_amount.ToString("c");
		//double subTotal = MyMoneyParse(Request.Form["subtotal"]);
		double total_amount_old = MyMoneyParse(Request.Form["totaldue"]);
		//double discount_rate = Math.Round(((1-(total_amount/subTotal))*100),2);
		
		//if(bManualSetGST)
			//subTotal = subTotal / (1 + (GST_rate));
		double dFreight = MyMoneyParse(Request.Form["freight"]);
		//double dPrice = MyMoneyParse(dtCart.Rows[4]["SalesPrice"].ToString());
		//DEBUG("dPrice", dPrice);
		double dPrice = 0;
		double dPriceOld = 0;
		double dPriceSpecial_add = 0;
		double dPrice_add = 0;
		int dQty = 0;
		for(i=rows-1; i>=0; i--)
		{
			string special_check = "0";
			for(int j = 0 ; j < nRows ; j++)
			{
				DataRow dr =  dst.Tables["speciallist"].Rows[j];
				if( dtCart.Rows[i]["code"].ToString() == dr["code"].ToString())
				{
					dPrice = MyMoneyParse(dtCart.Rows[i]["SalesPrice"].ToString());
					dQty = MyIntParse(dtCart.Rows[i]["quantity"].ToString());
					dPriceSpecial_add += (dPrice*dQty);
					special_check = "1";
					
				}
			}
			if( special_check != "1")
			{
				dPrice = MyMoneyParse(dtCart.Rows[i]["SalesPrice"].ToString());
				dQty = MyIntParse(dtCart.Rows[i]["quantity"].ToString());
				dPrice_add += (dPrice*dQty);
			}
		}
		double discount_rate = 0;
		if(total_amount - dPriceSpecial_add > 0)
			discount_rate = Math.Round(((1-((total_amount - dPriceSpecial_add) /dPrice_add))*100),2);
		else
			Session["total_amount"+m_ssid] = (dPriceSpecial_add + dPrice_add).ToString("c");
		Session["discount_per"+m_ssid] = discount_rate.ToString();
		
		for(i=rows-1; i>=0; i--)
		{
			
			dPrice = MyMoneyParse(dtCart.Rows[i]["SalesPrice"].ToString());
            if ( nRows == 0)
            {
                dtCart.Rows[i].BeginEdit();
				dtCart.Rows[i]["discount_percent"] = discount_rate;
				dtCart.Rows[i].EndEdit();
            }
            else
            {
			    for(int j = 0 ; j < nRows ; j++)
			    {
				    DataRow dr =  dst.Tables["speciallist"].Rows[j];
				    if( dtCart.Rows[i]["code"].ToString() == dr["code"].ToString())
				    {
					    dtCart.Rows[i].BeginEdit();
					    dtCart.Rows[i]["discount_percent"] = 0;
					    dtCart.Rows[i].EndEdit();
					    break;
				    }
				    else
				    {
					    dtCart.Rows[i].BeginEdit();
					    dtCart.Rows[i]["discount_percent"] = discount_rate;
					    dtCart.Rows[i].EndEdit();
				    }
			    }
            }	
			//DEBUG("discount_rate", discount_rate);
			//DEBUG("total_amount",  total_amount);
			//DEBUG("dQty", dQty);
			//DEBUG("dPrice", dPrice);
		}
		
	
	
		
		
		return true;
	}
	else if((Request.Form["discount_total_per"] != null && Request.Form["discount_total_per"] != "") && (Request.Form["discount_total"] == null || Request.Form["discount_total"] == ""))
	{
		string discount = Request.Form["discount_total"];
		string discount_per = Request.Form["discount_total_per"];
//DEBUG("total_amount",discount);	
		try
		{
			double.Parse(discount_per);
			
		}
		catch(Exception e)
		{
			return false;
		}
		
		string gst = Request.Form["custgst"];
		double GST_rate = double.Parse(gst);
//DEBUG("GST_rate", GST_rate);
		int rows = dtCart.Rows.Count;
		int i = 0;
		
		for(i=rows-1; i>=0; i--)
		{
			if(dtCart.Rows[i]["system"] == "1")
			{
				Response.Write("<br><br><h3><font color=red>Error, Discount calculation not applied to system quotations");
				return false;
			}
		}
		
		bool bCheckBottomPrice = MyBooleanParse(GetSiteSettings("enable_bottom_price_check", "1"));
		double dTotal_discount = double.Parse(discount_per);
		if(dTotal_discount.ToString().Trim() != "")
			Session["discount_per"+m_ssid] = dTotal_discount.ToString();
		//double subTotal = MyMoneyParse(Request.Form["subtotal"]);
		//double total_amount_old = MyMoneyParse(Request.Form["totaldue"]);
		//double discount_rate = Math.Round(((1-(total_amount/subTotal))*100),2);
		//if(bManualSetGST)
			//subTotal = subTotal / (1 + (GST_rate));
		double dFreight = MyMoneyParse(Request.Form["freight"]);
		//double dPrice = MyMoneyParse(dtCart.Rows[4]["SalesPrice"].ToString());
//DEBUG("dPrice", dPrice);
		if(dst.Tables["speciallist"] != null)
			dst.Tables["speciallist"].Clear();
		string sc = " Select code FROM specials WHERE 1 = 1";
		int nRows = 0;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			nRows = myCommand.Fill(dst, "speciallist");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}
		double dPrice = 0;
		double dPrice_add = 0;
		double dPriceSpecial_add = 0;
		int dQty = 0;
		for(i=rows-1; i>=0; i--)
		{
			string special_check = "0";
			for(int j = 0 ; j < nRows ; j++)
			{
				DataRow dr =  dst.Tables["speciallist"].Rows[j];
				if( dtCart.Rows[i]["code"].ToString() == dr["code"].ToString())
				{
					dPrice = MyMoneyParse(dtCart.Rows[i]["SalesPrice"].ToString());
					dQty = MyIntParse(dtCart.Rows[i]["quantity"].ToString());
					dPriceSpecial_add += (dPrice*dQty);
					special_check = "1";
					
				}
			}
			if( special_check != "1")
			{
				dPrice = MyMoneyParse(dtCart.Rows[i]["SalesPrice"].ToString());
				dQty = MyIntParse(dtCart.Rows[i]["quantity"].ToString());
				dPrice_add += (dPrice*dQty);
			}
		}
		
		double discount_rate = 0;
		if(dPrice_add != 0)
		{
			discount_rate = dTotal_discount;
			double total_amount = dPrice_add*(1-(discount_rate/100)) + dPriceSpecial_add;
			Session["total_amount"+m_ssid] = total_amount.ToString("c");
		}
		else
		{
			Session["total_amount"+m_ssid] = dPriceSpecial_add.ToString("c");
			Session["discount_per"+m_ssid] = discount_rate.ToString();
		}
			
		
		for(i=rows-1; i>=0; i--)
		{
			if ( nRows == 0)
            {
                dtCart.Rows[i].BeginEdit();
			    dtCart.Rows[i]["discount_percent"] = discount_rate;
			    dtCart.Rows[i].EndEdit();
            }
            else
            {
                for(int j = 0 ; j < nRows ; j++)
			    {
				    DataRow dr =  dst.Tables["speciallist"].Rows[j];
				    if( dtCart.Rows[i]["code"].ToString() == dr["code"].ToString())
				    {
					    dtCart.Rows[i].BeginEdit();
					    dtCart.Rows[i]["discount_percent"] = 0;
					    dtCart.Rows[i].EndEdit();
					    break;
				    }
				    else
				    {
					    dtCart.Rows[i].BeginEdit();
					    dtCart.Rows[i]["discount_percent"] = discount_rate;
					    dtCart.Rows[i].EndEdit();
				    }
			    }
            }	
//DEBUG("discount_rate", discount_rate);
//DEBUG("total_amount",  total_amount);
//DEBUG("dQty", dQty);
//DEBUG("dPrice", dPrice);
		}
		
		return true;
	}
	
	return true;
}

</script>

<form runat=server>
<asp:Label id=LInv runat=server/>

<asp:DataGrid id=MyDataGridBill 
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
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=Select
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="pos.aspx?bi={0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=Edit
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="ecard.aspx?id={0}"
			 Text=Edit/>
	</Columns>
</asp:DataGrid>

<asp:DataGrid id=MyDataGridQ
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
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=Select
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="q.aspx?ci={0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=Edit
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="eacc.aspx?id={0}"
			 Text=Edit/>
	</Columns>
</asp:DataGrid>

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=false
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
	AllowPaging=True
	PageSize=50
	PagerStyle-PageButtonCount=20
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_PageA
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:BoundColumn HeaderText=ID DataField=id/>
		<asp:HyperLinkColumn
			 HeaderText=Select
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=trading_name/>
		<asp:HyperLinkColumn
			 HeaderText=Select
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=company/>
		<asp:HyperLinkColumn
			 HeaderText=Select
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=Edit
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="ecard.aspx?id={0}"
			 Text=Edit/>
	</Columns>
</asp:DataGrid>

</form>

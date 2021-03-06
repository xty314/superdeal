<!-- #include file="q_functions.cs" -->

<script runat=server>
bool m_bSales = false; //if true print sales menu
DataSet dsCart = new DataSet();
DataTable dtCart = new DataTable();

DataSet dsLevel = new DataSet();

Boolean bCartAlterColor = false;
string sShippingFee = "5";
string m_system = "0";	//system quotation?

double dTotalPrice = 0;
double dTotalGST = 0;
double dAmount = 0;
double dTotalSaving = 0;
double m_dSessionFreight = 0;

bool m_bWithSystem = false;
bool m_bWithSystemOld = false;
bool m_bOrder = false; //true for use cart to order from wholesale
bool m_bAllowDuplicateCode = false; //true to allow same code to add to cart
bool u_compare = false;
int m_cols_cart = 7;

bool CheckShoppingCart()
{
	if(Session[m_sCompanyName + "_ordering"] != null)
		m_bOrder = true;

	string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	if(Session["ShoppingCart" + ssid] == null) 
	{
		dtCart = new DataTable();
		dtCart.Columns.Add(new DataColumn("site", typeof(String)));	//site identifier, m_sCompanyName
//		dtCart.Columns.Add(new DataColumn("id", typeof(String)));	//class identifier, for sales, purchase etc.
		dtCart.Columns.Add(new DataColumn("kid", typeof(String)));	//purchase/sales order item kid
		dtCart.Columns.Add(new DataColumn("code", typeof(String)));	//product code
		dtCart.Columns.Add(new DataColumn("name", typeof(String)));	//product code
		dtCart.Columns.Add(new DataColumn("quantity", typeof(String)));
		dtCart.Columns.Add(new DataColumn("system", typeof(String)));
		dtCart.Columns.Add(new DataColumn("kit", typeof(String)));
		dtCart.Columns.Add(new DataColumn("used", typeof(String)));
		dtCart.Columns.Add(new DataColumn("supplierPrice", typeof(String)));
		dtCart.Columns.Add(new DataColumn("salesPrice", typeof(String)));
		dtCart.Columns.Add(new DataColumn("supplier", typeof(String)));
		dtCart.Columns.Add(new DataColumn("supplier_code", typeof(String)));
		dtCart.Columns.Add(new DataColumn("s_serialNo", typeof(String)));
		dtCart.Columns.Add(new DataColumn("barcode", typeof(String)));
		dtCart.Columns.Add(new DataColumn("points", typeof(String)));
		dtCart.Columns.Add(new DataColumn("discount_percent", typeof(String)));
		dtCart.Columns.Add(new DataColumn("pack", typeof(String)));
		Session["ShoppingCart" + ssid] = dtCart;
		return false;
	}
	else
	{
		dtCart = (DataTable)Session["ShoppingCart" + ssid];
	}
	return true;
}

void EmptyCart()
{
	CheckShoppingCart();
	dtCart.Clear();
	DeleteCart();
//	dtCart = (DataTable)Session["ShoppingCart" + ssid];
/*	dtCart.AcceptChanges();
	for(int i=dtCart.Rows.Count-1; i>=0; i--)
	{
		string site = dtCart.Rows[i]["site"].ToString();
		if(site == m_sCompanyName)
			dtCart.Rows.RemoveAt(i);
	}
	dtCart.AcceptChanges();
*/
}

bool IsCartEmpty()
{
	for(int i=dtCart.Rows.Count-1; i>=0; i--)
	{
		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
			return false;
	}
	return true;
}

bool CartOnPageLoad()
{
	TS_PageLoad(); //do common things, LogVisit etc...
//	CheckUserTable();
	string action = Request.QueryString["t"];

	if(Request.QueryString["empty"] == "1")
	{
		EmptyCart();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return false;
	}
	if(Session[m_sCompanyName + "_ordering"] != null)
	{
		string buyType = "sales";
		if(Session[m_sCompanyName + "_salestype"] != null)
			buyType = Session[m_sCompanyName + "_salestype"].ToString();
		if(buyType == "purchase")
			Response.Redirect("purchase.aspx?ssid=" + Request.QueryString["ssid"]);
		else
			Response.Redirect("pos.aspx?ssid=" + Request.QueryString["ssid"]);
		return false;
	}

	if(action == "b")
	{
		bool bAdded = false;
		if(Request.QueryString["s"] == "1") //system quotation
		{
			m_system = "1";
			if(CreateSystemOrder())
				bAdded = true;
//			else
//				DEBUG("createsystemorder failed", "");
		}
		else if(Request.QueryString["s"] != null)
		{
			string code = Request.QueryString["c"];
			string supplier = Request.QueryString["s"];
			string supplier_code = Request.QueryString["sc"];
			if(AddToCart(code, supplier, supplier_code, "1", ""))
				bAdded = true;
		}
		else if(Request.QueryString["c"] != null)
		{
			string code = Request.QueryString["c"];			
			if(TSIsDigit(code))
			{
				if(Request.QueryString["used"] == "1")
				{
					if(Used_AddToCart(code))
						bAdded = true;
				}
				else
				{
					string qty = "1";
					if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "")
					{
						qty = Request.QueryString["qty"];
						try
						{
							qty = int.Parse(qty).ToString();
						}
						catch(Exception e)
						{
						}
					}										
					if(AddToCart(code, qty, ""))
						bAdded = true;
				}
			}
		}	
		if(!bAdded)
		{			
//			Response.Write("Product Not Found. ");
			return false;
		}
		Session["OrderCreated"] = "false"; //recreate the order
//		Session["bargain_final_price"] = null; 
		if(m_bOrder)
		{
			if(Session[m_sCompanyName + "_salestype"] != null)
			{
				if(Session[m_sCompanyName + "_salestype"].ToString() == "quote")
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=q.aspx?r=" + DateTime.Now.ToOADate() + "\">");
				else
					BackToLastPage();
			}
			else
				BackToLastPage();
		}
		else
		{
//			string code = Request.QueryString["c"];			
//			Response.Write("<script language=javascript>window.alert('Item added to cart');</script");
//			Response.Write(">");
//			BackToLastPage();
//			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=p.aspx?c=" + code + "&r=" + DateTime.Now.ToOADate() + "\">");
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		return false;
	}
	else if(action == "delete")
	{
		string row = Request.QueryString["row"];
		if(!DeleteItem(row))
		{
			Response.Write("Error Remove Item");
			return true;
		}
		Session["bargain_final_price"] = null;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		Session["OrderCreated"] = "false"; //recreate the order
		return false;
	}
	else if(action == "update")
	{
		UpdateQuantity();
		Session["OrderCreated"] = "false"; //recreate the order
		Session["bargain_final_price"] = null;
	}
	return true;
}
bool UpdateQuantity()
{
	string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	if(Session["ShoppingCart" + ssid] == null)
		return true;

	dtCart = (DataTable)Session["ShoppingCart" + ssid];

	dtCart.AcceptChanges(); //Commits all the changes made to this row since the last time AcceptChanges was called
	int quantity = 0;
	double dPrice = 0;
	double dTotal = 0;
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
		{
			if(dtCart.Rows[i]["system"] == "1")
				continue;

			string sqty = Request.Form["Qty"+i.ToString()];
			if(!TSIsDigit(sqty))
				quantity = 0;
			else
			{
				double dqty = double.Parse(sqty);
				quantity = (int)dqty;
			}
			if(quantity <= 0)
			{
				dtCart.Rows.RemoveAt(i);
			}
			else
			{
				dtCart.Rows[i].BeginEdit();
				dtCart.Rows[i]["quantity"] = quantity;
				if(dtCart.Rows[i]["kit"] == "0")
				{
					string card_id = "0";
					if(Session[m_sCompanyName + "_ordering"] != null)
					{
						if(Session[m_sCompanyName + "_dealer_card_id" + m_ssid] != null && Session[m_sCompanyName + "_dealer_card_id" + m_ssid] != "")
							card_id = Session[m_sCompanyName + "_dealer_card_id" + m_ssid].ToString();
					}
					else if(Session["card_id"] != null && Session["card_id"] != "")
					{
						card_id = Session["card_id"].ToString();
					}
					double dQtyPrice = GetSalesPriceForDealer(dtCart.Rows[i]["code"].ToString(), sqty, Session[m_sCompanyName + "dealer_level"].ToString(), card_id);
					dtCart.Rows[i]["SalesPrice"] = dQtyPrice.ToString();
				}
				dtCart.Rows[i].EndEdit();			
			}
		}
	}
	dtCart.AcceptChanges(); //Commits all the changes made to this row since the last time AcceptChanges was called
	SaveCart();
	return true;
}
bool DeleteItem(string row)
{
	string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	if(Session["ShoppingCart" + ssid] == null)
		return true;

	dtCart = (DataTable)Session["ShoppingCart" + ssid];

	int nRow = int.Parse(row);
	if(nRow >= dtCart.Rows.Count)
		return true;
	string sSystem = dtCart.Rows[nRow]["system"].ToString();
//DEBUG("here", sSystem);
	if(sSystem != "1")
		dtCart.Rows.RemoveAt(nRow);
	else
	{
		for(int i=dtCart.Rows.Count-1; i>=0; i--)
		{
			sSystem = dtCart.Rows[i]["system"].ToString();
			if(sSystem == "1")
				dtCart.Rows.RemoveAt(i);
		}
	}
	if(dtCart.Rows.Count <= 0) //enable credit card
		Session[m_sCompanyName + "no_credit_card"] = null;
	SaveCart();
	return true;
}
int GetCartItemForThisSite()
{
	int count = 0;
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
//DEBUG("site=", dtCart.Rows[i]["site"].ToString());
		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
			count++;
	}
//DEBUG("count=", count);
	return count;
}
bool AlreadyExists(string code)
{
	return AlreadyExists(code, 0);
}
bool AlreadyExists(string code, int nAddQty)
{
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["system"].ToString() == "1")
			continue;
		if(dr["code"].ToString() == code && dr["site"].ToString() == m_sCompanyName)
		{
			int nQty = MyIntParse(dr["quantity"].ToString());
			
			int nItemRow = MyIntParse(Request.QueryString["item_row"]);
			int nNewQty = MyIntParse(Request.QueryString["qty0"]);
			//DEBUG("nNewQty ", nAddQty.ToString() + " QTY " +Request.QueryString["qty"+nItemRow] );
			nQty += nNewQty;
			if(nAddQty != 0)
				nQty += nAddQty;
            dr.BeginEdit();
			dr["quantity"] = nQty.ToString();
			dr.EndEdit();
			dtCart.AcceptChanges();
			SaveCart();
            if(m_sSite != "admin" && g("t") == "b")
               Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx\">"); 
			return true;
		}
	}
	return false;
}
bool AlreadyExists(string supplier, string supplier_code)
{
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() == m_sCompanyName && dr["supplier"].ToString() == supplier && dr["supplier_code"].ToString() == supplier_code)
		{
			dtCart.AcceptChanges();
			dr.BeginEdit();
			dr["quantity"] = (int.Parse(dr["quantity"].ToString()) + 1).ToString();
			dr.EndEdit();
			dtCart.AcceptChanges();
			SaveCart();
			return true;
		}
	}
	return false;
}
bool AddToCart(string code, string qty, string sprice)
{
    string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	if(!IsInteger(code))
		return false;
	CheckShoppingCart();
	m_bAllowDuplicateCode = MyBooleanParse(GetSiteSettings("shopping_cart_double_up_qty", "1", false));

//	if(!m_bAllowDuplicateCode)
	if(Request.QueryString["confirm"] == null)
	{
		if(AlreadyExists(code, MyIntParse(qty))) //already exists, update quantity
			return false;
	}

	DataRow dr = dtCart.NewRow();

	DataRow drp = null;
	if(!GetProductFromCodeRelations(code, ref drp))
		return false;
//	if(!GetProduct(code, ref drp))
//		return false;

	string barcode = drp["barcode"].ToString();

	bool bCreditReturn = false;
	string orderType = "purchase";
	if(Session[m_sCompanyName + "_salestype"] != null)
		orderType = Session[m_sCompanyName + "_salestype"].ToString();
//DEBUG("m_bOrder =", m_bOrder.ToString());		
	if(m_bOrder && orderType == "purchase")
	{
		Session["purchase_currency"] = drp["currency"].ToString();
//		Session["purchase_exrate"] = GetSiteSettings("exchange_rate_" + GetEnumValue("currency", drp["currency"].ToString()));
		Session["purchase_exrate"] = GetCurrencyRate(drp["currency"].ToString());
		dr["salesPrice"] = drp["supplier_price"].ToString();
	}
	else
	{
		if(sprice != "")
			dr["salesPrice"] = sprice;
		else
		{
			string level = "1";
			if(Session[m_sCompanyName + "_dealer_level_for_pos" + ssid] != null)
				level = Session[m_sCompanyName + "_dealer_level_for_pos" + ssid].ToString();
     
			string card_id = "0";
			if(Session[m_sCompanyName + "_ordering"] != null)
			{
				if(Session[m_sCompanyName + "_dealer_card_id" + m_ssid] != null && Session[m_sCompanyName + "_dealer_card_id" + m_ssid] != "")
					card_id = Session[m_sCompanyName + "_dealer_card_id" + m_ssid].ToString();
			}
			else if(Session["card_id"] != null && Session["card_id"] != "")
			{
				card_id = Session["card_id"].ToString();
			}
            level = ReadDealerLevel(card_id);
            //DEBUG("l= ", level);
			dr["salesPrice"] = GetSalesPriceForDealer(code, qty, level, card_id); //drp["price"].ToString();			
			//double dBottomPrice = MyDoubleParse(drp["manual_cost_nzd"].ToString()) * MyDoubleParse(drp["rate"].ToString());
            double dBottomPrice = MyDoubleParse(drp["level_price0"].ToString()); //* MyDoubleParse(drp["rate"].ToString());
			if(drp["specials"].ToString() != "-1")
			{
				if(drp["special_price_end_date"].ToString() != null && drp["special_price_end_date"].ToString() != "")
                {
                    DateTime check_time = DateTime.Now;
                    DateTime special_end = DateTime.Parse(drp["special_price_end_date"].ToString());
                    if(check_time <= special_end)
                        dr["salesPrice"] = MyDoubleParse(drp["special_price"].ToString()) / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
                }
            }
			else 
			{
				if(Session[m_sCompanyName + "loggedin"] != null)
                {
                    if(int.Parse(GetSiteSettings("dealer_levels", "1")) > 0 && Session["card_type"].ToString() != "1") //set dealer to get dealer level price
					    dr["salesPrice"] = dBottomPrice * MyDoubleParse(drp["level_rate"+ level +""].ToString())/100;
				    else
					    dr["salesPrice"] = MyDoubleParse(drp["price1"].ToString()) / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
                }
                else if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
				    dr["salesPrice"] = MyDoubleParse(drp["price1"].ToString()) / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
			}
			
				//dr["salesPrice"] = drp["price1"].ToString();
		}
		if(Session["bargain_price_" + code] != null)
			dr["salesPrice"] = Session["bargain_price_" + code].ToString();
	}
	dr["site"] = m_sCompanyName;
	dr["quantity"] = qty;//nqty.ToString();
	dr["code"] = code;
	dr["barcode"] = barcode;
	dr["name"] = drp["name"].ToString();
	dr["supplier"] = drp["supplier"].ToString();
	dr["supplier_code"] = drp["supplier_code"].ToString();
	string cost = drp["supplier_code"].ToString();
	dr["supplierPrice"] = drp["supplier_price"].ToString();

	//use average cost 
	if(MyBooleanParse(GetSiteSettings("set_qpos_to_use_avg_cost", "0", true)))
	{
		if(drp["average_cost"].ToString() != "0")
			dr["supplierPrice"] = drp["average_cost"].ToString();
	}
	dr["system"] = m_system;
	dr["used"] = "0";
	dtCart.Rows.Add(dr);
	SaveCart();
	return true;	
}
bool AddToCart(string code, string supplier, string supplier_code, string qty, string supplier_price)
{
	bool bCreditReturn = false;
	if(Session["sales_type_credit"] != null)
		bCreditReturn = (bool)Session["sales_type_credit"];

	int nqty = MyIntParse(qty);
//	if(bCreditReturn)
//		nqty = 0 - nqty;
	return AddToCart(code, supplier, supplier_code, nqty.ToString(), supplier_price, "", "");
}

bool AddToCart(string code, string supplier, string supplier_code, string qty, string supplier_price, string name, string s_serialNo)
{
	return AddToCart("", code, supplier, supplier_code, qty, supplier_price, "", name, s_serialNo);
}

bool AddToCart(string code, string supplier, string supplier_code, string qty, string supplier_price, string salesPrice, string name, string s_serialNo)
{
	return AddToCart("", code, supplier, supplier_code, qty, supplier_price, salesPrice, name, s_serialNo);
}
bool AddToCart(string kid, string code, string supplier, string supplier_code, string qty, string supplier_price, string salesPrice, string name, string s_serialNo)
{
	return AddToCart(kid, code, supplier, supplier_code, qty, supplier_price, salesPrice, name, s_serialNo, "");
}
bool AddToCart(string kid, string code, string supplier, string supplier_code, string qty, string supplier_price, string salesPrice, string name, string s_serialNo, string discount_percent)
{
	return AddToCart(kid, code, supplier, supplier_code, qty, supplier_price, salesPrice, name, s_serialNo, discount_percent, "");
}
bool AddToCart(string kid, string code, string supplier, string supplier_code, string qty, string supplier_price, string salesPrice, string name, string s_serialNo, string discount_percent, string pack)
{
	
	CheckShoppingCart();
//	if(AlreadyExists(supplier, supplier_code)) //already exists, update quantity
//		return true;

	DataRow drp = null;
	//if(!GetProductWithSpecialPrice(code, ref drp))
		//return false;
    if(!GetProductFromCodeRelations(code, ref drp))
		return false;

	m_bAllowDuplicateCode = MyBooleanParse(GetSiteSettings("purchase_or_sales_double_up_qty", "1", false));
//DEBUG("m_bAllowDuplicateCode =", m_bAllowDuplicateCode.ToString());
	if(!m_bAllowDuplicateCode)
	{
		if(AlreadyExists(code)) //already exists, update quantity
			return true;
	}

	DataRow dr = dtCart.NewRow();

	if(supplier_price != "")
		dr["supplierPrice"] = supplier_price;
	else
	{
		if(drp != null)
		{
			dr["supplierPrice"] = drp["supplier_price"].ToString();
		}
		else
			dr["supplierPrice"] = "0";
	}
	if(drp != null)
	{
		
        dr["salesPrice"] = salesPrice;
        if(drp["specials"].ToString() != "-1")
		{
			if(drp["special_price_end_date"].ToString() != null && drp["special_price_end_date"].ToString() != "")
            {
                DateTime check_time = DateTime.Now;
                DateTime special_end = DateTime.Parse(drp["special_price_end_date"].ToString());
                if(check_time <= special_end)
                    dr["salesPrice"] = MyDoubleParse(drp["special_price"].ToString()) / (1+Double.Parse(Session[m_sCompanyName +"gst_rate"].ToString()));
            }
        }
        if(name == "")
			dr["name"] = drp["name"].ToString();
/*
        if(salesPrice == "")
			dr["salesPrice"] = drp["price"].ToString();
		else
			dr["salesPrice"] = salesPrice;
		if(name == "")
			dr["name"] = drp["name"].ToString();
 */
	}
	else
		dr["salesPrice"] = salesPrice;
	string barcode = "";
	if(drp != null)
		barcode = drp["barcode"].ToString();
	dr["kid"] = kid;
	dr["site"] = m_sCompanyName;
	dr["code"] = code;
	if(name != "")
		dr["name"] = name;
//DEBUG("qty=", qty);
	dr["quantity"] = qty;
	dr["supplier"] = supplier;
	dr["supplier_code"] = supplier_code;
	dr["used"] = "0";
	dr["s_serialNo"] = s_serialNo;
	dr["barcode"] = barcode;
	dr["discount_percent"] = discount_percent;
	dr["pack"] = pack;
	dtCart.Rows.Add(dr);
	SaveCart();
	return true;	
}

bool CreateSystemOrder()
{
	if(!CheckQTable())
	{
		Response.Write("Quotation table error.\r\n");
		return false;
	}
	
	for(int i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString(); 
		string qty = dtQ.Rows[0][fn[i]+"_qty"].ToString(); 
		if(IsInteger(code))
		{
			if(int.Parse(code) > 0)
			{
				if(!AddToCart(code, qty, ""))
				{
					Response.Write("code " + code + " error\r\n");
					return false;
				}
			}
		}
	}
	return true;
}

string PrintCart(bool bButton, bool bInvoice) //if bButton then print buttons(update, continue, shipping etc..)
{
	CheckShoppingCart();
//	CheckUserTable();
//	if(TS_UserLoggedIn())
//	{
//		sShippingFee = dtUser.Rows[0]["shipping_fee"].ToString();
//	}
//	else if(Session["ShippingFee"] != null)
//		sShippingFee = Session["ShippingFee"].ToString();

	int i = 0;
//DEBUG("sf=", sShippingFee);
	StringBuilder sb = new StringBuilder();
	 
	string U_account = @"
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
						  
	//header
	if(bButton)
	{
		sb.Append("\r\n\r\n<table width=100% bgcolor=#CCCCCC cellpadding=4 cellspacing=1 border=0>");
		sb.Append("<tr bgcolor=white><td colspan=" + m_cols_cart + " valign=top>");
	}
	else
	{    
	    sb.Append("<table width=100% align=center border=0>");
		sb.Append("\r\n<tr><td valign=top bgcolor=#F2F2F2 width=200>"+ U_account+"</td><td width=700>");
		sb.Append("<fieldset><legend style=\"font:bold 15px arail; color:#6699CC\">Order information</legend>");
		sb.Append("<table width=95% align=center border=0>");
		sb.Append("<tr><td>");

		sb.Append("<table width=100% align=center cellpadding=2 cellspacing=1 border=0>");
		if(!bInvoice)
		{
			sb.Append("\r\n<tr><td colspan=8><b>");
			if(m_bOrder)
				sb.Append("Order List");
			else
			//	sb.Append("Your Shopping Cart Information");
			sb.Append("</b>\r\n</td></tr>");
		}
		sb.Append("\r\n<tr><td colspan=" + m_cols_cart + " valign=top>");
	}
	sb.Append("\r\n\r\n<table border=0 cellpadding=0 width=100% cellspacing=0>");
	sb.Append("\r\n<tr><td width=100% align=right style='border:none;'>");
	if(bButton && m_sSite != "admin")
	{
		sb.Append("<input type=button value='Continue Shopping' class='btn btn-primary' OnClick=window.location=('" + Session["item_list_url"] + "');>");
		//sb.Append("<input type=button class=b OnClick=\"if(!window.confirm('You must be over 18 years old !!!')){return false;}else{window.location=('");
		sb.Append("<input type=button class='btn btn-success' style='margin-left:5px;' OnClick=\"window.location=('");
		if(m_bOrder)
			sb.Append("purchase.aspx?r=" + DateTime.Now.ToOADate());
		else if(m_bSales)
			sb.Append("pos.aspx?r=" + DateTime.Now.ToOADate());
		else
			sb.Append("checkout.aspx?r=" + DateTime.Now.ToOADate());
		if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
			sb.Append("&ssid=" + Request.QueryString["ssid"]);
		if(m_bOrder)
			sb.Append("')\" value=Purchase>");
		else
			sb.Append("')\" value='Checkout'>");//Continue to checkout</button>");
	}
	sb.Append("\r\n</td></tr></table>\r\n</td></tr>");

	sb.Append("<tr bgcolor=white>");
	sb.Append("<td align=center nowrap >&nbsp;</td>");
//	sb.Append("<td align=center colspan=1 nowrap><b>Quantity</b></td>");
	if(bButton)
		sb.Append("<td align=center width=60px nowrap><b>QTY</b></td>");
	sb.Append("<td align=center nowrap><b>MPN</b></td>");
	if(m_bOrder)
	{
		sb.Append("<td align=center nowrap><b>SUPPLIER</b></td>");
		sb.Append("<td align=center nowrap><b>SUPPLIEW CODE</b></td>");
	}
	sb.Append("<td align=center nowrap><b>IMG</b></td>");
	sb.Append("<td width=100% nowrap><b>DESCRIPTION</b></td>");
//	sb.Append("<td align=center nowrap><b>SHIPS</b></td>");
	if(m_bOrder)
		sb.Append("<td align=center nowrap><b>COST</b></td>");
	else
		sb.Append("<td align=center nowrap><b>PRICE</b></td>");
	if(!bButton)
		sb.Append("<td align=center nowrap><b>QTY</b></td>");
	if(!m_bOrder)
	{
//		sb.Append("<td align=center nowrap><b>GST</b></td>");
		sb.Append("<td align=center nowrap><b>TOTAL</b></td>\r\n</tr>");
	}

	dTotalPrice = 0;
	dTotalGST = 0; //used by confirm.cs
	dAmount = 0;
	dTotalSaving = 0;

	double dCost = 0;

	double dRowPrice = 0;
	double dRowGST = 0;
	double dRowTotal = 0;
	double dRowSaving = 0;
//DEBUG("cartrows=", dtCart.Rows.Count);
	//build up row list
	for(i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		bool bKit = MyBooleanParse(dtCart.Rows[i]["kit"].ToString());

		DataRow drp = null;
		string code = dtCart.Rows[i]["code"].ToString();

		if(bKit)
		{
			sb.Append(PrintOneKit(bButton, i, ref dRowPrice, ref dRowGST, ref dRowTotal, ref dRowSaving));
			dTotalPrice += dRowTotal;
			dTotalSaving += dRowSaving;
			continue;
		}
		else if(dr["used"].ToString() == "1")
		{
			drp = Used_GetProduct(code);
			if(drp == null)
				return "No such used product";
		}
		else
		{
			if(!GetProductWithSpecialPrice(code, ref drp))
				return "Price Error";
		}

		if(drp == null && m_bOrder)
		{
			if(!GetRawProduct(dr["supplier"].ToString(), dr["supplier_code"].ToString(), ref drp))
				return "GetRawProduct Error";
		}

		double dSupplierPrice = 0;
		if(dr["used"].ToString() != "1")
		{
			if(drp != null && drp["supplier_price"] != null)
				dSupplierPrice = double.Parse(drp["supplier_price"].ToString());

			dCost += dSupplierPrice;
		}
		if(drp != null)
			sb.Append(PrintOneRow(bButton, drp, dr["system"].ToString(), i, ref dRowPrice, ref dRowGST, ref dRowTotal, ref dRowSaving));
		else
			continue;
		dTotalPrice += dRowTotal;
//		dTotalGST += dRowGST;
//		dAmount += dRowTotal;
		dTotalSaving += dRowSaving;
	}

	//update quantity
	sb.Append("<tr bgcolor=#ffffff>");

	sb.Append("<td colspan=2 valign=top bgcolor=white>");
	 if(bButton)
	sb.Append("<input class='btn btn-warning' name=cmd type=submit " + Session["button_style"]+"  value='Update Quantity & Price'>");
	sb.Append("</td>");
	
	sb.Append("<td colspan=3 align=right>");
	//Freight options
	sb.Append(PrintFreightOptions(bButton));
	sb.Append("</tr>");
//	Session["ShippingFee"] = m_dSessionFreight;
//	Session["freight"] = m_dSessionFreight;
//DEBUG("m_dSessionFreight ", m_dSessionFreight.ToString());
	//for display on other pages
	dTotalPrice += m_dSessionFreight;
	Session["cart_total_no_gst"] = dTotalPrice;

	dCost = dCost * 1.03; //plus bank fee and dps fee
	//double dGstRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;
	double dGstRate = 0.15;
	if(Session[m_sCompanyName + "gst_rate"] != null)
		dGstRate = MyDoubleParse(Session[m_sCompanyName +"gst_rate"].ToString());
//DEBUG("gs =", dGstRate.ToString());
	dAmount = Math.Round(dTotalPrice * (1 + dGstRate), 2);	
	dTotalGST = dTotalPrice * dGstRate;
	dTotalGST = Math.Round(dTotalGST, 2);

	//the only place to set Session["Amount"]
	string sAmount = dAmount.ToString();
	if(sAmount.IndexOf('.') < 0)
		sAmount += ".00";	//for dps reports "Invalid Amount Format" withou ".00"
	Session["Amount"] = sAmount;
	Session["Cost"] = dCost.ToString(); //later for bargain
//DEBUG("amount=", dAmount.ToString("C"));

	//sub total
	sb.Append("<tr bgcolor=#ffffff>");
	sb.Append("<td colspan=" + (m_cols_cart - 1).ToString() + " align=right bgcolor=#ffffff nowrap>");
	sb.Append("<b>Sub Total&nbsp;&nbsp;</b></td>");
	sb.Append("<td align=right bgcolor=#ffffff nowrap>");
	sb.Append("<font size=1 face=verdana,helvtica><b>");
	sb.Append(dTotalPrice.ToString("c"));
	sb.Append("</font></td>\r\n</tr>");

	//Total GST
	sb.Append("<tr bgcolor=#ffffff>");
	sb.Append("<td colspan=" + (m_cols_cart - 1).ToString() + " valign=top align=right bgcolor=#ffffff nowrap>");
	sb.Append("<b>Total Tax&nbsp;&nbsp;</b></td>");
	sb.Append("<td align=right valign=top bgcolor=#ffffff nowrap>");
	sb.Append("<font size=1 face=verdana,helvtica><b>");
	sb.Append(dTotalGST.ToString("c"));
	sb.Append("</font></td>\r\n</tr>");

	sb.Append("<tr bgcolor=#ffffff>");
	if(bButton)
	{
		sb.Append("<td colspan=" + (m_cols_cart-1).ToString() + " valign=top align=right bgcolor=#ffffff nowrap>");
	}
	else
	{
		sb.Append("<td colspan=2 align=right valign=top bgcolor=#ffffff nowrap>");
		if(!bInvoice)
			sb.Append("<input type=button onClick=window.location='cart.aspx?r=" + DateTime.Now.ToOADate()+"' value='Edit Your Cart'>");
			 
		sb.Append("</td><td colspan=3 valign=top align=right bgcolor=#ffffff nowrap>");
	}
	sb.Append("<b>Total Amount Due&nbsp;&nbsp;</b></td>");
	sb.Append("<td align=right valign=top bgcolor=#ffffff nowrap>");
	sb.Append("<font size=1 face=verdana,helvtica><b>");
	sb.Append(dAmount.ToString("c"));
	sb.Append("</font></td>\r\n</tr>");

	if(Session["bargain_final_price"] != null)
	{
		if(TSIsDigit(Session["bargain_final_price"].ToString()))
		{
			double dBargainFinalPrice = (double)Session["bargain_final_price"];
			sb.Append("\r\n<tr><td colspan=4 valign=top align=right bgcolor=#ffffff><b>Final Bargain Price</b></td>");
			sb.Append("<td align=right valign=top bgcolor=#ffffff nowrap>");
			sb.Append("<font size=1 face=verdana,helvtica color=green><b>");
			sb.Append(dBargainFinalPrice.ToString("c"));
			sb.Append("</font></td>\r\n</tr>");
		}
	}

	sb.Append("<tr bgcolor=white>");
	sb.Append("<td><input type=button class='btn btn-danger' value='Reset Cart' " + Session["button_style"] + " onclick=");
	sb.Append("window.location=('cart.aspx?empty=1')></td>");
	sb.Append("<td colspan=" + m_cols_cart + " valign=middle align=right bgcolor=white nowrap>");
	if(bButton && m_sSite != "admin")
	{
		if(g_bEnableQuotation)
		{
			sb.Append("<input type=button " + Session["button_style"].ToString() + " OnClick=window.location=('");
			sb.Append("q.aspx') value='Move To System Quotation'>");
		}
		sb.Append("<input type=button value='Continue Shopping' class='btn btn-primary' OnClick=window.location=('" + Session["item_list_url"] + "');>");
		//sb.Append("<input type=button class=b OnClick=window.location=('");
        //sb.Append("<input type=button class=b OnClick=\"if(!window.confirm('You must be over 18 years old !!!')){return false;}else{window.location=('");
        sb.Append("<input type=button class='btn btn-success' style='margin-left:5px;' OnClick=\"window.location=('");
		if(m_bOrder)
			sb.Append("purchase.aspx?r=" + DateTime.Now.ToOADate());
		else if(m_bSales)
			sb.Append("pos.aspx?r=" + DateTime.Now.ToOADate());
		else
			sb.Append("checkout.aspx?r=" + DateTime.Now.ToOADate());
		if(m_bOrder)
			sb.Append("')\" value=Purchase>");
		else
			sb.Append("')\" value='Checkout'>");//Continue to checkout</button>");
	}
	sb.Append("\r\n</td></tr>");

	sb.Append("<tr><td bgcolor=white colspan=" + m_cols_cart + ">");
	sb.Append(GetSiteSettings("shopping_cart_notice", "", true));
	sb.Append("</td></tr>");
	
	sb.Append("\r\n</table>");
	if(!bButton)
		sb.Append("\r\n</td></tr></table>");

//DEBUG("Session[edensales]=", Session["edensales"].ToString());
	return sb.ToString();
}

string PrintOneRow(bool bButton, DataRow drp, string sSystem, int nRow, ref double dRowPrice, 
	ref double dRowGST, ref double dRowTotal, ref double dRowSaving)
{
	DataRow dr = dtCart.Rows[nRow];
	double dPrice = 0; 
	double dRetailPrice = double.Parse(drp["price"].ToString());
	if(dr["used"].ToString() != "1")
	{
		if(m_bOrder)
			dPrice = MyDoubleParse(drp["supplier_price"].ToString());
		else if(m_bSales) // normal retail price for shopsale
			dPrice = dRetailPrice;
		else 
		{
			dPrice = MyDoubleParse(dr["SalesPrice"].ToString());
			dRowSaving = dRetailPrice - dPrice;
		}
	}
	else
	{
		dPrice = MyDoubleParse(dr["SalesPrice"].ToString());
	}

	dPrice = Math.Round(dPrice, 2);

	//write salesPrice
	if(!m_bSales)
	{
		dtCart.AcceptChanges();
		dr.BeginEdit();
		dr["salesPrice"] = dPrice.ToString();
		dr.EndEdit();
		dtCart.AcceptChanges();
	}
	int quantity = MyIntParse(dr["quantity"].ToString());
	double dTotal = dPrice * quantity;
	dTotal = Math.Round(dTotal, 2);
	dRowSaving *= quantity;
	
	StringBuilder sb = new StringBuilder();

	sb.Append("\r\n<tr ");
	if(bCartAlterColor && bButton)
		sb.Append("bgcolor=white");
	else
		sb.Append("bgcolor=white");
	bCartAlterColor = !bCartAlterColor;

	sb.Append(">");

	//delete button
	sb.Append("<td align=center valign=middle nowrap>");

	m_bWithSystem = (sSystem == "1");
	//delete button
	if(bButton)
	{
		if(!m_bWithSystem || (m_bWithSystem && !m_bWithSystemOld))
		{
			sb.Append("<input class='btn btn-danger' type=button " + Session["button_style"].ToString() + " OnClick=window.location=('");
			sb.Append("cart.aspx?t=delete&row=" + nRow.ToString() + "&r=" + DateTime.Now.ToOADate());
			sb.Append("') value='");
			if(m_bWithSystem)
				sb.Append("Delete System'>");
			else
				sb.Append("Delete'>");
		}
	}
	sb.Append("</td>");
	m_bWithSystemOld = m_bWithSystem;

	//quantity
	if(bButton)
	{
		if(m_bWithSystem)
		{
			sb.Append("<td align=center>"+dr["quantity"].ToString() + "</td>");
		}
		else
		{
			sb.Append("<td align=center><input class='form-control account-input inputqty' type=text maxlength=3 name=Qty" + nRow.ToString() + " value=");
			sb.Append(dr["quantity"].ToString() + ">");
		}
	}

	//code
	sb.Append("<td align=center valign=middle nowrap>");
	sb.Append(dr["supplier_code"].ToString());
	sb.Append("</td>\r\n");

	if(m_bOrder)
	{
		sb.Append("<td>" + drp["supplier"].ToString() + "</td>");
		sb.Append("<td>" + drp["supplier_code"].ToString() + "</td>");
	}
	//img
	string code = drp["code"].ToString();
	string src = "";
	src = GetProductImgSrc(code);
	src = src.Replace("na.gif", "0.gif");
	sb.Append("<td align=center valign=middle nowrap>");
	sb.Append("<img style=\"width:50px;\" src=" + src + " border=\"0\">");
	sb.Append("</td>\r\n");

	//description
	sb.Append("</td><td valign=middle>");//<a href=p.aspx?");
	//sb.Append(dr["code"].ToString());
	//sb.Append(" class=d target=_blank>");
	sb.Append(drp["name"].ToString());
	sb.Append("</td>\r\n");

	//price
	sb.Append("<td align='right' valign=middle nowrap>");
	sb.Append(dPrice.ToString("c"));
	sb.Append("</td>\r\n");

	//quantity
	if(!bButton)
	{
		sb.Append("<td align=center>" + dr["quantity"].ToString());
		sb.Append("</td>\r\n");
	}

	if(!m_bOrder)
	{
		sb.Append("<td align=right valign=middle nowrap>");
		sb.Append(dTotal.ToString("c"));
		sb.Append("</td>\r\n");
	}

	sb.Append("\r\n</tr>\r\n");

	dRowPrice = dPrice;
	dRowTotal = dTotal;
	return sb.ToString();
}

bool GetSupplierPrice(string code, ref double dPrice)
{
	DataSet dso = new DataSet();
	int rows = 0;

	string sc = "SELECT supplier_price FROM product WHERE code=" + code;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		sc = "SELECT k.supplier_price FROM product_skip k JOIN code_relations c ON k.id=c.id WHERE c.code=" + code;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dso) <= 0)
				return false;
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	dPrice = double.Parse(dso.Tables[0].Rows[0]["supplier_price"].ToString());
	return true;
}

//used itme functions

bool Used_AddToCart(string code)
{
	if(!IsInteger(code))
		return false;
	CheckShoppingCart();
	if(AlreadyExists(code)) //already exists, update quantity
		return true;

	DataRow dr = dtCart.NewRow();

	DataRow drp = Used_GetProduct(code);
	if(drp == null)
		return false;

	dr["site"] = m_sCompanyName;
	dr["quantity"] = "1";
	dr["code"] = code;
	dr["system"] = "0";
	dr["used"] = "1";
	dr["salesPrice"] = drp["price"].ToString();

//DEBUG("code=", code);
	dtCart.Rows.Add(dr);
	return true;	
}

DataRow Used_GetProduct(string code)
{
	DataSet dsup = new DataSet();
	int rows = 0;

	string sc = "SELECT * FROM used_product WHERE id=" + code;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsup);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return null;
	}
	if(rows > 0)
		return dsup.Tables[0].Rows[0];
	return null;
}

string PrintOneKit(bool bButton, int nRow, ref double dRowPrice, 
	ref double dRowGST, ref double dRowTotal, ref double dRowSaving)
{
	DataRow dr = dtCart.Rows[nRow];
	if(!GetKit(dr["code"].ToString()))
		return "Get Kit Error";

	double dPrice = 0; 
	dPrice = m_dKitPrice;
	double dRetailPrice = dPrice;//double.Parse(drp["price"].ToString());

	int quantity = MyIntParse(dr["quantity"].ToString());
	double dTotal = dPrice * quantity;
	dTotal = Math.Round(dTotal, 2);
	dRowSaving *= quantity;
	
	StringBuilder sb = new StringBuilder();

	sb.Append("\r\n<tr bgcolor=aliceblue>");

	//delete button
	sb.Append("<td align=center valign=middle nowrap>");

	//delete button
	if(bButton)
	{
		sb.Append("<input type=button " + Session["button_style"].ToString() + " OnClick=window.location=('");
		sb.Append("cart.aspx?t=delete&row=" + nRow.ToString() + "&r=" + DateTime.Now.ToOADate());
		sb.Append("') value='");
		sb.Append("DELETE " + m_sKitTerm + "'>");
	}

	//quantity
	if(bButton)
	{
		sb.Append("<td><input type=text size=2 maxlength=3 name=Qty" + nRow.ToString() + " value=");
		sb.Append(dr["quantity"].ToString() + ">");
	}

	//code
	sb.Append("<td align=center valign=middle>");
	sb.Append(dr["code"].ToString());
	sb.Append("</td>\r\n");

	if(m_bOrder)
	{
		sb.Append("<td>&nbsp;</td>");
		sb.Append("<td>&nbsp;</td>");
	}

	bool bShowDetails = false;
	string sTitle = "Click to show details";
	if(Request.QueryString["sd"] == nRow.ToString())
	{
		bShowDetails = true;
		sTitle = "Click to hide details";
	}

	//description
	sb.Append("</td><td valign=middle><a href=cart.aspx");
	if(!bShowDetails)
		sb.Append("?sd=" + nRow.ToString());
	sb.Append(" class=o title='" + sTitle + "'>");
	sb.Append(m_sKitName);
	sb.Append("</a></td>\r\n");
	
	//price
	sb.Append("<td align='right' valign=middle nowrap>");
	sb.Append(dPrice.ToString("c"));
	sb.Append("</td>\r\n");

	//quantity
	if(!bButton)
	{
		sb.Append("<td align=center>" + dr["quantity"].ToString());
		sb.Append("</td>\r\n");
	}

	if(!m_bOrder)
	{
		sb.Append("<td align=right valign=middle nowrap>");
		sb.Append(dTotal.ToString("c"));
		sb.Append("</td>\r\n");
	}

	sb.Append("\r\n</tr>\r\n");

	dRowPrice = dPrice;
	dRowTotal = dTotal;

	if(!bShowDetails)
		return sb.ToString();

	for(int i=0; i<dskit.Tables["kit_item"].Rows.Count; i++)
	{
		DataRow drk = dskit.Tables["kit_item"].Rows[i];
		string code = drk["code"].ToString();
		string name = drk["name"].ToString();
		string qty = drk["qty"].ToString();
		sb.Append("<tr bgcolor=aliceblue><td>&nbsp;</td>");
		sb.Append("<td align=right>&nbsp;</td>");
		sb.Append("<td>&nbsp;</td>");
		sb.Append("<td colspan=3> x " + qty + " &nbsp&nbsp; " + name + "</td>");
		sb.Append("</tr>");
	}

	return sb.ToString();
}

string ReadDealerLevel(string card_id)
{
    if(dsLevel.Tables["dealer_level"] != null)
		dsLevel.Tables["dealer_level"].Clear();
    string sc = " SELECT dealer_level FROM card WHERE id ='" + card_id + "'";
    try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsLevel, "dealer_level") == 1)
			return dsLevel.Tables["dealer_level"].Rows[0]["dealer_level"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "1";
	}
	return "1";
    
}

string PrintFreightOptions(bool bOptions)
{
	string sBlank = "&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td>";
	//if(m_sSite != "www")
		//return sBlank;

	int nOptions = 0;
	string s = GetSiteSettings("number_of_freight_options", "0");
	try
	{
		nOptions = int.Parse(s);
	}
	catch(Exception e)
	{
	}

	if(nOptions <= 0)
		return sBlank;

	StringBuilder sb = new StringBuilder();
	StringBuilder sbo = new StringBuilder();
	//sbo.Append("<option value=0>Pick Up</option>");

	if(Request.QueryString["f"] != null)
	{
		try
		{
			m_dSessionFreight = double.Parse(Request.QueryString["f"].ToString());
			Session["ShippingFee"] = m_dSessionFreight;
			Session["freight"] = m_dSessionFreight;
		}
		catch(Exception e)
		{
		}
	}
    else
    {
        try
		{
			m_dSessionFreight = 3.8;
			Session["ShippingFee"] = m_dSessionFreight;
			Session["freight"] = m_dSessionFreight;
		}
		catch(Exception e)
		{
		}
    }

	if(m_dSessionFreight <= 0)	
	{
		if(Session["freight"] != null && Session["freight"] != "")
		{
			try
			{
				m_dSessionFreight = double.Parse(Session["freight"].ToString());
			}
			catch(Exception e)
			{
			}
		}
	}	
	Session["freight"] = m_dSessionFreight.ToString();
	Session["ShippingFee"] = m_dSessionFreight.ToString();
//DEBUG("fereight =", 	m_dSessionFreight);
//DEBUG("sear freign =", Session["freight"].ToString());
//DEBUG("sear shipping =", Session["ShippingFee"].ToString());
	for(int i=1; i<=nOptions && i<16; i++)
	{
		string sname = GetSiteSettings("freight_option_name" + i.ToString(), "option" + i.ToString());
		double dfreight = 0;
		s = GetSiteSettings("freight_option_price" + i.ToString(), "price" + i.ToString());
		try
		{
			dfreight = double.Parse(s);
		}
		catch(Exception e)
		{
		}
		sbo.Append("<option value=" + dfreight.ToString());
		if(m_dSessionFreight == dfreight)
			sbo.Append( " selected");
		sbo.Append(">" + sname + "</option>");
	}
	
	if(bOptions)
	{
		sb.Append("<div class=\"form-inline\"><b>Shipping By : </b>");
		sb.Append("<select class='form-control account-input' style='width:200px;' name=freight onchange=\"window.location=('cart.aspx?f='+this.options[this.selectedIndex].value)\">");
		//sb.Append("<option value=0>Pick Up</option>"); //CH 15.06.08
		sb.Append(sbo.ToString());
		sb.Append("</select></div>");
	}
	else
	{
		sb.Append("<b>Freight : </b>");
	}
	sb.Append("</td>");
	sb.Append("<td align=right valign=middle bgcolor=#ffffff nowrap>");
	if(bOptions)
		sb.Append(m_dSessionFreight.ToString("c"));
	sb.Append("</td>");
	sb.Append("<td align=right valign=middle bgcolor=#ffffff nowrap>");
	sb.Append(m_dSessionFreight.ToString("c"));
	sb.Append("</td>");

	return sb.ToString();

}
bool DeleteCart()
{
	if(m_sSite == "admin" || m_sSite == "www")
		return true;
	string card_id = Session["card_id"].ToString();
	string sc = " DELETE FROM cart WHERE card_id = " + card_id + " ";
//DEBUG("sc=", sc);	
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e1) 
	{
		ShowExp(sc, e1);
		return false;
	}
	return true;
}

bool SaveCart()
{
	if(m_sSite == "admin")// || m_sSite == "www")
		return true;
	CheckShoppingCart();
	string card_id = Session["card_id"].ToString();
	string sc = " DELETE FROM cart WHERE card_id = " + card_id + " ";
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		string site = dr["site"].ToString();
		string kid = dr["kid"].ToString();
		string kit = dr["kit"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string quantity = dr["quantity"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string salesPrice = dr["salesPrice"].ToString();
		string supplierPrice = dr["supplierPrice"].ToString();
		string barcode = dr["barcode"].ToString();
		string points = dr["points"].ToString();
		string discount_percent = dr["discount_percent"].ToString();
		string pack = dr["pack"].ToString();
		sc += " INSERT INTO cart (card_id, site, kid, kit, code, name, quantity, supplier, supplier_code, salesPrice, supplierPrice, barcode, points, discount_percent, pack) VALUES( ";
		sc += " " + card_id + " ";
		sc += ", '" + site + "' ";
		sc += ", '" + kid + "' ";
		sc += ", '" + kit + "' ";
		sc += ", '" + code + "' ";
		sc += ", N'" + EncodeQuote(name) + "' ";
		sc += ", '" + quantity + "' ";
		sc += ", N'" + EncodeQuote(supplier) + "' ";
		sc += ", N'" + EncodeQuote(supplier_code) + "' ";
		sc += ", '" + salesPrice + "' ";
		sc += ", '" + supplierPrice + "' ";
		sc += ", N'" + EncodeQuote(barcode) + "' ";
		sc += ", '" + points + "' ";
		sc += ", '" + discount_percent + "' ";
		sc += ", N'" + EncodeQuote(pack) + "' ";
		sc += ") ";
	}
	//DEBUG("sc",sc);
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e1) 
	{
		ShowExp(sc, e1);
		return false;
	}
	return true;
}
bool RestoreCart()
{
	if(m_sSite == "admin")
		return true;
	CheckShoppingCart();
	dtCart.Clear();
	string sc = " SELECT * from cart WHERE card_id = " + Session["card_id"].ToString();
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsCart, "data") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i=0; i<dsCart.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = dsCart.Tables["data"].Rows[i];
		string site = dr["site"].ToString();
		string kid = dr["kid"].ToString();
		string kit = dr["kit"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string quantity = dr["quantity"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string salesPrice = dr["salesPrice"].ToString();
		string supplierPrice = dr["supplierPrice"].ToString();
		string barcode = dr["barcode"].ToString();
		string points = dr["points"].ToString();
		string discount_percent = dr["discount_percent"].ToString();
		string pack = dr["pack"].ToString();

		DataRow drc = dtCart.NewRow();
		drc["supplierPrice"] = supplierPrice;
		drc["salesPrice"] = salesPrice;
		drc["barcode"] = barcode;
		drc["kid"] = kid;
		drc["kit"] = kit;
		drc["site"] = site;
		drc["code"] = code;
		drc["name"] = name;
		drc["quantity"] = quantity;
		drc["supplier"] = supplier;
		drc["supplier_code"] = supplier_code;
		drc["discount_percent"] = discount_percent;
		drc["pack"] = pack;
		dtCart.Rows.Add(drc);
	}
	return true;
}
</script>
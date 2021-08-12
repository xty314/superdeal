<!-- #include file="kit_fun.cs" -->
<!-- #include file="sales_function.cs" -->
<!-- #include file="credit_limit.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
const int m_cols = 8;	//how many columns main table has, used to write colspan=
string m_tableTitle = "";
string m_invoiceNumber = "";
string m_comment = "";	//Sales Comment in Invoice Table;
string m_custpo = "";
string m_salesNote = "";
string m_branchID = "1";
string m_sSalesType = ""; //as string, m_quoteType is receipt_type ID
string m_nShippingMethod = "1";
string m_specialShipto = "0";
string m_specialShiptoAddr = ""; //special
string m_pickupTime = "";

double m_dFreight = 0;

int m_nSearchReturn = 0;

bool b_create = false;
bool m_bCreditReturn = false;
bool m_bOrderCreated = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;

	//sales session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
		if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] == null)
		{
			PrepareNewSales();
			Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = Session[m_sCompanyName + "dealer_level"].ToString();
		}
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		PrepareNewSales();
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("corder.aspx" + par);
		return;
	}

	if(Request.QueryString["p"] == "new")
	{
		if(!PrepareNewSales())
			return;
		if(Request.QueryString["ft"] != null)
			m_sSalesType = Request.QueryString["ft"];
		if(m_sSalesType != null && m_sSalesType != "")
		{
			m_quoteType = GetEnumID("receipt_type", m_sSalesType);
			if(m_quoteType == "")
			{
				MsgDie("Error, sales type error, type <font color=red>" + m_sSalesType + "</font> not found, check your menu link");
			}
		}

		if(Request.QueryString["ft"] == "s")
			m_quoteType = "3";	//"3", indicating "invoice"
		else
		{
			if(Request.QueryString["ft"] == "o")
				m_quoteType = "2";	//"2", indicating "Order"
			else
			{
				if(Request.QueryString["ft"] == "q")
					m_quoteType = "1";	//"1", indicating "quote"
			}
		}
//		Session["SalesType"] = m_quoteType;
	}

	m_bOrder = false;
	Session["c_editing_order" + m_ssid] = true; //enter editing status

	Session[m_sCompanyName + "_ordering"] = null;
	Session[m_sCompanyName + "_salestype"] = "sales";
	if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] == null)
		Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = Session[m_sCompanyName + "dealer_level"].ToString();

	//Print SN in invoice Options
	if(Request.QueryString["psn"] == "y")
		Session["print_sn" + m_ssid] = "true";
	else if(Request.QueryString["psn"] == "n")
		Session["print_sn" + m_ssid] = null;
	if(Session["print_sn" + m_ssid] != null)
		m_bPrintSN = true;						

	m_bSales = true; //switch in cart.cs
	string s_url = Request.ServerVariables["URL"];// + "?" + Request.ServerVariables["QUERY_STRING"];
	if(Request.QueryString["ssid"] == null)
		s_url += "?ssid=" + m_ssid;
	else if(Request.ServerVariables["QUERY_STRING"] != null && Request.ServerVariables["QUERY_STRING"] != "")
		s_url += "?" + Request.ServerVariables["QUERY_STRING"];

//	m_sales = Session["card_id"].ToString(); //default

	//remember everything entered in Session Object
	if(Request.Form["custpo"] != null)
		UpdateAllFields();

	RestoreAllFields();

	//item search
	if(Request.Form["item_code_search"] != null && Request.Form["item_code_search"] != "")
	{

		Session["m_customer_po_number" + m_ssid] = m_custpo;
		Session["m_sales_note" + m_ssid] = m_salesNote;

		string s_SearchMsg = "notfound";
		if(s_SearchMsg == "found")
		{
			//found product by serial;
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
			return;
		}
		else if(s_SearchMsg == "notfound")
		{	
			if(IsInteger(Request.Form["item_code_search"]))
			{
				//true - means find exactly, false - mean find similar;
				if(!DoSearchItem(Request.Form["item_code_search"], true))
				{
					//if(!DoSearchItem(Request.Form["item_code_search"], false))	
					return;
				}
				if(m_nSearchReturn <= 0)
				{
					if(!DoSearchItem(Request.Form["item_code_search"], false))	
						return;
					
					if(m_nSearchReturn <=0 )
					{
						Response.Write("<h3>No Item's code matches <b>" + Request.Form["supplier_code_search"] + "</b></h3>");
						return;
					}

					LFooter.Text = m_sFooter;
					return;
				}
			}
			else
			{
				Response.Write("<b>Search Result of  <font size=+1 color=red>" + Request.Form["item_code_search"] + "</b></font>");
				Response.Write("<br><br><b>as S/N : Not Found!<br><br></b>");
				Response.Write("<b>as product code : Not Found --- Not Valid Product Code!</b>");
				return;
			}
		}
		else
		{
			Response.Write("<br><br><h3><b><font size=+1>Error:<font><br><br><br>" +s_SearchMsg+ "</b></h3>");
			return;
		}

		//only one matches, add to cart, refresh sales
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
		return;
	}	

	if(Request.QueryString["a"] == "add")
	{
		AddToCart(Request.QueryString["code"], Request.QueryString["supplier"], Request.QueryString["supplier_code"], "1", Request.QueryString["pri"]);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=corder.aspx?ssid=" + m_ssid + "\">");
		return;
	}

	if(m_sSalesType == "" && m_quoteType != "")
		m_sSalesType = GetEnumValue("receipt_type", m_quoteType);

	if(Request.QueryString["t"] == "vpq")
	{
		m_bOrder = false;
		Response.Write(BuildQInvoice("", m_quoteType, true, false, m_custpo, m_salesNote));//no email msg, inocie only, no system
		m_bOrder = true;
		return;
	}	

	bool bJustRestored = false;
	
	//order number
	if(Session["m_customer_po_number" + m_ssid] != null)
	{
		m_custpo = Session["m_customer_po_number" + m_ssid].ToString();
		m_salesNote = Session["m_sales_note" + m_ssid].ToString();
	}
	if(m_orderID != "")
		m_bOrderCreated = true;

	CheckShoppingCart();
	CheckUserTable();	//get user details if logged on

	if(Request.QueryString["t"] == "del")
	{
//		PrintHeaderAndMenu();
		PrintWWWHeader();
		bool bKeyOK = true;
		if(Session["delete_order_key" + m_ssid] == null)
			bKeyOK = false;
		else if(Session["delete_order_key" + m_ssid].ToString() != Request.QueryString["r"])
			bKeyOK = false;

		if(!bKeyOK)
		{
			Response.Write("<br><br><center><h3>Please follow the proper link to delete order.</h3>");
		}
		else
		{
			if(DoDeleteOrder())
			{
				Session["delete_order_key" + m_ssid] = null;
				Response.Write("<br><br><center><h3>Order Deleted.</h3>");
				Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('");
				Response.Write("status.aspx?t=1&r=" + DateTime.Now.ToOADate() + "') value='Back to Status'></center>");
				Session["c_editing_order"] = null; //leave editing status
				EmptyCart();
			}
			else
			{
				Response.Write("<br><br><h3>Error Deleting Order");
			}
		}
		LFooter.Text = m_sFooter;
		return;
	}
	//get old invoice
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_orderID = Request.QueryString["id"];

		if(!IsInteger(m_orderID))
		{
			Response.Write("<h3>ERROR, WRONG NUMBER</h3>");
			return;
		}
		if(!RestoreOrder())
			return;

		m_bOrderCreated = true;
		m_quoteType = GetEnumID("receipt_type", "order");

		Session["order_created" + m_ssid] = true;
		Session["SalesType" + m_ssid] = m_quoteType;
		Session["EditingOrder" + m_ssid] = true;
		Session["sales_current_order_number" + m_ssid] = m_orderNumber;
		Session["sales_current_order_id" + m_ssid] = m_orderID;
	}
	else if(Request.QueryString["ci"] != null)
	{
		m_customerID = Request.QueryString["ci"];
		GetCustomer();
		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=corder.aspx?i=" + m_orderID + "&ssid=" + m_ssid);
		Response.Write("\">");
		return;
	}
	else if(Request.Form["cmd"] == "Add More Items")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx?ssid=" + m_ssid + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Update Order")
	{
		dtCart = (DataTable)Session["ShoppingCart" + m_ssid];
		if(dtCart.Rows.Count <= 0)
		{
			PrintWWWHeader();
			Response.Write("<br><br><center><h3>Error. Cannot update empty order</h3>");
			Response.Write("<h3>Click Delete Order if you would like to delete this order</h3>");
			Response.Write("<form action=corder.aspx?ssid=" + m_ssid + " method=post>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=history.go(-1) value=' << Back '>");
			Response.Write(" <input type=submit " + Session["button_style"] + " name=cmd value='Delete Order'>");
			Response.Write("</form>");
			Response.Write("<br><br><br><br><br><br>");
			LFooter.Text = m_sFooter;
			return;
		}
//		PrintHeaderAndMenu();
		PrintWWWHeader();
		double dTotal = GetOrderTotal();
		string msg = "";
		bool bOnHold = false;
		if(!CreditLimitOK(Session["card_id"].ToString(), dTotal, ref bOnHold, ref msg))
		{
			Response.Write(msg);
		}
		else if(DoUpdateOrder())
		{
			Response.Write("<br><br><center><h3>Order Updated.</h3>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('");
			Response.Write("status.aspx?t=1&r=" + DateTime.Now.ToOADate() + "') value='Back to Status'></center>");
			Session["c_editing_order" + m_ssid] = null; //leave editing status
			EmptyCart();
		}
		else
		{
			Response.Write("<br><br><h3>Error updating order");
		}
		LFooter.Text = m_sFooter;
		return;
	}
	else if(Request.Form["cmd"] == "Delete Order")
	{
//		PrintHeaderAndMenu();
		PrintWWWHeader();
		string delkey = DateTime.Now.ToOADate().ToString();
		Session["delete_order_key" + m_ssid] = delkey;
		Response.Write("<script Language=javascript");
		Response.Write(">");
//		Response.Write(" rmsg = window.prompt('Are you sure you want to delete this order?')\r\n");
		Response.Write("if(window.confirm(' ");
		Response.Write("Are you sure you want to delete this order?         ");
		Response.Write("\\r\\nThis action cannot be undo.\\r\\n");
		Response.Write("\\r\\nClick OK to delete order.\\r\\n");
		Response.Write("'))");
		Response.Write("window.location='corder.aspx?ssid=" + m_ssid + "&t=del&id=" + m_orderID + "&r=" + delkey + "';\r\n");
		Response.Write("else window.location='corder.aspx?ssid=" + m_ssid + "&id=" + m_orderID + "&r=" + delkey + "';\r\n");
		Response.Write("</script");
		Response.Write(">");
		return;
	}
	else if(Request.Form["cmd"] == "Leave without Changes")
	{
		//unlock it
		if(m_orderID != "")
		{
			String sc = "UPDATE orders SET locked_by=null, time_locked=null WHERE id=" + m_orderID;
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
				return;
			}
		}
		Session["c_editing_order" + m_ssid] = null; //leave editing status
		EmptyCart();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=status.aspx?t=1&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Record")
	{
		dtCart = (DataTable)Session["ShoppingCart" + m_ssid];
		if(dtCart.Rows.Count <= 0)
		{
			PrintWWWHeader();
			Response.Write("<br><br><center><h3>Error. Cannot create empty order</h3>");
			LFooter.Text = m_sFooter;
			return;
		}

		m_custpo = Request.Form["custpo"];
		m_custpo = m_custpo.Replace("\"", "-");
		m_custpo = m_custpo.Replace("'", "-");
		m_salesNote = Request.Form["note"];
		
		Session["m_customer_po_number" + m_ssid] = m_custpo;
		Session["m_sales_note" + m_ssid] = m_salesNote;

		b_create = true; //MyMoneyParse(Request.Form["totaldue"]) > 0;
		if(b_create)
		{
			if(!DoCreateOrder(false, m_custpo, m_salesNote)) // false means not system quotation
			{
				Response.Write("<h3>ERROR CREATING QUOTE</h3>");
				return;
			}
			m_bOrderCreated = true;
			Session["order_created" + m_ssid] = true;
			
//			PrintHeaderAndMenu();
			PrintWWWHeader();
			Response.Write("<br><br><center><h3>" + m_sSalesType.ToUpper() + " Created</h3>");
			Response.Write("<h5>Number : </h5><h1><font color=red>" + m_orderID + "</h1><br>");
			Response.Write("<input type=button " + Session["button_style"]);
			Response.Write("onclick=window.location=('eorder.aspx?id=" + m_orderID + "&r=" + DateTime.Now.ToOADate() + "') ");
			Response.Write(" value='Process'>");
			Response.Write("<br><br><br><br><br>");
//			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eorder.aspx?id=" + m_orderID + "&r=" + DateTime.Now.ToOADate() + "\">");
			LFooter.Text = m_sFooter;
			return;
		}
	}

	if(m_orderNumber != "")
		m_tableTitle = m_sSalesType.ToUpper() + " # " + m_orderNumber + "<font color=red> LOCKED TO EDIT !!</font>";
	else
		m_tableTitle = "NEW " + m_sSalesType.ToUpper();

//	PrintHeaderAndMenu();
	PrintWWWHeader();
	MyDrawTable();
	LFooter.Text = m_sFooter;
}

bool RestoreCustomer()
{
	string status_bp = GetEnumID("order_item_status", "Being Processed");
	if(status_bp == "")
	{
		Response.Write("<br><br><center><h3>Error getting status ID 'Being Processed'");
		return false;
	}

	string sc = " SELECT branch, number, po_number, card_id, freight, shipping_method ";
	sc += ", special_shipto, shipto, pick_up_time ";
	sc += ", sales, sales_note, locked_by, time_locked ";
	sc += " FROM orders ";
	sc += " WHERE card_id=" + Session["card_id"] + " AND status=" + status_bp;
	sc += " AND id=" + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "order") <= 0)
		{
			PrintHeaderAndMenu();
			Response.Write("<br><br><center><h3>Order Not Found</h3><br><br><br><br>");
			PrintFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr =  dst.Tables["order"].Rows[0];
	string locker = dr["locked_by"].ToString();
	Trim(ref locker);
	if(locker != "")
	{
		if(locker != Session["card_id"].ToString())
		{
			string lockname = TSGetUserNameByID(locker);
			string locktime = DateTime.Parse(dr["time_locked"].ToString()).ToString("dd-MM-yyyy HH:mm");
			PrintHeaderAndMenu();
			Response.Write("<br><br><center><h3><font color=red>ORDER LOCKED</font></h3><br>");
			Response.Write("<h4>This order is locked by <font color=blue>" + lockname.ToUpper() + "</font> since " + locktime);
			LFooter.Text = m_sFooter;
			return false;
		}
	}

	//lock it
	sc = " UPDATE orders SET locked_by=" + Session["card_id"].ToString();
	sc += ", time_locked=GETDATE() ";
	sc += " WHERE id="+ m_orderID + " ";
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

	m_branchID = dr["branch"].ToString();
	if(m_branchID == "")
		m_branchID = "1";

	m_orderNumber = dr["number"].ToString();
	m_customerID = dr["card_id"].ToString();
	m_customerName = TSGetUserCompanyByID(m_customerID);
	if(m_customerName == "")
		m_customerName = TSGetUserNameByID(m_customerID);
	m_custpo = dr["po_number"].ToString();

	m_dFreight = MyDoubleParse(dr["freight"].ToString());
	m_sales = dr["sales"].ToString();
	m_salesNote = dr["sales_note"].ToString();

	string sst = dr["special_shipto"].ToString();
	if(sst != "" && bool.Parse(sst))
		m_specialShipto = "1";
	m_specialShiptoAddr = dr["shipto"].ToString();
	m_nShippingMethod = dr["shipping_method"].ToString();
	m_pickupTime = dr["pick_up_time"].ToString();

	Session["sales_current_freight" + m_ssid] = m_dFreight;
	Session["sales_customerid" + m_ssid] = m_customerID;
	Session["sales_shipping_method" + m_ssid] = m_nShippingMethod ;
	Session["sales_special_shipto" + m_ssid] = m_specialShipto;
	Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
	Session["sales_pick_up_time" + m_ssid] = m_pickupTime;
	
	return true;
}

bool RestoreOrder()
{
	PrepareNewSales(); //empty session sales data 

	if(!RestoreCustomer())
		return false;

	int items = 0;
	string sc = "SELECT * FROM order_item WHERE id=" + m_orderID + " AND kit=0 ";;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		items = myAdapter.Fill(dst, "items");
//		if(items <= 0)
//		{
//			Response.Write("<br><br><center><h3>ERROR, no order item found</h3>");
//			return false;
//		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int i = 0;
	for(i=0; i<items; i++)
	{
		DataRow dr = dst.Tables["items"].Rows[i];
		if(!AddToCart(dr["code"].ToString(), dr["supplier"].ToString(), dr["supplier_code"].ToString(), 
			dr["quantity"].ToString(), dr["supplier_price"].ToString(), dr["commit_price"].ToString(), 
			"", "") )
			return false;
	}

	//get kit
	sc = "SELECT * FROM order_kit WHERE id=" + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		items = myAdapter.Fill(dst, "kit");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(i=0; i<items; i++)
	{
		DataRow dr = dst.Tables["kit"].Rows[i];
		string kit_id = dr["kit_id"].ToString();
		string qty = dr["qty"].ToString();
		if(!DoAddKit(kit_id, MyIntParse(qty)))
			return false;
	}

	return true;
}

bool DoSearchItem(string kw, bool btype)
{
	string sc = "SELECT code, supplier, supplier_code, name, ISNULL(supplier_price, 0) AS supplier_price ";
	sc += " FROM product WHERE code ";// LIKE '"+ kw;

//DEBUG(" search = ", Request.Form["item_code_search"]);
	if(!btype)
	{
		sc += " LIKE '" + kw + "%'";
	}
	else
		sc += " = '" + kw + "'";
//DEBUG("sc=", sc);
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

//DEBUG(" SQL= ", sc);
//DEBUG(" Return Rows = ", m_nSearchReturn);

	if(m_nSearchReturn == 1)
	{
		DataRow dr = dst.Tables["isearch"].Rows[0];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), Session["card_id"].ToString());
		AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), "", "");
		
		string s_url = Request.ServerVariables["URL"];// + "?" + Request.ServerVariables["QUERY_STRING"];
		if(Request.QueryString["ssid"] == null)
			s_url += "?ssid=" + m_ssid;
		else if(Request.ServerVariables["QUERY_STRING"] != null && Request.ServerVariables["QUERY_STRING"] != "")
			s_url += "?" + Request.ServerVariables["QUERY_STRING"];

		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
		return false;
	}
	else if(m_nSearchReturn > 1)
	{
		Response.Write("<center><h3>Search Result For " + Request.Form["item_code_search"] + "</h3></center>");
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

void BindISTable()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>ProductID</td>\r\n");
	Response.Write("<td>Description</td>\r\n");
	Response.Write("<td>Supplier</td>\r\n");
	Response.Write("<td>SupplierCode</td>\r\n");
	
	Response.Write("<td>SupplierPrice</td>\r\n");
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
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string price = dr["supplier_price"].ToString();
		Response.Write("<tr" + scolor + ">");

		//Link on add items' Product ID
		Response.Write("<td><a href='corder.aspx?ssid=" + m_ssid + "&a=add&code="+code+"&supplier="+supplier);
		Response.Write("&supplier_code=" + HttpUtility.UrlEncode(supplier_code) + "&pri=" + price + "&r=" + DateTime.Now.ToOADate() + "'>");
		Response.Write(code + "</a></td>\r\n");

		//Link to add items to sales from search result
		Response.Write("<td><a href='corder.aspx?ssid=" + m_ssid + "&a=add&code="+code+"&supplier="+supplier);
		Response.Write("&supplier_code=" + HttpUtility.UrlEncode(supplier_code) + "&pri=" + price + "&r=" + DateTime.Now.ToOADate() + "'>");
		Response.Write(name + "</a></td>\r\n");

		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + supplier_code+ "</td>");
		
		Response.Write("<td>" + double.Parse(price).ToString("c") + "</td>");
		Response.Write("</tr>");
	}	
	Response.Write("</table>");
}

void RestoreAllFields()
{
	if(Session["sales_type_credit" + m_ssid] != null)
	{
		m_bCreditReturn = (bool)Session["sales_type_credit" + m_ssid];
		m_quoteType = "6";//GetEnumID("receipt_type", "credit note");
	}

	if(Session["sales_freight" + m_ssid] != null && Session["sales_freight" + m_ssid] != "")
		m_dFreight = MyDoubleParse(Session["sales_freight" + m_ssid].ToString());

	if(Session["SalesType" + m_ssid] != null && Session["SalesType" + m_ssid].ToString() != "")
		m_quoteType = Session["SalesType" + m_ssid].ToString();

	if(Session["sales_customerid" + m_ssid] != null && Session["sales_customerid" + m_ssid].ToString() != "")
		m_customerID = Session["sales_customerid" + m_ssid].ToString();

	if(Session["order_created" + m_ssid] != null)
		m_bOrderCreated = true;
	if(Session["m_sales_note" + m_ssid] != null)
		m_salesNote = Session["m_sales_note" + m_ssid].ToString();
	if(Session["sales_current_order_number" + m_ssid] != null)
		m_orderNumber = Session["sales_current_order_number" + m_ssid].ToString();
	if(Session["sales_current_order_id" + m_ssid] != null)
		m_orderID = Session["sales_current_order_id" + m_ssid].ToString();
	
	if(Session["sales_shipping_method" + m_ssid] != null && Session["sales_shipping_method" + m_ssid].ToString() != "")
		m_nShippingMethod = Session["sales_shipping_method" + m_ssid].ToString();
	if(Session["sales_special_shipto" + m_ssid] != null && Session["sales_special_shipto" + m_ssid].ToString() != "")
		m_specialShipto = Session["sales_special_shipto" + m_ssid].ToString();
	if(Session["sales_special_ship_to_addr" + m_ssid] != null && Session["sales_special_ship_to_addr" + m_ssid].ToString() != "")
		m_specialShiptoAddr = Session["sales_special_ship_to_addr" + m_ssid].ToString();
	if(Session["sales_pick_up_time" + m_ssid] != null && Session["sales_pick_up_time" + m_ssid].ToString() != "")
		m_pickupTime = Session["sales_pick_up_time" + m_ssid].ToString();
}

bool UpdateAllFields()
{
	m_custpo = Request.Form["custpo"];
	m_salesNote = Request.Form["note"];
	m_branchID = Request.Form["branch"];
	m_nShippingMethod = Request.Form["shipping_method"];
	m_specialShipto = "0";
	if(Request.Form["special_shipto"] == "on")
		m_specialShipto = "1";
	m_specialShiptoAddr = Request.Form["special_ship_to_addr"];
	m_pickupTime = Request.Form["pickup_time"];

	Session["m_customer_po_number" + m_ssid] = m_custpo;
	Session["m_sales_note" + m_ssid] = m_salesNote;
	Session["brach_id" + m_ssid] = m_branchID;
	Session["sales_shipping_method" + m_ssid] = m_nShippingMethod;
	Session["sales_special_shipto" + m_ssid] = m_specialShipto;
	Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
	Session["sales_pick_up_time" + m_ssid] = m_pickupTime;

	if(Session["ShoppingCart" + m_ssid] == null)
		return true;

	dtCart = (DataTable)Session["ShoppingCart" + m_ssid];

	dtCart.AcceptChanges(); //Commits all the changes made to this row since the last time AcceptChanges was called
	int quantity = 0;
	int quantityOld = 0;
	double dPrice = 0;
	double dPriceOld = 0;
	double dTotal = 0;
	int rows = dtCart.Rows.Count;

	for(int i=rows-1; i>=0; i--)
	{
//DEBUG("site="+dtCart.Rows[i]["site"].ToString(), " i="+i.ToString());
		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
		{
			if(dtCart.Rows[i]["system"] == "1")
				continue;
			string kit = dtCart.Rows[i]["kit"].ToString();
			string sqty_old = Request.Form["qty_old"+i.ToString()];
			string sqty = Request.Form["qty"+i.ToString()];
			string sprice_old = Request.Form["price_old"+i.ToString()];
			string sprice = Request.Form["price"+i.ToString()];
			dPriceOld = MyMoneyParse(sprice_old);
			quantity = MyIntParse(sqty);
			if(quantity == 0 || Request.Form["del" + i.ToString()] == "Remove") //do delete
			{
				dtCart.Rows.RemoveAt(i);
				continue;
			}
			else if(quantity < 0)
			{
				m_bCreditReturn = true;
				Session["sales_type_credit" + m_ssid] = true;
				m_quoteType = "6";//GetEnumID("receipt_type", "credit note");
			}
			else if(m_bCreditReturn)
				quantity = 0 - quantity; //asume all items follw up are for credit

			if(!TSIsDigit(sprice))
				dPrice = dPriceOld;
			else
				dPrice = MyMoneyParse(sprice);

			if(quantity != quantityOld)
			{
				dtCart.Rows[i].BeginEdit();
				dtCart.Rows[i]["quantity"] = quantity;
				if(dPrice != dPriceOld)
				{
					dtCart.Rows[i]["salesPrice"] = dPrice.ToString();
				}
				else
				{
					double dQtyPrice = dPrice;
					if(kit != "1")
						dQtyPrice = GetSalesPriceForDealer(dtCart.Rows[i]["code"].ToString(), sqty, Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), Session["card_id"].ToString());
					dtCart.Rows[i]["salesPrice"] = dQtyPrice.ToString();
				}
				dtCart.Rows[i].EndEdit();			
			}
		}
	}
	dtCart.AcceptChanges(); //Commits all the changes made to this row since the last time AcceptChanges was called

	return true;
}

bool MyDrawTable()
{
	if(!GetCustomer())
		return false;

	bool bRet = true;
	
	PrintJavaFunction();

	Response.Write("<form name=form1 action=corder.aspx?ssid=" + m_ssid + " method=post>");
	Response.Write("<table width=100% height=100% bgcolor=white align=center valign=center><tr><td valign=top>");
	
	Response.Write("<center><h3>" + m_tableTitle + "</h3></center>");
	
	Response.Write("<table width=90% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	//customer
	Response.Write("<input type=hidden name=customer value=" + m_customerID + ">");
	Response.Write("<input type=hidden name=branch value=" + m_branchID + ">");
	Response.Write("<input type=hidden name=customer value=" + m_customerID + ">");
	Response.Write("<tr><td>");
	Response.Write("<table><tr><td>");
	Response.Write("<b>P.O.Number : &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;</b></td>");
	Response.Write("<td><input type=editbox name=custpo value='" + m_custpo + "'>");
	Response.Write("</td></tr></table>");
	
	Response.Write("</td></tr>");
	Response.Write("</table><br>");


	Response.Write("<table width=90%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=" + m_cols + ">");

	if(!PrintShipToTable())
		return false;

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=" + m_cols + ">");

	if(!PrintCartItemTable())
		return false;

	Response.Write("</td></tr>");

	//start comment table
	Response.Write("<tr><td>&nbsp</td></tr><tr><td><b>&nbsp;Note : </b></td></tr>");
	Response.Write("<tr><td><textarea name=note cols=70 rows=5>" +m_salesNote+ "</textarea></td>");
	Response.Write("</tr>");
	//end comment table

	Response.Write("</td></tr>\r\n");
	
	Response.Write("</table></form>");
	return bRet;
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
}

bool PrintCartItemTable()
{
	CheckShoppingCart();
	int i = 0;

	Response.Write("<br>");
	Response.Write("<table border=0 cellpadding=0 width=100% cellspacing=0>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=130 align=left>STOCK ID</td>");
	Response.Write("<td width=70>CODE</td>");
	Response.Write("<td>DESCRIPTION</td>");
	Response.Write("<td align=right>PRICE</td>");
	Response.Write("<td align=right>STOCK</td>");
	Response.Write("<td align=right>QTY</td>");
	Response.Write("<td align=right>TOTAL</td>");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("</tr>");

	dTotalPrice = 0;
	dTotalGST = 0;
	dAmount = 0;
	dTotalSaving = 0;

	double dCost = 0;
	double dRowPrice = 0;
	double dRowGST = 0;
	double dRowTotal = 0;
	double dRowSaving = 0;

	//build up row list
	for(i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;

		DataRow dr = dtCart.Rows[i];
		string code = dr["code"].ToString();

		double dsupplierPrice = 0;

		int quantity = int.Parse(dr["quantity"].ToString());

		double dSalesPrice = 0;
		try
		{
			dSalesPrice = double.Parse(dr["salesPrice"].ToString());
		}
		catch(Exception e)
		{
		}
		dSalesPrice = Math.Round(dSalesPrice, 2);
//DEBUG("price=", dr["salesPrice"].ToString());
		dRowTotal = dSalesPrice * quantity;
		dRowTotal = Math.Round(dRowTotal, 2);

		string s_prodName = dr["name"].ToString();

		string supplierCode = dr["supplier_code"].ToString();
		if(m_bOrder)
			SSPrintOneRow(i, dr["code"].ToString(), supplierCode, s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString());
		else
			SSPrintOneRow(i, dr["code"].ToString(), dr["code"].ToString(), s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString());

		dTotalPrice += dRowTotal;
		dCost += dsupplierPrice;
	}
	
	//put an empty row for user input, which is used to search product by code or SN;
//	if(m_quoteType != "3" || Request.QueryString["p"] == "new")
	Response.Write("<tr>");
//	Response.Write("<td><input type=text name=item_code_search size=8 value=''></td>");
	Response.Write("<tr><td colspan=8 align=right><input type=submit name=cmd " + Session["button_style"] + " size=8 value='Add More Items'>");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Update Quantity'></td>");
	Response.Write("</tr>");
//	Response.Write("<tr><td><input type=submit " + Session["button_style"] + " name=cmd size=8 value='Select From Categories'></td></tr>");
	
	if(Request.Form["freight"] != null && Request.Form["freight"] != "")
	{
//DEBUG("Freight = ", Request.Form["freight"]);
		m_dFreight = double.Parse(Request.Form["freight"], NumberStyles.Currency, null);
		Session["sales_freight" + m_ssid] = m_dFreight;
	}
	dTotalPrice = Math.Round(dTotalPrice + m_dFreight, 2);

	double dFinal = dTotalPrice;
	if(Request.Form["total"] != null && Request.Form["total"] != "")
	{
		if(Request.Form["total"] != Request.Form["total_old"])
			dFinal = double.Parse(Request.Form["total"], NumberStyles.Currency, null);
	}
	double discount = Math.Round((dTotalPrice - dFinal) / dTotalPrice * 100, 0);

	if(m_bOrder)
		discount = 0;

	double dGstRate = MyDoubleParse(Session["gst_rate"].ToString());
	dTotalGST = dTotalPrice * dGstRate;
	dTotalGST = Math.Round(dTotalGST, 2);

	dAmount = dTotalPrice + dTotalGST;

	Response.Write("<tr bgcolor=#ffffff><td colspan=4>&nbsp;</td>");
	Response.Write("<td align=right>");
	Response.Write("&nbsp;");
	Response.Write("</td></tr>");

	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-3).ToString() + ">&nbsp;</td>");
	Response.Write("<td>&nbsp;</td>");

	//freight
//	Response.Write("<td align=right><b>Freight : </b></td>");
//	Response.Write("<td align=right>");
	Response.Write("<input type=hidden name=freight value='");
	Response.Write(m_dFreight + "'>");
//	Response.Write("</td></tr>");

	//sub total
	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<b>Sub-Total : </b></td><td align=right>");
	Response.Write(dTotalPrice.ToString("c"));
	Response.Write("</td></tr>");

	//total GST
	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<b>TAX : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write(dTotalGST.ToString("c"));
	Response.Write("</b></td></tr>");

	//total amount due
	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<b>Total Amount Due : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write(dAmount.ToString("c"));
	Response.Write("</b><input type=hidden name=totaldue value='" + dAmount.ToString("c") + "'</td></tr>");

	Response.Write("<tr bgcolor=#ffffff><td colspan=" + m_cols + " align=right>");

	Response.Write("<tr><td>");
	Response.Write("&nbsp;</td><td colspan=" + (m_cols - 1).ToString() + " align=right>");
	if(m_bOrderCreated)
	{
		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Delete Order'>");
		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Update Order'>");
		Response.Write("<input type=hidden name=order_id value=" + m_orderID + ">");
	}
//	else
//		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Record'>");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Leave without Changes'>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=" + m_cols + " align=right><font color=red><b>");
	Response.Write("Important : Click 'Update Order' or 'Leave without Changes' to unlock <br>your order before leaving this page!!");
	Response.Write("</b></font></td></tr>");

	Response.Write("</table>");
	return true;
}

bool SSPrintOneRow(int nRow, string sID, string code, string desc, double dCost, double dPrice, int qty, double dTotal, string sSNnum)
{
	string stock = "";
	if(IsInteger(sID))
		stock = GetProductStock(sID);

	Response.Write("<tr ");
	if(bCartAlterColor)
		Response.Write("bgcolor=#EEEEEE");
	else
		Response.Write("bgcolor=white");
	bCartAlterColor = !bCartAlterColor;

	Response.Write(">");

	//Online Store ID code;
	Response.Write("<td>");
	Response.Write(sID);
	Response.Write("</td>\r\n");

	//code
	Response.Write("<td>");
	Response.Write(code);
	Response.Write("</td>\r\n");

	//description
	Response.Write("<td><a href=p.aspx?");
	Response.Write(sID);
	Response.Write(" class=d target=_blank>");
	Response.Write(desc);
	Response.Write("</a></td>\r\n");

	//price
	Response.Write("<td>" + dPrice.ToString("c") + "</td>");
	Response.Write("<input type=hidden name=price" + nRow.ToString() + " value=" + dPrice + ">");
	Response.Write("<input type=hidden name=price_old" + nRow.ToString() + " value='");
	Response.Write(dPrice);
	Response.Write("'></td>\r\n");

	//current stock
	Response.Write("<td align=right>" + stock + "</td>");

	//quantity
	Response.Write("<td align=right><input type=text size=3 autocomplete=off style='text-align:right' name=qty" + nRow.ToString());
	Response.Write(" value='" + qty.ToString() + "'>");
	Response.Write("<input type=hidden name=qty_old" + nRow.ToString() + " value='" + qty.ToString() + "'></td>\r\n");

	//total
	Response.Write("<td align=right>");
	Response.Write(dTotal.ToString("c"));
	Response.Write("</td>\r\n");

	//total
	Response.Write("<td align=right>");
	Response.Write("<input type=submit name=del" + nRow + " " + Session["button_style"] + " value=Remove>");
	Response.Write("</td>\r\n</tr>\r\n");
	return true;
}

bool PrintShipToTable()
{
	DataRow dr = null;
	bool bCashSales = false;
	if(Session["sales_customerid" + m_ssid] == null)
	{
		bCashSales = true;
	}
	else
	{
		GetCustomer();
		if(dst.Tables["card"].Rows.Count > 0)
			dr = dst.Tables["card"].Rows[0];
		else
			bCashSales = true;
	}

	Response.Write("<table width=100% align=center><tr><td valign=top>");

	//ship to 
	Response.Write("<table>");

	//shipping method
	Response.Write("<tr><td valign=top>");
	
	Response.Write("<b>Shipping Method : </b>");
	Response.Write("<select name=shipping_method onchange=\"OnShippingMethodChange();\">");
	if(bCashSales)
		Response.Write("<option value=1>PICK UP</option>");
	else
		Response.Write(GetEnumOptions("shipping_method", m_nShippingMethod));
	Response.Write("</select>");

	Response.Write("<table align=right id=tPT");
	if(m_nShippingMethod != "1")
		Response.Write(" style='visibility:hidden' ");
	Response.Write("><tr><td>");
	Response.Write("Pick Up Time : <input type=text size=10 name=pickup_time value=\"" + m_pickupTime + "\">");
	Response.Write("</td></tr></table>");

	Response.Write("</td>");
	Response.Write("<td valign=top>");

	Response.Write("<table id=tShipTo");
	if(m_nShippingMethod == "1") //pickup
		Response.Write(" style='visibility:hidden' ");
	Response.Write(">");

	string sAddr = "";
	string sCompany = "";
	string sContact = "";
	if(!bCashSales)
	{
		if(dr["Address1"].ToString() != "")
		{
			sAddr = dr["Address1"].ToString();
			sAddr += "<br>";
			sAddr += dr["Address2"].ToString();
			sAddr += "<br>";
			sAddr += dr["address3"].ToString();
		}
		else
		{
			sAddr = dr["Address1B"].ToString();
			sAddr += "<br>";
			sAddr += dr["Address2B"].ToString();
			sAddr += "<br>";
			sAddr += dr["CityB"].ToString();
		}

		sCompany = dr["trading_name"].ToString();

		if(sCompany == "")
		{
			if(dr["Name"].ToString() != "")
				sCompany = dr["Name"].ToString();
			else
				sCompany = dr["NameB"].ToString();
		}
		else //if we have company name, then put contact person name here as well
		{
			if(dr["Name"].ToString() != "")
				sContact = dr["Name"].ToString();
			else
				sContact = dr["NameB"].ToString();
		}
	}

	sAddr = sCompany + "<br>\r\n" + sAddr + "<br>\r\n";

	Response.Write("<tr><td><b>Ship To:</b>");
	Response.Write(" <input type=checkbox name=special_shipto ");
	if(m_specialShipto == "1")
		Response.Write(" checked");
	Response.Write(" onclick=\"OnSpecialShiptoChange();\">Special Shipping Address : ");
//	Response.Write("<br>\r\n");

	Response.Write("<table><tr><td valign=top>");

	Response.Write("<table id=tshiptoaddr ");
	if(m_specialShipto == "1")
		Response.Write(" style='visibility:hidden' ");
	Response.Write("><tr><td>");
	Response.Write(sAddr);
	Response.Write("</td></tr></table>");

	Response.Write("</td><td valign=top>");
	
	Response.Write("<table id=ssta ");
	if(m_specialShipto != "1")
		Response.Write(" style='visibility:hidden' ");
	Response.Write("><tr><td>");
	Response.Write("<textarea name=special_ship_to_addr cols=30 rows=4>");
	Response.Write(m_specialShiptoAddr);
	Response.Write("</textarea>");
	Response.Write("</td></tr></table>");

	Response.Write("</td></tr></table>");

	if(m_specialShipto == "1")
	{
	}
	else if(!bCashSales)
	{
	}

	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
	//end of ship to

	Response.Write("</td></tr></table>");
	//end of bill and shipto table
	return true;
}

bool CreateOrder(string branch_id, string card_id, string po_number, string special_shipto, string shipto, 
				 string shipping_method, string pickup_time, string contact, string sales_id, string sales_note, 
				 ref string order_number)
{
	if(!CheckBottomPrice())
		return false;

	DataSet dsco = new DataSet();
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, card_id, po_number, freight) VALUES(0, " + card_id + ", '";
	sc += po_number + "', " + double.Parse(Request.Form["freight"], NumberStyles.Currency, null);
	sc += ") SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dsco, "id") == 1)
		{
			m_orderID = dsco.Tables["id"].Rows[0]["id"].ToString();
			m_orderNumber = m_orderID; //new order, same
			//assign ordernumber same as id
			sc = "UPDATE orders SET number=" + m_orderNumber + ", branch=" + branch_id + ", sales_note='" + sales_note + "' ";
			if(special_shipto == "1")
				sc += ", special_shipto=1, shipto='" + shipto + "' ";
			sc += ", contact='" + contact + "' ";
			if(sales_id != "")
				sc += ", sales=" + sales_id;
			sc += ", shipping_method=" + shipping_method;
			sc += ", pick_up_time='" + EncodeQuote(pickup_time) + "' ";
			sc += " WHERE id=" + order_number;
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
			Response.Write("<br><br><center><h3>Create Order failed, error getting new order number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(!WriteOrderItems(m_orderID))
		return false;
	return true;
}

bool DoCreateOrder(bool bSystem, string sCustPONumber, string sSalesNote)
{
	string branch = Request.Form["branch"];
	string contact = "";
	return CreateOrder(branch, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), ref m_orderID);
}

bool CheckBottomPrice()
{
	if(!MyBooleanParse(GetSiteSettings("enable_bottom_price_check", "1")))
		return true;

	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		string code = dtCart.Rows[i]["code"].ToString();
		string supplier_code = dtCart.Rows[i]["supplier_code"].ToString();
		if(supplier_code.ToLower() == "ac200") //temperory code for unknow product
			return true;
		if(dtCart.Rows[i]["kit"].ToString() == "1")
			continue; //ignore kit price

		double dPriceCheck = MyDoubleParse(dtCart.Rows[i]["salesPrice"].ToString());

		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			Response.Write("<br><br><center><h3>Product not found");
		double dBottomPrice = Math.Round(MyDoubleParse(drp["price"].ToString()), 2);
		if(dPriceCheck < dBottomPrice)
		{
			PrintHeaderAndMenu();
			Response.Write("<br><br><center><h5><font color=red>Bottom Price Protection</font><br><br>");
			Response.Write("Product, code:" + code + ", invalid price:" + dPriceCheck.ToString("c") + "<br><br>");
			Response.Write("Please contact our sales.</h5>");
//			Response.Write("<br>Product Code : " + code);
//			Response.Write("<br>Description : " + drp["name"].ToString());
//			Response.Write("<br>BottomPrice : " + dBottomPrice.ToString("c"));
//			Response.Write("<br>Sales Price : " + dPriceCheck.ToString("c"));
			return false;
		}
	}
	return true;
}

bool DeleteOrderKits(string m_orderID)
{
	string sc = " DELETE FROM order_kit WHERE id=" + m_orderID;
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
	return true;
}

bool DoDeleteOrder()
{
	DeleteOrderItems(m_orderID);
	string sc = " DELETE FROM orders WHERE id=" + m_orderID;
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
	return true;
}

bool DoUpdateOrder()
{
	if(!CheckBottomPrice())
		return false;

	m_orderID = Request.Form["order_id"];

	string sc = "UPDATE orders SET ";
	sc += " branch=" + m_branchID;
	sc += ", card_id=" + m_customerID;
	sc += ", po_number='" + EncodeQuote(m_custpo) + "' ";
	sc += ", sales_note='" + EncodeQuote(m_salesNote) + "' ";
	sc += ", shipping_method=" + m_nShippingMethod;
	sc += ", special_shipto=" + m_specialShipto;
	sc += ", shipto='" + EncodeQuote(m_specialShiptoAddr) + "' ";
	sc += ", pick_up_time='" + EncodeQuote(m_pickupTime) + "' ";
	sc += ", locked_by=null, time_locked=null "; //unlock
	sc += " WHERE id=" + m_orderID;

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
	
	DeleteOrderItems(m_orderID);
	WriteOrderItems(m_orderID);
	return true;
}

bool DeleteOrderItems(string m_orderID)
{
	if(!DeleteOrderKits(m_orderID))
		return false;

	int items = 0;
	string sc = " SELECT code, quantity FROM order_item WHERE id=" + m_orderID;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		items = myCommand1.Fill(dst, "delete_items");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";
	for(int i=0; i<items; i++)
	{
		DataRow dr = dst.Tables["delete_items"].Rows[i];
		string code = dr["code"].ToString();
		string sqty = dr["quantity"].ToString();
		sc += " UPDATE product SET allocated_stock=allocated_stock - " + sqty + " WHERE code=" + code;
		sc += " Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock - " + sqty;
		sc += " WHERE code=" + code + " AND branch_id = " + m_branchID;
	}
	sc += " DELETE FROM order_item WHERE id=" + m_orderID;
//DEBUG("del sc=", sc);
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
	return true;
}

bool WriteOrderItems(string order_id)
{
	CheckShoppingCart();

	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		string kit = dr["kit"].ToString();

		double dPrice = MyDoubleParse(dr["salesPrice"].ToString());

		string name = EncodeQuote(dr["name"].ToString());
		if(name.Length > 255)
			name = name.Substring(0, 255);

		if(kit == "1")
		{
			RecordKitToOrder(order_id, dr["code"].ToString(), name, dr["quantity"].ToString(), dPrice, m_branchID);
			continue;
		}

		string sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
		sc += ", commit_price) VALUES(" + order_id + ", " + dr["code"].ToString() + ", ";
		sc += dr["quantity"].ToString() + ", '" + name + "', '" + dr["supplier"].ToString();
		sc += "', '" + dr["supplier_code"].ToString() + "', " + Math.Round(MyDoubleParse(dr["supplierPrice"].ToString()), 2);
		sc += ", " + Math.Round(MyDoubleParse(dr["salesPrice"].ToString()), 2) + ") ";

		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + dr["code"].ToString();
		sc += " AND branch_id = " + m_branchID;
		sc += ")";
		sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
		sc += " VALUES (" + dr["code"].ToString() + ", " + m_branchID + ", 0, " + dr["quantity"].ToString() + ")"; 
		sc += " ELSE Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock + " + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString() + " AND branch_id = " + m_branchID;

		sc += " UPDATE product SET allocated_stock=allocated_stock+" + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString() + " ";
//DEBUG("write sc=", sc);
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
	return true;
}

void PrintJavaFunction()
{
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function OnShippingMethodChange()");
	Response.Write("{		");
	Response.Write("	var m = Number(document.form1.shipping_method.value);\r\n");
	Response.Write("	if(m == 1){document.all('tShipTo').style.visibility='hidden';document.all('tPT').style.visibility='visible';}\r\n");
	Response.Write("	else{document.all('tShipTo').style.visibility='visible';document.all('tPT').style.visibility='hidden';}\r\n");
	Response.Write("}\r\n");

	Response.Write("function OnSpecialShiptoChange()								");
	Response.Write("{																");
	Response.Write("	var v = document.all('ssta').style.visibility;				");
	Response.Write("	if(v != 'hidden')											");
	Response.Write("	{															");
	Response.Write("		document.all('ssta').style.visibility='hidden';			");
	Response.Write("		document.all('tshiptoaddr').style.visibility='visible';	");
	Response.Write("	}															");
	Response.Write("	else														");
	Response.Write("	{															");
	Response.Write("		document.all('ssta').style.visibility='visible';		");
	Response.Write("		document.all('tshiptoaddr').style.visibility='hidden';	");
	Response.Write("	}															");
	Response.Write("}																");

	Response.Write("</script");
	Response.Write(">");
}

void PrintWWWHeader()
{
	Response.Write(m_sHeaderEdit);
}

double GetOrderTotal()
{
	double dRet = 0;
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		double dPrice = MyDoubleParse(dr["salesPrice"].ToString());
		int nQty = MyIntParse(dr["quantity"].ToString());
		dRet += dPrice * nQty;
	}
	return dRet;
}

const string m_sHeaderEdit = @"

<html>
<head>

<meta http-equiv='Content-Type' content='text/html; charset=iso-8859-1'>

<title>Edit Order</title>

<style type=text/css>
td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}
body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}
.d{FONT-WEIGHT:300;FONT-SIZE:8PT;TEXT-DECORATION:none;FONT-FAMILY:verdana;COLOR:#000000;}
.m{TEXT-DECORATION:none;background-color:#EEEEEE;border-bottom: 1px solid #000000;border-left: 1px solid #000000;border-right: 1px solid #000000;border-top: 1px solid #000000;}
.x{FONT-WEIGHT:300;FONT-SIZE:8PT;TEXT-DECORATION:underline;FONT-FAMILY:verdana;COLOR:#0000ff;}
a{color:#000000;text-decoration:none} a:hover{color:red;text-decoration:none} a.d:hover{COLOR:#FF0000;TEXT-DECORATION:none}
.o{color:#0000FF;text-decoration:underline} a.o:hover{color:#FF0000;text-decoration:none}
.b{font-size:8pt;font-weight:bold;background-color:#EEEEEE;color:#444444;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696}
</style>
</head>

<body marginwidth=0 marginheight=0 topmargin=1 leftmargin=0 text=black link=black vlink=black alink=black>

<!-- Start Catalog Menu -->
<table cellpadding=0 cellspacing=0 BORDER=0 BGCOLOR=#CCCCCC width=100%>
<tr><td class=d colspan=2>
<table cellpadding=0 cellspacing=0 BORDER=0 BGCOLOR=#CCCCCC><tr>
<td align=center><img src=i/logo.gif border=0 height=16>&nbsp;&nbsp;&nbsp;</td><td><font color=#666696><b>Editing Order, click 'Select From Categories' to add new item</b></font>
</td></tr></table>
</td></tr></table>

";

</script>

<asp:Label id=LFooter runat=server/>


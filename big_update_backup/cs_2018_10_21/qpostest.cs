<!-- #include file="kit_fun.cs" -->
<!-- #include file="card_function.cs" -->
<!-- #include file="fifo_f.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
const int m_cols = 7;	//how many columns main table has, used to write colspan=
string m_tableTitle = "";
string m_invoiceNumber = "";
string m_comment = "";	//Sales Comment in Invoice Table;
string m_custpo = "";
string m_salesNote = "";
string m_branchID = "1";
string m_branchName = "";
string m_branchAddress = "";
string m_branchPhone = "";
string m_sSalesType = ""; //as string, m_quoteType is receipt_type ID
string m_salesName = "";
string m_nShippingMethod = "1";
string m_specialShipto = "0";
string m_specialShiptoAddr = ""; //special
string m_pickupTime = "";
string m_orderID = "";
string m_orderNumber = "";
string m_sales = "";
string m_customerID = "0";

string m_orderStatus = "1"; //Being Processed
string m_url = "";

double m_dFreight = 0;
double m_dInvoiceTotal = 0;
string m_discount = "0";
int m_nSearchReturn = 0;
int m_nNoOfReceiptPrintOut = 1;

bool m_bNoDoubleQTY = true;
bool b_create = false;
bool m_bCreditReturn = false;
bool m_bOrderCreated = false;
bool m_bFixedPrices = false;

string m_jdic = ""; //JScript Dictionary object
string m_sProductCache = "";
string m_sAgentCache = "";
string m_sSalesBarcodeCache = "";
string miscellaneous_code = "";
string opentilt = "";
double m_gst = 1.125;

double m_dCash = 0; //for kicking cash draw

//bool m_bDebug = true;
bool m_bDebug = false;
bool m_bUpdateCache = true;
bool m_bTrustedSite = true;
string m_shta = ""; //hta file content for offline invoicing
string m_sPaymentForm = "";
string m_sPaymentFormOnlineCache = "";
string m_sReceiptHeader = "";
string m_sReceiptFooter = "";
string m_sReceiptKickout = "";
string m_sReceiptPort = "";
string m_sReceiptPrinterObject = "";
string m_sAgent = "";
string m_sServer = "";

bool m_bDisplay = false;
string m_sDisplayPort = "COM1";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	if(Request.QueryString["t"] == "delete")
	{
		string inv = Request.QueryString["i"];
		if(inv != null && inv != "" && TSIsDigit(inv))
		{
			if(DoDeleteInvoice(inv))
			{
//				PrintAdminHeader();
//				Response.Write("<br><center><h4>Invoice deleted, please wait a second</h4>");
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpostest.aspx?t=new\">");
			}
			return;
		}
	}

	//check trusted site
	if(Request.QueryString["nts"] != null) //no trusted site, disable hta functions
	{
		Session["qpos_no_offline"] = true;
		m_bTrustedSite = false;
	}

	m_bDisplay = MyBooleanParse(GetSiteSettings("POS_HAS_DISPLAY_UNIT", "0"));
	if(!m_bTrustedSite)
		m_bDisplay = false; //force disable
	if(m_bDisplay)
		m_sDisplayPort = GetSiteSettings("POS_DISPLAY_PORT", "LPT1");

	string server = Request.ServerVariables["SERVER_NAME"];
	if(server == "localhost")
		m_bDebug = true; //no maximize, no move etc..
	m_sServer = "http://" + server;

	miscellaneous_code = GetSiteSettings("set_miscellaneous_code", "4000", false);
	opentilt = GetSiteSettings("set_open_tilt_code", "1000", false);
	m_bNoDoubleQTY = MyBooleanParse(GetSiteSettings("qpos_no_double_qty", "1", false));
	m_nNoOfReceiptPrintOut = int.Parse(GetSiteSettings("total_no_of_receipt_printout", "1", false));

	m_gst = MyDoubleParse(GetSiteSettings("gst_rate_percent", "1.125")) / 100;
	if(m_gst < 1)
		m_gst = 1 + m_gst;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		m_bFixedPrices = true;

	if(Session["branch_id"] != null)
		m_branchID = Session["branch_id"].ToString();
	
	m_branchName = GetBranchName(m_branchID);

	if(Request.QueryString["updatecache"] != null) //write hta file failed for some reason
	{
		Session["qpos_hta_update_needed"] = true;
	}

	string t = "";
	if(Request.QueryString["t"] != null)
		t = Request.QueryString["t"];
	if(Request.QueryString["oi"] == null & t == "")
	{
//		if(Session["leave_offline_invoice"] == null)
		if(m_bTrustedSite)
		{
			CheckOfflineInvoices();
			return;
		}
	}
	else
	{
		if(t == "new")
		{
			Session["leave_offline_invoice"] = 1;
		}
		else if(t == "load")
		{
			PrintAdminHeader();
			LoadOfflineInvoices();
			return;
		}
		else if(t == "process")
		{
			PrintAdminHeader();
			ProcessOfflienInvoices();
			return;
		}
	}

	if(Request.QueryString["t"] == "new")
		EmptyCart();
	else if(Request.QueryString["t"] == "p")
	{
		if(m_bTrustedSite)
			PrintTrustedPaymentForm();
		else
			PrintPaymentForm(false);
		return;
	}
	else if(Request.QueryString["t"] == "pr")
	{
		PrintAdminHeader();
		PrintReceipt();
		return;
	}
	else if(Request.QueryString["t"] == "plr")
	{
		if(Session["qpos_last_invoice_number"] == null)
		{
			PrintAdminHeader();
			Response.Write("<br><center><h3>Sorry you haven't done any invoice in this session</h3>");
			return;
		}
		m_invoiceNumber = Session["qpos_last_invoice_number"].ToString();
		if(DoPrintReceipt(false, false))
		{
			Response.Write("<script language.javascript>window.close();<");
			Response.Write(">");
		}
		return;
	}
	else if(Request.QueryString["t"] == "end")
	{
		if(!DoReceivePayment())
			return;
		DoPrintReceipt(false, true);
		return;
	}
	else if(Request.QueryString["t"] == "chl") //create hta link
	{
		PrintAdminHeader();
		DoCreateShortcut();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpostest.aspx\">");
		return;
	}

	bool bMassInterface = false;
	if(Request.Form["rows"] == null)
		bMassInterface = true;

	if(Session["qpos_cache_updated"] != null)
		m_bUpdateCache = false;

	if(bMassInterface)
	{
		if(Session["qpos_hta_update_needed"] == null && m_bTrustedSite)
			CheckOfflineHTAFile();

		if(Request.QueryString["t"] == "fu")
			m_bUpdateCache = true;

		//main output, choose interface to use
		if(m_bTrustedSite)
		{
			if(Session["qpos_hta_update_needed"] == null && !m_bUpdateCache)
			{
				PrintTrustedMassInterface();
			}
		}
		else
		{
			if(!BuildProductCache())
				return;
			if(!BuildAgentCache())
				return;
			if(!BuildSalesBarcodeCache())
				return;
			PrintMassInterface();
		}

		//check updates
		if(m_bTrustedSite)
		{
			if(Session["qpos_hta_update_needed"] != null || m_bUpdateCache)
			{
				PrintAdminHeader();
				Response.Write("<br><center><h4>Updating cache, please wait ... </h4>");

				if(!BuildProductCache())
					return;
				if(!BuildAgentCache())
					return;
				if(!BuildSalesBarcodeCache())
					return;
				PrintMassInterface();
				UpdateOfflineHTAFile();
				Session["qpos_hta_update_needed"] = null; //regardless write falied or not
				Session["qpos_cache_updated"] = 1;
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpostest.aspx\">");
				return;
			}
		}

		if(Request.QueryString["t"] == "mo") //modify order
		{
			string inv = Request.QueryString["i"];
			if(inv != null && inv != "" && TSIsDigit(inv))
			{
				if(!DoDeleteInvoice(inv))
					return;
			}
			if(m_bTrustedSite)
			{
				Response.Write("<script language=javascript>restore_last_order();</script");
				Response.Write(">");
			}
		}
		return;
	}

	if(Request.Form["confirm_checkout"] == "0") //force going back on unknow error
	{
//		Response.Write("Error");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpostest.aspx\">");
		return;
	}

	if(BuildShoppingCart())
	{
		PrintAdminHeader();
		if(!DoCreateOrder(false, m_custpo, m_salesNote)) // false means not system quotation
		{
			Response.Write("<h3>ERROR CREATING QUOTE</h3>");
			return;
		}
		EmptyCart();
		m_bOrderCreated = true;
		Session["order_created"] = true;

		if(!CreateInvoice(m_orderID))
		{
			Response.Write("<h3>Error Create Invoice</h3>");
			return;
		}

		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpostest.aspx?t=p");
		if(!m_bTrustedSite)
			Response.Write("&nts=1");
		Response.Write("&i=" + m_invoiceNumber + "&total=" + m_dInvoiceTotal + "\">");
		return;

		Response.Write("<br><br><center><h3>" + m_sSalesType.ToUpper() + " Created</h3>");
		Response.Write("<h5>Number : </h5><h1><font color=red>" + m_orderID + "</h1><br>");
		Response.Write("<input type=button " + Session["button_style"]);
		Response.Write("onclick=window.location=('invoice.aspx?n=" + m_invoiceNumber + "') ");
		Response.Write(" value='Print Invoice'>");
		Response.Write("<br><br><br><br><br>");
	}
}

bool CreateOrder(string branch_id, string card_id, string po_number, string special_shipto, string shipto, 
				 string shipping_method, string pickup_time, string contact, string sales_id, string sales_note, 
				 ref string order_number)
{
	string reason = "";
	bool bStopOrdering = false;//IsStopOrdering(card_id, ref reason);
	if(bStopOrdering)
	{
		if(reason == "")
			reason = "No reason given.";
		Response.Write("<br><br><center><h3>This account has been disabled to place order</h3><br>");
		Response.Write("<h4><font color=red>" + reason + "<font color=red></h4><br>");
		Response.Write("<h4><a href=ecard.aspx?id=" + card_id + " class=o>Edit Account</a></h4>");
		Response.Write("<br><br><br><br><br><br><br>");
		return false;
	}

//	if(!CheckBottomPrice())
//		return false;

//	string agent = Request.Form["tagent"];
	string agent = m_sAgent;
	if(Request.Form["agent"] != null)
		agent = Request.Form["agent"];
	if(agent == null || agent == "")
		agent = "0";

	DataSet dsco = new DataSet();
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, card_id, po_number, freight) VALUES(0, " + card_id + ", '";
	sc += po_number + "', 0 ";
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
			sc += ", shipping_method=1";// + shipping_method;
			sc += ", pick_up_time='" + EncodeQuote(pickup_time) + "' ";
			sc += ", agent = " + agent;
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

bool CreateInvoice(string id)
{
	DataRow dr = null;
	double dPrice = 0;
	double dFreight = 0;
	double dTax = 0;
	double dTotal = 0;
	int rows = 0;
	if(dst.Tables["invoice"] != null)
		dst.Tables["invoice"].Clear();
	string sc = "SELECT * FROM orders WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "invoice");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error creating invoice, id=" + id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	dr = dst.Tables["invoice"].Rows[0];
	string card_id = dr["card_id"].ToString();
	string po_number = dr["po_number"].ToString();
	string m_shippingMethod = dr["shipping_method"].ToString();
	string m_pickupTime = dr["pick_up_time"].ToString();
	string sales = dr["sales"].ToString();
	string agent = dr["agent"].ToString();
	if(sales != "")
		sales = TSGetUserNameByID(sales);

	dFreight = Math.Round(MyDoubleParse(dst.Tables["invoice"].Rows[0]["freight"].ToString()), 2);

	if(dst.Tables["item"] != null)
		dst.Tables["item"].Clear();
	sc = "SELECT * FROM order_item WHERE id=" + id;
//DEBUG(" sc order=", sc);
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "item");
		if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		double dp = MyDoubleParse(dr["commit_price"].ToString());
		dp = Math.Round(dp, 4);
//DEBUG("dp price= ",dp.ToString());
		double qty = MyDoubleParse(dr["quantity"].ToString());
//DEBUG("QTYT= ",qty);
		dPrice += dp * qty;
//DEBUG("dprice dp*qty= ",dPrice.ToString());
		dPrice = Math.Round(dPrice, 4);
//DEBUG("dprice round= ",dPrice.ToString());

	}
	dTax = (dPrice + dFreight) * GetGstRate(card_id);
	dTax = Math.Round(dTax, 4);
	dTotal = (dPrice + dFreight) * (1 + GetGstRate(card_id));
//DEBUG("dTotal no round= ", dTotal);
	dTotal = Math.Round(dTotal, 2);
//DEBUG("dTotal round= ", dTotal);
	m_dInvoiceTotal = dTotal;
//DEBUG("m_dInvoiceTotal invocie = ", m_dInvoiceTotal);
	dr = dst.Tables["invoice"].Rows[0];
	string special_shipto = "0";
	if(bool.Parse(dr["special_shipto"].ToString()))
		special_shipto = "1";
	
	string receipt_type = GetEnumID("receipt_type", "invoice");
	if(m_bCreditReturn)
		receipt_type = "6";//GetEnumID("receipt_type", "credit note");

	string sbSystem = "0";
	if(MyBooleanParse(dr["system"].ToString()))
		sbSystem = "1";
	string type = Request.Form["pm"];
	sc = " SET DATEFORMAT dmy ";
	sc += " BEGIN TRANSACTION ";
	sc += "INSERT INTO invoice (branch, type, card_id, price, tax, total, amount_paid, paid, commit_date, special_shipto, shipto ";
	sc += ", freight, cust_ponumber, shipping_method, pick_up_time, sales, sales_note, agent)";
	sc += " VALUES("+ m_branchID +", " + receipt_type + ", " + dr["card_id"].ToString() + ", " + dPrice;
	sc += ", " + dTax + ", " + dTotal + ", " + dTotal + ", 1, GETDATE(), ";
	sc += special_shipto + ", '" + EncodeQuote(dr["shipto"].ToString()) + "', " + dFreight + ", '" + po_number + "', ";
	sc += m_shippingMethod + ", '" + EncodeQuote(m_pickupTime) + "', '" + EncodeQuote(sales) + "', '";
	sc += EncodeQuote(dr["sales_note"].ToString()) + "' ";
	sc += ", '" + agent + "' ";
	sc += " )";
	sc += " SELECT IDENT_CURRENT('invoice') AS id";
	sc += " COMMIT ";
	if(dst.Tables["invoice_id"] != null)
		dst.Tables["invoice_id"].Clear();
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "invoice_id") == 1)
		{
			m_invoiceNumber = dst.Tables["invoice_id"].Rows[0]["id"].ToString();
		}
		else
		{
			Response.Write("<br><br><center><h3>Error get new invoice number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//update order to record invoice number
	sc = " UPDATE invoice SET invoice_number = id WHERE id = " + m_invoiceNumber;
	sc += " UPDATE orders SET invoice_number = " + m_invoiceNumber + ", status=3 WHERE id=" + id; //status 3 = shipped
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

	bool bHasKit = false;

	//write price history
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		string commit_price = dr["commit_price"].ToString();
		string quantity = dr["quantity"].ToString();
		string code = dr["code"].ToString();
		string name = dr["item_name"].ToString();
		string kit = dr["kit"].ToString();
		string krid = dr["krid"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		double dNormalPrice = dGetNormalPrice(code);
		sbSystem = "0";
		if(bool.Parse(dr["system"].ToString()))
			sbSystem = "1";

		string sKit = "0";
		if(MyBooleanParse(kit))
		{
			sKit = "1";
			bHasKit = true;
		}
		if(krid == "")
			krid = "null";

		sc = "INSERT INTO sales (invoice_number, code, name, quantity, commit_price, supplier, supplier_code, supplier_price, system, kit, krid ";
		sc += ", normal_price ";
		sc += " )";
		sc += " VALUES(" + m_invoiceNumber + ", " + code + ", '" + EncodeQuote(name) + "', " + quantity + ", " + commit_price + ", ";
		sc += "'" + supplier + "', '" + supplier_code + "', " + supplier_price + ", " + sbSystem + ", " + sKit + ", " + krid + "";
		sc += ", "+ dNormalPrice +" ";
		sc += " )";
//DEBUG("sc=", sc);
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
		
		double dQty = MyDoubleParse(quantity);

		//disable this call, we use last cost to calculate profit report for POS system
		//		fifo_sales_update_cost(m_invoiceNumber, code, commit_price, m_branchID, dQty);

		//update stock qty
		UpdateStockQty(dQty, code, m_branchID);
//		fifo_checkAC200Item(m_invoiceNumber, code, supplier_code, commit_price); //for unknow item
	}

	if(bHasKit)
	{
		if(!RecordKitToInvoice(id, m_invoiceNumber))
			return true;
	}

	Session["qpos_last_invoice_number"] = m_invoiceNumber;

	UpdateCardAverage(card_id, dPrice, MyIntParse(DateTime.Now.ToString("MM")));
	UpdateCardBalance(card_id, dTotal);

	return true;
}


double dGetNormalPrice(string code)
{
	if(dst.Tables["normal_price"] != null)
		dst.Tables["normal_price"].Clear();
	double dprice = 0;
	string sc = " SELECT DISTINCT ISNULL(";
	sc += "price1 / "+ m_gst +" "; //GST exclusive price
	sc += ",0) AS n_price FROM code_relations WHERE code = "+ code +" ";
	string nprice = "0";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "normal_price") == 1)
			 nprice= dst.Tables["normal_price"].Rows[0]["n_price"].ToString();
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return 999;
	}
	dprice = MyDoubleParse(nprice);
	return dprice;
}

bool UpdateStockQty(double qty, string id, string branch_id)
{
	string sc = "";
	sc = " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + id;
	sc += " AND branch_id = " + branch_id;
	sc += ")";
	sc += " INSERT INTO stock_qty (code, branch_id, qty, supplier_price) ";
	sc += " VALUES (" + id + ", " + branch_id + ", " + (0 - qty).ToString() + ", " + GetSupPrice(id) + ")"; 
	sc += " ELSE Update stock_qty SET ";
	sc += "qty = qty - " + qty + ", allocated_stock = allocated_stock - " + qty;
	sc += " WHERE code=" + id + " AND branch_id = " + branch_id;

	if(!g_bRetailVersion)
	{
		sc += " UPDATE product SET stock = stock - " + qty + ", allocated_stock = allocated_stock - " + qty;
		sc += " WHERE code=" + id;
	}
	else //retail version only update allocated stock in product table
	{
		sc += " UPDATE product SET allocated_stock = allocated_stock - " + qty;
		sc += " WHERE code=" + id;
	}
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

bool DoCreateOrder(bool bSystem, string sCustPONumber, string sSalesNote)
{
	string contact = "";
	m_sales = Request.Form["sales"];
	return CreateOrder(m_branchID, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), ref m_orderID);
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
	if(Request.Form["order_id"] == null || Request.Form["order_id"] == "")
		return true;

//	if(!CheckBottomPrice())
//		return false;

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
		int nqty = MyIntParse(sqty);
		sc += " Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock - " + sqty;
		sc += " WHERE code=" + code + " AND branch_id = " + m_branchID;
		sc += " UPDATE product SET allocated_stock = allocated_stock - " + sqty + " WHERE code=" + code;
	}
	sc += " DELETE FROM order_item WHERE id=" + m_orderID;
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
/*if(dtCart.Rows.Count <= 0)
{
	Response.Write("<script language=javascript> window.alert('Cart Empty');</script");
	Response.Write(">");
return false;
}
*/
//DEBUG("cart rows = ", dtCart.Rows.Count);
//print_t(dtCart);	
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		string kit = dr["kit"].ToString();
		double dPrice = Math.Round(MyDoubleParse(dr["salesPrice"].ToString()), 2);
//DEBUG("cart price=", dPrice);
		string name = EncodeQuote(dr["name"].ToString());
		
		if(name.Length > 255)
			name = name.Substring(0, 255);

		if(kit == "1")
		{
			RecordKitToOrder(order_id, dr["code"].ToString(), name, dr["quantity"].ToString(), dPrice, m_branchID);
			continue;
		}
		
		string sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
		sc += ", commit_price ";
//		sc += ", normal_price ";
		sc += " ) VALUES(" + order_id + ", " + dr["code"].ToString() + ", ";
		sc += dr["quantity"].ToString() + ", '" + name + "', '" + dr["supplier"].ToString();
		sc += "', '" + dr["supplier_code"].ToString() + "', " + Math.Round(MyDoubleParse(dr["supplierPrice"].ToString()), 4);
		sc += ", " + Math.Round(MyDoubleParse(dr["salesPrice"].ToString()), 4) + " ";
//		sc += ","+ dNormalPrice +" ";
		sc += ") ";
		
		sc += " UPDATE stock_qty SET allocated_stock = allocated_stock + " + dr["quantity"].ToString();
		sc += " WHERE code = " + dr["code"].ToString();
		sc += " AND branch_id = " + m_branchID;
		
		sc += " UPDATE product SET allocated_stock=allocated_stock+" + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString() + " ";
//DEBUG("sc order_item = ", sc);
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
	StringBuilder sb = new StringBuilder();

	sb.Append("<script TYPE=text/javascript");
	sb.Append(">");

	sb.Append("function OnShippingMethodChange()");
	sb.Append("{		");
	sb.Append("	var m = Number(document.form1.shipping_method.value);\r\n");
	sb.Append("	if(m == 1){document.all('tShipTo').style.visibility='hidden';document.all('tPT').style.visibility='visible';}\r\n");
	sb.Append("	else{document.all('tShipTo').style.visibility='visible';document.all('tPT').style.visibility='hidden';}\r\n");
	sb.Append("}\r\n");

	sb.Append("function OnSpecialShiptoChange()								");
	sb.Append("{																");
	sb.Append("	var v = document.all('ssta').style.visibility;				");
	sb.Append("	if(v != 'hidden')											");
	sb.Append("	{															");
	sb.Append("		document.all('ssta').style.visibility='hidden';			");
	sb.Append("		document.all('tshiptoaddr').style.visibility='visible';	");
	sb.Append("		document.all('tshiptoaddr').style.top = 10;			");
	sb.Append("	}															");
	sb.Append("	else														");
	sb.Append("	{															");
	sb.Append("		document.all('ssta').style.visibility='visible';		");
	sb.Append("		document.all('tshiptoaddr').style.visibility='hidden';	");
	sb.Append("	}															");
	sb.Append("}																");

	sb.Append("</script");
	sb.Append(">");

	m_shta += sb.ToString();
	Response.Write(sb.ToString());
}

bool PrintMassInterface()
{
	StringBuilder sb = new StringBuilder();

	string sheader = ReadSitePage("qpos_header");
	sb.Append(sheader);
	if(m_bTrustedSite)
	{
		sb.Append("\r\n<object classid=\"clsid:B816E029-CCCB-11D2-B6ED-444553540000\" ");
		sb.Append(" CODEBASE=\"asprint.ocx\" ");
		sb.Append(" id=\"AsPrint1\">\r\n");
		sb.Append("<param name=\"_Version\" value=\"65536\">\r\n");
		sb.Append("<param name=\"_ExtentX\" value=\"2646\">\r\n");
		sb.Append("<param name=\"_ExtentY\" value=\"1323\">\r\n");
		sb.Append("<param name=\"_StockProps\" value=\"0\">\r\n");
		sb.Append("<param name=\"HideWinErrorMsg\" value=\"1\">\r\n");
		sb.Append("</object>\r\n");
	}
	sb.Append(PrintMJava());
	sb.Append(m_sProductCache);
	sb.Append(m_sAgentCache);
	
	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");

	sb.Append("<form name=f action=?t=checkout");
	if(!m_bTrustedSite)
		sb.Append("&nts=1");
	sb.Append(" method=post onKeyDown=\"return on_form_keydown();\">\r\n");
	sb.Append("<input type=hidden name=printer_port value='" + m_sReceiptPort + "'>");
	sb.Append("<input type=hidden name=display_port value='" + m_sDisplayPort + "'>");
	sb.Append("<input type=hidden name=branch value=" + m_branchID + ">");
	sb.Append("<input type=hidden name=branch_name value='" + m_branchName + "'>");
	sb.Append(m_sSalesBarcodeCache);
	//java memory
	sb.Append("<input type=hidden name=rows value=0>");
	sb.Append("<input type=hidden name=focus_field value=2>");
	sb.Append("<input type=hidden name=last_barcode>");
	sb.Append("<input type=hidden name=last_code>");

	sb.Append("<input type=hidden name=confirm_checkout value=0>");

//sb.Append("<input type=text name=debug>");
/*	sb.Append("<font color=#444444><b>P.O.S. - " + m_sCompanyTitle + "</b></font>");
//	sb.Append(" &nbsp&nbsp&nbsp&nbsp;<b><font color=#888888>Sales : " + Session["name"] + "</b>");
	sb.Append("<div align=right><font color=#888888>");
	sb.Append(m_branchName + " - ");
	sb.Append(Session["name"] + "</div>");
*/

	sb.Append("<table width=100% border=0 cellspacing=0 cellpadding=0 bordercolor=red bgcolor=#EEEEEE >\r\n");
	sb.Append("<tr align=left><td colspan=1 width=15%>\r\n");
	sb.Append("<font color=#444444><b><br>&nbsp;<img border=0 src='" + m_sServer + "/i/eznz.gif'></td>");
	sb.Append("<td align=center colspan=4>");
	sb.Append("<input type=button name=pos_title value='POS' style=\"border:0;font-size:24pt;font-weight:bold;background-color:#EEEEEE;\">");

	if(m_bTrustedSite)
	{
		for(int nh=1; nh<=4; nh++)
		{
			sb.Append("&nbsp;<input type=button name=hold" + nh + " value=" + nh + " onclick=\"get_hold(" + nh + ");\" class=b ");
			sb.Append(" title=\"Click to restore order " + nh + "\" ");
			sb.Append(" style=\"visibility:hidden;border:0;font-size:24pt;font-weight:bold;background-color:#EEEEEE;\">");
		}
		sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;<input type=button value=H class=b style=\"font-size:12pt\" title='Click to hold current order' onclick=\"hold_current()\">");
		sb.Append("&nbsp;<input type=button value=D class=b style=\"font-size:12pt\" title='Click to delete current order' onclick=\"del_current(false)\">");
	}
	sb.Append("</td>\r\n<td nowrap align=right>");
	sb.Append("<b>Agent : </b><input type=hidden name=agent value=''>\r\n");
	sb.Append("<input type=text size=1 name=agent_code onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
	sb.Append(" onKeyDown=\"if(event.keyCode==13){CheckAgentCode(); document.f.button_ok.focus();event.keyCode=9;}\" >");
	sb.Append("&nbsp&nbsp;");
	sb.Append("<input type=text size=15 style='border:0;background-color:#EEEEEE' readonly=true name=agent_name value=''>");
//	sb.Append("<b>Discount : </b>");
	sb.Append("<input type=hidden size=1 style=text-align:right name=agent_discount value=0 >");
//	sb.Append(" onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
//	sb.Append(" onKeyDown=\"if(event.keyCode==13){document.f.s.focus();event.keyCode=38;}\">");
//	sb.Append("%");
	sb.Append("&nbsp&nbsp&nbsp&nbsp;");

//disabled time count down...
//	sb.Append("Count Down:<input wrap type=text size=1 readonly name=tmin value=60>:<input size=1% readonly type=text name=tsecond>");

	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td width=110>P.O.S.</b></font>");
//	sb.Append("<tr><td>P.O.S. v.1 - " + m_sCompanyTitle + "</b></font>");
	sb.Append("</td>\r\n");

	sb.Append("<td width=160>Item Barcode: </td>\r\n");
	sb.Append("<td width=70>QTY </td>\r\n");
	sb.Append("<td width=100 nowrap>Selling Price $ ");
	//sb.Append("<td width=6%>Mark Down By % </td>");
	//sb.Append("<td width=6%>Mark Down By $ ");
	sb.Append("</td><td>&nbsp;");
	sb.Append("</td>\r\n<td>");

//	sb.Append(" &nbsp&nbsp&nbsp&nbsp;<b><font color=#888888>Sales : " + Session["name"] + "</b>");
	sb.Append("<div align=right>");
	sb.Append("<b>Sales : </b>");
//	sb.Append(PrintSalesOptions());
	sb.Append("<input type=hidden name=sales value=''>\r\n");
//	sb.Append("<input type=text name=sales_barcode onchange=\"CheckSalesBarcode();\">\r\n");
	sb.Append("<input type=text name=sales_barcode onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
	sb.Append(" onKeyDown=\"if(event.keyCode==13){if(CheckSalesBarcode()){document.f.cmdgo.focus();}}\">");
	sb.Append(" &nbsp&nbsp&nbsp&nbsp&nbsp;");
	sb.Append("<font color=#888888>");
	sb.Append(m_branchName + " - ");
//	sb.Append(Session["name"]);
	sb.Append("<input type=text size=10 readonly=true style='border:0;background-color:#EEEEEE' name=sales_name value=''>");
	sb.Append("</div>");
	sb.Append("</td></tr>\r\n");
	sb.Append("</table>\r\n");
	sb.Append("<table width=100% border=1 cellspacing=0 cellpadding=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr><td width=15% valign=top>\r\n");
	
	//left side table
	sb.Append("<table width=100% border=0 cellspacing=0 cellpadding=0 bordercolor=#eeeeee bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr><td><input type=text name=code class=f style='font-weight:bold'></td></tr>\r\n");
	sb.Append("<tr><td><textarea name=desc rows=5 cols=15 class=f style=overflow:hidden></textarea></td></tr>\r\n");
	sb.Append("<tr><td align=right><input type=text name=price class=f style='font-weight:bold;text-align:right'></td></tr>\r\n");
	sb.Append("<tr><td><textarea name=sspace rows=15 cols=10 class=f style=overflow:hidden></textarea></td></tr>\r\n");
//	sb.Append("<tr rowspan=30><td> &nbsp; <br><br><br> &nbsp; </td></tr>");
	sb.Append("<tr><td align=right><b><font size=3>Total : </b>");
	sb.Append("<input type=text name=total size=5 value=0 style='font-weight:bold;text-align:right;font-size:10pt' onfocus=\"sh(this);\" onblur=\"rh(this);\">");
	sb.Append("<input type=button name=button_ok value=OK onfocus=\"sh(this);\" onblur=\"rh(this);\" onclick=\"CalcTotalDiscount();\" class=b>");
//	sb.Append("<input type=button class=b value=Calculator onclick=\"calc.exe;\">");
//	sb.Append("<a href='c:\\windows\\system32\\calc.exe'>Calculator</a>");

	sb.Append("</td></tr>\r\n");
	sb.Append("</table>\r\n");

	sb.Append("</td><td width=85% valign=top>\r\n");

	sb.Append("<table width=100% border=1 cellspacing=0 cellpadding=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	sb.Append("<tr>");
	sb.Append("<td colspan=7 bgcolor=#61AABC>");

	string keyEnter = "onfocus=\"sh(this);\" onblur=\"rh(this);\" onKeyDown=\"if(document.f.agent_code.value == '')";
	keyEnter += "{window.alert('Please enter agent code.');event.keyCode=8;document.f.agent_code.focus();}";
	keyEnter += "else if(event.keyCode==13 && document.f.agent_code.value != '00' && document.f.s.value!='' ";
	keyEnter += "&& document.f.s.value!='.' && document.f.s.value.indexOf('+') < 0 && document.f.s.value.indexOf('-') < 0 ";
	keyEnter += "&& document.f.s.value.indexOf('*') < 0 && document.f.s.value.indexOf('/') < 0){event.keyCode=9;}\"";
//	string keyEnter = "onfocus=\"sh(this);\" onblur=\"rh(this);\" ";

	sb.Append("<input type=text name=s " + keyEnter + ">\r\n");
	sb.Append("<input type=text size=6% name=md_qty "+ keyEnter +">\r\n");
	sb.Append("<input type=text size=6% name=md_dollar onfocus=\"sh(this);\" onblur=\"rh(this);\" onKeyDown=\"if(event.keyCode==13 && document.f.s.value!='.' && document.f.s.value.substring(0,1)!='*'){if(document.f.agent_name.value!='' &&this.value==''){window.alert('Please enter price!');document.f.md_qty.focus();}event.keyCode=9;}\">\r\n");

	//hidden value
	sb.Append("<input type=hidden name=miscel_code value='"+ miscellaneous_code +"'>\r\n");
	sb.Append("<input type=hidden name=open_tilt_code value='"+ opentilt +"'>\r\n");
	sb.Append("<input type=hidden name=gst_rate value='"+ m_gst +"'>\r\n");

	sb.Append("<script language=javascript>");
	sb.Append("document.f.agent_code.focus();</script");
	sb.Append(">\r\n");
	sb.Append("<input type=submit name=cmdgo value='  GO!  ' class=b onfocus=\"sh(this);\" onblur=\"rh(this);\" onclick=\"if(onscan()){return false;}else{LoadPaymentForm();return false;}\">\r\n");
	
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr bgcolor=#EEEFF><td>CODE</td><td>ITEM DESCRIPTION</td><td>NORMAL PRICE</td><td align=>PRICE (inc GST)</td>");
	sb.Append("<td>QTY</td><td>DISCOUNTED%</td><td>SUB-TOTAL</td></tr>\r\n");
	for(int i=0; i<50; i++)
	{
		sb.Append("<tr><td><input type=hidden name=rc" + i + "><input name=rb" + i + " size=5 class=f readonly></td>");
		sb.Append("<td><input size=30 name=rd" + i + " class=f readonly></td>");
		sb.Append("<td><input size=3 name=np" + i + " class=f readonly></td>");
		sb.Append("<td><input size=10 name=rp" + i + " class=f onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"cp(" + i + ");\"></td>");
		sb.Append("<td><input size=3 name=rq" + i + " class=f onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"ct();\"></td>");
		sb.Append("<td><input size=5 name=md" + i + " class=f onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"cd(" + i + ");\"></td>");
//		sb.Append("<td><input size=10 name=st" + i + " class=f onchange=\"cs(" + i + ");\"><input type=hidden name=np" + i + ">");
		sb.Append("<td><input size=10 name=st" + i + " class=f onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"cs(" + i + ");\">");
		sb.Append("<input type=button name=del" + i + " value=X onclick=\"del(" + i + ")\" class=b style=visibility:hidden>");
		sb.Append("<input type=hidden name=ro" + i + ">");
		sb.Append("</td>");
		sb.Append("</tr>\r\n");
	}

	sb.Append("</table>");
	sb.Append("</td></tr></table>");
	if(!m_bTrustedSite)
	{
		string sbcn = sb.ToString();
		sbcn = sbcn.Replace("else{LoadPaymentForm();return false;}", ""); //not for online
		Response.Write(sbcn);
		return true;
	}
	//all hta functions followed

	sb.Append("<script language=javascript>\r\n");
//	sb.Append("<!-- ");

	DoPrintReceipt(true, true); //get receipt script for offline hta
//	string s = " var scart = '" + m_sReceiptPrinterObject + "';\r\n";
	string s = "";
	s += @"
var hh = new Array();
refresh_holds();
var total_holds = hh.length;
var current_number = total_holds + 1;
show_hold_list();

function check_special_printer_port()
{
	var sport = '';
	fn = 'c:/qpos/p_port.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
	{
		tf = fso.OpenTextFile(fn, 1, false); 
		try
		{
			sport = tf.ReadAll();
		}
		catch(err)
		{
		}
		tf.Close(); 
	}
	if(sport != '')
		document.f.printer_port.value = sport;
}
check_special_printer_port();
function refresh_holds()
{
	hh = new Array();
	var forders = 'c:/qpos/orders.txt'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(!fso.FileExists(forders))
	{
		var tf = fso.OpenTextFile(forders , 8, 1, -2);
		tf.Write('');
		tf.Close();
	}
	else
	{
		var tf = fso.OpenTextFile(forders, 1, false); 
		while(!tf.AtEndOfStream)
		{
			var sline = tf.ReadLine();
			if(sline != '')
			{
//				window.alert(sline);
				eval('hh.push([' + sline + '])');
			}
		}
		tf.Close();
	}
}
function show_hold_list()
{
	for(var m=1; m<=4; m++)
	{
		if(m > total_holds+1)
";
s += "			eval(\"document.f.hold\" + m + \".style.visibility='hidden'\"); ";
s += "		else ";
s += "			eval(\"document.f.hold\" + m + \".style.visibility='visible'\"); ";
s += "		if(m == current_number) ";
s += "			eval(\"document.f.hold\" + m + \".style.color='#F5362E'\"); ";
s += "		else ";
s += "			eval(\"document.f.hold\" + m + \".style.color='black'\"); ";
s += @"
	}
}
function get_hold(nNumber)
{
	if(nNumber > hh.length+1)
		return;
	else if(nNumber > hh.length)
	{
		current_number = nNumber;
		clear_form_cart();
		show_hold_list();
		return;
	}
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
			break;
		";
s += "		eval(\"document.f.rc\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rb\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rd\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.ro\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rp\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rq\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.md\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.np\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.st\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.del\" + n + \".style.visibility='hidden'\"); \r\n";
s += @"
	}
	var cart = hh[nNumber-1];
	var gh_code = '';
	var gh_barcode = '';
	var gh_sdesc = '';
	var gh_sgprice = '';
	var gh_sinb = '';
	for(var n=0; n<cart.length; n++)
	{
		gh_code = cart[n][0];
		gh_barcode = cart[n][1];
		gh_sdesc = cart[n][2];
		gh_sprice = cart[n][4];
";
s += "		eval(\"document.f.rc\" + n + \".value = cart[n][0]\"); \r\n ";
s += "		eval(\"document.f.rb\" + n + \".value = cart[n][1]\"); \r\n ";
s += "		eval(\"document.f.rd\" + n + \".value = cart[n][2]\"); \r\n ";
s += "		eval(\"document.f.ro\" + n + \".value = cart[n][3]\"); \r\n ";
s += "		eval(\"document.f.rp\" + n + \".value = cart[n][4]\"); \r\n ";
s += "		eval(\"document.f.rq\" + n + \".value = cart[n][5]\"); \r\n ";
s += "		eval(\"document.f.md\" + n + \".value = cart[n][6]\"); \r\n ";
s += "		eval(\"document.f.np\" + n + \".value = cart[n][7]\"); \r\n ";
s += "		eval(\"document.f.st\" + n + \".value = cart[n][8]\"); \r\n ";
s += "		eval(\"document.f.del\" + n + \".style.visibility='visible'\"); \r\n ";
s += @"
	}
	document.f.code.value = gh_code;
	document.f.desc.value = gh_sdesc;
	document.f.price.value = gh_sgprice;
	document.f.last_barcode.value = gh_barcode;
	document.f.last_code.value = gh_code;
	document.f.rows.value = cart.length;

	ct();
	document.f.s.value='';
	document.f.md_qty.value='';
	document.f.md_dollar.value='';
	document.f.s.focus();
	current_number = nNumber;
	show_hold_list();

}
function hold_current()
{
	if(total_holds >= 3)
	{
		window.alert('Sorry I can hold no more orders.');
		return;
	}
	if(!window.confirm('Are you sure to hold current order?'))
		return;
	var cart = '';
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
		{
			if(n == 0)
				return;
			else
				break;
		}
		var rb = eval('document.f.rb' + n + '.value');
		var rd = eval('document.f.rd' + n + '.value');
		var ro = eval('document.f.ro' + n + '.value');
		var rp = eval('document.f.rp' + n + '.value');
		var rq = eval('document.f.rq' + n + '.value');
		var md = eval('document.f.md' + n + '.value');
		var np = eval('document.f.np' + n + '.value');
		var st = eval('document.f.st' + n + '.value');
		if(n > 0)
			cart += ',';
";
s += "		cart += '[' + rc + ',' + rb + ',\"' + rd + '\",' + ro + ',' + rp + ',' + rq + ',' + md + ',' + np + ',' + st + ']'; ";
s += @"
	}
	eval('hh.push([' + cart + '])');
	total_holds++;
	current_number = total_holds + 1;
	flush_holds();
	show_hold_list();
	clear_form_cart();
}
function clear_form_cart()
{
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
			break;
		";
s += "		eval(\"document.f.rc\" + n + \".value = ''\");";
s += "		eval(\"document.f.rb\" + n + \".value = ''\");";
s += "		eval(\"document.f.rd\" + n + \".value = ''\");";
s += "		eval(\"document.f.ro\" + n + \".value = ''\");";
s += "		eval(\"document.f.rp\" + n + \".value = ''\");";
s += "		eval(\"document.f.rq\" + n + \".value = ''\");";
s += "		eval(\"document.f.md\" + n + \".value = ''\");";
s += "		eval(\"document.f.np\" + n + \".value = ''\");";
s += "		eval(\"document.f.st\" + n + \".value = ''\");";
s += "		eval(\"document.f.del\" + n + \".style.visibility='hidden'\");";
s += @"
	}
	document.f.code.value = '';
	document.f.desc.value = '';
	document.f.price.value = '';
	document.f.last_barcode.value = '';
	document.f.last_code.value = '';
	document.f.rows.value = 0;

	ct();
	document.f.s.value='';
	document.f.md_qty.value='';
	document.f.md_dollar.value='';
	document.f.s.focus();
}
function flush_holds()
{
	var forders = 'c:/qpos/orders.txt'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(forders))
		fso.DeleteFile(forders);
	fso = new ActiveXObject('Scripting.FileSystemObject');
	var tf = fso.OpenTextFile(forders , 8, 1, -2);
	for(var n=0; n<hh.length; n++)
	{
		var sline = '';
		var cart = hh[n];
		for(var m=0; m<cart.length; m++)
		{
			if(m > 0)
				sline += ',';
			sline += '[';
			for(var t=0; t<cart[m].length; t++)
			{
				if(t > 0)
					sline += ',';
				if(t == 2)
";
s += "					sline += '\"' + cart[m][t] + '\"'; ";
s += @"
				else
					sline += cart[m][t];
			}
			sline += ']';
		}
		tf.WriteLine(sline);
	}
	tf.Close();
	refresh_holds();
}
function del_current(bCheckingout)
{
//	window.alert(' rows=' + document.f.rows.value);
//	window.alert('current_number=' + current_number + ' total_holds=' + total_holds);
	if(current_number > total_holds)
		return;
	if(document.f.rows.value == '0')
		return;
	if(!bCheckingout)
	{
		if(!window.confirm('Are you sure to delete current order?'))
			return;
	}
	hh.splice(current_number - 1, 1);
	flush_holds();
	if(bCheckingout)
		return;
	total_holds--;
	if(current_number > total_holds+1)
		current_number = total_holds + 1;
	clear_form_cart();
	show_hold_list();
}
function remember_last_order()
{
	var hhl = new Array();
	var cart = '';
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
		{
			if(n == 0)
				return;
			else
				break;
		}
		var rb = eval('document.f.rb' + n + '.value');
		var rd = eval('document.f.rd' + n + '.value');
		var ro = eval('document.f.ro' + n + '.value');
		var rp = eval('document.f.rp' + n + '.value');
		var rq = eval('document.f.rq' + n + '.value');
		var md = eval('document.f.md' + n + '.value');
		var np = eval('document.f.np' + n + '.value');
		var st = eval('document.f.st' + n + '.value');
		if(n > 0)
			cart += ',';
";
s += "		cart += '[' + rc + ',' + rb + ',\"' + rd + '\",' + ro + ',' + rp + ',' + rq + ',' + md + ',' + np + ',' + st + ']'; ";
s += @"
	}
	eval('hhl.push([' + cart + '])');
	var forders = 'c:/qpos/lorder.txt'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(forders))
		fso.DeleteFile(forders);
	fso = new ActiveXObject('Scripting.FileSystemObject');
	var tf = fso.OpenTextFile(forders , 8, 1, -2);
	var sline = '';
	cart = hhl[0];
	for(var m=0; m<cart.length; m++)
	{
		if(m > 0)
			sline += ',';
		sline += '[';
		for(var t=0; t<cart[m].length; t++)
		{
			if(t > 0)
				sline += ',';
			if(t == 2)
";
s += "					sline += '\"' + cart[m][t] + '\"'; ";
s += @"
			else
				sline += cart[m][t];
		}
		sline += ']';
	}
	tf.WriteLine(sline);
	tf.Close();

}
function restore_last_order()
{
	hhl = new Array();
	var forders = 'c:/qpos/lorder.txt'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(!fso.FileExists(forders))
	{
		var tf = fso.OpenTextFile(forders , 8, 1, -2);
		tf.Write('');
		tf.Close();
		return;
	}
	else
	{
		var tf = fso.OpenTextFile(forders, 1, false); 
		while(!tf.AtEndOfStream)
		{
			var sline = tf.ReadLine();
			if(sline == '')
			{
				tf.Close();
				return;
			}
//			window.alert(sline);
			eval('hhl.push([' + sline + '])');
		}
		tf.Close();
	}

	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
			break;
		";
s += "		eval(\"document.f.rc\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rb\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rd\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.ro\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rp\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rq\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.md\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.np\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.st\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.del\" + n + \".style.visibility='hidden'\"); \r\n";
s += @"
	}
	var cart = hhl[0];
	var gh_code = '';
	var gh_barcode = '';
	var gh_sdesc = '';
	var gh_sgprice = '';
	var gh_sinb = '';
	for(var n=0; n<cart.length; n++)
	{
		gh_code = cart[n][0];
		gh_barcode = cart[n][1];
		gh_sdesc = cart[n][2];
		gh_sprice = cart[n][4];
";
s += "		eval(\"document.f.rc\" + n + \".value = cart[n][0]\"); \r\n ";
s += "		eval(\"document.f.rb\" + n + \".value = cart[n][1]\"); \r\n ";
s += "		eval(\"document.f.rd\" + n + \".value = cart[n][2]\"); \r\n ";
s += "		eval(\"document.f.ro\" + n + \".value = cart[n][3]\"); \r\n ";
s += "		eval(\"document.f.rp\" + n + \".value = cart[n][4]\"); \r\n ";
s += "		eval(\"document.f.rq\" + n + \".value = cart[n][5]\"); \r\n ";
s += "		eval(\"document.f.md\" + n + \".value = cart[n][6]\"); \r\n ";
s += "		eval(\"document.f.np\" + n + \".value = cart[n][7]\"); \r\n ";
s += "		eval(\"document.f.st\" + n + \".value = cart[n][8]\"); \r\n ";
s += "		eval(\"document.f.del\" + n + \".style.visibility='visible'\"); \r\n ";
s += @"
	}
	document.f.code.value = gh_code;
	document.f.desc.value = gh_sdesc;
	document.f.price.value = gh_sgprice;
	document.f.last_barcode.value = gh_barcode;
	document.f.last_code.value = gh_code;
	document.f.rows.value = cart.length;

	ct();
	document.f.s.value='';
	document.f.md_qty.value='';
	document.f.md_dollar.value='';
	document.f.s.focus();
}
function reload_offline_app()
{
	var fn = 'c:/qpos/qpos.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false); 
	var s = tf.ReadAll(); 
	tf.Close(); 
	document.close();
	document.write(s);
	return true;
}
var scart = '';
function LoadPaymentForm()
{
	remember_last_order();
	var nrows = Number(document.f.rows.value);
	scart += '<form name=fc>';
	scart += '<input type=hidden name=branch value=' + document.f.branch.value + '>';
	scart += '<input type=hidden name=branch_name value=' + document.f.branch_name.value + '>';
	scart += '<input type=hidden name=sales value=' + document.f.sales.value + '>';
	scart += '<input type=hidden name=sales_name value=' + document.f.sales_name.value + '>';
	scart += '<input type=hidden name=agent value=' + document.f.agent.value + '>';
	scart += '<input type=hidden name=rows value=' + nrows + '>';
	scart += '<input type=hidden name=total value=' + document.f.total.value + '>';

	for(var i=0; i<=nrows; i++)
	{
		scart += '<input type=hidden name=rc' + i + ' value=' + eval('document.f.rc' + i + '.value') + '>';
		scart += '<input type=hidden name=rb' + i + ' value=' + eval('document.f.rb' + i + '.value') + '>';
		scart += '<input type=hidden name=rd' + i + ' value=' + eval('document.f.rd' + i + '.value') + '>';
		scart += '<input type=hidden name=rp' + i + ' value=' + eval('document.f.rp' + i + '.value') + '>';
		scart += '<input type=hidden name=rq' + i + ' value=' + eval('document.f.rq' + i + '.value') + '>';
	}
	scart += '</form>';
";

s += @"
	var dInvTotal = Number(document.f.total.value);
	var fn = 'c:/qpos/qpospay.hta'; 
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	tf = fso.OpenTextFile(fn, 1, false); 
	var s = tf.ReadAll(); 
	tf.Close(); 
	document.close();
	document.write(s);
	document.write(scart);
	document.f.cashin.focus();
	document.f.total.value = dInvTotal;
	document.f.total1.value = formatCurrency(dInvTotal);
";

	ASCIIEncoding encoding = new ASCIIEncoding( ); 
	byte[] select_display = {0x1b, 0x3d, 0x00, 0x02};
	byte[] select_printer = {0x1b, 0x3d, 0x00, 0x01};
	byte[] clear_screen = {0x00, 0x0c};
	byte[] move_home = {0x1f, 0x24, 0x01, 0x01};
	byte[] move_left_most = {0x08, 0x08, 0x08, 0x08, 0x08};
	string ssdisplay = encoding.GetString(select_display);
	string ssprinter = encoding.GetString(select_printer);
	string sclear = encoding.GetString(clear_screen);	
	string smovehome = encoding.GetString(move_home);
	string smoveleftmost = encoding.GetString(move_left_most);

	if(m_bDisplay)
	{
		s += " var s_d_desc = 'Total : '; \r\n";
		s += " var s_d_price = document.f.total1.value; \r\n";
		s += " var nspaces = 40 - s_d_desc.length - s_d_price.length; \r\n";
		s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
		s += " var s_display = s_d_desc + s_d_price; \r\n";
		s += " document.AsPrint1.Open('" + m_sDisplayPort + "')\r\n";
		s += " document.AsPrint1.PrintString('" + ssdisplay + sclear + smovehome + smoveleftmost + "');\r\n";
		s += " document.AsPrint1.PrintString(s_display);\r\n";
		s += " document.AsPrint1.Close();\r\n";
	}
s += @"
	}
		";
	sb.Append(s);
	sb.Append("</script");
	sb.Append(">\r\n ");

	string sbc = sb.ToString();
	m_shta += sbc.Replace("shelp = shelp.replace(re, '\\r\\n');", ""); //not for offline
//	sbc = sbc.Replace("else{LoadPaymentForm();return false;}", ""); //not for online
//	Response.Write(sbc);
//	Response.Write("<a href=qpostest.aspx?t=chl class=o>Download Offline Tools(Create shortcut on desktop)</a>");
	return true;
}

bool BuildSalesBarcodeCache()
{
//	if(Session["pos_sales_barcode"] != null)
//	{
//		m_sSalesBarcodeCache = Session["pos_sales_barcode"].ToString();
//		return true;
//	}
	
	int rows = 0;
	DataSet dsbc = new DataSet();
	string sc = " SELECT id, name, barcode FROM card WHERE type=4 AND barcode IS NOT NULL AND barcode <> '' ORDER BY name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsbc);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	StringBuilder sb = new StringBuilder();

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dsbc.Tables[0].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string barcode = dr["barcode"].ToString();
		if(barcode == "")
			continue;

		sb.Append("\r\n<input type=hidden name='sbb" + barcode + "' value=" + id + ">");
		sb.Append("\r\n<input type=hidden name='sbn" + id + "' value='" + name + "'>");
	}
	m_sSalesBarcodeCache = sb.ToString();
//	Session["pos_sales_barcode"] = m_sSalesBarcodeCache;
	return true;
}

bool BuildProductCache()
{
//	if(Session["pos_product_cache"] != null)
//	{
//		m_sProductCache = Session["pos_product_cache"].ToString();
//		return true;
//	}

	int rows = 0;
	DataSet dspc = new DataSet();
	string sc = " SELECT code, name, RTRIM(LTRIM(barcode)) AS barcode ";
	if(m_bFixedPrices)
		sc += ", price1 AS price "; //GST exclusive price
	else
		sc += ", supplier_price * rate * level_rate1 AS price ";
	sc += " FROM code_relations ";
//	sc += " where price1 <> 0 ";
	sc += " ORDER BY name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dspc);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	StringBuilder sb = new StringBuilder();
	sb.Append("<script language=javascript>\r\n");
	sb.Append("function searchitem(type, index, barcode){\r\n");
/*	sb.Append("var b = new Array(); c = new Array(); var n = new Array(); var p = new Array(); \r\n");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables[0].Rows[i];
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		name = name.Replace("\"", "`");
		name = name.Replace("\r\n", " ");
		string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 2).ToString();
		Trim(ref name);
		if(name == "")
			continue;

		if(name.Length > 24)
			name = name.Substring(0, 24);

//		sb.Append("b[" + i + "]=\"" + barcode + "\"; \r\n");
//		sb.Append("c[" + i + "]=\"" + code + "\"; \r\n");
//		sb.Append("n[" + i + "]=\"" + name + "\"; \r\n");
//		sb.Append("p[" + i + "]=\"" + price + "\"; \r\n");

//		sb.Append("b[\"" + barcode + "\"]=\"" + code + "\";");
//		sb.Append("n[\"" + code + "\"]=\"" + name + "\";");
//		sb.Append("p[\"" + code + "\"]=\"" + price + "\";");

//		sb.Append("\r\n<input type=hidden name=b" + barcode + " value=" + code + ">");
//		sb.Append("\r\n<input type=hidden name=d" + code + " value='" + name + "'>");
//		sb.Append("\r\n<input type=hidden name=p" + code + " value=" + price + ">");

	}
*/
	sb.Append(" var b = new Array(");
	int n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables[0].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string barcode = dr["barcode"].ToString().ToLower();

		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + barcode + "\"");
		n += 2 + barcode.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var c = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables[0].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string code = dr["code"].ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
//		sb.Append("\"" + code + "\"");
		sb.Append(code);
		n += code.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var p = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables[0].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 2).ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
//		sb.Append("\"" + price + "\"");
		sb.Append(price);
		n += price.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var n = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables[0].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;

		if(name.Length > 24)
			name = name.Substring(0, 24);
		name = name.Replace("\"", "");
		name = name.Replace("\r\n", " ");
		name = name.Replace("'", "");
		
		if(i > 0)
		{
			sb.Append(",");
			n += 1;
		}
		sb.Append("\"" + name + "\"");
		n += 2 + name.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");

	sb.Append(" if(barcode != ''){\r\n");
	sb.Append(" for(var i=0; i<b.length; i++){if(b[i] == barcode)return i} \r\n");
	sb.Append(" return -1; ");
	sb.Append("} \r\n");
	sb.Append(" else if(type == 'code'){return c[index];} \r\n");
	sb.Append(" else if(type == 'price'){return p[index];} \r\n");
	sb.Append(" else if(type == 'name'){return n[index];} \r\n");

	sb.Append("}\r\n");
	sb.Append("</script");
	sb.Append(">\r\n");

	m_sProductCache = sb.ToString();
//	Session["pos_product_cache"] = m_sProductCache;
	return true;
}

string PrintMJava()
{
	string s = "<script language=javascript>\r\n";
	if(!m_bDebug)
	{
		s += "self.moveTo(-4,-4);\r\n";
		s += "self.resizeTo((screen.availWidth+8),(screen.availHeight+8));\r\n";
	}

	ASCIIEncoding encoding = new ASCIIEncoding( ); 
	byte[] select_display = {0x1b, 0x3d, 0x00, 0x02};
	byte[] select_printer = {0x1b, 0x3d, 0x00, 0x01};
	byte[] clear_screen = {0x00, 0x0c};
	byte[] move_home = {0x1f, 0x24, 0x01, 0x01};
	byte[] move_left_most = {0x08, 0x08, 0x08, 0x08, 0x08};
	byte[] init_printer = {0x1b, 0x40};
	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
//	byte[] kick = {0x1b, 0x70, 0x00, 0x19, 0x7f, 0x00};//, 0x0a, 0x0};//new char[6];
	string ssdisplay = encoding.GetString(select_display);
	string ssprinter = encoding.GetString(select_printer);
	string sclear = encoding.GetString(clear_screen);	
	string smovehome = encoding.GetString(move_home);
	string smoveleftmost = encoding.GetString(move_left_most);
    string kickout = encoding.GetString(kick);// + "\\\\r\\\\n";
    string sinit = encoding.GetString(init_printer);

	if(m_bDisplay)
	{
//		s += " var s_display = 'Welcome to " + m_sCompanyTitle + "'; \r\n";
		s += " var s_display = '" + m_sCompanyTitle + "'; \r\n";
		s += " document.AsPrint1.Open('" + m_sDisplayPort + "')\r\n";
		s += " document.AsPrint1.PrintString('" + ssdisplay + sclear + smovehome + smoveleftmost + "');\r\n";
		s += " document.AsPrint1.PrintString(s_display);\r\n";
		s += " document.AsPrint1.Close();\r\n";
	}

	s += " var ssprinter = '" + ssprinter + "'; \r\n";
	s += @"
var shelp = 'key : function\\r\\n';
shelp += 'a : Search Agent\\r\\n';
shelp += 's : Sales Price Reference\\r\\n';
shelp += 'o : Order List\\r\\n';
shelp += 'c : Focus Barcode Field/Calculator\\r\\n';
shelp += 'l : Change Qty Price\\r\\n';
shelp += 'h : Hold Current Order\\r\\n';
shelp += 'd : Delete Current Order\\r\\n';
shelp += 'g : Print Agent Summary\\r\\n';
shelp += 'u : Print Last Receipt\\r\\n';
shelp += 'z : Open Cash Draw\\r\\n';
shelp += '. : Delete Last Item\\r\\n';
shelp += 'n : New Order\\r\\n';
shelp += '* : Set Last Qty to \\r\\n';
shelp += 'q : Get OnHold Order 1 \\r\\n';
shelp += 'w : Get OnHold Order 2 \\r\\n';
shelp += 'e : Get OnHold Order 3 \\r\\n';
shelp += 'r : Get OnHold Order 4 \\r\\n';
	function sh(field)
	{
		field.style.backgroundColor='#44ffff';
	}
	function rh(field)
	{
		field.style.backgroundColor='#ffffff';
	}
	function print_last_receipt()
	{
		var slreceipt = '';
		fn = 'c:/qpos/receipt.txt';
		fso = new ActiveXObject('Scripting.FileSystemObject'); 
		if(fso.FileExists(fn))
		{
			tf = fso.OpenTextFile(fn, 1, false); 
			slreceipt = tf.ReadAll();
			tf.Close(); 
		}
		if(slreceipt != '')
		{
			document.AsPrint1.Open(document.f.printer_port.value);
";
s += " document.AsPrint1.PrintString('" + sinit + "');\r\n";
if(m_bDisplay)
{
s += @"
			if(document.f.printer_port.value == document.f.display_port.value)
				document.AsPrint1.PrintString(ssprinter);
";
}
s += @"
			document.AsPrint1.PrintString(slreceipt);
			document.AsPrint1.Close();
		}
		else
			window.open('qpostest.aspx?t=plr');
	}
	function kick_cashdraw()
	{
";
	s += " document.AsPrint1.Open(document.f.printer_port.value)\r\n";
	s += " document.AsPrint1.PrintString('" + kickout + "');\r\n";
	s += " document.AsPrint1.PrintString('" + sinit + "');\r\n";
//	s += " document.AsPrint1.PrintString('\\\\r\\\\n');\r\n";
	s += " document.AsPrint1.Close();\r\n";
s += @"
	}
	function focus_last_row()
	{
		if(document.f.rows.value == '0')
			return;
		var nf_current = Number(document.f.focus_field.value) + 1;
		if(nf_current > 2)
			nf_current = 0;
		document.f.focus_field.value = nf_current;
		var field_to_focus = 'rp';
		if(nf_current == 1)
			field_to_focus = 'rq';
		else if(nf_current == 2)
			field_to_focus = 'st';
		field_to_focus += (Number(document.f.rows.value) - 1);
";
s += "		eval('document.f.' + field_to_focus + '.focus();'); ";
s += "		eval('document.f.' + field_to_focus + '.select();'); ";
s += @"
	}
	function set_special_printer_port()
	{
		var sport = '';
		fn = 'c:/qpos/p_port.txt';
		fso = new ActiveXObject('Scripting.FileSystemObject'); 
		if(fso.FileExists(fn))
		{
			tf = fso.OpenTextFile(fn, 1, false); 
			try
			{
				sport = tf.ReadAll();
			}
			catch(err)
			{
			}
			tf.Close(); 
		}

		sport = window.prompt('Special Printer Port?', sport);
		if(sport == null)
			return;
		if(sport == '' && sport == document.f.printer_port.value)
			return;
		if(fso.FileExists(fn))
			fso.DeleteFile(fn);
		tf = fso.OpenTextFile(fn , 8, 1, -2);
		tf.Write(sport);
		tf.Close();
	}
	function do_item_search()
	{
		var sbarcode = 'test';
//		document.AsPrint1.Open('COM1');
		sbarcode = document.AsPrint1.SearchItem();
		if(sbarcode != '')
		{
			document.f.s.value = sbarcode;
			onscan();
		}
//		document.AsPrint1.Close();
//		window.alert(sbarcode);
	}
	function on_form_keydown()
	{
//		window.alert(event.keyCode);
		var k = event.keyCode;
		switch(k)
		{
		case 73:
			do_item_search();
			break;
		case 85:
			print_last_receipt();
			break;
		case 88:
			set_special_printer_port();
			break;
		case 90:
			kick_cashdraw();
			break;
		case 76:
			focus_last_row();
			break;
		case 79:
			window.open('olist.aspx?o=14');
			break;
		case 83:
			window.open('salesref.aspx?code=' + document.f.last_code.value);
			break;
		case 65:
			window.open('card.aspx?type=2');
			break;
		case 67:
			document.f.s.focus();
			document.f.s.value = '';
			break;
		case 71:
			window.open('ragent.aspx');
			break;
		case 72:
			hold_current();
			break;
		case 68:
			del_current(false);
			break;
		case 78:
			reload_offline_app();
			break;
		case 191:
			re = /\\r\\n/g;
			shelp = shelp.replace(re, '\r\n');
			window.alert(shelp);
			break;
		case 81:
			get_hold(1);
			break;
		case 87:
			get_hold(2);
			break;
		case 69:
			get_hold(3);
			break;
		case 82:
			get_hold(4);
			break;
		default:
			return true;
			break;
		}
		return false;
	}
	function CheckSalesBarcode()
	{
		var barcode = document.f.sales_barcode.value;
		if(barcode == '')return;
		";
s += "	if(eval(\"document.f.sbb\" + barcode) == null || + eval(\"document.f.sbb\" + barcode) == 'undefined') \r\n";
s += "{window.alert('Sales Not Found.');document.f.sales_barcode.select();document.f.sales_barcode.focus();return;}  \r\n";
s += "	eval(\"document.f.sales.value = document.f.sbb\" + barcode + \".value\");  \r\n";
s += " var id = document.f.sales.value; \r\n";
s += " if(id == 'undefined') return;\r\n ";
s += "	eval(\"document.f.sales_name.value = document.f.sbn\" + id + \".value\");  \r\n";
s += @"
	}
	function IsNumberic(sText)
	{
		var ValidChars = '-0123456789.';
		var IsNumber=true;
		var Char;
		for (i = 0; i < sText.length && IsNumber == true; i++) 
		{ 
			Char = sText.charAt(i); 
			if(ValidChars.indexOf(Char) == -1) 
				IsNumber = false;
		}
		return IsNumber;
	}
	function onscan()
	{
		var sin = document.f.s.value;
		if(sin.indexOf('+') > 0 || sin.indexOf('-') > 0 || sin.indexOf('*') > 0 || sin.indexOf('/') > 0)
		{
			var calc = eval(sin);
			window.alert(sin + ' = ' + calc.toFixed(4));
			document.f.s.focus();
			document.f.s.select();
			return true;
		}
		if(document.f.agent_name.value == '' && document.f.agent_code.value != '00')
		{
			window.alert('Please enter agent code.');
			document.f.agent_code.select();
			document.f.agent_code.focus();
			return true;
		}
		if(document.f.s.value == '')
		{
			if(document.f.sales.value == '')
			{
				window.alert('Please scan sales barcode.');
				document.f.sales_barcode.select();
				document.f.sales_barcode.focus();
				return true;
			}
			if(document.f.confirm_checkout.value == '1')
			{
				return true; //no double invoicing, already confirmed, checking out
			}
			if(document.f.rows.value == '0')
			{
				document.f.s.focus();
				return true;
			}
			var bconfirm = window.confirm('Confirm to checkout?');
			document.f.s.focus();
			if(bconfirm)
			{
				document.f.confirm_checkout.value = 1;
				del_current(true);
			}
			return !bconfirm;
		}
		var i = Number(document.f.rows.value);
";
	
	s += @"
		var sinb = document.f.s.value;
		var nqty = 1;
		
		var md_q = document.f.md_qty.value;
		var md_d = document.f.md_dollar.value;
		var md_ad = document.f.agent_discount.value;
		var gst_rate = document.f.gst_rate.value;

		if(md_q.length >4)
			document.f.md_qty.value = '';
		if(md_d.length >5)
			document.f.md_dollar.value = '';
		var b_total = false;
		if(md_d != '' && md_d.substring(0, 1) == 't') //total price
		{
			b_total = true;
			md_d = md_d.substring(1, md_d.length);
		}
		if(md_q == '')
			md_q = 1;
		if(!IsNumberic(md_q))
		{
			//window.alert('Warning!!! Wrong Discounted Percentage ' + document.f.md_qty.value);
			window.alert('Warning!!! Invalid QTY ' + document.f.md_qty.value);
			document.f.md_qty.value = '';
			md_q = '';
		}
		if(!IsNumberic(md_d))
		{
			//window.alert('Warning!!! Wrong Discounted Price:' + document.f.md_dollar.value);
			window.alert('Warning!!! Invalid Selling Price:' + document.f.md_dollar.value);
			document.f.md_dollar.value = '';
			md_d = '';
		}
		if(!IsNumberic(md_ad))
		{
			window.alert('Warning!!! Wrong Agent Discounted Percentage ' + document.f.agent_discount.value);
			document.f.agent_discount.value = 0;
			md_ad = 0;
		}

		if(sin.substring(0, 1) == '*')
		{
			nqty = Number(sin.substring(1));
			if(i > 0)
				i -= 1;
		";
	s += "	eval(\"document.f.rq\" + i + \".value = nqty\");  \r\n";
	s += @"
			ct();
			document.f.s.value='';
			document.f.s.focus();
			return true;
		}
		else if(sin.substring(0, 1) == '.' || sin.substring(0, 1) == '/')
		{
			document.f.md_dollar.value='';
			document.f.md_qty.value='';
			removelastone();
			return true;
		}
		var barcode = sin;
		";

	s += "	var qty = eval(\"document.f.rq\" + i + \".value\"); \r\n";
	s += @"	
		if(qty == ''){qty = '1'};
		if(b_total)
		{
			md_d = (Number(md_d) / Number(md_q)).toFixed(4);
		}
		";
		
	//double up for the duplicate items
if(!m_bNoDoubleQTY)
{
	s += "	if(i>0 && sin == document.f.last_barcode.value) \r\n";
	s += "	{		\r\n";
	s += "		i--; \r\n";
	s += "		qty = eval(\"Number(document.f.rq\" + i + \".value) + 1\") \r\n";
	s += "		eval(\"document.f.rq\" + i + \".value = qty\"); \r\n";
	s += "		ct(); \r\n";
	s += "		document.f.s.value=''; \r\n";
	s += "		document.f.s.focus(); \r\n";
	s += "		return true; \r\n";
	s += "	}		\r\n"; 

}
	s += " var index = searchitem('', 0, barcode);\r\n";
	s += " if(index == -1){sin = 'badbarcode'; \r\n";
	s += " window.alert('Not Found');";
	s += "	ct();";
	s += "	document.f.s.value='';";
	s += "	document.f.md_qty.value='';";
	s += "	document.f.md_dollar.value='';";
	s += "	document.f.s.focus();";
	s += "	return true;";
	s += "	} else { sin = searchitem('code', index, ''); \r\n";
	s += "	var sdesc = searchitem('name', index, ''); \r\n";
	s += "	var sprice = searchitem('price', index, ''); \r\n";
	s += " var normal_price = Number(sprice); \r\n";
	s += " var sgprice = sprice; \r\n";
	s += " if(md_d > 0){ ";
	s += "  gst_rate = 1; ";
	s += " } ";
	s += " if( (md_d != '' && md_d > 0 ) || document.f.md_dollar.value == '0') ";
	s += "  sgprice = (md_d * gst_rate).toFixed(2); \r\n";
	s += " else ";
	s += " sgprice = sprice.toFixed(2); \r\n ";
	s += "	normal_price = sgprice; \r\n"; //GST inclusive
	s += " var md = '0'; ";
	s += " md = (sgprice  * (md_ad/100)).toFixed(2); ";
	
	s += "if(md_ad > 0) {";
	s += " md = document.f.agent_discount.value; ";
	s += " sgprice = (sgprice * (1 - md_ad/100)).toFixed(2); ";
	
	s += " } ";
	s += "	eval(\"document.f.rc\" + i + \".value = sin\"); \r\n";
	s += "	eval(\"document.f.rb\" + i + \".value = barcode\"); \r\n";
	s += "	eval(\"document.f.rd\" + i + \".value = sdesc\"); \r\n";
	s += "	eval(\"document.f.ro\" + i + \".value = sprice\"); \r\n";  //GST exclusive price
	s += "	eval(\"document.f.rp\" + i + \".value = sgprice\"); \r\n"; //GST Inclusive price
	
	s += "	eval(\"document.f.np\" + i + \".value = normal_price\"); \r\n"; //GST Inclusive price
//	s += "	eval(\"document.f.rq\" + i + \".value = qty\"); \r\n";
	s += "	eval(\"document.f.rq\" + i + \".value = md_q\"); \r\n";
	s += "	eval(\"document.f.md\" + i + \".value = md\"); \r\n";
	s += "	eval(\"document.f.st\" + i + \".value = sgprice * qty\"); \r\n";
	s += "	eval(\"document.f.del\" + i + \".style.visibility='visible'\"); \r\n";
	s += "	document.f.code.value = sin; \r\n";
	s += "	document.f.desc.value = sdesc; \r\n";
	s += "	document.f.price.value = sgprice; \r\n";
	s += "	document.f.last_barcode.value = sinb; \r\n";
	s += "	document.f.last_code.value = sin; \r\n";
		
	if(m_bDisplay)
	{
		s += " var s_d_desc = sdesc; \r\n";
		s += " if(s_d_desc.length > 30){s_d_desc = s_d_desc.substring(0, 30);} \r\n";
		s += " var s_d_price = '$' + sgprice; \r\n";
		s += " var nspaces = 40 - s_d_desc.length; \r\n";
		s += " nspaces -= s_d_price.length; \r\n";
		s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
		s += " var s_display = ''; \r\n";
		s += " s_display += s_d_desc; \r\n";
		s += " s_display += s_d_price; \r\n";
		s += " document.AsPrint1.Open('" + m_sDisplayPort + "')\r\n";
		s += " document.AsPrint1.PrintString('" + ssdisplay + sclear + smovehome + smoveleftmost + "');\r\n";
		s += " document.AsPrint1.PrintString(s_display);\r\n";
		s += " document.AsPrint1.Close();\r\n";
	}

	s += " } ";
	
	s += @"

		document.f.rows.value = i + 1;
	
		ct();
		document.f.s.value='';
		document.f.md_qty.value='';
		document.f.md_dollar.value='';
		document.f.s.focus();
		return true;
	}
	";

	s += @"

	function removelastone()
	{
		document.f.code.value = '';
		document.f.desc.value = '';
		document.f.price.value = '';

		var i = Number(document.f.rows.value) - 1;
		if(i < 0)
		{
			document.f.s.value='';
			document.f.s.focus();
			return;
		}
	";
	s += "	eval(\"document.f.rc\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rb\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rd\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.ro\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rp\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rq\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.md\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.np\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.st\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.del\" + i + \".style.visibility='hidden'\"); ";
	s += @"
		document.f.rows.value = i;
		ct();
		document.f.s.value='';
		document.f.s.focus();

		";

	s += " if(i > 0)\r\n{document.f.last_code.value = eval(\"document.f.rc\" + (i-1) + \".value\");document.f.last_barcode.value = eval(\"document.f.rb\" + (i-1) + \".value\");} \r\n";

	s += @"
	}

	function cp(row)
	{
		";
	s += " var p = Number(eval(\"document.f.rp\" + row + \".value\")); \r\n";
	s += " var np = Number(eval(\"document.f.np\" + row + \".value\")); \r\n";
//s += "window.alert('np='+np);";
	s += " var discount = ((np-p)/np*100).toFixed(2); \r\n";
	s += " var sd = discount; \r\n";
	s += " eval(\"document.f.md\" + row + \".value='\" + sd + \"'\"); \r\n";

	s += @"
		ct();
	}
	function cd(row)
	{
		";
	s += " var np = Number(eval(\"document.f.np\" + row + \".value\")); \r\n";
	s += " var d = eval(\"document.f.md\" + row + \".value\"); \r\n";
	s += " d = d.replace('%', ''); \r\n";
	s += " var p = np * (1 - Number(d)/100 ); \r\n";
	s += " eval(\"document.f.rp\" + row + \".value='\" + p.toFixed(2) + \"'\"); \r\n";

	s += @"
		ct();
	}
	function cs(row)
	{
		";
	s += " var q = Number(eval(\"document.f.rq\" + row + \".value\")); \r\n";
	s += " var np = Number(eval(\"document.f.np\" + row + \".value\")); \r\n";
	s += " var st = Number(eval(\"document.f.st\" + row + \".value\")); \r\n";
	s += " var p = (st/q).toFixed(2); \r\n";
	s += " var d = (np - p)/np * 100; \r\n";
	s += " eval(\"document.f.rp\" + row + \".value='\" + p + \"'\"); \r\n";
	s += " eval(\"document.f.md\" + row + \".value='\" + d.toFixed(2) + \"'\"); \r\n";
	s += @"
		ct();
	}

	function del(row)
	{
		var rows = Number(document.f.rows.value);
		for(var i=row; i<=rows; i++)
		{
			";
	s += " var j = i + 1; \r\n";
	s += " var c = eval(\"document.f.rc\" + j + \".value\"); \r\n";
	s += " var b = eval(\"document.f.rb\" + j + \".value\"); \r\n";
	s += " var d = eval(\"document.f.rd\" + j + \".value\"); \r\n";
	s += " var q = eval(\"document.f.rq\" + j + \".value\"); \r\n";
	s += " var md = eval(\"document.f.md\" + j + \".value\"); \r\n";
	s += " var p = eval(\"document.f.rp\" + j + \".value\"); \r\n";
	s += " var o = eval(\"document.f.ro\" + j + \".value\"); \r\n";
	s += " var np = eval(\"document.f.np\" + j + \".value\"); \r\n";
	s += " if(i+1 == rows){ \r\n";
		s += " eval(\"document.f.rc\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rb\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rd\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rq\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.md\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rp\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.ro\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.np\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.st\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.del\" + i + \".style.visibility='hidden'\"); \r\n";
	s += "}else{ \r\n";
		s += " eval(\"document.f.rc\" + i + \".value=c\"); \r\n";
		s += " eval(\"document.f.rb\" + i + \".value=b\"); \r\n";
		s += " eval(\"document.f.rd\" + i + \".value=d\"); \r\n";
		s += " eval(\"document.f.rq\" + i + \".value=q\"); \r\n";
		s += " eval(\"document.f.md\" + i + \".value=md\"); \r\n";
		s += " eval(\"document.f.rp\" + i + \".value=p\"); \r\n";
		s += " eval(\"document.f.ro\" + i + \".value=o\"); \r\n";
		s += " eval(\"document.f.np\" + i + \".value=np\"); \r\n";
	s += "} \r\n";
	s += @"
		}
		document.f.rows.value = rows - 1;
		document.f.last_barcode.value = '';
		document.f.last_code.value = '';
		document.f.s.focus();
		ct();
	}

	function ct()
	{
		var dtotal = 0;
		var rows = Number(document.f.rows.value);
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += "	eval(\"document.f.st\" + j + \".value = (Number(document.f.rp\" + j + \".value) * Number(document.f.rq\" + j + \".value)).toFixed(2)\");  \r\n";
	s += " dtotal += Number(eval(\"document.f.rp\" + j + \".value\")) * Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += @"
		}
		document.f.total.value = Math.round(dtotal * 100) / 100;

	}

	function CalcTotalDiscount()
	{
		var dtotal = 0;
		var rows = Number(document.f.rows.value);
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += " dtotal += Number(eval(\"document.f.rp\" + j + \".value\")) * Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += @"
		}

		var ddiscount = (dtotal - Number(document.f.total.value));
		dtotal = 0;
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += " var np = Number(eval(\"document.f.np\" + j + \".value\")); \r\n";
	s += " var p = Number(eval(\"document.f.rp\" + j + \".value\")); \r\n";
	s += " var qty = Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += " ddiscount /= qty; \r\n";
	s += " var new_p = (p - ddiscount).toFixed(2); \r\n";
	s += " eval(\"document.f.md\" + j + \".value = ((np-new_p)/np * 100).toFixed(2);\"); \r\n";
	s += "eval(\"document.f.rp\" + j + \".value = new_p;\"); \r\n";
	s += " break; \r\n";
	s += @"
		}
		ct();
	}
	
	";

	s += "</script";
	s += ">";

	return s;
}

bool BuildShoppingCart()
{
	if(Request.Form["rows"] == null)
		return false;

	int rows = MyIntParse(Request.Form["rows"]);
	for(int i=0; i<rows; i++)
	{
		string code = Request.Form["rc" + i];
		string qty = Request.Form["rq" + i];
		string mark_down_percent = "0";
		string mark_down_price = "0";
		string mark_down = Request.Form["md" + i];
		
		if(mark_down.IndexOf("%") >= 0)
			mark_down_percent = mark_down.Replace("%", "");
		if(mark_down.IndexOf("$") >= 0)
			mark_down_price = mark_down.Replace("$", "");

		string price = Request.Form["rp" + i];
		double dPrice = MyMoneyParse(price);
//DEBUG("form price = ", dPrice.ToString());
		dPrice /= m_gst;
		dPrice = Math.Round(dPrice, 4);
//DEBUG("code cart = ", code);
//DEBUG("qty cart= ", qty);
//DEBUG("price cart = ", dPrice.ToString());
		AddToCart(code, qty, dPrice.ToString());
	}
	return true;
}
bool PrintPaymentForm(bool bhta)
{
	StringBuilder sb = new StringBuilder();
	string sheader = ReadSitePage("qpos_header");
	sb.Append(sheader);

	if(m_bTrustedSite)
	{
		sb.Append("\r\n<object classid=\"clsid:B816E029-CCCB-11D2-B6ED-444553540000\" ");
		sb.Append(" CODEBASE=\"asprint.ocx\" ");
		sb.Append(" id=\"AsPrint1\">\r\n");
		sb.Append("<param name=\"_Version\" value=\"65536\">\r\n");
		sb.Append("<param name=\"_ExtentX\" value=\"2646\">\r\n");
		sb.Append("<param name=\"_ExtentY\" value=\"1323\">\r\n");
		sb.Append("<param name=\"_StockProps\" value=\"0\">\r\n");
		sb.Append("<param name=\"HideWinErrorMsg\" value=\"1\">\r\n");
		sb.Append("</object>\r\n");
	}

	sb.Append("<table width=100% border=0 cellspacing=0 cellpadding=3 bordercolor=#eeeeee  bgcolor=#EEEEE >");
	sb.Append("<tr align=left><td colspan=2>");
	sb.Append("<font color=#444444><b><br>&nbsp;<img border=0 src='" + m_sServer + "/i/eznz.gif'></td></tr>");
	sb.Append("<tr><td>P.O.S. v.1 - </b></font>");
	sb.Append("</td></tr>");
	sb.Append("</table>\r\n");

	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");

	sb.Append("<br><br><br>");
	sb.Append("<form name=f action=qpostest.aspx?t=end&i=replace_with_invoice_number");
	if(!m_bTrustedSite)
		sb.Append("&nts=1");
	sb.Append(" method=post>\r\n");
	sb.Append("<input type=hidden name=printer_port value='" + m_sReceiptPort + "'>");
	sb.Append("<input type=hidden name=display_port value='" + m_sDisplayPort + "'>");
	sb.Append("<input type=hidden name=invoice_number value=replace_with_invoice_number>\r\n");
	sb.Append("<input type=hidden name=pm value='cash'>\r\n");

	sb.Append("<input type=hidden name=payment_total value=0>\r\n");
	sb.Append("<input type=hidden name=payment_cash value=>\r\n");
	sb.Append("<input type=hidden name=payment_eftpos value=>\r\n");
	sb.Append("<input type=hidden name=payment_cheque value=>\r\n");
	sb.Append("<input type=hidden name=payment_cc value=>\r\n");
	sb.Append("<input type=hidden name=payment_bankcard value=>\r\n");
	sb.Append("<input type=hidden name=cash_out value=>\r\n");

	sb.Append("<table width=50% height=50% align=center valign=center border=1 cellspacing=0 cellpadding=0 bordercolor=#eeeeee bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");

	sb.Append("<tr><td colspan=2 align=center><h2>Payment</h3></td></tr>\r\n");

	sb.Append("<tr><td colspan=2>");
	sb.Append("<input type=text name=pmt value=CASH class=f style='font-weight:bold;font-size:30;'>");
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td><input type=text name=ttotal value='Total' class=f style='font-size:30'></td>");
	sb.Append("<td align=right>");
	
	sb.Append("<input type=text name=total1 readonly size=5 value=" + m_dInvoiceTotal.ToString("c") + " class=f style='text-align:right;font-weight:bold;font-size:30;'>");
	sb.Append("<input type=hidden name=total value=" + m_dInvoiceTotal + ">");
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td><input type=text name=tcashin value='Cash In' class=f style='font-size:30'></td>\r\n");
	sb.Append("<td align=right><input type=text name=cashin size=5 class=f style='text-align:right;font-weight:bold;font-size:30'");
	if(bhta)
		sb.Append(" onKeyDown=\"if(event.keyCode==13)document.f.cmd.focus();\"");
	sb.Append("></td></tr>\r\n");

	sb.Append("<tr><td><input type=text name=tcashchange value='Change' class=f style='font-size:30'></td>");
	sb.Append("<td align=right><input type=text name=cashchange size=5 class=f style='text-align:right;font-weight:bold;font-size:30;'></td></tr>\r\n");
	sb.Append("<tr><td colspan=2 align=right>");
	if(!bhta)
		sb.Append("<input type=submit name=cmd value=OK class=b onclick=\"if(!paymentok()){return false;}\">");
	else //for offline hta
		sb.Append("<input type=button name=cmd value=OK class=b onclick=\"if(paymentok()){WriteOrder();}\">");
	sb.Append("<input type=button value=Delete class=b ");
	sb.Append(" onclick=\"if(!window.confirm('Click Cancle to delete this order, Click OK to return.')){delete_order();}\">");
	if(m_bTrustedSite)
	{
		sb.Append("<input type=button value=Back class=b ");
		sb.Append(" onclick=\"if(!window.confirm('Click Cancle to modify order, Click OK to return.')){modify_order();}\">");
	}
	sb.Append("</td></tr>\r\n");
	sb.Append("</table>");
	sb.Append("</form>\r\n");

	string sj = PrintPaymentJava(bhta);
//	if(!bhta)
//		sj = sj.Replace("\\\\r\\\\n", "\\r\\n");
	sb.Append(sj);

	m_sPaymentForm = sb.ToString();
	m_sPaymentFormOnlineCache = sb.ToString();
//	if(!bhta) //output
//		Response.Write(sb.ToString());
	return true;
}

string PrintPaymentJava(bool bhta)
{
	string s = "<script language=javascript>\r\n";

	ASCIIEncoding encoding = new ASCIIEncoding( ); 
	byte[] select_display = {0x1b, 0x3d, 0x00, 0x02};
	byte[] select_printer = {0x1b, 0x3d, 0x00, 0x01};
	byte[] clear_screen = {0x00, 0x0c};
	byte[] move_home = {0x1f, 0x24, 0x01, 0x01};
	byte[] move_left_most = {0x08, 0x08, 0x08, 0x08, 0x08};
	string ssdisplay = encoding.GetString(select_display);
	string ssprinter = encoding.GetString(select_printer);
	string sclear = encoding.GetString(clear_screen);	
	string smovehome = encoding.GetString(move_home);
	string smoveleftmost = encoding.GetString(move_left_most);
	if(m_bDisplay && !bhta)
	{
		s += " var s_d_desc = 'Total : '; \r\n";
		s += " var s_d_price = document.f.total1.value; \r\n";
		s += " var nspaces = 40 - s_d_desc.length - s_d_price.length; \r\n";
		s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
		s += " var s_display = s_d_desc + s_d_price; \r\n";
		s += " document.AsPrint1.Open('" + m_sDisplayPort + "')\r\n";
		s += " document.AsPrint1.PrintString('" + ssdisplay + sclear + smovehome + smoveleftmost + "');\r\n";
		s += " document.AsPrint1.PrintString(s_display);\r\n";
		s += " document.AsPrint1.Close();\r\n";
	}
	if(bhta)
	{
		s += @"
function formatCurrency(num) 
{
	num = num.toString().replace(/\$|\,/g,'');
	if(isNaN(num))
		num = '0';
	sign = (num == (num = Math.abs(num)));
	num = Math.floor(num*100+0.50000000001);
	cents = num%100;
	num = Math.floor(num/100).toString();
	if(cents<10)
		cents = '0' + cents;
	for(var i = 0; i < Math.floor((num.length-(1+i))/3); i++)
		num = num.substring(0,num.length-(4*i+3))+','+
	num.substring(num.length-(4*i+3));
	return (((sign)?'':'-') + '$' + num + '.' + cents);
}

function GetNextInvNumber()
{
	var inv = 10001;
	var fn = 'c:/qpos/qposni.txt'; 
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(!fso.FileExists(fn))
	{
		fso = new ActiveXObject('Scripting.FileSystemObject');
		tf = fso.OpenTextFile(fn , 8, 1, -2);
		tf.Write(inv);
		tf.Close();
		return inv;
	}
	tf = fso.OpenTextFile(fn, 1, false); 
	inv = Number(tf.ReadAll());
	tf.Close(); 
	fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -2);
	tf.Write(inv+1);
	tf.Close();
	return inv;
}
function WriteOrder()
{
	var dCash = Number(document.f.payment_cash.value);
	var dEftpos = Number(document.f.payment_eftpos.value);
	var dCreditCard = Number(document.f.payment_cc.value);
	var dBankcard = Number(document.f.payment_bankcard.value);
	var dCheque = Number(document.f.payment_cheque.value);

	var inv_num = GetNextInvNumber();
	var invTotal = Number(document.fc.total.value);
	var sInvTotal = formatCurrency(invTotal);
	var branch = document.fc.branch.value;
	var branch_name = document.fc.branch_name.value;
	var sales = document.fc.sales.value;
	var sales_name = document.fc.sales_name.value;
	var agent = document.fc.agent.value;
	var d = new Date();
	var sd = d.getDate() + '/' + (d.getMonth()+1) + '/' + d.getFullYear() + ' ' + d.getHours() + ':' + d.getMinutes() + ':' + d.getSeconds();
	var s = 'invoice begin,' + inv_num + ',' + sd + ',' + branch + ',' + sales + ',' + agent + ',' + invTotal + ',' + dCash + ',' + dEftpos + ',' + dCreditCard + ',' + dBankcard + ',' + dCheque + '\\r\\n';
	var nrows = Number(document.fc.rows.value);
	var sp = '';
	for(var i=0; i<=nrows; i++)
	{
		var code = eval('document.fc.rc' + i + '.value');
		if(code == '')
			break;
		var name = eval('document.fc.rd' + i + '.value');
		var barcode = eval('document.fc.rb' + i + '.value');
		var price = formatCurrency(Number(eval('document.fc.rp' + i + '.value')));
		var qty = eval('document.fc.rq' + i + '.value');
		s += code + ',' + price + ',' + qty + '\\r\\n';

		sp += name + '\\r\\n';

		var slcode = '     ' + price;
		var len = 20 - slcode.length;
		sp += slcode;
		for(var n=0; n<len; n++)
			sp += ' ';
		price = formatCurrency(Number(eval('document.fc.rp' + i + '.value')) * Number(eval('document.fc.rq' + i + '.value')));
		len = qty.length + 1 + price.length;
		len = 22 - len;
		sp += 'x' + qty;
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += price + '\\r\\n';
	}

	var si = nrows + ' Items        TOTAL';
	sp += si;
	si += sInvTotal;
	var len = 42 - si.length;
	for(var n=0; n<len; n++)
		sp += ' ';
	sp += sInvTotal + '\\r\\n';

	var pm = document.f.pm.value;
	pm = pm.toUpperCase();
	len = 20 - pm.length;
	for(n=0; n<len; n++)
		sp += ' ';
	sp += pm;
	if(pm == 'CASH')
	{
		var cashin = formatCurrency(dCash);
		var cashchange = formatCurrency(Number(document.f.cashchange.value));
		len = 22 - cashin.length;
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += cashin + '\\r\\n';
		for(var n=0; n<14; n++)
			sp += ' ';
		sp += 'CHANGE';
		len = 22 - cashchange.length;
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += cashchange + '\\r\\n';
	}
	else
	{
		len = 22 - sInvTotal.Length;
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += sInvTotal + '\\r\\n';
	}

	sp += '\\r\\n';
		";

s += " var sReceipt = \"" + m_sReceiptHeader + "\";\\r\\n";
s += " sReceipt += sp;\\r\\n";
s += " sReceipt += \"" + m_sReceiptFooter.Replace("\r\n", "\\\r\\\n") + "\"\\r\\n";
s += " var s_kickout = \"" + m_sReceiptKickout + "\"\\r\\n";

s += @"
	s += 'invoice end\\r\\n';
//	s += sReceipt;
	try 
	{
		var fso, tf;
		var fn = 'c:/qpos/qposinv.csv';
		fso = new ActiveXObject('Scripting.FileSystemObject');
		tf = fso.OpenTextFile(fn , 8, 1, -2);
		tf.Write(s);
		tf.Close();
	}
	catch(err)
	{
		var strErr = 'Error:';
		strErr += '\\r\\nNumber:' + err.number;
		strErr += '\\r\\nDescription:' + err.description;
//		window.alert(strErr);
		return false;
	}
	sReceipt = sReceipt.replace('@@sales', sales_name);
	sReceipt = sReceipt.replace('@@date', sd);
	sReceipt = sReceipt.replace('@@time', '');
	sReceipt = sReceipt.replace('@@inv_num', inv_num);
//	document.write(sReceipt);

	fn = 'c:/qpos/receipt.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
		fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -2);
	tf.Write(sReceipt);
	tf.Close();

//	if(Number(document.f.cashchange.value) > 0)
//		sReceipt += s_kickout;
	";

//	s += "for(var n=0; n<" + m_nNoOfReceiptPrintOut + ";n++)\r\n";
//	s += "{\r\n";
	s += " document.AsPrint1.Open('" + m_sReceiptPort + "')\r\n";
	s += " document.AsPrint1.PrintString(sReceipt);\r\n";
	s += " document.AsPrint1.PrintString('" + m_sReceiptKickout + "');\r\n";
	s += " document.AsPrint1.Close();\r\n";
//	s += "}\r\n";

s += @"
	reload_offline_app();
}
function reload_offline_app()
{
	var fn = 'c:/qpos/qpos.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false); 
	var s = tf.ReadAll(); 
	tf.Close(); 
	document.close();
	document.write(s);
	return true;
}
		";
	}

	s += @"
	document.f.cashin.focus();
	function CalcPaymentTotal()
	{
		var total = 0;
		total += Number(document.f.payment_cash.value);
		total += Number(document.f.payment_eftpos.value);
		total += Number(document.f.payment_cheque.value);
		total += Number(document.f.payment_cc.value);
		total += Number(document.f.payment_bankcard.value);
		document.f.payment_total.value = Math.round(total * 100) / 100;
		var invoice_total = Number(document.f.total.value);
		var res = invoice_total - total;
		return res;
	}
	function MsgNotEnough(cashin)
	{
		var payment_total = Number(document.f.payment_total.value);
		var invoice_total = Number(document.f.total.value);
		var res = Math.round( (invoice_total - payment_total) * 100) / 100;
		var p_cash = Number(document.f.payment_cash.value);
		var p_eftpos = Number(document.f.payment_eftpos.value);
		var p_cheque = Number(document.f.payment_cheque.value);
		var p_cc = Number(document.f.payment_cc.value);
		var p_bankcard = Number(document.f.payment_bankcard.value);
		
		var msg = 'Payment Shortage.\\r\\n\\r\\n';
		msg += 'Invoice Total : $' + invoice_total + '\\r\\n';
		msg += 'Paid Total : $' + payment_total + '\\r\\n';
		msg += '------------------------------------\\r\\n';
		if(p_cash != 0)
			msg += 'Cash : $' + p_cash + '\\r\\n';
		if(p_eftpos != 0)
			msg += 'EFTPOS : $' + p_eftpos + '\\r\\n';
		if(p_cheque != 0)
			msg += 'Cheque : $' + p_cheque + '\\r\\n';
		if(p_cc != 0)
			msg += 'CREDIT Card : $' + p_cc + '\\r\\n';
		msg += '------------------------------------\\r\\n';
		msg += 'Shortage : $' + res + '\\r\\n';
		window.alert(msg);
		shownextform();
	}
	function MsgConfirmCheckout()
	{
		var bconfirm = window.confirm('Confirm Payment OK?');
		if(!bconfirm)
		{
			document.f.cashin.value = '';
			document.f.cashchange.value = '';
			document.f.cashin.focus();
		}
		return bconfirm;
	}
	function shownextform()
	{
		document.f.cashin.focus();
		if(document.f.pm.value == 'cash')
			showeftposform();
		else if(document.f.pm.value == 'eftpos')
			showcreditcardform();
		else if(document.f.pm.value == 'credit card')
			showcashform();
	}
	function FullyPaid()
	{
		if(CalcPaymentTotal() > 0)
			MsgNotEnough();
		else
			return MsgConfirmCheckout();
		return false;
	}
	function delete_order()
	{
		if(document.URL.indexOf('qpos.hta') > 0)
		{
			var fn = 'c:/qpos/qpos.hta'; 
			var fso = new ActiveXObject('Scripting.FileSystemObject'); 
			var tf = fso.OpenTextFile(fn, 1, false); 
			var s = tf.ReadAll(); 
			tf.Close(); 
			document.close();
			document.write(s);
		}
		else
";
s += "	window.location='qpostest.aspx?t=delete&i=replace_with_invoice_number';\r\n";
s += @"
	}
	function modify_order()
	{
		if(document.URL.indexOf('qpos.hta') > 0)
		{
			var fn = 'c:/qpos/qpos.hta'; 
			var fso = new ActiveXObject('Scripting.FileSystemObject'); 
			var tf = fso.OpenTextFile(fn, 1, false); 
			var s = tf.ReadAll(); 
			tf.Close(); 
			document.close();
			document.write(s);
			restore_last_order();
		}
		else
";
s += "	window.location='qpostest.aspx?t=mo&i=replace_with_invoice_number';\r\n";
s += @"
	}
	function paymentok()
	{
		if(document.f.cashin.value == '.')
		{
			document.f.cashin.value = '';
			document.f.cashin.focus();
			if(!window.confirm('Click Cancle to delete this order, Click OK to return.'))
				delete_order();
			return false;
		}
		if(document.f.cashin.value == '')
		{
			shownextform();
			return false;
		}
		if(document.f.pm.value == 'cash')
		{
			document.f.payment_cash.value = document.f.cashin.value;
			CalcPaymentTotal();
			if(document.f.cashchange.value != '') //ENTER after cashed in
			{
				if(Number(document.f.cashchange.value) >= 0) //ok, we got enough money
				{
					if(Number(document.f.cashin.value) > Number(document.f.total.value))
						document.f.payment_cash.value = document.f.total.value;
					return MsgConfirmCheckout();
				}
				else
				{
					document.f.cashchange.value = '';
					MsgNotEnough();
				}
				return false;
			}
			var cg = Number(document.f.payment_total.value) - Number(document.f.total.value);
			document.f.cashchange.value = cg.toFixed(2);
			return false;
		}
		else if(document.f.pm.value == 'eftpos')
		{
			if(Number(document.f.cashin.value) > Number(document.f.total.value))
				document.f.payment_eftpos.value = document.f.total.value;
			else
				document.f.payment_eftpos.value = document.f.cashin.value;
			return FullyPaid();
		}
		else if(document.f.pm.value == 'credit card')
		{
			if(Number(document.f.cashin.value) > Number(document.f.total.value))
				document.f.payment_cc.value = document.f.total.value;
			else
				document.f.payment_cc.value = document.f.cashin.value;
			return FullyPaid();
		}
		else if(document.f.pm.value == 'bank card')
		{
			if(Number(document.f.cashin.value) > Number(document.f.total.value))
				document.f.payment_bankcard.value = document.f.total.value;
			else
				document.f.payment_bankcard.value = document.f.cashin.value;
			return FullyPaid();
		}
		else if(document.f.pm.value == 'cheque')
		{
			if(Number(document.f.cashin.value) > Number(document.f.total.value))
				document.f.payment_cheque.value = document.f.total.value;
			else
				document.f.payment_cheque.value = document.f.cashin.value;
			return FullyPaid();
		}
		return false;
	}
	function showbankcardform()
	{
		document.f.cashin.focus();
		return;
		document.f.pm.value = 'bank card';
		document.f.pmt.value='BANK CARD';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.value = document.f.payment_bankcard.value;
		document.f.cashin.focus();
		document.f.cashin.select();
	}
	function showeftposform()
	{
		document.f.pm.value = 'eftpos';
		document.f.pmt.value='EFTPOS';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.value = document.f.payment_eftpos.value;
		document.f.cashin.focus();
		document.f.cashin.select();
	}
	function showcreditcardform()
	{
		document.f.pm.value = 'credit card';
		document.f.pmt.value='CREDIT CARD';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.value = document.f.payment_cc.value;
		document.f.cashin.focus();
		document.f.cashin.select();
	}
	function showchequeform()
	{
		document.f.pm.value = 'cheque';
		document.f.pmt.value='CHEQUE';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.value = document.f.payment_cheque.value;
		document.f.cashin.focus();
		document.f.cashin.select();
	}
	function showcashform()
	{
		document.f.pm.value = 'cash';
		document.f.pmt.value='CASH';
		document.f.tcashin.value = 'Cash In';
		document.f.tcashchange.value = 'Change';
		document.f.cashin.value = document.f.payment_cash.value;
		document.f.cashchange.value = '';
		document.f.cashin.focus();
		document.f.cashin.select();
	}
	function check_special_printer_port()
	{
		var sport = '';
		fn = 'c:/qpos/p_port.txt';
		fso = new ActiveXObject('Scripting.FileSystemObject'); 
		if(fso.FileExists(fn))
		{
			tf = fso.OpenTextFile(fn, 1, false); 
			try
			{
				sport = tf.ReadAll();
			}
			catch(err)
			{
			}
			tf.Close(); 
		}
		if(sport != '')
			document.f.printer_port.value = sport;
//		return sport;
	}
	check_special_printer_port();
	";

	s += "</script";
	s += ">";
	return s;
}

string BuildReceiptBody()
{
	if(m_invoiceNumber == null || m_invoiceNumber == "")
		return "";

	int rows = 0;
	string sc = " SELECT i.total, s.code, s.name, s.quantity, s.commit_price * "+ m_gst +" AS price, i.sales ";
	sc += ", c.barcode ";
	sc += " FROM invoice i JOIN sales s ON s.invoice_number=i.invoice_number ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = s.code ";
	sc += " WHERE i.invoice_number = " + m_invoiceNumber;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "receipt");
		if(rows <= 0)
		{
//			Response.Write("<br><br><center><h3>ERROR, Order Not Found</h3>");
			return "Error, Invoice #" + m_invoiceNumber + " Not Found or no item found.";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "SQL Error";
	}

	int n = 0;
	int len = 0;
	m_dInvoiceTotal = Math.Round(MyDoubleParse(dst.Tables["receipt"].Rows[0]["total"].ToString()), 2);

	m_salesName = dst.Tables["receipt"].Rows[0]["sales"].ToString();
	string stotal = m_dInvoiceTotal.ToString("c");
	string s = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["receipt"].Rows[i];
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		if(name.Length > 24)
			name = name.Substring(0, 24);
		name = name.Replace("\"", "");
		name = name.Replace("\r\n", " ");
		name = name.Replace("'", "");
		string qty = dr["quantity"].ToString();
		string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 2).ToString("c");
		string total = Math.Round(MyDoubleParse(dr["price"].ToString()) * MyIntParse(qty), 2).ToString("c");

		s += name + "\\r\\n";

		string spprice = "     " + price;
		s += spprice;
		len = 20 - spprice.Length;
		for(n=0; n<len; n++)
			s += ' ';
		len = qty.Length + 1 + total.Length;
		len = 22 - len;
		s += "x" + qty;
		for(n=0; n<len; n++)
			s += ' ';
		s += total + "\\r\\n";

//		s += "      " + code + "          x" + qty + "      " + total + "\r\n";
	}

	string si = rows + " Items        TOTAL";
	s += si;
	si += stotal;
	len = 42 - si.Length;
	for(n=0; n<len; n++)
		s += ' ';
	s += stotal + "\\r\\n";

	string pm = "";
	if(Request.Form["pm"] != null)
		pm = Request.Form["pm"].ToString().ToUpper(); //payment method
	
	len = 20 - pm.Length;
	for(n=0; n<len; n++)
		s += ' ';
	s += pm;
	if(pm == "CASH")
	{
		string cashin = MyDoubleParse(Request.Form["cashin"].ToString()).ToString("c"); 
		string cashchange = MyDoubleParse(Request.Form["cashchange"].ToString()).ToString("c");
		len = 22 - cashin.Length;
		for(n=0; n<len; n++)
			s += ' ';
		s += cashin + "\\r\\n";
		for(n=0; n<14; n++)
			s += ' ';
		s += "CHANGE";
		len = 22 - cashchange.Length;
		for(n=0; n<len; n++)
			s += ' ';
		s += cashchange + "\\r\\n";
	}
	else
	{
		len = 22 - stotal.Length;
		for(n=0; n<len; n++)
			s += ' ';
		s += stotal + "\\r\\n";
	}

	s += "\\r\\n";
	return s;
}

bool doUpdateInvoicePaymentType(string pm, string invoice_number, string sAmount)
{

	string payment_method = GetEnumID("payment_method", pm);
	if(pm == "amex")
		pm = "american express";
	if(pm == "diners")
		pm = "diners club";
if(payment_method == "")
	payment_method = "1";

	string sc = " UPDATE invoice SET payment_type = "+ payment_method +" ";
//	sc += ", amount_paid = amount_paid + "+ sAmount +", paid = 1 - convert(bit, total - amount_paid - "+ sAmount +") ";
	sc += " WHERE invoice_number = "+ invoice_number +" ";
	
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

bool DoReceiveOnePayment(string pm, double dAmount)
{
	if(dAmount <= 0)
		return true;

	string payment_method = GetEnumID("payment_method", pm);
	string sAmount = dAmount.ToString();

	//do transaction
	SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;

	myCommand.Parameters.Add("@shop_branch", SqlDbType.Int).Value = m_branchID;
	myCommand.Parameters.Add("@Amount", SqlDbType.Money).Value = sAmount;
	myCommand.Parameters.Add("@paid_by", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@bank", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@branch", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@nDest", SqlDbType.Int).Value = "1116";
	myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = 0;
	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = "0"; //cash sales
	myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = payment_method;
	myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = m_invoiceNumber;
	myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@finance", SqlDbType.Money).Value = "0";
	myCommand.Parameters.Add("@credit", SqlDbType.Money).Value = "0";
	myCommand.Parameters.Add("@bRefund", SqlDbType.Bit).Value = 0;
	myCommand.Parameters.Add("@amountList", SqlDbType.VarChar).Value = sAmount;
	myCommand.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
//		ShowExp("DoCustomerPayment", e);
//		return false;
		myConnection.Close();
		return true;
	}
//	string m_tranid = myCommand.Parameters["@return_tran_id"].Value.ToString();
	return true;
}

double MyMoneyParseNoWarning(string s)
{
	Trim(ref s);
	if(s == null || s == "")
		return 0;
	if(s == "NaN")
		return 0;

	double d = 0;
	try
	{
		d = double.Parse(s, NumberStyles.Currency, null);
	}
	catch(Exception e)
	{
//		ShowParseException(s);
	}
	return d;
}

bool DoReceivePayment()
{
	if(Request.Form["invoice_number"] != null)
		m_invoiceNumber = Request.Form["invoice_number"];

	bool bMultiPayments = false;
	m_dCash = MyMoneyParseNoWarning(Request.Form["payment_cash"]);
	double dEftpos = MyMoneyParseNoWarning(Request.Form["payment_eftpos"]);
	double dCreditCard = MyMoneyParseNoWarning(Request.Form["payment_cc"]);
	double dBankcard = MyMoneyParseNoWarning(Request.Form["payment_bankcard"]);
	double dCheque = MyMoneyParseNoWarning(Request.Form["payment_cheque"]);

	if(!DoReceiveOnePayment("cash", m_dCash))
		return false;
	if(!DoReceiveOnePayment("eftpos", dEftpos))
		return false;
	if(!DoReceiveOnePayment("bank card", dBankcard))
		return false;
	if(!DoReceiveOnePayment("cheque", dCheque))
		return false;
	if(!DoReceiveOnePayment("credit card", dCreditCard))
		return false;

	return true;
}

bool PrintReceipt()
{
	if(Request.QueryString["i"] == null || Request.QueryString["i"] == "")
	{
		MsgDie("Error, no invoice number");
		return false;
	}
	m_invoiceNumber = Request.QueryString["i"];
	if(DoPrintReceipt(false, false))
	{
		Response.Write("<script language.javascript>window.close();<");
		Response.Write(">");
	}
	return true;
}

bool DoPrintReceipt(bool bhta, bool bkick)
{
	string sReceiptBody = "";
	if(!bhta)
		sReceiptBody = BuildReceiptBody(); //get m_dInvoiceTotal first before record payment
//DEBUG("sReceiptosdb =", sReceiptBody);
	//print receipt
	byte[] bf = {0x1b, 0x21, 0x20, 0x0};//new char[4];
	byte[] sf = {0x1b, 0x21, 0x02, 0x0};//new char[4];
	byte[] cut = {0x1d, 0x56, 0x01, 0x00};//new char[4];
	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
	byte[] init_printer = {0x1b, 0x40};

	ASCIIEncoding encoding = new ASCIIEncoding( );
    string bigfont = encoding.GetString(bf);	
    string smallfont = encoding.GetString(sf);
    string scut = encoding.GetString(cut);
    string kickout = encoding.GetString(kick);
    string sinit = encoding.GetString(init_printer);
	
	string header = ReadSitePage("pos_receipt_header");
	header = header.Replace("@@branch_address", m_branchAddress);
	header = header.Replace("@@branch_phone", m_branchPhone);
//	DataRow drc = GetCardData(m_sales);
	if(!bhta)
		header = header.Replace("@@sales", m_salesName);

	string footer = ReadSitePage("pos_receipt_footer");
	string sbody = sReceiptBody;
//DEBUG("sbody = ", sbody);
	string sdate = DateTime.Now.ToString("dd/MM/yyyy");
	string stime = DateTime.Now.ToString("HH:mm");

	if(bhta)
		header = header.Replace("\r\n", "\\\\r\\\\n");
	else
		header = header.Replace("\r\n", "\\r\\n");
	header = header.Replace("[/b]", smallfont);
	header = header.Replace("[b]", bigfont);
	header = header.Replace("[cut]", scut.ToString());
	if(!bhta)
	{
		header = header.Replace("@@date", sdate);
		header = header.Replace("@@time", stime);
		header = header.Replace("@@inv_num", m_invoiceNumber);
	}

	if(bhta)
		footer = footer.Replace("\r\n", "\\\\r\\\\n");
	else
		footer = footer.Replace("\r\n", "\\r\\n");
	footer = footer.Replace("[/b]", smallfont);
	footer = footer.Replace("[b]", bigfont);
	footer = footer.Replace("[cut]", scut.ToString());
	if(!bhta)
	{
		footer = footer.Replace("@@date", sdate);
		footer = footer.Replace("@@time", stime);
		footer = footer.Replace("@@inv_num", m_invoiceNumber);
	}

	header = sinit + header;
	m_sReceiptHeader = header;
	m_sReceiptFooter = footer + scut;
	m_sReceiptKickout = kickout;

	string sprint = header + sbody + footer + scut;
	string sprint_nokick = sprint;
//	if(m_dCash > 0)
//		sprint +=  kickout;// + "\\r\\n";
	StringBuilder sb = new StringBuilder();

	//AsPrint ActiveX Control
	sb.Append("<object classid=\"clsid:B816E029-CCCB-11D2-B6ED-444553540000\" ");
	if(bhta)
	{
//		sb.Append(" CODEBASE=\"c:/qpos/asprint.ocx\" ");
		sb.Append(" CODEBASE=\"asprint.ocx\" ");
	}
	else
		sb.Append(" CODEBASE=\"..\\cs\\asprint.ocx\" ");
	sb.Append(" id=\"AsPrint1\">\r\n");
	sb.Append("<param name=\"_Version\" value=\"65536\">\r\n");
	sb.Append("<param name=\"_ExtentX\" value=\"2646\">\r\n");
	sb.Append("<param name=\"_ExtentY\" value=\"1323\">\r\n");
	sb.Append("<param name=\"_StockProps\" value=\"0\">\r\n");
	sb.Append("<param name=\"HideWinErrorMsg\" value=\"1\">\r\n");
	sb.Append("</object>\r\n");
	
	m_sReceiptPrinterObject = sb.ToString();
	m_sReceiptPrinterObject = m_sReceiptPrinterObject.Replace("\"", "\\\"");
	m_sReceiptPrinterObject = m_sReceiptPrinterObject.Replace("\r\n", "");
	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");
	if(bhta)
		return true;

	string s = "";
	s = "\r\n<script language=javascript>\r\n";
	s += " var printer_port = '" + m_sReceiptPort + "';\r\n";
	s += @"
	var sport = '';
	fn = 'c:/qpos/p_port.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
	{
		tf = fso.OpenTextFile(fn, 1, false); 
		try
		{
			sport = tf.ReadAll();
		}
		catch(err)
		{
		}
		tf.Close(); 
	}
	if(sport != '')
		printer_port = sport;
	fn = 'c:/qpos/receipt.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
		fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -2);
	";
	s += "tf.Write('" + sprint_nokick + "'); \r\n";
	s += "tf.Close();\r\n";
	s += "	document.AsPrint1.Open(printer_port);\r\n";
//	if(bkick)
//		s += "	document.AsPrint1.PrintString('" + kickout + "');\r\n";
//	else
		s += "	document.AsPrint1.PrintString('" + sprint + "');\r\n";
	s += "	document.AsPrint1.PrintString('" + kickout + "');\r\n";
	s += "	document.AsPrint1.Close();\r\n";

	s += "</script";
	s += ">";
//for(int i=0; i<m_nNoOfReceiptPrintOut; i++)
{
	sb.Append(s);
}

//	Response.Write(sprint); //test
	if(Request.QueryString["t"] != "pr")
	{
		sb.Append("<meta http-equiv=\"refresh\" content=\"0; URL=qpostest.aspx?t=new\">");
//		sb.Append("<script>window.close();</script");
//		sb.Append(">");
	}
	else
	{
//		Response.Write(sprint); //test
//		return false;
	}
	Response.Write(sb.ToString());
	return true;
}

string GetBranchName(string id)
{
	if(dst.Tables["branch_name"] != null)
		dst.Tables["branch_name"].Clear();

	string sc = " SELECT * FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "branch_name") == 1)
		{
			DataRow dr = dst.Tables["branch_name"].Rows[0];
			m_branchAddress = dr["address1"].ToString();
			m_branchPhone = dr["phone"].ToString();
			return dr["name"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}

bool BuildAgentCache()
{
	string sc = " SELECT id, name, barcode FROM card WHERE type = 2 AND barcode <> '' ORDER BY barcode, id, name";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "agent");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb = new StringBuilder();
/*	sb.Append("<select name=tagent><option name=0></option>");

	for(int i=0; i<dst.Tables["agent"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["agent"].Rows[i];
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
//		if(trading_name.Length > 10)
//			trading_name = trading_name.Substring(0, 10);
		
		sb.Append("<option value=" + id + ">" + name + " - " + trading_name + "</option>");
	}
	sb.Append("</select>");
*/
	sb.Append("<script language=javascript>function CheckAgentCode(){ \r\n");
	sb.Append(" var a = new Array(); \r\n");
	
	int n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<dst.Tables["agent"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["agent"].Rows[i];
		string id = dr["id"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		if(!TSIsDigit(barcode))
			continue;
		name = name.Replace("\"", "`");
//		string srow = "a[" + id + "]=\"" + name + "\";";
		string srow = "a[" + barcode + "]=\"" + name + "\";";
		sb.Append(srow);
		n += srow.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}

	sb.Append("\r\n");
	sb.Append(" var id = Number(document.f.agent_code.value); \r\n");
//	sb.Append(" if(id == 0){document.f.button_ok.focus();return;} \r\n");
	sb.Append(" if(a[id] != null){document.f.agent.value = id; document.f.agent_name.value = a[id];} \r\n");
	sb.Append("} </script");
	sb.Append(">");

	m_sAgentCache = sb.ToString();
	return true;
}

string PrintSalesOptions()
{
	string sc = " SELECT id, trading_name, name FROM card WHERE type = 4 ";
	if(Session["branch_support"] != null)
		sc += " AND our_branch = " + m_branchID;
	sc += " ORDER BY name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "sales");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Response.End();
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<select name=sales><option name=0>Please Select</option>");

	for(int i=0; i<dst.Tables["sales"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["sales"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		
		sb.Append("<option value=" + id + ">" + name + "</option>");
	}
	sb.Append("</select>");
	return sb.ToString();
}

bool CheckOfflineHTAFile()
{
	string s = "<script language=javascript>\r\n";
	s += @"
try 
{
	var fn = 'c:/qpos/qpos.hta';
	var fso = new ActiveXObject('Scripting.FileSystemObject');
	if(!fso.FileExists(fn))
		window.location='qpostest.aspx?updatecache=1';
}
catch(err)
{
	var strErr = 'checkofflinehtafile Error:';
	strErr += '\nNumber:' + err.number;
	strErr += '\nDescription:' + err.description;
//	window.alert(strErr);
	window.location = 'qpostest.aspx?nts=1';
}
</script";
	s += ">";
	Response.Write(s);
	return true;
}

//offline HTA file functions
bool UpdateOfflineHTAFile()
{
	string st = m_shta.Replace("'", "\\'");
	st = st.Replace("\r\n", "\\r\\n");
	st = st.Replace("script", "@@script@@");
	st = st.Replace("function", "@@function@@");
	string s = "\r\n\r\n\r\n<script language=javascript>\r\n";
	s += "var shta = '" + st + "';";
	s += @"
		re = /@@script@@/g;
		shta = shta.replace(re, 'script');
		re = /@@function@@/g;
		shta = shta.replace(re, 'function');
		try 
		{
			var fso, tf;
			var pn = 'c:/qpos';
			var fn = 'c:/qpos/qpos.hta';
			fso = new ActiveXObject('Scripting.FileSystemObject');
			if(!fso.FolderExists(pn))
				fso.CreateFolder(pn);
			if(fso.FileExists(fn))
				fso.DeleteFile(fn);
			tf = fso.OpenTextFile(fn , 8, 1, -2);
		";
	s += "		tf.Write(shta)";
	s += @"
			tf.Close();
		}
		catch(err)
		{
			var strErr = 'write qpos.hta Error:';
			strErr += '\nNumber:' + err.number;
			strErr += '\nDescription:' + err.description;
			window.alert(strErr);
		}
		</script
		";
	s += ">";

	Response.Write(s);

	//payment form for offline
	PrintPaymentForm(true); //true: fill m_sPaymentForm for offline use
	st = m_sPaymentForm.Replace("'", "\\'");
	st = st.Replace("\r\n", "\\r\\n");
	st = st.Replace("script", "@@script@@");
	s = "\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n<script language=javascript>\r\n";
	s += "shta = '" + st + "';";
	s += @"
	re = /@@script@@/g;
	shta = shta.replace(re, 'script');
	try 
	{
		var fso, tf;
		var fn = 'c:/qpos/qpospay.hta';
		fso = new ActiveXObject('Scripting.FileSystemObject');
		if(fso.FileExists(fn))
			fso.DeleteFile(fn);
		tf = fso.OpenTextFile(fn , 8, 1, -2);
	";
s += "		tf.Write(shta)";
s += @"
		tf.Close();
	}
	catch(err)
	{
		var strErr = 'write qpospay Error:';
		strErr += '\r\nNumber:' + err.number;
		strErr += '\r\nDescription:' + err.description;
		window.alert(strErr);
	}
	</script";
s += ">";
	Response.Write(s);

	//payment form for online
	PrintPaymentForm(false); //false: fill m_sPaymentFormOnlineCache for online use
	st = m_sPaymentFormOnlineCache.Replace("'", "\\'");
	st = st.Replace("\r\n", "\\r\\n");
	st = st.Replace("script", "@@script@@");
	s = "\r\n\r\n\r\n\r\n\r\n\r\n\r\n\r\n<script language=javascript>\r\n";
	s += "shta = '" + st + "';";
	s += @"
	re = /@@script@@/g;
	shta = shta.replace(re, 'script');
	try 
	{
		var fso, tf;
		var fn = 'c:/qpos/qpospayo.hta';
		fso = new ActiveXObject('Scripting.FileSystemObject');
		if(fso.FileExists(fn))
			fso.DeleteFile(fn);
		tf = fso.OpenTextFile(fn , 8, 1, -2);
	";
s += "		tf.Write(shta)";
s += @"
		tf.Close();
	}
	catch(err)
	{
		var strErr = 'write qpospay Error:';
		strErr += '\r\nNumber:' + err.number;
		strErr += '\r\nDescription:' + err.description;
		window.alert(strErr);
	}

//	document.write(strResult);

//var file = window.open('../cs/asprint.ocx','file');
//file.document.execCommand('SaveAs',false,'c:\\qpos\\asprint.ocx');
//file.window.close();

</script";
s += ">";
	Response.Write(s);

	return true;
}

bool DoCreateShortcut()
{
//	WshShell shell = new WshShell();
//	IWshShortcut link = (IWshShortcut)shell.CreateShortcut("POS - offline");
//	link.TargetPath = "c:\\qpos.hta";
//	link.Save();

	string s = @"
<script language=javascript>
var strResult;
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/eznz.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/eznz.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write eznz.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}

try
{
	var strURL = '";
s += m_sServer;
s += @"/i/asprint.ocx';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/asprint.ocx';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write asprint.ocx Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}

try
{
	var strURL = '";
s += m_sServer;
s += @"/i/eznzicon.exe';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/eznzicon.exe';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write eznzicon.exe Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}

Shell = new ActiveXObject('WScript.Shell');
DesktopPath = Shell.SpecialFolders('Desktop');
link = Shell.CreateShortcut(DesktopPath + '\\P.O.S..lnk');
link.Arguments = '';
link.Description = 'POS Offline';
link.HotKey = 'CTRL+ALT+SHIFT+P';
link.IconLocation = 'c:\\qpos\\eznzicon.exe,0';
link.TargetPath = 'c:\\qpos\\qpos.hta';
link.WindowStyle = 3;
link.WorkingDirectory = 'c:\\qpos';
link.Save();
</script";
	s += ">";
	Response.Write(s);
	return true;
}

bool CheckOfflineInvoices()
{
	string s = @"
<script language=javascript>
var pn = 'c:/qpos';
var fn = 'c:/qpos/qposinv.csv'; 
fso = new ActiveXObject('Scripting.FileSystemObject'); 
if(!fso.FolderExists(pn))
	fso.CreateFolder(pn);
if(fso.FileExists(fn))
	window.location='qpostest.aspx?oi=1&t=load';
else
	window.location='qpostest.aspx?t=new';
</script";
	s += ">";
	Response.Write(s);
	return true;
}

bool LoadOfflineInvoices()
{
	string s = @"
<script language=javascript>
var fn = 'c:/qpos/qposinv.csv'; 
fso = new ActiveXObject('Scripting.FileSystemObject'); 
if(fso.FileExists(fn))
{
	var tf = fso.OpenTextFile(fn, 1, false); 
	var s = tf.ReadAll(); 
	re = /\'/g;
	s = s.replace(re, '\\\'');
	tf.Close(); 
	document.write('<center><br><br><h4>You have unprocessed offline invoices, do you want to upload them now?</h4>');
	document.write('<form name=f action=qpostest.aspx?oi=1&t=process method=post>');";
	s += "document.write('<input type=hidden name=inv value=\"' + s + '\">');";
	s += "document.write('<input type=submit name=cmd value=\"Upload Now\" class=b>');";
	s += "document.write('<input type=button value=\"Later\" class=b onclick=window.location=\"qpostest.aspx?oi=1&t=new\">');";
	s += @"
	document.write('</form>');
	document.f.cmd.focus();
}
</script";
	s += ">";
	Response.Write(s);
	return true;
}

//	return CreateOrder(m_branchID, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
//		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), ref m_orderID);
bool ProcessOfflienInvoices()
{
	CheckShoppingCart();
	string s = Request.Form["inv"];
	string line = "";
	string ref_num = "";
	string inv_total = "";
	string sdate = "";
	string sCustPONumber = "";
	string contact = "";
	double dCash = 0;
	double dEftpos = 0;
	double dCreditCard = 0;
	double dBankcard = 0;
	double dCheque = 0;
	Response.Write("<h4>Processing, please wait...</h4>");
	Response.Flush();
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\r') //one line fully read
		{
//DEBUG("line=", line);
			char[] cb = line.ToCharArray();
			int pos = 0;
			string tag = CSVNextColumn(cb, ref pos);
			Trim(ref tag);
//DEBUG("tag=", tag);
			if(tag == "invoice begin")
			{
//DEBUG("begin", "");
				ref_num = CSVNextColumn(cb, ref pos);
				sdate = CSVNextColumn(cb, ref pos);
				m_branchID = CSVNextColumn(cb, ref pos);
				m_sales = CSVNextColumn(cb, ref pos);
				m_sAgent = CSVNextColumn(cb, ref pos);
				if(m_sales == "undefined")
					m_sales = "0"; //use cash sales
				inv_total = CSVNextColumn(cb, ref pos);
				m_salesNote = "offline invoice ref #" + ref_num + " date:" + sdate;
				dCash = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dEftpos = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dCreditCard = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dBankcard = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dCheque = MyMoneyParse(CSVNextColumn(cb, ref pos));
				EmptyCart();
			}
			else if(tag == "invoice end")
			{
//DEBUG("end", "");
				Response.Write("reference #" + ref_num + ", ");
				bool bRet = CreateOrder(m_branchID, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
					m_pickupTime, contact, m_sales,  EncodeQuote(m_salesNote), ref m_orderID);
				if(!bRet)
				{
					Response.Write("<h4>Error Create Order</h4>");
					return false;
				}
				Response.Write("order created #" + m_orderID + ", ");
				if(!CreateInvoice(m_orderID))
				{
					Response.Write("<h4>Error Create Invoice</h4>");
					return false;
				}
				Response.Write("invoice created #" + m_invoiceNumber + ", ");
				if(!DoReceiveOnePayment("cash", dCash))
					return false;
				if(!DoReceiveOnePayment("eftpos", dEftpos))
					return false;
				if(!DoReceiveOnePayment("bank card", dBankcard))
					return false;
				if(!DoReceiveOnePayment("cheque", dCheque))
					return false;
				if(!DoReceiveOnePayment("credit card", dCreditCard))
					return false;
				Response.Write("payment recorded<br>");
			}
			else //items, build cart
			{
				string code = tag;
				double dPrice = MyMoneyParse(CSVNextColumn(cb, ref pos));
				int qty = MyIntParse(CSVNextColumn(cb, ref pos));
				dPrice /= m_gst;
				dPrice = Math.Round(dPrice, 4);
//DEBUG("code cart = ", code);
//DEBUG("qty cart= ", qty);
//DEBUG("price cart = ", dPrice.ToString());
				AddToCart(code, qty.ToString(), dPrice.ToString());
			}
			line = "";
		}
		else if(s[i] == '\n')
		{
		}
		else
			line += s[i];
	}

	s = @"
<script language=javascript>
var fn = 'c:/qpos/qposinv.csv'; 
fso = new ActiveXObject('Scripting.FileSystemObject'); 
if(fso.FileExists(fn))
	fso.DeleteFile(fn);
</script";
	s += ">";
	Response.Write(s);
	Response.Write("<h4>done! please wait a second...</h4>");
	Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=qpostest.aspx?t=new\">");
	return true;
}

string CSVNextColumn(char[] cb, ref int pos)
{
	if(pos >= cb.Length)
		return "";

	char[] cbr = new char[cb.Length];
	int i = 0;

	if(cb[pos] == '\"')
	{
		while(true)
		{
			pos++;
			if(pos == cb.Length)
				break;
			if(cb[pos] == '\"')
			{
				pos++;
				if(pos >= cb.Length)
					break;
				if(cb[pos] == '\"')
				{
					cbr[i++] = '\"';
					continue;
				}
				else if(cb[pos] != ',')
				{
					Response.Write("<br><font color=red>Error</font>. CSV file corrupt, comma not followed quote. Line=");
					Response.Write(new string(cb));
					Response.Write("<br>\r\n");
					break;
				}
				else
				{
					pos++;
					break;
				}
			}
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
		}
	}
	else
	{
		while(cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
			pos++;
			if(pos == cb.Length)
				break;
		}
		pos++;
	}
	return new string(cbr, 0, i);
}

bool DoDeleteInvoice(string inv)
{
	string sc = " DELETE FROM invoice WHERE invoice_number = " + inv;
	sc += " DELETE FROM sales WHERE invoice_number = " + inv;
	sc += " DELETE FROM sales_kit WHERE invoice_number = " + inv;
	sc += " DELETE FROM orders WHERE invoice_number = " + inv;
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

bool PrintTrustedMassInterface()
{
	string s = @"
<script language=javascript>
	var fn = 'c:/qpos/qpos.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false); 
	var sh = tf.ReadAll(); 
	tf.Close(); 
	document.close();
	sh = sh.replace('else{LoadPaymentForm();return false;}', 'else{remember_last_order();}');
	";
s += "	sh = sh.replace('reload_offline_app()', 'window.location=(\"?t=new\")')\r\n";
s += @"
	document.write(sh);
</script";
	s += ">";
//Response.Write("trsuted interface test<br>");
	Response.Write(s);
	Response.Write("<a href=qpostest.aspx?t=chl class=o>Download Offline Tools(Create shortcut on desktop)</a> &nbsp; ");
	Response.Write("<a href=qpostest.aspx?t=fu&r=" + DateTime.Now.ToOADate() + " class=o>Update Cache</a> &nbsp; ");
	return true;
}

bool PrintTrustedPaymentForm()
{
	m_invoiceNumber = Request.QueryString["i"];
	m_dInvoiceTotal = 0;
	if(Request.QueryString["total"] != null)
		m_dInvoiceTotal = MyDoubleParse(Request.QueryString["total"].ToString());

	string s = @"
<script language=javascript>
	var fn = 'c:/qpos/qpospayo.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false); 
	var sh = tf.ReadAll(); 
	tf.Close(); 
	document.close();
";
	s += " 	re = /replace_with_invoice_number/g; ";
	s += "	sh = sh.replace(re, '" + m_invoiceNumber + "');";
	s += "	document.write(sh);";
	s += "document.f.total1.value = '" + m_dInvoiceTotal.ToString("c") + "';\r\n";
	s += "document.f.total.value = '" + m_dInvoiceTotal + "';\r\n";
	s += "</script";
	s += ">";
//Response.Write("trsuted interface test<br>");
	Response.Write(s);
	return true;
}

bool SendToDarcy(string s)
{
	MailMessage msgMail = new MailMessage();
	msgMail.From = "darcy@eznz.com";
	msgMail.To = "darcy@eznz.com";
	msgMail.Subject = "debug info";
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = s;
	SmtpMail.Send(msgMail);
	return true;
}
</script>

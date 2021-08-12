<!-- #include file="purchase_function.cs" -->
<!-- #include file="fifo_f.cs" -->

<script runat="server">

string tableTitle = "";
string m_url = "custgst.aspx?inv="; //this url

string m_orderID = ""; //map to invoice.invoice_number, ship to customer if this number valid

string m_supplierID = "0";
string m_supplierName = "";
string m_supplierShortName = "";
string m_supplierEmail = "";
string m_poNumber = "";
string m_poID = "";				//id for purchase and puchase_item table
string m_poIDNew = "";			//for backorders split
string m_supInvNumber = "";
string m_quoteType = "2";
string m_paymentType = "";
string m_sales = "";
string m_billToID = "";
string m_note = "";
string m_sdate = "";
string m_status = "1";
string m_branchID = "1"; //Request.Form["branch"];
string m_staffID = "0";

string m_shipto = ""; //optional ship to address (for online orders)
string m_sentNotice = ""; //email has been sent to supplier

string m_currency = "1";
string m_currencyName = "NZD";
string m_exrate = "1";
string m_gstrate = "0";

string m_pid = ""; //vin code
string m_getid = ""; //vin code


bool m_bPaid = false;
//bool bIncludeGST = false;
bool m_bNoIndividualPrice = false;
bool m_bOrderCreated = false;
bool m_bsnEntered = false;
bool m_bAlreadySent = false;
bool m_bStockUpdated = false;
bool m_bexistingProd = false;
bool m_bPrintView = false; //if true don't store things to session object

double m_dTotal = 0;
double m_dFreight = 0;

int m_nSearchReturn = 0;
int m_nCols = 6;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	
	//session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
//DEBUG("ssid=", m_ssid);
/*		if(Session["sales_dealer_level_for_pos" + m_ssid] == null) //garbage ssid
		{
			m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
			PrepareNewSales();
			string par = "?ssid=" + m_ssid;
			if(Request.QueryString.Count > 0)
				par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
			Response.Redirect("pos.aspx" + par);
			return;
		}
*/
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		PrepareNewPurchase();
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("custgst.aspx" + par);
		return;
	}
/*
	if(Request.QueryString["t"] == "end")
	{
		PrepareNewPurchase();
		Response.Redirect("plist.aspx");
		return;
	}
*/
	CheckShoppingCart();
	CheckUserTable();	//get user details if logged on

	if(Request.Form["cmd"] == "New Order" || Request.QueryString["t"] == "new" || Request.QueryString["m"] != "")
	{
		m_pid = Request.QueryString["m"];
		if(!PrepareNewPurchase())
		{
			Response.Write("<br><br><center><h3>Error prepare New Purchase</h3>");
			return;
		}
		m_currency = "1";
		m_exrate = "1.00";
//		m_gstrate = "0.125";
		m_gstrate = (MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100).ToString();		//Modified by NEO
	}
	else
	{
		if(Request.Form["branch"] != null)
			UpdateAllFields(); //store in session object

	}
	//restore values stored in session object
	RestoreAllFields();
//DEBUG("m_poID=", m_poID);	
	m_currencyName = GetEnumValue("currency", m_currency).ToUpper();

	//come from makepurchase
	if(Request.QueryString["oid"] != null && Request.QueryString["oid"] != "")
	{
		m_url += Request.QueryString["oid"];
		m_orderID = Request.QueryString["oid"];
		if(!GetOrderInfo())
			return;
	}
	m_url += "&ssid=" + m_ssid;

	if(Request.Form["cmd"] == "Cancel") //cancel search
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custgst.aspx?ssid=" + m_ssid + "\">");
		return;
	}

	m_bOrder = true;
	Session[m_sCompanyName + "_ordering"] = true;
	Session[m_sCompanyName + "_salestype"] = "purchase";

	//sales name
	m_sales = Session["name"].ToString(); //default

	m_sdate = DateTime.Now.ToString("dd/MM/yyyy");

	bool bJustRestored = false;
	if(Request.Form["po_number"] != null && Request.Form["po_number"] != "")
		m_poNumber = Request.Form["po_number"];

	//do update order before get old order
	if(Request.Form["cmd"] == "Update Order")
	{
		Session["purchase_need_update" + m_ssid] = null;
		if(DoUpdateQuote())
		{
			PrepareNewPurchase();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custgst.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
		}
		return;
	}
	if(Request.Form["cmd"] == "Change To Bill")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(DoChangeToBill())
		{
			PrepareNewPurchase();
			Response.Write("<br><br><center><h3>Done, changed to bill</h3>");
			Response.Write("<input type=button onclick=window.location=('default.aspx') value='Main Page' " + Session["button_style"] + ">");
		}
		return;
	}
	
	if(Request.QueryString["t"] == "pp") //print privew
	{
		m_bPrintView = true;
	}

	//generate or do insert code by vin
	if(Request.QueryString["g"] == "pp") 
	{
		m_bPrintView = false;
		m_pid = Request.QueryString["n"];
		GenerateGST(m_pid,"");
	}

	//get old order
	if(Request.QueryString["n"] != null && Request.QueryString["n"] != "")
	{
		m_poID = Request.QueryString["n"];
		if(!IsInteger(m_poID))
		{
			Response.Write("<br><br><center><h3>ERROR, WRONG ID</h3>");
			return;
		}
		if(!m_bPrintView)
		{
/*			if(Session["purchase_current_po_id"] != null)
			{
				PrintAdminHeader();
				PrintAdminMenu();
				Response.Write("<br><center><h3>Sorry you can't open two purchase orders use one session<br>");
				Response.Write("Please use 'End Purchase Session' button to end another purchase order editing<br>");
				Response.Write("<br>Start a new Explorer(not CTRL-N), relogin to launch the second order<br>");
				Response.Write("if you have to edit two purchase orders at the same time.<br>");
				Response.Write("<br>Click this button if you are sure there's no other purchase order is open<br>");
				Response.Write("<br><input type=button value='End Purchase Session' onclick=window.location=('purchase.aspx?t=end') ");
				Response.Write(" style=\"font-size:8pt;font-weight:bold;background-color:red;color:yellow;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\">");
				return;
			}
			else
				Session["purchase_current_po_id"] = m_poID;
*/
			Session["purchase_current_po_id" + m_ssid] = m_poID;
		}
		if(Request.Form["branch"] == null) //not post back
		{
			if(!RestorePurchase()) //false means not system quotation
				return;
		}
	}
	//order number
//	if(Session["purchase_current_po_id"] != null)
//		m_poID = Session["purchase_current_po_id"].ToString();
	if(m_poID != "")
		m_bOrderCreated = true;

//	m_url += "&n=" + m_poID; //get po id for next use

	if(Request.QueryString["a"] == "add")
	{
		string code = Request.QueryString["code"];
		string supplier = Request.QueryString["supplier"];
		string supplier_code = Request.QueryString["supplier_code"];
		string foreign_supplier_price = Request.QueryString["fsp"];
		if(!SameSupplier(supplier))
			return;
		AddToCart(code, supplier, supplier_code, "1", foreign_supplier_price);
		Session["purchase_need_update" + m_ssid] = true; //for update order first
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custgst.aspx?ssid=" + m_ssid + "\">");
		return;
	}
	
	if(m_bPrintView) //print privew
	{
		if(!RestoreCustomer())
			return;
		Response.Write(BuildOrderInvoice(m_quoteType));//no email msg, inocie only, no system
		return;
	}

	//item search
	if((Request.Form["our_code_search"] != null && Request.Form["our_code_search"] != "") 
		|| (Request.Form["supplier_code_search"] != null && Request.Form["supplier_code_search"] != ""))
	{
		if(!DoSearchItem())
			return;
		if(m_nSearchReturn <= 0)
		{
			Response.Write("<h3>No code matches <b>" + Request.Form["supplier_code_search"] + "</b></h3>");
			LFooter.Text = m_sAdminFooter;
			return;
		}
		else if(m_nSearchReturn > 1)
		{
//			PrintSearchForm();
			LFooter.Text = m_sAdminFooter;
			return; //print search result then return);
		}
	}

	if(Request.QueryString["search"] == "1")//	if(Request.Form["cmd"] == "Search")
	{
//		if(Request.QueryString["st"] == "ship")
//		{
//			Response.Write("<br><center><h3>Search For Bill to</h3></center>");
//			DoBillToSearch();
//		}
//		else
		{
			Response.Write("<br><center><h3>Search For Supplier</h3></center>");
			DoSupplierSearch();
		}
		return;
	}
	else if(Request.Form["cmd"] == "Select From Categories")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx?ssid=" + m_ssid + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Undo Delete")
	{
		if(Request.Form["chkundodelete"] == "on")
		{
			if(!DoDeleteOrder(true)) //bUndoDelete = true
				return;
			PrepareNewPurchase();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custgst.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
		}
		else
		{
			PrintAdminHeader();
			PrintAdminMenu();
			PrepareNewPurchase();
			Response.Write("<br><center><h3>Please tick 'Undo delete'</h3>");
			Response.Write("<input type=button onclick=history.go(-1) value=Back " + Session["button_style"] + ">");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Create Order")
	{
		GenerateGST(m_pid,"insert");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=plist.aspx\">");
		return;
	//	if(!DoCreatePurchase()) // "" means new quote
	//	{
	//		Response.Write("<h3>ERROR CREATING ORDER</h3>");
	//	}
	//	else
	//	{
	//		Session["purchase_need_update" + m_ssid] = null;
	//		PrepareNewPurchase(); //end this session
	//		PrintAdminHeader();
	//		PrintAdminMenu();
	//		Response.Write("<br><br><center><h3>Purchase Order Created</h3>");
	//		Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=custgst.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
	//	}
	//	return;
	}
	else if(Request.Form["cmd"] == "Send")
	{
		if(Request.Form["send_confirm"] != "on")
		{
			Response.Write("<br><br><center><h3>Error, please tick \"Email Order\" to confirm sending.</h3>");
		}
		else
		{
			UpdateAllFields();
			DoUpdateQuote();

			if(RestoreCustomer())
			{
				if(SendMail())
				{
					PrepareNewPurchase();
					PrintAdminHeader();
					PrintAdminMenu();
					Response.Write("<br><br><center><h3>Email Sent.</h3>");
					Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=custgst.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
				}
			}
		}
		return;
	}
//	else if(Request.Form["cmd"] == "Update Price")
//	{
//		if(!bJustRestored)
//		{
//			if(!UpdateSalesPrice())
//				return;
//		}
//	}
	else if(Request.Form["cmd"] == "Delete") 
	{
		if(Request.Form["chkdelete"] == "on")
		{
			if(!DoDeleteOrder(false))
				return;
			PrepareNewPurchase();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=plist.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		else
		{
			Response.Write("<h3>ERROR DELETING QUOTE/ORDER</h3>");
		}
		return;
	}
	else if(Request.Form["cmd"] == "Receive")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(DoReceive())
		{
			PrepareNewPurchase();
			Response.Write("<br><br><center><h3>Done, stock updated</h3>");
			Response.Write("<input type=button value='Enter Serial Number' " + Session["button_style"]);
			Response.Write(" onclick=window.location=('serial.aspx?n=" + m_poID + "')>");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
//	PrintHeaderAndMenu();
	MyDrawTable();

//	PrintSearchForm();
	LFooter.Text = m_sAdminFooter;
}

bool DoDeleteOrder(bool bUndoDelete)
{
	if(!CanUpdate())
		return false;

	string sc = "UPDATE purchase SET ";
	if(bUndoDelete) //undo delete , restore status to status_old
		sc += " status = status_old ";
	else // delete job, store current status to status_old
		sc += " status_old = status, status = 4 "; //status 4 : purchase_order_status deleted
	sc +=  " WHERE id = " + m_poID;  //set quote or order to be deleted

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

bool DoSearchItem()
{
	string kw=Request.Form["supplier_code_search"];
	bool bOurCode = false;
	if(Request.Form["our_code_search"] != null && Request.Form["our_code_search"] != "")
	{
		bOurCode = true;
		kw = Request.Form["our_code_search"];
	}

	string sc = "";
	sc = "SELECT TOP 100 code, supplier, supplier_code, name, currency, foreign_supplier_price, ISNULL(supplier_price, 0) AS supplier_price ";
	sc += " FROM code_relations WHERE ";
	if(bOurCode)
		sc += " code LIKE '" + kw + "%' ORDER BY code";
	else
		sc += " supplier_code LIKE '" + kw + "%' ORDER BY supplier_code";

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
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["foreign_supplier_price"].ToString();
		string currency = dr["currency"].ToString();
		if(!SameSupplier(supplier))
			return false;
//		if(currency != "")
//			supplier_price = dr["foreign_supplier_price"].ToString();
//		if(currency != "")
//		{
//			Session["purchase_currency"] = currency;
//			Session["purchase_exrate"] = GetSiteSettings("exchange_rate_" + GetEnumValue("currency", currency));
//			Session["purchase_gstrate"] = GetSiteSettings("gst_rate_" + GetEnumValue("currency", currency), "0");
//			supplier_price = dr["foreign_supplier_price"].ToString();
//		}
		AddToCart(code, supplier, supplier_code, "1", supplier_price, "", name, "");
		Session["purchase_need_update" + m_ssid] = true; //force update order first
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custgst.aspx?ssid=" + m_ssid + "\">");
		return false;
	}
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
//		PrintHeaderAndMenu();
		Response.Write("<center><h3>Search Result For " + kw + "</h3></center>");
		if(m_nSearchReturn == 100)
			Response.Write("top 100 rows returned, display 1-100");
		BindISTable();
//		DataView source = new DataView(dst.Tables["isearch"]);
//		MyDataGridIS.DataSource = source ;
//		MyDataGridIS.DataBind();
//DEBUG("rows=", m_nSearchReturn);
	}
	return true;
}

void BindISTable()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>Code</td>\r\n");
	Response.Write("<td>Supplier</td>\r\n");
	Response.Write("<td width=130>M_PN</td>\r\n");
	Response.Write("<td>Description</td>\r\n");
	Response.Write("<td>SupplierPrice</td>\r\n");
	Response.Write("</tr>\r\n");

	for(int i=0; i<m_nSearchReturn; i++)
	{
		DataRow dr = dst.Tables["isearch"].Rows[i];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string price = dr["supplier_price"].ToString();
		price = "NZ" + MyMoneyParse(price).ToString("c");
		string currency = dr["currency"].ToString();
		string url = "custgst.aspx?ssid=" + m_ssid + "&a=add&code=" + code;
		url += "&supplier=" + HttpUtility.UrlEncode(supplier) + "&supplier_code=" + HttpUtility.UrlEncode(supplier_code);
		if(currency != "")
		{
			url += "&currency=" + currency + "&fsp=" + dr["foreign_supplier_price"].ToString();
			currency = GetEnumValue("currency", currency).ToUpper();
			price = currency + MyMoneyParse(dr["foreign_supplier_price"].ToString()).ToString("c");
		}
		
		Response.Write("<tr>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td><a href=" + url + " class=o>" + supplier_code + "</a></td>");
		Response.Write("<td><a href=" + url + " class=o>" + name + "</a></td>");
		Response.Write("<td>" + price + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

bool SendMail()
{
	if(!CanUpdate())
		return false;

	string smail = BuildOrderInvoice(m_quoteType);
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = Request.Form["email"];
	msgMail.Bcc = GetSiteSettings("manager_email", "alert@eznz.com");
	msgMail.From = Session["email"].ToString();
	msgMail.Subject = "Purchase Order #" + m_poNumber;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = smail;

	SmtpMail.Send(msgMail);

	msgMail.To = Session["email"].ToString(); //backup copy
	SmtpMail.Send(msgMail);

	string sc = "UPDATE purchase SET already_sent=1, sent_to='" + Request.Form["email"];
	sc += "', who_sent='" + Session["name"].ToString() + "', time_sent=GETDATE() ";
	sc += " WHERE id=" + m_poID;
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

bool DoChangeToBill()
{
	if(!CanUpdate())
		return false;

	string sup_inv_number = Request.Form["sup_inv_number"];
	//DEBUG(" sup_inv_number = ", sup_inv_number);
	if(sup_inv_number == "")
	{
		Response.Write("<script Language=javascript");
		Response.Write(">\r\n");
		Response.Write("window.alert('Please enter supplier Inv No.')\r\n");
		Response.Write("</script");
		Response.Write(">\r\n ");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custgst.aspx?n=" + m_poID + "&r=" + DateTime.Now.ToOADate() + "\">");
		return false;
	}

	string inv_date = Request.Form["inv_date"];

	string sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE purchase SET type=" + GetEnumID("receipt_type", "bill");
	sc += ", inv_number='" + EncodeQuote(sup_inv_number) + "' ";
	sc += ", date_invoiced = '" + inv_date + "', tax_date = '" + inv_date + "' ";
	sc += " WHERE id=" + m_poID;

	sc += " UPDATE card SET balance = balance + " + m_dTotal; //supplier balance
	sc += " WHERE id=" + m_supplierID;
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
		if(e.ToString().IndexOf("out-of-range datetime value") >= 0)
			Response.Write("<br><br><center><h3>Error, Supplier Inv Date <font color=red>" + inv_date + "</font> is invalid, please use DD/MM/YYYY format</h3>");
		else
			ShowExp(sc, e);
		return false;
	}

	DoUpdateQuote(false);

	if(!UpdateBottomPrice())
		return false;

	if(!fifo_purchase_update_cost(m_poID)) //check if any sales cost record need to update, (item sold before this purchase billed)
		return false;

	return true;
}

bool UpdateBottomPrice()
{
	string sc = "SELECT i.kid, i.code, i.qty, i.price, c.id, p.currency, p.exchange_rate ";
	sc += " FROM purchase_item i JOIN purchase p ON p.id=i.id JOIN code_relations c ON c.code=i.code ";
	sc += " WHERE i.id = " + m_poID + " ORDER BY i.kid ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "updatebottomprice") == 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i = 0; i < dst.Tables["updatebottomprice"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["updatebottomprice"].Rows[i];
		string kid = dr["kid"].ToString();
		bool bUpdatePrice = (Request.Form["update_cost" + i.ToString()] == "on");
		bool bExistsInProduct = ExistingProd(dst.Tables["updatebottomprice"].Rows[i]["code"].ToString());
		if(!UpdateAverageCost(dst.Tables["updatebottomprice"].Rows[i], bExistsInProduct, bUpdatePrice))
			return false;
	}
	return true;
}

bool DoUpdateQuote()
{
	return DoUpdateQuote(true);
}

bool DoUpdateQuote(bool bCheck)
{
	if(bCheck)
	{
		if(!CanUpdate())
			return false;
	}

	//calc total
	double dTotalPrice = 0;
	int i = 0;
	for(i=0; i<dtCart.Rows.Count; i++)
	{
		string price = dtCart.Rows[i]["supplierPrice"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();
		double dPrice = MyMoneyParse(price);
		double dsTotal = dPrice * int.Parse(qty);
		dTotalPrice += dsTotal;
	}

	double dAmount = dTotalPrice + m_dFreight;
	double dGstRate = MyDoubleParse(m_gstrate);
	double dTotalTax = dAmount * dGstRate;
	dAmount *= (1 + dGstRate);

	//round
	m_dFreight = Math.Round(m_dFreight, 2);
	dTotalPrice = Math.Round(dTotalPrice, 2);
	dAmount = Math.Round(dAmount, 2);
	dTotalTax = Math.Round(dTotalTax, 2);
	m_dTotal = dAmount;

	m_poID = Request.Form["po_id"];

	string sc = "UPDATE purchase SET ";
	sc += "branch_id=" + m_branchID + ", ";
	if(Request.Form["sup_inv_number"] != null)
		sc += "inv_number='" + m_supInvNumber + "', ";
	sc += "supplier_id=" + m_supplierID + ", ";
	sc += "buyer_id=" + m_billToID + ", ";
	sc += "shipto='" + EncodeQuote(m_shipto) + "', ";
	sc += "currency=" + m_currency + ", ";
	sc += "exchange_rate=" + m_exrate + ", ";
	sc += "gst_rate=" + m_gstrate + ", ";
	sc += "note='" + EncodeQuote(m_note) + "' ";
	sc += ", total=" + dTotalPrice;
	sc += ", tax=" + dTotalTax;
	sc += ", freight=" + m_dFreight;
	sc += ", total_amount=" + m_dTotal;
	sc += " WHERE id=" + m_poID;

	sc += " DELETE FROM purchase_item WHERE id='" + m_poID + "'";
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

	if(!RecordPurchaseItem())
		return false;
	return true;
}

bool GetStockStatus()
{
	string sc = "SELECT all_in_stock FROM purchase WHERE id= " + m_poID; 
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "purch_instock") == 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	m_bStockUpdated = (bool)dst.Tables["purch_instock"].Rows[0]["all_in_stock"];
	return true;
}

bool UpdateStockQty()
{
	string sc = "SELECT i.code, i.qty, i.price, c.id, p.currency, p.exchange_rate, p.branch_id ";
	sc += " FROM purchase_item i JOIN purchase p ON p.id=i.id JOIN code_relations c ON c.code=i.code ";
	sc += " WHERE i.id = " + m_poID + " ORDER BY i.kid ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "newstock") == 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i = 0; i < dst.Tables["newstock"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["newstock"].Rows[i];
		bool bExistsInProduct = ExistingProd(dst.Tables["newstock"].Rows[i]["code"].ToString());

		if(!UpdateQty(dst.Tables["newstock"].Rows[i], bExistsInProduct))
			return false;
	}
	return true;
}

bool UpdateAverageCost(DataRow dr, bool bExistsInProduct, bool bUpdatePrice)
{
	string code = dr["code"].ToString();
	int qty = MyIntParse(dr["qty"].ToString());

	if(qty <= 0)
		return true; //no price update on credits

	string currency = dr["currency"].ToString();
	double dexRate = MyDoubleParse(dr["exchange_rate"].ToString());
	double dForeignSupplierPrice = Math.Round(MyDoubleParse(dr["price"].ToString()), 2);
	double dCost = Math.Round(dForeignSupplierPrice / dexRate, 2);
	
	string sc = "SELECT c.id, c.average_cost, c.supplier_price AS last_cost_nzd, c.nzd_freight, p.stock, c.rate ";
	sc += " FROM code_relations c JOIN ";
	if(bExistsInProduct)
		sc += " product p ON p.code=c.code ";
	else
		sc += " product_skip p ON p.id=c.id ";
	sc += " WHERE c.code=" + code;
	if(dst.Tables["ac"] != null)
		dst.Tables["ac"].Clear();
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "ac") == 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	DataRow drp = dst.Tables["ac"].Rows[0];

	string id = EncodeQuote(drp["id"].ToString());

	string nzd_freight = drp["nzd_freight"].ToString();
	double dFreight = 0;
	if(nzd_freight != "")
		dFreight = MyDoubleParse(nzd_freight);
	dCost += dFreight;
	
	double dRate = MyDoubleParse(drp["rate"].ToString());
	double dBottomPrice = Math.Round(dCost * dRate, 2);
	double dCostOld = Math.Round(MyDoubleParse(drp["average_cost"].ToString()), 2);
	if(dCostOld == 0)
		dCostOld = Math.Round(MyDoubleParse(drp["last_cost_nzd"].ToString()), 2);
	
	double qtyOld = 0;
	if(drp["stock"].ToString() != "") 
		qtyOld = MyIntParse(drp["stock"].ToString());
	//in case stock is wrong
	if(qtyOld < 0)
		qtyOld = 0;
	
	double dAverageCostNew = Math.Round((dCostOld * qtyOld + dCost * qty) / (qtyOld + qty), 2);
	
	//update last cost, average_cost
	sc = "UPDATE code_relations SET currency=" + currency + ", exchange_rate=" + dexRate;
	sc += ", foreign_supplier_price=" + dForeignSupplierPrice + ", average_cost=" + dAverageCostNew;
	sc += ", supplier_price=" + dCost;
	if(bUpdatePrice)
	{
		sc += ", manual_cost_frd=" + dForeignSupplierPrice;
		sc += ", manual_exchange_rate=" + dexRate;
		sc += ", manual_cost_nzd=" + dCost;
	}
	sc += " WHERE code=" + code;
	if(bUpdatePrice)
	{
		if(bExistsInProduct)
			sc += " UPDATE product SET price=" + dBottomPrice + ", supplier_price = " + dCost + " WHERE code=" + code; 
		else
			sc += " UPDATE product_skip SET price=" + dBottomPrice + " WHERE id='" + id + "' "; 
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

bool UpdateQty(DataRow dr, bool bExistsInProduct)
{
	double dPrice = MyMoneyParse(dr["price"].ToString());
	string branch_id = dr["branch_id"].ToString();
	string code = dr["code"].ToString();
	string id = EncodeQuote(dr["id"].ToString());
	int qty = MyIntParse(dr["qty"].ToString());

	string sc = "";
	sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + code;
	sc += " AND branch_id=" + branch_id + ")";
	sc += " INSERT INTO stock_qty (qty, code, branch_id, supplier_price) ";
	sc += " VALUES ('";
	sc += qty + "', ";
	sc += code + ", ";
	sc += branch_id + ", ";
	sc += dPrice;
	sc += ")";
	sc += " ELSE UPDATE stock_qty SET qty = qty + " + qty;
	sc += ", supplier_price = " + dPrice + " WHERE code = " + code + " AND branch_id=" + branch_id;

	if(bExistsInProduct)
	{
		sc += " UPDATE product SET eta='', stock=stock+" + qty + " WHERE code=" + code;
	}
	else
	{
		sc += " UPDATE product_skip SET eta='', stock=stock+" + qty + " WHERE id='" + id + "' ";
	}
//DEBUG("sc=", sc);
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

bool ExistingProd(string s_ProdID)
{
	string sc = "SELECT stock AS qty FROM product WHERE code = " + s_ProdID;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int nRet = myCommand.Fill(dst, "oldItemFound");
		if(nRet > 0)
			m_bexistingProd = true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return m_bexistingProd;
}

void UpdateAllFields()
{
	Session["purchase_branch_id" + m_ssid] = Request.Form["branch"];
	Session["purchase_staff_id" + m_ssid] = Request.Form["staff"];
	Session["purchase_type" + m_ssid] = Request.Form["type"];
	Session["purchase_status" + m_ssid] = Request.Form["status"];
	Session["purchase_comment" + m_ssid] = Request.Form["note"];
	Session["purchase_inv" + m_ssid] = Request.Form["sup_inv_number"];
	Session["purchase_freight" + m_ssid] = MyMoneyParse(Request.Form["freight"]);
	Session["purchase_currency" + m_ssid] = Request.Form["currency"];
	Session["purchase_exrate" + m_ssid] = Request.Form["exrate"];
	Session["purchase_gstrate" + m_ssid] = Request.Form["gstrate"];
	Session["purchase_billtoid" + m_ssid] = Request.Form["buyer_id"];
	Session["purchase_shipto" + m_ssid] = Request.Form["shipto"];
	Session["purchase_supplierid" + m_ssid] = Request.Form["supplier_id"];
	UpdateSalesPrice();
	if(Request.Form["exrate"] != Request.Form["exrate_old"])
	{
		m_currencyName = GetEnumValue("currency", Request.Form["currency"]).ToUpper();
		SetSiteSettings("exchange_rate_" + m_currencyName.ToLower(), Request.Form["exrate"]);
	}
}

void RestoreAllFields()
{
	if(Session["purchase_current_po_id" + m_ssid] != null)
		m_poID = Session["purchase_current_po_id" + m_ssid].ToString();
	if(Session["purchase_current_po_number" + m_ssid] != null)
		m_poNumber = Session["purchase_current_po_number" + m_ssid].ToString();
	if(Session["purchase_comment" + m_ssid] != null)
		m_note = Session["purchase_comment" + m_ssid].ToString();
	if(Session["purchase_type" + m_ssid] != null)
		m_quoteType = Session["purchase_type" + m_ssid].ToString();
	if(Session["purchase_inv" + m_ssid] != null)
		m_supInvNumber = Session["purchase_inv" + m_ssid].ToString();
	if(Session["purchase_freight" + m_ssid] != null)
		m_dFreight = MyDoubleParse(Session["purchase_freight" + m_ssid].ToString());
	if(Session["purchase_branch_id" + m_ssid] != null)
		m_branchID = Session["purchase_branch_id" + m_ssid].ToString();
	if(Session["purchase_staff_id" + m_ssid] != null)
		m_staffID = Session["purchase_staff_id" + m_ssid].ToString();
	if(Session["purchase_status" + m_ssid] != null)
		m_status = Session["purchase_status" + m_ssid].ToString();
	if(Session["purchase_currency" + m_ssid] != null)
		m_currency = Session["purchase_currency" + m_ssid].ToString();
	if(Session["purchase_exrate" + m_ssid] != null)
	{
		m_exrate = Session["purchase_exrate" + m_ssid].ToString();
		Trim(ref m_exrate);
		if(m_exrate == "")
			m_exrate = "1";
	}
	if(Session["purchase_gstrate" + m_ssid] != null)
		m_gstrate = Session["purchase_gstrate" + m_ssid].ToString();
	if(Session["purchase_billtoid" + m_ssid] != null)
		m_billToID = Session["purchase_billtoid" + m_ssid].ToString();
	if(Session["purchase_supplierid" + m_ssid] != null)
		m_supplierID = Session["purchase_supplierid" + m_ssid].ToString();
	if(Session["purchase_shipto" + m_ssid] != null)
		m_shipto = Session["purchase_shipto" + m_ssid].ToString();
}

bool UpdateSalesPrice()
{
	int quantity = 0;
	int quantityOld = 0;
	double dPrice = 0;
	double dPriceOld = 0;
	double dTotal = 0;
	int i = dtCart.Rows.Count - 1;
	for(; i>=0; i--)
	{
		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
		{
			if(dtCart.Rows[i]["system"] == "1")
				continue;
			string code = Request.Form["code"+i.ToString()];
			string name = Request.Form["name"+i.ToString()];
			string supplier = Request.Form["supplier"+i.ToString()];
			string supplier_code = Request.Form["supplier_code"+i.ToString()];
			string sqty = Request.Form["qty"+i.ToString()];
			string sprice = Request.Form["price"+i.ToString()];
			if(IsInteger(sqty))
			{
				double dqty = double.Parse(sqty);
				quantity = (int)dqty;
			}
			if(quantity == 0 || Request.Form["del" + i.ToString()] == "X")
			{
				dtCart.Rows.RemoveAt(i);
				continue;
			}

			dPrice = MyMoneyParse(sprice);
			dtCart.AcceptChanges();
			dtCart.Rows[i].BeginEdit();
			dtCart.Rows[i]["code"] = code;
			dtCart.Rows[i]["name"] = name;
			dtCart.Rows[i]["supplier"] = supplier;
			dtCart.Rows[i]["supplier_code"] = supplier_code;
			dtCart.Rows[i]["quantity"] = quantity;
			dtCart.Rows[i]["supplierPrice"] = dPrice.ToString();
			dtCart.Rows[i].EndEdit();			
			dtCart.AcceptChanges();
		}
	}

	return true;
}

bool MyDrawTable()
{
	if(m_supplierID == "0" || Request.QueryString["sup"] == "0") //sup=0 from makepurchase.cs
	{
		if(dtCart.Rows.Count > 0)
		{
			if(!GetPossibleSupplierID())
				return false;
		}
	}
	if(!GetSupplier())
		return false;

	bool bRet = true;
//	DrawTableHeader();
	
	tableTitle = "<font color=red>";
	if(m_bOrderCreated)
		tableTitle += GetEnumValue("receipt_type", m_quoteType).ToUpper() + " #" + m_poNumber;
	else
		tableTitle += "Custom GST";
	tableTitle += "</font>";

	Response.Write("<form name=form1 action='" + m_url + "' method=post>");
	Response.Write("<input type=hidden name=type value=" + m_quoteType + ">");

	Response.Write("<table width=100% height=100% bgcolor=white align=center valign=center><tr><td valign=top>");
	Response.Write("<center><h3>" + tableTitle + "</h3></center>");

	//print sales header table
	if(!PrintPurchaseHeaderTable())
		return false;
//	Response.Write("<table class=d align=center valign=center cellspacing=1 cellpadding=0 border=1>");

	Response.Write("<table width=90%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=5>");

	Response.Write(PrintShipToTable());

	Response.Write("</td></tr>");
	Response.Write("<input type=hidden name=exchange_rate_" + GetEnumID("currency", "nzd") + " value=1>");
	Response.Write("<input type=hidden name=exchange_rate_" + GetEnumID("currency", "usd") + " value=" + GetSiteSettings("exchange_rate_usd", "0.49") + ">");
	Response.Write("<input type=hidden name=exchange_rate_"  + GetEnumID("currency", "aud") + " value=" + GetSiteSettings("exchange_rate_aud", "0.87") + ">");
//	Response.Write("<tr><td colspan=5 align=right>");
//	Response.Write("<b>Currency : </b><select name=currency onchange=\"UpdateEXRate();\">");
//	Response.Write(GetEnumOptions("currency", m_currency));
//	Response.Write("</select>");
//	Response.Write("&nbsp&nbsp;<b>Exchange-Rate : </b><input type=text name=exrate size=3 style=text-align:right value=" + m_exrate + ">");
//	Response.Write("&nbsp&nbsp&nbsp&nbsp;<b>GST-Rate : </b><input type=text name=gstrate size=3 style=text-align:right value=" + m_gstrate + ">");
//	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=5>");

	if(!PrintCartItemTable())
		return false;

	Response.Write("</td></tr></table>");

	Response.Write("<input type=hidden name=order_id value=" + m_orderID + ">");
	Response.Write("</form>");

	PrintJavaFunction();

	if(m_bOrderCreated)
	{
		Response.Write(PrintDirectOrderFrom());
	}

	return bRet;
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
}

bool PrintCartItemTable()
{
	int i = 0;
	GenerateGST(m_pid,"");

	Response.Write("<br>");
//	Response.Write("<table width=90% align=center cellpadding=2 cellspacing=1 border=0>");
	Response.Write("<table border=0 cellpadding=0 width=100% cellspacing=0>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td nowrap>CODE</td>");
//	Response.Write("<td nowrap>SUPPLIER</td>");
	Response.Write("<td width=130 nowrap>M_PN</td>");
	Response.Write("<td>DESCRIPTION</td>");
//	Response.Write("<td align=right>COST</td>");
	Response.Write("<td align=right>PRICE(" + m_currencyName + ")</td>");
	if(m_bOrderCreated)//m_quoteType != GetEnumID("receipt_type", "bill"))
	{
		if(m_status == GetEnumID("purchase_order_status", "received"))
		{
			Response.Write("<td align=right>QTY</td>");
			Response.Write("<td align=right><font color=yellow><b>UPDATE PRICE?</b></font></td>");
		}
		else
		{
			Response.Write("<td align=right>ORDERED</td>");
			Response.Write("<td align=right>RECEIVED</td>");
		}
	}
	else
	{
		Response.Write("<td align=center>QTY</td>");
		Response.Write("<td align=right nowrap>TOTAL(" + m_currencyName + ")</td></tr>");
	}

	dTotalPrice = 0;
	dTotalGST = 0;
	dAmount = 0;
	dTotalSaving = 0;

	double dRowPrice = 0;
	double dRowGST = 0;
	double dRowTotal = 0;
	double dRowSaving = 0;


//vin transfer variable here
	Response.Write("<input type=hidden size=20 name=m_id value=" + m_id + ">");
	Response.Write("<input type=hidden size=20 name=m_po_num value=" + m_po_num + ">");
	Response.Write("<input type=hidden size=20 name=m_inv_num value=" + m_inv_num + ">");
	Response.Write("<input type=hidden size=20 name=m_branch_id value=" + m_branch_id + ">");
	Response.Write("<input type=hidden size=20 name=m_staff_id value=" + m_staff_id + ">");
	Response.Write("<input type=hidden size=20 name=m_type value=" + m_type + ">");
	Response.Write("<input type=hidden size=20 name=m_supplier_id value=" + m_supplier_id + ">");
	Response.Write("<input type=hidden size=20 name=m_buyer_id value=" + m_buyer_id + ">");
	Response.Write("<input type=hidden size=20 name=m_totalgst value=" + m_totalgst + ">");
	Response.Write("<input type=hidden size=20 name=m_totAmt value=" + m_totAmt + ">");
	Response.Write("<input type=hidden size=20 name=m_total value=" + m_total + ">");
	Response.Write("<input type=hidden size=20 name=m_freight value=" + m_freight + ">");
	Response.Write("<input type=hidden size=20 name=m_total_amount value=" + m_total_amount + ">");
	Response.Write("<input type=hidden size=20 name=m_shipto value='" + m_shipto + "'>");
	Response.Write("<input type=hidden size=20 name=m_date_create value=" + m_date_create + ">");
	Response.Write("<input type=hidden size=20 name=m_sales_order_id value=" + m_sales_order_id + ">");
	Response.Write("<input type=hidden size=20 name=m_all_in_stock value=" + m_all_in_stock + ">");
	Response.Write("<input type=hidden size=20 name=m_currency value=" + m_currency + ">");
	Response.Write("<input type=hidden size=20 name=m_exchange_rate value=" + m_exchange_rate + ">");
	Response.Write("<input type=hidden size=20 name=m_gstrate value=" + m_gstrate + ">");
	Response.Write("<input type=hidden size=20 name=m_status value=" + m_status+ ">");

	//supplier code
	Response.Write("<td>");
	Response.Write("<input type=hidden size=20 name=supplier_code");
	Response.Write(" value='");
	Response.Write("");
	Response.Write("'>" + "" + "</td>\r\n");

	//M_PN
	Response.Write("<td>");
	Response.Write("<input type=hidden size=50 name=m_pn value='");
	Response.Write("GST");
	Response.Write("'>" + "GST" + "</td>\r\n");

	//description
	Response.Write("<td width=40%>");
	Response.Write("<input type=hidden size=50 name=dep value='");
	Response.Write("NZ Custom");
	Response.Write("'>" + "NZ Custom" + "</td>\r\n");

	//price
	Response.Write("<td align=right>");
	Response.Write("<input type=text size=7 readonly style='text-align:right' name=price value='");
	Response.Write(m_totalgst.ToString());
	Response.Write("'></td>\r\n");

	//quantity
	Response.Write("<td align=right nowrap><input type=text size=3 readonly style='text-align:right' name=qty");
	Response.Write(" value='1'>");

	//total
	Response.Write("<td align=right nowrap><input type=text size=7 readonly style='text-align:right' name=tot");
	Response.Write(" value='"+ m_totalgst.ToString() +"'>");


	dTotalPrice = Math.Round(dTotalPrice, 2);

	double dFinal = dTotalPrice + m_dFreight;
	if(Request.Form["total"] != null && Request.Form["total"] != "")
	{
		if(Request.Form["total"] != Request.Form["total_old"])
			dFinal = MyMoneyParse(Request.Form["total"]);
	}
//	double discount = Math.Round((dTotalPrice - dFinal) / dTotalPrice * 100, 0);

//	if(m_bOrder)
//		discount = 0;

	dAmount = dFinal;
	double dGstRate = MyDoubleParse(m_gstrate);
	dTotalGST = Math.Round(dAmount * dGstRate, 2);
	dAmount *= (1 + dGstRate);

//	dTotalGST = dAmount/(1+dGstRate)*dGstRate;

	Response.Write("<tr bgcolor=#ffffff><td colspan=" + m_nCols + ">");

	//start comment and price table
	Response.Write("<table width=100%>");

	Response.Write("<tr><td>&nbsp;</td></tr>");
//	Response.Write("<tr><td colspan=6><hr></td></tr>");

	Response.Write("<tr><td>");

	//start comment table
	Response.Write("<table>");
	Response.Write("<tr><td><b>Comment : </b></td></tr>");
	Response.Write("<tr><td><textarea name=note cols=70 rows=5>" + m_note + "</textarea></td>");
	Response.Write("</tr></table>");
	//end comment table

	Response.Write("</td><td align=right>");

	//start price talbe
	Response.Write("<table>");

	Response.Write("<tr bgcolor=#ffffff><td><b>SubTotal(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write(dTotalPrice.ToString("c"));
	Response.Write("</b></td></tr>");

//	Response.Write("<tr bgcolor=#ffffff><td><b>Freight(" + m_currencyName + ") : </b></td>");
//	Response.Write("<td align=right>");
//	Response.Write("<input type=text size=5 style='text-align:right' align=right name=freight value='" + m_dFreight.ToString("c") + "'>");
//	Response.Write("</td></tr>");

	//total GST
	Response.Write("<tr bgcolor=#ffffff><td><b>GST(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write(m_totalgst.ToString("c"));
	Response.Write("</b></td></tr>");

	//total amount due
	Response.Write("<tr bgcolor=#ffffff><td><b>Total Amount(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write(m_totAmt.ToString("c")); 
	Response.Write("</b></td></tr>");

	Response.Write("</table>");
	//end price table

	Response.Write("</td></tr></table>");
	//end comment and price table

	Response.Write("</td></tr>");

	//Response.Write("<tr><td colspan=" + m_nCols + " align=right>");
	//Response.Write("<input type=submit name=cmd value='Recalculate Price' " + Session["button_style"] + "></td>");
	//Response.Write("</tr>");

	Response.Write("<tr><td colspan=" + m_nCols + " align=right>");
	if(m_bOrderCreated)
	{
		if((m_quoteType == "1" || m_quoteType == "2") && (m_status == "1" || m_status == "3") )  //Quote or Bill
		{
			Response.Write("<input type=checkbox name=chkdelete> Delete this record ");
			Response.Write("&nbsp;&nbsp;<input type=submit name=cmd value='Delete' " + Session["button_style"] + ">&nbsp;&nbsp;&nbsp;");
			if(Session["purchase_need_update" + m_ssid] == null)
				Response.Write("<input type=submit name=cmd value='Receive' " + Session["button_style"] + ">");
		}
		else if((m_quoteType == "1" || m_quoteType == "2") && m_status == "4")// 4 = GetEnumID("purchase_order_status", "deleted"))
		{
			Response.Write("<input type=checkbox name=chkundodelete>Undo Delete ");
			Response.Write("&nbsp;&nbsp;<input type=submit name=cmd value='Undo Delete' " + Session["button_style"] + ">&nbsp;&nbsp;&nbsp;");
		}
		if(m_status == "2" && m_quoteType != GetEnumID("receipt_type", "bill"))
		{
			//supplier invoice number
			Response.Write("<b>Supplier Inv Date. : </b>");
			Response.Write("<input type=text name=inv_date size=10 value='" + DateTime.Now.ToString("dd-MM-yyyy") + "'> ");
			Response.Write(" &nbsp&nbsp; <b>Supplier Inv No. : </b>");
			Response.Write("<input type=text name=sup_inv_number size=10 style='text-align:right' value='" + m_supInvNumber + "'> ");
			Response.Write("<input type=submit name=cmd value='Change To Bill' " + Session["button_style"] + ">");
		}
		else
		{
			Response.Write("<input type=submit name=cmd value='Update Order' " + Session["button_style"] + ">");
		}
		Response.Write("<input type=button onclick=window.open('custgst.aspx?t=pp&n=" + m_poID + "&ssid=" + m_ssid + "') value='Print Preview' " + Session["button_style"] + ">");
	}
	else
	{
		Response.Write("<input type=submit name=cmd value='Create Order' " + Session["button_style"] + ">");
	}
	Response.Write("</td></tr>");
/*
	//end purchase session to avoid mass use of session object, one puchase process in one session only
	Response.Write("<tr><td colspan=" + m_nCols + " align=right>");
	Response.Write("<input type=button value='End Purchase Session' onclick=window.location=('purchase.aspx?t=end') ");
	Response.Write("style=\"font-size:8pt;font-weight:bold;background-color:red;color:yellow;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\"");
	Response.Write(">");
	Response.Write("</td></tr>");
*/
	if(m_bOrderCreated)
	{
		Response.Write("<tr><td colspan=7><input type=checkbox name=send_confirm>");
		Response.Write("Email Order : <input type=text name=email value='" + m_supplierEmail + "'> ");
		Response.Write("<input type=submit name=cmd value=Send " + Session["button_style"] + ">");
		if(m_bAlreadySent)
			Response.Write(" <i>( " + m_sentNotice + " )</i>");
		Response.Write("</td></tr>");
	}

	Response.Write("</table>");
	Response.Write("<input type=hidden name=supplierID value='" + m_supplierID + "'>");
	return true;
}

bool SSPrintOneRow(string kid, int nRow, string code, string supplier, string supplier_code, string desc, double dPrice, int qty, double dTotal)
{
	Response.Write("<tr ");
	if(bCartAlterColor)
		Response.Write("bgcolor=#EEEEEE");
	else
		Response.Write("bgcolor=white");
	bCartAlterColor = !bCartAlterColor;

	Response.Write(">");

	//kid
	Response.Write("<input type=hidden name=kid" + nRow.ToString() + " value=" + kid + ">");

	//code
	Response.Write("<td>");
	Response.Write("<input type=hidden size=7 name=code" + nRow.ToString() + " value=");
	Response.Write(code);
//	if(m_status != "1")
//		Response.Write(" readonly=true ");
	Response.Write(">");

	Response.Write("<a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
	Response.Write(code);
	Response.Write("</a> ");
//	if(CheckPatent("viewsales"))
	{
		Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
		Response.Write("code=" + code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");

		Response.Write("<input type=button title='View Purchase History' onclick=\"window.open('viewpurchase.aspx?");
		Response.Write("code=" + code + "')\" value='P' " + Session["button_style"] + ">");
	}
	Response.Write("</td>\r\n");

	//supplier
//	Response.Write("<td>");
//	Response.Write("<input type=text size=7 name=supplier" + nRow.ToString() + " value=");
//	Response.Write(supplier);
//	if(m_status != "1")
//		Response.Write(" readonly=true ");
//	Response.Write("></td>\r\n");

	//supplier code
	Response.Write("<td>");
	Response.Write("<input type=hidden size=20 name=supplier_code" + nRow.ToString());
	if(m_status != "1")
		Response.Write(" readonly=true ");
	Response.Write(" value='");
	Response.Write(supplier_code);
	Response.Write("'>" + supplier_code + "</td>\r\n");

	//description
	Response.Write("<td width=40%>");
	Response.Write("<input type=hidden size=50 name=name" + nRow.ToString() + " value='");
	Response.Write(desc);
	Response.Write("'>" + desc + "</td>\r\n");

	//price
	Response.Write("<td align=right>");
	Response.Write("<input type=text size=7 style='text-align:right' name=price" + nRow.ToString() + " value='");
	Response.Write(dPrice.ToString("c"));
	Response.Write("'></td>\r\n");

	//quantity
	Response.Write("<td align=right nowrap><input type=text size=3 style='text-align:right' name=qty" + nRow.ToString());
	if(m_status != "1" && m_status != "3") // 1: order placed 3: on backorder
		Response.Write(" readonly=true ");
	Response.Write(" value='" + qty.ToString() + "'>");
	//remove button
	if(m_status != "2")
		Response.Write("<input type=submit name=del" + nRow + " " + Session["button_style"] + " value=X>");
	Response.Write("<input type=hidden name=qty_old" + nRow.ToString() + " value='" + qty.ToString() + "'></td>\r\n");
	
//	if(m_bOrderCreated && m_quoteType != GetEnumID("receipt_type", "bill"))
	if(m_bOrderCreated)// && m_status != GetEnumID("purchase_order_status", "received"))//m_quoteType != GetEnumID("receipt_type", "bill"))
	{
		if(m_status == GetEnumID("purchase_order_status", "received"))
		{
			Response.Write("<td align=center><input type=checkbox name=update_cost" + nRow.ToString());
			if(qty > 0)
				Response.Write(" checked");
			Response.Write("> Yes</td>");

		}
		else
		{
			//received quantity
			Response.Write("<td align=right><input type=text size=3 style='text-align:right' name=rqty" + nRow.ToString());
			Response.Write(" value='" + qty.ToString() + "'></td>");
		}
	}
	else
	{
		//total
		Response.Write("<td align=right>");
		Response.Write(dTotal.ToString("c"));
		Response.Write("</td>\r\n</tr>\r\n");
	}
	return true;
}

string PrintShipToTable()
{
	return PrintShipToTable(true);
}

string PrintShipToTable(bool bWithHeader)
{
	StringBuilder sb = new StringBuilder();

	DataRow dr = null;
	if(!GetBillTo())
		return "";
	if(dst.Tables["billto"] == null || dst.Tables["billto"].Rows.Count <= 0)
		return "<font color=red><h5>Error, cannot get bill to information<br>Please set the right value of card_id_for_purchase_bill_to in <a href=settings.aspx class=o>Site Settings</a><h5>";
	dr = dst.Tables["billto"].Rows[0];

	string sCompany = "";
	string sContact = "";

	sCompany = dr["trading_name"].ToString();

	if(sCompany == "")
	{
		if(dr["Company"].ToString() != "")
			sCompany = dr["Company"].ToString();
		else
			sCompany = dr["Name"].ToString();
	}
	else //if we have company name, then put contact person name here as well
	{
		if(dr["NameB"].ToString() != "")
			sContact = dr["NameB"].ToString();
		else
			sContact = dr["Name"].ToString();
	}

	sb.Append("<table width=100% align=center><tr>");

	if(m_shipto == "")
	{
		m_shipto += dr["trading_name"].ToString() + "\r\n";
		m_shipto += dr["address1"].ToString() + "\r\n"; 
		m_shipto += dr["address2"].ToString() + "\r\n"; 
		m_shipto += dr["address3"].ToString() + "\r\n"; 
		m_shipto += dr["country"].ToString() + "\r\n"; 
	}
	sb.Append("<td valign=top><table cellpadding=0 cellspacing=0 border=0>");
	sb.Append("<tr><td valign=top><b>Ship To : &nbsp;</b></td><td>");
	if(bWithHeader)
		sb.Append("<textarea name=shipto cols=30 rows=5>" + m_shipto + "</textarea>");
	else
		sb.Append(m_shipto.Replace("\r\n", "<br>"));
	Response.Write("</td></tr></table>");
	sb.Append("</td>");

	sb.Append("<td>&nbsp&nbsp&nbsp&nbsp;</td>");

	//bill to
	sb.Append("<td valign=top><table cellpadding=0 cellspacing=0 border=0><tr><td valign=top>");
	sb.Append("<b>Bill To : &nbsp;</b></td><td>");
	
	if(bWithHeader)
	{
		sb.Append("<input type=hidden name=buyer_id value=" + dr["id"].ToString() + ">");
		if(dr["trading_name"].ToString() != "")
			sb.Append(dr["trading_name"].ToString());
		else
			sb.Append(dr["name"].ToString());
	}
	else
	{
		if(dr["trading_name"].ToString() != "")
			sb.Append(dr["trading_name"].ToString());
		else
			sb.Append(dr["name"].ToString());
	}
	sb.Append("<br>");
	sb.Append(dr["postal1"].ToString());
	sb.Append("<br>\r\n");
	sb.Append(dr["postal2"].ToString());
	sb.Append("<br>\r\n");
	sb.Append(dr["postal3"].ToString());
	sb.Append("<br>\r\n");

	sb.Append("</td></tr></table>");
	sb.Append("</td>");
	sb.Append("</tr></table>");
	//end of bill and shipto table
	return sb.ToString();
}

bool GetNextPONumber(ref int nNumber)
{
	if(dst.Tables["nextponumber"] != null)
		dst.Tables["nextponumber"].Clear();

	nNumber = 1001;

	string sc = "SELECT TOP 1 id FROM po_number ORDER BY id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "nextponumber") == 1)
			nNumber = int.Parse(dst.Tables["nextponumber"].Rows[0]["id"].ToString()) + 1;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

////////////////////////////////////////////////
bool PrintPurchaseHeaderTable()
{
	GenerateGST(m_pid,"");

	Response.Write("<table width=90% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td valign=top>");
	
	//branch & staff & date
	Response.Write("<table><tr>");
	Response.Write("<td><b>Branch : </b></td><td>");
	//vin mod
	PrintBranchNameOptions(m_branch_id);
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Staff : </b></td><td><b>");
	if(!m_bOrderCreated)
	{
		m_staffID = Session["card_id"].ToString();
	}
	Response.Write(TSGetUserNameByID(m_staffID));
	Response.Write("<input type=hidden name=staff value=" + m_staffID + ">");
	Response.Write("</b></td></tr>");

	Response.Write("<tr><td><b>Date : </b></td><td>");
	Response.Write("<input type=submit name=cmd value='" + m_sdate + "' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("</table>");	

	Response.Write("</td><td valign=top align=center>");
	
	//po number & invoice number
	Response.Write("<table>");
	Response.Write("<tr><td align=right><b>P.O. Number : </b></td><td>");
//	if(m_bOrderCreated)
//	{
		Response.Write("<b><font size=+1>" + m_po_num + "</font></b>");
		Response.Write("<input type=hidden name=po_number value='" + m_poNumber + "'>");
		Response.Write("<input type=hidden name=po_id value=" + m_poID + ">");
//	}
//	else
//	{
		//get nextx po_number
//		if(m_poNumber == "")
//		{
//			int nNumber = 0;
//			GetNextPONumber(ref nNumber);
//			m_poNumber = nNumber.ToString();
//		}
//		Response.Write("<font size=+1>" + m_poNumber + "</font>");
//		Response.Write("<input type=hidden name=number value='" + m_poNumber + "'>");
//	}
	//vin code
	Response.Write("<input type=hidden name=number_old value='" + m_poNumber + "'>");
	Response.Write("</td></tr>");
	//Response.Write("<tr><td align=right><b>Supplier Inv No. : </b></td><td><b>" + m_supInvNumber + "</b></td></tr>");
	Response.Write("<tr><td align=right><b>Supplier Inv No. : </b></td><td><b>" + m_inv_num + "</b></td></tr>");

	Response.Write("</table>");
	Response.Write("</td><td valign=top align=right>");
	Response.Write("<table>");

	////////////////////////////////////////////////////////////////////////////
	//order status table
	//Supplier
	Response.Write("<tr><td align=right><b>Supplier : </b></td><td>");

	//vin code get data from sitesetting if nz_custom_id existed
	string customid = GetSiteSettings("nz_custom_id");
	if (customid != "")
		m_supplierID = customid;

//DEBUG("suppid",m_supplierID);
//	if(m_supplierID != "0")
//	{
//		Response.Write("<b>" + TSGetUserCompanyByID(m_supplierID) + "</b>");
//		Response.Write("<input type=hidden name=supplier_id value=" + m_supplierID + ">");
//	}
//	else
//	{
		Response.Write("<select name=supplier_id onclick=window.location=('" + m_url + "&pid=" + m_pid + "&search=1')>");
		Response.Write("<option value=0>&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
		if(m_supplierID != "0")
		{
			if(m_supplierID == "-2")
				Response.Write("<option value='" + m_supplierID + "' selected><b>Other</b></option>");
			else
				Response.Write("<option value='" + m_supplierID + "' selected><b>" + TSGetUserCompanyByID(m_supplierID) + "</b></option>");
		}
		Response.Write("</select>");
//	}
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right><b>GST-Rate : </b></td><td>" + m_gstrate + " &nbsp;");
	Response.Write("<input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
	Response.Write("id=" + m_supplierID + "','',' width=350,height=350');\" value='View Supplier' " + Session["button_style"] + ">&nbsp;");
	Response.Write("</td></tr>");
	Response.Write("<tr><td align=right><b>Currency : </b></td><td>" + GetEnumValue("currency", m_currency).ToUpper() + "</td></tr>");
	Response.Write("<input type=hidden name=currency value=" + m_currency + ">");
	Response.Write("<tr><td align=right><b>Exchange-Rate : </b></td>");
	Response.Write("<td><input type=text name=exrate ");
	if(m_currency == "1") // 1 : nzd
		Response.Write(" readonly=true ");
	Response.Write(" size=5 style=text-align:right value=" + m_exrate + "></td></tr>");
	Response.Write("<input type=hidden name=exrate_old value=" + m_exrate + ">");
	Response.Write("<input type=hidden name=gstrate size=3 style=text-align:right value=" + m_gstrate + ">");

	bool bTypeBill = false;
	if(m_quoteType == GetEnumID("receipt_type", "bill"))
		bTypeBill = true;

	Response.Write("<input type=hidden name=status value=" + m_status + ">");
	if(m_bOrderCreated)
	{
		if(bTypeBill)
		{
			//status
			Response.Write("<tr><td align=right><b>Status : </b></td><td><b>");
			Response.Write(Capital(GetEnumValue("purchase_order_status", m_status)));
			Response.Write("</b></td></tr>");
		}
	}
	//serial number
	if(bTypeBill)
	{
		Response.Write("<tr><td align=right><b>Serial Numbers : </b></td><td>");
		if(m_bsnEntered)
			Response.Write("<b>All Entered</b>");
		else
			Response.Write("<a href=serial.aspx?t=&n=" + m_poID + " class=o target=_blank>Enter Now</a>");
		Response.Write("</td></tr>");
	}
	Response.Write("</table>");
	//end of status table
	//////////////////////////////////////////////////////////////////////////////////////////
	
	Response.Write("</td></tr>");
	Response.Write("</table>");

	return true;
}

bool CheckOrderForReceive()
{
	if(m_poID == null && m_poID == "")
	{
		Response.Write("<br><br><center><h3>&nbsp;&nbsp;&nbsp;ERROR GET PURCHASE ORDER ID #" + m_poID + "</h3>");
		return false;
	}
	string sc = "SELECT p.*, c.company AS supplier_name, c.short_name AS supplier_short_name ";
	sc += " FROM purchase p JOIN card c ON c.id=p.supplier_id ";
	sc += " WHERE p.id=" + m_poID;
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "purchase") <= 0)
		{
			Response.Write("<br><br><center><h3>&nbsp;&nbsp;&nbsp;ERROR GET PURCHASE ORDER ID #" + m_poID + "</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dst.Tables["purchase"].Rows[0];
	m_status = dr["status"].ToString();

	if(m_status == GetEnumID("purchase_order_status", "received"))
	{
		Response.Write("<br><br><center><h3>Error, Order already received</h3>");
		return false;
	}
	return true;
}

bool CanUpdate()
{
	if(m_poID == null && m_poID == "")
	{
		Response.Write("<br><br><center><h3>&nbsp;&nbsp;&nbsp;ERROR GET PURCHASE ORDER ID #" + m_poID + "</h3>");
		Response.Write("<br><button value='back' "+ Session["button_style"] +" OnClick=history.go(-1)><< Back</button>");
		return false;
	}
	DataSet dscu = new DataSet();
	string sc = "SELECT type FROM purchase WHERE id=" + m_poID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dscu, "purchase_type") <= 0)
		{
			Response.Write("<br><br><center><h3>&nbsp;&nbsp;&nbsp;ERROR GET PURCHASE ORDER ID #" + m_poID + "</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dscu.Tables["purchase_type"].Rows[0];
	string type = dr["type"].ToString();

	if(type == GetEnumID("receipt_type", "bill"))
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, Order already billed, cannot update</h3>");
		PrintAdminFooter();
		return false;
	}
	return true;
}


bool DoReceive()
{
	if(!CheckOrderForReceive())
		return false;

	int rows = 0;
	string sc = "SELECT * FROM purchase_item WHERE id=" + m_poID + " ORDER BY kid ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "quote");
		if(rows <= 0)
		{
			Response.Write("<h3>&nbsp;&nbsp;&nbsp;ERROR, Purchase Not Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";

	bool bHaveBackOrder = false;

	if(Session["ShoppingCart" + m_ssid] == null)
		return true;

	double dReceivedTotal = 0;
	double dRemainTotal = 0;
	dtCart = (DataTable)Session["ShoppingCart" + m_ssid];
//	int rows = dtCart.Rows.Count - 1;
	for(int i=rows-1; i>=0; i--)
	{
		string code = Request.Form["code"+i.ToString()];
		string name = Request.Form["name"+i.ToString()];
//		string supplier = Request.Form["supplier"+i.ToString()];
		string supplier_code = Request.Form["supplier_code"+i.ToString()];
		string sprice = Request.Form["price"+i.ToString()];
		double dPrice = Math.Round(MyMoneyParse(sprice), 2);
		int qty = MyIntParse(Request.Form["qty"+i.ToString()]);
		int rqty = MyIntParse(Request.Form["rqty"+i.ToString()]);
		string kid = Request.Form["kid"+i.ToString()];
		
		if(Request.Form["kid"+i.ToString()] == null || Request.Form["kid"+i.ToString()] == "")
		{
			Response.Write("<center><br><h3>Please follow a proper link</h3>");
			return false;
		}

		dReceivedTotal += dPrice * rqty;
		dRemainTotal += dPrice * (qty-rqty);

		if(rqty > qty)
		{
			Response.Write("<br><br><center><h3>Error, Ordered " + qty + " received " + rqty + " ?</h3>");
			return false;
		}
		
		if(!bHaveBackOrder && rqty != qty)
		{
			bHaveBackOrder = true;
			if(!DoBackOrder())
				return false;
		}

		if(rqty < qty) //split remaining to a new order with same po_number
		{
			sc += "INSERT INTO purchase_item (id, code, supplier_code, name, qty, price)";
			sc += " VALUES(" + m_poIDNew + ", " + code + ", '" + EncodeQuote(supplier_code) + "', ";
			sc += "'" + EncodeQuote(name) + "', ";
			sc += (qty-rqty).ToString() + ", " + dPrice + ") ";
		}
//		if(kid == null || kid == "") //newly inserted
//		{
//			sc += " INSERT INTO purchase_item (id, code, name, supplier_code, qty, price) ";
//			sc += " VALUES(" + m_poID + ", " + code + ", '" + EncodeQuote(name) + "', " + EncodeQuote(supplier_code);
//			sc += "', " + rqty + ", " + dPrice + ") ";
//		}
//		else
		{
			sc += " UPDATE purchase_item SET qty=" + rqty + ", price=" + dPrice;
			sc += ", name='" + EncodeQuote(name) + "', supplier_code='" + EncodeQuote(supplier_code) + "'";
			sc += " WHERE kid=" + kid;
		}

		int npo_id = int.Parse(m_poID);
			//update phased out item in code_relations table 
		if(!UpdatePhasedOutItem(code, qty, npo_id))
			return false;
	}

	string freight = Request.Form["freight"];
	double dFreight = Math.Round(MyMoneyParse(freight), 2);
//	string sup_inv_number = Request.Form["sup_inv_number"];
	string currency = Request.Form["currency"];
	string exrate = Request.Form["exrate"];
	string gstrate = Request.Form["gstrate"];
	double dexrate = MyDoubleParse(exrate);
	double dgstrate = MyDoubleParse(gstrate);
	double dTax = Math.Round((dReceivedTotal + dFreight) * dgstrate, 2);
	double dAmount = Math.Round((dReceivedTotal + dFreight) * (1 + dgstrate), 2);

	sc += " UPDATE purchase SET ";//type=" + GetEnumID("receipt_type", "bill");
	sc += " status=" + GetEnumID("purchase_order_status", "received");
	sc += ", date_received=GETDATE(), currency=" + currency;
	sc += ", exchange_rate=" + dexrate + ", gst_rate=" + dgstrate;
	sc += ", freight=" + dFreight + ", tax=" + dTax;
	sc += ", total=" + dReceivedTotal + ", total_amount=" + dAmount;
//	sc += ", inv_number='" + EncodeQuote(sup_inv_number) + "' ";
	sc += " WHERE id=" + m_poID;
	
	if(bHaveBackOrder)
	{
		//update new order's total 
		double drtax = dRemainTotal * dgstrate * dexrate;
		sc += " UPDATE purchase SET total=" + dRemainTotal + ", tax=" + drtax;
		sc += ", total_amount=" + (dRemainTotal+dFreight).ToString();
		sc += " WHERE id=" + m_poIDNew + " ";
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
	
	UpdateStockQty();
	return true;
}

bool DoBackOrder()
{
	string sd = "GETDATE()";
	if(m_sdate == "")
	{
		if(Session[m_sCompanyName + "purchase_create_date"] != null)
		{
			m_sdate = Session[m_sCompanyName + "purchase_create_date"].ToString();
			sd = "'" + m_sdate + "'";
		}
	}

	string status_backorder = GetEnumID("purchase_order_status", "on backorder");

	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO purchase (status, po_number, branch_id, staff_id, type, supplier_id, ";
	sc += " buyer_id, freight, total_amount, shipto, note, date_create, sales_order_id, currency, exchange_rate, gst_rate ";
	sc += ") VALUES(" + status_backorder + ", " + m_poNumber + ", " + m_branchID + ", " + m_staffID + ", '";
	sc += GetEnumID("receipt_type", "order") + "', " + m_supplierID + ", " + m_billToID + ", " + m_dFreight + ", ";
	sc += m_dTotal + ", '" + EncodeQuote(m_shipto) + "', '" + m_note + "', " + sd + ", '" + m_orderID + "'";
	sc += ", " + m_currency + ", " + m_exrate + ", " + m_gstrate;
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('purchase') AS id ";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "poid") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting poid IDENT");
			return false;
		}
		m_poIDNew = dst.Tables["poid"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoCreatePurchase()
{
	if(m_supplierID != "-1")
	{
		Session["purchase_supplierid" + m_ssid] = m_supplierID;
	}

	if(m_supplierID == "0" && Request.Form["supplier_id"] == "-1")
	{
		if(!GetPossibleSupplierID())
		{
			Response.Write("<h3>ERROR, NO SUPPLIER</h3>");
			return false;
		}
	}
	else if(m_supplierID == "-2")
	{
		m_supplierName = "";
		m_supplierEmail = "";
	}
	else
	{
		DataRow dr = null;
		if(!GetSupplier())
			return false;
		if(dst.Tables["card"] != null && dst.Tables["card"].Rows.Count > 0)
			dr = dst.Tables["card"].Rows[0];
		else
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Error, No Supplier");
			return false;
		}
		m_supplierName = dr["company"].ToString();
		if(m_supplierName == "")
			m_supplierName = dr["name"].ToString();
		if(m_supplierName == "")
			m_supplierName = dr["short_name"].ToString();
	}

	if(!GetBillTo())
		return false;

	//calc total
	double dTotalPrice = 0;
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		string price = dtCart.Rows[i]["supplierPrice"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();
		double dPrice = Math.Round(MyMoneyParse(price), 2);
		double dsTotal = dPrice * int.Parse(qty);
		dTotalPrice += dsTotal;
	}

	double dAmount = dTotalPrice + m_dFreight;
	double dGstRate = MyDoubleParse(m_gstrate);
	double dTotalTax = dAmount * dGstRate;
	dAmount *= (1 + dGstRate);

	//round
	m_dFreight = Math.Round(m_dFreight, 2);
	dTotalPrice = Math.Round(dTotalPrice, 2);
	dAmount = Math.Round(dAmount, 2);
	dTotalTax = Math.Round(dTotalTax, 2);
	m_dTotal = dAmount;

	string sd = "GETDATE()";
	if(m_sdate == "")
	{
		if(Session[m_sCompanyName + "purchase_create_date"] != null)
		{
			m_sdate = Session[m_sCompanyName + "purchase_create_date"].ToString();
			sd = "'" + m_sdate + "'";
		}
	}

	string s_allinstock = "0";		//"s_allinstock"=1 --- flag shows stock_qty table is updated;
	if(m_quoteType == "4")
		s_allinstock = "1";	
	
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO po_number (dummy) VALUES(0) SELECT IDENT_CURRENT('po_number') AS id ";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "order_number") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting IDENT");
			return false;
		}
		m_poNumber = dst.Tables["order_number"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	m_orderID = Request.Form["order_id"];

	sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO purchase (po_number, inv_number, branch_id, staff_id, type, supplier_id, ";
	sc += " buyer_id, total, tax, freight, total_amount, shipto, note, date_create, sales_order_id, all_in_stock ";
	sc += ", currency, exchange_rate, gst_rate, status ";
	sc += ") VALUES(" + m_poNumber + ", '" + m_supInvNumber + "', " + m_branchID + ", " + m_staffID + ", '";
	sc += Request.Form["type"] + "', " + m_supplierID + ", " + m_billToID + ", " + dTotalPrice + ", ";
	sc += dTotalTax + ", " + m_dFreight + ", ";
	sc += m_dTotal + ", '" + EncodeQuote(m_shipto) + "', '" + EncodeQuote(m_note) + "', " + sd + ", '" + m_orderID + "',";
	sc += s_allinstock + ", " + m_currency + ", " + m_exrate + ", " + m_gstrate + ", 1 ";
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('purchase') AS id ";
	sc += " COMMIT ";

//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "poid") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting poid IDENT");
			return false;
		}
		m_poID = dst.Tables["poid"].Rows[0]["id"].ToString();
		
		UpdateOrderPurchaseStatus();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(!RecordPurchaseItem())
		return false;

//DEBUG("m_bStockUpdated = ", m_bStockUpdated.ToString());
	if(!m_bStockUpdated && m_quoteType == "4")
	{
//DEBUG("here = ", 0);
		if(!UpdateStockQty())
			return false;
	}
	return true;
}

bool GetPossibleSupplierID()
{
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		DataRow drp = null;
		m_supplierName = dtCart.Rows[i]["supplier"].ToString();

		//get supplier id
		if(dst.Tables["card"] != null)
			dst.Tables["card"].Clear();
		string sc = "SELECT id, company, gst_rate FROM card WHERE short_name='" + m_supplierName + "'";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dst, "card") <= 0)
			{
				return true;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		//get customer data
		m_supplierID = dst.Tables["card"].Rows[0]["id"].ToString();
		m_gstrate = dst.Tables["card"].Rows[0]["gst_rate"].ToString();
		Session["purchase_supplierid" + m_ssid] = m_supplierID;
		Session["purchase_gstrate" + m_ssid] = m_gstrate;
//DEBUG("supplier_id=", m_supplierID);
		return true;
	}
	return true;
}

bool RecordPurchaseItem()
{
	//do shopping cart
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		
		string code = dtCart.Rows[i]["code"].ToString();
		string name = dtCart.Rows[i]["name"].ToString();
		if(name.Length > 1024)
			name = name.Substring(0, 1024);
		string supplier = dtCart.Rows[i]["supplier"].ToString();
		string supplier_code = dtCart.Rows[i]["supplier_code"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();
		string price = dtCart.Rows[i]["supplierPrice"].ToString();

		double dPrice = double.Parse(price, NumberStyles.Currency, null);

		//write to database
		if(!InsertToPurchaseItemTable(code, name, supplier_code, qty, dPrice))
			return false;
	}
	return true;
}

bool InsertToPurchaseItemTable(string code, string name, string supplier_code, string sQty, double dPrice)
{
	string sc = "INSERT INTO purchase_item (id, code, name, supplier_code, qty, price) ";
	sc += " VALUES(" + m_poID + ", '" + code + "', '" + name + "', '";
	sc += supplier_code + "', " + sQty + ", " + dPrice + ")";
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

bool GetSupplier()
{
	if(dst.Tables["card"] != null)
		dst.Tables["card"].Clear();
	//prepare customer ID
	string id = "";
	if(Request.QueryString["si"] == null)
	{
		if(m_supplierEmail == "")
		{
			if(Session["purchase_supplierid" + m_ssid] == null)
				return true;
			id = Session["purchase_supplierid" + m_ssid].ToString();
		}
	}
	else if(Request.QueryString["si"] != "")
	{
		//vin code for supplier search
		m_pid = Request.QueryString["pid"];

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
		m_supplierEmail = "";
		return true;
	}

	//do search
	string sc = "";
	if(id != "")
		sc = "SELECT * FROM card WHERE id=" + id;
	else
		sc = "SELECT * FROM card WHERE email='" + m_supplierEmail + "'";
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
	m_supplierEmail = dr["email"].ToString();
	m_supplierName = dr["company"].ToString();
	m_currency = dr["currency_for_purchase"].ToString();
	m_currencyName = GetEnumValue("currency", m_currency);
	m_exrate = GetSiteSettings("exchange_rate_" + m_currencyName.ToLower());
	if(m_exrate == "")
		m_exrate = "1";
	Session["purchase_exrate" + m_ssid] = m_exrate;
	Session["purchase_currency" + m_ssid] = m_currency;
	m_gstrate = dr["gst_rate"].ToString();
	if(m_supplierName == "")
		m_supplierName = dr["name"].ToString();
	if(m_supplierName == "")
		m_supplierName = dr["short_name"].ToString();
		
//DEBUG("id=", id);	
	if(id != "")
	{
		m_supplierID = dr["id"].ToString();
		Session["purchase_supplierid" + m_ssid] = m_supplierID;
		Session["purchase_gstrate" + m_ssid] = m_gstrate;
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
		if(Session["purchase_billtoid" + m_ssid] == null)
		{
			id = GetSiteSettings("card_id_for_purchase_bill_to");
			if(id == "")
				id = "1"; //default set to the first card avoid error
		}
		else
			id = Session["purchase_billtoid" + m_ssid].ToString();
	}
	else if(Request.QueryString["bi"] != "")
	{
		id = Request.QueryString["bi"].ToString();
		Session["purchase_billtoid" + m_ssid] = id;
		SetSiteSettings("card_id_for_purchase_bill_to", id);
	}
	//do search
	string sc = "";
	sc = "SELECT * FROM card WHERE id=" + id;
//DEBUG("sc=", sc);
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
	if(id != "")
	{
		m_billToID = dr["id"].ToString();
		Session["purchase_billtoid" + m_ssid] = id;
	}
//DEBUG("m_billToID=", m_billToID);
	return true;
}

bool DoSupplierSearch()
{
	//vin code for supplier search
	m_pid = Request.QueryString["pid"];

	string sc = "";
	string type_supplier = GetEnumID("card_type", "supplier");
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%#@#@#@#@#@#@%'";
	sc = "SELECT '<a href=custgst.aspx?si=' + convert(varchar, id) + '&pid=" + m_pid + "&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
	sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
	sc += ", short_name AS name, email, company FROM card ";
	sc += " WHERE (name LIKE " + kw + " OR email LIKE " + kw + " OR company LIKE " + kw + ")";
	if(m_bOrder)
		sc += " AND type=" + type_supplier;
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

	if(rows == 0)
	{
		sc = "SELECT '<a href=custgst.aspx?si=' + convert(varchar, id) + '&pid=" + m_pid + "&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
		sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
		sc += ", short_name AS name, email, company ";
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

	Response.Write("<form id=searchaction='" + m_url + "&search=1' method=post>");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search><input type=submit name=cmd value=Cancel>");
	Response.Write("</td></tr></table></form>");
	return true;
}
/*
bool DoBillToSearch()
{
	string type_others = GetEnumID("card_type", "others");

	string sc = "";
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%#@#@#@#@#@#@%'";
	sc = "SELECT '<a href=purchase.aspx?si=' + convert(varchar, id) + '&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
	sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
	sc += ", name=CASE name WHEN '' THEN company ELSE name END, email, company ";
	sc += " FROM card ";
	sc += " WHERE (name LIKE " + kw + " OR email LIKE " + kw + " OR company LIKE " + kw + ")";
	if(m_bOrder)
		sc += " AND type=" + type_others;
	sc += " ORDER BY name";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "billto");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows == 0)
	{
		sc = "SELECT '<a href=purchase.aspx?si=' + convert(varchar, id) + '&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
		sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
		sc += ", name=CASE name WHEN '' THEN company ELSE name END, email, company ";
		sc += " FROM card WHERE type=" + type_others + " ORDER BY name";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(dst, "billto");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	if(rows == 0) //list all
	{
		sc = "SELECT '<a href=purchase.aspx?si=' + convert(varchar, id) + '&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
		sc += ", '<a href=ecard.aspx?id=' + convert(varchar, id) + ' class=o>Edit</a>' AS 'Edit' ";
		sc += ", name=CASE name WHEN '' THEN company ELSE name END, email, company ";
		sc += " FROM card ORDER BY name";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(dst, "billto");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	//bind grid
	DataView source = new DataView(dst.Tables["billto"]);
	MyDataGridBill.DataSource = source ;
	MyDataGridBill.DataBind();

	Response.Write("<form id=search action='" + m_url + "&search=1&st=ship' method=post>");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search><input type=submit name=cmd value=Cancel>");
	Response.Write("</td></tr></table></form>");
	return true;
}
*/

string BuildOrderInvoice(string sType)
{
	StringBuilder sb = new StringBuilder();

	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	sb.Append("<body>\r\n");

	sb.Append("<b>");

	sb.Append("</b>");

	string type = "custom";
//	string type = GetEnumValue("receipt_type", sType);

//DEBUG(" m_supInvNumber = ", m_supInvNumber);
//DEBUG("m_poNumber",m_poNumber);
//DEBUG("type",type);

	sb.Append(InvoicePrintHeader(type, "", m_poNumber, m_sdate, "", m_supplierID, m_supplierName, m_supInvNumber));

	DataRow dr = null;
	if(!GetBillTo())
		return "";
	if(dst.Tables["billto"] != null && dst.Tables["billto"].Rows.Count > 0)
		dr = dst.Tables["billto"].Rows[0];
	
	sb.Append("<tr><td>");

	sb.Append(PrintShipToTable(false));
//	sb.Append(InvoicePrintShip(dr));

	sb.Append("</td></tr><tr><td>\r\n");

	sb.Append("<table width=100% cellpadding=0 cellspacing=0");
	sb.Append("><tr><td>");
	sb.Append("</td></tr><tr>");
	sb.Append("<td width=130>PART#</td>\r\n");
	sb.Append("<td width=70%>DESCRIPTION</td>\r\n");
	sb.Append("<td align=right>");
	
	if(!m_bNoIndividualPrice)
		sb.Append("PRICE&nbsp&nbsp;");
	else
		sb.Append("&nbsp;");
	
	sb.Append("</td>\r\n");
	sb.Append("<td nowrap align=right>QTY&nbsp;</td>\r\n");
	sb.Append("<td align=right>");
	
	if(!m_bNoIndividualPrice)
		sb.Append("AMOUNT");
	else
		sb.Append("&nbsp;");
	sb.Append("</td>");

	sb.Append("</tr>\r\n");
	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	double dTotal = 0;


	
	double dFreight = 0;
	if(dst.Tables["purchase"].Rows.Count > 0)
		dFreight = MyDoubleParse(dst.Tables["purchase"].Rows[0]["freight"].ToString());

//DEBUG("freight = ", dst.Tables["purchase"].Rows.Count);

//	if(sType == "bill")
	{
		double dAmount = dTotal + dFreight;
		double dGstRate = MyDoubleParse(m_gstrate);
		double dTAX = Math.Round((dTotal + dFreight) * dGstRate, 2);
		dAmount *= (1 + dGstRate);
		dAmount = Math.Round(dAmount, 2);

		//vin code 20th april 2004
		dTAX = MyDoubleParse(dst.Tables["purchase"].Rows[0]["tax"].ToString());
		dAmount = MyDoubleParse(dst.Tables["purchase"].Rows[0]["total_amount"].ToString());
 
		sb.Append("<tr><td>" + "GST" + "&nbsp&nbsp;</td>");
		sb.Append("<td width=70%>" + "NZ Custom" + "&nbsp&nbsp;</td>");
		sb.Append("<td align=right>" + dTAX.ToString("c") + "</td>");
		sb.Append("<td nowrap align=right>1&nbsp&nbsp;</td>");
		sb.Append("<td nowrap align=right> " + dTAX.ToString("c"));
		sb.Append("</td>");
		sb.Append("</tr>");
		
		sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>Sub-Total:</b></td><td align=right>");
		sb.Append(dTotal.ToString("c"));
		sb.Append("</td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>Freight:</b></td><td align=right>");
		sb.Append(dFreight.ToString("c"));
		sb.Append("</td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>GST:</b></td><td align=right>");
		sb.Append(dTAX.ToString("c"));
		sb.Append("</td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>TOTAL:</b></td><td align=right>");
		sb.Append(dAmount.ToString("c"));
		sb.Append("</td></tr>\r\n");
//DEBUG("amount = ", dAmount.ToString());
//DEBUG("tax = ", dTAX.ToString());
//DEBUG("total = ", dTotal.ToString());
	}

	sb.Append("</table>\r\n");

	sb.Append("</td></tr><tr><td>");
//DEBUG("note=", m_note);
	if(m_note != "")
	{
		sb.Append("<br><br><b>Comment : </b><br>");
		sb.Append(m_note.Replace("\r\n", "\r\n<br>"));
	}
	sb.Append("</td></tr></table>");
	sb.Append("</td></tr></table>");
//	sb.Append(InvoicePrintBottom());

	sb.Append("</body></html>");
	return sb.ToString();
}

bool RestorePurchase()
{
	if(!RestoreCustomer())
	{
		Response.Write("<center><h3>Error restore customer</h3></center>");
		return false;
	}

	int rows = 0;
	string sc = "SELECT * FROM purchase_item WHERE id=" + m_poID + " ORDER BY kid ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "quote");
//		if(rows <= 0)
//		{
//			Response.Write("<h3>&nbsp;&nbsp;&nbsp;ERROR, Purchase Not Found</h3>");
//			return false;
//		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//delete current quotation
	EmptyCart(); //empty shopping cart for optionals

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["quote"].Rows[i];
		if(!AddToCart(dr["kid"].ToString(), dr["code"].ToString(), m_supplierShortName, dr["supplier_code"].ToString(), 
			dr["qty"].ToString(), dr["price"].ToString(), "", dr["name"].ToString(), ""))
			return false;
	}
	return true;
}

bool RestoreCustomer()
{
	string sc = "SELECT p.*, c.company AS supplier_name, c.short_name AS supplier_short_name ";
	sc += " FROM purchase p JOIN card c ON c.id=p.supplier_id ";
	sc += " WHERE p.id=" + m_poID;
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "purchase") <= 0)
		{
			Response.Write("<h3>&nbsp;&nbsp;&nbsp;ERROR GET PURCHASE ORDER ID #" + m_poID + "</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dst.Tables["purchase"].Rows[0];
	m_poNumber = dr["po_number"].ToString();
	m_branchID = dr["branch_id"].ToString();
	m_staffID = dr["staff_id"].ToString();
	m_quoteType = dr["type"].ToString();
	m_supplierID = dr["supplier_id"].ToString();
	m_supplierName = dr["supplier_name"].ToString();
	m_supplierShortName = dr["supplier_short_name"].ToString();
	m_note = dr["note"].ToString();
	m_supInvNumber = dr["inv_number"].ToString();
	m_sdate = DateTime.Parse(dr["date_create"].ToString()).ToString("dd-MM-yyyy HH:mm");
	m_status = dr["status"].ToString();
	m_currency = dr["currency"].ToString();
	m_exrate = dr["exchange_rate"].ToString();
	m_gstrate = dr["gst_rate"].ToString();
	m_shipto = dr["shipto"].ToString();
	m_dFreight = MyDoubleParse(dr["freight"].ToString());

	m_bsnEntered = (bool)dr["sn_entered"];

	if(!m_bPrintView) //don't store to session if t=pp ( view only )
	{
		Session[m_sCompanyName + "purchase_create_date"] = m_sdate;
		Session["purchase_current_po_number" + m_ssid] = m_poNumber;
		Session["purchase_supplierid" + m_ssid] = m_supplierID;
	}

	//restore ship to if exists
	m_orderID = dr["sales_order_id"].ToString();

	m_bAlreadySent = bool.Parse(dr["already_sent"].ToString());
	if(m_bAlreadySent)
	{
		m_sentNotice = "Already sent to " + dr["sent_to"].ToString() + " by " + dr["who_sent"].ToString();
		m_sentNotice += " at " + DateTime.Parse(dr["time_sent"].ToString()).ToString("dd-MM-yyyy HH:mm");
	}

	if(m_supplierID == "-2") //others
	{
		m_supplierEmail = "";
		return true;
	}

	sc = "SELECT * FROM card WHERE id=" + m_supplierID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "supplier") <= 0)
		{
			Response.Write("<h3>&nbsp;&nbsp;&nbsp;ERROR GET Supplier ID #" + m_supplierID + "</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	string email = dst.Tables["supplier"].Rows[0]["email"].ToString();
	if(email == "")
		return true; //cash sales
	
	m_supplierEmail = email;
	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["card"]);
	string path = Request.ServerVariables["URL"].ToString();
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void PrintJavaFunction()
{
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function UpdateEXRate()");
	Response.Write("{\r\n");
	Response.Write("	var currency_id = document.form1.currency.value;\r\n");
	Response.Write("	if(currency_id == '2') document.form1.exrate.value = Number(document.form1.exchange_rate_2.value);\r\n");
	Response.Write("	else if(currency_id == '3') document.form1.exrate.value = Number(document.form1.exchange_rate_3.value);\r\n");
	Response.Write("	else document.form1.exrate.value = 1;\r\n");
	Response.Write("}\r\n");
	Response.Write("</script");
	Response.Write(">");
}

bool SameSupplier(string short_name)
{
	return true; //disable check DW 24.01.2003

	if(Session["purchase_supplierid" + m_ssid] != null)
	{
		if(Session["purchase_supplierid" + m_ssid].ToString() != "" && Session["purchase_supplierid" + m_ssid].ToString() != "0")
		{
			DataRow dr = GetCardData(Session["purchase_supplierid" + m_ssid].ToString());
			string sname = dr["short_name"].ToString();
			Trim(ref sname);
			Trim(ref short_name);
			if(sname.ToLower() != short_name.ToLower())
			{
				PrintAdminHeader();
				PrintAdminMenu();
				Response.Write("<br><br><center><h3>Sorry, this item is from a different supplier, please make another purchase for it");
				return false;
			}
		}
	}
	return true;
}

bool GetOrderInfo()
{
	DataRow dr = null;
	int rows = 0;
	if(m_orderID == "" || m_orderID == "0")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Error, wrong order id");
		return false;
	}

	string sc = " SELECT p.*, c.name AS sales_name ";
	sc += " FROM purchase p LEFT OUTER JOIN card c ON c.id = p.staff_id ";
	sc += " WHERE p.sales_order_id = " + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "purchasecheck");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
	{
		dr = dst.Tables["purchasecheck"].Rows[0];
		string salesName = dr["sales_name"].ToString();
		string pid = dr["id"].ToString();
		string ptime = DateTime.Parse(dr["date_create"].ToString()).ToString("dd-MM-yyyy HH:mm");

		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3><font color=red>Warning, Purchase Already Made</font></h3>");
		Response.Write("<h4>" + salesName + " has already made a purchase for this order at " + ptime + "<h4>");
		Response.Write("<h4><a href=custgst.aspx?n=" + pid + "&ssid=" + m_ssid + " class=o>View Purchase</h4>");

		return false;
	}
	
	sc = "SELECT c.*, oi.* ";
	sc += " FROM card c JOIN orders o ON c.id = o.card_id ";
	sc += " JOIN order_item oi ON oi.id = o.id ";
	sc += " WHERE o.id=" + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<h3>Error Invoice Number</h3>");
		return false;
	}
	
	dr = dst.Tables["invoice"].Rows[0];
	if(dr["name"] != null && dr["name"].ToString() != "")
		m_shipto += "" + dr["name"].ToString() + "\r\n";
	if(dr["trading_name"] != null && dr["trading_name"].ToString() != "")
		m_shipto += dr["trading_name"].ToString() + "\r\n";
	if(dr["address1"] != null && dr["address1"].ToString() != "")
		m_shipto += dr["address1"].ToString() + "\r\n";
	if(dr["address2"] != null && dr["address2"].ToString() != "")
		m_shipto += dr["address2"].ToString() + "\r\n";
	if(dr["city"] != null && dr["address3"].ToString() != "")
		m_shipto += dr["address3"].ToString() + "\r\n";
	if(dr["country"] != null && dr["country"].ToString() != "")
		m_shipto += dr["country"].ToString() + "\r\n";
//	if(dr["email"] != null && dr["email"].ToString() != "")
//		m_shipto += dr["email"].ToString() + "\r\n";
	if(dr["phone"] != null && dr["phone"].ToString() != "")
		m_shipto += dr["phone"].ToString() + "\r\n";

	m_supplierShortName = dr["supplier"].ToString();
	m_note = "Please ship to the specified address above";

	//build cart
	//delete current quotation
	EmptyCart(); //empty shopping cart for optionals

	for(int i=0; i<rows; i++)
	{
		dr = dst.Tables["invoice"].Rows[i];
		if(!AddToCart(dr["kid"].ToString(), dr["code"].ToString(), m_supplierShortName, dr["supplier_code"].ToString(), 
			dr["quantity"].ToString(), dr["supplier_price"].ToString(), "", dr["item_name"].ToString(), ""))
			return false;
	}
	return true;
}

bool UpdateOrderPurchaseStatus()
{
	if(m_orderID == "")
		return true;

	//update order purchase status
	string sc = " UPDATE orders SET purchase_id = " + m_poID + " WHERE id = " + m_orderID;
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

string PrintDirectOrderFrom()
{
	string sc = " SELECT * FROM direct_order WHERE supplier = " + m_supplierID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "direct_order") != 1)
			return "<input type=button onclick=window.open('tp.aspx?n=direct_order') value='Config Direct Order' " + Session["button_style"] + ">";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	DataRow dr = dst.Tables["direct_order"].Rows[0];
	string uri = dr["uri"].ToString();
	string login = dr["login"].ToString();
	string password = dr["password"].ToString();

	string s = "";
	s += "<form action=" + uri + " method=post target=_blank>";
	s += "<input type=hidden name=login value=" + login + ">";	
	s += "<input type=hidden name=password value=" + password + ">";
	s += "<input type=hidden name=contact value='" + Session["name"].ToString() + "'>";
	s += "<input type=hidden name=shipto value='" + m_shipto + "'>";
	s += "<input type=hidden name=po_number value=" + m_poNumber + ">";
	s += "<input type=hidden name=note value='" + m_note + "'>";
	s += "<input type=submit name=cmd value='Direct Order' " + Session["button_style"] + ">";

	s += "<input type=hidden name=rows value=" + dtCart.Rows.Count + ">";
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		string name = dtCart.Rows[i]["name"].ToString();
		string supplier_code = dtCart.Rows[i]["supplier_code"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();

		s += "<input type=hidden name=code" + i + " value=" + supplier_code + ">";
		s += "<input type=hidden name=qty" + i + " value=" + qty + ">";
		s += "<input type=hidden name=name" + i + " value='" + name + "'>";
	}

	s += "</form>";
	return s;
}

//vin code start on 21-4-2003 end on 22-4-2003
bool GenerateGST(string pid, string status)
{	
	if (status == "" && pid !="")
	{
		//======================================================================================================
		//fetch total value and calculate the GST
		string sc = "SELECT * FROM purchase WHERE id = "+pid;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dst, "selectpurchase") == 0)
				return true;
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		//======================================================================================================
		//get data from purchase 

		m_id = dst.Tables["selectpurchase"].Rows[0]["id"].ToString();
		m_po_num = dst.Tables["selectpurchase"].Rows[0]["po_number"].ToString();
		m_inv_num = dst.Tables["selectpurchase"].Rows[0]["inv_number"].ToString();
		m_branch_id = dst.Tables["selectpurchase"].Rows[0]["branch_id"].ToString();
		m_staff_id = dst.Tables["selectpurchase"].Rows[0]["staff_id"].ToString();
		m_type = dst.Tables["selectpurchase"].Rows[0]["type"].ToString();
		
		m_supplier_id = GetSiteSettings("nz_custom_id");
		m_buyer_id = dst.Tables["selectpurchase"].Rows[0]["buyer_id"].ToString();

		amount = dst.Tables["selectpurchase"].Rows[0]["total"].ToString();
		m_totalgst = MyDoubleParse(amount) * 0.125;
		m_totAmt = m_totalgst;
		m_total = "0";
		m_freight = dst.Tables["selectpurchase"].Rows[0]["freight"].ToString();
		m_total_amount = dst.Tables["selectpurchase"].Rows[0]["total_amount"].ToString();
		m_shipto = dst.Tables["selectpurchase"].Rows[0]["shipto"].ToString();
		m_note = dst.Tables["selectpurchase"].Rows[0]["note"].ToString();
		m_date_create = dst.Tables["selectpurchase"].Rows[0]["date_create"].ToString();
		m_sales_order_id = dst.Tables["selectpurchase"].Rows[0]["sales_order_id"].ToString();
		m_all_in_stock = dst.Tables["selectpurchase"].Rows[0]["all_in_stock"].ToString();
		if (m_all_in_stock == "False")
			m_all_in_stock = "0";
		else
			m_all_in_stock = "1";

		m_currency = dst.Tables["selectpurchase"].Rows[0]["currency"].ToString();
		m_exchange_rate = dst.Tables["selectpurchase"].Rows[0]["exchange_rate"].ToString();
		m_gstrate = dst.Tables["selectpurchase"].Rows[0]["gst_rate"].ToString();
		m_status = dst.Tables["selectpurchase"].Rows[0]["status"].ToString();
	
		string outp = m_po_num +", "+ m_inv_num +", "+ m_branch_id +", "+ m_staff_id +", "+ m_type +", "+ m_supplier_id +", "+ m_buyer_id +", "+ m_totalgst;
//DEBUG("m",outp);
	}

	if (status == "insert")
	{
		//custom gst row vin code
		m_id = Request.Form["m_id"];
		m_po_num = Request.Form["m_po_num"];
		m_inv_num = Request.Form["m_inv_num"];
		m_branch_id = Request.Form["branch"];
		m_staff_id = Request.Form["m_staff_id"];
		m_type = Request.Form["m_type"];
			
		m_supplier_id = Request.Form["supplier_id"];
		m_buyer_id = Request.Form["m_buyer_id"];

		m_totalgst = MyDoubleParse(Request.Form["m_totalgst"]);
		m_totAmt = MyDoubleParse(Request.Form["m_totAmt"]);
		m_total = Request.Form["m_total"];
		m_freight = Request.Form["m_freight"];
		m_total_amount = Request.Form["m_total_amount"];
		m_shipto = Request.Form["shipto"];
		m_date_create = Request.Form["m_date_create"];
		m_sales_order_id = Request.Form["m_sales_order_id"];
		m_all_in_stock = Request.Form["m_all_in_stock"];
		m_currency = Request.Form["m_currency"];
		m_exchange_rate = Request.Form["m_exchange_rate"];
		m_gstrate = Request.Form["m_gstrate"];
		m_status = Request.Form["m_status"];
		m_note = Request.Form["note"];

		//generate GST and update total amount
		string sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO purchase (po_number, inv_number, branch_id, staff_id, type, supplier_id, ";
		sc += " buyer_id, total, tax, freight, total_amount, shipto, note, date_create, sales_order_id, all_in_stock ";
		sc += ", currency, exchange_rate, gst_rate, status, cust_gst, tax_date, date_received, date_invoiced";
		sc += ") VALUES(" + m_po_num + ", '" + m_inv_num + "', " + m_branch_id + ", " + m_staff_id + ", '";
		sc += m_type + "', " + m_supplier_id + ", " + m_buyer_id + ", " + m_total + ", ";
		sc += m_totalgst + ", " + m_freight + ", ";
		sc += m_totAmt + ", '" + m_shipto + "', '" + m_note + "', getdate() , '" + m_sales_order_id + "',";
		sc += m_all_in_stock + ", " + m_currency + ", " + m_exchange_rate + ", " + m_gstrate + ", 1, 1, getdate(), getdate(), getdate()";
		sc += ") ";
		sc += " SELECT IDENT_CURRENT('purchase') AS id ";
		sc += " COMMIT ";
//DEBUG("sc",sc);	
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
		//====================================================================================================================
		//GET id from parent
		
		sc = "SELECT * FROM purchase p WHERE po_number = "+ m_po_num + " ORDER BY p.id desc";	
//DEBUG("sc",sc);	

		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dst, "getpurchaseid") == 0)
				return true;
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		m_getid = dst.Tables["getpurchaseid"].Rows[0]["id"].ToString();

		//====================================================================================================================
		//Insert purchase item
		sc = "INSERT INTO purchase_item (id, code, supplier_code, name, qty, price)";
		sc += " VALUES(" + m_getid + ", '" + m_inv_num + "', '" + "GST" + "', ";
		sc += "'" + "Custom GST" + "', ";
		sc += "1" + ", " +  m_totalgst + ") ";
//DEBUG("sc",sc);	
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
		
		//====================================================================================================================
		//Update parent row refer to the new GST row

		sc = "UPDATE purchase SET cust_gst_pid = "+ m_getid +" WHERE id ="+m_id;
//DEBUG("sc",sc);	
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

string m_id;
string m_po_num;
string m_inv_num;
string m_branch_id;
string m_staff_id;
string m_type;
	
string m_supplier_id;
string m_buyer_id;

string amount;
double m_totalgst;
double m_totAmt;
string m_total = "0";
string m_freight;
string m_total_amount;
//string m_shipto;
//string m_note;
string m_date_create;
string m_sales_order_id="0";
string m_all_in_stock="0";
//string m_currency;
string m_exchange_rate;
//string m_gstrate;
//string m_status;

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
			 DataNavigateUrlFormatString="custgst.aspx?bi={0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=Edit
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="ecard.aspx?id={0}"
			 Text=Edit/>
	</Columns>
</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>

<!-- #include file="purchase_function.cs" -->
<!-- #include file="fifo_f.cs" -->

<script runat="server">

string tableTitle = "";
string m_url = "purchase.aspx?inv="; //this url

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
string m_staffID = "";
double bTotalReceived = 0;
string m_shipto = ""; //optional ship to address (for online orders)
string m_sentNotice = ""; //email has been sent to supplier

string m_currency = "1";
string m_currencyName = "NZD";
string m_exrate = "1";
string m_gstrate = "0";
string m_inv_date = "";
string m_supplierAddres = "";

bool m_bPaid = false;
//bool bIncludeGST = false;
bool m_bNoIndividualPrice = false;
bool m_bOrderCreated = false;
bool m_bsnEntered = false;
bool m_bAlreadySent = false;
bool m_bStockUpdated = false;
bool m_bexistingProd = false;
bool m_bPrintView = false; //if true don't store things to session object
double bAllBranchReceived = 0;
double m_dTotal = 0;
double m_dFreight = 0;
double m_totalReceivedQty = 0;
double m_totalPurchaseQty =0 ;

int m_nSearchReturn = 0;
int m_nCols = 7;
string tableWidth = "97%";
string m_staffName = "";
bool dispatch_list = false;
	
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string[] m_aBranchName = new String[64];
string[] m_aBranchID = new String[64];
int m_nBranches = 1;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
     if(Request.QueryString["da"] == "1")
	      dispatch_list = true; // Listing dispatch details
	//session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		PrepareNewPurchase();
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("purchase.aspx" + par);
		return;
	}
	if(Request.QueryString["full"] == "true")
	{
	 DoReceiveFull(Request.QueryString["kid"],  Request.QueryString["code"],  Request.QueryString["b"]);	
	}
	CheckShoppingCart();
	CheckUserTable();	//get user details if logged on

	if(Request.Form["cmd"] == "New Order" || Request.QueryString["t"] == "new")
	{
		if(!PrepareNewPurchase())
		{
			Response.Write("<br><br><center><h3>Error prepare New Purchase</h3>");
			return;
		}
		m_currency = "1";
		m_exrate = "1.00";
		m_gstrate = (MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100).ToString();		//Modified by NEO
	}
	else
	{
		if(Request.Form["branch"] != null)
			UpdateAllFields(); //store in session object

	}
	//restore values stored in session object
	RestoreAllFields();
	m_currencyName = GetCurrencyName(m_currency).ToUpper();
	if(Request.QueryString["oid"] != null && Request.QueryString["oid"] != "")
	{
		m_url += Request.QueryString["oid"];
		m_orderID = Request.QueryString["oid"];
		if(!GetOrderInfo())
			return;
	}
	m_url += "&ssid=" + m_ssid;
	if(Request.QueryString["n"] != null && Request.QueryString["n"] != "")
		m_url += "&n="+ Request.QueryString["n"].ToString();

	if(Request.Form["cmd"] == "Cancel") //cancel search
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?ssid=" + m_ssid + "\">");
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
		UpdateAllFields();
		if(DoUpdateQuote())
		{
			PrepareNewPurchase();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
		}
		return;
	}
	if(Request.Form["cmd"] == "Change To Bill")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(Request.Form["sup_inv_number"] == "")
	   {  
		Response.Write("<script Language=javascript");
		Response.Write(">\r\n");
		Response.Write("window.alert('Please enter supplier Inv No.')\r\n");
		Response.Write("</script");
		Response.Write(">\r\n ");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?n=" + m_poID + "&r=" + DateTime.Now.ToOADate() + "\">");
		//return false;
     	}
	    else if(DoChangeToBill())
		{
			//DoReceive();
			PrepareNewPurchase();
			Response.Write("<br><br><center><h3>Done, changed to bill</h3>");
			Response.Write("<input type=button onclick=window.location=('default.aspx') value='Main Page' " + Session["button_style"] + ">");
		}
	    else if(MyDoubleParse(Request.Form["totalDueAmout"]) != 0)
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

	//get old order
	if(Request.QueryString["n"] != null && Request.QueryString["n"] != "")
	{
		m_poID = Request.QueryString["n"];
		if(g("a") == "export")
		{
			DoExportPurchase();
			return;
		}
		if(!IsInteger(m_poID))
		{
			Response.Write("<br><br><center><h3>ERROR, WRONG ID</h3>");
			return;
		}
		if(!m_bPrintView)
		{
			Session["purchase_current_po_id" + m_ssid] = m_poID;
		}
		if(Request.Form["branch"] == null) //not post back
		{
			if(!RestorePurchase()) //false means not system quotation
				return;
		}
	}
	//order number

	if(m_poID != "")
		m_bOrderCreated = true;


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
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?ssid=" + m_ssid + "\">");
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
	if((Request.Form["our_code_search"] != null && Request.Form["our_code_search"] != "")) 
		//|| (Request.Form["item_code_search"] != null && Request.Form["item_code_search"] != ""))
	{
		if(!DoSearchItem())
			return;
		if(m_nSearchReturn <= 0)
		{
			//Response.Write("<h3>No code matches <b>" + Request.Form["item_code_search"] + "</b></h3>");
			Response.Write("<tr><td><h3>No code matches <b>" + Request.Form["item_code_search"] + "</b></h3>");
			Response.Write("<tr><td><input type=button value='Back to Purchase' onclick='window.history.back();' "+ Session["button_style"] +">");
			Response.Write("</td></tr></table>");
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
		
			Response.Write("<br><center><h3>Search For Supplier</h3></center>");
			DoSupplierSearch();
		    return;
	}
	else if(Request.Form["cmd"] == "Select From Categories")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx?quick=0&ssid=" + m_ssid + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Undo Delete")
	{
		if(Request.Form["chkundodelete"] == "on")
		{
			if(!DoDeleteOrder(true)) //bUndoDelete = true
				return;
			PrepareNewPurchase();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
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
		if(!DoCreatePurchase()) // "" means new quote
		{
			Response.Write("<h3>ERROR CREATING ORDER</h3>");
		}
		else
		{
			Session["purchase_need_update" + m_ssid] = null;
			PrepareNewPurchase(); //end this session
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Creating Purchase Order ...</h3> &nbsp; Please wait a second ...");
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=purchase.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
		}
		return;
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
					Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=purchase.aspx?n=" + m_poID + "&ssid=" + m_ssid + "\">");
				}
			}
		}
		return;
	}
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
	else if(Request.Form["cmd"] == "Receive Stock")
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
    if(!dispatch_list){
	PrintAdminHeader();
	PrintAdminMenu();
	}
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
	else 
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

	string kw = Request.Form["our_code_search"];
	  
	kw = EncodeQuote(kw);
	string sc = "";
	sc = " SELECT TOP 100 c.code, c.supplier, c.supplier_code, c.name, c.currency ";
	sc += ", c.foreign_supplier_price, ISNULL(c.supplier_price, 0) AS supplier_price ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
 	sc += " WHERE ";
	{

		sc += " c.code = (SELECT TOP 1 b.item_code FROM barcode b WHERE b.barcode ='"+ kw +"')";
		if(TSIsDigit(kw))
		{
//			if(kw.Length < 9)
//				sc += " OR c.code = "+ kw +" ";
			//else
				sc += " OR c.supplier_code = '"+ kw +"'";
                sc += " OR c.barcode = '"+ kw +"'";
		}
		else
        {
			sc += " OR c.supplier_code = '"+ kw +"'";
            sc += " OR c.barcode = '"+ kw +"'";
        }

	}
	sc += "  ORDER BY c.code ";
//DEBUG("sc = ", sc);
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
		if(supplier_price == "0")
			supplier_price = dr["supplier_price"].ToString();
		string currency = dr["currency"].ToString();
		if(!SameSupplier(supplier))
			return false;
		AddToCart(code, supplier, supplier_code, "1", supplier_price, "", name, "");
		Session["purchase_need_update" + m_ssid] = true; //force update order first
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?ssid=" + m_ssid + "\">");
		return false;
	}
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Search Result For </b><font color=red><b>" + kw + "");
	if(m_nSearchReturn == 100)
			Response.Write("top 100 rows returned, display 1-100");
	Response.Write("</td>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
		BindISTable();
	return true;
	}
}

void BindISTable()
{
	Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th>CODE</td>\r\n");
	Response.Write("<th>SUPPLIER</td>\r\n");
	Response.Write("<th width=100>M_PN</td>\r\n");
	Response.Write("<th>DESCRIPTION</td>\r\n");
	Response.Write("<th>SUPPLIER PRICE</td>\r\n");
	Response.Write("</tr>\r\n");

	for(int i=0; i<m_nSearchReturn; i++)
	{
		DataRow dr = dst.Tables["isearch"].Rows[i];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string price = dr["supplier_price"].ToString();
		
		string supplier_price = dr["foreign_supplier_price"].ToString();
		price = "NZ" + MyMoneyParse(price).ToString("c");
		string currency = dr["currency"].ToString();
		string url = "purchase.aspx?ssid=" + m_ssid + "&a=add&code=" + code;
		url += "&supplier=" + HttpUtility.UrlEncode(supplier) + "&supplier_code=" + HttpUtility.UrlEncode(supplier_code);
		if(currency != "")
		{
			url += "&currency=" + currency + "&fsp=" + dr["foreign_supplier_price"].ToString();
			//currency = GetEnumValue("currency", currency).ToUpper();
			currency = GetCurrencyName(currency).ToUpper();
			if(supplier_price == "0" || supplier_price == null || supplier_price == "")
				supplier_price = dr["supplier_price"].ToString();
			price = currency + MyMoneyParse(supplier_price).ToString("c");
		}
		
		Response.Write("<tr>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td><a href=" + url + " class=o>" + supplier_code + "</a></td>");
		Response.Write("<td><a href=" + url + " class=o>" + name + "</a></td>");
		Response.Write("<td>" + price + "</td>");
		Response.Write("</tr>");
	}
//	Response.Write("</table>");
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

	   if(!DoReceive())
	   return false;

	string sup_inv_number = Request.Form["sup_inv_number"];
	
	//DEBUG(" sup_inv_number = ", sup_inv_number);
	

	string inv_date = Request.Form["inv_date"];

	string sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE purchase SET type=" + GetEnumID("receipt_type", "bill");
	sc += ", inv_number='" + EncodeQuote(sup_inv_number) + "' ";
	sc += ", date_invoiced = '" + inv_date + "', tax_date = '" + inv_date + "' ";
	//sc += ", status = 2";

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
	//string sc = "SELECT i.kid, i.code, i.qty, i.price, c.id, p.currency, p.exchange_rate ";	
	string sc = "SELECT i.kid, i.code, i.qty, i.price, c.id, p.currency, p.exchange_rate, p.branch_id, p.freight ";
	sc +=", (SELECT sum(d.qty) FROM dispatch d WHERE d.received =1 and d.id = i.kid) AS receivedQty ";  // Get Received Stock CH 
	sc +=" FROM purchase_item i JOIN purchase p ON p.id=i.id JOIN code_relations c ON c.code=i.code ";	
	sc +=" WHERE  i.id = " + m_poID ;

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
		if(!UpdateAverageCost(dst.Tables["updatebottomprice"].Rows[i], bExistsInProduct, bUpdatePrice, false))
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
		{
			Response.Write("Cannot update");
			return false;
		}
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
	Trim(ref m_billToID);
	string sc = "UPDATE purchase SET ";
	sc += "branch_id=" + m_branchID + ", ";
	if(Request.Form["sup_inv_number"] != null)
		sc += "inv_number='" + m_supInvNumber + "', ";
	sc += "supplier_id=" + m_supplierID + ", ";
	if(m_billToID != null && m_billToID != "")
		sc += "buyer_id='" + m_billToID + "', ";
	sc += "shipto='" + EncodeQuote(m_shipto) + "', ";
	sc += "currency=" + m_currency + ", ";
	sc += "exchange_rate=" + m_exrate + ", ";
	sc += "gst_rate=" + m_gstrate + ", ";
	sc += "note='" + EncodeQuote(m_note) + "' ";
	if(Request.Form["cmd"] == "Update Order")
	{
		sc += ", total=" + dTotalPrice;
		sc += ", tax=" + dTotalTax;
		sc += ", freight=" + m_dFreight;
		sc += ", total_amount=" + m_dTotal;
	}
	sc += " WHERE id=" + m_poID;

/*** insert with new items *****/

	if(Request.Form["cmd"] != "Change To Bill")
	{
//**** delete old items first then insert new ***//
		for(i=0; i<dtCart.Rows.Count; i++)
		{
			string code = dtCart.Rows[i]["code"].ToString();
			string kid = Request.Form["kid" + i];
			if(kid != "")
			    sc += " DELETE FROM dispatch WHERE id="+kid+" AND code ="+code;
		}
		sc += " DELETE FROM purchase_item WHERE id='" + m_poID + "'";
	}
	else
	{
		//update price
		for(i=0; i<dtCart.Rows.Count; i++)
		{
			string code = dtCart.Rows[i]["code"].ToString();
			string kid = Request.Form["kid" + i];
			if(kid == "")
				continue;
			string price = Request.Form["price" + i];
			int rqty = MyIntParse(Request.Form["rqty"+i.ToString()]);
			Trim(ref price);
			if(price == "")
				price = "0";
			price = (MyCurrencyPrice(price)).ToString();
			string qty = dtCart.Rows[i]["quantity"].ToString();
			int nQty = MyIntParse(qty);
			 
			if(MyDoubleParse(m_status) !=1)
			{
				if(nQty != rqty)
				sc +=" UPDATE purchase_item SET price = "+price+", qty="+rqty+", dispatched = 1 WHERE kid ="+kid;
				else
				sc +=" UPDATE purchase_item SET price = "+price+", qty ="+nQty+", dispatched = 1 WHERE kid = " + kid;	
			}
			else if(MyDoubleParse(m_status) == 1)
				sc += " UPDATE purchase_item SET price = "+price+", qty ="+nQty+", dispatched = 1 WHERE kid = " + kid;	
		
		}
	}
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
//Create new purchase item 
	if(Request.Form["cmd"] != "Change To Bill")
	{
		if(!RecordPurchaseItem())
		return false;
	}
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
	string sc = "SELECT i.code, i.qty, i.price, c.id, p.currency, p.exchange_rate, p.branch_id, p.freight ";
	sc +=", (SELECT sum(d.qty) FROM dispatch d WHERE d.received =1 and d.id = i.kid) AS receivedQty ";  // Get Received Stock CH 
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

		////update average price
		if(!UpdateAverageCost(dst.Tables["newstock"].Rows[i], bExistsInProduct, false, true))
			return false;
		if(!UpdateQty(dst.Tables["newstock"].Rows[i], bExistsInProduct))
			return false;
		
	}
	return true;
}

bool UpdateAverageCost(DataRow dr, bool bExistsInProduct, bool bUpdatePrice, bool bForAverageCostOnly)
{
	string code = dr["code"].ToString();
	int qty = MyIntParse(dr["qty"].ToString());
	int receivedQty = MyIntParse(dr["receivedQty"].ToString());
	string cost = dr["price"].ToString();
	string exchange_rate = dr["exchange_rate"].ToString();
	string freight = dr["freight"].ToString();	
   
    if(qty != receivedQty)
	qty = receivedQty;      // Update average cost by received qty  CH
	double dAverageCost = ((MyDoubleParse(cost) / MyDoubleParse(exchange_rate)) * qty) + MyDoubleParse(freight);
	if(qty <= 0)
		return true; //no price update on credits
	
	string currency = dr["currency"].ToString();
	double dexRate = MyDoubleParse(dr["exchange_rate"].ToString());
	double dForeignSupplierPrice = Math.Round(MyDoubleParse(dr["price"].ToString()), 2);
	double dCost = Math.Round(dForeignSupplierPrice / dexRate, 2);
	
	string sc = "SELECT c.id, c.average_cost, c.supplier_price AS last_cost_nzd, c.nzd_freight ";
	sc += " , ISNULL((SELECT SUM(qty) FROM stock_qty WHERE code = c.code AND p.code = code ";
	if(Session["branch_support"] == null) ////set the stock branch when there is no branch support
		sc += " AND branch_id = "+ Session["branch_id"].ToString();
	sc += " HAVING SUM(qty) > 0  ";
	sc += " ),0) AS real_stock ";
	sc += " , p.stock, c.rate ";
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
	double dRate = MyDoubleParse(drp["rate"].ToString());
	double dBottomPrice = Math.Round(dCost * dRate, 2);
	double dCostOld = Math.Round(MyDoubleParse(drp["average_cost"].ToString()), 2);
	if(dCostOld == 0)
		dCostOld = Math.Round(MyDoubleParse(drp["last_cost_nzd"].ToString()), 2);
	int realStock = MyIntParse(drp["real_stock"].ToString());
	double dLastAverageCost = dCostOld * realStock;

	double qtyOld = 0;
	if(drp["stock"].ToString() != "") 
		qtyOld = MyIntParse(drp["stock"].ToString());
	if(drp["real_stock"].ToString() != "") 
		qtyOld = MyIntParse(drp["real_stock"].ToString());
	//in case stock is wrong
	if(qtyOld < 0)
		qtyOld = 0;
	double dAverageCostNew = 0;
	if((MyIntParse(drp["real_stock"].ToString()) + qty) == 0)
		dAverageCostNew = Math.Round(dAverageCost, 2);
	else
		dAverageCostNew = Math.Round((dLastAverageCost + dAverageCost) / (MyIntParse(drp["real_stock"].ToString()) + qty), 2);
	if(dAverageCostNew <0)
		dAverageCostNew = 1 - dAverageCostNew;
	if(bForAverageCostOnly && Session["branch_support"] == null)
	{
		sc = "UPDATE code_relations SET ";
		sc += " average_cost = "+ dAverageCostNew;
		sc += " WHERE code=" + code;	
	}
	else
	{
		sc = "UPDATE code_relations SET currency=" + currency + ", exchange_rate=" + dexRate;
		sc += ", foreign_supplier_price=" + dForeignSupplierPrice + " "; //, average_cost=" + dAverageCostNew;
		sc += ", supplier_price=" + dCost;
		//sc += ", manual_cost_frd=" + dForeignSupplierPrice;
		sc += ", manual_exchange_rate=" + dexRate;
		//sc += ", manual_cost_nzd=" + dCost;		
		sc += ", nzd_freight= "+ dFreight;
		sc += " WHERE code=" + code;
		sc += " IF (SELECT supplier_price FROM product WHERE code = "+ code +") = 0 ";
		sc += " BEGIN UPDATE product SET supplier_price = "+ dForeignSupplierPrice +"";
		sc += " WHERE code = "+ code;
		sc += " END ";
	}
	if(bForAverageCostOnly)
		sc += AddAVGCostLog(code, "Purchase Item: average cost had been changed", dCostOld.ToString(), dAverageCostNew.ToString(), m_poID.ToString());
	if(bUpdatePrice && !bForAverageCostOnly)
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
//	Session["purchase_supplierid" + m_ssid] = Request.Form["supplier_id"];
	Session["purchase_supplierid" + m_ssid] = Request.Form["supplier"];
	UpdateSalesPrice();

	if(Request.Form["exrate"] != Request.Form["exrate_old"])
	{	
		m_currencyName = GetCurrencyName(Request.Form["currency"]).ToUpper();				
		
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
			//string name = Request.Form["name"+i.ToString()];
            string name = Request.Form["desc"+i.ToString()];
			string supplier = Request.Form["supplier"+i.ToString()];
			string supplier_code = Request.Form["supplier_code"+i.ToString()];
			string sqty = Request.Form["qty"+i.ToString()];
			string sprice = Request.Form["price"+i.ToString()];
            //string sprice = Request.Form["price"+i.ToString()];
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
	
	tableTitle = "PURCHASE <font color=red>";
	if(m_bOrderCreated)
		tableTitle += GetEnumValue("receipt_type", m_quoteType).ToUpper() + " #" + m_poNumber;
	else
		tableTitle += "ORDER (new)";
	tableTitle += "</font>";
    
	Response.Write("<form name=form1 action='" + m_url + "' method=post>");
	Response.Write("<input type=hidden name=type value=" + m_quoteType + ">");
    if(!dispatch_list){
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>" + tableTitle + "</font><b>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
}

	//print sales header table
	if(!dispatch_list)
	if(!PrintPurchaseHeaderTable())
		return false;
//	Response.Write("<table class=d align=center valign=center cellspacing=1 cellpadding=0 border=1>");
   if(!dispatch_list){
	Response.Write("<table width='"+ tableWidth +"' align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	}
	Response.Write("<tr><td colspan=5>");
    if(!dispatch_list)
	Response.Write(PrintShipToTable());

	Response.Write("</td></tr>");
	Response.Write("<input type=hidden name=exchange_rate_" + GetEnumID("currency", "nzd") + " value=1>");
	Response.Write("<input type=hidden name=exchange_rate_" + GetEnumID("currency", "usd") + " value=" + GetSiteSettings("exchange_rate_usd", "0.49") + ">");
	Response.Write("<input type=hidden name=exchange_rate_"  + GetEnumID("currency", "aud") + " value=" + GetSiteSettings("exchange_rate_aud", "0.87") + ">");

	Response.Write("<tr><td colspan=5>");
     if(!dispatch_list)
	if(!PrintCartItemTable())
		return false;

	Response.Write("</td></tr></table>");

	Response.Write("<input type=hidden name=order_id value=" + m_orderID + ">");
	Response.Write("</form>");

	PrintJavaFunction();

	if(m_bOrderCreated)
	{
//		Response.Write(PrintDirectOrderFrom());
		if(Session["branch_support"] != null)
		DrawDispatchTable();
	}

	return bRet;
	
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
}


bool DrawDispatchTable()
{
	GetPurchaseItems();
//	Response.Write("<center><h4>Dispatch Information</h4>");
    Response.Write("<link rel='stylesheet' href='../print.css' type='text/css' media='print' >"); 
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=" + (m_nBranches+3) + "><font color=#80db80><b>Dispatch Information</b></font></td></tr>");

	Response.Write("<tr style='background-color:#80db80;color:#EEEEE'>");
	Response.Write("<th>ITEM CODE</th>");
	Response.Write("<th width=300>DESCRIPTION</th>");
	Response.Write("<th width=55>QTY</th>");
	for(int i=0; i<m_nBranches; i++)
		Response.Write("<th> &nbsp; " + m_aBranchName[i] + " &nbsp;</th>");
	 
	Response.Write("</tr>");

	bool bDispatched = false;
	bool bAllDispatched = true;
	m_nItems = dst.Tables["items"].Rows.Count;
	for(int i=0; i<dst.Tables["items"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["items"].Rows[i];
		string kid = dr["kid"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		bDispatched = MyBooleanParse(dr["dispatched"].ToString());
		string sdispatched = "0";
		if(bDispatched)
			sdispatched = "1";
		else
			bAllDispatched = false;
		 if(m_bOrderCreated)
		{
			for(int e=0; e<m_nBranches; e++)
			{
				string AllBranchReceivedQty = GetTotalReceivedQty(kid, code);
			    bTotalReceived = MyDoubleParse(AllBranchReceivedQty );
			  
			}
		}
      
	    string m_statusTitle =" Recieived ";
	    if(bTotalReceived < 0 )
		  m_statusTitle = " Returned";
		
		
		Response.Write("<input type=hidden name=code" + i + " value=" + code + ">");
		Response.Write("<input type=hidden name=total" + i + " value=" + qty + ">");
		Response.Write("<input type=hidden name=dispatched" + i + " value=" + sdispatched + ">");
               
		Response.Write("<tr><td align=center>" + code + "</td><td>" + name + "</td><td align=center title='Total Ordered: "+ qty +" /  Total "+m_statusTitle+" : "+ bTotalReceived+"'>" + qty + " / " +bTotalReceived.ToString()+"</td>");
		for(int m=0; m<m_nBranches; m++)
		{
			int dqty = 0;
			string qid = kid + m_aBranchID[m];
			if(Session["dispatch" + qid] != null && Session["dispatch" + qid] != "")
				dqty = MyIntParse(Session["dispatch" + qid].ToString());
//			if(!bDispatched)
//				Response.Write("<input type=text style=text-align:right name=qty" + qid + " size=1 value=" + dqty + ">");
//			else
			{
				string dispatched_qty = GetDispatchedQty(kid, m_aBranchID[m]);
				string received_qty = GetReceivedQty(kid, m_aBranchID[m]);
				string v_qty = GetVisaulReceivedQty(kid, m_aBranchID[m]);
				double left_qty = MyDoubleParse(dispatched_qty) - MyDoubleParse(received_qty);
				string sq = dispatched_qty;// + "/" + received_qty;
				string stitle = "Stock Receive Information";
				       stitle +="\r\n\r\n Dispatched: " + dispatched_qty + "\r\n" + m_statusTitle +" : " + received_qty;
				       if(v_qty != dispatched_qty)
				       stitle += "\r\n Left: ";
				       else
				       stitle += " \r\nPurchase Order Loss:";
				       stitle +=left_qty;
				       stitle +=" \r\nCurrent Status: ";
				       if(received_qty =="0" && v_qty != dispatched_qty)
				       stitle += "Dispatched ";
				       else if(left_qty > 0 && received_qty !="0" && v_qty != dispatched_qty)
				       stitle += " Receiving ";
				       else if(v_qty == dispatched_qty && MyDoubleParse(received_qty) > 0)
				       stitle += " Received ";
				       else if (received_qty == "0" && v_qty ==dispatched_qty)
				       stitle += " Cancel";
					   else if(MyDoubleParse(received_qty) < 0 && v_qty == dispatched_qty)
					   stitle += "Returned";
				double receivedAll = MyDoubleParse(v_qty);
				bAllBranchReceived += receivedAll;
				if(sq == "0")
				{
					sq = "";
					stitle = "no dispatch";
				}
				else
				{
					sq = dispatched_qty + "/" + received_qty;
					if(left_qty > 0 && received_qty != "0")
					{
						
						sq += "/";
						if(bAllDispatched)
						sq +="<font color=red>";
						sq +=left_qty;
						sq +="</font>";
					}
					
					
					
				}

			    

				Response.Write("<td align=center title='" + stitle + "'>");
				Response.Write(sq);
				

			}

			Response.Write("</td>");
			Response.Write("<input type=hidden name=qid" + i + m + " value=qty" + qid + ">");
		}
		Session ["AllBranchReceived"] = bAllBranchReceived;

		Response.Write("</tr>");
	}
	if(dispatch_list){
	Response.Write("<tr><td colspan=" + (m_nBranches+3) + " align=right class='print'><input type=button value='&nbsp;Print &nbsp;' onClick=\"window.print()\"><input type=button");
	Response.Write(" value='&nbsp; Close Window &nbsp;' onClick=\"window.close()\"></td></tr>");
 }
	Response.Write("</table>");

	return true;
}




bool PrintCartItemTable()
{
	int i = 0;
	double m_rCol = 2;
	m_currencyName = GetCurrencyName(m_currency);
	BuildBranchIndex();
	Response.Write("</td></tr></table>");
	Response.Write("<table align=center border=1 cellpadding=0 width='"+ tableWidth +"' cellspacing=0 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=20% >STOCK ID/BARCODE</td>");
    Response.Write("<td width=10% >CODE</td>");  
	Response.Write("<td width=10% >M_PN</td>");
	Response.Write("<td width=35%>DESCRIPTION</td>");
	Response.Write("<td width=20% align=right>PRICE(" + m_currencyName.ToUpper() + ")</td>");
	Response.Write("<td width=19% align=right>QTY</td>");
	if(m_bOrderCreated)
   {
	 Response.Write("<td width=19% align=right>Received</td>");
	 m_rCol = 3;
	 m_nCols = 8;
	}
	Response.Write("<td width=19% align=right>Sub Total</td>");
		
	dTotalPrice = 0;
	dTotalGST = 0;
	dAmount = 0;
	dTotalSaving = 0;

	double dRowPrice = 0;
	double dRowGST = 0;
	double dRowTotal = 0;
	double dRowSaving = 0;
	double cRows =0;
	       cRows = dtCart.Rows.Count;

	//build up row list
	for(i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;

		DataRow drp = null;
		DataRow dr = dtCart.Rows[i];
		string kid = dr["kid"].ToString();
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		int quantity = MyIntParse(dr["quantity"].ToString());
		
		
	
		double dPrice = 0;
		if(dr["supplierPrice"].ToString() != "")
			dPrice = Math.Round(MyDoubleParse(dr["supplierPrice"].ToString()), 2);

		
		
        if(m_bOrderCreated && kid != "" )
		{
			for(int e=0; e<m_nBranches; e++)
			{
				string AllBranchReceived_qty = GetTotalReceivedQty(kid, code);
				bAllBranchReceived = MyDoubleParse(AllBranchReceived_qty );		
				
			}
		}

		SSPrintOneRow(kid, i, code, barcode, supplier, supplier_code, name, dPrice, quantity, bAllBranchReceived, dRowTotal);
		
         if(MyDoubleParse(m_status) !=1  && MyDoubleParse(m_status) !=3)
			quantity= MyIntParse(bAllBranchReceived.ToString());   
	     
         dRowTotal = Math.Round(dPrice * quantity, 2);
		 dTotalPrice += dRowTotal;
		 dAmount += dRowTotal;

	}
		 
 
	dTotalPrice = dTotalPrice;// + m_dFreight;
	Response.Write("<tr><td colspan=5>");
	if( (m_status == "1" )) //&& !m_bOrderCreated) //on order or back order
	{
		
		Response.Write("<input type=text size=10 name=our_code_search>");
		Response.Write("<script");
		Response.Write(">");
		Response.Write("document.form1.our_code_search.focus();");
		Response.Write("</script");
		Response.Write(">");
		
		Response.Write("<input type=submit name=cmd " + Session["button_style"] + " size=8 value='Select From Categories'>");
		Response.Write("</td>");
//		Response.Write("<td>&nbsp;</td>");
		//**********this also support supplier code searching also
//		Response.Write("<td colspan=2 valign=\"middle\"></td>"); //<input type=text size=10 name=item_code_search></td>");
		Response.Write("<td colspan=3 align=right><input type=submit name=cmd value='Recalculate Price' " + Session["button_style"] + ">"); //</td>");
		
		//Response.Write("<tr><td><input type=submit name=cmd " + Session["button_style"] + " size=8 value='Select From Categories'></td></tr>");
	}
	if( (m_status != "1" ))
	Response.Write("<td colspan=2 align=right><input type=submit name=cmd value='Recalculate Price' " + Session["button_style"] + ">"); 
	Response.Write("</td></tr>");
	dTotalPrice = Math.Round(dTotalPrice, 2);

	double dFinal = dTotalPrice + m_dFreight;
	if(Request.Form["total"] != null && Request.Form["total"] != "")
	{
		if(Request.Form["total"] != Request.Form["total_old"])
			dFinal = MyMoneyParse(Request.Form["total"]);
	}
	dAmount = dFinal;
	double dGstRate = MyDoubleParse(m_gstrate);
	dTotalGST = Math.Round(dAmount * dGstRate, 2);
	dAmount *= (1 + dGstRate);
	Response.Write("<tr bgcolor=#EEE999><td colspan=" + m_nCols  + ">&nbsp;</td>");
	Response.Write("<tr bgcolor=#EEEEE><td rowspan=5 colspan=4>");

	//start comment table
	Response.Write("<table>");
	Response.Write("<input type=hidden name=purchase_order_qty value ="+  m_totalPurchaseQty +" >");
	Response.Write("<input type=hidden name=receive_order_qty value ="+  m_totalReceivedQty +" >");
	Response.Write("<tr><td><br> &nbsp;&nbsp;<b>Comment : </b></td></tr>");
	Response.Write("<tr ><td> &nbsp;&nbsp;<textarea name=note cols=70 rows=5>" + m_note +"</textarea><br><br></td>");
	Response.Write("</tr></table>");
	//end comment table

	Response.Write("</td></tr>");

	//start price talbe
	Response.Write("<tr bgcolor=#EEEEE><td valign=top colspan="+m_rCol +" align=right><b>SubTotal(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write(dTotalPrice.ToString("c"));
	Response.Write("</b></td></tr>");
	Response.Write("<input type=hidden name=rowSubTotal value="+dTotalPrice.ToString()+" >");

	Response.Write("<tr bgcolor=#EEEEE><td colspan="+m_rCol +"  align=right><b>Freight(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=text size=5 style='text-align:right' align=right name=freight value='" + m_dFreight.ToString("c") + "'");
	Response.Write(" onkeyup =\" document.form1.dReCalTax.value = (Number(document.form1.rowSubTotal.value) + Number(this.value))*0.125; document.form1.totalAmount.value = Number(document.form1.rowSubTotal.value)+Number(document.form1.dReCalTax.value)+Number(this.value);\"");
	Response.Write("></td></tr>");

	//total GST
	Response.Write("<tr bgcolor=#EEEEE><td colspan="+m_rCol +"  align=right><b>TAX(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write("<input type=text name=dReCalTax readonly value='");
	Response.Write(dTotalGST.ToString("c"));
	Response.Write("'  size=5 style=\"border=none; text-align=right; background=#EEEEEE\"></b></td></tr>");
	Response.Write("<input type=hidden name=totalTax value="+dTotalGST.ToString()+" >");

	//total amount due
	Response.Write("<tr bgcolor=#EEEEE><td colspan="+m_rCol +"  align=right><b>Total Amount(" + m_currencyName + ") : </b></td>");
	Response.Write("<td align=right><b>");
	Response.Write("<input type=text readonly name=totalAmount value='");
	Response.Write(dAmount.ToString("c"));
	Response.Write("' size=5 style=\"border=none; text-align=right; background=#EEEEEE\"></b></td></tr>");
	Response.Write("<input type=hidden name=totalDueAmout value="+dAmount.ToString()+" >");
	//end comment and price table

	Response.Write("</td></tr>");

	Response.Write("<tr bgcolor=#EEEEE><td colspan=8 align=right>");
	if(m_bOrderCreated)
	{
		Response.Write("<input type=button class=b value='Export to Excel' onclick=\"window.location='?a=export&n=" + m_poID + "';\"> &nbsp; ");
		Response.Write("<input type=checkbox name=chkdelete> Delete this record ");
		Response.Write("&nbsp;&nbsp;<input type=submit name=cmd value='Delete' " + Session["button_style"] + ">");
		if((m_quoteType == "1" || m_quoteType == "2") && m_status == "4")// 4 = GetEnumID("purchase_order_status", "deleted"))
		{
			Response.Write("<input type=checkbox name=chkundodelete>Undo Delete ");
			Response.Write("<input type=submit name=cmd value='Undo Delete' " + Session["button_style"] + ">");
		}
	
		if(m_quoteType != GetEnumID("receipt_type", "bill") && m_status !="1"  )
		{
		//supplier invoice number
			Response.Write("<b>Supplier Inv Date. : </b>");
			Response.Write("<input type=text name=inv_date size=10 value='" + DateTime.Now.ToString("dd-MM-yyyy") + "'> ");
			Response.Write(" &nbsp&nbsp; <b>Supplier Inv No. : </b>");
			Response.Write("<input type=text name=sup_inv_number size=10 style='text-align:right' value='" + m_supInvNumber + "'> ");
			Response.Write("<input type=submit name=cmd value='Change To Bill' " + Session["button_style"] + ">");
			if(Session["branch_support"] != null && m_status =="5")
			Response.Write("<input type=button name=cmd value='Receive Stock QTY' onclick=\"window.location=('dlist.aspx');\" " + Session["button_style"] + ">");				
		}
		if(m_status =="1")
        {
		    Response.Write("<input type=button value='Receive Stock' onclick=window.location=('dlist.aspx?branch=1&pid=" + m_poNumber);
			Response.Write("')  "+ Session["button_style"] +" >");
            Response.Write("<input type=submit name=cmd value='Update Order' " + Session["button_style"] + ">");
        }
		if(Session["branch_support"] != null)
		{
			if(m_status =="1" || m_status =="5" )
			{
			    Response.Write("<input type=button value='Dispatch To Branch' onclick=window.location=('dispatch.aspx?n=" + m_poID);
			    Response.Write("')  "+ Session["button_style"] +" >");
			}
		Response.Write("<input type=button onclick=window.open('purchase.aspx?t=pp&n=" + m_poID + "&ssid=" + m_ssid + "') value='Print Preview' " + Session["button_style"] + ">");
		}
	}
	else
	{
		Response.Write("<input type=submit name=cmd value='Create Order' " + Session["button_style"] + " onclick=\"if(document.form1.supplier.value == '0' || document.form1.supplier.value== '' || document.form1.supplier.value== 'Please Select'){window.alert('Please Select a Supplier'); return false; }\">");
	}
	
	if(m_bOrderCreated)
	{
	//	Response.Write("<tr bgcolor=#EEEEE align=right><td colspan="+m_nCols +">");
		if(m_bAlreadySent)
			Response.Write(" <i><font color=red>( " + m_sentNotice + " )</i></font>");
		Response.Write("<input type=checkbox name=send_confirm>");
		Response.Write("Email Order : <input type=text name=email value='" + m_supplierEmail + "'> ");
		Response.Write("<input type=submit name=cmd value=Send " + Session["button_style"] + ">");
		Response.Write("</td></tr>");
	 }

	Response.Write("</table>");
	Response.Write("<input type=hidden name=supplierID value='" + m_supplierID + "'>");
	return true;
}

bool SSPrintOneRow(string kid, int nRow, string code, string barcode, string supplier, string supplier_code, string desc, double dPrice, int qty, double received_qty,  double dTotal)
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

//barcode

    Response.Write("<td width=10%>"); 
	Response.Write("<a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
	Response.Write(barcode);
	Response.Write("</a> ");
    //	if(CheckPatent("viewsales"))
	{
		Response.Write("<input type=button title='Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
		Response.Write("code=" + code + "','','width=600,height=300, resizable=1, scrollbars=1');\" value='S' " + Session["button_style"] + ">");

		Response.Write("<input type=button title='Purchase History' onclick=\"window.open('viewpurchase.aspx?");
		Response.Write("code=" + code + "', '', 'resizable=1, scrollbars=1')\" value='P' " + Session["button_style"] + ">");
	}
	Response.Write("</td>\r\n");


	//code
	Response.Write("<td width=10%>");     //kevin item code
	Response.Write("<input type=hidden size=7 name=code" + nRow.ToString() + " value=");
    Response.Write(code);
    Response.Write(">");
    Response.Write("<a title='click here to view Sales Ref:' href='salesref.aspx?code=" + code +"' class=o target=_new>");
	Response.Write(code);
    Response.Write("</a> ");
//	if(m_status != "1")
//		Response.Write(" readonly=true ");
	
    Response.Write("</td>\r\n");
	//supplier code
	Response.Write("<td width=20%>");
	Response.Write("<input type=hidden size=20 name=supplier_code" + nRow.ToString());
	if(m_status != "1")
		Response.Write(" readonly=true ");
	Response.Write(" value='");
	Response.Write(supplier_code);
	Response.Write("'>" + supplier_code + "</td>\r\n");

	//description
	Response.Write("<td >");
	Response.Write("<input type=hidden size=50 name=name" + nRow.ToString() + " value='");
	Response.Write(desc);
	Response.Write("'><textarea name='desc"+ nRow.ToString() +"' cols='45' rows='1'>" + desc + "</textarea></td>\r\n");

	//price
	Response.Write("<td width=10% align=right>");
	Response.Write("<input type=text size=7 style='text-align:right' name=price" + nRow.ToString() + " value='");
	Response.Write(dPrice.ToString());
	if(m_bOrderCreated)
	Response.Write("' onkeyup =\" document.form1.rowTotal"+nRow.ToString()+".value = this.value*Number(document.form1.rqty"+nRow.ToString()+".value); calTotal()\"></td>\r\n");
	else
	Response.Write("' onkeyup =\" document.form1.rowTotal"+nRow.ToString()+".value = this.value*Number(document.form1.qty"+nRow.ToString()+".value); calTotal()\"></td>\r\n");
	//quantity
	Response.Write("<td  width=20% align=right nowrap><input type=text size=3 style='text-align:right' name=qty" + nRow.ToString());
	if(m_status != "1" && m_status != "3") // 1: order placed 3: on backorder
		Response.Write(" readonly=true ");
	Response.Write(" value='" + qty.ToString() + "' onkeyup=\"document.form1.rowTotal" + nRow.ToString()+".value = Number(this.value)*Number(document.form1.price" + nRow.ToString()+".value);\">");
	m_totalPurchaseQty +=qty;
	//remove button
	if(m_status != "2")
		Response.Write("<input type=submit name=del" + nRow + " " + Session["button_style"] + " value=X>");
	Response.Write("<input type=hidden name=qty_old" + nRow.ToString() + " value='" + qty.ToString() + "'></td>\r\n");
	if(m_bOrderCreated)
	{
		//received quantity
		Response.Write("<td align=right>");
		Response.Write("<input type=text size=5 style=\"text-align=right\" name=rqty" + nRow.ToString());
		Response.Write(" value='" + received_qty+ "'>");
		Response.Write("</td>\r\n"); 
	    m_totalReceivedQty += received_qty;
	
	}
    if(!m_bOrderCreated)
	   received_qty = qty;
	 
	Response.Write("<td align=right>");
	Response.Write("<input type=text name=rowTotal" + nRow.ToString()+"  size=5 style=\"text-align=right\" value="+(dPrice* received_qty).ToString("c")+" >");
	Response.Write("</td>\r\n</tr>\r\n");

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
		return "<font color=red><h5>Error, cannot get bill to information<br>Please set the right value of card_id_for_purchase_bill_to in <a href=setting.aspx class=o>Site Settings</a><h5>";
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

	sb.Append("<table border=1 width='100%' align=center ");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	if(m_shipto == "")
	{
		m_shipto += dr["trading_name"].ToString() + "\r\n";
		m_shipto += dr["address1"].ToString() + "\r\n"; 
		m_shipto += dr["address2"].ToString() + "\r\n"; 
		m_shipto += dr["address3"].ToString() + "\r\n"; 
		m_shipto += dr["country"].ToString() + "\r\n"; 
	}
	sb.Append("<tr><td valign=top><b>Ship To : &nbsp;</b></td><td>");
	if(bWithHeader)
		sb.Append("<textarea name=shipto cols=30 rows=5>" + m_shipto + "</textarea>");
	else
		sb.Append(m_shipto.Replace("\r\n", "<br>"));
	sb.Append("</td>");
	//bill to
	sb.Append("<td width=50% valign=top>");
	sb.Append("<b>Bill To : &nbsp;</b>");//</td><td valign=top>");
	sb.Append("<table><tr><td>&nbsp;&nbsp;</td><td>");
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
	Response.Write("<table width='"+ tableWidth +"' align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
Response.Write("<tr><td colspan=3><br></td></tr>");
	Response.Write("<tr><td valign=top>");
	
	//branch & staff & date
	
	Response.Write("<table><tr>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<tr><td><b>Branch : </b></td><td>");
		if(!PrintBranchNameOptions())
			return false;
		Response.Write("</tr>");
	}
	else
		Response.Write("<input type=hidden name=branch value=1>");
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
	if(m_bOrderCreated)
	{
		Response.Write("<b>" + m_poNumber + "</b>");
		Response.Write("<input type=hidden name=po_number value='" + m_poNumber + "'>");
		Response.Write("<input type=hidden name=po_id value=" + m_poID + ">");
	}
	else
	{
		//get nextx po_number
		if(m_poNumber == "")
		{
			int nNumber = 0;
			GetNextPONumber(ref nNumber);
			m_poNumber = nNumber.ToString();
		}
//		Response.Write("<font size=+1>" + m_poNumber + "</font>");
		Response.Write("<input type=hidden name=number value='" + m_poNumber + "'>");
	}
	Response.Write("<input type=hidden name=number_old value='" + m_poNumber + "'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td align=right><b>Supplier Inv No. : </b></td><td><b>" + m_supInvNumber + "</b></td></tr>");

	Response.Write("</table>");
	Response.Write("</td><td width=50% valign=top align=right>");
	Response.Write("<table>");

	////////////////////////////////////////////////////////////////////////////
	//order status table
	//Supplier
	Response.Write("<tr><td align=right><b>Supplier : </b></td><td>");
		Response.Write(PrintSupplierOptions(m_supplierID, ""+ m_url +"&si=", "supplier"));
		string luri = Request.ServerVariables["URL"] + "?"+ Request.ServerVariables["QUERY_STRING"];
		Response.Write("</td><td><input type=button onclick=window.location=('ecard.aspx?a=new&n=supplier&luri="+ HttpUtility.UrlEncode(luri) +"') value='New Supplier' "+ Session["button_style"] +">");
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right><b>GST-Rate : </b></td><td>" + m_gstrate + "</td><td>");
	Response.Write("<input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
	Response.Write("id=" + m_supplierID + "','',' width=350,height=350');\" value='View Supplier' " + Session["button_style"] + ">&nbsp;");
	Response.Write("</td></tr>");
	//Response.Write("<tr><td align=right><b>Currency : </b></td><td>" + GetEnumValue("currency", m_currency).ToUpper() + "</td></tr>");

	Response.Write("<tr><td align=right><b>Currency : </b></td><td><select name=currency disabled>" + PrintCurrencyOptions(false, m_currency).ToUpper() + "</select></td></tr>");
	Response.Write("<input type=hidden name=currency value=" + m_currency + ">");
	Response.Write("<tr><td align=right><b>Exchange-Rate : </b></td>");
	Response.Write("<td><input type=text name=exrate ");
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
		Response.Write("<br><br><center><h3>Error, Order already billed, cannot update</h3>");
		return false;
	}
	return true;
}


bool DoReceive()
{
	//if(!CheckOrderForReceive())
	//	return false;

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
    double totalRQTY = MyDoubleParse(Request.Form["receive_order_qty"]);
	double totalPQTY = MyDoubleParse(Request.Form["purchase_order_qty"]);
   	if(totalRQTY < totalPQTY)
	{
			if(!DoBackOrder())
				return false;	
	}


	double dReceivedTotal = 0;
	double dRemainTotal = 0;
	dtCart = (DataTable)Session["ShoppingCart" + m_ssid];
	for(int i=0 ; i< rows ; i++)
	{
		string code = Request.Form["code"+i.ToString()];
		string name = Request.Form["name"+i.ToString()];
		string supplier_code = Request.Form["supplier_code"+i.ToString()];
		string sprice = Request.Form["price"+i.ToString()];
		double dPrice = Math.Round(MyMoneyParse(sprice), 2);
		int qty = MyIntParse(Request.Form["qty"+i.ToString()]);
		int rqty = MyIntParse(Request.Form["rqty"+i.ToString()]);
		string kid = Request.Form["kid"+i.ToString()];
		double remain_qty =MyDoubleParse(qty.ToString()) - MyDoubleParse(rqty.ToString());
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
		if(rqty < qty) //split remaining to a new order with same po_number
		{
			sc += " INSERT INTO purchase_item (id, code, supplier_code, name, qty, price)";
			sc += " VALUES(" + m_poIDNew + ", " + code + ", '" + EncodeQuote(supplier_code) + "', ";
			sc += "'" + EncodeQuote(name) + "', ";
			sc += (qty-rqty).ToString() + ", " + dPrice + ") ";
            sc += " UPDATE dispatch SET id = (SELECT kid FROM purchase_item WHERE id="+m_poIDNew+" AND code="+code+") WHERE received =0 AND code ="+code;
		}
  
			sc +=" INSERT INTO purchase_order (id, code, oqty, po_number, rqty, branchID)";
			sc += " VALUES("+m_poID+","+code+", "+qty+",'',"+rqty+","+m_branchID+")";
 
	}

	string freight = Request.Form["freight"];
	double dFreight = Math.Round(MyMoneyParse(freight), 2);
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
	
	//UpdateStockQty(); //Avoil double up stock qty when change to bill 
	return true;
}

bool DoBackOrder()
{
	
	string sd = "GETDATE()";
	string getoqty = GetOriginalQty(m_poNumber);
	if(m_sdate == "")
	{
		if(Session[m_sCompanyName + "purchase_create_date"] != null)
		{
			m_sdate = Session[m_sCompanyName + "purchase_create_date"].ToString();
			sd = "'" + m_sdate + "'";
		}
	}
	double getOldOrderTotal = MyDoubleParse(GetPOrderTotal(m_poID));
    double invSupInvTotal = MyDoubleParse(Request.Form["totalDueAmout"]);
	double orderTotalWithoutGST = MyDoubleParse(Request.Form["rowSubTotal"]);
	double totalGSTPurchaseOrder = MyDoubleParse(Request.Form["totalTax"]);
	m_dTotal = getOldOrderTotal - invSupInvTotal;
	string status_backorder = GetEnumID("purchase_order_status", "on backorder");
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO purchase (status, po_number, branch_id, staff_id, type, supplier_id, ";
	sc += " buyer_id, freight, total_amount, shipto, date_create, sales_order_id, currency, exchange_rate, gst_rate, note, total, tax ";
	sc += ") VALUES(" + status_backorder + ", " + m_poNumber + ", " + m_branchID + ", " + m_staffID + ", '";
	sc += GetEnumID("receipt_type", "order") + "', " + m_supplierID + ", " + m_billToID + ", " + m_dFreight + ", ";
	sc += m_dTotal+", '" + EncodeQuote(m_shipto) + "',  " + sd + ", '" + m_orderID + "'";
	sc += ", " + m_currency + ", " + m_exrate + ", " + m_gstrate;
	if(m_note =="" )
	sc += ", 'Back order for "+ m_poNumber + " '";
	else
	sc += ",'" + m_note +"'";
    sc +=" , "+ orderTotalWithoutGST +", "+totalGSTPurchaseOrder;
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
	if(dtCart.Rows.Count <= 0)
	{
		return false;
	}
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
	
	if(!m_bStockUpdated && m_quoteType == "4")
	{
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
	string sc = "INSERT INTO purchase_item (id, code, name, supplier_code, qty, price, dispatched) ";
	sc += " VALUES(" + m_poID + ", '" + code + "', N'" + name + "', '";
	sc += supplier_code + "', " + sQty + ", " + dPrice; 
	if(Session["branch_support"] !=null)
	sc += ",0)";
	else 
	sc += ",1)";  
	 
	string getHeadBranchID = GetSiteSettings("head_office_branch_id", "1", true);
	if(Session["branch_support"] == null)
	{ 
		sc += " INSERT INTO dispatch (branch, id, code, name, qty, record_date, received) ";
		sc += " SELECT '"+getHeadBranchID+"', pi.kid, pi.code, pi.name, pi.qty, GETDATE(), 0";
		sc += " FROM purchase_item pi ";
		sc += " WHERE pi.id = "+ m_poID;
		sc += " AND pi.code = "+ code;
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
	m_currencyName = GetCurrencyName(m_currency);
	m_exrate = GetCurrencyRate(m_currency);
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
	string sc = "";
	string type_supplier = GetEnumID("card_type", "supplier");
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%#@#@#@#@#@#@%'";
	sc = "SELECT '<a href=purchase.aspx?si=' + convert(varchar, id) + '&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
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
		sc = "SELECT '<a href=purchase.aspx?si=' + convert(varchar, id) + '&ssid=" + m_ssid + " class=o>' + convert(varchar, id) + '</a>' AS 'Pick' ";
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

string BuildOrderInvoice(string sType)
{
	StringBuilder sb = new StringBuilder();
       
		sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	sb.Append("<link rel='stylesheet' href='../print.css' type='text/css' media='print' >"); 
	sb.Append("<body>\r\n");

	sb.Append("<b>");

	sb.Append("</b>");

	string type = GetEnumValue("receipt_type", sType);
	
	if(type.IndexOf("bill") < 0)
		type = "purchase "+ type;
	string sPurchaseHeader = ReadSitePage("purchase_header");
	string sPurchaseFooter = ReadSitePage("purchase_footer");
	
	string sInfo = "<!-- ******** Variable to use ********* -->";
    sInfo += "<!-- @@PO_NUMBER, @@PURCHASE_TYPE, @@TODAY_DATE, @@SUPPLIER_ID, @@SUPPLIER_NAME, @@SUPPLIER_NO, @@INVOICE_DATE, @@PO_NUMBER --> ";
	sInfo += "<!-- *** Shipping/Billing Address *** @@COMPANY_NAME, @@TRADING_NAME,@@NAME, @@CONTACT, @@ADDR1,  @@ADDR2,@ADDR3,@@COUNTRY,@@PHONE,@@FAX,@@EMAIL,@@COMMENTS,@@STAFF, @@SHIPPTO -->";
	sInfo += "<!-- *** Supplier Details *** @@SUPPLIER_NAME, @@SUPPLIER_ADDRESS -->";
    sInfo += "<!-- ******** END HERE ********* -->";
	
    sPurchaseHeader = sPurchaseHeader.Replace("@@PO_NUMBER", m_poNumber);
    sPurchaseHeader = sPurchaseHeader.Replace("@@PURCHASE_TYPE", type.ToUpper());
    sPurchaseHeader = sPurchaseHeader.Replace("@@TODAY_DATE", m_sdate);
    sPurchaseHeader = sPurchaseHeader.Replace("@@SUPPLIER_ID", m_supplierID);
    sPurchaseHeader = sPurchaseHeader.Replace("@@SUPPLIER_NAME", m_supplierName);
    sPurchaseHeader = sPurchaseHeader.Replace("@@SUPPLIER_NO", m_supInvNumber);
    sPurchaseHeader = sPurchaseHeader.Replace("@@INVOICE_DATE", m_inv_date);
    sPurchaseHeader = sPurchaseHeader.Replace("@@PO_NUMBER", m_poNumber);
	sPurchaseHeader = sPurchaseHeader.Replace("@@SUPPLIER_ADDRESS", m_supplierAddres);
	string sOldHeader = InvoicePrintHeader(type, "", m_poNumber, m_sdate, "", m_supplierID, m_supplierName, m_supInvNumber);
	sPurchaseHeader = sPurchaseHeader.Replace("@@OLD_HEADER", sOldHeader);
	

	DataRow dr = null;
	if(!GetBillTo())
		return "";
	if(dst.Tables["billto"] != null && dst.Tables["billto"].Rows.Count > 0)
		dr = dst.Tables["billto"].Rows[0];
	
	sPurchaseHeader = sPurchaseHeader.Replace("@@COMPANY_NAME", dr["Company"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@TRADING_NAME", dr["trading_name"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@NAME", dr["Name"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@CONTACT", dr["NameB"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@ADDR1", dr["address1"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@ADDR2", dr["address2"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@ADDR3", dr["address3"].ToString());
    sPurchaseHeader = sPurchaseHeader.Replace("@@COUNTRY", dr["country"].ToString());
	sPurchaseHeader = sPurchaseHeader.Replace("@@PHONE", dr["phone"].ToString());
	sPurchaseHeader = sPurchaseHeader.Replace("@@FAX", dr["fax"].ToString());
	sPurchaseHeader = sPurchaseHeader.Replace("@@EMAIL", dr["email"].ToString());	
	sPurchaseHeader = sPurchaseHeader.Replace("@@COMMENTS", m_note.Replace("\r\n", "\r\n<br>"));
	sPurchaseHeader = sPurchaseHeader.Replace("@@STAFF", m_staffName);
	if(m_shipto == "")
	{
		m_shipto += dr["trading_name"].ToString() + "\r\n";
		m_shipto += dr["address1"].ToString() + "\r\n"; 
		m_shipto += dr["address2"].ToString() + "\r\n"; 
		m_shipto += dr["address3"].ToString() + "\r\n"; 
		m_shipto += dr["country"].ToString() + "\r\n"; 
	}
	sPurchaseHeader = sPurchaseHeader.Replace("@@SHIPTO", m_shipto.Replace("\r\n", "\r\n<br>"));
	
	sPurchaseHeader += sInfo;
	sb.Append(sPurchaseHeader);
    GetPurchaseItems();
	sb.Append("<table width=100% cellpadding=0 cellspacing=0");
	sb.Append("><tr><td>");
	sb.Append("</td></tr><tr>");
	sb.Append("<td nowrap>CODE &nbsp;</td>\r\n");
	sb.Append("<td width=130>PART#</td>\r\n");
	sb.Append("<td>DESCRIPTION</td>\r\n");

	
	sb.Append("<td align=right>");
	if(!m_bNoIndividualPrice)
		sb.Append("PRICE&nbsp&nbsp;");
	else
		sb.Append("&nbsp;");
	sb.Append("</td>\r\n");
	sb.Append("<td nowrap align=right>&nbsp;O QTY&nbsp;</td>\r\n");
	sb.Append("<td nowrap align=right>&nbsp;&nbsp;R QTY&nbsp;</td>\r\n");
	sb.Append("<td align=right>");
	if(!m_bNoIndividualPrice)
		sb.Append("AMOUNT");
	else
		sb.Append("&nbsp;");
	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td colspan=7><hr></td></tr>\r\n");

	double dTotal = 0;
	
	sb.Append("<tr><td>&nbsp;</td></tr>");

	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;

		DataRow drp = null;
		string code = dtCart.Rows[i]["code"].ToString();
		string kid = dtCart.Rows[i]["kid"].ToString();
		string name = dtCart.Rows[i]["name"].ToString();
		string supplier = dtCart.Rows[i]["supplier"].ToString();
		string supplier_code = dtCart.Rows[i]["supplier_code"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();
		double dPrice = MyMoneyParse(dtCart.Rows[i]["supplierPrice"].ToString());
		for(int e=0; e<m_nBranches; e++)
		{
			string AllBranchReceived_qty = GetTotalReceivedQty(kid, code);
			bAllBranchReceived = MyDoubleParse(AllBranchReceived_qty );
			
		}
		if(bAllBranchReceived != 0)
		bAllBranchReceived = bAllBranchReceived;
	    string m_purchaseOrder = GetPurchaseOrderQty(kid, code);

		double dsTotal = 0;
     	if(m_quoteType != GetEnumID("receipt_type", "bill"))
		{
			dTotal += dPrice * MyDoubleParse(qty);
		    dsTotal = dPrice * MyIntParse(qty);
		}
		else
		{
			dTotal += dPrice * bAllBranchReceived;
			dsTotal = dPrice * bAllBranchReceived;
		}
		
		if(m_bPrintView)
			sb.Append("<tr><td nowrap>" + code + "&nbsp&nbsp;</td>");
		else
			sb.Append("<tr><td>&nbsp;</td>"); //don't confuse supplier with our code number
		sb.Append("<td nowrap>" + supplier_code + "&nbsp&nbsp;</td>");
		sb.Append("<td width=70% nowrap>" + name + "&nbsp&nbsp;</td>");
		
		sb.Append("<td align=right nowrap>");

		//sb.Append(GetEnumValue("currency", m_currency).Substring(0, 2).ToUpper() + dPrice.ToString("c"));
		sb.Append(GetCurrencyName(m_currency).Substring(0, 2).ToUpper() + dPrice.ToString("c"));

		sb.Append("&nbsp&nbsp;</td>");
		if(m_quoteType != GetEnumID("receipt_type", "bill") && qty.ToString() !="0")
		sb.Append("<td nowrap align=center nowrap>" + qty + "</td>");
		else
		sb.Append("<td nowrap align=center nowrap>" + m_purchaseOrder + "</td>");
		sb.Append(" <td nowrap align=center nowrap>"+bAllBranchReceived.ToString()+"&nbsp;&nbsp;</td>");
		sb.Append("<td nowrap align=right nowrap>");
//		sb.Append(GetEnumValue("currency", m_currency).Substring(0, 2).ToUpper() + dsTotal.ToString("c"));
		sb.Append(GetCurrencyName(m_currency).Substring(0, 2).ToUpper() + dsTotal.ToString("c"));
		sb.Append("</td></tr>");
	}

	double dFreight = 0;
	if(dst.Tables["purchase"].Rows.Count > 0)
		dFreight = MyDoubleParse(dst.Tables["purchase"].Rows[0]["freight"].ToString());

	{
		double dAmount = dTotal + dFreight;
		double dGstRate = MyDoubleParse(m_gstrate);
		double dTAX = Math.Round((dTotal+ dFreight) * dGstRate, 2);
		dAmount *= (1 + dGstRate);
		dAmount = Math.Round(dAmount, 2);

		sb.Append("<tr><td colspan=7><hr></td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=4 align=right><b>Sub-Total:</b></td><td align=right>");
		sb.Append(dTotal.ToString("c"));
		sb.Append("</td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=4 align=right><b>Freight:</b></td><td align=right>");
		sb.Append(dFreight.ToString("c"));
		sb.Append("</td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=4 align=right><b>GST:</b></td><td align=right>");
		sb.Append(dTAX.ToString("c"));
		sb.Append("</td></tr>\r\n");

		sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=4 align=right><b>TOTAL:</b></td><td align=right>");
		sb.Append(dAmount.ToString("c"));
		sb.Append("</td></tr>\r\n");
	}
    
	sb.Append("</table>\r\n");

	sb.Append("</td></tr><tr><td>");
	sPurchaseFooter = sPurchaseFooter.Replace("@@COMMENTS", m_note.Replace("\r\n", "\r\n<br>"));
	sPurchaseFooter = sPurchaseFooter.Replace("@@STAFF", m_staffName);
	sb.Append("</td></tr></table>");


	
	sb.Append(sPurchaseFooter);
	sb.Append("<br><br>");
	sb.Append("<p class='print' align=right><input type=button value='Dispatch Information' onClick=\"window.open('purchase.aspx?n="+ Request.QueryString["n"]+"&");
	sb.Append("da=1&ssid="+ Request.QueryString["ssid"]+"','my','resizable=yes, width=600, height=500,scrollbars')\"><input type=button value='&nbsp;Print Out&nbsp;'   onClick=\"window.print()\">");
	sb.Append("<input type=button value='Close Window' onClick=\"window.close()\"></p>");
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
	string sc = "SELECT  * FROM purchase_item  WHERE id=" + m_poID + " ORDER BY kid ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "quote");

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

	string sc = "SELECT p.*, c.company AS supplier_name, c.short_name AS supplier_short_name, c.address1 AS supplier_address1 ";
	sc += ", c.address2 AS supplier_address2, c.address3 AS supplier_address3, c.city AS supplier_city, c.phone AS supplier_phone, c.fax AS supplier_fax ";
	sc += " , c2.name AS staff_name ";
	sc += " FROM purchase p JOIN card c ON c.id=p.supplier_id ";
	sc += " JOIN card c2 ON c2.id = p.staff_id ";
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
	m_staffName = dr["staff_name"].ToString();
	m_supplierAddres = dr["supplier_address1"].ToString() + "\r\n <br>";
	m_supplierAddres += dr["supplier_address2"].ToString() + "\r\n <br>";
	m_supplierAddres += dr["supplier_address3"].ToString() + "\r\n <br>";
	m_supplierAddres += dr["supplier_city"].ToString() + "\r\n <br>";
	m_supplierAddres += "Phone: "+ dr["supplier_phone"].ToString() + "\r\n <br>";
	m_supplierAddres += "Fax: "+ dr["supplier_fax"].ToString() + "\r\n <br>";

	m_note = dr["note"].ToString();
	m_supInvNumber = dr["inv_number"].ToString();
	m_sdate = DateTime.Parse(dr["date_create"].ToString()).ToString("dd-MM-yyyy HH:mm");
	m_status = dr["status"].ToString();
	m_currency = dr["currency"].ToString();
	m_exrate = dr["exchange_rate"].ToString();
	m_gstrate = dr["gst_rate"].ToString();
	m_shipto = dr["shipto"].ToString();
	m_dFreight = MyDoubleParse(dr["freight"].ToString());
m_inv_date = dr["date_invoiced"].ToString();
	if(m_inv_date != "")
		m_inv_date = DateTime.Parse(m_inv_date).ToString("dd-MM-yyyy HH:mm");	
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
		Response.Write("<h4><a href=purchase.aspx?n=" + pid + "&ssid=" + m_ssid + " class=o>View Purchase</h4>");

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

bool DoReceiveFull(string id, string code, string branch)
{


	
	string sc = " UPDATE dispatch SET received = 1 WHERE id = " + id +" AND code = "+code +" AND branch="+branch+"";
	       sc += " AND received = 0 AND date_received IS NULL";
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



bool DoAllBranchReceived(string id, string code, string branch)
{


	
	string sc = " UPDATE dispatch SET received = 1 WHERE id = " + id +" AND code = "+code +" AND branch="+branch+"";
	       sc += " AND received = 0 AND date_received IS NULL";
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

//////////////////////////////////
//dispatch info functions
bool GetPurchaseItems()
{
	BuildBranchIndex();
	string sc = " SELECT * FROM purchase_item  WHERE id = " + m_poID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "items") <= 0)
		{
			MsgDie("no items found");
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


string GetPOrderTotal(string id)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT total_amount  FROM purchase WHERE id = " + id ;

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
     string nRet = dsd.Tables[0].Rows[0]["total_amount"].ToString();
	
	return nRet;

}


string GetDispatchedQty(string kid, string branch_id)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT SUM(qty) AS qty FROM dispatch WHERE id = " + kid + " AND branch = " + branch_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["qty"].ToString());
	return nRet.ToString();
}

string GetReceivedQty(string kid, string branch_id)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT SUM(qty) AS qty FROM dispatch WHERE id = " + kid + " AND branch = " + branch_id + " AND received = 1 AND date_received IS NOT NULL ";
	     
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["qty"].ToString());
	return nRet.ToString();
}

string GetOriginalQty(string po_number)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT SUM(qty) AS oqty FROM purchase_item i JOIN purchase p  ON i.id = p.id WHERE po_number= "+ po_number;
		  
	     
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["oqty"].ToString());
	return nRet.ToString();
}

string GetPurchaseQty(string id, string po_number)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT SUM(qty) AS pqty FROM purchase_item i JOIN purchase p  ON i.id = p.id WHERE po_number= "+ po_number+" AND i.id="+id;
		  
	     
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["pqty"].ToString());
	return nRet.ToString();
}

string GetTotalReceivedQty(string kid, string code)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT SUM(qty) AS totalReceivedQty FROM dispatch WHERE id = " + kid + " AND received = 1 AND code="+code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["totalReceivedQty"].ToString());
	return nRet.ToString();
}

string GetPurchaseOrderQty(string kid, string code)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT oqty FROM purchase_order po JOIN purchase_item pi ON po.id = pi.id WHERE pi.kid = " + kid + " AND po.code="+code;

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["oqty"].ToString());
	return nRet.ToString();
}

string GetDispatchKid(string kid)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT kid AS dKid  FROM dispatch WHERE id = " + kid + " AND received = 0 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["dKid"].ToString());
	return nRet.ToString();
}


string GetVisaulReceivedQty(string kid, string branch_id)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT SUM(qty) AS visual_qty FROM dispatch WHERE id = " + kid + " AND branch = " + branch_id + " AND received = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	int nRet = MyIntParse(dsd.Tables[0].Rows[0]["visual_qty"].ToString());
	return nRet.ToString();
}

bool BuildBranchIndex()
{
	DataSet dsb = new DataSet();

	//string sc = "SELECT id, name FROM branch  ORDER BY id";
	string sc = "SELECT id, name FROM branch where activated = 1  ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_nBranches = myCommand.Fill(dsb, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i=0; i<m_nBranches; i++)
	{
		string name = dsb.Tables["branch"].Rows[i]["name"].ToString();
		string id = dsb.Tables["branch"].Rows[i]["id"].ToString();
		m_aBranchName[i] = name;
		m_aBranchID[i] = id;
	}
	return true;
}
bool DoExportPurchase()
{
	int nRows = 0;
	string sc = " SELECT (SELECT TOP 1 ISNULL(trading_name, company) FROM card WHERE short_name = cr.supplier) AS supplier_name ";
	sc += ", cr.supplier AS supplier_short_name ";
	sc += ", CONVERT(varchar(99), p.date_create, 106) AS purchase_date ";
	sc += ", cr.code, pi.supplier_code, pi.name AS item_name, pi.qty, pi.price ";
	sc += " FROM purchase p ";
	sc += " JOIN purchase_item pi ON pi.id = p.id ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = pi.code ";
//	sc += " LEFT OUTER JOIN card c ON c.short_name = cr.supplier ";
	sc += " WHERE p.id = " + m_poID;
	sc += " ORDER BY supplier_name ";
//DEBUG("sc=", sc);	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(dst, "report");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
	{
		ErrMsgAdmin("purchase not found, id: " + m_poID);
		return false;
	}
	DataRow dr = dst.Tables["report"].Rows[0];
	string sdate = dr["purchase_date"].ToString();
	string fn = "doc/" + m_sCompanyTitle + "_PO" + m_poID + "_" + sdate + ".xls";
	fn = fn.Replace(" ", "_");
	string sPath = Server.MapPath(fn);
//DEBUG("p=", sPath);	
	if(DataTableExportToExcel(dst.Tables["report"], sPath))
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>" + Lang("Export done, click to download file") + "</br></br>");
		Response.Write("<a class=o href=" + fn + ">" + fn + "</a></h3>");
	}
	return true;
}

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
			 DataNavigateUrlFormatString="purchase.aspx?bi={0}"
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

<!-- #include file="kit_fun.cs" -->
<!-- #include file="card_function.cs" -->

<script runat="server">

double m_dTotal = 0;
double m_dTotalFromInvoice = 0;
double m_dTotalCost = 0;
double m_dRecPrice = 0; //recommended system promotion price
double m_dPromDiscount = 0;
double m_dFreight = 0;
double m_dealerRate = 1;
bool m_bAdmin = false; //print for administrator
bool m_bShowCost = true;
int m_id = -1;
int m_nManualBox = -1;
bool m_bNoReLoad = false;
bool m_bREcalculateClicked = false;

string m_branchID = "1";
string m_customerID = "0";
string m_customerName = "";
string m_customerEmail = "";
string m_customerLevel = "1";
string m_custPONumber = "";
string m_quoteNumber = "";
string m_quoteType = "1";	//"1"--- quote ;
string m_quoteDate = ""; //used for restore quote
string m_paymentType = "1";
string m_sales = "";
string m_salesName = "";
string m_quoteNote = "";
string m_discount = "0";

string m_owndealer_level = "1";
string m_nShippingMethod = "1";
string m_specialShipto = "0";
string m_specialShiptoAddr = ""; //special
string m_pickupTime = "";

bool m_bPaid = false;
bool m_bIncludeGST = false;
bool m_bNoIndividualPrice = true;
bool m_bQuoteCreated = false;

bool m_bSayWillAddLaborFee = true;
double m_dYourTotal = 0;
double m_dYourMargin = 0.1; //default
string m_yourCustomerName = "";
string m_sDealerDraft = "0"; //customer draft

double dGST = 0;								//26.06.2003 NEO

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

bool m_bCustomerChanged = false;

bool QPage_Load()
{
	RememberLastPage();
	dGST = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;					//XW 30.JUN.2003

	if(Request.Form["branch"] != null)
		m_branchID = Request.Form["branch"];

//	m_bNoReLoad = MyBooleanParse(GetSiteSettings("quotation_no_reload", "0", true));
//DEBUG("mbnoreoload = ", m_bNoReLoad);
	//clean last uri session
	if(Session["last_uri_exp"] != null)
		Session["last_uri_exp"] = null;
//clean up user interface once back from pos.aspx
	if(Session["use_order_interface"] != null)
		Session["use_order_interface"] = null;

	if(Request.Form["cmd"] == "Recalculate Price")
		m_bREcalculateClicked = true;
	//sales session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		PrepareNewQuote();
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("q.aspx" + par);
		return false;
	}

	if(g_bSysQuoteAddHardwareLabourCharge)
	{
		if(Session["q_labour_code_h"] == null)
		{
			m_labourCodeH = GetSiteSettings("system_hardware_labour_item_code", "");
			if(m_labourCodeH == "" && m_sSite == "admin")
			{
				Response.Write("<br><center><h3><font color=red>Warning, hardware labour charge service item code not defined, ");
				Response.Write("go to <a href=settings.aspx class=o target=_blank>Edit Site Settings</a> and enter system_hardware_labour_item_code.</h3>");
			}
			else
			{
				Session["q_labour_code_h"] = m_labourCodeH;
			}
		}
		else
			m_labourCodeH = Session["q_labour_code_h"].ToString();
	}

	if(g_bSysQuoteAddSoftwareLabourCharge)
	{
		if(Session["q_labour_code_s"] == null)
		{
			m_labourCodeS = GetSiteSettings("system_software_labour_item_code", "");
			if(m_labourCodeS == "" && m_sSite == "admin")
			{
				Response.Write("<br><center><h3><font color=red>Warning, software labour charge service item code not defined, ");
				Response.Write("go to <a href=settings.aspx class=o target=_blank>Edit Site Settings</a> and enter system_software_labour_item_code.</h3>");
			}
			else
			{
				Session["q_labour_code_s"] = m_labourCodeS;
			}
		}
		else
			m_labourCodeS = Session["q_labour_code_s"].ToString();
	}

	if(m_sSite == "admin")
	{
		m_bAdmin = true;
		Session[m_sCompanyName + "_ordering"] = true;	
		Session[m_sCompanyName + "_salestype"] = "quote";
	}

	if(Request.QueryString["p"] == "new")
		PrepareNewQuote();
	else if(Request.QueryString["t"] == "talk")
	{
		PrintTalkBackTable();
		return false;
	}
	
	CheckQTable();

	if(Request.Form["cmd"] == "New Quote")
	{
		PrepareNewQuote();
		Response.Redirect("q.aspx");	
		return false;
	}

	//remember everything entered in Session Object
	if(Request.Form["branch"] != null)
		UpdateAllFields();

	RestoreAllFields();

	if(Session["sales_current_quote_number" + m_ssid] != null)
		m_quoteNumber = Session["sales_current_quote_number" + m_ssid].ToString();
	if(m_quoteNumber != "")
		m_bQuoteCreated = true;

	if(m_quoteNumber != "")
		m_quoteType = sDoGetQuoteType(m_quoteNumber);
	
	if(m_sales == "")
	{
		if(TS_UserLoggedIn())
		{
			m_sales = Session["card_id"].ToString(); //default
			m_salesName = Session["name"].ToString();
		}
	}
	if(Request.QueryString["me"] != null)
	{
		try
		{
			m_nManualBox = int.Parse(Request.QueryString["me"]);
		}
		catch(Exception e)
		{
		}
	}
	
	if(Request.QueryString["n"] != null)
	{
		m_quoteNumber = Request.QueryString["n"];
		if(!IsInteger(m_quoteNumber))
		{
			Response.Write("<h3>ERROR, WRONG NUMBER</h3>");
			return false;
		}
		if(!RestoreQuote())
			return false;
		if(Request.QueryString["t"] == "credit") //credit
		{

		}
		else
		{
			Session["sales_current_quote_number" + m_ssid] = m_quoteNumber;
			m_bQuoteCreated = true;
		}
		if(g_bSysQuoteAddHardwareLabourCharge || g_bSysQuoteAddSoftwareLabourCharge)
			CheckInstallationCharge(false);
	}
	else
	{
		if(g_bSysQuoteAddHardwareLabourCharge || g_bSysQuoteAddSoftwareLabourCharge)
			CheckInstallationCharge(true);
	}

	if(Request.QueryString["sc"] == "0")
		m_bShowCost = false;
			
	string type = Request.QueryString["t"];

	if(Request.Form["postback"] == "yes")
	{
		if(Request.Form["cmd"] == "Delete")
		{
			if(DoDeleteQuote())
			{
				string uo = "olist.aspx?ot=1&system=1";
			
				if(!m_bAdmin)
				{
					uo = "status.aspx?system=1";
					PrintHeaderAndMenu();
				}
				else
					uo = "q.aspx";
				
				Response.Write("<br><br><center><h3>SYSTEM QUOTATION DELETED!!!</h3>");
				Response.Write("<input type=button ");
				if(!m_bAdmin)
					Response.Write("value='Quotation List' ");
				else
					Response.Write("value='New Quotation' ");
				Response.Write(" onclick=window.location=('" + uo + "') ");
				Response.Write(Session["button_style"] + ">");
			}
			return false;
		}

		if(Request.Form["cmd"] == "Call Me")
		{
			DoNotifySales();
			return false;
		}
		if(!ChangeAllOptions())
			return false;
		UpdateAllPrice();

		if(Request.Form["cmd"] == "Search") //search for customer
		{
			Response.Write("<br><center><h3>Search For Customer</h3></center>");
			DoCustomerSearchAndList();
			return false;
		}
		else if(Request.Form["cmd"] == null) //buy button clicked
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?t=b&s=1&r=");
			Response.Write(DateTime.Now.ToOADate() + "\">");
			return false;
		}
		else if(Request.Form["cmd"] == "Send")
		{
			if(!DoMail())
				return true;
		}
		else if(Request.Form["cmd"] == "View Printable Quote")
		{
			//update quote first, only for admin, customer not allowed to update order
			if(m_bQuoteCreated)
			{
				if(!DoUpdateQuote())
				{
					Response.Write("<h3>Error Update Quote</h3>");
					return false;
				}
			}
			Response.Write("<script language=JavaScript");
			Response.Write(">");
			Response.Write("quotation_window=window.open('q.aspx?ssid=" + m_ssid + "&n=" + m_quoteNumber + "&t=i&tp=" + m_quoteType + "'); quotatoin_window.focus() ");
			Response.Write("</script");
			Response.Write(">");
		}
		else if(Request.Form["cmd"] == "Print Quote")
		{
			//update quote first, only for admin, customer not allowed to update order
			if(m_bQuoteCreated)
			{
				if(!DoUpdateQuote())
				{
					Response.Write("<h3>Error Update Quote</h3>");
					return false;
				}
			}
			Response.Write("<script language=JavaScript");
			Response.Write(">");
			Response.Write("window.open('q.aspx?ssid=" + m_ssid + "&customer=1&n=" + m_quoteNumber + "&t=i&tp=" + m_quoteType + "')");
			Response.Write("</script");
			Response.Write(">");
		}
		else if(Request.Form["cmd"] == "Create Quote" || Request.Form["cmd"] == "Save Quote")
		{
			if(!m_bQuoteCreated)
			{
				if(!DoCreateQuote(false))
				{
//					Response.Write("<h3>ERROR CREATE QUOTE</h3>");
					return false;
				}
				Session["sales_current_quote_number" + m_ssid] = m_quoteNumber;
				m_bQuoteCreated = true;
			}
		}
		else if(Request.Form["cmd"] == "Update Quote" 
			|| Request.Form["cmd"] == "Place Order" 
			|| Request.Form["cmd"] == "Unlock Order")
		{
//DEBUG("paid = ", Request.Form["paid"]);
			if(Request.Form["paid"] == "0")
				m_bPaid = false;
			else
				m_bPaid = true;
			if(!DoUpdateQuote())
			{
				Response.Write("<h3>Error Update Quote</h3>");
				return false;
			}
			if(Request.Form["cmd"] == "Place Order" || Request.Form["cmd"] == "Unlock Order")
			{
				UnLockOrder(m_quoteNumber);
				PrintHeaderAndMenu();
				if(Request.Form["cmd"] == "Unlock Order")
					Response.Write("<br><br><center><h3>Order Unlocked.</h3>");
				else
					Response.Write("<br><br><center><h3>Order Placed.</h3>");
				Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('");
				if(m_bAdmin)
					Response.Write("olist.aspx?system=1') value='Quote List'></center>");
				else
					Response.Write("status.aspx?system=1') value='Quote List'></center>");
				Response.Write("<br><br><br><br><br><br><br>");
				PrintFooter();
				return false; //return false prevent showing form
			}
		}
		else if(Request.Form["cmd"] == "Send Email To")
		{
			if(!SalesSendMail())
				return true;
		}
	}
	
	if(Request.QueryString["id"] != null && Request.Form["cmd"] != "Add")
	{
		m_id = int.Parse(Request.QueryString["id"]);
		PrepareNewQuote();
		if(!GetRecommendedSystem())
			return false;
	}

	if(type == "change")
	{
		string key = Request.QueryString["k"];
		string code = Request.QueryString["v"];
		if(!ChangeOption(key, code, "1"))
			return false;
	}
	else if(type == "b") //conditional build
	{
		if(!DoBuild())
			return false;
	}
	else if(type == "i") //view invoice
	{
//		m_quoteType = Request.QueryString["tp"];
		if(Request.QueryString["customer"] == "1")
			Response.Write(PrintDraft());
		else
			Response.Write(BuildQInvoice("", true));
		return false;
	}
//BindDebugGrid(dtQ);
	return true;
}

string sDoGetQuoteType(string sQuoteNumber)
{
	string stype = "1";
	if(dst.Tables["quotetype"] != null)
		dst.Tables["quotetype"].Clear();
	string sc = "SELECT type, discount, isnull(quote_total,0) AS quote_total FROM orders WHERE id=" + sQuoteNumber;
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "quotetype") == 1)
		{
			stype = dst.Tables["quotetype"].Rows[0]["type"].ToString();
			m_discount = dst.Tables["quotetype"].Rows[0]["discount"].ToString();
			m_dTotal = MyDoubleParse(dst.Tables["quotetype"].Rows[0]["quote_total"].ToString());
		}
	}
	catch(Exception e) 
	{
		ShowExp("", e);
		return "";
	}
	return stype;
}
void DoNotifySales()
{
	string name = Request.Form["name"];
	string phone = Request.Form["phone"];
	string email = Request.Form["email"];
	Trim(ref name);
	Trim(ref phone);
	Trim(ref email);
	string err = "";
	if(name == "")
		err = "please enter your name.";
	else if(phone == "")
		err = "please enter your phone number.";
	else if(email == "")
		err = "pleae enter your email address.";
	if(err != "")
	{
		PrintHeaderAndMenu();
		Response.Write("<br><br><center><h3>Error, " + err + "</h3>");
		Response.Write("<input type=button onclick=history.go(-1) value=' << Back '>");
		PrintFooter();
		return;
	}

	m_quoteNumber = Request.Form["quoteNumber"];
	string smail = "Dear " + m_sCompanyName + " sales:\r\n\r\n<br><br>";
	smail += "A customer has created a system quotation and wish you to call him, or her, whatever, doesn't really matter, just sit back and pick up the phone, call the number, see what the hell he, or her whatever doesn't really matter wants :)\r\n<br>";
	smail += "\r\n<br>Name : " + name;
	smail += "\r\n<br>Phone : " + phone;
	smail += "\r\n<br>Email : <a href=mailto:" + email + ">" + email + "</a>";
	smail += "\r\n<br>Quote# : <a href=http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/admin/q.aspx?ssid=" + m_ssid + "&n=" + m_quoteNumber + ">" + m_quoteNumber + "</a>";
	smail += "\r\n\r\n<br><br>Have a nice day.\r\n<br>EZNZ Team";
	smail += "\r\n<br>" + DateTime.Now.ToString("dd-MM-yyyy HH:mm");
	
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = m_sSalesEmail;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = "Call Customer, Quote #" + m_quoteNumber;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = smail;
	SmtpMail.Send(msgMail);

	smail = "Dear " + name + " : \r\n\r\n<br>";
	smail += "Your Quote Number is : <a href=http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/q.aspx?ssid=" + m_ssid + "&n=" + m_quoteNumber + ">" + m_quoteNumber + "</a>";
	smail += "\r\n<br>You can click the above link to review your quote.\r\n\r\n<br><br>";
	smail += "Best Regards\r\n<br>";
	smail += m_sCompanyTitle;
	smail += "\r\n<br>" + DateTime.Now.ToString("dd-MM-yyyy HH:mm");
	msgMail.To = email;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = "Sysmtem Quotation# " + m_quoteNumber + " - " + m_sCompanyTitle;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = smail;
	SmtpMail.Send(msgMail);

	PrintHeaderAndMenu();
	Response.Write("<br><br><center><h3>Your request has been sent, one of our sales will call you soon</h3>");
	Response.Write("<h3>We have also sent one email to you for back up, please check it ou.</h3>");
	Response.Write("<input type=button onclick=window.location=('q.aspx?ssid=" + m_ssid + "&n=" + m_quoteNumber + "') value=' << Back to Quote '>");
	PrintFooter();
}

void PrintTalkBackTable()
{
	if(Session["sales_current_quote_number" + m_ssid] != null)
		m_quoteNumber = Session["sales_current_quote_number" + m_ssid].ToString();

	PrintHeaderAndMenu();
	Response.Write("<br><br><center><h3>Quote Created</h3>");
	Response.Write("<table><tr><td><h4>Your quote number is : <a href=q.aspx?ssid=" + m_ssid + "&n=" + m_quoteNumber + " class=o><font color=red>" + m_quoteNumber + "</font></a></h4>");
	Response.Write("<br><b>Would you like one of our sales to call you?");
	Response.Write("<br>Or you can call our sales line during office hour : " + GetSiteSettings("contact_phone") + "</b></td></tr>");
	Response.Write("</td></tr>");
	Response.Write("<form action=q.aspx?ssid=" + m_ssid + " method=post>");
	Response.Write("<input type=hidden name=quoteNumber value=" + m_quoteNumber + ">");
	Response.Write("<input type=hidden name=postback value=yes>");
	Response.Write("<tr><td><table border=1>");
	
	//name
	Response.Write("<tr><td><b>&nbsp&nbsp;Name : &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;</b></td><td>");
	Response.Write("<input type=text name=name size=30 value='");
	if(TS_UserLoggedIn())
		Response.Write(Session["name"].ToString());
	Response.Write("'></td></tr>");

	//Phone
	Response.Write("<tr><td><b>&nbsp&nbsp;Phone : </b></td><td><input type=text name=phone size=30 value='");
	if(TS_UserLoggedIn())
	{
		CheckUserTable();
		Response.Write(dtUser.Rows[0]["phone"].ToString());
	}
	Response.Write("'></td></tr>");

	//email
	Response.Write("<tr><td><b>&nbsp&nbsp;Email : </b></td><td><input type=text name=email size=30 value='");
	if(TS_UserLoggedIn())
		Response.Write(Session["email"].ToString());
	Response.Write("'></td></tr>");

	Response.Write("<tr><td>");
//	if(!TS_UserLoggedIn())
//		Response.Write("<a href=login.aspx class=o>login to get info</a>");
//	else
		Response.Write("&nbsp;");
	Response.Write("</td><td align=right>");
	Response.Write("<input type=submit name=cmd value='Call Me'>");
	Response.Write("</td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("</form>");

	if(!TS_UserLoggedIn())
	{
		Response.Write("<tr><td>");
		Response.Write("<a href=login.aspx class=o>login</a> to get your account info");
		Response.Write("</td></tr>");
	}
	Response.Write("</table>");
	PrintFooter();
}

void PrepareNewQuote()
{
	EmptyQTable();
/*
	Session["q_install_os"] = null;
	Session["sales_current_quote_number"] = null;
	Session["sales_discount"] = null;
	Session["sales_customerid" + m_ssid] = null;
	Session["quote_sales_id"] = null;
	Session["quote_sales_name"] = null;
	Session["sales_shipping_method"] = null;
	Session["sales_special_shipto"] = null;
	Session["sales_special_ship_to_addr"] = null;
	Session["sales_pick_up_time"] = null;
	Session["sales_customer_po_number"] = null;
	Session["quote_payment_type"] = null;
	Session["quote_note"] = null;
	Session["q_ship_as_parts"] = null;
	Session["q_your_customer_name"] = null;
*/
	m_bQuoteCreated = false;
	EmptyCart();
/*
	if(g_bSysQuoteAddHardwareLabourCharge && m_labourCodeH != "")
	{
		GetCustomer(); //get customer level first
		AddHardwareLabourCharge();
	}
*/
}

bool GetOrder()
{
	string sc = "SELECT * FROM orders WHERE id=" + m_quoteNumber;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "invoice") <= 0)
		{
			Response.Write("<h3>&nbsp;&nbsp;&nbsp;ERROR, Quote/Order Not Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr =  dst.Tables["invoice"].Rows[0];
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
	sc += " WHERE id="+ m_quoteNumber + " ";
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

	m_customerID = dr["card_id"].ToString();
	dGST = GetGstRate(m_customerID);				// 30.JUN.2003 XW
	m_quoteType = dr["type"].ToString();
//DEBUG("mqyotype =", m_quoteType);
	m_quoteDate = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy");
	m_paymentType = dr["payment_type"].ToString();
	m_dFreight = double.Parse(dr["freight"].ToString());
	Session["sales_freight" + m_ssid] = m_dFreight;
	m_dTotalFromInvoice = double.Parse(dr["quote_total"].ToString());
	m_sales = dr["sales"].ToString();
	Session["quote_sales_id" + m_ssid] = m_sales;
	m_bPaid = (bool)dr["paid"];
	m_bNoIndividualPrice = (bool)dr["no_individual_price"];
	m_bIncludeGST = (bool)dr["gst_inclusive"];
	m_custPONumber = dr["po_number"].ToString();
	m_pickupTime = dr["pick_up_time"].ToString();
	m_quoteNote = dr["sales_note"].ToString();
	m_nShippingMethod = dr["shipping_method"].ToString();
	//added discount to the system
	m_discount = dr["discount"].ToString();
	if(bool.Parse(dr["special_shipto"].ToString()))
		m_specialShipto = "1";
	else
		m_specialShipto = "0";
	m_specialShiptoAddr = dr["shipto"].ToString();

	m_yourCustomerName = dr["dealer_customer_name"].ToString();
	m_dYourTotal = MyDoubleParse(dr["dealer_total"].ToString());
	m_bShipAsParts = bool.Parse(dr["ship_as_parts"].ToString());

//DEBUG("nip=", m_bNoIndividualPrice.ToString());
//DEBUG("m_sales=", m_sales);	
	if(m_bNoIndividualPrice)
		Session["no_individual_price" + m_ssid] = "true";
	else
		Session["no_individual_price" + m_ssid] = null;

	if(m_bIncludeGST)
		Session["quotation_display_include_gst" + m_ssid] = "true";
	else
		Session["quotation_display_include_gst" + m_ssid] = null;

//	if(m_customerID == "")				
//		return true; //cash sales
	
	dr = GetCardData(m_customerID);
	if(dr == null)
		return true;

	m_customerEmail = dr["email"].ToString();
	m_customerLevel = dr["dealer_level"].ToString();

	dr = GetCardData(m_sales);
	if(dr == null)
		return true;
	m_salesName = dr["name"].ToString();
	Session["quote_sales_name" + m_ssid] = m_salesName;

	return true;
}

bool RestoreQuote()
{
	CheckShoppingCart(); //rebuild shopping cart
	EmptyCart(); //empty shopping cart for optionals

	if(!GetOrder())
		return false;
	int rows = 0;
	string sc = "SELECT * FROM order_item WHERE id=" + m_quoteNumber + " ORDER BY part";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "quote");
		if(rows <= 0)
		{
			Response.Write("<h3>&nbsp;&nbsp;&nbsp;ERROR, Quote/Order Not Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	m_dTotal = 0;
	//delete current quotation
	for(int i=0; i<m_qfields; i++)
	{
		dtQ.Rows[0][fn[i]] = "-1";
		dtQ.Rows[0][fn[i]+"_qty"] = "0";
		dtQ.Rows[0][fn[i]+"_price"] = "0";
		dtQ.Rows[0][fn[i]+"_name"] = "";
		dtQ.Rows[0][fn[i]+"_special"] = "0";
	}

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["quote"].Rows[i];
		int n = -2;
		string part = dr["part"].ToString();
		if(part != null && part != "")
		{
			if(IsInteger(part))
				n = int.Parse(part);
			string code = dr["code"].ToString();
			string name = dr["item_name"].ToString();
			string serial = "";//dr["serial_number"].ToString();
			string supplier = dr["supplier"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string supplier_price = dr["supplier_price"].ToString();
			string qty = dr["quantity"].ToString();
			string price = dr["commit_price"].ToString();
			bool bSpecial = bool.Parse(dr["sys_special"].ToString());
			m_dTotal += double.Parse(price) * int.Parse(qty);

			if(Request.QueryString["t"] == "credit") //credit
				qty = (0 - MyIntParse(qty)).ToString();

			if(n >= 0)
			{
				dtQ.Rows[0][fn[n]] = code;
				dtQ.Rows[0][fn[n]+"_qty"] = qty;
				dtQ.Rows[0][fn[n]+"_price"] = price;
				dtQ.Rows[0][fn[n]+"_name"] = name;
				if(bSpecial)
					dtQ.Rows[0][fn[n]+"_special"] = "1";
			}
			else if(n == -1)
			{
				if(!AddToCart(code, supplier, supplier_code, qty, supplier_price, price, name, serial))
					return false;
			}
		}
	}
	double dDiscount = (m_dTotal - m_dTotalFromInvoice) / m_dTotal;
	dDiscount *= 100;
	Session["sales_discount" + m_ssid] = dDiscount;
	return true;
}

bool DoUpdateQuote()
{
	if(!GetCustomer())
		return false;

	if(!DeleteOrderItems(m_quoteNumber))
		return false;

	if(!RecordOrderItems())
		return false;

	//update total price
//	if(MyDoubleParse(m_dTotal) > 0 && MyMoneyParse(Request.Form["total"] < 0 )
	m_dTotal = MyMoneyParse(Request.Form["total"]);

	string shipAsParts = "0";
	if(m_bShipAsParts)
		shipAsParts = "1";
	
	string sytotal = Request.Form["your_total"];
	if(sytotal != null && sytotal != "")
		m_dYourTotal = MyMoneyParse(sytotal);
	else
		m_dYourTotal = 0;

	string branch = Request.Form["branch"];
	if(branch == null || branch == "")
		branch = "1"; //default to 1
	if(Request.Form["type"] != null)
		m_quoteType = Request.Form["type"];
	if(Request.Form["cmd"] == "Place Order")
		m_quoteType = "2"; //change to order
//DEBUG("m_qtyotyep =", m_quoteType);
	if(Request.Form["discount"] != null)
		m_discount = Request.Form["discount"].ToString();

//DEBUG("quotetoal =", m_discount);
	string sc = "UPDATE orders SET type = " + m_quoteType;
	sc += ", branch = " + branch;
	sc += ", quote_total=" + m_dTotal;
	sc += ", freight = " + m_dFreight;
	if(m_bAdmin)
		sc += ", card_id=" + m_customerID;
//	sc += ", sales=" + m_sales;
	sc += ", po_number = '" + m_custPONumber + "' ";
	sc += ", shipping_method=" + m_nShippingMethod;
	sc += ", pick_up_time='" + m_pickupTime + "'";
	sc += ", payment_type=" + m_paymentType;
	sc += ", sales_note='" + EncodeQuote(m_quoteNote) + "' ";
	if(m_discount != null && m_discount != "")
		sc += ", discount = "+ m_discount +"";
//	sc += ", sales_note='" + m_quoteNote + "' ";
	sc += ", no_individual_price=";
	if(m_bNoIndividualPrice)
		sc += "1";
	else
		sc += "0";
	sc += ", gst_inclusive=";
	if(m_bIncludeGST)
		sc += "1";
	else
		sc += "0";

	if(Request.Form["special_shipto"] == "on" || (bool)Session["login_is_branch"] )
		sc += ", special_shipto=1, shipto='" + EncodeQuote(Request.Form["special_ship_to_addr"]) + "' ";
	sc += ", ship_as_parts=" + shipAsParts;
	sc += ", dealer_customer_name = '" + m_yourCustomerName + "' ";
	sc += ", dealer_total = " + m_dYourTotal;
	if(Request.Form["cmd"] == "Place Order")
		sc += ", dealer_draft = 0 ";
	sc += " WHERE id=" + m_quoteNumber;
//DEBUG("sc = ", sc);
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

bool DoCreateQuote(bool bUpdate)
{
	if(Request.Form["cmd"] == "Save Quote")
		m_sDealerDraft = "1";

	DataRow dr = null;
	if(!GetCustomer())
		return false;
	if(dst.Tables["card"] != null && dst.Tables["card"].Rows.Count > 0)
		dr = dst.Tables["card"].Rows[0];
	
	string branch = Request.Form["branch"];
	if(branch == null || branch == "")
		branch = "1"; //default to 1

	string snip = "0"; // no individual price
	if(m_bNoIndividualPrice)
		snip = "1";
	string sgsti = "0";
	if(m_bIncludeGST)
		sgsti = "1";

	if(Request.Form["cust_po"] != null)
		m_custPONumber = EncodeQuote(Request.Form["cust_po"]);

	string shipAsParts = "0";
	if(m_bShipAsParts)
		shipAsParts = "1";
	m_discount = Request.Form["discount"];
	if(m_discount == "")
		m_discount = "0";
//DEBUG("discount = ", m_discount);
//	if(Request.Form["ship_as_parts"] == "on")
//		m_quoteType = "2";// order

	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (branch, type, number, card_id, po_number, dealer_draft, dealer_customer_name) ";
	sc += " VALUES(" + branch + ", " + m_quoteType + ", 0, " + m_customerID + ", '";
	sc += m_custPONumber + "', " + m_sDealerDraft + ", '" + m_yourCustomerName + "') ";
	sc += " SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		myCommand1.Fill(dst, "id");
	}
	catch(Exception e) 
	{
		string s = e.ToString().ToLower();
		if(s.IndexOf("invalid column name 'dealer_draft'") >= 0)
		{
			myConnection.Close();
			if(!AddDealerDraftColumn())
				return false;
			try
			{
				SqlDataAdapter myCommand2 = new SqlDataAdapter(sc, myConnection);
				myCommand2.Fill(dst, "id");
			}
			catch(Exception e1) 
			{
				ShowExp(sc, e1);
				return false;
			}
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}
	}

	if(dst.Tables["id"] == null || dst.Tables["id"].Rows.Count != 1)
	{
		Response.Write("<br><br><center><h3>Create Order failed, error getting new order number</h3>");
		return false;
	}

	m_quoteNumber = dst.Tables["id"].Rows[0]["id"].ToString();
	Session["sales_current_quote_number" + m_ssid] = m_quoteNumber;

	//assign ordernumber same as id
	sc = "UPDATE orders SET number=" + m_quoteNumber;
	sc += ", freight=" + m_dFreight;
	sc += ", shipping_method=" + m_nShippingMethod;
	sc += ", pick_up_time='" + m_pickupTime + "'";
	if(Request.Form["special_shipto"] == "on" || (bool)Session["login_is_branch"] )
		sc += ", special_shipto=1, shipto='" + EncodeQuote(Request.Form["special_ship_to_addr"]) + "' ";
	sc += ", system=1 ";
	sc += ", po_number='" + m_custPONumber + "' ";
	sc += ", no_individual_price=" + snip;
	sc += ", gst_inclusive=" + sgsti;
	sc += ", ship_as_parts=" + shipAsParts;
	sc += ", payment_type=" + m_paymentType;
	if(m_sales != "")
		sc += ", sales=" + m_sales;
	if(m_discount != null && m_discount != "")
		sc += ", discount = " + m_discount +"";
	sc += ", sales_note='" + EncodeQuote(m_quoteNote) + "' ";
//	sc += ", sales_note='" + m_quoteNote + "' ";
	sc += " WHERE id=" + m_quoteNumber;
//DEBUG("sc = ", sc);
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

	if(!RecordOrderItems())
		return false;
	//now we had m_dTotal, go ahead record discount

	//calc discount
	double dDiscount = 0;
	if(Session["sales_discount" + m_ssid] != null)
		dDiscount = (double)Session["sales_discount" + m_ssid];

	if(Request.Form["discount"] != Request.Form["discount_old"])
	{
		string sd = Request.Form["discount"];
		if(!TSIsDigit(sd))
			sd = "0";
		dDiscount = double.Parse(sd, NumberStyles.Currency, null);
		m_dTotal *= (1 - dDiscount/100);
	}
	else if(Request.Form["total"] != Request.Form["total_old"])
	{
		string st = Request.Form["total"];
		double dt = double.Parse(st, NumberStyles.Currency, null);
		dDiscount = (m_dTotal - dt)/m_dTotal;
		dDiscount *= 100;
		m_dTotal = dt;
	}
	else
	{
		m_dTotal *= (1 - dDiscount/100);
	}
	Session["sales_discount" + m_ssid] = dDiscount;

	string sytotal = Request.Form["your_total"];
	if(sytotal != null && sytotal != "")
	{
		m_dYourTotal = MyMoneyParse(sytotal);
	}
	else
		m_dYourTotal = 0;

	//record it, we will restore discount using quote_total
	sc = "UPDATE orders SET quote_total=" + m_dTotal + ", dealer_total=" + m_dYourTotal + " WHERE id=" + m_quoteNumber;
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

bool RecordOrderItems()
{
	m_dTotal = 0;

	for(int i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString();
//DEBUG("i="+i, " code="+code);
		if(code == null || code == "")
		{
			continue;
		}
		if(int.Parse(code) < 0)
			continue;
		double dPrice = 0;
		dPrice = double.Parse(dtQ.Rows[0][fn[i]+"_price"].ToString(), NumberStyles.Currency, null);
		if(m_sSite.ToLower() != "admin")
			dPrice = dGetDealerPrice(code, m_owndealer_level);

		dPrice = Math.Round(dPrice, 2);
		DataRow drp = null;
		if(!GetProductWithSpecialPrice(code, ref drp))
		{
			Response.Write("<br><center><h3>Error getting product " + code);
			return false;
		}
		string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
		m_dTotal += dPrice * int.Parse(qty);

		//write to database
		if(!InsertToOrderItem(m_quoteNumber, "", qty, dPrice, drp, i))
			return false;
	}

	//optionals
	CheckShoppingCart();
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		if(dtCart.Rows[i]["system"].ToString() == "1")
			continue;

		DataRow drp = null;
		string code = dtCart.Rows[i]["code"].ToString();
		string name = dtCart.Rows[i]["name"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();
		if(!GetProductWithSpecialPrice(code, ref drp))
		{
			Response.Write("<br><center><h3>Error getting product " + code);
			return false;
		}

		double dPrice = MyMoneyParse(dtCart.Rows[i]["SalesPrice"].ToString());//MyDoubleParse(drp["price"].ToString());
		double dsTotal = dPrice * int.Parse(qty);
		double dsupplierPrice = double.Parse(drp["supplier_price"].ToString(), NumberStyles.Currency, null);;

		m_dTotal += dPrice;

		//write to database
		if(!InsertToOrderItem(m_quoteNumber, name, qty, dPrice, drp, -1))
			return false;
	}

//	double dGST = MyDounbleParse(GetSiteSettings("gst_rate_percent", "12.5"));
//	double m_dTAX = m_dTotal * 0.125;
		
	double m_dTAX = m_dTotal * dGST;						//Modified by NEO
	return true;
}

bool InsertToOrderItem(string id, string name, string sQty, double dPrice, DataRow drp, int nPart)
{
	string code = "";
	string supplier = "";
	string supplier_code = "0";
	string supplier_price = "0";
	string sys_special = "0";
//	string m_cost_nzd ="0";
	if(nPart >= 0)
		sys_special = dtQ.Rows[0][fn[nPart] + "_special"].ToString();
	if(drp != null)
	{
		code = drp["code"].ToString();
		supplier = drp["supplier"].ToString();
		supplier_code = drp["supplier_code"].ToString();
		supplier_price = drp["supplier_price"].ToString();
//		m_cost_nzd = drp["manual_cost_nzd"].ToString(); // Testing
	}
	else
	{
		code = "0"; //manually entered
	}

	if(name == "")
	{
		if(drp != null)
		{
			name = drp["name"].ToString();
		}
		else
		{
			name = dtQ.Rows[0][fn[nPart] + "_name"].ToString();
		}
	}

	if(name.Length > 255)
		name = name.Substring(0, 255);
	string sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
	sc += ", commit_price, system, sys_special, part) VALUES(" + id + ", " + code + ", ";
	sc += sQty + ", '" + EncodeQuote(name) + "', '" + supplier;
	sc += "', '" + supplier_code + "', " + supplier_price;
	sc += ", " + dPrice + ", 1, " + sys_special + ", " + nPart.ToString() + ") "; 
	if(m_quoteType == "2") //order
	{
		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + code;
		sc += " AND branch_id = " + m_branchID;
		sc += ")";
		sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
		sc += " VALUES (" + code + ", " + m_branchID + ", 0, " + sQty + ")"; 
		sc += " ELSE Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock + " + sQty;
		sc += " WHERE code=" + code + " AND branch_id = " + m_branchID;

		sc += " UPDATE product SET allocated_stock=allocated_stock+" + sQty;
		sc += " WHERE code=" + code;
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

bool SalesSendMail()
{
	if(!GetCustomer())
		return false;
	if(Request.Form["customer_email"] == null || Request.Form["customer_email"] == "")
		return false;

	string smail = BuildQInvoice("", false);
	if(smail == "")
		return false;
//DEBUG("here", 0);
//DEBUG("smail=", smail);
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = Request.Form["customer_email"];
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = "SYSTEM QUOTATION #" + m_quoteNumber;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = smail;

	SmtpMail.Send(msgMail);

	//cc to sales self
	msgMail.To = Session["email"].ToString();
	SmtpMail.Send(msgMail);
	return true;
}

string BuildQInvoice(string msg, bool bInvoiceOnly)
{
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
	sb.Append("</b>");

	if(m_quoteType == null)
		m_quoteType = "1";  //1 --- "Quote"
	
	//get customer
	DataRow dr = null;
	if(!GetCustomer())
		return "";
	if(dst.Tables["card"] != null && dst.Tables["card"].Rows.Count > 0)
		dr = dst.Tables["card"].Rows[0];

	if(m_quoteDate == "")
		m_quoteDate = DateTime.Now.ToString("dd-MM-yyyy");
	string temp_type = (GetEnumValue("receipt_type", m_quoteType));
	sb.Append(InvoicePrintHeader(temp_type, m_salesName, m_quoteNumber, m_quoteDate, m_custPONumber, m_customerID, ""));

	sb.Append(InvoicePrintShip(dr, ""));

	sb.Append("</td></tr><tr><td>\r\n");

	sb.Append("<table width=100% cellpadding=0 cellspacing=0 border=0 ");
	sb.Append(" bgcolor=#FFFFEE><tr><td><b>SYSTEM</b></td></tr><tr>");
	sb.Append("<td width=70>PART#</td>\r\n");
	sb.Append("<td>DESCRIPTION</td>\r\n");
	sb.Append("<td align=right>");
	if(!m_bNoIndividualPrice)
		sb.Append("PRICE");
	else
		sb.Append("&nbsp;");
	sb.Append("</td>\r\n");
	sb.Append("<td align=right>QTY</td>\r\n");
	sb.Append("<td width=70 align=right>");
	if(!m_bNoIndividualPrice)
		sb.Append("AMOUNT");
	else
		sb.Append("&nbsp;");
	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	double dTotal = 0;
	StringBuilder sq = new StringBuilder();
	sq.Append(Request.ServerVariables["SERVER_NAME"] + "/q.aspx?ssid=" + m_ssid + "&t=b"); //query string for url link
	
	for(int i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString();
//DEBUG("i="+i, " code="+code);
		if(code == null || code == "")
		{
			continue;
		}
		string name = "";
		if(int.Parse(code) > 0)
		{
			name = GetProductDesc(code);
		}
		else if(code == "0")
		{
			name = dtQ.Rows[0][fn[i] + "_name"].ToString();
		}
		else
			continue;

		string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
		double dPrice = double.Parse(dtQ.Rows[0][fn[i]+"_price"].ToString(), NumberStyles.Currency, null);
		double dsPrice = dPrice * int.Parse(qty);
		dTotal += dsPrice;
		if(m_bIncludeGST)
		{
			dPrice *= 1.125;
			dsPrice *= 1.125;
		}
		string price = dPrice.ToString("c");

		sq.Append("&" + fn[i] + "=" + code);
		if(qty != "0" && qty != "1")
			sq.Append("&" + fn[i] + "qty=" + qty);

		sb.Append("<tr><td>");
		sb.Append(code);
		sb.Append("</td><td>");
		sb.Append(name);
		sb.Append("</td><td align=right>");
		if(!m_bNoIndividualPrice)
			sb.Append(price);
		else
			sb.Append("&nbsp;");
		sb.Append("</td><td align=right>");
		sb.Append(qty);
		sb.Append("</td><td align=right>"); //quantity
		if(!m_bNoIndividualPrice)
			sb.Append(dsPrice.ToString("c"));
		else
			sb.Append("&nbsp;");
		sb.Append("</td></tr>");
	}

	//optionals
	sb.Append("<tr><td>&nbsp;</td></tr>");

	CheckShoppingCart();
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		if(dtCart.Rows[i]["system"].ToString() == "1")
			continue;

		string code = dtCart.Rows[i]["code"].ToString();
		string qty = dtCart.Rows[i]["quantity"].ToString();
		double dPrice = 0;
		double dsupplierPrice = 0;
		string name = dtCart.Rows[i]["name"].ToString();
		try
		{
			dPrice = double.Parse(dtCart.Rows[i]["salesPrice"].ToString(), NumberStyles.Currency, null);
			dsupplierPrice = double.Parse(dtCart.Rows[i]["supplierPrice"].ToString(), NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
		}

		double dsTotal = dPrice * int.Parse(qty);
		dTotal += dsTotal;
		if(m_bIncludeGST)
		{
			dPrice *= 1.125;
			dsTotal *= 1.125;
		}

		sb.Append("<tr><td>" + code + "</td>");
		sb.Append("<td>" + name + "</td>");
		sb.Append("<td align=right>");

		if(!m_bNoIndividualPrice)
			sb.Append(dPrice.ToString("c"));
		else
			sb.Append("&nbsp;");

		sb.Append("</td>");
		sb.Append("<td align=right>" + qty + "</td>");
		sb.Append("<td align=right>");

		if(!m_bNoIndividualPrice)
			sb.Append(dsTotal.ToString("c"));
		else
			sb.Append("&nbsp;");

		sb.Append("</td></tr>");
	}

//	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>SUB-TOTAL:</b></td><td align=right>");
//	sb.Append(dTotal.ToString("c"));
//	sb.Append("</td></tr>\r\n");

//	double dGstRate = MyDoubleParse((GetSiteSettings("gst_rate_percent", "12.5"))) / 100;

	double dDiscount = 0;
	if(Session["sales_discount" + m_ssid] != null)
		dDiscount = (double)Session["sales_discount" + m_ssid];
	dTotal *= (1 - dDiscount / 100);
	double dAmount = dTotal;
//	double dTAX = (dTotal + m_dFreight) * 0.125;
	double dTAX = (dTotal + m_dFreight) * dGST;					//Modified by NEO
	dTAX = Math.Round(dTAX, 2);
	dAmount = dTotal + m_dFreight + dTAX;

	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>SUB-TOTAL:</b></td><td align=right>");
	sb.Append(dTotal.ToString("c"));
	sb.Append("</td></tr>\r\n");

/*	if(dDiscount > 0)
	{
		sb.Append("<tr><td colspan=4 align=right><b>DISCOUNT:</b></td><td align=right>");
		sb.Append(Math.Round(dDiscount, 2).ToString());
		sb.Append("%</td></tr>\r\n");

		sb.Append("<tr><td colspan=4 align=right><b>YOUR PRICE:</b></td><td align=right>");
		sb.Append(dTotal.ToString("c"));
		sb.Append("</td></tr>\r\n");
	}
*/
	sb.Append("<tr><td>&nbsp;</td><td colspan=3 align=right><b>Freight:</b></td><td align=right>");
	sb.Append(m_dFreight.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td colspan=3 align=right><b>GST:</b></td><td align=right>");
	sb.Append(dTAX.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td colspan=4 align=right><b>TOTAL AMOUNT DUE:</b></td><td align=right>");
	sb.Append(dAmount.ToString("c"));
	sb.Append("</td></tr>");

	if(m_quoteNote != "")
	{
		sb.Append("<tr><td colspan=4><b>Note</b></td></tr>");
		sb.Append("<tr><td colspan=4>" + m_quoteNote.Replace("\r\n", "<br>\r\n") + "</td></tr>");
	}

	sb.Append("</table>\r\n");

	sb.Append("</table>");
	sb.Append("</td></tr>");
	if(!bInvoiceOnly && !m_bSales)
		sb.Append("<tr><td>Check out details on <a href=" + sq + ">" + sq + "</a></td></tr>");
	sb.Append("</table>");
	sb.Append(InvoicePrintBottom());

	sb.Append("</body></html>");
	return sb.ToString();
}

string PrintDraft()
{
	if(m_quoteType == null)
		m_quoteType = "1";  //1 --- "Quote"
	
	//get customer
	DataRow dr = null;
	if(!GetCustomer())
		return "";
	if(dst.Tables["card"] != null && dst.Tables["card"].Rows.Count > 0)
		dr = dst.Tables["card"].Rows[0];

	if(m_quoteDate == "")
		m_quoteDate = DateTime.Now.ToString("dd-MM-yyyy");
	string temp_type = (GetEnumValue("receipt_type", m_quoteType));

	string trading_name = Session["trading_name"].ToString();
	string address1 = "";
	string address2 = "";
	string address3 = "";
	string phone = "";
	string fax = "";

	if(dr != null)
	{
		trading_name = dr["trading_name"].ToString();
		address1 = dr["address1"].ToString();
		address2 = dr["address2"].ToString();
		address3 = dr["address3"].ToString();
		phone = dr["phone"].ToString();
		fax = dr["fax"].ToString();
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	sb.Append("<body>\r\n");

	sb.Append("<table width=100%><tr><td>");

	sb.Append("<table>");
	sb.Append("<tr><td>");
	sb.Append("<h1><i>" + trading_name + "</i></h1>");
	sb.Append("</td></tr>");
	sb.Append("<tr><td>" + address1 + "</td></tr>");
	sb.Append("<tr><td>" + address2 + "</td></tr>");
	sb.Append("<tr><td>" + address3 + "</td></tr>");
	sb.Append("<tr><td><b>Tel : </b>" + phone + " <b>Fax : </b>" + fax + "</td></tr>");
	sb.Append("</table>");

	sb.Append("</td><td align=right valign=top>");

	sb.Append("<table>");
	sb.Append("<tr><td align=right><b>Customer : </b></td><td>" + m_yourCustomerName + "</td></tr>");
	sb.Append("<tr><td align=right><b>Quote # : </b></td><td>" + m_quoteNumber + "</td></tr>");
	sb.Append("<tr><td align=right><b>Date : </b></td><td>" + m_quoteDate + "</td></tr>");
	sb.Append("</table>");

	sb.Append("</td></tr>");
	sb.Append("</table>");

	sb.Append("<center><h3>SYSTEM QUOTATION</h3>");

//	sb.Append(InvoicePrintHeader(temp_type, m_salesName, m_quoteNumber, m_quoteDate, m_custPONumber, m_customerID, ""));

//	sb.Append(InvoicePrintShip(dr, ""));

	sb.Append("</td></tr><tr><td>\r\n");

	sb.Append("<table width=100% cellpadding=0 cellspacing=0 border=0><tr>");
//	sb.Append("<td width=70>PART#</td>\r\n");
	sb.Append("<td width=70>&nbsp;</td>\r\n");
	sb.Append("<td>DESCRIPTION</td>\r\n");
	sb.Append("<td align=right>");
//	if(!m_bNoIndividualPrice)
//		sb.Append("PRICE");
//	else
		sb.Append("&nbsp;");
	sb.Append("</td>\r\n");
	sb.Append("<td align=right>QTY</td>\r\n");
	sb.Append("<td width=70 align=right>");
//	if(!m_bNoIndividualPrice)
//		sb.Append("AMOUNT");
//	else
		sb.Append("&nbsp;");
	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	double dTotal = 0;
	StringBuilder sq = new StringBuilder();
	sq.Append(Request.ServerVariables["SERVER_NAME"] + "/q.aspx?ssid=" + m_ssid + "&t=b"); //query string for url link
	
	for(int i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString();
		if(code == null || code == "")
		{
			continue;
		}

		string name = "";
		if(int.Parse(code) > 0)
		{
			name = GetProductDesc(code);
		}
		else if(code == "0")
		{
			name = dtQ.Rows[0][fn[i] + "_name"].ToString();
		}
		else
			continue;

		string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
		double dPrice = double.Parse(dtQ.Rows[0][fn[i]+"_price"].ToString(), NumberStyles.Currency, null);
		double dsPrice = dPrice * int.Parse(qty);
		dTotal += dsPrice;
		if(m_bIncludeGST)
		{
			dPrice *= 1.125;
			dsPrice *= 1.125;
		}
		string price = dPrice.ToString("c");

		sq.Append("&" + fn[i] + "=" + code);
		if(qty != "0" && qty != "1")
			sq.Append("&" + fn[i] + "qty=" + qty);

		sb.Append("<tr><td>");
		sb.Append("&nbsp;"); //code);
		sb.Append("</td><td>");
		sb.Append(name);
		sb.Append("</td><td align=right>");
//		if(!m_bNoIndividualPrice)
//			sb.Append(price);
//		else
			sb.Append("&nbsp;");
		sb.Append("</td><td align=right>");
		sb.Append(qty);
		sb.Append("</td><td align=right>"); //quantity
//		if(!m_bNoIndividualPrice)
//			sb.Append(dsPrice.ToString("c"));
//		else
			sb.Append("&nbsp;");
		sb.Append("</td></tr>");
	}

	//optionals
	sb.Append("<tr><td>&nbsp;</td></tr>");

	CheckShoppingCart();
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		if(dtCart.Rows[i]["system"].ToString() == "1")
			continue;

		string code = dtCart.Rows[i]["code"].ToString();
		if(code == m_labourCodeH || code == m_labourCodeS)
			continue;

		string qty = dtCart.Rows[i]["quantity"].ToString();
//			if(!GetProductWithSpecialPrice(code, ref drp))
//				return "";
//
//			double dPrice = double.Parse(drp["price"].ToString(), NumberStyles.Currency, null);
		double dPrice = 0;
		double dsupplierPrice = 0;
		string name = dtCart.Rows[i]["name"].ToString();
		try
		{
			dPrice = double.Parse(dtCart.Rows[i]["salesPrice"].ToString(), NumberStyles.Currency, null);
			dsupplierPrice = double.Parse(dtCart.Rows[i]["supplierPrice"].ToString(), NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
		}
//			if(!GetSupplierPrice(code, ref dsupplierPrice))
//				return "";

		double dsTotal = dPrice * int.Parse(qty);
		dTotal += dsTotal;
		if(m_bIncludeGST)
		{
			dPrice *= 1.125;
			dsTotal *= 1.125;
		}

		sb.Append("<tr>");
		sb.Append("<td>&nbsp;</td>");//" + code + "</td>");
		sb.Append("<td>" + name + "</td>");
		sb.Append("<td align=right>");

//			if(!m_bNoIndividualPrice)
//				sb.Append(dPrice.ToString("c"));
//			else
			sb.Append("&nbsp;");

		sb.Append("</td>");
		sb.Append("<td align=right>" + qty + "</td>");
		sb.Append("<td align=right>");

//			if(!m_bNoIndividualPrice)
//				sb.Append(dsTotal.ToString("c"));
//			else
			sb.Append("&nbsp;");

		sb.Append("</td></tr>");
	}

	double dDiscount = 0;
	if(Session["sales_discount" + m_ssid] != null)
		dDiscount = (double)Session["sales_discount" + m_ssid];
	dTotal *= (1 - dDiscount / 100);
	double dAmount = dTotal;
//	double dTAX = (dTotal + m_dFreight) * 0.125;
	double dTAX = (dTotal + m_dFreight) * dGST;					//Modified by NEO
	dTAX = Math.Round(dTAX, 2);
	dAmount = dTotal + m_dFreight + dTAX;


	dAmount = m_dYourTotal;
	dTotal = Math.Round(dAmount * 0.9, 2);
	dTAX = dAmount - dTotal;

	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>SUB-TOTAL:</b></td><td align=right>");
	sb.Append(dTotal.ToString("c"));
	sb.Append("</td></tr>\r\n");

//	sb.Append("<tr><td>&nbsp;</td><td colspan=3 align=right><b>Freight:</b></td><td align=right>");
//	sb.Append(m_dFreight.ToString("c"));
//	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td colspan=3 align=right><b>GST:</b></td><td align=right>");
	sb.Append(dTAX.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td colspan=4 align=right><b>TOTAL AMOUNT DUE:</b></td><td align=right>");
	sb.Append(dAmount.ToString("c"));
	sb.Append("</td></tr>");

	if(m_quoteNote != "")
	{
		sb.Append("<tr><td colspan=4><b>Note</b></td></tr>");
		sb.Append("<tr><td colspan=4>" + m_quoteNote.Replace("\r\n", "<br>\r\n") + "</td></tr>");
	}

	sb.Append("</table>\r\n");

	sb.Append("</table>");
	sb.Append("</td></tr>");
	sb.Append("</table>");

	sb.Append("</body></html>");
	return sb.ToString();
}

bool DoMail()
{
	if(Request.Form["email"] == null || Request.Form["email"] == "")
		return false;

	string smail = BuildQInvoice(Request.Form["mailbody"], false);
	if(smail == "")
		return false;

	MailMessage msgMail = new MailMessage();
	
	msgMail.To = Request.Form["email"];
	msgMail.Body = "Hello there, your friend ";
	if(TS_UserLoggedIn())
	{
		msgMail.From = Session["email"].ToString();
		msgMail.Body = Session["name"].ToString() + "<" + Session["email"].ToString() + "> ";
	}
	else 
		msgMail.From = m_sSalesEmail;
	msgMail.Body += "recommends a computer system to you.<br>\r\n";
	msgMail.Body += "Feel free to contact us by sending email to " + m_sSalesEmail + ".<br>\r\n";

	msgMail.Subject = "System Quotation";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body += smail;

	SmtpMail.Send(msgMail);

	if(Request.Form["ccsales"] == "on")
	{
		msgMail.To = m_sSalesEmail;
//		msgMail.To = "darcy@eznz.com";
		msgMail.Body += "<br>\r\n Original mail sent to : ";
		msgMail.Body += "<a href=mailto:" + Request.Form["email"] + ">";
		msgMail.Body += Request.Form["email"] + "</a><br>\r\n";
		SmtpMail.Send(msgMail);
	}

	return true;
}

bool GetRecommendedSystem()
{
	string sc = "SELECT * FROM q_sys WHERE id=" + m_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "rc_sys");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//BindDebugGrid(dst.Tables["rc_sys"]);
	if(dst.Tables.Count > 0 && dst.Tables["rc_sys"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["rc_sys"].Rows[0];
		for(int i=0; i<m_qfields; i++)
		{
			string code = dr[fn[i]].ToString();
//DEBUG("fn[i]="+fn[i], " code="+dr[fn[i]].ToString());
			dtQ.Rows[0][fn[i]] = code;
			dtQ.Rows[0][fn[i]+"_qty"] = dr[fn[i] + "_qty"].ToString();
			double dPrice = 0;
			if(IsInteger(code) && int.Parse(code) > 0)
			{
				if(!GetItemPrice(code, dr[fn[i] + "_qty"].ToString(), ref dPrice))
				{
					dPrice = 999;
					AlertMissProduct(code, fn[i], GetSiteSettings("manager_email", "alert@eznz.com"));
				}
			}
			dtQ.Rows[0][fn[i]+"_price"] = dPrice.ToString();
		}
		if(dr["price"].ToString() != "")
			m_dRecPrice = double.Parse(dr["price"].ToString());
	}
	else
	{
		Response.Write("<h3>&nbsp;&nbsp;&nbsp;Error, System Configuration Not Found</h3>");
		return false;
	}
	return true;
}

bool ApplyPriceForCustomer()
{
	if(!m_bCustomerChanged)
		return true;

	for(int i=0; i<m_qfields; i++)
	{
		string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
		if(!IsInteger(qty))
			qty = "1";

		string code = dtQ.Rows[0][fn[i]].ToString();
		if(code == "-1")
			continue;
		double dPrice = GetSalesPriceForDealer(code, qty, m_customerLevel, m_customerID);
		dtQ.Rows[0][fn[i] + "_price"] = dPrice.ToString();
	}

	CheckShoppingCart();
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		if(dtCart.Rows[i]["system"].ToString() == "1")
			continue;

		DataRow dr = dtCart.Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["quantity"].ToString();
		if(qty == "") //some sales just delete the qty not enter 0
			qty = "0"; //assume
//		string price = Request.Form["price_" + code];
//		if(price == null)
//			continue;
//		string price_old = Request.Form["price_" + code + "_old"];
//		double dPrice = MyMoneyParse(price);
//		if(price == price_old) //only apply level price when no manual entered price
		double dPrice = GetSalesPriceForDealer(code, qty, m_customerLevel, m_customerID);
//DEBUG("dprice = ", dPrice.ToString());
		dr["SalesPrice"] = dPrice.ToString();
	}
	return true;
}

bool PrintQForm()
{
	string title = "SYSTEM QUOTATION";
	if(m_bQuoteCreated)
		title += " #" + m_quoteNumber;
	if(Request.QueryString["t"] == "credit")
		title = "<font color=red>Creidt Note</font> for quote # <a href=q.aspx?ssid=" + m_ssid + "&n=" + m_quoteNumber + " target=_blank>" + m_quoteNumber + "</a>";
	if(Request.Form["discount"] != null)
		m_discount = Request.Form["discount"].ToString();
	m_dTotal = 0;
	Response.Write("<form name=form1 action=q.aspx?ssid=" + m_ssid + " method=post>");
	Response.Write("\r\n\r\n<center><font size=+1><b>" + title);
	Response.Write("</b></font></center><br>");

	if(!GetCustomer())
		return false;

	if(m_bSales) //private sales
	{

		Response.Write("<table width=90% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

		Response.Write("<tr><td valign=top>");
		
		//Customer Information
		Response.Write("<table>");
		Response.Write("<tr><td><b>Customer :&nbsp;</b></td>");

		Response.Write("<td>");
		Response.Write("<select name=customer onclick=window.location=('q.aspx?ssid=" + m_ssid + "&search=1&r=" + DateTime.Now.ToOADate().ToString() + "')>");
		Response.Write("<option value=0>Cash Sales</option>");
		if(m_customerID != "0")
			Response.Write("<option value='" + m_customerID + "' selected>" + m_customerName + "</option>");
		Response.Write("</select>");
		if(m_customerID != "0")
		{
			Response.Write("<input fgcolor=blue type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + m_customerID + "','', ' width=350,height=350');\" value='who?' " + Session["button_style"] + ">");
			dGST = GetGstRate(m_customerID);																									// 30.JUN.2003 XW
			//Response.Write(" <a href=\"javascript:window.open('viewcard.aspx?id=" + m_customerID + "', '', 'width=350, height=350');\" class=o>who?</a>");
			//Response.Write(" <a href='viewcard.aspx?id=" + m_customerID + "' width=350 height=350 class=o target=_blank>who?</a>");
		}
	string luri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
		Response.Write(" <input type=button title='add new customer' value='EZ CARD' " + Session["button_style"] + " onclick=\"javascript:addcard_window=window.open('ezcard.aspx?r="+ DateTime.Now.ToOADate() +"&luri="+ HttpUtility.UrlEncode(luri) +"','','resizable=1, screenX=300,screenY=200,left=300,top=200'); addcard_window.focus();\" >");
		Response.Write("</td></tr>");
		Response.Write("<tr><td><b>PO Number :&nbsp;</b></td>");
		Response.Write("<td><input type=text size=10 name=cust_po value='" + m_custPONumber + "'></td>");
		Response.Write("</tr>");

		Response.Write("</table>");
		
		Response.Write("</td><td valign=top>");

		//Invoice type and Payment status
		Response.Write("<table>");
		Response.Write("<tr><td><b>Type : </b></td><td><select name=type>");

		Response.Write("<option value=1>QUOTE</option>");
		Response.Write("<option value=2");
		if(m_quoteType == "2")
			Response.Write(" selected");
		Response.Write(">ORDER</option>");
		
		Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
		Response.Write("</td></tr><tr><td>");
		
		//payment type
		Response.Write("<b>Payment : </b></td><td><select name=payment_type>");

		Response.Write(GetEnumOptions("payment_method", m_paymentType));

		Response.Write("&nbsp;");
		Response.Write("</select>");	

		Response.Write("</td></tr><tr><td>");

		Response.Write("</td></tr>");
		Response.Write("</table>");

		Response.Write("</td><td valign=top>");
		
		//sales, branch
		Response.Write("<table>");
		Response.Write("<tr><td><b>Sales : </b></td><td><b>" + m_salesName + "</b>");
		Response.Write("<input type=hidden name=sales value='" + m_sales + "'></td></tr>");
		//Branch ID
		Response.Write("<tr><td>");
		Response.Write("<b>Branch : </b></td><td>");
		if(!PrintBranchNameOptions())
			return false;
		Response.Write("</td></tr>");
		Response.Write("</table>");

		Response.Write("</td><td valign=top>");

		//GST
		Response.Write("<table>");
		Response.Write("<tr><td><input type=checkbox name=gst_inclusive ");
		if(m_bIncludeGST)
			Response.Write(" checked");
		Response.Write(">GST Inclusive ");

		//Individual Price
		Response.Write("</td></tr><tr><td><input type=checkbox name=nip ");
		if(m_bNoIndividualPrice)
			Response.Write(" checked");
		Response.Write(">No Individual Price &nbsp;");
		Response.Write("</td></tr>");
		Response.Write("</table>");

		Response.Write("</td></tr>");

		Response.Write("<tr><td colspan=4>");
		PrintShipToTable();
		PrintJavaFunction();
		Response.Write("</td></tr>");
		Response.Write("</table><br>");
	}
	else //public site
	{
		Response.Write("<input type=hidden name=branch value=1>");
		Response.Write("<input type=hidden name=type value=" + m_quoteType + ">");

		if(Session["card_type"] != null && Session["card_type"].ToString() != "1" && m_bQuoteCreated)
		{
			Response.Write("<table width=70% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td colspan=4 align=center>");
			PrintShipToTable();
			PrintJavaFunction();
			Response.Write("</td></tr>");
			Response.Write("</table><br>");
		}
	}

	Response.Write("<table class=d align=center valign=center cellspacing=1 cellpadding=0 border=1>");
	
	ApplyPriceForCustomer();

	for(int i=0; i<m_qfields; i++)
	{
		if(!FormPrintRow(i))
			return false;
	}

	CheckShoppingCart();
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		if(dtCart.Rows[i]["system"].ToString() == "1")
			continue;

		DataRow dr = dtCart.Rows[i];
		DataRow drp = null;
		string code = dr["code"].ToString();
		string qty = dr["quantity"].ToString();
		if(qty == "") //some sales just delete the qty not enter 0
			qty = "0"; //assume

		if(!GetProductWithSpecialPrice(code, ref drp))
			return false;

		double dPrice = MyMoneyParse(dtCart.Rows[i]["salesPrice"].ToString());
		double dsupplierPrice = MyMoneyParse(dtCart.Rows[i]["supplierPrice"].ToString());

		m_dTotalCost += dsupplierPrice * MyIntParse(qty);
		m_dTotal += dPrice * MyIntParse(qty);
		Response.Write("<tr height=24><td><b>Others</b></td>");
		Response.Write("<td>" + dtCart.Rows[i]["name"].ToString() + "</td>");
		if(m_bSales)
		{
			Response.Write("<td align=right><input type=text size=1 name=qty_" + code);
			Response.Write(" style='text-align:right' value='" + qty + "'>");
			Response.Write("<input type=hidden name=qty_" + code + "_old value='" + qty + "'></td>");
			Response.Write("<td title='" + dsupplierPrice.ToString("c") + "' align=right><input type=text size=7 name=price_" + code + " style='text-align:right' ");
			Response.Write(" value='" + dPrice.ToString("c") + "'>");
			Response.Write("<input type=hidden name=price_" + code + "_old ");
			Response.Write(" value='" + dPrice.ToString("c") + "'>");
		}
		else
		{
			Response.Write("<td align=right>" + qty + "</td><td align=right>" + dPrice.ToString("c"));
		}
		Response.Write("</td></tr>");
	}
	//calc discount
	double dDiscount = 0;
	double dMargin = 0;
//	double dGST = 0;
	double dSubTotal = 0;

	if(Request.Form["margin"] != "" && TSIsDigit(Request.Form["margin"]))
	{
		
		string sm = Request.Form["margin"];
		dMargin = MyDoubleParse(sm);
		double dFinal = Math.Round(m_dTotalCost * (1 + dMargin/100), 2);	
		dDiscount = (m_dTotal - dFinal) / m_dTotal * 100;
		Session["sales_discount" + m_ssid] = dDiscount;
		m_dTotal = dFinal;
	}
	else
	{
		if(Session["sales_discount" + m_ssid] != null)
			dDiscount = (double)Session["sales_discount" + m_ssid];
	//	if(MyDoubleParse(m_discount) > 0)
		if(m_discount != Request.Form["discount_old"])
		{
			m_dTotal *= (1 - MyDoubleParse(m_discount) / 100);
		}
		else if(Request.Form["total"] != Request.Form["total_old"])
		{
			string st = Request.Form["total"];
			double dt = double.Parse(st, NumberStyles.Currency, null);
			dDiscount = (m_dTotal - dt)/m_dTotal;
			dDiscount *= 100;
			m_dTotal = dt;
			m_discount = dDiscount.ToString();
//DEBUG("m_dTotal=", m_dTotal.ToString());
		}
		else if(Request.Form["subtotal"] != Request.Form["subtotal_old"])
		{
			string st = Request.Form["subtotal"];
			double dt = MyMoneyParse(st);
			dSubTotal = dt;
			double fdTotal = dt / 1.125 - m_dFreight;
			dDiscount = (m_dTotal - fdTotal)/m_dTotal;
			dDiscount *= 100;
			m_dTotal = fdTotal;
			m_discount = dDiscount.ToString();
//DEBUG("m_dTotal=", m_dTotal.ToString());
		}
		else
		{
			m_dTotal *= (1 - MyDoubleParse(m_discount) / 100);
		}
	/*	{
			if(Request.Form["discount"] != Request.Form["discount_old"])
			{
				string sd = Request.Form["discount"];
				if(!TSIsDigit(sd))
					sd = "0";
				dDiscount = double.Parse(sd, NumberStyles.Currency, null);
				m_dTotal *= (1 - dDiscount/100);
			}
			else if(Request.Form["total"] != Request.Form["total_old"])
			{
				string st = Request.Form["total"];
				double dt = double.Parse(st, NumberStyles.Currency, null);
				dDiscount = (m_dTotal - dt)/m_dTotal;
				dDiscount *= 100;
				m_dTotal = dt;
	//DEBUG("m_dTotal=", m_dTotal.ToString());
			}
			else if(Request.Form["subtotal"] != Request.Form["subtotal_old"])
			{
				string st = Request.Form["subtotal"];
				double dt = MyMoneyParse(st);
				dSubTotal = dt;
				double fdTotal = dt / 1.125 - m_dFreight;
				dDiscount = (m_dTotal - fdTotal)/m_dTotal;
				dDiscount *= 100;
				m_dTotal = fdTotal;
	//DEBUG("m_dTotal=", m_dTotal.ToString());
			}
			else
			{
				m_dTotal *= (1 - dDiscount / 100);
			}
			
		}*/
		Session["sales_discount" + m_ssid] = dDiscount;
		dMargin = (m_dTotal - m_dTotalCost) / m_dTotalCost;
		dMargin = Math.Round(dMargin, 4);
		dMargin *= 100;
	}
	//TAX
//	dGST = (m_dTotal + m_dFreight) * 0.125;
	dGST = (m_dTotal + m_dFreight) * dGST;					//Modified by NEO
	dGST = Math.Round(dGST, 2);
	if(dSubTotal == 0)
		dSubTotal = m_dTotal + m_dFreight + dGST;			//Modified by NEO

	Response.Write("<tr><td colspan=3 align=right>");
	if(m_bSales && m_bShowCost)
	{
		Response.Write("<font color=#DDDDDD size=1>Cost=" + m_dTotalCost.ToString("c") + ", Margin=" + dMargin.ToString() + "%");
		Response.Write("&nbsp&nbsp&nbsp&nbsp;<input type=password size=3 name=margin>%</font>&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
		Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
		Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
		Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	}
	if(m_sSite == "admin" && (dDiscount != 0 || m_bSales) )
	{
		Response.Write("<b>Discount</b>&nbsp;<input type=text size=1 style=text-align:right name=discount value=");
//		Response.Write(Math.Round(dDiscount, 2));
		Response.Write(Math.Round(MyDoubleParse(m_discount), 2));
		if(!m_bSales)
			Response.Write(" readonly=true");
		//Response.Write("><input type=hidden name=discount_old value='" + Math.Round(dDiscount, 2) + "'>%&nbsp;&nbsp;&nbsp;");
		Response.Write(" OnChange=\"if(!parseFloat(document.form1.discount.value)){document.form1.discount.value=0;}\"><input type=hidden name=discount_old value='" + Math.Round(MyDoubleParse(m_discount), 2) + "'>%&nbsp;&nbsp;&nbsp;");
	}

	Response.Write("<b>Total</b></td><td align=right");
	if(m_bSales) //display cost and margin
		Response.Write(" title='" + m_dTotalCost.ToString("c") + ", " + dMargin.ToString() + "%'");

	Response.Write(">");
	if(m_bSales)
		Response.Write("<input type=text name=total size=7 style='text-align:right' value='");
	else
		Response.Write(m_dTotal.ToString("c") + "<input type=hidden name=total value='");

	Response.Write(m_dTotal.ToString("c"));
	Response.Write("'>");

	Response.Write("<input type=hidden name=total_old value='");
	Response.Write(m_dTotal.ToString("c"));
	Response.Write("'>");

	Response.Write("</td></tr>");

	if(m_sSite == "admin")
	{
		Response.Write("<tr><td colspan=3 align=right><b>Freight:</b></td><td align=right>");
		Response.Write("<input type=text size=7 name=freight style='text-align:right' value='" + m_dFreight.ToString("c") + "'>");
		Response.Write("</td></tr>\r\n");
	}
	else
	{
		Response.Write("<tr><td colspan=3 align=right><b>Freight</b></td><td align=right>");
		Response.Write(m_dFreight.ToString("c"));
		Response.Write("</td></tr>\r\n");
	}

	Response.Write("<tr><td colspan=3 align=right><b>GST</b></td><td align=right>" + (dGST).ToString("c") + "</td></tr>");
	Response.Write("<tr><td><b>&nbsp;Note : </td><td colspan=2 align=right>");
	
	Response.Write("<b>Sub Total</b></td><td align=right");
	if(m_bSales) //display cost and margin
	{
		Response.Write(" title='" + (m_dTotalCost*1.125).ToString("c") + ", " + dMargin.ToString() + "%'");
	}
	Response.Write(">");

	if(m_bSales)
		Response.Write("<input type=text name=subtotal size=7 style='text-align:right' value='");
	else
		Response.Write(dSubTotal.ToString("c") + "<input type=hidden name=subtotal value='");
	
	Response.Write(dSubTotal.ToString("c"));
	Response.Write("' OnChange=\"if(!parseFloat(document.form1.subtotal.value)){document.form1.subtotal.value="+ dSubTotal +";}\">");

	Response.Write("<input type=hidden name=subtotal_old value=" + dSubTotal.ToString("c") + ">");
	Response.Write("</td></tr>");

	//note
	Response.Write("<tr><td colspan=4><textarea name=quote_note rows=5 cols=90>" + m_quoteNote + "</textarea></td></tr>");

	Response.Write("<tr><td ");
	if(m_bSales)
	{
		Response.Write(" align=center><a href=q.aspx?ssid=" + m_ssid);
		if(m_bShowCost)
			Response.Write("&sc=0");
		Response.Write("><font color=#EEEEEE>&copy;EZNZ CORP</font></a></td><td colspan=3 ");
	}
	else
		Response.Write("colspan=4 ");
	Response.Write(" align=right>");

//DEBUG("created=" + m_bQuoteCreated.ToString(), ", type="+m_quoteType);
	//default button
	Response.Write("<input type=submit name=cmd DEFAULT=\"True\" value='Recalculate Price' " + Session["button_style"] + ">");
	if(m_sSite == "admin")
	{
		Response.Write("<input type=submit name=cmd value='Refresh Cache' " + Session["button_style"] + ">");
		//if(g_bUseSystemQuotation)
	//	Response.Write("<input type=button value='Use Order Interface' onclick=window.location=('pos.aspx?ssid=" + m_ssid + "&q="+ m_ssid +"') " + Session["button_style"] + ">");
		Response.Write("<input type=button value='Add More Parts' onclick=window.location=('search.aspx?ssid=" + m_ssid + "&q="+ m_ssid +"') " + Session["button_style"] + ">");
	}
	else if(m_bQuoteCreated)
	{
		if(m_quoteType == "1")
		{
			Response.Write("<input type=submit name=cmd value='Place Order' ");
			Response.Write("style=\"font-size:8pt;font-weight:bold;background-color:red;color:yellow;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\"");
			Response.Write(">");
		}
		else
		{
			Response.Write("<input type=submit name=cmd value='Unlock Order' ");
			Response.Write("style=\"font-size:8pt;font-weight:bold;background-color:red;color:yellow;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\"");
			Response.Write(">");
		}
	}

	if(m_bQuoteCreated)
	Response.Write("<input type=button name=cmd1 value='View Printable Quote' " + Session["button_style"] + " onclick=\"window.open('invoice.aspx?id="+m_quoteNumber+"&t=order')\">");
		//Response.Write("<input type=submit name=cmd value='View Printable Quote' " + Session["button_style"] + ">");
		
	Response.Write("<input type=hidden name=postback value=yes></td></tr>");
	Response.Write("<tr><td colspan=4 align=right>");

	Response.Write("<font color=red><b>Please tick : </b></font>");

	Response.Write("<input type=checkbox name=h_install ");
	Response.Write(" onclick=\"document.form1.ship_as_parts.checked = !this.checked; if(!this.checked){document.form1.s_install.checked=false;}\"");
	if(!m_bShipAsParts)
		Response.Write(" checked");
	Response.Write(">Hardware Installation &nbsp&nbsp; ");

	Response.Write("<input type=checkbox name=s_install");
	Response.Write(" onclick=\"if(this.checked){document.form1.ship_as_parts.checked = false; document.form1.h_install.checked=true;}\"");
	if(m_bInstallOS)
		Response.Write(" checked");
	Response.Write(">Software Installation &nbsp&nbsp; ");

	Response.Write("<input type=checkbox name=ship_as_parts ");
	Response.Write(" onclick=\"document.form1.h_install.checked = !this.checked; document.form1.s_install.checked = !this.checked; if(!this.checked){document.form1.s_install.checked=false;}\"");
	if(m_bShipAsParts)
		Response.Write(" checked");
	Response.Write(">Supply parts only(No installation)&nbsp&nbsp; ");

	if(!m_bAdmin)
		Response.Write("<b>PO# : </b><input type=text size=10 name=cust_po value='" + m_custPONumber + "'>&nbsp&nbsp;");
	
	Response.Write("</td></tr>");

	if(m_bSales)
	{
		Response.Write("<tr><td colspan=4 align=right>");
		if(m_bQuoteCreated)
		{
			Response.Write("<input type=checkbox name=confirm_delete>Tick to delete");
			Response.Write("<input type=submit name=cmd value='Delete' " + Session["button_style"] + ">");
			Response.Write("<input type=submit name=cmd value='New Quote' " + Session["button_style"] + ">");
			Response.Write("<input type=submit name=cmd value='Update Quote' " + Session["button_style"] + " ");
			if(!m_bREcalculateClicked)
				Response.Write(" disabled ");
			Response.Write(">");
			Response.Write("<input type=submit name=cmd value='Unlock Order' ");
			Response.Write("style=\"font-size:8pt;font-weight:bold;background-color:red;color:yellow;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\"");
			Response.Write(">");
			Response.Write("<input type=submit name=cmd value='Send Email To' " + Session["button_style"] + ">");
			Response.Write("<input type=text name=customer_email size=30 value='" + m_customerEmail + "'>");
		}
		else
			Response.Write("<input type=submit name=cmd value='Create Quote' " + Session["button_style"] + ">");
		Response.Write("</td></tr>");
	}

	if(!m_bAdmin)
	{
		if(m_dYourTotal == 0)
		{
			if(Request.Form["your_margin_old"] != null && Request.Form["your_margin_old"] != Request.Form["your_margin"])
			{
				string sUmargin = Request.Form["your_margin"];
				if(TSIsDigit(sUmargin))
					m_dYourMargin = MyDoubleParse(Request.Form["your_margin"]) / 100;
				Session["q_your_margin" + m_ssid] = m_dYourMargin;
			}
			else if(Request.Form["your_total_old"] != null && Request.Form["your_total_old"] != Request.Form["your_total"])
			{
				m_dYourTotal = MyMoneyParse(Request.Form["your_total"]);
				m_dYourMargin = Math.Round((m_dYourTotal - dSubTotal) / dSubTotal, 4);
				Session["q_your_margin" + m_ssid] = m_dYourMargin;
			}
			else if(Session["q_your_margin" + m_ssid] != null)
				m_dYourMargin = (double)Session["q_your_margin" + m_ssid];

			m_dYourTotal = Math.Round(dSubTotal * (1 + m_dYourMargin), 2);
//DEBUG("sub=", dSubTotal.ToString());
//DEBUG("youmargin=", m_dYourMargin.ToString());
//DEBUG("youtotal=", m_dYourTotal.ToString());
		}
		else
		{
//DEBUG("youtotal=", m_dYourTotal.ToString());
			m_dYourMargin = Math.Round((m_dYourTotal - dSubTotal) / dSubTotal, 4);
			Session["q_your_margin" + m_ssid] = m_dYourMargin;
		}

		Response.Write("<tr><td colspan=4>");

		Response.Write("<table width=100% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td><font size=+1>For your Customer</font></td></tr>");

		Response.Write("<tr><td><b>Customer Name : </b>");
		Response.Write("<input type=text name=your_customer_name maxlength=50 value='" + m_yourCustomerName + "'>");
		Response.Write("&nbsp&nbsp;");
		Response.Write("<b>Margin : </b>");
		Response.Write("<input type=text name=your_margin size=3 ");
		Response.Write(" style='text-align:right' value='" + (m_dYourMargin * 100).ToString() + "'>%");
		Response.Write("<input type=hidden name=your_margin_old value='" + (m_dYourMargin * 100).ToString() + "'>");
		Response.Write("&nbsp&nbsp;");
		Response.Write("<b>Total : </b></font>");
		Response.Write("<input type=text name=your_total size=7 ");
		Response.Write(" style='text-align:right' value='" + m_dYourTotal.ToString("c") + "'>");
		Response.Write("<input type=hidden name=your_total_old value='" + m_dYourTotal.ToString("c") + "'>");
		Response.Write("</td></tr>");

		Response.Write("<tr><td align=right>");
		if(m_bQuoteCreated)
		{
			Response.Write("<input type=checkbox name=confirm_delete>Tick to delete");
			Response.Write("<input type=submit name=cmd value='Delete' " + Session["button_style"] + ">");
			Response.Write("<input type=submit name=cmd value='New Quote' " + Session["button_style"] + ">");
			Response.Write("<input type=submit name=cmd value='Update Quote' ");
//			Response.Write("style=\"font-size:8pt;font-weight:bold;background-color:red;color:yellow;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\"");
			Response.Write(Session["button_style"]);
			if(!m_bREcalculateClicked)
				Response.Write(" disabled ");
			Response.Write(">");
			Response.Write("<input type=submit name=cmd value='Print Quote' " + Session["button_style"] + ">");
		}
		else
		{
			Response.Write("<input type=submit name=cmd value='Save Quote' " + Session["button_style"] + ">");
		}
		Response.Write("</td></tr>");

		Response.Write("</table>");

		Response.Write("</td></tr>");
	}
	Response.Write("</table>");

	if(!g_bRetailVersion && m_bSayWillAddLaborFee)
	{
		Response.Write("<table align=center border=0>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
		string qs = ReadSitePage("quotation_labour_fee_notice");
		qs = qs.Replace("\r\n", "<br>\r\n");
		Response.Write("<tr><td>");
		Response.Write(qs);
		Response.Write("</td></tr>");
		Response.Write("</table>");
	}

//	if(!m_bAdmin && !m_bSales)
	if(!m_bAdmin && !m_bSales && g_bRetailVersion)
	{
		Response.Write("<table width=90% align=center>");
		Response.Write("<tr><td align=right><input type=image name=cmd value='buy' ");
		Response.Write("src=/i/buy.gif></td></tr>");
		Response.Write("</table><br>");
		Response.Write("<table align=center border=0>");
		Response.Write("<tr><td><font size=+1><b>&#149; Recommend this system to a friend</b></font></td></tr>");
		Response.Write("<tr><td><textarea name=mailbody cols=75 rows=7>");
		Response.Write("</textarea></td></tr>");
		Response.Write("<tr><td align=right><b>Your Friend's Email : </b><input type=text name=email> <input type=submit name=cmd value='Send'></td></tr>");
		Response.Write("<tr><td align=right><input type=checkbox name=ccsales checked>CC this email to our sales</td></tr>");
		Response.Write("<tr><td><b>&#149; <a href=quotation.aspx>Back to QUOTATION+&#153</b></a></td></tr>");
		Response.Write("</table></form>");
	}

	return true;
}

bool GetCustomer()
{
	if(dst.Tables["card"] != null)
		dst.Tables["card"].Clear();

	//prepare customer ID
	string id = "";
	if(m_sSite != "admin")
	{
		if(TS_UserLoggedIn())
		{
			id = Session["card_id"].ToString();
			Session["sales_customerid" + m_ssid] = id;
		}
		else
		{
//			PrintHeaderAndMenu();
			Response.Write("<center><br><h3>Prepare registration form, please wait....(do <a href=login.aspx class=o>login</a> if you already registered)</h3>");
			Response.Write("<meta  http-equiv=\"refresh\" content=\"3; URL=register.aspx\">");
			return false;
		}
	}
	else if(Request.QueryString["ci"] == null)
	{
		if(m_customerEmail == "")
		{
			if(Session["sales_customerid" + m_ssid] == null)
				return true;
			id = Session["sales_customerid" + m_ssid].ToString();
		}
	}
	else if(Request.QueryString["ci"] != "")
	{
		id = Request.QueryString["ci"].ToString();
		Session["sales_customerid" + m_ssid] = id;
		m_bCustomerChanged = true;
	}

	if(id != "")
		m_customerID = id;
	
	//do search
	string sc = "";
	if(m_customerID != "")
		sc = "SELECT *, 1 as shipping_method FROM card WHERE id=" + m_customerID;
	else
		sc = "SELECT *, 1 as shipping_method FROM card WHERE email='" + m_customerEmail + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "card") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//get customer data
	DataRow dr = dst.Tables["card"].Rows[0];
	m_customerName = dr["name"].ToString();
	if(m_customerName == "")
		m_customerName = dr["trading_name"].ToString();
	if(m_customerName == "")
		m_customerName = dr["company"].ToString();

	m_customerLevel = dr["dealer_level"].ToString();
	m_customerEmail = dr["email"].ToString();
	if(m_customerName == "")
		m_customerName = dr["company"].ToString();
	m_owndealer_level = dr["dealer_level"].ToString();

	if(id == "")
	{
		m_customerID = dr["id"].ToString();
		Session["sales_customerid" + m_ssid] = m_customerID;
	}
//	else
//		ChangeAllOptions(); //apply new price for this customer

	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["card"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

bool FormPrintRow(int i)
{
	string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
	if(!IsInteger(qty))
		qty = "1";

	bool bManual = false;
	if(i == m_nManualBox || dtQ.Rows[0][fn[i]].ToString() == "0")
	{
		bManual = true;
		if(dtQ.Rows[0][fn[i]].ToString() == "0")
		{
			if(Request.QueryString["nme"] == i.ToString())
			{
				bManual = false; //restore options, drop manual
			}
		}
		if(bManual && i == 0) //set motherboard and ram and videocard to manual if cpu is manual
		{
			dtQ.Rows[0][fn[1]] = "0";
			dtQ.Rows[0][fn[2]] = "0";
			dtQ.Rows[0][fn[3]] = "0";
			dtQ.AcceptChanges();
		}
	}

	if(Request.QueryString["nme"] == i.ToString())
	{
		dtQ.Rows[0][fn[i] + "_special"] = "0";
	}

	Response.Write("<tr><td>");

//disable this feature, too complicated, DW. 06.May.2003
//	if(m_bSales)
//	{
//		Response.Write("<a href=q.aspx?ssid=" + m_ssid);
//		if(bManual)
//			Response.Write("&n");
//		Response.Write("me=" + i.ToString() + "><b>");
//		Response.Write(fd[i]);
//		Response.Write("</b></a></td><td>");
//	}
//	else

		Response.Write("<b>" + fd[i] + "</b></td><td>");
	if(bManual && m_bSales)
	{
		Response.Write("<input type=text name=name_" + fn[i] + " size=75 value='");
		Response.Write(dtQ.Rows[0][fn[i] + "_name"].ToString());
		Response.Write("'><input type=hidden name=" + fn[i] + " value=0>");
	}
	else if(dtQ.Rows[0][fn[i] + "_special"].ToString() == "1") //special item code, not from quotation system
	{
		string code = dtQ.Rows[0][fn[i]].ToString();
		string name = GetProductDesc(code);
		Response.Write("<input type=text name=name_" + fn[i] + " size=75 value='");
		Response.Write(name);
		Response.Write("'><input type=hidden name=" + fn[i] + " value='");
		Response.Write(code);
		Response.Write("'>");
	}
	else
	{
		if(!PrintQOptions(i, int.Parse(qty)))
			return false;
	}
	Response.Write("</td>");
	
	Response.Write("<td align=right><input type=text size=1 name=" + fn[i] + "_qty style='text-align:right' value=");
	Response.Write(qty);
	Response.Write("></td>");

	if(!PrintPartPrice(fn[i], qty, bManual))
		return false;
	return true;
}

bool PrintQOptions(int index, int nQuantity)
{
	string s = fn[index];
	DataSet dso = new DataSet();

	string cpu = dtQ.Rows[0]["cpu"].ToString();
	string mb = dtQ.Rows[0]["mb"].ToString();
//	string vga = dtQ.Rows[0]["video"].ToString();
//	string ram = dtQ.Rows[0]["ram"].ToString();
	string cn = "qtc_" + s;
	if(s == "mb")
		cn += "_" + cpu;
	else if(s == "ram" || s == "video")
		cn += "_" + mb;
		
	DataTable dtqo = null;
	
	if(Session[cn] != null && Request.Form["cmd"] != "Refresh Cache")
	{
//DEBUG("cache out : ", cn);
		dtqo = (DataTable)Session[cn];
	}
	else
	{
		Session["sysinstall_labour_code"] = null; //refresh labour code as well
		string sc = "";
		if(s == "cpu")
			sc = "SELECT DISTINCT ISNULL(sq.qty,0) AS qty, p.code, p.name, p.price, c.level_rate1,c.manual_cost_nzd, c.rate, c.price1,  p.supplier_price FROM q_mb q JOIN product p ON q.parent=p.code JOIN code_relations c ON c.code=p.code LEFT OUTER JOIN stock_qty sq ON sq.code = c.code AND sq.code = p.code ORDER BY p.name";
		else if(s == "mb")
			sc = "SELECT ISNULL(sq.qty,0) AS qty, q.code, p.name, p.price, c.level_rate1,c.manual_cost_nzd, c.rate, c.price1, p.supplier_price FROM q_mb q JOIN product p ON q.code=p.code JOIN code_relations c ON c.code=p.code LEFT OUTER JOIN stock_qty sq ON sq.code = c.code AND sq.code = p.code  WHERE q.parent=" + cpu + " ORDER BY p.name";
		else if(s == "ram" || s == "video")
			sc = "SELECT ISNULL(sq.qty,0) AS qty, q.code AS code, p.name, c.level_rate1, c.manual_cost_nzd, c.rate, p.price, c.price1, p.supplier_price FROM q_" + s + " q LEFT OUTER JOIN product p ON q.code=p.code JOIN code_relations c ON c.code=p.code LEFT OUTER JOIN stock_qty sq ON sq.code = c.code AND sq.code = p.code WHERE q.parent=" + mb + " ORDER BY p.name";
		else
			sc = "SELECT ISNULL(sq.qty,0) AS qty, q."+s+" AS code, p.name, p.price, c.level_rate1, c.manual_cost_nzd, c.rate, c.price1, p.supplier_price FROM q_flat q LEFT OUTER JOIN product p ON q." + s + "=p.code JOIN code_relations c ON c.code=p.code  LEFT OUTER JOIN stock_qty sq ON sq.code = c.code AND sq.code = p.code WHERE q." + s + ">0 ORDER BY p.name";
//	DEBUG("sc = ",sc);
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dso, s);
			dtqo = dso.Tables[s];
			Session[cn] = dtqo;
//		DEBUG("sdsenscn =", Session[cn].ToString());
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
//DEBUG("s =", s);
	Response.Write("\r\n<a name='" + s + "'></a>");
	Response.Write("\r\n<select name=");
	Response.Write(s);
	if(s == "cpu" || s == "mb")
	{
//		if(!m_bNoReLoad)
		{
			Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=change&k=" + s + "");
			if(m_ssid != "")
				Response.Write("&ssid="+ m_ssid +"");
			Response.Write("&r=" + DateTime.Now.ToOADate() + "&v='+this.options[this.selectedIndex].value");
			if(index > 10)
				Response.Write("+'#cpu'");
			Response.Write(")\"");
		}
	}
	Response.Write(">");

	bool bHasNone = false;
	for(int i=0; i<dtqo.Rows.Count; i++)
	{
//	DEBUG("dos =", dtqo.Rows.Count);
		DataRow dr = dtqo.Rows[i];
		string code = dr["code"].ToString();
		double dCost = 0;
		if(dr["supplier_price"].ToString() != "")
			dCost += double.Parse(dr["supplier_price"].ToString());
		dCost += 1000; //fake the price to make long-neck customer confused
		string cost = dCost.ToString();
		string stock = dr["qty"].ToString();
		if(code == null || code == "")
			code = "-1";
		Response.Write("\r\n<option value='" + code + "'");
		if(code == dtQ.Rows[0][s].ToString())
			Response.Write(" selected");
		Response.Write(">");
		string name = dr["name"].ToString();
		double cost_nzd = double.Parse(dr["manual_cost_nzd"].ToString());
		double b_rate = double.Parse(dr["rate"].ToString());
		double b_price = cost_nzd * b_rate;
		       
		
		if(name != "")
		{
			if(name.Length > 60)
				name = name.Substring(0, 60);
			Response.Write(name);
			if(m_bSales && m_bShowCost)
				Response.Write(" " + cost + ", 0"+stock+", ");
			//double dPrice = double.Parse(dr["price"].ToString());
			  double dPrice = b_price;
			if(dPrice.ToString() == "" || dPrice == 0)
			    dPrice = double.Parse(dr["price1"].ToString())/1.125;   
			//	dPrice = double.Parse(dr["supplier_price"].ToString()) * 1.1;
			double lr1 = MyDoubleParse(dr["level_rate1"].ToString());
			
			if(dPrice.ToString() != "" || dPrice != 0)
			dPrice *= lr1;
			dPrice = Math.Round(dPrice, 2);
			Response.Write(" " + dPrice.ToString("c"));
		}
		else
		{
			if(code != "-1") //delete obsolete item, code="-1" means no video card for that motherboard
			{
				string sc = "";
				if(s == "cpu")
					sc = "DELETE FROM q_" + s + " WHERE parent=" + code;// + " AND parent=" + m_parent;
				else if(s == "mb" || s == "ram" || s == "video")
					sc = "DELETE FROM q_" + s + " WHERE code=" + code;// + " AND parent=" + m_parent;
				else
					sc = "DELETE FROM q_flat WHERE " + s + "=" + code;
//DEBUG("do delete, sc=", sc);
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
				continue;
			}
			else
			{
				Response.Write(m_sNONE);
				bHasNone = true;
			}
		}
		Response.Write("</option>");
	}
	if(!bHasNone)
	{
		Response.Write("<option value=-1");
		if(dtQ.Rows[0][s].ToString() == "-1")
			Response.Write(" selected");
		Response.Write("> No " + fd[index]);
		Response.Write("</option>");
	}
	Response.Write("\r\n</select>");
	return true;
}

bool PrintPartPrice(string s, string sqty, bool bManual)
{
  
	string code = dtQ.Rows[0][s].ToString();

	if(code == null || code == "")
		return true;

	int qty = 0;
	if(sqty != null && sqty != "" && IsInteger(sqty))
		qty = int.Parse(sqty);

	double dPrice = 0;
	dPrice = double.Parse(dtQ.Rows[0][s+"_price"].ToString(), NumberStyles.Currency, null);

//DEBUG("dprice = ", dPrice.ToString());
	if(m_sSite.ToLower() != "admin")
		dPrice = dGetDealerPrice(code, m_owndealer_level);
//DEBUG("dprice2 = ", dPrice.ToString());
	double dsupplierPrice = 0;

	if(!bManual)
	{
		if(code != "" && code != null)
			if(GetSupplierPrice(code, ref dsupplierPrice))
				m_dTotalCost += dsupplierPrice * int.Parse(sqty); //calc total cost for later display
	}
	if(dPrice <=0 || dPrice.ToString() == "0")
		dPrice = dsupplierPrice * 1.1 * m_dealerRate;

	m_dTotal += dPrice*qty;

	Response.Write("<td align=right ");
	if(m_bSales)
	{
		Response.Write("title='" + dsupplierPrice.ToString("c") + "'>");
		Response.Write("<input type=text size=7 style='text-align=right' name=" + s + "_price value='");
		Response.Write(dPrice.ToString("c"));
		Response.Write("'><input type=hidden name="+s+"_price_old value='");
		Response.Write(dPrice.ToString("c"));
		Response.Write("'>");
	}
	else
	{
		Response.Write(">");
		Response.Write(dPrice.ToString("c"));
	}
	Response.Write("</td></tr>");
	return true;
}

double dGetDealerPrice(string code, string dealer_level)
{
		DataSet dso = new DataSet();
	double dPrice = 0;
	string sc = " SELECT distinct manual_cost_nzd, rate, level_rate1, level_rate2, level_rate3, level_rate4, level_rate5, level_rate6, level_rate7, level_rate8, level_rate9";
	sc += " ,  nzd_freight ";
	sc += " FROM code_relations WHERE code = "+ code;
//DEBUG("sc = ", sc);
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso, "dealerPrice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}
//DEBUG("roes = ", rows);
	if(rows == 1)
	{

		dPrice = MyDoubleParse(dso.Tables["dealerPrice"].Rows[0]["manual_cost_nzd"].ToString());
		double drate = MyDoubleParse(dso.Tables["dealerPrice"].Rows[0]["rate"].ToString());
		double dDealerRate = MyDoubleParse(dso.Tables["dealerPrice"].Rows[0]["level_rate"+ dealer_level].ToString());
		m_dealerRate = dDealerRate;
		dPrice = (dPrice * drate * dDealerRate) + MyDoubleParse(dso.Tables["dealerPrice"].Rows[0]["nzd_freight"].ToString());;
	}
	return dPrice;
}

void BindDebugGrid(DataTable dtd)
{
//	DataView source = new DataView(dtQ);
	DataView source = new DataView(dtd);
	MyDebugDataGrid.DataSource = source ;
	MyDebugDataGrid.DataBind();
}

bool DoBuild()
{
	for(int i=0; i<m_qfields; i++)
	{
		string s = Request.QueryString[fn[i]];
		if(Request.QueryString[fn[i]] == null)
			s = "-1";
//if(s != "0")
//DEBUG(fn[i], "=" + s + " " + GetProductDesc(s));
//else
//DEBUG(fn[i], "=" + s);
		dtQ.Rows[0][fn[i]] = s;
		if(s != "-1")
		{
			if(Request.QueryString[fn[i] + "qty"] != null)
				dtQ.Rows[0][fn[i] + "_qty"] = Request.QueryString[fn[i] + "qty"];
			else
				dtQ.Rows[0][fn[i] + "_qty"] = "1";
			double dPrice = 0;
			if(!GetItemPrice(s, dtQ.Rows[0][fn[i] + "_qty"].ToString(), ref dPrice))
				return false;
			dtQ.Rows[0][fn[i] + "_price"] = dPrice.ToString();
		}
		else
			dtQ.Rows[0][fn[i] + "_qty"] = "0";

	}
	return true;
}

//nPriceIndex: 0 means cheapest, -1 means most expensive
bool GetPartByPrice(string s, int nPriceIndex, ref string sCode)
{
	DataSet dso = new DataSet();
	int rows = 0;

	string sc = "";
	if(s == "cpu")
		sc = "SELECT DISTINCT p.code, p.price FROM q_mb q JOIN product p ON q.parent=p.code ORDER BY p.price";
	else if(s == "monitor")
		sc = "SELECT p.code, p.name, p.price FROM q_flat q LEFT OUTER JOIN product p ON q.monitor=p.code WHERE q.monitor>0 ORDER BY p.code";
		
	
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
	if(nPriceIndex < rows)
	{
		sCode = dso.Tables[0].Rows[nPriceIndex]["code"].ToString();
		return true;
	}
	return false;
}

bool DoCustomerSearchAndList()
{
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		m_ssid = Request.QueryString["ssid"];
	string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%%'";
	string sc = "SELECT *, '" + uri + "' + '&ci=' + LTRIM(STR(id)) AS uri FROM card ";
	sc += " WHERE (name LIKE " + kw + " OR email LIKE " + kw + " OR company LIKE " + kw + ")";
	sc += " AND type <> 6 ";
	sc += " ORDER BY name";
//DEBUG("sc=", sc);
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
	if(rows == 1)
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
		Response.Write("URL=q.aspx?ci=" + dst.Tables["card"].Rows[0]["id"].ToString() + "&ssid="+ m_ssid +"\">");
		return false;
	}
	BindGrid();
	Response.Write("<center><h3>Search for Customer</h3></center>");
	Response.Write("<form id=search action=" + uri + " method=post>");
//	Response.Write("<input type=hidden name=invoice_number value=" + m_quoteNumber + ">");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
//	Response.Write("<input type=button onclick=window.location=('q.aspx?ssid=" + m_ssid + "') value=Cancel " + Session["button_style"] + ">");
//	Response.Write("<input type=button onclick=window.location=('q.aspx?ssid=" + m_ssid + "&ci=0') value='Cash Sales' " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=window.location=('q.aspx?");
	if(m_ssid != "")
		Response.Write("ssid="+ m_ssid);
	Response.Write("') value=Cancel " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=window.location=('q.aspx?ci=0");
	if(m_ssid != "")
		Response.Write("&ssid="+ m_ssid);
	Response.Write("') value='Cash Sales' " + Session["button_style"] + ">");
	string luri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
	Response.Write("<input type=button onclick=window.location=('ecard.aspx?n=customer&a=new&ref=quotation&luri="+ HttpUtility.UrlEncode(luri) +"') value='New Customer' " + Session["button_style"] + ">");
//	Response.Write("<input type=button onclick=window.open('ecard.aspx?n=customer&a=new&ref=quotation') value='New Customer' " + Session["button_style"] + ">");
	Response.Write("</td></tr></table></form>");

	return true;
}

void MyDataGrid_PageA(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

bool DoDeleteQuote()
{
	if(Request.Form["confirm_delete"] != "on")
	{
		Response.Write("<br><br><center><h3>Please tick 'Confirm to delete'</h3>");
		Response.Write("<input type=button value='Back' onclick=history.go(-1) " + Session["button_style"] + ">");
		return false;
	}

	if(!DeleteOrderItems(m_quoteNumber))
		return false;

	string sc = " DELETE FROM orders WHERE id = " + m_quoteNumber;
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

bool DeleteOrderItems(string id)
{
	string sc = "";
	int items = 0;
	sc = " SELECT o.type, i.code, i.quantity FROM order_item i JOIN orders o ON o.id=i.id WHERE i.id=" + id;
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

	if(items > 0)
	{
		if(dst.Tables["delete_items"].Rows[0]["type"].ToString() == "2") //was order, do deallocate
		{
			for(int i=0; i<items; i++)
			{
				DataRow dr = dst.Tables["delete_items"].Rows[i];
				string code = dr["code"].ToString();
				string sqty = dr["quantity"].ToString();
				int nqty = MyIntParse(sqty);
				sc += " Update stock_qty SET ";
				sc += " allocated_stock = allocated_stock - " + sqty;
				sc += " WHERE code=" + dr["code"].ToString() + " AND branch_id = " + m_branchID;
				sc += " UPDATE product SET allocated_stock = allocated_stock - " + sqty + " WHERE code=" + code;
			}
		}
	}
	sc += " DELETE FROM order_item WHERE id=" + id;
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
	
	string sCompany = "";
	string sAddr = "";
	string sContact = "";

	if(m_bAdmin)
	{
		//bill to
		Response.Write("<table><tr><td>");
		Response.Write("<b>Bill To : <br></b>");

		if(!bCashSales)
		{
			sCompany = dr["trading_name"].ToString();
			sAddr += dr["postal1"].ToString() + "<br>";
			sAddr += dr["postal2"].ToString() + "<br>";
			sAddr += dr["postal3"].ToString() + "<br>";

			Response.Write(sCompany);
			Response.Write("<br>\r\n");
			Response.Write(sAddr);
			Response.Write("<br>\r\n");
		}
		Response.Write("</td></tr></table>");
	}

	Response.Write("</td><td valign=top align=right>");
	
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
	Response.Write("Pick Up Time : <input type=text size=10 name=pickup_time maxlength=49 value=\"" + m_pickupTime + "\">");
	Response.Write("</td></tr></table>");

	Response.Write("</td>");
	Response.Write("<td valign=top>");

	Response.Write("<table id=tShipTo");
	if(m_nShippingMethod == "1") //pickup
		Response.Write(" style='visibility:hidden' ");
	Response.Write(">");

	if(!bCashSales)
	{
		sAddr = dr["Address1"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address2"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address3"].ToString();
		sCompany = dr["trading_name"].ToString();
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
	Response.Write("<textarea name=special_ship_to_addr cols=20 rows=4>");
	Response.Write(m_specialShiptoAddr);
	Response.Write("</textarea>");
	Response.Write("</td></tr></table>");

	Response.Write("</td></tr></table>");

	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
	//end of ship to

	Response.Write("</td></tr></table>");
	//end of bill and shipto table
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
	Response.Write("		document.all('tshiptoaddr').style.top = 10;			");
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

void UpdateAllFields()
{
	if(Request.Form["your_customer_name"] != null)
	{
		m_yourCustomerName = Request.Form["your_customer_name"];
		Session["q_your_customer_name" + m_ssid] = m_yourCustomerName;
	}

	if(Request.Form["s_install"] == "on")
		m_bInstallOS = true;
	else
		m_bInstallOS = false;
	Session["q_install_os" + m_ssid] = m_bInstallOS;

	if(Request.Form["ship_as_parts"] == "on")
		m_bShipAsParts = true;
	else
		m_bShipAsParts = false;
	Session["q_ship_as_parts" + m_ssid] = m_bShipAsParts;

	if(Request.Form["customer"] != null)
	{
		m_customerID = Request.Form["customer"];
		Session["sales_current_customer_id" + m_ssid] = m_customerID;
	}

	if(Request.Form["cust_po"] != null)
	{
		m_custPONumber = EncodeQuote(Request.Form["cust_po"]);
		Session["sales_customer_po_number" + m_ssid] = m_custPONumber;
	}
	if(Request.Form["payment_type"] != null)
	{
		m_paymentType = EncodeQuote(Request.Form["payment_type"]);
		Session["quote_payment_type" + m_ssid] = m_paymentType;
	}
	if(Request.Form["quote_note"] != null)
	{
		m_quoteNote = Request.Form["quote_note"];
		Session["quote_note" + m_ssid] = m_quoteNote;
	}

	if(Request.Form["shipping_method"] != null)
	{
		m_nShippingMethod = Request.Form["shipping_method"];
		m_specialShipto = "0";
		if(Request.Form["special_shipto"] == "on")
			m_specialShipto = "1";
		m_specialShiptoAddr = Request.Form["special_ship_to_addr"];
		m_pickupTime = EncodeQuote(Request.Form["pickup_time"]);
		if(m_pickupTime != null)
		{
			if(m_pickupTime.Length > 49)
				m_pickupTime = m_pickupTime.Substring(0, 49);
		}
		else
			m_pickupTime = "";
		if(Request.Form["freight"] != null && Request.Form["freight"] != "")
		Session["sales_shipping_method" + m_ssid] = m_nShippingMethod;
		Session["sales_special_shipto" + m_ssid] = m_specialShipto;
		Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
		Session["sales_pick_up_time" + m_ssid] = m_pickupTime;
	}

	if(Request.Form["freight"] != null)
		m_dFreight = MyMoneyParse(Request.Form["freight"]);

	if(Request.Form["sales"] != null)
	{
		m_sales = Request.Form["sales"];
		if(m_sales != "")
		{
			DataRow drc = GetCardData(m_sales);
			if(drc != null)
				m_salesName = drc["name"].ToString();
		}
	}

	if(Request.Form["gst_inclusive"] == "on")
		Session["quotation_display_include_gst" + m_ssid] = true;
	else
		Session["quotation_display_include_gst" + m_ssid] = null;
	
	//price setting
	if(Request.Form["nip"] == "on")
		Session["no_individual_price" + m_ssid] = "true";
	else
		Session["no_individual_price" + m_ssid] = null;

	if(Session["no_individual_price" + m_ssid] != null)
		m_bNoIndividualPrice = true;
	else
		m_bNoIndividualPrice = false;

	if(Request.Form["type"] != null)
		m_quoteType = Request.Form["type"];
}

void RestoreAllFields()
{
	if(Session["q_install_os" + m_ssid] != null)
		m_bInstallOS = (bool)Session["q_install_os" + m_ssid];
	if(Session["q_your_customer_name" + m_ssid] != null)
		m_yourCustomerName = Session["q_your_customer_name" + m_ssid].ToString();
	if(Session["q_ship_as_parts" + m_ssid] != null)
		m_bShipAsParts = (bool)Session["q_ship_as_parts" + m_ssid];
	if(Session["sales_customer_po_number" + m_ssid] != null)
		m_custPONumber = Session["sales_customer_po_number" + m_ssid].ToString();
	if(Session["quote_payment_type" + m_ssid] != null)
		m_paymentType = Session["quote_payment_type" + m_ssid].ToString();
	if(Session["quote_note" + m_ssid] != null)
		m_quoteNote = Session["quote_note" + m_ssid].ToString();
	if(Session["sales_current_customer_id" + m_ssid] != null)
		m_customerID = Session["sales_current_customer_id" + m_ssid].ToString();
	if(Session["sales_shipping_method" + m_ssid] != null)
		m_nShippingMethod = Session["sales_shipping_method" + m_ssid].ToString();
	if(Session["sales_special_shipto" + m_ssid] != null)
		m_specialShipto = Session["sales_special_shipto" + m_ssid].ToString();
	if(Session["sales_special_ship_to_addr" + m_ssid] != null)
		m_specialShiptoAddr = Session["sales_special_ship_to_addr" + m_ssid].ToString();
	if(Session["sales_pick_up_time" + m_ssid] != null)
		m_pickupTime = Session["sales_pick_up_time" + m_ssid].ToString();
	if(Session["sales_freight" + m_ssid] != null)
		m_dFreight = (double)Session["sales_freight" + m_ssid];
	if(Session["quote_sales_id" + m_ssid] != null)
		m_sales = Session["quote_sales_id" + m_ssid].ToString();
	if(Session["quote_sales_name" + m_ssid] != null)
		m_salesName = Session["quote_sales_name" + m_ssid].ToString();
	if(Session["quotation_display_include_gst" + m_ssid] != null)
		m_bIncludeGST = true;
	if(Session["no_individual_price" + m_ssid] != null)
		m_bNoIndividualPrice = true;
}

void UpdateAllPrice()
{
	CheckShoppingCart();
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;
		if(dtCart.Rows[i]["system"].ToString() == "1")
			continue;

		DataRow dr = dtCart.Rows[i];
		string code = dr["code"].ToString();

		if(Request.Form["qty_" + code] != null)
		{
			string qty = Request.Form["qty_" + code];
			if(qty == "0")
			{
				dtCart.Rows.RemoveAt(i);
				continue;
			}
			
			//update price if changed
			string price_new = Request.Form["price_" + code];
			dtCart.AcceptChanges(); //Commits all the changes made to this row since the last time AcceptChanges was called
			dtCart.Rows[i].BeginEdit();
			dtCart.Rows[i]["salesPrice"] = price_new;
			dtCart.Rows[i]["quantity"] = qty;
			dtCart.Rows[i].EndEdit();			
		}
	}
}

bool AddDealerDraftColumn()
{
	string sc = "";
	sc += " ALTER TABLE orders ADD dealer_draft bit NOT NULL DEFAULT 0 WITH VALUES ";
	sc += " ALTER TABLE orders ADD ship_as_parts bit NOT NULL DEFAULT 0 WITH VALUES ";
	sc += " ALTER TABLE orders ADD dealer_customer_name varchar(50) NULL ";
	sc += " ALTER TABLE orders ADD dealer_total money NOT NULL DEFAULT 0 WITH VALUES ";
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

bool UnLockOrder(string orderID)
{
	if(orderID == "")
		return true;

	String sc = "UPDATE orders SET locked_by=null, time_locked=null WHERE id=" + orderID;
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

</script>

<asp:DataGrid id=MyDebugDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=Tan
	CellPadding=5 
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=460px 
	HorizontalAlign=center>
</asp:DataGrid>

<form runat=server>
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
	PageSize=20
	PagerStyle-PageButtonCount=20
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_PageA
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=ID
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=id/>
		<asp:HyperLinkColumn
			 HeaderText=Name
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=Company
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=company/>
		<asp:HyperLinkColumn
			 HeaderText=Trading_Name
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=trading_name/>
		<asp:HyperLinkColumn
			 HeaderText=Edit
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="ecard.aspx?id={0}"
			 Text=Edit/>
	</Columns>
</asp:DataGrid>

<div align=right><asp:Label id=LOKButton runat=server/></div>
</form>
<asp:Label id=LFooter runat=server/>
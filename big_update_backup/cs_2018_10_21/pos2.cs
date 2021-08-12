<!-- #include file="kit_fun.cs" -->
<!-- #include file="sales_function.cs" -->
<!-- #include file="credit_limit.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
const int m_cols = 7;	//how many columns main table has, used to write colspan=
string m_tableTitle = "";
string m_invoiceNumber = "";
string m_comment = "";	//Sales Comment in Invoice Table;
string m_custpo = "";
string m_custdn = "";
string m_cCustGst = "";
string m_cSales = "";
string m_sOrderDate = "";
string m_salesNote = "";
string m_branchID = "1";
string m_sSalesType = ""; //as string, m_quoteType is receipt_type ID
string m_nShippingMethod = "1";
string m_specialShipto = "0";
string m_specialShiptoAddr = ""; //special
string m_pickupTime = "";
string m_sCurrencyName="NZD";
string m_orderStatus = "1"; //Being Processed
string m_sCheckedDeliveryOption = "0";
double m_dFreight = 0;
double m_dCustomerGst = 0;
int m_nSearchReturn = 0;
string m_shipping = "1";

bool m_bEnableCopyOrderFunction = false;

double m_dCreditsBalance = 0;
bool b_create = false;
bool m_bCreditReturn = false;
bool m_bOrderCreated = false;
bool m_bCreditTermsOK = true;
bool m_bNoIndividualPrice = false;
bool m_bUseBarcodeInSalesMode = false;
bool m_bUseLastSalesFixedPrice = false;
bool m_bSetDefaultItemPriceWithGST = false;
double m_dSetMaximumDiscountPerItem = 30;
bool m_GSTSession = false;

string tableWidth = "97%";
string m_sSetItemPriceWithGST = "false";

/***
modified on: 10-04-06 tee
purpose:	to support barcode scaning, this to enable to use barcode in sales mode
			default setting is off, switch on when need it.
variable used: m_bUseBarcodeInSalessMode
***/

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;
	
	m_bEnableCopyOrderFunction = MyBooleanParse(GetSiteSettings("enable_copy_sales_order_function", "0", true));
	m_bUseBarcodeInSalesMode = MyBooleanParse(GetSiteSettings("Enable_Barcode_In_Sales_Mode", "0"));
	m_bSetDefaultItemPriceWithGST = MyBooleanParse(GetSiteSettings("Set_Default_Item_Price_With_GST", "1"));
	m_dSetMaximumDiscountPerItem = MyDoubleParse(GetSiteSettings("Set_Maximum_Discount_Per_Item_In_Sales", "30"));

	//sales session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
		if(Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] == null)
			PrepareNewSales();
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		PrepareNewSales();
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("pos.aspx" + par);		
		return;
	}
	if(Request.QueryString["gst"] != null && Request.QueryString["gst"] != "")
	{
		m_sSetItemPriceWithGST = Request.QueryString["gst"];
		try
		{
			m_sSetItemPriceWithGST = bool.Parse(m_sSetItemPriceWithGST).ToString();

		}
		catch (Exception e) 
		{ 
			m_sSetItemPriceWithGST = "false";
		} 		
		Session[m_sCompanyName + "gst_onoff_for_pos"] = m_sSetItemPriceWithGST;
	}
	else
	{
		if(Session[m_sCompanyName + "gst_onoff_for_pos"] == null)
		{
			if(m_bSetDefaultItemPriceWithGST)
				Session[m_sCompanyName + "gst_onoff_for_pos"] = "true";
			else
				Session[m_sCompanyName + "gst_onoff_for_pos"] = "false";			
		}
	}
     
	m_sCurrencyName=GetSiteSettings("default_currency_name", "NZD");
	
	if(Session["customergst" + m_ssid] != null)
	{
	    m_cCustGst = Session["customergst" + m_ssid].ToString();
		m_dCustomerGst = double.Parse(m_cCustGst);
	}
	//else
	//	m_dCustomerGst =  MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;
	
	m_dGstRate = m_dCustomerGst;// MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;					//26.06.2003 NEO	
	
	if(Request.QueryString["t"] == "created")
	{
		m_orderID = Request.QueryString["id"];
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>" + m_sSalesType.ToUpper() + Lang("Created")+"</h3>");
		Response.Write("<h5>"+Lang("Number")+" : </h5><h1><font color=red>" + m_orderID + "</h1><br>");
		Response.Write("<input type=button " );
		Response.Write("onclick=window.open('invoice.aspx?t=order&id=" + m_orderID + "') ");
		Response.Write(" value='"+Lang("Print")+" ' "+ Session["button_style"] +"><br><br><br>");
		Response.Write("<input type=button " );
		Response.Write("onclick=window.location=('eorder.aspx?id=" + m_orderID + "&r=" + DateTime.Now.ToOADate() + "') ");
		Response.Write(" value='"+Lang("Process")+"' "+ Session["button_style"]+">");
		Response.Write("<br><br><br><br><br>");
		//PrintSearchForm();
		LFooter.Text = m_sAdminFooter;
		return;
	}	
	else if(Request.QueryString["p"] == "new")
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
		Session["SalesType" + m_ssid] = m_quoteType;
	}

	m_bOrder = true;
	Session[m_sCompanyName + "_ordering"] = true;
	Session[m_sCompanyName + "_salestype"] = "sales";

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

	m_sales = Session["card_id"].ToString(); //default

	//remember everything entered in Session Object
	if(Request.Form["branch"] != null)
		UpdateAllFields();

	RestoreAllFields();

	//customer account search
	if(Request.Form["branch"] != null && Request.Form["ckw"] != m_customerID)
	{
		DoCustomerSearchAndList();
		return;
	}
	
	//item search
	if(Request.Form["item_code_search"] != null && Request.Form["item_code_search"] != "")
	{

		Session["sales_customer_po_number" + m_ssid] = m_custpo;
		Session["sales_note" + m_ssid] = m_salesNote;
		Session["delivery_number"+m_ssid] =m_custdn ;  // ch
		Session["sales" + m_ssid] = m_cSales;  // ch
	    Session["customergst" + m_ssid]=m_cCustGst ;
		  Session["ordered_date" + m_ssid]=m_sOrderDate ;

//******************* Code for serial search ************************************
		string s_SearchMsg = "";	
		s_SearchMsg = DoSerialSearch(Request.Form["item_code_search"]);
//*******************************************************************************

//		string s_SearchMsg = "notfound";
		if(s_SearchMsg == "found")
		{
			//found product by serial;
			//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
			Response.Redirect(s_url);
			return;
		}
		else if(s_SearchMsg == "notfound")
		{	
			bool bInteger = false;
			try
			{
				int.Parse(Request.Form["item_code_search"]);
				bInteger = true;
			}
			catch(Exception ei)
			{
				//ignore, not integer
			}

			if(bInteger)
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
						PrintAdminHeader();
						PrintAdminMenu();
						Response.Write("<br><br><center><h3>"+Lang("No Item's code matches")+" <b>" + Request.Form["supplier_code_search"] + "</b></h3>");
						Response.Write("<input type=button "+ Session["button_style"] +" value="+Lang("Back")+" onclick=window.location=('pos.aspx?ssid=" + m_ssid + "')>");
						return;
					}

					PrintSearchForm();
					LFooter.Text = m_sAdminFooter;
					return;
				}
			}
			else
			{
				DoMPNSearch(Request.Form["item_code_search"]);
				if(m_nSearchReturn <= 0)
				{
					PrintAdminHeader();
					PrintAdminMenu();
					Response.Write("<br><br><center><b>"+Lang("Search Result of")+"  <font size=+1 color=red>" + Request.Form["item_code_search"] + "</b></font>");
					Response.Write("<br><br><b>"+Lang("as S/N : Not Found")+"!<br><br></b>");
					Response.Write("<b>"+Lang("as product code : Not Found --- Not Valid Product Code")+"!</b><br>");
					Response.Write("<b>"+Lang("as supplier code : Not Found --- Not Valid Product Code")+"!</b>");
				}
				return;
			}
		}
		else
		{
			Response.Write("<br><br><h3><b><font size=+1>"+Lang("Error")+":<font><br><br><br>" +Lang(s_SearchMsg)+ "</b></h3>");
			return;
		}

		//only one matches, add to cart, refresh sales
		//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
		Response.Redirect(s_url);
		return;
	}	

	if(Request.QueryString["a"] == "add")
	{
		string code = Request.QueryString["code"];
		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		
		if(bCheckCustQuotes(m_customerID))
	    {
			string m_GetQuotePrice = getQuotePrice(m_customerID, code);
		    if(m_GetQuotePrice != "0")
				dSalesPrice = double.Parse(m_GetQuotePrice);
		}
		AddToCart(code, Request.QueryString["supplier"], Request.QueryString["supplier_code"], "1", Request.QueryString["pri"], dSalesPrice.ToString(), "", "");
		Response.Redirect("pos.aspx?ssid=" + m_ssid + "");
		//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos.aspx?ssid=" + m_ssid + "\">");
		return;
	}

	if(m_sSalesType == "" && m_quoteType != "")
		m_sSalesType = GetEnumValue("receipt_type", m_quoteType);

	if(Request.QueryString["t"] == "del")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		bool bKeyOK = true;
		if(Session["delete_order_key" + m_ssid] == null)
			bKeyOK = false;
		else if(Session["delete_order_key" + m_ssid].ToString() != Request.QueryString["r"])
			bKeyOK = false;

		if(!bKeyOK)
		{
			Response.Write("<br><br><center><h3>"+Lang("Please follow the proper link to delete order")+".</h3>");
		}
		else
		{
			m_orderID = Request.QueryString["id"];
			if(m_orderID == null || m_orderID == "")
			{
				Response.Write("<br><br><center><h3>"+Lang("Bad order number")+".</h3>");
			}
			else if(DoDeleteOrder())
			{
				Session["delete_order_key" + m_ssid] = null;
				Response.Write("<br><br><center><h3>"+Lang("Order/Quote Deleted")+".</h3>");
				Response.Write("<input type=button "+ Session["button_style"] +" onclick=window.location=('");
				Response.Write("olist.aspx?t=1&r=" + DateTime.Now.ToOADate() + "') value='"+Lang("Back to Order List")+"'></center>");
			}
			else
			{
				Response.Write("<br><br><h3>"+Lang("Error Deleting Order"));
			}
		}
		LFooter.Text = m_sFooter;
		return;
	}
	else if(Request.QueryString["t"] == "vpq")
	{
		m_bOrder = false;
		Response.Write(BuildQInvoice("", m_quoteType, true, false, m_custpo, m_salesNote));//no email msg, inocie only, no system
		m_bOrder = true;
		return;
	}	

	bool bJustRestored = false;
	
	if(m_orderID != "")
		m_bOrderCreated = true;

	CheckShoppingCart();
	CheckUserTable();	//get user details if logged on


	//get old invoice
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_orderID = Request.QueryString["id"];

		if(!IsInteger(m_orderID))
		{
			Response.Write("<h3>"+Lang("ERROR, WRONG NUMBER")+"</h3>");
			return;
		}
		if(!RestoreOrder())
			return;
	
		//Credit Note from here
		if(Request.QueryString["t"] == "credit")
		{
			m_dFreight = 0 - m_dFreight;
			m_quoteType = GetEnumID("receipt_type", "credit notes");
			//m_salesNote = "credit for order #" + m_orderNumber;
			m_salesNote = "credit for invoice #" + m_invoiceNumber;
			Session["sales_note"] = m_salesNote;
			m_bOrderCreated = false;
			Session["order_created"] = null;
		}
		else
		{
			m_bOrderCreated = true;
		//	m_quoteType = GetEnumID("receipt_type", "order");
			m_quoteType = GetEnumID("receipt_type", m_sSalesType);

			Session["order_created" + m_ssid] = true;
			Session["SalesType" + m_ssid] = m_quoteType;
			Session["EditingOrder" + m_ssid] = true;
			Session["sales_current_order_number" + m_ssid] = m_orderNumber;
			Session["sales_current_order_id" + m_ssid] = m_orderID;
		}
		//Credit Note from here
		m_sSalesType = GetEnumValue("receipt_type", m_quoteType);
		Session["SalesType"] = m_quoteType;

	}
	else if(Request.QueryString["ci"] != null)
	{
		m_customerID = Request.QueryString["ci"];
		Session[m_sCompanyName + "_dealer_card_id" + m_ssid] = m_customerID;
		GetCustomer();	
		ApplyPriceForCustomer();
		//Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=pos.aspx?i=" + m_orderID + "&ssid=" + m_ssid);
		//Response.Write("\">");
		Response.Redirect("pos.aspx?i=" + m_orderID + "&ssid=" + m_ssid);
		return;
	}
	else if(Request.QueryString["search"] == "1")
	{
		DoCustomerSearchAndList();
		return;
	}
	else if(Request.Form["cmd"] == ""+ Lang("Select From Categories") +"")
	{
		//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx?ssid=" + m_ssid + "\">");
		Response.Redirect("c.aspx?ssid=" + m_ssid + "");
		return;
	}
	/*else if(Request.Form["cmd"] == ""+ Lang("Recalculate Price") +"")
	{
		Response.Redirect("pos.aspx?ssid=" + m_ssid);
		return;
	}*/

	else if(Request.Form["cmd"] == ""+ Lang("Update Order") +"" || Request.Form["cmd"] == ""+ Lang("Update Quote") +"")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(DoUpdateOrder())
		{
			if(Request.Form["cmd"] == ""+ Lang("Update Order") +"")
				Response.Write("<br><br><center><h3>"+ Lang("Order Updated") +".</h3>");
			else
				Response.Write("<br><br><center><h3>"+ Lang("Quote Updated") +".</h3>");
			Response.Write("<input type=button  "+ Session["button_style"] +"  onclick=window.location=('");
			Response.Write("olist.aspx?r=" + DateTime.Now.ToOADate() + "') value='"+Lang("Back to Order List")+"'></center>");
		}
		else
		{
			Response.Write("<br><br><h3>"+ Lang("Error updating order") +"");
		}
		PrintAdminFooter();
		return;
	}  

	else if(Request.Form["cmd"] == ""+ Lang("Delete Order") +"" || Request.Form["cmd"] == ""+ Lang("Update Quote") +"")
	{
		PrintHeaderAndMenu();
		string delkey = DateTime.Now.ToOADate().ToString();
		Session["delete_order_key" + m_ssid] = delkey;
		Response.Write("<script Language=javascript");
		Response.Write(">");
//		Response.Write(" rmsg = window.prompt('Are you sure you want to delete this order?')\r\n");
		Response.Write("if(window.confirm('Dear " + Session["name"]);
		if(Request.Form["cmd"] == ""+ Lang("Delete Quote") +"")
		{
			Response.Write("\\r\\n\\r\\"+ Lang("nAre you sure you want to delete this") +""+ Lang("quote") +"?         ");
			Response.Write("\\r\\n"+ Lang("This action cannot be undone") +".\\r\\n");
			Response.Write("\\r\\n"+ Lang("Click OK to delete") +" "+ Lang("quote") +".\\r\\n");
		}
		else
		{
			Response.Write("\\r\\n\\r\\n"+ Lang("Are you sure you want to delete this") +" "+ Lang("order") +"?         ");
			Response.Write("\\r\\n"+ Lang("This action cannot be undone") +".\\r\\n");
			Response.Write("\\r\\n"+ Lang("Click OK to delete") +" "+ Lang("order") +".\\r\\n");
		}
		Response.Write("'))");
		Response.Write("window.location='pos.aspx?t=del&id=" + m_orderID + "&r=" + delkey + "&ssid=" + m_ssid + "';\r\n");
		Response.Write("else window.location='pos.aspx?id=" + m_orderID + "&r=" + delkey + "&ssid=" + m_ssid + "';\r\n");
		Response.Write("</script");
		Response.Write(">");
		return;
	}
	else if(Request.Form["cmd"] == ""+ Lang("Cancel") +"")
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
		Response.Redirect("olist.aspx?r=" + DateTime.Now.ToOADate());
		//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == ""+ Lang("Record") +"")
	{
		dtCart = (DataTable)Session["ShoppingCart" + m_ssid];
		Session["discount_total_"+ m_sCompanyName] = null;
		if(dtCart.Rows.Count <= 0)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>"+ Lang("Error. Cannot create empty order") +"");
			PrintAdminFooter();
			return;
		}

		m_custpo = Request.Form["custpo"];
		m_custdn = Request.Form["custdn"];
		m_cSales = Request.Form["sales"];
		m_cCustGst = Request.Form["custgst"];
		m_sOrderDate = Request.Form["order_date"];
		//m_cOrderDate = Request.Form["o_date"];
		m_custdn = m_custdn.Replace("\"", "-");
		m_custdn = m_custdn.Replace("'", "-");
		m_custpo = m_custpo.Replace("\"", "-");
		m_custpo = m_custpo.Replace("'", "-");
		m_salesNote = Request.Form["note"];
		m_sales = Request.Form["sales"];

		Session["sales_customer_po_number" + m_ssid] = m_custpo;
		Session["sales_note" + m_ssid] = m_salesNote;
		Session["delivery_number"+m_ssid] =m_custdn ;  // ch
		Session["sales" + m_ssid] = m_cSales;  // ch
	    Session["customergst" + m_ssid]=m_cCustGst ;
		Session["ordered_date" + m_ssid]=m_sOrderDate ;

		b_create = true; //MyMoneyParse(Request.Form["totaldue"]) > 0;
		if(b_create)
		{
			if(!CheckBottomPrice())
				return;

			string term_msg = "";
			if(m_sSalesType != "quote")
				m_bCreditTermsOK = CreditTermsOK(m_customerID, ref term_msg);
			if(!CreditLimitOK(m_customerID,  MyDoubleParse(Session["totalAmount"].ToString())))
                return;
            		
			if(!m_bOrderCreated)	//no double record for PageFresh(F5)
			{
				if(!DoCreateOrder(false, m_custpo, m_salesNote)) // false means not system quotation
				{
					Response.Write("<h3>"+ Lang("ERROR CREATING QUOTE") +"</h3>");
					return;
				}
				m_bOrderCreated = true;
				Session["order_created" + m_ssid] = true;
			}

			if(m_bCreditTermsOK)
				Response.Redirect("pos.aspx?t=created&id=" + m_orderID + "&ssid=" + m_ssid);
			//	Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=pos.aspx?t=created&id=" + m_orderID + "&ssid=" + m_ssid + "\">");
			else
			{
				PrintAdminHeader();
				PrintAdminMenu();
				Response.Write(term_msg);
				Response.Write("<br><center><input type=button onclick=window.location='pos.aspx?t=created&id=" + m_orderID + "&ssid=" + m_ssid + "' "+ Session["button_style"] +" value=Continue>");
				PrintAdminFooter();
			}
			return;
		}
	}
	else if(Request.Form["cmd"] == ""+ Lang("Move To Quote") +"")
	{
		Response.Write("<script language=JavaScript");
		Response.Write(">");
		Response.Write("window.location=('q.aspx?ssid=" + m_ssid + "')");
		Response.Write("</script");
		Response.Write(">");
		return;
	}
	else if(Request.Form["cmd"] == ""+ Lang("Change To Order") +"")
	{
		if(DoChangeToOrder())
			Response.Redirect("pos.aspx?id=" + m_orderID + "&ssid=" + m_ssid);
		//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos.aspx?id=" + m_orderID + "&ssid=" + m_ssid + "\">");
		return;
	}
	else if(Request.Form["cmd"] == Lang("Apply Total") || (Request.Form["discount_total"] != null && Request.Form["discount_total"] != "") ||  (Request.Form["discount_total_per"] != "" && Request.Form["discount_total_per"] != null ))
	{
		//Session["discount_total_type_"+ m_sCompanyName] = Request.Form["discount_total_type"];
		GetCustomer();		
		if(DoApplyDiscountTotal(m_dGstRate, MyBooleanParse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString())))
		{
			
			Response.Redirect("pos.aspx?ssid=" + m_ssid);
			//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos.aspx?ssid=" + m_ssid + "\">");
		}
		else
		{
			Response.Write("<cneter>111111111111111111</center>");
		}
	
		
		return;
	}

	if(m_orderNumber != "")
	{
		//Credit Note from here
		if(Request.QueryString["t"] == "credit")
		{
			bCheckCreditNote();
			m_tableTitle = ""+ Lang("Credit Notes") +" - "+ Lang("for invoice") +" #<a title='"+ Lang("view invoice details") +"' href='invoice.aspx?i=" + m_invoiceNumber + "' target=_blank><font color=red>" + m_invoiceNumber + "</font></a>";
			//m_tableTitle = "Credit Notes - for order #<font color=red>" + m_orderNumber + "</font>";
		}
		else
		{
			m_tableTitle = Lang(m_sSalesType.ToUpper()) + " #<font color=red>" + m_orderNumber + "</font>";
			m_tableTitle += " - <font color=green>" +  Lang(GetEnumValue("order_item_status", m_orderStatus)) + "</font>";
		}
	}
	else
		m_tableTitle = "NEW " + Lang(m_sSalesType).ToUpper();

	PrintAdminHeader();
	PrintAdminMenu();
	MyDrawTable();
	LFooter.Text = m_sAdminFooter;
} 
//method Pageload ends

bool RestoreCustomer()
{
	string status_invoiced = GetEnumID("order_item_status", "Invoiced");
	string status_shipped = GetEnumID("order_item_status", "Shipped");
	if(status_invoiced == "")
	{
		Response.Write("<br><br><center><h3>"+ Lang("Error getting status ID") +" '"+ Lang("Being Processed") +"'");
		return false;
	}

	string sc = " SELECT invoice_number, branch, number, po_number, card_id, freight, shipping_method ";
	sc += ", special_shipto, shipto, pick_up_time, status ";
	sc += ", sales, sales_note, locked_by, time_locked, type, no_individual_price, ISNULL(delivery_number, 0 ) AS delivery_number, customer_gst, record_date ";
	sc += " FROM orders ";
//	sc += " WHERE status<>" + status_invoiced + " AND status<>" + status_shipped;
	sc += " WHERE id=" + m_orderID;

	if(Request.QueryString["t"] != "credit")
		sc += " AND status<>" + status_invoiced + " AND status<>" + status_shipped;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "order") <= 0)
		{
			Response.Write("<br><br><center><h3>"+ Lang("ERROR, Order Not Found") +"</h3>");
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
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3><font color=red>"+ Lang("ORDER LOCKED") +"</font></h3><br>");
			Response.Write("<h4>"+ Lang("This order is locked by") +" <font color=blue>" + lockname.ToUpper() + "</font> "+ Lang("since") +" " + locktime);
			PrintAdminFooter();
			return false;
		}
	}
	
	if(Request.QueryString["t"] != "credit")
	{
		
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
	}

	m_orderStatus = dr["status"].ToString();
	m_branchID = dr["branch"].ToString();
	m_orderNumber = dr["number"].ToString();
	m_invoiceNumber = dr["invoice_number"].ToString();
	m_customerID = dr["card_id"].ToString();
	Session["custCardId"] = m_customerID;
	m_customerName = TSGetUserCompanyByID(m_customerID);
	if(m_customerName == "")
		m_customerName = TSGetUserNameByID(m_customerID);
	m_custpo = dr["po_number"].ToString();
	m_sOrderDate = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy");
    m_custdn = dr["delivery_number"].ToString();
	m_cCustGst = dr["customer_gst"].ToString();
	m_dFreight = MyMoneyParse(dr["freight"].ToString());
	m_sales = dr["sales"].ToString();
	m_salesNote = dr["sales_note"].ToString();

	string sst = dr["special_shipto"].ToString();
	if(sst != "" && bool.Parse(sst))
		m_specialShipto = "1";
	m_specialShiptoAddr = dr["shipto"].ToString();
	m_nShippingMethod = dr["shipping_method"].ToString();
	m_pickupTime = dr["pick_up_time"].ToString();
	m_quoteType = dr["type"].ToString();
	m_sSalesType = GetEnumValue("receipt_type", m_quoteType);

	m_bNoIndividualPrice = MyBooleanParse(dr["no_individual_price"].ToString());
	string nip = "0";
	if(m_bNoIndividualPrice)
		nip = "1";

	Session["SalesType" + m_ssid] = m_quoteType;
	Session["sales_freight" + m_ssid] = m_dFreight;
	Session["sales_customerid" + m_ssid] = m_customerID;
	Session["sales_shipping_method" + m_ssid] = m_nShippingMethod ;
	Session["sales_special_shipto" + m_ssid] = m_specialShipto;
	Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
	Session["sales_pick_up_time" + m_ssid] = m_pickupTime;
	Session["sales_no_individual_price" + m_ssid] = nip;
	//restore customer id
	Session[m_sCompanyName + "_dealer_card_id" + m_ssid] = m_customerID;
	dr = GetCardData(m_customerID);
	if(dr != null)
	{
		Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid] = dr["dealer_level"].ToString();
		Session[m_sCompanyName + "_card_type_for_pos" + m_ssid] = dr["type"].ToString();
	}
	
	return true;
} //method RestoreCustomer ends

bool RestoreOrder()
{
	PrepareNewSales(); //empty session sales data 

	if(!RestoreCustomer())
		return false;

	int items = 0;
	string sc = "SELECT discount_percent,* FROM order_item WHERE id=" + m_orderID + " AND kit=0 ORDER BY kid ";
//DEBUG("sc = ",sc);
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
		if(e.ToString().IndexOf("Invalid column name 'discount_percent'")>=0)
		{
			string ssc = " ALTER TABLE dbo.order_item add discount_percent [float] NOT NULL default(0) ";
			ssc += " ALTER TABLE dbo.sales ADD discount_percent [float] NOT NULL default(0) ";
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(ssc, ee);
				return false;
			}			
		}
		ShowExp(sc, e);
		return false;
	}
	
	int i = 0;
	for(i=0; i<items; i++)
	{
		DataRow dr = dst.Tables["items"].Rows[i];
		//Credit Note here
		int qty = MyIntParse(dr["quantity"].ToString());

		if(Request.QueryString["t"] == "credit")
			qty = 0 - qty;

	/*	if(!AddToCart(dr["code"].ToString(), dr["supplier"].ToString(), dr["supplier_code"].ToString(), 
			qty.ToString(), dr["supplier_price"].ToString(), dr["commit_price"].ToString(), 
			"", "") )
			*/
	
		if(!AddToCart("",dr["code"].ToString(), dr["supplier"].ToString(), dr["supplier_code"].ToString(), 
			qty.ToString(), dr["supplier_price"].ToString(), dr["commit_price"].ToString(), 
			"", "", dr["discount_percent"].ToString() ) )

			return false;
	}

	//get kit
	sc = "SELECT * FROM order_kit WHERE id=" + m_orderID +" ";
//DEBUG("sc1 = ",sc);

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
		if(Request.QueryString["t"] == "credit")
			qty = (0 - int.Parse(qty)).ToString();
		//if(!DoAddKit(kit_id, MyIntParse(qty)))
		if(!DoAddKit(kit_id, MyIntParse(qty), m_orderID))
			return false;
	}

	return true;
} // method RestoreOrder ends

bool DoSearchItem(string kw, bool btype)
{
//	kw = EncodeQuote(kw);
//	string sc = "SELECT p.code, p.supplier, p.supplier_code, p.name, ISNULL(p.supplier_price, 0) AS supplier_price ";
//	sc += " FROM product p JOIN code_relations c ON c.code = p.code Where 1=1 "; // LIKE '"+ kw;
	string sc = "SELECT p.code, p.supplier, p.supplier_code, p.name, ISNULL(p.supplier_price, 0) AS supplier_price, price" + Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString() +" ";
	sc += " FROM code_relations p JOIN product pr ON pr.code = p.code Where 1=1 "; // LIKE '"+ kw;
	
	kw = kw.ToLower();
	if(!btype)
	{
		sc += "  AND (p.code LIKE '" + kw + "%' OR p.supplier_code LIKE '"+ kw +"%') ";
	}
	else
	{
		if(!TSIsDigit(kw))
			sc += " AND Lower(p.supplier_code) = '" + kw + "' ";
		else
		{
			if(m_bUseBarcodeInSalesMode)
				sc += " AND p.barcode = '" + kw + "' ";
			else
				sc += " AND (p.code = '" + kw + "' OR Lower(p.supplier_code) = '"+ kw +"') ";
		}
	}
	sc += " AND p.skip = 0 ";
//DEBUG(" sc =",sc);

	m_customerID = Request.Form["customer"].ToString();
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
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
	   // double dSalesPrice = 110;

        if(bCheckCustQuotes(m_customerID))
	    {
			string m_GetQuotePrice = getQuotePrice(m_customerID, code);
		    if(m_GetQuotePrice != "0")
				dSalesPrice = double.Parse(m_GetQuotePrice);
		}
		AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), "", "");
		string s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "&r=" + DateTime.Now.ToOADate();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
	//	Response.Redirect(s_url);
		return false; //end response
	}
	else if(m_nSearchReturn > 1)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		
		Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
		Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
		Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3>"+ Lang("Search Result For") +" " + Request.Form["item_code_search"] + "</td>");
		Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
		Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
		Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
		Response.Write("</tr></table>");	
		Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		//Response.Write("<center><h3>Search Result For " + Request.Form["item_code_search"] + "</h3></center>");
		Response.Write("<tr><td colspan=8><br>");
		if(m_nSearchReturn >= 100)
		{
			Response.Write(""+ Lang("Top 100 rows returned") +", "+ Lang("Display") +" 1-100");
			m_nSearchReturn = 100;
		}
		else
			Response.Write(""+ Lang("top") +" " +(m_nSearchReturn).ToString()+ " "+ Lang("rows returned") +", "+ Lang("display") +" 1-" + (m_nSearchReturn).ToString());
		Response.Write("</td></tr>");
		Response.Write("</table>");
		BindISTable();
		return false;
	}
	return true;
} // method DoSearchItem ends


bool DoMPNSearch(string kw)
{
	kw = EncodeQuote(kw);
//	string sc = "SELECT p.code, p.supplier, p.supplier_code, p.name, ISNULL(p.supplier_price, 0) AS supplier_price ";
//	sc += " FROM product p JOIN code_relations c ON c.code = p.code ";
	kw = kw.ToLower();
	string sc = "SELECT p.code, p.supplier, p.supplier_code, p.name, p.price1, ISNULL(p.supplier_price, 0) AS supplier_price, price" + Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString();
	sc += " FROM code_relations p ";
	sc += " WHERE ("; // supplier_code ";// LIKE '"+ kw;
	sc += "  Lower(p.supplier_code) LIKE '" + kw + "%'";
	if(m_bUseBarcodeInSalesMode)
		sc += " OR Lower(p.barcode) = '"+ kw +"' ";
	sc += " ) ";
	m_customerID = Request.Form["customer"].ToString();	
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
	if(m_nSearchReturn == 1)
	{
		DataRow dr = dst.Tables["isearch"].Rows[0];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		
		if(bCheckCustQuotes(m_customerID))
	    {
			string m_GetQuotePrice = getQuotePrice(m_customerID, code);
		    if(m_GetQuotePrice != "0")
				dSalesPrice = double.Parse(m_GetQuotePrice);
		}
		AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), "", "");
		//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos.aspx?ssid=" + m_ssid + "\">");	
		Response.Redirect("pos.aspx?ssid=" + m_ssid + "");
		return false; //end response
	}
	else if(m_nSearchReturn > 1)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		
		Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
		Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
		Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3>Search Result For " + Request.Form["item_code_search"] + "</td>");
		Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
		Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
		Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
		Response.Write("</tr></table>");	
		Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		//Response.Write("<center><h3>Search Result For " + Request.Form["item_code_search"] + "</h3></center>");
		Response.Write("<tr><td colspan=8><br>");
		//Response.Write("<center><h3>Search Result For " + Request.Form["item_code_search"] + "</h3></center>");
		if(m_nSearchReturn >= 100)
		{
			Response.Write(""+ Lang("Top") +" 100 "+ Lang("rows returned") +", "+ Lang("Display") +" 1-100");
			m_nSearchReturn = 100;
		}
		else
			Response.Write(""+ Lang("top") +" " +(m_nSearchReturn).ToString()+ " "+ Lang("rows returned") +", "+ Lang("display") +" 1-" + (m_nSearchReturn).ToString());
		Response.Write("</td></tr></table>");
		BindISTable();
	}
	return true;
} //method DoMPNSearch ends

void BindISTable()
{
	Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>"+ Lang("ProductID") +"</td>\r\n");
	Response.Write("<td>"+ Lang("Description") +"</td>\r\n");
	Response.Write("<td>"+ Lang("Supplier") +"</td>\r\n");
	Response.Write("<td>"+ Lang("SupplierCode") +"</td>\r\n");
		
	Response.Write("<td>"+ Lang("SupplierPrice") +"</td>\r\n");
	Response.Write("<td>"+ Lang("SellingPrice(GST Inc)") +"</td>\r\n");
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
		string sellingprice = dr["price"+ Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString()].ToString();
		Response.Write("<tr" + scolor + ">");

		//Link on add items' Product ID
		Response.Write("<td><a href='pos.aspx?a=add&code="+code+"&supplier="+supplier);
		Response.Write("&supplier_code=" + HttpUtility.UrlEncode(supplier_code) + "&pri=" + price + "&ssid=" + m_ssid + "'>");
		Response.Write(code + "</a></td>\r\n");

		//Link to add items to sales from search result
		Response.Write("<td><a href='pos.aspx?a=add&code="+code+"&supplier="+supplier);
		Response.Write("&supplier_code=" + HttpUtility.UrlEncode(supplier_code) + "&pri=" + price + "&ssid=" + m_ssid + "'>");
		Response.Write(name + "</a></td>\r\n");

		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + supplier_code+ "</td>");
		
		Response.Write("<td>" + double.Parse(price).ToString("c") + "</td>");
		Response.Write("<td>" + double.Parse(sellingprice).ToString("c") + "</td>");
		Response.Write("</tr>");
	}	
	Response.Write("</table>");
} // method BindISTable ends

void RestoreAllFields()
{
	if(Session["sales_type_credit" + m_ssid] != null)
	{
		m_bCreditReturn = (bool)Session["sales_type_credit" + m_ssid];
		m_quoteType = "6";//GetEnumID("receipt_type", "credit note");
	}

	if(Session["sales_freight" + m_ssid] != null && Session["sales_freight" + m_ssid] != "")
		m_dFreight = MyMoneyParse(Session["sales_freight" + m_ssid].ToString());

	if(Session["SalesType" + m_ssid] != null && Session["SalesType" + m_ssid].ToString() != "")
		m_quoteType = Session["SalesType" + m_ssid].ToString();

	if(Session["sales_customerid" + m_ssid] != null && Session["sales_customerid" + m_ssid].ToString() != "")
		m_customerID = Session["sales_customerid" + m_ssid].ToString();

	if(Session["order_created" + m_ssid] != null)
		m_bOrderCreated = true;
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
	if(Session["sales_no_individual_price" + m_ssid] != null && Session["sales_no_individual_price" + m_ssid].ToString() != "")
		m_bNoIndividualPrice = MyBooleanParse(Session["sales_no_individual_price" + m_ssid].ToString());
	
	//order number
	//DEBUG("SS" ,Session["delivery_number"+m_ssid].ToString());
	if(Session["sales_customer_po_number" + m_ssid] != null)
		m_custpo = Session["sales_customer_po_number" + m_ssid].ToString();  // ch
	if(Session["sales_note" + m_ssid] != null)
		m_salesNote = Session["sales_note" + m_ssid].ToString();
		
	if(Session["delivery_number"+m_ssid] != null)
		m_custdn = Session["delivery_number"+m_ssid].ToString();  // ch
	
	if(Session["sales" + m_ssid] != null)
		m_cSales = Session["sales" + m_ssid].ToString();  // ch
	if(Session["customergst" + m_ssid] != null)
	    m_cCustGst = Session["customergst" + m_ssid].ToString();
		
	if(Session["ordered_date" + m_ssid] != null)
	    m_sOrderDate = Session["ordered_date" + m_ssid].ToString();
	if(Session["sales_shipping" + m_ssid] != null)
		m_shipping = Session["sales_shipping" + m_ssid].ToString();
	
} // method RestoreAllFields ends

bool UpdateAllFields()
{
	m_custpo = Request.Form["custpo"];
	m_custdn = Request.Form["custdn"];
	m_cCustGst = Request.Form["custgst"];
	m_salesNote = Request.Form["note"];
	m_cSales = Request.Form["sales"];
	m_sOrderDate = Request.Form["order_date"];
	m_shipping = Request.Form["shipping_method"];
	if(Request.Form["branch"] != null && Request.Form["branch"] != "")
		m_branchID = Request.Form["branch"];
	m_dFreight = MyMoneyParse(Request.Form["freight"]);
	m_nShippingMethod = Request.Form["shipping_method"];
	m_specialShipto = "0";
	if(Request.Form["special_shipto"] == "on")
		m_specialShipto = "1";
	m_specialShiptoAddr = Request.Form["special_ship_to_addr"];
	m_pickupTime = EncodeQuote(Request.Form["pickup_time"]);
	m_customerID = Request.Form["customer"].ToString();
	if(m_pickupTime != null)
	{
		if(m_pickupTime.Length > 49)
			m_pickupTime = m_pickupTime.Substring(0, 49);
	}
	else
		m_pickupTime = "";

	string nip = "0";
	if(Request.Form["nip"] == "on")
		nip = "1";
	m_bNoIndividualPrice = MyBooleanParse(nip);

	Session["sales_customer_po_number" + m_ssid] = m_custpo;
	Session["delivery_number"+m_ssid] = m_custdn;
	Session["customergst" + m_ssid] = m_cCustGst;
	Session["sales" + m_ssid] = m_cSales;
	Session["ordered_date" + m_ssid] = m_sOrderDate;
	Session["sales_note" + m_ssid] = m_salesNote;
	Session["brach_id" + m_ssid] = m_branchID;
	Session["sales_shipping_method" + m_ssid] = m_nShippingMethod;
	Session["sales_special_shipto" + m_ssid] = m_specialShipto;
	Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
	Session["sales_pick_up_time" + m_ssid] = m_pickupTime;
	Session["sales_freight" + m_ssid] = m_dFreight;
	Session["sales_no_individual_price" + m_ssid] = nip;
	Session["sales_shipping" + m_ssid] = m_shipping;
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
//		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
		{
			if(dtCart.Rows[i]["system"] == "1")
				continue;
			string kit = dtCart.Rows[i]["kit"].ToString();
			string sqty_old = Request.Form["qty_old"+i.ToString()];
			string sqty = Request.Form["qty"+i.ToString()];
			string sprice_old = Request.Form["price_old"+i.ToString()];
			string sprice = Request.Form["price"+i.ToString()];			
			if(sprice != null && sprice != "")
			{
				//sprice = sprice.Replace(",", "");
				//sprice = sprice.Replace("$", "");				
				sprice = MyCurrencyPrice(sprice).ToString();
			}
			else
				sprice = "0";

			dPriceOld = MyMoneyParse(sprice_old);
			quantity = MyIntParse(sqty);
			quantityOld = MyIntParse(sqty_old);
			if(quantity == 0 || Request.Form["del" + i.ToString()] == "X") //do delete
			{
				dtCart.Rows.RemoveAt(i);
				Response.Redirect("pos.aspx?ssid=" + m_ssid + "");
				//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos.aspx?ssid=" + m_ssid + "\">");
				return false; //major bug fix, if delete row one then all prices messed up. we should refresh the page instead of continue print the cart list D.W. 24.May.2004
//				continue;
			}
			else if(quantity < 0)
			{
				m_bCreditReturn = true;
				Session["sales_type_credit" + m_ssid] = true;
				m_quoteType = "6";//GetEnumID("receipt_type", "credit note");
			}
//			else if(m_bCreditReturn)
//				quantity = 0 - quantity; //asume all items follw up are for credit

			if(!TSIsDigit(sprice))
				dPrice = dPriceOld;
			else
				dPrice = MyMoneyParse(sprice);
		
			if(bool.Parse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString()))
			{
				dPrice = dPrice / ( 1 + m_dGstRate);						
			}
						
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
						dQtyPrice = GetSalesPriceForDealer(dtCart.Rows[i]["code"].ToString(), sqty, Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
					//DEBUG("dQtyPrice",dQtyPrice);
					dtCart.Rows[i]["salesPrice"] = dQtyPrice.ToString();		
				}
				
				dtCart.Rows[i].EndEdit();			
			}
			else if(sprice != sprice_old)
			{
				dtCart.Rows[i]["salesPrice"] = dPrice.ToString();
			}
		
			dtCart.Rows[i]["name"] = Request.Form["name" + i];  //***********************************Sean
			/////add discount percent ///
			string discount = Request.Form["discount" + i];
			if(discount == null || discount == "")
				discount = "0";
			try
			{
				discount = double.Parse(discount).ToString();
			}catch(Exception e){ discount="0"; }
			//set to maximum allow...
			if(double.Parse(discount) > m_dSetMaximumDiscountPerItem)
				discount = m_dSetMaximumDiscountPerItem.ToString();
			
			dtCart.Rows[i]["discount_percent"] = discount;
			////end herere /////
		}
	}
	dtCart.AcceptChanges(); //Commits all the changes made to this row since the last time AcceptChanges was called
	return true;
} // method UpdateAllFields ends

bool MyDrawTable()
{
	if(!GetCustomer())
		return false;

	bool bRet = true;
	
	PrintJavaFunction();
	Response.Write("</table>");
	Response.Write("<form name=form1 action='pos.aspx?ssid=" + m_ssid + "");
	//if(Session[m_sCompanyName + "gst_onoff_for_pos"] != null && Session[m_sCompanyName + "gst_onoff_for_pos"] != "")
	//	Response.Write("&gst="+ Session[m_sCompanyName + "gst_onoff_for_pos"].ToString());
	Response.Write("' method=post>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3>" + m_tableTitle + "</td>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");

//	Response.Write("<td valign=top class='pageName' >");
	//Response.Write("<br><center><h3>" + m_tableTitle + "</h3></center>");
//	Response.Write(" <font size=3>" + m_tableTitle + "");
//	Response.Write("</td></tr></table>");
	Response.Write("</tr></table>");	
	//print sales header table
	if(!PrintSalesHeaderTable(m_custpo, m_custdn, m_sOrderDate))
		return false;
//	Response.Write("<table class=d align=center valign=center cellspacing=1 cellpadding=0 border=1>");

	Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=" + m_cols + ">");

	if(!PrintShipToTable(m_nShippingMethod))
		return false;

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=" + m_cols + ">");
	if(!PrintCartItemTable())
		return false;

	Response.Write("</td></tr>");

/*	//start comment table
	Response.Write("<tr><td>&nbsp</td></tr><tr><td><b>&nbsp;Comment : </b></td></tr>");
	Response.Write("<tr><td><textarea name=note cols=70 rows=5>" +m_salesNote+ "</textarea></td>");
	Response.Write("</tr>");
	//end comment table
*/
	Response.Write("</td></tr>\r\n");
	
	Response.Write("</table></form>");
	return bRet;
} // method MyDrawTable ends

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
} // method DrawTableHeade ends

bool PrintCartItemTable()
{
	CheckShoppingCart();
	int i = 0;

//	Response.Write("<br>");
//	DEBUG("tabwl =", tableWidth);
	Response.Write("<table border=1 cellpadding=1 width='"+ tableWidth +"' cellspacing=0 align=center valign=center ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td id='table-list-title-format width=130' align=left width=10% >"+ Lang("STOCK ID") +"</td>");
	Response.Write("<td width=70>"+ Lang("CODE") +"</td>");
	Response.Write("<td >"+ Lang("DESCRIPTION") +"</td>");
	Response.Write("<td align=right>"+ Lang("PRICE") +"</td>");
	Response.Write("<td align=right>"+ Lang("DISCOUNT") +"(%)</td>");
	Response.Write("<td align=right>"+ Lang("STOCK") +"</td>");
	Response.Write("<td align=right>"+ Lang("QTY") +"</td>");
	
	Response.Write("<td width=18% align=right>"+ Lang("TOTAL") +"</td></tr>");

	dTotalPrice = 0;
	dTotalGST = 0;
	dAmount = 0;
	dTotalSaving = 0;

	double dCost = 0;
	double dRowPrice = 0;
	double dRowGST = 0;
	double dRowTotal = 0;
	double dRowSaving = 0;
    bool bAddGSTInPrice = MyBooleanParse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString());	
	//build up row list
		for(i=0; i<dtCart.Rows.Count; i++)
		{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;

		DataRow dr = dtCart.Rows[i];
		string code = dr["code"].ToString();
		double dsupplierPrice = 0; //MyDoubleParse(dr["manual_cost_nzd"].ToString()) * MyDoubleParse(dr["rate"].ToString());
	
		if(dr["kit"].ToString() == "0")
			dsupplierPrice = double.Parse(dr["supplierPrice"].ToString()) * 1.1; //set to have cost * 1.1 profit
		
		int quantity = 1;
		try
		{
			quantity = int.Parse(dr["quantity"].ToString());
		}
		catch(Exception e){ //do nothing from herer 
		}
		
		double dDiscount =  0;
		string sDiscount = dr["discount_percent"].ToString();
		try
		{
			dDiscount = double.Parse(sDiscount);
			dDiscount = dDiscount / 100;
		}
		catch(Exception e)
		{
		}

		double dSalesPrice = 0;
//////////////set the GST PRICE switch when click on the check box button
		
		try
		{
			dSalesPrice = double.Parse(dr["salesPrice"].ToString());
		 double dOldPrice = MyMoneyParse(Request.Form["old_price" + i].ToString());
            double dCurrentPrice = MyMoneyParse(Request.Form["price" + i].ToString());
			if(Request.Form["del"+i] != "X" && (dOldPrice != dCurrentPrice))
			{
				string sprice = "";
				if(Request.Form["price"+ i] != null && Request.Form["price"+ i] != "")
					sprice = Request.Form["price"+ i];
				if(sprice != null && sprice != "")
				{
					sprice = sprice.Replace("$", "");
					if(MyBooleanParse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString()))
						bAddGSTInPrice = false;
					dSalesPrice = MyCurrencyPrice(sprice);
					//dSalesPrice = MyDoubleParse(sprice);
				}
			}
		}
		catch(Exception e)
		{
		}
	
		///set the price back to where is 		
		if(bAddGSTInPrice)
			dSalesPrice = dSalesPrice * ( 1 + m_dGstRate);

		dSalesPrice = Math.Round(dSalesPrice, 2);
		if(dDiscount > 0)
			dRowTotal = (dSalesPrice * (1-dDiscount)) * quantity;
		else
			dRowTotal = dSalesPrice * quantity;
		dRowTotal = Math.Round(dRowTotal, 2);
		string s_prodName = dr["name"].ToString();

		string supplierCode = dr["supplier_code"].ToString();	

		if(Request.Form["price"+i] != "" && Request.Form["price"+i] !=null)
			dSalesPrice = double.Parse(Request.Form["price"+i].Replace("$", "")); // CH 23/03/09 Store Price when qty changed

		if(m_bOrder)
		{
			SSPrintOneRow(i, dr["code"].ToString(), supplierCode, s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString(), MyDoubleParse(dr["discount_percent"].ToString()), MyBooleanParse(dr["kit"].ToString()));
		//	SSPrintOneRow(i, dr["code"].ToString(), supplierCode, s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString());
		}
		else
		{
			SSPrintOneRow(i, dr["code"].ToString(), dr["code"].ToString(), s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString(), MyDoubleParse(dr["discount_percent"].ToString()), MyBooleanParse(dr["kit"].ToString()));
			//SSPrintOneRow(i, dr["code"].ToString(), dr["code"].ToString(), s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString());
		}
		dTotalPrice += dRowTotal;
		dCost += dsupplierPrice;
	}

	//put an empty row for user input, which is used to search product by code or SN;
//	if(m_quoteType != "3" || Request.QueryString["p"] == "new")
	Response.Write("<tr ><td colspan='"+ m_cols +"'><input type=text name=item_code_search size=8 value=''>");

Response.Write("<script language=javascript>");
Response.Write("<!-- hide old browser ");
string sjava = @"
function iCalPrice(price, qty, i, discount)
{
	var dtotal, dDiscount;
	if(parseFloat(discount))
		dDiscount = discount;
	else
		dDiscount = 0;
	dDiscount = (dDiscount / 100);
	
	//price = price.replace('$', '');
	price = convertPrice(price);
	qty = qty.replace('$', '');
	if(IsNumberic(price) && IsNumberic(qty))
	{
		dtotal = (price *(1-dDiscount)) * qty;		
		dtotal = dtotal.toFixed(2);
";
sjava += "		eval(\"document.form1.dtotal\" + i + \".value = dtotal\")";
sjava += @"
	}
	var bfalse;
	bfalse = false;
	if(!IsNumberic(price))
	{
		bfalse = true;
	}
	if(!IsNumberic(qty))
	{
		bfalse = true;
	}
	if(bfalse)
		return false;
	else
		return true;
//	window.alert(dtotal);
	
}
function convertPrice(sPrice)
{	
	var sSwap, bFoundDot;
	sSwap = '';
	for (i = 0; i < sPrice.length; i++) 
	{ 
		if(parseFloat(sPrice.charAt(i))) 
		{			
			sSwap = sSwap + sPrice.charAt(i);				
		}
		if(sPrice.charAt(i) == '.' && !bFoundDot)
		{
			sSwap = sSwap + sPrice.charAt(i);
			bFoundDot = true;
		}
		if(sPrice.charAt(i) == '0')
			sSwap = sSwap + sPrice.charAt(i);
	}			
	return sSwap;
	
}
function IsNumberic(sText)
	{
	   var ValidChars = '0123456789.';
	   var IsNumber=true;
	   var Char;
 	   for (i = 0; i < sText.length && IsNumber == true; i++) 
	   { 
		  Char = sText.charAt(i); 
		  if (ValidChars.indexOf(Char) == -1) 
		  {
			 IsNumber = false;
		  }
	   }
		   return IsNumber;
	}
";
Response.Write(sjava);
Response.Write("-->");
Response.Write("</script");
Response.Write(">");
	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.form1.item_code_search.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<input type=submit name=cmd "+ Session["button_style"] +" size=8 value='"+ Lang("Select From Categories") +"'>");
	Response.Write("</td><td align=right><input type=submit name=cmd "+ Session["button_style"] +" value='"+ Lang("Recalculate Price") +"'></td></tr>");
	Response.Write("</td></tr>");
/*	Response.Write("</td><td colspan=7 align=right><input type=submit name=cmd "+ Session["button_style"] +" value='Recalculate Price'></td></tr>");
	Response.Write("<tr><td colspan=7><input type=submit name=cmd "+ Session["button_style"] +" size=8 value='Select From Categories'></td></tr>");
*/	
	if(Request.Form["freight"] != null && Request.Form["freight"] != "")
	{
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
//DEBUG(" msdfds f=", m_dGstRate.ToString());
	if(Session["customergst" + m_ssid] != null)
	{
	    m_cCustGst = Session["customergst" + m_ssid].ToString();
		m_dCustomerGst = double.Parse(m_cCustGst);
	}
	else if(m_cCustGst != "" && m_cCustGst != null)
	    m_dCustomerGst = double.Parse(m_cCustGst);
	m_dGstRate = m_dCustomerGst;
	if(bool.Parse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString()))
	{		
		dTotalGST = (dTotalPrice / (1 + m_dGstRate)) * m_dGstRate;
		dAmount = (dTotalPrice / (1 + m_dGstRate)) + dTotalGST;
	}
	else
	{
		dTotalGST =(dTotalPrice * m_dGstRate);
		dAmount = dTotalPrice + dTotalGST;
	}

	Response.Write("<tr bgcolor=#EEE999><td colspan="+ m_cols +">&nbsp;</td>");
	Response.Write("<td align=right>");
	if(m_bOrder)
	{
		Response.Write("&nbsp;");
	}
	else
	{
		Response.Write("<b>Discount : </b>");
		Response.Write("<input type=text size=1 style='text-align:right' align=right name=discount value='" + discount.ToString() + "'> % ");
	}

	Response.Write(" </td></tr>");

	Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-3).ToString() + " rowspan='8'>");
		//start comment table
	Response.Write("<table align=center><tr><td>&nbsp</td></tr><tr><td><b>&nbsp;"+ Lang("Comment") +" : </b></td></tr>");
	Response.Write("<tr><td><textarea name=note cols=60 rows=7>" +m_salesNote+ "</textarea></td>");
	Response.Write("</tr></table>");
	//end comment table

	Response.Write("&nbsp;</td>");
	if(m_bOrder)
	{
		//Response.Write("<td>&nbsp;</td>");
	}
	else
	{
		Response.Write("<td title='" + dCost.ToString("c") + "'>");
		Response.Write("<input type=text size=7 style='text-align:right' name=total value='" + dFinal.ToString("c") + "'>");
		Response.Write("<input type=hidden name=total_old value='" + dFinal.ToString("c") + "'></td>");	
	}

	//freight	
	Response.Write("<td colspan=3 align=right><b>"+ Lang("Freight") +" : </b></td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=text size=5 style='text-align:right' align=right name=freight value='");
	Response.Write(m_dFreight.ToString("c") + "'>");
	Response.Write("</td></tr>");

	
//	Response.Write("<tr bgcolor=#ffffff><td colspan=" + m_cols + " align=right>");
//	Response.Write("<input type=submit name=cmd value='Recalculate Price'></td>");

	double dCredit = 0;
	double dBalance = 0;
	bool bCredit = CreditLimitOK(m_customerID, dAmount, ref dCredit, ref dBalance, false);
	string term_msg = "";
	int nOverdues = 0;
	int nOverdueDays = 0;
	double dOverdueAmount = 0;
	bool bTermOK = CreditTermsOK(m_customerID, ref nOverdues, ref dOverdueAmount, ref nOverdueDays, ref term_msg);
	double dAvailable = dCredit - dBalance - dAmount;
	///credit balance ///
	m_dCreditsBalance = GetCreditBalance(m_customerID);
	Session["totalAmount"] = dAmount;
	

/*	
	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<b>Credit Limit : </b></td>");
	Response.Write("<td align=right><b>" + dCredit.ToString("c") + "</td></tr>");
	
	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<b>Account Balance : </b></td>");
	Response.Write("<td align=right><b>" + dBalance.ToString("c") + "</td></tr>");
*/	
	if(m_customerID != "" && m_customerID != "0")
	{
		//credit limit
		//Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-1).ToString() + " align=right>");
		
		Response.Write("<tr><td  bgcolor=#EEEEE colspan=3 align=right>");
		Response.Write("<b>"+ Lang("Credit Available") +" : </b></td>");
		Response.Write("<td  bgcolor=#EEEEE align=right title='"+ Lang("Credit Limit") +" : " + dCredit.ToString("c") + ", "+ Lang("Account Balance") +" : " + dBalance.ToString("c") + "'>");
		if(bCredit)
			Response.Write("<font color=green>");

		else
			Response.Write("<font color=red>");
		Response.Write("<b>" + ((dCredit==0) ? "Unlimited" : dAvailable.ToString("c")) + "</b></td></tr>");
///////////////////////credit balance/////customer's deposit credit...
		Response.Write("<tr><td  bgcolor=#EEEEE colspan=3 align=right>");
		Response.Write("<b>Credit : </b></td>");
		Response.Write("<td  bgcolor=#EEEEE align=right title='" + m_dCreditsBalance.ToString("c") + "'>");
		Response.Write("<b>" + m_dCreditsBalance.ToString("c") + "</b></td></tr>");
///////////////////////////////////end here.......///////////////////
		//credit term
		if(MyBooleanParse(GetSiteSettings("enable_credit_term_check", "0")))
		{
			//Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-1).ToString() + " align=right>");
			Response.Write("<tr><td  bgcolor=#EEEEE colspan=3 align=right>");
			Response.Write("<b>"+ Lang("Overdue Invoices/Amount") +" : </b></td>");
			Response.Write("<td  bgcolor=#EEEEE align=right title='"+ Lang("Most Overdue Days") +" : " + nOverdueDays.ToString() + "'>");
			if(bTermOK)
				Response.Write("<font color=green>");
			else
				Response.Write("<font color=red>");
			Response.Write("<b>" + ((bTermOK) ? "None" : nOverdues.ToString()) + "/" + dOverdueAmount.ToString("c") + "</b></td></tr>");
		}
	}	
///total discount in price//

	Response.Write("<tr bgcolor=#EEEEE><td colspan=3 align=right>");
	Response.Write("<b>"+ Lang("Discount Total ") +"(GST Excl.):");
	/*Response.Write("<b>"+ Lang("Discount Total") +"(<i>");
	if(!MyBooleanParse(GetSiteSettings("discount_include_GST", "0", true)))
		Response.Write(""+ Lang("GST Incl.") +"");
	else
		Response.Write(""+ Lang("GST Inclusive") +"");
	Response.Write("</i>):</b></td><td align=right>");*/
	
	/*Response.Write("<select name=discount_total_type>");
	Response.Write("<option value='$' ");	
	Response.Write(">$</option>");
	Response.Write("<option value='%' ");
	if(Session["discount_total_type_"+ m_sCompanyName] != null)
	{
		if(Session["discount_total_type_"+ m_sCompanyName].ToString() == "%")
			Response.Write(" selected ");
	}
	Response.Write(">%</option>");
	Response.Write("</select>");
	*/
	Response.Write("</b></td><td align=right>");
	//Response.Write("<input type=hidden name=discount_total_type value=$  >");
	Response.Write("<span style=\"font:bold 12px arail\">Total Amount $ </span><input type=text name=discount_total size=5 maxlenth=8 style=text-align:right onclick=\"document.all.discount_total_per.value ='' \" ");
	if(Session["total_amount"+ m_ssid] != null)
		Response.Write(" value='" +Session["total_amount"+ m_ssid].ToString()+"'");
	Response.Write("><br>");
	Response.Write("<span style=\"font:bold 12px arail\">Total Percent % </span><input type=text name=discount_total_per size=5 maxlenth=8 style=text-align:right onclick=\"document.all.discount_total.value ='' \" ");
	if(Session["discount_per"+m_ssid] != null)
		Response.Write(" value='"+Session["discount_per"+m_ssid].ToString()+"'");
	Response.Write("><br>");
	//Response.Write("<input type=submit name=cmd value='"+ Lang("Apply Total") +"' "+ Session["button_style"] +"> ");
//DEBUG("m_broder = ", m_bOrderCreated.ToString());
	Response.Write("</td></tr>");
	
	
	Response.Write("<tr bgcolor=#EEEEE><td colspan=3 align=right><b>GST Selection:</b>");
	Response.Write("</td><td align=right>");
	Response.Write("<b>"+ m_dGstRate.ToString("p") +"<b />&nbsp;<select name=custgst >");
	Response.Write("<option value='0.15' ");
	if(m_cCustGst == "0.15" )
	 Response.Write(" selected ");
	Response.Write(" selected > 15  </option>");
	Response.Write("<option value='0' ");
	if(m_cCustGst == "0")
	 Response.Write(" selected ");
	Response.Write(" > 0  </option>");
	Response.Write("</select>");
	Response.Write("</td></tr>");// ****************************GST SELECT******************************
	
	//sub total
//	Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<tr bgcolor=#EEEEE><td bgcolor=#EEEEE colspan=3 align=right>");
	Response.Write("<b>"+ Lang("Sub-Total") +" : </b></td><td bgcolor=#EEEEE align=right>");
/*	if(bool.Parse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString())) 
		Response.Write((dTotalPrice * (1+ m_dGstRate)).ToString("c"));
	else
*/
		Response.Write(dTotalPrice.ToString("c"));
	Response.Write("<input type=hidden name=subtotal value=" + dTotalPrice + ">");
	Response.Write("</td></tr>");

	//total GST
	//Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<tr bgcolor=#EEEEE><td bgcolor=#EEEEE colspan=3 align=right>");
	Response.Write("<b>"+ Lang("TAX") +" : </b></td>");
	Response.Write("<td bgcolor=#EEEEE align=right><b>");
	Response.Write(dTotalGST.ToString("c"));
	Response.Write("</b></td></tr>");

	//total amount due
	//Response.Write("<tr bgcolor=#ffffff><td colspan=" + (m_cols-1).ToString() + " align=right>");
	Response.Write("<tr bgcolor=#EEEEE><td bgcolor=#EEEEE ");
	if(m_customerID != "" && m_customerID != "0")
		Response.Write("colspan=7");
	else
		Response.Write("colspan=3");
	Response.Write(" align=right><b>"+ Lang("Total Amount Due") +" : </b></td>");
	Response.Write("<td bgcolor=#EEEEE align=right><b>");
	Response.Write(dAmount.ToString("c"));
	Response.Write("</b><input type=hidden name=totaldue value='" + dAmount.ToString("c") + "'></td></tr>");
	
	
	//Apply BTN
	Response.Write("<tr bgcolor=#EEEEE><td bgcolor=#EEEEE ");
	if(m_customerID != "" && m_customerID != "0")
		Response.Write("colspan="+ m_cols.ToString() +"");
	else
		Response.Write("colspan=3");
	Response.Write(" align=right></td><td bgcolor=#EEEEE align=right>");
	Response.Write("<input type=submit name=cmd value='"+ Lang("Apply Total") +"' "+ Session["button_style"] +"> ");
	Response.Write("</td></tr>");
	

	
	
	Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols + 1 ).ToString() + " align=right>");

	if(m_bOrderCreated)
	{
//		if(bCredit)

		if(m_orderStatus != "2" && m_orderStatus != "3" && m_orderStatus != "6")
		{	
			if(m_sSalesType == "quote")
				Response.Write("<input type=submit name=cmd value='"+ Lang("Change To Order") +"' "+ Session["button_style"] +">");
			if(m_bEnableCopyOrderFunction)
				Response.Write("<input type=button "+ Session["button_style"] +" name=cmd value='"+ Lang("Copy") +" " + Capital(Lang(m_sSalesType)) + "' onclick=\"window.location=('cpso.aspx?oid="+ m_orderID +"');\">");
			//Response.Write("<input type=submit "+ Session["button_style"] +" name=cmd value='"+ Lang("Delete") +" " + Capital(Lang(m_sSalesType)) + "'>");
			Response.Write("<input type=submit "+ Session["button_style"] +" name=cmd value='"+Lang("Delete "+m_sSalesType)+"'>");
			//Response.Write("<input type=submit "+ Session["button_style"] +" name=cmd value='"+ Lang("Update") +" " + Capital(Lang(m_sSalesType)) + "'>");
			Response.Write("<input type=submit "+ Session["button_style"] +" name=cmd value='"+Lang("Update "+m_sSalesType)+"'>");
			
			Response.Write("<input type=hidden name=order_id value=" + m_orderID + ">");
		}			
		else if(bCredit)
		{			
			Response.Write("<input type=submit "+ Session["button_style"] +" name=cmd value='"+ Lang("Record") +"' ");		
			Response.Write(">");
		}
	}
	else
	{
		//if(bCredit)
		//	Response.Write("<input type=button onclick=window.location=('q.aspx?ssid=" + m_ssid + "') value='Move To Quote' "+ Session["button_style"] +">");		
		if(g_bUseSystemQuotation)
			Response.Write("<input type=submit name=cmd value='"+ Lang("Move To Quote") +"' "+ Session["button_style"] +">");
		
		Response.Write("<input type=submit "+ Session["button_style"] +" name=cmd value='"+ Lang("Record") +"' ");
		if(dtCart.Rows.Count < 1)
				Response.Write(" disabled ");
		Response.Write(">");
	}
	Response.Write("<input type=submit name=cmd "+ Session["button_style"] +" value='"+ Lang("Cancel") +"'>");
	Response.Write("</td></tr>");

/*	Response.Write("<tr bgcolor=#EEEEE><td colspan=" + m_cols + " align=right><font color=red><b>");
//	Response.Write("Important : Click 'Update Order' or 'Cancel' to unlock <br>your order before leaving this page!!");
	Response.Write("</b></font></td></tr>");
*/
	Response.Write("</table>");
	return true;
} //method PrintCartItemTable ends

bool SSPrintOneRow(int nRow, string sID, string code, string desc, double dCost, double dPrice, int qty, double dTotal, string sSNnum)
{
	return SSPrintOneRow(nRow, sID, code, desc, dCost, dPrice, qty, dTotal, sSNnum, 0, false);
}

bool SSPrintOneRow(int nRow, string sID, string code, string desc, double dCost, double dPrice, int qty, double dTotal, string sSNnum, double dDiscountPercent, bool bKit)
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
   // dPrice = dPrice;
	Response.Write(">"); 

	//Online Store ID code;
	Response.Write("<td><a title='click here to view Sales Ref:' href='salesref.aspx?code=" + sID +"' class=o target=_new>");
	Response.Write(sID);
	Response.Write("</a> ");
	if(CheckPatent("viewsales"))
	{
		//Response.Write("<input type=button title='View Sales History' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
		//Response.Write("code=" + sID + "&cid="+ m_customerID +"','','left=0, top=0, width=550,height=550,resizable=1');\" value='S' "+ Session["button_style"] +">");
		Response.Write("<input type=button title='"+ Lang("View Customer Sales History") +"' onclick=\"javascript:viewsales_window=window.open('viewcustsales.aspx?");
		Response.Write("code=" + sID + "&cid="+ m_customerID +"','','left=0, top=0, resizable=1');\" value='S'  "+ Session["button_style"] +" >");
		Response.Write("<input type=button title='"+ Lang("View Purchase History") +"' onclick=\"window.open('viewpurchase.aspx?");
		Response.Write("code=" + sID + "')\" value='P' "+ Session["button_style"] +">");
	}
	Response.Write("</td>\r\n");

	//code
	Response.Write("<td nowrap>");
	Response.Write(code);
	Response.Write("</td>\r\n");

	//description
	
	desc = StripHTMLtags(desc);
	Response.Write("<td>");
	Response.Write("<textarea rows=1 cols=60 name=name"+ nRow.ToString() +">");
	Response.Write(desc);
	Response.Write("</textarea></td>\r\n");
/*	
	Response.Write("<input type=text size=50 maxlength=255 name=name" + nRow.ToString() + " value='");
	Response.Write(desc);
	Response.Write("'></td>\r\n");
	*/

	//price
	Response.Write("<td title=");
	Response.Write(dCost.ToString("c"));
	Response.Write(" align=right>");
	
	//Response.Write("<td align=right>");
	Response.Write("<input type=text size=7 style='text-align:right' name=price" + nRow.ToString() + " value='");
	Response.Write(dPrice.ToString("c"));
//	Response.Write(dPrice.ToString());
	Response.Write("'");
	Response.Write(" onchange=\"return iCalPrice(document.form1.price"+ nRow +".value, document.form1.qty"+ nRow +".value, "+ nRow +", document.form1.discount"+ nRow +".value);\" ");
	Response.Write("><input type=hidden name=price_old" + nRow.ToString() + " value='");
	Response.Write(dPrice.ToString("c"));
	Response.Write("'></td>\r\n");
//DEBUG("dPrice 3 ", dPrice);


/////// discount price for each item /////////
	Response.Write("<td align=right><input type=text size=2 maxlength=5 autocomplete=off style='text-align:right' name=discount" + nRow.ToString());
	Response.Write(" readonly value='" + dDiscountPercent.ToString() + "' ");
	Response.Write(" onchange=\"return iCalPrice(document.form1.price"+ nRow +".value, document.form1.qty"+ nRow +".value, "+ nRow +", document.form1.discount"+ nRow +".value);\" ");
	if(bKit)
		Response.Write(" readonly ");
	Response.Write(">");
	Response.Write("<input type=hidden name=discount_old" + nRow.ToString() + " value='" + dDiscountPercent.ToString() + "'>");
	/////
	//current stock
	Response.Write("<td align=right>" + stock + "</td>");

	//quantity
	Response.Write("<td align=right><input type=text size=3 autocomplete=off style='text-align:right' name=qty" + nRow.ToString());
	Response.Write(" value='" + qty.ToString() + "' ");
	Response.Write(" onchange=\"return iCalPrice(document.form1.price"+ nRow +".value, document.form1.qty"+ nRow +".value, "+ nRow +", document.form1.discount"+ nRow +".value);\" ");
	Response.Write(">");

	Response.Write("<input type=hidden name=qty_old" + nRow.ToString() + " value='" + qty.ToString() + "'>");
	Response.Write("<input type=submit name=del" + nRow + " "+ Session["button_style"] +" value='X'>");
	Response.Write("</td>\r\n");
//DEBUG("QTY ", qty);
	//total
	Response.Write("<td align=right>");
	Response.Write("<input style='text-align:right' size=10% type=textbox name=dtotal" + nRow.ToString() + " readonly value=");
	Response.Write(dTotal.ToString("c"));
	Response.Write(">");
//	Response.Write(dTotal.ToString("c"));
//	Response.Write(dTotal.ToString());
	Response.Write("</td>\r\n</tr>\r\n");

	return true;
} //method SSPrintOneRow ends

bool PrintShipToTable(string shipping )
{
	DataRow dr = null;
	bool bCashSales = false;
	if(Session["sales_customerid" + m_ssid] == null)
	{
		bCashSales = true;
	}
	else if(Session["sales_customerid" + m_ssid].ToString() == "0")
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

	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=0 border=1");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td width=40% valign=top>");
	//bill to
	Response.Write("<table><tr><td valign=top>");
	Response.Write("<b>"+ Lang("Bill To") +" : <br></b><td><td valign=top>");

	string sCompany = "";
	string sAddr = "";
	string sContact = "";

	if(!bCashSales)
	{
		sCompany = dr["trading_name"].ToString();
		sAddr += dr["postal1"].ToString() + "<br>";
		sAddr += dr["postal2"].ToString() + "<br>";
		sAddr += dr["postal3"].ToString() + "<br>";		
		sAddr += "Ph: "+ dr["phone"].ToString() + "<br>";
		sAddr += "Fax: "+ dr["fax"].ToString() + "<br>";

		Response.Write(sCompany);
		Response.Write("<br>\r\n");
		Response.Write(sAddr);
		Response.Write("<br>\r\n");
//		Response.Write(dr["Email"].ToString());
//		Response.Write("<br>\r\n");
	}
	Response.Write("</td></tr></table></td><td valign=top align=right>");

	//ship to 
	Response.Write("<table width=100% border=0>");

	//shipping method
	Response.Write("<tr><td valign=top>");
	Response.Write("<table border=0 cellspacing=0><tr><td width=40%>");
	Response.Write("<b>"+ Lang("Shipping Method") +" :</b></td><td>");
	Response.Write("<select name=shipping_method onchange=\"OnShippingMethodChange();\">");
	//if(bCashSales)
	//	Response.Write("<option value=1>"+Lang("PICK UP")+"</option>");
	//else
		Response.Write(GetEnumOptions("shipping_method", shipping, false, true, m_sCheckedDeliveryOption));
	Response.Write("</select>");
	Response.Write("</td></tr>");
	
//pick up row	
	Response.Write("<table width=100% border=0 id=tPT");
	if(m_nShippingMethod != "1")
		Response.Write(" style='visibility:hidden' ");
	Response.Write("><tr><td width=40%>");
	Response.Write(""+ Lang("Pick Up Time") +" : </td><td><input type=text size=10 name=pickup_time maxlength=49 value=\"" + m_pickupTime + "\">");
	Response.Write("</td></tr></table>");
//end pick up row
	Response.Write("</td></tr></table>");
	//end ****

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
	
	Response.Write("</td><td>");
	Response.Write("<tr><td><b>"+ Lang("Ship To") +":</b>");
	Response.Write(" <input type=checkbox name=special_shipto ");
	if(m_specialShipto == "1")
		Response.Write(" checked");
	Response.Write(" onclick=\"OnSpecialShiptoChange();\">"+ Lang("Special Shipping Address") +" : ");
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

	if(m_specialShipto == "1")
	{
	}
	else if(!bCashSales)
	{
	}

	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
	//end of ship to


	Response.Write("</td></tr></table>");
	//end of bill and shipto table
	return true;
} // method PrintShipToTable ends

string DoSerialSearch(string s_SN)
{
	string s_msgSN = "";
	s_SN = EncodeQuote(s_SN);
	string sc = "SELECT s.sn, s.status, s.product_code, p.name AS prod_desc, p.supplier_code, p.supplier, p.supplier_price AS cost ";
	       sc += "FROM stock s JOIN product p ON p.code = s.product_code WHERE s.sn = '" + s_SN + "' ORDER BY s.update_time DESC ";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nSearchReturn = myAdapter.Fill(dst, "prod_sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
m_customerID = Request.Form["customer"].ToString();
	
	if(m_nSearchReturn == 1)  //> 0 )
	{
		DataRow dr = dst.Tables["prod_sn"].Rows[0];
		if(GetEnumValue("stock_status", dr["status"].ToString()) == "in stock")    // Stock Status : "2" indicating the item is in "stock";
		{
			string code = dr["product_code"].ToString();
			string supplier = dr["supplier"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string supplier_price = dr["cost"].ToString();
			string prod_name = dr["prod_desc"].ToString();
			string s_serialNo = s_SN;						
    		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		    
			if(bCheckCustQuotes(m_customerID))
	    {
			string m_GetQuotePrice = getQuotePrice(m_customerID, code);
		    if(m_GetQuotePrice != "0")
				dSalesPrice = double.Parse(m_GetQuotePrice);
		}
			AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), prod_name, s_serialNo);
			return "found";
		}
		else
		{
			s_msgSN = "The item (SN #: " + s_SN + " ) is not for selling, it's sold already!  >_< !!!";
			return s_msgSN;
		}
	}

	return "notfound"; 
} // metohd DoSerialSearch ends

bool PrintSalesHeaderTable(string sCustomerPONumber , string sDeliveryNumber, string sOrderDate)
{
	Response.Write("<table width='"+ tableWidth +"' align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
Response.Write("<tr><td colspan=2><br></td></tr>");
	//customer
	if(m_customerID == null && m_customerID == "" || m_customerID == "0")
		Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] = "1";
string luri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
	Response.Write("<tr><td >");
	
	Response.Write("<table><tr><td>");
	
	Response.Write("<b>"+ Lang("Customer") +" : </b></td><td>");
	Response.Write("<select name=customer onclick=window.location=('pos.aspx?search=1&ssid=" + m_ssid + "')>");
	Response.Write("<option value=0>"+Lang("Cash Sales")+"</option>");
	if(m_customerID != "" && m_customerID != "0")
		Response.Write("<option value='" + m_customerID + "' selected>" + m_customerName + "</option>");
	Response.Write("</select>");
	Response.Write(" <input type=button title='"+ Lang("add new customer") +"' value='"+ Lang("EZ CARD") +"' onclick=\"javascript:addcard_window=window.open('ezcard.aspx?r="+ DateTime.Now.ToOADate() +"&luri="+ HttpUtility.UrlEncode(luri) +"','','resizable=1, screenX=300,screenY=200,left=300,top=200'); addcard_window.focus();\"  " + Session["button_style"] + ">");
	if(m_customerID.Length > 2)
	{
		Response.Write("<input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
		Response.Write("id=" + m_customerID + "','','width=350,height=340, scrollbars=1, resizable=1');\" value='"+ Lang("View Card") +"' "+ Session["button_style"] +">");
	}

	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>"+ Lang("ACC#/Search") +" : </b></td>");
	Response.Write("<td><table cellpadding=0 cellspacing=0><tr><td><input type=text name=ckw size=15 value='" + m_customerID + "'>");
	Response.Write("</td><td valign=middle><input type=submit name=cmd value="+ Lang("GO") +" "+ Session["button_style"] +"></td></tr></table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<b>"+ Lang("P.O.Number") +" : </b></td><td><input type=editbox name=custpo value='" + sCustomerPONumber + "'>");
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td>");
	Response.Write("<b>"+ Lang("D. Number") +" : </b></td><td><input type=text name=custdn value='" + sDeliveryNumber + "'>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	
/*	//payment
	Response.Write("</td><td>");
	Response.Write("<table><tr><td>");
	Response.Write("<tr><td><b>Paid : </b></td><td><font color=red><b>");
	if(m_bPaid)
		Response.Write("YES");
	else
		Response.Write("NO");
	Response.Write("</b></font></td></tr><tr><td><b>Payment : </b></td><td><b>");
	if(m_bPaid)
		Response.Write(GetEnumValue("payment_method", m_paymentType).ToUpper());
	Response.Write("</b></td></tr></table>");
*/
	//branch and sales
	Response.Write("</td><td width=60% align=right valign=top>");
	Response.Write("<table><tr><td>");

	if(Session["branch_support"] != null)
	{
		Response.Write("<tr><td><b>"+ Lang("Branch") + " : </b></td><td>");
		if(!PrintBranchNameOptions())
			return false;
		Response.Write("</tr>");
	}
	else
		Response.Write("<input type=hidden name=branch value=1>");

	Response.Write("<tr><td><b>"+ Lang("Sales") +" : </b></td><td>");
//DEBUG("sdfs =", Session["branch_id"].ToString());
    m_sales = m_cSales;
	Response.Write("<select name=sales>");
	PrintSalesPersonOptions(m_sales);
	Response.Write("</select");
//	Response.Write("<b>Sales : </b></td><td><input type=submit name=cmd value='" + TSGetUserNameByID(m_sales) + "' "+ Session["button_style"] +"></b>");
//	Response.Write("<input type=hidden name=sales value='" + m_sales + "'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=1 align=right><b>"+ Lang("No Individual Price") +" : </b></td><td><input type=checkbox name=nip ");
	if(m_bNoIndividualPrice)
		Response.Write(" checked");
	Response.Write(">");
	Response.Write("</td></tr>");
	/////inclusive GST/////	
	string uri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
	if(uri.IndexOf("&gst=true")>=0)
	{
		uri = uri.Replace("&gst=true", "");
	}
	if(uri.IndexOf("&gst=false")>=0)
	{
		uri = uri.Replace("&gst=false", "");
	}
	/*Response.Write("<tr><td colspan=1 align=right><b>"+ Lang("Show Price in GST Inc") +" : </b></td><td><input type=checkbox name=gst ");
	if(bool.Parse(Session[m_sCompanyName + "gst_onoff_for_pos"].ToString()))
		Response.Write(" checked");
	Response.Write(" onclick=\"window.location=('" + uri +"&gst=' + document.form1.gst.checked); \"> <b>"+ m_dGstRate.ToString("p") +"<b />");	
	Response.Write("<select name=custgst >");
	
	Response.Write("<option value='0.15' ");
	if(m_cCustGst == "0.15" )
	 Response.Write(" selected ");
	Response.Write(" selected > 15  </option>");
	Response.Write("<option value='0' ");
	if(m_cCustGst == "0")
	 Response.Write(" selected ");
	Response.Write(" > 0  </option>");
	
        Response.Write("</select>");*/	//***************************************Sean CIL
	Response.Write("</td></tr>");
	Response.Write("<tr><td><B>Order Date:</B></td><td><input type=text name=order_date value='"+ sOrderDate+"'   onClick=\"displayDatePicker(this.name);\">");
	
	Response.Write("</td></tr></table>");

	Response.Write("</td></tr>");
	Response.Write("</table><br>");

	return true;
} // method PrintSalesHeaderTable ends

void PrintSalesPersonOptions(string current)
{
	int rows = 0;
	if(current == "")
		current = Session["login_card_id"].ToString();
	string sc = " SELECT id, name FROM card WHERE type = 4 ";
	if(Session["branch_support"] != null)
	{
		sc += " AND our_branch = " + Session["branch_id"].ToString();
	}
	sc += " ORDER BY name ";
//DEBUG("sc =", sc);	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "sales");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
	Response.Write("<option value=-1>&nbsp;</option>");
	for(int i=0; i<rows; i++)
	{
		string id = dst.Tables["sales"].Rows[i]["id"].ToString();
		Response.Write("<option value=");
		Response.Write(id);
		if(id == current)
			Response.Write(" selected");
		Response.Write(">" + dst.Tables["sales"].Rows[i]["name"].ToString() + "</option>");
	}
} //method PrintSalesPersonOptions ends

bool DoCustomerSearchAndList()
{
	string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	if((Request.ServerVariables["QUERY_STRING"].ToString()).IndexOf("search=1") < 0)
		uri += "&search=1";
	int rows = 0;
	//string kw = "'%" + Request.Form["ckw"] + "%'";
	string kw = "%" + Request.Form["ckw"] + "%";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "%%";
	//kw = "'%%'";
	kw = EncodeQuote(kw.ToLower());
	//kw = kw.Replace("'", "");
	string sc = "SELECT *, '" + uri + "' + '&ci=' + LTRIM(STR(id)) AS uri FROM card ";
	sc += " WHERE type <> 3  AND main_card_id IS NULL ";
	//sc += " WHERE type IN(0,1,2) AND main_card_id IS NULL ";
	if(IsInteger(Request.Form["ckw"]))
		sc += " AND id=" + Request.Form["ckw"];
	else
	{
		sc += " AND (name LIKE '" + kw + "' OR email LIKE '" + kw + "' OR company LIKE '" + kw + "') ";
		sc += " AND type<>" + GetEnumID("card_type", "supplier");
	}
	if(Session["branch_support"] != null)
	{
		if(!bSecurityAccess(Session["card_id"].ToString()))
		{
			sc += " AND our_branch = "+ Session["branch_id"].ToString();
		}					
	}
	sc += " ORDER BY company";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
//DEBUG("rows =", rows);
		if(rows == 1)
		{
			Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] = dst.Tables["card"].Rows[0]["dealer_level"].ToString();
			Session[m_sCompanyName + "_card_type_for_pos" + m_ssid] = dst.Tables["card"].Rows[0]["type"].ToString();
		
			m_customerID = dst.Tables["card"].Rows[0]["id"].ToString();
			ApplyPriceForCustomer();
			Response.Redirect("" + uri + "&ci=" + dst.Tables["card"].Rows[0]["id"].ToString() + "");
		//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
		//	Response.Write("URL=" + uri + "&ci=" + dst.Tables["card"].Rows[0]["id"].ToString() + "\">");
			
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	BindGrid();

	PrintAdminHeader();
	PrintAdminMenu();

	//Response.Write("<center><h3>Search for Dealer</h3></center>");
	Response.Write("<form id=search action=" + uri + " method=post>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>"+ Lang("Search for Customer") +"</b><font color=red><b>");
	Response.Write("</td>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
	
	Response.Write("<input type=hidden name=invoice_number value=" + m_invoiceNumber + ">");
	Response.Write("<table align=center width='"+ tableWidth +"' ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");
	Response.Write("<input type=editbox size=25 name=ckw>");
	Response.Write("<input type=submit name=cmd value="+ Lang("Search") +" "+ Session["button_style"] +">");
	Response.Write("<input type=button name=cmd value='"+ Lang("Cancel") +"'");
	Response.Write(" onClick=window.location=('pos.aspx?ssid=" + m_ssid + "') "+ Session["button_style"] +">");
	Response.Write("<input type=button onclick=window.open('ecard.aspx?n=customer&a=new') value='"+ Lang("New Customer") +"' "+ Session["button_style"] +">");
	Response.Write("<input type=button onclick=window.location=('pos.aspx?ci=0&ssid=" + m_ssid + "') value='"+ Lang("Cash Sales") +"' "+ Session["button_style"] +">");
	Response.Write("</td></tr></table></form>");

	LFooter.Text = m_sAdminFooter;
	return true;
} // method DoCustomerSearchAndList ends

string GetSalesManager(string card_id)
{
	if(dst.Tables["salesmanager"] != null)
		dst.Tables["salesmanager"].Clear();

	string sc = " SELECT sales FROM card WHERE id = " + card_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "salesmanager") <= 0)
			return "null";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "null";
	}
	
	string output = dst.Tables["salesmanager"].Rows[0]["sales"].ToString();
	if(output == "")
		output = "null";
	return output;
} // method GetSalesManager ends

bool CreateOrder(string branch_id, string card_id, string po_number, string special_shipto, string shipto, 
				 string shipping_method, string pickup_time, string contact, string sales_id, string sales_note, 
				 bool bNoIndividualPrice, ref string order_number)
{
	
	string reason = "";
	bool bStopOrdering = IsStopOrdering(card_id, ref reason);
	if(bStopOrdering)
	{
		if(reason == "")
			reason = "No reason given.";
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>This account has been disabled to place order</h3><br>");
		Response.Write("<h4><font color=red>" + reason + "<font color=red></h4><br>");
		Response.Write("<h4><a href=ecard.aspx?id=" + card_id + " class=o>Edit Account</a></h4>");
		Response.Write("<br><br><br><br><br><br><br>");
		LFooter.Text = m_sFooter;
		return false;
	}

//	if(!CheckBottomPrice())
//		return false;

	if(branch_id == null || branch_id == "")
		branch_id = "1";

	DataSet dsco = new DataSet();

	string sc = "BEGIN TRANSACTION ";
	sc += "  SET DATEFORMAT dmy  INSERT INTO orders (number, type, card_id, po_number, freight, delivery_number , customer_gst, record_date) "; //, sales_manager) ";
	sc += " VALUES(0, " + m_quoteType + ", " + card_id + ", '";
	sc += po_number + "', " + MyMoneyParse(Request.Form["freight"]);
//	sc += ", " + GetSalesManager(card_id);  //get sales manager in update..
	sc += ", '"+ m_custdn + "','" +m_cCustGst+"'" ;
	if(m_sOrderDate !="")
	 	sc += ",'"+ m_sOrderDate+"'";
	else
		sc += ", GETDATE()";
	sc += ") SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dsco, "id") == 1)
		{
			string nip = "0";
			if(bNoIndividualPrice)
				nip = "1";
			m_orderID = dsco.Tables["id"].Rows[0]["id"].ToString();
			m_orderNumber = m_orderID; //new order, same
			//assign ordernumber same as id
			sc = "  SET DATEFORMAT dmy  UPDATE orders SET number=" + m_orderNumber + ", branch=" + branch_id + ", sales_note=N'" + sales_note + "' ";
			if(special_shipto == "1")
				sc += ", special_shipto=1, shipto='" + EncodeQuote(shipto) + "' ";
			sc += ", contact='" + contact + "' ";
			if(sales_id != "")	
				sc += ", sales=" + sales_id;
			sc += ", sales_manager = (SELECT ISNULL(sales, '"+ sales_id +"' ) FROM card WHERE id ='"+ card_id +"' ) ";
			if(!m_bCreditTermsOK)
				sc += ", status=5 "; //put on hold
			sc += ", customer_gst =  "+ m_cCustGst;//(SELECT ISNULL(gst_rate,'0.125') FROM card WHERE id ='"+ card_id +"' ) ";
			sc += ", shipping_method=" + shipping_method;
			sc += ", pick_up_time='" + EncodeQuote(pickup_time) + "' ";
			sc += ", no_individual_price = " + nip;
			sc += ", delivery_number = N'"+ m_custdn +"'";
			if(m_sOrderDate != "")
				sc += ", record_date = '"+ m_sOrderDate + "'";
            else
				sc += ", record_date = GETDATE()";				
			
			//if(Session["m_GSTSession"].ToString() == "True")
			sc += ", gst_inclusive = '"+Session[m_sCompanyName + "gst_onoff_for_pos"]+"'";
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
	Session["sales_current_order_id" + m_ssid] = m_orderID;
	Session[m_sCompanyName + "gst_onoff_for_pos"] = "false";
	return true;
} // method CreateOrder ends

bool DoCreateOrder(bool bSystem, string sCustPONumber, string sSalesNote)
{
	string branch = Request.Form["branch"];
	string contact = "";
	return CreateOrder(branch, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), m_bNoIndividualPrice, ref m_orderID);
} // method DoCreateOrder ends

bool CheckBottomPrice()
{
	if(!MyBooleanParse(GetSiteSettings("enable_bottom_price_check", "1")))
		return true;

	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		string supplier_code = dtCart.Rows[i]["supplier_code"].ToString();
		if(supplier_code.ToLower() == "ac200") //temperory code for unknow product
			return true;
		if(dtCart.Rows[i]["kit"].ToString() == "1")
			continue; //ignore kit price

		string code = dtCart.Rows[i]["code"].ToString();
		double dPriceCheck = MyMoneyParse(dtCart.Rows[i]["salesPrice"].ToString());
		int iQty = MyIntParse(dtCart.Rows[i]["quantity"].ToString());

		DataRow drp = null;
		if(!GetProduct(code, ref drp))
		{
			Response.Write("<br><br><center><h3>Product not found");
			return false;
		}
		dPriceCheck = Math.Round(dPriceCheck, 2);
//		double dBottomPrice = Math.Round(MyMoneyParse(drp["price"].ToString()), 2);
		double dLastCostNZD = Math.Round(MyMoneyParse(drp["supplier_price"].ToString()), 2);
		double dManualCostNZD = Math.Round(MyMoneyParse(drp["manual_cost_nzd"].ToString()), 2);
		if(MyBooleanParse(GetSiteSettings("enable_under_cost_checked", "1", true)))
		{
			if(iQty > 0 && dPriceCheck < dLastCostNZD && dPriceCheck < dManualCostNZD)
			{
				PrintAdminHeader();
				PrintAdminMenu();
				Response.Write("<br><br><center><h3>Error, Under-Cost sales detected.</h3>");
				Response.Write("<br>Product Code : " + code);
				Response.Write("<br>Description : " + drp["name"].ToString());
				Response.Write("<br>Last Cost "+m_sCurrencyName+" : " + dLastCostNZD.ToString("c"));
				Response.Write("<br>Manual Cost "+m_sCurrencyName+" : " + dManualCostNZD.ToString("c"));
				Response.Write("<br>Sales Price : " + dPriceCheck.ToString("c"));
				Response.Write("<br><br><input type=button "+ Session["button_style"] +" value=Back onclick=history.go(-1)>");
				return false;
			}
		}
	}
	return true;
} // method CheckBottomPrice ends

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
} // method DoDeleteOrder ends



bool DoUpdateOrder()
{
	if(m_orderID == "")
		return false;

	if(!CheckBottomPrice())
		return false;

	string freight = "";
	if(Request.Form["freight"] != null)
		freight = Request.Form["freight"].ToString();
	freight = MyCurrencyPrice(freight).ToString();
	m_orderID = Request.Form["order_id"];
	string salesID = Session["card_id"].ToString();
	if(Request.Form["sales"] != null)
		salesID = Request.Form["sales"].ToString();
	string nip = "0";
	if(m_bNoIndividualPrice)
		nip = "1";
	
	string sc = "SET DATEFORMAT dmy  UPDATE orders SET ";
	sc += " branch=" + m_branchID;
	sc += ", card_id=" + m_customerID;
	sc += ", freight=" + freight;
	sc += ", delivery_number = N'"+m_custdn;
	sc += "',  customer_gst = '"+ m_cCustGst;
	sc += "', po_number='" + EncodeQuote(m_custpo) + "' ";
	sc += ", sales_note='" + EncodeQuote(m_salesNote) + "' ";
	sc += ", sales=" + salesID;
	sc += ", shipping_method=" + m_nShippingMethod;
	sc += ", special_shipto=" + m_specialShipto;
	sc += ", shipto='" + EncodeQuote(m_specialShiptoAddr) + "' ";
	sc += ", pick_up_time='" + EncodeQuote(m_pickupTime) + "' ";
	sc += ", locked_by=null, time_locked=null "; //unlock
	sc += ", no_individual_price = " + nip;
	if(m_sOrderDate != "")
		sc += ", record_date ='"+m_sOrderDate+ "'";
	else
		sc += ", record_date = GETDATE()";
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
} // method DoUpdateOrder ends

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
} // method DeleteOrderKits ends

bool DeleteOrderItems(string m_orderID)
{
	if(!DeleteOrderKits(m_orderID))
		return false;

	int items = 0;

	//check if there's any items to delete
	string sc = " SELECT o.branch, i.code, i.quantity ";
	sc += " FROM order_item i JOIN orders o ON o.id=i.id ";
	sc += " WHERE i.id=" + m_orderID;
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

	if(items <= 0)
		return true;

	m_branchID = dst.Tables["delete_items"].Rows[0]["branch"].ToString();
	if(m_branchID == "")
		m_branchID = "1";

    sc = " SELECT type FROM orders WHERE id="+m_orderID;
    int orderType=2;
 	try
	{   
        DataSet ds = new DataSet();
		SqlDataAdapter ad = new SqlDataAdapter(sc, myConnection);
        myConnection.Open();
		ad.Fill(ds);
        myConnection.Close();
        orderType = (int)ds.Tables[0].Rows[0][0];
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
//		if(g_bRetailVersion)
        if(orderType != 1)//Session["SalesType" + m_ssid].ToString()!="1")
		{
			sc += " Update stock_qty SET ";
			sc += " allocated_stock = allocated_stock - " + sqty;
			sc += " WHERE code=" + code + " AND branch_id = " + m_branchID;
//		}
//		else
			sc += " UPDATE product SET allocated_stock = allocated_stock - " + sqty + " WHERE code=" + code;
        }
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
} // method DeleteOrderItems ends

bool WriteOrderItems(string order_id)
{
	CheckShoppingCart();

	for(int i=0; i<dtCart.Rows.Count; i++)
	{   
        string sc = " SELECT type FROM orders WHERE id="+m_orderID;
        int orderType=2;
 	    try
	    {    
            DataSet ds = new DataSet();
	    	SqlDataAdapter ad = new SqlDataAdapter(sc, myConnection);
            myConnection.Open();
		    ad.Fill(ds);
            myConnection.Close();
            orderType = (int)ds.Tables[0].Rows[0][0];
	    }
	    catch(Exception e) 
	    {
		    ShowExp(sc, e);
        }   
         sc = "";   
 
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		string kit = dr["kit"].ToString();
		double dPrice = 0;
		try
		{
			dPrice = double.Parse(dr["salesPrice"].ToString());
		}
		catch(Exception ec)
		{
		}

//		if(dr["salesPrice"].ToString() != null && dr["salesPrice"].ToString() != "")
//			dPrice = MyMoneyParse(dr["salesPrice"].ToString());
//		dPrice = Math.Round(dPrice, 2);
		string name = EncodeQuote(dr["name"].ToString());
		if(name.Length > 255)
			name = name.Substring(0, 255);

		if(kit == "1")
		{
			RecordKitToOrder(order_id, dr["code"].ToString(), name, dr["quantity"].ToString(), dPrice, m_branchID);
			continue;
		}

		sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
		sc += ", commit_price, discount_percent) VALUES(" + order_id + ", " + dr["code"].ToString() + ", ";
		sc += dr["quantity"].ToString() + ", N'" + name + "', N'" + dr["supplier"].ToString();
		sc += "', N'" + dr["supplier_code"].ToString() + "', " + Math.Round(MyMoneyParse(dr["supplierPrice"].ToString()), 2);
		sc += " , "+ dPrice +" ";
		sc += ", "+ dr["discount_percent"].ToString() +"";
		sc += " ) ";		
//		sc += ", " + Math.Round(MyMoneyParse(dr["salesPrice"].ToString()), 2) + ") ";
//		if(g_bRetailVersion)
        if(orderType != 1)//Session["SalesType" + m_ssid].ToString()!="1")
		{
			sc += " UPDATE stock_qty SET allocated_stock = allocated_stock + " + dr["quantity"].ToString();
			sc += " WHERE code = " + dr["code"].ToString();
			sc += " AND branch_id = " + m_branchID;
//		}
//      else
//		{
			sc += " UPDATE product SET allocated_stock = allocated_stock + " + dr["quantity"].ToString();
			sc += " WHERE code=" + dr["code"].ToString() + " ";
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
	}
	return true;
} // method WriteOrderItems ends

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
} // metohd PrintJavaFunction ends

bool ApplyPriceForCustomer()
{
	string customerLevel = Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString();
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
			
		double dPrice = MyDoubleParse(dr["SalesPrice"].ToString());
		string sKit = dr["kit"].ToString();
		if(sKit == null || sKit == "")
			sKit = "0";
		if(sKit == "0") ///it's kit then get sales dealer price
			dPrice = GetSalesPriceForDealer(code, qty, customerLevel, m_customerID);
//DEBUG("dprice = ", dPrice.ToString());
		dr["SalesPrice"] = dPrice.ToString();
	}
	return true;
} // method  ApplyPriceForCustomer ends
 
bool DoChangeToOrder()
{   
    string sc = "SELECT quantity FROM order_item WHERE id="+m_orderID; 
    string qty="";
    try{
    //SqlDataAdapter ad = new SqlDataAdpater(sc, myConnection);
      SqlDataAdapter ad = new SqlDataAdapter(sc, myConnection);

    myConnection.Open();
    DataSet ds = new DataSet();
    ad.Fill(ds);  
    qty = ds.Tables[0].Rows[0]["Quantity"].ToString();
    myConnection.Close();
    }
    catch(Exception e){}
	sc = " UPDATE orders SET type = 2 WHERE id = " + m_orderID; 
    sc+= " UPDATE stock_qty SET allocated_stock=allocated_stock+"+qty+"  WHERE code IN (SELECT code from order_item WHERE id=" + m_orderID+")";
    sc+= " AND branch_id= ( SELECT branch FROM orders WHERE id=" + m_orderID +")";
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
} // method DoChangeToOrder ends

bool bCheckCreditNote()
{
	string sc = " SELECT i.invoice_number FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number ";
//	sc += " WHERE o.sales_note LIKE 'credit for order #" +m_orderNumber +"' ";
	sc += " WHERE o.sales_note LIKE 'credit for invoice #" +m_invoiceNumber +"' ";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "foundLastCreditNote") >= 1)
		{
			Response.Write("<script language=javascript>window.alert('Already Credited, the Invoice Number: "+ dst.Tables["foundLastCreditNote"].Rows[0][0].ToString() +"');</script");
			Response.Write(">");
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

bool bCheckCustQuotes(string custID)
{
	if(dst.Tables["custQuotes"] != null)
		dst.Tables["custQuotes"].Clear();
	string sc = "SELECT * FROM orders WHERE card_id ="+ custID +"AND status = 1 AND type=1 AND sales IS NOT NULL AND system=0";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "custQuotes") <=0)
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

string  getQuotePrice(string m_custID, string code)
{
	
       string Str = code.Trim();
       double Num;
       bool isNum = double.TryParse(Str, out Num);
	if(dst.Tables["QuotePrice"] != null)
		dst.Tables["QuotePrice"].Clear();
	string sc = "SELECT oi.commit_price FROM order_item oi JOIN orders o ON oi.id = o.number";
	sc += " WHERE o.card_id = "+ m_custID;
	sc += " AND o.status = 1 AND o.type= 1 AND o.sales IS NOT NULL AND o.system=0";
	if(isNum)
		sc += " AND oi.code ="+code;
	else
		sc += " AND oi.supplier_code="+code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "QuotePrice") <=0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
    string m_QuotesPrice = dst.Tables["QuotePrice"].Rows[0]["commit_price"].ToString();
	return m_QuotesPrice;
}
</script>



<asp:Label id=LFooter runat=server/>


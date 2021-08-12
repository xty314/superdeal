<!-- #include file="kit_fun.cs" -->
<!-- #include file="sales_function.cs" -->
<!-- #include file="card_function.cs" -->
<!-- #include file="fifo_f.cs" -->
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
string m_url = "";
string m_cardID = "0";

string m_orderStatus = "1"; //Being Processed

double m_dFreight = 0;
double m_dInvoiceTotal = 0;
string m_discount = "0";
int m_nSearchReturn = 0;
string tableWidth = "97%";

bool b_create = false;
bool m_bCreditReturn = false;
bool m_bOrderCreated = false;
bool m_bNoIndividualPrice = false;
string m_customerID_PDA = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

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
		Response.Redirect("pos_retail.aspx" + par);
		return;
	}
    if(Request.Form["customer_id"] != null && Request.Form["customer_id"] != "-1")
    {
        m_customerID = Request.Form["customer_id"];
		Session[m_sCompanyName + "customerid" + m_ssid] = m_customerID;
		GetCustomer();
    }

    if(g_bPDA)
        tableWidth = "95%";
	string defButton = "Recalculate Price";
	
	m_dGstRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "15")) / 100;
	
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
		Session["SalesType" + m_ssid] = m_quoteType;
		Session["order_created" + m_ssid] = null;
					
	}
	else if(Request.QueryString["p"] == "pay")
	{
		PrintPaymentForm();
		return;
	}
	else if(Request.QueryString["p"] == "end")
	{
		if(Request.QueryString["paylater"] != "1")
		{
			if(!DoReceivePayment())
				return;
		}
		else
		{
			m_invoiceNumber = Request.QueryString["i"];
			if(m_invoiceNumber == null || m_invoiceNumber == "")
			{
				MsgDie("Error, invoice number needed");
				return;
			}
		}
		Response.Write("<html>");
		Response.Write("<body onload='window.print()'>");
		Response.Write(BuildInvoice(m_invoiceNumber));
	
	//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?p=new\">");
		return;
	}

	m_bOrder = true;
	Session[m_sCompanyName + "_ordering"] = true;
	Session[m_sCompanyName + "_salestype"] = "quick_sales";
	if(Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] == null)
		Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] = "1";

	//Print SN in invoice Options
	if(Request.QueryString["psn"] == "y")
		Session["print_sn" + m_ssid] = "true";
	else if(Request.QueryString["psn"] == "n")
		Session["print_sn" + m_ssid] = null;
	if(Session["print_sn" + m_ssid] != null)
		m_bPrintSN = true;						

	m_bSales = true; //switch in cart.cs
	m_url = "pos_retail.aspx";
	string s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "&r=" + DateTime.Now.ToOADate();

	m_sales = "";//Session["card_id"].ToString(); //default
    if(Session[m_sCompanyName + "customerid" + m_ssid] != null && Session[m_sCompanyName + "customerid" + m_ssid].ToString() != "")
		m_customerID = Session[m_sCompanyName + "customerid" + m_ssid].ToString();
    //DEBUG("id555", m_customerID);
	//remember everything entered in Session Object
	if(Request.Form["branch"] != null)
		UpdateAllFields();

	RestoreAllFields();

	//customer account search
//	if(Request.Form["branch"] != null && Request.Form["ckw"] != m_customerID)
//	{
//		DoCustomerSearchAndList();
//		return;
//	}
	
/*	if(g_bPDA && Request.Form["customer_ID"] != "-1")
    {
        
        m_customerID = Request.Form["customer_ID"];
    }
*/
    //item search
	if((Request.Form["item_code_search"] != null && Request.Form["item_code_search"] != "") || (Request.Form["item_option"] != null && Request.Form["item_option"] != "-1"))
	{
		if(!g_bPDA)
            Session["m_customer_po_number" + m_ssid] = m_custpo;
		Session["m_sales_note" + m_ssid] = m_salesNote;

//******************* Code for serial search ************************************
		string s_SearchMsg = "";	
		
        s_SearchMsg = DoSerialSearch(Request.Form["item_code_search"]);
//*******************************************************************************

//		string s_SearchMsg = "notfound";
		if(s_SearchMsg == "found")
		{
			//found product by serial;
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + s_url + "\">");	
			return;
		}
		else if(s_SearchMsg == "notfound")
		{	
			if(IsInteger(Request.Form["item_code_search"]) || IsInteger(Request.Form["item_option"]) )
			{
				//true - means find exactly, false - mean find similar;
                if(Request.Form["item_option"] != null && Request.Form["item_option"] != "-1")
                {
                    if(!DoSearchItem(Request.Form["item_option"], true))
				    {
					//if(!DoSearchItem(Request.Form["item_code_search"], false))	
					    return;
				    }
				    if(m_nSearchReturn <= 0)
				    {
					    if(!DoSearchItem(Request.Form["item_option"], false))	
						    return;
					
					    if(m_nSearchReturn <=0 )
					    {
						    PrintAdminHeader();
                            Response.Write("<br><br><center><h3>No Item's code matches <b>" + Request.Form["supplier_code_search"] + "</b></h3>");
						    Response.Write("<input type=button  "+ Session["button_style"] +"  value=Back onclick=history.go(-1)>");
						    return;
					    }

					    PrintSearchForm();
					    LFooter.Text = m_sAdminFooter;
					    return;
				    }
                }
                else if(IsInteger(Request.Form["item_code_search"]))
                {
				    if(!DoSearchItem(Request.Form["item_code_search"], true))
				    {
					//if(!DoSearchItem(Request.Form["item_code_search"], false))	
					    return;
				    }
				    if(m_nSearchReturn <= 0)
				    {
					    if(!DoSearchItem(Request.Form["item_code_search"], false))	
						    return;
					    if(!DoMPNSearch(Request.Form["item_code_search"]))
                            return;
                            
					    if(m_nSearchReturn <=0 )
					    {
						    PrintAdminHeader();
                            if(!g_bPDA)
						        PrintAdminMenu();
						    Response.Write("<br><br><center><h3>No Item's code matches <b>" + Request.Form["supplier_code_search"] + "</b></h3>");
						    Response.Write("<input type=button  "+ Session["button_style"] +"  value=Back onclick=history.go(-1)>");
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
                        if(!g_bPDA)
					        PrintAdminMenu();
					    Response.Write("<br><br><center><b>Search Result of  <font size=+1 color=red>" + Request.Form["item_code_search"] + "</b></font>");
					    Response.Write("<br><br><b>as S/N : Not Found!<br><br></b>");
					    Response.Write("<b>as product code : Not Found --- Not Valid Product Code!</b><br>");
					    Response.Write("<b>as supplier code : Not Found --- Not Valid Product Code!</b>");
				    }
				    return;
                    
                }
			}
			else
			{
				DoMPNSearch(Request.Form["item_code_search"]);
				if(m_nSearchReturn <= 0)
				{
					PrintAdminHeader();
                    if(!g_bPDA)
					    PrintAdminMenu();
					Response.Write("<br><br><center><b>Search Result of  <font size=+1 color=red>" + Request.Form["item_code_search"] + "</b></font>");
					Response.Write("<br><br><b>as S/N : Not Found!<br><br></b>");
					Response.Write("<b>as product code : Not Found --- Not Valid Product Code!</b><br>");
					Response.Write("<b>as supplier code : Not Found --- Not Valid Product Code!</b>");
				}
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
		AddToCart(Request.QueryString["code"], Request.QueryString["supplier"], Request.QueryString["supplier_code"], "1", Request.QueryString["pri"],Request.QueryString["pri"],"","");
        //AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), "", "");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "\">");
		return;
	}

	if(m_sSalesType == "" && m_quoteType != "")
		m_sSalesType = GetEnumValue("receipt_type", m_quoteType);

	if(Request.QueryString["t"] == "del")
	{
		PrintAdminHeader();
        if(!g_bPDA)
		    PrintAdminMenu();
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
				Response.Write("<input type=button  "+ Session["button_style"] +"  onclick=window.location=('");
				Response.Write("olist.aspx?t=1&r=" + DateTime.Now.ToOADate() + "') value='Back to Order List'></center>");
			}
			else
			{
				Response.Write("<br><br><h3>Error Deleting Order");
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
	
	//order number
	if(Session["m_customer_po_number" + m_ssid] != null)
	{
		m_custpo = Session["m_customer_po_number" + m_ssid].ToString();
        
	}
	
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
		Session[m_sCompanyName + "customerid" + m_ssid] = m_customerID;
		GetCustomer();
        ApplyPriceForCustomer();
		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?i=" + m_orderID + "&ssid=" + m_ssid);
		Response.Write("\">");
		return;
	}
	else if(Request.QueryString["search"] == "1")
	{
		DoCustomerSearchAndList();
		return;
	}
	else if(Request.Form["cmd"] == "Select From Categories")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=c.aspx?ssid=" + m_ssid + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Update Order")
	{
		PrintAdminHeader();
        if(!g_bPDA)
		    PrintAdminMenu();
		if(DoUpdateOrder())
		{
			Response.Write("<br><br><center><h3>Order Updated.</h3>");
			Response.Write("<input type=button  "+ Session["button_style"] +"  onclick=window.location=('");
			Response.Write("olist.aspx?r=" + DateTime.Now.ToOADate() + "') value='Back to Order List'></center>");
		}
		else
		{
			Response.Write("<br><br><h3>Error updating order");
		}
		PrintAdminFooter();
		return;
	}
	else if(Request.Form["cmd"] == "Delete Order")
	{
		PrintHeaderAndMenu();
		string delkey = DateTime.Now.ToOADate().ToString();
		Session["delete_order_key" + m_ssid] = delkey;
		Response.Write("<script Language=javascript");
		Response.Write(">");
//		Response.Write(" rmsg = window.prompt('Are you sure you want to delete this order?')\r\n");
		Response.Write("if(window.confirm('Dear " + Session["name"]);
		Response.Write("\\r\\n\\r\\nAre you sure you want to delete this order?         ");
		Response.Write("\\r\\nThis action cannot be undo.\\r\\n");
		Response.Write("\\r\\nClick OK to delete order.\\r\\n");
		Response.Write("'))");
		Response.Write("window.location='pos_retail.aspx?ssid=" + m_ssid + "&t=del&id=" + m_orderID + "&r=" + delkey + "';\r\n");
		Response.Write("else window.location='pos_retail.aspx?ssid=" + m_ssid + "&id=" + m_orderID + "&r=" + delkey + "';\r\n");
		Response.Write("</script");
		Response.Write(">");
		return;
	}
	else if(Request.Form["cmd"] == "New Sales")
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
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "&p=new\">");
		return;
	}
	else if(Request.Form["cmd"] == "Set" || (Request.Form["discount_total"] != null && Request.Form["discount_total"] != "") )
	{
		GetCustomer();		
		if(DoApplyDiscountTotal(m_dGstRate,false))
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "\">");
		return;
		return;
	}
	else if(Request.Form["cmd"] == "Record")
	{
		dtCart = (DataTable)Session["ShoppingCart" + m_ssid];
		if(dtCart.Rows.Count <= 0)
		{
			
            PrintAdminHeader();
            if(!g_bPDA)
			    PrintAdminMenu();
			Response.Write("<br><br><center><h3>Error. Cannot create empty order");
			PrintAdminFooter();
			return;
		}
		if(Request.Form["sales"] == "-1")
		{
			
            PrintAdminHeader();
            if(!g_bPDA)
			    PrintAdminMenu();
			Response.Write("<br><br><center><h3>Please select Sales Person</h3>");
			Response.Write("<input type=button value=' Back ' onclick=history.go(-1)  "+ Session["button_style"] +" >");
			PrintAdminFooter();
			return;
		}
		if(!g_bPDA)
        {
            m_custpo = Request.Form["custpo"];
		    m_custpo = m_custpo.Replace("\"", "-");
		    m_custpo = m_custpo.Replace("'", "-");
            Session["m_customer_po_number" + m_ssid] = m_custpo;
        }
		m_salesNote = Request.Form["note"];
		
		
		Session["m_sales_note" + m_ssid] = m_salesNote;

		b_create = true; //MyMoneyParse(Request.Form["totaldue"]) > 0;
		if(b_create)
		{
			if(!CheckBottomPrice())
				return;

			if(!DoCreateOrder(false, m_custpo, m_salesNote)) // false means not system quotation
			{
			
                Response.Write("<h3>ERROR CREATING QUOTE</h3>");
				return;
			}
			m_bOrderCreated = true;
			Session["order_created" + m_ssid] = true;

			if(!g_bPDA)
            {
                if(!CreateInvoice(m_orderID))
			    {
				
                    Response.Write("<h3>Error Create Invoice</h3>");
				    return;
			    }
			   /* else
			    {
				    if(!DoReceivePayment())
				    {
					
                        Response.Write("<h3>Error, Record Payment</h3>");
					    return;
				    }
				
			    }*/
            }
			
//			Response.Write("<html>");
//			Response.Write("<body onload='window.print()'>");
//			Response.Write(BuildInvoice(m_invoiceNumber));

/*			Response.Write("<script Language=javascript");
			Response.Write(">");
			Response.Write("window.open('invoice.aspx?n=" + m_invoiceNumber + "')");
			Response.Write("</script");
			Response.Write(">");
*/			if(!g_bPDA)
            {
			    //Response.Write("<div align=center><input type=button name=cmd value='Print Invoice' onclick=\"window.open('pos_retail.aspx?ssid=" + m_ssid + "&p=end&paylater=1&i=" + m_invoiceNumber + "')\" >");
			    //Response.Write("&nbsp;&nbsp;<input type=button onclick=\"window.location=('pos_retail.aspx?p=new')\" value='New Invoice'></div>");
                Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=esales.aspx?i=" +m_invoiceNumber +"&r=" + DateTime.Now.ToOADate() + "\">"); 
            }
            else
            {
                Response.Write("<div align=center><input type=button onclick=\"window.location=('pos_retail.aspx?p=new')\" value='New Invoice' size=20 style='height:80px;font-size:28px' ></div>");
            }
         //   Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "&p=end&paylater=1&i=" + m_invoiceNumber + "\">");
//     		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "&p=pay&i=" + m_invoiceNumber + "&total=" + m_dInvoiceTotal + "\">");
//			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?p=new\">");
//			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=custpay.aspx?id=" + m_customerID + "&amount=" + m_dInvoiceTotal + "\">");
			return;

			PrintAdminHeader();
            if(!g_bPDA)
			    PrintAdminMenu();
//			PrintHeaderAndMenu();
			Response.Write("<br><br><center><h3>" + m_sSalesType.ToUpper() + " Created</h3>");
			Response.Write("<h5>Number : </h5><h1><font color=red>" + m_orderID + "</h1><br>");
			Response.Write("<input type=button  "+ Session["button_style"] +"  ");
			Response.Write("onclick=window.location=('invoice.aspx?n=" + m_invoiceNumber + "') ");
			Response.Write(" value='Print Invoice'>");
			Response.Write("<br><br><br><br><br>");
//			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eorder.aspx?id=" + m_orderID + "&r=" + DateTime.Now.ToOADate() + "\">");
//			PrintSearchForm();
            LFooter.Text = m_sAdminFooter;
			return;
		}
	}

	if(m_orderNumber != "")
	{
		m_tableTitle = m_sSalesType.ToUpper() + " #<font color=red>" + m_orderNumber + "</font>";
		m_tableTitle += " - <font color=green>" + GetEnumValue("order_item_status", m_orderStatus) + "</font>";
	}
	else
		m_tableTitle = "NEW " + m_sSalesType.ToUpper();
    //DEBUG("id", m_customerID);
	PrintAdminHeader();
    if(!g_bPDA)
	    PrintAdminMenu();
	MyDrawTable();
	LFooter.Text = m_sAdminFooter;
}

bool RestoreCustomer()
{
	string status_invoiced = GetEnumID("order_item_status", "Invoiced");
	string status_shipped = GetEnumID("order_item_status", "Shipped");
	if(status_invoiced == "")
	{
		Response.Write("<br><br><center><h3>Error getting status ID 'Being Processed'");
		return false;
	}

	string sc = " SELECT branch, number, po_number, card_id, freight, shipping_method ";
	sc += ", special_shipto, shipto, pick_up_time, status ";
	sc += ", sales, sales_note, locked_by, time_locked, no_individual_price ";
	sc += " FROM orders ";
	sc += " WHERE status<>" + status_invoiced + " AND status<>" + status_shipped;
	sc += " AND id=" + m_orderID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "order") <= 0)
		{
			Response.Write("<br><br><center><h3>ERROR, Order Not Found</h3>");
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
			Response.Write("<br><br><center><h3><font color=red>ORDER LOCKED</font></h3><br>");
			Response.Write("<h4>This order is locked by <font color=blue>" + lockname.ToUpper() + "</font> since " + locktime);
			PrintAdminFooter();
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

	m_orderStatus = dr["status"].ToString();
	m_branchID = dr["branch"].ToString();
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
	m_bNoIndividualPrice = MyBooleanParse(dr["no_individual_price"].ToString());
	string nip = "0";
	if(m_bNoIndividualPrice)
		nip = "1";

	Session["sales_current_freight" + m_ssid] = m_dFreight;
	Session[m_sCompanyName + "customerid" + m_ssid] = m_customerID;
	Session["sales_shipping_method" + m_ssid] = m_nShippingMethod ;
	Session["sales_special_shipto" + m_ssid] = m_specialShipto;
	Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
	Session["sales_pick_up_time" + m_ssid] = m_pickupTime;
	Session["sales_no_individual_price" + m_ssid] = nip;

	dr = GetCardData(m_customerID);
	if(dr != null)
	{
		Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] = dr["dealer_level"].ToString();
		Session[m_sCompanyName + "_card_type_for_pos" + m_ssid] = dr["type"].ToString();
	}

	return true;
}

bool RestoreOrder()
{
	PrepareNewSales(); //empty session sales data 

	if(!RestoreCustomer())
		return false;

	int items = 0;
	string sc = "SELECT * FROM order_item WHERE id=" + m_orderID + " AND kit=0 ";
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
			dr["item_name"].ToString(), "") )
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
	//cut input string length not longer than 9
	string s = kw;
	if(s.Length > 9)
	{
		for(int i=0; i<9; i++)
			kw = s[i].ToString();
	}
	//DEBUG("kw = ", kw);
	string sc = "SELECT p.code, p.supplier, p.supplier_code, p.name, ISNULL(p.supplier_price, 0) AS supplier_price, c.price1 as pos_price ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code WHERE c.inactive = 0 AND p.code ";// LIKE '"+ kw;
	if(!btype)
	{
		sc += " LIKE '" + kw + "%'  OR c.code = (SELECT item_code FROM barcode WHERE barcode ='"+ kw +"')";
	}
	else
		sc += " = '" + kw + "' OR c.code = (SELECT item_code FROM barcode WHERE barcode ='"+ kw +"')";
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

//		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);		
		AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), "", "");
//		string s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "&r=" + DateTime.Now.ToOADate();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "\">");	
		return false; //end response
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

bool DoMPNSearch(string kw)
{
	kw = EncodeQuote(kw);
	string sc = "SELECT p.code, p.supplier, p.supplier_code, p.name, ISNULL(p.supplier_price, 0) AS supplier_price, c.price1 as pos_price ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code WHERE c.inactive = 0 AND ( p.supplier_code ";// LIKE '"+ kw;
	sc += " LIKE '" + kw + "%' OR upper(p.name) LIKE upper('%" + kw + "%') )";
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
        string pos_price = dr["pos_price"].ToString();

//		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);		
		AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), "", "");
//		string s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "&r=" + DateTime.Now.ToOADate();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "\">");	
		return false; //end response
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
	
	//Response.Write("<td>SupplierPrice</td>\r\n");
    Response.Write("<td>Selling Price</td>\r\n");
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
		//string price = dr["supplier_price"].ToString();
        string pos_price = dr["pos_price"].ToString();
        //DEBUG("id= ", m_customerID);
        double price = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
		Response.Write("<tr" + scolor + ">");

		//Link on add items' Product ID
		Response.Write("<td><a href='pos_retail.aspx?ssid=" + m_ssid + "&a=add&code="+code+"&supplier="+supplier);
		Response.Write("&supplier_code=" + supplier_code + "&pri=" + price.ToString() + "&r=" + DateTime.Now.ToOADate() + "'>");
		Response.Write(code + "</a></td>\r\n");

		//Link to add items to sales from search result
		Response.Write("<td><a href='pos_retail.aspx?ssid=" + m_ssid + "&a=add&code="+code+"&supplier="+supplier);
		Response.Write("&supplier_code=" + supplier_code + "&pri=" + price.ToString() + "&r=" + DateTime.Now.ToOADate() + "'>");
		Response.Write(name + "</a></td>\r\n");

		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + supplier_code+ "</td>");
		
		Response.Write("<td>" + (double.Parse(pos_price)/1.15).ToString("c") + "</td>");
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

	if(Session[m_sCompanyName + "customerid" + m_ssid] != null && Session[m_sCompanyName + "customerid" + m_ssid].ToString() != "")
		m_customerID = Session[m_sCompanyName + "customerid" + m_ssid].ToString();

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
	if(Session["sales_no_individual_price" + m_ssid] != null && Session["sales_no_individual_price" + m_ssid] != "")
		m_bNoIndividualPrice = MyBooleanParse(Session["sales_no_individual_price" + m_ssid].ToString());
}

bool UpdateAllFields()
{
//	m_discount = Request.Form["discount"];
//	if(!TSIsDigit(m_discount))
//		m_discount = Session["sales_current_order_discount"].ToString();
//	else
//		Session["sales_current_order_discount"] = m_discount;

	m_sales = Request.Form["sales"];
	if(!g_bPDA)
        m_custpo = Request.Form["custpo"];
	m_salesNote = Request.Form["note"];
	if(Request.Form["branch"] != null && Request.Form["branch"] != "")
		m_branchID = Request.Form["branch"];
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

	string nip = "0";
	if(Request.Form["nip"] == "on")
		nip = "1";
	m_bNoIndividualPrice = MyBooleanParse(nip);
    if(!g_bPDA)
	    Session["m_customer_po_number" + m_ssid] = m_custpo;
	Session["m_sales_note" + m_ssid] = m_salesNote;
	Session["brach_id"] = m_branchID;
	Session["sales_shipping_method" + m_ssid] = m_nShippingMethod;
	Session["sales_special_shipto" + m_ssid] = m_specialShipto;
	Session["sales_special_ship_to_addr" + m_ssid] = m_specialShiptoAddr;
	Session["sales_pick_up_time" + m_ssid] = m_pickupTime;
	Session["sales_no_individual_price" + m_ssid] = nip;

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
			//DEBUG("sprice_odl = ", sprice_old);
			if(sprice_old != null)
				dPriceOld = MyMoneyParse(sprice_old);
			quantity = MyIntParse(sqty);
			quantityOld = MyIntParse(sqty_old);
            //DEBUG("dPriceOld = ", dPriceOld);
			if(quantity == 0 || Request.Form["del" + i.ToString()] == "X") //do delete
			{
				dtCart.Rows.RemoveAt(i);
				
				if(Session["pack"+i.ToString()] != null)
					Session["pack"+i.ToString()] = null;//CH
					
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pos_retail.aspx?ssid=" + m_ssid + "\">");
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
            //DEBUG("dPrice = ", dPrice);
			if(quantity != quantityOld)
			{
				dtCart.Rows[i].BeginEdit();
				dtCart.Rows[i]["quantity"] = quantity;
				if(dPrice != dPriceOld)
				{
//DEBUG("p = ", dPrice);
                    dtCart.Rows[i]["salesPrice"] = dPrice.ToString();
				}
				else
				{
					double dQtyPrice = dPrice;
					if(kit != "1")
						dQtyPrice = GetSalesPriceForDealer(dtCart.Rows[i]["code"].ToString(), sqty, Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);		
						//dQtyPrice = GetSalesPriceForDealer(dtCart.Rows[i]["code"].ToString(), sqty, Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
					dtCart.Rows[i]["salesPrice"] = dQtyPrice.ToString();
//DEBUG("code = ", dtCart.Rows[i]["code"].ToString());
//DEBUG("sqty = ", sqty);	
//DEBUG("sess =", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString());
//DEBUG("m_customerID = ", m_customerID);	
				}
				dtCart.Rows[i].EndEdit();			
			}
			else if(sprice != sprice_old)
			{
				dtCart.Rows[i]["salesPrice"] = dPrice.ToString();
			}
			dtCart.Rows[i]["name"] = Request.Form["name" + i];
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

	Response.Write("<form name=form1 action=pos_retail.aspx?ssid=" + m_ssid + " method=post>");
	if(!g_bPDA)
    {
        Response.Write("<table width=100% height=100% bgcolor=white align=center valign=center><tr><td valign=top>");
	
//	Response.Write("<br><center><h3>" + m_tableTitle + "</h3></center>");
	//Response.Write("<br><center><h3>NEW SALES</h3></center>");
	    Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	    Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	    Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>NEW SALES</b><font color=red><b>");
	
	    Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	    Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	    Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	    Response.Write("</tr></table>");	
    }
	//print sales header table
	if(!g_bPDA)
	{
		if(!PrintSalesHeaderTable(m_custpo))
			return false;
	}
	else
	{
		if(!PrintSalesHeaderTable_pda())
			return false;
	}
//	Response.Write("<table class=d align=center valign=center cellspacing=1 cellpadding=0 border=1>");

	Response.Write("<table width='"+ tableWidth +"'  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr><td colspan=" + m_cols + ">");

//	if(!PrintShipToTable())
//		return false;

//	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=" + m_cols + ">");
	if(!PrintCartItemTable())
		return false;

	Response.Write("</td></tr>");

	//start comment table
	Response.Write("<tr><td>&nbsp</td></tr><tr><td><b>&nbsp;Comment : </b></td></tr>");
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

//	Response.Write("<br>");
	Response.Write("<table border=1 cellpadding=1 width=100% cellspacing=0");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
    if(!g_bPDA)
    {
	    Response.Write("<td><b>PACK</b></td>");
	    Response.Write("<td width=130 align=left>STOCK ID</td>");
        Response.Write("<td width=70>CODE</td>");
	    Response.Write("<td width=45% >DESCRIPTION</td>");
	    Response.Write("<td width=10%  align=right>PRICE</td>");
        Response.Write("<td width=8%  align=right>STOCK</td>");
        Response.Write("<td  width=5% align=right>QTY</td>");
	    Response.Write("<td align=right>TOTAL</td></tr>");
    }
    else
    {
        Response.Write("<td width=5% align=center><font size=-2>Code</font></td>");
	    Response.Write("<td width=30% align=center><font size=-2>Name</font></td>");
	    Response.Write("<td width=30% align=center><font size=-2>Price</font></td>");
        Response.Write("<td  width=30% align=center><font size=-2>QTY</font></td>");
	    Response.Write("<td width=5% align=center><font size=-2>Total</font></td></tr>");
    }

	
        

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
		dSalesPrice = Math.Round(dSalesPrice, 3);
		dRowTotal = dSalesPrice * quantity;
		dRowTotal = Math.Round(dRowTotal, 3);
		string s_prodName = dr["name"].ToString();

		string supplierCode = dr["supplier_code"].ToString();
		if(m_bOrder)
			SSPrintOneRow(i, dr["code"].ToString(), supplierCode, s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString());
		else
			SSPrintOneRow(i, dr["code"].ToString(), dr["code"].ToString(), s_prodName, dsupplierPrice, dSalesPrice, quantity, dRowTotal, dr["s_serialNo"].ToString());

		dTotalPrice += dRowTotal;
		dCost += dsupplierPrice;
	}
	
	if(Request.Form["freight"] != null && Request.Form["freight"] != "")
	{
		m_dFreight = double.Parse(Request.Form["freight"], NumberStyles.Currency, null);
		Session["sales_freight" + m_ssid] = m_dFreight;
	}
	dTotalPrice = Math.Round(dTotalPrice + m_dFreight, 3);

	double dFinal = dTotalPrice;
	if(Request.Form["total"] != null && Request.Form["total"] != "")
	{
		if(Request.Form["total"] != Request.Form["total_old"])
			dFinal = double.Parse(Request.Form["total"], NumberStyles.Currency, null);
	}

	dTotalGST = dTotalPrice * m_dGstRate;

	dAmount = dTotalPrice + dTotalGST;

	double discount = 0;
	if(dTotalPrice > 0)
		discount = Math.Round((dTotalPrice - dFinal) / dTotalPrice * 100, 0);

	//put an empty row for user input, which is used to search product by code or SN;
//	if(m_quoteType != "3" || Request.QueryString["p"] == "new")
	if(!g_bPDA)
        Response.Write("<tr><td colspan=7><input type=text name=item_code_search size=8 value=''>");
    else
        Response.Write("<tr height='50'><td colspan=5 align=right><input type=text name=item_code_search size=13 value='' style='height:35px; ' >");
    if(!g_bPDA)
	    Response.Write("<input type=submit name=cmd  "+ Session["button_style"] +"  size=8 value='Select From Categories'>");
    else
        Response.Write("&nbsp;&nbsp;<input type=submit name=cmd  "+ Session["button_style"] +"  size=55 value='&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Add Item&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;' style='height:40px;'>");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.form1.item_code_search.focus();");
	Response.Write("</script");
	Response.Write(">");
    if(!g_bPDA)
    {
	    Response.Write("</td><td colspan=1 align=right>");
        Response.Write("<input type=submit  name=cmd  "+ Session["button_style"] +"  value='Recalculate Price'></td></tr>");
    }
    else
    {
        Response.Write("</td></tr><tr height='50'><td colspan=5 align=right>");
        Response.Write("<input type=submit  name=cmd  "+ Session["button_style"] +"  value='Recalculate Price' style='height:40px;'></td></tr>");
    }

//	Response.Write("<b>Discount : </b>");
//	Response.Write("<input type=text size=1 style='text-align:right' align=right name=discount value='" + discount.ToString() + "'> % ");

	

	
    if(!g_bPDA)
    {
        Response.Write("<tr bgcolor=#EEE999><td colspan=" + (m_cols).ToString() + ">&nbsp;</td>");
	//sub total
	    Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-1).ToString() + " align=right>");
	    Response.Write("<b>Sub-Total : </b></td><td align=right>");
	    Response.Write(dTotalPrice.ToString("c"));
	    Response.Write("<input type=hidden name=subtotal value=" + dTotalPrice + ">");
	    Response.Write("</td></tr>");
  
	//total GST
	    Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-1).ToString() + " align=right>");
	    Response.Write("<b>TAX : </b></td>");
	    Response.Write("<td align=right><b>");
	    Response.Write(dTotalGST.ToString("c"));
	    Response.Write("</b></td></tr>");

	//total amount due
	    Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-1).ToString() + " align=right>");
	    Response.Write("<b>Total Amount Due : </b></td>");
	    Response.Write("<td align=right><b>");
	    Response.Write(dAmount.ToString("c"));
	    Response.Write("</b><input type=hidden name=totaldue value='" + dAmount.ToString("c") + "'</td></tr>");



	    double dCredit = 0;
	    double dBalance = 0;
	    bool bCredit = true;//CreditLimitOK(m_customerID, dAmount, ref dCredit, ref dBalance, false);
	    double dAvailable = dCredit - dBalance - dAmount;

	    if(m_customerID != "" && m_customerID != "0")
	    {
		    Response.Write("<tr bgcolor=#EEEEE><td colspan=" + (m_cols-1).ToString() + " align=right>");
		    Response.Write("<b>Credit Available : </b></td>");
		    Response.Write("<td align=right title='Credit Limit : " + dCredit.ToString("c") + ", Account Balance : " + dBalance.ToString("c") + "'>");
		    if(bCredit)
			    Response.Write("<font color=green>");
		    else
			    Response.Write("<font color=red>");
		    Response.Write("<b>" + ((dCredit==0) ? "No Limit" : dAvailable.ToString("c")) + "</b></td></tr>");
	    }
  
	    Response.Write("<tr bgcolor=#EEEEE>");
	//Response.Write("<input type=submit name=cmd  "+ Session["button_style"] +"  value='New Sales'>");
	    Response.Write("<td colspan=" + (m_cols - 1).ToString() + " align=right>");

	    Response.Write("<b>Discount Total(GST Inc):</b></td><td align=right><input type=text name=discount_total size=5 style=text-align:right>");
	    Response.Write("<input type=submit name=cmd value=Set  "+ Session["button_style"] +" > ");
	    Response.Write("</td></tr>");
	
	/*
	//payment option
	    Response.Write("<tr bgcolor=#EEEEE>");
	    Response.Write("<td colspan=" + (m_cols - 1).ToString() + " align=right>");
	    Response.Write("<input type=hidden name=amount value='"+dAmount+"'>");
	    Response.Write("<b>Cash:</b></td><td align=right><input type=text name=cash onFocus=\" ");
	    Response.Write(" if(cal('option') ==0){ this.value = '" +dAmount+"'; clearup('cash');}");
	    Response.Write(" this.value=("+  dAmount +" - document.all.eftpos.value - document.all.cheque.value - document.all.cc.value).toFixed(2); this.select();\" ");
	    Response.Write(" onkeyup=\"if(cal('payment') == false){this.value='';}\"");
	    Response.Write(" style=\"text-align:right\">  ");
	    Response.Write("</td></tr>");
	
	    Response.Write("<tr bgcolor=#EEEEE>");
	    Response.Write("<td colspan=" + (m_cols - 1).ToString() + " align=right>");
	    Response.Write("<b>Eftpos:</b></td><td align=right> <input type=text name=eftpos onFocus=\" ");
	    Response.Write(" if(cal('option') ==0){ this.value = '" +dAmount+"'; clearup('eftpos');}");
	    Response.Write("this.value=("+  dAmount +" - document.all.cheque.value - document.all.cash.value-document.all.cc.value).toFixed(2);this.select();\" ");
	    Response.Write(" onKeyUp=\" if(cal('payment') == false){ this.value='';}\" ");
	    Response.Write(" style=\"text-align:right\">");
	    Response.Write("</td></tr>");
	
	    Response.Write("<tr bgcolor=#EEEEE>");
	    Response.Write("<td colspan=" + (m_cols - 1).ToString() + " align=right>");
	    Response.Write("<b>Credit Card:</b> </td><td align=right><input type=text name=cc onFocus=\" ");
	    Response.Write(" if(cal('option') ==0){ this.value = '" +dAmount+"'; clearup('credit');}");
	    Response.Write("this.value=("+  dAmount +" - document.all.eftpos.value - document.all.cash.value - document.all.cheque.value).toFixed(2);this.select();\" ");
	    Response.Write(" onKeyUp = \"if(cal('payment')== false){this.value='';}\" ");
	    Response.Write(" style=\"text-align:right\">");
	    Response.Write("</td></tr>");
	
	    Response.Write("<tr bgcolor=#EEEEE>");
	    Response.Write("<td colspan=" + (m_cols - 1).ToString() + " align=right>");
	    Response.Write("<b>Cheque:</b> </td><td align=right><input type=text name=cheque onFocus=\" ");
	    Response.Write(" if(cal('option') ==0){ this.value = '" +dAmount+"'; clearup('cheque');}");
	    Response.Write("this.value=("+  dAmount +" - document.all.eftpos.value - document.all.cash.value - document.all.cc.value).toFixed(2);this.select();\" ");
	    Response.Write(" onKeyUp = \"if(cal('payment')== false){this.value='';}\" ");
	    Response.Write(" style=\"text-align:right\">");
	    Response.Write("</td></tr>");
	*/
	    Response.Write("<tr bgcolor=#EEEEE align=right><td colspan=" + (m_cols).ToString() + " >");
	    Response.Write("<input type=submit name=cmd  "+ Session["button_style"] +"  value='New Sales'>");
	    if(m_bOrderCreated)
	    {
//		if(bCredit)
		    {
		        Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Delete Order'>");
			    Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Update Order'>");
			    Response.Write("<input type=hidden name=order_id value=" + m_orderID + ">");
		    }
	    }
	    else
	    {
//		if(bCredit)
			//Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Record' onClick=\"if(cal('checkout') == false){event.returnValue = false;}\">");
            Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Record' >");
	    }
	    Response.Write("</td></tr>");

	    Response.Write("<tr><td colspan=" + m_cols + " align=right><font color=red><b>");
//	Response.Write("Important : Click 'Update Order' or 'Cancel' to unlock <br>your order before leaving this page!!");
	    Response.Write("</b></font></td></tr>");

	    Response.Write("</table>");
    }
    else
    {
      Response.Write("</table><table width=100% align=center><tr colspan=2 bgcolor=#EEE999><td>&nbsp;</td>");
	//sub total
	    Response.Write("<tr bgcolor=#EEEEE><td align=right>");
	    Response.Write("<b>Sub-Total : </b></td><td align=right>");
	    Response.Write(dTotalPrice.ToString("c"));
	    Response.Write("<input type=hidden name=subtotal value=" + dTotalPrice + ">");
	    Response.Write("</td></tr>");

	//total GST
	    Response.Write("<tr bgcolor=#EEEEE><td align=right>");
	    Response.Write("<b>TAX : </b></td>");
	    Response.Write("<td align=right><b>");
	    Response.Write(dTotalGST.ToString("c"));
	    Response.Write("</b></td></tr>");

	//total amount due
	    Response.Write("<tr bgcolor=#EEEEE><td align=right>");
	    Response.Write("<b>Total Amount Due : </b></td>");
	    Response.Write("<td align=right><b>");
	    Response.Write(dAmount.ToString("c"));
	    Response.Write("</b><input type=hidden name=totaldue value='" + dAmount.ToString("c") + "'</td></tr>");



	    double dCredit = 0;
	    double dBalance = 0;
	    bool bCredit = true;//CreditLimitOK(m_customerID, dAmount, ref dCredit, ref dBalance, false);
	    double dAvailable = dCredit - dBalance - dAmount;

	  
  
	   
	
	
	   
	
	   
	
	    
	
	    
	
	    Response.Write("<tr bgcolor=#EEEEE align=right><td colspan=2 >");
	//    Response.Write("<input type=submit name=cmd  "+ Session["button_style"] +"  value='New Sales'>");
	    if(m_bOrderCreated)
	    {
//		if(bCredit)
		    {
		        Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Delete Order'>");
			    Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Update Order'>");
			    Response.Write("<input type=hidden name=order_id value=" + m_orderID + ">");
		    }
	    }
	    else
	    {
//		if(bCredit)
			Response.Write("<input type=submit  "+ Session["button_style"] +"  name=cmd value='Record' >");
	    }
	    Response.Write("</td></tr>");



	    Response.Write("</table>");  
    }
	
	//payment calculation
	string cal =@"
	<script type=text/javascript>
	function cal(type)
	{
		
		var cash = document.all.cash.value;
		var eftpos = document.all.eftpos.value;
		var cheque = document.all.cheque.value;
		var cc = document.all.cc.value
		var amount = document.all.amount.value;
		var total = Number(cash) +  Number(eftpos) + Number(cheque) + Number(cc);
		var rest = total - Number(amount);
		if(total > amount && type=='payment')
		{
			alert('Sorry, Over Charge: $' + Math.round(rest, 2));
			return false;
		}
		else if(total < amount && type=='checkout')
		{
			if(total  == '0')
			{
				alert('Sorry, Please Select Payment');
				return false;
			}
			if(rest < 0.05)
				return true;
			rest = 0-rest;
			
			alert('Sorry, Less Charge: $' + Math.round(rest, 2));
			return false;
		}
		else if(rest == 0 && type=='option')
		{
			return rest;
		}
		
		return true;
	}
	function clearup(field)
	{
		if(field == 'cash')
		{
			document.all.eftpos.value='';
			document.all.cc.value='';
			document.all.cheque.value='';
		}
		else if(field == 'eftpos')
		{
			document.all.cash.value='';
			document.all.cc.value='';
			document.all.cheque.value='';
		}
		else if(field == 'credit')
		{
			document.all.eftpos.value='';
			document.all.cash.value='';
			document.all.cheque.value='';
		}
		else if(field == 'cheque')
		{
			document.all.eftpos.value='';
			document.all.cc.value='';
			document.all.cash.value='';
		}
	}
	";
	Response.Write(cal);
	Response.Write("</script");
	Response.Write(">");
	return true;
}
bool SSPrintOneRow(int nRow, string sID, string code, string desc, double dCost, double dPrice, int qty, double dTotal, string sSNnum)
{
	string stock = "";
	if(IsInteger(sID))
		stock = GetProductStock(sID);
	
	string pack = "";
	if(Session["pack"+nRow] != null &&(qty == 0 || Request.Form["del" + nRow.ToString()] == "X"))
		Session["pack"+nRow] = null;
		
	if(Request.Form["pack"+nRow] != null)
	{	
		Session["pack"+nRow] = Request.Form["pack"+nRow];
	}
	if(Session["pack"+nRow] != null)
		pack = Session["pack"+nRow].ToString();


	Response.Write("<tr ");
	if(bCartAlterColor)
		Response.Write("bgcolor=#EEEEEE");
	else
		Response.Write("bgcolor=white");
	bCartAlterColor = !bCartAlterColor;
	Response.Write(">");
	//pack
    if(!g_bPDA)
	    Response.Write("<td><input type =text size=5 name=pack"+nRow.ToString()+" value='"+pack+"'></td>");
    else
        Response.Write("<input type =hidden name=pack"+nRow.ToString()+" value='"+pack+"'>");
	//
//	Response.Write("<input type=hidden value='"+sSNnum+"' name='hiddenSN'>");
	//Online Store ID code;
	if(!g_bPDA)
    {
        Response.Write("<td><a href=salesref.aspx?code=");
	    Response.Write(sID);
	    Response.Write("  target=_blank>");
	    Response.Write(sID);
	    Response.Write("</a></td>\r\n");
    }

	
	
	//code
    if(!g_bPDA)
    {
	    Response.Write("<td><a href=p.aspx?");
	    Response.Write(sID);
	    Response.Write(" target=_blank>");
	    Response.Write(code);
	    Response.Write("</a></td>\r\n");
    }
    else
    {
        Response.Write("<td>");
	    Response.Write("<font size=-3>" + code + "</font>");
	    Response.Write("</td>\r\n");
    }

	//description
	Response.Write("<td>");
    if(!g_bPDA)
    {
	    Response.Write("<input type=text size=50 maxlength=255 name=name" + nRow.ToString() + " value='");
        Response.Write(desc);
    }
    else
        Response.Write("<font size=-3>" + desc + "</font>");
        
    if(!g_bPDA)
	    Response.Write("'></td>\r\n");
    else
        Response.Write("'</td>\r\n");

	//price
	if(!g_bPDA)
    {
        Response.Write("<td title=");
	    Response.Write(dCost.ToString("c"));
	    Response.Write(" align=right>");
	    Response.Write("<input type=text size=10 style='text-align:right' name=price" + nRow.ToString() + " value='");
    }
    else
    {
        Response.Write("<td>");
	    Response.Write("<input type=text size=8  style='height:30px' name=price" + nRow.ToString() + " value='");   
    }
	Response.Write(dPrice.ToString("c"));
//	Response.Write(dPrice.ToString());
	Response.Write("'><input type=hidden name=price_old" + nRow.ToString() + " value='");
	Response.Write(dPrice.ToString("c"));
	Response.Write("'></td>\r\n");
//DEBUG("price =", dPrice.ToString("c"));
	//current stock
    if(!g_bPDA)
	    Response.Write("<td align=right>" + stock + "</td>");

	//quantity
    if(!g_bPDA)
    {
	    Response.Write("<td align=right><input type=text size=3 autocomplete=off style='text-align:right' name=qty" + nRow.ToString());
	    Response.Write(" value='" + qty.ToString() + "'>");
	    Response.Write("<input type=hidden name=qty_old" + nRow.ToString() + " value='" + qty.ToString() + "'>");
	    Response.Write("<input type=submit name=del" + nRow + "  "+ Session["button_style"] +"  value='X'>");
	    Response.Write("</td>\r\n");
    }
    else
    {
        Response.Write("<td><table><tr><td><input type=text size=5 autocomplete=off style='text-align:right;height:20px' name=qty" + nRow.ToString());
	    Response.Write(" value='" + qty.ToString() + "'>");
	    Response.Write("<input type=hidden name=qty_old" + nRow.ToString() + " value='" + qty.ToString() + "'></td></tr>");
	    Response.Write("<tr><td><input type=submit name=del" + nRow + "  "+ Session["button_style"] +"  value='X'>");
	    Response.Write("</td></tr></table></td>\r\n");  
    }

	//total
	Response.Write("<td align=right>");
    if(!g_bPDA)
        Response.Write(dTotal.ToString("c"));
    else
    
        Response.Write("<font size=-5>" + dTotal.ToString("c") + "</font>");
   
	
//	Response.Write(dTotal.ToString());
	Response.Write("</td>\r\n</tr>\r\n");

	return true;
}

bool PrintShipToTable()
{
	DataRow dr = null;
	bool bCashSales = false;
	if(Session[m_sCompanyName + "customerid"] == null)
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
	//bill to
	Response.Write("<table><tr><td>");
	Response.Write("<b>Bill To : <br></b>");

	string sCompany = "";
	string sAddr = "";
	string sContact = "";

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
//		Response.Write(dr["Email"].ToString());
//		Response.Write("<br>\r\n");
	}

	Response.Write("</td></tr></table></td><td valign=top align=right>");
	
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

string DoSerialSearch(string s_SN)
{
	string s_msgSN = "";
	string sc = "SELECT sn, status, product_code, prod_desc, supplier_code, supplier, cost ";
	       sc+= "FROM stock WHERE sn = '" + s_SN + "'";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nSearchReturn = myAdapter.Fill(dst, "prod_sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}

	if(m_nSearchReturn == 1)  //> 0 )
	{
		DataRow dr = dst.Tables["prod_sn"].Rows[0];
		if(GetEnumValue("stock_status", dr["status"].ToString()) == "in stock")    // Stock Status : "2" indicating the item is in "stock";
		{
			//string sn = dr["sn"].ToString();
			string code = dr["product_code"].ToString();
			string supplier = dr["supplier"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string supplier_price = dr["cost"].ToString();
			string prod_name = dr["prod_desc"].ToString();
			string s_serialNo = s_SN;
			//double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);
			double dSalesPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "sales_dealer_level_for_pos" + m_ssid].ToString(), m_customerID);		
			AddToCart(code, supplier, supplier_code, "1", supplier_price, dSalesPrice.ToString(), prod_name, s_serialNo);
//			string s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "&r=" + DateTime.Now.ToOADate();
//			AddToCart(code, supplier, supplier_code, "1", supplier_price, prod_name, s_serialNo);
			return "found";
		}
		else
		{
			s_msgSN = "The item (SN #: " + s_SN + " ) is not for selling, it's sold already!  >_< !!!";
			return s_msgSN;
		}
	}

	return "notfound"; 
}

bool PrintCustomerSeletion()
{
	int rows = 0;
	if(dst.Tables["customeroption"] != null)
		dst.Tables["customeroption"].Clear();
	string sc = "SELECT id, trading_name FROM card WHERE type=1 OR type=2 ";
    sc += " ORDER BY trading_name";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "customeroption");
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}



    Response.Write("<select name=customer_id style='width:300px;font-size: 25px' ");
//    Response.Write(" onchange=\"window.location=('");
//    Response.Write("pos_retail.aspx?ssid=" + m_ssid + "&ci='+ this.options[this.selectedIndex].value ) \" ");
    Response.Write(">");
	Response.Write("<option value=-1>Select Customer</option>");
	for(int i=0; i<rows; i++)
	{
		string id = dst.Tables["customeroption"].Rows[i]["id"].ToString();
		string name = dst.Tables["customeroption"].Rows[i]["trading_name"].ToString();
		Response.Write("<option value='" + id + "' ");
	    if( m_customerID == id)
	        Response.Write("selected");
        
		Response.Write(">" + name + "</option>");
    }

	
	Response.Write("</select>");
//	DEBUG("test", m_customerID);
	return true;
}
bool PrintItemSeletion()
{
	int rows = 0;
	if(dst.Tables["itemoption"] != null)
		dst.Tables["itemoption"].Clear();
	string sc = "SELECT code, name, supplier_code FROM code_relations WHERE 1=1 ";
    sc += " ORDER BY name";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "itemoption");
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<select name=item_option style='width:300px;font-size: 25px'>");
	Response.Write("<option value=-1>Select Item</option>");
	for(int i=0; i<rows; i++)
	{
		string code = dst.Tables["itemoption"].Rows[i]["code"].ToString();
        string supplier_code = dst.Tables["itemoption"].Rows[i]["supplier_code"].ToString();
		string name = dst.Tables["itemoption"].Rows[i]["name"].ToString();
		Response.Write("<option value='" + code + "' ");
		Response.Write(">" + supplier_code + "&nbsp;&nbsp;" + name + "</option>");
	}
	Response.Write("</select>");
	
	return true;
    
}
bool PrintSalesHeaderTable_pda()
{
	
    string staff_id =  Session["login_card_id"].ToString();
	Response.Write("<table width='80%' align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr height=48><td aline=center >");
    Response.Write("<input type=hidden name=sales value= '"  + staff_id + "' >");
    Response.Write("<input type=hidden name=branch value= '1' >");
    if(!PrintCustomerSeletion())
		return false;
	Response.Write("</td></tr>");
	Response.Write("<tr><td aline=center>");
	if(!PrintItemSeletion())
		return false;
	Response.Write("</td></tr>");
	Response.Write("</table>");
	return true;
}
bool PrintSalesHeaderTable(string sCustomerPONumber)
{
	Response.Write("<table width='" + tableWidth +"' align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=2><br></td></tr>");
	//customer
	Response.Write("<tr><td>");
	Response.Write("<table><tr><td>");
//DEBUG("m_customerID=", m_customerID);
		//Response.Write("<input type=submit name=cmd value='Customer'  "+ Session["button_style"] +" ></td><td>");
	Response.Write("<select name=customer onclick=window.location=('pos_retail.aspx?ssid=" + m_ssid + "&search=1')>");
	Response.Write("<option value=0>Cash Sales</option>");

	if(m_customerID != "" && m_customerID != "0")
		Response.Write("<option value='" + m_customerID + "' selected>" + m_customerName + "</option>");
	Response.Write("</select>");
	Response.Write("<input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
	Response.Write("id=" + m_customerID + "','', ' width=350,height=350');\" value='View Card'  "+ Session["button_style"] +" >");
	//Response.Write("<input type=submit value=''  "+ Session["button_style"] +"  >");
	Response.Write(" </td></tr>");
	//Response.Write("<tr><td><b>Level :</b> " + Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid]);
	//Response.Write(" <b>GST-Rate :</b> " + m_dGstRate);
	//Response.Write("</td></tr>");

//	Response.Write("<tr><td><b>ACC#/Search : </b></td>");
//	Response.Write("<td><table cellpadding=0 cellspacing=0><tr><td><input type=text name=ckw size=15 value='" + m_customerID + "'>");
//	Response.Write("</td><td valign=middle><input type=submit name=cmd value=GO  "+ Session["button_style"] +" ></td></tr></table>");
//	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<input type=submit value='P.O.Number :'  "+ Session["button_style"] +"  >&nbsp;&nbsp;&nbsp;<input type=editbox name=custpo value='" + sCustomerPONumber + "'>");
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
	Response.Write("</td><td align=right valign=top>");
	Response.Write("<table><tr><td>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<tr><td><b>Branch : </b></td><td>");
		if(!PrintBranchNameOptions())
			return false;
		Response.Write("</tr>");
	}
	else
		Response.Write("<input type=hidden name=branch value=1>");

	Response.Write("<tr><td>");
	Response.Write("<b>Sales : </b></td><td>");
	Response.Write("<select name=sales>");
	PrintSalesPersonOptions(m_sales);
	Response.Write("</select></td></tr>");
//	Response.Write("<input type=submit name=cmd value='" + TSGetUserNameByID(m_sales) + "'  "+ Session["button_style"] +" >");
//	Response.Write("<input type=hidden name=sales value='" + m_sales + "'>");

	Response.Write("<tr><td colspan=2 align=right><input type=checkbox name=nip ");
	if(m_bNoIndividualPrice)
		Response.Write(" checked");
	Response.Write("><b>No Individual Price</b>");

	Response.Write("</td></tr></table>");

	Response.Write("</td></tr>");
	Response.Write("</table><br>");

	return true;
}

void PrintSalesPersonOptions(string current)
{
	int rows = 0;
	string access_id =  Session["login_card_id"].ToString();
	string sc = " SELECT id, name FROM card WHERE type = 4 ";
    if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	    sc += " AND id ="+ access_id +" ";
    sc += " ORDER BY name ";
		
	
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
	//Response.Write("<option value=-1> </option>");
	for(int i=0; i<rows; i++)
	{
		string id = dst.Tables["sales"].Rows[i]["id"].ToString();
		Response.Write("<option value=");
		Response.Write(id);
		if(id == access_id )
			Response.Write(" selected");
		Response.Write(">" + dst.Tables["sales"].Rows[i]["name"].ToString() +  "</option>");
	}
}

bool DoCustomerSearchAndList()
{
	//string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
    string uri = Request.ServerVariables["URL"] + "?sk=1&search=1&ssid=" + m_ssid;
	int rows = 0;
	//string kw = "'%" + EncodeQuote(Request.Form["ckw"]) + "%'";
	/*string kw = "%" + EncodeQuote(Request.Form["ckw"]) + "%";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "%%";
	kw = kw.Replace("'", "");
	//kw = "'%%'";
	string sc = "SELECT *, '" + uri + "' + '&ci=' + LTRIM(STR(id)) AS uri FROM card ";
	sc += " WHERE type <> 3 AND type > 0 AND main_card_id IS NULL ";
	if(IsInteger(Request.Form["ckw"]))
		sc += " AND id=" + Request.Form["ckw"];
	else
	{
		sc += " AND (name LIKE '" + kw + "'  OR trading_name LIKE '" + kw + "' OR contact LIKE '" + kw + "' OR email LIKE '" + kw + "' OR company LIKE '" + kw + "' OR phone LIKE '" + kw + "') ";
	}
     */
    string kw =""; 
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
    {
		//kw = "%%";
        if(Request.QueryString["sk"] == "1" && Session["kw_record"] != null)
        {
            kw =  Session["kw_record"].ToString();
            kw_record = Session["kw_record"].ToString();
        }
    }
    else
    {
        kw = Request.Form["ckw"];
        Session["kw_record"] = kw;
        kw_record = kw;
    }
	//kw = EncodeQuote(kw.ToLower());
	string sc = "SELECT *, '" + uri + "' + '&ci=' + LTRIM(STR(id)) AS uri FROM card ";
	sc += " WHERE type <> 3 AND type > 0 ";
//	sc += " AND main_card_id IS NULL  ";
    if(kw != null && kw != "")
    {
	    if(IsInteger(Request.Form["ckw"]))
	    {
		    sc += " AND id=" + Request.Form["ckw"];
	    }
	    else
	    {
		    sc += " AND (name LIKE N'%" + kw + "%' OR email LIKE N'%" + kw + "%' OR trading_name LIKE N'%" + kw + "%' OR company LIKE N'%" + kw + "%') ";
		    sc += " AND type<>" + GetEnumID("card_type", "supplier");
	    }
    }
   if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
        sc += " AND our_branch = " + Session["branch_id"].ToString() + " ";
	sc += " ORDER BY company";
//DEBUG(" sc =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
		if(rows == 1)
		{
//DEBUG(" level =", dst.Tables["card"].Rows[0]["dealer_level"].ToString());
			Session[m_sCompanyName + "_dealer_level_for_pos" + m_ssid] = dst.Tables["card"].Rows[0]["dealer_level"].ToString();
			Session[m_sCompanyName + "_card_type_for_pos" + m_ssid] = dst.Tables["card"].Rows[0]["type"].ToString(); 

            m_customerID = dst.Tables["card"].Rows[0]["id"].ToString();
			ApplyPriceForCustomer();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
			Response.Write("URL=" + uri + "&ci=" + dst.Tables["card"].Rows[0]["id"].ToString() + "\">");
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

	Response.Write("<center><h3>Search for Customer</h3></center>");
	Response.Write("<form id=search action=" + uri + " method=post>");
	Response.Write("<input type=hidden name=invoice_number value=" + m_invoiceNumber + ">");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw value='"+ kw_record +"'></td><td>");
	Response.Write("<input type=submit name=cmd value=Search  "+ Session["button_style"] +" >");
	Response.Write("<input type=button name=cmd value='Cancel' ");
	Response.Write(" onClick=window.location=('pos_retail.aspx?ssid=" + m_ssid + "')  "+ Session["button_style"] +" >");
	Response.Write("<input type=button onclick=window.open('ecard.aspx?n=customer&a=new') value='New Customer'  "+ Session["button_style"] +" >");
	Response.Write("<input type=button onclick=window.location=('pos_retail.aspx?ci=0&ssid=" + m_ssid + "') value='Cash Sales'  "+ Session["button_style"] +" >");
	Response.Write("</td></tr></table></form>");

	LFooter.Text = m_sAdminFooter;
	return true;
}

bool CreateOrder(string branch_id, string card_id, string po_number, string special_shipto, string shipto, 
				 string shipping_method, string pickup_time, string contact, string sales_id, string sales_note, 
				 ref string order_number, bool bNoIndividualPrice)
{
	string reason = "";
	bool bStopOrdering = false;//IsStopOrdering(card_id, ref reason);
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
	sc += " INSERT INTO orders (number, card_id, po_number, freight) VALUES(0, " + card_id + ", '";
	sc += po_number + "', 0 ";
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
			sc = "UPDATE orders SET number=" + m_orderNumber + ", branch=" + branch_id + ", sales_note='" + sales_note + "' ";
			if(special_shipto == "1")
				sc += ", special_shipto=1, shipto='" + shipto + "' ";
			sc += ", contact='" + contact + "' ";
			if(sales_id != "")
				sc += ", sales=" + sales_id;
			sc += ", shipping_method=1";// + shipping_method;
			sc += ", pick_up_time='" + EncodeQuote(pickup_time) + "' ";
			sc += ", no_individual_price = " + nip;
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
    string branchId = dr["branch"].ToString();
	if(sales != "")
		sales = TSGetUserNameByID(sales);
	m_bNoIndividualPrice = MyBooleanParse(dr["no_individual_price"].ToString());
	string nip = "0";
	if(m_bNoIndividualPrice)
		nip = "1";

	dFreight = MyDoubleParse(dst.Tables["invoice"].Rows[0]["freight"].ToString());

	sc = "SELECT * FROM order_item WHERE id=" + id;
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
		dp = Math.Round(dp, 3);
		int qty = MyIntParse(dr["quantity"].ToString());
		dPrice += dp * qty;
		dPrice = Math.Round(dPrice, 3);
	}
	dTax = (dPrice + dFreight) * GetGstRate(card_id);
	dTax = Math.Round(dTax, 3);
	dTotal = dPrice + dFreight + dTax;

//	dTotal = Math.Round(dTotal,2);
	dTotal = MyCurrencyPrice(dTotal.ToString("c"));
	m_dInvoiceTotal = dTotal;
	
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

    DateTime Chkday = DateTime.Now;
  
 
	
    sc = "BEGIN TRANSACTION ";
	sc += " SET DATEFORMAT dmy ";
	sc += "INSERT INTO invoice (branch, type, card_id, price, tax, total, commit_date, special_shipto, shipto ";
	sc += ", freight, cust_ponumber, shipping_method, pick_up_time, sales, sales_note, no_individual_price)";
	sc += " VALUES(" + branchId + ", " + receipt_type + ", " + dr["card_id"].ToString() + ", " + dPrice;
	sc += ", " + dTax + ", " + dTotal; // + ", GETDATE(), ";
    if(Chkday.Day >= 25)
    {
        if(Chkday.Month == 12)
        {
            DateTime invDate = new DateTime(Chkday.Year+1, 1, 1);
            string invDateNew = invDate.ToString("dd/MM/yyyy");
            sc += ", '" + invDateNew + "', ";
        }
        else
        {
            DateTime invDate = new DateTime(Chkday.Year, Chkday.Month+1, 1);
            string invDateNew = invDate.ToString("dd/MM/yyyy");
            sc += ", '" + invDateNew + "', ";
        }
    
    }
    else
        sc += ", GETDATE(), ";

	sc += special_shipto + ", '" + EncodeQuote(dr["shipto"].ToString()) + "', " + dFreight + ", '" + po_number + "', ";
	sc += m_shippingMethod + ", '" + EncodeQuote(m_pickupTime) + "', '" + EncodeQuote(sales) + "', '";
	sc += EncodeQuote(dr["sales_note"].ToString()) + "' ";
	sc += ", " + nip;
	sc += " )";
	sc += " SELECT IDENT_CURRENT('invoice') AS id";
	sc += " COMMIT ";
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
	sc = "UPDATE orders SET invoice_number=" + m_invoiceNumber + ", status=3 WHERE id=" + id; //status 3 = shipped
	sc += " UPDATE invoice SET invoice_number = " + m_invoiceNumber + " WHERE id = " + m_invoiceNumber; //compatible with change invoice functionality
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
		string pack = dr["pack"].ToString();
		
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

		sc = "INSERT INTO sales (invoice_number, code, name, quantity, commit_price, supplier, supplier_code, supplier_price, system, kit, krid, pack)";
		sc += " VALUES(" + m_invoiceNumber + ", " + code + ", N'" + name + "', " + quantity + ", " + commit_price + ", ";
		sc += "'" + supplier + "', '" + supplier_code + "', " + supplier_price + ", " + sbSystem + ", " + sKit + ", " + krid + ", N'"+pack+"')";

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
		
		int nQty = MyIntParse(quantity);
		fifo_sales_update_cost(m_invoiceNumber, code, commit_price, m_branchID, nQty);
		//update stock qty
		fifo_updateStockQty(nQty, code, m_branchID, id);
		fifo_checkAC200Item(m_invoiceNumber, code, supplier_code, commit_price); //for unknow item
	}

	if(bHasKit)
	{
		if(!RecordKitToInvoice(id, m_invoiceNumber))
			return true;
	}

	UpdateCardAverage(card_id, dPrice, MyIntParse(DateTime.Now.ToString("MM")));
	UpdateCardBalance(card_id, dTotal);
	return true;
}

bool DoCreateOrder(bool bSystem, string sCustPONumber, string sSalesNote)
{
	string branch = Request.Form["branch"];
	string contact = "";
	return CreateOrder(branch, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), ref m_orderID, m_bNoIndividualPrice);
}

bool CheckBottomPrice()
{
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		string supplier_code = dtCart.Rows[i]["supplier_code"].ToString();
		if(supplier_code.ToLower() == "ac200") //temperory code for unknow product
			return true;
		if(dtCart.Rows[i]["kit"].ToString() == "1")
			continue; //ignore kit price

		string code = dtCart.Rows[i]["code"].ToString();
		double dPriceCheck = MyDoubleParse(dtCart.Rows[i]["salesPrice"].ToString());
		int iQty = MyIntParse(dtCart.Rows[i]["quantity"].ToString());

		DataRow drp = null;
		if(!GetProduct(code, ref drp))
		{
			Response.Write("<br><br><center><h3>Product not found");
			return false;
		}
		dPriceCheck = Math.Round(dPriceCheck, 3);
//		double dBottomPrice = Math.Round(MyDoubleParse(drp["price"].ToString()), 2);
		double dLastCostNZD = Math.Round(MyDoubleParse(drp["supplier_price"].ToString()), 3);
		double dManualCostNZD = Math.Round(MyDoubleParse(drp["manual_cost_nzd"].ToString()), 3);

		/*if(iQty > 0 && dPriceCheck < dLastCostNZD && dPriceCheck < dManualCostNZD)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Error, Under-Cost sales detected.</h3>");
			Response.Write("<br>Product Code : " + code);
			Response.Write("<br>Description : " + drp["name"].ToString());
			Response.Write("<br>Last Cost NZD : " + dLastCostNZD.ToString("c"));
			Response.Write("<br>Manual Cost NZD : " + dManualCostNZD.ToString("c"));
			Response.Write("<br>Sales Price : " + dPriceCheck.ToString("c"));
			Response.Write("<br><br><input type=button  "+ Session["button_style"] +"  value=Back onclick=window.location=('pos_retail.aspx?ssid=" + m_ssid + "')>");
			return false;
		}*/
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
	if(Request.Form["order_id"] == null || Request.Form["order_id"] == "")
		return true;

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
		sc += " UPDATE product SET allocated_stock = allocated_stock - " + sqty + " WHERE code=" + code;
		sc += " Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock - " + sqty;
		sc += " WHERE code=" + code + " AND branch_id = " + m_branchID;
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
		sc += ", commit_price, pack) VALUES(" + order_id + ", " + dr["code"].ToString() + ", ";
		sc += dr["quantity"].ToString() + ", N'" + name + "', '" + dr["supplier"].ToString();
		sc += "', '" + dr["supplier_code"].ToString() + "', " + Math.Round(MyDoubleParse(dr["supplierPrice"].ToString()), 3);
		sc += ", " + Math.Round(MyDoubleParse(dr["salesPrice"].ToString()), 3) + ",  N'"+ Request.Form["pack"+i].ToString()+"') ";

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
		if(Request.Form["pack"+i] != null || Request.Form["pack"+i] != "")
			Session["pack"+i] = null; // CH
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


bool PrintPaymentForm()
{
	m_invoiceNumber = Request.QueryString["i"];
	m_dInvoiceTotal = MyDoubleParse(Request.QueryString["total"].ToString());

//	if(m_dInvoiceTotal != "")
		m_dInvoiceTotal = Math.Round(m_dInvoiceTotal,2);
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><br><br>");
	Response.Write("<form name=f action=pos_retail.aspx?ssid=" + m_ssid + "&p=end method=post>");
	Response.Write("<input type=hidden name=invoice_number value=" + m_invoiceNumber + ">");
	Response.Write("<input type=hidden name=pm value=cash>");

	Response.Write("<table width=50% height=50% align=center valign=center border=1 cellspacing=0 cellpadding=0 bordercolor=#eeeeee bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=2 align=center><h2>Payment</h3></td></tr>");

	Response.Write("<tr><td colspan=2>");
//	Response.Write("<input type=text name=tptype value='Type' class=f style='font-size:30'>");
	Response.Write("<input type=text name=pmt value=Cash class=f style='font-weight:bold;font-size:30;'>");
//	Response.Write("<i>(press ENTER for EFTPOS)</i>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><input type=text name=ttotal value='Total' class=f style='font-size:30'></td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=text name=total size=3 value=" + m_dInvoiceTotal + " class=f style='text-align:right;font-weight:bold;font-size:30;'>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><input type=text name=tcashin value='Cash In' class=f style='font-size:30'></td>");
	Response.Write("<td align=right><input type=text name=cashin size=3 class=f style='text-align:right;font-weight:bold;font-size:30'>");
//	Response.Write("<input type=submit value=GO  "+ Session["button_style"] +"  onclick=\"oncashin();return false;\"></td></tr>");

	Response.Write("<tr><td><input type=text name=tcashchange value='Changes' class=f style='font-size:30'></td>");
	Response.Write("<td align=right><input type=text name=cashchange size=3 class=f style='text-align:right;font-weight:bold;font-size:30;'></td></tr>");
	Response.Write("<tr><td><input type=button value='Pay Later' ");
	Response.Write(" onclick=window.location=('pos_retail.aspx?ssid=" + m_ssid + "&p=end&paylater=1&i=" + m_invoiceNumber + "')  "+ Session["button_style"] +" ></td>");
	Response.Write("<td align=right><input type=submit name=cmd value=OK  "+ Session["button_style"] +"  onclick=\"if(!paymentok()){return false;}\">");
	Response.Write("</table>");
	Response.Write("</form>");

	PrintPaymentJava();

	return true;
}

void PrintPaymentJava()
{
	string s = "<script";
	s += ">\r\n";
	s += @"

	document.f.cashin.focus();

	function paymentok()
	{
		if(document.f.cashin.value == '.' && document.f.pm.value != 'cash')
		{
			if(document.f.pm.value == 'cheque')
				showmasterform();
			else if(document.f.pm.value == 'master')
				showvisaform();
			else if(document.f.pm.value == 'visa')
				showeftposform();
			else
				showcashform();
			document.f.cashin.value = '';
			return false;
		}

		if(document.f.pm.value == 'cash')
		{
			if(document.f.cashin.value == '')
			{
				showeftposform();
				return false;
			}
			else if(document.f.cashchange.value != '')
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
			else
			{
				var cg = Number(document.f.cashin.value) - Number(document.f.total.value);
				document.f.cashchange.value = Math.round(cg * 100) / 100;
				return false;
			}
		}
		else if(document.f.pm.value == 'eftpos')
		{
			if(document.f.cashin.value == '')
			{
				showvisaform();
				return false;
			}
			else if(document.f.cashin.value == '0')
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
		}
		else if(document.f.pm.value == 'visa')
		{
			if(document.f.cashin.value == '')
			{
				showmasterform();
				return false;
			}
			else if(document.f.cashin.value == '0')
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
		}
		if(document.f.pm.value == 'master')
		{
			if(document.f.cashin.value == '')
			{
				showchequeform();
				return false;
			}
			else if(document.f.cashin.value == '0')
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
		}
		else if(document.f.pm.value == 'cheque')
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
		return false;
	}

	function showeftposform()
	{
		document.f.pm.value = 'eftpos';
		document.f.pmt.value='EFTPOS';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.focus();
	}
	function showvisaform()
	{
		document.f.pm.value = 'visa';
		document.f.pmt.value='VISA CARD';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.focus();
	}
	function showmasterform()
	{
		document.f.pm.value = 'master';
		document.f.pmt.value='MASTER CARD';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.focus();
	}
	function showchequeform()
	{
		document.f.pm.value = 'cheque';
		document.f.pmt.value='CHEQUE';
		document.f.tcashin.value = '';
		document.f.tcashchange.value = '';
		document.f.cashin.focus();
	}

	function showcashform()
	{
		document.f.pm.value = 'cash';
		document.f.pmt.value='CASH';
		document.f.tcashin.value = 'Cash In';
		document.f.tcashchange.value = 'Changes';
		document.f.cashin.value = '';
		document.f.cashchange.value = '';
		document.f.cashin.focus();
	}
	";

	s += "</script";
	s += ">";

	Response.Write(s);
}
/*
bool DoReceiveOnePayment(string pm, double dAmount)
{
	m_invoiceNumber = Request.Form["invoice_number"];
	string sReceiptBody = BuildReceiptBody(); //get m_dInvoiceTotal first before record payment

	//RecordTransaction()
	string pm = Request.Form["pm"].ToString();
	if(pm == "visa" || pm == "master")
		pm = "credit card";
	string payment_method = GetEnumID("payment_method", pm);
	string sAmount = m_dInvoiceTotal.ToString();

	//do transaction
	SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;

	myCommand.Parameters.Add("@Amount", SqlDbType.Money).Value = sAmount;
	myCommand.Parameters.Add("@paid_by", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@bank", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@branch", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@nDest", SqlDbType.Int).Value = "1116";
	myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = sAmount;
	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = m_cardID;//got this from BuildReceiptBody
	myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = payment_method;
	myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = m_invoiceNumber + ",";
	myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@finance", SqlDbType.Money).Value = "0";
	myCommand.Parameters.Add("@credit", SqlDbType.Money).Value = "0";
	myCommand.Parameters.Add("@bRefund", SqlDbType.Bit).Value = 0;
	myCommand.Parameters.Add("@amountList", SqlDbType.VarChar).Value = sAmount + ",";
	myCommand.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoCustomerPayment", e);
		return false;
	}
	string m_tranid = myCommand.Parameters["@return_tran_id"].Value.ToString();

	return true;
}
*/
string BuildReceiptBody()
{
	int rows = 0;
	string sc = " SELECT i.total, i.card_id, s.code, s.name, s.quantity, s.commit_price * 1.125 AS price ";
	sc += " FROM invoice i JOIN sales s ON s.invoice_number=i.invoice_number ";
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
	m_dInvoiceTotal = Math.Round(MyDoubleParse(dst.Tables["receipt"].Rows[0]["total"].ToString()), 3);
	m_cardID = dst.Tables["receipt"].Rows[0]["card_id"].ToString();
	if(m_cardID == "")
		m_cardID = "0";

	string stotal = m_dInvoiceTotal.ToString("c");
	string s = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["receipt"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["quantity"].ToString();
		string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 3).ToString("c");

		s += name + "\r\n";

		string slcode = "       " + code;
		s += slcode;
		len = 20 - slcode.Length;
		for(n=0; n<len; n++)
			s += ' ';
		len = qty.Length + 1 + price.Length;
		len = 22 - len;
		s += "x" + qty;
		for(n=0; n<len; n++)
			s += ' ';
		s += price + "\r\n";

//		s += "      " + code + "          x" + qty + "      " + price + "\r\n";
	}

	string pm = Request.Form["pm"].ToString().ToUpper(); //payment method
	
	string si = rows + " Items        TOTAL";
	s += si;
	si += stotal;
	len = 42 - si.Length;
	for(n=0; n<len; n++)
		s += ' ';
	s += stotal + "\r\n";

//	s += rows + " Items       Total " + "           " + stotal + "\r\n";
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
		s += cashin + "\r\n";
		for(n=0; n<14; n++)
			s += ' ';
		s += "CHANGE";
		len = 22 - cashchange.Length;
		for(n=0; n<len; n++)
			s += ' ';
		s += cashchange + "\r\n";
	}
	else
	{
		len = 22 - stotal.Length;
		for(n=0; n<len; n++)
			s += ' ';
		s += stotal + "\r\n";
	}

	s += "\r\n";
	return s;
}
bool DoReceivePayment()
{
	///if(Request.Form["invoice_number"] != null)
	//	m_invoiceNumber = Request.Form["invoice_number"];
	//IE refresh protection, fix double pay record
	if(Session["qpos_last_invoice_paid"] != null)
	{
		if(Session["qpos_last_invoice_paid"].ToString() == m_invoiceNumber)
			return true;
	}

	bool bMultiPayments = false;
	double m_dCash = MyMoneyParseNoWarning(Request.Form["cash"]);
	double dEftpos = MyMoneyParseNoWarning(Request.Form["eftpos"]);
	double dCreditCard = MyMoneyParseNoWarning(Request.Form["cc"]);
	double dBankcard =0 ;//MyMoneyParseNoWarning(Request.Form["payment_bankcard"]);
	double dCheque = MyMoneyParseNoWarning(Request.Form["cheque"]);

	if(!DoReceiveOnePayment("cash", m_dCash))
		return false;
	if(!DoReceiveOnePayment("eftpos", dEftpos))
		return false;
	//if(!DoReceiveOnePayment("bank card", dBankcard))
	//	return false;
	if(!DoReceiveOnePayment("cheque", dCheque))
		return false;
	if(!DoReceiveOnePayment("credit card", dCreditCard))
		return false;

	Session["qpos_last_invoice_paid"] = m_invoiceNumber;
	
	string sc = "UPDATE invoice SET paid ='1' WHERE invoice_number='"+m_invoiceNumber+"'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
	    myAdapter.Fill(dst, "payment");
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
	if(dAmount == 0)
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
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = "Quick Sales Order";
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
		AlertAdmin("DoCustomerPayment, e = " + e.ToString());
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
} // method  ApplyPriceFo
</script>

<asp:Label id=LFooter runat=server/>


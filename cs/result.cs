<!-- #include file="kit_fun.cs" -->
<!-- #include file="credit_limit.cs" -->
<!-- #include file="card_function.cs" -->
<!-- #include file="resolver.cs" -->

<script runat=server>

DataSet ds = new DataSet();

string m_orderNumber = "";
string p_total ="";
//for retail version
string sPaymentType = "cheque";
string sDiscountInfo = "";
bool m_bIncreaseDiscount = false;
bool m_bPaid = false;
bool m_bCreditTermsOK = true;
int m_nOverdueDays = 0;
string err = "";

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	CheckShoppingCart();
	CheckUserTable();


	if(g_bRetailVersion && m_sSite == "www")
	{
		
        if(!ValidateUserDetails())
	    {
		    PrintHeaderAndMenu();
			PrintBodyHeader();
		    string sc = "<br><center><h3>";
		    sc += err;
		    sc += "</h3><br><br><input type=button onclick=history.go(-1) value=Back class=b></center>\r\n";
		    Response.Write(sc);
			PrintBodyFooter();
            PrintFooter();
		    return;
	    }
        PrintHeaderAndMenu();
		PrintBodyHeader();
        if(!CreateOrder())
		    return;
        if(Session["OrderNumber"] == null || Session["OrderNumber"] == "")
		{    
		   string U_acc = @"
		     <table width=200 cellpadding=0 cellspacing=0 border=0 class=confirm_t>
					     <tr>
						  <td align=right><a href=status.aspx? ><img src=pic/trace.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=status.aspx? >Order Status</a></td>
						  </tr>
						    <tr>
						  <td align=right><a href=register.aspx ><img src=pic/update.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=register.aspx >Update Details</a></td>
						  </tr>
						   <tr>
						  <td align=right><a href=setpwd.aspx ><img src=pic/password.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=setpwd.aspx >Change Password</a></td>
						  </tr>
						 
						    <tr>
						  <td align=right><a href=login.aspx?logoff=true ><img src=pic/poweroff.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=login.aspx?logoff=true >Logout</a></td>
						  </tr>
						  </table>";
			Response.Write("<table width=100% height=300 cellspacing cellpadding=0 border=0>");
			Response.Write("<tr><td valign=top bgcolor=#F2F2F2 width=200>"+U_acc+"</td><td>");
			Response.Write( "<br><center><h3>Error. No transaction found</h3> Please follow the links. \r\n");
			Response.Write("<a href=c.aspx> Back to Main Page </a><br>\r\n ");
			Response.Write("<br>Contact " + m_sCompanyTitle + " if you have any question.</center><br><br>");
			Response.Write( "</td></tr></table>");
		//	Response.Write(s);
			PrintBodyFooter();
			PrintFooter();
			return;
		}

		m_orderNumber = Session["OrderNumber"].ToString();
        p_total = Session["Amount"].ToString();
		string Oplace = "";
		 Oplace = Oplace.Replace("@@OrderNumber", m_orderNumber);
		 Oplace = Oplace.Replace("@@Total", p_total);
		
		if(Request.QueryString["t"] == "cc")
		
		{
			if(!Retail_IsPaymentOK())
			{
				PrintFooter();
				return;
			}
		}

		if(Retail_PlaceOrder())
			Retail_CleanUp();
		
	/*	Response.Write("<br><table align=center width=75%>\r\n");
		Response.Write("<tr><td><h3>Order Placed!</h3></td></tr>");
		Response.Write("<tr><td>Your order is being processed, order has been sent to you via email.</td></tr>\r\n");
		Response.Write("<tr><td>You will receive another email once your order has been shipped.</td></tr>\r\n");
		Response.Write("<tr><td>&nbsp;</td></tr>\r\n");

		Response.Write("<tr><td>");
		if(!g_bOrderOnlyVersion)
			Response.Write("<a href=status.aspx?t=1&r=" + DateTime.Now.ToOADate() + " class=o>view order status</a> &nbsp;&nbsp;\r\n");
		Response.Write("<a href=default.aspx class=o>Home</a></td></tr>\r\n");
		Response.Write("</table><br><br><hr width=75%>\r\n");
		Response.Write("<br><br><br><br><br><br><br><br><br><br><br><br><br><br>");*/
        Session["order_ref"] = m_orderNumber;
        string online_payment ="<form id=form1 method=post action='dpspayment.aspx'>";
		online_payment += "	<input type=hidden name=txtAmountInput  value='"+p_total+"'  />";
		online_payment += "	<input type=hidden name=txtCurrencyInput />";
		online_payment += "	<input type=hidden name=txtMerchantReference value ='"+m_orderNumber+"'/>";
		online_payment += "	<input type=hidden name=txtEmailAddress value='"+Session["Email"].ToString()+"'  />";
		online_payment += "	<input type=hidden name=ddlTxnType  value=Purchase  />";
		online_payment += "	<input  type=submit  value=' ' name=cmd  style='background-image:url(images/creditcard.jpg); border:none; width:100px; height:100px;'/>";
		online_payment += "	</form>";
		
		string U_account =@"
		                  <table width=200 cellpadding=0 cellspacing=0 border=0 class=confirm_t>
						  <tr>
						   <td height=25 bgcolor=#6699CC style='font:bold 13px arial; color:#ffffff; text-align:center' colspan=3>My Account</td>
						   </tr>
					     <tr>
						  <td align=right><a href=status.aspx? ><img src=pic/trace.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=status.aspx? >Order Status</a></td>
						  </tr>
						    <tr>
						  <td align=right><a href=register.aspx ><img src=pic/update.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=register.aspx >Update Details</a></td>
						  </tr>
						   <tr>
						  <td align=right><a href=setpwd.aspx ><img src=pic/password.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=setpwd.aspx >Change Password</a></td>
						  </tr>
						 
						    <tr>
						  <td align=right><a href=login.aspx?logoff=true ><img ssrc=pic/poweroff.png border=0 width=30 height=30></a></td><td width=10></td><td align=left><a href=login.aspx?logoff=true >Logout</a></td>
						  </tr>
						  </table>";
			
	    Response.Write("<table width=100% cellpadding=0 cellspacing=0 border=0>");
	    Response.Write("<tr><td valign=top bgcolor=#F2F2F2 >" +U_account+"</td><td width=10></td><td><br>");
	    Response.Write("<fieldset><legend style=\"font:bold 18px Arial, Helvetica, sans-serif; color:#6699CC\">Congraduations Order Placed</legend><br>");
		Response.Write("<table width=95% cellpadding=0 cellspacing=0 border=0>");
		Response.Write("<tr><td width=150><b>Order Number:</b></td><td align=left ><b>"+m_orderNumber+"</b></td>");
		Response.Write("<td width=150><b>Total Order Amount:</b></td><td align=left><b>$"+p_total+"</b></td></tr>");
		Response.Write("</table><Br>");
        string showlayout = ReadSitePage("result_page");
        showlayout = showlayout.Replace("@@online_payment", online_payment);
		Response.Write(showlayout);
		Response.Write("</td></tr></table><bR>");
		PrintBodyFooter();
		PrintFooter();
		return;
	}

	//wholesale version
	if(Request.Form["note"] == null)
	{
		PrintHeaderAndMenu();
		Response.Write("<center><h3><br><br><br><br><br><br>Invalid Form Data<br><br></h3>");
		Response.Write("<a href=c.aspx class=\"btn btn-primary\">Continue Shopping</a></center><br><br><br><br><br>\r\n");
		PrintFooter();
		return;
	}

	PrintHeaderAndMenu();
	PrintBodyHeader();
	if(IsCartEmpty())
	{
		Response.Write("<center><h3><br><br><br><br><br><br>Your shopping cart is empty, cannot place order. <br><br></h3>");
		Response.Write("<a href=c.aspx class=\"btn btn-primary\">Continue Shopping</a></center><br><br><br><br><br>\r\n");
	}
	else
		PlaceOrder();
	PrintBodyFooter();
	PrintFooter();
}

Boolean PlaceOrder()
{
	string term_msg = "";
	int nOverdues = 0;
	double dOverdueAmount = 0;
	if(m_sSite == "dealer")
		m_bCreditTermsOK = CreditTermsOK(Session["card_id"].ToString(), ref nOverdues, ref dOverdueAmount, ref m_nOverdueDays, ref term_msg);

	if(!CreateOrder())		//create new order, add to sales history, update products stock number...
		return false;
//	if(!SendInvoice())		//use customer email
//		return false;
	if(!ArrangeDelivery())	//send email to our sales
		return false;

	if(m_bCreditTermsOK)
	{
		if(m_sSite == "dealer")
		{
	 		Response.Write(ReadSitePage("DEALER_ORDER_CREDIT_OK"));
		}
		else
		{
			Response.Write(ReadSitePage("result_page"));
		}
	}
	else
		Response.Write(term_msg);
	EmptyCart();
	return true;
}

string GetSalesManager(string card_id)
{
	if(ds.Tables["salesmanager"] != null)
		ds.Tables["salesmanager"].Clear();

	string sc = " SELECT sales FROM card WHERE id = " + card_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "salesmanager") <= 0)
			return "null";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "null";
	}
	
	string output = ds.Tables["salesmanager"].Rows[0]["sales"].ToString();
	if(output == "")
		output = "null";
	return output;
}

bool CreateOrder()
{
	if(Session["card_id"] == null)
		return false;
	if(Session["card_id"].ToString() == "")
		return false;

	string pickup_time = EncodeQuote(Request.Form["pickup_time"]);
	string sales_note = EncodeQuote(Request.Form["note"]);
	if(pickup_time != null && pickup_time.Length > 49)
		pickup_time = pickup_time.Substring(0, 49);

	//vin add
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, card_id, po_number) VALUES(0, " + Session["card_id"].ToString() + ", '";
	sc += EncodeQuote(Request.Form["po_number"]) + "' "; //, " + GetSalesManager(Session["card_id"].ToString());
	sc += ") SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";

	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "id") == 1)
		{
			m_orderNumber = ds.Tables["id"].Rows[0]["id"].ToString();
			//assign ordernumber same as id
            if(m_sSite == "www")
            {
                Session["OrderNumber"] = m_orderNumber;
			    sc = "UPDATE orders SET number=" + m_orderNumber;
			    sc += ", shipping_method=4";// 4: 2 day courier + Request.Form["shipping_method"];
			    sc += ", freight=" + Session["ShippingFee"].ToString();
			    sc += ", no_individual_price = 0 ";
                if(Session[m_sCompanyName +"gst_rate"] != null && Session[m_sCompanyName +"gst_rate"] != "")
				    sc += ", customer_gst = '"+ Session[m_sCompanyName +"gst_rate"].ToString() +"' ";
			    sc += ", sales_note='Online Retail Order' ";
			    sc += " WHERE id=" + m_orderNumber;
            }
            else
            {
			    sc = "UPDATE orders SET number=" + m_orderNumber;
			    sc += ", shipping_method=" + Request.Form["shipping_method"];
			    sc += ", pick_up_time='" + pickup_time + "'";
			    if(Request.Form["special_shipto"] == "1" || (bool)Session["login_is_branch"] )
				    sc += ", special_shipto=1, shipto='" + EncodeQuote(Request.Form["ssta"]) + "' ";
			    if(Session[m_sCompanyName +"gst_rate"] != null && Session[m_sCompanyName +"gst_rate"] != "")
				    sc += ", customer_gst = '"+ Session[m_sCompanyName +"gst_rate"].ToString() +"' ";			
			    sc += ", sales_manager = (SELECT ISNULL(sales,'0') FROM card WHERE id ='"+ Session["card_id"].ToString() +"' AND sales IS NOT NULL) ";
			    sc += ", sales = (SELECT ISNULL(sales,'0') FROM card WHERE id ='"+ Session["card_id"].ToString() +"' AND sales IS NOT NULL) ";
			    sc += ", contact='" + EncodeQuote(Request.Form["contact"]) + "' ";
			    sc += ", no_individual_price = 0 ";
			    sc += ", sales_note='" + sales_note + "' ";
			    if(!m_bCreditTermsOK && m_nOverdueDays > 3)
				    sc += ", status=5 "; //put on hold
			    sc += " WHERE id=" + m_orderNumber;
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
	
    if(!RecordOrderItems())
		return false;
	
    return true;
}

bool RecordOrderItems()
{
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		string kit = dr["kit"].ToString();
		double dPrice = MyDoubleParse(dr["salesPrice"].ToString());
		dPrice = Math.Round(dPrice, 2);
		string name = EncodeQuote(dr["name"].ToString());
		if(name.Length > 255)
			name = name.Substring(0, 255);

		if(kit == "1")
		{
			RecordKitToOrder(m_orderNumber, dr["code"].ToString(), name, dr["quantity"].ToString(), dPrice, "1");
			continue;
		}

		string sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
		sc += ", commit_price) VALUES(" + m_orderNumber + ", " + dr["code"].ToString() + ", ";
		sc += dr["quantity"].ToString() + ", '" + name + "', '" + dr["supplier"].ToString();
		sc += "', '" + dr["supplier_code"].ToString() + "', " + Math.Round(MyDoubleParse(dr["supplierPrice"].ToString()), 2);
		sc += ", " + dPrice;
		sc += ") ";

		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + dr["code"].ToString();
		sc += " AND branch_id = 1 "; //use main branch for online sales
		sc += ")";
		sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
		sc += " VALUES (" + dr["code"].ToString() + ", 1, 0, " + dr["quantity"].ToString() + ")"; 
		sc += " ELSE Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock + " + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString() + " AND branch_id = 1 ";
		
        sc += " UPDATE product SET allocated_stock = allocated_stock + " + dr["quantity"].ToString();
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
	}
	return true;
}

Boolean SendInvoice()
{
	string subject = "";
	subject = "Order Information No.";

	subject += m_orderNumber;

	MailMessage msgMail = new MailMessage();

	msgMail.To = Session["Email"].ToString();

	msgMail.From = m_sSalesEmail;
	msgMail.Subject = subject;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = BuildOrderMail(m_orderNumber);

	SmtpMail.Send(msgMail);
//Response.Write(msgMail.Body);
	return true;
}

Boolean ArrangeDelivery()
{
	string subject = "New Order - ";
	subject += m_orderNumber;

	StringBuilder sb = new StringBuilder();
	sb.Append("<div align=right>Order received at ");
	DateTime dNow = DateTime.Now;
	sb.Append(dNow.ToString());
	sb.Append(". ");
	sb.Append("</div>\r\n");

	sb.Append("<table width=100%><tr><td height=1 bgcolor=red>&nbsp;</td></tr></table><br>\r\n");
	sb.Append("Shipping Method: " + GetEnumValue("shipping_method", Request.Form["shipping_method"].ToString()) + "<br><br>");
	sb.Append(PrintBillingDetails());	
	sb.Append(PrintCart(false, true));
	sb.Append("Note: " + EncodeQuote(Request.Form["note"]));

	MailMessage msgMail = new MailMessage();

	msgMail.To = m_sSalesEmail;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = subject;

	msgMail.BodyFormat = MailFormat.Html;
	string strBody = "<html><style type=\"text/css\">\r\n";
	strBody += "td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n";
	strBody += "body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n<head><h2>Order ";
	strBody += m_orderNumber.ToString();
	strBody += "</h2></head>\r\n<body>\r\n";
	strBody += sb.ToString();
	strBody += "</body></html>";
	msgMail.Body = strBody;

	SmtpMail.Send(msgMail);
	return true;
}

string PrintBillingDetails()
{
	CheckUserTable();

	DataRow dr = dtUser.Rows[0];
	
	StringBuilder sb = new StringBuilder();

	sb.Append("<table align=center width=100%>\r\n");
	sb.Append("<tr><td colspan=2>&nbsp;</td></tr>\r\n");
	sb.Append("<tr><td>");

	sb.Append("<table><tr><td colspan=2><h3>Shipping Address</h3></td></tr>\r\n");
	sb.Append("<tr><td>");
	if(Request.Form["special_shipto"] == "1" || (bool)Session["login_is_branch"] )
	{
		sb.Append(Request.Form["ssta"].ToString().Replace("\r\n", "<br>"));
	}
	else
	{
		sb.Append(dr["trading_name"].ToString() + "<br>");
		sb.Append(dr["Address1"].ToString() + "<br>");
		sb.Append("</td></tr>\r\n<tr><td>&nbsp;</td><td>");
		sb.Append(dr["Address2"].ToString() + "<br>");
		sb.Append("</td></tr>\r\n<tr><td>&nbsp;</td><td>");
		sb.Append(dr["address3"].ToString() + "<br>");
		sb.Append("</td></tr>\r\n<tr><td>Phone</td><td>");
		sb.Append(dr["Phone"].ToString() + "<br>");
		sb.Append("</td></tr>\r\n<tr><td>Email</td><td>");
		sb.Append("<a href=mailto:");
		sb.Append(dr["Email"].ToString());
		sb.Append(">");
		sb.Append(dr["Email"].ToString());
		sb.Append("</a><br>");
	}
	sb.Append("</td></tr>\r\n</table>\r\n");

	sb.Append("</td>\r\n\r\n<td valign=top>\r\n<table>\r\n");
	
	sb.Append("<tr><td colspan=2><h3>Billing Address</h3></td></tr>\r\n");
	sb.Append("<tr><td>");
	sb.Append(dr["trading_name"].ToString() + "<br>");
	if(dr["postal1"].ToString() != "")
	{
		sb.Append(dr["postal1"].ToString() + "<br>");
		sb.Append(dr["postal2"].ToString() + "<br>");
		sb.Append(dr["postal3"].ToString() + "<br>");
	}
	else
	{
		sb.Append(dr["address1"].ToString() + "<br>");
		sb.Append(dr["address2"].ToString() + "<br>");
		sb.Append(dr["address3"].ToString() + "<br>");
	}

	sb.Append("</td></tr>\r\n</table>\r\n</td></tr></table>\r\n\r\n");

	return sb.ToString();
}

/////////////////////////////////////////////////////////////////////////////////////////
//retail functions
void Retail_CleanUp()
{
	Session["OrderNumber"] = null;
	Session["OrderCreated"] = null;
	
	//clear cart, only items for this site
	for(int i=dtCart.Rows.Count-1; i>=0; i--)
	{
		if(dtCart.Rows[i]["site"].ToString() == m_sCompanyName)
		{
			dtCart.Rows.RemoveAt(i);
		}
	}
}

Boolean Retail_PlaceOrder()
{
	if(!Retail_SendOrderInfoMail())		//use customer email
		return false;
	if(!Retail_ArrangeDelivery())	//send email to our sales
		return false;
	return true;
}

bool Retail_UpdateUserStatus()
{
	if(!TS_UserLoggedIn())
		return true; //not logged in, don't do update DW. 15. Aug. 2002

	double dAmount = double.Parse(Session["Amount"].ToString());
	string sDiscount = "";
	double dDiscount = 0;
	double dDiscountUp =0;
	if(Session[m_sCompanyName + "discount"] != null)
	{
		sDiscount = Session[m_sCompanyName + "discount"].ToString();
		dDiscount = double.Parse(sDiscount);
	}
	dDiscountUp = dAmount/100;

	if ((dDiscount + dDiscountUp) > 100)
		 dDiscountUp = 100 - dDiscount;
	dDiscount += dDiscountUp;

	if (dDiscountUp >= 1)
		sDiscountInfo = "You get " + ((int)dDiscountUp).ToString() + " credits by this purchase!";

//	DataRow dr = dtUser.Rows[0];
	UpdateDiscount(Session["card_id"].ToString(), dDiscount);
	Session[m_sCompanyName + "discount"] = dDiscount.ToString();

	return true;
}

bool Retail_IsPaymentOK()
{
	m_orderNumber = Request.QueryString["oid"];

	DataSet ds = new DataSet();
	int rows = 0;
	string sc = "SELECT paid, payment_type, trans_failed_reason FROM orders WHERE id=" + m_orderNumber;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "orders");
		if(rows == 1)
		{
			string pm = ds.Tables["orders"].Rows[0]["payment_type"].ToString();
			if(pm != "")
				sPaymentType = GetEnumValue("payment_method", pm);
			m_bPaid = (bool)ds.Tables["orders"].Rows[0]["paid"];
			if(m_bPaid == false)
			{
				string s = "<center><h3>Transcation failed</h3></center><br>";
				s += "<table align=center><tr><td>Server Response: ";
				if(ds.Tables[0].Rows[0]["trans_failed_reason"] != null)
					s += ds.Tables[0].Rows[0]["trans_failed_reason"].ToString();
				else 
					s += "UNKNOWN";
				s += "</td></tr><tr><td>&nbsp;</td></tr><tr><td>\r\n";
				s += "Contact " + m_sCompanyTitle + " if you have any question</td></tr>";
				s += "</td></tr><tr><td>&nbsp;</td></tr><tr><td>\r\n";
				s += "<a href=checkout.aspx> Click here to try again </a></td></tr>\r\n ";
				s += "</table>";
				Response.Write(s);
				return false;
			}
			else
			{
				Retail_UpdateUserStatus();
				return true;
			}
		}
		else
		{
			Response.Write("No transaction found");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

Boolean Retail_SendOrderInfoMail()
{
	string subject = "";
//	if(sPaymentType == "credit card")
//		subject = "Tax Invoice No.";
//	else
		subject = "Order Information No.";

	subject += m_orderNumber;

	MailMessage msgMail = new MailMessage();

	DataRow dr = dtUser.Rows[0];
	
	msgMail.To = dr["Email"].ToString();

	msgMail.From = m_sSalesEmail;
	msgMail.Subject = subject;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = BuildOrderMail(m_orderNumber);

//	SmtpMail.Send(msgMail);
	return true;
}

Boolean Retail_ArrangeDelivery()
{
	//debug , don't borther jerry
	DataRow dr = dtUser.Rows[0];
	string e = dr["Email"].ToString();
	if(e.Length > 9)
	{
		string company = e.Substring(e.Length - 9, 9);
		if(company == "@eznz.com")
			return true;
	}

	string subject = "New Order - ";
	subject += m_orderNumber;

	StringBuilder sb = new StringBuilder();
	sb.Append("<div align=right>Order received at ");
	DateTime dNow = DateTime.Now;
	sb.Append(dNow.ToString());
	sb.Append(". ");
	if(sPaymentType == "credit card")
	{
		sb.Append("Payment confirmed.");
	}
	else
	{
		sb.Append("<font size=+2 color=red><b>Waiting for ");
		sb.Append(sPaymentType);
		sb.Append(" DO NOT ARRANGE DELIVERY !</b></font>");
	}
	sb.Append("</div>\r\n");

	sb.Append("<table width=100%><tr><td height=1 bgcolor=red>&nbsp;</td></tr></table><br>\r\n");
	sb.Append(Retail_PrintBillingDetails());	
	sb.Append(PrintCart(false, true));

	MailMessage msgMail = new MailMessage();

	msgMail.To = m_sSalesEmail;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = subject;

	msgMail.BodyFormat = MailFormat.Html;
	string strBody = "<html><style type=\"text/css\">\r\n";
	strBody += "td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n";
	strBody += "body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n<head><h2>Order ";
	strBody += m_orderNumber;
	strBody += "</h2></head>\r\n<body>\r\n";
	strBody += sb.ToString();
	strBody += "</body></html>";
	msgMail.Body = strBody;

	SmtpMail.Send(msgMail);
	return true;
}


string Retail_PrintBillingDetails()
{
	CheckUserTable();

	DataRow dr = dtUser.Rows[0];
	
	StringBuilder sb = new StringBuilder();

	sb.Append("<table align=center width=100%>\r\n");
	sb.Append("<tr><td colspan=2>&nbsp;</td></tr>\r\n");
	sb.Append("<tr><td><table><tr><td colspan=2><h3>Shipping Address</h3></td></tr>\r\n");

	sb.Append("<tr><td>Name</td><td>");
	sb.Append(dr["Name"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Company</td><td>");
	sb.Append(dr["Company"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Address</td><td>");
	sb.Append(dr["Address1"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>&nbsp;</td><td>");
	sb.Append(dr["Address2"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>City</td><td>");
	sb.Append(dr["City"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Phone</td><td>");
	sb.Append(dr["Phone"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Email</td><td>");
	sb.Append("<a href=mailto:");
	sb.Append(dr["Email"].ToString());
	sb.Append(">");
	sb.Append(dr["Email"].ToString());
	sb.Append("</a></td></tr>\r\n</table>\r\n");

	sb.Append("</td>\r\n\r\n<td valign=top>\r\n<table>\r\n");
	
	sb.Append("<tr><td colspan=2><h3>Billing Address</h3></td></tr>\r\n<tr><td>Name</td><td>");

	if(dr["NameB"].ToString() != "")
		sb.Append(dr["NameB"].ToString());
	else
		sb.Append(dr["Name"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>Company</td><td>");
	if(dr["CompanyB"].ToString() != "")
		sb.Append(dr["CompanyB"].ToString());
	else
		sb.Append(dr["Company"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>Address</td><td>");
	if(dr["Address1B"].ToString() != "")
		sb.Append(dr["Address1B"].ToString());
	else
		sb.Append(dr["Address1"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>&nbsp;</td><td>");
	if(dr["Address2B"].ToString() != "")
		sb.Append(dr["Address2B"].ToString());
	else
		sb.Append(dr["Address2"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>City</td><td>");
	if(dr["CityB"].ToString() != "")
		sb.Append(dr["CityB"].ToString());
	else
		sb.Append(dr["City"].ToString());

	sb.Append("</td></tr>\r\n</table>\r\n</td></tr></table>\r\n\r\n");

	return sb.ToString();
}

Boolean ValidateUserDetails()
{
	if(dtUser.Rows.Count <= 0)
	{
//DEBUG("error, user table empty", "");
		err = "internal error";
		return false;
	}

	string email = Request.Form["email"];
	string email_confirm = Request.Form["email_confirm"];

	string name = EncodeQuote(Request.Form["Name"]);
	string company = EncodeQuote(Request.Form["Company"]);
	string address1 = EncodeQuote(Request.Form["Address1"]);
	string address2 = EncodeQuote(Request.Form["Address2"]);
	string address3 = EncodeQuote(Request.Form["City"]);
	string city = EncodeQuote(Request.Form["City"]);
	string country = EncodeQuote(Request.Form["Country"]);
	string phone = EncodeQuote(Request.Form["Phone"]);

	string nameB = EncodeQuote(Request.Form["NameB"]);
	string companyB = EncodeQuote(Request.Form["CompanyB"]);
	string address1B = EncodeQuote(Request.Form["Address1B"]);
	string address2B = EncodeQuote(Request.Form["Address2B"]);
	string cityB = EncodeQuote(Request.Form["CityB"]);
	string countryB = EncodeQuote(Request.Form["CountryB"]);

	Trim(ref email);
	Trim(ref email_confirm);
	Trim(ref name);
	Trim(ref company);
	Trim(ref address1);
	Trim(ref address2);
	Trim(ref address3);
	Trim(ref city);
	Trim(ref country);
	Trim(ref phone);
	Trim(ref nameB);
	Trim(ref companyB);
	Trim(ref address1B);
	Trim(ref address2B);
	Trim(ref cityB);
	Trim(ref countryB);

	bool bChanged = false;
	string nd = ""; //new data, used to compare to old data to detect changes
	nd += name;
	nd += company;
	nd += address1;
	nd += address2;
	nd += city;
	nd += country;
	nd += phone;

	nd += nameB;
	nd += companyB;
	nd += address1B;
	nd += address2B;
	nd += cityB;
	nd += countryB;
//DEBUG("od1",nd);
//DEBUG("od1",Request.Form["old_data"].ToString());
	if(nd != Request.Form["old_data"])
		bChanged = true;

	DataRow dr = dtUser.Rows[0];
	
	dtUser.AcceptChanges();
	dr.BeginEdit();

	if(bChanged)
	{
		dr["Name"] = name;
		dr["Company"] = company;
		dr["Address1"] = address1;
		dr["Address2"] = address2;
		dr["Address3"] = city;
		dr["City"] = city;
		dr["Country"] = country;
		dr["Phone"] = phone;
		dr["Email"] = email;
		
		dr["NameB"] = nameB;
		dr["CompanyB"] = companyB;
		dr["Address1B"] = address1B;
		dr["Address2B"] = address2B;
		dr["CityB"] = cityB;
		dr["CountryB"] = countryB;
		if(Session["ShippingFee"] != null)
			dr["shipping_fee"] = Session["ShippingFee"].ToString();
	}

	dr["CardType"] = Request.Form["CardType"];
	dr["NameOnCard"] = Request.Form["NameOnCard"];
	dr["CardNumber"] = Request.Form["CardNumber"];
	dr["ExpireMonth"] = Request.Form["ExpireMonth"];
	dr["ExpireYear"] = Request.Form["ExpireYear"];

	dr.EndEdit();

	dtUser.AcceptChanges();

//	if(Session["TotalPrice"] == null || Session["TotalGST"] == null || Session["Amount"] == null)
	if(dr["name"].ToString() == "")
		err = "Error, Name can't be blank.";
	else if(dr["Address1"].ToString() == "")
		err = "Error, Address can't be blank.";
	else if(dr["Phone"].ToString() == "")
		err = "Error, Phone Number can't be blank.";
	else if(dr["Email"].ToString() == "")
		err = "Error, Email can't be blank.";
	else if(email != email_confirm)
		err = "Error, Confirming Email address not identical to Email Address.";

	string pass = "";
	string ads = "0";
	if(err == "" && Request.Form["remember"] == "on")
	{
		if(Request.Form["pass"] == "")
			err = "Error, password can't be blank.";
		else if(Request.Form["pass"] != Request.Form["pass_confirm"])
			err = "Error, Confirming Password not identical to Password.";
		else
			pass = Request.Form["pass"];
		if(Request.Form["accept_mass_email"] == "on")
			ads = "1";
	}
	bool bRet = (err == "");
	if(bRet)
	{
		if(bChanged && TS_UserLoggedIn())
			bRet = UpdateAccount();
		else
		{
			string registered = "0";
			if(Request.Form["remember"] == "on")
				registered = "1";
			
			if(!TS_UserLoggedIn())
			{
				bRet = NewCard(email, pass, "customer", name, "", company, address1, address2, city, country,
					nameB, companyB, address1B, address2B, cityB, countryB, phone, "", "", "", "", "", //postal1, 2, 3
					ads, dr["shipping_fee"].ToString(), "0", "0", 1, true, registered);
				if(bRet)
				{
                    return true;
				}
                else
                   err = "Error, this email address has already been used."; 
			}
		}
	}
	string UU_account = @"
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
	if(bRet && GetCartItemForThisSite() <= 0)
	{   err = "<table width=100% cellpadding=0 cellspacing=0 border=0>";
	    err +="<tr><td valign=top bgcolor=#f2f2f2 >"+UU_account+"</td><td>";
		err += "Your Shopping Cart is empty , cannot checkout.<br><br>";
		//err += "<a href=http://nz.eznz.com/" + m_sCompanyName + "/cart.aspx class=d><font size=+1><b>view cart</b></font></a>";
		err += "<a href=c.aspx class=d><font size=+1><b>Go To Home Page</b></font></a>";
		err +="</td></tr></table>";
		return false;
	}
	return bRet;
}

Boolean UpdateAccount()
{
	DataRow dr = dtUser.Rows[0];
//DEBUG("shipping_fee=", dr["shipping_fee"].ToString());
	StringBuilder sb = new StringBuilder();

	sb.Append("UPDATE card SET Name='");
	sb.Append(dr["Name"].ToString());
	sb.Append("', Company='");
	sb.Append(dr["Company"].ToString());
	sb.Append("', Address1='");
	sb.Append(dr["Address1"].ToString());
	sb.Append("', Address2='");
	sb.Append(dr["Address2"].ToString());
	sb.Append("', Address3='");
	sb.Append(dr["Address3"].ToString());
	sb.Append("', City='");
	sb.Append(dr["City"].ToString());
	sb.Append("', Country='");
	sb.Append(dr["Country"].ToString());
	sb.Append("', Phone='");
	sb.Append(dr["Phone"].ToString());
	sb.Append("', NameB='");
	sb.Append(dr["NameB"].ToString());
	sb.Append("', CompanyB='");
	sb.Append(dr["CompanyB"].ToString());
	sb.Append("', Address1B='");
	sb.Append(dr["Address1B"].ToString());
	sb.Append("', Address2B='");
	sb.Append(dr["Address2B"].ToString());
	sb.Append("', CityB='");
	sb.Append(dr["CityB"].ToString());
	sb.Append("', CountryB='");
	sb.Append(dr["CountryB"].ToString());
	sb.Append("', shipping_fee=");
	sb.Append(dr["shipping_fee"].ToString());
	sb.Append(" WHERE email='");
	sb.Append(dr["Email"].ToString());
	sb.Append("'");

	try
	{
        myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

</script>

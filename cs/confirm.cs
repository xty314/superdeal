<!-- #include file="resolver.cs" -->
<!-- #include file="kit_fun.cs" -->
<!-- #include file="card_function.cs" -->

<script runat=server>

string err = "";
Boolean alterColor = false;

int invoiceNumber = -1;
string sCheckoutType = "";
string sPaymentType = "credit card";
string m_orderNumber = "";
string m_branchID = "1";

DataSet ds = new DataSet();

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	string sc = "";
	CheckShoppingCart();
	CheckUserTable();	//get user details if logged on

	if(IsCartEmpty())
	{ 
	
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
		PrintHeaderAndMenu();
		Response.Write("<table width=100% height=400 cellpadding=0 cellspacing=0 border=0>");
		Response.Write("<tr><td valign=top width=200 bgcolor=#F2F2F2><br>"+U_account +"</td><td>");
		Response.Write("<center><h3>Your shopping cart is empty , cannot checkout. <br></h3>");
		Response.Write(" <input type=button onClick=window.location=('cart.aspx') value='View Cart' ></center><br><br><br>\r\n");
		Response.Write("</td></tr></table>");
		PrintFooter();
		return;
	}

	if(!ValidateUserDetails())
	{
		PrintHeaderAndMenu();
		sc = "<br><center><h3>";
		sc += err;
		sc += "</h3><br><br><input type=button onclick=history.go(-1) value=Back class=b></center>\r\n";
		Response.Write(sc);
		return;
	}

	PrintHeaderAndMenu();

	//security check, block suspicious transactions
	if(!CheckTranHistory())
	{
		PrintFooter();
		return;
	}

//	sc = "<br>";
	sc += PrintCart(false, false);	//print shoppingcart, false: no buttons

	if(!CreateOrder())
		return;

	sc += PrintConfirmTable();

	sc += "<tr><td colspan=2 align=center>";
	if(sPaymentType == "credit card")
		sc += "<input type=submit value='Continue' >";
	else
		sc += "<input type=submit value=&nbsp;&nbsp;Continue&nbsp;&nbsp; >";
	sc += "</td></tr><tr><td colspan=2 align=center>";
	if(sPaymentType == "Credit Card")
		sc += "<font color=red size=-2>please only press this button once, this will invoke secured credit card transaction.</font>";
	sc += "</td></tr></table></td></tr></table></form></fieldset>";
	sc +="</td></tr></table>";

	Response.Write(sc);
	PrintFooter();
}

Boolean ValidateUserDetails()
{
	if(dtUser.Rows.Count <= 0)
	{
//DEBUG("error, user table empty", "");
		err = "internal error";
		return false;
	}

	sCheckoutType = Request.Form["CheckoutType"];
	if(sCheckoutType == "Continue With Credit Card >>")
	{
		sPaymentType = "credit card";
	}
	else if(sCheckoutType == "Continue With BankDeposit >>")
	{
		sPaymentType = "deposit";
	}
	else //if(sCheckoutType == "Continue With Cheque >>")
	{
		sPaymentType = "cheque";
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
//					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=checkout.aspx?r=" + DateTime.Now.ToOADate() + "\">");
//					Response.End();
					return true;
				}
			}
		}
	}
	
	
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
	if(bRet && GetCartItemForThisSite() <= 0)
	{   err = "<table width=100% cellpadding=0 cellspacing=0 border=0>";
	    err +="<tr><td valign=top bgcolor=#f2f2f2 >"+U_account+"</td><td>";
		err += "Your Shopping Cart is empty , cannot checkout.<br><br>";
		err += "<a href=http://nz.eznz.com/" + m_sCompanyName + "/cart.aspx class=d><font size=+1><b>view cart</b></font></a>";
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
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
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

string PrintConfirmTable()
{
	DataRow dr = dtUser.Rows[0];
	
	StringBuilder sb = new StringBuilder();

	sb.Append("<form action=");
	if(sPaymentType == "credit card")
	{
///////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////
// for testing
		bool bTest = false;
		if(Session["email"] != null && Session["email"].ToString().IndexOf("@eznz.com") > 0)
			bTest = true;
///////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////

		if(bTest)
		{
			Response.Write("<form action=trans.asp?r=");
			Response.Write(DateTime.Now.ToOADate() + "&pid=" + m_orderNumber);
			Response.Write(" method=post>");
			Response.Write("<input type=hidden name=test value=1>");
		}
		else
		{
			sb.Append("https://www.eznz.co.nz/" + m_sCompanyName + "/trans.asp?pid=" + m_orderNumber);
			sb.Append("&r=" + DateTime.Now.ToOADate());
		}
	}
	else
	{
		sb.Append("result.aspx");
	}
	sb.Append(" method=post>");

	if(Session["host_name"] == null)
		Session["host_name"] = "";
	string host_name = Session["host_name"].ToString();
	
	if(Session["rip"] == null)
		Session["rip"] = "";
	sb.Append("<input type=hidden name=card_id value=" + Session["card_id"].ToString() + ">");
	sb.Append("<input type=hidden name=ip value=" + Session["rip"].ToString() + ">"); //show card details form
	sb.Append("<input type=hidden name=host_name value='" + host_name + "'>"); //show card details form
	sb.Append("<input type=hidden name=po_id value='" + m_orderNumber + "'>");

	string url = "";
	string servername = Request.ServerVariables["SERVER_NAME"];
	string s = Request.ServerVariables["URL"];
//DEBUG("s=", s);
	int i = s.Length - 1;
	for(; i>=0; i--)
	{
		if(s[i] == '/')
			break;
	}
	
	s = s.Substring(0, i);
	url = "http://" + servername + s + "/result.aspx?t=cc&oid=" + Session["OrderNumber"];
//DEBUG("url=", url);

	sb.Append("<input type=hidden name=result_url value='");
	sb.Append(url + "'>");
	sb.Append("<table width=95% align=center border=0 title><tr><td>\r\n");
	sb.Append("<table width=100% cellpadding=2 cellspacing=1 align=center border=0>\r\n");
//	sb.Append("<tr><td colspan=2>&nbsp;</td></tr>\r\n");
	sb.Append("<tr><td><table><tr><td colspan=2 style=\"font:bold 13px arail; color:#6699CC\">Shipping Address</td></tr>\r\n");

	sb.Append("<tr><td>Name </td><td>");
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
	sb.Append(dr["Email"].ToString());
	sb.Append("</td></tr>\r\n</table>\r\n");

	sb.Append("</td>\r\n\r\n<td valign=top>\r\n");

	//billing table
	sb.Append("<table>\r\n");
	sb.Append("<tr><td colspan=2 style=\"font:bold 13px arail; color:#6699CC\">Billing Address</td></tr>\r\n<tr><td>Name</td><td>");

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

	sb.Append("</td></tr>\r\n</table>\r\n");
	//end of billing table
	sb.Append("<input type=hidden name=Amount value='");
	sb.Append(Session["Amount"].ToString());
	sb.Append("'>\r\n");
//DEBUG("amout=", Session["Amount"].ToString());
	sb.Append("<input type=hidden name=InvoiceNumber value='");
	sb.Append(Session["OrderNumber"]);
	sb.Append("'>\r\n");
	
	sb.Append("<input type=hidden name=PaymentType value='");
	sb.Append(sPaymentType);
	sb.Append("'>\r\n");

	sb.Append("</td></tr>");

	return sb.ToString();
}

bool UpdateOrder()
{
	string temp_paymenttype = GetEnumID("payment_method", sPaymentType.ToLower());

	string sc = " UPDATE orders SET payment_type='" + temp_paymenttype + "'";
	sc += " WHERE id=" + m_orderNumber;
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

Boolean CreateOrder()
{
	if(Session["OrderCreated"] != null)
	{
		if(Session["OrderCreated"] == "true")
		{
			m_orderNumber = Session["OrderNumber"].ToString();
			return UpdateOrder(); //update payment type
		}
	}
//DEBUG("sdf gstg =", Session[m_sCompanyName +"gst_rate"].ToString());
//DEBUG("sPaymentType =", sPaymentType);
	string payment_method = GetEnumID("payment_method", sPaymentType.ToLower());

	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, card_id, po_number) VALUES(0, " + Session["card_id"].ToString() + ", '";
	sc += EncodeQuote(Request.Form["po_number"]) + "') SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "id") == 1)
		{
			m_orderNumber = ds.Tables["id"].Rows[0]["id"].ToString();
			Session["OrderNumber"] = m_orderNumber;
			//assign ordernumber same as id
			sc = "UPDATE orders SET number=" + m_orderNumber;
			sc += ", shipping_method=4";// 4: 2 day courier + Request.Form["shipping_method"];
			sc += ", freight=" + Session["ShippingFee"].ToString();
			sc += ", no_individual_price = 0 ";
//			sc += ", pick_up_time='" + pickup_time + "'";
//			if(Request.Form["special_shipto"] == "1" || (bool)Session["login_is_branch"] )
//				sc += ", special_shipto=1, shipto='" + EncodeQuote(Request.Form["ssta"]) + "' ";
//			sc += ", contact='" + EncodeQuote(Request.Form["contact"]) + "' ";
			if(Session[m_sCompanyName +"gst_rate"] != null && Session[m_sCompanyName +"gst_rate"] != "")
				sc += ", customer_gst = '"+ Session[m_sCompanyName +"gst_rate"].ToString() +"' ";
			sc += ", sales_note='Online Retail Order' ";
			sc += ", payment_type=" + payment_method; 
			sc += " WHERE id=" + m_orderNumber;
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

	Session["OrderCreated"] = "true";
/*
	MailMessage msgMail = new MailMessage();

	msgMail.To = m_sAdminEmail;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = "Online Checking Out - " + m_sCompanyName;
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "Order Number : " + m_orderNumber + " \r\n";
	msgMail.Body += "Payment Method : " + sPaymentType;

	SmtpMail.Send(msgMail);
*/
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
		sc += ", " + Math.Round(MyDoubleParse(dr["salesPrice"].ToString()), 2) + ") "; 

		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + dr["code"].ToString();
		sc += " AND branch_id = " + m_branchID;
		sc += ")";
		sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
		sc += " VALUES (" + dr["code"].ToString() + ", " + m_branchID + ", 0, " + dr["quantity"].ToString() + ")"; 
		sc += " ELSE Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock + " + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString() + " AND branch_id = " + m_branchID;

		sc += " UPDATE product SET allocated_stock = allocated_stock+" + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString();
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

bool CheckTranHistory()
{
//trans log checking is only for eden
return true;

	string host_name = "";
	if(Session["host_name"] == null)
	{
		CResolver rs = new CResolver();
		if(Session["rip"] != null)
			host_name = rs.Resolve(Session["rip"].ToString());
		Session["host_name"] = host_name;
	}
	else
		host_name = Session["host_name"].ToString();

	string country = "nz";
//host_name = "p321-ipadla.saitma.ocn.ne.jp";
	if(host_name.Length >= 3)
	{
		country = host_name.Substring(host_name.Length - 3, 3);
	}
	country = country.ToLower();
	if(country == ".jp" || country == ".id" || country == ".ph")
	{
		MailMessage msgMail = new MailMessage();
		
		msgMail.To = m_emailAlertTo;
		msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
		msgMail.Subject = "IP rejected for checkout";
		msgMail.Body = "IP:" + Session["rip"].ToString() + "\r\n";
		msgMail.Body += "host:" + Session["host_name"].ToString() + "\r\n";

		SmtpMail.Send(msgMail);
		Response.Write("<br><br><center><font color=red><h3>Stop !</h3></font>");
		Response.Write("<h3>Sorry, we only sell within New Zealand</h3>");
		Response.Write("<h5>Call us if you are right in New Zealand now, there maybe an IP confusion</h5><br>");
		Response.Write("<b>Your IP : </b>" + Session["rip"].ToString() + "<br>");
		Response.Write("<b>Your Host Name : </b>" + Session["host_name"].ToString() + "<br>");
		Response.Write("<br><br><br><br><br><br><br>");
		return false;
	}

	DataSet dsctl = new DataSet();
	int rows = 0;
	string sc = "SELECT DISTINCT card_number FROM cctrans_log WHERE ip='" + Session["rip"] + "' ";
	sc += " AND success=0 AND DATEDIFF(hour, LogTime, GETDATE())<=24";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsctl, "cctrans_log");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows > 2)
	{
		MailMessage msgMail = new MailMessage();
		
		msgMail.To = m_emailAlertTo;
		msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
		msgMail.Subject = "Suspicious Transaction Stopped";
		msgMail.Body = "IP:" + Session["rip"].ToString() + "\r\n";
		msgMail.Body += "host:" + Session["host_name"].ToString() + "\r\n";

		SmtpMail.Send(msgMail);
		Response.Write("<br><br><center><font color=red><h3>Stop !</h3></font>");
		Response.Write("<h5>Suspicious transaction, call us if you are using your won credit card</b><br><br><br><br><br><br>");
		return false;
	}
	return true;
}
</script>

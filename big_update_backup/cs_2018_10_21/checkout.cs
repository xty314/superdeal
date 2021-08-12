<!-- #include file="credit_limit.cs" -->
<!-- #include file="kit_fun.cs" -->

<script runat=server>

bool alterColor = false;
bool m_bDirectOrder = false;
string m_msg = "";

//retail version
string m_sPaymentOption = "to be defined";

protected void Page_Load(Object Src, EventArgs E ) 
{
	//wholesale direct order process
	if(Request.Form["cmd"] == "Direct Order")
	{
		m_bDirectOrder = true;
		TS_Init();
		if(!DoLogin())
		{
			Response.Redirect("login.aspx");
			return;
		}
		if(!DoDirectOrder())
		{
			PrintHeaderAndMenu();
			Response.Write("<center><br><br><h3>Direct Order Failed</h3>");
			Response.Write("<h5>" + m_msg + "</h5>");
			Response.Write("<br><br><br><br><br><br><br>");
			PrintFooter();
			return;
		}
	}
	//retail version
	TS_PageLoad(); //do common things, LogVisit etc...
	if(g_bRetailVersion && m_sSite == "www")
	{
		RememberLastPage();
		PrintHeaderAndMenu();
		m_sPaymentOption = ReadSitePage("payment_option_public");

		if(Session[m_sCompanyName + "_terms_agreed"] == null)
		{
			if(Request.QueryString["t"] == "at") //agreed
			{
				Session[m_sCompanyName + "_terms_agreed"] = true;
			}
			else
			{
				AskForTerms();
				PrintFooter();
				Response.End();
			}
		}

		string s = "<INPUT TYPE=submit NAME=CheckoutType VALUE='Continue With CreditCard >>'>";
		if(Session[m_sCompanyName + "no_credit_card"] != null)
			s = "<input type=button value=' Credit Card Not Applied ! '>";
		
		m_sPaymentOption = m_sPaymentOption.Replace("@@credit_card_button", s);

		CheckUserTable();		//get user details if logged on
		Retail_PrintBody();
		LPaymentOption.Text = m_sPaymentOption;
		LFooter.Text = m_sFooter;
		return;
	}

	if(Session["online_order_po_number"] == null)
		Session["online_order_po_number"] = "";
	if(Request.Form["po_number"] != null)
		Session["online_order_po_number"] = Request.Form["po_number"];

	if(Session["online_order_contact"] == null)
		Session["online_order_contact"] = Session["name"].ToString();
	if(Request.Form["contact"] != null)
		Session["online_order_contact"] = Request.Form["contact"];

	if(Request.Form["shipto"] != null)
		Session["online_order_shipto"] = Request.Form["shipto"];

	if(Request.Form["cmd"] == "Place Order")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=result.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	PrintHeaderAndMenu();
/*	
	if(Session[m_sCompanyName + "_terms_agreed"] == null)
	{
		if(Request.QueryString["t"] == "at") //agreed
		{
			Session[m_sCompanyName + "_terms_agreed"] = true;
		}
		else
		{
			AskForTerms();
			PrintFooter();
		}
	}
*/
	CheckUserTable();		//get user details if logged on
	bool bCreditOK = true;
	if(Session["Amount"] != null)
	{
		bCreditOK = CreditLimitOK(Session["card_id"].ToString(), MyDoubleParse(Session["Amount"].ToString()));
		if(!bCreditOK)
			return;
	}

	string reason = "";
	bool bStopOrdering = IsStopOrdering(Session["card_id"].ToString(), ref reason);
	if(bStopOrdering)
	{
		if(reason == "")
			reason = "Contact our sales for more information.";
		Response.Write("<br><br><center><h3>Your account has been disabled to place order</h3><br>");
		Response.Write("<br><center><h4><font color=red>" + reason + "<font color=red></h4><br><br><br><br><br><br><br><br>");
		LFooter.Text = m_sFooter;
		return;
	}

	PrintBody();
	LFooter.Text = m_sFooter;
}

void AskForTerms()
{  

   string LeftHandSideMenu = ReadSitePage("public_left_side_menu");
   if(Cache["item_categories"] != null)
		LeftHandSideMenu = LeftHandSideMenu.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	else
	   LeftHandSideMenu = LeftHandSideMenu.Replace("@@HEADER_MENU_TOP_CAT", ""); 
	             
    
    Response.Write("<table width=100% cellpadding=0 cellspacing=0 border=0>");
	Response.Write("<tr><td valign=top>");
	//Response.Write(LeftHandSideMenu);
	Response.Write("</td>");
	Response.Write("<td width=10></td><td>");
	Response.Write("<center><h4>Please read our <a href=sp.aspx?terms class=o target=_blank>Terms and Conditions of Sales</a>");
    //Response.Write(" and <font color=red>make sure you must be over 18 years old</font> ");
	Response.Write("before check out</h4>");
	Response.Write("<br><br><input type=button onclick=window.location=('checkout.aspx?t=at&r=" + DateTime.Now.ToOADate() + "') value='I have read and agree' class=b>");
	Response.Write("</td></tr></table>");
}

void PrintBody()
{
	if(dtUser.Rows.Count <= 0)
	{
//DEBUG("error, user table empty", "");
		return;
	}

	string od = ""; //(old data), used to detect changes
	DataRow dr = dtUser.Rows[0];

	string myAddr = dr["trading_name"].ToString();
//	if((bool)Session["login_is_branch"])
	myAddr += " " + dr["branch"];
	myAddr += "\r\n";
	myAddr += dr["address1"].ToString() + "\r\n";
	myAddr += dr["address2"].ToString() + "\r\n";
	myAddr += dr["address3"].ToString() + "\r\n";
	myAddr += "Ph : " + dr["phone"].ToString() + "\r\n";

	Response.Write("<form name=f action=result.aspx?r=" + DateTime.Now.ToOADate() + " method=post>");
	Response.Write("<input type=hidden name=myaddr value='" + myAddr + "'>");
	Response.Write("<center><font size=+1><b>");
	if(m_bDirectOrder)
		Response.Write("Direct Order");
	else
		Response.Write("PO Number & Shipping Options");
	Response.Write("</b></font><br><br>");

	double dSubTotal = 0;
	double dTotal = 0;
	if(m_bDirectOrder)
	{
		Response.Write("<table valign=center cellspacing=2 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

		Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
		Response.Write("<th>Code</th>");
		Response.Write("<th>M_PN</th>");
		Response.Write("<th>Description</th>");
		Response.Write("<th align=right>Price</th>");
		Response.Write("<th align=right>Quantity</th>");
		Response.Write("<th align=right>Total</th>");
		Response.Write("</tr>");

		bool bAlterColor = false;
		for(int i=0; i<dtCart.Rows.Count; i++)
		{
			DataRow drc = dtCart.Rows[i];
			string code = drc["code"].ToString();
			string supplier_code = drc["supplier_code"].ToString();
			string name = drc["name"].ToString();
			int quantity = MyIntParse(drc["quantity"].ToString());
			double dPrice = Math.Round(MyDoubleParse(drc["salesPrice"].ToString()), 2);

			double dRowTotal = Math.Round(dPrice * quantity, 2);
			dSubTotal += dRowTotal;

			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");

			Response.Write("<td>" + code + "</td>");
			Response.Write("<td>" + supplier_code + "</td>");
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td align=right>" + dPrice.ToString("c") + "</td>");
			Response.Write("<td align=right>" + quantity + "</td>");
			Response.Write("<td align=right>" + dRowTotal.ToString("c") + "</td>");
			Response.Write("</tr>");
		}
		
		double dTax = dSubTotal * MyDoubleParse(Session["gst_rate"].ToString());
		dTax = Math.Round(dTax, 2);
		dTotal = dSubTotal + dTax;

		Response.Write("<tr><td colspan=5 align=right><b>Sub Total : </b></td>");
		Response.Write("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
//		Response.Write("<tr><td colspan=5 align=right><b>Tax : </b></td>");
//		Response.Write("<td align=right>" + dTax.ToString("c") + "</td></tr>");
//		Response.Write("<tr><td colspan=5 align=right><b>Total Amount Due : </b></td>");
//		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td></tr>");

		Response.Write("</table>");
	}

	Response.Write("\r\n<table align=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("\r\n<tr><td nowrap><b>P.O. Number</b></td>");
	Response.Write("\r\n<td><input type=text name=po_number value='" + Session["online_order_po_number"].ToString() + "'></td></tr>");
	Response.Write("\r\n<tr><td nowrap><b>Contact Person</b></td>");
	Response.Write("\r\n<td><input type=text name=contact value='" + Session["online_order_contact"].ToString() + "'></td></tr>");
/*
	Response.Write("\r\n<tr><td><b>Shipping Method</b></td><td nowrap>");
	Response.Write("\r\n<select name=shipping_method onchange=\"if(this.value=='1')");
	Response.Write("\r\n{document.all('tpt').style.visibility='visible';");
	Response.Write("\r\ndocument.all('sshipto').style.visibility='hidden';");
	Response.Write("\r\ndocument.all('ssta').style.visibility='hidden';}");
	Response.Write("\r\nelse{document.all('tpt').style.visibility='hidden';");
	Response.Write("\r\ndocument.all('sshipto').style.visibility='visible';");
//	Response.Write("if(document.f.special_shipto.value=='1')document.all('ssta').style.visibility='visible';}\">");
	Response.Write("\r\ndocument.all('ssta').style.visibility='visible';}\">");
	Response.Write("\r\n}\">");
	Response.Write("\r\n<option value=0 selected>Please Select One</option>");
	Response.Write(GetEnumOptions("shipping_method", "0"));
	Response.Write("\r\n</select>");
	*/
    Response.Write("\r\n<input type=hidden  name=shipping_method value=4>");
	Response.Write("\r\n<span id=tpt style=visibility:hidden>");
	Response.Write(" <b>Pick Up Time : </b><input type=text name=pickup_time maxlength=49 size=10 value='" + Session["pick_up_time"] + "'>");
	Response.Write("\r\n</span>");

	Response.Write("\r\n</td></tr>");

	//Response.Write("\r\n<tr><td colspan=2>");
Response.Write("\r\n<tr><td valign=top><b>Ship To</b></td><td>");
	Response.Write("\r\n<table id=sshipto style=visibility:visible>");
	Response.Write("\r\n<tr><td>");
//	Response.Write("\r\n<b>Ship To</b></td><td>");
	Response.Write("\r\n<select name=special_shipto onclick=\"");
	Response.Write("\r\n{if(this.value=='0'){");
	Response.Write("\r\ndocument.f.ssta.value=document.f.myaddr.value;");
	Response.Write("\r\ndocument.f.ssta.disabled=true;");
	Response.Write("\r\n}else{");
	Response.Write("\r\ndocument.f.ssta.disabled=false;");
	string ssship = "";
	if(Session["online_order_shipto"] != null)
		ssship = Session["online_order_shipto"].ToString();
	ssship = ssship.Replace("\r\n", "' + '\\r\\n' + '");
	Response.Write("document.f.ssta.value='" + ssship + "';");
	Response.Write("}}\">");
	Response.Write("<option value=0>" + TSGetUserCompanyByID(Session["card_id"].ToString()) + "</option>");
	Response.Write("<option value=1 ");
	if(m_bDirectOrder && Request.Form["shipto"] != "")
		Response.Write(" selected");
	Response.Write(">Special Address</option>");
	Response.Write("</select>");
	Response.Write("</td></tr>");

	//Response.Write("<tr><td valign=top>&nbsp;</td>");
	Response.Write("<tr>");
	Response.Write("<td><textarea ");
	if(m_bDirectOrder && Request.Form["shipto"] != null && Request.Form["shipto"] != "")
	{
		Response.Write(" name=ssta cols=35 rows=7>");
		Response.Write(Session["online_order_shipto"]);
	}
	else
	{
		Response.Write(" disabled ");
		Response.Write(" name=ssta cols=35 rows=7>");
		Response.Write(myAddr);
	}
	Response.Write("</textarea>");
	Response.Write("\r\n</td></tr>");
	Response.Write("\r\n</table>");

	Response.Write("\r\n</td></tr>");

	Response.Write("\r\n<tr><td valign=top><b>Note (optional)</b></td><td><textarea name=note cols=40 rows=5>");
	Response.Write(Request.Form["note"]);
//	Response.Write(Session["online_order_note"]);
	Response.Write("</textarea></td></tr>");

	Response.Write("\r\n<tr><td colspan=2 align=right>");
	Response.Write("<font color=red><b><i>(Please select shipping method) </i></b></font>");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Place Order' ");
	//Response.Write(" onclick='return checkship()'");
    Response.Write(" OnClick=\"if(!window.confirm('You must Read and Agree the Terms and Conditions of Super Deal LTD!')){return false;}\" ");
	Response.Write("></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");

	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string s = @"
	
	function checkship() 
	{
		if(document.f.shipping_method.value == '0')
		{
			window.alert('Please select shipping method!');
			return false;
		}
		else 
			return true;
	}";
	Response.Write("--> "); 
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> ");
}

bool DoLogin()
{
	string login = Request.Form["login"];
	if(login == null || login == "")
		return false;
	string password = Request.Form["password"];
	if(password == null || password == "")
		return false;

	DataTable dt = null;
	DataRow dr = null;

	if(GetAccount(login, ref dt))
	{
		if(dt == null)
			return false;

		DataRow dra = dt.Rows[0];

		string pass = dt.Rows[0]["password"].ToString();
		if(password == pass) //check password first
		{
			TS_LogUserIn();
			dr = dt.Rows[0];
			DataRow drl = dr; //login datarow
			Session["login_card_id"] = dr["id"].ToString();
			Session["name"] = dr["name"].ToString();
			Session["email"] = dr["email"].ToString();
			Session["main_card_id"] = dr["main_card_id"].ToString();
			Session["branch_card_id"] = dr["branch_card_id"].ToString();
			Session["customer_access_level"] = dr["customer_access_level"].ToString();
			Session["login_is_branch"] = false;
			if(bool.Parse(dr["is_branch"].ToString()))
				Session["login_is_branch"] = true;
			if(Session["main_card_id"] != null && Session["main_card_id"].ToString() != "")
			{
				dr = GetCardData(Session["main_card_id"].ToString());
			}
			Session["card_id"] = dr["id"].ToString();

			string lkey = m_sCompanyName + "AccessLevel";
			Session[lkey] = dr["access_level"].ToString();

			//discount and credit
			string dkey = m_sCompanyName + "discount";
			Session[dkey] = dr["discount"].ToString();
			dkey = m_sCompanyName + "balance";
			Session[dkey] = dr["balance"].ToString();
			dkey = m_sCompanyName + "dealer_level";
			Session[dkey] = dr["dealer_level"].ToString();
			
			string bkey = m_sCompanyName + "lastbranch";
			Session[bkey] = dr["last_branch_id"].ToString();

			Session["card_type"] = dr["type"].ToString();
			Session["supplier_short_name"] = dr["short_name"].ToString();
			Session["gst_rate"] = dr["gst_rate"].ToString();
			Session["cat_access"] = dr["cat_access"].ToString() + "," + GetCatAccessGroupString(dr["id"].ToString());
			UpdateSessionLog();

			CheckUserTable();
			DataRow dru = dtUser.Rows[0];
			
			dtUser.AcceptChanges();
			dr.BeginEdit();

			dru["id"]		= dr["id"].ToString();//Session["card_id"].ToString();
			dru["Name"]		= dr["name"].ToString();//Session["name"].ToString();
			dru["Branch"]	= "";
			dru["Company"]	= dr["Company"].ToString();
			dru["trading_name"]	= dr["trading_name"].ToString();
			dru["corp_number"]	= dr["corp_number"].ToString();
			dru["directory"]	= dr["directory"].ToString();
			dru["gst_rate"]	= dr["gst_rate"].ToString();

			if(Session["branch_card_id"].ToString() != "")
			{
				DataRow drBranch = GetCardData(Session["branch_card_id"].ToString());
				if(drBranch != null)
					drl = drBranch; //use branch card for shipping address
				dru["Address1"]	= drl["Address1"].ToString();
				dru["Address2"]	= drl["Address2"].ToString();
				dru["Address3"]	= drl["Address3"].ToString();
				dru["Phone"]	= drl["Phone"].ToString();
				dru["Fax"]		= drl["fax"].ToString();
				dru["branch"]	= drl["trading_name"].ToString();
			}
			else if((bool)Session["login_is_branch"])
			{
				dru["Address1"]	= drl["Address1"].ToString();
				dru["Address2"]	= drl["Address2"].ToString();
				dru["Address3"]	= drl["Address3"].ToString();
				dru["Phone"]	= drl["Phone"].ToString();
				dru["Fax"]		= drl["fax"].ToString();
				dru["branch"]	= drl["trading_name"].ToString();
			}
			else
			{
				dru["Address1"]	= dr["Address1"].ToString();
				dru["Address2"]	= dr["Address2"].ToString();
				dru["Address3"]	= dr["Address3"].ToString();
				dru["Phone"]	= dr["Phone"].ToString();
				dru["Fax"]		= dr["fax"].ToString();
			}
			dru["postal1"]	= dr["postal1"].ToString();
			dru["postal2"]	= dr["postal2"].ToString();
			dru["postal3"]	= dr["postal3"].ToString();
			dru["Email"]		= dr["email"].ToString();//Session["email"].ToString();

			dru["pm_email"]	= dr["pm_email"].ToString();
			dru["pm_ddi"]	= dr["pm_ddi"].ToString();
			dru["pm_mobile"]	= dr["pm_mobile"].ToString();
			dru["sm_name"]	= dr["sm_name"].ToString();
			dru["sm_email"]	= dr["sm_email"].ToString();
			dru["sm_ddi"]	= dr["sm_ddi"].ToString();
			dru["sm_mobile"]	= dr["sm_mobile"].ToString();
			dru["ap_name"]	= dr["ap_name"].ToString();
			dru["ap_email"]	= dr["ap_email"].ToString();
			dru["ap_ddi"]	= dr["ap_ddi"].ToString();
			dru["ap_mobile"]	= dr["ap_mobile"].ToString();
			
			dr.EndEdit();
			dtUser.AcceptChanges();
			return true;
		}
	}
	return false;
}

bool DoDirectOrder()
{
	string srows = Request.Form["rows"];
	if(srows == null || srows == "" || !IsInteger(srows))
	{
		m_msg = "Total Items count error";
		return false;
	}
	int rows = MyIntParse(srows);
	int addedToCart = 0;
	for(int i=0; i<rows; i++)
	{
		string code = Request.Form["code" + i];
		string name = Request.Form["name" + i];
		if(!CodeOK(code, name))
			continue;

		string qty = Request.Form["qty" + i];
		
		double dSalesPrice = GetSalesPriceForDealer(code, qty, Session[m_sCompanyName + "dealer_level"].ToString(), Session["card_id"].ToString());
		if(AddToCart(code, qty, dSalesPrice.ToString()))
			addedToCart++;
	}
	if(addedToCart <= 0)
		return false;

	Session["online_order_po_number"] = Request.Form["po_number"];
	Session["online_order_contact"] = Request.Form["contact"];

	return true;
}

bool CodeOK(string code, string name)
{
	if(!IsInteger(code))
	{
		m_msg += "<br><b>Item code# <font color=red>" + code + " " + name + "</font> not found.<b>";
		return false;
	}

	DataSet ds = new DataSet();
	string sc = "SELECT p.price, c.* FROM product p JOIN code_relations c ON p.code=c.code WHERE c.code=" + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "codeok") <= 0)
		{
			m_msg += "<br><b>Item code# <font color=red>" + code + " " + name + "</font> not found.<b>";
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

bool GetAccount(string name, ref DataTable dt)
{
	DataSet dsa = new DataSet();
	string sc = "SELECT c.*, '" + m_sCompanyName + "' AS site ";
	sc += "FROM card c WHERE c.email='";
	sc += EncodeQuote(name);
	sc += "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsa, "cart") > 0)
		{
			if(dsa.Tables["cart"] == null)
				return false;
			else
			{
				dt = dsa.Tables["cart"];
				return true;
			}
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

//retail version
void Retail_PrintBody()
{
	if(dtUser.Rows.Count <= 0)
		return;

    string od = ""; //(old data), used to detect changes
	DataRow dr = dtUser.Rows[0];
	StringBuilder sb = new StringBuilder();

    //shopping cart info
    CheckShoppingCart();
    if(dtCart != null && dtCart.Rows.Count>0)
    {
        
        string cart_table_top = @"
            <br><table cellpadding=0 cellspacing=5 border=1 bordercolor=#CCCCCC width=100% align=center>
	            <tr style='background-color:#208BAE'>
    	            <td width=70% style='border:none; font-size:18px; font-family:Arial, Helvetica, sans-serif; color:#FFFFFF;' align=left>&nbsp;&nbsp;Item</td>
                    <td width=10% style='border:none; font-size:18px; font-family:Arial, Helvetica, sans-serif; color:#FFFFFF;' align=center>QTY</td>
                    <td width=10% style='border:none; font-size:18px; font-family:Arial, Helvetica, sans-serif; color:#FFFFFF;' align=center>Price</td>
                    <td width=10% style='border:none; font-size:18px; font-family:Arial, Helvetica, sans-serif; color:#FFFFFF;' align=center>Total</td>
                </tr>
        ";
        sb.Append(cart_table_top);
        string item_details = "";
        double totalSalePrice = 0;
        for(int k=0; k<dtCart.Rows.Count; k++)
        {
            DataRow drCart = dtCart.Rows[k];
            double singlePrice = 0;
            singlePrice = double.Parse(drCart["salesPrice"].ToString())*1.15;
            string item_name = drCart["name"].ToString();
            int item_qty = int.Parse(drCart["quantity"].ToString());
            string item_code = drCart["code"].ToString();
            totalSalePrice += double.Parse(drCart["salesPrice"].ToString())*int.Parse(drCart["quantity"].ToString());
            item_details += "<tr><td style='border:none;' align=left>&nbsp;&nbsp;" + item_name + "</td>";
            item_details += "<td style='border:none;' align=center>" + item_qty + "</td>";
            item_details += "<td style='border:none;' align=center><b>" + singlePrice.ToString("c") +"</b></td>";
            item_details += "<td style='border:none;' align=center><b>" + (item_qty*singlePrice).ToString("c") + "</b</td>";
            item_details += "</tr>";
            item_details += "<tr><td colspan='4' style='border:none;'><hr></td></tr>";
        }
        totalSalePrice = totalSalePrice * 1.15;
        sb.Append(item_details);
        double ship_fee = double.Parse(Session["ShippingFee"].ToString())*1.15;
        string cart_table_bot = "<tr><td style='border:none;'></td><td align=center colspan=2 style='border:none;'>*Delivery Cost</td>";
        cart_table_bot += "<td style='border:none;' align=center><b>" + ship_fee.ToString("c") + "</b></td></tr>";
        cart_table_bot += "<tr><td style='border:none;'></td><td colspan=3 style='border:none;'><hr></td></tr>";
        cart_table_bot += "<tr ><td style='border:none;'></td>";
        cart_table_bot += "<td align=center colspan=2 style='border:none;color:#FFFFFF;font-weight:bold;background-color:#990000;' >TOTAL</td>";
        cart_table_bot += "<td style='border:none;color:#FFFFFF;font-weight:bold;background-color:#990000;' align=center>" + (totalSalePrice+ship_fee).ToString("c") + "</td></tr></table>";
        sb.Append(cart_table_bot);
        //totalSalePrice = Math.Round(totalSalePrice, 2);
        LCartInfo.Text = sb.ToString();
    }
    else
    {
       LCartInfo.Text = "<br> <font style='font-family:Arial, Helvetica, sans-serif; font-size:20px; font-weight:bold; color:#FF0000' >Your Shopping Cart is Empty!</font><br><br>";
       LOptions.Text = "<input type=button OnClick=window.location=('" + Session["item_list_url"] + "'); style=\"background-image:url(pic/login_btn.jpg);border:none;color:#FFFFFF;font-family:Arial, Helvetica, sans-serif; font-weight:bold;font-size:15px;cursor:pointer;\" value='Continue Shopping'>";
        return;
    }

    sb.Remove(0, sb.Length);
	//sb.Append("<H3  Style=\"font:bold 18px arail; color:#6699CC; text-align:left\">Shipping Address</H3><TABLE BORDER=0>\r\n");
    sb.Append("<TABLE BORDER=0>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Name<font color=red>*</font></TD><TD><INPUT NAME=name VALUE='");
	sb.Append(dr["Name"].ToString());
	od += dr["name"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Company</TD><TD><INPUT NAME=company VALUE='");
	sb.Append(dr["Company"].ToString());
	od += dr["Company"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Address<font color=red>*</font></TD><TD><INPUT NAME=address1 VALUE='");
	sb.Append(dr["Address1"].ToString());
	od += dr["Address1"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>&nbsp;</TD><TD><INPUT NAME=address2 VALUE='");
	sb.Append(dr["Address2"].ToString());
	od += dr["Address2"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">City<font color=red>*</font></TD><TD><INPUT NAME=city VALUE='");
	sb.Append(dr["City"].ToString());
	od += dr["City"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Country<font color=red>*</font></TD><TD><INPUT NAME=country VALUE='");
	sb.Append(dr["Country"].ToString());
	od += dr["Country"];
	if(dr["Country"] == null || dr["Country"].ToString() == "")
	{
//		sb.Append("New Zealand");
		od += "New Zealand";
	}
	sb.Append("' SIZE=30 MAXLENGTH=30 ></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Phone<font color=red>*</font></TD><TD><INPUT NAME=phone VALUE='");
	sb.Append(dr["Phone"].ToString());
	od += dr["Phone"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
    sb.Append("</TABLE>\r\n");
	
	LFormS.Text = sb.ToString();
	
	
    
	sb.Remove(0, sb.Length);
	if(!TS_UserLoggedIn())
    {
	    string Ftitle = @"
	        <fieldset><legend style='font:bold 15px arial;color:#6699CC'>New Customer Register</legend>  
		    ";
		string Ftitleb = @"
	        </fieldset> 
		    ";
        LFormss.Text = "New Customer Register"; 
	    //LFormss.Text = Ftitle;
        //Ltb.Text = Ftitleb;
    }
    else
       LFormss.Text = "Account Information"; 
	
	sb.Remove(0, sb.Length);
	string LeftHandSideMenu = ReadSitePage("public_left_side_menu");
    if(Cache["item_categories"] != null)
		LeftHandSideMenu = LeftHandSideMenu.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	else
	   LeftHandSideMenu = LeftHandSideMenu.Replace("@@HEADER_MENU_TOP_CAT", ""); 
	//sb.Append(LeftHandSideMenu);
	//Lmenu.Text = sb.ToString();
	
	sb.Remove(0, sb.Length);
	if(!TS_UserLoggedIn())
    {
	    sb.Append("<fieldset><legend style=\"font:bold 15px arial;color:#6699CC\">Existing Customer Login</legend>");
	    sb.Append("<table width=600 height=50 cellpadding=0 cellspacing=0 border=0 align=center   >");
	    sb.Append("<tr>");
	    sb.Append("<td  Style=\"font:bold 13px arail; color:#6699CC; text-align:center\" >LoginEmail : ");
	    sb.Append("<input type=text name=name size=22 class=LoginField ><input type=hidden name=name_old value='@@name_field'></td>");
	
	    sb.Append("<td   Style=\"font:bold 13px arail; color:#6699CC; text-align:left\" >Password : ");
	    sb.Append("<input type=password name=pass size=22 class=LoginField >");
	    //sb.Append("<td  height=28 align=left  >");
	    sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<input type=submit name=cmd ");
        sb.Append("style=\"background-image:url(pic/login_btn.jpg);border:none;color:#FFFFFF;font-family:Arial, Helvetica, sans-serif; font-weight:bold;font-size:15px;cursor:pointer; \"  value='Login' /></td>");
	    sb.Append("</tr></table>");
	    sb.Append("</fieldset>");
	}
	else
    {
	    string p_logoff = @" 
						<fieldset><legend style='font:bold 15px arial;color:#6699CC'>Account Control Panel</legend>
		                <table cellpadding=0 cellspacing=0 border=0 class=confirm_t>
					        <tr>
						        <td align=right width=8%><a href=status.aspx? ><img src=pic/trace.png border=0 width=30 height=30></a></td><td align=left width=17%>&nbsp;<a href=status.aspx? >Order Status</a></td>
						        <td align=right width=8%><a href=register.aspx ><img src=pic/update.png border=0 width=30 height=30></a></td><td align=left width=17%>&nbsp;<a href=register.aspx >Update Details</a></td>
						        <td align=right width=8%><a href=setpwd.aspx ><img src=pic/password.png border=0 width=30 height=30></a></td><td align=left width=23%>&nbsp;<a href=setpwd.aspx >Change Password</a></td>
						        <td align=right width=8%><a href=login.aspx?logoff=true ><img src=pic/poweroff.png border=0 width=30 height=30></a></td><td align=left width=11%>&nbsp;<a href=login.aspx?logoff=true >Logout</a></td>
                            </tr>
					    </table>
						</fieldset>
						  ";
	    sb.Append(p_logoff);
	}
   	TloginF.Text = sb.ToString();
	
    //billing address table
	sb.Remove(0, sb.Length);
    
    sb.Append("<INPUT type=hidden NAME=nameB VALUE='" +dr["NameB"].ToString()+ "'>");
    od += dr["NameB"];
	sb.Append("<INPUT type=hidden NAME=companyB VALUE='" +dr["CompanyB"].ToString()+ "'>");
	od += dr["CompanyB"];
	sb.Append("<INPUT type=hidden NAME=address1B VALUE='" +dr["Address1B"].ToString()+ "'>");
    od += dr["Address1B"];
	sb.Append("<INPUT type=hidden NAME=address2B VALUE='" +dr["Address2B"].ToString()+ "'>");
	od += dr["Address2B"];
	sb.Append("<INPUT type=hidden NAME=cityB VALUE='" +dr["CityB"].ToString()+ "'>");
	od += dr["CityB"];
	sb.Append("<INPUT type=hidden NAME=countryB VALUE='" +dr["CountryB"].ToString()+ "'>");
	od += dr["CountryB"];
	if(dr["CountryB"] == null || dr["CountryB"].ToString() == "")
	{
        od += "New Zealand";
	}
	
    //sb.Append("<H3  Style=\"font:bold 18px arail; color:#6699CC; text-align:left\">Order Info</H3><TABLE BORDER=0>\r\n");
    sb.Append("<TABLE BORDER=0 >\r\n");
    if(TS_UserLoggedIn())
	{
		sb.Append("<tr ><td  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">EMail<font color=red>*</font></td><td>");
		sb.Append("<input type=hidden name=email value='" + dr["email"] + "'>");
		sb.Append("<input type=hidden name=email_confirm value='" + dr["email"] + "'>");
		sb.Append(dr["email"]);
		sb.Append("</td></tr>");
	}
	else
	{
		sb.Append("<TR valign=top><TD  valign=top Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">EMail<font color=red>*</font></TD>");
        sb.Append("<TD><INPUT NAME=email VALUE='");
		sb.Append(dr["Email"]);
		sb.Append("' SIZE=30 MAXLENGTH=40></TD></TR>\r\n");
		sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Confirm<font color=red>*</font></TD>");
        sb.Append("<TD><INPUT NAME=email_confirm VALUE='");
        sb.Append(dr["Email"]);
        sb.Append("' SIZE=30 MAXLENGTH=40></TD></TR>");
		sb.Append("<TR><TD>&nbsp;</TD><TD><font size=2><I>(type email address again)</I></font></TD></TR>\r\n");
		sb.Append("<tr><td colspan=2></td></tr>");
        sb.Append("<tr><td  Style='font:bold 13px arail; color:#6699CC; text-align:left'>Password<font color=red>*</font></td>");
        sb.Append("<td><input type=password name=pass SIZE=30></td></tr>");
        sb.Append("<tr><td  Style='font:bold 13px arail; color:#6699CC; text-align:left'>Re-type<font color=red>*</font></td>");
        sb.Append("<td><input type=password name=pass_confirm SIZE=30></td></tr>");
        sb.Append("<tr><td colspan=2></td></tr>");
        sb.Append("<tr><td colspan=2 align=left><font color=red size=2>* fields in red are mandatory</font></td></tr>");
        sb.Append("<input type=checkbox name=remember checked style='visibility:hidden' >");
	}

    /*
    sb.Append("<tr><td colspan=2 >");
	sb.Append("<input type=checkbox> Same as Shipping Address");
	sb.Append("</td></tr>");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Name</TD><TD><INPUT NAME=nameB VALUE='");
	sb.Append(dr["NameB"].ToString());
	od += dr["NameB"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Company</TD><TD><INPUT NAME=companyB VALUE='");
	sb.Append(dr["CompanyB"].ToString());
	od += dr["CompanyB"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Address</TD><TD><INPUT NAME=address1B VALUE='");
	sb.Append(dr["Address1B"].ToString());
	od += dr["Address1B"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">&nbsp;</TD><TD><INPUT NAME=address2B VALUE='");
	sb.Append(dr["Address2B"].ToString());
	od += dr["Address2B"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">City</TD><TD><INPUT NAME=cityB VALUE='");
	sb.Append(dr["CityB"].ToString());
	od += dr["CityB"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD  Style=\"font:bold 13px arail; color:#6699CC; text-align:left\">Country</TD><TD><INPUT NAME=countryB VALUE='");
	sb.Append(dr["CountryB"].ToString());
	od += dr["CountryB"];
	if(dr["CountryB"] == null || dr["CountryB"].ToString() == "")
	{
        od += "New Zealand";
	}
	sb.Append("' SIZE=20 MAXLENGTH=30 ></TD></TR>\r\n");
     */
	sb.Append("</table>");
	sb.Append("<input type=hidden name=old_data value='" + od + "'>");
    
	LFormB.Text = sb.ToString();
	/*
    if(!TS_UserLoggedIn())
	{
		string soption = @"
            <table>
                <tr><td> <fieldset ><legend style='font:bold 15px arial;color:#6699CC'>Login Information</legend><table>
        <tr><td  Style='font:bold 13px arail; color:#6699CC; text-align:left'>password</td><td><input type=password name=pass></td><td  Style='font:bold 13px arail; color:#6699CC; text-align:left'>Re-type:</td>
<td><input type=password name=pass_confirm></td>
<tr><td colspan=2><input type=checkbox name=ads>I wish to receive promotions by email</td></tr>
<tr><td colspan=3><font color=red size=2>* fields in red are mandatory</font></td></tr>
</table></fieldset>
</td></tr>
<tr>
 <td colspan=2 align=center><input type=submit value=Continue name=cmd></td></tr>
</table>

<input type=checkbox name=remember checked style='visibility:hidden' >
		";
		LOptions.Text = soption;
	}
    else 
    {
	    string soption = @"
	        <table width=100%>
	            <tr><td align=center width=100%  ><input type=submit value=Continue name=cmd></td></tr>
		    </table>";
		LOptions.Text = soption;
	 }
     */
     LOptions.Text = "<input type=button OnClick=window.location=('" + Session["item_list_url"] + "'); style=\"background-image:url(pic/login_btn.jpg);border:none;color:#FFFFFF;font-family:Arial, Helvetica, sans-serif; font-weight:bold;font-size:15px;cursor:pointer;\" value='Continue Shopping' name=cmd>";
     LOptions.Text += "&nbsp;&nbsp;&nbsp;<input type=submit style=\"background-image:url(pic/red_btn.jpg);border:none;color:#FFFFFF;font-family:Arial, Helvetica, sans-serif; font-weight:bold;font-size:15px;cursor:pointer;\" value='Place Order' name=cmd>";
}
</script>

<table width=80% cellpadding=0 cellspacing=0 border=0 align=center>
    <tr> 
        <td width="80%" align=center>
       <asp:Label id=LCartInfo runat=server/>
        <br>
            <table width=100% cellpadding=0 cellspacing=0 border=0>
                <tr>
	                <td  valign=top align=center>
                        <form name="flogin" method="post" action="login.aspx" >
                            <asp:Label id=TloginF runat=server/>
                        </form>
                    </td>
	            </tr>
	        </table>
        <br>
            <form action=result.aspx method="post">
            <fieldset>
            <legend style="font:bold 15px arial;color:#6699CC"><asp:Label id=LFormss runat=server/></legend> 
            <table width=100%>
                <tr><td colspan=3 ><br></td></tr>
                <tr>
                    <td><asp:Label id=LFormS runat=server/></td>
	                <td width=10></td>
	                <td valign=top><asp:Label id=LFormB runat=server/></td>
                </tr>
                <tr><td colspan=3 ><br></td></tr>
	            <tr>
	                <td colspan=3><asp:Label id=LOptions runat=server/></td>
	            </tr>
            </table>
            </fieldset>
	        <asp:Label id=Ltb runat=server/>
        <br>
            </FORM>
        </td>
    </tr>
</table>
<asp:Label id=LPaymentOption runat=server/>
<asp:Label id=LFooter runat=server/>
<script runat=server>

bool NewCard(string email, string pass, string type, string name, string short_name, string company,
			string address1, string address2, string city, string country, string nameB, string companyB,
			string address1B, string address2B, string cityB, string countryB, string phone, string fax,
			string contact, string postal1, string postal2, string postal3, string accept_mass_email,
			string shipping_fee, string discount, string credit_limit, int access_level, bool bLogin, string registered)
{
	string stype = GetEnumID("card_type", type);
	return NewCard(email, pass, int.Parse(stype), name, short_name, company,
			address1, address2, city, country, nameB, companyB,
			address1B, address2B, cityB, countryB, phone, fax,
			contact, postal1, postal2, postal3, accept_mass_email,
			shipping_fee, discount, credit_limit, access_level, bLogin, registered);
}


bool NewCard(string email, string pass, int ntype, string name, string short_name, string company,
			string address1, string address2, string city, string country, string nameB, string companyB,
			string address1B, string address2B, string cityB, string countryB, string phone, string fax,
			string contact, string postal1, string postal2, string postal3, string accept_mass_email,
			string shipping_fee, string discount, string credit_limit, int access_level, bool bLogin, string registered)
{
	if(IsDuplicateEmail(email))
	{
		Response.Write("<center><h3>Error, this email address has already been used</h3>");
		return false;
	}

	int id = 0;//GetNextID("card", "id");
	StringBuilder sb = new StringBuilder();

	sb.Append(" BEGIN TRANSACTION ");
	sb.Append(" INSERT INTO card (email, password, type, name, short_name, company, address1, address2, address3, ");
	sb.Append("city, country, nameB, companyB, address1B, address2B, cityB, countryB, phone, fax, ");
	sb.Append("contact, postal1, postal2, postal3, shipping_fee, register_date, accept_mass_email, ");
	sb.Append("discount, credit_limit, access_level, registered) VALUES(" );
//	sb.Append(id);
	sb.Append(" '");
	sb.Append(email);
	sb.Append("', '");
	sb.Append(FormsAuthentication.HashPasswordForStoringInConfigFile(pass, "md5"));
	sb.Append("', ");
	sb.Append(ntype.ToString());
	sb.Append(", '");
	sb.Append(name);
	sb.Append("', '");
	sb.Append(short_name);
	sb.Append("', '");
	sb.Append(company);
	sb.Append("', '");
	sb.Append(address1);
	sb.Append("', '");
	sb.Append(address2);
	sb.Append("', '");
	sb.Append(city);	//as address3
	sb.Append("', '");
	sb.Append(city);
	sb.Append("', '");
	sb.Append(country);
	sb.Append("', N'");
	sb.Append(nameB);
	sb.Append("', '");
	sb.Append(companyB);
	sb.Append("', '");
	sb.Append(address1B);
	sb.Append("', '");
	sb.Append(address2B);
	sb.Append("', '");
	sb.Append(cityB);
	sb.Append("', '");
	sb.Append(countryB);
	sb.Append("', '");
	sb.Append(phone);
	sb.Append("', '");
	sb.Append(fax);
	sb.Append("', '");
	sb.Append(contact);
	sb.Append("', '");
	sb.Append(postal1);
	sb.Append("', '");
	sb.Append(postal2);
	sb.Append("', '");
	sb.Append(postal3);
	sb.Append("', ");
	sb.Append(5);
	sb.Append(", ");
	sb.Append("GETDATE()");
	sb.Append(", ");
	sb.Append(accept_mass_email);
	sb.Append(", ");
	sb.Append(discount);
	sb.Append(", ");
	sb.Append(credit_limit);
	sb.Append(", ");
	sb.Append(access_level);
	sb.Append(", ");
	sb.Append(registered);
	sb.Append(") ");

	sb.Append(" SELECT IDENT_CURRENT('card') AS id ");
	sb.Append(" COMMIT ");

	DataSet dsND = new DataSet();
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sb.ToString(), myConnection);
		if(myCommand.Fill(dsND, "id") == 1)
		{
			id = MyIntParse(dsND.Tables["id"].Rows[0]["id"].ToString());
		}
		else
		{
			ShowExp(sb.ToString(), null);
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	
	string sc = "";
	if(company != "")
	{
		sc = " UPDATE card SET trading_name='" + company + "' WHERE id=" + id;
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
	if(ntype == 0) //personal
	{
		sc = " UPDATE card SET personal_id=" + Session["card_id"].ToString() + " WHERE id=" + id;
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

	Session["new_card_id"] = id;
	if(bLogin)
	{
		//log the basturd in
		TS_LogUserIn();
		Session["name"] = name;
		Session["email"] = email;
		Session["card_id"] = id;
		Session["card_type"] = ntype.ToString();

		UpdateSessionLog();
	}
	return true;
}

bool AddNewDealer(string email, string pass, string trading_name, string corp_number, string directory, string gst_rate, 
				  string currency_for_purchase, string company, string address1, string address2, string address3, string phone, string fax, 
				  string postal1, string postal2, string postal3, string access_level, 
				  string dealer_level, string note, string credit_term, string credit_limit, string approved, bool bLogin, 
				  string purchase_nza, string cat_access, 
				  string name, string pm_email, string pm_ddi, string pm_mobile, 
				  string sm_name, string sm_email, string sm_ddi, string sm_mobile, 
				  string ap_name, string ap_email, string ap_ddi, string ap_mobile, string short_name, string type, string barcode)
{
	if(IsDuplicateEmail(email))
	{
		Response.Write("<center><h3>Error, this email address has already been used</h3>");
		return false;
	}

	string id = "";
	if(type == null || type == "")
		type = "1";

	string sc = " BEGIN TRANSACTION ";
	sc += " INSERT INTO card (email, ";
	if(pass != "") //
		sc += "password, ";
	sc += "type, company, trading_name, corp_number, directory, gst_rate, currency_for_purchase, ";
	sc += " address1, address2, address3, barcode, postal1, postal2, postal3, phone, fax, access_level, "; 
	sc += "	dealer_level, note, credit_term, credit_limit, approved, ";
	sc += " purchase_nza, cat_access, short_name, ";
	sc += " name, pm_email, pm_ddi, pm_mobile, sm_name, sm_email, sm_ddi, sm_mobile, ap_name, ap_email, ap_ddi, ap_mobile) ";
	sc += " VALUES('" + email + "', ";
	if(pass != "")
		sc += "'" + pass + "', ";
	sc += type + ", ";
	sc += "'" + company + "', '" + trading_name + "', '" + corp_number + "', " + directory;
	sc += ", " + gst_rate + ", " + currency_for_purchase + ", '" + address1 + "', '" + address2 + "', '" + address3 + "', '";
	sc += barcode + "', '";
	sc += postal1 + "', '" + postal2 + "', '" + postal3 + "', '" + phone;
	sc += "', '" + fax + "', " + access_level + ", " + dealer_level + ", '" + note + "', ";
	sc += credit_term + ", " + credit_limit + ", " + approved + ", ";
	sc += purchase_nza + ", '" + cat_access + "', '" + short_name + "', '";
	sc += name + "', '" + pm_email + "', '" + pm_ddi + "', '" + pm_mobile + "', '";
	sc += sm_name + "', '" + sm_email + "', '" + sm_ddi + "', '" + sm_mobile + "', '";
	sc += ap_name + "', '" + ap_email + "', '" + ap_ddi + "', '" + ap_mobile + "')";
	
	sc += " SELECT IDENT_CURRENT('card') AS id";
	sc += " COMMIT ";

	DataSet dsND = new DataSet();
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsND, "id") == 1)
		{
			id = dsND.Tables["id"].Rows[0]["id"].ToString();
//DEBUG("id=", m_topicid);
		}
		else
		{
			ShowExp(sc, null);
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(trading_name == "" && company != "")
	{
		sc = " UPDATE card SET trading_name='" + company + "' WHERE id=" + id;
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
	if(type == "0") //personal
	{
		sc = " UPDATE card SET personal_id=" + Session["card_id"].ToString() + " WHERE id=" + id;
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

	Session["new_card_id"] = id;

	if(bLogin)
		DoSessionLogin(company, email, id, type, dealer_level, access_level);
	return true;
}

bool IsDuplicateEmail(string email)
{
	DataSet dsa = new DataSet();
	int rows = 0;
	string sc = "SELECT * FROM card WHERE email='" + email + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsa, "card") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return true;
	}
	return true;
}

double GetGstRate(string id)
{
	DataRow dr = GetCardData(id);
	if(dr != null)
		return MyDoubleParse(dr["gst_rate"].ToString());
	return 0.15;
}

bool UpdateDiscount(string id, string sTotal)	//HG 13.Aug.2002
{
//	string sTotal = dst.Tables["invoice"].Rows[0]["total"].ToString();
//	string email = dst.Tables["invoice"].Rows[0]["email"].ToString();
//	string card_id = dst.Tables["invoice"].Rows[0]["card_id"].ToString();
	if(id == "")
		return true; //card not found

	double dDiscount = 0;
	double dDiscountUp =0;

	dDiscount = GetUserDiscount(id);
//DEBUG("discount=", dDiscount.ToString());
	if(dDiscount < 0)
		return true; //DW changed to return true, in case didn't find user

	dDiscountUp = double.Parse(sTotal)/100;

	if ((dDiscount + dDiscountUp) > 100)
		 dDiscountUp = 100 - dDiscount;
	dDiscount += dDiscountUp;

	return UpdateDiscount(id, dDiscount);
}

double GetUserDiscount(string id)
{
	DataSet dsud = new DataSet();
	int rows = 0;
	string sc = "SELECT ISNULL(discount, 0) AS discount FROM card WHERE id=" + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsud, "discount");
//DEBUG("rows=", rows);
		if(rows <= 0)
			return -1; //card not found
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return -1;
	}
	return double.Parse(dsud.Tables["discount"].Rows[0]["discount"].ToString());
}

void DoSessionLogin(string name, string email, string id, string type, string dealer_level, string access_level)
{
	//log the basturd in
	TS_LogUserIn();
	Session["name"] = name;
	Session["email"] = email;
	Session["card_id"] = id;
	Session["card_type"] = type;
	Session[m_sCompanyName + "dealer_level"] = dealer_level;
	Session[m_sCompanyName + "access_level"] = access_level;
//	Session[m_sCompanyName + "discount"] = "0";

	UpdateSessionLog();
	return;
}

Boolean DrawCardTable(DataRow dr, bool bNew, string newType)
{
	Boolean bRet = true;

//	int acc_count = 0;
	string id = "";
	string branch_id = "";
	string email = "";
	string type = "2";
	string password = "";
	string name = "";
	string trading_name = "";
	string corp_number = "";
	string barcode = "";
	string directory = "1"; //chinese, OEM etc..
	string short_name = "";
	string gst_rate = (MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100).ToString();				// 30.JUN.2003 XW
//	string gst_rate = "";				// 30.JUN.2003 XW
	string currency_for_purchase = "1";
	string company = "";
	string address1 = "";
	string address2 = "";
	string address3 = "";
	string phone = "";
	string fax = "";
	string contact = "";
	string postal1 = "";
	string postal2 = "";
	string postal3 = "";
	string dealer_level = "1";
	string discount = "0";
	string purchase_nza = "0";
	string purchase_average = "0";
	string credit_term = "0";
	string credit_limit = "0";
	string access_level = "0";
	string note = "";
	string salesemail = "";
	string accountemail = "";
	bool bApproved = true;
	bool bStopOrder = false;
	string stop_reason = "";
	bool bNoSysQuote = false;

	string pm_email = "";
	string pm_ddi = "";
	string pm_mobile = "";
	string sm_name = ""; //sales manager
	string sm_email = "";
	string sm_ddi = "";
	string sm_mobile = "";
	string ap_name = ""; //account payable
	string ap_email = "";
	string ap_ddi = "";
	string ap_mobile = "";

	string cat_access = "all";
	string cat_access_group = "0";

	string sales = "";
    int cr_limit = int.Parse(GetSiteSettings("edit_credit_limit"));
	double dGstRate = 0;
    bool cr_allow = true;
	if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString())< cr_limit)
	     cr_allow = false;
	bool bIsBranch = false;
	if(dr != null)
	{
//		acc_count = drst.Tables[0].Rows.Count;
		id = dr["id"].ToString();
		if(m_sSite == "admin")
			branch_id = dr["our_branch"].ToString();
		//branch_id =Session["branch_id"].ToString();
		email = dr["email"].ToString();
		type = dr["type"].ToString();
		if(type == "")
			type = GetEnumID("card_type", "dealer");
//		password = dr["password"].ToString();
		name = dr["name"].ToString();
		trading_name = dr["trading_name"].ToString();
		corp_number = dr["corp_number"].ToString();
		barcode = dr["barcode"].ToString();
		directory = dr["directory"].ToString();
		short_name = dr["short_name"].ToString();
		gst_rate = dr["gst_rate"].ToString();
		dGstRate = MyDoubleParse(gst_rate);
		currency_for_purchase = dr["currency_for_purchase"].ToString();
		company = dr["company"].ToString();
		address1 = dr["address1"].ToString();
		address2 = dr["address2"].ToString();
		address3 = dr["address3"].ToString();
		phone = dr["phone"].ToString();
		fax = dr["fax"].ToString();

	//decode the apostrophe to display in text box
		name = name.Replace("'","&#39;");
		email = email.Replace("'","&#39;");
		trading_name = trading_name.Replace("'","&#39;");
		short_name = short_name.Replace("'","&#39;");
		company = company.Replace("'","&#39;");
		phone = phone.Replace("'","&#39;");
		fax = fax.Replace("'","&#39;");

		postal1 = dr["postal1"].ToString();
		postal2 = dr["postal2"].ToString();
		postal3 = dr["postal3"].ToString();
		dealer_level = dr["dealer_level"].ToString();
//		discount = dr["discount"].ToString();
		purchase_nza = dr["purchase_nza"].ToString();
		purchase_average = dr["purchase_average"].ToString();
		credit_term = dr["credit_term"].ToString();
		credit_limit = dr["credit_limit"].ToString();
		access_level = dr["access_level"].ToString();
		note = dr["note"].ToString();
		pm_email = dr["pm_email"].ToString();
		pm_ddi = dr["pm_ddi"].ToString();
		pm_mobile = dr["pm_mobile"].ToString();
		sm_name = dr["sm_name"].ToString();
		sm_email = dr["sm_email"].ToString();
		sm_ddi = dr["sm_ddi"].ToString();
		sm_mobile = dr["sm_mobile"].ToString();
		ap_name = dr["ap_name"].ToString();
		ap_email = dr["ap_email"].ToString();
		ap_ddi = dr["ap_ddi"].ToString();
		ap_mobile = dr["ap_mobile"].ToString();
		bApproved = MyBooleanParse(dr["approved"].ToString());
		bStopOrder = MyBooleanParse(dr["stop_order"].ToString());
		bNoSysQuote = MyBooleanParse(dr["no_sys_quote"].ToString());
		stop_reason = dr["stop_order_reason"].ToString();
		cat_access = dr["cat_access"].ToString();
		cat_access_group = dr["cat_access_group"].ToString();
		sales = dr["sales"].ToString();
		bIsBranch = false;//MyBooleanParse(dr["is_branch"].ToString());
	}
	else
	{
		switch(newType)
		{
			case "supplier":
				type = "3";
				break;
			case "employee":
				type = "4";
				break;
			case "dealer":
				type = "2";
				break;
			case "customer":
				type = "1";
				break;
			case "member":
				type = "5";
				break;
			case "branch_login":
				type = "4";
				bIsBranch = true;
				break;
			default:
				type = "1";
				break;
		}
	}

	string sType = "CUSTOMER";
	if(type != null && type != "")
		sType = GetEnumValue("card_type", type).ToUpper();
	if(bIsBranch)
		sType = Lang("Branch Login Account");
	Response.Write("<br><center><h3>");
	if(!bNew)
	{
		if(m_sSite == "admin" && Request.QueryString["v"] == "view")
			Response.Write("View Card Details");
		else if(m_sSite == "admin")
			Response.Write("Edit " + sType);
		else
			Response.Write("Update Account Details");
	}
	else
	{
		if(m_sSite == "admin")
		{
			Response.Write("New ");
			if(newType != "")
				Response.Write(newType.ToUpper());
			else
				Response.Write("Card");
		}
		else
			Response.Write("Dealer Application Form");
	}
	Response.Write("</h3>");

	Response.Write("<input type=hidden name=id value='" + id + "'>");
	Response.Write("<input type=hidden name=email_old value='" + email + "'>");

	Response.Write("<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#AAAAAA bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	if( (bNew && newType == "")
		|| (bNew && sType != "DEALER" && sType != "SUPPLIER" && sType != "AGENT" && sType != "OTHERS")
		|| (m_sSite == "admin" && !bNew))
	{
		Response.Write("<tr><td><table width=100%><tr><td align=left style=\"font:bold 14px arial; color:#000000\">"+sType+ " ID: " +id+"</td><td align=right><b>CARD TYPE : </b><select name=type>");
		Response.Write(GetEnumOptions("card_type", type));
		Response.Write("</select></td></tr></table></td></tr>");
	}
	else
	{
		type = GetEnumID("card_type", newType);
		Response.Write("<input type=hidden name=type value=" + type + ">");
	}

	if(Request.QueryString["luri"] != null && Request.QueryString["luri"] != "")
		Session["last_uri_exp"] = Request.QueryString["luri"].ToString();
	if(m_sSite == "admin")
	{
		Response.Write("<tr><td  valign=top>");
		Response.Write("<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#AAAAAA bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	}			
	if(m_sSite == "admin" && Session["branch_support"] != null)
	{
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<font color=red>Branch</font></td><td>");
		if(bIsBranch)
			Response.Write(PrintBranchOptionsForBranchLogin(branch_id));
		else
			Response.Write(PrintBranchOptions(branch_id));
//		Response.Write(" <a href=branch.aspx class=o>Branch List</a>");
		if(type == "4" || bIsBranch)
		{
			Response.Write("<input type=checkbox name=is_branch value=1 ");
			if(bIsBranch)
				Response.Write(" checked ");
			Response.Write(">" + Lang("Branch Login Account") + "");
		}
		Response.Write("</td></tr>");
	}

	if(newType != "employee" && sType !="EMPLOYEE" && !bIsBranch)
	{
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<font color=red>Company Legal Name</font></td><td><input type=text size=55 name=company maxlength=50 value='" + company + "' onkeyDown=\"this.style.backgroundColor='#FFFFFF'\"></td></tr>");
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<font color=red>Company Trading Name</font></td><td><input type=text size=55 name=trading_name maxlength=50 value='" + trading_name + "'  onkeyDown=\"this.style.backgroundColor='#FFFFFF'\"></td></tr>");
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("Incorporation Number</td><td><input type=text size=55 name=corp_number maxlength=50 value='" + corp_number + "'></td></tr>");
	}

	string type_supplier = GetEnumID("card_type", "supplier");
	if(newType == "supplier" || sType == "SUPPLIER" || newType == "others" || sType == "OTHERS") //card type
	{
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<font color=red>Supplier Short Name</font></td><td><input type=text size=5 name=short_name maxlength=5 value='");
		Response.Write(short_name + "'>(2-5 characters, unique)");
		Response.Write("</td></tr>");

		Response.Write("<tr><td bgcolor=#EEEEEE align=center>");
		Response.Write("Contact Email</td><td><input type=text size=55 name=email maxlength=50  onkeyDown=\"this.style.backgroundColor='#FFFFFF'\" value='");
		Response.Write(email);
		Response.Write("'>");
	}
	else
	{ 
		string eTitle ="Manager Email";
		string  sName ="Manager Name";
		if(newType == "employee" || sType =="EMPLOYEE" || bIsBranch)
		{
			eTitle = "Login Email";
			sName =" Name";
		}
		if (newType =="customer" || sType =="CUSTOMER")
		{
			eTitle =" Customer Email ";
			sName =" Customer Name ";
		}
		  
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<font color=red>"+ sName +"</font></td>");
		Response.Write ("<td><input type=text size=55 name=name onkeyDown=\"this.style.backgroundColor='#FFFFFF'\" maxlength=50 ");
		if(bIsBranch)
			Response.Write(" readonly=true style='background-color:#EEEEEE;' ");
		Response.Write(" value='" + name + "'></td></tr>");
		Response.Write("<tr><td bgcolor=#EEEEEE align=center>");
		Response.Write("<font color=red>"+ eTitle +"</font>");
		 
		Response.Write("<br><font size=-3><i>(*This will be your login Email<br> and where we send you password)</i></font></td>");
		Response.Write("<td><input type=text size=55 name=email maxlength=50  onkeyDown=\"this.style.backgroundColor='#FFFFFF'\"");
		if(m_sSite != "admin" && !bNew)
			Response.Write(" readonly=true ");
		Response.Write(" value='");
		Response.Write(email + "'");
//		if(!bNew)
//			Response.Write(" readonly=true");
		Response.Write(">");
	
	}
	Response.Write("</td></tr>");
    if(newType == "employee" || sType =="EMPLOYEE" || bIsBranch)
	{
		Response.Write("<tr><td bgcolor=#eeeeee align=center>");
		Response.Write("<font color=red>Sales Barcode</font></td><td><input type=text size=45 name=barcode maxlength=50 value='");
		Response.Write(barcode + "'>");
		if(!bNew)
			Response.Write("<input type=submit " + Session["button_style"].ToString() + " name=cmd value='Update'></td></tr>");
    }
	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("<font color=red>Physical Address</font></td><td><input type=text size=55 name=address1 maxlength=50  onkeyDown=\"this.style.backgroundColor='#FFFFFF'\" value='");
	Response.Write(address1 + "'></td></tr>");

	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("(line 2)</td><td><input type=text size=55 name=address2 maxlength=50 value='");
	Response.Write(address2 + "'></td></tr>");

	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("(line 3)</td><td><input type=text size=55 name=address3 maxlength=50 value='");
	Response.Write(address3 + "'></td></tr>");

	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("<font color=red>Phone Number</font></td><td><input type=text size=55 name=phone maxlength=50   onkeyDown=\"this.style.backgroundColor='#FFFFFF'\" value='");
	Response.Write(phone);
	Response.Write("'>");

	Response.Write("</td></tr><tr><td bgcolor=#eeeeee align=center>");
	if(newType =="employee" || sType=="EMPLOYEE")
	{
		Response.Write("<font color=red>Mobile:</font></td><td>");
	}
	else
	{
		Response.Write("<font color=red>Fax Number</font></td><td>");
	}
	Response.Write ("<input type=text size=55   onkeyDown=\"this.style.backgroundColor='#FFFFFF'\" name=Fax maxlength=50 value='");
	Response.Write(fax + "'></td></tr>");

	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("Postal Address</td><td><input type=text size=55 name=postal1 maxlength=50 value='");
	Response.Write(postal1 + "'></td></tr>");

	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("(line 2)</td><td><input type=text size=55 name=postal2 maxlength=50 value='");
	Response.Write(postal2 + "'></td></tr>");

	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("(line 3)</td><td><input type=text size=55 name=postal3 maxlength=50 value='");
	Response.Write(postal3 + "'></td></tr>");

	double dGSTRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;				// 30.JUN.2003 XW

	if(newType == "supplier" || sType == "SUPPLIER" || newType == "others" || sType == "OTHERS") //card type
	{
		Response.Write("<input type=hidden name=purchase_average value=0>");
		Response.Write("<input type=hidden name=purchase_nza value=0>");
		Response.Write("<input type=hidden name=credit_term value=0>");
		Response.Write("<input type=hidden name=credit_limit value=0>");
		Response.Write("<input type=hidden name=dealer_level value=1>");
		Response.Write("<input type=hidden name=rights value=1>");
		Response.Write("<input type=hidden name=directory value=1>");	
		Response.Write("<input type=hidden name=approved value=1>");
		Response.Write("<tr><td bgcolor=#eeeeee>GST Rate</td><td><select name=gst_rate>");

		Response.Write("<option value=0.1");
		if(gst_rate == "0.1")
			Response.Write(" selected");
		Response.Write(">10%");
		Response.Write("</option>");
		Response.Write("<option value=0.15");
		if(gst_rate == "0.15")
			Response.Write(" selected");
		Response.Write(">15%");
		Response.Write("</option>");
		Response.Write("<option value=0");
		if(gst_rate == "0")
			Response.Write(" selected");
		Response.Write(">0.00%");
		Response.Write("</option>");
		Response.Write("</select>");				
		Response.Write("</td></tr>");
		Response.Write("<tr><td bgcolor=#eeeeee>Currency For Purchase</td><td><select name=currency_for_purchase>");
		Response.Write(PrintCurrencyOptions(false, currency_for_purchase));	 //false no rates, only ID
		Response.Write("</select></td></tr>");
	}
	else
	{
		Response.Write("<input type=hidden name=currency_for_purchase value=1>");
		if(newType !="employee" && sType !="EMPLOYEE" && newType !="customer" && sType !="CUSTOMER" && newType !="otherse" && sType !="OTHERS")
		{
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Purchase Manager DDI </td><td><input type=text size=55 name=pm_ddi maxlength=50 value='");
			Response.Write(pm_ddi + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Purchase Manager Mobile</td><td><input type=text size=55 name=pm_mobile maxlength=50 value='");
			Response.Write(pm_mobile + "'></td></tr>");

			//sales part
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Sales Manager</td><td><input type=text size=55 name=sm_name maxlength=50 value='");
			Response.Write(sm_name + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Sales Manager Email</td><td><input type=text size=55 name=sm_email maxlength=50 value='");
			Response.Write(sm_email + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Sales Manager DDI</td><td><input type=text size=55 name=sm_ddi maxlength=50 value='");
			Response.Write(sm_ddi + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Sales Manager Mobile</td><td><input type=text size=55 name=sm_mobile maxlength=50 value='");
			Response.Write(sm_mobile + "'></td></tr>");
			
			//account part
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Account Payable</td><td><input type=text size=55 name=ap_name maxlength=50 value='");
			Response.Write(ap_name + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Account Payable Email</td><td><input type=text size=55 name=ap_email maxlength=50 value='");
			Response.Write(ap_email + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Account Payable DDI</td><td><input type=text size=55 name=ap_ddi maxlength=50 value='");
			Response.Write(ap_ddi + "'></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Account Payable Mobile</td><td><input type=text size=55 name=ap_mobile maxlength=50 value='");
			Response.Write(ap_mobile + "'></td></tr>");
			if(m_sSite == "admin")
			{
			  Response.Write("</table></td>");
			  Response.Write("<td valign=top>");
			  Response.Write("<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#AAAAAA bgcolor=white");
			  Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");		
			}
		}
		/////////////////////////////
		if (newType == "employee" || sType =="EMPLOYEE")
		{
			Response.Write ("<input type=hidden name=company value="+GetSiteSettings("company_name")+" >");
			Response.Write ("<input type=hidden name=trading_name value='"+GetAccessClassName(access_level) +" ' >");
			Response.Write( "<input type=hidden name=directory value=1 >");
			Response.Write ("<input type=hidden name=gst_rate value=0.15 >");
			Response.Write("<input type=hidden name=purchase_nza value=0 >");
			Response.Write("<input type=hidden name=purchase_average value=0 >");
			Response.Write ("<input type=hidden name=dealer_level value=1 >");
			Response.Write ("<input type=hidden name=credit_term value=0 >");
			Response.Write ("<input type=hidden name=cat_access_group value=0 >");
			Response.Write ("<input type=hidden name=cat_access value=all >");
			Response.Write ("<input type=hidden name=cat_access_old value=all >");
			Response.Write ("<input type=hidden name=no_sys_quote value=  >");


			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Access Level</td><td><select name=rights>");

			string sop = GetAccessClassOptions(access_level);

			if(sType != "EMPLOYEE")
				access_level = "1"; //no access
			if(Session[m_sCompanyName + "AccessLevel"].ToString() == GetAccessClassID("Administrator") && sType == "EMPLOYEE")
			{
				Response.Write(sop);
			}
			else
			{
				Response.Write("<option value='" + access_level + "'>" + GetAccessClassName(access_level) + "</option>"); //only administrator can change access level
			}
			Response.Write("</select>");
			Response.Write("</td></tr>");
			
				if(bApproved)
			{
				Response.Write("<tr><td bgcolor=#eeeeee align=center>Approved</td><td><input type=checkbox name=approved");
				if(bApproved)
					Response.Write(" checked");
				Response.Write("></td></tr>");
			}
			else{
				Response.Write("<input type=hidden name=approved value=0>");
				}
	     }
		 	
		else {
		
		 if (newType == "customer" || sType =="CUSTOMER")
		 {
			Response.Write( "<input type=hidden name=directory value=1 >");
			Response.Write("<input type=hidden name=purchase_nza value=0 >");
			Response.Write("<input type=hidden name=purchase_average value=0 >");
			Response.Write ("<input type=hidden name=dealer_level value=1 >");
			Response.Write ("<input type=hidden name=credit_term value=0 >"); 
			Response.Write ("<input type=hidden name=cat_access value=all >");
			Response.Write ("<input type=hidden name=cat_access_old value=all >");
			Response.Write ("<input type=hidden name=no_sys_quote value=  >");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>GST Rate</td><td><select name=gst_rate>");
			Response.Write("<option value=0.1");
			if(gst_rate == "0.1")				// 03.July.2003 XW
				Response.Write(" selected");				// 03.July.2003 XW
			Response.Write(">10%");
			Response.Write("</option>");
			Response.Write("<option value=0.15");
			if(gst_rate == "0.15")
				Response.Write(" selected");
			Response.Write(">15%");
			Response.Write("</option>");
			Response.Write("<option value=0");
			if(gst_rate == "0")
				Response.Write(" selected");
			Response.Write(">0.00%");
			Response.Write("</option>");
			Response.Write("</select>");				

			Response.Write("</td></tr>");
				Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Access Level</td><td><select name=rights>");

			string sop = GetAccessClassOptions(access_level);

			if(sType != "EMPLOYEE")
				access_level = "1"; //no access
			if(Session[m_sCompanyName + "AccessLevel"].ToString() == GetAccessClassID("Administrator") && sType == "EMPLOYEE")
			{
				Response.Write(sop);
			}
			else
			{
				Response.Write("<option value='" + access_level + "'>" + GetAccessClassName(access_level) + "</option>"); //only administrator can change access level
			}
			Response.Write("</select>");
			Response.Write("</td></tr>");
				if(bApproved)
			{
				Response.Write("<tr><td bgcolor=#eeeee align=center> Approved</td><td><input type=checkbox name=approved");
				if(bApproved)
					Response.Write(" checked");
				Response.Write("></td></tr>");
			}
			else
				Response.Write("<input type=hidden name=approved value=0>");
			}
		
		else 
		{
		if(m_sSite == "admin")
		{
			Response.Write("</table></td>");
			Response.Write("<td valign=top>");
			Response.Write("<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#AAAAAA bgcolor=white");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");		 
				
			Response.Write("<tr ><td bgcolor=#eeeeee align=center>");
			Response.Write("Directory</td><td><select name=directory>");
			Response.Write(GetEnumOptions("card_dir", directory) + "</select></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>GST Rate</td><td><select name=gst_rate>");

			Response.Write("<option value=0.1");
			if(gst_rate == "0.1")				// 03.July.2003 XW
				Response.Write(" selected");				// 03.July.2003 XW
			Response.Write(">10%");
			Response.Write("</option>");
			Response.Write("<option value=0.15");
			if(gst_rate == "0.15")
				Response.Write(" selected");
			Response.Write(">15%");
			Response.Write("</option>");
			Response.Write("<option value=0");
			if(gst_rate == "0")
				Response.Write(" selected");
			Response.Write(">0.00%");
			Response.Write("</option>");
			Response.Write("</select>");				

			Response.Write("</td></tr>");

			//purchase NZA
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Purchase NZA</td><td>");
			Response.Write("<input type=text name=purchase_nza value=" + purchase_nza + ">");
			Response.Write("<input type=hidden name=purchase_nza_old value=" + purchase_nza + ">");
			Response.Write("</td></tr>");

			//purchase average
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Purchase Average</td><td>");
			Response.Write("<input type=text name=purchase_average value=" + purchase_average + ">");
			Response.Write("<input type=hidden name=purchase_average_old value=" + purchase_average + ">");
			Response.Write("</td></tr>");

			//customer levels
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Dealer Level</td><td><select name=dealer_level");
			  if(!cr_allow)
			Response.Write (" onClick=\"window.alert('Sorry, You are not allow to edit credit limit,Please contact your manager')\" ");
			Response.Write ("  >");
			  if(cr_allow)
			PrintDealerLevelOptions(dealer_level.ToString());
			Response.Write("</select>");
			  if(cr_allow)
			Response.Write("<a href=levels.aspx?ci=" + id + " class=o>More</a></td></tr>");
          
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Credit Term</td><td>");
		
			Response.Write("<select name=credit_term ");
			  if(!cr_allow)
			Response.Write ("onClick=\"window.alert('Sorry, You are not allow to edit credit limit,Please contact your manager')\" ");
			Response.Write ("  >");
			  if(cr_allow)
			Response.Write(GetEnumOptions("credit_terms", credit_term));
			Response.Write("</select>");
			Response.Write("</td></tr><tr><td bgcolor=#eeeeee align=center> ");
		   
			Response.Write("Credit Limit</td><td><input type=text size=55 name=credit_limit maxlength=8 value='");
			Response.Write(credit_limit);
			Response.Write("' ");
			if(!cr_allow){
			Response.Write(" readonly" );
			Response.Write (" onClick=\"window.alert('Sorry, You are not allow to edit credit limit,");
			Response.Write( " Please contact your manager')\" ");
			}
			Response.Write (" >");

			Response.Write("</td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Access Level</td><td><select name=rights>");

			string sop = GetAccessClassOptions(access_level);

			if(sType != "EMPLOYEE")
				access_level = "1"; //no access
			if(Session[m_sCompanyName + "AccessLevel"].ToString() == GetAccessClassID("Administrator") && sType == "EMPLOYEE")
			{
				Response.Write(sop);
			}
			else
			{
				Response.Write("<option value='" + access_level + "'>" + GetAccessClassName(access_level) + "</option>"); //only administrator can change access level
			}
			Response.Write("</select>");
			Response.Write("</td></tr>");

			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("Dealer View Group</td><td>");
			Response.Write(PrintCatAccessGroup(cat_access_group));
			Response.Write(" <a href=vlimit.aspx class=o target=_blank>Edit Groups</a>");
			Response.Write("</td></tr>");

			//catlog access
			Response.Write("<input type=hidden name=cat_access size=55 value='" + cat_access + "'>");
			Response.Write("<input type=hidden name=cat_access_old value='" + cat_access + "'>");

			//system quotation access
			Response.Write("<tr><td bgcolor=#eeeeee align=center>");
			Response.Write("No System Quotation</td><td>");
			Response.Write("<input type=checkbox name=no_sys_quote");
			if(bNoSysQuote)
				Response.Write(" checked");
			Response.Write("></td></tr>");

			if(bApproved)
			{
				Response.Write("<tr><td bgcolor=#eeeee align=center> Approved</td><td><input type=checkbox name=approved");
				if(bApproved)
					Response.Write(" checked");
				Response.Write("></td></tr>");
			}
			else
				Response.Write("<input type=hidden name=approved value=0>");

			//stop placing order
			Response.Write("<tr><td bgcolor=#eeeeee align=center>Stop Placing Order</td><td><input type=checkbox name=stop_order");
			if(bStopOrder)
				Response.Write(" checked");
			Response.Write("></td></tr>");
			Response.Write("<tr><td bgcolor=#eeeeee align=center>Stop Reason(say sth to him)</td>");
			Response.Write("<td><input type=text name=stop_order_reason size=55 maxlength=50 value='" + stop_reason + "'></td></tr>");

			Response.Write("<tr><td bgcolor=#eeeeee align=center>Sales Person</td>");
			Response.Write("<td>");
			Response.Write(PrintSalesOptions(sales));
			Response.Write("</td></tr>");

		}
		else //www site register
		{
			Response.Write("<input type=hidden name=directory value=" + directory + ">");
			Response.Write("<input type=hidden name=gst_rate value=" + gst_rate + ">");

			if(!TS_UserLoggedIn())
			{
				Response.Write("<tr><td colspan=2><input type=checkbox name=agree_terms> ");
				Response.Write("I have read, understood and accept the Terms and Conditions of Trade of " + GetSiteSettings("company_name") + " </td></tr>");
				Response.Write("<tr><td colspan=2><input type=checkbox name=disclose> ");
				Response.Write("I agree that I will not disclose my login details to anyone.</td></tr>");
			}
			else
			{
				Response.Write("<input type=hidden name=agree_terms value=on> ");
				Response.Write("<input type=hidden name=disclose value=on> ");
			}
		}
	}
}
}
	Response.Write("<tr><td bgcolor=#eeeeee align=center>");
	Response.Write("Note</td><td><textarea name=note rows=5 cols=55>");
		//<input type=text size=55 name=note maxlength=50 value=\"");
	Response.Write(note);
	Response.Write("</textarea>");
	Response.Write("</td></tr>");
	Response.Write("</table></td>");
	Response.Write("<tr><td colspan=3 align=right>");

	if(bNew)
	{
		Response.Write("<input type=submit " + Session["button_style"].ToString() + " name=cmd value='Submit'>");
		if(Session["last_uri_exp"] != null && Session["last_uri_exp"] != "")
			Response.Write("<input type=button " + Session["button_style"].ToString() + " name=cmd value='<< Back to Expense' Onclick=\"window.location=('"+ Session["last_uri_exp"] +"')\">");
	}
	else
	{
		if(TS_UserLoggedIn())
		{
			if(SecurityCheck("manager", false))
			{
				if(m_sSite == "admin" && Request.QueryString["v"] == "view")
					Response.Write(" ");
				else if(m_sSite == "admin")
				{
					if(!bApproved)
						Response.Write("<input type=submit " + Session["button_style"].ToString() + " name=cmd value='Approve'>&nbsp&nbsp;");
					else
						Response.Write("<input type=submit " + Session["button_style"].ToString() + " name=cmd value='Send Password'>&nbsp&nbsp;");
					
					if(Session["last_uri_exp"] != null && Session["last_uri_exp"] != "")
						Response.Write("<input type=button " + Session["button_style"].ToString() + " name=cmd value='<< Back to Expense' Onclick=\"window.location=('"+ Session["last_uri_exp"] +"')\">");

					if(id != "0")
					{
					Response.Write("<input type=checkbox name=delete> Delete this card ");
					Response.Write("<input type=submit " + Session["button_style"].ToString() + " name=cmd value='Delete'>&nbsp;&nbsp;");
					}
				}
			}
			if(m_sSite == "www")
			{
				Response.Write("<input type=button " + Session["button_style"].ToString() + " onclick=window.location=('setpwd.aspx') value='Change Password'>");
			}
		}
		if(Request.QueryString["v"] == "view")
			Response.Write("<input type=button " + Session["button_style"].ToString() + " value='Close' onclick='window.close();'></td></tr>");
		else
			Response.Write("<input type=submit " + Session["button_style"].ToString() + " name=cmd value='Update'></td></tr>");
	}

	Response.Write("</td></tr>");
	Response.Write("</table>");
	return bRet;
}

bool PrintDealerLevelOptions(string current_level)
{
	int levels = MyIntParse(GetSiteSettings("dealer_levels", "3"));
	if(levels <= 0)
		levels = 1;
	if(levels > 9)
		levels = 9;
	for(int i=1; i<=levels; i++)
	{
		Response.Write("<option value=" + i.ToString());
		if(current_level == i.ToString())
			Response.Write(" selected");
		Response.Write(">");
		Response.Write("Level " + i.ToString());
		Response.Write("</option>");
	}
	return true;
}

bool UpdateCardBalance(string id, double dBalanceAdd)
{
	string sc = "UPDATE card SET balance=balance+" + dBalanceAdd;
	sc += " WHERE id=" + id;
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

bool UpdateCardAverage(string id, double amount, int month)
{
	int working_on = 0;

	DataSet dsf = new DataSet();
	string sc = "SELECT working_on FROM card WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsf, "working_on") == 1)
			working_on = MyIntParse(dsf.Tables["working_on"].Rows[0]["working_on"].ToString());
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	//reset next month
	if(working_on != month)
	{
		sc = "UPDATE card SET working_on=" + month;
		int start = working_on + 1;
		if(start > 12)
			start = 1;
		int i = 0;
		if(working_on < month)
		{
			for(i=start; i<month; i++)
			{
				sc += ", m" + i.ToString() + "=0 "; //no purchase in these months, reset to zero
			}
		}
		else if(!(working_on == 12 && month == 1) ) //from December to Jaunary is continued, no reset
		{
			for(i=start; i<=12; i++)
			{
				sc += ", m" + i.ToString() + "=0 "; //no purchase in these months, reset to zero
			}
			for(i=1; i<start; i++)
			{
				sc += ", m" + i.ToString() + "=0 "; //no purchase in these months, reset to zero
			}
		}
		sc += " WHERE id=" + id;
	}

	//top up current
	sc = " UPDATE card SET m" + month + "=m" + month + "+" + amount + " WHERE id=" + id;
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

	if(working_on != month) //update average
	{
		sc += " SELECT m1, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12 FROM card WHERE id=" + id;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dsf, "average") <= 0)
				return false;
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		int months = 0;
		double dTotal = 0;
		double dMonth = 0;
		DataRow dr = dsf.Tables["average"].Rows[0];
		for(int i=1; i<=12; i++)
		{
			if(i == month)
				continue;
			
			dMonth = MyDoubleParse(dr["m" + i.ToString()].ToString());
			if(dMonth > 0)
			{
				 months++;
				 dTotal += dMonth;
			}
		}
		double dAverage = 0;
		if(months > 0)
			dAverage = dTotal / months;
		dAverage = Math.Round(dAverage, 2);
		sc = "UPDATE card SET purchase_average=" + dAverage + " WHERE id=" + id;
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

string PrintSalesOptions(string current_sales)
{
//DEBUG("id=", current_sales);
	DataSet dscf = new DataSet();
	string sc = " SELECT name, id FROM card WHERE type=4 ORDER BY name";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dscf, "sales");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
		
	string s = "<select name=sales><option value=null></option>";
	for(int i=0; i<dscf.Tables["sales"].Rows.Count; i++)
	{
		DataRow dr = dscf.Tables["sales"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		s += "<option value=" + id;
		if(id == current_sales)
			s += " selected";
		s += ">" + name + "</option>";
	}
	s += "</select>";
	return s;
}

string PrintCatAccessGroup(string current_group_id)
{
	DataSet dscf = new DataSet();
	string sc = " SELECT * FROM view_limit ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dscf, "groups");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
		
	string s = "<select name=cat_access_group><option value=0>No Limit</option>";
	for(int i=0; i<dscf.Tables["groups"].Rows.Count; i++)
	{
		DataRow dr = dscf.Tables["groups"].Rows[i];
		string id = dr["id"].ToString();
		string sgroup = dr["sgroup"].ToString();
		s += "<option value=" + id;
		if(id == current_group_id)
			s += " selected";
		s += ">" + sgroup + "</option>";
	}
	s += "</select>";
	return s;
}

string PrintBranchOptionsForBranchLogin(string current_id)
{
	if(dstcom.Tables["branch"] != null)
		dstcom.Tables["branch"].Clear();

	if(Session["branch_support"] != null)
	{
		if(current_id == null || current_id == "")
			current_id = Session["branch_id"].ToString();
	}
	else
	{
		current_id = "1";
	}
	int rows = 0;
	string s = "";
	string sc = " SELECT DISTINCT b.id, b.name ";
	sc += " FROM branch b ";
	sc += " LEFT OUTER JOIN card c ON c.our_branch = b.id AND c.is_branch = 1 AND c.type = 4 ";
	sc += " WHERE b.activated = 1 AND b.id > 1 ";
	sc += " AND (c.id IS NULL OR b.id = " + current_id + ") ";
	sc += " ORDER BY b.id ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "branch");
	}
	catch(Exception e1) 
	{
		ShowExp(sc, e1);
		return "";
	}
	s += "<select name=branch>";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dstcom.Tables["branch"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		s += "<option value=" + id;
		if(id == current_id)
			s += " selected";
		s += ">" + name + "</option>";
	}
	s += "</select>";
	return s;	
}
</script>

<!-- #include file="card_function.cs" -->

<script runat=server>

string err = "";
Boolean bAccountExists = false;
DataSet ds = new DataSet();

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_Init(); //do common things, LogVisit etc...

	if(Request.QueryString["t"] == "logins" || Request.QueryString["t"] == "branch")
	{
		if(!SecurityCheck("normal"))
			return;
		PrintHeaderAndMenu();
	}
        
		
	if(Request.QueryString["t"] == "logins")
	{
		if(Session["customer_access_level"] == null)
		{
			Response.Write("<h3>ACCESS DENIED</h3>");
			return;
		}
		if(MyIntParse(Session["customer_access_level"].ToString()) > 2)
		{
			Response.Write("<h3>ACCESS DENIED</h3>");
			return;
		}
		if(Request.QueryString["a"] == "del")
		{
			if(DoDeleteLogin())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?t=logins\">");
				return;
			}
		}
		if(Request.Form["cmd"] == "Add")
		{
			if(DoAddLogin())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?t=logins\">");
				return;
			}
		}
		if(Request.Form["cmd"] == "Update")
		{
			if(DoUpdateLogin())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?t=logins\">");
				return;
			}
		}
		ShowLogins();
		PrintFooter();
		return;
	}
	if(Request.QueryString["t"] == "branch")
	{
		if(Session["customer_access_level"] == null || MyIntParse(Session["customer_access_level"].ToString()) > 2) //only manager or branch manager have access
		{
			Response.Write("<h3>ACCESS DENIED</h3>");
			return;
		}
		if(Request.QueryString["a"] == "del")
		{
			if(DoDeleteBranch())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?t=branch\">");
				return;
			}
		}
		if(Request.Form["cmd"] == "Add")
		{
			if(DoAddBranch())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?t=branch\">");
				return;
			}
		}
		else if(Request.Form["cmd"] == "Update")
		{
			if(DoUpdateBranch())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?t=branch\">");
				return;
			}
		}
		ShowBranch();
		PrintFooter();
		return;
	}

	if(TS_UserLoggedIn())
		PrintHeaderAndMenu();
	else
		//PrintBasicHeader();
	    PrintHeaderAndMenu();
	if(Request.Form["cmd"] == "Continue")
	{
		if(Request.Form["agree_terms"] == "on")// && Request.Form["18plus"] == "on")
			Session["terms_read"] = true;
	}

	if(!TS_UserLoggedIn() && Session["terms_read"] == null)
	{
        string rsout = ReadSitePage("public_left_side_menu");
	    if(Cache["item_categories"] != null)
	        rsout = rsout.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	    else
		    rsout = rsout.Replace("@@HEADER_MENU_TOP_CAT", "");
		
		Response.Write("<center><table width=100% cellpadding=0 cellspacing=0 border=0><tr><td valign=top>");
		//Response.Write(rsout);
		Response.Write("</td>");
		Response.Write("<td width=10></td><td>");
		Response.Write(ReadSitePage("terms"));
		Response.Write("<br><center><form actino=register.aspx method=post>");
		Response.Write("<input type=checkbox name=agree_terms>");
	    Response.Write("<b>I have read, understood and accept this Terms And Conditions Of Sales of " + GetSiteSettings("company_name") + "</b> ");
        //Response.Write("<br/><br/><input type=checkbox name=18plus>");
	    //Response.Write("<b>I am over 18 years old</b> ");
		Response.Write(" <br/><br/><input type=submit name=cmd value=Continue  " + Session["button_style"].ToString() + " >");
		Response.Write("</form></td></tr></table>");
	}
    else if(m_sSite == "www")
    {
        Response.Write(ReadSitePage("downloads"));
    }
	else
	{
		if(TS_UserLoggedIn())
			bAccountExists = true;
		CheckUserTable();	//get user details if logged on
		GetQueryString();
	}
	if(TS_UserLoggedIn())
		PrintFooter();
	else
	//	PrintBasicFooter();
	PrintFooter();
}

void PrintBasicHeader()
{
	string s = ReadSitePage("page_header");
	int pos = s.ToLower().IndexOf("<body ");
	if(pos > 0)
		s = s.Substring(0, pos);
	Response.Write(s);
	Response.Write("<body marginwidth=0 marginheight=0 topmargin=1 leftmargin=0 text=black link=black vlink=black alink=black>");
}

void PrintBasicFooter()
{
	Response.Write("</body></html>");
}

void GetQueryString()
{
	string action = Request.QueryString["action"];
	string sc = "";

	if(action == "validate")
	{
		sc = "<br><br><div align=center><b>";
		if(ValidateUserDetails())
		{
			if(bAccountExists)
			{
				if(UpdateAccount())
					sc += "<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?action=updated\">";
			}
			else
			{
				if(CreateAccount())
					sc += "<meta http-equiv=\"refresh\" content=\"0; URL=register.aspx?action=created\">";
			}
		}
		else
		{
			sc += err;
			sc += "</b><br><br><input type=button " + Session["button_style"].ToString() + " onclick=window.location=('register.aspx?action=showform') ";
			sc += " value='<- Back'>\r\n";
		}
		sc += "</div>";
		Response.Write(sc);
	}
	else if(action == "created")
	{
		//PrintHeaderAndMenu();
		string rsout = ReadSitePage("public_left_side_menu");
	    if(Cache["item_categories"] != null)
			rsout = rsout.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
		else
			rsout = rsout.Replace("@@HEADER_MENU_TOP_CAT", "");
		Response.Write("<center>");
		Response.Write ("<table width=100% cellspacing cellpadding=0 border=0>");
		Response.Write("<tr><td valign=top>");
		//Response.Write(rsout);
	    Response.Write("</td>");
	    Response.Write("<td width=10></td>");
		Response.Write("<td align=center>");
		Response.Write("<h4> Thank you, your Login Name and Password will be emailed to you ASAP.<br><br>");		
		Response.Write("<br><input type=button " + Session["button_style"].ToString() + " onclick=window.location=('default.aspx') ");
		Response.Write(" value='Go to Home Page'>\r\n");
		Response.Write ("</td></tr></table>");
	}
	else if(action == "updated")
	{
		Response.Write("<center><br><br><h4>Account details saved.<br>\r\n");
		Response.Write("<br><input type=button " + Session["button_style"].ToString() + " onclick=window.location=('http://");
		Response.Write(Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "') value='Main Page'><br><br><br>\r\n");
	}
	else
	{
		ShowUserForm();
	}
}

Boolean ValidateUserDetails()
{
	if(dtUser.Rows.Count <= 0)
		return false;

	DataRow dr = dtUser.Rows[0];

	string email = Request.Form["email"];
	string email_old = Request.Form["email_old"];
	Trim(ref email);
	Trim(ref email_old);

	if(email != email_old)
	{
		if(IsDuplicateEmail(email))
		{
			Response.Write("<br><br><center><h3>Error, this email address has already been used</h3>");
			return false;
		}
	}

	dtUser.AcceptChanges();
	dr.BeginEdit();

	dr["id"] = EncodeQuote(Request.Form["id"]);
	dr["Name"] = EncodeQuote(Request.Form["name"]);
	dr["Email"] = EncodeQuote(Request.Form["email"]);
	dr["Company"] = EncodeQuote(Request.Form["company"]);
	dr["trading_name"] = EncodeQuote(Request.Form["trading_name"]);
	dr["corp_number"] = EncodeQuote(Request.Form["corp_number"]);
	dr["Address1"] = EncodeQuote(Request.Form["Address1"]);
	dr["Address2"] = EncodeQuote(Request.Form["Address2"]);
	dr["Address3"] = EncodeQuote(Request.Form["Address3"]);
	dr["postal1"] = EncodeQuote(Request.Form["postal1"]);
	dr["postal2"] = EncodeQuote(Request.Form["postal2"]);
	dr["postal3"] = EncodeQuote(Request.Form["postal3"]);
	dr["directory"] = EncodeQuote(Request.Form["directory"]);
	dr["gst_rate"] = EncodeQuote(Request.Form["gst_rate"]);
	dr["Phone"] = EncodeQuote(Request.Form["Phone"]);
	dr["fax"] = EncodeQuote(Request.Form["fax"]);
//	dr["City"] = EncodeQuote(Request.Form["City"]);
//	dr["Country"] = EncodeQuote(Request.Form["Country"]);
	
//	dr["pm_email"] = EncodeQuote(Request.Form["pm_email"]);
	dr["pm_email"] = EncodeQuote(Request.Form["email"]);
	dr["pm_ddi"] = EncodeQuote(Request.Form["pm_ddi"]);
	dr["pm_mobile"] = EncodeQuote(Request.Form["pm_mobile"]);
	dr["sm_name"] = EncodeQuote(Request.Form["sm_name"]);
	dr["sm_email"] = EncodeQuote(Request.Form["sm_email"]);
	dr["sm_ddi"] = EncodeQuote(Request.Form["sm_ddi"]);
	dr["sm_mobile"] = EncodeQuote(Request.Form["sm_mobile"]);
	dr["ap_name"] = EncodeQuote(Request.Form["ap_name"]);
	dr["ap_email"] = EncodeQuote(Request.Form["ap_email"]);
	dr["ap_ddi"] = EncodeQuote(Request.Form["ap_ddi"]);
	dr["ap_mobile"] = EncodeQuote(Request.Form["ap_mobile"]);

	dr.EndEdit();

	dtUser.AcceptChanges();
    if(m_sSite != "www")
    {
	    if(dr["company"].ToString() == "")
		    err = "Error, Company Legal Name can't be blank.";
	    else if(dr["trading_name"].ToString() == "")
		    err = "Error, Company Trading Name can't be blank.";
    }
	if(dr["Email"].ToString() == "")
		err = "Error, Login Email can't be blank.";
	else if(dr["Address1"].ToString() == "")
		err = "Error, Physical Address (line 1) can't be blank.";
	else if(dr["Phone"].ToString() == "")
		err = "Error, Phone Number can't be blank.";
	else if(dr["name"].ToString() == "")
		err = "Error, Name can't be blank.";
   
	    if(Request.Form["agree_terms"] != "on")
		    err = "Error, you must agree terms of sales.";
	   // else if(Request.Form["disclose"] != "on")
		    //err = "Error, you must be over 18 Years Old.";
    
	
	if(err != "")
		err = "<br><center><h3>" + err + "</h3></center>";
	Boolean bRet = (err == "");
//bRet = true;
	return bRet;
}

void ShowUserForm()
{
	if(dtUser.Rows.Count <= 0)
	{
//DEBUG("error, user table empty", "");
		return;
	}
    string rsout = ReadSitePage("public_left_side_menu");
	    if(Cache["item_categories"] != null)
		rsout = rsout.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	else
		rsout = rsout.Replace("@@HEADER_MENU_TOP_CAT", "");
	DataRow dr = dtUser.Rows[0];
	bool bNew = true;
	if(TS_UserLoggedIn())
		bNew = false;
	Response.Write ("<Table width=100% cellpadding=0 cellspacing=0 border=0>");
	Response.Write ("<tr><td valign=top>");
	//if(!m_bDealerArea)
	//Response.Write(rsout);
	Response.Write("</td>");
	Response.Write("<td width=10></td>");
	Response.Write ("<td>");
	Response.Write("<form action=register.aspx?action=validate method=post>");
	DrawCardTable(dr, bNew, "customer"); //draw empty
	Response.Write("</form>");
	Response.Write("</td></tr></table>");
}

Boolean UpdateAccount()
{
	if(Session["dtUser"] == null || dtUser.Rows.Count <= 0)
	{
//DEBUG("error, user table empty. CreateAccount failed", "");
		return false;
	}
	
	DataRow dr = dtUser.Rows[0];

	string email = dr["Email"].ToString();
	string email_old = Request.Form["email_old"];
	Trim(ref email);
	Trim(ref email_old);

	StringBuilder sb = new StringBuilder();

	sb.Append("UPDATE card SET "); //password='");
//	sb.Append(FormsAuthentication.HashPasswordForStoringInConfigFile(Request.Form["pass"], "md5"));

	if(email != email_old)
		sb.Append(" email='" + email + "', ");

	sb.Append(" Name='");
	sb.Append(dr["Name"].ToString());
	sb.Append("', Company='");
	sb.Append(dr["Company"].ToString());
	sb.Append("', Address1='");
	sb.Append(dr["Address1"].ToString());
	sb.Append("', Address2='");
	sb.Append(dr["Address2"].ToString());
	sb.Append("', address3='");
	sb.Append(dr["address3"].ToString());
	sb.Append("', fax='");
	sb.Append(dr["fax"].ToString());
	sb.Append("', Phone='");
	sb.Append(dr["Phone"].ToString());
	sb.Append("', trading_name='");
	sb.Append(dr["trading_name"].ToString());
	sb.Append("', corp_number='");
	sb.Append(dr["corp_number"].ToString());
	sb.Append("', postal1='");
	sb.Append(dr["postal1"].ToString());
	sb.Append("', postal2='");
	sb.Append(dr["postal2"].ToString());
	sb.Append("', postal3='");
	sb.Append(dr["postal3"].ToString());
	sb.Append("', pm_ddi='");
	sb.Append(dr["pm_ddi"].ToString());
	sb.Append("', pm_mobile='");
	sb.Append(dr["pm_mobile"].ToString());
	sb.Append("', sm_name='");
	sb.Append(dr["sm_name"].ToString());
	sb.Append("', sm_email='");
	sb.Append(dr["sm_email"].ToString());
	sb.Append("', sm_ddi='");
	sb.Append(dr["sm_ddi"].ToString());
	sb.Append("', sm_mobile='");
	sb.Append(dr["sm_mobile"].ToString());
	sb.Append("', ap_name='");
	sb.Append(dr["ap_name"].ToString());
	sb.Append("', ap_email='");
	sb.Append(dr["ap_email"].ToString());
	sb.Append("', ap_ddi='");
	sb.Append(dr["ap_ddi"].ToString());
	sb.Append("', ap_mobile='");
	sb.Append(dr["ap_mobile"].ToString());
	sb.Append("' WHERE id='");
	sb.Append(dr["id"].ToString());
	sb.Append("'");
//DEBUG("sc=", sb.ToString());
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

//	if(!UpdateGroup(dr["Email"].ToString(), null, null, null))
//		return false;

	return true;
}

Boolean CreateAccount()
{
	string id = Request.Form["id"];
	string email = Request.Form["email"];
	string password = ""; //Request.Form["password"];
//	if(password != "")
//		password = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
	string type = GetEnumID("card_type", "customer");//Request.Form["type"];
	string name = Request.Form["name"];
//	string short_name = Request.Form["short_name"];
	string gst_rate = Request.Form["gst_rate"];
	string company = Request.Form["company"];
	string trading_name = Request.Form["trading_name"];
	string corp_number = Request.Form["corp_number"];
	string directory = Request.Form["directory"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string postal1 = Request.Form["postal1"];
	string postal2 = Request.Form["postal2"];
	string postal3 = Request.Form["postal3"];
//	string city = Request.Form["city"];
//	string country = Request.Form["country"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];
	string dealer_level = "1";//Request.Form["dealer_level"];
	string credit_term = "0";//Request.Form["credit_term"];
	string credit_limit = "0";//Request.Form["credit_limit"];

	string note = "";//Request.Form["note"];
	string access_level = "1";// rights;//GetEnumID("access_level", rights);

	string pm_email = Request.Form["pm_email"];
	string pm_ddi = Request.Form["pm_ddi"];
	string pm_mobile = Request.Form["pm_mobile"];
	string sm_name = Request.Form["sm_name"];
	string sm_email = Request.Form["sm_email"];
	string sm_ddi = Request.Form["sm_ddi"];
	string sm_mobile = Request.Form["sm_mobile"];
	string ap_name = Request.Form["ap_name"];
	string ap_email = Request.Form["ap_email"];
	string ap_ddi = Request.Form["ap_ddi"];
	string ap_mobile = Request.Form["ap_mobile"];
	string currency_for_purchase = "1";

	string approved = "0";
	Trim(ref email);
	email = EncodeQuote(email);

	name = EncodeQuote(name);
	company = EncodeQuote(company);
	trading_name = EncodeQuote(trading_name);
	phone = EncodeQuote(phone);
	fax = EncodeQuote(fax);
	address1 = EncodeQuote(address1);
	address2 = EncodeQuote(address2);
	address3 = EncodeQuote(address3);
	postal1 = EncodeQuote(postal1);
	postal2 = EncodeQuote(postal2);
	postal3 = EncodeQuote(postal3);
	note = EncodeQuote(note);

	pm_email = EncodeQuote(pm_email);
	pm_ddi = EncodeQuote(pm_ddi);
	pm_mobile = EncodeQuote(pm_mobile);
	sm_name = EncodeQuote(sm_name);
	sm_email = EncodeQuote(sm_email);
	sm_ddi = EncodeQuote(sm_ddi);
	sm_mobile = EncodeQuote(sm_mobile);
	ap_name = EncodeQuote(ap_name);
	ap_email = EncodeQuote(ap_email);
	ap_ddi = EncodeQuote(ap_ddi);
	ap_mobile = EncodeQuote(ap_mobile);

	if(gst_rate == null || gst_rate == "")
		gst_rate = "0.125";
	if(directory == null || directory == "")
		directory = "1";
	bool bRet = AddNewDealer(email, password, trading_name, corp_number, directory, gst_rate, 
		currency_for_purchase, company, address1, address2, 
		address3, phone, fax, postal1, postal2, postal3, access_level, dealer_level, note, 
		credit_term, credit_limit, approved, false, "0", "all", name, pm_email, pm_ddi, pm_mobile, sm_name, sm_email, 
		sm_ddi, sm_mobile, ap_name, ap_email, ap_ddi, ap_mobile, "", GetEnumID("card_type", "customer").ToString(), "", "1");

	if(bRet)
	{
		MailMessage msgMail = new MailMessage();

		string url = "http://" + Request.ServerVariables["SERVER_NAME"] + "";
		//url += "/"+ m_sCompanyName;
		url += "/admin/ecard.aspx?id=" + Session["new_card_id"].ToString();
		msgMail.To = GetSiteSettings("account_manager_email", "alert@eznz.com");
		msgMail.From = m_sSalesEmail;
		msgMail.Subject = "Total Liquor Website Customer Application Form Received";
		msgMail.BodyFormat = MailFormat.Html;
		msgMail.Body += "<br>\r\nCompany : " + company + "<br>\r\n";
		msgMail.Body += "Customer Name : " + name + "<br>\r\n";
		msgMail.Body += "Customer EMail : " + email + "<br><br>\r\n";
		msgMail.Body += "click here to process <a href=" + url + ">" + url + "</a>\r\n";
		SmtpMail.Send(msgMail);
	}

	return bRet;
}

bool ShowLogins()
{
	int rows = 0;
	string sc = " SELECT c.*, c1.trading_name AS branch_name, a.class_name ";
	sc += " FROM card c LEFT OUTER JOIN card_access_class a ON a.class_id=c.customer_access_level AND a.main_card_id = " + Session["card_id"];
	sc += " LEFT OUTER JOIN card c1 ON c1.id = c.branch_card_id ";
	sc += " WHERE c.main_card_id = " + Session["card_id"];// + " AND c.is_branch=0 ";
	if(Session["main_card_id"].ToString() != "") //branch manager logged in
	{
//		sc += " AND card_id != " + Session["login_card_id"]; //don't list himself
		sc += " AND c.branch_card_id = " + Session["login_card_id"]; //only show logins of this branch
	}
	sc += " ORDER BY c.name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "logins");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<form action=register.aspx?t=logins method=post>");

	Response.Write("<br><center><h3>Extra Login List</h3>");
	Response.Write("<table width=500 cellspacing=3 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Name</th>");
	Response.Write("<th nowrap>Login Email</th>");
	Response.Write("<th nowrap>Branch</th>");
//	if(Session["main_card_id"].ToString() == "") //manager loggedin
		Response.Write("<th nowrap>Acess Level</th>");
	Response.Write("<th nowrap>&nbsp;</th>");
	Response.Write("</tr>");
	
	bool bEdit = false;
	bool bAlterColor = false;
	if(Request.QueryString["a"] == "edit")
		bEdit = true;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["logins"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string email = dr["email"].ToString();
		string branch_name = dr["branch_name"].ToString();
		string branch_card_id = dr["branch_card_id"].ToString();
		bool bBranch = bool.Parse(dr["is_branch"].ToString());
		string branch = "NO";
		if(bBranch)
		{
			branch = "YES";
			branch_name = dr["trading_name"].ToString(); //it's the main branch card
		}
		if(branch_name == "")
			branch_name = "Main";

		string customer_access_level = dr["customer_access_level"].ToString();
		string customer_access_level_name = dr["class_name"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		if(bEdit && Request.QueryString["id"] == id)
		{
			Response.Write("<input type=hidden name=id value=" + id + ">");
			Response.Write("<input type=hidden name=email_old value=" + email + ">");
			Response.Write("<td><input type=text name=name value='" + name + "'></td>");
			Response.Write("<td><input type=text name=email value='" + email + "'></td>");
			if(Session["main_card_id"] == null || Session["main_card_id"] == "") //only manager can change branch
				Response.Write("<td>" + PrintLoginBranchOption(dr["branch_card_id"].ToString()) + "</td>");
			else //otherwise can't change branch
				Response.Write("<td><input type=hidden name=branch value=" + branch_card_id + ">" + branch_name + "</td>");
			if(MyIntParse(Session["customer_access_level"].ToString()) <= 2) //manager or branch manager
			{
				Response.Write("<td>");
				Response.Write(PrintPublicAccessOption(customer_access_level));
				Response.Write("</td>");
			}
			else
				Response.Write("<td><input type=hidden name=level value=" + customer_access_level + ">" + customer_access_level_name + "</td>");
			Response.Write("<td><input type=submit name=cmd value=Update1 " + Session["button_style"] + ">");
			Response.Write("<input type=button value=Cancel onclick=window.location=('register.aspx?t=" + Request.QueryString["t"] + "') " + Session["button_style"] + "></td>");
		}
		else
		{
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td>" + email + "</td>");
			Response.Write("<td>" + branch_name + "</td>");
			Response.Write("<td>" + customer_access_level_name + "</td>");
			Response.Write("<td nowrap>");
			if(MyIntParse(Session["customer_access_level"].ToString()) <= 2)
			{
				if(id != Session["login_card_id"].ToString()) //suicide not recommended
					Response.Write("<a href=register.aspx?t=logins&a=del&id=" + id + " class=o>del</a> ");
			}
			if(id == branch_card_id) //main branch card
				Response.Write("<a href=register.aspx?t=branch&a=edit&id=" + id + " class=o>edit</a> ");
			else
				Response.Write("<a href=register.aspx?t=logins&a=edit&id=" + id + " class=o>edit</a> ");
			Response.Write("</td>");
		}
		Response.Write("</tr>");
	}

	if(!bEdit && MyIntParse(Session["customer_access_level"].ToString()) <= 2)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		Response.Write("<td><input type=text name=name></td>");
		Response.Write("<td><input type=text name=email></td>");
		if(MyIntParse(Session["customer_access_level"].ToString()) == 2) //branch manager logged in
			Response.Write("<td><input type=hidden name=branch value=" + Session["login_card_id"] + ">" + Session["trading_name"] + "</td>");
		else //otherwise can't change branch
			Response.Write("<td>" + PrintLoginBranchOption(Session["card_id"].ToString()) + "</td>");
		if(MyIntParse(Session["customer_access_level"].ToString()) <= 2) //manager or branch manager
		{
			Response.Write("<td>");
			Response.Write(PrintPublicAccessOption("1"));
			Response.Write("</td>");
		}
		Response.Write("<td align=right><input type=submit name=cmd value=Add " + Session["button_style"] + "></td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");

	if(MyIntParse(Session["customer_access_level"].ToString()) <= 2) //manager or branch manager
	{
/*		Response.Write("<table width=500 cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=2><b>General Configuration</b></td></tr>");

		Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
		Response.Write("<th width=30% align=left>Level</th><th align=left>Denied Access</th></tr>");

		Response.Write("<tr><td width=30%><b>Manager</td><td>no</td></tr>");
		Response.Write("<tr><td width=30% nowrap><b>Branch Manager</td><td>Statement</td></tr>");
		Response.Write("<tr><td width=30%><b>Sales</td><td>Statement, RMA</td></tr>");
		Response.Write("<tr><td width=30%><b>Accountant</td><td>Order Placing, RMA, Product Price Viewing</td></tr>");
		Response.Write("<tr><td width=30%><b>Technician</td><td>Statement, Order Placing, Product Price Viewing</td></tr>");

//		Response.Write("<tr><td colspan=2><font color=red><i> * Only Manager can change login settings, of course</td></tr>");
		if(MyIntParse(Session["customer_access_level"].ToString()) == 1) //manager
		{
			Response.Write("<tr><td colspan=2><font color=red><i> * You can edit these basic settings if needed <a href=card_as.aspx class=o>Advance Settings</a></td></tr>");
			Response.Write("<tr><td colspan=2><font color=red><i> * You can add or edit branches here <a href=register.aspx?t=branch class=o>Edit Branch</a></td></tr>");
		}
		Response.Write("</table>");
*/

		Response.Write("<table><tr><td>");
		Response.Write("<b>General Configuration</b><br>");
		Response.Write("<font color=red><i>");
		Response.Write(" * Only manager can place order by default. <a href=card_as.aspx class=o>Allowing others to place order</a> on your own risk.<br>");
		Response.Write(" * Only manager and accountant can view account statement. <br>");
		Response.Write(" * Branch Manager can add and edit logins for his branch. <br>");
		Response.Write(" * Sales have no RMA access. <br>");
		Response.Write(" * Accountant cannot view product category, no RMA access. <br>");
		Response.Write(" * Technician can only access RMA section. <br>");
		Response.Write(" * You can modify all these settings here in <a href=card_as.aspx class=o>Advance Settings</a><br>");
		Response.Write("</i></font>");
		Response.Write("</td></tr>");
		if(MyIntParse(Session["customer_access_level"].ToString()) == 1) //manager
		{
			Response.Write("<tr><td colspan=2><font color=red><i> * You can add or edit branches here <a href=register.aspx?t=branch class=o>Edit Branch</a></td></tr>");
		}
		Response.Write("</table>");
	}
	return true;
}

bool DoAddLogin()
{
	string name = Request.Form["name"];
	string email = Request.Form["email"];
	string level = Request.Form["level"];
	string branch = Request.Form["branch"];

	Trim(ref name);
	Trim(ref email);

	string err = "";
	if(name == "")
		err = "Error, name cannot be blank";
	else if(email == "")
		err = "Error, please enter email";
	else if(IsDuplicateEmail(email))
		err = "Error, this email has already been used";

	if(err != "")
	{
		Response.Write("<br><center><h3>" + err + "</h3>");
		return false;
	}

	string password = GenRandomString();
	
	string main_card_id = Session["main_card_id"].ToString();
	if(main_card_id == "")
		main_card_id = Session["card_id"].ToString();

	string sc = " INSERT INTO card (name, email, password, main_card_id, customer_access_level, branch_card_id)";
	sc += " VALUES('" + EncodeQuote(name) + "', '" + EncodeQuote(email) + "', '";
	sc += FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
	sc += "', " + main_card_id;
	sc += ", " + level + ", " + branch;
	sc += ") ";
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
	
	string url = "http://" + Request.ServerVariables["SERVER_NAME"] + "/" + TSGetPath();
	MailMessage msgMail = new MailMessage();

	msgMail.To = email;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = "New Login Activited";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body += "<br>\r\nDear : " + name + "<br>\r\n";
	msgMail.Body += "Your login on " + m_sCompanyName + " is activited.<br><br>\r\n";
	msgMail.Body += "Login EMail : " + email + "<br>\r\n";
	msgMail.Body += "Password : " + password + "<br><br>\r\n";
	msgMail.Body += "Click here to login <a href=" + url + ">" + url + "</a>\r\n";
	SmtpMail.Send(msgMail);

	Response.Write("<br><center><h3>Done! Auto generated password has been sent to " + email + "</h3><br>");
	Response.Write("<h5>Please wait a few second ......</h5><br><br><br><br><br><br>");
	return true;
}

bool DoUpdateLogin()
{
	string id = Request.Form["id"];
	string name = Request.Form["name"];
	string email = Request.Form["email"];
	string email_old = Request.Form["email_old"];
	string level = Request.Form["level"];
	string branch = Request.Form["branch"];

	Trim(ref name);
	Trim(ref email);

	string err = "";
	if(name == "")
		err = "Error, name cannot be blank";
	else if(email == "")
		err = "Error, please enter email";
	else if(email != email_old)
	{
		if(IsDuplicateEmail(email))
			err = "Error, this email has already been used";
	}

	if(err != "")
	{
		Response.Write("<br><center><h3>" + err + "</h3>");
		return false;
	}

	string sc = " Update card SET ";
	sc += " name = '" + EncodeQuote(name) + "' ";
	sc += ", email = '" + EncodeQuote(email) + "' ";
	sc += ", customer_access_level = " + level;
	sc += ", branch_card_id = " + branch;
	sc += " WHERE id = " + id;
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

bool DoDeleteLogin()
{
	string id = Request.QueryString["id"];
	string sc = " DELETE FROM card WHERE id=" + id + " AND main_card_id = " + Session["card_id"].ToString();
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
	Response.Write("<br><center><h3>Login Deleted, Please wait a second ......</h3><br><br><br><br><br><br><br>");
	return true;
}


bool ShowBranch()
{
	int rows = 0;
	string sc = " SELECT * FROM card ";
	sc += " WHERE main_card_id = " + Session["card_id"] + " AND is_branch=1 ";
	if(Session["main_card_id"].ToString() != "") //branch manager logged in
	{
		sc += " AND branch_card_id = " + Session["login_card_id"]; //only show logins of this branch
	}
	sc += " ORDER BY register_date ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<form action=register.aspx?t=branch method=post>");

	Response.Write("<br><center><font size=+1><b>Edit Local Branch</b></font><br>");
	Response.Write("<font color=red><i><b>(Same account, different address)</b></i></font><br><br>");
	Response.Write("<table width=500 cellspacing=3 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th nowrap>Branch Name</th>");
	Response.Write("<th nowrap>Manager Name</th>");
	Response.Write("<th nowrap>Login Email</th>");
	Response.Write("<th nowrap>Shipping Address Line 1</th>");
	Response.Write("<th nowrap>Shipping Address Line 2</th>");
	Response.Write("<th nowrap>Shipping Address Line 3</th>");
	Response.Write("<th nowrap>Phone</th>");
	Response.Write("<th nowrap>Fax</th>");
	Response.Write("<th nowrap>&nbsp;</th>");
	Response.Write("</tr>");
	
	bool bEdit = false;
	bool bAlterColor = false;
	if(Request.QueryString["a"] == "edit")
		bEdit = true;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["branch"].Rows[i];
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string name = dr["name"].ToString();
		string email = dr["email"].ToString();
		string address1 = dr["address1"].ToString();
		string address2 = dr["address2"].ToString();
		string address3 = dr["address3"].ToString();
		string phone = dr["phone"].ToString();
		string fax = dr["fax"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		if(bEdit && Request.QueryString["id"] == id)
		{
			Response.Write("<input type=hidden name=id value=" + id + ">");
			Response.Write("<input type=hidden name=email_old value=" + email + ">");
			Response.Write("<td><input type=text size=10 name=trading_name value='" + trading_name + "'></td>");
			Response.Write("<td><input type=text size=10 name=name value='" + name + "'></td>");
			Response.Write("<td><input type=text size=20 name=email value='" + email + "'></td>");
			Response.Write("<td><input type=text size=20 name=address1 value='" + address1 + "'></td>");
			Response.Write("<td><input type=text size=20 name=address2 value='" + address2 + "'></td>");
			Response.Write("<td><input type=text size=20 name=address3 value='" + address3 + "'></td>");
			Response.Write("<td><input type=text size=10 name=phone value='" + phone + "'></td>");
			Response.Write("<td><input type=text size=10 name=fax value='" + fax + "'></td>");
			Response.Write("<td><input type=submit name=cmd value=Update " + Session["button_style"] + "></td>");
		}
		else
		{
			Response.Write("<td>" + trading_name + "</td>");
			Response.Write("<td>" + name + "</td>");
			Response.Write("<td>" + email + "</td>");
			Response.Write("<td>" + address1 + "</td>");
			Response.Write("<td>" + address2 + "</td>");
			Response.Write("<td>" + address3 + "</td>");
			Response.Write("<td>" + phone + "</td>");
			Response.Write("<td>" + fax + "</td>");
			Response.Write("<td nowrap>");
			if(id != Session["login_card_id"].ToString()) //suicide not recommended
				Response.Write("<a href=register.aspx?t=branch&a=del&id=" + id + " class=o>del</a> ");
			Response.Write("<a href=register.aspx?t=branch&a=edit&id=" + id + " class=o>edit</a> ");
			Response.Write("</td>");
		}

		Response.Write("</tr>");
	}

	if(!bEdit && Session["main_card_id"].ToString() == "") //only manager can add branch
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		Response.Write("<td><input type=text size=10 name=trading_name></td>");
		Response.Write("<td><input type=text size=10 name=name></td>");
		Response.Write("<td><input type=text size=20 name=email></td>");
		Response.Write("<td><input type=text size=20 name=address1></td>");
		Response.Write("<td><input type=text size=20 name=address2></td>");
		Response.Write("<td><input type=text size=20 name=address3></td>");
		Response.Write("<td><input type=text size=10 name=phone></td>");
		Response.Write("<td><input type=text size=10 name=fax></td>");
		Response.Write("<td><input type=submit name=cmd value=Add " + Session["button_style"] + "></td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("<a href=register.aspx?t=logins class=o>Manage Extra Logins</a>");
	return true;
}

bool DoAddBranch()
{
	string trading_name = Request.Form["trading_name"];
	string name = Request.Form["name"];
	string email = Request.Form["email"];
	string email_old = Request.Form["email_old"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];

	Trim(ref trading_name);
	Trim(ref name);
	Trim(ref email);
	Trim(ref address1);

	string err = "";
	if(trading_name == "")
		err = "Error, please enter branch name";
	if(name == "")
		err = "Error, name cannot be blank";
	else if(email == "")
		err = "Error, please enter email";
	else if(email != email_old)
	{
		if(IsDuplicateEmail(email))
			err = "Error, this email has already been used";
	}
	else if(address1 == "")
		err = "Error, please enter shipping address";

	if(name.Length >= 50)
		err = "Error, name is too long, max length is 50";
	if(email.Length >= 50)
		err = "Error, email is too long, max length is 50";
	if(address1.Length >= 50)
		err = "Error, address line 1 is too long, max length is 50";
	if(address2.Length >= 50)
		err = "Error, address line 2 is too long, max length is 50";
	if(address3.Length >= 50)
		err = "Error, address line 3 is too long, max length is 50";
	if(phone.Length >= 50)
		err = "Error, phone number is too long, max length is 50";
	if(fax.Length >= 50)
		err = "Error, fax number is too long, max length is 50";

	if(err != "")
	{
		Response.Write("<br><center><h3>" + err + "</h3>");
		return false;
	}

	string password = GenRandomString();
	
	string sc = " INSERT INTO card (trading_name, name, email, password, address1, address2, address3 ";
	sc += ", main_card_id, is_branch, phone, fax, customer_access_level)";
	sc += " VALUES('" + EncodeQuote(trading_name) + "', '" + EncodeQuote(name) + "', '" + EncodeQuote(email) + "', '";
	sc += FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
	sc += "', '" + EncodeQuote(address1);
	sc += "', '" + EncodeQuote(address2);
	sc += "', '" + EncodeQuote(address3);
	sc += "', " + Session["card_id"].ToString() + ", 1 ";
	sc += ", '" + EncodeQuote("phone") + "' ";
	sc += ", '" + EncodeQuote("fax") + "', 2 "; //default to branch manager
	sc += ") ";
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
	
	string url = "http://" + Request.ServerVariables["SERVER_NAME"] + "/" + TSGetPath();
	MailMessage msgMail = new MailMessage();

	msgMail.To = email;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = "Branch ID Activited";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body += "<br>\r\nDear : " + name + "<br>\r\n";
	msgMail.Body += "Your login (new branch) on " + m_sCompanyName + " is activited.<br><br>\r\n";
	msgMail.Body += "Login EMail : " + email + "<br>\r\n";
	msgMail.Body += "Password : " + password + "<br><br>\r\n";
	msgMail.Body += "Click here to login <a href=" + url + ">" + url + "</a>\r\n";
	SmtpMail.Send(msgMail);

	Response.Write("<br><center><h3>Done! Auto generated password has been sent to " + email + "</h3><br>");
	Response.Write("<h5>Please wait a few second ......</h5><br><br><br><br><br><br>");
	return true;
}

bool DoUpdateBranch()
{
	string id = Request.Form["id"];
	string trading_name = Request.Form["trading_name"];
	string name = Request.Form["name"];
	string email = Request.Form["email"];
	string email_old = Request.Form["email_old"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];

	Trim(ref trading_name);
	Trim(ref name);
	Trim(ref email);
	Trim(ref address1);

	string err = "";
	if(trading_name == "")
		err = "Error, please enter branch name";
	if(name == "")
		err = "Error, name cannot be blank";
	else if(email == "")
		err = "Error, please enter email";
	else if(email != email_old)
	{
		if(IsDuplicateEmail(email))
			err = "Error, this email has already been used";
	}
	else if(address1 == "")
		err = "Error, please enter shipping address";


	if(name.Length >= 50)
		err = "Error, name is too long, max length is 50";
	if(email.Length >= 50)
		err = "Error, email is too long, max length is 50";
	if(address1.Length >= 50)
		err = "Error, address line 1 is too long, max length is 50";
	if(address2.Length >= 50)
		err = "Error, address line 2 is too long, max length is 50";
	if(address3.Length >= 50)
		err = "Error, address line 3 is too long, max length is 50";
	if(phone.Length >= 50)
		err = "Error, phone number is too long, max length is 50";
	if(fax.Length >= 50)
		err = "Error, fax number is too long, max length is 50";

	if(err != "")
	{
		Response.Write("<br><center><h3>" + err + "</h3>");
		return false;
	}

	string sc = " Update card ";
	sc += " SET trading_name = '" + EncodeQuote(trading_name) + "' ";
	sc += ", name = '" + EncodeQuote(name) + "' ";
	sc += ", email = '" + EncodeQuote(email) + "' ";
	sc += ", address1 = '" + EncodeQuote(address1) + "' ";
	sc += ", address2 = '" + EncodeQuote(address2) + "' ";
	sc += ", address3 = '" + EncodeQuote(address3) + "' ";
	sc += ", phone = '" + EncodeQuote(phone) + "' ";
	sc += ", fax = '" + EncodeQuote(fax) + "' ";
	sc += " WHERE id = " + id + " AND main_card_id = " + Session["card_id"].ToString();
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

	//update card
	if(id == Session["login_card_id"].ToString())
	{
		CheckUserTable();
		DataRow dru = dtUser.Rows[0];
		dru["Address1"]	= address1;
		dru["Address2"]	= address2;
		dru["Address3"]	= address3;
		dru["Phone"]	= phone;
		dru["Fax"]		= fax;
		dru["branch"]	= trading_name;
		dtUser.AcceptChanges();
	}
	Response.Write("<br><center><h3>Done! Information saved</h3><br>");
	Response.Write("<h5>Please wait a few second ......</h5><br><br><br><br><br><br>");
	return true;
}

bool DoDeleteBranch()
{
	string id = Request.QueryString["id"];
	string sc = " DELETE FROM card WHERE id=" + id + " AND main_card_id = " + Session["card_id"].ToString();
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
	Response.Write("<br><center><h3>Login Deleted, Please wait a second ......</h3><br><br><br><br><br><br><br>");
	return true;
}

</script>

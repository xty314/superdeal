<!-- #include file="card_function.cs" -->
<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
int m_id = -1;
string m_email = "";
string m_type = "";
string m_action = "";
string m_cmd = "";
string m_referer = "";
bool m_bRestrict = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(Request.QueryString["v"] != "view") // allow view card details for all employee
	{
	if(!SecurityCheck("sales"))
		return;
	}
	string sstrict = GetSiteSettings("secure_employee_card", "0");
	if(MyBooleanParse(sstrict))
		m_bRestrict = true;
	if(m_bRestrict)
	{
		string al = Session[m_sCompanyName + "AccessLevel"].ToString();
		if(al == "10" || al == "8")
			m_bRestrict = false; //administrator or manager have access
	}

	if(Request.QueryString["id"] != null)
		m_id = MyIntParse(Request.QueryString["id"]);
	if(Request.Form["cmd"] != null)
		m_cmd = Request.Form["cmd"];
	if(Request.QueryString["email"] != null)
		m_email = Request.QueryString["email"];
	if(Request.QueryString["a"] != null)
		m_action = Request.QueryString["a"];

	if(Request.QueryString["t"] != null && Request.QueryString["t"] == "member")
	{
		if(m_cmd == "")
		{
			PrintEditMemberForm();
		}
		else
		{
			UpdateMember();
		}
		return;
	}

	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["r"] == null)
		{
			Response.Redirect(Request.ServerVariables["URL"] + "?" 
				+ Request.ServerVariables["QUERY_STRING"] + "&r="
				+ DateTime.Now.ToOADate());
			return;
		}
	}
	if(Request.Form["referer"] != null)
		m_referer = Request.Form["referer"];
	else if(Request.QueryString["ref"] != null)
		m_referer = Request.QueryString["ref"];

	//new card type
	if(Request.QueryString["n"] != null)
		m_type = Request.QueryString["n"];

	if(m_bRestrict && Request.QueryString["v"] != "view") // allow view card details for all employee
	{

		if(m_type != null && m_type != "" && MyIntParse(GetEnumID("card_type", m_type)) > 2) //MyIntParse(GetEnumValue("card_type", m_type)) > 2)
		{
			Response.Write("<h3>ACCESS DENIED");
			return;
		}
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.Form["cmd"] == "Send Mail")
	{
		Response.Write("<br><br><h3><center>Application Process</center></h3>");
		if(!MailProcessing())
		{
			Response.Write("<br><br><br><b>Failure in mail sending! Please send the mail again!</b>");

			PrintAdminFooter();
		}
		else
		{
			Response.Write("<br><br><br><b>&nbsp;&nbsp;The e-mail has been sent <font color=blue>successfully</font>!</b>");

			PrintAdminFooter();
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=card.aspx?approved=0&r=" +  DateTime.Now.ToOADate() + "\">");			
		}
		return;	
	}

	if(Request.QueryString["t"] == "del")
	{

		bool bKeyOK = true;
		if(Session["delete_card_key"] == null)
			bKeyOK = false;
		else if(Session["delete_card_key"].ToString() != Request.QueryString["r"])
			bKeyOK = false;

		if(!bKeyOK)
		{
			Response.Write("<br><br><center><h3>Please follow the proper link to delete order.</h3>");
		}
		else
		{
			if(DoDelete())
			{
				Session["delete_card_key"] = null;
				Response.Write("<br><br><center><h3>Card Deleted.</h3>");
				Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('");
				Response.Write("card.aspx?r=" + DateTime.Now.ToOADate() + "') value='Back to Card List'></center>");
			}
			else
			{
				Response.Write("<br><br><h3>Error Deleting Order");
			}
		}
		//LFooter.Text = m_sFooter;
		return;
	}

	if(Request.Form["cmd"] == "Approve" || Request.Form["cmd"] == "Send Password" || Request.Form["cmd"] == "Generate")
	{
		if(Request.Form["cmd"] == "Approve")
		{
			if(!DoApprove())
				return;
		}

		string pass = GenRandomString();

		Response.Write("<br><br><h3><center>Application Process</center></h3>");

		DrawAppProcessForm(pass);
//Response.Write(pass);
		PrintAdminFooter();
		return;
	}
	else if(Request.Form["cmd"] == "Apply Password")
	{
		UpdatePwd();
		Response.Write("<br><br><h3><center>Application Process</center></h3>");

		DrawAppProcessForm(Request.Form["pwd"]);
		PrintAdminFooter();
		return;
	}


	WriteHeaders();
//DEBUG("m_cmd=", m_cmd);
	if(m_cmd == "Update" || m_cmd == "Submit")
	{
		Response.Write("<h3>Processing, wait ...... </he>");
		Response.Flush();
		if(DoUpdate())
		{
			if(m_referer == "")
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=ecard.aspx?");
//				if(m_id >= 0)
					Response.Write("id=" + m_id + "&r=" + DateTime.Now.ToOADate() + "\">");
//				else
//					Response.Write("email=" + Request.Form["email"] + "&r=" + DateTime.Now.ToOADate() + "\">");
			}
			else if(m_referer == "quotation")
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=q.aspx?ci=" + m_id + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
	}
	else if(m_cmd == "Add New" || m_action == "new")
	{
		m_id = -1;
		DrawCardTable(null, true, m_type); //draw empty
	}
	else if(m_cmd == "Delete")
	{
		if(Request.Form["delete"] != "on")
		{
			Response.Write("<br><center><h3>Please tick 'Delete this card' to confirm deletion</h3><br><br>");
			Response.Write("<input type=button value=' Back ' onclick=history.go(-1) " + Session["button_style"] + ">");
			Response.Write("<br><br><br><br>");
			return;
		}
		string delkey = DateTime.Now.ToOADate().ToString();
		Session["delete_card_key"] = delkey;
		Response.Write("<script Language=javascript");
		Response.Write(">");
//		Response.Write(" rmsg = window.prompt('Are you sure you want to delete this order?')\r\n");
		Response.Write("if(window.confirm(' ");
		Response.Write("Are you sure you want to delete this Card?         ");
		Response.Write("\\r\\nThis action cannot be undo.\\r\\n");
		Response.Write("\\r\\nClick OK to delete Card.\\r\\n");
		Response.Write("'))");
		Response.Write("window.location='ecard.aspx?t=del&id=" + m_id.ToString() + "&r=" + delkey + "';\r\n");
		Response.Write("else window.location='ecard.aspx?id=" + m_id.ToString() + "&r=" + delkey + "';\r\n");
		Response.Write("</script");
		Response.Write(">");
		return;
	}
	else
	{
		if(m_id >= 0 || m_email != "")
		{
			if(!DoSearch())
				return;
		}
		DataRow dr = null;
		if(dst.Tables["card"] != null && dst.Tables["card"].Rows.Count > 0)
		{
			dr = dst.Tables["card"].Rows[0];
			if(m_bRestrict && Request.QueryString["v"] != "view") // allow view card details for all employee
			{
				string card_type = dr["type"].ToString();
				if(MyIntParse(card_type) > 2)
				{
					Response.Write("<h3>ACCESS DENIED");
					return;
				}
			}
			
			DrawCardTable(dr, false, "");
		}
	}
	WriteFooter();
	PrintAdminFooter();
}

Boolean DoSearch()
{
	dst.Clear();
	
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * ");
	sb.Append("FROM card ");
	if(m_id >= 0)
	{
		sb.Append(" WHERE id=");
		sb.Append(m_id);
	}
	else
	{
		sb.Append(" WHERE email='");
		sb.Append(m_email);
		sb.Append("'");
	}
//DEBUG("sc=", sb.ToString());	
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		if(myAdapter.Fill(dst, "card") <= 0)
		{
			Response.Write("<br><center><h3>Card Not Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

Boolean DoDelete()
{
	int orders = 0;
	int purchases = 0;

	if(!SecurityCheck("administrator"))
	{
		Response.Write("<h3>Error, you don't have permission to delete</h3>");
		return false;
	}

	if(Request.QueryString["id"] == null)
	{
		Response.Write("<h2>Error, null id to delete?</h2>");
		return false;
	}
	else
	{
		if(!CheckOrderHistory(ref orders, ref purchases))	//check sales record, if have then can not be deleted;
			return false;
		
		if(orders > 0)
		{
			Response.Write("<br><br><br><center><h3>Error, This Account Holder has " + orders + " order records");
			return false;
		}
		else if(purchases > 0)
		{
			Response.Write("<br><br><br><center><h3>Error, This Account Holder has " + purchases + " purchase record");
			return false;
		}
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("DELETE FROM card WHERE id=");
	sb.Append(Request.QueryString["id"]);
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

bool CheckOrderHistory(ref int orders, ref int purchases)
{
	string sc = " SELECT count(*) FROM orders WHERE card_id = " + Request.QueryString["id"];
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "orders") == 1)
			orders = MyIntParse(dst.Tables["orders"].Rows[0][0].ToString());
		else
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = " SELECT count(*) FROM purchase WHERE supplier_id = " + Request.QueryString["id"];
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "purchase") == 1)
			purchases = MyIntParse(dst.Tables["purchase"].Rows[0][0].ToString());
		else
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean DoUpdate()
{
	Boolean bRet = true;

	string id = Request.Form["id"];
	string branch = Request.Form["branch"];
	if(branch == null || branch == "")
		branch = "1";
	string email = Request.Form["email"];
	string email_old = Request.Form["email_old"];
//	string password = Request.Form["password"];
//	string password_old = Request.Form["password_old"];
//	if(password != null && password != "")
//		password = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
	string type = Request.Form["type"];
	string name = Request.Form["name"];
	string short_name = Request.Form["short_name"];
	string gst_rate = Request.Form["gst_rate"];
	string company = Request.Form["company"];
	string trading_name = Request.Form["trading_name"];
	string corp_number = Request.Form["corp_number"];
	string barcode = Request.Form["barcode"];
	string directory = Request.Form["directory"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string postal1 = Request.Form["postal1"];
	string postal2 = Request.Form["postal2"];
	string postal3 = Request.Form["postal3"];
	string city = Request.Form["city"];
	string country = Request.Form["country"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];
	string dealer_level = Request.Form["dealer_level"];
	Trim(ref dealer_level);
	if(dealer_level == "")
		dealer_level = "1";
	double npurchase_average = MyMoneyParse(Request.Form["purchase_average"]);
	double npurchase_nza = MyMoneyParse(Request.Form["purchase_nza"]);
	int ncredit_term = MyIntParse(Request.Form["credit_term"]);
	double ncredit_limit = MyMoneyParse(Request.Form["credit_limit"]);
	string purchase_nza = npurchase_nza.ToString();
	string purchase_average = npurchase_average.ToString();
	string purchase_average_old = Request.Form["purchase_average_old"];
	string credit_term = ncredit_term.ToString();
	string credit_limit = ncredit_limit.ToString();
//	if(credit_term == null || credit_term == "")
//		credit_term = 0;
//	if(credit_limit == null || credit_limit == "")
//		credit_limit = 0;

	string rights = Request.Form["rights"];
	string note = Request.Form["note"];
	string access_level = rights;//GetEnumID("access_level", rights);

//	string pm_name = Request.Form["pm_name"]; // == name
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
	string cat_access = Request.Form["cat_access"];
	string cat_access_group = Request.Form["cat_access_group"];
	string nameb = Request.Form["nameb"];
	if(cat_access_group == null || cat_access_group == "")
		cat_access_group = "0";
	string currency_for_purchase = Request.Form["currency_for_purchase"];

	string approved = "0";
	if(Request.Form["approved"] == "on" || Request.Form["approved"] == "1")
		approved = "1";

	string stop_order = "0";
	if(Request.Form["stop_order"] == "on" || Request.Form["stop_order"] == "1")
		stop_order = "1";

	string no_sys_quote = "0";
	if(Request.Form["no_sys_quote"] == "on" || Request.Form["no_sys_quote"] == "1")
		no_sys_quote = "1";

	string stop_order_reason = Request.Form["stop_order_reason"];

	if(email == null || email == "")
	{
		email = FakeEmail();
	}
	else
	{
		Trim(ref email);
//		email = EncodeQuote(email);
	}

	string sales = Request.Form["sales"];
	if(sales == null)
		sales = "null";

	name = EncodeQuote(name);
	company = EncodeQuote(company);
	trading_name = EncodeQuote(trading_name);
	barcode = EncodeQuote(barcode);
	phone = EncodeQuote(phone);
	fax = EncodeQuote(fax);
	address1 = EncodeQuote(address1);
	address2 = EncodeQuote(address2);
	address3 = EncodeQuote(address3);
	postal1 = EncodeQuote(postal1);
	postal2 = EncodeQuote(postal2);
	postal3 = EncodeQuote(postal3);
	city = EncodeQuote(city);
	country = EncodeQuote(country);
	note = EncodeQuote(note);

	Trim(ref email);
	Trim(ref email_old);
	email = EncodeQuote(email);
	email_old = EncodeQuote(email_old);

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

	cat_access = EncodeQuote(cat_access);

	if(m_cmd == "Update")
	{
		string sc = "UPDATE card SET ";
		if(email != email_old)
		{
			if(IsDuplicateEmail(email))
			{
				Response.Write("<br><br><center><h3>Error, this email address has already been used</h3>");
				return false;
			}
			sc += " email='" + email + "', ";
		}
		sc += " our_branch=" + branch + ", name = N'" + name + "', company = N'" + company + "', trading_name = N'" + trading_name + "',";
		sc += " corp_number='" + corp_number + "', directory=" + directory + ", gst_rate='" + gst_rate + "', ";
//		if(password != "")
//			sc += "password='" + password + "', ";
		sc += " barcode = '" + barcode + "', ";
		sc += " postal1='" + postal1 + "', postal2='" + postal2 + "', postal3='" + postal3 + "', ";
		sc += " address1='" + address1 + "', address2='" + address2 + "', address3='" + address3 + "', ";
		sc += " city='" + city + "', country='" + country + "', phone='" + phone + "', fax='" + fax + "', ";
		sc += " pm_email='" + pm_email + "', pm_ddi='" + pm_ddi + "', pm_mobile='" + pm_mobile + "', ";
		sc += " sm_name='" + sm_name + "', sm_email='" + sm_email + "', sm_ddi='" + sm_ddi + "', sm_mobile='" + sm_mobile + "', ";
		sc += " ap_name='" + ap_name + "', ap_email='" + ap_email + "', ap_ddi='" + ap_ddi + "', ap_mobile='" + ap_mobile + "', ";
		sc += " dealer_level=" + dealer_level + ", note='" + note + "', approved=" + approved + ", type=" + type + ", ";
		if(purchase_average != purchase_average_old)
		{
			sc += " purchase_average=" + purchase_average + ", ";
			sc += " m" + MyIntParse(DateTime.Now.AddMonths(-1).ToString("MM")) + "=" + purchase_average + ", ";
		}
		sc += " access_level=" + access_level + ", credit_term=" + credit_term + ", credit_limit=" + credit_limit;
		if(type == GetEnumID("card_type", "supplier"))
		{
			sc += ", short_name='" + short_name + "' ";
		}
		else if(type == GetEnumID("card_type", "dealer"))
		{
		}
		else
		{
		}
		sc += ", purchase_nza=" + purchase_nza + ", cat_access='" + cat_access + "' ";
		sc += ", cat_access_group = " + cat_access_group;
		sc += ", currency_for_purchase='" + currency_for_purchase + "'";
		sc += ", stop_order = " + stop_order + ", stop_order_reason = '" + stop_order_reason + "' ";
		sc += ", sales=" + sales;
		sc += ", no_sys_quote = " + no_sys_quote;
		sc += ", nameb = N'"+nameb+"'";
		sc += " WHERE id=" + id;
		//DEBUG("SC" , sc);
		//return false;
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
	else if(m_cmd == "Submit")
	{
		bRet = AddNewDealer(email, "", trading_name, corp_number, directory, gst_rate, currency_for_purchase, company, address1, address2, 
			address3, phone, fax, postal1, postal2, postal3, access_level, dealer_level, note, 
			credit_term, credit_limit, approved, false, purchase_nza, cat_access, name, pm_email, pm_ddi, pm_mobile, sm_name, sm_email, 
			sm_ddi, sm_mobile, ap_name, ap_email, ap_ddi, ap_mobile, short_name, type, barcode, branch);
		if(bRet)
		{
//DEBUG("here", 0);
			if(Session["new_card_id"] != null)
				m_id = MyIntParse(Session["new_card_id"].ToString());
//DEBUG("m_id=", m_id);
		}
	}
	return bRet;
}

string FakeEmail()
{
	DataSet dst = new DataSet();
	int rows = 0;
	
	string sc = "SELECT TOP 1 id from card ORDER BY id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "fakeemail");
		if(rows <= 0)
			return "0";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return (int.Parse(dst.Tables["fakeemail"].Rows[0]["id"].ToString())+1).ToString();
}

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("<html><style type=\"text/css\">td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
//	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>");
//	sb.Append("<body bgcolor=#666696>");
//	sb.Append("<table bgcolor=white width=100% height=100% align=center><tr><td valign=top>");
//////////////////////////////
    /*
	 sb.Append ("<script type=text/javascript");
	 sb.Append (">" );
	 sb.Append  (" function checkform(){");
	 sb.Append ("if( document.card.company.value ==''){");
	 sb.Append ("alert (\"Please Compnay's Name Field is required. Please type 'N/A' if not available\");event.returnValue=false; document.card.company.style.backgroundColor='#FF0000';");
	 sb.Append ("document.card.company.focus();}");
	 sb.Append (" else {if(document.card.trading_name.value ==''){");
	 sb.Append ("alert (\"Pleases Trading Name is required. Please type 'N/A' if not available\");event.returnValue=false; document.card.trading_name.style.backgroundColor='#FF0000';");
	 sb.Append (" document.card.trading_name.focus();}  ");
	 sb.Append ("else {if(document.card.name.value ==''){");
	 sb.Append ("alert (\"Pleases Name is required\");event.returnValue=false;  document.card.name.style.backgroundColor='#FF0000';");
	 sb.Append ("document.card.name.focus(); }");
	 sb.Append ("else { if(document.card.address1.value ==''){alert (\"Pleases Address 1 is required\");event.returnValue=false;");
	 sb.Append ("document.card.address1.style.backgroundColor='#FF0000'; ");
	 sb.Append ("document.card.address1.focus();}");
	 sb.Append ("else { if(document.card.phone.value ==''){alert (\"Pleases phone is required\");event.returnValue=false;");
	 sb.Append ("document.card.phone.style.backgroundColor='#FF0000'; ");
	 sb.Append ("document.card.phone.focus();}");
	 sb.Append ("else { if(document.card.Fax.value ==''){alert (\"Pleases fax is required. Please type 'N/A' if not available\");event.returnValue=false;");
	 sb.Append ("document.card.Fax.style.backgroundColor='#FF0000';");
	 sb.Append ("document.card.Fax.focus();}");
	 sb.Append ("else { if(document.card.email.value ==''){alert (\"Please email is required, otherwise you cannot login system\");event.returnValue=false;");
	 sb.Append ("document.card.email.style.backgroundColor='#FF0000'; ");
	 sb.Append ("document.card.email.focus();}");
     sb.Append ("else{ var text = document.card.email.value;var  regexp = new RegExp(\"^[a-zA-Z0-9/.]+@[a-zA-Z0-9/.]+.[a-zA-Z/.]+$\");if (text.match(regexp)){return true;}");
	 sb.Append ("else { window.alert(\"Please make sure the Email field has filled correctly\"); event.returnValue=false;");
	 sb.Append ("document.card.email.style.backgroundColor = '#FF0000'; document.card.email.focus();}");
	 sb.Append ("}");
	 sb.Append ("}"); 
	 sb.Append ("}");
	 sb.Append ("}");
	 sb.Append ("}"); 
	 sb.Append("} ");
	 sb.Append ("}");
	 sb.Append ("}");
	 sb.Append ( "</script");
	 sb.Append (">");
	 */
	sb.Append("<form action=ecard.aspx?id=" + m_id + " method=post name=card onSubmit=\"checkform()\">\r\n");
	Response.Write(sb.ToString());
	Response.Flush();
}
   
void WriteFooter()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("</form>");//</td></tr></table></body></html>");
	Response.Write(sb.ToString());
}
////////////////////////////////////////////////////
void DrawAppProcessForm(string s_pass)
{
	string s_subject = "";
	string s_body = "";

	if(!GetApplicant())
		return;

	s_subject = ReadSitePage("approval_mail_subject");
	s_body = ReadSitePage("approval_mail_body");

	DataRow dr = dst.Tables["card"].Rows[0];

	s_body = s_body.Replace("@@login_name", dr["email"].ToString());
	s_body = s_body.Replace("@@s_password", s_pass);

	Response.Write("<form action=ecard.aspx?id=" + m_id + " method=post>");
	Response.Write("<br><br><table bgcolor=white align=center valign=center cellspacing=3 cellpadding=0 border=0>\r\n");
	Response.Write("<tr><td colspan=2><font size=2><b>Applicant:</b></font></td></tr>");
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>\r\n");
	Response.Write("<tr><td><b>Company Legal Name: </b><i>" + dr["company"].ToString() + "</i></td>");
	Response.Write("<td align=right><b>Auto-generated Password : </b>");
	Response.Write("<input type=text size=10 name=pwd value='" + s_pass + "'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td><b>Company Trading Name: </b><i>" + dr["trading_name"] + "</i></td>");
	Response.Write("<td align=right>");//<font size=+2 color=red><b>" + s_pass + "</b></font>&nbsp;&nbsp;");
//	Response.Write("<input type=text name=pwd value='" + s_pass + "'>");
	Response.Write("<input type=submit name=cmd value='Generate' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Apply Password' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=2><br><br><b>E-mail To: <font color=blue>" + dr["email"].ToString() + "</font></b>");
	Response.Write("<input type=hidden name=mailto value='" + dr["email"].ToString() + "'></td></tr>\r\n");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2><b>Subject:</b>&nbsp;&nbsp;<input type=text name=subject size=80 value='" + s_subject + "'></td></tr>\r\n");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2><b>E-mail Body Text:</b></td></tr>\r\n");
	Response.Write("<tr><td colspan=2><textarea name=mailbody rows=20 cols=80>" + s_body + "</textarea></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2 align=center><button onClick=window.location=('ecard.aspx?id=" + Request.QueryString["id"]);
	Response.Write("&r=" + DateTime.Now.ToOADate() + "') " + Session["button_style"] + ">Cancel</button>&nbsp;&nbsp;");
	Response.Write("<input type=submit name=cmd value='Send Mail' " + Session["button_style"] + "></td></tr>\r\n");
	Response.Write("</form>");
	Response.Write("</table>\r\n");

}

bool DoApprove()
{
	string sc = "UPDATE card SET approved=1 WHERE id=" + Request.QueryString["id"];
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

bool GetApplicant()
{
	string sc = "SELECT * FROM card WHERE id=" + Request.QueryString["id"];
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "card") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool MailProcessing()
{
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = Request.Form["mailto"];
	msgMail.From = GetSiteSettings("account_manager_email");
	msgMail.Subject = Request.Form["subject"];
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = Request.Form["mailbody"];
//DEBUG("mail from : ", GetSiteSettings("account_manager_email"));
	SmtpMail.Send(msgMail);

//	msgMail.To = Session["email"].ToString(); //backup copy
//	SmtpMail.Send(msgMail);	
//DEBUG("here", " sent");

	if(!UpdatePwd())
		return false;
	return true;
}

bool UpdatePwd()
{
	string newpwd = Request.Form["pwd"];
	newpwd = FormsAuthentication.HashPasswordForStoringInConfigFile(newpwd, "md5");	
	string sc = "UPDATE card SET password='" + newpwd + "' WHERE id=" +  Request.QueryString["id"];
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

bool PrintEditMemberForm()
{
	PrintAdminHeader();
	PrintAdminMenu();

	string name = "";
	string phone = "";
	string points = "0";

	if(m_id != -1)
	{
		string sc = " SELECT name, phone, points FROM card WHERE id = " + m_id;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dst, "member") <= 0)
			{
				Response.Write("<br><center><h3>Card Not Found</h3>");
				return false;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		DataRow dr = dst.Tables["member"].Rows[0];
		name = dr["name"].ToString();
		phone = dr["phone"].ToString();
		points = dr["points"].ToString();
	}

	if(m_id == -1)
		Response.Write("<br><center><h4>New Member</h4>");
	else
		Response.Write("<br><center><h4>Edit Member</h4>");

	Response.Write("<form action=ecard.aspx?t=member&id=" + m_id + " method=post>");
	Response.Write("<table bgcolor=white align=center valign=center cellspacing=3 cellpadding=1 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>&nbsp; Name &nbsp&nbsp&nbsp&nbsp; </b></td><td><input name=name value='" + name + "'></td></tr>");	
	Response.Write("<tr><td><b>&nbsp; Phone </b></td><td><input name=phone value='" + phone + "'></td></tr>");	
	Response.Write("<tr><td><b>&nbsp; Points </b></td><td><input name=points value='" + points + "'></td></tr>");	
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Save class=b></td></tr>");
	Response.Write("</table></form>");
	PrintAdminFooter();
	return true;
}

bool UpdateMember()
{
	PrintAdminHeader();
	PrintAdminMenu();

	string name = Request.Form["name"];
	string points = Request.Form["points"];
	if(!TSIsDigit(points))
	{
		Response.Write("<br><center><h4>Error, points must be a number");
		return false;
	}
	string phone = Request.Form["phone"];
	if(phone == "")
	{
		Response.Write("<br><center><h4>Error, phone number cannot be blank");
		return false;
	}

	string sc = " SELECT id, name FROM card WHERE type = 6 AND id <> " + m_id + " AND phone = '" + EncodeQuote(phone) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "phone") > 0)
		{
			Response.Write("<br><center><h3>Error, this phone ID has already been used by ");
			Response.Write("<a href=ecard.aspx?t=member&id=" + dst.Tables["phone"].Rows[0]["id"].ToString() + " class=o>this member</a></h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(m_id == -1)// new member
	{
		string email = FakeEmail();
		sc = " INSERT INTO card(type, name, email, phone, points) VALUES( ";
		sc += "6, '" + EncodeQuote(name) + "', '" + EncodeQuote(email) + "', '" + EncodeQuote(phone) + "', " + points + ") ";
	}
	else
	{
		sc = " UPDATE card SET name = '" + EncodeQuote(name) + "', phone = '" + EncodeQuote(phone) + "', points = " + points;
		sc += " WHERE id = " + m_id;
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
	if(m_id == -1)
		Response.Write("<br><center><h4>New Member Added</h4>");
	else
		Response.Write("<br><center><h4>Member Updated</h4>");
	return true;
}

 
</script>

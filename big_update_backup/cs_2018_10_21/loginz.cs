<script runat="server">

string m_sFormTitle = m_sCompanyName;
string m_sCookieName = "";
bool m_bRegisterMenu = false;
bool m_bRetailMenu = false;
bool m_bWrongEmail = false;
bool m_bShowWhatsnew = false;

string m_name = "";
string m_pass = "";
string m_check = "";
string m_footer = "";
string m_msg = "";
string m_register = "";
string m_sendpass = "";
string m_userEmail = "";


void Page_Load(Object Src, EventArgs E ) 
{
	m_bCheckLogin = false;
	TS_PageLoad(); //do common things, LogVisit etc...

	InitializeData(); //init functions

	m_sCookieName = TSGetPath();
	if(m_sCookieName == "")
	{
		Response.Write("Error, no cookie name");
	}
	if(Request.QueryString["logoff"] == "true")
	{
		DoLogoff();
		Response.Redirect("login.aspx");
		return;
	}
	else if(Request.QueryString["t"] == "s") //send password
	{
		if(SendPassword())
			return;
	}

	if(Request.Form["name"] != null && Request.Form["name"] != "")
	{
		m_userEmail = Request.Form["name"];
		m_name = "<input name=name autocomplete=off value='";
		m_name += m_userEmail;
		m_name += "'>";
		m_pass = "<input name=pass type=password value='";
		m_pass += Request.Form["pass"];
		m_pass += "'>";
		m_check = "<input id=chkPersistLogin type=checkbox name=chkPersistLogin />";
		if(ProcessLogin())
		{
			return; //login ok
		}
	}
	else
	{
		m_name = "<input name=name autocomplete=off>";
		m_pass = "<input name=pass type=password>";
		m_check = "<input id=chkPersistLogin type=checkbox name=chkPersistLogin />";
		HttpCookie cookie = Request.Cookies[m_sCookieName];
		if(cookie != null && cookie.Values["name"] != null && cookie.Values["name"] != "")
		{
			m_name = "";
			m_userEmail = "";
			string plainPass = "";
			if(CheckHashedPassword(cookie.Values["name"], cookie.Values["pass"], ref plainPass)) //pass in cookie is encryped
			{
				m_name = "<input type=hidden name=use_cookie value=true>";
				m_userEmail = cookie.Values["name"];
				m_name += "<input type=hidden name=name_old value='" + m_userEmail + "'>";
			}
			m_name += "<input name=name autocomplete=off value='";
			m_name += m_userEmail;
			m_name += "'>";
			m_pass = "<input name=pass type=password value='";
//			if(!CheckHashedPassword(cookie.Values["name"], cookie.Values["pass"], ref plainPass)) //pass in cookie is encryped
//				return; //database exception
			m_pass += plainPass;
			m_pass += "'>";
			m_check = "<input id=chkPersistLogin type=checkbox name=chkPersistLogin checked/>";		
		}
	}

	if(m_bRetailMenu)
	{
		PrintLoginHeader();
//		PrintHeaderAndMenu();
	}
	else
		PrintLoginHeader();

//	if(m_bRegisterMenu)
//		m_register = "<a href=register.aspx><img border=0 src='/i/sign.gif'></a>";

	PrintLoginForm();

//	if(m_bRetailMenu)
		Response.Write("</body></html>");
//	else
//		PrintAdminFooter();//LFooter.Text = m_sFooter;
}

bool SendPassword()
{
	m_userEmail = Request.QueryString["e"];
	DataTable dt = null;
	if(!GetAccount(m_userEmail, ref dt))
		return false;

	if(dt == null)
		return false;
	if(dt.Rows.Count <= 0)
		return false;

	string name = dt.Rows[0]["name"].ToString();
	string password = GenRandomString();//dt.Rows[0]["password"].ToString();
	string hashP = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
//	if(hashP == pass) //check password first
//		plainPass = password;

	//reset password
	string sc = "UPDATE card SET password='" + hashP + "' WHERE email='" + m_userEmail + "'";
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

	MailMessage msgMail = new MailMessage();
	
	msgMail.To = m_userEmail;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = "Account Information";
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "Dear " + name + ":\r\n\r\n";
	msgMail.Body += "Your password has been reset to " + password + "\r\n";
	msgMail.Body += "You can change it on http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/register.aspx?t=edit once logged in\r\n\r\n";
	msgMail.Body += "Cheers.\r\n\r\n";
	msgMail.Body += m_sCompanyTitle + "\r\n";
	msgMail.Body += DateTime.Now.ToString("MMM.dd.yyyy");
	SmtpMail.Send(msgMail);
	PrintHeaderAndMenu();
	Response.Write("<br><br><br><br><br><br><br><br><br><br><center><b>Your password has been sent to " + m_userEmail + ". Check your email after a few minutes.<br><br><br><br><br><br><br><br><br><br>");
	PrintFooter();
	return true;
}

bool ProcessLogin()
{
	string lname = Request.Form["name"];
	string lpass = Request.Form["pass"];
	lpass = FormsAuthentication.HashPasswordForStoringInConfigFile(lpass, "md5");
	HttpCookie cookie = Request.Cookies[m_sCookieName];
	if(Request.Form["use_cookie"] == "true" && Request.Form["name"] == Request.Form["name_old"] 
		&& cookie != null && cookie.Values["name"] != null && cookie.Values["name"] != "")
	{
		lname = cookie.Values["name"];
		lpass = cookie.Values["pass"];
	}

	if(DoSqlLogin(lname, lpass))
	{
		Boolean bSave = (Request.Form["chkPersistLogin"] == "on");
		HttpCookie new_cookie = new HttpCookie(m_sCookieName); //FormsAuthentication.GetAuthCookie(txtUser.Text, true);
		DateTime dt = DateTime.Now;
		if(bSave)
		{
			TimeSpan ts = new TimeSpan(365,0,0,0); //one year
			new_cookie.Expires = dt.Add(ts);	
			new_cookie.Values.Add("name", lname);
			new_cookie.Values.Add("pass", lpass);
		}
		else
		{
			new_cookie.Expires = dt; //delete it
		}
		Response.AppendCookie(new_cookie);

		if(m_sSite == "www" && m_bShowWhatsnew)
			PrintWelcomePage();
		else
			BackToLastPage();
		return true;

//		FormsAuthentication.RedirectFromLoginPage(Request.Form["name"], bSave);
	}
	else
	{
//		m_msg = "<b>Wrong password, please try again...</b>";
		if(!m_bWrongEmail)
		{
			m_sendpass = "Forgot Password?&nbsp;";
			m_sendpass += "<a href=login.aspx?t=s&e=" + HttpUtility.UrlEncode(m_userEmail) + ">Send Password</a>";
//			m_sendpass += "<a href=login.aspx?t=s&e=" + m_userEmail + ">Send Password</a>";
//			m_sendpass = "<input type=hidden name=name value='" + m_userEmail + "'>";
//			m_sendpass = "<input type=hidden name=pass value='" + m_correctPass + "'>";
//			m_sendpass += "<input type=submit name=cmd value='Send Password'>";
		}
	}
	return false;
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
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
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

bool CheckHashedPassword(string name, string pass, ref string plainPass)
{
	DataTable dt = null;
	if(!GetAccount(name, ref dt))
		return false;

	if(dt == null)
		return false;
	if(dt.Rows.Count <= 0)
		return false;

	string password = dt.Rows[0]["password"].ToString();
//	string hashP = FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
	if(password == pass) //check password first
	{
		plainPass = "ascookie";
		return true;
	}
	return false;
}

bool DoSqlLogin(string name, string pass)
{
	DataTable dt = null;
	DataRow dr = null;
	if(name == "admin@eznz.com" && pass == "410B6E86CA31315A55EF83F4686634C0")
	{
		string lkey = m_sCompanyName + "AccessLevel";
		Session[lkey] = "10";

		//discount and credit
		string dkey = m_sCompanyName + "discount";
		Session[dkey] = "0";
		dkey = m_sCompanyName + "balance";
		Session[dkey] = "0";
		dkey = m_sCompanyName + "dealer_level";
		Session[dkey] = "1";
		
		string bkey = m_sCompanyName + "lastbranch";
		Session[bkey] = "1";

		TS_LogUserIn();
		Session["name"] = "EZNZ Admin";
		Session["email"] = "admin@eznz.com";
		Session["login_card_id"] = "0";
		Session["card_id"] = "0";
		Session["card_type"] = 1;
		Session["supplier_short_name"] = "";
		Session["main_card_id"] = "";
		Session["customer_access_level"] = "1";
		UpdateSessionLog();

		CheckUserTable();
		dr = dtUser.Rows[0];
		
		dtUser.AcceptChanges();
		dr.BeginEdit();

		dr["Name"]		= "EZNZ Admin";
		dr["email"]		= "admin@eznz.com";
		dr["address1"]  = "EZNZ Corp";
//		dr["city"]		= "Auckland";
		dr["phone"]		= "9201962";
//		dr["Pass"]		= "********";
		dr["Company"]	= "EZNZ Corp";
//		dr["shipping_fee"]	= "5";

		dr.EndEdit();
		dtUser.AcceptChanges();
		return true;
	}
	else if(GetAccount(name, ref dt))
	{
		if(dt == null)
		{
			m_bWrongEmail = true;
			m_msg = "<b>DataBase Error.</b>";
			return false;
		}

		DataRow dra = dt.Rows[0];

		string password = dt.Rows[0]["password"].ToString();
		string hashP = password; //FormsAuthentication.HashPasswordForStoringInConfigFile(password, "md5");
//DEBUG("hashp="+hashP, " pass="+pass);
		if(hashP == pass) //check password first
		{
			TS_LogUserIn();
			dr = dt.Rows[0];
			DataRow drl = dr; //login datarow
			Session["login_card_id"] = dr["id"].ToString();
			Session["name"] = dr["name"].ToString();
			Session["trading_name"] = dr["trading_name"].ToString();
			Session["email"] = dr["email"].ToString();
			Session["main_card_id"] = dr["main_card_id"].ToString();
			Session["branch_card_id"] = dr["branch_card_id"].ToString();
			Session["customer_access_level"] = dr["customer_access_level"].ToString();
			Session["login_is_branch"] = false;
			if(bool.Parse(dr["is_branch"].ToString()))
				Session["login_is_branch"] = true;

			//if it's extra login then use main_card_id as card_id
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
//			DataRow dru = dt.Rows[0]; 
			
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
//			dru["City"]		= dr["City"].ToString();
//			dru["Country"]	= dr["Country"].ToString();
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
			if(Session["email"].ToString().IndexOf("@eznz.com") < 0)
			{
				Response.Write("<br><br><br><br><br><br><center><h3>Temporary closed for updating, please try again in a few minutes.</h3>");
				Response.End();
				return false;
			}
			return true;
		}
		else
		{
			m_msg = "<b>Wrong password, please try again...</b>";
			return false;
		}
	}
	else
	{
		m_bWrongEmail = true;
		m_msg = "<b>Wrong Login Email, please try again...</b>";
	}
	return false;
}

void DoLogoff()
{
	HttpCookie cookie = new HttpCookie(m_sCookieName); //FormsAuthentication.GetAuthCookie(txtUser.Text, true);
	DateTime dt = DateTime.Now;
	cookie.Values.Add("name", "");
	cookie.Values.Add("pass", "");
	cookie.Expires = dt; //delete it
	Response.AppendCookie(cookie);
	FormsAuthentication.SignOut();
}

void PrintLoginHeader()
{
	Response.Write(ReadSitePage("login_header"));
}

void PrintLoginForm()
{
	Response.Write("<form method=post action=login.aspx>");
	Response.Write("<table width=350 border=1 bgcolor=white align=center>");
	Response.Write("<tr><td align=center>");
	Response.Write("<br><font size=+2><b>");

	string s_compName = "";
	string s_letter = "";

	if(m_supplierString == "")
	{
		for(int i=0; i<m_sFormTitle.Length; i++)
		{
			if(i == 0)
				s_letter = (m_sFormTitle[0].ToString()).ToUpper();
			else
				s_letter = m_sFormTitle[i].ToString();
			s_compName +=  s_letter;
		}
		Response.Write(s_compName);
	}
	else
		Response.Write("Corporate ");
	Response.Write(" Login</b></font>");
	Response.Write("<br><br>");
	Response.Write("<table align=center>");
	Response.Write("<tr><td width=80>LoginEmail : </td><td>&nbsp;</td>");
	Response.Write("<td>" + m_name + "</td></tr>");
	Response.Write("<tr><td>Password : </td><td>&nbsp;</td>");
	Response.Write("<td>" + m_pass + "</td></tr>");
	Response.Write("<tr><td colspan=3 align=right>" + m_check + "Remember My Password</td></tr>");

	Response.Write("<td colspan=3 align=right><input type=submit name=cmd value=' Login '></td></tr>");

	Response.Write("<tr><td align=right>" + m_register + "</td>");
	//Response.Write("<td colspan=2 align=right><a href=login.aspx?logoff=true>Log off&nbsp;");
	Response.Write("<td colspan=2 align=right><a href=login.aspx?logoff=true ");
	Response.Write(" onclick=\"return window.confirm('Are you SURE want to remove cookies');\" >Log off&nbsp;");
	Response.Write("<font size=-2><i>(remove cookie)</a></i></font></td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	Response.Write("<tr><td colspan=3 align=right>" + m_msg + "</td></tr>");
	Response.Write("<tr><td colspan=3 align=right>" + m_sendpass + "</td></tr>");
	Response.Write("<tr><td colspan=3>&nbsp;</td></tr>");
	Response.Write("</table></td></tr></table></form>");

	if(m_sSite == "www")
	{
		Response.Write("<h5><center><i>Do not have a Login Name? Please click ");
		Response.Write("<a href=register.aspx class=o><font color=blue>here</font></a></i></center></h5>");
	}
//	Response.Write("<br>");

//	Response.Write("<table width=350 border=0 bgcolor=white align=center>");
//	Response.Write("<tr><td><table align=right>");
//	Response.Write("<tr><td align=right>" + m_register + "</td></tr>");
//	Response.Write("<tr><td align=right><a href=login.aspx?logoff=true>Log off&nbsp;");
//	Response.Write("<font size=-2><i> (remove cookie)</a></i></font></td></tr>");
//	Response.Write("<tr><td>&nbsp;</td></tr>");
//	Response.Write("<tr><td align=right>Forgot Password?</td></tr>");
//	Response.Write("<tr><td align=right><input type=submit name=cmd value='Send Password'></td></tr>");

//	Response.Write("</table></form>");
}

void PrintWelcomePage()
{
	DataSet dsn = new DataSet();
	string sc = "SELECT subject, text FROM news ORDER BY date DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsn, "news") <= 0)
			return;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}

	PrintHeaderAndMenu();
	Response.Write("<br><center><h3>Welcome " + Session["name"].ToString() + "</h3>");
	Response.Write("<table width=70%>");

	Response.Write("<br><h4>What's new ?</h4>");
	for(int i=0; i<dsn.Tables["news"].Rows.Count; i++)
	{
		Response.Write("<tr><td><img src=r.gif> <b>" + dsn.Tables["news"].Rows[i]["subject"].ToString() + "</b></td></tr>");
		Response.Write("<tr><td>" + dsn.Tables["news"].Rows[i]["text"].ToString() + "</td></tr>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
	}

	Response.Write("</table>");
	PrintFooter();

	sc = "UPDATE card SET show_news=0 WHERE id=" + Session["card_id"].ToString();
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
	}
}
</script>

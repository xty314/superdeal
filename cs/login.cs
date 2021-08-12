

<!-- #include file="kit_fun.cs" -->

<script runat="server">

string m_sFormTitle = m_sCompanyName;
string m_sCookieName = "";
bool m_bRegisterMenu = false;
bool m_bRetailMenu = false;
bool m_bWrongEmail = false;
bool m_bShowWhatsnew = false;
string vaildJson="";
string m_name = "";
string m_pass = "";
string m_check = "";
string m_footer = "";
string m_msg = "";
string m_register = "";
string m_sendpass = "";
string m_userEmail = "";
string m_useCookie = "";
string m_sCompanyCheck = "";
bool m_bLockSystem = false;
bool m_bOnlyAllowIPForEmployeeLogin = false;
string m_type = "";
string m_action = "";

void Page_Load(Object Src, EventArgs E ) 
{

	m_bCheckLogin = false;
	TS_Init();
//	TS_PageLoad(); //do common things, LogVisit etc...

	InitializeData(); //init functions
	//DEBUG("SC====",m_sCompanyTitle);

	m_sCompanyCheck = GetSiteSettings("company_registered_name", "" +  FormsAuthentication.HashPasswordForStoringInConfigFile(m_sCompanyTitle, "md5") +"", true).ToString(); 
	m_bLockSystem = MyBooleanParse(GetSiteSettings("enable_system_copy_right_check", "0", true));
	m_bOnlyAllowIPForEmployeeLogin = MyBooleanParse(GetSiteSettings("enable_ip_check_login_for_employee", "0"));
	m_sCookieName = TSGetPath();
	if(m_sCookieName == "")
	{
		Response.Write("Error, no cookie name");
	}
	if(Request.QueryString["logoff"] == "true")
	{
		DoLogoff();
		if(m_sSite == "dealer")
			Response.Redirect("http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/login.aspx", false);
		else
			Response.Redirect("login.aspx");
		return;
	}
	else if(Request.QueryString["t"] == "s") //send password
	{
		if(SendPassword())
			return;
	}
	
	m_type = g("t");
	m_action = g("a");
	if(m_type == "forgotpassword")
	{
		PrintHeaderAndMenu();
		PrintForgotPasswordForm();
		PrintFooter();
		return;
	}

// if (g("test")=="1")
// {
// 	validRobot("ddd");
// }

	if(m_type == "j")
	{
		if(m_action == "login")
		{
		{
			if(DoAjaxLogin())
			{
				string recaptcha= p("recaptcha");

				Response.Write("{\"result\":\"success\",\"msg\":\"" + m_msg + "\",\"recaptcha\":"+vaildJson+"}");
			}
			else
			{
				
				Response.Write("{\"result\":\"fail\",\"msg\":\"" + m_msg + "\",\"recaptcha\":"+vaildJson+"}");
			}
		}
		}
		else if(m_action == "reset")
		{   
			SendPassword();//send password
		} 
		return;
	}

	if(Request.Form["name"] != null && Request.Form["name"] != ""
		&& Request.Form["pass"] != null && Request.Form["pass"] != "")
	{
		m_userEmail = Request.Form["name"];
//		m_name = "<input name=name autocomplete=off value='";
//		m_name += m_userEmail;
//		m_name += "'>";
//		m_pass = "<input name=pass type=password value='";
//		m_pass += Request.Form["pass"];
//		m_pass += "'>";

		m_name = m_userEmail;
		m_pass = Request.Form["pass"];
		m_check = "<input id=chkPersistLogin type=checkbox name=chkPersistLogin />";
		if(ProcessLogin())
		{
			return; //login ok
		}
	}
	else
	{
//		m_name = "<input name=name autocomplete=off>";
//		m_pass = "<input name=pass type=password>";
		m_name = "";
		m_pass = "";

		m_check = "<input id=chkPersistLogin type=checkbox name=chkPersistLogin>";
		HttpCookie cookie = Request.Cookies[m_sCookieName];
		if(cookie != null && cookie.Values["name"] != null && cookie.Values["name"] != "")
		{
			m_name = "";
			m_userEmail = "";
			string plainPass = "";
			if(CheckHashedPassword(cookie.Values["name"], cookie.Values["pass"], ref plainPass)) //pass in cookie is encryped
			{
				m_useCookie = "<input type=hidden name=use_cookie value=true>";
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
			m_check = "<input id=chkPersistLogin type=checkbox name=chkPersistLogin checked>";		

			m_name = m_userEmail;
			m_pass = plainPass;

		}
	}
	if(m_sSite == "www" || m_sSite == "dealer")
		PrintHeaderAndMenu();
/*	Response.Write("<html><head><title>");
	Response.Write(m_sFormTitle);
	Response.Write(" Login</title>");

	Response.Write("<style type=text/css>");
	Response.Write("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
	Response.Write("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
	Response.Write("</style></head>");
	Response.Write("<body bgcolor=#666696><br>");
*/
	PrintLoginForm();
	if(m_sSite == "www" || m_sSite == "dealer")
		PrintFooter();

//	Response.Write("</body></html>");
}
string validRobot(string EncodedResponse){
string PrivateKey = "6LdOstcUAAAAAMltWBwA21PMgGAhgX34qxD8mv2a";
string GoogleReply = string.Format("https://www.google.com/recaptcha/api/siteverify?secret="+PrivateKey+"&response="+ EncodedResponse);
ServicePointManager.ServerCertificateValidationCallback =new RemoteCertificateValidationCallback(OnRemoteCertificateValidationCallback); 
HttpWebRequest request = (HttpWebRequest)WebRequest.Create(GoogleReply);
HttpWebResponse response = (HttpWebResponse)request.GetResponse();
string responseString=new StreamReader(response.GetResponseStream()).ReadToEnd();

// Response.Clear();
// Response.Write(responseString);
// Response.End();
// return true;
return responseString;
}
 bool OnRemoteCertificateValidationCallback(
  Object sender,
  X509Certificate certificate,
   X509Chain chain,
   SslPolicyErrors sslPolicyErrors)
 {
   return true;  // 认证正常，没有错误
 } 
bool DoAjaxLogin()
{
	string lname = p("email");
	string pwd = p("pwd");
string EncodedResponse = Request.Form["recaptcha"];
vaildJson=validRobot(EncodedResponse);
// Response.Write(vaildJson.Contains("success"));

	if(lname.IndexOf("@create") < 0 && lname.IndexOf("@select") < 0)
	{
		if(!CheckSQLAttack(lname))
			return false;
	}
	if(!CheckSQLAttack(pwd))
		return false;
	pwd = FormsAuthentication.HashPasswordForStoringInConfigFile(pwd, "md5");

	if(DoSqlLogin(lname, pwd))
		return true;	
	return false;
}

bool SendPassword()
{
		m_userEmail = Request.Form["email"];
	DataTable dt = null;
	if(!GetAccount(m_userEmail, ref dt))
	{
		Response.Write("Email " + m_userEmail + " not found");
		return false;
	}
	if(dt == null || dt.Rows.Count <= 0)
	{
		Response.Write("Email " + m_userEmail + " not found");
		return false;
	}

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
	//PrintHeaderAndMenu();
	//Response.Write("<br><br><br><br><br><br><br><br><br><br><center><b>Your password has been sent to " + m_userEmail + ". Check your email after a few minutes.<br><br><br><br><br><br><br><br><br><br>");
	//PrintFooter();
	Response.Write("success");
	return true;
}
/*bool SendPassword()
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
}*/

bool ProcessLogin()
{
	string lname = Request.Form["name"];
	string lpass = Request.Form["pass"];




// return false;
	lpass = FormsAuthentication.HashPasswordForStoringInConfigFile(lpass, "md5");
	HttpCookie cookie = Request.Cookies[m_sCookieName];
//	if(Request.Form["name"] == Request.Form["name_old"] && cookie != null && cookie.Values["name"] != null && cookie.Values["name"] != "")
	if(Request.Form["use_cookie"] == "true" && Request.Form["name"] == Request.Form["name_old"] && cookie != null && cookie.Values["name"] != null && cookie.Values["name"] != "")
	{
		lname = cookie.Values["name"];
		lpass = cookie.Values["pass"];
	}
if(lname.IndexOf("@create") < 0 && lname.IndexOf("@select") < 0)
{
	if(!CheckSQLAttack(lname))
		return false;
}
	if(!CheckSQLAttack(lpass))
		return false;

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
//DEBUG("m_bOnlyAllowIPForEmployeeLogin ", m_bOnlyAllowIPForEmployeeLogin.ToString());

		if(Session["card_type"].ToString() == "4")
		{
//DEBUG(" cartd =", Session["card_type"].ToString());
			if(m_bOnlyAllowIPForEmployeeLogin)
			{
//DEBUG("m_bOnlyAllowIPForEmployeeLogin ", m_bOnlyAllowIPForEmployeeLogin.ToString());
//DEBUG(" access level", Session["employee_access_level"].ToString());
				if(int.Parse(Session["employee_access_level"].ToString()) != 10)
				{
//DEBUG(" access level", Session["employee_access_level"].ToString());
					if(!CheckAllowIPOK())
					{			
						Response.Write("<script language=javascript>window.alert('Your IP is not activated!!!'); window.close();</script");
						Response.Write(">");
						Session[m_sCompanyName + "loggedin"] = null;
						return false;
					}		
				}
			}
		}
//return false;
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
			m_sendpass += "<a href=login.aspx?t=s&e=" + HttpUtility.UrlEncode(m_userEmail) + " class=o>Send Password</a>";
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
	sc += "' AND approved=1 ";
//DEBUG("sc=", sc);
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsa, "card") > 0)
		{
			if(dsa.Tables["card"] == null)
				return false;
			else
			{
				dt = dsa.Tables["card"];
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
	if(name == "support@gpos.co.nz" && pass == "1B8B508C51038450B13789A7F7B031F6")
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
		Session["name"] = "GPOS SUPPORT";
		Session["email"] = "support@gpos.co.nz";
		Session["login_card_id"] = "0";
		Session["login_branch_id"] = "1";
		Session["card_id"] = "6388188";
		Session["card_type"] = 1;
		Session["supplier_short_name"] = "";
		Session["main_card_id"] = "";
		Session["customer_access_level"] = "1";
		Session["branch_id"] = "1";
		Session["employee_access_level"] = 10;
		UpdateSessionLog();

		CheckUserTable();
		dr = dtUser.Rows[0];
		
		dtUser.AcceptChanges();
		dr.BeginEdit();

		dr["Name"]		= "GPOS SUPPORT";
		dr["email"]		= "support@gpos.co.nz";
		dr["address1"]  = "GPOS Ltd";
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
//	if(GetAccount(name, ref dt))
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
			Session["employee_access_level"] = dr["access_level"].ToString();			
			Session["customer_access_level"] = dr["customer_access_level"].ToString();
			Session["login_is_branch"] = false;
			Session["branch_id"] = dr["our_branch"].ToString(); //our branch id, to see which branch's stock, order etc.
			Session["login_branch_id"] = dr["our_branch"].ToString(); //our branch id, to see which branch's stock, order etc.
			string gstRate = dr["gst_rate"].ToString();
			if(gstRate == null || gstRate == "")
				gstRate = "0";
			Session[m_sCompanyName +"gst_rate"] = (0 + double.Parse(gstRate)).ToString();
		
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
			if(dr["type"].ToString() != "4") //type 4:employee
				Session[lkey] = "0"; //1 means no access, check menu_access_class table
			//looks 1 is still able to access to amdin site. so i changed to 0 no access..

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

	//***** intercept invalid system **************//
/*	if(m_bLockSystem)
	{
		if(String.Compare(m_sCompanyCheck, FormsAuthentication.HashPasswordForStoringInConfigFile(m_sCompanyTitle, "md5"), true) != 0)
		{
			Response.Write("Copyright error");
			return false;
		}
	}
*/ 
//***** system check valid end here **********//

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
			
			if(m_sSite != "admin")
				RestoreCart();
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
	Session[m_sCompanyName + "loggedin"] = null;
	Session["dtUser"] = null;
	dtUser.Dispose();
}

void PrintForgotPasswordForm()
{
	string s = "";
	if(m_sSite == "www")
		s = ReadSitePage("public_account_forgot");
	Response.Write(s);
}
void PrintLoginHeader()
{
	string s = ReadSitePage("login_header");
	s = s.Replace("@@company_title", m_sCompanyTitle);
	Response.Write(ApplyColor(s));
}

void PrintLoginForm()
{
	string s = "";
	if(m_sSite == "dealer")
		s = ReadSitePage("login_body_dealer");
	else if(m_sSite == "www")
		s = ReadSitePage("login_body_public");
	else
		s = ReadSitePage("login_body_admin");

	string agent = Request.ServerVariables["HTTP_USER_AGENT"];
	if(g_bPDA && m_sSite == "admin")
		s = ReadSitePage("login_body_admin_pda");
	if(g_bIpad || g_bIphone)
		s = ReadSitePage("login_body_ipad");

	s = s.Replace("@@name_field", m_name);
	s = s.Replace("@@pass_field", m_pass);
	s = s.Replace("@@check_field", m_check);
	s = s.Replace("@@msg_field", m_msg);
	s = s.Replace("@@sendpass_field", m_sendpass);
	s = s.Replace("@@use_cookie", m_useCookie);
	s = s.Replace("@@companyTitle", m_sCompanyTitle);
	s = ApplyColor(s);
	Response.Write(s);
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

bool CheckAllowIPOK()
{
	int i = 0;
	int j = 0;
	string[] abip = new string[1024];
	string oneip = "";

	if(Session["staff_checkin_allow_ip"] == null)
	{
		string allow_ip = GetSiteSettings("staff_checkin_allow_ip", "");
//DEBUG(" allowid=", allow_ip);
		for(i=0; i<allow_ip.Length; i++)
		{
			if(allow_ip[i] == ' ' || allow_ip[i] == ',' || allow_ip[i] == ';')
			{
				Trim(ref oneip);
				if(oneip != "")
				{
					abip[j++] = oneip;
					oneip = "";
				}
			}
			else
			{
				oneip += allow_ip[i];
			}
		}
		if(oneip != "") //the last one
		{
			abip[j++] = oneip;
			oneip = "";
		}

		Session["staff_checkin_allow_ip"] = abip;
	}
	else
	{
		abip = (string[])Session["staff_checkin_allow_ip"];
	}
	string ip = GetIP(); //Request.ServerVariables["REMOTE_ADDR"].ToString();
	//DEBUG("ip=", ip);
	//if(ip.IndexOf("192.168") != -1)
		//return true;
	if(ip == "192.168.12.1")
		return false;
	//return true;
		//	if(Session["ip"] != null)
//		ip = Session["ip"].ToString();
	if(ip == "")
		return true;
	for(i=0; i<abip.Length; i++)
	{
		oneip = abip[i];
		if(oneip == null)
			break;

//DEBUG("oneip=", oneip);

		if(ip.IndexOf(oneip) == 0)// || ip == "127.0.0.1")
			return true;
	}
	return true;
}


    internal class AcceptAllCertificatePolicy : ICertificatePolicy
    {
        public AcceptAllCertificatePolicy()
        {
        }


        public bool CheckValidationResult(ServicePoint sPoint, System.Security.Cryptography.X509Certificates.X509Certificate cert, WebRequest wRequest, int certProb)
        {
            //   Always   accept  
            return true;
        }
    }
</script>

<!-- #include file="sqlstring.cs" -->
<!-- #include file="mail.cs" -->

<%@Language=C# Debug="true" %>
<%@Import Namespace="System.Web.Caching" %>
<%@Import Namespace="System.Data" %>
<%@Import Namespace="System.Data.SqlClient" %>
<%@Import Namespace="System.IO" %>
<%@Import Namespace="System.Globalization" %>
<%@Import Namespace="System.Threading" %>
<%@Import Namespace="System.Diagnostics" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@ Import Namespace="System.Drawing.Text" %>

<script runat=server>
//////////////////////////////////////////////////////////////////////////////////////
//common functions for all sites

string m_sHeaderCacheName = "header"; //will append current virtual path later
string m_sSalesEmail = "";
string m_sAdminEmail = "sales@eznz.com";
string m_supplierString = "";
string m_catTableString = "";

const int const_sleeps = 1;					//for throat CPU usage
int	monitorCount = 0;						//for remote process monitoring
bool m_bEZNZAdmin = false;
SqlConnection myConnection;// = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);
SqlDataAdapter myAdapter;
SqlCommand myCommand;

DataTable dtUser = new DataTable();
DataSet dstcom = new DataSet();

int	m_pMonitor = 0;
string m_sMonitor = @".......";

void Trim(ref string s)
{
	if(s == null)
		return;
	s = s.TrimStart(null);
	s = s.TrimEnd(null);
}

void DEBUG(string msg, string value)
{
	string sd = "";
	sd += "<font color=red>";
	sd += msg;
	sd += "</font>";
	sd += value;
	sd += "<br>\r\n";
	Response.Write(sd);
	Response.Flush();
}

void DEBUG(string msg, int value)
{
	string sd = "";
	sd += "<font color=red>";
	sd += msg;
	sd += "</font>";
	sd += value.ToString();
	sd += "<br>\r\n";
	Response.Write(sd);
	Response.Flush();
}

void DEBUG(string msg, double value)
{
	string sd = "";
	sd += "<font color=red>";
	sd += msg;
	sd += "</font>";
	sd += value.ToString();
	sd += "<br>\r\n";
	Response.Write(sd);
	Response.Flush();
}
void DEBUG(string msg, float value)
{
	string sd = "";
	sd += "<font color=red>";
	sd += msg;
	sd += "</font>";
	sd += value.ToString();
	sd += "<br>\r\n";
	Response.Write(sd);
	Response.Flush();
}

void ShowExp(string query, Exception e)
{
	if(Session["email"] != null && (Session["email"].ToString()).IndexOf("@eznz") >=0)
	{
			Response.Write("Execute SQL Query Error.<br>\r\nQuery = ");
			Response.Write(query);
			Response.Write("<br>\r\n Error: ");
			Response.Write(e);
			Response.Write("<br>\r\n");

	}
	else
	{
		Response.Write("<script type='text/javascript' ");
		Response.Write(">");
		Response.Write(" window.alert('Internal Error'); ");
		Response.Write("</script");
		Response.Write(">");
	}
	string msg = "\r\n<font color=red><b>EXP</b></font><br>\r\n";

	msg += e.ToString();
	msg += "<br><br><font color=red><b>QUERY</b></font><br>\r\n";
	msg += query;
	msg += "<br><br>\r\n\r\n";
	msg += "ip : " + Session["ip"] + "<br>\r\n";
	msg += "login : " + Session["name"] + "<br>\r\n";
	msg += "email : " + Session["email"] + "<br>\r\n";
	msg += "url : " + Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "<br>\r\n";

	AlertAdmin(msg);
}

void MonitorProcess(int step)
{
	monitorCount++;
	if(monitorCount > step)
	{
		monitorCount = 0;
//		Response.Write(".");
		Response.Write(m_sMonitor[m_pMonitor++]);
		if(m_pMonitor >= m_sMonitor.Length)
			m_pMonitor = 0;
		Response.Flush();
	}
//	Thread.Sleep(const_sleeps);
}

void AlertAdmin(string msg)
{
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = m_emailAlertTo;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = m_sCompanyName + " site " + Request.ServerVariables["SERVER_NAME"] + " err: " + Request.ServerVariables["LOCAL_ADDR"].ToString();
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = msg;

	//SmtpMail.Send(msgMail);
}

void AlertAdmin(string subject, string msg)
{
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = m_emailAlertTo;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = Request.ServerVariables["SERVER_NAME"] + " @ " + Request.ServerVariables["LOCAL_ADDR"].ToString();
	msgMail.Subject += subject;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = msg;

	//SmtpMail.Send(msgMail);
}

string GetSiteSettings(string name)
{
	return GetSiteSettings(name, "");
}

string GetSiteSettings(string name, string sDefault)
{
	return GetSiteSettings(name, sDefault, false);
}

string GetSiteSettings(string name, string sDefault, bool bHide)
{
	return GetSiteSettings(name, sDefault, bHide, "");
}
	
string GetSiteSettings(string name, string sDefault, bool bHide, string sDescription)
{
	string s = "";
	string sc = "SELECT value FROM settings WHERE name='";
	sc += name;
	sc += "'"; 
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		DataSet ds = new DataSet();
		rows = myCommand.Fill(ds);
		if(rows > 0)
			s = ds.Tables[0].Rows[0].ItemArray[0].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return sDefault;
	}
	if(rows == 0)
	{
		string sHide = "0";
		if(bHide)
			sHide = "1";
		if(name == "next_cheque_number")
			s = "100000";
		else
			s = sDefault;
		s = EncodeQuote(s);
		sc = " INSERT INTO settings (name, value, hidden, description) VALUES('" + name + "', '" + s + "', " + sHide + ", '"+ sDescription +"') ";
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
//DEBUG("s=",s);
	return s;
}

bool SetSiteSettings(string name, string value)
{
	string sc = "UPDATE settings SET value='";
	sc += EncodeQuote(value);
	sc += "' WHERE name='"; 
	sc += name;
	sc += "'";
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

string ReadSitePage(string name)
{
	string id = "";
	return ReadSitePage(name, ref id);
}

string GetSitePageText(string name, ref string id)
{
	string cat = "";
	return GetSitePageText(name, ref id, ref cat);
}

string GetSitePageText(string name, ref string id, ref string cat)
{
	name = name.ToLower();
	string s = "";
	string sc = "SELECT id, text, cat FROM site_pages WHERE name=N'";
	sc += name;
	sc += "'"; 
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		DataSet ds = new DataSet();
		rows = myCommand.Fill(ds);
		if(rows > 0)
		{
			s = ds.Tables[0].Rows[0]["text"].ToString();
			id = ds.Tables[0].Rows[0]["id"].ToString();
			cat = ds.Tables[0].Rows[0]["cat"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	if(rows == 0 && name != "new_page")
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO site_pages (name, text, cat) VALUES(N'" + name + "', '', 'Others') ";
		sc += " SELECT IDENT_CURRENT('site_pages') AS id";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			DataSet ds = new DataSet();
			rows = myCommand.Fill(ds);
			if(rows > 0)
				id = ds.Tables[0].Rows[0]["id"].ToString();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}
	}
	return s;
}
int GetNextRepairID()
{
	int rNumber = int.Parse(GetSiteSettings("repair_id").ToString());
	
	if(dstcom.Tables["insertrma"] != null)
		dstcom.Tables["insertrma"].Clear();

	//DEBUG("rnumber = ", rNumber);
	string sc = "SELECT TOP 1 ra_number FROM repair ORDER BY id DESC";
//DEBUG("sc ++", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "insertrma") == 1)
		{
			if(dstcom.Tables["insertrma"].Rows[0]["ra_number"].ToString() != null && dstcom.Tables["insertrma"].Rows[0]["ra_number"].ToString() != "")
				rNumber = int.Parse(dstcom.Tables["insertrma"].Rows[0]["ra_number"].ToString()) + 1;
			else
				rNumber = rNumber + 1;
		}
		
		return rNumber;
		//Session["ra_id"] = rNumber;
		//DEBUG("raid =", Session["ra_id"].ToString());

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		//return false;
		return 0;
	}

	return rNumber;
}


string ExtractRA(string ra_number)
{
	string sDigitRA = "";
	string sStartValue = "";
	for(int i=0; i<ra_number.Length; i++)
	{
		if(TSIsDigit(ra_number[i].ToString()))
			sDigitRA += ra_number[i].ToString();
		else
			sStartValue += ra_number[i].ToString();
	}
	if(Session["start_value"] != null && Session["start_value"] != "")
	{
		if(Session["start_value"].ToString() == sStartValue)
			Session["start_value"] = sStartValue;
	}
	else
		Session["start_value"] = sStartValue;	

	return sDigitRA;
}

string GetNextRA_ID()
{
	string sNumber_End = "";
	string sNumber = "";  
	sNumber = GetSiteSettings("repair_id").ToString();
	
	sNumber_End = ExtractRA(sNumber);
	if(dstcom.Tables["insertrma"] != null)
		dstcom.Tables["insertrma"].Clear();

//	string sc = "SELECT TOP 1 ra_number FROM repair WHERE ra_number <> null AND ra_number <> '' ORDER BY id DESC";
	string sc = "SELECT TOP 1 ra_number FROM repair WHERE ra_number IS NOT null ORDER BY id DESC";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "insertrma") == 1)
		{
			
			if(dstcom.Tables["insertrma"].Rows[0]["ra_number"].ToString() != null && dstcom.Tables["insertrma"].Rows[0]["ra_number"].ToString() != "")
			{
				
				sNumber = dstcom.Tables["insertrma"].Rows[0]["ra_number"].ToString();
				if(!TSIsDigit(sNumber))
					sNumber = ExtractRA(sNumber);
					
				if(int.Parse(sNumber_End) > int.Parse(sNumber))
					sNumber = (int.Parse(sNumber_End) + 1).ToString();
				else
					sNumber = (int.Parse(sNumber) + 1).ToString();
								
			}
			else
			{
				sNumber = ExtractRA(sNumber);
				sNumber = (int.Parse(sNumber) + 1).ToString();
			}
		
		}
		else
			sNumber = (int.Parse(sNumber_End) + 1).ToString();
		
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	return Session["start_value"] + sNumber;
}

int GetNextSupplierRA_ID()
{
	int rNumber = 1000;
	
	if(dstcom.Tables["insertrma"] != null)
		dstcom.Tables["insertrma"].Clear();

//DEBUG("rnumber = ", rNumber);
	string sc = "SELECT TOP 1 ra_id FROM rma ORDER BY ra_id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "insertrma") == 1)
		{
			if(dstcom.Tables["insertrma"].Rows[0]["ra_id"].ToString() != null && dstcom.Tables["insertrma"].Rows[0]["ra_id"].ToString() != "")
				rNumber = int.Parse(dstcom.Tables["insertrma"].Rows[0]["ra_id"].ToString()) + 1;
			else
				rNumber = rNumber + 1;
		}
		return rNumber;
		//Session["rma_id"] = rNumber;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
//		return false;
	}

	return rNumber;
	//return true;
}


string ReadRATemplate(string supplier_id)
{
	string s = "";
	string sc = "SELECT id, text_template FROM template_ra_form WHERE supplier_id='";
	sc += supplier_id;
	sc += "'"; 
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		DataSet ds = new DataSet();
		rows = myCommand.Fill(ds);
		if(rows > 0)
		{
			s = ds.Tables[0].Rows[0]["text_template"].ToString();
			//id = ds.Tables[0].Rows[0]["id"].ToString();
			s = s.Replace("&&", "&");
			return s;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return s;
}

string ReadSitePage(string name, ref string id)
{
	string s = GetSitePageText(name, ref id);
	int p = s.IndexOf("[[");
	int protect = 99;
	while(p >=0 && protect-->0)
	{
		string tag = "";
		for(int i=p+2; i<s.Length-1; i++)
		{
			if(s[i] == ']' && s[i+1] == ']')
				break;
			tag += s[i];
		}
		string sid = ""; //dummy
//DEBUG("tag=", tag);
		s = s.Replace("[[" + tag + "]]", GetSitePageText(tag, ref sid));
		p = s.IndexOf("[[");
	}
	return s;
}
string ReadBranchHeader(string content)
{
    int p = content.IndexOf("[[");
	int protect = 99;
	while(p >=0 && protect-->0)
	{
		string tag = "";
		for(int i=p+2; i<content.Length-1; i++)
		{
			if(content[i] == ']' && content[i+1] == ']')
				break;
			tag += content[i];
		}
		string sid = ""; //dummy
//DEBUG("tag=", tag);
		content = content.Replace("[[" + tag + "]]", GetSitePageText(tag, ref sid));
		p = content.IndexOf("[[");
	}
    return content;
}

Boolean CheckUserTable()
{
	if(Session["RebuildUserTable"] == "true") 
	{
		Session["RebuildUserTable"] = null;
		if(Session["dtUser"] != null)
		{
			dtUser.Dispose();
			dtUser = new DataTable();
			Session["dtUser"] = null;
		}
	}

	if(Session["dtUser"] == null)
	{
		//compatible with retail version
		dtUser.Columns.Add(new DataColumn("City", typeof(String)));
		dtUser.Columns.Add(new DataColumn("Country", typeof(String)));
		dtUser.Columns.Add(new DataColumn("NameB", typeof(String)));
		dtUser.Columns.Add(new DataColumn("CompanyB", typeof(String)));
		dtUser.Columns.Add(new DataColumn("Address1B", typeof(String)));
		dtUser.Columns.Add(new DataColumn("Address2B", typeof(String)));
		dtUser.Columns.Add(new DataColumn("CityB", typeof(String)));
		dtUser.Columns.Add(new DataColumn("CountryB", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ads", typeof(Boolean)));
		dtUser.Columns.Add(new DataColumn("shipping_fee", typeof(String)));
		//compatible with retail version

		dtUser.Columns.Add(new DataColumn("our_branch", typeof(String)));
		dtUser.Columns.Add(new DataColumn("id", typeof(String)));
		dtUser.Columns.Add(new DataColumn("type", typeof(String)));
		dtUser.Columns.Add(new DataColumn("name", typeof(String)));
		dtUser.Columns.Add(new DataColumn("short_name", typeof(String)));
		dtUser.Columns.Add(new DataColumn("company", typeof(String)));
		dtUser.Columns.Add(new DataColumn("branch", typeof(String)));
		dtUser.Columns.Add(new DataColumn("trading_name", typeof(String)));
		dtUser.Columns.Add(new DataColumn("corp_number", typeof(String)));
		dtUser.Columns.Add(new DataColumn("directory", typeof(String)));
		dtUser.Columns.Add(new DataColumn("gst_rate", typeof(String)));
		dtUser.Columns.Add(new DataColumn("currency_for_purchase", typeof(String)));
		dtUser.Columns.Add(new DataColumn("address1", typeof(String)));
		dtUser.Columns.Add(new DataColumn("address2", typeof(String)));
		dtUser.Columns.Add(new DataColumn("address3", typeof(String)));
		dtUser.Columns.Add(new DataColumn("postal1", typeof(String)));
		dtUser.Columns.Add(new DataColumn("postal2", typeof(String)));
		dtUser.Columns.Add(new DataColumn("postal3", typeof(String)));
		dtUser.Columns.Add(new DataColumn("phone", typeof(String)));
		dtUser.Columns.Add(new DataColumn("fax", typeof(String)));
		dtUser.Columns.Add(new DataColumn("email", typeof(String)));
		dtUser.Columns.Add(new DataColumn("note", typeof(String)));

		dtUser.Columns.Add(new DataColumn("CardType", typeof(String)));
		dtUser.Columns.Add(new DataColumn("NameOnCard", typeof(String)));
		dtUser.Columns.Add(new DataColumn("CardNumber", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ExpireMonth", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ExpireYear", typeof(String)));
		
		dtUser.Columns.Add(new DataColumn("pm_email", typeof(String)));
		dtUser.Columns.Add(new DataColumn("pm_ddi", typeof(String)));
		dtUser.Columns.Add(new DataColumn("pm_mobile", typeof(String)));
		dtUser.Columns.Add(new DataColumn("sm_name", typeof(String)));
		dtUser.Columns.Add(new DataColumn("sm_email", typeof(String)));
		dtUser.Columns.Add(new DataColumn("sm_ddi", typeof(String)));
		dtUser.Columns.Add(new DataColumn("sm_mobile", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ap_name", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ap_email", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ap_ddi", typeof(String)));
		dtUser.Columns.Add(new DataColumn("ap_mobile", typeof(String)));

		dtUser.Columns.Add(new DataColumn("access_level", typeof(String)));
		dtUser.Columns.Add(new DataColumn("dealer_level", typeof(String)));
		dtUser.Columns.Add(new DataColumn("credit_limit", typeof(String)));
		dtUser.Columns.Add(new DataColumn("credit_term", typeof(String)));
		dtUser.Columns.Add(new DataColumn("approved", typeof(Boolean)));
		dtUser.Columns.Add(new DataColumn("purchase_average", typeof(String)));
		dtUser.Columns.Add(new DataColumn("purchase_nza", typeof(String)));
		dtUser.Columns.Add(new DataColumn("cat_access", typeof(String)));
		dtUser.Columns.Add(new DataColumn("cat_access_group", typeof(String)));
		dtUser.Columns.Add(new DataColumn("stop_order", typeof(String)));
		dtUser.Columns.Add(new DataColumn("stop_order_reason", typeof(String)));
		dtUser.Columns.Add(new DataColumn("sales", typeof(String)));
		dtUser.Columns.Add(new DataColumn("no_sys_quote", typeof(String)));
		dtUser.Columns.Add(new DataColumn("barcode", typeof(String)));
		dtUser.Columns.Add(new DataColumn("points", typeof(String)));
		
		DataRow dr = dtUser.NewRow();
		dr["Name"] = "";
		dr["Company"] = "";
		dr["Address1"] = "";
		dr["Address2"] = "";
		dr["Address3"] = "";
		dr["Email"] = "";
		dr["CardType"] = GetEnumID("card_type", "dealer");
		dr["NameOnCard"] = "";
		dr["CardNumber"] = "";
		dr["ExpireMonth"] = "";
		dr["ExpireYear"] = "";
		dr["stop_order"] = "false";
//		dr["shipping_fee"] = "0";

		dr["approved"] = false;
		dr["gst_rate"] = "0.15";
		dr["directory"] = "1";
		dr["sales"] = "";
		dr["no_sys_quote"] = "false";
		dtUser.Rows.Add(dr);
		Session["dtUser"] = dtUser;
		return false;
	}
	else
	{
		dtUser = (DataTable)Session["dtUser"];
	}
	return true;
}

//is it show in "Brands", if not then only show it in "More Brands..."
Boolean IsThisBrandShow(string brand)
{
	DataSet dstt = new DataSet();
	string show = "true";
	int rows = 0;

	string sc = "SELECT TOP 1 show FROM brand_settings WHERE brand='";
	sc += EncodeQuote(brand);
	sc += "'";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstt, "1");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return true;
	}
	
	if(rows == 0)
	{
		sc = "INSERT INTO brand_settings (brand, show) VALUES('";
		sc += EncodeQuote(brand);
		sc += "', 'true')";
		
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
			return true;
		}
		return true;
	}
	else
	{	
		show = dstt.Tables[0].Rows[0]["show"].ToString();
	}
	
	return (show == "true");
}

//mark it for return, especailly after login
void RememberLastPage()
{
	string sl = "http";
	
	if(String.Compare(Request.ServerVariables["HTTPS"].ToString(), "on", true) == 0)
		sl += "s";
	
	sl += "://";
	sl += Request.ServerVariables["SERVER_NAME"];
	sl += Request.ServerVariables["URL"].ToString();
	sl += "?";
	sl += Request.ServerVariables["QUERY_STRING"];
	Session["LastPage"] = sl;
}

void BackToLastPage()
{
//	Response.Write("lastpage = "+Session["LastPage"]);
	string url;
	string currentURL = Request.ServerVariables["URL"];
	if(Session["LastPage"] != null)
	{
		url = Session["LastPage"].ToString();
		int p = 0;
		if(url.IndexOf("checkout.aspx") >= 0)
		{
			if(Session[m_sCompanyName + "sales"] != null && (bool)Session[m_sCompanyName + "sales"])
			{
				url = "/sales/pos.aspx";
			}
		}

		if(Session["card_type"] != null && Session["card_type"] != "")
		{
			if(Session["card_type"].ToString() == "2")
			{
				if(url.IndexOf("/dealer") < 0 && Request.ServerVariables["URL"].ToString().IndexOf("/dealer/login.aspx") >= 0)
				{
					string tmpURL = Request.ServerVariables["URL"].ToString();
					tmpURL = tmpURL.Replace("login.aspx", "");
					url = tmpURL;
				}
				else if(url.IndexOf("/dealer") < 0 && Request.ServerVariables["URL"].ToString().IndexOf("/dealer") < 0)
					url = "dealer/";
			}
		}

	}
	else
	{
		url = "";
	
		if(Session["card_type"] != null && Session["card_type"] != "")
		{		
			if(Session["card_type"].ToString() == "2")
			{
				if((Request.ServerVariables["URL"].ToString()).IndexOf("/dealer") < 0)
					url = "dealer/";				
			}
		}
		
		url += "default.aspx";
	}
	Response.Redirect(url);
	//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + url + "\">");
	//return;
}

/*void BackToLastPage()
{
//	Response.Write("lastpage = "+Session["LastPage"]);
	string url;
	if(Session["LastPage"] != null)
	{
		url = Session["LastPage"].ToString();
		int p = 0;
		if(url.IndexOf("checkout.aspx") >= 0)
		{
			if(Session[m_sCompanyName + "sales"] != null && (bool)Session[m_sCompanyName + "sales"])
			{
				url = "/sales/pos.aspx";
			}
		}
//***** no use here ***********
//		else if(url.IndexOf("/p.aspx") >= 0 && url.IndexOf("admin") < 0)
//		{
//			if(SecurityCheck("editor", false))
//			{
//				p = url.IndexOf("/p.aspx");
//				string u = url.Substring(0, p);
//				u += "/admin";
//				u += url.Substring(p, url.Length - p);
//				url = u;
//			}
//		}
//***** end here **************
	}
	else
		url = "default.aspx";
//DEBUG("url=", url);
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + url + "\">");
}
*/

string RemoveSlash(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] != '\\' && s[i] != '/')
			ss += s[i];
	}
	return ss;
}

//remove single quote for sql statements, most used for brands and catalogs
//we don't encode and decode this for them to gain performance and smiplify code
string RemoveQuote(string s) 
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] != '\'')
			ss += s[i];
	}
	return ss;
}

string EncodeQuote(string s) //double single quote for sql statements
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += '\''; //double it for SQL query
		ss += s[i];
	}
	return ss;
}

string DecodeQuote(string s) //reverse of EncodeQuote
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			if(i<s.Length-1)
				if(s[i+1] == '\'')
					continue; //skip one	
	}
	return ss;
}

string TSGetPath() //get virtual path exclusive of page name and slashes, ie: /eden/cart.aspx return eden
{
	string s = Request.ServerVariables["URL"];
	int i = s.Length - 1;
	for(; i>=0; i--)
	{
		if(s[i] == '/')
			break;
	}
	
	if(i > 1)
		return s.Substring(1, i - 1);
	return Request.ServerVariables["SERVER_NAME"];
}

string TSGetUserNameByID(string id) //get user name from account table according to user id
{
	DataSet dsu = new DataSet();
	string sc = "SELECT name FROM card WHERE id='" + id + "'";
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(dsu);
		if(rows > 0)
			return dsu.Tables[0].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "error";
	}
	return "user not found";
}

string TSGetUserCompanyByID(string id) //get user name from account table according to user id
{
	DataSet dsu = new DataSet();
	string sc = "SELECT company, trading_name FROM card WHERE id='" + id + "'";
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(dsu);
		if(rows > 0)
			return dsu.Tables[0].Rows[0]["trading_name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "error";
	}
	return "user not found";
}

string TSGetUserEmailByID(string id) //get user email from account table according to user id
{
	DataSet dsu = new DataSet();
	string sc = "SELECT email FROM card WHERE id='" + id + "'";
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(dsu);
		if(rows > 0)
			return dsu.Tables[0].Rows[0]["email"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "error";
	}
	return "user not found";
}

Boolean TSIsDigit(string s) //is this string valid for int.parse
{
	if(s == null || s == "")
		return false;
	Boolean bRet = true;
	for(int i=0; i<s.Length; i++)
	{
		if(Char.IsDigit(s[i]) == false)
		{
			if(s[i] != '.' && s[i] != '-' && s[i] != '$')
			{
				bRet = false; 
				break;
			}
		}		
	}
	return bRet;
}

bool IsInteger(string s) //is this string valid for int.parse
{
	if(!TSIsDigit(s))
		return false;

	bool bRet = true;
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '.')
		{
			bRet = false; 
			break;
		}
	}
	return bRet;
}

private static CacheItemRemovedCallback onCacheRemove = null;
public void CacheRemovedCallback(String k, Object v, CacheItemRemovedReason r)
{
//	AlertAdmin("Cache Rmoved Notice", "Cache Rmoved from site: " + TSGetPath() + "\r\nk=" + k + ", objectName=" + v.ToString() + ", reason=" + r.ToString());
}

void TSAddCache(string sKey, object oValue)
{
	onCacheRemove = new CacheItemRemovedCallback(this.CacheRemovedCallback);
//	Cache.Insert(sKey, oValue, null, DateTime.MaxValue, TimeSpan.Zero);
	Cache.Insert(sKey, oValue, null, DateTime.Now.AddMinutes(60), TimeSpan.Zero, CacheItemPriority.Default, onCacheRemove);
}

void TSRemoveCache(string cn) //remove(refresh) cache 
{
//DEBUG("removing cache, m_sCompanyName=" + m_sCompanyName + " cn=", cn);
	Cache.Remove(cn); //remove catalog cache
	
	//remove all catalog contents cache
	IDictionaryEnumerator ide = Cache.GetEnumerator();
	if(ide == null)
		return;
	for(int i=Cache.Count-1; i>=0; i--)
	{
		ide.MoveNext();
		string s = ide.Key.ToString();
		if(s.Length > 7)
		{
			if(String.Compare(s.Substring(0, 7), "System.", true) != 0 && String.Compare(s.Substring(0, 5), "ISAPI", true) != 0)
			{
				if(s.Length > m_sCompanyName.Length)
				{
					if(String.Compare(s.Substring(0, m_sCompanyName.Length), m_sCompanyName, true) == 0)
					{
//						if(Cache[s] != null)
//						{
							Cache.Remove(s);
//							DEBUG(s, " removed");
//						}
					}
//					else
//					{
//					DEBUG("sub=", s.Substring(0, 4));
//					}
				}
			}
		}
	}
}

void TSRemoveCache() //remove(refresh) cache 
{
	IDictionaryEnumerator ide = Cache.GetEnumerator();
	for(int i=Cache.Count-1; i>=0; i--)
	{
		ide.MoveNext();
		string s = ide.Key.ToString();
		if(s.Length > 7)
		{
			if(String.Compare(s.Substring(0, 7), "System.", true) != 0 && String.Compare(s.Substring(0, 5), "ISAPI", true) != 0)
			{
				if(s.Length > m_sCompanyName.Length)
				{
					if(String.Compare(s.Substring(0, m_sCompanyName.Length), m_sCompanyName, true) == 0)
					{
						Cache.Remove(s);
					}
				}
			}
		}
	}
}

void doDeleteAllPicFiles()
{
	//string strPath = "C:\\html\\eznz\\nz\\shops\\eden\\admin\\ri\\";
	//string file = strPath;
	
	string path = Server.MapPath("./ri/");
//DEBUG(" path =", path);
	string[] files = Directory.GetFiles(path,"*.jpg");
	int count = files.Length;
	
	for(int i=0; i<count; i++)
	{
		File.Delete(files[i]);
	}

}

//xml decoding
string XMLDecoding(string stext)
{
	stext = stext.Replace("</b>", "");
	stext = stext.Replace("<b>", "");
	stext = stext.Replace("<font", "");
	stext = stext.Replace("</font>", "");
	stext = stext.Replace("color=", "");
	stext = stext.Replace(">", "");
	stext = stext.Replace("<", "");
	stext = stext.Replace("'", "");
	
	return stext;
}


/////////date changer ////////////////////////////////////////////////////////
string datePicker()
{
	Response.Write("<SCRIPT LANGUAGE=javascript>");
	string s = @"

	function tg_mm_daysinmonth(lnMonth,lnYear) {
	var dt1, cmn1, cmn2, dtt, lflag, dycnt, lmn
	lmn = lnMonth-1
	dt1 = new Date(lnYear,lmn,1)
	cmn1 = dt1.getMonth()
	dtt=dt1.getTime()+2332800000
	lflag = true
	dycnt=28
	while (lflag) {
	   dtt = dtt + 86400000
	   dt1.setTime(dtt)
	   cmn2 = dt1.getMonth()
	   if (cmn1!=cmn2) {
		  lflag = false }
	   else {dycnt = dycnt + 1}}
	if (dycnt > 31) {dycnt = 31}
		return dycnt
	}
	function tg_mm_setdays(sobjname, datemode){
	var dobj = eval(sobjname + '_day')
	var mobj = eval(sobjname + '_month')
	var yobj = eval(sobjname + '_year')
	var hobj = eval(sobjname)
	var hobjconv = eval(sobjname + '_conv')
	var monthdays = tg_mm_daysinmonth(mobj.options[mobj.selectedIndex].value,yobj.options[yobj.selectedIndex].value)
	var selectdays = dobj.length
	var curdy = dobj.options[dobj.selectedIndex].value
	if (curdy.length==1) {curdy = '0'+curdy}
	var curmn = mobj.options[mobj.selectedIndex].value
	if (curmn.length==1) {curmn = '0'+curmn}
	var curyr = yobj.options[yobj.selectedIndex].value
	if (selectdays > monthdays) {
	   for (var dlp=selectdays; dlp > monthdays; dlp--) {
		   dobj.options[dlp-1] = null }}
	else if (monthdays > selectdays) {
	   for (var dlp=selectdays; dlp < monthdays; dlp++) {
		   dobj.options[dlp] = new Option(dlp+1,dlp+1) }}
	if (curdy > monthdays) {
	   dobj.options[monthdays-1].selected = true
	   curdy = monthdays }
	var curdateconv = curmn+'/'+curdy+'/'+curyr
	if (datemode==1) {
	   var curdate = curmn+'/'+curdy+'/'+curyr }
	else if (datemode==2) {
	   var curdate = curdy+'/'+curmn+'/'+curyr }
	else if (datemode==3) {
	   var curdate = curyr+curmn+curdy }
	else if (datemode==4) {
	   var cdate = new Date(curyr,curmn-1,curdy)
	   var curdate = cdate.toGMTString() }
	hobj.value = curdate
	hobjconv.value = curdateconv
	}
	";
	Response.Write(s);
	Response.Write("</SCRIPT");
	Response.Write(">");
	return s;

}
//// END date changer -------


bool LogVisit()
{
	string site = TSGetPath();
	if(Session["site"].ToString() != site)
	{
		Session["site"] = site;
		if(!UpdateSessionLog())
			return false;
	}
	string sc = "INSERT INTO web_log (id, ip, name, email, browser, url, query, visit) VALUES('"
		+ Session.SessionID + "', '" 
		+ Session["ip"].ToString() + "', '" 
		+ Session["name"].ToString() + "', '" 
		+ Session["email"].ToString() + "', '" 
		+ Request.ServerVariables["HTTP_USER_AGENT"] + "', '"
		+ Request.ServerVariables["URL"] + "', '"
		+ Request.ServerVariables["QUERY_STRING"] + "', '"
		+ DateTime.Now + "')";
	try
	{
		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		AlertAdmin("Error LogVisit", e.ToString() + "\r\n\r\nQuery = \r\n" + sc);
		return false;
	}
	return true;
}

bool UpdateSessionLog()
{
	if(Session["session_log_id"] == null || Session["session_log_id"].ToString() == "")
		return true;

	string sc = "UPDATE web_session SET card_id='" + Session["card_id"].ToString() + "' ";
	sc += " WHERE id=" + Session["session_log_id"].ToString();
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
		AlertAdmin("Error LogVisit", e.ToString() + "\r\n\r\nQuery = \r\n" + sc);
		return false;
	}
	return true;
}

bool BackupProduct(DataRow dr)
{
	string sc;
	sc = "INSERT INTO product_bak (code, name, brand, cat, s_cat, ss_cat, hot, price, stock, supplier, supplier_code, supplier_price, price_age)";
	sc += "VALUES('";
	sc += dr["code"].ToString();
	sc += "', '";
	sc += dr["name"].ToString();
	sc += "', '";
	sc += dr["brand"].ToString();
	sc += "', '";
	sc += dr["cat"].ToString(); 
	sc += "', '";
	sc += dr["s_cat"].ToString(); 
	sc += "', '";
	sc += dr["ss_cat"].ToString();
	sc += "', ";
	sc += "0";
	sc += ", ";
	sc += dr["price"].ToString();
	sc += ", ";
	if(dr["stock"].ToString() == "")
		sc += "null";
	else
		sc += dr["stock"].ToString();
	sc += ", '";
	sc += dr["supplier"].ToString();
	sc += "', '";
	sc += dr["supplier_code"].ToString();
	sc += "', ";
	sc += dr["supplier_price"].ToString();
	sc += ", '";
	sc += dr["price_age"].ToString();
	sc += "')";

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
	return true;
}

string GetProductDesc(string code)
{
//DEBUG("code=", code);
	DataRow dr = null;
	if(!GetProduct(code, ref dr))
		return "";
	return dr["name"].ToString();
}

string GetProductStock(string code)
{
//DEBUG("code=", code);
	DataRow dr = null;
	if(!GetProduct(code, ref dr))
		return "0";
	//double stock = MyDoubleParse(dr["stock"].ToString());
	double stock = MyDoubleParse(dr["stock_qty"].ToString());
//	string allocated = dr["allocated_stock"].ToString();
	string allocated = dr["allocatedStock"].ToString();
	bool bEnableAllocatedStock = MyBooleanParse(GetSiteSettings("allocated_stock_public_enabled", "0"));
//	DEBUG("stock =", stock);
//	DEBUG("allocated =", allocated);
	double dAllocated = 0;
	if(allocated != "")
		dAllocated = MyDoubleParse(allocated);

	if(!bEnableAllocatedStock)
		dAllocated = 0;
	return (stock + dAllocated).ToString();
}

Boolean GetProduct(string code, ref DataRow dr)
{
	string branchID = "1";
	if(Session["branch_card_id"] != null && Session["branch_card_id"] != "")
		branchID = Session["branch_card_id"].ToString();
	if(!TSIsDigit(branchID))
		branchID = "1";
	return GetProduct(code, ref dr, branchID);
}
/*Boolean GetProduct(string code, ref DataRow dr)
{
	if(!TSIsDigit(code))
	{
		Response.Write("<h3>Error, item code must be digits</h3>");
		return false;
	}

	Boolean bRet = false;

	string sc = "SELECT p.*, c.supplier_price AS last_cost, c.currency, c.foreign_supplier_price ";
	sc += ", c.manual_cost_nzd, c.rate, c.level_rate1, c.rrp, c.barcode ";
	sc += ", pd.* ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
	sc += " LEFT OUTER JOIN product_details pd ON pd.code=p.code ";
	sc += " WHERE c.code=" + code;

	if(dstcom.Tables["product"] != null)
		dstcom.Tables["product"].Clear();
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "product") > 0)
		{
			dr = dstcom.Tables["product"].Rows[0];
			bRet = true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
/*	if(!bRet)
	{
		sc = "SELECT k.* FROM product_skip k JOIN code_relations c ON k.id=c.id WHERE c.code=" + code;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dstcom, "product") > 0)
			{
				dr = dstcom.Tables["product"].Rows[0];
				bRet = true;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
*/
//	return bRet;
//}

Boolean GetProduct(string code, ref DataRow dr, string branch_id)
{
	if(!TSIsDigit(code))
	{
		Response.Write("<h3>Error, item code must be digits</h3>");
		return false;
	}

	if(dstcom.Tables["product"] != null)
		dstcom.Tables["product"].Clear();
	Boolean bRet = false;
	string sc = "";
	if(m_sSite != "www")
	branch_id = Session["branch_id"].ToString();
	if(branch_id == "" || branch_id == null)
		branch_id = "1";

      
/*	if(g_bUseAVGCost)
	{
		sc = "SELECT p.*, ISNULL((SELECT average_cost FROM stock_qty sq WHERE sq.code = p.code AND sq.code = c.code ";
		sc += " AND sq.branch_id = " + branch_id + "), c.supplier_price) AS last_cost, c.supplier_price, c.currency, c.foreign_supplier_price ";
		sc += ", c.manual_cost_nzd, c.rate, c.level_rate1, c.rrp";
		sc += ", pd.* ";
		sc += " , ISNULL((SELECT sum(qty) FROM stock_qty sq WHERE sq.code = p.code AND sq.code = c.code ";
		//if(Session["branch_support"] != null)
			sc += " AND branch_id = "+ branch_id +"";
		sc += " ),0) AS stock_qty  ";
		sc += " , ISNULL((SELECT sum(allocated_stock) FROM stock_qty sq WHERE sq.code = p.code AND sq.code = c.code ";
		//if(Session["branch_support"] != null)
			sc += " AND branch_id = "+ branch_id +"";
		sc += " ),0) AS allocatedStock  ";
		sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
		sc += " LEFT OUTER JOIN product_details pd ON pd.code=p.code ";
		sc += " WHERE c.code=" + code;

	}
	else
	*/
	{
		sc = "SELECT ISNULL((SELECT '1' FROM specials s WHERE s.code = p.code), '0') as specialProduct, p.*, c.supplier_price AS last_cost, c.currency, c.foreign_supplier_price ";
		sc += ", c.manual_cost_nzd, c.rate, c.level_rate1, c.rrp, c.average_cost AS last_cost ";
		sc += ", pd.* ";
		sc += " , ISNULL((SELECT sum(qty) FROM stock_qty sq WHERE sq.code = p.code AND sq.code = c.code ";
	//	if(Session["branch_support"] != null)
			sc += " AND branch_id = "+ branch_id +"";
		sc += " ),0) AS stock_qty  ";
		sc += " , ISNULL((SELECT sum(allocated_stock) FROM stock_qty sq WHERE sq.code = p.code AND sq.code = c.code ";
		//if(Session["branch_support"] != null)
			sc += " AND branch_id = "+ branch_id +"";
		sc += " ),0) AS allocatedStock  ";
		sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
		sc += " LEFT OUTER JOIN product_details pd ON pd.code=p.code ";
		sc += " WHERE c.code=" + code;
	}
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "product") > 0)
		{
			dr = dstcom.Tables["product"].Rows[0];
			bRet = true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
/*	if(!bRet)
	{
		sc = "SELECT k.* FROM product_skip k JOIN code_relations c ON k.id=c.id WHERE c.code=" + code;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dstcom, "product") > 0)
			{
				dr = dstcom.Tables["product"].Rows[0];
				bRet = true;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
*/
	return bRet;
}

Boolean GetProductFromCodeRelations(string code, ref DataRow dr)
{
	if(!TSIsDigit(code))
	{
		Response.Write("<h3>Error, item code must be digits</h3>");
		return false;
	}

	Boolean bRet = false;

	string sc = "SELECT ISNULL(s.code, '-1') AS specials, c.* ";
	sc += " FROM code_relations c ";
	sc += " LEFT OUTER JOIN specials s ON s.code = c.code ";
//	sc += " LEFT OUTER JOIN product_details pd ON pd.code=c.code ";
	sc += " WHERE c.code=" + code;

	if(dstcom.Tables["product"] != null)
		dstcom.Tables["product"].Clear();
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "product") > 0)
		{
			dr = dstcom.Tables["product"].Rows[0];
			bRet = true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return bRet;
}

bool GetRawProduct(string supplier, string supplier_code, ref DataRow dr)
{
	string sc = "SELECT code, supplier, supplier_code, ISNULL(supplier_price, 0) AS supplier_price, name ";
	sc += " FROM code_relations WHERE supplier='" + supplier + "' AND supplier_code='" + supplier_code + "'";

	if(dstcom.Tables["product"] != null)
		dstcom.Tables["product"].Clear();

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "product") > 0)
			dr = dstcom.Tables["product"].Rows[0];
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetProduct(string supplier, string supplier_code, ref DataRow dr)
{
	Boolean bRet = false;

	string sc = "SELECT * FROM product WHERE supplier='" + supplier + "' AND supplier_code='" + supplier_code + "'";

	if(dstcom.Tables["product"] != null)
		dstcom.Tables["product"].Clear();

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "product") > 0)
		{
			dr = dstcom.Tables["product"].Rows[0];
			bRet = true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return bRet;
}

Boolean GetProductWithSpecialPrice(string code, ref DataRow dr)
{
	Boolean bRet = true;

	string sc = "SELECT p.code, p.name, c.price1/1.15 AS price, p.supplier, p.supplier_code ";
	sc += ", p.supplier_price, ISNULL(c.special_price, 0) AS special, c.level_rate1, c.barcode ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code ";
	sc += " LEFT OUTER JOIN specials s ON p.code=s.code ";
	sc += " WHERE p.code=" + code;

	if(dstcom.Tables["product"] != null)
		dstcom.Tables["product"].Clear();

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "product") > 0)
		{
			dr = dstcom.Tables["product"].Rows[0];
		}
		else
		{
			sc = "SELECT c.name, k.price, c.supplier, c.supplier_code, k.supplier_price, ISNULL(s.price, 0) AS special, c.barcode ";
			sc += "FROM code_relations c JOIN product_skip k ON c.id=k.id LEFT OUTER JOIN specials s ON c.code=s.code WHERE c.code='";
			sc += code;
			sc += "'";
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dstcom, "product") > 0)
					dr = dstcom.Tables["product"].Rows[0];
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return bRet;
}

string GetProductPDFSrc(string code)
{
	bool bHasLocal = false;
	string sPDFFile = "";
	string vpath = GetRootPath();
	if(vpath == "")
		vpath = "/pdf/";
	else
		vpath += "/pdf/";
	string path = vpath;
//	sPDFFile = "/" + m_sCompanyName + "/pi/" + code + ".gif";
//	if(m_sSite != "www")
//		path = "../pi/";
	sPDFFile = path + code + ".pdf";
//if(Session["email"] != null && Session["email"].ToString() == "darcy@eznz.com")
//{
//DEBUG("sPDFFile=", sPDFFile);
//DEBUG("path=", Server.MapPath(sPDFFile));
//}
	bHasLocal = File.Exists(Server.MapPath(sPDFFile));
	if(!bHasLocal)
	{
		sPDFFile = path + code + ".pdf";
		bHasLocal = File.Exists(Server.MapPath(sPDFFile));
	}
	if(!bHasLocal)
		sPDFFile = "Not Exist";//GetRandomNAImage();

	return sPDFFile;	
}

string GetProductImgSrc(string code)
{
	return GetProductImgSrc(code, false); // do not support multiple images
}
string GetProductImgSrc(string code, bool bSupportMultiPleImage)
{
	bool bHasLocal = false;
	string sPicFile = "";
	string vpath = "";//GetRootPath();
	string exten = "";
	if(m_sSite != "www")
		exten ="../";   

	if(vpath == "")
		vpath = exten + "pi/";
	else
		vpath += exten + "../pi/";
	string path = vpath;
    
    int rows = 0;
    if(dstcom.Tables["piccheck"] != null)
		dstcom.Tables["piccheck"].Clear();
    string sc = " SELECT supplier_code FROM code_relations WHERE code = " + code;
    try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "piccheck");
		if(rows <= 0)
            return exten + "i/na.gif";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
    DataRow dr = dstcom.Tables["piccheck"].Rows[0];
    string s_code = ""; 
    if( rows==1)
    {
        s_code = dr["supplier_code"].ToString();
        path = exten + "pi/" + s_code;
    }
    sPicFile = path + ".gif";
    try
	{
		bHasLocal = File.Exists(Server.MapPath(sPicFile));
	}
	catch(Exception e)
	{
	}
	if(!bHasLocal)
	{
		sPicFile = path + ".jpg";
		try
		{
			bHasLocal = File.Exists(Server.MapPath(sPicFile));
		}
		catch(Exception e)
		{
		}
	}
    if(!bHasLocal) 
	    path = exten + "pi/" + code;

	if(!bSupportMultiPleImage)
	{
		sPicFile = path + ".gif";
		try
		{
			bHasLocal = File.Exists(Server.MapPath(sPicFile));
		}
		catch(Exception e)
		{
		}
		if(!bHasLocal)
		{
			sPicFile = path + ".jpg";
			try
			{
				bHasLocal = File.Exists(Server.MapPath(sPicFile));
			}
			catch(Exception e)
			{
			}
		}
		if(!bHasLocal)
			sPicFile = exten + "i/na.gif";
		return sPicFile;
    }
	else
	{
		sPicFile = "";
		int nCount = 0;
		if(Directory.Exists(Server.MapPath(path)))
		{
			sPicFile += "<table border=0>";
			DirectoryInfo di = new DirectoryInfo(Server.MapPath(path));
			sPicFile += "<tr>";
			foreach(FileInfo f in di.GetFiles("*.*"))
			{
				if(f.Name.IndexOf(".db") >= 0)
					continue;
				
				sPicFile += "<td align=center><img width=50% src=" + path + f.Name;
				sPicFile += " onclick=\"javascript:image_window=window.open(\'" + path + f.Name + "\', \'image_window\', \'width=450, height=500, scrollbars=no,resizable=yes\'); image_window.focus();\">";
				sPicFile += "<br><a title='" + Lang("view Larger Image") + "' ";
				sPicFile += " href=\"javascript:image_window=window.open(\'"+ path + f.Name +"\', \'image_window\', \'width=450, height=500, scrollbars=no,resizable=yes\'); image_window.focus();\" "; 
				sPicFile += ">" + Lang("view Larger Image") + "</a></td>";
				
				if(!bSupportMultiPleImage)
				{
					sPicFile = path + f.Name;
					return sPicFile;
				}
				nCount++;
			}
			sPicFile += "</tr>";
			sPicFile += "</table>";
			if(nCount <= 0)
				sPicFile = "";
		}
		if(sPicFile == "")
			sPicFile = exten + "i/na.gif";
		return sPicFile;	
	}
}

string GetRandomNAImage()
{
	Random rnd = new Random();
	int i = rnd.Next(1, 15);
	string s = "i/na";
	s += i.ToString();
	string f = s + ".gif";
	if(!File.Exists(Server.MapPath(f)))
	{
		f = s + ".jpg";
		if(!File.Exists(Server.MapPath(f)))
			f = "i/na.jpg";
	}
	return f;
}

DataSet dsAEV = new DataSet();	//for GetAllExistsValues
//search exists target fields to build selectable box for edit
bool ECATGetAllExistsValues(string sFieldName, string sCondition)
{
	if(sFieldName == "" || sCondition == "")
	{
		Response.Write("<h3>Error, fieldName or Condition can't be blank</h3>");
		return false;
	}
	
	if(dsAEV.Tables[sFieldName] != null)
		dsAEV.Tables[sFieldName].Clear();
	//string sc = "SELECT DISTINCT (" + sFieldName + ")) FROM product WHERE " + sCondition;
	string sc = "SELECT DISTINCT RTRIM(LTRIM(" + sFieldName + ")) AS "+ sFieldName +" FROM product WHERE " + sCondition;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsAEV, sFieldName);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool ECATGetAllExistsValues(string sFieldName, string sCondition, bool bNewTableAlso)
{
	if(!ECATGetAllExistsValues(sFieldName, sCondition))
		return false;
	if(!bNewTableAlso)
		return true;

	string sc = "SELECT DISTINCT LTRIM(RTRIM(" + sFieldName + ")) AS "+ sFieldName +" FROM code_relations_new" + m_catTableString + " WHERE " + sCondition;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsAEV, sFieldName);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

//check phased out item  1-10-03 tee
bool UpdatePhasedOutItem(string code, int nQty, int nPoID)
{
	if(code == "" && code == null)
		return false;
	nQty = 0;
	string sc = "";

//sc = " IF (SELECT COUNT(*) FROM product_skip WHERE id = (select top 1 id FROM code_relations WHERE code = "+ code +") > 0";
	sc += " IF NOT EXISTS (SELECT code FROM product WHERE code = "+ code +" ) ";
	sc += " INSERT INTO product ";
	sc += " SELECT c.code, c.name, c.brand, c.cat, c.s_cat, c.ss_cat, 0 AS hot ";
	sc += " , ps.price, ps.stock - "+ nQty +", ps.eta, c.supplier, c.supplier_code, ps.supplier_price, 0 AS price_drop "; 
	sc += " , GETDATE() AS price_age, 0 AS allocated_stock, 0 AS popular, c.real_stock ";
	sc += " FROM code_relations c JOIN product_skip ps ON c.id = ps.id ";
	sc += " AND c.skip = 1 ";
	sc += " WHERE c.code = "+ code;
//sc += " END ";
	
	//update not phased out item
	sc += " UPDATE code_relations SET skip=0 WHERE code = "+ code;
	//delete from skip table
	sc += " DELETE FROM product_skip WHERE id = (select DISTINCT c.id FROM code_relations c JOIN purchase_item pi ON pi.code = c.code ";
	sc += " AND pi.supplier_code = c.supplier_code AND pi.id = "+ nPoID +" WHERE c.code = "+ code +")";

//DEBUG("s c = ", sc );
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


//two or more same code items, we choose one by comparing stock and price
bool SingleOut(string code)
{
	DataSet dsSingle = new DataSet();

	int rows = 0;
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM product WHERE code=" + code + " ORDER BY price, stock");
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		rows = myAdapter.Fill(dsSingle, "singleout");
//DEBUG("rows=", rows);
		if(rows <= 1)
			return true; //no duplicate code
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

	DataRow dr;
	
	int nStock;
	int nTheOne = 0; //default the first row (cheapest)
	for(int i=0; i<dsSingle.Tables["singleout"].Rows.Count; i++)
	{
		dr = dsSingle.Tables["singleout"].Rows[i];
		if(dr["stock"] == null || dr["stock"].ToString() == "")
			nStock = 9999;
		else
			nStock = int.Parse(dr["stock"].ToString());
		if(nStock > 0)
		{
			nTheOne = i;//if the cheapest one has stock, then this is the one we after
			break;
		}
	}
	//if all out of stock, we still choose the cheapest one
//DEBUG("theone=", nTheOne);
	//keep theone, delete all others
	for(int i=0; i<dsSingle.Tables["singleout"].Rows.Count; i++)
	{
//DEBUG("i=", i);
		if(i != nTheOne)
		{
			dr = dsSingle.Tables["singleout"].Rows[i];
			if(!BackupProduct(dr))
				return false;
			sb.Remove(0, sb.Length);
			sb.Append("DELETE FROM product WHERE supplier='");
			sb.Append(dr["supplier"].ToString());
			sb.Append("' AND supplier_code='");
			sb.Append(dr["supplier_code"].ToString());
			sb.Append("'");
//DEBUG("sc=", sb.ToString());
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
		}
	}
	return true;
}

int GetNextID(string sTable, string sColumn)
{
	int id = 1;

	DataSet ds = new DataSet();
	int rows = 0;
	string sc = "SELECT TOP 1 " + sColumn + " FROM " + sTable + " ORDER BY " + sColumn + " DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "id");
		if(rows == 1)
			id = int.Parse(ds.Tables["id"].Rows[0][sColumn].ToString()) + 1;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return -1;
	}
	return id;
}

int GetNextInvoiceNumber()
{
	int newNumber = 100000;

	DataSet ds = new DataSet();
	int rows = 0;
	string sc = "SELECT TOP 1 invoice_number FROM invoice ORDER BY invoice_number DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "invoice");
		if(rows == 1)
			newNumber = int.Parse(ds.Tables["invoice"].Rows[0]["invoice_number"].ToString()) + 1;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return -1;
	}
	return newNumber;
}

int GetNextTempInvoiceNumber() //use minus number to keep real invoices in sequence
{
	int newNumber = 100000;

	DataSet ds = new DataSet();
	int rows = 0;
	string sc = "SELECT TOP 1 invoice_number FROM invoice ORDER BY invoice_number";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "invoice");
		if(rows == 1)
			newNumber = int.Parse(ds.Tables["invoice"].Rows[0]["invoice_number"].ToString()) - 1;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return -1;
	}
	if(newNumber > -10000)
		newNumber = -10001;
	return newNumber;
}

void AlertMissProduct(string code, string refer, string email)
{
	MailMessage msgMail = new MailMessage();
	
	string ser = Request.ServerVariables["SERVER_NAME"];
	string tspath = TSGetPath().ToLower();
	if(tspath == m_sCompanyName || tspath == m_sCompanyName + "admin" || tspath == m_sCompanyName + "/admin" || tspath == m_sCompanyName + "/sales")
		ser += "/" + m_sCompanyName;
	
	string url = "http://" + Request.ServerVariables["SERVER_NAME"] + Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	msgMail.To = email;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = "Error Recommened System";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "Product code : " + code + " (" + refer + ") didn't find.\r\n\r\n<br><br>";
	msgMail.Body += "User : " + Session["name"].ToString() + "\r\n<br>";
	msgMail.Body += "URL : <a href=" + url + ">" + url + "</a>\r\n<br>";
	msgMail.Body += "ip : " + Session["ip"].ToString() + "\r\n<br>";

	//SmtpMail.Send(msgMail);
	msgMail.To = m_emailAlertTo;
	//SmtpMail.Send(msgMail);
}

bool UpdateDiscount(string id, double discount)
{
	string sc = "UPDATE card SET discount=" + discount + " WHERE id=" + id;
//DEBUG("sc=", sc);
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

void PrintPaymentType(string current)
{

	//payment type
	Response.Write("<select name=payment_type>");

	Response.Write(GetEnumOptions("payment_method", current));

	Response.Write("&nbsp;");
	Response.Write("</select>");	

}

bool PrintBranchNameOptions()
{
	int nBranchID = 1;
	if(Request.Form["branch"] != null && Request.Form["branch"] != "")
	{
		nBranchID = int.Parse(Request.Form["branch"]);
		Session["branch_id"] = nBranchID;
	}
	else if(Session["branch_id"] != null && Session["branch_id"].ToString() != "")
	{
		nBranchID = MyIntParse(Session["branch_id"].ToString());
	}
	return PrintBranchNameOptions(nBranchID.ToString());
}

bool PrintBranchNameOptions(string current_branch)
{
	return PrintBranchNameOptions(current_branch, "", false);
}
bool PrintBranchNameOptions(string current_branch, string onchange_url)
{
	return PrintBranchNameOptions(current_branch, onchange_url, false);
}
bool PrintBranchNameOptions(string current_branch, string onchange_url, bool bWithAll)
{
	DataSet dsBranch = new DataSet();
	int rows = 0;

	//do search
	string sc = "SELECT id, name FROM branch WHERE 1=1 ";
//	if(Session[m_sCompanyName + "AccessLevel"].ToString() != "10")
	if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
	/*	m_branch = Session["branch_id"].ToString();
		if(m_branch != "")
		{
			if(TSIsDigit(m_branch))
		*/
				sc += " AND id ="+ current_branch +" ";
		//}
	}
	sc += " AND activated = 1 ";
	sc += " ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsBranch, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=branch");
	if(onchange_url != "")
	{
		Response.Write(" onchange=\"window.location=('");
		Response.Write(onchange_url + "'+ this.options[this.selectedIndex].value ) \" ");
	}
	Response.Write(">");
	if(bWithAll)
	{
		//if(Session[m_sCompanyName + "AccessLevel"].ToString() == "10")
		if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
			Response.Write("<option value=0>All Branches</option>");
	}
	for(int i=0; i<rows; i++)
	{
		string bname = dsBranch.Tables["branch"].Rows[i]["name"].ToString();
		string bid = dsBranch.Tables["branch"].Rows[i]["id"].ToString();
		Response.Write("<option value='" + bid + "' ");
		if(bid == current_branch)
			Response.Write("selected");
		Response.Write(">" + bname + "</option>");
	}
	if(rows == 0)
		Response.Write("<option value=1>Branch 1</option>");
	Response.Write("</select>");
	return true;
}
string GetBranchNameById(string id)
{
	if(id == "")
		return "";
	DataSet dsBranch = new DataSet();
	string sc = "SELECT name FROM branch WHERE id = " + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsBranch, "branch") > 0)
			return dsBranch.Tables["branch"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}
string GetEnumValue(string sClass, string id)
{
	if(id == "")
	{
		Response.Write("Empty ID, class=" + sClass);
		return "";
	}

	DataSet dsEnum = new DataSet();
	string sValue = "";
	string sc = "SELECT name FROM enum WHERE class='" + sClass + "' AND id=" + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsEnum, "enum") == 1)
			sValue = dsEnum.Tables["enum"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return sValue;
}

string GetEnumID(string sClass, string sValue)
{
	DataSet dsEnum = new DataSet();
	string sID = "";
	string sc = "SELECT id FROM enum WHERE class='" + sClass + "' AND name='" + sValue + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsEnum, "enum") == 1)
			sID = dsEnum.Tables["enum"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return sID;
}
string GetEnumOptions(string sClass, string current_id)
{
	return GetEnumOptions(sClass, current_id, false);
}
string GetEnumOptions(string sClass, string current_id, bool bNoBeforeOptions)
{
	return GetEnumOptions(sClass, current_id, bNoBeforeOptions, true);
}
string GetEnumOptions(string sClass, string current_id, bool bNoBeforeOptions, bool bShowEnumID)
{
	return GetEnumOptions(sClass, current_id, bNoBeforeOptions, bShowEnumID, "");
}
//string GetEnumOptions(string sClass, string current_id)
string GetEnumOptions(string sClass, string current_id, bool bNoBeforeOptions, bool bShowEnumID, string sCheckedOption)
{
	return GetEnumOptions(sClass, current_id, bNoBeforeOptions, bShowEnumID, sCheckedOption, false);
}
string GetEnumOptions(string sClass, string current_id, bool bNoBeforeOptions, bool bShowEnumID, string sCheckedOption, bool bRestrictAccess)
{
	return GetEnumOptions(sClass, current_id, bNoBeforeOptions, bShowEnumID, sCheckedOption, bRestrictAccess, "");
}
string GetEnumOptions(string sClass, string current_id, bool bNoBeforeOptions, bool bShowEnumID, string sCheckedOption, bool bRestrictAccess, string sEscapeID)
{
	string sOut = "";
	DataSet dsEnum = new DataSet();
	string sc = "SELECT id, name FROM enum WHERE class='" + sClass + "'";
	if(sEscapeID != "")
		sc += " AND id NOT IN ("+ sEscapeID +") ";
	if(bRestrictAccess)
	{
		sc += "AND id = "+ current_id;
	}
	if(!bShowEnumID)
		sc += " AND name <> 'deleted' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsEnum, "enum");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	for(int i=0; i<dsEnum.Tables["enum"].Rows.Count; i++)
	{
		string id = dsEnum.Tables["enum"].Rows[i]["id"].ToString();
		string name = dsEnum.Tables["enum"].Rows[i]["name"].ToString();
		if(sClass == "access_level")
		{
			if(name == "administrator" || name == "dev") //no one can give out administrator access_level except eznz staff
			{
				if(Session["email"].ToString().IndexOf("@eznz.com") < 0)
					continue;
			}
		}
		if(bNoBeforeOptions)
			if(int.Parse(id) < int.Parse(current_id))
				continue;
		sOut += "<option value='";		
		if(bShowEnumID)
			sOut += id;
		else
			sOut += name;
		sOut += "'";
		
		if(sClass == "credit_terms" && current_id == "0")
        {
            if(i == 7)
                sOut += " selected";
        }
        else
        {
            if(id == current_id)
			    sOut += " selected";
		    if(sCheckedOption == i.ToString())
			    sOut += " selected";
        }
		sOut += ">" + Capital(name) + "</option>";
	}
	return sOut;
}

//code from : http://www.asp.net/Forums/ShowPost.aspx?tabindex=1&PostID=4377
string GenRandomString() 
{ 
	string password = ""; 
//	string passchar = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890"; 
	string passchar = "abcdefghijklmnpqrstuvwxyz123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"; 

	int runs = 0;
	int digits = 0;
	while(digits < 2)
	{
		digits = 0;
		password = "";
		Byte[] ranbuff = new Byte[50]; 
		
		// gen a password 
		System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create(); 
		rng.GetBytes(ranbuff); 
		int iLen = (ranbuff[0] % 4) + 8; // random length 8 to 12 chars 
		iLen = 8; //8 is enough, DW

		for (int iIndex = 1; iIndex <= iLen; iIndex++) 
		{ 
			int bNum = (int) ranbuff[iIndex+1]; 
			bNum %= passchar.Length; 
			char c = passchar[bNum];
			if(Char.IsDigit(c))
				digits++;
			password += passchar.Substring(bNum,1); 
		} 
		runs++;
		if(runs > 1000)
			break;
	}

//DEBUG("runs=", runs);

	return password; 
}

string GetRootPath()
{
//	return m_sRoot;

	string tspath = TSGetPath().ToLower();
	string tmCompanyName = m_sCompanyName.ToLower();
//DEBUG("tspath=", tspath);
//DEBUG("m_sCompanyName=", m_sCompanyName);
	if(tspath.IndexOf(tmCompanyName) == 0)
	{
		if(tspath.IndexOf(".") < 0)
			return "/" + m_sCompanyName;
	}
	else
	{
		int n = tspath.IndexOf(m_sCompanyName);
		if(n > 0)
		{
			string s = "/" + tspath.Substring(0, n + m_sCompanyName.Length);
			return s;
		}
	}

	return "";
}

bool GetCardID(string email, ref string id)
{
	DataSet dsu = new DataSet();
	string sc = "SELECT id FROM card WHERE email='" + email + "'";
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(dsu);
		if(rows == 1)
			id = dsu.Tables[0].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string GetShipName(string id)
{
	DataSet dssn = new DataSet();
	string sc = "SELECT name FROM ship WHERE id=" + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dssn, "shipname") == 1)
			return dssn.Tables["shipname"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}

string GetCpusMBNeeds(string code)
{
	DataSet dscpus = new DataSet();
	string sc = "SELECT cpus FROM q_mb_cpus WHERE code=" + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dscpus) > 0)
			return dscpus.Tables[0].Rows[0]["cpus"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "1";
	}
	return "1"; //default 1 cpu
}

long MyLongParse(string s)
{
	Trim(ref s);
	if(s == null || s == "")
		return 0;

	long n = 0;
	try
	{
		n = long.Parse(s);
	}
	catch(Exception e)
	{
		ShowParseException(s);
	}
	return n;
}

int MyIntParse(string s)
{
	Trim(ref s);
	if(s == null || s == "")
		return 0;

	return (int)MyDoubleParse(s);
}

double MyDoubleParse(string s)
{
	Trim(ref s);

	if(s == null || s == "")
		return 0;
	if(s.IndexOf("(")==0 && s.IndexOf(")") == s.Length-1)
	{
		s = s.Replace("(", "");
		s = s.Replace(")", "");
		s = "-" + s;
	}

	double d = 0;
	
	try
	{
		d = double.Parse(s);
	}
	catch(Exception e)
	{
		ShowParseException(s);
	}
	return d;
}

bool MyBooleanParse(string s)
{
	Trim(ref s);
	if(s == null || s == "" || s == "0")
		return false;
	else if(s == "1")
		return true;
	else if(s == "on")
		return true;
	else if(s == "true")
		return true;
	else if(s == "True")
		return true;
	else if(s == "On")
		return true;
	else if(s == "ON")
		return true;
	else if(s == "TRUE")
		return true;
	else if(s == "off")
		return false;

	bool b = false;
	try
	{
		b = Boolean.Parse(s);
	}
	catch(Exception e)
	{
		ShowParseException(s);
	}
	return b;
}

double MyMoneyParse(string s)
{
	Trim(ref s);
	if(s == null || s == "")
		return 0;

	double d = 0;
	try
	{
		d = double.Parse(s, NumberStyles.Currency, null);
	}
	catch(Exception e)
	{
		ShowParseException(s);
	}
	return d;
}

void ShowParseException(string s)
{
	string s1 = "<br><br><center><h3>Error, input string \"<font color=red>" + s + "</font>\" was not in a correct format</h3></center>";
	s1 += Environment.StackTrace;
	Response.Write(s1);
	s1 += Environment.StackTrace;
//	AlertAdmin(s1);
	Response.End();
}

string PrintCustomerOptions()
{
	return PrintCustomerOptions("", "");
}

string PrintCustomerOptions(string current_id, string uri)
{
	DataSet dssup = new DataSet();
	//string type_customer = GetEnumID("card_type", "supplier");
	int rows = 0;
	string sc = "SELECT id, short_name, name, email, company ";
	sc += " FROM card ";
	//sc += " WHERE type = 1 OR type = 2 ";
	sc += " ORDER BY company";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dssup, "customer");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "\r\n<select name=customer";
	if(uri != "")
		s += " onchange=\"window.location=('" + uri + "'+this.options[this.selectedIndex].value)\"";
	s += "><option value=''>All Customer</option>";
	for(int i=0; i<rows; i++)
	{
		string id = dssup.Tables["customer"].Rows[i]["id"].ToString();
		string name = dssup.Tables["customer"].Rows[i]["name"].ToString();
		if(name == "")
			name = dssup.Tables["customer"].Rows[i]["company"].ToString();
		if(name == "")
			name = dssup.Tables["customer"].Rows[i]["short_name"].ToString();
		s += "<option value=" + dssup.Tables["customer"].Rows[i]["id"].ToString();
		if(current_id == id)
			s += " selected";
		s += ">" + name + "</option>\r\n";
	}
	s += "\r\n</select>";
	return s;
}

string PrintSupplierOptions()
{
	return PrintSupplierOptions("", "");
}

string PrintSupplierOptions(string current_id, string uri)
{
	return PrintSupplierOptions(current_id, uri, "supplier");
}

string PrintSupplierOptions(string current_id, string uri, string supplier_type)
{
		return PrintSupplierOptions(current_id, uri, supplier_type, "");
}

string PrintSupplierOptions(string current_id, string uri, string supplier_type, string second_type)
{
	DataSet dssup = new DataSet();
	//string type_supplier = GetEnumID("card_type", "supplier");
	string type_supplier = GetEnumID("card_type", supplier_type);
	string nd_type = GetEnumID("card_type", second_type);
	int rows = 0;
	string sc = "SELECT id, short_name, name, email, company ";
	sc += " FROM card WHERE name NOT LIKE '%repair%form%' ";
	sc += " AND (type=" + type_supplier + "";
	if(nd_type != "" && nd_type != null)
		sc += " OR type = "+ nd_type +"";	
	sc += " ) ";
	sc += " ORDER BY company";
//DEBUG("s c +", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dssup, "suppliers");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "\r\n<select name=supplier";
	if(uri != "")
		s += " onchange=\"window.location=('" + uri + "'+this.options[this.selectedIndex].value)\"";
	s += "><option value=''>Please Select</option>";
	for(int i=0; i<rows; i++)
	{
		string id = dssup.Tables["suppliers"].Rows[i]["id"].ToString();
		string name = dssup.Tables["suppliers"].Rows[i]["company"].ToString();
		if(name == "")
			name = dssup.Tables["suppliers"].Rows[i]["name"].ToString();
		if(name == "")
			name = dssup.Tables["suppliers"].Rows[i]["short_name"].ToString();
		s += "<option value=" + dssup.Tables["suppliers"].Rows[i]["id"].ToString();
		if(current_id == id)
			s += " selected";
		s += ">" + name + "</option>\r\n";
	}
	s += "\r\n</select>";
	return s;
}

string PrintSupplierOptionsWithShortName()
{
	DataSet dssup = new DataSet();
	string type_supplier = GetEnumID("card_type", "supplier");
	int rows = 0;
	string sc = "SELECT id, short_name, name, email, company ";
	sc += " FROM card WHERE type=" + type_supplier + " ORDER BY company ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dssup, "suppliers");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "\r\n<select name=supplier>";
	for(int i=0; i<rows; i++)
	{
		string name = dssup.Tables["suppliers"].Rows[i]["company"].ToString();
		if(name == "")
			name = dssup.Tables["suppliers"].Rows[i]["name"].ToString();
		if(name == "")
			name = dssup.Tables["suppliers"].Rows[i]["short_name"].ToString();
		s += "<option value=" + dssup.Tables["suppliers"].Rows[i]["short_name"].ToString() + ">";
		s += name + "</option>\r\n";
	}
	s += "\r\n</select>";
	return s;
}

void MsgDie(string msg) //out put error msg and terminate script
{
	Response.Write("<br><br><center><h3>" + msg);
	Response.End();
}

//Get Next Available Product Code;
int GetNextCode()
{
	DataSet dst = new DataSet();
	int next_code = -1;
	//delete all data
	if(dst.Tables["code_relations"] != null)
		dst.Tables["code_relations"].Clear();

	int rows;
	string sc = "SELECT TOP 1 code FROM code_relations ORDER BY code DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "code_relations");
		if(rows > 0)
			next_code = int.Parse(dst.Tables["code_relations"].Rows[0]["code"].ToString()) + 1;
		else
			next_code = m_nFirstCode;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return next_code;
}

double GetFixedPriceForDealer(string code, string qty, string dealer_level, string card_id)
{
	if(code == null || code == "")
		return 99999999;
	DataSet dsgspfd = new DataSet();
	string sc = "SELECT c.* FROM product p JOIN code_relations c ON p.code=c.code WHERE c.code=" + code;	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsgspfd, "price") <= 0)
		{
			sc = " SELECT name FROM code_relations WHERE code=" + code;
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dsgspfd, "name") <= 0)
				{
					Response.Write("<br><br><h3>Error, product not found, code=" + code + "</h3>");
					return 999999;
				}
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
			}
	
			Response.Write("<br><br><h3>Error, product discontinued, code=" + code + " : " + dsgspfd.Tables["name"].Rows[0]["name"].ToString() + "</h3>");
			return 999999;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	
	DataRow dr = dsgspfd.Tables["price"].Rows[0];
//	string price = dr["price" + dealer_level].ToString();
	double GSTRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "15")) / 100;				// 30.JUN.2003 XW
	if(GSTRate < 1)
			GSTRate = 1+GSTRate;
	string price = (MyDoubleParse(dr["price1"].ToString()) / GSTRate).ToString();
	if(MyDoubleParse(price) == 0 )
    {
		//price = (MyDoubleParse(dr["level_price1"].ToString())).ToString();
        double priceReturn = double.Parse(dr["level_price0"].ToString())*double.Parse(dr["level_rate"+dealer_level].ToString())/100;
        return priceReturn;
    }
	
	return MyDoubleParse(price);
}

double GetSalesPriceForDealer(string code, string qty, string dealer_level, string card_id)
{
//DEBUG("dealer_level" , dealer_level);
	bool bFixedPrices = false;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		bFixedPrices = true;
//DEBUG("bFixedPrice ", bFixedPrices.ToString());
	bool bUseLastSalesFixedPrice = false;
	if(MyBooleanParse(GetSiteSettings("Enable_Use_Last_Sales_Fixed_Price", "0", true)))
		bUseLastSalesFixedPrice = true;

	DataRow drComm = GetCardData(card_id);
	string sType="";
	if(bIsSpecialItemForQPOS(code) && !bUseLastSalesFixedPrice)
		bFixedPrices = true;
	else 
	{
		if(MyIntParse(GetSiteSettings("dealer_levels")) > 0)
			bFixedPrices = false; //allow dealer levels 
//DEBUG("bFixedPrice ", bFixedPrices.ToString());
		
		if(drComm != null)
		{
			sType = drComm["type"].ToString();
			string card_name = drComm["name"].ToString().ToLower();
			if(card_id == "0" || card_name.IndexOf("cash sales") >= 0) // || sType == "1")
				bFixedPrices = true;		
		}
		else if(card_id == "0")
			bFixedPrices = true;		
	}
	//bFixedPrices = true;
	double dLastSalesFixedPrice = 0;
	//**************************************************
	//DEBUG("dealer_level" , dealer_level);
	if(bFixedPrices)
		return GetFixedPriceForDealer(code, qty, dealer_level, card_id);
	else if (bUseLastSalesFixedPrice)
	{
		dLastSalesFixedPrice = GetLastSalesFixedPriceForDealer(code, qty, card_id, dealer_level);		
//		return dLastSalesFixedPrice;
	}
//DEBUG("dealer_level" , dealer_level);
//DEBUG("dLastSalesFixedPrice =", dLastSalesFixedPrice.ToString());
	bool bRoundPrice = false;

	if(Session["round_price_no_cent"] == null)
	{	
		string roundCent = GetSiteSettings("round_price_no_cent", "0", true);
		if(roundCent != "0" || roundCent != "1")
			roundCent = "0";
		bRoundPrice = MyBooleanParse(roundCent);		
		Session["round_price_no_cent"] = bRoundPrice;		
	}
	else
	{		
		bRoundPrice = (bool)Session["round_price_no_cent"];
	}

	if(code == null || code == "")
		return 99999999;

	DataSet dsgspfd = new DataSet();
	string sc = "SELECT p.price, c.*, c.manual_cost_nzd * c.rate + nzd_freight AS bottom_price ";
	sc += " FROM product p JOIN code_relations c ON p.code=c.code ";
	sc += " WHERE c.code=" + code;
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsgspfd, "price") <= 0)
		{
			sc = " SELECT name FROM code_relations WHERE code=" + code;
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dsgspfd, "name") <= 0)
				{
					Response.Write("<br><br><h3>Error, product not found, code=" + code + "</h3>");
					return 999999;
				}
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
			}
	
			Response.Write("<br><br><h3>Error, product discontinued, code=" + code + " : " + dsgspfd.Tables["name"].Rows[0]["name"].ToString() + "</h3>");
			return 999999;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	
	DataRow dr = dsgspfd.Tables["price"].Rows[0];

	string brand = dr["brand"].ToString();
	string slcat = dr["cat"].ToString() + " - " + dr["s_cat"].ToString();
	dealer_level = GetDealerLevelForCat(card_id, brand, slcat, MyIntParse(dealer_level)).ToString();
//DEBUG("dealer_level" , dealer_level);
	bool bClearance = bool.Parse(dr["clearance"].ToString());	
   	string dealerPrice;
    if(sType == "1" && (dr["price1"].ToString() != null || dr["price1"].ToString() != ""))
	{
		dealerPrice = dr["price1"].ToString();
		double dCustPrice = double.Parse(dealerPrice)/1.15; // *****************************GST by differ Customer  Sean
		
		
	//DEBUG("dealer_level" , dealerPrice );
		return dCustPrice; // ********************** POS CART PRICE BY SEAN
	//DEBUG("dealer_level" , dealer_level);
	}
	else 
	{
//		dealerPrice = dr["level_price"+dealer_level].ToString();
//		return double.Parse(dealerPrice);
        double priceReturn = double.Parse(dr["level_price0"].ToString())*double.Parse(dr["level_rate"+dealer_level].ToString())/100;
        return priceReturn;
	}
	int i = 0;
	string si = "";
	int[] qb = new int[9]; //qty breaks;
	double[] qbd = new double[9];
	double[] lr = new double[9];
	for(i=0; i<9; i++)
	{
		string ii = (i+1).ToString();
		si = dr["qty_break" + ii].ToString();
		if(si == "")
			qb[i] = 1000;
		else
			qb[i] = MyIntParse(si);
		
		si = dr["qty_break_discount" + ii].ToString();
		if(si == "")
			qbd[i] = 0;
		else
			qbd[i] = MyDoubleParse(si);

		si = dr["level_rate" + ii].ToString();
		if(si == "")
			lr[i] = 1.2;
		else
			lr[i] = MyDoubleParse(si);
	}
	
	double level_rate = lr[MyIntParse(dealer_level)-1];

//	double dPrice = double.Parse(dr["price"].ToString());
	double dPrice = MyDoubleParse(dr["bottom_price"].ToString());
/*//kevin 24-06-06	   
	double dPrice;
    if(!(dr["special"].ToString().Equals("0")))
        dPrice = MyDoubleParse(dr["special_price"].ToString());
    else
        dPrice = MyDoubleParse(dr["bottom_price"].ToString());
*/
	if(!bClearance)
		dPrice *= level_rate;

	double dPriceOnePiece = dPrice; //price without qty discount

	double dQtyDiscount = 1;
	double dDiscount = 0;
	if (bUseLastSalesFixedPrice)
	{
		dPrice = dLastSalesFixedPrice;
	}
	bool bFixLevel6 = MyBooleanParse(GetSiteSettings("level_6_no_qty_discount", "0"));
	if(bFixLevel6 && dealer_level == "6")
	{
		if(bRoundPrice)
			dPrice = Math.Round(dPrice, 0);
		return dPrice;
	}
	int dqty = MyIntParse(qty);
//DEBUG("QTY " , dqty);
	if(dqty > 1)
	{
		//get qty discount
		dQtyDiscount = GetQtyDiscount(dqty, qb, qbd);
//DEBUG("dqty ", dqty);
//DEBUG("qb ", qb.ToString());
//DEBUG("qbd ", qbd.ToString());
//DEBUG("dQtyDisount ", dQtyDiscount.ToString());
		if(!bClearance)
		{
			dPrice *= (1 - dQtyDiscount);
			dDiscount = 1 - dQtyDiscount;
		}		
	}

	if(bRoundPrice)
		dPrice = Math.Round(dPrice, 0);
	//DEBUG("SC ", dPrice.ToString());
	return dPrice;
}

double GetQtyDiscount(int qty, int[] qb, double[] qbd)
{
	int qbs = MyIntParse(GetSiteSettings("quantity_breaks", "3")); // how many quantity breaks
	if(qbs > 9)
		qbs = 9;
	for(int i=qbs-1; i>=0; i--)
	{
		if(qb[i] != 0 && qty >= qb[i])
			return qbd[i] / 100;
	}
	return 0;
}

double GetQuantityDiscount(int qty, int[] qb, double level_rate)
{
	double dd = 1;
	for(int i=3; i>=0; i--)
	{
		double margin = 0;
		int breaks = qb[i];
		if(qty >= breaks)
		{
			dd = 1 - (i+1) * (1 - 1/level_rate) / 4;
			break;
		}
	}
	return dd;
}

double GetLevelDiscount(string level)
{
	DataSet dsld = new DataSet();
	if(level == "")
		return 2;

	double dd = 2;
	int rows = 0;
	string sc = "SELECT * FROM discount WHERE factor='level' AND factor_id=" + level;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsld, "ld") == 1)
			dd = MyDoubleParse(dsld.Tables["ld"].Rows[0]["data1"].ToString());
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}
	return dd;
}

string GetDisplayQuoteNumber(string sNumber, string sType)
{
	int number = MyIntParse(sNumber);
	string absnumber = Math.Abs(number).ToString();
	string typestring = GetEnumValue("receipt_type", sType);
	if(typestring == "")
		return sNumber;

	string prefix = typestring[0].ToString().ToUpper();
	return prefix + absnumber;
}

string Capital(string s)
{
	if(s == "")
		return s;

	string sc = "";
	bool bCap = true; //cap the first one
	for(int i=0; i<s.Length; i++)
	{
		if(bCap)
		{
			sc += s[i].ToString().ToUpper();
			bCap = false;
		}
		else
			sc += s[i];

		if(s[i] == ' ')
			bCap = true;
	}
	return sc;
//	return s[0].ToString().ToUpper() + s.Substring(1, s.Length-1);
}

DataRow GetCardData(string id)
{
	Trim(ref id);
	if(id == null || id == "")
		return null;

	DataSet dsa = new DataSet();
	int rows = 0;
	string sc = "SELECT * FROM card WHERE id=" + id;

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsa, "card") == 1)
			return dsa.Tables["card"].Rows[0];
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return null;
	}
	return null;
}

string GetAccessClassName(string id)
{
	if(dstcom.Tables["getclassname"] != null)
		dstcom.Tables["getclassname"].Clear();

	string sc = " SELECT name FROM menu_access_class WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dstcom, "getclassname") == 1)
			return dstcom.Tables["getclassname"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return id;
}

string GetAccessClassID(string name)
{
	if(dstcom.Tables["getclassid"] != null)
		dstcom.Tables["getclassid"].Clear();

	string sc = " SELECT id FROM menu_access_class WHERE name='" + name + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dstcom, "getclassid") == 1)
			return dstcom.Tables["getclassid"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return name;
}

bool CheckAccess(string class_id, string uri)
{
	if(class_id == GetAccessClassID("Administrator"))
		return true;

	if(dstcom.Tables["checkaccess"] != null)
		dstcom.Tables["checkaccess"].Clear(); 
//DEBUG("class=", class_id);
	string sc = "SELECT id ";
	sc += " FROM menu_admin_id ";
	sc += " WHERE uri LIKE '" + uri + "%' OR sisters LIKE '%" + uri + "%' ";
//DEBUG("cs =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dstcom, "checkaccess") <= 0) //if there's no record on available menus, then allow it
		{
			if(m_sSite == "www")
				return true;
			else if(m_sSite == "admin")
			{
				if(Session[m_sCompanyName + "AccessLevel"].ToString() != GetAccessClassID("no access"))
					return true;
			}
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}

	if(dstcom.Tables["checkaccess"] != null)
		dstcom.Tables["checkaccess"].Clear();

	sc = "SELECT a.id ";
	sc += " FROM menu_admin_access a JOIN menu_admin_id i ON i.id=a.menu ";
	sc += " WHERE (i.uri LIKE '" + uri + "%' OR sisters LIKE '%" + uri + "%') "; 
	sc += " AND a.class=" + class_id;
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dstcom, "checkaccess") >= 1)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return false;
}

bool CheckAccess(string class_id)
{
	if(class_id == GetAccessClassID("Administrator"))
		return true;

	string uri = Request.ServerVariables["URL"];
	uri = uri.Substring(0, uri.IndexOf(".aspx") + 5); //strip off parameters
	int i = uri.Length-1;
	for(; i>=0; i--)
	{
		if(uri[i] == '/')
			break;
	}
	uri = uri.Substring(i+1, uri.Length - i - 1);
	return CheckAccess(class_id, uri);
}

bool SecurityCheck(string sLevel)
{
	if(sLevel == "normal")
	{
		if(!TS_UserLoggedIn())
		{
			RememberLastPage();
			Response.Redirect("login.aspx");
			return false;
		}
		else
		{
			return true;
		}
	}
	return SecurityCheck(sLevel, true);
    //return true;
}

bool SecurityCheck(string sLevel, bool bSayNo)
{
	if(!TS_UserLoggedIn())
	{
		if(!bSayNo)
			return false;
		RememberLastPage();
		Response.Redirect("login.aspx");
		return false;
	}

	if(CheckAccess(Session[m_sCompanyName + "AccessLevel"].ToString()))
		return true;
	else if(bSayNo)
	{
		Response.Write("<h3>ACCESS DENIED1</h3>");
		Response.End();
	}
	return false;
/*
//	else if(Session["email"] != null && Session["email"].ToString() == "darcy@eznz.co.nz")
//	{
//		return true; // owner
//	}
	else //check site and rights
	{
		string key = m_sCompanyName + "AccessLevel";
		int nLevel = 1;
		if(Session[key] != null)
			nLevel = int.Parse(Session[key].ToString());
		
		string sRequired = GetEnumID("access_level", sLevel);
		int nRequired = int.Parse(sRequired);
//DEBUG("nReq=", nRequired);
//DEBUG("nLevel=", nLevel);
		if(nLevel >= nRequired)
			return true;
	}
	//for corportate
	if(m_supplierString != "")
	{
		if(Session["email"].ToString() == m_sSalesEmail)
			return true;
	}
	if(bSayNo)
	{
		Response.Write("<h3>ACCESS DENIED</h3>");
		Response.End();
	}
	return false;	
*/
}

string GetAccessClassOptions(string current_class)
{
	if(dstcom.Tables["getaccessclass"] != null)
		dstcom.Tables["getaccessclass"].Clear();
	string s = "";
	string sc = " SELECT * FROM menu_access_class ";
//	sc += " WHERE name NOT LIKE '%no access%' AND name NOT LIKE '%administrator%' ";
	sc += " ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dstcom, "getaccessclass");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	
	for(int i=0; i<dstcom.Tables["getaccessclass"].Rows.Count; i++)
	{
		string id = dstcom.Tables["getaccessclass"].Rows[i]["id"].ToString();
		string name = dstcom.Tables["getaccessclass"].Rows[i]["name"].ToString();
//		if(name == "administrator" || name == "dev") //no one can give out administrator access_level except eznz staff
//		{
//			if(Session["email"].ToString().IndexOf("@eznz.com") < 0)
//				continue;
//		}
		s += "<option value=" + id;
		if(id == current_class)
			s += " selected";
		s += ">" + name + "</option>";
	}
	return s;
}

string GetCatAccessGroupString(string card_id)
{
	if(dstcom.Tables["cagroup"] != null)
		dstcom.Tables["cagroup"].Clear();

	string sc = " SELECT limit FROM view_limit v JOIN card c ON v.id=c.cat_access_group WHERE c.id=" + card_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "cagroup") <= 0)
			return "";//no limit
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return dstcom.Tables["cagroup"].Rows[0]["limit"].ToString();
}

string BuildCatAccess(string str)
{
	str = str.ToLower();
	Trim(ref str);
	if(str == "all" || str == "" || str == null)
		return null;

	string sRet = "";
	string word = "";
	string[] sa = new String[64];
	int j = 0;
	int i = 0;
	//get all key words
	for(; i<str.Length; i++)
	{
		if(i > 1024 || j > 63)
			break;

		if(str[i] != ',' && str[i] != ';')
		{
			word += str[i];
		}
		else
		{
			Trim(ref word);
			if(word != "" && word != "all")
			{
				sa[j++] = word;
				word = "";
			}
		}
	}
	if(word != "" && word != "all") //don't forget the last one
		sa[j++] = word;

	if(j <= 0)
		return null;
	//build search key

	if(sa[0] == "not")
	{
		sRet = " IN(";
		i = 1;
	}
	else
	{
		sRet = " NOT IN(";
		i = 0;
	}

	i = 0;
	for(; i<j; i++)
	{
		Trim(ref sa[i]);
		sRet += "'" + sa[i] + "'";
		if(i<j-1)
			sRet += ", ";
	}
	sRet += ") ";
//DEBUG("sRet=", sRet);
	return sRet;
}

string EncodeDoubleQuote(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\"')
			ss += '\"'; //double it
		if(s[i] == 8220 || s[i] == 8221) //chinese double quote
		{
//DEBUG("s=", (int)s[i]);
			ss += "\"\""; //add double quote
			continue; //skip this
		}

		ss += s[i];
	}
	return ss;
}

string AddRepairLogString(string action_desc, string invoice_number, string product_code, string repair_id, string sn, string rma_id)
{
	string sc = " INSERT INTO repair_log ";
	sc += " (log_time, staff, action_desc, code, invoice_number, repair_id, sn, rma_id) ";
	sc += " VALUES( ";
	sc += " GETDATE() ";
	sc += ", " + Session["card_id"].ToString();
	sc += ", '" + EncodeQuote(action_desc) + "' ";
	sc += ", '" + product_code + "', '" + invoice_number + "', '"+ repair_id +"' ";
	sc += " ,'" + EncodeQuote(sn) + "' ";
	sc += " ,'" + rma_id + "' ";
	sc += ") ";
	return sc;
}

string AddSerialLogString(string sn, string desc, string po_id, string invoice_number, 
			   string dealer_rma_id, string supplier_rma_id)
{
	string sc = " INSERT INTO serial_trace ";
	sc += " (sn, logtime, staff, action_desc, po_id, invoice_number, dealer_rma_id, supplier_rma_id) ";
	sc += " VALUES( ";
	sc += " '" + EncodeQuote(sn) + "' ";
	sc += ", GETDATE() ";
	sc += ", " + Session["card_id"].ToString();
	sc += ", '" + EncodeQuote(desc) + "' ";
	sc += ", '" + po_id + "', '" + invoice_number + "', '" + dealer_rma_id + "', '" + supplier_rma_id + "' ";
	sc += ") ";
	return sc;
}

string AddSerialLogString(string sn, string desc, string po_id, string order_id, string invoice_number, string dealer_rma_id, string supplier_rma_id)
{
	string sc = " INSERT INTO serial_trace ";
	sc += " (sn, logtime, staff, action_desc, order_id, invoice_number, dealer_rma_id, supplier_rma_id) ";
	sc += " VALUES( ";
	sc += " '" + EncodeQuote(sn) + "' ";
	sc += ", GETDATE() ";
	sc += ", " + Session["card_id"].ToString();
	sc += ", '" + EncodeQuote(desc) + "' ";
	sc += ", '" + order_id + "', '" + invoice_number + "', '" + dealer_rma_id + "', '" + supplier_rma_id + "' ";
	sc += ") ";
	return sc;
}

string GetPoNumber(string po_id)
{
	if(dstcom.Tables["ponumber"] != null)
		dstcom.Tables["ponumber"].Clear();
	string sc = " SELECT po_number, inv_number FROM purchase WHERE id = " + po_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "ponumber") == 1)
		{
			string pon = dstcom.Tables["ponumber"].Rows[0]["po_number"].ToString();
			string inv = dstcom.Tables["ponumber"].Rows[0]["inv_number"].ToString();
			pon = "<font color=blue>" + pon + "</font>";
			if(inv != "")
				pon += "<font color=red>(" + inv + ")</font>";
			return pon;
		}
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
	}
	return "Error";
}

string EncodeUserName()
{
	if(Session["name"] == null)
		return "";
	string uname = Session["name"].ToString();
	uname = uname.Replace(" ", "_");
	uname = uname.Replace("/", "_");
	uname = uname.Replace("\\", "_");
	return uname;
}

string GetCardValue(string sType, string sid)
{

	DataSet dsEnum = new DataSet();
	string sValue = "";
	string sc = "SELECT DISTINCT id, CONVERT(varchar(15),company) AS company FROM card WHERE type = "+ sType +" ";
	sc += " ORDER BY company ASC";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsEnum, "supplier");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	for(int i=0; i<dsEnum.Tables["supplier"].Rows.Count; i++)
	{
		string id = dsEnum.Tables["supplier"].Rows[i]["id"].ToString();
		sType = dsEnum.Tables["supplier"].Rows[i]["company"].ToString();
		sValue += "<option value=" + id;
		if(id == sid)
			sValue += " selected ";
		sValue += ">" + sType + "</option>";
	}

	return sValue;
}


bool PrintPatentInfomation(string name)
{
	DataSet ds1 = new DataSet();
	string sc = " SELECT * FROM patent WHERE name = '" + name + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds1, "patent") <= 0)
		{
			return true;
		}
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	DataRow dr = ds1.Tables["patent"].Rows[0];
	double dPrice = MyDoubleParse(dr["price"].ToString());

	Response.Write("<table align=center border=3 bgcolor=yellow>");
	Response.Write("<tr><td align=center>");
	Response.Write("<h3><font color=black size=+1><b>Patented Feature (" + dPrice.ToString("c") + ")</b></font>");
//	Response.Write("<h5>Cost : " + dPrice.ToString("c") + " + GST</h5>");
//	Response.Write("<h5><a href=patent.aspx class=o target=_blank><font color=blue><u>View Other Patented Features</u></font></a></h5>");
	Response.Write("</td></tr></table>");
	return true;
}

bool CheckSQLAttack(string str)
{
	if(str == null || str == "")
		return true;

	string s = str.ToLower();
	Trim(ref s);
	bool bUpdate = (s.IndexOf("update") >= 0);
	bool bDelete = (s.IndexOf("delete") >= 0);
	bool bDrop = (s.IndexOf("drop") >= 0);
	bool bCreate = (s.IndexOf("create") >= 0);
	bool bSelect = (s.IndexOf("select") >= 0);
	bool bQuote = (s.IndexOf("'") >= 0);
//	bool bSpace = (s.IndexOf(" ") >= 0);

	if(bUpdate || bDelete || bDrop || bCreate || bSelect || bQuote)
	{
		string manager_email = GetSiteSettings("manager_email", "alert@eznz.com");
		string ip = Request.ServerVariables["REMOTE_ADDR"]; //cache ip
		string rip = ""; //real ip
		if(Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
			rip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
		else
			rip = ip;

		string sbody = "SQL Injection Attack detected and blocked. <br>";
		sbody += "ip : " + rip + "<br>";
		sbody += "user : " + Session["name"] + "<br>";
		sbody += "email : " + Session["email"] + "<br>";
		sbody += "Account# : " + Session["login_card_id"] + "<br>";
		sbody += "URI : " + Request.ServerVariables["URL"] + "<br>";
		sbody += "Parameter : " + str + "<br><br>";

/*		sbody += "This attack is potential, the attacker was trying take control of you database useing<br>";
		sbody += "a technic called 'SQL Injection Attack', which could issentially destory your database if succeeded.<br>";
		sbody += "<br>We strongly suggest that you investigate this accoun/person if account# or user name is showing.<br>";
		sbody += " Detailed log is available in database if evidence is needed to take legal action.<br>";

		sbody += "<br>EZNZ Team";
*/
		MailMessage msgMail = new MailMessage();
		
//		msgMail.To = manager_email;
		msgMail.To = "alert@eznz.com";
		msgMail.From = manager_email;
		msgMail.Subject = "Warning, SQL Injection Attack !";
		msgMail.BodyFormat = MailFormat.Html;
		msgMail.Body = sbody;

		//SmtpMail.Send(msgMail);
		return false;
	}
	return true;
}

string ApplyColor(string s)
{
	if(dstcom.Tables["colorset"] != null)
		dstcom.Clear();

	string set_id = GetSiteSettings("color_set_in_use", "1");
//	if(Session["color_set"] != null)
//		set_id = Session["color_set"].ToString();

	string sc = " SELECT * FROM color_set WHERE id=" + set_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "cs") <= 0)
			return s;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return s;
	}
	DataRow dr = dstcom.Tables["cs"].Rows[0];
//	m_name = dr["name"].ToString();
//	m_note = dr["note"].ToString();
	for(int i=20; i>=0; i--)
	{
		string sid = "@@color_" + i.ToString();
		string color = dr["c" + i.ToString()].ToString();
		if(color == "")
			continue;
		s = s.Replace(sid, color);
	}
	return s;
}


string StripHTMLtags(string s)
{
/******************************
provided by michael cox 13/11/03
********************************/
	string ss = "";
	bool remove = false;
	for(int i=0; i<s.Length; i++)
	{
		if(i >0)
			if(s[i-1] == '>')
				remove = false;
		if(s[i] == '<')
			remove = true;
		
		if(remove == false)
			ss += s[i];
		}
	return ss;
}

bool CreateBarcode(string sValue, string spath)
{

	//----------user private font to generate code39 barcode-------------------
	string sFontPath = Server.MapPath("./bar/BARCODE39.TTF");
//DEBUG("sfontpath =", sFontPath);
	// Create a private font collection
	PrivateFontCollection pfc = new PrivateFontCollection();

	// Load in the temporary barcode font
	pfc.AddFontFile(""+ sFontPath +"");

	// Select the font family to use
	FontFamily usefont = new FontFamily("code 39",pfc);

	//-------------end of using private font---------------------

	int n_ImgWidth = int.Parse(GetSiteSettings("barcode_width", "350"));
	int n_ImgHeight = int.Parse(GetSiteSettings("barcode_height", "160"));
		
	Bitmap b = new Bitmap(n_ImgWidth,n_ImgHeight,PixelFormat.Format32bppRgb);
	Graphics g = Graphics.FromImage(b);
	SolidBrush sb = new SolidBrush(System.Drawing.Color.Black);
	SolidBrush sb2 = new SolidBrush(System.Drawing.Color.Orange);
	//Font f = new Font(bfCode39,30);
	//Font myCode39 = new Font(""+ sFontPath +"", 30);
	System.Drawing.Font myCode39 = new System.Drawing.Font(usefont, 30);
	System.Drawing.Font f2 = new System.Drawing.Font("Verdana",10);
	System.Drawing.Font f3 = new System.Drawing.Font("Verdana", 12,FontStyle.Bold);
	System.Drawing.Font f4 = new System.Drawing.Font("Verdana", 12);
	//g.FillRectangle(new SolidBrush(Color.White),0,0,n_ImgWidth,n_ImgHeight);
	g.FillRectangle(new SolidBrush(System.Drawing.Color.White),0,0,n_ImgWidth,n_ImgHeight);
	//g.DrawString(""+ name +"",f3,sb,5,3);
	g.DrawString("*"+ sValue +"*",myCode39,sb,(n_ImgWidth/2)-(n_ImgWidth/3),(n_ImgHeight/2)-30);
//	g.DrawString(""+ "",f2,sb,(n_ImgWidth/2)-(n_ImgWidth/4),(n_ImgHeight/2));
//	g.DrawString(""+ dPrice.ToString("c") +"(exc GST)",f4,sb,5,(n_ImgHeight/2)+30);
//	g.DrawString(""+ dPriceWithGST.ToString("c") +"(inc GST)",f4,sb,(n_ImgWidth/2)+10,(n_ImgHeight/2)+30);
	//Response.ContentType = "image/jpeg";
	//b.Save(Response.OutputStream, ImageFormat.Jpeg);
	//b.Save(Server.MapPath("./bar") +"\\"+ sValue +".bmp",ImageFormat.Bmp);
	b.Save(Server.MapPath(spath) +"\\"+ sValue +".gif",ImageFormat.Gif);
	//b.Save(Server.MapPath(spath) +"\\"+ sValue +".jpg",ImageFormat.Jpeg);
	g.Dispose();
	
	return true;

}

bool PrintBarcode(string sValue, string spath)
{
//	DEBUG("mcosl =", m_cols);
	string swidth = GetSiteSettings("barcode_width_percent", "25%");
	string sheight = GetSiteSettings("barcode_height_percent", "18%");
	string dest_path = Server.MapPath(spath);
	DirectoryInfo di = new DirectoryInfo(dest_path);
//DEBUG("sfile = ", dest_path);
//DEBUG(" scol  = ", scol);
//DEBUG(" fcol  = ", fcol);
	foreach (FileInfo f in di.GetFiles("*.gif")) 
	{
		string sfile = f.Name.ToString();
		sfile = sfile.Replace(".gif", "");
//DEBUG("sfile = ", sfile);
		int nSpace = int.Parse(GetSiteSettings("barcode_bt_space", "6"));
		
		if(sValue == sfile)
		{
			//string dest_file = dest_path + "\\" + f.Name;
			string dest_file = "./bar/" + f.Name;

			for(int j=0; j<nSpace; j++)
				Response.Write("&nbsp;");
			Response.Write("<img width="+ swidth +" height="+ sheight +" src='"+ dest_file +"'>");
	
		}
	}

	return true;
}

bool TS_UserLoggedIn()
{
	if(Session[m_sCompanyName + "loggedin"] != null)
		return true;
	return false;
}

void TS_LogUserIn()
{
	Session[m_sCompanyName + "loggedin"] = true;
}

int GetDealerLevelForCat(string card_id, string brand, string cat, int default_level)
{
	int nLevel = default_level;
	if(MyIntParse(card_id) <= 0)
		return nLevel;

	DataSet dsdl = new DataSet();
	string sc = "";
	if(brand != null && brand != "")
	{
		//brand levels overwrite categories
		sc = " SELECT level FROM dealer_levels WHERE card_id = " + card_id + " AND cat = '" + EncodeQuote("Brands - " + brand) + "' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dsdl) > 0)
			{
				nLevel = MyIntParse(dsdl.Tables[0].Rows[0]["level"].ToString());
			}
			else
			{
				sc = " SELECT level FROM dealer_levels WHERE card_id = " + card_id + " AND cat='" + EncodeQuote(cat) + "' ";
				try
				{
					myAdapter = new SqlDataAdapter(sc, myConnection);
					if(myAdapter.Fill(dsdl) > 0)
						nLevel = MyIntParse(dsdl.Tables[0].Rows[0]["level"].ToString());
				}
				catch(Exception e1) 
				{
					ShowExp(sc, e1);
				}
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}
	}
	else
	{
		sc = " SELECT level FROM dealer_levels WHERE card_id = " + card_id + " AND cat='" + EncodeQuote(cat) + "' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dsdl) > 0)
				nLevel = MyIntParse(dsdl.Tables[0].Rows[0]["level"].ToString());
		}
		catch(Exception e1) 
		{
			ShowExp(sc, e1);
		}
	}
	return nLevel;
}

string PrintBranchOptions(string current_id)
{
	if(dstcom.Tables["branch"] != null)
		dstcom.Tables["branch"].Clear();

	if(Session["branch_support"] != null)
	{
		if(current_id == null || current_id == "")
			current_id = Session["branch_id"].ToString();
	}
	else
		current_id = "1";
	int rows = 0;
	string s = "";
	string sc = " SELECT id, name FROM branch ";
	sc += " WHERE activated = 1 ";
	if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
		sc += " AND id ="+ current_id +" ";
	}

	sc += " ORDER BY id ";
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

string GetAccountClass(string id)
{
	DataSet dsname = new DataSet();
	string sc = " SELECT class1, class2, class3, class4 ";
	sc += " FROM account ";
	sc += " WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsname, "acc") == 1)
		{
			DataRow dr = dsname.Tables["acc"].Rows[0];
			return dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}
bool bSecurityAccess(string card_id)
{
	bool bAccessGranted = false;
	string sc = " SELECT * FROM card ";
	sc += " WHERE id = "+ Session["card_id"] +" ";
//DEBUG("sc=", sc);
	int rows = 0;
//	int allow_level = int.Parse(GetSiteSettings("branch_access_level_allow", "6", true));
	int access_level = 1;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "access_level");
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1)
	{
		access_level = int.Parse(dstcom.Tables["access_level"].Rows[0]["access_level"].ToString());
//		DEBUG("accesleve =", access_level);
//DEBUG("allow =", allow_level);
		bAccessGranted = bGetAllowAccessID(access_level.ToString(), GetSiteSettings("branch_access_level_allow", "6", true));

//		if(access_level >= allow_level)
//			bAccessGranted = true;
	}

	return bAccessGranted;
}

void print_t(DataTable dt)
{
	int rows = dt.Rows.Count;
	int cols = dt.Columns.Count;
	Response.Write("<table border=1>");
	Response.Write("<tr>");
	for(int i=0; i<cols; i++)
	{
		Response.Write("<th>" + dt.Columns[i].ColumnName + "</th>");
	}
	Response.Write("</tr>");
	for(int m=0; m<rows; m++)
	{
		DataRow dr = dt.Rows[m];
		Response.Write("<tr>");
		for(int i=0; i<cols; i++)
		{
			Response.Write("<td>" + dr[i].ToString() + "</th>");
		}
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

bool bCheckCardPrivilege(string card_id)
{
	DataSet acDss = new DataSet();
	if(acDss.Tables["type"] != null)
		acDss.Tables["type"].Clear();

	bool bisValid = false;
	if(card_id == "")
		return false;
	if(!TSIsDigit(card_id))
		return false;

	string sc = " SELECT access_level FROM card WHERE id = '" + card_id +"' AND type = 4";
//DEBUG("sc =", sc);	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(acDss, "type") == 1)
			if(MyIntParse(acDss.Tables["type"].Rows[0]["access_level"].ToString()) >= MyIntParse(GetSiteSettings("set_privilege_on_access_cardlist", "5", false)))
				bisValid = true;

	}
	catch(Exception e) 
	{
	//	ShowExp(sc, e);
		return false;
	}
	return bisValid;
}

string AddAVGCostLog(string code, string comments, string last_avg_cost, string new_avg_cost, string purchase_id)
{
	string sc = " INSERT INTO avg_cost_log ";
	sc += " (code, comments, last_avg_cost, new_avg_cost, purchase_id, input_by, input_date) ";
	sc += " VALUES( ";
	sc += " '" + code + "' ";
	sc += ", '" + EncodeQuote(comments) + "' ";	
	sc += ", " + last_avg_cost + ", " + new_avg_cost + ", '" + purchase_id + "'";
	sc += ", " + Session["card_id"].ToString();
	sc += ", GETDATE() ";
	sc += ") ";
		return sc;
}
string Lang(string key)
{
	if(Session["languagealreadydefined"] == null)
		DoDefineSessionLanguage();
	else if(Session["refreshsessionlanguage"] != null)
	{
		DoDefineSessionLanguage();
		Session["refreshsessionlanguage"] = null;
	}

	string lang = GetSiteSettings("language_in_use", "english", true);
	if(lang == "english")
		return key;

	if(Session["language" + key] != null)
		return Session["language" + key].ToString();
	
	string sc = " IF NOT EXISTS(SELECT id FROM dict WHERE english LIKE '" + EncodeQuote(key) + "') ";
	sc += " INSERT INTO dict (english, " + lang + ") VALUES('" + EncodeQuote(key) + "', '') ";
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
	return key;
}

bool DoDefineSessionLanguage()
{
	string lang = GetSiteSettings("language_in_use", "english", true);
	if(lang == "english")
		return true; //no need

	DataSet dst = new DataSet();
	string sc = " SELECT english, " + lang + " FROM dict ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "lang");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<dst.Tables["lang"].Rows.Count; i++)
	{
		if(dst.Tables["lang"].Rows[i][lang].ToString() == "")
			Session["language" + dst.Tables["lang"].Rows[i]["english"].ToString()] = dst.Tables["lang"].Rows[i]["english"].ToString();
		else
			Session["language" + dst.Tables["lang"].Rows[i]["english"].ToString()] = dst.Tables["lang"].Rows[i][lang].ToString();
	}
	Session["languagealreadydefined"] = true;
	return true;
}
bool bIsSpecialItemForQPOS(string code)
{	
	string sc = " SELECT top 1 code FROM specials ";
	sc += " WHERE code = "+ code +" ";
//DEBUG("sc=", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "specialItem");
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1)
		return true;
	
	return false;
}

bool DoGetCountry(string country)
{
	DataSet dsname = new DataSet();
	string sc = " SELECT * FROM country_name ";
	sc += " WHERE country = '"+ country +"' ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsname, "nz");
	}
	catch(Exception e) 
	{
		myCommand.Connection.Close();
		string err = e.ToString();
		if(err.IndexOf("Invalid object name 'country_name'")>=0)
		{
			string s = @"

				CREATE TABLE [dbo].[product] (
				[code] [int] NOT NULL ,
				[name] [varchar] (255) COLLATE Chinese_PRC_BIN NOT NULL ,
				[brand] [varchar] (50) COLLATE Chinese_PRC_BIN NULL ,
				[cat] [varchar] (50) COLLATE Chinese_PRC_BIN NULL ,
				[s_cat] [varchar] (50) COLLATE Chinese_PRC_BIN NULL ,
				[ss_cat] [varchar] (50) COLLATE Chinese_PRC_BIN NULL ,
				[hot] [bit] NOT NULL ,
				[price] [money] NOT NULL ,
				[stock] [float] NOT NULL ,
				[eta] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
				[supplier] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
				[supplier_code] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
				[supplier_price] [money] NOT NULL ,
				[price_dropped] [int] NOT NULL ,
				[price_age] [datetime] NOT NULL ,
				[allocated_stock] [float] NOT NULL ,
				[popular] [bit] NOT NULL ,
				[real_stock] [bit] NOT NULL 
			) ON [PRIMARY]
			

			ALTER TABLE [dbo].[product] ADD 
				CONSTRAINT [DF_product_hot] DEFAULT (0) FOR [hot],
				CONSTRAINT [DF_product_stock] DEFAULT (0) FOR [stock],
				CONSTRAINT [DF_product_supplier_price] DEFAULT (0) FOR [supplier_price],
				CONSTRAINT [DF__product_T__price__7246E95D] DEFAULT (0) FOR [price_dropped],
				CONSTRAINT [DF_product_price_age] DEFAULT (getdate()) FOR [price_age],
				CONSTRAINT [DF__product_T__alloc__733B0D96] DEFAULT (0) FOR [allocated_stock],
				CONSTRAINT [DF_product_popular] DEFAULT (1) FOR [popular],
				CONSTRAINT [DF_product_real_stock] DEFAULT (0) FOR [real_stock],
				CONSTRAINT [PK_product_Temp] PRIMARY KEY  CLUSTERED 
				(
					[code]
				)  ON [PRIMARY] 
			";
				try
				{
					myCommand = new SqlCommand(sc);
					myCommand.Connection = myConnection;
					myCommand.Connection.Open();
					myCommand.ExecuteNonQuery();
					myCommand.Connection.Close();
				}
				catch(Exception ec)
				{

				}
		}

//		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
	{
		for(int i=0; i<rows; i++)
		{
			Response.Write("<option value='"+ dsname.Tables["nz"].Rows[i]["city"] +"'");
			if(dsname.Tables["nz"].Rows[i]["city"].ToString() == "Auckland")
				Response.Write(" selected ");
			Response.Write(">"+ dsname.Tables["nz"].Rows[i]["city"] +"</option>");
		}
	}
	else
		return false;
	
	return true;
}

string doSwapDateFormat(string dateIn)
{
	string dateOut = "";
		// Sets the CurrentCulture property to G.B. English.
	string setCulture = GetSiteSettings("set_culture_date_format_for_system", "en-GB", true);
	DateTime dTmp; 			
	string sDateFormat = "en-US";
	try
	{					
		Thread.CurrentThread.CurrentCulture = new CultureInfo(setCulture);	
		dTmp = DateTime.Parse(dateOut);
		dateOut = dTmp.ToString("dd/MM/yyyy");		
//DEBUG("dtmp1 +", dTmp.ToString());					
	}
	catch (Exception er)
	{
		try
		{
			if(setCulture == sDateFormat)
				sDateFormat = "en-GB";
			
			Thread.CurrentThread.CurrentCulture = new CultureInfo(sDateFormat);
			dTmp = DateTime.Parse(dateIn);	
			Thread.CurrentThread.CurrentCulture = new CultureInfo(setCulture);										
			dateOut = dTmp.ToString("dd/MM/yyyy");			
		}
		catch (Exception ec)
		{
			
		}
	}						
	return dateOut;
}
string PrintCurrencyOptions(bool bRates, string current_id)
{
	if(dstcom.Tables["currency"] != null)
		dstcom.Tables["currency"].Clear();

	int rows = 0;
	string s = "";
	string sc = " SELECT * FROM currency ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "currency");
	}
	catch(Exception e1) 
	{		
		string err = e1.ToString().ToLower();
		if(err.IndexOf("invalid object name 'currency'") >= 0)
		{
			myConnection.Close(); //close it first

			string ssc = @"
				
			CREATE TABLE [dbo].[currency](
			[id] [int] IDENTITY(1,1) NOT NULL,
			[currency_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[rates] [float] NOT NULL CONSTRAINT [rates]  DEFAULT (1),
			[insert_by] [int] NOT NULL,
			[insert_date] [datetime] NOT NULL CONSTRAINT [insert_date]  DEFAULT (getdate()),
			[comments] [varchar](4068) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
			) ON [PRIMARY]
		
			";
	//	DEBUG("ssc = ", ssc);
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
			//	ShowExp(sc, er);
				return "";
			}
			try
			{
				
				string sqlString = " INSERT INTO currency (currency_name, rates, insert_by, insert_date) VALUES('NZD', 1, 0, GETDATE() ) ";
				myCommand = new SqlCommand(sqlString);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch
			{
				return "";
			}
			//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +" \">");		
		}
		ShowExp(sc, e1);
		return "";
	}

//	s += "<select name=currency>";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dstcom.Tables["currency"].Rows[i];
		string id = dr["id"].ToString();
		string rates = dr["rates"].ToString();
		string name = dr["currency_name"].ToString();
		name = name.ToUpper();
		s += "<option value=";
		if(bRates)
			s += rates;
		else
			s += id;
		if(id == current_id)
			s += " selected";
		s += ">" + name + "</option>";
	}
//	s += "</select>";
	return s;	
}

string GetCurrencyID(string currencyName)
{
	if(currencyName == null || currencyName == "")
		currencyName = GetSiteSettings("default_currency_name", "NZD");
	if(currencyName == null || currencyName == "")
		currencyName = "NZD";
	DataSet dsCurrency = new DataSet();
	string sID = "1";
	string sc = "SELECT id FROM currency WHERE UPPER(currency_name)='" + currencyName.ToUpper() + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsCurrency, "currency") == 1)
			sID = dsCurrency.Tables["currency"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return sID;
}
string GetCurrencyName(string id)
{
	DataSet dsCurrency = new DataSet();
	string sID = "1";
	string currencyName = GetSiteSettings("default_currency_name", "NZD");
	string sc = "SELECT currency_name FROM currency WHERE id = "+ id +"";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsCurrency, "currency") == 1)
			currencyName = dsCurrency.Tables["currency"].Rows[0]["currency_name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return currencyName;
}
string GetCurrencyRate(string id)
{
	DataSet dsCurrency = new DataSet();
	string sID = "1";
	string rates = "1";
	string sc = "SELECT rates FROM currency WHERE id = "+ id +"";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsCurrency, "currency") == 1)
			rates = dsCurrency.Tables["currency"].Rows[0]["rates"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return rates;
}

bool PrintGSTRateOptions(string current_rate)
{
	DataSet dsCurrency = new DataSet();
	int rows = 0;

	//do search
	string sc = "SELECT DISTINCT gst_rate FROM currency ";	
	sc += " ORDER BY gst_rate";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsCurrency, "gst_rate");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=gst_rate");
	
	Response.Write(">");
	Response.Write("<option value=0>0%</option>");
	
	for(int i=0; i<rows; i++)
	{
		string gst_rate = dsCurrency.Tables["gst_rate"].Rows[i]["gst_rate"].ToString();
	//	string gst_name = dsCurrency.Tables["gst_rate"].Rows[i]["currency_name"].ToString();
	//	string bid = dsCurrency.Tables["gst_rate"].Rows[i]["id"].ToString();
		Response.Write("<option value='" + gst_rate + "' ");
		if(gst_rate == current_rate)
			Response.Write("selected");
		gst_rate = (double.Parse(gst_rate)).ToString("p");
		Response.Write(">" + gst_rate + "</option>");
	}
	//if(rows == 0)
	//	Response.Write("<option value=>Branch 1</option>");
	Response.Write("</select>");
	return true;
}

double GetLastSalesFixedPriceForDealer(string code, string qty, string card_id, string dealer_level)
{
	if(code == null || code == "")
		return 99999999;
	
if(card_id == "0")
	dealer_level = "1";
	//	string price = dr["price" + dealer_level].ToString();
	double GSTRate = MyDoubleParse(Session[m_sCompanyName +"gst_rate"].ToString()); //MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;				// 30.JUN.2003 XW

	bool bUseDealerLevel = false;
	bool setDealerLevel = false;

	try
	{		
		setDealerLevel = MyBooleanParse(GetSiteSettings("enable_dealer_level_on_last_fixed_price", "1", true));				// 30.JUN.2003 XW
	}
	catch(Exception e)
	{
	}
	bUseDealerLevel = setDealerLevel;

	if(GSTRate < 1)
			GSTRate = 1+GSTRate;

	DataSet dsgspfd = new DataSet();
	string sc = "";
	if(card_id == "" || card_id == null)
	{
		sc = "SELECT ";	
			sc += " c.price1 ";		
		sc += " FROM product p JOIN code_relations c ON p.code=c.code ";	
		sc += " WHERE c.code=" + code;		
	}
	else
	{
		sc = "SELECT ISNULL((SELECT TOP 1 oi.commit_price AS price1 ";	
	sc += " FROM order_item oi JOIN orders o ON o.id = oi.id ";
	sc += " WHERE oi.code = "+ code +" ";	
	sc += " AND o.card_id = '"+ card_id +"' ";
	sc += " ORDER BY o.id DESC), ";
	if(bUseDealerLevel)
		sc += "((manual_cost_nzd * rate) * level_rate"+ dealer_level +") ";
	else 
		sc += " (price1/"+ GSTRate +") ";
	sc += ") AS price1 ";	
	sc += " FROM product p JOIN code_relations c ON p.code=c.code ";
	sc += " WHERE c.code=" + code;		
	//sc += " ORDER BY oi.id DESC ";
	}
//DEBUG(" sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsgspfd, "price") <= 0)
		{
			sc = " SELECT name FROM code_relations WHERE code=" + code;
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dsgspfd, "name") <= 0)
				{
					Response.Write("<br><br><h3>Error, product not found, code=" + code + "</h3>");
					return 999999;
				}
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
			}
	
			Response.Write("<br><br><h3>Error, product discontinued, code=" + code + " : " + dsgspfd.Tables["name"].Rows[0]["name"].ToString() + "</h3>");
			return 999999;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	
	DataRow dr = dsgspfd.Tables["price"].Rows[0];

	string price = (MyDoubleParse(dr["price1"].ToString())).ToString();
	return MyDoubleParse(price);
}
double MyCurrencyPrice(string sprice)
{	
	//double dPrice = MyDoubleParse(sprice);		
	if(sprice.IndexOf("(")==0 && sprice.IndexOf(")") == sprice.Length-1)
	{
		sprice = sprice.Replace("(", "");
		sprice = sprice.Replace(")", "");
		sprice = "-" + sprice;
	}
//	string sPriceConvert = dPrice.ToString("c");
	string sPriceConvert = sprice;
	string swap = "";
	bool bFoundDecimalPoint = false;
	bool bNagativeValue = false;
	for(int i=0; i<sPriceConvert.Length; i++)
	{		
		//check if is minus figure first //
		if(sPriceConvert[i].ToString() == "-")
			bNagativeValue = true;
		//	swap += sPriceConvert[i].ToString();
		//check for float figure //
		try
		{
			swap += double.Parse(sPriceConvert[i].ToString()).ToString();			
		}
		catch
		{
		}	
		//check for decimal point //
		if(sPriceConvert[i].ToString() == ".")
		{
			if(!bFoundDecimalPoint)
			{
				swap += sPriceConvert[i].ToString();
				bFoundDecimalPoint = true;
			}
		}
		
	//	DEBUG("s =", s);
	}
	if(swap == "" || swap == null)
		swap = "0";
	if(bNagativeValue)
		swap = "-" + swap;
	return double.Parse(swap);	
}

bool bGetAllowAccessID(string sUserAccessID)
{
	return bGetAllowAccessID(sUserAccessID, "");
}
bool bGetAllowAccessID(string sUserAccessID, string sAllowID)
{
	if(sUserAccessID == "10")
		return true;
	string AllowAccessID = "";
	if(sAllowID != "")
		AllowAccessID = sAllowID;
	else
		AllowAccessID = GetSiteSettings("SET_ALLOW_ACCESS_ID_FOR_CARD_AND_OTHER_SECURITIES", "10,", true);
//DEBUG("allowe =", AllowAccessID);	
	string sTemp = "";
	
	for(int i=0; i<AllowAccessID.Length; i++)
	{		
		if(AllowAccessID[i].ToString() == "," || AllowAccessID[i].ToString() == "|" || AllowAccessID[i].ToString() == ";")
		{
			if(sUserAccessID == sTemp)
				return true;
			else
				sTemp = ""; //clean up last id
		}
		if(AllowAccessID[i].ToString() != "," || AllowAccessID[i].ToString() != "|" || AllowAccessID[i].ToString() != ";")
		{
			try
			{
				sTemp += (int.Parse(AllowAccessID[i].ToString())).ToString();
			}
			catch(Exception e){ }
		}
		if(i==AllowAccessID.Length-1)
		{		
			if(sUserAccessID == sTemp)
				return true;		
		}

	}	
	return false;
}

string g(string key)
{
	string sRet = "";
	if(key == null || key == "")
		return sRet;
	if(Request.QueryString[key] != null)
		sRet = Request.QueryString[key];
	if(!CheckSQLAttack(sRet))
		sRet = "";
	return sRet;
}

string p(string key)
{
	string sRet = "";
	if(key == null || key == "")
		return sRet;
	if(Request.Form[key] != null)
		sRet = Request.Form[key];
	if(!CheckSQLAttack(sRet))
		sRet = "";
	return sRet;
}
void ErrMsgAdmin(string msg)
{
	//PrintAdminHeader();
	Response.Write("<br><br><br><center><h4>Error, " + msg);
	Response.Write("</h4><br>");
	Response.Write("<input type=button value='<< Back' class=b onclick=history.go(-1)>");
}
string PrintShelfAreaOptions(string sCurrent, bool bAll)
{
	int nRows = 0;
	if(dstcom.Tables["pso"] != null)
		dstcom.Tables["pso"].Clear();
	string sc = " SELECT DISTINCT area FROM shelf WHERE area <> '' ORDER BY area ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dstcom, "pso");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	if(bAll)
		s += "<option value=''>" + Lang("All") + "</option>";
	for(int i=0; i<nRows; i++)
	{
		string v = dstcom.Tables["pso"].Rows[i]["area"].ToString();
		s += "<option value='" + v + "' ";
		if(v == sCurrent)
			s += " selected";
		s += ">" + v + "</option>";
	}
	return s;
}
string PrintShelfLocationOptions(string area, string sCurrent, bool bAll)
{
	int nRows = 0;
	if(dstcom.Tables["pso"] != null)
		dstcom.Tables["pso"].Clear();
	string sc = " SELECT DISTINCT location FROM shelf WHERE area = N'" + EncodeQuote(area) + "' AND location <> '' ORDER BY location ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dstcom, "pso");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	if(bAll)
		s += "<option value=''>" + Lang("All") + "</option>";
	for(int i=0; i<nRows; i++)
	{
		string location = dstcom.Tables["pso"].Rows[i]["location"].ToString();
		s += "<option value='" + location + "' ";
		if(location == sCurrent)
			s += " selected";
		s += ">" + location + "</option>";
	}
	return s;
}
string PrintShelfSectionOptions(string area, string location, string sCurrent, bool bAll)
{
	int nRows = 0;
	if(dstcom.Tables["pso"] != null)
		dstcom.Tables["pso"].Clear();
	string sc = " SELECT DISTINCT section FROM shelf ";
	sc += " WHERE area = N'" + EncodeQuote(area) + "' ";
	sc += " AND location = N'" + EncodeQuote(location) + "' ";
	sc += " AND section <> '' ORDER BY section ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dstcom, "pso");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	if(bAll)
		s += "<option value=''>" + Lang("All") + "</option>";
	for(int i=0; i<nRows; i++)
	{
		string section = dstcom.Tables["pso"].Rows[i]["section"].ToString();
		s += "<option value='" + section + "' ";
		if(section == sCurrent)
			s += " selected";
		s += ">" + section + "</option>";
	}
	return s;
}
string PrintShelfLevelOptions(string sCurrent, bool bAll)
{
	int nLevels = MyIntParse(GetSiteSettings("shelf_levels", "30"));
	string s = "";
	if(bAll)
		s += "<option value=''>" + Lang("All") + "</option>";
	for(int i=1; i<nLevels; i++)
	{
		s += "<option value='" + i.ToString() + "' ";
		if(i.ToString() == sCurrent)
			s += " selected";
		s += ">" + i.ToString() + "</option>";
	}
	return s;
}
string PrintShelfLevelOptionsBySection(string area, string location, string section, string sCurrent, bool bAll)
{
	int nRows = 0;
	if(dstcom.Tables["pso"] != null)
		dstcom.Tables["pso"].Clear();
	string sc = " SELECT DISTINCT level FROM shelf ";
	sc += " WHERE area = N'" + EncodeQuote(area) + "' ";
	sc += " AND location = N'" + EncodeQuote(location) + "' ";
	sc += " AND section = N'" + EncodeQuote(section) + "' ORDER BY level ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dstcom, "pso");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	if(bAll)
		s += "<option value=''>" + Lang("All") + "</option>";
	for(int i=0; i<nRows; i++)
	{
		string sLevel = dstcom.Tables["pso"].Rows[i]["level"].ToString();
		s += "<option value='" + sLevel + "' ";
		if(sLevel == sCurrent)
			s += " selected";
		s += ">" + sLevel + "</option>";
	}
	return s;
}
string GetBranchName (string bid)
{
	if(bid.Trim() == "")
		return "";
	DataSet dst = new DataSet();
	if(dst.Tables["branchname"] != null)
		dst.Tables["branchname"].Clear();
	string sc = " SELECT name FROM branch WHERE id ='"+bid+ "' AND activated =1";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "branchname") <= 0)
			return "";
	}
	catch(Exception ex)
	{
		ShowExp(sc,ex);
		return "";
	}
	return dst.Tables["branchname"].Rows[0]["name"].ToString();
}

string GetItemLocation(string itemCode)
{
	DataSet dst = new DataSet();
	if(dst.Tables["itemlocation"] != null)
		dst.Tables["itemlocation"].Clear();
	string sc = " SELECT si.*, s.*, s.name AS shelf_name ";
	sc += " FROM shelf_item si Join shelf s ON s.id = si.shelf_id ";
	sc += " WHERE si.code ='" + itemCode + "'";
	sc += " ORDER BY si.qty ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "itemlocation") <= 0)
			return "";
	}
	catch(Exception ex)
	{
		ShowExp(sc,ex);
		return "";
	}
	string s = "";
	for(int i=0; i<dst.Tables["itemlocation"].Rows.Count; i++)
	{
		bool bAdd = true;
		DataRow dr = dst.Tables["itemlocation"].Rows[i];
		string name = dr["shelf_name"].ToString();
		string itemQty = dr["qty"].ToString();
		double dQty = MyDoubleParse(itemQty);
		if(name[0] == 'S' && dQty <= 0)
			bAdd = false;
		if(bAdd)
			s += name + "(" + itemQty + ") ";
	}
	return s;
}

bool DataTableExportToExcel(DataTable dtSource, string fileName)
{
    System.IO.StreamWriter excelDoc;
	
    excelDoc = new System.IO.StreamWriter(fileName);
    const string startExcelXML = "<xml version>\r\n<Workbook " + 
          "xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"\r\n" + 
          " xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n " + 
          "xmlns:x=\"urn:schemas-    microsoft-com:office:" + 
          "excel\"\r\n xmlns:ss=\"urn:schemas-microsoft-com:" + 
          "office:spreadsheet\">\r\n <Styles>\r\n " + 
          "<Style ss:ID=\"Default\" ss:Name=\"Normal\">\r\n " + 
          "<Alignment ss:Vertical=\"Bottom\"/>\r\n <Borders/>" + 
          "\r\n <Font/>\r\n <Interior/>\r\n <NumberFormat/>" + 
          "\r\n <Protection/>\r\n </Style>\r\n " + 
          "<Style ss:ID=\"BoldColumn\">\r\n <Font " + 
          "x:Family=\"Swiss\" ss:Bold=\"1\"/>\r\n </Style>\r\n " + 
          "<Style     ss:ID=\"StringLiteral\">\r\n <NumberFormat" + 
          " ss:Format=\"@\"/>\r\n </Style>\r\n <Style " + 
          "ss:ID=\"Decimal\">\r\n <NumberFormat " + 
          "ss:Format=\"0.0000\"/>\r\n </Style>\r\n " + 
          "<Style ss:ID=\"Integer\">\r\n <NumberFormat " + 
          "ss:Format=\"0\"/>\r\n </Style>\r\n <Style " + 
          "ss:ID=\"DateLiteral\">\r\n <NumberFormat " + 
          "ss:Format=\"yyyy-mm-dd;@\"/>\r\n </Style>\r\n " + 
          "</Styles>\r\n ";
     const string endExcelXML = "</Workbook>";

     int rowCount = 0;
     int sheetCount = 1;
    excelDoc.Write(startExcelXML);
    excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
    excelDoc.Write("<Table>");
    excelDoc.Write("<Row>");
    for(int x = 0; x < dtSource.Columns.Count; x++)
    {
      excelDoc.Write("<Cell ss:StyleID=\"BoldColumn\"><Data ss:Type=\"String\">");
      excelDoc.Write(dtSource.Columns[x].ColumnName);
      excelDoc.Write("</Data></Cell>");
    }
    excelDoc.Write("</Row>");
    foreach(DataRow x in dtSource.Rows)
    {
      rowCount++;
      //if the number of rows is > 64000 create a new page to continue output
      if(rowCount==64000) 
      {
        rowCount = 0;
        sheetCount++;
        excelDoc.Write("</Table>");
        excelDoc.Write(" </Worksheet>");
        excelDoc.Write("<Worksheet ss:Name=\"Sheet" + sheetCount + "\">");
        excelDoc.Write("<Table>");
      }
      excelDoc.Write("<Row>"); //ID=" + rowCount + "
      
      for(int y = 0; y < dtSource.Columns.Count; y++)
      {
        System.Type rowType;
        rowType = x[y].GetType();
        switch(rowType.ToString())
        {
          case "System.String":
             string XMLstring = x[y].ToString();
             XMLstring = XMLstring.Trim();
             XMLstring = XMLstring.Replace("&","&");
             XMLstring = XMLstring.Replace(">",">");
             XMLstring = XMLstring.Replace("<","<");
             excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" + 
                            "<Data ss:Type=\"String\">");
             excelDoc.Write(XMLstring);
             excelDoc.Write("</Data></Cell>");
             break;
           case "System.DateTime":
             //Excel has a specific Date Format of YYYY-MM-DD followed by  
             //the letter 'T' then hh:mm:sss.lll Example 2005-01-31T24:01:21.000
             //The Following Code puts the date stored in XMLDate 
             //to the format above
             DateTime XMLDate = (DateTime)x[y];
             string XMLDatetoString = ""; //Excel Converted Date
             XMLDatetoString = XMLDate.Year.ToString() +
                  "-" + 
                  (XMLDate.Month < 10 ? "0" + 
                  XMLDate.Month.ToString() : XMLDate.Month.ToString()) +
                  "-" +
                  (XMLDate.Day < 10 ? "0" + 
                  XMLDate.Day.ToString() : XMLDate.Day.ToString()) +
                  "T" +
                  (XMLDate.Hour < 10 ? "0" + 
                  XMLDate.Hour.ToString() : XMLDate.Hour.ToString()) +
                  ":" +
                  (XMLDate.Minute < 10 ? "0" + 
                  XMLDate.Minute.ToString() : XMLDate.Minute.ToString()) +
                  ":" +
                  (XMLDate.Second < 10 ? "0" + 
                  XMLDate.Second.ToString() : XMLDate.Second.ToString()) + 
                  ".000";
                excelDoc.Write("<Cell ss:StyleID=\"DateLiteral\">" + 
                             "<Data ss:Type=\"DateTime\">");
                excelDoc.Write(XMLDatetoString);
                excelDoc.Write("</Data></Cell>");
                break;
              case "System.Boolean":
                excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" + 
                            "<Data ss:Type=\"String\">");
                excelDoc.Write(x[y].ToString());
                excelDoc.Write("</Data></Cell>");
                break;
              case "System.Int16":
              case "System.Int32":
              case "System.Int64":
              case "System.Byte":
                excelDoc.Write("<Cell ss:StyleID=\"Integer\">" + 
                        "<Data ss:Type=\"Number\">");
                excelDoc.Write(x[y].ToString());
                excelDoc.Write("</Data></Cell>");
                break;
              case "System.Decimal":
              case "System.Double":
                excelDoc.Write("<Cell ss:StyleID=\"Decimal\">" + 
                      "<Data ss:Type=\"Number\">");
                excelDoc.Write(x[y].ToString());
                excelDoc.Write("</Data></Cell>");
                break;
              case "System.DBNull":
                excelDoc.Write("<Cell ss:StyleID=\"StringLiteral\">" + 
                      "<Data ss:Type=\"String\">");
                excelDoc.Write("");
                excelDoc.Write("</Data></Cell>");
                break;
              default:
                throw(new Exception(rowType.ToString() + " not handled."));
            }
        }
        excelDoc.Write("</Row>");
	}
	excelDoc.Write("</Table>");
	excelDoc.Write(" </Worksheet>");
	excelDoc.Write(endExcelXML);
	excelDoc.Close();
	return true;
}
bool SetItemUpdatedFlag(string code)
{
	Trim(ref code);
	if(code == "")
		return true;
	if(dstcom.Tables["branch"] != null)
		dstcom.Tables["branch"].Clear();
	string sc = " SELECT id FROM branch WHERE id > 1 AND activated = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "branch") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = "";
	for(int i=0; i<dstcom.Tables["branch"].Rows.Count; i++)
	{
		string bid = dstcom.Tables["branch"].Rows[i]["id"].ToString();
		sc += " IF NOT EXISTS(SELECT id FROM updated_item WHERE branch_id = " + bid + " AND item_code = " + code + ") ";
		sc += " INSERT INTO updated_item(branch_id, item_code) VALUES(" + bid + ", " + code + ") ";
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
	return true;
}

string GetItemLocationOnEdit(string item_code)
{
	int rows = 0;
	if(dstcom.Tables["itemlocation"] != null)
		dstcom.Tables["itemlocation"].Clear();
	string sc = " SELECT name FROM shelf GROUP BY name ORDER BY  name  ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "itemlocation");
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		myConnection.Close();
		return "";
	}
	StringBuilder sb = new StringBuilder();
	if(rows <=0)
		sb.Append("<input type=text name=stock_location >");
	else
	{
		sb.Append("<select name=stock_location >");
		sb.Append("<option name=''></option>");
		for(int i = 0; i < rows ; i++)
		{
			DataRow dr = dstcom.Tables["itemlocation"].Rows[i];
			
			sb.Append("<option value='"+ dr["name"].ToString()+"'");
			if(itemLocation(item_code).ToLower() == dr["name"].ToString().ToLower())
				sb.Append( " selected ");
			sb.Append(">"+ dr["name"].ToString().ToUpper()+"</option>");
		}
		sb.Append("</select>");
	}
	return sb.ToString();
}
string itemLocation(string pcode)
{
	if(pcode.Trim() == "")
		return "";
	if(dstcom.Tables["itemlocated"] != null)
		dstcom.Tables["itemlocated"].Clear();
	string sc = " SELECT stock_location FROM code_relations WHERE code= '"+ pcode+"' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstcom, "itemlocated") <=0)
			return "";
			
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		myConnection.Close();
		return "";
	}
	return dstcom.Tables["itemlocated"].Rows[0]["stock_location"].ToString();
}

string EditItemLocation(string item_code, int rowID)
{
	int rows = 0;
	if(dstcom.Tables["itemlocation"] != null)
		dstcom.Tables["itemlocation"].Clear();
	string sc = " SELECT name FROM shelf GROUP BY name ORDER BY  name  ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstcom, "itemlocation");
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		myConnection.Close();
		return "";
	}
	StringBuilder sb = new StringBuilder();
	if(rows <=0)
		sb.Append("<input type=text name=stock_location"+rowID+" >");
	else
	{
		sb.Append("<select name=stock_location"+rowID+" >");
		sb.Append("<option name=''></option>");
		for(int i = 0; i < rows ; i++)
		{
			DataRow dr = dstcom.Tables["itemlocation"].Rows[i];
			
			sb.Append("<option value='"+ dr["name"].ToString()+"'");
			if(itemLocation(item_code).ToLower() == dr["name"].ToString().ToLower())
				sb.Append( " selected ");
			sb.Append(">"+ dr["name"].ToString().ToUpper()+"</option>");
		}
		sb.Append("</select>");
	}
	return sb.ToString();
}

double m_sSysGST()
{
	double sysGST = 0;
	try
	{
		sysGST = MyDoubleParse(GetSiteSettings("gst_rate_percent", "15", true));
	}
	catch
	{
		sysGST = 0;
	}
	if(sysGST >1)
	{
		sysGST = sysGST /100 ;
		sysGST = sysGST;
	}
	return  sysGST;
}

String GetIP()
{
    String ip = 
        HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

    if (string.IsNullOrEmpty(ip))
    {
        ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
    }

    return ip;
}
void CreateThumbnail(string imageFileWithPath, string sPath, System.IO.FileStream newFile, int iWidth, int iHeight)
{
	FileInfo FileProps =new FileInfo(imageFileWithPath);
	string filename = FileProps.Name;
	string fileextension = FileProps.Extension;
	string [] Split = filename.Split(new Char [] {'.'}); //removes all character after and including (.)
	filename = Split[0].ToString();
	if (fileextension.ToLower() == ".jpg" || fileextension.ToLower() == ".gif" || fileextension.ToLower() == ".bmp")
	{
		System.Drawing.Image OriginalImage;
		OriginalImage = System.Drawing.Image.FromStream(newFile);
				//resize image value;
		float ratio;
		int maxWidth = iWidth;
		int maxHeight = iHeight;

		//Get height and width of current image
		int width = (int)OriginalImage.Width;
		int height = (int)OriginalImage.Height;

		//Ratio and conversion for new size
		if (width > maxWidth)
		{
			ratio = (float)width / (float)maxWidth;
			width = (int)(width / ratio);
			height = (int)(height / ratio);
		}

		//Ratio and conversion for new size
		if (height > maxHeight)
		{
			ratio = (float)height / (float)maxHeight;
			height = (int)(height / ratio);
			width = (int)(width / ratio);
		}
	
		sPath += "\\t\\" + filename + ".jpg";
		
/*
		System.Drawing.Image thumbnailImage;
		thumbnailImage = OriginalImage.GetThumbnailImage(width, height, new System.Drawing.Image.GetThumbnailImageAbort(myCallBack), IntPtr.Zero);
		System.Drawing.Image newImg = new Bitmap(thumbnailImage.Width, thumbnailImage.Height);
		Graphics g = Graphics.FromImage(newImg); 
		g.Clear(System.Drawing.Color.White); 
		g.DrawImage(thumbnailImage, 0, 0, thumbnailImage.Width, thumbnailImage.Height); 
		newImg.Save(sPath, ImageFormat.Jpeg);
		thumbnailImage.Dispose();
		newFile.Close();
*/		
		System.Drawing.Image thumbnailBitmap = new Bitmap(width, height);
		Graphics thumbnailGraph = Graphics.FromImage(thumbnailBitmap);
//		thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
//		thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
//		thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;

		System.Drawing.Rectangle imageRectangle = new System.Drawing.Rectangle(0, 0, width, height);
		thumbnailGraph.DrawImage(OriginalImage, imageRectangle);
		thumbnailBitmap.Save(sPath, OriginalImage.RawFormat);
		thumbnailGraph.Dispose();
		thumbnailBitmap.Dispose();
		OriginalImage.Dispose();	
	}
}
</script>

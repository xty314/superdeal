<!-- #include file="kit_fun.cs" -->

<script runat=server>

string m_page = "about";
DataSet dstsp = new DataSet();
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(Request.QueryString.Count > 0)
		m_page = Request.QueryString[0];
    
    if((Request.QueryString[0].ToLower() == "contact_form") && Request.Form["name"] != null)
	{
		if(DoSendEmail())
		{
			Response.Write("<script  type=text/javascript ");
			Response.Write("> alert('Your message has been sent to us. Thank You')");
			Response.Write("</script");
			Response.Write("><meta http-equiv=\"REFRESH\" content=\"0;url='c.aspx'\">");
		}
		else
		{
			Response.Write("<script  type=text/javascript ");
			Response.Write("> alert('Make sure filled the form propertly.Thank You')");
			Response.Write("</script");
			Response.Write("><meta http-equiv=\"REFRESH\" content=\"0;url='sp.aspx?contact_form'\">");
		}
		
	}
	
	if(!CheckSQLAttack(m_page))
		return;

	PrintHeaderAndMenu();
	if(m_page.IndexOf(".") >= 0)
	{
		PrintFooter();
		return;
	}
	string Left_handside_manu = "";
	string spage = "";
	if(!CheckforExistingData(m_page))
		spage = "";
	else
		spage = ReadSitePage(m_page);
		
	if(m_sSite == "www")
	    Left_handside_manu = ReadSitePage("public_left_side_menu");
	if(m_sSite =="dealer")
		Left_handside_manu = ReadSitePage("left_side_menu");
			
	spage = spage.Replace("@@LEFT_SIDE_MENU", Left_handside_manu);
    
    string login =@"
					<a class='top_base' href='sp.aspx?account'>MyAccount</a> &nbsp; | &nbsp; <a href='login.aspx?logoff=true' class='top_base'>Logout</a>";

	string logout =@"
					<a class='top_base_in' href='login.aspx'>Login</a> &nbsp; | &nbsp; <a href='Register.aspx' class='top_base'>Register</a>";
	
	if(m_page == "about" || m_page == "career" || m_page == "contact_form" || m_page == "contacts" || m_page == "downloads" || m_page == "news" || m_page == "promotion" || m_page == "solution" || m_page == "solution_aio" || m_page == "solution_crb" || m_page == "solution_eftpos" || m_page == "solution_rs" || m_page == "support")
	{
		if(Session[m_sCompanyName + "loggedin"] != null && Session[m_sCompanyName + "loggedin"] != "")
			spage = spage.Replace("@@111", login);
		spage = spage.Replace("@@111", logout);
	}

	if(Cache["item_categories"] != null)
		spage = spage = spage.Replace("@@HEADER_MENU_TOP_CAT", Cache["item_categories"].ToString());
	else
		spage =spage = spage.Replace("@@HEADER_MENU_TOP_CAT", "");

	string READ_LSMENU = "";

//	Response.Write(ReadSitePage(m_page));
	Response.Write(spage);
	PrintFooter();
}
bool CheckforExistingData(string spage)
{
	bool bFound = false;
	string sc = " SELECT name FROM site_pages WHERE name = '"+ EncodeQuote(spage) +"'";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstsp, "sitepages");		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
		bFound = true;
//DEBUG("found =", bFound);
	return bFound;
}
bool DoSendEmail()
{
	string name = Request.Form["name"];
	string message = Request.Form["message"];
	string email = Request.Form["email"];
	string company = Request.Form["company"];
	//string subject = Request.Form["subject"];
	string receiver = "info@superdealnz.co.nz";//GetSiteSettings("account_manager_email", "alert@eznz.com");
	
	// Validate detail from user
	if(name == "" || email.IndexOf("@") <= 0 || message == "")
		return false;
		
		string body = "Name:" + name + "<br><br>";
	body += "Email:" + email + "<br><br>";
	if(company !="")
		body += "Company:" + company +"<br><br>";
	
	body += "Message:" + message + "<br>";

	MailMessage msgMail = new MailMessage();
	msgMail.To = receiver;
	msgMail.From = "website@superdealnz.co.nz";
	msgMail.Subject = "online enquiry";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = body;
	SmtpMail.Send(msgMail);
	return true;
	
}

</script>

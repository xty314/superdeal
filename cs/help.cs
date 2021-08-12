<script runat=server>

string m_page = "help_index";
string m_content = "";
string m_helpid = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["q"] != null && Request.QueryString["q"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		if(SentMail())
		{
			Response.Write("<br><br><h3>&nbsp;&nbsp;&nbsp;......Your Request was sent !</h3>");
		}
		else
		{
			Response.Write("<br><br><h3>&nbsp;&nbsp;&nbsp;......Failure of sending request mail, Please try it again !</h3>");
		}
		LFooter.Text = m_sAdminFooter;
		return;
	}

	if(Request.QueryString["p"] != null && Request.QueryString["p"].Length > 0)
	{
		m_page = Request.QueryString["p"];
	}
	m_content = ReadHelpPage(m_page, ref m_helpid);	

	if(Request.Form["help_topic"] != null)
	{
		SaveHelpPage(Request.Form["help_id"], Request.Form["help_topic"], EncodeQuote(Request.Form["txt"]));
		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=help.aspx?p=" + m_page + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	/*
	if(Request.QueryString["t"] != null && Request.QueryString["t"] == "edit")
	{
		PrintAdminHeader();
		PrintAdminMenu();
	}
	else
	{
		PrintHeaderAndMenu();
	}		*/

	if(Request.QueryString["t"] == "edit")
		DisplayEdit();
	else
		PrintHelp();

	LFooter.Text = m_sAdminFooter;
	
	//Response.Write(ReadHelpPage(m_page));


	/*
	if(Request.QueryString["t"] != null && Request.QueryString["t"] == "edit")
	{
		LFooter.Text = m_sAdminFooter;
	}
	else
	{
		PrintFooter();
	}	*/
}

bool SentMail()
{
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = "alert@eznz.com"; 
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = "Request Help Topic Notify";
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "Dear " + ":\r\n\r\n";
	msgMail.Body += Session["name"] + " has post a new question/message :\r\n";
	msgMail.Body += "-------------------------------------------------------------------------------------------------\r\n";
	msgMail.Body += "Subject : Help Topic is Needed on "+ Request.Form["s_url"] + "\r\n\r\n";
	msgMail.Body += "Specific Question:" + "\r\n";
	msgMail.Body += Request.Form["s_question"] + "\r\n\r\n";
	msgMail.Body += "From: " + Session["name"] + " - Login: " + Session["email"] + "\r\n";
	msgMail.Body += "-------------------------------------------------------------------------------------------------\r\n";
	//msgMail.Body += "http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "\r\n\r\n";
	msgMail.Body += "Have a good day.\r\n";
	msgMail.Body += "EZNZ Team\r\n";
	msgMail.Body += DateTime.Now.ToString("MMM.dd.yyyy");
	SmtpMail.Send(msgMail);

	return true;
}

void PrintHelp()
{
	//string text = ReadHelpPage(m_page);
	if(m_content != "")
	{
		Response.Write("<br><table width=75% border=0 cellspacing=0 cellpadding=0 align=center>");
		Response.Write("<tr><td width=100%><p align=justify>" + m_content + "</td></tr>");
		Response.Write("</table>");
	}
	else
	{
		Response.Write("<form method=post action='help.aspx?q=1' name=rqfrm");
		Response.Write("<br><br><table width=75% border=0 cellspacing=0 cellpadding=0 align=center>");
		Response.Write("<tr><td><font size=+1><b>Content - </b></font>");
		Response.Write("<font color=red><b>Not Available</b></font><br><hr></td></tr>\r\n");
		Response.Write("<tr><td>&nbsp;</td></tr>");
		Response.Write("<tr><td>We apologize for lack information on this topic.<br><br>");
		Response.Write("Please send us a request for help information on the topic. We will add information on the topic");
		Response.Write(" as soon as we receive your request.<br>\r\n");
		Response.Write("<br><br><font color=blue><b>To Send a help Request</b></font><br>");
		Response.Write("<br>If you have specific questions, please write down the question. ");
		Response.Write("Click on <font color=blue>Send Request</font> button ");
		Response.Write("then we will receive your request by e-mail automatcially.<br><br><br></td></tr>\r\n");

		//check login status
		if(TS_UserLoggedIn())
		{
			//User Account Information
			Response.Write("<tr><td><table width=40% border=0 cellspacing=0 cellpadding=0 align=left>");
			Response.Write("<tr bgcolor=#CCCCCC><td><i><b>Request By:</b></i></td></tr>");
			Response.Write("<tr><td><br>Name: " + Session["name"] + "</td></tr>");
			Response.Write("<tr><td><br>Login: " + Session["email"] + "</td></tr>");
			Response.Write("<tr><td><input type=hidden name=s_url value='" + Request.ServerVariables["URL"] + "?");
			Response.Write(Request.ServerVariables["QUERY_STRING"] + "'>");
			Response.Write("</table>");
			//Question field
			Response.Write("<table width=60% border=0 cellspacing=0 cellpadding=0 align=left>");
			Response.Write("<tr bgcolor=#CCCCCC><td><i><b>Specific Question:</b></i></td></tr>");
			Response.Write("<tr><td><br><textarea name=s_question cols=50 rows=5></textarea></td></tr>");
			Response.Write("</table></td></tr>");
		}
		else
			Response.Write("<tr><td>Please Login to send request.</td></tr>");
		Response.Write("<tr align=center><td><br><br><input type=submit name=request value=' Send Request '></td></tr>");
		Response.Write("</form></table>");
	}	
	Response.Write("<br><br><br><br><br><br><br><br><br>");
	Response.Write("<br><br><br><br><br><br><br><br><br>");
	Response.Write("<a href=help.aspx?t=edit&p=" + m_page + ">edit</a>");
}

void DisplayEdit()
{
	Response.Write("<form action=help.aspx?p=" + m_page + " method=post>");
	Response.Write("<input type=hidden name=help_id value=" + m_helpid + ">");
	Response.Write("<center><h3>Help - <font color=red>" + m_page.ToUpper() + "</font></h3>");
	Response.Write("<table border=1><tr><td><b>TOPIC: &nbsp;&nbsp;</b></td><td height=25><b><font color=blue>");
	Response.Write("<input type=text name=help_topic size=70 value=" + m_page + "></font></b></td></tr>");
	Response.Write("<tr><td valign=top><b>CONTENT: &nbsp;&nbsp;</b></td>");
	Response.Write("<td><textarea name=txt rows=30 cols=120>");
	Response.Write(m_content);
	Response.Write("</textarea>");
	Response.Write("<tr><td colspan=2 align=center>");
	Response.Write("<input type=submit name=cmd value=' Save '>");
	Response.Write("<input type=button value=Cancel onclick=window.location=('help.aspx?p=" + m_page + "&r=" + DateTime.Now.ToOADate() + "')>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=checkbox name=del_confirm>Tick to confirm deletion <input type=submit name=cmd value=Delete>");
	Response.Write("</td></tr></table>");
	Response.Write("</form>");

//	return;
}

string ReadHelpPage(string page)
{
	string id = "";
	return ReadHelpPage(page, ref id);
}

string ReadHelpPage(string page, ref string id)
{
	string s = "";
	string sc = "SELECT id, content FROM help WHERE page='";
	sc += page;
	sc += "'"; 
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		DataSet ds = new DataSet();
		rows = myCommand.Fill(ds);
		if(rows > 0)
		{
			s = ds.Tables[0].Rows[0]["content"].ToString();
			id = ds.Tables[0].Rows[0]["id"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	if(rows == 0)
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO help (page, content) VALUES('" + page + "', '') ";
		sc += " SELECT IDENT_CURRENT('help') AS id";
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

bool SaveHelpPage(string id, string page, string content)
{
	string sc = "";
	if(page == "" && content == "" && id != "")
		sc = "DELETE FROM help WHERE id=" + id;
	else
	{
		sc = "UPDATE help SET content='";
		sc += content;
		sc += "', page='"; 
		sc += page;
		sc += "' WHERE id=" + id;
	}
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
</script>

<asp:Label id=LFooter runat=server/>

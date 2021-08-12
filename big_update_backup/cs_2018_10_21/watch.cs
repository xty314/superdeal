<script runat=server>

string m_type = "";
string m_code = "";
protected void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	PrintHeaderAndMenu();

	if(Request.Form["cmd"] != null)
	{
		DoFeedBack();
	}
	else
	{
		if(Request.QueryString["code"] != null)
			m_code = Request.QueryString["code"];
		if(Request.QueryString["t"] != null)
			m_type = Request.QueryString["t"];
		PrintBody();
	}

	PrintFooter();
}

void PrintBody()
{
	Response.Write("<br>");
	Response.Write("<center><h3>Welcome To Feed Back</h3>");
	Response.Write("<form action=feedback.aspx method=post>");
	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<table>");
	Response.Write("<tr><td><br><b>&nbsp;Product Information</b><br>");
//	Response.Write("<tr><td>welcome To Feed Back</td></tr>");
//	Response.Write("<tr><td>Product Info");
	Response.Write("<table border=1>");
	Response.Write("<tr><td></td></tr>");
	if(m_code != "")
	{
		DataRow dr = null;
		if(!GetProduct(m_code, ref dr))
			return;
		double dPrice = double.Parse(dr["price"].ToString());
		Response.Write("<input type=hidden name=current_price value='" + dPrice.ToString("c") + "'>");
		Response.Write("<input type=hidden name=code value='" + m_code + "'>");
		Response.Write("<input type=hidden name=desc value='" + dr["name"].ToString() + "'>");

		Response.Write("<tr><td bgcolor=#EEEEEE>Code</td><td>" + m_code + "</td></tr>");
		Response.Write("<tr><td bgcolor=#EEEEEE>Description</td><td>" + dr["name"].ToString() + "</td></tr>");
		Response.Write("<tr><td bgcolor=#EEEEEE>Current Price</td><td>" + dPrice.ToString("c") + "</td></tr>");
		Response.Write("<tr><td bgcolor=#EEEEEE>Your Ideal Price &nbsp;&nbsp;</td><td><input type=text size=5 name=new_price></td></tr>");
	}
	else
	{
		Response.Write("<tr><td>Code : </td><td><input type=text name=code></td></tr>");
	}

	if(Request.QueryString["adi"] == "1")
	{
		Response.Write("<tr><td colspan=2><font color=red> * don't forget to tell us your ideal price and please be reasonable</font></td></tr>");
		Response.Write("</table><br></td></tr>");
		Response.Write("<tr><td><table>");
		Response.Write("<tr><td colspan=2><br><b>Optional Information</b></td></tr>");
		Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
		
		Response.Write("<tr><td bgcolor=#EEEEEE>Your Name</td><td><input type=text name=user_name");
		if(Session["name"] != null)
			Response.Write(" value='" + Session["name"].ToString() + "'");
		Response.Write("></td></tr>");

		Response.Write("<tr><td bgcolor=#EEEEEE>Email</td><td><input type=text name=email");
		if(Session["email"] != null)
			Response.Write(" value='" + Session["email"].ToString() + "'");
		Response.Write("></td></tr>");

		Response.Write("<tr><td bgcolor=#EEEEEE>Phone Number</td><td><input type=text name=phone></td></tr>");
//		Response.Write("<tr><td bgcolor=#EEEEEE>Your Ideal Price</td><td><input type=text name=new_price></td></tr>");
		Response.Write("<tr><td bgcolor=#EEEEEE valign=top>Comment</td><td><textarea name=comment cols=50 rows=5></textarea></td><tr>");
	}
	else
	{
		Response.Write("<tr><td colspan=2><a href=feedback.aspx?t=price&adi=1&code=" + m_code);
		Response.Write("><font color=blue> * Click here to leave your contact information in case we agree with your price</font></a></td></tr>");
	}
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='Feed Back'></td></tr>");
	Response.Write("</table></td></tr>");
	Response.Write("</table></form>");	
}

bool DoFeedBack()
{
	string code = Request.Form["code"];
	string desc = Request.Form["desc"];
	string current_price = Request.Form["current_price"];
	string new_price = Request.Form["new_price"];
	string name = Request.Form["user_name"];
	string comment = Request.Form["comment"];
	string sname = "";
	if(Session["name"] != null)
		sname = Session["name"].ToString();
	string email = Request.Form["email"];
	string semail = "";
	if(Session["email"] != null)
		semail = Session["email"].ToString();
	string phone = Request.Form["phone"];
	string rip = Session["rip"].ToString();
	string uri = "";
	if(Session["LastPage"] != null)
		uri = Session["LastPage"].ToString();

	MailMessage msgMail = new MailMessage();

	string subject = "Feed Back ";
	if(Session["site"] != null)
		subject += Session["site"].ToString();

	msgMail.To = m_sSalesEmail;
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Subject = subject;

	msgMail.Body = "<html><style type=\"text/css\">\r\n";
	msgMail.Body += "td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n";
	msgMail.Body += "body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n<head><h2>Customer FeedBack";
	msgMail.Body += "</h2></head>\r\n<body>\r\n";

	msgMail.Body += "<table><tr><td>\r\n";
	msgMail.Body += "Product</td><td>";
	if(Session["site"] != null)
		msgMail.Body += "<a href=http://" + Session["site"] + "/admin/liveedit.aspx?code=" + code;
	msgMail.Body += ">" + code + " " + desc + "</a></td></tr>\r\n";

	msgMail.Body += "<tr><td>Our Price</td><td><font color=red><b>" + current_price + "</b></font></td></tr>\r\n";
	msgMail.Body += "<tr><td>Wanted Price</td><td><font color=red><b>$" + new_price + "</b></font></td></tr>\r\n";
	msgMail.Body += "<tr><td>Customer Name&nbsp;&nbsp;&nbsp;</td><td>" + name + "</td></tr>\r\n";
	msgMail.Body += "<tr><td>Email</td><td><a href=mailto:" + email + ">" + email + "</a></td></tr>\r\n";
	msgMail.Body += "<tr><td>Phone</td><td>" + phone + "</td></tr>\r\n";
	msgMail.Body += "<tr><td>Comment:</td><td>" + comment + "</td></tr>\r\n";
	msgMail.Body += "<tr><td>&nbsp;</td></tr>\r\n";

	msgMail.Body += "<tr><td colspan=2><b>Reference</b></td></tr>\r\n";
	msgMail.Body += "<tr><td>Client IP</td><td>" + rip + "</td></tr>\r\n";
	msgMail.Body += "<tr><td>URI</td><td><a href=" + uri + ">" + uri + "</a></td></tr>\r\n";
	msgMail.Body += "<tr><td>Time</td><td>" + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + "</td></tr>\r\n";
	msgMail.Body += "<tr><td>Login Name</td><td>" + sname + "</td></tr>\r\n";
	msgMail.Body += "<tr><td>Login Email</td><td>" + semail + "</td></tr>\r\n";
	msgMail.Body += "</table></body></html>";

//	Response.Write(msgMail.Body);
	SmtpMail.Send(msgMail);

	Response.Write("<br><br><table width=75%><tr><td align=center><br>");
	Response.Write("<table align=center valign=center cellspacing=10 cellpadding=10 border=3 bordercolor=blue bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td><table align=center>");
	Response.Write("<tr><td><b>T</b>hanks for your feedback, </td></tr>");
	Response.Write("<tr><td>we will get you back ASAP if you left your contact information.</td></tr>");
	Response.Write("<tr><td align=right><br><button onclick=window.location=('default.aspx')><b>Continue Shopping</b></button></td></tr>");
	Response.Write("</table></td></tr></table></td></tr></table>");

	return true;
}
</script>

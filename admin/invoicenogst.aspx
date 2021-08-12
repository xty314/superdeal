<!-- #include file="config.cs" -->
<!-- #include file="..\invoicenogst.cs" -->

<script runat=server>
bool m_bOrder = false;
protected void Page_Load(Object Src, EventArgs E ) 
{
	if(Request.QueryString.Count > 0)
		m_inv_sNumber = Request.QueryString[0];

	TS_PageLoad(); //do common things, LogVisit etc...
	if(!TS_UserLoggedIn() || Session["email"] == null)
	{
		Response.Write("<h3>Error, please <a href=login.aspx>login</a> first</h3>");
		return;
	}

	string s = "";
	if(Request.QueryString["t"] == "order" && Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		s = BuildOrderMail(Request.QueryString["id"]);
		//Response.Write(s);
		if(Request.QueryString["email"] != null && Request.QueryString["email"] != "")
		{
			string email = Request.QueryString["email"];
			if(Request.QueryString["confirm"] == "1")
			{
				Response.Write("<script Language=javascript");
				Response.Write(">");
				Response.Write("if(window.confirm('");
				Response.Write("Email order to " + email + "?         ");
				Response.Write("\\r\\n\\r\\n");
				Response.Write("\\r\\nClick OK to send.\\r\\n");
				Response.Write("'))");
				Response.Write("window.location='invoice.aspx?t=order&id=" + Request.QueryString["id"] + "&email=" + HttpUtility.UrlEncode(email) + "';\r\n");
				Response.Write("else window.close();\r\n");
				Response.Write("</script");
				Response.Write(">");
			}
			else
			{
				MailMessage msgMail = new MailMessage();
				msgMail.From = m_sSalesEmail;
				msgMail.To = email;
				msgMail.Subject = "Order " + Request.QueryString["id"];
				msgMail.BodyFormat = MailFormat.Html;
				msgMail.Body = s;
				SmtpMail.Send(msgMail);
			}
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h3>Order Sent.</h3>");
			Response.Write("<input type=button value='Close Window' onclick=window.close() " + Session["button_style"] + ">");
			Response.Write("<br><br><br><br><br><br>");
			PrintAdminFooter();
		}
		else
			Response.Write(s);

		return;
	}
	else
	{
		s = BuildInvoice(m_inv_sNumber);
		//continue followed code;
	}

	if(Request.QueryString["email"] != null && Request.QueryString["email"] != "")
	{
		string email = Request.QueryString["email"];
		if(Request.QueryString["confirm"] == "1")
		{
			Response.Write("<script Language=javascript");
			Response.Write(">");
			Response.Write("if(window.confirm('");
			Response.Write("Email invoice to " + email + "?         ");
			Response.Write("\\r\\n\\r\\n");
			Response.Write("\\r\\nClick OK to send.\\r\\n");
			Response.Write("'))");
			Response.Write("window.location='invoice.aspx?n=" + m_inv_sNumber + "&email=" + HttpUtility.UrlEncode(email) + "';\r\n");
			Response.Write("else window.close();\r\n");
			Response.Write("</script");
			Response.Write(">");
		}
		else
		{
			MailMessage msgMail = new MailMessage();
			msgMail.From = m_sSalesEmail;
			msgMail.To = email;
			msgMail.Subject = "Invoice " + m_inv_sNumber;
			msgMail.BodyFormat = MailFormat.Html;
			msgMail.Body = s;
			SmtpMail.Send(msgMail);
		}
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Invoice Sent.</h3>");
		Response.Write("<input type=button value='Close Window' onclick=window.close() " + Session["button_style"] + ">");
		Response.Write("<br><br><br><br><br><br>");
		PrintAdminFooter();
	}
	else
		Response.Write(s);

}
</script>

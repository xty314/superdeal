<script runat=server>

string m_id = ""; //transaction ID
string m_apEmail = "";
string m_cmd = "";
DataSet dst = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];

	if(m_id == "")
	{
		Response.Write("<br><center><h3>Error, no transcation ID</h3>");
		return;
	}
	
	string sbody = PrintRemittance();

	if(Request.Form["cmd"] != null)
		m_cmd = Request.Form["cmd"];
	if(m_cmd == "Print")
	{
		Response.Write("<html>");
		Response.Write("<body onload='window.print()'>");
		Response.Write(sbody);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=remit.aspx?id=" + m_id + "\">");
		return;
	}
	else if(m_cmd == "Email")
	{
		MailMessage msgMail = new MailMessage();
		msgMail.To = Request.Form["email"];
		msgMail.Bcc = GetSiteSettings("manager_email", "alert@eznz.com");
		msgMail.From = Session["email"].ToString();
		msgMail.Subject = "Remittance Advice -- " + m_sCompanyName;
		msgMail.BodyFormat = MailFormat.Html;
		msgMail.Body = sbody;

		SmtpMail.Send(msgMail);

		msgMail.To = Session["email"].ToString(); //backup copy
		SmtpMail.Send(msgMail);
		
		PrintAdminHeader();
		Response.Write("<br><br><br><center><h4>Email has sent to " + Request.Form["email"] + ", please wait a second.</h4>");
		Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=remit.aspx?id=" + m_id + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<form name=f action=remit.aspx?id=" + m_id + " method=post>");
	Response.Write("<center><table width=75%><tr><td>");
	Response.Write(sbody);
	Response.Write("</td></tr>");
	Response.Write("<tr><td align=right>");
	//link to payment trace...
	Response.Write("<input type=button name=cmd value='Payment Trace' onclick=\"window.location=('payhistory.aspx');\"  "+ Session["button_style"] +" >");
	Response.Write("<input type=submit name=cmd value=Print "+ Session["button_style"] +" >");
	Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
	Response.Write("<input type=text name=email value='" + m_apEmail + "'>");
	Response.Write("<input type=submit name=cmd value=Email "+ Session["button_style"] +">");
	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	PrintAdminFooter();
}

string PrintRemittance()
{
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT d.id, c.id AS supplier_id, c.name, c.trading_name, c.company, c.postal1, c.postal2, c.postal3 ";
	sc += ", c.address1, c.address2, c.address3, c.ap_email, c.email, p.date_invoiced, p.inv_number AS supplier_invoice ";
	sc += ", ISNULL(i.amount_applied, 0) AS amount_applied, i.purchase, d.trans_date, d.note ";
	sc += ", i.invoice_number, d.payment_method, d.payment_ref, t.amount, c1.name AS accountant ";
	sc += ", d.bank, d.branch, d.paid_by, d.invoice_number, ISNULL(p.total_amount, 0) AS purchase_total ";
	sc += ", p.po_number, c.phone, c.fax ";
	sc += " FROM tran_detail d LEFT OUTER JOIN tran_invoice i ON d.id=i.tran_id ";
	sc += " JOIN trans t ON t.id = d.id ";
//	sc += " LEFT OUTER JOIN invoice inv ON inv.invoice_number=i.invoice_number ";
	sc += " JOIN purchase p ON p.id=i.invoice_number ";
	sc += " LEFT OUTER JOIN card c ON c.id=d.card_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id=d.staff_id ";
	sc += " WHERE d.id=" + m_id;
	sc += " ORDER BY p.date_invoiced ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "custpaydetails") <= 0)
		{
			Response.Write("<br><center><h4>payment not found</h4>");
			return "";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	int nCols = 4;
	//title
//	sb.Append("<tr><td colspan=" + nCols + " align=center><font size=+1><b>Transaction Detail</b></font></td></tr>");

	sb.Append("<tr><td>&nbsp;</td></tr>");

	DataRow dr = dst.Tables["custpaydetails"].Rows[0];
	string payment_method = GetEnumValue("payment_method", dr["payment_method"].ToString()).ToUpper();
	string company_name = dr["trading_name"].ToString();
	m_apEmail = dr["ap_email"].ToString();
	if(m_apEmail == "")
		m_apEmail = dr["email"].ToString();
	string supplier_id = dr["supplier_id"].ToString();
	string postal1 = dr["postal1"].ToString();
	string postal2 = dr["postal2"].ToString();
	string postal3 = dr["postal3"].ToString();
	if(postal1 == "")
	{
		postal1 = dr["address1"].ToString();
		postal2 = dr["address2"].ToString();
		postal3 = dr["address3"].ToString();
	}
	string phone = dr["phone"].ToString();
	string fax = dr["fax"].ToString();
	string email = dr["email"].ToString();

	string payment_ref = dr["payment_ref"].ToString();
	if(payment_ref != "")
		payment_method += " ref #" + payment_ref;
	string date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy");

	sb.Append("<tr><td colspan=" + nCols + " align=center><hr></td></tr>");

	sb.Append("<tr><th width=100 nowrap>Invoice Date</th>");
	sb.Append("<th width=150 nowrap>Invoice Number</th>");
	sb.Append("<th align=right nowrap>Invoice Total </th>");
	sb.Append("<th width=120 align=right nowrap>Amount Paid</th>");
	sb.Append("</tr>");
	sb.Append("<tr><td colspan=" + nCols + " align=center><hr></td></tr>");
	int NumInv = 0;
	string sInvNo = "";
	string sInvoices = "";
	string sTotalPaid = "";
	string sNote = "";
	for(int i=0; i<dst.Tables["custpaydetails"].Rows.Count; i++)
	{
		dr = dst.Tables["custpaydetails"].Rows[i];
		string purchase = dr["purchase"].ToString();
		bool bPurchase = false;
		if(purchase != "")
			bPurchase = bool.Parse(purchase);

		string total = "0";
		sb.Append("<tr>");
		sb.Append("<td>" + DateTime.Parse(dr["date_invoiced"].ToString()).ToString("dd-MM-yyyy") + "</td>");
		sb.Append("<td align=center>");
		sb.Append(dr["supplier_invoice"].ToString() + "</td>");
		total = dr["purchase_total"].ToString();

		double dTotal = 0;
		if(total != "")
			dTotal = Math.Round(double.Parse(total), 2);
		double dPaid = Math.Round(double.Parse(dr["amount_applied"].ToString()), 2);
		sb.Append("<td align=right>" + dTotal.ToString("c") + "</td>");
		sb.Append("<td align=right>" + dPaid.ToString("c") + "</td>");
			
		sTotalPaid = dr["amount"].ToString();
	}
	if(sTotalPaid == null || sTotalPaid == "")
		sTotalPaid = "0";

	sb.Append("<tr><td colspan=" + nCols + " align=center><hr></td></tr>");
	sb.Append("<tr><td colspan=" + nCols + " align=right><b>Total Amount : ");
	sb.Append(MyDoubleParse(sTotalPaid).ToString("c")+ "</b></td></tr>");
	sb.Append("<tr><td>&nbsp;</td></tr>");
	sb.Append("</table>");

	string tp = ReadSitePage("remittance");
	tp = tp.Replace("@@date", date);
	tp = tp.Replace("@@supplier_id", supplier_id);
	tp = tp.Replace("@@payment_method", payment_method);
	tp = tp.Replace("@@payment_details", sb.ToString());
	tp = tp.Replace("@@company_name", company_name);
	tp = tp.Replace("@@consultant", "");
	tp = tp.Replace("@@postal1", postal1);
	tp = tp.Replace("@@postal2", postal2);
	tp = tp.Replace("@@postal3", postal3);
	tp = tp.Replace("@@phone", phone);
	tp = tp.Replace("@@fax", fax);
	tp = tp.Replace("@@email", email);
//	Response.Write(tp);
	return tp;
}

</script>
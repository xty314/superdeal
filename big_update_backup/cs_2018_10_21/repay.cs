<!-- #include file="cart.cs" -->

<script runat=server>

string err = "";
Boolean alterColor = false;

int invoiceNumber = -1;
string sCheckoutType = "";
string sPaymentType = "credit card";

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	string sc = "";
	CheckShoppingCart();
	CheckUserTable();	//get user details if logged on

	if(Request.QueryString["i"] == null)
	{
		Response.Write("<center><h3>Error Invoice Number</h3>");
		return;
	}

	PrintHeaderAndMenu();

	sc = "<br>";

	Session["OrderCreated"] = "true";
	Session["InvoiceNumber"] = Request.QueryString["i"];

	sc += PrintConfirmTable();

	sc += "<tr><td colspan=2 align=center>";
	if(sPaymentType == "credit card")
		sc += "<input type=submit value=&nbsp;&nbsp;Submit&nbsp;&nbsp;>";
	else
		sc += "<input type=submit value=&nbsp;&nbsp;Continue&nbsp;&nbsp;>";
	sc += "</td></tr><tr><td colspan=2 align=center>";
	if(sPaymentType == "credit card")
		sc += "<font color=red size=-2>please only press this button once, this will invoke secured credit card transaction.</font>";
	sc += "</td></tr></table></form>";

	Response.Write(sc);
	PrintFooter();
}

string PrintConfirmTable()
{
//	DataRow dr = dtUser.Rows[0];

	DataSet dsi = new DataSet();
	int rows = 0;
	string sc = "SELECT * FROM invoice WHERE invoice_number=" + Request.QueryString["i"];
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi, "invoice");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	if(rows != 1)
	{
		Response.Write("<h3>Error invoice record</h3>");
		return "";
	}
	
	DataRow dri = dsi.Tables["invoice"].Rows[0];

	DataRow dr = dtUser.Rows[0];
	
	//rebuild user data table
	dtUser.AcceptChanges();
	dr.BeginEdit();

	dr["Name"] = dri["name"];
	dr["Company"] = dri["company"];
	dr["Address1"] = dri["address1"];
	dr["Address2"] = dri["address2"];
	dr["City"] = dri["city"];
	dr["Country"] = dri["country"];
	dr["Phone"] = dri["phone"];
	dr["Email"] = dri["email"];
	
	dr["NameB"] = dri["nameB"];
	dr["CompanyB"] = dri["companyB"];
	dr["Address1B"] = dri["address1B"];
	dr["Address2B"] = dri["address2B"];
	dr["CityB"] = dri["cityB"];

	dr.EndEdit();

	dtUser.AcceptChanges();
	
	//the only place to set Session["Amount"]
	string sAmount = dri["total"].ToString();
	if(sAmount.IndexOf('.') < 0)
		sAmount += ".00";	//for dps reports "Invalid Amount Format" withou ".00"
	Session["Amount"] = sAmount;

	StringBuilder sb = new StringBuilder();

	sb.Append("<form action=");
	if(sPaymentType == "credit card")
		sb.Append("https://www.eznz.co.nz/" + m_sCompanyName + "/trans.asp?r=" + DateTime.Now.ToOADate() + "&i=" + Session["InvoiceNumber"].ToString());
//		sb.Append("trans.asp");
	else
		sb.Append("result.aspx");
	sb.Append(" method=post>");

	string url = "";
	string servername = Request.ServerVariables["SERVER_NAME"];
	string s = Request.ServerVariables["URL"];
//DEBUG("s=", s);
	int i = s.Length - 1;
	for(; i>=0; i--)
	{
		if(s[i] == '/')
			break;
	}
	
	s = s.Substring(0, i);
	url = "http://" + servername + s + "/result.aspx";
//DEBUG("url=", url);

	sb.Append("<input type=hidden name=result_url value='");
	sb.Append(url + "'>");
	sb.Append("<table width=100% align=center><tr><td>\r\n");
	sb.Append("<table align=center>\r\n");
	sb.Append("<tr><td colspan=2>&nbsp;</td></tr>\r\n");
	sb.Append("<tr><td><table><tr><td colspan=2><b>Shipping Address</b></td></tr>\r\n");

	sb.Append("<tr><td>Name</td><td>");
	sb.Append(dr["Name"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Company</td><td>");
	sb.Append(dr["Company"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Address</td><td>");
	sb.Append(dr["Address1"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>&nbsp;</td><td>");
	sb.Append(dr["Address2"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>City</td><td>");
	sb.Append(dr["City"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Phone</td><td>");
	sb.Append(dr["Phone"].ToString());
	sb.Append("</td></tr>\r\n<tr><td>Email</td><td>");
	sb.Append(dr["Email"].ToString());
	sb.Append("</td></tr>\r\n</table>\r\n");

	sb.Append("</td>\r\n\r\n<td valign=top>\r\n");

	//billing table
	sb.Append("<table>\r\n");
	sb.Append("<tr><td colspan=2><b>Billing Address</b></td></tr>\r\n<tr><td>Name</td><td>");

	if(dr["NameB"].ToString() != "")
		sb.Append(dr["NameB"].ToString());
	else
		sb.Append(dr["Name"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>Company</td><td>");
	if(dr["CompanyB"].ToString() != "")
		sb.Append(dr["CompanyB"].ToString());
	else
		sb.Append(dr["Company"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>Address</td><td>");
	if(dr["Address1B"].ToString() != "")
		sb.Append(dr["Address1B"].ToString());
	else
		sb.Append(dr["Address1"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>&nbsp;</td><td>");
	if(dr["Address2B"].ToString() != "")
		sb.Append(dr["Address2B"].ToString());
	else
		sb.Append(dr["Address2"].ToString());

	sb.Append("</td></tr>\r\n<tr><td>City</td><td>");
	if(dr["CityB"].ToString() != "")
		sb.Append(dr["CityB"].ToString());
	else
		sb.Append(dr["City"].ToString());

	sb.Append("</td></tr>\r\n</table>\r\n");
	//end of billing table

	sb.Append("</td></tr>\r\n\r\n<tr><td colspan=2 align=center>\r\n");
	
	//payment table
	sb.Append("<table>\r\n");
	sb.Append("<tr><td align=center><b>Payment Information:</b></td></tr>\r\n");

	sb.Append("<tr><td><b>Total Amount : " + Session["Amount"].ToString() + "</b></td></tr>\r\n");

	if(sPaymentType == "credit card")
	{
		sb.Append(visaTable);
	}
	sb.Append("</table>");
	//end of payment table

	sb.Append("<input type=hidden name=Amount value='");
	sb.Append(Session["Amount"].ToString());
	sb.Append("'>\r\n");
//DEBUG("amout=", Session["Amount"].ToString());
	sb.Append("<input type=hidden name=InvoiceNumber value='");
	sb.Append(Session["InvoiceNumber"]);
//DEBUG("invoiceNumber=", Session["InvoiceNumber"].ToString());
	sb.Append("'>\r\n");
	
	sb.Append("<input type=hidden name=PaymentType value='");
	sb.Append(sPaymentType);
	sb.Append("'>\r\n");

	return sb.ToString();
}

const string visaTable = @"

<TABLE BORDER=0 align=center>
<tr><td colspan=2 bgcolor=#EEEEEE><b>Credit Card</b></td></tr>
<tr><td>&nbsp;</td></tr>
  <TR>
    <TD><font color=red>Credit Card</font></TD>
    <TD><SELECT NAME=CardType>
 <OPTION VALUE=>Card Type
 <OPTION VALUE=Visa>Visa
 <OPTION VALUE=Mastercard>Mastercard
 <OPTION VALUE='American Express'>American Express
 </SELECT>
	</TD>
  </TR>
  <TR>
    <TD><font color=red>Cardholders Name</font><BR><FONT SIZE=1><I>(As appears on card)
    </I></FONT></TD>
    <TD><INPUT NAME=NameOnCard SIZE=20 MAXLENGTH=30 AUTOCOMPLETE=OFF></td>
  </TR>

  <TR>
    <TD><font color=red>Credit Card Number</font><BR></TD>
    <TD><INPUT NAME=CardNumber SIZE=20 MAXLENGTH=16 AUTOCOMPLETE=OFF></td>
  </TR>
  <TR>
    <TD><font color=red>Expiration Date</font></TD>
	<TD><font size=2>Month </font><INPUT NAME=ExpireMonth SIZE=2 MAXLENGTH=2 AUTOCOMPLETE=OFF>
	&nbsp;/&nbsp;<INPUT NAME=ExpireYear SIZE=2 MAXLENGTH=2 AUTOCOMPLETE=OFF><font size=2> Year</font></TD>
  </TR>
</TABLE>
";

</script>

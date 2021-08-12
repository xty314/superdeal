<script runat=server>

Boolean alterColor = false;

string m_sPaymentOption = "to be defined";

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	PrintHeaderAndMenu();
	
	m_sPaymentOption = ReadSitePage("payment_option_public");

	if(Session[m_sCompanyName + "_terms_agreed"] == null)
	{
		if(Request.QueryString["t"] == "at") //agreed
		{
			Session[m_sCompanyName + "_terms_agreed"] = true;
		}
		else
		{
			AskForTerms();
			PrintFooter();
			Response.End();
		}
	}

	string s = "<INPUT TYPE=submit NAME=CheckoutType VALUE='Continue With CreditCard >>'>";
	if(Session[m_sCompanyName + "no_credit_card"] != null)
		s = "<input type=button value=' Credit Card Not Applied ! '>";
	
	m_sPaymentOption = m_sPaymentOption.Replace("@@credit_card_button", s);

	CheckUserTable();		//get user details if logged on
	PrintBody();
	LPaymentOption.Text = m_sPaymentOption;
	LFooter.Text = m_sFooter;
}

void AskForTerms()
{
	Response.Write("<br><br><center><h4>Please read our <a href=sp.aspx?terms class=o target=_blank>Terms and Conditions of Sales</a> ");
	Response.Write("before check out</h4>");
	Response.Write("<br><br><input type=button onclick=window.location=('checkout.aspx?t=at&r=" + DateTime.Now.ToOADate() + "') value='I have read and agree'>");
}

void PrintBody()
{
	if(dtUser.Rows.Count <= 0)
	{
//DEBUG("error, user table empty", "");
		return;
	}

	string od = ""; //(old data), used to detect changes
	DataRow dr = dtUser.Rows[0];
	StringBuilder sb = new StringBuilder();

	sb.Append("<H3>Shipping Address</H3><TABLE BORDER=0>\r\n");
	sb.Append("<TR><TD><font color=red>Name</font></TD><TD><INPUT NAME=name VALUE='");
	sb.Append(dr["Name"].ToString());
	od += dr["name"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>Company</TD><TD><INPUT NAME=company VALUE='");
	sb.Append(dr["Company"].ToString());
	od += dr["Company"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD><font color=red>Address</font></TD><TD><INPUT NAME=address1 VALUE='");
	sb.Append(dr["Address1"].ToString());
	od += dr["Address1"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>&nbsp;</TD><TD><INPUT NAME=address2 VALUE='");
	sb.Append(dr["Address2"].ToString());
	od += dr["Address2"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD><font color=red>City</font></TD><TD><INPUT NAME=city VALUE='");
	sb.Append(dr["City"].ToString());
	od += dr["City"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD><font color=red>Country</font></TD><TD><INPUT NAME=country VALUE='");
	sb.Append(dr["Country"].ToString());
	od += dr["Country"];
	if(dr["Country"] == null || dr["Country"].ToString() == "")
	{
		sb.Append("New Zealand");
		od += "New Zealand";
	}
	sb.Append("' SIZE=30 MAXLENGTH=30 readonly=true></TD></TR>\r\n");
	sb.Append("<TR><TD><font color=red>Phone</font></TD><TD><INPUT NAME=phone VALUE='");
	sb.Append(dr["Phone"].ToString());
	od += dr["Phone"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");

	if(TS_UserLoggedIn())
	{
		sb.Append("<tr><td><font color=red>EMail</font></td><td>");
		sb.Append("<input type=hidden name=email value='" + dr["email"] + "'>");
		sb.Append("<input type=hidden name=email_confirm value='" + dr["email"] + "'>");
		sb.Append(dr["email"]);
		sb.Append("</td></tr>");
	}
	else
	{
		sb.Append("<TR><TD><font color=red>EMail</font></TD><TD><INPUT NAME=email VALUE='");
		sb.Append(dr["Email"]);
		sb.Append("' SIZE=30 MAXLENGTH=40></TD></TR>\r\n");
		sb.Append("<TR><TD><font color=red>Confirm</font></TD><TD><font size=2><I>(type email address again)</I></font></TD></TR>\r\n");
		sb.Append("<TR><TD>&nbsp;</TD><TD><INPUT NAME=email_confirm VALUE='");
		sb.Append(dr["Email"]);
		sb.Append("' SIZE=30 MAXLENGTH=40></TD></TR>");
	}
	sb.Append("</TABLE>\r\n");

	LFormS.Text = sb.ToString();

	//billing address table
	sb.Remove(0, sb.Length);
	sb.Append("<H3>Billing Address</H3><TABLE BORDER=0>\r\n");
	sb.Append("<TR><TD>Name</TD><TD><INPUT NAME=nameB VALUE='");
	sb.Append(dr["NameB"].ToString());
	od += dr["NameB"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>Company</TD><TD><INPUT NAME=companyB VALUE='");
	sb.Append(dr["CompanyB"].ToString());
	od += dr["CompanyB"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>Address</TD><TD><INPUT NAME=address1B VALUE='");
	sb.Append(dr["Address1B"].ToString());
	od += dr["Address1B"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>&nbsp;</TD><TD><INPUT NAME=address2B VALUE='");
	sb.Append(dr["Address2B"].ToString());
	od += dr["Address2B"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>City</TD><TD><INPUT NAME=cityB VALUE='");
	sb.Append(dr["CityB"].ToString());
	od += dr["CityB"];
	sb.Append("' SIZE=30 MAXLENGTH=30></TD></TR>\r\n");
	sb.Append("<TR><TD>Country</TD><TD><INPUT NAME=countryB VALUE='");
	sb.Append(dr["CountryB"].ToString());
	od += dr["CountryB"];
	if(dr["CountryB"] == null || dr["CountryB"].ToString() == "")
	{
		sb.Append("New Zealand");
		od += "New Zealand";
	}
	sb.Append("' SIZE=30 MAXLENGTH=30 readonly=true></TD></TR>\r\n");
	sb.Append("<TR rowspan=2><TD>&nbsp;</TD><TD><font size=2>leave billing address blank if same as shipping address</font></TD></TR>\r\n");
//	sb.Append("<TR><TD>&nbsp;</TD></TR>");
//	sb.Append("<TR><TD>&nbsp;</TD></TR>");
//	sb.Append("<TR><td colspan=2 align=right><a href=https://www.thawte.com/cgi/server/certdetails.exe?code=NZTOPS1 target=_blank>");
//	sb.Append("<img src=https://www.thawte.com/certs/server/stamp.gif></a>");
//	sb.Append("</td></tr></TABLE>\r\n");
	sb.Append("</table>");
	sb.Append("<input type=hidden name=old_data value='" + od + "'>");
	LFormB.Text = sb.ToString();
	if(!TS_UserLoggedIn())
	{
		string soption = @"
<tr><td>
<table cellspacing=3 cellpadding=3 border=1 bordercolor=#888888 bgcolor=white style=font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed>
<tr><td><table>
<tr><td><b>Optional</b></td><td><input type=checkbox name=remember> Remember My Deatils</td></tr>
<tr><td align=right><b>Tips : </b></td><td><I>Store your details to get <a href=discount.aspx class=o>incremental discount</a><I></td></tr>
<tr><td>&nbsp;</td><td><I>Or <a href=login.aspx class=o>Log in</a> to get your account information<I></td></tr>
<tr><td><b>password</b></td><td><input type=password name=pass></td></tr>
<tr><td><b>confirm</b><br><font size=1><i>(type again)</i></font></td>
<td><input type=password name=pass_confirm></td></tr>
<tr><td colspan=2><input type=checkbox name=ads>I wish to receive promotions by email</td></tr>
<tr><td colspan=2 align=right><input type=submit name=cmd value=Register></td></tr>
</table>
</td></tr></table>
</td></tr>

<tr><td colspan=3><font color=red size=2>* fields in red are mandatory, we don't do international shipping. <a href=sp.aspx?ship class=o>shipping info</a></font></td></tr>
		";
		LOptions.Text = soption;
	}
}
</script>

<br>
<!-- font color=#CCCCCC><b>Your Shopping Cart</b></font -->
<div align=center>

<FORM ACTION=confirm.aspx?<%Response.Write(DateTime.Now.ToOADate());%> METHOD="post">

<table width=90% border=0 align=center>

<tr valign=top>

<td>
<asp:Label id=LFormS runat=server/>
</td>

<td>&nbsp;&nbsp;</td>

<td>
<asp:Label id=LFormB runat=server/>
</td>

</tr>

<asp:Label id=LOptions runat=server/>

</table>

<br>	
<hr>

<asp:Label id=LPaymentOption runat=server/>

</FORM>

</div>

</body>
</html>
<asp:Label id=LFooter runat=server/>


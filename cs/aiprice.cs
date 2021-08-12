<script runat=server>

string m_type = "";
string m_code = "";
double m_dCost = 9999999;
double m_dBottomPrice = 0;
double m_dPrice = 0;
double m_dProfitCut = 0.5; //how much profit we want to keep, depends on calculation base on date
double m_dCurrentMargin = 0;
double m_dBargainMargin = 0.04;
string m_msg = "";

bool m_bDeal = false;
bool m_bBottomReached = false;
bool m_bAdmin = false;
bool m_bIncludeGST = true;
DataRow dr = null;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(Session["display_include_gst"].ToString() == "false")
		m_bIncludeGST = false;

	if(Request.QueryString["c"] != null)
		m_code = Request.QueryString["c"];
	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];

//DEBUG("code=", m_code);
	string err = "";
	if(m_code != "" && TSIsDigit(m_code))
	{
		GetProduct(m_code, ref dr);
		if(dr == null)
			err = "Error, product not found.";
	}
	else
		err = "Product Code Error, please follow a valid link";
	if(err != "")
	{
		PrintHeaderAndMenu();
		Response.Write("<br><br><center><h3>" + err + "<br><br><br><br><br>");
		PrintFooter();
		Response.End();
	}

	try
	{
		m_dBargainMargin = double.Parse(GetSiteSettings("max_bargain_percent_cut", "0.04"));
	}
	catch(Exception e)
	{
	}
	if(m_dBargainMargin > 0.08)
		m_dBargainMargin = 0.04; // protection

	m_dCost = double.Parse(dr["supplier_price"].ToString());
	double dRate = double.Parse(dr["rate"].ToString());
	m_dPrice = m_dCost * dRate;
//DEBUG("not null, cost=", m_dCost.ToString("c"));

//	m_dPrice = double.Parse(dr["price"].ToString());
	double lr1 = MyDoubleParse(dr["level_rate1"].ToString());
	m_dPrice *= lr1;
//DEBUG("rate=", ((m_dPrice - m_dCost)/m_dCost).ToString());
	m_dCost *= 1.03; // credit card charge and dps charge

	int dayOfMonth = (int)DateTime.Now.Day;
	if(dayOfMonth < 9)
		dayOfMonth += 7;
	int dayOfWeek = (int)DateTime.Now.DayOfWeek;
	int days = dayOfMonth * 2 + dayOfWeek;
	m_dProfitCut = (double)days / 100;

	int nSeed = (int)DateTime.Now.Minute;
	if(nSeed > 30)
		nSeed = 60 - nSeed;
	m_dProfitCut += (double)nSeed / 100;

	m_dCurrentMargin = Math.Round((m_dPrice - m_dCost) / m_dCost, 2);
	if(m_dCurrentMargin > 0.1)
		m_dProfitCut = m_dCurrentMargin / 2 * m_dProfitCut;
	else if(m_dCurrentMargin > m_dBargainMargin)
		m_dProfitCut *= m_dBargainMargin;
	else
		m_dProfitCut *= m_dCurrentMargin;


//	Random rnd = new Random();
//	double drnd = (double)rnd.Next(0, 100) / 100;
//	m_dProfitCut += drnd;
//	if(m_dProfitCut >= 1)
//		m_dProfitCut = drnd;

	m_dBottomPrice = m_dCost + m_dCost * Math.Abs(m_dCurrentMargin - m_dProfitCut); //keep a profit margin
/*
DEBUG("Cost=", m_dCost.ToString("c"));
DEBUG("RetailPirce=", m_dPrice.ToString("c"));
DEBUG("margin=", m_dProfitCut.ToString());
DEBUG("bottomPirce=", m_dBottomPrice.ToString("c"));
*/
	if(m_sSite == "admin")
		m_bAdmin = true;

	if(m_bAdmin)
	{
		PrintAdminHeader();
		PrintAdminMenu();
	}

	PrintHeaderAndMenu();

	if(Request.Form["price"] != null)
	{
		DoBargain();
	}
	else
	{
		PrintBody();
	}
	PrintFooter();

	if(m_bAdmin)
		PrintAdminFooter();
}

void PrintBody()
{
	if(m_dBottomPrice > m_dPrice)
	{
		Response.Write("<br><br><center><h3>Sorry, This item is on a special price lower than cost ");
		Response.Write("<input type=button onclick=history.go(-1) value='<< Back'><br><br><br><br><BR>");
		return;
	}

	if(Session["bargain_price_" + m_code] != null)
	{
		double dPrice =	(double)Session["bargain_price_" + m_code];
		Response.Write("<br><br><center>");
		Response.Write("<table><tr><td valign=center><h3>I thought we already had a deal, <font color=red>" + dPrice.ToString("c") + "</font></td><td>");
		Response.Write("&nbsp&nbsp&nbsp&nbsp;<a href=cart.aspx?t=b&c=" + m_code + "&r=" + DateTime.Now.ToOADate() + "><img src=/i/buy.gif border=0></a>");
		Response.Write("</h3></td></tr></table>");
		return;
	}

	double m_dPriceDisplay = m_dPrice;
	if(m_bIncludeGST)
	{
		double dGST = double.Parse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;
		m_dPriceDisplay *= (1 + dGST);
	}

	Response.Write("<form name=frm action=aiprice.aspx?c=" + m_code + " method=post>");
	Response.Write("<br><br>");
	Response.Write("<center><h3>The Retail Price is <font color=red>" + m_dPriceDisplay.ToString("c") + "</font>, how much do you want?</h3>");
	Response.Write("<br><input type=text name=price><input type=submit name=cmd value=Submit>");
	Response.Write("<script");
	Response.Write(">\r\ndocument.frm.price.focus();\r\n</script");
	Response.Write(">\r\n");

	Response.Write("</form>");	

	if(m_bAdmin)
	{
		Response.Write("<table>");
		Response.Write("<tr><td><b>BottomPirce : </b></td><td><font color=red>" + m_dBottomPrice.ToString("c")+ "</font></td></tr>");
		Response.Write("<tr><td><b>Cost : </b></td><td><font color=red>" + m_dCost.ToString("c") + "</font></td></tr>");
		Response.Write("<tr><td><b>Margin : </b></td><td><font color=red>" + m_dProfitCut.ToString() + "</font></td></tr>");
		Response.Write("</table>");
	}
}

bool DoBargain()
{
	string price = Request.Form["price"];
	double dPrice = 0;
	try
	{
		dPrice = double.Parse(price, NumberStyles.Currency, null);
	}
	catch(Exception e)
	{
		Response.Write("<br><br><center><h3>Invalid Price Format</h3>");
		Response.End();
	}
//	double dPriceDisplay = dPrice;
	if(m_bIncludeGST)
	{
		double dGST = double.Parse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;
		dPrice /= (1 + dGST);
	}
//DEBUG("his price=", dPrice.ToString("c"));
	//bargain log
	string sc = "INSERT INTO aiprice (code, cost, price, margin, bottom_price, bargain_price, card_id, ip) ";
	sc += " VALUES(" + m_code + ", " + m_dCost + ", " + m_dPrice + ", " + m_dProfitCut + ", " + m_dBottomPrice;
	sc += ", " + dPrice + ", ";
	if(TS_UserLoggedIn())
		sc += Session["card_id"].ToString();
	else
		sc += "null";
	sc += ", '" + Session["rip"].ToString();
	sc += "')";
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

	//send mail
	if(Session["bargain_mail_sent_" + m_code] == null)
	{
		Session["bargain_mail_sent_" + m_code] = true;
		string uri = "http://" + Request.ServerVariables["SERVER_NAME"] + "/p.aspx?" + m_code;
		string msg = uri + "<br><br>\r\n\r\n";
		msg += "<a href=" + uri + ">" + m_code + "</a><br>\r\n";
		msg += "cost : " + m_dCost.ToString("c") + "<br>\r\n";
		msg += "price : " + m_dPrice.ToString("c") + "<br>\r\n";
		msg += "margin : " + m_dProfitCut.ToString("c") + "<br>\r\n";
		msg += "bottom : " + m_dBottomPrice.ToString("c") + "<br><br>\r\n\r\n";
		msg += "bargain price : " + dPrice.ToString("c") + "<br>\r\n";
		if(TS_UserLoggedIn())
			msg += "name : " + Session["name"].ToString() + "<br>\r\n";
		msg += "client ip : " + Session["rip"].ToString() + "<br>\r\n";
		AlertAdmin("Oh fight - " + m_sCompanyTitle, msg);
	}
	
	Response.Write("<br><br><center>");

	if(dPrice > m_dBottomPrice)
	{
		Session["bargain_price_" + m_code] = dPrice;
		Response.Write("<table><tr><td valign=center><h1>OK, <font color=red>" + dPrice.ToString("c") + "</font></h1></td><td>");
//		Response.Write("&nbsp&nbsp&nbsp&nbsp;<a href=cart.aspx?t=b&c=" + m_code + "&r=" + DateTime.Now.ToOADate() + "><img src=/i/buy.gif border=0></a>");
		Response.Write("</td></tr></table>");
		Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=cart.aspx?t=b&c=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\">");
	}
	else
	{
		Response.Write("<br><br><center><h3>Sorry, I can not take this price</h3>");
		Response.Write("<br><h4>Would you like to talk to our sales? ");
		Response.Write("<input type=button onclick=window.location=('feedback.aspx?t=price&code=" + m_code + "') value=' YES '>");
		Response.Write("<input type=button onclick=window.location=('aiprice.aspx?c=" + m_code + "&r=" + DateTime.Now.ToOADate() + "') value=' NO '>");
	}

	Response.Write("<br><br><br><br><br><br><br><br></center>");
	return true;
}
</script>

<!-- #include file="q_functions.cs" -->

<script runat="server">

double m_dBottomPrice = 0;
double m_dCurrentPrice = 0;
double m_dExpectPrice = 0; //the price we expect to deal
double m_dProfitMargin = 0.5; //how much profit we want to keep, depends on calculation base on date
string m_msg = "";
string m_code = "";
string m_type = "";

bool m_bDeal = false;
bool m_bBottomReached = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

//	if(Session["bargain_profit_margin"] == null)
//	{
		int dayOfMonth = (int)DateTime.Now.Day;
		if(dayOfMonth < 9)
			dayOfMonth += 7;
		int dayOfWeek = (int)DateTime.Now.DayOfWeek;
		int days = dayOfMonth * 2 + dayOfWeek;
		m_dProfitMargin = (double)days / 100;
//		Session["bargain_profit_margin"] = dMargin;
//	}
	if(Session["Amount"] != null)
	{
		if(double.Parse(Session["Amount"].ToString()) > 0)
		{
			m_dBottomPrice = double.Parse(Session["Cost"].ToString());
			m_dCurrentPrice = double.Parse(Session["Amount"].ToString());
		}
	}
//DEBUG("margin=", m_dProfitMargin.ToString());
//DEBUG("bottom=", m_dBottomPrice.ToString());
	
	if(Request.QueryString["t"] == "b")
	{
		m_type = "b";
		if(!DoBargain())
			return;
		if(m_bDeal)
			return;
	}
	else
	{
		if(m_dCurrentPrice - m_dBottomPrice > 50)
			m_dExpectPrice = m_dCurrentPrice - 10;
		else if(m_dCurrentPrice - m_dBottomPrice > 20)
			m_dExpectPrice = m_dCurrentPrice - 5;
		else
			m_dExpectPrice = m_dCurrentPrice - 2;
		Session["bargain_expect_price"] = m_dExpectPrice;
//DEBUG("expec=", m_dExpectPrice.ToString("c"));
	}
//	PrintHeaderAndMenu();
	PrintBargainForm();
//	PrintFooter();
}
/*
bool GetBottomPrice()
{
	if(m_code == "s") //bargain system quotation
	{
		if(!GetSystemBottomPrice())
			return false;
		if(!GetSystemCurrentPrice())
			return false;
	}
	else
	{
		double dPrice = 0;
		if(!GetSupplierPrice(m_code, ref dPrice))
			return false;
		m_dBottomPrice = dPrice * 1.125;
		if(!GetItemPrice(m_code, ref dPrice))
			return false;
		m_dCurrentPrice = dPrice * 1.125;
	}
	Session["bargain_bottom_price"] = m_dBottomPrice;
	Session["bargain_current_price"] = m_dCurrentPrice;
	Session["bargain_bargain_price"] = m_dCurrentPrice;
	return true;
}

bool GetSystemBottomPrice()
{
	CheckQTable();
	double dTotal = 0;
	StringBuilder sq = new StringBuilder();
	for(int i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString();
		if(int.Parse(code) <= 0)
			continue;
		double dPrice = 0;
		if(!GetSupplierPrice(code, ref dPrice))
			return false;
		dTotal += dPrice;
	}
	
	m_dBottomPrice = dTotal * 1.125;
//	double dTAX = dTotal * 0.125;
//	double dAmount = dTotal * 1.125;
	return true;
}


bool GetSystemCurrentPrice()
{
	CheckQTable();
	double dTotal = 0;
	StringBuilder sq = new StringBuilder();
	for(int i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString();
		if(int.Parse(code) <= 0)
			continue;
		double dPrice = 0;
		if(!GetItemPrice(code, ref dPrice))
			return false;
		dTotal += dPrice;
	}
	
	m_dCurrentPrice = dTotal * 1.125;
//	double dTAX = dTotal * 0.125;
//	double dAmount = dTotal * 1.125;
	return true;
}
*/

void PrintBargainForm()
{
	Response.Write("<html><body><br><br><center>");
	if(m_dCurrentPrice == 0)
		Response.Write("<h3>Your shopping cart is empty, nothing to bargain.</h3>");
	else
	{
		Response.Write("<h3>Let's talk about Money</h3>");
		Response.Write("<form action=bargain.aspx?t=b&c=" + m_code + " method=post>");
		Response.Write("<table align=center>");
		Response.Write("<tr><td>Current Total Amount is : <b>" + m_dCurrentPrice.ToString("c") + "</b></td>");
		if(m_type != "b")
		{
			Response.Write("<td><input type=submit name=cmd value='Accept'></td></tr>");
			Response.Write("<tr><td colspan=2>Click Accept to continue check out if you are happy with this price</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td>Or ");
		}
		else
			Response.Write("</tr><tr><td>");
		if(!m_bBottomReached)
		{
			Response.Write("make a near offer : </td>");
			Response.Write("<td><input type=text name=offer></td><td><input type=submit name=cmd value=Bargain></td>");
		}
		else
			Response.Write("</td>");
		Response.Write("</tr><tr>");
	//	Response.Write("<td align=right>We say : </td>");
		Response.Write("<td><font color=green><b>" + m_msg + "</b></font></td>");
		Response.Write("</tr>");
	//	Response.Write("<tr><td colspan=2 align=right><input type=submit value=Bargain></td></tr>");
		Response.Write("</table></form>");
	}
	Response.Write("</body></html>");
}

bool DoBargain()
{
	if(Request.Form["cmd"] == "Accept")
	{
		Session["bargain_final_price"] = m_dExpectPrice;
		Response.Write("<center><h3>Thank You! Please continue to check out </h3> <a href=confirm.aspx>Continue Check Out</a>");
	}
	string newPrice = Request.Form["offer"].ToString();
	if(!TSIsDigit(newPrice))
	{
		m_msg = "invalid price";
	}
	else
	{
		m_dExpectPrice = (double)Session["bargain_expect_price"];
		double dPrice = double.Parse(newPrice);
//DEBUG("expec price=", m_dExpectPrice.ToString("c"));
		if(dPrice >= m_dExpectPrice)
		{
			m_msg = "<font size=+1><b>deal ! </b></font><a href=" + Session["lastpage"].ToString() + ">Click here to return</a>";
			Response.Write(m_msg);
			Session["bargain_final_price"] = dPrice;
			Session["bargain_expect_price"] = null; //reset
			m_bDeal = true;
		}
		else
		{
			if(m_dExpectPrice < m_dCurrentPrice - (m_dCurrentPrice - m_dBottomPrice) * m_dProfitMargin) //keep a profit margin
			{
				m_msg = "Sorry, this is my bottom price. " + m_dExpectPrice.ToString("c") + " </td><td><input type=submit name=cmd value='Accept'>";
				m_bBottomReached = true;
			}
			else
			{
				if(m_dExpectPrice - m_dBottomPrice > 50)
					m_dExpectPrice = m_dExpectPrice - 10;
				else if(m_dExpectPrice - m_dBottomPrice > 20)
					m_dExpectPrice = m_dExpectPrice - 5;
				else
					m_dExpectPrice = m_dExpectPrice - 2;
				m_msg = "Sorry, how about " + m_dExpectPrice.ToString("c") + "</td><td><input type=submit name=cmd value='Accept'>";
				Session["bargain_expect_price"] = m_dExpectPrice;
			}
/*			Random rnd = new Random();
			double drop = rnd.Next(0, 3);
			double dp = (double)Session["bargain_bargain_price"] - 5 * drop;
			if(drop == 3)
			{
				m_msg = "A little higer, please.";
			}
			else if(dp <= m_dBottomPrice + 50)
				m_msg = "Sorry, No .";
			else
			{
				m_msg = "How about " + dp.ToString("c") + "</td><td><input type=submit name=cmd value='Accept'>";
				Session["bargain_bargain_price"] = dp;
			}
*/
		}
	}
	return true;
}
</script>
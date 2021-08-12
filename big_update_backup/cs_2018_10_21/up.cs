<!-- #include file="bb_function.cs" -->
<script runat=server>

string m_sAdminFooter1 = "";
bool m_bAdminMenu = false;
string m_first_post_id = "";
string m_first_poster_name = "";
string m_warranty = "";
string m_brand = "";
string m_price = "";
string m_desc = "";
string m_details = "";
string m_firstPosterID = "";
string m_poster_ip = "";
string m_date = "";
string m_location = "";
int m_nAccessLevel = 1; //normal customer
int rows = 0;

int m_nPage = 1;
int m_nPageSize = 20;
int m_cols = 6;
int m_nStartPageButton = 1;
int m_nPageButtonCount = 9;

protected void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	InitializeData(); //init functions

	if(Request.QueryString["p"] != null)
		m_nPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_nStartPageButton = int.Parse(Request.QueryString["spb"]);

	if(Request.QueryString["c"] != null)
		m_code = Request.QueryString["c"];

	m_arg = "&code=" + m_code + "&r=" + DateTime.Now.ToOADate();

	RememberLastPage();
	PrintHeaderAndMenu();

	PrintBody();

	if(m_bAdminMenu)
		LFooter.Text = m_sAdminFooter1;
	else
		LFooter.Text = m_sFooter;
}

bool PrintBody()
{
	if(m_code == "")
		return false;

	string sc = "SELECT p.*, bp.id AS first_post_id, bp.card_id, bp.poster_ip, t.subject, ";
	sc += " t.id AS mtopic_id, t.first_post_time, uc.cat, ";
	sc += " card.name AS poster_name, card.city AS location, card.access_level, bp.text ";
	sc += " FROM used_product p ";
	sc += " JOIN bb_topic t ON t.id=p.topic_id ";
	sc += " JOIN bb_post bp ON bp.topic_id=t.id ";
	sc += " JOIN card ON card.id=t.first_poster_id ";
	sc += " JOIN used_catalog uc ON uc.id=p.cat_id ";
	sc += " WHERE p.id=" + m_code + " ORDER BY bp.post_time ";
//DEBUG("sc=", sc);
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "up");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = ds.Tables["up"].Rows[0];
	m_topicid = dr["mtopic_id"].ToString();
	m_price = double.Parse(dr["price"].ToString()).ToString("c");
	m_first_post_id = dr["first_post_id"].ToString();
	m_first_poster_name = dr["poster_name"].ToString();
	m_warranty = dr["warranty"].ToString();
	m_cat = dr["cat"].ToString();
	m_brand = dr["brand"].ToString();
	m_desc = dr["name"].ToString();
	m_details = dr["text"].ToString();
	m_firstPosterID = dr["card_id"].ToString();
	m_poster_ip = dr["poster_ip"].ToString();
	m_location = dr["location"].ToString();
	m_date = DateTime.Parse(dr["first_post_time"].ToString()).ToString("dd-MM-yyyy");
	m_nAccessLevel = int.Parse(dr["access_level"].ToString());

	BindGrid();
	Response.Write("<br><table width=70%><tr><td>");
	PrintAddNewForm();
	Response.Write("</td></tr></table>");

	return true;
}

void PrintItemHeaderTable()
{
	Response.Write("<br><center><font color=red size=+1><b>" + m_desc + "</b></font><br>");
	Response.Write("<table width=90% align=center>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
		Response.Write("<tr><td>");
		Response.Write("<table>");
			Response.Write("<tr><td width=200 valign=top>");

			//print pic
			Response.Write("<table height=100% width=100%>");
				Response.Write("<tr><td>");
				if(!ShowAllExists(false, m_first_post_id))
					Response.Write("<img src=/i/naa.gif width=150>");
				Response.Write("</td></tr>");
			Response.Write("</table>");

			Response.Write("</td><td>&nbsp;&nbsp;&nbsp;</td><td valign=top>");

			//details
			Response.Write("<table><tr><td>");
			Response.Write("<table cellpadding=10 border=1 height=100 width=450><tr><td valign=top>");
			Response.Write(bbEnhance(m_details));
			Response.Write("</td></tr></table>");
			Response.Write("</td></tr>");
			
			//warranty
			Response.Write("<tr><td>");

			if(m_nAccessLevel > 1)
				m_warranty = "30 days";
			Response.Write("<br><table>");
			Response.Write("<tr><td align=right><b>Brand : </b></td><td>" + m_brand + "</td></tr>");
			Response.Write("<tr><td align=right><b>Price : </b></td><td>" + m_price + "</td></tr>");
			Response.Write("<tr><td align=right><b>Warranty : </b></td><td>" + m_warranty + "</td></tr>");
			Response.Write("<tr><td align=right><b>Location : </b></td><td>" + m_location + "</td></tr>");
			Response.Write("<tr><td align=right><b>Date Added : </b></td><td>" + m_date + "</td></tr>");
			Response.Write("<tr><td align=right><b>Supplier : </b></td><td>");
			if(m_nAccessLevel > 1)
				Response.Write(m_sCompanyTitle);
			else
				Response.Write(m_first_poster_name);
			Response.Write("</td></tr>");
			Response.Write("<tr><td align=right><b>Buy Online Appliable : </b></td><td valign=center>");
			if(m_nAccessLevel > 1) //eden staff
				Response.Write("<b>YES</b></td><tr>");
			Response.Write("</td></tr>");
			Response.Write("</table>");

			Response.Write("</td></tr>");

			Response.Write("<tr><td align=right>");
			Response.Write("<font size=+1 color=red><b>" + m_price + "</b></font>");
			Response.Write("</td></tr>");
			Response.Write("<tr><td align=right>");
			if(m_nAccessLevel > 1) //eden staff
				Response.Write("<a href=cart.aspx?t=b&c=" + m_code + "&used=1><img src=/i/buy.gif border=0></a>");
			Response.Write("</td></tr>");

			Response.Write("</table>");

			Response.Write("</td></tr>");
		Response.Write("</table>");

	
	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr></td></tr>");

	Response.Write("<tr><td align=right>");
	if(TS_UserLoggedIn())
	{
		string arg = "&ti=" + m_topicid + "&pi=" + m_first_post_id + "&code=" + m_code + " class=o";
		if(Session["card_id"].ToString() == m_firstPosterID)
		{
			Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=a" + arg + ">Attach Image</a>");
			Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=e" + arg + ">Edit Item</a>");
			Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=d" + arg + ">Del Item</a>");
		}
		if(SecurityCheck("sales", false))
		{
			if(m_firstPosterID != Session["card_id"].ToString())
			{
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=a" + arg + ">Attach Image</a>");
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=e" + arg + ">Edit Item</a>");
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=d" + arg + ">Del Item</a>");
			}
			Response.Write("&nbsp&nbsp;<b>ip : <a href=trace.aspx?" + m_poster_ip + " class=o>" + m_poster_ip + "</a>");
		}
	}
	Response.Write("</td></tr></table>");
}

void BindGrid()
{
	PrintItemHeaderTable();
	Response.Write("<br><table width=90%><tr><td><h3>COMMENTS</h3></td></tr></table>");
	PrintPosts(false);
}
/*
void PrintNewPostForm(bool bTopic)
{
	if(bTopic)
	{
		Response.Write("<br><center><h3>New Message</h3>");
		Response.Write("<table width=90% align=center>");
		Response.Write("<tr><td><a href=bb.aspx?fi=1 class=o><b>Message Board Index</b></a></td></tr>");
		Response.Write("</table>");
	}
	Response.Write("<table width=90% align=center ");
	if(bTopic)
		Response.Write(" border=1 cellspacing=1 cellpadding=3 ");
	else
		Response.Write(" border=0 cellspacing=1 cellpadding=0 ");
	Response.Write("bordercolorlight=#44444 bordercolordark=#AAAAAA bgcolor=#EEEEEE style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<form action=bb.aspx?ti=" + m_topicid + " method=post>");
	if(bTopic)
		Response.Write("<tr><td><b>Subject : </b></td><td><input type=text name=subject size=90 maxlength=70></td></tr>");
	else
	{
		Response.Write("<input type=hidden name=topic_id value=" + m_topicid + ">");
//		Response.Write("<input type=hidden name=subject value='" + m_topicSubject + "'>");
	}
	
	//name
//	Response.Write("<tr><td><b>Your Name : </b></td>");
//	Response.Write("<td><input type=text name=name size=50 value='");
//	if(TS_UserLoggedIn())
//		Response.Write(Session["name"].ToString());
//	Response.Write("'></td></tr>");

	if(bTopic)
		Response.Write("<tr><td valign=top><b>Text : </b></td><td><textarea name=text rows=7 cols=70></textarea></td></tr>");
	else
		Response.Write("<tr><td colspan=2><textarea name=text rows=7 cols=70></textarea></td></tr>");

	if(bTopic)
		Response.Write("<tr><td><b>Post by : </b></td><td><b>Name : </b>");
	else
		Response.Write("<tr><td colspan=2><b>Name : </b>");
	Response.Write("<input type=text name=name size=20 value='");
	if(TS_UserLoggedIn())
		Response.Write(Session["name"].ToString());
	Response.Write("'>&nbsp&nbsp&nbsp&nbsp&nbsp;<input type=checkbox name=notify>Notify me on reply by ");
	Response.Write("<b>Email : </b><input type=text name=email size=20 value='");
	if(TS_UserLoggedIn())
		Response.Write(Session["email"].ToString());
	Response.Write("'>");
//	Response.Write("<b> IP : " + Session["rip"].ToString() + "</b>");
	Response.Write("</td></tr>");

	if(SecurityCheck("sales", false))
	{
		Response.Write("<tr><td colspan=2 align=right><input type=checkbox name=notice>Notice ");
		Response.Write("<input type=checkbox name=stick>Stick</td></tr>");
	}
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Submit></td></tr>");
	Response.Write("</form></table>");

//	Response.Write("</td></tr></table>");
}
*/
string PrintPageIndex()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=" + m_cols + " align=right>Page: ");
	int pages = ds.Tables[0].Rows.Count / m_nPageSize + 1;
	int i=m_nStartPageButton;
	if(m_nStartPageButton > 10)
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&p=");
		sb.Append((i-10).ToString());
		sb.Append("&spb=");
		sb.Append((i-10).ToString());
		sb.Append(">...</a> ");
	}
	for(;i<=m_nStartPageButton + m_nPageButtonCount; i++)
	{
		if(i > pages)
			break;
		if(i != m_nPage)
		{
			sb.Append("<a href=");
			sb.Append(WriteURLWithoutPageNumber());
			sb.Append("&p=");
			sb.Append(i.ToString());
			sb.Append("&spb=" + m_nStartPageButton.ToString() + ">");
			sb.Append(i.ToString());
			sb.Append("</a> ");
		}
		else
		{
			sb.Append("<font size=+1><b>" + i.ToString() + "</b></font>");
			sb.Append(" ");
		}
	}
	if(i<pages)
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&p=");
		sb.Append(i.ToString());
		sb.Append("&spb=");
		sb.Append(i.ToString());
		sb.Append(">...</a> ");
		sb.Append("</td></tr>");
	}
	return sb.ToString();
}

string WriteURLWithoutPageNumber()
{
	string s = "?";
	return s;
}

</script>

<asp:Label id=LOldPic runat=server/>
<asp:Label id=LFooter runat=server/>
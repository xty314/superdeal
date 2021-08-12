<!-- #include file="bb_function.cs" -->
<script runat=server>

string m_sAdminFooter1 = "";
bool m_bAdminMenu = false;

int rows = 0;

int m_nPage = 1;
int m_nPageSize = 20;
int m_cols = 7;
int m_nStartPageButton = 1;
int m_nPageButtonCount = 9;

protected void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	InitializeData(); //init functions

	RememberLastPage();
	PrintHeaderAndMenu();

	if(Request.QueryString["p"] != null)
		m_nPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_nStartPageButton = int.Parse(Request.QueryString["spb"]);
	if(Request.QueryString["c"] != null)
		m_cat = Request.QueryString["c"];

	if(Request.QueryString.Count <= 0)
		PrintMenu();
	else
	{
		PrintUPList();
	}

	if(m_bAdminMenu)
		LFooter.Text = m_sAdminFooter1;
	else
		LFooter.Text = m_sFooter;
}

bool PrintMenu()
{
	Response.Write("<br><center><h3>USED ITEMS</h3><br>");
	Response.Write("<table align=center width=70%>");
	Response.Write("<tr><td><font size=+1><b>BUY : </b></font></td></tr>");
	Response.Write("</table>");

	string sc = "SELECT id, cat FROM used_catalog ORDER BY cat";
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<table align=center width=70%>");
	Response.Write("<tr><td valign=top>");
	Response.Write("<table>");
	int max = 10;
	int j = max;
	for(int i=0; i<rows; i++)
	{
		if(--j == 0)
		{
			Response.Write("</td></tr><tr><td valign=top>");
			j = max;
		}
		
		string cat = ds.Tables["cat"].Rows[i]["cat"].ToString();
		Response.Write("<tr><td><img src=r.gif> <a href=used.aspx?c=" + HttpUtility.UrlEncode(cat) + ">" + cat + "</td></tr>");
	}
	Response.Write("<tr><td><img src=r.gif> <a href=used.aspx?c=all>List All Categories</td></tr>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");

	Response.Write("<br>");
	Response.Write("<table align=center width=70%>");
	Response.Write("<tr><td><font size=+1><b>SELL : </b></font></td></tr>");
	Response.Write("</table>");

	Response.Write("<table align=center width=70%>");
	Response.Write("<tr><td>");

	Response.Write("Free to add your used itmes for sell(registered user only) ");
	Response.Write("<a href=login.aspx class=o>login</a> to see the submit('Add') button. ");

	Response.Write("</td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	Response.Write("<tr><td>");
	PrintAddNewForm();
	Response.Write("</td></tr>");

	Response.Write("</table>");
	return true;
}

bool PrintUPList()
{
	string sc = "SELECT c.cat, p.*, t.subject, t.first_post_time, t.last_post_time, t.replies, card.city FROM ";
	sc += " used_catalog c";
	sc += " JOIN used_product p ON p.cat_id=c.id ";
	sc += " JOIN bb_topic t ON t.id=p.topic_id ";
	sc += " JOIN card ON card.id=t.first_poster_id ";
	if(m_cat == "all")
	{
		sc += " WHERE t.deleted=0 ";
		sc += " ORDER BY t.last_post_time DESC ";
	}
	else
	{
		sc += " WHERE c.cat='" + m_cat + "' AND t.deleted=0 ";
		sc += " ORDER BY t.first_post_time DESC ";
	}
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

	BindGrid();
	Response.Write("<br><table width=70%><tr><td>");
	PrintAddNewForm();
	Response.Write("</td></tr></table>");

	return true;
}

void BindGrid()
{
	Response.Write("<br><center><h3>");
	if(m_cat == "all")
		Response.Write("All Itmes");
	else
		Response.Write(m_cat);
	Response.Write("</h3>");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1><tr><td>");

	Response.Write("<table cellspacing=0 cellpadding=3 rules=all bgcolor=white ");
	Response.Write("bordercolor=White border=0 width=100% style=\"font-family:Verdana;font-size:8pt;");
	Response.Write("border-collapse:collapse;\">");
	Response.Write("<tr style=\"text-align:left;font-weight:bold;color:White;background-color:#888888;\">");
	if(m_cat == "all")
	{
		Response.Write("<th>CATEGORY</th>");
		m_cols++;
	}
	Response.Write("<th>CODE</th>");
	Response.Write("<th>BRAND</th>");
	Response.Write("<th>DESCRIPTION</th>");
	Response.Write("<th>PRICE</th>");
	Response.Write("<th>LOCATION</th>");
	Response.Write("<th>DATE</th>");
	Response.Write("<th>POSTS</th>");
	Response.Write("</tr>");

	string cat = "";
	string cat_old = "";
	int rows = ds.Tables[0].Rows.Count;
	int start = (m_nPage - 1)* m_nPageSize;
	int i = start;
	bool bAlterColor = false;
	for(; i < rows; i++)
	{
		if(i >= start + m_nPageSize)
			break;

		DataRow dr = ds.Tables[0].Rows[i];
		string code = dr["id"].ToString();
		string name = dr["subject"].ToString();
		string brand = dr["brand"].ToString();
		string price = double.Parse(dr["price"].ToString()).ToString("c");
		string location = dr["city"].ToString();
		string date = DateTime.Parse(dr["first_post_time"].ToString()).ToString("dd-MM-yyyy");
		string replies = dr["replies"].ToString();
		
		string scat = "";
		if(m_cat == "all")
		{
			cat = dr["cat"].ToString();
			if(cat != cat_old)
			{
//				Response.Write("<tr><td colspan=6><font color=red><b>" + cat + "</b></font></td></tr>");
				cat_old = cat;
				scat = cat;
			}
			else
				scat = "";

		}

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");

		if(m_cat == "all")
			Response.Write("<td><a href=used.aspx?c=" + HttpUtility.UrlEncode(cat) + "><font color=red><b>" + scat + "</b></font></a></td>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + brand + "</td>");
		Response.Write("<td><a href=up.aspx?c=" + code + ">" + name + "</td>");
		Response.Write("<td>" + price + "</td>");
		Response.Write("<td>" + location + "</td>");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + replies + "</td>");
		Response.Write("</tr>");
	}
//	Response.Write("<tr><td colspan=" + m_cols + "><hr></td></tr>");
	Response.Write(PrintPageIndex());
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
}

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
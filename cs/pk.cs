<!-- #include file="kit_fun.cs" -->

<script runat=server>

string m_id = "";
string m_sAdminFooter1 = "";

bool m_bAdminMenu = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	InitKit();

	if(Request.QueryString.Count > 0)
	{
		m_id = Request.QueryString[0];
		if(Request.QueryString["ssid"] != null)
			m_ssid = Request.QueryString["ssid"];
	}
	if(m_id == "")
	{
		return;
	}
	try
	{
		if(MyIntParse(m_id).ToString() != m_id)
			return;
	}
	catch(Exception e)
	{
		return;
	}

	if(Request.QueryString["t"] == "b") //buy
	{
		if(DoAddKit(m_id, 1))
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?ssid=" + m_ssid + "\">");
			return;
		}
	}
	else if(Request.QueryString["t"] == "bi") //buy as individual item
	{
		if(DoAddKitAsItems(m_id, 1))
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=cart.aspx?ssid=" + m_ssid + "\">");
			return;
		}
	}

	InitializeData(); //init functions

	if(!GetKit(m_id))
		return;

	RememberLastPage();
	PrintHeaderAndMenu();
	PrintBody();
	PrintFooter();
}

void PrintBody()
{
	//administrator menu
	if(m_bAdminMenu)
	{
		Response.Write("<table width=100% bgcolor=#EEEEEE>");
		Response.Write("<tr><td><img src=r.gif> ");
		Response.Write("<a href=addpic.aspx?kit=1&code=");
		Response.Write(m_id);
		Response.Write("&name=");
		Response.Write(HttpUtility.UrlEncode(m_sKitName));
		Response.Write(" target=_blank>Upload Photo</a> ");

		Response.Write("<img src=r.gif> <a href=kit.aspx?id=");
		Response.Write(m_id);
		Response.Write(" target=_blank>Edit Details</a> ");

		Response.Write("</td></tr></table>\r\n");
	}
	//end of administrator menu

	Response.Write("<table width=80% align=center>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
		Response.Write("<tr><td>");
		Response.Write("<table>");
			Response.Write("<tr><td width=30% valign=top>");

			//print pic
			Response.Write("<table height=100% width=10%>");
				Response.Write("<tr><td>");
				string sPicFile = GetKitImgSrc(m_id); //no pic
				Response.Write("<table cellpadding=0 cellspacing=0><tr><td align=center>");
				Response.Write("<a href=");
				Response.Write(sPicFile);
				Response.Write("><img src=");
				Response.Write(sPicFile);
				string rp = Server.MapPath(sPicFile);
				int iWidth = 0;
				if(File.Exists(rp))
				{
					System.Drawing.Image im = System.Drawing.Image.FromFile(rp);
					iWidth = im.Width;
					if(im.Width > 200)
						Response.Write(" width=200 title='Click For Large Image'");
					im.Dispose();
				}
				else
					Response.Write(" width=150");
				
				Response.Write(" border=0></a></td></tr><tr><td align=center>");
				Response.Write("<a href=");
				Response.Write(sPicFile);
				if(sPicFile.Length > 2)
				{
					if(sPicFile.Substring(0, 2) == "/i")
						Response.Write("><font size=+1 color=red><b>We are sorry, Image temporarily unavailable</b></font>");
					else
					{
						Response.Write(">");
						if(iWidth > 200)
							Response.Write("<font size=1 color=blue>Click For Large Image</font>");
					}
				}
				Response.Write("</a></td></tr>");
				Response.Write("</table>");
				Response.Write("</td></tr>");
//				Response.Write("<tr valign=bottom><td height=200 valign=bottom>");
//				Response.Write("<a href=" + p_h_s + " class=d><img width=200 src=" + p_h_s + "><br>Price History</a>");
//				Response.Write("</td></tr>");
			Response.Write("</table>");

			//price
			Response.Write("</td><td>&nbsp;&nbsp;&nbsp;</td><td valign=top>");
			PrintPrice();
			Response.Write("</td></tr>");
		Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("<tr><td><hr></td></tr>");

	//spec
	Response.Write("<tr><td><b>" + m_sKitTerm + " contents : </b></td></tr>");
	Response.Write("<tr><td>");
	PrintItems();
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right>&nbsp;<br>");
	Response.Write("<font size=+1 color=red><b>&nbsp&nbsp;Any Question?&nbsp;&nbsp;</b></font>");
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('bb.aspx?t=nt&fi=1&q=1') value='&nbsp&nbsp; Ask Us &nbsp;'>");
	Response.Write("</td></tr>");


	Response.Write("<tr><td>&nbsp;</td></tr>");

	//warranty
	Response.Write("<tr><td>");
	string sWarranty = HashLink(m_sKitWarranty);
	if(sWarranty != "")
	{
		Response.Write("<b>Warranty:</b><br>");
		Response.Write(sWarranty);
	}
	else
	{
		Response.Write("<b>Warranty:</b><br>");
		Response.Write("<ul><li>12 months return to base</ul>");
	}
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td>");
	Response.Write(ReadSitePage("order_terms"));
	Response.Write("</td></tr>");

	Response.Write("</table>");
}

void PrintItems()
{
	string thumbExt = GetSiteSettings("thumbnail_image_file_extension", ".jpg_t.jpg");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
//	Response.Write("<th>&nbsp;</th>");
	Response.Write("<th>Item Code</th><th>Description</th><th>Qty</th></tr>");
	for(int i=0; i<dskit.Tables["kit_item"].Rows.Count; i++)
	{
		DataRow dr = dskit.Tables["kit_item"].Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
		string name = GetProductDesc(code);
		Response.Write("<tr>");
/*
		Response.Write("<td>");
		string fn = "/t/" + code + thumbExt;
		if(File.Exists(Server.MapPath(fn)))
			Response.Write("<img width=40 src=/t/" + code + thumbExt + ">");
		else
			Response.Write("<table border=1 width=40 height=40><tr><td>&nbsp;</td></tr></table>");
		Response.Write("</td>");
*/		
		Response.Write("<td>");
		Response.Write(code + "</td><td><a href=p.aspx?" + code + ">" + name + "</a></td><td>x " + qty + "</td></tr>");
	}
	Response.Write("</table>");
}

void PrintPrice()
{
	Response.Write("<table><tr><td colspan=2><font color=red><h3>");
	Response.Write(m_sKitName);
	Response.Write("</h3></font></td></tr>\r\n<tr><td>&nbsp;</td></tr>\r\n<tr><td colspan=2>");
	Response.Write(ListText(m_sKitDetails));
	Response.Write("</td></tr>");
	
	Response.Write("<tr rowspan=3>");

	Response.Write("<td colspan=2>&nbsp;</td></tr>");

	Response.Write("<tr><td align=right><b>" + m_sKitTerm + " ID:</b></td><td><b>" + m_id + "</b>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=right><b>Your Price:</b></td><td><b>");
	Response.Write(m_dKitPrice.ToString("c"));
	Response.Write("</b>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=button " + Session["button_style"]);
	Response.Write(" onclick=window.location=('pk.aspx?id=" + m_id + "&t=b&ssid=" + m_ssid + "') value='Add To Cart'>");
	Response.Write("<input type=button " + Session["button_style"]);
	Response.Write(" onclick=window.location=('pk.aspx?id=" + m_id + "&t=bi&ssid=" + m_ssid + "') value='Change Spec'>");
//	Response.Write("<font size=-2 color=red>(add to cart as individual items)</font>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");
	Response.Write("<font size=-2 color=red>Click 'Change Spec' to add to cart as individual items (no " + m_sKitTerm + " discount)</font>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
}

string HashLink(string s)
{
	int len = s.Length;

	StringBuilder sb = new StringBuilder();
	StringBuilder sl = new StringBuilder();
	
	Boolean bLink = false;
	bool bAddHttp = false;
	int i = 0;
	for(i=0; i<len; i++)
	{
		if(s[i] == 'h' || s[i] == 'H')
			if(i+6 < len)
				if(s[i+1] == 't' || s[i+1] == 'T')
					if(s[i+2] == 't' || s[i+2] == 'T')
						if(s[i+3] == 'p' || s[i+3] == 'P')
							if(s[i+4] == ':' && s[i+5] == '/' && s[i+6] == '/')
								bLink = true;
		
		if(!bLink)
		{
			if(s[i] == 'w' || s[i] == 'W')
				if(i+3 < len)
					if(s[i+1] == 'w' || s[i+1] == 'W')
						if(s[i+2] == 'w' || s[i+2] == 'W')
							if(s[i+3] == '.')
							{
								bLink = true;
								bAddHttp = true;
							}
		}

//		bLink = false;
		if(!bLink)
		{
			sb.Append(s[i]);
		}
		else
		{
			if(i+1 == len || s[i] == ' ')
			{
				if(i+1 == len) //the last character
					sl.Append(s[i]);

				sb.Append("<a href=");
				if(bAddHttp)
				{
					sb.Append("http://");
					bAddHttp = false;
				}
				sb.Append(sl.ToString());
				sb.Append(" class=o target=_blank>");
				sb.Append(sl.ToString());
				sb.Append("</a>");
				bLink = false;
				sl.Remove(0, sl.Length);
			}
			else
				sl.Append(s[i]);
		}
	}
	return sb.ToString();
}

string ListText(string s)
{
	CompareInfo ci = CompareInfo.GetCompareInfo(1);
	bool bHasTable = (ci.IndexOf(s, "<table", CompareOptions.IgnoreCase) >= 0);
	if(!bHasTable)
		bHasTable = (ci.IndexOf(s, "<td>", CompareOptions.IgnoreCase) >= 0);

	if(bHasTable)
		return s;

	string sRet = "<ul><li>";
	for(int i=0; i<s.Length; i++)
	{
		if(i<s.Length-4 && s[i] == '\r' && s[i+1] == '\n')
		{
			if(i>2 && (s[i-2] == '\r' && s[i-1] == '\n') || (s[i+2] == '\r' && s[i+3] == '\n'))
				sRet += "<br>";
			else
				sRet += "<li>";
		}
		else
			sRet += s[i];
	}
	sRet += "</ul>"; 

	return sRet; 
}

</script>


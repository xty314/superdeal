<!-- #include file="card_function.cs" -->
<script runat=server>

DataSet ds = new DataSet();

string m_arg = ""; //basic parameters
string m_code = ""; //if not blank then it's for used items
string m_cat = "";
string m_topicSubject = ""; //for email notify
string m_topicid = "";
string m_forumid = "";
bool m_bFromProductPage = false;
bool m_bIsEmployee = false;

string bbEnhance(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\n')
			ss += s[i] + "<br>";
		else
			ss += s[i];
	}
	string sl = ss.ToLower();
	string sr = "";
	string uri = "";
	for(int i=0; i<ss.Length; i++)
	{
		if(uri != "")
		{
			if(sl[i] == ' ' || sl[i] == ']' || sl[i] == '\r' || sl[i] == '\n' || i == ss.Length-1)
			{
				if(i == ss.Length-1)
					uri += ss[i];
				sr += "<a href=" + uri + " class=o>" + uri + "</a> ";
				uri = "";
			}
			else
				uri += ss[i];
			continue;
		}
		
		if(i<ss.Length-7)
		{
			if(sl[i] == 'h' && sl[i+1] == 't' && sl[i+2] == 't' && sl[i+3] == 'p' && sl[i+4] == ':' && sl[i+5] == '/' && sl[i+6] == '/')
			{
				uri += ss[i];
				continue;
			}
			else if(sl[i] == 'w' && sl[i+1] == 'w' && sl[i+2] == 'w' && sl[i+3] == '.')
			{
				uri += "http://" + ss[i];
				continue;
			}
			else
				sr += ss[i];
		}
		else
			sr += ss[i];
	}
	return sr;
}

string bbEncode(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += "\'\'"; //double it for SQL query
		else if(s[i] == '<')
			ss += '[';
		else if(s[i] == '>')
			ss += ']';
//		else if(s[i] == '\n')
//			ss += s[i] + "<br>";
		else
			ss += s[i];
	}
	return ss;
}

void DoSalesNotify(string poster, string text, string uri)
{
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = m_sSalesEmail; //"darcy@eznz.com"; 
	msgMail.From = GetSiteSettings("postmaster_email", "postmaster@eznz.com");
	msgMail.Subject = "New Message Notify - " + m_sCompanyName + " BBS";
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "Dear " + m_sCompanyTitle + ":\r\n\r\n";
	msgMail.Body += poster + " has post a new question/message :\r\n";
	msgMail.Body += "-------------------------------------------------------------------------------------------------\r\n";
	msgMail.Body += "Subject : " + m_topicSubject + "\r\n\r\n";
	msgMail.Body += text + "\r\n";
	msgMail.Body += "-------------------------------------------------------------------------------------------------\r\n";
	msgMail.Body += "http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + uri + "\r\n\r\n";
	msgMail.Body += "Have a good day.\r\n";
	msgMail.Body += "EZNZ Team\r\n";
	msgMail.Body += DateTime.Now.ToString("MMM.dd.yyyy");
	SmtpMail.Send(msgMail);
}

void DoNotifyEmail(string name, string replier, string text, string email, string uri)
{
	MailMessage msgMail = new MailMessage();
	
	msgMail.To = email;
	msgMail.From = m_sSalesEmail;
	msgMail.Subject = "Reply Notify - " + m_sCompanyTitle;
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = "Dear " + name + ":\r\n\r\n";
	msgMail.Body += replier + " has replied your question/message " + m_topicSubject + "\r\n\r\n";
	msgMail.Body += text + "\r\n\r\n";
	msgMail.Body += "There may be more replies followed, you can check it out at ";
	msgMail.Body += "http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + uri + "\r\n\r\n";
	msgMail.Body += "Cheers.\r\n\r\n";
	msgMail.Body += m_sCompanyTitle + "\r\n";
	msgMail.Body += DateTime.Now.ToString("MMM.dd.yyyy");
	SmtpMail.Send(msgMail);
}

bool ShowAllExists(bool bShowLink, string postid)
{
	string imgPath = GetRootPath();
	imgPath += "/bbi/" + postid;

//	string strPath = Server.MapPath(imgPath);
	string strPath = Server.MapPath(GetRootPath()) + "\\bbi\\" + postid;
	if(!Directory.Exists(strPath))
		return false;

	strPath += "\\";

	StringBuilder sb = new StringBuilder();
	sb.Append("<table border=0><tr>");
	DirectoryInfo di = new DirectoryInfo(strPath);
	int n = 0;
	foreach (FileInfo f in di.GetFiles("*.*")) 
	{
		string s = f.FullName;
		System.Drawing.Image im = System.Drawing.Image.FromFile(s);
		string file = s.Substring(strPath.Length, s.Length-strPath.Length);
//DEBUG("file=", file);
		string imgsrc = imgPath + "/" + file;
//		if(n == 0 || n == 3 || n == 6 || n == 9)
		sb.Append("</tr><tr>");
		sb.Append("<td valign=bottom><table><tr><td colspan=2><img src=" + imgsrc);
//		if(im.Width > 250)
//			Response.Write(" width=250");
		sb.Append(" border=0></td></tr>");
		if(bShowLink)
		{
			sb.Append("<tr><td>" + im.Width.ToString() + "x" + im.Height.ToString() + " " + (f.Length/1000).ToString() + "K ");
			if(f.Length > 20480)
				sb.Append(" <font color=red> * big file * </font>");
			sb.Append("</td>");
			sb.Append("<td align=right><a href=bb.aspx?t=da" + m_arg + "&file=" + HttpUtility.UrlEncode(file));
			sb.Append(" class=o>DELETE</a></td></tr>");
		}
		sb.Append("<tr><td>&nbsp;</td></tr></table></td>");
		im.Dispose();
		n++;
	}
	sb.Append("</tr></table>");
	if(bShowLink)
	{
		if(m_code != "")
			sb.Append("<a href=up.aspx?c=" + m_code + "&r=" + DateTime.Now.ToOADate() + " class=o>Back to View Item</a>");
		else
			sb.Append("<a href=bb.aspx?t=" + m_arg + " class=o>Back to View Post</a>");
		LOldPic.Text = "<b>" + n.ToString() + " Images Already Attached</b>";
		LOldPic.Text += sb.ToString();
	}
	else
		Response.Write(sb.ToString());
	if(n > 0)
		return true;
	return false; //no pic
}

string GetUCatID(string cat)
{
	string sc = "SELECT id FROM used_catalog WHERE cat='" + cat + "'";
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "catid");
		if(rows > 0)
			return ds.Tables["catid"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return "";
}

bool PrintExistsCatalogOptions(string sid)
{
	if(ds.Tables["cat"] == null)
	{
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
	}

	for(int i=0; i<rows; i++)
	{
		string id = ds.Tables["cat"].Rows[i]["id"].ToString();
		string cat = ds.Tables["cat"].Rows[i]["cat"].ToString();
		Response.Write("<option value=" + id);
		if(id == sid)
			Response.Write(" selected");
		Response.Write(">" + cat + "</option>");
	}
	return true;
}


bool PrintPosts(bool bShowImage)
{
	m_topicid = m_topicid.Replace("?=", "");
	string sc = "SELECT *, p.id AS post_id FROM bb_post p LEFT OUTER JOIN bb_topic t ON t.id=p.topic_id ";
	sc += " LEFT OUTER JOIN code_relations c ON t.code = c.code ";
	sc += " WHERE p.topic_id=" + m_topicid;
	if(Request.QueryString["c"] == null)
		sc += " AND t.deleted=0 AND p.deleted=0 ";
//DEBUG("sc=", sc);
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "posts");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		if(Request.QueryString["c"] != null)
			Response.Write("<br><h1><font color=red>Already Sold</font></h1>");
		else
			Response.Write("<br><br><center><h3>Topic Not Found</h3>");
		return true;
	}

	bool bSold = bool.Parse(ds.Tables["posts"].Rows[0]["deleted"].ToString());

	string description = ds.Tables["posts"].Rows[0]["name"].ToString();
	string code = ds.Tables["posts"].Rows[0]["code"].ToString();
	string subject = "";
	string nclass = "9";
//	if(rows > 0)
//	{
//		subject = ds.Tables["posts"].Rows[0]["subject"].ToString();
//		Response.Write("<br><table width=550 align=center cellspacing=0 cellpadding=0>");
//		Response.Write("<tr><td><font size=+1 color=red><b>" + subject + "</b></font></td></tr></table>");
//	}
	if(m_code == "")
	{
		Response.Write("<br><table width=90% align=center cellspacing=1 cellpadding=3 border=0 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=\"font-family:Verdana;font-size:8pt;fixed\">");
		Response.Write("<tr><td><a href=bb.aspx?fi=1 class=o><b>Message Board Index</b></a></td></tr>");
		Response.Write("<br>");
	}	
	
	Response.Write("<table width=90% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<tr><td colspan=2><font size=4 color=green><a title='More Details' href=\"javascript:product_window=window.open('p.aspx?"+ code +"', '', 'height=500, width=450, resizable=1'); product_window.focus();\" class=o>"+ description +"</a></td></tr>");
	int i = 0;
//	if(m_code != "")
//		i = 1;
	for(; i<rows; i++)
	{
		DataRow dr = ds.Tables["posts"].Rows[i];
		string card_id = dr["card_id"].ToString();
		string post_id = dr["post_id"].ToString();
		string poster_ip = dr["poster_ip"].ToString();
//		string img = dr["image"].ToString();
		string name = dr["poster_name"].ToString();
		string city = "";

		int access_level = 1;
		if(card_id != "")
		{
			DataRow drc = GetCardData(card_id);
			if(drc != null)
			{
				name = drc["name"].ToString();
				city = drc["city"].ToString();
				access_level = int.Parse(drc["access_level"].ToString());
			}
		}
		if(i == 0)
		{
			nclass = dr["class"].ToString();
			subject = ds.Tables["posts"].Rows[0]["subject"].ToString();
			m_topicSubject = subject;
			Response.Write("<tr><td bgcolor=#EEEEEE><b>Topic</b></td><td bgcolor=#EEEEEE><font color=red><b>" + subject + "</b></font></td></tr>");
		}

		string time = (DateTime.Parse(dr["post_time"].ToString()) ).ToString("dd-MM-yyyy HH:mm");
		string text = bbEnhance(dr["text"].ToString());
		Response.Write("<tr><td valign=top><b>");
		if(!bShowImage && access_level > 1)
			Response.Write(m_sCompanyName);
		else
			Response.Write(name);
		Response.Write("</b>");
		Response.Write("<br><br>" + city);
		if(access_level > 1)
			Response.Write("<br>" + m_sCompanyTitle);
		Response.Write("</td><td width=100%>" + text);
		
		//images
		if(bShowImage)
			ShowAllExists(false, post_id);

		Response.Write("</td></tr>");
		Response.Write("<tr><td nowrap bgcolor=#EEEEEE>" + time + "</td>");
		Response.Write("<td bgcolor=#EEEEEE align=right>");
		if(TS_UserLoggedIn())
		{
			string arg = "&fi=" + m_forumid + "&ti=" + m_topicid + "&pi=" + post_id + "&code=" + m_code;
			if(card_id == Session["card_id"].ToString())
			{
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=a" + arg + " class=o>Attach Image</a>");
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=e" + arg + " class=o>Edit Post</a>");
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=d" + arg + " class=o>Del Post</a>");
			}
			if(m_bIsEmployee)
			{
				if(card_id != Session["card_id"].ToString())
				{
					Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=a" + arg + " class=o>Attach Image</a>");
					Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=e" + arg + " class=o>Edit Post</a>");
					Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=d" + arg + " class=o>Del Post</a>");
				}
				Response.Write("&nbsp&nbsp;<b>ip : <a href=trace.aspx?" + poster_ip + " class=o>" + poster_ip + "</a>");
			}
		}
		else
			Response.Write("&nbsp;");
		Response.Write("</td></tr>");

//		Response.Write("<tr><td colspan=2>" + text);
//		Response.Write("</div></td></tr>");
	}
//	Response.Write("<tr><td colspan=2><hr></td></tr>");
//	Response.Write("<tr><td colspan=2 align=right><input type=button onclick=window.location=('bb.aspx?t=r&ti=" + m_topicid + "') value=Reply></td></tr>");
	if(nclass != "0" && !bSold)
	{
		Response.Write("<tr><td valign=top bgcolor=#EEEEEE><b>Quick Reply</b></td>");
		Response.Write("<td bgcolor=#EEEEEE><table><tr><td>");
		PrintNewPostForm(false);
		Response.Write("</td></tr></table>");
		Response.Write("</td></tr>");
	}
	Response.Write("</table>");

	if(m_code == "")
		Response.Write("</td></tr></table>");
	return true;
}

void PrintNewPostForm(bool bTopic)
{
	if(bTopic)
	{
		if(m_bFromProductPage)
			Response.Write("<br><center><h3>Question Form</h3>");
		else
			Response.Write("<br><center><h3>New Message</h3>");
		Response.Write("<table width=90% align=center>");
		Response.Write("<tr><td><a href=bb.aspx?fi=1 class=o><b>Message Board Index</b></a></td></tr>");
		Response.Write("<tr><td><b><font color=green size=4>"+ Request.QueryString["dp"] +"</td></tr>"); 
		
		Response.Write("</table>");
	}
	
	Response.Write("<table width=90% align=center ");
	if(bTopic)
		Response.Write(" border=1 cellspacing=1 cellpadding=3 ");
	else
		Response.Write(" border=0 cellspacing=1 cellpadding=0 ");
	Response.Write("bordercolorlight=#44444 bordercolordark=#AAAAAA bgcolor=#EEEEEE style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<form action=bb.aspx?t=" + m_arg + " method=post>");
	Response.Write("<input type=hidden name=hide_code value="+ Request.QueryString["cd"] +">");
	if(bTopic)
		Response.Write("<tr><td><b>Subject : </b></td><td><input type=text name=subject size=90 maxlength=70></td></tr>");
	else
	{
		Response.Write("<input type=hidden name=topic_id value=" + m_topicid + ">");
		Response.Write("<input type=hidden name=subject value='" + m_topicSubject + "'>");
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
	Response.Write("'>&nbsp&nbsp&nbsp&nbsp&nbsp;<input type=checkbox name=notify");
	if(TS_UserLoggedIn())
		Response.Write(" checked");
	Response.Write(">Notify me on reply by ");
	Response.Write("<b>Email : </b><input type=text name=email size=20 value='");
	if(TS_UserLoggedIn())
		Response.Write(Session["email"].ToString());
	Response.Write("'>");
//	Response.Write("<b> IP : " + Session["rip"].ToString() + "</b>");
	Response.Write("</td></tr>");

	if(m_bIsEmployee)
	{
		Response.Write("<tr><td colspan=2 align=right><input type=checkbox name=notice>Notice ");
		Response.Write("<input type=checkbox name=stick>Stick</td></tr>");
	}
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Submit></td></tr>");
	Response.Write("</form></table>");

//	Response.Write("</td></tr></table>");
}

void PrintAddNewForm()
{
	Response.Write("<center><font size=+1><b>Add Item</b></font><font size=-1><i>(registered user only, <a href=used.aspx class=o>Click here</a> to see details)</i></font>");
	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=3 border=1 border=#000000 style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<form action=bb.aspx?t=au method=post>");
	Response.Write("<tr><td><b>Catalog</b></td><td><select name=cat>");
	string cat_id = "";
	if(m_cat != "")
		cat_id = GetUCatID(m_cat);
	PrintExistsCatalogOptions(cat_id);
	Response.Write("</select> <b> &nbsp&nbsp&nbsp&nbsp;New Catalog</b><font size=-2><i> (if no suitable)<i></font><input type=text name=new_cat size=10></td></tr>");
	Response.Write("<tr><td><b>Brand</b></td><td><input type=text name=brand></td></tr>");
	Response.Write("<tr><td><b>Description</b></td><td><input type=text name=subject size=70 maxlength=120></td></tr>");
	Response.Write("<tr><td><b>Price</b></td><td><input type=text name=price></td></tr>");
	Response.Write("<tr><td valign=top><b>Details</b></td><td><textarea name=text cols=70 rows=7></textarea></td></tr>");
	if(TS_UserLoggedIn())
		Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=' Add '></td></tr>");
	Response.Write("</table>");
}

</script>

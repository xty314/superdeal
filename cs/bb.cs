<!-- #include file="bb_function.cs" -->
<!-- #include file="page_index.cs" -->
<script runat=server>

string m_type = "";
string m_postid = "";
string m_upid = "";

string m_sAdminFooter1 = "";
bool m_bAdminMenu = false;

int rows = 0;

protected void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	InitializeData(); //init functions

	RememberLastPage();
	PrintHeaderAndMenu();

	if(Session["card_type"] != null)
	{
		if(Session["card_type"].ToString() == "4")
			m_bIsEmployee = true;
	}

	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];
	if(Request.QueryString["fi"] != null)
		m_forumid = Request.QueryString["fi"];
	if(Request.QueryString["ti"] != null)
		m_topicid = Request.QueryString["ti"];
	if(Request.QueryString["pi"] != null)
		m_postid = Request.QueryString["pi"];
	if(Request.QueryString["q"] == "1")
		m_bFromProductPage = true;
	if(Request.QueryString["code"] != null)
		m_code = Request.QueryString["code"];

	m_arg = "&fi=" + m_forumid + "&ti=" + m_topicid + "&pi=" + m_postid + "&code=" + m_code + "&r=" + DateTime.Now.ToOADate();

	if(Request.Form["cmd"] == "Submit")
	{
		if(DoSubmit())
		{
			string uri = "bb.aspx?t=" + m_arg;
			if(m_code != "")
				uri = "up.aspx?c=" + m_code + "&r=" + DateTime.Now.ToOADate();
			Response.Write("<br><br><br><center><table align=center width=400 border=1><tr><td>");
			Response.Write("<table width=100%>");
			Response.Write("<tr><td align=center><br><h3>Post Saved</h3></td></tr>");
			Response.Write("<tr><td align=center><br>Wait a moment or <a href=" + uri + " class=o>click here</a> to view topic</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("</table></td></tr></table>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=" + uri + "\">");
		}
		return;
	}
	if(Request.Form["cmd"] == "Save")
	{
		if(DoSave())
		{
			string uri = "bb.aspx?t=" + m_arg;
			if(m_code != "")
				uri = "up.aspx?c=" + m_code + "&r=" + DateTime.Now.ToOADate();
			Response.Write("<br><br><br><center><table align=center width=400 border=1><tr><td>");
			Response.Write("<table width=100%>");
			Response.Write("<tr><td align=center><br><h3>Post Saved</h3></td></tr>");
			Response.Write("<tr><td align=center><br>Wait a moment or <a href=" + uri + " class=o>click here</a> to view topic</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("</table></td></tr></table>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=" + uri + "\">");
		}
		return;
	}
	else if(m_type == "a")
	{
		ShowAllExists(true, m_postid); //show exists photo
		Form1.Visible = true;
	}
	else if(m_type == "da")
	{
		DoDelPic();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=bb.aspx?t=a" + m_arg + "\">");
		return;
	}
	else if(m_type == "d")
	{
		DoDelPost();
		if(m_code != "")
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=up.aspx?c=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\">");
		else
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=bb.aspx?t=" + m_arg + "\">");
		return;
	}
	else if(m_type == "e")
		PrintEditPostForm();
	else if(m_type == "nt") //new topic
		PrintNewPostForm(true);
	else if(m_type == "au")
	{
		if(DoAddUsedItem())
		{
			string uri = "up.aspx?c=" + m_upid + "&r=" + DateTime.Now.ToOADate();
			Response.Write("<br><br><br><center><table align=center width=400 border=1><tr><td>");
			Response.Write("<table width=100%>");
			Response.Write("<tr><td align=center><br><h3>Item Saved</h3></td></tr>");
			Response.Write("<tr><td align=center><br>Wait a moment or <a href=" + uri + " class=o>click here</a> to view</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("</table></td></tr></table>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=" + uri + "\">");
		}
	}
	else if(m_topicid != "")
		PrintPosts(true);
	else if(m_forumid != "")
		PrintTopics();
	else
		PrintForums();

	if(m_bAdminMenu)
		LFooter.Text = m_sAdminFooter1;
	else
		LFooter.Text = m_sFooter;
}

bool PrintForums()
{
	m_forumid = "1";
	PrintTopics();
	return true;
}

bool PrintTopics()
{
	string sc = "SELECT * FROM bb_topic WHERE forum_id=" + m_forumid + " AND deleted=0 ";
	sc += " ORDER BY class, last_post_time DESC, id";
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "topics");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int irows = 0;
	if(ds.Tables["topics"] != null)
		irows = ds.Tables["topics"].Rows.Count;

	m_cPI.TotalRows = irows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<br><center><h3>Message Board</h3></center>");
	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=1 border=0 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<tr><td colspan=5 align=right><input type=button ");
	Response.Write(" onclick=window.location=('bb.aspx?t=nt" + m_arg + "') value='New Message'></td></tr>");

	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=1 border=1 bgcolor=#FFFFFF bordercolorlight=#888888 bordercolor=#FFFFFF style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<tr bgcolor=#EEEEEE><td width=60% align=center><b>SUBJECT</b></td><td align=center><b>NAME</b></td>");
	Response.Write("<td align=center><b>REPLIES</b></td><td align=center><b>LAST REPLY</b></td></tr>");
	for(; i<irows && i<end; i++)
	{
		DataRow dr = ds.Tables["topics"].Rows[i];
		string topic_id = dr["id"].ToString();
		string nclass = dr["class"].ToString();
		string subject = dr["subject"].ToString();
		string first_poster_id = dr["first_poster_id"].ToString();
		string last_poster_id = dr["last_poster_id"].ToString();
		string first_post_time = (DateTime.Parse(dr["first_post_time"].ToString()) ).ToString("dd-MM-yyyy HH:mm");
		string last_post_time = (DateTime.Parse(dr["last_post_time"].ToString()) ).ToString("dd-MM-yyyy HH:mm");
		string replies = dr["replies"].ToString();
		string first_poster = dr["first_poster_name"].ToString();
		string last_poster = dr["last_poster_name"].ToString();

		if(first_poster_id != "" && first_poster_id != "0")
		{
			DataRow drc = GetCardData(first_poster_id);
			if(drc != null)
				first_poster = drc["name"].ToString();
		}
		if(last_poster_id != "" && last_poster_id != "0")
		{
			DataRow drc = GetCardData(last_poster_id);
			if(drc != null)
				last_poster = drc["name"].ToString();
		}

		Response.Write("<tr><td>");
		Response.Write("<font color=red><b>");
		if(nclass == "0")
			Response.Write("&#169 Notice:");
		else if(nclass == "1")
			Response.Write("&#167");
		else
			Response.Write("&#164");
		Response.Write(" </b></font>");
		Response.Write("<a href=bb.aspx?ti=" + topic_id + ">" + subject + "</a></td>");
		Response.Write("<td align=center>" + first_post_time + "<br>by " + first_poster + "</td>");
		Response.Write("<td align=center>" + replies + "</td>");
		if(replies != "0")
			Response.Write("<td align=center>" + last_post_time + "<br>by " + last_poster + "</td>");
		else
			Response.Write("<td>&nbsp;</td>");
		Response.Write("</tr>");

/*		if(TS_UserLoggedIn())
		{
			if(card_id == Session["card_id"].ToString())
			{
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=a&ti=" + m_topicid + "&pi=" + post_id + " class=o>Attach Image</a>");
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=e&ti=" + m_topicid + "&pi=" + post_id + " class=o>Edit Post</a>");
			}
			if(m_bIsEmployee)
			{
				if(card_id != Session["card_id"].ToString())
					Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=e&ti=" + m_topicid + "&pi=" + post_id + " class=o>Edit Post</a>");
				Response.Write("&nbsp&nbsp;<a href=bb.aspx?t=d&ti=" + m_topicid + "&pi=" + post_id + " class=o>Del Post</a>");
				Response.Write("&nbsp&nbsp;<b>ip : <a href=trace.aspx?" + poster_ip + " class=o>" + poster_ip + "</a>");
			}
		}
		else
			Response.Write("&nbsp;");
*/
	}
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=5 align=right>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
	return true;
}

bool PrintEditPostForm()
{
	string sc = "SELECT bp.*, c.* ";
	if(m_code != "")
		sc += ", p.brand, p.price, p.cat_id ";
	sc += " FROM bb_post bp JOIN bb_topic c ON c.id=bp.topic_id ";
	if(m_code != "")
		sc += " JOIN used_product p ON p.topic_id=c.id ";
	sc += " WHERE bp.id=" + m_postid + " AND bp.deleted=0";
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
		Response.Write("<br><center><h3>Post Not Found</h3>");
		return false;
	}
	DataRow dr = ds.Tables["posts"].Rows[0];
	string card_id = dr["card_id"].ToString();
	string subject = dr["subject"].ToString();
	string nclass = dr["class"].ToString();
	bool bSubject = bool.Parse(dr["is_first_post"].ToString());

	string brand = "";
	string price = "";
	string cat_id = "";
	string name = "";
	string city = "";
	if(m_code != "")
	{
		brand = dr["brand"].ToString();
		price = dr["price"].ToString();
		cat_id = dr["cat_id"].ToString();
	}
	if(!TS_UserLoggedIn())
	{
		Response.Write("<br><center><h3>ACCESS DENIED</h3>");
		Response.End();
		return false;
	}
	else if(!m_bIsEmployee && card_id != Session["card_id"].ToString())
	{
		Response.Write("<br><center><h3>ACCESS DENIED</h3>");
		Response.End();
		return false;
	}

	Response.Write("<br><table width=70% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<tr><td colspan=2 align=center><font size=+1><b>Edit ");
	if(m_code != "")
		Response.Write("Item");
	else
		Response.Write("Post");
	Response.Write("</b></font></td></tr>");
	Response.Write("<form action=bb.aspx?t=" + m_arg + " method=post>");

	if(m_code != "")
	{
		Response.Write("<tr><td><b>Catalog</b></td><td><select name=cat>");
		PrintExistsCatalogOptions(cat_id);
		Response.Write("</select> <b> &nbsp&nbsp&nbsp&nbsp;New Catalog</b><font size=-2><i> (if no suitable)<i></font><input type=text name=new_cat size=10></td></tr>");
		Response.Write("<tr><td><b>Brand</b></td><td><input type=text name=brand value='" + brand + "'></td></tr>");
//		Response.Write("<tr><td><b>Description</b></td><td><input type=text name=subject size=70 maxlength=120></td></tr>");
		Response.Write("<tr><td><b>Price</b></td><td><input type=text name=price value='" + price + "'></td></tr>");
//		Response.Write("<tr><td valign=top><b>Details</b></td><td><textarea name=text cols=70 rows=7></textarea></td></tr>");
	}

//	subject = ds.Tables["posts"].Rows[0]["subject"].ToString();
	if(bSubject)
	{
		Response.Write("<tr><td bgcolor=#EEEEEE><b>");
		if(m_code != "")
			Response.Write("Description");
		else
			Response.Write("Subject");
		Response.Write("</b></td><td bgcolor=#EEEEEE><input type=text size=70 name=subject maxlength=70 value='");
		Response.Write(subject + "'></td></tr>");
	}
	string time = (DateTime.Parse(dr["post_time"].ToString()) ).ToString("dd-MM-yyyy HH:mm");
	string text = dr["text"].ToString();
//	if(bTopic)
//		Response.Write("<tr><td><b>Subject : </b></td><td><input type=text name=subject size=60 max=60></td></tr>");
//	else
//		Response.Write("<input type=hidden name=topic_id value=" + m_topicid + ">");
	
	Response.Write("<tr><td><b>");
	if(m_code != "")
		Response.Write("Details");
	else
		Response.Write("Text");
	Response.Write("</b></td><td width=100%><textarea name=text rows=20 cols=70>");
	Response.Write(text);
	Response.Write("</textarea></td></tr>");
	if(bSubject &&m_bIsEmployee)
	{
		Response.Write("<tr><td colspan=2 align=right><input type=checkbox name=notice");
		if(nclass == "0")
			Response.Write(" checked");
		Response.Write(">Notice ");
		Response.Write("<input type=checkbox name=stick");
		if(nclass == "1")
			Response.Write(" checked");
		Response.Write(">Stick</td></tr>");
		if(nclass == "0")
		{
			Response.Write("<input type=hidden name=notice_old value=on>");
			Response.Write("<input type=hidden name=stick_old value=off>");
		}
		else if(nclass == "1")
		{
			Response.Write("<input type=hidden name=notice_old value=off>");
			Response.Write("<input type=hidden name=stick_old value=on>");
		}
		else
		{
			Response.Write("<input type=hidden name=notice_old value=off>");
			Response.Write("<input type=hidden name=stick_old value=off>");
		}
	}
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Save></td></tr>");
	Response.Write("</form></table>");
	return true;
}

bool DoSave()
{
	if(!TS_UserLoggedIn())
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=login.aspx\">");
		return false;
	}

	string sc = "SELECT * FROM bb_post WHERE id=" + m_postid + " AND deleted=0";
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
		Response.Write("<br><center><h3>Post Not Found</h3>");
		return false;
	}
	DataRow dr = ds.Tables["posts"].Rows[0];
	string card_id = dr["card_id"].ToString();
	
	if(!m_bIsEmployee && card_id != Session["card_id"].ToString())
	{
		Response.Write("<br><center><h3>ACCESS DENIED</h3>");
		return false;
	}

//	string subject = Request.Form["subject"];
	string text = Request.Form["text"];
	if(!m_bIsEmployee && m_code == "")
		text += "\r\n\r\n----- last edit by " + Session["name"] + " at " + DateTime.Now.ToString("dd-MM-yyyy	HH:mm");
//	if(subject == "")
//	{
//		Response.Write("<h3>Error, no subject</h3>");
//		return false;
//	}
	if(m_postid == "")
	{
		Response.Write("<h3>Error, No Post ID</h3>");
		return false;
	}
	else if(text == "")
	{
		Response.Write("<h3>Error, no content</h3>");
		return false;
	}

	string subject = "";
	string cat_id = "";
	double dPrice = 0;
	string brand = "";
	if(m_code != "")
	{
		//check catalog
		string cat = Request.Form["new_cat"];
		Trim(ref cat);
		cat = bbEncode(cat);
		if(cat == "")
			cat_id = Request.Form["cat"];
		else
			cat_id = CheckNewCatalog(cat);

		//check price format
		string price = Request.Form["price"];
		if(price == "")
		{
			Response.Write("<br><br><center><h3>Error, please supplier price</h3>");
			return false;
		}
		try
		{
			dPrice = double.Parse(price, NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
			Response.Write("<br><br><h3>Error, invalid price format, please try again</h3>");
			return false;
		}

		//brand
		brand = Request.Form["brand"];
		brand = bbEncode(brand);
	}

//	subject = bbEncode(subject);
	if(text.Length > 20480)
		text = text.Substring(0, 20480);
	text = bbEncode(text);

	if(Request.Form["subject"] != null)
	{
		subject = Request.Form["subject"];
		if(subject.Length > 70)
			subject = subject.Substring(0, 70);
		m_topicSubject = subject;
	}

	//update post
	sc = "UPDATE bb_post SET text='" + text + "' ";
	if(!m_bIsEmployee)
		sc += ", poster_ip='" + Session["rip"].ToString() + "' ";
	sc += " WHERE id=" + m_postid;
	if(Request.Form["subject"] != null)
	{
		sc += " UPDATE bb_topic SET subject='" + bbEncode(subject) + "' ";
		if(Request.Form["notice_old"] != Request.Form["notice"] || Request.Form["stick_old"] != Request.Form["stick"])
		{
			string nclass = "9";
			if(Request.Form["notice"] == "on")
				nclass = "0";
			else if(Request.Form["stick"] == "on")
				nclass = "1";
			sc += ", class=" + nclass;
		}
		sc += " WHERE id=" + m_topicid;
	}
	if(m_code != "")
	{
		sc += " UPDATE used_product SET cat_id=" + cat_id + ", brand='" + brand + "' ";
		sc += ", name='" + subject + "', price=" + dPrice;
		sc += " WHERE id=" + m_code;
	}
//DEBUG("sc=", sc);
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
		return false;
	}
	return true;
}

bool DoDelPost()
{
	if(!TS_UserLoggedIn())
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=login.aspx\">");
		return false;
	}

	string sc = "SELECT * FROM bb_post WHERE id=" + m_postid + " AND deleted=0";
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
		Response.Write("<br><center><h3>Post Not Found</h3>");
		return false;
	}
	DataRow dr = ds.Tables["posts"].Rows[0];
	string card_id = dr["card_id"].ToString();
	bool bFirst = bool.Parse(dr["is_first_post"].ToString());

	if(!m_bIsEmployee && card_id != Session["card_id"].ToString())
	{
		Response.Write("<br><center><h3>ACCESS DENIED</h3>");
		return false;
	}

	sc = "UPDATE bb_post SET deleted=1 WHERE id=" + m_postid; 
	if(bFirst) //delete whole topic
		sc += " UPDATE bb_topic SET deleted=1 WHERE id=" + m_topicid;
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
		return false;
	}
	return true;
}

bool DoSubmit()
{
	if(!m_bIsEmployee && Session["last_post_time"] != null)
	{
		if( (DateTime.Now - (DateTime)Session["last_post_time"]).TotalSeconds < 60)
		{
			Response.Write("<br><br><center><h3>Sorry, one post only per minute, please wait a moment</h3>");
			return false;
		}
	}
	string name = "";
	string email = "";
	string card_id = "";
//	if(!TS_UserLoggedIn())
//	{
//		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=login.aspx\">");
//		return false;
//	}
	if(TS_UserLoggedIn())
		card_id = Session["card_id"].ToString(); //if have card_id then ignore name and email
	string subject = Request.Form["subject"];
	m_topicSubject = subject;
	string text = Request.Form["text"];

	name = Request.Form["name"];
	email = Request.Form["email"];
	if(Request.Form["notify"] == "on")
	{
		if(email == "")
		{
			Response.Write("<h3>Error, email address needed to notify you on reply</h3>");
			return false;
		}
	}
	if(card_id == "" && name == "")
	{
		Response.Write("<h3>Error, no poster name</h3>");
		return false;
	}
	if(subject == "")
	{
		Response.Write("<h3>Error, no subject</h3>");
		return false;
	}
	else if(text == "")
	{
		Response.Write("<h3>Error, no content</h3>");
		return false;
	}

	name = bbEncode(name);
	if(subject != null)
	{
		subject = bbEncode(subject);
		if(subject.Length > 70)
			subject = subject.Substring(0, 70);
	}
	if(text.Length > 10240)
		text = text.Substring(0, 10240);
	text = bbEncode(text);
	string code = "0";
	if(Request.Form["hide_code"] != null && Request.Form["hide_code"] != "")
		code = Request.Form["hide_code"];

	string sc = "";
	if(Request.Form["topic_id"] != null)
		m_topicid = Request.Form["topic_id"];
	else
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO bb_topic (forum_id, subject, first_poster_id, first_poster_name, last_poster_id, last_poster_name, class, code) ";
		sc += " VALUES(1, '" + subject + "', '" + card_id + "', '" + name + "', '" + card_id + "', '" + name + "', ";
		if(Request.Form["notice"] == "on")
			sc += "0"; //class 0 = public_notice
		else if(Request.Form["stick"] == "on")
			sc += "1"; //class 1 = stick to top
		else
			sc += "9"; //normal post
		if(TSIsDigit(code))
			sc += ", "+code;
		else
			sc += ",0";
		sc += ") ";
		sc += " SELECT IDENT_CURRENT('bb_topic') AS id";
		sc += " COMMIT ";
		try
		{
			//insert topic and get topic id first
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			rows = myCommand1.Fill(ds, "id");
			if(rows == 1)
			{
				m_topicid = ds.Tables["id"].Rows[0]["id"].ToString();
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	if(m_topicid == "")
	{
		Response.Write("<h3>Error Getting New Topic ID</h3>");
		return false;
	}

	//insert post
	sc = "INSERT INTO bb_post (topic_id, poster_ip, card_id, poster_name, poster_email, text, is_first_post) ";
	sc += "VALUES(" + m_topicid + ", '" + Session["rip"].ToString();
	sc += "', '" + card_id + "', '" + name + "', '" + email + "', '" + text + "', ";
	if(Request.Form["topic_id"] == null)
		sc += "1";
	else
		sc += "0";
	sc += ") ";
	if(card_id != "")
		sc += " UPDATE card SET last_post_time=GETDATE(), total_posts=total_posts+1 WHERE id=" + card_id;
	if(Request.Form["topic_id"] != null)
	{
		sc += " UPDATE bb_topic SET replies=replies+1, last_poster_name='" + name + "', last_poster_id='" + card_id;
		sc += "', last_post_time=GETDATE() WHERE id=" + m_topicid;
	}
	if(Request.Form["notify"] == "on") //notify
	{
		sc += " IF NOT EXISTS (SELECT id FROM bb_notify WHERE topic_id=" + m_topicid + " AND ( ";
		if(card_id != "")
			sc += " card_id=" + card_id + " OR ";
		sc += " email='" + email + "' ";
		sc += " )) INSERT INTO bb_notify(topic_id, card_id, email) VALUES (" + m_topicid + ", '" + card_id + "', '" + email + "') ";
	}
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
		return false;
	}

	if(Request.Form["topic_id"] != null) //it is reply, not new post
	{
		sc = "SELECT * FROM bb_notify WHERE topic_id=" + m_topicid;
//		sc += " DELETE FROM bb_notify WHERE topic_id=" + m_topicid;
		try
		{
			//insert topic and get topic id first
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			rows = myCommand1.Fill(ds, "notify");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["notify"].Rows[i];
			string nname = " ... ";
			string nemail = dr["email"].ToString();
			string ncard_id = dr["card_id"].ToString();
			if(ncard_id != "" && ncard_id != "0")
			{
				DataRow drc = GetCardData(ncard_id);
				if(drc != null)
				{
					nemail = drc["email"].ToString();
					nname = drc["name"].ToString();
				}
			}
			bool bDoNotify = true;
			if(Session["email"] != null)
			{
				if(Session["email"].ToString() == nemail)
					bDoNotify = false;
			}
			if(bDoNotify)
			{
				DoNotifyEmail(nname, name, text, nemail, "/bb.aspx?ti=" + m_topicid);
				sc = "DELETE FROM bb_notify WHERE topic_id=" + m_topicid;
				try
				{
					//insert topic and get topic id first
					SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
					rows = myCommand1.Fill(ds, "notify");
				}
				catch(Exception e) 
				{
					ShowExp(sc, e);
					return false;
				}
			}
		}
	}
	else
		DoSalesNotify(name, text, "/bb.aspx?ti=" + m_topicid);

	Session["last_post_time"] = DateTime.Now;
	return true;
}

void DoDelPic()
{
	string file = Server.MapPath(GetRootPath() + "/bbi/" + m_postid + "/" + Request.QueryString["file"]);
//DEBUG("file=", file);
	File.Delete(file);
}

// Processes click on our cmdSend button
void cmdSend_Click(object sender, System.EventArgs e)
{
	// Check to see if file was uploaded
	if( filMyFile.PostedFile != null )
	{
		// Get a reference to PostedFile object
		HttpPostedFile myFile = filMyFile.PostedFile;

		string ext = Path.GetExtension(myFile.FileName);
		ext = ext.ToLower();
		if(ext != ".jpg" && ext != ".gif")
		{
			Response.Write("<h3>ERROR Only .jpg, .gif File Allowed</h3>");
			return;
		}

		// Get size of uploaded file
		int nFileLen = myFile.ContentLength; 
//DEBUG("nFileLen=", nFileLen);
		if(nFileLen > 204800)
		{
			Response.Write("<h3>ERROR Max File Size(200 KB) Exceeded. ");
			Response.Write(Path.GetFileName(myFile.FileName) + " " + (int)nFileLen/1000 + " KB </h3>");
			return;
		}

		// make sure the size of the file is > 0
		if( nFileLen > 0 )
		{
			// Allocate a buffer for reading of the file
			byte[] myData = new byte[nFileLen];

			// Read uploaded file from the Stream
			myFile.InputStream.Read(myData, 0, nFileLen);

			// Create a name for the file to store
			string strFileName = Path.GetFileName(myFile.FileName);
			string strPath = Server.MapPath(GetRootPath() + "/bbi/" + m_postid);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += "\\";
			string purePath = strPath;
			strPath += strFileName;
			
//DEBUG("pathname=", strPath);

			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);
			if(m_code == "")
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=bb.aspx?t=" + m_arg + "\">");
			else
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=up.aspx?c=" + m_code + "\">");
		}
	}
	return;
}

// Writes file to current folder
void WriteToFile(string strPath, ref byte[] Buffer)
{
	// Create a file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(Buffer, 0, Buffer.Length);
	// Close file
	newFile.Close();
}

bool DoAddUsedItem()
{
	if(!TS_UserLoggedIn())
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=login.aspx\">");
		return false;
	}

	if(Session["last_post_time"] != null)
	{
		if( (DateTime.Now - (DateTime)Session["last_post_time"]).TotalSeconds < 60)
		{
			Response.Write("<br><br><center><h3>Sorry, one post only per minute, please wait a moment</h3>");
			return false;
		}
	}

	//check catalog
	string cat_id = "";
	string cat = Request.Form["new_cat"];
	Trim(ref cat);
	cat = bbEncode(cat);
	if(cat == "")
		cat_id = Request.Form["cat"];
	else
		cat_id = CheckNewCatalog(cat);

	//check price format
	double dPrice = 0;
	string price = Request.Form["price"];
	if(price == "")
	{
		Response.Write("<br><br><center><h3>Error, please supplier price</h3>");
		return false;
	}
	try
	{
		dPrice = double.Parse(price, NumberStyles.Currency, null);
	}
	catch(Exception e)
	{
		Response.Write("<br><br><h3>Error, invalid price format, please try again</h3>");
		return false;
	}

	//brand
	string brand = Request.Form["brand"];
	brand = bbEncode(brand);

	//description
	string desc = Request.Form["subject"];
	if(desc == "")
	{
		Response.Write("<br><br><center><h3>Error, No Description</h3>");
		return false;
	}
	desc = bbEncode(desc);

	//details
	string details = Request.Form["text"];
	if(details != null)
	{
		if(details.Length > 20480)
			details = details.Substring(0, 20480);
		details = bbEncode(details);
	}
	if(details == null || details == "")
	{
		Response.Write("<br><br><center><h3>Error, No Details</h3>");
		return false;
	}

	//post info
	string card_id = Session["card_id"].ToString();
	string name = "";
	string email = "";
	
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO bb_topic (forum_id, subject, first_poster_id, first_poster_name, last_poster_id, last_poster_name, class, code) ";
	sc += " VALUES(2, '" + desc + "', '" + card_id + "', '" + name + "', '" + card_id + "', '" + name + "', ";
	sc += "9"; //normal post
	sc += ",0";
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('bb_topic') AS id";
	sc += " COMMIT ";
	try
	{
		//insert topic and get topic id first
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "id");
		if(rows == 1)
		{
			m_topicid = ds.Tables["id"].Rows[0]["id"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(m_topicid == "")
	{
		Response.Write("<h3>Error Getting New Topic ID</h3>");
		return false;
	}

	//insert post
	sc = "INSERT INTO bb_post (topic_id, poster_ip, card_id, poster_name, poster_email, text, is_first_post) ";
	sc += "VALUES(" + m_topicid + ", '" + Session["rip"].ToString();
	sc += "', '" + card_id + "', '" + name + "', '" + email + "', '" + details + "', 1) ";
	sc += " UPDATE card SET last_post_time=GETDATE(), total_posts=total_posts+1 WHERE id=" + card_id;
//	if(Request.Form["notify"] == "on") //notify
//	{
		sc += " IF NOT EXISTS (SELECT id FROM bb_notify WHERE topic_id=" + m_topicid + " AND ( ";
		if(card_id != "")
			sc += " card_id=" + card_id + " OR ";
		sc += " email='" + email + "' ";
		sc += " )) INSERT INTO bb_notify(topic_id, card_id, email) VALUES (" + m_topicid + ", '" + card_id + "', '" + email + "') ";
//	}
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
		return false;
	}

	sc = " BEGIN TRANSACTION ";
	sc += " INSERT INTO used_product (cat_id, name, brand, price, topic_id) ";
	sc += " VALUES(" + cat_id + ", '" + desc + "', '" + brand + "', " + dPrice + ", " + m_topicid + ")";
	sc += " SELECT IDENT_CURRENT('used_product') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "up_id");
//DEBUG("rows=", rows);
		if(rows == 1)
		{
			m_upid = ds.Tables["up_id"].Rows[0]["id"].ToString();
//DEBUG("id=", m_upid);
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Session["last_post_time"] = DateTime.Now;

	return true;
}

string CheckNewCatalog(string cat)
{
	string sc = " BEGIN TRANSACTION ";
	sc += " IF NOT EXISTS (SELECT id FROM used_catalog WHERE cat='" + cat + "') ";
	sc += "  BEGIN ";
	sc += "   INSERT INTO used_catalog (cat) VALUES('" + cat + "')";
	sc += " SELECT IDENT_CURRENT('used_catalog') AS id";
	sc += "  END ";
	sc += " ELSE ";
	sc += "  BEGIN ";
	sc += "   SELECT id FROM used_catalog WHERE cat = '" + cat + "' ";
	sc += "  END ";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(ds, "cat_id");
//DEBUG("rows=", rows);
		if(rows == 1)
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName); //rebuild menu cache
			return ds.Tables["cat_id"].Rows[0]["id"].ToString();
//DEBUG("id=", m_topicid);
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}

</script>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<br>
<table width=70% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>
<tr><td><font size=+1 color=red><b>Attach Images</b></font><br>

<table><tr>
<td><input id="filMyFile" type="file" size=50 runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>

<br>
<asp:Label id=LOldPic runat=server/>
</td></tr></table>


</FORM>
<asp:Label id=LFooter runat=server/>
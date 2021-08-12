<!-- #include file="stat_fun.cs" -->
<script runat=server>

string m_sdirectory = "1"; //chinese, OEM etc..
string m_cardid = ""; //card id for specified customer statement e-mailing;
string m_vpath = "";
string m_customer = "";

bool b_singlemail = false;
DataSet dsi = new DataSet();

void mPage_Load()
{
	m_vpath = GetRootPath();
	m_vpath += "/upload/" + DateTime.Now.ToString("ddMMyy") + "/" + Session.SessionID.ToString();

	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
	{
		m_cardid = Request.QueryString["ci"];	
		b_singlemail = true;
		DataRow dr = GetCardData(m_cardid);
		if(dr != null)
		{
			m_customer = dr["trading_name"].ToString();
			if(m_customer == "")
				m_customer = dr["company"].ToString();
		}
	}

	if(Request.Form["cmd"] == "Send")
	{
		if(b_singlemail)
		{
			if(SendSglMail())
			{
				Response.Write("<br><h3><font size=+1>&nbsp;&nbsp;&nbsp;<b>Your mail has been sent...!</b></font></h3><br><br>");
				Response.Write("&nbsp;&nbsp;&nbsp;<input type=button name=tostatement " + Session["button_style"]);
				Response.Write(" value=' Statement List ' onclick=window.location='statement.aspx?r="+DateTime.Now.ToOADate());
				Response.Write("'>&nbsp;&nbsp;&nbsp;");
				Response.Write("<input type=button name=gomain " + Session["button_style"] + " value=' Main Menu '");
				Response.Write(" onclick=window.location='default.aspx'>");				
			}
			else
				Response.Write("<br><h3><font size=+1><b>Please fill receiver e-mail address...!</b></font></h3>");
			return;
		}
		else
		{
			if(SendMail())
				PrintMailSentInfo();
			else
				Response.Write("<br><h3><font size=+1><b>Error in mailing prossess!</b></font></h3>");
			return;		
		}
	}
	else if(Request.Form["cmd"] == "Add")
	{
		string fn = Request.Form["attachment"];
		if(fn != null)
		{
			int m = 1;
			while(Session["broadmail_attach" + m] != null)
			{
				m++;
			}
			Session["broadmail_attach" + m] = fn;
		}
	}

	if(Request.QueryString["t"] == "da") //delete attachment
	{
		string sm = Request.QueryString["fm"];
		if(sm != null)
		{
			DoDeleteAttachment(sm);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?\">");
			return;
//			int m = MyIntParse(sm);
//			if(m > 0)
//				Session["broadmail_attach" + m] = ""; // not null
		}
	}

	PrintMail();

	return;
}

void PrintMail()
{
	string s_sendermail = m_sSalesEmail;//Session["email"].ToString();
	string s_sglmailto = "";
	string s_url = "";

	s_url = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
//DEBUG("url= ", s_url);

	if(b_singlemail)
		Response.Write("<br><h3><font size=+1><center><b>Statement</b></center></font></h3><br>");
	else
		Response.Write("<br><h3><font size=+1><center><b>Group e-mailing</b></center></font></h3><br>");
	Response.Write("<form action='" + s_url + "' method=post>");
	Response.Write("<table align=center valign=top cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td width=10% align=right><b>From:&nbsp</b></td><td><font color=blue>" + s_sendermail + "</font></td>");		
	//Get Directory list
	if(b_singlemail)
	{
		GetSglMailto(ref s_sglmailto);
		Response.Write("</tr>");
		Response.Write("<tr><td align=right><b>To:&nbsp</b></td>");
		Response.Write("<td><input type=editbox size=100 name=singlemailto value='"+s_sglmailto+"'></td></tr>");
	}
	if(!b_singlemail)
	{
		Response.Write("<tr><td valign=center align=right><b>BCC Directory:&nbsp;</b><td>");
		TickDirectory();
		Response.Write("</td></tr>");
	}
	Response.Write("<tr><td align=right nowrap><b>BCC Additional:&nbsp</b></td><td><input type=editbox size=100 name=mailbcc value=''></td></tr>");
	
	if(b_singlemail)
	{
		Response.Write("<tr><td align=right><b>Subject:&nbsp</b></td><td>");
		Response.Write("<input type=editbox size=100 name=mailsubj value='Statement Notice --- " + m_customer + "'></td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td align=left><b>");
		Response.Write("<font color=red>Statement is atteched!</font></b></td></tr>");
	}
	else
	{
		Response.Write("<tr><td align=right><b>Subject:&nbsp</b></td>");
		Response.Write("<td><input type=editbox size=100 name=mailsubj value=''></td></tr>");
	}

	Response.Write("<tr><td colspan=2><br><font color=blue><b>Content:</b></font></td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=mailcontent rows=15 cols=90>");
	if(!b_singlemail)
		Response.Write(ReadSitePage("group_mail_template"));
	Response.Write("</textarea></td></tr>");

	string rpath = Server.MapPath(m_vpath);
	if(Directory.Exists(rpath))
	{
		Response.Write("<tr><td colspan=2>");
		Response.Write("<b>Attachment List</b>");
		Response.Write("</td></tr>");

		Response.Write("<tr><td colspan=2>");
		Response.Write("<table cellspacing=3 cellpadding=1 border=0 bordercolor#EEEEEE");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
		Response.Write("<tr style=\"color:black;background-color:aliceblue;font-weight:bold;\">\r\n");
		Response.Write("<td width=100><b>FILE</b></td>");
		Response.Write("<td width=70><b>SIZE</b></td>");
		Response.Write("<td width=50><b>DELETE</b></td>");
		Response.Write("</tr>");

		DirectoryInfo di = new DirectoryInfo(rpath);
		foreach (FileInfo f in di.GetFiles("*.*")) 
		{
			string file = Path.GetFileName(f.FullName);
			Response.Write("<tr><td>");
			Response.Write(file);
			Response.Write("</td>");
			Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");

			Response.Write("<td>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('?t=da&fm=" + HttpUtility.UrlEncode(file) + "') value=Delete>");
			Response.Write("</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");

		Response.Write("</td></tr>");
	}

/*	Response.Write("<tr><td valign=top><b>Attachments : </b></td>");
	Response.Write("<td><input type=file width=20 name=attachment " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value=Add " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");
	string afile = "";
	int m = 1;
	while(Session["broadmail_attach" + m] != null)
	{
		afile = Session["broadmail_attach" + m].ToString();
		if(afile != "")
			Response.Write("<a href=broadmail.aspx?t=da&fm=" + m + " class=o title='Click To Remove'>" + afile + "</a><br>");
		m++;
	}
	Response.Write("</td></tr>");
*/
	Response.Write("<tr><td colspan=2 align=right><br><input type=submit name=cmd value=Send " + Session["button_style"]);
	Response.Write(" value=' Send '>&nbsp;&nbsp;&nbsp<input type=button name=cmd value=Cancel " + Session["button_style"]);
	Response.Write(" value=' Cancel ' onclick=window.location=('broadmail.aspx')></td></tr>");

	Response.Write("</table></form>");
	LFooter.Text = m_sAdminFooter;
	return;
}

void PrintMailSentInfo()
{
	Response.Write("<br><h3><font size=+1>&nbsp;&nbsp;&nbsp;<b>Your mail has been sent...!</b></font></h3><br><br>");
	Response.Write("&nbsp;&nbsp;&nbsp;<input type=button name=newmail " + Session["button_style"]);
	Response.Write(" value=' Compose new mail ' onclick=window.location='broadmail.aspx?r="+DateTime.Now.ToOADate());
	Response.Write("'>&nbsp;&nbsp;&nbsp;");
	Response.Write("<input type=button name=gomain " + Session["button_style"] + " value=' Main Menu '");
	Response.Write(" onclick=window.location='default.aspx'>");

	return;
}

bool SendMail()
{
	MailMessage msgMail = new MailMessage();
	int rows = 0;
	int i = 0;
	int dirs = MyIntParse(Request.Form["dirs"]);
	string s = "";
	for(i=0; i<dirs; i++)
	{
		string dir_id = Request.Form["dir_id" + i];
		if(Request.Form["dir" + i] == "on")
		{
			if(s != "")
				s += " OR ";
			s += " directory = " + dir_id;
		}
	}

	string sc = "";
	sc = "SELECT email FROM card ";
	sc += " WHERE main_card_id is null AND type IN(1, 2) AND approved=1 "; //only customer and dealer
	if(s != "")
		sc += " AND (" + s + ") ";
	else
		sc += " AND directory = -1 "; //nothing to select
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows =  myAdapter.Fill(dsi, "eaddress");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	string s_bcc = "";
	if(rows > 0)
	{
		for(int j=0; j<rows; j++)
		{
			string email = dsi.Tables["eaddress"].Rows[j]["email"].ToString();
			if(email.IndexOf("@") > 0)
				s_bcc += email + "; ";		
		}
	
	}
	s_bcc += Request.Form["mailbcc"]; //additional bcc
	
	string rpath = Server.MapPath(m_vpath);
	if(Directory.Exists(rpath))
	{
		DirectoryInfo di = new DirectoryInfo(rpath);
		foreach (FileInfo f in di.GetFiles("*.*")) 
		{
			string fn = f.FullName;
			MailAttachment attach = new MailAttachment(fn);
			msgMail.Attachments.Add(attach);
		}
	}

	msgMail.From = m_sSalesEmail;//Session["email"].ToString();
	msgMail.To = Session["email"].ToString();
	msgMail.Subject = Request.Form["mailsubj"];
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = Request.Form["mailcontent"];
	msgMail.Body = msgMail.Body.Replace("\r\n", "<br>\r\n");
	msgMail.Cc = Request.Form["mailcc"];
	msgMail.Bcc = s_bcc;
	SmtpMail.Send(msgMail);

	//remove attachments list
	return true;
}

bool SendSglMail()
{
	m_timeOpt = "4";
	m_custID = m_cardid;

	if(!(Request.Form["singlemailto"].ToString().Length > 0))
		return false;

	MailMessage msgMail = new MailMessage();

	GetSelectedCust(m_custID);
	GetInvRecords(m_custID);

	string s_mailbody = PrintStatmentDetails();

	string afile = "";
	int m = 1;
	while(Session["broadmail_attach" + m] != null)
	{
		afile = Session["broadmail_attach" + m].ToString();
		if(afile != "")
		{
			MailAttachment attach = new MailAttachment(afile);
			msgMail.Attachments.Add(attach);
		}
		m++;
	}

	msgMail.From = m_sSalesEmail;//Session["email"].ToString();
	msgMail.Bcc = Session["email"].ToString();
	//msgMail.To = "@hotmail.com";
	msgMail.To = Request.Form["singlemailto"];
	msgMail.Subject = Request.Form["mailsubj"];
	msgMail.BodyFormat = MailFormat.Html;
	//msgMail.Body = s_mailbody;
	msgMail.Body = Request.Form["mailcontent"];
	msgMail.Body = msgMail.Body.Replace("\r\n", "<br>\r\n");
//	msgMail.Body = msgMail.Body.Replace("<br>", "\r\n<br>");
	msgMail.Body += "\r\n<br>\r\n<br>" + s_mailbody;
	msgMail.Cc = Request.Form["mailcc"];
	if(Request.Form["mailbcc"] != null && Request.Form["mailbcc"] != "")
	{
		msgMail.Bcc += "; ";
		msgMail.Bcc += Request.Form["mailbcc"];
	}

	SmtpMail.Send(msgMail);
	
	//remove attachments list
	m = 1;
	while(Session["broadmail_attach" + m] != null)
	{
		Session["broadmail_attach" + m] = null;
		m++;
	}

	return true;
}

void GetSglMailto(ref string s_email)
{
	int rows = 0;
	string sc = "SELECT email, ap_email FROM card WHERE id=" + m_cardid;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows =  myAdapter.Fill(dsi, "sglmailaddr");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
	if(rows > 0)
	{
		s_email = dsi.Tables["sglmailaddr"].Rows[0]["email"].ToString();
		if(dsi.Tables["sglmailaddr"].Rows[0]["ap_email"].ToString() != "")
			s_email = dsi.Tables["sglmailaddr"].Rows[0]["ap_email"].ToString();
		return;
	}

	return;
}

bool TickDirectory()
{
	int rows = 0;
	string sc = " SELECT * FROM enum WHERE class='card_dir' ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows =  myAdapter.Fill(dsi, "dir");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<input type=hidden name=dirs value=" + rows + ">");
	int j = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dsi.Tables["dir"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();

		if(j >= 3)
		{
//			Response.Write("<br>");
			j = 0;
		}
		j++;
		Response.Write("<input type=hidden name=dir_id" + i + " value=" + id + ">");
		Response.Write("<input type=checkbox name=dir" + i + ">" + name + "&nbsp&nbsp&nbsp&nbsp;");
	}
	return true;
}

// Processes click on our cmdSend button
void cmdSend_Click(object sender, System.EventArgs e)
{
	if(filMyFile.PostedFile == null)
		return;

	HttpPostedFile myFile = filMyFile.PostedFile;
	int nFileLen = myFile.ContentLength; 

	if(nFileLen <= 0)
		return;

	byte[] myData = new byte[nFileLen];
	myFile.InputStream.Read(myData, 0, nFileLen);

	string strFileName = Path.GetFileName(myFile.FileName);
//	string sExt = Path.GetExtension(myFile.FileName);
//	strFileName += sExt;
//	m_fileName = strFileName;
	string vpath = m_vpath;
	DirectoryInfo nd = new DirectoryInfo(Server.MapPath(vpath));
	nd.Create();

	vpath += "/";
	string strPath = Server.MapPath(vpath);
	string purePath = strPath;
	strPath += strFileName;
/*	
	//check old files, delete .gif or jpg (another type)if exists
	string sExtOld = ".gif";
	if(String.Compare(sExt, ".gif", true) == 0)
		sExtOld = ".jpg";
	string oldFile = purePath + m_code + sExtOld;
	if(File.Exists(oldFile))
		File.Delete(oldFile);
*/
//DEBUG("strPath=", strPath);
	WriteToFile(strPath, ref myData);

	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?\">");
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

void DoDeleteAttachment(string fn)
{
	string pf = Server.MapPath(m_vpath) + "\\" + fn;
	if(File.Exists(pf))
		File.Delete(pf);
}

</script>

<asp:label id=LTitle runat=server/>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=true>
<table><tr>
<td><b>Attachment : </b> <input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>

<br>
<asp:Label id=LOldPic runat=server/>

</FORM>
<asp:Label id=LFooter runat=server/>

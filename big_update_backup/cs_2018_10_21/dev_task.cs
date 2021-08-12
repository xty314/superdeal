<!-- #include file="page_index.cs" -->
<script runat=server>

DataSet ds = new DataSet();
string m_type = "";
string m_owner = "";
string m_founder = "";
string m_status = "";
string m_id = "";
string m_noteid = "";

void Page_Load(Object Src, EventArgs E )
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;
	
	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];
	if(Request.QueryString["owner"] != null)
	{
		if(IsInteger(Request.QueryString["owner"]))
			m_owner = Request.QueryString["owner"];
		else
			m_owner = Session["card_id"].ToString();
	}
	if(Request.QueryString["founder"] != null)
		m_founder = Request.QueryString["founder"];
	if(Request.QueryString["status"] != null)
		m_status = Request.QueryString["status"];
	if(Request.QueryString["id"] != null)
		m_id = Request.QueryString["id"];
	if(Request.QueryString["nid"] != null)
		m_noteid = Request.QueryString["nid"];

	r = DateTime.Now.ToOADate().ToString();

	if(Request.Form["kw"] != null && Request.Form["kw"] != "")
	{
		if(!DoSearchTask())
			return;
		PrintAdminHeader();
		PrintAdminMenu();
		PrintTaskList();
		LFooter.Text = m_sAdminFooter;
		return;
	}
	
	if(m_type == "un") //update note
	{
		if(UpdateTaskNote())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dev_task.aspx?");
			if(m_id != "")
				Response.Write("id=" + m_id + "&nid=" + m_noteid);
			Response.Write("&r=" + r + "\">");
		}
		return;
	}
	else if(m_type == "s" || m_type == "u")
	{
		if(SaveOrUpdateTask())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dev_task.aspx?id=");
			Response.Write(m_id + "&nid=" + m_noteid + "&r=" + r + "\">");
		}
		return;
	}
	else if(m_type == "da")
	{
		DoDelPic();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dev_task.aspx?id=");
		Response.Write(m_id + "&nid=" + m_noteid + "&r=" + r + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(m_type == "attach") //attachment
	{
		PrintAttachForm();
	}
	else if(m_type == "enote") //edit note
	{
		PrintEditNoteForm();
	}
	else if(m_type == "a" || m_type == "e") //add new task
	{
		PrintAddNewForm();
	}
	else
	{
		if(GetTask())
		{
			if(m_id == "")
				PrintTaskList();
			else
				PrintDetails();
		}
	}
	
	LFooter.Text = m_sAdminFooter;
}

bool PrintAddNewForm()
{
	if(!GetAllDevelopers())
		return false;

	DataRow dr = null;
	if(m_type == "e") //edit
	{
		if(!GetTask())
			return false;
		dr = ds.Tables["task"].Rows[0];
	}

	if(m_type == "e") //edit
	{
		Response.Write("<br><center><h3>Task - ");
		Response.Write("ID:" + m_id + "</h3></center>");
		Response.Write("<form action=dev_task.aspx?t=u&id="+m_id+ "&nid=" + m_noteid + " method=post>");
	}
	else
	{
		Response.Write("<br><center><h3>New Task</h3></center>");
		Response.Write("<form action=dev_task.aspx?t=s method=post>");
	}
	
	Response.Write("<table align=center cellspacing=2 cellpadding=2 border=1 bordercolorlight=#44444 bordercolordark=#AAAAAA bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

//	Response.Write("<table border=0 align=center>");
	
	if(m_type == "e")
	{
		Response.Write("<input type=hidden name=id value=" + m_id + ">");
		Response.Write("<input type=hidden name=owner_old value=" + ds.Tables["task"].Rows[0]["owner"].ToString() + ">");
		Response.Write("<input type=hidden name=founder value=" + ds.Tables["task"].Rows[0]["founder"].ToString() + ">");
		Response.Write("<tr><td><b>Current Owner</b></td><td>");
	}
	else
	{
		Response.Write("<tr><td><b>Assign To</b></td><td>");
	}
	PrintDeveloperList();
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Priority</b></td><td>");
	if(m_type == "e")
	{
		string pr = dr["priority"].ToString();
		Response.Write("<select name=priority>");
		
		Response.Write("<option value='Urgent' ");
		if(pr == "Urgent")
			Response.Write("selected");
		Response.Write(">Urgent</option>");
		
		Response.Write("<option value='High' ");
		if(pr == "High")
			Response.Write("selected");
		Response.Write(">High</option>");
		
		Response.Write("<option value='Mediate' ");
		if(pr == "Mediate")
			Response.Write("selected");
		Response.Write(">Mediate</option>");
		
		Response.Write("<option value='Low' ");
		if(pr == "Low")
			Response.Write("selected");
		Response.Write(">Low</option>");
	}
	else
	{
		Response.Write("<select name=priority>");
		Response.Write("<option value='Urgent'>Urgent</option>");
		Response.Write("<option value='High'>High</option>");
		Response.Write("<option value='Mediate' selected>Mediate</option>");
		Response.Write("<option value='Low'>Low</option>");
	}
	Response.Write("</select></td></tr>");

	if(m_type == "e")
	{
		Response.Write("<tr><td><b>Status</b></td><td>");
		string status = "pending";
		if(m_type == "e")
			status = dr["status"].ToString();
		Response.Write("<select name=status>");
		Response.Write("<option value='pending' ");
		if(status == "pending")
			Response.Write("selected");
		Response.Write(">pending</option>");

		Response.Write("<option value='in development' ");
		if(status == "in development")
			Response.Write("selected");
		Response.Write(">in development</option>");

		Response.Write("<option value='deffered' ");
		if(status == "deffered")
			Response.Write("selected");
		Response.Write(">deffered</option>");

		Response.Write("<option value='testing' ");
		if(status == "testing")
			Response.Write("selected");
		Response.Write(">testing</option>");

		Response.Write("<option value='finished' ");
		if(status == "finished")
			Response.Write("selected");
		Response.Write(">finished</option>");
		Response.Write("</select></td></tr>");

		Response.Write("<tr><td><b>Estimate Time</b></td><td>");
		Response.Write("<input type=text name=time size=30 value='");
		Response.Write(dr["time_need"].ToString());
		Response.Write("'></td></tr>");
	}

	string subject = "";
	if(m_type == "e")
	{
		subject = dr["subject"].ToString();
		subject = subject.Replace("\"", "-");
		subject = subject.Replace("'", "`");
	}

	Response.Write("<tr><td><b>Description</b></td><td>");
	Response.Write("<input type=text name=subject size=90 value=\"");
	if(m_type == "e")
		Response.Write(subject);
	Response.Write("\"></td></tr>");

	Response.Write("<tr><td><b>Related URL</b></td><td>");
	Response.Write("<input type=text name=url size=90 value='");
	if(m_type == "e")
		Response.Write(dr["url"].ToString());
	Response.Write("'></td></tr>");

	Response.Write("<tr><td colspan=2><b>Notes</b></td></tr>");
	Response.Write("<tr><td colspan=2>");
	Response.Write("<textarea name=note cols=80 rows=15>");
//	if(m_type == "e")
//		Response.Write(dr["details"].ToString());
	Response.Write("</textarea>");
	Response.Write("</td></tr>");

	string note = "";
	if(ds.Tables["task_note"] != null && ds.Tables["task_note"].Rows.Count > 0)
	{
		note = ds.Tables["task_note"].Rows[0]["note"].ToString().Replace("\r\n", "<br>");
		Response.Write("<tr><td colspan=2><b>Previous Note</b></td></tr>");
		Response.Write("<tr><td colspan=2>");
		Response.Write(note);
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td colspan=2 align=right>");
	if(m_type == "e" && SecurityCheck("administrator", false))
		Response.Write("<input type=submit name=cmd value='Delete' class=b>&nbsp;&nbsp;");
	Response.Write("<input type=submit name=cmd value='");
	if(m_type == "e")
		Response.Write("Update");
	else
		Response.Write("Save");
	Response.Write("' class=b>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2 align=right><a href=dev_task.aspx?r=" + r + " class=o>Show All Tasks</a>");
	
	Response.Write("</table></form>");

//	Response.Write("</td></tr></table>");	
	return true;
}

void PrintDeveloperList()
{
	Response.Write("<select name=owner>");
	for(int i=0; i<ds.Tables["dev"].Rows.Count; i++)
	{
		string id = ds.Tables["dev"].Rows[i]["id"].ToString();
		string name = ds.Tables["dev"].Rows[i]["name"].ToString();
		Response.Write("<option value='" + id + "'");
		if(m_type == "e")
		{
			if(id == ds.Tables["task"].Rows[0]["owner"].ToString())
				Response.Write(" selected");
		}
		Response.Write(">" + name + "</option>");
	}
	Response.Write("</select>");
}

bool GetAllDevelopers()
{
	string sc = "SELECT * FROM card ";// WHERE type<>" + GetEnumID("card_type", "dealer");
	sc += " WHERE type = " + GetEnumID("card_type", "employee");
//	sc += " AND type<>" + GetEnumID("card_type", "supplier");
	sc += " ORDER BY name";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "dev");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DeleteTask()
{
	string id = Request.Form["id"].ToString();
	string sc = "DELETE FROM dev_task WHERE id=" + id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	m_id = "";
	m_noteid = "";
	return true;
}

bool SaveOrUpdateTask()
{
	bool bUnAssigned = false;
	string cmd = Request.Form["cmd"];
	if(cmd == "Delete")
		return DeleteTask();

	string founder = Session["card_id"].ToString();
	string owner_old = Request.Form["owner_old"];
	string owner = Request.Form["owner"];
	if(owner == "0")
	{
		owner = founder;
		bUnAssigned = true;
	}
	
	string url = Request.Form["url"];
	string subject = Request.Form["subject"];
	string priority = Request.Form["priority"];
	string time_need = Request.Form["time"];
	string note = Request.Form["note"];
	string status = Request.Form["status"];

	time_need = EncodeQuote(time_need);
	subject = EncodeQuote(subject);
	note = EncodeQuote(note);

	string sc = "";
	if(m_type == "s") //new task
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO dev_task (founder, owner, subject, url, priority) VALUES('";
		sc += founder;
		sc += "', '";
		sc += owner;
		sc += "', N'";
		sc += subject;
		sc += "', '";
		sc += url;
		sc += "', '";
		sc += priority;
		sc += "')";
		sc += " SELECT IDENT_CURRENT('dev_task') AS id";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			if(myCommand1.Fill(ds, "id") == 1)
			{
				m_id = ds.Tables["id"].Rows[0]["id"].ToString();
//DEBUG("id=", m_topicid);
			}
			else
			{
				Response.Write("<br><br><center><h3>Failed getting IDEN_CURRENT");
				return false;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	else if(m_type == "u") //update task
	{
		sc = "UPDATE dev_task SET ";
		sc += "subject = N'" + subject + "', ";
		sc += "url = N'" + url + "', ";
		sc += " time_need = '" + time_need + "', ";
		sc += " status = '" + status + "', ";
		sc += " priority = '" + priority + "',  ";
		if(owner != owner_old)
			sc += "assign_time = GETDATE(), owner=" + owner + ", ";
		sc += " last_updated = GETDATE() ";
		sc += "WHERE id=" + m_id;
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	else
		return true;

	if(note != "")
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO dev_task_note (task_id, card_id, note) VALUES(" + m_id + ", ";
		sc += Session["card_id"].ToString() + ", N'" + note + "') ";
		sc += " SELECT IDENT_CURRENT('dev_task_note') AS id";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			if(myCommand1.Fill(ds, "note_id") == 1)
			{
				m_noteid = ds.Tables["note_id"].Rows[0]["id"].ToString();
//DEBUG("id=", m_topicid);
			}
			else
			{
				Response.Write("<br><br><center><h3>Failed getting IDEN_CURRENT from dev_task_note");
				return false;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	if(m_type == "s") //new task
	{
		string email = TSGetUserEmailByID(owner);

		MailMessage msgMail = new MailMessage();
		
		msgMail.To = email;
		if(bUnAssigned)
			msgMail.To = "alert@eznz.com";

		msgMail.From = Session["email"].ToString(); //"postmaster@eznz.com";
		msgMail.Subject = "Bug/Task";
		msgMail.BodyFormat = MailFormat.Html;

		string uname = TSGetUserNameByID(owner);
		string sname = "";
		for(int i=0; i<uname.Length; i++)
		{
			if(uname[i] == ' ')
				break;
			sname += uname[i];
		}
		string assignto = sname;
		if(bUnAssigned)
			assignto = "Team";

		string msg = "Dear " + assignto + ",<br>\r\n";
		msg += "There's a new task:<br>\r\n";
		msg += subject;
		msg += "<br><br>\r\n\r\n";
		msg += "URL: " + url + "<br>\r\n";
		msg += "Status: " + status + "<br>\r\n";
		msg += "Note:<br>\r\n";
		msg += HTMLDetails(note);
		msg += "<br><br>\r\n\r\n";
		msg += "More details on BugReport page : ";
		msg += "http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/admin/dev_task.aspx?owner=" + owner + "&r=" + r;
		msgMail.Body = msg;

		SmtpMail.Send(msgMail);
	}
	else if(status == "finished")
	{
		founder = Request.Form["founder"];
		string email = TSGetUserEmailByID(founder);
		MailMessage msgMail = new MailMessage();
		
		msgMail.To = email;

		msgMail.From = Session["email"].ToString(); //"webmaster@eznz.com";
		msgMail.Subject = "Bug/Task fixed/finished";
		msgMail.BodyFormat = MailFormat.Html;

		string uname = TSGetUserNameByID(founder);
		string sname = "";
		for(int i=0; i<uname.Length; i++)
		{
			if(uname[i] == ' ')
				break;
			sname += uname[i];
		}
		string msg = "Dear " + sname + ",<br>\r\n";
		msg += "Your bug/task has been fixed/finished:<br>\r\n";
		msg += subject;
		msg += "<br><br>\r\n\r\n";
		msg += "Status: " + status + "<br>\r\n";
		msg += "Note:<br>\r\n";
		msg += HTMLDetails(note);
		msg += "<br><br>\r\n\r\n";
		msg += "More details on BugReport page : ";
		msg += "http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/admin/dev_task.aspx?finished=1&id=" + m_id + "&r=" + r;
		msgMail.Body = msg;

		SmtpMail.Send(msgMail);
		
		//back to list
		m_id = "";
		m_noteid = "";
	}
	else if(owner != owner_old)
	{
		string email = TSGetUserEmailByID(owner);
		if(email == "")
			return true;

		MailMessage msgMail = new MailMessage();
		msgMail.To = email;
		msgMail.From = Session["email"].ToString(); //"webmaster@eznz.com";
		msgMail.Subject = "Bug/Task Assignment";
		msgMail.BodyFormat = MailFormat.Html;

		string uname = TSGetUserNameByID(owner);
		string sname = "";
		for(int i=0; i<uname.Length; i++)
		{
			if(uname[i] == ' ')
				break;
			sname += uname[i];
		}
		string msg = "Dear " + sname + ",<br>\r\n";
		msg += Session["name"].ToString() + " has assigned a task for you:<br>\r\n";
		msg += "Subject : " + subject;
		msg += "<br>\r\n";
		msg += "Status: " + status + "<br>\r\n";
		msg += "Note:<br>\r\n";
		msg += HTMLDetails(note);
		msg += "<br><br>\r\n\r\n";
		msg += "More details on Task page : ";
		msg += "http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/admin/dev_task.aspx?id=" + m_id + "&r=" + r;
		msgMail.Body = msg;
		SmtpMail.Send(msgMail);
	}
	return true;
}

bool PrintTaskList()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["task"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?id=" + m_id + "&nid=" + m_noteid + "r=" + r;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<br><center><h3>Task List");
	if(m_owner != "")
		Response.Write(" for " + TSGetUserNameByID(m_owner));
	else
	{
		if(m_status != "")
			Response.Write(" - <font color=red>" + m_status.ToUpper() + "</color>");
		else
			Response.Write(" - <font color=red>CURRENT ACTIVE</color>");
	}
	Response.Write("</h3></center>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<form name=f action=dev_task.aspx method=post>");
	Response.Write("<tr><td colspan=4>");
	Response.Write("<input type=text name=kw value='" + Session["dev_task_search_kw"] + "'>");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.kw.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<input type=submit name=cmd " + Session["button_style"] + " value=Search>");
	Response.Write("</td>");
	Response.Write("</form>");

	Response.Write("<td colspan=4 align=right>");
	Response.Write("<img src=r.gif> <a href=dev_task.aspx?r=" + DateTime.Now.ToOADate() + " class=o>Active</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=dev_task.aspx?status=finished&finished=1&r=" + DateTime.Now.ToOADate() + " class=o>Finished</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=dev_task.aspx?status=deffered&r=" + DateTime.Now.ToOADate() + " class=o>Deffered</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=dev_task.aspx?status=testing&r=" + DateTime.Now.ToOADate() + " class=o>Testing</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=dev_task.aspx?status=all&r=" + DateTime.Now.ToOADate() + " class=o>All</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=dev_task.aspx?t=a&r=" + r + " class=o>Add New Task</a>");
	Response.Write("</td></tr>");

	Response.Write("<tr style='color:white;background-color:#666696;font-weight:bold;'>\r\n");
	Response.Write("<th>Founder</th><th>Date</th><th>TASK ID</th><th>Priority</th><th>Desc</th><th>Owner</th><th>Status</th><th>Link</th></tr>");

	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["task"].Rows[i];
		string taskID = dr["id"].ToString();
		string status = dr["status"].ToString();
		string ownerID = dr["owner"].ToString();
		string founderID = dr["founder"].ToString();
		string founder = TSGetUserNameByID(founderID);
		string owner = TSGetUserNameByID(ownerID);
		string create_time = DateTime.Parse(dr["create_time"].ToString()).ToString("hh:mm dd/MM/yyyy");

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a href=dev_task.aspx?founder=" + founderID + "&r="+r + ">" + founder + "</a></td>");
		Response.Write("<td>" + create_time + "</td>");
		Response.Write("<td align=center>" + taskID + "</td>");
		Response.Write("<td>" + dr["priority"].ToString() + "</td>");
		Response.Write("<td>" + dr["subject"].ToString() + "</td>");
		Response.Write("<td><a href=dev_task.aspx?owner=" + ownerID + "&r="+r + ">" + owner + "</a></td>");
		Response.Write("<td>" + dr["status"].ToString() + "</td>");
		Response.Write("<td><a href=dev_task.aspx?id=" + taskID);
		if(status == "finished")
			Response.Write("&finished=1");
		Response.Write("&r="+r + ">Details</a></td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=8>");
	Response.Write(sPageIndex);
	Response.Write("</td></tr>");
	Response.Write("</table>");
	return true;
}

bool PrintDetails()
{
	if(ds.Tables["task"].Rows.Count <= 0)
		return false;

	DataRow dr = ds.Tables["task"].Rows[0];
	string ownerId = dr["owner"].ToString();
	string founderId = dr["founder"].ToString();

	Response.Write("<br><center><h3>Task ID " + dr["id"].ToString() + "</h3></center>");
	Response.Write("<table align=center border=1>");
	Response.Write("<tr><td colspan=2 align=right><a href=dev_task.aspx?t=a&r=" + r + " class=o>Add New Task</a></td></tr>");

	Response.Write("<tr><td valign=top>");
		Response.Write("<table>");
		Response.Write("<tr><td><b>Founder</b></td>");
		Response.Write("<td>" + TSGetUserNameByID(founderId) + "</td></tr>");

		Response.Write("<tr><td><b>Current Owner</b></td>");
		Response.Write("<td>" + TSGetUserNameByID(ownerId) + "</td></tr>");

		Response.Write("<tr><td><b>Status</b></td><td>");
		Response.Write(dr["status"].ToString());
		Response.Write("</td></tr>");

		Response.Write("</table>");
	Response.Write("</td><td valign=top>");
		Response.Write("<table>");
		Response.Write("<tr><td><b>Date Created</b></td><td>");
		Response.Write(dr["create_time"].ToString());
		Response.Write("</td></tr>");
		Response.Write("<tr><td><b>Date Assigned</b></td><td>");
		Response.Write(dr["assign_time"].ToString());
		Response.Write("</td></tr>");

		Response.Write("<tr><td><b>Estimate Time</b></td><td>");
		Response.Write(dr["time_need"].ToString());
		Response.Write("</td></tr>");

		Response.Write("<tr><td><b>Priority</b></td><td>");
		Response.Write(dr["Priority"].ToString());
		Response.Write("</td></tr>");

		Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Description</b></td><td>");
	Response.Write(dr["subject"].ToString());
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Related URL</b></td><td>");
	Response.Write(HTMLDetails(dr["url"].ToString()));
	Response.Write("</td></tr>");

//	Response.Write("<tr><td colspan=2><b>Details</b></td></tr>");

	Response.Write("<tr><td colspan=2>");
	PrintTaskNotes();
	Response.Write("</td></tr>");

	Response.Write("<tr><td><a href=dev_task.aspx?r=" + r + " class=o>Show All Tasks</a>");
	Response.Write("<td align=right><a href=dev_task.aspx?t=e&id=" + m_id + "&nid=" + m_noteid);
	Response.Write("&r=" + r + " class=o>Edit Task</a></td></tr>");
	Response.Write("</table>");
	return true;
}

void PrintTaskNotes()
{
	string sc = "<table width=100% cellspacing=0 cellpadding=0>";
	for(int i=0; i<ds.Tables["task_note"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["task_note"].Rows[i];
		string note_id = dr["id"].ToString();
		string card_id = dr["card_id"].ToString();
		string note = dr["note"].ToString();
		string note_time = DateTime.Parse(dr["note_time"].ToString()).ToString("HH:mm dd-MM-yyyy");
		string name = TSGetUserNameByID(dr["card_id"].ToString());

		note = HTMLDetails(note);
//		sc += "<tr><td>===========================================================</td></tr>";
		sc += "<tr><td><b>" + note_time + " " + name + " wrote:</b></td></tr>";
		sc += "<tr><td>" + note + "</td></tr>";
		sc += "<tr><td>" + GetNoteImages(note_id) + "</td></tr>";
//		if(Session["card_id"].ToString() == card_id)
		{
			sc += "<tr><td align=right><a href=dev_task.aspx?t=enote&id=";
			sc += m_id + "&nid=" + note_id + " class=o>Edit Note</a>";

			sc += "&nbsp&nbsp;<a href=dev_task.aspx?t=attach&id=";
			sc += m_id + "&nid=" + note_id + " class=o>Attach Image</a></td></tr>";
		}
		sc += "<tr><td>=============================================================================<br></td></tr>";
		sc += "<tr><td>&nbsp;</td></tr>";
	}
	sc += "</table>";
	Response.Write(sc);
}

string HTMLDetails(string s)
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

string GetNoteImages(string id)
{
	string s = "";
	string path = Server.MapPath(".");
	path += "\\pt\\" + id;
	if(Directory.Exists(path))
	{
		DirectoryInfo di = new DirectoryInfo(path);
		foreach (FileInfo f in di.GetFiles("*.*")) 
		{
			string file = f.FullName;
			file = file.Substring(path.Length+1, file.Length-path.Length-1);
//DEBUG("path=", file);
			s += "<img src=./pt/" + id + "/" + file + ">";
		}
	}
	return s;
}

bool DoSearchTask()
{
	string kw = Request.Form["kw"];
	Session["dev_task_search_kw"] = kw;
	kw = EncodeQuote(kw);
	string sc = "SELECT DISTINCT t.* ";
	sc += " FROM dev_task t JOIN dev_task_note n ON n.task_id=t.id WHERE ";
	if(IsInteger(kw))
		sc += " t.id=" + kw;
	else
	{
		sc += " t.subject LIKE N'%" + kw + "%' OR t.url LIKE N'%" + kw + "%' OR n.note LIKE N'%" + kw + "%' ";
		sc += " ORDER BY t.last_updated DESC";
	}
	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "task");
		if(rows == 1)
		{
			string id = ds.Tables["task"].Rows[0]["id"].ToString();
			string status = ds.Tables["task"].Rows[0]["status"].ToString();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dev_task.aspx?id=" + id);
			if(status == "finished")
				Response.Write("&finished=1");
			Response.Write("&r=" + DateTime.Now.ToOADate() + "\">");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetTask()
{
	string sc = "SELECT * FROM dev_task WHERE status<>'closed' ";
	if(m_owner != "")
		sc += " AND owner=" + m_owner;
	if(m_founder != "")
		sc += " AND founder=" + m_founder;
	if(m_id != "")
		sc += " AND id=" + m_id;
	if(m_status != "" && m_status != "all")
		sc += " AND status='" + m_status + "' ";
	else if(m_id == "" && Request.QueryString["finished"] != "1" && m_status != "all")
		sc += " AND status<>'finished' AND status<>'deffered' ";
	sc += " ORDER BY last_updated DESC";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "task");
		if(rows <= 0)
		{
//			Response.Write("<br><center><h3>No Task Found</h3></center>");
//			LFooter.Text = m_sAdminFooter;
//			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(m_id != "")
	{
		sc = "SELECT * FROM dev_task_note WHERE task_id=" + m_id + " ORDER BY note_time";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(ds, "task_note");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}

void PrintAttachForm()
{
	Response.Write("<center><br><h3>Attachment</h3>");
	ShowAllExists();
	Form1.Visible = true;
}

bool ShowAllExists()
{
	string strPath = Server.MapPath(".") + "\\pt\\" + m_noteid;
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
		string imgsrc = "./pt/" + m_noteid + "/" + file;
//		if(n == 0 || n == 3 || n == 6 || n == 9)
		sb.Append("</tr><tr>");
		sb.Append("<td valign=bottom><table><tr><td colspan=2><img src=" + imgsrc);
//		if(im.Width > 250)
//			Response.Write(" width=250");
		sb.Append(" border=0></td></tr>");
		sb.Append("<tr><td>" + im.Width.ToString() + "x" + im.Height.ToString() + " " + (f.Length/1000).ToString() + "K ");
		if(f.Length > 20480)
			sb.Append(" <font color=red> * big file * </font>");
		sb.Append("</td>");
		sb.Append("<td align=right><a href=dev_task.aspx?t=da&id=" + m_id + "&nid=" + m_noteid + "&file=" + HttpUtility.UrlEncode(file));
		sb.Append(" class=o>DELETE</a></td></tr>");
		sb.Append("<tr><td>&nbsp;</td></tr></table></td>");
		im.Dispose();
		n++;
	}
	sb.Append("</tr></table>");
	sb.Append("<input type=button onclick=window.location=('dev_task.aspx?id=" + m_id + "&nid=" + m_noteid + "&r=" + r + "') value='Cancel'>");
	LOldPic.Text = "<b>" + n.ToString() + " Images Already Attached</b>";
	LOldPic.Text += sb.ToString();
	if(n > 0)
		return true;
	return false; //no pic
}

void DoDelPic()
{
	string strPath = Server.MapPath(".") + "\\pt\\" + m_noteid;
	string file = strPath + "\\" + Request.QueryString["file"];
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
			string strPath = Server.MapPath(".") + "\\pt\\" + m_noteid;
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += "\\";
			string purePath = strPath;
			strPath += strFileName;
			
//DEBUG("pathname=", strPath);
			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dev_task.aspx?id=" + m_id + "&nid=" + m_noteid + "&r=" + r + "\">");
		}
	}
	return;
}

void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}

bool PrintEditNoteForm()
{
	string sc = "SELECT note FROM dev_task_note WHERE id=" + m_noteid;
	string note = "";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "note") == 1)
		{
			note = ds.Tables["note"].Rows[0]["note"].ToString();
		}
		else
		{
			Response.Write("<br><br><center><h3>Failed getting note id, " + m_noteid);
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<br><center><h3>Edit Note</h3>");
	Response.Write("<form action=dev_task.aspx?t=un&id=" + m_id + "&nid=" + m_noteid + "&r=" + r + " method=post>");
	Response.Write("<textarea name=note cols=90 rows=20>");
	Response.Write(note);
	Response.Write("</textarea><br>");
	Response.Write("<input type=submit name=cmd value=Save>");
	Response.Write("</form>");

	return true;
}

bool UpdateTaskNote()
{
	string note = Request.Form["note"];
	note = EncodeQuote(note);
	note += "\r\n\r\n<font color=red><i>Last edited by " + Session["name"].ToString();
	note += " at " + DateTime.Now.ToString("HH:mm dd-MM-yyyy") + "</i></font>";
	
	string sc = "UPDATE dev_task_note SET note = N'" + note + "' WHERE id = " + m_noteid;
	sc += " UPDATE dev_task SET last_updated=GETDATE() WHERE id = " + m_id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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
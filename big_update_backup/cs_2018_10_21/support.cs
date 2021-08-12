<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

int m_rows = 0;
bool m_bNoJob = false;
bool m_bHideAppend = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	initializeData();
	
	int rNumber = int.Parse(GetSiteSettings("support_id").ToString());

	if(!SecurityCheck("technician"))
		return;
	if(Request.Form["cmd"] == "Finish")
	{
		if(!doInsertSupportJob())
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+" \">");
	}
	if(Request.Form["cmd"] == "Append This Log")
	{
		if(!doAppendOldLog())
			return;
	}
	string search = "";
		string job_id = "";
	if(Request.Form["cmd"] == "Search")
	{
		if(Request.Form["search"] != null && Request.Form["search"] != "")
			search = Request.Form["search"];
		if(Request.Form["support_id"] != null && Request.Form["support_id"] != "")
			job_id = Request.Form["support_id"];
		if(!doSearch(search, job_id))
			return;
	}
	if(Request.QueryString["id"] != null)
	{
		if(!doSearch(search, job_id))
			return;
	}
	if(!GetNextSupport_ID(rNumber))
		return;
	if(Request.QueryString["cst_id"] != null && Request.QueryString["cst_id"] != "" || 
		(Request.Form["cmd"] == "Search Customer" || Request.Form["cust_search"] != null && Request.Form["cust_search"] != ""))
	{
		if(!doSearchCustomer())
			return;
		displayAllCustomer();
	}
	else
		displaySearchJob();
	
}

bool doAppendOldLog()
{

	string support_id = Request.Form["hide_support_id"];
	string log = "";
	if(Request.Form["append"] != null && Request.Form["append"] != "")
		log = msgEncode(Request.Form["append"]);
	string card_id = Request.Form["hide_card_id"];
	//DEBUG("log = ", log);
	//DEBUG("card = ", card_id);
	//DEBUG("support = ", support_id);
	if(log != null && log != "")
	{
		string sc = "";
		sc = " SET DATEFORMAT dmy ";
		sc += " INSERT INTO support_log (support_id, staff, request_date, card_id, finish_time, problem)";
		sc += " VALUES( '"+ support_id +"', '"+ Session["card_id"].ToString() +"' ";
		sc += " , GetDate(), '"+ card_id +"', GETDATE(), '"+ log +"' ";
		sc += " ) ";
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
	}
	Session["sp_card_id"] = "";
	Session["sp_company"] = "";
	return true;
}
//copy from bb.c to decode the message with special char
string msgEncode(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += "\'\'"; //double it for SQL query
		else if(s[i] == '(')
			ss += '[';
		else if(s[i] == ')')
			ss += ']';
		else if(s[i] == '<')
			ss += '[';
		else if(s[i] == '>')
			ss += ']';
		else if(s[i] == '*')
			ss += '-';
		else if(s[i] == '.')
			ss += '.';
		else if(s[i] == '~')
			ss += '~';
		else if(s[i] == '`')
			ss += '`';
		else
			ss += s[i];
	}
	return ss;
}

bool doInsertSupportJob()
{

	string problem = "";
	if(Request.Form["problem"] != null && Request.Form["problem"] != "")
		problem = msgEncode(Request.Form["problem"]);
	//DEBUG("problem = ", problem);
		
	if(Session["sp_card_id"] != "" && Session["sp_card_id"] != "")
	{
		string sc = "";
		sc = " SET DATEFORMAT dmy ";
		sc += " INSERT INTO support_log (support_id, staff, request_date, card_id, finish_time, problem)";
		sc += " VALUES( '"+ Session["support_id"] +"', '"+ Session["card_id"].ToString() +"' ";
		sc += " , GetDate(), '"+ Session["sp_card_id"] +"', GETDATE(), '"+ problem +"' ";
		sc += " ) ";

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
	}

	Session["sp_card_id"] = "";
	Session["sp_company"] = "";
	
	return true;
}

bool doSearch(string card_id, string job_id)
{
	string sc = "";
	
	sc = " SELECT TOP 15 sl.id, sl.problem, sl.request_date, sl.finish_time";
	sc += " , sl.support_id, c1.name, c1.company, c1.address1, c1.trading_name, c1.contact, c1.fax";
	sc += " ,c1.address2, c1.phone, c1.email, c2.name AS staff , c1.id AS card_id ";
	sc += " FROM support_log sl LEFT OUTER JOIN card c1 ON c1.id = sl.card_id ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = sl.staff ";
	sc += " WHERE 1=1 ";
	if(card_id != null && card_id != "")
	{
		if(TSIsDigit(card_id))
		{
			sc += " AND sl.card_id = "+ card_id;
			sc += " OR c1.phone LIKE '%"+ card_id +"%' ";
		}
		else
		{
			sc += " AND c1.phone LIKE '%"+ card_id +"%' OR c1.name LIKE '%"+ card_id +"%' ";
			sc += " OR c1.email LIKE '%"+ card_id +"%' OR c1.company LIKE '%"+ card_id +"%' ";
			sc += " OR c1.trading_name LIKE '%"+ card_id +"%' OR c1.contact LIKE '%"+ card_id +"%' ";
		}
	}
	if(job_id != "" && job_id != null)
	{
		if(TSIsDigit(job_id))
			sc += " AND sl.support_id = "+ job_id +"";
			
	}

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		if(TSIsDigit(Request.QueryString["id"]))
			sc += " AND sl.id = "+ Request.QueryString["id"];

	sc += " ORDER BY finish_time DESC ";
	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_rows = myCommand.Fill(dst, "support");
			
	//DEBUG("rows=", m_rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(m_rows > 0)
	{
		if(!m_bNoJob)
		{
			Session["sp_card_id"] = dst.Tables["support"].Rows[0]["card_id"].ToString();
			Session["sp_job_id"] = dst.Tables["support"].Rows[0]["id"].ToString();
		}
	}

	return true;
}

bool doSearchCustomer()
{
	string sc = "SELECT Top 60 id, name, trading_name, company, address1, address2, city, phone, fax, email";
	sc += " FROM card WHERE 1=1 ";
	string card_id = "";
	if(Request.QueryString["cst_id"] != "all" && Request.QueryString["cst_id"] != null && Request.QueryString["cst_id"] != "")
	{
		if(Request.QueryString["cst_id"] != null && Request.QueryString["cst_id"] != "")
			card_id = Request.QueryString["cst_id"];
		if(TSIsDigit(card_id))
		{
			sc += " AND id = "+ card_id;
			sc += " OR phone LIKE '%"+ card_id +"%' ";
		}
		else
		{
			sc += " AND phone LIKE '%"+ card_id +"%' OR name LIKE '%"+ card_id +"%' "; 
			sc += " OR company LIKE '%"+ card_id +"%' OR email LIKE '%"+ card_id +"%' "; 
			sc += " OR fax LIKE '%"+ card_id +"%' OR trading_name LIKE '%"+ card_id +"%' "; 
		}

	}	
	if(Request.Form["cmd"] == "Search Customer" || Request.Form["cust_search"] != null && Request.Form["cust_search"] != "")
	{
		string search = "";
		if(Request.Form["cust_search"] != null && Request.Form["cust_search"] != "")
			card_id = Request.Form["cust_search"];
		//DEBUG("card = ", card_id);
		if(TSIsDigit(card_id))
		{
			sc += " AND id = "+ card_id;
			sc += " OR phone LIKE '%"+ card_id +"%' ";
		}
		else
		{
			sc += " AND phone LIKE '%"+ card_id +"%' OR name LIKE '%"+ card_id +"%' "; 
			sc += " OR company LIKE '%"+ card_id +"%' OR email LIKE '%"+ card_id +"%' "; 
			sc += " OR fax LIKE '%"+ card_id +"%' OR trading_name LIKE '%"+ card_id +"%' "; 
		}
	}
	
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "customer");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows == 1)
	{
		Session["sp_card_id"] = dst.Tables["customer"].Rows[0]["id"].ToString();
		Session["sp_company"] = dst.Tables["customer"].Rows[0]["company"].ToString();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+" \">");
		return false;
	}
	return true;
}
void displayAllCustomer()
{
	Response.Write("<form method=post name=frm>");
	Response.Write("<table width=85% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th align=left colspan=3>Search Customer By: ID, Phone, Name, and etc...<input type=text name=cust_search value=''></th>");
	Response.Write("<td colspan=2><input type=submit name=cmd value='Search Customer'"+Session["button_style"]+">");
	Response.Write("&nbsp;|&nbsp;<a title='Add New Customer'  href=\"javascript:newcard_window=window.open('ecard.aspx?a=new', '', ''); newcard_window.focus()\" class=o>Add New Customer</a>");
	Response.Write("</td></tr>");
	Response.Write("\r\n<script");
	Response.Write(">\r\ndocument.frm.cust_search.focus();\r\ndocument.frm.cust_search.select();\r\n</script");
	Response.Write(">\r\n");
	Response.Write("<tr></tr>");
	Response.Write("<tr></tr>");
	Response.Write("<tr bgcolor=#EEEEE><th>ID</th><th>Name</th><th>Company</th><th>Phone</th><th>Email</th></tr>");
	bool bAlter = true;
	for(int i=0; i<dst.Tables["customer"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["customer"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string company = dr["company"].ToString();
		string addr1 = dr["address1"].ToString();
		string addr2 = dr["address2"].ToString();
		string city = dr["city"].ToString();
		string phone = dr["phone"].ToString();
		string fax = dr["fax"].ToString();
		string email = dr["email"].ToString();

		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		string sLink = "support.aspx?cst_id="+id;
		Response.Write(">");
		Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ id +"</a></td>");
		Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ name +"</td>");
		Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ company +"</td>");
		Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ phone +"</td>");
		Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ email +"</td>");
	
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");
}

bool GetNextSupport_ID(int rNumber)
{
	
	if(dst.Tables["insert_support"] != null)
		dst.Tables["insert_support"].Clear();

	//DEBUG("rnumber = ", rNumber);
	string sc = "SELECT TOP 1 support_id FROM support_log ORDER BY id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "insert_support") == 1)
		{
			if(dst.Tables["insert_support"].Rows[0]["support_id"].ToString() != null && dst.Tables["insert_support"].Rows[0]["support_id"].ToString() != "")
				rNumber = int.Parse(dst.Tables["insert_support"].Rows[0]["support_id"].ToString()) + 1;
			else
				rNumber = rNumber + 1;
		}
		Session["support_id"] = rNumber;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

void displaySearchJob()
{
	string bcolor="#EEEEEE";	//DFACCC
	Response.Write("<center><h4>Customer Support Section</h4></center>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table width=85% align=center cellspacing=0 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><th bgcolor="+ bcolor +" align=right>Search Customer : </td><td><input type=text name=search value=''></td></tr>");
	Response.Write("<tr><th bgcolor="+ bcolor +" align=right>Search Support JOB# : </td><td><input type=text name=support_id value=''>");
	Response.Write("<input type=submit name=cmd value='Search' "+Session["button_style"] +" onclick='return chkSearch()'></td></tr>");
	Response.Write("<tr></tr>");
	Response.Write("<tr></tr>");
	
	DataRow dr;
	string uri = "support.aspx";
	if(m_rows > 0)
	{
		string card_id = "", company = "", trading_name = "", name = "",  city = ""; 
		string phone = "", fax = "", addr1 = "", addr2 = "", email;
		string support_id = "", problem = "", finish_time = "", staff = "", id = "";
		//Response.Write("<tr>Service Log</tr>");
		Response.Write("<tr><td colspan=2>");
		Response.Write("<table width=90% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=gray bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor="+ bcolor +" ><th>SUPPORT#</th><th align=left>NAME</th><th>FINISH TIME</th>");
		Response.Write("<th>Problem</th><th>Service Staff</th></tr>");
		if(!m_bNoJob)
		{
			bool bAlter = false;
			for(int i=0; i<m_rows; i++)
			{
				dr = dst.Tables["support"].Rows[i];
				card_id = dr["card_id"].ToString();
				name = dr["name"].ToString();
				if(name == "")
					name = dr["company"].ToString();
				company = dr["company"].ToString();
				problem = dr["problem"].ToString();
				finish_time = dr["finish_time"].ToString();
				support_id = dr["support_id"].ToString();
				id = dr["id"].ToString();
				staff = dr["staff"].ToString();
				Response.Write("<tr");
				if(bAlter)
					Response.Write(" bgcolor=#EEEEE");
				Response.Write(">");
				bAlter = !bAlter;
				if(Request.QueryString["id"] == null)
				{
					uri = "support.aspx?id="+ id +"";
					m_bHideAppend = true;
				}
				if(Request.QueryString["id"] != null)
				{
					uri = "support.aspx";
					m_bHideAppend = false;
				}
				Response.Write("<td align=center><a title='View and Append This Job' href='"+ uri +"' class=o>"+ support_id +"</a></td>");
				Response.Write("<th align=left><a title='View Customer Details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["sp_card_id"] + "', '',");
				Response.Write("'width=350, height=350'); viewcard_window.focus()\" class=o><font color=Green>"+ name +"</font></a></td>");
				//Response.Write("<td><a title='View and Append This Job' href='"+ uri +"' class=o>"+ finish_time +"</a></td>");
				Response.Write("<td>"+ finish_time +"</td>");
				Response.Write("<td><a title='View and Append This Job' href='"+ uri +"' class=o>"+ problem +"</a></td>");
				Response.Write("<td>"+ staff +"</td>");
				Response.Write("</tr>");
				if(Request.QueryString["id"] != null)
				{
					Response.Write("<input type=hidden name=hide_support_id value='"+ support_id +"'>");
					Response.Write("<input type=hidden name=hide_card_id value='"+ card_id +"'>");
					Response.Write("<tr><th>Customer\'s <br>Problem</th><td colspan=4><textarea cols=70 rows=7 name=append value=''></textarea>");
					Response.Write("<input type=submit name=cmd value='Append This Log'"+ Session["button_style"] +">");
					Response.Write("</td></tr>");
				}
			}
			m_bHideAppend = !m_bHideAppend;
		}
		else
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?cst_id=all \">");
		}
		Response.Write("</table>");
		Response.Write("</td></tr>");
	}
		
	Response.Write("<tr></tr>");
	Response.Write("<tr></tr>");
	Response.Write("<tr><th bgcolor="+ bcolor +" colspan=2>NEW JOB# "+Session["support_id"]+"</td></tr>");
	Response.Write("<tr><th bgcolor="+ bcolor +" align=right>Today\'s Date : </td><td>"+DateTime.Now.ToString("dd.MMM.yyyy")+"</td></tr>");
	Response.Write("<tr><th bgcolor="+ bcolor +" align=right>Customer Details : </th><td>");
	
	//if(Session["sp_card_id"] == null || Session["sp_card_id"] == "")
		Response.Write("<select name=slt_cust onclick=\"window.location=('support.aspx?cst_id=all')\" >\r\n"); //'+ this.options[this.selectedIndex].value)\" >\r\n");
	//else
	//	Response.Write("<select name=slt_cust onchange=\"window.location=('support.aspx?cst_id='+ this.options[this.selectedIndex].value)\" >\r\n");

	if(Session["sp_company"] != null)
		Response.Write("<option value='"+ Session["sp_card_id"] +"' selected>"+ Session["sp_company"] +"</option>");
	
	//Response.Write("<option value='all'>All");
	Response.Write("</select>");
	Response.Write("|&nbsp;<a title='click to view Customer Details'  href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["sp_card_id"] + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>who?</a>");
	Response.Write("&nbsp;|&nbsp;<a title='Add New Customer'  href=\"javascript:newcard_window=window.open('ecard.aspx?a=new', '', ''); newcard_window.focus()\" class=o>Add New Customer</a>");
	Response.Write("</td></tr>");
	Response.Write("<tr><th bgcolor="+ bcolor +" align=right>PROBLEMS :</td><td><textarea cols=70 rows=15 name='problem'></textarea></td></tr>");
	Response.Write("<tr><th colspan=2><input type=submit name=cmd onclick=\"return chkProblem(); \" ");
	Response.Write(" value='Finish'"+ Session["button_style"] +"></td><tr>");

	Response.Write("</table></form>");
	Response.Write("<script language=javascript>");
	Response.Write("<!---hide from old browser");
	string s = @"
		function chkSearch()
		{
			if(document.frm.search.value =='' && document.frm.support_id.value =='')
			{
				window.alert('Please Enter Search ID!!\r\nSearch By: \r\nPhone\r\nCustomer Name\r\nCompany\r\nCustomer ID\r\n ');
				document.frm.search.focus();
				document.frm.search.select();
				return false;
			}
		
		}
		function chkProblem()
		{
			if(document.frm.problem.value == ''){
				window.alert('Please Enter Problem Details!!');
				document.frm.problem.focus();
				document.frm.problem.select();
				return false;
			}
			else
			{
				return window.confirm('Do you Want to Finish This Job????');
			}
			return true;
		}
		
	";
	Response.Write("--->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");

	
}

</script>

<asp:Label id=LFooter runat=server/>

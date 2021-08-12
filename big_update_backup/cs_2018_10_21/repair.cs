<!-- #include file="page_index.cs" -->
<!-- #include file="isdate.cs" -->
<script runat="server">
string m_selected = "1";  //selected value 

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string repair_warranty_note = "";
string term_condition = "";
string m_uri = "";

string m_current_sn = "";
string m_current_code = "";
string m_current_inv = "";
string m_current_inv_date = "";
string m_current_desc = "";
string m_current_fault = "";
string m_current_supplier_id = "";
string m_current_supplier_code = "";

string ra_conditions = "";
string ra_header = "";
string ra_packslip = "";
string m_sEmail = "";
string m_repair_form_template = "";
string company_id = "";
string m_search = "";
string m_command = "";
string m_querystring = "";

bool bHide = true;
bool m_bIsFound = false;  //flag for searching sn on the database
bool m_bIsFirst = true;
bool m_bVerified = false; // verified for public site

//----- m_querystring attribute ---//
//----- if m_querystring == ip ***** input faulty items
//----- if m_querystring == view ***** dealer, public site view ra list after apply ra
//----- if m_querystring == cr ******then create new ra number at admin site
//----- if m_querystring == all ****** show all created ra number on admin site
//---------------------------------------//

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(Session["card_id"] == null || Session["card_id"] == "")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dealer \">");
		return;
	}
	GetAllQueryString();
	
//	repair_warranty_note = ReadSitePage("ra_warranty_note");
//	ra_header = ReadSitePage("repair_header");
//	ra_conditions = ReadSitePage("repair_condition");
//	ra_packslip = ReadSitePage("repair_pickup_slip");
	
	if(Request.QueryString["ty"] == "1")
		company_id = GetSiteSettings("company_id_for_repair_form", GetCompanyCardID(GetEnumID("card_type","others")));
	if(Request.QueryString["ty"] == "2")
		company_id = GetSiteSettings("company_id_for_repair_finish_form", GetCompanyCardID(GetEnumID("card_type","others")));
	//string company_id_finish = GetSiteSettings("company_id_for_repair_finish_form", GetCompanyCardID(GetEnumID("card_type","others")));
	//if(Request.QueryString["ty"] == "2")
	//	company_id = company_id_finish;
//DEBUG("companyid=", company_id);
	m_repair_form_template = ReadRATemplate(company_id);
//DEBUG("m_repair_form_templat= ", m_repair_form_template);
	if(m_command.ToLower() == "accept agreement")
	{
		if(Request.Form["agree"] == "on")
		{
			Session["bAgree"] = "1";
			m_bVerified = true;
		}
	}
//DEBUG("m_bVerified = ", m_bVerified.ToString());
	if(m_sSite.ToLower() != "admin" && Request.QueryString["s"] != "view" && Request.QueryString["print"] != "form")
	{
		if(Session["bAgree"] == null || Session["bAgree"] == "")
		{
			InitializeData();
			Response.Write("<center>"+ ReadSitePage("repair_condition"));
			//Session["bVerified"] = "true";
			Response.Write("<br><center><form name=f method=post><input type=checkbox name=agree ><input type=submit name=cmd value='Accept Agreement'"+ Session["button_style"] +"></form><br><br><br>");
			return;
		}
	}
	if(Request.QueryString["res"] != null && Request.QueryString["res"] != "")
	{
		if(DoTemporyDeleteRepair())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?ra=all&r="+ DateTime.Now.ToOADate() +" \">");
			return;
		}
	}
	if(Request.QueryString["radel"] != null && Request.QueryString["radel"] != "")
	{
		if(DoTemporyDeleteRepair())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?ra=all&r="+ DateTime.Now.ToOADate() +" \">");
			return;
		}
	}
	if(m_command == "Generate RMA Number")
	{
		InitializeData();
//		Response.Write("<form name=frm method=post>");	
		if(DoGenerateRANumber())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?nra=done \">");
			return;
		}
		return;
	}
	//if(m_querystring.ToLower() == "view")
	if(Request.QueryString["s"] == "view")
	{
		InitializeData();
		if(!getRepairDetails())
				return;
		displayRA_Item();
		return;
	}
	if(m_command.ToLower() == "request rma#")
	{
		
		if(DoInsertRAItem())
		{
			SessionCleanUp();
		
			//refresh to admin site:
			if(m_sSite.ToLower() == "admin")
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='techr.aspx?r=" + DateTime.Now.ToOADate() + "&op=0&s=0");
				Response.Write("\">");
				return;
			}
			else
			{
				autoSendMail();
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='repair.aspx?r=" + DateTime.Now.ToOADate() + "&s=view");
				Response.Write("\">");
				return;
			}
		
			return;
		}

	}
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		if(DoDeleteSessionItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "");
			if(m_querystring != "")
				Response.Write("&ra="+ m_querystring +"");
			if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
				Response.Write("&rid="+ HttpUtility.UrlEncode(Request.QueryString["rid"]) +"");
			if(Request.QueryString["ifra"] != null && Request.QueryString["ifra"] != "")
				Response.Write("&ifra="+ Request.QueryString["ifra"] +"");
			Response.Write("\">");
			return;
		}
		
	}
	if(m_command.ToLower() == "check sn#" || Request.Form["sn"] != null && Request.Form["sn"] != "")
	{
		if(!CheckFaultyItem())
			return;
	}
	if(m_command.ToLower() == "add this faulty item")
	{
		DoAddFaultyItemToSession();		
		//clean current inputted items
		m_current_sn = "";
		m_current_code = "";
		m_current_inv = "";
		m_current_inv_date = "";
		m_current_desc = "";
		m_current_fault = "";
		m_current_supplier_id = "";
		m_current_supplier_code = "";
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "&ra="+ m_querystring +"");
		if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
			Response.Write("&rid="+ HttpUtility.UrlEncode(Request.QueryString["rid"]) +"");
		if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
			Response.Write("&cid="+ Request.QueryString["cid"] +"");
		Response.Write("\">");
		return;
	}

	if(Request.QueryString["print"] == "form" && Request.QueryString["ra"] != null)  //print form
	{
		if(!getRepairDetails())
			return;
//		Response.Write(PrintDocket());
		m_sEmail = PrintDocket();
	
		if(Request.QueryString["email"] != null && Request.QueryString["email"] != "")
		{
			string email = Request.QueryString["email"];
			string ra_id = Request.QueryString["ra"];
			if(Request.QueryString["confirm"] == "1")
			{
				Response.Write("<script Language=javascript");
				Response.Write(">");
				Response.Write("if(window.confirm('");
				Response.Write("Email RMA#" + ra_id + " to " + email + "?         ");
				Response.Write("\\r\\n\\r\\n");
				Response.Write("\\r\\nClick OK to send.\\r\\n");
				Response.Write("'))");
				Response.Write("window.location='"+ Request.ServerVariables["URL"] +"?print=form&ra=" +ra_id + "&email=" + HttpUtility.UrlEncode(email) + "';\r\n");
				Response.Write("else window.close();\r\n");
				Response.Write("</script");
				Response.Write(">");
				return;
			}
			else
			{
				MailMessage msgMail = new MailMessage();
				msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
				msgMail.To = email;
				msgMail.Subject = "RMA#" + " " + ra_id + " - " + m_sCompanyTitle;
				msgMail.BodyFormat = MailFormat.Html;
				msgMail.Body = m_sEmail;
				SmtpMail.Send(msgMail);
			}
			Response.Write("<form name=frm onload='window.close()'>");
			Response.Write("<br><center><h3>RMA# " + ra_id + " Sent.</h3>");
			Response.Write("<input type=button value='Close Window' onclick=window.close() " + Session["button_style"] + ">");
			Response.Write("<br><br><br><br><br><br>");
			Response.Write("</from>");
			return;
		}	
		Response.Write(PrintDocket());
		return;
	}
	if(m_querystring == "all")  //show all created ra#
	{
		InitializeData();
		Response.Write("<form name=frm method=post>");
		QueryRANumberCreated();
		//clean inputted session
			SessionCleanUp();
		return;
	}

//	if(Request.QueryString["ra"] == "ip")  //input data form
	if(m_querystring == "ip")  //input data form
	{
		InitializeData();
		Response.Write("<form name=frm method=post>");	
		GetFaultyItems();
		Response.Write("<br><br><br><br>");
//		return;
	}
	if(m_sSite.ToLower() != "admin")
		return;

	if(m_querystring == "cr")  //create rma for admin site
	{
		InitializeData();
		Response.Write("<form name=frm method=post>");	
		GenerateRANumber();
		return;
	}


	if(Request.QueryString["nra"] == "done")  
	{
		InitializeData();
		if(Session["sRANumber"] == null || Session["sRANumber"] == "")
		{
			Response.Write("<br><br><center><h4>No RMA NUMBER CREATED </h4>");
			Response.Write("<br><br><h3><a title='go to ra list' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all' class=o>RA LIST</a>");
			return;
		}

		Response.Write("<br><br><center><h4>RMA Number is: </h4><h3><font color=red><b>"+ Session["sRANumber"] +"</h3>");
		Response.Write("<br><br><h4><a title='Print RMA Form' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra="+ HttpUtility.UrlEncode(Session["sRANumber"].ToString()) +"&print=form&ty=1' target=new class=o>Print RMA Form</a>");
		//Response.Write("<br><br><a title='Email RA Form to Customer' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra="+ HttpUtility.UrlEncode(Session["sRANumber"].ToString()) +"&email="+ Session["sCustomer_email"] +"&print=form' target=new class=o>Email RA Form to Customer</a>");
		Response.Write("<br><br><a title='Email RMA Form to Customer' href=\"javascript:if(confirm('Email to Customer??'))window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra="+ HttpUtility.UrlEncode(Session["sRANumber"].ToString()) +"&email="+ Session["sCustomer_email"] +"&print=form')\" target=_blank class=o>Email RMA Form to Customer</a>");
		Response.Write("<br><br><a title='Input Fault Items!!' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&rid="+ HttpUtility.UrlEncode(Session["sRANumber"].ToString()) +"&ra=ip&cid="+ Session["sCustomer_id"] +"'  class=o>Input Faulty Items</a>");
		Response.Write("<br><br><h4><a title='Go to RMA List' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all' target=new class=o>Go to RMA List</a>");
		
		return;
	}
	
	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "" || Request.QueryString["ci"] == "all")
	{	
		InitializeData();
		Response.Write("<form name=frm method=post>");
		if(!GetCardID())
			return;
		ShowCardInfor();
		return;
	}
	
	Response.Write("</form>");

}

bool DoAddFaultyItemToSession()
{
	string sn = Request.Form["sn"];
	string code = Request.Form["code"];
	string inv = Request.Form["inv"];
	string inv_date = Request.Form["inv_date"];
	string desc = Request.Form["desc"];
	string fault = Request.Form["fault"];
	string supplier_id = Request.Form["supplier_id"];
	string supplier_code = Request.Form["supplier_code"];
	
	int nSession = 0;
	bool IsDuplicate = false;
//DEBUG(" code = ", code);	
	int nCt = 0;
	int nlimit = 0;
	bool bSqlAttack = false;
	if(!CheckSQLAttack(sn) || !CheckSQLAttack(code) || !CheckSQLAttack(inv) || !CheckSQLAttack(desc) || !CheckSQLAttack(fault))
		bSqlAttack = true;

	if(bSqlAttack)
	{
		Response.Write("<script language=javascript>window.alert('Invalid Input, Please remove word with delete table, update table, create table');");
		Response.Write("window.location=('"+ Request.QueryString["URL"] +"?ra=ip&rid="+ HttpUtility.UrlEncode(Request.QueryString["rid"]) +"&r="+ DateTime.Now.ToOADate() +"');");
		Response.Write("</script");
		Response.Write(">");
		return false;
	}

	if(m_sSite.ToLower() == "admin")
		if(Session["ss_rp_count"] != null && Session["ss_rp_count"] != "")
			if((int)Session["ss_rp_count"] > int.Parse(GetSiteSettings("ra_qty_limit", "10")))
				return false;
	if(desc != null && desc != "" && fault != null && fault != "")
	{
		if(Session["ss_rp_count"] == null)
		{
			Session["ss_rp_count"] = 1;
			Session["ss_rp_sn1"] = sn;
			Session["ss_rp_code1"] = code;
			Session["ss_rp_inv1"] = inv;
			Session["ss_rp_inv_date1"] = inv_date;
			Session["ss_rp_desc1"] = desc;
			Session["ss_rp_fault1"] = fault;
			Session["ss_rp_supplier_id1"] = supplier_id;
			Session["ss_rp_supplier_code1"] = supplier_code;
			nSession = (int)Session["ss_rp_count"];
//DEBUG("value = ", Session["ss_rp_count"].ToString());
		}
		else
		{
			nSession = (int)Session["ss_rp_count"] + 1;		
			for(int i=1; i<=nSession; i++)
			{	
				if(Session["ss_rp_sn"+ i] != null && Session["ss_rp_sn"+ i] != "")
				{
					if(sn == Session["ss_rp_sn"+ i].ToString())
						IsDuplicate = true;
				}
			}
			if(!IsDuplicate)
			{
				Session["ss_rp_count"] = nSession;
				Session["ss_rp_sn"+ nSession] = sn;
				Session["ss_rp_code"+ nSession] = code;
				Session["ss_rp_inv"+ nSession] = inv;
				Session["ss_rp_inv_date"+ nSession] = inv_date;
				Session["ss_rp_desc"+ nSession] = desc;
				Session["ss_rp_fault"+ nSession] = fault;
				Session["ss_rp_supplier_id"+ nSession] = supplier_id;
				Session["ss_rp_supplier_code"+ nSession] = supplier_code;
			}
			else
			{
				Response.Write("<script language=javascript1.2>window.alert('Duplicate SN#, Please Try Again')</script");
				Response.Write(">");
			}

		}
	}
	
	return IsDuplicate;
}


bool CheckFaultyItem()
{
//	DEBUG("sn = ", Request.Form["sn"].ToString());
	DataRow dr;
	dr = CheckSN(Request.Form["sn"], "");
	if(dr != null)
	{
		m_current_sn = dr["sn"].ToString();
		m_current_inv_date = dr["commit_date"].ToString();
		m_current_desc = dr["name"].ToString();
		if(m_current_inv_date != "")
		{
			//if(IsDate(m_current_inv_date))
				m_current_inv_date = DateTime.Parse(m_current_inv_date).ToString("dd-MM-yyyy");
			//else
			//	m_current_inv_date = "";
		}
		if(m_current_desc != null && m_current_desc != "")
			m_current_desc = StripHTMLtags(m_current_desc);
		m_current_inv = dr["invoice_number"].ToString();
		if(m_current_inv == "")
			m_current_inv = dr["invoice_number2"].ToString();
		m_current_code = dr["code"].ToString();
		m_current_supplier_id = dr["supplier_id"].ToString();
		m_current_supplier_code = dr["supplier_code"].ToString();

	}
	else
	{
		m_current_sn = Request.Form["sn"];
		m_current_inv_date = Request.Form["inv_date"];
		m_current_desc = Request.Form["desc"];
		m_current_inv = Request.Form["inv"];
		m_current_code = Request.Form["code"];
//		m_current_supplier_id = dr["supplier_id"].ToString();
		m_current_supplier_code = Request.Form["supplier_code"];
		m_current_fault = Request.Form["fault"];	
	}

	return true;
}
bool DoDeleteSessionItem()
{
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		Session["ss_rp_sn"+ Request.QueryString["del"]] = null;
		Session["ss_rp_code"+ Request.QueryString["del"]] = null;
		Session["ss_rp_desc"+ Request.QueryString["del"]] = null;
		Session["ss_rp_fault"+ Request.QueryString["del"]] = null;
		Session["ss_rp_inv"+ Request.QueryString["del"]] = null;
		Session["ss_rp_inv_date"+ Request.QueryString["del"]] = null;
		Session["ss_rp_supplier_id"+ Request.QueryString["del"]] = null;
		Session["ss_rp_supplier_code"+ Request.QueryString["del"]] = null;
	}
	int nCt = 0;
	int nNumber = 0;
	if(Session["ss_rp_count"] != null && Session["ss_rp_count"] != "")
	{	
		nCt = (int)Session["ss_rp_count"];
		for(int i=1; i<=nCt; i++)
		{
			if(Session["ss_rp_desc"+ i] != null && Session["ss_rp_desc"+ i] != "" && Session["ss_rp_fault"+ i] != null && Session["ss_rp_fault"+ i] != "")
			{
				nNumber++;
//DEBUG("nnumber = ",nNumber);
				Session["ss_rp_sn"+ nNumber] = Session["ss_rp_sn"+ i];
				Session["ss_rp_code"+ nNumber] = Session["ss_rp_code"+ i];
				Session["ss_rp_desc"+ nNumber] = Session["ss_rp_desc"+ i];
				Session["ss_rp_fault"+ nNumber] = Session["ss_rp_fault"+ i];
				Session["ss_rp_inv"+ nNumber] = Session["ss_rp_inv"+ i];
				Session["ss_rp_inv_date"+ nNumber] = Session["ss_rp_inv_date"+ i];
				Session["ss_rp_supplier_id"+ nNumber] = Session["ss_rp_supplier_id"+ i];
				Session["ss_rp_supplier_code"+ nNumber] = Session["ss_rp_supplier_code"+ i];
			}
		}

		if(nNumber >= 1)
			Session["ss_rp_count"] = nNumber;
		else
			Session["ss_rp_count"] = null;
	}
	
	return true;
	
}

bool DoInsertRAItem()
{
	string ra_number = Request.QueryString["rid"];
	string customer_id = "";
	if(Session["sCustomer_id"] != null && Session["sCustomer_id"] != "")
		customer_id = Session["sCustomer_id"].ToString();
	if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		customer_id = Request.QueryString["cid"];
	string status = "3";
	if(m_sSite.ToLower() != "admin")
	{
		ra_number = GetNextRA_ID();
		customer_id = Session["card_id"].ToString();
		status = "1";
	}
	
	string sc = "";
	if(Session["ss_rp_count"] == null || Session["ss_rp_count"] == "")
		return false;
	
	for(int i=1; i<=(int)Session["ss_rp_count"]; i++)
	{
		if(Session["ss_rp_desc"+ i.ToString()] != null && Session["ss_rp_desc"+ i.ToString()] != "" && Session["ss_rp_fault"+ i.ToString()] != null && Session["ss_rp_fault"+ i.ToString()] != "")
		{
			sc = " BEGIN TRANSACTION ";	
			sc += "SET DATEFORMAT dmy ";
			
			if(m_sSite.ToLower() == "admin" && i == 1)
			{
				sc += " UPDATE repair SET serial_number = '"+ Session["ss_rp_sn"+ i.ToString()] +"' ";
				sc += ", invoice_number = '"+ Session["ss_rp_inv"+ i.ToString()] +"' ";
				sc += ", purchase_date = '"+ Session["ss_rp_inv_date"+ i.ToString()] +"' ";
				sc += ", status = "+ status +" ";
				sc += ", fault_desc = '"+ StripHTMLtags(EncodeQuote(Session["ss_rp_fault"+ i.ToString()].ToString())) +"' ";
				sc += ", repair_date = GETDATE() ";
				sc += ", code = '"+ Session["ss_rp_code"+ i.ToString()] +"' ";
				sc += ", supplier_code = '"+ Session["ss_rp_supplier_code"+ i.ToString()] +"' ";
				sc += ", prod_desc = '"+ StripHTMLtags(EncodeQuote(Session["ss_rp_desc"+ i.ToString()].ToString())) +"' ";
				sc += ", supplier_id = '"+ Session["ss_rp_supplier_id"+ i.ToString()] +"' ";
				sc += ", system = 0, isinput=1 ";

				sc += " WHERE ra_number = '"+ ra_number +"' ";
								
			}
			else
			{
				sc += " INSERT INTO repair (ra_number, serial_number, invoice_number, customer_id ";
				sc += ", status,  fault_desc, repair_date, code, supplier_code, prod_desc, supplier_id ";
				sc += ", system, purchase_date, isinput )";
				sc += " VALUES('"+ ra_number +"', '"+ Session["ss_rp_sn"+ i.ToString()] +"', '"+ Session["ss_rp_inv"+ i.ToString()] +"' ";
				sc += ", '"+ customer_id +"', "+ status +", '"+ StripHTMLtags(EncodeQuote(Session["ss_rp_fault"+ i.ToString()].ToString())) +"', GETDATE(), '"+ Session["ss_rp_code"+ i.ToString()] +"' ";
				sc += ",'"+ Session["ss_rp_supplier_code"+ i.ToString()] +"','"+ StripHTMLtags(EncodeQuote(Session["ss_rp_desc"+ i.ToString()].ToString()).ToString()) +"','"+ Session["ss_rp_supplier_id"+ i.ToString()] +"' ";
				sc += " , 0 ,'"+ Session["ss_rp_inv_date"+ i.ToString()] +"', 1 ";
				sc += ")";
			}
			
			//update item status, set to rma status
			if(Session["ss_rp_sn"+ i.ToString()] != null && Session["ss_rp_sn"+ i.ToString()] != "")
			{
				sc += " UPDATE stock SET status = 4 WHERE sn = '"+ Session["ss_rp_sn"+ i.ToString()] +"' ";
				if(Session["ss_rp_inv"+i] != null && Session["ss_rp_inv"+ i] != "")
					if(!TSIsDigit(Session["ss_rp_inv"+i].ToString()))
						Session["ss_rp_inv"+ i] = "";

				sc += AddSerialLogString(Session["ss_rp_sn"+ i.ToString()].ToString(), "Item Request for Repair: "+ StripHTMLtags(EncodeQuote(Session["ss_rp_desc"+ i.ToString()].ToString())) +"", "", (Session["ss_rp_inv"+ i.ToString()]).ToString(), ra_number, "");
			}
			sc += " COMMIT ";
//	DEBUG(" sc = ", sc);
//	return false;
	
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
	}
	
	return true;
}
//get faulty items from all sites,
void GetFaultyItems()
{ 
    string CompanyName = GetSiteSettings("company_name");
	Response.Write("<form name=frm method=post>");
	//Response.Write("<br><center><h4>"+ m_sCompanyName.ToUpper() +" RMA APPLICATION </h4>");
	Response.Write("<br><center><h4>"+ CompanyName.ToUpper() +" RMA APPLICATION </h4>");
	if(m_sSite.ToLower() == "admin")
	{
		if(Request.QueryString["rid"] == null || Request.QueryString["rid"] == "")
		{
			Response.Write("<br><h4>NO RMA#!! Please Go to <a href='"+ Request.ServerVariables["URL"] +"?ra=cr&r="+ DateTime.Now.ToOADate() +"' class=o>Create New RMA#</a></h4>");
			Response.Write("<br><h4>OR Go to <a href='"+ Request.ServerVariables["URL"] +"?ra=all&r="+ DateTime.Now.ToOADate() +"' class=o>Created RMA# List</a></h4>");
			return;
		}
		Response.Write("<h4><center>For Customer: <a title='view customer details' ");
		Response.Write(" href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["sCustomer_id"] + "', '',");
		Response.Write("'width=350, height=350'); viewcard_window.focus()\" class=o><font color=Green>");

		Response.Write(""+ Session["sCustomer_name"] +"</a></font>  On RMA#: "+ Request.QueryString["rid"] +"</h4>");
		
	}
	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td>");
	Response.Write("<table align=center  cellspacing=0 cellpadding=0 border=0 bordercolor=#CCCCCC ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	//hidden value
	Response.Write("<input type=hidden name=supplier_id value='"+ m_current_supplier_id +"'>");
	Response.Write("<input type=hidden name=supplier_code value='"+ m_current_supplier_code +"'>");

	Response.Write("<tr><th align=left>SN#:");
	Response.Write("</th><td><input size=42 type=text name=sn value='"+ EncodeQuote(m_current_sn) +"'>");
	Response.Write("<script language=javascript1.2>document.frm.sn.focus();</script");
	Response.Write(">");
	Response.Write("<input type=submit name=cmd value='Check SN#'"+ Session["button_style"] +"> ");
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left>PRODUCT CODE#:");
	Response.Write("</th><td><input type=text name=code size=42 value='"+ m_current_code +"'></td></tr>");
	Response.Write("<tr><th align=left >INVOICE#:");
	Response.Write("</th><td><input type=text size=42 name=inv value='"+ m_current_inv +"'></td></tr>");
	Response.Write("<tr><th align=left >INVOICE DATE:");
	Response.Write("</th><td><input type=text size=42 name=inv_date ");
//		Response.Write(" onfocus=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.inv_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
	Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.inv_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
	Response.Write(" value='"+ m_current_inv_date +"' ></td></tr>");
	
	Response.Write("<tr><th align=left><font color=red>PRODUCT DESCRIPTION:");
	Response.Write("</th><td><textarea name=desc rows=6 cols=79 >"+ EncodeQuote(m_current_desc) +"</textarea></td></tr>");
	Response.Write("<tr><th align=left><font color=red>FAULT DESCRIPTION:");
	Response.Write("</th><td><textarea name=fault rows=6 cols=79 >"+ EncodeQuote(m_current_fault) +"</textarea></td></tr>");
	Response.Write("<tr><th colspan=2 align=center>");
	if(m_sSite.ToLower() == "admin")
	{
		Response.Write("<input type=button  value='<< Back to Created RMA# List'"+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all');\"> ");
	}
	Response.Write("<input type=submit name=cmd value='Add This Faulty Item'"+ Session["button_style"] +" ");
	Response.Write(" onclick=\"if(document.frm.fault.value=='' || document.frm.desc.value==''){window.alert('Please Fill Both Product Descriptions and Fault Descriptions'); return false;}\" ");
	Response.Write("> ");
	Response.Write("</td></tr>");
	
	Response.Write("</table>");
	Response.Write("</td></tr>");
//	Response.Write("</td></tr>");
//	Response.Write("</table>");	
	if(Session["ss_rp_count"] != null && Session["ss_rp_count"] != "") //(int)Session["ss_rp_count"] > 0)
	{
		bool bAlter = false;
		
		Response.Write("<tr><td><br>");
		Response.Write("<table width=100% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC ");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><th align=left colspan=4>CURRENT ADDED FAULTY ITEM(S)</td>");
		Response.Write("<td colspan=3 align=right><input type=submit name=cmd value='Request RMA#' "+ Session["button_style"] +" ");
		if(m_sSite.ToLower() != "admin")
			Response.Write(" onclick=\"if(!confirm('Processing RMA# Request...')){return false;};\" ");
		else
			Response.Write(" onclick=\"if(!confirm('Processing RMA#...')){return false;};\" ");
		Response.Write(">");
		Response.Write("</tr>");
		Response.Write("<tr bgcolor=#DED123 align=left><th>SN#</th><th>INVOICE#</th><th>INV_DATE</th><th>CODE#</th><th>PRO_DESC</th><th>FAULT_DESC</th><th>ACTION</th></tr>");
		for(int i=1; i<=(int)Session["ss_rp_count"]; i++)
		{
			if(Session["ss_rp_desc"+ i.ToString()] != null && Session["ss_rp_desc"+ i.ToString()] != "")
			{
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#E3E3E3 ");
			Response.Write(">");
			bAlter = !bAlter;
			
			Response.Write("<td>"+ Session["ss_rp_sn"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_rp_inv"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_rp_inv_date"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_rp_code"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_rp_desc"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_rp_fault"+ i.ToString()] +"</td>");
			Response.Write("<th align=left><a title='delete this item' href='"+ Request.ServerVariables["URL"]+"?r="+DateTime.Now.ToOADate() +"");
			if(m_querystring != "")
				Response.Write("&ra="+ m_querystring +"");
			if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
				Response.Write("&rid="+ HttpUtility.UrlEncode(Request.QueryString["rid"]) +"");
			if(Request.QueryString["ifra"] != null && Request.QueryString["ifra"] != "")
				Response.Write("&ifra="+ Request.QueryString["ifra"]);
			Response.Write("&del="+i+"' class=o><font color=red>X</a></font></td>");
			Response.Write("</tr>");
			}
		}
		Response.Write("</table>");
		Response.Write("</td></tr>");
	}
	Response.Write("</table><br><br><br>");

}

bool DoGenerateRANumber()
{

	if(Session["slt_customer"] == null || Session["slt_customer"] == "")
	{
		Response.Write("<br><br><center><h4><font color=red>Please Select Customer Before Generate a RMA Number!!</h4>");
		Response.Write("<br><a title='back to repair' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
		if(m_querystring != "")
			Response.Write("&ra="+ m_querystring +"");
		
		Response.Write("' class=o>Back to Repair</a></center>");
		return false;
	}

	Session["sRANumber"] = GetNextRA_ID();
//DEBUG(" next ra id = ", Session["sRANumber"].ToString());
	Session["sCustomer_id"] = Session["slt_customer"];
	Session["sCustomer_email"] = Session["slt_email"];
	string ra_number = Session["sRANumber"].ToString();
	string sc = " BEGIN TRANSACTION INSERT INTO repair (ra_number, customer_id, repair_date, accepted, isinput) ";
	sc += " VALUES ('"+ ra_number +"', "+ Session["slt_customer"] +", GETDATE(), 1, 0) ";
	sc += " UPDATE settings SET value = '"+ ra_number +"' WHERE name = 'repair_id' ";
	sc += " COMMIT ";

//DEBUG("s c = ", sc);

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

	SessionCleanUp();
	return true;
}

bool QueryRANumberCreated()
{
	if(dstcom.Tables["created_ra"] != null)
		dstcom.Tables["created_ra"].Clear();

	string sc = " SELECT r.ra_number, r.repair_date, c.phone, c.name, c.company, c.trading_name, r.customer_id ";
	sc += ", r.bdelete ";
	sc += " FROM repair r ";
	sc += " LEFT OUTER JOIN card c ON c.id = r.customer_id ";
	sc += " WHERE isinput = 0 ";
	if(Request.QueryString["ira"] != null && Request.QueryString["ira"] != "" &&Request.QueryString["ira"] != "all")
		sc += " AND r.ra_number = '"+ Request.QueryString["ira"] +"' ";
	if(m_search != "")
	{
		sc += " AND (r.ra_number = '"+ m_search +"' OR c.name LIKE '%"+ m_search +"%' ";
		sc += " OR c.company LIKE '%"+ m_search +"%' ";
		sc += " OR c.trading_name LIKE '%"+ m_search +"%' ";
		sc += " OR c.phone LIKE '%"+ m_search +"%' ";
		sc += " )";
	}
	if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		sc += " AND r.ra_number = '"+ Request.QueryString["rid"] +"'";
	if(m_search == "" || m_search == null)
		sc += " AND r.bdelete = 0 ";
	sc += " ORDER BY r.repair_date DESC ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "created_ra");
	
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
	{
		Response.Write("<br><br><center><h4>No RMA Number Created!!</h4>");
		Response.Write("<br><br><h5><a title='Create New RMA Number' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=cr' class=o>Create New RMA Number</a></h5>");
		return false;
	}
string uri = ""+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"";
	Response.Write("<table align=center width=90% cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	/*if(rows == 1)
	{
		if(MyBooleanParse(dst.Tables["created_ra"].Rows[0]["bdelete"].ToString()))
		{
			string salign = "right";
			Response.Write("<tr><th align="+ salign +">DELETED RMA#</td><td>&nbsp;</td></tr>");
			Response.Write("<tr><td align="+ salign +">RMA# </td><td>" + dst.Tables["created_ra"].Rows[0]["ra_number"].ToString()+"</td></tr>");
			Response.Write("<tr><td align="+ salign +">CUSTOMER# </td><td> " + dst.Tables["created_ra"].Rows[0]["customer_id"].ToString()+"</td></tr>");
			Response.Write("<tr><td align="+ salign +">NAME# </td><td> " + dst.Tables["created_ra"].Rows[0]["name"].ToString()+"</td></tr>");
			Response.Write("<tr><td align="+ salign +">COMPANY# </td><td> " + dst.Tables["created_ra"].Rows[0]["company"].ToString()+"</td></tr>");
			Response.Write("<tr><td align="+ salign +">PHONE# </td><td> " + dst.Tables["created_ra"].Rows[0]["phone"].ToString()+"</td></tr>");
			Response.Write("<tr><td align="+ salign +">CREATE DATE# </td><td> " + dst.Tables["created_ra"].Rows[0]["repair_date"].ToString()+"</td></tr>");
			Response.Write("<tr><td align="+ salign +">Restore this RMA#: </td><td><a title='restore this rma#' href='"+ uri +"&res="+ HttpUtility.UrlEncode(dst.Tables["created_ra"].Rows[0]["ra_number"].ToString()) +"' class=o><font color=red>RESTORE</a>");
			Response.Write(" &nbsp;&nbsp;<a title='back' href='"+ uri +"&ra=all' class=o>BACK</a>");
			Response.Write("</td></tr>");
			
			return false;
		}
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ uri +"&ra=ip&rid="+ HttpUtility.UrlEncode(dst.Tables["created_ra"].Rows[0]["ra_number"].ToString()) +"");
		Response.Write("&cid="+ dst.Tables["created_ra"].Rows[0]["customer_id"] +"");
		Response.Write("\">");
		
		Session["sCustomer_id"] = dst.Tables["created_ra"].Rows[0]["customer_id"].ToString();
		Session["sCustomer_name"] = dst.Tables["created_ra"].Rows[0]["name"].ToString();
		if(!g_bRetailVersion)
			Session["sCustomer_name"] = dst.Tables["created_ra"].Rows[0]["company"].ToString();
		return false;
	}
	*/
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;
	m_cPI.URI = "?ra=all&r="+ DateTime.Now.ToOADate() +"";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;

	string sPageIndex = m_cPI.Print();
	Response.Write("<br><center><h4>Created RMA Number(s) </h4>");
	
	Response.Write("<tr><td colspan=5>SEARCH RMA Number: <input type=text name=search value='"+ Request.Form["search"] +"'><input type=submit name=cmd value='Search' "+Session["button_style"] +">");
	Response.Write("<input type=button value='   ALL   ' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all')\">");
	Response.Write("<input type=button value='CREATE NEW RMA#' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=cr')\">");
	Response.Write("</td><td colspan=2>"+ sPageIndex +"");
	Response.Write("</td></tr>");
	Response.Write("<script language=javascript>");
	Response.Write("\n\r document.frm.search.focus();\r\n </script");
	Response.Write(">");
	//Response.Write("<tr bgcolor=#EDE3E3>");
	Response.Write("<tr align=left bgcolor=#EEDDDD><th>RMA#</th><th>CREATED DATE</th><th>NAME</th><th>COMPANY</th><th>TRADING NAME</th><th>PHONE</th><th>ACTION</th></tr>");
	
	bool bAlter = true;
	
	for(; i<rows && i<end; i++)
	{
		DataRow dr = dst.Tables["created_ra"].Rows[i];	
		string id = dr["customer_id"].ToString();
		string name = dr["name"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string phone = dr["phone"].ToString();
		string company = dr["company"].ToString();
		string repair_date = dr["repair_date"].ToString();
		string ra = dr["ra_number"].ToString();
		bool bdelete = MyBooleanParse(dr["bdelete"].ToString());
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(">");
		bAlter = !bAlter;  
		Response.Write("<td><a title='Input Faulty Items' href='"+ uri +"&rid="+ HttpUtility.UrlEncode(ra) +"&cid="+ id +"&ra=ip' class=o>"+ ra +"</a></td>");
		Response.Write("<td>"+name+"</td>");
		Response.Write("<td>"+ repair_date+"</td>");
		Response.Write("<td>"+company+"</td>");
		Response.Write("<td>"+trading_name+"</td>");
		Response.Write("<td>"+phone+"</td>");

		Response.Write("<td>");
		if(bdelete)
			Response.Write("<a title='restore RMA#' href='"+ uri +"&res="+ HttpUtility.UrlEncode(ra) +"' class=o><font color=red>REST</a>");
		else
		{
			Response.Write("<a title='Input Faulty Items' href='"+uri +"&rid="+ HttpUtility.UrlEncode(ra) +"&cid="+ id +"&ra=ip' class=o>INPUT FAULTY ITEM</a>");
			Response.Write("&nbsp;&nbsp;<a title='delete RMA#' href='"+ uri +"&radel="+ HttpUtility.UrlEncode(ra) +"' class=o><font color=red>X</a>");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	
	Response.Write("</table>");
	return true;
}

void SessionCleanUp()
{
	if(m_command.ToLower() == "generate rma number")
	{
		Session["sCustomer_name"] = Session["slt_name"];
		Session["slt_customer"] = null;
		Session["slt_add1"] = null;
		Session["slt_add2"] = null;
		Session["slt_city"] = null;
		Session["slt_phone"] = null;
		Session["slt_fax"] = null;
		Session["slt_email"] = null;
		Session["slt_name"] = null;
	}
	else //if(m_command.ToLower() == "request rma#")
	{
		if(Session["ss_rp_rp_count"] != null && Session["ss_rp_count"] != "")
		{
			for(int i=1; i<=(int)Session["ss_rp_count"]; i++)
			{
				Session["ss_rp_sn"+i] = null;
				Session["ss_rp_sn"+ i] = null;
				Session["ss_rp_code"+ i] = null;
				Session["ss_rp_inv"+ i] = null;
				Session["ss_rp_inv_date"+ i] = null;
				Session["ss_rp_desc"+ i] = null;
				Session["ss_rp_fault"+ i] = null;
				Session["ss_rp_supplier_id"+ i] = null;
				Session["ss_rp_supplier_code"+ i] = null;
			}
		}
		Session["ss_rp_count"] = null;
	}

}
void GenerateRANumber()
{
	Response.Write("<br><br><h4><center>CREATE NEW RMA#</h4>");
	Response.Write("<table width=40% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr align=center><td >");
	Response.Write("Select Customer: <select name=customer onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?slt="+ m_selected +"&ci=all')\" >\r\n");
	if(Session["slt_customer"] != null)
		Response.Write("<option value="+ Session["slt_customer"] +" selected>"+ Session["slt_name"] +" </option>");
	
	Response.Write("<option value='no_cust'>ALL");
	Response.Write("</select>");
	Response.Write("<input fgcolor=blue type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
	Response.Write("id=" + Session["slt_customer"] + "','', ' width=350,height=350');\" value='who?' " + Session["button_style"] + ">");
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=0 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th align=right>Customer Name:</td><td>");
	Response.Write(Session["slt_name"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Address:</td><td>");
	Response.Write(Session["slt_add1"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>");
	Response.Write(Session["slt_add2"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><td></td><td>");
	Response.Write(Session["slt_city"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Phone:</td><td>");
	Response.Write(""+ Session["slt_phone"] +"");
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Fax:</td><td>");
	Response.Write(Session["slt_fax"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Email:</td><td>");
	Response.Write(Session["slt_email"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("<br><br><center>");
	Response.Write("<input type=button value='<< Back to Created RMA# List' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all')\" ");
	Response.Write(">");
	Response.Write("<input type=submit name=cmd value='Generate RMA Number' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"if(document.frm.customer.value=='no_cust'){window.alert('Please Select Customer');return false;} if(!confirm('Continue to Create RMA#?')){return false;}\" ");
	Response.Write(">");
	

}

void GetAllQueryString()
{
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		m_command = Request.Form["cmd"];
	if(Request.Form["search"] != null && Request.Form["search"] != "")
		m_search = Request.Form["search"];
	if(Request.QueryString["slt"] != null && Request.QueryString["slt"] != "")
		m_selected = Request.QueryString["slt"];
	if(Request.QueryString["ra"] != null && Request.QueryString["ra"] != "")
		m_querystring = Request.QueryString["ra"];
}

bool GetCardID()
{
	if(dst.Tables["customer"] != null)
		dst.Tables["customer"].Clear();

	string sc = " SELECT distinct id, name, trading_name, company, phone, email, contact, address1, address2, city, fax ";
	sc += " FROM card WHERE 1=1 ";
	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "" && Request.QueryString["ci"] != "all")
		sc += " AND id = "+ Request.QueryString["ci"];
	
	if(Request.Form["search"] != null && Request.Form["search"] != "")
	{
		if(TSIsDigit(Request.Form["search"].ToString()))
			sc += " AND id = "+ Request.Form["search"];
		else
		{
			sc += " AND (name LIKE '%"+ Request.Form["search"].ToString() +"%' OR trading_name LIKE '%"+ Request.Form["search"].ToString() +"%' ";
			sc += " OR company LIKE '%"+ Request.Form["search"].ToString() +"%' OR  email LIKE '%"+ Request.Form["search"].ToString()+"%'";
			sc += " OR phone LIKE '%"+ Request.Form["search"].ToString() +"%'";
			sc += " ) ";
		}
	}
	sc += " ORDER BY id ";
///DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "customer");
	
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(dst.Tables["customer"].Rows.Count == 1)
	{
		Session["slt_customer"] = dst.Tables["customer"].Rows[0]["id"].ToString();
		Session["slt_add1"] = dst.Tables["customer"].Rows[0]["address1"].ToString();
		Session["slt_add2"] = dst.Tables["customer"].Rows[0]["address2"].ToString();
		Session["slt_city"] = dst.Tables["customer"].Rows[0]["city"].ToString();
		Session["slt_phone"] = dst.Tables["customer"].Rows[0]["phone"].ToString();
		Session["slt_fax"] = dst.Tables["customer"].Rows[0]["fax"].ToString();
		Session["slt_email"] = dst.Tables["customer"].Rows[0]["email"].ToString();
		if(g_bRetailVersion)
			Session["slt_name"] = dst.Tables["customer"].Rows[0]["name"].ToString();
		else
			Session["slt_name"] = dst.Tables["customer"].Rows[0]["company"].ToString();
		if(Session["slt_name"] == null || Session["slt_name"] == "")
			Session["slt_name"] = dst.Tables["customer"].Rows[0]["trading_name"].ToString();

		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?r="+ DateTime.Now.ToOADate() +"");
		Response.Write("&ra=cr");
		Response.Write("\">");
		return false;
	}

	return true;
}
void ShowCardInfor()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = dst.Tables["customer"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;
	m_cPI.URI = "?ci=all&r="+ DateTime.Now.ToOADate() +"";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;

	string sPageIndex = m_cPI.Print();

//	Response.Write("<form name=frm method=post>");
	Response.Write("<br><h4><center>CUSTOMER(s) Search</h4>");
	Response.Write("<table align=center width=90% cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4>SEARCH CLIENT(s): <input type=text name=search value='"+ Request.Form["search"] +"'><input type=submit name=cmd value='Search' "+Session["button_style"] +">");
	Response.Write("<input type=button name=addnew value='Add New Customer' "+ Session["button_style"] +" onclick=\"javascript:new_window=window.open('ecard.aspx?a=new&r="+ DateTime.Now.ToString("ddMMyyyyHHmm") +"', '',''); new_window.focus();\">");
	Response.Write("</td><td colspan=2>"+ sPageIndex +"");
	Response.Write("</td></tr>");
	Response.Write("<script language=javascript>");
	Response.Write("\n\r document.frm.search.focus();\r\n </script");
	Response.Write(">");
	Response.Write("<tr bgcolor=#EDE3E3>");
	Response.Write("<tr bgcolor=#EEDDDD><th>ID</th><th>Name</th><th>Contact</th><th>Trading Name</th><th>Company</th><th>Email</th></tr>");
	string uri = ""+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ci=";
	bool bAlter = true;
	
	for(; i<rows && i<end; i++)
	{
		DataRow dr = dst.Tables["customer"].Rows[i];	
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string contact = dr["contact"].ToString();
		string company = dr["company"].ToString();
		string email = dr["email"].ToString();
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(">");
		bAlter = !bAlter;
		Response.Write("<td><a title='select Customer' href='"+uri+id+"' class=o>"+id+"</a></td>");
		Response.Write("<td><a title='select Customer' href='"+uri+id+"' class=o>"+name+"</a></td>");
		Response.Write("<td><a title='select Customer' href='"+uri+id+"' class=o>"+contact+"</a></td>");
		Response.Write("<td><a title='select Customer' href='"+uri+id+"' class=o>"+trading_name+"</a></td>");
		Response.Write("<td><a title='select Customer' href='"+uri+id+"' class=o>"+company+"</a></td>");
		Response.Write("<td>"+email+"</td>");
		Response.Write("</tr>");
	}
	
	Response.Write("</table>");
//	Response.Write("</form>");
//	return true;
}

string PrintDocket()
{
	StringBuilder sb = new StringBuilder();
	DataRow dr;
	sb.Append("<title>"+Session["CompanyName"]+" Repair Form</title>");
	sb.Append("<body onload='window.print()'>");
	sb.Append("<table align=center width=98% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	//bgcolor=#cc766E
	sb.Append("<tr ><th align=left colspan=8>"+ra_header+"</th></tr>");
	sb.Append("<tr><td colspan=4>");

	string technician = "";
	string r_date = "";
	string ra_number = "";
//DEBUG("rows = ", dst.Tables["ra_detail"].Rows.Count.ToString());
	string ticket = "";
	
	for(int i=0; i<dst.Tables["tickets"].Rows.Count; i++)
		ticket += dst.Tables["tickets"].Rows[i]["ticket"].ToString() +" &nbsp;&nbsp;";

	if(dst.Tables["ra_detail"].Rows.Count > 0)
	{
		dr = dst.Tables["ra_detail"].Rows[0];
		string name = dr["cname"].ToString();
		if(name == "" && name == null)
			name = dr["trading_name"].ToString();
		string company = dr["ccompany"].ToString();
	//	string ticket = dr["ticket"].ToString();
		string addr1 = dr["addr1"].ToString();
		string addr2 = dr["addr2"].ToString();
		string city = dr["ccity"].ToString();
		string phone = dr["cphone"].ToString();
		string fax = dr["cfax"].ToString();
		string email = dr["cemail"].ToString();
		technician = dr["technician"].ToString();
		ra_number = dr["ra_number"].ToString();
		r_date = dr["repair_date"].ToString();
		r_date = DateTime.Parse(r_date).ToString("dd-MMM-yyyy");
		
		string authorize_date = dr["authorize_date"].ToString();
		string received_date = dr["received_date"].ToString();

		m_repair_form_template = m_repair_form_template.Replace("@@authorize_date", authorize_date);
		m_repair_form_template = m_repair_form_template.Replace("@@received_date", received_date);
		m_repair_form_template = m_repair_form_template.Replace("@@company", company);
		m_repair_form_template = m_repair_form_template.Replace("@@customer", name);
		m_repair_form_template = m_repair_form_template.Replace("@@ticket", ticket);
		m_repair_form_template = m_repair_form_template.Replace("@@addr1", addr1);
		m_repair_form_template = m_repair_form_template.Replace("@@addr2", addr2);
		m_repair_form_template = m_repair_form_template.Replace("@@city", city);
		m_repair_form_template = m_repair_form_template.Replace("@@phone", phone);
		m_repair_form_template = m_repair_form_template.Replace("@@fax", fax);
		m_repair_form_template = m_repair_form_template.Replace("@@email", email);
		m_repair_form_template = m_repair_form_template.Replace("@@ra_number", ra_number);
		m_repair_form_template = m_repair_form_template.Replace("@@repair_date", r_date);
		m_repair_form_template = m_repair_form_template.Replace("@@technician", technician);

		sb.Append("<table align=center width=98% cellspacing=3 cellpadding=2 border=0 bordercolor=black bgcolor=white");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		sb.Append("<tr><th colspan=2 align=left><font size=3>Customer Details:</font></th></tr>");
		//sb.Append("<tr><td>"+name+"</td><td>|&nbsp;</td></tr>");
		//sb.Append("<tr><td>"+company+"</td><td>|&nbsp;</td></tr>");
		sb.Append("<tr><td>"+name+"</td><td>&nbsp; ph: "+phone+"</td></tr>");
		sb.Append("<tr><td>"+company+"</td><td>&nbsp; fx: "+fax+"</td></tr>");
		sb.Append("<tr><td>"+addr1+"</td><td>&nbsp; email: "+email+"</td></tr>");
		if(addr2 != "")
			sb.Append("<tr><td>"+addr2+"</td><td>&nbsp;</td></tr>");
		if(city != "")
			sb.Append("<tr><td>"+city+"</td><td>&nbsp; </td></tr>");
		sb.Append("</table>");
		sb.Append("</td><th colspan=4 valign=top>");
		sb.Append("<table border=0 ><tr><th colspan=2>RMA#: "+ ra_number +"</th></tr>");
		sb.Append("<tr><td>Repair Date: </td><td>"+ r_date +"</td></tr>");
		sb.Append("<tr><td>Delivery Ticket: </td><td>"+ ticket +"</td></tr>");
		sb.Append("</table></td></tr>");
		
	}	
	else
	{
		m_repair_form_template = m_repair_form_template.Replace("@@authorize_date", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@received_date", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@company", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@customer", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@ticket", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@addr1", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@addr2", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@city", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@phone", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@fax", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@email", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@ra_number", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@repair_date", "&nbsp;");
		m_repair_form_template = m_repair_form_template.Replace("@@technician", "&nbsp;");
	}
	sb.Append("<tr></tr><tr></tr><tr></tr>");
	//sb.Append("<tr><th colspan=8 align=left>Faulty Items:</th></tr>");
	sb.Append("<tr><th colspan=9 align=left><hr size=1 width=100%></th></tr>");
	sb.Append("<tr>");
	sb.Append("<th align=left width=3%>&nbsp;</th>");
	sb.Append("<th align=left width=15%>SN#</th>");
	sb.Append("<th align=left width=8%>INVOICE#</th>");
	sb.Append("<th align=left width=10%>PURCHASE DATE</th>");
	sb.Append("<th align=left width=7%>CODE#</th>");
	sb.Append("<th align=left width=10%>M_PN#</th>");
	sb.Append("<th align=left>DESCRIPTION</th>");
	sb.Append("<th align=left width=7%>STATUS</th>");
	sb.Append("<th align=right width=4%>QTY&nbsp;</th>");
	sb.Append("</tr>");
	sb.Append("<tr><th colspan=9 align=left><hr size=1 width=100%></th></tr>");
	
	for(int i=0; i<dst.Tables["ra_detail"].Rows.Count; i++)
	{
		dr = dst.Tables["ra_detail"].Rows[i];
		string rs_sn = dr["rs_sn"].ToString();
		string rs_invoice = dr["rs_invoice"].ToString();
		//string ticket = dr["ticket"].ToString();
		string staff = dr["staff"].ToString();
		string rs_code = dr["rs_code"].ToString();
		string rs_desc = dr["description"].ToString();
		string rs_date = dr["rs_date"].ToString();
		string rs_supp_code = dr["rs_supp_code"].ToString();
			//string rs_sn = dr["rs_sn"].ToString();
		string replaced = dr["replaced"].ToString();
		string sn = dr["serial_number"].ToString();
		string rid = dr["rid"].ToString();
		string ra_id = dr["ra_number"].ToString();
//		string code = dr["code"].ToString();
		string code = dr["cr_code"].ToString();
		string pur_date = dr["purchase_date"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string fault = dr["fault_desc"].ToString();
//		string desc = dr["prod_desc"].ToString();
		string desc = dr["cr_name"].ToString();
		string card_id = dr["customer_id"].ToString();
		string repair_date = dr["repair_date"].ToString();
		string status = dr["ra_status"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string invoice_date = dr["purchase_date"].ToString();
		int nStatus = int.Parse(dr["status"].ToString());
//		technician = dr["technician"].ToString();
		string note = dr["note"].ToString();

		if(MyBooleanParse(replaced))
		{
			replaced = "+1";
			status = "Replacement";
		}
		else
		{
			replaced = "";
			status = "Repaired";
		}
		string repair_qty = "-1";
		if(replaced == "+1")
		{
			repair_qty = "-1";
			//status = "return";
		}
		else
		{
			repair_qty = "1";
			replaced = "";
			///status = "Repaired";
		}
		m_repair_form_template = m_repair_form_template.Replace("@@replace_sn"+i, rs_sn);
		m_repair_form_template = m_repair_form_template.Replace("@@replace_inv"+i, rs_invoice);
		m_repair_form_template = m_repair_form_template.Replace("@@replace_code"+i, rs_code);
		m_repair_form_template = m_repair_form_template.Replace("@@replace_item_desc"+i, StripHTMLtags(rs_desc));
		m_repair_form_template = m_repair_form_template.Replace("@@replace_date"+i, rs_date);
		m_repair_form_template = m_repair_form_template.Replace("@@replace_supp_code"+i, rs_supp_code);
		m_repair_form_template = m_repair_form_template.Replace("@@replaced_qty"+i, replaced);
		m_repair_form_template = m_repair_form_template.Replace("@@repair_qty"+i, repair_qty);
		m_repair_form_template = m_repair_form_template.Replace("@@sn"+i, sn);
		m_repair_form_template = m_repair_form_template.Replace("@@code"+i, code);
		m_repair_form_template = m_repair_form_template.Replace("@@invoice"+i, invoice);
		m_repair_form_template = m_repair_form_template.Replace("@@invoice_date"+i, invoice_date);
		m_repair_form_template = m_repair_form_template.Replace("@@fault_desc"+i, fault);
		m_repair_form_template = m_repair_form_template.Replace("@@item_desc"+i, StripHTMLtags(desc));
		m_repair_form_template = m_repair_form_template.Replace("@@status"+i, status);
		m_repair_form_template = m_repair_form_template.Replace("@@supp_code"+i, supplier_code);
		m_repair_form_template = m_repair_form_template.Replace("@@repair_note"+i, note);
//		m_repair_form_template = m_repair_form_template.Replace("@@technician"+i, technician);
		m_repair_form_template = m_repair_form_template.Replace("@@status"+i, status);
		//m_repair_form_template = m_repair_form_template.Replace("@@ticket"+i, ticket);
		//sb.Append("<tr><th colspan=5 align=left>Faulty Items:</th></tr>");
		//sb.Append("<tr bgcolor=#EEEEEE>");
		sb.Append("<tr><td colspan=9>");
		sb.Append("<table align=center width=100% cellspacing=1 cellpadding=3 border=0 bordercolor=gray bgcolor=white");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
			
		sb.Append("<tr><td width=3%>"+(i+1)+".</td><td width=15%>"+sn+"</td>");
		sb.Append("<td width=8%>"+invoice+"</td>");
		sb.Append("<td width=10%>"+pur_date+"</td>");
		sb.Append("<td width=7%>"+code+"</td>");
		sb.Append("<td width=10%>"+supplier_code+"</td>");
		sb.Append("<td>"+desc+"</td>");
		sb.Append("<td width=7%>&nbsp;</td>");
		sb.Append("<td align=right width=4%>");
		if(replaced == "1")
			sb.Append("-");
		else
			sb.Append("");
		sb.Append("1&nbsp;</td>");
		sb.Append("</tr>");
		
		if(replaced == "1")
		{
			sb.Append("<tr bgcolor=#EEEEEE><td>&nbsp;</td>");
			//sb.Append("<tr bgcolor=#eeeee><th align=left width=10%>Replaced With:</td>");
			sb.Append("<td>"+rs_sn.ToUpper()+"</td><td>"+ rs_invoice +"</td><td>"+ pur_date +"</td><td>&nbsp;</td><td>"+ rs_supp_code +"</td>");
			sb.Append("<td>"+ rs_desc +"</td>");
			sb.Append("<td width=7%>Replaced</td>");
			sb.Append("<td align=right>1&nbsp;");
			sb.Append("</td></tr>");

		}
		sb.Append("<tr ><td>&nbsp;</td><th colspan=3 align=left>REPAIR STATUS:&nbsp;&nbsp;<font color=Red>"+ status.ToUpper() +"</font></td>");
		sb.Append("<th colspan=2 align=right>REPAIR DESC:</td>");
		sb.Append("<td colspan=3>"+note+"</td></tr>");
		sb.Append("</table></td></tr>");
		//sb.Append("<tr></tr>");
		
	}
//get replaced value

		for(int j=dst.Tables["ra_detail"].Rows.Count; j<MyIntParse(GetSiteSettings("ra_qty_limit", "5")); j++)
		{
			m_repair_form_template = m_repair_form_template.Replace("@@replace_sn"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@replace_inv"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@replace_code"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@replace_item_desc"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@replace_date"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@replace_supp_code"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@replaced_qty"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@repair_qty"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@sn"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@code"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@invoice"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@invoice_date"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@fault_desc"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@item_desc"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@status"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@supp_code"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@repair_note"+j, "&nbsp;");
			m_repair_form_template = m_repair_form_template.Replace("@@technician"+j, "&nbsp;");
//			DEBUG("pass 3=", "pass here");
		}

	ra_packslip = ra_packslip.Replace("@@technician", technician);
	ra_packslip = ra_packslip.Replace("@@jobnumber", ra_number);
	ra_packslip = ra_packslip.Replace("@@repairdate", r_date);
	//sb.Append("<tr><td colspan=6>&nbsp;</td></tr>");
	int rows = dst.Tables["ra_detail"].Rows.Count;
	if(rows == 1 )
	{
		for(int i=0; i<200; i++)
			sb.Append("<tr></tr>");
	}
	else if(rows == 2 )
	{
		for(int i=0; i<150; i++)
			sb.Append("<tr></tr>");
	}
	else if(rows == 3 )
	{
		for(int i=0; i<100; i++)
			sb.Append("<tr></tr>");
	}
	else if(rows == 4 )
	{
		for(int i=0; i<70; i++)
			sb.Append("<tr></tr>");
	}
	else if(rows == 5 )
	{
		for(int i=0; i<50; i++)
			sb.Append("<tr></tr>");
	}
//	sb.Append("<tr><td colspan=8>"+ra_conditions+"</td></tr>");
//	sb.Append("<tr><td colspan=8>"+ra_packslip+"</td></tr>");
	sb.Append("</table>");

//DEBUG("company_id =", company_id);
	if(company_id != "")
		return m_repair_form_template;
	else
		return sb.ToString();
	
}

string GetCompanyCardID(string type)
{
	if(dst.Tables["card_id"] != null)
		dst.Tables["card_id"].Clear();
	string sc = " SELECT TOP 1 id FROM card WHERE type = "+ type;
//DEBUG("sc =", sc);
string card_id = "";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "card_id") == 1)
			card_id = dst.Tables["card_id"].Rows[0]["id"].ToString();
	
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	return card_id;

}

bool displayRA_Item()
{
	DataRow dr;
		
	if(dst.Tables["ra_detail"].Rows.Count <=0)
	{
		Response.Write("<center><h4>No RMA Items with "+Session["CompanyName"]+"</h4></center>");
		return false;
	}
		//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["ra_detail"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;

	m_cPI.URI = "?ra=view&r="+ DateTime.Now.ToString("ddMMyyyyHHmmss") +"";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	Response.Write("<form name=frm method=post>");
	Response.Write("<br><center><h5>Thank You for using our RMA Online Application. Our Technical Support Team will contact you As Soon As Possible</h5></center>");
	Response.Write("<table align=center width=80% cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	//Response.Write("<tr><td colspan=6>Hi, "+Session["name"]+", You have : <b><font color=green>"+dst.Tables["ra_detail"].Rows.Count+"</font></b> item(s) for repairing with us</td></tr>");
	Response.Write("<tr><td colspan=4>");
	Response.Write(sPageIndex);
	Response.Write("</td><td colspan=2>");
	Response.Write("Status Options: <select name=slt_option onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?s=view&t='+ this.options[this.selectedIndex].value)\" >");
	Response.Write("<option value=0");
	Response.Write(">All</option>");
	Response.Write("<option value=1");
	if(Request.QueryString["t"] == "1")
		Response.Write(" selected ");
	Response.Write(">Waiting For Authorized</option>");
	Response.Write("<option value=2 ");
	if(Request.QueryString["t"] == "2")
		Response.Write(" selected ");
	Response.Write(">Authorized</option>");
	Response.Write("<option value=3");
	if(Request.QueryString["t"] == "3")
		Response.Write(" selected ");
	Response.Write(">Repairing</option>");
	Response.Write("<option value=5");
	if(Request.QueryString["t"] == "5")
		Response.Write(" selected ");
	Response.Write(">Finish</option>");
	Response.Write("</select>");
	Response.Write("</td></tr>");
	Response.Write("<tr bgcolor=#E2EED9><th>&nbsp;</th><th>STATUS</th><th>REPAIR DATE</th><th>PROD_DESC</th><th>RMA#</th><th>&nbsp;</th></tr>");
	bool bAlter = true;
	//for(int i=0; i<dst.Tables["ra_detail"].Rows.Count; i++)
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["ra_detail"].Rows[i];
		string sn = dr["serial_number"].ToString();
		string rid = dr["rid"].ToString();
		string ra_id = dr["ra_number"].ToString();
		string code = dr["code"].ToString();
		string pur_date = dr["purchase_date"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string fault = dr["fault_desc"].ToString();
		string desc = dr["prod_desc"].ToString();
		string card_id = dr["customer_id"].ToString();
		string repair_date = dr["repair_date"].ToString();
		string status = dr["ra_status"].ToString();
		int nStatus = int.Parse(dr["status"].ToString());
		string technician = dr["technician"].ToString();
		string note = dr["note"].ToString();
		Response.Write("<tr ");
		if(!bAlter)
			Response.Write(" bgcolor=#E3E3EE");
		bAlter = !bAlter;
		Response.Write(">");
		//Response.Write("<td><a title='click me to view ur detial' href='"+ m_uri +"' class=o>"+ (i+1) +"</a></td>");
		Response.Write("<th>"+ (i+1) +"</th>");
		if(status.ToLower() == "gone" )
			status = "finish";
		Response.Write("<th align=left>"+status.ToUpper()+"</th>");
		Response.Write("<td>"+DateTime.Parse(repair_date).ToString("dd-MMM-yyyy")+"</td>");
		Response.Write("<td>"+desc+"</td>");
		if(nStatus >=2 && nStatus <=6)
		{
			Response.Write("<td>"+ra_id+"</td>");
			Response.Write("<td>");
			if(ra_id != "")
			{
				Response.Write("<a title='click to print repair application'  href='"+ Request.ServerVariables["URL"] +"?print=form&ra="+ ra_id +"&r="+ DateTime.Now.ToOADate()+"&ty=1' class=o target=_blank >PRF</a>&nbsp;&nbsp;");
				if(nStatus >= 4)
				Response.Write("<a title='click to print finish repair application'  href='"+ Request.ServerVariables["URL"] +"?print=form&ra="+ ra_id +"&r="+ DateTime.Now.ToOADate()+"&ty=2' class=o target=_blank >PFF</a>&nbsp;&nbsp;");
			}
				//Response.Write("");
			Response.Write("<a title='click me to view job status' href=\"javascript:viewcard_window=window.open('view_ra.aspx?");
			Response.Write("job=" + rid + "','viewcard_window','width=640,height=480'); viewcard_window.focus();\" class=o><font color=red><b>S</font></b></a></td>");
		}
		else
		{
			Response.Write("<td><font color=red>Waiting for RMA_NO#</font></td>");
			Response.Write("<td><a title='click me to view job status' href=\"javascript:viewcard_window=window.open('view_ra.aspx?");
			Response.Write("job=" + rid + "','viewcard_window','width=640,height=480'); viewcard_window.focus();\" class=o><font color=red><b>S</font></b></a></td>");
		}
		Response.Write("</tr>");
		
	}
	//Response.Write("");
	Response.Write("</table>");
	Response.Write("<br><center>");
	Response.Write("</form>");
	if(m_sSite != "admin")
		Response.Write("<input type=button value='GO SHOPPING' "+ Session["button_style"] +" onclick=\"window.location=('c.aspx')\"><br><br>");
	
	return true;
}

string chckLogin()
{
	
	string stype = "";
	if(Session["card_id"] != null && Session["card_id"] != "")
	{
		if(dst.Tables["login"] != null)
			dst.Tables["login"].Clear();
		string sc = " SELECT type FROM card ";
		sc += " WHERE id = "+ Session["card_id"];
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			int rows = myAdapter.Fill(dst, "login");
			if(rows > 0)
			{	
				stype = dst.Tables["login"].Rows[0]["type"].ToString();
				//return stype;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return "errer";
		}
	}
	return stype;
}

bool getRepairDetails()
{
	string type = chckLogin();

//	if(dst.Tables["ra_detail"] != null)
//		dst.Tables["ra_detail"].Clear();

	string id = "";
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		id = Request.QueryString["id"].ToString();
	string ra = "";
	if(Request.QueryString["ra"] != null && Request.QueryString["ra"] != "")
		ra = HttpUtility.UrlDecode(Request.QueryString["ra"]); //.ToString();

	string sc = "SELECT r.*, r.id AS rid, e.name AS ra_status, c.fax AS cfax, c.name AS cname, c.trading_name";
	sc += " ,c.city AS ccity, c.company AS ccompany, c.address1 AS addr1, c.address2 AS addr2, c.phone AS cphone, c.email AS cemail ";
	sc += " ,rs.code AS rs_code, rs.sn AS rs_sn, rs.description, rs.invoice_number AS rs_invoice,  rs.rs_date ";
	sc += ", cr.supplier_code AS rs_supp_code ";
	sc += " ,c2.name AS staff, c2.email AS staff_email";
	sc += ", cr2.name AS cr_name, cr2.code AS cr_code ";
	sc += " FROM repair r JOIN card c ON c.id = r.customer_id ";
	sc += " LEFT OUTER JOIN enum e ON e.id = r.status ";
//	sc += " LEFT OUTER JOIN ra_statement rs ON rs.ra_id = r.id ";
	sc += " LEFT OUTER JOIN ra_replaced rs ON rs.ra_id = r.id ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = rs.code ";
	sc += " LEFT OUTER JOIN code_relations cr2 ON cr2.supplier_code = r.supplier_code ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = rs.staff ";

	sc += " WHERE r.status < 6 ";
	sc += " AND e.class = 'rma_status' ";
	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
	{
		if(TSIsDigit(Request.QueryString["t"].ToString()))
		{
			sc += " AND (r.status = "+ Request.QueryString["t"];
			if(Request.QueryString["t"] == "3")
				sc += " OR r.status = 4 ";
			sc += " ) ";
		}
	}
	if(m_sSite.ToLower() != "admin")
		sc += " AND r.customer_id = "+ Session["card_id"].ToString() ;
	if(ra != null && ra != "" && Request.QueryString["s"] != "view")
		sc += " AND r.ra_number = '"+ ra +"' "; 
	if(Request.QueryString["ty"] == "2")
		sc += " AND r.status > 3 "; 
	if(Request.QueryString["id"] != null)
	{
		sc += " AND r.id = "+ id +" ";
		if(bHide)
			bHide = !bHide;
	}
	else
		bHide = true;
	sc += " ORDER BY r.repair_date DESC ";
//DEBUG("sc = ", sc );
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "ra_detail");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//query tickets for repair
	if(Request.QueryString["print"] == "form")
	{
		sc = " SELECT ticket FROM ra_freight WHERE repair_number = '"+ ra +"' ";
//DEBUG("sc = ", sc );
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "tickets");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}


DataRow CheckSN(string sn, string invoice)
{
	DataSet dscsn = new DataSet();
	
	sn = sn.ToUpper();
/*	string sc = " SELECT t.sn, i.invoice_number, ss.invoice_number AS invoice_number2, s.name";
	sc += ", CONVERT(varchar(12),i.commit_date,13) AS commit_date, s.code, p.supplier_id ";
	sc += " , s.supplier_code ";
	sc += " FROM serial_trace t LEFT OUTER JOIN sales_serial ss ON ss.sn = t.sn ";
	sc += " LEFT OUTER JOIN invoice i ON (i.invoice_number = t.invoice_number AND i.invoice_number = ss.invoice_number)";
	sc += " LEFT OUTER JOIN sales s ON (s.invoice_number = i.invoice_number AND s.invoice_number = ss.invoice_number AND s.code = ss.code) ";
	sc += " LEFT OUTER JOIN purchase p ON p.id = t.po_id ";
	//if(isNum)
	//	sc += " WHERE i.invoice_number = '"+ invoice +"'";
	sc += " WHERE s.code = ss.code ";
	sc += " AND UPPER(RTRIM(t.sn)) = '"+ sn +"'  ";
*/
	if(sn != "" && sn != null)
	{
		string sc = " SET DATEFORMAT dmy SELECT s.sn, i.invoice_number, CONVERT(varchar(12),i.commit_date,13) AS commit_date ";
		sc += " , ss.code, ss.supplier_code, ss.name, st.purchase_order_id AS supplier_id ";
		sc += " FROM sales_serial s JOIN invoice i ON i.invoice_number = s.invoice_number ";
		sc += " LEFT OUTER JOIN sales ss ON ss.invoice_number = s.invoice_number AND i.invoice_number = ss.invoice_number AND s.code = ss.code ";
		sc += " LEFT OUTER JOIN stock st ON st.sn = s.sn AND s.code = st.product_code ";
		sc += " WHERE 1=1 ";

		sc += " AND UPPER(RTRIM(s.sn)) = '"+ sn +"'  ";
		sc += " ORDER BY i.commit_date DESC ";
		
//	DEBUG("sc=", sc);		
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dscsn) > 0)
				return dscsn.Tables[0].Rows[0];
			else if(m_sCompanyName.ToLower() == "iway")
			{
				sc = " SELECT model AS supplier_code, bar_code AS sn, description AS name ";
				sc += " ,CONVERT(varchar(12), date_in, 13) AS commit_date ";
				sc += " ,supplier, 0 AS invoice_number ";
				sc += " FROM sn_old ";
				sc += " WHERE UPPER(RTRIM(bar_code)) = '" + sn + "' ";
				//sc += " ORDER BY bar_code ";
				try
				{
					myAdapter = new SqlDataAdapter(sc, myConnection);
					if(myAdapter.Fill(dscsn) > 0)
						return dscsn.Tables[0].Rows[0];
				}
				catch (Exception e)
				{
				//	ShowExp(sc,e);
					//return false;
				}
			}
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
		}
	}
	return null;
}


bool autoSendMail()
{
	MailMessage msgMail = new MailMessage();
	msgMail.To = Session["email"].ToString();
	msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Subject = "Repair Job with " + m_sCompanyTitle;
	string smsg = "Dear Customer<br><br>";
	
	smsg += "Thank you for having a request Return Material Authorization with us.<br>";
	smsg += "Our Technician will contact you as soon as possible.<br><br><br>";
	smsg += "Best regards<br>";
	smsg += "Support Team<br>";
	
	string smsg_tmp = ReadSitePage("repair_email_message");
	if(smsg_tmp != "" && smsg_tmp != null)
		smsg = smsg_tmp;

	msgMail.Body = smsg;
	
	SmtpMail.Send(msgMail);

	//cc to itself
	msgMail.To = GetSiteSettings("service_email", "alert@eznz.com");
	msgMail.Subject = "New Repair Job ";
	SmtpMail.Send(msgMail);
	return true;
	
	
}
bool DoTemporyDeleteRepair()
{
	string ra = "";
	string sc = "";
	if(Request.QueryString["radel"] != "" && Request.QueryString["radel"] != null)
	{
		ra = Request.QueryString["radel"];
		sc = "UPDATE repair ";
		sc += " SET bdelete = 1";
		sc += ", status = 6 ";
		sc += " WHERE ra_number = '"+ ra +"' ";
		//DEBUG(" sc =", sc);
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
//			ShowExp(sc, e);
			return false;
		}	
	}
	if(Request.QueryString["res"] != "" && Request.QueryString["res"] != null)
	{
		ra = Request.QueryString["res"];
		sc = "UPDATE repair ";
		sc += " SET bdelete = 0";
		sc += ", status = 3 ";
		sc += " WHERE ra_number = '"+ ra +"' ";
		//DEBUG(" sc =", sc);
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
//			ShowExp(sc, e);
			return false;
		}	
	}
	return true;
}


</script>
<asp:Label id=LFooter runat=server/>

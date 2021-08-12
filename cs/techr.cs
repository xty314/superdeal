<!-- #include file="page_index.cs" -->

<script runat=server>

int m_jobrows = 0;
string m_sOptions = "0";
string m_sSystem = "2";
string m_search = "";

string ra_conditions = "";
string ra_header = "";
string ra_packslip = "";

bool m_bFound = false;
bool bClicktoView = true;

DataSet dst = new DataSet();
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("technician"))
		return;
	InitializeData();
	
//	ra_header = ReadSitePage("repair_header");
//	ra_conditions = ReadSitePage("repair_condition");
//	ra_packslip = ReadSitePage("repair_pickup_slip");

	if(Request.QueryString["s"] != "" && Request.QueryString["s"] != null)
		m_sSystem = Request.QueryString["s"];
	if(Request.QueryString["op"] != "" && Request.QueryString["op"] != null)
		m_sOptions = Request.QueryString["op"];
	if(Request.Form["search"] != "" && Request.Form["search"] != null)
		m_search = Request.Form["search"];
	if(Request.QueryString["sh"] != null && Request.QueryString["sh"] != "")
		m_search = Request.QueryString["sh"];

	if(Request.QueryString["src"] != null && Request.QueryString["src"] != "")
		m_search = Request.QueryString["src"];
	if(!TSIsDigit(m_sSystem))
			m_sSystem = "2";
	
	if(Request.QueryString["del_id"] != null && Request.QueryString["del_id"] != "")
	{
		if(!doDeleteRepair(true))
			return;
		Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?op="+ m_sOptions +"&s="+ m_sSystem +"') </script");
		Response.Write(">");
		return;
		
	}
	if(Request.QueryString["pdel_id"] != null && Request.QueryString["pdel_id"] != "")
	{
		if(!doDeleteRepair(true))
			return;
		Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?op="+ m_sOptions +"&s="+ m_sSystem +"') </script");
		Response.Write(">");
		return;
		
	}
	if(Request.QueryString["res_id"] != null && Request.QueryString["res_id"] != "")
	{
		if(!doDeleteRepair(false))
			return;
		Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?op="+ m_sOptions +"&s="+ m_sSystem +"') </script");
		Response.Write(">");
		return;
		
	}
	if(Request.Form["cmd"] == "Approve" || Request.Form["cmd"] == "Received")
	{
		if(!doUpdateRepair())
			return;
	}

	if(Request.Form["cmd"] == "Update Repair" || Request.Form["cmd"] == "Send to Supplier" || Request.Form["cmd"] == "Out of Warranty")
	{
		if(!doUpdateRepair())
			return;
	}
	if(Request.Form["cmd"] == "Search Repair" && ((Request.Form["rp_sn"] != null && Request.Form["rp_sn"] != "")
		||(Request.Form["rp_code"] != null && Request.Form["rp_code"] != "")) 
		||(Request.QueryString["sltcode"] != null && Request.QueryString["sltcode"] != ""))
	{
		if(!doSearchReplaceItem())
			return;
	}
	if(Request.Form["cmd"] == "Search Repair" || Request.Form["search"] != null && Request.Form["search"] != "" 
		||(Request.QueryString["src"] != null && Request.QueryString["src"] != ""))
	{
		if(Request.QueryString["src"] != "" && Request.QueryString["src"] != null)
			m_search = Request.QueryString["src"];
		if(Request.Form["search"] != "" && Request.Form["search"] != null)
			m_search = Request.Form["search"];
	
	}
	
	if(!getRepairJob())
		return;
	RepairGrid();
	
}


bool doSearchReplaceItem()
{
	string sc = "";
	int rows = 0;
	string sr_code = "";
	if(Request.Form["rp_code"] != null && Request.Form["rp_code"] != "" ) 
		sr_code = Request.Form["rp_code"];
	if(Request.QueryString["sltcode"] != null && Request.QueryString["sltcode"] != "")
		sr_code = Request.QueryString["sltcode"];
	
	//DEBUG("sr_code= ", sr_code);
	if(Request.Form["rp_sn"] != null && Request.Form["rp_sn"] != "")
	{
		sc = " SELECT rs.replaced_sn AS sn, c.code, c.name";
		sc += " FROM return_sn rs JOIN code_relations c ON c.code = rs.code ";
		sc += " WHERE rs.replaced_sn = '" + Request.Form["rp_sn"] +"' ";
	
	}
	else if(sr_code != null && sr_code != "")
	{
		sc += " SELECT p.code, p.name, '' AS sn FROM product p ";
		//if(g_bRetailVersion)
			sc += " JOIN stock_qty sq ON sq.code = p.code ";
		
		if(TSIsDigit(sr_code))
			sc += " WHERE p.code = '" + sr_code +"' ";
	}
	//DEBUG("code =",  Request.Form["rp_code"]);
//DEBUG("sc = ", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "rp_item");
		if(rows < 0)
		{
			if(Request.Form["rp_sn"] != null && Request.Form["rp_sn"] != "")
			{
				sc = " SELECT s.sn, s.product_code AS code, s.prod_desc AS name";
				sc += " FROM stock s ";
				sc += " WHERE s.sn = '" + Request.Form["rp_sn"] +"' ";
				
			}
			else if(sr_code != null && sr_code != "")
			{
				sc += " SELECT p.code, p.name, '' AS sn FROM product p ";
				//if(g_bRetailVersion)
					sc += " JOIN stock_qty sq ON sq.code = p.code ";
				
				if(TSIsDigit(sr_code))
					sc += " WHERE p.code = '" + sr_code +"' ";
			}
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				rows = myAdapter.Fill(dst, "rp_item");
			}
			catch (Exception e)
			{
				ShowExp(sc,e);
				return false;
			}
		}
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	
	if(rows <= 0)
	{
		if(Request.Form["ticket"] == "" && Request.Form["ticket"] == null)
		{
		Response.Write("<script language=javascript>");
		Response.Write("window.alert('No Item Found')\r\n");
		Response.Write("</script");
		Response.Write(">");
		}
		Session["rp_sn"] = null;
		Session["rp_code"] = null;
		Session["rp_pname"] = null;
	}	
	if(rows == 1)
	{
		Session["rp_sn"] = dst.Tables["rp_item"].Rows[0]["sn"].ToString();
		Session["rp_code"] = dst.Tables["rp_item"].Rows[0]["code"].ToString();
		Session["rp_pname"] = dst.Tables["rp_item"].Rows[0]["name"].ToString();
		m_bFound = true;
	}
	
	return true;
}

bool doUpdateTicket()
{
	if(Request.Form["ticket"] != "" && Request.Form["ticket"] != null)
	{
	string sc = " UPDATE repair ";
	sc += " SET status = 5 ";
	//if(Request.Form["ticket"] != "" && Request.Form["ticket"] != null)
		sc += ", ticket = '" + Request.Form["ticket"] +"' ";
	sc += " WHERE ra_number = '"+ Request.Form["hide_raid"] +"' ";
	//DEBUG("sc = ", sc);
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
	return true;
}

bool doDeleteRepair(bool bDelete)
{
	string id = "";
	string sc = "";
	if(Request.QueryString["pdel_id"] != "" && Request.QueryString["pdel_id"] != null && bDelete)
	{
		id = Request.QueryString["pdel_id"];
		sc = "DELETE repair ";
		//sc += " SET status = 6";
		sc += " WHERE id = '"+ id +"' ";
		sc += AddRepairLogString("Permanently Delete Repair Job ","", "", id, "","");
	}
	else
	{
	if(bDelete)
	{
		id = Request.QueryString["del_id"];
		sc = "UPDATE repair ";
		sc += " SET bdelete = 1";
		sc += ", status = 6 ";
		sc += " WHERE id = '"+ id +"' ";
		sc += AddRepairLogString("Delete Repair Job ","", "", id, "","");
	}
	else
	{
		id = Request.QueryString["res_id"];
		sc = "UPDATE repair ";
		sc += " SET bdelete = 0";
		sc += ", status = 3 ";
		sc += " WHERE id = '"+ id +"' ";
		sc += AddRepairLogString("Restore Repair Job ","", "", id, "","");
	}
	}
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
		ShowExp(sc, e);
		return false;
	}

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


bool doUpdateRepair()
{
	string status = Request.Form["status"];
	string note = Request.Form["note"];
	string id = Request.Form["hide_id"];
    string ra_number = Request.Form["hide_raid"];
	string invoice = "";
	string sn = "";
	string pur_date = "";
	string code = "";
	if(Request.Form["hide_inv"] != null && Request.Form["hide_inv"] != "")
		invoice = Request.Form["hide_inv"];
	if(Request.Form["hide_sn"] != null && Request.Form["hide_sn"] != "")
		sn = Request.Form["hide_sn"];	
	note = msgEncode(note);	
	int rows = int.Parse(Request.Form["hide_rows"]);
	string mf_pn = Request.Form["mf_pn"];

	string row_id = "";
	string approved = "";
	string sc = "";
	string received = "";
	//DEBUG("statust =", status);
	//DEBUG("id =", id);
	if(Request.Form["cmd"] == "Update Repair" || Request.Form["cmd"] == "Out of Warranty" || Request.Form["cmd"] == "Send to Supplier")
	{
		if(Request.Form["inv"] != null && Request.Form["inv"] != "")
			invoice = Request.Form["inv"];
		if(Request.Form["code"] != null && Request.Form["code"] != "")
			code = Request.Form["code"];
		if(Request.Form["p_date"] != null && Request.Form["p_date"] != "")
			pur_date = Request.Form["p_date"];
		string old_sn = Request.Form["old_sn"].ToString();
		string customer_id = Request.Form["dealer"];
		//DEBUG("m_sOption =", m_sOptions);
		if(Request.Form["cmd"] == "Send to Supplier")
			note = "Faulty product send to supplier on "+ DateTime.Now.ToString("dd-MMM-yy")+" \rWaitnig for return\r\n" +note; 
		if(Request.Form["cmd"] == "Out of Warranty")
			note = "This item is out of warranty \r\n" + note; 
		sc = " BEGIN TRANSACTION ";
		sc += "UPDATE repair ";
		if(Request.Form["cmd"] == "Out of Warranty")
			sc += " SET status = 5";
		else
			sc += " SET status = '"+ status +"'";
		sc += " ,supplier_code = '"+ mf_pn +"' ";
		sc += " ,customer_id = '"+ customer_id +"' ";
		sc += " , technician = '"+ Session["name"].ToString() +"' ";
		sc += " ,note = '"+ StripHTMLtags(note) +"', serial_number='"+ EncodeQuote(old_sn) +"' ";
		sc += ", purchase_date = '"+ pur_date +"' , code = '"+ code +"' , invoice_number = '" + invoice +"' ";
		if(status == "5")
			sc += " ,repair_finish_date = getdate() ";
		sc += " WHERE id = '"+ id +"' ";
		if(Request.Form["cmd"] == "Send to Supplier")
			sc += AddRepairLogString("Send Repair Job to supplier: "+ invoice +" Desc: "+ note +"", "", "", ra_number, sn, "");
		else
			sc += AddRepairLogString("Update Repair Job: "+ invoice +" Desc: '"+ note +"'", "", "", ra_number, sn, "");
		sc += " COMMIT ";
	//DEBUG("sc = ", sc);
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
		Session["slt_dealer_id"] = null;
		Session["slt_dealer_name"] = null;
	}
	
	if(Request.Form["cmd"] == "Approve" || Request.Form["cmd"] == "Received")
	{
		for(int i=0; i<=rows; i++)
		{	
			string reason = "";
			if(Request.Form["row_id"+i.ToString()] != null && Request.Form["row_id"+i.ToString()] != "")
				row_id = Request.Form["row_id"+i.ToString()];
			if(Request.Form["approved"+i.ToString()] != null || Request.Form["approved"+i.ToString()] != "")
				approved = Request.Form["approved"+i.ToString()];
			if(Request.Form["received"+i.ToString()] == "on")
				received = Request.Form["received"+i.ToString()];
			if(Request.Form["reason"+i.ToString()] != null && Request.Form["reason"+i.ToString()] != "")
				reason = Request.Form["reason"+i.ToString()];
			string email = Request.Form["email"+i.ToString()];
			string customer = Request.Form["customer"+i.ToString()];
			
			string desc ="";	string row_sn ="";		
			string row_invoice ="";		string r_date =""; 
			
			string row_raid = Request.Form["ra_id"+i.ToString()];
			
			if(Request.Form["desc"+i.ToString()] != null && Request.Form["desc"+i.ToString()] != "")
				desc = Request.Form["desc"+i.ToString()];
			if(Request.Form["sn"+i.ToString()] != null && Request.Form["sn"+i.ToString()] != "")
				row_sn = Request.Form["sn"+i.ToString()];
			if(Request.Form["invoice"+i.ToString()] != null && Request.Form["invoice"+i.ToString()] != "")
				row_invoice = Request.Form["invoice"+i.ToString()];
			if(Request.Form["r_date"+i.ToString()] != null && Request.Form["r_date"+i.ToString()] != "")
				r_date = Request.Form["r_date"+i.ToString()];
		
			if((approved != null && approved != "") || (received == "on"))
			{
				//DEBUG("approved = ", approved);
				//DEBUG("rows id = ", row_id);
				sc = " BEGIN TRANSACTION ";
				sc += "UPDATE repair ";
				if(approved == "1")
					sc += " SET status = '2'";
				if(approved == "0")
				{
					sc += " SET status = '6'";
					sc += " ,note = '"+ reason +"'";
					sc += " , authorize_date = getdate() ";
				}
				if(received == "on")
					sc += " SET status = '3', received_date = getdate() ";
				sc += ", technician = '" + Session["name"] +"' ";
				sc += " WHERE id = '"+ row_id +"' ";
				if(approved == "1")
					sc += AddRepairLogString("Repair Job Approved: '"+ row_invoice +"' Desc: '"+ desc +"'", "", "", row_raid, row_sn, "");
				if(approved == "0")
					sc += AddRepairLogString("Update Repair Job: '"+ row_invoice +"' reason: '"+ reason +"'", "", "", row_raid, row_sn, "");
				sc += " COMMIT ";
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
				
				if(approved == "1" || approved == "0")
				{
					MailMessage msgMail = new MailMessage();
					msgMail.To = email;
					msgMail.BodyFormat = MailFormat.Html;
					msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
					string items = " Faulty Items: invoice :" + row_invoice +" <br>";
					items += "		sn :" + row_sn +" <br>";
					items += "		description :" + desc +" <br>";
					items += "		repair date :" + r_date +" <br>";
					
					if(approved == "1")
					{
						msgMail.Subject = "RMA# " + row_raid + " Authorized";
						msgMail.Body = "Dear " + customer + ":<br><br>";
						msgMail.Body += "Your RA Application has been authorized RMA# "+ row_raid +", please send faulty product to us.<br>";
						msgMail.Body += items;
					}
					if(approved == "0")
					{
						msgMail.Subject = "RMA# " + row_raid + " unAuthorized";
						msgMail.Body = "Dear " + customer + ":<br><br>";
						msgMail.Body += "Your RA Application has not been authorized, <br>";
						msgMail.Body += items;
						msgMail.Body += "Reason : "+ reason +" <br>";
					}

					//msgMail.Body += "Also, You can view details or trace status on http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/repair.aspx?s=view\r\n\r\n";
					msgMail.Body += "Also, You can view details or trace status on http://" + Request.ServerVariables["SERVER_NAME"] + "/repair.aspx?s=view<br><br>";
					msgMail.Body += "Regards.<br><br>";
					msgMail.Body += m_sCompanyTitle + "<br>";
					msgMail.Body += DateTime.Now.ToString("MMM.dd.yyyy");
					SmtpMail.Send(msgMail);
					//DEBUG("msgMail=", msgMail.Body.ToString());		
				}
			}
		}
	}
	return true;
}

void RepairGrid()
{
	DataRow dr;
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["rJobs"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 26;
	m_cPI.URI = "?s="+ m_sSystem +"&op="+ m_sOptions +"";
	if(m_search != "")
		m_cPI.URI += "&src="+ m_search;
	
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	Response.Write("<br>");
	Response.Write("<form name=frm method=post >");
	Response.Write("<table width=99% align=center cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th colspan=2 align=left><h5>Repairing List</th>");
	Response.Write("<td colspan=1><input type=text name=search value='"+ m_search +"'><input type=submit name=cmd value='Search Repair'"+Session["button_style"]+">");
	Response.Write("<script language='javascript' ");
	Response.Write(">");
	Response.Write("document.frm.search.focus();");
	Response.Write("</script");
	Response.Write(">");
	Response.Write("<input type=button value='Show All' "+ Session["button_style"] +" onclick=\"window.location=('techr.aspx?s=2&op=0&r="+ DateTime.Now.ToOADate() +"') \" >");
	Response.Write("</th>");
	Response.Write("<td colspan=3 align=right>");
	Response.Write("System: <select name=slt onchange=\"window.location=('techr.aspx?op="+m_sOptions+"&s='+this.options[this.selectedIndex].value)\" >");
	string s_dName1 = "", s_dName = "";
	string system = "";
	for(int ii = 2; ii>=0; ii--)
	{
		if(ii == 0)
			system = "Parts";
		if(ii == 1)
			system = "Complete System";
		
		if(ii == 2)
			system = "All";
		Response.Write("<option value='"+ ii +"' ");
		
		if(int.Parse(m_sSystem) == ii)
		{
			Response.Write(" selected ");
			s_dName1 = system;
		}
		Response.Write(" > "+ system +"</option>");
	}
	Response.Write("</select>");
	Response.Write(" Status:<select name=options onchange=\"window.location=('techr.aspx?s="+m_sSystem+"&op='+ this.options[this.selectedIndex].value)\" ");
	Response.Write(">");
	for(int ii=0; ii<7; ii++)
	{
		string soption = "";	
		if(ii == 0)
			soption = "All";
		if(ii == 1)
			soption = "Waiting Authorize";
		else if(ii == 2)
			soption = "Authorized";
		else if(ii == 3)
			soption = "Received & Repair";
		//else if(ii == 4)
		//	soption = "Repairing";
		else if(ii == 4)
			soption = "Repair Done";
		else if(ii == 5)
			soption = "Gone";
		else if(ii == 6)
			soption = "Deleted";
		if(int.Parse(m_sOptions) == ii)
		{
			Response.Write("<option value='"+ ii +"' ");
			Response.Write(" selected> "+ soption +"</option>");
			s_dName = soption;
		}
		else
			Response.Write("<option value='"+ ii +"' > "+ soption +"</option>");
	}
	Response.Write("</select>");
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left>Systems:: "+s_dName1+"</th><th colspan=4><font size=2 color=#F56699>- "+s_dName+" -</font></th></tr>");
	Response.Write("<tr><td colspan=10>");
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=10>"+ sPageIndex +"</td></tr>");
	Response.Write("<tr bgcolor=#CAEDED>");
	if(m_sOptions == "1")
		Response.Write("<th colspan=1>APPROVED|DECLINED</th>");
	else if(m_sOptions == "2")
		Response.Write("<th colspan=1>RECEIVED??</th>");
	else
		Response.Write("<th>RMA#</th>");
	Response.Write("<th></th><th>CUSTOMER</th><th>DESCRIPTION</th><th>INVOICE#</th><th>SN#</th><th>REPAIR DATE</th>");
//	if(m_sOptions == "0")
	Response.Write("<th>STATUS</th><th></th>");
	Response.Write("<th>ACTION</th></tr>");
	bool bAlter = true;
	string uri = "";
	int nrows = 0;
	if(rows == 1)
		i = 0;
	for(; i<rows && i<end; i++)
	{
		dr = dst.Tables["rJobs"].Rows[i];
		string id = dr["id"].ToString();
		string ra_id = dr["ra_number"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string sn = dr["serial_number"].ToString();
		string r_date = dr["repair_date"].ToString();
		string name =  dr["cname"].ToString();
		string addr1 = dr["addr1"].ToString();
		string addr2 = dr["addr2"].ToString();
		string city = dr["ccity"].ToString();
		string phone = dr["cphone"].ToString();
		string fax = dr["cfax"].ToString();
		string email = dr["cemail"].ToString();
		string desc = dr["prod_desc"].ToString();
		string card_id = dr["customer_id"].ToString();
		string fault = dr["fault_desc"].ToString();
		string note = dr["note"].ToString();
		string finish_date = dr["repair_finish_date"].ToString();
		string replaced = dr["replaced"].ToString();
		string ticket = dr["ticket"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string p_date = dr["purchase_date"].ToString();
		string code = dr["code"].ToString();
		string customer_id = dr["customer_id"].ToString();
		string status = dr["status"].ToString();
		string rs_sn = "";
		string rs_code = "";
		string rs_desc = "";
		string rs_id = "";
		string rs_raid = "";
		//DEBUG("replacedd =", replaced);
		if(name == "" && name == null)
			name = dr["ctrading_name"].ToString();
		if(name == "" && name == null)
			name = dr["ccompany"].ToString();
		string company = dr["ccompany"].ToString();

		int nstatus = int.Parse(dr["status"].ToString());
		//string eStatus = dr["estatus"].ToString();
		//DEBUG("n status =", nstatus);
		Response.Write("<tr");
		if(!bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(">");
		bAlter = !bAlter;
	
		uri = Request.ServerVariables["URL"]+"?op="+m_sOptions+"&s="+m_sSystem+"&id="+ id +"&p="+ m_cPI.CurrentPage +"&spb="+ m_cPI.StartPageButton +"";
		if(Request.QueryString["id"] == id  )
			uri = Request.ServerVariables["URL"]+"?op="+m_sOptions+"&s="+m_sSystem+"&p="+ m_cPI.CurrentPage +"&spb="+ m_cPI.StartPageButton;
		if(m_search != null && m_search != "")
			uri += "&src="+ HttpUtility.UrlEncode(m_search);
		
		Response.Write("<td>");
		if(nstatus == 1)
		{
			Response.Write("<input type=radio name='approved"+i.ToString()+"' value='1'>YES: ");
			Response.Write("<input type=text size=15% readonly value='RMA#: "+ra_id+"'><br>");
			Response.Write("<input type=radio name='approved"+i.ToString()+"' value='0'>NO: &nbsp;<input type=text name='reason"+i.ToString()+"' value='reason'>");
		}
		else if(nstatus == 2)
		{
			Response.Write("<input type=radio name='received"+i.ToString()+"'>Received? ");
			Response.Write("RMA#:");
		}
		
			//Response.Write("<a title='click me to process repair' href='"+uri+"' class=o> "+ra_id+"</a>&nbsp;");
		//	Response.Write(""+ra_id+"&nbsp;");
		//if(nstatus != 1)
			Response.Write(" "+ra_id+"");
		Response.Write("</td><td>");
			Response.Write(" <a href=repair.aspx?print=form&ra=" + HttpUtility.UrlEncode(ra_id) + "&confirm=1&email=" + HttpUtility.UrlEncode(email));
			
			Response.Write(" class=o target=_blank title='Email Repair Form to customer'><font color=Green><b>EM</b></font></a>&nbsp;");
			//Response.Write("<a title='email job to customer' href='"+uri+"' class=o><font color=Green><b>EM</b></font></a>&nbsp;");
			Response.Write("<a title='Print Repair Form' href='repair.aspx?print=form&ra="+ HttpUtility.UrlEncode(ra_id)+"&ty=1' target=_blank ");
			//Response.Write("<a title='Print Repair Form' href=\"javascript:form_window=window.open('repair.aspx?print=form&ra="+ HttpUtility.UrlEncode(ra_id)+"&ty=1', '', ''); form_window.focus();\"' ");
			Response.Write(" class=o><font color=#38B2D5><b>PRF</b></font></a>&nbsp;");
			if(nstatus >=4)
			{
				Response.Write("<a title='Print Finish Form' href='repair.aspx?print=form&ra="+HttpUtility.UrlEncode(ra_id)+"&ty=2' target=_blank ");
				//Response.Write("<a title='Print Finish Form' href=\"javascript:form_window=window.open('repair.aspx?print=form&ra="+HttpUtility.UrlEncode(ra_id)+"&ty=2', '', ''); form_window.focus();\"' ");
			//	Response.Write(" class=o><font color=#C97907><b>FRF</b></font></a>&nbsp;");
			}
			Response.Write("<a title='View Repair Log' href=\"javascript:log_window=window.open('repair_log.aspx?job="+ra_id+"', '', 'scrollbars=1, resizable=1, width=500, height=350'); form_window.focus();\"' ");
			Response.Write(" class=o><font color=RED><b>RLOG</b></font></a>&nbsp;");
		
		Response.Write("</td>");
		Response.Write("<td><a title='click me to view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id="+card_id+"','', 'width=350,height=350'); viewcard_window.focus();\" class=o> ");
		if(g_bRetailVersion)
			Response.Write(""+name+"</a></td>");
		else
			Response.Write(""+company+"</a></td>");
		//Response.Write("<td><a title='click me to process repair' href='"+uri+"' class=o> "+desc+"</a></td>");
		Response.Write("<td>"+desc+"</td>");
		Response.Write("<td><a title='click me to view invoice details' href='invoice.aspx?i="+invoice+"', '', width=654 height=455 class=o target=_blank> "+invoice+"</a></td>");
		Response.Write("<td><a title='click me to view SN# trace' href='snsearch.aspx?sn="+sn+"' class=o target=new> "+sn+"</a></td>");
		//Response.Write("<td>"+ supplier_code +"</td>");
		Response.Write("<td align=center> "+DateTime.Parse(r_date).ToString("dd-MMM-yy")+"</td>");
		Response.Write("<td>");
		if(m_sOptions == "0")
			Response.Write(GetEnumValue("rma_status", status).ToUpper());
		Response.Write("</td><td>");
		Response.Write("<table border=0 width=100%  align=center>");
		Response.Write("<tr><td size=50%>");
		
		if(MyBooleanParse(dr["for_supp_ra"].ToString()))
			Response.Write("<a title='faulty item has sent to supplier' href='ra_supplier.aspx?id="+ id +"&r="+ DateTime.Now.ToOADate() +"' class=o >SSP</a>");
		else
			Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td>");
		//if(replaced == "1")
		if(!MyBooleanParse(dr["for_supp_ra"].ToString()))
			Response.Write("<td><a title='faulty item already replaced' href='ra_replace.aspx?id="+ id +"&r="+ DateTime.Now.ToOADate() +"' class=o >RPL</a>");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write("<a title='click me to process repair' href='"+uri+"' class=o><b>PRO</b></a> ");
		//if(int.Parse(m_sOptions) >= 3)
		if(int.Parse(status) >= 3)
			Response.Write(" <a title='input freight ticket number' href='ra_freight.aspx?rid="+ HttpUtility.UrlEncode(ra_id) +"&ty=1' class=o><b>FTK</b></a>");
		if(m_sOptions == "6")
		{
			Response.Write("&nbsp;&nbsp;&nbsp;<a title='Restore This Job' href='techr.aspx?s="+m_sSystem+"&op="+m_sOptions+"&res_id="+id+"' class=o><font color=red><b>RES</b></a>");
			Response.Write("&nbsp;&nbsp;&nbsp;<a title='Delete This Job Permanently' href='techr.aspx?s="+m_sSystem+"&op="+m_sOptions+"&pdel_id="+id+"' class=o onclick=\"return window.confirm('are you sure want to DELET THIS JOB PERMANENTLY')\"><font color=red><b>X</b></a>");
		}
		else
			Response.Write("&nbsp;&nbsp;<a title='Delete This JOB' href='techr.aspx?s="+m_sSystem+"&op="+m_sOptions+"&del_id="+id+"' class=o onclick=\"if(!confirm('Are you Sure want to do this...')){return false;}\"><font color=red><b>X</b></a>");
		
		Response.Write("</td>");
		Response.Write("</tr>");
//DEBUG("id = ", id);
		if(Request.QueryString["id"] == id)
		{
			
			Response.Write("<tr><td colspan=10>");
			Response.Write("<table width=94% align=center cellspacing=1 cellpadding=3 border=0 bordercolor=gray bgcolor=EEEEE");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td colspan=4>");
			Response.Write("<table width=50% border=0>");
			string slt_uri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
			Response.Write("<tr><td><b>Customer Details:</b></td>");
			//Response.Write("<tr><td colspan=2><select name=dealer ");
			if(Session["slt_dealer_id"] != null && Session["slt_dealer_id"] != "")
			{
				customer_id = Session["slt_dealer_id"].ToString();
				company = Session["slt_dealer_name"].ToString();
			}
			Response.Write("<td><select name=dealer ");
			Response.Write(" onclick=\"window.location=('slt_dealer.aspx?uri="+ HttpUtility.UrlEncode(slt_uri) +"')\" >");
			Response.Write("<option value='"+ customer_id +"'>");
			if(g_bRetailVersion)
				Response.Write(name);
			else
				Response.Write(company);
			Response.Write("</option>");
			//Response.Write("<option value='"+ customer_id +"'>"+ company +"</option>");
			Response.Write("</select>");
			Response.Write("&nbsp;&nbsp;<a title='click me to view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id="+ customer_id +"','', 'width=350,height=350'); viewcard_window.focus();\" class=o>");
			Response.Write("who??</a>");
			Response.Write("</td></tr>");
					
			Response.Write("</table></td></tr>");
			Response.Write("<tr><td colspan=4><hr size=1 color=black></td></tr>");
		
			Response.Write("<tr><th align=right>M_PN:</td><td><input type=text name=mf_pn value='"+ supplier_code +"' >");
			Response.Write("</td><th align=left>Prod_Desc: <input size=75% type=text name=prod_desc value='"+ desc +"'>");
			Response.Write("</td>");
	
			Response.Write("</tr>");

			Response.Write("<tr><th align=right>Purchase Date:</td><td><input type=text name=p_date value='"+ p_date +"' ");
			Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.p_date','calendar_window','width=190,height=230');calendar_window.focus()\" >");
			Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.p_date','calendar_window','width=190,height=230');calendar_window.focus()\" class=o>...</a>");
			Response.Write("</td>");
			Response.Write("<th colspan=2 align=left>Code: <input type=text  maxlength=12  size=8% name=code value='"+ code +"'>");
			Response.Write("&nbsp;&nbsp;INV#: <input type=text maxlength=12 name=inv value='"+ invoice +"'></td>");
			Response.Write("&nbsp;&nbsp;SN#: <input size=28% type=text name=old_sn value='"+ sn +"'></td>");
			Response.Write("</tr>");
			Response.Write("<tr><th align=right>Fault Desc:</th>");
			Response.Write("<td colspan=3><textarea readonly rows=4 cols=107>"+ fault +"</textarea></td></tr>");
			//if(int.Parse(m_sOptions) >2)			
			{
				Response.Write("<tr><th align=right>Repair Desc:</th>");
				Response.Write("<td colspan=3><textarea name=note rows=8 cols=107>"+note+"</textarea>");
				Response.Write("</td></tr>");
				Response.Write("<tr><th align=right>Status:</th><td colspan=3>");
				int nrow = int.Parse(m_sOptions);
				Response.Write("<select name=status ");
				if(m_sOptions == "1" || m_sOptions == "2")
					Response.Write(" disabled ");
				Response.Write(" >");
				Response.Write(GetEnumOptions("rma_status", nstatus.ToString()));
				Response.Write("</select>&nbsp;");
				string Enable_button_rma_tecr = GetSiteSettings("Enable_button_rma_tecr");
				
				Response.Write("<input type=submit name=cmd value='Update Repair' "+Session["button_style"] +">");
				if(Enable_button_rma_tecr =="1"){
				Response.Write("<input type=submit name=cmd value='Out of Warranty' "+Session["button_style"] +" onclick=\"if(!confirm('Are you sure want to process this faulty item')){return false;}\">");
				Response.Write("<input type=button value='Send to Supplier' "+Session["button_style"] +" onclick=\"window.location=('ra_supplier.aspx?id="+ id +"')\">");
	
				if((dr["for_supp_ra"].ToString()).ToLower() != "true")
					Response.Write("<input type=button value='Item Replacement' "+Session["button_style"] +" onclick=\"window.location=('ra_replace.aspx?id="+ id +"')\">");
					}
			//	DEBUG("status =", status);
				if(int.Parse(status) >= 3)
					Response.Write("<input type=button title='input freight ticket number' value='FTK' "+Session["button_style"] +" onclick=\"window.location=('ra_freight.aspx?rid="+ HttpUtility.UrlEncode(ra_id) +"&ty=1')\">");
					

				Response.Write("</td></tr>");
			
				Response.Write("</td>");
				Response.Write("</tr>");
				Response.Write("<input type=hidden name=hide_id value='"+id+"'>");
				Response.Write("<input type=hidden name=hide_invoice value='"+invoice+"'>");
				Response.Write("<input type=hidden name=hide_sn value='"+sn+"'>");
				Response.Write("<input type=hidden name=hide_raid value='"+ra_id+"'>");
			}

			//if(int.Parse(m_sOptions) != 4)
			//	Response.Write("<tr><th align=right>Delivery Ticket:</th><td colspan=3><input type=text name=ticket value='"+ ticket +"'>");
			//else
			//	Response.Write("<input type=submit name=cmd value='Input Delivery Ticket' "+ Session["button_style"] +"></td></tr>");
				
			Response.Write("</table><br></td></tr>");
		}
		else
		{
			Session["slt_dealer_id"] = null;
			Session["slt_dealer_name"] = null;
		}
		Response.Write("<input type=hidden name='row_id"+i.ToString()+"' value='"+id+"'>");
		Response.Write("<input type=hidden name='email"+i.ToString()+"' value='"+email+"'>");
		Response.Write("<input type=hidden name='customer"+i.ToString()+"' value='"+name+"'>");
		Response.Write("<input type=hidden name='desc"+i.ToString()+"' value='"+desc+"'>");
		Response.Write("<input type=hidden name='invoice"+i.ToString()+"' value='"+invoice+"'>");
		Response.Write("<input type=hidden name='sn"+i.ToString()+"' value='"+sn+"'>");
		Response.Write("<input type=hidden name='ra_id"+i.ToString()+"' value='"+ra_id+"'>");
		Response.Write("<input type=hidden name='r_date"+i.ToString()+"' value='"+r_date+"'>");
		//DEBUG("id = ",id);
		nrows = i;
	}

	Response.Write("<input type=hidden name=hide_rows value='"+nrows+"'>");
	if(m_sOptions == "1")
	{
		Response.Write("<tr><td colspan=10><input type=submit name=cmd value='Approve' "+ Session["button_style"] +"></td></tr>");
	}
	if(m_sOptions == "2")
	{
		Response.Write("<tr><td colspan=10><input type=submit name=cmd value='Received' "+ Session["button_style"] +"></td></tr>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("<br><br><br>");
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string s = @"
	function chkApproval() 
	{	
		return true;
	}
	";
	Response.Write("-->"); 
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> \r\n");
	
}

bool getRepairJob()
{
	string sc = " SET DATEFORMAT dmy SELECT DISTINCT r.*, c.name AS cname, c.trading_name AS ctrading_name, c.company AS ccompany, c.address1 AS addr1, c.address2 AS addr2, c.city AS ccity";
	sc += ", c.phone AS cphone, c.fax AS cfax, c.email AS cemail, e.name AS eStatus ";
//	if(Request.QueryString["op"] != null &Request.QueryString["op"] != "")
//			sc += " ,rs.id AS rs_id, rs.ra_id, rs.rs_date, rs.sn AS rs_sn, rs.code AS rs_code, c2.name, c2.email, rs.description ";

	sc += " FROM repair r JOIN card c ON c.id = r.customer_id ";
	sc += " JOIN enum e ON e.id = r.status ";
	
	sc += " WHERE e.class = 'rma_status' ";
	if(Request.QueryString["rpid"] != null && Request.QueryString["rpid"] != "")
		sc += " AND r.id = "+Request.QueryString["rpid"];

	if(Request.QueryString["s"] != null && Request.QueryString["s"] != "" && Request.QueryString["s"] != "2")
		if(TSIsDigit(m_sSystem))
			sc += " AND r.system = "+ m_sSystem;
//DEBUG("SYSTEM ", m_sSystem);
	if(m_search == "" || m_search == null)
		sc += " AND (r.repair_date BETWEEN DATEADD(month, -6, GETDATE()) AND GETDATE())";

	if((m_search != null && m_search != "")) // || (Request.QueryString["src"] != null && Request.QueryString["src"] != ""))
	{
		if(TSIsDigit(m_search))
			sc += " AND r.id = '"+ m_search +"' ";
		else
		{
			sc += " AND(r.ra_number LIKE '%"+ EncodeQuote(m_search) +"%' OR c.name LIKE '%"+ EncodeQuote(m_search) +"%' OR c.trading_name LIKE '%"+ EncodeQuote(m_search) +"%'";
			sc += " OR c.company LIKE '%"+ EncodeQuote(m_search) +"%' OR c.phone LIKE '%"+ EncodeQuote(m_search) +"%' ";
			sc += " OR r.note LIKE '%"+ EncodeQuote(m_search) +"%' )";
		}
	}
	else if(Request.QueryString["id"] != null && Request.QueryString["id"] != "" ) // && Request.Form["search"] == null || Request.Form["search"] == "")
		sc += " AND r.id = "+ Request.QueryString["id"];
	else
	{
		if(Request.QueryString["op"] != null && Request.QueryString["op"] != "" && Request.QueryString["op"] != "0")
			sc += " AND r.status = "+ m_sOptions;
		else
			sc += " AND r.status < 6";
	}
	/*if(m_sOptions == "1" || m_sOptions == "2")
		sc += " AND r.isinput = 0 "; //faulty items inputted
	else
		sc += " AND r.isinput = 1 ";
	*/
	sc += " ORDER BY r.repair_date DESC";
//DEBUG("s c= ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_jobrows = myAdapter.Fill(dst,"rJobs");
	}
	catch (Exception e) 
	{
		ShowExp(sc,e);
		return false;
	}

	return true;
}


</script>
<asp:Label id=LFooter runat=server/>
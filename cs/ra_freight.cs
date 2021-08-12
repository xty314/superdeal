<!-- #include file="page_index.cs" -->

<script runat=server>

string m_search = "";
string m_cust_query = "";
string m_supp_query = "";
string m_sdFrom = "";
string m_sdTo = "";
string m_id = "";
string m_command = "";
string m_rma_id = "";
string m_supplier_id = "";
string m_del_id = "";

string m_ra_supplier_id = "";
string m_supplier_ra = "";

/*//------current item to be processed
string mc_sn = "";
string mc_code = "";
string mc_supplier_code = "";
string mc_supplier_id = "";
string mc_customer_id = "";
string mc_po_id = "";
string mc_id = "";
string mc_stock = "";
string mc_name = "";
string mc_repair_id = "";
string mc_status = "";
string mc_condition = "";
string mc_old_sn = "";
*/

string uri = "";
bool m_bReplaced = false;
string[] m_EachMonth = new string[13];

DataSet dst = new DataSet();
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
		//monthly name
	m_EachMonth[0] = "JAN";
	m_EachMonth[1] = "FEB";
	m_EachMonth[2] = "MAR";
	m_EachMonth[3] = "APR";
	m_EachMonth[4] = "MAY";
	m_EachMonth[5] = "JUN";
	m_EachMonth[6] = "JUL";
	m_EachMonth[7] = "AUG";
	m_EachMonth[8] = "SEP";
	m_EachMonth[9] = "OCT";
	m_EachMonth[10] = "NOV";
	m_EachMonth[11] = "DEC";
	//----
	
	GetQueryString();
	if(!SecurityCheck("stockman"))
		return;
	
	if(m_command == "Continue")
	{
		if(DoInsertTicket())
		{
			InitializeData();
			DoCleanUpSession();
			DisplayPrintFormOptions();
			///Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='techr.aspx?r=" + DateTime.Now.ToOADate() + "'\">");
			return;
		}
	}
	if(Request.QueryString["drid"] != null && Request.QueryString["drid"] != "")
	{
//	DEBUG("m_rma_id =", m_rma_id);
		if(DoDeleteRows())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ uri +"?r=" + DateTime.Now.ToOADate() + "&rid="+ m_rma_id +"");
			if(Request.QueryString["ty"] != null && Request.QueryString["ty"] != "")
				Response.Write("&ty="+ Request.QueryString["ty"] +"");
			Response.Write("\">");
			return;
		}
	}
	if(m_command == "Input Ticket" || (Request.Form["ticket"] != null && Request.Form["ticket"] != ""))
	{
		if(DoAddTicketToSession())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ uri +"?r=" + DateTime.Now.ToOADate() + "&rid="+ m_rma_id +"");
			if(Request.QueryString["ty"] != null && Request.QueryString["ty"] != "")
				Response.Write("&ty="+ Request.QueryString["ty"] +"");
			Response.Write("\">");
			return;
		}
	}
	
	InitializeData();
	if(!DoCheckExistingRMA())
		return;

}


void DisplayPrintFormOptions()
{
	Response.Write("<br><br><center>");
	if(Request.QueryString["ty"] == "2")
	{
		Response.Write("<br><a title='Back to RMA Processing' href='supp_rma.aspx?r="+ DateTime.Now.ToOADate() +"&rma=rd&st=2&rid="+ m_rma_id +"' class=o><h4><< Back to Supplier RMA Process</h4></a>");
	//	Response.Write("<br><a title='print RMA basic form' href=\"javascript:ra_form_window=window.open('ra_form1.aspx?r="+ DateTime.Now.ToOADate() +"&print=form&ra="+ m_rma_id +"&sid="+ Session["rma_supplier_id"] +"', '','');  ra_form_window.focus()\" class=o><h4>Print Basic RMA FORM</h4></a>");
		Response.Write("<br><a title='print RMA form' href=\"javascript:ra_form_window=window.open('ra_form.aspx?r="+ DateTime.Now.ToOADate() +"&print=form&ra="+ m_rma_id +"&sid="+ Session["rma_supplier_id"] +"', '','');  ra_form_window.focus()\" class=o><h4>Print RMA FORM</h4></a>");
	}
	else
	{
		Response.Write("<br><a title='Print Repair Form' href='repair.aspx?r="+ DateTime.Now.ToOADate() +"&ra="+ m_rma_id +"&print=form&ty=1' class=o><h4>Print Repair Form</h4></a>");
		Response.Write("<br><a title='Print Finish Repair Form' href='repair.aspx?r="+ DateTime.Now.ToOADate() +"&ra="+ m_rma_id +"&print=form&ty=1' class=o><h4>Print Finish Form</h4></a>");
		Response.Write("<br><a title='Back to RMA Processing' href='techr.aspx?r="+ DateTime.Now.ToOADate() +"&src="+ m_rma_id +"&s=2&op=3' class=o><h4><< Back to Repair</h4></a>");
	}
	Response.Write("</center>");
}
bool DoCleanUpSession()
{
	if(Session["ticket_count"] != null && Session["ticket_count"] != "")
	{
		for(int i=1; i<=(int)Session["ticket_count"]; i++)
			Session["ticket" + i] = null;

		Session["ticket_count"] = null;
	}
	return true;
}

bool DoInsertTicket()
{
	if(Request.QueryString["rid"] == null && Request.QueryString["rid"] == "")
		return false;
string sc = "";
if(Session["ticket_count"] != null && Session["ticket_count"] != "")
{
	for(int i=1; i<=(int)Session["ticket_count"]; i++)
	{
		if(Session["ticket"+ i] != null && Session["ticket" + i] != "")
		{
			string ticket = EncodeQuote(Session["ticket"+ i].ToString());
			sc = " INSERT INTO ra_freight (depatch_date, ticket, price, ra_number, repair_number, ship_desc )";
			sc += " VALUES ( GETDATE(), '"+ ticket +"', 0 ";
			if(Request.QueryString["ty"] == "2")
				sc += ", '"+ Request.QueryString["rid"] +"', '' ";
			else
				sc += ", '', '"+ Request.QueryString["rid"] +"' ";
			sc += " , '" + StripHTMLtags(EncodeQuote(Request.Form["ship_by"])) +"' ";
			sc += ")";
	//	DEBUG("s c=", sc);
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
	if(Request.QueryString["ty"] == "2")
		sc = " UPDATE rma SET sent_date = GetDate(), check_status = 2, isticket=1, status=2  WHERE ra_id = '"+ Request.QueryString["rid"] +"' ";		
	else
		sc = " UPDATE repair SET isticket = 1, status = 5 WHERE ra_number = '"+ HttpUtility.UrlDecode(Request.QueryString["rid"]) +"' AND status > 3 ";
	
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

bool DoAddTicketToSession()
{
	string ticket = Request.Form["ticket"].ToString();

	int nSession = 0;
	bool IsDuplicate = false;
//DEBUG(" code = ", ticket);	
	if(ticket != null && ticket != "")
	{
	if(Session["ticket_count"] == null)
	{
		Session["ticket_count"] = 1;
		Session["ticket1"] = ticket;
		nSession = (int)Session["ticket_count"];
//DEBUG("ticket = ", Session["ticket1"].ToString());
	}
	else
	{
		nSession = (int)Session["ticket_count"] + 1;		
		for(int i=1; i<=nSession; i++)
		{	
			if(Session["ticket"+ i] != null)
			{
				if(ticket == Session["ticket"+ i].ToString())
					IsDuplicate = true;
			}
		}
		if(!IsDuplicate)
		{
			Session["ticket_count"] = nSession;
			Session["ticket"+ nSession] = ticket;
	//		DEBUG("ticket = ", Session["ticket"+ nSession].ToString());	
		//	for(int i=1; i<=nSession; i++)
		//		DEBUG("code = ", Session["ticket"+ i].ToString());	
		}
		
//		DEBUG("nSession=", (int)Session["ticket_count"]);
	}
	}
	
	return true;
}


bool DoDeleteRows()
{
	string ticket = "";
//DEBUG("coutn = ", Session["ticket_count"].ToString());
	if(Session["ticket"+ Request.QueryString["drid"]] != null)
	{
		Session["ticket"+ Request.QueryString["drid"]] = null;
		int noldrows = (int)Session["ticket_count"]; //old rows
		//int nrows = (int)Session["ticket_count"] - 1; //new rows		
		int nrows = 0;
		if(Session["ticket"+ Request.QueryString["drid"]] == null)
			nrows = int.Parse(Request.QueryString["drid"].ToString());
	
		for(int i=1; i<=noldrows; i++)
		{
			if(Session["ticket"+ i] != null)
				Session["ticket"+ i] = Session["ticket"+ i];
//		Response.Write("ticket ="+ Session["ticket"+ i]);
		}
	}
	else
	{
		string sc = "";
		if(Request.QueryString["drid"] != null && Request.QueryString["drid"] != "")
		{
			sc = " DELETE FROM ra_freight WHERE id = "+ Request.QueryString["drid"] +" ";
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

void GetQueryString()
{
	uri = Request.ServerVariables["URL"];
	if(Request.QueryString["id"] != "")
		m_id = HttpUtility.UrlEncode(Request.QueryString["id"]);
	if(Request.Form["cmd"] != "" && Request.Form["cmd"] != null)
		m_command = Request.Form["cmd"];
	if(Request.QueryString["sid"] != "")
		m_supplier_id = Request.QueryString["sid"];
	if(Request.QueryString["rid"] != "" && Request.QueryString["rid"] != null)
		m_rma_id = HttpUtility.UrlEncode(Request.QueryString["rid"]);
	if(Request.QueryString["del"] != "")
		m_del_id = Request.QueryString["del"];
	
	if(Request.Form["search"] != null && Request.Form["search"] != "")
		m_search = Request.Form["search"];

	if(m_search != null && m_search != "")
		m_rma_id = m_search;
//DEBUG("search =", m_search);
}

void GetFreightTicket()
{
	string rma_number = Request.QueryString["rid"];
	if(m_search != null && m_search != "")
		rma_number = m_search;
	Response.Write("<br><center><h4>Input Freight Ticket Number</h4></center>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table width=70% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td valign=top>");
	Response.Write("<table align=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<th align=right>RA#:</th><th align=left>"+ HttpUtility.UrlDecode(rma_number) +"</th>");
//	Response.Write("<th>&nbsp;</th><td valign=top rowspan=3>");
	
//	Response.Write("</td>");
	Response.Write("</tr>");
	Response.Write("<tr>");
	Response.Write("<th>Ticket#:</th><td align=left><input type=text name=ticket></td>");
	Response.Write("</tr>");
	Response.Write("<script language=javascript>document.frm.ticket.focus()</script");
	Response.Write(">");
	Response.Write("<tr><th colspan=2><input type=submit name=cmd value='Input Ticket' "+ Session["button_style"] +">");
	Response.Write("</td></tr>");
	Response.Write("</table></td>");
	Response.Write("<td valign=top>");
	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px; border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#CCCEEE><th colspan=5>INPUTTED TICKET NUMBER</th></tr>");
	
	bool bAlter = false;
	int nCt = 1;
//DEBUG(" uri = ", uri);
	for(int i=0; i<dst.Tables["ticket"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["ticket"].Rows[i];
		string id = dr["id"].ToString();
		string ticket = dr["ticket"].ToString();
		string ship_by = dr["ship_desc"].ToString();
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		Response.Write("><td>Ticket#"+ nCt++ +":</td><td>"+ ticket +"</td>");
		Response.Write("<td>"+ ship_by +"</td>");
		Response.Write("<td><font color=green>Last Ticket</td>");
		Response.Write("<td><a title='delete this ticket' href='"+ uri +"?rid="+ m_rma_id +"&drid="+ id +"");
		if(Request.QueryString["ty"] == "2")
			Response.Write("&ty=2");
		else
			Response.Write("&ty=1");
		Response.Write("' class=o><font color=red><b>X</b></font></td></tr>");
	}
	if(Session["ticket_count"] != null)
	{
		
		for(int i=1; i<=(int)Session["ticket_count"]; i++)
		{	
			if(Session["ticket"+ i] != null && Session["ticket" + i] != "")
			{
				Response.Write("<tr");
				if(bAlter)
					Response.Write(" bgcolor=#EEEEEE ");
				bAlter = !bAlter;
				Response.Write("><td>Ticket#"+ nCt++ +":</td><td>"+ Session["ticket"+ i] +"</td><td>&nbsp;</td>");
				Response.Write("<td><font color=blue>New Ticket</td>");
				Response.Write("<td><a title='delete this ticket' href='"+ uri +"?rid="+ Request.QueryString["rid"] +"&drid="+ i +"");
				if(Request.QueryString["ty"] == "2")
					Response.Write("&ty=2");
				else
					Response.Write("&ty=1");
				Response.Write("'><font color=red><b>X</b></font></td></tr>");
			}
		}
	}
	
	Response.Write("</table>");
	Response.Write("</td>");
	Response.Write("</tr>");

	Response.Write("<tr><th colspan=2>Ship By: ");
	GetFreightDescription();
	//Response.Write("<input type=text size=60 name=ship_by ><input type=submit name=cmd value='Continue' "+ Session["button_style"] +" ");
	Response.Write("<input type=submit name=cmd value='Continue' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\" if(document.frm.ship_by.value == '' || document.frm.ship_by.value == null){window.alert('Please Define a Courier Company'); return false;}\" ");
	Response.Write("></td></tr>");
	Response.Write("</table>");
	
	Response.Write("</form>");
	Response.Write("</form>");

}

bool GetFreightDescription()
{
	string sc = " SELECT name +' '+ description AS shipping FROM ship ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "ship");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}	
	
	if(rows > 0)
	{
		Response.Write("<select name=ship_by>");
		for(int i=0; i<rows; i++)
			Response.Write("<option value='"+ dst.Tables["ship"].Rows[i]["shipping"].ToString() +"'>"+ dst.Tables["ship"].Rows[i]["shipping"].ToString() +" </option>");	
		Response.Write("</select>");
	}
	else
		Response.Write("<input type=text name=ship_by >");
	
	return true;
}
bool DoCheckExistingRMA()
{

	string rid = HttpUtility.UrlDecode(Request.QueryString["rid"]);
	string sc = "SET DATEFORMAT dmy ";
	if(Request.QueryString["ty"] == "2" )
	{
		sc += " SELECT DISTINCT r.supp_rmano, r.id, r.repair_date, r.product_desc AS prod_desc, e.name AS status";
		sc += " , r.supplier_id AS customer_id, r.ra_id AS ra_number, c.name,c.trading_name, c.phone, c.fax, c.company, c.address1, c.address2, c.city ";
		sc += " FROM rma r Left OUTER JOIN ra_freight rf ON r.ra_id = rf.ra_number ";
		sc += " LEFT OUTER JOIN card c ON c.id = r.supplier_id ";
		sc += " LEFT OUTER JOIN enum e ON e.id = r.status AND e.class='rma_status' ";
		sc += " WHERE 1=1 ";
		sc += " AND r.ra_id <> null OR r.ra_id <> '' ";
		if(m_id != "" && m_id != null)
			sc += " AND r.ra_id = '"+ m_id +"' ";
	
		if(m_search != null && m_search != "")
		{
			if(TSIsDigit(m_search))
			sc += " AND r.ra_id = '"+ EncodeQuote(m_search) +"' ";
			else
			{
				sc += " AND (c.name LIKE '%"+ EncodeQuote(m_search) +"%' OR c.company LIKE '%"+ EncodeQuote(m_search) +"%' ";
			sc += " OR c.phone LIKE '%"+ EncodeQuote(m_search) +"%' ";
			sc += " ) ";
			}
		}
		else
		{
			if(rid != null && rid != "")
			{
				if(TSIsDigit(rid))
					sc += " AND r.ra_id = '"+ EncodeQuote(rid) +"' ";
				else
				{
					sc += " AND (c.name LIKE '%"+ EncodeQuote(rid) +"%' OR c.company LIKE '%"+ EncodeQuote(rid) +"%' ";
					sc += " OR c.phone LIKE '%"+ EncodeQuote(rid) +"%' ";
					sc += " ) ";
				}
				
			}
		}
		sc += " ORDER BY r.repair_date ";

	}
	else
	{
		sc += " SELECT DISTINCT r.prod_desc, r.repair_date, e.name AS status, r.customer_id, r.ra_number, c.name,c.trading_name, c.phone, c.fax, c.company, c.address1, c.address2, c.city ";
		sc += " FROM repair r Left OUTER JOIN ra_freight rf ON r.ra_number = rf.repair_number ";
		sc += " LEFT OUTER JOIN card c ON c.id = r.customer_id ";
		sc += " LEFT OUTER JOIN enum e ON e.id = r.status AND e.class='rma_status' ";
		sc += " WHERE 1=1 ";
	//	sc += " AND r.ra_number <> null OR r.ra_number <> '' ";
		if(m_id != "" && m_id != null)
			sc += " AND r.ra_number = '"+ HttpUtility.UrlDecode(m_id) +"' ";
	//	sc += " AND r.status = 4 "; //OR r.status =5 ";
		//if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
		if(m_search != null && m_search != "")
		{
			sc += " AND (r.ra_number = '"+ m_search +"' ";
			sc += " OR c.name LIKE '%"+ EncodeQuote(m_search) +"%' OR c.company LIKE '%"+ EncodeQuote(m_search) +"%' ";
				sc += " OR c.phone LIKE '%"+ EncodeQuote(m_search) +"%' ";
				sc += " ) ";
		}
		else
		{
			if(rid != null && rid != "")
			{
				//if(TSIsDigit(rid))
					sc += " AND (r.ra_number = '"+ EncodeQuote(rid) +"' ";
					sc += " OR c.name LIKE '%"+ EncodeQuote(rid) +"%' OR c.company LIKE '%"+ EncodeQuote(rid) +"%' ";
					sc += " OR c.phone LIKE '%"+ EncodeQuote(rid) +"%' ";
					sc += " ) ";
				//else
				//	rid = "";
			}
		}
		sc += " AND r.status > 3 ";
		sc += " ORDER BY r.repair_date DESC ";
	}
//DEBUG("s c=", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "repair_check");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " SELECT id, ticket, ship_desc FROM ra_freight ";
	sc += " WHERE 1=1 ";
	//if(rid != null && rid != "")
	if(Request.QueryString["ty"] == "2")
	{
		//if(TSIsDigit(rid))
		sc += " AND ra_number ='" + rid +"' ";	
	}
	else
		sc += " AND repair_number ='" + rid +"' ";
		
	if(m_search != null && m_search != "")
	{
		if(Request.QueryString["ty"] == "2")
			sc += " AND ra_number = '"+ m_search +"' ";
		else
			sc += " AND repair_number = '"+ m_search +"' ";
			
	}
	
//DEBUG(" sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "ticket");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
	{
		Response.Write("<br><br><center><h4><font color=red> No Records </font></h4>");
		Response.Write("<br><input type=button value='<< back' "+ Session["button_style"] +" onclick=\"window.history.back()\">");

		return false;
	}
//	if(rid != null && rid != "")
//	{

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
		m_cPI.TotalRows = rows;
	m_cPI.PageSize = 15;
	m_cPI.URI = "?";
	
//	if(m_cPI.CurrentPage.ToString() != "" && m_cPI.CurrentPage.ToString() != null)
//		uri += "?p="+ m_cPI.CurrentPage.ToString() +"&";
//	if(m_cPI.StartPageButton.ToString() != "" && m_cPI.StartPageButton.ToString() != null)
//		uri += "spb="+ m_cPI.StartPageButton.ToString() +"&";
//	if(m_command != "")
//		uri += "cmd="+m_command +"";
//uri += "?";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	Response.Write("<form name=frm method=post>");
	Response.Write("<br><br><table width=80% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px; border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4> SEARCH RMA#: ");
	Response.Write("<input type=text name=search value='"+ Request.Form["search"] +"'>");
	Response.Write("<input type=submit name=cmd value='SEARCH >>' "+ Session["button_style"] +" >");
	Response.Write("<input type=button value='   All   ' "+ Session["button_style"] +"  onclick=\"window.location=('"+ uri +"')\">");
	Response.Write("</td>");
	Response.Write("<td colspan=2>"+ sPageIndex +"</td></tr>");
	Response.Write("<tr bgcolor=#F2EEE3 align=left>");
	Response.Write("<th>RA#</th><th>PROD_DESC</th>");
	Response.Write("<th>");
	if(Request.QueryString["ty"] != "2")
		Response.Write("CUSTOMER");
	else
		Response.Write("SUPPLIER");
	Response.Write("</th><th>SHIP_ADD</th><th>RMA_DATE</th><th>STATUS</th>");
	Response.Write("</tr>");
	bool bAlter = true;

	for(; i<rows && i<end; i++)
	{
		DataRow dr = dst.Tables["repair_check"].Rows[i];
		string name = "";
		if(g_bRetailVersion)
			name = dr["name"].ToString();
		else
			name = dr["company"].ToString();
		string customer_id = dr["customer_id"].ToString();
		string add = dr["address1"].ToString();
		add += "<br>" + dr["address2"].ToString();
		add += "<br>" + dr["city"].ToString();
		add += "<br>ph: " + dr["phone"].ToString();
		add += "<br>fax: " + dr["fax"].ToString();
		string desc = dr["prod_desc"].ToString();
		string ra = dr["ra_number"].ToString();
		string ra_date = dr["repair_date"].ToString();
		string status = dr["status"].ToString();
		string sid = "";
		string srma = "";
		if(Request.QueryString["ty"] == "2")
		{
			sid = dr["id"].ToString();
			srma = dr["supp_rmano"].ToString();
			Session["rma_supplier_id"] = customer_id;
		}
		
		Response.Write("<tr ");
		if(!bAlter)
			Response.Write(" bgcolor=#EEEEEE");
		
		Response.Write(" >");
	
		Response.Write("<td valign=top><a title='' href='"+ uri +"?rid="+ ra +"");
		if(Request.QueryString["ty"] == "2")
			Response.Write("&ty=2");
		else
			Response.Write("&ty=1");
		Response.Write("' class=o>"+ ra +"</a>");
		if(Request.QueryString["ty"] == "2")
		{
			Response.Write(" <a title='view/print ra basic form' href='ra_form1.aspx?print=form&ra="+ HttpUtility.UrlEncode(ra) +"&srma="+ HttpUtility.UrlEncode(srma) +"&sid="+ customer_id +"&r="+ DateTime.Now.ToOADate() +"' target=_blank class=o>PRF1</a> ");
			Response.Write(" <a title='view ra form' href=\"'ra_form.aspx?print=form&ra="+ HttpUtility.UrlEncode(ra) +"&srma="+ (srma) +"&sid="+ customer_id +"&r="+ DateTime.Now.ToOADate() +"' target=_blank class=o>PRF</a> ");
		}
		else
			Response.Write(" <a title='view ra form' href='repair.aspx?print=form&r="+ DateTime.Now.ToOADate() +"&ra="+ HttpUtility.UrlEncode(ra) +"&ty=1' class=o target=_blank>PRF</a> ");
		Response.Write("</td>");
		Response.Write("<td>"+ desc +"</td>");
		Response.Write("<td valign=top><a title='view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id="+ customer_id +"','', 'width=350,height=350'); viewcard_window.focus();\" class=o> ");
		Response.Write(""+ name +"</a></td>");
		Response.Write("<td valign=top>");
		Response.Write("<table width=98% align=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE ");
		if(!bAlter)
			Response.Write(" bgcolor=#EEEEEE");
		bAlter = !bAlter;
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr align=left>");
		Response.Write("<td valign=top>"+ add +"</td>");
		Response.Write("</tr></table>");
		Response.Write("</td>");
		Response.Write("<td valign=top>"+ ra_date +"</td>");
		Response.Write("<td valign=top>"+ status +"</td>");

		Response.Write("</tr>");
	}
	Response.Write("</table><br>");
	
	if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "" || m_search != null && m_search != "")
		GetFreightTicket();
//	}
	return true;
}


</script>
<asp:Label id=LFooter runat=server/>
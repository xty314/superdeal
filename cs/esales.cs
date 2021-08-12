<!-- #include file="card_function.cs" -->

<script runat=server>

const string cols = "11";	//how many columns main table has, used to write colspan=
const string tableTitle = "Process Order";
const string thisurl = "esales.aspx";
bool bItemProcessing = false;
//bool m_bDodiscount = true;	//assess whether update account discount, HG 13.Aug.2002
bool m_bOrder = false;

string m_sDN = ""; //delivery notice sent to customer by email
string m_sInvoiceNumber;
string m_type;
string m_freight = "";
int page = 1;
const int m_nPageSize = 100; //how many rows in oen page
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet dsf = new DataSet();	//for getting freight

string m_cardID = "";
string m_customerEmail = "";
double m_dOldFreight = 0;
double m_dPrice = 0;
double m_dTotalOld = 0;

string m_shippingMethod = "1";
bool m_bSpecialShipto = false;
string m_specialShiptoAddr = "";
string m_pickupTime = "";

string m_delTicket = "";
string m_customerName = "";
bool m_bFreightCharged = false;

void Page_Load(Object Src, EventArgs E) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	string spage = Request.QueryString["p"];
	if(spage != null)
		page = int.Parse(spage);
	m_type = Request.QueryString["t"];
	m_sInvoiceNumber = Request.QueryString["i"];
	if(!TSIsDigit(m_sInvoiceNumber))
	{
		Response.Write("<h3>Wrong Invoice Number: " + m_sInvoiceNumber + "</h3>");
		return;
	}

	if(Request.QueryString["done"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();

		Response.Write("<br><br><center><h3>done!");
		Response.Write("<br><br><center><h3><a href=invoice.aspx?" + m_sInvoiceNumber + " class=o target=_blank>Print Credit/Invoice</a>");
		Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + " class=o target=_blank>Print Packing Slip</a>");
		//Response.Write("<br><br><center><h3><a href=invoice.aspx?n=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(m_customerEmail));
		Response.Write("<br><br><center><h3><a href=invoice.aspx?n=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(Session["customerEmail"].ToString()));
		Response.Write(" class=o target=_blank title='Email Invoice to customer'>Email Credit/Invoice</a>");
		//Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(m_customerEmail));
		Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(Session["customerEmail"].ToString()));
		Response.Write(" target=_blank title='Email Packing Slip to customer' class=o>Email Packing Slip</a>");
		Response.Write("<br><br><center><h3><a href=olist.aspx?r="+ DateTime.Now.ToOADate() +" class=o target=_blank>Back to Order List</a>");
              
		return;
	}

	m_delTicket = Request.QueryString["delt"];
	if(m_delTicket != null && m_delTicket != "")
	{
		DoDelTicket();
	}

	if(Session["shipping_ticket_count"] == null || (m_type != "continue" && Request.Form["cmd"] == null))
		Session["shipping_ticket_count"] = 0;

	if (!GetShipDetails())
		return;

	if(!GetCustomerDetails())
		return;

	if(Request.Form["ticket"] != null && Request.Form["ticket"] != "") //scan ticket
	{
		if(DoScanTicket())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
			Response.Write(" URL=esales.aspx?t=continue&i=" + m_sInvoiceNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
			return;
		}
	}
	else if(Request.Form["cmd"] != null)
	{
		bool bSuccess = false;
		string cmd = Request.Form["cmd"];
		if(cmd == "Record")
			bSuccess = UpdateStatus();
		else if(cmd == "Delete")
			bSuccess = DoDelete();
		else if(cmd == "Send")
			bSuccess = ReSendInvoice();
		if(bSuccess)
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
			Response.Write(" URL=esales.aspx?done=1&i=" + m_sInvoiceNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
			return;
		}
		return;
	}
	else
	{
		if(!GetOrder())
		{
			Response.Write("<h3>Order# " + m_sInvoiceNumber + " not found</h3>");
			return;
		}

		PrintAdminHeader();
		PrintAdminMenu();
		WriteHeaders();
		DrawProcessTable();
		DrawItemTable();
		WriteFooter();
	}
	PrintAdminFooter();
}

bool ReSendInvoice()
{
	string body = BuildInvoice(m_sInvoiceNumber);
	string email = Request.Form["email"];
	MailMessage msgMail = new MailMessage();

	msgMail.From = m_sSalesEmail;
	msgMail.To = email;
	msgMail.Subject = "Invoice " + m_sInvoiceNumber +" From Clipman";
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = BuildInvoice(m_sInvoiceNumber);

	SmtpMail.Send(msgMail);
	return true;
}

bool GetCustomerDetails()
{
	int rows = 0;
	string status_deleted = GetEnumID("general_status", "deleted");
	string sc = "SELECT i.*, c.*, c.email AS emailpass, o.status AS order_status ";
	sc += ", c.company AS c_company, c.address1 AS c_address1, c.address2 AS c_address2 ";
	sc += ", c.address3 AS c_address3, c.phone AS c_phone, c.city AS c_city, c.country AS c_country ";
	sc += " FROM orders o JOIN invoice i ON o.invoice_number=i.invoice_number LEFT OUTER JOIN card c ON c.id=i.card_id ";
	sc += " WHERE i.invoice_number=" + m_sInvoiceNumber;

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "invoice");
		if(rows <= 0)
		{
			Response.Write("<br><center><h3>Invoice Not Found</h3></center>");
			return false;
		}
		else
		{
			string sStatus = dst.Tables["invoice"].Rows[0]["status"].ToString();
			if(sStatus == status_deleted)
			{
				Response.Write("<h3>Invoice Deleted</h3>");
				return false;
			}
			else
			{
				DataRow dr = dst.Tables["invoice"].Rows[0];
				m_freight = dr["freight"].ToString();
				if(MyDoubleParse(m_freight) > 0 || MyDoubleParse(m_freight) < 0)
					m_bFreightCharged = true;
		
				m_bSpecialShipto = bool.Parse(dr["special_shipto"].ToString());
				m_shippingMethod = dr["shipping_method"].ToString();
				m_pickupTime = dr["pick_up_time"].ToString();
				m_specialShiptoAddr = dr["shipto"].ToString();
				string company = dr["trading_name"].ToString();
				string address = dr["address1"].ToString();
				string type = dr["type"].ToString();
				m_cardID = dr["card_id"].ToString();
				m_customerEmail = dr["emailpass"].ToString();
				Session["customerEmail"] = m_customerEmail;
				m_customerName = dr["name"].ToString();
				m_dPrice = MyDoubleParse(dr["price"].ToString());
				m_dTotalOld = MyDoubleParse(dr["total"].ToString());
				m_dOldFreight = MyDoubleParse(dr["freight"].ToString());
				string status = dr["order_status"].ToString();
				if(type == "6")//GetEnumID("receipt_type", "credit note"))
				{
					PrintAdminHeader();
					PrintAdminMenu();
					Response.Write("<br><br><br><center><h3><a href=invoice.aspx?" + m_sInvoiceNumber + " class=o target=_blank>Print Credit/Invoice</a>");
					Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + " class=o target=_blank>Print Packing Slip</a>");
					Response.Write("<br><br><center><h3><a href=invoice.aspx?n=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(m_customerEmail));
					
					Response.Write(" class=o target=_blank title='Email Invoice to customer'>Email Credit/Invoice</a>");
					Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(m_customerEmail));
					Response.Write(" target=_blank title='Email Packing Slip to customer' class=o>Email Packing Slip</a>");
					return false;
				}
				else if(m_shippingMethod == "1") //pickup
				{
					string status_invoiced = GetEnumID("order_item_status", "Invoiced");
					if(status == status_invoiced)
					{
						sc = "UPDATE orders SET date_shipped=GETDATE() ";
						sc += ", status=" + GetEnumID("order_item_status", "Shipped");
						sc += " WHERE invoice_number=" + m_sInvoiceNumber;
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
					PrintAdminHeader();
					PrintAdminMenu();
					Response.Write("<br><br><br><center><h3><font color=red>Pick Up " + m_pickupTime + "</font><br><br><a href=invoice.aspx?" + m_sInvoiceNumber + " class=o target=_blank>Print Credit/Invoice</a>");
					Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + " class=o target=_blank>Print Packing Slip</a>");
					Response.Write("<br><br><center><h3><a href=invoice.aspx?n=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(m_customerEmail));
					Response.Write(" class=o target=_blank title='Email Invoice to customer'>Email Credit/Invoice</a>");
					Response.Write("<br><br><center><h3><a href=pack.aspx?i=" + m_sInvoiceNumber + "&confirm=1&email=" + HttpUtility.UrlEncode(m_customerEmail));
					Response.Write(" target=_blank title='Email Packing Slip to customer' class=o>Email Packing Slip</a>");

					return false;
				}
			}
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetShipDetails()
{
	int rows = 0;
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM ship");
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		rows = myAdapter.Fill(dst, "ship");

	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

bool GetOrder()
{
	int rows = 0;
	string sc = "SELECT * FROM sales WHERE invoice_number=" + m_sInvoiceNumber;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "product");
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoDelete()
{
	if(Request.Form["delete"] != "on")
	{
		Response.Write("<h3>Error, please tick the checkbox to confirm deletion</h3>");
		return false;
	}
	string status_deleted = GetEnumID("general_status", "deleted");
	string sc = "UPDATE invoice SET status=" + status_deleted + " WHERE invoice_number=" + m_sInvoiceNumber;
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

Boolean UpdateStatus()
{
	int tickets = (int)Session["shipping_ticket_count"];
	if(tickets <= 0)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, no freight");
		return false;
	}

	string invoice = Request.Form["invoice0"];
	string s_freight = MyMoneyParse(Request.Form["freight"]).ToString();
	string sc = "UPDATE orders SET date_shipped=GETDATE() "; //shipby=" + shipby + ", ticket='" + ticket + "' ";
	sc += ", status=" + GetEnumID("order_item_status", "Shipped");// + ", freight=" + s_freight; 
	sc += " WHERE invoice_number=" + invoice;
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

	if(!UpdateInvoiceFreight(s_freight))
		return false;

	if(m_sDN != "") //send delivery notice
	{
		MailMessage msgMail = new MailMessage();
		msgMail.From = m_sSalesEmail;
		msgMail.To = dst.Tables["invoice"].Rows[0]["email"].ToString();
		msgMail.Subject = "Shipment Notice - " + m_sCompanyTitle;
		msgMail.BodyFormat = MailFormat.Html;
		msgMail.Body = m_sDN;

		SmtpMail.Send(msgMail);
	}		

	return true;
}

bool UpdateInvoiceFreight(string s_freight)
{
	string sc = "";
	int tickets = (int)Session["shipping_ticket_count"];
	if(tickets <= 0)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, no freight");
		return false;
	}

	string stickets = "";
	string ticket = "";
	double dFreight = MyDoubleParse(s_freight);
	for(int i=0; i<tickets; i++)
	{
		string id = ""; //for later use
		
		string shipid = "";
		string shipname = "";
		string shipdesc = "";
		double dShipFreight = 0;
		DataRow dr = null;

		ticket = Session["shipping_ticket" + i].ToString();

		dr = GetShipPrice(ticket);
		if(dr == null)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Error, Freight ticke # " + ticket + " undefined");
			return false;
		}
		shipid = dr["id"].ToString();
		shipname = dr["name"].ToString();
		shipdesc = dr["description"].ToString();
		if(Request.Form["already_charged" + i] == "on")
			dShipFreight = 0;
		else
			dShipFreight = MyDoubleParse(dr["price"].ToString());

		sc += " INSERT INTO	invoice_freight (invoice_number, ship_name, ship_desc, ticket, price, ship_id) ";
		sc += " VALUES(" + m_sInvoiceNumber;
		sc += ", '" + EncodeQuote(shipname) + "' ";
		sc += ", '" + EncodeQuote(shipdesc) + "' ";
		sc += ", '" + EncodeQuote(ticket) + "' ";
		sc += ", " + dShipFreight;
		sc += ", " + shipid;
		sc += ") ";

		stickets += shipdesc;
		stickets += " : ";
		stickets += ticket;
		stickets += "<br>\r\n";
	}

	double dTaxNew = (m_dPrice + dFreight) * GetGstRate(m_cardID);
	//double dTaxNew = (m_dPrice) * GetGstRate(m_cardID);
	dTaxNew = Math.Round(dTaxNew, 2);
	double dTotalNew = m_dPrice + dFreight + dTaxNew;
	
	double dTotalAdd = dTotalNew - m_dTotalOld;
	UpdateCardBalance(m_cardID, dTotalAdd);

	sc += " Update invoice SET freight=" + dFreight;
	sc += ", tax=" + dTaxNew;
	sc += ", total=" + dTotalNew;
	sc += " WHERE invoice_number=" + m_sInvoiceNumber; 
//DEBUG("sc = ", sc);
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

	if(stickets != "")
	{
		//DEBUG("hello","hello");
		//DEBUG("stickets",stickets);
		m_sDN = ReadSitePage("shipment_notice");
		m_sDN = m_sDN.Replace("@@customer_name", m_customerName);
		m_sDN = m_sDN.Replace("@@invoice_number", m_sInvoiceNumber);
		m_sDN = m_sDN.Replace("@@ship_address", WriteCustomerDetails());
		m_sDN = m_sDN.Replace("@@ship_date", DateTime.Now.ToString("dd-MM-yyyy HH:mm"));
		m_sDN = m_sDN.Replace("@@ship_tickets", stickets);
		string item_list = BuildInvoiceItemList(m_sInvoiceNumber);
		m_sDN = m_sDN.Replace("@@product_list", item_list);
	}

	return true;
}

void DrawTableHeader()
{
	StringBuilder sb = new StringBuilder();;
	sb.Append("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("<td width=50>INVOICE#</td>\r\n");
	sb.Append("<td>DATE/TIME</td>\r\n");
	sb.Append("<td>CODE</td>");
	sb.Append("<td>M_PN</td>");
	sb.Append("<td>NAME</td>");
	sb.Append("<td>SUPPLIER</td>");
	sb.Append("<td>SUPPLIER_CODE</td>");
	sb.Append("<td>QTY</td>");
	if(bItemProcessing)
	{
		sb.Append("<td>STATUS</td>");
		sb.Append("<td>SHIP BY</td>");
		sb.Append("<td>TICKET#</td>");
//		sb.Append("<td>NOTES</td>");
	}
	sb.Append("</tr>\r\n");
	
	Response.Write(sb.ToString());
	Response.Flush();
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
	string invoice = dr["invoice_number"].ToString();
	string code = dr["code"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string name = dr["name"].ToString();
	string supplier = dr["supplier"].ToString();
	string quantity = dr["quantity"].ToString();
	string status = dr["status"].ToString();
	string shipby = dr["shipby"].ToString();
	string ticket = dr["ticket"].ToString();
	string note = dr["note"].ToString();
	bool bSystem = (bool)dr["system"];

	string date = dst.Tables["invoice"].Rows[0]["commit_date"].ToString();
	string index = i.ToString();

	StringBuilder sb = new StringBuilder();
	
	sb.Append("<input type=hidden name=invoice" + index + " value='" + invoice + "'>");
	sb.Append("<input type=hidden name=code" + index + " value='" + code + "'>");

	sb.Append("<tr");
	if(bSystem)
		sb.Append(" bgcolor=#FFFFEE");
	else if(alterColor)
		sb.Append(" bgcolor=#EEEEEE");
	sb.Append("><td><a href=invoice.aspx?" + invoice + " target=_blank>");
	sb.Append(invoice);
	sb.Append("</a></td><td>");
	sb.Append(date);
	sb.Append("</td><td>");
	sb.Append(code);
	sb.Append("</td><td>");
	sb.Append(supplier_code);
	sb.Append("</td><td>");
	sb.Append(name);
	sb.Append("</td><td>");
	sb.Append(supplier);
	sb.Append("</td><td>");
	sb.Append(supplier_code);
	sb.Append("</td><td>");
	sb.Append(quantity);
	sb.Append("</td></tr>");

	Response.Write(sb.ToString());
	Response.Flush();
	return true;
}

string AddShips(string id, string sName, string shipby)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<option value='" + id + "'");
	if(shipby == id)
		sb.Append(" selected");
	sb.Append(">" + sName + "</option>");
	return sb.ToString();
}

bool DoScanTicket()
{
//DEBUG("ticket=", Request.Form["ticket"]);
	if(Request.Form["ticket"] != null && Request.Form["ticket"] != "")
	{
		int tickets = (int)Session["shipping_ticket_count"];
		for(int i=0; i<tickets; i++)
		{
			if(Request.Form["already_charged" + i] == "on")
				Session["shipping_ticket_already_charged" + i] = "1";
			else
				Session["shipping_ticket_already_charged" + i] = null;
		}
		Session["shipping_ticket" + tickets] = Request.Form["ticket"];
		if(m_bFreightCharged)
			Session["shipping_ticket_already_charged" + tickets] = "1";
		else
			Session["shipping_ticket_already_charged" + tickets] = null;
		Session["shipping_ticket_count"] = tickets + 1;
	}
	return true;
}

bool DoDelTicket()
{
	if(Session["shipping_ticket_count"] == null)
		return true;

	int row = MyIntParse(m_delTicket);
	int tickets = (int)Session["shipping_ticket_count"];
	tickets--;
	for(int i=row; i<tickets; i++)
	{
		Session["shipping_ticket" + i] = Session["shipping_ticket" + (i+1).ToString()].ToString();
	}
	Session["shipping_ticket_count"] = tickets;
	return true;
}

DataRow GetShipPrice(string ticket)
{
	DataRow dr = null;

	if(dsf.Tables["scan"] != null)
		dsf.Tables["scan"].Clear();

	string sc = "SELECT * FROM ship ORDER BY prefix";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsf, "scan");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	bool bsuffixMatch = false;
	bool bPrefixMatch = false;

	string pm = "";
	string sm = "";
	string prefix = "";
	string suffix = "";
	for(int i=0; i<dsf.Tables["scan"].Rows.Count; i++)
	{
		dr = dsf.Tables["scan"].Rows[i];
		prefix = dr["prefix"].ToString();
		suffix = dr["suffix"].ToString();
		Trim(ref prefix);
		Trim(ref suffix);
		if(prefix == "")
		{
			if(suffix == "")
				continue;
			if(suffix.Length > ticket.Length)
				continue;
//DEBUG("check suffix ", suffix);
			//now check suffix
			if(ticket.Substring(ticket.Length - suffix.Length, suffix.Length).ToLower() == suffix.ToLower())
			{
			//	return dr;
				bsuffixMatch = true;
//DEBUG("suffixMatch=", suffix);
				sm = suffix;
				break;
			}
			continue;
		}
//DEBUG("pl="+prefix.Length.ToString(), " tl="+ticket.Length.ToString());
		if(prefix.Length > ticket.Length)
			continue;
//DEBUG("check prefix ", prefix);
//DEBUG("t="+ticket.Substring(0, prefix.Length).ToLower(), " pf="+prefix.ToLower());
		if(ticket.Substring(0, prefix.Length).ToLower() == prefix.ToLower())
		{
			bPrefixMatch = true;
//DEBUG("prefixMatch=", prefix);
			pm = prefix;
			break;
		}
	}
//DEBUG("bsuffixMatch=", bsuffixMatch.ToString());
//DEBUG("bprefixMatch=", bPrefixMatch.ToString());
	if(!bsuffixMatch && !bPrefixMatch)
		return null;

	bool bDoubleMatch = false;
	//check double matches
	for(int i=0; i<dsf.Tables["scan"].Rows.Count; i++)
	{
		DataRow drd = dsf.Tables["scan"].Rows[i];
		prefix = drd["prefix"].ToString();
		suffix = drd["suffix"].ToString();
		Trim(ref prefix);
		Trim(ref suffix);
		if(prefix == "")
		{
			if(suffix == "")
				continue;
			if(suffix.Length > ticket.Length)
				continue;
			if(bsuffixMatch)
				continue;

			//now check suffix
			if(ticket.Substring(ticket.Length - suffix.Length, suffix.Length).ToLower() == suffix.ToLower())
			{
				bDoubleMatch = true;
				sm = suffix;
				break;
			}
		}
		if(prefix.Length > ticket.Length)
			continue;
		if(ticket.Substring(0, prefix.Length).ToLower() == prefix.ToLower())
		{
			if(bPrefixMatch)
				continue;
			bDoubleMatch = true;
			pm = prefix;
			break;
		}
	}
	if(bDoubleMatch)
	{
		Response.Write("<br><br><center><h1><font color=red>Double Match Detected, ");
		Response.Write("ticket scanned match both prefix '</font>" + pm + "<font color=red>' ");
		Response.Write("and suffix '</font>" + sm + "<font color=red>', please check ticket settings");
	}
	return dr;
}

void DrawProcessTable()
{
	Response.Write("<table width=100% cellspacing=3 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><td valign=top>");

	Response.Write("<table>");
	Response.Write("<tr><td><h5><font color=green>TICKETS</font></h5></td>");
	Response.Write("<td valign=top>&nbsp&nbsp;");
	Response.Write("<input type=text name=ticket autocomplete=off>");
	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.ticket.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Enter'>");

	Response.Write(" <b>Shipping Method : </b><font color=red><b>" + GetEnumValue("shipping_method", m_shippingMethod).ToUpper() + "</b></font>");
	if(m_bFreightCharged)
		Response.Write(" <b>(Freight Already Charged)</b>");
	Response.Write("</td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("<tr><td>");

	//ticket list
	Response.Write("<table>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-size:8pt;font-weight:bold;\">\r\n");
	Response.Write("<th width=70>Ticket#</th>");
	Response.Write("<th width=90>Ship By</th>");
	Response.Write("<th width=90>Description</th>");
	Response.Write("<th>Freight Charge</th>");
	Response.Write("<th>Already ChargeD</th>");
	Response.Write("</tr>");

	int tickets = (int)Session["shipping_ticket_count"];
//DEBUG("tickets=", tickets);
//	if(tickets <= 0)
//		Response.Write("<tr><td colspan=2><b>There's no tickets</b></td></tr>");
	string ticket = "";
	double dFreight = 0;
	if(m_freight != "")
		dFreight = MyDoubleParse(m_freight);
	for(int i=0; i<tickets; i++)
	{
		string id = ""; //for later use
		
		string shipid = "";
		string shipname = "";
		string shipdesc = "";
		double dShipFreight = 0;
		DataRow dr = null;

		ticket = Session["shipping_ticket" + i].ToString();
		dr = GetShipPrice(ticket);
		if(dr != null)
		{
			shipid = dr["id"].ToString();
			shipname = dr["name"].ToString();
			shipdesc = dr["description"].ToString();
			dShipFreight = MyDoubleParse(dr["price"].ToString());
		}

		Response.Write("<tr><td>" + ticket);
		Response.Write(" <a href=esales.aspx?t=continue&i=" + m_sInvoiceNumber + "&delt=" + i);
		Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o title=Delete><b>X</b></a>");
		Response.Write("</td><td>");
		Response.Write(shipname);
		Response.Write("</td><td>");
		Response.Write(shipdesc);
		Response.Write("</td><td align=right>");
		Response.Write("<a href=newship.aspx?t=m&i=" + shipid + "&r=" + DateTime.Now.ToOADate());
		Response.Write(" target=_blank title=Edit>" + dShipFreight.ToString("c") + "</a>");
		Response.Write("<input type=hidden name=freight" + i + " value=" + dShipFreight + ">");
		Response.Write("</td><td align=center>");
		Response.Write("<input type=checkbox name=already_charged" + i + " onclick=\"UpdateFreight();\" ");
		if(Session["shipping_ticket_already_charged" + i] != null)
		{
			Response.Write(" checked");
		}
		else
		{
			dFreight += dShipFreight;
		}
		Response.Write("></td></tr>");
	}

	//javascript function
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function UpdateFreight()");
	Response.Write("{ var total = 0;");
	for(int i=0; i<tickets; i++)
	{
		Response.Write("	if(!document.f.already_charged" + i.ToString() + ".checked)");
		Response.Write("	total += Number(document.f.freight" + i.ToString() + ".value);\r\n");
	}
	Response.Write("	document.f.freight.value = Number(document.f.freight_charged.value) + total;\r\n");
	Response.Write("}");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<input type=hidden name=freight_charged value=" + m_freight + ">");
	m_freight = dFreight.ToString();
	Response.Write("<tr></td><td colspan=4 align=right>");
	Response.Write("<b>Total Freight : </b><input type=text size=8 readonly=true style=text-align:right; name=freight value='");
	if(m_freight != "")
		Response.Write(dFreight.ToString("c"));
	Response.Write("'></td></tr>");

	Response.Write("<tr></td><td colspan=4 align=right>");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value=Record>");


	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr></table>");
}

void WriteHeaders()
{
	Response.Write("\r\n<form name=f action=esales.aspx?i=");
	Response.Write(m_sInvoiceNumber);
	Response.Write("&t=update&p=");
	Response.Write(page);
	if(bItemProcessing)
		Response.Write("&jt=i");
	else
		Response.Write("&jt=a");
	Response.Write(" method=post>\r\n");
	Response.Write("\r\n\r\n<table width=100% bgcolor=white align=center>");
	Response.Write("\r\n<tr><td valign=top>");
	Response.Write("<br><center><h3>Shipping</h3></center></td></tr><tr><td>");
}

void WriteFooter()
{
	DataRow dr = dst.Tables["invoice"].Rows[0];
	Response.Write("<tr valign=top><td>");
	Response.Write(WriteCustomerDetails());
	Response.Write("</td></tr>");
	Response.Write("<tr><td>");
	
	// add button for Packing Order
	//added resent email button, 15 Aug 2002 DW.
	Response.Write("<table width=100% align=right><tr>");
	Response.Write("</td>");
	
	Response.Write("</tr>");
	Response.Write("</table>\r\n");

	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

string WriteCustomerDetails()
{
	DataRow dr = dst.Tables["invoice"].Rows[0];
	StringBuilder sb = new StringBuilder();
	sb.Append("<table valign=top colspan=4><tr><td>");
	if(m_bSpecialShipto)
		sb.Append("<font size=+1 color=red><b>Special Shipping Address</b></font>");
	else
		sb.Append("<font size=+1><b>Shipping Address</b></font>");
	sb.Append("</td></tr>");
	sb.Append("<tr><td>");
	if(m_bSpecialShipto)
	{
		sb.Append("<h4>");
		sb.Append(m_specialShiptoAddr.Replace("\r\n", "\r\n<br>"));
		sb.Append("</h4>");
	}
	else
	{
		sb.Append(dr["trading_name"].ToString() + "<br>");
		sb.Append(dr["c_address1"].ToString() + "<br>");
		sb.Append(dr["c_address2"].ToString() + "<br>");
		if(dr["c_address3"].ToString() != "")
			sb.Append(dr["c_address3"].ToString() + "<br>");
		else if(dr["c_city"].ToString() != "")
			sb.Append(dr["c_city"].ToString() + "<br>");
		sb.Append(dr["c_country"].ToString() + "<br>");
		sb.Append("Ph : " + dr["c_phone"].ToString() + "<br>");
	}

	sb.Append("</td></tr></table><br>\r\n");
	return sb.ToString();
}

Boolean DrawItemTable()
{
	Boolean bRet = true;

	DataRow dr = dst.Tables["product"].Rows[0];
//	string status = dr["status"].ToString();
	string shipby = dr["shipby"].ToString();
	string ticket = dr["ticket"].ToString();
	string note = dr["note"].ToString();

//	Response.Write("</td></tr><tr><td>");

	DrawTableHeader();
	string s = "";
	Boolean alterColor = true;
	int startPage = (page-1) * m_nPageSize;
	for(int i=startPage; i<dst.Tables["product"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
		{
			bRet = false;
			break;
		}
	}

	int pages = dst.Tables["product"].Rows.Count / m_nPageSize + 1;
	if(pages > 1)
	{
		Response.Write("<tr><td colspan=" + cols + " align=right>Page: ");
		for(int i=1; i<=pages; i++)
		{
			if(i != page)
			{
				Response.Write("<a href=esales.aspx?i=");
				Response.Write(m_sInvoiceNumber);
				Response.Write("&p=");
				Response.Write(i.ToString());
				Response.Write(">");
				Response.Write(i.ToString());
				Response.Write("</a> ");
			}
			else
			{
				Response.Write(i.ToString());
				Response.Write(" ");
			}
		}
		Response.Write("</td></tr>");
	}

	Response.Write("</table>\r\n");

	return bRet;
}

bool DoCheckEnterSN()
{
	for(int i=0; i<dst.Tables["product"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["product"].Rows[i];
		string sc = "SELECT COUNT(sn) AS num FROM sales_serial WHERE invoice_number='" + m_sInvoiceNumber;
			sc += "' AND code=" + dr["code"].ToString();
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "NumOfSn");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		if(int.Parse(dr["quantity"].ToString()) > int.Parse(dst.Tables["NumOfSn"].Rows[0]["num"].ToString()))
		{
			return false;
		}
	}

	return true;
}

string BuildInvoiceItemList(string sInvoiceNumber)
{
	string m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);

	string CheckDigit = check_IsNumber(sInvoiceNumber);
	if(CheckDigit == "False")
		return "";

	DataSet dsi = new DataSet();
	string sc = "SELECT oi.*, o.*, s.discount_percent, i.invoice_number, i.type, i.payment_type, i.commit_date, i.price, i.gst_inclusive, i.customer_gst, i.card_id";
	sc += ", i.tax, i.total, i.sales, i.cust_ponumber, ";
	sc += " i.freight, i.sales_note, c.*, s.code, s.supplier_code, s.quantity, s.name as item_name ";
	sc += ", s.commit_price, i.type, i.special_shipto, i.shipto, i.shipping_method, '' AS stock_location, '' AS shelf, ";
	sc += " s.status AS si_status, s.note, s.shipby, s.ship_date, s.ticket, s.processed_by ";
	sc += ", s.system AS bSystem, i.system AS iSystem, cr.is_service, cr.price1, i.no_individual_price AS iNoIndividual ";
	sc += ", s.kit, s.krid, k.kit_id, k.qty AS kit_qty, k.name AS kit_name, k.commit_price AS kit_price, s.normal_price ";
	sc += " FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number LEFT OUTER JOIN card c ON c.id=i.card_id ";
	sc += " JOIN orders o ON o.invoice_number=s.invoice_number";
	sc += " JOIN order_item oi ON  oi.id=o.id";
	sc += " JOIN code_relations cr ON cr.code = s.code ";
	sc += " LEFT OUTER JOIN sales_kit k ON k.invoice_number=i.invoice_number AND k.krid = s.krid ";
	sc += " WHERE i.invoice_number=";
	sc += sInvoiceNumber;
	if(!SecurityCheck("sales", false))
		sc += " AND i.card_id=" + Session["card_id"];
	sc += " ORDER BY s.id ";

	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	if(rows <= 0)
		return "";

	DataRow dr = dsi.Tables[0].Rows[0];
	m_account_number = dr["id"].ToString();
	m_account_name = dr["trading_name"].ToString();
	string sDate = dr["commit_date"].ToString();
	DateTime tDate = DateTime.Parse(sDate);
	m_sDN = m_sDN.Replace("@@order_date", tDate.ToString("dd-MM-yyyy"));
	m_sDN = m_sDN.Replace("@@po_number", dr["cust_ponumber"].ToString());

	return BuildItemTable(dsi.Tables[0], false);
}

</script>
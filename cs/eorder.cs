<!-- #include file="kit_fun.cs" -->
<!-- #include file="card_function.cs" -->
<!-- #include file="credit_limit.cs" -->
<!-- #include file="fifo_f.cs" -->
<!-- #include file="isdate.cs" -->
<script runat=server>

const string cols = "6";	//how many columns main table has, used to write colspan=
string m_orderNumber;
string m_orderBranch = "1";
string m_id;
string m_new_id;
string m_type;
string m_status;
string m_invoiceNumber = "";
string m_invoice_date = "";
string m_manual_invoice_number = "";

int m_totalRows = 0;

int m_rows = 0;

int m_splits = 0;
int m_sp = 0; //current working point
int[] m_sr = new int[64];

int m_editQtyRow = -1;
double m_dTotalValue = 0;
bool m_bCreditReturn = false;

string m_cardID = "";
double m_dOrderTotal = 0;
bool m_bCreditOK = true;
string m_msg = "";
bool m_bPutOnHold = false;
bool m_bHasKit = false;
bool m_allow_zero_stock_sales = false;
bool m_bAllowChangeDate = false; //for store invoice date manually instead of use the system's date 
bool m_bCorrectDate = false;
bool m_bChangeInvoiceNumber = false;
bool m_bNoSerialNumberSupport = false;
string tableWidth = "97%";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

/************************** Program Objective/Changes ****************************************
Changes Details here: add new date box for date change for invoice date
When: 21-04-05
Variables Added: m_bAllowChangeDate -- switch to use the date or not
**********************************************************************************************/

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	InitKit();
	m_allow_zero_stock_sales = MyBooleanParse(GetSiteSettings("allow_process_zero_stock_sales", "0", true));
	m_bAllowChangeDate = MyBooleanParse(GetSiteSettings("allow_manual_chage_invoice_date", "0", true));
	m_bChangeInvoiceNumber = MyBooleanParse(GetSiteSettings("allow_manual_chage_invoice_number", "0", true));
	m_bNoSerialNumberSupport = MyBooleanParse(GetSiteSettings("no_serial_number_support", "1", true));
	m_type = Request.QueryString["t"];
	m_orderNumber = Request.QueryString["i"];
	m_id = Request.QueryString["id"];
	
	if(!TSIsDigit(m_id))
	{
		Response.Write("<h3>Wrong Order Number: " + m_id + "</h3>");
		return;
	}

	if(Request.QueryString["eq"] != null)
		m_editQtyRow = MyIntParse(Request.QueryString["eq"]);

	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "Split" && Request.Form["cmd"] != "UnSplit")
	{
		if(Session["in_eorder"] == null)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3><font color=red>STOP!</font> Repost Form is forbidden");
			PrintAdminFooter();
			return;
		}
		Session["in_eorder"] = null;
	}
	else
	{
		Session["in_eorder"] = true;
	}

	if(Request.Form["rows"] != null)
		m_rows = MyIntParse(Request.Form["rows"]);
	if(Request.Form["cmd"] == "Split")
	{
		DoSplit();
		return;
	}
	else if(Request.Form["cmd"] == "BackOrder")
	{
		string status = GetEnumID("order_item_status", "Back Ordered");
		if(DoUpdateOrder(status))
		{
			DoBackOrderMail();
			AdminMsgDie("Done, put to BackOrder");
		}
//		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eorder.aspx?id=" + m_new_id + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "OnHold")
	{
		string status = GetEnumID("order_item_status", "On Hold");
		if(DoUpdateOrder(status))
			AdminMsgDie("Done, put to OnHold");
//		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eorder.aspx?id=" + m_new_id + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Invoice")
	{
		if(m_bAllowChangeDate)
		{
			if(Request.Form["invoice_date"] != null && Request.Form["invoice_date"] != "")
				m_invoice_date = Request.Form["invoice_date"].ToString();
			
			try
			{
				m_invoice_date = DateTime.Parse(m_invoice_date).ToString("dd-MM-yyyy");
                if(IsDate(m_invoice_date))
                    m_bCorrectDate = true; 
			}
			catch(Exception e)
			{
				m_bCorrectDate = false;
			}
			//if(IsDate(m_invoice_date) || IsDateLong(m_invoice_date) || IsDateShort(m_invoice_date))
			//	m_bCorrectDate = true;			
			
			if(m_invoice_date == "" || m_invoice_date == null) //false as insert with system date
			{
				m_bCorrectDate = true; // true if empty, set as default
				m_bAllowChangeDate = false;
			}
					
			if(!m_bCorrectDate)
			{
				Response.Write("<center><br><h4>Invoice Date Invalid!!!!");
				Response.Write("<br><br><input type=button value='BACK' "+ Session["button_style"] +" onclick='window.history.go(-1);' >");

				return;
			}
		}
		if(m_bChangeInvoiceNumber)
		{
			bool bNotValidInvoice =false;
			bool bExistsInvoice = false;
			if(Request.Form["manual_invoice_number"] != null && Request.Form["manual_invoice_number"] != "")
				m_manual_invoice_number = Request.Form["manual_invoice_number"].ToString();

			if(!TSIsDigit(m_manual_invoice_number))
			{
				m_bChangeInvoiceNumber = false;
				bNotValidInvoice = true;
			}
			if(!bNotValidInvoice)
			{
				if(IsExitsInvoice(m_manual_invoice_number))
				{
					m_bChangeInvoiceNumber = false;
					bExistsInvoice = true;				
				}
			}
			if(m_manual_invoice_number == "" || m_manual_invoice_number == null)
			{
				m_bChangeInvoiceNumber = false;
				bNotValidInvoice = false;
			}
			if(bNotValidInvoice || bExistsInvoice)
			{
				Response.Write("<center><br><h4>");
				if(bExistsInvoice)
					Response.Write("******EXISTING INVOICE******");
				if(bNotValidInvoice)
					Response.Write("<br>******INVOICE NUMBER INVALID*******");

				Response.Write("<br><br><input type=button value='BACK' "+ Session["button_style"] +" onclick='window.history.go(-1);' >");
				return;
			}			

		}
		
		if(!m_allow_zero_stock_sales)
		{
			if(!CheckCurrentStock())
				return;
		}
			
		if(GetOrderInfo())
		{
			m_bCreditOK = CreditLimitOK(m_cardID, m_dOrderTotal, ref m_bPutOnHold, ref m_msg);
		}
		if(!m_bCreditOK && Request.QueryString["forceship"] == null)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write(m_msg);
			return;
		}
		string status = GetEnumID("order_item_status", "Invoiced");
		if(DoUpdateOrder(status))
		{
			if(CreateInvoice(m_new_id))
			{
				if(!m_bNoSerialNumberSupport)
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=inputsn.aspx?inv=" + m_invoiceNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
				else
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=esales.aspx?i=" + m_invoiceNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
			}
		}
		return;
	}
	else if(Request.Form["cmd"] == "Credit")
	{
		m_bCreditReturn = true;
		if(m_bAllowChangeDate)
		{
			if(Request.Form["invoice_date"] != null && Request.Form["invoice_date"] != "")
				m_invoice_date = Request.Form["invoice_date"].ToString();
			if(IsDate(m_invoice_date) || IsDateLong(m_invoice_date) || IsDateShort(m_invoice_date))
				m_bCorrectDate = true;
			if(m_invoice_date == "" || m_invoice_date == null) //false will insert with system date
			{
				m_bCorrectDate = true;
				m_bAllowChangeDate = false;
			}
	
			if(!m_bCorrectDate)
			{
				Response.Write("<center><br><h4>Invoice Date Invalid!!!!");
				Response.Write("<br><br><input type=button value='BACK' "+ Session["button_style"] +" onclick='window.history.go(-1);' >");
				return;
			}
		}
		if(m_bChangeInvoiceNumber)
		{
			bool bNotValidInvoice =false;
			bool bExistsInvoice = false;
			if(Request.Form["manual_invoice_number"] != null && Request.Form["manual_invoice_number"] != "")
				m_manual_invoice_number = Request.Form["manual_invoice_number"].ToString();

			if(!TSIsDigit(m_manual_invoice_number))
			{
				m_bChangeInvoiceNumber = false;
				bNotValidInvoice = true;
			}
			if(!bNotValidInvoice)
			{
				if(IsExitsInvoice(m_manual_invoice_number))
				{
					m_bChangeInvoiceNumber = false;
					bExistsInvoice = true;				
				}
			}
			if(m_manual_invoice_number == "" || m_manual_invoice_number == null)
			{
				m_bChangeInvoiceNumber = false;
				bNotValidInvoice = false;
			}
			if(bNotValidInvoice || bExistsInvoice)
			{
				Response.Write("<center><br><h4>");
				if(bExistsInvoice)
					Response.Write("******EXISTING INVOICE******");
				if(bNotValidInvoice)
					Response.Write("<br>******INVOICE NUMBER INVALID*******");

				Response.Write("<br><br><input type=button value='BACK' "+ Session["button_style"] +" onclick='window.history.go(-1);' >");
				return;
			}
			//else
			//	m_bChangeInvoiceNumber = true;

		}
				
		string status = GetEnumID("order_item_status", "Returned");
		DoUpdateOrder(status);
		CreateInvoice(m_new_id);
		if(!m_bNoSerialNumberSupport)
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=inputsn.aspx?inv=" + m_invoiceNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
		else
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=esales.aspx?i=" + m_invoiceNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else
	{
		if(!GetOrder())
			return;
	
		PrintAdminHeader();
		PrintAdminMenu();
		PrintJavaFunctions();
		MyDrawTable();
	}
	PrintAdminFooter();
}

void DoBackOrderMail()
{
	string sc = "SELECT o.number, o.po_number, o.part, o.record_date, oi.code, oi.quantity, oi.item_name, ";
	sc += " oi.commit_price, p.eta, o.card_id, c.name, c.email, k.kit_id ";
	sc += " FROM orders o INNER JOIN order_item oi ON o.id = oi.id ";
	sc += " JOIN product p ON p.code=oi.code ";
	sc += " LEFT OUTER JOIN order_kit k ON k.id = o.id AND k.krid = oi.krid ";
	sc += " LEFT OUTER JOIN card c ON c.id = o.card_id ";
	sc += " WHERE o.id=" + m_new_id;
	sc += " AND o.status=4";	//"status=4" --- backordered items only!
	sc += " ORDER BY o.part";

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "borderitems");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
	if(dst.Tables["borderitems"].Rows.Count > 0)
	{
		if(MyIntParse(dst.Tables["borderitems"].Rows[0]["card_id"].ToString()) > 0)
			DoSendMail();
	}
	return;
}

void DoSendMail()
{
	string name = dst.Tables["borderitems"].Rows[0]["name"].ToString();
	string email = dst.Tables["borderitems"].Rows[0]["email"].ToString();
	string po_number = dst.Tables["borderitems"].Rows[0]["po_number"].ToString();
	StringBuilder sb = new StringBuilder();
		
/*	sb.Append("Dear " + name + ":<br><br><b>Re: The following items have been placed on back order!</b><br><br>");
	sb.Append("Details are listed as follow:<br>");	

	sb.Append("<br><font size=+1><b>Your PO Number is: </b></font><b><font color=red>");
	sb.Append(po_number + "</b></font><br><br>");
*/
	DataRow dr = null;
	Response.Write("<table width=100% valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:7pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:#EEEEE;background-color:#444444;font-weight:bold;\" align=left>");
	sb.Append("<th>Order NO.</th><th>Product Code</th><th>Description</th><th>Qty</th><th>ETA</th></tr>");
	for(int i=0; i<dst.Tables["borderitems"].Rows.Count; i++)
	{
		dr=dst.Tables["borderitems"].Rows[i];
		sb.Append("<tr><td>" + dr["number"].ToString() + "." + dr["part"].ToString() + "</td>");
		sb.Append("<td>" + dr["code"].ToString() + "</td>");
		sb.Append("<td>" + dr["item_name"].ToString() + "</td>");
		sb.Append("<td>" + dr["quantity"].ToString() + "</td>");
//		sb.Append("<td>" + dr["commit_price"].ToString() + "</td>");
		sb.Append("<td>" + dr["eta"].ToString() + "</td></tr>");	
	}
	sb.Append("</table>");

	string backOrderNotice = ReadSitePage("backorder_notice");
	backOrderNotice = backOrderNotice.Replace("@@CUSTOMER", name);
	backOrderNotice = backOrderNotice.Replace("@@PONUMBER", po_number);
	backOrderNotice = backOrderNotice.Replace("@@COMPANYNAME", m_sCompanyTitle);
	backOrderNotice = backOrderNotice.Replace("@@PRODUCTLIST", sb.ToString());

//	sb.Append("<br><br><br>Regards,<br><br>" + m_sCompanyTitle);

	//Build mail
	MailMessage msgMail = new MailMessage();

	msgMail.From = m_sSalesEmail; //Session["email"].ToString();
	msgMail.To = email;
	msgMail.Bcc = m_sSalesEmail; //notice sales as well
	msgMail.Subject = "BackOrder Notice!";
	msgMail.BodyFormat = MailFormat.Html;
	//msgMail.Body = sb.ToString();
	msgMail.Body = backOrderNotice;

	SmtpMail.Send(msgMail);
	return;
}

string GetCustEmailadd(int card_id)
{
	int rows = 0;
	string s_email = "";
	string sc = "SELECT email FROM card WHERE id=" + card_id;

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "cust_email");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return s_email = m_sSalesEmail;
	}

	if(rows > 0)
		return s_email = dst.Tables["cust_email"].Rows[0]["email"].ToString();
	else
		return s_email = m_sSalesEmail;

}

void AdminMsgDie(string msg)
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h3>" + msg + "</h3>");
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('");
	Response.Write("olist.aspx?r=" + DateTime.Now.ToOADate() + "') value='Back to Order List'></center>");
	Response.End();
}

bool GetOrderInfo()
{
	int rows = 0;
	string sc = "SELECT SUM(i.commit_price * i.quantity) as total, c.id AS card_id,i.pack ";
	sc += " FROM order_item i JOIN orders o ON i.id=o.id LEFT OUTER JOIN card c ON c.id=o.card_id ";
	sc += " WHERE o.id=" + m_id;
	sc += " GROUP BY c.id ";
	sc += " , i.pack ";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "order_info");
		if(rows != 1)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<h3>Order ID " + m_id + " not found</h3>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	m_dOrderTotal = MyDoubleParse(dst.Tables["order_info"].Rows[0]["total"].ToString());
	m_cardID = dst.Tables["order_info"].Rows[0]["card_id"].ToString();
	return true;
}

bool GetOrder()
{
	int rows = 0;
	string sc = "SELECT i.*, o.branch, o.status, o.number, o.special_shipto, o.shipto, o.card_id, o.po_number, i.pack,";
	sc += " o.record_date, o.contact, o.sales, o.freight, o.locked_by, o.time_locked, p.stock ";
	//if(g_bRetailVersion)
		sc += ", ISNULL(q.qty, 0) AS real_stock ";
	//	sc += ", cs.is_service ";
	sc += ", o.shipping_method, o.pick_up_time, o.sales_note, o.ship_as_parts, k.kit_id ";
	sc += " FROM order_item i JOIN orders o ON i.id=o.id ";
//	if(g_bRetailVersion)
//	if(Session["branch_support"] != null)
		sc += " LEFT OUTER JOIN stock_qty q ON q.code = i.code AND q.branch_id = o.branch ";
	sc += " LEFT OUTER JOIN order_kit k ON k.id = o.id AND k.krid = i.krid ";
	sc += " LEFT OUTER JOIN product p ON p.code=i.code ";
//	sc += " JOIN code_relations cs = ON cs.code = i.code ";
	sc += " WHERE o.id=" + m_id;
	sc += " ORDER BY i.kid ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "product");
		if(rows <= 0)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<h3>Order ID " + m_id + " not found</h3>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//check if has kit
	for(int m=0; m<dst.Tables["product"].Rows.Count; m++)
	{
		if(MyBooleanParse(dst.Tables["product"].Rows[m]["kit"].ToString()))
		{
			m_bHasKit = true;
			break;
		}
	}

	string locker = dst.Tables["product"].Rows[0]["locked_by"].ToString();
	Trim(ref locker);
	if(locker != "")
	{
		if(locker != Session["card_id"].ToString())
		{
			string lockname = TSGetUserNameByID(locker);
			string locktime = DateTime.Parse(dst.Tables["product"].Rows[0]["time_locked"].ToString()).ToString("dd-MM-yyyy HH:mm");
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3><font color=red>ORDER LOCKED</font></h3><br>");
			Response.Write("<h4>This order is locked by <font color=blue>" + lockname.ToUpper() + "</font> since " + locktime);
			PrintAdminFooter();
			return false;
		}
	}
	m_status = GetEnumValue("order_item_status", dst.Tables["product"].Rows[0]["status"].ToString());
	if(m_status == "Invoiced" || m_status == "Shipped" || m_status == "Returned")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3><font color=red>Order has already been " + m_status);
		PrintAdminFooter();
		return false;
	}

	m_orderNumber = dst.Tables["product"].Rows[0]["number"].ToString();
	m_orderBranch = dst.Tables["product"].Rows[0]["branch"].ToString();
	if(m_orderBranch == "")
		m_orderBranch = "1";

	//lock it
	sc = " UPDATE orders SET locked_by=" + Session["card_id"].ToString();
	sc += ", time_locked=GETDATE() ";
	sc += " WHERE id=" + m_id;
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

void DrawTableHeader()
{
//	Response.Write("<table width=100%  align=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	DataRow dr = dst.Tables["product"].Rows[0];
	bool bShipAsParts = bool.Parse(dr["ship_as_parts"].ToString());

	Response.Write("<tr><td colspan=6><h4>" + TSGetUserCompanyByID(dr["card_id"].ToString()) );
	Response.Write("&nbsp&nbsp&nbsp&nbsp; " + GetEnumValue("shipping_method", dr["shipping_method"].ToString()).ToUpper() );
	Response.Write("&nbsp&nbsp&nbsp&nbsp; " + dr["po_number"].ToString() );
	Response.Write("&nbsp&nbsp&nbsp&nbsp; " + dr["pick_up_time"].ToString());
	if(bShipAsParts)
		Response.Write("&nbsp&nbsp&nbsp&nbsp; <font color=Green>Supply Parts Only</font>");
	Response.Write("</h4></td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=70>PACK/BOX#</td>");
	Response.Write("<td width=150>M_PN</td>");
	Response.Write("<td>DESCRIPTION</td>");
	Response.Write("<td>STOCK</td>");
	Response.Write("<td>QTY</td>");
//	if(Session["branch_support"] != null)
//		Response.Write("<td>BRANCH</td>");
//	Response.Write("<td>SUPPLIER_CODE</td>");
//	Response.Write("<td>SUPPLIER</td>");
//	Response.Write("<td align=right>SELECT</td>");
	Response.Write("</tr>\r\n");
//	Response.Write("</table>");
}

Boolean DrawRow(DataRow dr, ref int i, Boolean alterColor)
{
	string code = dr["code"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string name = dr["item_name"].ToString();
	string stock = dr["stock"].ToString();
	
//	if(g_bRetailVersion)
		stock = dr["real_stock"].ToString();
	string quantity = dr["quantity"].ToString();
	bool bKit = MyBooleanParse(dr["kit"].ToString());
	string kit_id = dr["kit_id"].ToString();
	string pack = dr["pack"].ToString();
	if(bKit)
		name = "<font color=red><i>(" + m_sKitTerm + " #" + kit_id + ")</i></font> " + name;

	int qty = MyIntParse(quantity);
	
	//******analyze total value is negative or positive to show as credit or invoice
	double dcommitprice = MyDoubleParse(dr["commit_price"].ToString());
	dcommitprice = dcommitprice * qty;
	m_dTotalValue += dcommitprice;
//	if(qty < 0)
//		m_bCreditReturn = true;

	int qty1 = qty;
	int qty2 = 0;
	int nstock = 0;
	if(stock != "")
		nstock = MyIntParse(stock);
	if(m_splits > 0 && m_splits < 63 && m_sr[m_sp] == i)
	{
		qty1 = qty/2;
		if(qty1 > nstock && nstock > 0)
			qty1 = nstock;
		qty2 = qty - qty1;
	}

	Response.Write("<input type=hidden name=code" + i.ToString() + " value='" + code + "'>");
	Response.Write("<input type=hidden name=name"+ i +" value="+ dr["item_name"].ToString() +">");
//	Response.Write("<input type=hidden name=service"+ i +" value="+ dr["is_service"].ToString() +">");
	Response.Write("<input type=hidden name=itemID"+ i +" value="+ dr["kid"].ToString() +">");
	Response.Write("<input type=hidden name=hstock"+ i +" value="+ stock +">");
	
	
	Response.Write("<input type=hidden name=hqty"+ i +" value="+ qty +">");

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");
	Response.Write("<td>");
	Response.Write(pack);
	Response.Write("<input type=checkbox name=sel" + i.ToString());
	if(m_totalRows == 1)
		Response.Write(" checked");
	Response.Write(">");
	Response.Write("</td><td>");
	Response.Write(supplier_code);
	Response.Write("</td><td>");
	Response.Write(name);
	Response.Write("</td><td>");
	Response.Write(stock);

	//qty
	Response.Write("</td><td>");
	if(m_splits > 0 && m_splits < 63 && m_sr[m_sp] == i)
	{
		Response.Write("<input type=hidden name=splited" + i.ToString() + " value=1>");
		Response.Write("<input type=hidden name=total_qty" + i.ToString() + " value='" + qty + "'>");
		Response.Write("<input type=text size=3 autocomplete=off name=qty" + i.ToString());
		Response.Write(" value=" + qty1 + " onchange=\"OnChangeQty1(" + i.ToString() + ")\">");
	}
	else
		Response.Write(qty1);
	Response.Write("</td>");
//	Response.Write("<td align=right>");

//	Response.Write("<input type=checkbox name=sel" + i.ToString());
//	if(m_totalRows == 1)
//		Response.Write(" checked");
//	Response.Write(">");
	Response.Write("</td>");
	Response.Write("</tr>");

	if(m_splits > 0 && m_splits < 63 && m_sr[m_sp] == i)
	{
		i++;
		Response.Write("<input type=hidden name=splited" + i.ToString() + " value=1>");
		Response.Write("<input type=hidden name=total_qty" + i.ToString() + " value='" + qty + "'>");
		Response.Write("<input type=hidden name=code" + i.ToString() + " value='" + code + "'>");
		Response.Write("<input type=hidden name=h_sp_stock"+ i.ToString() +" value='" + stock +"'>");
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		Response.Write("<td>");
		Response.Write(code);
		Response.Write("</td><td>");
		Response.Write(supplier_code);
		Response.Write("</td><td>");
		Response.Write(name);
		Response.Write("</td><td>");
		Response.Write(stock);
		//qty
		
		Response.Write("</td>");
		
		Response.Write("<td>");
		Response.Write("<input type=text size=3 autocomplete=off name=qty" + i.ToString());
		Response.Write(" value=" + qty2 + " onchange=\"OnChangeQty2(" + i.ToString() + ")\">");
		Response.Write("</a>");
		Response.Write("</td><td align=right>");
		Response.Write("<input type=checkbox name=sel" + i.ToString() + ">");
		Response.Write("</td></tr>");
		
		m_sp++;
	}

	return true;
}

Boolean MyDrawTable()
{
	Boolean bRet = true;	
	Response.Write("<form name=form action=eorder.aspx?id=" + m_id);
	if(Request.QueryString["forceship"] != null)
		Response.Write("&forceship=1");
	Response.Write(" method=post>");

	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Order # </b><font color=red><b>");
	Response.Write(m_orderNumber + " - <font color=red>");
	Response.Write(m_status);
	Response.Write("</font></font>");
	if(Session["branch_support"] != null)
		Response.Write(" &nbsp;&nbsp;Branch: "+ PrintBranchOptions(Session["branch_id"].ToString()));
	Response.Write("</td>");

	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=7><br></td></tr>");
	
	/*Response.Write("<br><center><h3>Order #");
//	Response.Write("<b>Order Number : </b>");
//	Response.Write("<a href=olist.aspx?n=" + m_orderNumber + " class=o>" + m_orderNumber + "</a>&nbsp&nbsp;");
	Response.Write(m_orderNumber + " - <font color=red>");
	Response.Write(m_status);
	Response.Write("</font>");
	if(Session["branch_support"] != null)
		Response.Write("<br>Branch: "+ PrintBranchOptions(Session["branch_id"].ToString()));
	Response.Write("</h3>");
	*/	
	
//	Response.Write("<table width=100% bgcolor=white align=center>");
//	Response.Write("<tr><td>");

	DrawTableHeader();
	string s = "";
	Boolean alterColor = true;
	int rows = dst.Tables["product"].Rows.Count;
	m_totalRows = rows;
	int index = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, ref index, alterColor))
		{
			bRet = false;
			break;
		}
		index++;
//		m_rows++;
	}

	Response.Write("<tr><td colspan=" + cols + " align=left><b>Select All </b>");
	Response.Write("<input type=checkbox name=allbox value='Select All' onClick='CheckAll();'>");
	Response.Write("</td></tr>");
	if(m_dTotalValue < 0)
		m_bCreditReturn = true;

	string note = dst.Tables["product"].Rows[0]["sales_note"].ToString();
	if(note != "")
	{
		note = note.Replace("\r\n", "\r\n<br>");
		Response.Write("<tr bgcolor=#EEEEE ><td rowspan=2><h3><font color=red>Customer Note : </font></h3><h4>");
		Response.Write(note);
		Response.Write("</h4></td></tr>");
	}
	Response.Write("<tr bgcolor=#EEEEE><td valign=bottom colspan=" + cols + " align=right>");
	string keyEnter = "onKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"";
	if(m_bChangeInvoiceNumber)
	{
			Response.Write("<table><tr><td>");
			Response.Write("<i><font size=2 color=red>*(if the manual invoice number is entered greater than the invoice on the system, then the system will carry on the bigger number on the new invoice.)<i>");
			Response.Write("</td></tr></table>");
			Response.Write(" <b>INVOICE/CREDIT NO:</b><input size='10' maxlength=10 style='border-style: double; border-width: 1px' type=text name=manual_invoice_number "+ keyEnter +"> &nbsp;");
	}
	if(m_bAllowChangeDate)
    {
		Response.Write(" <b>INVOICE DATE:</b><i>(dd-mm-yy)</i><input size='10' maxlength=10 style='border-style: double; border-width: 1px' type=text name=invoice_date "+ keyEnter +" ");
	    Response.Write(" onclick='displayDatePicker(this.name);' value='"+ DateTime.Now.ToString("dd-MM-yyyy")+"'> &nbsp;");
    }
	Response.Write("<b>CHANGE TO : </b>");
	
	if(m_bCreditReturn)
		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Credit'>");
	else
		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Invoice'>");

	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='BackOrder'>");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='OnHold'>");
	if(m_splits <= 0)
	{
		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Split'>");
		Response.Write("<input type=hidden name=h_splits value=0>");
	}
	else
	{
		Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='UnSplit'>");
		Response.Write("<input type=hidden name=h_splits value=1>");
	}
	Response.Write("</td></tr></table>");
// **** notes ******//
/*	string note = dst.Tables["product"].Rows[0]["sales_note"].ToString();
	if(note != "")
	{
		note = note.Replace("\r\n", "\r\n<br>");
		Response.Write("<br><br><h3><font color=red>Customer Note : </font></h3><h4>");
		Response.Write(note);
		Response.Write("</h4>");
	}
	*/
	Response.Write("<input type=hidden name=rows value=" + index + ">");
	Response.Write("</form>");
	return bRet;
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.form.elements.length;i++) 
		{
			var e = document.form.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form.allbox.checked;
		}
	}
	";
	Response.Write(s);

	if(m_splits > 0)
	{
		Response.Write("function OnChangeQty1(r)\r\n");
		Response.Write("{\r\n");
		Response.Write("	var total = Number(eval(\"document.form.total_qty\" + r + \".value\"));\r\n");
		Response.Write("	var qty1 = Number(eval(\"document.form.qty\" + r + \".value\"));\r\n");
		Response.Write("	eval(\"document.form.qty\" + (r+1) + \".value=total-qty1\");\r\n");
		Response.Write("}\r\n");

		Response.Write("function OnChangeQty2(r)\r\n");
		Response.Write("{\r\n");
		Response.Write("	var total = Number(eval(\"document.form.total_qty\" + r + \".value\"));\r\n");
		Response.Write("	var qty2 = Number(eval(\"document.form.qty\" + r + \".value\"));\r\n");
		Response.Write("	eval(\"document.form.qty\" + (r-1) + \".value=total-qty2\");\r\n");
		Response.Write("}\r\n");
	}

	Response.Write("</script");
	Response.Write(">");
}

bool DoSplit()
{
	if(!GetOrder())
		return false;

	int index = 0;
	for(int i=0; i<m_rows; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
		{
			if(m_splits > 62)
			{
				Response.Write("<br><br><center><h3>Error, too many rows to split, max is 64</h3>");
				return false;
			}
			m_sr[m_splits] = index;
			m_splits++;
			index++;
		}
		index++;
	}
	PrintAdminHeader();
	PrintAdminMenu();
	PrintJavaFunctions();
	MyDrawTable();

	return true;
}

bool DoUpdateOrder(string status)
{
	if(!GetOrder())
		return false;

	m_new_id = m_id;
	string sc = "";
	int selected = 0;
	for(int i=0; i<m_rows; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
			selected++;
	}
	if(selected <= 0)
	{
		Response.Write("<br><br><center><h3>Error, no row selected</h3>");
		return false;
	}
	else if(selected == m_rows) //select all
	{
		sc = "UPDATE orders SET status=" + status;
		sc += ", locked_by=null, time_locked=null ";
		sc += " WHERE id=" + m_id;
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
	else if(m_bHasKit)
	{
		Response.Write("<br><br><center><h3>Sorry, please select all rows, Kit orders have to be processed in whole</h3>");
		return false;
	}

	DataRow dr = dst.Tables["product"].Rows[0];
	m_orderNumber = dr["number"].ToString();

	string part = "0";
	sc = "SELECT part FROM orders WHERE number=" + m_orderNumber + " ORDER BY part DESC";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand1.Fill(dst, "part");
		part = rows.ToString(); //new order part number eg:10015.1
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	string special_shipto = "0";
	if(bool.Parse(dr["special_shipto"].ToString()))
		special_shipto = "1";

	string shipping_method = dr["shipping_method"].ToString();
	string pickup_time = dr["pick_up_time"].ToString();

	//split order
	sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (branch, number, part, card_id, po_number, contact, special_shipto, shipto ";
	sc += ", shipping_method, pick_up_time, status, record_date, sales)";
	sc += " VALUES(" + m_orderBranch + ", " + m_orderNumber + ", " + part + ", " + dr["card_id"].ToString() + ", '";
	sc += EncodeQuote(dr["po_number"].ToString()) + "', '" + EncodeQuote(dr["contact"].ToString()) + "', " + special_shipto + ", '";
	sc += EncodeQuote(dr["shipto"].ToString()) +  "', " + shipping_method + ", '" + pickup_time + "', " + status + ", ";
//	sc += dr["record_date"].ToString() + "', '" + dr["sales"].ToString();
	sc += "GETDATE(), '" + dr["sales"].ToString();
	sc += "') SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "id") == 1)
		{
			m_new_id = dst.Tables["id"].Rows[0]["id"].ToString();
		}
		else
		{
			Response.Write("<br><br><center><h3>Error get new id</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<m_rows; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
		{
			string code = Request.Form["code" + i.ToString()];
			if(Request.Form["splited" + i.ToString()] == "1")
			{
				string qty = Request.Form["qty" + i.ToString()];
				string total_qty = Request.Form["total_qty" + i.ToString()];
				int qty_remain = MyIntParse(total_qty) - MyIntParse(qty);
				if(!DoSplitItem(m_id, m_new_id, code, qty, qty_remain.ToString()))
					return false;
			}
			else
			{
				if(!DoUpdateItem(m_id, m_new_id, code))
					return false;
			}
		}
	}
	return true;
}

bool DoSplitItem(string id, string new_id, string code, string qty, string qty_remain)
{
	int rows = 0;
	string sc = "SELECT * FROM order_item WHERE id=" + id + " AND code=" + code;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "oneitem");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dst.Tables["oneitem"].Rows[0];

	//insert new row for splited backorders 
	sc = "INSERT INTO order_item (id, code, quantity, item_name, commit_price, supplier, supplier_code, supplier_price, eta, note)";
	sc += "VALUES(" + new_id + ", " + code + ", " + qty + ", '" + EncodeQuote(dr["item_name"].ToString()) + "', ";
	sc += Math.Round(MyDoubleParse(dr["commit_price"].ToString()), 2) + ", '";
	sc += dr["supplier"].ToString() + "', '" + dr["supplier_code"].ToString() + "', " + dr["supplier_price"].ToString();
	sc += ", '" + EncodeQuote(dr["eta"].ToString()) + "', '" + EncodeQuote(dr["note"].ToString()) + "') ";
	//update remain record's quantity
	sc += " UPDATE order_item SET quantity=" + qty_remain + " WHERE id=" + id + " AND code=" + code;
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
	dst.Tables["oneitem"].Clear();
	return true;
}

bool DoUpdateItem(string id, string new_id, string code)
{
	string sc = "UPDATE order_item SET id=" + new_id + " WHERE id=" + id + " AND code=" + code;
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

bool CreateInvoice(string id)
{
	DataRow dr = null;
	double dPrice = 0;
	double dFreight = 0;
	double dTax = 0;
	double dTotal = 0;
	int rows = 0;
	//string sc = "SELECT * FROM orders o INNER JOIN card c ON o.sales = c.id WHERE ";
	//sc += " o.id=" + id;
	string sc = "SELECT o.*, c.name, c.company, c.trading_name, c.address1, c.address2, c.address3 ";
	sc += ", c.phone, c.fax, c.email, c.postal1, c.postal2, c.postal3, c.type AS cardType, b.id AS to_branch_id ";
	sc += " FROM orders o LEFT OUTER JOIN card c ON c.id = o.card_id ";
	sc += " LEFT OUTER JOIN branch b ON b.id = c.our_branch AND c.name = b.name ";
	sc += " WHERE o.id=" + id;		
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "invoice");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error creating invoice, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	dr = dst.Tables["invoice"].Rows[0];
	string card_id = dr["card_id"].ToString();
	string po_number = dr["po_number"].ToString();
	string m_shippingMethod = dr["shipping_method"].ToString();
	string m_pickupTime = dr["pick_up_time"].ToString();
	string m_branch_id = dr["branch"].ToString();
    string customerType = dr["cardType"].ToString(); ////////check if it's member or not...
	string to_branch_id = dr["to_branch_id"].ToString();
	string quote_total = dr["quote_total"].ToString();
	double dQuoteTotal = 0;
	if(quote_total != "" && quote_total != "0")
		dQuoteTotal = MyDoubleParse(quote_total);
	string nip = "0";
	if(bool.Parse(dr["no_individual_price"].ToString()))
		nip = "1";
	string gst_inclusive = "0";
	if(bool.Parse(dr["gst_inclusive"].ToString()))
		gst_inclusive = "1";

	//string sales_person = dr["name"].ToString();
	if(m_branch_id == "")
		m_branch_id = "1";

	string sales = dr["sales"].ToString();
	if(sales != "")
		sales = TSGetUserNameByID(sales);
		
	string custGst = dr["customer_gst"].ToString();
	
	dFreight = MyDoubleParse(dst.Tables["invoice"].Rows[0]["freight"].ToString());

	sc = "SELECT * FROM order_item WHERE id=" + id +" ORDER BY kid ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "item");
		if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		double dp = MyDoubleParse(dr["commit_price"].ToString());		
		double dDiscountPercent = MyDoubleParse(dr["discount_percent"].ToString());
		dDiscountPercent /= 100;	
		dp = Math.Round(dp, 4);
		int qty = MyIntParse(dr["quantity"].ToString());
		dPrice += (dp * (1 - dDiscountPercent)) * qty;
		
//		string sprice = dPrice.ToString("c");
//		sprice = sprice.Replace("$", "");
//		dPrice = MyDoubleParse(sprice);
		dPrice = Math.Round(dPrice, 4);
	}
	//dPrice += dFreight;
	if(dQuoteTotal != 0)
		dPrice = dQuoteTotal; //keep quotation discount
//return false;
	dTax = (dPrice + dFreight) * MyDoubleParse(custGst);//GetGstRate(card_id);
//	string stax = dTax.ToString("c");
//	stax = stax.Replace("$", "");
//	dTax = MyDoubleParse(stax);
	
	dTax = Math.Round(dTax, 4);
	
	dTotal = dPrice + dFreight + dTax;
//string total = dTotal.ToString("c");
//total = total.Replace("$","");
//dTotal = MyDoubleParse(total);
//DEBUG("dtotal = ",dTotal.ToString());	
//return false;
	dTotal = MyCurrencyPrice(dTotal.ToString("c"));
	dr = dst.Tables["invoice"].Rows[0];
	string special_shipto = "0";
	if(bool.Parse(dr["special_shipto"].ToString()))
		special_shipto = "1";
	
	string receipt_type = GetEnumID("receipt_type", "invoice");
	string salesNote = dr["sales_note"].ToString();
	if(m_bCreditReturn)
	{
		receipt_type = "6";//GetEnumID("receipt_type", "credit note");
		salesNote = "credit for invoice # "+ dr["invoice_number"].ToString() +"";
	}

	string sbSystem = "0";
	if(bool.Parse(dr["system"].ToString()))
		sbSystem = "1";

	sc = "BEGIN TRANSACTION ";
	
	if(m_bChangeInvoiceNumber)
		sc += " SET IDENTITY_INSERT invoice ON  ";
	sc += " SET DATEFORMAT dmy ";
	sc += "INSERT INTO invoice (branch, type, card_id, price, tax, total, commit_date, special_shipto, shipto ";
	sc += ", name, company, trading_name, address1, address2, address3, postal1, postal2, postal3, phone, fax, email ";
	sc += ", freight, cust_ponumber, shipping_method, pick_up_time, sales, sales_note, system, payment_type ";
	sc += ", no_individual_price, gst_inclusive ";
	if(m_bChangeInvoiceNumber)
		sc += ", invoice_number ";	
	sc += ", customer_gst ";
	sc += ")";
	sc += " VALUES(" + m_branch_id + ", " + receipt_type + ", " + dr["card_id"].ToString() + ", " + dPrice;
	sc += ", " + dTax + ", ROUND(" + dTotal + ", 2) ";
	if(m_bAllowChangeDate)
		sc += ", '"+ m_invoice_date +"' ";
	else
		sc += ", GETDATE() ";	
	sc += ", "+special_shipto + ", '" + EncodeQuote(dr["shipto"].ToString()) + "' ";
	sc += ", N'" + EncodeQuote(dr["name"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["company"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["trading_name"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["address1"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["address2"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["address3"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["postal1"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["postal2"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["postal3"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["phone"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["fax"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["email"].ToString()) + "' ";
	sc += ", " + dFreight + ", '" + EncodeQuote(po_number) + "', ";
	sc += m_shippingMethod + ", '" + EncodeQuote(m_pickupTime) + "', '" + EncodeQuote(sales) + "', '";
	sc += EncodeQuote(dr["sales_note"].ToString()) + "' ";
	sc += ", " + sbSystem + ", " + dr["payment_type"].ToString(); 
	sc += ", " + nip;
	sc += ", " + gst_inclusive;
	if(m_bChangeInvoiceNumber)
		sc += ", "+ m_manual_invoice_number +"";
	sc += ", '" + custGst+"'";
	sc += " )";
	if(m_bChangeInvoiceNumber )
		sc += " SET IDENTITY_INSERT invoice OFF ";
	
	sc += " SELECT IDENT_CURRENT('invoice') AS id";	
	
	sc += " COMMIT ";
//DEBUG("sc = ", sc);

	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "invoice_id") == 1)
		{	
			m_invoiceNumber = dst.Tables["invoice_id"].Rows[0]["id"].ToString();
			if(m_bChangeInvoiceNumber)
				m_invoiceNumber = m_manual_invoice_number;
		
		}
		else
		{
			Response.Write("<br><br><center><h3>Error get new invoice number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//update order to record invoice number
	sc = "UPDATE orders SET invoice_number=" + m_invoiceNumber + " ";
	// auto set unchecked to orders once the card privilege is above. 
	if(bCheckCardPrivilege(Session["card_id"].ToString()))
		sc += ", unchecked = 0 ";
	sc += " WHERE id=" + id;
	sc += " UPDATE sales_serial SET invoice_number = " + m_invoiceNumber + " WHERE order_id = " + m_id;
	sc += " UPDATE serial_trace SET invoice_number = " + m_invoiceNumber + " WHERE order_id = " + m_id;
	sc += " UPDATE invoice SET invoice_number = id WHERE id = " + m_invoiceNumber; //this for qpos
	sc += " UPDATE settings SET value = '"+ (int.Parse(m_invoiceNumber) + 1).ToString() +"' WHERE name = 'qpos_next_invoice_number' ";	
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

	bool bHasKit = false;

	//write price history
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		string commit_price = dr["commit_price"].ToString();
		string quantity = dr["quantity"].ToString();
		string code = dr["code"].ToString();
		string name = dr["item_name"].ToString();
		string kit = dr["kit"].ToString();
		string krid = dr["krid"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		string pack = dr["pack"].ToString();
		sbSystem = "0";
		if(bool.Parse(dr["system"].ToString()))
			sbSystem = "1";
		if(name.Length > 255)
			name = name.Substring(0, 255);

		string sKit = "0";
		if(MyBooleanParse(kit))
		{
			sKit = "1";
			bHasKit = true;
		}
		if(krid == "")
			krid = "null";
	
		sc = "BEGIN TRANSACTION INSERT INTO sales (invoice_number, code, name, quantity, commit_price, supplier, supplier_code, supplier_price, system, kit, krid ";
		sc += ", income_account, costofsales_account , discount_percent, pack";
		sc += ")";
		//sc += " VALUES(" + m_invoiceNumber + ", " + code + ", '" + name + "', " + quantity + ", " + commit_price + ", ";
		//sc += "'" + supplier + "', '" + supplier_code + "', " + supplier_price + ", " + sbSystem + ", " + sKit + ", " + krid + ")";
		sc += " SELECT " + m_invoiceNumber + ", " + code + ", N'" + EncodeQuote(name) + "', " + quantity + ", " + commit_price + ", ";
		sc += "'" + supplier + "', '" + supplier_code + "', " + supplier_price + ", " + sbSystem + ", " + sKit + ", " + krid + " ";
		sc += ", income_account,   costofsales_account , "+ dr["discount_percent"].ToString() +"";
		sc +=", N'"+pack+"'";
		sc += " FROM code_relations WHERE code = "+ code +" ";
		
		sc += " UPDATE account SET balance = balance + ("+ commit_price +" * "+ quantity +") FROM code_relations cs JOIN account a ON (a.class1 * 1000) + (a.class2 * 100) + (a.class3 * 10) + a.class4 =  cs.income_account WHERE (class1 * 1000) + (class2 * 100) + (class3 * 10) + class4 =  cs.income_account AND cs.code = "+ code +"";
		sc += " UPDATE account SET balance = balance + ("+ supplier_price +" * "+ quantity +") FROM code_relations cs JOIN account a ON (a.class1 * 1000) + (a.class2 * 100) + (a.class3 * 10) + a.class4 =  cs.costofsales_account WHERE (class1 * 1000) + (class2 * 100) + (class3 * 10) + class4 =  cs.costofsales_account AND cs.code = "+ code +"";
		sc += " UPDATE account SET balance = balance - ("+ supplier_price +" * "+ quantity +") FROM code_relations cs JOIN account a ON (a.class1 * 1000) + (a.class2 * 100) + (a.class3 * 10) + a.class4 =  cs.inventory_account WHERE (class1 * 1000) + (class2 * 100) + (class3 * 10) + class4 =  cs.inventory_account AND cs.code = "+ code +"";
		if(to_branch_id != "")
		{
			sc += " IF EXISTS(SELECT id FROM stock_qty WHERE branch_id = " + to_branch_id + " AND code = '" + code + "') ";
			sc += " UPDATE stock_qty SET qty = qty + " + quantity + " WHERE branch_id = " + to_branch_id + " AND code = '" + code + "'; ";
			sc += " ELSE ";
			sc += " INSERT INTO stock_qty (branch_id, qty, code) VALUES(" + to_branch_id + ", " + quantity + ", " + code + "); ";
		}
		sc += " COMMIT ";
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
		
		int nQty = MyIntParse(quantity);
		fifo_sales_update_cost(m_invoiceNumber, code, commit_price, m_branch_id, nQty);
		//update stock qty
		fifo_updateStockQty(nQty, code, m_branch_id, m_id);
//		CheckAC200Item(m_invoiceNumber, code, supplier_code, commit_price); //for unknow item

	}

	if(bHasKit)
	{
		if(!RecordKitToInvoice(id, m_invoiceNumber))
			return true;
	}

	UpdateCardAverage(card_id, dPrice, MyIntParse(DateTime.Now.ToString("MM")));
	UpdateCardBalance(card_id, dTotal);

	//****update and delete file that create in importing by file ****//
	doDeleteImportedFile(id);	

/*	///create a invoice for qpos///
	Response.Write("<form name=f1 ><input type=hidden name=next_inv value="+ (int.Parse(m_invoiceNumber) + 1ss).ToString()+"></form>");
	Response.Write("<script language=javascript> ");
	string s = @" 
		var fn = 'c:/qpos/qposni.txt'; 
	var inv = Number(document.f1.next_inv.value);
	fso = new ActiveXObject('Scripting.FileSystemObject'); 	
		fso.DeleteFile(fn);
		tf = fso.OpenTextFile(fn , 8, 1, -2);
		tf.Write(inv);
		tf.Close();	
		";
		Response.Write(s);
		Response.Write(" </script ");
		Response.Write(">");
		*/
	return true;
}

bool isCodeIntheList(String [] code, String [] compareCode, int rows, ref double totalStock, ref double totalOrder)
{
	for(int i=0; i<rows; i++)
	{
		if(String.Compare(code[i],compareCode[i]) == 0)
			return true;
	}
	return false;
}

bool CheckCurrentStock()
{
string sc = "";
bool bFoundNoStock = false;
//check if has kit
	int nStock = 0;
	
	string scode = "";
	string sname = "";
	
	int rows = 0;
	sc = " SELECT DISTINCT SUM(i.quantity) AS quantity, i.code, i.item_name, isnull(cs.is_service,0) AS is_service ";
	sc += ",  s.qty AS 'stock' ";			
	sc += " FROM order_item i JOIN orders o ON i.id=o.id ";
	sc += " JOIN code_relations cs ON cs.code = i.code ";
	sc += " LEFT OUTER JOIN order_kit k ON k.id = o.id ";
	sc += " LEFT OUTER JOIN stock_qty s ON (s.code=i.code AND s.branch_id=o.branch) ";		
	sc += " WHERE o.id = "+m_id;
			sc += " GROUP BY i.code,i.item_name,  cs.is_service, s.qty ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "checkstock");
						
		if(rows <= 0)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<h3>Order# " + m_orderNumber + " not found</h3>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
			
	
	for(int m=0; m<rows; m++)
	{
		DataRow dr = dst.Tables["checkstock"].Rows[m];
		string stock = dr["stock"].ToString();
		
		bool biservice = MyBooleanParse(dr["is_service"].ToString());				
		
		scode = dr["code"].ToString();
		sname = dr["item_name"].ToString();
		if(biservice) //return true if it's service item
		{
			bFoundNoStock = false;
			break;			
		}
		double totalOrder = 0;
		bool avaliableCode = false;
		for(int i=0; i<m_rows; i++)
		{			
			string oqty = "";
			if(Request.Form["sel" + i.ToString()] == "on")
			{			
				if(scode == Request.Form["code"+ i])
				{
					avaliableCode = true;

					oqty = Request.Form["hqty"+ i];
					if(Request.Form["h_splits"].ToString() == "1")
					{
						oqty = Request.Form["qty" + i.ToString()];
					}
					
					if(oqty == "" || oqty == null)
						oqty = "0";
					if(!TSIsDigit(oqty))
						oqty = "0";	
					totalOrder += MyDoubleParse(oqty);
		//			DEBUG("order = ", totalOrder);
				}	
				
			}
			//DEBUG("availiableCode = ", avaliableCode);
		}
//		DEBUG("availiableCode = ", avaliableCode);
//		DEBUG("order = ", totalOrder);
		if(avaliableCode)
		{
			if(MyDoubleParse(stock) - totalOrder < 0)
			{
				bFoundNoStock = true;
				break;
			}
		}
		
	}
//DEBUG(" found =", bFoundNoStock);	
	
	if(bFoundNoStock)
	{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h4><font color=red>***Item Out of Stock, Cannot create invoice.***</h4>");
			Response.Write("<br><h4>***Please Put on Back Order First.***</font></h4>");
			Response.Write("<h5>#" + scode + " : " + sname + "</h5>");
			Response.Write("<h5>Current Stock : " + nStock.ToString() + "</h5>");
			Response.Write("<input type=button value=' Back '  "+ Session["button_style"] +"  onclick=history.go(-1)>");
			return false;
	}
//return false;
/*	string sc = " SELECT i.*, o.status, o.number, o.special_shipto, o.shipto, o.card_id, o.po_number, ";
	sc += " o.record_date, o.contact, o.sales, o.freight, o.locked_by, o.time_locked, s.qty AS 'stock' ";
	sc += ", o.shipping_method, o.pick_up_time, o.sales_note, k.kit_id ";
	sc += " FROM order_item i JOIN orders o ON i.id=o.id ";
	sc += " LEFT OUTER JOIN order_kit k ON k.id = o.id ";
	sc += " LEFT OUTER JOIN stock_qty s ON (s.code=i.code AND s.branch_id=o.branch) ";
	sc += " WHERE o.id=" + m_id;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "checkstock");
		if(rows <= 0)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<h3>Order# " + m_orderNumber + " not found</h3>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//check if has kit
	int nStock = 0;
	for(int m=0; m<dst.Tables["checkstock"].Rows.Count; m++)
	{
		DataRow dr = dst.Tables["checkstock"].Rows[m];
		string stock = dr["stock"].ToString();
		string qty = dr["quantity"].ToString();
		if(MyIntParse(qty) < 0)
			continue; //credit is ok

		if(stock != "")
			nStock = MyIntParse(stock);
		if(nStock <= 0)
		{
			string code = dr["code"].ToString();
			string name = dr["item_name"].ToString();
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h4><font color=red>***Item Out of Stock, Cannot create invoice.***</h4>");
			Response.Write("<br><h4>***Please Put on Back Order First.***</font></h4>");
			Response.Write("<h5>#" + code + " : " + name + "</h5>");
			Response.Write("<h5>Current Stock : " + nStock.ToString() + "</h5>");
			Response.Write("<input type=button value=' Back '  "+ Session["button_style"] +"  onclick=history.go(-1)>");
			return false;
		}
	}
	*/
	return true;
}
bool IsExitsInvoice(string manualINV)
{
	bool bFoundInvoice = false;
	
	string sc = " SELECT invoice_number FROM invoice WHERE invoice_number = "+ manualINV +"";
//DEBUG("sc += ", sc);
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "foundINV") > 0)
			bFoundInvoice = true;
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}		

	return bFoundInvoice;
}
bool doDeleteImportedFile(string id)
{	
	string sc = " SELECT TOP 1 ISNULL(file_name, '')AS file_name, id FROM import_sales_order_format WHERE order_number = "+ id;
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "getFileName");		
	}
	catch(Exception e) 
	{
		//ShowExp(sc, e);
		return true;
	}		
	
	if(rows > 0)
	{
		string fileName = dst.Tables["getFileName"].Rows[0]["file_name"].ToString();
		string fileID = dst.Tables["getFileName"].Rows[0]["id"].ToString();
		if(fileName != "")
		{
			string root = GetRootPath() + "data/inv";
			root = Server.MapPath(root);
			string pathname = root + "\\" +  fileName;
			if(File.Exists(pathname))
				File.Delete(pathname);
			
		/*	//update the status// to be deleted
			sc = " UPDATE import_sales_order_format SET is_delete = 1 WHERE order_number = "+ id +" AND id = "+ fileID;
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
				//ShowExp(sc, e);
				return true;
			}
			*/
		}
	}
	return true;
}
</script>
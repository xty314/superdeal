<!-- #include file="fifo_f.cs" -->
<!-- #include file="page_index.cs" -->

<script runat=server>

string m_step = "welcome";
string m_old_item_status = "";
DataSet dst = new DataSet();
string m_orderID = "";
string m_orderNumber = "";
string m_invoiceNumber = "";
double m_dInvoiceTotal = 0;
string m_itemStatus = "1";
string m_oldItemStatus = "1";
string m_itemFault = "";
string m_itemName = "";
string m_itemCode = "";
string m_itemQty = "1";
string m_restockCode = "";

string m_replaceItemCode = "";
string m_replaceItemName = "";
string m_replaceItemQty = "1";
string m_replaceItemSupplier = "";
string m_replaceItemSupplierCode = "";
string m_replaceItemSupplierPrice = "";
string m_rmaItemID = "";
string m_rmaCardID = "";
string m_branchID = "1";
string m_supplierID = "";

string m_poID = "";
string m_restockItemName = "";
bool m_bSentToSupplier = false;
string m_code_received_from_supplier = "";
string m_date_received_from_supplier = "";

int nRows = 0; //for db queries

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		m_step = Request.QueryString["t"];

	switch(m_step)
	{
	case "welcome":
		PrintRMAHeader();
		PrintWelcomeMenu();
		break;
	case "reserve_number":
		if(Request.Form["kw"] != "" && Request.Form["kw"] != null && Request.QueryString["ci"] == null)
		{
			DoCustomerSearch();
			return;
		}
		else if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
		{
			if(Request.QueryString["confirmed"] == "1")
			{
				DoReserveNumber();
				return;
			}
			else if(Request.QueryString["number"] != null && Request.QueryString["number"] != "")
			{
				PrintRMAHeader();
				SayReserveOK();
			}
			else
			{
				PrintRMAHeader();
				PrintSelectCustomerConfirmPage();
			}
		}
		else
		{
			PrintRMAHeader();
			PrintSelectCustomerForm();
		}
		break;
	case "list":
		PrintRMAHeader();
		PrintRAMList();
		break;
	case "receive_item":
		if(Request.Form["cmd"] == "Receive")
		{
			DoReceiveItem();
			return;
		}
		else if(Request.QueryString["delete_kid"] != null && Request.QueryString["delete_kid"] != "")
		{
			DoDeleteReceivedItem();
			return;
		}
		else if(Request.Form["sn"] != null && Request.Form["sn"] != "")
		{
			DoReceiveScanSN();
			return;
		}
		else
		{
			PrintRMAHeader();
			PrintReceiveItemForm();
		}
		break;
	case "view":
		PrintRMAHeader();
		PrintRMAItemList(Request.QueryString["id"]);
		break;
	case "process":
		PrintRMAHeader();
		PrintItemProcessForm();
		break;
	case "trash":
		DoTrashItem();
		return;
		break;
	case "repair":
		DoRepairItem();
		return;
		break;
	case "noidea":
		DoNoIdeaItem();
		break;
	case "resell":
		if(Request.Form["cmd"] == "Sell")
		{
			DoResellItem();
			return;
		}
		PrintResellForm();
		break;
	case "replace":
		Session["back_to_sra"] = null;
		Session["sra_item_id"] = null;
		if(Request.QueryString["finished"] == "1")
		{
			PrintRMAHeader();
			Response.Write("<h4>Item Replaced</h4>");
			PrintItemDetails(Request.QueryString["item_id"]);
			PrintRMAFooter();
			return;
		}
		if(Request.Form["rma_card"] != null && Request.Form["rma_card"] != "")
		{
			SetSiteSettings("rma_account_number", Request.Form["rma_card"].ToString());
			DoItemReplacement();
		}
		if(Request.Form["cmd"] == "Replace")
		{
			DoItemReplacement();
			return;
		}
		else if(Request.Form["sn"] != null && Request.Form["sn"] != "")
		{
			DoSearchReplacementSN();
			return;
		}
		else
		{
			PrintRMAHeader();
			PrintReplaceForm();
		}
		break;
	case "restock_replacement":
		if(Request.Form["cmd"] == "Restock")
		{
			DoRestockReplacement();
			return;
		}
		if(Request.Form["code"] != null)
		{
			DoCheckReplacementCode();
		}
		if(Request.QueryString["finish"] == "1")
		{
			PrintResotckFinishPage();
			return;
		}
		PrintRestockReplacementForm();
		break;
	case "receive_replacement_scan":
		Session["back_to_sra"] = true;
		Session["sra_item_id"] = Request.QueryString["item_id"];
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=serial.aspx?n=" + Request.QueryString["n"] + "\">");
		return;
		break;
	case "sendtosupplier":
		PrintRMAHeader();
		if(DoSendToSupplier())
		{
			Response.Write("<br><br><center><h4>Done, please wait a second...</h4>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=sra.aspx?t=process&item_id=" + Request.QueryString["item_id"] + "\">");
		}
		break;
	default:
		break;
	}
	PrintRMAFooter();
}

void PrintRMAHeader()
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h1>RMA</h1>");
}

void PrintRMAFooter()
{
	Response.Write("<br><br><br>");
	Response.Write("<hr width=75%>");
	Response.Write("<a href=sra.aspx class=o>RMA Home</a>");
	Response.Write(" &nbsp;|&nbsp; ");
	Response.Write("<a href=sra.aspx?t=reserve_number class=o>New RMA</a>");
	Response.Write(" &nbsp;|&nbsp; ");
//	Response.Write("<a href=sra.aspx?t=list class=o>Receive Item</a>");
//	Response.Write(" &nbsp;|&nbsp; ");
	Response.Write("<a href=sra.aspx?t=list class=o>RMA List</a>");
	PrintAdminFooter();
}

void PrintWelcomeMenu()
{
	Response.Write("<table cellspacing=20>");
	Response.Write("<tr><td valign=bottom>");
	Response.Write("<a href=sra.aspx?t=reserve_number class=o title='Reserve a number for new RMA'><img width=90 src=../i/reserve_number.jpg><br>New RMA Number</a>");
	Response.Write("</td><td valign=bottom>");
	Response.Write("<a href=sra.aspx?t=list class=o title='Receive faulty items from customer'><img width=90 src=../i/receive_item.jpg><br>Receive Faulty Items</a>");
	Response.Write("</td><td valign=bottom>");
	Response.Write("<a href=sra.aspx?t=restock_replacement class=o title='Restock replacement item from supplier'><img width=120 src=../i/replacement.jpg><br>Receive Replacement</a>");
	Response.Write("</td><td valign=bottom>");
	Response.Write("<a href=sra.aspx?t=list class=o title='RMA list'><img width=100 src=../i/rma_list.jpg><br>RMA List / Search</a>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
}

void PrintSelectCustomerForm()
{
	Response.Write("<form name=f action=sra.aspx?t=reserve_number method=post>");
	Response.Write("<h5>Select Customer</h5>");
	Response.Write("<br>");
	Response.Write("<b>Customer ID or Search Keyword : </b><input type=text size=10 name=kw>");
	Response.Write("<input type=submit name=cmd value=GO class=b>");
	Response.Write("<br><br>");
	Response.Write("<input type=submit name=cmd value=Skip class=b>");
	Response.Write("</form>");
}

bool DoCustomerSearch()
{
	string kw = Request.Form["kw"];
	string sc = " SELECT id, company, trading_name, name, email ";
	sc += ", '<a href=sra.aspx?t=reserve_number&ci=' + CONVERT(varchar, id, 50) + ' class=o>Select </a>' AS 'Select' ";
	sc += " FROM card ";
	sc += " WHERE 1=1 ";
	if(TSIsDigit(kw))
		sc += " AND id = " + kw;
	else
	{
		string key = "'%" + EncodeQuote(kw) + "%'";
		sc += " AND (company LIKE " + key + " OR trading_name LIKE " + key;
		sc += " OR name LIKE " + key + " OR email LIKE " + key + ") ";
	}
	sc += " ORDER BY trading_name, company, name, email ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "customer");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(nRows == 1)
	{
		string customer = dst.Tables["customer"].Rows[0]["id"].ToString();
		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=reserve_number&ci=" + customer);
		Response.Write("\">");
		return true;
	}

	BindGrid();

	return true;
}

bool DoReserveNumber()
{
	string customer = Request.QueryString["ci"];
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO sra (customer, staff) ";
	sc += " VALUES(" + customer;
	sc += ", " + Session["card_id"].ToString();
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('sra') AS sra_id ";
	sc += " COMMIT ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "rn") == 1)
		{
			string id = dst.Tables["rn"].Rows[0]["sra_id"].ToString();
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=reserve_number&ci=" + customer + "&number=" + id);
			Response.Write("\">");
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["customer"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

void PrintSelectCustomerConfirmPage()
{
	string id = Request.QueryString["ci"];
	DataRow dr = GetCardData(id);
	if(dr == null)
		return;
	string trading_name = dr["trading_name"].ToString();
	string name = dr["name"].ToString();
	string email = dr["email"].ToString();

	Response.Write("<h5>Reserve RMA number for</h5>");
	Response.Write("<b>Company : <b>" + trading_name + "<b><br>");
	Response.Write("<b>Manager : <b>" + name + "<br>");
	Response.Write("<b>Email : </b>" + email + "<br>");
	Response.Write("<br>");
	Response.Write("<input type=button onclick=window.location=('sra.aspx?t=reserve_number&ci=" + id + "&confirmed=1') value=YES class=b>");
	Response.Write("<input type=button onclick=history.go(-2) value=NO class=b>");
}

void SayReserveOK()
{
	string id = Request.QueryString["ci"];
	DataRow dr = GetCardData(id);
	if(dr == null)
		return;
	string trading_name = dr["trading_name"].ToString();
	string name = dr["name"].ToString();
	string email = dr["email"].ToString();
	string sra_id = Request.QueryString["number"];

	Response.Write("<h1><font color=green>" + sra_id + "</font></h1>");
	Response.Write("<h4>Has reserved for " + trading_name + " " + name + "</h4>");
	Response.Write("<br><br>");
	Response.Write("<input type=button onclick=window.location=('sra.aspx') Value='Done' class=b>");
	Response.Write("<input type=button onclick=window.location=('sra.aspx?t=receive_item&id=" + sra_id + "') Value='Receive Item' class=b>");
}

bool AllItemFinished(string sra_id)
{
	if(dst.Tables["checkitemstatus"] != null)
		dst.Tables["checkitemstatus"].Clear();

	int n = 0;

	string sc = " SELECT i.* ";
	sc += " FROM sra_item i ";
	sc += " WHERE i.sra_id = " + sra_id;
	sc += " ORDER BY date_received ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		n = myAdapter.Fill(dst, "checkitemstatus");
		if(n == 0) //number reserved, no item received yet
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	bool bFinished = true;
	for(int i=0; i<n; i++)
	{
		DataRow dr = dst.Tables["checkitemstatus"].Rows[i];
		int nStatus = MyIntParse(dr["status"].ToString());
		if(nStatus <= 2)
		{
			bFinished = false;
			break;
		}
	}
	return bFinished;
}

bool PrintRAMList()
{
	string s = Request.QueryString["s"];
	string kw = "";
	string key = "";
	if(Request.Form["kw"] != null && Request.Form["kw"] != "")
	{
		kw = Request.Form["kw"];
		Session["SRA_SEARCH_KW"] = kw;
		key = EncodeQuote(kw);
	}
	

	string sc = " SELECT r.*, c1.name AS customer_name, c2.name AS staff_name ";
	sc += " FROM sra r JOIN card c1 ON c1.id = r.customer ";
	sc += " JOIN card c2 ON c2.id = r.staff ";
	if(kw != "")
		sc += " LEFT OUTER JOIN sra_item i ON i.sra_id = r.id ";
	sc += " WHERE 1=1 ";
	if(kw != "")
	{
		sc += " AND (";
		if(TSIsDigit(kw))
		{
			sc += " r.id = " + kw + " OR i.id = " + kw;
			sc += " OR i.code = " + kw;
			sc += " OR i.sn LIKE '%" + kw + "%' ";
		}
		else
		{
			sc += " c1.trading_name LIKE '%" + key + "%' ";
			sc += " OR i.name LIKE '%" + key + "%' ";
			sc += " OR i.sn LIKE '%" + key + "%' ";
			sc += " OR i.fault LIKE '%" + key + "%' ";
		}
		sc += ") ";
	}
	if(s != "all")
	{
		if(s == "reserved")
			sc += " AND r.item_received = 0 ";
		else if(s == "received")
			sc += " AND r.item_received = 1 ";
	}
	sc += " ORDER BY r.date_created ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "rma");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<form name=f action=sra.aspx?t=list method=post>");
	Response.Write("<tr><td colspan=3>");
	Response.Write("<b>Search RMA : </b><input type=text name=kw value='" + Session["SRA_SEARCH_KW"] + "'>");
	Response.Write("<input type=submit name=cmd value=GO class=b>");
	Response.Write("</td>");
	Response.Write("<td colspan=3 align=right>");
	Response.Write("<a href=sra.aspx?t=list&s=reserve><img src=r.gif border=0> Reserved</a> ");
	Response.Write("<a href=sra.aspx?t=list&s=received><img src=r.gif border=0> Received</a> ");
	Response.Write("<a href=sra.aspx?t=list&s=finished><img src=r.gif border=0> Finished</a> ");
	Response.Write("<a href=sra.aspx?t=list&s=all><img src=r.gif border=0> All</a> ");
	Response.Write("</td></tr>");
	Response.Write("</form>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th>Number</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Company</th>");
	Response.Write("<th>Staff</th>");
	Response.Write("<th>Status</th>");
	Response.Write("<th>Action</th>");
	Response.Write("</tr>");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = dst.Tables["rma"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = BuildParameterNoPageIndex();
	m_cPI.PageSize = 50;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	bool bAlterColor = false;
	int m = i;
	for(; i<nRows; i++)
	{
		DataRow dr = dst.Tables["rma"].Rows[i];
		string id = dr["id"].ToString();
		string customer = dr["customer_name"].ToString();
		string staff = dr["staff_name"].ToString();
		string date = DateTime.Parse(dr["date_created"].ToString()).ToString("dd-MM-yyy");
		string status = "Number Reserved";
		bool bReceived = MyBooleanParse(dr["item_received"].ToString());
		if(bReceived)
			status = "Item Received";
		if(AllItemFinished(id))
		{
			status = "Finished";
//			if(s != "finished" && s != "all")
//				continue;
		}
		else if(s == "finished")
		{
			continue;
		}

		if(s == "reserve")
		{
			if(status != "Number Reserved")
				continue;
		}
		else if(s == "received")
		{
			if(status != "Item Received")
				continue;
		}

		if(m >= nRows || m >= end)
			break;
		m++;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td align=center>" + id + "</td>");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + customer + "</td>");
		Response.Write("<td>" + staff + "</td>");
		Response.Write("<td>" + status + "</td>");
		Response.Write("<td align=right>");
		if(status == "Number Reserved")
		{
			Response.Write("<a href=sra.aspx?t=receive_item&id=" + id + " class=o>Receive Item</a> ");
			Response.Write("<a href=sra.aspx?id=" + id + "&a=del class=o>Delete</a> ");
		}
		else if(status == "Item Received")
		{
			Response.Write("<a href=sra.aspx?t=receive_item&id=" + id + " class=o>Receive More</a> ");
			Response.Write("<a href=sra.aspx?t=view&id=" + id + " class=o>Process</a> ");
		}
		else
		{
			Response.Write("<a href=sra.aspx?t=view&id=" + id + " class=o>View</a> ");
			Response.Write("<a href=sra.aspx?t=view&id=" + id + " class=o>Supplier Replacement</a> ");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write(sPageIndex);
	return true;
}

string BuildParameterNoPageIndex()
{
	string s = Request.QueryString["s"];
	string sq = "?t=list";
	if(s != "")
		sq += "&s=" + HttpUtility.UrlEncode(s);
	return sq;
}

bool PrintReceiveItemForm()
{
	string id = Request.QueryString["id"];
	string sn = "";
	string code = "";
	string name = "";
	string qty = "1";
	string invoice_number = "";
	if(Request.QueryString["scan"] == "1")
	{
		sn = Request.QueryString["sn"];
		if(Request.QueryString["code"] != null) //found
		{
			code = Request.QueryString["code"];
			name = Request.QueryString["name"];
			qty = Request.QueryString["qty"];
			invoice_number = Request.QueryString["inv"];
		}
	}

	Response.Write("<h4><font color=green>Receive Item for RMA# " + id + "</font></h4>");
	Response.Write("<form name=f action=sra.aspx?t=receive_item&id=" + id + " method=post>");
	Response.Write("<table width=450 cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>S/N</b></td><td><input type=text size=40 name=sn value='" + sn + "'><input type=submit name=cmd value=Scan class=b></td></tr>");
	Response.Write("<tr><td><b>Code</b></td><td><input type=text size=40 name=code value='" + code + "'></td></tr>");
	Response.Write("<tr><td><b>Name</b></td><td><input type=text size=40 name=name value='" + name + "'></td></tr>");
	Response.Write("<tr><td><b>Qty</b></td><td><input type=text size=40 name=qty value='" + qty + "'></td></tr>");
	Response.Write("<tr><td><b>Invoice#</b></td><td><input type=text size=40 name=invoice value='" + invoice_number + "'></td></tr>");
	Response.Write("<tr><td colspan=2><b>Faulty Description</td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=fault rows=5 cols=40></textarea></td></tr>");
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Receive class=b>");
	Response.Write("<input type=button value=Done onclick=window.location=('sra.aspx?t=view&id=" + id + "') class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("<script language=javascript");
	Response.Write(">document.f.sn.focus();</script");
	Response.Write(">");

	PrintRMAItemList(id);
	return true;
}

bool PrintRMAItemList(string sra_id)
{
	if(sra_id == null || sra_id == "")
		return false;

	string sc = " SELECT i.*, c.name ";
	sc += " FROM sra_item i JOIN code_relations c ON c.code = i.code ";
	sc += " WHERE i.sra_id = " + sra_id;
	sc += " ORDER BY date_received ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "items");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<font size=+1>Items in RAM# " + sra_id + "</font><br>");
	Response.Write("<table width=750 cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th>Code</th>");
	Response.Write("<th>Name</th>");
	Response.Write("<th>Qty</th>");
	Response.Write("<th>Invoice#</th>");
	Response.Write("<th>Fault</th>");
	Response.Write("<th>FaultyItem</th>");
	Response.Write("<th>Status</th>");
	Response.Write("<th>Action</th>");
	Response.Write("</tr>");
	
	bool bAlterColor = false;
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = dst.Tables["items"].Rows[i];
		string id = dr["id"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string fault = dr["fault"].ToString();
		if(fault.Length > 50)
			fault = fault.Substring(0, 50);
		string old_item_status = GetEnumValue("sra_old_item_status", dr["old_item_status"].ToString());
		string status = GetEnumValue("sra_item_status", dr["status"].ToString());

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td>" + qty + "</td>");
		Response.Write("<td><a href=invoice.aspx?id=" + invoice + " target=_blank class=o>" + invoice + "</td>");
		Response.Write("<td>" + fault + "</td>");
		Response.Write("<td>" + old_item_status + "</td>");
		Response.Write("<td>" + status + "</td>");
		Response.Write("<td>");
//		Response.Write("<a href=sra.aspx?t=receive_item&edit_kid=" + id + " class=o>Edit</a> ");
		Response.Write("<a href=sra.aspx?t=receive_item&delete_kid=" + id + "&id=" + sra_id + " class=o>Delete</a> ");
		Response.Write("<a href=sra.aspx?t=process&item_id=" + id + " class=o>Process</a> ");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

bool DoReceiveScanSN()
{
	string sra_id = Request.QueryString["id"];
	string sn = Request.Form["sn"];
	string sc = " SELECT s.code, s.invoice_number, c.name ";
	sc += " FROM sales_serial s JOIN code_relations c ON c.code=s.code ";
	sc += " WHERE s.sn = '" + EncodeQuote(sn) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows > 0)
	{
		DataRow dr = dst.Tables["sn"].Rows[0];
		string invoice_number = dr["invoice_number"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		if(name.Length > 100)
			name = name.Substring(0, 100);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=receive_item&scan=1&id=" + sra_id);
		Response.Write("&sn=" + HttpUtility.UrlEncode(sn));
		Response.Write("&inv=" + HttpUtility.UrlEncode(invoice_number) + "&code=" + code + "&name=" + HttpUtility.UrlEncode(name));
		Response.Write("\">");
		return true;
	}
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=receive_item&scan=1&id=" + sra_id);
	Response.Write("&sn=" + HttpUtility.UrlEncode(sn));
	Response.Write("\">");
	return false;
}

bool DoReceiveItem()
{
	string sra_id = Request.QueryString["id"];
	string sn = Request.Form["sn"];
	string code = Request.Form["code"];
	string name = Request.Form["name"];
	string qty = Request.Form["qty"];
	string invoice_number = Request.Form["invoice_number"];
	string fault = Request.Form["fault"];

	if(code == null || code == "")
	{
		Response.Write("<h4>Error, item code needed.</h4>");
		return false;
	}

	string sc = " INSERT INTO sra_item (sra_id, code, name, qty, sn, invoice_number, fault) ";
	sc += " VALUES( ";
	sc += sra_id;
	sc += ", " + code;
	sc += ", '" + EncodeQuote(name) + "' ";
	sc += ", " + qty + " ";
	sc += ", '" + sn + "' ";
	sc += ", '" + invoice_number + "' ";
	sc += ", '" + fault + "' ";
	sc += ") ";
	sc += " UPDATE sra SET item_received = 1 WHERE id = " + sra_id;
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
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=receive_item&id=" + sra_id);
	Response.Write("\">");
	return true;
}

bool PrintItemDetails(string item_id)
{
	if(item_id == null || item_id == "")
		return false;

	string sc = " SELECT * FROM sra_item WHERE id = " + item_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "item");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(nRows <= 0)
	{
		Response.Write("<br><h5>RMA item not found</h5>");
		return false;
	}

	DataRow dr = dst.Tables["item"].Rows[0];
	m_old_item_status = GetEnumValue("sra_old_item_status", dr["old_item_status"].ToString());
	string sra_id = dr["sra_id"].ToString();
	m_itemCode = dr["code"].ToString();
	m_itemName = dr["name"].ToString();
	m_replaceItemQty = dr["qty"].ToString();
	m_itemQty = m_replaceItemQty;
	string invoice_number = dr["invoice_number"].ToString();
	m_itemFault = dr["fault"].ToString();
	m_oldItemStatus = GetEnumValue("sra_old_item_status", dr["old_item_status"].ToString());
	m_itemStatus = GetEnumValue("sra_item_status", dr["status"].ToString());
	string date_sent_to_supplier = dr["date_sent_to_supplier"].ToString();
	if(date_sent_to_supplier != "")
		date_sent_to_supplier = DateTime.Parse(date_sent_to_supplier).ToString("dd-MM-yyyy");

	m_code_received_from_supplier = dr["code_received_from_supplier"].ToString();
	string received_date = dr["date_received_from_supplier"].ToString();
	if(received_date != "")
		m_date_received_from_supplier = DateTime.Parse(received_date).ToString("dd-MM-yyyy");

	Response.Write("<table>");
	Response.Write("<tr><td>RMA# : </td><td><a href=sra.aspx?t=view&id=" + sra_id + " target=_blank class=o><b>" + sra_id + "</b></a></td></tr>");
	Response.Write("<tr><td colspan=2>Code:<b>" + m_itemCode + "</b> &nbsp;&nbsp;&nbsp;&nbsp; ");
	Response.Write("Qty:<b>" + m_replaceItemQty + "</b>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Name : </td><td><b>" + m_itemName + "</b></td></tr>");
	Response.Write("<tr><td>Status : </td><td><b>" + m_itemStatus + "</b></td></tr>");
	Response.Write("<tr><td nowrap>Faulty Item : </td><td><b>" + m_oldItemStatus + "</b></td></tr>");
	if(m_oldItemStatus == "sent to supplier")
	{
		m_bSentToSupplier = true;
		Response.Write("<tr><td nowrap>Date Sent : </td><td><b>" + date_sent_to_supplier + "</b></td></tr>");
		if(m_code_received_from_supplier != "")
		{
			Response.Write("<tr><td nowrap>Supplier Replacement</td><td>");
			Response.Write("<b>Received</b>, Code:<b>" + m_code_received_from_supplier + "</b>, Date:<b>");
			Response.Write(m_date_received_from_supplier + "</b></td></tr>");
		}
	}
	Response.Write("<tr><td>Invoice# : </td><td><a href=invoice.aspx?id=" + invoice_number + " traget=_blank class=o><b>" + invoice_number + "</b></a></td></tr>");
	Response.Write("<tr><td colspan=2><b>Faulty Description</b></td></tr>");
	Response.Write("<tr><td colspan=2>" + m_itemFault + "</td></tr>");
	Response.Write("</table>");
	return true;
}

bool PrintItemProcessForm()
{
	string item_id = Request.QueryString["item_id"];
	PrintItemDetails(item_id);

	int nStatus = MyIntParse(GetEnumID("sra_item_status", m_itemStatus));
//	if(nStatus >= 3)//finished
//		return true;

	if(m_oldItemStatus == "received")
	{
		Response.Write("<h5><font color=red>First, what to do with the faulty item ?</font></h5>");

		string iw = "60";
		Response.Write("<table cellspacing=20>");
		Response.Write("<tr><td valign=bottom>");
		Response.Write("<a href=sra.aspx?t=trash&item_id=" + item_id + " class=o><img width=" + iw + " src=../i/trash.jpg><br><b>Trash It</b></a>");
		Response.Write("</td><td valign=bottom>");
		Response.Write("<a href=sra.aspx?t=repair&item_id=" + item_id + " class=o><img width=" + iw + " src=../i/repair.jpg><br><b>Repair It</b></a>");
		Response.Write("</td><td valign=bottom>");
		Response.Write("<a href=sra.aspx?t=resell&item_id=" + item_id + " class=o><img width=" + iw + " src=../i/resell.jpg><br><b>Sell As Used</b></a>");
		Response.Write("</td></tr>");

		Response.Write("<tr><td valign=bottom>");
		Response.Write("<a href=sra.aspx?t=sendback&item_id=" + item_id + " class=o><img width=" + iw + " src=../i/sendback.jpg><br><b>Return to customer</b></a>");
		Response.Write("</td><td valign=bottom>");
		Response.Write("<a href=sra.aspx?t=sendtosupplier&item_id=" + item_id + " class=o><img width=" + iw + " src=../i/madeinchina.jpg><br><b>Send to supplier</b></a>");
		Response.Write("</td><td valign=bottom>");
		Response.Write("<a href=sra.aspx?t=noidea&item_id=" + item_id + " class=o><img width=" + iw + " src=../i/noidea.jpg><br><b>No Idea</b></a>");
		Response.Write("</td></tr>");

		Response.Write("</table>");
		return true;
	}

	if(nStatus <= 2) //"faulty item received"
	{
		if(m_bSentToSupplier && m_code_received_from_supplier == "")
		{
			Response.Write("<h5><font color=red>Faulty Item has been sent to supplier,<br>did you received the replacement from supplier?</font></h5>");
			Response.Write("<input type=button value='Receive From Supplier' onclick=window.location=('sra.aspx?t=restock_replacement&item_id=" + item_id + "') class=b>");
			Response.Write("<h5><font color=red>Or you can replace it with a current stocked item</font></h5>");
			Response.Write("<input type=button value='Relacement' onclick=window.location=('sra.aspx?t=replace&item_id=" + item_id + "') class=b>");
		}
		else
		{
			Response.Write("<input type=button value='Relacement' onclick=window.location=('sra.aspx?t=replace&item_id=" + item_id + "') class=b>");
		}
	}
	else
	{
		if(m_bSentToSupplier)// && m_code_received_from_supplier == "")
		{
			Response.Write("<h5><font color=red>Faulty Item has been sent to supplier,<br>did you received the replacement from supplier?</font></h5>");
			Response.Write("<input type=button value='Receive From Supplier' onclick=window.location=('sra.aspx?t=restock_replacement&item_id=" + item_id + "') class=b>");
		}
	}
	return true;
}

bool DoTrashItem()
{
	string item_id = Request.QueryString["item_id"];

	if(item_id == null || item_id == "")
		return false;
	string sc = " UPDATE sra_item SET old_item_status = 2, old_item_status_date = GETDATE() WHERE id = " + item_id;
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
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=replace&item_id=" + item_id);
	Response.Write("\">");
	return true;
}

bool DoRepairItem()
{
	string item_id = Request.QueryString["item_id"];

	if(item_id == null || item_id == "")
		return false;
	string sc = " UPDATE sra_item SET old_item_status = 3, old_item_status_date = GETDATE() ";
	sc += ", status = 4 "; //reparid, end of item process
	sc += " WHERE id = " + item_id;
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
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=process&item_id=" + item_id);
	Response.Write("\">");
	return true;
}

bool DoNoIdeaItem()
{
	PrintRMAHeader();
	Response.Write("<h5><font color=blue>OK, leave it for now.</font></h5>");
	Response.Write("<input type=button onclick=history.go(-1) value=OK class=b>");
	return true;
}

bool PrintReplaceForm()
{
	string item_id = Request.QueryString["item_id"];
	string sn = "";
	string code = "";
	string name = "";
	string stock = "";
//	string qty = "1";
	if(Request.QueryString["sn"] != null)
		sn = Request.QueryString["sn"];
	if(Request.QueryString["code"] != null)
		code = Request.QueryString["code"];
	if(Request.QueryString["name"] != null)
		name = Request.QueryString["name"];
	if(Request.QueryString["stock"] != null)
		stock = Request.QueryString["stock"];
//	if(Request.QueryString["qty"] != null)
//		qty = Request.QueryString["qty"];

	PrintItemDetails(item_id);

	if(m_itemStatus == "replaced")
		return true;
	
	Response.Write("<form name=f action=sra.aspx?t=replace&item_id=" + item_id + " method=post>");
	Response.Write("<h5>Old one <font color=red>" + m_old_item_status);
	if(m_code_received_from_supplier != "")
		Response.Write("</font>, <font color=green>supplier replacement received");
	Response.Write("</font>, select item to replace</h5>");
	Response.Write("<table width=450 cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>S/N : </b></td><td><input type=text size=40 name=sn value='" + sn + "'>");
	Response.Write("<input type=submit name=cmd value=Scan class=b>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Code : </b></td><td><input type=text size=40 name=code value=" + code + "></td></tr>");
	Response.Write("<tr><td><b>Name : </b></td><td><input type=text size=40 name=name value='" + name + "'></td></tr>");
	Response.Write("<tr><td><b>Qty : </b></td><td><input type=text size=40 name=qty readonly=true value=" + m_replaceItemQty + "></td></tr>");
	Response.Write("<tr><td><b>Stock : </b></td><td><input type=text size=40 name=stock readonly=true value=" + stock + "></td></tr>");
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Replace class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table>");

	Response.Write("</form>");
	Response.Write("<script language=javascript");
	Response.Write(">document.f.sn.focus();</script");
	Response.Write(">");
	return true;
}

bool DoSearchReplacementSN()
{
	string item_id = Request.QueryString["item_id"];
	string sn = Request.Form["sn"];
	string sc = " SELECT s.product_code, s.supplier_code, c.name, ISNULL(sq.qty, 0) AS stock ";
	sc += " FROM stock s JOIN code_relations c ON c.code = s.product_code ";
	sc += " LEFT OUTER JOIN stock_qty sq ON sq.code = s.product_code ";
	sc += " WHERE s.sn = '" + EncodeQuote(sn) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(nRows <= 0)
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=replace&item_id=" + item_id);
		Response.Write("&sn=" + HttpUtility.UrlEncode(sn));
		Response.Write("\">");
		return false;
	}

	DataRow dr = dst.Tables["sn"].Rows[0];
	string code = dr["product_code"].ToString();
	string name = dr["name"].ToString();
	if(name.Length > 100)
		name = name.Substring(0, 100);
	string stock = dr["stock"].ToString();

	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=replace&item_id=" + item_id);
	Response.Write("&sn=" + HttpUtility.UrlEncode(sn) + "&code=" + code + "&name=" + HttpUtility.UrlEncode(name));
	Response.Write("&stock=" + stock);
	Response.Write("\">");
	return true;
}

bool CheckRMAAccount(string url)
{
	string item_id = Request.QueryString["item_id"];
	string rma_card = GetSiteSettings("rma_account_number", "");
	if(rma_card == "")
	{
		PrintRMAHeader();
		Response.Write("<form action=" + url + " method=post>");
		Response.Write("<font size=+1>Enter RMA Account#/Card ID : </font>");
		Response.Write("<input type=text name=rma_card><input type=submit name=cmd value=GO class=b>");
		Response.Write("<h5>* Create an account/card for RMA replacement if you haven't done so.</h5>");
		Response.Write("</form>");
		PrintRMAFooter();
		return false;
	}
	return true;
}

bool DoItemReplacement()
{
	string item_id = Request.QueryString["item_id"];
	string sn = "";
	string code = "";
	string name = "";
	string stock = "";
	string qty = "1";
	if(Request.Form["sn"] != null)
	{
		sn = Request.Form["sn"];
		code = Request.Form["code"];
		name = Request.Form["name"];
		stock =Request.Form["stock"];
		qty = Request.Form["qty"];
	}
	else
	{
		if(Request.QueryString["sn"] != null)
			sn = Request.QueryString["sn"];
		if(Request.QueryString["code"] != null)
			code = Request.QueryString["code"];
		if(Request.QueryString["name"] != null)
			name = Request.QueryString["name"];
		if(Request.QueryString["stock"] != null)
			stock = Request.QueryString["stock"];
		if(Request.QueryString["qty"] != null)
			qty = Request.QueryString["qty"];
	}

	m_rmaItemID = item_id;
	m_replaceItemCode = code;
	m_replaceItemName = name;
	m_replaceItemQty = qty;

	if(code == "")
		return false;

	string url = "sra.aspx?t=replace&item_id=" + item_id + "&sn=" + HttpUtility.UrlEncode(sn);
	url += "&code=" + code + "&name=" + HttpUtility.UrlEncode(name);

	if(!CheckRMAAccount(url))
		return false;

	string rma_card = GetSiteSettings("rma_account_number", "");
	m_rmaCardID = rma_card;
	DataRow dra = GetCardData(rma_card);
	if(dra == null)
	{
		Response.Write("<h4><font color=red>RMA account not found ! </font></h4>");
		SetSiteSettings("rma_account_number", "");
		CheckRMAAccount(url);
		return false;
	}

	if(!CreateOrder(m_branchID, m_rmaCardID, m_rmaItemID, "0", "", "1", "", "", "", "RMA", ref m_orderID))
		return false;

	if(!CreateInvoice(m_orderID))
		return false;

	string sc = " UPDATE sra_item SET status = 3, replacement_code = " + code;
	sc += ", replacement_sn = '" + sn + "' WHERE id = " + item_id;
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
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=replace&finished=1&item_id=" + item_id);
	Response.Write("\">");
	return true;
}


bool CreateOrder(string branch_id, string card_id, string po_number, string special_shipto, string shipto, 
				 string shipping_method, string pickup_time, string contact, string sales_id, string sales_note, 
				 ref string order_number)
{
	DataSet dsco = new DataSet();
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, card_id, po_number, freight) VALUES(0, " + card_id + ", '";
	sc += po_number + "', 0 ";
	sc += ") SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dsco, "id") == 1)
		{
			m_orderID = dsco.Tables["id"].Rows[0]["id"].ToString();
			m_orderNumber = m_orderID; //new order, same
			//assign ordernumber same as id
			sc = "UPDATE orders SET number=" + m_orderNumber + ", branch=" + branch_id + ", sales_note='" + sales_note + "' ";
			if(special_shipto == "1")
				sc += ", special_shipto=1, shipto='" + shipto + "' ";
			sc += ", contact='" + contact + "' ";
			if(sales_id != "")
				sc += ", sales=" + sales_id;
			sc += ", shipping_method=1";// + shipping_method;
			sc += ", pick_up_time='" + EncodeQuote(pickup_time) + "' ";
			sc += " WHERE id=" + order_number;
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
		else
		{
			Response.Write("<br><br><center><h3>Create Order failed, error getting new order number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(!WriteOrderItems(m_orderID))
		return false;
	return true;
}

bool WriteOrderItems(string order_id)
{
	string sc = " SELECT supplier, supplier_code, supplier_price FROM product WHERE code = " + m_replaceItemCode;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "replace_item");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
	{
		Response.Write("<h4>Item not found</h4>");
		return false;
	}

	DataRow dr = dst.Tables["replace_item"].Rows[0];
	m_replaceItemSupplier = dr["supplier"].ToString();
	m_replaceItemSupplierCode = dr["supplier_code"].ToString();
	m_replaceItemSupplierPrice = dr["supplier_price"].ToString();

	sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
	sc += ", commit_price) VALUES(" + order_id + ", " + m_replaceItemCode + ", ";
	sc += m_replaceItemQty + ", '" + m_replaceItemName + "', '" + m_replaceItemSupplier;
	sc += "', '" + m_replaceItemSupplierCode + "', " + m_replaceItemSupplierPrice;
	sc += ", 0) "; //RMA loss, salesPrice is zero

	sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + m_replaceItemCode;
	sc += " AND branch_id = " + m_branchID;
	sc += ")";
	sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
	sc += " VALUES (" + m_replaceItemCode + ", " + m_branchID + ", 0, " + m_replaceItemQty + ")"; 
	sc += " ELSE Update stock_qty SET ";
	sc += " allocated_stock = allocated_stock + " + m_replaceItemQty;
	sc += " WHERE code=" + m_replaceItemCode + " AND branch_id = " + m_branchID;

	sc += " UPDATE product SET allocated_stock = allocated_stock + " + m_replaceItemQty;
	sc += " WHERE code=" + m_replaceItemCode + " ";
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
	string sc = "SELECT * FROM orders WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "invoice");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error creating invoice, id=" + id + ", rows return:" + rows + "</h3>");
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
	string sales = dr["sales"].ToString();
	if(sales != "")
		sales = TSGetUserNameByID(sales);

	dFreight = MyDoubleParse(dst.Tables["invoice"].Rows[0]["freight"].ToString());

	sc = "SELECT * FROM order_item WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "item");
		if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + id + ", rows return:" + rows + "</h3>");
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
		dp = Math.Round(dp, 3);
		int qty = MyIntParse(dr["quantity"].ToString());
		dPrice += dp * qty;
		dPrice = Math.Round(dPrice, 3);
	}
	dTax = 0;
	dTotal = dPrice + dFreight + dTax;

	m_dInvoiceTotal = dTotal;
	
	dr = dst.Tables["invoice"].Rows[0];
	string special_shipto = "0";
	if(bool.Parse(dr["special_shipto"].ToString()))
		special_shipto = "1";
	
	string receipt_type = GetEnumID("receipt_type", "invoice");
//	if(m_bCreditReturn)
//		receipt_type = "6";//GetEnumID("receipt_type", "credit note");

	string sbSystem = "0";
	if(MyBooleanParse(dr["system"].ToString()))
		sbSystem = "1";

	sc = "SET DATEFORMAT dmy ";
	sc += " BEGIN TRANSACTION ";
	sc += "INSERT INTO invoice (type, card_id, price, tax, total, commit_date, special_shipto, shipto ";
	sc += ", freight, cust_ponumber, shipping_method, pick_up_time, sales, sales_note, paid)";
	sc += " VALUES(" + receipt_type + ", " + dr["card_id"].ToString() + ", " + dPrice;
	sc += ", " + dTax + ", " + dTotal + ", GETDATE(), ";
	sc += special_shipto + ", '" + EncodeQuote(dr["shipto"].ToString()) + "', " + dFreight + ", '" + po_number + "', ";
	sc += m_shippingMethod + ", '" + EncodeQuote(m_pickupTime) + "', '" + EncodeQuote(sales) + "', '";
	sc += EncodeQuote(dr["sales_note"].ToString()) + "', 1 ";
	sc += " )";
	sc += " SELECT IDENT_CURRENT('invoice') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "invoice_id") == 1)
		{
			m_invoiceNumber = dst.Tables["invoice_id"].Rows[0]["id"].ToString();
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
	sc = "UPDATE orders SET invoice_number=" + m_invoiceNumber + ", status=3 WHERE id=" + id; //status 3 = shipped
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
		sbSystem = "0";
		if(bool.Parse(dr["system"].ToString()))
			sbSystem = "1";

		string sKit = "0";
		if(MyBooleanParse(kit))
		{
			sKit = "1";
			bHasKit = true;
		}
		if(krid == "")
			krid = "null";

		sc = "INSERT INTO sales (invoice_number, code, name, quantity, commit_price, supplier, supplier_code, supplier_price, system, kit, krid)";
		sc += " VALUES(" + m_invoiceNumber + ", " + code + ", '" + name + "', " + quantity + ", " + commit_price + ", ";
		sc += "'" + supplier + "', '" + supplier_code + "', " + supplier_price + ", " + sbSystem + ", " + sKit + ", " + krid + ")";

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
		fifo_sales_update_cost(m_invoiceNumber, code, commit_price, m_branchID, nQty);
		//update stock qty
		fifo_updateStockQty(nQty, code, m_branchID);
		fifo_checkAC200Item(m_invoiceNumber, code, supplier_code, commit_price); //for unknow item
	}
	return true;
}

bool PrintResellForm()
{
	PrintRMAHeader();

	string item_id = Request.QueryString["item_id"];
	PrintItemDetails(item_id);

	Response.Write("<form name=f action=sra.aspx?t=resell&item_id=" + item_id + " method=post>");
	Response.Write("<input type=hidden name=code value=" + m_itemCode + ">");
	Response.Write("<input type=hidden name=qty value=" + m_itemQty + ">");

	Response.Write("<h5><font color=green>Restock Item for sell (as used)</font></h5>");

	Response.Write("<table width=450 cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>Selling Price : </b></td><td><input type=text name=price></td></tr>");
	Response.Write("<tr><td colspan=2><b>Description : </b></td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=name rows=5 cols=50>");
	Response.Write(m_itemFault + "\r\n" + m_itemName);
	Response.Write("</textarea></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Sell class=b></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");

	return true;
}

bool DoResellItem()
{
	PrintRMAHeader();

	string item_id = Request.QueryString["item_id"];
	m_supplierID = GetSiteSettings("rma_account_number", "");
	if(m_supplierID == "")
	{
		PrintRMAHeader();
		string url = "sra.aspx?t=resell&item_id=" + item_id;
		Response.Write("<form action=" + url + " method=post>");
		Response.Write("<font size=+1>Enter RMA Account#/Card ID : </font>");
		Response.Write("<input type=text name=rma_card><input type=submit name=cmd value=GO class=b>");
		Response.Write("<h5>* Create an account/card for RMA replacement if you haven't done so.</h5>");
		Response.Write("</form>");
		PrintRMAFooter();
		return false;
	}

	string code = Request.Form["code"];
	string name = Request.Form["name"];
	string qty = Request.Form["qty"];
	double dPrice = MyMoneyParse(Request.Form["price"]);

	if(!Restock())
		return false;
	if(!RecordPurchase(m_restockCode, "RMA", item_id, name, qty))
		return false;

	//update item status
	string sc = " UPDATE sra_item SET old_item_status = 4 WHERE id = " + item_id;
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

	Response.Write("<h4>Done, item restocked under UsedItem category. Please wait a second ...</h4>");
	Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=sra.aspx?t=process&item_id=" + item_id);
	Response.Write("\">");
	PrintRMAFooter();
	return true;
}

bool Restock()
{
	string item_id = Request.QueryString["item_id"];
	string old_code = Request.Form["code"];
	string name = EncodeQuote(Request.Form["name"]);
	string qty = Request.Form["qty"];
	double dPrice = MyMoneyParse(Request.Form["price"]);

	string code = GetNextCode().ToString();
	m_restockCode = code;
	string supplier = "RMA";
	string supplier_code = item_id;
	string supplier_id = supplier + supplier_code;
	double dSupplier_price = 0;

	string brand = "";
	string cat = "UsedItem";
	string s_cat = "UsedItem";
	string ss_cat = "";
	string skip = "0";
	string currency = "1";
	string exchange_rate = "1";
	string raw_supplier_price = "0";
	string freight = "0";

	string sc = "INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, code, name, brand, ";
	sc += " cat, s_cat, ss_cat, hot, skip, rate, currency, exchange_rate, foreign_supplier_price, nzd_freight ";
	sc += ", clearance, manual_cost_frd, manual_cost_nzd) VALUES('";
	sc += supplier_id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += dSupplier_price;
	sc += ", ";
	sc += code;
	sc += ", '";
	sc += name;
	sc += "', '";
	sc += brand;
	sc += "', '";
	sc += cat;
	sc += "', '";
	sc += s_cat; 
	sc += "', '";
	sc += ss_cat;
	sc += "', ";
	sc += "1";
	sc += ", ";
	sc += skip;
	sc += ", ";
	sc += 1;
	sc += ", '";
	sc += currency;
	sc += "', ";
	sc += exchange_rate;
	sc += ", ";
	sc += raw_supplier_price;
	sc += ", ";
	sc += freight;
	sc += ", 1 ";
	sc += ", " + dPrice;
	sc += ", " + dPrice;
	sc += ")";

	sc += " INSERT INTO product (supplier, supplier_code, code, name, brand, cat, s_cat, ss_cat, supplier_price ";
	sc += ", price, stock, eta, hot, price_age) ";
	sc += "VALUES('" + supplier + "', '" + supplier_code + "', " + code + ", '" + name;
	sc += "', '" + brand + "', '" + cat + "', '" + s_cat + "', '" + ss_cat;
	sc += "', " + dSupplier_price + ", " + dPrice + ", " + qty + ", '', 1 ";
	sc += ", GETDATE())";
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

bool RecordPurchase(string code, string supplier, string supplier_code, string name, string qty)
{
//	string item_id = Request.QueryString["item_id"];
//	string name = EncodeQuote(Request.Form["name"]);
//	string qty = Request.Form["qty"];
	double dPrice = 0;//this is purchase cost, set it to zero //MyMoneyParse(Request.Form["price"]);

//	string code = m_restockCode;
//	string supplier = "RMA";
//	string supplier_code = item_id;
	string supplier_id = supplier + supplier_code;

	string m_supplierID = GetSiteSettings("rma_account_number", "");

	//calc total
	double dTotalPrice = 0;
	double dAmount = 0;
	double dGstRate = 0;
	double dTotalTax = 0;

	//round
	double m_dFreight = 0;
	double m_dTotal = 0;

//	string sd = "GETDATE()";
	string s_allinstock = "1";		//"s_allinstock"=1 --- flag shows stock_qty table is updated;
	string m_quoteType = "4";
	string m_poNumber = "";
	
	string sc = " BEGIN TRANSACTION ";
	sc += " INSERT INTO po_number (dummy) VALUES(0) SELECT IDENT_CURRENT('po_number') AS id ";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "order_number") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting IDENT");
			return false;
		}
		m_poNumber = dst.Tables["order_number"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	m_orderID = "";
	string m_billToID = m_supplierID;
	string m_shipto = "";
	string m_note = "RMA Restock";
	string m_currency = "1";
	string m_exrate = "1";
	string m_gstrate = "0";

	sc = " BEGIN TRANSACTION ";
	sc += "INSERT INTO purchase (po_number, inv_number, branch_id, staff_id, type, supplier_id, ";
	sc += " buyer_id, total, tax, freight, total_amount, shipto, note, date_create, date_received, date_invoiced ";
	sc += ", tax_date, sales_order_id, all_in_stock ";
	sc += ", currency, exchange_rate, gst_rate, status, payment_status ";
	sc += ") VALUES(" + m_poNumber + ", '', " + m_branchID + ", " + Session["card_id"].ToString();
	sc += ", 4, " + m_supplierID + ", " + m_billToID + ", " + dTotalPrice; //payment_status 2:billed
	sc += ", " + dTotalTax + ", " + m_dFreight;
	sc += ", " + m_dTotal + ", '" + EncodeQuote(m_shipto) + "', '" + EncodeQuote(m_note) + "', GETDATE()";
	sc += ", GETDATE(), GETDATE(), GETDATE() ";
	sc += ", '" + m_orderID + "' ";
	sc += ", " + s_allinstock + ", " + m_currency + ", " + m_exrate + ", " + m_gstrate + ", 2 "; //status 2:received
	sc += ", 2"; //type 4:bill
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('purchase') AS id ";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "poid") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting poid IDENT");
			return false;
		}
		m_poID = dst.Tables["poid"].Rows[0]["id"].ToString();
		
//		UpdateOrderPurchaseStatus();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "INSERT INTO purchase_item (id, code, name, supplier_code, qty, price) ";
	sc += " VALUES(" + m_poID + ", '" + code + "', '" + name + "', '";
	sc += supplier_code + "', " + m_itemQty + ", " + dPrice + ")";
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

	//update stock 
	sc = " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + code;
	sc += " AND branch_id=" + m_branchID + ")";
	sc += " INSERT INTO stock_qty (qty, code, branch_id, supplier_price) ";
	sc += " VALUES ('";
	sc += qty + "', ";
	sc += code + ", ";
	sc += m_branchID + ", ";
	sc += dPrice;
	sc += ")";
	sc += " ELSE UPDATE stock_qty SET qty = qty + " + qty;
	sc += ", supplier_price = " + dPrice + " WHERE code = " + code + " AND branch_id=" + m_branchID;

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

void PrintRestockReplacementForm()
{
	string code = Request.Form["code"];
	string qty = Request.Form["qty"];
	string item_id = Request.QueryString["item_id"];
//	string sn = Request.Form["sn"];

	if(qty == null || qty == "")
		qty = "1";

	PrintRMAHeader();

	Response.Write("<h5>Restock Replacement Item</h5>");

//	Response.Write("<font size=+1 color=red><b>Please contact sales manager for restocking.</b></font><br>");
//

	Response.Write("<form name=f action=sra.aspx?t=restock_replacement&item_id=" + item_id + " method=post ");
//	Response.Write(" onsubmit=\"if(document.f.name.value=='' && document.f.cmd.value!='Check'){window.alert('Item code invalid, please click Check button to fill the Name Field.');return false;}\"");
	Response.Write(">");

	Response.Write("<table>");
	Response.Write("<tr><td><b>Code : </b></td><td>");
	Response.Write("<input type=text name=code value=" + code + ">");
	Response.Write("<input type=submit name=cmd value=Check class=b>");
	Response.Write("</td><tr>");

	Response.Write("<tr><td><b>Name : </b></td>");
	Response.Write("<td><input type=text name=name value='" + m_restockItemName + "' readonly=true style='background-color:#EEEEEE'></td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<b>Quantity : </b></td><td>");
	Response.Write("<input type=text name=qty value=" + qty + ">");
	Response.Write("</td></tr>");

/*	Response.Write("<tr><td colspan=2><b>Serial Numbers : (one per line)</b></td></tr>");
	Response.Write("<tr><td colspan=2><textarea rows=3 cols=50 name=sn>");
	Response.Write(sn);
	Response.Write("</textarea>");
	Response.Write("</td></tr>");
*/
	Response.Write("<tr><td colspan=2 align=center>");
	Response.Write("<input type=submit name=cmd value=Restock class=b ");
	if(m_restockItemName == "")
		Response.Write(" disabled");
	Response.Write(">");
	Response.Write("</td></tr>");

	Response.Write("</table>");

	Response.Write("</form>");
	Response.Write("<script language=javascript");
	Response.Write(">document.f.code.focus();</script");
	Response.Write(">");

}

bool DoCheckReplacementCode()
{
	string code = Request.Form["code"];
	if(code == "")
		return true;

	m_restockItemName = "";

	string sc = " SELECT name FROM code_relations WHERE code = " + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "checkreplacementcode") >= 1)
		{
			m_restockItemName = dst.Tables["checkreplacementcode"].Rows[0]["name"].ToString();
			if(m_restockItemName.Length > 100)
				m_restockItemName = m_restockItemName.Substring(0, 100);
			return true;
		}

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoRestockReplacement()
{
	string code = Request.Form["code"];
	string qty = Request.Form["qty"];
	string item_id = Request.QueryString["item_id"];
	if(item_id == null || item_id == "")
	{
		Response.Write("<br><br><center><h5>Error, item_id needed.</h5>");
		return false;
	}

	DataRow dr = null;
	if(!GetProduct(code, ref dr))
		return false;

	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string name = dr["name"].ToString();
	if(RecordPurchase(code, supplier, supplier_code, name, qty))
	{
		string sc = " UPDATE sra_item SET code_received_from_supplier = " + code;
		sc += ", date_received_from_supplier = GETDATE() ";
		sc += " WHERE id = " + item_id;
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
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=restock_replacement&finish=1&pid=" + m_poID + "&item_id=" + Request.QueryString["item_id"]);
		Response.Write("\">");
	}
	return false;
}

void PrintResotckFinishPage()
{
	string po_id = Request.QueryString["pid"];
	PrintRMAHeader();
	Response.Write("<h4>Done. Replacement item stocked.</h4>");

	Response.Write("<input type=button value='Scan Serial Number' onclick=window.location=('sra.aspx?t=receive_replacement_scan&n=" + po_id + "&item_id=" + Request.QueryString["item_id"] + "') class=b>");
//	Response.Write("<input type=button value='Scan Serial Number' onclick=window.location=('serial.aspx?n=" + po_id + "') class=b>");

	PrintRMAFooter();
}

bool DoSendToSupplier()
{
	string item_id = Request.QueryString["item_id"];
	if(item_id == "")
	{
		Response.Write("<h4>Error, item_id needed.");
		return false;
	}

	string sc = " UPDATE sra_item ";
	sc += " SET status = 2 "; //waiting for replacement
	sc += ", old_item_status = 6 "; //sent to supplier
	sc += ", date_sent_to_supplier = GETDATE() ";
	sc += " WHERE id = " + item_id;
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

bool DoDeleteReceivedItem()
{
	string sra_id = Request.QueryString["id"];
	string kid = Request.QueryString["delete_kid"];
	string sc = " DELETE FROM sra_item WHERE id = " + kid;
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

	//check if all item deleted, if true, reset status to reserved, otherwise return to process page
	bool bAllDeleted = false;
	sc = " SELECT count(*) FROM sra_item WHERE sra_id = " + sra_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "checkempty") <= 0)
			bAllDeleted = true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(bAllDeleted)
	{
		sc = " UPDATE sra SET item_received = 0 WHERE id = " + sra_id;
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
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=receive_item&id=" + sra_id);
	Response.Write("\">");
/*	else
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=sra.aspx?t=view&id=" + sra_id);
		Response.Write("\">");
	}
*/
	return true;
}
</script>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=100
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
</asp:DataGrid>
</form>

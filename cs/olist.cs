<!-- #include file="card_function.cs" -->
<!-- #include file="page_index.cs" -->
<script runat="server">

int m_cols = 6;

string m_branchID = "1";
string m_orderType = ""; //order
string m_action = "";
string m_id = "";
string m_kw = "";
bool m_bPIAList = false;
bool m_bSystem = false;
bool m_bListWaitingPayment = true;
bool m_bShowPaid = true; //show smilly face or not
bool m_bDraft = false;
bool m_bUnchecked = false;
bool m_bCanCheck = false;
bool m_bCTicket = false; // search for courier ticket;
DataSet ds = new DataSet();

int m_nOption = -1;
string m_sOption = "All";
int m_op1s = 0;
int m_op2s = 0;
int m_op3s = 0;
String[] m_op = new string[64];
string[] m_sql = new string[64];
//bool m_bEZNZAdmin = false;
bool m_bEnableCopyOrderFunction = false;
bool m_bAdminLogin = false;
string tableWidth = "97%";
bool m_bEnablePrintOverDueStatement = false;
bool m_bEnableCreditInvoice = false;
string m_sFrom = "";
string m_sTo = "";


void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
    if(Session["email"].ToString().IndexOf("eznz.com") >= 0)
		m_bEZNZAdmin = true;
	

	
//DEBUG("level", b_viewOrder);

	if(MyIntParse(Session[m_sCompanyName + "AccessLevel"].ToString()) == 10)
		m_bAdminLogin = true;
/*		
	if(int.Parse(Session["employee_access_level"].ToString()) != 10)
	{
//DEBUG(" access level", Session["employee_access_level"].ToString());
		if(!CheckAllowIPOK())
		{			
			Response.Write("<script language=javascript>window.alert('Your IP is not activated!!!'); window.close();</script");
			Response.Write(">");
			Session[m_sCompanyName + "loggedin"] = null;
			return;
		}		
	}

*/	m_bEnablePrintOverDueStatement = MyBooleanParse(GetSiteSettings("enable_print_overdue_statement", "0", true));
	m_bEnableCreditInvoice = MyBooleanParse(GetSiteSettings("ENABLE_FUNCTION_CREDIT_INVOICE", "0", true));

	if(Request.QueryString["t"] == "release") //release onhold orders
	{
		DoReleaseOrder();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?unchecked=1\">");
		return;
	}
	m_bEnableCopyOrderFunction = MyBooleanParse(GetSiteSettings("enable_copy_sales_order_function", "0", true));
	if(Request.QueryString["t"] == "transfer")
	{
		if(Request.Form["cmd"] == "Transfer")
		{
			DoTransferOrder();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?t=transfer&id=" + m_id + "\">");
			return;
		}
		else
		{
			PrintTransferForm();
		}
		return;
	}
	else if(Request.QueryString["t"] == "delete")
	{
		string inv = Request.QueryString["inv"];
		string order_id = Request.QueryString["order"];
				if(inv != null && inv != "")
		{
			if(DoDeleteInvoice(inv,order_id))
			{
				PrintAdminHeader();
				Response.Write("<br><center><h4>Invoice deleted, please wait a second</h4>");
				Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=olist.aspx?o=" + Request.Form["o"] + "\">");
			}
			return;
		}
	}

	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();

		Session["branch_id"] = m_branchID;
	}

	RememberLastPage();
	if(Request.QueryString["cticket"] == "on")
		m_bCTicket = true;

	if(g_bOrderOnlyVersion)
	{
		m_bListWaitingPayment = false;
		OrderOnly_Page_Load();
		return;
	}

	if(Request.QueryString["o"] != null && Request.QueryString["o"] != "")
		m_nOption = MyIntParse(Request.QueryString["o"]);

	int m = 0;

	m_sql[m] = " AND status = 1 AND sales IS NULL ";
	if(m_nOption == m)
	{
	}
	m_op[m++] = "All Online Orders";

	m_sql[m] = " AND status = 1 AND system = 0 AND sales IS NULL AND paid = 1 ";
	if(m_nOption == m)
	{
	}
	m_op[m++] = "Paid Credit Card"; //paid by credit card

	//m_sql[m] = " AND status = 1 AND system = 0 AND type = 2 AND sales IS NULL AND paid = 0 AND payment_type <> 3 "; //no waiting for credit card
	m_sql[m] = " AND status = 1 AND type = 2  AND unchecked = 1 AND invoice_number IS NULL";
	m_op[m++] = "Waiting Payment";

	m_sql[m] = " AND status = 1 AND sales IS NULL AND paid = 0 AND payment_type = 3 "; //payment type 3 is default (credit card), it will change to 2 or 5 (cheque or deposit)
	m_op[m++] = "Order Draft";

//	m_sql[m] = " AND status = 1 AND sales IS NULL AND system = 1 AND dealer_draft = 1 AND type = 1 ";
//	m_op[m++] = "System Draft";

//	m_sql[m] = " AND status = 1 AND sales IS NULL AND system = 1 AND dealer_draft = 0 AND type = 2 ";
//	m_op[m++] = "System Orders";
	m_op1s = m;

	m_sql[m] = " AND status = 1 AND sales IS NOT NULL ";
	m_op[m++] = "All Sales Orders";

	m_sql[m] = " AND status = 1 AND type=1 AND sales IS NOT NULL AND system=0 ";
	m_op[m++] = "Product Quotes";
	
	m_sql[m] = " AND status = 1 AND type=2 AND sales IS NOT NULL AND system=0 ";
	m_op[m++] = "Product Orders";
	
	//m_sql[m] = " AND unchecked=1 AND type=2 AND sales IS NOT NULL AND invoice_number IS NULL ";
	m_sql[m] = " AND status = 1 AND type = 2  AND unchecked = 1 AND invoice_number IS NULL";
	m_op[m++] = "Unapproved Order";
//	m_sql[m] = " AND status = 1 AND sales IS NOT NULL AND system=1 AND type=2 AND dealer_draft=0 ";
//	m_op[m++] = "System Orders";

//	m_sql[m] = " AND status = 1 AND sales IS NOT NULL AND system=1 AND type=1 AND dealer_draft=0 ";
//	m_op[m++] = "System Quotes";
	m_op2s = m;

	m_sql[m] = " AND unchecked=0 ";
	m_op[m++] = "All Checked Orders";

	m_sql[m] = " AND unchecked=0 AND status = 1 AND type=2 ";
	m_op[m++] = "Being Processed";

	m_sql[m] = " AND status = 2 ";
	m_op[m++] = "Invoiced Orders";

	m_sql[m] = " AND status = 3 ";
	m_op[m++] = "Shipped Orders";
	
	m_sql[m] = " AND status = 4 ";
	m_op[m++] = "Back Orders";

	m_sql[m] = " AND status = 5 ";
	m_op[m++] = "OnHold Orders";

	m_sql[m] = " AND status = 6 ";
	m_op[m++] = "Credit Notes";
	m_op3s = m;

	
	if(m_nOption >= 0)
		m_sOption = m_op[m_nOption];
	
	if(Request.QueryString["unchecked"] == "1")
	{
		m_bUnchecked = true;
		m_sOption = "Unchecked";
	}

	if(Request.QueryString["wp"] == "1")
		m_bPIAList = true;

	if(Request.QueryString["draft"] == "1")
		m_bDraft = true;

	if(Request.QueryString["ot"] != null && Request.QueryString["ot"] != "")
		m_orderType = Request.QueryString["ot"];

	if(Request.QueryString["system"] == "1")
	{
		m_bSystem = true;
	}
	else if(Request.QueryString["system"] == "0")
	{
		m_bSystem = false;
	}

	m_action = Request.QueryString["a"];
	m_id = Request.QueryString["id"];

	m_bCanCheck = CheckAccess(Session[m_sCompanyName + "AccessLevel"].ToString(), "checkorder.aspx");

	if(m_action == "u") //unlock
	{
		DoUnlock(m_id);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx" + BuildParameter());
		Response.Write("\">");
		return;
	}
	else if(Request.QueryString["kw"] != null && Request.QueryString["kw"] != "")
	{
		m_kw = Request.QueryString["kw"];
		Session["order_list_search_kw"] = m_kw;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["id"] != null) //view perticular order
	{
		if(!GetOneOrder())
			return;
	}
	if(Request.QueryString["from"] != "" && Request.QueryString["from"] !=null)
		m_sFrom = Request.QueryString["from"].ToString();
	if(Request.QueryString["to"] != "" && Request.QueryString["to"] !=null)
		m_sTo = Request.QueryString["to"].ToString();
	if(GetOrders())
		BindGrid();
	PrintAdminFooter();
}

bool DoUnlock(string id)
{
	string sc = " UPDATE orders SET locked_by=null, time_locked=null ";
	sc += " WHERE id=" + id;
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

Boolean GetOrders()
{
	string sc = "SET DATEFORMAT dmy SELECT * FROM (";
	sc += "SELECT ";
	//sc += "SELECT TOP 50";
	sc += " ISNULL(";
	sc += " i.total ";
	sc += " , ISNULL((SELECT (SUM((commit_price * (1-(discount_percent/100))) * quantity) + o.freight) * (1 + o.customer_gst) FROM order_item WHERE id = o.id ), 0))";
	sc += " AS stotal, c2.name AS agent_name, b.name AS branch_name ";
	sc += ", ISNULL(i.commit_date, o.record_date) AS record_date1";
	sc += ", o.*, i.paid AS ipaid, c.name, c.company, c.trading_name, c.email, c1.name AS lockedby ";
	sc += ", DATEDIFF(day, o.time_locked, GETDATE()) AS days_locked ";
	sc += ", c.credit_term, c.balance, d.name AS sales_name ";
	sc += ", (SELECT SUM((commit_price * (1-(discount_percent/100))) * quantity) FROM order_item WHERE id=o.id) AS amount ";
	sc += " FROM orders o LEFT OUTER JOIN card c ON c.id=o.card_id LEFT OUTER JOIN card d ON d.id=o.sales ";
	sc += " LEFT OUTER JOIN branch b ON b.id = o.branch ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number = o.invoice_number ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id=o.locked_by ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id=o.agent ";
	if(m_bCTicket)
		sc += " LEFT OUTER JOIN invoice_freight inf ON inf.invoice_number = i.invoice_number OR inf.invoice_number = o.invoice_number ";
	if(m_kw != "")
	{
		if(m_bCTicket)
			sc += " WHERE inf.ticket LIKE '%" + EncodeQuote(m_kw) + "%' ";
		else
		{
			sc += " WHERE (o.po_number LIKE '%" + EncodeQuote(m_kw) + "%' OR o.number LIKE '%" + EncodeQuote(m_kw) + "%' ";
			sc += " OR o.invoice_number LIKE '%" + EncodeQuote(m_kw) + "%' ";
			sc += " OR c.name LIKE '%" + EncodeQuote(m_kw) + "%' OR c.trading_name LIKE '%" + EncodeQuote(m_kw) + "%'";
			sc += " OR o.delivery_number LIKE '%" + EncodeQuote(m_kw) + "%')";
		}
	}
	else if(m_bUnchecked)
	{
		sc += " WHERE o.unchecked=0 AND o.status = 1 AND o.type = 2 ";
	}
	sc += " ) DERIVEDTBL WHERE 1=1 ";
	if(Session["branch_support"] != null)
	{
		if(m_kw == "") //search all branch for orders
			sc += " AND branch = " + m_branchID + " ";
	}
	if(m_nOption >= 0)
		sc += m_sql[m_nOption];
	if(Request.QueryString["from"] != "" && Request.QueryString["from"] !=null)
		sc += " AND record_date1 >= '" + m_sFrom + "'";
	if(Request.QueryString["to"] != "" && Request.QueryString["to"] !=null)
		sc +=" AND record_date1<= '" + m_sTo + " 23:59"+"' ";
	//sc += " ORDER BY  id DESC";
	sc += " ORDER BY number DESC, id DESC";
//DEBUG("sc +", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "order");
		return true;
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
	Response.Write("<form name=f action=olist.aspx method=get>");
/*	Response.Write("<br><center><font size=+1><b>Order List - </b><font color=red><b>");
	if(m_kw != "")
		Response.Write("Search");
	else
		Response.Write(m_sOption);
	Response.Write("</b></font></font>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" &nbsp; <b>Branch:</b>");
		PrintBranchNameOptions(m_branchID);
		Response.Write("<input type=submit name=cmd value=GO  "+ Session["button_style"] +" >");
	}
	*/
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Order List - </b><font color=red><b>");
	if(m_kw != "")
		Response.Write("Search");
	else
		Response.Write(m_sOption);
	Response.Write("</b></font></font>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" &nbsp; <b>Branch:</b>");
		PrintBranchNameOptions(m_branchID);
		Response.Write("<input type=submit name=cmd value=GO  "+ Session["button_style"] +" >");
	}
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	

//	Response.Write("<br>");
/*	if(m_bSystem)
	{
		Response.Write("Quotation List - <font color=red>");
		if(m_orderType == "1")
			Response.Write("Quote");
		else if(m_orderType == "2")
			Response.Write("Order");
		else
			Response.Write("ALL");
	} 
	else
	{
		Response.Write("Order List - ");
		Response.Write("<font color=red>");
		if(m_bUnchecked)
			Response.Write("Unchecked");
		else if(m_bPIAList)
			Response.Write("Waiting For Payment");
		else if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
			Response.Write(GetEnumValue("order_item_status", Request.QueryString["t"]));
		else
			Response.Write("ALL");
	}
	Response.Write("</font></h3>");
*/
	Response.Write("<table align=center width='"+ tableWidth +"' ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write(">");
	Response.Write("<tr><td><br></td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<b>SEARCH TICKET ONLY</b> <input type=checkbox name=cticket onclick=\"document.f.kw.value=''; document.f.kw.focus();\" ");
	if(Request.QueryString["cticket"] == "on")
		Response.Write(" checked ");
	Response.Write(">");
	Response.Write("<input type=text size=10 name=kw autocomplete=off value='" + Session["order_list_search_kw"] + "'>");
	//Response.Write("<input type=text size=10 name=kw autocomplete=off value=\"" + Request.QueryString["kw"] + "\">");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Search Order'>");
	Response.Write(" &nbsp;<b>From:</b><input type=text name=from size=10 onClick=\"displayDatePicker(this.name);\" >&nbsp;<b>To:</B>");
	Response.Write("<input type=text name=to size=10 onClick=\"displayDatePicker(this.name);\"> <input type=button name=cmd value='Search By Date' ");
	Response.Write(" onclick=\"window.location=('?");
	if(Request.QueryString["o"] != "" && Request.QueryString["o"] != null)
		Response.Write("o="+ Request.QueryString["o"]);
	Response.Write("&from='+document.all.from.value+'");
	Response.Write("&to='+document.all.to.value)\" ");
	Response.Write(">");
	Response.Write("</td>");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.kw.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<td align=right>");
//	Response.Write("<img src=r.gif> <a href=olist.aspx?system=1&draft=1 class=o>CustomerDraft</a>&nbsp&nbsp&nbsp&nbsp;");

	int i = 0;
   
	//online orders
	
	Response.Write("<a href=olist.aspx?o=" + i + "><img src=r.gif border=0></a> ");
	Response.Write("<select onchange=\"window.location=('olist.aspx?o=' + this.options[this.selectedIndex].value)\">");
	
	for(i=0; i<m_op1s; i++)
	{
		Response.Write("<option value=" + i);
		if(m_nOption == i)
			Response.Write(" selected");
		Response.Write(">" + m_op[i] + "</option>");
		
	}
	Response.Write("</select>&nbsp&nbsp&nbsp&nbsp;");
//}
	//sales orders
	Response.Write("<a href=olist.aspx?o=" + i + "><img src=r.gif border=0></a> ");
	Response.Write("<select onchange=\"window.location=('olist.aspx?o=' + this.options[this.selectedIndex].value)\">");
	for(; i<m_op2s; i++)
	{
		Response.Write("<option value=" + i);
		if(m_nOption == i)
			Response.Write(" selected");
		Response.Write(">" + m_op[i] + "</option>");
	}
	Response.Write("</select>&nbsp&nbsp&nbsp&nbsp;");

	//others
	Response.Write("<a href=olist.aspx?o=" + i + "><img src=r.gif border=0></a> ");
	Response.Write("<select onchange=\"window.location=('olist.aspx?o=' + this.options[this.selectedIndex].value)\">");
	for(; i<m_op3s; i++)
	{
		Response.Write("<option value=" + i);
		if(m_nOption == i)
			Response.Write(" selected");
		Response.Write(">" + m_op[i] + "</option>");
	}
	Response.Write("</select>&nbsp&nbsp&nbsp&nbsp;");

/*
	Response.Write("<img src=r.gif> <a href=olist.aspx?system=1 class=o>SysQuote</a>&nbsp&nbsp&nbsp&nbsp;");
//	if(m_bListWaitingPayment)
//		Response.Write("<img src=r.gif> <a href=olist.aspx?t=1&wp=1 class=o>Waiting Payment</a>&nbsp&nbsp&nbsp&nbsp;");
	if(m_bCanCheck)
		Response.Write("<img src=r.gif> <a href=olist.aspx?t=1&unchecked=1 class=o>Unchecked</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=1 class=o>Being Processed</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=2 class=o>Invoiced</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=3 class=o>Shipped</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=4 class=o>BackOrder</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=5 class=o>OnHold</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=6 class=o>Credit</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx? class=o>All</a> ");
*/
	Response.Write("</td></tr>");
	Response.Write("</form>");

	Response.Write("<tr><td colspan=2>");
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(Session["branch_support"] != null)
		Response.Write("<th>Branch</th>");
	Response.Write("<th>Sales</th>");
	Response.Write("<th>Date</th>");
//	Response.Write("<th>Agent</th>");
	Response.Write("<th>Customer</th>");
	Response.Write("<th>Term</th>");
	Response.Write("<th nowrap>PO Number</th>");
	Response.Write("<th>Invoice#</th>");
	Response.Write("<th>Total</th>");
	Response.Write("<th>Shipping</th>");
	Response.Write("<th>Order#</th>");
	Response.Write("<th nowrap>Locked</th>");
	Response.Write("<th>Action</th>");
	Response.Write("</tr>");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["order"].Rows.Count;
	m_cPI.TotalRows = rows;
	bool bQAdded = false;
	m_cPI.URI = BuildParameterNoPageIndex();
	m_cPI.PageSize = 30;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	string bgc = GetSiteSettings("table_row_bgcolor", "#EEEEEE");
	string sbgc = GetSiteSettings("sys_quote_bgcolor", "#AAFFFF");
	string sbgcl = GetSiteSettings("sys_quote_bgcolor_light", "#DDFFFF");
	bool bCanChangeInvoice = CheckAccess(Session[m_sCompanyName + "AccessLevel"].ToString(), "invchg.aspx");
	
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["order"].Rows[i];
		string branch_id = dr["branch"].ToString();
		string branch = dr["branch_name"].ToString();
		string dn = dr["delivery_number"].ToString();
		string type = dr["type"].ToString();
		string id = dr["id"].ToString();
		bool bPaid = false;
		bool bDraft = bool.Parse(dr["dealer_draft"].ToString());
		bool bUnchecked = bool.Parse(dr["unchecked"].ToString());
		string paid = dr["paid"].ToString();
		if(paid != "")
			bPaid = bool.Parse(paid);
		bool biPaid = false;
		string ipaid = dr["ipaid"].ToString();
		if(ipaid != "")
			biPaid = bool.Parse(ipaid);
		string email = dr["email"].ToString();
		string purchase_id = dr["purchase_id"].ToString();
		string card_id = dr["card_id"].ToString();
		string sales = dr["sales_name"].ToString();
		int p = sales.IndexOf(" ");
		if(p >= 0)
			sales = sales.Substring(0, p);
		string invoice_number = dr["invoice_number"].ToString();
		string order_number = dr["number"].ToString();
		bool IsSalesequaltoOrders = false;
		if(invoice_number != "")
			IsSalesequaltoOrders = IsSalesQualToOrders(invoice_number, order_number);
		string po_number = dr["po_number"].ToString();
		string part = dr["part"].ToString();
		string credit_term = dr["credit_term"].ToString();
		string dealer_customer_name = dr["dealer_customer_name"].ToString();
		if(credit_term != "")
			credit_term = GetEnumValue("credit_terms", dr["credit_term"].ToString());
		if(credit_term == "cash only")
			credit_term = "<font color=red><b>Cash Only</b></font>";
		else if(credit_term == "pay in advance")
			credit_term = "<b>P.I.A.</b>";
		else if(credit_term.IndexOf("20th") >= 0)
			credit_term = "20th";
		else 
			credit_term = credit_term.ToUpper();
		string payment_type = dr["payment_type"].ToString();
		if(payment_type != "")
			payment_type = GetEnumValue("payment_method", payment_type);

		bool bSystem = bool.Parse(dr["system"].ToString());

		string company = dr["trading_name"].ToString();
		if(company == "")
			company = dr["trading_name"].ToString();
		if(company == "")
			company = dr["name"].ToString();

		string companyfull = company;
		if(company.Length >= 10)
			company = company.Substring(0, 10);
		
		string agent = dr["agent_name"].ToString();
		double dstotal = MyDoubleParse(dr["stotal"].ToString());
		
		string date = DateTime.Parse(dr["record_date1"].ToString()).ToString("dd-MM-yyyy");
		string shipping_method = GetEnumValue("shipping_method", dr["shipping_method"].ToString());
		shipping_method = shipping_method.ToUpper();
		string p_time = dr["pick_up_time"].ToString();
		string sm_title = shipping_method; //shipping_method title
		if(shipping_method == "PICK UP")
		{
			shipping_method = "<font color=green>Pick Up ";
			string p_stime = p_time;
			shipping_method += p_stime + "</font>";
			sm_title = "Pick Up : " + p_time;
		}
		else if(shipping_method == "SUB60")
			shipping_method = "<font color=red>" + shipping_method + "</font>";
		else if(shipping_method == "2 DAYS COURIER")
			shipping_method = "<font color=blule>2 Days Courier</font>";
		else if(shipping_method == "OVERNIGHT COURIER")
			shipping_method = "<font color=purple>Overnight Courier</font>";
		else if(shipping_method == "LOCAL(AKL) COURIER")
			shipping_method = "Local(AKL) Courier";

		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		string statusPlus = status;
		if(status == "Shipped")
			statusPlus += " " + DateTime.Parse(dr["date_shipped"].ToString()).ToString("dd-MM-yyyy HH:mm");
		
		string locker_id = dr["locked_by"].ToString();
		string lockedby = dr["lockedby"].ToString();
		string lockedby1 = lockedby;
		int days_locked = 0;
		int auto_unlock = MyIntParse(GetSiteSettings("auto_unlock_order_after_days", "2"));
		if(lockedby1 != "")	
		{
			string time_locked = dr["time_locked"].ToString();
			if(time_locked != "")
				time_locked = DateTime.Parse(time_locked).ToString("HH:mm");
			lockedby1 = lockedby + "  " + time_locked;
			days_locked = MyIntParse(dr["days_locked"].ToString());
			if(days_locked >= auto_unlock)
				lockedby1 += " (" + days_locked.ToString() + " days)";
		}
		if(days_locked >= auto_unlock)
		{
			DoUnlock(id);
		}
		p = lockedby.IndexOf(" ");
		if(p > 0)
			lockedby = "<font color=red><b>" + lockedby.Substring(0, p) + "</b></font>";
		else
			lockedby = "<font color=red><b>" + lockedby + "</b></font>";
		string sStatus = dr["status"].ToString();
		string sSales = dr["sales"].ToString();
		string sUnchecked = dr["unchecked"].ToString();
		string sPaid = dr["paid"].ToString();
		string sPType = dr["payment_type"].ToString();
		string sSystem =dr["system"].ToString();
		string sInvoice = dr["invoice_number"].ToString();
		string sCustomerGSTRate = dr["customer_gst"].ToString();
		
		
			

        string sStatusIden = "#FFCC00";
		string sStatusIden1 = "#FFCC00";
		if(sStatus == "1" && sSales == "" && sPaid == "False" && sPType =="3" )
			sStatusIden ="#000000" ;// Order Draft;
	    else if(sStatus == "1" && type=="1" && sSales != "-1" && sSystem == "False")
			sStatusIden ="#66FFFF" ;// Quotation;
		else if(sStatus == "1" && type=="2"  && sSales != "-1"  && sSystem == "False" )
			sStatusIden ="#99FF99" ;// Product Order;
		else if(sStatus == "1" && type == "2" && sUnchecked == "True" && sInvoice == "")
			sStatusIden ="#FF6600" ;// Unproved Order;
		else if(sStatus == "1" && type == "2" && sUnchecked == "False")
			sStatusIden ="#99FF00" ;// Being Processed;
		else if(sStatus == "2")
			sStatusIden ="#6699FF"; // Being Invioced;
		else if(sStatus == "3")
			sStatusIden ="#FF9933" ;// Being Order shipped;
		else if(sStatus == "4")
			sStatusIden ="#CC00CC";// Back Order;
		else if(sStatus == "5")
			sStatusIden ="#FFFF99" ;// On Hold;
		else if(sStatus == "6")
			sStatusIden ="#CCCCCC" ;// Credit note;
		if(sCustomerGSTRate != "0" )
			sStatusIden1 = "#000000"; // NZ GST RATE
		else 
			sStatusIden1 = "#999999";
		bAlterColor = !bAlterColor;
		Response.Write("<tr");
		if(bSystem)
		{
			if(m_bSystem)
			{
				if(bAlterColor)
					Response.Write(" bgcolor=" + sbgc);
				else 
					Response.Write(" bgcolor=" + sbgcl);
			}
			else 
			{
				Response.Write(" bgcolor=" + sbgc);
			}
		}
		else
		{
			if(bAlterColor)
				Response.Write(" bgcolor=" + bgc);
		}
		
		bAlterColor = !bAlterColor;

		Response.Write(">");
	
		if(Session["branch_support"] != null)
			Response.Write("<td>" + branch + "</td>");
		Response.Write("<td>" + sales + "</td>");
		
		Response.Write("<td align=center nowrap>" + date + "</td>");
//		Response.Write("<td>" + agent + "</td>");
		Response.Write("<td nowrap title='" + companyfull + "'><a title='View Customer Details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + dr["card_id"].ToString() + "', '',");
		Response.Write("'width=350, height=400, scrollbars=1, resizable=1'); viewcard_window.focus()\" class=o><font color=Green>");
		Response.Write("" + company + "</a></td>");
		Response.Write("<td nowrap>" + credit_term + "</td>");
		
		

		Response.Write("<td nowrap");
		if(bDraft)
			Response.Write(" title='Customer Name : " + dealer_customer_name + "'");
		Response.Write(">" + po_number);
		if(dn != "")
		Response.Write(" DN: "+ dn);
		Response.Write("</td>");
		Response.Write("<td nowrap>");

		if(invoice_number != "")
		{
			if(m_bEnablePrintOverDueStatement)
			{
				//if(credit_term == "C.O.D" || credit_term == "C.O. D") //only for cash on delivery
					Response.Write("<input type=checkbox name=OverDue title='Click to Print Invoice With OverDue Statement' onclick=\"window.open('invoice.aspx?id=" + invoice_number + "&r=" + DateTime.Now.ToOADate()+"&stat='+ this.checked);\">");
			}

			//if(Session[m_sCompanyName + "AccessLevel"].ToString() == "10")
			{
			if(bCanChangeInvoice)
				Response.Write(" <a href='invchg.aspx?i="+ invoice_number +"' title='change invoice' class=o><font color=orange><b>UE</b></font></a> ");
			}		
			
			Response.Write("<a href=invoice.aspx?id=" + invoice_number + "&r=" + DateTime.Now.ToOADate()+" ");						
			Response.Write(" class=o target=_blank title='click to view invoice'>" + invoice_number + "</a>");			
		
			Response.Write("<input type=button value=P title='Print Receipt' onclick=window.open('qpos.aspx?t=pr&i=" + invoice_number + "')  "+ Session["button_style"] +" >");
			Response.Write(" <a href=invoice.aspx?id=" + invoice_number + "&confirm=1&email=" + HttpUtility.UrlEncode(email));
			Response.Write(" class=o target=_blank title='Email Invoice to customer'>M</a>");

			Response.Write(" <a href=pack.aspx?i=" + invoice_number + "&r=" + DateTime.Now.ToOADate());
			Response.Write(" target=_blank title='click to view packing slip' class=o>PKS</a>");
			Response.Write(" <a href='pack.aspx?i=" + invoice_number + "&confirm=1&email=" + HttpUtility.UrlEncode(email) +"' ");
			Response.Write("target=_blank title='Email Packing Slip to customer' class=o>M</a>");
			
            if(m_bAdminLogin)
			{
				Response.Write("<input type=button value=X title='Delete Invoice' ");
				Response.Write("onclick=\"if(window.confirm('Are you sure to delete this invoice?'))");
				Response.Write("window.location=('olist.aspx?t=delete&inv=" + invoice_number + "&order=" + order_number);
				Response.Write("&o=" + Request.QueryString["o"] + "');\"  "+ Session["button_style"] +" >");
			}
			Response.Write(" <a href=invoicenogst.aspx?id=" + invoice_number + "&r=" + DateTime.Now.ToOADate());
			Response.Write(" target=_blank title='click to view invoice without GST' class=o>NoGST</a>");
			/*Response.Write(" <a href=as.aspx?id=" + invoice_number + "&r=" + DateTime.Now.ToOADate());
			Response.Write(" target=_blank title='click to view East Tamaki invoice' class=o>AS</a>");
			Response.Write(" <a href=sd.aspx?id=" + invoice_number + "&r=" + DateTime.Now.ToOADate());
			Response.Write(" target=_blank title='click to view Symond St invoice' class=o>SD</a>")*/;

			if(m_bShowPaid)
			{			
				if(biPaid)
					Response.Write("&nbsp;<span title=Paid style=cursor:pointer><font color=green>v</font></span>");//<img src=/i/nod.gif title=paid>");
				else
					Response.Write("&nbsp;<span title=Unpaid style=cursor:pointer><font color=red>x</font></span>");//<img src=/i/no.gif title=unpaid>");			
			}
			if(IsSalesequaltoOrders)
				Response.Write("&nbsp;<span style=cursor:pointer><font color=blue>=</font></span>");
			else
				Response.Write("&nbsp;<span style=cursor:pointer><font color=red>!=</font></span>");
		}
		Response.Write("</td>");
				Response.Write("<td   style=color:"+sStatusIden1+"><B>" + dstotal.ToString("c") + "</B></td>");
		Response.Write("<td nowrap align=center title=\"" + sm_title + "\">" + shipping_method + "</td>");
		Response.Write("<td nowrap><a href=olist.aspx" + BuildParameter());
		if(Request.QueryString["id"] != id)
			Response.Write("&id=" + id);
		Response.Write(" class=o>" + order_number);
		if(part != "0")
			Response.Write("." + part);
		Response.Write("</a>");
		Response.Write(" <a href=invoice.aspx?id=" + id + "&t=order class=o title='Print Order' target=_blank>V</a>");
		Response.Write(" <a href=invoice.aspx?id=" + id + "&t=order&confirm=1&email=" + HttpUtility.UrlEncode(email));
		Response.Write(" class=o target=_blank title='Email Order to customer'>M</a>");
		Response.Write(" <a href=pkf.aspx?i=" + id + "&r=" + DateTime.Now.ToOADate());
		Response.Write(" target=_blank title='click to view/print pickup form' class=o><b><font color=green>PKF</font></b></a>");

		Response.Write("</td>");

		bool bLocked = false;
		if(locker_id != null && locker_id != "" && locker_id != Session["card_id"].ToString())
			bLocked = true;

		//locked by
		Response.Write("<td nowrap title='" + lockedby1 + "'>" + lockedby + "</td>");
		string sAction = "";

		if((status == "Invoiced" )&& !bDraft)
		{
			if(!bLocked)
			{
				sAction += "<a href=inputsn.aspx?inv=" + invoice_number + " class=o title='S/N Enter'>S/N</a> ";
				sAction += "<a href=esales.aspx?i=" + invoice_number + " class=o title=Shipping><b>SHIP</b></a> ";
				sAction += "<a href=inv_note.aspx?i=" + invoice_number + "&r="+ DateTime.Now.ToOADate() +" class=o title='View/Update Sales Note'><b><font color=purple>NOTES</b></a> ";
			}
			if(purchase_id == "")
			{
				if(!bLocked)
					sAction += "<a href=purchase.aspx?t=new&oid=" + id + " class=o title='Make Purchase'><font color=green><b>PUR</b></font></a> ";
			}
			else
				sAction += "<a href=purchase.aspx?n=" + purchase_id + " class=o title='View Purchase'><font color=orange>VP</font></a> ";
			if(m_bEnableCreditInvoice)
				sAction += "<a href=pos.aspx?t=credit&id=" + id + " class=o title='Make Credit'><font color=red><b>CR</b></font></a> ";
		}
		else if((status == "Shipped" || status.IndexOf("Returned") >= 0) && !bDraft)
		{		
			
		
			if(!bLocked)
			{
				sAction += "<a href=inputsn.aspx?inv=" + invoice_number + " class=o title='S/N Enter'>S/N</a> ";
				sAction += "<a href=inv_note.aspx?i=" + invoice_number + "&r="+ DateTime.Now.ToOADate() +" class=o title='View/Update Sales Note'><b><font color=purple>NOTES</b></a> ";
			}		
			if(purchase_id != "")
				sAction += "<a href=purchase.aspx?n=" + purchase_id + " class=o title='View Purchase'><font color=orange>VP</font></a> ";
			if(m_bEnableCreditInvoice)
			    sAction += "<a href=pos.aspx?t=credit&id=" + id + " class=o title='Make Credit'><font color=red><b>CR</b></font></a> ";
			
		}
		else if(!bLocked && !bDraft && type != "1")
		{
			if(status == "On Hold")
			{
				sAction += "<a href=olist.aspx" + BuildParameter() + "&t=release&id=" + id + " class=o title=Release><font color=red><b>Release</b></font></a> ";
			}
			if(!m_bPIAList && Request.QueryString["t"] != null && Request.QueryString["t"] != "")
			{
				if(!bUnchecked)
					sAction += "<a href=eorder.aspx?id=" + id + "&part=" + part + "&r=" + DateTime.Now.ToOADate() + " class=o title=Process><font color=red><b>PRO</b></font></a> ";
				if(purchase_id == "")
				{
					sAction += "<a href=purchase.aspx?t=new&oid=" + id + " class=o title='Make Purchase'><font color=green><b>PUR</b></font></a> ";
					sAction += "<a href=inv_note.aspx?i=" + invoice_number + "&r="+ DateTime.Now.ToOADate() +" class=o title='View/Update Sales Note'><b><font color=purple>NOTES</b></a> ";
				}
				else
					sAction += "<a href=purchase.aspx?n=" + purchase_id + " class=o title='View Purchase'><font color=orange>VP</font></a> ";
			}
			else
			{
/*				if(g_bRetailVersion)
				{
					sAction += "<a href=custpay.aspx?id=" + card_id + " class=o title='Deposit Money As Credit First ";
					if(payment_type != "")
						sAction += ", Waiting for " + payment_type.ToUpper();
					sAction += "'>Pay";
					if(payment_type != "")
						sAction += "(" + payment_type.Substring(0, 2).ToUpper() + ")";
					sAction += "</a> ";
				}
*/
//////////////////////////////////
//temperary
				if(!bUnchecked)
					sAction += "<a href=eorder.aspx?id=" + id + "&part=" + part + "&r=" + DateTime.Now.ToOADate() + " class=o title=Process><font color=red><b>PRO</b></font></a> ";
				if(purchase_id == "")
					sAction += "<a href=purchase.aspx?t=new&oid=" + id + " class=o title='Make Purchase'><font color=green><b>PUR</b></font></a> ";
				else
					sAction += "<a href=purchase.aspx?n=" + purchase_id + " class=o title='View Purchase'><font color=orange>VP</font></a> ";
////////////////////////////////

			}
		}

		if(!m_bDraft && m_bCanCheck)
		{	
			if(m_nOption <= m_op2s && bUnchecked)
			{
			  
				//sAction = ""; //no process
				if(type == "2" && invoice_number == "")
					sAction = "<a href=checkorder.aspx?pass=1&id=" + id + " class=o>Pass</a> &nbsp; ";
					
			}
			if(m_nOption == 11 && !bUnchecked)
			{
				sAction += "<a href=checkorder.aspx?pass=0&id=" + id + " class=o title='Take It Back'>TB</a> &nbsp; ";
			}
		}
		if(m_bEnableCopyOrderFunction)
			sAction += "<a href=cpso.aspx?oid=" + id + " class=o title='Copy Order'><font color=orange><b>CP</font></a> &nbsp; ";
		if(locker_id != "")
		{
			if(locker_id == Session["card_id"].ToString())
			{
				sAction += "<a href=olist.aspx" + BuildParameter() + "&a=u&id=" + id;
				sAction += " class=o title=Unlock>Unlock</a> ";
			}
			//else if(MyIntParse(Session[m_sCompanyName + "AccessLevel"].ToString()) >= 7) //manager and admin can unlock
			else if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
			{
				sAction += "<a href=olist.aspx" + BuildParameter() + "&a=u&id=" + id;
				sAction += " class=o title=Unlock><font color=red>Unlock(admin)</font></a> ";
			}
		}
		if(SecurityCheck("manager", false) 
			&& (status == "Being Processed" || status == "Back Ordered" || status == "On Hold")
			&& Session["access_level"] != GetEnumID("access_level", "stockman")
			&& !bLocked && !bDraft)
		{
			string uri = "pos.aspx?id=" + id;
			if(bSystem)
				uri = "q.aspx?n=" + id;
	  //  int e_olist = int.Parse(GetSiteSettings("edit_order").ToString());
		//int e_olistlevel = int.Parse(GetSiteSettings("allow_to_edit_invoiced_order").ToString());
		//if(e_olist == 1 ){
		//if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) >=e_olistlevel)
		//{
			sAction += "<a href=" + uri + " class=o title=Edit><font color=black>E</font></a> ";
			//sAction += "<a href=stktran.aspx?o=" + id + " class=o title='Stock Stransfer'><font color=blue ><b>TR</b></font></a> ";
		//}
		//}
		}
		if(m_nOption == 0) //all online orders
			sAction = "";
		if(Session["branch_support"] != null)
		{
			if(branch_id != m_branchID)
			{
				sAction = "";
				if(SecurityCheck("manager", false) 
					&& (status == "Being Processed" || status == "Back Ordered" || status == "On Hold")
					&& Session["access_level"] != GetEnumID("access_level", "stockman")
					&& !bLocked && !bDraft)
				{
					sAction = "<a href=olist.aspx?t=transfer&id=" + id + " class=o>Transfer</a> ";
				}
			}
		}
//DEBUG("SC" , sAction);
		Response.Write("<td nowrap>" + sAction + "</td>");
		Response.Write("</tr>");

		if(Request.QueryString["id"] != null && id == Request.QueryString["id"])
		{
			Response.Write("<tr><td colspan=15 align=center>");
			PrintOneOrderStatus();
			Response.Write("<br>&nbsp;</td></tr>");
		}
	}
	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
//	Response.Write(PrintPageIndex());
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
}

bool GetOneOrder()
{
	string sc = "SELECT b.name AS branch_name, o.branch, o.number, o.part, o.contact, o.po_number ";
	if(g_bOrderOnlyVersion)
		sc += ", o.status_orderonly AS status ";
	else
		sc += ", o.status ";
	sc += ", o.freight ";
	sc += ", o.invoice_number, o.date_shipped, o.sales_note, o.record_date, c.gst_rate, i.*, k.kit_id ";
	sc += " FROM orders o JOIN order_item i ON i.id=o.id ";
	sc += " LEFT OUTER JOIN order_kit k ON k.id = o.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = o.card_id ";
	sc += " LEFT OUTER JOIN branch b ON b.id = o.branch ";
	sc += " WHERE o.id=" + Request.QueryString["id"];
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "one");
		return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintOneOrderStatus()
{
//	Response.Write("<br><center><h3>Order Information</h3>");
	Response.Write("<table width=90% valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<th>Code</th>");
	Response.Write("<th>M_PN</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>Status</th>");
	Response.Write("<th>Amount</th>");
	Response.Write("</tr>");
/*
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["one"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
*/
	int rows = ds.Tables["one"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

//	double dGstRate = 0.125;
	double dGstRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;				//Modified by NEO
	string gst_rate = ds.Tables["one"].Rows[0]["gst_rate"].ToString();
	if(gst_rate != "")
		dGstRate = MyDoubleParse(gst_rate);
	double dTotal = 0;
	double dTax = 0;
	bool bAlterColor = false;
//	for(; i < rows && i < end; i++)
	string m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);
	DataRow dr = null;
	for(int i=0; i < rows; i++)
	{
		dr = ds.Tables["one"].Rows[i];
		string code = dr["code"].ToString();
		string m_pn = dr["supplier_code"].ToString();
		string name = dr["item_name"].ToString();
		string quantity = dr["quantity"].ToString();
		string price = dr["commit_price"].ToString();
		double dAmount = MyDoubleParse(price) * MyIntParse(quantity);
		bool bKit = MyBooleanParse(dr["kit"].ToString());
		string kit_id = dr["kit_id"].ToString();
//		string  = dr[""].ToString();
//		string invoice_number = dr["invoice_number"].ToString();
//		string order_number = dr["number"].ToString();
//		string po_number = dr["po_number"].ToString();
//		string part = dr["part"].ToString();
//		string contact = dr["contact"].ToString();
//		string date = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		if(status == "Shipped")
			status += " " + DateTime.Parse(dr["date_shipped"].ToString()).ToString("dd-MM-yyyy HH:mm");
		
		Response.Write("<tr");
		if(bKit)
			Response.Write(" bgcolor=aliceblue");
		else if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;

		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + m_pn + "</td>");
		Response.Write("<td><a href=p.aspx?" + code + " target=_blank>");
		if(bKit)
			Response.Write("<font color=red><i>(" + m_sKitTerm + " #" + kit_id + ")</i></font> ");
		Response.Write(name + "</a></td>");
		Response.Write("<td>" + quantity + "</td>");
		Response.Write("<td nowrap>" + status + "</td>");
		Response.Write("<td nowrap align=right>" + dAmount.ToString("c") + "</td>");
		Response.Write("</tr>");
		dTotal += dAmount;
	}
	double dFreight = 0;
	if(dr["freight"].ToString() != "")
	{
		dFreight = MyDoubleParse(dr["freight"].ToString());
		dTotal += dFreight;
	}
	dTax = dTotal * dGstRate;

	Response.Write("<tr><td colspan=6 align=right>");

	Response.Write("<table cellspacing=1 cellpadding=1 border=0 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr><td align=right><b>Sub Total : </b></td><td align=right>" + dTotal.ToString("c") + "</td></tr>");
//	Response.Write("<tr><td align=right><b>Tax : </b></td><td align=right>" + dTax.ToString("c") + "</td></tr>");
	Response.Write("<tr><td align=right><b>Total Amount : </b></td><td align=right>" + (dTotal).ToString("c") + " + " + dTax.ToString("c") +"");
//	if(dFreight > 0)
//		Response.Write(" + "+ dFreight.ToString("c"));
	Response.Write("= "+ (dTotal + dTax).ToString("c") +"</td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr>");

	string note = dr["sales_note"].ToString();
	if(note != "")
	{
		note = note.Replace("\r\n", "\r\n<br>");
		Response.Write("<tr><td colspan=6><table cellspacing=0 cellpadding=0>");
		Response.Write("<tr><td valign=top><b>Comment : &nbsp&nbsp;</b></td>");
		Response.Write("<td colspan=5><font color=red>" + note + "</font></td></tr>");
		Response.Write("</table></td></tr>");
	}
	Response.Write("</table>");
}

string BuildParameter()
{
	string s = "?r=";
	if(m_kw != "")
		s += "&kw=" + HttpUtility.UrlEncode(m_kw);
	if(Request.QueryString["o"] != null && Request.QueryString["o"] != "")
		s += "&o=" + Request.QueryString["o"];
//	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
//		s += "&t=" + Request.QueryString["t"];
//	if(Request.QueryString["wp"] != null)
//		s += "&wp=" + Request.QueryString["wp"];
//	if(m_bSystem)
//		s += "&system=1";
//	if(m_bDraft)
//		s += "&draft=1";
//	if(Request.QueryString["unchecked"] != null)
//		s += "&unchecked=" + Request.QueryString["unchecked"];
	if(Request.QueryString["p"] != null)
		s += "&p=" + Request.QueryString["p"];
	if(Request.QueryString["spb"] != null)
		s += "&spb=" + Request.QueryString["spb"];	
	if(m_bUnchecked)
		s += "&unchecked=1";
	return s;
}

string BuildParameterNoPageIndex()
{
	string s = "?r=";
	if(m_kw != "")
		s += "&kw=" + HttpUtility.UrlEncode(m_kw);
	if(Request.QueryString["o"] != null && Request.QueryString["o"] != "")
		s += "&o=" + Request.QueryString["o"];
	if(m_bUnchecked)
		s += "&unchecked=1";
	if(Request.QueryString["from"] != "" && Request.QueryString["from"] !=null)
		s += "&from="+Request.QueryString["from"].ToString();
	if(Request.QueryString["to"] != "" && Request.QueryString["to"] !=null)
		s += "&to="+Request.QueryString["to"].ToString();
	return s;
}

//order only functions
void OrderOnly_Page_Load()
{
	if(Request.QueryString["wp"] == "1")
		m_bPIAList = true;

	if(Request.QueryString["ot"] != null && Request.QueryString["ot"] != "")
		m_orderType = Request.QueryString["ot"];

	if(Request.QueryString["system"] == "1")
	{
		m_bSystem = true;
	}
	else if(Request.QueryString["system"] == "0")
	{
		m_bSystem = false;
	}

	m_action = Request.QueryString["a"];
	m_id = Request.QueryString["id"];

	if(m_action == "process")
	{
		OrderOnlyDoProcessOrder();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?t=");
		Response.Write(Request.QueryString["t"] + "\">");
		return;
	}
	else if(m_action == "delete")
	{
		OrderOnlyDoDeleteOrder();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?t=");
		Response.Write(Request.QueryString["t"] + "\">");
		return;
	}
	else if(m_action == "u") //unlock
	{
		DoUnlock(m_id);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?t=");
		Response.Write(Request.QueryString["t"] + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.QueryString["kw"] != null && Request.QueryString["kw"] != "")
	{
		m_kw = Request.QueryString["kw"];
		Session["order_list_search_kw"] = m_kw;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["id"] != null) //view perticular order
	{
		if(!GetOneOrder())
			return;
	}
	if(OrderOnlyGetOrders())
		OrderOnlyBindGrid();
	PrintAdminFooter();
}

bool OrderOnlyDoProcessOrder()
{
	string sc = " UPDATE orders SET status_orderonly = 2 WHERE id = " + m_id;
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

bool OrderOnlyDoDeleteOrder()
{
	string sc = " UPDATE orders SET status_orderonly = 0 WHERE id = " + m_id;
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

Boolean OrderOnlyGetOrders()
{
	bool bWhereAdded = false;
	string sc = " SELECT * FROM (";
	sc += "SELECT o.*, i.paid AS ipaid, c.name, c.company, c.trading_name, c.email, c1.name AS lockedby ";
	sc += ", DATEDIFF(day, o.time_locked, GETDATE()) AS days_locked ";
	sc += ", c.credit_term, c.balance, d.name AS sales_name ";
	sc += ", (SELECT SUM(commit_price * quantity) FROM order_item WHERE id=o.id) AS amount ";
	sc += " FROM orders o LEFT OUTER JOIN card c ON c.id=o.card_id LEFT OUTER JOIN card d ON d.id=o.sales ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number = o.invoice_number ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id=o.locked_by ";
	if(m_kw != "")
	{
		sc += " WHERE (o.po_number LIKE '%" + m_kw + "%' OR o.number LIKE '%" + m_kw + "%' OR o.invoice_number LIKE '%" + m_kw + "%' ";
		sc += " OR c.name LIKE '%" + m_kw + "%' OR c.trading_name LIKE '%" + m_kw + "%') ";
	}
	else if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		sc += " WHERE o.status_orderonly=" + Request.QueryString["t"];
	sc += " ) DERIVEDTBL WHERE 1=1 ";
	if(Session["branch_support"] != null)
		sc += " AND branch=" + m_branchID + " ";
	if((m_kw == null || m_kw == "") && Request.QueryString["t"] == "1" && m_bListWaitingPayment)
	{
		if(!m_bPIAList)
			sc += " AND (credit_term NOT IN (1, 2) OR (credit_term IN (1, 2) AND amount + balance <= 0)) ";
		else
			sc += " AND ((credit_term IN (1, 2) AND amount + balance > 0 ) OR credit_term is null OR amount is null ) ";
	}
	if(m_bSystem)
	{
		sc += " AND ";
		sc += " system = 1 ";
		if(m_orderType != "")
			sc += " AND type=" + m_orderType;
	}
	else if(m_kw == null || m_kw == "")
	{
		sc += " AND ";
		sc += " type=2 "; //display Order only, ignore quote
	}
	sc += " ORDER BY number DESC, id DESC";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "order");
		return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void OrderOnlyBindGrid()
{
	Response.Write("<form name=f action=olist.aspx method=get>");
	Response.Write("<br><center><font size=+1><b>");
	if(m_bSystem)
	{
		Response.Write("Quotation List - <font color=red>");
		if(m_orderType == "1")
			Response.Write("Quote");
		else if(m_orderType == "2")
			Response.Write("Order");
		else
			Response.Write("ALL");
	} 
	else
	{
		Response.Write("</b>Order List - ");
		Response.Write("<font color=red>");
		if(Request.QueryString["t"] == "8")
			Response.Write("Done");
		else if(m_bPIAList)
			Response.Write("Waiting For Payment");
		else if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
			Response.Write(GetEnumValue("order_item_status", Request.QueryString["t"]));
		else
			Response.Write("ALL");
	}
	Response.Write("</font></font>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" &nbsp; <b>Branch:</b>");
		PrintBranchNameOptions();
		Response.Write("<input type=submit name=cmd value=GO  "+ Session["button_style"] +" >");
	}
	Response.Write("<br><br>");

	Response.Write("<table width=100%>");
	
	Response.Write("<tr><td>");
	Response.Write("<input type=text size=10 name=kw autocomplete=off value=\"" + Request.QueryString["kw"] + "\">");
	//Response.Write("<input type=text size=10 name=kw autocomplete=off value='" + Session["order_list_search_kw"] + "'>");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Search Order'>");
	Response.Write("</td>");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.kw.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<td align=right>");
	if(g_bEnableQuotation)
		Response.Write("<img src=r.gif> <a href=olist.aspx?system=1 class=o>SysQuote</a>&nbsp&nbsp&nbsp&nbsp;");
	if(m_bListWaitingPayment)
		Response.Write("<img src=r.gif> <a href=olist.aspx?t=1&wp=1 class=o>Waiting Payment</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=1 class=o>Being Processed</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=2 class=o>Invoiced</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx?t=0 class=o>Deleted</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=olist.aspx? class=o>All</a> ");

	Response.Write("</td></tr>");
	Response.Write("</form>");

	Response.Write("<tr><td colspan=2>");
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Sales</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Customer</th>");
	Response.Write("<th>Term</th>");
	Response.Write("<th>Customer#</th>");
	Response.Write("<th>Shipping</th>");
	Response.Write("<th>Our#</th>");
	Response.Write("<th>Action</th>");
	Response.Write("</tr>");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["order"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?";
	m_cPI.PageSize = 20;
	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		m_cPI.URI += "t=" + Request.QueryString["t"];
	if(Request.QueryString["wp"] != null)
		m_cPI.URI += "&wp=" + Request.QueryString["wp"];
	if(m_bSystem)
		m_cPI.URI += "&system=1";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	string bgc = GetSiteSettings("table_row_bgcolor", "#EEEEEE");
	string sbgc = GetSiteSettings("sys_quote_bgcolor", "#AAFFFF");
	string sbgcl = GetSiteSettings("sys_quote_bgcolor_light", "#DDFFFF");
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["order"].Rows[i];
		string id = dr["id"].ToString();
		bool bPaid = false;
		bool biPaid = false;
		string ipaid = dr["ipaid"].ToString();
		if(ipaid != "")
			biPaid = bool.Parse(ipaid);
		string email = dr["email"].ToString();
		string card_id = dr["card_id"].ToString();
		string sales = dr["sales_name"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string order_number = dr["number"].ToString();
		string po_number = dr["po_number"].ToString();
		string part = dr["part"].ToString();
		string credit_term = dr["credit_term"].ToString();


		if(credit_term != "")
			credit_term = GetEnumValue("credit_terms", dr["credit_term"].ToString());
		if(credit_term == "cash only")
			credit_term = "<font color=red><b>Cash Only</b></font>";
		else if(credit_term == "pay in advance")
			credit_term = "<b>P.I.A.</b>";
		else if(credit_term.IndexOf("20th") >= 0)
			credit_term = "20th";
		else 
			credit_term = credit_term.ToUpper();
		string company = dr["trading_name"].ToString();
		if(company == "")
			company = dr["trading_name"].ToString();
		if(company == "")
			company = dr["name"].ToString();
		
		string date = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy");
		string shipping_method = GetEnumValue("shipping_method", dr["shipping_method"].ToString());
		shipping_method = shipping_method.ToUpper();
		string p_time = dr["pick_up_time"].ToString();
		string sm_title = shipping_method; //shipping_method title
		if(shipping_method == "PICK UP")
		{
			shipping_method = "<font color=green>Pick Up ";
			string p_stime = p_time;
			shipping_method += p_stime + "</font>";
			sm_title = "Pick Up : " + p_time;
		}
		else if(shipping_method == "SUB60")
			shipping_method = "<font color=red>" + shipping_method + "</font>";
		else if(shipping_method == "2 DAYS COURIER")
			shipping_method = "<font color=blule>2 Days Courier</font>";
		else if(shipping_method == "OVERNIGHT COURIER")
			shipping_method = "<font color=purple>Overnight Courier</font>";
		else if(shipping_method == "LOCAL(AKL) COURIER")
			shipping_method = "Local(AKL) Courier";

		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		string statusPlus = status;
		if(status == "Shipped")
			statusPlus += " " + DateTime.Parse(dr["date_shipped"].ToString()).ToString("dd-MM-yyyy HH:mm");
		
		string locker_id = dr["locked_by"].ToString();
		string lockedby = dr["lockedby"].ToString();
		int days_locked = 0;
		int auto_unlock = MyIntParse(GetSiteSettings("auto_unlock_order_after_days", "2"));
		if(lockedby != "")	
		{
			string time_locked = dr["time_locked"].ToString();
			if(time_locked != "")
				time_locked = DateTime.Parse(time_locked).ToString("HH:mm");
			lockedby = "<font color=red><b>" + lockedby + "</b></font> " + time_locked;
			days_locked = MyIntParse(dr["days_locked"].ToString());
			if(days_locked >= auto_unlock)
				lockedby += " (" + days_locked.ToString() + " days)";
		}
		if(days_locked >= auto_unlock)
		{
			DoUnlock(id);
		}
       
			
		Response.Write("<tr");
		{
			if(bAlterColor)
				Response.Write(" bgcolor=" + bgc);
		}
		bAlterColor = !bAlterColor;

		Response.Write(">");
		Response.Write("<td>" + sales + "</td>");
		Response.Write("<td align=center>" + date + "</td>");
		Response.Write("<td>" + company + "</td>");
		Response.Write("<td>" + credit_term + "</td>");
		Response.Write("<td>" + po_number + "</td>");
		Response.Write("<td align=center title=\"" + sm_title + "\">" + shipping_method + "</td>");
		Response.Write("<td><a href=olist.aspx?");
		if(Request.QueryString["id"] == id)
			Response.Write("r=" + DateTime.Now.ToOADate());
		else
			Response.Write("id=" + id);
		if(m_bPIAList)
			Response.Write("&wp=1");
		if(Request.QueryString["t"] != null)
			Response.Write("&t=" + Request.QueryString["t"]);
		if(Request.QueryString["ot"] != null)
			Response.Write("&ot=" + Request.QueryString["ot"]);
		if(Request.QueryString["p"] != null)
			Response.Write("&p=" + Request.QueryString["p"]);
		if(Request.QueryString["spb"] != null)
			Response.Write("&spb=" + Request.QueryString["spb"]);
		if(m_kw != "")
			Response.Write("&kw=" + HttpUtility.UrlEncode(m_kw));
		Response.Write("&system=" + Request.QueryString["system"]);
		Response.Write(" class=o>" + order_number);
		if(part != "0")
			Response.Write("." + part);
		Response.Write("</a>");
		Response.Write(" <a href=invoice.aspx?t=order&id=" + id + " class=o title='Print Order' target=_blank>V</a>");
		Response.Write("</td>");

		bool bLocked = false;
		if(locker_id != "" && locker_id != Session["card_id"].ToString())
			bLocked = true;

		//locked by
		string sAction = "";
		if(Request.QueryString["t"] == "1")
			sAction = "<input type=button onclick=window.location='olist.aspx?a=process&id=" + id + "&t=" + Request.QueryString["t"] + "' value='Process' " + Session["button_style"] + ">";
		if(Request.QueryString["t"] != "1" && Request.QueryString["t"] != "0")
			sAction += "<input type=button onclick=window.location='olist.aspx?a=delete&id=" + id + "&t=" + Request.QueryString["t"] + "' value='Delete' " + Session["button_style"] + ">";
		Response.Write("<td nowrap>" + sAction + "</td>");
		Response.Write("</tr>");

		if(Request.QueryString["id"] != null && id == Request.QueryString["id"])
		{
			Response.Write("<tr><td colspan=15 align=center>");
			PrintOneOrderStatus();
			Response.Write("<br>&nbsp;</td></tr>");
		}
	}
	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
}

bool PrintTransferForm()
{
	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		if(!GetOneOrder())
			return false;
	}
	else
	{
		Response.Write("<center><h3>Error, Order ID needed</h3>");
		return false;
	}

	DataRow dr = ds.Tables["one"].Rows[0];
	string currentBranchName = dr["branch_name"].ToString();
	string currentOrderDate = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy");
	string currentSalesNote = dr["sales_note"].ToString();

	Response.Write("<center>");
	Response.Write("<form name=f action=olist.aspx?t=transfer&id=" + m_id + " method=post>");
	Response.Write("<h3>Transfer Order</h3>");

	Response.Write("<table width=75%>");
	Response.Write("<tr><td><b>Order# : </b>" + m_id);
	Response.Write("<b> &nbsp; Order Date : </b>" + currentOrderDate + "</td>");
	Response.Write("</tr>");

	Response.Write("<tr><td>");
	PrintOneOrderStatus();
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>Current Branch : &nbsp; </b>" + currentBranchName + "</td></tr>");
	Response.Write("<tr><td><b>Destination Branch : </b>");
	Response.Write(PrintBranchOptions(m_branchID));
//	Response.Write(" <input type=submit name=cmd value=Transfer  "+ Session["button_style"] +" ></td>");
	Response.Write("</tr>");

	Response.Write("<tr><td><b>Sales Note : </b></td></tr>");
	Response.Write("<tr><td><textarea name=sales_note rows=5 cols=50>");
	Response.Write(currentSalesNote);
	Response.Write("</textarea></td></tr>");
	
	Response.Write("<tr><td align=middle>");
	Response.Write("<input type=submit name=cmd value=Transfer  "+ Session["button_style"] +" >");
	Response.Write("</td></tr>");
	
	Response.Write("</table>");	

	Response.Write("</form>");

	return true;
}
/*
string GetBranchName(string id)
{
	if(ds.Tables["branch_name"] != null)
		ds.Tables["branch_name"].Clear();

	string sc = " SELECT name FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "branch_name") == 1)
			return ds.Tables["branch_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";

}
*/
bool DoTransferOrder()
{
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		if(!GetOneOrder())
			return false;
	}
	else
	{
		Response.Write("<center><h3>Error, Order ID needed</h3>");
		return false;
	}

	DataRow dr = ds.Tables["one"].Rows[0];
	string currentBranchID = dr["branch"].ToString();
	string currentBranchName = dr["branch_name"].ToString();
	string destBranchID = Request.Form["branch"];
	string destBranchName = GetBranchName(destBranchID);
	string sales_note = Request.Form["sales_note"];
	sales_note += "\r\n";
	sales_note += "* Transfered from " + currentBranchName + " branch to " + destBranchName;
	sales_note += " branch by " + Session["name"] + " at " + DateTime.Now.ToString("dd-MM-yyyy HH:mm");
	sales_note += "* \r\n";

	string sc = " UPDATE orders SET branch=" + destBranchID + ", sales_note = '" + sales_note + "' ";
	sc += " WHERE id=" + m_id;
	for(int i=0; i < ds.Tables["one"].Rows.Count; i++)
	{
		dr = ds.Tables["one"].Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["quantity"].ToString();
		double dQty = MyDoubleParse(qty);

		//deallocate from current branch
		sc += " UPDATE stock_qty SET allocated_stock = allocated_stock - " + dQty;
		sc += " WHERE code=" + code + " AND branch_id=" + currentBranchID;

		//allocate at dest branch
		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + code;
		sc += " AND branch_id = " + destBranchID;
		sc += ")";
		sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
		sc += " VALUES (" + code + ", " + destBranchID + ", 0, " + dQty + ")"; 
		sc += " ELSE Update stock_qty SET ";
		sc += " allocated_stock = allocated_stock + " + dQty;
		sc += " WHERE code=" + code + " AND branch_id = " + destBranchID;
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
	return true;
}

bool DoReleaseOrder()
{
	string id = Request.QueryString["id"];
	if(id == null || id == "")
		return true;

	string sc = " UPDATE orders SET status = 1 WHERE id=" + id;
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

bool DoDeleteInvoice(string inv, string order_id)
{
	if(ds.Tables["inv_delete"] != null)
		ds.Tables["inv_delete"].Clear();
	string sc = "SELECT s.discount_percent, i.customer_gst, b.branch_header, b.branch_footer, i.invoice_number, i.type, i.payment_type, i.commit_date, i.price ";
	sc += ", i.tax, i.total, i.sales, i.cust_ponumber, cr.stock_location, ";
	sc += " i.freight, i.sales_note, c.*, s.code, s.supplier_code, s.quantity, s.name as item_name, cr.brand, cr.price1, ISNULL(cr.level_price0*cr.level_rate1/100,0) AS rrp, i.branch ";
	sc += ", s.commit_price, s.normal_price, i.type, i.special_shipto, i.shipto, shipping_method, ";
	sc += " s.status AS si_status, s.note, s.shipby, s.ship_date, s.ticket, s.processed_by ";
	sc += ", s.system AS bSystem, i.system AS iSystem, ISNULL(cr.is_service,0) AS is_service, i.no_individual_price AS iNoIndividual ";
	sc += ", s.kit, s.krid, k.kit_id, k.qty AS kit_qty, k.name AS kit_name, k.commit_price AS kit_price, s.pack ";	
	sc += ", '' AS shelf ";
	sc += " FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number LEFT OUTER JOIN card c ON c.id=i.card_id ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = s.code ";
	sc += " LEFT OUTER JOIN sales_kit k ON k.invoice_number=i.invoice_number AND k.krid = s.krid ";
	sc += " LEFT OUTER JOIN branch b ON b.id = i.branch ";
	sc += " WHERE i.invoice_number=" + inv;
	//DEBUG("sc +", sc);
	//return false;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "inv_delete");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int rows = ds.Tables["inv_delete"].Rows.Count;
	for(int i=0 ; i < rows ; i++)
	{
		DataRow dr = ds.Tables["inv_delete"].Rows[i];
		string code = dr["code"].ToString();
		int qty = MyIntParse(dr["quantity"].ToString());
		string branch_id = dr["branch"].ToString();
		updateStockQty(qty, code, branch_id, order_id);
	}
	sc = " DELETE FROM invoice WHERE invoice_number = " + inv;
	sc += " DELETE FROM sales WHERE invoice_number = " + inv;
	sc += " DELETE FROM sales_kit WHERE invoice_number = " + inv;
	sc += " UPDATE orders Set status = 5, invoice_number = null where invoice_number = " + inv;
	//sc += " DELETE FROM orders WHERE invoice_number = " + inv;

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
/*
	sc = " SELECT invoice_number FROM invoice WHERE invoice_number > " + inv;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "inv");
		return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<ds.Tables["inv"].Rows.Count; i++)
	{
		string inv = ds.Tables["inv"].Rows[i]["invoice_number"].ToString();
		if(!DoDecreaseInvNumber(inv))
			return false;
	}
*/
	return true;
}

bool DoDecreadInvNumber(string inv)
{
	string current_number = inv;
	string new_nubmer = (MyIntParse(inv) - 1).ToString();
	string sc = " SET IDENTITY_INSERT invoice ON ";
	sc = " UPDATE invoice SET ";
	return true;
}

bool CheckAllowIPOK()
{
	int i = 0;
	int j = 0;
	string[] abip = new string[1024];
	string oneip = "";

	if(Session["staff_checkin_allow_ip"] == null)
	{
		string allow_ip = GetSiteSettings("staff_checkin_allow_ip", "");
//DEBUG(" allowid=", allow_ip);
		for(i=0; i<allow_ip.Length; i++)
		{
			if(allow_ip[i] == ' ' || allow_ip[i] == ',' || allow_ip[i] == ';')
			{
				Trim(ref oneip);
				if(oneip != "")
				{
					abip[j++] = oneip;
					oneip = "";
				}
			}
			else
			{
				oneip += allow_ip[i];
			}
		}
		if(oneip != "") //the last one
		{
			abip[j++] = oneip;
			oneip = "";
		}

		Session["staff_checkin_allow_ip"] = abip;
	}
	else
	{
		abip = (string[])Session["staff_checkin_allow_ip"];
	}
	string ip = GetIP(); //Request.ServerVariables["REMOTE_ADDR"].ToString();
	//if(ip.IndexOf("192.168") != -1)
		//return true;
	if(ip == "192.168.12.1")
		return false;
//	if(Session["ip"] != null)
//		ip = Session["ip"].ToString();
	if(ip == "")
		return true;
	for(i=0; i<abip.Length; i++)
	{
		oneip = abip[i];
		if(oneip == null)
			break;

//DEBUG("oneip=", oneip);
//DEBUG("ip=", ip);
		if(ip.IndexOf(oneip) == 0)// || ip == "127.0.0.1")
			return true;
	}
	//return false;
	return true;
}
bool IsSalesQualToOrders(string invoice_number, string order_id)
{
	int nRows1 = 0;
	if(ds.Tables["isao1"] != null)
		ds.Tables["isao1"].Clear();
	string sc = " SELECT * FROM sales WHERE invoice_number = " + invoice_number;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows1 = myCommand.Fill(ds, "isao1");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int nRows2 = 0;
	if(ds.Tables["isao2"] != null)
		ds.Tables["isao2"].Clear();
	sc = " SELECT  * FROM order_item WHERE id = " + order_id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows2 = myCommand.Fill(ds, "isao2");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	//DEBUG("nRows1=", nRows1);
	//DEBUG("nRows2=", nRows2);
	if(nRows1 == nRows2)
		return true;
	return false;
	}
bool updateStockQty(int qty, string code, string branch_id, string orderID)
{   
	string sc = "";
    int orderType=2;
        
    sc  = " SELECT type from orders WHERE id = " + orderID;
    try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		SqlDataReader reader = myCommand.ExecuteReader();
        while (reader.Read())
        {
            orderType = reader.GetInt32(0);
        }
	    reader.Close();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + code;
	sc += " AND branch_id = " + branch_id;
	sc += ")";
	sc += " INSERT INTO stock_qty (code, branch_id, qty, supplier_price) ";
	sc += " VALUES (" + code + ", " + branch_id + ", " + (0 - qty).ToString() + ", ";
	sc += GetSupPrice(code) + ") "; //" (SELECT supplier_price FROM code_relations WHERE code=" + code + ")) "; 
	sc += " ELSE Update stock_qty SET ";
	sc += "qty = qty + " + qty ;
    if(orderType!=1)
    {
        sc += ",allocated_stock = allocated_stock + " + qty;
    }
	sc += " WHERE code=" + code + " AND branch_id = " + branch_id;
	if(!g_bRetailVersion)
	{
		sc += " UPDATE product SET stock = stock + " + qty + ", allocated_stock = allocated_stock + " + qty;
		sc += " WHERE code=" + code;
	}
	else //retail version only update allocated stock in product table
	{
		sc += " UPDATE product SET allocated_stock = allocated_stock + " + qty;
		sc += " WHERE code=" + code;
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
	return true;
}
string GetSupPrice(string code)
{
	if(ds.Tables["sup_price"] != null)
		ds.Tables["sup_price"].Clear();

	string s_price ="0";
	int nRows = 0;
	string sc = " SELECT supplier_price FROM code_relations WHERE code=" + code;	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "sup_price");
		if(nRows != 1)
		{
			return s_price;
		}
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return s_price;
	}
	s_price = ds.Tables["sup_price"].Rows[0]["supplier_price"].ToString();
	return s_price;
}
</script>

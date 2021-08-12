<!-- #include file="page_index.cs" -->
<!-- #include file="isdate.cs" -->
<script runat="server">
string m_selected = "1";  //selected value 

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_uri = "";
string m_SalesType = "Sales";
string m_invoice_number = "";
string m_branchID = "1";
string m_ReturnNewOrderID = "";
//----- m_querystring attribute ---//
//----- if m_querystring == ip ***** input faulty items
//----- if m_querystring == view ***** dealer, public site view ra list after apply ra
//----- if m_querystring == cr ******then create new ra number at admin site
//----- if m_querystring == all ****** show all created ra number on admin site
//---------------------------------------//

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	GetQueryString();
	if(Request.QueryString["rd"] == "done")
	{
		ShowProcessDone();
		return;
	}
	if(m_invoice_number == "" || m_invoice_number == null)
	{
		Response.Write("<center><h4>No Invoice Number!!!</h4>");
		Response.Write("<h4><a href=olist.aspx>Back to Order List</a></h4>");		
		return;
	}
	else if(!TSIsDigit(m_invoice_number))
	{
		Response.Write("<center><h4>Invalid Invoice Number!!!</h4></center>");
		return;
	}
	
	if(Request.Form["cmd"] == "Copy This Sales" || Request.Form["cmd"] == "Copy This Purchase")
	{
//DEBUG("cmd =", Request.Form["cmd"]);
		if(doCopyInvoice())
		{
			CleanSessionData();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?rd=done&rid="+ m_ReturnNewOrderID +"\">");
			return;
		}

	}
	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "" || Request.QueryString["ci"] == "all")
	{
		
		Response.Write("<form name=frm method=post>");
		if(!GetCardID())
			return;
		ShowCardInfor();
		
		return;
	}
		
	PrintForm();
	
	PrintAdminFooter();
}
void GetQueryString()
{
	if(Request.QueryString["pid"] != null && Request.QueryString["pid"] != "")
	{
		m_SalesType = "Purchase";
		m_invoice_number = Request.QueryString["pid"];
	}
	if(Request.QueryString["oid"] != null && Request.QueryString["oid"] != "")
	{
		m_SalesType = "Sales";
		m_invoice_number = Request.QueryString["oid"];
	}

	
}
void PrintForm()
{
	Response.Write("<br><br><h4><center>COPY "+ m_SalesType.ToUpper() +" ORDER</h4>");

	//Order Info...
	getOrderInfo();
	Response.Write("<br><br>");
	Response.Write("<table width=40% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr align=center><td >");
	Response.Write("Select Customer: <select name=customer onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?slt="+ m_selected +"&ci=all");
	if(m_SalesType == "Purchase")
		Response.Write("&pid=");
	else
		Response.Write("&oid=");
	Response.Write(m_invoice_number);
	Response.Write("')\" >\r\n");
	if(Session["slt_customer_cp"] != null)
		Response.Write("<option value="+ Session["slt_customer_cp"] +" selected>"+ Session["slt_name_cp"] +" </option>");
	
//	string luri = "http://"+ Request.ServerVariables["SERVER_NAME"] + Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
	string luri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];

	Response.Write("<option value='no_cust'>ALL");
	Response.Write("</select>");
	Response.Write("<input fgcolor=blue type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
	Response.Write("id=" + Session["slt_customer_cp"] + "','', ' width=350,height=350');\" value='Who?' " + Session["button_style"] + ">");
	Response.Write(" <input type=button name='Add New Customer' onclick=\"javascript:addcard_window=window.open('ezcard.aspx?r="+ DateTime.Now.ToOADate() +"&luri="+ HttpUtility.UrlEncode(luri) +"','','resizable=1, screenX=300,screenY=260,left=300,top=260, scrolling=1'); addcard_window.focus();\" value='Add new customer' " + Session["button_style"] + ">");
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=0 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th align=right>Customer Name:</td><td>");
	Response.Write(Session["slt_name_cp"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Address:</td><td>");
	Response.Write(Session["slt_add1_cp"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>");
	Response.Write(Session["slt_add2_cp"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><td></td><td>");
	Response.Write(Session["slt_city_cp"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Phone:</td><td>");
	Response.Write(""+ Session["slt_phone_cp"] +"");
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Fax:</td><td>");
	Response.Write(Session["slt_fax_cp"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("<tr><th align=right>Email:</td><td>");
	Response.Write(Session["slt_email_cp"]);
	Response.Write("&nbsp;</td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("<br><br><center>");
	//Response.Write("<input type=button value='<< Back to Created RMA# List' "+ Session["button_style"] +"");
	//Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all')\" ");
	//Response.Write(">");
	if(Session["slt_customer_cp"] != null)
	{
	Response.Write("<form name=f method=post>");
	Response.Write("<input type=submit name=cmd value='Copy This "+ m_SalesType +"' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"if(document.frm.customer.value=='no_cust'){window.alert('Please Select Customer');return false;} if(!confirm('Continue to Create RMA#?')){return false;}\" ");
	Response.Write(">");
	}
	Response.Write("</form>");
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
	m_cPI.PageSize = 35;
	m_cPI.URI = "?ci=all&r="+ DateTime.Now.ToOADate() +"";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;

	string sPageIndex = m_cPI.Print();

//	Response.Write("<form name=frm method=post>");
	Response.Write("<br><h4><center>Card List</h4>");
	Response.Write("<table align=center width=90% cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4>Seach by (ID,Name,Email...): <input type=text name=search value='"+ Request.Form["search"] +"'><input type=submit name=cmd value='Search' "+Session["button_style"] +">");
	string luri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
	Response.Write("<input type=button name=addnew value='Add New Customer' "+ Session["button_style"] +" onclick=\"window.location=('ecard.aspx?a=new&n=customer&luri="+ HttpUtility.UrlEncode(luri) +"&r="+ DateTime.Now.ToOADate() +"'); \">");
//	Response.Write("<input type=button name=addnew value='Add New Customer' "+ Session["button_style"] +" onclick=\"javascript:new_window=window.location=('ecard.aspx?a=new&n=customer&luri="+ HttpUtility.UrlEncode(luri) +"&r="+ DateTime.Now.ToString("ddMMyyyyHHmm") +"', '',''); new_window.focus();\">");
	Response.Write("</td><td colspan=2>"+ sPageIndex +"");
	Response.Write("</td></tr>");
	Response.Write("<script language=javascript>");
	Response.Write("\n\r document.frm.search.focus();\r\n </script");
	Response.Write(">");
	Response.Write("<tr bgcolor=#EDE3E3>");
	Response.Write("<tr bgcolor=#EEDDDD><th>ID</th><th>Name</th><th>Contact</th><th>Trading Name</th><th>Company</th><th>Email</th></tr>");
	string uri = ""+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"";
	if(m_SalesType == "Purchase")
		uri += "&pid=";
	else
		uri += "&oid=";
	uri += m_invoice_number;
	uri += "&ci=";
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
			string search = EncodeQuote(Request.Form["search"].ToString());

			sc += " AND (name LIKE '%"+ search +"%' OR trading_name LIKE '%"+ search +"%' ";
			sc += " OR company LIKE '%"+ search +"%' OR  email LIKE '%"+ search +"%'";
			sc += " OR phone LIKE '%"+ search +"%'";
			sc += " ) ";
		}
	}
	sc += " ORDER BY id ";
//DEBUG("sc =", sc);
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
		Session["slt_customer_cp"] = dst.Tables["customer"].Rows[0]["id"].ToString();
		Session["slt_add1_cp"] = dst.Tables["customer"].Rows[0]["address1"].ToString();
		Session["slt_add2_cp"] = dst.Tables["customer"].Rows[0]["address2"].ToString();
		Session["slt_city_cp"] = dst.Tables["customer"].Rows[0]["city"].ToString();
		Session["slt_phone_cp"] = dst.Tables["customer"].Rows[0]["phone"].ToString();
		Session["slt_fax_cp"] = dst.Tables["customer"].Rows[0]["fax"].ToString();
		Session["slt_email_cp"] = dst.Tables["customer"].Rows[0]["email"].ToString();
		if(g_bRetailVersion)
			Session["slt_name_cp"] = dst.Tables["customer"].Rows[0]["name"].ToString();
		else
			Session["slt_name_cp"] = dst.Tables["customer"].Rows[0]["company"].ToString();
		if(Session["slt_name_cp"] == null || Session["slt_name_cp"] == "")
			Session["slt_name_cp"] = dst.Tables["customer"].Rows[0]["trading_name"].ToString();

		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?r="+ DateTime.Now.ToOADate() +"");
		if(m_SalesType == "Purchase")
			Response.Write("&pid="+ m_invoice_number +"");
		else
			Response.Write("&oid="+ m_invoice_number +"");

		Response.Write("\">");
		return false;
	}

	return true;
}

bool getOrderInfo()
{
	if(dst.Tables["order"] != null)
		dst.Tables["order"].Clear();

	string sc = "";
//	if(m_SalesType == "Sales")
//	{
		
//	sc = " SELECT * FROM (";
	sc += "SELECT o.*, i.paid AS ipaid, c.name, c.company, c.trading_name, c.email, c1.name AS lockedby ";
	sc += ", DATEDIFF(day, o.time_locked, GETDATE()) AS days_locked ";
	sc += ", c.credit_term, c.balance, d.name AS sales_name ";
//	if(Session["branch_support"] != null)
		sc += " , b.name AS branch_name ";
	sc += ", (SELECT SUM(commit_price * quantity) FROM order_item WHERE order_item.id=o.id) AS amount ";
	sc += " FROM orders o LEFT OUTER JOIN card c ON c.id=o.card_id LEFT OUTER JOIN card d ON d.id=o.sales ";
	sc += " LEFT OUTER JOIN invoice i ON i.invoice_number = o.invoice_number ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id=o.locked_by ";
//	if(Session["branch_support"] != null)
		sc += " LEFT OUTER JOIN branch b ON b.id = o.branch ";
	sc += " WHERE 1=1 ";
//	if(Session["branch_support"] != null)
//		sc += " AND o.branch=" + m_branchID + " ";
	sc += " AND o.id = "+ m_invoice_number;
	sc += " ORDER BY o.id DESC";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "order");
	
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<table width=89% align=center cellspacing=1 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=9><b>"+ m_SalesType +" Details</b></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	if(Session["branch_support"] != null)
		Response.Write("<th>Branch</th>");
	Response.Write("<th>Sales</th>");
	Response.Write("<th>Date</th>");
//	Response.Write("<th>Agent</th>");
	Response.Write("<th>Customer</th>");
	Response.Write("<th>Term</th>");
	Response.Write("<th nowrap>PO Number</th>");
	Response.Write("<th>"+ m_SalesType +" Order#</th>");
	Response.Write("<th>Total</th>");
	Response.Write("<th>Shipping</th>");
	Response.Write("<th>Order#</th>");
	
	Response.Write("</tr>");


	bool bAlterColor = false;
	string bgc = GetSiteSettings("table_row_bgcolor", "#EEEEEE");
	string sbgc = GetSiteSettings("sys_quote_bgcolor", "#AAFFFF");
	string sbgcl = GetSiteSettings("sys_quote_bgcolor_light", "#DDFFFF");
	bool bCanChangeInvoice = CheckAccess(Session[m_sCompanyName + "AccessLevel"].ToString(), "invchg.aspx");
	for(int i=0; i<dst.Tables["order"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["order"].Rows[i];
		string branch_id = dr["branch"].ToString();
		string branch = dr["branch_name"].ToString();
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
		
//		string agent = dr["agent_name"].ToString();
		double dstotal = MyDoubleParse(dr["amount"].ToString());
		
		string date = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM HH:mm");
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
	
		Response.Write("<tr");
		
		bAlterColor = !bAlterColor;

		Response.Write(">");
	//	if(Session["branch_support"] != null)
			Response.Write("<td>" + branch + "</td>");
		Response.Write("<td>" + sales + "</td>");
		
		Response.Write("<td align=center nowrap>" + date + "</td>");
//		Response.Write("<td>" + agent + "</td>");
		Response.Write("<td nowrap title='" + companyfull + "'>" + company + "</td>");
		Response.Write("<td nowrap>" + credit_term + "</td>");
		

		Response.Write("<td nowrap");
		if(bDraft)
			Response.Write(" title='Customer Name : " + dealer_customer_name + "'");
		Response.Write(">" + po_number + "</td>");
		Response.Write("<td nowrap>");
		Response.Write("<a href=invoice.aspx?id=" + id + "&t=order class=o title='Print Invoice' target=_blank>");
		Response.Write(invoice_number);
		Response.Write("</a></td>");
				Response.Write("<td>" + dstotal.ToString("c") + "</td>");
		Response.Write("<td nowrap align=center title=\"" + sm_title + "\">" + shipping_method + "</td>");
		Response.Write("<td nowrap>");		
		Response.Write("<a href=invoice.aspx?id=" + id + "&t=order class=o title='Print Order' target=_blank>" + order_number);
		if(part != "0")
			Response.Write("." + part);
		Response.Write("</a>");
		Response.Write(" <a href=invoice.aspx?id=" + id + "&t=order class=o title='Print Order' target=_blank>V</a>");
	//	Response.Write(" <a href=invoice.aspx?id=" + id + "&t=order&confirm=1&email=" + HttpUtility.UrlEncode(email));
	//	Response.Write(" class=o target=_blank title='Email Order to customer'>M</a>");

		Response.Write("</td>");
		Response.Write("</tr>");

	}
	

	Response.Write("</table>");

	return true;
}
void CleanSessionData()
{
	Session["slt_customer_cp"] = null;
	Session["slt_add1_cp"] = null;
	Session["slt_add2_cp"] = null;
	Session["slt_city_cp"] = null;
	Session["slt_phone_cp"] = null;
	Session["slt_fax_cp"] = null;
	Session["slt_email_cp"] = null;
}
bool doCopyInvoice()
{
	string card_id = Session["slt_customer_cp"].ToString();
//	DEBUG("card =", card_id);
	string sc = " ";
	if(card_id == null || card_id == "")
		return false;
	
	doCreateCopyInvoiceSP();

	if(m_SalesType == "Sales")
	{

		/*sc = " EXECUTE sp_copy_invoice "+ card_id +", "+ m_invoice_number +" ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dst);
			
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		*/
		SqlCommand myCommand = new SqlCommand("sp_copy_invoice", myConnection);
		myCommand.CommandType = CommandType.StoredProcedure;
		myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = card_id;
		myCommand.Parameters.Add("@invoice_number", SqlDbType.Int).Value = m_invoice_number;
		myCommand.Parameters.Add("@return_status", SqlDbType.Int).Direction = ParameterDirection.Output;

		try
		{
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp("Copy Invoice Error: ", e);
			return false;
		}

		m_ReturnNewOrderID = myCommand.Parameters["@return_status"].Value.ToString();
	//	DEBUG("returnID =", returnID);
	}

	return true;
}

bool doCreateCopyInvoiceSP()
{
	string sc = " IF NOT EXISTS (SELECT * FROM syscomments WHERE id = object_id('dbo.sp_copy_invoice')) SELECT top 10 *  FROM card ";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst);
	}
	catch(Exception e) 
	{
		//if(e.ToString().IndexOf("already an object named") >= 0)
		//	return true;
		ShowExp(sc, e);
		//doDropChangeCodeProcedure();
		return false;
	}
	if(rows > 0)
	{
//	DEBUG("rows =", rows);
//	return false;
	sc = " CREATE PROCEDURE sp_copy_invoice  \r\n";
	sc += " @card_id int, \r\n";
	sc += " @invoice_number int,  \r\n";	
	sc += " @return_status int  OUTPUT \r\n";
	sc += " AS \r\n";
	sc += " DECLARE @new_order_id int  \r\n";	
	sc += " BEGIN TRANSACTION \r\n";

	sc += " BEGIN \r\n";
	sc += " INSERT INTO orders \r\n";
	sc += " (branch, number, part, card_id, po_number, status, record_date, contact, special_shipto, shipto, date_shipped, shipby, freight, \r\n";
    sc += " ticket, sales, sales_manager, sales_note, locked_by, time_locked, shipping_method, pick_up_time, payment_type,  \r\n";
	sc += " debug_info, system, no_individual_price, gst_inclusive, type, quote_total,  dealer_draft, ship_as_parts, dealer_customer_name, \r\n";
	sc += " dealer_total, unchecked, status_orderonly, credit_order_id, agent) \r\n";
	sc += " SELECT branch, number, part, @card_id, po_number, 1, record_date, contact, special_shipto, shipto, date_shipped, shipby, freight, \r\n";
	sc += " ticket, sales, sales_manager, sales_note, locked_by, time_locked, shipping_method, pick_up_time, payment_type, \r\n";
	sc += " debug_info, system, no_individual_price, gst_inclusive, type, quote_total,  dealer_draft, ship_as_parts, dealer_customer_name, \r\n";
	sc += " dealer_total, unchecked, status_orderonly, credit_order_id, agent \r\n";		
	sc += " FROM orders WHERE id = @invoice_number \r\n";

	sc += " \t SELECT @new_order_id = IDENT_CURRENT('orders')  \r\n";
	sc += " \t UPDATE orders SET number = @new_order_id WHERE id = @new_order_id AND number = @invoice_number \r\n";

	//*** insert into order item table
	sc += " \t INSERT INTO order_item (id, code, quantity, item_name, supplier \r\n";
	sc += " , supplier_code, supplier_price, commit_price, eta, note, system, sys_special, part, kit, krid) \r\n";
	sc += " SELECT @new_order_id, code, quantity, item_name, supplier, supplier_code \r\n";
	sc += " , supplier_price, commit_price"; //ISNULL((SELECT TOP 1 oi.commit_price FROM order_item oi JOIN orders o ON o.id = oi.id AND oi.id = @invoice_number  AND oi.code = oii.code ORDER BY oi.id DESC), commit_price) \r\n";
	sc += " , eta, note, system, sys_special, part, kit, krid \r\n";
	sc += " FROM order_item oii WHERE id = @invoice_number \r\n";
	
	//*** insert into order kit
	sc += " \t INSERT INTO order_kit (id, kit_id, qty, name, details, warranty, base_selling_price, commit_price) \r\n";
	sc += " SELECT @new_order_id, kit_id, qty, name, details, warranty, base_selling_price, commit_price \r\n";	
	sc += " FROM order_kit WHERE id = @invoice_number \r\n";

	sc += " \t END \r\n";
//	sc += " END \r\n";

	sc += " commit transaction \r\n";
	sc += " set @return_status = @new_order_id --done\r\n";

//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst);
	}
	catch(Exception e) 
	{
		//if(e.ToString().IndexOf("already an object named") >= 0)
		//	return true;
		ShowExp(sc, e);
		//doDropChangeCodeProcedure();
		return false;
	}
	
	}
	return true;
}
void ShowProcessDone()
{
	m_ReturnNewOrderID = Request.QueryString["rid"];

	Response.Write("<center><br><h4>Copy Done...</h4>");
	Response.Write("<br><br><font size=4 color=red> New "+ m_SalesType +" Order ID: <a href='olist.aspx?kw="+ m_ReturnNewOrderID +"'>"+ m_ReturnNewOrderID +"</a></font>");
	
	//Response.Write("<script language=javascript>window.alert('Copy Done...'); window.location='"+ Request.ServerVariables["URL"] +"?cd=" + Session["ch_code"].ToString() + "';</script");
	//Response.Write(">");
	Response.Write("</center>");
}

</script>
<asp:Label id=LFooter runat=server/>

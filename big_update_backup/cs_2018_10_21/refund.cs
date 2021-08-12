<!-- #include file="cart.cs" -->
<!-- #include file="card_function.cs" -->
<!-- #include file="sales_function.cs" -->

<script runat=server>

string m_account = "";
string m_inv = "";

bool m_bPayAll = false;
string m_invoiceNumber = "";
string m_paymentMethod = "";
string m_salesType = "";
string m_sAccBalance = "";
string m_status = "";

bool m_bDodiscount = false;	//assess whether update account discount, HG 13.Aug.2002

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("accountant"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	
	if(Request.QueryString["t"] == "payall")
		m_bPayAll = true;
	
	if(Request.QueryString["i"] != null)
		m_invoiceNumber = Request.QueryString["i"];
//DEBUG("inv=", m_invoiceNumber);

	if(Request.Form["cmd"] == "Cancel")
	{

	}
	else if(Request.QueryString["ci"] != null)
	{
		m_customerID = Request.QueryString["ci"];
		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=salespay.aspx?i=" + m_invoiceNumber + "\">");
		return;
	}
	else if(Request.QueryString["search"] == "1")
	{
		Response.Write("<br><center><h3>Search For Customer</h3></center>");
		DoCustomerSearch();
		return;
	}
	else if(Request.Form["cmd"] == "Record")
	{
		if(RecordTransaction())
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=salespay.aspx?i=" + m_invoiceNumber + "\">");
		return;
	}

	if(m_invoiceNumber != "")
	{
		if(!DoSearch())
			return;
		MyDrawTable();
	}
	
	PrintAdminFooter();
}

bool RecordTransaction()
{
	string payment_method = Request.Form["payment_method"];
	string sTotal = Request.Form["total"];

	SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;
	myCommand.Parameters.Add("@nSource", SqlDbType.Int).Value = Request.Form["account"];
	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	if(Request.Form["card_id"] != "")
	{
		myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = Request.Form["card_id"];
		m_bDodiscount = true;
	}
//	myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = sTotal;
	myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = payment_method;
	myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = m_invoiceNumber;
	myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = Request.Form["payment_ref"];
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = Request.Form["note"];
	myCommand.Parameters.Add("@Amount", SqlDbType.Money).Value = "-" + sTotal;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoUpplierPayment", e);
		return false;
	}

	//close invoice
	string receipt_type = GetEnumID("receipt_type", "invoice"); //fully paid
	string status_close = GetEnumID("general_status", "closed");
	string sc = "UPDATE invoice SET paid=1, type=" + receipt_type + ", status=" + status_close;
	sc += " WHERE invoice_number=" + m_invoiceNumber;
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
	}

	sc = "UPDATE sales SET status=";
	sc += GetEnumID("sales_item_status", "payment confirmed");
	sc += " WHERE invoice_number=" + m_invoiceNumber;
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
	if(m_bDodiscount)
	{
		if(!UpdateDiscount(Request.Form["card_id"], sTotal))
			return false;
	}
	return true;
}

bool DoSearch()
{
	int rows = 0;
	string sc = "SELECT * FROM invoice WHERE invoice_number=" + m_invoiceNumber;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "inv");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1)
	{
		m_salesType = GetEnumValue("sales_type", dst.Tables["inv"].Rows[0]["sales_type"].ToString());
		m_paymentMethod = GetEnumValue("payment_method", dst.Tables["inv"].Rows[0]["payment_type"].ToString());
	}
	return true;
}

bool PrintAccountList()
{
	if(m_account == "")
	{
		if(m_salesType == "online")
			m_account = GetSiteSettings("income_account_for_online_sales");
		else
			m_account = GetSiteSettings("income_account");
	}

	int rows = 0;
	string sc = "SELECT * FROM account WHERE class1=4 OR class1=1 ORDER BY class1, class2, class3, class4";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "account");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<table width=100%>");
	Response.Write("<tr><td><b>Account : </b>");
	Response.Write("<select name=account onchange=\"window.location=");
	Response.Write("('salespay.aspx?acc='+this.options[this.selectedIndex].value)\">");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["account"].Rows[i];
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		double dAccBalance = double.Parse(dr["balance"].ToString());
		Response.Write("<option value=" + number);
		if(number == m_account)
		{
			Response.Write(" selected");
			m_sAccBalance = dAccBalance.ToString("c");
		}
		Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString() + dAccBalance.ToString("c"));		
	}
	Response.Write("</select>");
	if(m_sAccBalance != "")
		Response.Write("<b>&nbsp&nbsp&nbsp; ------------ Balance : " + m_sAccBalance + "</b>");
	Response.Write("</td></tr></table><br>");
	return true;
}

bool MyDrawTable()
{
	//get data
	DataRow dr = dst.Tables["inv"].Rows[0];
	m_status = GetEnumValue("general_status", dr["status"].ToString());
	string card_id = dr["card_id"].ToString();
	string name = dr["name"].ToString();
	string company = dr["trading_name"].ToString();
	string date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd/MM/yyyy");
	string inv_number = dr["invoice_number"].ToString();
	string payment_method = dr["payment_type"].ToString();
	string amount = dr["total"].ToString();
	string tax = dr["gst"].ToString();
	double dBalance = 0;
	double dHistory = 0;
	double dCredit = 0;
	if(card_id != "")
	{
		m_customerID = card_id;
		DataRow drCard = GetCardData(card_id);
		if(drCard != null)
		{
			dBalance = double.Parse(drCard["balance"].ToString());
			dHistory = double.Parse(drCard["trans_total"].ToString());
			dCredit = double.Parse(drCard["credit_limit"].ToString());
			m_customerName = name;
		}
	}

	double dAmount = 0;
	if(amount != "")
		dAmount = double.Parse(amount);
	double dTax = 0;
	if(tax != "")
		dTax = double.Parse(tax);
	double dTotal = dAmount + dTax;
	string status = GetEnumValue("general_status", dr["status"].ToString());
	
	//print table
	Response.Write("<br><center><h3>Refund</h3></center>");
	Response.Write("<form name=form1 action=salespay.aspx?i=" + m_invoiceNumber + " method=post>");
	Response.Write("<input type=hidden name=card_id value=" + card_id + ">");
	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td>");

	//account list table
	if(!PrintAccountList())
		return false;
	
	//customer & cheque table
	Response.Write("\r\n<table width=100%>");
	Response.Write("\r\n<tr><td valign=top>");

	//customer table
	Response.Write(DrawCustomerTable());

	Response.Write("\r\n</td><td>");

	//cheque table
	Response.Write("\r\n<table align=right>");
	Response.Write("\r\n<tr><td align=right><b>Payment Ref : </b></td><td><input type=text name=payment_ref style='text-align:right'></td></tr>");
	Response.Write("\r\n<tr><td align=right><b>Date : </b></td><td><input type=text name=date value='" + DateTime.Now.ToString("dd/MM/yyyy") + "' style='text-align:right'></td></tr>");
	Response.Write("\r\n<tr><td align=right><b>Amount : </b></td><td>");
	Response.Write("\r\n<input type=text name=amount onclick=\"if(this.value=='')this.value=" + dTotal + ";\" ");
	if(m_bPayAll)
		Response.Write("value=" + dTotal.ToString());
	Response.Write(" style='text-align:right'></td></tr>");
	Response.Write("\r\n</table>");

	Response.Write("\r\n</td></tr></table>");
	//end of customer & cheque table

	Response.Write("\r\n</td>");
	Response.Write("</tr><tr><td>");

	//main list
	Response.Write("\r\n<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("\r\n<td>Invoice #</td>\r\n");
	Response.Write("\r\n<td>Date</td>\r\n");
	Response.Write("\r\n<td>Status</td>");
//	Response.Write("\r\n<td>Card ID</td>");
	Response.Write("\r\n<td>Customer</td>");
	Response.Write("\r\n<td>Company</td>");
	Response.Write("\r\n<td>Purchase History</td>");
	Response.Write("\r\n<td>Balance</td>");
	Response.Write("\r\n<td>Credit</td>");
	Response.Write("\r\n<td>Payment</td>");
//	Response.Write("\r\n<td>Amount</td>");
//	Response.Write("\r\n<td>Tax</td>");
	Response.Write("\r\n<td>Total</td>");
	Response.Write("\r\n<td align=center>Applied Amount</td>");
	Response.Write("</tr>\r\n");

	Response.Write("\r\n<tr>");
	Response.Write("\r\n<td><a href=invoice.aspx?" + inv_number + " class=o target=_blank>" + inv_number + "</a></td>");
	Response.Write("\r\n<td>" + date + "</td>");
	Response.Write("\r\n<td>" + m_status + "</td>");
//	Response.Write("\r\n<td>" + card_id + "</td>");
	Response.Write("\r\n<td>" + name + "</td>");
	Response.Write("\r\n<td>" + company + "</td>");
	Response.Write("\r\n<td>" + dHistory.ToString("c") + "</td>");
	Response.Write("\r\n<td>" + dBalance.ToString("c") + "</td>");
	Response.Write("\r\n<td>" + dCredit.ToString("c") + "</td>");
	Response.Write("\r\n<td><select name=payment_method>" + GetEnumOptions("payment_method", payment_method) + "</select></td>");
//	Response.Write("\r\n<td>" + dAmount.ToString("c") + "<input type=hidden name=amount value=" + dAmount + "></td>");
//	Response.Write("\r\n<td>" + dTax.ToString("c") + "</td>");
	Response.Write("\r\n<td>" + dTotal.ToString("c") + "<input type=hidden name=total value=" + dTotal + "></td>");
	Response.Write("\r\n<td align=right>\r\n<input type=text name=applied_amount");
	Response.Write(" onclick=\"if(this.value=='')this.value=" + dAmount);
	Response.Write(";CalcTotal();\" onchange=\"CalcTotal()\"");
	if(m_bPayAll)
		Response.Write(" value=" + dTotal.ToString());
	Response.Write(" style='text-align:right'></td>\r\n");
	Response.Write("</tr>\r\n");
	Response.Write("</table>");
	
	Response.Write("<br><br>");

	Response.Write("</td></tr><tr><td>");

	double dzero = 0;
	//paid table and buttons
	Response.Write("<table width=100%>");
	Response.Write("<tr><td colspan=2 align=right><b>Total Applied : </b><input type=text name=total_applied value='");
	if(m_bPayAll)
		Response.Write(dTotal.ToString());
	Response.Write("' style='text-align:right'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><b>Finance Charge : </b><input type=text name=finance_charge value='");
	if(m_bPayAll)
		Response.Write(dzero.ToString());
	Response.Write("' style='text-align:right'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><b>Total Paid : </b><input type=text name=total_paid value='");
	if(m_bPayAll)
		Response.Write(dTotal.ToString());
	Response.Write("' style='text-align:right'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><b>Out Of Balance : </b><input type=text name=out_of_balance value='");
	if(m_bPayAll)
		Response.Write(dzero.ToString());
	Response.Write("' style='text-align:right'></td></tr>");
	
//	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2><b>Note : </b></td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=note rows=3 cols=50></textarea></td></tr>");

	Response.Write("<tr><td>");
	if(m_status != "closed")
		Response.Write("<button onclick=window.location=('salespay.aspx?t=payall&i=" + m_invoiceNumber + "')>Pay All</button>");

	Response.Write("</td><td align=right>");

	if(m_status != "closed")
		Response.Write("<input type=submit name=cmd value=Record>");
	else
	{
		if(m_salesType == "online")
		{
			Response.Write("<button onclick=window.location=('esales.aspx?i=" + m_invoiceNumber + "')>Go On Shipping</button>");
		}
	}
	Response.Write("<input type=submit name=cmd value=Cancel></td></tr>");

	Response.Write("</table>");

	Response.Write("</td></tr></table>");

	Response.Write("\r\n<input type=hidden name=invoice_number value='" + m_invoiceNumber + "'>\r\n");
	//buttons
	Response.Write("</form>");

	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
//	Response.Write("function OnClickAppliedAmount(amout){");
	Response.Write("function CalcTotal()");
	Response.Write("{ var total = 0;");
//	Response.Write("for(var i=0; i<" + dst.Tables["purchase"].Rows.Count + ";i++)");	
//	Response.Write("{");
	Response.Write("	total += Number(document.form1.total.value);\r\n");
//	Response.Write("	document.form1.amount.value=total;\r\n");
	Response.Write("	document.form1.total_applied.value=total;\r\n");
	Response.Write("	document.form1.total_paid.value=total;\r\n");
//	Response.Write("}");
	Response.Write("}");
	Response.Write("</script");
	Response.Write(">");

	return true;
}
</script>
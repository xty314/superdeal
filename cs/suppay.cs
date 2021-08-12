<script runat=server>

int page = 1;
const int m_nPageSize = 15; //how many rows in oen page

int m_supplierID = 0;
string m_sSupplierList = "";
string m_sSupplierNameAddress = "";
string m_account = "";
string m_inv = "";
string m_tranid = "";
string m_paymentType = "2";
string m_paymentRef = "";
string m_nextChequeNumber = "";
bool m_bPayAll = false;
string tableWidth = "97%";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	m_nextChequeNumber = GetSiteSettings("next_cheque_number", "1000");
	m_paymentRef = m_nextChequeNumber;

	PrintAdminHeader();
	PrintAdminMenu();
	
	if(Request.QueryString["t"] == "payall")
		m_bPayAll = true;

	string id = Request.QueryString["id"];
	if(id != null)
	{
		m_supplierID = int.Parse(id);
		Session[m_sCompanyName + "supplier_payment"] = m_supplierID;
	}
	else if(Session[m_sCompanyName + "supplier_payment"] != null)
		m_supplierID = (int)Session[m_sCompanyName + "supplier_payment"];

	if(Request.QueryString["acc"] != null)
	{
		m_account = Request.QueryString["acc"];
		if(!SetSiteSettings("purchase_account", m_account))
			return;
	}
	if(!GetSupplierList())
		return;
	
		
//DEBUG("action=", action);
	if(Request.Form["cmd"] == "Record")
	{
		if(Session["in_suppay"] == null)
		{
			Response.Write("<br><br><center><h3><font color=red>STOP!</font> Repost Form is forbidden");
			PrintAdminFooter();
			return;
		}
		Session["in_suppay"] = null;
		if(Request.Form["confirm_record"] != "on")
		{
			Response.Write("<br><br><center><h3>Please tick 'Tick to record'</h3>");
			return;
		}
		if(RecordTransaction())
		{
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=remit.aspx?id=" + m_tranid + "\">");
			//	Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=suppay.aspx?id=" + m_supplierID + "\">");
		}
	}
	else
	{
		if(!DoSearch())
			return;
		MyDrawTable();
		Session["in_suppay"] = true;
	}
	
	PrintAdminFooter();
}

bool RecordTransaction()
{
	string status_closed = GetEnumID("general_status", "closed");
	string sc = "";
	int i = 0;
	string id = Request.Form["id" + i];
	string invoices = "";
	string amountList = "";
	while(id != null)
	{
		double d_balance = MyDoubleParse(Request.Form["owed" + i]);
		d_balance = Math.Round(d_balance, 2);

		double d_applied = 0;
		if(Request.Form["applied_amount" + i] != "" && Request.Form["applied_amount" + i] != null)
		{
			d_applied = MyDoubleParse(Request.Form["applied_amount" + i]);
			d_applied = Math.Round(d_applied, 2);
		}

		//record applied invoice number and amount applied to each
		if(d_applied != 0 || d_balance == 0)
		{
			invoices += id + ","; //must have a comma at the end of amountlist string, see store procedure
			string amount = Request.Form["applied_amount" + i];
			if(amount == "")
				amount = "0";
			amountList += amount + ","; //must have a comma at the end of amountlist string, see store procedure
		}
		i++;
		id = Request.Form["id" + i];
	}
//DEBUG("invoices=", invoices);
//DEBUG("amountlist=", amountList);
//return false;
	if(invoices == "") //nothing to record
		return true;

	m_paymentType = Request.Form["payment_type"];//GetEnumID("payment_method", "cheque");
	m_paymentRef = Request.Form["payment_ref"];
	string sAmount = Request.Form["amount"];
	string finance = Request.Form["finance"];
	if(finance == "")
		finance = "0";
	double dFinance = MyMoneyParse(finance);
	
	string currency_loss = Request.Form["currency_loss"];
	if(currency_loss == "")
		currency_loss = "0";
	double dCurrencyLoss = MyMoneyParse(currency_loss);
	
	double dAmount = Math.Round(MyMoneyParse(sAmount), 2);

	SqlCommand myCommand1 = new SqlCommand("eznz_payment", myConnection);
	myCommand1.CommandType = CommandType.StoredProcedure;

	myCommand1.Parameters.Add("@bPurchase", SqlDbType.Bit).Value = 1;
	myCommand1.Parameters.Add("@nSource", SqlDbType.Int).Value = Request.Form["account"];
	myCommand1.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand1.Parameters.Add("@card_id", SqlDbType.Int).Value = Request.Form["supplier"];
	myCommand1.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = dAmount;
	myCommand1.Parameters.Add("@payment_method", SqlDbType.Int).Value = m_paymentType;
	myCommand1.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = invoices.ToString();
	myCommand1.Parameters.Add("@amountList", SqlDbType.VarChar).Value = amountList;
	myCommand1.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = EncodeQuote(m_paymentRef);
	myCommand1.Parameters.Add("@note", SqlDbType.VarChar).Value = Request.Form["note"];
	myCommand1.Parameters.Add("@currency_loss", SqlDbType.Money).Value = dCurrencyLoss;
	myCommand1.Parameters.Add("@finance", SqlDbType.Money).Value = dFinance;
	myCommand1.Parameters.Add("@Amount", SqlDbType.Money).Value = dAmount;
	myCommand1.Parameters.Add("@payment_date", SqlDbType.VarChar).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm"); //Request.Form["date"];
	myCommand1.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand1.ExecuteNonQuery();
		myCommand1.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoSupplierPayment", e);
		return false;
	}

	m_tranid = myCommand1.Parameters["@return_tran_id"].Value.ToString();
//DEBUG("m_trans=", m_tranid);
	if(m_paymentType == "2") //cheque
	{
		string s = m_paymentRef;
		int p = s.Length - 1;
		for(; p>=0; p--)
		{
			if(!Char.IsDigit(s[p]))
				break;
		}
		string tail = "";
		if(p < s.Length - 1)
			tail = s.Substring(p+1, s.Length - 1 - p);
		if(tail != "")
		{
			string head = "";
			if(p > 0)
				head = m_paymentRef.Substring(0, p);
			s = head + (MyIntParse(tail) + 1).ToString();
			SetSiteSettings("next_cheque_number", s);
		}
	}
/*
	i = 0;
	id = Request.Form["id" + i];
	while(id != null)
	{
		string amount = Request.Form["amount" + i];
		string paid = Request.Form["applied_amount" + i];
		if(paid != "" && paid != "0")
		{
			dAmount = Math.Round(MyMoneyParse(amount), 2);
			double dPaid = Math.Round(MyMoneyParse(paid), 2);
			if(dPaid > dAmount)
			{
				Response.Write("<br><br><center><h3>Error, applied payment amount cannot more than total amount due</h3>");
				return false;
			}
			else if(dPaid == dAmount)
			{
				sc += " UPDATE purchase SET payment_status=" + status_closed + ", amount_paid=" + dPaid + " WHERE id=" + id + " ";
			}
			else if(dPaid < dAmount)
			{
				sc += " UPDATE purchase SET amount_paid=" + dPaid + " WHERE id=" + id;
			}
			sc += " INSERT INTO tran_invoice (tran_id, invoice_number, amount_applied, purchase) ";
			sc += " values( " + m_tranid + ", " + id + ", " + dPaid + ", 1) ";
		}
		i++;
		id = Request.Form["id" + i];
	}

	if(sc == "")
		return true;

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
*/
	//increase chequen number
//	int nCheque = int.Parse(Request.Form["cheque"]) + 1;
//	SetSiteSettings("next_cheque_number", nCheque.ToString());

	return true;
}

bool GetSupplierList()
{
	int rows = 0;
	string supplier = GetEnumID("card_type", "supplier");
	string sc = "SELECT DISTINCT c.* FROM card c  ";
	sc += " WHERE c.type=" + supplier + "  ";	
	sc += " ORDER BY c.company ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "card");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	m_sSupplierList = "<select name=supplier onchange=\"window.location=";
	m_sSupplierList += "('suppay.aspx?r=" + DateTime.Now.ToOADate() + "&id='+this.options[this.selectedIndex].value)\">";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["card"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["company"].ToString();
		if(name == "")
			name = dr["short_name"].ToString();
		if(name == "")
			name = dr["name"].ToString();
		if(m_supplierID == 0)
			m_supplierID = int.Parse(id);
		m_sSupplierList += "<option value=" + id;
		if(m_supplierID == 0 && i == 0)
			m_supplierID = int.Parse(id); //set to first supplier
		if(m_supplierID.ToString() == id)
		{
			m_sSupplierList += " selected";
			m_sSupplierNameAddress += dr["name"].ToString() + "\r\n";
			if(dr["company"] != null)
				m_sSupplierNameAddress += dr["company"].ToString() + "\r\n";
			m_sSupplierNameAddress += dr["address1"].ToString() + "\r\n";
			m_sSupplierNameAddress += dr["address2"].ToString() + "\r\n";
			m_sSupplierNameAddress += dr["city"].ToString() + "\r\n";
			m_sSupplierNameAddress += dr["postal1"].ToString() + "\r\n";
			m_sSupplierNameAddress += dr["postal2"].ToString() + "\r\n";
			m_sSupplierNameAddress += dr["postal3"].ToString() + "\r\n";
		}
		m_sSupplierList += ">" + name;
	}
	m_sSupplierList += "</select>";
	return true;
}

bool DoSearch()
{
	string status_open = GetEnumID("general_status", "open");
	string sc = "SELECT id, date_create, date_invoiced, po_number, inv_number ";
	sc += ", total_amount, total_amount-amount_paid AS owed, payment_status ";
	sc += " FROM purchase WHERE supplier_id=" + m_supplierID;
	sc += " AND payment_status=" + status_open + " AND type=4 ORDER BY convert(datetime, date_invoiced)";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "purchase");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintAccountList()
{
	if(m_account == "")
		m_account = GetSiteSettings("purchase_account");

	int rows = 0;
	string sc = "SELECT * FROM account WHERE class1=1 OR class1=2 ORDER BY class1, class2, class3, class4";
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
	Response.Write("('suppay.aspx?acc='+this.options[this.selectedIndex].value)\">");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["account"].Rows[i];
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		Response.Write("<option value=" + number);
		if(number == m_account)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString()+ " $" +dr["balance"].ToString());		
	}
	Response.Write("</select></td></tr></table>");
	return true;
}

bool MyDrawTable()
{
//	Response.Write("<br><center><h3>Supplier Payment</h3></center>");
		Response.Write("<form name=f action=suppay.aspx method=post>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>" + Lang("Supplier Payment") + "</font></font>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
//	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#CCCCCC bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<table border=1 align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=11><br></td></tr>");

	Response.Write("<tr><td>");

	//account list table
	if(!PrintAccountList())
		return false;

	Response.Write("</td></tr><tr><td>");

	double dTotal = 0;
	StringBuilder sb = new StringBuilder();
	bool bAlter = false;
	int PurchaesRows = dst.Tables["purchase"].Rows.Count;
	for(int i=0; i<dst.Tables["purchase"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["purchase"].Rows[i];
		string id = dr["id"].ToString();
		string date = DateTime.Parse(dr["date_create"].ToString()).ToString("dd/MM/yyyy");
		if(dr["date_invoiced"].ToString() != "")
			date = DateTime.Parse(dr["date_invoiced"].ToString()).ToString("dd/MM/yyyy");
		string po_number = dr["po_number"].ToString();
		string inv_number = dr["inv_number"].ToString();
//		string supplier = dr["short_name"].ToString();
		string owed = dr["owed"].ToString();
		double dOwed = MyMoneyParse(owed);
		string amount = dr["total_amount"].ToString();
		double dAmount = MyMoneyParse(amount);
		
		string status = GetEnumValue("general_status", dr["payment_status"].ToString());

		sb.Append("<input type=hidden name=id" + i + " value=" + id + ">");
		sb.Append("<input type=hidden name=amount" + i + " value=" + amount + ">");
		sb.Append("<input type=hidden name=owed" + i + " value=" + dOwed + ">");
		sb.Append("\r\n<tr");
		if(bAlter)
			sb.Append(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		sb.Append(">");
		sb.Append("\r\n<td>" + date + "</td>");
//		sb.Append("<td>" + supplier + "</td>");
		sb.Append("<td><a href=purchase.aspx?t=pp&n=" + dr["id"].ToString() + "&r=" + DateTime.Now.ToOADate() + " target=_blank class=o>" + po_number + "</a></td>");
//		sb.Append("\r\n<td>" + po_number + "</td>");
		sb.Append("<td>" + inv_number + "</td>");
		sb.Append("\r\n<td>" + status + "</td>");
		sb.Append("\r\n<td>" + dAmount.ToString("c") + "</td>");
//		sb.Append("\r\n<td>&nbsp;</td>");
		sb.Append("\r\n<td>" + dOwed.ToString("c") + "</td>");
		sb.Append("\r\n<td align=right>\r\n<input type=text name=applied_amount" + i.ToString());
//		sb.Append(" onclick=\"if(this.value=='')this.value=" + dOwed);
		sb.Append(" onclick=\"if(this.value=='' || this.value=='0')\r\n");
		sb.Append("{var left=CalcApplyLeft();\r\n ");
		sb.Append("if(left>" + dOwed + "){this.value=" + dOwed + ";}else{this.value=left;}\r\n ");
		sb.Append(";CalcTotal();}\" onchange=\"CalcTotal();\"");
		if(m_bPayAll)
			sb.Append(" value=" + dOwed.ToString());
		sb.Append(" style='text-align:right'></td>\r\n");
		sb.Append("</tr>\r\n");
		dTotal += dOwed;
		if(m_inv != "")
			m_inv += ",";
		m_inv += inv_number;
	}
	
	//supplier & cheque table
	{
		Response.Write("\r\n<table width=100%>");
		Response.Write("\r\n<tr><td>");

		//supplier list table
		Response.Write("\r\n<table>");
		Response.Write("\r\n<tr><td><b>Supplier : </b></td><td>");
		Response.Write(m_sSupplierList);
		Response.Write("\r\n</td></tr>");
		Response.Write("\r\n<tr><td><b>Address : </b></td><td><textarea rows=5 cols=30>" + m_sSupplierNameAddress + "</textarea></td></tr>");
		Response.Write("\r\n</table>");

		Response.Write("\r\n</td><td>");

		//cheque table
		Response.Write("\r\n<table align=right>");
		Response.Write("<tr><td align=right><b>Payment Type : </b></td>");
		Response.Write("<td>");
		Response.Write("<select name=payment_type onchange=");
		Response.Write(" \"if(this.options[this.selectedIndex].value == '2')");
		Response.Write("document.f.payment_ref.value=" + m_nextChequeNumber + "; ");
		Response.Write("else document.f.payment_ref.value=document.f.payment_ref_old.value;");
		Response.Write("\">");
		Response.Write(GetEnumOptions("payment_method", m_paymentType));
		Response.Write("</select></td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Reference : </b></td>");
		Response.Write("<td><input type=text name=payment_ref style='text-align:right' value='" + m_paymentRef + "'");
		Response.Write(" onchange=\"document.f.payment_ref_old.value=this.value;\">");
		Response.Write("<input type=hidden name=payment_ref_old></td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Date : </b></td><td><input type=text name=date readonly bgcolor='#EEEEE' ");
	//	Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=f.date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
		Response.Write(" value='" + DateTime.Now.ToString("dd/MM/yyyy") + "' style='text-align:right'></td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Amount : </b></td><td>");
		Response.Write("\r\n<input type=text name=amount ");
		if(m_bPayAll)
			Response.Write(" value=" + dTotal.ToString());
		Response.Write(" onclick=\"if(this.value=='' || this.value=='0')this.value=" + dTotal + ";\" ");
		Response.Write(" onchange=\"ClearAllFields();\" style='text-align:right'></td></tr>");
//		Response.Write("<script");
//		Response.Write(">OnClickAmount(");
		Response.Write("\r\n</table>");

		Response.Write("\r\n</td></tr></table>");
	}
	//end of supplier & cheque table

	Response.Write("\r\n</td></tr><tr><td>");

	//main list
	Response.Write("\r\n<table width=100% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("\r\n<td>DATE</td>\r\n");
	Response.Write("\r\n<td>P.O.NUMBER</td>\r\n");
	Response.Write("\r\n<td>INVOICE NUMBER</td>");
	Response.Write("\r\n<td>STATUS</td>");
//	Response.Write("<td>Supplier InvNo.</td>\r\n");
	Response.Write("\r\n<td>TOTAL AMOUNT</td>");
	Response.Write("\r\n<td>TOTAL OWED</td>");
	Response.Write("\r\n<td align=center>APPLIED AMOUNT</td>");
	Response.Write("</tr>\r\n");

	Response.Write(sb.ToString());
	Response.Write("</table>");
	
	Response.Write("<br><br>");

	Response.Write("</td></tr><tr bgcolor=#EEEEE><td>");


	double dzero = 0;
	//paid table and buttons
	Response.Write("<table width=100%>");
	Response.Write("<tr ><td rowspan=5><b>Note : </b><br>");
	Response.Write("<textarea name=note rows=5 cols=80></textarea></td></tr>");	

	Response.Write("<tr><td colspan=2 align=right><b>Total Applied : </b><input type=text ");
	Response.Write(" name=total_applied readonly=true value='");
	if(m_bPayAll)
		Response.Write(dTotal.ToString());
	Response.Write("' style='text-align:right'></td></tr>");

	Response.Write("<tr><td colspan=2 align=right><b>Currency Loss : </b><input type=text name=currency_loss value='");
	if(m_bPayAll)
		Response.Write(dzero.ToString());
	Response.Write("' style='text-align:right' onclick=\"this.value=0-document.f.out_of_balance.value;CalcTotal();\" onchange=\"CalcTotal();\"></td></tr>");

	Response.Write("<tr><td colspan=2 align=right><b>Finance Charge : </b><input type=text name=finance value='");
	if(m_bPayAll)
		Response.Write(dzero.ToString());
	Response.Write("' style='text-align:right' onclick=\"this.value=document.f.out_of_balance.value;CalcTotal();\" onchange=\"CalcTotal();\"></td></tr>");
//	Response.Write("<tr><td colspan=2 align=right><b>Total Paid : </b><input type=text name=total_paid value='");
//	if(m_bPayAll)
//		Response.Write(dTotal.ToString());
//	Response.Write("' style='text-align:right'></td></tr>");

	Response.Write("<tr><td colspan=2 align=right><b>Out Of Balance : </b><input type=text name=out_of_balance value='");
	if(m_bPayAll)
		Response.Write(dzero.ToString());
	Response.Write("' style='text-align:right'></td></tr>");
	
	//notice field
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<table id=notice style='visibility:hidden'>");
	Response.Write("<tr><td align=right>");
	Response.Write("<font color=red><i>You cannot record untill this blanace is zero<i></font>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr>");

//	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
//	Response.Write("<tr><td colspan=2><b>Note : </b></td></tr>");
//	Response.Write("<tr><td colspan=2><textarea name=note rows=3 cols=50></textarea></td></tr>");
if(PurchaesRows > 0) // bill payment 
{
	Response.Write("<tr><td align=right colspan=3><button " + Session["button_style"] + " ");
	Response.Write(" onclick=\"if(confirm('This will PAY ALL Purchase Invoices to All Suppliers')){window.location=('suppay.aspx?t=payall&id=" + m_supplierID + "');}else{return false;}\">Pay All</button>");
//	Response.Write("<td align=right>");
	Response.Write("<input type=checkbox name=confirm_record>Tick to record ");
	Response.Write("<input type=submit name=cmd value=Record " + Session["button_style"] + ">");
	Response.Write("<input type=button " + Session["button_style"]);
	Response.Write(" value=Cancel onclick=window.location=('suppay.aspx')>");
	Response.Write("</td></tr>");
}
	Response.Write("</table>");

	Response.Write("</td></tr>");

	Response.Write("</table>");

	Response.Write("\r\n<input type=hidden name=invoice_number value='" + m_inv + "'>\r\n");
	//buttons
	Response.Write("</form>");

	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
//	Response.Write("function OnClickAppliedAmount(amout){");
	Response.Write("function CalcTotal()");
	Response.Write("{ var total = 0;");
	for(int i=0; i<dst.Tables["purchase"].Rows.Count; i++)
		Response.Write("	total += Number(document.f.applied_amount" + i.ToString() + ".value);\r\n");
	Response.Write("	total = Math.round(total * 100) / 100;\r\n");

	Response.Write("var amount = Number(document.f.amount.value);");
	Response.Write("var finance = Number(document.f.finance.value);");
	Response.Write("var currency_loss = Number(document.f.currency_loss.value);");
//	Response.Write("var credit = Number(document.f.credit.value);");
	Response.Write("var balance = amount - total - finance + currency_loss;");
	Response.Write("balance = Math.round(balance * 100) / 100;\r\n");
	Response.Write("	document.f.total_applied.value=total;\r\n");
	Response.Write("	document.f.out_of_balance.value=balance;\r\n");
	Response.Write("if(balance != 0){document.all('notice').style.visibility='visible';document.f.cmd.style.visibility='hidden';}");
	Response.Write("else{document.all('notice').style.visibility='hidden';document.f.cmd.style.visibility='visible';}");
	
	
	Response.Write("}\r\n");

	Response.Write("function CalcApplyLeft(){\r\n");
	Response.Write("	var total = Number(document.f.amount.value);\r\n");
	Response.Write("	var left = total;\r\n");
	for(int i=0; i<dst.Tables["purchase"].Rows.Count; i++)
		Response.Write("	left -= Number(document.f.applied_amount" + i.ToString() + ".value);\r\n");
	Response.Write("	return Math.round(left * 100) / 100;\r\n");
	Response.Write("}\r\n");

	Response.Write("function ClearAllFields(){\r\n");
	for(int i=0; i<dst.Tables["purchase"].Rows.Count; i++)
		Response.Write("	document.f.applied_amount" + i.ToString() + ".value = '';\r\n");
	Response.Write("	CalcTotal();\r\n ");
	Response.Write("}\r\n");
	Response.Write("</script");
	Response.Write(">");

	return true;
}

string GetNextChequeNumber()
{
	string s = GetSiteSettings("next_cheque_number");
	return s;
}
</script>
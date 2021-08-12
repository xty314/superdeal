<script runat=server>

int page = 1;
const int m_nPageSize = 15; //how many rows in oen page

int m_custID = 0;

DataSet dsc = new DataSet();
//int m_supplierID = 0;
string m_sCustomerList = "";
string m_sCustomerNameAddress = "";
string m_sCustAddr = "";
string m_account = "";
string m_inv = "";
string m_paymentType = "1";
string m_paidby = "";
string m_reference = "";
string m_amount = "";
string m_bank = "";
string m_branch = "";
string m_banknew = "";
string m_branchnew = "";

string m_invoice = "";

string m_tranid = ""; //store procedure return value

//string[] s_amount = new string[64];
string tableWidth = "97%";

bool m_bPayAll = false;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Request.Form["invoice"] != null && Request.Form["invoice"] != "" || Request.Form["cmd"] == "Search INV#")
		m_invoice = Request.Form["invoice"];

	if(Request.QueryString["recorded"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Done. Transaction Recorded !</h3><br><br><br><br>");

//		Response.Write("<input type=button onclick=window.location=('pos_retail.aspx') value='New Sales' " + Session["button_style"] + ">");
		Response.Write("<input type=button onclick=window.location=('custpay.aspx') value='Receive More' " + Session["button_style"] + ">");
/*			Response.Write("<form action=custpay.aspx method=post>");
		Response.Write("<font color=red>Any thing wrong? click Roll Back to undo this transaction</font><br><br>");
		Response.Write("<input type=submit name=cmd " + Session["button_style"] + " value='Roll Back'>");
		Response.Write("<input type=hidden name=tran_id value=\"" + m_tranid + "\">");
		Response.Write("<input type=hidden name=customer_id value=\"" + m_custID + "\">");
		Response.Write("<input type=hidden name=payment_type value=\"" + m_paymentType + "\">");
		Response.Write("<input type=hidden name=reference value=\"" + m_reference + "\">");
		Response.Write("<input type=hidden name=paid_by value=\"" + m_paidby + "\">");
		Response.Write("<input type=hidden name=bank value=\"" + m_bank + "\">");
		Response.Write("<input type=hidden name=branch value=\"" + m_branch + "\">");
		Response.Write("<input type=hidden name=bank_new value=\"" + m_banknew + "\">");
		Response.Write("<input type=hidden name=branch_new value=\"" + m_branchnew + "\">");
		Response.Write("<input type=hidden name=amount value=\"" + m_amount + "\">");
		Response.Write("<input type=hidden name=credit_id value=" + Request.Form["credit_id"] + ">");
		Response.Write("</form>");
*/		
		Session["ss_invoice"] = null;
		LFooter.Text = m_sAdminFooter;
		return;
//			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=custpay.aspx?id=" + m_custID + "\">");
	}

	if(Request.Form["cmd"] != null
		&& Request.Form["cmd"] != "Continue" 
		&& Request.Form["cmd"] != "Search" 
		&& Request.Form["cmd"] != "Apply" 
		&& Request.Form["cmd"] != "Roll Back")
	{
		if(Session["in_custpay"] == null)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Stop! Transaction already recorded!");
			Response.Write("<br><br><a title='back to Receive Money' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"' class=o><< Back to Receive Money</a>");
			return;
		}
		if(Request.Form["cmd"] != "Record")
			Session["in_custpay"] = null;
	}
	else
		Session["in_custpay"] = true;
	if(Request.Form["cmd"] == "Roll Back")
	{
		m_tranid = Request.Form["tran_id"];
		if(!RollBackTranscation(m_tranid))
			return;
	}
	
	if(Request.QueryString["pt"] == "s")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		LoadCustomerList();
		LFooter.Text = m_sAdminFooter;
		return;
	}

	if(Request.QueryString["t"] == "payall")
		m_bPayAll = true;

	string id = Request.QueryString["id"];
	if(id != null && id != "")
	{
		m_custID = MyIntParse(id);
		if(Request.QueryString["nc"] == null && HaveCredit())
			PrintCreditPaymentForm();
		else
			PrintPaymentForm();
		return;
	}
	else
	{
		if(Request.Form["customer_id"] != null && Request.Form["customer_id"] != "")
		{
			if( IsInteger(Request.Form["customer_id"]) )
			{
				m_custID = MyIntParse(Request.Form["customer_id"]);
				m_paymentType = Request.Form["payment_type"];
				m_reference = Request.Form["reference"];
				m_paidby = Request.Form["paid_by"];
				m_amount = Request.Form["amount"];
				m_bank = Request.Form["bank"];
				m_banknew = Request.Form["bank_new"];
				m_branch = Request.Form["branch"];
				m_branchnew = Request.Form["branch_new"];
			}
		}
	}

	if(Request.Form["amount"] == null || Request.Form["amount"] == "")
	{
		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=custpay.aspx?pt=s&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
//	else if(Session[m_sCompanyName + "cust_payment"] != null)
//		m_custID = (int)Session[m_sCompanyName + "cust_payment"];
//	else
//		m_custID = 1020; //Default card_id for "cash sales"

	if(Request.QueryString["acc"] != null)
	{
		m_account = Request.QueryString["acc"];
		if(!SetSiteSettings("purchase_account", m_account))
			return;
	}
	
	GetCustomerDetails();

	if(!DoSearch())
		return;
	if(Request.Form["cmd"] == "Record")
	{
		if(Session["in_custpay"] == null)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3><font color=red>STOP!</font> Repost Form is forbidden");
			PrintAdminFooter();
			return;
		}
		Session["in_custpay"] = null;
		if(RecordTransaction())
		{
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=custpay.aspx?recorded=1\">");
			return;
		}
	}
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
		MyDrawTable();
		Session["in_custpay"] = true;
	}
	LFooter.Text = m_sAdminFooter;
}

bool HaveCredit()
{
	string sc = "SELECT c.*, d.trans_date, e.name AS payment_method ";
	sc += " FROM credit c JOIN tran_detail d ON d.id = c.tran_id ";
	sc += " JOIN enum e ON (e.id = d.payment_method AND e.class = 'payment_method') ";
	sc += " WHERE c.card_id = " + m_custID + " AND c.amount > c.amount_applied ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "credit") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

bool PrintCreditPaymentForm()
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h4>This customer has credit, apply to sales? ");
	Response.Write("<input type=button value='No, leave it' onclick=window.location=('custpay.aspx?id=" + m_custID + "&nc=1') " + Session["button_style"] + ">");
	Response.Write("</h4>");

	Response.Write("<table align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Credit_Received &nbsp&nbsp;</th>");
	Response.Write("<th>Payment &nbsp&nbsp;</th>");
//	Response.Write("<th>Reference &nbsp&nbsp;</th>");
	Response.Write("<th>Amount &nbsp&nbsp;</th>");
	Response.Write("<th>Amount_Applied &nbsp&nbsp;</th>");
	Response.Write("<th>Credit_Left &nbsp&nbsp;</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	int rows = dst.Tables["credit"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return true;
	}

	string method_credit = GetEnumID("payment_method", "credit apply");

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["credit"].Rows[i];
		string id = dr["id"].ToString();
		string payment_method = Capital(dr["payment_method"].ToString());
		string date = dr["trans_date"].ToString(); //DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy");
//	DEBUG("date =", date);
//		string date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy");
		double dAmount = MyDoubleParse(dr["amount"].ToString());
		double dAmount_applied = MyDoubleParse(dr["amount_applied"].ToString());
		double dCredit = dAmount - dAmount_applied;
		
		Response.Write("<form action=custpay.aspx method=post>");
		Response.Write("<input type=hidden name=customer_id value=" + m_custID + ">");
		Response.Write("<input type=hidden name=payment_type value=" + method_credit + ">");
		Response.Write("<input type=hidden name=credit_id value=" + id + ">");
//DEBUG("ID " , id);
		Response.Write("<input type=hidden name=amount value=" + dCredit + ">");
		Response.Write("<input type=hidden name=reference value='" + id + "'>");

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");

		Response.Write("<td>" + date + " &nbsp&nbsp;</td>");
		Response.Write("<td>" + payment_method + " &nbsp&nbsp;</td>");
		Response.Write("<td>" + dAmount.ToString("c") + " &nbsp&nbsp;</td>");
		Response.Write("<td> &nbsp&nbsp;" + dAmount_applied.ToString("c") + "</td>");
		Response.Write("<td>" + dCredit.ToString("c") + " &nbsp&nbsp;</td>");
		Response.Write("<td><input type=submit name=cmd value=Apply " + Session["button_style"] + "></td>");
		Response.Write("</tr>");

		Response.Write("</form>");
	}
	Response.Write("</table>");
	return true;
}

bool PrintPaymentForm()
{
	if(!GetCustomerDetails())
		return false;

	DataRow dr = dst.Tables["card"].Rows[0];
	string customer = dr["trading_name"].ToString();
	
	if(customer == "")
		customer = dr["name"].ToString();
	string balance = MyDoubleParse(dr["total_balance"].ToString()).ToString("c");
	//if(Session["ss_invoice"] != null && Session["ss_invoice"] != "")
	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			balance = MyDoubleParse(dr["total"].ToString()).ToString("c");
	PrintAdminHeader();
	PrintAdminMenu();
	
//	Response.Write("<br><center><h3>Customer Payment</h3>");
	
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width=50% valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Customer Payment</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	

Response.Write("<form action='custpay.aspx?r="+ DateTime.Now.ToOADate() +"");
		
	if(m_invoice != "")
		if(TSIsDigit(m_invoice))
			Response.Write("&i="+ m_invoice +"");
	Response.Write("' method=post>");
	Response.Write("<input type=hidden name=customer_id value=" + m_custID + ">");
//	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<table align=center width=50%  valign=center cellspacing=3 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td valign=top><font color=red><b>" + customer + "</b></font></td><td><b>Balance : " + balance + "<br>&nbsp;</b></td></tr>");
	
	//payment method
	string sEscapeCreditApplyID = GetEnumID("payment_method", "credit apply");
    if(sEscapeCreditApplyID == null || sEscapeCreditApplyID == "")
    sEscapeCreditApplyID = "7"; 
			
	Response.Write("<tr><td><b>Payment Method </b></td>");
	Response.Write("<td>");
	Response.Write("<select name=payment_type>");
	//Response.Write(GetEnumOptions("payment_method", m_paymentType));
	Response.Write(GetEnumOptions("payment_method", m_paymentType, false, true, "", false, sEscapeCreditApplyID));
	Response.Write("</select></td></tr>");
	
	Response.Write("<tr><td><b>Reference / Cheque # </b></td><td><input type=text name=reference></td></tr>\r\n");
	Response.Write("<tr><td><b>Paid By </b></td><td><input type=text maxlength=49 name=paid_by></td></tr>\r\n");
	Response.Write("<tr><td><b>Bank </b></td><td><select name=bank>");
	Response.Write(GetBankOptions(""));
	Response.Write("</select><input type=text size=10 name=bank_new maxlength=49></td></tr>\r\n");

	Response.Write("<tr><td><b>Branch </b></td><td><select name=branch>");
	Response.Write(GetBranchOptions(""));
	Response.Write("</select><input type=text size=10 name=branch_new maxlength=49></td></tr>\r\n");
	//Response.Write("<tr><td><b>Amout</b></td><td><input type=text name=amount value='" + Request.QueryString["amount"] + "'></td></tr>");
	Response.Write("<tr><td><b>Amout</b></td><td><input type=text name=amount value='" +  Math.Round(MyDoubleParse(dr["total_balance"].ToString()),2) + "'></td></tr>");
	Response.Write("<tr><td><b>Apply To Invoices</b></td><td>");
	Response.Write("<input type=radio name=date_range value=0 checked>All ");
	Response.Write("<input type=radio name=date_range value=1>This Month ");
	Response.Write("<input type=radio name=date_range value=2>1 Month Ago ");
	Response.Write("<input type=radio name=date_range value=3>2 Months Ago ");
	Response.Write("<input type=radio name=date_range value=4>3 Months Ago ");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd " + Session["button_style"] + " value=Continue></td></tr>");

	Response.Write("</table></form>");

	PrintAdminFooter();
	return true;
}

string GetBankOptions(string current)
{
	string s = "<option value=''></option>";
	string sc = " SELECT DISTINCT bank FROM tran_detail WHERE bank<>'' ORDER BY bank";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "bank");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return s;
	}
	for(int i=0; i<dst.Tables["bank"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["bank"].Rows[i];
		string bank = dr["bank"].ToString();
		s += "<option value=\"" + bank + "\" ";
		if(bank == current)
			s += " selected";
		s += ">" + bank + "</option>";
	}
	return s;
}

string GetBranchOptions(string current)
{
	string s = "<option value=''></option>";
	string sc = " SELECT DISTINCT branch FROM tran_detail WHERE branch<>'' ORDER BY branch";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return s;
	}
	for(int i=0; i<dst.Tables["branch"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["branch"].Rows[i];
		string branch = dr["branch"].ToString();
		s += "<option value=\"" + branch + "\" ";
		if(branch == current)
			s += " selected";
		s += ">" + branch + "</option>";
	}
	return s;
}

bool RecordTransaction()
{
	string payment_method = Request.Form["payment_type"];//GetEnumID("payment_method", "cheque");
	string sAmount = Request.Form["amount"];
	string sTotalApplied = Request.Form["total_applied"];
	double dAmountForCardBalance = 0;
	double dCreditApplied = 0;
	if(m_paymentType == "7") //GetEnumID("payment_method", "credit apply")
		dCreditApplied = MyMoneyParse(sTotalApplied) + MyMoneyParse(Request.Form["finance"]);
	else
		dAmountForCardBalance = MyMoneyParse(sAmount) - MyMoneyParse(Request.Form["finance"]);


	m_bank = Request.Form["bank"];
	if(m_banknew != "")
		m_bank = m_banknew;
	if(m_branchnew != "")
		m_branch = m_branchnew;

	m_bank = EncodeQuote(m_bank);
	m_branch = EncodeQuote(m_branch);

	string invoices = "";
	string amountList = "";

	for(int i=0; i<dst.Tables["cust_invoice"].Rows.Count; i++)
	{
		string s_sAmount = "applied_amount" + i.ToString();
		double d_balance = MyDoubleParse(dst.Tables["cust_invoice"].Rows[i]["balance"].ToString());
		d_balance = Math.Round(d_balance, 2);

		double d_applied = 0;
		if(Request.Form[s_sAmount] != null && Request.Form[s_sAmount].ToString() != "")
		{
			d_applied = MyDoubleParse(Request.Form[s_sAmount].ToString());
			d_applied = Math.Round(d_applied, 2);
		}

		//record applied invoice number and amount applied to each
		if(d_applied != 0 || d_balance == 0)
		{
			/*if(i > 0){
				if(i == 1){ 
					invoices += ","; amountList += ", "; 
				}
			invoices += Request.Form["invoice_number" + i.ToString()] + ",";			
			amountList += Request.Form[s_sAmount] + ",";
			}
			else{ */

				invoices += Request.Form["invoice_number" + i.ToString()] + ",";			
				amountList += Request.Form[s_sAmount] + ",";
			//}

		}
	}	
///DEBUG("invoices =", invoices);	
//DEBUG("amountList =", amountList);
//return false;
	//do transaction
	SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;
	if(Request.Form["credit_id"] != null)
	{
		myCommand.Parameters.Add("@amount", SqlDbType.Money).Value = dCreditApplied;
		myCommand.Parameters.Add("@credit_id", SqlDbType.Int).Value = Request.Form["credit_id"];
		myCommand.Parameters.Add("@credit_applied", SqlDbType.Money).Value = dCreditApplied;
	}
	else
	{
		myCommand.Parameters.Add("@amount", SqlDbType.Money).Value = Request.Form["amount"];
		myCommand.Parameters.Add("@paid_by", SqlDbType.VarChar).Value = m_paidby;
		myCommand.Parameters.Add("@bank", SqlDbType.VarChar).Value = m_bank;
		myCommand.Parameters.Add("@branch", SqlDbType.VarChar).Value = m_branch;
		myCommand.Parameters.Add("@nDest", SqlDbType.Int).Value = Request.Form["account"];
		myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = dAmountForCardBalance;
	}
	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = m_custID.ToString();
	myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = Request.Form["payment_type"];
	myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = invoices;
	myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = Request.Form["reference"];
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = Request.Form["note"];
	myCommand.Parameters.Add("@finance", SqlDbType.Money).Value = Request.Form["finance"];
	myCommand.Parameters.Add("@credit", SqlDbType.Money).Value = Request.Form["credit"];
	myCommand.Parameters.Add("@bRefund", SqlDbType.Bit).Value = 0;
	myCommand.Parameters.Add("@amountList", SqlDbType.VarChar).Value = amountList;
	myCommand.Parameters.Add("@payment_date", SqlDbType.VarChar).Value = Request.Form["date"];
	myCommand.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoCustomerPayment", e);
		return false;
	}
	m_tranid = myCommand.Parameters["@return_tran_id"].Value.ToString();
//DEBUG(" m =", m_tranid);

	return true;
}

bool RollBackTranscation(string id)
{
	//do transaction
	SqlCommand myCommand = new SqlCommand("eznz_payment_rollback", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;
	myCommand.Parameters.Add("@tran_id", SqlDbType.Int).Value = MyIntParse(id);
	myCommand.Parameters.Add("@return_status", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoCustomerPayment", e);
		return false;
	}
	string return_status = myCommand.Parameters["@return_status"].Value.ToString();
	if(return_status != "0")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, " + return_status);
		LFooter.Text = m_sAdminFooter;
		return false;
	}
	return true;
}	

bool GetCustomerDetails()
{
//	if(m_custID == 0)
//		return true;
	m_invoice = Request.QueryString["i"];
	bool bValidInvoice = false;
	//if(Session["ss_invoice"] != null && Session["ss_invoice"] != "")
	//	if(TSIsDigit(Session["ss_invoice"].ToString()))
	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			bValidInvoice = true;
	int rows = 0;
	string sc = "SELECT c.* ";
	
	if(bValidInvoice)
		sc += " , (i.total-i.amount_paid) AS total ";

	sc += ", ISNULL((SELECT SUM(total - amount_paid) ";
	sc += " FROM invoice ";
	sc += " WHERE card_id = c.id AND paid = 0), 0) ";
	sc += " - ";
	sc += " ISNULL((SELECT SUM(amount - amount_applied) ";
	sc += " FROM credit ";
	sc += " WHERE card_id = c.id), 0) ";
	sc += " AS total_balance ";

	sc += " FROM card c ";
	if(bValidInvoice)
		sc += " JOIN invoice i ON i.card_id = c.id ";

	sc += " WHERE c.id = " + m_custID;
	
	if(bValidInvoice)
		sc += " AND i.invoice_number = "+ m_invoice;
		//sc += " AND i.invoice_number = "+ Session["ss_invoice"].ToString();
//DEBUG(" sc = ", sc);
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

	DataRow dr = null;
	if(rows > 0 )
	{
		dr= dst.Tables["card"].Rows[0];
		m_sCustAddr = dr["companyB"].ToString()+ "\r\n" + dr["address1B"].ToString() + "\r\n" + dr["address2B"].ToString();
		m_sCustAddr += "\r\n" + dr["cityB"].ToString() + "\r\n" + dr["countryB"].ToString();
		return true;
	}
	return false;
}

bool DoSearch()
{
	string datestr = "";
	if(Request.Form["date_range"] == "1") //this month
		datestr = " AND DATEDIFF(month, commit_date, GETDATE()) = 0 ";
	else if(Request.Form["date_range"] == "2") //1 month ago
		datestr = " AND DATEDIFF(month, commit_date, GETDATE()) >= 1 ";
	else if(Request.Form["date_range"] == "3") //2 month ago
		datestr = " AND DATEDIFF(month, commit_date, GETDATE()) >= 2 ";
	else if(Request.Form["date_range"] == "4") //3 month ago
		datestr = " AND DATEDIFF(month, commit_date, GETDATE()) >= 3 ";

	//get credit and refund first
	string sc = " SELECT commit_date, cust_ponumber, invoice_number, total, amount_paid, (ROUND(total,2)-ROUND(amount_paid,2)) AS balance, paid, branch ";
	sc += " FROM invoice WHERE card_id=" + m_custID;
	sc += datestr;
	//if(Session["ss_invoice"] != null && Session["ss_invoice"] != "")
	//	if(TSIsDigit(Session["ss_invoice"].ToString()))
	//		sc += " AND invoice_number = "+ Session["ss_invoice"].ToString();
	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			sc += " AND invoice_number = "+ m_invoice;
	sc += " AND paid=0 AND ROUND(total,2)-ROUND(amount_paid,2) <= 0 ORDER BY commit_date "; //paid=0 - not fully paid, type=3 - invoice
//DEBUG("credit =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "cust_invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//all rest unpaid
	sc = " SELECT commit_date, cust_ponumber, invoice_number, total, amount_paid, (ROUND(total,2)-ROUND(amount_paid,2)) AS balance, paid ";
	sc += " FROM invoice WHERE card_id=" + m_custID;
	sc += datestr;
	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			sc += " AND invoice_number = "+ m_invoice;
	sc += " AND paid=0 AND ROUND(total,2) - ROUND(amount_paid,2) > 0 ORDER BY commit_date ";
//DEBUG("unpaid =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "cust_invoice");
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
		m_account = GetSiteSettings("income_account");

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
	Response.Write("<tr><td><b>To : ");
	Response.Write("Undeposit Account<input type=hidden name=account value=1116>");
/*
	Response.Write("<select name=account onchange=\"window.location=");
	Response.Write("('custpay.aspx?acc='+this.options[this.selectedIndex].value)\">");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["account"].Rows[i];
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		Response.Write("<option value=" + number);
		if(number == m_account)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString() + double.Parse(dr["balance"].ToString()).ToString("c"));		
	}
	Response.Write("</select>");
*/
	Response.Write("</td></tr></table>");
	return true;
}

bool MyDrawTable()
{
	Response.Write("<form name=f action=custpay.aspx method=post>");
/*	Response.Write("<br><center><h3>Customer Payment</h3></center>");	
	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
*/
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Customer Payment</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td>");

	//account list table
	if(m_paymentType != "7") //GetEnumID("payment_method", "credit apply")
	{
		if(!PrintAccountList())
			return false;
	}
//	else
//	{
//		Response.Write("<input type=hidden name=account value=1116>");
//	}

	Response.Write("</td></tr><tr><td>");

	double dTotal = 0;
	double dTotalApplied = 0;
	StringBuilder sb = new StringBuilder();

	double dApplied_left = MyDoubleParse(Request.Form["amount"]);
	
	int match = -1;
	//check specified payment first, try to find exact amount value
	for(int i=0; i<dst.Tables["cust_invoice"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["cust_invoice"].Rows[i];
		string balance = dr["balance"].ToString();
		if(balance == "")
			continue;
		double dAmount = double.Parse(balance);
		dAmount = Math.Round(dAmount, 2);
		if(dAmount == dApplied_left)
			match = i;
	}
	for(int i=0; i<dst.Tables["cust_invoice"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["cust_invoice"].Rows[i];
		string date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd/MM/yyyy");
		string po_number = dr["cust_ponumber"].ToString();
		string inv_number = dr["invoice_number"].ToString();
		string amount_paid = dr["amount_paid"].ToString();
		string inv_total = dr["total"].ToString();
		string balance = dr["balance"].ToString();
		double dAmount = 0;
		if(balance != "")
		{
			dAmount = double.Parse(balance);
			dAmount = Math.Round(dAmount, 2);
		}
		double dAmountPaid = MyDoubleParse(amount_paid);
		dAmountPaid = Math.Round(dAmountPaid, 2);
		double dInvTotal = MyDoubleParse(inv_total);
		dInvTotal = MyCurrencyPrice(dInvTotal.ToString("c"));
		//dInvTotal = Math.Round(dInvTotal, 2);

		//string status = GetEnumValue("general_status", dr["status"].ToString());
		sb.Append("\r\n<tr>");
		sb.Append("\r\n<td>" + date + "</td>");
		sb.Append("\r\n<td><a href=invoice.aspx?n=" + inv_number + " target=_blank>" + inv_number + "</a></td>");
		sb.Append("\r\n<td>" + po_number + "</td>");
		sb.Append("\r\n<td>" + dInvTotal.ToString("c") + "</td>");
		sb.Append("\r\n<td>" + dAmountPaid.ToString("c") + "</td>");
		sb.Append("\r\n<td>" + dAmount.ToString("c") + "</td>");
		sb.Append("\r\n<td align=right>\r\n<input type=text name=applied_amount" + i.ToString());
		sb.Append(" onchange=\"CalcTotal();\" ");
//		sb.Append(" onchange=\"OnAppliedClick(" + i.ToString() + ", " + Number(this.value) + ");\" ");

//		sb.Append(" onclick=\"if(this.value=='')this.value=" + dAmount);
//		sb.Append(";CalcTotal();\" onchange=\"CalcTotal()\"");
		if(m_bPayAll)
			sb.Append(" value=" + dAmount.ToString());
		if(match == i)
		{
			double apply = dAmount;
			if(dApplied_left < dAmount)
				apply = dApplied_left;
			sb.Append(" value=" + apply);
			dApplied_left -= apply;
			dTotalApplied += apply;
		}
		else if(match == -1)
		{
			double apply = dAmount;
			if(dApplied_left < dAmount)
				apply = dApplied_left;
			sb.Append(" value=" + apply);
			dApplied_left -= apply;
			dTotalApplied += apply;
		}

		sb.Append(" style='text-align:right'>");//<input type=hidden name=s_amount" + i.ToString() + " value='" + dAmount.ToString() + "'></td>\r\n");
		sb.Append("<input type=hidden name=invoice_number" + i.ToString() + " value=" + inv_number + ">");
		sb.Append("</tr>\r\n");
		dTotal += dAmount;
//		if(m_inv != "")
//			m_inv += ",";
//		m_inv += inv_number;

//DEBUG("dAmount = ", d
	}
	
	//customer & reference table
	{
		Response.Write("\r\n<table width=100%>");
		Response.Write("\r\n<tr><td>");

		string customer = dst.Tables["card"].Rows[0]["trading_name"].ToString();
		if(customer == "")
			customer = dst.Tables["card"].Rows[0]["name"].ToString();
		//customer list table
		Response.Write("\r\n<table>");

		Response.Write("\r\n<tr><td><b>Customer : </b></td><td><b>");
		if(m_custID != 0 && dst.Tables["card"].Rows.Count > 0)
			Response.Write(customer);
		else
			Response.Write("Cash Sales");
		Response.Write("</b></td></tr>");
		Response.Write("<input type=hidden name=customer_id value=" + m_custID + ">");

/*		Response.Write("<select name=customer onclick=window.location=('custpay.aspx?pt=s&r=");
		Response.Write(DateTime.Now.ToOADate().ToString() + "')>");
		if(m_custID != 0 && dst.Tables["card"].Rows.Count > 0)
			Response.Write("<option value=1>&nbsp;" + customer + "&nbsp;&nbsp;&nbsp;");
		else
			Response.Write("<option value=1>" + "&nbsp;Cash Sales&nbsp;&nbsp;&nbsp;");
		Response.Write("</select>");
		Response.Write("</td></tr>");
*/
		if(m_paymentType == "7") //GetEnumID("payment_method", "credit apply")
		{
			Response.Write("<tr><td><b>Payment Type : </b></td>");
			Response.Write("<td><font color=red><b>Credit Apply</b></td></tr>");
			Response.Write("<input type=hidden name=payment_type value=7>");
			Response.Write("<input type=hidden name=credit_id value=" + Request.Form["credit_id"] + ">");
		}
		else
		  
		{   
		    string sEscapeCreditApplyID = GetEnumID("payment_method", "credit apply");
            if(sEscapeCreditApplyID == null || sEscapeCreditApplyID == "")
            sEscapeCreditApplyID = "7"; 
			Response.Write("<tr><td><b>Payment Type </b></td>");
			Response.Write("<td>");
			Response.Write("<select name=payment_type>");
		//	Response.Write(GetEnumOptions("payment_method", m_paymentType));
			Response.Write(GetEnumOptions("payment_method", m_paymentType, false, true, "", false, sEscapeCreditApplyID)); 
			Response.Write("</select></td></tr>");
		
			Response.Write("<tr><td><b>Paid By</b></td><td><input type=text name=paid_by value=\"" + m_paidby + "\"></td></tr>\r\n");
			Response.Write("<tr><td><b>Bank </b></td><td><select name=bank>");
			Response.Write(GetBankOptions(m_bank));
			Response.Write("</select><input type=text size=10 name=bank_new maxlength=49 value=\"" + m_banknew + "\"></td></tr>\r\n");
			Response.Write("<tr><td><b>Branch </b></td><td><select name=branch>");
			Response.Write(GetBranchOptions(m_branch));
			Response.Write("</select><input type=text size=10 name=branch_new maxlength=49 value=\"" + m_branchnew + "\"></td></tr>\r\n");
		}
/*
		DataRow drc = null;
		if(m_custID != 0)
		{
			if(GetCustCredits())
			{
				Response.Write("<tr><td>&nbsp;</td></tr>");
				Response.Write("<tr><td><b><font color=red>Credits</font></b></td></tr>");
				Response.Write("<tr><td colspan=5>");
				Response.Write("<table width=100% valign=top cellspacing=1 cellpadding=1");
				Response.Write(" border=1 bordercolor=#000000 bgcolor=white");
				Response.Write(" style=\"font-family:Verdana;font-size:8pt;");
				Response.Write("border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

				Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
				Response.Write("<th>Amount</th>");
				Response.Write("<th>Method</th>");
				Response.Write("<th>Date</th>");
				Response.Write("<th>Ref</th>");
				Response.Write("<th>Use</th>");
				Response.Write("</tr>\r\n");
//DEBUG("rows = ", dsc.Tables["credits"].Rows.Count);
				for(int j=0; j<dsc.Tables["credits"].Rows.Count; j++)
				{
					drc = dsc.Tables["credits"].Rows[j];
//DEBUG("here = ", 0);
//DEBUG("amount = ", drc["amount"].ToString());

					Response.Write("<tr><td align=left><font color=red>"+double.Parse(drc["amount"].ToString()).ToString("c")+"</font></td>");
					Response.Write("<td align=center>");
					Response.Write(GetEnumValue("payment_method", drc["payment_method"].ToString()) + "</td>");
					Response.Write("<td align=center>" + DateTime.Parse(drc["date_paid"].ToString()).ToString("dd-MM-yyyy") + "</td>");
					Response.Write("<td align=left>" + drc["ref"].ToString() + "</td>");
					Response.Write("<td align=center><a href='cuspay.aspx?id=" + m_custID + "&cdi=");
					Response.Write(drc["id"].ToString() + "' class=o>Spend</a></td></tr>\r\n");
				}
				Response.Write("</table></td></tr>");
			}
		}
*/
		//Response.Write("<tr><td><b>Address : </b></td><td><textarea rows=5 cols=30>" + m_sCustAddr + "</textarea></td></tr>");
		Response.Write("\r\n</table>");

		Response.Write("\r\n</td><td valign=bottom>");

		//reference table
		Response.Write("\r\n<table align=right>");
		Response.Write("\r\n<tr><td align=right><b>Reference No. : </b></td>");
		Response.Write("<td><input type=text name=reference style='text-align:right' ");
		if(m_paymentType == "7") //GetEnumID("payment_method", "credit apply")
			Response.Write(" readonly=true ");
		Response.Write(" value='" + m_reference + "' ");
		Response.Write("></td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Payment Date : </b></td><td><input type=text name=date value='" + DateTime.Now.ToString("dd/MM/yyyy") + "' style='text-align:right' ");
		Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=f.date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
		Response.Write("></td></tr>");
		Response.Write("\r\n<tr><td align=right>");
//		Response.Write("<input type=button value='Pay All' " + Session["button_style"] + " onclick=window.location=('custpay.aspx?t=payall&id=" + m_custID + "')> ");
		Response.Write("<b>Amount : </b></td><td>");
		Response.Write("\r\n<input type=text name=amount readonly=true "); // onclick=\"if(this.value=='')this.value=" + dTotal + ";\" ");
		Response.Write(" onchange=\"document.f.applied_left.value=(this.value).toFixed(2); ");
//Response.Write("window.alert('applied_left=' + document.f.applied_left.value); ");
		Response.Write("\" ");
		if(m_bPayAll)
			Response.Write("value=" + dTotal.ToString());
		else
			Response.Write("value=" + m_amount);
		Response.Write(" style='text-align:right'></td></tr>");		
//		Response.Write("<script");
//		Response.Write(">OnClickAmount(");
		Response.Write("\r\n</table>");
Response.Write("<input type=hidden name=applied_left value=" + dApplied_left + ">");
		Response.Write("\r\n</td></tr></table>");
	}
	//end of supplier & cheque table

	Response.Write("\r\n</td></tr><tr><td>");

	//main list
	Response.Write("\r\n<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("\r\n<td>Invoice Date</td>\r\n");
	Response.Write("\r\n<td>Invoice No.</td>\r\n");
	Response.Write("\r\n<td>P.O.#</td>");
	Response.Write("\r\n<td>Invoice Total</td>");
	Response.Write("<td>Amount Paid</td>\r\n");
	Response.Write("\r\n<td>Balance</td>");
	//Response.Write("\r\n<td>Amount to Pay:</td>");
	Response.Write("\r\n<td align=center>Applied Amount</td>");
	Response.Write("</tr>\r\n");

	Response.Write(sb.ToString());
	Response.Write("</table>");
	
	Response.Write("<br><br>");

	Response.Write("</td></tr><tr><td>");

	Response.Write("<table width=100% cellpadding=0 cellspacing=0>");
	Response.Write("<tr bgcolor=#EEEEE><td valign=top>");

	Response.Write("<table>");
	Response.Write("<tr><td colspan=2><b>Note : </b></td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=note rows=5 cols=80></textarea></td></tr>");
	Response.Write("</table>");

	Response.Write("</td><td>");
	double dOutBalance = Math.Round(MyDoubleParse(m_amount) - dTotalApplied, 2);

	//out of balance
	Response.Write("<table width=100%>");
	Response.Write("<tr><td colspan=2 align=right><b>Total Applied : </b><input type=text name=total_applied value='");
	Response.Write(dTotalApplied.ToString());
	Response.Write("' style='text-align:right'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><b>Finance Charge : </b><input type=text name=finance value=0");
	Response.Write(" style='text-align:right' onclick=\"this.value=document.f.out_of_balance.value;CalcTotal();\" onchange=\"CalcTotal();\"></td></tr>");
//	Response.Write("<tr><td colspan=2 align=right><b>Total Paid : </b><input type=text name=total_paid value='");
//	Response.Write(dTotalPaid.ToString());
//	Response.Write("' style='text-align:right'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><b>Credit ");
	if(m_paymentType == "7") //GetEnumID("payment_method", "credit apply")
	{
		Response.Write(" Left : </b><input type=text name=credit value=" + dOutBalance);
		dOutBalance = 0;
	}
	else
	{
		Response.Write(": </b><input type=text name=credit value=0");
	}

	Response.Write(" style='text-align:right' onclick=\"this.value=document.f.out_of_balance.value;CalcTotal();\" onchange=\"CalcTotal();\"></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><b>Out Of Balance : </b><input type=text name=out_of_balance readonly=true value=" + dOutBalance);
	Response.Write(" style='text-align:right'></td></tr>");
	
	//notice field
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<table id=notice style='visibility:hidden'>");
	Response.Write("<tr><td align=right>");
	Response.Write("<font color=red><i>You cannot record untill this blanace is zero<i></font>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	
	Response.Write("</td></tr>");

//	Response.Write("<tr><td><button onclick=window.location=('custpay.aspx?t=payall&id=" + m_custID + "')>Pay All</button></td>");
	Response.Write("<tr bgcolor=#EEEEE><td colspan=2 align=right><input type=submit name=cmd value=Record " + Session["button_style"]);
	Response.Write(">");
	Response.Write("<input type=button " + Session["button_style"]);
	Response.Write(" value=Cancel onclick=window.location=('custpay.aspx?pt=s&r=" + DateTime.Now.ToOADate() + "')>");
	Response.Write("</td></tr>");

	Response.Write("</table>");

	Response.Write("</td></tr></table>");

//	Response.Write("\r\n<input type=hidden name=invoice_number value='" + m_inv + "'>\r\n");
	//buttons
	Response.Write("</form>");

	PrintJavaFunction();

	return true;
}

bool GetCustCredits()
{
	int rows = 0;
	string sc = " SELECT * FROM credit WHERE card_id = " + m_custID;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
	    rows = myAdapter.Fill(dsc, "credits");
//DEBUG("sc = ", sc);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <=0)
		return false;

	return true;
}

void LoadCustomerList()
{
	string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	Response.Write("<form id=search name=search action=" + uri + " method=post>");
//	Response.Write("<br><br><center><h3><font size=+1><b>Customer Payment</b></font></h3>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Customer Payment</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	

	
	if(m_sSite == "admin")
	{		
		Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

		Response.Write("<tr><td width=15% align=left nowrap><b>SELECT CUSTOMER :&nbsp;<td width=10%>");
		Response.Write("<input type=editbox size=20 name=ckw></td><td>");

		Response.Write("<script");
		Response.Write(">");
		Response.Write("document.search.ckw.focus();");
		Response.Write("</script");
		Response.Write(">");

		Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
//		Response.Write("<input type=button name=cmd value='Cash Sales' " + Session["button_style"] + " ");
//		Response.Write(" onClick=window.location=('custpay.aspx?id=0&r=" + DateTime.Now.ToOADate() + "')>");
		Response.Write("<input type=button name=cmd value='Show All' " + Session["button_style"]);
		Response.Write(" onClick=window.location=('custpay.aspx?pt=s&r=" + DateTime.Now.ToOADate() + "')>");
		Response.Write("</td></tr>");
		Response.Write("<tr><td align=left><b>");
		Response.Write("SEARCH BY INVOICE# :</td><td><input type=text name=invoice>");
		Response.Write("</td><td><input type=submit name=cmd value='Search INV#' "+ Session["button_style"] +">");
		Response.Write("</td></tr></table></form>\r\n");

		if(!DoCustomerSearchAndList())
			return;
	}
}

bool DoCustomerSearchAndList()
{
	string uri = Request.ServerVariables["URL"] + "?";	// + Request.ServerVariables["QUERY_STRING"];
	int rows = 0;
	string kw = Request.Form["ckw"];
	if(kw != null && kw != "")
		kw = "'%" + EncodeQuote(kw) + "%'";
//	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%%'";
	kw = kw.ToLower();

	string s_random = DateTime.Now.ToOADate().ToString();
	string sc = "SELECT c.*, '" + uri + "' + '&id='+ LTRIM(STR(c.id)) + '&r=" + DateTime.Now.ToOADate() + "' AS uri ";

	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			sc += " ,  i.invoice_number ";

	sc += ", ISNULL((SELECT SUM(total - amount_paid) ";
	sc += " FROM invoice ";
	sc += " WHERE card_id = c.id AND paid = 0), 0)  ";
	sc += " - ";
	sc += " ISNULL((SELECT SUM(amount - amount_applied) ";
	sc += " FROM credit ";
	sc += " WHERE card_id = c.id), 0) ";
	sc += " AS total_balance ";
	sc += " FROM card c ";
	
	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			sc += " JOIN invoice i ON c.id = i.card_id ";

	sc += " WHERE c.main_card_id is null AND c.type<>3 "; //type 3: supplier;
	if(IsInteger(Request.Form["ckw"]))
		sc += " AND (c.id=" + Request.Form["ckw"] + ") ";
	else if(Request.Form["ckw"] != null && Request.Form["ckw"] != "")
		sc += " AND (c.name LIKE " + kw + " OR c.email LIKE " + kw + " OR c.trading_name LIKE " + kw + ")";
	if(m_invoice != null && m_invoice != "")
		if(TSIsDigit(m_invoice))
			sc += " AND i.invoice_number = "+ m_invoice +" ";
	sc += " ORDER BY total_balance DESC ";
//DEBUG("sc = ",sc);
//	if(m_invoice != null && m_invoice != "" && rows <=0)
//	{
//		Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
//		return false;
//	}
//DEBUG(" sc =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
		if(rows == 1)
		{
			//Session["ss_invoice"] = m_invoice;
			string search_id = dst.Tables["card"].Rows[0]["id"].ToString();
			if(m_invoice != "" && m_invoice != null)
				m_invoice = dst.Tables["card"].Rows[0]["invoice_number"].ToString();
			Trim(ref search_id);
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=custpay.aspx?id=" + search_id + "");
			if(m_invoice != "")
				Response.Write("&i="+ m_invoice +"");
			Response.Write("\">");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	BindGrid();

	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["card"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_PageA(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

void PrintJavaFunction()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	Response.Write("if(document.f.out_of_balance.value != '0'){document.all('notice').style.visibility='visible';document.f.cmd.style.visibility='hidden';}\r\n");

	const string s = @"
	function OnAppliedClick(r, amount)
	{
		var applied = 0;
		var applied_changes = 0;
		var applied_left = Number(document.f.applied_left.value);
		if(Number(eval(" + "\"document.f.applied_amount\" + r + \".value\" " + @")) !=0 ) 
		{
			applied_left += Number(eval(" + "\"document.f.applied_amount\" + r + \".value\" " + @"));
			applied = 0;
		}
		else
		{
			if(applied_left >= amount)
				applied = amount;
			else 
				applied = applied_left;
			applied_left = applied_left - applied;
		}
		applied_changes = applied - Number(eval(" + "\"document.f.applied_amount\" + r + \".value\" " + @")); 
		eval(" + "\"document.f.applied_amount\" + r + \".value=applied;\")" + @"
		document.f.applied_left.value = applied_left; 
		document.f.total_applied.value = Number(document.f.total_applied.value) + applied_changes; 
		document.f.out_of_balance.value = Number(document.f.amount.value) - Number(document.f.total_applied.value) - Number(document.f.credit.value) - Number(document.f.finance_charge.value); 
	}
	";
	
	Response.Write(s);

	Response.Write("\r\nfunction CalcTotal()");
	Response.Write("{ var total = 0;");
	for(int i=0; i<dst.Tables["cust_invoice"].Rows.Count; i++)
		Response.Write("	total += Number(document.f.applied_amount" + i.ToString() + ".value);\r\n");
	Response.Write("	total = Math.round(total * 100) / 100;\r\n");
	Response.Write("var amount = Number(document.f.amount.value);");
	Response.Write("var finance = Number(document.f.finance.value);");
	Response.Write("var credit = Number(document.f.credit.value);");
	Response.Write("var balance = amount - total - credit - finance;");
	Response.Write("balance = Math.round(balance * 100) / 100;\r\n");
	Response.Write("	document.f.total_applied.value=total;\r\n");
	Response.Write("	document.f.out_of_balance.value=balance;\r\n");
	Response.Write("if(balance != 0){document.all('notice').style.visibility='visible';document.f.cmd.style.visibility='hidden';}");
	Response.Write("else{document.all('notice').style.visibility='hidden';document.f.cmd.style.visibility='visible';}");
	Response.Write("}");

	Response.Write("</script");
	Response.Write(">");

//window.alert('applied_left=' + applied_left); 
}
</script>

<form runat=server>
<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=false
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=4
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=9pt 
	width=97%
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=20
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_PageA
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText="ACC#"
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=id/>
		<asp:HyperLinkColumn
			 HeaderText=PURCHASER
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText=CUSTOMER
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=trading_name/>
		<asp:HyperLinkColumn
			 HeaderText="COMPANY NAME"
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=company/>
		<asp:HyperLinkColumn
			 HeaderText=EMAIL#
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=email/>
		<asp:HyperLinkColumn
			 HeaderText=PHONE#
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=phone/>
		<asp:BoundColumn HeaderText=BALANCE DataField=total_balance DataFormatString="{0:c}"/>
	</Columns>
</asp:DataGrid>
</form>


<asp:Label id=LFooter runat=server/>
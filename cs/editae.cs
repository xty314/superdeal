<script runat=server>

DataSet dst = new DataSet();
DataTable dtExpense = new DataTable();

string m_id = "";
bool m_bRecorded = false;
bool m_bIsPaid = false;

string m_branch = "";
string m_fromAccount = "";
string m_toAccount = "";
string m_customerID = "-1";
string m_customerName = "";
string m_paymentType = "1";
string m_paymentDate = "";
string m_paymentRef = "";
string m_note = "";
string m_editRow = "";
string m_nextChequeNumber = "";
string m_sAutoFrequency = "0";
string m_sNextAutoDate = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	
	if(Request.Form["cmd"] != null)
		UpdateAllFields();
	RestoreAllFields();

	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		Session["expense_customer"] = Request.QueryString["sid"];
	else
		Session["expense_customer"] = null;
	if(Session["expense_customer"] != null && Session["expense_customer"] != "")
		m_customerID = Session["expense_customer"].ToString();

	m_nextChequeNumber = GetSiteSettings("next_cheque_number", "1000");

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		EmptyExpenseTable();
		if(!RestoreRecord())
			return;
		m_bRecorded = true;
		Session["expense_current_id"] = m_id;
		Session["expense_recorded"] = true;
	}
	else if(Request.Form["id"] != null && Request.Form["id"] != "")
	{
		m_id = Request.Form["id"];
		m_bRecorded = true;
		Session["expense_current_id"] = m_id;
		Session["expense_recorded"] = true;
	}
	if(Request.QueryString["t"] == "new")
	{
		EmptyExpenseTable();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editae.aspx");
		if(Request.QueryString["auto"] == "1")
			Response.Write("?auto=1");		
		Response.Write("\">");
		return;
	}
	else if(Request.QueryString["t"] == "del")
	{
		CheckExpenseTable();
		dtExpense.Rows.RemoveAt(MyIntParse(Request.QueryString["row"]));
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editae.aspx?r="+ DateTime.Now.ToOADate() +"");
		if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
			Response.Write("&sid="+ Request.QueryString["sid"] +"");
//		Response.Write("&pd="+ m_bIsPaid +"");
		Response.Write("\">");
	}
	else if(Request.QueryString["t"] == "edit")
	{
		m_editRow = Request.QueryString["row"];
//		if(Request.QueryString["pd"] != null && Request.QueryString["pd"] != "")
//		m_bIsPaid = MyBooleanParse(Request.QueryString["pd"].ToString());
	}
	else if(Request.QueryString["saydone"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Done, expense saved");
		if(m_id != "")
		{
			Response.Write("<br><br><a title='go to main page' href='' class=o>Go to Main Page</a>");
			Response.Write("<br><br><a title='go to expense report' href='explist.aspx?s="+ Session["card_id"] +"&id="+ Request.QueryString["acid"] +"&type=1' class=o>Go to Expense Report</a>");
			Response.Write("<br><br><a title='go to expense records' href='"+ Request.ServerVariables["URL"] +"?id="+ m_id +"' class=o>Back to Expense Record</a>");
			return;
//			Response.Write(", please wait 1 second...<meta http-equiv=\"refresh\" content=\"2; URL=editae.aspx?id=" + m_id + "\">");
		}
		else
		{
			Response.Write("<input type=button value='Expense List' onclick=window.location=('explist.aspx') " + Session["button_style"] + ">");
			Response.Write("<br><br><br><br><br><br>");
		}
		PrintAdminFooter();
		return;
	}
	else if(Request.QueryString["search"] == "1")
	{
		DoCustomerSearchAndList();
		return;
	}
	else if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
	{
		m_customerID = Request.QueryString["cid"];
		Session["expense_customer"] = m_customerID;
		DataRow drc = GetCardData(m_customerID);
		if(drc != null)
		{
			m_customerName = drc["trading_name"].ToString();
			if(m_customerName == "")
				m_customerName = drc["company"].ToString();
			if(m_customerName == "")
				m_customerName = drc["name"].ToString();
		}
	}

	if(Request.Form["cmd"] == "OK")
	{
		DoUpdateExpenseRow();
	}
	else if(Request.Form["cmd"] == "Search Card")
	{
		Response.Redirect("editae.aspx?search=1");
		return;
	}
	else if(Request.Form["cmd"] == "Add")
	{
		if(!DoAddExpense())
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editae.aspx?r="+ DateTime.Now.ToOADate() +"");
		if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
			Response.Write("&sid="+ Request.QueryString["sid"] +"");
//		Response.Write("&pd="+ m_bIsPaid +"");
		Response.Write("\">");
	}
	else if(Request.Form["cmd"] == "Record")
	{
		if(Request.Form["confirm_record"] != "on")
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Please tick 'Tick to record'</h3>");
			Response.Write("<br><br><br><br><br><br>");
			PrintAdminFooter();
			return;
		}
		if(DoRecordExpense())
		{
			Response.Write("<script language=javascript>window.location=('editae.aspx?saydone=1&id=" + m_id +"&acid="+ Request.Form["to_account"] +"');</script");
			Response.Write(">");
		}
			//Response.Redirect("editae.aspx?saydone=1&id=" + m_id);
		return;
	}
	else if(Request.Form["cmd"] == "Save Settings")
	{
		if(Request.Form["confirm_record"] != "on")
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Please tick 'Tick to update'</h3>");
			Response.Write("<br><br><br><br><br><br>");
			PrintAdminFooter();
			return;
		}
		if(DoUpdateSettings())
		{
			//Response.Redirect("editae.aspx?saydone=1&id=" + m_id);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?id=" + m_id +" \">");
			return;
		}
	}
	else if(Request.Form["ckw"] != null)
	{
		DoCustomerSearchAndList();
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	PrintBody();

	PrintAdminFooter();
}

void UpdateAllFields()
{
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			Session["expense_branch"] = Request.Form["branch"];
		else if(Session["branch_id"] != null)
			m_branch = Session["branch_id"].ToString();
	}

	Session["expense_customer"] = Request.Form["customer"];
	Session["expense_from_account"] = Request.Form["from_account"];
	Session["expense_to_account"] = Request.Form["to_account"];
	Session["expense_payment_type"] = Request.Form["payment_type"];
	Session["expense_payment_ref"] = Request.Form["payment_ref"];
	Session["expense_payment_date"] = Request.Form["payment_date"];
	Session["expense_note"] = Request.Form["note"];
	Session["expense_paid_status"] = Request.Form["ispaid"];
	Session["expense_customer"] = Request.Form["supplier"];
}	

void RestoreAllFields()
{
	if(Session["expense_branch"] != null)
		m_branch = Session["expense_branch"].ToString();
	if(Session["expense_customer"] != null)
	{
		m_customerID = Session["expense_customer"].ToString();
		if(m_customerID != "")
		{
			DataRow drc = GetCardData(m_customerID);
			if(drc != null)
			{
				m_customerName = drc["trading_name"].ToString();
				if(m_customerName == "")
					m_customerName = drc["company"].ToString();
				if(m_customerName == "")
					m_customerName = drc["name"].ToString();
			}
		}
	}
	if(Session["expense_from_account"] != null)
		m_fromAccount = Session["expense_from_account"].ToString();
	if(Session["expense_to_account"] != null)
		m_toAccount = Session["expense_to_account"].ToString();
	if(Session["expense_payment_type"] != null)
		m_paymentType = Session["expense_payment_type"].ToString();
	if(Session["expense_payment_ref"] != null)
		m_paymentRef = Session["expense_payment_ref"].ToString();
	if(Session["expense_payment_date"] != null)
		m_paymentDate = Session["expense_payment_date"].ToString();
	if(Session["expense_note"] != null)
		m_note = Session["expense_note"].ToString();
	if(Session["expense_recorded"] != null)
		m_bRecorded = true;
	if(Session["expense_current_id"] != null)
		m_id = Session["expense_current_id"].ToString();

	if(Session["expense_current_id"] != null)
		m_id = Session["expense_current_id"].ToString();

	if(Request.QueryString["ps"] != null && Request.QueryString["ps"] != "")
		Session["expense_paid_status"] = Request.QueryString["ps"];

	if(Session["expense_paid_status"] != null)
		m_bIsPaid = MyBooleanParse(Session["expense_paid_status"].ToString());

	if(m_paymentDate == "")
		m_paymentDate = DateTime.Now.ToString("dd-MM-yyyy");
}

bool CheckExpenseTable()
{
	if(Session["ExpenseTable"] == null) 
	{
		dtExpense.Columns.Add(new DataColumn("invoice_number", typeof(String)));
		dtExpense.Columns.Add(new DataColumn("invoice_date", typeof(String)));
		dtExpense.Columns.Add(new DataColumn("tax", typeof(String)));
		dtExpense.Columns.Add(new DataColumn("total", typeof(String)));

		dtExpense.Columns.Add(new DataColumn("taxonly", typeof(String)));
		dtExpense.Columns.Add(new DataColumn("ispaid", typeof(Boolean)));
		Session["ExpenseTable"] = dtExpense;
		return false;
	}
	else
	{
		dtExpense = (DataTable)Session["ExpenseTable"];
	}
	return true;
}

void EmptyExpenseTable()
{
	CheckExpenseTable();
	for(int i=dtExpense.Rows.Count - 1; i>=0; i--)
		dtExpense.Rows.RemoveAt(i);

	//clear session objects for expense
	Session["expense_branch"] = null;
	Session["expense_customer"] = null;
	Session["expense_from_account"] = null;
	Session["expense_to_account"] = null;
	Session["expense_payment_type"] = null;
	Session["expense_payment_ref"] = null;
	Session["expense_payment_date"] = null;
	Session["expense_note"] = null;
	Session["expense_recorded"] = null;
	Session["expense_current_id"] = null;
	Session["expense_paid_status"] = null;
}

bool PrintBody()
{
	double dSubTotal = 0;

	StringBuilder sb = new StringBuilder();

	//main list
	sb.Append("\r\n<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("\r\n<th nowrap>Invoice Number</th>");
	sb.Append("\r\n<th nowrap>Invoice Date</th>");
	sb.Append("\r\n<th>GST Only</th>");
	sb.Append("\r\n<th>Amount</th>");
	sb.Append("\r\n<th width=70>TAX</th>");
	sb.Append("\r\n<th>Total</th>");
	sb.Append("<th>&nbsp;</th>");
	sb.Append("</tr>\r\n");

	CheckExpenseTable();

	for(int i=0; i<dtExpense.Rows.Count; i++)
	{
		DataRow dr = dtExpense.Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = dr["invoice_date"].ToString();
		string tax = dr["tax"].ToString();
		string taxonly = dr["taxonly"].ToString();
		string total = dr["total"].ToString();
		
		double dTax = MyDoubleParse(tax);
		double dTotal = MyDoubleParse(total);
		double dAmount = dTotal - dTax;
		if(taxonly == "on")
			dAmount = 0;

		dSubTotal += dTotal;
		if(m_editRow == i.ToString())
		{
			if(taxonly == "on")
			{
				dTotal = dTax;
				dTax = 0;
			}
			sb.Append("<tr><input type=hidden name=edit_row value=" + i + ">");
			sb.Append("<td><input type=text name=invoice_number maxlength=49 value='" + invoice_number + "'></td>");
			sb.Append("<td><input type=text name=invoice_date maxlength=49 value='" + invoice_date + "'></td>");
			sb.Append("<td align=center>");
			sb.Append("<input type=checkbox name=taxonly ");
			if(taxonly == "on")
				sb.Append(" checked ");
//tee, why set 0.00000001?
			sb.Append(" onclick=\"if(document.f.taxonly.checked){document.f.amount.disabled=true;}else{document.f.amount.value='0';document.f.amount.disabled=false;}\">");
//			sb.Append(" onclick=\"if(document.f.taxonly.checked){document.f.amount.value='0.0000001';}else{document.f.amount.value='0';document.f.amount.disabled=false;}\">");
			sb.Append("</td>");
			
			sb.Append("<td align=right><input type=text name=amount maxlength=49 ");
			if(taxonly == "on")
				sb.Append(" disabled=true ");
			sb.Append("></td>");
			sb.Append("<td align=center><input type=checkbox name=tax ");
			if(dTax != 0)
				sb.Append(" checked");
			if(taxonly == "on")
				sb.Append(" disabled=true ");
			sb.Append(" ></td>");
			sb.Append("<td align=right><input type=text name=total maxlength=49 value=" + dTotal + "></td>");
			sb.Append("<td align=right><input type=submit name=cmd value='OK' " + Session["button_style"] + "></td>");
		}
		else
		{
			sb.Append("<tr>");
			sb.Append("<td>" + invoice_number + "</td>");
			sb.Append("<td>" + invoice_date + "</td>");
			sb.Append("<td align=center>");
			sb.Append("<input type=checkbox name=taxonly1 disabled ");
			if(taxonly == "on")
				sb.Append(" checked ");
				
			sb.Append(">");
			sb.Append("</td>");
			sb.Append("<td align=right>" + dAmount.ToString("c") + "</td>");
			sb.Append("<td align=right>" + dTax.ToString("c") + "</td>");
			sb.Append("<td align=right>" + dTotal.ToString("c") + "</td>");
			sb.Append("<td align=right>");
/*			sb.Append("<a href='editae.aspx?t=edit&row=" + i + "");
			if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
				sb.Append("&sid="+ Request.QueryString["sid"] +"");
			sb.Append("&ps="+ m_bIsPaid +"");
			sb.Append("' class=o>EDIT</a> ");
			sb.Append("<a href='editae.aspx?t=del&row=" + i + "");
			if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
				sb.Append("&sid="+ Request.QueryString["sid"] +"");
			sb.Append("&ps="+ m_bIsPaid +"");
			sb.Append("' class=o>DEL</a> ");
*/
			sb.Append("</tr>");
		}
	}
	
/*	if(m_editRow == "")
	{
		sb.Append("<tr>");
		sb.Append("<td><input type=text name=invoice_number maxlength=49></td>");
		sb.Append("<td><input type=text name=invoice_date maxlength=49 value='" + DateTime.Now.ToString("dd-MM-yyyy") + "'></td>");
		//sb.Append("<td align=right><input type=text name=amount maxlength=49></td>");
		sb.Append("<td align=center><input type=checkbox name=taxonly ");
		sb.Append(" onclick=\"if(document.f.taxonly.checked)");
		sb.Append("{document.f.amount.disabled=true;document.f.tax.disabled=true;}");
		sb.Append("else");
		sb.Append("{document.f.amount.value='0';document.f.amount.disabled=false;}\"></td>");
		sb.Append("<td align=right>");
		sb.Append("<input type=text name=amount maxlength=49 ");
		sb.Append("></td>");
		sb.Append("<td align=center><input type=checkbox name=tax checked></td>");
		sb.Append("<td align=right><input type=text name=total maxlength=49></td>");
		sb.Append("<td align=right><input type=submit name=cmd value='Add' " + Session["button_style"] + "></td>");
		sb.Append("</tr>");
	}
*/

	sb.Append("<tr><td colspan=6>&nbsp;<br><br><br></td></tr>");
	sb.Append("</table>");

	Response.Write("<br><center><h3>Auto Payment Setup</h3></center>");
	Response.Write("<form name=f action='editae.aspx");
	if(Session["expense_customer"] != null && Session["expense_customer"] != "")
		Response.Write("?sid="+ Session["expense_customer"] +"");
	Response.Write("' method=post>");

	//hidden values
	Response.Write("<input type=hidden name=id value=" + m_id + ">");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=0 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td valign=top>");

		Response.Write("<table>");
		Response.Write("<tr><td>");
		//branch
		if(Session["branch_support"] != null)
		{
			Response.Write("<b>Branch : </b></td>");
			Response.Write("<td>");
			Response.Write(GetBranchName(m_branch));
//			if(!PrintBranchNameOptions())
//				return false;
			Response.Write("</td>");
		}
		Response.Write("</tr>");
		string uri = Request.ServerVariables["URL"] +"?sid=";

		//payee
		if(m_customerID != "-1")
			Session["expense_customer"] = m_customerID;

		Response.Write("\r\n<tr><td><b>Payee : </b></td><td>");
		if(Session["expense_customer"] != null && Session["expense_customer"] != "")
		{
			DataRow drc = GetCardData(Session["expense_customer"].ToString());
			if(drc != null)
			{
				string cn = drc["company"].ToString();
				if(cn == "")
				{
					cn = drc["trading_name"].ToString();
					if(cn == "")
						cn = drc["name"].ToString();
				}
				Response.Write(cn);
			}
		}

		string last_uri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];

		//from account
		Response.Write("<tr><td><b>From Account : </b></td><td>");
		if(!PrintFromAccountList())
			return false;
		Response.Write("</td></tr>");

		//To account
		Response.Write("<tr><td><b>Expense Type : </b></td><td>");
		if(!PrintToAccountList())
			return false;
		Response.Write("</td></tr>");
		Response.Write("</table>");

	Response.Write("</td><td align=right valign=top>");

		//payment table
		Response.Write("\r\n<table>");
		Response.Write("<tr><td><b>Payment Type : </b></td>");
		Response.Write("<td>");
		Response.Write(GetEnumValue("payment_method", m_paymentType));
		Response.Write("</td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Reference : </b></td>");
		Response.Write("<td>" + m_paymentRef + "</td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Payment Date : </b></td><td>");
		Response.Write(m_paymentDate + "</td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Amount : </b></td><td>");
		Response.Write(dSubTotal.ToString("c"));
		Response.Write("</td></tr>");
		Response.Write("\r\n</table>");

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");

	Response.Write(sb.ToString());

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=1><b>Note : </b><br>");
	Response.Write("<textarea disabled name=note rows=3 cols=50>" + m_note + "</textarea>");
	Response.Write("</td>");
//	if(m_bRecorded)
	{
		Response.Write("<td align=right>");
		Response.Write("<table width=100% cellspacing=0 cellpadding=4 border=0 bordercolor=#CCCCCC bgcolor=#EEEEE");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td>");
		Response.Write("<b> Payment Frequency : </b>");
		Response.Write("<select name=autopayment_frequency>");
		Response.Write(GetEnumOptions("autopayment_frequency", m_sAutoFrequency));
		Response.Write("</select> ");
		if(m_sAutoFrequency == "0")
			Response.Write("<font color=red><i>This AutoPayment has been disabled.</i></font>");
		Response.Write("</td></tr><tr><td>");
		Response.Write("Next Payment Date : <input type=text size=10 name=next_payment_date value='" + m_sNextAutoDate + "'>");
		Response.Write(" <a href=explist.aspx?autolist=1 class=o target=_blank>Auto Payment List</a>");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		Response.Write("</td>");
	}
	Response.Write("</tr>");
	Response.Write("<tr bgcolor=#EEEEE><td colspan=2 align=right>");
	
/*	Response.Write("<b>Type Record:</b> <select name=ispaid ");
	Response.Write(">");
	if(Session["expense_paid_status"] != null && Session["expense_paid_status"] != "")
		m_bIsPaid = MyBooleanParse(Session["expense_paid_status"].ToString());
	if(!m_bIsPaid)
	{
	Response.Write("<option value=0 ");
	if(!m_bIsPaid)
		Response.Write(" selected ");
	Response.Write(">Record Only");
	}
	Response.Write("<option value=1 ");
	if(m_bIsPaid)
		Response.Write(" selected  ");
	Response.Write(">Paid Expense");
	Response.Write("</select> ");
*/
	Response.Write("<input type=checkbox name=confirm_record>Confirm to Save ");
	Response.Write("<input type=submit name=cmd value='Save Settings' class=b");
	Response.Write(" onclick=\"if(!document.f.confirm_record.checked)");
	Response.Write("{window.alert('Please Tick \\'Confirm to Save\\' check box');return false;}");
	Response.Write("else if(!confirm('Are you sure to alter this autopayment settings?'))");
	Response.Write("{return false;}\" ");
	Response.Write(">");
//	Response.Write("<input type=button value='Expense List' onclick=window.location=('explist.aspx') " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool PrintFromAccountList()
{
	int rows = 0;
	string sc = "SELECT * FROM account WHERE class1=1 OR class1=2 ORDER BY class1, class2, class3, class4";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "account");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=from_account>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["account"].Rows[i];
		string id = dr["id"].ToString();
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		double dAccBalance = double.Parse(dr["balance"].ToString());
		if(id != m_fromAccount)
			continue;
		Response.Write("<option value=" + id);
		if(id == m_fromAccount)
		{
			Response.Write(" selected");
//			m_sAccBalance = dAccBalance.ToString("c");
		}
		Response.Write(">" + disnumber + " " + dr["name4"].ToString());
		Response.Write(" " + dr["name1"].ToString());
//		Response.Write(" " + dAccBalance.ToString("c"));		
	}
	Response.Write("</select>");
//	if(m_sAccBalance != "")
//		Response.Write("<b>&nbsp&nbsp&nbsp; ------------ Balance : " + m_sAccBalance + "</b>");
	return true;
}

bool PrintToAccountList()
{
	int rows = 0;
	string sc = "SELECT *, name4+' ' +name1 AS type ";
	sc += " FROM account ";
	sc += " WHERE class1 = 6 OR class1 = 2 ";
	sc += " ORDER BY type ";//class1, class2, class3, class4";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "toaccount");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=to_account>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["toaccount"].Rows[i];
		string id = dr["id"].ToString();
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		double dAccBalance = double.Parse(dr["balance"].ToString());
		if(id != m_toAccount)
			continue;
		Response.Write("<option value=" + id);
		if(id == m_toAccount)
		{
			Response.Write(" selected");
//			m_sAccBalance = dAccBalance.ToString("c");
		}
		Response.Write(">" + dr["type"].ToString());
//		Response.Write(" " +dr["name1"].ToString());
//		Response.Write(" " + dAccBalance.ToString("c"));		
	}
	Response.Write("</select>");
	return true;
}

bool DoCustomerSearchAndList()
{
	int rows = 0;
	string kw = "'%" + Request.Form["ckw"] + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%%'";
	string sc = "SELECT c.*, c1.name AS sales_name ";
	sc += " FROM card c LEFT OUTER JOIN card c1 ON c1.id = c.sales ";
	sc += " WHERE c.type=3 AND ("; //type 3: supplier;
	if(IsInteger(Request.Form["ckw"]))
		sc += " c.id=" + Request.Form["ckw"] + ") ";
	else
		sc += " c.name LIKE " + kw + " OR c.email LIKE " + kw + " OR c.trading_name LIKE " + kw + " OR c.company LIKE " + kw + ")";
	sc += " ORDER BY c.trading_name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
		if(rows == 1)
		{
			string search_id = dst.Tables["card"].Rows[0]["id"].ToString();
			Trim(ref search_id);
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=editae.aspx?cid=" + search_id);
			Response.Write("\">");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<center><h3>Search for Payee</h3></center>");
	Response.Write("<form id=search action=editae.aspx method=post>");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	Response.Write("<input type=button name=cmd value='Cancel'");
	Response.Write(" onClick=window.location=('editae.aspx') " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=window.open('ecard.aspx?n=customer&a=new') value='New Customer' " + Session["button_style"] + ">");
	Response.Write("</td></tr></table></form>");

	BindCustomer();
	PrintAdminFooter();

	return true;
}

void BindCustomer()
{
	if(dst.Tables["card"].Rows.Count == 0)
	{
		Response.Write("<br><center><h3>Your search for " + Request.Form["ckw"] + " returns 0 result</h3>");
		return;
	}
	string bgcolor = "lightblue";//GetSiteSettings("table_row_bgcolor", "#666696");
	Response.Write("<table width=90%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:" + bgcolor +";font-weight:bold;\">\r\n");
	Response.Write("<th>ACC#</th>");
	Response.Write("<th>Company</th>\r\n");
	Response.Write("<th>Contact</th>\r\n");
	Response.Write("<th>Email</th>\r\n");
	Response.Write("<th>Phone</th>\r\n");
	Response.Write("<th>Balance</td>\r\n");
	Response.Write("</tr>\r\n");

	bool bcolor = true;
	string scolor = "";

	int rows = 50;
	for(int i=0; i<rows; i++)
	{
		if(i >= dst.Tables["card"].Rows.Count)
			break;

		if(bcolor)
			scolor = " bgcolor=#EEEEEE";
		else
			scolor = "";

		bcolor = !bcolor;

		DataRow dr = dst.Tables["card"].Rows[i];
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string name = dr["name"].ToString();
		string email = dr["email"].ToString();
		string balance = MyDoubleParse(dr["balance"].ToString()).ToString("c");
		string phone = dr["phone"].ToString();

		Response.Write("<tr" + scolor + ">");

		Response.Write("<td><a href=editae.aspx?cid=" + id + ">");
		Response.Write(id + "</a></td>\r\n");

		Response.Write("<td><a href=editae.aspx?cid=" + id + ">");
		Response.Write(trading_name + "</a></td>");

		Response.Write("<td><a href=editae.aspx?cid=" + id + ">");
		Response.Write(name + "</a></td>");

		Response.Write("<td><a href=editae.aspx?cid=" + id + ">");
		Response.Write(email + "</a></td>");

		Response.Write("<td>" + phone + "</td>");
		Response.Write("<td align=right>" + balance + "</td>");

		Response.Write("</tr>");
	}	
	Response.Write("</table>");
}

bool DoAddExpense()
{
	string invoice_number = Request.Form["invoice_number"];
	string invoice_date = Request.Form["invoice_date"];
	string amount = Request.Form["amount"];
	string total = Request.Form["total"];
	string taxonly = Request.Form["taxonly"];
	m_bIsPaid = MyBooleanParse(Request.Form["ispaid"].ToString());
	double dAmount = 0;
	double dTotal = 0;

	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	DateTime tInvoice;
	string msg = "";
	dAmount = MyMoneyParse(amount);
	dTotal = MyMoneyParse(total);
	try
	{
		tInvoice = DateTime.Parse(invoice_date, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
	}
	catch(Exception e)
	{
		msg = "Invoice Date. Input string <font color=red>" + invoice_date + "</font> was not in a correct format";
	}

	double dGst = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100 + 1;
	
	double dTax = 0;
	bool bTaxOnly = false;
	if(taxonly == "on")
		bTaxOnly = true;

	bool bTax = false;
	if(Request.Form["tax"] == "on")
		bTax = true;
	if(bTaxOnly)
	{
		dTax = dTotal;
		dTotal = 0;
	}
	else
	{
		if(dTotal != 0 && dAmount != 0)
		{
			dTax = dTotal - dAmount;
		}
		else if(dTotal != 0)
		{
			if(bTax)
			{
				//dAmount = Math.Round(dTotal / dGst);
				dAmount = Math.Round(dTotal / dGst, 3);
				dTax = dTotal - dAmount;
			}
			else
				dAmount = dTotal;
		}
		else if(dAmount != 0)
		{
			if(bTax)
			{
				//dTotal = Math.Round(dAmount * dGst);
				dTotal = Math.Round(dAmount * dGst, 3);
				dTax = dTotal - dAmount;
			}
			else
				dTotal = dAmount;
		}
		else
		{
			msg = "Error, please enter either Amount or Total";
		}
	}

	if(msg != "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><br><center><h3>Error, " + msg + "</h3>");
		Response.Write("<br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}

	CheckExpenseTable();

	DataRow dr = dtExpense.NewRow();
	dr["invoice_number"] = invoice_number;
	dr["invoice_date"] = invoice_date;
	dr["tax"] = dTax.ToString();
	dr["total"] = dTotal.ToString();
	dr["taxonly"] = taxonly;

	dtExpense.Rows.Add(dr);
	return true;
}

bool DoUpdateExpenseRow()
{
	CheckExpenseTable();
	int row = MyIntParse(Request.Form["edit_row"]);
	if(row >= dtExpense.Rows.Count)
		return false;
	string invoice_number = Request.Form["invoice_number"];
	string invoice_date = Request.Form["invoice_date"];
	string amount = Request.Form["amount"];
	string total = Request.Form["total"];
	string taxonly = Request.Form["taxonly"];
//	m_bIsPaid = MyBooleanParse(Request.Form["ispaid"].ToString());
	double dAmount = 0;
	double dTotal = 0;
	
	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	DateTime tInvoice;
	string msg = "";
	dAmount = MyMoneyParse(amount);
	dTotal = MyMoneyParse(total);
	try
	{
		tInvoice = DateTime.Parse(invoice_date, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
	}
	catch(Exception e)
	{
		msg = "Invoice Date. Input string <font color=red>" + invoice_date + "</font> was not in a correct format";
	}

	double dGst = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100 + 1;
	bool bTax = false;
	if(Request.Form["tax"] == "on")
		bTax = true;
	double dTax = 0;

	bool bTaxOnly = false;
	if(taxonly == "on")
		bTaxOnly = true;

	if(bTaxOnly)
	{
		dTax = dTotal;
		dTotal = 0;
	}
	else
	{
		if(dTotal != 0 && dAmount != 0)
		{
			dTax = dTotal - dAmount;
		}
		else if(dTotal != 0)
		{
			if(bTax)
			{
				dAmount = Math.Round(dTotal / dGst, 3);
				dTax = dTotal - dAmount;
			}
			else
				dAmount = dTotal;
		}
		else if(dAmount != 0)
		{
			if(bTax)
			{
				dTotal = Math.Round(dAmount * dGst, 3);
				dTax = dTotal - dAmount;
			}
			else
				dTotal = dAmount;
		}
		else
		{
			msg = "Error, please enter either Amount or Total";
		}
	}

	if(msg != "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><br><center><h3>Error, " + msg + "</h3>");
		Response.Write("<br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}

	DataRow dr = dtExpense.Rows[row];
	dr["invoice_number"] = invoice_number;
	dr["invoice_date"] = invoice_date;
	dr["tax"] = dTax.ToString();
	dr["total"] = dTotal.ToString();
	dr["taxonly"] = taxonly;
	dtExpense.AcceptChanges();
	return true;
}

bool DoRecordExpense()
{
	CheckExpenseTable();
	if(dtExpense.Rows.Count == 0 || Session["expense_customer"] == null && Session["expense_customer"] == "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, no expense to record</h3>");
		Response.Write("<br><br><br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}
	
	string payment_date = Request.Form["payment_date"];
	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	DateTime tPayment;
	string msg = "";
	try
	{
		tPayment = DateTime.Parse(payment_date, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
	}
	catch(Exception e)
	{
		msg = "Payment Date. Input string <font color=red>" + payment_date + "</font> was not in a correct format";
	}
	if(msg != "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><br><center><h3>Error, " + msg + "</h3>");
		Response.Write("<br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}

	string new_id = "";
//DEBUG("ispaid = ", Request.Form["ispaid"].ToString());
if(Request.Form["supplier"] != null && Request.Form["supplier"] != "")
m_customerID = Request.Form["supplier"];

	Session["expense_customer"] = m_customerID;

	//get new id
	string sc = "BEGIN TRANSACTION ";
	sc += " SET DATEFORMAT dmy ";
	sc += " INSERT INTO expense (card_id, from_account, to_account, payment_type ";
	sc += ", payment_ref, payment_date, recorded_by ";
	sc += ", ispaid ";
	sc += ") ";
	sc += " VALUES(" + m_customerID;
	sc += ", " + m_fromAccount;
	sc += ", " + m_toAccount;
	sc += ", " + m_paymentType;
	sc += ", '" + EncodeQuote(m_paymentRef) + "' ";
	sc += ", '" + m_paymentDate + "' ";
	sc += ", " + Session["card_id"].ToString();
	sc += " , "+ Request.Form["ispaid"] +" ";
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('expense') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "id") == 1)
		{
			new_id = dst.Tables["id"].Rows[0]["id"].ToString();
			m_id = new_id;
		}
		else
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Error recording expense, failed to get new id</h3>");
			Response.Write("<br><br><br><br><br><br>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//record items
	sc = " SET DATEFORMAT dmy ";
	double dSubTax = 0;
	double dSubTotal = 0;
	double dSubTotalWithTaxOnly = 0;
	for(int i=0; i<dtExpense.Rows.Count; i++)
	{
		DataRow dr = dtExpense.Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = dr["invoice_date"].ToString();
		double dTax = MyDoubleParse(dr["tax"].ToString());
		double dTotal = MyDoubleParse(dr["total"].ToString());
		
		dSubTax += dTax;
		dSubTotal += dTotal;
		dSubTotalWithTaxOnly += dTotal;
		if(dTotal == 0)
			dSubTotalWithTaxOnly += dTax;
			

		sc += " SET DATEFORMAT dmy INSERT INTO expense_item (id, invoice_number, invoice_date, tax, total) ";
		sc += " VALUES(" + new_id + ", '" + EncodeQuote(invoice_number) + "', '" + invoice_date + "' ";
		sc += ", " + dTax + ", " + dTotal + ") ";
	}
	sc += " UPDATE expense SET ";
	sc += " tax = " + dSubTax;
	sc += ", total = " + dSubTotal;
	sc += ", note = '" + EncodeQuote(m_note) + "' ";
	sc += ", ispaid = "+ Request.Form["ispaid"] +" ";
	sc += " WHERE id = " + new_id;
	
	if(Request.Form["ispaid"] == "1")
	{
		sc += " UPDATE account SET balance = balance - " + dSubTotalWithTaxOnly + " WHERE id = " + m_fromAccount;
		sc += " UPDATE account SET balance = balance + " + dSubTotalWithTaxOnly + " WHERE id = " + m_toAccount;
	}
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
	EmptyExpenseTable();
	return true;
}

bool RestoreRecord()
{
	string sc = " SELECT e.* ";
	sc += ", i.kid, i.invoice_number, i.invoice_date, i.tax AS item_tax, i.total AS item_total ";
	sc += " FROM expense e ";
	sc += " JOIN expense_item i ON i.id=e.id ";
	sc += " WHERE e.id = " + m_id;
//DEBUG(" sc +", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "restore") <= 0)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Record Not Found</h3>");
			Response.Write("<br><br><br><br><br><br>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dst.Tables["restore"].Rows[0];
	m_branch = dr["branch"].ToString();
	m_customerID = dr["card_id"].ToString();
	m_fromAccount = dr["from_account"].ToString();
	m_toAccount = dr["to_account"].ToString();
	m_paymentType = dr["payment_type"].ToString();
	m_paymentRef = dr["payment_ref"].ToString();
	m_paymentDate = DateTime.Parse(dr["payment_date"].ToString()).ToString("dd-MM-yyyy");
	m_note = dr["note"].ToString();
	m_bIsPaid = bool.Parse(dr["ispaid"].ToString());

	DataRow drc = GetCardData(m_customerID);
	if(drc != null)
	{
		m_customerName = drc["trading_name"].ToString();
		if(m_customerName == "")
			m_customerName = drc["company"].ToString();
		if(m_customerName == "")
			m_customerName = drc["name"].ToString();
	}

	Session["expense_branch"] = m_branch;
	Session["expense_customer"] = m_customerID;
	Session["expense_from_account"] = m_fromAccount;
	Session["expense_to_account"] = m_toAccount;
	Session["expense_payment_type"] = m_paymentType;
	Session["expense_payment_ref"] = m_paymentRef;
	Session["expense_payment_date"] = m_paymentDate;
	Session["expense_note"] = m_note;

	for(int i=0; i<dst.Tables["restore"].Rows.Count; i++)
	{
		dr = dst.Tables["restore"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = DateTime.Parse(dr["invoice_date"].ToString()).ToString("dd-MM-yyyy");
		string tax = dr["item_tax"].ToString();
		string total = dr["item_total"].ToString();

		DataRow dre = dtExpense.NewRow();
		dre["invoice_number"] = invoice_number;
		dre["invoice_date"] = invoice_date;
		dre["tax"] = tax;
		dre["total"] = total;
		if(total == "0")
			dre["taxonly"] = "on";
		dtExpense.Rows.Add(dre);
	}

	if(!GetAutoPaymentInfo())
		return false;
	return true;
}

bool DoUpdateSettings()
{
	GetAutoPaymentInfo();

	string newFrequency = Request.Form["autopayment_frequency"];
	string next_date = Request.Form["next_payment_date"];
	if(newFrequency == m_sAutoFrequency && next_date == m_sNextAutoDate)
		return true;

	string sc = " SET DATEFORMAT dmy ";
	if(next_date == m_sNextAutoDate) //frequency changed
	{
		if(newFrequency == "0") //remove
			sc += " DELETE FROM auto_expense WHERE id = " + m_id;
		else if(m_sAutoFrequency == "0") //add
		{
			if(next_date == "")
				next_date = GalcNextPaymentDate(newFrequency);//DateTime.Now.AddDays(7).ToString("dd-MM-yyyy");
			sc += " INSERT INTO auto_expense (id, frequency, next_payment_date) VALUES (" + m_id + ", " + newFrequency + ", '" + next_date + "') ";
		}
		else //update
			sc += " UPDATE auto_expense SET frequency = " + newFrequency + " WHERE id = " + m_id;
	}
	else //date changed
	{
		if(next_date == "")
			next_date = GalcNextPaymentDate(newFrequency);
		if(newFrequency != m_sAutoFrequency) //frequency changed too
		{
			if(newFrequency == "0")
				sc += " DELETE FROM auto_expense WHERE id = " + m_id;
			else if(m_sAutoFrequency == "0") //add
				sc += " INSERT INTO auto_expense (id, frequency, next_payment_date) VALUES (" + m_id + ", " + newFrequency + ", '" + next_date + "') ";
			else
				sc += " UPDATE auto_expense SET frequency = " + newFrequency + ", next_payment_date='" + next_date + "' WHERE id = " + m_id;
		}
		else
		{
			sc += " UPDATE auto_expense SET next_payment_date='" + next_date + "' WHERE id = " + m_id;
		}
	}
//DEBUG("sc=", sc);
//Response.End();
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

bool GetAutoPaymentInfo()
{
	if(m_id == "")
		return true;

	if(dst.Tables["ap"] != null)
		dst.Tables["ap"].Clear();

	string sc = " SELECT * FROM auto_expense WHERE id = " + m_id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "ap") <= 0)
		{
			return true;
		}
		else
		{
			m_sAutoFrequency = dst.Tables["ap"].Rows[0]["frequency"].ToString();
			m_sNextAutoDate = DateTime.Parse(dst.Tables["ap"].Rows[0]["next_payment_date"].ToString()).ToString("dd-MM-yyyy");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string GalcNextPaymentDate(string frequency)
{
	DateTime d = DateTime.Now;
	int nf = MyIntParse(frequency);
	switch(nf)
	{
	case 1:
		d = d.AddDays(7);
		break;
	case 2:
		d = d.AddDays(14);
		break;
	case 3:
		d = d.AddDays(28);
		break;
	case 4:
		d = d.AddMonths(1);
		break;
	case 5:
		d = d.AddMonths(2);
		break;
	case 6:
		d = d.AddDays(84); //12 weeks
		break;
	case 7:
		d = d.AddMonths(3);
		break;
	case 8:
		d = d.AddMonths(6);
		break;
	case 9:
		d = d.AddYears(1);
		break;
	default:
		break;
	}
	return d.ToString("dd-MM-yyyy");
}

string GetBranchName(string id)
{
	DataSet ds = new DataSet();
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

</script>

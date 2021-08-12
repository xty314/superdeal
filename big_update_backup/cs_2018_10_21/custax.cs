<script runat=server>

DataSet dst = new DataSet();
DataTable dtExpense = new DataTable();

string m_id = "";
bool m_bRecorded = false;
bool m_bIsPaid = false;

string m_branch = "1";
string m_fromAccount = "";
string m_toAccount = "";
string m_customerID = "-1";
string m_customerName = "";
string m_paymentType = "1";
string m_paymentDate = "";
string m_paymentRef = "";
string m_note = "";
string m_editRow = "";
string m_nextChequeNumber = "100000";
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
	if(!GetNextChequeNumber())
		return;
	
	if(Request.QueryString["rd"] == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		ShowPaidCustomGSTDone();
			PrintAdminFooter();
		return;
	}
	if(Request.Form["cmd"] == "Record")
	{
		if(doPayCustomGST())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?rd=done\">");
			return;
		}
	}

	PrintAdminHeader();
	PrintAdminMenu();

	PrintBody();

	PrintAdminFooter();
}

void ShowPaidCustomGSTDone()
{
	Response.Write("<center><br><h4>Record Done...");
	Response.Write("<br><br><a title='go to custom gst report' href='custax_rp.aspx?r="+ DateTime.Now.ToOADate() +"' class=o>Go to Custom GST List</a>");
	Response.Write("<br><br><a title='new custom gst transaction' href='custax.aspx?r="+ DateTime.Now.ToOADate() +"' class=o>Pay New Custom GST</a>");
}

bool doPayCustomGST()
{

	string new_total = Request.Form["total_amount"];
	string from_account = Request.Form["from_account"];
	string location_account = Request.Form["to_account"];

	string sc = "BEGIN TRANSACTION ";
	sc += " SET DATEFORMAT dmy ";
	sc += " INSERT INTO custom_tax (branch, clientcode, statement_date, date_payment_due ";
	sc += ", total_gst, recorded_date, recorded_by, ispaid, claimable";
	sc += ", location_acc, from_acc, payment_type,	payment_ref, note, payee )";
	sc += " VALUES (" + m_branch;
	sc += ", '" + EncodeQuote(Request.Form["client_code"]) + "' ";
	sc += ", '" + EncodeQuote(Request.Form["payment_date"]) + "' ";
	sc += ", '" + EncodeQuote(Request.Form["payment_due_date"]) + "' ";
	sc += ", " + Request.Form["total_amount"];
	sc += ", GETDATE()";
	sc += ", " + Session["card_id"];
	sc += ", 1, 1 ";
	sc += ", " + Request.Form["to_account"];
	sc += ", " + Request.Form["from_account"];
	sc += ", " + Request.Form["payment_type"];
	sc += ", CONVERT(varchar(150),'" + EncodeQuote(Request.Form["payment_ref"]) + "') ";
	sc += ", '" + EncodeQuote(StripHTMLtags(Request.Form["note"])) + "' ";
	sc += ", " + Request.Form["supplier"];
	sc += " ) ";
	sc += " SELECT IDENT_CURRENT('custom_tax') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "id") == 1)
		{
			m_id = dst.Tables["id"].Rows[0]["id"].ToString();
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

	//update account balance
	sc = " UPDATE account SET balance = balance - "+ Request.Form["total_amount"] +" WHERE id = "+ from_account;
	sc += " UPDATE account SET balance = balance + "+ Request.Form["total_amount"] +" WHERE id = "+ location_account;

	sc += " INSERT INTO custom_tax_log (id, update_by, update_date, old_total, new_total, note) ";
	sc += " SELECT id, "+ Session["card_id"] +", getdate(), total_gst, total_gst, 'recorded to custom gst' ";
	sc += " FROM custom_tax WHERE id = " + m_id;
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

bool CreatTableCustomTax()
{
	string sc = "";
	sc += "	CREATE TABLE [dbo].[custom_tax] ( ";
	sc += "	[id] [int] IDENTITY (1, 1) NOT FOR REPLICATION  NOT NULL ,";
	sc += "	[clientcode] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,";
	sc += "	[statement_date] [datetime] NULL ,";
	sc += "	[date_payment_due] [datetime] NULL ,";
	sc += "	[total_gst] [money] NULL ,";
	sc += "	[recorded_date] [datetime] NULL ,";
	sc += "	[recorded_by] [datetime] NULL ,";
	sc += "	[autopayment] [bit] NOT NULL ,";
	sc += "	[routine_time] [datetime] NULL ,";
	sc += "	[ispaid] [bit] NOT NULL ,";
	sc += "	[claimable] [bit] NOT NULL ,";
	sc += "	[location_acc] [int] NOT NULL ,";
	sc += "	[from_acc] [int] NOT NULL ,";
	sc += "	[to_acc] [int] ,";
	sc += "	[payment_type] [int] NOT NULL ,";
	sc += "	[payment_ref] [varchar] (150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,";
	sc += " [note] [varchar] (1024) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ";
	sc += " [payee] [int]  NOT NULL ";
	sc += " ) ON [PRIMARY]";
	//sc += " GO";

	sc += " ALTER TABLE [dbo].[custom_tax] WITH NOCHECK ADD ";
	sc += " CONSTRAINT [DF_custom_tax_autopayment] DEFAULT (0) FOR [autopayment],";
	sc += "	CONSTRAINT [DF_custom_tax_ispaid] DEFAULT (0) FOR [ispaid],";
	sc += " CONSTRAINT [DF_custom_tax_claimable] DEFAULT (1) FOR [claimable]";
//	sc += " GO";

	sc += " CREATE TABLE [dbo].[custom_tax_log] ( ";
	sc += " [kid] [bigint] IDENTITY (1, 1) NOT NULL , ";
	sc += " [id] [int] NOT NULL , ";
	sc += " [update_date] [datetime] NOT NULL , ";
	sc += " [update_by] [int] NOT NULL , ";
	sc += " [old_total] [money] NOT NULL , ";
	sc += " [new_total] [money] NOT NULL , ";
	sc += " [note] [varchar] (512) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL  ";
	sc += " ) ON [PRIMARY] ";
//	sc += " GO ";

	sc += " ALTER TABLE [dbo].[custom_tax_log] WITH NOCHECK ADD  ";
	sc += " CONSTRAINT [DF_custom_tax_log_update_date] DEFAULT (getdate()) FOR [update_date] ";
//	sc += " GO ";
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
void UpdateAllFields()
{
	Session["expense_branch"] = Request.Form["branch"];
	Session["expense_customer"] = Request.Form["customer"];
	Session["expense_from_account"] = Request.Form["from_account"];
	Session["expense_to_account"] = Request.Form["to_account"];
	Session["expense_payment_type"] = Request.Form["payment_type"];
	Session["expense_payment_ref"] = Request.Form["payment_ref"];
	Session["expense_payment_date"] = Request.Form["payment_date"];
	Session["expense_note"] = Request.Form["note"];
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

	if(m_paymentDate == "")
		m_paymentDate = DateTime.Now.ToString("dd-MM-yyyy");
}


bool PrintBody()
{
	double dSubTotal = 0;

	StringBuilder sb = new StringBuilder();

	//main list
/*	sb.Append("\r\n<table width=100% align=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("\r\n<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("\r\n<th nowrap>Invoice Number</th>");
	sb.Append("\r\n<th nowrap>Invoice Date</th>");
//	sb.Append("\r\n<th>GST Only</th>");
	sb.Append("\r\n<th>Amount</th>");
	sb.Append("\r\n<th>DUTY/LEVY/ITF</th>");
	sb.Append("\r\n<th>Total</th>");
	sb.Append("<th>&nbsp;</th>");
	sb.Append("</tr>\r\n");

	CheckExpenseTable();

	for(int i=0; i<dtExpense.Rows.Count; i++)
	{
		DataRow dr = dtExpense.Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = dr["invoice_date"].ToString();
		string total = dr["total"].ToString();
		
		double dTotal = MyDoubleParse(total);
		double dAmount = dTotal;

		dSubTotal += dTotal;
		if(m_editRow == i.ToString())
		{
			sb.Append("<tr><input type=hidden name=edit_row value=" + i + ">");
			sb.Append("<td><input type=text name=invoice_number maxlength=49 value='" + invoice_number + "'></td>");
			sb.Append("<td><input type=text name=invoice_date maxlength=49 value='" + invoice_date + "'></td>");
			sb.Append("<td align=lleft><input type=text name=amount maxlength=49></td>");
			sb.Append("<td align=lleft><input type=text name=levy maxlength=49></td>");		
			sb.Append("<td align=lleft><input type=text name=total maxlength=49 value=" + total + "></td>");
			sb.Append("<td align=right><input type=submit name=cmd value='OK' " + Session["button_style"] + "></td>");
		}
		else
		{
			sb.Append("<tr>");
			sb.Append("<td>" + invoice_number + "</td>");
			sb.Append("<td>" + invoice_date + "</td>");
			sb.Append("<td align=lleft>" + dAmount.ToString("c") + "</td>");
			sb.Append("<td align=lleft>" + dAmount.ToString("c") + "</td>");
			sb.Append("<td align=lleft>" + dTotal.ToString("c") + "</td>");
			sb.Append("<td align=lleft>");
			sb.Append("<a href='"+ Request.ServerVariables["URL"] +"?t=edit&row=" + i + "");
			if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
				sb.Append("&sid="+ Request.QueryString["sid"] +"");
			sb.Append("' class=o>EDIT</a> ");
			sb.Append("<a href='"+ Request.ServerVariables["URL"] +"?t=del&row=" + i + "");
			if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
				sb.Append("&sid="+ Request.QueryString["sid"] +"");
			sb.Append("' class=o>DEL</a> ");
			sb.Append("</tr>");
		}
	}
	
	if(m_editRow == "")
	{
		sb.Append("<tr>");
		sb.Append("<td><input type=text name=invoice_number maxlength=49></td>");
		sb.Append("<td><input type=text name=invoice_date maxlength=49 value='" + DateTime.Now.ToString("dd-MM-yyyy") + "'");
		sb.Append(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=f.invoice_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
		sb.Append("></td>");
		sb.Append("<td align=left>");
		sb.Append("<input type=text name=amount maxlength=49 ");
		sb.Append("></td>");
		sb.Append("<td align=left>");
		sb.Append("<input type=text name=levy maxlength=49 ");
		sb.Append("></td>");
		sb.Append("<td align=left><input type=text name=total maxlength=49></td>");
		sb.Append("<td align=right><input type=submit name=cmd value='Add' " + Session["button_style"] + "></td>");
		sb.Append("</tr>");
	}

//	sb.Append("<tr><td colspan=6 align=right>");
//	sb.Append("<input type=submit name=cmd value='Recalculate' " + Session["button_style"] + ">");
//	sb.Append("<br><br><br>");
//	sb.Append("</td></tr>");

	sb.Append("<tr><td colspan=6>&nbsp;<br><br><br></td></tr>");
	sb.Append("</table>");
*/
	Response.Write("<br><center><h3>GST Transaction</h3></center>");
	Response.Write("<form name=f action='"+ Request.ServerVariables["URL"] +"");
	if(Session["expense_customer"] != null && Session["expense_customer"] != "")
		Response.Write("?sid="+ Session["expense_customer"] +"");
	Response.Write("' method=post>");

	//hidden values
	Response.Write("<input type=hidden name=id value=" + m_id + ">");

	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td valign=top>");

		Response.Write("<table>");
		//branch
		Response.Write("<tr><td><b>Branch : </b></td>");
		Response.Write("<td>");
		if(!PrintBranchNameOptions())
			return false;
		Response.Write("</td></tr>");
string uri = Request.ServerVariables["URL"] +"?sid=";
		//payee
		Response.Write("\r\n<tr><td><b>Payee : </b></td><td>");
		if(Session["expense_customer"] != null && Session["expense_customer"] != "")
			Response.Write(PrintSupplierOptions(Session["expense_customer"].ToString(), "", "others"));
		else
			Response.Write(PrintSupplierOptions("", "", "others"));

		string last_uri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
		Response.Write("<input type=button name=cmd value='Add New Payee' " + Session["button_style"] + "");
		Response.Write(" onclick=\"window.location=('ecard.aspx?a=new&n=others&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&luri="+ last_uri +"')\" ");
		Response.Write(">");
		//Response.Write(" <i><font color=red>(please refresh browser to get a new added payee)</font></i></td></tr>");

		//from account
		Response.Write("<tr><td><b>From Account : </b></td><td>");
		if(!PrintFromAccountList())
			return false;
		Response.Write("<input type=button name=cmd value='Add New ACC' " + Session["button_style"] + "");
		Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=1&luri="+last_uri+"')\" ");
		Response.Write(">");
		Response.Write("</td></tr>");

		//To account
		Response.Write("<tr><td><b>Account Location: </b></td><td>");
		if(!PrintToAccountList())
			return false;
		Response.Write("<input type=button name=cmd value='Add New ACC' " + Session["button_style"] + "");
		Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=6&luri="+last_uri+"')\" ");
		Response.Write(">");
		Response.Write("</td></tr>");
		Response.Write("</table>");

	Response.Write("</td><td align=right valign=top>");

		//payment table
		Response.Write("\r\n<table border=0>");
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
		Response.Write("\r\n<tr><td align=right><b>Payment Date : </b></td><td>");
		Response.Write("<input type=text name=payment_date value='" + m_paymentDate + "' style='text-align:right'");
		Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=f.payment_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
		Response.Write("></td></tr>");
		
		Response.Write("\r\n<tr><td align=right><b>Custom Provided ID# : </b></td><td>");
		Response.Write("<input type=text name=client_code value='"+ Request.Form["client_code"] +"' style='text-align:right'></td></tr>");
		
		Response.Write("\r\n<tr><td align=right><b>Date Payment Due# : </b></td><td>");
		Response.Write("<input type=text name=payment_due_date value='"+ m_paymentDate +"' style='text-align:right'");
		Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=f.payment_due_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
		Response.Write("></td></tr>");

		Response.Write("\r\n<tr><td align=right><b>Amount : </b></td><td>");
		Response.Write("\r\n<input type=text name=total_amount onclick=\"if(this.value=='')this.value=" + dSubTotal + ";\" ");
		Response.Write("value=" + dSubTotal.ToString());
		Response.Write(" style='text-align:right'></td></tr>");
		Response.Write("\r\n</table>");

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");

	Response.Write(sb.ToString());

	Response.Write("</td></tr>");

	Response.Write("<tr bgcolor=#EEEEE><td colspan=2><b>Note : </b><br>");
	Response.Write("<textarea name=note rows=3 cols=50>" + m_note + "</textarea>");
//	Response.Write("</td>");
//	Response.Write("</tr>");

//	Response.Write("<td align=left valign=bottom>");
//	Response.Write("<table border=1>");
//	Response.Write("<tr><td>");
//	Response.Write("<font color=red><b> Auto Repeat : </b></font>");
//	Response.Write("<select name=autopayment_frequency>");
//	Response.Write(GetEnumOptions("autopayment_frequency", m_sAutoFrequency));
//	Response.Write("</select> <a href=autopay.aspx class=o>Auto Payment List</a>");
//	Response.Write("</td></tr>");
//	Response.Write("</table>");
	Response.Write("</td></tr>");

	if(m_bRecorded)
	{
		Response.Write("<tr><td>");
		Response.Write("<font color=red><b> Auto Repeat : </b></font>");
		Response.Write("<select name=autopayment_frequency>");
		Response.Write(GetEnumOptions("autopayment_frequency", m_sAutoFrequency));
		Response.Write("</select> Next Payment Date : <input type=text size=10 name=next_payment_date value='" + m_sNextAutoDate + "'>");
		Response.Write(" <a href=explist.aspx?autolist=1 class=o target=_blank>Auto Payment List</a>");
		Response.Write("</td></tr>");
	}

	Response.Write("<tr bgcolor=#EEEEE><td colspan=2 align=right>");
	
/*	Response.Write("<b>Type Record:</b> <select name=ispaid>");
	Response.Write("<option value=0 ");
	if(!m_bIsPaid)
		Response.Write(" selected ");
	Response.Write(">Record Only");
	Response.Write("<option value=1 ");
	if(m_bIsPaid)
		Response.Write(" selected  ");
	Response.Write(">Paid Custom GST");
	Response.Write("</select> ");
*/
	if(m_bRecorded)
	{
		Response.Write("<input type=checkbox name=confirm_record>Tick to update ");
		Response.Write("<input type=submit name=cmd value='Update Record' " + Session["button_style"] + "");
		Response.Write(" onclick=\"if(document.f.supplier.value == '' || !document.f.confirm_record.checked){window.alert('Please Select Payee & Tick to Record check box');return false;}else if(!confirm('Process now...')){return false;}\" ");
		Response.Write(">");
		Response.Write("<input type=button value='View Transaction' onclick=window.location=('custax_rp.aspx') " + Session["button_style"] + ">");
	}
	else
	{
		
		Response.Write("<input type=checkbox name=confirm_record>Tick to record ");
		Response.Write("<input type=submit name=cmd value=Record " + Session["button_style"] + "");
		Response.Write(" onclick=\"return checkinput(); \" "); //if(document.f.supplier.value == '' || !document.f.confirm_record.checked){window.alert('Please Select Payee & Tick to Record check box');return false;}else if(!confirm('Process now...')){return false;}\" ");
		Response.Write(">");
	}
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");

	Response.Write("<script language=javascript1.2>");
	Response.Write("<!--- hide old browser ");

	string sjava = @"
		function checkinput()
	{
		var bInvalid;
		bInvalid = false;
		if(document.f.supplier.value == '')
			bInvalid = true;
		if(document.f.total_amount.value == '0' || document.f.total_amount.value == '')
			bInvalid = true;
		if(document.f.client_code.value == '')
			bInvalid = true;
		if(!document.f.confirm_record.checked)
			bInvalid = true;
		if(bInvalid)
		{
			window.alert('Please Select Payee, Enter Custom Provided ID#, Amount and Check the Record Box!!');
			return false;
		}
		else
		{
			if(!confirm('Process Now...'))
				return false;
		}
		return true;

	}
	function isNumber(sInput)
	{
	   var sValidChars = '0123456789.';
	   var bisNum=true;
	   var Char;
	   for (i = 0; i < sInput.length && bisNum == true; i++) 
	   { 
		  Char = sInput.charAt(i); 
		  if (sValidChars.indexOf(Char) == -1) 
		  {
			bisNum = false;
		  }
	   }
	   return bisNum;
   	}
	";
	Response.Write("-->");
	Response.Write(sjava);
	Response.Write("</script");
	Response.Write(">");
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
	sc += " WHERE class1 = 2 ";
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
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?cid=" + search_id);
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
	Response.Write("<form id=search action="+ Request.ServerVariables["URL"] +" method=post>");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	Response.Write("<input type=button name=cmd value='Cancel'");
	Response.Write(" onClick=window.location=('"+ Request.ServerVariables["URL"] +"') " + Session["button_style"] + ">");
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

		Response.Write("<td><a href="+ Request.ServerVariables["URL"] +"?cid=" + id + ">");
		Response.Write(id + "</a></td>\r\n");

		Response.Write("<td><a href="+ Request.ServerVariables["URL"] +"?cid=" + id + ">");
		Response.Write(trading_name + "</a></td>");

		Response.Write("<td><a href="+ Request.ServerVariables["URL"] +"?cid=" + id + ">");
		Response.Write(name + "</a></td>");

		Response.Write("<td><a href="+ Request.ServerVariables["URL"] +"?cid=" + id + ">");
		Response.Write(email + "</a></td>");

		Response.Write("<td>" + phone + "</td>");
		Response.Write("<td align=right>" + balance + "</td>");

		Response.Write("</tr>");
	}	
	Response.Write("</table>");
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

bool CheckAutoPaymentChanges()
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

bool GetNextChequeNumber()
{
	string sc = " SELECT TOP 1 c.payment_ref FROM custom_tax c ";
	sc += " WHERE c.payment_ref <> '' AND c.payment_type = 2 ";
	sc += " ORDER BY c.payment_ref DESC ";
//DEBUG(" sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "chequeID") == 1)
		{
			m_nextChequeNumber = (MyDoubleParse(dst.Tables["chequeID"].Rows[0]["payment_ref"].ToString()) + 1).ToString();
		}
		
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid object name 'custom_tax'") >= 0)
		{
			myCommand.Connection.Close(); //close it first
			if(!CreatTableCustomTax()) //creat new table
				return false;
			//db updated, try again
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e1)
			{
				ShowExp(sc, e1);
				return false;
			}
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}

	}
	return true;
}

</script>

<script runat=server>

DataSet dst = new DataSet();
DataTable dtAssets = new DataTable();

string m_id = "";
bool m_bRecorded = false;

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

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	if(Request.Form["cmd"] != null)
		UpdateAllFields();
	RestoreAllFields();

	m_nextChequeNumber = GetSiteSettings("next_cheque_number", "1000");

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_id = Request.QueryString["id"];
		EmptyAssetsTable();
		if(!RestoreRecord())
			return;
		m_bRecorded = true;
		Session["assets_current_id"] = m_id;
		Session["assets_recorded"] = true;
	}
	else if(Request.Form["id"] != null && Request.Form["id"] != "")
	{
		m_id = Request.Form["id"];
		m_bRecorded = true;
		Session["assets_current_id"] = m_id;
		Session["assets_recorded"] = true;
	}
	if(Request.QueryString["t"] == "new")
	{
		EmptyAssetsTable();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=assets.aspx\">");
		return;
	}
	else if(Request.QueryString["t"] == "del")
	{
		CheckAssetsTable();
		dtAssets.Rows.RemoveAt(MyIntParse(Request.QueryString["row"]));
	}
	else if(Request.QueryString["t"] == "edit")
	{
		m_editRow = Request.QueryString["row"];
	}
	else if(Request.QueryString["saydone"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Done, assets saved");
		if(m_id != "")
		{
			Response.Write(", please wait 1 second...<meta http-equiv=\"refresh\" content=\"2; URL=assets.aspx?id=" + m_id + "\">");
		}
		else
		{
			Response.Write("<input type=button value='Assets List' onclick=window.location=('asstlist.aspx') " + Session["button_style"] + ">");
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
		Session["assets_customer"] = m_customerID;
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
		DoUpdateAssetsRow();
	}
	else if(Request.Form["cmd"] == "Search Card")
	{
		Response.Redirect("assets.aspx?search=1");
		return;
	}
	else if(Request.Form["cmd"] == "Add")
	{
		if(!DoAddAssets())
			return;
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
		if(DoRecordAssets())
			Response.Redirect("assets.aspx?saydone=1&id=" + m_id);
		return;
	}
	else if(Request.Form["cmd"] == "Update Record")
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
		if(DoUpdateRecord())
			Response.Redirect("assets.aspx?saydone=1&id=" + m_id);
		return;
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
		Session["assets_branch"] = Request.Form["branch"];
	Session["assets_customer"] = Request.Form["customer"];
	Session["assets_from_account"] = Request.Form["from_account"];
	Session["assets_to_account"] = Request.Form["to_account"];
	Session["assets_payment_type"] = Request.Form["payment_type"];
	Session["assets_payment_ref"] = Request.Form["payment_ref"];
	Session["assets_payment_date"] = Request.Form["payment_date"];
	Session["assets_note"] = Request.Form["note"];
}

void RestoreAllFields()
{
	if(Session["assets_branch"] != null)
		m_branch = Session["assets_branch"].ToString();
	if(Session["assets_customer"] != null)
	{
		m_customerID = Session["assets_customer"].ToString();
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
	if(Session["assets_from_account"] != null)
		m_fromAccount = Session["assets_from_account"].ToString();
	if(Session["assets_to_account"] != null)
		m_toAccount = Session["assets_to_account"].ToString();
	if(Session["assets_payment_type"] != null)
		m_paymentType = Session["assets_payment_type"].ToString();
	if(Session["assets_payment_ref"] != null)
		m_paymentRef = Session["assets_payment_ref"].ToString();
	if(Session["assets_payment_date"] != null)
		m_paymentDate = Session["assets_payment_date"].ToString();
	if(Session["assets_note"] != null)
		m_note = Session["assets_note"].ToString();
	if(Session["assets_recorded"] != null)
		m_bRecorded = true;
	if(Session["assets_current_id"] != null)
		m_id = Session["assets_current_id"].ToString();

	if(m_paymentDate == "")
		m_paymentDate = DateTime.Now.ToString("dd-MM-yyyy");
}

bool CheckAssetsTable()
{
	if(Session["AssetsTable"] == null) 
	{
		dtAssets.Columns.Add(new DataColumn("name", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("invoice_number", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("invoice_date", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("tax", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("total", typeof(String)));
		Session["AssetsTable"] = dtAssets;
		return false;
	}
	else
	{
		dtAssets = (DataTable)Session["AssetsTable"];
	}
	return true;
}

void EmptyAssetsTable()
{
	CheckAssetsTable();
	for(int i=dtAssets.Rows.Count - 1; i>=0; i--)
		dtAssets.Rows.RemoveAt(i);

	//clear session objects for Assets
	Session["assets_branch"] = null;
	Session["assets_customer"] = null;
	Session["assets_from_account"] = null;
	Session["assets_to_account"] = null;
	Session["assets_payment_type"] = null;
	Session["assets_payment_ref"] = null;
	Session["assets_payment_date"] = null;
	Session["assets_note"] = null;
	Session["assets_recorded"] = null;
	Session["assets_current_id"] = null;
}

bool PrintBody()
{
	double dSubTotal = 0;

	StringBuilder sb = new StringBuilder();

	//main list
	sb.Append("\r\n<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("\r\n<th nowrap>Description</th>");
	sb.Append("\r\n<th nowrap>Invoice Number</th>");
	sb.Append("\r\n<th nowrap>Invoice Date</th>");
	sb.Append("\r\n<th>Amount</th>");
	sb.Append("\r\n<th width=70>TAX</th>");
	sb.Append("\r\n<th>Total</th>");
	sb.Append("<th>&nbsp;</th>");
	sb.Append("</tr>\r\n");

	CheckAssetsTable();

	for(int i=0; i<dtAssets.Rows.Count; i++)
	{
		DataRow dr = dtAssets.Rows[i];
		string name = dr["name"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = dr["invoice_date"].ToString();
		string tax = dr["tax"].ToString();
		//DEBUG("tax = ", tax);
		string total = dr["total"].ToString();
		double dTax = MyDoubleParse(tax);
		double dTotal = MyDoubleParse(total);
		double dAmount = dTotal - dTax;

		dSubTotal += dTotal;
		if(m_editRow == i.ToString())
		{
			sb.Append("<input type=hidden name=edit_row value=" + i + ">");
			sb.Append("<td><input type=text name=name maxlength=255 value='" + name + "'></td>");
			sb.Append("<td><input type=text name=invoice_number size=15 maxlength=49 value='" + invoice_number + "'></td>");
			sb.Append("<td><input type=text name=invoice_date size=10 maxlength=49 value='" + invoice_date + "'></td>");
			sb.Append("<td align=right><input type=text name=amount maxlength=49></td>");
			sb.Append("<td align=center><input type=checkbox name=tax");
			if(dTax != 0)
				sb.Append(" checked");
			sb.Append("></td>");
			sb.Append("<td align=right><input type=text name=total size=10 maxlength=49 value=" + total + "></td>");
			sb.Append("<td align=right><input type=submit name=cmd value='OK' " + Session["button_style"] + "></td>");
		}
		else
		{
			sb.Append("<tr>");
			sb.Append("<td>" + name + "</td>");
			sb.Append("<td>" + invoice_number + "</td>");
			sb.Append("<td>" + invoice_date + "</td>");
			sb.Append("<td align=right>" + dAmount.ToString("c") + "</td>");
			sb.Append("<td align=right>" + dTax.ToString("c") + "</td>");
			sb.Append("<td align=right>" + dTotal.ToString("c") + "</td>");
			sb.Append("<td align=right>");
			sb.Append("<a href=assets.aspx?t=edit&row=" + i + " class=o>EDIT</a> ");
			sb.Append("<a href=assets.aspx?t=del&row=" + i + " class=o>DEL</a> ");
			sb.Append("</tr>");
		}
	}
	
	if(m_editRow == "")
	{
		sb.Append("<tr>");
		sb.Append("<td><input type=text name=name maxlength=255></td>");
		sb.Append("<td><input type=text name=invoice_number size=15 maxlength=49></td>");
		sb.Append("<td><input type=text name=invoice_date size=10 maxlength=49 value='" + DateTime.Now.ToString("dd-MM-yyyy") + "'></td>");
		sb.Append("<td align=right><input type=text name=amount size=10 maxlength=49></td>");
		sb.Append("<td align=center><input type=checkbox name=tax checked></td>");
		sb.Append("<td align=right><input type=text name=total size=10 maxlength=49></td>");
		sb.Append("<td align=right><input type=submit name=cmd value='Add' " + Session["button_style"] + "></td>");
		sb.Append("</tr>");
	}

//	sb.Append("<tr><td colspan=6 align=right>");
//	sb.Append("<input type=submit name=cmd value='Recalculate' " + Session["button_style"] + ">");
//	sb.Append("<br><br><br>");
//	sb.Append("</td></tr>");

	sb.Append("<tr><td colspan=6>&nbsp;<br><br><br></td></tr>");
	sb.Append("</table>");

	Response.Write("<br><center><h3>Fixed Assets</h3></center>");
	Response.Write("<form name=f action=assets.aspx method=post>");

	//hidden values
	Response.Write("<input type=hidden name=id value=" + m_id + ">");

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=0 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td valign=top>");

		Response.Write("<table>");

		if(Session["branch_support"] != null)
		{
			//branch
			Response.Write("<tr><td><b>Branch : </b></td>");
			Response.Write("<td>");
			if(!PrintBranchNameOptions())
				return false;
			Response.Write("</td></tr>");
		}

		//payee
		Response.Write("\r\n<tr><td><b>Payee : </b></td><td>");
		Response.Write("<input type=hidden name=customer value=" + m_customerID + ">");
		if(m_customerID != "" && m_customerID != "0")
			Response.Write("<input type=text name=custoemr_name value='" + m_customerName + "' readonly=true>");
		Response.Write(" ");
		if(m_customerID.Length > 2)
		{
			Response.Write("<input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + m_customerID + "','','width=350,height=340');\" value='View Card' " + Session["button_style"] + ">");
		}
		Response.Write("<input type=submit name=cmd value='Search Card' " + Session["button_style"] + ">");
		Response.Write("</td></tr>");

		//from account
/*		Response.Write("<tr><td><b>From Account : </b></td><td>");
		if(!PrintFromAccountList())
			return false;
		Response.Write("</td></tr>");
*/
		//To account
		Response.Write("<tr><td><b>Assets Type : </b></td><td>");
		if(!PrintToAccountList())
			return false;
		Response.Write("</td></tr>");
		Response.Write("</table>");

	Response.Write("</td><td align=right valign=top>");
/*
		//payment table
		Response.Write("\r\n<table>");
		Response.Write("<tr><td><b>Payment Type : </b></td>");
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
		Response.Write("<input type=text name=payment_date value='" + m_paymentDate + "' style='text-align:right'></td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Amount : </b></td><td>");
		Response.Write("\r\n<input type=text name=total_amount onclick=\"if(this.value=='')this.value=" + dSubTotal + ";\" ");
		Response.Write("value=" + dSubTotal.ToString());
		Response.Write(" style='text-align:right'></td></tr>");
		Response.Write("\r\n</table>");
*/
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");

	Response.Write(sb.ToString());

	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2><b>Note : </b><br>");
	Response.Write("<textarea name=note rows=3 cols=50>" + m_note + "</textarea>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2 align=right>");
	if(m_bRecorded)
	{
		Response.Write("<input type=checkbox name=confirm_record>Tick to update ");
		Response.Write("<input type=submit name=cmd value='Update Record' " + Session["button_style"] + ">");
		Response.Write("<input type=button value='Assets List' onclick=window.location=('asstlist.aspx') " + Session["button_style"] + ">");
	}
	else
	{
		Response.Write("<input type=checkbox name=confirm_record>Tick to record ");
		Response.Write("<input type=submit name=cmd value=Record " + Session["button_style"] + ">");
	}
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
	sc += " WHERE class1=1 AND class2=3 ";
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
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=assets.aspx?cid=" + search_id);
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
	Response.Write("<form id=search action=assets.aspx method=post>");
	Response.Write("<table width=100/%><tr><td>");
	Response.Write("<input type=editbox size=7 name=ckw></td><td>");
	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	Response.Write("<input type=button name=cmd value='Cancel'");
	Response.Write(" onClick=window.location=('assets.aspx') " + Session["button_style"] + ">");
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

		Response.Write("<td><a href=assets.aspx?cid=" + id + ">");
		Response.Write(id + "</a></td>\r\n");

		Response.Write("<td><a href=assets.aspx?cid=" + id + ">");
		Response.Write(trading_name + "</a></td>");

		Response.Write("<td><a href=assets.aspx?cid=" + id + ">");
		Response.Write(name + "</a></td>");

		Response.Write("<td><a href=assets.aspx?cid=" + id + ">");
		Response.Write(email + "</a></td>");

		Response.Write("<td>" + phone + "</td>");
		Response.Write("<td align=right>" + balance + "</td>");

		Response.Write("</tr>");
	}	
	Response.Write("</table>");
}

bool DoAddAssets()
{
	string name = Request.Form["name"];
	string invoice_number = Request.Form["invoice_number"];
	string invoice_date = Request.Form["invoice_date"];
	string amount = Request.Form["amount"];
	string total = Request.Form["total"];
	double dAmount = 0;
	double dTotal = 0;
	
	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	DateTime tInvoice;
	string msg = "";
	if(amount != "")
	{
		try
		{
			dAmount = Double.Parse(amount, NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
			msg = "Amount. Input string <font color=red>" + amount + "</font> was not in a correct format";
		}
	}
	if(total != "")
	{
		try
		{
			dTotal = Double.Parse(total, NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
			msg = "Total. Input string <font color=red>" + total + "</font> was not in a correct format";
		}
	}
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

	if(msg != "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><br><center><h3>Error, " + msg + "</h3>");
		Response.Write("<br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}

	CheckAssetsTable();

	DataRow dr = dtAssets.NewRow();
	dr["name"] = name;
	dr["invoice_number"] = invoice_number;
	dr["invoice_date"] = invoice_date;
	dr["tax"] = dTax.ToString();
	dr["total"] = dTotal.ToString();

	dtAssets.Rows.Add(dr);
	return true;
}

bool DoUpdateAssetsRow()
{
	CheckAssetsTable();
	int row = MyIntParse(Request.Form["edit_row"]);
	if(row >= dtAssets.Rows.Count)
		return false;
	string name = Request.Form["name"];
	string invoice_number = Request.Form["invoice_number"];
	string invoice_date = Request.Form["invoice_date"];
	string amount = Request.Form["amount"];
	string total = Request.Form["total"];
	double dAmount = 0;
	double dTotal = 0;
	
	System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
	DateTime tInvoice;
	string msg = "";
	if(amount != "")
	{
		try
		{
			dAmount = Double.Parse(amount, NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
			msg = "Amount. Input string <font color=red>" + amount + "</font> was not in a correct format";
		}
	}
	if(total != "")
	{
		try
		{
			dTotal = Double.Parse(total, NumberStyles.Currency, null);
		}
		catch(Exception e)
		{
			msg = "Total. Input string <font color=red>" + total + "</font> was not in a correct format";
		}
	}
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

	if(msg != "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><br><center><h3>Error, " + msg + "</h3>");
		Response.Write("<br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}

	DataRow dr = dtAssets.Rows[row];
	dr["name"] = name;
	dr["invoice_number"] = invoice_number;
	dr["invoice_date"] = invoice_date;
	dr["tax"] = dTax.ToString();
	dr["total"] = dTotal.ToString();

	dtAssets.AcceptChanges();
	return true;
}

bool DoRecordAssets()
{
	CheckAssetsTable();
	if(dtAssets.Rows.Count == 0)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, no Assets to record</h3>");
		Response.Write("<br><br><br><br><br><br><br><br><br>");
		PrintAdminFooter();
		return false;
	}
	string new_id = "";

	//get new id
	string sc = " SET DATEFORMAT dmy ";
	sc += " BEGIN TRANSACTION ";
	sc += " INSERT INTO assets (card_id, to_account, recorded_by) ";
	sc += " VALUES(" + m_customerID;
	sc += ", " + m_toAccount;
	sc += ", " + Session["card_id"].ToString();
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('assets') AS id";
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
			Response.Write("<br><br><center><h3>Error recording assets, failed to get new id</h3>");
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
	for(int i=0; i<dtAssets.Rows.Count; i++)
	{
		DataRow dr = dtAssets.Rows[i];
		string name = dr["name"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = dr["invoice_date"].ToString();
		double dTax = MyDoubleParse(dr["tax"].ToString());
		double dTotal = MyDoubleParse(dr["total"].ToString());
		
		dTax = Math.Round(dTax, 2);
		dTotal = Math.Round(dTotal, 2);
		dSubTax += dTax;
		dSubTotal += dTotal;

		sc += " INSERT INTO assets_item (id, name, invoice_number, invoice_date, tax, total) ";
		sc += " VALUES(" + new_id + ", '" + EncodeQuote(name) + "', '" + EncodeQuote(invoice_number) + "' ";
		sc += ", '" + invoice_date + "' ";
		sc += ", " + dTax + ", " + dTotal + ") ";
	}
	sc += " UPDATE assets SET ";
	sc += " tax = " + dSubTax;
	sc += ", total = " + dSubTotal;
	sc += ", note = '" + EncodeQuote(m_note) + "' ";
	sc += " WHERE id = " + new_id;

//	sc += " UPDATE account SET balance = balance - " + dSubTotal + " WHERE id = " + m_fromAccount;
	sc += " UPDATE account SET balance = balance + " + dSubTotal + " WHERE id = " + m_toAccount;
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
*/
	EmptyAssetsTable();
	return true;
}

bool RestoreRecord()
{
	string sc = " SELECT e.* ";
	sc += ", i.kid, i.name, i.invoice_number, i.invoice_date, i.tax AS item_tax, i.total AS item_total ";
	sc += " FROM assets e ";
	sc += " JOIN assets_item i ON i.id=e.id ";
	sc += " WHERE e.id = " + m_id;
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
//	m_fromAccount = dr["from_account"].ToString();
	m_toAccount = dr["to_account"].ToString();
//	m_paymentType = dr["payment_type"].ToString();
//	m_paymentRef = dr["payment_ref"].ToString();
//	m_paymentDate = DateTime.Parse(dr["payment_date"].ToString()).ToString("dd-MM-yyyy");
	m_note = dr["note"].ToString();
	DataRow drc = GetCardData(m_customerID);
	if(drc != null)
	{
		m_customerName = drc["trading_name"].ToString();
		if(m_customerName == "")
			m_customerName = drc["company"].ToString();
		if(m_customerName == "")
			m_customerName = drc["name"].ToString();
	}

	Session["assets_branch"] = m_branch;
	Session["assets_customer"] = m_customerID;
	Session["assets_from_account"] = m_fromAccount;
	Session["assets_to_account"] = m_toAccount;
	Session["assets_payment_type"] = m_paymentType;
	Session["assets_payment_ref"] = m_paymentRef;
	Session["assets_payment_date"] = m_paymentDate;
	Session["assets_note"] = m_note;

	for(int i=0; i<dst.Tables["restore"].Rows.Count; i++)
	{
		dr = dst.Tables["restore"].Rows[i];
		string name = dr["name"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = DateTime.Parse(dr["invoice_date"].ToString()).ToString("dd-MM-yyyy");
		string tax = dr["item_tax"].ToString();
		string total = dr["item_total"].ToString();

		DataRow dre = dtAssets.NewRow();
		dre["name"] = name;
		dre["invoice_number"] = invoice_number;
		dre["invoice_date"] = invoice_date;
		dre["tax"] = tax;
		dre["total"] = total;
		dtAssets.Rows.Add(dre);
	}

	return true;
}

bool DoUpdateRecord()
{
	string sc = " SELECT * FROM assets WHERE id = " + m_id;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "old") != 1)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><center><h3>Original record not found, cannot update</h3>");
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

	DataRow dr = dst.Tables["old"].Rows[0];
	string old_from = dr["from_account"].ToString();
	string old_to = dr["to_account"].ToString();
	double dOldTotal = MyDoubleParse(dr["total"].ToString());


	//do update first
	string payment_date = Request.Form["payment_date"];

	CheckAssetsTable();

	//record items
	sc = " DELETE FROM assets_item WHERE id = " + m_id;
	sc += " SET DATEFORMAT dmy ";
	double dSubTax = 0;
	double dSubTotal = 0;
	for(int i=0; i<dtAssets.Rows.Count; i++)
	{
		dr = dtAssets.Rows[i];
		string name = dr["name"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string invoice_date = dr["invoice_date"].ToString();
		double dTax = MyDoubleParse(dr["tax"].ToString());
		double dTotal = MyDoubleParse(dr["total"].ToString());
		
		dTax = Math.Round(dTax, 2);
		dTotal = Math.Round(dTotal, 2);
		dSubTax += dTax;
		dSubTotal += dTotal;

		sc += " INSERT INTO assets_item (id, invoice_number, invoice_date, tax, total) ";
		sc += " VALUES(" + m_id + ", '" + EncodeQuote(invoice_number) + "', '" + invoice_date + "' ";
		sc += ", " + dTax + ", " + dTotal + ") ";
	}
	sc += " UPDATE assets SET ";
	sc += " card_id = " + m_customerID;
	if(m_fromAccount != null && m_fromAccount != "")
		sc += ", from_account = " + m_fromAccount;
	if(m_toAccount != null && m_toAccount != "")
		sc += ", to_account = " + m_toAccount;
	sc += ", payment_type = " + m_paymentType;
	sc += ", payment_ref = '" + m_paymentRef + "' ";
	sc += ", note = '" + EncodeQuote(m_note) + "' ";
	sc += ", last_edit_by = " + Session["card_id"];
	sc += ", last_edit_time = GETDATE() ";
	sc += ", tax = " + dSubTax;
	sc += ", total = " + dSubTotal;
	sc += " WHERE id = " + m_id;

//	sc += " UPDATE account SET balance = balance + " + dOldTotal + " WHERE id = " + old_from;
	sc += " UPDATE account SET balance = balance - " + dOldTotal + " WHERE id = " + old_to;
//	sc += " UPDATE account SET balance = balance - " + dSubTotal + " WHERE id = " + m_fromAccount;
	sc += " UPDATE account SET balance = balance + " + dSubTotal + " WHERE id = " + m_toAccount;
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
	EmptyAssetsTable();

	return true;
}
</script>

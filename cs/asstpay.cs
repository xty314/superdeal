<script runat=server>

int page = 1;
const int m_nPageSize = 15; //how many rows in oen page

string m_account = "";
string m_inv = "";
string m_tranid = "";
string m_paymentType = "2";
string m_paymentRef = "";
string m_nextChequeNumber = "";
bool m_bPayAll = false;

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

	if(Request.QueryString["acc"] != null)
	{
		m_account = Request.QueryString["acc"];
		if(!SetSiteSettings("assets_account", m_account))
			return;
	}
	
	if(Request.Form["cmd"] == "Record")
	{
		if(Session["in_asstpay"] == null)
		{
			Response.Write("<br><br><center><h3><font color=red>STOP!</font> Repost Form is forbidden");
			PrintAdminFooter();
			return;
		}
		Session["in_asstpay"] = null;
		if(Request.Form["confirm_record"] != "on")
		{
			Response.Write("<br><br><center><h3>Please tick 'Tick to record'</h3>");
			return;
		}
		if(RecordTransaction())
		{
			Response.Write("<br><br><center><h4>Payment Recorded, please wait a second ... </h4>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=asstpay.aspx\">");
		}
	}
	else
	{
		if(!DoSearch())
			return;
		MyDrawTable();
		Session["in_asstpay"] = true;
	}
	
	PrintAdminFooter();
}

bool RecordTransaction()
{
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

	myCommand1.Parameters.Add("@nSource", SqlDbType.Int).Value = Request.Form["account"];
	myCommand1.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand1.Parameters.Add("@payment_method", SqlDbType.Int).Value = m_paymentType;
	myCommand1.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = EncodeQuote(m_paymentRef);
	myCommand1.Parameters.Add("@note", SqlDbType.VarChar).Value = Request.Form["note"];
	myCommand1.Parameters.Add("@currency_loss", SqlDbType.Money).Value = dCurrencyLoss;
	myCommand1.Parameters.Add("@finance", SqlDbType.Money).Value = dFinance;
	myCommand1.Parameters.Add("@Amount", SqlDbType.Money).Value = dAmount;
	myCommand1.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand1.ExecuteNonQuery();
		myCommand1.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("DoAssetsPayment", e);
		return false;
	}

	m_tranid = myCommand1.Parameters["@return_tran_id"].Value.ToString();

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

	string sc = "";
	int i = 0;
	string id = Request.Form["id" + i];
	while(id != null)
	{
		double d_balance = MyMoneyParse(Request.Form["owed" + i]);
		d_balance = Math.Round(d_balance, 2);

		double d_applied = 0;
		if(Request.Form["applied_amount" + i] != "" && Request.Form["applied_amount" + i] != null)
		{
			d_applied = MyMoneyParse(Request.Form["applied_amount" + i]);
			d_applied = Math.Round(d_applied, 2);
		}

		//record applied invoice number and amount applied to each
		if(d_applied != 0 || d_balance == 0)
		{
			sc += " UPDATE assets SET amount_paid = amount_paid + " + d_applied + " WHERE id = " + id + "\r\n";
			sc += " INSERT INTO assets_payment (assets_id, amount_applied, tran_id) ";
			sc += " VALUES(" + id + ", " + d_applied + ", " + m_tranid + ") \r\n";
		}
		i++;
		id = Request.Form["id" + i];
	}
	if(sc != "")
	{
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
	return true;
}

bool DoSearch()
{
	string sc = " SELECT a.*, acc.name4 AS asset_type ";//, i.invoice_number, i.invoice_date, i.tax AS item_tax, i.total AS item_total ";
	sc += " FROM assets a LEFT OUTER JOIN account acc ON acc.id = a.to_account ";//JOIN assets_item i ON i.id=a.id ";
	sc += " WHERE a.amount_paid <> a.total ";
	sc += " ORDER BY a.date_recorded ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "assets");
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
		m_account = GetSiteSettings("assets_account");

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
	Response.Write("<select name=account>");
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
	Response.Write("<br><center><h3>Pay Assets</h3></center>");
	Response.Write("<form name=f action=asstpay.aspx method=post>");
	Response.Write("<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td>");

	//account list table
	if(!PrintAccountList())
		return false;

	Response.Write("</td></tr><tr><td>");

	double dTotal = 0;
	StringBuilder sb = new StringBuilder();
	for(int i=0; i<dst.Tables["assets"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["assets"].Rows[i];
		string id = dr["id"].ToString();
		string names = GetAssetsItemNames(id);
		string asset_type = dr["asset_type"].ToString();
		string date = DateTime.Parse(dr["date_recorded"].ToString()).ToString("dd/MM/yyyy");
		if(dr["date_recorded"].ToString() != "")
			date = DateTime.Parse(dr["date_recorded"].ToString()).ToString("dd/MM/yyyy");
//		string inv_number = dr["invoice_number"].ToString();
		string amount = dr["total"].ToString();
		double dAmount = MyMoneyParse(amount);
		double dPaid = MyDoubleParse(dr["amount_paid"].ToString());
		double dOwed = dAmount - dPaid;
		
//		string status = GetEnumValue("general_status", dr["payment_status"].ToString());

		sb.Append("<input type=hidden name=id" + i + " value=" + id + ">");
		sb.Append("<input type=hidden name=amount" + i + " value=" + amount + ">");
		sb.Append("<input type=hidden name=owed" + i + " value=" + dOwed + ">");
		sb.Append("\r\n<tr>");
		sb.Append("\r\n<td>" + date + "</td>");
		sb.Append("<td><a href=assets.aspx?id=" + id + " target=_blank class=o>" + asset_type + "</a></td>");
		sb.Append("<td><a href=assets.aspx?id=" + id + " target=_blank class=o>" + names + "</a></td>");
		sb.Append("\r\n<td>" + dAmount.ToString("c") + "</td>");
		sb.Append("\r\n<td>" + dOwed.ToString("c") + "</td>");
		sb.Append("\r\n<td align=right>\r\n<input type=text name=applied_amount" + i.ToString());
		sb.Append(" onclick=\"if(this.value=='' || this.value=='0')\r\n");
		sb.Append("{var left=CalcApplyLeft();\r\n ");
		sb.Append("if(left>" + dOwed + "){this.value=" + dOwed + ";}else{this.value=left;}\r\n ");
		sb.Append(";CalcTotal();}\" onchange=\"CalcTotal();\"");
		if(m_bPayAll)
			sb.Append(" value=" + dOwed.ToString());
		sb.Append(" style='text-align:right'></td>\r\n");
		sb.Append("</tr>\r\n");
		dTotal += dOwed;
	}
	
	//payment / cheque table
	{
		Response.Write("\r\n<table width=100%>");
		Response.Write("\r\n<tr><td>");
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
		Response.Write("\r\n<tr><td align=right><b>Date : </b></td><td><input type=text name=date value='" + DateTime.Now.ToString("dd/MM/yyyy") + "' style='text-align:right'></td></tr>");
		Response.Write("\r\n<tr><td align=right><b>Amount : </b></td><td>");
		Response.Write("\r\n<input type=text name=amount ");
		if(m_bPayAll)
			Response.Write(" value=" + dTotal.ToString());
		Response.Write(" onclick=\"if(this.value=='' || this.value=='0')this.value=" + dTotal + ";\" ");
		Response.Write(" onchange=\"ClearAllFields();\" style='text-align:right'></td></tr>");
		Response.Write("\r\n</table>");

		Response.Write("\r\n</td></tr></table>");
	}

	Response.Write("\r\n</td></tr><tr><td>");

	//main list
	Response.Write("\r\n<table width=100% cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("\r\n<td>Date</td>\r\n");
	Response.Write("<td>Type</td>\r\n");
	Response.Write("<td>Assets</td>\r\n");
	Response.Write("\r\n<td>Total Amount</td>");
	Response.Write("\r\n<td>Total Owed</td>");
	Response.Write("\r\n<td align=center>Applied Amount</td>");
	Response.Write("</tr>\r\n");

	Response.Write(sb.ToString());
	Response.Write("</table>");
	
	Response.Write("<br><br>");

	Response.Write("</td></tr><tr><td>");

	double dzero = 0;
	//paid table and buttons
	Response.Write("<table width=100%>");
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

	Response.Write("<tr><td colspan=2><b>Note : </b></td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=note rows=3 cols=50></textarea></td></tr>");

	Response.Write("<tr><td><button " + Session["button_style"] + " ");
	Response.Write(" onclick=window.location=('asstpay.aspx?t=payall')>Pay All</button></td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=checkbox name=confirm_record>Tick to record ");
	Response.Write("<input type=submit name=cmd value=Record " + Session["button_style"] + ">");
	Response.Write("<input type=button " + Session["button_style"]);
	Response.Write(" value=Cancel onclick=window.location=('asstpay.aspx')>");

	Response.Write("</table>");

	Response.Write("</td></tr></table>");

	Response.Write("\r\n<input type=hidden name=invoice_number value='" + m_inv + "'>\r\n");
	Response.Write("</form>");

	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function CalcTotal()");
	Response.Write("{ var total = 0;");
	for(int i=0; i<dst.Tables["assets"].Rows.Count; i++)
		Response.Write("	total += Number(document.f.applied_amount" + i.ToString() + ".value);\r\n");
	Response.Write("	total = Math.round(total * 100) / 100;\r\n");

	Response.Write("var amount = Number(document.f.amount.value);");
	Response.Write("var finance = Number(document.f.finance.value);");
	Response.Write("var currency_loss = Number(document.f.currency_loss.value);");
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
	for(int i=0; i<dst.Tables["assets"].Rows.Count; i++)
		Response.Write("	left -= Number(document.f.applied_amount" + i.ToString() + ".value);\r\n");
	Response.Write("	return Math.round(left * 100) / 100;\r\n");
	Response.Write("}\r\n");

	Response.Write("function ClearAllFields(){\r\n");
	for(int i=0; i<dst.Tables["assets"].Rows.Count; i++)
		Response.Write("	document.f.applied_amount" + i.ToString() + ".value = '';\r\n");
	Response.Write("	CalcTotal();\r\n ");
	Response.Write("}\r\n");
	Response.Write("</script");
	Response.Write(">");

	return true;
}

string GetAssetsItemNames(string assets_id)
{
	if(dst.Tables["item_names"] != null)
		dst.Tables["item_names"].Clear();

	int nRows = 0;
	string names = "";
	string sc = " SELECT name FROM assets_item WHERE id = " + assets_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "item_names");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	for(int i=0; i<nRows; i++)
	{
		names += dst.Tables["item_names"].Rows[i]["name"].ToString();
		if(i < nRows - 1)
			names += ",";
	}
	return names;
}
</script>
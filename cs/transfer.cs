<script runat=server>

string m_fromAcc = "";
string m_toAcc = "";
string m_tranid = "";
string m_paymentType = "2";
string m_paymentRef = "";
int m_nAccounts = 0;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	
	if(Request.Form["cmd"] == "Transfer")
	{
		if(Session["in_transfer"] == null)
		{
			Response.Write("<br><br><center><h3><font color=red>STOP!</font> Repost Form is forbidden");
			PrintAdminFooter();
			return;
		}
		Session["in_transfer"] = null;
		if(Request.Form["confirm_record"] != "on")
		{
			Response.Write("<br><br><center><h3>Please tick 'Tick to record'</h3>");
			return;
		}
		if(RecordTransaction())
		{
			Response.Write("<br><br><center><h3>Done, money transfered.</h3>");
			Response.Write("<br><input type=button value='Account List' onclick=window.location=('account.aspx') class=b>");
//			Response.Write("<meta  http-equiv=\"refresh\" content=\"3; URL=transfer.aspx?t=done\">");
		}
	}
	else
	{
		MyDrawTable();
		Session["in_transfer"] = true;
	}
	
	PrintAdminFooter();
}

bool RecordTransaction()
{
	string fa = Request.Form["from_account"];
	string ta = Request.Form["to_account"];
	m_paymentType = Request.Form["payment_type"];
	m_paymentRef = Request.Form["payment_ref"];
	string sAmount = Request.Form["amount"];
	double dAmount = Math.Round(MyMoneyParse(sAmount), 2);
//	double dExRate = MyDoubleParse(Request.QueryString["ex_rate"]);
	double dDestAmount = MyDoubleParse(Request.QueryString["dest_amount"]);

/*
	string fclass1 = fa.Substring(0, 1);
	string fclass2 = fa.Substring(1, 1);
	string fclass3 = fa.Substring(2, 1);
	string fclass4 = fa.Substring(3, 1);
	string tclass1 = ta.Substring(0, 1);
	string tclass2 = ta.Substring(1, 1);
	string tclass3 = ta.Substring(2, 1);
	string tclass4 = ta.Substring(3, 1);

	string sc = " UPDATE account SET balance = balance - " + dAmount;
	sc += " where class1=" + fclass1 + " AND class2=" + fclass2 + " AND class3=" + fclass3 + " AND class4=" + fclass4;
	sc += " UPDATE account SET balance = balance + " + Math.Round(dAmount/dExRate, 2);
	sc += " where class1=" + tclass1 + " AND class2=" + tclass2 + " AND class3=" + tclass3 + " AND class4=" + tclass4;
DEBUG("sc=", sc);
return false;
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
*/

	SqlCommand myCommand1 = new SqlCommand("eznz_payment", myConnection);
	myCommand1.CommandType = CommandType.StoredProcedure;

	myCommand1.Parameters.Add("@nSource", SqlDbType.Int).Value = fa;
	myCommand1.Parameters.Add("@nDest", SqlDbType.Int).Value = ta;
	myCommand1.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand1.Parameters.Add("@payment_method", SqlDbType.Int).Value = m_paymentType;
	myCommand1.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = "";
	myCommand1.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = EncodeQuote(m_paymentRef);
	myCommand1.Parameters.Add("@note", SqlDbType.VarChar).Value = EncodeQuote(Request.Form["note"]);
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
		ShowExp("DoSupplierPayment", e);
		return false;
	}

	m_tranid = myCommand1.Parameters["@return_tran_id"].Value.ToString();
	string sc = " UPDATE trans SET banked = 1 WHERE id = " + m_tranid;
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

bool GetAllAccounts()
{
	if(dst.Tables["account"] != null)
		dst.Tables["account"].Clear();

	string sc = "SELECT * FROM account ORDER BY class1, class2, class3, class4 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nAccounts = myAdapter.Fill(dst, "account");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}
	
void PrintAccountList(string title, string field_name)
{
	Response.Write("<tr><td><b>" + title + " : </b></td><td>");
	Response.Write("<select name=" + field_name + ">");

	for(int i=0; i<m_nAccounts; i++)
	{
		DataRow dr = dst.Tables["account"].Rows[i];
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		Response.Write("<option value=" + number);
//		if(number == m_account)
//			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString()+ " $" +dr["balance"].ToString());		
	}
	Response.Write("</select></td></tr>");
}

bool MyDrawTable()
{
	Response.Write("<br><center><h3>Transfer Money</h3>");
	Response.Write("<form name=f action=transfer.aspx method=post onsubmit='return ValidateTransfer()'>");
	Response.Write("<table cellspacing=1 cellpadding=3 border=0 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	if(!GetAllAccounts())
		return false;

	//account list table
	PrintAccountList("From Account", "from_account");
	PrintAccountList("To Account", "to_account");

	Response.Write("<tr><td><b>Payment Type : </b></td>");
	Response.Write("<td>");
	Response.Write("<select name=payment_type>");
	Response.Write(GetEnumOptions("payment_method", m_paymentType));
	Response.Write("</select></td></tr>");
	Response.Write("\r\n<tr><td><b>Reference : </b></td>");
	Response.Write("<td><input type=text name=payment_ref style='text-align:right' value='" + m_paymentRef + "'>");
	Response.Write("<input type=hidden name=payment_ref_old></td></tr>");
	Response.Write("\r\n<tr><td><b>Date : </b></td><td><input type=text name=date value='" + DateTime.Now.ToString("dd/MM/yyyy") + "' style='text-align:right'></td></tr>");
	Response.Write("\r\n<tr><td><b>Amount to send : </b></td><td>");
	Response.Write("\r\n<input type=text name=amount value=0 style=text-align:right onchange='CalcDest();'>");
	Response.Write("</td></tr>");
	Response.Write("\r\n<tr><td><b>Exchange Rate : </b></td><td>");
	Response.Write("\r\n<input type=text name=ex_rate value=1 style=text-align:right onchange='CalcDest();'>");
//	Response.Write(" (From Acc/To Acc)");
	Response.Write("</td></tr>");
	
	Response.Write("\r\n<tr><td><b>Amount to receive : </b></td><td>");
	Response.Write("\r\n<input type=text name=dest_amount value=0 style=text-align:right onchange='CalcExRate();'>");
	Response.Write("</td></tr>");

	//note field
	Response.Write("<tr><td colspan=2><b>Note : </b></td></tr>");
	Response.Write("<tr><td colspan=2><textarea name=note rows=3 cols=50></textarea></td></tr>");

	Response.Write("<tr>");
	Response.Write("<td colspan=2 align=right>");
	Response.Write("<input type=checkbox name=confirm_record>Tick to transfer ");
	Response.Write("<input type=submit name=cmd value=Transfer class=b>");
	Response.Write("<input type=button value=Cancel onclick=window.location=('transfer.aspx') class=b>");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");

	StringBuilder sb = new StringBuilder();

	sb.Append("\r\n<script language=JavaScript");
	sb.Append(">\r\n");
	sb.Append("function CalcDest()	\r\n");
	sb.Append("{						\r\n");
	sb.Append("		var amount = Number(document.f.amount.value);		\r\n");
	sb.Append("		var exrate = Number(document.f.ex_rate.value);		\r\n");
	sb.Append("		var dest_amount = Math.round(amount / exrate * 100) / 100;		\r\n");
	sb.Append("		document.f.dest_amount.value = dest_amount;		\r\n");
	sb.Append("}			\r\n");
	sb.Append("function CalcExRate()	\r\n");
	sb.Append("{						\r\n");
	sb.Append("		var amount = Number(document.f.amount.value);		\r\n");
	sb.Append("		var dest_amount = Number(document.f.dest_amount.value);		\r\n");
	sb.Append("		var ex_rate = Math.round(amount / dest_amount * 100) / 100;		\r\n");
	sb.Append("		document.f.ex_rate.value = ex_rate;		\r\n");
	sb.Append("}			\r\n");
	sb.Append("function ValidateTransfer()	\r\n");
	sb.Append("{						\r\n");
	sb.Append("		if(!document.f.confirm_record.checked)					\r\n");
	sb.Append("		{															\r\n");
	sb.Append("			window.alert('Please tick \\'Tick to transfer\\' to confirm transaction.');			\r\n");
	sb.Append("			return false;		\r\n");
	sb.Append("		}															\r\n");
	sb.Append("		if(document.f.from_account.value == document.f.to_account.value)		\r\n");
	sb.Append("		{															\r\n");
	sb.Append("			window.alert('Please select a different To Account.');	\r\n");
	sb.Append("			return false;		\r\n");
	sb.Append("		}															\r\n");
	sb.Append("		var amount = Number(document.f.amount.value);				\r\n");
	sb.Append("		var dest_amount = Number(document.f.dest_amount.value);		\r\n");
	sb.Append("		if(amount == 0 || dest_amount == 0)							\r\n");
	sb.Append("		{															\r\n");
	sb.Append("			window.alert('Invalid amount, Amount To Send & Amount To Received should not be zero.');	\r\n");
	sb.Append("			return false;		\r\n");
	sb.Append("		}															\r\n");
	sb.Append("		return true;												\r\n");
	sb.Append("}			\r\n");
	sb.Append("</script");
	sb.Append(">\r\n");

	Response.Write(sb.ToString());

	return true;
}

</script>
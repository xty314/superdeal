<!-- #include file="cart.cs" -->
<!-- #include file="card_function.cs" -->

<script runat=server>

string m_srcaccount = "0";
string m_destaccount = "0";

string m_inv = "";

bool m_bPayAll = false;
string m_invoiceNumber = "";
string m_paymentMethod = "";
string m_salesType = "";
string m_sAccBalance = "";
string m_status = "";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	m_paymentMethod = GetEnumID("payment_method", "cheque"); //default cheque

	if(Request.QueryString["sa"] != null)
		m_srcaccount = Request.QueryString["sa"];
	if(Request.QueryString["da"] != null)
		m_destaccount = Request.QueryString["da"];

	PrintAdminHeader();
	PrintAdminMenu();
	
	if(Request.QueryString["i"] != null)
		m_invoiceNumber = Request.QueryString["i"];
//DEBUG("inv=", m_invoiceNumber);

	if(Request.Form["cmd"] == "Record")
		RecordTransaction();

	MyDrawTable();

	PrintAdminFooter();
}

bool RecordTransaction()
{
	string err = "";
	string payment_method = Request.Form["payment_method"];
	string sAmount = Request.Form["amount"];
	double dAmount = 0;
	if(Request.Form["amount"] == "")
		err = "No Amount Entered";
	else
	{
		try
		{
			dAmount = double.Parse(sAmount, NumberStyles.Currency, null);
		}
		catch
		{
			err = "Invalid Amount Format";
		}
	}
	if(err != "")
	{
		Response.Write("<br><br><center><h3>Error, " + err + "</h3>");
		PrintAdminFooter();
		return false;
	}

	m_srcaccount = Request.Form["src_account"];
	m_destaccount = Request.Form["dest_account"];
	m_paymentMethod = Request.Form["payment_method"];

	SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;
	if(Request.Form["src_account"] != null && Request.Form["src_account"] != "")
		myCommand.Parameters.Add("@nSource", SqlDbType.Int).Value = Request.Form["src_account"];
	if(Request.Form["dest_account"] != null && Request.Form["dest_account"] != "")
		myCommand.Parameters.Add("@nDest", SqlDbType.Int).Value = Request.Form["dest_account"];

	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
//	if(Request.Form["card_id"] != "")
//		myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = Request.Form["card_id"];
//	myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = sTotal;
	myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = payment_method;
	myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = m_invoiceNumber;
	myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = Request.Form["payment_ref"];
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = Request.Form["note"];
	myCommand.Parameters.Add("@Amount", SqlDbType.Money).Value = dAmount;

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

	Response.Write("<br><br><center><h3>Recorded.</h3>");
	PrintAdminFooter();
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

bool MyDrawTable()
{
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
	
	Response.Write("<form action=payment.aspx method=post>");
	Response.Write("<br><center><h3>Payment</h3>");

	Response.Write("<table width=100% cellspacing=10 cellpadding=10 border=1 bordercolor=#CCCCCC bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");

	Response.Write("<table>");
	if(m_srcaccount != "")
	{
		Response.Write("<tr><td><b>Source Account : </b></td><td>");
		Response.Write("<select name=src_account>");
		Response.Write("<option value=''></option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["account"].Rows[i];
			string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
			string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
			double dAccBalance = double.Parse(dr["balance"].ToString());
			Response.Write("<option value=" + number);
			if(number == m_srcaccount)
			{
				Response.Write(" selected");
				m_sAccBalance = dAccBalance.ToString("c");
			}
			Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString() + dAccBalance.ToString("c"));		
		}
		Response.Write("</select>");
		if(m_sAccBalance != "")
			Response.Write("<b>&nbsp&nbsp&nbsp; ------------ Balance : " + m_sAccBalance + "</b>");
		Response.Write("</td></tr>");
	}

	if(m_destaccount != "")
	{
		Response.Write("<tr><td><b>Destination Account : </b></td><td>");
		Response.Write("<select name=dest_account>");
		Response.Write("<option value=''></option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["account"].Rows[i];
			string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
			string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
			double dAccBalance = double.Parse(dr["balance"].ToString());
			Response.Write("<option value=" + number);
			if(number == m_destaccount)
			{
				Response.Write(" selected");
				m_sAccBalance = dAccBalance.ToString("c");
			}
			Response.Write(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString() + dAccBalance.ToString("c"));		
		}
		Response.Write("</select>");
		if(m_sAccBalance != "")
			Response.Write("<b>&nbsp&nbsp&nbsp; ------------ Balance : " + m_sAccBalance + "</b>");
		Response.Write("</td></tr>");
	}
	
	Response.Write("<tr><td><b>Amount : </b></td><td><input type=text size=30 name=amount></td></tr>");
	Response.Write("<tr><td><b>Payment Method : </b></td><td><select name=payment_method>");
	Response.Write(GetEnumOptions("payment_method", m_paymentMethod));
	Response.Write("</select></td></tr>");
	Response.Write("<tr><td><b>Payment Ref : </b></td><td><input type=text size=30 name=ref></td></tr>");
//	Response.Write("<tr><td><b>Date : </b></td><td><input type=text size=30 name=date value='" + DateTime.Now.ToString("dd-MM-yyyy") + "'></td></tr>");

	Response.Write("<tr><td valign=top><b>Comment : </b></td><td><textarea name=note rows=3 cols=40></textarea></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Record></td></tr>");
	Response.Write("</table><br>");

	Response.Write("</td></tr>");
	return true;
}
</script>
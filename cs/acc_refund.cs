<!-- #include file="page_index.cs" -->

<script runat=server>

DataSet dst = new DataSet();
DataTable dtAssets = new DataTable();

string m_id = "";
bool m_bRecorded = false;
double dsub = 0;
string m_branch = "";
string m_fromAccount = "";
string m_toAccount = "";
string m_customerID = "-1";
string m_customerName = "";
string m_paymentType = "2";
string m_paymentDate = DateTime.Now.ToString("dd-MM-yyyy");
string m_paymentRef = "";
string m_note = "";
string m_editRow = "";
string m_nextChequeNumber = "";
string m_command = "";
string m_search = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	if(!SecurityCheck("accountant"))
		return;
//m_nextChequeNumber = GetNextCheaqueNO(); //GetSiteSettings("next_cheque_number", "1000");
m_nextChequeNumber = GetSiteSettings("next_cheque_number", "100000");
	if(TSIsDigit(m_nextChequeNumber))
		m_paymentRef = (int.Parse(m_nextChequeNumber) + 1).ToString();
	else
		m_paymentRef = ""; //m_nextChequeNumber;

	PrintAdminHeader();
	PrintAdminMenu();
	GetQueryString();
	if(m_command == "REFUND")
	{
		if(!DoInsertRefund())
			return;
			}
	if(m_command == "SEARCH" || m_search != "" || m_command == "SEARCH PAYEE")
	{
		if(!GetInvoiceRefundCusotmer())
			return;
		PrintAdminFooter();
		return;
	}
	RefundForm();

	PrintAdminFooter();
}


void GetQueryString()
{
	if(Request.Form["cmd"] != null)
		m_command = Request.Form["cmd"];
	if(Request.Form["s_card_id"] != null && Request.Form["s_card_id"] != "" || m_command == "SEARCH PAYEE")
		m_search = Request.Form["s_card_id"];
	if(Request.QueryString["cid"] != null && Request.QueryString["cid"] != "")
		m_search = Request.QueryString["cid"];
	if(Request.QueryString["cmd"] != null && Request.QueryString["cmd"] != "")
		m_command = Request.QueryString["cmd"];
//	DEBUG("m_seaerch = ", m_search);
//	DEBUG("m_seaerch = ", m_command);
}

string GetNextCheaqueNO()
{
	string iCheaque = "100000";
	string sc = " SELECT TOP 1 payment_ref  FROM acc_refund ";
	sc += " WHERE payment_ref <> null OR payment_ref != '' ";
	sc += " ORDER BY payment_ref DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "cheaque") == 1)
			return iCheaque = dst.Tables["cheaque"].Rows[0]["payment_ref"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	
	return iCheaque;
}
bool GetAllRefundInvoice()
{
	string sc =" SELECT i.invoice_number, i.total, i.commit_date, i.amount_paid, i.cust_ponumber ";
	sc += " FROM invoice i ";
	sc += " WHERE i.paid = 0 AND total <0 ";
	//sc += " AND i.invoice_number NOT IN (SELECT ars.invoice_number FROM acc_refund ar JOIN acc_refund_sub ars ON ars.id = ar.id)";
	if(Session["rcard_id"] != null)
	sc += " AND i.card_id = "+ Session["rcard_id"] +"";
//DEBUG(" sc = ", sc);
int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	bool bAlter = false;
	double dbalance = 0;
	double dtotal = 0;
	double dtotalpaid = 0;
//	double dsub = 0;
	if(rows > 0)
	{
		Response.Write("<table width=100% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr style='color:white;background-color:#666696;font-weight:bold;' align=left><th>INV DATE</td><th>INVOICE#</td><th>CUST PO#</td><th>INV TOTAL</td><th>TOTAL CUSTOMER PAID</td><th>BALANCE</td>");
		Response.Write("<th align=right>APPLIED AMOUNT</td>");
		Response.Write("</tr>");
		Response.Write("<input type=hidden name=rows value="+ rows +">");
		for(int i=0; i<rows; i++)
		{
			Response.Write("<tr");
			if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
			Response.Write(">");
			bAlter = !bAlter;
			DataRow dr = dst.Tables["invoice"].Rows[i];
			string invoice = dr["invoice_number"].ToString();
			string total = dr["total"].ToString();
			string po = dr["cust_ponumber"].ToString();
			string invoice_date = dr["commit_date"].ToString();
			string paid = dr["amount_paid"].ToString();
	
			if(total == "" && total == null)
				total = "0";
			//if(double.Parse(total) < 0)
			//	total = (0 - double.Parse(total)).ToString();
			if(paid == "" && paid == null)
				paid = "0";
			//if(double.Parse(paid) < 0)
			//	paid = (0 - double.Parse(paid)).ToString();
			dtotal = Math.Round(double.Parse(total),2);
			dtotalpaid = Math.Round(double.Parse(paid),2);
			dbalance = dtotal - dtotalpaid;
			dsub += dbalance;
			Response.Write("<td>"+ DateTime.Parse(invoice_date).ToString("dd-MM-yyyy") +"</td>");
			Response.Write("<td><a title='view invocie' href='invoice.aspx?i="+ invoice +"' class=o target=blank>"+ invoice +"</a></td>");
			Response.Write("<td>"+ po +"</td>");
			Response.Write("<td>"+ dtotal.ToString() +"</td>");
			Response.Write("<td>"+ dtotalpaid.ToString() +"</td>");
			Response.Write("<td>"+ dbalance.ToString() +"</td>");
			
			//Response.Write("<td align=right><input type=checkbox name='refund_total"+i+"' value='"+ total +"' onclick=\"if(document.frm.refund_total\"+ i +\".checked){window.alert(document.frm.total_amount.value = eval(document.frm.total_amount.value) + eval(document.frm.refund_total\"+i+\".value));}\"></td>");
			Response.Write("<input type=hidden name='h_invoice"+ i +"' value='"+ invoice +"'>");
			Response.Write("<input type=hidden name='h_balance"+ i +"' value='"+ dbalance +"'>");
			Response.Write("<td align=right><input style='text-align:right' type=text name=refund_total"+ i +" value='"+ dbalance.ToString() +"' ");
			
			Response.Write("onclick=\"");
			Response.Write("if(this.value>0){this.value=0;} return CalOnChange(this, document.frm.bclick.value,'"+ i +"' ); this.select();");
			//Response.Write("this.value=0; this.select();");
			Response.Write("\" ");
				//	Response.Write("onchange=\"return Calculate('"+ i +"', document.frm.bclick.value );\"");
			Response.Write("onchange=\"return CalOnChange(this, document.frm.bclick.value,'"+ i +"' );\"");
			Response.Write("'></td>");
		
			Response.Write("</tr>");
		}
		Response.Write("<input type=hidden name=dsub value="+ dsub +">");
		Response.Write("<input type=hidden name=bclick value=false>");
		Response.Write("</table>");
	}
	javaFunction();
	
	

	return true;
}

void javaFunction()
{
		Response.Write("<script TYPE=text/javascript");
	Response.Write(">\r\n");

		Response.Write("function Calculate(ivalue, bClick)\r\n");
	Response.Write("{	\r\n");
	Response.Write("}\r\n");
	
		Response.Write("function CalOnChange(ivalue, bClick, irows)\r\n");
	Response.Write("{	\r\n");
		Response.Write("var dsub = 0;\r\n ");
		Response.Write("if(!IsNumberic(ivalue.value)) {");
		Response.Write("  ivalue.value='0'; ivalue.select(); return false; }\r\n");
		Response.Write(" if(eval(ivalue.value) > eval(eval(\"document.frm.h_balance\"+ irows +\".value\"))){ ");
			Response.Write(" ivalue.value ='0'; ivalue.select(); return false; }\r\n");
		Response.Write("for(var i=0; i<document.frm.rows.value; i++){ \r\n");
		Response.Write(" var total = eval(\"document.frm.refund_total\"+ i +\".value\") \r\n");
		Response.Write(" dsub = eval(total) + eval(dsub); ");
		Response.Write("} \r\n");
		Response.Write("document.frm.total_amount.value = eval(dsub); \r\n");
	Response.Write("}\r\n");
	Response.Write("function IsNumberic(sText)");
	Response.Write("{");
	Response.Write("var ValidChars = '0123456789-.';");
	Response.Write("		  var IsNumber=true;");
	Response.Write("		   var Char;");
	Response.Write("		   for (i = 0; i < sText.length && IsNumber == true; i++) ");
	Response.Write("		  { ");
	Response.Write("	  Char = sText.charAt(i); ");
	Response.Write("		if (ValidChars.indexOf(Char) == -1) ");
	Response.Write("{");
	Response.Write("			 IsNumber = false;");
	Response.Write("				 }");
	Response.Write("	   }");
	Response.Write("			return IsNumber;");
   	Response.Write("    }");
	Response.Write("</script");
	Response.Write(">");


}

bool DoInsertRefund()
{

	string total_amount = Request.Form["total_amount"];
	if(total_amount == "" && total_amount == null)
	total_amount = "0";
	double dtotal = double.Parse(total_amount);
	string account_id = Request.Form["from_account"];
	//DEBUG("account_id +", account_id);
	string note = Request.Form["note"];
	if(note != "")
	note = EncodeQuote(note);
	string payment_reference = Request.Form["payment_reference"];
	string sc = " SET DATEFORMAT dmy ";
	string refund_id = "";
	string payment_type = Request.Form["payment_type"];

	if(total_amount != "0")
	{
		
		sc += " INSERT INTO acc_refund (total, recorded_by, recorded_date, card_id, from_account ";
		sc += " , note,  payment_ref, payment_type ";
		sc += " ) ";
		sc += " VALUES("+ dtotal +", '"+ Session["card_id"] +"', GETDATE(), '"+ Session["rcard_id"] +"' ";
		sc += " ,'"+ account_id +"', '"+ note +"',  '"+ payment_reference +"', "+ payment_type +" ";
		sc += " ) ";

		sc += " UPDATE account SET balance = balance - "+ dtotal +" WHERE id = "+ account_id +" ";
		sc += " UPDATE settings SET value = '"+ payment_reference +"' WHERE name = 'next_cheque_number' ";
	//DEBUG("sc =", sc);
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

		sc = " SELECT TOP 1 id FROM acc_refund ORDER BY id DESC ";
		
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dst, "refund_id") == 1)
				refund_id = dst.Tables["refund_id"].Rows[0]["id"].ToString();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		

	for(int i=0; i<int.Parse(Request.Form["rows"].ToString()); i++)
	{
		
		string inv = Request.Form["h_invoice"+ i].ToString();
		string balance = Request.Form["h_balance"+ i].ToString();
		string amount_paid = Request.Form["refund_total"+ i].ToString();
		string complete_trans = "0";
		if(double.Parse(amount_paid) >= double.Parse(balance))
			complete_trans = "1";
		if(amount_paid != "0" && refund_id != "")
		{
			
		sc = " SET DATEFORMAT dmy ";
		sc += " INSERT INTO acc_refund_sub (id, invoice_number, amount_refund, amount_owe, complete_trans) ";
		sc += " VALUES ('"+ refund_id +"', '"+ inv +"', "+ amount_paid +", "+ balance +", ";
		sc += complete_trans;
		sc += " )";
if(double.Parse(amount_paid) > 0)
amount_paid = "-"+ amount_paid;
		sc += " UPDATE invoice SET amount_paid = "+ amount_paid +", paid=1, refunded=1 ";
		sc += " WHERE invoice_number = "+ inv +" ";

//DEBUG("sc =", sc);
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
		}

	}
	}
	return true;
}

bool PrintFromAccountList()
{
	int rows = 0;
	string sc = "SELECT * FROM account ";
	//sc += " WHERE class1=1 OR class1=2 ";
	sc += " ORDER BY class1, class2, class3, class4";
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

		}
		Response.Write(">" + disnumber + " " + dr["name4"].ToString());
		Response.Write(" " + dr["name1"].ToString());

	}
	Response.Write("</select>");

	return true;
}

bool PrintToAccountList()
{
	int rows = 0;
	string sc = "SELECT *, name4+' ' +name1 AS type ";
	sc += " FROM account ";
	//sc += " WHERE class1=1 AND class2=3 ";
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
//			m_sAccBalance = dAccBalance.ToString("");
		}
		Response.Write(">" + dr["type"].ToString());
//		Response.Write(" " +dr["name1"].ToString());
//		Response.Write(" " + dAccBalance.ToString());		
	}
	Response.Write("</select>");
	return true;
}

void RefundForm()
{
	
	Response.Write("<br><center><h3>REFUND CREDIT NOTE</center><h3>");
	Response.Write("<form name=frm method=post >");
	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>Branch : </b></td>");
	Response.Write("<td>");
	if(!PrintBranchNameOptions())
		return;
	Response.Write("</td>");
	Response.Write("<td align=right valign=top>");
		//payment table
		Response.Write("<b>Payment Type : </b></td>");
		Response.Write("<td>");
		
		Response.Write("<select name=payment_type onchange=");
		Response.Write(" \"if(this.options[this.selectedIndex].value == '2')");
		Response.Write("document.frm.payment_ref.value=" + m_nextChequeNumber + "; ");
		Response.Write("else document.frm.payment_ref.value=document.frm.payment_ref_old.value;");
		Response.Write("\">");
		Response.Write(GetEnumOptions("payment_method", m_paymentType));
		Response.Write("</select></td></tr>");
	Response.Write("<tr align=left><th>Payee :</td><td><input type=text name=s_card_id value='"+ Session["rname"] + "' onclick=\"document.frm.s_card_id.value=''\">");
	Response.Write("<script");
		Response.Write(">\r\ndocument.frm.s_card_id.select();\r\n</script");
		Response.Write(">\r\n");
	if(Session["rcard_id"] != null)
	{
		Response.Write("<input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
		Response.Write("id=" + Session["rcard_id"] + "','',' width=350,height=350');\" value='VIEW' " + Session["button_style"] + ">");
	}
	Response.Write(" <input type=submit name=cmd value=SEARCH "+ Session["button_style"] +">");
	Response.Write("</td>");
	Response.Write("<td align=right><b>Reference : </b></td>");
		Response.Write("<td><input type=text name=payment_ref style='text-align:right' value='" + m_paymentRef + "'");
		Response.Write(" onchange=\"document.f.payment_ref_old.value=this.value;\">");
		Response.Write("<input type=hidden name=payment_ref_old></td></tr>");

		//from account
	Response.Write("<tr><td><b>From Account : </b></td><td>");
	if(!PrintFromAccountList())
		return;
	Response.Write("</td>");
	Response.Write("<td align=right><b>Payment Date : </b></td><td>");
	Response.Write("<input type=text name=payment_date value='" + m_paymentDate + "' style='text-align:right'>");
	Response.Write("</td></tr>");
	
	//To account
//	Response.Write("<tr><td><b>Assets Type : </b></td><td>");
//	if(!PrintToAccountList())
//		return;
//	Response.Write("</td>");
//	Response.Write("<td align=right><b>Amount : </b></td><td>");
//	Response.Write("\r\n<input type=text name=total_amount style='text-align:right' value='"+ dsub +"' onclick=''>");
//	Response.Write("</td></tr>");
	
	if(Session["rcard_id"] != null)
	{
		Response.Write("<tr><td colspan=4>");
		if(!GetAllRefundInvoice())
			return;
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><th align=left colspan=3>NOTE: &nbsp;&nbsp;&nbsp;<textarea name=note cols=70 rows=7></textarea>");
	Response.Write("</td>");
	//Response.Write("</td></tr>");
	Response.Write("<td align=right valign=top>");
	Response.Write("\r\n<b>Total Applied : </b><input type=text name=total_amount style='text-align:right' value='"+ dsub.ToString() +"' onclick=''>");
	Response.Write("</td></tr>");
	Response.Write("<tr align=right>");
	Response.Write("<td valign=bottom colspan=4>");
	Response.Write("<input type=button name=cmd value='VIEW REFUND REPORT' "+ Session["button_style"] +" onclick=\"window.location=('ref_report.aspx')\"> ");
//	Response.Write("<i>Pls Check to do the Transaction</i> <input type=checkbox name=check_refund value='1'>&nbsp;&nbsp;");
	Response.Write("<input type=submit name=cmd value='REFUND' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\"return confirm('Would you like to DO it???');\" ");
	Response.Write(">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

bool GetInvoiceRefundCusotmer()
{
	string sc = " SELECT sum(i.total - i.amount_paid) as total,c.name, c.company, c.email, c.phone, c.fax, c.id ";
	sc += " FROM card c JOIN invoice i on i.card_id = c.id  ";
	sc += " WHERE i.total - i.amount_paid < 0 ";
	//sc += " WHERE i.paid = 0 ";
	if(m_search != "")
	{
		sc += " AND ";
		if(TSIsDigit(m_search))
			sc += " c.id = "+ m_search +" ";
		else
		{
			m_search = EncodeQuote(m_search);
			m_search = "%"+ m_search +"%";
			sc += " c.name LIKE '"+ m_search +"' OR c.company LIKE '"+ m_search +"' ";
			sc += " OR c.phone LIKE '"+ m_search +"' OR c.email LIKE '"+ m_search +"' ";
		}
	}
//	if(m_card_id != "")
//		sc += " AND c.id = "+ m_card_id +"";
	sc += " GROUP BY c.name, c.company, c.email, c.phone, c.fax, c.id ";
//	sc += " ORDER BY sum(i.total) ";
//DEBUG("sc = ",sc);
	int rows =0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "refund_cust");
		if(rows == 1)
		{
			Session["rname"] = dst.Tables["refund_cust"].Rows[0]["name"].ToString();
			if(Session["rname"] == null || Session["rname"] == "")
				Session["rname"] = dst.Tables["refund_cust"].Rows[0]["company"].ToString();
			Session["rcard_id"] = dst.Tables["refund_cust"].Rows[0]["id"].ToString();
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"');</script");
			Response.Write(">");
			return false;
		}
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
	{
		Response.Write("<script language=javascript>window.alert('No Payee Found');window.history.go(-1);</script");
		Response.Write(">");
		return false;
	}
	double dTotalRefund = 0;
	
	if(rows > 0)
	{
		//paging class
		PageIndex m_cPI = new PageIndex(); //page index class
		if(Request.QueryString["p"] != null)
			m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
		if(Request.QueryString["spb"] != null)
			m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
		rows = dst.Tables["refund_cust"].Rows.Count;
		m_cPI.TotalRows = rows;
		m_cPI.PageSize = 25;
		m_cPI.URI = "?";
		
		string uri = Request.ServerVariables["URL"] +"?";
		if(m_cPI.CurrentPage.ToString() != "" && m_cPI.CurrentPage.ToString() != null)
			uri += "p="+ m_cPI.CurrentPage.ToString() +"&";
		if(m_cPI.StartPageButton.ToString() != "" && m_cPI.StartPageButton.ToString() != null)
			uri += "spb="+ m_cPI.StartPageButton.ToString() +"&";
		if(m_command != "")
			m_cPI.URI += "cmd="+m_command;
		int i = m_cPI.GetStartRow();
		int end = i + m_cPI.PageSize;
		string sPageIndex = m_cPI.Print();

		Response.Write("<form name=frm method=post>");
		Response.Write("<table width=90% align=center cellspacing=2 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=3>");
		Response.Write("<b>SEARCH:</b> <input type=text name=s_card_id value=''><input type=submit name=cmd value='SEARCH PAYEE' "+ Session["button_style"] +">");
		Response.Write("<script");
		Response.Write(">\r\ndocument.frm.s_card_id.select();\r\n</script");
		Response.Write(">\r\n");
		Response.Write("<input type=button name=cmd value='<< Back' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"]+"')\">");
		Response.Write("</td><td align=right colspan=3>");
		Response.Write(sPageIndex);
		Response.Write("</td></tr>");
		Response.Write("<tr bgcolor=#A3EEE align=left><th>CARD ID</th><th>NAME</th><th>COMPANY</th><th>PHONE</th><th>EMAIL</th><th>TOTAL</th></tr>");
		bool bAlter = false;
		for(; i<rows && i<end; i++)
		{
			DataRow dr = dst.Tables["refund_cust"].Rows[i];
			string id = dr["id"].ToString();
			string name = dr["name"].ToString();
			string company = dr["company"].ToString();
			string email = dr["email"].ToString();
			string phone = dr["phone"].ToString();
			string total = dr["total"].ToString();
			
			if(total == "" && total == null)
				total = "0";
			dTotalRefund = Math.Round(double.Parse(total), 2);
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEE ");
			Response.Write(">");
			bAlter = !bAlter;
			Response.Write("<td><a title='select this "+name+"' href='"+ uri +"cid="+ id +"' class=o>");
			Response.Write(id);
			Response.Write("</a></td>");
			Response.Write("<td><a title='select this "+name+"' href='"+ uri +"cid="+ id +"' class=o>");
			Response.Write(name);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(company);
			Response.Write("</td>");
			Response.Write("<td><a title='select this "+name+"' href='"+ uri +"cid="+ id +"' class=o>");
			Response.Write(email);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(phone);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(dTotalRefund.ToString("c"));
			Response.Write("</td>");
			
			Response.Write("</tr>");
		}
		Response.Write("</table>");
		Response.Write("</form>");
	}
	return true;
}

</script>

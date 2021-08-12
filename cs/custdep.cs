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
string m_paymentType = "2";
string m_paymentDate = DateTime.Now.ToString("dd-MM-yyyy");
string m_paymentRef = "";
string m_note = "";
string m_editRow = "";
string m_nextChequeNumber = "";
string m_type = "2"; //1 for withdraw, 2=deposit
string[] m_type_name = new string[3];
string m_command = "";

string m_dest_balance = "0";
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	if(!SecurityCheck("accountant"))
		return;
	m_nextChequeNumber = GetNextCheaqueNO(); //GetSiteSettings("next_cheque_number", "1000");
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		m_customerID = Request.QueryString["sid"];
	if(TSIsDigit(m_nextChequeNumber))
	{
		if(m_nextChequeNumber.IndexOf("-.,@#$%^&*") >=0)
			m_paymentRef = (MyIntParse(m_nextChequeNumber) + 1).ToString();
		else
			m_paymentRef = "";
		
	}
	else
		m_paymentRef = ""; //m_nextChequeNumber;
	//m_paymentRef = (int.Parse(m_nextChequeNumber) + 1).ToString();
	PrintAdminHeader();
	PrintAdminMenu();
	GetQueryString();
	
	if(Request.QueryString["t"] == "done")
	{
		Response.Write("<br><center><h3>Transaction Recorded!!");
			
			Response.Write("<br><br><input type=button value='New Transaction'  "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");
			Response.Write("<input type=button value='Go Bank Deposit' "+ Session["button_style"] +" onclick=\"window.location=('banking.aspx')\">");
			return;
	}
	if(Request.QueryString["did"] != null && Request.QueryString["did"] != "")
	{
		if(DoDelTransDeposit(Request.QueryString["did"].ToString()))
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?t=done');</script");
			Response.Write(">");
			return;
		}
	}
	if(Request.QueryString["vw"] == "rp")
	{
		GetLastTransactionQuery();
		return;
	}
	if(m_command == m_type_name[int.Parse(m_type)])
	{

		if(DoInsertRecord())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?t=done');</script");
			Response.Write(">");
			return;
		}
	}
	
	TransactionForm();
	PrintAdminFooter();

}

bool DoDelTransDeposit(string did)
{
	string sc = " DELETE FROM trans WHERE id ="+ did;
	sc += " DELETE FROM tran_detail WHERE id ="+ did;
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
	
	return true;
}

bool DoInsertRecord()
{

	string sBalance = "";
	string sAccount = Request.Form["account_type1"];
	int nCt = 0;
	string account_type = "";
	string account_id = "";
	for(int i=0; i<sAccount.Length; i++)
	{
		if(sAccount[i].ToString() == ",")
			break;
		
		account_type += sAccount[i].ToString();
		nCt++;
		
	}
	for(int i=nCt + 1; i<sAccount.Length; i++)
	{
		if(sAccount[i].ToString() == ";")
			break;

		sBalance += sAccount[i].ToString();
		nCt++;
	}
	for(int i=nCt + 2; i<sAccount.Length; i++)
		account_id += sAccount[i].ToString();

	string sAccount2 = Request.Form["account_type2"];
	string account_type2 = "";
	string account_id2 = "";
	string sbalance2 = "";

	nCt = 0;
	for(int i=0; i<sAccount2.Length; i++)
	{
		if(sAccount2[i].ToString() == ",")
			break;
		
		account_type2 += sAccount2[i].ToString();
		nCt++;
		
	}
	for(int i=nCt + 1; i<sAccount2.Length; i++)
	{
		if(sAccount2[i].ToString() == ";")
			break;

		sbalance2 += sAccount2[i].ToString();
		nCt++;
	}
	for(int i=nCt + 2; i<sAccount2.Length; i++)
		account_id2 += sAccount2[i].ToString();

	string owner_acc = Request.Form["join_account"];
//	string account_type = Request.Form["account_type"];
	string branch = Request.Form["branch"];
	string amount = Request.Form["total_amount"];
	string payment_type = Request.Form["payment_type"];
	string payment_ref = Request.Form["payment_ref"];
	string note = Request.Form["note"];
	string invoice_number = Request.Form["invoice#"];
//	string invoice_ref = Request.Form["invoice_ref"];
	if(note != null && note != "")
		note = EncodeQuote(note);
	if(amount == "" && amount == null)
		amount = "0";
double dAmount = double.Parse(amount);
m_dest_balance = Request.Form["acc"+ account_type];

//DEBUG("mcustom =erid =", m_customerID);
	string sc = "SET DATEFORMAT dmy ";
	if(double.Parse(amount) > 0 && m_customerID != "" && m_customerID != null && m_customerID.ToLower() != "all")
	{
		m_dest_balance = (double.Parse(sBalance) + double.Parse(amount)).ToString();
		sc += " INSERT INTO trans (dest, amount, trans_date, banked )";
		sc += " VALUES( 1116, "+ amount +", "+ DateTime.Now.ToString("yyyMMdd") +", 0 )";
	string sNext_tran_id = GetNextTransactionID();		
		sc += " INSERT INTO tran_detail (id, invoice_number, source_balance, dest_balance, trans_date, staff_id ";
		sc += " ,card_id , note, payment_method, payment_ref, finance, currency_loss, credit, bank, branch )";
		sc += " VALUES ("+ sNext_tran_id +", '"+ invoice_number +"', "+ amount +", "+ m_dest_balance +", getdate(), "+ Session["card_id"] +" ";
		sc += ", "+ m_customerID +", '"+ note +"', "+ payment_type +", '"+ payment_ref +"', 0, 0, 0 , 0,  "+ branch +"";
		sc += " )";
		
		sc += " INSERT INTO trans_other (id, to_account, location_acc, record_date, record_by, amount ) ";
		sc += " VALUES("+ sNext_tran_id +", "+ account_id +", "+ account_id2 +", GETDATE(), "+ Session["card_id"] +", "+ amount +") ";
		sc += " UPDATE account set balance = balance + "+ amount +" WHERE id = "+ account_id2;
//DEBUG("sc = ",sc);
//return false;
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
	return true;
}
void GetQueryString()
{
	if(Request.QueryString["type"] != null && Request.QueryString["type"] != "")
		m_type = Request.QueryString["type"];
	if(!TSIsDigit(m_type))
		m_type = "1";
	else if(int.Parse(m_type) <0 || int.Parse(m_type) > 2)
		m_type = "1";
	if(Request.Form["cmd"] != "")
		m_command = Request.Form["cmd"];

	m_type_name[1] = "WITHDRAW";
	m_type_name[2] = "DEPOSIT";
	//if(m_type ==  "1")
	//	m_type_name[int.Parse(m_type)] = "WITHDRAW";
	//if(m_type ==  "2")
	//	m_type_name[int.Parse(m_type)] = "DEPOSIT";

//	DEBUG("m_command = ", m_command);
}

string GetNextTransactionID()
{	
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT top 1 id FROM trans ";
	sc += " WHERE 1=1 ";
	sc += " ORDER bY id DESC ";
int id = 0;
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "trans_id");
		
		id = int.Parse(dst.Tables["trans_id"].Rows[0]["id"].ToString()) + 1;
			
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	return id.ToString();
}

bool GetLastTransactionQuery()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT top 10 ";

	sc += " t.id,  t.amount, t.banked, t.trans_bank_id, td.invoice_number, td.dest_balance ";
	sc += " ,td.trans_date, c.name AS staff, c1.name AS customer, td.note, td.payment_method ";
	sc += " ,td.payment_ref, td.bank, td.branch, a.name1 + a.name2 + a.name3 + a.name4 AS account_type ";
	sc += " , a2.name1 + a2.name2 + a2.name3 + a2.name4 AS account_type2 ";
	sc += " FROM trans t JOIN tran_detail td ON td.id = t.id ";
	sc += " LEFT OUTER JOIN tran_invoice ti ON ti.id = t.id AND ti.id = td.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = td.staff_id ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = td.card_id ";
	sc += " LEFT OUTER JOIN account a ON CONVERT(varchar(4), a.class1) + CONVERT(varchar(4), a.class2) + CONVERT(varchar(4), a.class3) + CONVERT(varchar(4),a.class4) = t.dest ";
	sc += " LEFT OUTER JOIN tran_deposit_id tpi ON tpi.tran_id = t.id AND td.id = tpi.tran_id ";
	sc += " LEFT OUTER JOIN tran_deposit tp ON tp.id = tpi.id  ";
	sc += " LEFT OUTER JOIN account a2 ON a2.id = tp.account_id ";
	sc += " WHERE 1=1 ";
	if(m_customerID != null && m_customerID != "")
		sc += " AND td.card_id = "+ m_customerID +" ";
	sc += " ORDER bY td.trans_date DESC ";
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "last_trans");
			
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	bool bAlter = false;
	if(rows <=0 && Request.QueryString["vw"] == "rp")
	{
		Response.Write("<br><center><h3>No Records Found!!");
		Response.Write("<br><br><input type=button value='Back to Transaction'  "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");
		return false;
	}
	if(rows > 0)
	{
		Response.Write("<br><table width=100% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		if(Request.QueryString["vw"] == "rp")
		{
			Response.Write("<tr><td colspan=9><input type=button value='<< BACK' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\" >");
			Response.Write("<input type=button value='PRINT REPORT' "+ Session["button_style"] +" onclick=\"window.print();\" >");
			Response.Write("</td></tr>");
		}
		Response.Write("<tr style='color:white;background-color:#666696;font-weight:bold;' align=left><th>RECORED DATE</th><th>RECORDED BY</th>");
		Response.Write(" <th>ACC_TYPE</th><th>CUSTOMER</th><th>PAYMENT_REF</th><th>BANKED?</th><th>NOTE</th><th>TOTAL</th><th></th></tr>");
	//	Session["last_tran_id"] = dst.Tables["last_trans"].Rows[0]["id"].ToString();
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["last_trans"].Rows[i];
			string payment_type = "Deposit";
			string id = dr["id"].ToString();
			string total = dr["amount"].ToString();
			string note = dr["note"].ToString();
			string recorded_by = dr["staff"].ToString();
			string recorded_date = dr["trans_date"].ToString();
			string payment_ref = dr["payment_ref"].ToString();
			string account_type = dr["account_type"].ToString();
			string join_account = dr["customer"].ToString();
			string type = dr["banked"].ToString();
		
			recorded_date = DateTime.Parse(recorded_date).ToString("dd-MM-yyyy");
			Response.Write("<tr");
			Response.Write(" bgcolor= ");
			if(bAlter)
				Response.Write(" #EEEEE9 ");
			if(type.ToLower() == "true")
				Response.Write(" #DDEEEE ");
			bAlter = !bAlter;
			Response.Write(">");
			if(type.ToLower() == "true")
				account_type = dr["account_type2"].ToString();
			Response.Write("<td>");
			Response.Write(recorded_date);Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(recorded_by);Response.Write("</td>");
			
			Response.Write("<td>");
			Response.Write(account_type);		Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(join_account);	Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(payment_ref);	Response.Write("</td>");
			//Response.Write("<td>");
			//Response.Write(payment_type);	Response.Write("</td>");
			Response.Write("<td>");
			if(type.ToLower() == "false")
				Response.Write("<a title='Deposit this transaction' href='banking.aspx?r="+ DateTime.Now.ToString("ddMMyyyyHHmmss") +"' class=o>GO Banking?</a>");
			else
				Response.Write("<a title='view history' href='tp.aspx?n=deposit_list&r="+ DateTime.Now.ToString("ddMMyyyyHHmmss") +"' class=o>View History</a>");
			//Response.Write(type);
			
			Response.Write("</td>");
			
			Response.Write("<td>");
			
			Response.Write(note);		Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(double.Parse(total).ToString("c"));		Response.Write("</td>");	
			Response.Write("<td>");
			if(type.ToLower() == "false")
				Response.Write(" <a title='Delete this record' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToString("yyyyMMddHHmmss") +"&did="+ id +"' onclick=\"if(!confirm('Are you sure to delete this transaction')){return false;}\" class=o><font color=red>X</font></a>");
			Response.Write("</td>");
			Response.Write("</tr>");

		}
		Response.Write("</table><br>");
	}
	return true;
}
string GetNextCheaqueNO()
{
	string iCheaque = "100000";
	string sc = " SELECT TOP 1 payment_ref FROM tran_detail  ";
	sc += " WHERE payment_ref <> null OR payment_ref != '' AND payment_method = 2 ";
	
	sc += " ORDER BY payment_ref DESC ";
//DEBUG(" sc =", sc);
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

void javaFunction()
{
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">\r\n");
	Response.Write("function CalOnChange(iAmount)\r\n");
	Response.Write("{	\r\n");
	Response.Write("var dsub = 0;\r\n ");

	Response.Write("if(!IsNumberic(iAmount.value)) {");
	Response.Write("  iAmount.value='0'; iAmount.select(); return false; }\r\n");
	Response.Write("if(iAmount.value > 99999999) {");
	Response.Write("  iAmount.value='0'; iAmount.select(); return false; }\r\n");		
	Response.Write("}\r\n");
	Response.Write("function IsNumberic(sText)");
	Response.Write("{");
	Response.Write("var ValidChars = '0123456789.';");
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


void TransactionForm()
{
	javaFunction();
	Response.Write("<br><center><h3>Other Deposit/Rebate Transaction </center><h3>");
	Response.Write("<form name=frm method=post >");
	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr><td><b>SELECT TRANSACTION TYPE: </b></td><td  colspan=3><select name=tran_type ");
//	Response.Write(" onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?type='+ this.options[this.selectedIndex].value)\" ");
//	Response.Write("><option value=1");
//	if(m_type == "1")
//		Response.Write(" selected ");
//	Response.Write(">"+ m_type_name[1] +"");
//	Response.Write("<option value=2 ");
//	if(m_type == "2")
//		Response.Write(" selected ");
//	Response.Write(">"+ m_type_name[2] +"");
//	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Branch : </b></td>");
	Response.Write("<td>");
	if(!PrintBranchNameOptions())
		return;
	Response.Write("</td>");

	Response.Write("<td align=right valign=top>");
		//payment table
		
		Response.Write("<b>"+ m_type_name[int.Parse(m_type)]+" Type : </b></td>");
		Response.Write("<td>");
		Response.Write("<select name=payment_type onchange=");
		Response.Write(" \"if(this.options[this.selectedIndex].value == '2')");
		Response.Write("document.frm.payment_ref.value=" + m_nextChequeNumber + "; ");
		Response.Write("else document.frm.payment_ref.value=document.frm.payment_ref_old.value;");
		Response.Write("\">");
		Response.Write(GetEnumOptions("payment_method", m_paymentType));
		Response.Write("</select></td></tr>");
string last_uri = Request.ServerVariables["URL"] +"?"+ Request.ServerVariables["QUERY_STRING"];
	//Response.Write("</tr>");
	Response.Write("<tr><th align=left>");

/*	Response.Write("<tr><td><b>Select Joiner Account : </b></td>");
	Response.Write("<td>");
	if(!GetOwnerJoinAccount())
		return;
	Response.Write("<input type=button name=cmd value='Add New Share Holder' " + Session["button_style"] + "");
	Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=3&luri="+last_uri+"')\" ");
	Response.Write(">");
	Response.Write("</td>");
*/
	Response.Write("Select Payer :</td><td>");
	string uri = Request.ServerVariables["URL"] +"?sid=";
	if(m_customerID != null && m_customerID != "" && m_customerID != "-1")
		Response.Write(PrintSupplierOptions(m_customerID, uri, "others"));
	else
		Response.Write(PrintSupplierOptions("", uri, "others"));
	Response.Write("<input type=button name=cmd value='Add New Payer' " + Session["button_style"] + "");
		Response.Write(" onclick=\"window.location=('ecard.aspx?a=new&n=others&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&luri="+ last_uri +"')\" ");
		Response.Write(">");
	Response.Write("</td>");
		Response.Write("<td align=right><b>Reference : </b></td>");
	Response.Write("<td><input type=text name=payment_ref style='text-align:right' value='" + m_paymentRef + "'");
	Response.Write(" onchange=\"document.frm.payment_ref_old.value=this.value;\">");
	Response.Write("<input type=hidden name=payment_ref_old></td></tr>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>");
	Response.Write(m_type_name[int.Parse(m_type)]);
	if(m_type_name[int.Parse(m_type)] == "WITHDRAW")
		Response.Write(" FROM");
	else
		Response.Write(" TO");
	Response.Write(" Account : </b></td>");
	Response.Write("<th align=left>");
	if(!GetAccountType("1116"))
		return;
//	Response.Write(" Undeposit Account ");
//	Response.Write("<input type=button name=cmd value='Add New ACC' " + Session["button_style"] + "");
//		Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=6&luri="+last_uri+"')\" ");
//		Response.Write(">");
	Response.Write("</td>");
	Response.Write("<td align=right><b>"+ m_type_name[int.Parse(m_type)]+" Date : </b></td><td>");
		Response.Write("<input type=text name=payment_date value='" + m_paymentDate + "' style='text-align:right'>");

	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Account Location: </td><td>");
	if(!GetAccountType(""))
		return;
	Response.Write("<input type=button name=cmd value='Add New ACC' " + Session["button_style"] + "");
		Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=6&luri="+last_uri+"')\" ");
		Response.Write(">");
	Response.Write("</td><th align=right>");
	Response.Write("INVOICE# :</td><td><input type=text name=invoice# >");
	Response.Write("</td></tr>");
//	Response.Write("<tr><td colspan=2></td><th align=right>");
//	Response.Write("INVOICE# REF :</td><td><input type=text name=invoice_ref >");
//	Response.Write("</td></tr>");
/*	Response.Write("<tr><td><b>TO ");
	Response.Write(" Account : </b></td>");
	Response.Write("<td>");
	if(!GetOwnerJoinAccount())
		return;
	  Response.Write("</td>");
	  */
	Response.Write("<tr><td colspan=4>");
	if(!GetLastTransactionQuery())
		return;
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left colspan=2>NOTE: &nbsp;&nbsp;&nbsp;<textarea name=note cols=70 rows=7></textarea>");
	Response.Write("</td>");
	Response.Write("<td align=right valign=top><b>"+ m_type_name[int.Parse(m_type)] +" Amount : </b></td><td valign=top>");
	Response.Write("\r\n<input type=text name=total_amount value='0' onchange='return CalOnChange(this);' ");
	Response.Write(" style='text-align:right'></td></tr>");
Response.Write("<script language=javascript> document.frm.total_amount.select();</script");
Response.Write(">\r\n");

	//Response.Write("<tr><th align=left colspan=3>NOTE: &nbsp;&nbsp;&nbsp;<textarea name=note cols=70 rows=7></textarea>");
	//Response.Write("</td>");
	
	//Response.Write("<tr><td colspan=4><input type=submit name=cmd value='"+ m_type_name[int.Parse(m_type)] +"' "+ Session["button_style"] +">");
	Response.Write("<tr> ");
	Response.Write("<td align=right colspan=4>");
	Response.Write("<input type=button name=cmd value='VIEW TRANSACTION' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?vw=rp&sid="+ m_customerID +"')\"> ");
	Response.Write("<input type=submit name=cmd value='"+ m_type_name[int.Parse(m_type)] +"' "+ Session["button_style"] +"");
//	Response.Write(" onclick=\"return confirm('ATTN:\\r\\n\\r\\nThe Amont you "+ m_type_name[int.Parse(m_type)] +" is : \\r\\n\\r\\n$'+ document.frm.total_amount.value +'\\r\\n');\" ");
	Response.Write(" onclick=\"if(document.frm.supplier.value==''){window.alert('Please Select Payer!!!');document.frm.supplier.focus();return false;} ");
	Response.Write(" else {return confirm('ATTN:\\r\\n\\r\\nThe Amont you "+ m_type_name[int.Parse(m_type)] +" is : \\r\\n\\r\\n$'+ document.frm.total_amount.value +'\\r\\n');}\" ");
	Response.Write(">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}


/*bool GetOwnerJoinAccount()
{
	string sc = " SELECT * ";
	sc += " FROM account ";
//	sc += " WHERE name1 = 'equity' ";
	sc += " WHERE 1 = 1 ";
	//if(m_type == "1")
		sc += " AND class1 = 3 AND class2=1  ";
	//if(m_type == "1")
	//	sc += " AND class1 = 3 AND class2=1 AND class3=2 ";

	int rows =0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "joiner");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<select name=join_account>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["joiner"].Rows[i];
		string id = dr["id"].ToString();
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string balance = dr["total"].ToString();
		
		double dAccBalance = double.Parse(dr["balance"].ToString());
		Response.Write("<option value=" + id);
		if(id == m_fromAccount)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString());
		Response.Write(" " + dr["name1"].ToString());

	}
	Response.Write("</select>");
	return true;
}
*/
bool GetAccountType(string sclass)
{
	if(dst.Tables["account_type"] != null )
		dst.Tables["account_type"].Clear();
	string sc = " SELECT * ";
	sc += " FROM account ";
	sc += " WHERE 1=1 ";
//	sc += " AND name1 = 'equity' ";
	if(sclass != null && sclass != "")
		sc += " AND CONVERT(varchar(5), class1) + CONVERT(varchar(5), class2) + CONVERT(varchar(5), class3) + CONVERT(varchar(5), class4) = "+ sclass;
//	else
//		sc += " AND class1 = 4 ";
	sc += " ORDER BY class1, class2, class3 ";
	//if(m_type == "1")

//DEBUG("sc = ", sc);
	int rows =0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "account_type");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<select name='account_type");
	if(sclass != "")
		Response.Write("1");
	else
		Response.Write("2");
	Response.Write("'>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["account_type"].Rows[i];
		string id = dr["id"].ToString();
		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		double dAccBalance = double.Parse(dr["balance"].ToString());
		string acc_id = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		//Response.Write("<option value=" + id);
		Response.Write("<option value='" + acc_id +","+ dAccBalance +";"+ id +"' " );
		if(id == m_fromAccount)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString());
		Response.Write(" " + dr["name1"].ToString());
		
	}
	Response.Write("</select>");
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

			Response.Write(" selected");
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
			Response.Write(" selected");
	
		Response.Write(">" + dr["type"].ToString());

	}
	Response.Write("</select>");
	return true;
}



</script>

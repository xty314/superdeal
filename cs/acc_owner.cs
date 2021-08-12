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

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	if(!SecurityCheck("accountant"))
		return;
	m_nextChequeNumber = GetNextCheaqueNO(); //GetSiteSettings("next_cheque_number", "1000");
	if(TSIsDigit(m_nextChequeNumber))
		m_paymentRef = (int.Parse(m_nextChequeNumber) + 1).ToString();
	else
		m_paymentRef = ""; //m_nextChequeNumber;
	//m_paymentRef = (int.Parse(m_nextChequeNumber) + 1).ToString();
	PrintAdminHeader();
	PrintAdminMenu();
	GetQueryString();
	if(Request.QueryString["vw"] == "rp")
	{
		GetLastTransactionQuery();
		return;
	}

	if(m_command == m_type_name[int.Parse(m_type)])
	{
		if(DoInsertRecord())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"');</script");
			Response.Write(">");
			return;
		}
	}
	TransactionForm();
	PrintAdminFooter();

}

bool DoInsertRecord()
{
	string owner_acc = Request.Form["join_account"];
	string account_type = Request.Form["account_type"];
	string branch = Request.Form["branch"];
	string amount = Request.Form["total_amount"];
	string payment_type = Request.Form["payment_type"];
	string payment_ref = Request.Form["payment_ref"];
	string note = Request.Form["note"];
	note = EncodeQuote(note);
	if(amount == "" && amount == null)
		amount = "0";
double dAmount = double.Parse(amount);
	string sc = "SET DATEFORMAT dmy ";
	if(double.Parse(amount) > 0 )
	{
	sc += " INSERT INTO acc_equity ( branch, type, join_account, payment_type, payment_ref";
	sc += " , total, account_type ";
	sc += " , note, recorded_by, recorded_date )";
	sc += " VALUES("+ branch +", "+ m_type +", "+owner_acc +", "+ payment_type +" ";
	sc += " ,'"+ payment_ref +"'";
//	if(m_type == "2")
		sc += " , "+ amount +"";
//	else
//		sc += " , -"+ amount +" ";
	sc += " , "+ account_type +", '"+ note +"', "+ Session["card_id"] +", GETDATE() ";
	sc += " )";

	if(m_type == "2")
	{
		sc += " UPDATE account SET balance = balance + "+ dAmount +" WHERE id = "+ account_type +"";
		sc += " UPDATE account SET balance = balance + "+ dAmount +" WHERE id = "+ owner_acc +"";
	}
	if(m_type == "1")
	{
		sc += " UPDATE account SET balance = balance - "+ dAmount +" WHERE id = "+ account_type +"";
		sc += " UPDATE account SET balance = balance - "+ dAmount +" WHERE id = "+ owner_acc +"";
	}

//DEBUG("sc = ",sc);
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


bool GetLastTransactionQuery()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";
	if(Request.QueryString["vw"] == null && Request.QueryString["vw"] != "")
		sc += " TOP 5 ";
	sc += " ae.type, ae.id, e.name AS payment_type, ae.total, ae.note, c.name AS recorded_by, ae.recorded_date, ae.payment_ref ";
	sc += ", CONVERT(varchar(12),a1.class1) + CONVERT(varchar(12),a1.class2) + CONVERT(varchar(12),a1.class3) + CONVERT(varchar(12),a1.class4) +' - '+ a1.name1 +' '+ a1.name2 +' '+ a1.name4 AS account_type ";
	sc += ", CONVERT(varchar(12),a2.class1) + CONVERT(varchar(12),a2.class2) + CONVERT(varchar(12),a2.class3) + CONVERT(varchar(12),a2.class4) +' - '+ a2.name1 +' '+ a2.name2 +' '+ a2.name4 AS join_account ";
	sc += " FROM acc_equity ae JOIN account a1 ON a1.id = ae.account_type ";
	sc += " JOIN account a2 ON a2.id = ae.join_account ";
	sc += " JOIN card c ON c.id = ae.recorded_by ";
	sc += " JOIN enum e ON e.id = ae.payment_type AND e.class = 'payment_method' ";
//	sc += " WHERE ae.type = "+ m_type +" ";
	sc += " WHERE 1=1 ";
	if(Request.QueryString["ty"] != null && Request.QueryString["ty"] != "")
		sc += " AND ae.type = "+ Request.QueryString["ty"];
	sc += " ORDER BY ae.recorded_date DESC ";
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
		Response.Write("<tr style='color:white;background-color:#666696;font-weight:bold;' align=left><th>RECORDED DATE</th><th>STAFF</th>");
		Response.Write(" <th>TYPE</th><th>ACC_TYPE</th><th>OWNER ACC</th><th>PAYMENT_REF</th><th>PAYMENT_TYPE</th><th>NOTE</th><th>TOTAL</th></tr>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["last_trans"].Rows[i];
			string payment_type = dr["payment_type"].ToString();
			string id = dr["id"].ToString();
			string total = dr["total"].ToString();
			string note = dr["note"].ToString();
			string recorded_by = dr["recorded_by"].ToString();
			string recorded_date = dr["recorded_date"].ToString();
			string payment_ref = dr["payment_ref"].ToString();
			string account_type = dr["account_type"].ToString();
			string join_account = dr["join_account"].ToString();
			string type = dr["type"].ToString();

			recorded_date = DateTime.Parse(recorded_date).ToString("dd-MM-yyyy");
			Response.Write("<tr");
			if(type == "1")
			Response.Write(" bgcolor=#DDEEFF ");
			bAlter = !bAlter;
			Response.Write(">");
			Response.Write("<td>");
			Response.Write(recorded_date);Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(recorded_by);Response.Write("</td>");
			Response.Write("<td>"+ m_type_name[int.Parse(type)] +"</td>");
			Response.Write("<td>");
			Response.Write(account_type);		Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(join_account);	Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(payment_ref);	Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(payment_type);	Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(note);		Response.Write("</td>");
			
			Response.Write("<td>");
			Response.Write(double.Parse(total).ToString("c"));		Response.Write("</td>");	
			Response.Write("</tr>");

		}
		Response.Write("</table><br>");
	}
	return true;
}
string GetNextCheaqueNO()
{
	string iCheaque = "100000";
	string sc = " SELECT TOP 1 payment_ref  FROM acc_equity ";
	sc += " WHERE payment_ref <> null OR payment_ref != '' ";
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
	Response.Write("<br><center><h3>"+ m_type_name[int.Parse(m_type)] +" for SHAREHOLDERS ACCOUNT </center><h3>");
	Response.Write("<form name=frm method=post >");
	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>SELECT TRANSACTION TYPE: </b></td><td  colspan=3><select name=tran_type ");
	Response.Write(" onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?type='+ this.options[this.selectedIndex].value)\" ");
	Response.Write("><option value=1");
	if(m_type == "1")
		Response.Write(" selected ");
	Response.Write(">"+ m_type_name[1] +"");
	Response.Write("<option value=2 ");
	if(m_type == "2")
		Response.Write(" selected ");
	Response.Write(">"+ m_type_name[2] +"");
	Response.Write("</td></tr>");
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
	Response.Write("<tr><td><b>Select Joiner Account : </b></td>");
	Response.Write("<td>");
	if(!GetOwnerJoinAccount())
		return;
	Response.Write("<input type=button name=cmd value='Add New Share Holder' " + Session["button_style"] + "");
	Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=3&luri="+last_uri+"')\" ");
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
	Response.Write("<td>");
	if(!GetAccountType())
		return;
	Response.Write("<input type=button name=cmd value='Add New ACC' " + Session["button_style"] + "");
		Response.Write(" onclick=\"window.location=('account.aspx?t=e&r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&c=6&luri="+last_uri+"')\" ");
		Response.Write(">");
	Response.Write("</td>");
	Response.Write("<td align=right><b>"+ m_type_name[int.Parse(m_type)]+" Date : </b></td><td>");
		Response.Write("<input type=text name=payment_date value='" + m_paymentDate + "' style='text-align:right'>");

	Response.Write("</td></tr>");

/*	Response.Write("<tr><td><b>TO ");
	Response.Write(" Account : </b></td>");
	Response.Write("<td>");
	if(!GetOwnerJoinAccount())
		return;
	  Response.Write("</td>");
	  */
	Response.Write("<tr><td colspan=4>");
//	if(!GetLastTransactionQuery())
//		return;
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
	Response.Write("<input type=button name=cmd value='VIEW TRANSACTION' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?vw=rp')\"> ");
	Response.Write("<input type=submit name=cmd value='"+ m_type_name[int.Parse(m_type)] +"' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"return confirm('ATTN:\\r\\n\\r\\nThe Amont you "+ m_type_name[int.Parse(m_type)] +" is : \\r\\n\\r\\n$'+ document.frm.total_amount.value +'\\r\\n\\r\\n\\r\\n');\" ");
	Response.Write(">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}


bool GetOwnerJoinAccount()
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
		double dAccBalance = double.Parse(dr["balance"].ToString());
	//	string acc_name = dr["name4"].ToString() +" "+ dr["name1"].ToString();
	
		Response.Write("<option value=" + id);
		if(id == m_fromAccount)
			Response.Write(" selected");
		Response.Write(">" + disnumber + " " + dr["name4"].ToString());
		Response.Write(" " + dr["name1"].ToString());

	}
	Response.Write("</select>");
	return true;
}

bool GetAccountType()
{
	string sc = " SELECT * ";
	sc += " FROM account ";
	sc += " WHERE 1=1 ";
	//if(m_type == "1")
	sc += " AND class1 = 1";
//	sc += " WHERE name1 = 'equity' ";
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

	Response.Write("<select name=account_type>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["account_type"].Rows[i];
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

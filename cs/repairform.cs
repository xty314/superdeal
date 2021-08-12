
<script runat="server">

string m_quoteNumber = "";
string m_RMANumber = "";
string sm_Body = "";
int m_RowsReturn = 0;

//current user
string m_sname ="";
string m_scompany ="";
string m_saddr1 ="";
string m_saddr2 ="";
string m_scity ="";
string m_sphone ="";
string m_semail ="";

string m_SN ="";
string m_Inv ="";
string m_char = "r";
string m_id = "";
bool m_bchPostBack = true;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

//current row
string c_sn = "";
string c_invoice_number = "";

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	InitializeData();

	if(Request.Form["cmd"] == "Apply for RA Number")
	{
		if(DoApplyRA())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=repairform.aspx?t=sent&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.QueryString["t"] == "sent")
	{
//		Response.Write("<br><br><center><h3>Thank you</h3> <h4>Our Service Department will issue an RA number to you as late as possible<br><br><br><br><br><br><br><br>");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=rmastatus.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	int rNumber = 0;
	string s_invoice = "";
	string s_sn = "";
	string sfaulty = "";
	RememberLastPage();

//	getSelection();

	string sSearch = "";
	if(Request.Form["cmd"] == "Search By Name or ID" || (Request.Form["txtSearch"] != null && Request.Form["txtSearch"] != ""))
		sSearch = Request.Form["txtSearch"].ToString();
	
	if(Request.QueryString["ci"] != "" && Request.QueryString["ci"] != null)
	{
		if(!SearchCustomer(sSearch))
			return;
		return;
	}
	if(Request.Form["serial_number_search"] != null && Request.Form["serial_number_search"] != "")
	{
		if(!checkWarranty())
			return;
	}
	else if(Request.QueryString["t"] == "sn")
	{
		PrintSNForm();
	}
	else
	{
		if(Request.QueryString["sn"] != null && Request.QueryString["sn"] != "")
		{
			if(!checkWarranty())
				return;
		}

		PrintApplicationForm();
		
	}

}
//added by Tee for getting customer
bool SearchCustomer(string sSearch)
{
	//DEBUG("sSerach = ", sSearch);
	bool isNum = true;
	int ptr = 0;
	while (ptr < sSearch.Length)
	{
		if (!char.IsDigit(sSearch, ptr++))
		{
			isNum = false;
			break;
		}
	}
	string sc = "SELECT TOP 60 id, name, type,company, trading_name, address1,address2, city, phone, email ";
	sc += "FROM card ";
	sc += " WHERE 1=1 ";
	//sc += " WHERE type != 4 ";
	if(sSearch != "")
	{
		if(isNum)
			sc += " AND id = "+sSearch+" ";
		else
		{
			sc += " AND name like '%"+sSearch+"%' or trading_name like '%"+ sSearch +"%' or company like '%"+ sSearch +"%' ";
			sc += " AND email like '%"+sSearch+"%' or phone like '%"+ sSearch +"%' or fax like '%"+ sSearch +"%' ";
		}
	}
	if(Request.QueryString["ci"] != "" && Request.QueryString["ci"] != null && Request.QueryString["ci"] != "all")
		sc += " AND id = "+ Request.QueryString["ci"];

	sc += " ORDER BY id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"search");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	if(dst.Tables["search"].Rows.Count == 1)
	{
		Session["ci"] = dst.Tables["search"].Rows[0]["id"].ToString();
		if(g_bRetailVersion)
		{
			Session["name"] = dst.Tables["search"].Rows[0]["name"].ToString();
			if(Session["name"] == "" || Session["name"] == null)
				Session["name"] = dst.Tables["search"].Rows[0]["company"].ToString();
		}
		else
		{
			Session["name"] = dst.Tables["search"].Rows[0]["company"].ToString();
//DEBUG("sesion=",Session["name"].ToString());
			if(Session["name"] == "" || Session["name"] == null)
				Session["name"] = dst.Tables["search"].Rows[0]["name"].ToString();
		}
		if(Session["name"] == "" || Session["name"] == null)
			Session["name"] = dst.Tables["search"].Rows[0]["trading_name"].ToString();
		//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "?ci="+ Session["ci"].ToString()+"\">");	
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "\">");	
		return false;
	}
	Response.Write("<form name=frm method=post action='repairform.aspx?card=all'>");
	Response.Write("<table width=90% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6><input type=text name=txtSearch> <input type=submit name=cmd value='Search By Name or ID' "+Session["button_style"]+">");
	Response.Write("<input type=button value='Cancel' "+Session["button_style"]+" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\" ></td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3><th>id</th><th>Customer Name</th><th>Company</th><th>Trading Name</th><th>Email</th></tr>");
	bool bChange = true;
	for(int i=0; i<dst.Tables["search"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["search"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string email = dr["email"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string company = dr["company"].ToString();
		Response.Write("<tr ");
		if(!bChange)
			Response.Write("bgcolor=#EEEEE");
		Response.Write(">");
		bChange = !bChange;

		Response.Write("<td><a href='repairform.aspx?ci="+id+"' class=o>"+id+"</font></b></td>");
		Response.Write("<td><a href='repairform.aspx?ci="+id+"' class=o>"+name+"</font></b></td>");
		Response.Write("<td>"+company+"</td>");
		//Response.Write("<td><a href='repairform.aspx?ci="+id+"'><font color=blue><b>"+company+"</font></b></td>");
		Response.Write("<td><a href='repairform.aspx?ci="+id+"' class=o>"+trading_name+"</font></b></td>");
		Response.Write("<td><a href='repairform.aspx?ci="+id+"' class=o>"+ email +"</td>");
		//Response.Write("<td>"+email+"</td></tr>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");

	return true;
}


bool DoApplyRA()
{
	string code = EncodeQuote(Request.Form["code"]);
	string supplier_code = EncodeQuote(Request.Form["supplier_code"]);
	string name = EncodeQuote(Request.Form["name"]);
	string serial_number = EncodeQuote(Request.Form["serial_number"]);
	string invoice_number = EncodeQuote(Request.Form["invoice_number"]);
	string purchase_date = EncodeQuote(Request.Form["purchase_date"]);
	string fault = EncodeQuote(Request.Form["fault"]);
	string motherboard = EncodeQuote(Request.Form["motherboard"]);
	string ram = EncodeQuote(Request.Form["ram"]);
	string cpu = EncodeQuote(Request.Form["cpu"]);
	string vga = EncodeQuote(Request.Form["vga"]);
	string os = EncodeQuote(Request.Form["os"]);
	string other = EncodeQuote(Request.Form["other"]);
	string c_id = "", status = "";
	if(Request.Form["select"] != null && Request.Form["select"] != "")
	{
		c_id = Request.Form["select"].ToString();
		status = "3";
	}
	else
	{
		status = "1";
		c_id = Session["card_id"].ToString();
	}
	if(fault.Length > 1024)
		fault = fault.Substring(0, 1024);
	if(other.Length > 1024)
		other = other.Substring(0, 1024);
	int rNumber = GetNextRepairID();
//DEBUG("ranumber =", rNumber);

	//if(c_id == "" && c_id == null)
	//	c_id = Session["card_id"].ToString();
	string sc = "INSERT INTO repair (ra_number, status, prod_desc, invoice_number, serial_number, customer_id ";
	sc += ", code, supplier_code, fault_desc, purchase_date, motherboard, ram, cpu, vga, os, other) ";
	sc += " VALUES ("+ rNumber +", "+status+", '" + name + "', '" + invoice_number + "', '" + serial_number + "', " + c_id +" ";
	sc += ", '" + code + "', '" + supplier_code + "', '" + fault + "', '" + purchase_date + "', '" + motherboard + "' ";
	sc += ", '" + ram + "', '" + cpu + "', '" + vga + "', '" + os + "', '" + other + "') ";

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

void PrintSNForm()
{
	Response.Write("<br><center><font size=+1><b>"+ m_sCompanyTitle+" Online RA Application</b></font><br>");
	Response.Write("<form name=f action=repairform.aspx method=post>");

	Response.Write("<table cellspacing=3 cellpadding=10 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td><b>Enter Serial Number</b></td>");
	Response.Write("<td><input type=text size=20 maxlength=49 name=serial_number_search></td></tr>");

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd " + Session["button_style"] + " value='Search'>");
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('repairform.aspx?r=" + DateTime.Now.ToOADate() + "') value='Fill Form Manually'>");
	Response.Write("</td></tr>");
	Response.Write("</table></form>");
}

void PrintApplicationForm()
{
	DataRow dr;
	string code = "";
	string supplier_code = "";
	string name = "";
	string sn = m_SN;
	string invoice_number = "";
	string purchase_date = "";

	if(dst.Tables["warranty"] != null && dst.Tables["warranty"].Rows.Count > 0)
	{
		dr = dst.Tables["warranty"].Rows[0];

		code = dr["code"].ToString();
		supplier_code = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		invoice_number = dr["invoice_number"].ToString();
		purchase_date = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy");
	}

	Response.Write("<br><center><font size=+1><b>"+ m_sCompanyTitle +" Online RA Application</b></font><br>");
	Response.Write("<b><i>(Rows in <font color=red>Red</font> must be filled in)</i></b><br>");

	string s_email = Session["email"].ToString();
	
	if(!GetLoginName(s_email))
		return;
	
	if(Request.Form["txtInvoice"] != null)
		m_Inv = Request.Form["txtInvoice"].ToString();
	
	if(Request.Form["txtSerial"] != null)
		m_SN = Request.Form["txtSerial"].ToString();
	
	Response.Write("<form name=f action=repairform.aspx method=post>");

	Response.Write("<table cellspacing=1 cellpadding=0 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	string sType = "1";
	if(dst.Tables["login"].Rows.Count > 0)
	{
		dr = dst.Tables["login"].Rows[0];
		string type = dr["type"].ToString();
		sType = type;
		if(type == "4")
		{
			//dr = dst.Tables["cards"].Rows[0];
			//string id = dr["id"].ToString();
			//string s_name = dr["name"].ToString();
			//string trading_name = dr["trading_name"].ToString();
			Response.Write("<tr><th align=left>Select Customer: </th><td><select name=select Onclick=window.location=('repairform.aspx?ci=all')>");
			Response.Write("<option value=");
			if(Session["ci"] != null)
				Response.Write(""+ Session["ci"] +">"+ Session["name"].ToString() +"</option></select>");
			else
					Response.Write("''>ALL</option></select>");

			//Response.Write(" <input type=button onclick=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write(" <a title='Veiw Customer Details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id="); // + Session["ci"].ToString() + "','',' width=350,height=350'); viewcard_window.focus();\" class=o >VC</a> &nbsp;");
			if(Session["ci"] != null)
				Response.Write(Session["ci"].ToString() + "','',' width=350,height=350'); viewcard_window.focus();\" class=o >VC</a> &nbsp;");
			else
				Response.Write("0','',' width=350,height=350'); viewcard_window.focus();\" class=o >VC</a> &nbsp;");
			Response.Write("OR &nbsp;");
			Response.Write("<a href=ecard.aspx?a=new target=_blank><font color=blue><b><u>New Customer</u></b></font></a></td>");
			Response.Write("</tr>");
		}
		else
		{
			Response.Write("<input type=hidden name=select value=''>");
		}
	}
	Response.Write("<tr><td><b>Product Code</b></td>");
	Response.Write("<td><input type=text size=50 maxlength=49 name=code value='" + code + "'></td></tr>");

	Response.Write("<tr><td><b>Manufacture Part Number</b></td>");
	Response.Write("<td><input type=text size=50 maxlength=49 name=supplier_code value='" + supplier_code + "'></td></tr>");

	Response.Write("<tr><td><font color=red><b>Product Description</b></font></td>");
	Response.Write("<td><input type=text size=50 maxlength=49 name=name value='" + name + "'></td></tr>");

	Response.Write("<tr><td><b>Serial Number</b></td>");
	Response.Write("<td><input type=text size=50 maxlength=49 name=serial_number value='" + m_SN + "'></td></tr>");

	Response.Write("<tr><td><b>Invoice Number</b></td>");
	Response.Write("<td><input type=text size=50 maxlength=49 name=invoice_number value='" + invoice_number + "'></td></tr>");

	Response.Write("<tr><td><b>Purchase Date</b></td>");
	Response.Write("<td><input type=text size=50 maxlength=49 name=purchase_date value='" + purchase_date + "'></td></tr>");

	Response.Write("<tr><td colspan=2><br><h4><font color=red>Fault Description</font></td></tr>");
	Response.Write("<tr><td colspan=2><textarea rows=7 cols=65 name=fault></textarea></td></tr>");
		
	Response.Write("<tr><td colspan=2><br><h4>Environment</td></tr>");

	Response.Write("<tr><td colspan=2>");
	Response.Write("<table width=100% cellspacing=1 cellpadding=0 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>Motherboard</b></td><td><input type=text maxlength=49 name=motherboard></td>");
	Response.Write("<td><b>RAM</b></td><td><input type=text maxlength=49 name=ram></td></tr>");
	Response.Write("<tr><td><b>CPU</b></td><td><input type=text maxlength=49 name=cpu></td>");
	Response.Write("<td><b>VGA</b></td><td><input type=text maxlength=49 name=vga></td></tr>");
	Response.Write("<tr><td colspan=2 valign=bottom><b>Other</b></td><td><b>O/S</b></td><td><input type=text name=os></td></tr>");
//	Response.Write("<tr><td colspan=4><b>Other</b></td></tr>");
	Response.Write("<tr><td colspan=4><textarea name=other cols=65 rows=5></textarea></td></tr>");

	Response.Write("</table>");
	Response.Write("</td></tr>");
	//Response.Write("<tr><td colspan=4 align=center>On Hand: <input type=radio name=radio value=1>No<input type=radio name=radio value=3>Yes</td></tr>");
	Response.Write("<tr><td colspan=2 align=center>");
	Response.Write("<br><input type=submit " + Session["button_style"] + " name=cmd value='Apply for RA Number' OnClick='return checkform();'><br>&nbsp;</td></tr>");

	Response.Write("<tr><td colspan=2><table><tr><td><b>Notice : </b></td></tr>");
	Response.Write("<tr><td>");
	Response.Write("Some products' warranty are carried by their respective manufacturers. <br>Please refer to <a href=sp.aspx?warranty class=o>Warranty Reference</a>");
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr>");

	Response.Write("</table></form>");

	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string s = @"

	function checkform() 
	{	
		if(document.f.name.value=='') {
		window.alert ('Please Enter Product Description!');
		document.f.name.focus();
		return false;
		}
		if(document.f.fault.value==''){
			window.alert('Please Enter Fault Description');
			document.f.fault.focus();
			return false;
		}
		";
	if(sType == "4")
	{
		s +=@"
		if(document.f.select.value==''){
			window.alert('Please select Customer Name');
			//document.f.select.focus();
			return false;
		}
		";
	}
	s +=@"
		return true;		
	}
	";
/*
		if (document.f.serial_number.value=='') { 
		window.alert ('Please Specify Serial Number!');
		document.f.serial_number.focus();
		return false;
		}
		if(document.f.supplier_code.value=='') {
		window.alert ('Please Enter Manufacture Part Number!');
		document.f.supplier_code.focus();
		return false;
		}
		if(document.f.motherboard.value==''){
				window.alert('Please Specify Motherboard');
				document.f.motherboard.focus();
				return false;
		}
		if(document.f.ram.value==''){
				window.alert('Please Specify RAM');
				document.f.ram.focus();
				return false;
		}
		if(document.f.cpu.value==''){
				window.alert('Please Specify CPU');
				document.f.cpu.focus();
				return false;
		}
		if(document.f.vga.value==''){
				window.alert('Please Specify VGA');
				document.f.vga.focus();
				return false;
		}
		if(document.f.os.value==''){
				window.alert('Please Specify O/S');
				document.f.os.focus();
				return false;
		}
*/
	Response.Write("--> "); 
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> ");
}

/*void GetFaultyDesc()
{
	
	//Response.Write("<tr border=0><td border=0>&nbsp;</td></tr>");
	Response.Write("<tr><td align=right>Fault Description:<br>(Please specify as much detail as possible)</td>");
	Response.Write("<td colspan=4><textarea rows=5 cols=70% name=txtFaulty></textarea></td></tr>");
		
	if(Session["email"] == null || Session["email"] == "")
		Response.Write("<tr><td colspan=4 align=right><input type=submit name=cmdUpdate value='Send Repair Job' OnClick='return checkform();'> </td></tr>");
	else
		Response.Write("<tr><td colspan=4 align=right><input type=submit name=cmdUpdate value='Send Repair Job' OnClick='return checkwarrantform()'> </td></tr>");

	Response.Write("<tr><th colspan=4>Conditions of Repair:</th></tr>");
	Response.Write("<tr><td colspan=4>Please check our <a href=labterm.htm><i><u><b>TERMS & CONDITIONS</b></i></u></a> policy</td></tr>");
	Response.Write("<tr><td border=1 colspan=4>*Although all possible care is taken to preserve your data, we cannot guarantee your data. Please make sure</td></tr>");
	Response.Write("<tr><td colspan=4>you have backed up all important data <b>BEFORE</b> bringing your computer to us for serving. No Responsibility will</td></tr>");
	Response.Write("<tr><td colspan=4>be accepted by Eden Computers Ltd or the staff of Eden Computers Ltd for any lose of data</td></tr>");
	Response.Write("<tr><td colspan=4>Thank you for choosing Eden Computers LTD</td></tr>");

	Response.Write("</table></form></form>");
	//Response.Write("</form>");

	//Response.Write("<hr size=1 color=black size=40%>");
}	
*/
bool GetLoginName(string email)
{	
	string sc ="SELECT id, type, name, company, trading_name, address1,address2, city, phone, email FROM card ";
	sc += "WHERE email = '"+email+"'";
	sc += " ORDER BY email DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"login");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<dst.Tables["login"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["login"].Rows[i];
		m_semail = dr["email"].ToString();
		m_sname = dr["name"].ToString();
		
		m_scompany = dr["trading_name"].ToString();
		m_saddr1 = dr["address1"].ToString();
		m_saddr2 = dr["address2"].ToString();
		
		m_scity = dr["city"].ToString();
		m_sphone = dr["phone"].ToString();
		string type = dr["type"].ToString();

		/*if(type == "4")
		{
			string id = "";
			if(Request.QueryString["ci"] != "" && Request.QueryString["ci"] != null )
				id = Request.QueryString["ci"].ToString();
			sc ="SELECT id, type, name, company, trading_name, address1,address2, city, phone, email FROM card ";
			if(id != "" && id != null)
				sc += " WHERE id = "+id+"";
			sc += " ORDER BY name DESC";
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				myAdapter.Fill(dst,"cards");
			}
			catch(Exception e)
			{
				ShowExp(sc, e);
				return false;
			}
		}
		*/
		
	}
	return true;
}

void BindLoginDetial()
{
	if(Request.Form["txtInvoice"] != null)
		m_Inv = Request.Form["txtInvoice"].ToString();
	
	if(Request.Form["txtSerial"] != null)
		m_SN = Request.Form["txtSerial"].ToString();
	
	if(Request.QueryString["system"] == "parts")
		Response.Write("<form name=frmWarranty method=post action='repairform.aspx?thz=success&id="+m_RMANumber+"&system=parts'>");
	else
		Response.Write("<form name=frmWarranty method=post action='repairform.aspx?thz=success&id="+m_RMANumber+"'>");
	//Response.Write("<form name=frmWarranty method=post action='repairform.aspx?thz=success&id="+m_RMANumber+"'>");

	Response.Write("<table cellspacing=2 cellpadding=0 border=1 bordercolor=#E3E3E3 bgcolor=white");
	//Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:black;background-color:#E3E3E3;font-weight:bold;\">\r\n");
	Response.Write("<td>&nbsp;</td><td align=center><font size=2>Repair Application:</font></td><td>Current NO: "+m_RMANumber+"</td>");
	string s_date=DateTime.Now.ToString("dd/MM/yyyy");
	Response.Write("<td>Repair Date:"+s_date+"</td><td>&nbsp;</td></tr>");

	//Response.Write("<tr><td colspan=4 align=right><input type=button name=btRegisted value='None Register'></td>");
	
	for(int i=0;i<dst.Tables["login"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["login"].Rows[i];
	
		m_sname = dr["name"].ToString();
		m_scompany = dr["trading_name"].ToString();
		m_saddr1 = dr["address1"].ToString();
		m_saddr2 = dr["address2"].ToString();
		m_scity = dr["city"].ToString();
		m_sphone = dr["phone"].ToString();
		m_semail = dr["email"].ToString();
		Response.Write("<tr><td>&nbsp;</td><td align=right>Customer Name:</td>");
		Response.Write("<td>"+m_sname+"</td></tr>");

		Response.Write("<tr><td>&nbsp;</td><td align=right>Company Name: </td>");
		Response.Write("<td>"+m_scompany+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td align=right>Address: </td>");
		Response.Write("<td>"+m_saddr1+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td align=right>&nbsp;</td>");
		Response.Write("<td>"+m_saddr2+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td align=right>City: </td>");
		Response.Write("<td>"+m_scity+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td align=right>Phone:</td>");
		Response.Write("<td>"+m_sphone+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td align=right>Email:</td> ");
		Response.Write("<td>"+m_semail+"</td></tr>");

	}
	//Response.Write("</table>");

}

bool displayResultRMA(string sRMA)
{
	string search = "r"+sRMA;
	string sc = "SELECT invoice_number, customer_id, serial_number, name, company, address1, ";
	sc += " prod_desc, address2, city, phone, email, fault_desc ";
	sc += " FROM repair ";
	sc += " WHERE customer_id='"+search+"'";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int irows=myAdapter.Fill(dst, "displayCustomer");
		
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

void BindRMAResultGrid()
{
	string scustomer_id = "";
	string sname = "";
	string scompany = "";
	string saddr1 = "";
	string saddr2 = "";
	string scity = "";
	string sphone = "";
	string semail = "";
	string sfault = "";
	string sinvoice = "";
	string s_serial = "";
	string sprodesc = "";

	Response.Write("<table cellspacing=3 cellpadding=2 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:black;background-color:#E3E3E3;font-weight:bold;\">\r\n");
	for(int i=0; i<=m_RowsReturn; i++)
	{
		DataRow dr = dst.Tables["displayCustomer"].Rows[i];
		scustomer_id = dr["customer_id"].ToString();
		sname= dr["name"].ToString();
		scompany = dr["trading_name"].ToString();
		saddr1 = dr["address1"].ToString();
		saddr2 = dr["address2"].ToString();
		scity = dr["city"].ToString();
		sphone = dr["phone"].ToString();
		semail = dr["email"].ToString();
		sprodesc = dr["prod_desc"].ToString();
		sfault = dr["fault_desc"].ToString();
		sinvoice = dr["invoice_number"].ToString();
		s_serial = dr["serial_number"].ToString();
	}
	
	Response.Write("<td><font size=1> Repair Job No:</font></td><td><font size=+2> "+scustomer_id+"</font></td></tr>");
	Response.Write("<tr><td>Invoice No:</td><td>"+sinvoice+"</td></tr>");
	Response.Write("<tr><td>Serial No:</td><td>"+s_serial+"</td></tr>");
	Response.Write("<tr><td>Customer Details:</td></tr>");
	Response.Write("<tr><td>Name:</td><td>"+sname+"</td></tr>");
	Response.Write("<tr><td>Company:</td><td>"+scompany+"</td></tr>");
	Response.Write("<tr><td>Address:</td><td>"+saddr1+"</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>"+saddr2+"</td></tr>");
	Response.Write("<tr><td>City:</td><td>"+scity+"</td></tr>");
	Response.Write("<tr><td>Phone:</td><td>"+sphone+"</td></tr>");
	Response.Write("<tr><td>Email:</td><td>"+semail+"</td></tr>");
	Response.Write("<tr><td>Product Descriptions:</td><td> "+ sprodesc +"</td></tr> ");
	Response.Write("<tr><td>Fault Descriptions:</td>");

	Response.Write("<td>"+sfault+"</td></tr>");
	//Response.Write("<tr><td>&nbsp;</td></tr>");


	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td><input type=button value='Print this Docket' Onclick='window.print()'></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<br><br><tr><td colspan=2>note: Please keep your record as an reciept in order to trace your product</td></tr>");
	Response.Write("</table>");
//	Response.Write("<hr size=1 color=black width=40%)<br><br>");
	
}


/*string GetLoginChar(string sname)
{

	char[] cb = sname.ToCharArray();
	int p=0;
	string sReturn = "";
	if(sname.Length != 0)
	{
		for(int i=0; i<2; i++)
		{
			sReturn += cb[i].ToString();
			for(p=sname.Length-2; p>=(sname.Length-2); p--)
			{
					sReturn += cb[p].ToString();
			}
		}
	}
	else
	{
		sReturn="EDEN";
	}	

	//m_char = sReturn;
	//Response.Write(sReturn);
	return sReturn;
}*/

//copy from bb.c to decode the message with special char
string msgEncode(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += "\'\'"; //double it for SQL query
		else if(s[i] == '<')
			ss += '[';
		else if(s[i] == '>')
			ss += ']';
//		else if(s[i] == '\n')
//			ss += s[i] + "<br>";
		else
			ss += s[i];
	}
	return ss;
}

bool insertRMANumber(string sSNnumber, string sCustomer_id, string sSales_ref, string sFaultDesc)
{
	string s_prodesc = "";
	if(Request.Form["Prod_desc"] != null && Request.Form["Prod_desc"] != "")
		s_prodesc = Request.Form["Prod_desc"].ToString();
	else
		s_prodesc = "warning no product description";
		
	s_prodesc = msgEncode(s_prodesc);
	sFaultDesc = msgEncode(sFaultDesc);
	m_sname = msgEncode(m_sname);
	
//DEBUG("m sname = ", m_sname);

	string ssc = "SELECT customer_id FROM repair ";
	ssc += " WHERE customer_id = '"+ m_char+sCustomer_id +"'";
	try
	{
		myAdapter = new SqlDataAdapter(ssc, myConnection);
		myAdapter.Fill(dst, "check");
		
	}
	catch(Exception e)
	{
		ShowExp(ssc, e);
		return false;
	}
	if(dst.Tables["check"].Rows.Count <= 0)
	{
		string sinvoice = Request.Form["txtInvoice"].ToString();
		if(sinvoice == null || sinvoice == "") 
			sinvoice = "0";
	
		string sc = "INSERT INTO repair (status, prod_desc, invoice_number, serial_number, id, customer_id, name, company, address1, address2 ";
		sc += " , city, phone, email, fault_desc, repair_date) ";
		sc += " VALUES (0, '"+ s_prodesc +"', "+sinvoice+", '"+sSNnumber+"', "+sCustomer_id+",'"+ m_char+sCustomer_id +"', '"+m_sname+"' ";
		sc += ",'"+m_scompany+"','"+m_saddr1+"','"+m_saddr2+"','"+m_scity+"','"+m_sphone+"','"+m_semail+"','"+ sFaultDesc+"', GETDATE()) ";

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

void getSelection()
{
	Response.Write("<SCRIPT LANGUAGE='JavaScript'");
	Response.Write("> <!-- Begin");
	const string s = @"
		function formHandler(form)
		{
			var URL = document.frmSelect.rma.options[document.frmSelect.rma.selectedIndex].value;
			window.location=(URL);
		}
		function go(loc) {
		window.location.href = loc
		}
	";
	Response.Write("// End --");
	Response.Write(">");
	Response.Write(s);
	Response.Write("</SCRIPT");
	Response.Write(">");

	Response.Write("<form name=frmSelect method=post action=repairform.aspx>");
	Response.Write("<br><center><table border=0 cellspacing=1 cellpadding=4>");
	Response.Write("<tr><td align=center>Select Repair Option </td>");

	Response.Write("<td align=center><select name=rma onChange='formHandler()' size=1>");

	Response.Write("<option >--Please Select Option-- ");

	Response.Write("<option value='repairform.aspx'>Repair Form");
	//Response.Write("<option value='rma.aspx'>Repair Online Warranty ");
	Response.Write("<option value='rmastatus.aspx'>Check Repair Status </select></td>");

	if(Session["email"] == null || Session["email"] == "")
		Response.Write("<td align=right><a href='login.aspx'><u><font color=BLUE>User Login</font></u></a></td></tr>");

	if(Request.QueryString["system"] == "system")
	{
		Response.Write("<tr><td colspan=2 align=center><input type=radio name=chk value='system' ");
		Response.Write(" onclick=\"window.location=('"+Request.ServerVariables["URL"]+"?r="+DateTime.Now.ToOADate()+"system=");
		Response.Write("'+document.frmSelect.chk[0].value)\" checked>");
	}
	else
	{
		Response.Write("<tr><td colspan=2 align=center><input type=radio name=chk value='system' ");
		Response.Write(" onclick=\"window.location=('"+Request.ServerVariables["URL"]+"?r="+DateTime.Now.ToOADate()+"system=");
		Response.Write("'+document.frmSelect.chk[0].value)\" checked>");
	}
	Response.Write("Complete System");
	if(Request.QueryString["system"] == "parts")
	{
		Response.Write("&nbsp;<input type=radio name=chk value='parts' ");
		Response.Write(" onclick=\"window.location=('"+Request.ServerVariables["URL"]+"?r="+DateTime.Now.ToOADate()+"&system=");
		Response.Write("'+document.frmSelect.chk[1].value)\" checked>");
	}
	else
	{
		Response.Write("&nbsp;<input type=radio name=chk value='parts' ");
		Response.Write(" onclick=\"window.location=('"+Request.ServerVariables["URL"]+"?r="+DateTime.Now.ToOADate()+"&system=");
		Response.Write("'+document.frmSelect.chk[1].value)\"");
		Response.Write(">");
	}
	
	Response.Write("Parts</td></tr>");
	
	Response.Write("</table>");
	Response.Write("</form>");

}

void getWarranty()
{
	Response.Write("<script language=javascript>");
	Response.Write("<!---");
	const string s = @"
		function checkwarrantform()
		{
			
			//if(document.frmWarranty.txtInvoice.value==''){
			//	window.alert('Please enter your Invoice Number');
			//	document.frmWarranty.txtInvoice.focus();
			//	document.frmWarranty.txtInvoice.select();
			//	return false;
			//}
			//if(!IsNumberic(document.frmWarranty.txtInvoice.value)){
			//	window.alert('Number Only for Invoice number');
			//	document.frmWarranty.txtInvoice.focus();
			//	document.frmWarranty.txtInvoice.select();
			//	return false;
			//}
			//if(document.frmWarranty.txtSN.value==''){
			//	window.alert('Please enter Serial Number to warranty check');
			//	document.frmWarranty.txtSN.focus();
			//	document.frmWarranty.txtSN.select();
			//	return false;
			//}
			if(document.frmWarranty.txtFaulty.value==''){
				window.alert('Please tell us Your problems of your computer');
				document.frmWarranty.txtFaulty.focus();
				document.frmWarranty.txtFaulty.select();
				return false;
			}
			return true;
		}
		
		function IsNumberic(sText)
		{
		   var ValidChars = '0123456789';
		   var IsNumber=true;
		   var Char;
		   for (i = 0; i < sText.length && IsNumber == true; i++) 
			  { 
			  Char = sText.charAt(i); 
			  if (ValidChars.indexOf(Char) == -1) 
				 {
				 IsNumber = false;
				 }
		   }
		   return IsNumber;
   	    }
	";

	Response.Write("--->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	
	//Response.Write("<form name=frmWarranty method=post action='repairform.aspx?success'>");
	//Response.Write("<form name=frmWarranty method=post action='repairform.aspx'>");

	//Response.Write("<table border=0 cellspacing=0 cellpadding=0>");
	//Response.Write("<tr><td colspan=3 align=center><img src='/i/sn.gif'><h2>Return Materail Authority -RMA- </h2><hr size=1></td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>Warranty Repair:</td></tr>");
	string s_invoice = "";
	if(Request.Form["txtInvoice"] != null)
		s_invoice = Request.Form["txtInvoice"].ToString();
	string s_sn = "";
	if(Request.Form["txtSN"] != null)
		s_sn = Request.Form["txtSN"].ToString();
	string s_purchase_date = "";
	if(Request.Form["txtP_Date"] != null)
		s_purchase_date = Request.Form["txtP_Date"].ToString();

	Response.Write("<tr><td>&nbsp;</td><td align=right>Please enter Invoice Number: </td>");
	
	//Response.Write("<td><input type=text name=txtInvoice value='"+m_Inv+"'><br></td><td>&nbsp;</td></tr>");
	Response.Write("<td><input type=text name=txtInvoice value='"+s_invoice+"'></td><td>&nbsp;</td></tr>");
	if(Request.QueryString["system"] == "parts")
		Response.Write("<tr><td>&nbsp;</td><td align=right>Enter Serial NO:</td><td><input type=text name=txtSN value='"+s_sn+"'></td></tr>");
	else
		Response.Write("<input type=hidden name=txtSN value='complete_system'>");
	//Response.Write("<tr><td align=right>Please enter your Purchase Date: </td>");
	//Response.Write("<td><input type=text name=txtP_Date value='"+s_purchase_date+"'></td><td>&nbsp;</td></tr>");
	if(Request.QueryString["system"] == "parts")
	{
		Response.Write("<tr><td colspan=4 align=right><input type=submit name=cmd value='Check Warranty' onclick='return checkwarrantform()'></td></tr>");
		GridWarrant();
	}
	if(Request.QueryString["system"] != "parts")
	{
		Response.Write("<tr><td align=right>Product Description:</td>");
		Response.Write("<td colspan=5><textarea name=Prod_desc cols=60%></textarea></td></tr>");
	}
}

void getCustomerDetail()
{
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string s = @"

	function checkform() 
	{	
		if(document.f.supplier_code.value=='') {
		window.alert ('Please Enter Manufacture Part Number!');
		document.f.supplier_code.focus();
		return false;
		}
		if(document.f.name.value=='') {
		window.alert ('Please Enter Product Description!');
		document.f.cLast.focus();
		return false;
		}
		if (document.f.serial_number.value=='') { 
		window.alert ('Please Specify Serial Number!');
		document.f.serial_number.focus();
		return false;
		}
		if(document.f.txtFaulty.value==''){
				window.alert('Please Enter Fault Description');
				document.f.txtFaulty.focus();
				return false;
		}
		return true;
	}
	function IsNumberic(sText)
	{
		   var ValidChars = '0123456789';
		   var IsNumber=true;
		   var Char;
		   for (i = 0; i < sText.length && IsNumber == true; i++) 
			  { 
			  Char = sText.charAt(i); 
			  if (ValidChars.indexOf(Char) == -1) 
				 {
				 IsNumber = false;
				 }
		   }
		   return IsNumber;
   	 }
	function echeck(str) 
	{
		var at='@';
		var dot='.';
		var lat=str.indexOf(at);
		var lstr=str.length;
		var ldot=str.indexOf(dot);
		if (str.indexOf(at)==-1){
		   window.alert('Invalid E-mail Address');
		   return false;
		}
		if (str.indexOf(at)==-1 || str.indexOf(at)==0 || str.indexOf(at)==lstr){
		   window.alert('Invalid E-mail Address');
		   return false;
		}
		if (str.indexOf(dot)==-1 || str.indexOf(dot)==0 || str.indexOf(dot)==lstr){
			window.alert('Invalid E-mail Address');
			return false;
		}
		if (str.indexOf(at,(lat+1))!=-1){
			window.alert('Invalid E-mail Address');
			return false;
		}
		if (str.substring(lat-1,lat)==dot || str.substring(lat+1,lat+2)==dot){
			window.alert('Invalid E-mail Address');
			return false;
		}
		if (str.indexOf(dot,(lat+2))==-1){
			window.alert('Invalid E-mail Address');
			return false;
		}
		if (str.indexOf(' ')!=-1){
			window.alert('Invalid E-mail Address');
			return false;
		}
 		return true;					
	}
	";

	Response.Write("--> "); 
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> ");

	if(Request.QueryString["system"] == "parts")
		Response.Write("<form name=frmWarranty method=post action='repairform.aspx?thz=success&id="+m_RMANumber+"&system=parts'>");
	else
		Response.Write("<form name=frmWarranty method=post action='repairform.aspx?thz=success&id="+m_RMANumber+"'>");
	//Response.Write("<form name=frmWarranty method=post action='repairform.aspx'>");
	string s_invoice = "";
	if(Request.Form["txtInvoice"] != null)
		s_invoice = Request.Form["txtInvoice"].ToString();
	string s_sn = "";
	if(Request.Form["txtSN"] != null)
		s_sn = Request.Form["txtSN"].ToString();
	string s_purchase_date = "";
	if(Request.Form["txtP_Date"] != null)
		s_purchase_date = Request.Form["txtP_Date"].ToString();

	Response.Write("<table cellspacing=0 cellpadding=0 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr bgcolor=#E3E3E3><td>&nbsp;</td><td aling=center><font size=2>Repair Application: </font></td><td>Current No: "+m_RMANumber+"</td>");
	
	string s_date=DateTime.Now.ToString("dd/MM/yyyy");
	Response.Write("<td>Today's Date:"+s_date+"</td><td>&nbsp;</td></tr>");
	/*string first = "", last = "", tmp = "";
	if(Request.Form["cFirst"] != null || Request.Form["cLast"] != null)
		m_sname = Request.Form["cFirst"].ToString()+","+Request.Form["cLast"].ToString();

	char[] cfirst = m_sname.ToCharArray();

	int ncheck = 0;
	//for(int i=0; i<ncheck; i++);
	for(int i=0; i<(m_sname.Length-ncheck); i++)
	{
		tmp = cfirst[i].ToString();
		ncheck = i;
		if(tmp != ",")
		{
			first += cfirst[i].ToString();		

		}
	}
	for(int j=ncheck+1; j<m_sname.Length; j++)
			last += cfirst[j].ToString();
	*/
	Response.Write("<tr ><td>&nbsp;</td><td align=right>First Name:(*)<input type=text name=cFirst value=''></td>");
	//Response.Write("<tr ><td align=right>First Name:(*)</td><td><input type=text name=cFirst value='"+first+"'></td></tr>");
	Response.Write("<td align=right>Last Name:(*)<input type=text name=cLast value=''></td></tr>");
	//Response.Write("<tr><td align=right>Last Name:(*)</td><td><input type=text name=cLast value='"+last+"'></td></tr>");

	Response.Write("<tr><td>&nbsp;</td><td align=right>Company:<input type=text name=cCompany value='"+m_scompany+"'></td>");
	Response.Write("<td align=right>City:<input type=text name=cCity value='"+m_scity+"'></td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td align=right>Address:<input type=text name=cAdd1 value='"+m_saddr1+"'></td>");
	Response.Write("<td align=right>Phone:(*)<input type=text name=cPh value='"+m_sphone+"'></td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td align=right>Address1<input type=text name=cAdd2 value='"+m_saddr2+"'></td>");
	Response.Write("<td align=right>Email:(*)<input type=text name=cEmail value='"+m_semail+"'</td></tr>");

	Response.Write("<tr><td>&nbsp;</td></tr>");

	Response.Write("<tr><td>&nbsp;</td><td><b>Warranty Repair:</b></td></tr>");

	Response.Write("<tr><td>&nbsp;</td><td align=right>Please enter Invoice Number: </td>");
	
	//Response.Write("<td><input type=text name=txtInvoice value='"+m_Inv+"'><br></td><td>&nbsp;</td></tr>");
	Response.Write("<td><input type=text name=txtInvoice value='"+s_invoice+"'></td><td>&nbsp;</td></tr>");
	if(Request.QueryString["system"] == "parts")
		Response.Write("<tr><td>&nbsp;</td><td align=right>Enter Serial NO:</td><td><input type=text name=txtSN value='"+s_sn+"'></td></tr>");
	else
		Response.Write("<input type=hidden name=txtSN value='complete_system'>");
	
	//Response.Write("<tr><td align=right>Please enter your Purchase Date: </td>");
	//Response.Write("<td><input type=text name=txtP_Date value='"+s_purchase_date+"'> </td><td>&nbsp;</td></tr>");
	if(Request.QueryString["system"] == "parts")
	{
		Response.Write("<tr><td colspan=4 align=right><input type=submit name=cmd value='Check Warranty' onclick='return checkform()'></td></tr>");
		GridWarrant();
	}
	if(Request.QueryString["system"] != "parts")
	{
		Response.Write("<tr><td align=right>Product Description:</td>");
		Response.Write("<td colspan=4><textarea name=Prod_desc cols=50%></textarea></td></tr>");
	}

}

bool checkWarranty()
{	
	if(Request.Form["invoice_number"] != null)
		m_Inv = Request.Form["invoice_number"].ToString();
	if(Request.Form["serial_number_search"] != null)
		m_SN = Request.Form["serial_number_search"].ToString();
	else if(Request.QueryString["sn"] != null && Request.QueryString["sn"] != "")
		m_SN = Request.QueryString["sn"];

	if(!CheckSQLAttack(m_Inv))
		return false;
	if(!CheckSQLAttack(m_SN))
		return false;

	string ssc = "SELECT serial_number, invoice_number ";
	ssc += "FROM repair ";

	if(m_SN != null && m_SN != "")
	{
		ssc += " WHERE serial_number = '"+ m_SN +"'";
	}
	//if((m_SN != null && m_SN != "") && (m_Inv != null && m_Inv != ""))
	//{
	//	ssc += " WHERE serial_number = '"+ m_SN +"'";
	//	ssc += " AND invoice_number = '"+ m_Inv +"'";
	//}
	else if(m_Inv != null)
		ssc += " WHERE invoice_number = '"+ m_Inv +"'";
	
	try
	{
		myAdapter = new SqlDataAdapter(ssc, myConnection);
		myAdapter.Fill(dst, "checkduplicate");

		if(dst.Tables["checkduplicate"].Rows.Count<1)
		{
				
			string sc = " SELECT i.commit_date, ss.sn, s.invoice_number, s.code, s.supplier_code ";
			sc += ", CONVERT(varchar(49),s.name) AS name ";
			sc += " FROM sales s INNER JOIN sales_serial ss ON ss.invoice_number=s.invoice_number AND ss.code=s.code ";
			sc += " INNER JOIN invoice i ON i.invoice_number=ss.invoice_number ";
//			sc += " LEFT OUTER JOIN card c ON c.id=i.card_id ";
			
			if(m_SN != null && m_SN != "" && m_SN != "complete_system")
			{
				sc += " WHERE ss.sn = '" + m_SN +"'";
			}
			//if((m_SN != null && m_SN != "" && m_SN != "complete_system") && (m_Inv != null && m_Inv != ""))
			//{
			//	sc += " WHERE ss.sn = '" + m_SN +"'";
			//	sc += " AND ss.invoice_number = '"+m_Inv+"' ";
			//}
			else if(m_Inv != null && m_Inv != "")
				sc += " WHERE ss.invoice_number = '"+m_Inv+"'";
						
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				m_RowsReturn = myAdapter.Fill(dst, "warranty");
				
				if(m_RowsReturn==0){
				
					Response.Write("<center><font bgcolor=RED><h5>Invalid Serial Number</h5></font><br>");
					Response.Write("<input type=button name=back " + Session["button_style"] + " value='Try Again' onclick='history.go(-1)'>");
					Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('repairform.aspx?r=" + DateTime.Now.ToOADate() + "') value='Fill Form Manually'>");
					return false;
				}
				else
				{
					if(Request.Form["serial_number_search"] != null)
					{
						Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=repairform.aspx?sn=");
						Response.Write(HttpUtility.UrlEncode(m_SN) + "&r=" + DateTime.Now.ToOADate() + "\">");
					}
				}
			
			}
			catch (Exception e)
			{
				ShowExp(sc, e);
				return false;
			}		
		}
		else
		{
			Response.Write("<script language=javascript> <!--");
			const string invalid = @"
				window.alert('Serial number already Exist, PLEASE TRY AGAIN');
				window.location=('repairform.aspx?t=sn');
			";
			Response.Write("-->");
			Response.Write(invalid);
			Response.Write("</script");
			Response.Write(">");
			return false;
		}
	
	}
	catch (Exception e)
	{
		ShowExp(ssc, e);
		return false;
	}
	return true;
}

bool GetNextCustomerID(ref int rNumber)
{
	
	if(dst.Tables["insertrma"] != null)
		dst.Tables["insertrma"].Clear();

	rNumber = 100000;

	string sc = "SELECT TOP 1 id FROM repair ORDER BY id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		//int i = myAdapter.Fill(dst,"insertrma");
		if(myAdapter.Fill(dst, "insertrma") == 1)
			rNumber = int.Parse(dst.Tables["insertrma"].Rows[0]["id"].ToString()) + 1;

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool autoSendMail(ref string sid)
{

	MailMessage msgMail = new MailMessage();

	msgMail.To = m_semail;

	msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Subject = "Repair Job with " + m_sCompanyTitle;
	
	msgMail.Body = "<html><style type=\"text/css\">\r\n";
	msgMail.Body += "td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n";
	msgMail.Body += "body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n<head><h2>Online Repair";
	msgMail.Body += "</h2></head>\r\n<body>\r\n";
	msgMail.Body = "Here is your Repair Job Details";
	msgMail.Body += "<br>***************************************************<br>";
	msgMail.Body += "Customer Detail : "+m_sname+"";
	msgMail.Body += "<br>Repair Job Number :"+m_char+sid+"";
	msgMail.Body += "<br>Invoice No : "+m_Inv+""; 
	msgMail.Body += "<br>Serail No : "+m_SN+""; 
	msgMail.Body += "<br>***************************************************<br>";
	msgMail.Body += "Our Technician will process your repair as soon as possible";
	msgMail.Body += "<br>Thank you for using "+ m_sCompanyTitle +"'s Service";
	msgMail.Body += "</html>";	

	
	SmtpMail.Send(msgMail);

	//cc to sales self

	msgMail.To = GetSiteSettings("service_email", "alert@eznz.com");
	SmtpMail.Send(msgMail);
	return true;
	
	
}

void GridWarrant()
{
	Response.Write("<tr bgcolor=#E3E3E3><td>Invoice Number</td>");
	Response.Write("<td>Description</td>");
	Response.Write("<td>Serial Number</td>");
	//Response.Write("<td>Customer Name</td>");
	
	Response.Write("<td align=center>Date of Purchase</td>");
	//Response.Write("<td align=center>Date of Warranty Util</td>");
	Response.Write("</tr>");
	for(int i=0; i<m_RowsReturn; i++)
	{
	
		DataRow dr = dst.Tables["warranty"].Rows[i];
		string s_invoice = dr["invoice_number"].ToString();
		string s_customer = dr["Customer"].ToString();
		string s_purchase_date = dr["Purchase Date"].ToString();
		string s_sn = dr["sn"].ToString();
		string s_desc = dr["Description"].ToString();

		m_SN = dr["invoice_number"].ToString();
		Response.Write("<tr><td>"+ s_invoice +"</td>");
		Response.Write("<td>"+ s_desc +"</td>");
		Response.Write("<td>"+ s_sn +"</td>");
		//Response.Write("<td>"+ s_customer +"</td>");
		
		Response.Write("<td>"+ s_purchase_date +"</td>");
		//Response.Write("<td>"+ s_invoice +"</td>");
		Response.Write("</tr>");
	}

}

void BindStatusGrid()
{
	string s_sn = "";
	string s_customer_id = "";
	string s_desc = "";
	string s_note = "";
	for(int i=0; i<dst.Tables["status"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["status"].Rows[0];
		s_sn = dr["serial_number"].ToString();
		s_customer_id = dr["Customer ID"].ToString();
		s_desc = dr["name"].ToString();
		s_note = dr["note"].ToString();
	}	

	Response.Write("<table width=30% align=center border=0 bordercolor=black cellspacing=5 cellpadding=4>");
	Response.Write("<tr><td bgcolor=#BCD9F9 colspan=2 align=center><b>Check Repair Status</b></td></tr>");
	Response.Write("<tr><td width=40% bgcolor=#BCD9F9 align=right>Serial Number</td>");
	Response.Write("<td>"+ s_sn +"</td></tr>");
	
	Response.Write("<tr><td bgcolor=#BCD9F9 align=right>Customer ID</td>");
	Response.Write("<td>"+ s_customer_id +"</td></tr>");
	
	Response.Write("<tr><td bgcolor=#BCD9F9 align=right>Repair Status</td>");
	Response.Write("<td>"+ s_desc +"</td></tr>");
	
	Response.Write("<tr><td bgcolor=#BCD9F9 align=right>Repair Description</td>");
	Response.Write("<td>"+ s_note +"</td></tr>");

}	
</script>
<asp:Label id=LFooter runat=server/>



<script runat="server">

string m_quoteNumber = "";
string m_RMANumber = "";
string m_sTecheMail = "";
string sm_Body = "";
int m_RowsReturn = 0;
//login user

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

//current row
string c_sn = "";
string c_invoice_number = "";

void SPage_Load()
{
	m_sTecheMail = GetSiteSettings("service_email", "alert@eznz.com");
	if(m_sSite =="www")
	
	
	
	getSelection();
	if(TS_UserLoggedIn())
	{
		//if(!DisplayRepairStatus())
		//	return;
		if(!checkStatus())
			return;
	
		BindStatusGrid();
	}
	else
		Response.Write("<h6>Please login to check your Repair Status, &nbsp;<a href=login.aspx><i><u><font color=blue>Login Now</font></i></u></a></h6>");
}

bool DisplayRepairStatus()
{
	string s_email = Session["email"].ToString();
	
	string sc = "SELECT r.serial_number 'SN#', r.invoice_number 'Invoic#', r.note 'Repair Description',  ";
	sc += " CONVERT(varchar(12),r.repair_date,13) AS 'Repair Date', CONVERT(varchar(12),r.repair_finish_date,13) ";
	sc += " AS 'Finish Date', r.charge_detail 'Charge Detail', r.charge '$Charges', r.technician 'Service Technician', e.name 'Job Status'";
	sc += " FROM repair r INNER JOIN enum e ON e.id = r.status ";
	sc += " WHERE e.class = 'repair_status'";
	sc += " AND r.customer_id = '"+ Session["card_id"] + "' ";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_RowsReturn = myAdapter.Fill(dst, "repair");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	Response.Write("Check Repair Status");
	Response.Write("<table cellspacing=2 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold; \">");
	//Response.Write("<td>Check Repair Status</td></tr>");
	Response.Write("<td>");
	return true;
}

void getSelection()
{
	Response.Write("<SCRIPT LANGUAGE='JavaScript'");
	Response.Write("> <!-- Begin");
	string s = @"
		function formHandler(form)
		{
			var URL = document.frmSelect.rma.options[document.frmSelect.rma.selectedIndex].value;
			window.open(URL);
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
/*	
	Response.Write("<form name=frmSelect method=post >");
	Response.Write("<br><center><table border=0 cellspacing=0 cellpadding=4>");
	Response.Write("<tr><td align=center>Select Repair Option </td>");

	Response.Write("<td align=center><select name=rma onChange='formHandler()' size=1>");

	Response.Write("<option >--Please Select Option-- ");

	Response.Write("<option value='repairform.aspx'>Repair Form");
	//Response.Write("<option value='rma.aspx'>Repair Online Warranty ");
	Response.Write("<option value='rmastatus.aspx'>Check Repair Status </select></td>");
	Response.Write("</form>");
	//Response.Write("</table></form>");
	//Response.Write("</center>");
*/
}

void getRMAStatus()
{
	Response.Write("<script language=javascript>");
	Response.Write("<!--- hide from old browser");
	string s = @"
		function ValidateForm()
		{
			if(document.frmStatus.txtStatus.value=='')
			{
				window.alert('Please enter your Repair Job Number');
				document.frmStatus.txtStatus.focus();
				return false;
			}
			/*if(!IsEmpty(document.frmStatus.txtStatus.value)){
				document.frmStatus.txtStatus.focus();
				return false;
			}*/

		   /*if (!IsNumberic(document.frmStatus.txtStatus.value)) { 
			  window.alert('Please enter only numbers or decimal points in the account field'); 
			  document.frmStatus.txtStatus.focus();
			  return false; 
			} */
 
		return true;
 
		} 
		function IsEmpty(sText)
		{
			if(sText.length==0)
				window.alert('No data in the field, please try again');
				return false;
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
   
	   }";
		   
	Response.Write("---->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<form name=frmStatus method=post action='rmastatus.aspx?rma=s'>");

	//Response.Write("<center><form name=frmStatus action='rma.aspx?r=s' method=post>");
	//Response.Write("<table border=0 cellspacing=0 cellpadding=0>");
	Response.Write("<table cellspacing=2 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold; \">");
	
	//Response.Write("<tr><td colspan=3><font size=1 ><b>Repair Status</b></font><br>");
	Response.Write("<td align=center>Please enter your Repair Job number </td>");
	Response.Write("<td bgcolor=white><input type=text name=txtStatus value=''></td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td><input type=submit value='Check Status' name=cmd onClick='return ValidateForm()'>");
	Response.Write("</td><td>&nbsp;</td><td>&nbsp;</td></tr>");
	Response.Write("</form>");
	//Response.Write("</table></form></center>");
}

bool checkStatus()
{
	string s_email = Session["email"].ToString();
	string sc = "SELECT r.ra_number, r.id, r.code, r.supplier_code, r.prod_desc, r.serial_number, r.customer_id, r.purchase_date ";
	sc += ", r.invoice_number, r.note, r.name, r.email, r.fault_desc, ";
	sc += " CONVERT(varchar(12),r.repair_date,13) AS repair_date, CONVERT(varchar(12),r.repair_finish_date,13) ";
	sc += " AS finish_date, r.charge_detail, r.charge, r.technician, e.name AS status, r.status AS status_id ";
	sc += " FROM repair r LEFT OUTER JOIN enum e ON (e.id=r.status AND e.class='rma_status')";
	sc += " WHERE r.customer_id="+ Session["card_id"];// + " AND r.status<>1 "; //status 1 "application received"
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		sc += " AND r.id=" + Request.QueryString["id"];
	sc += " ORDER BY r.id DESC ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_RowsReturn = myAdapter.Fill(dst, "status");

	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	return true;
}

void BindStatusGrid()
{
	string id = "";
	string s_sn = "";
	string s_customer_id = "";
	string s_status ="";
	string s_fdate = "";
	string s_rdate = "";
	string s_fdesc = "";
	string s_note = "";
	string s_name = "";
	string s_addr1 = "";
	string s_addr2 = "";
	string s_city = "";
	string s_phone = "";
	string s_email = "";
	string s_charge = "";
	string s_charge_detail = "";
	string s_technician = "";
	string s_prodesc = "";
	Response.Write("<center><br>");
	Response.Write("<table width=65% cellspacing=1 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<table cellspacing=2 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	//Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");

	if(dst.Tables["status"].Rows.Count == 1 )
	{
		DataRow dr = dst.Tables["status"].Rows[0];
		s_sn = dr["serial_number"].ToString();
		s_customer_id = dr["customer_id"].ToString();
		id = dr["id"].ToString();
		string ra_number = dr["ra_number"].ToString();
		s_status = dr["status"].ToString();
		string status_id = dr["status_id"].ToString();
		s_note = dr["note"].ToString();
		s_fdate = dr["finish_date"].ToString();
		s_rdate = dr["repair_date"].ToString();
		s_fdesc = dr["fault_desc"].ToString();
		s_name = dr["name"].ToString();
		s_charge = dr["charge"].ToString();
		s_charge_detail = dr["charge_detail"].ToString();
		s_technician = dr["technician"].ToString();
		s_prodesc = dr["prod_desc"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		
		if(s_status.ToLower() != "pick up already" && s_status.ToLower() != "deleted")
		{
			Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold; \">");
			
			Response.Write("<td size=50% bgcolor=#B3048><font><b>Check Repair Status: </font></b></td>");
			Response.Write("<td>Repair JobNO:<b>");
	//		DEBUG("s_status =", s_status);
			if(int.Parse(status_id) > 2)
				Response.Write(ra_number);
			else
				Response.Write("</b></td></tr>");

			//Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td align=right>Product Serial Number:</td>");
			Response.Write("<td>"+s_sn+"</td></tr>");
			Response.Write("<tr><td align=right>Product Code:</td>");
			Response.Write("<td>" + code + "</td></tr>");
			Response.Write("<tr><td align=right>Manufacture Part Number</td>");
			Response.Write("<td>" + supplier_code + "</td></tr>");
			Response.Write("<tr><td align=right>Invoice Number</td>");
			Response.Write("<td>" + invoice_number + "</td></tr>");
			Response.Write("<tr><td align=right>Product Description</td>");
			Response.Write("<td>" + s_prodesc + "</td></tr>");
//			Response.Write("<tr><td align=right>Customer Name:</td>");
//			Response.Write("<td>"+s_name+"</td></tr>");
			Response.Write("<tr><td align=right>Job Status:</td>");
			Response.Write("<td><font color=Red><b>"+s_status+"</b></font></td></tr>");
			//Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr bgcolor=#E3E3E3><td>Date Enter Repair:</td><td>Finish Date : </td></tr>");
			Response.Write("<tr><td>&nbsp;"+s_rdate+"</td><td>&nbsp;"+s_fdate+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr bgcolor=#E3E3E3 ><td>Fault Description:</td><td>&nbsp;</td></tr>");
			Response.Write("<tr><td colspan=2>"+s_fdesc+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr bgcolor=#E3E3E3><td>Repair Description:</td><td>&nbsp;</td></tr>");
			Response.Write("<tr><td colspan=2>"+s_note+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr bgcolor=#E3E3E3><td>Repair Charge:</td><td>&nbsp;</td></tr>");
			Response.Write("<tr><td align=right>"+s_charge_detail+"</td></tr>");
			Response.Write("<tr><td align=right>Total Charge:</td><td>$"+s_charge+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td>Service Technician: </td><td>"+s_technician+"</td></tr>");
			//Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td colspan=4><hr size=1></td></tr>");
		
/*			Response.Write("<tr bgcolor=#E3E3E3><td>Conditions: </td><td>Please check our <a href=labterm.htm><b><u><i>TERMS & CONDITIONS</b></u></i></a></td></tr>");
			Response.Write("<tr><td colspan=2>Thank You for your enquiry with us<br>");
			Response.Write("All the information above is Showing your repair status: including repair description, finish date,<br>");
			Response.Write(" repair status and some charges will apply under repair considerations <br>");
			Response.Write("Please feel free to contact our Eden Technician @ (09)638-8188 ");
			Response.Write(" for further enquiry<br><br>");
			
			Response.Write("Once again, Thank You for Choosing Eden Computers LTD</td></tr>");
			//Response.Write("<tr><td>&nbsp;</td></tr>");
			Response.Write("<tr><td colspan=3><hr size=1 color=black></td></tr>");
*/			
			Response.Write("</center>");
		}
		else
			Response.Write("<h5><font color=Red>Thank you for visiting Repair Section, currently you have no RA application</font></h5>");
	}
	else if(dst.Tables["status"].Rows.Count > 1 )
	{
		int iCt = dst.Tables["status"].Rows.Count;
		Response.Write("<tr><td align=center colspan=7><br><b><font>RA Status</font></b>&nbsp;&nbsp;You currently have <font color=red><b>"+iCt+"</font></b> jobs with us</td></tr>");
		Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold; \">");
		Response.Write("<td>&nbsp;</td><td>Serial Number</td><td>JOB#</td><td>Status</td><td>Product DESC</td>");
		Response.Write("<td>Repair DESC</td><td>Update Date</td>");
		Response.Write("</tr>");
		for(int i=0; i<dst.Tables["status"].Rows.Count; i++)
		{
			DataRow dr = dst.Tables["status"].Rows[i];
			id = dr["id"].ToString();
			string ra_number = dr["ra_number"].ToString();
			s_sn = dr["serial_number"].ToString();
			s_customer_id = dr["customer_id"].ToString();
			string status_id = dr["status_id"].ToString();
			s_status = dr["status"].ToString();
			s_note = dr["note"].ToString();
			s_fdate = dr["finish_date"].ToString();
			s_rdate = dr["repair_date"].ToString();
			s_fdesc = dr["fault_desc"].ToString();
			s_name = dr["name"].ToString();
			s_charge = dr["charge"].ToString();
			s_charge_detail = dr["charge_detail"].ToString();
			s_technician = dr["technician"].ToString();
			s_prodesc = dr["prod_desc"].ToString();
			string code = dr["code"].ToString();
			bool bTR = true;

			if(s_status != "Pick up Already")
			{
				if(bTR)
				{
					Response.Write("<tr>");
					bTR = false;
				}
				else
				{
					Response.Write("<tr bgcolor=red>");
					bTR = true;
				}
				//Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold; \">");
				Response.Write("<td width=2%>"+(i+1).ToString()+"</td>");
				Response.Write("<td width=5%>" + s_sn + "</td>");
				Response.Write("<td width=7%><a href=rmastatus.aspx?id=" + id + "&r=" + DateTime.Now.ToOADate() + " class=o>");
	//	DEBUG("ssattus =", s_status);
				if(int.Parse(status_id) >= 2)
					Response.Write(ra_number);
				Response.Write("</a></td>");
				Response.Write("<td width=10%><font color=Red><b>"+s_status+"</b></font></td>");
				Response.Write("<td width=15%>"+s_prodesc+"</td>");
				Response.Write("<td width=20%>"+s_note+"</td><td width=6%>"+s_fdate+"</td>");
				Response.Write("</tr>");
				
				Response.Write("<tr><td colspan=7><hr size=1></td></tr>");
			}
		}
		Response.Write("<tr><td colspan=3><b>Service Technician: </b></td><td>"+s_technician+"</td></tr>");
/*		Response.Write("<tr bgcolor=#E3E3E3><td>&nbsp;</td><td>&nbsp;</td><td>Conditions: </td><td colspan=5>Please check our <a href=labterm.htm><b><u><i>TERMS & CONDITIONS</b></u></i></a></td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=5>Thank You for your enquiry with us<br>");
		Response.Write("All the information above is Showing your repair status: including repair description, finish date,<br>");
		Response.Write(" repair status and some charges will apply under repair considerations <br>");
		Response.Write("Please feel free to contact our Eden Technician @ (09)638-8188 ");
		Response.Write(" for further enquiry<br><br>");
		
		Response.Write("Once again, Thank You for Choosing Eden Computers LTD</td></tr>");
		//Response.Write("<tr><td>&nbsp;</td></tr>");
*/
		Response.Write("<tr><td colspan=6><hr size=1 color=black></td></tr>");
		
	}
	else
		Response.Write("<h5><font color=Red>Thank you to visit Repair Section, Currently "+Session["name"].ToString()+" no repair jobs</font></h5>");
	
	Response.Write("</table>");
	Response.Write("</table>");
	Response.Write("</center>");

}	

void GridStatus()
{
	DataView source = new DataView(dst.Tables["repair"]);
	string path = Request.ServerVariables["URL"].ToString();
	MyDataGrid.DataSource = source;
	MyDataGrid.DataBind();
}


</script>

<form runat=server>

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    

</asp:DataGrid>

</form>

<asp:Label id=LFooter runat=server/>
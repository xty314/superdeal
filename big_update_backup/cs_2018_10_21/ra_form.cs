<script runat="server">

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_sEmail = "";
string supplier_rma = "";
string supplier_id = "";
string rma_header = "";
string m_template = "";
string template_text = "";

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!getRMADetails())
		return;
	
	string email = "";
	string ra_id = "";
	if(Request.QueryString["email"] != null && Request.QueryString["email"] != "" )
		email = Request.QueryString["email"];
	if(Request.QueryString["ra"] != null && Request.QueryString["ra"] != "" )
		ra_id = Request.QueryString["ra"];
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		supplier_id = Request.QueryString["sid"];
	if(TSIsDigit(supplier_id))
		template_text = ReadRATemplate(supplier_id);

	string total_item = dst.Tables["ra_detail"].Rows.Count.ToString();
	string own_rma_no = "";

	//new 
	string sn  = "";
	string invoice  = "";
	string supplier_code  = "";
	string desc = "";
	string repair_date  = "";
	string pur_date  = "";
	string fault  = "";

	string rid = "";
	string supplier_rmano = "";
	string ticket = "";
	for(int i=0; i<dst.Tables["tickets"].Rows.Count; i++)
		ticket += "Ticket#:"+ (i+1) +" <b>"+ dst.Tables["tickets"].Rows[i]["ticket"].ToString() +" </b>\tShip by: <b>"+ dst.Tables["tickets"].Rows[i]["ship_desc"].ToString() +"</b><br>";
	template_text = template_text.Replace("@ticket", ticket);	

	for(int i=0; i<dst.Tables["ra_detail"].Rows.Count; i++)
	{
		DataRow dr;
		dr = dst.Tables["ra_detail"].Rows[i]; 
		own_rma_no = dr["ra_id"].ToString();

		sn = dr["serial_number"].ToString();
		invoice = dr["invoice_number"].ToString();
		supplier_code = dr["supplier_code"].ToString();
		desc  = dr["product_desc"].ToString();
		if(desc != null && desc != "")
		{
			desc = desc.Replace("[","<");
			desc = desc.Replace("]",">");
			
			desc = StripHTMLtags(desc);
		}
		repair_date = dr["repair_date"].ToString();
		pur_date = dr["purchase_date"].ToString();
		fault = dr["fault_desc"].ToString();
		if(fault != null && fault != "")
		{
			fault = fault.Replace("[","<");
			fault = fault.Replace("]",">");		
			fault = StripHTMLtags(fault);
		}
		if(pur_date != null && pur_date != "")
			pur_date = DateTime.Parse(pur_date).ToString("dd-MM-yyyy");
		if(pur_date == "01-01-1900")
			pur_date = "";

		template_text = template_text.Replace("@invoice" + i, invoice);
		template_text = template_text.Replace("@sn" + i, sn);
		template_text = template_text.Replace("@supplier_code" + i, supplier_code);
		template_text = template_text.Replace("@desc" + i, desc);
		template_text = template_text.Replace("@repair_date" + i, repair_date);
		template_text = template_text.Replace("@pur_date" + i, pur_date);
		template_text = template_text.Replace("@fault" + i, fault);	
		
	
		rid = dr["id"].ToString();
		supplier_rmano = dr["supp_rmano"].ToString();
	}	
	for(int i=dst.Tables["ra_detail"].Rows.Count; i>=dst.Tables["ra_detail"].Rows.Count && i<MyIntParse(GetSiteSettings("ra_qty_limit", "5")); i++)
	{
		template_text = template_text.Replace("@invoice" + i, "&nbsp;");
		template_text = template_text.Replace("@sn" + i, "&nbsp;");
		template_text = template_text.Replace("@supplier_code" + i, "&nbsp;");
		template_text = template_text.Replace("@desc" + i, "&nbsp;");
		template_text = template_text.Replace("@repair_date" + i, "&nbsp;");
		template_text = template_text.Replace("@pur_date" + i, "&nbsp;");
		template_text = template_text.Replace("@fault" + i, "&nbsp;");	
	}
	
	template_text = template_text.Replace("@supplier_rmano", supplier_rmano);

	template_text = template_text.Replace("@total_item", total_item);
	template_text = template_text.Replace("@own_rma_no", own_rma_no);
	m_sEmail = template_text;

	if(Request.QueryString["print"] == "form" && Request.QueryString["ra"] != null)
	{
		Response.Write(template_text);
		if(template_text == "" || template_text == null)
		{
			Response.Write(PrintPackForm());	
		}
//		return;
	}
	if(Request.QueryString["email"] != null && Request.QueryString["email"] != "")
	{
		if(m_sEmail == "" || m_sEmail == null)
		{
			m_sEmail = PrintPackForm();
		}
		if(Request.QueryString["confirm"] == "1")
		{
			Response.Write("<script Language=javascript");
			Response.Write(">");
			Response.Write("if(window.confirm('");
			Response.Write("Email RMA#" + supplier_rma + " to " + email + "?         ");
			Response.Write("\\r\\n\\r\\n");
			Response.Write("\\r\\nClick OK to send.\\r\\n");
			Response.Write("'))");
			Response.Write("window.location='ra_form.aspx?sid="+ Request.QueryString["sid"] +"&ra=" + ra_id + "&email=" + HttpUtility.UrlEncode(email) + "&srma="+ supplier_rma +"';\r\n");
			Response.Write("else window.close();\r\n");
			Response.Write("</script");
			Response.Write(">");
			
		}
		else
		{
	//	DEBUG("form = ", m_sEmail);
			MailMessage msgMail = new MailMessage();
			msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
			msgMail.To = email;
			msgMail.Subject = "RMA#" + " " + supplier_rma + " - " + m_sCompanyTitle;
			msgMail.BodyFormat = MailFormat.Html;
			msgMail.Body = m_sEmail;
			SmtpMail.Send(msgMail);
			if(!doUpdateSentMail(ra_id))
				return;
		}
		
		Response.Write("<form name=frm onload='window.close()'>");
		Response.Write("<br><center><h3>RMA# " + Request.QueryString["ra"] + " Sent.</h3>");
		Response.Write("<input type=button value='Close Window' onclick=window.close() " + Session["button_style"] + ">");
		Response.Write("<br><br><br><br><br><br>");
		Response.Write("</from>");
		return;
	}	
	if(Request.QueryString["email"] == null && Request.QueryString["email"] == "")
	{
		Response.Write("<center><h3><font color=red>Invalid Email Address, No Email Sent</font></h3>");
		Response.Write("<input type=button value='Close This Window' "+ Session["button_style"] +" onclick=window.close()></center>");//<script language=javascript>window.close()</script");
		return;
	}
	/*rma_header = ReadSitePage("repair_header");
	if(Request.QueryString["srma"] != null && Request.QueryString["srma"] != "")
		supplier_rma = Request.QueryString["srma"];
	m_sEmail = PrintPackForm();
	if(Request.QueryString["print"] == "form" && Request.QueryString["ra"] != null && Request.QueryString["confirm"] == null)
	{
		Response.Write(PrintPackForm());
	}
		
		
	//Response.Write(PrintPackForm());
			
//	}*/
}

bool doUpdateSentMail(string ra_id)
{
	string sc = " UPDATE rma SET email_supplier = 1 ";
	sc += " WHERE ra_id = "+ ra_id +"";
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

string PrintPackForm()
{

	StringBuilder sb = new StringBuilder();
	DataRow dr;
	sb.Append("<table align=center width=97% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><td>");
	sb.Append(ReadSitePage("repair_header"));
	sb.Append("<title>"+Session["CompanyName"]+" RMA Request.</title>");
//	sb.Append("<body onload='window.print()'>");
	sb.Append("<tr><td colspan=2>");
	sb.Append("<table align=center width=100% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	//sb.Append("<tr ><th align=left colspan=5>"+ra_header+"</th></tr>");
	sb.Append("<tr><td colspan=3 width=55%>");
	string technician = "";
	string r_date = "";
	string ra_number = "";
//DEBUG("rows = ", dst.Tables["ra_detail"].Rows.Count.ToString());
	if(dst.Tables["ra_detail"].Rows.Count > 0)
	{
		dr = dst.Tables["ra_detail"].Rows[0];
		string name = dr["supplier_name"].ToString();
		if(name == "" && name == null)
			name = dr["trading_name"].ToString();
		string company = dr["company"].ToString();
		string ticket = dr["pack_no"].ToString();
		string addr1 = dr["address1"].ToString();
		string addr2 = dr["address2"].ToString();
		string city = dr["city"].ToString();
		string phone = dr["phone"].ToString();
		string fax = dr["fax"].ToString();
		string email = dr["email"].ToString();
		string supp_rmano = dr["supp_rmano"].ToString();
		ra_number = dr["ra_id"].ToString();
		r_date = dr["repair_date"].ToString();
		r_date = DateTime.Parse(r_date).ToString("dd-MMM-yyyy");
		sb.Append("<table align=center width=100% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		sb.Append("<tr><th align=left colspan=3>SHIP TO:  </th></tr>");
		sb.Append("<tr><td>"+company+"</td></tr>");
		sb.Append("<tr><td>"+addr1+"</td></tr>");
		sb.Append("<tr><td>"+addr2+"</td></tr>");
		sb.Append("<tr><td>"+city+"</td></tr>");
		sb.Append("<tr><td>ph: "+phone+"</td></tr>");
		sb.Append("<tr><td>fx: "+fax+"</td></tr>");
		sb.Append("<tr><td>email: "+email+"</td></tr>");
		sb.Append("<tr><th colspan=2 align=left>RMA#: "+ supp_rmano +"</th></tr>");
		sb.Append("</table>");
		sb.Append("</td><th colspan=2 valign=top width=40%>");
		sb.Append("<table align=center width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=white bgcolor=white");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		//sb.Append("<table border=0 >");
		sb.Append("<tr><th colspan=2><font size=3>RMA#: "+ supp_rmano +"</font></th></tr>");
		sb.Append("<tr><td>Repair Date: </td><td>"+ r_date +"</td></tr>");
		sb.Append("<tr><td>Delivery Ticket: </td><td>"+ ticket +"</td></tr>");
		sb.Append("</table></td></tr>");
	}	
	
	sb.Append("<tr></tr>");
	//sb.Append("<tr><th colspan=6 align=left>Faulty Items:</th></tr>");
	sb.Append("<tr><td colspan=7><hr size=1 color=gray></td></tr>");
	sb.Append("<tr>");
	sb.Append("<th>&nbsp</th>");
	sb.Append("<th align=left>SN#</th>");
	sb.Append("<th align=left>SUPP_CODE#</th>");
	sb.Append("<th align=left>PURCHASE INV#</th>");
	sb.Append("<th align=left>PURCHASE DATE</th>");
	sb.Append("<th align=left>DESCRIPTION</th>");
	sb.Append("<th align=left>FAULT DESC</th></tr>");
	sb.Append("<tr><td colspan=7><hr size=1 color=gray></td></tr>");
	for(int i=0; i<dst.Tables["ra_detail"].Rows.Count; i++)
	{
		dr = dst.Tables["ra_detail"].Rows[i];
		string sn = dr["serial_number"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string desc = dr["product_desc"].ToString();
		string repair_date = dr["repair_date"].ToString();
		string rid = dr["id"].ToString();
		string supplier_rmano = dr["supp_rmano"].ToString();
		string pur_date = dr["purchase_date"].ToString();
		string fault = dr["fault_desc"].ToString();
		technician = dr["technician"].ToString();
		if(invoice == "0")
			invoice = "";

		//sb.Append("<tr><th colspan=5 align=left>Faulty Items:</th></tr>");
		/*sb.Append("<tr bgcolor=#EEEEEE>");
		sb.Append("<th rowspan=3>"+(i+1)+".</th>");
		sb.Append("<th>SN#</th>");
		sb.Append("<th>Supp Code#</th>");
		sb.Append("<th>Purchase Invoice#</th>");
		sb.Append("<th>Purchase Date#</th>");
		sb.Append("<th>Description</th></tr>");
		*/
		sb.Append("<tr>");
		sb.Append("<th width=4% rowspan=2>"+(i+1)+".</th>");
		sb.Append("<td width=10%>"+sn+"</td>");
		sb.Append("<td width=15%>"+supplier_code+"</td>");
		sb.Append("<td width=15%>"+invoice+"</td>");
		sb.Append("<td width=15%>");
		if(pur_date != "1/01/1900 12:00:00 a.m.")
			sb.Append(pur_date);
		else
			sb.Append("&nbsp;");
		sb.Append("</td>");
		sb.Append("<td>"+desc+"</td>");
		//sb.Append("<td>"+desc+"</td></tr>");
		//sb.Append("<tr><th align=left>FAULT DESC:</th><td colspan=4><font color=Red>"+ fault +"</font></td></tr>");
		sb.Append("<td>"+ fault +"</td></tr>");
		sb.Append("<tr></tr>");
		//sb.Append("<tr><td colspan=6><hr size=1 color=gray></td></tr>");
	}
	//ra_packslip = ra_packslip.Replace("@@Technician", technician);
	//ra_packslip = ra_packslip.Replace("@@Jobnumber", ra_number);
	//ra_packslip = ra_packslip.Replace("@@Repairdate", r_date);
	//sb.Append("<tr><td colspan=6>&nbsp;</td></tr>");
	//sb.Append("<tr><td colspan=5>"+ra_conditions+"</td></tr>");
	//sb.Append("<tr><td colspan=5>"+ra_packslip+"</td></tr>");
	if(dst.Tables["ra_detail"].Rows.Count < 5 )
	{
		sb.Append("<tr><td colspan=6>");
		for(int i=0; i<30; i++)
			sb.Append("<br>");
		sb.Append("</td></tr>");
	}
	sb.Append("</table>");

	return sb.ToString();
	
}

bool getRMADetails()
{

	string sc = "SELECT r.*, c.name AS technician, c2.name AS supplier_name, c2.company, c2.address1 ";
	sc += " , c2.address2, c2.city, c2.email, c2.trading_name, c2.tech_email, c2.fax, c2.phone ";
	sc += " FROM rma r JOIN card c ON c.id = r.technician ";
	sc += " JOIN card c2 ON c2.id = r.supplier_id ";
	if(Request.QueryString["ra"] != null && Request.QueryString["ra"] != "")
		sc += " WHERE ra_id = "+ Request.QueryString["ra"];
//DEBUG("s c =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "ra_detail");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

//	if(Request.QueryString["print"] == "form")
	{
		sc = " SELECT ticket, ship_desc FROM ra_freight WHERE ra_number = '"+ Request.QueryString["ra"] +"' ";
//DEBUG("sc = ", sc );
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "tickets");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}


</script>




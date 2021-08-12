<script runat=server>

string m_sInvoiceNumber = "";
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
//string sInvoiceNumber="";
string m_sDate="";
string m_sPackingSlipDisplay = "";
bool m_bDoSN = true;
string m_branchHeader = "";
string m_branchFooter = "";

protected void Page_Load(Object Src, EventArgs E) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	/////////////////////////
//	if(!SecurityCheck("sales"))
//		return;

		/*-- Querry Database --*/
		///////////////////////////////
	m_sInvoiceNumber = Request.QueryString["i"];
//DEBUG("msdn =", m_sInvoiceNumber);
	if(m_sInvoiceNumber == null || m_sInvoiceNumber == "")
	{
		Response.Write("<br><br><center><h3>Unable to Proceed the Packing Slip!!! No Packing Slip Number!!!");
		Response.Write("<script langauge=javascript>window.close();</script");
		Response.Write(">");
		return;
	}
	if(!TSIsDigit(m_sInvoiceNumber))
	{
		Response.Write("<br><br><center><h3>Unable to Proceed the Packing Slip!!! Invalid Packing Slip Number!!!");
		Response.Write("<script langauge=javascript>window.close();</script");
		Response.Write(">");
		return;
	}
	m_sPackingSlipDisplay = ReadSitePage("packingslip");
	StringBuilder sb = new StringBuilder();
	if(!DoGetInvoiceDetail(m_sInvoiceNumber))
		return;
	sb.Append(m_sPackingSlipDisplay);

 string email = Request.QueryString["email"];
// DEBUG("Email =", email);
// DEBUG("confirm",Request.QueryString["confirm"]);
	if(email != null && email != "")
	{
		
		if(Request.QueryString["confirm"] == "1")
		{
			Response.Write("<script Language=javascript");
			Response.Write(">");
			Response.Write("if(window.confirm('");
			Response.Write("Email packing slip to " + email + "?         ");
			Response.Write("\\r\\n\\r\\n");
			Response.Write("\\r\\nClick OK to send.\\r\\n");
			Response.Write("'))");
			Response.Write("window.location='pack.aspx?i=" + Request.QueryString["i"] + "&email=" + HttpUtility.UrlEncode(email) + "';\r\n");
			Response.Write("else window.close();\r\n");
			Response.Write("</script");
			Response.Write(">");
		}
		else
		{
			MailMessage msgMail = new MailMessage();
			msgMail.From = m_sSalesEmail;
			msgMail.To = email;
			msgMail.Subject = "Packing Slip, Invoice " + Request.QueryString[0];
			msgMail.BodyFormat = MailFormat.Html;
			msgMail.Body = sb.ToString();
			SmtpMail.Send(msgMail);
		}
		
		
		Response.Write("<br><center><h3>Packing Slip Sent.</h3>");
		Response.Write("<input type=button value='Close Window' onclick=window.close() " + Session["button_style"] + ">");
		Response.Write("<br><br><br><br><br><br>");
	}
	else
		Response.Write(sb.ToString());
		
}
bool DoGetInvoiceDetail(string sInvoiceNumber)
{
	string sc = "SELECT s.code, s.supplier_code, s.quantity, s.name AS product_desc, i.*,i.freight AS freight_charge, c.* ";
	if(Session["branch_support"] != null)
		sc += " , b.name AS branch_name, b.id AS branch_id, b.branch_header, b.branch_footer ";
	sc += ", e1.name AS credit_terms, s.pack ";
	//sc += ", c.address1, c.address2, c.address3, c.phone, c.fax ";
	sc += " FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
	sc += " JOIN card c ON c.id=i.card_id ";
	sc += " LEFT OUTER JOIN enum e1 ON e1.id = c.credit_term AND e1.class = 'credit_terms' ";
	if(Session["branch_support"] != null)
	{	
		sc += " JOIN branch b ON b.id = i.branch ";
	}
//	sc += " JOIN card c2 ON c2.id = i.card_id
	sc += " WHERE i.invoice_number=";
	sc += sInvoiceNumber;
	sc += " ORDER BY s.pack ";
//DEBUG("sc =", sc);	
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "sales");
	}
	catch(Exception e) 
	{
		ShowExp("Invalid Query String", e);
		return false; 
	}

	if(rows > 0)
	{
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@COMPANY_NAME", m_sCompanyName);
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@RECORD_DATE", dst.Tables["sales"].Rows[0]["commit_date"].ToString());
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@TODAY_DATE", DateTime.Now.ToString());
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@INVOICE_NUMBER", dst.Tables["sales"].Rows[0]["invoice_number"].ToString());
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CUSTOMER_PONUMBER", dst.Tables["sales"].Rows[0]["cust_ponumber"].ToString());
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@STAFF", dst.Tables["sales"].Rows[0]["sales"].ToString());
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@FREIGHT", MyDoubleParse(dst.Tables["sales"].Rows[0]["freight_charge"].ToString()).ToString("c"));
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CUSTOMER_ID", dst.Tables["sales"].Rows[0]["cust_ponumber"].ToString());
		if(Session["branch_support"] != null)
		{
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@BRANCH_NAME", dst.Tables["sales"].Rows[0]["branch_name"].ToString());
            string branch_header = ReadBranchHeader(dst.Tables["sales"].Rows[0]["branch_header"].ToString());
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@BRANCH_HEADER", branch_header);
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@BRANCH_FOOTER", dst.Tables["sales"].Rows[0]["branch_footer"].ToString());
		}
		else
		{
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@BRANCH_NAME", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@BRANCH_HEADER", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@BRANCH_FOOTER", "");
		}
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CREDIT_TERMS", dst.Tables["sales"].Rows[0]["credit_terms"].ToString());
		

		
		string address = "<table width=100% border=0>";
		if(MyBooleanParse(dst.Tables["sales"].Rows[0]["special_shipto"].ToString()))
		{
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["shipto"].ToString() +"</td></tr>";
			address = address.Replace("\r\n", "\r\n<br>");
			address += "</table>";		
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CUSTOMER_COMPANY", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CUSTOMER_NAME", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR1", address);
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR2", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR3", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CITY", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@FAX", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@EMAIL", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@PHONE", "");
		}
		else
		{
			
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["company"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["name"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["address1"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["address2"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["address3"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["city"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["fax"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["email"].ToString() +"</td></tr>";
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["phone"].ToString() +"</td></tr>";
			address += "</table>";
		m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR1", address);
			
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CUSTOMER_COMPANY", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CUSTOMER_NAME", "");
			//m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR1", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR2", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR3", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@CITY", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@FAX", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@EMAIL", "");
			m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@PHONE", "");
			
		}
//	DEBUG("address = ", address);
		
	//	m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ADDR1", address);
	}
	string stext = "";
	bool bAlter = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["sales"].Rows[i];
		string code = dr["code"].ToString();
//		string location = GetItemLocation(code);
		string pack = dr["pack"].ToString();
		string qty = dr["quantity"].ToString();
		string product_desc = dr["product_desc"].ToString();
		string m_code = dr["supplier_code"].ToString();
//		string sn = dr["sn"].ToString();

		stext += "<tr";
		if(bAlter)
			stext += " bgcolor=#EEEEE ";
		bAlter = !bAlter;
		stext += "><td>"+ pack +"</td><td>"+ product_desc +"</td><td>"+ m_code +"</td><td>"+ qty +"</td></tr>";
	}
	m_sPackingSlipDisplay = m_sPackingSlipDisplay.Replace("@@ITEM_LIST", stext);
	return true;

}
	
</script>

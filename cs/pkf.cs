<script runat=server>

string m_sOrderNumber = "";
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
//string sInvoiceNumber="";
string m_sDate="";
string m_sPickupFormDisplay = "";
bool m_bDoSN = true;
string m_branchHeader = "";
string m_branchFooter = "";

protected void Page_Load(Object Src, EventArgs E) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
//	if(!SecurityCheck("sales"))
//		return;

		/*-- Querry Database --*/
	m_sOrderNumber = Request.QueryString["i"];
//DEBUG("msdn =", m_sOrderNumber);
	if(m_sOrderNumber == null || m_sOrderNumber == "")
	{
		Response.Write("<br><br><center><h3>Unable to Proceed the Pickup Form!!! No Pickup Number!!!");
		Response.Write("<script langauge=javascript>window.close();</script");
		Response.Write(">");
		return;
	}
	if(!TSIsDigit(m_sOrderNumber))
	{
		Response.Write("<br><br><center><h3>Unable to Proceed the Pickup Form!!! Invalid Pickup Form Number!!!");
		Response.Write("<script langauge=javascript>window.close();</script");
		Response.Write(">");
		return;
	}
	m_sPickupFormDisplay = ReadSitePage("pickup_form_template");
	StringBuilder sb = new StringBuilder();
	if(!DoGetOrderDetail(m_sOrderNumber))
		return;
	sb.Append(m_sPickupFormDisplay);

	if(Request.QueryString["email"] != null && Request.QueryString["email"] != "")
	{
		string email = Request.QueryString["email"];
		if(Request.QueryString["confirm"] == "1")
		{
			Response.Write("<script Language=javascript");
			Response.Write(">");
			Response.Write("if(window.confirm('");
			Response.Write("Email packing slip to " + email + "?         ");
			Response.Write("\\r\\n\\r\\n");
			Response.Write("\\r\\nClick OK to send.\\r\\n");
			Response.Write("'))");
			Response.Write("window.=location'pack.aspx?i=" + Request.QueryString["i"] + "&email=" + HttpUtility.UrlEncode(email) + "';\r\n");
			Response.Write("else window.close();\r\n");
			Response.Write("</script");
			Response.Write(">");
		}
		else
		{
			MailMessage msgMail = new MailMessage();
			msgMail.From = m_sSalesEmail;
			msgMail.To = email;
			msgMail.Subject = "Pickup Form, Invoice " + Request.QueryString[0];
			msgMail.BodyFormat = MailFormat.Html;
			msgMail.Body = sb.ToString();
			SmtpMail.Send(msgMail);
		}
		Response.Write("<br><center><h3>Pickup Form Sent.</h3>");
		Response.Write("<input type=button value='Close Window' onclick=window.close() " + Session["button_style"] + ">");
		Response.Write("<br><br><br><br><br><br>");
	}
	else
		Response.Write(sb.ToString());
}
bool DoGetOrderDetail(string sInvoiceNumber)
{
	string sc = "SELECT s.code, s.supplier_code, s.quantity, s.item_name AS product_desc, i.*,i.freight AS freight_charge, c.* ";
	if(Session["branch_support"] != null)
		sc += " , b.name AS branch_name, b.id AS branch_id, b.branch_header, b.branch_footer ";
	sc += ", e1.name AS credit_terms, s.pack ";
	//sc += ", c.address1, c.address2, c.address3, c.phone, c.fax ";
	sc += " FROM order_item s JOIN orders i ON s.id=i.id ";
	sc += " JOIN card c ON c.id=i.card_id ";
	sc += " LEFT OUTER JOIN enum e1 ON e1.id = c.credit_term AND e1.class = 'credit_terms' ";
	if(Session["branch_support"] != null)
	{	
		sc += " JOIN branch b ON b.id = i.branch ";
	}
//	sc += " JOIN card c2 ON c2.id = i.card_id
	sc += " WHERE i.id=";
	sc += sInvoiceNumber;
	sc += " ORDER BY s.supplier_code ";
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
		string shipping_method = GetEnumValue("shipping_method",  dst.Tables["sales"].Rows[0]["shipping_method"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@COMPANY_NAME", m_sCompanyName);
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR1",dst.Tables["sales"].Rows[0]["address1"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR2", dst.Tables["sales"].Rows[0]["address2"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR3", dst.Tables["sales"].Rows[0]["address3"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@FAX", dst.Tables["sales"].Rows[0]["fax"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@EMAIL", dst.Tables["sales"].Rows[0]["email"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@PHONE", dst.Tables["sales"].Rows[0]["phone"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_NAME", dst.Tables["sales"].Rows[0]["name"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@RECORD_DATE", dst.Tables["sales"].Rows[0]["record_date"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@TODAY_DATE", DateTime.Now.ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ORDER_NUMBER", dst.Tables["sales"].Rows[0]["id"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_PONUMBER", dst.Tables["sales"].Rows[0]["po_number"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@STAFF", dst.Tables["sales"].Rows[0]["sales"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@FREIGHT", MyDoubleParse(dst.Tables["sales"].Rows[0]["freight_charge"].ToString()).ToString("c"));
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_ID", dst.Tables["sales"].Rows[0]["card_id"].ToString());
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@SHIPPING_METHOD", shipping_method);
		if(Session["branch_support"] != null)
		{
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@BRANCH_NAME", dst.Tables["sales"].Rows[0]["branch_name"].ToString());
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@BRANCH_HEADER", dst.Tables["sales"].Rows[0]["branch_header"].ToString());
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@BRANCH_FOOTER", dst.Tables["sales"].Rows[0]["branch_footer"].ToString());
		}
		else
		{
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@BRANCH_NAME", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@BRANCH_HEADER", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@BRANCH_FOOTER", "");
		}
		m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CREDIT_TERMS", dst.Tables["sales"].Rows[0]["credit_terms"].ToString());
		

		
		string address = "<table width=100% border=0>";
		if(MyBooleanParse(dst.Tables["sales"].Rows[0]["special_shipto"].ToString()))
		{			
			address += "<tr><td>"+ dst.Tables["sales"].Rows[0]["shipto"].ToString() +"</td></tr>";
			address = address.Replace("\r\n", "\r\n<br>");
			address += "</table>";		
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_COMPANY", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_NAME", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR1", address);
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR2", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR3", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CITY", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@FAX", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@EMAIL", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@PHONE", "");
		}
		else
		{
		//	DEBUG("m_sCompanyName =",m_sCompanyName);
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
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR1", address);
			
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_COMPANY", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CUSTOMER_NAME", "");
			//m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR1", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR2", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR3", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@CITY", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@FAX", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@EMAIL", "");
			m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@PHONE", "");
		
		}
		
//	DEBUG("address = ", address);
		
	//	m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ADDR1", address);
	}
	string stext = "";
	bool bAlter = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["sales"].Rows[i];
		string code = dr["code"].ToString();
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
	m_sPickupFormDisplay = m_sPickupFormDisplay.Replace("@@ITEM_LIST", stext);
	return true;

}
	
</script>

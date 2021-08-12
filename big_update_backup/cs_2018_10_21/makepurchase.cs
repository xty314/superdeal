<!-- #include file="purchase_function.cs" -->

<script runat=server>

const string cols = "7";	//how many columns main table has, used to write colspan=
//const string tableTitle = "Process Order";
//const string thisurl = "esales.aspx";
//bool bItemProcessing = true;
//bool m_bDodiscount = true;	//assess whether update account discount, HG 13.Aug.2002
//bool m_bOrder = false;

//string m_sDN = ""; //delivery notice sent to customer by email
string m_sInvoiceNumber;
string m_type;
int page = 1;
const int m_nPageSize = 1000; //how many rows in oen page
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	string spage = Request.QueryString["p"];
	if(spage != null)
		page = int.Parse(spage);
	m_type = Request.QueryString["t"];
	m_sInvoiceNumber = Request.QueryString["i"];
	if(!TSIsDigit(m_sInvoiceNumber))
	{
		Response.Write("<h3>Wrong Invoice Number: " + m_sInvoiceNumber + "</h3>");
		return;
	}

	if(Request.Form["cmd"] == "Build Purchase")
	{
		if(PreparePurchase())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?sup=0&inv=" + m_sInvoiceNumber + "&r=" + DateTime.Now.ToOADate().ToString() + "\">");
		return;
	}
	else
	{
		if(!GetOrder())
		{
			Response.Write("<h3>Order# " + m_sInvoiceNumber + " not found</h3>");
			return;
		}
	
		PrintAdminHeader();
		PrintAdminMenu();
		PrintJavaFunctions();
		WriteHeaders();

		if (!GetShipDetails())
			return;

		if(!GetCustomerDetails())
			return;

		MyDrawTable();
		WriteFooter();
	}
	PrintAdminFooter();
}

bool GetCustomerDetails()
{
	int rows = 0;
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM invoice WHERE invoice_number=" + m_sInvoiceNumber);
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		rows = myAdapter.Fill(dst, "invoice");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

bool GetShipDetails()
{
	int rows = 0;
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM ship");
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		rows = myAdapter.Fill(dst, "ship");

	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

bool GetOrder()
{
	int rows = 0;
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM sales WHERE invoice_number=" + m_sInvoiceNumber);
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		rows = myAdapter.Fill(dst, "product");
//DEBUG("rows=", rows);
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
//	Response.Write("<td width=50>INVOICE#</td>\r\n");
//	Response.Write("<td>DATE/TIME</td>\r\n");
	Response.Write("<td>CODE</td>");
	Response.Write("<td>NAME</td>");
	Response.Write("<td>QTY</td>");
	Response.Write("<td>SUPPLIER_CODE</td>");
	Response.Write("<td>SUPPLIER</td>");
	Response.Write("<td align=right>SELECT</td>");
	Response.Write("</tr>\r\n");
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
//	string invoice = dr["invoice_number"].ToString();
	string code = dr["code"].ToString();
	string name = dr["name"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string quantity = dr["quantity"].ToString();
//	string status = dr["status"].ToString();
//	string shipby = dr["shipby"].ToString();
//	string ticket = dr["ticket"].ToString();
//	string note = dr["note"].ToString();
	bool bSystem = (bool)dr["system"];

//	string date = dst.Tables["invoice"].Rows[0]["commit_date"].ToString();
	string index = i.ToString();

//	Response.Write("<input type=hidden name=invoice" + index + " value='" + invoice + "'>");
	Response.Write("<input type=hidden name=code" + index + " value='" + code + "'>");

	Response.Write("<tr");
	if(bSystem)
		Response.Write(" bgcolor=#FFFFEE");
	else if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");
//	Response.Write("<td><a href=invoice.aspx?" + invoice + " target=_blank>");
//	Response.Write(invoice);
//	Response.Write("</a></td><td>");
//	Response.Write(date);
//	Response.Write("</td><td>");
	Response.Write("<td>");
	Response.Write(code);
	Response.Write("</td><td>");
	Response.Write(name);
	Response.Write("</td><td>");
	Response.Write(quantity);
	Response.Write("</td><td>");
	Response.Write(supplier_code);
	Response.Write("</td><td>");
	Response.Write(supplier);
	Response.Write("</td><td align=right>");
	Response.Write("<input type=checkbox name=sel" + index + ">");
	Response.Write("</td>");

	Response.Write("</tr>");
	return true;
}

void WriteHeaders()
{
	Response.Write("<br><center><h3>Make Purchase</h3>");
	Response.Write("\r\n<form name=form action=makepurchase.aspx?i=");
	Response.Write(m_sInvoiceNumber);
	Response.Write("&t=update&p=");
	Response.Write(page);
	Response.Write(" method=post>\r\n");
	Response.Write("\r\n\r\n<table width=100% height=100% bgcolor=white align=center>");
	Response.Write("\r\n<tr><td>");
}

void WriteFooter()
{
	DataRow dr = dst.Tables["invoice"].Rows[0];
	Response.Write("<tr valign=top><td>");
	Response.Write(WriteCustomerDetails());
	Response.Write("</td></tr>");
	Response.Write("</table>\r\n");

	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

string WriteCustomerDetails()
{
	DataRow dr = dst.Tables["invoice"].Rows[0];
	StringBuilder sb = new StringBuilder();
	sb.Append("<table valign=top colspan=4><tr><td colspan=2><font size=+1><b>Shipping Address</b></font></td></tr>");

	sb.Append("<tr><td><b>Name</b></td>");
	sb.Append("<td>" + dr["name"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>Company</b></td>");
	sb.Append("<td>" + dr["company"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>Address</b></td>");
	sb.Append("<td>" + dr["address1"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>&nbsp;</b></td>");
	sb.Append("<td>" + dr["address2"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>City</b></td>");
	sb.Append("<td>" + dr["city"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>Country</b></td>");
	sb.Append("<td>" + dr["country"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>Phone</b></td>");
	sb.Append("<td>" + dr["phone"].ToString() + "</td></tr>");
	
	sb.Append("<tr><td><b>Email</b></td>");
	sb.Append("<td>" + dr["email"].ToString() + "</td></tr>");
	
	sb.Append("</table><br>\r\n");
	return sb.ToString();
}

Boolean MyDrawTable()
{
	Boolean bRet = true;

	DataRow dr = dst.Tables["product"].Rows[0];
/*
	Response.Write("\r\n\r\n<table align=right cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr>");
	Response.Write("<td>&nbsp;&nbsp;&nbsp;<a href=esales.aspx?&i=");
	Response.Write(m_sInvoiceNumber);
	Response.Write("&p=");
	Response.Write(page);
	Response.Write("</a>&nbsp;&nbsp;&nbsp;</td>");

	Response.Write("</tr></table>");
	//end of status
*/
//	Response.Write("</td></tr><tr><td>");

	DrawTableHeader();
	string s = "";
//	DataRow dr;
	Boolean alterColor = true;
	int startPage = (page-1) * m_nPageSize;
	for(int i=startPage; i<dst.Tables["product"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
		{
			bRet = false;
			break;
		}
	}

	Response.Write("<tr><td colspan=" + cols + " align=right><b>Select All </b>");
	Response.Write("<input type=checkbox name=allbox value='Select All' onClick='CheckAll();'>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=" + cols + " align=right>");
//	Response.Write("<b>Supplier : </b>");
//	Response.Write(PrintSupplierOptions());
	
	Response.Write("&nbsp&nbsp;<input type=submit name=cmd value='Build Purchase'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=" + cols + " align=right>Page: ");
	int pages = dst.Tables["product"].Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != page)
		{
			Response.Write("<a href=esales.aspx?i=");
			Response.Write(m_sInvoiceNumber);
			Response.Write("&p=");
			Response.Write(i.ToString());
			Response.Write(">");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write(i.ToString());
			Response.Write(" ");
		}
	}
	Response.Write("</td>");
	Response.Write("</table>\r\n");
	return bRet;
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.form.elements.length;i++) 
		{
			var e = document.form.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form.allbox.checked;
		}
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
}

bool PreparePurchase()
{
	if(!PrepareNewPurchase())
	{
		Response.Write("<br><br><center><h3>PrepareNewPurchase falied</h3>");
		return false;
	}

	if(!GetOrder())
	{
		Response.Write("<br><br><center><h3>Error Getting New Order</h3>");
		return false;
	}
	
	for(int i=0; i<dst.Tables["product"].Rows.Count; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
		{
			DataRow dr = dst.Tables["product"].Rows[i];
			string code = dr["code"].ToString();
			string name = dr["name"].ToString();
			string supplier = dr["supplier"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string supplier_price = dr["supplier_price"].ToString();
			string quantity = dr["quantity"].ToString();
//DEBUG("qty=", quantity);
			//add to cart
			AddToCart(code, supplier, supplier_code, quantity, supplier_price, "", name);
		}
	}
	return true;
}

Boolean UpdateAllRows()
{
	int i = (page-1) * m_nPageSize;
	string invoice = Request.Form["invoice"+i.ToString()];
	while(invoice != null)
	{
		if(!UpdateOneRow(i.ToString()))
			return false;;
		i++;
		invoice = Request.Form["invoice"+i.ToString()];
	}

	return true;
}

Boolean UpdateOneRow(string sRow)
{
	Boolean bRet = true;

	string invoice = Request.Form["invoice"+sRow];
	string code = Request.Form["code"+sRow];
	string newShip = "";
	string status = "";
	string status_old = "";
	string shipby = "";
	string ticket = "";
	string note = "";

	if(status_old != "Deliveried")
	{
		if(ticket != "" && status == "Deliveried")
		{
			status = "Deliveried"; // force update status
		}
	}
	StringBuilder sb = new StringBuilder();
	sb.Append("UPDATE sales SET status='");
	sb.Append(status);
	sb.Append("', shipby='");
	sb.Append(shipby);
	sb.Append("', ticket='");
	sb.Append(ticket);
	sb.Append("', note='");
	sb.Append(note);
	sb.Append("', ship_date=");
	sb.Append("GETDATE()");
	sb.Append(", processed_by='");
	sb.Append(Session["name"].ToString());
	sb.Append("', p_status='");
	if(status.ToLower() == "payment confirmed")
	{
		sb.Append("open'");
	}
	else
	{
		sb.Append("pendding'");
	}
	sb.Append(" WHERE invoice_number=");
	sb.Append(invoice);
	sb.Append(" AND code=");
	sb.Append(code);
	try
	{
//DEBUG("sc=", sb.ToString());
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

</script>
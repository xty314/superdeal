<script runat=server>

const string cols = "11";	//how many columns main table has, used to write colspan=
const string tableTitle = "Process Order";
const string thisurl = "esales.aspx";
bool m_bProcess = false; //processing items, not viewing orders
bool m_bHaveLocks = false;

string m_sInvoiceNumber;
string m_type;
int page = 1;
const int m_nPageSize = 100; //how many rows in oen page
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["jt"] == "i")
		m_bProcess = true;
	else if(Request.QueryString["jt"] == "a")
		m_bProcess = false;

	string spage = Request.QueryString["p"];
	if(spage != null)
		page = int.Parse(spage);
	m_type = Request.QueryString["t"];
//	m_sInvoiceNumber = Request.QueryString["i"];
//	if(!TSIsDigit(m_sInvoiceNumber))
//	{
//		Response.Write("<h3>Wrong Invoice Number: " + m_sInvoiceNumber + "</h3>");
//		return;
//	}

//	PrintAdminHeader();
//	PrintAdminMenu();
	WriteHeaders();
	PrintJavaFunctions();

	if(m_type == "update")
	{
		bool bSuccess = false;
		string cmd = Request.Form["cmd"];
		if(cmd == "Update")
			bSuccess = UpdateAllRows();
		else if(cmd == "Delete")
			bSuccess = DoDelete();
		if(bSuccess)
		{
			string s = "<br><br>done! wait a moment......... <br>\r\n";
			s += "<meta http-equiv=\"refresh\" content=\"1; URL=esales.aspx?i=";
			s += m_sInvoiceNumber;
			s += "&p=";
			s += page;
			if(m_bProcess)
				s += "&jt=i";
			else
				s += "&jt=a";
			s += "\"></body></html>";
			Response.Write(s);
		}
	}
	else
	{
		if(!GetOrder())
		{
			Response.Write("<h3>Order# " + m_sInvoiceNumber + " not found</h3>");
			return;
		}

		if(!GetCustomerDetails())
			return;
	
		MyDrawTable();
		WriteFooter();
	}
//	PrintAdminFooter();
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

bool GetOrder()
{
	int rows = 0;
	string sc = "SELECT * FROM sales WHERE supplier='TP' AND p_status='open' OR p_status='locked' ORDER by invoice_number";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "product");
//DEBUG("rows=", rows);
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	m_sInvoiceNumber = dst.Tables["product"].Rows[0]["invoice_number"].ToString();
	return true;
}

bool DoDelete()
{
	if(Request.Form["delete"] != "on")
	{
		Response.Write("<h3>Error, please tick the checkbox to confirm deletion</h3>");
		return false;
	}

	string sc = "DELETE FROM sales WHERE invoice_number=" + m_sInvoiceNumber;
	try
	{
//DEBUG("sc=", sb.ToString());
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
	
	sc = "DELETE FROM invoice WHERE invoice_number=" + m_sInvoiceNumber;
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
	string status = "";
	string shipby = "";
	string ticket = "";
	string note = "";

	if(m_bProcess)
	{
		status = Request.Form["status"+sRow];
		shipby = Request.Form["shipby"+sRow];
		ticket = Request.Form["ticket"+sRow];
		note = Request.Form["note"+sRow];
	}
	else
	{
		status = Request.Form["status"];
		shipby = Request.Form["shipby"];
		ticket = Request.Form["ticket"];
		note = Request.Form["note"];
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
	sb.Append("' WHERE invoice_number=");
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

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=1 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=50>INVOICE#</td>\r\n");
	Response.Write("<td>DATE/TIME</td>\r\n");
//	Response.Write("<td>CODE</td>");
	Response.Write("<td>NAME</td>");
	Response.Write("<td>SUPPLIER&nbsp;</td>");
	Response.Write("<td>CODE&nbsp;</td>");
	Response.Write("<td>QTY&nbsp;</td>");

	Response.Write("<td>STATUS&nbsp;</td>");
	Response.Write("<td>OWNER&nbsp;</td>");
	Response.Write("<td>SELECT</td>");
	
	Response.Write("</tr>\r\n");
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
	string invoice = dr["invoice_number"].ToString();
	string code = dr["code"].ToString();
	string name = dr["name"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string quantity = dr["quantity"].ToString();
	string status = dr["status"].ToString();
	string shipby = dr["shipby"].ToString();
	string ticket = dr["ticket"].ToString();
	string note = dr["note"].ToString();
	bool bSystem = (bool)dr["system"];
	string p_status = dr["p_status"].ToString();
	bool bLock = false;
	string owner = ""; //who locked it
	if(p_status.ToLower() == "locked")
	{
		bLock = true;
		owner = dr["owner"].ToString();
		if(owner == Session["name"].ToString())
			m_bHaveLocks = true;
	}
//	string  = dr[""].ToString();

	string date = dst.Tables["invoice"].Rows[0]["commit_date"].ToString();
	string index = i.ToString();

	Response.Write("<input type=hidden name=invoice" + index + " value='" + invoice + "'>");
	Response.Write("<input type=hidden name=code" + index + " value='" + code + "'>");

	Response.Write("<tr");
	if(bLock)
		Response.Write(" bgcolor=yellow");
	else if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write("><td>");
	Response.Write(invoice);
	Response.Write("</td><td>");
	Response.Write(date);
//	Response.Write("</td><td>");
//	Response.Write(code);
	Response.Write("</td><td>");
	Response.Write(name);
	Response.Write("</td><td>");
	Response.Write(supplier);
	Response.Write("</td><td>");
	Response.Write(supplier_code);
	Response.Write("</td><td>");
	Response.Write(quantity);
	Response.Write("</td>");

/*	if(m_bProcess)
	{
		Response.Write("<td><select name=shipby" + index + ">");
		Response.Write(AddShips("", shipby));
		Response.Write(AddShips("Courier", shipby));
		Response.Write(AddShips("Fedex", shipby));
		Response.Write(AddShips("DHL", shipby));
		Response.Write(AddShips("EMS", shipby));
		Response.Write("</select>");

		Response.Write("</td><td>");

		Response.Write("<input type=text size=10 name=ticket");
		Response.Write(index);
		Response.Write(" value='");
		Response.Write(ticket);
		Response.Write("'>");

		Response.Write("</td><td>");

		Response.Write("<input type=text size=10 name=note");
		Response.Write(index);
		Response.Write(" value='");
		Response.Write(note);
		Response.Write("'>");
	}
	else
*/	{
		Response.Write("<td>" + p_status + "</td><td>" + owner + "</td>");
		Response.Write("<td align=right>");
		Response.Write("<input type=checkbox name=sel>");
		Response.Write("</td>");
	}
	Response.Write("</tr>");

	return true;
}

string AddShips(string sName, string shipby)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<option value='" + sName + "'");
	if(shipby == sName)
		sb.Append(" selected");
	sb.Append(">" + sName + "</option>");
	return sb.ToString();
}

void WriteHeaders()
{
	Response.Write("<html><header><style type=\"text/css\">td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
	Response.Write("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>");
	Response.Write("<title>ORDER " + m_sCompanyTitle + "</title></header>");
	Response.Write("<body bgcolor=#666696>\r\n");
	Response.Write("<table width=100% height=100% bgcolor=white align=center valign=top><tr><td valign=top>");

	Response.Write("\r\n<form action=tp.aspx?i=");
	Response.Write(m_sInvoiceNumber);
	Response.Write("&p=");
	Response.Write(page);
//	if(m_bProcess)
//		Response.Write("&jt=p"); //process
//	else
//		Response.Write("&jt=v"); //view 
	Response.Write(" method=post name=form1>\r\n");
	Response.Write("\r\n\r\n<table width=100% bgcolor=white align=center valign=top>");
	Response.Write("\r\n<tr><td valign=top>");
	Response.Write("<br><center><h3>" + tableTitle + "</h3></center></td></tr><tr><td valign=top>");
}

void WriteFooter()
{
	Response.Write("<tr valign=top><td>");
	if(m_bProcess)
		WriteCustomerDetails();
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("</td></tr></table></body></html>");
}

void WriteCustomerDetails()
{
	DataRow dr = dst.Tables["invoice"].Rows[0];
	Response.Write("<table valign=top><tr><td colspan=2><font size=+1><b>Shipping Address</b></font></td></tr>");

	Response.Write("<tr><td><b>Name</b></td>");
	Response.Write("<td>" + dr["name"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>Company</b></td>");
	Response.Write("<td>" + dr["company"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>Address</b></td>");
	Response.Write("<td>" + dr["address1"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>&nbsp;</b></td>");
	Response.Write("<td>" + dr["address2"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>City</b></td>");
	Response.Write("<td>" + dr["city"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>Country</b></td>");
	Response.Write("<td>" + dr["country"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>Phone</b></td>");
	Response.Write("<td>" + dr["phone"].ToString() + "</td></tr>");
	
	Response.Write("<tr><td><b>Email</b></td>");
	Response.Write("<td>" + dr["email"].ToString() + "</td></tr>");
	
//	Response.Write("<tr><td><b></b></td>");
//	Response.Write("<td>" + dr[""].ToString() + "</td></tr>");

	Response.Write("</table>\r\n");
}

Boolean MyDrawTable()
{
	Boolean bRet = true;

//	Response.Write("<form action=tp.aspx method=post>");
	Response.Write("\r\n\r\n<table align=center valign=top cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("\r\n<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	if(m_bProcess)
	{
		Response.Write("<td>STATUS</td>");
		Response.Write("<td>SHIP BY</td>");
		Response.Write("<td>TICKET#</td>");
		Response.Write("<td>NOTES</td>");
	}
//	Response.Write("<td>JOB TYPE</td>");
	Response.Write("</tr>\r\n");
	
/*	Response.Write("<tr>");

	string shipby = "";
	Response.Write("<select name=shipby>");
	Response.Write(AddShips("", shipby));
	Response.Write(AddShips("Courier", shipby));
	Response.Write(AddShips("Fedex", shipby));
	Response.Write(AddShips("DHL", shipby));
	Response.Write(AddShips("EMS", shipby));
	Response.Write("</select>");
	Response.Write("</td><td>");
	Response.Write("<input type=text size=10 name=ticket value=''>");
	Response.Write("</td><td>");
	Response.Write("<input type=text size=10 name=note value=''></td>");

	Response.Write("<td>&nbsp;&nbsp;&nbsp;<a href=esales.aspx?&i=");
	Response.Write(m_sInvoiceNumber);
	Response.Write("&p=");
	Response.Write(page);
	if(m_bProcess)
		Response.Write("&jt=a>Invoice Processing");
	else
		Response.Write("&jt=i>Item Processing");
	Response.Write("</a>&nbsp;&nbsp;&nbsp;</td>");

	Response.Write("</tr></table>");
	//end of status

	Response.Write("</td></tr>");
*/
	Response.Write("<tr><td>");

	DrawTableHeader();
	string s = "";
	DataRow dr;
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

	Response.Write("<tr><td colspan=" + cols + " align=right>");

	if(!SecurityCheck("Manager"))
	{
		Response.Write("<input type=checkbox name=delete> Delete this order ");
		Response.Write("<input type=submit name=cmd value=Delete>&nbsp;&nbsp;");
	}
	Response.Write("<input type=checkbox name=allbox value='Select All' onClick='CheckAll();'>");
	if(m_bHaveLocks)
	{
		Response.Write("&nbsp;&nbsp;");
		Response.Write("<input type=submit name=cmd value=Unlock>");
	}
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Page: ");
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
	Response.Write("</td><td colspan=" + (int.Parse(cols)-1).ToString() + " align=right><input type=submit name=cmd value=Process>");
	Response.Write("</td></tr>");
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
		for (var i=0;i<document.form1.elements.length;i++) 
		{
			var e = document.form1.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form1.allbox.checked;
		}
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
}

</script>
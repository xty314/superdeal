<!-- #include file="page_index.cs" -->

<script runat="server">

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_search = "";
string m_command = "";
string m_querystring = "";
string m_invoice = "";

bool bHide = true;
bool m_bIsFound = false;  //flag for searching sn on the database
bool m_bIsFirst = true;
bool m_bVerified = false; // verified for public site


void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	InitializeData();
	GetAllQueryString();
	
	if(m_invoice == "" || m_invoice == null)
	{
		Response.Write("<br><center><h4>No Invoice Found!!! Please Try Again</h4>");
		Response.Write("<br><a title='back to order list' href='olist.aspx?o=11&r="+ DateTime.Now.ToOADate() +"' class=o>Back to Order List</a>");
		return;
	}
	if(!TSIsDigit(m_invoice))
	{
		Response.Write("<br><center><h4>Invalid Invoice#!!! Please Try Again</h4>");
		Response.Write("<br><a title='back to order list' href='olist.aspx?o=11&r="+ DateTime.Now.ToOADate() +"' class=o>Back to Order List</a>");
		return;
	}
	if(m_command.ToLower() == "update sales note")
	{
		if(DoInsertNotesToInvoice())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?i="+ m_invoice +"&r="+ DateTime.Now.ToOADate() +" \">");
			return;
		}
	}
	if(Request.QueryString["inv"] != null && Request.QueryString["inv"] != "")
	{
		if(!ShowTraceNote())
			return;
	}
	else
	{
		if(!QueryInvoiceInfo())
			return;
	}

}


bool ShowTraceNote()
{
	bool bNoRecord = false;
	if(TSIsDigit(m_invoice))
	{
		string sc = " SELECT i.*, c.name FROM invoice_note i JOIN card c ON c.id = i.staff_id ";
		sc += " WHERE invoice_number = "+ m_invoice;
		int rows = 0;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "note_trace");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		if(rows > 0)
		{
			Response.Write("<br><h4><center>NOTE TRACE INV#"+ m_invoice +" </h4>");
			
			Response.Write("<table align=center width=90% cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr align=left bgcolor=#DDAAAA><th>INVOICE#</th><th>RECORD_DATE</th><th>STAFF</th><th>NOTES</th></tr>");
			bool bAlter = false;
			for(int i=0; i<rows; i++)
			{
				DataRow dr = dst.Tables["note_trace"].Rows[i];
				Response.Write("<tr");
				if(bAlter)
					Response.Write(" bgcolor=#EEEEEE ");
				Response.Write(">");
				bAlter = !bAlter;
				
				Response.Write("<td>"+ dr["invoice_number"].ToString() +"</td>");
				Response.Write("<td>"+ dr["record_date"].ToString() +"</td>");
				Response.Write("<td>"+ dr["name"].ToString() +"</td>");
				Response.Write("<td>"+ dr["notes"].ToString() +"</td>");
				Response.Write("</tr>");

			}
			Response.Write("<tr><td colspan=4 align=right>");
			Response.Write("<br><a href='"+ Request.ServerVariables["URL"] +"?i="+ m_invoice +"&r="+ DateTime.Now.ToOADate() +"' class=o>Back to Notes</a>");
			Response.Write("</td></tr>");
			Response.Write("</table>");
		}
		else
			bNoRecord = true;
	}
	else
		bNoRecord = true;

	if(bNoRecord)
	{
		Response.Write("<br><center><h4>No Trace Notes!!!</h4>");
		Response.Write("<br><a href='"+ Request.ServerVariables["URL"] +"?i="+ m_invoice +"&r="+ DateTime.Now.ToOADate() +"' class=o>Back to Notes</a>");
		return false;
	}

	return true;
}

void GetAllQueryString()
{
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		m_command = Request.Form["cmd"];
	if(Request.Form["search"] != null && Request.Form["search"] != "")
		m_search = Request.Form["search"];
	if(Request.QueryString["i"] != null && Request.QueryString["i"] != "")
		m_invoice = Request.QueryString["i"];
	if(Request.QueryString["inv"] != null && Request.QueryString["inv"] != "")
		m_invoice = Request.QueryString["inv"];
}



bool DoInsertNotesToInvoice()
{
	
	string notes = Request.Form["notes"];

	if(notes != "")
	{
		//notes = "\r\n" + notes +"\r\n";
		string sc = "  ";
		sc += " UPDATE invoice SET sales_note = '" + EncodeQuote(notes) +"' ";
		sc += " WHERE invoice_number = "+ m_invoice +" ";

		sc += " SET DATEFORMAT dmy INSERT INTO invoice_note (staff_id, notes, record_date, invoice_number) ";
		sc += " VALUES('"+ Session["card_id"] +"', '"+ EncodeQuote(notes) +"', GETDATE(), '"+ m_invoice +"'";
		sc += ")";
//DEBUG("sc = ", sc);

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




bool QueryInvoiceInfo()
{
	
	string sc = " SELECT i.invoice_number, i.commit_date, i.total, i.price, i.tax, i.sales_note, c.name, c.id, c.company, c.phone ";
	sc += ", c.address1, c.address2, c.city, c.email, c.fax, c.trading_name ";
	sc += " FROM invoice i JOIN card c ON c.id = i.card_id ";
	sc += " WHERE i.invoice_number = "+ m_invoice;
		
//DEBUG("sc = ", sc );
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

	if(rows > 0)
	{

		Response.Write("<form name=frm method=post>");
		Response.Write("<br><h4><center>SALES NOTE ON TAX INVOICE# "+ m_invoice +" </h4>");
		Response.Write("<hr size=0 color=gray width=60%>");
		Response.Write("<table align=center cellspacing=0 cellpadding=3 border=0 bordercolor=#CCCCCC");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		
		DataRow dr = dst.Tables["invoice"].Rows[0];
		string invoice = dr["invoice_number"].ToString();
		string total = dr["total"].ToString();
		string sales_note = dr["sales_note"].ToString();
		string name = dr["name"].ToString();
		string company = dr["company"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string addr1 = dr["address1"].ToString();
		string addr2 = dr["address2"].ToString();
		string city = dr["city"].ToString();
		string phone = dr["phone"].ToString();
		string fax = dr["fax"].ToString();
		string email = dr["email"].ToString();
		string commit_date = dr["commit_date"].ToString();
		string sAlign = "right";

		Response.Write("<tr><th align="+ sAlign +">INVOICE# :</th><td>");
		Response.Write("<a title='view invoice' href='invoice.aspx?i="+ invoice +"' target=new class=o>");
		Response.Write(""+ invoice +"</a>");
		Response.Write(" &nbsp;<a title='Trace Sales Notes' href='"+ Request.ServerVariables["URL"] +"?inv="+ invoice +"&r="+ DateTime.Now.ToOADate() +"' class=o>trace</a>");
		Response.Write("</td></tr>");		
		
		Response.Write("<tr><th align="+ sAlign +">INVOICE DATE# :</th><td>"+ commit_date +"</td></tr>");		
		Response.Write("<tr><th align="+ sAlign +">CUSTOMER DETAIL :</th><td>"+ name +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>"+ company +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>"+ trading_name +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>"+ addr1 +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>"+ addr2 +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>"+ city +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>ph: "+ phone +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>fax: "+ fax +"</td></tr>");		
		Response.Write("<tr><th>&nbsp;</th><td>email: "+ email +"</td></tr>");		
		Response.Write("<tr><th align="+ sAlign +">SALES NOTE#</th><td><textarea name=notes rows=10 cols=50>"+ sales_note +"</textarea></td></tr>");		
		
		Response.Write("<tr><td colspan=2  align="+ sAlign +">");
		Response.Write("<input type=button value='Back to Order List' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('olist.aspx?kw="+ invoice +"&r="+ DateTime.Now.ToOADate() +"')\" >");
		Response.Write("<input type=submit name=cmd value='UPDATE SALES NOTE' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"if(!confirm('ARE YOU SURE WANT TO UPDATE SALES NOTE')){return false;}\" >");
		Response.Write("</td></tr>");		
		
		Response.Write("</table>");
		Response.Write("</form>");

	}

	return true;
}







</script>
<asp:Label id=LFooter runat=server/>

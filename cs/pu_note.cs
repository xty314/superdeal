<!-- #include file="page_index.cs" -->

<script runat="server">

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_search = "";
string m_command = "";
string m_querystring = "";
string m_invoice = "";
string m_kid ="";

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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?i="+ m_invoice +"&ri="+m_kid+"&r="+ DateTime.Now.ToOADate() +" \">");
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
		string sc = " SELECT i.*, c.name FROM purchase_note i JOIN card c ON c.id = i.staff_id ";
		sc += " WHERE i.po_number = "+ m_invoice+" AND i.id="+ m_kid;
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
				
				Response.Write("<td>"+ dr["po_number"].ToString() +"</td>");
				Response.Write("<td>"+ dr["record_date"].ToString() +"</td>");
				Response.Write("<td>"+ dr["name"].ToString() +"</td>");
				Response.Write("<td>"+ dr["notes"].ToString() +"</td>");
				Response.Write("</tr>");

			}
			Response.Write("<tr><td colspan=4 align=right>");
			Response.Write("<br><a href='"+ Request.ServerVariables["URL"] +"?i="+ m_invoice +"&r="+ DateTime.Now.ToOADate() +"&ri="+m_kid+"' class=o>Back to Notes</a>");
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
		Response.Write("<br><a href='"+ Request.ServerVariables["URL"] +"?i="+ m_invoice +"&r="+ DateTime.Now.ToOADate() +"&ri="+m_kid+"' class=o>Back to Notes</a>");
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
	if(Request.QueryString["ri"] != null && Request.QueryString["ri"] != "")
	   m_kid = Request.QueryString["ri"];
	
}



bool DoInsertNotesToInvoice()
{
	
	string notes = Request.Form["notes"];

	if(notes != "")
	{
		//notes = "\r\n" + notes +"\r\n";
		string sc = "  ";
		sc += " UPDATE purchase SET note = '" + EncodeQuote(notes) +"' ";
		sc += " WHERE po_number = "+ m_invoice +" AND id= "+m_kid+" ";

	sc += " SET DATEFORMAT dmy INSERT INTO purchase_note (staff_id, notes, record_date, po_number, id) ";
		sc += " VALUES('"+ Session["card_id"] +"', '"+ EncodeQuote(notes) +"', GETDATE(), '"+ m_invoice +"', "+m_kid;
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
	
	int rows = 0;
	if(dst.Tables["purchase_note"] != null)
		dst.Tables["purchase_note"].Clear();
	string sc ="SELECT *, b.name AS branchName FROM purchase p JOIN card c ON p.staff_id = c.id JOIN branch b ON c.our_branch = b.id where p.po_number = "+ m_invoice +" AND p.id= "+m_kid;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "purchase_note");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG("ROWS ", rows.ToString());
	if(rows > 0)
	{

		DataRow dr = dst.Tables["purchase_note"].Rows[0];
		string invoice = dr["po_number"].ToString();
		string total = dr["total_amount"].ToString();
		string sales_note = dr["note"].ToString();
		string name = dr["name"].ToString();
		string company = dr["company"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string addr1 = dr["address1"].ToString();
		string addr2 = dr["address2"].ToString();
		string city = dr["city"].ToString();
		string phone = dr["phone"].ToString();
		string fax = dr["fax"].ToString();
		string email = dr["email"].ToString();
		string commit_date = dr["date_create"].ToString();
		string branch_name= dr["branchName"].ToString();
		string sAlign = "right";

		
		Response.Write("<br><br><br>");
		Response.Write("<form name=frm method=post>");
		Response.Write("<table align=center cellspacing=0 cellpadding=0 width=80% valign=center bgcolor=white border=0 >");
		Response.Write("<tr><td width='17' height='30' id='top-header1'>&nbsp;</td>");
		Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3>PURCHASE NOTE ON PO NUMBER# "+ m_invoice +"</td>");
		Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
		Response.Write("<td  height='30' id='top-header4'>&nbsp;</td>");
		Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
		Response.Write("</tr></table>");
		
		Response.Write("<table align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white width=80%>");
		Response.Write("<tr align=left><th align="+sAlign+">P.O Number:</th><th><a title='view invoice' href='purchase.aspx?n="+ m_kid +"' target=new class=o>"+invoice+" </a>|<a title='Trace Sales Notes'");
		Response.Write(" href='"+ Request.ServerVariables["URL"] +"?inv="+ invoice +"&ri="+m_kid+"&r="+ DateTime.Now.ToOADate() +"' class=o > Trace Note </a></th>");
		Response.Write("<th align="+ sAlign +">INVOICE DATE# :</th><td>"+ commit_date +"</td></tr>");		
		Response.Write("<tr><th align="+ sAlign +">Purchase Staff :</th><td>"+ name +"</td>");		
		Response.Write("<th align=right>Branch</th><td>"+ branch_name +"</td></tr>");		
		Response.Write("<tr><th align=right >Address:</th><td colspan=3>"+ trading_name +"</td>");		
		
		Response.Write("<tr><th align=right>Phone:</th><td> "+ phone +"</td>");		
		Response.Write("<th align=right>Fax:</th><td> "+ fax +"</td></tr>");		
		Response.Write("<tr><th align=right>Email:</th><td> "+ email +"</td></tr>");		
		Response.Write("<tr><th align=center colspan=4 >Purchase NOTE</th></tr>");
		Response.Write("<tr><td colspan=4 align=center><textarea name=notes rows=10 cols=100>"+ sales_note +"</textarea></td></tr>");		
		
		Response.Write("<tr><td colspan=4  align=center>");
		Response.Write("<input type=button value='Back to Order List' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('plist.aspx?kw="+ invoice +"&r="+ DateTime.Now.ToOADate() +"')\" >");
		Response.Write("<input type=submit name=cmd value='UPDATE PURCHASE NOTE' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"if(!confirm('ARE YOU SURE WANT TO UPDATE SALES NOTE')){return false;}\" >");
		Response.Write("</td></tr>");		
		
		Response.Write("</table>");
		Response.Write("</form>");

	}

	return true;
}







</script>
<asp:Label id=LFooter runat=server/>

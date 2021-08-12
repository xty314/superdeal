
<script runat=server>
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;
	//InitializeData(); //init functions

	//PrintAdminHeader();
	//PrintAdminMenu();
	if(!getCustomer())
		return;
	//BindCustomer();
	//LFooter.Text = m_sAdminFooter;
}

bool getCustomer()
{
	int rows = 0;
	string card_id = "";
	if(Request.QueryString["id"] != "" && Request.QueryString["id"] != null)
		card_id = Request.QueryString["id"].ToString();
	else
		return true;
	string sc = "SELECT c.*, e.name AS card_type, e1.name AS term, c2.name AS sales_manager ";
	if(Session["branch_support"] != null)
		sc += " , b.name AS branchName ";
	sc += " FROM card c LEFT OUTER JOIN enum e ON e.id = c.type AND e.class='card_type' ";
	sc += " LEFT OUTER JOIN enum e1 ON e1.id = c.credit_term AND e1.class = 'credit_terms' ";
	if(Session["branch_support"] != null)
		sc += " JOIN branch b ON b.id = c.our_branch ";
	sc += " LEFT OUTER JOIN card c2 ON c.sales = c2.id ";
	sc += " WHERE c.id = "+ card_id +" ";
	
	sc += " ORDER BY c.id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<center><h5><font color=Red>NO Customer Details</h5></font></center>");
		return false;
	}

	DataRow dr = dst.Tables["card"].Rows[0];
	string id = dr["id"].ToString();
	string name = dr["name"].ToString();
	string company = dr["company"].ToString();
	string email = dr["email"].ToString();
	string trading_name = dr["trading_name"].ToString();
	string contact = dr["contact"].ToString();
	string phone = dr["phone"].ToString();
	string addr1 = dr["address1"].ToString();
	string addr2 = dr["address2"].ToString();
	string city = dr["city"].ToString();
	string fax = dr["fax"].ToString();
	string dealer_level = dr["dealer_level"].ToString();
	string card_type = dr["card_type"].ToString();
	string credit_term = dr["term"].ToString();
	string sales_manager = dr["sales_manager"].ToString();
	string note = dr["note"].ToString();
	string branchName = "";
	if(Session["branch_support"] != null)
		branchName = dr["branchName"].ToString();
	//Response.Write("<center><h5>Customer Details</h5></center>");
	Response.Write("<table align=center width=95% cellspacing=0 cellpadding=1 border=1 bordercolor=#83CCF6 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#6DE2A7><th colspan=2>Customer Details</th></tr>");
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6 nowrap>Customer ID:</th><td> "+ id +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Name:</th><td> "+ name +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Trading Name:</th><td> "+ trading_name +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Company:</th><td> "+ company +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Address:</th><td> "+ addr1 +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>&nbsp;</th><td> "+ addr2 +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>&nbsp;</th><td> "+ city +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Email:</th><td> "+ email +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Phone:</th><td> "+ phone +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Fax:</th><td> "+ fax +"</td></tr>");
//	Response.Write("<tr><td align=right bgcolor=#83CCF6>Contact:</th><td> "+ contact +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Dealer Level:</th><td> "+ dealer_level +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Card Type:</th><td> "+ card_type +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Credit Term:</th><td> "+ credit_term +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Sales Manager:</th><td> "+ sales_manager +"</td></tr>");
	if(Session["branch_support"] != null)
		Response.Write("<tr><td align=right bgcolor=#83CCF6>Branch:</th><td> "+ branchName +"</td></tr>");
	Response.Write("<tr><td align=right  bgcolor=#83CCF6>Note:</th><td> "+ note +"</td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=button value='    close   ' "+ Session["button_style"] +"  onclick='window.close();'>");
	Response.Write("</table>");
	return true;
}

bool BindCustomer()
{
	string search = "";
	if(Request.Form["search"] != null)
		search = Request.Form["search"].ToString();
	
	Response.Write("<br>");
	Response.Write("<form name=frm >");
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=6 align=right>Customers&nbsp;&nbsp;&nbsp;");
	Response.Write("Dealers&nbsp;&nbsp;&nbsp;");
	Response.Write("Suppliers&nbsp;&nbsp;&nbsp;");
	Response.Write("Employees&nbsp;&nbsp;&nbsp;");
	Response.Write("Others&nbsp;&nbsp;&nbsp;");
	Response.Write("All&nbsp;&nbsp;&nbsp;");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6><input type=text name=search value='"+search+"'><input type=submit name=cmd value='Search Customer'></td></tr>");
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr bgcolor=#B995A7><th>ID</th><th>Name</th><th>Trading Name</th><th>Email</th><th>Contact</th><th>Phone</th></tr>");
	bool bChange = true;
	for(int i=0; i<dst.Tables["card"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["card"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string company = dr["company"].ToString();
		string email = dr["email"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string contact = dr["contact"].ToString();
		string phone = dr["phone"].ToString();
		
		//string id = dr["id"].ToString();
		//string id = dr["id"].ToString();
		if(!bChange)
		{
			Response.Write("<tr bgcolor=#EDEDC7>");
			bChange = true;
		}
		else
		{
			Response.Write("<tr>");
			bChange = false;
		}
		Response.Write("<td>"+id+"</td>");
		Response.Write("<td>"+name+"</td>");
		Response.Write("<td>"+trading_name+"</td>");
		Response.Write("<td>"+email+"</td>");
		Response.Write("<td>"+contact+"</td>");
		Response.Write("<td>"+phone+"</td>");
		
		Response.Write("</tr>");
	}	
	
	Response.Write("</table></form>");
		
	return true;
}

</script>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"><html>
<head>
    <title>--- Customer Details ---</title> 
</head>

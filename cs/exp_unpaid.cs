<!-- #include file="page_index.cs" -->

<script runat=server>

DataSet dst = new DataSet();
DataTable dtExpense = new DataTable();

string m_id = "";
bool m_bRecorded = false;
bool m_bIsPaid = false;

string m_branch = "";
string m_fromAccount = "";
string m_toAccount = "";
string m_customerID = "-1";
string m_customerName = "";
string m_paymentType = "1";
string m_paymentDate = "";
string m_paymentRef = "";
string m_note = "";
string m_editRow = "";
string m_nextChequeNumber = "";
string m_sAutoFrequency = "0";
string m_sNextAutoDate = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	PrintAdminHeader();
	PrintAdminMenu();

	QueryUnpiadExpense();
	//PrintBody();

	PrintAdminFooter();
}



bool QueryUnpiadExpense()
{
	string sid = "";
	if(Request.QueryString["sid"] != null && Request.QueryString["sid"] != "")
		sid = Request.QueryString["sid"];

	string sc = " SELECT e.*, c.name, c.company, en.name AS payment_type1 FROM expense e ";
	sc += " JOIN card c ON c.id = e.card_id ";
	sc += " JOIN enum en ON en.id = e.payment_type AND en.class='payment_method' ";
	sc += " WHERE e.ispaid = 0 ";
	if(sid != "" && sid != null)
		if(TSIsDigit(sid))
			sc += " AND e.card_id = "+ sid;
	sc += " AND e.id NOT IN (SELECT id FROM auto_expense WHERE id = e.id ) ";
	sc += " AND e.total > 0 ";
	sc += " ORDER BY e.payment_date ";
//DEBUG(" sc = ", sc);
int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "unpaid_exp");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
	{
		Response.Write("<center><font color=red><h4>NO UNPAID EXPENSE</font></h4></center>");
		Response.Write("<center><input type=button value='<< Back' "+ Session["button_style"] +" onclick=\"window.history.back()\"></center><br>");
		Response.Write("<center><input type=button value='<< Main Page' "+ Session["button_style"] +" onclick=window.location=('')>");
		return false;
	}

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
//	rows = dst.Tables["unpaid_exp"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	
	string uri = Request.ServerVariables["URL"] +"?sid=";
	if(Request.QueryString["p"] != null)
		uri += "p="+Request.QueryString["p"];
	if(Request.QueryString["spb"] != null)
		uri += "spb="+Request.QueryString["spb"];
	
	Response.Write("<br><center><h4>Unpaid Expense List</h4></center>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table width=90%  align=center valign=center cellspacing=0 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr align=left><td colspan=3>"+ sPageIndex +"</td>");
	Response.Write("<td colspan=3> Select Payee: ");
	Response.Write(PrintSupplierOptions(sid, uri, "others"));
	Response.Write("</td></tr>");
	Response.Write("<tr align=left style=\"color:black;background-color:lightblue;font-weight:bold;\">\r\n");
	Response.Write("<th>PAYMENT DATE</th>");
	Response.Write("<th>PAYEE</th>\r\n");
	Response.Write("<th>PAYMENT TYPE</th>\r\n");
	Response.Write("<th>REFERENCE</th>\r\n");
	Response.Write("<th>AMOUNT</th>\r\n");
	Response.Write("<th>EDIT</td>\r\n");
	Response.Write("</tr>\r\n");
	bool bAlter = false;
	for(; i<rows && i<end; i++)
	{
		
		DataRow dr = dst.Tables["unpaid_exp"].Rows[i];
		string id = dr["id"].ToString();
		string payee = dr["company"].ToString();
		if(payee == "")
			payee = dr["name"].ToString();
		string payment_type = dr["payment_type1"].ToString();
		string payment_date = dr["payment_date"].ToString();
		string payment_ref = dr["payment_ref"].ToString();
		string total = dr["total"].ToString();
		string ispaid = dr["ispaid"].ToString();
		
		Response.Write("<tr ");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEE ");
		bAlter = !bAlter;
		Response.Write(">");
		Response.Write("<td>"+ DateTime.Parse(payment_date).ToString("dd-MM-yyyy") +"</td>");
		Response.Write("<td>"+ payee +"</td>");
		Response.Write("<td>"+ payment_type +"</td>");
		Response.Write("<td>"+ payment_ref +"</td>");
		Response.Write("<td>"+ total +"</td>");
		Response.Write("<td><a title='Edit Expense' href='expense.aspx?id="+ id +"' class=o>Edit</a></td>");
		
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</form><br><br><br><br><br>");
	return true;

}

</script>

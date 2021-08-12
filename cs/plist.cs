<!-- #include file="page_index.cs" -->

<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_type = "all";
string m_nType = "";
string m_status = "";
string m_payment_status = "";
string m_supplierID = "";
string m_kw = "";
string tableWidth = "97%";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;
	
	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
	{
		m_nType = Request.QueryString["t"];
		m_type = GetEnumValue("receipt_type", m_nType);
	}
	if(Request.QueryString["s"] != null)
		m_status = Request.QueryString["s"];
	if(Request.QueryString["ps"] != null)
		m_payment_status = Request.QueryString["ps"];
	if(Request.QueryString["si"] != null)
		m_supplierID = Request.QueryString["si"];

	if(Request.QueryString["kw"] != null && Request.QueryString["kw"] != "")
	{
		m_kw = Request.QueryString["kw"];
		Session["purchase_list_search_kw"] = m_kw;
	}

	WriteHeaders();

	if(!DoSearch())//m_itype))
		return;

	if(!IsPostBack)
	{
		BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
}

Boolean DoSearch()//int m_itype)
{
	string sword = " WHERE ";
	int rows = 0;
	string sc = "SELECT p.id, p.type, p.po_number, ISNULL(c.company, 'Other') AS short_name, p.date_create ";
	sc += ", p.inv_number, p.status, p.total_amount, p.payment_status ";
	sc += " FROM purchase p LEFT OUTER JOIN card c ON p.supplier_id=c.id ";
	//
	//sc += " JOIN  purchase_item pi ON pi.id = p.id ";
	//sc += " JOIN dispatch d ON d.id = pi.kid";
	
	if(m_kw != "")
		sc += " WHERE (p.po_number LIKE '%" + m_kw + "%' OR p.inv_number LIKE '%" + m_kw + "%') ";
	else
	{
		if(m_type != "all")
		{	
		//	int temp_type = int.Parse(Request.QueryString["t"]);
			sc += "WHERE p.type=" + Request.QueryString["t"];
			sword = " AND ";
			if(m_supplierID != "")
				sc += " AND p.supplier_id=" + m_supplierID;
		}
		else if(m_supplierID != "")
		{
			sc += " WHERE p.supplier_id=" + m_supplierID;
			sword = " AND ";
		}
		if(m_status != "")
		{
			sc += sword + " status=" + m_status;
			sword = " AND ";
		}
		if(m_payment_status != "")
			sc += sword + " p.payment_status=" + m_payment_status + " AND p.status=" + GetEnumID("purchase_order_status", "received"); 
	}
	sc += " ORDER BY p.date_create DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "pr");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void WriteHeaders()
{
	string stype = "All";
	if(m_type != "all")
		stype = m_type;

	string url = "plist.aspx?r=" + DateTime.Now.ToOADate();
	if(m_nType != "")
		url += "&t=" + m_nType;
	url += "&s=" + m_status + "&si=";

	string status_received = GetEnumID("purchase_order_status", "received");
	string status_backorder = GetEnumID("purchase_order_status", "on backorder");
	string status_deleted = GetEnumID("purchase_order_status", "deleted");

	Response.Write("<form name=f action=plist.aspx method=get>");
/*	Response.Write("<br><center><h3>PURCHASE LIST - <font color=red>" + stype.ToUpper() + "</font></h3>");
//	Response.Write("<br><img border=0 src='../../i/smt.gif'><br>");
	
	*/
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>PURCHASE LIST - <font color=red>" + stype.ToUpper() + "</font></font>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
	
	Response.Write("<table align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><br></td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<b>Supplier : </b>");
	Response.Write(PrintSupplierOptions(m_supplierID, url));
	Response.Write(" &nbsp&nbsp; <b>PO#/INV# Search : </b>");
	
	Response.Write("<input type=text size=10 name=kw autocomplete=off value=" + Session["purchase_list_search_kw"] + ">");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Search'>");

	Response.Write("</form>");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.f.kw.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("</td><td></td><tr><td align=right>");
	Response.Write("<img src=r.gif><a href=?si=" + m_supplierID + "&"+r+" >All</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif><a href=?t=2&s=1&si=" + m_supplierID + "&"+r+" >Orders</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif><a href=?t=2&s=5&si=" + m_supplierID + "&"+r+" >Dispatched</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif><a href=?t=2&ps=1&s=" + status_received + "&si=" + m_supplierID + "&"+r+" >Received</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif><a href=?t=2&s=" + status_backorder + "&si=" + m_supplierID + "&"+r+" BackOrder</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<a href=?s=" + status_deleted + "&si=" + m_supplierID + "&"+r+" >Deleted</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif><a href=?t=4&ps=1&si=" + m_supplierID + "&"+r+" >Open Bills</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("<img src=r.gif><a href=?t=4&ps=2&si=" + m_supplierID + "&"+r+" >Closed Bills</a>&nbsp;&nbsp;&nbsp;");
//	Response.Write("<img src=r.gif><a href=?t=4&s=2&si=" + m_supplierID + "&"+r+" class=d>Closed Bills</a>&nbsp;&nbsp;&nbsp;");
//	Response.Write("<img src=r.gif><a href=?t=1&s=3&"+r+" class=d>deleted Quotes</a>&nbsp;&nbsp;&nbsp;");
//	Response.Write("<img src=r.gif><a href=?t=2&s=3&"+r+" class=d>deleted Orders</a>&nbsp;&nbsp;&nbsp;");
	Response.Write("</td></tr></table>");
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["pr"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 30;
	m_cPI.URI = "?t=" + m_nType + "&s" + m_status + "&si=" + m_supplierID ;
	if(m_payment_status != "")
		m_cPI.URI += "&ps=" + m_payment_status;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width='"+ tableWidth +"' align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th align=left>Date</td>\r\n");
	Response.Write("<th align=left>Supplier</td>\r\n");
	Response.Write("<th>P.O.Number</td>\r\n");
	Response.Write("<th>Supplier InvNo.</td>\r\n");
	Response.Write("<th>Total Amount</td>");
	Response.Write("<th>Status</td>");
	Response.Write("<th>&nbsp;</td>");
	Response.Write("</tr>\r\n");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}
string uri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToString("ddMMyyyyHHmmss");
if(Request.QueryString["si"] != null && Request.QueryString["si"] != "")
uri += "&si="+ Request.QueryString["si"];
if(Request.QueryString["s"] != null && Request.QueryString["s"] != "")
uri += "&s="+ Request.QueryString["s"];
if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
uri += "&t="+ Request.QueryString["t"];
if(Request.QueryString["p"] != null)
uri += "&p="+ Request.QueryString["p"];
if(Request.QueryString["spb"] != null)
uri += "&spb="+ Request.QueryString["spb"];
if(Request.QueryString["pays"] != null)
uri += "&pays="+ Request.QueryString["pays"];
	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["pr"].Rows[i];
		string date = DateTime.Parse(dr["date_create"].ToString()).ToString("dd/MM/yyyy");
		string id = dr["id"].ToString();
		string type = dr["type"].ToString();
		string po_number = dr["po_number"].ToString();
		string inv_number = dr["inv_number"].ToString();
		string supplier = dr["short_name"].ToString();
		string amount = dr["total_amount"].ToString();
		double dAmount = 0;
		if(amount != "")
			dAmount = double.Parse(amount);
		string status = GetEnumValue("purchase_order_status", dr["status"].ToString());
		if(type == "4") //"bill")
			status = "billed";
		string payment_status = GetEnumValue("general_status", dr["payment_status"].ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + supplier + "</td>");
//		Response.Write("<td>" + po_number + "</td>");
		Response.Write("<td align=center>" + po_number + " ");		
		Response.Write(" <a title='view purchase items details' href='"+ uri +"");
		if(Request.QueryString["pid"] != id)
		Response.Write("&pid="+ id +"");
		Response.Write("' class=o>V</a></b>");
		Response.Write("</td>");				//27.06.2003 NEO
//		Response.Write("<td>" + inv_number + "</td>");
		Response.Write("<td align=center>" + inv_number + "</td>");							//27.06.2003 NEO
		Response.Write("<td align=right>" + dAmount.ToString("c") + "</td>");					//27.06.2003 NEO
//		Response.Write("<td>" + dAmount.ToString("c") + "</td>");
		Response.Write("<td align=center>" + status + "</td>");
		Response.Write("<td>");
	    if(status == "order placed" || status == "on backorder" || status =="dispatched" || status =="receiving")
	    {
			Response.Write("<a href=# onclick=\"window.open('purchase.aspx?n=" + id + "','s','width=500,height=500,resizable,scrollbars')\" class=o title='Supplier invoice received, update price'>Receive Invoice</a> ");
			Response.Write("&nbsp;| &nbsp;<a href=dlist.aspx?pid=" + po_number +" class=o title='Receive Item'>Receive Stock</a> ");
		}
		else if(status == "deleted")
			Response.Write("<a href=purchase.aspx?n=" + id + " title='Undo Delete' style=\"color=red;\">Recover Order</a> ");
		else if(status == "received" || status == "billed")
		{
//			if(payment_status == "closed")// || type == GetEnumID("receipt_type", "bill"))
			if(type == "4")
				Response.Write("<a href=# onclick=\"window.open('purchase.aspx?t=pp&n=" + id + "','new','width=500,height=500, resizable,scrollbars')\" class=o title='View Order'>View</a> ");
			else
				Response.Write("<a href=# onclick=\"window.open('purchase.aspx?n=" + id + "','s','width=500,height=500,resizable,scrollbars')\" class=o title='Supplier invoice received, update price'>Receive Invoice</a> ");
		}
		if(status != "deleted")
	    {
			Response.Write("<a href=# onclick=\"window.open('serial.aspx?n=" + id + "','serial','width=500,height=500,resizable,scrollbars')\" class=o title='Enter Serial Number'>SN# Input</a>");
			if(Session["branch_support"] != null)
		    Response.Write("<a href=# onclick=\"window.open('purchase.aspx?n="+ id +"&da=1','d','width=500,height=500,resizable,scrollbars')\" class=o title='Dispatch Information'> Dispatch Detail</a>");
	    }
		
		//Response.Write(" &nbsp;| &nbsp;<a href=# onclick=\"window.open('pur_note.aspx?ri="+id+"&i="+po_number+"','note','width=500,height=500,resizable,scrollbars')\" class=o title='Edit Purchase Note'> Note</a>");
		Response.Write(" &nbsp;| &nbsp;<a href=pur_note.aspx?ri="+id+"&i="+po_number+" class=o title='Edit Purchase Note'> Note</a>");
		Response.Write("</td></tr>");
		if(Request.QueryString["pid"] != null && Request.QueryString["pid"] != "" )
		{
			if(Request.QueryString["pid"] == id)
			{
			Response.Write("<tr><td colspan=8>");
			DisplayPurchaseItem(Request.QueryString["pid"].ToString(), bAlterColor);
			Response.Write("</td></tr>");
			}
		}
	}
	
	Response.Write("<tr><td colspan=4>");
	Response.Write(sPageIndex);
	Response.Write("</td></tr>");
	Response.Write("</table>");
}

bool DisplayPurchaseItem(string pid, bool bAlter)
{
	string sc = "SELECT * FROM purchase_item WHERE id = "+ pid;
	int rows =0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "pItem");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
		return false;

	Response.Write("<table width=80%  align=center valign=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#E3E3E3 align=left><th>CODE</th><th>MP_CODE</th><th>PROD_DESC</th><th>QTY</th><th>COST</th>");
	Response.Write("</tr>");
//	bool bAlter = false;
	double dTotalCost = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["pItem"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		name = StripHTMLtags(name);
		string qty = dr["qty"].ToString();
		string price = dr["price"].ToString();
		if(price == null && price == "")
			price = "0";
		dTotalCost += double.Parse(price);

		Response.Write("<tr ");
		if(bAlter)
			Response.Write(" bgcolor=#E9E9E9 ");
		Response.Write(">");
		bAlter = !bAlter;
		Response.Write("<td>"+ code +"</td>");
		Response.Write("<td>"+ supplier_code +"</td>");
		Response.Write("<td>"+ name +"</td>");
		Response.Write("<td>"+ qty +"</td>");
		Response.Write("<td>"+ double.Parse(price).ToString("c") +"</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr align=right><th colspan=4>Total Purchase: &nbsp;&nbsp;</td><th align=left>"+ dTotalCost.ToString("c") +"</td></tr>");
	Response.Write("</table>");
	return true;
	
}

</script>

<asp:Label id=LFooter runat=server/>
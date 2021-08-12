<!-- #include file="card_function.cs" -->
<!-- #include file="page_index.cs" -->
<script runat="server">

int m_nPage = 1;
int m_nPageSize = 20;
int m_cols = 6;
int m_nStartPageButton = 1;
int m_nPageButtonCount = 9;

DataSet ds = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["id"] != null) //view perticular order
	{
		if(!GetOneOrder())
			return;
	}
	if(GetOrders())
		BindGrid();
	PrintAdminFooter();
}

Boolean GetOrders()
{
	string sc = "SELECT o.*, c.short_name AS supplier, d.name AS sales_name ";
	sc += " FROM purchase_order o LEFT OUTER JOIN card c ON c.id=o.supplier_id LEFT OUTER JOIN card d ON d.id=o.staff_id ";
	if(Request.QueryString["t"] != null)
		sc += " WHERE o.status=" + Request.QueryString["t"];
	sc += " ORDER BY o.id DESC";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "order");
		return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void BindGrid()
{
	Response.Write("<br><center><h3>Purchase Order List - <font color=red>");
	if(Request.QueryString["t"] != null)
		Response.Write(GetEnumValue("order_item_status", Request.QueryString["t"]));
	else
		Response.Write("ALL");
	Response.Write("</font></h3>");
	Response.Write("<table width=100%>");
	
	Response.Write("<tr><td align=right>");
	Response.Write("<img src=r.gif> <a href=polist.aspx?t=1&r=" + DateTime.Now.ToOADate() + " class=o>Being Processed</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=polist.aspx?t=2&r=" + DateTime.Now.ToOADate() + " class=o>Invoiced</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=polist.aspx?t=3&r=" + DateTime.Now.ToOADate() + " class=o>Shipped</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=polist.aspx?t=4&r=" + DateTime.Now.ToOADate() + " class=o>Back Ordered</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=polist.aspx?t=5&r=" + DateTime.Now.ToOADate() + " class=o>OnHode</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=polist.aspx?t=6&r=" + DateTime.Now.ToOADate() + " class=o>Returns</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=polist.aspx?r=" + DateTime.Now.ToOADate() + " class=o>All</a> ");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<table align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Supplier</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Sales</th>");
	Response.Write("<th>Bill#</th>");
	Response.Write("<th>Status</th>");
	Response.Write("<th>Order#</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["order"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["order"].Rows[i];
		string id = dr["id"].ToString();
		string order_number = dr["number"].ToString();
		string sales = dr["sales_name"].ToString();
		string bill_number = dr["bill_number"].ToString();
		string part = dr["part"].ToString();
		string supplier = dr["supplier"].ToString();
		string date = DateTime.Parse(dr["date_created"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		if(status == "Received")
			status += " " + DateTime.Parse(dr["date_received"].ToString()).ToString("dd-MM-yyyy HH:mm");
		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;

		Response.Write(">");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + sales + "</td>");
		Response.Write("<td><a href=invoice.aspx?" + bill_number + "&r=" + DateTime.Now.ToOADate());
		Response.Write(" class=o target=_blank title='click to view invoice'>" + bill_number + "</a></td>");
		Response.Write("<td>" + status + "</td>");
		Response.Write("<td><a href=polist.aspx?id=" + id);
		if(Request.QueryString["t"] != null)
			Response.Write("&t=" + Request.QueryString["t"]);
		Response.Write(" class=o>" + order_number);
		if(part != "0")
			Response.Write("." + part);
		Response.Write("</a></td>");
		Response.Write("<td>");
		if(status == "Billed")
			Response.Write("<a href=esales.aspx?i=" + bill_number + "&r=" + DateTime.Now.ToOADate() + " class=o>Process</a>");
		else
			Response.Write("<a href=eporder.aspx?id=" + id + "&part=" + part + "&r=" + DateTime.Now.ToOADate() + " class=o>Process</a>");
		Response.Write("</td>");
		Response.Write("</tr>");

		if(Request.QueryString["id"] != null && id == Request.QueryString["id"])
		{
			Response.Write("<tr><td colspan=6 align=center>");
			PrintOneOrderStatus();
			Response.Write("<br>&nbsp;</td></tr>");
		}
	}
	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
//	Response.Write(PrintPageIndex());
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
}

bool GetOneOrder()
{
	string sc = "SELECT o.id, o.status, o.number, o.bill_number, o.date_received, ";
	sc += " i.* FROM purchase_order o JOIN purchase_order_item i ON i.id=o.id WHERE o.id=" + Request.QueryString["id"];
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "one");
		return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintOneOrderStatus()
{
//	Response.Write("<br><center><h3>Order Information</h3>");
	Response.Write("<table width=90% valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<th>Code</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th>Quantity</th>");
	Response.Write("<th>Status</th>");
	Response.Write("</tr>");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["one"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
//	for(; i < rows && i < end; i++)
	for(; i < rows; i++)
	{
		DataRow dr = ds.Tables["one"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string quantity = dr["qty"].ToString();
//		string  = dr[""].ToString();
//		string invoice_number = dr["invoice_number"].ToString();
//		string order_number = dr["number"].ToString();
//		string po_number = dr["po_number"].ToString();
//		string part = dr["part"].ToString();
//		string contact = dr["contact"].ToString();
//		string date = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		if(status == "Shipped")
			status += " " + DateTime.Parse(dr["date_shipped"].ToString()).ToString("dd-MM-yyyy HH:mm");
		
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;

		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td nowrap><a href=p.aspx?" + code + " target=_blank>" + name + "</a></td>");
		Response.Write("<td>" + quantity + "</td>");
		Response.Write("<td nowrap>" + status + "</td>");
		Response.Write("</tr>");
	}
//	Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
}
</script>

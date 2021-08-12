<!-- #include file="card_function.cs" -->
<!-- #include file="page_index.cs" -->
<script runat="server">

int m_cols = 6;

string m_custID = "";
string m_customerName = "";

string m_kw = ""; //search key word
DataSet ds = new DataSet();

bool m_bSystem = false;

void SPage_Load() 
{
	if(Session["card_id"] == null)
		Response.Redirect("login.aspx");

	if(Request.QueryString["system"] == "1")
		m_bSystem = true;

	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
	{
		if(IsInteger(Request.QueryString["ci"]))
			m_custID = Request.QueryString["ci"];
	}
	if(m_sSite != "admin")
		m_custID = Session["card_id"].ToString();

	if(Request.QueryString["kw"] != null && Request.QueryString["kw"] != "")
	{
		m_kw = Request.QueryString["kw"];
		Session["status_order_search_kw"] = m_kw;
	}

	if(Request.QueryString["a"] == "u") //unlock
	{
		DoUnlock(Request.QueryString["id"]);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=status.aspx?t=");
		Response.Write(Request.QueryString["t"]);
		if(m_bSystem)
			Response.Write("&system=1");
		if(m_kw != "")
			Response.Write("&kw=" + HttpUtility.UrlEncode(m_kw));
		Response.Write("\">");
		return;
	}

	if(!TS_UserLoggedIn())
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=login.aspx\">");
		return;
	}
	
	if(Request.QueryString["id"] != null) //view perticular order
	{
		if(!GetOneOrder())
			return;
	}

	if(GetOrders())
		BindGrid();
}

Boolean GetOrders()
{
	if(m_custID == null || m_custID == "")
	{
		Response.Write("<br><br><center><h3>Error, no customer ID");
		return false;
	}

	string sc = "SELECT o.*, c.name, c.trading_name FROM orders o JOIN card c ON c.id=o.card_id WHERE o.card_id=" + m_custID;
	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		sc += " AND o.status=" + Request.QueryString["t"] + " AND o.type <> 1 ";
	if(m_bSystem)
		sc += " AND system = 1 ";
	if(m_kw != "")
		sc += " AND (o.po_number LIKE '%" + m_kw + "%' OR o.number LIKE '%" + m_kw + "%' OR o.invoice_number LIKE '%" + m_kw + "%') ";
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
	//if(m_sSite == "www")
    Response.Write("<br><center><h3>Order Trace");
	if(m_sSite == "admin")
	{
		if(ds.Tables["order"].Rows.Count > 0)
			m_customerName = ds.Tables["order"].Rows[0]["trading_name"].ToString();
		Response.Write(" - <font color=red>" + m_customerName + "</font>");
	}
	else
	{
		Response.Write(" - <font color=red>");
		if(m_bSystem)
			Response.Write("SysQuote");
		else if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
			Response.Write(GetEnumValue("order_item_status", Request.QueryString["t"]));
		else
			Response.Write("ALL");
	}

	Response.Write("</h3>");
  
	Response.Write("<table width=90%>");
	
	Response.Write("<tr><td>");
	Response.Write("<form action=status.aspx method=get>");
	Response.Write("<input type=text size=14 name=kw autocomplete=off value=" + Session["status_order_search_kw"] + ">");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='Search Order'>");
	Response.Write("</td>");
	Response.Write("</form>");
	Response.Write("</tr><tr>");
    if(m_sSite == "admin")
	    Response.Write("<td align=right>");
    else
        Response.Write("<td align=left><br>");
	if(g_bEnableQuotation)
	{
		Response.Write("<img src=r.gif> <a href=status.aspx?system=1"); 
		if(Request.QueryString["ci"] != null)
			Response.Write("&ci=" + Request.QueryString["ci"]);
		Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o>SysQuote</a>&nbsp&nbsp&nbsp&nbsp;");
	}
	Response.Write("<img src=r.gif> <a href=status.aspx?t=1"); if(Request.QueryString["ci"] != null)Response.Write("&ci=" + Request.QueryString["ci"]);Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o>Being Processed</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=status.aspx?t=2"); if(Request.QueryString["ci"] != null)Response.Write("&ci=" + Request.QueryString["ci"]);Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o>Invoiced</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=status.aspx?t=3"); if(Request.QueryString["ci"] != null)Response.Write("&ci=" + Request.QueryString["ci"]);Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o>Shipped</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=status.aspx?t=4"); if(Request.QueryString["ci"] != null)Response.Write("&ci=" + Request.QueryString["ci"]);Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o>Back Ordered</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=status.aspx?t=5"); if(Request.QueryString["ci"] != null)Response.Write("&ci=" + Request.QueryString["ci"]);Response.Write("&r=" + DateTime.Now.ToOADate() + " class=o>OnHold</a>&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<img src=r.gif> <a href=status.aspx?r=" + DateTime.Now.ToOADate()); if(Request.QueryString["ci"] != null)Response.Write("&ci=" + Request.QueryString["ci"]);Response.Write(" class=o>All</a> ");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2 >");
	Response.Write("<table ");
    if(m_sSite == "admin")
        Response.Write(" width=100% ");
    else
       Response.Write(" width=930 "); 
    Response.Write("valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Your Order#</th>");
	if(m_bSystem)
		Response.Write("<th>Your Customer</th>");
	Response.Write("<th>Date</th>");
	Response.Write("<th>Your Contact</th>");
	Response.Write("<th>Our Order#</th>");
	Response.Write("<th>Invoice#</th>");
	Response.Write("<th>Status</th>");
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
	if(Request.QueryString["ci"] != null)
		m_cPI.URI += "&ci=" + Request.QueryString["ci"];
	if(m_bSystem)
		m_cPI.URI += "&system=1";
	if(m_kw != "")
		m_cPI.URI += "&kw=" + HttpUtility.UrlEncode(m_kw);

	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
/*
	if(rows <= 0)
	{
		//Response.Write("</table>");
		//return;
	}

 */
	bool bAlterColor = false;
	string bgc = GetSiteSettings("table_row_bgcolor", "#EEEEEE");
	string sbgc = GetSiteSettings("sys_quote_bgcolor", "#AAFFFF");
	string sbgcl = GetSiteSettings("sys_quote_bgcolor_light", "#DDFFFF");
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["order"].Rows[i];
		string id = dr["id"].ToString();
		string type = dr["type"].ToString();
		string your_customer = dr["dealer_customer_name"].ToString();
		string invoice_number = dr["invoice_number"].ToString();
		string order_number = dr["number"].ToString();
		string po_number = dr["po_number"].ToString();
		string part = dr["part"].ToString();
		string contact = dr["contact"].ToString();
		string date = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		string locked_by = dr["locked_by"].ToString();
		string time_locked = dr["time_locked"].ToString();
		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		if(type == "1") //quote
			status = "Quote";
		bool bSystem = bool.Parse(dr["system"].ToString());
		string uedit = "corder.aspx?id=" + id;
		if(bSystem)
			uedit = "q.aspx?n=" + id;
		if(status == "Shipped")
			status += " " + DateTime.Parse(dr["date_shipped"].ToString()).ToString("dd-MM-yyyy HH:mm");
		else
		{
			if(locked_by != "")
			{
				status = "<font color=red><b>Locked by " + TSGetUserNameByID(locked_by) + " ";
				if(time_locked != "")
					status += DateTime.Parse(time_locked).ToString("MMM.dd HH:mm");
				status += "</b></font>";
				if(locked_by == Session["card_id"].ToString())
				{
					if(!g_bOrderOnlyVersion && !g_bRetailVersion)
						status += " <a href=" + uedit + " class=o>EDIT ORDER</a>";
					status += " <a href=status.aspx?a=u&id=" + id + "&t=" + Request.QueryString["t"];
					if(m_bSystem)
						status += "&system=1";
					if(m_kw != "")
						status += "&kw=" + HttpUtility.UrlEncode(m_kw);
					status += " class=o title=Unlock>Unlock</a> ";
				}
			}
			else if(status.ToLower() == "being processed")// || status.ToLower() == "on hold")
			{
				if(!g_bOrderOnlyVersion && !g_bRetailVersion)
					status = "Being Processed &nbsp&nbsp&nbsp&nbsp; <a href=" + uedit + " class=o>EDIT ORDER</a>";
			}
			else if(status == "Quote")
			{
				status = "Quote ";
				if(!g_bOrderOnlyVersion && !g_bRetailVersion)
					status += " <a href=" + uedit + " class=o>EDIT ORDER</a>";
			}
		}
		Response.Write("<tr");
		if(bSystem)
		{
			if(bAlterColor)
				Response.Write(" bgcolor=" + sbgc);
			else 
				Response.Write(" bgcolor=" + sbgcl);
		}
		else
		{
			if(bAlterColor)
				Response.Write(" bgcolor=" + bgc);
		}
		bAlterColor = !bAlterColor;

		Response.Write(">");
		Response.Write("<td>" + po_number + "</td>");
		if(m_bSystem)
			Response.Write("<td>" + your_customer + "</td>");
		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + contact + "</td>");

		Response.Write("<td><a href=status.aspx?");
		if(Request.QueryString["id"] == id)
			Response.Write("r=" + DateTime.Now.ToOADate());
		else
			Response.Write("id=" + id);
		if(Request.QueryString["t"] != null)
			Response.Write("&t=" + Request.QueryString["t"]);
		if(Request.QueryString["p"] != null)
			Response.Write("&p=" + Request.QueryString["p"]);
		if(Request.QueryString["spb"] != null)
			Response.Write("&spb=" + Request.QueryString["spb"]);
		if(Request.QueryString["ci"] != null)
			Response.Write("&ci=" + Request.QueryString["ci"]);
		if(m_kw != "")
			Response.Write("&kw=" + HttpUtility.UrlEncode(m_kw));
		if(m_bSystem)
			Response.Write("&system=1");
		Response.Write(" class=o>" + order_number);
		if(part != "0")
			Response.Write("." + part);
		Response.Write("</a></td>");

		Response.Write("<td><a href=invoice.aspx?" + invoice_number + "&r=" + DateTime.Now.ToOADate());
		Response.Write(" class=o title='click to view invoice'>" + invoice_number + "</a>");
		if(invoice_number != "")
		{
			Response.Write(" <a href=pack.aspx?i=" + invoice_number + "&r=" + DateTime.Now.ToOADate() + " class=o ");
			Response.Write(" title='Clilck to view Packing Slip' class=o>P</a>");
		}
		Response.Write("</td>");
		Response.Write("<td>" + status + "</td>");
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
	string id = Request.QueryString["id"];
	if(!CheckSQLAttack(id))
		return false;

	string sc = "SELECT o.number, o.part, o.contact, o.po_number, o.status, o.invoice_number, o.date_shipped, ";
	sc += " i.*, k.kit_id ";
	sc += " FROM orders o JOIN order_item i ON i.id=o.id ";
	sc += " LEFT OUTER JOIN order_kit k ON k.id = o.id ";
	sc += " WHERE o.card_id=" + m_custID;
	sc += " AND o.id=" + id;
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
/*
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
*/
	int rows = ds.Tables["one"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	string m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);
	bool bAlterColor = false;
	for(int i=0; i < rows; i++)
	{
		DataRow dr = ds.Tables["one"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["item_name"].ToString();
		string quantity = dr["quantity"].ToString();
		string status = GetEnumValue("order_item_status", dr["status"].ToString());
		if(status == "Shipped")
			status += " " + DateTime.Parse(dr["date_shipped"].ToString()).ToString("dd-MM-yyyy HH:mm");
		
		bool bKit = MyBooleanParse(dr["kit"].ToString());
		string kit_id = dr["kit_id"].ToString();
		
		Response.Write("<tr");
		if(bKit)
			Response.Write(" bgcolor=aliceblue");
		else if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;

		Response.Write(">");
		Response.Write("<td>" + code + "</td>");
		if(bKit)
			Response.Write("<td><a href=pk.aspx?" + code + " target=_blank>");
		else
			Response.Write("<td><a href=p.aspx?" + code + " target=_blank>");
		if(bKit)
			Response.Write("<font color=red><i>(" + m_sKitTerm + " #" + kit_id + ")</i></font> ");
		Response.Write(name + "</a></td>");
		Response.Write("<td>" + quantity + "</td>");
		Response.Write("<td>" + status + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

bool DoUnlock(string id)
{
	string sc = " UPDATE orders SET locked_by=null, time_locked=null ";
	sc += " WHERE id=" + id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
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
</script>

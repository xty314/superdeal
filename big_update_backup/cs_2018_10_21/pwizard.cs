<!-- #include file="purchase_function.cs" -->
<script runat=server>

DataSet dst = new DataSet();
string m_days = "30"; //purchase_wizard_days_of_data_to_analyze

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	m_days = GetSiteSettings("purchase_wizard_days_of_data_to_analyze", "30", true);

	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("pwizard.aspx" + par);
		return;
	}

	if(Request.QueryString["t"] == "p")
	{
		string code = Request.QueryString["c"];
		string qty = Request.QueryString["q"];
		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			return;
		string supplier = drp["supplier"].ToString();
		string supplier_code = drp["supplier_code"].ToString();
		string foreign_supplier_price = drp["foreign_supplier_price"].ToString();
		AddToCart(code, supplier, supplier_code, qty, foreign_supplier_price);
		Session["purchase_need_update" + m_ssid] = true; //for update order first
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?ssid=" + m_ssid + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<center><br>");
	Response.Write("<font size=+1>Purchase Wizard</font><br>");
	Response.Write("<b>Analyze sales and purchase in last " + m_days + " days, please wait...</b>");
	Response.Flush();

	if(!DoAnalyzeSalesAndStock())
		return;

	if(!DoAnalyzePurchase())
		return;

	PrintAdminFooter();
}

bool DoAnalyzeSalesAndStock()
{
	//string sc = " SELECT s.code, s.name, s.quantity AS sold, s.commit_price, c.cost, cr.low_stock ";
	string sc = " SELECT s.code, s.name, s.quantity AS sold, s.commit_price, cr.supplier_price as cost, cr.low_stock ";
	sc += ", q.qty AS stock ";
	//sc += " FROM invoice i JOIN sales_cost c ON c.invoice_number = i.invoice_number ";
	sc += " FROM invoice i  ";
	sc += " JOIN sales s ON s.invoice_number = i.invoice_number ";
	sc += " LEFT OUTER JOIN stock_qty q ON q.code = s.code ";
	sc += " JOIN code_relations cr ON cr.code = s.code";
	sc += " WHERE DATEDIFF(day, i.commit_date, GETDATE()) <= " + m_days;
	sc += " AND (SELECT sum(qty) FROM stock_qty WHERE code = s.code) < cr.low_stock";
	sc += " AND cr.low_stock <>'0'";
	sc += " ORDER BY s.code ";
   
	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "sales");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
	
	
}

bool DoAnalyzePurchase()
{
	Response.Write("<form name=f action=pwizard.aspx method=post>");
	Response.Write("<table align=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");	
	Response.Write("<th>Code</th>");
	Response.Write("<th>Name</th>");
	Response.Write("<th>Sold</th>");
	Response.Write("<th>Current Stock(Total)</th>");
	Response.Write("<th>Lower Stock Warning</th>");
	Response.Write("<th>Last Order</th>");
	Response.Write("<th>Ordered Branch</th>");
	Response.Write("<th>LastPurchaseQty</th>");
	Response.Write("<th>QTY</th>");
	Response.Write("<th>Purchase</th>");
	Response.Write("</tr>");

	bool bAlterColor = false;
	string code_old = "";
	int rows = dst.Tables["sales"].Rows.Count;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["sales"].Rows[i];
		
		
		string code = dr["code"].ToString();
	
		if(code == code_old)
			continue;

		code_old = code;
		string name = dr["name"].ToString();
		string wLowStock = dr["low_stock"].ToString();
		double dQty = MyDoubleParse(dr["sold"].ToString());
		double dStock = MyDoubleParse(dr["stock"].ToString());
		double dOnOrder = GetOnOrderQty(code);
	
			   
		double dAStock = dStock + dOnOrder;
		//if(dAStock > dQty / 4)
		//	 continue;
			 
		if(!GetPurchaseHistory(code))
			return false;

		string pdate = "";
		string pqty = "";
		string BranchName = "";
		if(dst.Tables["purchase"].Rows.Count > 0)
		{
			dr = dst.Tables["purchase"].Rows[0];
			pdate = DateTime.Parse(dr["date_create"].ToString()).ToString("dd-MM-yyyy");
			pqty = dr["qty"].ToString();
			BranchName = dr["name"].ToString();
		}
		
		if(!GetCurrentQty(code))
			return false;
			
		string CurrentStock = "";
        if(dst.Tables["CurrentQty"].Rows.Count >0)
		{ 
		    dr = dst.Tables["CurrentQty"].Rows[0];
			CurrentStock = dr["CurrentQty"].ToString();
		}
		
		if(!GetTotalSoldQty(code))
			return false;
			
		string TotalSoldQty = "";
        if(dst.Tables["TotalSoldQty"].Rows.Count >0)
		{ 
		    dr = dst.Tables["TotalSoldQty"].Rows[0];
			TotalSoldQty = dr["TotalSoldQty"].ToString();
		}
		string dp = pqty;
		double warningStock = MyIntParse(wLowStock.ToString());
		if(dp == "")
		      dp =(warningStock - dAStock).ToString();
			//dp = (dQty - dAStock).ToString();
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td><a href=lowstock.aspx?lowstockcode="+code+"&g=1 title=\"Check Branch Stock\">" + code + "</a><input type=hidden name=code" + i + " value=" + code + "></td>");
		Response.Write("<td>" + name + "</td>");
		//Response.Write("<td>" + dQty.ToString() + TotalSoldQty+"</td>");
		Response.Write("<td>" + TotalSoldQty+"</td>");
	    //Response.Write("<td>" + dAStock.ToString() + "</td>");
		Response.Write("<td>" + CurrentStock + "</td>");
		Response.Write("<td style=\"color:red\">" + wLowStock +"</td>");
		Response.Write("<td>" + pdate + "</td>");
		
		Response.Write("<td>" + BranchName +"</td>");
		
		Response.Write("<td align=center>" + pqty + "</td>");
		Response.Write("<td><input type=text size=1 name=qty" + i + " value=" + dp + " style=text-align:right></td>");
		Response.Write("<td align=center><input type=checkbox disabled=true name=check" + i + ">");
		Response.Write("<input type=button value=Purchase onclick=\"document.f.check" + i + ".checked=1;");
		Response.Write("window.open('pwizard.aspx?t=p&c=" + code + "&q=' + document.f.qty" + i + ".value + '&ssid=" + m_ssid + "');");
		Response.Write("\" class=b>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}

//	Response.Write("<tr><td colspan=8 align=right><input type=submit name=cmd value='Create Purchase Order' class=b></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool GetPurchaseHistory(string code)
{
	if(dst.Tables["purchase"] != null)
		dst.Tables["purchase"].Clear();

	string sc = " SELECT p.date_create, i.qty, b.name ";
	sc += " FROM purchase_item i JOIN purchase p ON i.id = p.id JOIN branch b ON b.id = p.branch_id ";
	sc += " WHERE i.code = " + code;
	//sc += " AND DATEDIFF(day, p.date_create, GETDATE()) >" + m_days;
	sc += " ORDER BY p.date_create DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "purchase");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

double GetOnOrderQty(string code)
{
	Trim(ref code);
	if(code == "")
		return 0;

	DataSet dsoo = new DataSet();
	string sc = " SELECT SUM(i.qty) AS qty ";
	sc += " FROM purchase_item i JOIN purchase p ON i.id = p.id ";
	sc += " WHERE i.code = " + code;
	sc += " AND p.status IN(1, 3) "; //ordered or backordered (not received yet)
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsoo);
		return MyDoubleParse(dsoo.Tables[0].Rows[0]["qty"].ToString());
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return 0;
}

bool GetCurrentQty(string code)
{
	if(dst.Tables["CurrentQty"] != null)
		dst.Tables["CurrentQty"].Clear();

	string sc = " SELECT SUM(qty)AS CurrentQty FROM stock_qty WHERE code =" + code ;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "CurrentQty");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetTotalSoldQty(string code)
{
	if(dst.Tables["TotalSoldQty"] != null)
		dst.Tables["TotalSoldQty"].Clear();

	string sc = " SELECT SUM(s.quantity)AS TotalSoldQty FROM sales s  JOIN invoice i ON s.invoice_number = i.invoice_number WHERE s.code="+code;
	       sc += " AND  DATEDIFF(day, i.commit_date, GETDATE()) <= " + m_days;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "TotalSoldQty");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}


</script>

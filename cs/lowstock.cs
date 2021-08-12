<!-- #include file="page_index.cs" -->
<!-- #include file="purchase_function.cs" -->
<script runat=server>

string m_branchID = "";
string m_type = "";
string m_tableTitle = "Low Stock Item List";
string[] m_aBranchID = new string[16];
string[] m_aBranchName = new string[16];
int m_nBranches = 0;
string m_sSupplier = "";
string tableWidth = "97%";
string last_purchase = "";
string purchase_price = "";
DateTime P_Create_date;


DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
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
		Response.Redirect("lowstock.aspx" + par);
		return;
	}

	 
	if(Request.QueryString["sup"] != null)
	{
		m_sSupplier = Request.QueryString["sup"];
	}
	
	if(Request.QueryString["t"] == "p")
	{
	 
		if(DoPurchaseProcess())
		{
			
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?ssid=" + m_ssid + "\">");
		}
	
		return;
	}
	
	if(!GetData())
		return;
	//if(!GetAllBranchNames())
	//	return;
	PrintList();
}

bool DoPurchaseProcess()
{
	//DEBUG("ROWS", p("rows"));
	int nRows = int.Parse(p("rows"));
//5	DEBUG("ROWS ", nRows.ToString());
	for(int i = 1; i <= nRows ; i++)
	{
		string code = p("code"+i);
		string newLowStock = p("low_stock"+i);
		string oldLowStock = p("low_stock_old"+i);
//		DEBUG("i ", i.ToString());
//	DEBUG("code ", code);
//	return false;
		string qty = p("qty"+i);
		if(qty == "0")
			continue;
		//DEBUG("QTY ", qty);
		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			return false;
		string supplier = drp["supplier"].ToString();
		string supplier_code = drp["supplier_code"].ToString();
		string foreign_supplier_price = drp["foreign_supplier_price"].ToString();
		AddToCart(code, supplier, supplier_code, qty, foreign_supplier_price);
		Session["purchase_need_update" + m_ssid] = true; //for update order first
		if(newLowStock != oldLowStock)
			doUpdateWarningStock(code, newLowStock);
	}
	return true;
}

bool GetData()
{
	if(ds.Tables["lowstock"] != null)
		ds.Tables["lowstock"].Clear();
	string sc="";
	if(m_sSupplier != "")
	{
		sc = " SELECT c.supplier, c.name, c.name_cn, c.code, c.low_stock, c.supplier_code, s.qty ";
		sc += " FROM code_relations c JOIN stock_qty s ON c.code = s.code ";
		sc += " WHERE 1=1 ";
		sc += " AND c.low_stock != 0 ";
		sc += " AND c.low_stock > s.qty "; 
		//sc += " AND s.qty >0 ";
		sc += " AND s.branch_id =1";

		sc += "AND c.supplier = '"+m_sSupplier+"'";
		sc += " ORDER BY c.supplier";
	}

	else
	{
		sc = " SELECT top 100 c.supplier, c.name, c.name_cn, c.code, c.low_stock, c.supplier_code, s.qty ";
		sc += " FROM code_relations c JOIN stock_qty s ON c.code = s.code ";
		sc += " WHERE 1=1 ";
		sc += " AND c.low_stock != 0 ";
		sc += " AND c.low_stock > s.qty "; 
		//sc += " AND s.qty >0 ";
		sc += " AND s.branch_id =1";
		sc += " ORDER BY c.supplier";
	}
//DEBUG("sc=",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "lowstock");
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		return false;
	}
	return true;
}

/*
bool GetAllBranchNames()
{
	string sc = " SELECT * FROM branch WHERE activated = 1 ";
	if(Session["branch_support"] == null)
		sc += " AND id = 1 ";

	//sc += " ORDER BY id ";
//DEBUG(" sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nBranches = myAdapter.Fill(ds, "branches");
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<m_nBranches && i<16; i++)
	{
		DataRow dr = ds.Tables["branches"].Rows[i];
		string bid = dr["id"].ToString();
		string bname = dr["name"].ToString();
		m_aBranchID[i] = bid;
		m_aBranchName[i] = bname;
	}
	return true;
}
*/
/////////////////////////////////////////////////////////////////
/*
void PrintList()
{
	PrintAdminHeader();
	PrintAdminMenu();
	string supplier_old = "";
	
	Response.Write("<form name=frm action=?t=p&ssid="+m_ssid+" method=post>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>"+ m_tableTitle +"</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>Supplier:");
	DoSupplier(m_sSupplier);
	Response.Write("</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#0080db;font-weight:bold;\">");
	//Response.Write("<th>Supplier</td>");
	Response.Write("<th>Barcode</th>");
	Response.Write("<th>Description</th>");
	
	if(Session["branch_support"] != null)
	{
	for(int i=0; i<m_nBranches; i++)
		Response.Write("<th nowrap>" + m_aBranchName[i] + "</th>");
	}
	else
	{
		Response.Write("<td></td>");
	}
	
	Response.Write("<th>Total</th>");
	Response.Write("<th nowrap>Warning Value</th>");
	Response.Write("<th>Order Total</th>");
	Response.Write("</tr>");
	int rows = ds.Tables["report"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}
    
	string code = "";
	string code_old = "";
	string barcode = "";
	string supplier = "";
	string name = "";
	//string low_stock = "";
	int low_stock = 0;
	int w_stock = 0;
	int nTotalQty = 0;
	int[] nQty = new int[16];
	bool bAlterColor = false;
	int iRows = 0;
	
	
	for(int i=0; i<=rows; i++)
	{
		DataRow dr = null;
		string code_curr = "";

		if(i < rows)
		{
			dr = ds.Tables["report"].Rows[i];
			code_curr = dr["code"].ToString();
			
		}
		if(code_curr != code)
		{
			if(code != "") //begin print one row
			{
				//DEBUG("SUPPLIER ", supplier + " supplier_old = "+ supplier_old);				
				if(supplier != supplier_old)
				{
					Response.Write("<tr><td colspan=6 bgcolor=black style=\"color:white\" ><b>"+DoGetSupplierFullName(supplier)+"<b></td></tr>");
					supplier_old = supplier;
				}

				iRows++;
				string purchase_color = "bgcolor=";
				switch(doAlterColor(code, double.Parse(low_stock.ToString())))
				{
					case "0":
						purchase_color += "#FFFFFF";
						break;
					case "e":
						purchase_color += "#FF6600";
						break;
					case "1":
						purchase_color += "#99CC00";
						break;
					case "2":
						purchase_color += "#FF0000";
						break;
					default:
						break;
				}
				
			
				Response.Write("<input type=hidden name=code"+iRows.ToString()+" value='"+code+"'>");
				Response.Write("<tr ");
				//if(bAlterColor)
				//	Response.Write(" bgcolor=#EEEEEE");
				//bAlterColor = !bAlterColor;
				Response.Write(purchase_color);
				Response.Write(" >");
				//Response.Write("<td>"+ supplier+"</td>");
				Response.Write("<td>");
				if(SecurityCheck("manager"))
					Response.Write("<a href=liveedit.aspx?code=" + code + " target=_blank>");
				Response.Write(barcode );
				if(SecurityCheck("manager"))
					Response.Write("</a>");	
				Response.Write("</td>");
				Response.Write("<td>" + name + "</td>");
				if(Session["branch_support"] != null)
				{
				for(int n=0; n<m_nBranches; n++)
				{				
					int qty = nQty[MyIntParse(m_aBranchID[n])];
					Response.Write("<td align=center>");
					if(qty != 0)
						Response.Write(qty);

					else
						Response.Write("0");
					Response.Write("</td>");
					nTotalQty += qty;

				}
				}
				else
				{
				 	for(int n=0; n<m_nBranches; n++)
				   {	
				     int qty = nQty[MyIntParse(m_aBranchID[n])];
					 nTotalQty += qty;
				    }
				}
				Response.Write("<td align=center>" + nTotalQty + "</td>");
				Response.Write("<td align=center>" + low_stock);
				//int p_stock = (w_stock - nTotalQty);
				//Response.Write("&nbsp;<input type=button onclick =\"window.open('lowstock.aspx?t=p&c="+code+"&q=1&ssid=" + m_ssid + "')\" value='Pur'></td>");
				Response.Write("</td>");
				Response.Write("<td align=right ><input type=text size=5 name=qty"+iRows.ToString()+" value='0'>");
				Response.Write("<td align=right><a href=# rel=\"balloon"+i+"\">view details</a></td>");
				Response.Write("</tr>");
				Response.Write("<tr id=\"balloon"+i+"\" class=\"balloonstyle\" style=\"width: 350px; background-color: lightyellow\"><td>");
				Response.Write(doItemReport(code, double.Parse(low_stock.ToString())));
				Response.Write("</td></tr>");
				
				
			}
			if(i >= rows)
				break;
			code = code_curr;
			barcode = dr["barcode"].ToString();
			name = dr["name"].ToString();
			supplier = dr["supplier"].ToString();
			low_stock = MyIntParse(dr["low_stock"].ToString());
			for(int m=0; m<16; m++)
				nQty[m] = 0;
			nTotalQty = 0;
		}
		//else

		{
			int branch_id = MyIntParse(dr["branch_id"].ToString());
	//	DEBUG("branch =", branch_id);
			nQty[branch_id] = MyIntParse(dr["qty"].ToString());
		}
	}
	//DEBUG("IROWS ", iRows.ToString());
	Response.Write("<input type=hidden name=rows value='"+iRows.ToString()+"'>");
	Response.Write("<tr><td colspan=6 align=right ><input type=submit name=cmd value=Purchase></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}
*/


void PrintList()
{
	PrintAdminHeader();
	PrintAdminMenu();
	string supplier_old = "";
	
	Response.Write("<form name=frm action=?t=p&ssid="+m_ssid+" method=post>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>"+ m_tableTitle +"</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>Supplier:");
	DoSupplier(m_sSupplier);
	Response.Write("</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#0080db;font-weight:bold;\">");
	//Response.Write("<th>Supplier</td>");
	Response.Write("<th>Supplier Code</th>");
	Response.Write("<th>Description</th>");
	Response.Write("<th>Description CN</th>");
	Response.Write("<th>Current Stock</th>");
	Response.Write("<th>Warning Stock</th>");
	Response.Write("<th>Last Purchase</th>");
	Response.Write("<th>Purchase Price</th>");
	Response.Write("<th>Purchase Time</th>");
	Response.Write("<th>Purchase Stock</th>");
	//Response.Write("<th>Details</th>");
	Response.Write("<tr>");
	
	int rows  = 0;
	int iRows = 0;
	//string supplier_old = "";
	rows = ds.Tables["lowstock"].Rows.Count;
	if(rows <=0)
	{
		Response.Write("<tr><td colspan=7 align=center>No Data</td></tr>");
		Response.Write("</table>");
	}
	for(int i = 0; i < rows ; i++)
	{
		DataRow dr = ds.Tables["lowstock"].Rows[i];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string name_cn = dr["name_cn"].ToString();
		string low_stock = dr["low_stock"].ToString();
		string current_stock = dr["qty"].ToString();
		iRows++;
		if(supplier != supplier_old)
		{
			Response.Write("<tr><td colspan=9 bgcolor=black style=\"color:white\" ><b>"+DoGetSupplierFullName(supplier)+"<b></td></tr>");
			supplier_old = supplier;
		}
		Response.Write("<input type=hidden name=code"+iRows.ToString()+" value='"+code+"'>");
		Response.Write("<input type=hidden name=low_stock_old"+iRows.ToString()+" value='"+low_stock+"'>");
		string purchase_color = "bgcolor=";
		string font_color = "<font color=";
		switch(doAlterColor(code, double.Parse(low_stock.ToString())))
		{
			case "0":
				purchase_color += "#FFFFFF";
				font_color += "#000000>";
				break;
			case "e":
				purchase_color += "#FFFFFF";//purchase_color += "#FF6600";
				font_color += "#000000>";
				break;
			case "1":
				purchase_color += "#FFFFFF";//purchase_color += "#99CC00";
				font_color += "#000000>";
				break;
			case "2":
				purchase_color += "#FFFFFF";//purchase_color += "#FF0000";
				font_color += "#000000>";//font_color += "#FFFFFF>";
				break;
			case "3":
				purchase_color += "#FFFFFF";//purchase_color += "#FFFF00";
				font_color += "#000000>";
				break;
			default:
				break;
		}
		Response.Write("<tr "+ purchase_color+" >");
		Response.Write("<td>"+ font_color +""+ supplier_code +"</font></td>");
		Response.Write("<td>"+ font_color +""+ name +"</font></td>");
		Response.Write("<td>"+ font_color +""+ name_cn +"</font></td>");
		Response.Write("<td>"+ font_color +""+current_stock+"</font></td>");
		//Response.Write("<td>&nbsp;"+ font_color +"" +low_stock+"</font></td>");
		Response.Write("<td><input type=text style=border:0 readonly=readonly size=5 name=low_stock"+iRows.ToString()+" value='"+low_stock+"'></td>");
		//Response.Write("<td><input type=text readonly size=5 name=pqty"+iRows.ToString()+" value='"+last_purchase+"'></td>");
		Response.Write("<td>&nbsp;"+ font_color +""+last_purchase+"</font></td>");
		/**********************************/
		Response.Write("<td>&nbsp;"+ font_color +""+purchase_price+"</font></td>");
		Response.Write("<td>&nbsp;"+ font_color +""+P_Create_date+"</font></td>");
		/**********************************/
		Response.Write("<td><input type=text size=5 name=qty"+iRows.ToString()+" value='0'></td>");
		//Response.Write("<td><a href=# rel=\"balloon"+i+"\">Details</a></td>");
		Response.Write("</tr>");
		//Response.Write("<div  id=\"balloon"+i+"\" class=\"balloonstyle\" style=\"width: 350px; background-color: lightyellow\">");
		//Response.Write(doItemReport(code, double.Parse(low_stock.ToString())));
		//Response.Write("</div>");
		
	}
	Response.Write("<input type=hidden name=rows value='"+iRows.ToString()+"'>");
	Response.Write("<tr><td colspan=9 align=right ><input type=submit name=cmd value=Purchase></td></tr>");
	Response.Write("</table>");
}
string DoGetSupplierFullName(string init)
{
	if(ds.Tables["getsuppliername"] != null)
		ds.Tables["getsuppliername"].Clear();
	int rows = 0;
	string sc =" SELECT trading_name FROM card WHERE short_name ='"+init+"'";
	sc += " AND type=3 ";
	//DEBUG("sc ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "getsuppliername");
		if(rows <= 0)
			return init;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return ds.Tables["getsuppliername"].Rows[0]["trading_name"].ToString();
	//return true;
}
void DoSupplier(string supp)
{
	int rows = 0;
	if(ds.Tables["supplier"] != null)
		ds.Tables["supplier"].Clear();
	string sc =" SELECT id, trading_name, short_name FROM card WHERE type=3 ";
	sc += " ORDER BY trading_name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "supplier");
		if(rows <= 0)
			return ;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return ;
	}
	string s = "<select name=suppleir onchange=\"window.location=('?sup='+this.options[this.selectedIndex].value)\">";
	s += "<option value=''> All</option>";
	for(int i = 0; i < rows ; i++)
	{
		DataRow dr = ds.Tables["supplier"].Rows[i];
		string trading_name = dr["trading_name"].ToString();
		string short_name = dr["short_name"].ToString();
		string id = dr["id"].ToString();
		s += "<option value='"+id+"'";
		if(id == supp)
			s += " SELECTED ";
		s += ">";
		s += trading_name +"</option>";
	}
	s += "</select>";
    Response.Write(s);
	
}

string doAlterColor(string code, double warning_stock)
{
	
	//return "0";
	if(ds.Tables["p_report"] != null)
		ds.Tables["p_report"].Clear();
	string sc =" SELECT TOP 1 pi.qty AS qty, pi.price, ISNULL(p.date_create, 0) AS date_create, sq.last_stock , sq.qty AS current_stock ";
	sc += " FROM stock_qty sq ";
	sc += " LEFT OUTER JOIN purchase_item pi ON sq.code = pi.code";
	sc += " LEFT OUTER JOIN purchase p ON p.id = pi.id ";
	sc += " WHERE pi.code ="+code;
	sc += " AND sq.branch_id =1";
	sc += " GROUP BY pi.id, pi.price, p.date_create, sq.last_stock, sq.qty, pi.qty";
	sc += " ORDER BY pi.id DESC ";
//DEBUG("sc1=",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "p_report") <=0)
			return "0";
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		return "e";
	}
	
	string qty = ds.Tables["p_report"].Rows[0]["qty"].ToString();
	string create_date = DateTime.Parse(ds.Tables["p_report"].Rows[0]["date_create"].ToString()).ToString("dd-MM-yyyy");
	string today = System.DateTime.Now.ToString("dd-MM-yyyy");
	string check_point_date = GetSiteSettings("purchase_check_point_date", "10", false);
	string check_rate = GetSiteSettings("purchase_check_rate", "20", false);
	string check_rate_mix = GetSiteSettings("purchase_check_rate_mix", "5", false);
	string last_stock = ds.Tables["p_report"].Rows[0]["last_stock"].ToString();
	string current_stock = ds.Tables["p_report"].Rows[0]["current_stock"].ToString();
	if(double.Parse(last_stock) <=0)
		last_stock = qty;
	last_purchase = qty;
	double dSaleQty = double.Parse(last_stock) - double.Parse(current_stock);
	
	
	DateTime Current_date = System.DateTime.Now;
	P_Create_date = DateTime.Parse(ds.Tables["p_report"].Rows[0]["date_create"].ToString());
	purchase_price = MyDoubleParse(ds.Tables["p_report"].Rows[0]["price"].ToString()).ToString();
	TimeSpan span = Current_date.Subtract(P_Create_date);
	int days = span.Days;
	if(days == 0)
		days = 1;
	double daily_rate = Math.Round((dSaleQty /double.Parse(last_stock))/days,4) *100;
//DEBUG("DAILY RATE", daily_rate.ToString() + " check rate "+ check_rate);
	if(daily_rate >= double.Parse(check_rate))
		return "1";
	else if(daily_rate <= double.Parse(check_rate_mix))
		return "2";
	else 
		return "3";
}

string doItemReport(string code, double warning_stock)
{
	
	int rows = 0;
	if(ds.Tables["p_report"] != null)
		ds.Tables["p_report"].Clear();
	string sc =" SELECT TOP 1 pi.qty AS qty, ISNULL(p.date_create, 0) AS date_create, sq.last_stock , sq.qty AS current_stock ";
	sc += " FROM stock_qty sq ";
	sc += " LEFT OUTER JOIN purchase_item pi ON sq.code = pi.code";
	sc += " LEFT OUTER JOIN purchase p ON p.id = pi.id ";
	sc += " WHERE pi.code ="+code;
	sc += " AND sq.branch_id =1";
	sc += " GROUP BY pi.id, p.date_create, sq.last_stock, sq.qty, pi.qty";
	sc += " ORDER BY pi.id DESC ";
//DEBUG("sc2=",sc);
	//Response.Write(sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "p_report") ;
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		return "Sorry, Error Getting Report";
	}
	
	string tb = "";

	if(rows <=0)
	{
		
		tb += "No Purchase Record :: "+Lang("No Purchase Record");
		return tb;
	}
	
	string qty = ds.Tables["p_report"].Rows[0]["qty"].ToString();
	string create_date = DateTime.Parse(ds.Tables["p_report"].Rows[0]["date_create"].ToString()).ToString("dd-MM-yyyy");
	string today = System.DateTime.Now.ToString("dd-MM-yyyy");
	string check_point_date = GetSiteSettings("purchase_check_point_date", "10", false);
	string check_rate = GetSiteSettings("purchase_check_rate", "20", false);
	string check_rate_mix = GetSiteSettings("purchase_check_rate_mix", "5", false);
	string last_stock = ds.Tables["p_report"].Rows[0]["last_stock"].ToString();
	string current_stock = ds.Tables["p_report"].Rows[0]["current_stock"].ToString();
	if(double.Parse(last_stock) <=0)
		last_stock = qty;
		
	double dSaleQty = double.Parse(last_stock) - double.Parse(current_stock);
	
	
	DateTime Current_date = System.DateTime.Now;
	DateTime P_Create_date = DateTime.Parse(ds.Tables["p_report"].Rows[0]["date_create"].ToString());
	TimeSpan span = Current_date.Subtract(P_Create_date);
	int days = span.Days;
	if(days == 0)
		days = 1;
	double daily_rate = Math.Round((dSaleQty /double.Parse(last_stock))/days,4);
	tb += " Check Rate:"+(double.Parse(check_rate)/100).ToString("p")+" (Good) :: "+(double.Parse(check_rate_mix)/100).ToString("p")+" (Bad) <br>";
	tb += " Daily Rate:"+ daily_rate.ToString("p")+" (per day)<br>";
	tb += " =================<br>Details<br>================<br>";
	tb += " Last Purchase Qty:"+qty+"<br>";
	tb += " Last Purchase Date:"+create_date+"<br>";
	tb += " Last Stock:"+last_stock+"<br>";
	tb += " Current Stock:"+current_stock+"<br>";
	tb += " Duration:"+span.Days.ToString()+ " Days<br>";
	tb += " Warning Stock Qty:"+warning_stock.ToString()+"<br>";
	tb += " =================<br>Reported Date<br>===========<br>";
	tb += Current_date.ToString("dd-MM-yyyy");
	return tb;
	  
}
bool doUpdateWarningStock(string code, string value)
{
	if(ds.Tables["updatewarningstock"] != null)
		ds.Tables["updatewarningstock"].Clear();
	string sc = " UPDATE code_relations SET low_stock='"+value+"'";
	sc += " WHERE code ='"+code+"'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "updatewarningstock");
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		return false;
	}
	return true;
}
</script>

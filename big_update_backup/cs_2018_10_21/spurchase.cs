<!-- #include file="page_index.cs" -->
<!-- #include file="purchase_function.cs" -->
<script runat=server>

string m_branchID = "";
string m_type = "";
string m_tableTitle = "Supplier Purchase List";
string[] m_aBranchID = new string[16];
string[] m_aBranchName = new string[16];
string m_sLow = "";
string cat = "";
string s_cat = "";
string ss_cat = "";
int m_nBranches = 0;
string m_sSupplier = "";
string tableWidth = "97%";
string last_purchase = "";
string purchase_price = "";
string box_qty = "0";
string box = "0";
DateTime P_Create_date;


DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
    if(Request.QueryString["low"] != null && Request.QueryString["low"] != "")
		m_sLow = Request.QueryString["low"];
    Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);
    Trim(ref m_sLow);

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
		Response.Redirect("spurchase.aspx" + par);
		return;
	}
    
    if(Request.QueryString["t"] == "new")
        Session["m_sSupplier"] = null;
	 
	if(Request.QueryString["sup"] != null)
	{
		m_sSupplier = Request.QueryString["sup"];
        Session["m_sSupplier"] = m_sSupplier;
	}
    else
        Session["fixSupCode"] = "0";
      
    
    if(Request.QueryString["fix"] == "1")
        Session["fixSupCode"] = "1";
	
	if(Request.QueryString["t"] == "p")
	{
	 
		if(DoPurchaseProcess())
		{
			
			Session["fixSupCode"] = "0";
            Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?si="+ Session["m_sSupplier"] +"&spc=" + Session["m_sSupplier"] +"&pt=s&ssid=" + m_ssid + "\">");
              Session["m_sSupplier"] = null;
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
//DEBUG("supplier ", supplier);
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
		sc = " SELECT c.supplier, c.name, c.name_cn, c.code, c.low_stock, c.supplier_code, c.weight, s.qty, c.allocated_stock ";
		sc += " FROM code_relations c JOIN stock_qty s ON c.code = s.code ";
		sc += " WHERE 1=1 ";
        if(m_sLow == "1")
        {
		    sc += " AND c.low_stock != 0 ";
		    sc += " AND c.low_stock > s.qty "; 
        }
		//sc += " AND s.qty >0 ";
		sc += " AND s.branch_id =1";
        sc += "AND c.supplier = '"+m_sSupplier+"'";
        if(cat != "" && cat != "all")
		    sc += " AND c.cat = N'"+ cat +"' ";
	    if(s_cat != "" && s_cat != "all")
		    sc += " AND c.s_cat = N'"+ s_cat +"' ";
	    if(ss_cat != "" && ss_cat != "all")
		    sc += " AND c.ss_cat = N'"+ ss_cat +"' ";
		sc += " ORDER BY s.qty";
	}

	else
	{
		sc = " SELECT top 30 c.supplier, c.name, c.name_cn, c.code, c.low_stock, c.weight, c.supplier_code, s.qty, c.allocated_stock ";
		sc += " FROM code_relations c JOIN stock_qty s ON c.code = s.code ";
		sc += " WHERE 1=1 ";
        if(m_sLow == "1")
        {
		    sc += " AND c.low_stock != 0 ";
		    sc += " AND c.low_stock > s.qty "; 
        }
		//sc += " AND s.qty >0 ";
		sc += " AND s.branch_id =1";
        if(cat != "" && cat != "all")
		    sc += " AND c.cat = N'"+ cat +"' ";
	    if(s_cat != "" && s_cat != "all")
		    sc += " AND c.s_cat = N'"+ s_cat +"' ";
	    if(ss_cat != "" && ss_cat != "all")
		    sc += " AND c.ss_cat = N'"+ ss_cat +"' ";
		sc += " ORDER BY s.qty";
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
	Response.Write("<tr><td colspan=9><b>" + Lang("Supplier") + " :</b>&nbsp;&nbsp;");
	DoSupplier(m_sSupplier);
    Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>" + Lang("Catalog Select") + " :</b>&nbsp;&nbsp;");
    doCatSearch();
    Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b>" + Lang("Type") + " :</b>&nbsp;&nbsp;");
    doLowStock();
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
	Response.Write("<th>Box Qty</th>");
	Response.Write("<th>Purchase Time</th>");
	Response.Write("<th>Box</th>");
	Response.Write("<th>Purchase Stock</th>");
	//Response.Write("<th>Details</th>");
	Response.Write("<tr>");
	
	int rows  = 0;
	int iRows = 0;
	//string supplier_old = "";
	rows = ds.Tables["lowstock"].Rows.Count;
	if(rows <=0)
	{
		Response.Write("<tr><td colspan=9 align=center>No Data</td></tr>");
		Response.Write("</table>");
	}
	for(int i = 0; i < rows ; i++)
	{
		DataRow dr = ds.Tables["lowstock"].Rows[i];
		string code = dr["code"].ToString();
		string supplier = dr["supplier"].ToString();
//DEBUG("supplier",supplier);
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string name_cn = dr["name_cn"].ToString();
		string low_stock = dr["low_stock"].ToString();
		string current_stock = dr["qty"].ToString();
		//string box_qty = dr["allocated_stock"].ToString();
		string box_qty = dr["weight"].ToString();
		iRows++;
		if(supplier != supplier_old)
		{
			Response.Write("<tr><td colspan=11 bgcolor=black style=\"color:white\" ><b>"+DoGetSupplierFullName(supplier)+"<b></td></tr>");
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
		Response.Write("<td><input type=text style=border:0 readonly=readonly size=5 name=box_qty"+iRows.ToString()+" value='"+box_qty+"'></td>");
		//Response.Write("<td>&nbsp;"+ font_color +""+ box_qty +"</font></td>");
		Response.Write("<td>&nbsp;"+ font_color +""+P_Create_date+"</font></td>");
		Response.Write("<td><input type=text size=3 name=box"+iRows.ToString()+" value='");
		Response.Write("' onkeyup=\"document.all.qty"+iRows.ToString()+".value= this.value * document.all.box_qty"+iRows.ToString()+".value");
		Response.Write("\"> ");
		Response.Write("</td>");
		/**********************************/
		Response.Write("<td><input type=text size=5 name=qty"+iRows.ToString()+" value='0'></td>");
		//Response.Write("<td><a href=# rel=\"balloon"+i+"\">Details</a></td>");
		Response.Write("</tr>");
		//Response.Write("<div  id=\"balloon"+i+"\" class=\"balloonstyle\" style=\"width: 350px; background-color: lightyellow\">");
		//Response.Write(doItemReport(code, double.Parse(low_stock.ToString())));
		//Response.Write("</div>");
		
	}
	Response.Write("<input type=hidden name=rows value='"+iRows.ToString()+"'>");
	Response.Write("<tr><td colspan=11 align=right ><input type=submit name=cmd value=Purchase></td></tr>");
	Response.Write("</table>");
}
string DoGetSupplierFullName(string init)
{
	if(ds.Tables["getsuppliername"] != null)
		ds.Tables["getsuppliername"].Clear();
	int rows = 0;
	string sc =" SELECT trading_name, short_name, company FROM card WHERE id ='"+init+"'";
	sc += " AND type=3 ";
	//DEBUG("sc ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "getsuppliername");
		if(rows <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	if(ds.Tables["getsuppliername"].Rows[0]["short_name"].ToString() != "" && ds.Tables["getsuppliername"].Rows[0]["short_name"].ToString() != null)
        return ds.Tables["getsuppliername"].Rows[0]["short_name"].ToString();
    else if(ds.Tables["getsuppliername"].Rows[0]["trading_name"].ToString() != "" && ds.Tables["getsuppliername"].Rows[0]["trading_name"].ToString() != null)
        return ds.Tables["getsuppliername"].Rows[0]["trading_name"].ToString();
    else if(ds.Tables["getsuppliername"].Rows[0]["company"].ToString() != "" && ds.Tables["getsuppliername"].Rows[0]["company"].ToString() != null)
        return ds.Tables["getsuppliername"].Rows[0]["company"].ToString();
    else
        return "";
	//return true;
}
void DoSupplier(string supp)
{
	int rows = 0;
	if(ds.Tables["supplier"] != null)
		ds.Tables["supplier"].Clear();
	string sc =" SELECT id, trading_name, short_name, company FROM card WHERE type=3 ";
    if(Session["fixSupCode"] == "1")
        sc += " AND id =" + supp;
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
//    string s = "<select name=suppleir onchange=\"window.location=('?sup='+this.options[this.selectedIndex].value)\">";
	string s = "";
    if(Session["fixSupCode"] == "1")
    {
        s = "<b>";
       
		    DataRow dr = ds.Tables["supplier"].Rows[0];
		    string trading_name = dr["trading_name"].ToString();
		    string short_name = dr["short_name"].ToString();
            string company = dr["company"].ToString();
            string sId = dr["id"].ToString();
		 
		    if(short_name != "" && short_name != null)
                s += short_name;
            else if(trading_name != "" && trading_name != null)
                s += trading_name;
            else if(company != "" && company != null)
                s += company;
            s += "</b>";
	    
        
    }
    else
    {
        s = "<select name=suppleir onchange=\"window.location=('";
        s += "?cat=" + HttpUtility.UrlEncode(cat);
	    s += "&scat=" + HttpUtility.UrlEncode(s_cat);
	    s += "&sscat=" + HttpUtility.UrlEncode(ss_cat);
        s += "&low=" + m_sLow;
        s += "&ssid=" + m_ssid;
		//s += "&si='+this.options[this.selectedIndex].value)'";
        s += "&sup='+this.options[this.selectedIndex].value)\">";
	
    
	    s += "<option value=''> All</option>";
	    for(int i = 0; i < rows ; i++)
	    {
		    DataRow dr = ds.Tables["supplier"].Rows[i];
		    string trading_name = dr["trading_name"].ToString();
		    string short_name = dr["short_name"].ToString();
            string company = dr["company"].ToString();
            string sId = dr["id"].ToString();
		    s += "<option value='"+sId+"'";
		    if(sId == supp)
			    s += " SELECTED ";
		    s += ">";
		    if(short_name != "" && short_name != null)
                s += short_name +"</option>";
            else if(trading_name != "" && trading_name != null)
                s += trading_name +"</option>";
            else if(company != "" && company != null)
                s += company +"</option>";
	    }
	    s += "</select>";
    }
    Response.Write(s);
	
}
void doLowStock()
{
    string s ="";
    s = "<select name=low onchange=\"window.location=('";
    s += "?cat=" + HttpUtility.UrlEncode(cat);
	s += "&scat=" + HttpUtility.UrlEncode(s_cat);
	s += "&sscat=" + HttpUtility.UrlEncode(ss_cat);
    s += "&sup=" + m_sSupplier;
	s += "&si=" + m_sSupplier;
    s += "&ssid=" + m_ssid;
    s += "&low='+this.options[this.selectedIndex].value)\">";
	s += "<option value='0'> All</option>";
    if(m_sLow == "1")
	    s += "<option value='1' selected>Low Stock Only</option>";
	else
	    s += "<option value='1'>Low Stock Only</option>";
    s += "</select>";
    Response.Write(s);
    
}

string doAlterColor(string code, double warning_stock)
{
	
	//return "0";
	if(ds.Tables["p_report"] != null)
		ds.Tables["p_report"].Clear();
	string sc =" SELECT TOP 1 pi.qty AS qty, pi.price, ISNULL(p.date_create, 0) AS date_create, sq.last_stock , sq.qty AS current_stock, c.allocated_stock ";
	sc += " FROM stock_qty sq ";
	sc += " LEFT OUTER JOIN purchase_item pi ON sq.code = pi.code";
	sc += " LEFT OUTER JOIN purchase p ON p.id = pi.id ";
	sc += " LEFT OUTER JOIN code_relations c on c.code = pi.code ";
	sc += " WHERE pi.code ="+code;
	sc += " AND sq.branch_id =1";
	sc += " GROUP BY pi.id, pi.price, p.date_create, sq.last_stock, sq.qty, pi.qty , c.allocated_stock";
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
	string box_qty = ds.Tables["p_report"].Rows[0]["allocated_stock"].ToString();
//DEBUG("box_qty=",box_qty);
	if(double.Parse(last_stock) <=0)
		last_stock = qty;
	last_purchase = qty;
	double dSaleQty = double.Parse(last_stock) - double.Parse(current_stock);
	
	
	DateTime Current_date = System.DateTime.Now;
	P_Create_date = DateTime.Parse(ds.Tables["p_report"].Rows[0]["date_create"].ToString());
	purchase_price = MyDoubleParse(ds.Tables["p_report"].Rows[0]["price"].ToString()).ToString();
	box_qty = MyDoubleParse(ds.Tables["p_report"].Rows[0]["allocated_stock"].ToString()).ToString();
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

string doItemReport111(string code, double warning_stock)
{
	
	int rows = 0;
	if(ds.Tables["p_report"] != null)
		ds.Tables["p_report"].Clear();
	string sc =" SELECT TOP 1 pi.qty AS qty, ISNULL(p.date_create, 0) AS date_create, sq.last_stock , sq.qty AS current_stock, c.allocated_stock ";
	sc += " FROM stock_qty sq ";
	sc += " LEFT OUTER JOIN purchase_item pi ON sq.code = pi.code";
	sc += " LEFT OUTER JOIN purchase p ON p.id = pi.id ";
	sc += " LEFT OUTER JOIN code_relations c on c.code = pi.code ";
	sc += " WHERE pi.code ="+code;
	sc += " AND sq.branch_id =1";
	sc += " GROUP BY pi.id, p.date_create, sq.last_stock, sq.qty, pi.qty, c.allocated_stock ";
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
	string box_qty = ds.Tables["p_report"].Rows[0]["allocated_stock"].ToString();
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
bool doCatSearch()
{
    int rows = 0;
	string sc = "SELECT DISTINCT cat FROM catalog WHERE cat <> 'Brands' ";
	sc += " ORDER BY cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "cat");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;
	Response.Write("<select name=s ");
 //   Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
    Response.Write(" onchange=\"window.location=('");
//	Response.Write("?r="+ DateTime.Now.ToOADate() +"");
    Response.Write("?sup=" + m_sSupplier +"");
	Response.Write("&si=" + m_sSupplier +"");
    Response.Write("&low=" + m_sLow + "");
    Response.Write("&ssid=" + m_ssid + "");
    Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
		else
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
	    sc = "SELECT DISTINCT s_cat FROM catalog ";
		sc += " WHERE cat <> 'Brands' AND cat = N'" + cat + "' ";
		sc += " ORDER BY s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(ds, "s_cat");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
//		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
        Response.Write(" onchange=\"window.location=('");
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
        Response.Write("&sup=" + m_sSupplier +"");
		 Response.Write("&si=" + m_sSupplier +"");
        Response.Write("&low=" + m_sLow + "");
        Response.Write("&ssid=" + m_ssid + "");
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>" + Lang("Show All") + "</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
			
		}

		Response.Write("</select>");
	}
	
	if(s_cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM catalog ";
		sc += " WHERE cat <> 'Brands' AND cat = N'" + cat + "' ";
		sc += " AND s_cat = N'" + s_cat + "' ";
		sc += " ORDER BY ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(ds, "ss_cat");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
//		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
        Response.Write(" onchange=\"window.location=('");
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
        Response.Write("&sup=" + m_sSupplier +"");
		 Response.Write("&si=" + m_sSupplier +"");
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
        Response.Write("&low=" + m_sLow + "");
        Response.Write("&ssid=" + m_ssid + "");
		Response.Write("&sscat=' + this.options[this.selectedIndex].value) \"");
		Response.Write(">");
		Response.Write("<option value='all'>" + Lang("Show All") + "</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = ds.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
		}
		Response.Write("</select>");
	}
	return true;
}
</script>

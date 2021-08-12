<!-- #include file="page_index.cs" -->

<script runat=server>
DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table

string m_cat = "";
string m_scat = "";
string m_sscat = "";
bool m_DisplayInput = false;
string m_branchID = "1";
string m_action = "";

void Page_Load(Object Src, EventArgs E ) 
{
	
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("stock"))
		return;
	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"];
	
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		m_scat = Request.QueryString["scat"];
	if(Request.QueryString["br"] != null && Request.QueryString["br"] != "" && Request.QueryString["br"] != "all")
	{
		m_branchID = Request.QueryString["br"];
	}

	if(Session["branch_support"] != null)
		m_DisplayInput = false;
	if(!DoQueryStockItem())
		return;
		
	m_action = g("a");
	if(m_action == "export")
	{
		string fn = "temp/stocktake_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm") + ".xls";
		if(DataTableExportToExcel(dst.Tables["stock_qty"], Server.MapPath(fn)))
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><Center><h4>Stock List Export Done, click to download</h4>");
			Response.Write("<a href=" + fn + " class=o>" + fn + "</a>");
			Response.Write("<br><br><br><input type=button class=b value='Close Window' onclick='window.close()'>");
		}
		return;
	}
		
	string scmd = Request.Form["cmd"];
	
	if(scmd == "INPUT ITEM TO STOCK")
	{
		if(DoInsertToStock_Qty())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "");
			Response.Write("\">");	
			return;
		}
	
	}

	if(Request.QueryString["pr"] != null && Request.QueryString["pr"] != "")
		BindStockTakingForm();
	else
	{
		InitializeData();
		InputItemtoStock();
	}
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT RTRIM(LTRIM(cat)) AS cat FROM product p  ORDER BY RTRIM(LTRIM(cat))";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
		return true;
	Response.Write("Catalog Select: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "");

	Response.Write("&cat='+escape(this.options[this.selectedIndex].value))\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");

//DEBUG("mcat = ", m_cat);
	string cat_scode = "";
	string cat_fcode = "";
	string scat_scode = "";
	string scat_fcode = "";
	string sscat_scode = "";
	string sscat_fcode = "";
		
	for(int i=0; i<rows; i++)
	{
		
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		
		if(m_cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(m_cat != null && m_cat != "" && m_cat != "all")
	{
		sc = "SELECT DISTINCT RTRIM(LTRIM(s_cat)) AS s_cat FROM product  WHERE cat = '"+ m_cat +"' ";
		sc += " ORDER BY RTRIM(LTRIM(s_cat))";
//DEBUG("sc = ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&");

		Response.Write("cat="+ m_cat +"&scat='+ escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");

		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			//DEBUG(" s = ", s);
//DEBUG(" scat = ", s_cat);
			if(m_scat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
		}

		Response.Write("</select>");
	}
/*	if(m_scat != null && m_scat != ""  && m_scat != "all")
	{
		sc = "SELECT DISTINCT RTRIM(LTRIM(ss_cat)) AS ss_cat FROM product p WHERE cat = '"+ m_cat +"' ";
		sc += " AND s_cat = '"+ m_scat +"' ";
		sc += " ORDER BY RTRIM(LTRIM(ss_cat)) ";
//DEBUG("sc = ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&");

		Response.Write("cat="+ m_cat+"&scat="+ m_scat +"&sscat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");

		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(m_sscat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}
	*/
	return true;
}


void InputItemtoStock()
{
	string tableName = "s_cat";
	Response.Write("<br><br><form name=frm method=post>");
	Response.Write("<table align=center width= valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr align=center><td>");
	
/*	Response.Write("SELECT SUB CATALOG: <select name=s_cat><option value='all'>All");
	for(int i=0; i<dst.Tables[tableName].Rows.Count; i++)
	{
		DataRow dr = dst.Tables[tableName].Rows[i];
		//string s_cat = dr["s_cat"].ToString();
		string s_cat = dr["cat"].ToString();
		Response.Write("<option value='"+ s_cat +"'>"+ s_cat +"</option>");
	}
	
	Response.Write("</select>");
	*/
	/*Response.Write("Select Branch : ");	
	PrintBranchNameOptions(m_branchID, "", true);
	*/
	if(Session["branch_support"] != null)
	{
		Response.Write("<tr align=center><td><b>Select Branch : </b>");
		if(!PrintBranchNameOptions())
			return;
		Response.Write("</td></tr>");	
	}
	
	
	Response.Write("<tr><td>");
	DoItemOption();
	Response.Write("<input title='print all items qty greater than 0' type=button  value='Print Stock > 0 QTY' "+Session["button_style"]+"  onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?pr=1&cat="+ HttpUtility.UrlEncode(Request.QueryString["cat"]) +"&scat="+ HttpUtility.UrlEncode(Request.QueryString["scat"]) +"");
	
	if(Session["branch_support"] != null)
	{
		//Response.Write("&br='+ document.frm.branch.selectedIndex");
		//Response.Write("&br='+ this.options[this.selectedIndex].value");
		Response.Write("&br='+ document.frm.branch.value"); //.options[this.selectedIndex].value");
	}
	else
		Response.Write("'");
	
	Response.Write(")\" >");
	Response.Write("<input title='print all items qty equal 0' type=button value='Print Stock = 0 QTY' "+Session["button_style"]+" onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?pr=0&cat="+ HttpUtility.UrlEncode(Request.QueryString["cat"]) +"&scat="+ HttpUtility.UrlEncode(Request.QueryString["scat"]) +"");
	

	if(Session["branch_support"] != null)
		Response.Write("&br='+ document.frm.branch.value"); //.options[this.selectedIndex].value");
		//Response.Write("&br='+document.frm.branch.selectedIndex");
		//Response.Write("&br='+this.options[this.selectedIndex].value");
	else
		Response.Write("'");
	Response.Write(")\" >");
	
	//Response.Write("')\" >");
	Response.Write("<input title='print all items qty less than 0' type=button value='Print Stock < 0 QTY' "+Session["button_style"]+" onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?pr=2&cat="+ HttpUtility.UrlEncode(Request.QueryString["cat"]) +"&scat="+ HttpUtility.UrlEncode(Request.QueryString["scat"]) +"");
	
	if(Session["branch_support"] != null)
	{
		Response.Write("&br='+ document.frm.branch.value"); //.options[this.selectedIndex].value");
		//Response.Write("&br='+document.frm.branch.selectedIndex");
		//Response.Write("&br='+this.options[this.selectedIndex].value");
	}
	else
		Response.Write("'");
	
	Response.Write(")\" >");
	Response.Write("<input title='print all stock items' type=button value='Print All Items' "+Session["button_style"]+"  onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?pr=3&cat="+ HttpUtility.UrlEncode(Request.QueryString["cat"]) +"&scat="+ HttpUtility.UrlEncode(Request.QueryString["scat"]) +"");
	
	if(Session["branch_support"] != null)
	{
		Response.Write("&br='+ document.frm.branch.value"); //.options[this.selectedIndex].value");
		//Response.Write("&br='+document.frm.branch.selectedIndex");
		//Response.Write("&br='+this.options[this.selectedIndex].value");
	}
	else
		Response.Write("'");

	Response.Write(")\" >");
	//Response.Write("')\" >");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Select Qty < <input name=qty_limit size=3 value=0> ");
	Response.Write("<input type=button value='Export' class=b  onclick=\"window.open('?a=export&br=1&qty=' + document.frm.qty_limit.value + '&pr=3&cat=" + HttpUtility.UrlEncode(Request.QueryString["cat"]) + "&scat=" + HttpUtility.UrlEncode(Request.QueryString["scat"]) + "')\">");
	Response.Write("</td></tr>");
	Response.Write("</table>");

	string code = Request.Form["code"];
	string supplier_code = Request.Form["supplier_code"];
	string desc = Request.Form["desc"];
	string cost = "0";
	//Request.Form["cost"];
	string qty = "0";
	if(Session["slt_code"] != null && Session["slt_code"] != "")
	{
		code = Session["slt_code"].ToString();
		supplier_code = Session["slt_supplier_code"].ToString();
		desc = Session["slt_name"].ToString();
	}
	Response.Write("</form>");
}

bool CheckValidCode(string code)
{
	bool bValid = false;

	string sc = " SELECT * FROM ";
	if(g_bRetailVersion)
		sc += " stock_qty sq ";
	else
		sc += " product ";
	sc += " WHERE code = "+ code;
//DEBUG("sc = ", sc);

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst) > 0)
		{
			bValid =true;
			return bValid;
		}

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	return bValid;

}
bool DoInsertToStock_Qty()
{
	string sc =" SET DATEFORMAT dmy ";
	string code = Request.Form["code"];
	string supplier_code = Request.Form["supplier_code"];
	string stock = Request.Form["qty"];
	string cost = Request.Form["cost"];
	if(code == "")
	{
		Response.Write("<script language=javascript");
		Response.Write("> window.alert('INVALID PRODUCT CODE'); window.location=('"+ Request.ServerVariables["URL"] +"'); \r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		return false;
	}
	if(code != "" && code != null)
	{
		if(!TSIsDigit(code))
		{
			Response.Write("<script language=javascript");
			Response.Write("> window.alert('INVALID PRODUCT CODE'); window.location=('"+ Request.ServerVariables["URL"] +"'); \r\n");
			Response.Write("</script\r\n");
			Response.Write(">\r\n");
			return false;
		}
	}
	
	bool bDuplicate = CheckValidCode(code);
	

	if(bDuplicate)
	{
		//Response.Write("<br><center><h3><font color=red>This Item is ALREADY EXISTED in STOCK</font></h3>");
		Response.Write("<script language=javascript");
		Response.Write("> window.alert('ITEM ALREADY EXISTED in STOCK'); window.location=('"+ Request.ServerVariables["URL"] +"'); \r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		//Response.Write("<input type=button value='BACK' "+ Session["button_style"] +" onclick='window.history.go(-1)'> </center><br>");

		return false;
	}
//DEBUG("bvalid = ", bDuplicate.ToString());
	if(g_bRetailVersion)
	{
		sc += " INSERT INTO stock_qty (code, qty, supplier_price) ";
		sc += " VALUES("+ code +", "+ stock +", "+ cost +") ";
	}
	else
	{
		sc += " INSERT INTO product (code, name, brand, cat, s_cat, ss_cat, hot, price, stock, eta, supplier ";
		sc += " , supplier_code, supplier_price, price_dropped, price_age, allocated_stock, popular, real_stock ";
		sc += "  )";
		sc += " (SELECT code, name, brand, cat, s_cat, ss_cat, hot, supplier_price * rate, "+ cost +", '', supplier ";
		sc += " , supplier_code, supplier_price, 0, GETDATE(), 0, popular, "+ stock +" FROM code_relations WHERE code= "+ code +")";

	}
//DEBUG("sc = ", sc);
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}
bool DoQueryStockItem()
{
	string sc = "";
	string query_qty = Request.QueryString["pr"];
	string query_scat = Request.QueryString["scat"];
	if(query_qty != null && query_qty != "")
	{
		if(!TSIsDigit(query_qty))
		{
			Response.Write("<script language=javascript>window.location=('stocktake.aspx');</script");
			Response.Write(">");
			return false;
		}
	}
	sc = " SELECT c.s_cat, c.code, c.name , c.barcode, c.supplier_code ";
	sc += ", sq.qty AS stock ";
	sc += ", (SELECT sum(oi.quantity) FROM order_item oi JOIN orders o ON o.id = oi.id WHERE oi.code=c.code ";
	if(Session["branch_support"] != null)
	{
		if(Request.QueryString["br"] != null && Request.QueryString["br"] != "" && Request.QueryString["br"] != "all" && Request.QueryString["br"] != "0")
			sc += " AND o.branch = '"+ m_branchID +"' ";
	}
	else
		sc += " AND o.branch = 1 ";
	sc += " AND (invoice_number is null OR invoice_number = '')) AS orderQTY "; 
	sc += " FROM code_relations c LEFT OUTER JOIN ";
	sc += " stock_qty sq ON sq.code = c.code ";
	sc += " WHERE 1 = 1 ";
	if(query_qty == "0")
	{
		sc += " AND sq.qty = 0 ";
	}
	else if(query_qty == "1")
	{
		sc += " AND sq.qty > 0 ";
	}
	else if(query_qty == "2")
	{
		sc += " AND sq.qty < 0 ";
	}
	int nQtyLimit = MyIntParse(g("qty"));
	if(Request.QueryString["qty"] != null && Request.QueryString["qty"].ToString() != "")
	{
		sc += " AND sq.qty < " + nQtyLimit;
	}
	if(query_scat != "" && query_scat != null && query_scat != "all")
		sc += " AND c.s_cat = '"+ query_scat +"' ";
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "" && Request.QueryString["cat"] != "all")
		sc += " AND c.cat = '"+ Request.QueryString["cat"] +"' ";
//		sc += " AND c.cat = '"+ query_scat +"' ";
	
	if(Session["branch_support"] != null)
	{
		if(Request.QueryString["br"] != null && Request.QueryString["br"] != "" && Request.QueryString["br"] != "all" && Request.QueryString["br"] != "0")
			sc += " AND sq.branch_id = '"+ m_branchID +"' ";
	}
	else
		sc += " AND sq.branch_id = 1 ";
	sc += " GROUP BY c.s_cat, c.code, c.name, c.barcode, c.supplier_code ";
	sc += ", sq.qty ";
	sc += " ORDER BY c.supplier_code "; //c.barcode, c.code ";
//DEBUG("sc= ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "stock_qty");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}
bool BindStockTakingForm()
{
		
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "stock_qty";

	int rows = 0;
	if(dst.Tables[tableName] != null)
		rows = dst.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();

	sb.Append("<html><head>");
	sb.Append("<style type=text/css>");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:6PT;FONT-FAMILY:verdana;}");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:6PT;FONT-FAMILY:verdana;}");
	sb.Append("</style></head>");
	sb.Append("<body onload='window.print()' marginwidth=0 marginheight=0 topmargin=10 leftmargin=0 text=black>");
	
	sb.Append("<table width=100% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:3px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	//if(page == 1)
	{
		sb.Append("<tr><td colspan=2>");
		//string header = ReadSitePage("flare_header");
		string header = "<center><font size=2><b>" + Lang("Stock Taking Form") + "<b></font></ceter>";
		header += "<br>Taking Date: "+ DateTime.Now.ToString("dd/MM/yy")+"<br>";
		
		header += "<br><br><br>";
		header = header.Replace("@@date", DateTime.Now.ToString("dd/MM/yy"));
		sb.Append(header);
	
		sb.Append("</td></tr>");
	}

	if(rows <= 0)
	{
		sb.Append("</table>");
		return true;
	}

	int start = 0;

	StringBuilder sb1 = new StringBuilder();
	sb1.Append("<tr>");
	int loop = 10000;
	while(start < rows)
	{
		
		if(loop-- < 0)
			break; //protection

		sb1.Append("<tr>");
		sb1.Append("<td valign=top>");
		sb1.Append(PrintHalfPage(start, rows, page_rows, ref start));
		sb1.Append("</td>");

		if(start < rows)
		{
	
			sb1.Append("<td valign=top>");
			sb1.Append(PrintHalfPage(start, rows, page_rows, ref start));
			sb1.Append("</td>");
		}
		sb1.Append("</tr>");

		sb1.Append("<tr><td colspan=2>&nbsp;</td></tr>");
	}

	sb.Append(sb1.ToString());

	sb.Append("</td></tr>");
	sb.Append("</table>");

	Response.Write(sb.ToString());
	return true;
}

string PrintHalfPage(int start, int rows, int page_rows, ref int finished)
{
	string th1 = "<table width=100% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white";
	th1 += " style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">";

	StringBuilder sb1 = new StringBuilder();
	sb1.Append(th1);

	string s_cat = "";
	string ss_cat = "";
	string s_cat_old = "";
	string code = "";
	string barcode = "";
	string name = "";
	string qty = "";
	string price = "";
	string lr1 = "";
	string supplier_code = "";
	double drrp = 0;
	int count = 0;
	
	string tableName = "stock_qty";

	int i = start;
	for(i=start; i<rows; i++)
	{
		DataRow dr = dst.Tables[tableName].Rows[i];

		s_cat = dr["s_cat"].ToString();
		Trim(ref s_cat);
		code = dr["code"].ToString();
		supplier_code = dr["supplier_code"].ToString();
		barcode = dr["barcode"].ToString();
		name = dr["name"].ToString();
	name = StripHTMLtags(name);
		//price = dr["price"].ToString();
		qty = dr["stock"].ToString();
		if(count > page_rows)
			break; //print at the right half of the page with a new catalog start
		if(s_cat != s_cat_old || i == start)
		{
			if(i ==	start)
				sb1.Append("<tr bgcolor=#DDDDDD><td><b>Code</b></td><td><b>BarCode</b></td><td><b>" + s_cat + "</b></td><td align=right><b>QTY</b></td><td align=right><b>ORDER QTY</b></td><td>ADJ</td></tr>");
			else
				sb1.Append("<tr bgcolor=#DDDDDD><td>&nbsp;</td><td>&nbsp;</td><td><b>" + s_cat + "</b></td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>");
			s_cat_old = s_cat;
			count++;
			if(count > page_rows)
				break; //print at the right half of the page with a new catalog start
		}

		if(name.Length > 50)
			name = name.Substring(0, 50);
		sb1.Append("<tr>");
		sb1.Append("<td width=10%>" +  supplier_code + "&nbsp&nbsp;</td>");
		sb1.Append("<td width=10%>" + barcode + "</td>");
		sb1.Append("<td width=40%>" + name + "</td>");
		sb1.Append("<td align=right width=5%>" + qty + "</td>");
		sb1.Append("<td align=right width=5%>" + dr["orderQTY"].ToString() +"</td>");
		sb1.Append("<td width=4%>&nbsp;</td>");
		//sb1.Append("<td align=right>" + drrp.ToString("c") + "</td>");
		sb1.Append("</tr>");
		count++;
	}
	sb1.Append("</table>");
	finished = i;
	return sb1.ToString();
}

</script>

<asp:Label id=LFooter runat=server/>

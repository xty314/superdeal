<!-- #include file="price.cs" -->

<script runat=server>

string ebrand;
string ecat;
string es_cat;
string ess_cat;

string brand;
string cat;
string s_cat;
string ss_cat;

string m_editPriceBox = ""; //for edit retail price

string m_type = null;
bool m_bPhasedOut = false;
bool m_bService = false;

int page = 1;
const int m_nPageSize = 35; //how many rows in oen page
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

const string cols = "10";	//how many columns main table has, used to write colspan=
string tableTitle = "Item List";
const string thisurl = "eprice.aspx";

string m_supplier = null;
string tableWidth = "97%";
int m_cols = 8;
string[] m_aBranch = new string[64];
int m_branches = 1;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	string sbranch = GetSiteSettings("show_stock_branch_idz", "1,2,3");
	m_aBranch[0] = "1";
	if(Session["branch_support"] != null)
	{
	if(sbranch != "")
	{
		int i = 0;
		int j = 0;
		string ones = "";

		for(i=0; i<sbranch.Length; i++)
		{
			if(sbranch[i] == ' ' || sbranch[i] == ',' || sbranch[i] == ';')
			{
				Trim(ref ones);
				if(ones != "")
				{
					if(!TSIsDigit(ones))
						break;
					m_aBranch[j++] = ones;
					ones = "";
				}
			}
			else
			{
				ones += sbranch[i];
			}
		}
		if(ones != "" && TSIsDigit(ones)) //the last one
			m_aBranch[j++] = ones;
		m_branches = j;
	}
	}

	GetQueryStrings();

	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["ep"] != null)
		m_editPriceBox = Request.QueryString["ep"];
	
	if(Request.QueryString["d"] != null && Request.QueryString["d"] != "")
		m_supplier = Request.QueryString["d"];

/*	if(m_type == "update")
	{
		string update = Request.Form["update"];
		if(UpdateAllRows())
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			string s = "<br><br>done! wait a moment......... <br>\r\n";
			s += "<meta http-equiv=\"refresh\" content=\"1; URL=";
			s += WriteURLWithoutPageNumber();
			s += "&p=";
			s += page;
			s += "\"></body></html>";
			Response.Write(s);
		}
	}
	else
*/
	{
		if(!DoSearch())
			return;
	
		WriteHeaders();
		MyDrawTable();
		WriteFooter();
		PrintCatalogSelection();
	}

	PrintAdminFooter();
}

string WriteURLWithoutPageNumber()
{
	StringBuilder sb = new StringBuilder();

	sb.Append(thisurl + "?po=" + (m_bPhasedOut ? "1" : "0").ToString());
	sb.Append("&service=" + (m_bService ? "1" : "0").ToString() + "&");
	if(brand != null)
	{
		sb.Append("b=");
		sb.Append(ebrand);
	}
	else
	{
		sb.Append("c=");
		sb.Append(ecat);
	}
	if(s_cat != null)
	{
		sb.Append("&s=");
		sb.Append(es_cat);
	}
	if(ss_cat != null)
	{
		sb.Append("&ss=");
		sb.Append(ess_cat);
	}
	if(m_supplier != null)
	{
		sb.Append("&d=");
		sb.Append(m_supplier);
	}
	sb.Append("&r=" + DateTime.Now.ToOADate());
	return sb.ToString();
}

void GetQueryStrings()
{
	brand = Request.QueryString["b"];
	cat = Request.QueryString["c"];
	s_cat = Request.QueryString["s"];
	ss_cat = Request.QueryString["ss"];

	ebrand = HttpUtility.UrlEncode(brand);
	ecat = HttpUtility.UrlEncode(cat);
	es_cat = HttpUtility.UrlEncode(s_cat);
	ess_cat = HttpUtility.UrlEncode(ss_cat);
	m_type = Request.QueryString["t"];
	m_bPhasedOut = (Request.QueryString["po"] == "1");
	if(m_bPhasedOut)
		tableTitle = "Phased Out Items";
	m_bService = (Request.QueryString["service"] == "1");
	if(m_bService)
		tableTitle = "Service Items";
	string spage = Request.QueryString["p"];
	if(spage != null)
		page = int.Parse(spage);
//DEBUG("page=", page);
}

Boolean DoSearch()
{
	StringBuilder sb = new StringBuilder();

	sb.Append("SELECT c.id, c.code, c.supplier_code, c.brand, c.name, c.cat, c.s_cat, c.ss_cat, c.average_cost, c.supplier_price");
	if(!m_bPhasedOut)
		sb.Append(", p.stock");
	else
	{
		sb.Append(", ISNULL((SELECT sum(qty) FROM stock_qty WHERE c.code = code ");
		if(Session["branch_support"] == null)
			sb.Append(" AND branch_id = 1 ");
		sb.Append(" ), 0) AS stock ");
	}
		//sb.Append(", ISNULL(q.qty, '0') AS stock ");
	sb.Append(", p.price, c.supplier_price, c.rate ");
//	sb.Append(", isnull(s.code, 0) AS special ");
	if(m_bPhasedOut)
	{
		sb.Append(" FROM product_skip p JOIN code_relations c ON p.id=c.id ");
//		sb.Append(" LEFT OUTER JOIN stock_qty q ON q.code = c.code ");
//		sb.Append(" FROM code_relations c Left outer join product_skip p  ON p.id=c.id ");
	}
	else
		sb.Append(" FROM product p JOIN code_relations c ON p.code = c.code "); //p.supplier+p.supplier_code=c.id ");
//	sb.Append(" LEFT OUTER JOIN specials s on c.code=s.code ");
	string swhere = "";
	if(m_bService)
		swhere += " c.is_service = 1 ";
	else
	{
		if(brand != null)
			swhere += " c.brand='" + brand + "'";
		else if(cat != null && cat != "")
			swhere += " c.cat='" + cat + "'";

		if(s_cat != null && s_cat != "")
		{
			if(swhere != "")
				swhere += " AND ";
			swhere += " c.s_cat='" + s_cat + "'";
		}
		if(ss_cat != null && ss_cat != "")
		{
			if(swhere != "")
				swhere += " AND ";
			swhere += " c.ss_cat='" + ss_cat + "'";
		}
		if(m_supplier != null)
		{
			if(swhere != "")
				swhere += " AND ";
			swhere += " c.supplier='" + m_supplier + "'";
		}
	}
	if(swhere != "")
		sb.Append(" WHERE " + swhere);

if(Request.Form["search"] != null && Request.Form["search"] != "")
{
	string kw = Request.Form["search"];
	
	if(TSIsDigit(Request.Form["search"].ToString()))
		sb.Append(" AND (c.supplier_code = '" + kw + "' OR c.barcode = '"+ kw +"') ");
	else
	{
		kw = EncodeQuote(kw);
		sb.Append(" AND (c.barcode = '"+ kw +"' OR c.supplier_code = '"+ kw +"' OR c.name like '%"+ kw +"%') ");
	}
}

	sb.Append(" ORDER BY c.brand, c.s_cat, c.ss_cat, c.name, c.code");
//DEBUG("query=", sb.ToString());	
	try
	{
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		int rows = myAdapter.Fill(dst, "product");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return true;
}

Boolean UpdateAllRows()
{
	int i = (page-1) * m_nPageSize;
	string id = Request.Form["id"+i.ToString()];
	while(id != null)
	{
		if(!UpdateOneRow(i.ToString()))
			return false;;
		i++;
		id = Request.Form["id"+i.ToString()];
	}
	return true;
}

Boolean UpdateOneRow(string sRow)
{
	Boolean bRet = true;

	string id		= Request.Form["id"+sRow];
	string code		= Request.Form["code"+sRow];
	string name		= Request.Form["name"+sRow];
	string brand	= Request.Form["brand"+sRow];
	string supplier_price_old = Request.Form["supplier_price_old"+sRow];
	string supplier_price = Request.Form["supplier_price"+sRow];
	string rate = Request.Form["rate"+sRow];
	string price_old = Request.Form["price_old"+sRow];
	double dPrice_old = double.Parse(price_old, NumberStyles.Currency, null);
	string price = "";
	if(Request.Form["price"+sRow] != null)
		price = Request.Form["price"+sRow];
	string stock = Request.Form["stock"+sRow];
	if(stock == "" || string.Compare(stock, "yes", true) == 0)
		stock = "null";
	string stock_old = Request.Form["stock_old"+sRow];
	if(stock_old == "" || string.Compare(stock_old, "yes", true) == 0)
		stock_old = "null";

	bool bSpecial = (Request.Form["special"+sRow] == "on");
	bool bSpecial_old = (Request.Form["special_old"+sRow] == "on");

	double dsupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dPrice = 0;
	double dRate = double.Parse(rate);
	if(price != "")
	{
//DEBUG("price=", price);
		dPrice = double.Parse(price, NumberStyles.Currency, null);
//		dRate = CalculatePriceRate(dsupplier_price, dPrice);
	}
	else
	{
//DEBUG("rate=", dRate.ToString());
		dPrice = CalculateRetailPrice(dsupplier_price, dRate);
	}
	if(supplier_price_old != supplier_price || price != "")
	{
		//update product (live update)
		if(!UpdateSupplierPrice(code, id, dsupplier_price, dPrice))
			return false;
//		if(price != "")
//		{
//			if(!UpdatePriceRate(code, dRate))
//				return false;
//		}
	}

	//write price history
//	if(dPrice != dPrice_old)
	if(supplier_price_old != supplier_price)
	{
		if(!WritePriceHistory(code, dsupplier_price))
			return false;
	}

	if(stock_old != stock)
	{
		if(!UpdateStock(code, id, stock))
			return false;
	}

	if(bSpecial != bSpecial_old)
	{
		if(bSpecial) //do insert
			bRet = AddSpecial(code);
		else //do delete
			bRet = RemoveSpecial(code);
	}
	return bRet;
}

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("<html><style type=\"text/css\">td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
//	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>");
//	sb.Append("<body bgcolor=#666696>\r\n");
//	sb.Append("<table width=100% height=100% bgcolor=white align=center valign=center><tr><td valign=top>");
	if(m_bPhasedOut)
		tableTitle = "Phased Out Items";
/*	sb.Append("<br><center><h3>" + tableTitle + "</h3></center>");
*/
	Response.Write("<form action=");
	Response.Write(WriteURLWithoutPageNumber());
	Response.Write("&t=update&p=");
	Response.Write(page);
	Response.Write(" method=post>\r\n");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>"+ tableTitle +"</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");

	Response.Write("</tr></table>");	
	
	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//Response.Write("<tr><td><br></td></tr>");	
	Response.Write("<tr><td colspan='"+ m_cols +"'><input type=text name=search ><input type=submit name=cmd value='SEARCH PRODUCT' "+ Session["button_style"] +">");
	Response.Write("<input type=submit name=cmd value='SHOW All' "+ Session["button_style"] +">");
	Response.Write("<script language=javascript>window.froms[0].search.focus();</script");
	Response.Write(">");
//	Response.Write("</td></tr></table>");
	//Response.Write(sb.ToString());
	Response.Flush();
}

void WriteFooter()
{
	StringBuilder sb = new StringBuilder();
//	sb.Append("</td></tr></table>");
	sb.Append("</form>");//</body></html>");
	Response.Write(sb.ToString());
}

Boolean MyDrawTable()
{
	Boolean bRet = true;
	DrawTableHeader();
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (page-1) * m_nPageSize;
	for(int i=startPage; i<dst.Tables["product"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
		{
			bRet = false;
			break;
		}
	}

	StringBuilder sb = new StringBuilder();
//	sb.Append("<tr><td colspan=" + cols + " align=right><input type=submit name=update value='Update'></td></tr>");
	sb.Append("<tr><td colspan=" + cols + " align=right>Page: ");
	int pages = dst.Tables["product"].Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != page)
		{
			sb.Append("<a href=");
			sb.Append(WriteURLWithoutPageNumber());
			sb.Append("&p=");
			sb.Append(i.ToString());
			sb.Append(">");
			sb.Append(i.ToString());
			sb.Append("</a> ");
		}
		else
		{
			sb.Append("<font color=red><b>" + i.ToString() + "</b></font>");
			sb.Append(" ");
		}
	}
	sb.Append("</td></tr>");
	sb.Append("</table>\r\n");
	Response.Write(sb.ToString());
	return bRet;
}

void DrawTableHeader()
{
	StringBuilder sb = new StringBuilder();;
//	sb.Append("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
//	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("<td width='15%'>DELETE</td>");
	sb.Append("<td>EDIT</td>");
	sb.Append("<td width=50>M_PN</td>");
	sb.Append("<td>BRAND</td>");
	sb.Append("<td>NAME (DESCRIPTION)</td>");
//	sb.Append("<td>LAST COST</td>");	
//	sb.Append("<td width=50>cost</td>");
//	sb.Append("<td width=50>rate</td>");
//	sb.Append("<td>price</td>");
	sb.Append("<td>STOCK</td>");
//	sb.Append("<td>special</td>");
	sb.Append("</tr>\r\n");
	
	Response.Write(sb.ToString());
	Response.Flush();
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
	string id = dr["id"].ToString();
	string code = dr["code"].ToString();
	string m_pn = dr["supplier_code"].ToString();
	string name = dr["name"].ToString();
	string brand = dr["brand"].ToString();
	string stock = dr["stock"].ToString();
/*	string supplier_price = dr["supplier_price"].ToString();
	string rate = dr["rate"].ToString();
	string price = dr["price"].ToString();
	string special = dr["special"].ToString();
*/
	string index = i.ToString();

	bool bHaveStock = false;
	if(stock == "" || stock != "0")
		bHaveStock = true; //cannot delete

	StringBuilder sb = new StringBuilder();;
	
	sb.Append("<tr");
	if(alterColor)
		sb.Append(" bgcolor=#EEEEEE");
	sb.Append(">");

	sb.Append("<input type=hidden name=id");
	sb.Append(index);
	sb.Append(" value='");
	sb.Append(id);
	sb.Append("'>");

	sb.Append("<input type=hidden name=code");
	sb.Append(index);
	sb.Append(" value='");
	sb.Append(code);
	sb.Append("'>");

/*	sb.Append("<input type=hidden name=special_old");
	sb.Append(index);
	sb.Append(" value=");
	if(special != "0")
		sb.Append("on");
	sb.Append(">");

	sb.Append("<input type=hidden name=supplier_price_old" + index + " value='" + supplier_price + "'>");
	sb.Append("<input type=hidden name=price_old" + index + " value='" + price + "'>");
*/
	sb.Append("<td>");
	if(bHaveStock)
		sb.Append("<font color=red>Have Stock</font>");
	else
		sb.Append("<a href=dp.aspx?" + code + " class=o>DEL</a> ");
	sb.Append("</td>");
	sb.Append("<td><a href=liveedit.aspx?code=" + code + " class=o>EDIT</a> </td>");
	sb.Append("<td>" + m_pn + "</td>");
	sb.Append("<td>" + brand + "</td>");
	sb.Append("<td>" + name + "</td>");
//	sb.Append("<td>" + dr["supplier_price"].ToString() + "</td>");
/*
	sb.Append("<td>");
	sb.Append("<input type=text size=5 readonly=true name=supplier_price" + index + " value='" + supplier_price + "'>");
	sb.Append("</td>");

	sb.Append("<td><input type=hidden name=rate" + index + " value=" + rate + ">" + rate + "</td>");

	sb.Append("<td>");
	if(m_editPriceBox == code)
	{
		sb.Append("<input type=text size=5 name=price");
		sb.Append(index);
		sb.Append(" value='");
		sb.Append(price);
		sb.Append("'>");
	}
	else
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&p=" + page + "&ep=" + code + ">");
		sb.Append(price);
		sb.Append("</a>");
	}
	sb.Append("</td>");
*/
	sb.Append("<td><input type=hidden name=stock_old" + index + " value='" + stock + "'>"); // + stock);
	sb.Append(""+ GetAllBranchStock(code) +"");
//	sb.Append("<input type=text size=5 readonly=true name=stock" + index + " value='");
//	if(stock != null && stock != "")
//		sb.Append(stock);
//	else
//		sb.Append("Yes");
//	sb.Append("'>");
	sb.Append("</td>");
/*
	sb.Append("<td><input type=checkbox name=special");
	sb.Append(index);
	if(special != "0")
		sb.Append(" checked");
	sb.Append("></td>");
*/
	sb.Append("</tr>");

	Response.Write(sb.ToString());
	Response.Flush();
	return true;
}

bool PrintCatalogSelection()
{
	int rows = 0;
	string sc = "SELECT DISTINCT cat FROM code_relations ";
	if(m_supplier != null)
		sc += " WHERE supplier='" + m_supplier + "' ";
	sc += " ORDER BY cat";
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

//	Response.Write("<form action=eprice.aspx?d=" + m_supplier + "&r=" + DateTime.Now.ToOADate() + " method=Get>");
	Response.Write("Select Catalog : <select name=c");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?po=" + (m_bPhasedOut ? "1" : "0").ToString() + "&d=" + m_supplier + "&r=" + DateTime.Now.ToOADate() + "&c='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write("><option value=''>Show All</option>");
	string s = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		s = dr["cat"].ToString();
		Response.Write("<option value='" + s + "'");
		string lcat = cat;
		if(cat != null)
			lcat = cat.ToLower();
		if(s.ToLower() == lcat)
			Response.Write(" selected");
		Response.Write(">" + s + "</option>");
	}
	Response.Write("</select>\r\n");

	//sub catalog
	sc = "SELECT DISTINCT s_cat FROM code_relations WHERE cat='" + cat + "' ";
	if(m_supplier != null)
		sc += " WHERE supplier='" + m_supplier + "' ";
	sc += " ORDER BY s_cat";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "s_cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("<select name=s");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?po=" + (m_bPhasedOut ? "1" : "0").ToString() + "&d=" + m_supplier + "&c=" + HttpUtility.UrlEncode(cat) + "&r=" + DateTime.Now.ToOADate() + "&s='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write("><option value=''>Show All</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["s_cat"].Rows[i];
		s = dr["s_cat"].ToString();
		Response.Write("<option value='" + s + "'");
		string ls_cat = s_cat;
		if(s_cat != null)
			ls_cat = s_cat.ToLower();
		if(s.ToLower() == ls_cat)
			Response.Write(" selected");
		Response.Write(">" + s + "</option>");
	}
	Response.Write("</select>\r\n");
	
	//ss_cat
	sc = "SELECT DISTINCT ss_cat FROM code_relations WHERE cat='" + cat + "' AND s_cat='" + s_cat;
	if(m_supplier != null)
		sc += " WHERE supplier='" + m_supplier + "' ";
	sc += "' ORDER BY ss_cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "ss_cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("<select name=ss");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?po=" + (m_bPhasedOut ? "1" : "0").ToString() + "&d=" + m_supplier + "&c=" + HttpUtility.UrlEncode(cat) + "&s=" + HttpUtility.UrlEncode(s_cat) + "&r=" + DateTime.Now.ToOADate() + "&ss='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write("><option value=''>Show All</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["ss_cat"].Rows[i];
		s = dr["ss_cat"].ToString();
		Response.Write("<option value='" + s + "'");
		string lss_cat = ss_cat;
		if(ss_cat != null)
			lss_cat = ss_cat.ToLower();
		if(s.ToLower() == lss_cat)
			Response.Write(" selected");
		Response.Write(">" + s + "</option>");
	}
	Response.Write("</select>\r\n");

//	Response.Write("<input type=submit name=cmd value='List Product'></form>");
	return true;
}

string GetAllBranchStock(string code)
{
	if(dst.Tables["stocks"] != null)
		dst.Tables["stocks"].Clear();

	bool bManyBranch = false;
	if(m_branches > 1)
		bManyBranch = true;

	string sc = " ";
	if(bManyBranch) //get many branch stock
	{
		sc += " SELECT s.branch_id, isnull(s.qty, 0) AS qty FROM stock_qty s ";
		sc += " JOIN branch b ON b.id = s.branch_id AND b.activated = 1 ";
		sc += " WHERE s.code = " + code; // + " AND branch_id IN (" + sbranch + ") ";
		sc += " ORDER BY branch_id ";
	}
	else //get summary stock from all branch
	{
		sc += " SELECT 1 AS branch_id, ISNULL((SELECT SUM(s.qty) FROM stock_qty s WHERE s.code = "+ code +" )";
		sc += " ,0) as qty ";
		sc += " FROM branch b WHERE b.id = 1 AND b.activated = 1 ";
	}

//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "stocks") <= 0)
			return "";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	if(dst.Tables["branchName"] == null)
	{
		sc = " SELECT id, name FROM branch b order by id "; 	
//	DEBUG("sc=", sc);
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dst, "branchName");			
		}
		catch(Exception e) 
		{			
		}
	}
	string BranchName = "";
	string s = "";	

	if(bManyBranch)
	{
	s = "<table align=left cellspacing=1 cellpadding=2 border=1 bordercolor=#AAAAAA bgcolor=white ";
	s += " style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">";
	BranchName += s + "<tr>";
	
	for(int i=0; i<dst.Tables["branchName"].Rows.Count; i++)
	{
		for(int m=0; m<m_branches; m++)
		{
			int mBranch = MyIntParse(m_aBranch[m]);
			if(mBranch == int.Parse(dst.Tables["branchName"].Rows[i]["id"].ToString()))
			{
				BranchName += "<td align=center>"+ dst.Tables["branchName"].Rows[i]["name"].ToString() +"";		
				BranchName += "</td>";
				break;
			}
		}		
	}
	
	BranchName += "</tr>";
	}
	string[] aStock = new string[16];
	for(int i=0; i<16; i++)
		aStock[i] = "0";

	for(int i=0; i<dst.Tables["stocks"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["stocks"].Rows[i];
		int nBranch = MyIntParse(dr["branch_id"].ToString());
		string qty = dr["qty"].ToString();

		for(int m=0; m<m_branches; m++)
		{
			int mBranch = MyIntParse(m_aBranch[m]);
			if(mBranch == nBranch)
			{
				aStock[m] = qty;
				break;
			}
		}
	}
	
	s = BranchName;
	if(bManyBranch)
		s += "<tr align=center>";
	String [] aStockString = new String [m_branches];
	for(int i=0; i<m_branches; i++)
	{
		aStockString[i] = aStock[i];
		if(bManyBranch)
			s += "<td bgcolor=#EEEEEE>";
		s += aStockString[i];
		if(bManyBranch)
			s += "</td>";
	}
	if(bManyBranch)
		s += "</tr></table>";
	return s;
}
</script>
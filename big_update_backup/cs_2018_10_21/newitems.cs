<!-- #include file="price.cs" -->

<script runat=server>
bool bSaveAllButton = false;
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet ds = new DataSet();
int m_count = -1;
int m_nPageSize = 30;
const string thisurl = "newitems.aspx";
const string cols = "12";
int page = 1;
int	m_nStartPageButton = 1;
int m_nPageButtonCount = 40;

void Page_Load(Object Src, EventArgs E ) 
{
	if(Request.QueryString["showall"] == "1" || m_sCompanyName == "test")
		bSaveAllButton = true;

	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["spb"] != null)
		m_nStartPageButton = int.Parse(Request.QueryString["spb"]);

	string spage = Request.QueryString["p"];
	if(spage != null)
		page = int.Parse(spage);
	
	if(bSaveAllButton)
		m_bShowProgress = true;

	PrintAdminHeader();
	PrintJavaFunctions();
	PrintAdminMenu();
	WriteHeaders();
	Response.Flush();

	if(Request.QueryString["t"] == "save")
	{
		if(Request.Form["cmd"] == "Save" || Request.Form["cmd"] == "SaveAll")
		{
			if(DoSave())
			{	
				TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=newitems.aspx?p=" + page.ToString() + "&spb=" + m_nStartPageButton + "&r=" + DateTime.Now.ToOADate() + "\">");
				return;
			}
		}
		else if(Request.Form["cmd"] == "Skip" || Request.Form["cmd"] == "SkipAll")
		{
			if(DoSkip())
			{	
				TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=newitems.aspx?p=" + page.ToString() + "&spb=" + m_nStartPageButton + "&r=" + DateTime.Now.ToOADate() + "\">");
				return;
			}
		}
	}
	else if(Request.QueryString["t"] == "sb") //select brand to process
	{
		string sel_brand = "All Brands";
		if(Request.Form["sel_brand"] != null && Request.Form["sel_brand"] != "")
			sel_brand = Request.Form["sel_brand"];
		
		if(Application["pni_brand_" + sel_brand] != null)
		{
			//check if user is online
			string dev = Application["pni_brand_" + sel_brand].ToString();
			if(!IsUserOnline(dev))
			{
				Application["pni_brand_" + sel_brand] = null;
//				return;
			}
		}

		if(Application["pni_brand_" + sel_brand] != null)
		{
			string dev = Application["pni_brand_" + sel_brand].ToString();
			if(!IsUserOnline(dev))
				Application["pni_brand_" + sel_brand] = null;
			if(dev != Session["name"].ToString())
			{
				Response.Write("<font size=+2 color=red><b>Error, " + dev);
				Response.Write(" is working on " + sel_brand + "</b></font>");
			}
			else
			{
				if(Session["pni_brand"] != null)
				{
					string old_sel = Session["pni_brand"].ToString();
					Application["pni_brand_" + old_sel] = null;
				}
				Session["pni_brand"] = sel_brand;
				Application["pni_brand_" + sel_brand] = Session["name"].ToString();
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=newitems.aspx?r=" + DateTime.Now.ToOADate() + "\">");
				return;
			}
		}
		else
		{
			if(Session["pni_brand"] != null)
			{
				string old_sel = Session["pni_brand"].ToString();
//				string developer = Application["pni_brand_" + old_sel].ToString();
//				if(developer == Session["name"].ToString())
				Application["pni_brand_" + old_sel] = null;
			}
			Session["pni_brand"] = sel_brand;
			Application["pni_brand_" + sel_brand] = Session["name"].ToString();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=newitems.aspx?r=" + DateTime.Now.ToOADate() + "\">");
			return;
		}
	}

	if(!IsPostBack)
		BindGrid();
	PrintAdminFooter();
}

bool IsUserOnline(string name)
{
	DataSet dsws = new DataSet();
	string sc = "SELECT site FROM web_session WHERE name='" + name + "'";
	try
	{
		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsws) > 0)
		{
			string site = dsws.Tables[0].Rows[0]["site"].ToString();
//DEBUG("site=", site);
			if(site == m_sCompanyName + "admin")
				return true;
		}
		else
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.form.elements.length;i++) 
		{
			var e = document.form.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form.allbox.checked;
		}
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
}

bool DoSkip()
{
	dst.Clear();
	int rows = 0;

	bool bSkipAll = false;
	if(Request.Form["cmd"] != null)
	{
		if(Request.Form["cmd"] == "SkipAll")
		{
			bSkipAll = true;
			Response.Write("<h3>Skipping All, Wait ... </h3>");
			Response.Flush();
		}
	}
//	int page = int.Parse(Request.Form["page"]);
//DEBUG("page=", page);

	string sel_brand = "All Brands";
	if(Session["pni_brand"] != null)
		sel_brand = Session["pni_brand"].ToString();
	
	string sc = "SELECT c.rrp, c.code, c.id, c.brand, c.name, c.cat, c.s_cat, c.ss_cat, c.supplier, c.supplier_code, c.hot, c.rate, ";
	sc += "p.stock, p.supplier_price, p.price, p.eta, p.details FROM code_relations_new" + m_catTableString;
	sc += " c JOIN product_new" + m_catTableString + " p ON c.id=p.id ";
	if(sel_brand != "All Brands")
		sc += "WHERE c.brand='" + sel_brand + "' ";
//	sc += "ORDER BY c.id";
	sc += "ORDER BY c.brand, c.cat, c.s_cat, c.ss_cat";

//return true;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "skip");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int start = (page-1) * m_nPageSize;
	DataRow dr = null;
	if(bSkipAll)
	{
		Response.Write("processing...busy...");
		Response.Flush();
		for(int i=0; i<dst.Tables["skip"].Rows.Count;i++)
		{
			dr = dst.Tables["skip"].Rows[i];
			if(!SkipOne(dr))
				return false;
Thread.Sleep(1);
			MonitorProcess(100);
		}
		Response.Write("..done");
		Response.Flush();
	}
	else
	{
		for(int i=start; i<start+m_nPageSize;i++)
		{
			if(i>=dst.Tables["skip"].Rows.Count)
				break;
			if(Request.Form["save"+i.ToString()] != "on")
				continue;
			dr = dst.Tables["skip"].Rows[i];
			if(!SkipOne(dr))
				return false;
		}
	}
	return true;
}

bool SkipOne(DataRow dr)
{
	Boolean bRet = true;

	string id = dr["id"].ToString();
	string code = dr["code"].ToString();
	string brand = dr["brand"].ToString();
	string name = dr["name"].ToString();
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	string ss_cat = dr["ss_cat"].ToString();
	string stock = dr["stock"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string supplier_price = dr["supplier_price"].ToString();
	string price = dr["price"].ToString();
	string hot = dr["hot"].ToString();
	string eta = dr["eta"].ToString();
	string details = dr["details"].ToString();
	string rate = dr["rate"].ToString();

	hot = "0"; //default not hot
//	if(hot == null)
//		hot = "0";
//	else
//		hot = "1";

	if(name.Length > 254)
		name = name.Substring(0, 254);
	if(cat.Length > 49)
		cat = cat.Substring(0, 49);
	if(s_cat.Length > 49)
		s_cat = s_cat.Substring(0, 49);
	if(ss_cat.Length > 49)
		ss_cat = ss_cat.Substring(0, 49);

	//insert code_relations
	string sc = "INSERT INTO code_relations (id, supplier, supplier_code, code, name, brand, cat, s_cat, ss_cat, hot, rate, skip)";
	sc += "VALUES('";
	sc += id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += code;
	sc += ", '";
	sc += name;
	sc += "', '";
	sc += brand;
	sc += "', '";
	sc += cat;
	sc += "', '";
	sc += s_cat; 
	sc += "', '";
	sc += ss_cat;
	sc += "', ";
	sc += hot;
	sc += ", ";
	sc += rate;
	sc += ", 1)";

	sc += " INSERT INTO product_skip (id, stock, eta, supplier_price, price, details) ";
	sc += " VALUES('";
	sc += id;
	sc += "', ";
	sc += stock;
	sc += ", '";
	sc += eta;
	sc += "', ";
	sc += supplier_price;
	sc += ", ";
	sc += price;
	sc += ", '";
	sc += details;
	sc += "')";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	//delete from produc_new
	StringBuilder sb = new StringBuilder();
	sb.Append("DELETE FROM product_new" + m_catTableString + " WHERE id='");
	sb.Append(id);
	sb.Append("'");
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

	//delete from code_relations_new
	sb.Remove(0, sb.Length);
	sb.Append("DELETE FROM code_relations_new" + m_catTableString + " WHERE id='");
	sb.Append(id);
	sb.Append("'");
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	WritePriceHistory(code, double.Parse(price, NumberStyles.Currency, null));
	return true;
}

bool DoSave()
{
	dst.Clear();
	int rows = 0;

	bool bSaveAll = false;
	if(Request.Form["cmd"] != null)
	{
		if(Request.Form["cmd"] == "SaveAll")
		{
			bSaveAll = true;
			Response.Write("<h3>Saving All, Wait ... </h3>");
			Response.Flush();
		}
	}
//	int page = int.Parse(Request.Form["page"]);
//DEBUG("page=", page);

	string sel_brand = "All Brands";
	if(Session["pni_brand"] != null)
		sel_brand = Session["pni_brand"].ToString();
	
	string sc = "SELECT c.code, c.id, c.brand, c.name, c.cat, c.s_cat, c.ss_cat, c.supplier, c.supplier_code, c.hot, c.rate, c.rrp, ";
	sc += "p.stock, p.supplier_price, p.price, p.eta, p.details FROM code_relations_new" + m_catTableString;
	sc += " c JOIN product_new" + m_catTableString + " p ON c.id=p.id ";
	if(sel_brand != "All Brands")
		sc += "WHERE c.brand='" + sel_brand + "' ";
//	sc += "ORDER BY c.id";
	sc += "ORDER BY c.brand, c.cat, c.s_cat, c.ss_cat";

//return true;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "save");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(bSaveAll)
	{
		Response.Write("processing...busy...");
		Response.Flush();
		DataRow dr = null;
		for(int i=0; i<dst.Tables["save"].Rows.Count;i++)
		{
			dr = dst.Tables["save"].Rows[i];

//if(dr["brand"].ToString() != "CISCO")			
//	continue;

//if(int.Parse(dr["code"].ToString()) > 143685)
//	break;
			
			
			if(!UpdateOne(dr))
				return false;
Thread.Sleep(1);
			MonitorProcess(100);
		}
		Response.Write("..done");
		Response.Flush();
	}
	else
	{
		int start = (page-1) * m_nPageSize;
		DataRow dr = null;
		for(int i=start; i<start+m_nPageSize;i++)
		{
			if(i>=dst.Tables["save"].Rows.Count)
				break;
			if(Request.Form["save"+i.ToString()] != "on")
				continue;
			dr = dst.Tables["save"].Rows[i];
			if(!UpdateOne(dr))
				return false;
		}
	}
	return true;
}

Boolean UpdateOne(DataRow dr)
{
	Boolean bRet = true;

	string id = dr["id"].ToString();
	string code = dr["code"].ToString();
	string brand = dr["brand"].ToString();
	string name = dr["name"].ToString();
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	string ss_cat = dr["ss_cat"].ToString();
	string stock = dr["stock"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string supplier_price = dr["supplier_price"].ToString();
	string price = dr["price"].ToString();
	string hot = dr["hot"].ToString();
//	string skip = dr["skip"].ToString();
	string eta = dr["eta"].ToString();
	string details = dr["details"].ToString();
	string rate = dr["rate"].ToString();
	string rrp = dr["rrp"].ToString();

	hot = "0"; //default not hot
//	if(hot == null)
//		hot = "0";
//	else
//		hot = "1";

	//insert code_relations
	string sc = "IF NOT EXISTS (SELECT code FROM code_relations WHERE id='" + id + "') ";
	sc += " INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, code, name, brand, cat, s_cat, ss_cat, hot, rate";
	sc += ", manual_cost_nzd, manual_cost_frd, rrp)";
	sc += "VALUES('";
	sc += id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += supplier_price;
	sc += ", ";
	sc += code;
	sc += ", '";
	sc += name;
	sc += "', '";
	sc += brand;
	sc += "', '";
	sc += cat;
	sc += "', '";
	sc += s_cat; 
	sc += "', '";
	sc += ss_cat;
	sc += "', ";
	sc += hot;
	sc += ", ";
	sc += rate;
	sc += ", "+ supplier_price +", "+ supplier_price +", "+ rrp +"";

	sc += ")";
	sc += " ELSE UPDATE code_relations SET supplier_price=" + supplier_price + ", code=" + code;
	sc += ", name='" + name + "', brand='" + brand + "', cat='" + cat + "', s_cat='" + s_cat;
	sc += "', ss_cat='" + ss_cat + "', hot=" + hot + ", rate=" + rate + " WHERE id='" + id + "' ";

	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "IF NOT EXISTS (SELECT code FROM product WHERE supplier+supplier_code='" + id + "') ";
	sc += "INSERT INTO product (supplier, supplier_code, code, name, brand, ";
	sc += " cat, s_cat, ss_cat, hot, stock, supplier_price, price, eta, price_dropped, price_age) VALUES('";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += code;
	sc += ", '";
	sc += name;
	sc += "', '";
	sc += brand;
	sc += "', '";
	sc += cat;
	sc += "', '";
	sc += s_cat; 
	sc += "', '";
	sc += ss_cat;
	sc += "', ";
	sc += hot;
	sc += ", ";
	sc += stock;
	sc += ", ";
	sc += supplier_price;
	sc += ", ";
	sc += price;
	sc += ", '";
	sc += eta;
	sc += "', 0";
	sc += ", ";
	sc += "GETDATE()";
	sc += ")";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(details != "")
	{
		//update product spec
		sc = "INSERT INTO product_details (code, spec) VALUES(";
		sc += code;
		sc += ", '";
		sc += details;
		sc += "')";
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	sc = "DELETE FROM code_relations_new" + m_catTableString + " WHERE id='";
	sc += id;
	sc += "'";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	sc = "DELETE FROM product_raw" + m_catTableString + " WHERE id='";
	sc += id;
	sc += "'";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	WritePriceHistory(code, double.Parse(price, NumberStyles.Currency, null));
	return true;
}

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<br><center><h3>New Products List</h3></center>");
	Response.Write(sb.ToString());
	Response.Flush();
}

/////////////////////////////////////////////////////////////////
bool BindGrid()
{
	string sel_brand = "All Brands";
	if(Session["pni_brand"] != null)
		sel_brand = Session["pni_brand"].ToString();

	ds.Clear();
	string sc = "SELECT c.code, c.id, c.brand, c.name, c.cat, c.s_cat, c.ss_cat, ";
	sc += "p.stock, p.eta, p.price FROM code_relations_new" + m_catTableString;
	sc += " c JOIN product_new" + m_catTableString + " p ON c.id=p.id ";
	if(sel_brand != "All Brands")
		sc += "WHERE c.brand='" + sel_brand + "' ";
//	sc += "ORDER BY c.id";
	sc += "ORDER BY c.brand, c.cat, c.s_cat, c.ss_cat";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_count = myCommand.Fill(ds, "product");
//DEBUG("m_count=", m_count);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return MyDrawTable();
}

void PrintPageIndex()
{
	Response.Write("<tr><td colspan=" + cols + ">Page: ");
	int pages = ds.Tables["product"].Rows.Count / m_nPageSize + 1;
	int i=m_nStartPageButton;
	if(m_nStartPageButton > m_nPageButtonCount)
	{
		Response.Write("<a href=newitems.aspx?p=");
		Response.Write((i - m_nPageButtonCount).ToString());
		Response.Write("&spb=");
		Response.Write((i - m_nPageButtonCount).ToString());
		Response.Write(">...</a> ");
	}
	for(;i<m_nStartPageButton + m_nPageButtonCount; i++)
	{
		if(i > pages)
			break;
		if(i != page)
		{
			Response.Write("<a href=newitems.aspx?p=");
			Response.Write(i.ToString());
			Response.Write(">");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font size=24><b>"+i.ToString()+"</b></font>");
			Response.Write(" ");
		}
	}
	if(i<pages)
	{
		Response.Write("<a href=newitems.aspx?p=");
		Response.Write(i.ToString());
		Response.Write("&spb=");
		Response.Write(i.ToString());
		Response.Write(">...</a> ");
	}
	Response.Write(" Total <font color=red>" + pages.ToString() + "</font> Pages</td></tr>");	
}

bool PrintBrandSelection()
{
	int rows = 0;
	string sc = "SELECT DISTINCT brand FROM code_relations_new" + m_catTableString + " ORDER BY brand";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "brands");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
		return true;

	string sel_brand = "All Brands";
	if(Session["pni_brand"] != null)
		sel_brand = Session["pni_brand"].ToString();

	StringBuilder sb = new StringBuilder();

	Response.Write("\r\n<form action=newitems.aspx?p=" + page.ToString() + "&spb=" + m_nStartPageButton + "&t=sb&r=");
	Response.Write(DateTime.Now.ToOADate() + " method=post name=sel_form>");
	Response.Write("\r\nProcess this brand only : <select name=sel_brand>");
	Response.Write("<option value='All Brands'");
	if(sel_brand == "All Brands")
		Response.Write(" selected");
	Response.Write(">All Brands</option>");
	if(Application["pni_brand_All Brands"] != null)
	{
		string dev = Application["pni_brand_All Brands"].ToString();
		string nick = "";
		for(int n=0; n<dev.Length; n++)
		{
			if(dev[n] == ' ')
				break;
			nick += dev[n];
		}
		sb.Append("<br><font size=+1 color=red><b>" + nick + " On All Brands</b></font>");
	}
	
	for(int i=0; i<rows; i++)
	{
		string brand = ds.Tables["brands"].Rows[i]["brand"].ToString();
		Response.Write("<option value='" + brand + "'");
		if(brand == sel_brand)
			Response.Write(" selected");
		Response.Write(">" + brand + "</option>");
		if(Application["pni_brand_" + brand] != null)
		{
			string dev = Application["pni_brand_" + brand].ToString();
			string nick = "";
			for(int n=0; n<dev.Length; n++)
			{
				if(dev[n] == ' ')
					break;
				nick += dev[n];
			}
			sb.Append("<br><font size=+1 color=red><b>" + nick + " On " + brand + "</b></font>");
		}
	}
	Response.Write("</select><input type=submit name=cmd value=' OK '></form>\r\n");
	Response.Write(sb.ToString());
	return true;
}

bool MyDrawTable()
{
	DrawTableHeader();
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (page-1) * m_nPageSize;
//DEBUG("startPage=", startPage);
	for(int i=startPage; i<ds.Tables["product"].Rows.Count; i++)
//	for(int i=0; i<2; i++)
	{
//DEBUG("i=", i);
		if(i-startPage >= m_nPageSize)
			break;
		dr = ds.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
			return false;
	}

	Response.Write("<tr><td colspan=" + cols + " align=right>Select All<input type=checkbox name=allbox value='Select All' onClick='CheckAll();'></td></tr>");
	Response.Write("<tr><td colspan=5>");
	Response.Write("<input type=submit name=cmd value='Skip'> ( skip selected items )");
	Response.Write("<td colspan=" + (int.Parse(cols) - 5).ToString() + " align=right>");
	Response.Write("<input type=submit name=cmd value='Save'>");
	if(bSaveAllButton)
	{
		Response.Write("&nbsp;&nbsp;&nbsp;<input type=submit name=cmd value='SaveAll'>");
		Response.Write("&nbsp;&nbsp;&nbsp;<input type=submit name=cmd value='SkipAll'>");
	}
	else
		Response.Write("</td></tr><tr><td colspan=12 align=right><a href=newitems.aspx?showall=1 class=o>Show SkipSaveAll Button</a>");
	Response.Write("</td></tr>");
	PrintPageIndex();
	Response.Write("</table></form>\r\n");

	return PrintBrandSelection();
}

void DrawTableHeader()
{
	Response.Write("<form action=newitems.aspx?p=" + page.ToString() + "&spb=" + m_nStartPageButton + "&t=save&r=");
	Response.Write(DateTime.Now.ToOADate() + " method=post name=form>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=1 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>EDIT</td>");
	Response.Write("<td>CODE</td>");
	Response.Write("<td>ID</td>");
	Response.Write("<td>BRAND</td>");
	Response.Write("<td>NAME</td>");
	Response.Write("<td>CAT</td>");
	Response.Write("<td>S_CAT</td>");
	Response.Write("<td>SS_CAT</td>");
	Response.Write("<td>STOCK</td>");
	Response.Write("<td>ETA</td>");
	Response.Write("<td>RRP PRICE</td>");
	Response.Write("<td>SAVE</td>");
	Response.Write("</tr>\r\n");
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
	string code = dr["code"].ToString();
	string id = dr["id"].ToString();
	string name = dr["name"].ToString();
	string brand = dr["brand"].ToString();
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	string ss_cat = dr["ss_cat"].ToString();
	string stock = dr["stock"].ToString();
	string eta = dr["eta"].ToString();
	string price = dr["price"].ToString();
	string index = i.ToString();

//	if(!CheckCodeRelations(id, dr)) //if id is blank, then delete this from product table
//		return false;

//	StringBuilder sb = new StringBuilder();

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write("><td>");
	Response.Write("<a href=eni.aspx?id=" + HttpUtility.UrlEncode(id) + " target=_blank>EDIT</a></td><td>");
	Response.Write(code);
	Response.Write("</td><td>");
	Response.Write(id);
	Response.Write("</td><td>");
	Response.Write(brand);
	Response.Write("</td><td>");
	Response.Write(name);
	Response.Write("</td><td>");
	Response.Write(cat);
	Response.Write("</td><td>");
	Response.Write(s_cat);
	Response.Write("</td><td>");
	Response.Write(ss_cat);
	Response.Write("</td><td>");
	Response.Write(stock);
	Response.Write("</td><td>");
	Response.Write(eta);
	Response.Write("</td><td>");
	Response.Write(price);
	Response.Write("</td>");

	Response.Write("<td align=right><input type=checkbox name=save");
	Response.Write(index);
	Response.Write(">");
	
	Response.Write("</td></tr>");

//	Response.Write(sb.ToString());
//	Response.Flush();
	return true;
}

</script>

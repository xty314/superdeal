<!-- #include file="chart.cs" -->
<script runat=server>

string m_sAdminFooter1 = "";
string m_mainTitleIndex = "s_cat";

DataSet ds = new DataSet();
string mainTitleIndex = "s_cat";
string subTableIndex = "brand";
int rows_return = 0;

string brand = null;
string cat = null;
string s_cat = null;
string ss_cat = null;

//decode "zzzOthers"
string dbrand = null;
string dcat = null;
string ds_cat = null;
string dss_cat = null;

//encoded space
string ebrand = null;
string ecat = null;
string es_cat = null;
string ess_cat = null;

//string m_sWhereNoHot = "";

bool bShowSpecial = false;
bool bIncludeGST = false;
bool bShowAllStock = false;

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	InitializeData(); //init functions

	RememberLastPage();
	PrintHeaderAndMenu();
	if(!DoSearch()) //return true mean cache written
		PrintBody();
	PrintFooter();
}

bool DoSearch()
{
	if(Session["display_include_gst"] != null)
		bIncludeGST = true;
	if(Session["display_show_all_stock"] != null)
		bShowAllStock = true;

	if(Request.QueryString["b"] == null && Request.QueryString["c"] == null 
		&& Request.QueryString["s"] == null && Request.QueryString["ss"] == null)
	{
		bShowSpecial = true;
	}
	else
	{
		brand = Request.QueryString["b"];
		cat = Request.QueryString["c"];
		s_cat = Request.QueryString["s"];
		ss_cat = Request.QueryString["ss"];

		if(!CheckSQLAttack(brand))
			return false;
		if(!CheckSQLAttack(cat))
			return false;
		if(!CheckSQLAttack(s_cat))
			return false;
		if(!CheckSQLAttack(ss_cat))
			return false;
		
		dbrand = brand;
		dcat = cat;
		ds_cat = s_cat;
		dss_cat = ss_cat;
		if(brand == "zzzOthers")
			dbrand = "";
		if(cat == "zzzOthers")
			dcat = "";
		if(s_cat == "zzzOthers")
			ds_cat = "";
		if(ss_cat == "zzzOthers")
			dss_cat = "";

		ebrand = HttpUtility.UrlEncode(dbrand);
		ecat = HttpUtility.UrlEncode(dcat);
		es_cat = HttpUtility.UrlEncode(ds_cat);
		ess_cat = HttpUtility.UrlEncode(dss_cat);
	}

	if(brand != null)
		m_mainTitleIndex = "brand";
	
	string sca = "12345"; //try again if no hot product later
	string sWhere = "";
	string sc = "";
	bool bHotOnly = false;
	if(bShowSpecial)
	{
		sc = "SELECT p.code, p.name, p.brand, p.price";
		if(bIncludeGST)
			sc += "*1.125 AS price";
		sc += ", ISNULL(STR(p.stock), 'Yes') AS stock, p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped ";
		sc += ", c.supplier_code, c.barcode, c.moq, c.price1 ";
		sc += "FROM product p JOIN specials s ON p.code=s.code ";
		sc += " JOIN code_relations c ON c.code = p.code ";
		if(m_supplierString != "")
			sc += " WHERE supplier IN" + m_supplierString;
		sc += " ORDER BY p.brand, p.cat, p.s_cat, p.ss_cat";
	}
	else
	{
		sc = "SELECT p.code, p.name, p.brand, p.price";
		if(bIncludeGST)
			sc += "*1.125 AS price";
		sc += ", ISNULL(STR(stock), 'Yes') AS stock, p.cat, p.s_cat, p.ss_cat, p.eta, p.price_dropped ";
		sc += ", c.supplier_code, c.barcode, c.moq, c.price1 ";
		sc += " FROM product p ";
		sc += " JOIN code_relations c ON c.code = p.code ";
		sc += " WHERE";
		if(!bShowAllStock)
			sc += " (stock>0 OR stock IS NULL) AND ";
		if(brand != null)
		{
			sWhere += " p.brand='";
			sWhere += dbrand;
			sWhere += "'";
			mainTitleIndex = "brand";
			subTableIndex = "ss_cat";
		}
		
		if(cat != null)
		{
			if(sWhere != "")
				sWhere += " AND";
			sWhere += " p.cat='";
			sWhere += dcat;
			sWhere += "'";
		}

		if(s_cat != null)
		{
			if(sWhere != "")
				sWhere += " AND";
			sWhere += " p.s_cat='";
			sWhere += ds_cat;
			sWhere += "'";
		}
		if(ss_cat != null)
		{
			if(sWhere != "")
				sWhere += " AND";
			sWhere += " p.ss_cat='";
			sWhere += dss_cat;
			sWhere += "'";
		}

		sc += sWhere;
		if(m_supplierString != "")
			sc += " AND p.supplier IN" + m_supplierString + " ";
		sca = sc;
		sc += " ORDER BY p.brand, p.s_cat, p.ss_cat, p.name, p.code";
	}
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows_return = myCommand.Fill(ds, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return true;
	}
//DEBUG("rows=", rows_return);

	string scWhere = "";
	int cross = 0;
	if(!bShowSpecial && brand == null)
	{
		sc = "SELECT code FROM cat_cross WHERE";
		if(cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " cat='";
			scWhere += dcat;
			scWhere += "'";
		}
		if(s_cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " s_cat='";
			scWhere += ds_cat;
			scWhere += "'";
		}
		if(ss_cat != null)
		{
			if(scWhere != "")
				scWhere += " AND";
			scWhere += " ss_cat='";
			scWhere += dss_cat;
			scWhere += "'";
		}
		sc += scWhere;
//DEBUG("sc=", sc);
		if(scWhere != "")
		{
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				cross = myCommand.Fill(ds, "cross");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return true;
			}
			if(cross > 0)
			{
				for(int c=0; c<cross; c++)
				{
					string ccode = ds.Tables["cross"].Rows[c]["code"].ToString();
					sc = " SELECT p.code, p.name, p.brand, p.price, p.stock, p.cat, p.s_cat, p.ss_cat, p.eta ";
					sc += ", c.supplier_code, c.barcode, c.moq, c.price1 ";
					sc += " FROM product p ";
					sc += " JOIN code_relations c ON c.code = p.code ";
					sc += " WHERE p.code = " + ccode;
					if(m_supplierString != "")
						sc += " AND p.supplier IN" + m_supplierString + " ";
					if(bHotOnly)
						sc += " AND p.hot=1";
					try
					{
						SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
						rows_return += myCommand.Fill(ds, "product");
					}
					catch(Exception e) 
					{
						ShowExp(sc, e);
						return true;
					}
				}
			}
		}
	}		

	if(rows_return <= 0)
	{
//DEBUG("sca=", sca);
		if(sca.Substring(sca.Length-5, 5) == "hot=1") //try display all
		{
			sc = "SELECT TOP 10 ";
			sc += sca.Substring(7, sca.Length-17);
			sc += " ORDER BY brand, s_cat, ss_cat, name, code";
			try
			{
				SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
				rows_return = myCommand.Fill(ds, "product");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return true;
			}
			
			if(rows_return <= 0)
				return true;
//			bSayHot = false; //don't display word " - hot product"
		}
		else
			return true;
	}
	return false;
}

void PrintBody()
{
	if(ds.Tables["product"] == null)
		return;

	int count = ds.Tables["product"].Rows.Count;
	if(count <=0 )
	{
		return;
	}

	bool bNeedPrefix = false;
	string tspath = GetRootPath();
	tspath += "/pi/";
//	string tspath = TSGetPath().ToLower();
//DEBUG("tspath=", tspath);
//	if(tspath == m_sCompanyName || tspath == m_sCompanyName + "admin" || tspath == m_sCompanyName + "/admin" )
//		bNeedPrefix = true;
	Response.Write("<center><h3><font color=red>");
	Response.Write(ds.Tables["product"].Rows[0][m_mainTitleIndex].ToString());
	Response.Write(" Show Room</font></h3>");

	Response.Write("<table width=80% align=center>");
	Response.Write("<tr>");
	//print pic
	int cols = 0;
	int show = 0;
	for(int i=0; i<count; i++)
	{
		DataRow dr = ds.Tables["product"].Rows[i];
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		double dPrice = MyDoubleParse(dr["price1"].ToString());
		string mpn = dr["supplier_code"].ToString();
		string exh = GetItemLocation(code);
		string moq = dr["moq"].ToString();
		
//		string fn = "/";
//		if(bNeedPrefix || m_supplierString != "")
//			fn += m_sCompanyName + "/";
		string fn = tspath + code;
		string sPicFile = fn + ".gif";
		string rp = Server.MapPath(fn);
		if(!File.Exists(rp))
		{
			sPicFile = fn + ".jpg";
			rp = Server.MapPath(sPicFile);
			if(!File.Exists(rp))
			{
				sPicFile = fn + ".gif";
				rp = Server.MapPath(sPicFile);
				if(!File.Exists(rp))
//DEBUG("no pic", " code="+code);
					continue;
			}
		}
		show++;
		Response.Write("<td>");
		Response.Write("<table>");
		Response.Write("<tr><td>");
//		Response.Write("<a href=" + sPicFile + ">");
		Response.Write("<a href=p.aspx?" + code + ">");
		Response.Write("<img src=" + sPicFile + " border=0");

		System.Drawing.Image im = System.Drawing.Image.FromFile(rp);
//DEBUG("width=", im.Width);
		int iWidth = im.Width;
		if(im.Width > 200)
			Response.Write(" width=200 title='Click For Large Image'");
		im.Dispose();

		Response.Write("></a></td></tr>");
		Response.Write("<tr><td>");
		
		Response.Write("<table>");
		Response.Write("<tr><td colspan=2><a href=p.aspx?" + code + ">" + dr["name"].ToString() + "</a></td></tr>");
		Response.Write("<tr><td>M_PN:</td><td>" + mpn + "</td></tr>");
		Response.Write("<tr><td>Barcode:</td><td>" + barcode + "</td></tr>");
		Response.Write("<tr><td>EXH:</td><td>" + exh + "</td></tr>");
		Response.Write("<tr><td>MOQ:</td><td>" + moq + "</td></tr>");
		Response.Write("<tr><td>Price:</td><td>" + dPrice.ToString("c") + "</td></tr>");
		Response.Write("</table>");

		Response.Write("</td></tr>");
		Response.Write("</table>");
		Response.Write("</td>");
		cols++;
		if(cols >= 3)
		{
			Response.Write("</tr><tr><td>&nbsp;</td></tr>");
			cols = 0;
		}
	}
	Response.Write("</tr></table>");
	if(show == 0)
		Response.Write("<b>SORRY, CURRENTLY NO IMAGES AVAILABLE IN THIS CATALOG</b>");
}

</script>


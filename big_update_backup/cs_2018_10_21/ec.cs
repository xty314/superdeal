<!-- #include file="catalog.cs" -->

<script runat=server>
// ec.cs Update Catalog, get all catalogs from product and code_relations table, then edit,
// save changes, and refresh catalog table

DataSet dsc = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
//DataRow[] drs;	//for sorting

string m_action;
string m_show;
string m_q;
string m_c;
string m_s;
string m_ss;
string m_co = "-1";
string m_so = "-1";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	GetQueryStrings();
	if(m_action == "e") //edit
	{
		DoEdit();	
	}
	else if(m_action == "tb") //toggle brand show
	{
		if(ToggleBrandShow())
		{
			if(!GetAvailableCatalogs())
				return;
//			GetTempCatalogTable();
			DrawCatalogTable();	
		}
	}
	else if(m_action == "s") //save
	{
//		DEBUG("cmd=", Request.Form["cmd"]);	
		if(DoSave())
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("ec.aspx?a=e&t=c&q=");
			sb.Append(HttpUtility.UrlEncode(m_q));
			sb.Append("&c=");
			sb.Append(HttpUtility.UrlEncode(m_c));
			sb.Append("&s=");
			sb.Append(HttpUtility.UrlEncode(m_s));
			sb.Append("&ss=");
			sb.Append(HttpUtility.UrlEncode(m_ss));
			sb.Append("&r=" + DateTime.Now.ToOADate());
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + sb.ToString() + "\">");
		}
	}
	else if(m_action == "u") //update
	{
		if(!DoUpdate())
		{
			Response.Write("Error update live catalog.");
		}
		else
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=ec.aspx\">");
		}
	}
	else
	{
		if(EmptyTempTables())
		{
			if(GetAvailableCatalogs())
			{
//				if(SaveTempCatalogTable())
//					if(GetTempCatalogTable()) //this is for sorting
						DrawCatalogTable();
			}
		}
	}

	PrintAdminFooter();
}

void GetQueryStrings()
{
	m_action = Request.QueryString["a"];
	m_show = Request.QueryString["sh"];
	m_q = Request.QueryString["q"];
	m_c = Request.QueryString["c"];
	m_s = Request.QueryString["s"];
	m_ss = Request.QueryString["ss"];
	if(Request.QueryString["co"] != null)
		m_co = Request.QueryString["co"];
	if(Request.QueryString["so"] != null)
		m_so = Request.QueryString["so"];
}

bool DoEdit()
{
	if(!ECATGetAllExistsValues("brand", "brand<>'-1' ORDER BY brand"))
		return false;

	if(!ECATGetAllExistsValues("cat", "cat<>'Brands' ORDER BY cat"))
		return false;

	string sc = "";
	if(m_co == "-1")
		sc = "cat='" + EncodeQuote(m_c) + "' ORDER BY s_cat";
	else
		sc = "cat='" + EncodeQuote(m_co) + "' ORDER BY s_cat";
	if(!ECATGetAllExistsValues("s_cat", sc, false))
		return false;

	if(m_co == "-1")
	{
		if(m_so == "-1")
			sc = "cat='" + EncodeQuote(m_c) + "' AND s_cat='" + EncodeQuote(m_s) + "' ORDER BY ss_cat";
		else
			sc = "cat='" + EncodeQuote(m_c) + "' AND s_cat='" + EncodeQuote(m_so) + "' ORDER BY ss_cat";
	}
	else
	{
		if(m_so == "-1")
			sc = "cat='" + EncodeQuote(m_co) + "' AND s_cat='" + EncodeQuote(m_s) + "' ORDER BY ss_cat";
		else
			sc = "cat='" + EncodeQuote(m_co) + "' AND s_cat='" + EncodeQuote(m_so) + "' ORDER BY ss_cat";
	}
//DEBUG("sc=", sc);
	if(!ECATGetAllExistsValues("ss_cat", sc, false))
		return false;

	Response.Write("<form action=ec.aspx?a=s method=post>");
	Response.Write("<input type=hidden name=q value='");
	Response.Write(m_q);
	Response.Write("'>");
	Response.Write("<input type=hidden name=c value='");
	Response.Write(m_c);
	Response.Write("'>");
	Response.Write("<input type=hidden name=s value='");
	Response.Write(m_s);
	Response.Write("'>");
	Response.Write("<input type=hidden name=ss value='");
	Response.Write(m_ss);
	Response.Write("'>");
	Response.Write("<table cellspacing=0 cellpadding=0 align=Center rules=all bgcolor=white \r\n");
	Response.Write("bordercolor=#FFFFFF border=1 width=90% style=\"font-family:Verdana);font-size:8pt);\r\n");
	Response.Write("border-collapse:collapse);\">\r\n");
	Response.Write("<tr style=\"color:red;background-color:#CCCCCC;font-weight:bold;\">\r\n");
	Response.Write("<td>Name</td>\r\n");
	Response.Write("<td>Old Value</td>\r\n");
	Response.Write("<td>New Value</td>\r\n");
	Response.Write("</tr>\r\n\r\n");
	
	Response.Write("<tr><td>seq</td><td>");
	Response.Write(m_q);
	Response.Write("</td><td><input type=text name=seq size=10 value='");
	Response.Write(m_q);
	Response.Write("'></td></tr>");

	bool bIsBrand = false;
	if(String.Compare(m_c, "brands", true) == 0)
		bIsBrand = true;
	
	string str = "";

	if(!bIsBrand)
	{
		Response.Write("<tr><td>cat</td><td>");
		Response.Write(m_c);
		Response.Write("</td><td>");

		Response.Write("<select name=cat onchange=\"window.location=('ec.aspx?r=" + DateTime.Now.ToOADate());
		Response.Write("&a=e&q=" + m_q + "&c=" + HttpUtility.UrlEncode(m_c) + "&s=" + HttpUtility.UrlEncode(m_s) + "&ss=" + HttpUtility.UrlEncode(m_ss));
		Response.Write("&so=-1&co='+this.options[this.selectedIndex].value)\">");
		for(int j=0; j<dsAEV.Tables["cat"].Rows.Count; j++)
		{
			str = dsAEV.Tables["cat"].Rows[j][0].ToString();
			Response.Write("<option value='");
			Response.Write(str);
			Response.Write("'");
			if(m_co == "-1")
			{
				if(str == m_c)
					Response.Write(" selected");
			}
			else
			{
				if(str == m_co)
					Response.Write(" selected");
			}
			Response.Write(">");
			Response.Write(str);
			Response.Write("</option>");
		}
		Response.Write("</select>");
		Response.Write("<input type=text size=10 name=cat_new> <input type=submit name=cmd value='change cat only'>");
//		Response.Write("<input type=checkbox name=cco><font color=red><b>I am sure to do this batch command</b></font>");
	}
	else
		Response.Write("<input type=hidden name=cat value='" + m_c + "'>");

	Response.Write("</td></tr>");

	Response.Write("<tr><td>s_cat</td><td>");
	if(m_s == "zzzOthers")
		Response.Write("&nbsp;");
	else
		Response.Write(m_s);
	Response.Write("</td><td>");

	Response.Write("\r\n<select name=s_cat onchange=\"window.location=('ec.aspx?r=" + DateTime.Now.ToOADate());
	Response.Write("&a=e&q=" + m_q + "&c=" + HttpUtility.UrlEncode(m_c) + "&s=" + HttpUtility.UrlEncode(m_s) + "&ss=" + HttpUtility.UrlEncode(m_ss));
	Response.Write("&co=" + HttpUtility.UrlEncode(m_co) + "&so='+this.options[this.selectedIndex].value)\">");
	if(m_c == "Brands")
	{
		for(int j=0; j<dsAEV.Tables["brand"].Rows.Count; j++)
		{
			str = dsAEV.Tables["brand"].Rows[j][0].ToString();
			Response.Write("<option value='");
			Response.Write(str);
			Response.Write("'");
			if(m_so != "-1")
			{
				if(str == m_so)
					Response.Write(" selected");
			}
			else
			{
				if(str == m_s)
					Response.Write(" selected");
			}
			Response.Write(">");
			Response.Write(str);
			Response.Write("</option>");
		}
		Response.Write("</select>");
		Response.Write("<input type=text size=10 name=s_cat_new>");
	}
	else
	{
		for(int j=0; j<dsAEV.Tables["s_cat"].Rows.Count; j++)
		{
			str = dsAEV.Tables["s_cat"].Rows[j][0].ToString();
			Response.Write("\r\n<option value='");
			Response.Write(str);
			Response.Write("'");
			if(m_co == "-1" && m_so == "-1")
			{
				if(str == m_s)
					Response.Write(" selected");
			}
			else
			{
				if(m_so != "-1")
				{
					if(str == m_so)
						Response.Write(" selected");
				}
				else
				{
					if(str != "" && j<=1)
						Response.Write(" selected");
				}
			}
			Response.Write(">");
			Response.Write(str);
			Response.Write("</option>");
		}
		Response.Write("</select>");
		Response.Write("<input type=text size=10 name=s_cat_new> <input type=submit name=cmd value='change s_cat only'>");
//		Response.Write("<input type=checkbox name=cso><font color=red><b>I am sure to do this batch command</b></font>");
	}

	Response.Write("</td></tr>");
	if(!bIsBrand)
	{
		Response.Write("<tr><td>ss_cat</td><td>");
		if(m_ss == "zzzOthers")
			Response.Write("&nbsp;");
		else
			Response.Write(m_ss);
		Response.Write("</td><td>");
		Response.Write("<select name=ss_cat>");
		for(int j=0; j<dsAEV.Tables["ss_cat"].Rows.Count; j++)
		{
			str = dsAEV.Tables["ss_cat"].Rows[j][0].ToString();
			Response.Write("<option value='");
			Response.Write(str);
			Response.Write("'");
			if(str == m_ss)
				Response.Write(" selected");
			Response.Write(">");
			Response.Write(str);
			Response.Write("</option>");
		}
		Response.Write("</select>");
		Response.Write("<input type=text size=10 name=ss_cat_new>");

		Response.Write("</td></tr>\r\n");
	}
	
	Response.Write("<tr><td colspan=3 align=right><input type=submit value='save'></td></tr>\r\n");
	Response.Write("<tr><td colspan=3>Note this will only save your work to a temperary table, you need click the Update button in previous page to update the live catalog.</td></tr>");
	Response.Write("</table></form>");

	return true;
}

Boolean ToggleBrandShow()
{
	string sc = "UPDATE brand_settings SET show='";
	sc += m_show;
	sc += "' WHERE brand='";
	sc += m_s;
	sc += "'";	
//DEBUG("sc=", sc);
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
	return true;
}

Boolean DoSave()
{
	Boolean bRet = true;

	string q_old = Request.Form["q"];
	string c_old = Request.Form["c"];
	string s_old = Request.Form["s"];
	string ss_old = Request.Form["ss"];
	
	string seq = Request.Form["seq"];
	string cat = Request.Form["cat"];
	string s_cat = Request.Form["s_cat"];
	string ss_cat = Request.Form["ss_cat"];

	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	if(Request.Form["cat_new"] != null && Request.Form["cat_new"] != "")
		cat = Request.Form["cat_new"];
	if(Request.Form["s_cat_new"] != null && Request.Form["s_cat_new"] != "")
		s_cat = Request.Form["s_cat_new"];
	if(Request.Form["ss_cat_new"] != null && Request.Form["ss_cat_new"] != "")
		ss_cat = Request.Form["ss_cat_new"];

	m_q = seq;
	m_c = cat;
	m_s = s_cat;
	m_ss = ss_cat;

	if(c_old == "zzzOthers")
		c_old = "";
	if(s_old == "zzzOthers")
		s_old = "";
	if(ss_old == "zzzOthers")
		ss_old = "";

	if(cat == "zzzOthers")
		cat = "";
	if(s_cat == "zzzOthers")
		s_cat = "";
	if(ss_cat == "zzzOthers")
		ss_cat = "";

	if(q_old != seq)
	{
		if(!UpdateCatSeq(c_old, seq))
			return false;
	}
	
	string cmd = Request.Form["cmd"];
	if(cmd == "change cat only")
	{
		s_cat = s_old;
		ss_cat = ss_old;

//		if(Request.Form["cco"] == "on")
//		{
//			s_old = "-1";
//			ss_old = "-1";
//		}
	}
	else if(cmd == "change s_cat only")
	{
		cat = c_old;
		ss_cat = ss_old;
//		if(Request.Form["cso"] == "on")
//		{
//			ss_old = "-1";
//		}
	}

	if(!UpdateCatalogTable("product", cat, s_cat, ss_cat, c_old, s_old, ss_old))
		return false;
	if(!UpdateCatalogTable("code_relations", cat, s_cat, ss_cat, c_old, s_old, ss_old))
		return false;

	//update temp table
	string sc = "UPDATE catalog_temp SET ";
	sc += "cat='";
	sc += cat + "'";
	
	if(s_old != "-1")
		sc += ", s_cat='" + s_cat + "'";
	if(ss_old != "-1")
		sc += ", ss_cat='" + ss_cat + "'";
	
	sc += " WHERE cat='" + c_old + "'";
	
	if(s_old != "-1")
		sc += " AND s_cat='" + s_old + "'";
	if(ss_old != "-1")
		sc += " AND ss_cat='" + ss_old + "'";
		
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
/*
	sc = "INSERT INTO catalog_changes (cat, s_cat, ss_cat, c_old, s_old, ss_old) VALUES(";
	sc += cat;
	sc += "', ";
	sc += s_cat;
	sc += "', ";
	sc += ss_cat;
	sc += "', ";
	sc += c_old;
	sc += "', ";
	sc += s_old;
	sc += "', ";
	sc += ss_old;
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
*/
	return true;
}

Boolean DoUpdate()
{
/*	int rows = 0;
	string sc = "SELECT * FROM catalog_changes";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsc, "changes");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = null;
	string seq;
	string cat;
	string s_cat;
	string ss_cat;
	string c_old;
	string s_old;
	string ss_old;
	int i = 0;
	for(;i<rows;i++)
	{
		dr = dsc.Tables["changes"].Rows[i];
		cat = dr["cat"].ToString();
		s_cat = dr["s_cat"].ToString();
		ss_cat = dr["ss_cat"].ToString();
		c_old = dr["c_old"].ToString();
		s_old = dr["s_old"].ToString();
		ss_old = dr["ss_old"].ToString();

		//update tables;
		if(!UpdateCatalogTable("product", cat, s_cat, ss_cat, c_old, s_old, ss_old))
			return false;
		if(!UpdateCatalogTable("code_relations", cat, s_cat, ss_cat, c_old, s_old, ss_old))
			return false;
	}
*/
//	GetTempCatalogTable();
	
	return DoUpdateCatalogTable();
}

Boolean UpdateCatalogTable(string tableName, string cat, string s_cat, string ss_cat, string c_old, string s_old, string ss_old)
{
/*
DEBUG("tableName=", tableName);
DEBUG("cat=", cat);
DEBUG("s_cat=", s_cat);
DEBUG("ss_cat=", ss_cat);
DEBUG("c_old=", c_old);
DEBUG("s_old=", s_old);
DEBUG("ss_old=", ss_old);
*/
	string sc = "UPDATE ";
	sc += tableName;

	if(String.Compare(cat, "brands", true) == 0)
	{
		sc += " SET brand='";
		sc += s_cat;
		sc += "' WHERE brand='";
		sc += s_old + "'";
	}
	else
	{
		sc += " SET cat='" + cat + "'";
		
		if(s_old != "-1")
			sc += ", s_cat='" + s_cat + "'";
		if(ss_old != "-1")
			sc += ", ss_cat='" + ss_cat + "'";
		
		sc += " WHERE cat='" + c_old + "'";
		
		if(s_old != "-1")
			sc += " AND s_cat='" + s_old + "'";
		if(ss_old != "-1")
			sc += " AND ss_cat='" + ss_old + "'";
	}
//DEBUG("sc=", sc);
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
	return true;
}

Boolean EmptyTempTables()
{
	string sc = "DELETE FROM catalog_temp";
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
	sc = "DELETE FROM catalog_changes";
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
	return true;
}

Boolean SaveTempCatalogTable()
{
	string sc = "SELECT * FROM catalog_temp";
	try
	{
		SqlDataAdapter custDA = new SqlDataAdapter(sc, myConnection);
		SqlCommandBuilder custCB = new SqlCommandBuilder(custDA);
		
		myConnection.Open();
		custDA.Update(dst, "catalog");
		myConnection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean GetTempCatalogTable()
{
	dst.Clear();
	string sc = "SELECT * FROM catalog_temp ORDER BY seq, cat, s_cat, ss_cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "catalog");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean DrawCatalogTable()
{
	if(dst.Tables["catalog"].Rows.Count <= 0)
		return true;

	Boolean bRet = true;
	
	DrawTableHeader();
	int i = 0;

	DataRow dr = dst.Tables["catalog"].Rows[0];
	string brand_old = "-1";
	for(; i<dst.Tables["catalog"].Rows.Count; i++)
	{
		dr = dst.Tables["catalog"].Rows[i];
		if(dr["cat"].ToString() == "Brands")
		{
			string scat = dr["s_cat"].ToString();
			Trim(ref scat);
			if(scat.ToLower() == brand_old.ToLower())
				continue;
			else
				brand_old = scat;
		}
		if(!DrawRow(dr))
		{
			bRet = false;
			break;
		}
	}
	DrawTableFooter();
	return bRet;
}

void DrawTableHeader()
{
	Response.Write("<table width=100% cellspacing=0 cellpadding=0 align=Center rules=all bgcolor=white \r\n");
	Response.Write("bordercolor=#FFFFFF border=1 width=90% style=\"font-family:Verdana);font-size:8pt);\r\n");
	Response.Write("border-collapse:collapse);\">\r\n");
	Response.Write("<tr><td colspan=5 align=right>");
	Response.Write("<button onclick=window.location=('ec.aspx?a=u&r=" + DateTime.Now.ToOADate() + "')>");
	if(m_supplierString != "")
		Response.Write("Update " + m_catTableString.ToUpper() + "'s Catalog</button></td></tr>");
	else
		Response.Write("Update " + m_sCompanyName + "'s Catalog</button></td></tr>");
	Response.Write("<tr style=\"color:red;background-color:#CCCCCC;font-weight:bold;\">\r\n");
	Response.Write("<td>ORDER</td>");
	Response.Write("<td>CATALOG</td>");
	Response.Write("<td>S_CAT</td>");
	Response.Write("<td>SS_CAT</td>");
	Response.Write("<td>EDIT / TOGGLE BRAND SHOW</td>");
	Response.Write("</tr>\r\n");
}

void DrawTableFooter()
{
	Response.Write("<tr><td colspan=");
	if(m_supplierString != "")
		Response.Write("3");
	else
		Response.Write("5 align=right");
	Response.Write("><button onclick=window.location=('ec.aspx?a=u&r=" + DateTime.Now.ToOADate() + "')>");
	Response.Write("Update " + m_sCompanyName + "'s Catalog</button></td>");

	if(m_supplierString != "")
	{
		Response.Write("<td colspan=2 align=right>");
		Response.Write("<button onclick=window.location=('ec.aspx?a=u&main=1&r=" + DateTime.Now.ToOADate() + "')>");
		Response.Write("Update " + m_catTableString.ToUpper() + "'s Catalog</button></td>");
	}
	Response.Write("</tr>");
	Response.Write("</table>\r\n<br>\r\n");
}

Boolean m_bAlterColor = false;

Boolean DrawRow(DataRow dr)
{
	Boolean bRet = true;
	
	string seq = dr["seq"].ToString();
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	string ss_cat = dr["ss_cat"].ToString();

	string hcat = HttpUtility.UrlEncode(cat);
	string hs_cat = HttpUtility.UrlEncode(s_cat);
	string hss_cat = HttpUtility.UrlEncode(ss_cat);
	
	bool bIsBrand = false;
	if(String.Compare(hcat, "brands", true) == 0)
		bIsBrand = true;
	
	Response.Write("<tr");

	if(m_bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
	m_bAlterColor = !m_bAlterColor;

	Response.Write("><td>");
	Response.Write(seq);
	Response.Write("</td><td>");
	if(cat != null && cat != "")
		Response.Write(cat);
	else
		Response.Write("_");
	Response.Write("</td><td>");
	if(s_cat == "zzzOthers")
		Response.Write("&nbsp;");
	else
		Response.Write(s_cat);
	Response.Write("</td><td>");
	if(!bIsBrand)
	{
		if(ss_cat == "zzzOthers")
			Response.Write("&nbsp;");
		else
			Response.Write(ss_cat);
	}
	Response.Write("</td><td>");

	Response.Write("<a href=ec.aspx?a=e&q=" + seq + "&c=");
	Response.Write(hcat);
	Response.Write("&s=");
	Response.Write(hs_cat);
	Response.Write("&ss=");
	Response.Write(hss_cat);
	Response.Write(" target=_blank>Edit</a>");

	if(bIsBrand)
	{
		Boolean bShow = IsThisBrandShow(s_cat);
		Response.Write("&nbsp;&nbsp;&nbsp;<a href=ec.aspx?a=tb&s=");
		Response.Write(hs_cat);
		Response.Write("&sh=");
		if(bShow)
			Response.Write("false");
		else
			Response.Write("true");
		Response.Write(" class=d>");
		if(bShow)
			Response.Write("show");
		else
			Response.Write("in more brands");
		Response.Write("</a>");
	}

	Response.Write("</td></tr>");
	return bRet;
}

Boolean UpdateCatSeq(string cat, string seq)
{
	string sc = "";
	Boolean bExists = false;

	int rows = 0;

	sc = "SELECT TOP 1 seq FROM cat_seq WHERE cat='";
	sc += cat;
	sc += "'";

	DataSet dstt = new DataSet();
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dstt, "cat_seq");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows == 1) //exists, do update
	{
		sc = "UPDATE cat_seq SET ";
		sc += "seq='";
		sc += seq;
		sc += "' WHERE cat='";
		sc += cat;
		sc += "'";
	}
	else	//new, do insert
	{
		sc = "INSERT INTO cat_seq (cat, seq) VALUES('";
		sc += cat;
		sc += "', '";
		sc += seq;
		sc += "')";
	}

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
	return true;
}
</script>


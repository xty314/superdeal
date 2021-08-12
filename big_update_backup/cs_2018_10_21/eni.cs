<!-- #include file="price.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_id = "";
string m_type = "";
string m_cmd = null;
string m_co = "-1";
string m_so = "-1";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["co"] != null)
		m_co = Request.QueryString["co"];
	if(Request.QueryString["so"] != null)
		m_so = Request.QueryString["so"];
	if(Request.QueryString["id"] != null)
		m_id = Request.QueryString["id"];
	if(Request.Form["cmd"] != null)
		m_cmd = Request.Form["cmd"];
//DEBUG("m_so=", m_so);
	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();
//DEBUG("m_cmd=", m_cmd);
	if(m_cmd == "Update" || m_cmd == "Add")
	{
		if(DoUpdate())
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			if(g_bOrderOnlyVersion)
			{
				string s = "<meta http-equiv=\"refresh\" content=\"0; URL=eni.aspx?id=";
				s += HttpUtility.UrlEncode(m_id);
				s += "\">";
				Response.Write(s);
			}
			else
			{
				Response.Write("<br><br><center><h3>Done. New item added.</h3><br>");
				Response.Write("<a href=close.htm class=o>Close Window</a>");
			}
		}
	}
	else if(m_cmd == "Add New")
	{
		MyDrawTable(); //draw empty
	}
	else
	{
		if(!DoSearch())
			return;
		MyDrawTable();
	}
	WriteFooter();
	PrintAdminFooter();
}

Boolean DoSearch()
{
	dst.Clear();
	
	string sc = "SELECT c.id, c.supplier, c.supplier_code, c.code, c.brand, c.name ";
	sc += ", c.cat, c.s_cat, c.ss_cat, c.hot, c.skip, p.details, ";
	sc += "p.eta, p.stock, p.supplier_price, p.price, c.rate ";
	sc += " FROM code_relations_new" + m_catTableString + " c JOIN product_new" + m_catTableString + " p ON c.id=p.id ";
	sc += " WHERE c.id='" +  m_id + "' ";
	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(dst.Tables[0].Rows.Count <= 0)
	{
		Response.Write("<h3>Product not found</h3>");
		return false;
	}

	string cat = dst.Tables[0].Rows[0]["cat"].ToString();
	string s_cat = dst.Tables[0].Rows[0]["s_cat"].ToString();

	if(!ECATGetAllExistsValues("brand", "brand<>'-1' ORDER BY brand"))
		return false ;
	if(!ECATGetAllExistsValues("cat", "cat<>'Brands' ORDER BY cat"))
		return false;

	if(m_co == "-1")
		sc = "cat='" + cat + "' ORDER BY s_cat";
	else
		sc = "cat='" + m_co + "' ORDER BY s_cat";
	if(!ECATGetAllExistsValues("s_cat", sc, false))
		return false;

	if(m_co == "-1")
	{
		if(m_so == "-1")
			sc = "cat='" + cat + "' AND s_cat='" + s_cat + "' ORDER BY ss_cat";
		else
			sc = "cat='" + cat + "' AND s_cat='" + m_so + "' ORDER BY ss_cat";
	}
	else
	{
		if(m_so == "-1")
			sc = "cat='" + m_co + "' AND s_cat='" + s_cat + "' ORDER BY ss_cat";
		else
			sc = "cat='" + m_co + "' AND s_cat='" + m_so + "' ORDER BY ss_cat";
	}
//DEBUG("sc=", sc);
	if(!ECATGetAllExistsValues("ss_cat", sc, false))
		return false;
/*	
	if(!ECATGetAllExistsValues("cat", "cat<>'Brands' ORDER BY cat"))
		return false;

	sc = "cat='" + cat + "' ORDER BY s_cat";
	if(!ECATGetAllExistsValues("s_cat", sc))
		return false;

	sc = "cat='" + cat + "' AND s_cat='" + s_cat + "' ORDER BY ss_cat";
	if(!ECATGetAllExistsValues("ss_cat", sc))
		return false;
*/	
	return true;
}

bool BatchUpdate(string sField, string sNew, string sCondition)
{
	string sel_brand = "All Brands";
	if(Session["pni_brand"] != null)
		sel_brand = Session["pni_brand"].ToString();

	StringBuilder sb = new StringBuilder();
	sb.Append("UPDATE code_relations_new" + m_catTableString + " SET " + sField + "='");
	sb.Append(sNew);
	sb.Append("' " + sCondition);
	if(sField != "brand" && sel_brand != "All Brands")
		sb.Append(" AND brand='" + sel_brand + "'");
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
	return true;
}

bool DoUpdate()
{
	Boolean bRet = true;

	string id = Request.Form["id"];
	string supplier = Request.Form["supplier"];
	string supplier_code = Request.Form["supplier_code"];
	string brand = Request.Form["brand"];
	string name = Request.Form["name"];
	string cat = Request.Form["cat"];
	string s_cat = Request.Form["s_cat"];
	string ss_cat = Request.Form["ss_cat"];

	string code = Request.Form["code"];
	string stock = Request.Form["stock"];
	string supplier_price = Request.Form["supplier_price"];
	string price = Request.Form["price"];
	string hot = Request.Form["hot"];
	string skip = Request.Form["skip"];
	string eta = Request.Form["eta"];
	string details = Request.Form["details"];

	string brand_old = Request.Form["brand_old"];
	string cat_old = Request.Form["cat_old"];
	string s_cat_old = Request.Form["s_cat_old"];
	string ss_cat_old = Request.Form["ss_cat_old"];

	if(Request.Form["brand_new"] != "")
		brand = Request.Form["brand_new"];
	if(Request.Form["cat_new"] != "")
		cat = Request.Form["cat_new"];
	if(Request.Form["s_cat_new"] != "")
		s_cat = Request.Form["s_cat_new"];
	if(Request.Form["ss_cat_new"] != "")
		ss_cat = Request.Form["ss_cat_new"];

	Trim(ref name);
	Trim(ref brand);
	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	details = EncodeQuote(details);

	Boolean bBrandIndividual = (Request.Form["brand_individual"] == "on");
	Boolean bCatIndividual = (Request.Form["cat_individual"] == "on");
	Boolean bSCatIndividual = (Request.Form["s_cat_individual"] == "on");
	Boolean bSSCatIndividual = (Request.Form["ss_cat_individual"] == "on");

	string scon = "";

	if(brand != brand_old && !bBrandIndividual)
	{
		scon = " WHERE brand='" + brand_old + "'";
		if(brand_old == "")
			scon = " WHERE brand='' OR brand IS NULL ";
		if(!BatchUpdate("brand", brand, scon))
			return false;
	}
//	if(cat != cat_old && cat_old != "" && !bCatIndividual)
	if(cat != cat_old && !bCatIndividual)
	{
		scon = " WHERE cat='" + cat_old + "'";
		if(cat_old == "")
			scon = " WHERE cat='' OR cat IS NULL ";
		if(!BatchUpdate("cat", cat, scon))
			return false;
	}
//	if(s_cat != s_cat_old && s_cat_old != "" && !bSCatIndividual)
	if(s_cat != s_cat_old && !bSCatIndividual)
	{
		scon = "WHERE cat='" + cat + "' AND s_cat='" + s_cat_old + "'";
		if(s_cat_old == "")
			scon = "WHERE cat='" + cat + "' AND (s_cat='' OR s_cat IS NULL) ";
		if(!BatchUpdate("s_cat", s_cat, scon))
			return false;
	}
//	if(ss_cat != ss_cat_old && ss_cat_old != "" && !bSSCatIndividual)
	if(ss_cat != ss_cat_old && !bSSCatIndividual)
	{
		scon = "WHERE cat='" + cat + "' AND s_cat='" + s_cat + "' AND ss_cat='" + ss_cat_old + "'";
		if(ss_cat == "")
			scon = "WHERE cat='" + cat + "' AND s_cat='" + s_cat + "' AND (ss_cat='' OR ss_cat IS NULL) ";
		if(!BatchUpdate("ss_cat", ss_cat, scon))
			return false;
	}

	if(hot == null)
		hot = "0";
	else
		hot = "1";

	if(skip == null)
		skip = "0";
	else
		skip = "1";

	double dsupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dPrice = double.Parse(price, NumberStyles.Currency, null);
	double dRate = CalculatePriceRate(dsupplier_price, dPrice);
	
	if(g_bOrderOnlyVersion)
	{
		dRate = 1;
		skip = "0";
	}

	StringBuilder sb = new StringBuilder();
	if(skip=="1")
	{
		//update code_relations_new
		sb.Append("UPDATE code_relations_new" + m_catTableString + " SET skip=");
		sb.Append(skip);
		sb.Append(" WHERE id='");
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
		
		if(name.Length > 254)
			name = name.Substring(0, 254);
		if(cat.Length > 49)
			cat = cat.Substring(0, 49);
		if(s_cat.Length > 49)
			s_cat = s_cat.Substring(0, 49);
		if(ss_cat.Length > 49)
			ss_cat = ss_cat.Substring(0, 49);

		//insert code_relations
		string sc = "INSERT INTO code_relations (id, supplier, supplier_code, code, name, brand, cat, s_cat, ss_cat, hot, skip, rate)";
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
		sc += skip;
		sc += ", ";
		sc += dRate;
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
	
		//delete from produc_new
		sb.Remove(0, sb.Length);
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
		return true;
	}

	if(m_cmd == "Update")
	{
		sb.Remove(0, sb.Length);
		sb.Append("UPDATE code_relations_new" + m_catTableString + " SET code=");
		sb.Append(code);
		sb.Append(", brand='");
		sb.Append(brand);
		sb.Append("', name='");
		sb.Append(name);
		sb.Append("', cat='");
		sb.Append(cat);
		sb.Append("', s_cat='");
		sb.Append(s_cat);
		sb.Append("', ss_cat='");
		sb.Append(ss_cat);
		sb.Append("', hot=");
		sb.Append(hot);
		sb.Append(", skip=");
		sb.Append(skip);
		sb.Append(", rate=");
		sb.Append(dRate);
		sb.Append(" WHERE id='");
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

		//update product_new
		sb.Remove(0, sb.Length);
		sb.Append("UPDATE product_new" + m_catTableString + " SET stock=");
		sb.Append(stock);
		sb.Append(", price=");
		sb.Append(dPrice);
		sb.Append(", eta='");
		sb.Append(eta);
		sb.Append("', details='");
		sb.Append(details);
		sb.Append("' WHERE id='");
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
	}
	else if(m_cmd == "Add")
	{
	}
	return true;
}

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<form action=eni.aspx?id=");
	sb.Append(HttpUtility.UrlEncode(m_id));
	sb.Append(" method=post>\r\n");
	Response.Write(sb.ToString());
	Response.Flush();
}

void WriteFooter()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("</form>");//</body></html>");
	Response.Write(sb.ToString());
}

Boolean MyDrawTable()
{
//DEBUG("m_cmd=", m_cmd);
	string id = "";
	string supplier = "";
	string supplier_code = "";
	string code = "";
	string brand = "";
	string name = "";
	string cat = "";
	string s_cat = "";
	string ss_cat = "";
	string supplier_price = "";
	string price = "";
	string rate = "";
	string eta = "";
	string stock = "";
	string hot = "";
	string skip = "";
	string details = "";

	if(dst.Tables.Count > 0 && dst.Tables[0].Rows.Count > 0)
	{
		DataRow dr = dst.Tables[0].Rows[0];

		id = dr["id"].ToString();
		supplier = dr["supplier"].ToString();
		supplier_code = dr["supplier_code"].ToString();
		code = dr["code"].ToString();
		brand = dr["brand"].ToString();
		name = dr["name"].ToString();
		cat = dr["cat"].ToString();
		s_cat = dr["s_cat"].ToString();
		ss_cat = dr["ss_cat"].ToString();
		supplier_price = dr["supplier_price"].ToString();
		price = dr["price"].ToString();
		rate = dr["rate"].ToString();
		eta = dr["eta"].ToString();
		stock = dr["stock"].ToString();
		hot = dr["hot"].ToString();
		skip = dr["skip"].ToString();
		details = dr["details"].ToString();
	}
	else
	{
		Response.Write("<h3>Product Not Found</h3>");
		return false;
	}

//DEBUG("supplier_price=", supplier_price);
	if(supplier_price == "0" && !g_bOrderOnlyVersion)
		Response.Write("<h3><font color=red>Error, supplier_price is 0, please skip this product</font></h3>");

	StringBuilder sb = new StringBuilder();;
	sb.Append("<input type=hidden name=supplier value='" + supplier + "'>");
	sb.Append("<input type=hidden name=supplier_code value='" + supplier_code + "'>");
	sb.Append("<input type=hidden name=brand_old value='" + brand + "'>");
	sb.Append("<input type=hidden name=cat_old value='" + cat + "'>");
	sb.Append("<input type=hidden name=s_cat_old value='" + s_cat + "'>");
	sb.Append("<input type=hidden name=ss_cat_old value='" + ss_cat + "'>");

	sb.Append("<table border=0 bordercolor=black cellspacing=1 cellpadding=3 align=center>");
	sb.Append("<tr><td colspan=2 bgcolor=#666696 align=center><font color=white><b>");
	
	if(m_cmd != "Add New")
		sb.Append("Edit New Item");
	else
		sb.Append("New Item");
	sb.Append("</b></font></td></tr>");

	if(!g_bOrderOnlyVersion)
	{
		sb.Append(PrintOneRow("code", false, true)); //true to add other columns
		sb.Append(" &nbsp;&nbsp;&nbsp; hot <input type=checkbox name=hot value=");
		if(String.Compare(hot, "true", true) == 0)
			sb.Append("1 checked");
		else
			sb.Append("0 unchecked");
		sb.Append(">");

		sb.Append(" &nbsp;&nbsp;&nbsp; skip <input type=checkbox name=skip value=");
		if(String.Compare(skip, "true", true) == 0)
			sb.Append("1 checked");
		else
			sb.Append("0 unchecked");
		sb.Append(">&nbsp;&nbsp;&nbsp;<input type=submit name=cmd value='Update'>(same button as the bottom one)</td></tr>");
	}
	else
	{
		sb.Append("<input type=hidden name=code value='" + dst.Tables[0].Rows[0]["code"].ToString() + "'>");
		sb.Append("<input type=hidden name=hot value='" + dst.Tables[0].Rows[0]["hot"].ToString() + "'>");
		sb.Append("<input type=hidden name=skip value='" + dst.Tables[0].Rows[0]["skip"].ToString() + "'>");
	}

	sb.Append(PrintOneRow("name", false, false));
	sb.Append(PrintOneRow("brand", false, false));
	sb.Append(PrintOneRow("cat", false, false));
	sb.Append(PrintOneRow("s_cat", false, false));
	sb.Append(PrintOneRow("ss_cat", false, false));

	if(!g_bOrderOnlyVersion)
	{
		sb.Append(PrintOneRow("price", false, true));
		sb.Append("&nbsp;&nbsp;&nbsp;supplier_price : ");
		sb.Append(supplier_price);
		sb.Append("&nbsp;&nbsp;&nbsp;rate : ");
		sb.Append(rate);
		sb.Append("</td></tr>");
		sb.Append(PrintOneRow("stock", false, true));
		sb.Append("&nbsp;&nbsp;&nbsp;eta <input type=text name=eta size=10 value='");
		sb.Append(eta);
		sb.Append("'></td></tr>");

		sb.Append("<tr><td bgcolor=#eeeeee>");
		sb.Append("Specs</td><td bgcolor=#EEEEEE><textarea cols=45 rows=7 name=details>");
		sb.Append(details);
		sb.Append("</textarea>");
		sb.Append("</td></tr>");
	}
	else
	{
		sb.Append("<input type=hidden name=price value='" + dst.Tables[0].Rows[0]["price"].ToString() + "'>");
		sb.Append("<input type=hidden name=eta value='" + dst.Tables[0].Rows[0]["eta"].ToString() + "'>");
		sb.Append("<input type=hidden name=stock value='" + dst.Tables[0].Rows[0]["stock"].ToString() + "'>");
		sb.Append("<input type=hidden name=details value=\"" + details + "\">");
	}

	sb.Append("<tr><td colspan=2 align=right>");

	if(m_cmd == "Add New")
	{
		sb.Append("<input type=submit name=cmd value='Add' " + Session["button_style"] + ">");
	}
	else
	{
		sb.Append("<input type=submit name=cmd value='Update' " + Session["button_style"] + "></td></tr>");
	}
	sb.Append("</td></tr>");
	sb.Append("</table>");

	sb.Append("<input type=hidden name=id value='");
	sb.Append(id);
	sb.Append("'>");

	sb.Append("<input type=hidden name=supplier_price value='");
	sb.Append(supplier_price);
	sb.Append("'>");

	Response.Write(sb.ToString());
	return true;
}

string PrintOneRow(string sName, Boolean bOpenStart, Boolean bOpenEnd)
{
	string sv = "";
	if(dst.Tables.Count > 0 && dst.Tables[0].Rows.Count > 0)
		sv = dst.Tables[0].Rows[0][sName].ToString();

	StringBuilder sb = new StringBuilder();

	if(sName == "brand" || sName == "cat" || sName == "s_cat" || sName == "ss_cat")
	{
		string str;
		bool bMatch = false;
		sb.Append("<tr><td bgcolor=#EEEEEE>" + sName);
		sb.Append("</td><td><select name=" + sName);
		if(sName == "cat" || sName == "s_cat")
		{
			sb.Append(" onchange=\"window.location=('eni.aspx?id=" + HttpUtility.UrlEncode(m_id));
			if(sName == "cat")
				sb.Append("&so=-1&co='+this.options[this.selectedIndex].value)\"");
			else if(sName == "s_cat")
				sb.Append("&co=" + m_co + "&so='+this.options[this.selectedIndex].value)\"");
		}
		sb.Append(">");
		for(int j=0; j<dsAEV.Tables[sName].Rows.Count; j++)
		{
			str = dsAEV.Tables[sName].Rows[j][0].ToString();
			sb.Append("<option value='");
			sb.Append(str);
			sb.Append("'");
			if(!bMatch)
			{
				if(sName == "cat")
				{
					if(m_co == "-1")
					{
						if(str == sv)
						{
							bMatch = true;
							sb.Append(" selected");
						}
					}
					else
					{
						if(str == m_co)
						{
							bMatch = true;
							sb.Append(" selected");
						}
					}
				}
				else if(sName == "s_cat")
				{
					if(m_so == "-1")
					{
						if(str == sv)
						{
							bMatch = true;
							sb.Append(" selected");
						}
					}
					else
					{
						if(str == m_so)
						{
							bMatch = true;
							sb.Append(" selected");
						}
					}
				}
				else
				{
					if(str == sv)
					{
						bMatch = true;
						sb.Append(" selected");
					}
				}
			}
			sb.Append(">");
			sb.Append(str);
			sb.Append("</option>");
		}
		if(!bMatch)
			sb.Append("<option value='" + sv + "' selected>" + sv + "</option>");

		sb.Append("</select>");
		sb.Append("<input type=text size=20 name=" + sName + "_new>");
		sb.Append("<input type=checkbox name=" + sName + "_individual checked>Individual</td></tr>");
		return sb.ToString();
	}


	if(!bOpenStart)
	{	
		sb.Append("<tr><td ");
		sb.Append(" bgcolor=#EEEEEE>");
	}
	sb.Append(sName);
	sb.Append("</td><td bgcolor=#EEEEEE>");
	sb.Append("<input type=text size=");
	if(bOpenEnd || bOpenStart)
		sb.Append("10");
	else
		sb.Append("75");
	sb.Append(" name=");
	sb.Append(sName);
	sb.Append(" value='");
	sb.Append(sv);
	sb.Append("'>");
	if(!bOpenEnd)
		sb.Append("</td></tr>");
	else
		sb.Append("&nbsp;&nbsp;&nbsp;");
	return sb.ToString();
}

</script>

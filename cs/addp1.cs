<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations
string m_type = "";
string m_action = "";
string m_bo = "";
string m_co = "-1";
string m_so = "-1";
string m_sso = "";
string m_code = "";
string m_supplier_code = "";
string m_default_bottom_rate = "1.1";
string m_redurl = "";
string m_redBar = "";
string m_prename = "";

int nTotalLevel = 9;

string[] m_dealer_level = new string[10];
string[] m_sLevel_rate_figure = new string [10];

string m_nextSupplierCode = "-1";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	m_default_bottom_rate = GetSiteSettings("default_bottom_rate", "1.1", false);
	GetQueryStrings();
	nTotalLevel = int.Parse(GetSiteSettings("dealer_levels", "6", false));
	string help = Request.QueryString["h"];
	if(help != null && help != "")
	{
		ShowHelp(help);
		return;
	}
	m_sLevel_rate_figure[1] = "1.08";
	m_sLevel_rate_figure[2] = "1.05";
	m_sLevel_rate_figure[3] = "1.03";
	m_sLevel_rate_figure[4] = "1.02";
	m_sLevel_rate_figure[5] = "1.01";
	m_sLevel_rate_figure[6] = "1.00";
	m_sLevel_rate_figure[7] = "2";
	m_sLevel_rate_figure[8] = "2";
	m_sLevel_rate_figure[9] = "2";

	for(int i=1;i<10; i++)
	{
		string slevel = "";
		
		slevel = GetSiteSettings("set_import_level_rate" + i, m_sLevel_rate_figure[i], false);
		if(slevel != null && slevel != "")
			m_dealer_level[i] = slevel;
	}

	PrintAdminHeader();
	PrintAdminMenu();

    if(Request.QueryString ["s"] != "" && Request.QueryString["s"] != null)
		m_supplier_code  = Request.QueryString ["s"];
	if(m_redBar != "")
    {
		m_supplier_code = Session["redBar"].ToString();
		m_prename = " New Item  barcode: "+m_redBar;
		
	}

	if(m_action == "save")
	{
		if(DoSave())
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=liveedit.aspx?code=" + m_code + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}

	if(!ECATGetAllExistsValues("brand", "brand<>'-1' ORDER BY brand", false))
		return ;
	if(!ECATGetAllExistsValues("cat", "cat<>'Brands' ORDER BY cat", false))
		return;
	
	if(m_co == "-1")
	{
		if(dsAEV.Tables["cat"].Rows.Count > 0)
			m_co = dsAEV.Tables["cat"].Rows[0][0].ToString(); 
		else
			m_co = "";
	}
	string sc = "cat='" +  m_co + "' ORDER BY s_cat";
	if(!ECATGetAllExistsValues("s_cat", sc, false))
		return;

	if(m_so == "-1")
	{
		if(dsAEV.Tables["s_cat"].Rows.Count > 0)
			m_so = dsAEV.Tables["s_cat"].Rows[0][0].ToString();
		else
			m_so = "";
	}
	sc = "cat='" + m_co + "' AND s_cat='" + m_so + "' ORDER BY ss_cat";
	if(!ECATGetAllExistsValues("ss_cat", sc, false))
		return;

	if(!GetNextPrivateCode())
		return;

	PrintForm();
	PrintAdminFooter();
}

void GetQueryStrings()
{
	if(Session["addp_brand"] != null)
		m_bo = Session["addp_brand"].ToString();
	if(Session["addp_cat"] != null)
		m_co = Session["addp_cat"].ToString();
	if(Session["addp_s_cat"] != null)
		m_so = Session["addp_s_cat"].ToString();
	if(Session["addp_ss_cat"] != null)
		m_sso = Session["addp_ss_cat"].ToString();

	m_type = Request.QueryString["t"];
	m_action = Request.QueryString["a"];
	
	if(m_type == null)
	{
		m_action = null;
		m_type = "addnew";
	}
	if(Request.QueryString["co"] != null)
		m_co = Request.QueryString["co"];
	if(Request.QueryString["so"] != null)
		m_so = Request.QueryString["so"];
//	if(Request.QueryString["sso"] != null)
//		m_sso = Request.QueryString["sso"];
}

void PrintForm()
{
	Response.Write("<br><center><h3>Add New Product</h3></center>");
	Response.Write("<form name=form1 action=addp.aspx?a=save&t=");
	Response.Write(m_type);
	Response.Write(" method=POST>");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td>name</td><td>value</td></tr>");

//	if(m_type == "addnew")
		PrintFormNew();

	Response.Write("<tr><td colspan=2 align=right><i>(Item added as phased out)</i>");
	Response.Write(" <a href=?h=po title=help class=o target=_blank>?</a>");
	Response.Write(" <input type=submit "+ Session["button_style"] +" value=' &nbsp; Add &nbsp; ' ");
	Response.Write(" Onclick=\"if(document.form1.supplier_code.value=='' || document.form1.name.value==''){ window.alert('Please supply Manufacuturer Code and Product Description!!!'); return false;}\"> ");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=2>* <font color=red>dealer level rate:</font> <i>(please adjust your dealer level at edit site settings under <a href='setting.aspx'>set_import_level_rate</a>)</i>");
	for(int i=1; i<=nTotalLevel; i++)
		Response.Write("<br>Dealer Level "+ i +"= "+ m_dealer_level[i]);  
	Response.Write("</td></tr>");
	Response.Write("</table></form>");
	PrintJavaFunction();
	
}

void PrintFormNew()
{
	Response.Write("<tr><td>cat</td><td>");//<input type=text name=cat size=30 value='");
//	Response.Write(Request.Form["cat"]);
	Response.Write(PrintSelectionRow("cat", m_co));
	Response.Write(" <a href=?h=new_cat title=help class=o target=_blank>?</a></td></tr>");
	Response.Write("<tr><td>s_cat</td><td>");//<input type=text name=s_cat size=30 value='");
//	Response.Write(Request.Form["s_cat"]);
	Response.Write(PrintSelectionRow("s_cat", m_so));
	Response.Write("</td></tr>");
	Response.Write("<tr><td>ss_cat</td><td>");//<input type=text name=ss_cat size=30 value='");
//	Response.Write(Request.Form["ss_cat"]);
	Response.Write(PrintSelectionRow("ss_cat", m_sso));
//	Response.Write("'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Brand</td><td>");//<input type=text name=brand size=30 value='");
	Response.Write(PrintSelectionRow("brand", m_bo));
//	Response.Write(Request.Form["brand"]);
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Manufacture_PN<font color=red><b>*</b></font></td><td><input type=text name=supplier_code size=30 maxlength=100 value='");
	Response.Write(m_supplier_code);
	Response.Write("'></td></tr>");
	Response.Write("<tr><td>Description<font color=red><b>*</b></font></td><td><input type=text name=name size=65></td></tr>");
	Response.Write("<tr><td>Supplier</td><td>");
	Response.Write(PrintSupplierOptionsWithShortName_Addp());
	Response.Write(" <a href=?h=supplier title=help class=o target=_blank>?</a></td></tr>");

	Response.Write("<tr><td>Default Bottom Rate<font color=red>*</font></td><td><input type=text name=bottom_rate value='"+ m_default_bottom_rate +"' readonly>");
	Response.Write("<a href='setting.aspx' target=blank> Change Bottom Rate at site settings</a>");
	Response.Write("</td></tr>");

	//currency selection
	string rate_usd = GetSiteSettings("exchange_rate_usd", "0.49");
	string rate_aud = GetSiteSettings("exchange_rate_aud", "0.87");
	Response.Write("<input type=hidden name=rate_nzd value=1" + rate_usd + ">");
	Response.Write("<input type=hidden name=rate_usd value=" + rate_usd + ">");
	Response.Write("<input type=hidden name=rate_aud value=" + rate_aud + ">");
	Response.Write("<input type=hidden name=rate_usd_old value=" + rate_usd + ">");
	Response.Write("<input type=hidden name=rate_aud_old value=" + rate_aud + ">");
	Response.Write("<input type=hidden name=currency_name value='nzd'>");
	Response.Write("<input type=hidden name=currency value=1>");
/*	Response.Write("<tr><td>Currency</td><td><select name=currency onchange=\"UpdateEXRate()\">");
	Response.Write("<option value=1>NZD</option>");
	Response.Write("<option value=" + rate_usd + ">USD</option>");
	Response.Write("<option value=" + rate_aud + ">AUD</option>");
	Response.Write("</select></td></tr>");

	Response.Write("<tr><td>Exchange Rate</td><td><input type=text name=exrate value=1 onchange=\"OnExRateChange()\"></td></tr>");
	Response.Write("<tr><td>Supplier's Price</td><td><input type=text name=raw_supplier_price value=0 onchange=\"CalcCost()\"></td></tr>");
	Response.Write("<tr><td>Freight(NZD)</td><td><input type=text name=freight value=0 onchange=\"CalcCost()\"></td></tr>");

	Response.Write("<tr><td>NZD Cost</td><td><input type=text name=supplier_price size=30 onchange=\"CalcPrice()\"></td></tr>");
	Response.Write("<tr><td>Bottom Rate</td><td><input type=text name=rate size=30 onchange=\"CalcPrice()\" value=");
	Response.Write(GetSiteSettings("default_bottom_rate", "1.01"));
	Response.Write("></td></tr>");
	Response.Write("<tr><td>Bottom Price</td><td><input type=text name=price size=30 onchange=\"CalcRate()\"></td></tr>");
	Response.Write("<tr><td>Stock</td><td><input type=text name=stock size=30 value=0></td></tr>");
	Response.Write("<tr><td>ETA</td><td colspan=2><input type=text size=30 name=eta></td></tr>");
	Response.Write("<tr><td>Hot</td><td><input type=checkbox name=hot checked></td></tr>");
	Response.Write("<tr><td>Phase Out</td><td colspan=2><input type=checkbox name=skip></td></tr>\r\n");
	Response.Write("<tr><td>special</td><td colspan=2><input type=checkbox name=special></td></tr>");
	
	Response.Write("<tr><td>Level-1 Rate</td><td colspan=2><input type=text name=level_rate1 value=1.08></td></tr>");
	Response.Write("<tr><td>Level-2 Rate</td><td colspan=2><input type=text name=level_rate2 value=1.04></td></tr>");
//	Response.Write("<tr><td>Level-6 Rate</td><td colspan=2><input type=text name=level_rate2 value=1 readonly=true></td></tr>");

	Response.Write("<tr><td>Quantity Break-1&nbsp&nbsp;</td><td colspan=2><input type=text name=qty_break1 value=2></td></tr>");
	Response.Write("<tr><td>Quantity Break-2</td><td colspan=2><input type=text name=qty_break2 value=5></td></tr>");
	Response.Write("<tr><td>Quantity Break-3</td><td colspan=2><input type=text name=qty_break3 value=10></td></tr>");
	Response.Write("<tr><td>Quantity Break-4</td><td colspan=2><input type=text name=qty_break4 value=50></td></tr>");
*/
}

string PrintSupplierOptionsWithShortName_Addp()
{
	DataSet dssup = new DataSet();
	string type_supplier = GetEnumID("card_type", "supplier");
	int rows = 0;
	string sc = "SELECT id, short_name, name, email, company ";
	sc += " FROM card WHERE type=" + type_supplier + " ORDER BY company ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dssup, "suppliers");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "\r\n<select name=supplier>";
	s += "<option value=''></option>";
	for(int i=0; i<rows; i++)
	{
		string name = dssup.Tables["suppliers"].Rows[i]["company"].ToString();
		if(name == "")
			name = dssup.Tables["suppliers"].Rows[i]["name"].ToString();
		if(name == "")
			name = dssup.Tables["suppliers"].Rows[i]["short_name"].ToString();
		s += "<option value=" + dssup.Tables["suppliers"].Rows[i]["short_name"].ToString() + ">";
		s += name + "</option>\r\n";
	}
	s += "\r\n</select>";
	return s;
}

bool DoSave()
{
	m_code = Request.Form["code"];
	if(m_type == "addnew")
	{
		int nCode = GetNextCode();
		if(nCode <= 0)
		{
			Response.Write("Error generating code for new product");
			return false;
		}
		m_code = nCode.ToString();
	}

	Boolean bRet = true;

	string supplier	= Request.Form["supplier"];
	string supplier_code = Request.Form["supplier_code"];
	string supplier_price = "0";//Request.Form["supplier_price"];
	string id		= supplier + supplier_code;

	if(id.Length > 100)
	{
		Response.Write("<center><br><br><h3>M_PN is too long, maxmum 50 characters.</h3>");
		return false;
	}

	string name		= EncodeQuote(Request.Form["name"]);
	string brand	= EncodeQuote(Request.Form["brand"]);
	string cat		= EncodeQuote(Request.Form["cat"]);
	string s_cat	= EncodeQuote(Request.Form["s_cat"]);
	string ss_cat	= EncodeQuote(Request.Form["ss_cat"]);
	string rate		= "1.10";//Request.Form["rate"];
	string price	= "0";//Request.Form["price"];
	string stock	= "0";//Request.Form["stock"];
	string currency	= Request.Form["currency_name"];
	currency = GetEnumID("currency", currency);

	double level_rate1 = 1.08;//MyDoubleParse(Request.Form["level_rate1"]);
	double level_rate2 = 1.06;//MyDoubleParse(Request.Form["level_rate2"]);
	int qty_break1 = 2;//MyIntParse(Request.Form["qty_break1"]);
	int qty_break2 = 5;//MyIntParse(Request.Form["qty_break2"]);
	int qty_break3 = 10;//MyIntParse(Request.Form["qty_break3"]);
	int qty_break4 = 50;//MyIntParse(Request.Form["qty_break4"]);	
	
	//rate = "1.1";
	rate = GetSiteSettings("default_bottom_rate", "1.1", false);
	string exchange_rate = "1";//Request.Form["exrate"];
	string freight = "0";//Request.Form["freight"];
	string raw_supplier_price = "0";//Request.Form["raw_supplier_price"];

	//update exchange rate if changed
	string rate_usd = Request.Form["rate_usd"];
	string rate_aud = Request.Form["rate_aud"];
	string rate_usd_old = Request.Form["rate_usd_old"];
	string rate_aud_old = Request.Form["rate_aud_old"];
	if(rate_usd != rate_usd_old)
		SetSiteSettings("exchange_rate_usd", rate_usd);
	if(rate_aud != rate_aud_old)
		SetSiteSettings("exchange_rate_aud", rate_aud);

	if(stock == "")
		stock = "0"; //will display 'YES' instead of stock numbers if value is NULL in database
	string hot = "0";
	if(Request.Form["hot"] == "on")
		hot = "1";
	string skip		= "1"; //default to skip, 
	string special = "0";
	if(Request.Form["special"] == "on")
		special = "1";
	string isnew	= "0";

	Trim(ref name);
	Trim(ref brand);
	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	if(name != null && name.Length > 255)
		name = name.Substring(0, 255);

	if(Request.Form["cat_new"] != null && Request.Form["cat_new"] != "")
		cat = EncodeQuote(Request.Form["cat_new"]);
	if(Request.Form["s_cat_new"] != null && Request.Form["s_cat_new"] != "")
		s_cat = EncodeQuote(Request.Form["s_cat_new"]);
	if(Request.Form["ss_cat_new"] != null && Request.Form["ss_cat_new"] != "")
		ss_cat = EncodeQuote(Request.Form["ss_cat_new"]);
	if(Request.Form["brand_new"] != null && Request.Form["brand_new"] != "")
		brand = EncodeQuote(Request.Form["brand_new"]);

	if(supplier_code == null || supplier_code == "")
	{
		Response.Write("Error, no supplier code/manufacturer code");
		return false;
	}
	if(name == null || name == "")
	{
		Response.Write("Error, no product name");
		return false;
	}
	if(supplier_price == null || supplier_price == "" || TSIsDigit(supplier_price) == false)
	{
		Response.Write("Error, invalid supplier_price");
		return false;
	}
	if(price == null || price == "" || TSIsDigit(price) == false)
	{
		Response.Write("Error, invalid price");
		return false;
	}
	if(stock == "")
		stock = "0";
	
	double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dPrice = double.Parse(price, NumberStyles.Currency, null);
//	double dRate = MyDoubleParse(rate);
//	if(dSupplier_price > 0) //if cost <= 0 then disable rate
//		dRate = CalculatePriceRate(dSupplier_price, dPrice);

	//check for duplicate product ID
	string sc = "SELECT code, name FROM code_relations WHERE id='" + id + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "id_check");
		if(rows > 0)
		{
			string ic_code = dst.Tables["id_check"].Rows[0]["code"].ToString();
			string ic_name = dst.Tables["id_check"].Rows[0]["name"].ToString();
			Response.Write("<br><br><center><h3>Error, dupplicat Product ID (supplier + manualfacture_PN) found.</h3>");
			Response.Write("<b>Code : </b><a href=liveedit.aspx?code=" + ic_code + " class=o>" + ic_code + "</a><br>");
			Response.Write("<b>Desc : </b>" + ic_name);
			Response.Write("<br><br><h4>Check manualfacture_PN or use a different supplier.</h4>");
			PrintAdminFooter();
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}


	sc = "INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, code, name, brand, ";
	sc += " cat, s_cat, ss_cat, hot, skip, rate, currency, exchange_rate, foreign_supplier_price, nzd_freight ";
	sc += ", barcode ";
	for(int i=1; i<=9; i++)
		sc += ", level_rate"+ i +"";  
//	sc += " , level_rate1, level_rate2, qty_break1, qty_break2, qty_break3, qty_break4 ";
	sc += ") VALUES('";
	sc += id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += dSupplier_price;
	sc += ", ";
	sc += m_code;
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
	sc += "1";
	sc += ", ";
	sc += skip;
	sc += ", ";
	sc += rate;
	sc += ", '";
	sc += currency;
	sc += "', ";
	sc += exchange_rate;
	sc += ", ";
	sc += raw_supplier_price;
	sc += ", ";
	sc += freight;
	sc += ", '"+ m_code +"'";
/*	sc += ", ";
	sc += level_rate1;
	sc += ", ";
	sc += level_rate2;
	sc += ", ";
	sc += qty_break1;
	sc += ", ";
	sc += qty_break2;
	sc += ", ";
	sc += qty_break3;
	sc += ", ";
	sc += qty_break4;
*/
	for(int i=1; i<=9; i++)
		sc += ", "+ m_dealer_level[i] +" ";
	sc += ")";
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

//	DateTime dNow = DateTime.Now;
//	DateTime price_age = dNow;
	

//	sc = "INSERT INTO product (code, name, brand, cat, s_cat, ss_cat, hot, price, stock, supplier, supplier_code, supplier_price, price_age)";
	sc = "INSERT INTO product_skip (id, stock, supplier_price, price) ";
	sc += " VALUES('" + id + "', 0, 0, 0)";
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
	if(bRet)
	{
		Session["addp_brand"] = brand;
		Session["addp_cat"] = cat;
		Session["addp_s_cat"] = s_cat;
		Session["addp_ss_cat"] = ss_cat;
	}
//	if(special == "1")
//		AddSpecial(m_code);

	return bRet;
}

bool GetNextPrivateCode()
{
	int next_code = 0;
//	string sc = "SELECT TOP 1 supplier_code FROM code_relations WHERE supplier='' ORDER BY id DESC";
	string sc = "SELECT TOP 1 code FROM code_relations ORDER BY code DESC";
	if(dst.Tables["code_relations"] != null)
		dst.Tables["code_relations"].Clear();
	
	int rows;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "code_relations");
		if(rows > 0)
		{
			string last_code = dst.Tables["code_relations"].Rows[0]["code"].ToString();
//DEBUG("code=", last_code);
			try
			{
				next_code = int.Parse(last_code) + 1;
			}
			catch(Exception e)
			{
//				Response.Write("<br><br><center><h3>Code Error, <font color=red>" + last_code + "</font> isn't a number</h3>");
			}
		}
		else
			next_code = m_nFirstCode;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(next_code > 0)
		m_nextSupplierCode = next_code.ToString();
	return true;
}

string PrintSelectionRow(string sName, string sValue)
{
	bool bMatch = false;
	string str = "";
	StringBuilder sb = new StringBuilder();
	sb.Append("\r\n<select name=" + sName);
	if(sName == "cat" || sName == "s_cat")
	{
		sb.Append(" onchange=\"window.location=('addp.aspx?r=" + DateTime.Now.ToOADate());
		if(sName == "cat")
		{
			sb.Append("&so=-1");
			sb.Append("&co='");
		}
		else if(sName == "s_cat")
		{
			sb.Append("&co="+HttpUtility.UrlEncode(m_co));
			sb.Append("&so='");
		}
		sb.Append("+ escape(this.options[this.selectedIndex].value))\"");
	}
	sb.Append(">");
	bool bHasBlank = false;
	for(int j=0; j<dsAEV.Tables[sName].Rows.Count; j++)
	{
		str = dsAEV.Tables[sName].Rows[j][0].ToString();
//		Trim(ref str);
		if(str == "")
			bHasBlank = true;
		sb.Append("<option value='");
		sb.Append(str);
		sb.Append("'");
		if(str == sValue)
		{
			bMatch = true;
			sb.Append(" selected");
		}
		if(!bMatch)
		{
			if(sName == "cat" && m_co == "-1")
			{
				bMatch = true;
				sb.Append(" selected");
			}
			else if(sName == "s_cat" && m_so == "-1")
			{
				if(str != "")
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
			else if(sName == "ss_cat")
			{
				if(str != "")
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
	{
		sb.Append("<option value='" + sValue + "' selected>" + sValue + "</option>");
		if(sValue == "")
			bHasBlank = true;
	}
	if(!bHasBlank)
		sb.Append("<option value=''></option>");
	sb.Append("</select>");
	sb.Append(" New " + sName + " : <input type=text size=30 name=" + sName + "_new" + ">");
	return sb.ToString();
}

void PrintJavaFunction()
{
	Response.Write("<script TYPE=text/javascript");
	Response.Write(">");
	Response.Write("function CalcPrice()");
	Response.Write("{ var d = 0;\r\n");
	Response.Write("	var rate = Number(document.form1.rate.value);\r\n");
	Response.Write("	if(rate <= 1){rate = 1.01;window.alert('Error, Bottom Rate cannot less than 1.00 !\\r\\nI will reset it to 1.01');}\r\n");
	Response.Write("	d = rate * Number(document.form1.supplier_price.value);\r\n");
	Response.Write("	d = Math.round(d * 100) / 100;\r\n");
	Response.Write("	document.form1.rate.value=rate;\r\n");
	Response.Write("	document.form1.price.value=d;\r\n");
	Response.Write("}\r\n");
	Response.Write("function CalcRate()");
	Response.Write("{ var d = 0;\r\n");
	Response.Write("	d = Number(document.form1.price.value) / Number(document.form1.supplier_price.value);\r\n");
	Response.Write("	if(d <= 1){d = 1.01;window.alert('Error, Bottom Rate cannot less than 1.00 !\\r\\nI will reset it to 1.01');}\r\n");
//	Response.Write("	d = Math.round(d, 2);\r\n");
	Response.Write("	document.form1.rate.value=d;\r\n");
	Response.Write("}\r\n");
	Response.Write("function CalcRateAndPrice()");
	Response.Write("{\r\n");
	Response.Write("	var cost = Number(document.form1.supplier_price.value);\r\n");
	Response.Write("	var rate = Number(document.form1.rate.value);\r\n");
	Response.Write("	var d = 0;\r\n");
	Response.Write("	d = cost * rate;\r\n");
	Response.Write("	d = Math.round(d, 3);\r\n");
	Response.Write("	document.form1.rate.value=rate;\r\n");
	Response.Write("	document.form1.price.value=d;\r\n");
	Response.Write("}\r\n");
	Response.Write("function UpdateEXRate()");
	Response.Write("{\r\n");
	Response.Write("	var rate = Number(document.form1.currency.value);\r\n");
	Response.Write("	document.form1.exrate.value = rate;\r\n");
	Response.Write("	if(rate == Number(document.form1.rate_nzd.value))document.form1.currency_name.value=nzd;\r\n");
	Response.Write("	else if(rate == Number(document.form1.rate_usd.value))document.form1.currency_name.value='usd';\r\n");
	Response.Write("	else if(rate == Number(document.form1.rate_aud.value))document.form1.currency_name.value='aud';\r\n");
	Response.Write("	CalcCost();\r\n");
	Response.Write("}\r\n");
	Response.Write("function CalcCost()");
	Response.Write("{ var d = 0;\r\n");
	Response.Write("	d = Number(document.form1.raw_supplier_price.value) / Number(document.form1.exrate.value) + Number(document.form1.freight.value);\r\n");
	Response.Write("	d = Math.round(d * 100) / 100;\r\n");
	Response.Write("	document.form1.supplier_price.value=d;\r\n");
	Response.Write("	CalcPrice();\r\n");
	Response.Write("}\r\n");
	Response.Write("function OnExRateChange()");
	Response.Write("{\r\n");
	Response.Write("	if(document.form1.currency_name.value == 'nzd')document.form1.exrate.value=1;\r\n"); //fix NZD exchange rate to 1.00
	Response.Write("	else if(document.form1.currency_name.value == 'usd')document.form1.rate_usd.value=document.form1.exrate.value;\r\n");
	Response.Write("	else if(document.form1.currency_name.value == 'aud')document.form1.rate_aud.value=document.form1.exrate.value;\r\n");
	Response.Write("	CalcCost();");
	Response.Write("};");
	Response.Write("</script");
	Response.Write(">");
}

void ShowHelp(string key)
{
	PrintAdminHeader();
	Response.Write("<br><table height=93% width=95% align=center valign=center bgcolor=white><tr><td valign=top>");
	string s = "<br><center><h3>Help - ";

	if(key == "new_cat")
	{
		s += "Select Category</h3><table><tr><td>";
		s += "Select suitable category for this new item.";
		s += "<br><br>Enter new category name if no existing category fits.";
	}
	else if(key == "supplier")
	{
		s += "Select Supplier</h3><table><tr><td><h5>";
		s += "System identifies item with its supplier_short_name + manufacture_ProductNumber ";
		s += "<br>You may use blank supplier only if you don't use this system to do purchase. ";
		s += "<br>Note that <font color=red>supplier can not be changed later</font>, so do ";
		s += "<a href=ecard.aspx?n=supplier&a=new class=o>add new supplier</a> first.";
	}
	else if(key == "po") //cross reference
	{
		s += "Why added as phased out?</h3><table><tr><td><h5>";
		s += "Item prices will only be set in the next step, they all zero now, ";
		s += "<br>and because this is a live system, web visitors will be able to ";
		s += "<br>see new item right after you added it. so we hide it (by added as ";
		s += "<br>phased out) before set all prices.";
		s += "<br><br>Remember to uncheck 'Phased out' once prices been set(in next step)";
	}
	s += "</td></tr></table>";
	Response.Write(s);
	Response.Write("<br><br><center><h4><a href=close.htm class=o>Close Window</a></h4>");
	Response.Write("</td></tr></table>");
}

</script>

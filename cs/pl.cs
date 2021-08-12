<!-- #include file="kit_fun.cs" -->
<script runat=server>

DataSet ds = new DataSet();

int m_nDealerLevel = 1;
string m_fileName = "";
string m_type = "";

string m_server = "";
string m_port = "";
string m_path = "";
string m_login = "";
string m_pass = "";
string m_response = "";
string m_rtime = "";
string m_uploaded = "";

int m_nDiscountLevel = 1;
int m_ftps = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;

	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];

	if(Request.Form["cmd"] != null)
	{
		if(Request.Form["cmd"] != "Delete")
			DoUpdateFtpSettings();
		else
			DoDeleteFtpSettings();
		if(Request.Form["cmd"] == "Test")
			CheckAutoFtp(true);
	}

	GetFtpSettings();
//	InitializeData();
	PrintHeaderAndMenu();
	if(m_type == "")
	{
		SelectMethod();
	}
	else if(m_type == "dl")
	{
		GenerateFile();
	}
	else if(m_type == "ftp")
	{
		ShowFtpForm();
	}

	PrintFooter();
}

bool GetFtpSettings()
{
	if(ds.Tables["ftp"] != null)
		ds.Tables["ftp"].Clear();

	string sc = "SELECT * FROM auto_ftp WHERE card_id = " + Session["card_id"].ToString() + " ORDER BY id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_ftps = myCommand.Fill(ds, "ftp");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(m_ftps > 0)
	{
		DataRow dr = ds.Tables["ftp"].Rows[0];
		m_server = dr["server"].ToString();
		m_port = dr["port"].ToString();
		m_path = dr["path"].ToString();
		m_login = dr["login"].ToString();
		m_pass = dr["pass"].ToString();
		m_response = dr["last_response"].ToString();
		m_rtime = dr["last_response_time"].ToString();
		if(m_rtime != "")
			m_rtime = DateTime.Parse(m_rtime).ToString("dd-MM-yyyy HH:mm:ss");
		m_uploaded = dr["file_uploaded"].ToString();
	}
	return true;
}

void ShowFtpForm()
{
	Response.Write("<br><center><h3>Price List FTP Settings</h3>");
	Response.Write("<form action=pl.aspx?t=ftp method=post>");
	Response.Write("<table width=500 cellspacing=3 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>Server : </td><td><input type=text name=server size=40 value='" + m_server + "'></td></tr>");
	Response.Write("<tr><td><b>Port : </td><td><input type=text name=port size=40 value='" + m_port + "'></td></tr>");
	Response.Write("<tr><td><b>Path : </td><td><input type=text name=path size=40 value='" + m_path + "'></td></tr>");
	Response.Write("<tr><td><b>Login Name : </td><td><input type=text name=login size=40 value='" + m_login + "'></td></tr>");
	Response.Write("<tr><td><b>Password : </td><td><input type=password name=pass size=42 value='" + m_pass + "'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=checkbox name=del_confirm>Tick to confirm delete ");
	Response.Write("<input type=submit name=cmd value='Delete' " + Session["button_style"] + ">");
	Response.Write("&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=submit name=cmd value='Test' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Save' " + Session["button_style"] + "></td></tr>");

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");

	Response.Write("<tr><td nowrap><b>Last Upload Path : </b></td><td>" + m_uploaded + "</td></tr>");
	Response.Write("<tr><td nowrap><b>Last Upload Time : </b></td><td>" + m_rtime + "</td></tr>");
	Response.Write("<tr><td nowrap valign=top><b>Last Upload Response : </b></td><td>" + m_response + "</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
}

void SelectMethod()
{
	Response.Write("<br><center><h3>Please Select</h3>");
	Response.Write("<table width=55%>");// cellspacing=10 cellpadding=10 bordercolor=#EEEEEE bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:11pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><img src=r.gif> <a href=pl.aspx?t=dl class=o><font size=+1>Download Price List File Now</font></a></td></tr>");
//	Response.Write("<tr><td><img src=r.gif> <a href=pl.aspx?t=ftp class=o><font size=+1>Auto FTP Price List(everyday around midnight)</font></a></td></tr>");
	Response.Write("</table>");
	Response.Write("<br><br><br><br><br><br><br>");
}

bool GenerateFile()
{
	Response.Write("<br><br><center><h3>Generating price list file, please wait....</h3><br>");

	bool bFixedPrices = false;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		bFixedPrices = true;

	string sc = "SELECT p.supplier_code, p.code, p.name, p.brand, p.price";
	sc += ", p.stock-p.allocated_stock, p.cat, p.s_cat, p.ss_cat, p.eta ";
	sc += ", c.level_rate1, c.level_rate2, c.qty_break1, c.qty_break2, c.qty_break3, c.qty_break4 "; 
	sc += ", c.level_rate3, c.level_rate4, c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9 ";
//	sc += ", c.manual_cost_nzd * c.rate AS bottom_price, c.clearance ";
	sc += ", (c.manual_cost_nzd * c.rate) + nzd_freight AS bottom_price, c.clearance ";
    sc += ", c.level_price0 AS rrp, c.barcode AS item_barcode ";
    sc += ", SUM(sq.qty) AS stock ";
    if(bFixedPrices)
	{
		for(int n=1; n<=9; n++)
			sc += ", c.price" + n.ToString();
	}
	sc += " FROM product p";
	//sc += " INNER JOIN ( SELECT code, SUM(qty) AS stock FROM stock_qty GROUP BY code) st ON p.code = st.code ";
    sc += " JOIN stock_qty sq ON sq.code=p.code ";
    sc += " JOIN code_relations c ON c.code=p.code ";
    sc += " WHERE 1=1 AND c.skip=0";
	if(Session["cat_access_sql"] != null)
	{
		if(Session["cat_access_sql"].ToString() != "all")
		{
			string limit = Session["cat_access_sql"].ToString();
			sc += " AND (p.brand " + limit;
			if(limit.ToLower().IndexOf("not") >= 0)
				sc += " AND ";
			else
				sc += " OR ";
			sc += " p.s_cat " + limit + ") ";
		}
	}
    sc += " GROUP BY p.supplier_code, p.code, p.name, p.brand, p.price, p.stock-p.allocated_stock, p.cat, p.s_cat, p.ss_cat, p.eta ";
    sc += ", c.level_rate1, c.level_rate2, c.qty_break1, c.qty_break2, c.qty_break3, c.qty_break4,  c.level_rate3, c.level_rate4, c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9  ";
    sc += ", (c.manual_cost_nzd * c.rate) + nzd_freight, c.clearance ";
    sc += ", c.level_price0, c.barcode ";
    if(bFixedPrices)
	{
		for(int n=1; n<=9; n++)
			sc += ", c.price" + n.ToString();
	}
	//sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name";
    sc += " ORDER BY p.supplier_code ";
//DEBUG("sc=", sc);
//return false;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(!WritePriceList())
		Response.Write("sth wrong");
	else
	{
		Response.Write("<h4>File is ready to download</h4><br>");
		Response.Write("<a href=/download/" + m_fileName + " + class=o>" + m_fileName + "</a><br><br><br><br><br><br><br>");
	}
	return false;
}

bool WritePriceList()
{
	bool bFixedPrices = false;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		bFixedPrices = true;

	bool bRoundPrice = false;
	if(Session["round_price_no_cent"] == null)
	{
		bRoundPrice = MyBooleanParse(GetSiteSettings("round_price_no_cent", "0", true));
		Session["round_price_no_cent"] = bRoundPrice;
	}
	else
		bRoundPrice = (bool)Session["round_price_no_cent"];

	if(TS_UserLoggedIn())
		m_nDealerLevel = MyIntParse(Session[m_sCompanyName + "dealer_level"].ToString());

	StringBuilder sb = new StringBuilder();
	//sb.Append("\"Category\",\"Sub Category\",\"More Category\",\"Brand\",\"Code\",\"Manufacture PN\",\"Description\",\"Stock On Hand\",\"ETA\",\"Your Price\",\r\n"); 
    sb.Append("\"Category\",\"Sub Category\",\"More Category\",\"Brand\",\"Product Code\",\"Description\",\"Barcode\",\"Price\",\r\n");
	
	DataRow dr = null;
	string code = "";
	
	double level_rate = 1.3;

	double dPrice = 999999;//double.Parse(dr["price"].ToString());

	for(int i=0; i<ds.Tables["product"].Rows.Count; i++)
	{
		dr = ds.Tables["product"].Rows[i];
		code = dr["code"].ToString();
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		cat = cat +" - "+ s_cat;

		bool bClearance = MyBooleanParse(dr["clearance"].ToString());	

		if(bFixedPrices)
		{
			dPrice = MyDoubleParse(dr["price" + m_nDealerLevel].ToString());
		}
		else
		{
			if(bReturnDealerLevel(cat, Session["card_id"].ToString()))
				level_rate = MyDoubleParse(dr["level_rate" + m_nDiscountLevel].ToString());
			else
				level_rate = MyDoubleParse(dr["level_rate" + m_nDealerLevel].ToString());
			dPrice = MyDoubleParse(dr["bottom_price"].ToString());
			if(!bClearance)
				dPrice *= level_rate;
			if(bRoundPrice)
				dPrice = Math.Round(dPrice, 0);
		}
        dPrice = MyDoubleParse(dr["rrp"].ToString());
		//get level rate
//		level_rate = MyDoubleParse(dr["level_rate" + m_nDealerLevel].ToString());
//		dPrice = double.Parse(dr["price"].ToString());
//		dPrice *= level_rate;
		
		sb.Append("\"" + EncodeDoubleQuote(dr["cat"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["s_cat"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["ss_cat"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["brand"].ToString()) + "\",");
		//sb.Append("\"" + EncodeDoubleQuote(dr["code"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["supplier_code"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["name"].ToString()) + "\",");
		//sb.Append("\"" + EncodeDoubleQuote(dr["stock"].ToString()) + "\",");
		//sb.Append("\"" + EncodeDoubleQuote(dr["eta"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["item_barcode"].ToString()) + "\",");
        sb.Append("\"" + dPrice.ToString("c") + "\",\r\n");
	}

	string strPath = Server.MapPath("/download/") + "\\";
	string lname = Session["name"].ToString();
	int bpos = lname.IndexOf(" ");
	if(bpos > 0)
		lname = lname.Substring(0, bpos);
	lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	m_fileName += lname + "_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + ".csv";
	strPath += m_fileName;

	Encoding enc = Encoding.GetEncoding("iso-8859-1");
	byte[] Buffer = enc.GetBytes(sb.ToString());

	// Create a file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(Buffer, 0, Buffer.Length);
	// Close file
	newFile.Close();

	return true;
}

bool bReturnDealerLevel(string cat, string card_id)
{
	if(ds.Tables["dealer_level"] != null)
		ds.Tables["dealer_level"].Clear();
	bool bFoundDealerLevel = false;
	string sc = " SELECT level FROM dealer_levels ";
	sc += " WHERE cat = '"+ cat +"' AND card_id = "+ card_id +"";
//DEBUG("sc = ", sc);
int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "dealer_level");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows == 1)
	{
		m_nDiscountLevel = int.Parse(ds.Tables["dealer_level"].Rows[0]["level"].ToString());
		bFoundDealerLevel = true;
	}

	return bFoundDealerLevel;
}

bool DoUpdateFtpSettings()
{
	string sc = "";
	GetFtpSettings();

	m_server = EncodeQuote(Request.Form["server"]);
	m_path = EncodeQuote(Request.Form["path"]);
	m_port = EncodeQuote(Request.Form["port"]);
	m_login = EncodeQuote(Request.Form["login"]);
	m_pass = EncodeQuote(Request.Form["pass"]);

	Trim(ref m_server);
	Trim(ref m_path);
	Trim(ref m_port);
	Trim(ref m_login);
	Trim(ref m_pass);
	
	if(m_server == "")
		return true;

	if(m_server.Length > 49)
		m_server = m_server.Substring(0, 49);
	if(m_path.Length > 49)
		m_path = m_path.Substring(0, 49);
	if(m_port.Length > 49)
		m_port = m_port.Substring(0, 49);
	if(m_login.Length > 49)
		m_login = m_login.Substring(0, 49);
	if(m_pass.Length > 49)
		m_pass = m_pass.Substring(0, 49);

	if(m_ftps == 0)
	{
		sc = " INSERT INTO auto_ftp (card_id, server, path, port, login, pass)";
		sc += " VALUES(" + Session["card_id"].ToString();
		sc += ", '" + m_server + "' ";
		sc += ", '" + m_path + "' ";
		sc += ", '" + m_port + "' ";
		sc += ", '" + m_login + "' ";
		sc += ", '" + m_pass + "' ";
		sc += ") ";
	}
	else
	{
		sc = "UPDATE auto_ftp SET ";
		sc += " server = '" + m_server + "' ";
		sc += ", path = '" + m_path + "' ";
		sc += ", port = '" + m_port + "' ";
		sc += ", login = '" + m_login + "' ";
		sc += ", pass = '" + m_pass + "' ";
		sc += " WHERE card_id = " + Session["card_id"].ToString();
	}
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
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

bool DoDeleteFtpSettings()
{
	if(Request.Form["del_confirm"] != "on")
	{
		Response.Write("<br><br><center><font color=red><h3>Error, tick delete confirmation to delete</h3></font>");
		return false;
	}
	string sc = "DELETE FROM auto_ftp WHERE card_id = " + Session["card_id"].ToString();
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
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

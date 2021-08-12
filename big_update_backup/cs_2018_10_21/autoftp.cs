<!-- #include file="ftpclient.cs" -->
<script runat=server>

DataSet dsftp = new DataSet();

bool CheckAutoFtp(bool bTest)
{
	int nHour = int.Parse(DateTime.Now.ToString("HH"));
	if(nHour < 19 && nHour > 9) //do not do ftp during daytime
	{
		if(!bTest)
			return true;
	}

	string dNow = DateTime.Now.ToString("dd-MM-yyyy");

	string last_ftp_date = GetSiteSettings("last_ftp_date", dNow);
	if(last_ftp_date == dNow && !bTest)
		return true;

	if(!bTest)
		SetSiteSettings("last_ftp_date", dNow);

	string sc = " SELECT * FROM auto_ftp WHERE disabled = 0 ";
	if(bTest)
		sc += " AND card_id = " + Session["card_id"].ToString();
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsftp, "job");
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return false;
	}

	DataRow dr = null;
	string id = "";
	string card_id = "";
	string server = "";
	string path = "";
	string port = "";
	string login = "";
	string pass = "";
	string level = "";
	string limit = "";

	for(int i=0; i<dsftp.Tables["job"].Rows.Count; i++)
	{
		dr = dsftp.Tables["job"].Rows[i];
		id = dr["id"].ToString();
		card_id = dr["card_id"].ToString();
		server = dr["server"].ToString();
		path = dr["path"].ToString();
		port = dr["port"].ToString();
		login = dr["login"].ToString();
		pass = dr["pass"].ToString();
		
		dr = GetCardData(card_id);
		level = dr["dealer_level"].ToString();
		if(level == null || level == "" || !IsInteger(level))
			level = "1";
		limit = BuildCatAccess(dr["cat_access"].ToString());
		
		DoFtp(int.Parse(level), limit, server, path, port, login, pass, id);
	}
	return true;
}

bool DoFtp(int nLevel, string limit, string server, string path, string port, string user, string pwd, string id)
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

	if(dsftp.Tables["product"] != null)
		dsftp.Tables["product"].Clear();

	string sc = "SELECT p.supplier_code, p.code, p.name, p.brand, p.price";
	sc += ", p.stock-p.allocated_stock AS stock, p.cat, p.s_cat, p.ss_cat, p.eta ";
	sc += ", c.level_rate1, c.level_rate2, c.qty_break1, c.qty_break2, c.qty_break3, c.qty_break4 "; 
	sc += ", c.level_rate3, c.level_rate4, c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9 ";
	sc += ", c.manual_cost_nzd * c.rate AS bottom_price, c.clearance ";
	if(bFixedPrices)
	{
		for(int n=1; n<=9; n++)
			sc += ", c.price" + n.ToString();
	}
	sc += " FROM product p";
	sc += " JOIN code_relations c ON c.code=p.code ";
	sc += " WHERE c.skip=0 AND c.cat <> 'ServiceItem' ";
	if(limit != null && limit != "" && limit != "all")
		sc += " AND (p.brand IN(" + limit + ") OR p.s_cat IN(" + limit + ")) ";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsftp, "product");
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("\"Category\",\"Sub Category\",\"More Category\",\"Brand\",\"Code\",\"Manufacture PN\",\"Description\",\"Stock On Hand\",\"ETA\",\"Your Price\",\r\n"); 
	
	DataRow dr = null;
	string code = "";
	
	double level_rate = 1.3;//GetLevelRate(m_nDealerLevel, lr1, lr2);

	double dPrice = 999999;//double.Parse(dr["price"].ToString());

	for(int i=0; i<dsftp.Tables["product"].Rows.Count; i++)
	{
		dr = dsftp.Tables["product"].Rows[i];
		code = dr["code"].ToString();
		bool bClearance = MyBooleanParse(dr["clearance"].ToString());	

		if(bFixedPrices)
		{
			dPrice = MyDoubleParse(dr["price" + nLevel].ToString());
		}
		else
		{
			level_rate = MyDoubleParse(dr["level_rate" + nLevel].ToString());
			dPrice = MyDoubleParse(dr["bottom_price"].ToString());
			if(!bClearance)
				dPrice *= level_rate;
			if(bRoundPrice)
				dPrice = Math.Round(dPrice, 0);
		}
		
		sb.Append("\"" + EncodeDoubleQuote(dr["cat"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["s_cat"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["ss_cat"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["brand"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["code"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["supplier_code"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["name"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["stock"].ToString()) + "\",");
		sb.Append("\"" + EncodeDoubleQuote(dr["eta"].ToString()) + "\",");
		sb.Append("\"" + dPrice.ToString("c") + "\",\r\n");
	}

	Encoding enc = Encoding.GetEncoding("iso-8859-1");
	byte[] buf = enc.GetBytes(sb.ToString());

	string uri = "";
	if(!server.StartsWith("ftp://"))
		uri += "ftp://";
	uri += server.ToLower();
	if(port != "")
		uri += ":" + port.ToLower();
	if(!path.StartsWith("/") && path != "")
		uri += "/";
	uri += path.ToLower();
	if(!path.EndsWith("/"))
		uri += "/";
	uri += "price.csv";

	string file_uploaded = uri;

	// register the "ftp:" prefix here for WebRequest
	FtpRequestCreator Creator = new FtpRequestCreator();
	WebRequest.RegisterPrefix("ftp:", Creator);

	string sRet = "success";

	FtpWebRequest w = null;

	try
	{
		Uri url = new Uri(uri);
		w = new FtpWebRequest( url );
//		w = WebRequest.Create( uri );
	}
	catch(Exception ex)
	{
		return false;
	}

	if(w == null)
		sRet = "Connect to " + uri + " falied";
	else
	{
//		w.Timeout = 300000; //15 second
		w.Passive = true;
		w.Method = "put";
		w.Credentials = new NetworkCredential(user, pwd);
		Stream writestream = w.GetRequestStream();
		if(writestream == null)
			sRet = "Connect to " + uri + " falied";
		else
		{
			writestream.Write(buf, 0, buf.Length);
			writestream.Close();

			WebResponse r = null;
			try
			{		
				r = w.GetResponse();
			}
			catch(Exception ex)
			{
				string s = ex.ToString();
				int p = s.IndexOf("Exception:");
				if(p > 0 && p+10 < s.Length)
					s = s.Substring(p + 10, s.Length - 10 - p);
				
				p = s.IndexOf("at ASP.");
				if(p > 0)
					s = s.Substring(0, p);
			
				s = s.Replace("\r\n", "\r\n<br>");
				sRet = s;
			}
		}
	}

	if(sRet.Length > 1023)
		sRet = sRet.Substring(0, 1022);

	//log
	sc = " UPDATE auto_ftp SET last_response = '" + EncodeQuote(sRet) + "' ";
	sc += ", last_response_time=GETDATE(), file_uploaded='" + EncodeQuote(file_uploaded) + "' ";
	sc += " WHERE id=" + id;
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
//		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>

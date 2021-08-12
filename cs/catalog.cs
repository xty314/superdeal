<script runat=server>
Boolean CreateTempTable(string tableName)
{
	string sc = "SELECT TOP 0 * FROM ";
	sc += tableName; 
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, tableName);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetAvailableCatalogs() //default parameters
{
	bool bMain = true;
	if(m_supplierString != "")
		bMain = false; // do main update only implicit call GetAvailableCatalogs(true)
	return GetAvailableCatalogs(bMain); 
}

bool GetAvailableCatalogs(bool bMain) //if bMain then update main site catalog, other wise update Corperate catalog only
{
	if(!CreateTempTable("catalog"))
	{
		Response.Write("Create temp table failed.");
		return false;	
	}

	int rows = 0;
	string sc = "";

	dsc.Clear();
	dst.Clear();

//	if(m_supplierString == "")
	{
		sc = " SELECT DISTINCT p.brand, p.s_cat ";
		sc += " FROM product p JOIN code_relations c ON c.code=p.code WHERE 1=1 ";
		if(m_sSite != "admin")
			sc += " AND c.inactive=0 ";
		if(!bMain && m_supplierString != "")
			sc += " AND p.supplier IN" + m_supplierString + " ";
		sc += " ORDER BY p.brand, p.s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dsc, "brands");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	int brandsFound = 0;
	if(rows > 0)
	{
		DataRow dr;
		string lastBrand = "-1";
		string currentBrand = "-1";
		string lastCat = "-1";
		string currentCat = "-1";
		for(int i=0; i<rows; i++)
		{
			currentBrand = dsc.Tables["brands"].Rows[i]["brand"].ToString();
			currentCat = dsc.Tables["brands"].Rows[i]["s_cat"].ToString();

			if(currentBrand == null || currentBrand == "")
				currentBrand = "zzzOthers";
			if(currentCat == null || currentCat == "")
				currentCat = "zzzOthers";

			if(String.Compare(currentBrand, lastBrand, true) == 0)
				if(String.Compare(currentCat, lastCat, true) == 0)
					continue;
			 //new brand, or new catalog of this brands, add to temp catalog table
			lastBrand = currentBrand;
			lastCat = currentCat;
			brandsFound++;
			dr = dst.Tables["catalog"].NewRow();
			dr["seq"] = "1";
			dr["cat"] = "Brands";
			dr["s_cat"] = currentBrand;
			dr["ss_cat"] = currentCat;
			dst.Tables["catalog"].Rows.Add(dr);
//			MonitorProcess(100);
		}
	}

	sc = " SELECT DISTINCT p.cat, p.s_cat, p.ss_cat ";
	sc += " FROM product p JOIN code_relations c ON c.code = p.code ";
	sc += " WHERE 1=1 ";
	if(m_sSite != "admin")
		sc += " AND c.inactive=0 ";
	if(!bMain && m_supplierString != "")
		sc += " AND p.supplier IN" + m_supplierString + " ";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsc, "catalog");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int cats_found = 0;
	if(rows > 0)
	{
		DataRow dr;

		string seq = "99";
		string last_cat = "-1";
		string current_cat = "-1";
		string last_s_cat = "-1";
		string current_s_cat = "-1";
		string last_ss_cat = "-1";
		string current_ss_cat = "-1";

		Boolean doAdd = false;
		for(int i=0; i<rows; i++)
		{
			dr = dsc.Tables["catalog"].Rows[i];

			current_cat = dr["cat"].ToString();
			current_s_cat = dr["s_cat"].ToString();
			current_ss_cat = dr["ss_cat"].ToString();
			
			if(current_cat == null || current_cat == "")
				current_cat= "zzzOthers";
			if(current_s_cat == null || current_s_cat == "")
				current_s_cat= "zzzOthers";
			if(current_ss_cat == null || current_ss_cat == "")
				current_ss_cat= "zzzOthers";

			if(current_ss_cat != last_ss_cat) //new ss_cat, add to temp catalog table
			{
				doAdd = true;
			}
			else if(current_s_cat != last_s_cat) //rare cases, ss_cat identical but s_cat not
			{
				doAdd = true;
			}
			else if(current_cat != last_cat) //rare
			{
				doAdd = true;
			}

			if(doAdd)
			{
				cats_found++;
				doAdd = false;
				last_cat = current_cat;
				last_s_cat = current_s_cat;
				last_ss_cat = current_ss_cat;

				dr = dst.Tables["catalog"].NewRow();
				dr["seq"] = GetCatSeq(current_cat);
				dr["cat"] = current_cat;
				dr["s_cat"] = current_s_cat;
				dr["ss_cat"] = current_ss_cat;
				dst.Tables["catalog"].Rows.Add(dr);
			}
//			MonitorProcess(100);
		}
	}

//begin kit
	string p_cat = GetSiteSettings("package_bundle_kit_name", "Kit", true);
	sc = "SELECT DISTINCT s_cat, ss_cat FROM kit ";
	if(m_sSite != "admin")
		sc += " WHERE inactive=0 ";
	sc += " ORDER BY s_cat, ss_cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsc, "kit");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows > 0)
	{
		string seq = "99";
		string last_cat = "-1";
		string current_cat = "-1";
		string last_s_cat = "-1";
		string current_s_cat = "-1";
		string last_ss_cat = "-1";
		string current_ss_cat = "-1";

		Boolean doAdd = false;
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dsc.Tables["kit"].Rows[i];

			current_cat = p_cat;
			current_s_cat = dr["s_cat"].ToString();
			current_ss_cat = dr["ss_cat"].ToString();
			
			if(current_cat == null || current_cat == "")
				current_cat= "zzzOthers";
			if(current_s_cat == null || current_s_cat == "")
				current_s_cat= "zzzOthers";
			if(current_ss_cat == null || current_ss_cat == "")
				current_ss_cat= "zzzOthers";

			if(current_ss_cat != last_ss_cat) //new ss_cat, add to temp catalog table
			{
				doAdd = true;
			}
			else if(current_s_cat != last_s_cat) //rare cases, ss_cat identical but s_cat not
			{
				doAdd = true;
			}
			else if(current_cat != last_cat) //rare
			{
				doAdd = true;
			}

			if(doAdd)
			{
				cats_found++;
				doAdd = false;
				last_cat = current_cat;
				last_s_cat = current_s_cat;
				last_ss_cat = current_ss_cat;

				dr = dst.Tables["catalog"].NewRow();
				dr["seq"] = GetCatSeq(current_cat);
				dr["cat"] = current_cat;
				dr["s_cat"] = current_s_cat;
				dr["ss_cat"] = current_ss_cat;
				dst.Tables["catalog"].Rows.Add(dr);
			}
		}
	}
//end kit

	Response.Write("Total <font color=red>" + cats_found + "</font> categories.\r\n");
	return true;
}

string GetCatSeq(string cat)
{
	string s = "99";
	string sc = "SELECT TOP 1 seq FROM cat_seq WHERE cat='";
	sc += cat;
	sc += "'"; 
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		DataSet dscs = new DataSet();
		if(myCommand.Fill(dscs) > 0)
			s = dscs.Tables[0].Rows[0].ItemArray[0].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	return s;
}

bool DoUpdateCatalogTable() //default true
{
	bool bMain = true;
	if(m_supplierString != "")
		bMain = false; // do main update only implicit call GetAvailableCatalogs(true)
	return DoUpdateCatalogTable(bMain);
}

bool DoUpdateCatalogTable(bool bMain) //if bMain then update main site catalog, other wise update Corperate catalog only
{
	if(!GetAvailableCatalogs(bMain))
		return false;

	int rows = 0;
	DataRow dr = null;
	string seq;
	string cat;
	string s_cat;
	string ss_cat;
	int i = 0;
	string sc = "";
	
	Response.Write("Updating ");
	if(bMain)
		Response.Write("main site ");
	Response.Write("catalog table...");
	Response.Flush();

	sc = "DELETE catalog";
	if(!bMain && m_supplierString != "")
		sc += m_catTableString;
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

	rows = dst.Tables["catalog"].Rows.Count;
	for(i=0;i<rows;i++)
	{
		dr = dst.Tables["catalog"].Rows[i];
		seq = dr["seq"].ToString();
		cat = dr["cat"].ToString();
		s_cat = dr["s_cat"].ToString();
		ss_cat = dr["ss_cat"].ToString();

//		cat = RemoveQuote(cat);
//		s_cat = RemoveQuote(s_cat);
//		ss_cat = RemoveQuote(ss_cat);
//
		sc = "INSERT INTO catalog";
		if(!bMain && m_supplierString != "")
			sc += m_catTableString;
		sc += " (seq, cat, s_cat, ss_cat) VALUES('";
		sc += seq;
		sc += "', N'";
		sc += EncodeQuote(cat);
		sc += "', N'";
		sc += EncodeQuote(s_cat);
		sc += "', N'";
		sc += EncodeQuote(ss_cat);
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
		MonitorProcess(100);
	}
	Response.Write(" done<br>\r\n");
	Response.Flush();
	return true;
}
</script>
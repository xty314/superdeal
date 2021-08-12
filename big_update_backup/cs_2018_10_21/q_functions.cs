<script runat="server">

DataTable dtQ = new DataTable();
int m_qfields = 64;
string[] fn = new string[64];
string[] fd = new string[64];
const string m_sNONE = "***NONE***(video on board)";

string m_labourCodeH = "";
string m_labourCodeS = "";
bool m_bShipAsParts = false;
bool m_bInstallOS = false;
bool m_bWithOS = false;

string m_ssid = "";

bool CheckQTable()
{
	int i=0;
	int j=0;
	fn[i++] = "cpu";
	fd[j++] = "CPU";
	fn[i++] = "mb";
	fd[j++] = "MotherBoard";
	fn[i++] = "ram";
	fd[j++] = "Memory";
	fn[i++] = "video";
	fd[j++] = "VideoCard";
	fn[i++] = "sound";
	fd[j++] = "SoundCard";
	fn[i++] = "hd";
	fd[j++] = "HardDrive";
	fn[i++] = "fd";
	fd[j++] = "FloppyDrive";
	fn[i++] = "cd";
//	fd[j++] = "CDROMDrive";
    fd[j++] = "Optical Drives 1";
	fn[i++] = "cdrw";
//	fd[j++] = "CDWriter";
    fd[j++] = "Optical Drives 2";
	fn[i++] = "modem";
	fd[j++] = "Modem";
	fn[i++] = "monitor";
	fd[j++] = "Monitor";
	fn[i++] = "pccase";
	fd[j++] = "Case";
	fn[i++] = "kb";
	fd[j++] = "Keyboard";
	fn[i++] = "mouse";
	fd[j++] = "Mouse";
	fn[i++] = "speaker";
	fd[j++] = "Speaker";
	fn[i++] = "printer";
	fd[j++] = "Printer";
	fn[i++] = "scanner";
	fd[j++] = "Scanner";
	fn[i++] = "os";
	fd[j++] = "System";
	fn[i++] = "nic";
	fd[j++] = "NetworkCard";
//	fn[i++] = "opt1";
//	fd[j++] = "Optional";
//	fn[i++] = "opt2";
//	fd[j++] = "Optional";
//	fn[i++] = "opt3";
//	fd[j++] = "Optional";
	m_qfields = i;
//	Session["dtQ" + m_ssid] = null;
	if(Session["dtQ" + m_ssid] != null)
	{
		dtQ = (DataTable)Session["dtQ" + m_ssid];
		return true;
	}
//DEBUG("rebuild QTable", "");
	BuildQTable();
	return true;

}

void EmptyQTable()
{
	Session["dtQ" + m_ssid] = null;
	CheckQTable();
}

bool BuildQTable()
{
	dtQ = null;
	dtQ = new DataTable();
	int i=0;
//DEBUG("i=", i);
	
//DEBUG("quotation table created", "");
	for(i=0; i<m_qfields; i++)
		dtQ.Columns.Add(new DataColumn(fn[i], typeof(String)));
	for(i=0; i<m_qfields; i++)
		dtQ.Columns.Add(new DataColumn(fn[i]+"_price", typeof(String)));
	for(i=0; i<m_qfields; i++)
		dtQ.Columns.Add(new DataColumn(fn[i]+"_qty", typeof(String)));
	for(i=0; i<m_qfields; i++)
		dtQ.Columns.Add(new DataColumn(fn[i]+"_name", typeof(String)));
	for(i=0; i<m_qfields; i++)
		dtQ.Columns.Add(new DataColumn(fn[i]+"_special", typeof(String))); //flag for manual entered item or item not from quotations system

	DataRow dr = dtQ.NewRow();
	for(i=0; i<m_qfields; i++)
		dr[i] = "-1";
	for(i=m_qfields; i<m_qfields*3; i++)
		dr[i] = "0";
	for(i=m_qfields*3; i<m_qfields*4; i++)
		dr[i] = ""; //for names (manually entered items)
	for(i=m_qfields*4; i<m_qfields*5; i++)
		dr[i] = "0"; //for special (manually entered items)
//	for(i=m_qfields; i<m_qfields*2; i++)
//		dr[i] = "0";
	dtQ.Rows.Add(dr);
	Session["dtQ" + m_ssid] = dtQ;
/*
	if(!SetFirstOption("cpu"))
		return false;
	if(!ValidatePart("mb"))
		return false;
	if(!ValidatePart("ram"))
		return false;
	if(!ValidatePart("video"))
		return false;
	for(i=4; i<m_qfields; i++)
	{
//		if(fn[i] == "opt1" || fn[i] == "opt2" || fn[i] == "opt3")
//			continue;
		if(!SetFirstOption(fn[i]))
			return false;
	}
*/
	return true;
}

bool ChangeOption(string key, string code, string qty)
{
//DEBUG("change option, key="+key, " code="+code);
	DataRow dr = dtQ.Rows[0];
	dtQ.AcceptChanges();
	dr.BeginEdit();
	dr[key] = code;
	if(key == "cpu")
	{
		if(!ValidatePart("mb"))
			return false;
		if(!ValidatePart("ram"))
			return false;
		if(!ValidatePart("video"))
			return false;
	}
	else if(key == "mb")
	{
		if(!ValidatePart("ram"))
			return false;
		if(!ValidatePart("video"))
			return false;
	}

	dr[key] = code;
	if(key == "mb")
	{
		string cpus = GetCpusMBNeeds(code);
		dtQ.Rows[0]["cpu_qty"] = cpus;
	}
	else if(key != "cpu") //cpu qty will be set in validatePart("mb")
	{
		if(code == "")
		{
//			RemovePartFromSetup(key);
//			Response.Write("Error, There's a " + key + " not found");
			string u = "http://" + Request.ServerVariables["SERVER_NAME"] + "/" + TSGetPath() + "/admin/sqm.aspx?t=" + key;
			AlertAdmin("System Quotation Error - " + Request.ServerVariables["SERVER_NAME"], "A <font color=red><b>" + key + "</b></font> is missing\r\n, please open page <a href=" + u + ">" + u + "</a> once to automatically correct this problem");
			qty = "0";
		}
		else if(MyIntParse(code) < 0)
			qty = "0";
		else
			qty = "1";
		dr[key + "_qty"] = qty;
	}
	if(code != "" && MyIntParse(code) > 0)
	{
		double dPrice = 0;
		if(!GetItemPrice(code, qty, ref dPrice))
			return false;
		dr[key + "_price"] = dPrice.ToString();
	}
//	if(qty == "0")
//		dr["key"] = "-1";
	dr.EndEdit();
	dtQ.AcceptChanges();
	return true;
}

bool UpdateOption(string key, string code, string qty)
{
//DEBUG("change option, key="+key+" code="+code, " qty="+qty+" name="+Request.Form["name_" + key]);
	double dPrice = 0;
	bool bPriceChanged = false;
	string oldCode = dtQ.Rows[0][key].ToString();
	string name = ""; //for manually entered items
	if(code == "0") //manually entered item
	{
		try
		{
			int ncode = int.Parse(Request.Form["name_" + key].ToString());
			GetItemPrice(Request.Form["name_" + key].ToString(), qty, ref dPrice);
			if(dPrice > 0) //found
			{
				//if we got here, then a product code has been manually entered, take it
				code = Request.Form["name_" + key].ToString();
				dtQ.Rows[0][key + "_special"] = "1";
			}
		}
		catch(Exception e)
		{
		}
	}
//	else
//		dr[key + "_special"] = "0";

	if(code == "0") //manually entered item
	{
		name = Request.Form["name_" + key];
		dPrice = double.Parse(Request.Form[key + "_price"], NumberStyles.Currency, null);
		bPriceChanged = true;
	}
	else
	{
		if(oldCode == code)
		{
			if(Request.Form[key + "_price_old"] != null)
			{
				string spo = Request.Form[key + "_price_old"];
				string spn = Request.Form[key + "_price"];
				if(spo != spn) //sales changed price
				{
					dPrice = MyMoneyParse(spn);
					bPriceChanged = true;
				}
			}
		}
		else //item changed, update price
		{
			if(code != "" && code != null && int.Parse(code) > 0)
			{
	//DEBUG("here", 0);
				if(!GetItemPrice(code, qty, ref dPrice))
					return false;
	//DEBUG("here1, pirce=", dPrice.ToString("c"));
			}
			bPriceChanged = true;
		}
	}
	DataRow dr = dtQ.Rows[0];
	dtQ.AcceptChanges();
	dr.BeginEdit();
	dr[key] = code;
	dr[key + "_qty"] = qty;
	dr[key + "_name"] = name;
	if(bPriceChanged)
	{
//		if(Session["display_include_gst"] != null)
//		{	
//			double dGST = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;
//			dPrice = Math.Round(dPrice / (1 + dGST), 2);
//DEBUG("price=", dPrice.ToString());
//		}
		dr[key + "_price"] = dPrice.ToString();
	}
	dr.EndEdit();
	dtQ.AcceptChanges();
	return true;
}

bool ChangeAllOptions()
{
//Response.Write("Changing all options");
	for(int i=0; i<m_qfields; i++)
	{
		string code = Request.Form[fn[i]];
		string qty = Request.Form[fn[i] + "_qty"];
		if(code == "-1")
			qty = "0";
		else
		{
			if(qty == "0")
				qty = "1";
		}
//DEBUG("i="+i, " qty="+qty);
		if(!UpdateOption(fn[i], code, qty))
			return false;
	}
	return true;
}

bool ValidatePart(string s)
{
	string code = dtQ.Rows[0][s].ToString();
//DEBUG("changin " + s, "code="+code);
	if(!TSIsDigit(code))
		return true;
	if(code != null && code != "" && int.Parse(code) > 0)
	{
		DataSet dso = new DataSet();
		int rows = 0;

		string cpu = dtQ.Rows[0]["cpu"].ToString();
		string mb = dtQ.Rows[0]["mb"].ToString();
		string sc = "";
		if(s == "mb")
			sc = "SELECT p.code, p.name FROM q_mb q JOIN product p ON q.code=p.code WHERE q.parent=" + cpu + " ORDER BY p.code";
		else if(s == "ram" || s == "video")
			sc = "SELECT p.code, p.name FROM q_" + s + " q JOIN product p ON q.code=p.code WHERE q.parent=" + mb + " ORDER BY p.code";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(dso, s);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		for(int i=0; i<rows; i++)
		{
			string c = dso.Tables[0].Rows[i]["code"].ToString();
			if(c == code)
			{
//DEBUG(s + " " + code, " valid");
				return true;
			}
		}
	}
	if(!SetFirstOption(s))
		return false;
	return true;
}

bool SetFirstOption(string s)
{
	string cpu = dtQ.Rows[0]["cpu"].ToString();
	string mb = dtQ.Rows[0]["mb"].ToString();

	DataSet dso = new DataSet();
	int rows = 0;

	string sc = "";
	if(s == "cpu")
		sc = "SELECT DISTINCT p.code, p.name FROM q_mb q JOIN product p ON q.parent=p.code ORDER BY p.code";
	else if(s == "mb")
		sc = "SELECT p.code, p.name FROM q_" + s + " q JOIN product p ON q.code=p.code WHERE q.parent=" + cpu + " ORDER BY p.code";
	else if(s == "ram" || s == "video")
		sc = "SELECT p.code, p.name FROM q_" + s + " q JOIN product p ON q.code=p.code WHERE q.parent=" + mb + " ORDER BY p.code";
	else
		sc = "SELECT p.code, p.name FROM q_flat q JOIN product p ON q." + s + "=p.code WHERE q." + s + ">0 ORDER BY p.code";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	string sf = "-1";
	if(rows > 0)
		sf = dso.Tables[0].Rows[0]["code"].ToString();

	double dPrice = 0;
	int nsf = 0;
	if(sf != "" && int.Parse(sf) > 0)
	{
		nsf = int.Parse(sf);
		if(!GetItemPrice(sf, "1", ref dPrice))
			return false;
	}

	dtQ.AcceptChanges();
	DataRow dr = dtQ.Rows[0];
	dr.BeginEdit();
	dtQ.Rows[0][s] = sf;
	if(sf == "-1")
		dtQ.Rows[0][s + "_qty"] = "0";
	else
		dtQ.Rows[0][s + "_qty"] = "1";
	dtQ.Rows[0][s + "_price"] = dPrice.ToString();
	if(s == "mb" && nsf > 0)
	{
		string cpus = GetCpusMBNeeds(sf);
		dtQ.Rows[0]["cpu_qty"] = cpus;
	}

	dr.EndEdit();
	dtQ.AcceptChanges();
//DEBUG("changed dtQ."+s, "="+sf);
	return true;
}

bool GetItemPrice(string code, string qty, ref double dPrice)
{
	DataSet dso = new DataSet();
	int rows = 0;

//	string sc = "SELECT p.price * c.level_rate1  AS price FROM product p JOIN code_relations c ON c.code=p.code WHERE p.code=" + code;
    string sc = "SELECT c.manual_cost_nzd * c.level_rate1 * c.rate AS price FROM product p JOIN code_relations c ON c.code=p.code WHERE p.code=" + code; // modify by colin 
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso, sc);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
//		DEBUG("item not found, code=", code);
		return false;
	}

	dPrice = double.Parse(dso.Tables[0].Rows[0]["price"].ToString());
	return true;
}

string GetNewOptionsKey(string sKey)
{
	/*string sc = "SELECT code, name, price FROM product WHERE ";
	if(sKey == "cpu")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='cpu' AND name NOT LIKE '%bundle%' AND name NOT LIKE '%kit%' ");
	else if(sKey == "mb")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='motherboard'");
	else if(sKey == "ram")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='memory' AND name NOT LIKE '%cisco%' AND name NOT LIKE '%stick%' AND name NOT LIKE '%upgrade%' AND name NOT LIKE '%compaq%' AND name NOT LIKE '%acer%' AND name NOT LIKE '%brother%'");
	else if(sKey == "video")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='video card'");
	else if(sKey == "sound")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='MultiMedia' AND ss_cat='sound card'");
	else if(sKey == "hd")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='HDD'");
	else if(sKey == "fd")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='FDD'");
	else if(sKey == "cd")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Optical Disk Drive' AND name NOT LIKE '%RW%' AND name NOT LIKE '%writer%'");
	else if(sKey == "cdrw")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Optical Disk Drive' AND (name LIKE '%RW%' OR name LIKE '%writer%')");
	else if(sKey == "modem")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Modem'");
	else if(sKey == "nic")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='network'");
	else if(sKey == "monitor")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Monitor'");
	else if(sKey == "pccase")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Case'");
	else if(sKey == "kb")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='keyboard'");
	else if(sKey == "mouse")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Mouse'");
	else if(sKey == "speaker")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='MultiMedia' AND ss_cat='Speaker'");
	else if(sKey == "printer")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat LIKE '%printer%'");
	else if(sKey == "scanner")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat LIKE '%scanner%'");
	else if(sKey == "os")
		sc += GetQCat(sKey, "cat='software' AND s_cat LIKE 'Windows%'");
	else
		return "error";
		*/
		/********************************************** MODIFY ************************************************/
		string sc = "SELECT code, name, manual_cost_nzd , price1 , rate, level_rate1 FROM code_relations WHERE ";
	if(sKey == "cpu")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='cpu' AND name NOT LIKE '%bundle%' AND name NOT LIKE '%kit%' ");
	else if(sKey == "mb")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='motherboard'");
	else if(sKey == "ram")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='memory' AND name NOT LIKE '%cisco%' AND name NOT LIKE '%stick%' AND name NOT LIKE '%upgrade%' AND name NOT LIKE '%compaq%' AND name NOT LIKE '%acer%' AND name NOT LIKE '%brother%'");
	else if(sKey == "video")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='video card'");
	else if(sKey == "sound")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='MultiMedia' AND ss_cat='sound card'");
	else if(sKey == "hd")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='HDD'");
	else if(sKey == "fd")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='FDD'");
	else if(sKey == "cd")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Optical Disk Drive' AND name NOT LIKE '%RW%' AND name NOT LIKE '%writer%'");
	else if(sKey == "cdrw")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Optical Disk Drive' AND (name LIKE '%RW%' OR name LIKE '%writer%')");
	else if(sKey == "modem")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Modem'");
	else if(sKey == "nic")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='network'");
	else if(sKey == "monitor")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Monitor'");
	else if(sKey == "pccase")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Case'");
	else if(sKey == "kb")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='keyboard'");
	else if(sKey == "mouse")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='Mouse'");
	else if(sKey == "speaker")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat='MultiMedia' AND ss_cat='Speaker'");
	else if(sKey == "printer")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat LIKE '%printer%'");
	else if(sKey == "scanner")
		sc += GetQCat(sKey, "cat='hardware' AND s_cat LIKE '%scanner%'");
	else if(sKey == "os")
		sc += GetQCat(sKey, "cat='software' AND s_cat LIKE 'Windows%'");
	else
		return "error";
		
	sc += " ORDER BY name";
	return sc;
}

string GetQCat(string part)
{
	return GetQCat(part, "");
}

string GetQCat(string part, string sDefault)
{
	string s = "";
	string sc = "SELECT cat FROM q_cat WHERE part='" + part + "' ";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		DataSet ds = new DataSet();
		rows = myCommand.Fill(ds);
		if(rows > 0)
			s = ds.Tables[0].Rows[0].ItemArray[0].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	if(rows == 0 && sDefault != "")
	{
		s = sDefault;
		sc = "INSERT INTO q_cat (part, cat) VALUES('" + part + "', '" + EncodeQuote(s) + "')";
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
		}
	}
	return s;
}

bool SetQCat(string part, string cat)
{
	string sc = "UPDATE q_cat SET cat='";
	sc += EncodeQuote(cat);
	sc += "' WHERE part='" + part + "' ";
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

bool CheckInstallationCharge(bool bDoAdd)
{
	if(Request.Form["ship_as_parts"] == "on")
		m_bShipAsParts = true;
	else if(Session["q_ship_as_parts"] != null)
		m_bShipAsParts = (bool)Session["q_ship_as_parts"];
	bool bAlreadyAdded = false;
	bool bHAdded = false; //check hardware installation as well

	string code = dtQ.Rows[0]["os"].ToString();
	if(MyIntParse(code) > 0)// && !m_bShipAsParts) //with OS
		m_bWithOS = true;
	CheckShoppingCart();
	for(int i=dtCart.Rows.Count-1; i>=0; i--)
	{
		if(dtCart.Rows[i]["site"].ToString() != m_sCompanyName)
			continue;

		DataRow drp = null;
		string code_cart = dtCart.Rows[i]["code"].ToString();
		if(code_cart == m_labourCodeS) //found
		{
//			if(!bDoAdd) //restore qutoe
//				m_bInstallOS = true;
			if(!m_bInstallOS || m_bShipAsParts) //no OS
			{
				dtCart.Rows.RemoveAt(i);
				m_bInstallOS = false;
				Session["q_install_os"] = false;
			}
			else
				bAlreadyAdded = true;
		}
		else if(code_cart == m_labourCodeH)
		{
			if(m_bShipAsParts)
				dtCart.Rows.RemoveAt(i);
			else
				bHAdded = true;
		}
	}
	if(!bDoAdd)
		return true;

	if(!bHAdded && g_bSysQuoteAddHardwareLabourCharge && !m_bShipAsParts)
		AddHardwareLabourCharge();
	if(m_bInstallOS && !bAlreadyAdded)
		AddSoftwareLabourCharge();
	return true;
}

bool AddHardwareLabourCharge()
{
	DataRow dr = null;
	if(!GetProduct(m_labourCodeH, ref dr))
		return false;
	double dPrice = 0;
	if(!GetItemPrice(m_labourCodeH, "1", ref dPrice))
		return false;
	AddToCart(m_labourCodeH, dr["supplier"].ToString(), dr["supplier_code"].ToString(), 
		"1", dr["supplier_price"].ToString(), dPrice.ToString(), dr["name"].ToString(), "");
	return true;
}

bool AddSoftwareLabourCharge()
{
	DataRow dr = null;
	if(!GetProduct(m_labourCodeS, ref dr))
		return false;
	double dPrice = 0;
	if(!GetItemPrice(m_labourCodeS, "1", ref dPrice))
		return false;
	AddToCart(m_labourCodeS, dr["supplier"].ToString(), dr["supplier_code"].ToString(), 
		"1", dr["supplier_price"].ToString(), dPrice.ToString(), dr["name"].ToString(), "");
	return true;
}

bool RemovePartFromSetup(string key, string code)
{
	if(key != "video") //delete obsolete item
	{
		string sc = "";
		if(key == "cpu")
			sc = "DELETE FROM q_mb WHERE parent=" + code;
		else if(key == "mb" || key == "ram")
			sc = "DELETE FROM q_" + key + " WHERE code=" + code;
		else
			sc = "DELETE FROM q_flat WHERE " + key + "=" + code;
//DEBUG("do delete, sc=", sc);
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
//			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}
</script>


<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_barcode = "";
string m_pwd = "";
string m_code = "";
string m_card_id = "";
string m_name = "";
string m_fileName = "";
string m_pic = "";
string m_orderID = "";

void Page_Load(Object Src, EventArgs E ) 
{
//	TS_PageLoad(); //do common things, LogVisit etc...
//	if(!SecurityCheck(""))
//		return;
	myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);

//DEBUG("agent=", Request.ServerVariables["HTTP_USER_AGENT"]);

	m_type = g("t");
	m_action = g("a");
	m_cmd = p("cmd");
	m_barcode = g("c");

	m_pwd = p("pwd");
	if(p("pwd") == "")
		m_pwd = g("pwd");
	string pwd = GetSiteSettings("xcode_password", "6232176");
	if(m_pwd != pwd)
	{
		Response.Write("incorrect password");
		return;
	}

	if(Request.Form["barcode"] != null && Request.Form["barcode"] != "")
		m_barcode = p("barcode");
	else if(Request.QueryString["barcode"] != null && Request.QueryString["barcode"] != "")
		m_barcode = g("barcode");
	
	m_card_id = g("cid");
	if(Request.Form["card_id"] != null && Request.Form["card_id"] != "")
		m_card_id = p("card_id");
	if(m_card_id != "")
		Session["card_id"] = m_card_id;
	else if(Session["card_id"] != null)
		m_card_id = Session["card_id"].ToString();

	if(m_cmd == "upload")
	{
		DoUploadFile();
		return;
	}
	else if(m_cmd == "save")
	{
		DoSaveItem();
		return;
	}
	
	if(m_barcode != "")
	{
		ResponseItem();
		return;
	}
	
	Response.Write("fin" + m_cmd);
}
bool ResponseItem()
{
	string name = "";
	string code = "0";
	string price = "";
	int nRows = 0;
	string sc = " SELECT cr.name, cr.code, cr.level_price0 ";
	sc += " FROM code_relations cr ";
	sc += " JOIN barcode b ON b.item_code = cr.code ";
	sc += " WHERE b.barcode = '" + EncodeQuote(m_barcode) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "detail");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
	{
		name = "new item?";
	}
	else
	{
		
		DataRow dr = ds.Tables["detail"].Rows[0];
		name = dr["name"].ToString().Replace(",", " ");
		code = dr["code"].ToString();
		double dPrice = MyDoubleParse(dr["level_price0"].ToString());
		price = Math.Round(dPrice, 2).ToString();
	}
	
	sc = " SELECT id, name FROM branch WHERE activated = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = "";
	for(int i=0; i<nRows; i++)
	{
		string bid = ds.Tables["branch"].Rows[i]["id"].ToString();
		sc += " IF NOT EXISTS(SELECT id FROM stock_qty WHERE code = " + code + " AND branch_id = " + bid + ") ";
		sc += " INSERT INTO stock_qty (code, branch_id, qty) VALUES(" + code + ", " + bid + ", 0) ";
	}
	if(sc == "")
	{
		Response.Write("");
		Response.End();
		return true;
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
	
	sc = " SELECT sq.qty, b.name AS branch_name, sq.branch_id ";
	sc += " FROM stock_qty sq ";
	sc += " JOIN branch b ON b.id = sq.branch_id AND b.activated = 1 ";
	sc += " WHERE 1 = 1 ";
	sc += " AND sq.code = " + code + " ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "stock");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
	{
		Response.Write("");
		Response.End();
		return true;
	}

	string s = "";
	s += code + "," + name + "," + price;
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["stock"].Rows[i];
		string branch_name = dr["branch_name"].ToString().Replace(",", " ");
		string branch_id = dr["branch_id"].ToString();
		string stock = dr["qty"].ToString();
		if(s != "")
			s += "[ROW]";
		s += "" + branch_id + "," + branch_name + "," + stock;
	}
	Response.Write(s);
	return true;
}
bool DoSaveItem()
{
	string barcode = p("barcode").Trim();
	if(barcode == "")
		return false;
	
	int nRows = 0;
	string sc = "";
	sc = " SELECT TOP 1 item_code FROM barcode WHERE barcode = '" + EncodeQuote(barcode) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "detail");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
	{
		if(!DoAddItem())
			return false;
		return true;
	}
	
	DataRow dr = ds.Tables["detail"].Rows[0];
	string code = dr["item_code"].ToString();
	m_code = code;
	
	nRows = MyIntParse(p("rows"));
	string name = p("name");
	double dPrice = MyMoneyParse(p("price"));
	
	sc = " UPDATE code_relations SET name = N'" + EncodeQuote(name) + "' ";
	sc += ", level_price0 = " + dPrice + " ";
	sc += " WHERE code = " + code;
	for(int i=0; i<nRows; i++)
	{
		string branch_id = p("bid" + i);
		double dQty = MyDoubleParse(p("qty" + i));
		sc += " UPDATE stock_qty SET qty = " + dQty + " WHERE branch_id = " + branch_id + " AND code = " + code + " ";
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
	Response.Write("done," + code);
	return true;	
}
bool DoAddItem()
{
	string sc = "";
	int nRows = MyIntParse(p("rows"));
	int nCode = GetNextCode();
	m_code = nCode.ToString();
	string id = m_code;
	string name = p("name");
	double dPrice = MyMoneyParse(p("price"));
	
	string supplier = "";
	string supplier_code = m_code;
	double dSupplier_price = dPrice;
	string brand = "";
	string cat = "New";
	string s_cat = "";
	string ss_cat = "";
	string hot = "0";
	string skip	= "0"; 
	string isnew	= "0";

	sc = " INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, level_price0, code, name, brand, ";
	sc += " cat, s_cat, ss_cat, hot, skip ";
	sc += ") VALUES('" + id + "', '" + supplier + "', '" + supplier_code + "', " + dSupplier_price + ", " + dPrice + ", " + m_code;
	sc += ", N'" + name + "', N'" + brand + "', N'" + cat + "', N'" + s_cat + "', N'" + ss_cat + "', 0, 0)";
	sc += " INSERT INTO product(code, supplier, supplier_code, name, brand, cat, s_cat, ss_cat, price, supplier_price) VALUES( ";
	sc += m_code + ", '" + EncodeQuote(supplier) + "', '" + EncodeQuote(supplier_code) + "' ";
	sc += ", N'" + EncodeQuote(name) + "' ";
	sc += ", N'" + EncodeQuote(brand) + "' ";
	sc += ", N'" + EncodeQuote(cat) + "' ";
	sc += ", N'" + EncodeQuote(s_cat) + "' ";
	sc += ", N'" + EncodeQuote(ss_cat) + "' ";
	sc += ", " + dPrice + ", " + dSupplier_price;
	sc += ") ";
	sc += " INSERT INTO barcode (item_code, barcode) VALUES(" + m_code + ", '" + EncodeQuote(m_barcode) + "') ";
	for(int i=0; i<nRows; i++)
	{
		string branch_id = p("bid" + i);
		double dQty = MyDoubleParse(p("qty" + i));
		sc += " INSERT INTO stock_qty (branch_id, code, qty) VALUES(" + branch_id + ", " + m_code + ", " + dQty + ") ";
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
	Response.Write("done," + m_code);
	return true;	
}
void DoUploadFile()
{
	string code = p("code");
	string fn = code + ".jpg";
//fn = "test.jpg";
	try
	{
		HttpPostedFile file = Request.Files.Count > 0 ? Request.Files[0] : null;
		if (file == null)
		{
			Response.Write("No file found");
			Response.End();
			return;
		}
		else
		{
			//Get an entry for file upload
//			string fileName = Utility.GetID(IDType.File);
//			String[] names = file.FileName.Split('.');
//			string src = fileName + "." + names[names.Length - 1];
			file.SaveAs(Server.MapPath("../pi/") + fn);
			Response.Write("Success");
//			Response.End();
			return;
		}
	}
	catch (Exception e)
	{
		Response.Write("Failure, e=" + e.ToString());
//		Response.End();
	}
}
</script>

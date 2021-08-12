<!-- #include file="cart.cs" -->

<script runat=server>

string m_sKitID = "";
string m_sKitTerm = "Kit";
string m_sKitName = "";
string m_sKitDetails = "";
string m_sKitWarranty = "";

double m_dKitPrice = 0;
double m_dKitRate = 1;
double m_dKitItemBasePriceTotal = 0;
double m_dKitItemLastTotal = 0;

bool m_bKitHot = false;
bool m_bKitInactive = true;
bool m_bKitAutoUpdatePrice = true;
bool m_bKitSpecial = false;
bool m_bItemMissing = false;

DataSet dskit = new DataSet();
DataTable dtKit = new DataTable();

bool InitKit()
{
	m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);
	CheckKitTable();
	CheckShoppingCart();
	return true;
}

void CheckKitTable()
{
	if(Session["kit_table" + m_ssid] == null)
	{
		dtKit.Columns.Add(new DataColumn("kid", typeof(String)));
		dtKit.Columns.Add(new DataColumn("id", typeof(String)));
		dtKit.Columns.Add(new DataColumn("seq", typeof(Double)));
		dtKit.Columns.Add(new DataColumn("code", typeof(String)));
		dtKit.Columns.Add(new DataColumn("name", typeof(String)));
		dtKit.Columns.Add(new DataColumn("price", typeof(String)));
		dtKit.Columns.Add(new DataColumn("rate", typeof(String)));
		dtKit.Columns.Add(new DataColumn("qty", typeof(String)));
		dtKit.Columns.Add(new DataColumn("note", typeof(String)));
		Session["kit_table" + m_ssid] = dtKit;
	}
	else
	{
		dtKit = (DataTable)Session["kit_table" + m_ssid];
	}
}
bool GetKit(string kit_id)
{
	return GetKit(kit_id, "");
}

bool GetKit(string kit_id, string Order_kitID)
{
	if(dskit.Tables["kit"] != null)
		dskit.Tables["kit"].Clear();
	if(dskit.Tables["kit_item"] != null)
		dskit.Tables["kit_item"].Clear();

/*	string sc = " SELECT * FROM kit WHERE 1=1 ";
	if(m_sSite != "admin")
		sc += " AND inactive=0 ";
	sc += " AND id = " + kit_id;
*/
	bool bOrderKit = false;
	if(Order_kitID != "" && Order_kitID != null && TSIsDigit(Order_kitID))
		bOrderKit = true;

	string sc = " SELECT k.* ";
	if(bOrderKit)
		sc += " , ok.commit_price ";
	sc += " FROM kit k ";
	if(bOrderKit)
	{
		sc += " JOIN order_kit ok ON ok.kit_id = k.id AND ok.id = "+ Order_kitID +"";
	}
	sc += " WHERE 1=1 ";
	if(m_sSite != "admin")
		sc += " AND k.inactive=0 ";
	sc += " AND k.id = " + kit_id;

//DEBUG(" kitsc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dskit, "kit") == 0)
		{
			Response.Write("<br><center><h3>" + m_sKitTerm + " #" + kit_id + " not found.");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = dskit.Tables["kit"].Rows[0];
	m_sKitName = dr["name"].ToString();
	m_sKitDetails = dr["details"].ToString();
	m_sKitWarranty = dr["warranty"].ToString();
	if(bOrderKit)
		m_dKitPrice = MyDoubleParse(dr["commit_price"].ToString());
	else
		m_dKitPrice = MyDoubleParse(dr["price"].ToString());
	m_dKitItemLastTotal = MyDoubleParse(dr["last_item_total"].ToString());
	m_dKitRate = MyDoubleParse(dr["rate"].ToString());
	m_bKitInactive = MyBooleanParse(dr["inactive"].ToString());
	m_bKitAutoUpdatePrice = MyBooleanParse(dr["auto_update_price"].ToString());

	sc = " SELECT k.*, c.name, c.weight, c.skip FROM kit_item k JOIN code_relations c ON c.code=k.code WHERE k.id = " + kit_id;
	sc += " ORDER BY k.seq ";
//DEBUG("kitsw =", sc );
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dskit, "kit_item");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	double dTotal = 0;
	for(int i=0; i<dskit.Tables["kit_item"].Rows.Count; i++)
	{
		dr = dskit.Tables["kit_item"].Rows[i];
		if(MyBooleanParse(dr["skip"].ToString()))
			m_bItemMissing = true; //if missing then stop updating price

		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();

		double dPrice = GetSalesPriceForDealer(code, qty, "1", "0"); //all level one price
		dTotal += dPrice * MyIntParse(qty);
	}
	m_dKitItemBasePriceTotal = dTotal;
//DEBUG("m_dkit item s=", m_dKitItemBasePriceTotal.ToString());
/*
	//temperary
	if(dTotal != m_dKitPrice) //update price
	{
		m_dKitPrice = dTotal;
		sc = " UPDATE kit SET price = " + dTotal + " WHERE id=" + kit_id;
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
	}
*/
	return true;
}

bool DoAddKit(string kit_id, int nQty)
{
	return DoAddKit(kit_id, MyDoubleParse(nQty.ToString()), "");
}

bool DoAddKit(string kit_id, double dQty, string Order_kitID)
{
	if(!GetKit(kit_id, Order_kitID))
		return false;
	
	if(m_bKitInactive && m_sSite != "admin")
	{
		Response.Write("<br><center><h2>Inactive " + m_sKitTerm + ", buy failed.");
		Response.End();
		return false;
	}

	DataRow dr = dtCart.NewRow();
	dr["site"] = m_sCompanyName;
	dr["quantity"] = dQty.ToString();
	dr["code"] = kit_id;
	dr["name"] = m_sKitName;
//	dr["supplier"] = drp["supplier"].ToString();
//	dr["supplier_code"] = drp["supplier_code"].ToString();
//	dr["supplierPrice"] = drp["supplier_price"].ToString();
	dr["salesPrice"] = m_dKitPrice;
	dr["kit"] = "1";
	dr["system"] = "0";
	dr["used"] = "0";
	dtCart.Rows.Add(dr);

	return true;
}

bool RecordKitToOrder(string order_number, string kit_id, string kit_name, string sQty, double dSalesPrice, string branchID)
{
	if(!GetKit(kit_id))
		return false;

	string krid = "";
	int nKitQty = MyIntParse(sQty);

	if(dskit.Tables["kit_id"] != null)
		dskit.Tables["kit_id"].Clear();

	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO order_kit (id, kit_id, qty, name, details, warranty, base_selling_price, commit_price) ";
	sc += " VALUES(" + order_number + ", " + kit_id + ", " + nKitQty;
	sc += ", '" + EncodeQuote(kit_name) + "' ";
	sc += ", '" + EncodeQuote(m_sKitDetails) + "' ";
	sc += ", '" + EncodeQuote(m_sKitWarranty) + "' ";
	sc += ", " + m_dKitPrice;
	sc += ", " + dSalesPrice;
	sc += ") SELECT IDENT_CURRENT('order_kit') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dskit, "kit_id") == 1)
		{
			krid = dskit.Tables["kit_id"].Rows[0]["id"].ToString();
		}
		else
		{
			Response.Write("<br><center><h3>Error recording kit id</h3>");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	int i = 0;

	DataRow dr = null;

	//get total cost
	double dTotalCost = 0;
	for(i=0; i<dskit.Tables["kit_item"].Rows.Count; i++)
	{
		dr = dskit.Tables["kit_item"].Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
		int nQty = MyIntParse(qty);
		
		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			return false;

		double dCost = MyDoubleParse(drp["last_cost"].ToString());
		dTotalCost += dCost * nQty;
	}

	double dRate = dSalesPrice / dTotalCost;

	double dAmountLeft = dSalesPrice;

	//split discount to individual items and record them with kit flag
	for(i=0; i<dskit.Tables["kit_item"].Rows.Count; i++)
	{
		dr = dskit.Tables["kit_item"].Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
		int nQty = MyIntParse(qty);
		
		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			return false;

		string supplier = drp["supplier"].ToString();
		string supplier_code = drp["supplier_code"].ToString();
		string item_name = drp["name"].ToString();
		if(item_name.Length > 254)
			item_name = item_name.Substring(0, 254);

		double dCost = MyDoubleParse(drp["last_cost"].ToString());
		double dPrice = Math.Round(dCost * dRate, 2);

		if(i == dskit.Tables["kit_item"].Rows.Count - 1)
		{
			dPrice = Math.Round(dAmountLeft / nQty, 2);
		}
		dAmountLeft -= dPrice * nQty;

		sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
		sc += ", commit_price, kit, krid) VALUES(" + order_number + ", " + code;
		sc += ", " + nQty * nKitQty + ", '" + EncodeQuote(item_name) + "', '" + drp["supplier"].ToString() + "' ";
		sc += ", '" + drp["supplier_code"].ToString() + "', " + dCost;
		sc += ", " + dPrice + ", 1, " + krid;
		sc += ") ";
//		if(g_bRetailVersion)
		{
			sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + dr["code"].ToString();
			sc += " AND branch_id = " + branchID;
			sc += ")";
			sc += " INSERT INTO stock_qty (code, branch_id, qty, allocated_stock) ";
			sc += " VALUES (" + dr["code"].ToString() + ", " + branchID + ", 0, " + nQty * nKitQty + ")"; 
			sc += " ELSE Update stock_qty SET ";
			sc += " allocated_stock = allocated_stock + " + nQty * nKitQty;
			sc += " WHERE code=" + dr["code"].ToString() + " AND branch_id = " + branchID;
		}
//		else
		{
			sc += " UPDATE product SET allocated_stock = allocated_stock + " + nQty * nKitQty;
			sc += " WHERE code=" + dr["code"].ToString() + " ";
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
	}
	if(dAmountLeft != 0)
	{
		sc = " UPDATE order_kit SET commit_price = commit_price - " + dAmountLeft;
		sc += " WHERE krid = " + krid;
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
//AlertAdmin("amountLeft=" + dAmountLeft.ToString());
	}
	return true;
}

string GetKitImgSrc(string kit_id)
{
	string s = "";
	string mi = "../pk/" + kit_id + ".jpg";
	if(m_sSite != "admin"){
	 mi = "../qpospro/pk/" + kit_id + ".jpg";
}
	if(File.Exists(Server.MapPath(mi)))
	{
		s = mi;
	}
	else
	{
		mi = "../pk/" + kit_id + ".gif";
		if(File.Exists(Server.MapPath(mi)))
			s = mi;
	}
	if(s == "")
		s = "../i/na.gif";
	return s;
}

bool Kit_NeedThisItem(string item_code, string action)
{
	string sc = " SELECT DISTINCT k.id ";
	sc += " FROM kit k JOIN kit_item i ON i.id=k.id ";
	sc += " WHERE i.code = " + item_code;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dskit, "needs") <= 0)
			return false;
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<br><center><font color=red><h4>Error, item cannot be " + action + ", you must remove it from the following " + m_sKitTerm + " first. </h4></font>");
	for(int i=0; i<dskit.Tables["needs"].Rows.Count; i++)
	{
		string kit_id = dskit.Tables["needs"].Rows[i]["id"].ToString();
		Response.Write("<a href=kit.aspx?id=" + kit_id + " class=o target=_blank title='click to edit'>" + m_sKitTerm + "# " + kit_id + "</a><br>");
	}
	return true;
}

bool Kit_AutoUpdatePrice(string item_code)
{
	string sc = " SELECT DISTINCT k.id ";
	sc += " FROM kit k JOIN kit_item i ON i.id=k.id ";
	sc += " WHERE k.inactive=0 AND k.auto_update_price=1 AND i.code = " + item_code;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dskit, "update") <= 0)
			return true;
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<dskit.Tables["update"].Rows.Count; i++)
	{
		string kit_id = dskit.Tables["update"].Rows[i]["id"].ToString();
		if(GetKit(kit_id)) //GetKit() will clean dskit.Tables["kit"]
		{
			if(m_bItemMissing)
				return false;

			if(m_dKitItemLastTotal == m_dKitItemBasePriceTotal)
				continue;

			sc = " UPDATE kit SET price = " + Math.Round(m_dKitItemBasePriceTotal * m_dKitRate, 0);
			sc += ", last_item_total = " + m_dKitItemBasePriceTotal;
			sc += " WHERE id = " + kit_id;
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
		//			ShowExp(sc, e);
		//			return false;
			}
		}
	}

	return true;
}

bool RecordKitToInvoice(string orderID, string invoice_number)
{
	DataSet dsi = new DataSet();
	
	string sc = "SELECT k.*, o.branch ";
	sc += " FROM order_kit k JOIN orders o ON k.id=o.id ";
	sc += " WHERE o.id=" + orderID;
	sc += " ORDER BY k.krid ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi, "kit");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	string branch_id = dsi.Tables["kit"].Rows[0]["branch"].ToString();

	for(int i=0; i<dsi.Tables["kit"].Rows.Count; i++)
	{
		DataRow dr = dsi.Tables["kit"].Rows[i];

		//record kit
		string kit_id = dr["kit_id"].ToString();
		string krid = dr["krid"].ToString();
		string qty = dr["qty"].ToString();
		string name = dr["name"].ToString();
		string details = dr["details"].ToString();
		string warranty = dr["warranty"].ToString();
		string base_selling_price = dr["base_selling_price"].ToString();
		string commit_price = dr["commit_price"].ToString();

		double dQty = MyDoubleParse(qty);

		sc = " INSERT INTO sales_kit (krid, invoice_number, kit_id, qty, name, details, warranty, base_selling_price, commit_price) ";
		sc += " VALUES (";
		sc += krid;
		sc += ", " + invoice_number;
		sc += ", " + kit_id;
		sc += ", " + qty;
		sc += ", '" + EncodeQuote(name) + "' ";
		sc += ", '" + EncodeQuote(details) + "' ";
		sc += ", '" + EncodeQuote(warranty) + "' ";
		sc += ", " + base_selling_price;
		sc += ", " + commit_price;
		sc += ") ";

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
	}
	return true;
}

bool DoAddKitAsItems(string kit_id, int nQty)
{
	if(!GetKit(kit_id))
		return false;
	
	if(m_bKitInactive && m_sSite != "admin")
	{
		Response.Write("<br><center><h2>Inactive " + m_sKitTerm + ", buy failed.");
		Response.End();
		return false;
	}

	for(int i=0; i<dskit.Tables["kit_item"].Rows.Count; i++)
	{
		DataRow dr = dskit.Tables["kit_item"].Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
		int q = MyIntParse(qty);
		q *= nQty;
		string dealer_level = "1";
		if(Session[m_sCompanyName + "dealer_level"] != null)
			dealer_level = Session[m_sCompanyName + "dealer_level"].ToString();
		double dPrice = GetSalesPriceForDealer(code, qty, dealer_level, Session["card_id"].ToString()); 
		AddToCart(code, q.ToString(), dPrice.ToString());
	}

	return true;
}

</script>
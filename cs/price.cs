<script runat=server>

bool UpdateSupplierPrice(string code, string id, double dsupplier_price, double dPrice)
{
	//update product (live update)
	StringBuilder sb = new StringBuilder();
	sb.Append("UPDATE product SET price=");
	sb.Append(dPrice);
	sb.Append(", supplier_price=");
	sb.Append(dsupplier_price);
	sb.Append(" WHERE code=");
	sb.Append(code);
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

	string sc = "UPDATE code_relations SET supplier_price=" + dsupplier_price;
	sc += " WHERE id='" + id + "'";
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
	return WritePriceHistory(code, dPrice);
}	

bool UpdateStock(string code, string id, string stock)
{
	//update product (live update)
	StringBuilder sb = new StringBuilder();
	sb.Append("UPDATE product SET stock=");
	sb.Append(stock);
	sb.Append(" WHERE code=");
	sb.Append(code);
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

bool UpdatePriceRate(string code, double dRate)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("UPDATE code_relations SET rate=");
	sb.Append(dRate);
	sb.Append(" WHERE code="); //not id, use code to set new rate to all suppliers on same product
	sb.Append(code);
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

bool WritePriceHistory(string code, double dPrice)
{
	return WritePriceHistory(code, dPrice, 0);
}

bool WritePriceHistory(string code, double dPrice, double dPrice_old)
{
	int nPrice_dropped = 0;
	
	if(dPrice > dPrice_old)
	{
		nPrice_dropped = -1; //price raised
	}
	else
	{
		nPrice_dropped = 1;
	}

	string sc = "INSERT INTO price_history (code, price, price_date) VALUES('";
	sc += code;
	sc += "', ";
	sc += dPrice;
	sc += ", ";
	sc += "GETDATE()";
	sc += ") ";

	if(dPrice_old != 0)
	{
		sc += " UPDATE product SET price_dropped = " + nPrice_dropped.ToString();
		sc += ", price_age = GETDATE() ";
		sc += " WHERE code=" + code;
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

bool AddSpecial(string code)
{
	string sc = "IF NOT EXISTS (SELECT code FROM specials WHERE code=" + code + ") ";
	sc += " INSERT INTO specials (code) VALUES(" + code + ")";
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

bool RemoveSpecial(string code)
{
	string sc = "DELETE FROM specials WHERE code=" + code;
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

double GetPriceRate(int code, double dsupplier_price)
{
	DataSet dsprice = new DataSet();
	double dRate = 1.1;
	int rows = 0;
	string sc = "SELECT rate FROM code_relations WHERE code=" + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsprice, "rate");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 2;
	}

	if(rows <= 0)
		dRate = GetDefaultPriceRate(dsupplier_price);
	else
		dRate = double.Parse(dsprice.Tables[0].Rows[0]["rate"].ToString());
	
	return dRate;
}

double GetPriceRate(string id, double dsupplier_price)
{
	DataSet dsprice = new DataSet();
	double dRate = 1.1;
	int rows = 0;
	string sc = "SELECT rate FROM code_relations WHERE id='" + id + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsprice, "rate");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 2;
	}

	if(rows <= 0)
		dRate = GetDefaultPriceRate(dsupplier_price);
	else
		dRate = double.Parse(dsprice.Tables[0].Rows[0]["rate"].ToString());
	
	return dRate;
}

double GetDefaultPriceRate(double dsupplier_price)
{
	return MyDoubleParse(GetSiteSettings("default_bottom_rate", "1.1"));
/*
	double dRate = 1.1;
	if(dsupplier_price > 2000)
		dRate = 1.08;
	if(dsupplier_price > 200)
		dRate = 1.08;
	if(dsupplier_price > 1000)
		dRate = 1.06;
	return dRate;
*/
}

double CalculateRetailPrice(double dsupplier_price, double dRate)
{
	if(dsupplier_price <= 0)
	{
//		Response.Write("<br><font size=+1 color=red><b>Warning, supplier price found 0, skip</b></font><br>");
//		Response.Write("0");
		return 99999;
	}

	double dPlus = 0;
//	if(dsupplier_price < 50)
//		dPlus = 10;
//	else if(dsupplier_price <= 200)
//		dPlus = 5;
	double rdPrice = dsupplier_price * dRate;
	double dPrice = Math.Round(rdPrice, 1);
	double cents = rdPrice - dPrice;
	if(cents >=5 )
		dPrice += 0.05;
//DEBUG("cost=" + dsupplier_price.ToString(), " rate="+dRate.ToString() + " price="+dPrice.ToString() + " plus="+dPlus.ToString());
	return dPrice + dPlus;
}

double CalculatePriceRate(double dsupplier_price, double dPrice)
{
	double dRate = 0;
	double dPlus = 0;

//	if(dsupplier_price < 50)
//		dPlus = 10;
//	else if(dsupplier_price <= 200)
//		dPlus = 5;

	double draw = dPrice - dPlus;
	dRate = draw / dsupplier_price;
	dRate = Math.Round(dRate, 2);
	return dRate;
}

bool AddToSkipTable(string code)
{
	DataSet dsprice = new DataSet();
	string sc = "SELECT supplier, supplier_code, supplier_price, stock, eta FROM product WHERE code=" + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dsprice, "skip");
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dsprice.Tables[0].Rows[0];
	return AddToSkipTable(dr["supplier"].ToString(), dr["supplier_code"].ToString(), dr["supplier_price"].ToString(), dr["stock"].ToString(), dr["eta"].ToString(), "");
}

bool UpdateSkipTable(string supplier, string supplier_code, string supplier_price, 
	string stock, string eta, string details)
{
	return UpdateSkipTable(supplier, supplier_code, supplier_price, "", stock, eta, details);
}

bool UpdateSkipTable(string supplier, string supplier_code, string supplier_price, 
	string rrp, string stock, string eta, string details)
{
	double dsupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dRate = GetPriceRate(supplier + supplier_code, dsupplier_price);
	if(dRate >= 2)
		return false;
	double dPrice = CalculateRetailPrice(dsupplier_price, dRate);
	if(rrp != "" && rrp != "0")
	{
		try
		{
			dPrice = double.Parse(rrp, NumberStyles.Currency, null);
//DEBUG("got rrp=", dPrice.ToString("c"));
		}
		catch (Exception e)
		{
		}
	}
	if(dPrice <= 0)
		return false;

	if(stock == null || stock == "")
		stock = "0";

	DataSet dsprice = new DataSet();
	string sc = "IF NOT EXISTS (SELECT * FROM product_skip WHERE id='" + supplier + supplier_code + "') ";
	sc += " INSERT INTO product_skip (id, stock, eta, supplier_price, price, details) VALUES('";
	sc += supplier + supplier_code + "', " + stock + ", '" + eta + "', " + dsupplier_price + ", " + dPrice;
	sc += ", '" + details + "') ";
	sc += " ELSE UPDATE product_skip SET supplier_price=";
	sc += dsupplier_price;
	sc += ", price=";
	sc += dPrice;
	sc += ", stock=";
	sc += stock;
	sc += ", eta='";
	sc += eta;
	sc += "' WHERE id='";
	sc += supplier + supplier_code;
	sc += "'";
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

bool AddToSkipTable(string supplier, string supplier_code, string supplier_price, 
	string stock, string eta, string details)
{
	double dsupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dRate = GetPriceRate(supplier + supplier_code, dsupplier_price);
	if(dRate >= 2)
		return false;
	double dPrice = CalculateRetailPrice(dsupplier_price, dRate);

	DataSet dsprice = new DataSet();

	int rows = 0;
	string sc = "SELECT STR(supplier_price)+STR(price)+STR(stock)+eta AS data FROM product_skip WHERE id='" + supplier + supplier_code + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsprice, "skip");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0) //exists, do update
	{
		sc = "UPDATE product_skip SET supplier_price=";
		sc += dsupplier_price;
		sc += ", price=";
		sc += dPrice;
		sc += ", stock=";
		sc += stock;
		sc += ", eta='";
		sc += eta;
		sc += "' WHERE id='";
		sc += supplier + supplier_code;
		sc += "'";
	}
	else //do insert
	{
		sc = "INSERT INTO product_skip (id, supplier_price, price, stock, eta, details) VALUES('";
		sc += supplier + supplier_code;
		sc += "', ";
		sc += dsupplier_price;
		sc += ", ";
		sc += dPrice;
		sc += ", ";
		sc += stock;
		sc += ", '";
		sc += eta;
		sc += "', '')";
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

<script runat=server>

DataSet dsfifo = new DataSet();

bool fifo_sales_update_cost(string invoice_number, string code, string commit_price, string branch, double dQty)
{
	return fifo_sales_update_cost(invoice_number, code, commit_price, branch, MyIntParse(Math.Round(dQty, 0).ToString()));
}

bool fifo_sales_update_cost(string invoice_number, string code, string commit_price, string branch, int nQty)
{
	double dCost = MyDoubleParse(commit_price); //set zero profit if no stock_cost record found
	string need_update_cost = "0";
	int nStock = 0;

	if(nQty == 0)
		return true;
	if(nQty < 0)
	{
		if(!fifo_do_sales_refund(invoice_number, code, commit_price, branch, nQty, ref dCost, ref nStock))
			return false;
	}
	else
	{
		if(!fifo_do_sales(code, branch, nQty, invoice_number, ref dCost, ref need_update_cost, ref nStock))
			return false;
	}

//	if(g_bUseAVGCost)
		dCost = MyDoubleParse(doReturnAVGCost(code, branch, ref dCost));


	string sc = " UPDATE sales SET supplier_price = " + dCost;
	sc += ", stock_at_sales = " + nStock;
	sc += " WHERE invoice_number = " + invoice_number + " AND code = " + code;

	sc += " INSERT INTO sales_cost (invoice_number, code, qty, cost, need_update_cost) ";
	sc += " VALUES(" + invoice_number;
	sc += ", " + code;
	sc += ", " + nQty;
	sc += ", " + dCost;
	sc += ", " + need_update_cost;
	sc += ")";
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

bool fifo_do_sales(string code, string branch, int nQty, string invoice_number, ref double dAveCost, ref string need_update_cost, ref int nStock)
{
	if(dsfifo.Tables["sales_cost"] != null)
		dsfifo.Tables["sales_cost"].Clear();

	int rows = 0;
	string sc = " SELECT id, cost ";
	sc += " FROM stock_cost ";
	sc += " WHERE code = " + code + " AND branch = " + branch;
	sc += " AND instock = 1 ";
	sc += " ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfifo, "sales_cost");
		if(rows < nQty) //one row reprents one item(qty is 1)
		{
			need_update_cost = "1"; //cost unkonw, leave dAveCost
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	nStock = rows; //one row one qty

	double dTotal = 0;
	sc = "";
	for(int i=0; i<nQty; i++)
	{
		DataRow dr = dsfifo.Tables["sales_cost"].Rows[i];
		string id = dr["id"].ToString();
		double cost = MyDoubleParse(dr["cost"].ToString());
		dTotal += cost;
		//sc += " UPDATE stock_cost SET instock = 0, history = ISNULL(history, '') + ' sold(inv#" + invoice_number + "); ' ";
		sc += " UPDATE stock_cost SET instock = 0, history = 'sold(inv#" + invoice_number + ");' ";
		sc += " WHERE id = " + id; 
	}
	dAveCost = Math.Round(dTotal / nQty, 2);

	if(sc == "")
		return true;
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

bool fifo_do_sales_refund(string invoice_number, string code, string commit_price, string branch, int nQty, ref double dAveCost, ref int nStock)
{
	//get current stock
	if(dsfifo.Tables["sales_cost"] != null)
		dsfifo.Tables["sales_cost"].Clear();

	int rows = 0;
	string sc = " SELECT id, cost ";
	sc += " FROM stock_cost ";
	sc += " WHERE code = " + code + " AND branch = " + branch;
	sc += " AND instock = 1 ";
	sc += " ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfifo, "sales_cost");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	nStock = rows;

	//get sold
	if(dsfifo.Tables["refund_cost"] != null)
		dsfifo.Tables["refund_cost"].Clear();

	nQty = 0 - nQty;

	sc = " SELECT TOP " + nQty.ToString() + " id, cost ";
	sc += " FROM stock_cost ";
	sc += " WHERE code = " + code + " AND branch = " + branch;
	sc += " AND instock = 0 ";
	sc += " ORDER BY id DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfifo, "refund_cost");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int i = 0;
	sc = "";
	double dTotal = 0;
	for(i=0; i<rows; i++) //refund sales
	{
		DataRow dr = dsfifo.Tables["refund_cost"].Rows[i];
		string id = dr["id"].ToString();
		double cost = MyDoubleParse(dr["cost"].ToString());
		dTotal += cost;
		//sc += " UPDATE stock_cost SET instock = 1, history = ISNULL(history, '') + 'refund(inv#" + invoice_number + "); ' ";
		sc += " UPDATE stock_cost SET instock = 1, history = 'refund(inv#" + invoice_number + "); ' ";
		sc += " WHERE id = " + id; 
	}
	for(; i<nQty; i++) //out of sales record, re-stock as purchase, cost=0
	{
		sc += " INSERT INTO stock_cost (code, branch, cost, history) ";
		sc += " VALUES(" + code + ", " + branch + ", 0, 'refund(inv#" + invoice_number + "),out of cost records; ') ";
	}
	dAveCost = Math.Round(dTotal / nQty, 2);

	if(sc == "")
		return true;
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

bool fifo_do_purchase_refund(string code, string branch, int nQty)
{
	if(dsfifo.Tables["refund_cost"] != null)
		dsfifo.Tables["refund_cost"].Clear();

	nQty = 0 - nQty;

	int rows = 0;
	string sc = " SELECT TOP " + nQty.ToString() + " id, cost ";
	sc += " FROM stock_cost ";
	sc += " WHERE code = " + code + " AND branch = " + branch;
	sc += " AND instock = 1 ";
	sc += " ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfifo, "refund_cost");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";
	for(int i=0; i<rows; i++) //refund purchaes
	{
		string id = dsfifo.Tables["refund_cost"].Rows[i]["id"].ToString();
		sc += " DELETE FROM stock_cost ";
		sc += " WHERE id = " + id; 
	}

	if(sc == "")
		return true;
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

bool fifo_purchase_update_cost(string purchase_id)
{
	DataSet dsfp = new DataSet();
	int rows = 0;
	string sc = " SELECT p.branch_id, i.code, i.qty, i.price ";
	sc += " FROM purchase p JOIN purchase_item i ON i.id = p.id ";
	sc += " WHERE p.id = " + purchase_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfp, "purchase");
		if(rows <= 0)
		{
			return true; //empty purchase order?
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	//record stock cost
	sc = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dsfp.Tables["purchase"].Rows[i];
		string branch = dr["branch_id"].ToString();
		string code = dr["code"].ToString();
		int nQty = MyIntParse(dr["qty"].ToString());
		double dCost = MyDoubleParse(dr["price"].ToString());
		sc = "";
		for(int j=0; j<nQty; j++)
		{
			sc += " INSERT INTO stock_cost (code, branch, cost, purchase_id) VALUES(";
			sc += code + ", " + branch + ", " + dCost + ", " + purchase_id + ") ";
		}
			//compatible with old or POS version, update supplier_price AS last_cost to be used by profit report
			sc += " UPDATE code_relations SET supplier_price = " + dCost + " WHERE code = " + code;
			sc += " UPDATE product SET supplier_price = " + dCost + " WHERE code = " + code;
		//}
		if(sc != "")
		{
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
		if(nQty <= 0) //purchase refund
		{
			if(fifo_do_purchase_refund(code, branch, nQty))
				return false;
		}
		//check sales_cost and stock_loss see if any records waiting to update
//		if(!fifo_purchase_update_sales_cost(code, branch))
//			return false;
//		if(!fifo_purchase_update_stock_loss_cost(code, branch))
//			return false;
	}
	return true;
}

bool fifo_purchase_update_sales_cost(string code, string branch)
{
	int nQty = 0;
	DataSet dsfp = new DataSet();
	int rows = 0;

	//check how many sales need update cost
	string sc = " SELECT s.* ";
	sc += " FROM sales_cost s JOIN invoice i ON i.invoice_number = s.invoice_number ";
	sc += " WHERE i.branch = " + branch;
	sc += " AND s.code = " + code + " AND s.need_update_cost = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfp, "sales_cost");
		if(rows <= 0) //how many records?
		{
			return true; //no record need to update
		}
		for(int i=0; i<rows; i++) //how many qty?
		{
			nQty += MyIntParse(dsfp.Tables["sales_cost"].Rows[i]["qty"].ToString());
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//get how many qty in stock that have cost record
	int rows1 = 0;
	sc = " SELECT * FROM stock_cost ";
	sc += " WHERE branch = " + branch;
	sc += " AND code = " + code + " AND instock = 1 ";
	sc += " ORDER BY id DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows1 = myAdapter.Fill(dsfp, "stock_cost");
		if(rows1 < nQty)
		{
			return true; //not enought record to update cost, leave for next purchase
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";
	int m = 0;
	for(int i=0; i<rows; i++) //updat cost by each invoice
	{
		DataRow dr = dsfp.Tables["sales_cost"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string id = dr["id"].ToString();
		int qty = MyIntParse(dr["qty"].ToString());
		if(qty <= 0)
			continue; //protection
		double dTotal = 0;
		int n = 0;
		for(; m<rows1; m++)
		{
			n++;
			DataRow drstock = dsfp.Tables["stock_cost"].Rows[m];
			string idstock = drstock["id"].ToString();
			double cost = MyDoubleParse(drstock["cost"].ToString());
			dTotal += cost;
			sc += " UPDATE stock_cost SET instock = 0, history = ISNULL(history, '') + 'sold(inv#" + invoice_number + "); ' ";
			sc += " WHERE id = " + idstock;
			if(n >= qty)
				break;
		}
		double dAveCost = Math.Round(dTotal / nQty, 2);
		sc += " UPDATE sales_cost SET cost = " + dAveCost;
		sc += ", need_update_cost = 0 ";
		sc += " WHERE id = " + id;
		sc += " UPDATE sales SET supplier_price = " + dAveCost;
		sc += " WHERE invoice_number = " + invoice_number + " AND code = " + code;
	}

	if(sc == "")
		return true;
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

bool fifo_purchase_update_stock_loss_cost(string code, string branch)
{
	int nQty = 0;
	DataSet dsfp = new DataSet();
	int rows = 0;

	//check how many stock loss records need update cost
	string sc = " SELECT * ";
	sc += " FROM stock_loss ";
	sc += " WHERE branch = " + branch;
	sc += " AND code = " + code + " AND need_update_cost = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfp, "sales_cost");
		if(rows <= 0) //how many records?
		{
			return true; //no record need to update
		}
		for(int i=0; i<rows; i++) //how many qty?
		{
			nQty += MyIntParse(dsftp.Tables["sales_cost"].Rows[i]["qty"].ToString());
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//get how many qty in stock that have cost record
	int rows1 = 0;
	sc = " SELECT * FROM stock_cost ";
	sc += " WHERE branch = " + branch;
	sc += " AND code = " + code + " AND instock = 1 ";
	sc += " ORDER BY id DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows1 = myAdapter.Fill(dsfp, "stock_cost");
		if(rows1 < nQty)
		{
			return true; //not enought record to update cost, leave for next purchase
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";
	int m = 0;
	for(int i=0; i<rows; i++) //updat cost by each invoice
	{
		DataRow dr = dsfp.Tables["sales_cost"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string kid = dr["kid"].ToString();
		string id = dr["id"].ToString();
		int qty = MyIntParse(dr["qty"].ToString());
		if(qty <= 0)
			continue; //protection
		double dTotal = 0;
		int n = 0;
		for(; m<rows1; m++)
		{
			n++;
			DataRow drstock = dsfp.Tables["stock_cost"].Rows[i];
			string idstock = drstock["id"].ToString();
			double cost = MyDoubleParse(drstock["cost"].ToString());
			dTotal += cost;
			sc += " UPDATE stock_cost SET instock = 0, history = ISNULL(history, '') + 'stock_loss(id#" + id + "); ' ";
			sc += " WHERE id = " + idstock;
			if(n >= qty)
				break;
		}
		double dAveCost = Math.Round(dTotal / nQty, 2);
		sc += " UPDATE stock_loss SET cost = " + dAveCost;
		sc += ", need_update_cost = 0 ";
		sc += " WHERE kid = " + kid;
	}

	if(sc == "")
		return true;
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

bool fifo_writeStockLoss(string log_id, string code, string branch, int nQty, double dCost, string need_update_cost)
{
	string sc = " INSERT INTO stock_loss ";
	sc += " (id, code, branch, qty, cost, need_update_cost) VALUES( ";
	sc += log_id;
	sc += ", " + code;
	sc += ", " + branch;
	sc += ", " + nQty;
	sc += ", " + dCost;
	sc += ", " + need_update_cost;
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
	return true;
}

bool fifo_RecordStockLoss(string code, string branch, int nQty, string log_id)
{
	if(nQty < 0)
		return fifo_record_stock_gain(code, branch, nQty, log_id);

	double dAveCost = 0;
	string need_update_cost = "0";

	if(dsfifo.Tables["sales_cost"] != null)
		dsfifo.Tables["sales_cost"].Clear();

	int rows = 0;
	string sc = " SELECT id, cost ";
	sc += " FROM stock_cost ";
	sc += " WHERE code = " + code + " AND branch = " + branch;
	sc += " AND instock = 1 ";
	sc += " ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfifo, "sales_cost");
		if(rows < nQty) //one row reprents one item(qty is 1)
		{
			need_update_cost = "1"; //cost unkonw, needs update later when purchase
			return fifo_writeStockLoss(log_id, code, branch, nQty, dAveCost, need_update_cost);
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	double dTotal = 0;
	sc = "";
	for(int i=0; i<nQty; i++)
	{
		DataRow dr = dsfifo.Tables["sales_cost"].Rows[i];
		string id = dr["id"].ToString();
		double cost = MyDoubleParse(dr["cost"].ToString());
		dTotal += cost;
		sc += " UPDATE stock_cost SET instock = 0, history = ISNULL(history, '') + 'stockloss(id#" + log_id + "); ' ";
		sc += " WHERE id = " + id; 
	}
	dAveCost = Math.Round(dTotal / nQty, 2);
	if(!fifo_writeStockLoss(log_id, code, branch, nQty, dAveCost, need_update_cost))
		return false;

	if(sc == "")
		return true;
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

bool fifo_record_stock_gain(string code, string branch, int nQty, string log_id)
{
	if(dsfifo.Tables["refund_cost"] != null)
		dsfifo.Tables["refund_cost"].Clear();

	nQty = 0 - nQty;

	int rows = 0;
	string sc = " SELECT TOP " + nQty.ToString() + " id, cost ";
	sc += " FROM stock_cost ";
	sc += " WHERE code = " + code + " AND branch = " + branch;
	sc += " AND instock = 0 ";
	sc += " ORDER BY id DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsfifo, "refund_cost");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int i = 0;
	sc = "";
	double dTotal = 0;
	for(i=0; i<rows; i++) //do stock gain
	{
		DataRow dr = dsfifo.Tables["refund_cost"].Rows[i];
		string id = dr["id"].ToString();
		double cost = MyDoubleParse(dr["cost"].ToString());
		dTotal += cost;
		sc += " UPDATE stock_cost SET instock = 1, history = ISNULL(history, '') + 'stock_gain(id#" + log_id + "); ' ";
		sc += " WHERE id = " + id; 
	}
	for(; i<nQty; i++) //out of sales record, re-stock as purchase, cost=0
	{
		sc += " INSERT INTO stock_cost (code, branch, cost, history) ";
		sc += " VALUES(" + code + ", " + branch + ", 0, 'stock_gain(id#" + log_id + "),out of cost records; ') ";
	}
	double dAveCost = Math.Round(dTotal / nQty, 2);
	string need_update_cost = "0";
	if(!fifo_writeStockLoss(log_id, code, branch, nQty, dAveCost, need_update_cost))
		return false;

	if(sc == "")
		return true;
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

bool fifo_updateStockQty(int qty, string code, string branch_id, string orderID)
{   
	string sc = "";
    int orderType=2;
        
    sc  = " SELECT type from orders WHERE id = " + orderID;
    try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		SqlDataReader reader = myCommand.ExecuteReader();
        while (reader.Read())
        {
            orderType = reader.GetInt32(0);
        }
	    reader.Close();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + code;
	sc += " AND branch_id = " + branch_id;
	sc += ")";
	sc += " INSERT INTO stock_qty (code, branch_id, qty, supplier_price) ";
	sc += " VALUES (" + code + ", " + branch_id + ", " + (0 - qty).ToString() + ", ";
	sc += GetSupPrice(code) + ") "; //" (SELECT supplier_price FROM code_relations WHERE code=" + code + ")) "; 
	sc += " ELSE Update stock_qty SET ";
	sc += "qty = qty - " + qty ;
    if(orderType!=1)
    {
        sc += ",allocated_stock = allocated_stock - " + qty;
    }
	sc += " WHERE code=" + code + " AND branch_id = " + branch_id;
	if(!g_bRetailVersion)
	{
		sc += " UPDATE product SET stock = stock - " + qty + ", allocated_stock = allocated_stock - " + qty;
		sc += " WHERE code=" + code;
	}
	else //retail version only update allocated stock in product table
	{
		sc += " UPDATE product SET allocated_stock = allocated_stock - " + qty;
		sc += " WHERE code=" + code;
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

string GetSupPrice(string code)
{
	if(dst.Tables["sup_price"] != null)
		dst.Tables["sup_price"].Clear();

	string s_price ="0";
	int nRows = 0;
	string sc = " SELECT supplier_price FROM code_relations WHERE code=" + code;	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst, "sup_price");
		if(nRows != 1)
		{
			return s_price;
		}
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return s_price;
	}
	s_price = dst.Tables["sup_price"].Rows[0]["supplier_price"].ToString();
	return s_price;
}

bool fifo_checkAC200Item(string invoice_number, string code, string supplier_code, string commit_price)
{
	if(supplier_code.ToLower() != "ac200")
		return true;

	double dPrice = MyDoubleParse(commit_price);
	dPrice *= 0.9;
	dPrice = Math.Round(dPrice, 2);
	string sc = " UPDATE sales SET supplier_price = " + dPrice;
	sc += " WHERE invoice_number=" + invoice_number + " AND code=" + code;	
	sc += " UPDATE sales_cost SET cost = " + dPrice + ", need_update_cost=0 ";
	sc += " WHERE invoice_number=" + invoice_number + " AND code=" + code;	
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

string doReturnAVGCost(string code, string branch_id, ref double dCost)
{
	if(dst.Tables["avg_cost"] != null)
	dst.Tables["avg_cost"].Clear();
	
	int rows = 0;
	//string sc = " SELECT average_cost FROM stock_qty WHERE code=" + code +" AND branch_id = "+ branch_id +"";	
	string sc = " SELECT average_cost FROM code_relations WHERE code=" + code +" ";	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "avg_cost");
		
	}
	catch (Exception e)
	{
		ShowExp("",e);
		return dCost.ToString();
	}
	if(rows > 0)
		return dst.Tables["avg_cost"].Rows[0]["average_cost"].ToString(); 

	//get last cost DW
	if(dst.Tables["last_cost"] != null)
	dst.Tables["last_cost"].Clear();
	sc = " SELECT TOP 1 price FROM purchase_item WHERE code = " + code + " ORDER BY kid DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "last_cost");
		
	}
	catch (Exception e)
	{
		ShowExp("",e);
		return dCost.ToString();
	}
	if(rows > 0)
		return dst.Tables["last_cost"].Rows[0]["price"].ToString(); 
	return "0";	
}
</script>

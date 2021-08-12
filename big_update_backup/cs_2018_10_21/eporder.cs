<!-- #include file="card_function.cs" -->
<script runat=server>

const string cols = "5";	//how many columns main table has, used to write colspan=
string m_orderNumber;
string m_id;
string m_new_id;
string m_type;
string m_status;
string m_freight;

int m_totalRows = 0;

int m_rows = 0;

int m_splits = 0;
int m_sp = 0; //current working point
int[] m_sr = new int[64];

int m_editQtyRow = -1;

bool m_bCreditReturn = false;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	m_type = Request.QueryString["t"];
	if(Request.Form["po_number"] != null)
		m_orderNumber = Request.Form["po_number"];
	else
		m_orderNumber = Request.QueryString["i"];
	
	m_id = Request.QueryString["id"];
	if(!TSIsDigit(m_id))
	{
		Response.Write("<h3>Wrong id : " + m_id + "</h3>");
		return;
	}

	if(Request.QueryString["eq"] != null)
		m_editQtyRow = MyIntParse(Request.QueryString["eq"]);

	if(Request.Form["cmd"] != null && Request.Form["rows"] != null)
		m_rows = MyIntParse(Request.Form["rows"]);

	if(Request.Form["cmd"] == "Split")
	{
		DoSplit();
		return;
	}
	else if(Request.Form["cmd"] == "BackOrder")
	{
		string status = GetEnumID("order_item_status", "Back Ordered");
		DoUpdateOrder(status);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eporder.aspx?id=" + m_new_id + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "OnHold")
	{
		string status = GetEnumID("order_item_status", "On Hold");
		DoUpdateOrder(status);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eporder.aspx?id=" + m_new_id + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Receive")
	{
		string status = GetEnumID("order_item_status", "Invoiced");
		DoUpdateOrder(status);
		CreateInvoice(m_new_id);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=eporder.aspx?t=freight&id=" + m_id + "&i=" + m_orderNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
//		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=inputsn.aspx?inv=" + m_billNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Credit")
	{
		m_bCreditReturn = true;
		string status = GetEnumID("order_item_status", "Returned");
		DoUpdateOrder(status);
		CreateInvoice(m_new_id);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=inputsn.aspx?inv=" + m_orderNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
//		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=esales.aspx?i=" + m_billNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else if(Request.Form["cmd"] == "Record") //record freight and supplier invoice number
	{
		DoRecordFreight();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=inputsn.aspx?inv=" + m_orderNumber + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	else
	{
		if(!GetOrder())
		{
			Response.Write("<h3>Order# " + m_orderNumber + " not found</h3>");
			return;
		}
	
		PrintAdminHeader();
		PrintAdminMenu();
		if(Request.QueryString["t"] == "freight")
		{
			PrintFreightForm();
		}
		else
		{
			PrintJavaFunctions();
			MyDrawTable();
		}
	}
	PrintAdminFooter();
}

void PrintFreightForm()
{
	Response.Write("<br><br><center>");
	Response.Write("<form action=eporder.aspx?id=" + m_id + "&i=" + m_orderNumber + " method=post>");
	Response.Write("<table align=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td nowrap><b>Freight</b></td>");
	Response.Write("<td><input type=text name=freight value='" + m_freight + "'></td></tr>");
	Response.Write("<tr><td nowrap><b>Supplier Invioce #</b></td>");
	Response.Write("<td><input type=text name=inv_number></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Record></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

bool DoRecordFreight()
{
	double dFreight = MyMoneyParse(Request.Form["freight"]);
	string inv_number = Request.Form["inv_number"];
	string sc = "UPDATE purchase_bill SET freight=" + dFreight + ", inv_number='" + inv_number + "' ";
	sc += " WHERE po_number=" + m_orderNumber;
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

bool GetOrder()
{
	int rows = 0;
	string sc = "SELECT i.*, o.number, o.status, o.staff_id, o.date_created, o.supplier_id, o.freight ";
	sc += " FROM purchase_order_item i JOIN purchase_order o ON i.id=o.id ";
	sc += " WHERE o.id=" + m_id;
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "product");
//DEBUG("rows=", rows);
		if(rows <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	m_status = GetEnumValue("order_item_status", dst.Tables["product"].Rows[0]["status"].ToString());
	m_orderNumber = dst.Tables["product"].Rows[0]["number"].ToString();
	m_freight = dst.Tables["product"].Rows[0]["freight"].ToString();

	return true;
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>NAME</td>");
	Response.Write("<td>CODE</td>");
//	Response.Write("<td>STOCK</td>");
	Response.Write("<td>QTY</td>");
//	Response.Write("<td>SUPPLIER_CODE</td>");
//	Response.Write("<td>SUPPLIER</td>");
	Response.Write("<td align=right>SELECT</td>");
	Response.Write("</tr>\r\n");
}

Boolean DrawRow(DataRow dr, ref int i, Boolean alterColor)
{
	string code = dr["code"].ToString();
	string name = dr["name"].ToString();
//	string stock = dr["stock"].ToString();
	string quantity = dr["qty"].ToString();

	int qty = MyIntParse(quantity);
	
	if(qty < 0)
		m_bCreditReturn = true;

	int qty1 = qty;
	int qty2 = 0;
//	int nstock = MyIntParse(stock);
	if(m_splits > 0 && m_splits < 63 && m_sr[m_sp] == i)
	{
		qty1 = qty/2;
		qty2 = qty - qty1;
	}

	Response.Write("<input type=hidden name=code" + i.ToString() + " value='" + code + "'>");

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");
	Response.Write("<td>");
	Response.Write(name);
	Response.Write("</td><td>");
	Response.Write(code);
//	Response.Write("</td><td>");
//	Response.Write(stock);

	//qty
	Response.Write("</td><td>");
	if(m_splits > 0 && m_splits < 63 && m_sr[m_sp] == i)
	{
		Response.Write("<input type=hidden name=splited" + i.ToString() + " value=1>");
		Response.Write("<input type=hidden name=total_qty" + i.ToString() + " value='" + qty + "'>");
		Response.Write("<input type=text size=1 autocomplete=off name=qty" + i.ToString());
		Response.Write(" value=" + qty1 + " onchange=\"OnChangeQty1(" + i.ToString() + ")\">");
	}
	else
		Response.Write(qty1);
	Response.Write("</td><td align=right>");

	Response.Write("<input type=checkbox name=sel" + i.ToString());
	if(m_totalRows == 1)
		Response.Write(" checked");
	Response.Write(">");
	Response.Write("</td></tr>");

	if(m_splits > 0 && m_splits < 63 && m_sr[m_sp] == i)
	{
		i++;
		Response.Write("<input type=hidden name=splited" + i.ToString() + " value=1>");
		Response.Write("<input type=hidden name=total_qty" + i.ToString() + " value='" + qty + "'>");
		Response.Write("<input type=hidden name=code" + i.ToString() + " value='" + code + "'>");
		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		Response.Write("<td>");
		Response.Write(name);
		Response.Write("</td><td>");
		Response.Write(code);

		//qty
		Response.Write("</td><td>");
		Response.Write("<input type=text size=1 autocomplete=off name=qty" + i.ToString());
		Response.Write(" value=" + qty2 + " onchange=\"OnChangeQty2(" + i.ToString() + ")\">");
		Response.Write("</a>");
		Response.Write("</td><td align=right>");
		Response.Write("<input type=checkbox name=sel" + i.ToString() + ">");
		Response.Write("</td></tr>");
		
		m_sp++;
	}

	return true;
}

Boolean MyDrawTable()
{
	Boolean bRet = true;

	Response.Write("<br><center><h3>Purchase Order #");
	Response.Write(m_orderNumber + " - <font color=red>");
	Response.Write(m_status);
	Response.Write("</font></h3>");
	Response.Write("<form name=form action=eporder.aspx?id=" + m_id + "&i=" + m_orderNumber + " method=post>");
	Response.Write("<table width=100% bgcolor=white align=center>");
	Response.Write("<tr><td>");

	DrawTableHeader();
	string s = "";
	Boolean alterColor = true;
	int rows = dst.Tables["product"].Rows.Count;
	m_totalRows = rows;
	int index = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["product"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, ref index, alterColor))
		{
			bRet = false;
			break;
		}
		index++;
//		m_rows++;
	}

	Response.Write("<tr><td colspan=" + cols + " align=right><b>Select All </b>");
	Response.Write("<input type=checkbox name=allbox value='Select All' onClick='CheckAll();'>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=" + cols + " align=right>");
	Response.Write("<b>Action : </b>");
	if(m_bCreditReturn)
		Response.Write("<input type=submit name=cmd value='Credit'>");
	else
		Response.Write("<input type=submit name=cmd value='Receive'>");

	Response.Write("<input type=submit name=cmd value='BackOrder'>");
	Response.Write("<input type=submit name=cmd value='OnHold'>");
	if(m_splits <= 0)
		Response.Write("<input type=submit name=cmd value='Split'>");
	else
		Response.Write("<input type=submit name=cmd value='UnSplit'>");
	Response.Write("</td></tr></table>");
	
	Response.Write("<input type=hidden name=rows value=" + index + ">");
	Response.Write("</form>");
	return bRet;
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.form.elements.length;i++) 
		{
			var e = document.form.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form.allbox.checked;
		}
	}
	";
	Response.Write(s);

	if(m_splits > 0)
	{
		Response.Write("function OnChangeQty1(r)\r\n");
		Response.Write("{\r\n");
		Response.Write("	var total = Number(eval(\"document.form.total_qty\" + r + \".value\"));\r\n");
		Response.Write("	var qty1 = Number(eval(\"document.form.qty\" + r + \".value\"));\r\n");
		Response.Write("	eval(\"document.form.qty\" + (r+1) + \".value=total-qty1\");\r\n");
		Response.Write("}\r\n");

		Response.Write("function OnChangeQty2(r)\r\n");
		Response.Write("{\r\n");
		Response.Write("	var total = Number(eval(\"document.form.total_qty\" + r + \".value\"));\r\n");
		Response.Write("	var qty2 = Number(eval(\"document.form.qty\" + r + \".value\"));\r\n");
		Response.Write("	eval(\"document.form.qty\" + (r-1) + \".value=total-qty2\");\r\n");
		Response.Write("}\r\n");
	}

	Response.Write("</script");
	Response.Write(">");
}

bool DoSplit()
{
	if(!GetOrder())
	{
		Response.Write("<h3>Purchase Order# " + m_orderNumber + " not found</h3>");
		return false;
	}

	int index = 0;
	for(int i=0; i<m_rows; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
		{
			if(m_splits > 62)
			{
				Response.Write("<br><br><center><h3>Error, too many rows to split, max is 64</h3>");
				return false;
			}
			m_sr[m_splits] = index;
			m_splits++;
			index++;
		}
		index++;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	PrintJavaFunctions();
	MyDrawTable();

	return true;
}

bool DoUpdateOrder(string status)
{
	m_new_id = m_id;
	string sc = "";
	int selected = 0;
	for(int i=0; i<m_rows; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
			selected++;
	}
	if(selected <= 0)
	{
		Response.Write("<br><br><center><h3>Error, no row selected</h3>");
		return false;
	}
	else if(selected == m_rows) //select all
	{
		sc = "UPDATE purchase_order SET status=" + status + " WHERE id=" + m_id;
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

	if(!GetOrder())
	{
		Response.Write("<h3>Order id " + m_id + " not found</h3>");
		return false;
	}

	DataRow dr = dst.Tables["product"].Rows[0];

	string part = "0";
	sc = "SELECT part FROM purchase_order WHERE number=" + m_orderNumber + " ORDER BY part DESC";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand1.Fill(dst, "part");
		part = rows.ToString(); //new order part number eg:10015.1
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//split order
	sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO purchase_order (number, part, card_id, po_number, contact, special_shipto, shipto, status, record_date, sales)";
	sc += " VALUES(" + m_orderNumber + ", " + part + ", " + dr["card_id"].ToString() + ", '";
	sc += status + ", '" + dr["date_created"].ToString() + "', '" + dr["staff_id"].ToString();
	sc += "') SELECT IDENT_CURRENT('purchase_order') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "id") == 1)
		{
			m_new_id = dst.Tables["id"].Rows[0]["id"].ToString();
		}
		else
		{
			Response.Write("<br><br><center><h3>Error get new id</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<m_rows; i++)
	{
		if(Request.Form["sel" + i.ToString()] == "on")
		{
			string code = Request.Form["code" + i.ToString()];
			if(Request.Form["splited" + i.ToString()] == "1")
			{
				string qty = Request.Form["qty" + i.ToString()];
				string total_qty = Request.Form["total_qty" + i.ToString()];
				int qty_remain = MyIntParse(total_qty) - MyIntParse(qty);
				if(!DoSplitItem(m_id, m_new_id, code, qty, qty_remain.ToString()))
					return false;
			}
			else
			{
				if(!DoUpdateItem(m_id, m_new_id, code))
					return false;
			}
		}
	}
	return true;
}

bool UpdateStockQty(int qty, string id)
{
	string sc = "Update product SET stock=stock - " + qty + " WHERE code=" + id;
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

bool DoSplitItem(string id, string new_id, string code, string qty, string qty_remain)
{
	int rows = 0;
	string sc = "SELECT * FROM purchase_order_item WHERE id=" + id + " AND code=" + code;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "oneitem");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dst.Tables["oneitem"].Rows[0];

	//insert new row for splited backorders
	sc = "INSERT INTO purchase_order_item (id, code, qty, name, price, supplier_code)";
	sc += "VALUES(" + new_id + ", " + code + ", " + qty + ", '" + EncodeQuote(dr["item_name"].ToString()) + "', '";
	sc += dr["supplier_code"].ToString() + "') ";
	//update remain record's quantity
	sc += " UPDATE purchase_order_item SET qty=" + qty_remain + " WHERE id=" + id + " AND code=" + code;
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
	dst.Tables["oneitem"].Clear();
	return true;
}

bool DoUpdateItem(string id, string new_id, string code)
{
	string sc = "UPDATE purchase_order_item SET id=" + new_id + " WHERE id=" + id + " AND code=" + code;
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

bool CreateInvoice(string id)
{
	DataRow dr = null;
	double dPrice = 0;
	double dFreight = 0;
	double dTax = 0;
	double dTotal = 0;
	int rows = 0;
	string sc = "SELECT * FROM purchase_order WHERE id=" + id;
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "invoice");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error creating invoice, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	string staff_id = dst.Tables["invoice"].Rows[0]["staff_id"].ToString();
	double dGstRate = MyDoubleParse(dst.Tables["invoice"].Rows[0]["gst_rate"].ToString());

//	dFreight = MyDoubleParse(Request.Form["freight"]);

	sc = "SELECT * FROM purchase_order_item WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "item");
		if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		double dp = MyDoubleParse(dr["price"].ToString());
		int qty = MyIntParse(dr["qty"].ToString());
		dPrice += dp * qty;

		if(!UpdateStockQty(qty, dr["code"].ToString()))
			return false;
	}
	dTax = dPrice * dGstRate;
	double dAmount = dPrice + dTax + dFreight;
	
	dr = dst.Tables["invoice"].Rows[0];
	string supplier = dr["supplier_id"].ToString();

//	string special_shipto = "0";
//	if(bool.Parse(dr["special_shipto"].ToString()))
//		special_shipto = "1";
	
	string receipt_type = GetEnumID("receipt_type", "invoice");

	sc = " INSERT INTO purchase_bill (po_number, branch_id, staff_id, type, supplier_id, ";
	sc += " buyer_id, total, tax, total_amount, note, date_create, sales_inv, currency, exchange_rate, gst_rate ";
	sc += ") VALUES(" + m_orderNumber + ", " + dr["branch_id"].ToString() + ", " + dr["staff_id"].ToString();
	sc += ", " + receipt_type + ", " + dr["supplier_id"].ToString() + ", " + dr["buyer_id"].ToString();
	sc += ", " + dPrice + ", " + dTax + ", " + dAmount + ", '" + EncodeQuote(dr["note"].ToString());
	sc += "', '" + dr["date_created"].ToString();
	sc += "', '" + dr["sales_inv"].ToString() + "', " + dr["currency"].ToString() + ", " + dr["exchange_rate"].ToString();
	sc += ", " + dGstRate + ")";
	sc += "\r\n";

	//update order to record invoice number
	sc += " UPDATE purchase_order SET bill_number=" + m_orderNumber + " WHERE id=" + id;
	sc += "\r\n";

	//write item table
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		string price = dr["price"].ToString();
		string qty = dr["qty"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string iprice = dr["price"].ToString();

		sc += " INSERT INTO purchase_bill_item (po_number, product_code, name, supplier, supplier_code, qty, price) ";
		sc += " VALUES(" + m_orderNumber + ", '" + code + "', '" + name + "', '" + supplier + "', '";
		sc += supplier_code + "', " + qty + ", " + iprice + ")";
		sc += "\r\n";
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
</script>
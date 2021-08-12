<%@Import Namespace="Microsoft.Data.Odbc" %>

<!-- #include file="price.cs" -->
<!-- #include file="s_item.cs" -->

<script runat=server>

DataSet ds = new DataSet();

bool m_bFormPrinted = false;

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
	
//	m_bShowProgress = true;
	PrintAdminHeader();
	PrintAdminMenu();

//	DEBUG("m=", MyIntParse(DateTime.Now.ToString("MM")));
//	UpdateCardAverage("9", 100, 2);

//	return;


	string cmd = Request.Form["cmd"];
	if(cmd == "Copy Barcode")
	{
		if(CopyBarcode())
			Response.Write("done!<br>");
//		Response.Write("<meta http-equiv=\"refresh\" content=\"10; URL=?\">");
		return;
	}
	else if(cmd == "Change PH ID to Code")
	{
//		if(ChangeIDToCode())
//			Response.Write("done!<br>");
		return;
	}
	else if(cmd == "Insert Customer ID")
	{
//		if(DoInsertCustomerID())
//			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Dump Old Skip")
	{
//		if(DumpOldSkipItems())
//			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Abandon Session")
	{
		Session.Abandon();
	}
	else if(cmd == "Copy Card")
	{
		if(DoCopyCard())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Get Supplier Price")
	{
		if(DoGetSupplierPrice())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Copy Sales")
	{
		if(DoCopySales())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Create Table Test")
	{
		if(CreateTableTest())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Change Payment Type")
	{
		if(ChangePaymentType())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Change Sales Status Type")
	{
		if(ChangeSalesStatusTypes())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Get TransTotal")
	{
		if(GetTransTotal())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "CorrectSupplierPrice")
	{
		if(CorrectSupplierPrice())
			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "MoveStock")
	{
		Response.Write("<embed src=/wav/09.mp3 volume=100 hidden=true autostart=true>");
//		if(DoMoveStock())
//			Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Insert Stock_q")
	{
		Response.Write("<embed src=/wav/09.mp3 volume=100 hidden=true autostart=true>");
		//if(InsertStock_q())
		//	Response.Write("<br><h3>done!</h3>");
		return;
	}
	else if(cmd == "Execute SQL")
	{
		if(ExecuteSQL())
		{
			string sc = Request.Form["sql"];
			if(sc.ToLower().IndexOf("select") != 0)
			{
				Response.Write("<br><h3>done!</h3>");
				Response.Write("<form action=? method=post><textarea name=sql rows=10 cols=90>");
				Response.Write(Session["tools_sql"]);
				Response.Write("</textarea><br><input type=submit name=cmd value='Execute SQL'>");
				Response.Write("<input type=submit name=cmd value='Query ODBC'></form>");
				m_bFormPrinted = true;
			}
		}
	}
	else if(cmd == "Query ODBC")
	{
		if(ODBCQuery())
		{
			string sc = Request.Form["sql"];
			if(sc.ToLower().IndexOf("select") != 0)
			{
				Response.Write("<br><h3>done!</h3>");
				Response.Write("<form action=? method=post><textarea name=sql rows=10 cols=90>");
				Response.Write(Session["tools_sql"]);
				Response.Write("</textarea><br><input type=submit name=cmd value='Execute SQL'>");
				Response.Write("<input type=submit name=cmd value='Query ODBC'></form>");
				m_bFormPrinted = true;
			}
		}
	}
	else if(cmd == "UpdateCardBalances")
	{
		if(UpdateCardBalances())
			Response.Write("<br><h3>done!</h3>");
	}
	else if(cmd == "Test Service")
	{
		if(DoServiceTest())
			Response.Write("<br><h3>done!</h3>");
	}
	else if(cmd == "Import Item")
	{
		if(DoImportItem())
			Response.Write("<br><h3>done!</h3>");
	}
	else if(cmd == "Process Pic")
	{
		if(DoProcessPic())
			Response.Write("<br><h3>done!</h3>");
	}
	else if(cmd == "Add Shelf")
	{
		if(AddShelf())
			Response.Write("<br><h3>done!</h3>");
	}

	if(!IsPostBack && !m_bFormPrinted)
	{
		Response.Write("<form action=? method=post><textarea name=sql rows=10 cols=90>");
		Response.Write(Session["tools_sql"]);
		Response.Write("</textarea><br><input type=submit name=cmd value='Execute SQL'>");
//		Response.Write("<input type=submit name=cmd value='Import Item'>");
		Response.Write("<input type=submit name=cmd value='Add Shelf'>");
		Response.Write("</form>");
	}

//	string ad = "Order Number : EDEN1001\r\n";
//	ad += "Darcy Wang\r\n";
//	ad += "Eden Computers Ltd.\r\n";

//	Encoding enc = Encoding.GetEncoding("iso-8859-1");
//	byte[] data = enc.GetBytes(ad);
//	FileStream newFile = new FileStream("c:\\html\\eznz\\nz\\wholesale\\upload\\address.txt", FileMode.Create);
//	newFile.Write(data, 0, data.Length);
//	newFile.Close();

//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Process Pic'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Change PH ID to Code'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Abandon Session'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Dump Old Skip'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Insert Customer ID'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Copy Card'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Get Supplier Price'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Copy Sales'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Create Table Test'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Change Payment Type'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Get TransTotal'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='Change Sales Status Type'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='CorrectSupplierPrice'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='MoveStock'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='UpdateCardBalances'></form>");
//	Response.Write("<form action=? method=post><textarea name=sql rows=10 cols=90>");
//	Response.Write(Session["tools_sql"]);
//	Response.Write("</textarea><br><input type=submit name=cmd value='Execute SQL'></form>");
//	Response.Write("<form action=? method=post><input type=submit name=cmd value='UpdateCost'></form>");

//	Response.Write(GenRandomString() + "<br>");	
//	Response.Write(GetRootPath());
//	BindGrid();

//	Response.Write("<br><br>");
//	PrintAdminFooter();

//	for(int i=0; i<1025; i++)
//		Response.Write("& # " + i + " : &#" + i + "<br>");
}
/*
bool DoUpdateCost()
{
	string sc = " SELECT * FROM coolway ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "cost");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<ds.Tables["cost"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["cost"].Rows[i];
		string code = dr["code"].ToString();
		string cost = dr["cost"].ToString();

		sc = "UPDATE code_relations ";
		sc += " SET supplier_price = " + cost;
		sc += ", foreign_supplier_price=" + cost;
		sc += ", exchange_rate=1";
		sc += ", manual_cost_frd = " + cost;
		sc += ", manual_exchange_rate = 1";
		sc += ", manual_cost_nzd = " + cost;
		sc += ", allocated_stock = 0 ";
		sc += ", nzd_freight = 0 ";
		sc += ", currency=1 ";
		sc += ", average_cost=" + cost;
		sc += " WHERE code=" + code;
		sc += " UPDATE product SET supplier_price = " + cost + " WHERE code=" + code;
//DEBUG("sc=", sc);
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
	sc = " UPDATE product SET stock = 0, price = 0, allocated_stock = 0 WHERE 1=1 ";
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
*/
bool DoServiceTest()
{
//	csTest ctest = new csTest("http://192.168.1.20/wholesale/service/item.asmx");
	csItem csItem = new csItem("http://www.datawellonline.co.nz/service/item.asmx");
//	int n = ctest.Add(1, 1);
//	DEBUG("n=", n);

	DataSet ds1 = csItem.GetItemDetail("10646");
	if(ds1 == null)
		return false;
	DataView dv = new DataView(ds1.Tables[0]);
	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();

	string fileType = csItem.GetItemPhotoType("10646");
	if(fileType == null || fileType == "")
		return false;
	byte[] buffer = csItem.GetItemPhotoData("10646." + fileType);
	if(buffer == null)
		return false;

	string strPath = Server.MapPath("./test." + fileType);
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(buffer, 0, buffer.Length);
	// Close file
	newFile.Close();

	return true;
}

bool UpdateCardBalances()
{
	return true; //finished;

	string sc = "SELECT card_id, total-amount_paid AS balance ";
	sc += " FROM invoice ";
	sc += " WHERE card_id is not null AND card_id<>0 ";
	sc += " ORDER BY card_id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "balance");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<ds.Tables["balance"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["balance"].Rows[i];
		string card_id = dr["card_id"].ToString();
		string balance = dr["balance"].ToString();
//DEBUG("id="+card_id, ", balance="+balance);
//continue;				
		sc = "UPDATE card SET balance=balance+" + balance + " WHERE id=" + card_id;
//DEBUG("sc=", sc);
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

void On_Odbc_FillError(object sender, FillErrorEventArgs args)
{
	//add the row and continue if there's sth returned, regardless erros
	if(args.Values != null && args.DataTable != null)
	{
		args.DataTable.Rows.Add(args.Values);
		args.Continue = true;
	}
/*
//	Response.Write(args.Values[0].ToString());
//	this.Session["fillerro"] = args.Errors.ToString();
  if (args.Errors.GetType() == typeof(System.OverflowException))
  {
    // Code to handle precision loss.
    //Add a row to table using the values from the first two columns.
    DataRow myRow = args.DataTable.Rows.Add(new object[] {args.Values[0], args.Values[1], DBNull.Value});
    //Set the RowError containing the value for the third column.
//    args.RowError = "OverflowException Encountered. Value from data source: " + args.Values[2];

    args.Continue = true;
  }
*/
}

bool ODBCQuery()
{
	string sc = Request.Form["sql"];
	if(sc == null || sc == "")
	{
		if(Session["tools_sql"] == null || Session["tools_sql"].ToString() == "")
			return false;
		else
			sc = Session["tools_sql"].ToString();
	}

	if(sc.ToLower().IndexOf("update") >= 0)
	{
		if(sc.ToLower().IndexOf("where") < 0)
		{
			Response.Write("<br><center><h3>Error, UPDATE command without WHERE indentity is dangerous</h3>");
			return false;
		}
	}

	Session["tools_sql"] = sc;
	if(sc.ToLower().IndexOf("select") == 0)
	{

//		string strConn = "DRIVER={MYOB};DATABASE=........;USER=........;PWD =.....;";
//		string strConn = "DRIVER={MYOB ODBC};DATABASE=MYOB;dsn=MYOB;SERVER=localhost;USER=;PWD=;";
//		string strConn = "DRIVER={MYOB ODBC};DATABASE=MYOB;dsn=MYOB;";

		OdbcConnection conn = new OdbcConnection("DSN=MYOBSYS");
//		OdbcConnection conn = new OdbcConnection("DSN=MYOBEDEN");
//		OdbcConnection myOdbcConn = new OdbcConnection(strConn);
		int rows = 0;
		try
		{
			conn.Open();
			OdbcDataAdapter da = new OdbcDataAdapter(sc, conn);
			da.FillError += new FillErrorEventHandler(On_Odbc_FillError);
			rows = da.Fill(ds, "test");
		}
		catch(Exception e) 
		{
//DEBUG("rows=", rows);
//if(ds.Tables["test"] != null)
//DEBUG("r=", ds.Tables["test"].Rows.Count);

			string se = e.ToString();
			if(se.IndexOf("NO_DATA") < 0 && se.IndexOf("Driver does not support this function") < 0) //NO_DATA seems means no error,  (DW)
			{
				conn.Close();
				Response.Write(e);
//				ShowExp(sc, e);
				return false;
			}

//			Response.Write(e);
//			return false;
		}

		BindGrid();
		conn.Close();

/*
String strConnect = "DSN=MYOBSYS";

OdbcConnection objConnect = new OdbcConnection(strConnect);
objConnect.Open();
OdbcCommand objCommand = new OdbcCommand(sc, objConnect);
OdbcDataReader objDataReader = null;
try
{
	objDataReader = objCommand.ExecuteReader();
MyDataGrid.DataSource = objDataReader;
MyDataGrid.DataBind();

}
catch(Exception e)
{
	string se = e.ToString();
	if(se.IndexOf("NO_DATA") < 0) //NO_DATA seems means no error (DW)
	{
		ShowExp(sc, e);
		return false;
	}
}

//objDataReader.Close();
objConnect.Close();
*/
		Response.Write("<a href=tools.aspx class=o>New Query</a>");
		return true;
	}
	return true;
}

bool ExecuteSQL()
{
	return ExecuteSQL(true);
}

bool ExecuteSQL(bool bBind)
{
	string sc = Request.Form["sql"];
	if(sc == null || sc == "")
	{
		if(Session["tools_sql"] == null || Session["tools_sql"].ToString() == "")
			return false;
		else
			sc = Session["tools_sql"].ToString();
	}

	if(sc.ToLower().IndexOf("update") >= 0)
	{
		if(sc.ToLower().IndexOf("where") < 0)
		{
			Response.Write("<br><center><h3>Error, UPDATE command without WHERE indentity is dangerous</h3>");
			return false;
		}
	}

	Session["tools_sql"] = sc;
	if(sc.ToLower().IndexOf("select") == 0)
	{
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(ds);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		if(bBind)
			BindGrid();
		Response.Write("<a href=tools.aspx class=o>New Query</a>");
		return true;
	}

	try
	{
//		SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		Response.Write("<h3>Error</h3><font color=red>" + e + "</font><br><br>");
		return false;
	}
	return true;
}

bool InsertStock_q()
{
	int rows = 0;
	string sc = " SELECT COUNT(product_code) AS Quantity, product_code ";
	sc += " FROM stock ";
	sc += " WHERE product_code <> 'HD10001' AND product_code > '0' ";
	sc += " GROUP BY product_code ";
	sc += " HAVING COUNT(product_code) > 0 ";

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "insert");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<ds.Tables["insert"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["insert"].Rows[i];
		string quantity = dr["Quantity"].ToString();
		string p_code = dr["product_code"].ToString();

		sc =  " INSERT INTO stock_q (p_code, p_quantity) ";
		sc += " VALUES ( "+ p_code +", "+ quantity +") ";
		
		try
		{
//			SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
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

bool DoMoveStock()
{
	int rows = 0;
	string sc = "SELECT * FROM stock2";

//	SqlConnection conn = new SqlConnection("Initial Catalog=eden;data source=192.168.1.3;" + m_sSecurityString);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "stock");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//	BindGrid();

	for(int i=0; i<ds.Tables["stock"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["stock"].Rows[i];
		string sn = dr["serial_number"].ToString();
		string purchase_date = dr["purchase_date"].ToString();
		string po_number = dr["invoice_number"].ToString();
		string supplier = dr["supplier"].ToString();

		if(supplier == "don't know")
			supplier = "";
		else if(supplier == "Time" || supplier == "TIMES TECHNOLOGY")
			supplier = "TM";
		else if(supplier == "DYNALINK" || supplier == "Mark")
			supplier = "DY";
		else if(supplier == "techlink" || supplier == "TECH PACIFIC")
			supplier = "TP";
		else if(supplier == "Datawell (nz) LTD" || supplier == "DATAWELL")
			supplier = "DW";
		else if(supplier == "IWAY")
			supplier = "IW";
		else if(supplier == "RENAISSANCE")
			supplier = "RN";
		else if(supplier == "DOVE")
			supplier = "DV";
		else if(supplier == "Morning Star Trading Ltd" || supplier == "mORNING STAR")
			supplier = "MS";
		else if(supplier == "VST (NZ) LTD" || supplier == "VST")
			supplier = "VT";
		else if(supplier == "ingram micro")
			supplier = "IM";
		else if(supplier == "pb technology")
			supplier = "PB";
		else if(supplier == "computer dynamics")
			supplier = "CD";


//		string status = dr["status"].ToString();
		string prod_desc = dr["prod_desc"].ToString();
		string warranty = dr["warranty"].ToString();
		string update_time = dr["update_time"].ToString();

		sc = "INSERT INTO stock (sn, purchase_date, po_number, supplier, prod_desc, warranty, update_time) ";
		sc += " VALUES('" + sn + "', '" + purchase_date + "', '" + po_number + "', '" + supplier + "', '" + prod_desc;
		sc += "', '" + warranty + "', '" + update_time + "')";
		try
		{
//			SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
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

bool CorrectSupplierPrice()
{
	int rows = 0;
	string sc = "SELECT c.id, p.supplier_price FROM product p INNER JOIN ";
	sc += " code_relations c ON p.code = c.code AND p.supplier_price <> c.supplier_price";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "cr");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["cr"].Rows[i];
		string id = dr["id"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		sc = "UPDATE code_relations SET supplier_price=" + supplier_price + " WHERE id='" + id + "'";
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

bool ChangeSalesStatusTypes()
{
	int rows = 0;
	string sc = "SELECT s.id, s.status, s.p_status, s.processed_by, s.shipby, p.id AS shipby_id FROM sales s LEFT OUTER JOIN ship p ON p.name=s.shipby";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "sales");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["sales"].Rows[i];
		string id = dr["id"].ToString();
		string status = dr["status"].ToString();
		string p_status = dr["p_status"].ToString();
		string processed_by = dr["processed_by"].ToString();
		string shipby = dr["shipby"].ToString();
		string shipby_id = dr["shipby_id"].ToString();

		string nstatus = status;
		string np_status = p_status;
		string nprocessed_by = processed_by;
		status = status.ToLower();
		if(status == "quote created" || status == "" || status == "order created" || status == "quote created" || status == "waiting for cheque" || status == "waiting for bankdeposit")
			nstatus = GetEnumID("sales_item_status", "order placed");
		else if(status == "deliveried")
			nstatus = GetEnumID("sales_item_status", "shipped");
		else if(status == "back order")
			nstatus = GetEnumID("sales_item_status", "on backorder");
		else if(status == "payment confirmed")
			nstatus = GetEnumID("sales_item_status", "payment confirmed");
//DEBUG("status="+status, " nstatus="+nstatus);

		p_status = p_status.ToLower();
		if(p_status == "open")
			np_status = GetEnumID("general_status", "open");
		else if(p_status == "pendding" || p_status == "")
			np_status = GetEnumID("general_status", "pending");
		else if(p_status == "closed")
			np_status = GetEnumID("general_status", "closed");

		if(processed_by == "Jerry dong")
			nprocessed_by = "3";
		else if(processed_by == "Darcy Wang")
			nprocessed_by = "2";

		sc = "UPDATE sales SET status_new='" + nstatus + "', p_status_new='" + np_status + "', processed_by_new='" + nprocessed_by + "' ";
		if(shipby_id != "")
			sc += ", shipby_new='" + shipby_id + "' ";
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
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}

bool ChangePaymentType()
{
	int rows = 0;
	string sc = "SELECT invoice_number, ISNULL(type, 'quote') AS type, payment_type FROM invoice";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "inv");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["inv"].Rows[i];
		string invoice_number = dr["invoice_number"].ToString();
		string type = dr["type"].ToString();
		string payment_type = dr["payment_type"].ToString();

		string ntype = "1";
		string npayment_type = "1";

		if(type == "order")
			ntype = "2";
		else if(type == "invoice")
			ntype = "3";
		else if(type == "bill")
			ntype = "4";

		if(payment_type == "CreditCard")
			npayment_type = "3";
		else if(payment_type == "BankDeposit")
			npayment_type = "5";
		else if(payment_type == "Cheque")
			npayment_type = "2";
		else if(payment_type == "")
			npayment_type = "1";

		sc = "UPDATE invoice SET type_new='" + ntype + "', payment_type_new='" + npayment_type + "' WHERE invoice_number=" + invoice_number;
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

bool GetTransTotal()
{
	int rows = 0;
	string sc = "SELECT id, email FROM card";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "card");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i=0; i<rows; i++)
	{
		string email = ds.Tables["card"].Rows[i]["email"].ToString();
		string card_id = ds.Tables["card"].Rows[i]["id"].ToString();
		sc = "SELECT ISNULL(SUM(total), 0) AS total FROM invoice WHERE email='" + email + "' AND paid=1";
		if(ds.Tables["total"] != null)
			ds.Tables["total"].Clear();
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(ds, "total");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		string sum = ds.Tables["total"].Rows[0]["total"].ToString();
		sc = "UPDATE card SET trans_total=" + sum + " WHERE id=" + card_id;
//		sc = " UPDATE INVOICE SET card_id=" + card_id + " WHERE email='" + email + "'";
//DEBUG("id="+card_id + ", email="+email, " sum="+sum);
//continue;
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

bool CreateTableTest()
{
	m_catTableString = "_bb";

	string sc = "if not exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[product_new";
	sc += m_catTableString + "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) ";
	sc += " begin CREATE TABLE [dbo].[product_new" + m_catTableString + "] (";
	sc += @"
	[id] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[stock] [int] NOT NULL ,
	[eta] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[dealer_price] [money] NOT NULL ,
	[price] [money] NOT NULL ,
	[details] [varchar] (1024) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
) ON [PRIMARY]
end

";

	sc += "if not exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[product_raw";
	sc += m_catTableString + "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) ";
	sc += " begin CREATE TABLE [dbo].[product_raw" + m_catTableString + "] (";
	sc += @"
	[id] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[stock] [int] NOT NULL ,
	[eta] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[dealer_price] [money] NOT NULL ,
	[details] [varchar] (1024) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
) ON [PRIMARY]
end
			
";
	sc += "if not exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[code_relations_new";
	sc += m_catTableString + "]') and OBJECTPROPERTY(id, N'IsUserTable') = 1) ";
	sc += " begin CREATE TABLE [dbo].[code_relations_new" + m_catTableString + "] (";
	sc += @"
	[id] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[dealer] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[dealer_code] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[dealer_price] [money] NULL ,
	[code] [int] NOT NULL ,
	[name] [varchar] (1024) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[brand] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[cat] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[s_cat] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ss_cat] [varchar] (255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[hot] [bit] NOT NULL ,
	[skip] [bit] NOT NULL ,
	[rate] [float] NOT NULL 
) ON [PRIMARY]

";
	sc += "	ALTER TABLE [dbo].[code_relations_new" + m_catTableString + "] WITH NOCHECK ADD ";
	sc += @"
	CONSTRAINT [PK_code_relations_new_bb] PRIMARY KEY  CLUSTERED 
	(
		[id]
	)  ON [PRIMARY] 
";

	sc += " ALTER TABLE [dbo].[code_relations_new" + m_catTableString + "] WITH NOCHECK ADD ";
	sc += @"
	CONSTRAINT [DF_code_relations_new_bb_dealer_price] DEFAULT (0) FOR [dealer_price],
	CONSTRAINT [DF_code_relations_new_bb_hot] DEFAULT (0) FOR [hot],
	CONSTRAINT [DF_code_relations_new_bb_drop] DEFAULT (0) FOR [skip]

end
			
";

//DEBUG("sc=", sc);
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

bool DoGetSupplierPrice()
{
	int dcount = 0;
	int rows = 0;
	string sc = "SELECT code, supplier, supplier_code FROM code_relations WHERE supplier_price IS NULL OR supplier_price=0 ORDER BY code";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "gsp");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("Getting supplier price ... ");
	Response.Flush();
	for(int i=0; i<rows; i++)
	{
		MonitorProcess(100);

		string sprice = "0";
		if(ds.Tables["pp"] != null)
			ds.Tables["pp"].Clear();
		sc = "SELECT supplier_price FROM product WHERE supplier='" + ds.Tables["gsp"].Rows[i]["supplier"].ToString() + "' AND supplier_code='" + ds.Tables["gsp"].Rows[i]["supplier_code"].ToString() + "'";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "pp") > 0)
			{
				sprice = ds.Tables["pp"].Rows[0]["supplier_price"].ToString();
			}
			else
			{
				sc = "SELECT supplier_price FROM product_skip WHERE id='" + ds.Tables["gsp"].Rows[i]["supplier"].ToString() + ds.Tables["gsp"].Rows[i]["supplier_code"].ToString() + "'";
				try
				{
					SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
					if(myCommand1.Fill(ds, "pp") > 0)
						sprice = ds.Tables["pp"].Rows[0]["supplier_price"].ToString();
				}
				catch(Exception e) 
				{
					ShowExp(sc, e);
					return false;
				}
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		if(sprice != "0")
		{
			sc = "UPDATE code_relations SET supplier_price=" + sprice + " WHERE id='" + ds.Tables["gsp"].Rows[i]["supplier"].ToString() + ds.Tables["gsp"].Rows[i]["supplier_code"].ToString() + "'";
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
		else
		{
			dcount++;
		}
	}
	Response.Write("<font color=red><b>" + dcount + "<b></font> disappeared.<br>");
	return true;
}

bool DoCopySales()
{
	int rows = 0;
	string sc = "SELECT * FROM invoice";

	SqlConnection conn = new SqlConnection("Initial Catalog=eden;data source=192.168.1.3;" + m_sSecurityString);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, conn);
		rows = myCommand.Fill(ds, "invoice");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//	BindGrid();

	for(int i=0; i<ds.Tables["invoice"].Rows.Count; i++)
	{
		sc = "INSERT INTO invoice (";
		DataRow dr = ds.Tables["invoice"].Rows[i];
		try
		{
			SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
			myCommand = new SqlCommand(sc);
			myCommand.Connection = conn1;
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

bool DoCopyCard()
{
	int rows = 0;
	string sc = @"SELECT a.*, ISNULL(g.type, 'normal') AS access, g.discount, g.note
		FROM account a INNER JOIN account_group g ON a.email=g.email AND g.site='eden'
		ORDER BY a.id";

	SqlConnection conn = new SqlConnection("Initial Catalog=eznz;data source=192.168.1.3;" + m_sSecurityString);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, conn);
		rows = myCommand.Fill(ds);
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//	BindGrid();

	//clean it first
	sc = "DELETE card";
	try
	{
		SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
		myCommand = new SqlCommand(sc);
		myCommand.Connection = conn1;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		string access = dr["access"].ToString();
		if(access == "")
			access = "normal";
		string access_level = GetEnumID("access_level", access);
		string mpass = FormsAuthentication.HashPasswordForStoringInConfigFile(dr["password"].ToString(), "md5");
//Response.Write("access_level="+access_level + ", mpass="+mpass+"<br>");

		string accept_mass_email = "0";
		if(dr["ads"] != null)
		{
			if(dr["ads"].ToString() != "")
			{
				if(bool.Parse(dr["ads"].ToString()))
					accept_mass_email = "1";
			}
		}
		sc = @"INSERT INTO card (id, email, password, name, company, address1, address2, city, country, phone, fax, contact,
			nameB, companyB, address1B, address2B, cityB, countryB, postal1, postal2, postal3, register_date, 
			shipping_fee, accept_mass_email, access_level, discount, note) VALUES(" + dr["id"].ToString() + ", '";
		sc += dr["email"].ToString();
		sc += "', '";
		sc += mpass;
		sc += "', '";
		sc += dr["name"].ToString();
		sc += "', '";
		sc += dr["company"].ToString();
		sc += "', '";
		sc += dr["address1"].ToString();
		sc += "', '";
		sc += dr["address2"].ToString();
		sc += "', '";
		sc += dr["city"].ToString();
		sc += "', '";
		sc += dr["country"].ToString();
		sc += "', '";
		sc += dr["phone"].ToString();
		sc += "', '";
		sc += dr["fax"].ToString();
		sc += "', '";
		sc += dr["contact"].ToString();
		sc += "', '";
		sc += dr["nameB"].ToString();
		sc += "', '";
		sc += dr["companyB"].ToString();
		sc += "', '";
		sc += dr["address1B"].ToString();
		sc += "', '";
		sc += dr["address2B"].ToString();
		sc += "', '";
		sc += dr["cityB"].ToString();
		sc += "', '";
		sc += dr["countryB"].ToString();
		sc += "', '";
		sc += dr["postal1"].ToString();
		sc += "', '";
		sc += dr["postal2"].ToString();
		sc += "', '";
		sc += dr["postal3"].ToString();
		sc += "', '";
		sc += dr["register_date"].ToString();
		sc += "', ";
		sc += dr["shipping_fee"].ToString();
		sc += ", ";
		sc += accept_mass_email;
		sc += ", ";
		sc += access_level;
		sc += ", ";
		sc += dr["discount"].ToString();
		sc += ", '";
		sc += EncodeQuote(dr["note"].ToString());
		sc += "')";

		try
		{
			SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
			myCommand = new SqlCommand(sc);
			myCommand.Connection = conn1;
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
/*
	//temperay add some supplier
	sc = "INSERT INTO card (id, email, password, name, short_name, type) VALUES(1000, 'sales@techlink.co.nz', '', 'tech link', 'TP', 3)\r\n";
	sc += "INSERT INTO card (id, email, password, name, short_name, type) VALUES(1001, 'sales@renaissance.co.nz', '', 'Renaissance', 'RN', 3)\r\n";
	sc += "INSERT INTO card (id, email, password, name, short_name, type) VALUES(1002, 'sales@bbfnz.co.nz', '', 'BBF Components', 'BB', 3)\r\n";
	try
	{
		SqlConnection conn1 = new SqlConnection("Initial Catalog=eden;data source=192.168.1.4;" + m_sSecurityString);
		myCommand = new SqlCommand(sc);
		myCommand.Connection = conn1;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
*/
	return true;
}

bool DoInsertCustomerID()
{
	int rows = 0;
	string sc = "SELECT invoice_number, email FROM invoice";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
		return false;
	for(int i=0; i<rows; i++)
	{
		if(ds.Tables["account"] != null)
			ds.Tables["account"].Clear();
		string email = ds.Tables["invoice"].Rows[i]["email"].ToString();
		string invoice_number = ds.Tables["invoice"].Rows[i]["invoice_number"].ToString();
		sc = "SELECT id FROM account WHERE email='" + email + "'";
		try
		{
			SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "account") != 1)
			{
				Response.Write("<b>Error, account not found, email:" + email + "</b><br>");
				continue;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		string id = ds.Tables["account"].Rows[0]["id"].ToString();
		sc = "UPDATE invoice SET card_id=" + id + " WHERE invoice_number=" + invoice_number;
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
	}
	return true;
}

bool AdjustPrice()
{
	int rows;
	string sc = "SELECT p.code, c.id, p.supplier_price, c.rate, p.price, p.stock ";
	sc += "FROM product p JOIN code_relations c ON p.code=c.code ";
	sc += "WHERE c.rate=1.06 ORDER BY p.supplier_price";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	BindGrid();
	return true;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		double dsupplierPrice = double.Parse(dr["supplier_price"].ToString());
		double dRate = 1.1;
		if(dsupplierPrice > 200)
			dRate = 1.08;
		if(dsupplierPrice > 1000)
			dRate = 1.06;
		double dPrice = CalculateRetailPrice(dsupplierPrice, dRate);
//if(dRate == 1.1)
//	DEBUG("supplierPrice=" + dr["supplier_price"].ToString(), " dPrice=" + dPrice.ToString("c"));
		
//		if(!UpdatePrice(dr["code"].ToString(), dr["id"].ToString(), dsupplierPrice, dPrice, dr["stock"].ToString()))
//			break;
//		if(!UpdatePriceRate(dr["code"].ToString(), dRate))
//			break;
		Response.Write(".");
		Response.Flush();
	}
	return true;
}

bool ChangeIDToCode()
{
	int rows;
	string sc = "SELECT h.id, c.code FROM price_history h JOIN code_relations c ON h.id=c.id";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//BindGrid();
//return true;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		if(!UpdatePH(dr["id"].ToString(), dr["code"].ToString()))
			return false;
	}
	return true;
}

bool UpdatePH(string id, string code)
{
	string sc = "UPDATE price_history SET code=";
	sc += code;
	sc += " WHERE id='";
	sc += id;
	sc += "'";

	Response.Write(id + " -> " + code + "...added");
	Response.Flush();
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
	Response.Write("done.<br>");
	Response.Flush();
	return true;
}

bool CopyBarcode()
{
	int rows;
	string sc = "SELECT code, barcode FROM code_relations WHERE barcode IS NOT NULL AND barcode <> '' ORDER BY code ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "code_relations");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("copying, please wait...");
	sc = "";
	int count = 0;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["code_relations"].Rows[i];
		string code = dr["code"].ToString();
		string barcode = EncodeQuote(dr["barcode"].ToString());
		sc += " IF NOT EXISTS(SELECT id FROM barcode WHERE barcode = '" + barcode + "') ";
		sc += " INSERT INTO barcode(item_code, barcode, item_qty) VALUES(" + code + ", '" + barcode + "', 1) ";
		count++;
		if(count > 100)
		{
			Response.Write(".");
			Response.Flush();
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
			sc = "";
			count = 0;
		}
	}
	if(sc != "")
	{
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
	}
	Response.Write("done.<br>");
	Response.Flush();
	return true;
}

bool UpdateCodeRelationsID(string old_id, string id)
{
	string sc = "UPDATE code_relations SET supplier='RN', id='";
	sc += id;
	sc += "' WHERE id='";
	sc += old_id;
	sc += "'";

	Response.Write(old_id + " -> " + id + "...");
	Response.Flush();
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
	Response.Write("done.<br>");
	Response.Flush();
	return true;
}

void BindGrid()
{
	DataView dv = new DataView(ds.Tables[0]);
	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();
}

bool DumpOldSkipItems()
{
	int rows = 0;
	int rowsc = 0;
	string sc = "SELECT id FROM product_skip";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "skip");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<rows; i++)
	{
		string id = ds.Tables["skip"].Rows[i]["id"].ToString();
		sc = "SELECT code FROM code_relations WHERE id='" + id + "'";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "cr") <= 0) //not exists
			{
				Response.Write(id + "<br>");
			}
			else 
				ds.Tables["cr"].Clear();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	ExecuteSQL(false);
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

bool DoImportItem()
{
	string sc = " SELECT * FROM parts ORDER BY code ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "item") <= 0)
		{
			Response.Write("<br>no item");
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i=0; i<ds.Tables["item"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["item"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string barcode = dr["barcode"].ToString();
		string price1 = dr["price1"].ToString();

		sc = " INSERT INTO code_relations (code, id, name, barcode, price1) ";
		sc += " VALUES(" + code + ", " + code;
		sc += ", '" + EncodeQuote(name) + "' ";
		sc += ", '" + EncodeQuote(barcode) + "' ";
		sc += ", " + price1 + ") ";
		sc += " INSERT INTO product (code, name, price) VALUES( ";
		sc += code + ", '" + EncodeQuote(name) + "', " + price1 + ") ";
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
	}
	return true;
}

bool DoProcessPic()
{
	int nRows = 0;
	string sc = " SELECT code, ref_code FROM code_relations WHERE ref_code IS NOT NULL AND ref_code <> '' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "item");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	string spath = "../pi/";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["item"].Rows[i];
		string code = dr["code"].ToString();
		string ref_code = dr["ref_code"].ToString();

		if(ref_code.IndexOf("(") >= 0)
			continue;
		if(ref_code.IndexOf("?") >= 0)
			continue;
			
		string fn = Server.MapPath(spath + ref_code);
		if(!File.Exists(fn))
			continue;
		string fn_new = Server.MapPath(spath + code + ".jpg");
		File.Move(fn, fn_new);
//DEBUG(i.ToString() + ":", code);
	}
	return true;
}

bool AddShelf()
{
	string sc = "";
	string area = "Eo";
	string[] aSection = {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P"};
	for(int i=0; i<=14; i++)
	{
		string location = i.ToString();
		for(int j=0; j<aSection.Length; j++)
		{
			string section = aSection[j];
			for(int m=1; m<=6; m++)
			{
				string level = m.ToString();
				string name = area + location + section + level;
				sc = " IF NOT EXISTS(SELECT id FROM shelf WHERE area = '" + area + "' AND location = '" + location + "' AND section = '" + section + "' AND level = " + level + ") ";
				sc += " INSERT INTO shelf (area, location, section, level, name) VALUES(";
				sc += " '" + area + "' ";
				sc += ", '" + location + "' ";
				sc += ", '" + section + "' ";
				sc += ", " + level + " ";
				sc += ", '" + name + "') ";
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
			}
		}
	}
	return true;
}
</script>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1 
	BorderStyle=Solid 
	BorderColor=#CCCCCC
	CellPadding=0 
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	HorizontalAlign=center
	AllowPaging=True
	PageSize=50
	PagerStyle-PageButtonCount=20
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
>

	<HeaderStyle BackColor=#FFFFFF ForeColor=#000000 Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
</asp:DataGrid>
</form>
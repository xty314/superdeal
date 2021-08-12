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
    TS_PageLoad(); //do common things, LogVisit etc...
//	if(!SecurityCheck(""))
//		return;

//DEBUG("agent=", Request.ServerVariables["HTTP_USER_AGENT"]);
    
	m_type = g("t");
	m_action = g("a");
	m_barcode = g("c");
	if(Request.Form["barcode"] != null && Request.Form["barcode"] != "")
		m_barcode = p("barcode");
	m_pwd = g("i");
	m_card_id = g("cid");
	if(Request.Form["card_id"] != null && Request.Form["card_id"] != "")
		m_card_id = p("card_id");
	if(m_card_id != "")
		Session["card_id"] = m_card_id;
	else if(Session["card_id"] != null)
		m_card_id = Session["card_id"].ToString();
	m_cmd = p("cmd");

	if(m_action == "da")
	{
		DoDelPic();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode);
		Response.Write("&code=" + m_code + "&name=" + HttpUtility.UrlEncode(m_name));
		return;
	}
	else if(m_action == "dc")
	{
		DoRemoveItemFromCart();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode + "&cid=" + m_card_id + "\">");
		return;
	}
	
	if(m_cmd == Lang("Save"))
	{
		if(DoSaveItem())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode + "&cid=" + m_card_id + "\">");
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Add"))
	{
		if(DoAddItem())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode + "&cid=" + m_card_id + "\">");
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Add to Cart"))
	{
		if(DoAddToCart())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode + "&cid=" + m_card_id + "\">");
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Place Order"))
	{
		if(DoPlaceOrder())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode + "&cid=" + m_card_id + "\">");
		return; //if it's a form post then do nothing else, quit here
	}
	
	string header = ReadSitePage("admin_page_header_pda");
	header = header.Replace("@@companyTitle", m_sCompanyTitle);
	Response.Write(header);
	PrintMainForm();
}
bool PrintMainForm()
{
	string pwd = GetSiteSettings("scan_password", "6232176");
	if(m_pwd != pwd)
	{
		Response.Write("access denied");
		return false;
	}
	
	int nRows = 0;
	string sc = " SELECT cr.name, cr.code, cr.price1 AS price, sq.qty, branch.name AS branch_name, sq.branch_id ";
//	sc += ", (SELECT SUM(qty) FROM stock_qty WHERE code = b.item_code) AS stock ";
	sc += " FROM barcode b ";
	sc += " JOIN code_relations cr ON cr.code = b.item_code ";
	sc += " LEFT OUTER JOIN stock_qty sq ON sq.code = cr.code ";
	sc += " LEFT OUTER JOIN branch ON branch.id = sq.branch_id AND branch.activated = 1 ";
	sc += " WHERE b.barcode = '" + EncodeQuote(m_barcode) + "' ";
//DEBUG("sc=", sc);	
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
	if(nRows <= 0)
	{
		sc += " OR cr.code = " + m_barcode;
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
	}	
	if(nRows <= 0)
	{
		Response.Write("<h3>not found, add new item</h3>");
		PrintNewItemForm();
		return true;
	}

	DataRow dr = ds.Tables["item"].Rows[0];
	string name = dr["name"].ToString();
	string code = dr["code"].ToString();
	m_name = name;
	m_code = code;
	GetOldData(); //display old photo if exists, otherwise insert new row, prepare to upload
	double dPrice = MyDoubleParse(dr["price"].ToString());
	string price = Math.Round(dPrice, 2).ToString();

	Response.Write("<form name=f action=?i=" + m_pwd + "&cid=" + m_card_id + "&c=" + m_barcode + " method=post>");
	Response.Write("<input type=hidden name=card_id value=" + m_card_id + ">");
	Response.Write("<input type=hidden name=code value=" + code + ">");
	Response.Write("<input type=hidden name=rows value=" + nRows + ">");
	Response.Write("<table class=t width=100% align=center>");
	Response.Write("<tr><th>Barcode</th><td><input size=15 class=b name=barcode value='" + m_barcode + "' onclick=select()>");
	Response.Write("<input type=submit name=cmd value='" + Lang("Search") + "' class=b></td></tr>");
	Response.Write("<tr><th>Desc</th><td><input size=15 class=b name=name value='" + name + "' onclick=select()></td></tr>");
	Response.Write("<tr><th>Price</th><td><input size=10 class=b name=price value='" + price + "' onclick=select()></td></tr>");
//	Response.Write("<tr><th colspan=2 align=left>stock</th></tr>");
	
	for(int i=0; i<nRows; i++)
	{
		dr = ds.Tables["item"].Rows[i];
		string branch_name = dr["branch_name"].ToString();
		string branch_id = dr["branch_id"].ToString();
		string stock = dr["qty"].ToString();
		Response.Write("<tr><th><input type=hidden name=branch_id" + i + " value=" + branch_id + ">");
		Response.Write(branch_name + "</th><td><input size=2 class=b name=stock_" + branch_id + " value='" + stock + "' onclick=select()></td></tr>");
	}
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='" + Lang("Save") + "' class=b></td></tr>");
	Response.Write("<tr><td colspan=2><input size=2 name=qty value=1 class=b onclick=select()><input type=submit name=cmd value='" + Lang("Add to Cart") + "' class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	DisplayCart();
	Response.Write("</form>");
	return true;
}
bool DoSaveItem()
{
	string sc = "";
	int nRows = MyIntParse(p("rows"));
	string code = p("code");
	string name = p("name");
	double dPrice = MyMoneyParse(p("price"));
	sc = " UPDATE code_relations SET name = N'" + EncodeQuote(name) + "' ";
	sc += ", price1 = " + dPrice + " ";
    sc += ", level_price0 = " + dPrice + " ";
	sc += " WHERE code = " + code;
	for(int i=0; i<nRows; i++)
	{
		string branch_id = p("branch_id" + i);
		double dQty = MyDoubleParse(p("stock_" + branch_id));
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
	return true;	
}
bool PrintNewItemForm()
{
	int nRows = 0;
	string sc = " SELECT id, name FROM branch WHERE activated = 1 ORDER BY id ";
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

	Response.Write("<form name=f action=?i=" + m_pwd + "&c=" + m_barcode + " method=post>");
	Response.Write("<input type=hidden name=rows value=" + nRows + ">");
	Response.Write("<table class=t width=100% align=center>");
	Response.Write("<tr><th>Barcode</th><td><input size=15 class=b name=barcode value='" + m_barcode + "' onclick=select()>");
	Response.Write("<tr><th>Desc</th><td><input size=15 class=b name=name value='' onclick=select()></td></tr>");
	Response.Write("<tr><th>Price</th><td><input size=10 class=b name=price value='' onclick=select()></td></tr>");
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["branch"].Rows[i];
		string branch_name = dr["name"].ToString();
		string branch_id = dr["id"].ToString();
		Response.Write("<tr><th><input type=hidden name=branch_id" + i + " value=" + branch_id + ">");
		Response.Write(branch_name + "</th><td><input size=10 class=b name=stock_" + branch_id + " value='0' onclick=select()></td></tr>");
	}
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='" + Lang("Add") + "' class=b></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
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

	sc = " INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, price1, level_price0, code, name, brand, ";
	sc += " cat, s_cat, ss_cat, hot, skip ";
	sc += ") VALUES('" + id + "', '" + supplier + "', '" + supplier_code + "', " + dSupplier_price + ", " + dPrice + ", " + dPrice + ", " + m_code;
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
		string branch_id = p("branch_id" + i);
		double dQty = MyDoubleParse(p("stock_" + branch_id));
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
	return true;	
}
bool DoAddToCart()
{
	string code = p("code");
	double dQty = MyDoubleParse(p("qty"));
	string sc = " IF NOT EXISTS(SELECT id FROM cart WHERE card_id = " + m_card_id + " AND code = " + code + ") ";
	sc += " INSERT INTO cart (card_id, code, quantity) VALUES(" + m_card_id + ", " + code + ", " + dQty + ") ";
	sc += " ELSE ";
	sc += " UPDATE cart SET quantity = quantity + " + dQty + " WHERE card_id = " + m_card_id + " AND code = " + code;
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
bool DoRemoveItemFromCart()
{
	string code = g("code");
	string sc = " DELETE FROM cart WHERE card_id = " + m_card_id + " AND code = " + code;
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
bool DisplayCart()
{
	int nRows = 0;
	string sc = " SELECT cr.name, cr.price1 AS price, cart.code, cart.quantity ";
	sc += " FROM cart ";
	sc += " JOIN code_relations cr ON cr.code = cart.code ";
	sc += " WHERE cart.card_id = " + m_card_id;
	sc += " ORDER BY cart.id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "cart");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
		return true;
	double dTotal = 0;
	Response.Write("<table class=t>");
	Response.Write("<tr><th>Description</th><th>Price</th><th>Qty</th><th>Amount</th><th>&nbsp;</th></tr>");
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["cart"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		double dPrice = MyDoubleParse(dr["price"].ToString());
		double dQty = MyDoubleParse(dr["quantity"].ToString());
		dTotal += dPrice * dQty;
		
		Response.Write("<tr");
		if(i%2 != 0)
			Response.Write(" bgcolor=#CCCCCC");
		Response.Write("><td>" + name + "</td><td>" + dPrice.ToString("c") + "</td><td align=center>" + dQty + "</td><td>" + (dPrice * dQty).ToString("c") + "</td>");
		Response.Write("<td align=center><a href=?i=" + m_pwd + "&cid=" + m_card_id + "&a=dc&code=" + code + "&r=" + DateTime.Now.ToOADate() + ">Remove</a></td>");
	}
	Response.Write("<tr bgcolor=#FFFFAA><td colspan=3 align=right>Total:</td><td>" + dTotal.ToString("c") + "</td><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=5>");
	Response.Write("Customer:<select name=customer>" + PrintCustomerDealerOptions() + "</select><input type=submit name=cmd value='" + Lang("Place Order") + "' class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	return true;
}
bool DoPlaceOrder()
{
	int nRows = 0;
	string sc = " SELECT cr.name, cr.supplier, cr.supplier_code, cr.price1 AS price, cart.code, cart.quantity ";
	sc += " FROM cart ";
	sc += " JOIN code_relations cr ON cr.code = cart.code ";
	sc += " WHERE cart.card_id = " + m_card_id;
	sc += " ORDER BY cart.id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "cart");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(nRows <= 0)
		return true;
	
	string cust_id = p("customer");
	if(cust_id == "")
		cust_id = "0";
	sc = " BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, type, card_id, sales) "; 
	sc += " VALUES(0, 2, " + cust_id + ", " + m_card_id + ") SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "orderid") == 1)
		{
			m_orderID = ds.Tables["orderid"].Rows[0]["id"].ToString();
		}
		else
		{
			Response.Write("<br><br><center><h3>Create Order failed, error getting new order number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = "";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["cart"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		double dPrice = MyDoubleParse(dr["price"].ToString());
		double dQty = MyDoubleParse(dr["quantity"].ToString());
		
		sc += " INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price, commit_price) ";
		sc += " VALUES(" + m_orderID + ", " + code + ", " + dQty + ", N'" + EncodeQuote(name) + "', '" + EncodeQuote(supplier) + "' ";
		sc += ", '" + EncodeQuote("supplier_code") + "', " + dPrice + ", " + dPrice + ") ";
	}
	sc += " DELETE FROM cart WHERE card_id = " + m_card_id;
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
string PrintCustomerDealerOptions()
{
	int nRows = 0;
	string sc = " SELECT TOP 20 id, trading_name, company, name FROM card WHERE type IN(1, 2) ORDER BY trading_name, company, name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "card");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	if(nRows <= 0)
		return "";
	string s = "<option value=0></option>";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["card"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["trading_name"].ToString();
		if(name == "")
			name = dr["company"].ToString();
		if(name == "")
			name = dr["name"].ToString();
		if(name.Length > 16)
			name = name.Substring(0, 16);
		s += "<option value=" + id + ">" + name + "</option>";
	}
	return s;
}
void cmdSend_Click(object sender, System.EventArgs e)
{
	if( filMyFile.PostedFile != null )
	{
		HttpPostedFile myFile = filMyFile.PostedFile;
		int nFileLen = myFile.ContentLength; 
		if( nFileLen > 0 )
		{
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);

			string strFileName = m_code;
			string sExt = Path.GetExtension(myFile.FileName);
			strFileName += sExt;
			m_fileName = strFileName;
			string vpath = GetRootPath();
			vpath += "/pi/";
			string strPath = Server.MapPath(vpath);
			string purePath = strPath;
			strPath += strFileName;
			
			string sExtOld = ".gif";
			if(String.Compare(sExt, ".gif", true) == 0)
				sExtOld = ".jpg";
			string oldFile = purePath + m_code + sExtOld;
			if(File.Exists(oldFile))
				File.Delete(oldFile);

			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?i=" + m_pwd + "&c=" + m_barcode);
			Response.Write("&code=" + m_code + "&name=" + HttpUtility.UrlEncode(m_name));
			Response.Write("\">");
		}
	}
}
void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}
bool GetOldData()
{
	string sPicFile = "";
	sPicFile = GetProductImgSrc(m_code);
//	LOldPic.Text = "<b>Old Image:</b> ";
	LOldPic.Text += m_pic;
	LOldPic.Text += "<br><img src=" + sPicFile + ">";
	LOldPic.Text += "<br><a href=?i=" + m_pwd + "&c=" + m_barcode + "&code=" + m_code + "&a=da&file=" + HttpUtility.UrlEncode(sPicFile);
	LOldPic.Text += " class=o>DELETE</a>";
	return true;
}
void DoDelPic()
{
	string file = Server.MapPath(Request.QueryString["file"]);
	File.Delete(file);
}
</script>
<asp:label id=LTitle runat=server/>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<table><tr>
<td><input id="filMyFile" type="file" runat="server" class=b></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload" class=b/></td>
</tr></table>

<br>
<asp:Label id=LOldPic runat=server/>

</FORM>
<asp:Label id=LFooter runat=server/>

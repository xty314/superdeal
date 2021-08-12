<%@Import Namespace="Microsoft.Data.Odbc" %>
<%@Import Namespace="System.Threading" %>
<script runat=server>
///////////////////////////////////////////////////////////////////////////////////////////////////////////
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_sDSN = "";
string m_sTable = "";
string m_smSupplier = "";
string m_smCode = "";
string m_smName = "";
string m_smBrand = "";
string m_smCat = "";
string m_smSCat = "";
string m_smSSCat = "";
string m_smCost = "";
string m_smPrice = "";
string m_smStock = "";
string m_smEta = "";

int m_nDisappeared = 0;
int m_nNew = 0;
int m_nUpdated = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.Form["cmd"] == "Save Settings")
	{
		if(SaveODBCSettings())
		{
			Response.Redirect("odbcsync.aspx");
			return;
		}
	}
	else if(Request.Form["cmd"] == "Update Stock")
	{
		m_bShowProgress = true;
		PrintAdminHeader();
		PrintAdminMenu();

		if(GetODBCSettings())
		{
			DoUpdateStock();
		}
//		PrintAdminFooter();
		return;
	}
	else if(Request.Form["cmd"] == "Test DSN")
	{
		PrintAdminHeader();
		PrintAdminMenu();

		if(SaveODBCSettings())
		{
			DoTestDSN();
			PrintODBCSetupForm();
		}
//		PrintAdminFooter();
		return;
	}
	else if(Request.Form["cmd"] == "Test Mapping")
	{
		PrintAdminHeader();
		PrintAdminMenu();

		if(SaveODBCSettings())
		{
			DoTestMapping();
			PrintODBCSetupForm();
		}
//		PrintAdminFooter();
		return;
	}
	if(Request.QueryString["t"] == "setup")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		
		if(GetODBCSettings())
			PrintODBCSetupForm();
		
//		PrintAdminFooter();
		return;
	}
	else if(Request.QueryString["t"] == "update")
	{
		return;
	}
	
	//default page
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h3>Stock Update</h3>");

	Response.Write("<input type=button value='Update Stock' onclick=window.location=('odbcsync.aspx?t=update') " + Session["button_style"] + ">");
	Response.Write("<input type=button value='Settings' onclick=window.location=('odbcsync.aspx?t=setup') " + Session["button_style"] + ">");

//	PrintAdminFooter();
}

bool SaveODBCSettings()
{
	m_sDSN = Request.Form["dsn_name"];
	m_sTable = Request.Form["table_name"];
	m_smSupplier = Request.Form["supplier"];
	m_smCode = Request.Form["code"];
	m_smName = Request.Form["desc"];
	m_smBrand = Request.Form["brand"];
	m_smCat = Request.Form["cat"];
	m_smSCat = Request.Form["s_cat"];
	m_smSSCat = Request.Form["ss_cat"];
	m_smCost = Request.Form["cost"];
	m_smPrice = Request.Form["price"];
	m_smStock = Request.Form["stock"];
	m_smEta = Request.Form["eta"];

	string sc = " UPDATE odbc_mapping SET ";
	sc += " dsn_name = '" + EncodeQuote(m_sDSN) + "' ";
	sc += ", table_name = '" + EncodeQuote(m_sTable) + "' ";
	sc += ", supplier = '" + EncodeQuote(m_smSupplier) + "' ";
	sc += ", code = '" + EncodeQuote(m_smCode) + "' ";
	sc += ", name = '" + EncodeQuote(m_smName) + "' ";
	sc += ", brand = '" + EncodeQuote(m_smBrand) + "' ";
	sc += ", cat = '" + EncodeQuote(m_smCat) + "' ";
	sc += ", s_cat = '" + EncodeQuote(m_smSCat) + "' ";
	sc += ", ss_cat = '" + EncodeQuote(m_smSSCat) + "' ";
	sc += ", cost = '" + EncodeQuote(m_smCost) + "' ";
	sc += ", price = '" + EncodeQuote(m_smPrice) + "' ";
	sc += ", stock = '" + EncodeQuote(m_smStock) + "' ";
	sc += ", eta = '" + EncodeQuote(m_smEta) + "' ";
	sc += " WHERE map_name = 'myob_items' ";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e1) 
	{
		ShowExp(sc, e1);
		return false;
	}
	return true;
}

bool GetODBCSettings()
{
	string sc = " SELECT * FROM odbc_mapping WHERE map_name = 'myob_items' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "setting") <= 0)
		{
//			Response.Write("<br><br><center><h3>Error, getting odbc_mapping table failed.</h3><br><br><br><br>");
			sc = " INSERT INTO odbc_mapping (map_name, dsn_name, table_name) VALUES('myob_items', 'MYOBSYS', 'items') ";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e1) 
			{
				ShowExp(sc, e1);
				return false;
			}

			//try again
			sc = " SELECT * FROM odbc_mapping WHERE map_name = 'myob_items' ";
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dst, "setting") <= 0)
				{
					Response.Write("<br><br><center><h3>Error, getting odbc_mapping table failed.</h3><br><br><br><br>");
					return false;
				}
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);
				return false;
			}
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = dst.Tables["setting"].Rows[0];
	m_sDSN = dr["dsn_name"].ToString();
	m_sTable = dr["table_name"].ToString();
	m_smSupplier = dr["supplier"].ToString();
	m_smCode = dr["code"].ToString();
	m_smName = dr["name"].ToString();
	m_smBrand = dr["brand"].ToString();
	m_smCat = dr["cat"].ToString();
	m_smSCat = dr["s_cat"].ToString();
	m_smSSCat = dr["ss_cat"].ToString();
	m_smCost = dr["cost"].ToString();
	m_smPrice = dr["price"].ToString();
	m_smStock = dr["stock"].ToString();
	m_smEta = dr["eta"].ToString();
	return true;
}

bool PrintODBCSetupForm()
{
	Response.Write("<form action=odbcsync.aspx method=post>");
	Response.Write("<br><h3>Update Settings</h3>");
	Response.Write("<table>");
	Response.Write("<tr><td colspan=2>");

	Response.Write("<table align=center cellspacing=3 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");
	//DSN
	Response.Write("<tr><td><b>DSN</b></td>");
	Response.Write("<td><input type=text name=dsn_name value='" + m_sDSN + "'></td></tr>");

	//table
	Response.Write("<tr><td><b>Table &nbsp; </td>");
	Response.Write("<td><input type=text name=table_name value='" + m_sTable + "'></td></tr>");

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value='Test DSN' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("</table>");

	//mapping
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2 align=center><font size=+1>Column Mapping</font></td></tr>");
	Response.Write("<tr><td colspan=2>");

	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#EEEEEE>");
	Response.Write("<th>SUPPLIER</th>");
	Response.Write("<th>CODE</th>");
	Response.Write("<th>DESC</th>");
	Response.Write("<th>BRAND</th>");
	Response.Write("<th>CAT</th>");
	Response.Write("<th>S_CAT</th>");
	Response.Write("<th>SS_CAT</th>");
	Response.Write("<th>COST</th>");
	Response.Write("<th>PRICE</th>");
//	Response.Write("<th>RRP</th>");
	Response.Write("<th>STOCK</th>");
	Response.Write("<th>ETA</th>");
//	Response.Write("<th>DETAILS</th>");
	Response.Write("</tr>");

	Response.Write("<tr>");
	Response.Write("<td><input type=text size=10 name=supplier value='" + m_smSupplier + "'></td>");
	Response.Write("<td><input type=text size=10 name=code value='" + m_smCode + "'></td>");
	Response.Write("<td><input type=text size=10 name=desc value='" + m_smName+ "'></td>");
	Response.Write("<td><input type=text size=10 name=brand value='" + m_smBrand + "'></td>");
	Response.Write("<td><input type=text size=10 name=cat value='" + m_smCat + "'></td>");
	Response.Write("<td><input type=text size=10 name=s_cat value='" + m_smSCat + "'></td>");
	Response.Write("<td><input type=text size=10 name=ss_cat value='" + m_smSSCat + "'></td>");
	Response.Write("<td><input type=text size=10 name=cost value='" + m_smCost + "'></td>");
	Response.Write("<td><input type=text size=10 name=price value='" + m_smPrice + "'></td>");
	Response.Write("<td><input type=text size=10 name=stock value='" + m_smStock + "'></td>");
	Response.Write("<td><input type=text size=10 name=eta value='" + m_smEta + "'></td>");
	Response.Write("</tr>");
	Response.Write("</table>");

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value='Test Mapping' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Save Settings' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
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
}

bool DoTestDSN()
{
	OdbcConnection conn = new OdbcConnection("DSN=" + m_sDSN);
	int rows = 0;
	string sc = " SELECT * FROM " + m_sTable;
	try
	{
		conn.Open();
		OdbcDataAdapter da = new OdbcDataAdapter(sc, conn);
		da.FillError += new FillErrorEventHandler(On_Odbc_FillError);
		rows = da.Fill(dst, "dsn_test");
	}
	catch(Exception e) 
	{
		string se = e.ToString();
		if(se.IndexOf("NO_DATA") < 0 && se.IndexOf("Driver does not support this function") < 0) //NO_DATA seems means no error,  (DW)
		{
			conn.Close();
			Response.Write(e);
			return false;
		}
	}

	BindGrid("dsn_test");
	conn.Close();
	return true;
}

bool DoTestMapping()
{
	OdbcConnection conn = new OdbcConnection("DSN=" + m_sDSN);
	int rows = 0;
	string sc = " SELECT cards.name AS supplier, items.* ";
	sc += " FROM items LEFT OUTER JOIN cards ON cards.CardRecordID = items.PrimarySupplierID ";
	sc += " WHERE items.IsInactive = 'N' ";
	try
	{
		conn.Open();
		OdbcDataAdapter da = new OdbcDataAdapter(sc, conn);
		da.FillError += new FillErrorEventHandler(On_Odbc_FillError);
		rows = da.Fill(dst, "mapping_test");
	}
	catch(Exception e) 
	{
		string se = e.ToString();
		if(se.IndexOf("NO_DATA") < 0 && se.IndexOf("Driver does not support this function") < 0) //NO_DATA seems means no error,  (DW)
		{
			conn.Close();
			Response.Write(e);
			return false;
		}
	}
	conn.Close();

	DataTable dtt = new DataTable();
	dtt.Columns.Add(new DataColumn("code", typeof(String)));
	dtt.Columns.Add(new DataColumn("supplier", typeof(String)));
	dtt.Columns.Add(new DataColumn("supplier_code", typeof(String)));
	dtt.Columns.Add(new DataColumn("name", typeof(String)));
	dtt.Columns.Add(new DataColumn("brand", typeof(String)));
	dtt.Columns.Add(new DataColumn("cat", typeof(String)));
	dtt.Columns.Add(new DataColumn("s_cat", typeof(String)));
	dtt.Columns.Add(new DataColumn("ss_cat", typeof(String)));
	dtt.Columns.Add(new DataColumn("cost", typeof(String)));
	dtt.Columns.Add(new DataColumn("price", typeof(String)));
	dtt.Columns.Add(new DataColumn("stock", typeof(String)));
	dtt.Columns.Add(new DataColumn("eta", typeof(String)));

	for(int i=0; i<dst.Tables["mapping_test"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["mapping_test"].Rows[i];
		DataRow drn = dtt.NewRow();
		drn["code"] = dr["ItemID"].ToString();
		drn["supplier"] = dr[m_smSupplier].ToString();
		drn["supplier_code"] = dr[m_smCode].ToString();
		drn["name"] = dr[m_smName].ToString();
		if(m_smBrand != "")
			drn["brand"] = dr[m_smBrand].ToString();
		if(m_smCat != "")
			drn["cat"] = dr[m_smCat].ToString();
		if(m_smSCat != "")
			drn["s_cat"] = dr[m_smSCat].ToString();
		if(m_smSSCat != "")
			drn["ss_cat"] = dr[m_smSSCat].ToString();
		if(m_smCost != "")
			drn["cost"] = dr[m_smCost].ToString();
		if(m_smPrice != "")
			drn["price"] = dr[m_smPrice].ToString();
		if(m_smStock != "")
			drn["stock"] = dr[m_smStock].ToString();
		if(m_smEta != "")
			drn["eta"] = dr[m_smEta].ToString();
		dtt.Rows.Add(drn);
	}

	DataView dv = new DataView(dtt);
	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();
	return true;
}

void BindGrid(string sTable)
{
	DataView dv = new DataView(dst.Tables[sTable]);
	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
//	BindGrid();
}

bool DoUpdateStock()
{
	OdbcConnection conn = new OdbcConnection("DSN=" + m_sDSN);
	int rows = 0;
	string sc = " SELECT cards.name AS supplier, items.* ";
	sc += " FROM items LEFT OUTER JOIN cards ON cards.CardRecordID = items.PrimarySupplierID ";
	sc += " WHERE items.IsInactive = 'N' ";
	sc += " ORDER BY items.ItemID ";
	try
	{
		conn.Open();
		OdbcDataAdapter da = new OdbcDataAdapter(sc, conn);
		da.FillError += new FillErrorEventHandler(On_Odbc_FillError);
		rows = da.Fill(dst, "mapping_test");
	}
	catch(Exception e) 
	{
		string se = e.ToString();
		if(se.IndexOf("NO_DATA") < 0 && se.IndexOf("Driver does not support this function") < 0) //NO_DATA seems means no error,  (DW)
		{
			conn.Close();
			Response.Write(e);
			return false;
		}
	}
	conn.Close();

	DataTable dtt = new DataTable();
	dtt.Columns.Add(new DataColumn("code", typeof(String)));
	dtt.Columns.Add(new DataColumn("supplier", typeof(String)));
	dtt.Columns.Add(new DataColumn("supplier_code", typeof(String)));
	dtt.Columns.Add(new DataColumn("name", typeof(String)));
	dtt.Columns.Add(new DataColumn("brand", typeof(String)));
	dtt.Columns.Add(new DataColumn("cat", typeof(String)));
	dtt.Columns.Add(new DataColumn("s_cat", typeof(String)));
	dtt.Columns.Add(new DataColumn("ss_cat", typeof(String)));
	dtt.Columns.Add(new DataColumn("cost", typeof(String)));
	dtt.Columns.Add(new DataColumn("price", typeof(String)));
	dtt.Columns.Add(new DataColumn("stock", typeof(String)));
	dtt.Columns.Add(new DataColumn("eta", typeof(String)));

	int i = 0;
	for(i=0; i<dst.Tables["mapping_test"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["mapping_test"].Rows[i];
		DataRow drn = dtt.NewRow();
		drn["code"] = dr["ItemID"].ToString();
		drn["supplier"] = dr[m_smSupplier].ToString();
		drn["supplier_code"] = dr[m_smCode].ToString();
		drn["name"] = dr[m_smName].ToString();
		if(m_smBrand != "")
			drn["brand"] = dr[m_smBrand].ToString();
		if(m_smCat != "")
			drn["cat"] = dr[m_smCat].ToString();
		if(m_smSCat != "")
			drn["s_cat"] = dr[m_smSCat].ToString();
		if(m_smSSCat != "")
			drn["ss_cat"] = dr[m_smSSCat].ToString();
		if(m_smCost != "")
			drn["cost"] = dr[m_smCost].ToString();
		if(m_smPrice != "")
			drn["price"] = dr[m_smPrice].ToString();
		if(m_smStock != "")
			drn["stock"] = dr[m_smStock].ToString();
		if(m_smEta != "")
			drn["eta"] = dr[m_smEta].ToString();
		dtt.Rows.Add(drn);
	}

	string sc = " DELETE code_relations_new ";
	sc += " DELETE product_new ";
	sc += " SELECT * FROM code_relations ORDER BY code ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "code_relations");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int rows = dst.Tables["code_relations"].Rows.Count;

	int j = 0;
	for(i=0; i<dtt.Rows.Count; i++)
	{
		DataRow dr1 = dtt.Rows[i];
		DataRow dr2 = null;
		if(j < dst.Tables["code_relations"].Rows.Count)
			dr2 = dst.Tables["code_relations"].Rows[j];

		int nID = 0; //myob code
		int nCode = 0; //sql code

		nID = MyIntParse(dr1["ItemID"].ToString());
		if(dr2 != null)
			nCode = MyIntParse(dr2["code"].ToString());
		else
		{
			DoAddNew(dr1);
			continue;
		}

		if(nID < nCode) // new
		{
			DoAddNew(dr1); //add new, j stays same, next loop
		}
		else if(nID > nCode) //
		{
			while(nID > nCode)
			{
				DoInactive(nCode);
				j++; //next code
				if(j >= rows)
					break;
				dr2 = dst.Tables["code_relations"].Rows[j];
				nCode = MyIntParse(dr2["code"].ToString());
			}
			if(nID == nCode) //finally we found one, check changes
			{
				CheckChanges(dr1, dr2);
				j++;
			}
		}
		else
		{
			CheckChanges(dr1, dr2);
			j++; //next code
		}
	}

}

bool CheckChanges(DataRow dr1, DataRow dr2)
{
	string[] s = new string[16];
	s[0] = "supplier";
	s[1] = "supplier_code";
	s[2] = "name";
	s[3] = "price";
	s[4] = "stock";
//	s[5] = "eta";

	string update = "";
	for(int i=0; i<5; i++)
	{
		if(dr1[s[i]].ToString() != dr2[s[i]].ToString())
		{
			update += ", " + s[i] + " = '" + dr1[s[i]].ToString() + "' ";
		}
	}
	if(update == "")
		return true;

	//do update
	m_nUpdated++;
return true;
	string sc = " UPDATE code_relations SET inactive=0, id=";
	sc += EncodeQuote(dr1["supplier"].ToString()) + EncodeQuote(dr1["supplier_code"].ToString());
	sc += "' " + update + " WHERE code=" + dr2["code"].ToString();
	sc += " UPDATE product SET supplier = '" + EncodeQuote(dr1["supplier"].ToString()) + "' ";
	sc += ", supplier_code = '" + EncodeQuote(dr1["supplier_code"].ToString()) + "' ";
	sc += ", name = '" + EncodeQuote(dr1["name"].ToString()) + "' ";
	sc += ", price = " + dr1["price"].ToString();
	sc += ", stock = " + dr1["stock"].ToString();
	sc += " WHERE code = " + dr2["code"].ToString();
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

bool DoAddNew(DataRow dr)
{
	string sc = " INSERT INTO code_relations_new (code, id, suppleir, supplier_code, name, price, stock, eta) ";
	sc += " VALUES( ";
	sc += dr["code"].ToString();
	sc += ", '" + EncodeQuote(dr["supplier"].ToString()) + EncodeQuote(dr["supplier_code"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["supplier"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["supplier_code"].ToString()) + "' ";
	sc += ", '" + EncodeQuote(dr["name"].ToString()) + "' ";
	sc += ", " + dr["price"].ToString();
	sc += ", " + dr["stock"].ToString();
	sc += ", '" + EncodeQuote(dr["eta"].ToString()) + "' ";
	sc += ") ";
	sc += " INSERT INTO product_new (id, price, stock, eta) VALUES( ";
	sc += " '" + EncodeQuote(dr["supplier"].ToString()) + EncodeQuote(dr["supplier_code"].ToString()) + "' ";
	sc += ", " + dr["price"].ToString();
	sc += ", " + dr["stock"].ToString();
	sc += ", '" + EncodeQuote(dr["eta"].ToString()) + "' ";
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
	m_nNew++;
	return true;
}

bool DoInactive(int code)
{
	m_nDisappeared++;
return true;
	string sc = " UPDATE code_relations SET inactive = 1 WHERE code = " + code;
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

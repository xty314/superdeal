<script runat=server>
DataSet ds = new DataSet();
string sType = "";
string uri = "";
string m_kc = ""; //selected sku category
void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad();
	if(!SecurityCheck("administrator"))
		return;
	sType = g("type");
	string action = g("a");
	string branch_id = g("branch_id");
	string t = g("t");
	string id = g("id");
	string barcode = g("barcode");
	string code = g("code");
	m_kc = g("kc").ToUpper();
	PrintAdminHeader();
	PrintAdminMenu();
	if(action == "pa") //process all
	{
		if(DoProcessAll())
		{
			Response.Write("<br><center><h3>" + Lang("Done, please wait a second") + "</h3>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=?kc=" + HttpUtility.UrlEncode(m_kc) + "&type=" + sType + "&branch_id=" + branch_id + "&r=" + DateTime.Now.ToOADate() + "\">");
		}
		return;
	}
	if(g("com") != "" || branch_id == "")
	{
		DoDeleteTempData();
	}
	if(t != "")
	{
		if(t == "del")
		{
			DoChangeInstock(id, t, "");
		}
		if(t == "edit")
		{
			DoChangeInstock(id, t, g("qty"));
		}
		if(t == "y")
		{
			if(DoUpdateStock_Qty(branch_id, code, MyIntParse(g("qty")), MyIntParse(g("oldqty"))))
			{
				Response.Write("<br/><center><h3>Done</h3><br/><input type=button class=b value='" + Lang("Close Window") + "' onclick=window.close()>");
				return;
			}
		}
		if(t == "in")
		{
			DoChangeInstock(id, "insert", g("qty"));
			DoUpdateStock_Qty(branch_id, code, MyIntParse(g("qty")), MyIntParse(g("oldqty")));
		}
	}
	if(t == "" && barcode != "")
	{
		if(CheckBarcode(barcode))
		{
			InputInstockTable(branch_id, g("qty"), barcode);	
		}
		else
		{
			string js = "<script language=javascript>";
			js += "alert('" + Lang("barcode is not exists") + "');\r\n ";
			js += "</";
			js += "script>";
			Response.Write(js);
		}
	}
	PrintMainPage();
	PrintAdminFooter();
}

string GetCode(string barcode)
{
	if(ds.Tables["code"] != null)
		ds.Tables["code"].Clear();
	int Rows = 0;
	string sc = " SELECT code FROM code_relations ";
	if(barcode != "")
		sc += " WHERE barcode = '" + EncodeQuote(barcode) + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "code");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	if(Rows > 0)
	{
		return ds.Tables["code"].Rows[0][0].ToString();
	}
	else
		return "";
}

string GetOldStockQty(string code,string branch_id)
{
	if(ds.Tables["stock_qty"] != null)
		ds.Tables["stock_qty"].Clear();
	int Rows = 0;
	string sc = " SELECT qty FROM stock_qty ";
	sc += " WHERE 1 = 1";
	sc += " AND branch_id = " + branch_id ;
	sc += " AND code = '" + code + "'"; 
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "stock_qty");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	if(Rows > 0)
	{
		return ds.Tables["stock_qty"].Rows[0][0].ToString();
	}
	else
		return "";
}

bool DoUpdateStock_Qty(string branch_id, string code, int nNewQty, int nOldQty)
{
	if(nNewQty == nOldQty)
		return true;
	string sc = " BEGIN TRANSACTION ";
	sc += " IF EXISTS(SELECT id FROM stock_qty ";
	sc += " WHERE 1 = 1 ";
	sc += " AND branch_id = " + branch_id;
	sc += " AND code = '" + code + "'";
	sc += ") UPDATE stock_qty SET qty = " + nNewQty;
	sc += " WHERE 1 = 1 ";
	sc += " AND branch_id = " + branch_id;
	sc += " AND code = '" + code + "'";
	sc += " ELSE ";
	sc += " INSERT INTO stock_qty ( ";
	sc += " qty, branch_id, code) VALUES ( ";
	sc += nNewQty + ", " + branch_id + ", '" + code + "')";
	sc += " INSERT INTO stock_adj_log (staff, code, qty, branch_id, note) ";
	sc += " VALUES(" + Session["card_id"].ToString();
	sc += ", " + code + ", " + (nNewQty - nOldQty).ToString() + ", " + branch_id + ", N'" + Lang("Import Stock") + "') "; 
	sc += " COMMIT ";
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

bool DoProcessAll()
{
	int nRows = 0;
	string branch_id = g("branch_id");
	string sc = " SELECT i.code, i.qty, sq.qty AS old_qty ";
	sc += " FROM instock i ";
	sc += " LEFT OUTER JOIN stock_qty sq ON sq.branch_id = i.branch_id AND sq.code = i.code ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = i.code ";
	sc += " WHERE i.branch_id = " + branch_id;
	if(m_kc != "")
		sc += " AND c.cat = N'" + EncodeQuote(m_kc) + "' ";
	sc += " UNION ";
	sc += " SELECT sq.code, i.qty, sq.qty AS old_qty ";
	sc += " FROM stock_qty sq ";
	sc += " LEFT OUTER JOIN instock i ON sq.branch_id = i.branch_id AND sq.code = i.code ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = i.code ";
	sc += " WHERE sq.branch_id = " + branch_id ;
	if(m_kc != "")
		sc += " AND c.cat = N'" + EncodeQuote(m_kc) + "' ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(ds, "process_all_instock");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	for(int i=0; i<nRows; i++)	
	{
		DataRow dr = ds.Tables["process_all_instock"].Rows[i];
		string code = dr["code"].ToString();
		int nQty = MyIntParse(dr["qty"].ToString());
		int nOldQty = MyIntParse(dr["old_qty"].ToString());
		if(nQty != nOldQty)
		{
			DoUpdateStock_Qty(branch_id, code, nQty, nOldQty);
		}
	}
	return true;
}

void DoDeleteTempData()
{
	string sc = " DELETE FROM instock WHERE 1 = 1 ";
	if(g("branch_id") != "")
		sc += " AND branch_id = " + g("branch_id");
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
	}
	if(g("com") != "")
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?r=" + DateTime.Now.ToOADate() + "\">");
}

bool DoChangeInstock(string id, string Tag, string qty)
{
	string sc = "";
	if(Tag == "del")
		sc = " DELETE FROM instock ";
	if(Tag == "edit")
		sc = " UPDATE instock SET qty = " + qty;
	sc += " WHERE id = " + id;
	if(Tag == "insert")
	{
		sc = " INSERT INTO instock (qty, code, branch_id) VALUES ( ";
		sc += qty + ", '" + g("code") + "', " + g("branch_id") + " )";
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

void InputInstockTable(string branch_id, string qty, string code)
{
	string sql = " WHERE 1 = 1 ";
	sql += " AND branch_id = " + branch_id;
	sql += " AND code = '" + code + "' ";
	string sc = " IF EXISTS(SELECT id FROM instock ";
	sc += sql;
	sc += " )";
	sc += " UPDATE instock SET qty = qty + " + qty;
	sc += sql;
	sc += " ELSE ";
	sc += " INSERT INTO instock (qty, code, branch_id) VALUES ( ";
	sc += qty + ", '" + code + "', " + branch_id + " )";
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
	}
}

bool GetStockResult(string branch_id)
{
	int Rows = 0;
	string sc = "";
	sc += " SELECT i.id, i.code, c.supplier_code, c.name AS item_name, sq.qty AS stock_qty, i.qty AS instock_qty ";
	sc += " FROM instock i ";
	sc += " LEFT OUTER JOIN stock_qty sq ON sq.code = i.code AND sq.branch_id = i.branch_id";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = i.code ";
	sc += " WHERE 1 = 1 ";
	sc += " AND i.branch_id = " + branch_id ;
	if(m_kc != "")
		sc += " AND c.cat = N'" + EncodeQuote(m_kc) + "' ";
	if(sType == "-1")
		sc += " AND sq.qty = i.qty ";
	else if(sType == "1" || sType == "3") //1:not equal 3:changes in file
		sc += " AND (sq.qty <> i.qty OR sq.qty IS NULL) ";
	else if(sType == "" || sType == "0" || sType == "1") 
		sc += " AND sq.qty <> 0 ";
/*		
	if(sType == "" || sType == "0" || sType == "1") 
	{
		sc += " UNION";
		sc += " SELECT sq.code, c.supplier_code, c.name AS item_name, sq.qty AS stock_qty, '0' AS instock_qty, sq.id ";
		sc += " FROM stock_qty sq ";
		sc += " LEFT OUTER JOIN instock i ON i.branch_id = sq.branch_id AND i.code = sq.code ";
		sc += " LEFT OUTER JOIN code_relations c ON c.code = sq.code ";
		sc += " WHERE sq.branch_id =  " + branch_id;
		sc += " AND sq.qty <> 0 ";
		if(m_kc != "")
			sc += " AND c.cat = N'" + EncodeQuote(m_kc) + "' ";
	}
*/ 
	sc += " ORDER BY i.code ";
//DEBUG("sc=", sc);	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "stock_result");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return Rows>0 ? true : false;
}

bool GetInstockResult(string branch_id)
{
	int Rows = 0;
	string sc = "";
	sc += " SELECT s.code ,ISNULL(s.qty,0) as stock_qty,'0' as instock_qty, '0' AS id ";
	sc += ", c.supplier_code, c.name AS item_name ";
	sc += " FROM stock_qty s";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = s.code ";
	sc += " WHERE 1 = 1 ";
	sc += " AND s.branch_id = " + branch_id;
	sc += " AND s.qty != 0 ";
	sc += " AND s.code NOT IN (SELECT code FROM instock WHERE branch_id = " + branch_id + ")";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "instock_result");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return Rows>0 ? true : false;
}

string InStockQty(string branch_id)
{
	int Rows = 0;
	string sc = "";
	sc += " SELECT Sum(qty) FROM instock WHERE ";
	sc += " branch_id = " + branch_id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "instock_qty");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	return Rows == 1 ? ds.Tables["instock_qty"].Rows[0][0].ToString() : "";
}

bool CheckBarcode(string barcode)
{
	DataSet ds = new DataSet();
	int Rows = 0;
	string sc = "";
	sc += " SELECT DISTINCT * "; 
	sc += " FROM code_relations cr ";
//	sc += " LEFT OUTER JOIN barcode b ON cr.code = b.item_code ";
//	sc += " LEFT OUTER JOIN sku s on s.code = cr.code ";
//	sc += " LEFT OUTER JOIN sku_item si on s.id = si.sku_id ";
	sc += " WHERE 1 = 1 ";
	if(barcode != "")
		sc += " AND (c.barcode = '" + EncodeQuote(barcode) + "' ";//OR si.barcode = '" + barcode + "')";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "CheckBarcode");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(Rows > 0)
		return true;
	else
		return false;
}

string GetAllBranchs()
{
	if(ds.Tables["branchs"] != null)
		ds.Tables["branchs"].Clear();
	int rows = 0;
	string sc = " SELECT id, name FROM branch ";
	sc += " WHERE activated = 1 ";
	string sErrorString = "<select><option value=\"-1\">" + Lang("NOT FOUND") + "</option></select>";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "branchs")==0)
			return sErrorString;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return sErrorString;
	}
	StringBuilder sb = new StringBuilder();
	sb.Append("<select name=branch");
	sb.Append(" onchange=\"window.location=('");
	sb.Append(uri + "&branch_id='+ this.options[this.selectedIndex].value ) \" ");
	sb.Append(">");
	sb.Append("<option value=\"-1\">" + Lang("ALL") + "</option>");
	for(int i=0; i<ds.Tables["branchs"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["branchs"].Rows[i];
		string sId = dr["id"].ToString();
		string name = dr["name"].ToString();
		sb.Append("<option value=\"" + sId + "\"");
		if(p("branch") != "")
		{
			if(p("branch") == sId )
				sb.Append(" selected ");
		}
		else
		{
			if(g("branch_id") == sId)
				sb.Append(" selected ");
		}
		sb.Append(">" + name + "</option>");
	}
	sb.Append("</select>");
	return sb.ToString();
}

void PrintMainPage()
{
	string total = "";
	string branch_id = g("branch_id");
	if(branch_id != "" && g("action") == "")
	{
		GetStockResult(branch_id);
        total = InStockQty(branch_id);
	}
	string barcode = "";
	string qty = "1";
	if(g("barcode") != "" && g("t") == "")
		barcode = g("barcode");
	if(g("b")!= "")
		barcode = g("b");
	if(g("qty") != "" && g("t") == "")
		qty = g("qty");
	string js = "<script language=javascript>";
/*	js += "function CheckForm(){\r\n";
	js += " var barcode = document.f.barcode.value;\r\n";
	js += " var qty = document.f.qty.value;\r\n";
	js += " var branch_id = document.f.branch.value;\r\n";
	js += " if(barcode == ''){\r\n";
	js += "alert('" + Lang("Enter Barcode") + "');\r\n";
	js += "document.f.barcode.focus();\r\n";
	js += "return false;}\r\n";
	js += "else if(isNaN(qty)){\r\n";
	js += "alert('" + Lang("Enter QTY") + "');\r\n";
	js += "document.f.qty.value='1';\r\n";
	js += "document.f.qty.select();\r\n";
	js += "return false;}\r\n";
	js += "else{";
	js += "window.location=('instock.aspx?r=" + DateTime.Now.ToOADate() + "&branch_id='+branch_id+'&barcode='+barcode+'&qty='+qty);";
	js += "}}\r\n";
*/
	js += " function forCheckUploadFile(){\r\n";
	js += " var file = document.f.fileName.value;\r\n";
	js += " if(file == ''){\r\n";
	js += " alert('" + Lang("first upload file") + "');\r\n";
	js += " return false;}\r\n";
	js += " if(confirm('" + Lang("Are you sure upload for change stock") + "')){\r\n ";
	js += " if(file.indexOf('txt')<0 || file.indexOf('csv')<0){\r\n";
	js += " alert('" + Lang("file format error") + "');\r\n";
	js += " document.f.fileName.select();\r\n";
	js += " return false}\r\n";
	js += " else\r\n";
	js += " return true;\r\n";
	js += " }\r\n";
	js += " return false;\r\n";
	js += "}";
	js += "</script";
	js += ">";
	string style_str = "style='width:80px'";
	StringBuilder sb = new StringBuilder();
	sb.Append(js);
	sb.Append("<br><form name=f method=post encType=multipart/form-data runat='server' >");
	sb.Append("<center><font size=+1 color=#656565><b>" + Lang("Check Stock") + "</b></font> -- <b><font color=#add300>");
	string stitle = Lang("All");
	if(sType == "1")
		stitle = Lang("NOT Equals");
	else if(sType == "-1")
		stitle = Lang("Equals");
	sb.Append(stitle + "</b></font>");
	sb.Append("<table width=70%>");
	sb.Append("<tr><td>");
	sb.Append("<b><font color=#656565>" + Lang("Branch") + "&nbsp;:&nbsp;</font></b>");
	uri = "instock.aspx?action=sel&r="+DateTime.Now.ToOADate();
	sb.Append(GetAllBranchs());
/*	sb.Append("&nbsp;&nbsp;<b><font color=#656565>" + Lang("barcode") + "&nbsp;:&nbsp;</font></b>");
	sb.Append("<input type=text name=barcode " + style_str.Replace("80","120") + " value='' ");
	sb.Append("  autocomplete=off  onKeyDown=\"if(event.keyCode==13)CheckForm();\">");
	sb.Append("&nbsp;&nbsp;<b><font color=#656565>" + Lang("QTY") + "&nbsp;:&nbsp;</font></b>");
	sb.Append("<input type=text autocomplete=off onKeyDown=\"if(event.keyCode==13)CheckForm();\" name=qty " + style_str.Replace("80","30") + " value='1'>");
	sb.Append("&nbsp;&nbsp;");
	sb.Append("<input type=button class=b " + style_str + " value='" + Lang("ENTER") + "'");
	sb.Append(" onclick=\"CheckForm();\">\r\n");
*/ 
	sb.Append("<input class=b  type=button value='" + Lang("Upload stock file") + "'  title='" + Lang("Upload stock file") + "' ");
	sb.Append(" onclick=\"javascript:if(document.f.branch.value=='-1') {alert('" + Lang("Please Selected One Branch") + "');");
	sb.Append("return false;} ");
	sb.Append("else {window.open('upstock.aspx?branch_id=' + document.f.branch.value, '',");
	sb.Append(" 'width=550, height=180,top=180,left=280,scrollbars=0, resizable=1');}\" ");
	sb.Append(" >");
//	sb.Append("<script language=javascript>document.f.barcode.focus();</script");
//	sb.Append(">");
	sb.Append("</td></tr>");
	sb.Append("<tr><td>");

	int iQTY = 0;
	int sQTY = 0;
	int nRows = 0;
	if(ds.Tables["stock_result"] != null)
		nRows = ds.Tables["stock_result"].Rows.Count;
	if(branch_id == "" || ds.Tables["stock_result"] == null)
	{
		Response.Write(sb.ToString());
		Response.Write("</td></tr></table>");
		return;
	}

	sb.Append("<table width=100% cellspacing=1 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=#FFFFFFFF ");
	sb.Append(" class=t>");
	sb.Append("<tr><td colspan=6>");
	sb.Append("<font size=4 color=#add300><b>" + Lang("Total QTY") + "&nbsp;:&nbsp;");
	sb.Append(total.ToString() + "</b></font>&nbsp;&nbsp;");
	sb.Append("<input type=button class=b value=\"" + Lang("ALL") + "\" onclick=\"window.location=('instock.aspx?kc=" + HttpUtility.UrlEncode(m_kc) + "&type=0&branch_id=" + branch_id + "');\">");
	sb.Append("&nbsp;");
	sb.Append("<input type=button class=b value=\"" + Lang("NOT Equals") + "\" onclick=\"window.location=('instock.aspx?kc=" + HttpUtility.UrlEncode(m_kc) + "&type=1&branch_id=" + branch_id + "');\">");
	sb.Append("&nbsp;");
	sb.Append("<input type=button class=b value=\"" + Lang("Equals") + "\" onclick=\"window.location=('instock.aspx?kc=" + HttpUtility.UrlEncode(m_kc) + "&type=-1&branch_id=" + branch_id + "');\">");
	sb.Append("&nbsp;");
	sb.Append("<font color=#656565>" + Lang("Category") + ":</font><select name=cat onchange=\"window.location=('instock.aspx?&kc=' + this.options[this.selectedIndex].value + '&type=" + sType + "&branch_id=" + branch_id + "');\"><option value=''>" + Lang("All") + "</option>" + PrintCatOptions(m_kc) + "</select>");
	sb.Append("&nbsp;");
	sb.Append("<input type=button class=b value=\"" + Lang("complete") + "\" ");
	sb.Append("	onclick=\"javascript:if(confirm('" + Lang("Are you sure complete") + "?')){window.location=('instock.aspx?kc=" + HttpUtility.UrlEncode(m_kc) + "&com=yes&branch_id=" + branch_id + "');}else{return false;}\">");
	sb.Append("&nbsp;");
	sb.Append("<input type=button class=b value=\"" + Lang("Process All") + "\" onclick=\"window.location=('instock.aspx?kc=" + HttpUtility.UrlEncode(m_kc) + "&a=pa&type=" + sType + "&branch_id=" + branch_id +"');\">");
	sb.Append("&nbsp;");
	sb.Append("</td></tr>");
	sb.Append("<tr>");
	sb.Append("<th>" + Lang("Code") + "</th>");
	sb.Append("<th>" + Lang("M_PN") + "</th>");
	sb.Append("<th>" + Lang("Product Name") + "</th>");
	sb.Append("<th>" + Lang("Stock QTY") + "</th>");
	sb.Append("<th>" + Lang("QTY") + "</th>");
	sb.Append("<th>" + Lang("Action") + "</th>");
	sb.Append("<tr>");
	string fColor = "<b><i><font color=red></font></i></b>";
	for(int i=0; i<nRows; i++)
	{	
		DataRow dr = ds.Tables["stock_result"].Rows[i];
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string stock_qty = dr["stock_qty"].ToString();
		string instock_qty = dr["instock_qty"].ToString();
		string item_name = dr["item_name"].ToString();
		bool bEquals = false;
		string id = dr["id"].ToString();
		if(MyIntParse(stock_qty) == MyIntParse(instock_qty))
			bEquals = true;
		bool bEdit = false;
		if(g("id") == id && g("t") == "edit_status")
			bEdit = true;
		iQTY += MyIntParse(instock_qty);
		sQTY += MyIntParse(stock_qty);
		sb.Append("<tr");
		if(i%2 != 0)
			sb.Append(" class=tn");
		else
			sb.Append(" class=ta");
		sb.Append(">");
		sb.Append("<td width=10% class=tb>" + code + "</td>");
		sb.Append("<td width=10% class=tb>" + supplier_code + "</td>");
		sb.Append("<td width=20% class=tb>" + item_name + "</td>");
		sb.Append("<td width=20% align=center class=tb>");
		if(bEquals)
			sb.Append(fColor.Replace("</f",stock_qty + "</f"));
		else
			sb.Append(stock_qty);
		sb.Append("</td>");
		sb.Append("<td width=20% align=center class=tb>");
		if(bEdit)
		{
			sb.Append("<input type=text " + style_str.Replace("80","120") + " name=e_qty value='" + instock_qty + "'>");
		}
		else
		{
			if(bEquals)
				sb.Append("<b><i><font color=blue>" + instock_qty + "</font></i></b>");
			else							
				sb.Append(instock_qty);
		}
		sb.Append("</td>");
		if(!bEdit)
		{
			sb.Append("<td align=right nowrap class=tb><a href='?r=" + DateTime.Now.ToOADate() + "&branch_id=" + branch_id + "&kc=" + HttpUtility.UrlEncode(m_kc) + "&t=edit_status&id=" + id + "&type=" + sType + "' class=o style='color:#2f3563'>" + Lang("Edit") + "</a>");
			sb.Append("&nbsp;&nbsp;<a href='?r=" + DateTime.Now.ToOADate() + "&branch_id=" + branch_id + "&kc=" + HttpUtility.UrlEncode(m_kc) + "&t=del&id=" + id + "&type=" + sType + "'");
			sb.Append(" onclick=\"if(!confirm('" + Lang("Do you Sure Delete") + "')){return false;}\" ");
			sb.Append("class=o >" + Lang("DELETE") + "</a>");
			sb.Append("&nbsp;&nbsp;");
			sb.Append("<input type=button class=b value='" + Lang("Pass") + "'");
			sb.Append(" onclick=\"if(confirm('" + Lang("Pass") + "?'))window.open('?&r=" + DateTime.Now.ToOADate() + "&t=y&id=" + id + "&qty=" + instock_qty + "&oldqty=" + stock_qty + "&code=" + code + "&branch_id=" + branch_id + "&kc=" + HttpUtility.UrlEncode(m_kc) + "');else{return false;}\">");
			sb.Append("</td>");
		}
		else
		{
			sb.Append("<td align=right nowrap class=tb><input type=button class=b value='" + Lang("Update") + "'");
			sb.Append(" onclick=\"javascript:if(!isNaN(document.f.e_qty.value))window.location=('instock.aspx?r=" + DateTime.Now.ToOADate() + "&branch_id=" + branch_id + "&kc=" + HttpUtility.UrlEncode(m_kc) + "&t=edit&type=" + sType + "&id=" + id + "&qty='+document.f.e_qty.value);else{alert('" + Lang("please enter number") + "');return false;}\">");
			sb.Append("&nbsp;&nbsp;");
			sb.Append("<input type=button class=b value='" + Lang("Cancel") + "' onclick=\"window.location=('instock.aspx?type=" + sType + "&branch_id=" + branch_id + "&kc=" + HttpUtility.UrlEncode(m_kc) + "&r=" + DateTime.Now.ToOADate() + "')\">");
			sb.Append("</td>");
		}
		sb.Append("</tr>") ;
	}
	sb.Append("<tr>");
	sb.Append("<th colspan=2></th>");
	sb.Append("<th align=right><b>" + Lang("Total") + "&nbsp;:&nbsp;</b></th>");
	sb.Append("<th align=center><b>" + sQTY.ToString() + "</b></th>");
	sb.Append("<th align=center><b>" + iQTY.ToString() + "</b></th><th></th>");
	sb.Append("</tr>");
	sb.Append("</table>");
	sb.Append("</td></tr>");
	sb.Append("</table>");
	Response.Write(sb);
}

void OutPutCSVFile()
{
	GetStockResult(g("branch_id"));
	string fileName = Server.MapPath("data/stock/") + GetBranchName(g("branch_id")) + DateTime.Now.ToLongDateString().ToString() + ".csv";//file all path
	DataTable dt = new DataTable();
	dt = ds.Tables["instock"];
	StringBuilder sb = new StringBuilder();
	using(System.IO.FileStream file = new System.IO.FileStream(fileName,System.IO.FileMode.OpenOrCreate,System.IO.FileAccess.Write))
	{
		using(System.IO.StreamWriter wr = new System.IO.StreamWriter(file,System.Text.Encoding.Default))
		{
			foreach(DataRow row in dt.Rows)   
			{   
				sb.Append(row["code"].ToString() + "," + row["qty"].ToString() + "\r\n");
			}   
			wr.Write(sb.ToString());   
			wr.Close();   
		}
	}
	StringBuilder sb2 = new StringBuilder();
	sb2.Append("<h2><b>" + Lang("Processing, wait") + "...</b></h2>");
	sb2.Append("<font size=6 color=red><b>" + Lang("File Path") + "&nbsp;:&nbsp;");
	sb2.Append(fileName + "</b></font>");
	Response.Write(sb2.ToString());
	Response.Write("<meta http-equiv=\"refresh\" content=\"5; URL=?r=" + DateTime.Now.ToOADate() + "\">");
}

string GetStockQty(string code)
{
	DataSet dst = new DataSet();
	int Rows = 0;
	string sc = " SELECT qty FROM stock_qty";
	sc += " WHERE 1 = 1 ";
	sc += " AND branch_id = " + g("branch_id");
	sc += " AND code = '" + code + "' ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(dst);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	if(Rows == 0)
		return "0";
	string qty = dst.Tables[0].Rows[0][0].ToString(); 
	if(qty == null)
		return "0";
	return qty;
}

string GetBranchName(string branch_id)
{
	int Rows = 0;
	string sc = " SELECT name FROM branch WHERE id = " + branch_id;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		Rows = myCommand.Fill(ds, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return Rows >0 ? ds.Tables["branch"].Rows[0][0].ToString() : "";
}

string PrintCatOptions(string sCurrent)
{
	int nRows = 0;
	DataSet dst = new DataSet();
	string sc = " SELECT DISTINCT cat FROM code_relations ORDER BY cat ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(dst,"cat_op");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	string s = "";
	sCurrent = sCurrent.ToUpper();
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = dst.Tables["cat_op"].Rows[i];
		string cat = dr["cat"].ToString().ToUpper();
		s += "<option value='" + cat + "'";
		if(cat == sCurrent)
			s += " selected";
		s += ">" + cat + "</option>";
	}
	return s;
}
</script>

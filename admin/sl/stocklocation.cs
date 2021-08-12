// <!-- #include file="/cs/page_index.cs" -->
// <script runat=server>

// DataSet ds = new DataSet();
// string m_type = "";	//query type &t=
// string m_action = "";	//query action &a=
// string m_cmd = "";		//post button value, name=cmd
// string m_kw = "";

// void Page_Load(Object Src, EventArgs E ) 
// {
// 	m_bCheckLogin = false;
// 	m_bDealerArea = false;

// 	TS_PageLoad(); //do common things, LogVisit etc...

    
// 	m_type = g("t");
// 	m_action = g("a");
// 	m_kw = p("kw");
// 	if(m_kw == "")
// 		m_kw = g("kw");
// 	m_cmd = p("cmd");
	
// 	if(m_cmd == "Save")
// 	{
// 		if(DoUpdateData())
// 		{
// 			// PrintAdminHeader();
// 			Response.Write("<br><br><br><center><h3>Stock Location saved, please wait a moment.");
// 			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
// 		}
// 		return; //if it's a form post then do nothing else, quit here
// 	}
// 	// PrintAdminHeader();
// //	PrintAdminMenu();
// 	PrintMainForm();
// }
// bool PrintMainForm()
// {
// 	Response.Write("<br><center><h4>Stock Location</h4></center>");
// 	Response.Write("<form name=f action=? method=post>");
// 	Response.Write("<table width=666 align=center cellspacing=1 cellpadding=2 border=0 class=t>");
// 	Response.Write("<tr><td colspan=5 align=left>");
// 	Response.Write("<b>Barcode : </b><input type=text size=40 name=kw value='" + m_kw + "' onkeydown=\"if(event.key=='Enter' || window.event.keyCode == 13){window.location='?kw='+this.value;return false;}\"  onchange=\"window.location='?kw='+this.value;\">");
// 	Response.Write("<input type=submit name=cmd class=b value='SCAN'>");
// 	Response.Write("</td></tr>");
// 	Response.Write("<tr class=th>");
// 	Response.Write("<th>Code</th>");
// 	Response.Write("<th>SupplierCode</th>");
// 	Response.Write("<th>Description</th>");
// 	Response.Write("<th>Price</th>");
// 	Response.Write("<th>StockLocation</th>");
// 	Response.Write("</tr>");

// 	int nRows = 0;
// 	string sc = " SELECT c.code, c.supplier_code, c.name, c.level_price0 AS price, c.stock_location ";
// 	sc += " FROM code_relations c ";
// 	sc += " WHERE 1 = 1 ";
// 	if (m_kw == "")
// 		sc += " AND 1 = 2 ";
// 	else
// 		sc += " AND (c.barcode = '" + EncodeQuote(m_kw) + "' OR c.code = (SELECT TOP 1 item_code FROM barcode WHERE barcode = '" + EncodeQuote(m_kw) + "')) ";
// 	sc += " ORDER BY c.code ";
// //DEBUG("sc=", sc);	
// 	try
// 	{
// 		myAdapter = new SqlDataAdapter(sc, myConnection);
// 		nRows = myAdapter.Fill(ds, "data");
// 	}
// 	catch(Exception e)
// 	{
// 		ShowExp(sc, e);
// 		return false;
// 	}

// 	if (nRows > 0)
// 	{
// 		DataRow dr = ds.Tables["data"].Rows[0];
// 		string code = dr["code"].ToString();
// 		string supplier_code = dr["supplier_code"].ToString();
// 		string name = dr["name"].ToString();
// 		double dPrice = MyDoubleParse(dr["price"].ToString());
// 		string stock_location = dr["stock_location"].ToString();

// 		Response.Write("<input type=hidden name=code value='" + code + "'><tr>");
// 		Response.Write("<td class=tb align=center>" + code + "&nbsp;&nbsp;&nbsp;</td>");
// 		Response.Write("<td class=tb align=center>" + supplier_code + "&nbsp;&nbsp;&nbsp;</td>");
// 		Response.Write("<td class=tb align=center>" + name + "&nbsp;&nbsp;&nbsp;</td>");
// 		Response.Write("<td class=tb align=center>" + dPrice.ToString("c") + "</td>");
// 		Response.Write("<td class=tb align=center><input size=10 name=stock_location value='" + stock_location + "'></td>");
// 		Response.Write("</tr>");
// 		Response.Write("<tr><td colspan=5 align=right><input type=submit name=cmd class=b value='Save'></td>");
// 		Response.Write("</tr>");
// 		Response.Write("</table></form>");
// //		Resposne.Write("<script>document.f.stock_location.select();</script");
// //		Response.Write(">");
// 	}
// //	else
// 	{
// 		Response.Write("</table></form><script>document.f.kw.select();</script");
// 		Response.Write(">");
// 	}
// 	return true;
// }
// bool DoUpdateData()
// {
// 	string code = p("code");
// 	string stock_location = p("stock_location");
	
// 	if(code == "" || code == "0")
// 	{
// 		ErrMsgAdmin("Invalid Code");
// 		return false;
// 	}
	
// 	string sc = "";
// 	sc += " UPDATE code_relations SET stock_location = N'" + EncodeQuote(stock_location) + "' ";
// 	sc += " WHERE code = " + code;
// 	try
// 	{
// 		myCommand = new SqlCommand(sc);
// 		myCommand.Connection = myConnection;
// 		myCommand.Connection.Open();
// 		myCommand.ExecuteNonQuery();
// 		myCommand.Connection.Close();
// 	}
// 	catch(Exception e) 
// 	{
// 		ShowExp(sc, e);
// 		return false;
// 	}
// 	return true;
// }
// </script>

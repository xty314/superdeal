<!-- #include file="page_index.cs" -->
<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_id = "";
string m_kw = "";
string m_code = "";
string m_sShelfName = "";
string m_area = "";
string m_location = "";
string m_section = "";

void Page_Load(Object Src, EventArgs E ) 
{
    TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
    
	m_type = g("t");
	m_action = g("a");
	m_kw = p("kw");
	if(m_kw == "")
		m_kw = g("kw");
	m_cmd = p("cmd");
	m_id = MyIntParse(g("id")).ToString();
	if(m_id == "0")
		m_id = MyIntParse(p("shelf")).ToString();
	m_code = g("code");
	m_area = g("area");
	m_location = g("location");
	m_section = g("section");

	if(m_type == "j") //ajax
	{
		if(m_action == "get_location")
			DoAjaxResponseGetLocation();
		else if(m_action == "get_section")
			DoAjaxResponseGetSection();
		else if(m_action == "get_level")
			DoAjaxResponseGetLevel();
		else
			DoAjaxResponseCheckCode();
		return;
	}
	
	if(m_cmd == Lang("Record"))
	{
		if(DoTransfer())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Stock transfered, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?t=item&code=" + m_code + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Transfer"))
	{
		if(DoTransferShelf())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Stock transfered, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?t=shelf&id=" + m_id + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Apply"))
	{
		if(DoApplyShelfActions())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Stock action applied, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?code=" + m_code + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Del"))
	{
		if(DoDeleteShelfRecord())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Zero record deleted, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?code=" + m_code + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	
	PrintAdminHeader();
	if(!g_bPDA)
		PrintAdminMenu();
	if(m_type == "item")
	{
		PrintItemSummary();
		PrintAdminFooter();
		return;
	}
	else if(m_type == "shelf")
	{
		PrintShelfSummary();
		PrintAdminFooter();
		return;
	}
	else if(m_type == "trace")
	{
		PrintShelfTrace();
		PrintAdminFooter();
		return;
	}
	PrintShelfTransferForm();
	if(!g_bPDA)
		PrintAdminFooter();
}

string PrintShelfOptions(string current_id, bool bAll)
{
	int nRows = 0;
	if(ds.Tables["shelf"] != null)
		ds.Tables["shelf"].Clear();
	string sc = " SELECT s.id, s.area, s.name ";
	sc += " FROM shelf s ";
	sc += " WHERE 1 = 1 ";
	sc += " ORDER BY s.name ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "shelf");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "";
	if(bAll)
		s += "<option value=''>" + Lang("All") + "</option>";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["shelf"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string area = dr["area"].ToString();
		
		s += "<option value=" + id + "";
		if(id == current_id)
			s += " selected";
		s += ">" + name + "</option>";
	}
	return s;
}

bool GetItemDetails(string kw)
{
	if(kw == "" && m_code == "")
		return false;
	string sc = " SELECT c.supplier_code, c.code, c.name, s.id AS shelf_id, s.name AS shelf_name, si.qty ";
	sc += " FROM code_relations c ";
	sc += " LEFT OUTER JOIN shelf_item si ON si.code = c.code ";
	sc += " LEFT OUTER JOIN shelf s ON s.id = si.shelf_id ";
	sc += " WHERE 1 = 1 ";
	if(kw != "")
		sc += " AND c.supplier_code = '" + EncodeQuote(kw) + "' ";
	else if(m_code != "")
		sc += " AND c.code = " + m_code;
//	if(IsInteger(kw))
//		sc += " OR c.code = " + kw + " ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "item") <= 0)
		{
			Response.Write("!" + Lang("Item not found"));
			return false;
		}
	}
	catch(Exception e)
	{
		Response.Write("!" + e.ToString());
		return false;
	}
	return true;
}

bool PrintShelfTransferForm()
{
	int nRows = 0;
	string item_name = "";
	string mpn = p("supplier_code");
	if(mpn != "" || m_code != "")
	{
		if(!GetItemDetails(mpn))
			return false;
		nRows = ds.Tables["item"].Rows.Count;
		DataRow dr = ds.Tables["item"].Rows[0];
		m_code = dr["code"].ToString();
		item_name = dr["name"].ToString();
		mpn = dr["supplier_code"].ToString();
	}
	
	string sWidth = "50";
	if(g_bPDA)
		sWidth = "10";
	Response.Write("<br><center><h4>" + Lang("Shelf Transfer") + "</h4></center>");
	Response.Write("<form name=f action=?code=" + m_code + " method=post>");
	Response.Write("<input type=hidden name=shelf_id>");
	Response.Write("<input type=hidden name=rows value=" + nRows + ">");
	Response.Write("<table ");
	if(g_bPDA)
		Response.Write(" width=220 ");
	else
		Response.Write(" width=90% ");
	Response.Write("align=center cellspacing=1 cellpadding=1 border=1 class=t>");
	Response.Write("<tr><th align=right>&nbsp;" + Lang("M_PN") + "&nbsp;:&nbsp;</th>");
	Response.Write("<td colspan=3>");
//	Response.Write("<input type=text size=5 name=supplier_code onchange=checkCode() onKeyDown=\"if(event.keyCode==13){checkCode();document.f.shelf_name.focus();}\">");
	Response.Write("<input type=text size=10 name=supplier_code value='" + mpn + "'> <input type=submit class=b name=cmd value='" + Lang("Search") + "'></td></tr>");
//	Response.Write("<input type=button value='" + Lang("Check") + "' class=b onclick=\"if(document.f.code.value != ''){checkCode();}\">");
//	Response.Write("&nbsp;<input type=text size=" + sWidth + " class=f name=item_name value='" + item_name + "' disabled></td></tr>");
	
	if(m_code == "")
	{
		Response.Write("<script language=javascript>document.f.supplier_code.focus();</script");
		Response.Write(">");
		Response.Write("</table></form>");
		return true;
	}
	
	Response.Write("<tr height=64><td colspan=4>" + Lang("Item Description") + " : <b>" + mpn + " " + item_name + "</b></td></tr>");

	Response.Write("<tr>");
	Response.Write("<th colspan=2>" + Lang("Current Locations") + ":</th>");
	Response.Write("<th>" + Lang("Stock Inputs") + ":</th>");
	Response.Write("<th>" + Lang("Stock Transfers") + ":</th>");
	Response.Write("</tr>");
	
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["item"].Rows[i];
		string shelf_id = dr["shelf_id"].ToString();
		string shelf_name = dr["shelf_name"].ToString();
		string qty = dr["qty"].ToString();
//		if(MyDoubleParse(qty) == 0)
//			continue;
		
		Response.Write("<tr>");
		Response.Write("<td><input type=hidden name=shelf_id" + i + " value=" + shelf_id + ">");
		Response.Write("<a href=?t=shelf&id=" + shelf_id + " class=o target=_blank title='" + Lang("Click to see shelf summary") + "'>" + shelf_name + "</a></td>");
		Response.Write("<td><a href=?t=trace&code=" + m_code + " class=o target=_blank title='" + Lang("Click to trace item stock") + "'>" + qty + "</a></td>");
		Response.Write("<td nowrap>IN:<input type=text name=qty_in" + i + " size=3> OUT:<input type=text name=qty_out" + i + " size=3>");
		Response.Write("<input type=submit class=b name=cmd value='" + Lang("Apply") + "'>");
		if(MyDoubleParse(qty) == 0)
		{
			Response.Write("<input type=submit class=b name=cmd value='" + Lang("Del") + "' onclick=\"document.f.shelf_id.value=" + shelf_id + ";");
			Response.Write(" if(!window.confirm('Are you sure to delete?')){return false;}\">");
		}
		Response.Write("</td><td nowrap>" + Lang("Transfer") + "(Qty):<input type=text name=qty_transfer" + i + " size=3> " + Lang("to Location") + " ");
		Response.Write("<input type=text name=shelf_name_to" + i + " size=5> ");
		Response.Write("<input type=submit class=b name=cmd value='" + Lang("Apply") + "'></td>");
		Response.Write("</tr>");
	}
	
	Response.Write("<tr><th colspan=2 nowrap>&nbsp;" + Lang("Add into new Location") + "&nbsp;:&nbsp;</th><td colspan=2>");
	Response.Write("Qty:<input type=text name=qty_in size=3>");
	Response.Write(" " + Lang("Shelf Name") + ":<input type=text size=3 name=shelf_name value='" + m_sShelfName + "'>");
	Response.Write(" " + Lang("Area") + ":<select name=area onchange=\"getLocation();\"><option value=''></option>" + PrintShelfAreaOptions(m_area, false) + "</select>");
	Response.Write(" " + Lang("Location") + ":<select name=location onchange=\"getSection();\">");
	Response.Write("<option value=''></option>" + PrintShelfLocationOptions(m_area, m_location, false) + "</select>");
//	Response.Write(" " + Lang("Section") + ":<select name=section onchange=\"window.location='?section=' + escape(this.options[this.selectedIndex].value);\">");
	Response.Write(" " + Lang("Section") + ":<select name=section id=section onchange=\"getLevel();\">");
	Response.Write("<option value=''></option>" + PrintShelfSectionOptions(m_area, m_location, m_section, false) + "</select>");
	Response.Write(" " + Lang("Level") + ":<select name=level id=level>" + PrintShelfLevelOptionsBySection(m_area, m_location, m_section, "", false) + "</select>");
	Response.Write("<input type=submit class=b name=cmd value='" + Lang("Apply") + "'>");
	Response.Write("</td></tr>");
//	Response.Write("<tr><td align=right>&nbsp;" + Lang("Stock") + " <font color=green><b>" + Lang("IN") + "</b></font>&nbsp;:&nbsp;</td><td><input type=text size=10 name=qty_in onclick=select()></td></tr>");
//	Response.Write("<tr><td align=right>&nbsp;" + Lang("Stock") + " <font color=red><b>" + Lang("OUT") + "</b></font>&nbsp;:&nbsp;</td><td><input type=text size=10 name=qty_out onclick=select()></td></tr>");
//	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd class=b value='" + Lang("Record") + "' ");
//	Response.Write(" onclick=\"if(document.f.qty.value==''){document.f.qty.focus();return false;}\"></td></tr>");
	Response.Write("</table></form>");
	Response.Write("<script language=javascript>document.f.code.focus();</script");
	Response.Write(">");
	Response.Write(PrintJavaScript());
	
	Response.Write("<br><br><br><br><br>");
	return true;
}

bool DoDeleteShelfRecord()
{
	string code = m_code;
	string shelf_id = p("shelf_id");
	if(shelf_id == "")
	{
		ErrMsgAdmin("Invalid shelf id");
		return false;
	}
	if(code == "")
	{
		ErrMsgAdmin("Invalid item code");
		return false;
	}
	string sc = " DELETE FROM shelf_item WHERE shelf_id = " + shelf_id + " AND code = " + code + " AND qty = 0 ";
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

bool DoApplyShelfActions()
{
	string code = m_code;
	if(code == "")
	{
		ErrMsgAdmin("Invalid item code");
		return false;
	}
	int nRows = MyIntParse(p("rows"));
	if(nRows <= 0)
	{
		ErrMsgAdmin("nothing to do");
		return false;
	}
	string sc = "";
	for(int i=0; i<nRows; i++)
	{
		string shelf_id = p("shelf_id" + i);
		if(shelf_id == "")
			continue;
		double dQtyIn = MyMoneyParse(p("qty_in" + i));
		double dQtyOut = MyMoneyParse(p("qty_out" + i));
		double dQty = dQtyIn - dQtyOut;
		if(dQty != 0)
		{
			sc = " BEGIN TRANSACTION ";
			sc += " IF NOT EXISTS(SELECT id FROM shelf_item WHERE shelf_id = " + shelf_id + " AND code = " + code + ") ";
			sc += " INSERT INTO shelf_item (shelf_id, code, qty) VALUES(" + shelf_id + ", " + code + ", " + dQty.ToString() + ") ";
			sc += " ELSE ";
			sc += " UPDATE shelf_item SET qty = qty + " + dQty.ToString() + " WHERE shelf_id = " + shelf_id + " AND code = " + code + " ";
			sc += " INSERT INTO shelf_log (shelf_id, code, qty, card_id) VALUES(" + shelf_id + ", " + code + ", " + dQty.ToString() + ", " + Session["card_id"].ToString() + ") ";
			sc += " COMMIT ";
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
		
		dQty = MyMoneyParse(p("qty_transfer" + i));
		if(dQty == 0)
			continue;
		string shelf_name_to = p("shelf_name_to" + i);
		if(shelf_name_to == "")
			continue;
		string shelf_id_to = GetShelfIdByName(shelf_name_to);
		if(shelf_id_to == "")
			continue;
		
		sc = " BEGIN TRANSACTION ";
		sc += " IF NOT EXISTS(SELECT id FROM shelf_item WHERE shelf_id = " + shelf_id_to + " AND code = " + code + ") ";
		sc += " INSERT INTO shelf_item (shelf_id, code, qty) VALUES(" + shelf_id_to + ", " + code + ", " + dQty.ToString() + ") ";
		sc += " ELSE ";
		sc += " UPDATE shelf_item SET qty = qty + " + dQty.ToString() + " WHERE shelf_id = " + shelf_id_to + " AND code = " + code + " ";
		sc += " INSERT INTO shelf_log (shelf_id, code, qty, card_id) VALUES(" + shelf_id_to + ", " + code + ", " + dQty.ToString() + ", " + Session["card_id"].ToString() + ") ";
		sc += " UPDATE shelf_item SET qty = qty - " + dQty.ToString() + " WHERE shelf_id = " + shelf_id + " AND code = " + code;
		sc += " INSERT INTO shelf_log (shelf_id, code, qty, card_id) VALUES(" + shelf_id + ", " + code + ", 0 - " + dQty.ToString() + ", " + Session["card_id"].ToString() + ") ";
		sc += " COMMIT ";
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
	double dQtyNew = MyMoneyParse(p("qty_in"));
	if(dQtyNew != 0)
	{
		if(!DoTransfer())
			return false;
	}
	return true;
}

string GetShelfIdByName(string shelf_name)
{
	if(ds.Tables["shelf_name"] != null)
		ds.Tables["shelf_name"].Clear();
	string sc = " SELECT id FROM shelf WHERE name LIKE '" + EncodeQuote(shelf_name) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "shelf_name") <= 0)
		{
			return "";
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	string shelf_id = ds.Tables["shelf_name"].Rows[0]["id"].ToString();
	return shelf_id;
}

bool DoTransfer()
{
	string sc = "";
	string shelf_id = "";
	string area = p("area");
	string section = p("section");
	string level = p("level");
	string shelf_name = p("shelf_name");
	if(shelf_name != "")
	{
		sc = " SELECT id FROM shelf WHERE name LIKE '" + EncodeQuote(shelf_name) + "' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "shelf_name") <= 0)
			{
				ErrMsgAdmin("shelf " + section + level + " not found");
				return false;
			}
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
		shelf_id = ds.Tables["shelf_name"].Rows[0]["id"].ToString();
	}
	if(shelf_id == "")
	{
		sc = " SELECT TOP 1 id FROM shelf WHERE area = N'" + EncodeQuote(area) + "' ";
		sc += " AND section = N'" + EncodeQuote(section) + "' ";
		sc += " AND level = " + MyIntParse(level) + " ORDER BY id ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "checkshelf") <= 0)
			{
				ErrMsgAdmin("shelf " + section + level + " not found");
				return false;
			}
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
		shelf_id = ds.Tables["checkshelf"].Rows[0]["id"].ToString();
	}
	
	double dQtyIn = MyMoneyParse(p("qty_in"));
	double dQtyOut = MyMoneyParse(p("qty_out"));
	double dQty = dQtyIn - dQtyOut;
	if(dQty == 0)
		return true;
	string code = p("code");
	if(code == "")
		code = m_code;
	if(code == "" || !IsInteger(code))
	{
		ErrMsgAdmin("Invalid Code, must be integer");
		return false;
	}
	m_code = code;
	sc = " SELECT code, name FROM code_relations WHERE code = " + code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "checkcode") <= 0)
		{
			ErrMsgAdmin("item code:" + code + " not found");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	sc = " BEGIN TRANSACTION ";
	sc += " IF NOT EXISTS(SELECT id FROM shelf_item WHERE shelf_id = " + shelf_id + " AND code = " + code + ") ";
	sc += " INSERT INTO shelf_item (shelf_id, code, qty) VALUES(" + shelf_id + ", " + code + ", " + dQty.ToString() + ") ";
	sc += " ELSE ";
	sc += " UPDATE shelf_item SET qty = qty + " + dQty.ToString() + " WHERE shelf_id = " + shelf_id + " AND code = " + code + " ";
	sc += " INSERT INTO shelf_log (shelf_id, code, qty, card_id) VALUES(" + shelf_id + ", " + code + ", " + dQty.ToString() + ", " + Session["card_id"].ToString() + ") ";
	sc += " COMMIT ";
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
bool PrintAreaOptionsTo(string sCurrent)
{
	DataSet dsArea = new DataSet();
	int nRows = 0;

	string sc = " SELECT DISTINCT area FROM shelf WHERE 1 = 1 ";
	sc += " ORDER BY area ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand.Fill(dsArea, "area");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<nRows; i++)
	{
		string area = dsArea.Tables["area"].Rows[i]["area"].ToString();
		Response.Write("<option value='" + area + "' ");
		if(area == sCurrent)
			Response.Write("selected");
		Response.Write(">" + area + "</option>");
	}
	return true;
}
bool DoTransferShelf()
{
	string area = p("area");
	string location = p("location");
	string section = p("section");
	string level = p("level");
	string area_to = p("area_to");
	string location_to = p("location_to");
	string section_to = p("section_to");
	string level_to = p("level_to");
	string sc = " SELECT s.id AS shelf_id, si.id AS shelf_item_id, si.code, si.qty ";
	sc += " FROM shelf_item si ";
	sc += " JOIN shelf s ON s.id = si.shelf_id ";
	sc += " WHERE 1 = 1 ";
	sc += " AND s.area = N'" + EncodeQuote(area) + "' ";
	sc += " AND s.location = N'" + EncodeQuote(location) + "' ";
	sc += " AND s.section = N'" + EncodeQuote(section) + "' ";
	sc += " AND s.level = " + MyIntParse(level) + " ";
	sc += " ORDER BY si.id ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "shelf_from") <= 0)
		{
			ErrMsgAdmin("no items on shelf " + section + level + "");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	sc = " SELECT s.id AS shelf_id ";
	sc += " FROM shelf s ";
	sc += " WHERE s.area = N'" + EncodeQuote(area) + "' ";
	sc += " WHERE s.location = N'" + EncodeQuote(location) + "' ";
	sc += " AND s.section = N'" + EncodeQuote(section_to) + "' ";
	sc += " AND s.level = " + MyIntParse(level_to) + " ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "shelf_to") <= 0)
		{
			ErrMsgAdmin("shelf not found : " + section + level + " ");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	string shelf_id_from = ds.Tables["shelf_from"].Rows[0]["shelf_id"].ToString();
	string shelf_id_to = ds.Tables["shelf_to"].Rows[0]["shelf_id"].ToString();
	
	sc = " BEGIN TRANSACTION ";
	for(int i=0; i<ds.Tables["shelf_from"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["shelf_from"].Rows[i];
		string shelf_item_id = dr["shelf_item_id"].ToString();
		string code = dr["code"].ToString();
		double dQty = MyDoubleParse(dr["qty"].ToString());
	
		sc += " IF NOT EXISTS(SELECT id FROM shelf_item WHERE shelf_id = " + shelf_id_to + " AND code = " + code + ") ";
		sc += " INSERT INTO shelf_item (shelf_id, code, qty) VALUES(" + shelf_id_to + ", " + code + ", " + dQty.ToString() + ") ";
		sc += " ELSE ";
		sc += " UPDATE shelf_item SET qty = qty + " + dQty.ToString() + " WHERE shelf_id = " + shelf_id_to + " AND code = " + code + " ";
		sc += " INSERT INTO shelf_log (shelf_id, code, qty, card_id) VALUES(" + shelf_id_to + ", " + code + ", " + dQty.ToString() + ", " + Session["card_id"].ToString() + ") ";
		sc += " DELETE FROM shelf_item WHERE id = " + shelf_item_id;
		sc += " INSERT INTO shelf_log (shelf_id, code, qty, card_id) VALUES(" + shelf_id_from + ", " + code + ", 0 - " + dQty.ToString() + ", " + Session["card_id"].ToString() + ") ";
	}
	sc += " COMMIT ";
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
	m_id = shelf_id_to;
	return true;
}

bool PrintItemSummary()
{
	string sc = "";
	string item_name = Lang("All Item");
	string supplier_code = "";
	string code = m_code;
	int nCols = 5;
	if(code != "" && IsInteger(code))
	{
		sc = " SELECT c.name, c.supplier_code ";
		sc += " FROM code_relations c ";
		sc += " WHERE c.code = " + code + " ";
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			if(myCommand1.Fill(ds, "item") > 0)
			{
				DataRow dr = ds.Tables["item"].Rows[0];
				item_name = dr["name"].ToString();
				supplier_code = dr["supplier_code"].ToString();
			}
			else
			{
				ErrMsgAdmin(Lang("code not found"));
				return false;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		nCols = 3;
	}
	
	Response.Write("<br><center><h4>" + Lang("Shelf Storage") + "</h4></center>");
	Response.Write("<script language=javascript src=../cssjs/calendar30en.js></script");
	Response.Write("><form name=f action=?t=item method=post>");
	Response.Write("<table width=80% align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr><td>" + Lang("Item") + "&nbsp;:&nbsp;#" + code + " &nbsp; " + Lang("Description") + "&nbsp;:&nbsp;" + item_name);
	if(code != "" && code != "0")
		Response.Write(" &nbsp; <input type=button class=b value='" + Lang("Trace") + "' onclick=\"window.location='?t=trace&code=" + code + "';\">");
	Response.Write("</td>");
	Response.Write("<td colspan=" + nCols.ToString() + " align=right>");
	Response.Write("<input type=text name=kw value='" + m_kw + "'>");
	Response.Write("<input type=submit class=b value='" + Lang("Search") + "'>");
	if(m_kw != "")
		Response.Write("<input type=button value='" + Lang("Show All") + "' class=b onclick=\"window.location='?t=item';\">");
	Response.Write("</td></tr>");
	
	Response.Write("<tr class=th>");
	Response.Write("<th>" + Lang("Area") + "</th>");
	Response.Write("<th>" + Lang("Shelf") + "</th>");
	if(code == "" || code == "0")
	{
		Response.Write("<th>" + Lang("M_PN") + "</th>");
		Response.Write("<th>" + Lang("Description") + "</th>");
	}
	Response.Write("<th>" + Lang("Quantity") + "</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");
	
	int nRows = 0;
	sc = " SELECT i.shelf_id, s.name AS shelf_name, s.area, c.name AS item_name, i.code, i.qty, c.supplier_code ";
	sc += " FROM shelf s ";
	sc += " JOIN shelf_item i ON i.shelf_id = s.id ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = i.code ";
	sc += " WHERE 1 = 1 ";
//	sc += " AND i.qty <> 0 ";
	if(code != "" && code != "0")
		sc += " AND i.code = " + code;
	if(m_kw != "")
	{
		sc += " AND (c.name LIKE N'%" + EncodeQuote(m_kw) + "%' OR c.supplier_code LIKE N'%" + EncodeQuote(m_kw) + "%' )";
	}
	sc += " ORDER BY s.name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "data");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	double dTotalQty = 0;
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		string shelf_name = dr["shelf_name"].ToString();
		item_name = dr["item_name"].ToString();
		supplier_code = dr["supplier_code"].ToString();
		string item_code = dr["code"].ToString();
		string shelf_id = dr["shelf_id"].ToString();
		double dQty = MyDoubleParse(dr["qty"].ToString());
		dTotalQty += dQty;
		Response.Write("<tr class=tn>");
		Response.Write("<td class=tb align=center><a href=?t=shelf&id=" + shelf_id + " class=o>" + shelf_name + "</a></td>");
		if(code == "" || code == "0")
		{
			Response.Write("<td class=tb>" + supplier_code + "</th>");
			Response.Write("<td class=tb align=left><a href=?t=item&code=" + item_code + " class=o>" + item_name + "</a></th>");
		}
		Response.Write("<td class=tb align=right>" + dQty.ToString() + "</td>");
		Response.Write("<td class=tb align=right>");
		Response.Write("<input type=button class=b value='" + Lang("Trace") + "' onclick=\"window.location='?t=trace&id=" + shelf_id + "';\">");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><th colspan=" + nCols.ToString() + " align=right>" + Lang("Total") + "&nbsp;:&nbsp;" + dTotalQty.ToString() + "</th><th>&nbsp;</th></tr>");
	Response.Write("</table></form>");
	return false;
}

bool PrintShelfSummary()
{
	string sc = "";
	string area = "";
	string location = p("location");
	string section = p("section");
	string level = p("level");
	int nShelfRows = 0;
	string shelf_name = p("shelf_name");
	if(shelf_name != "")
	{
		sc = " SELECT id FROM shelf WHERE name LIKE '" + EncodeQuote(shelf_name) + "' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "shelf_name") <= 0)
			{
				ErrMsgAdmin("shelf " + section + level + " not found");
				return false;
			}
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
		m_id = ds.Tables["shelf_name"].Rows[0]["id"].ToString();
	}
	if( (m_id != "" && m_id != "0") || (section != "" && level != "") )
	{
		sc = " SELECT s.id, s.name AS shelf_name, s.area, s.location, s.section, s.level ";
		sc += " FROM shelf s ";
		sc += " WHERE 1 = 1 ";
		if(m_id != "" && m_id != "0")
			sc += " AND s.id = " + m_id + " ";
		if(area != "")
			sc += " AND s.area = N'" + EncodeQuote(area) + "' ";
		if(location != "")
			sc += " AND s.location = N'" + EncodeQuote(location) + "' ";
		if(section != "")
			sc += " AND s.section = N'" + EncodeQuote(section) + "' ";
		if(level != "")
			sc += " AND s.level = " + MyIntParse(level) + " ";
//DEBUG("sc=", sc);		
		try
		{
			SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
			nShelfRows = myCommand1.Fill(ds, "shelf");
			if(nShelfRows <= 0)
			{
				ErrMsgAdmin(Lang("Shelf #" + m_id + " not found"));
				return false;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	if(nShelfRows == 1)
	{
		DataRow dr = ds.Tables["shelf"].Rows[0];
		shelf_name = dr["shelf_name"].ToString();
		area = dr["area"].ToString();
		location = dr["location"].ToString();
		section = dr["section"].ToString();
		level = dr["level"].ToString();
		m_id = dr["id"].ToString();
	}

	Response.Write("<br><center><h4>" + Lang("Shelf Storage") + "</h4></center>");
	Response.Write("<script language=javascript src=../cssjs/calendar30en.js></script");
	Response.Write("><form name=f action=?t=shelf method=post>");
	Response.Write("<table width=90% align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr><td colspan=1>" + Lang("Shelf") + "&nbsp;:&nbsp;" + shelf_name + "</td>");
	Response.Write("<td colspan=5 align=right nowrap>");
//	Response.Write("&nbsp;" + Lang("Shelf") + "&nbsp;&nbsp;");
	Response.Write(" " + Lang("Shelf") + ":<input type=text size=3 name=shelf_name value='" + m_sShelfName + "'><input type=submit name=cmd class=b value=GO>");
	Response.Write("&nbsp;" + Lang("Area") + ":<select name=area onchange=\"getLocation();\"><option value=''></option>" + PrintShelfAreaOptions(area, false) + "</select>");
	Response.Write("&nbsp;" + Lang("Location") + ":<select name=location onchange=\"getSection();\">");
	Response.Write("<option value=''></option>" + PrintShelfLocationOptions(area, location, false) + "</select>");
	Response.Write("&nbsp;" + Lang("Section") + ":<select name=section id=section onchange=\"getLevel();\">");
	Response.Write("<option value=''></option>" + PrintShelfSectionOptions(area, location, section, false) + "</select>");
	Response.Write(" " + Lang("Level") + ":<select name=level id=level>" + PrintShelfLevelOptionsBySection(area, location, section, level, false) + "</select>");
	Response.Write(Lang("Item") + "&nbsp;:&nbsp;<input type=text name=kw value='" + m_kw + "'>");
	Response.Write("<input type=submit class=b value='" + Lang("Search") + "'>");
	if(m_kw != "")
		Response.Write("<input type=button value='" + Lang("Show All") + "' class=b onclick=\"window.location='?t=shelf';\">");
	Response.Write("</td></tr>");
	
	if(nShelfRows == 1)
	{
		Response.Write("<tr><td colspan=6>" + Lang("Transfer whole Shelf to") + "&nbsp;:&nbsp;");
//		Response.Write("<td colspan=4 align=left>");
		Response.Write("&nbsp;" + Lang("Area") + ":<select name=area_to><option value=''></option>" + PrintShelfAreaOptions("", false) + "</select>");
		Response.Write("&nbsp;" + Lang("Location") + ":<select name=location_to onchange=\"getLocationTo();\">" + PrintShelfLocationOptions(area, "", false) + "</select>");
		Response.Write("&nbsp;" + Lang("Section") + ":<select name=section_to onchange=\"getLevelTo();\">");
		Response.Write("<option value=''></option>" + PrintShelfSectionOptions(area, location, "", false) + "</select>");
		Response.Write(" " + Lang("Level") + ":<select name=level_to id=level_to>" + PrintShelfLevelOptionsBySection(area, location, section, "", false) + "</select>");
		Response.Write("<input type=submit name=cmd class=b value='" + Lang("Transfer") + "' onclick=\"");
		Response.Write(" if(document.f.section_to.value==''){window.alert('" + Lang("Please select section") + "');document.f.section_to.focus();return false;}");
		Response.Write(" else if(document.f.level_to.value==''){window.alert('" + Lang("Please select level") + "');document.f.level_to.focus();return false;}");
		Response.Write("\"></td></tr>");
	}
	
	Response.Write("<tr class=th>");
	Response.Write("<th>" + Lang("Barcode") + "</th>");
	Response.Write("<th>" + Lang("Shelf") + "</th>");
	Response.Write("<th>" + Lang("M_PN") + "</th>");
	Response.Write("<th>" + Lang("Description") + "</th>");
	Response.Write("<th>" + Lang("Quantity") + "</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");
	
	int nRows = 0;
	sc = " SELECT s.area, s.name AS shelf_name, c.name AS item_name, i.shelf_id, i.code, i.qty, c.supplier_code, c.barcode ";
	sc += " FROM shelf s ";
	sc += " JOIN shelf_item i ON i.shelf_id = s.id ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = i.code ";
	sc += " WHERE 1 = 1 ";
//	sc += " AND i.qty <> 0 ";
	if(m_id != "" && m_id != "0")
		sc += " AND s.id = " + m_id;
	if(m_kw != "")
	{
		sc += " AND (c.name LIKE N'%" + EncodeQuote(m_kw) + "%' OR c.supplier_code LIKE N'%" + EncodeQuote(m_kw) + "%' OR c.barcode LIKE N'%" + EncodeQuote(m_kw) + "%') ";
	}
	sc += " ORDER BY s.name, i.code ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "data");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	double dTotalQty = 0;

	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(g("p"));
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = MyIntParse(g("spb"));
	m_cPI.TotalRows = nRows;
	m_cPI.URI = "?t=shelf&r=" + DateTime.Now.ToOADate();
	if(m_kw != null)
		m_cPI.URI += "&kw=" + HttpUtility.UrlEncode(m_kw);	
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	
	for(; i<end && i<nRows; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		shelf_name = dr["shelf_name"].ToString();
		string shelf_id = dr["shelf_id"].ToString();
		string item_name = dr["item_name"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		double dQty = MyDoubleParse(dr["qty"].ToString());
		dTotalQty += dQty;
		Response.Write("<tr class=tn>");
		Response.Write("<td class=tb align=center>" + barcode + "</td>");
		Response.Write("<td class=tb align=center><a href=?t=shelf&id=" + shelf_id + " class=o>" + shelf_name + "</a></td>");
		Response.Write("<td class=tb align=center>" + supplier_code + "</td>");
		
		Response.Write("<td class=tb align=left><a href=?t=item&code=" + code + " class=o>" + item_name + "</a></td>");
		Response.Write("<td class=tb align=right>" + dQty.ToString() + "</td>");
		Response.Write("<td class=tb align=right>");
		Response.Write("<input type=button class=b value='" + Lang("Trace") + "' title='" + Lang("Trace Item") + "' onclick=\"window.location='?t=trace&code=" + code + "';\">");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><th colspan=5 align=right>" + Lang("Total") + "&nbsp;:&nbsp;" + dTotalQty.ToString() + "</th><th>&nbsp;</th></tr>");
	if(nRows > m_cPI.PageSize)
		Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");	
	Response.Write("</table></form>");
	Response.Write(PrintJavaScript());
	return false;
}

bool PrintShelfTrace()
{
	string sc = "";
	string shelf_name = "All Shelf";
	string area = p("area");
	string location = p("location");
	string section = p("section");
	string level = p("level");
	sc = " SELECT s.id AS shelf_id, s.area, s.location, s.section, s.level, s.name AS shelf_name ";
	sc += " FROM shelf s ";
	sc += " WHERE 1 = 1 ";
	if(m_id != "0" && m_id != "")
		sc += " AND s.id = " + m_id + " ";
	if(area != "")
		sc += " AND s.area = N'" + EncodeQuote(area) + "' ";
	if(location != "")
		sc += " AND s.location = N'" + EncodeQuote(location) + "' ";
	if(section != "")
		sc += " AND s.section = N'" + EncodeQuote(section) + "' ";
	if(level != "")
		sc += " AND s.level = " + MyIntParse(level) + " ";
//DEBUG("sc=", sc);			
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "shelf") > 0)
		{
			DataRow dr = ds.Tables["shelf"].Rows[0];
			shelf_name = dr["shelf_name"].ToString();
			area = dr["area"].ToString();
			location = dr["location"].ToString();
			section = dr["section"].ToString();
			level = dr["level"].ToString();
			m_id = dr["shelf_id"].ToString();
		}
		else
		{
			ErrMsgAdmin(Lang("Shelf #" + m_id + " | " + section + level + " not found"));
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int nRows = 0;
	sc = " SELECT card.name AS staff_name, c.name AS item_name, i.code, L.qty, c.supplier_code ";
	sc += ", s.area, s.name AS shelf_name, i.shelf_id ";
	sc += ", CONVERT(varchar(100), L.log_time, 103) AS sdate ";
	sc += " FROM shelf_log L ";
	sc += " JOIN shelf_item i ON i.code = L.code ";
	sc += " JOIN shelf s ON s.id = i.shelf_id ";
	sc += " LEFT OUTER JOIN code_relations c ON c.code = i.code ";
	sc += " LEFT OUTER JOIN card ON card.id = L.card_id ";
	sc += " WHERE 1 = 1 ";
	if(m_id != "" && m_id != "0")
		sc += " AND s.id = " + m_id;
	if(m_code != "" && m_code != "0")
		sc += " AND L.code = " + m_code;
	if(m_kw != "")
	{
		sc += " AND (c.supplier_code LIKE N%'" + EncodeQuote(m_kw) + "%' OR c.name LIKE N'%" + EncodeQuote(m_kw) + "%') ";
	}
	sc += " ORDER BY L.log_time DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "data");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<br><center><h4>" + Lang("Shelf Trace") + "</h4></center>");
	Response.Write("<script language=javascript src=../cssjs/calendar30en.js></script");
	Response.Write("><form name=f action=?t=trace method=post>");
	Response.Write("<table width=80% align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	
	Response.Write("<tr><td colspan=3>" + Lang("Shelf") + "&nbsp;:&nbsp;" + shelf_name + "</td>");
	Response.Write("<td colspan=4 align=right>");
	Response.Write("&nbsp; " + Lang("Area") + ":<select name=area onchange=\"getLocation();\">");
	Response.Write("<option value=''></option>" + PrintShelfAreaOptions(area, false) + "</select>");
	Response.Write("&nbsp;" + Lang("Location") + ":<select name=location onchange=\"getSection();\">");
	Response.Write("<option value=''></option>" + PrintShelfLocationOptions(area, location, false) + "</select>");
	Response.Write("&nbsp;" + Lang("Section") + ":<select name=section id=section onchange=\"getLevel();\">");
	Response.Write("<option value=''></option>" + PrintShelfSectionOptions(area, location, section, false) + "</select>");
	Response.Write(" " + Lang("Level") + ":<select name=level id=level>" + PrintShelfLevelOptionsBySection(area, location, section, level, false) + "</select>");
	Response.Write(Lang("Item") + "&nbsp;:&nbsp;<input type=text name=kw value='" + m_kw + "'>");
	Response.Write("<input type=submit class=b value='" + Lang("Search") + "'>");
	if(m_kw != "")
		Response.Write("<input type=button value='" + Lang("Show All") + "' class=b onclick=\"window.location='?t=trace';\">");
	Response.Write("</td></tr>");
	
	Response.Write("<tr class=th>");
	Response.Write("<th>" + Lang("Date") + "</th>");
	Response.Write("<th>" + Lang("Staff") + "</th>");
	Response.Write("<th>" + Lang("Shelf") + "</th>");
	Response.Write("<th>" + Lang("Code") + "</th>");
	Response.Write("<th>" + Lang("Description") + "</th>");
	Response.Write("<th>" + Lang("Quantity") + "</th>");
	Response.Write("</tr>");
	
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(g("p"));
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = MyIntParse(g("spb"));
	m_cPI.TotalRows = nRows;
	m_cPI.URI = "?t=trace&id=" + m_id + "&r=" + DateTime.Now.ToOADate();
	if(m_kw != null)
		m_cPI.URI += "&kw=" + HttpUtility.UrlEncode(m_kw);	
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	
	for(; i<end && i<nRows; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		string sdate = dr["sdate"].ToString();
		string staff = dr["staff_name"].ToString();
		string shelf = dr["shelf_name"].ToString();
		string shelf_id = dr["shelf_id"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string item_name = dr["item_name"].ToString();
		double dQty = MyDoubleParse(dr["qty"].ToString());
		
		Response.Write("<tr class=tn>");
		Response.Write("<td class=tb align=center>" + sdate + "</td>");
		Response.Write("<td class=tb align=center>" + staff + "</td>");
		Response.Write("<td class=tb align=center><a href=?t=shelf&id=" + shelf_id + " class=o>" + shelf + "</a></td>");
		Response.Write("<td class=tb align=center><a href=?t=item&code=" + code + " class=o>" + supplier_code + "</a></td>");
		Response.Write("<td class=tb><a href=?t=item&code=" + code + " class=o>" + item_name + "</a></td>");
		Response.Write("<td class=tb align=right>" + dQty + "</td>");
		Response.Write("</tr>");
	}

	if(nRows > m_cPI.PageSize)
		Response.Write("<tr><td colspan=6>" + sPageIndex + "</td></tr>");	
	Response.Write("</table></form>");
	Response.Write(PrintJavaScript());
	return true;
}

string PrintJavaScript()
{
	string s = @"
<script language=javascript>
var moz = (typeof document.implementation != 'undefined') && (typeof document.implementation.createDocument != 'undefined');
var ie = (typeof window.ActiveXObject != 'undefined');
var xmlHttp;
function createXMLHttpRequest()
{
	var x;
	if(window.ActiveXObject)
		x = new ActiveXObject('Microsoft.XMLHTTP');
	else if(window.XMLHttpRequest)
		x = new XMLHttpRequest();
    if(!x)
	{
		alert('Giving up :( Cannot create an XMLHTTP instance');
		return null;
	}
	return x;
}
function checkCode()
{
	var code = document.f.code.value;
	if(code == 'NaN' || code == '')
		return;
	xmlHttp = createXMLHttpRequest();
	if(!xmlHttp)
		return;
	var formdata = 'code=' + code;
";
s += "	var url = '?t=j';\r\n";
s += @"
	xmlHttp.onreadystatechange = handleCheckCode;
	xmlHttp.open('POST', url, true);
	xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
	xmlHttp.send(formdata);
	document.f.name.value = '';
}
function handleCheckCode()
{
	if(xmlHttp.readyState == 4)
	{
		if(xmlHttp.status == 200)
		{
			var ret = xmlHttp.responseText;
			if(ret.substr(0, 1) == '!')
			{
				window.alert(ret);
				return;
			}
			document.f.item_name.value = ret;
		} 
		else
		{
			alert('There was a problem with the request.');
		}
	}
}
function getLevel()
{
	var section = document.f.section.value;
	if(section == 'NaN')
		return;
	if(section == '')
	{
		clearLevelOptions();
		return;
	}
	xmlHttp = createXMLHttpRequest();
	if(!xmlHttp)
		return;
	var area = document.f.area.value;
	var location = document.f.location.value;
	var formdata = 'area=' + area + '&location=' + location + '&section=' + section;
";
s += "	var url = '?t=j&a=get_level';\r\n";
s += @"
	xmlHttp.onreadystatechange = handleGetLevel;
	xmlHttp.open('POST', url, true);
	xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
	xmlHttp.send(formdata);
	document.f.name.value = '';
}
function handleGetLevel()
{
	if(xmlHttp.readyState == 4)
	{
		if(xmlHttp.status == 200)
		{
			var ret = xmlHttp.responseText;
			if(ret.substr(0, 1) == '!')
			{
				window.alert(ret);
				return;
			}
			resetLevel(ret);
		} 
		else
		{
			alert('There was a problem with the request.');
		}
	}
}
function resetLevel(txt)
{
	if(txt == '')
		return;
	var doc = null;
	if(moz)
	{
		var parser = new DOMParser();
		doc = parser.parseFromString(txt, 'text/xml');
	}
	else if(ie)
	{
		doc = new ActiveXObject('Microsoft.XMLDOM');
		doc.async = false;
		doc.loadXML(txt);
	}
	if(!doc)
		return;
	var obj = document.getElementById('level');
	if(obj == null)
		return;
	clearLevelOptions();
	var root_doc = doc.getElementsByTagName('level');
	for(var i=0; i<root_doc.length; i++)
	{
		var t = root_doc[i].childNodes[0].nodeValue;
		var textNode = document.createTextNode(t);
		var no = document.createElement('option');
		no.value = t;
		no.appendChild(textNode);
		obj.appendChild(no);
	}
}
function clearLevelOptions()
{
	var obj = document.getElementById('level');
	if(obj == null)
		return;
	var n = obj.childNodes.length;
	for(var m=0; m<n; m++)
		obj.removeChild(obj.childNodes[0]);
}
function getLevelTo()
{
	var area = document.f.area.value;
	if(area == 'NaN')
		return;
	var location = document.f.location.value;
	if(location == 'NaN')
		return;
	var section = document.f.section_to.value;
	if(section == 'NaN')
		return;
	if(section == '')
	{
		clearLevelOptionsTo();
		return;
	}
	xmlHttp = createXMLHttpRequest();
	if(!xmlHttp)
		return;
	var formdata = 'area=' + area + '&location=' + location + '&section=' + section;
";
s += "	var url = '?t=j&a=get_level';\r\n";
s += @"
	xmlHttp.onreadystatechange = handleGetLevelTo;
	xmlHttp.open('POST', url, true);
	xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
	xmlHttp.send(formdata);
	document.f.name.value = '';
}
function handleGetLevelTo()
{
	if(xmlHttp.readyState == 4)
	{
		if(xmlHttp.status == 200)
		{
			var ret = xmlHttp.responseText;
			if(ret.substr(0, 1) == '!')
			{
				window.alert(ret);
				return;
			}
			resetLevelTo(ret);
		} 
		else
		{
			alert('There was a problem with the request.');
		}
	}
}
function resetLevelTo(txt)
{
	if(txt == '')
		return;
	var doc = null;
	if(moz)
	{
		var parser = new DOMParser();
		doc = parser.parseFromString(txt, 'text/xml');
	}
	else if(ie)
	{
		doc = new ActiveXObject('Microsoft.XMLDOM');
		doc.async = false;
		doc.loadXML(txt);
	}
	if(!doc)
		return;
	var obj = document.getElementById('level_to');
	if(obj == null)
		return;
	clearLevelOptionsTo();
	var root_doc = doc.getElementsByTagName('level');
	for(var i=0; i<root_doc.length; i++)
	{
		var t = root_doc[i].childNodes[0].nodeValue;
		var textNode = document.createTextNode(t);
		var no = document.createElement('option');
		no.value = t;
		no.appendChild(textNode);
		obj.appendChild(no);
	}
}
function clearLevelOptionsTo()
{
	var obj = document.getElementById('level_to');
	if(obj == null)
		return;
	var n = obj.childNodes.length;
	for(var m=0; m<n; m++)
		obj.removeChild(obj.childNodes[0]);
}
function getSection()
{
	var area = document.f.area.value;
	if(area == 'NaN')
		return;
	var location = document.f.location.value;
	if(location == 'NaN')
		return;
	if(location == '')
	{
		clearSectionOptions();
		clearLevelOptions();
		return;
	}
	xmlHttp = createXMLHttpRequest();
	if(!xmlHttp)
		return;
	var formdata = 'area=' + area + '&location=' + location;
";
s += "	var url = '?t=j&a=get_section';\r\n";
s += @"
	xmlHttp.onreadystatechange = handleGetSection;
	xmlHttp.open('POST', url, true);
	xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
	xmlHttp.send(formdata);
	document.f.name.value = '';
}
function handleGetSection()
{
	if(xmlHttp.readyState == 4)
	{
		if(xmlHttp.status == 200)
		{
			var ret = xmlHttp.responseText;
			if(ret.substr(0, 1) == '!')
			{
				window.alert(ret);
				return;
			}
			resetSection(ret);
		} 
		else
		{
			alert('There was a problem with the request.');
		}
	}
}
function resetSection(txt)
{
	if(txt == '')
		return;
	var doc = null;
	if(moz)
	{
		var parser = new DOMParser();
		doc = parser.parseFromString(txt, 'text/xml');
	}
	else if(ie)
	{
		doc = new ActiveXObject('Microsoft.XMLDOM');
		doc.async = false;
		doc.loadXML(txt);
	}
	if(!doc)
		return;
	var obj = document.getElementById('section');
	if(obj == null)
		return;
	clearSectionOptions();
	clearLevelOptions();
	var root_doc = doc.getElementsByTagName('section');
	for(var i=0; i<root_doc.length; i++)
	{
		var t = root_doc[i].childNodes[0].nodeValue;
		var textNode = document.createTextNode(t);
		var no = document.createElement('option');
		no.value = t;
		no.appendChild(textNode);
		obj.appendChild(no);
	}
}
function clearSectionOptions()
{
	var obj = document.getElementById('section');
	if(obj == null)
		return;
	var n = obj.childNodes.length;
	for(var m=0; m<n; m++)
		obj.removeChild(obj.childNodes[0]);
	var textNode = document.createTextNode('');
	var no = document.createElement('option');
	no.value = '';
	no.appendChild(textNode);
	obj.appendChild(no);
}
function clearLocationOptions()
{
	var obj = document.getElementById('location');
	if(obj == null)
		return;
	var n = obj.childNodes.length;
	for(var m=0; m<n; m++)
		obj.removeChild(obj.childNodes[0]);
	var textNode = document.createTextNode('');
	var no = document.createElement('option');
	no.value = '';
	no.appendChild(textNode);
	obj.appendChild(no);
}

function getLocation()
{
	var area = document.f.area.value;
	if(area == 'NaN')
		return;
	if(area == '')
	{
		clearLocationOptions();
		clearSectionOptions();
		clearLevelOptions();
		return;
	}
	xmlHttp = createXMLHttpRequest();
	if(!xmlHttp)
		return;
	var formdata = 'area=' + area;
";
s += "	var url = '?t=j&a=get_location';\r\n";
s += @"
	xmlHttp.onreadystatechange = handleGetLocation;
	xmlHttp.open('POST', url, true);
	xmlHttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
	xmlHttp.send(formdata);
	document.f.name.value = '';
}
function handleGetLocation()
{
	if(xmlHttp.readyState == 4)
	{
		if(xmlHttp.status == 200)
		{
			var ret = xmlHttp.responseText;
			if(ret.substr(0, 1) == '!')
			{
				window.alert(ret);
				return;
			}
			resetLocation(ret);
		} 
		else
		{
			alert('There was a problem with the request.');
		}
	}
}
function resetLocation(txt)
{
	if(txt == '')
		return;
	var doc = null;
	if(moz)
	{
		var parser = new DOMParser();
		doc = parser.parseFromString(txt, 'text/xml');
	}
	else if(ie)
	{
		doc = new ActiveXObject('Microsoft.XMLDOM');
		doc.async = false;
		doc.loadXML(txt);
	}
	if(!doc)
		return;
	var obj = document.getElementById('location');
	if(obj == null)
		return;
	clearLocationOptions();
	clearSectionOptions();
	clearLevelOptions();
	var root_doc = doc.getElementsByTagName('location');
	for(var i=0; i<root_doc.length; i++)
	{
		var t = root_doc[i].childNodes[0].nodeValue;
		var textNode = document.createTextNode(t);
		var no = document.createElement('option');
		no.value = t;
		no.appendChild(textNode);
		obj.appendChild(no);
	}
}
</script";
	s += ">";
	return s;
}

bool DoAjaxResponseCheckCode()
{
	string code = p("code");
	if(code == "")
		return true;
	if(!IsInteger(code))
		return true;
	string sc = " SELECT c.name, s.name AS shelf_name, si.qty ";
	sc += " FROM code_relations c ";
	sc += " LEFT OUTER JOIN shelf_item si ON si.code = c.code ";
	sc += " LEFT OUTER JOIN shelf s ON s.id = si.shelf_id ";
	sc += " WHERE 1 = 1 ";//c.supplier_code = '" + EncodeQuote(code) + "' ";
	sc += " AND c.code = " + code + " ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "item") <= 0)
		{
			Response.Write("!" + Lang("M_PN not found"));
			return false;
		}
	}
	catch(Exception e) 
	{
		Response.Write("!" + e.ToString());
		return false;
	}
	string s = "";
	for(int i=0; i<ds.Tables["item"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["item"].Rows[0];
		if(s == "")
			s = dr["name"].ToString();;
		string shelf_name = dr["shelf_name"].ToString();
		string qty = dr["qty"].ToString();
		if(qty != "" && MyDoubleParse(qty) != 0)
			s += ", " + qty + "@" + shelf_name;
	}
	Response.Write(s);
	return true;
}

bool DoAjaxResponseGetLevel()
{
	string area = p("area");
	string location = p("location");
	string section = p("section");
	if(section == "")
		return true;
	int nRows = 0;
	string sc = " SELECT DISTINCT level ";
	sc += " FROM shelf ";
	sc += " WHERE area = N'" + EncodeQuote(area) + "' ";
	sc += " AND location = N'" + EncodeQuote(location) + "' ";
	sc += " AND section = N'" + EncodeQuote(section) + "' ";
	sc += " ORDER BY level ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand1.Fill(ds, "level");
	}
	catch(Exception e) 
	{
		Response.Write("!" + e.ToString());
		return false;
	}
	string s = "<doc>";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["level"].Rows[i];
		string level = dr["level"].ToString();
		s += "<level>" + level + "</level>";
	}
	s += "</doc>";
	Response.Write(s);
	return false;
}

bool DoAjaxResponseGetSection()
{
	string area = p("area");
	string location = p("location");
	if(location == "")
		return true;
	int nRows = 0;
	string sc = " SELECT DISTINCT section ";
	sc += " FROM shelf ";
	sc += " WHERE area = N'" + EncodeQuote(area) + "' ";
	sc += " AND location = N'" + EncodeQuote(location) + "' ";
	sc += " ORDER BY section ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand1.Fill(ds, "section");
	}
	catch(Exception e) 
	{
		Response.Write("!" + e.ToString());
		return false;
	}
	string s = "<doc>";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["section"].Rows[i];
		string level = dr["section"].ToString();
		s += "<section>" + level + "</section>";
	}
	s += "</doc>";
	Response.Write(s);
	return false;
}

bool DoAjaxResponseGetLocation()
{
	string area = p("area");
	if(area == "")
		return true;
	int nRows = 0;
	string sc = " SELECT DISTINCT location ";
	sc += " FROM shelf ";
	sc += " WHERE area = N'" + EncodeQuote(area) + "' ";
	sc += " ORDER BY location ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		nRows = myCommand1.Fill(ds, "location");
	}
	catch(Exception e) 
	{
		Response.Write("!" + e.ToString());
		return false;
	}
	string s = "<doc>";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["location"].Rows[i];
		string level = dr["location"].ToString();
		s += "<location>" + level + "</location>";
	}
	s += "</doc>";
	Response.Write(s);
	return false;
}
</script>

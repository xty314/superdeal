<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_cid = "";
string m_id = "";
string m_kw = "";

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
	m_cid = MyIntParse(g("cid")).ToString();
	m_id = MyIntParse(g("id")).ToString();
	
	if(m_cmd == Lang("Add"))
	{
		if(DoUpdateData(""))
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("New data successfully added, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?t=" + m_type + "&cid=" + m_cid + "&id=" + m_id + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Save"))
	{
		if(DoUpdateData(m_id))
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully saved, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?t=" + m_type + "&cid=" + m_cid + "&id=" + m_id + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_action == "del")
	{
		if(DoDeleteData())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully deleted, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?t=" + m_type + "&cid=" + m_cid + "&id=" + m_id + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_action == "sd")
	{
		if(DoSetDefault())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully saved, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?t=" + m_type + "&cid=" + m_cid + "\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	
	PrintAdminHeader();
	if(m_type != "sa")
		PrintAdminMenu();
	PrintMainForm();
}

bool PrintMainForm()
{
	Response.Write("<br><center><h4>" + Lang("Edit Address") + "</h4></center>");
	Response.Write("<form name=f action=?t=" + m_type + "&cid=" + m_cid + "&id=" + m_id + " method=post>");
	Response.Write("<table width=95% align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr class=th>");
	Response.Write("<th>ID</th>");
	Response.Write("<th>" + Lang("Branch Name") + "</th>");
	Response.Write("<th>" + Lang("Contact") + "</th>");
	Response.Write("<th>" + Lang("Phone") + "</th>");
	Response.Write("<th>" + Lang("Mobile") + "</th>");
	Response.Write("<th>" + Lang("Address") + "</th>");
	Response.Write("<th>" + Lang("Default") + "</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	int nRows = 0;
	string sc = " SELECT ca.id, ca.company, ca.contact, ca.phone, ca.mobile, ca.address, ca.is_default ";
	sc += " FROM card_address ca ";
	sc += " WHERE card_id = " + m_cid;
	sc += " ORDER BY ca.id ";
//DEBUG("sc=", sc);	
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
	
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		string id = dr["id"].ToString();
		string company = dr["company"].ToString();
		string contact = dr["contact"].ToString();
		string phone = dr["phone"].ToString();
		string mobile = dr["mobile"].ToString();
		string address = dr["address"].ToString();
		bool bDefault  = MyBooleanParse(dr["is_default"].ToString());
		string sDefault = "";
		string sDefaultTitle = "<a href=?cid=" + m_cid + "&id=" + id + "&a=sd class=w style=\"color:#888888;\">" + Lang("Set default") + "</a>";
		if(bDefault)
		{
			sDefault = " checked";
			sDefaultTitle = "<font color=green>" + Lang("Yes") + "</font>";
		}
		
		Response.Write("<tr class=tn>");
		Response.Write("<td class=tb align=center>" + id + "</td>");
		if(id == m_id) //edit
		{
			Response.Write("<td class=tb><input type=text name=company value='" + company + "'></td>");
			Response.Write("<td class=tb><input type=text name=contact value='" + contact + "' onclick=\"calendar();\"></td>");
			Response.Write("<td class=tb><input type=text size=5 name=phone value='" + phone + "'></td>");
			Response.Write("<td class=tb><input type=text size=5 name=mobile value='" + mobile + "'></td>");
			Response.Write("<td class=tb><input type=text size=50 name=address value='" + address + "'></td>");
			Response.Write("<td><input type=checkbox name=is_default value=1 " + sDefault + "></td>");
			Response.Write("<td class=tb align=right>");
			Response.Write("<input type=submit name=cmd value='" + Lang("Save") + "' class=b>");
			Response.Write("<input type=button class=b value='" + Lang("Cancel") + "' onclick=\"history.go(-1);\">");
			Response.Write("<input type=button class=b value='" + Lang("Delete") + "' onclick=\"if(!window.confirm('");
			Response.Write(Lang("Are you sure to delete?") + "')){return false;}else{window.location='?id=" + id + "&a=del';}\">");
			Response.Write("</td>");
		}
		else
		{
			string sClick = "";
			string sClickEnd = "";
			string addr = company + "\\r\\n" + contact + "\\r\\n" + address + "\\r\\n" + phone + " " + mobile;
			if(m_type == "sa")
			{
				sClick = "<a class=o style=\"cursor:hand\" onclick=\"window.opener.document.form1.special_shipto.checked=1;";
				sClick += "window.opener.document.form1.special_ship_to_addr.value='" + addr + "';";
				sClick += "window.opener.document.all('tshiptoaddr').style.visibility='hidden';";
				sClick += "window.opener.document.all('ssta').style.visibility='visible';";
				sClick += "window.close();\">";
				sClickEnd = "</a>";
			}
			Response.Write("<td class=tb>" + sClick + company + sClickEnd + "</td>");
			Response.Write("<td class=tb>" + sClick + contact + sClickEnd + "</td>");
			Response.Write("<td class=tb>" + sClick + phone + sClickEnd + "</td>");
			Response.Write("<td class=tb>" + sClick + mobile + sClickEnd + "</td>");
			Response.Write("<td class=tb>" + sClick + address + sClickEnd + "</td>");
			Response.Write("<td align=center>" + sDefaultTitle + "</td>");
			Response.Write("<td class=tb align=right>");
			if(m_type == "sa")
				Response.Write(sClick + Lang("Select This") + sClickEnd + " &nbsp; ");
			Response.Write("<input type=button class=b value='" + Lang("Edit") + "' onclick=\"window.location='?t=e&id=" + id + "';\">");
			Response.Write("</td>");
		}
		Response.Write("</tr>");
	}
	
	if(m_id == "0")
	{	
		Response.Write("<tr class=ts>");
		Response.Write("<td class=tb align=right>&nbsp;" + Lang("Add New") + ":</td>");
		Response.Write("<td class=tb><input type=text size=10 name=company></td>");
		Response.Write("<td class=tb><input type=text size=10 name=contact></td>");
		Response.Write("<td class=tb><input type=text size=5 name=phone></td>");
		Response.Write("<td class=tb><input type=text size=5 name=mobile></td>");
		Response.Write("<td class=tb><input type=text size=50 name=address></td>");
		Response.Write("<td class=tb>&nbsp;</td>");
		Response.Write("<td class=tb align=right><input type=submit name=cmd value='" + Lang("Add") + "' class=b></td>");
		Response.Write("</tr>");
	}
	Response.Write("</table></form>");
	return true;
}

bool DoUpdateData(string id)
{
	string company = p("company");
	string contact = p("contact");
	string phone = p("phone");
	string mobile = p("mobile");
	string address = p("address");
	
	string sc = "";
	if(id == "") //add new
	{
		sc = " BEGIN TRANSACTION ";
		sc += " INSERT INTO card_address (card_id) VALUES(" + m_cid + ") ";
		sc += " SELECT IDENT_CURRENT('card_address') AS id ";
		sc += " COMMIT ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "data") <= 0)
			{
				ErrMsgAdmin("getting new id failed");
				return false;
			}
			m_id = ds.Tables["data"].Rows[0]["id"].ToString();
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
	}
	
	if(m_id == "" || m_id == "0")
	{
		ErrMsgAdmin("Invalid id");
		return false;
	}
	
	sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE card_address SET company = N'" + EncodeQuote(company) + "' ";
	sc += ", contact = N'" + EncodeQuote(contact) + "' ";
	sc += ", phone = N'" + EncodeQuote(phone) + "' ";
	sc += ", mobile = N'" + EncodeQuote(mobile) + "' ";
	sc += ", address = N'" + EncodeQuote(address) + "' ";
	sc += " WHERE id = " + m_id;
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

bool DoDeleteData()
{
	if(m_id == "" || m_id == "0")
		return true;
	
	string sc = " DELETE FROM card_address WHERE id = " + m_id;
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
bool DoSetDefault()
{
	string sc = " UPDATE card_address SET is_default = 0 WHERE card_id = " + m_cid;
	sc += " UPDATE card_address SET is_default = 1 WHERE id = " + m_id;
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

<script runat=server>

DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_id = "";

void Page_Load(Object Src, EventArgs E ) 
{
    TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
    
	m_type = g("t");
	m_action = g("a");
	m_cmd = p("cmd");
	m_id = MyIntParse(g("id")).ToString();
	
	if(m_cmd == Lang("Add"))
	{
		if(DoUpdateData(""))
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("New data successfully added, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_cmd == Lang("Save"))
	{
		if(DoUpdateData(m_id))
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully saved, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	else if(m_action == "del")
	{
		if(DoDeleteData())
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>" + Lang("Data successfully deleted, please wait a moment."));
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	if(m_type == "new" || m_type == "e")
		PrintEditForm();
	else
		PrintMainForm();
}

bool PrintMainForm()
{
	Response.Write("<br><center><h4>" + Lang("Battery Setup") + "</h4></center>");
	Response.Write("<form name=f action=?id=" + m_id + " method=post>");
	Response.Write("<table width=750 align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr><td colspan=9 align=right>");
	Response.Write("<input type=button class=b value='" + Lang("Add New") + "' onclick=\"window.location='?t=new';\">");
	Response.Write("</td></tr>");
	Response.Write("<tr class=th>");
	Response.Write("<th>ID</th>");
	Response.Write("<th>" + Lang("Site") + "</th>");
	Response.Write("<th nowrap>" + Lang("Battery Group") + "</th>");
	Response.Write("<th nowrap>" + Lang("Battery Count") + "</th>");
	Response.Write("<th>" + Lang("Brand") + "</th>");
	Response.Write("<th>" + Lang("Model") + "</th>");
	Response.Write("<th>" + Lang("Capacity") + "</th>");
	Response.Write("<th>IP</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");

	string sc = " SELECT b.*, s.name AS site_name ";
	sc += " FROM battery b ";
	sc += " LEFT OUTER JOIN site s ON s.id = b.site_id ";
	sc += " ORDER BY b.site_id, b.group_number, b.id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "data");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		string id = dr["id"].ToString();
		string site_name = dr["site_name"].ToString();
		string group_number = dr["group_number"].ToString();
		string battery_count = dr["battery_count"].ToString();
		string brand = dr["brand"].ToString();
		string model = dr["model"].ToString();
		string capacity = dr["capacity"].ToString();
		string ip = dr["ip"].ToString();
		
		Response.Write("<tr class=ts>");
		Response.Write("<td class=tb align=center>" + id + "</td>");
		Response.Write("<td class=tb>" + site_name + "</td>");
		Response.Write("<td class=tb>" + group_number + "</td>");
		Response.Write("<td class=tb>" + battery_count + "</td>");
		Response.Write("<td class=tb>" + brand + "</td>");
		Response.Write("<td class=tb>" + model + "</td>");
		Response.Write("<td class=tb>" + capacity + "</td>");
		Response.Write("<td class=tb>" + ip + "</td>");
		Response.Write("<td class=tb align=right><input type=button class=b value='" + Lang("Edit") + "' onclick=\"window.location='?t=e&id=" + id + "';\">");
		Response.Write("<input type=button class=b value='" + Lang("Delete") + "' onclick=\"if(!window.confirm('");
		Response.Write(Lang("Are you sure to delete?") + "')){return false;}else{window.location='?id=" + id + "&a=del';}\">");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table></form>");
	return true;
}

bool PrintEditForm()
{
	string site_id = "";
	string group_number = "";
	string battery_count = "";
	string brand = "";
	string model = "";
	string capacity = "";
	string serial_number = "";
	string maintenance_count = "";
	string date_manufact = "";
	string date_install = "";
	string ip = "";
	string port = "";
	if(m_id == "" || m_id == "0")
	{
		Response.Write("<br><center><h4>" + Lang("Add New Battery") + "</h4></center>");
	}
	else
	{
		Response.Write("<br><center><h4>" + Lang("Edit Battery") + "</h4></center>");
		string sc = "SELECT * ";
		sc += ", CONVERT(varchar(100), date_manufact, 23) AS sdate_manufact ";
		sc += ", CONVERT(varchar(100), date_install, 23) AS sdate_install ";
		sc += " FROM battery WHERE id = " + m_id;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "data") <= 0)
			{
				ErrMsgAdmin("Battery #" + m_id + " not found");
				return false;
			}
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
		DataRow dr = ds.Tables["data"].Rows[0];
		site_id = dr["site_id"].ToString();
		group_number = dr["group_number"].ToString();
		battery_count = dr["battery_count"].ToString();
		brand = dr["brand"].ToString();
		model = dr["model"].ToString();
		capacity = dr["capacity"].ToString();
		serial_number = dr["serial_number"].ToString();
		maintenance_count = dr["maintenance_count"].ToString();
		date_manufact = dr["sdate_manufact"].ToString();
		date_install = dr["sdate_install"].ToString();
		ip = dr["ip"].ToString();
		port = dr["port"].ToString();
	}
	Response.Write("<form name=f action=?id=" + m_id + " method=post>");
	Response.Write("<script language=javascript src='cssjs/calendar30.js' charset=gb2312></script");
	Response.Write("><table width=300 align=center cellspacing=0 cellpadding=0 border=0 class=t>");
	Response.Write("<tr><th align=right>" + Lang("Site") + " :&nbsp;</th><td align=left style=\"background-color:#DDDDDD;\"><select name=site_id>");
	Response.Write(PrintSiteOptions(site_id));
	Response.Write("</select></th></tr>");
	
	Response.Write("<tr><th align=right>&nbsp;" + Lang("Group Number") + " :&nbsp;</th><td class=tb><input type=text name=group_number value='" + group_number + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Battery Count") + " :&nbsp;</th><td class=tb><input type=text name=battery_count value='" + battery_count + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Brand") + " :&nbsp;</th><td class=tb><input type=text name=brand value='" + brand + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Model") + " :&nbsp;</th><td class=tb><input type=text name=model value='" + model + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Capacity") + " :&nbsp;</th><td class=tb><input type=text name=capacity value='" + capacity + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Serial Number") + " :&nbsp;</th><td class=tb><input type=text name=serial_number value='" + serial_number + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Maintenance Count") + " :&nbsp;</th><td class=tb><input type=text name=maintenance_count value='" + maintenance_count + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Manufacture Date") + " :&nbsp;</th><td class=tb><input type=text name=date_manufact value='" + date_manufact + "' onclick=\"calendar();\"></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Installation Date") + " :&nbsp;</th><td class=tb><input type=text name=date_install value='" + date_install + "' onclick=\"calendar();\"></td></tr>");
	Response.Write("<tr><th align=right>IP :&nbsp;</th><td class=tb><input type=text name=ip value='" + ip + "'></td></tr>");
	Response.Write("<tr><th align=right>" + Lang("Port") + " :&nbsp;</th><td class=tb><input type=text name=port value='" + port + "'></td></tr>");

	Response.Write("<tr><td class=tb colspan=2 align=right>");
	if(m_id == "" || m_id == "0")
		Response.Write("<input type=submit name=cmd value='" + Lang("Add") + "' class=b>");
	else
		Response.Write("<input type=submit name=cmd value='" + Lang("Save") + "' class=b>");
	Response.Write("<input type=button class=b value='" + Lang("Cancel") + "' onclick=\"history.go(-1);\">");
	Response.Write("</td></tr>");
	Response.Write("</table></form>");
	return true;
}

bool DoUpdateData(string id)
{
	string site_id = p("site_id");
	string group_number = p("group_number");
	string battery_count = p("battery_count");
	string brand = p("brand");
	string model = p("model");
	string capacity = p("capacity");
	string serial_number = p("serial_number");
	string maintenance_count = p("maintenance_count");
	string date_manufact = p("date_manufact");
	string date_install = p("date_install");
	string ip = p("ip");
	string port = p("port");
	string sc = "";
	if(id == "") //add new
	{
		sc = " BEGIN TRANSACTION ";
		sc += " INSERT INTO battery (site_id) VALUES(" + EncodeQuote(site_id) + ") ";
		sc += " SELECT IDENT_CURRENT('battery') AS id ";
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
	
	sc = " SET DATEFORMAT ymd ";
	sc += " UPDATE battery SET site_id = " + site_id + " ";
	sc += ", group_number = N'" + EncodeQuote(group_number) + "' ";
	sc += ", battery_count = " + MyIntParse(battery_count) + " ";
	sc += ", maintenance_count = " + MyIntParse(maintenance_count) + " ";
	sc += ", brand = N'" + EncodeQuote(brand) + "' ";
	sc += ", model = N'" + EncodeQuote(model) + "' ";
	sc += ", capacity = N'" + EncodeQuote(capacity) + "' ";
	sc += ", serial_number = N'" + EncodeQuote(serial_number) + "' ";
	sc += ", date_manufact = N'" + EncodeQuote(date_manufact) + "' ";
	sc += ", date_install = N'" + EncodeQuote(date_install) + "' ";
	sc += ", ip = '" + EncodeQuote(ip) + "' ";
	sc += ", port = " + MyIntParse(port) + " ";
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
	string sc = " DELETE FROM site WHERE id = " + m_id;
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

<!-- #include file="q.cs" -->
<script runat=server>

//DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_type = "";
string m_cmd = null;

int sys_count = 0;
string name = "";
string note = "";
bool m_bNewSys = false; //adding a new system

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

/*	if(Request.Form["cmd"] == null)
	{
		if(Request.QueryString["r"] == null) //force refresh
		{
			Response.Redirect(Request.ServerVariables["URL"] + "?" 
				+ Request.ServerVariables["QUERY_STRING"] + "&r="
				+ DateTime.Now.ToOADate());
			return;
		}
	}
*/
	if(Session[m_sCompanyName + "_syse_new"] != null)
	{
		if((bool)Session[m_sCompanyName + "_syse_new"])
			m_bNewSys = true;
	}
	
	if(!QPage_Load())
		return;

	GetQueryStrings();
	PrintAdminHeader();
	PrintAdminMenu();

	m_cmd = Request.Form["cmd"];
	if(m_cmd == "Update Configuration" || m_cmd == "Save Configuration")
	{
		Response.Write("<h3>Processing, wait ...... </h3>");
		Response.Flush();
		if(DoUpdate())
		{
			Session[m_sCompanyName + "_syse_new"] = false;
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=sqm_syse.aspx");
			if(m_id >= 0)
				Response.Write("?rid=" + m_id + "&t=get&r=" + DateTime.Now.ToOADate());
			Response.Write("\">");
		}
	}
	else if(m_cmd == "Add New Configuration")
	{
		//delete current quotation
		for(int i=0; i<m_qfields; i++)
		{
			dtQ.Rows[0][fn[i]] = "-1";
			dtQ.Rows[0][fn[i]+"_qty"] = "0";
			dtQ.Rows[0][fn[i]+"_price"] = "0";
		}
		m_bNewSys = true;
		Session[m_sCompanyName + "_syse_new"] = true;
		MyDrawTable(); //draw empty
	}
	else if(m_cmd == "Delete Configuration")
	{
		if(DoDelete())
		{
			Response.Write("<h3>Configuration Deleted. <a href=close.htm>Close</a></h3>");
		}
	}
	else
	{
		if(Request.QueryString["t"] == "get")
		{
			if(!DoSearch())
				return;
		}
		MyDrawTable();
	}

	PrintAdminFooter();
}

void GetQueryStrings()
{
	if(Request.QueryString["rid"] != null)
	{
		m_id = int.Parse(Request.QueryString["rid"]);
		Session[m_sCompanyName + "sqm_sys_id"] = m_id.ToString();
		Session[m_sCompanyName + "_syse_new"] = null;
		m_bNewSys = false;
	}
	else
	{
		if(Session[m_sCompanyName + "sqm_sys_id"] != null)
		{
			m_id = int.Parse(Session[m_sCompanyName + "sqm_sys_id"].ToString());
//			Session[m_sCompanyName + "_syse_new"] = null;
//			m_bNewSys = false;
		}
		else
		{
//			Session[m_sCompanyName + "_syse_new"] = true;
			Session[m_sCompanyName + "sqm_sys_id"] = null;
//			m_bNewSys = true;
//			PrepareNewQuote();
		}
	}
	if(Session[m_sCompanyName + "sqm_sys_name"] != null)
		name = Session[m_sCompanyName + "sqm_sys_name"].ToString();
	if(Session[m_sCompanyName + "sqm_sys_note"] != null)
		note = Session[m_sCompanyName + "sqm_sys_note"].ToString();
}

bool DoSearch()
{
	if(m_id < 0)
		return true;

	dst.Clear();
	
	string sc = "SELECT * FROM q_sys WHERE id=" + m_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "rec_sys");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//delete current quotation
	for(int i=0; i<m_qfields; i++)
	{
		dtQ.Rows[0][fn[i]] = "-1";
		dtQ.Rows[0][fn[i]+"_qty"] = "0";
		dtQ.Rows[0][fn[i]+"_price"] = "0";
		dtQ.Rows[0][fn[i]+"_name"] = "";
		dtQ.Rows[0][fn[i]+"_special"] = "0";
	}

	if(dst.Tables.Count > 0 && dst.Tables["rec_sys"].Rows.Count > 0)
	{
		sys_count = dst.Tables["rec_sys"].Rows.Count;
		DataRow dr = dst.Tables["rec_sys"].Rows[0];
		if(Request.QueryString["t"] != "change")
		{
			name = dr["name"].ToString();
			note = dr["note"].ToString();
			Session[m_sCompanyName + "sqm_sys_name"] = name;
			Session[m_sCompanyName + "sqm_sys_note"] = note;
			for(int i=0; i<m_qfields; i++)
			{
				double dPrice = 0;
				string code = dr[fn[i]].ToString();
				string qty = dr[fn[i]+"_qty"].ToString();
				GetItemPrice(code, qty, ref dPrice);
				dtQ.Rows[0][fn[i]] = code;
				dtQ.Rows[0][fn[i]+"_qty"] = qty;
				dtQ.Rows[0][fn[i]+"_price"] = dPrice.ToString();
//				dtQ.Rows[0][fn[i]+"_name"] = name;
//				if(bSpecial)
//					dtQ.Rows[0][fn[i]+"_special"] = "1";
			}
		}
		if(dr["price"].ToString() != "")
			m_dRecPrice = double.Parse(dr["price"].ToString());
	}

	return true;
}

Boolean DoDelete()
{
	if(Request.Form["delete"] != "on")
	{
		Response.Write("<h3>Error, please tick the checkbox to confirm deletion</h3>");
		return false;
	}
	if(m_id < 0)
	{
		Response.Write("<h2>Error, no id to delete</h2>");
		return false;
	}

	string sc = "DELETE FROM q_sys WHERE id=" + m_id;
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

Boolean DoUpdate()
{
	Boolean bRet = true;

	string name = Request.Form["name"];
	string note = Request.Form["note"];
//	string price = (double.Parse(Request.Form["proprice"], NumberStyles.Currency, null)).ToString();
	name = EncodeQuote(name);
	note = EncodeQuote(note);

	DataRow dr = dtQ.Rows[0];
	
	StringBuilder sb = new StringBuilder();
	if(m_cmd == "Update Configuration")
	{
		sb.Append("UPDATE q_sys SET ");
		sb.Append("name='" + name + "', ");
		sb.Append("note='" + note + "'");
		for(int i=0; i<m_qfields; i++)
		{
			sb.Append(", " + fn[i] + "='" + dr[fn[i]].ToString() + "', ");
			sb.Append(fn[i] + "_qty=" + dr[fn[i]+"_qty"].ToString());
		}
//		sb.Append(", price=" + price);
		sb.Append(" WHERE id=" + m_id);
	}
	else
	{
		sb.Append("INSERT INTO q_sys (id, name, note");
		for(int i=0; i<m_qfields; i++)
		{
			sb.Append(", " + fn[i]);
			sb.Append(", " + fn[i] + "_qty");
		}
		sb.Append(") VALUES(" + GetNextSysId() + ", ");
		sb.Append("'" + name + "', ");
		sb.Append("'" + note + "'");
		for(int i=0; i<m_qfields; i++)
		{
			sb.Append(", '" + dr[fn[i]].ToString());
			sb.Append("', " + dr[fn[i]+"_qty"].ToString());
		}
//		sb.Append(", " + price);
		sb.Append(") ");
	}
//DEBUG("sc=", sb.ToString());
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	return bRet;
}

string GetNextSysId()
{
	int id = 0;
	string sc = "SELECT TOP 1 id FROM q_sys ORDER BY id DESC";
	DataSet dsi = new DataSet();
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsi) > 0)
		{
			string sid = dsi.Tables[0].Rows[0]["id"].ToString();
			id = int.Parse(sid);
			id++;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	m_id = id;
	Session[m_sCompanyName + "sqm_sys_id"] = m_id.ToString();
//DEBUG("m_id=", id);
	return id.ToString();
}

Boolean MyDrawTable()
{
	bool bRet = true;

	Response.Write("<form action=sqm_syse.aspx?rid=" + m_id + " method=post>\r\n");
//	Response.Write("<table border=1 bordercolor=#EEEEEE cellspacing=0 cellpadding=0 align=center>");
	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4 align=center><h3>");
	
	if(!m_bNewSys)
		Response.Write("Edit Configuration");
	else
		Response.Write("New Configuration");
	Response.Write("</h3></td></tr>");
	
	m_bAdmin = true;
	Response.Write("<tr><td colspan=2>");
	PrintQForm();
	Response.Write("</td></tr>");

//	Response.Write("<tr><td colspan=4 align=right><input type=submit name=cmd value=ReCalculate " + Session["button_style"] + "></td></tr>");
	Response.Write("<tr><td align=right>&nbsp;</td></tr>");
	Response.Write("<tr><td><b>Name</b></td><td><input type=text size=50 name=name value='");
	Response.Write(name);
	Response.Write("'>");
/*	Response.Write("&nbsp&nbsp&nbsp;<div align=right><b>Sales Price : </b>");
	Response.Write("<input type=text style='text-align:right' size=10 name=proprice value='");
	if(m_dRecPrice <= 0)
		Response.Write(m_dTotal.ToString("c")); 
	else
		Response.Write(m_dRecPrice.ToString("c"));
	Response.Write("'> + GST</div>");
*/
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Description</b></td><td><textarea name=note cols=75 rows=7>" + note + "</textarea></td></tr>");
	Response.Write("<tr><td colspan=2 align=right>");

	if(m_bNewSys)
	{
		Response.Write("<input type=submit name=cmd value='Save Configuration' " + Session["button_style"] + ">");
	}
	else
	{
		Response.Write("<input type=checkbox name=delete> Delete this configuration ");
		Response.Write("<input type=submit name=cmd value='Delete Configuration' " + Session["button_style"] + ">&nbsp;&nbsp;");
		Response.Write("<input type=submit name=cmd value='Add New Configuration' " + Session["button_style"] + ">&nbsp;&nbsp;");
		Response.Write("<input type=submit name=cmd value='Update Configuration' " + Session["button_style"] + ">");
	}
	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	return bRet;
}
</script>

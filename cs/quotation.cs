<!-- #include file="kit_fun.cs" -->
<script runat=server>

DataSet ds = new DataSet();

bool m_bSayWillAddLaborFee = true;

bool GetSystemPrice(string id, ref string price)
{
	DataSet dst = new DataSet();
	string sc = "SELECT * FROM q_sys WHERE id=" + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "rc_sys");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	double dTotalPrice = 0;
	if(dst.Tables.Count > 0 && dst.Tables["rc_sys"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["rc_sys"].Rows[0];
		for(int i=0; i<m_qfields; i++)
		{
			string code = dr[fn[i]].ToString();
			string qty = dr[fn[i]+"_qty"].ToString();
//DEBUG("fn[i]="+fn[i], " code="+dr[fn[i]].ToString());
			double dPrice = 0;
			if(IsInteger(code) && int.Parse(code) > 0)
			{
				string card_id = "";
				if(Session["card_id"] != null)
					card_id = Session["card_id"];
				dPrice = GetSalesPriceForDealer(code, qty, Session[m_sCompanyName + "dealer_level"].ToString(), card_id);

//				if(!GetItemPrice(code, qty, ref dPrice))
//				{
//					dPrice = 999;
//					AlertMissProduct(code, fn[i], m_sSalesEmail);
//					return false;
//				}
				dTotalPrice += dPrice * MyIntParse(qty);
			}
		}
	}
	else
	{
		Response.Write("<h3>&nbsp;&nbsp;&nbsp;Error, System Configuration Not Found</h3>");
		return false;
	}
	price = dTotalPrice.ToString("c");
	return true;
}

bool PrintPage()
{
	CheckQTable();
	if(!GetRecommended())
		return false;

	Response.Write("\r\n\r\n<br><table border=0 align=center ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td align=center><h3>SYSTEM QUOTATION</h3></td></tr>");
//	Response.Write("<tr><td><img border=0 src='/i/quot.gif'></td></tr><tr><td>");

	if(!g_bRetailVersion && m_bSayWillAddLaborFee)
	{
		string qs = ReadSitePage("quotation_labour_fee_notice");
		qs = qs.Replace("\r\n", "<br>\r\n");
		Response.Write("<tr><td>");
		Response.Write(qs);
		Response.Write("</td></tr>");
	}
	Response.Write("<tr><td>");
	Response.Write("\r\n<table width=90% align=center valign=center cellspacing=3 cellpadding=3 border=0>");

	Response.Write("<tr><td><img src=r.gif border=0><font size=+1><b> Smart Build</b></font></td></tr>");
	Response.Write("<tr><td><form action=qcb.aspx method=post>");
	Response.Write("<table border=1><tr><td colspan=3><b>My ideal computer would be like:</b></td></tr>");
	Response.Write("<tr><td><input type=checkbox name=ccpu checked></td><td>With a CPU</td><td>");
	PrintCpuOptions("cpu");
	Response.Write("</td></tr>");
//	Response.Write("<tr><td><input type=checkbox name=cram></td><td>With Memory</td><td>256MB</td></tr>");
//	Response.Write("<tr><td><input type=checkbox name=chd></td><td>Have A Hard Drive</td><td>20G</td></tr>");
//	Response.Write("<tr><td><input type=checkbox name=cprice></td><td>System Price Around $</td>");
	Response.Write("<tr><td align=center><font size=+1 color=gold><b>$</b></font></td><td>System Price Around $</td>");
	Response.Write("<td><input type=text size=10 name=price>&nbsp;<input type=checkbox name=cgst>Inclusive of GST</td></tr>");
	Response.Write("<tr><td colspan=3>&nbsp;</td></tr><tr><td colspan=3><b>I will use this computer for:</b></td></tr>");
	Response.Write("<tr><td><input type=checkbox name=cgaming></td><td colspan=2>3D Gaming</td></tr>");
//	Response.Write("<tr><td><input type=checkbox name=cstudy></td><td colspan=2>Study</td></tr>");
	Response.Write("<tr><td><input type=checkbox name=cnet></td><td colspan=2>Internet</td></tr>");
	Response.Write("<tr><td colspan=3><input type=submit name=cmd value='Click here to give it a try !' " + Session["button_style"] + "></td></tr>");
	Response.Write("</table></form></td></tr>");

	Response.Write("<tr><td><a href=q.aspx?p=new><img src=r.gif border=0><font size=+1><b> DIY (Do It Yourself)</b></font></a></td></tr>");
	Response.Write("<tr><td><hr></td></tr>");
	int rows = ds.Tables[0].Rows.Count;
	if(rows > 0)
	{
//		Response.Write("\r\n<table width=90% align=center valign=center cellspacing=3 cellpadding=3 border=0>");
		Response.Write("<tr><td><img src=r.gif border=0><font size=+1><b> Recommended Configurations</b></font></td></tr>");
	}
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		string sid = dr["id"].ToString();
		string name = dr["name"].ToString();
		string desc = dr["note"].ToString();
		string price = "";//dr["price"].ToString();
		if(!GetSystemPrice(sid, ref price))
			return false;
		if(price != "")
		{
			double dPrice = double.Parse(price, NumberStyles.Currency, null);
			price = dPrice.ToString("c");
			desc += " <font size=+1 color=red><b>" + price + " + GST<b></font>";
		}
		
		Response.Write("<form action=q.aspx method=get><tr><td>");
		Response.Write("<b>System " + (i+1).ToString() + ":</b> <a href=q.aspx?id=" + sid + "><b>" + name + "</b></a>");
		Response.Write("<input type=hidden name=id value='" + sid + "'>&nbsp&nbsp;");
		Response.Write("<input type=submit value='View Details' " + Session["button_style"] + ">");
		Response.Write("</td></tr></form>");
		Response.Write("<tr><td><p>" + desc + "</p></td></tr>");
//		Response.Write("<tr><td><a href=q.aspx?id=" + sid + ">View Details</a></td></tr>");
//		Response.Write("<tr><td><form action=q.aspx method=get><input type=hidden name=id value='" + sid + "'>");
//		Response.Write("<input type=submit value='View Details' " + Session["button_style"] + "></form></td></tr>");
		Response.Write("<tr><td>&nbsp; </td></tr>");
	}

	Response.Write("</table></td></tr>");
	Response.Write("</table>\r\n");
	return true;
}

bool GetRecommended()
{
	ds.Clear();
	string sc = "SELECT id, name, note FROM q_sys";	 
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
	return true;
}


bool PrintCpuOptions(string s)
{
	DataSet dso = new DataSet();
	int rows = 0;

	string sc = "";
	if(s == "cpu")
		sc = "SELECT DISTINCT p.code, p.name, p.price FROM q_mb q JOIN product p ON q.parent=p.code ORDER BY p.code";
	else if(s == "monitor")
		sc = "SELECT p.code, p.name, p.price FROM q_flat q LEFT OUTER JOIN product p ON q.monitor=p.code WHERE q.monitor>0 ORDER BY p.code";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("\r\n<select name=cpu>");

	string card_id = "";
	if(Session["card_id"] != null)
		card_id = Session["card_id"];

	bool bMatch = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dso.Tables[0].Rows[i];
		string code = dr["code"].ToString();
		double dPrice = GetSalesPriceForDealer(code, "1", Session[m_sCompanyName + "dealer_level"].ToString(), card_id);
		Response.Write("\r\n<option value='" + code + "'");
		if(!bMatch)
		{
			Response.Write(" selected");
			bMatch = true;
		}
		Response.Write(">");
		string name = dr["name"].ToString();
		if(name != "")
		{
			Response.Write(name);
			Response.Write(" " + dPrice.ToString("c"));
		}
		Response.Write("</option>");
	}
	Response.Write("\r\n</select>");
	return true;
}
</script>

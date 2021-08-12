<!-- #include file="kit_fun.cs" -->

<script runat=server>

DataSet ds = new DataSet();
DataSet dst = new DataSet();

string m_code = "";
string m_new_code = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	InitKit();

	string help = Request.QueryString["h"];
	if(help != null && help != "")
	{
		ShowHelp(help);
		return;
	}

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
	{
		m_code = Request.QueryString["code"];
		if(!GetKitsNeedThisItem(m_code))
			return;
	}

	string t = Request.QueryString["t"];
	if(t == "readcart")
	{
		for(int i=0; i<dtCart.Rows.Count; i++)
		{
			m_new_code = dtCart.Rows[i]["code"].ToString();
		}
		EmptyCart();
	}

	string cmd = Request.Form["cmd"];
	if(cmd == "Browse")
	{
		CheckShoppingCart();
		EmptyCart();
		Session[m_sCompanyName + "_ordering"] = true;
		Session[m_sCompanyName + "_salestype"] = m_sKitTerm;
		Session[m_sCompanyName + "_salesurl"] = "kit_ci.aspx?t=readcart&code=" + m_code;
		Response.Redirect("c.aspx?ssid=" + m_ssid + "&t=" + m_sKitTerm);
		return;
	}
	else if(cmd == "Change Now")
	{
		if(Request.Form["new_code"] == null || Request.Form["new_code"] == "")
			Response.Write("<h1>Change to waht?</h1>");
		else
		{
			m_new_code = Request.Form["new_code"];
			if(KitDoChangeItem(m_code, m_new_code))
				return;
		}
	}
	PrintMainForm();
}

bool KitDoChangeItem(string old_code, string new_code)
{
	//check new_code
	string sc = "SELECT name, supplier_code, supplier_price FROM code_relations WHERE code=" + new_code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dskit, "new_item") <= 0)
		{
			Response.Write("<br><center><h1><font color=red>Error. Item " + m_new_code + " not found</h1>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	int rows = dskit.Tables["needs"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("<br><center><h3>No " + m_sKitTerm + " uses this item.");
		return true;
	}

	//begin processing
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<center><br><h3>Change " + m_sKitTerm + " Item " + "</h3>");
	Response.Write("<br><h4>Processing, please wait.......</h4>");

	Response.Write("<table border=0>");

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dskit.Tables["needs"].Rows[i];
		string kit_id = dr["id"].ToString();
		if(Request.Form["check_" + kit_id] == "on")
		{
			sc = " UPDATE kit_item SET code=" + new_code;
			sc += " WHERE id = " + kit_id + " AND code=" + old_code;
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

			if(GetKit(kit_id)) //GetKit() will clean dskit.Tables["kit"]
			{

				double dNewKitPrice = Math.Round(m_dKitItemBasePriceTotal * m_dKitRate, 0);

				if(m_dKitItemLastTotal != m_dKitItemBasePriceTotal)
				{
					sc = " UPDATE kit SET price = " + dNewKitPrice.ToString();
					sc += ", last_item_total = " + m_dKitItemBasePriceTotal;
					sc += " WHERE id = " + kit_id;
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
				Response.Write("<tr>");
				Response.Write("<td><a href=kit.aspx?id=" + kit_id + " target=_blank class=o>" + m_sKitTerm + "#" + kit_id + "</a></td>");
				Response.Write("<td>" + m_sKitName + "</td>");
				Response.Write("<td><b>" + m_sKitTerm + " Price changed from <font color=red>" + m_dKitPrice.ToString("c"));
				Response.Write("</font> to <font color=blue>" + dNewKitPrice.ToString("c") + "</font></td>");
				Response.Write("</tr>");
			}
		}
	}
	Response.Write("</table>");
	Response.Write("<h1>Done</h1>");
	PrintAdminFooter();
	return true;
}

bool GetKitsNeedThisItem(string item_code)
{
	if(!TSIsDigit(item_code))
		return false;

	string sc = " SELECT DISTINCT k.id, k.name ";
	sc += " FROM kit k JOIN kit_item i ON i.id=k.id ";
	sc += " WHERE i.code = " + item_code;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dskit, "needs") <= 0)
			return false;
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool PrintMainForm()
{
	PrintAdminHeader();
	PrintAdminMenu();

	string title = "Change " + m_sKitTerm + " Item";

	Response.Write("<br><center><h3>" + title + "</h3>");
	Response.Write("<form name=form action=kit_ci.aspx?code=" + m_code + " method=post>");

	Response.Write("<table cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	//print item details
	if(m_code != "")
	{
		string sc = "SELECT name, supplier_code FROM code_relations WHERE code=" + m_code;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dskit, "item") > 0)
			{
				DataRow drp = dskit.Tables["item"].Rows[0];
				Response.Write("<tr><td>");
				Response.Write("<font color=red size=+1>Old item : </font></td>");
				Response.Write("<td colspan=2>" + m_code);
				Response.Write("[" + drp["supplier_code"].ToString());
				Response.Write("] " + drp["name"].ToString() + "</td></tr>");
			}
			else
			{
				Response.Write("<h3>Item not found</h3>");
			}
		}
		catch(Exception e) 
		{
			Response.Write("<h3>Item not found</h3>");
			ShowExp(sc, e);
			return false;
		}
	}

	Response.Write("<tr><td><font size=+1>Change to : </td>");
	Response.Write("<td colspan=2>Code : <input type=text size=5 name=new_code value=" + m_new_code + "> ");
	Response.Write("<input type=submit name=cmd value=Browse class=b>");
	Response.Write("</td></tr>");

	if(dskit.Tables["needs"] != null && dskit.Tables["needs"].Rows.Count > 0)
	{
		Response.Write("<tr><td colspan=3><font size=+1>Apply to the following " + m_sKitTerm + "s : </font></td></tr>");
		for(int i=0; i<dskit.Tables["needs"].Rows.Count; i++)
		{
			DataRow dr = dskit.Tables["needs"].Rows[i];
			string kit_id = dr["id"].ToString();
			Response.Write("<tr><td>");
			Response.Write("<a href=kit.aspx?id=" + kit_id + " class=o target=_blank title='click to view'>");
			Response.Write(m_sKitTerm + "# " + kit_id + "</a> &nbsp&nbsp&nbsp&nbsp;");
			Response.Write("</td>");
			Response.Write("<td>" + dr["name"].ToString() + " &nbsp&nbsp&nbsp&nbsp;</td>");
			Response.Write("<td align=right><input type=checkbox name=check_" + kit_id + " checked></td></tr>");
		}
		Response.Write("<tr><td colspan=3 align=right>Select All : ");
		Response.Write("<input type=checkbox name=allbox value='Select All' checked onClick='CheckAll();'></td></tr>");
	}

	Response.Write("</table>");
	Response.Write("<input type=submit name=cmd value='Change Now' class=b>");
	Response.Write("</form>");
	PrintJavaFunctions();
	PrintAdminFooter();
	return true;
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.form.elements.length;i++) 
		{
			var e = document.form.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form.allbox.checked;
		}
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
}

void ShowHelp(string key)
{
	PrintAdminHeader();
	Response.Write("<br><table height=93% width=95% align=center valign=center bgcolor=white><tr><td valign=top>");
	string s = "<br><center><h3>Help - ";

	if(key == "aup") //auto update price
	{
		s += "Auto Update Price</h3></center>";
		s += "" + m_sKitTerm + " price will be updated automatically if any individual item's price changed.";
		s += "<br><br>New price will be calculated base on the original discount rate (" + m_sKitTerm + "_price / total_item_price).";
		s += "<br><br>However, You can manually update " + m_sKitTerm + " price at anytime, regardless this option is selected or not.";
	}
	Response.Write(s);
	Response.Write("<br><br><center><a href=close.htm class=o><font size=+1><b>Close Window</b></font></a>");
	Response.Write("</td></tr></table>");
}
</script>

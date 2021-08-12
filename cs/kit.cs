<!-- #include file="kit_fun.cs" -->

<script runat=server>

DataSet ds = new DataSet();
DataSet dst = new DataSet();

string m_scat = "";
string m_sscat = "";

string m_c = "hardware";
string m_s = "case";
string m_co = "-1"; //cat for options
string m_so = "-1"; //s_cat for options
string m_ck = "-1"; //cat for options
string m_sk = "-1"; //s_cat for options

double m_dTotal = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	//session control
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("kit.aspx" + par);
		return;
	}

	InitKit();

	m_c = Request.QueryString["c"];
	m_s = Request.QueryString["s"];
	if(Request.QueryString["co"] != null)
		m_co = Request.QueryString["co"];
	if(Request.QueryString["so"] != null)
		m_so = Request.QueryString["so"];
	if(Request.QueryString["ck"] != null)
		m_ck = Request.QueryString["ck"];
	if(Request.QueryString["sk"] != null)
		m_sk = Request.QueryString["sk"];

	string help = Request.QueryString["h"];
	if(help != null && help != "")
	{
		ShowHelp(help);
		return;
	}

	string t = Request.QueryString["t"];
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		m_sKitID = Request.QueryString["id"];
		if(t == null)
		{
			if(!RestoreKit())
				return;
		}
	}

	//save qty every time
	if(t == "ec")
		DoSaveQty();

	if(Request.QueryString["seq"] != null && Request.QueryString["seq"] != "")
	{
		if(DoMoveItem())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "\">");
		return;
	}

	else if(t == "addcache")
	{
		if(DoAddItemToCache(Request.QueryString["code"]))
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "\">");
		return;
	}
	else if(t == "rc") //remove from cache
	{
		DoRemoveItemFromCache(Request.QueryString["code"]);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "\">");
		return;
	}
	else if(t == "add") //add item
	{
		string code = Request.QueryString["code"];
		if(DoAddItem(code, "1", "0"))
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "\">");
		return;
	}

	if(Request.Form["cmd"] == "OK")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?rt=keepcontent&ssid=" + m_ssid + "&id=" + m_sKitID + "\">");
		return;
	}

	if(Request.Form["code"] != null && Request.Form["code"] != "") //add item
	{
		string code = Request.Form["code"];
		if(DoAddItem(code, "1", "0"))
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "\">");
		return;
	}

	string cmd = Request.Form["cmd"];

	if(cmd == "Browse")
	{
		CheckShoppingCart();
		EmptyCart();
		Session[m_sCompanyName + "_ordering"] = true;
		Session[m_sCompanyName + "_salestype"] = m_sKitTerm;
		Session[m_sCompanyName + "_salesurl"] = "kit.aspx?ssid=" + m_ssid + "&t=readcart&id=" + m_sKitID;
		Response.Redirect("c.aspx?ssid=" + m_ssid + "&t=" + m_sKitTerm);
		return;
	}
	else if(cmd == "Save")
	{
		if(DoSaveKit())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "\">");
		return;
	}
	else if(cmd != null && cmd.IndexOf("Copy") >= 0)
	{
		string old_id = m_sKitID;
		m_sKitID = ""; //so DoSaveKit will create a new kit
		if(DoSaveKit())
		{
			//copy img
			string sPicFile = "";
			string vpath = GetRootPath();
			vpath += "/pk/";
			string path = vpath;
			sPicFile = path + old_id + ".gif";
			string newfile = path + m_sKitID;
			bool bHasLocal = File.Exists(Server.MapPath(sPicFile));
			if(!bHasLocal)
			{
				sPicFile = path + old_id + ".jpg";
				bHasLocal = File.Exists(Server.MapPath(sPicFile));
				newfile += ".jpg";
			}
			else
				newfile += ".gif";

			if(bHasLocal)
			{
				if(!File.Exists(Server.MapPath(newfile)))
					File.Copy(Server.MapPath(sPicFile), Server.MapPath(newfile)); //copy image
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "\">");
		}
		return;
	}
	else if(cmd != null && cmd.IndexOf("Delete") >= 0)
	{
		if(DoDeleteKit())
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><br><br><center><h3>" + m_sKitTerm + " #" + m_sKitID + " deleted.</h3>");
			Response.Write("<br><br><a href=close.htm class=o><font size=+1><b>Close Window</b></font></a>");
			m_sKitID = "";
		}
		return;
	}
	if(Request.QueryString["new"] == "1")
	{
		EmptyItemTable();
	}

	if(t == "del")
	{
		if(DoRemoveItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "\">");
			return;
		}
	}
	else if(t == "readcart")
	{
		for(int i=0; i<dtCart.Rows.Count; i++)
		{
			string code = dtCart.Rows[i]["code"].ToString();
			string qty = dtCart.Rows[i]["quantity"].ToString();
			DoAddItem(code, qty, "0");
		}
		EmptyCart();
		PrintEditContentPage();
		return;
	}
	else if(t == "ec")
	{
		PrintEditContentPage();
		return;
	}
	else if(t == "addcross")
	{
		if(KitAddCross())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "\">");
		return;
	}
	else if(t == "delete")
	{
		if(KitDeleteCross())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "\">");
		return;
	}

	PrintNewForm();
}

bool DoAddItem(string code, string qty, string seq)
{
	if(code == null || code == "")
		return false;

	if(!TSIsDigit(code))
		return false;

	DataRow drp = null;
	GetProduct(code, ref drp);
	if(drp == null)
	{
		Response.Write("<font color=red size=+1><b>Item not found, code=" + code + "</b></font>");
		Response.Write(" <a href=kit_ci.aspx?code=" + code + " class=o><font size=+1>Change Item</a>");
		return false;
	}

	for(int i=0; i<dtKit.Rows.Count; i++)
	{
		string o_code = dtKit.Rows[i]["code"].ToString();
		if(o_code == code)
		{
			dtKit.Rows[i].AcceptChanges();
			dtKit.Rows[i].BeginEdit();
			dtKit.Rows[i]["qty"] = MyIntParse(dtKit.Rows[i]["qty"].ToString()) + 1;
			dtKit.Rows[i].EndEdit();
			return true;
		}
	}
	
	double dseq = MyDoubleParse(seq);
	if(dtKit.Rows.Count <= 0)
	{
		dseq = 1;
	}
	else if(dseq == 0) //unkonwn
	{
		DataRow[] drs = dtKit.Select("", "seq");
		string last_seq = drs[dtKit.Rows.Count - 1]["seq"].ToString();
		dseq = MyDoubleParse(last_seq) + 1; //put it to the end
	}

	DataRow dr = dtKit.NewRow();
	dr["kid"] = "";
	dr["id"] = "";
	dr["seq"] = dseq;
	dr["code"] = code;
	dr["name"] = drp["name"].ToString();
	dr["price"] = GetSalesPriceForDealer(code, qty, "1", "0"); //all level one price;
	dr["rate"] = "0";
	dr["qty"] = qty;
	dr["note"] = "";
	dtKit.Rows.Add(dr);
	return true;
}

bool DoRemoveItem()
{
	string s = Request.QueryString["code"];
	if(s == null || s == "")
		return true;
	int n = MyIntParse(s);
	for(int i=0; i<dtKit.Rows.Count; i++)
	{
		string code = dtKit.Rows[i]["code"].ToString();
		if(code == s)
		{
			dtKit.Rows.RemoveAt(i);
			break;
		}
	}
	return true;
}

string PrintPTable(bool bListOnly)
{
	StringBuilder sb = new StringBuilder();

	sb.Append("<table width=100% align=center valign=center cellspacing=3 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:3px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	sb.Append("<th>Code</th>");
	sb.Append("<th>Name</th>");
	sb.Append("<th>Cost</th>");
	sb.Append("<th>Price</th>");
	sb.Append("<th>Quantity</th>");
	sb.Append("<th>Total</th>");
	if(!bListOnly)
	{
		sb.Append("<th>Cache ");
		sb.Append("<a href=kit.aspx?ssid=" + m_ssid + "&h=cache target=_blank class=o><font color=white><b>?</b></font></a>");
		sb.Append("</th>");
	}
	sb.Append("</tr>");
	
	double dseq = 0; //current sequence
	double sequ1 = 0; //previous previous menu's sequence
	double sequ = 0; //previous menu's sequence
	double seqd = 0; //next menu's sequence
	double seqd1 = 0; //next next menu's sequence
	double seqn = 0; //new sequence number (calculated)

	m_dTotal = 0;
	double dTotalCost = 0;
	DataRow[] drs = dtKit.Select("", "seq");
	int rows = dtKit.Rows.Count;
	for(int i=0; i<dtKit.Rows.Count; i++)
	{
		DataRow dr = drs[i];
		if(i < rows - 1 )
			seqd = MyDoubleParse(drs[i+1]["seq"].ToString()); //next seq number
		else
			seqd = 0;
		if(i < rows - 2 )
			seqd1 = MyDoubleParse(drs[i+2]["seq"].ToString()); //next next seq number 
		else
			seqd1 = 0;

		string seq = dr["seq"].ToString();
		string kid = dr["kid"].ToString();
		string id = dr["id"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string price = dr["price"].ToString();
		string rate = dr["rate"].ToString();
		string qty = dr["qty"].ToString();
		string note = dr["note"].ToString();
		double dPrice = MyDoubleParse(price);
		int nQty = MyIntParse(qty);
		double dCost = GetSupplierPrice(code);
		double dQPrice = dPrice * nQty;
		m_dTotal += dQPrice;
		dTotalCost += dCost * nQty;
	
		dseq = MyDoubleParse(seq);

		sb.Append("<input type=hidden name=code" + i + " value=" + code + ">"); //for seq

		sb.Append("<tr>");
		sb.Append("<td nowrap>" + code);
		sb.Append(" <a href=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "&t=del&code=" + code + " class=o title='Remove'>X</a>");
		sb.Append(" <a href=kit_ci.aspx?code=" + code + " class=o title='Change Item'>C</a>");
		sb.Append("</td>");
		sb.Append("<td>");
		if(!bListOnly)
		{
//			if(i>0 && i<rows-1)
			{
				seqn = sequ - (sequ - sequ1) / 2;
				if(i == 0) //first row
					seqn = dseq; //no changes
				sb.Append("<a href=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "&code=" + code);
				sb.Append("&t=ec&seq=" + seqn.ToString() + " class=o title='Move Up'>UP</a> ");

				if(seqd1 != 0)
					seqn = seqd + (seqd1 - seqd) / 2;
				else
					seqn = seqd + 1;
				if(i == rows - 1) //last row
					seqn = dseq; //no changes
				sb.Append("<a href=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID + "&code=" + code);
				sb.Append("&t=ec&seq=" + seqn.ToString() + " class=o title='Move Down'>DW</a> ");
			}
//			sb.Append(" seq=" + seq + ", seqd=" + seqd + ", seqd1=" + seqd1);
			sequ1 = sequ;
			sequ = dseq;
		}
		sb.Append(name + "</td>");
		sb.Append("<td align=right>" + dCost.ToString("c") + "</td>");
		sb.Append("<td align=right>" + dPrice.ToString("c") + "</td>");
//		sb.Append("<td>" + rate + "</td>");
//		sb.Append("<td>" + dPrice_p.ToString("c") + "</td>");
		sb.Append("<td align=center><input type=text size=1 style=text-align:right name=qty" + code + " value=" + qty + "></td>");
		sb.Append("<td align=right>" + dQPrice.ToString("c") + "</td>");
		if(!bListOnly)
		{
			sb.Append("<td align=center>");
			if(!KitAlreadyInCache(code))
			{
				sb.Append("<a href=kit.aspx?ssid=" + m_ssid + "&id=" + m_sKitID);
				sb.Append("&t=addcache&code=" + code + " class=o>Add</a>");
			}
			sb.Append("</td>");
		}
		sb.Append("</tr>");
	}

	m_dTotal = Math.Round(m_dTotal, 2);
	if(m_sKitID == "")
	{
		m_dKitPrice = m_dTotal;
		m_dKitPrice = Math.Round(m_dKitPrice, 2);
	}

	sb.Append("<tr>");
	sb.Append("<td>");
	sb.Append("&nbsp;</td>");
	sb.Append("<td colspan=");
	if(!bListOnly)
		sb.Append("3");
	else
		sb.Append("2");
	sb.Append(" align=right>&nbsp;");
	if(bListOnly)
	{
		sb.Append("<b>Total Cost : " + dTotalCost.ToString("c") + "</b>");
		sb.Append("</td><td align=right colspan=2><b>Total : </b></td>");
		sb.Append("<td align=right><b>" + m_dTotal.ToString("c") + "</b></td></tr>");
		sb.Append("</table>");
		return sb.ToString();
	}

	sb.Append("<b>Code : </b><input type=text size=10 name=code>");
	sb.Append("<input type=submit name=cmd value='Add' " + Session["button_style"] + ">");
	sb.Append("<input type=submit name=cmd value='Browse' " + Session["button_style"] + ">");
	
	sb.Append("&nbsp;&nbsp;&nbsp;&nbsp; <b>Total Cost : " + dTotalCost.ToString("c") + "</b>");
	sb.Append("</td><td align=right><b>Total : </b></td>");
	sb.Append("<td align=right><b>" + m_dTotal.ToString("c") + "</b></td></tr>");

//	sb.Append("<tr><td colspan=5 align=right>");
//	sb.Append("<input type=submit name=cmd value='Save' " + Session["button_style"] + ">");
//	sb.Append("</td></tr>");

	sb.Append("</table>");
	return sb.ToString();
}

double GetSupplierPrice(string code)
{
//	DEBUG("code = ", code);
	string cost = "0";
	string sc = " SELECT p.supplier_price FROM product p JOIN code_relations c ON c.code = p.code ";
	sc += " WHERE p.code = "+ code +" AND c.code = "+ code;
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "Cost");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return 1;
	}
	for(int i=0; i<dst.Tables["Cost"].Rows.Count; i++)
	{
		cost = dst.Tables["Cost"].Rows[i]["supplier_price"].ToString();
	}
//DEBUG("cost = ", cost);
	return double.Parse(cost);
}
void PrintEditContentPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h3>Edit " + m_sKitTerm + " Content");
	if(m_sKitID == "")
		Response.Write(" - <font color=red>New</b>");
	Response.Write("</h3>");
	Response.Write("<form action=kit.aspx?t=" + Request.QueryString["t"] + "&ssid=" + m_ssid + "&id=" + m_sKitID + " method=post>");

	Response.Write(PrintPTable(false));

	Response.Write("<input type=submit name=cmd value=OK " + Session["button_style"] + ">");
	Response.Write("</form>");

	PrintItemCache();
}

void PrintNewForm()
{
	PrintAdminHeader();
	PrintAdminMenu();

	string title = "New " + m_sKitTerm;
	if(m_sKitID != "")
		title = m_sKitTerm + " #" + m_sKitID;

	Response.Write("<br><center><h3>" + title + "</h3>");

	Response.Write("<form action=kit.aspx?ssid=" + m_ssid + "&t=save&id=" + m_sKitID + " method=post>");

	Response.Write("<table width=70% align=center valign=center cellspacing=3 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=2><font size=+1>Content List </font>");
	Response.Write("<input type=button onclick=window.location=('kit.aspx?ssid=" + m_ssid + "&t=ec&id=" + m_sKitID + "') ");
	Response.Write(" value='Edit Content' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2>");
	Response.Write(PrintPTable(true));
	Response.Write("</td></tr>");

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("&nbsp;");
//	Response.Write("<input type=submit name=cmd value='Edit Item' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	//name
	Response.Write("<tr><td nowrap><b>" + m_sKitTerm + " Name : </b></td>");
	Response.Write("<td><input type=text size=70 name=name value=\"" + m_sKitName + "\"></td>");
	Response.Write("</tr>");

	Response.Write("<tr><td><b>Category</b></td>");
	Response.Write("<td>");
	Response.Write("<input type=text size=10 name=cat value=\"" + m_sKitTerm + "\">");
	Response.Write("</td></tr>");

	//s_cat
	Response.Write("<tr><td><b>s_cat</b></td>");
	Response.Write("<td>");
	Response.Write(PrintKitSCat(m_scat));
	Response.Write("<input type=text size=10 name=s_cat_new>");
	Response.Write("</td></tr>");

	//ss_cat
	Response.Write("<tr><td><b>ss_cat</b></td>");
	Response.Write("<td>");
	Response.Write(PrintKitSSCat(m_sscat));
	Response.Write("<input type=text size=10 name=ss_cat_new>");
	Response.Write("</td></tr>");

	//price
	Response.Write("<tr><td nowrap><b>" + m_sKitTerm + " Price : </b></td>");
	Response.Write("<td>");
	Response.Write("<input type=text size=5 style=text-align:right name=price value=");
	if(m_dKitItemLastTotal != m_dTotal) //modified content?
		Response.Write(m_dTotal);
	else
		Response.Write(m_dKitPrice);
	Response.Write(">");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=checkbox name=auto_update_price ");
	if(m_bKitAutoUpdatePrice)
		Response.Write(" checked");
	Response.Write(">Auto Update Price ");
	Response.Write("<input type=button onclick=");
	Response.Write("\"javascript:viewcard_window=window.open('kit.aspx?ssid=" + m_ssid + "&h=aup','','width=400,height=300');");
	Response.Write("\" value='?' " + Session["button_style"] + ">");
	Response.Write(" &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp; ");
	
	//special
	Response.Write("<input type=checkbox name=special ");
	if(m_bKitSpecial)
		Response.Write(" checked");
	Response.Write(">Show In Special List ");
	Response.Write("<input type=button onclick=");
	Response.Write("\"javascript:viewcard_window=window.open('kit.aspx?ssid=" + m_ssid + "&h=special','','width=400,height=300');");
	Response.Write("\" value='?' " + Session["button_style"] + ">");
	Response.Write(" &nbsp&nbsp&nbsp&nbsp&nbsp&nbsp; ");

	Response.Write("</td></tr>");

	//inactive
	Response.Write("<tr><td nowrap><b>Inactive : </b></td>");
	Response.Write("<td><input type=checkbox name=inactive ");
	if(m_bKitInactive)
		Response.Write(" checked");
	Response.Write("> &nbsp; <i><font color=red>*</font> ( keep inactive until everything is set)<i></td>");
	Response.Write("</tr>");

	//hot kit
	Response.Write("<tr><td nowrap><b>" + m_sKitTerm + " Hot : </b></td>");
	Response.Write("<td><input type=checkbox name=hot ");
	if(m_bKitHot)
		Response.Write(" checked");
	Response.Write("> &nbsp; </td>");
	Response.Write("</tr>");

	//details
	Response.Write("<tr><td nowrap><b>Description : </b></td>");
	Response.Write("<td><textarea name=details cols=60 rows=5>");
	Response.Write(m_sKitDetails);
	Response.Write("</textarea></td>");
	Response.Write("</tr>");

	//warranty
	Response.Write("<tr><td nowrap><b>Warranty : </b></td>");
	Response.Write("<td><textarea name=warranty cols=60 rows=2>");
	Response.Write(m_sKitWarranty);
	Response.Write("</textarea></td>");
	Response.Write("</tr>");

	//buttons
	Response.Write("<tr><td>");
	Response.Write("<input type=submit name=cmd value='Save' " + Session["button_style"] + ">");
	Response.Write("</td><td align=right>");
	if(m_sKitID != "")
	{
		Response.Write("<input type=submit name=cmd value='Copy " + m_sKitTerm + "' class=b>");
		Response.Write("<input type=submit name=cmd value='Delete " + m_sKitTerm + "' ");
		Response.Write(" class=b onclick=\"return window.confirm('Are you sure to delete?');\">");
	}
	Response.Write(" <a href=addpic.aspx?kit=1&code=" + m_sKitID);
	Response.Write("&name=" + HttpUtility.UrlEncode(m_sKitName) + " class=o target=_blank>Edit Photo</a> &nbsp; ");
	Response.Write("<input type=submit name=cmd value='Save' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");

	Response.Write(KitPrintAdvanceSettings());
	Response.Write("<br><br>");
	PrintAdminFooter();
}

bool DoDeleteKit()
{
	if(m_sKitID == "")
		return false;

	string sc = " DELETE FROM kit WHERE id = " + m_sKitID;
	sc += " DELETE FROM kit_item WHERE id = " + m_sKitID;
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

bool DoSaveKit()
{
	if(dtKit.Rows.Count <= 0)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Error no item found.");
		return false;
	}

	string sc = "";
	if(m_sKitID == "") //new kit
	{
		sc = "BEGIN TRANSACTION ";
		sc += " INSERT INTO kit (name) VALUES ('New Kit') ";
		sc += " SELECT IDENT_CURRENT('kit') AS id ";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "id") != 1)
				return false;
			m_sKitID = ds.Tables["id"].Rows[0]["id"].ToString();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	else
	{
		//delete old item list if not new kit
		sc = " DELETE FROM kit_item WHERE id = " + m_sKitID;
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

	sc = "";
	double dTotal = 0;
	for(int i=0; i<dtKit.Rows.Count; i++)
	{
		DataRow dr = dtKit.Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string seq = dr["seq"].ToString();
		string price = dr["price"].ToString();
//		string rate = dr["rate"].ToString();
		string qty = dr["qty"].ToString();
		string note = dr["note"].ToString();
		if(note == null)
			note = "";

		double dPrice = MyDoubleParse(price);
//		double dRate = MyDoubleParse(rate);
		int nQty = MyIntParse(qty);

		dTotal += dPrice * nQty;

		sc += " INSERT INTO kit_item (id, code, qty, note, seq) VALUES( ";
		sc += m_sKitID;
		sc += ", " + code;
//		sc += ", " + dRate;
		sc += ", " + qty;
		sc += ", '" + EncodeQuote(note) + "' ";
		sc += ", " + seq;
		sc += ") ";
	}
	if(sc != "")
	{
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

	//do update kit
	string p_name = Request.Form["name"];
	string cat = Request.Form["cat"];
	string cat_new = Request.Form["cat_new"];
	string s_cat = Request.Form["s_cat"];
	string s_cat_new = Request.Form["s_cat_new"];
	string ss_cat = Request.Form["ss_cat"];
	string ss_cat_new = Request.Form["ss_cat_new"];
	string details = Request.Form["details"];
	string warranty = Request.Form["warranty"];
	if(details == null)
		details = "";
	if(warranty == null)
		warranty = "";

	if(cat_new != null && cat_new != "")
		cat = cat_new;
	if(s_cat_new != null && s_cat_new != "")
		s_cat = s_cat_new;
	if(ss_cat_new != null && ss_cat_new != "")
		ss_cat = ss_cat_new;

	string p_price = Request.Form["price"];
	double dPPrice = MyDoubleParse(p_price);

	string inactive = "0";
	if(Request.Form["inactive"] == "on")
		inactive = "1";

	string hot = "0";
	if(Request.Form["hot"] == "on")
		hot = "1";

	string auto_update_price = "0";
	if(Request.Form["auto_update_price"] == "on")
		auto_update_price = "1";

	double dRate = dPPrice / dTotal;
	sc = " UPDATE kit SET ";
	sc += " name = '" + EncodeQuote(p_name) + "' ";
	sc += ", s_cat = '" + EncodeQuote(s_cat) + "' ";
	sc += ", ss_cat = '" + EncodeQuote(ss_cat) + "' ";
	sc += ", price = " + Math.Round(dPPrice, 2);
	sc += ", rate = " + dRate;
	sc += ", last_item_total = " + Math.Round(dTotal, 2);
	sc += ", inactive = " + inactive;
	sc += ", hot = " + hot;
	sc += ", auto_update_price = " + auto_update_price;
	sc += ", details='" + EncodeQuote(details) + "' ";
	sc += ", warranty='" + EncodeQuote(warranty) + "' ";
	sc += " WHERE id = " + m_sKitID;
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

	if(cat != m_sKitTerm)
		SetSiteSettings("package_bundle_kit_name", cat);
	
	//special
	KitCheckSpecial();
	return true;	
}

bool KitCheckSpecial()
{
	bool bSpecial = false;
	if(Request.Form["special"] == "on")
		bSpecial = true;
//	if(bSpecial == m_bKitSpecial)
//		return true;
	string sc = "";
	if(!bSpecial)
	{
		sc = " DELETE FROM specials_kit WHERE code = " + m_sKitID;
	}
	else
	{
		sc = " IF NOT EXISTS (SELECT * FROM specials_kit WHERE code=" + m_sKitID + ") ";
		sc += " INSERT INTO specials_kit (code) VALUES(" + m_sKitID + ") ";
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

string PrintKitSCat(string sCurrent)
{
	string sc = " SELECT DISTINCT s_cat FROM kit ORDER BY s_cat";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "s_cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	string s = "<select name=s_cat>";
	
	for(int i=0; i<ds.Tables["s_cat"].Rows.Count; i++)
	{
		string c = ds.Tables["s_cat"].Rows[i]["s_cat"].ToString();
		s += "<option value=\"" + c + "\" ";
		if(c == sCurrent)
			s += " selected";
		s += ">" + c + "</option>"; 
	}
	s += "</select>";
	return s;
}

string PrintKitSSCat(string sCurrent)
{
	string sc = " SELECT DISTINCT ss_cat FROM kit ORDER BY ss_cat";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "ss_cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	string s = "<select name=ss_cat>";
	
	for(int i=0; i<ds.Tables["ss_cat"].Rows.Count; i++)
	{
		string c = ds.Tables["ss_cat"].Rows[i]["ss_cat"].ToString();
		s += "<option value=\"" + c + "\" ";
		if(c == sCurrent)
			s += " selected";
		s += ">" + c + "</option>"; 
	}
	s += "</select>";
	return s;
}

void EmptyItemTable()
{
	for(int i=dtKit.Rows.Count-1; i>=0; i--)
		dtKit.Rows.RemoveAt(i);
}

bool RestoreKit()
{
	string sc = " SELECT * FROM kit WHERE id = " + m_sKitID;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "kit") != 1)
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h3>" + m_sKitTerm + " #" + m_sKitID + " not found.");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " SELECT * FROM specials_kit WHERE code=" + m_sKitID;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "kit") > 0)
		{
			m_bKitSpecial = true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	DataRow dr = ds.Tables["kit"].Rows[0];
	m_sKitName = dr["name"].ToString();
	m_scat = dr["s_cat"].ToString();
	m_sscat = dr["ss_cat"].ToString();
	m_sKitDetails = dr["details"].ToString();
	m_sKitWarranty = dr["warranty"].ToString();
	m_dKitItemLastTotal = MyDoubleParse(dr["last_item_total"].ToString());
	m_dKitPrice = MyDoubleParse(dr["price"].ToString());
	m_bKitInactive = MyBooleanParse(dr["inactive"].ToString());
	m_bKitHot = MyBooleanParse(dr["hot"].ToString());
	m_bKitAutoUpdatePrice = MyBooleanParse(dr["auto_update_price"].ToString());

	if(Request.QueryString["rt"] == "keepcontent") //return from edit content, don't restore content
		return true;

	sc = " SELECT * FROM kit_item WHERE id = " + m_sKitID + " ORDER BY seq ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "item");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	double dSeqOld = 0;
	double dSeq = 0;
	EmptyItemTable();
	for(int i=0; i<ds.Tables["item"].Rows.Count; i++)
	{
		dr = ds.Tables["item"].Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();

		dSeq = MyDoubleParse(dr["seq"].ToString());
		if(dSeq <= dSeqOld)
			dSeq = dSeqOld + 1;
		DoAddItem(code, qty, dSeq.ToString());
		dSeqOld = dSeq;
	}
	return true;
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
	else if(key == "special")
	{
		s += "Show As Special</h3></center>";
		s += "Add this " + m_sKitTerm + " to special list, same as normal items. ";
		s += "<br><br>";
	}
	else if(key == "cr") //cross reference
	{
		s += "Cross Reference</h3></center>";
		s += "List this " + m_sKitTerm + " more than one place(category), same as normal items. ";
		s += "<br><br>";
	}
	else if(key == "cache") //cross reference
	{
		s += "Add to Speed Add Item Cache</h3></center>";
		s += "Will be list in Speed Add Item table once added to cache, save you from browsing thick categories. <br><br>";
		s += "<b>P.S. </b>Items will be shown in order, by their category and sub categories.<br><br>";
		s += "<br><br>";
	}
	Response.Write(s);
	Response.Write("<br><br><center><a href=close.htm class=o><font size=+1><b>Close Window</b></font></a>");
	Response.Write("</td></tr></table>");
}

bool DoMoveItem()
{
	string code = Request.QueryString["code"];
	double dseq = MyDoubleParse(Request.QueryString["seq"]);

	int items = dtKit.Rows.Count;
	for(int i=0; i<items; i++)
	{
		DataRow dr = dtKit.Rows[i];
		if(dr["code"].ToString() == code)
		{
			dtKit.AcceptChanges();
			dr.BeginEdit();
			dr["seq"] = dseq;
			dr.EndEdit();
			dtKit.AcceptChanges();
			break;
		}
	}
	return true;
}

void DoSaveQty()
{
	dtKit.AcceptChanges();
	int items = dtKit.Rows.Count;
	for(int i=0; i<items; i++)
	{
		if(Request.Form["code"+i] == null)
			break;

		string code = Request.Form["code"+i];
		for(int j=0; j<items; j++)
		{
			DataRow dr = dtKit.Rows[j];
			if(dr["code"].ToString() == code)
			{
				dr.BeginEdit();
//				dr["seq"] = Request.Form["seq" + code];
				dr["qty"] = Request.Form["qty" + code];
				dr.EndEdit();
				break;
			}
		}
	}
	dtKit.AcceptChanges();
}

string KitPrintAdvanceSettings()
{
	if(m_sKitID == "")
		return "";

	if(!GetCrossReferences())
		return "";

//	Response.Write("<table width=100% align=center valign=center cellspacing=3 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//
//	Response.Write("<tr><td><b><br>Advance Settings</b></td></tr>");
//	Response.Write("<tr><td><b>Cross Reference</b><i>(show in other category also)</i></td></tr>");

	return PrintCrossReferences();
}

string PrintCrossReferences()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<form name=form2 action=kit.aspx?ssid=" + m_ssid + "&t=addcross&id=" + m_sKitID + " method=post>");
	sb.Append("<table width=70%  cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	sb.Append("<tr><td colspan=4><font size=+1>Cross Reference</font> ");
	sb.Append("<input type=button onclick=");
	sb.Append("\"javascript:viewcard_window=window.open('?h=cr','','width=400,height=300');");
	sb.Append("\" value='?' " + Session["button_style"] + ">");
	sb.Append("</td></tr>");

	sb.Append("<tr height=10 style='color:white;background-color:#666696;font-weight:bold;'><td>CAT</td><td>S_CAT</td><td>SS_CAT</td><td>ACTION</td></tr>");
	bool alterColor = true;
	for(int i=0; i<dst.Tables["cross"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["cross"].Rows[i];
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		alterColor = !alterColor;
		sb.Append("<tr");
		if(alterColor)
			sb.Append(" bgcolor=#EEEEEE");
		sb.Append("><td>");
		sb.Append(cat);
		sb.Append("</td><td>");
		sb.Append(s_cat);
		sb.Append("</td><td>");
		sb.Append(ss_cat);
		sb.Append("</td><td align=right><a href=?t=delete&id=" + m_sKitID + "&c=");
		sb.Append(HttpUtility.UrlEncode(cat));
		sb.Append("&s=");
		sb.Append(HttpUtility.UrlEncode(s_cat));
		sb.Append("&ss=");
		sb.Append(HttpUtility.UrlEncode(ss_cat));
		sb.Append(">DELETE</a></td></tr>");
	}
	sb.Append("<tr><td>&nbsp;</td></tr><tr><td>");
	sb.Append(PrintSelectionRowForCross("cat", m_co));
	sb.Append("</td><td>");
	sb.Append(PrintSelectionRowForCross("s_cat", m_so));
	sb.Append("</td><td>");
	sb.Append(PrintSelectionRowForCross("ss_cat", ""));
	sb.Append("</td><td align=right><input type=submit value=' Add ' " + Session["button_style"] + "></td></tr>");
	sb.Append("</table></form>");
	return sb.ToString();
}

string PrintSelectionRowForCross(string sName, string sValue)
{
	bool bMatch = false;
	string str = "";
	StringBuilder sb = new StringBuilder();
	sb.Append("\r\n<select name=" + sName);
	if(sName != "ss_cat")
	{
		sb.Append(" onchange=\"window.location=('?id=" + m_sKitID);
		sb.Append("&r=" + DateTime.Now.ToOADate());
		if(sName == "cat")
			sb.Append("&so=-1&co='");
		else if(sName == "s_cat")
			sb.Append("&co="+HttpUtility.UrlEncode(m_co) + "&so='");
		sb.Append("+this.options[this.selectedIndex].value)\"");
	}
	sb.Append(">");
	for(int j=0; j<dsAEV.Tables[sName].Rows.Count; j++)
	{
		str = dsAEV.Tables[sName].Rows[j][0].ToString();
		sb.Append("<option value='" + str + "'");
		if(str == sValue)
		{
			bMatch = true;
			sb.Append(" selected");
		}
		if(!bMatch)
		{
			if(sName == "cat" && m_co == "-1")
			{
				bMatch = true;
				sb.Append(" selected");
			}
			else if(sName == "s_cat" && m_so == "-1")
			{
				if(str != "")
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
			else if(sName == "ss_cat")
			{
				if(str != "")
				{
					bMatch = true;
					sb.Append(" selected");
				}
			}
		}
		sb.Append(">"+str+"</option>");
	}
	if(!bMatch)
		sb.Append("<option value='" + sValue + "' selected>" + sValue + "</option></select>");
//	if(sName == "ss_cat")
//		sb.Append("<input type=text size=10 name=" + sName + "_new>");
	return sb.ToString();
}

bool GetCrossReferences()
{
	if(!ECATGetAllExistsValues("cat", "cat<>'Brands' ORDER BY cat", false))
		return false;

	if(dsAEV.Tables["cat"].Rows.Count <= 0)
		return true;

	if(m_sKitID == "")
		return true;

	int rows = 0;
	string sc = "SELECT id, cat, s_cat, ss_cat FROM cat_cross_kit WHERE code=" + m_sKitID + " ORDER BY cat, s_cat, ss_cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cross");
//DEBUG("sc=", sc);
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid object name 'cat_cross_kit'") >= 0)
		{
			myConnection.Close(); //close it first
			if(!kit_updateDatabase("")) //compatible with old version
				return false;

			//db updated, try again
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				rows = myAdapter.Fill(dst, "cross");
			}
			catch(Exception e1)
			{
				ShowExp(sc, e1);
				return false;
			}
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}
	}
//	if(rows <= 0)
//		return true;

	if(m_co == "-1")
		m_co = dsAEV.Tables["cat"].Rows[0][0].ToString(); 
	sc = "cat='" +  m_co + "' ORDER BY s_cat";
//DEBUG("sc=", sc);
	if(!ECATGetAllExistsValues("s_cat", sc, false))
		return false;

	if(m_so == "-1")
		m_so = dsAEV.Tables["s_cat"].Rows[0][0].ToString();
	sc = "cat='" + m_co + "' AND s_cat='" + m_so + "' ORDER BY ss_cat";
	if(!ECATGetAllExistsValues("ss_cat", sc, false))
		return false;

	return true;
}

bool kit_updateDatabase(string tableName) //compatible with old version
{
	string sc = "";
	if(tableName == "")
	{
		sc += @"
CREATE TABLE [dbo].[cat_cross_kit] (
	[id] [int] IDENTITY (1, 1) NOT FOR REPLICATION  NOT NULL ,
	[code] [int] NOT NULL ,
	[cat] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL ,
	[s_cat] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL ,
	[ss_cat] [varchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL 
) ON [PRIMARY]

ALTER TABLE [dbo].[cat_cross_kit] WITH NOCHECK ADD 
	CONSTRAINT [PK_cat_cross_kit] PRIMARY KEY  CLUSTERED 
	(
		[id]
	)  ON [PRIMARY] 
		";
	}
	else if(tableName == "kit_item_cache")
	{
		sc += @"
CREATE TABLE [dbo].[kit_item_cache] (
	[id] [int] IDENTITY (1, 1) NOT FOR REPLICATION  NOT NULL ,
	[code] [int] NOT NULL 
) ON [PRIMARY]

ALTER TABLE [dbo].[kit_item_cache] WITH NOCHECK ADD 
	CONSTRAINT [PK_kit_item_cache] PRIMARY KEY  CLUSTERED 
	(
		[id]
	)  ON [PRIMARY] 
		";
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

bool KitAddCross()
{
	string cat = EncodeQuote(Request.Form["cat"]);
	string s_cat = EncodeQuote(Request.Form["s_cat"]);
	string ss_cat = EncodeQuote(Request.Form["ss_cat"]);
	if(KitCrossExists(cat, s_cat, ss_cat))
		return true;
	StringBuilder sb = new StringBuilder();
	sb.Append("INSERT INTO cat_cross_kit (cat, s_cat, ss_cat, code) VALUES('");
	sb.Append(cat);
	sb.Append("', '");
	sb.Append(s_cat);
	sb.Append("', '");
	sb.Append(ss_cat);
	sb.Append("', ");
	sb.Append(m_sKitID);
	sb.Append(")");
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
	return true;
}

bool KitDeleteCross()
{
	string cat = EncodeQuote(Request.QueryString["c"]);
	string s_cat = EncodeQuote(Request.QueryString["s"]);
	string ss_cat = EncodeQuote(Request.QueryString["ss"]);
	StringBuilder sb = new StringBuilder();
	sb.Append("DELETE FROM cat_cross_kit WHERE code=" + m_sKitID + " AND cat='");
	sb.Append(cat);
	sb.Append("' AND s_cat='");
	sb.Append(s_cat);
	sb.Append("' AND ss_cat='");
	sb.Append(ss_cat);
	sb.Append("'");
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
	return true;
}

bool KitCrossExists(string cat, string s_cat, string ss_cat)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("SELECT * FROM cat_cross_kit WHERE cat='");
	sb.Append(cat);
	sb.Append("' AND s_cat='");
	sb.Append(s_cat);
	sb.Append("' AND ss_cat='");
	sb.Append(ss_cat);
	sb.Append("' AND code=");
	sb.Append(m_sKitID);
	try
	{
		DataSet dsex = new DataSet();
		myAdapter = new SqlDataAdapter(sb.ToString(), myConnection);
		if(myAdapter.Fill(dsex) > 0)
			return true;

	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return true; //return true to stop adding
	}
	return false;
}

/////////////////////////////////////////////////////////////////////////////////////
//speed add cache functions
bool KitAlreadyInCache(string code)
{
	string sc = "SELECT * FROM kit_item_cache WHERE code=" + code;
	try
	{
		DataSet dsf = new DataSet();
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsf) > 0)
			return true;

	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid object name 'kit_item_cache'") >= 0)
		{
			myConnection.Close(); //close it first
			if(!kit_updateDatabase("kit_item_cache")) //compatible with old version
				return false;

			//db updated, try again
			try
			{
				DataSet dsf = new DataSet();
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dsf) > 0)
					return true;
			}
			catch(Exception e1)
			{
				ShowExp(sc, e1);
				return false;
			}
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return false;
}

bool PrintItemCache()
{
	DataSet dsf = new DataSet();
	int rows = 0;

	string sc = " SELECT c.cat, c.s_cat, c.ss_cat, c.code, c.name ";
	sc += " FROM code_relations c JOIN kit_item_cache k ON c.code=k.code ";
	sc += " ORDER BY c.cat, c.s_cat, c.ss_cat, c.name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsf);
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid object name 'kit_item_cache'") >= 0)
		{
			myConnection.Close(); //close it first
			if(!kit_updateDatabase("kit_item_cache")) //compatible with old version
				return false;

			//db updated, try again
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				rows = myAdapter.Fill(dsf);
			}
			catch(Exception e1)
			{
				ShowExp(sc, e1);
				return false;
			}
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}
	}

	Response.Write("<center><b>Speed Add Item</b>");
	if(rows <= 0)
	{
		Response.Write("<h5>No items in cache</h5>");
		return true;
	}

	Response.Write("<table align=center valign=center cellspacing=3 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:3px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Group</th>");
	Response.Write("<th>Code</th>");
	Response.Write("<th>Desc</th>");
	Response.Write("<th>Action</th>");
	Response.Write("</tr>");

	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dsf.Tables[0].Rows[i];
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td>" + cat + "->" + s_cat + "->" + ss_cat + "</td>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=right>");
		Response.Write("<a href=kit.aspx?t=add&code=" + code + "&ssid=" + m_ssid + "&id=" + m_sKitID + " class=o>Add</a> ");
		Response.Write("<a href=kit.aspx?t=rc&code=" + code + "&ssid=" + m_ssid + "&id=" + m_sKitID + " class=o>Remove</a> ");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	return true;
}

bool DoAddItemToCache(string code)
{
	if(code == null || code == "")
	{
		Response.Write("Invalid Code");
		return false;
	}
	string sc = " IF NOT EXISTS (SELECT * FROM kit_item_cache WHERE code=" + code + ") ";
	sc += " INSERT INTO kit_item_cache (code) VALUES (" + code + ") ";
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

bool DoRemoveItemFromCache(string code)
{
	if(code == null || code == "")
		return true;

	string sc = " DELETE FROM kit_item_cache WHERE code=" + code;
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

</script>

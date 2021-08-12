<script runat=server>

int MaxRows = 20;
int MaxCols = 5;

void MPage_Load()
{
	RememberLastPage();

	PrintHeaderAndMenu();
	DataSet ds = new DataSet();
	
	string menu = Request.QueryString["m"];
	string type = Request.QueryString["t"];

	if(string.Compare(menu, "brands", true) == 0)
	{
		type = "more";
		MaxRows = 20;
	}

	int rows = 0;
	string sc = "";
	sc = "SELECT DISTINCT s_cat";
//	if(type != "more")
		sc += ", ss_cat";
	sc += " FROM catalog WHERE cat='";
	sc += menu;
	sc += "' ORDER BY s_cat";
//	if(type != "more")
		sc += ", ss_cat";

//DEBUG("sc=", sc);
	if(m_supplierString != "")
	{
		MaxRows = 24;
		if(type == "more")
		{
			sc = "SELECT DISTINCT p.brand AS s_cat, p.s_cat AS ss_cat FROM product p ";
				sc += " WHERE p.supplier IN" + m_supplierString + " ";
			sc += " ORDER BY p.brand, p.s_cat";
		}
		else
		{
			sc = "SELECT DISTINCT s_cat, ss_cat FROM product ";
				sc += " WHERE cat=N'" + menu + "' AND supplier IN" + m_supplierString + " ";
			sc += " ORDER BY s_cat, ss_cat";
		}
	}
	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "menu");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}

	StringBuilder sb = new StringBuilder();
	
	//draw title
	sb.Append("<br><center><font size=+1><b>");
	if(type == "more")
		sb.Append("Brands");
	else
		sb.Append(menu);
	sb.Append("</b></font></center>");
	
	//begin menu list
	//sb.Append("\r\n\r\n<table align=center cellspacing=0 cellpadding=0>");
	sb.Append("<table align=center cellspacing=0 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:10pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr>");

	if(rows <= 0)
	{
		sb.Append("<tr><td>?</td></tr>");
	}

	string subTableIndex = "s_cat";
	Boolean bAlterColor = false;
	DataRow dr = null;
	string sti = null;
	string stiOld = null;
	string ss_cat = null;

	string CompCat = "";
	int mCount = 0;
	for(int i=0; i < rows; i++)
	{
		dr = ds.Tables[0].Rows[i];
		string scat = dr["s_cat"].ToString(); 
//DEBUG("scat -=", scat);
		if(scat != CompCat)
		{
			CompCat = scat;
			mCount++;
		}
		
	}


	int j = 0;
	bool bColumn = true;
	double dRowLeft = mCount % MaxCols;
	int totalRows = mCount / MaxCols;
	if(dRowLeft > 0)
		totalRows += 1;
//DEBUG("mCount =", totalRows);	
	for(int i=0; i < rows; i++)
	{
//		if(j >= MaxRows)
		if(j > totalRows)
		{
			bColumn = true;
			j = 0;
		}
	//	j++;
//DEBUG("j =", j);
		dr = ds.Tables[0].Rows[i];
		sti = dr[subTableIndex].ToString(); //sti: subTableIndex
		Trim(ref sti);
		string usti = sti; //for url, no capital
		if(sti.Length > 1)
			sti = sti.Substring(0, 1).ToUpper() + sti.Substring(1, sti.Length-1).ToLower();
//		if(type != "more")
			ss_cat = dr["ss_cat"].ToString();
//		string ss_cat_old = "";
//	DEBUG("sti =", sti);
//		if(sti == "Zzzothers")
//			continue;
		if(stiOld != sti)
		{
			//if(bColumn || j == MaxRows-1)
			if(bColumn) // || j == totalRows-1)
			{
				bColumn = false;
				if(i != 0)
					sb.Append("</table></td>");
				sb.Append("\r\n\r\n<td width=170 valign=top><table cellspacing=0 cellpadding=0>");
			//	j = 0;
			//	if(i == 0)
			//		j = 1;
			}

			sb.Append("<tr><td colspan=2>&nbsp;</td></tr>");
			sb.Append("<tr><td><img src=/i/reddot.gif></td><td>&nbsp;<a href=c.aspx?");
			if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
				sb.Append("ssid="+ Request.QueryString["ssid"]+"&");
			if(String.Compare(menu, "brands", true) == 0)
			{
				sb.Append("b=");
				sb.Append(HttpUtility.UrlEncode(sti));
			}
			else
			{
				sb.Append("c=");
				sb.Append(HttpUtility.UrlEncode(menu));
				sb.Append("&s=");
				sb.Append(HttpUtility.UrlEncode(sti));
			}
			sb.Append(" class=d><b>");
			if(sti == "Zzzothers" || sti == "")
				sb.Append("All Others");
			else
				sb.Append(sti.ToUpper());
			sb.Append("</b></a></td></tr>");
			j++;
		}
		
		stiOld = sti;
		Encoding unicode = Encoding.Unicode;
		
		if(type != "more")
		{
			sb.Append("<tr><td> </td>");
			sb.Append("<td>&nbsp;<img src=/i/reda1.gif> <a href=c.aspx?");
			if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
				sb.Append("ssid="+ Request.QueryString["ssid"]+"&");
			if(String.Compare(menu, "brands", true) == 0)
			{
				sb.Append("b=");
				sb.Append(HttpUtility.UrlEncode(usti));
				sb.Append("&s=");
			}
			else
			{
				sb.Append("c=");
				sb.Append(HttpUtility.UrlEncode(menu));
				sb.Append("&s=");
				sb.Append(HttpUtility.UrlEncode(usti));
				sb.Append("&ss=");
			}
			sb.Append(HttpUtility.UrlEncode(ss_cat));
			sb.Append(" class=d>");
			if(ss_cat == "zzzOthers" || ss_cat == "")
				sb.Append("All Others");
			else
				sb.Append(ss_cat);
			sb.Append("</td></tr>");
		}
	}
	sb.Append("</table>");
	sb.Append("</td></tr></table><br>");
	Response.Write(sb.ToString());
}
</script>

<!-- #include file="cart.cs" -->

<script runat="server">

double m_dTotal = 0;
double m_dPriceLimit = 0;
bool m_bCPU = false;
bool m_bPrice = false;
bool m_bGST = false;
bool m_bGaming = false;
bool m_bNet = false;
bool m_bNoMoreCPU = false; //true means already got most expensive CPU
string[] m_sPartName = new string[36];
int[] m_nPartStep = new int[36];
int m_nPartCount = 0;

DataSet dso = new DataSet();

void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	PrintHeaderAndMenu();

	CheckQTable();
	PrepareNewQuote();
	DoConditionalBuild();
//BindGrid();
	PrintFooter();
}

void PrepareNewQuote()
{
	EmptyQTable();
	Session["sales_current_quote_number"] = null;
	Session["sales_discount"] = null;
	Session["sales_customerid" + m_ssid] = null;
	EmptyCart();
}

void CalcTotal()
{
	m_dTotal = 0;
//DEBUG("m_qfields=", m_qfields);
	for(int i=0; i<m_qfields; i++)
	{
		int qty = int.Parse(dtQ.Rows[0][fn[i] + "_qty"].ToString());
		string sPrice = dtQ.Rows[0][fn[i] + "_price"].ToString();
		if(!TSIsDigit(sPrice))
		{
//DEBUG("price="+sPrice, " part="+fn[i-m_qfields]);
			continue; //sth wrong, product might has been deleted, skip
		}
		double dTotal = double.Parse(sPrice) * qty;
		m_dTotal += dTotal * 1.125;
//DEBUG("price="+sPrice, " part="+fn[i-m_qfields]);
	}
//DEBUG("m_dTotal=", m_dTotal.ToString());
}

bool DoConditionalBuild()
{
	BuildQTable(); //reset quotation table
	int i = 0;
	for(; i<m_qfields; i++)
		dtQ.Rows[0][i] = "-1";
	for(i=m_qfields; i<m_qfields*3; i++)
	{
		dtQ.Rows[0][i] = "0";
//DEBUG("i=", i);
	}
	m_bCPU = (Request.Form["ccpu"] == "on");
	m_bPrice = (Request.Form["cprice"] == "on");
	m_bGaming = (Request.Form["cgaming"] == "on");
	m_bNet = (Request.Form["cnet"] == "on");

	string sPrice = Request.Form["price"];
	if(TSIsDigit(sPrice))
	{
		m_bPrice = true;
		m_bGST = (Request.Form["cgst"] == "on");
		try
		{
			m_dPriceLimit = double.Parse(sPrice);
		}
		catch(Exception e)
		{
			m_bPrice = false;
		}
		if(!m_bGST)
			m_dPriceLimit *= 1.125;
	}
	else
	{
		m_bPrice = false; //no price input
	}
	
	EmptyQTableNoCPU();
	string sStep = "all";
	bool bNoMore = false;
	if(m_bCPU)
	{
		ChangeOption("cpu", Request.Form["cpu"], "1");
//DEBUG("cpu=", GetProductDesc(Request.Form["cpu"]));
		if(!GetPartPriceList())
			return false;
		TopupPart(1); //get motherboard
//		double dcpuPrice = 0;
//		if(!GetItemPrice(Request.Form["cpu"], ref dcpuPrice))
//			return false;
//		dtQ.Rows[0]["cpu_price"] = dcpuPrice.ToString();
		sStep = "allwithoutcpu";
	}
	else
	{
		if(!GetPartPriceList())
			return false;
		TopupPart(0); //cpu
	}
//	TopupPart(9); //os
	TopupPart(15); //fdd

	if(m_bGaming)
	{
		TopupPart(3); //monitor
		TopupPart(6); //mouse
	}
	if(m_bNet)
		TopupPart(11); //modem
	
	double m_dTotalOld = m_dTotal;
	for(i=2;i<10;i++)
		TopupPart(i);
	if(!m_bCPU)
	{
		//go to top cpu in this price range
		int n = 0;
		int loop = 0;
		int count = 0;
		while(m_dTotal < m_dPriceLimit)
		{
			if(loop++ > 1000)
				break;
//			for(i=0;i<3;i++)
//				TopupPart(i);
			TopupPart(0); //cpu
			count++;
			if(m_dPriceLimit > 3000)
			{
				if(count >= 5)
				{
					count = 0;
					for(i=3;i<m_nPartCount;i++)
						TopupPart(i);
				}
			}
			if(m_bNoMoreCPU)
				break;
		}
		if(!m_bNoMoreCPU)
		{
			loop = 0;
			while(m_dTotal > m_dPriceLimit)
			{
				if(loop++ > 1000)
					break;
				for(i=0;i<3;i++) //go back one step of cpu
					BackupPart(i);
			}
		}
	}

	if(m_bCPU && m_bPrice && m_dTotal > m_dPriceLimit)
	{
		Response.Write("<br><br><table width=90% align=center><tr><td><img src=r.gif border=0>&nbsp;<font size=+1><b>Sorry, we didn't make it, the cheapest system we can offer is:</b></font></td></tr><tr><td>&nbsp;</td></tr>");
	}
	else
	{
		if(m_bPrice && m_dTotal < m_dPriceLimit)
		{
			int n = 0;
			m_dTotalOld = m_dTotal;
			int loop = 0;
			while(m_dTotal < m_dPriceLimit - 30)
			{
				if(loop++ > 1000)
					break;
				n = 1; //skip cpu
				for(;n<m_nPartCount;n++)
				{
					if(m_dPriceLimit < 1500)
					{
						if(n > 5)
							continue;
					}
					else if(m_dPriceLimit < 1800)
					{
						if(n > 7)
							continue;
					}
					else if(m_dPriceLimit < 2000)
					{
						if(n > 9)
							continue;
					}
					else if(m_dPriceLimit < 2500)
					{
						if(n > 12)
							continue;
					}

					if(TopupPart(n) >= m_dPriceLimit - 30)
						break;
					if(m_sPartName[n] == "mb")
					{
						TopupPart(16);//ram
						TopupPart(17); //video
					}
					else if(m_sPartName[n] == "sound")
						TopupPart(18); //speaker
//DEBUG("UP, total="+m_dTotal.ToString("c"), " Step="+m_nPartStep[n].ToString()+" n="+n.ToString()+" part="+m_sPartName[n]);
				}
				if(m_dTotal == m_dTotalOld) //highest
					break;
				m_dTotalOld = m_dTotal;
				if(m_dTotal < m_dPriceLimit - 30)
				{
					if(TopupPart(2) > m_dPriceLimit) //topup hd
						break;
//DEBUG("UP, total="+m_dTotal.ToString("c"), " nStep="+m_nPartStep[n].ToString()+" part=hd");
				}
			}

			int end = 0;
			if(m_bCPU)
				end = 1;
			loop = 0;
			while(m_dTotal > m_dPriceLimit + 30)
			{
				if(loop++ > 1000)
					break;
				n = m_nPartCount - 1;
				for(;n>=end;n--)
				{
					if(BackupPart(n) <= m_dPriceLimit)
						break;
				}
			}
			
			double limit = m_dPriceLimit - 10;
			if(m_dTotal < m_dPriceLimit - 500)
			{
				loop = 0;
				while(m_dTotal < limit)
				{
					if(loop++ > 1000)
						break;
					if(TopupPart(1) >= limit) //topup mb
						break;
					if(TopupPart(2) >= limit) //hd
						break;
					if(TopupPart(5) >= limit) //sound
						break;

					if(m_dTotal == m_dTotalOld) //highest
						break;
					m_dTotalOld = m_dTotal;
				}
			}

			loop = 0;
			while(m_dTotal < limit)
			{
				if(loop++ > 1000)
					break;
				if(TopupPart(4) >= limit) //topup cd
					break;
				if(TopupPart(6) >= limit) //topup d
					break;
				if(TopupPart(7) >= limit) //topup cd
					break;

				if(m_dTotal == m_dTotalOld) //highest
					break;
				m_dTotalOld = m_dTotal;
			}
		}

		string scpu = GetProductDesc(dtQ.Rows[0]["cpu"].ToString());
		string smb = GetProductDesc(dtQ.Rows[0]["mb"].ToString());
		Response.Write("<br><center><h3>Conditional Quotation Result</h3></center>");
		Response.Write("<table width=90% align=center><tr><td><img src=r.gif border=0>&nbsp;<font size=+1><b>Conditions:</b></font></td></tr>");
		Response.Write("<tr><td><table>");
		if(m_bCPU)
			Response.Write("<tr><td>&#149; CPU</td><td>" + scpu + "</td></tr>");
		if(m_bPrice)
		{
			Response.Write("<tr><td>&#149; Price</td><td>Around $" + sPrice);
			if(m_bGST)
				Response.Write(" inclusive of GST");
			else
				Response.Write(" exclusive of GST");
			Response.Write("</td></tr>");
		}
		Response.Write("</table></td><tr><tr><td>&nbsp;</td></tr><tr><td>");

		Response.Write("<table><tr><td><img src=r.gif border=0>&nbsp;<font size=+1><b>The System we can offer you:</b></font></td></tr>");
	}	
	
	for(i=0; i<m_qfields; i++)
	{
		string code = dtQ.Rows[0][i].ToString();
		string qty = dtQ.Rows[0][fn[i] + "_qty"].ToString();
		if(!TSIsDigit(code))
		{
//DEBUG("code=", code);
			continue; //wrong code, skip, there maybe sth has been deleted
		}
		if(int.Parse(dtQ.Rows[0][i].ToString()) <= 0)
			continue;
		Response.Write("<tr><td>&#149; ");
		Response.Write(GetProductDesc(dtQ.Rows[0][i].ToString()));
		if(qty != "1")
			Response.Write("&nbsp&nbsp;<font color=red><b> x " + qty + " (Dual Processor)</b></font>");
		Response.Write("</td></tr>");
	}
	double dSubTotal = m_dTotal;
	double GstRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;   //Modified by NEO
	m_dTotal /= 1.125;
//	double dGST = m_dTotal * 0.125;	
	double dGST = m_dTotal * GstRate;			//Modified by NEO
	Response.Write("<tr><td>&nbsp;</td></tr><tr><td>");
	Response.Write("<table>");
	Response.Write("<tr><td align=right><b>Total Amount</b></td><td align=right><font color=red><b>" + m_dTotal.ToString("c") + "</b></font></td></tr>");
	Response.Write("<tr><td align=right><b>GST</b></td><td align=right><font color=red><b>" + dGST.ToString("c") + "</b></font></td></tr>");
	Response.Write("<tr><td align=right><b>Sub Total</b></td><td align=right><font color=red><b>" + dSubTotal.ToString("c") + "</b></font></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("</table></td></tr>");
	Response.Write("<tr><td><form action=q.aspx method=get><input type=hidden name=r value="+DateTime.Now.ToOADate()+">");
	Response.Write("<input type=submit name=cmd value='View/Change Details'></form></td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr></table>");

//for debugging
/*	m_dTotal = 0;
	for(int i=m_qfields; i<m_qfields*2; i++)
	{
		sPrice = dtQ.Rows[0][i].ToString();
		double dTotal = double.Parse(sPrice);
		m_dTotal += dTotal * 1.125;
DEBUG(fn[i-m_qfields] + "_price=", sPrice);
	}
*/
	return true;
}

double DoTopupPart(int nPartIndex)
{
	string s = m_sPartName[nPartIndex]; //part name
	string code = "-1";
	string price = "0";
//	if(s == "ram" || s == "monitor")
//	{
//		if(m_bPrice && m_dPriceLimit > 1800)
//			m_nPartStep[nPartIndex]++;
//	}
	if(dso.Tables[s].Rows.Count == 0) //no video or no ram
	{
		dtQ.Rows[0][s] = "-1";
		dtQ.Rows[0][s + "_qty"] = "0";
		dtQ.Rows[0][s + "_price"] = 0;
		return m_dTotal;
	}
//if(s == "pccase")
//DEBUG(s+"+rows="+ dso.Tables[s].Rows.Count, " step="+m_nPartStep[nPartIndex]);
	if(m_nPartStep[nPartIndex] >= dso.Tables[s].Rows.Count)
		m_nPartStep[nPartIndex] = dso.Tables[s].Rows.Count - 1;

//	if(m_nPartStep[nPartIndex] < dso.Tables[s].Rows.Count)
//	{
		code = dso.Tables[s].Rows[m_nPartStep[nPartIndex]]["code"].ToString();
		price = dso.Tables[s].Rows[m_nPartStep[nPartIndex]]["price"].ToString();
//		if(s == "mb")
//		{
//			if(m_bPrice && m_dPriceLimit > 1500)
//			{
//				if(double.Parse(price) >= m_dPriceLimit / 5)
//					return; // don't spend too much on mb
//			}
//		}
		ChangeOption(s, code, "1");
//		dtQ.Rows[0][s + "_price"] = price;
		m_nPartStep[nPartIndex]++; //step it, go higher price next time
		CalcTotal();
//	}
//DEBUG("UP, total="+m_dTotal.ToString("c"), " Step="+m_nPartStep[nPartIndex].ToString()+" part="+m_sPartName[nPartIndex]);
	return m_dTotal;
}

double DoBackupPart(int nPartIndex)
{
	string s = m_sPartName[nPartIndex]; //part name
//	if((s == "ram" || s == "monitor" ) && m_dPriceLimit > 1500)
//		return m_dTotal;

	if(dso.Tables[s].Rows.Count > 0)
	{
		if(m_nPartStep[nPartIndex] >= dso.Tables[s].Rows.Count)
			m_nPartStep[nPartIndex] = dso.Tables[s].Rows.Count - 1; //down to the top
	}
	else
	{
		dtQ.Rows[0][s] = "-1"; //clear, no records
		dtQ.Rows[0][s + "_qty"] = "0";
		dtQ.Rows[0][s + "_price"] = "0";
		return m_dTotal;
	}

	string code = "-1";
	string price = "0";
	code = dso.Tables[s].Rows[m_nPartStep[nPartIndex]]["code"].ToString();
	price = dso.Tables[s].Rows[m_nPartStep[nPartIndex]]["price"].ToString();
//		if(s == "mb")
//		{
//			if(double.Parse(price) >= m_dPriceLimit / 6 && m_dPriceLimit > 1500)
//			{
//				return m_dTotal; 
//			}
//		}
//if(!TSIsDigit(price))
//{
//DEBUG("price=" + price+" code="+code, "part="+fn[nPartIndex]);
//	return m_dTotal; //wrong product code?
//}
	ChangeOption(s, code, "1");
//	dtQ.Rows[0][s + "_price"] = price;
	CalcTotal();
//DEBUG("DOWN total=" + m_dTotal.ToString("c") + " rows=" + dso.Tables[s].Rows.Count.ToString(), " nStep=" + m_nPartStep[nPartIndex].ToString() + " part=" + m_sPartName[nPartIndex]);
	return m_dTotal;
}

double TopupPart(int nPartIndex)
{
	string s = m_sPartName[nPartIndex]; //part name
	if(s == "video")
	{
		if(OnBoard("video"))
			return m_dTotal;
		if(m_bGaming)
		{
			if(DoTopupPart(17) > m_dPriceLimit) //videocard
				return m_dTotal;
		}
	}
	else if(s == "sound")
	{
		if(OnBoard("sound") && m_dPriceLimit < 3000)
		{
			return m_dTotal;
		}
		if(DoTopupPart(18) > m_dPriceLimit) //speaker
			return m_dTotal;
	}
	else if(s == "modem")
	{
		if(!m_bNet && m_dPriceLimit < 3000)
			return m_dTotal;
	}
//	else if(s == "mb")
//	{
//		DoTopupPart(16); //ram
//		DoTopupPart(17); //video
//	}

	DoTopupPart(nPartIndex);
	if(s == "cpu")
	{
		UpdatePartPriceList("mb");
		DoTopupPart(1); //mb
		UpdatePartPriceList("ram");
		UpdatePartPriceList("video");
		DoTopupPart(16); //ram
		DoTopupPart(17); //view
		if(m_nPartStep[nPartIndex] >= dso.Tables[s].Rows.Count)
			m_bNoMoreCPU = true;
	}
	else if(s == "mb")
	{
		UpdatePartPriceList("ram");
		UpdatePartPriceList("video");
		DoTopupPart(16); //ram
		DoTopupPart(17); //video
	}
	return m_dTotal;
}

double BackupPart(int nPartIndex)
{
	if(m_nPartStep[nPartIndex] <= 0)
		return m_dTotal;
	m_nPartStep[nPartIndex]--;
	
	string s = m_sPartName[nPartIndex]; //part name
	if(s == "video")
	{
		if(OnBoard("video"))
			return m_dTotal;
		if(m_bGaming)
		{
			if(DoBackupPart(17) < m_dPriceLimit) //videocard
				return m_dTotal;
		}
	}
	else if(s == "sound")
	{
		if(OnBoard("sound") && m_dPriceLimit < 3000)
			return m_dTotal;
		if(DoBackupPart(18) < m_dPriceLimit) //speaker
			return m_dTotal;
	}
	else if(s == "modem")
	{
		if(!m_bNet && m_dPriceLimit < 3000)
			return m_dTotal;
	}
	else if(s == "mb")
	{
		if(DoBackupPart(16) < m_dPriceLimit) //ram
			return m_dTotal;
		if(DoBackupPart(17) < m_dPriceLimit) //video
			return m_dTotal;
	}
	
	DoBackupPart(nPartIndex);
	
	if(s == "cpu")
	{
		UpdatePartPriceList("mb");
		DoBackupPart(1); //mb
		UpdatePartPriceList("ram");
		UpdatePartPriceList("video");
		DoBackupPart(16); //mb
		DoBackupPart(17); //mb
	}
	else if(s == "mb")
	{
		UpdatePartPriceList("ram");
		UpdatePartPriceList("video");
		DoBackupPart(16); //mb
		DoBackupPart(17); //mb
	}
	return m_dTotal;
}

bool GetPartPriceList()
{
	int n = 0;
	m_sPartName[n++] = "cpu";
	m_sPartName[n++] = "mb";
	m_sPartName[n++] = "hd";
	m_sPartName[n++] = "monitor";
	m_sPartName[n++] = "cd";
	m_sPartName[n++] = "sound";
	m_sPartName[n++] = "mouse"; //6
	m_sPartName[n++] = "pccase";
	m_sPartName[n++] = "kb";	
	m_sPartName[n++] = "os";
	m_sPartName[n++] = "nic";	//10
	m_sPartName[n++] = "modem";		
	m_sPartName[n++] = "printer"; //12
	m_sPartName[n++] = "scanner";
	m_sPartName[n++] = "cdrw";	//14
	m_sPartName[n++] = "fd";
	m_sPartName[n++] = "ram"; //16
	m_sPartName[n++] = "video";
	m_sPartName[n++] = "speaker";
	m_nPartCount = n;
	for(n=0; n<m_nPartCount; n++)
		m_nPartStep[n] = 0;

	string sc = "";
	string s = "cpu";
	for(int i=0; i<m_nPartCount; i++)
	{
		s = m_sPartName[i];
		if(s == "cpu")
			sc = "SELECT DISTINCT p.code, p.price FROM q_mb q JOIN product p ON q.parent=p.code ORDER BY p.price";
		else if(s == "mb")
		{
			if(m_bGaming)
				sc = "SELECT DISTINCT p.code, p.price FROM q_mb q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["cpu"].ToString() +" AND p.name NOT LIKE '%VIDEO%' ORDER BY p.price";
			else
				sc = "SELECT DISTINCT p.code, p.price FROM q_mb q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["cpu"].ToString() + " ORDER BY p.price";
		}
		else if(s == "video")
			sc = "SELECT DISTINCT p.code, p.price FROM q_video q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["mb"].ToString() + " ORDER BY p.price";
		else if(s == "ram")
			sc = "SELECT DISTINCT p.code, p.price FROM q_ram q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["mb"].ToString() + " ORDER BY p.price";
		else if(s == "monitor")
			sc = "SELECT p.code, p.name, p.price FROM q_flat q LEFT OUTER JOIN product p ON q."+s+"=p.code WHERE q."+s+" IS NOT NULL AND p.name NOT LIKE '%AOC%' ORDER BY p.price";
		else if(s == "hd")
		{
			if(m_bPrice && m_dPriceLimit >= 1800)
			{
				sc = "SELECT p.code, p.name, p.price FROM q_flat q LEFT OUTER JOIN product p ON q."+s+"=p.code WHERE q."+s+" IS NOT NULL AND p.name NOT LIKE '%5400%' ORDER BY p.price";
			}
			else
				sc = "SELECT p.code, p.name, p.price FROM q_flat q LEFT OUTER JOIN product p ON q."+s+"=p.code WHERE q."+s+" IS NOT NULL ORDER BY p.price";
		}
		else
			sc = "SELECT p.code, p.name, p.price FROM q_flat q LEFT OUTER JOIN product p ON q."+s+"=p.code WHERE q."+s+" IS NOT NULL ORDER BY p.price";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dso, s);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
//	DataView dv = new DataView(dso.Tables["pccase"]);
//	MyDataGrid.DataSource = dv ;
//	MyDataGrid.DataBind();
	return true;
}


bool UpdatePartPriceList(string s)
{
	if(dso.Tables[s] != null)
		dso.Tables[s].Clear();
	string sc = "";
	if(s == "cpu")
		sc = "SELECT DISTINCT p.code, p.price FROM q_mb q JOIN product p ON q.parent=p.code ORDER BY p.price";
	else if(s == "mb")
		sc = "SELECT DISTINCT p.code, p.price FROM q_mb q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["cpu"].ToString() + " ORDER BY p.price";
	else if(s == "video")
		sc = "SELECT DISTINCT p.code, p.price FROM q_video q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["mb"].ToString() + " ORDER BY p.price";
	else if(s == "ram")
		sc = "SELECT DISTINCT p.code, p.price FROM q_ram q JOIN product p ON q.code=p.code WHERE q.parent="+dtQ.Rows[0]["mb"].ToString() + " ORDER BY p.price";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dso, s);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void EmptyQTableNoCPU()
{
	for(int i=1; i<m_qfields*3; i++)
	{
		if(i == m_qfields)
			continue; //skip cpu price
		if(i<m_qfields)
			dtQ.Rows[0][i] = "-1";
		else
			dtQ.Rows[0][i] = "0";
	}
}

bool OnBoard(string s)
{
	string mb = dtQ.Rows[0]["mb"].ToString();
//DEBUG("mb=", mb);
	if(mb == null || mb == "")
		return true;
	if(int.Parse(mb) <= 0)
		return true;
	string desc = GetProductDesc(mb);
	desc = desc.ToLower();

	CompareInfo ci = CompareInfo.GetCompareInfo(1);
	if(s == "sound")
	{
		int p = ci.IndexOf(desc, "audio", CompareOptions.IgnoreCase);
		if(p > 0)
		{
			string sub = desc.Substring(0, p);
			if(ci.IndexOf(sub, "no",  CompareOptions.IgnoreCase) < 0)
				return true;
		}
		else if(ci.IndexOf(desc, "on board", CompareOptions.IgnoreCase) >= 0)
		{
			if(ci.IndexOf(desc, "sound", CompareOptions.IgnoreCase) >= 0)
				return true;
			if(ci.IndexOf(desc, "audio", CompareOptions.IgnoreCase) >= 0)
				return true;
		}
	}
	else if(s == "video")
	{
		if(ci.IndexOf(desc, "with video", CompareOptions.IgnoreCase) >= 0)
			return true;
		if(ci.IndexOf(desc, "on board", CompareOptions.IgnoreCase) >= 0)
		{
			if(ci.IndexOf(desc, "vga", CompareOptions.IgnoreCase) >= 0)
				return true;
			if(ci.IndexOf(desc, "video", CompareOptions.IgnoreCase) >= 0)
				return true;
		}
	}
	else if(s == "nic")
	{
//DEBUG("hs=", s);
		if(ci.IndexOf(desc, "with lan", CompareOptions.IgnoreCase) >= 0)
			return true;
		if(ci.IndexOf(desc, "on board", CompareOptions.IgnoreCase) >= 0)
		{
			if(ci.IndexOf(desc, "lan", CompareOptions.IgnoreCase) >= 0)
				return true;
			if(ci.IndexOf(desc, "network", CompareOptions.IgnoreCase) >= 0)
				return true;
		}
	}
	return false;
}

void BindGrid()
{
	DataView source = new DataView(dtQ);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}
</script>

<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=0px 
	BorderStyle=Solid 
	BorderColor=Tan
	CellPadding=5 
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=460px 
	HorizontalAlign=center>
</asp:DataGrid>

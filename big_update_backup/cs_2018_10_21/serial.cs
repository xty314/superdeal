<!-- #include file="kit_fun.cs" -->
<script runat="server">

//string m_quoteNumber = "";
string m_poID = "";
string m_invNumber = "";

bool m_bEnd = false;

int m_nSearchReturn = 0;
int m_nSerialReturn = 0;
int m_iTatalOrdrItems = 0;
int m_nBulkDuplicateFound = 0;
int m_nBulkInputError = 0;
bool m_bLastErrorIsDuplicate = false;

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

//Current Row
string c_code = "";
string c_supplier = "";
string c_supplier_code = "";
string c_name = "";
string c_price = "";
string c_qty = ""; 
string m_end = "";
int m_step = 1;

bool m_xtra_qty = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("sales"))
		return;

	if(Request.QueryString["t"] == "bulk")
	{
		if(Request.Form["cmd"] == "Process")
			DoProcessBulkInput();
		else
			PrintBulkInputInterface();
		return;
	}

	string temp_type = "";
	m_poID = Request.QueryString["n"];
	PrintAdminHeader();
	PrintAdminMenu();
	temp_type = Request.Form["cmd"];
	if(Request.QueryString["x"] == "1")
		Session["xtra_qty"] = "1";
	else
		Session["xtra_qty"] = null;

	if(temp_type == null)
		temp_type = "";
	if((temp_type.ToLower() == "search" || Request.Form["txtSearch"] != null) && Request.Form["t"]!="u")
	{

		SearchSerial();
		if(Request.Form["txtSearch"] != null)
		{
			if(!DisplaySearchResult())
			{
				return;
			}
		}
		PrintBottom();
	}
	else
	{

		if(Request.QueryString["t"] != null && Request.QueryString["t"].ToLower() == "del")
		{
			if(DoDelSerial())
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=serial.aspx?n="+m_poID+"");
				if(Request.QueryString["sc"] != null && Request.QueryString["sc"] != "")
						Response.Write("&mcd="+ Request.QueryString["sc"] +"");
				Response.Write("&r=0&t=v\">");
			}
			return;
		}
		else if(temp_type == "Skip")
		{
			if(SkipSerial())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=serial.aspx?n="+m_poID+"&r=0&t=v\">");
			return;
		}
		else if(temp_type == "Update Serial")
		{
			if(Request.Form["txtBegin"] != null && Request.Form["txtBegin"] != "")
			{
				if(DoMassInsert())
				{
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=serial.aspx?n="+m_poID+"");
					if(Request.QueryString["sc"] != null && Request.QueryString["sc"] != "")
						Response.Write("&mcd="+ Request.QueryString["sc"] +"");
					Response.Write("&r=0&t=v\">");
					//if(Session["xtra_qty"] == "1")
					//	Response.Write("&x=1");
				}
				return;
			}
			else
			{
				string sSerial = "";
				if(InsertSerial(sSerial))
				{
					Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=serial.aspx?n="+m_poID+"&r=0&t=v");
					if(Request.QueryString["sc"] != null && Request.QueryString["sc"] != "")
							Response.Write("&mcd="+ Request.QueryString["sc"] +"");
					if(Session["xtra_qty"] == "1")
						Response.Write("&x=1");
					Response.Write("\">");
				}
				return;
			}
		}
		else            
		{
			if(!GetProducts())
				return;

			Response.Write("<br><br><center><h4><b>Serial Number Page</b></center>");
			BindISTable();
			if(!GetInsertedSerial())
				return;
			PrintTextArea();
		//	PrintBottom();
			BindSerialGrid();
		}
	}

	PrintAdminFooter();	
}

bool DisplaySearchResult()
{
	string SearchSN = Request.Form["txtSearch"];

	string sc = "SELECT purchase_order_id AS po_number, supplier, purchase_date, status FROM stock ";
		   sc+= " WHERE sn = '" +SearchSN + "'";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "SearchResult");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}


	Response.Write("<br><table width=50% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE background-color=white");
	
	if(dst.Tables["SearchResult"].Rows.Count == 1 )
	{
		DataRow dr = dst.Tables["SearchResult"].Rows[0];
		string po_number = dr["po_number"].ToString();
		string supplier = dr["supplier"].ToString();
		string purchase_date = dr["purchase_date"].ToString();
		string status = dr["status"].ToString();

		Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
		Response.Write("<td>Order No.</td><td>Supplier</td><td>Date Received</td><td>Status</td></tr>");
		Response.Write("<tr style=\"color:red;background-color:#FFFFFF;font-weight:bold;\">");
		Response.Write("<td><b><a href = 'purchase.aspx?n=" +po_number+ "'>" +po_number+ "</a></b></td>");
		Response.Write("<td>" +supplier+ "</td>");
		Response.Write("<td>" +purchase_date+ "</td>");
		Response.Write("<td>" +status+ "</td></tr>");

	}
	else
	{
		Response.Write("<tr style=\"color:red;background-color:#FFFFFF;font-weight:bold;\">");
		Response.Write("<td align=center height=20><font size=+1><b>No Match Record Found</b></font></td></tr>");
	}
	
	Response.Write("</table>");
	return true;
}

void SearchSerial()
{
	Response.Write("<form method=post action='serial.aspx?n="+m_poID+"&t=s' name=frm>");
	Response.Write("<br><br><table width=50% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE background-color=white");
	Response.Write("<tr><td>- Search Purchase Order by Product Serial No. -</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td align=center height=40><font size=+1><b>SEARCH:&nbsp;&nbsp</b></font>");
	Response.Write("<input type=text name=txtSearch value='" + Request.Form["txtSearch"]+ "'>&nbsp;&nbsp");


	Response.Write("<script");
	Response.Write(">\r\ndocument.frm.txtSearch.focus();\r\n</script");
	Response.Write(">\r\n");
	//Response.Write("<input type=text name=txtSearch onchange=\"document.frmSearchSerial.cmd.Focus();\">&nbsp;&nbsp");
	Response.Write("<input type=submit name=cmd value='Go!'>");
	Response.Write("</td></tr></table></form>");

}

bool DoDelSerial()
{
	string sSerialNo = Request.QueryString["r"].ToString();

	string s_id = Request.QueryString["i"];
	string sc = "";
	sc += "  UPDATE purchase_item SET sn_entered = 0 WHERE code = (SELECT product_code FROM stock WHERE id = "+ s_id +" AND purchase_order_id = " + Request.QueryString["n"] +" ) ";
	sc += " AND id = "+ Request.QueryString["n"] + "";
	sc += " DELETE FROM stock WHERE id = " + s_id;
	sc += AddSerialLogString(sSerialNo, "Deleted while stocking", m_poID, "", "", "");

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

	string temp_value = "0";
	UpdatePurchase(temp_value);

	return true;
}

bool DoMassInputEnd(string s_end,  ref string s_endAt)
{
	char[] cb = s_end.ToCharArray();
	int p=0;
	for(p=s_end.Length-1; p>=0; p--)
	{
		if(!Char.IsDigit(cb[p]))
			break;
	}
	p++;
	if(p<s_end.Length-1)
	{
		for(;p<s_end.Length;p++)
			s_endAt += cb[p];
	}
	return true;
}

bool DoMassiveInput(ref string prefix, ref string sb)
{
	string sBegin = Request.Form["txtBegin"];
	prefix = "";
	sb = "";
	char[] cb = sBegin.ToCharArray();	
	int p = 0;
	for(p=sBegin.Length-1; p>=0; p--)
	{
		if(!Char.IsDigit(cb[p]))
			break;
	}
	p++;
	for(int i=0; i<p; i++)
		prefix += cb[i];

	if(p<sBegin.Length-1)
	{
		for(;p<sBegin.Length;p++)
			sb += cb[p];
	}
	return true;
}

bool DoMassInsert()
{
	string s = Request.Form["txtBegin"];
	string part2 = Request.Form["part2"];
	string qty = Request.Form["qty"];
//DEBUG("s = ", s);
//DEBUG("part2 = ", part2);
//DEBUG("qty = ", qty);

	if(!IsInteger(qty))
	{
		Msg_Error("Error, wrong Quantity for multiple input");
		return false;
	}

	//int digits = qty.Length + 1;
	int digits = s.Length + 1;

	int n = MyIntParse(qty);

	int p1 = 0;
	int p2 = s.Length;
	int d = 0;

	for(int i=s.Length-1; i>=0; i--)
	{
		if(!Char.IsDigit(s[i]) && d == 0)
		{
			p2 = i;
			continue;
		}
		d++;
//DEBUG("digist = ", digits);
		if(!Char.IsDigit(s[i]) || d > digits)
			break;
		p1 = i;
	
	}
//DEBUG("p 1 = ", p1);
	if(p2 <= p1)
	{
		Msg_Error("Error, the S/N entered doesn't contain any digits");
		return false;
	}

	string prefix = s.Substring(0, p1);
	string v = s.Substring(p1, p2-p1);
	string tail = s.Substring(p2, s.Length - p2);
//DEBUG("prefix=", prefix);
//DEBUG("v=", v);
//DEBUG("tail=", tail);
//	int nv = MyIntParse(v);
	long nv = MyLongParse(v);
	int len = v.Length;
	int step = MyIntParse(Request.Form["step"]);
	for(int i=0; i<n; i++)
	{
		string vv = (nv + i*step).ToString();
		string vf = vv;
		if(vv.Length < v.Length)
		{
//DEBUG("vvl=", vv.Length);
//DEBUG("vl=", v.Length);
			vf = "";
			for(int j=vv.Length; j<v.Length; j++)
				vf += "0";
			vf += vv;
		}
//		else if(vv.Length > v.Length)
//		{
//			if(prefix.Length >= 2)
//			{
//				if(prefix[prefix.Length - 2] == '0'

//		}
		string sn = prefix + vf + tail + part2;
//		DEBUG("sn =", sn);
		InsertSerial(sn);
	}
//	return false;
	return true;
}

void Msg_Error(string msg)
{
	Response.Write("<br><br><center><h3>" + msg + "</h3>");
}

bool MassInsertSerial(string sEnd, int iStep, string sPrefix, string sSb)
{
	long iStart = long.Parse(sSb);
	long iEnd   = long.Parse(sEnd);
	int qty = int.Parse(Request.Form["sOrderQty"].ToString());

	int countTimes = 0;
	
	//int nBeginLen = Request.Form["txtBegin"].Length;
	long nBeginLen = Request.Form["txtEnd1"].Length;
	int nNumLen = 0;
	//for(int i=iStart;i<=iEnd;i+=iStep)
	for(long i=iStart;i<=iEnd;i+=iStep)
	{
		string temp_serial = sPrefix;
		nNumLen = i.ToString().Length;
		//int pad = nBeginLen - sPrefix.Length - i.ToString().Length;
		long pad = nBeginLen - sPrefix.Length - i.ToString().Length;

		//for(int j=0; j<pad; j++)
		for(long j=0; j<pad; j++)
			temp_serial += "0";
		temp_serial += i.ToString();

		countTimes+=1; 
		//m_iNumOfUpdated = countTimes;
		if(countTimes >qty)
			return false;
		InsertSerial(temp_serial);
	}
	return true;	
}

bool CheckSerialValidation(string sSerialNo)
{
	if(sSerialNo != "")
	{
		string sc = "SELECT sn FROM stock WHERE sn = '"+sSerialNo +"'";

		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "checkserial");
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}

		if(dst.Tables["checkserial"].Rows.Count >0)//  && sSerialNo !="")
		{
			if(Request.QueryString["t"] == "bulk")
			{
				Response.Write("Duplicate found, s/n : " + sSerialNo + "<br>\r\n");
				m_nBulkDuplicateFound++;
				m_bLastErrorIsDuplicate = true;
				return false;
			}
			else
			{
				Response.Write("<script language=javascript>window.alert('");
				Response.Write(sSerialNo + " Serial Number Duplication   ");
				Response.Write(dst.Tables["checkserial"].Rows.Count.ToString());
				Response.Write("');\r\n");
				Response.Write(" window.location=(\""+ Request.ServerVariables["URL"] +"?n="+ Request.QueryString["n"] +"&s="+ Request.QueryString["s"] +"");
				Response.Write("&sc="+ Request.QueryString["sc"] +"&cd="+ Request.QueryString["cd"] +"&t="+ Request.QueryString["u"] +"\");</script");
				Response.Write(">");
				return false;
			}
		}
	}
	return true;
}


bool GetProducts()
{

	m_poID = Request.QueryString["n"];
//	string sc ="Select p.*, c1.short_name AS supplier ";
	string sc =" Select p.id, p.code, p.supplier_code, p.name, SUM(p.qty) AS qty, AVG(p.price) AS price, p.sn_entered, c1.short_name AS supplier ";

	if(Session["xtra_qty"] == null)
	{
		sc += ", (SELECT COUNT(purchase_order_id) AS sn_qty ";
		sc += " FROM  stock s WHERE s.purchase_order_id = '"+ m_poID +"' ";
		sc += " AND s.supplier_code = c.supplier_code AND s.product_code = c.code) AS nqty ";
	}
	else
		sc += " ,0 AS nqty ";

	sc += ",  (SELECT  SUM(qty) FROM purchase_item pi WHERE pi.id = '"+ m_poID +"') AS sqty ";
	sc += " From code_relations c INNER JOIN purchase_item p ON c.supplier_code=p.supplier_code ";
	sc += " AND p.code = c.code ";
	sc += " INNER JOIN purchase pc ON pc.id = p.id INNER JOIN card c1 ON c1.id = pc.supplier_id ";
	sc += " Where p.id='" +m_poID + "'";
	sc += " AND p.sn_entered = 0 ";
	sc += " GROUP BY p.id, p.code, p.supplier_code, p.name, p.sn_entered, c1.short_name, c.supplier_code, c.code ";
	sc += " ORDER BY p.code ";

//DEBUG("getsc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nSearchReturn = myAdapter.Fill(dst, "serial");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	
//	for(int i=0; i< dst.Tables["serial"].Rows.Count; i++)
//		m_iTatalOrdrItems += int.Parse(dst.Tables["serial"].Rows[i]["qty"].ToString());
if(m_nSearchReturn > 0)
	m_iTatalOrdrItems = int.Parse(dst.Tables["serial"].Rows[0]["sqty"].ToString());

//	if(dst.Tables["serial"].Rows.Count > 0)
//		return true;
//	else
//		return false;
    
	return true;
}

bool GetProductNameQty(string s_supplier, string s_supplier_code, ref string s_name, ref string s_orderQty)
{
	s_name = "";
	s_orderQty = "";

	for(int i=0; i<dst.Tables["serial"].Rows.Count; i++)
	{
		if(dst.Tables["serial"].Rows[i]["supplier"].ToString() == s_supplier 
			&& dst.Tables["serial"].Rows[i]["supplier_code"].ToString() == s_supplier_code)
		{
			s_name = dst.Tables["serial"].Rows[i]["name"].ToString();
			s_orderQty = dst.Tables["serial"].Rows[i]["qty"].ToString();
			return true;
		}
	}

	return false;
}

bool PrintTextArea()	//print text area
{
	string scOrderQty = c_qty;
	if(scOrderQty == "")
		scOrderQty = "0";
	if(Request.QueryString["s"] != null && Request.QueryString["sc"] != null)
	{
		c_supplier_code = Request.QueryString["sc"];
		c_code = Request.QueryString["cd"];
		if(!GetProductNameQty(Request.QueryString["s"], c_supplier_code, ref c_name, ref scOrderQty))
			return false;
	}
	
	int iSerialqty = 0;
	CheckSerialTable(m_poID, c_supplier_code, ref iSerialqty, c_code);

	scOrderQty = (int.Parse(scOrderQty) - iSerialqty).ToString();
//	Response.Write("<form method=post action='serial.aspx?n="+m_poID+"&s="+ HttpUtility.UrlEncode(c_supplier) + "&sc=" + HttpUtility.UrlEncode(c_supplier_code) +"&cd="+ c_code +"&t=u");
	Response.Write("<form method=post action='"+ Request.ServerVariables["URL"] +"?n="+m_poID+"&cd="+ HttpUtility.UrlEncode(c_code) +"&s="+ HttpUtility.UrlEncode(c_supplier) + "&sc=" + HttpUtility.UrlEncode(c_supplier_code) +"");
	if(Session["xtra_qty"] == "1")
		Response.Write("&x=1");
	Response.Write("' name=frmSerial>");
	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=3><hr color=black></td></tr>");
	Response.Write("<tr><td colspan=3 ><font size=2> - Input Serial Number Area - </td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");

	//Response.Write("<td>&nbsp;</td>");
	
	Response.Write("<th colspan=3 align=center>CURRENT ITEM</td></tr>");
		
	Response.Write("<tr ><td align=center colspan=3><font size=+1 color=#F2B809>" +c_supplier_code + "&nbsp;"+c_name);
	Response.Write("</font></td></tr>");
	
	//Response.Write("<tr><td align=center colspan=3 bgcolor=yellow>Input Serial Number</td></tr>");
//	Response.Write("<tr><td align=center colspan=3 bgcolor=yellow>Input Serial Number</td></tr>");
//	if(m_xtra_qty)
	{
		Response.Write("<tr align=left><th colspan=1>ADD Xtra S/N: </td><td><input type=checkbox name=x_qty ");
		Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?n="+ Request.QueryString["n"] +"&t="+ Request.QueryString["t"] +"");
		if(Request.QueryString["mcd"] != null && Request.QueryString["mcd"] != "")
			Response.Write("&mcd="+ Request.QueryString["mcd"] +"");
		if(Request.QueryString["cd"] != null && Request.QueryString["cd"] != "")
			Response.Write("&cd="+ Request.QueryString["cd"] +"");
		Response.Write("&r="+ Request.QueryString["r"] +"&x=");	
		if(Session["xtra_qty"] == "1")
			Response.Write("0')\"  checked ");
		else
			Response.Write("1')\"");
		
		Response.Write("> For <b>"+ c_name +"</td></tr>");
	}
	if(m_bEnd)
	{
		//Response.Write("<table border=0 cellspacing=0 cellpadding=0>");	
		Response.Write("<input type=hidden name=txtBegin value=" + Request.Form["txtBegin"] + ">");
		Response.Write("<tr><td><b>End S/N (step by 1): </b></td><td><input type=text name=txtEnd1><script");
		Response.Write(">\r\ndocument.frmSerial.txtEnd1.focus();\r\n</script");
		Response.Write(">\r\n</td>");
		Response.Write("<tr><td><b>End S/N (step by 2): </b></td><td><input type=text name=txtEnd2>");
		Response.Write("\r\n");
	}
	else
	{
		Response.Write("<input type=hidden name=ready value=1>");
		Response.Write("<tr><td><b>Enter Single S/N : </b></td><td><input type=text name=txtSerial><script");
		Response.Write(">\r\ndocument.frmSerial.txtSerial.focus();\r\n</script");
		Response.Write(">\r\n</td>");
		Response.Write("<tr><td><b>Multiple S/N, Begin : </b></td>");
		Response.Write("<td><input type=text size=10 name=txtBegin onchange='document.frmSerial.ready.value=0; document.frmSerial.part2.focus()'>");
		Response.Write("<input type=text size=10 name=part2><i>(first part that increases)</i></td>");
		Response.Write("<tr><td><b>Multiple S/N, Interval : &nbsp;</b></td><td>");
		Response.Write("<input type=radio name=step value=1 checked>1 &nbsp&nbsp; ");
		Response.Write("<input type=radio name=step value=2>2 &nbsp&nbsp; ");
		
		Response.Write("</td></tr>");
		Response.Write("<tr><td><b>Multiple S/N, Quantity : &nbsp;</b></td><td>");
		Response.Write("<input type=text name=qty value=" + scOrderQty + ">");
//		Response.Write("</td></tr>");
		Response.Write("\r\n");
	
	}

	Response.Write("<input type=hidden name=sOrderQty value="+ scOrderQty+ ">");
	Response.Write("<input type=submit name=cmd value='Update Serial' " + Session["button_style"] + " onclick='if(document.frmSerial.ready.value==\"0\"){document.frmSerial.ready.value=\"1\";return false;}'></td>");

	Response.Write("</tr>");


	Response.Write("<tr><td colspan=6><hr color=black></td></tr>");
	
	Response.Write("</table>");

	return true;
}


bool BindISTable()
{
	Response.Write("<table align=center width=90% valign=center cellspacing=0 cellpadding=0 border=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6>- Purchase Item - </td></tr>");
	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th>CODE</th>\r\n");
	Response.Write("<th>M_PN</th>\r\n");
	Response.Write("<th>DESCRIPTION</th>\r\n");
	Response.Write("<th>QTY</th>\r\n");
//	Response.Write("<th> &nbsp; XTRA QTY? &nbsp;</th>\r\n");

	Response.Write("<th>&nbsp;</th>\r\n");
	Response.Write("</tr>\r\n");
//if(Session["xtra_qty"] == null)
//	if(OrdrLokUp())
//		return false;

	bool bGetCurrent = false;
//DEBUG("m_sn = ",m_nSearchReturn);
	int total_qty_entered = 0;
	for(int i=0; i<m_nSearchReturn; i++)
	{
		DataRow dr = dst.Tables["serial"].Rows[i];
		string code = dr["code"].ToString();
		//string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
//		string price = dr["price"].ToString();
		string qty = dr["qty"].ToString();
		if(name != "" && name != null)
			name = StripHTMLtags(name);
		int nQty = int.Parse(qty);

		int sqty = 0;
//		DEBUG("nqty = ", nQty);
		total_qty_entered += nQty;
//		DEBUG("totalqty enterd =", total_qty_entered);
		sqty = int.Parse(dr["nqty"].ToString());
		if(!IsAllSNEntered(int.Parse(dr["sqty"].ToString()), total_qty_entered, nQty, sqty, code, supplier_code))
			return false;
//		if(!CheckSerialTable(m_poID, supplier_code, ref sqty, code))
//			return false;

		nQty -= sqty;
		
		string qryCode = Request.QueryString["cd"];
		string qryMCode = Request.QueryString["mcd"];
		if(qryMCode == supplier_code || qryCode == code && nQty == 0)
			bGetCurrent = true;
	
		if((qryMCode == null || qryCode == null) && i == 0)
			bGetCurrent = true;

		if(nQty <=0)
			continue;

		if(qryMCode == supplier_code || qryCode == code)
			bGetCurrent = true;

		if(bGetCurrent)
		{
			if(sqty == 0)
			m_xtra_qty = true;

			c_code = dr["code"].ToString();
			c_supplier = dr["supplier"].ToString();
			c_supplier_code = dr["supplier_code"].ToString();
			c_name = dr["name"].ToString();
			c_price = dr["price"].ToString();
			c_qty = dr["qty"].ToString(); 
		
			Response.Write("<form method=post action='serial.aspx?qty=" + nQty + "&n="+m_poID+"&s="+ HttpUtility.UrlEncode(c_supplier) + "&sc=" + HttpUtility.UrlEncode(c_supplier_code) +"&t=u'>");
		}
		qty = nQty.ToString();

		Response.Write("<tr");
		if(qryMCode == supplier_code)
			Response.Write(" bgcolor=#F9E5C1 ");

		Response.Write(">");
		Response.Write("<td>" +code+ "</td>");
		Response.Write("<td>" +supplier_code+ "</td>");
		Response.Write("<td>");

		Response.Write("<input type=button value='Bulk Input' onclick=window.open('serial.aspx?t=bulk&s=");
		Response.Write(HttpUtility.UrlEncode(c_supplier) + "&n=" + Request.QueryString["n"] + "&cd=" + HttpUtility.UrlEncode(code));
		Response.Write("&sc=" + HttpUtility.UrlEncode(supplier_code) + "&name=" + HttpUtility.UrlEncode(name) + "') class=b>");

		Response.Write("<a title='select this item first' href='"+ Request.ServerVariables["URL"] +"?n="+ Request.QueryString["n"]+"&cd="+ code +"&mcd="+ supplier_code +"");
		Response.Write("&r="+ Request.QueryString["r"] +"&t="+ Request.QueryString["t"] +"' class=o>");
		Response.Write(name + "</a></td>");		
		Response.Write("<td>" +qty+ "</td>");

		if(bGetCurrent)
		{
			Response.Write("<td><input type=submit name=cmd value=Skip " + Session["button_style"] + "></td>");
			bGetCurrent = false;
		}
		else
			Response.Write("<td>&nbsp;</td>");
		Response.Write("</tr>");
	}
	Response.Write("</form>");
	Response.Write("</table>");
	return true;
}

bool IsAllSNEntered(int tQty, int t_eqty, int actual_qty, int enter_qty, string code, string supplier_code)
{
	string sc = "";
	if(actual_qty <= enter_qty)
	{

		sc = "UPDATE purchase_item SET sn_entered = 1 WHERE code ="+ code +" AND supplier_code = '"+ supplier_code +"' AND id = '"+ m_poID +"' ";
		if(tQty <= t_eqty)
			sc = "UPDATE purchase SET sn_entered = 1 WHERE po_number = '"+ m_poID +"' ";
//	DEBUG("sc = ", sc);

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

	}
	return true;
}
bool OrdrLokUp()
{
	string sc = "SELECT COUNT(purchase_order_id) AS Instock_qty FROM stock WHERE purchase_order_id ='" + m_poID + "'";

//DEBUG("sc= +", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "total_instock");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	//int Total_ordr_item_row = (dst.Tables["serial"].Rows.Count);   //the problem;
	int Total_instock = int.Parse(dst.Tables["total_instock"].Rows[0]["Instock_qty"].ToString());

	if(m_iTatalOrdrItems > Total_instock)
		return false;

	string temp_value = "1";
	if(!UpdatePurchase(temp_value))
		return false;

	return true;
}

bool UpdatePurchase(string svalue)
{
	string sc = "UPDATE purchase SET sn_entered = "+ svalue+ " WHERE po_number =" + m_poID;

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

bool CheckSerialTable(string temp_poNumber, string temp_dealNum, ref int qty, string code)
{
	string sc = "SELECT COUNT(purchase_order_id) AS sn_qty ";
	sc += " FROM stock ";
	sc += " WHERE purchase_order_id ='";
	sc += temp_poNumber + "' AND supplier_code = '" + temp_dealNum +"'";
	sc += " AND product_code = '"+ code +"'";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "tempNumOfSN");

	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	qty = int.Parse(dst.Tables["tempNumOfSN"].Rows[0]["sn_qty"].ToString());

	dst.Tables["tempNumOfSN"].Clear();
	return true;
}

//copy from bb.c to decode the message with special char
string msgEncode(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += "\'\'"; //double it for SQL query
		else if(s[i] == '<')
			ss += '[';
		else if(s[i] == '>')
			ss += ']';
		else if(s[i] == '*')
			ss += '-';
		else if(s[i] == '.')
			ss += ' ';
		else if(s[i] == '/')
			ss += ' ';
		else if(s[i] == '(')
			ss += ' ';
		else if(s[i] == ')')
			ss += ' ';
		else
			ss += s[i];
	}
	return ss;
}

bool InsertSerial(string sMassSerial)
{
	c_supplier = Request.QueryString["s"];
	c_supplier_code = Request.QueryString["sc"];
	c_code = Request.QueryString["cd"];
	string sProduct_code = "";
	string sProduct_desc = "";
	
	string ssc = "SELECT pi.* ";
	ssc += " FROM purchase_item pi ";
	ssc += " JOIN purchase po ON po.id=pi.id ";
	//ssc += " INNER JOIN product p ON p.code = pi.code AND pi.supplier_code = p.supplier_code ";
	ssc += " INNER JOIN code_relations p ON p.code = pi.code AND pi.supplier_code = p.supplier_code ";
	ssc += " WHERE po.id=" + m_poID;
	if(c_supplier_code != "")
		ssc += " AND pi.supplier_code='" + c_supplier_code + "'";
	ssc += " AND pi.code = '"+ c_code +"'";
//DEBUG("sc1 = ", ssc);

	try
	{
		myAdapter = new SqlDataAdapter(ssc, myConnection);
		int rows = myAdapter.Fill(dst, "codetable");
		if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting purchase order");
			return false;
		}
	}
	catch (Exception e)
	{
		ShowExp(ssc,e);
		return false;
	}
	DataRow dr = dst.Tables["codetable"].Rows[0];

	m_invNumber = m_poID; //dr["inv_number"].ToString();

	//string temp_price = double.Parse(c_price.ToString()).ToString("c");
	double temp_price = double.Parse(dr["price"].ToString());
	sProduct_code = dr["code"].ToString();
	sProduct_desc = dr["name"].ToString();
	
	if(sProduct_desc != null && sProduct_desc != "")
		sProduct_desc = StripHTMLtags(sProduct_desc);

	/* Added code to cut down length of product description on 7-11-02 tee */
//	string s_swap = "";
//	if(sProduct_desc.Length >= 100)
//	{
//		for(int i=0; i<sProduct_desc.Length; i++)
//			s_swap += sProduct_desc[i].ToString();
//	}
//	else
//		s_swap = sProduct_desc;
	/* decode the sign char in product description */
	sProduct_desc = msgEncode(sProduct_desc);

	string sn_temp = "";

	if(Request.Form["txtBegin"] == null || Request.Form["txtBegin"] == "")
		sn_temp = Request.Form["txtSerial"];
	else
		sn_temp = sMassSerial;

	Trim(ref sn_temp);

	if(sn_temp == "")
		return true;

	if(!CheckSerialValidation(sn_temp))
		return false;

	string sc = "INSERT INTO stock (product_code, sn, branch_id, purchase_order_id, cost, supplier, supplier_code, status, prod_desc) ";
	sc += "VALUES ("+sProduct_code+" ,'"+ sn_temp + "', 1,'"+ m_invNumber + "'," + temp_price + " ,'" + c_supplier +"', ";
	sc += " '"+ c_supplier_code +"', 2, '"+ sProduct_desc+ "' )";
//DEBUG("sc2 = ", sc);

	//serial log
	sc += AddSerialLogString(sn_temp, "Stocked", m_poID, "", "", "");

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

bool SkipSerial()
{
	c_supplier = Request.QueryString["s"];
	c_supplier_code = Request.QueryString["sc"];

	int qty = MyIntParse(Request.QueryString["qty"]);

	string sProduct_code = "";
	string sProduct_desc = "";
	
	string ssc = "SELECT pi.* ";
	ssc += " FROM purchase_item pi ";
	ssc += " JOIN purchase po ON po.id=pi.id ";
	//ssc += " INNER JOIN product p ON p.code = pi.code AND pi.supplier_code = p.supplier_code ";
	ssc += " INNER JOIN code_relations p ON p.code = pi.code AND pi.supplier_code = p.supplier_code ";
	ssc += " WHERE po.id=" + m_poID;
	if(c_supplier_code != "")
		ssc += " AND pi.supplier_code='" + c_supplier_code + "'";
	try
	{
		myAdapter = new SqlDataAdapter(ssc, myConnection);
		if(myAdapter.Fill(dst, "codetable") <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting purchase order");
			return false;
		}
	}
	catch (Exception e)
	{
		ShowExp(ssc,e);
		return false;
	} 
	DataRow dr = dst.Tables["codetable"].Rows[0];

	m_invNumber = m_poID; //dr["inv_number"].ToString();

	double temp_price = double.Parse(dr["price"].ToString());
	sProduct_code = dr["code"].ToString();
	sProduct_desc = dr["name"].ToString();
	/* Added code to cut down length of product description on 7-11-02 tee */
	string s_swap = "";
	if(sProduct_desc.Length >= 60)
	{
		for(int i=0; i<sProduct_desc.Length; i++)
			s_swap += sProduct_desc[i].ToString();
	}
	else
		s_swap = sProduct_desc;
	/* decode the sign char in product description */
	sProduct_desc = msgEncode(sProduct_desc);

	string sn_temp = "";

//	if(!CheckSerialValidation(sn_temp))
//		return false;

	string sc = "";
	for(int i=0; i<qty; i++)
	{
		sc += " INSERT INTO stock (product_code, sn, branch_id, purchase_order_id, cost, supplier, supplier_code, status, prod_desc) ";
		sc += " VALUES ("+sProduct_code+" ,'"+ sn_temp + "', 1,'"+ m_invNumber + "'," + temp_price + " ,'" + c_supplier +"', ";
		sc += " '"+ c_supplier_code +"', 2, '"+ sProduct_desc+ "' ) ";
	}
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

bool GetInsertedSerial()
{
/*	string sc = "SELECT s.id, c.code, c.name, s.sn, s.supplier_code, s.purchase_date ";
	sc += " FROM stock s INNER JOIN code_relations c ";
	sc += " ON s.supplier_code = c.supplier_code AND s.product_code = c.code AND s.purchase_order_id = '" + m_poID + "' ";
	sc += " ORDER BY s.purchase_date DESC";
*/
	string sc = "SELECT s.id, s.product_code AS code, s.prod_desc AS name, s.sn, s.supplier_code, s.purchase_date ";
	sc += " FROM stock s ";
	sc += " WHERE s.purchase_order_id = '" + m_poID + "' ";
	sc += " ORDER BY s.purchase_date DESC";

//DEBUG("s c = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nSerialReturn = myAdapter.Fill(dst, "insertedserial");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	
	return true;
}

void BindSerialGrid()
{	
	string sBoldO = "<b>";
	string sBoldC = "</b>";
	string sBkColor = "bgcolor=#DEEEFA";
	Response.Write("<table align=center width=80% valign=center cellspacing=0 cellpadding=2 border=3 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>Code</td>");
	Response.Write("<td>M_PN</td>");
	Response.Write("<td>Description</td>\r\n");
	Response.Write("<td colspan=2>Serial Number</td>\r\n");
	
	Response.Write("</tr>\r\n");
	
	for(int i=0; i<m_nSerialReturn; i++)
	{
		DataRow dr = dst.Tables["insertedserial"].Rows[i];
		string s_code = dr["code"].ToString();
		string s_name = dr["name"].ToString();
		string s_sn	= dr["sn"].ToString();
		string s_supplier_code = dr["supplier_code"].ToString();
		string s_stock_id = dr["id"].ToString();
		if(s_name != null && s_name != "")
			s_name = StripHTMLtags(s_name);
		if(i>0)
		{
			sBoldO = "";
			sBoldC = "";
			sBkColor = "";
		}
		Response.Write("<tr><td " +sBkColor+ ">"+sBoldO+s_code+sBoldC +"</td>");
		Response.Write("<td " +sBkColor+ ">"+sBoldO+s_supplier_code+sBoldC +"</td>");
		Response.Write("<td " +sBkColor+ ">" +sBoldO+ s_name+ sBoldC +"</td>");
		Response.Write("<td " +sBkColor+ ">"+ sBoldO +s_sn+ sBoldC +"</td>");
		Response.Write("<td align=right " +sBkColor+ "><a href = 'serial.aspx?n=");
		//Response.Write(m_poID+ "&r=" +s_sn+ "&t=del&i=" +s_stock_id+ "'><font color=red size=2>DEL</a></td></tr>");
		Response.Write(m_poID+ "&r=" +s_sn+ "&sc="+ HttpUtility.UrlEncode(s_supplier_code) +"&t=del&i=" +s_stock_id+ "' onclick=\"if(!confirm('DELETING THIS SN#: "+ s_sn +"....')){return false;}\" class=o>Del</a></td></tr>");

	}

	Response.Write("<tr><td colspan=5 align=right><input type=submit name=cmd value='Search' " + Session["button_style"] + "></td></tr>");
//	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=5 align=right><input type=button name=cmd value=' Finish Enter SN# ' " + Session["button_style"]);
	Response.Write(" Onclick=window.location=('");
	if(Session["back_to_sra"] != null)
	{
		Response.Write("sra.aspx?t=replace&item_id=" + Session["sra_item_id"]);
	}
	else
		Response.Write("purchase.aspx?n=" + m_poID);
	Response.Write("')></td></tr>");
	Response.Write("</table></form>");
}

void PrintBottom()
{
	Response.Write("<br><br><br><table width=50% border=0 cellspacing=0 cellpadding=0  align=center valign=center>");
	Response.Write("<tr align=right><td><input type=button name=cmd value='Back To Purchase List'");
	Response.Write(" Onclick=window.location=('plist.aspx')></td></tr>");
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("</table>");

}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["insertedserial"]);
//	string path = Request.ServerVariables["URL"].ToString();
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

bool PrintBulkInputInterface()
{
	c_code = Request.QueryString["cd"];
	c_supplier = Request.QueryString["s"];
	c_supplier_code = Request.QueryString["sc"];
	string name = Request.QueryString["name"];

	PrintAdminHeader();
	Response.Write("<br><center><h5>" + name + "</h5>");	

	Response.Write("<form name=f method=post action=serial.aspx?t=bulk&n=");
	Response.Write(Request.QueryString["n"] + "&cd=" + HttpUtility.UrlEncode(c_code) + "&sc=" + HttpUtility.UrlEncode(c_supplier_code));
	Response.Write("&s=" + HttpUtility.UrlEncode(c_supplier));
	Response.Write("&name=" + HttpUtility.UrlEncode(name) + ">");

	Response.Write("<textarea name=sn rows=20 cols=50>");
	Response.Write("</textarea>");
	Response.Write("<br><input type=submit name=cmd value=Process class=b>");

	Response.Write("<input type=hidden name=txtBegin value='dummy'>"); //triger inputsn to accept parameter as sn

	Response.Write("</form>");

	Response.Write("<script language=javascript");
	Response.Write(">document.f.sn.focus();</script");
	Response.Write(">");

	return true;
}

bool DoProcessBulkInput()
{
	c_code = Request.QueryString["cd"];
	c_supplier = Request.QueryString["s"];
	c_supplier_code = Request.QueryString["sc"];
	string name = Request.QueryString["name"];
	m_poID = Request.QueryString["n"];

	Response.Write("<br><h4>" + name + "</h4>");
	Response.Write("<br><h5>Processing, please wait....................</h5>");
	string s = Request.Form["sn"];
	string sn = "";
	int nProcessed = 0;
	for(int i=0; i<=s.Length; i++)
	{
		if(i == s.Length || s[i] == '\r' || s[i] == '\n')
		{
			if(sn != "")
			{
				nProcessed++;
				if(!InsertSerial(sn))
				{
					if(!m_bLastErrorIsDuplicate)
					{
						Response.Write("Error, input failed, s/n : " + sn + "<br>\r\n");
						m_nBulkInputError++;
					}
					else
						m_bLastErrorIsDuplicate = false;
				}
				sn = "";
			}
			continue;
		}
		sn += s[i];
	}
	Response.Write("<br><h4>Done!</h4><br>");
	Response.Write("<table>");
	Response.Write("<tr><td align=right>Total Processed : </td><td>" + nProcessed.ToString() + "</td></tr>");
	Response.Write("<tr><td align=right>Failed : </td><td>" + m_nBulkInputError.ToString() + "</td></tr>");
	Response.Write("<tr><td align=right>Duplicates : </td><td>" + m_nBulkDuplicateFound.ToString() + "</td></tr>");
	Response.Write("</table>");

	string url = "serial.aspx?t=bulk&n=" + Request.QueryString["n"];
	url += "&cd=" + c_code + "&sc=" + c_supplier_code;
	url += "&s=" + HttpUtility.UrlEncode(c_supplier);
	url += "&name=" + HttpUtility.UrlEncode(name);

	Response.Write("<br><a href=" + url + " class=o><h5>Enter More</h5></a>");
	Response.Write("<a href=close.htm class=o><h5>Close Window</h5></a>");
	return true;
}
</script>


<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    

</asp:DataGrid>


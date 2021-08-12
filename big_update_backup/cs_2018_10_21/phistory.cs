<!-- #include file="page_index.cs" -->
<script runat=server>

string m_tableTitle = "";

string m_brand = "";
string m_cat = "";
string m_scat = "";
string m_sscat = "";
string m_code = "";

DataSet ds = new DataSet();	
DataSet dst = new DataSet();

string m_sdFrom = "";
string m_sdTo = "";
string m_poid = ""; //used for sn display
string m_datePeriod = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		m_code = Request.QueryString["code"];
	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
		m_brand = Request.QueryString["b"];
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		m_scat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		m_sscat = Request.QueryString["sscat"];

	if(Request.Form["cmd"] != null)
	{
		//string day = Request.Form["day_from"];
		//string monthYear = Request.Form["month_from"];
		//ValidateMonthDay(monthYear, ref day);
		string day = Request.Form["Datepicker1_day"];
		string monthYear = Request.Form["Datepicker1_month"] + "-" +Request.Form["Datepicker1_year"];
		m_sdFrom = day + "-" + monthYear;

		//day = Request.Form["day_to"];
		//monthYear = Request.Form["month_to"];
		//ValidateMonthDay(monthYear, ref day);
		day = Request.Form["Datepicker2_day"];
		monthYear = Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];
		m_sdTo = day + "-" + monthYear;
	}
	else
	{
		m_sdTo = DateTime.Now.ToString("dd-MM-yyyy");
		m_sdFrom = DateTime.Now.AddMonths(-3).ToString("dd-MM-yyyy");
	}

	m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
	m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";

	if(m_sdTo != "")
	{
		System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
		DateTime dTo = DateTime.Parse(m_sdTo, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault);
		m_sdTo = dTo.AddDays(1).ToString("dd-MM-yyyy");
	}

	PrintHeader();

	if(Request.Form["cmd"] != null)
	{
		if(Request.Form["b"] != null && Request.Form["b"] != "")
			m_brand = Request.Form["b"];
		if(Request.Form["c"] != null && Request.Form["c"] != "")
			m_cat = Request.Form["c"];
		if(Request.Form["s"] != null && Request.Form["s"] != "")
			m_scat = Request.Form["s"];
		if(Request.Form["ss"] != null && Request.Form["ss"] != "")
			m_sscat = Request.Form["ss"];
		if(Request.Form["code"] != null && Request.Form["code"] != "")
			m_code = Request.Form["code"];
		DoSRItem();
	}
}

bool PrintHeader()
{
	PrintAdminHeader();
	PrintAdminMenu();

//	if(m_sCompanyName == "demo")// && Request.QueryString["np"] == null)
//		PrintPatentInfomation("viewsales");

	Response.Write("<br><center><h3>Brand Purchase History</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");
//	Response.Write("</center>");

	Response.Write("<form name=f action=phistory.aspx method=post>");
	Response.Write("<table align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>");
	Response.Write("<tr><td>");
	Response.Write("<table><tr><td>");
	PrintDateSelector();
	Response.Write("</td></tr></table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	DoItemOption();
	Response.Write("<input type=submit name=cmd value='View Report' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");

	return true;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
bool DoSRItem()
{
	Trim(ref m_code);
	Trim(ref m_cat);
	Trim(ref m_scat);
	Trim(ref m_sscat);

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.*, p.date_invoiced, p.inv_number, p.po_number ";
	sc += ", e.name AS currency_name, p.exchange_rate  ";
	sc += ", card.trading_name AS supplier_name ";
	sc += " FROM purchase_item i JOIN purchase p ON i.id = p.id ";
	sc += " JOIN code_relations c ON c.code = i.code ";
	sc += " LEFT OUTER JOIN card ON card.id = p.supplier_id ";
	sc += " LEFT OUTER JOIN enum e ON e.class='currency' AND e.id = p.currency ";
	sc += " WHERE p.date_invoiced >= '" + m_sdFrom + "' AND p.date_invoiced <= '" + m_sdTo + "' ";
	if(m_code != "")
		sc += " AND i.code = " + m_code + " ";
	else
	{
		if(m_brand != "")
			sc += " AND c.brand = '" + m_brand + "' ";
		if(m_cat != "")
			sc += " AND c.cat = '" + m_cat + "' ";
		if(m_scat != "")
			sc += " AND c.s_cat = '" + m_scat + "' ";
		if(m_sscat != "")
			sc += " AND c.ss_cat = '" + m_sscat + "' ";
	}
	sc += " ORDER BY p.date_invoiced ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "report");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	BindSRItem();

	return true;
}

/////////////////////////////////////////////////////////////////
void BindSRItem()
{
	int i = 0;

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=4>");
	//main list
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=yellow>");
	Response.Write("<th nowrap>Inv Date</th>");
//	Response.Write("<th nowrap><a href=viewpurchase.aspx?code=" + m_code + "&ob=date_invoiced class=o>Inv Date</a></th>");
	Response.Write("<th nowrap>Supplier</th>");
	Response.Write("<th nowrap>Supplier Inv#</th>");
	Response.Write("<th nowrap>PO No.</th>");
	Response.Write("<th nowrap>Code</th>");
	Response.Write("<th nowrap>M_PN</th>");
	Response.Write("<th nowrap>Description</th>");
	Response.Write("<th nowrap>Qty</th>");
	Response.Write("<th nowrap>Cost_FRD</th>");
	Response.Write("<th nowrap>EX_RATE</th>");
	Response.Write("<th nowrap>Cost_NZD</th>");
	Response.Write("</tr>");

	bool bAlterColor = false;
	for(i=0; i<ds.Tables["report"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["report"].Rows[i];
		string po_id = dr["id"].ToString();
		string date = DateTime.Parse(dr["date_invoiced"].ToString()).ToString("dd-MM-yyyy");
		string invoice_number = dr["inv_number"].ToString();
		string po_number = dr["po_number"].ToString();
		string code = dr["code"].ToString();
		string supplier = dr["supplier_name"].ToString();
		string m_pn = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		double ex_rate = MyDoubleParse(dr["exchange_rate"].ToString());
		double price = MyDoubleParse(dr["price"].ToString());
		double price_nzd = Math.Round(price / ex_rate, 2);

		Response.Write("<tr");	
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + supplier + "</td>");
		Response.Write("<td>" + invoice_number + "</td>");
		Response.Write("<td><a href=purchase.aspx?t=pp&n=" + po_id + " class=o target=_blank>" + po_number + "</a></td>");
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + m_pn + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td>" + qty + "</td>");
		Response.Write("<td align=right>" + price.ToString("c") + "</td>");
		Response.Write("<td align=right>" + ex_rate + "</td>");
		Response.Write("<td align=right>" + price_nzd.ToString("c") + "</td>");

		Response.Write("</tr>");
	}

	Response.Write("</table>");

	Response.Write("</td></tr></table>");
}

void PrintDateSelector()
{
	int i = 1;
	datePicker(); //call date picker function from common.cs
	Response.Write("<tr><td>");
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<tr><td>");
	Response.Write("<b>Select : </b> From Date ");
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		//if(int.Parse(s_day) == d)
		//	Response.Write("<option value="+ d +" selected>"+d+"</option>");
		//else
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		if(m == 1)
			txtMonth = "JAN";
		if(m == 2)
			txtMonth = "FEB";
		if(m == 3)
			txtMonth = "MAR";
		if(m == 4)
			txtMonth = "APR";
		if(m == 5)
			txtMonth = "MAY";
		if(m == 6)
			txtMonth = "JUN";
		if(m == 7)
			txtMonth = "JUL";
		if(m == 8)
			txtMonth = "AUG";
		if(m == 9)
			txtMonth = "SEP";
		if(m == 10)
			txtMonth = "OCT";
		if(m == 11)
			txtMonth = "NOV";
		if(m == 12)
			txtMonth = "DEC";
		if(int.Parse(s_month) == m)
			Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
		else
			Response.Write("<option value="+m+">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
	for(int y=1997; y<int.Parse(s_year)+1; y++)
	{
		if(int.Parse(s_year) == y)
			Response.Write("<option value="+y+" selected>"+y+"</option>");
		else
			Response.Write("<option value="+y+">"+y+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name='Datepicker1'>");
	Response.Write("<input type=hidden name='Datepicker1_conv'>");
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker1',1)");
	Response.Write("</script ");
	Response.Write(">");
	Response.Write("</td>");
		//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
	Response.Write("<select name='Datepicker2_day' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		if(m == 1)
			txtMonth = "JAN";
		if(m == 2)
			txtMonth = "FEB";
		if(m == 3)
			txtMonth = "MAR";
		if(m == 4)
			txtMonth = "APR";
		if(m == 5)
			txtMonth = "MAY";
		if(m == 6)
			txtMonth = "JUN";
		if(m == 7)
			txtMonth = "JUL";
		if(m == 8)
			txtMonth = "AUG";
		if(m == 9)
			txtMonth = "SEP";
		if(m == 10)
			txtMonth = "OCT";
		if(m == 11)
			txtMonth = "NOV";
		if(m == 12)
			txtMonth = "DEC";
		if(int.Parse(s_month) == m)
			Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
		else
			Response.Write("<option value="+m+">"+txtMonth+"</option>");
	}
	
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_year' onChange=\"tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int y=1997; y<int.Parse(s_year)+1; y++)
	{
		if(int.Parse(s_year) == y)
			Response.Write("<option value="+y+" selected>"+y+"</option>");
		else
			Response.Write("<option value="+y+">"+y+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<input type=hidden name='Datepicker2'>");
	Response.Write("<input type=hidden name='Datepicker2_conv'>");
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker2',1)");
	Response.Write("</script ");
	Response.Write(">");
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT brand FROM product ";
	sc += " ORDER BY brand";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "brand");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<select name=b ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?cat=" + HttpUtility.UrlEncode(m_cat));
	Response.Write("&scat=" + HttpUtility.UrlEncode(m_scat));
	Response.Write("&sscat=" + HttpUtility.UrlEncode(m_sscat));
	Response.Write("&b=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value=''>All Brand</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["brand"].Rows[i];
		string s = dr["brand"].ToString();
		Trim(ref s);
		if(m_brand == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "</option>");
		else
			Response.Write("<option value='"+s+"'>" +s+ "</option>");
	}
	Response.Write("</select>");


	sc = "SELECT DISTINCT cat FROM product ";
	if(m_brand != "")
		sc += " WHERE brand = '" + m_brand + "' ";
	sc += " ORDER BY cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;
	Response.Write("<select name=c ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?b=" + HttpUtility.UrlEncode(m_brand));
	Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value=''>Show All</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(m_cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(m_cat != "")
	{
		sc = "SELECT DISTINCT s_cat FROM product ";
		sc += " WHERE cat = '" + m_cat + "' ";
		if(m_brand != "")
			sc += " AND brand = '" + m_brand + "' ";
		sc += " ORDER BY s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(m_cat));
		Response.Write("&b=" + HttpUtility.UrlEncode(m_brand));
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value=''>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(m_scat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}

		Response.Write("</select>");
	}
	
	if(m_scat != "")
	{
		sc = "SELECT DISTINCT ss_cat FROM product ";
		sc += " WHERE cat = '"+ m_cat +"' ";
		sc += " AND s_cat = '"+ m_scat +"' ";
		if(m_brand != "")
			sc += " AND brand = '" + m_brand + "' ";
		sc += " ORDER BY ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=ss ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(m_cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(m_scat));
		Response.Write("&b=" + HttpUtility.UrlEncode(m_brand));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value=''>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(m_sscat == s)
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}

	if(m_sscat != "")
	{
		sc = "SELECT code, name FROM product ";
		sc += " WHERE cat = '"+ m_cat +"' ";
		sc += " AND s_cat = '"+ m_scat +"' ";
		sc += " AND ss_cat = '"+ m_sscat +"' ";
		if(m_brand != "")
			sc += " AND brand = '" + m_brand + "' ";
		sc += " ORDER BY name";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "item");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=code>");
		Response.Write("<option value=''>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["item"].Rows[i];
			string code = dr["code"].ToString();
			string name = dr["name"].ToString();
			Trim(ref name);
			if(code == m_code)
				Response.Write("<option value='" + code + "' selected>" + name + "</option>");
			else
				Response.Write("<option value='" + code + "'>" + name + "</option>");
		}
		Response.Write("</select>");
	}

	return true;
}

</script>

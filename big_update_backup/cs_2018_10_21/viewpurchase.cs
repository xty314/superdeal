<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@Import Namespace="ASPNet_Drawing" %>

<!-- #include file="page_index.cs" -->
<script runat=server>

string m_tableTitle = "";
string m_code = "";

StringBuilder m_sb = new StringBuilder();  //xml data for 3d chart
string m_picFile = "";
double m_nMaxY = 0;
double m_nMinY = 0;
double m_nMaxX = 0;
bool m_bHasLegends = true;
string m_xLabel = "";
string m_yLabel = "";
string[] m_IslandTitle = new string[64];
int m_nIsland = 0;

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string[] sct = new string[16];
int cts = 0;
int m_ct = 1;

bool m_bPurchase = true;

string m_sdFrom = "";
string m_sdTo = "";
string m_poid = ""; //used for sn display
string m_datePeriod = "";
string m_sCurrencyName="NZD";
void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	m_sCurrencyName=GetSiteSettings("default_currency_name", "NZD");
	if(!SecurityCheck(""))
		return;

//	if(Request.QueryString["pu"] != null)
		m_bPurchase = true;

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		m_code = Request.QueryString["code"];
	else
	{
		Response.Write("<br><center><h3>No code?</h3>");
		return;
	}

	if(Request.QueryString["t"] == "1")
		Session["viewsales_type"] = "1";
	if(Request.QueryString["t"] == "0")
		Session["viewsales_type"] = null;

	int i = 0;
	sct[i++] = "Bar Graph 2D";
	sct[i++] = "Bar Graph 3D"; 
	sct[i++] = "Blocks Chart 2D"; 
	sct[i++] = "Blocks Chart 3D"; 
	sct[i++] = "Pie Chart 2D"; 
	sct[i++] = "Pie Chart 3D"; 
	sct[i++] = "Stacked Bar 2D"; 
	sct[i++] = "Stacked Bar 3D"; 
	sct[i++] = "Line Graph 2D"; 
	sct[i++] = "Line Area Graph"; 
	sct[i++] = "Point Chart"; 
	sct[i++] = "Spine Graph 2D"; 
	sct[i++] = "Spine Area Graph"; 
	cts = i;

	if(Request.Form["chart_type"] != null)
		m_ct = MyIntParse(Request.Form["chart_type"]);

	if(Request.Form["Datepicker1_day"] != null)
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

//	if(Request.Form["legends"] != "on")
//		m_bHasLegends = false;

	string header = @"
<html><head>
<title>Salse History (quantity)</title>
<style type=text/css>
td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:vardana;}
body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}
a{color:#000000;text-decoration:none} a:hover{color:red;text-decoration:none} a.d:hover{COLOR:#FF0000;TEXT-DECORATION:none}
a{color:blue;text-decoration:underline} a:hover{color:red;text-decoration:none} a.d:hover{COLOR:#FF0000;TEXT-DECORATION:none}
</style>
</head>
<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0 bgcolor=aliceblue>
		";
		Response.Write(header);

	if(m_sCompanyName == "demo")// && Request.QueryString["np"] == null)
		PrintPatentInfomation("viewsales");

	if(Request.QueryString["sn"] == "1")
	{
		m_poid = Request.QueryString["pid"];
		if(m_poid == null || m_poid == "")
		{
			Response.Write("<br><center><h3>Error, no puchase order id</h3>");
			return;
		}
		DoSN();
	}
	else
		DoSRItem();

}

bool DoSN()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT sn FROM stock WHERE product_code = " + m_code + " AND purchase_order_id = " + m_poid;
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

	sc = " SELECT name, supplier_code FROM code_relations WHERE code = " + m_code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "item");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	BindSN();

	return true;
}

void BindSN()
{
	int i = 0;

	DataRow dr = ds.Tables["item"].Rows[0];
/*	string name = dr["name"].ToString();
	string supplier_code = dr["supplier_code"].ToString();

	string title = "Serial Number List";
	Response.Write("<center><h3>" + title + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td><font size=+1>" + name + "</font>");
	Response.Write(" &nbsp&nbsp; <b> Code : </b><a href=liveedit.aspx?code=" + m_code + " class=o target=_blank>" + m_code + "</a>");
	Response.Write(" &nbsp&nbsp; <b>M_PN : </b>" + supplier_code);
	Response.Write(" &nbsp&nbsp; <b>Purchase ID : </b><a href=purchase.aspx?t=pp&n=" + m_poid + " class=o target=_blank>" + m_poid + "</a>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
*/
	//main list
	for(i=0; i<ds.Tables["report"].Rows.Count; i++)
	{
		dr = ds.Tables["report"].Rows[i];
		Response.Write(dr["sn"].ToString() + "<br>");
	}

//	Response.Write("</td></tr></table>");
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool DoSRItem()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT i.*, p.date_invoiced, p.inv_number, p.po_number, e.name AS currency_name, p.exchange_rate ";
	sc += ", card.trading_name AS supplier_name ";
	sc += " FROM purchase_item i JOIN purchase p ON i.id = p.id ";
	sc += " LEFT OUTER JOIN card ON card.id = p.supplier_id ";
	sc += " LEFT OUTER JOIN enum e ON e.class='currency' AND e.id = p.currency ";
	sc += " WHERE p.date_invoiced >= '" + m_sdFrom + "' AND p.date_invoiced <= '" + m_sdTo + "' ";
	sc += " AND i.code = " + m_code;
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

	sc = " SELECT name, supplier_code FROM code_relations WHERE code = " + m_code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "item");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	if(ds.Tables["item"].Rows.Count > 0)
		BindSRItem();

	return true;
}

/////////////////////////////////////////////////////////////////
void BindSRItem()
{
	int i = 0;

	DataRow dr = ds.Tables["item"].Rows[0];
	string name = dr["name"].ToString();
	string supplier_code = dr["supplier_code"].ToString();

	string title = "Sales History";
	if(m_bPurchase)
		title = "Purchase History";
	Response.Write("<center><h3>" + title + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td><font size=+1>" + name + "</font>");
	Response.Write(" &nbsp&nbsp; <b> Code : </b><a href=liveedit.aspx?code=" + m_code + " class=o target=_blank>" + m_code + "</a>");
	Response.Write(" &nbsp&nbsp; <b>M_PN : </b>" + supplier_code + "</td></tr>");

	Response.Write("<tr><td colspan=4>");
	//main list
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=yellow>");
	Response.Write("<th nowrap>Inv Date</th>");
//	Response.Write("<th nowrap><a href=viewpurchase.aspx?code=" + m_code + "&ob=date_invoiced class=o>Inv Date</a></th>");
	Response.Write("<th nowrap>Supplier</th>");
	Response.Write("<th nowrap>Inv Number</th>");
	Response.Write("<th nowrap>PO Number</th>");
	Response.Write("<th nowrap>Qty</th>");
	Response.Write("<th nowrap>Cost_FRD</th>");
	Response.Write("<th nowrap>EX_RATE</th>");
	Response.Write("<th nowrap>Cost_"+m_sCurrencyName+"</th>");
	Response.Write("<th nowrap>S/N</th>");
	Response.Write("</tr>");

	bool bAlterColor = false;
	for(i=0; i<ds.Tables["report"].Rows.Count; i++)
	{
		dr = ds.Tables["report"].Rows[i];
		string po_id = dr["id"].ToString();
		string date = DateTime.Parse(dr["date_invoiced"].ToString()).ToString("dd-MM-yyyy");
		string invoice_number = dr["inv_number"].ToString();
		string po_number = dr["po_number"].ToString();
		string supplier = dr["supplier_name"].ToString();
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
		Response.Write("<td>" + qty + "</td>");
		Response.Write("<td>" + price.ToString("c") + "</td>");
		Response.Write("<td>" + ex_rate + "</td>");
		Response.Write("<td>" + price_nzd.ToString("c") + "</td>");
//		Response.Write("<td><a href=viewpurchase.aspx?sn=1&pid=" + po_id + "&code=" + m_code + " class=o target=_blank>View</td>");

		Response.Write("<td>");
		Response.Write("<input type=button title='View S/N' onclick=\"javascript:viewsn_window=window.open('viewpurchase.aspx?");
		Response.Write("code=" + m_code + "&sn=1&pid=" + po_id + "','','width=400,height=300');\" value='View' " + Session["button_style"] + ">");
		Response.Write("</td>");

		Response.Write("</tr>");
	}

	Response.Write("</table>");

	Response.Write("</td></tr></table>");

	Response.Write("<form name=f action=viewpurchase.aspx?code=" + m_code + " method=post>");
	PrintDateSelector();
	Response.Write(" &nbsp&nbsp; <input type=submit name=cmd value=GO " + Session["button_style"] + ">");
	Response.Write("</form>");
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
</script>

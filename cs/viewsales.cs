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

bool m_bPurchase = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["pu"] != null)
		m_bPurchase = true;

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		m_code = Request.QueryString["code"];

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

//	if(Request.Form["legends"] != "on")
//		m_bHasLegends = false;

	string header = @"
<html><head>
<title>Salse History (quantity)</title>
<style type=text/css>
td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:vardana;}
body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}
a{color:#000000;text-decoration:none} a:hover{color:red;text-decoration:none} a.d:hover{COLOR:#FF0000;TEXT-DECORATION:none}
</style>
</head>
<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0 bgcolor=aliceblue>
		";
		Response.Write(header);

	Response.Write("<a href=viewsales.aspx?");
	if(m_bPurchase)
		Response.Write("pu=1&");
	Response.Write("t=");
	if(Session["viewsales_type"] == null)
		Response.Write("1");
	else
		Response.Write("0");
	Response.Write("&code=" + m_code + "><font color=red><b>x</b></font></a>");
	Response.Write("<a href=viewsales.aspx?");
	if(!m_bPurchase)
		Response.Write("pu=1&");
	Response.Write("code=" + m_code + "><font color=green><b>");
	if(m_bPurchase)
		Response.Write("s");
	else
		Response.Write("p");
	Response.Write("</b></font></a>");

	if(m_sCompanyName == "demo")// && Request.QueryString["np"] == null)
		PrintPatentInfomation("viewsales");

	DoSRItem();

}

void DrawChart()
{
	Stream chartFile = null;
	XmlDocument obXmlDoc = null;
	string uname = EncodeUserName();
	m_picFile = "./ri/" + uname + DateTime.Now.ToString("ddMMyyyyHHmmss") + "_datachart.jpg";
	string strFileName = m_picFile;
	string strDataXmlFile = "./ri/" + uname + "_chartdata.xml";
	try
	{
		chartFile = new FileStream(Server.MapPath(strFileName),
				FileMode.Create, FileAccess.ReadWrite);
		obXmlDoc = new XmlDocument();
		obXmlDoc.Load(Server.MapPath(strDataXmlFile));
		Graph dc = new BarGraph3D();
		switch(m_ct)
		{
		case 0:
			dc = new BarGraph2D();
			break;
		case 2:
			dc = new BlocksChart2D();
			break;
		case 3:
			dc = new BlocksChart3D();
			break;
		case 4:
			dc = new PieGraph2D();
			break;
		case 5:
			dc = new PieGraph3D();
			break;
		case 6:
			dc = new StackBarGraph2D();
			break;
		case 7:
			dc = new StackBarGraph3D();
			break;
		case 8:
			dc = new LineGraph2D();
			break;
		case 9:
			dc = new LineGraph3D();
			break;
		case 10:
			dc = new PointGraph();
			break;
		case 11:
			dc = new SplineGraph();
			break;
		case 12:
			dc = new SplineAreaGraph();
			break;
		default :
			break;
		}

		dc.SetGraphData (obXmlDoc);
		dc.Title = "";
		for(int n=0; n<m_nIsland; n++)
			dc.IslandTitle[n] = m_IslandTitle[n];
		dc.HasTickers = false;
		dc.HasGridLines = true;
		dc.HasLegends = m_bHasLegends;
		dc.Width = 500;
		dc.Height = 350;
		dc.MinXValue = 0;
//		dc.XTickSpacing = 100;
		dc.MaxXValue = (int)m_nMaxX;
		dc.MinYValue = (int)m_nMinY;
		dc.MaxYValue = (int)m_nMaxY;
		dc.YAxisLabel = m_yLabel;
		dc.XAxisLabel = m_xLabel;
		dc.AxisPaddingTop = 50;
		dc.AxisPaddingBottom = 100;
		dc.YTickSpacing = (int)((m_nMaxY - m_nMinY)/ 10);
		dc.XTickSpacing = 1000;
//		dc.TickStep = 100;
		dc.TitlePaddingFromTop = 30;
//		dc.XAxisLabelColor = Color.Red;
		dc.DrawGraph(ImageFormat.Jpeg, ref chartFile);
		chartFile.Close();

		chartFile = null;
	}
	catch(Exception ex)
	{
		ShowExp("draw chart error", ex);
	}
	finally
	{
		if(null != chartFile)
		{
			chartFile.Close();
		}
	}
	Response.Write("<img src=" + m_picFile + ">");
}

void WriteXMLFile()
{
	string s = "";
	s += "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n";
	s += "<chartdatalist>\r\n";
//	s += "<chartdataisland>\r\n";

	s += m_sb.ToString();

//	s += "</chartdataisland>\r\n";
	s += "</chartdatalist>\r\n";

	string uname = EncodeUserName();
	string strPath = Server.MapPath("./ri");
	strPath += "\\" + uname + "_chartdata.xml";
	
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	TextWriter tw = new StreamWriter(newFile);
	
	// Write data to the file
	tw.Write(s.ToCharArray());
	tw.Flush();
	
	newFile.Close();
}

///////////////////////////////////////////////////////////////////////////////////////////////////////
//Item based
bool DoSRItem()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT ";

	if(m_bPurchase)
	{
		for(int i=1; i<=12; i++)
		{
			if(i > 1)
				sc += ",";
			sc += " ( ";
			sc += " SELECT SUM(i.qty) FROM purchase_item i JOIN purchase p ON p.id=i.id ";
			sc += " WHERE DATEDIFF(WEEK, p.date_create, GETDATE()) = " + (i-1).ToString() + " AND i.code = " + m_code;
			sc += " ) AS 'week" + i.ToString() + "' ";
		}
		for(int i=1; i<=12; i++)
		{
			sc += ", ( ";
			sc += " SELECT SUM(i.qty) FROM purchase_item i JOIN purchase p ON p.id=i.id ";
			sc += " WHERE DATEDIFF(MONTH, p.date_create, GETDATE()) = " + (i-1).ToString() + " AND i.code = " + m_code;
			sc += " ) AS 'month" + i.ToString() + "' ";
		}
	}
	else
	{
		for(int i=1; i<=12; i++)
		{
			if(i > 1)
				sc += ",";
			sc += " ( ";
			sc += " SELECT SUM(s.quantity) FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
			sc += " WHERE DATEDIFF(WEEK, i.commit_date, GETDATE()) = " + (i-1).ToString() + " AND s.code = " + m_code;
			sc += " ) AS 'week" + i.ToString() + "' ";
		}
		for(int i=1; i<=12; i++)
		{
			sc += ", ( ";
			sc += " SELECT SUM(s.quantity) FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
			sc += " WHERE DATEDIFF(MONTH, i.commit_date, GETDATE()) = " + (i-1).ToString() + " AND s.code = " + m_code;
			sc += " ) AS 'month" + i.ToString() + "' ";
		}
	}
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
		if(myAdapter.Fill(ds, "item") <=  0)
		{
			Response.Write("<br><center><h3>No Record</h3>");
			return false;
		}

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	if(!m_bPurchase)
	{
		sc = " SELECT TOP 5 i.invoice_number, i.commit_date, s.code, s.commit_price, s.name,s.supplier_price, s.quantity  ";
		sc += " FROM invoice i JOIN sales s ON s.invoice_number = i.invoice_number ";
		sc += " WHERE s.code = "+ m_code +"";
		if(Request.QueryString["cid"] != "" && Request.QueryString["cid"] != null)
			sc += " AND i.card_id = "+ Request.QueryString["cid"] +"";
		sc += " ORDER BY i.commit_date DESC ";
	//DEBUG("sc = ", sc);	
		int rows = 0;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "history") <=0)
			{
				Response.Write("<br><center><h3>No Sales History With this ID: <a title='view card details' <a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Request.QueryString["cid"] + "', '', 'width=350, height=350'); viewcard_window.focus();\" class=o><u>"+ Request.QueryString["cid"] +"</a></u> &nbsp;</h3>");
			//	return false;
			}

		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
//	Response.Write("<center><h3>" + m_tableTitle + "</h3>");


	if(Session["viewsales_type"] == null)
	{
		Bind2DChart();
	}
	else
	{
		BindSRItem();
		DrawChart();
	}

	return true;
}

/////////////////////////////////////////////////////////////////
void Bind2DChart()
{
	int i = 0;

	if(ds.Tables.Count <=0)
		return;
	if(ds.Tables["item"].Rows.Count <= 0)
		return;

	DataRow dr = ds.Tables["item"].Rows[0];
	string name = dr["name"].ToString();
	string supplier_code = dr["supplier_code"].ToString();

	dr = ds.Tables["report"].Rows[0];
	int[] week = new int[16];
	int[] month = new int[16];

	int max = 0;
	int wtotal = 0;
	int mtotal = 0;
	string s = "";
	int q = 0;
	for(i=1; i<=12; i++)
	{
		q = MyIntParse(Math.Round(MyDoubleParse(dr["week" + i].ToString()), 0).ToString());
		wtotal += q;
		week[i] = q;
	}

	for(i=1; i<=12; i++)
	{
		q = MyIntParse(Math.Round(MyDoubleParse(dr["month" + i].ToString()), 0).ToString());
		mtotal += q;
		month[i] = q;
	}
	double dRate = 200;
	if(m_bPurchase)
		dRate = 100;
	max = wtotal;
	if(max < mtotal)
		max = mtotal;
	if(max <= 0)
		max = (int)dRate;
	double rate = dRate / (double)max;
//DEBUG("r=", rate.ToString());

	string title = "Sales History";
	if(m_bPurchase)
		title = "Purchase History";
	Response.Write("<center><h3>" + title + "</h3>");

	//-----start show sales price and cost from here ---
	if(!m_bPurchase)
	{
	if(ds.Tables["history"].Rows.Count > 0)
	{
		bool bAlter = false;
//		Response.Write("<tr><td colspan=2>");
		Response.Write("<table width=90% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td colspan=3><b>Code : </b>" + m_code + "</td>");
		Response.Write("<td colspan=3><b>M_PN : </b>" + supplier_code + "</td></tr>");
		Response.Write("<tr><td colspan=5><font size=+1>" + name + "</font></td></tr>");
		Response.Write("<tr bgcolor=#F3DAD align=left><th>INV#</td><th>DATE</td><th>COST</td><th>QTY</th><th>SALES PRICE</td></tr>");
		for(int j=0; j<ds.Tables["history"].Rows.Count; j++)
		{
			dr = ds.Tables["history"].Rows[j];
			string invoice = dr["invoice_number"].ToString();
		//	string code = dr["code"].ToString();
			string sold_price = dr["commit_price"].ToString();
			string cost = dr["supplier_price"].ToString();
			string sold_date = dr["commit_date"].ToString();
			string qty = dr["quantity"].ToString();
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
			bAlter = !bAlter;
			Response.Write(">");
			Response.Write("<td>");
			Response.Write(invoice);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(sold_date);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(double.Parse(cost).ToString("c"));
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(qty);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(double.Parse(sold_price).ToString("c"));
			Response.Write("</td>");
	
			Response.Write("</tr>");

		}
		
		Response.Write("</table><br><br>");
//		Response.Write("</td></tr>");
	}
	}
//--- end show sales price and cost ---

	Response.Write("<table align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>");

	Response.Write("<tr><td><b>Code : </b>" + m_code + "</td>");
	Response.Write("<td><b>M_PN : </b>" + supplier_code + "</td></tr>");
	Response.Write("<tr><td colspan=2><font size=+1>" + name + "</font></td></tr>");
	
	Response.Write("<tr><td colspan=2><table border=0>");
	for(i=1; i<=12; i++)
	{
		Response.Write("<tr>");
		Response.Write("<td nowrap>Week " + i.ToString() + " : </td>");
		Response.Write("<td nowrap><b>" + week[i] + "</b></td>");
		Response.Write("<td nowrap>");
		if(week[i] > 0)
		{
			double dw = ((double)week[i])*rate;
//DEBUG("dw=", dw.ToString());
			Response.Write("<table cellspacing=0 cellpadding=0 width=" + (int)dw + "><tr><td bgcolor=red>&nbsp;</td></tr></table>");
		}
		Response.Write("</td>");

		Response.Write("<td>&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;</td>");

		Response.Write("<td nowrap>Month " + i.ToString() + " : </td>");
		Response.Write("<td nowrap><b>" + month[i] + "</b></td>");
		Response.Write("<td nowrap>");
		if(month[i] > 0)
		{
			double dw = ((double)month[i])*rate;
			Response.Write("<table cellspacing=0 cellpadding=0 width=" + (int)dw + "><tr><td bgcolor=red>&nbsp;</td></tr></table>");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=7><hr></td></tr>");
	Response.Write("<tr><td><b>Total : </b></td><td><b>" + wtotal + "</b></td>");
	Response.Write("<td>&nbsp;</td><td>&nbsp;</td>");
	Response.Write("<td><b>Total : </b></td><td><b>" + mtotal + "</b></td>");
	Response.Write("</tr>");
	Response.Write("</table></td></tr>");

	Response.Write("</table>");
}

/////////////////////////////////////////////////////////////////
void BindSRItem()
{
	int i = 0;

	DataRow dr = ds.Tables["item"].Rows[0];
	string name = dr["name"].ToString();
	string supplier_code = dr["supplier_code"].ToString();

	dr = ds.Tables["report"].Rows[0];
	int[] week = new int[16];
	int[] month = new int[16];

	int total = 0;
	int aweek = 0;
	int amonth = 0;
	string s = "";
	int q = 0;
	for(i=1; i<=12; i++)
	{
		q = MyIntParse(dr["week" + i].ToString());
		total += q;
		week[i] = q;
	}
	aweek = total / 12;

	total = 0;
	for(i=1; i<=6; i++)
	{
		q = MyIntParse(dr["month" + i].ToString());
		total += q;
		month[i] = q;
	}
	amonth = total / 3;

	string title = "Sales History";
	if(m_bPurchase)
		title = "Purchase History";
	Response.Write("<center><h3>" + title + "</h3>");

	//-----start show sales price and cost from here ---
	if(!m_bPurchase)
	{
	if(ds.Tables["history"].Rows.Count > 0)
	{
		bool bAlter = false;
		
		Response.Write("<table width=90%  align=center valign=center cellspacing=2 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td>CODE : </td><td colspan=3><b>" + m_code + "</b></td></tr>");
		Response.Write("<tr><td colspan=4><font size=+1>" + name + "</font></td></tr>");

		Response.Write("<tr align=left bgcolor=#F3ADE><th>INV#</td><th>DATE</td><th>COST</td><th>SALES PRICE</td></tr>");
		for(int j=0; j<ds.Tables["history"].Rows.Count; j++)
		{
			dr = ds.Tables["history"].Rows[j];
			string invoice = dr["invoice_number"].ToString();
		//	string code = dr["code"].ToString();
			string sold_price = dr["commit_price"].ToString();
			string cost = dr["supplier_price"].ToString();
			string sold_date = dr["commit_date"].ToString();
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
			bAlter = !bAlter;
			Response.Write(">");
			Response.Write("<td>");
			Response.Write(invoice);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(sold_date);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(cost);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(sold_price);
			Response.Write("</td>");
	
			Response.Write("</tr>");

		}
		
		Response.Write("</table><br>");
		
	}
	}
//--- end show sales price and cost ---

	Response.Write("<table width=90%  align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=2><font size=+1>" + name + "</font></td></tr>");
	Response.Write("<tr><td valign=top>");

	Response.Write("<table>");
	Response.Write("<tr><td align=right>Code : </td><td><b>" + m_code + "</b></td></tr>");
	Response.Write("<tr><td align=right>This Week : </td><td><font color=red><b>" + week[1] + "</b></font></td></tr>");
	Response.Write("<tr><td align=right>Last Week : </td><td><font color=red><b>" + week[2] + "</b></font></td></tr>");
	Response.Write("<tr><td align=right>Week Before Last Week : </td><td><font color=red><b>" + week[3] + "</b></font></td></tr>");
	Response.Write("<tr><td align=right>Average in Last 3 months : </td><td><font color=red><b>" + aweek + "</b></font></td></tr>");
	Response.Write("</table>");

	Response.Write("</td><td valign=top>");

	Response.Write("<table>");
	Response.Write("<tr><td align=right>M_PN : </td><td><b>" + supplier_code + "</b></td></tr>");
	Response.Write("<tr><td align=right>This Month : </td><td><font color=red><b>" + month[1] + "</b></font></td></tr>");
	Response.Write("<tr><td align=right>Last Month : </td><td><font color=red><b>" + month[2] + "</b></font></td></tr>");
	Response.Write("<tr><td align=right>Month Before Last Month : </td><td><font color=red><b>" + month[3] + "</b></font></td></tr>");
	Response.Write("<tr><td align=right>Average in Last 6 months : </td><td><font color=red><b>" + amonth + "</b></font></td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr>");
	Response.Write("</table>");

	StringBuilder sb1 = new StringBuilder();
	StringBuilder sb2 = new StringBuilder();
	string legend = "";
	string x = "";
	string y = "";

	//weekly legend
	for(i=1; i<=3; i++)
	{
		if(week[i] > m_nMaxY)
			m_nMaxY = week[i];

		if(week[i] < 0 && week[i] < m_nMinY)
			m_nMinY = week[i];

		//xml chart data
		x = (i).ToString();

		y = week[i].ToString();
		legend = "";
/*		if(i == 1)
			legend = "This Week";
		else if(i == 2)
			legend = "Last Week";
		else
			legend = "Week b4 Last Week";
*/
		sb1.Append("<chartdata>\r\n");
		sb1.Append("<x");
		if(m_bHasLegends)
			sb1.Append(" legend='" + legend + "'");
		sb1.Append(">" + x + "</x>\r\n");
		sb1.Append("<y>" + y + "</y>\r\n");
		sb1.Append("</chartdata>\r\n");
	}
	i++;
	m_nMaxX = i;
	y = aweek.ToString();
	legend = "Average";
	sb1.Append("<chartdata>\r\n");
	sb1.Append("<x");
	if(m_bHasLegends)
		sb1.Append(" legend='" + legend + "'");
	sb1.Append(">" + x + "</x>\r\n");
	sb1.Append("<y>" + y + "</y>\r\n");
	sb1.Append("</chartdata>\r\n");

	//monthly legend
	for(i=1; i<=3; i++)
	{
		if(month[i] > m_nMaxY)
			m_nMaxY = month[i];

		if(month[i] < 0 && month[i] < m_nMinY)
			m_nMinY = month[i];

		//xml chart data
		x = i.ToString();

		y = month[i].ToString();
		legend = "";
		if(i == 1)
			legend = "This Week";
		else if(i == 2)
			legend = "Last Week";
		else
			legend = "Week b4 Last Week";
		sb2.Append("<chartdata>\r\n");
		sb2.Append("<x");
			sb2.Append(" legend='" + legend + "'");
		sb2.Append(">" + x + "</x>\r\n");
		sb2.Append("<y>" + y + "</y>\r\n");
		sb2.Append("</chartdata>\r\n");
	}
	i++;
	y = amonth.ToString();
	legend = "Average";
	sb2.Append("<chartdata>\r\n");
	sb2.Append("<x");
	if(m_bHasLegends)
		sb2.Append(" legend='" + legend + "'");
	sb2.Append(">" + x + "</x>\r\n");
	sb2.Append("<y>" + y + "</y>\r\n");
	sb2.Append("</chartdata>\r\n");

	if(aweek > m_nMaxY)
		m_nMaxY = aweek;
	if(aweek < 0 && aweek < m_nMinY)
		m_nMinY = aweek;
	if(amonth > m_nMaxY)
		m_nMaxY = amonth;
	if(amonth < 0 && amonth < m_nMinY)
		m_nMinY = amonth;
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb1.ToString());
	m_sb.Append("</chartdataisland>\r\n");
	
	m_sb.Append("<chartdataisland>\r\n");
	m_sb.Append(sb2.ToString());
	m_sb.Append("</chartdataisland>\r\n");

	m_IslandTitle[0] = "--Week";
	m_IslandTitle[1] = "--Month";
	m_nIsland = 3;

	//write xml data file for chart image
	WriteXMLFile();
}
</script>

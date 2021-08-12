<script runat=server>

bool DoProfitAndLoss()
{
	m_tableTitle = "Sales Person Summary";
	
	switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + "' ";
		break;
	default:
		break;
	}

	ds.Clear();

	string sc = " SET DATEFORMAT dmy ";
	sc += " 
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
	
	m_dra = ds.Tables["report"].Select("amount > 0", "amount DESC");

	Response.Write("<br><center><h3>" + m_tableTitle + "</h3>");
	Response.Write("<b>Date Period : " + m_datePeriod + "</b><br><br>");

	PrintProfitAndLoss();
	if(m_dra.Length <= 0)
		return true;

	piecharts pc = new piecharts();
	Bitmap objBitmap = pc.GetPieChart(ds.Tables["report"].Rows, "amount", "name", "", 400);
	string fn = DateTime.Now.ToOADate().ToString();
	fn = fn.Replace(".", "_") + ".jpg";
	objBitmap.Save(Server.MapPath(".") + "\\" + fn);
	Response.Write("<center><img src=" + fn + ">");

	return true;
}

/////////////////////////////////////////////////////////////////
void PrintProfitAndLoss()
{
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = m_dra.Length;
//	if(ds.Tables["report"] != null)
//		rows = ds.Tables["report"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th>Sales_Name</th>");
	Response.Write("<th>Total_Orders</th>");
	Response.Write("<th>Total_Amount</th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	double dTotalWithGST = 0;
	double dTotalNoGST = 0;
	double dTotalTax = 0;
	bool bAlterColor = false;
	for(; i<rows && i<end; i++)
	{
//		DataRow dr = ds.Tables["report"].Rows[i];
		DataRow dr = m_dra[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string orders = dr["orders"].ToString();
		string amount = dr["amount"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center>" + orders + "</td>");
		Response.Write("<td align=right>" + MyDoubleParse(amount).ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
}

</script>

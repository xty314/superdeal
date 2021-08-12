<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_type = "";
string m_job = "";
string m_find = "";

int m_nPage = 1;
int m_nPageSize = 30;
int m_cols = 9;
int m_nStartPageButton = 1;
int m_nPageButtonCount = 9;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["p"] != null)
		m_nPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_nStartPageButton = int.Parse(Request.QueryString["spb"]);

	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];

	if(Request.QueryString["j"] != null)
		m_job = Request.QueryString["j"];
	
	if(Request.QueryString["kw"] != null)
	{
		m_find = Request.QueryString["kw"];
		m_type = "";
	}
	
	
	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();

	if(!DoSearch())
		return;

	if(!IsPostBack)
	{
		BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
}

bool DoSearch()
{
	string scon = "";
	if(m_job == "refund")
		scon = "WHERE i.type=" + GetEnumID("receipt_type", "invoice") + " OR i.type=" + GetEnumID("receipt_type", "back order");
	else if(m_type != "")
		scon = "WHERE i.type=" + m_type;
	else if(m_find != "")
		scon = "WHERE i.invoice_number LIKE '%" + m_find + "%'";
//	if(m_type == "order")
//		scon = "WHERE i.type=" + GetEnumID("receipt_type", m_type);			//2---"order";
//	else if(m_type == "invoice")
//		scon = "WHERE i.type='3' ";			//3---"invoice"
//	else if(m_type == "all")
//		scon = " ";

//	string sc = "SELECT i.invoice_number as Invoice#, i.commit_date as date, s.code, s.quantity as Qty, s.name as item, s.status ";
	string sc = "SELECT DISTINCT i.invoice_number as Invoice#, i.system, i.commit_date as date, i.name, i.company, i.city, i.email, i.total, branch, sales ";
	sc += "FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
	sc += scon + " ORDER BY i.commit_date DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void WriteHeaders()
{
	string title = "ALL";
	if(m_type != "")
		title = GetEnumValue("receipt_type", m_type).ToUpper();
	if(m_job != "")
		title = m_job.ToUpper();

	Response.Write("<br><center><font size=4><b>" + title + "</b></font></center><br>");

	//Invoice Search
	string s_kw = "";

	Response.Write("<form action=order.aspx method=get>");
	if(Request.QueryString["kw"] != null)
		s_kw = Request.QueryString["kw"];
	Response.Write("<input type=editbox size=7 name=kw value='" + s_kw + "'>");
	Response.Write("<input type=submit value='Search'>");
	Response.Write("<button onClick=window.location=('order.aspx?r="+ DateTime.Now.ToOADate() + "')");
	Response.Write(">Cancle</button>");
	Response.Write("</form>");	

	//catalog headers
	Response.Write("<div align=right>");

	string sOut = "";
	DataSet dsEnum = new DataSet();
	string sc = "SELECT id, name FROM enum WHERE class='receipt_type'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsEnum, "enum");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}
	for(int i=0; i<dsEnum.Tables["enum"].Rows.Count; i++)
	{
		string id = dsEnum.Tables["enum"].Rows[i]["id"].ToString();
		string name = dsEnum.Tables["enum"].Rows[i]["name"].ToString();
		Response.Write("<img src=r.gif> <a href=?t=" + id + "&" + r + " class=d>" + name + "</a>&nbsp;&nbsp;&nbsp;");
	}
	Response.Write("<img src=r.gif> <a href=?t=&" + r + " class=d>all</a>&nbsp;&nbsp;&nbsp;");
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	DataView source = new DataView(ds.Tables[0]);
	DrawTableHeader();
	int rows = ds.Tables[0].Rows.Count;
	int start = (m_nPage - 1)* m_nPageSize;
	int i = start;
	bool bAlterColor = false;
	for(; i < rows; i++)
	{
		if(i >= start + m_nPageSize)
			break;
		if(!DrawRow(ds.Tables[0].Rows[i], bAlterColor))
			break;
		bAlterColor = !bAlterColor;
	}
	Response.Write(PrintPageIndex());
	Response.Write("</table>");
}

void DrawTableHeader()
{
	Response.Write("<table width=100%  align=center valign=center cellspacing=1 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=50>INVOICE#</td>\r\n");
	Response.Write("<td>CUSTOMER</td>\r\n");
	Response.Write("<td>COMPANY</td>");
	Response.Write("<td>CITY&nbsp;</td>");
	Response.Write("<td>TOTAL&nbsp;</td>");
	Response.Write("<td>DATE/TIME&nbsp;</td>");

	Response.Write("<td>BRANCH&nbsp;</td>");
	Response.Write("<td>SALES&nbsp;</td>");
	Response.Write("<td>&nbsp;</td>");
	
	Response.Write("</tr>\r\n");
}

bool DrawRow(DataRow dr, Boolean bAlterColor)
{

	string s_snUrl = "";

	if(Request.QueryString["t"] == "invoice")
		s_snUrl = "&sn=i";

//i.invoice_number as Invoice#, i.system, i.commit_date as date, i.name, i.company, i.city, i.email, i.total, branch, sales ";
	string number = dr["invoice#"].ToString();
	bool bSystem = (bool)dr["system"];
	Response.Write("<tr");
	if(bAlterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write("><td>");
	Response.Write("<a href=");
	if(bSystem)
		Response.Write("q.aspx?t=i&n=" + number);
	else
		Response.Write("invoice.aspx?n=" + number);
	Response.Write(" target=_blank>" + number + "</a>");
	Response.Write("</td><td>");
	Response.Write(dr["name"].ToString());
	Response.Write("</td><td>");
	Response.Write(dr["trading_name"].ToString());
	Response.Write("</td><td>");
	Response.Write(dr["city"].ToString());
	Response.Write("</td><td>");
	Response.Write(double.Parse(dr["total"].ToString(), NumberStyles.Currency, null).ToString("c"));
	Response.Write("</td><td>");
	Response.Write(DateTime.Parse(dr["date"].ToString()).ToString("dd/MM/yyyy"));
	Response.Write("</td><td>");
	Response.Write(dr["branch"].ToString());
	Response.Write("</td><td>");
	Response.Write(dr["sales"].ToString());
	Response.Write("</td><td align=center>");
	Response.Write("<a href=");
	if(m_job == "refund")
	{
		Response.Write("salespay.aspx?refund=1&i=" + number);
	}
	else
	{
		if(bSystem)
			Response.Write("q.aspx?n=" + number);
		else
			Response.Write("pos.aspx?i=" + number + s_snUrl);
	}
	Response.Write(" class=o");
	if(m_job != "")
	{
		Response.Write(">");
		Response.Write(m_job.ToUpper());
	}
	else
	{
//		Response.Write(" target=_blank>");
		Response.Write(">Process");
	}
	Response.Write("</a>");
	Response.Write("</td></tr>");
	return true;
}

string PrintPageIndex()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=" + m_cols + " align=right>Page: ");
	int pages = ds.Tables[0].Rows.Count / m_nPageSize + 1;
	int i=m_nStartPageButton;
	if(m_nStartPageButton > 10)
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&p=");
		sb.Append((i-10).ToString());
		sb.Append("&spb=");
		sb.Append((i-10).ToString());
		sb.Append(">...</a> ");
	}
	for(;i<=m_nStartPageButton + m_nPageButtonCount; i++)
	{
		if(i > pages)
			break;
		if(i != m_nPage)
		{
			sb.Append("<a href=");
			sb.Append(WriteURLWithoutPageNumber());
			sb.Append("&p=");
			sb.Append(i.ToString());
			sb.Append("&spb=" + m_nStartPageButton.ToString() + ">");
			sb.Append(i.ToString());
			sb.Append("</a> ");
		}
		else
		{
			sb.Append("<font size=+1><b>" + i.ToString() + "</b></font>");
			sb.Append(" ");
		}
	}
	if(i<pages)
	{
		sb.Append("<a href=");
		sb.Append(WriteURLWithoutPageNumber());
		sb.Append("&p=");
		sb.Append(i.ToString());
		sb.Append("&spb=");
		sb.Append(i.ToString());
		sb.Append(">...</a> ");
		sb.Append("</td></tr>");
	}
	return sb.ToString();
}

string WriteURLWithoutPageNumber()
{
	string s = "?t=" + m_type;
	return s;
}

</script>


<asp:Label id=LFooter runat=server/>
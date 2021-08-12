<script runat=server>

DataTable dtCheck = new DataTable();

string m_idays = "1";
int m_rows = 0;
int page = 1;
const string cols = "7";
const string photoSize = "64";
const int m_nPageSize = 10; //how many rows in oen page

DataSet dscp = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	if(Request.QueryString["p"] != null)
		page = int.Parse(Request.QueryString["p"]);

	if(Request.QueryString["d"] != null)
		m_idays = Request.QueryString["d"];

	if(!BuildCheckTable())
		return;

	PrintAdminHeader();
	PrintAdminMenu();
	
	StringBuilder sb = new StringBuilder();
	
	sb.Append("<table width=100% border=1 bordercolor=#EEEEEE >");

	sb.Append("<tr><td valign=top>");
	sb.Append("<table align=center width=100% cellspacing=1 cellpadding=0>");
	sb.Append(WritePageIndex());
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	sb.Append("<td width=" + photoSize + ">PHOTO</td><td>CODE</td><td>DESCRIPTION</td>");
	sb.Append("<td align=right>EDITOR</td><td align=right>IP</td><td align=right>SITE</td><td align=right>TIME</td></tr>");
	DataRow[] drAry = dtCheck.Select("", "time DESC");

	bool bAlterColor = false;
	int startPage = (page-1) * m_nPageSize;
	for(int i=startPage; i<dtCheck.Rows.Count; i++)
//	for(int i=startPage; i<m_nPageSize*2; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		sb.Append("<tr");
		if(bAlterColor)
			sb.Append(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		sb.Append("><td width=96>");
		sb.Append(drAry[i]["photo"].ToString());
		sb.Append("</td><td valign=top>" + drAry[i]["code"].ToString());
		sb.Append("</td><td valign=top>");
		sb.Append(drAry[i]["desc"].ToString());
		sb.Append("</td><td valign=top nowrap align=right>");
		sb.Append(drAry[i]["editor"].ToString());
		sb.Append("</td><td valign=top nowrap align=right>");
		sb.Append(drAry[i]["ip"].ToString());
		sb.Append("</td><td valign=top nowrap align=right>");
		sb.Append(drAry[i]["site"].ToString());
		sb.Append("</td><td valign=top nowrap align=right>");
		sb.Append(DateTime.Parse(drAry[i]["time"].ToString()).ToString());
		sb.Append("</td></tr>");
	}
//	sb.Append("<tr><td colspan=" + cols + "><hr></td></tr>");
	sb.Append(WritePageIndex());
	sb.Append("</table>");
	sb.Append("</td></tr></table>");
	LLinks.Text = sb.ToString();
	LFooter.Text = m_sAdminFooter;
}

string WritePageIndex()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<tr><td colspan=" + cols + " align=right>Page: ");
	int pages = dtCheck.Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != page)
		{
			sb.Append("<a href=?d=" + m_idays);
			sb.Append("&p=");
			sb.Append(i.ToString());
			sb.Append(">");
			sb.Append(i.ToString());
			sb.Append("</a> ");
		}
		else
		{
			sb.Append("<font size=+1 color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	sb.Append("</td></tr>");
	return sb.ToString();
}

bool BuildCheckTable()
{
	dtCheck.Columns.Add(new DataColumn("photo", typeof(String)));
	dtCheck.Columns.Add(new DataColumn("code", typeof(String)));
	dtCheck.Columns.Add(new DataColumn("desc", typeof(String)));
	dtCheck.Columns.Add(new DataColumn("editor", typeof(String)));
	dtCheck.Columns.Add(new DataColumn("ip", typeof(String)));
	dtCheck.Columns.Add(new DataColumn("site", typeof(String)));
	dtCheck.Columns.Add(new DataColumn("time", typeof(DateTime)));

	if(!GetRecentEditors())
		return false;
	string code_old = "";
//DEBUG("rows=", m_rows);
	for(int i=0; i<m_rows; i++)
	{
		DataRow dr = dscp.Tables[0].Rows[i];
		string editor = dr["editor"].ToString();
		string time = dr["logtime"].ToString();
		string site = dr["site"].ToString();
		string ip = dr["clienthost"].ToString();
		string code = dr["code"].ToString();
		string sPicFile = dr["filename"].ToString();
		string desc = "";
		if(code == code_old)
			continue;
		code_old = code;
		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			continue;
		if(drp == null)
			continue;
		DataRow drc = dtCheck.NewRow();
		drc["photo"] = "<img border=0 width=96 src=" + GetRootPath() + "/pi/" + sPicFile + ">";
		drc["code"] = "<a href=p.aspx?" + code + ">" + code + "</a>";
		drc["desc"] = drp["name"].ToString();
		drc["editor"] = editor;
		drc["ip"] = ip;
		drc["site"] = site;
		drc["time"] = DateTime.Parse(time);
		dtCheck.Rows.Add(drc);
	}
	return true;
}

bool GetRecentEditors()
{
//	string sc = "SELECT name, visit, query FROM web_log WHERE url LIKE '%/addpic.aspx'";
//	sc += " AND DATEDIFF(day, visit, GETDATE()) < " + m_idays;
//	sc += " ORDER BY visit DESC";
	string sc = "SELECT * FROM edit_log WHERE type='pic' AND DATEDIFF(day, logtime, GETDATE()) < " + m_idays;
	sc += " ORDER BY logtime DESC";
	try
	{
//DEBUG("sc=", sc);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_rows = myCommand.Fill(dscp);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string GetEditor(string code, string desc)
{
	DataSet ds = new DataSet();
	string sc = "SELECT name, visit FROM web_log WHERE url LIKE '%/addpic.aspx'";
	sc += "' AND query='code=" + code + "&name=" + HttpUtility.UrlEncode(desc) + "' ORDER BY visit DESC";
	try
	{
//DEBUG("sc=", sc);
		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds);
		if(rows <= 0)
			return "unknown";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return ds.Tables[0].Rows[0]["name"].ToString();
}

string GetEditorWithEmail(string code, string desc)
{
	DataSet ds = new DataSet();
	string sc = "SELECT name, email, visit FROM web_log WHERE url LIKE '%/addpic.aspx'";
	sc += " AND query='code=" + code + "&name=" + HttpUtility.UrlEncode(desc) + "' ORDER BY visit DESC";
	try
	{
//DEBUG("sc=", sc);
		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz;" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds);
		if(rows <= 0)
			return "unknown";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string sRet = "<a href=mailto:";
	sRet += ds.Tables[0].Rows[0]["email"].ToString();
	sRet += ">" + ds.Tables[0].Rows[0]["name"].ToString();
	sRet += "</a>";
	return sRet; 
}
</script>

<br>
<center><h3>Check Photos</h3></center>


<table width=100% align=center>
<tr><td align=right>
<img src=r.gif> <a href=checkpic.aspx?d=1>today</a>
&nbsp;&nbsp;&nbsp;<img src=r.gif> <a href=checkpic.aspx?d=2> + Yesterday</a>
&nbsp;&nbsp;&nbsp;<img src=r.gif> <a href=checkpic.aspx?d=7>This Week</a>
&nbsp;&nbsp;&nbsp;<img src=r.gif> <a href=checkpic.aspx?d=30>Last 30 Days</a>
&nbsp;&nbsp;&nbsp;<img src=r.gif> <a href=checkpic.aspx?d=9999>All</a>
</td></tr>

<tr><td>
<asp:Label id=LLinks runat=server/>
</td></tr>
</table>

</td></tr></table>
<asp:Label id=LFooter runat=server/>

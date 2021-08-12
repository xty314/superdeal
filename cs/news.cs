<script runat=server>

DataSet ds = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();
	//PrintHeaderAndMenu();

	GetNews();

	DisplayNews();
//	PrintFooter();
}

void GetNews()
{
	
	string sc = "SELECT * FROM news ORDER BY CONVERT(datetime, date) DESC";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "news");
	}
	catch(Exception sqle) 
	{
		Response.Write("Execute SQL Query Error.<br>\r\nQuery = ");
		Response.Write(sc);
		Response.Write("<br>\r\n Error: ");
		Response.Write(sqle);
		Response.Write("<br>\r\n");
	}
}

DateTime GetStartDate()
{
	string s = "1/1/2002";
	string sc = "SELECT value FROM settings WHERE name='news_start_date'"; 
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "time");
		s = ds.Tables["time"].Rows[0].ItemArray[0].ToString();
	}
	catch(Exception sqle) 
	{
		Response.Write("Execute SQL Query Error.<br>\r\nQuery = ");
		Response.Write(sc);
		Response.Write("<br>\r\n Error: ");
		Response.Write(sqle);
		Response.Write("<br>\r\n");
	}
	return DateTime.Parse(s);
}

void DisplayNews()
{
	Response.Write("<br><br><table width=460 align=center>");
	Response.Write("<tr><td colspan=2><font size=+1><b>" + m_sCompanyName + " News</b></font></td></tr>");
	Response.Write("<tr><td colspan=2><hr width=460px></td></tr>");

	for(int i=0; i<ds.Tables["news"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["news"].Rows[i];
		string subject = dr["subject"].ToString();
		string text = dr["text"].ToString();
		string time = DateTime.Parse(dr["date"].ToString()).ToString("dd-MM-yyyy");
		text = text.Replace("\r\n", "\r\n<br>");

		Response.Write("<tr><td><b>" + subject + "</b>" + "</td><td align=right><b>" + time + "</td></tr>");
		Response.Write("<tr><td colspan=2>" + text + " <br><br></td></tr>");
	}
	Response.Write("<tr><td colspan=2><hr width=460px></td></tr>");
	Response.Write("</table>");
}

</script>

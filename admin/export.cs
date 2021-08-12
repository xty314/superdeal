<script runat=server>

DataSet ds = new DataSet();

string m_fileName = "";
string m_path = "download";
string m_cmd = "";
StringBuilder sb = new StringBuilder();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;
	m_cmd = p("cmd");
	
	PrintAdminHeader();
	PrintAdminMenu();
	
	if(m_cmd == "Export")
	{
		if(DoExport())
		{
			Response.Write("<br><center><h4>Export done, click to download file<br><br>");
			Response.Write("<a href=" + m_path + "/" + m_fileName + " class=o>" + m_fileName + "</a><br><br></h4>");
		}
		return;
	}

	Response.Write("<br><center><h3>EXPORT DATA</h3>");
	Response.Write("<form action=export.aspx method=post>");
	Response.Write("<table>");
	Response.Write("<tr><td colspan=2><input type=checkbox name=item value=1 checked>Item &nbsp;&nbsp;</td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Export class=b></td></tr>");
	Response.Write("</table></center>");
}
bool DoExport()
{
	string sc = " SELECT supplier_code, name, price1 AS price, barcode ";
	sc += " FROM code_relations ";
	sc += " WHERE 1 = 1 AND skip = 0 ";
	sc += " ORDER BY supplier_code ";
	m_fileName = "item.csv";
	bool bRet = WriteCSVFile(m_fileName, sc);
	return bRet;
}
bool WriteCSVFile(string fn, string sc)
{
	Response.Write("Getting data ...");
	Response.Flush();

	//get data
	DataSet ds = new DataSet();
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds) <= 0)
		{
			Response.Write("done<br>\r\n");
			Response.Flush();
			return true; //no data
		}
	}
	catch(Exception e) 
	{
		Response.Write("failed.<br>\r\n");
		Response.Flush();
		return true;
	}

	StringBuilder sb = new StringBuilder();

	int i = 0;

	//write column names
	DataColumnCollection dc = ds.Tables[0].Columns;
	int cols = dc.Count;
	for(i=0; i<cols; i++)
	{
		if(i > 0)
			sb.Append(",");
		sb.Append(dc[i].ColumnName);
	}
	sb.Append("\r\n");
/*
	//column data type
	for(i=0; i<cols; i++)
	{
		if(i > 0)
			sb.Append(",");
		sb.Append(dc[i].DataType.ToString().Replace("System.", ""));
	}
	sb.Append("\r\n");
*/	
	DataRow dr = null;

	for(i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		dr = ds.Tables[0].Rows[i];
		for(int j=0; j<cols; j++)
		{
			if(j > 0)
				sb.Append(",");
			string sValue = dr[j].ToString();
			sb.Append("\"" + EncodeDoubleQuote(sValue) + "\"");
		}
		sb.Append("\r\n");
		MonitorProcess(100);
	}

	string strPath = Server.MapPath(m_path) + "\\" + fn;

	Encoding enc = Encoding.GetEncoding("iso-8859-1");
	byte[] Buffer = enc.GetBytes(sb.ToString());

	//create file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();

	Response.Write("done<br>\r\n");
	Response.Flush();
	return true;
}
</script>

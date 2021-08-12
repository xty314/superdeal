<script runat=server>

DataSet ds = new DataSet();
string m_type = "";
string m_action = "";
string m_cmd = "";
string m_sLine1 = "mpn,stock,price";
string m_sBranchId = "0";
int m_nItems = 0;
int m_nStocks = 0;

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
	
	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<center><h4>Import Branch Price & Stock</h4></center>");
	
	m_action = g("a");
	m_type = g("t");
	m_cmd = p("cmd");
	if(m_action == "list")
	{
		DoListFiles();
		return;
	}
	if(m_cmd == "Process")
	{
		DoProcessFile("data/" + p("fn"));
		return;
	}
}
void DoListFiles()
{
	string fn = g("fn");
	Response.Write("<center><h4>File Upladed</h4>");
	Response.Write("<form name=f action=? method=post>");
	Response.Write("<table>");
	Response.Write("<tr><td align=right><input type=hidden name=fn value='" + fn + "'>");
	Response.Write("File:</td><td>" + fn + "</td></tr>");	
	Response.Write("<tr><td align=right>Target Branch:</td><td>" + PrintBranchOptions("1") + "</td></tr>");
	Response.Write("<tr><td align=right>");
	Response.Write("<input type=checkbox name=cbc value=1>Clear Branch Price First</td><td>");
	Response.Write("<input type=submit name=cmd class=b value=Process></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");	
}
bool DoProcessFile(string sPath)
{
	m_sBranchId = p("branch");
	string cbc = p("cbc");
	if(cbc == "1") 
	{
		string sc = " DELETE FROM code_branch WHERE branch_id = " + m_sBranchId;
		try
		{
			SqlCommand myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myCommand.Connection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch (Exception e)
		{
			myConnection.Close();
			ShowExp(sc, e);
			return false;
		}
	}	

	string fileName = Server.MapPath(sPath);
	string[] aLine = File.ReadAllLines(fileName);

	string sExpected = m_sLine1;
	if(aLine[0] != sExpected)
	{
		Response.Write("error first line, expected:" + sExpected + "\r\nRead:" + aLine[0]);
		return false;
	}
	for(int i=1; i<aLine.Length; i++)
	{
		if(!ProcessLine(aLine[i]))
			break;
		MonitorProcess(1);	
	}
	Response.Write("done<br>");
	DEBUG("line processed:", (aLine.Length - 1));
	DEBUG("price imported:", m_nItems);
	DEBUG("stock imported:", m_nStocks);
	return true;
}
private bool ProcessLine(string sLine)
{
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	string supplier_code = CSVNextColumn(cb, ref pos).Trim();
	double dStock = MyDoubleParse(CSVNextColumn(cb, ref pos).Trim());
	double dPrice = MyDoubleParse(CSVNextColumn(cb, ref pos).Trim());
	
	if(ds.Tables["c"] != null)
		ds.Tables["c"].Clear();
	string sc = " SELECT code FROM code_relations WHERE supplier_code = '" + EncodeQuote(supplier_code) + "' ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "c") <= 0)
		{
			DEBUG("mpn:", supplier_code + " not found");
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	string code = ds.Tables["c"].Rows[0]["code"].ToString();

	sc = " IF NOT EXISTS (SELECT id FROM code_branch WHERE branch_id = " + m_sBranchId + " AND code = " + code + ") ";
	sc += " INSERT INTO code_branch(branch_id, code, price1) VALUES(" + m_sBranchId + ", " + code + ", " + dPrice + ") ";
	sc += " ELSE UPDATE code_branch SET price1 = " + dPrice + " WHERE branch_id = " + m_sBranchId + " AND code = " + code;
	if(dStock != 0)
	{
		sc += " IF NOT EXISTS (SELECT id FROM stock_qty WHERE branch_id = " + m_sBranchId + " AND code = " + code + ") ";
		sc += " INSERT INTO stock_qty (branch_id, code, qty) VALUES (" + m_sBranchId + ", " + code + ", " + dStock + ") ";
		sc += " ELSE UPDATE stock_qty SET qty = " + dStock + " WHERE branch_id = " + m_sBranchId + " AND code = " + code;
		m_nStocks++;
	}
	try
	{
		SqlCommand myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch (Exception e)
	{
		myConnection.Close();
		ShowExp(sc, e);
		return false;
	}
	m_nItems++;
	return true;
}
string CSVNextColumn(char[] cb, ref int pos)
{
	if (pos >= cb.Length)
		return "";

	char[] cbr = new char[cb.Length];
	int i = 0;

	if (cb[pos] == '\"')
	{
		while (true)
		{
			pos++;
			if (pos == cb.Length)
				break;
			if (cb[pos] == '\"')
			{
				pos++;
				if (pos >= cb.Length)
					break;
				if (cb[pos] == '\"')
				{
					cbr[i++] = '\"';
					continue;
				}
				else if (cb[pos] != ',')
				{
					break;
				}
				else
				{
					pos++;
					break;
				}
			}
			cbr[i++] = cb[pos];
		}
	}
	else
	{
		while (cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
			pos++;
			if (pos == cb.Length)
				break;
		}
		pos++;
	}
	return new string(cbr, 0, i);
}
void cmdSend_Click(object sender, System.EventArgs e)
{
	if(filMyFile.PostedFile != null)
	{
		HttpPostedFile myFile = filMyFile.PostedFile;
		int nFileLen = myFile.ContentLength; 
		if( nFileLen > 0 )
		{
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);
			string strFileName = Path.GetFileName(myFile.FileName);
			string sExt = Path.GetExtension(myFile.FileName);
			if(sExt.ToLower() != ".csv")
			{
				Response.Write("<h3>Error, " + strFileName + " is not a .csv file</h3>");
				return;
			}
			string m_fileName = strFileName;
			string vpath = "data/";
			string strPath = Server.MapPath(vpath);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += strFileName;
			
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?a=list&fn=" + HttpUtility.UrlEncode(strFileName) + "\">");
		}
	}
}
void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}
</script>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=true>
<br>
<table align=center>
<tr><td colspan=4 align=center><font size=+1><b>Upload File</b><br>&nbsp;</td></tr>
<tr><td><b>File:</b><input id="filMyFile" type="file" runat="server" size=90><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</form>
<br><br>

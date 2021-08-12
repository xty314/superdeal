<script runat=server>

string branch_id = "";
DataSet ds = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
}
// Processes click on our cmdSend button
void cmdSend_Click(object sender, System.EventArgs e)
{
	branch_id = g("branch_id");
	string strPath="";
	string imagesurl2 = "";
	string FilePath = Server.MapPath("data/stock/" +  branch_id + ".txt");
	if(File.Exists(FilePath))
		File.Delete(FilePath);
	if( filMyFile.PostedFile != null )
	{
		HttpPostedFile myFile = filMyFile.PostedFile;
		string ext = Path.GetExtension(myFile.FileName);
		ext = ext.ToLower();
		if(ext != ".txt" && ext != ".csv")
		{
			Response.Write("<h3>" + Lang("ERROR Only .txt .csv File Allowed") + "</h3>");
			return;
		}
		int nFileLen = myFile.ContentLength; 
		if(nFileLen > 204800)
		{
			Response.Write("<h3>" + Lang("ERROR Max File Size(200 KB) Exceeded") + ". ");
			Response.Write(Path.GetFileName(myFile.FileName) + " " + (int)nFileLen/1000 + " KB </h3>");
			return;
		}
		if( nFileLen > 0 )
		{
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);
			string strFileName = branch_id + ".txt";
			strPath = Server.MapPath("../");
			strPath += @"\admin\data\stock\";
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			string purePath = strPath;
			strPath += strFileName;
			WriteToFile(strPath, ref myData);
			string tmpRootDir = Server.MapPath("../..");
		}
	}
	InputInStock();
}
void InputInStock()
{	
	StringBuilder sb = new StringBuilder();
	sb.Append("<script language=javascript>");
	sb.Append("window.close();\r\n");
	sb.Append("window.opener.document.location = 'instock.aspx?r=" + DateTime.Now.ToOADate() + "&branch_id=" + branch_id + "';\r\n</");
	sb.Append("script>");
	string FilePath = Server.MapPath("data/stock/" + branch_id + ".txt");
	FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	string line = r.ReadLine();
	if(line != "DetailLine,ItemNo,SubCode,ItemUPC,Qty,Stock")
	{
		Response.Write("<h4>Error, invalid file, first line must be:</h4><font color=red>DetailLine,ItemNo,SubCode,ItemUPC,Qty,Stock</font><br>");
		return;
	}
	line = r.ReadLine();
	string text = "";
	DoDeleteInStock();
	
	string dl = ""; //detailLine
	string supplier_code = "";
	string sub_code = "";
	string barcode = "";
	double dQty = 0;
	while(line != null)
	{
		if( line.IndexOf(",") < 0 )
		{
			r.Close();
			fs.Close();	
			Response.Write(Lang("Please Enter Current File"));
			return;
		}
		
		char[] cb = line.ToCharArray();
		int pos = 0;
		dl = CSVNextColumn(cb, ref pos);
		supplier_code = CSVNextColumn(cb, ref pos);
		sub_code = CSVNextColumn(cb, ref pos);
		barcode = CSVNextColumn(cb, ref pos);
//		if(pos >= cb.Length)
//			break;
		string qty = CSVNextColumn(cb, ref pos);
//DEBUG("m_pn=", supplier_code + ", barcode=" + barcode + ", qty=" + qty + "<br>");
		try
		{
			dQty = MyDoubleParse(qty);
		}
		catch(Exception e)
		{
			line = r.ReadLine();
			continue;
		}
		Trim(ref supplier_code);
		if(supplier_code != "" && dQty != 0)
			DoUpdateInStock(supplier_code, dQty);
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();
	Response.Write(sb.ToString());
}
string CSVNextColumn(char[] cb, ref int pos)
{
	if(pos >= cb.Length)
		return "";

	char[] cbr = new char[cb.Length];
	int i = 0;
	if(cb[pos] == '\"')
	{
		while(true)
		{
			pos++;
			if(pos == cb.Length)
				break;			
			if(cb[pos] == '\"')
			{
				pos++;
				if(pos >= cb.Length)
					break;
				if(cb[pos] == '\"')
				{
					cbr[i++] = '\"';
					continue;
				}
				else if(cb[pos] != ',')
				{
					Response.Write("<br><font color=red>Error</font>. CSV file corrupt, comma not followed quote. Line=");
					Response.Write(new string(cb));
					Response.Write("<br>\r\n");
					break;
				}
				else
				{
					pos++;
					break;
				}
			}
			cbr[i++] = cb[pos];
//			if(cb[pos] == '\'')
//				cbr[i++] = '\'';
		}
	}
	else
	{
		while(cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
//			if(cb[pos] == '\'')
//				cbr[i++] = '\'';
			pos++;
			if(pos == cb.Length)
				break;
		}		
		pos++;
	}
	return new string(cbr, 0, i);
}
bool DoDeleteInStock()
{	
	bool bIsInt = IsInteger(branch_id);
	if( bIsInt == false)
	{	
		Response.Write(Lang("Please Enter Current File"));
		return false;
	}
	string sc = " DELETE FROM instock WHERE branch_id = " + branch_id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
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
bool DoUpdateInStock(string supplier_code, double dQty)
{
	if(ds.Tables["get_code"] != null)
		ds.Tables["get_code"].Clear();
	string code = "";
	string sc = " SELECT code FROM code_relations WHERE supplier_code = '" + EncodeQuote(supplier_code) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "get_code") <= 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	code = ds.Tables["get_code"].Rows[0]["code"].ToString();
	sc = " IF EXISTS(SELECT id FROM instock WHERE branch_id = " + branch_id;
	sc += " AND code = '" + code + "') ";
	sc += " UPDATE instock SET ";
	sc += " qty = qty + " + dQty.ToString();
	sc += " WHERE branch_id = " + branch_id;
	sc += " AND code = '" + code + "'";
	sc += " ELSE ";
	sc += " INSERT INTO instock ( ";
	sc += " branch_id, qty, code) ";
	sc += " VALUES ( ";
	sc +=  branch_id + ", " + dQty.ToString() + ", ";
	sc += "'" + code + "');";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
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
// Writes file to current folder
void WriteToFile(string strPath, ref byte[] Buffer)
{
	// Create a file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(Buffer, 0, Buffer.Length);
	// Close file
	newFile.Close();
}
</script>
<form id="Form1" name="f" method="post" runat="server" enctype="multipart/form-data" visible=true>
<table width=70% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>
<tr>
<td><input id="filMyFile" type="file" size=50 runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr>
<br>
<asp:Label id=LOldPic runat=server/>
</td></tr></table>
</FORM>

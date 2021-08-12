<script runat=server>

const string date_format = "MMM-dd HH:mm";
string m_file = "";

//for configuration
int aNameCount = 64;
string[] aName = new string[64];
string[] aValue = new string[64];
string[] aColumn = new string[64];
bool m_bUPDATEREGULARLY = true;
bool m_bTestFormat = false;
//int debug_del = 0; //for debug only

DataSet ds = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dsc = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations

int m_nMapOptions = 0;
string[] m_aMapOption = new String[64];

int m_nSkipLine = 0;
StringBuilder sbTest = new StringBuilder();
string m_subCatSeperator = "";
string m_last_scat = "";
string m_last_sscat = "";

int m_nNoFileID = 0;
int	m_nNoCodeItem = 0;
int	m_nExistsItem = 0;
int m_nExistsBarcode = 0;
int	m_nNoPriceItem = 0;
int m_nStockErrorItem = 0;
int m_nNewItem = 0;
string m_createdOrderID = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	if(!GetColumns())
		return;

	if(Request.QueryString["file"] != null)
		m_file = Request.QueryString["file"];
	
	if(Request.QueryString["createdone"] == "1")
	{		
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h4>Sales Order Has Been Created...");
		Response.Write("<br>Sales Order# : <a title='view purchase order' href='plist.aspx?kw="+ Request.QueryString["orderid"].ToString() +"'><font color=red><b>"+ Request.QueryString["orderid"].ToString() +"</b></font></a>");
		Response.Write("<br><a title='back to import data' href='"+ Request.ServerVariables["URL"] +"'>Back to Import Data</a>");
		Response.Write("</center>");
		return;
		
	}
	if(Request.QueryString["fileid"] != null  && Request.QueryString["create"] == "1" && Request.QueryString["last_price"] != null )
	{
		if(!TSIsDigit(Request.QueryString["fileid"].ToString()))
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h4>Cannot Create Purchase Order!!! No File ID was found...");
			Response.Write("<br><a title='back to import data' href='"+ Request.ServerVariables["URL"] +"'>Back to Import Data</a>");
			Response.Write("</center>");
			return;
		}
		if(doCreatePurchaseOrder(Request.QueryString["fileid"].ToString()))
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?createdone=1&orderid="+ m_createdOrderID +"\">");
			return;
		}
		else
		{
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h4>Unable to Create Purchase Order, Please Check your CSV File...");			
			Response.Write("<br><a title='back to import data' href='"+ Request.ServerVariables["URL"] +"'>Back to Import Data</a>");
			Response.Write("</center>");			
			return;
		}
	}
	if(Request.Form["cmd"] != null)
	{
		if(!DoSaveConfiguration())
		{
			Response.Write("<br><br><center><h3>Error Save Configuration</h3>");
			return;
		}

		if(Request.Form["cmd"] == "Test")
		{
			PrintAdminHeader();
			PrintAdminMenu();
			m_bTestFormat = true;
			m_bShowProgress = true;
			CheckSCVFormat();
			DoTest();
			PrintAnalyzePage();
		}
		else
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?t=analyze&file=" + HttpUtility.UrlEncode(m_file) + "\">");
		}
		return;
	}

	string type = Request.QueryString["t"];
//	if(type == null || type == "")
//	{
//		PrintMainForm();
//		return;
//	}
	if(type == "process")
	{
		CheckSCVFormat();
		//do delete the old data same file first... 
		doCleanUpImportedData(Session["import_file_id"].ToString());
		if(DoProcessFile())
		{
		}
		return;
	}
	else if(type == "delete")
	{
		string root = GetRootPath() + "data/inv";
		root = Server.MapPath(root);
		string pathname = root + "\\" +  m_file;
		if(File.Exists(pathname))
			File.Delete(pathname);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"\">");
		return;
	}
	else if(type == "analyze")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		CheckSCVFormat();
		PrintAnalyzePage();
		return;
	}
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
//CheckSCVFormat();
		string root = GetRootPath() + "data/inv";
		root = Server.MapPath(root);
		if(!Directory.Exists(root))
			Directory.CreateDirectory(root);

		Response.Write("<br><center><h3>Import Purchase Order in CSV Format</h3>");
		Response.Write("<table align=center cellspacing=1 cellpadding=7 border=1 bordercolor#EEEEEE");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
		Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
		Response.Write("<td><b>FILE</b></td>");
//		Response.Write("<td><b>SUPPLIER</b></td>");
		Response.Write("<td><b>SIZE</b></td>");
		Response.Write("<td><b>FILE DATE</b></td>");
		Response.Write("<td><b>ACTION</b></td>");
		Response.Write("</tr>");

		string[] dirs = Directory.GetDirectories(root);
		string path = "";
		string dir = "";
		DirectoryInfo di = new DirectoryInfo(root);
		foreach (FileInfo f in di.GetFiles("*.csv")) 
		{			
			string s = f.FullName;
			string file = f.Name;//s.Substring(path.Length+1, s.Length-path.Length-1);
			m_file = file;
			CheckSCVFormat();
			string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
			Response.Write("<tr><td><a href="+ Request.ServerVariables["URL"] +"?supplier=" + dir + "&file=");
			Response.Write(HttpUtility.UrlEncode(file));
			Response.Write(">");
			Response.Write(file);
			Response.Write("</a></td>");
//			Response.Write("<td>"+ dir + "</td>");
			Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
			Response.Write("<td>" + f.LastWriteTime.ToString(date_format) + "</td>");
			Response.Write("<td>");
			
			Response.Write("<input type=button class=b onclick=window.location=('"+ Request.ServerVariables["URL"] +"?t=process&file=" + HttpUtility.UrlEncode(file) + "') value=Process>");
			Response.Write("<input type=button class=b onclick=window.location=('"+ Request.ServerVariables["URL"] +"?t=delete&file=" + HttpUtility.UrlEncode(file) + "&fileid="+ Session["import_file_id"] +"') value=Delete>");
			string sp = dir.ToUpper();
			Response.Write("<input type=button class=b onclick=window.location=('"+ Request.ServerVariables["URL"] +"?t=analyze&file=" + HttpUtility.UrlEncode(file) + "') value='Analyze'>");
			Response.Write("</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");
		Form1.Visible = true;
		return;
	}
}
bool DoCreateTables()
{
	string sc = @" CREATE TABLE [dbo].[import_purchase_order_format](
			[id] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
			[file_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[supplier_id] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[mpn] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[price] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[quantity] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[po_number] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[comments] [varchar](4000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,	
			[lines_to_skip] [int] NOT NULL CONSTRAINT [DF_import_purchase_order_format_lines_to_skip]  DEFAULT (0),
			 CONSTRAINT [PK_import_purchase_order_format] PRIMARY KEY CLUSTERED 
			(
				[id] ASC
			) ON [PRIMARY]
			) ON [PRIMARY]
		";

	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception ee) 
	{
		ShowExp(sc, ee);
		return false;
	}
/*
	sc = @" CREATE TABLE [dbo].[import_purchase_order_format_log](
			[id] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
			[file_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[order_number] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,										
			[comments] [varchar](4000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,															
			[record_date] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[card_id] [int] NULL,
			 CONSTRAINT [PK_import_purchase_order_format_log] PRIMARY KEY CLUSTERED 
			(
				[id] ASC
			) ON [PRIMARY]
			) ON [PRIMARY]
		";

	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception ee) 
	{
		ShowExp(sc, ee);
		return false;
	}
*/
	sc = @" CREATE TABLE [dbo].[import_tmp_purchase_order](
			[kid] [int] IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
			[id] [varchar] (50) NOT NULL,					
			[supplier_id] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[mpn] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[price] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[quantity] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[po_number] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
			[comments] [varchar](4000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,	
			) ON [PRIMARY]
		";

	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception ee) 
	{
		ShowExp(sc, ee);
		return false;
	}
	return true;
}
bool GetColumns()
{
	string sc = " SELECT TOP 1 * FROM import_purchase_order_format ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "columns");
	}
	catch(Exception e) 
	{
		PrintAdminHeader();
		PrintAdminMenu();
//DEBUG("e.ToString() =", e.ToString());
		if((e.ToString()).IndexOf("Invalid object name 'import_purchase_order_format'.") >=0 )
		{
			if(!DoCreateTables())
				return false;
			else
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?\">");
			return false;
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}
	}

	DataColumnCollection dc = ds.Tables["columns"].Columns;
	aNameCount = dc.Count - 2; //ignore id, file_name
	int m = 0;
	for(int i=0; i<dc.Count; i++)
	{
		string name = dc[i].ColumnName;
		if(name == "id" || name == "file_name")
			continue;
		aName[m++] = name;
	}

	for(int i=0; i<aNameCount; i++)
		aValue[i] = "0";

	return true;
}

void PrintMainForm()
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h4>Import Data</h4>");
	Response.Write("<h5><a href="+ Request.ServerVariables["URL"] +"?t=item class=o>Import Purchase Order</a><h5>");
	
}

bool CheckSCVFormat()
{
	if(dst.Tables["csv"] != null)
		dst.Tables["csv"].Clear();
	string sc = "SELECT * FROM import_purchase_order_format WHERE file_name = '" + m_file + "' ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "csv") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(dst.Tables["csv"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["csv"].Rows[0];
		Session["import_file_id"] = dr["id"].ToString();		
		for(int i=0; i<aNameCount; i++)
		{
			string s = dr[aName[i]].ToString();
			if(s == "")
				s = "0"; //this column (aColumn[0]) is set to blank
			string nstr = dr[aName[i]].ToString();
			Trim(ref nstr);
			aValue[i] = nstr;
			if(aName[i] == "lines_to_skip")
				m_nSkipLine = MyIntParse(nstr);
		}
	}
	return true;	
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
bool GetSampleLine(string sLine)
{
	m_aMapOption[0] = "<option value=''></option>";
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 1;
	for(i=1; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		m_aMapOption[i] = CSVNextColumn(cb, ref pos);
	}
	m_nMapOptions = i;
	return true;
}
string BuildMapOptions(int current)
{
	StringBuilder sb = new StringBuilder();
	sb.Append(m_aMapOption[0]);

	for(int i=1; i<m_nMapOptions; i++)
	{
		sb.Append("<option value=" + i.ToString());
		if(current == i)
			sb.Append(" selected");
		sb.Append(">" + m_aMapOption[i] + "</option>");
	}
	return sb.ToString();
}
bool PrintAnalyzePage()
{
	//read a few lines of the file
	string root = GetRootPath() + "data/inv/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string text = "";
	bool bGetSample = true;
	for(int i=0; i<30; i++)
	{
		if(line == null)
			break;
		if(bGetSample)
		{
			bGetSample = false;
			GetSampleLine(line);
		}
		text += "\r\n";
		text += line;
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();

	Response.Write("<br><center><font size=3><b>File Format Configuration</b></font>");
	Response.Write("<table width=90%>");
	Response.Write("<tr><td><b>" + m_file + "</b></td></tr>");

	Response.Write("<tr><td><textarea name=text wrap=off cols=110 rows=10>");
	Response.Write(text);
	Response.Write("</textarea></td></tr>");

	Response.Write("<tr><td><br><b>Column Mapping</b>");

	//configuration table
	Response.Write("<table valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=50>Name</td>\r\n");
	Response.Write("<td width=50>MapTo</td>\r\n");
	Response.Write("</tr>\r\n");

	Response.Write("<form action="+ Request.ServerVariables["URL"] +"?file=" + HttpUtility.UrlEncode(m_file) + " method=post>");

	for(int i=0; i<aNameCount; i++)
	{
		Response.Write("<tr><td><b>");
		if(aName[i] == "order_number" || aName[i] == "is_delete" )
			continue;
		Response.Write(aName[i].ToUpper() + "</b> ");
		if(aName[i] == "mpn" || aName[i] == "quantity" || aName[i] == "branch")
			Response.Write(" <font color=red><b>*</b></font> ");
		Response.Write("</td><td>");
		if(aName[i] == "total_process_time" || aName[i] == "order_number")
		{					
		}
		else if(aName[i] == "lines_to_skip")
		{
			Response.Write("<input type=text size=10 name=" + aName[i] + " value='");
			Response.Write(aValue[i]);
			Response.Write("'>");
		}
		else
		{
			Response.Write("<select name=" + aName[i] + ">");
			Response.Write(BuildMapOptions( MyIntParse(aValue[i]) ));
			Response.Write("</select>");
		}
        if(aName[i] == "lines_to_skip")
			Response.Write(" <i>(Define Which Line in the CSV file to Skip to import, mostly is the Header of the CSV file)</i>");
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Test class=b>");
	Response.Write("<input type=submit name=cmd value=Save class=b>");
	Response.Write("<input type=button class=b onclick=window.location=('"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "') value=OK>");
	Response.Write("</td></tr>");
	Response.Write("</form>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
	Response.Write("</center>");
	return true;
}

/////////////////////////////////////////////////////////////////
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
			string vpath = GetRootPath() + "data/inv/";
			string strPath = Server.MapPath(vpath);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += strFileName;
			
			WriteToFile(strPath, ref myData);
			doInsertUploadFile(m_fileName);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"\">");
		}
	}
}
void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}
bool DoSaveConfiguration()
{
	string sc = "IF NOT EXISTS (SELECT id FROM import_purchase_order_format WHERE file_name = '" + m_file + "') ";
	sc += " INSERT INTO import_purchase_order_format (file_name";
	for(int i=0; i<aNameCount; i++)
	{
		sc += "," + aName[i];
	}
	sc += ") VALUES('" + m_file + "'";
	for(int i=0; i<aNameCount-1; i++)
	{
		sc += ",'" + Request.Form[aName[i]] + "' ";
	}
	sc += ", '" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += ") ELSE UPDATE import_purchase_order_format SET ";
	for(int i=0; i<aNameCount-1; i++)
	{
		sc += aName[i] + "='" + Request.Form[aName[i]] + "', ";
	}
	sc += aName[aNameCount-1] + "='" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += " WHERE file_name = '" + m_file + "' ";
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
		if((e.ToString()).IndexOf("Invalid object name 'import_purchase_order_format'.") >=0 )
		{
			if(!DoCreateTables())
				return false;
		}
		else
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}
bool DoTest()
{
	//read a few lines of the file
	string root = GetRootPath() + "data/inv/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string text = "";

	int i = 1;
	for(; i<10; i++)
	{
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		if(line == null)
			break;
		text += "\r\n";
		text += line;
		if(!ProcessLine(line))
			break;
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 bordercolor=#EEEEEE bgcolor=white border=1");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(i=0; i<aNameCount; i++)
	{
//		if(aName[i] == "sub_cat_seperator" || aName[i] == "lines_to_skip")
//			continue;
		if(aValue[i] == "" || aValue[i] == "0")
		{
//			if(aName[i] != "s_cat" && aName[i] != "ss_cat")
				continue;
		}
		Response.Write("<th>" + aName[i] + "</th>");
	}
	Response.Write("</tr>");
	Response.Write(sbTest.ToString());
	Response.Write("</table>");
	return true;
}
bool DoAddItem()
{	
	string id = "";
	string mpn = "";	
	string supplier_id = "0";
	string po_number = "";
	string quantity = "";
	string comments = "";	
	string price = "";
	string branch = "";
	
	if(Session["import_file_id"] != null) //if(Session["import_order_item_id"] != null)
	{
		id = Session["import_file_id"].ToString();//Session["import_order_item_id"].ToString();
		//Session["import_order_item_id"] = null;
	}
	if(Session["import_purchase_item_mpn"] != null)
	{
		mpn = Session["import_purchase_item_mpn"].ToString();
		Session["import_purchase_item_mpn"] = null;
	}
	if(Session["import_purchase_item_supplier_id"] != null)
	{
		supplier_id = Session["import_purchase_item_supplier_id"].ToString();
		Session["import_purchase_item_supplier_id"] = null;
	}
	if(Session["import_purchase_item_po_number"] != null)
	{
		po_number = Session["import_purchase_item_po_number"].ToString();
		Session["import_purchase_item_po_number"] = null;
	}
	if(Session["import_purchase_item_quantity"] != null)
	{
		quantity = Session["import_purchase_item_quantity"].ToString();
		Session["import_purchase_item_quantity"] = null;
	}
	if(Session["import_purchase_item_comments"] != null)
	{
		comments = Session["import_purchase_item_comments"].ToString();
		Session["import_purchase_item_comments"] = null;
	}
	if(Session["import_purchase_item_price"] != null)
	{
		price = Session["import_purchase_item_price"].ToString();
		Session["import_purchase_item_price"] = null;
	}
	
	if(id == "")
	{
		m_nNoFileID++;
		return false; //skip this one //break the process
	}

	if(mpn == "")
	{
		m_nNoCodeItem++;
		return false; //skip this one // break the process..
	}
	Trim(ref supplier_id);

	double dQuantity = 0;
	Trim(ref quantity);
	if(quantity != "")
	{
		try
		{
			dQuantity = double.Parse(quantity);
		}
		catch(Exception e) 
		{
			m_nStockErrorItem++;
			return true;
		}
	}
	quantity = ((int)dQuantity).ToString();	

	if(price == "")
		price = "0";
	double dprice = 0;
	Trim(ref price);
	price = (MyCurrencyPrice(price)).ToString();
	
	if(po_number == "")
		po_number = DateTime.Now.ToString("MMddHHmm");
	if(supplier_id == "")
		supplier_id = "0";
	string sc = " BEGIN TRANSACTION SET DATEFORMAT dmy ";
	sc += " INSERT INTO import_tmp_purchase_order (id, supplier_id, mpn, quantity, price, po_number, comments) ";
	sc += " VALUES( '" + id + "', '" + supplier_id + "', '" + mpn + "', '" + quantity + "', '" + price + "', '" + EncodeQuote(po_number) + "',  '" + EncodeQuote(comments) + "')";
	sc += " COMMIT ";
//DEBUG("sc=", sc);	
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		myCommand.Connection.Close();
		return false;
	}
	m_nNewItem++;
	return true;
}
bool ProcessLine(string sLine)
{
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 1;
	for(i=1; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		aColumn[i] = CSVNextColumn(cb, ref pos);
	}
	if(m_bTestFormat)
		sbTest.Append("<tr>");
	for(i=0; i<aNameCount; i++)
	{
	//	if(aName[i] == "sub_cat_seperator" || aName[i] == "lines_to_skip")
	//		continue;
		if(aName[i] == "lines_to_skip" || aName[i] == "po_number")
			continue;
		if(aValue[i] == "" || aValue[i] == "0")
			continue;
		string v = "";
		int n = MyIntParse(aValue[i]);
		if(n >= 0)
			v = aColumn[n];
		if(m_bTestFormat)
			sbTest.Append("<td>" + v + "</td>");
		else
			Session["import_purchase_item_" + aName[i]] = v;
//DEBUG("import_purchase_item_" + aName[i] + "=", v);			
	}
	if(m_bTestFormat)
		sbTest.Append("</tr>");
	else
	{
		if(!DoAddItem())
			return false;
	}
	return true;
}
bool DoProcessFile()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = GetRootPath() + "data/inv/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
//DEBUG("fileName = ", fileName);
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	Response.Write("done.<br>");
	Response.Write("Processing, please wait...");
	
	string line = r.ReadLine();
	string text = "";

	bool bRet = true;
	int i = 0;
	while(line != null)
	{
		i++;
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		text += "\r\n";
		text += line;
		if(!ProcessLine(line))
		{
			bRet = false;
			break;
		}
		MonitorProcess(300);
		line = r.ReadLine();
	}
//DEBUG("i = ", i);
	r.Close();
	fs.Close();
	if(!doCreatePurchaseOrder(Session["import_file_id"].ToString()))
		return false;
	Response.Write("done.<br>");
	Response.Write("<h5>Total lines processed : <b>" + (i - m_nSkipLine).ToString() + "</b><br>");
	Response.Write("No File ID Found : <b>" + m_nNoFileID +"</b><br />");
	Response.Write("No MPN Items(skipped) : <b>" + m_nNoCodeItem + "</b><br>");	
	Response.Write("Total Purchase Order Item(Imported) : <b>" + m_nNewItem + "</b><br><br>");
	string tmp = GenRandomString() + GenRandomString() + GenRandomString()+ GenRandomString()+ GenRandomString()+ GenRandomString()+ GenRandomString();
//DEBUG(" tmp =", tmp);
//	Response.Write("<a title='click here to create purchase order' href='"+ Request.ServerVariables["URL"] +"?create=1&r="+ DateTime.Now.ToOADate() +"&nop="+ tmp +"&fileid=" + Session["import_file_id"] + "'>Click Here to Create Purchase Order Now:</a><b></b></h5>");	
	Response.Write("<a href=? class=o>done</a>");
	return bRet;
}
bool doCleanUpImportedData(string fileID)
{
	string sc = " DELETE FROM import_tmp_purchase_order WHERE id = '" + fileID +"' ";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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
bool doCreatePurchaseOrderSP()
{
	string sc = " IF NOT EXISTS (SELECT * FROM syscomments WHERE id = object_id('dbo.sp_import_purchase_order')) SELECT top 1 *  FROM card ";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst);
	}
	catch(Exception e) 
	{
		//if(e.ToString().IndexOf("already an object named") >= 0)
		//	return true;
		ShowExp(sc, e);
		//doDropChangeCodeProcedure();
		return false;
	}
	if(rows > 0)
	{
		sc = " CREATE PROCEDURE sp_import_purchase_order  \r\n";
		sc += " @file_id int, \r\n";	
		sc += " @staff_id int, \r\n";	
		sc += " @return_status int  OUTPUT \r\n";
		sc += " AS \r\n";
		sc += " DECLARE @new_order_id int  \r\n";
		
		sc += " BEGIN TRANSACTION \r\n";
		sc += " INSERT INTO purchase \r\n";
		sc += " (type, supplier_id, buyer_id, po_number, status, staff_id) \r\n";
		sc += " SELECT TOP 1 1, oi.supplier_id, @staff_id, oi.po_number, 1, @staff_id \r\n";
		sc += " FROM import_tmp_purchase_order oi WHERE oi.id = @file_id ORDER BY kid  \r\n";

		sc += " \t SELECT @new_order_id = IDENT_CURRENT('purchase')  \r\n";

		//*** insert into order item table
		sc += " INSERT INTO purchase_item (id, code, supplier_code, qty, name, price) \r\n";
		sc += " SELECT @new_order_id, c.code, oi.mpn, oi.quantity, c.name, c.supplier_price \r\n"; 
		sc += " FROM import_tmp_purchase_order oi JOIN code_relations c ON c.supplier_code = oi.mpn WHERE oi.id = @file_id ORDER BY kid  \r\n";
		
		//** update order# to format file ***
//		sc += " UPDATE import_purchase_order_format SET order_number = @new_order_id, total_process_time = total_process_time + 1 WHERE id = @file_id ";

		sc += " COMMIT transaction \r\n";
		sc += " set @return_status = @new_order_id --done\r\n";
//DEBUG("sc = ", sc);
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dst);
		}
		catch(Exception e) 
		{
			//if(e.ToString().IndexOf("already an object named") >= 0)
			//	return true;
			ShowExp(sc, e);
			//doDropChangeCodeProcedure();
			return false;
		}
	}
	return true;
}
bool doCreatePurchaseOrder(string file_id)
{
	string sc = " ";
	if(file_id == null || file_id == "")
		return false;
	
	doCreatePurchaseOrderSP();
	
	SqlCommand myCommand = new SqlCommand("sp_import_purchase_order", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;
	myCommand.Parameters.Add("@file_id", SqlDbType.Int).Value = file_id;
	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand.Parameters.Add("@return_status", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("Error to Create Purchase Order: ", e);
		return false;
	}
	doCleanUpImportedData(file_id);
	m_createdOrderID = myCommand.Parameters["@return_status"].Value.ToString();
	//DEBUG("returnID =", m_createdOrderID);


	return true;
}
bool doInsertUploadFile(string fileName)
{
	string sc = " BEGIN TRANSACTION ";
	sc += " IF NOT EXISTS (SELECT TOP 1 file_name FROM import_purchase_order_format Where file_name = '" + fileName +"') ";
	sc += " BEGIN ";
	sc += " Insert into import_purchase_order_format (file_name) VALUES( '" + fileName +"')  ";		
	sc += " END ";
//	sc += " INSERT INTO import_purchase_order_format_log (file_name, order_number, comments, record_date, card_id) VALUES('" + fileName +"', '', 'File Uploaded', GETDATE(), "+ Session["card_id"] +" ) ";
	sc += " COMMIT  ";
//DEBUG(" sc =", sc);
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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
</script>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<br>
<table align=center>
<tr><td colspan=4 align=center><font size=+1><b>Upload File</b><br>&nbsp;</td></tr>
<tr><td><b> File : </b><input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td> <asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</form>
<br><br>

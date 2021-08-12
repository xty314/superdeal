<%@Import Namespace="System.Threading" %>
<script runat=server>
///////////////////////////////////////////////////////////////////////////////////////////////////////////
//for getdata
int code_index = 0;			//for search topsys_code
int next_code = m_nFirstCode;		//next available code in code_relations table, init a default value here
int files_processed = 0;
int total_items = 0;
int dropped_items = 0;
int new_items = 0;
//int changed_items = 0;
int old_items = 0;
int	deleted_items = 0;
string target_supplier = "@";			//the one to process, if "all" then process all suppliers datafile
string target_file = "";
string target_file_id = "";
const string date_format = "MMM-dd HH:mm";

string m_sTimeStamp = "";

//for configuration
int aNameCount = 23;
string[] aName = new string[64];
int[] aValue = new int[64];
string[] aColumn = new string[64];

bool m_bTestFormat = false;
//int debug_del = 0; //for debug only

DataSet dsc = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations
///////////////////////////////////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////////////////////////////////
//for analyze
double	m_dPrice_rate_settings = 0;
int		m_nPrice_age_settings = 0;
int		m_nUpdatedItems = 0;
///////////////////////////////////////////////////////////////////////////////////////////////////////////

string m_sCheckSumLine = "";
//StringBuilder m_scDump = new StringBuilder();
string m_sDumpP = ""; //product being dumped
string m_sDumpK = ""; //product_skip being dumped

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

//	target_supplier = m_sCompanyName.ToLower();
	int i = 0;
	for(i=0; i<64; i++)
		aValue[i] = 0;
	i = 0;
	aName[i++] = "code";
	aName[i++] = "name";
	aName[i++] = "brand";
	aName[i++] = "cat";
	aName[i++] = "s_cat";
	aName[i++] = "ss_cat";
	aName[i++] = "cost";
	aName[i++] = "RRP";
	aName[i++] = "stock";
	aName[i++] = "eta";
	aName[i++] = "details";
	aName[i++] = "skip_lines";
	aName[i++] = "checksum_line_number";
	aName[i++] = "checksum_line_text";
	aName[i++] = "price1";
	aName[i++] = "price2";
	aName[i++] = "price3";
	aName[i++] = "price4";
	aName[i++] = "price5";
	aName[i++] = "price6";
	aName[i++] = "price7";
	aName[i++] = "price8";
	aName[i++] = "price_system";
	aName[i++] = "barcode";

	aNameCount = i;

	//get query string settings
	if(Request.QueryString["supplier"] != null && Request.QueryString["supplier"] != "")
	{
		target_supplier = Request.QueryString["supplier"];
//		target_supplier = target_supplier.Substring(0, 2);

		if(Request.QueryString["file"] != null)
		{
			target_file = Request.QueryString["file"];
		}

		bool bShowAnalyzePage = false;
		if(Request.Form["cmd"] != null)
		{
			if(!DoSaveConfiguration())
			{
				Response.Write("<br><br><center><h3>Error Save Configuration</h3>");
			}
			if(Request.Form["cmd"] == "Test")
			{
				m_bTestFormat = true;
				m_bShowProgress = true;
				PrintAdminHeader();
				PrintAdminMenu(); //true, no table, we want to see progress
				CheckSCVFormat();
				PrintAnalyzePage();
				DoTest();
				BindTempTable();
				PrintAdminFooter();
				return;
			}
			bShowAnalyzePage = true;
		}

		if(Request.QueryString["t"] == "delete")
		{
			string root = "./data";
			root = Server.MapPath(root);
			string pathname = root + "\\" + target_supplier + "\\" +  target_file;
//DEBUG("path=", pathname);
			if(File.Exists(pathname))
				File.Delete(pathname);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pbupdate.aspx\">");
			return;
		}
		else if(Request.QueryString["t"] == "analyze")
			bShowAnalyzePage = true;

		if(!CheckSCVFormat() || bShowAnalyzePage)
		{
//			m_bShowProgress = true;
			PrintAdminHeader();
			PrintAdminMenu(); //true, no table, we want to see progress
			
			PrintAnalyzePage();
			PrintAdminFooter();
			return;
			//else go ahead do a testing process with top 10 lines
		}
	}
	else //print price list files
	{
		PrintAdminHeader();
		PrintAdminMenu();

		string root = "./data";
		root = Server.MapPath(root);
//DEBUG("root=", root);
		if(Directory.Exists(root))
		{
			Response.Write("<br><center><h3>Update Items</h3>");
			Response.Write("<table align=center cellspacing=1 cellpadding=7 border=1 bordercolor#EEEEEE");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
			Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
			Response.Write("<td><b>FILE</b></td>");
			Response.Write("<td><b>SUPPLIER</b></td>");
			Response.Write("<td><b>SIZE</b></td>");
			Response.Write("<td><b>FILE DATE</b></td>");
			Response.Write("<td><b>LAST UPDATED BY</b></td>");
			Response.Write("<td><b>LAST UPDATED TIME</b></td>");
			Response.Write("<td><b>ACTION</b></td>");
			Response.Write("</tr>");

			string[] dirs = Directory.GetDirectories(root);
			string path = "";
			string dir = "";
			foreach(string sd in dirs)
			{
				path = sd;
				dir = path.Substring(root.Length+1, path.Length-root.Length-1);
//				if(dir.ToLower() != m_sCompanyName.ToLower())
//					continue;
				DirectoryInfo di = new DirectoryInfo(sd);
				foreach (FileInfo f in di.GetFiles("*.csv")) 
				{
					string s = f.FullName;
					string file = s.Substring(path.Length+1, s.Length-path.Length-1);
					string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
					Response.Write("<tr><td><a href=pbupdate.aspx?supplier=" + dir + "&file=");
					Response.Write(HttpUtility.UrlEncode(file));
					Response.Write(">");
					Response.Write(file);
					Response.Write("</a></td><td>"+ dir + "</td>");
					Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
					Response.Write("<td>" + f.LastWriteTime.ToString(date_format) + "</td>");

					DataSet dsaul = new DataSet();
					int rows = 0;
					string sc = "SELECT * FROM auto_update_log WHERE file_id='" + file_id + "'";
					try
					{
						myAdapter = new SqlDataAdapter(sc, myConnection);
						rows = myAdapter.Fill(dsaul, "log");
					}
					catch(Exception e) 
					{
						ShowExp(sc, e);
						return;
					}
					string name = "&nbsp;";
					string time = "&nbsp;";
					if(rows >= 1)
					{
						name = dsaul.Tables["log"].Rows[0]["updated_by"].ToString();
						time = DateTime.Parse(dsaul.Tables["log"].Rows[0]["updated_time"].ToString()).ToString(date_format);
					}
					Response.Write("<td>" + name + "</td>");
					Response.Write("<td>" + time + "</td>");
					Response.Write("<td>");
					Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('pbupdate.aspx?supplier=" + dir + "&file=" + HttpUtility.UrlEncode(file) + "') value=Process>");
					Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('pbupdate.aspx?t=delete&supplier=" + dir + "&file=" + HttpUtility.UrlEncode(file) + "') value=Delete>");
					string sp = dir.ToUpper();
//					if(sp != "TP" && sp != "BB" && sp != "IM" && sp != "RN")// && sp != "CD")
					if(sp != "TP" && sp != "IM" && sp != "RN")// && sp != "CD")
						Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('pbupdate.aspx?t=analyze&supplier=" + dir + "&file=" + HttpUtility.UrlEncode(file) + "') value='Analyze'>");
					Response.Write("</td>");
					Response.Write("</tr>");
				}
			}
			Response.Write("</table>");
			Form1.Visible = true;
		}
		LFooter.Text = m_sAdminFooter;
		return;
	}

	DateTime dtStart = DateTime.Now;

	m_bShowProgress = true;
	PrintAdminHeader();
	PrintAdminMenu(); //true, no table, we want to see progress
//	Response.Write("<font color=white>");
	Response.Flush();

	//get settings
	if(!GetSettings())
		return;

	//get data from data file
	if(!PrepareData())
		return;
	if(!GetFiles())
		return;
	
	//anaylze data
/*	Response.Write("<br><b>Analyze data ... </b><br>");
	Response.Flush();
	if(!PrepareTablesForAnalyze())
		return;
	if(!CollectAllForSaleItems()) //collect items from product_raw join code_relations to dst.Tables["product_all"]
		return;
	if(!AnalyzeData())
		return;
*/
	Response.Write("<br><br><table><tr><td>\r\n");
	Response.Write("<table cellspacing=0 cellpadding=3 align=Center rules=all bgcolor=white \r\n");
	Response.Write("bordercolor=White border=0 width=100% style='font-family:Verdana;font-size:8pt;\r\n");
	Response.Write("border-collapse:collapse;'><tr><td>\r\n");
	Response.Write("<font color=red>Total files processed: </font>");
	Response.Write(files_processed);
	Response.Write("<font color=red> Total items: </font>");
	Response.Write(total_items);
	Response.Write("<font color=red> New items: </font>");
	Response.Write(new_items);
	Response.Write("<font color=red> Old items: </font>");
	Response.Write(old_items);
	Response.Write("<font color=red> Skipped items: </font>");
	Response.Write(dropped_items);
	Response.Write("<font color=red> Disappered: </font><font color=green>");
	Response.Write(deleted_items);
	Response.Write("</font></td></tr></table>");

	Response.Write("<br><font color=red>");
	if(new_items > 0)
	{
		Response.Write(new_items);
		Response.Write(" new items added.");// <a href=newitems.aspx class=o>Edit New Items</a>");
	}
	Response.Write("</font>");
	Response.Write("<br><font color=red>");
	if(m_nUpdatedItems > 0)
	{
		Response.Write(m_nUpdatedItems);
		Response.Write(" items updated</font>");//, <a href=updatelog.aspx>check updatelog</a>");
	}
	else if(new_items < 0)
	{
		Response.Write("No changes detected</font>, back to <a href=default.aspx> Home </a>");
	}

	TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - dtStart.Ticks);
//	Response.Write("<br><font color=white><b>Updating used " + ((double)ts.TotalMilliseconds / 1000).ToString() + " seconds</b></font><br>");
	Response.Write("<br><b>Updating used " + ts.TotalSeconds + " seconds</b><br>");

	TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
	//end of page
	WriteAutoUpdateLog();
	PrintAdminFooter();
}

bool DoTest()
{
	if(!CreateTempTable())
	{
		Response.Write("<font color=red>Error creating temp table. Program terminated.</font><br>\r\n");
		return false;
	}
	return GetFiles();
}

void BindTempTable()
{
	DataView dv = new DataView(dst.Tables["temp_" + target_supplier]);
	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();
}

bool CheckSCVFormat()
{
	string sp = target_supplier.ToUpper();
//	if(sp == "TP" || sp == "BB" || sp == "IM" || sp == "RN")// || sp == "CD")
	if(sp == "TP" || sp == "IM" || sp == "RN")// || sp == "CD")
	{
//		m_bManu = true;
		return true;
	}

	string sc = "SELECT * FROM csv_format WHERE supplier='" + target_supplier + "'";
//DEBUG("sc=", sc);
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
		for(int i=0; i<aNameCount; i++)
		{
			string s = dr[aName[i]].ToString();
			if(s == "")
				s = "0"; //this column (aColumn[0]) is set to blank
			string nstr = dr[aName[i]].ToString();
			Trim(ref nstr);
//			if(i == 13)
//				m_sCheckSumLine = nstr;
//			else
			try
			{
				aValue[i] = int.Parse(nstr);
			}
			catch(Exception e)
			{
			}
		}
		m_sCheckSumLine = dr["checksum_line_text"].ToString();
	}
	return true;	
}

bool PrintAnalyzePage()
{
	//read a few lines of the file
	string root = "./data/" + target_supplier + "/";
	root = Server.MapPath(root);
	string fileName = root + target_file;
//DEBUG("filename=", fileName);
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string text = "";
	for(int i=0; i<30; i++)
	{
		if(line == null)
			break;
		text += "\r\n";
		text += line;
		line = r.ReadLine();
	}

	Response.Write("<br><center><h3>File Format Configuration</h3>");
	Response.Write("<table width=90%>");
	Response.Write("<tr><td><b>" + target_file + "</b></td></tr>");

	Response.Write("<tr><td><textarea name=text wrap=off cols=110 rows=10>");
	Response.Write(text);
	Response.Write("</textarea></td></tr>");

	Response.Write("<tr><td><br><b>Configuration</b>");

	//configuration table
	Response.Write("<table valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=50>NAME</td>\r\n");
	Response.Write("<td width=50>COLUMN_NUMBER</td>\r\n");
	Response.Write("</tr>\r\n");

	Response.Write("<form action=pbupdate.aspx?supplier=" + target_supplier + "&file=" + HttpUtility.UrlEncode(target_file) + " method=post>");

	for(int i=0; i<aNameCount; i++)
	{
		Response.Write("<tr><td><b>" + aName[i].ToUpper() + "</b></td>");
		Response.Write("<td><input type=text size=110 name=" + aName[i] + " value='");
		if(i == 13)
			Response.Write(m_sCheckSumLine);
		if(aValue[i] != 0)
			Response.Write(aValue[i]);
		Response.Write("'></td></tr>");
	}

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Test " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value=Save " + Session["button_style"] + ">");
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('pbupdate.aspx?r=" + DateTime.Now.ToOADate() + "') value=OK>");
	Response.Write("</td></tr>");
	Response.Write("</form>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
	Response.Write("</center>");
	return true;
}

bool DoSaveConfiguration()
{
	string sc = "IF NOT EXISTS (SELECT id FROM csv_format WHERE supplier='" + target_supplier + "') ";
	sc += " INSERT INTO csv_format (supplier ";
	for(int i=0; i<aNameCount; i++)
		sc += ", " + aName[i];
	sc += ") VALUES('" + target_supplier + "' ";
	for(int i=0; i<aNameCount-1; i++)
		sc += ", '" + Request.Form[aName[i]] + "' ";
	sc += ", '" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += ") ELSE UPDATE csv_format SET ";
	for(int i=0; i<aNameCount-1; i++)
	{
		sc += aName[i] + "='" + Request.Form[aName[i]] + "', ";
	}
	sc += aName[aNameCount-1] + "='" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += " WHERE supplier='" + target_supplier + "' ";
//DEBUG("sc=", sc);	
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

bool WriteAutoUpdateLog()
{
	string root = "./data";
	root = Server.MapPath(root);
	if(Directory.Exists(root))
	{
		string[] dirs = Directory.GetDirectories(root);
		string path = "";
		string dir = "";
		foreach(string sd in dirs)
		{
			path = sd;
			int len = target_supplier.Length;
			if(sd.Length < len)
				continue;
			string sdir = sd.Substring(sd.Length - len, len);
			if(sdir.ToLower() != target_supplier.ToLower())
				continue;

			dir = path.Substring(root.Length+1, path.Length-root.Length-1);
			DirectoryInfo di = new DirectoryInfo(sd);
			foreach (FileInfo f in di.GetFiles("*.csv")) 
			{
				string s = f.FullName;
				string file = s.Substring(path.Length+1, s.Length-path.Length-1);
				if(file == target_file)
				{
					string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
					string sc = " DELETE FROM auto_update_log WHERE file_id='" + file_id + "'\r\n";
					sc += " INSERT INTO auto_update_log (file_id, updated_by) VALUES('" + file_id + "', '" + Session["name"].ToString() + "')";
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
			}
		}
	}
	Response.Write("<h3>Error Write Auto Update Log</h3>");
	return false;
}

Boolean	PrepareData()
{
	if(!EmptyTempTables())
	{
		Response.Write("<font color=red>Error clearing temp tables. Program terminated.</font><br>\r\n");
		return false;
	}

	Response.Write("Calculating next product code........");
	Response.Flush();
	next_code = GetNextCode();
	if(next_code == -1)
	{
		Response.Write("<font color=red>Error getting next code. Program terminated.</font><br>\r\n");
		return false;
	}
	Response.Write("done<br>\r\n");
	
	Response.Write("Cacheing product table......");
	Response.Flush();
	if(!CacheProductTable())
	{
		Response.Write("<font color=red>Error getting product cache. Program terminated.</font><br>\r\n");
		return false;
	}
	Response.Write("done<br>\r\n");

	Response.Write("Creating temperary tables.......");
	Response.Flush();
	if(!CreateTempTable())
	{
		Response.Write("<font color=red>Error creating temp table. Program terminated.</font><br>\r\n");
		return false;
	}
	Response.Write("done<br>\r\n");
	Response.Flush();
	return true;
}

Boolean CreateTempTable()
{
	string sc = "";
	sc = "SELECT TOP 0 * FROM code_relations"; 
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "new_code");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean CacheCodeRelations(string supplier)
{
	Response.Write("Caching code realations for ");
	Response.Write(supplier);
	Response.Write(".........");
	Response.Flush();
	//delete all data
	if(dsc.Tables["code_relations"] != null)
		dsc.Tables["code_relations"].Clear();

	code_index = 0;

	int rows;
	string sc = "SELECT supplier, supplier_code, skip FROM code_relations ";
	sc += "WHERE supplier='";
	sc += supplier;
	sc += "' ORDER BY supplier_code";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsc, "code_relations");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	//sort it again!
	dracr = dsc.Tables["code_relations"].Select("", "supplier_code");
	Response.Write("done<br>\r\n");
	return true;
}

Boolean CacheProductTable()
{
	string sc = "SELECT code, stock, supplier_price, eta FROM product";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsc, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

Boolean EmptyTempTables()
{
	string sc = "DELETE product_raw";
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
	
	sc = "DELETE product_new";
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

	sc = "DELETE code_relations_new";
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

void BindGrid()
{
	DataView dv ;

//	dv = new DataView(dst.Tables["temp_rn"]);
//	dv = new DataView(dsc.Tables["code_relations"]);
//	dv = new DataView(dst.Tables["product_raw"]);

	string sc = "SELECT TOP 100 * FROM product_raw ";

	myAdapter = new SqlDataAdapter(sc, myConnection);
	DataSet ds = new DataSet();
	myAdapter.Fill(ds);
	dv = new DataView(dst.Tables["new_code"]);

//	dv = new DataView(dsc.Tables["code_relations"]);

	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();
}

bool GetFiles()
{
	string root = "./data/" + target_supplier + "/";
	root = Server.MapPath(root);
	if(target_file == "")
		return false;

	if(target_file != "")
	{
		if(!CacheCodeRelations(target_supplier.ToUpper()))
			return false;
		if(!AutoProcessFile(root + target_file))
			return false;
		files_processed++;
	}
	return true;
}

Boolean AutoProcessFile(string fileName)
{
	Boolean bRet = true;

	int index = 0;
	int len = 0;
	string sLine = "";

	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string tableName = "temp_" + target_supplier;
	int i = 0;
	sLine = r.ReadLine();
	Response.Write("Creating temp " + target_supplier + " Table");
	if(!CreateTempRawTable(tableName))
		return false;
	
	int nSkipLines = 0;
	if(aValue[11] != 0)
		nSkipLines = aValue[11];

	int nChecksumLineNumber = 0;
	try
	{
		nChecksumLineNumber = aValue[12];
	}
	catch(Exception e) 
	{
		//do nothing if falied
	}
	int products = 0;
	while(sLine != null)
	{
		i++;
		if(m_bTestFormat)
		{
			if(i > nSkipLines + 10)
				break;
		}
		if(sLine != "" && i > nSkipLines)
		{
			MonitorProcess(1000);
			if(!AutoProcessLine(tableName, sLine))
			{
				bRet = false;
				break;
			}
			products++;
		}
		else if(i == nChecksumLineNumber)
		{
//DEBUG("line=", sLine);
			if(m_sCheckSumLine != sLine)
			{
				Response.Write("<br><br><center><h3>Error, Checksum Line changed, please check file format</h3>");
				r.Close();
				return false;
			}
		}
		sLine = r.ReadLine();
	}
	Response.Write("done. <font color=red>");
	Response.Write(products);
	Response.Write(" </font>items added to temp table<br>\r\n");
	r.Close();

	if(m_bTestFormat)
		return true;

	return SortProcessTempTable(target_supplier.ToUpper(), tableName);
}

bool AutoProcessLine(string tableName, string sLine)
{
	bool bRet = true;
	char[] cb = sLine.ToCharArray();
	int pos = 0;

	//get all column
	for(int i=1; i<64; i++)
	{
		aColumn[i] = CSVNextColumn(cb, ref pos);
//		if(aColumn[i] == "")
//			break;
	}
	aColumn[0] = ""; //set the last one to blank
//DEBUG("value=", aValue[0]);

//	int col_number = 0;
//	string scol = aValue[0];
	
	string supplier_code	= aColumn[aValue[0]];
//DEBUG("s_c=", supplier_code);
	if(supplier_code == "") //skip empty lines
		return true;
	
	Trim(ref supplier_code);
//DEBUG("brandc=", aValue[2]);
	string brand			= aColumn[aValue[2]];
//DEBUG("brand=", brand);
	string supplier_name	= aColumn[aValue[1]];
	string supplier_price	= aColumn[aValue[6]];
	string RRP				= aColumn[aValue[7]];
	string stock			= aColumn[aValue[8]];
	string eta				= aColumn[aValue[9]];
	string cat				= aColumn[aValue[3]];
	string s_cat			= aColumn[aValue[4]];
	string ss_cat			= aColumn[aValue[5]];
	string details			= aColumn[aValue[10]];

	int p = 14;
	string price1			= aColumn[aValue[p++]];
	string price2			= aColumn[aValue[p++]];
	string price3			= aColumn[aValue[p++]];
	string price4			= aColumn[aValue[p++]];
	string price5			= aColumn[aValue[p++]];
	string price6			= aColumn[aValue[p++]];
	string price7			= aColumn[aValue[p++]];
	string price8			= aColumn[aValue[p++]];
	string price_system		= aColumn[aValue[p++]];
	string barcode			= aColumn[aValue[p++]];

	if(cat == "")
		cat = "Category";

	string supup = target_supplier.ToUpper();
	bRet = AddToTempRawTable(tableName, supplier_code, brand, supplier_name, supplier_price
		, RRP, stock, eta, cat, s_cat, ss_cat, details
		, price1, price2, price3, price4, price5, price6, price7, price8, price_system, barcode);
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
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
		}
	}
	else
	{
		while(cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
			pos++;
			if(pos == cb.Length)
				break;
		}
		pos++;
	}
	return new string(cbr, 0, i);
}

string MapColumns(string sCol)
{
	string s = "";
	string sn = "";
	int m = 0;
	bool bGotOne = false;
	int len = sCol.Length;
	for(int i=0; i<len; i++)
	{
		sn += sCol[i];
		if(i+1 >= len) //the end
		{
			bGotOne = true;
		}
		else if(sCol[i+1] == ' ' || sCol[i+1] == ',')
		{
				bGotOne = true;
				i++;
		}

		if(bGotOne)
		{
			m = int.Parse(sn);
			s += aColumn[m] + " ";
			sn = "";
			bGotOne = false;
		}
	}
	return s;
}

Boolean ProcessDataRow(string supplier, string supplier_code, string supplier_name, string brand, string cat, 
	string s_cat, string ss_cat, string stock, string eta, string supplier_price, string rrp, string details,
	string price1, string price2, string price3, string price4, string price5, string price6, string price7, 
	string price8, string price_system, string barcode)
{
	Boolean bRet = true;

	try
	{
		price1 = MyMoneyParse(price1).ToString();
	}
	catch(Exception e)
	{
		price1 = "999999";
	}

	try
	{
		price2 = MyMoneyParse(price2).ToString();
	}
	catch(Exception e)
	{
		price2 = "999999";
	}
	try
	{
		price3 = MyMoneyParse(price3).ToString();
	}
	catch(Exception e)
	{
		price3 = "999999";
	}
	try
	{
		price4 = MyMoneyParse(price4).ToString();
	}
	catch(Exception e)
	{
		price4 = "999999";
	}
	try
	{
		price5 = MyMoneyParse(price5).ToString();
	}
	catch(Exception e)
	{
		price5 = "999999";
	}
	try
	{
		price6 = MyMoneyParse(price6).ToString();
	}
	catch(Exception e)
	{
		price6 = "999999";
	}
	try
	{
		price7 = MyMoneyParse(price7).ToString();
	}
	catch(Exception e)
	{
		price7 = "999999";
	}
	try
	{
		price8 = MyMoneyParse(price8).ToString();
	}
	catch(Exception e)
	{
		price8 = "999999";
	}

	try
	{
		price_system = MyMoneyParse(price_system).ToString();
	}
	catch(Exception e)
	{
		price_system = "999999";
	}

	//get nearest price level for missing prices
	if(price1 == "0" || price1 == "999999")
	{
		if(price2 != "0" && price2 != "999999")
			price1 = price2;
		else if(price3 != "0" && price3 != "999999")
			price1 = price3;
	}
	if(price2 == "0" || price2 == "999999")
	{
		if(price1 != "0" && price1 != "999999")
			price2 = price1;
		else if(price3 != "0" && price3 != "999999")
			price2 = price3;
	}
	if(price3 == "0" || price3 == "999999")
	{
		if(price2 != "0" && price2 != "999999")
			price3 = price2;
		else if(price1 != "0" && price1 != "999999")
			price3 = price1;
	}

//	if(stock == "")
//		stock = "0";
//	else if(!TSIsDigit(stock))
	try
	{
		MyIntParse(stock);
	}
	catch(Exception e)
	{
		string s = "";
		for(int i=0; i<stock.Length; i++)
		{
			if(!Char.IsDigit(stock[i]))
				break;
			s += stock[i];
		}
		stock = s;
	}

	if(stock == null || stock == "")
		stock = "1";

	string name = "";
	Boolean skip = false;
	Boolean bIsNew = false;
	if(!GetTopsysCode(supplier, supplier_code, supplier_name, supplier_price, rrp, brand, cat, s_cat, ss_cat, ref skip, ref bIsNew,
		price1, price2, price3, price4, price5, price6, price7, price8, price_system, stock, barcode))
	{
		Response.Write("<font size=+2 color=red><b>Error Getting New Code</b></font><br>\r\n");
		Response.Flush();
		return false; //error getting new code
	}

	string sc = "";

	double dsupplier_price = 9999999;
/*	try
	{
		dsupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
		if(dsupplier_price <= 0)
			return true;
	}
	catch(Exception e) 
	{
		return true;
	}
*/
	double dPlus = 0;
	if(dsupplier_price < 50)
		dPlus = 10;
	else if(dsupplier_price <= 200)
		dPlus = 5;

	double dRRP = 99999;
	if(rrp != "" && rrp != "0")
	{
		try
		{
			dRRP = double.Parse(rrp, NumberStyles.Currency, null);
		}
		catch (Exception e)
		{
		}
	}

	string id = supplier;
	id += supplier_code;
/*
	if(skip)
	{
		dropped_items++;
	
		string sc = "";
		sc += " UPDATE product_skip SET supplier_price=";
		sc += dsupplier_price;
		sc += ", price=";
		if(dRRP > 0 && dRRP != 99999)
			sc += dRRP;
		else
			sc += dsupplier_price + " * (SELECT rate FROM code_relations WHERE id='" + id + "') + " + dPlus;
		sc += ", stock=";
		sc += stock;
		sc += ", eta='";
		sc += eta;
		sc += "' WHERE id='";
		sc += id;
		sc += "'";
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e1) 
		{
			ShowExp(sc, e1);
			return false;
		}
		return true;
	}
*/
	//add to tmp table
/*
	string sTableName = "product_raw";
	if(bIsNew)
		sTableName = "product_new";

	DataRow dr = dst.Tables[sTableName].NewRow();

	dr["id"] = id;
	dr["stock"]	= stock;
	dr["eta"] = eta;
	dr["supplier_price"] = dsupplier_price;
	dr["details"] = details;
	
	if(bIsNew)
	{
		if(dRRP > 0 && dRRP != 99999)
			dr["price"] = dRRP;
		else
			dr["price"] = dsupplier_price * 1.1 + dPlus;
	}

	dst.Tables[sTableName].Rows.Add(dr);
*/
	total_items++;
	return bRet;
}

Boolean GetTopsysCode(string supplier, string supplier_code, string supplier_name, string supplier_price, 
	string rrp, string brand, string cat,  string s_cat, string ss_cat, ref Boolean skip, ref Boolean bIsNew,
	string price1, string price2, string price3, string price4, string price5, string price6, string price7, 
	string price8, string price_system, string stock, string barcode)
{
	Boolean found = false;
	DataRow dr;
	if(dracr.Length > code_index)
	{
		dr = dracr[code_index];
		string csupplier_code = dr["supplier_code"].ToString();
		Trim(ref csupplier_code);
		Trim(ref supplier_code);
		int nRet = string.Compare(supplier_code, csupplier_code);
		if(nRet == 0)
			found = true;
		else if(nRet > 0)  //new code not in the same order as cached code table, maybe supplier discontinued some products.......
		{
			if(DumpAndSearch(supplier_code))
			{
				found = true;
			}
		}
	}
	if(found)
	{
		dr = dracr[code_index];
		skip = Boolean.Parse(dr["skip"].ToString());
		if(!UpdateCodeRelations(supplier, supplier_code, supplier_name, supplier_price, rrp, brand, cat, s_cat, ss_cat,
			price1, price2, price3, price4, price5, price6, price7, price8, price_system, stock, barcode))
			return false;
		code_index++;
		old_items++;
	}
	else
	{
		if(!AddNewCodeToTempTable(supplier, supplier_code, supplier_name, supplier_price, rrp, brand, cat, s_cat, ss_cat,
			price1, price2, price3, price4, price5, price6, price7, price8, price_system, stock, barcode))
			return false;
		bIsNew = true;
	}
	return true;;
}

Boolean DumpAndSearch(string supplier_code)
{
	DataRow dr;
	int rows = dracr.Length - 1;
	string sc = "";
	int nRet = -1;
	while(code_index < rows)
	{
		deleted_items++;
		//store code to dump
		if(m_sDumpP != "")
			m_sDumpP += ",";
		m_sDumpP += "'" + dracr[code_index]["supplier_code"].ToString() + "'";
		if(m_sDumpK != "")
			m_sDumpK += ",";
		m_sDumpK += "'" + target_supplier + dracr[code_index]["supplier_code"].ToString() + "'";

		code_index++;

		dr = dracr[code_index];
		string csupplier_code = dr["supplier_code"].ToString();
		Trim(ref csupplier_code);
		
		nRet = string.Compare(supplier_code, csupplier_code);
		if(nRet == 0)
			return true; //yes we found it
		if(nRet < 0)
		{
			return false;
		}
	}
	return false; //didn't find, all dumpped? how many products they removed
}

Boolean DumpCurrentCode(string supplier_code)
{
	string sc = " ";
/*	if(g_bRetailVersion) //check if have real stock
	{
		if(dst.Tables["dump_check"] != null)
			dst.Tables["dump_check"].Clear();
		sc = " SELECT q.qty ";
		sc += " FROM stock_qty q INNER JOIN code_relations c ON q.code = c.code	";
		sc += " WHERE c.supplier = '" + target_supplier.ToUpper() + "' AND c.supplier_code = '" + supplier_code + "' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dst, "dump_check") > 0)
			{
				return true;
			}
		}
		catch(Exception e1) 
		{
			ShowExp(sc, e1);
			return false;
		}
	}
*/
	if(g_bRetailVersion)
	{
		sc += " IF NOT EXISTS (SELECT q.qty FROM stock_qty q JOIN code_relations c ON q.code = c.code ";
		sc += " WHERE c.supplier = '" + target_supplier.ToUpper() + "' AND c.supplier_code = '" + supplier_code + "') ";
		sc += " BEGIN ";
	}
	sc += " DELETE FROM product WHERE supplier='" + target_supplier + "' AND supplier_code='" + supplier_code + "'";
	sc += " DELETE FROM product_skip WHERE id='" + target_supplier + supplier_code + "'";
	if(g_bRetailVersion)
		sc += " END ";
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
	deleted_items++;
	return true; //we don't delete old code hence 18, Feb. 2002 DW
}

bool UpdateCodeRelations(string supplier, string supplier_code, string supplier_name,
		string supplier_price, string rrp, string brand, string cat, string s_cat, string ss_cat,
		string price1, string price2, string price3, string price4, string price5, string price6, string price7, 
		string price8, string price_system, string stock, string barcode)
{
	//update code_relations table
	string id = supplier;
	id += supplier_code;
	double dSupplier_price = 0;
	double drate = 1;
	double drrp = 0;

	string sc = "UPDATE code_relations ";
	sc += " SET ";
	sc += " name='" + supplier_name + "' ";
	sc += ", brand='" + brand + "' ";
	sc += ", cat='" + cat + "' ";
	sc += ", s_cat='" + s_cat + "' ";
	sc += ", ss_cat='" + ss_cat + "' ";
	sc += ", price1=" + price1;
	sc += ", price2=" + price2;
	sc += ", price3=" + price3;
	sc += ", price4=" + price4;
	sc += ", price5=" + price5;
	sc += ", price6=" + price6;
	sc += ", price7=" + price7;
	sc += ", price8=" + price8;
	sc += ", price_system=" + price_system;
	sc += ", barcode='" + barcode + "' ";
	sc += " WHERE id='" + id + "' ";

	sc += " UPDATE product SET ";
	sc += " name = '" + supplier_name + "' ";
	sc += ", brand='" + brand + "' ";
	sc += ", cat='" + cat + "' ";
	sc += ", s_cat='" + s_cat + "' ";
	sc += ", ss_cat='" + ss_cat + "' ";
	sc += ", stock=" + stock;
	sc += " WHERE supplier+supplier_code = '" + id + "' ";

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

bool AddNewCodeToTempTable(string supplier, string supplier_code, string supplier_name,
		string supplier_price, string rrp, string brand, string cat, string s_cat, string ss_cat,
		string price1, string price2, string price3, string price4, string price5, string price6, string price7, 
		string price8, string price_system, string stock, string barcode)
{
	//update code_relations table
	string id = supplier;
	id += supplier_code;
	double dSupplier_price = 99999;
	try
	{
		dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
		if(dSupplier_price <= 0)
			return true;
	}
	catch(Exception e) 
	{
		return true;
	}

	new_items++;
	double drate = 1;//GetDefaultPriceRate(dSupplier_price);
	double drrp = 0;
	try
	{
		drrp = double.Parse(rrp, NumberStyles.Currency, null);
		if(drrp != 0)
			drate = drrp / dSupplier_price;
	}
	catch(Exception e)
	{
	}

	drate = Math.Round(drate, 2);

	string code = next_code.ToString();
	next_code++;

	//insert code_relations
	string sc = " IF NOT EXISTS (SELECT code FROM code_relations WHERE id='" + id + "') ";
	sc += " BEGIN ";
	sc += " INSERT INTO code_relations (id, supplier, supplier_code, supplier_price, code ";
	sc += ", name, brand, cat, s_cat, ss_cat, skip, hot, rate, popular ";
	sc += ", price1, price2, price3, price4, price5, price6, price7, price8, price_system, rrp, barcode ";
	sc += ")";
	sc += " VALUES('";
	sc += id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += dSupplier_price;
	sc += ", ";
	sc += code;
	sc += ", '";
	sc += supplier_name;
	sc += "', '";
	sc += brand;
	sc += "', '";
	sc += cat;
	sc += "', '";
	sc += s_cat; 
	sc += "', '";
	sc += ss_cat;
	sc += "', 0, 1, ";
	sc += drate;
	sc += ", 1, ";
	sc += price1;
	sc += ", ";
	sc += price2;
	sc += ", ";
	sc += price3;
	sc += ", ";
	sc += price4;
	sc += ", ";
	sc += price5;
	sc += ", ";
	sc += price6;
	sc += ", ";
	sc += price7;
	sc += ", ";
	sc += price8;
	sc += ", ";
	sc += price_system;
	sc += ", " + drrp;
	sc += ", '" + barcode + "' ";
	sc += ")";

//	if(stock == null || stock == "")
//		stock = "1";

	sc += " INSERT INTO product (code, name, brand, cat, s_cat, ss_cat, price, stock, supplier, supplier_code ";
	sc += ", supplier_price, popular) VALUES (";
	sc += code + ", '" + supplier_name + "', '" + brand + "', '" + cat + "', '" + s_cat + "', '" + ss_cat + "' ";
	sc += ", " + drrp;
	sc += ", " + stock;
	sc += ", '" + m_sCompanyName.ToUpper() + "'";
	sc += ", '" + supplier_code + "' ";
	sc += ", 0"; //no supplier price for pb
	sc += ", 1)"; //all pb product set to populer

	sc += " END "; //end if not exists id

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
		if(e.ToString().IndexOf("Violation of PRIMARY KEY constraint") >= 0)
		{
			myCommand.Connection.Close();
//			DEBUG("Error, Duplite product code found ! supplier_code=" + supplier_code, " skip");
			return true;
		}
		ShowExp(sc, e);
		return false;
	}
	return true;
}


Boolean AddToTempRawTable(string tableName, string supplier_code, string brand, string supplier_name, string supplier_price, 
	string RRP, string stock, string eta, string cat, string s_cat, string ss_cat, string details,
	string price1, string price2, string price3, string price4, string price5, string price6, 
	string price7, string price8, string price_system, string barcode)
{
//	details = DropHTMLCode(details);
	DataRow myDataRow;

	if(supplier_price == "")
		supplier_price = "0";
	
	brand = RemoveQuote(brand);
	supplier_name = RemoveQuote(supplier_name);
	cat = RemoveQuote(cat);
	s_cat = RemoveQuote(s_cat);
	ss_cat = RemoveQuote(ss_cat);
	details = RemoveQuote(details);

	if(supplier_name.Length > 254)
		supplier_name = supplier_name.Substring(0, 254);
	if(cat.Length > 49)
		cat = cat.Substring(0, 49);
	if(s_cat.Length > 49)
		s_cat = s_cat.Substring(0, 49);
	if(ss_cat.Length > 49)
		ss_cat = ss_cat.Substring(0, 49);
	
	double dcost = 0;
	double drrp = 0;
	double drate = 0;

/*	try
	{
		dcost = double.Parse(supplier_price, NumberStyles.Currency, null);
		drrp = double.Parse(RRP, NumberStyles.Currency, null);
		drate = Math.Round(drrp/dcost, 2);
		if(drate > 1.6)
			drate -= 0.07;
		else if(drate > 1.5)
			drate -= 0.06;
		else if(drate > 1.4)
			drate -= 0.04;
		else if(drate > 1.3)
			drate -= 0.04;
		else if(drate > 1.2)
			drate -= 0.03;
		else if(drate > 1.1)
			drate -= 0.02;
		else if(drate > 1.01)
			drate -= 0.01;
		drrp = dcost * drate;
		drate = CalculatePriceRate(dcost, drrp);
	}
	catch(Exception e)
	{
	}
*/	
	myDataRow = dst.Tables[tableName].NewRow();

	myDataRow["supplier_code"]	= supplier_code;
	myDataRow["supplier_name"]	= supplier_name;
	myDataRow["brand"]			= brand;
	myDataRow["supplier_price"]	= supplier_price;
	myDataRow["rrp"]			= drrp.ToString();
	myDataRow["rate"]			= drate.ToString();
	myDataRow["stock"]			= stock;
	myDataRow["eta"]			= eta;
	myDataRow["cat"]			= cat;
	myDataRow["s_cat"]			= s_cat;
	myDataRow["ss_cat"]			= ss_cat;
	myDataRow["details"]		= details;

	myDataRow["price1"]		= price1;
	myDataRow["price2"]		= price2;
	myDataRow["price3"]		= price3;
	myDataRow["price4"]		= price4;
	myDataRow["price5"]		= price5;
	myDataRow["price6"]		= price6;
	myDataRow["price7"]		= price7;
	myDataRow["price8"]		= price8;
	myDataRow["price_system"]		= price_system;
	myDataRow["barcode"]	= barcode;

	dst.Tables[tableName].Rows.Add(myDataRow);
	return true;
}

Boolean CreateTempRawTable(string tableName)
{
	// Create a new DataTable.
	System.Data.DataTable myDataTable = new DataTable(tableName);

	// Declare variables for DataColumn and DataRow objects.
	DataColumn myDataColumn;

	string[] cn = new string[64];

	cn[0] = "supplier_code";
	cn[1] = "supplier_name";
	cn[2] = "brand";
	cn[3] = "supplier_price";
	cn[4] = "rrp";
	cn[5] = "rate";
	cn[6] = "stock";
	cn[7] = "eta";
	cn[8] = "cat";
	cn[9] = "s_cat";
	cn[10] = "ss_cat";
	cn[11] = "details";
	cn[12] = "price1";
	cn[13] = "price2";
	cn[14] = "price3";
	cn[15] = "price4";
	cn[16] = "price5";
	cn[17] = "price6";
	cn[18] = "price7";
	cn[19] = "price8";
	cn[20] = "price_system";
	cn[21] = "barcode";

	// Create new DataColumn, set DataType, ColumnName and add to DataTable.    
	for(int i=0; i<=21; i++)
	{
		myDataColumn = new DataColumn();
		myDataColumn.DataType = System.Type.GetType("System.String");
		myDataColumn.ColumnName = cn[i];
		myDataColumn.ReadOnly = false;
		myDataColumn.Unique = false;
		myDataTable.Columns.Add(myDataColumn);
	}

	// Add the new DataTable to the DataSet.
	dst.Tables.Add(myDataTable);
	return true;
}

Boolean SortProcessTempTable(string supplier, string tableName)
{
	Boolean bRet = true;

	Response.Write("Sorting ");
	Response.Write(supplier);
	Response.Write(" table..........");
	DataRow[] dra = dst.Tables[tableName].Select("", "supplier_code");
	Response.Write("done<br>\r\n");

/*	int rows = dra.Length;
	string supplier_code;
	string brand;
	string supplier_name;
	string supplier_price;
	string price;
	string stock;
	string eta;
	string cat;
	string s_cat;
	string ss_cat;
	string details;
	int items_got = total_items;
*/	
	int items = 0;	
	Response.Write("Processing ");
	Response.Write(supplier);
	Response.Write(" table <font color=green>...");
	Response.Flush();

	Random rnd = new Random();
	m_pMonitor = rnd.Next(0, m_sMonitor.Length);

	int i = 0;
//	int line = 0;
//	int j = 0;
//	int m = 0;
	for(i=0;i<dra.Length;i++)
	{
//DEBUG("rows="+dra.Length+" i=", i);
		string scode = dra[i]["supplier_code"].ToString();
		if(scode == "" || scode.Length >= 45)
			continue;
/*
		supplier_code	= dra[i]["supplier_code"].ToString();
		if(supplier_code == "")
			continue;

		brand		= dra[i]["brand"].ToString();
		supplier_name	= dra[i]["supplier_name"].ToString();
		supplier_price	= dra[i]["supplier_price"].ToString();
		price		= dra[i]["rrp"].ToString();
		stock		= dra[i]["stock"].ToString();
		eta			= dra[i]["eta"].ToString();
		cat			= dra[i]["cat"].ToString();
		s_cat		= dra[i]["s_cat"].ToString();
		ss_cat		= dra[i]["ss_cat"].ToString();
		details		= dra[i]["details"].ToString();
*/
		bRet = ProcessDataRow(supplier, dra[i]["supplier_code"].ToString(), 
			dra[i]["supplier_name"].ToString(), dra[i]["brand"].ToString(), 
			dra[i]["cat"].ToString(), dra[i]["s_cat"].ToString(), dra[i]["ss_cat"].ToString(), 
			dra[i]["stock"].ToString(), dra[i]["eta"].ToString(), dra[i]["supplier_price"].ToString(), 
			dra[i]["supplier_price"].ToString(), dra[i]["details"].ToString(),
			dra[i]["price1"].ToString(),
			dra[i]["price2"].ToString(),
			dra[i]["price3"].ToString(),
			dra[i]["price4"].ToString(),
			dra[i]["price5"].ToString(),
			dra[i]["price6"].ToString(),
			dra[i]["price7"].ToString(),
			dra[i]["price8"].ToString(),
			dra[i]["price_system"].ToString(),
			dra[i]["barcode"].ToString());
		if(!bRet)
			break;
		MonitorProcess(10);
		items++;
	}

	if(m_sDumpP != "")
	{
		string sc = "";
		//prepare dumpping flags
/*		sc = " UPDATE product SET real_stock = 1 WHERE code IN (SELECT code FROM stock_qty) ";
		sc += " UPDATE product_skip SET real_stock = 1 ";
		sc += " WHERE id IN (SELECT c.id FROM code_relations c JOIN stock_qty q ON c.code = q.code) ";
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e1) 
		{
			ShowExp(sc, e1);
			return false;
		}
*/
		StringBuilder sb = new StringBuilder();
		sb.Append(" DELETE FROM product WHERE supplier='");
		sb.Append(target_supplier);
		sb.Append("' AND supplier_code IN (");
		sb.Append(m_sDumpP);
		sb.Append(") DELETE FROM product_skip WHERE id IN (");
		sb.Append(m_sDumpK);
		sb.Append(") ");
//DEBUG("sc=", sb.ToString());
		try
		{
			myCommand = new SqlCommand(sb.ToString());
			myCommand.Connection = myConnection;
			myConnection.Open();
			myCommand.ExecuteNonQuery();
			myCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			ShowExp(sb.ToString(), e);
			return false;
		}
	}

	Response.Write("... </font>done<br>\r\n");
	Response.Flush();
	return bRet;
}

///////////////////////////////////////////////////////////////////////////////////////////
//Analyze functions
// * get all product from product_raw table accompany with code_relations
// * compare to product talbe, write changed items to product_update
Boolean GetSettings()
{
	string price_rate = GetSiteSettings("price_rate");
	if(price_rate == "")
	{
		price_rate = "1.1";
//		Response.Write("Error Getting price_rate settings, contact database administrator<br>\r\n");
///		return false;
	}

	string price_age = GetSiteSettings("price_age");
	if(price_age == "")
	{
		price_age = "7";
//		Response.Write("Error Getting price_age settings, contact database administrator<br>\r\n");
//		return false;
	}

	m_dPrice_rate_settings = double.Parse(price_rate, NumberStyles.Currency, null);
	m_nPrice_age_settings = int.Parse(price_age);

	return true;
}

void cmdSend_Click(object sender, System.EventArgs e)
{
	// Check to see if file was uploaded
	if( filMyFile.PostedFile != null )
	{
		// Get a reference to PostedFile object
		HttpPostedFile myFile = filMyFile.PostedFile;

		// Get size of uploaded file
		int nFileLen = myFile.ContentLength; 

		// make sure the size of the file is > 0
		if( nFileLen > 0 )
		{
			// Allocate a buffer for reading of the file
			byte[] myData = new byte[nFileLen];

			// Read uploaded file from the Stream
			myFile.InputStream.Read(myData, 0, nFileLen);

			// Create a name for the file to store
			string strFileName = Path.GetFileName(myFile.FileName);
			string sExt = Path.GetExtension(myFile.FileName);
			if(sExt.ToLower() != ".csv")
			{
				Response.Write("<h3>Error, " + strFileName + " is not a .csv file</h3>");
				return;
			}
			string m_fileName = strFileName;
			string supplier = m_sCompanyName.ToLower();
			string vpath = "./data/" + supplier + "/";
			string strPath = Server.MapPath(vpath);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += strFileName;
			
			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=pbupdate.aspx\">");
		}
	}
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


///////////////////////////////////////////////////////////////////////////////
//Write update
</script>

<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1 
	BorderStyle=Solid 
	BorderColor=#CCCCCC
	CellPadding=0 
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	HorizontalAlign=center
>

	<HeaderStyle BackColor=#CCCCCC ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=Beige/>
</asp:DataGrid>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<br>
<table align=center>
<tr><td colspan=4 align=center><font size=+1><b>Upload File</b><br>&nbsp;</td></tr>
<tr><td><b> File : </b><input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</form>

<asp:Label id=LFooter runat=server />
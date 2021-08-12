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
string target_supplier = "all";			//the one to process, if "all" then process all suppliers datafile
string target_file = "";
string target_file_id = "";
const string date_format = "MMM-dd HH:mm";

string m_sTimeStamp = "";

//for configuration
int aNameCount = 14;
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

	for(int i=0; i<aNameCount; i++)
		aValue[i] = 0;
	aName[0] = "code";
	aName[1] = "name";
	aName[2] = "brand";
	aName[3] = "cat";
	aName[4] = "s_cat";
	aName[5] = "ss_cat";
	aName[6] = "cost";
	aName[7] = "RRP";
	aName[8] = "stock";
	aName[9] = "eta";
	aName[10] = "details";
	aName[11] = "skip_lines";
	aName[12] = "checksum_line_number";
	aName[13] = "checksum_line_text";

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
			string root = GetRootPath() + "/admin/data";
			root = Server.MapPath(root);
			string pathname = root + "\\" + target_supplier + "\\" +  target_file;
//DEBUG("path=", pathname);
			if(File.Exists(pathname))
				File.Delete(pathname);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=autoupdate.aspx\">");
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

		string root = GetRootPath() + "/admin/data";
//		string root = GetRootPath() + ".../admin/data";
		root = Server.MapPath(root);
//DEBUG("root=", root);
		if(Directory.Exists(root))
		{
			Response.Write("<br><center><h3>Auto Update</h3>");
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
				DirectoryInfo di = new DirectoryInfo(sd);
				foreach (FileInfo f in di.GetFiles("*.csv")) 
				{
					string s = f.FullName;
					string file = s.Substring(path.Length+1, s.Length-path.Length-1);
					string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
					Response.Write("<tr><td><a href=autoupdate.aspx?supplier=" + dir + "&file=");
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
					Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('autoupdate.aspx?supplier=" + dir + "&file=" + HttpUtility.UrlEncode(file) + "') value=Process>");
					Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('autoupdate.aspx?t=delete&supplier=" + dir + "&file=" + HttpUtility.UrlEncode(file) + "') value=Delete>");
					string sp = dir.ToUpper();
//					if(sp != "TP" && sp != "BB" && sp != "IM" && sp != "RN")// && sp != "CD")
					if(sp != "TP" && sp != "IM" && sp != "RN")// && sp != "CD")
						Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('autoupdate.aspx?t=analyze&supplier=" + dir + "&file=" + HttpUtility.UrlEncode(file) + "') value='Analyze'>");
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
	Response.Write("<font color=blue>");
	Response.Flush();

	//get settings
	if(!GetSettings())
		return;

	//get data from data file
	if(!PrepareData())
		return;
	if(!GetFiles())
		return;
	if(!SaveData())
		return;
	
	//anaylze data
	Response.Write("<br><b>Analyze data ... </b><br>");
	Response.Flush();
	if(!PrepareTablesForAnalyze())
		return;
	if(!CollectAllForSaleItems()) //collect items from product_raw join code_relations to dst.Tables["product_all"]
		return;
	if(!AnalyzeData())
		return;

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
		Response.Write(" new items found, <a href=newitems.aspx?s=");
		Response.Write(DateTime.Now.ToOADate());
		Response.Write(">Edit New Items</a>");
	}
	Response.Write("</font>");
	Response.Write("<br><font color=red>");
	if(m_nUpdatedItems > 0)
	{
		Response.Write(m_nUpdatedItems);
		Response.Write(" items updated</font>, <a href=updatelog.aspx>check updatelog</a>");
	}
	else if(new_items < 0)
	{
		Response.Write("No changes detected</font>, back to <a href=default.aspx> Home </a>");
	}

	TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - dtStart.Ticks);
//	Response.Write("<br><font color=white><b>Updating used " + ((double)ts.TotalMilliseconds / 1000).ToString() + " seconds</b></font><br>");
	Response.Write("<br><font color=white><b>Updating used " + ts.TotalSeconds + " seconds</b></font><br>");

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
			if(i == 13)
				m_sCheckSumLine = nstr;
			else
				aValue[i] = MyIntParse(nstr);
		}
	}
	return true;	
}

bool PrintAnalyzePage()
{
	//read a few lines of the file
	string root = GetRootPath() + "/admin/data/" + target_supplier + "/";
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
	r.Close();
	fs.Close();

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

	Response.Write("<form action=autoupdate.aspx?supplier=" + target_supplier + "&file=" + HttpUtility.UrlEncode(target_file) + " method=post>");

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
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('autoupdate.aspx?r=" + DateTime.Now.ToOADate() + "') value=OK>");
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
	string root = GetRootPath() + "/admin/data";
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

Boolean SaveData()
{
	if(SaveTempTable("product_raw"))
	{
		if(SaveTempTable("product_new"))
			return true;
	}
	return false;
}

Boolean CreateTempTable()
{
	string sc = "SELECT TOP 0 * FROM product_raw"; 
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "product_raw");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "SELECT TOP 0 * FROM product_new"; 
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "product_new");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

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

	sc = "SELECT TOP 0 * FROM product"; 
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "product_temp");
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
	string root = GetRootPath() + "/admin/data/" + target_supplier + "/";
	root = Server.MapPath(root);
	if(target_file == "")
		return false;

	if(target_file != "")
	{
		if(String.Compare(target_supplier, "rn", true) == 0)
		{
			if(!CacheCodeRelations("RN"))
				return false;
		 	if(!RenaissanceProcessFile(root + target_file))
				return false;
			files_processed++;
		}
//		else if(String.Compare(target_supplier, "bb", true) == 0)
//		{
//			if(!CacheCodeRelations("BB"))
//				return false;
//		 	if(!BbfProcessFile(root + target_file))
//				return false;
//			files_processed++;
//		}
		else if(String.Compare(target_supplier, "tp", true) == 0 || String.Compare(target_supplier, "TP", true) == 0)
		{
			if(!CacheCodeRelations("TP"))
				return false;
		 	if(!TPProcessCVSFile(root + target_file))
				return false;
			files_processed++;
		}
		else if(String.Compare(target_supplier, "im", true) == 0)
		{
			if(!CacheCodeRelations("IM"))
				return false;
		 	if(!IMProcessFile(root + target_file))
				return false;
			files_processed++;
		}
//		else if(String.Compare(target_supplier, "cd", true) == 0)
//		{
//			if(!CacheCodeRelations("CD"))
//				return false;
//		 	if(!CDProcessFile(root + target_file))
//				return false;
//			files_processed++;
//		}
		else
		{
			if(!CacheCodeRelations(target_supplier.ToUpper()))
				return false;
			if(!AutoProcessFile(root + target_file))
				return false;
			files_processed++;
		}
		return true;
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

/*
	string supplier_code	= MapColumns(aValue[0]);
//DEBUG("s_c=", supplier_code);
	if(supplier_code == "") //skip empty lines
		return true;
	
	Trim(ref supplier_code);
//DEBUG("brandc=", aValue[2]);
	string brand			= MapColumns(aValue[2]);
//DEBUG("brand=", brand);
	string supplier_name	= MapColumns(aValue[1]);
	string supplier_price	= MapColumns(aValue[6]);
	string RRP				= MapColumns(aValue[7]);
	string stock			= MapColumns(aValue[8]);
	string eta				= MapColumns(aValue[9]);
	string cat				= MapColumns(aValue[3]);
	string s_cat			= MapColumns(aValue[4]);
	string ss_cat			= MapColumns(aValue[5]);
	string details			= MapColumns(aValue[10]);
*/	
	string supup = target_supplier.ToUpper();
	if(supup == "RN")
	{
		char[] cbs = cat.ToCharArray();
	//	cat = "hardware";
		if(cbs.Length > 0)
			cat = RenaissanceCat(cbs, ref s_cat, ref ss_cat);
	}
	else if(supup == "IM")
	{
		char[] cbs = brand.ToCharArray();
		if(cbs.Length > 0)
			brand = IMCat(cbs, ref cat);

		try
		{
			double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
		}
		catch(Exception e) 
		{
//		DEBUG("Error, price incorrect : " + supplier_price + " , code=" + (next_code-1),  " , reset to 9999 (AddToTempTable())");
			//most likely it has two dots, try to correct it
			char[] cbp = supplier_price.ToCharArray();
			supplier_price = "";
			bool bAlreadyHadPoint = false;
			for(int i=0; i<cbp.Length; i++)
			{
				if(cbp[i] == '.')
				{
					if(bAlreadyHadPoint)
						continue; //already got a dot, skip this 2nd one
					else
						bAlreadyHadPoint = true; //set flag, go get the dot
				}
				supplier_price += cbp[i];
			}
		}
	}
//	else if(supup == "BB")
//	{
//		char[] cbs = brand.ToCharArray();
//		ss_cat = BbfCat(s_cat, cbs, ref brand);
//	}
	bRet = AddToTempRawTable(tableName, supplier_code, brand, supplier_name, supplier_price, RRP, stock, eta, cat, s_cat, ss_cat, details);
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

Boolean RenaissanceProcessFile(string fileName)
{
	Boolean bRet = true;

	int index = 0;
	int len = 0;
	string sLine = "";

	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string tableName = "temp_rn";
	int i = 0;
	sLine = r.ReadLine();
	Response.Write("Creating temp Renaissance Table");
	if(!CreateTempRawTable(tableName))
		return false;
	
	int products = 0;
	while(sLine != null)
	{
		i++;
		if(sLine != "")
		{
			MonitorProcess(1000);
			if(!RenaissanceProcessLine(tableName, sLine))
			{
				bRet = false;
				break;
			}
			products++;
		}
		sLine = r.ReadLine();
	}
	Response.Write("done. <font color=red>");
	Response.Write(products);
	Response.Write(" </font>items added to temp table<br>\r\n");
	r.Close();

	return SortProcessTempTable("RN", tableName);
}

Boolean RenaissanceProcessLine(string tableName, string sLine)
{
	Boolean bRet = true;

	char[] cb = sLine.ToCharArray();

	int pos = 0;

	string supplier_code	= CSVNextColumn(cb, ref pos);
	if(supplier_code == "") //skip empty lines
		return true;
	string brand		= CSVNextColumn(cb, ref pos);
	string supplier_name	= CSVNextColumn(cb, ref pos);
	string supplier_price	= CSVNextColumn(cb, ref pos);
	string RRP			= CSVNextColumn(cb, ref pos);
	string stock		= CSVNextColumn(cb, ref pos);
	string eta			= "";
	string cat			= CSVNextColumn(cb, ref pos);
	string details		= CSVNextColumn(cb, ref pos);
	
	char[] cbs = cat.ToCharArray();
	string s_cat = "";
	string ss_cat = "";
//	cat = "hardware";
	if(cbs.Length > 0)
		cat = RenaissanceCat(cbs, ref s_cat, ref ss_cat);

	bRet = AddToTempRawTable(tableName, supplier_code, brand, supplier_name, supplier_price, "", stock, eta, cat, s_cat, ss_cat, details);
	return bRet;
}

string RenaissanceCat(char[] cb, ref string s_cat, ref string ss_cat)
{
	string cat;
	int sep = 0;
	int sep1 = 0;
	int i = 0;
	for(;i<cb.Length;i++)
	{	
		if(cb[i] == ' ')
		{
			sep = i;
			break;
		}			
	}
	i++;
	for(;i<cb.Length;i++)
	{	
		if(cb[i] == ' ')
		{
			sep1 = i;
			break;
		}			
	}
	if(sep > 0)
	{
		cat = new string(cb, 0, sep);
		sep++;
		if(sep1 > 0)
		{
			s_cat = new string(cb, sep, sep1 - sep);
			sep1++;
			ss_cat = new string(cb, sep1, cb.Length - sep1);
		}
		else
		{
			s_cat = new string(cb, sep, cb.Length - sep);
		}
	}
	else
	{
		cat = new string(cb);
	}

	return cat;
}

bool IMProcessFile(string fileName)
{
	Boolean bRet = true;

	int index = 0;
	int len = 0;
	string sLine = "";

	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string tableName = "temp_im";
	int i = 0;
	sLine = r.ReadLine();
	Response.Write("Creating temp IM Table");
	if(!CreateTempRawTable(tableName))
		return false;
	
	int products = 0;
	while(sLine != null)
	{
		i++;
		if(i == 1) //first line, column name
		{
			if(sLine != "\"Part.\",\"Vend Part.\",\"Total Desc\",\"vdr_name\",\"Category\",\"Available\",\"ResellerBuy\"")
			{
				Response.Write("<h3>IM file format changed, cannot carry on, program quit</h3>");
				return false;
			}
			i++;
			sLine = r.ReadLine(); //next line
		}
		if(sLine != "")
		{
			MonitorProcess(1000);
			if(!IMProcessLine(tableName, sLine))
			{
				bRet = false;
				break;
			}
			products++;
		}
		sLine = r.ReadLine();
//if(i>2)
//	break;
	}
	Response.Write("done. <font color=red>");
	Response.Write(products);
	Response.Write(" </font>items added to temp table<br>\r\n");
	r.Close();

	return SortProcessTempTable("IM", tableName);
}

Boolean IMProcessLine(string tableName, string sLine)
{
	Boolean bRet = true;

	char[] cb = sLine.ToCharArray();

	int pos = 0;

	string supplier_code	= CSVNextColumn(cb, ref pos);
	if(supplier_code == "") //skip empty lines
		return true;
	CSVNextColumn(cb, ref pos); //skip vender code
	string supplier_name	= CSVNextColumn(cb, ref pos);
	string brand		= CSVNextColumn(cb, ref pos);
	string s_cat		= CSVNextColumn(cb, ref pos);
	string stock		= CSVNextColumn(cb, ref pos);
	string supplier_price	= CSVNextColumn(cb, ref pos);
	string eta			= "";
	string details		= "";
	
	char[] cbs = brand.ToCharArray();
	string cat = "";
	string ss_cat = "";
	if(cbs.Length > 0)
		brand = IMCat(cbs, ref cat);
	try
	{
		double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	}
	catch(Exception e) 
	{
//		DEBUG("Error, price incorrect : " + supplier_price + " , code=" + (next_code-1),  " , reset to 9999 (AddToTempTable())");
		
		//most likely it has two dots, try to correct it
		char[] cbp = supplier_price.ToCharArray();
		supplier_price = "";
		bool bAlreadyHadPoint = false;
		for(int i=0; i<cbp.Length; i++)
		{
			if(cbp[i] == '.')
			{
				if(bAlreadyHadPoint)
					continue; //already got a dot, skip this 2nd one
				else
					bAlreadyHadPoint = true; //set flag, go get the dot
			}
			supplier_price += cbp[i];
		}
	}

	bRet = AddToTempRawTable(tableName, supplier_code, brand, supplier_name, supplier_price, "", stock, eta, cat, s_cat, ss_cat, details);
	return bRet;
}

string IMCat(char[] cb, ref string cat)
{
	string brand = "";
	bool bCatStart = false;
	int i = 0;
	for(;i<cb.Length;i++)
	{
		if(bCatStart)
			cat += cb[i];
		else
		{
			if(cb[i] == '-')
			{
				bCatStart = true;
				continue;
			}
			brand += cb[i];
		}
	}
	Trim(ref brand);
	Trim(ref cat);
	return brand;
}

/**** old tp process csv code *****/

bool TPProcessCVSFile(string fileName)
{
	Boolean bRet = true;

	int index = 0;
	int len = 0;
	string sLine = "";

	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string tableName = "temp_tp";
	Response.Write("Processing tp file <font color=green> ...");
	if(!CreateTempRawTable(tableName))
		return false;

	Random rnd = new Random();
	m_pMonitor = rnd.Next(0, m_sMonitor.Length);
	
	CompareInfo ci = CompareInfo.GetCompareInfo(1);
	int dash = 0;//sub catalog seperator
	int products = 0;
	string cat = "";
	string s_cat = "";
//	string ss_cat = "";
	int i = 0;
	sLine = r.ReadLine();
	while(sLine != "")
	{
		if(sLine == null)
			break;
		sLine = r.ReadLine(); //skip header
	}

	string sCatLine = ""; //this is the one we will process
	string sItemLine = "";
	while(sLine != null)
	{
		if(sLine == "")
		{
			sLine = r.ReadLine();
			continue;
		}
		
		sCatLine = "";
		sItemLine = "";

		if(sLine.Length > 7 && sLine.Substring(sLine.Length - 6, 6) == ",,,,,,")
		{
			if(sLine.Length > 3)
			{
				dash = ci.IndexOf(sLine, " - ", CompareOptions.IgnoreCase);
				if(dash >= 0)
				{
					sCatLine = sLine; //backup
					sLine = r.ReadLine(); //should be blank!
					if(sLine == null)
						break;
					if(sLine != "")
					{
						Response.Write("<h3>Warning, wrong format!</h3>");
						return false;
					}
					sLine = r.ReadLine();
					if(sLine == null)
						break;
	
					if(sLine.Length > 7 && sLine.Substring(sLine.Length - 6, 6) == ",,,,,,")
					{
						//skip previous line, it's techlink's useless brands catalog
						sCatLine = sLine;
					}
					else
					{
						sItemLine = sLine;
					}

				}
			}
		}
		else
		{
			sItemLine = sLine;
		}
		
		if(sCatLine != "")
		{
			dash = ci.IndexOf(sCatLine, " - ", CompareOptions.IgnoreCase);
			if(dash >= 0)
			{
				cat = sCatLine.Substring(0, dash);
				s_cat = sCatLine.Substring(dash + 3, sCatLine.Length - dash - 10);
//				Response.Write(" " + cat + " " + s_cat + "<br>");
//				Response.Flush();
			}
		}
		if(sItemLine != "")
		{
			if(!TPProcessCVSLine(tableName, sItemLine, cat, s_cat))
			{
				bRet = false;
				break;
			}
			products++;
		}

		sLine = r.ReadLine();
		MonitorProcess(300);
	}

	Response.Write("... </font>done. <font color=red>");
	Response.Write(products);
	Response.Write(" </font>items added to temp table<br>\r\n");
	r.Close();

	return SortProcessTempTable("TP", tableName);
}
Boolean TPProcessCVSLine(string tableName, string sLine, string cat, string s_cat)
{
	Boolean bRet = true;

	char[] cb = sLine.ToCharArray();

	int pos = 0;
	string supplier_code = CSVNextColumn(cb, ref pos);
	if(supplier_code == "")
		return true;	//skip empty lines

//	CSVNextColumn(cb, ref pos);

	string supplier_name	= CSVNextColumn(cb, ref pos);
	string supplier_price = CSVNextColumn(cb, ref pos);
	string stock		= CSVNextColumn(cb, ref pos);
	string eta			= CSVNextColumn(cb, ref pos);
	string brand		= CSVNextColumn(cb, ref pos);

	string ss_cat		= "";
	string details		= "";

	bRet = AddToTempRawTable(tableName, supplier_code, brand, supplier_name, supplier_price, "", stock, eta, cat, s_cat, ss_cat, details);
	return bRet;
}
 
/********** end here *************************/
/*** stop new one 
bool TPProcessCVSFile(string fileName)
{
	Boolean bRet = true;

	int index = 0;
	int len = 0;
	string sLine = "";

	FileStream fs;
	try
	{
		fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	}
	catch(Exception e)
	{
		MsgDie(e.ToString());
		return false;
	}
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string tableName = "temp_tp";
	Response.Write("Processing tp file <font color=green> ...");
	if(!CreateTempRawTable(tableName))
		return false;

	Random rnd = new Random();
	m_pMonitor = rnd.Next(0, m_sMonitor.Length);
	
	CompareInfo ci = CompareInfo.GetCompareInfo(1);
	int dash = 0;//sub catalog seperator
	int products = 0;
	string cat = "";
	string s_cat = "";
	string ss_cat = "";
	int i = 0;
	sLine = r.ReadLine();
	while(sLine.Length <= 7 || (sLine.Length > 7 && sLine.Substring(sLine.Length - 6, 6) != ",,,,,,")) //goto 1st special line
	{
		if(sLine == null)
			break;
		sLine = r.ReadLine(); //skip header
	}

	string sCatLine = ""; //cat - s_cat line
	string ssCatLine = ""; //brand - ss_cat line
	string sItemLine = "";
	while(sLine != null)
	{
		if(sLine == "")
		{
			sLine = r.ReadLine();
			continue;
		}
		
		sCatLine = "";
		ssCatLine = "";
		sItemLine = "";

		if(sLine.Length > 7 && sLine.Substring(sLine.Length - 6, 6) == ",,,,,,") //special line
		{			DEBUG("sLine1 = ", sLine);
			if(sLine.IndexOf("Tech Pacific") >= 0)
				break;
			if(sLine.IndexOf("For account") >= 0)
				break;
			DEBUG("sLine1 = ", sLine);
			if(sLine.Length > 3)
			{
				DEBUG("sLine2 = ", sLine);
				dash = ci.IndexOf(sLine, " - ", CompareOptions.IgnoreCase); 
				if(dash >= 0)//1st special line, if no 2nd special line then this is brand - ss_cat
				{
					ssCatLine = sLine; //suppose no 2nd special line
					sLine = r.ReadLine(); //should be blank!

				DEBUG("dssh = ", dash);
				DEBUG("sscatlin = ", ssCatLine);
					if(sLine == null)
						break;
					if(sLine != "")
					{
						Response.Write("<h3>Warning, wrong format!</h3>");
						return false;
					}
					sLine = r.ReadLine();
					if(sLine == null)
						break;
	
					//2nd special line found, so the 1st special line should be cat - s_cat
					if(sLine.Length > 7 && sLine.Substring(sLine.Length - 6, 6) == ",,,,,,") 
					{
						sCatLine = ssCatLine;
						ssCatLine = sLine;
					}
					else
					{
						sItemLine = sLine;
					}

				}
			}
		}
		else
		{
			sItemLine = sLine;
		}
		
		if(sCatLine != "") //refresh cat, s_cat
		{
			dash = ci.IndexOf(sCatLine, " - ", CompareOptions.IgnoreCase);
			if(dash >= 0)
			{
				cat = sCatLine.Substring(0, dash);
				s_cat = sCatLine.Substring(dash + 3, sCatLine.Length - dash - 9);
			}
		}
		if(ssCatLine != "") //refresh ss_cat
		{
			dash = ci.IndexOf(ssCatLine, " - ", CompareOptions.IgnoreCase);
			if(dash >= 0)
			{
				string sbrand = ssCatLine.Substring(0, dash); //this wont be used, as there is another brand column exists
				ss_cat = ssCatLine.Substring(dash + 3, ssCatLine.Length - dash - 9);
			}
		}

		if(sItemLine != "")
		{
			if(!TPProcessCVSLine(tableName, sItemLine, cat, s_cat, ss_cat))
			{
				bRet = false;
				break;
			}
			products++;
		}

		sLine = r.ReadLine();
		MonitorProcess(300);
	}

	Response.Write("... </font>done. <font color=red>");
	Response.Write(products);
	Response.Write(" </font>items added to temp table<br>\r\n");
	r.Close();

	return SortProcessTempTable("TP", tableName);
}

Boolean TPProcessCVSLine(string tableName, string sLine, string cat, string s_cat, string ss_cat)
{
	Boolean bRet = true;

	char[] cb = sLine.ToCharArray();

	int pos = 0;
	string supplier_code = CSVNextColumn(cb, ref pos);
	if(supplier_code == "")
		return true;	//skip empty lines

//	CSVNextColumn(cb, ref pos);

	string supplier_name	= CSVNextColumn(cb, ref pos);
	string supplier_price = CSVNextColumn(cb, ref pos);
	string stock		= CSVNextColumn(cb, ref pos);
	string eta			= CSVNextColumn(cb, ref pos);
	string brand		= CSVNextColumn(cb, ref pos);

//	string ss_cat		= ss_cat;
	string details		= "";

	bRet = AddToTempRawTable(tableName, supplier_code, brand, supplier_name, supplier_price, "", stock, eta, cat, s_cat, ss_cat, details);
	return bRet;
}

***/

Boolean ProcessDataRow(string supplier, string supplier_code, string supplier_name, string brand, string cat, 
	string s_cat, string ss_cat, string stock, string eta, string supplier_price, string rrp, string details)
{
	Boolean bRet = true;

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

	string name = "";
	Boolean skip = false;
	Boolean bIsNew = false;
	if(!GetTopsysCode(supplier, supplier_code, supplier_name, supplier_price, rrp, brand, cat, s_cat, ss_cat, ref skip, ref bIsNew))
	{
		Response.Write("<font size=+2 color=red><b>Error Getting New Code</b></font><br>\r\n");
		Response.Flush();
		return false; //error getting new code
	}

//temp, add supplier_price to code_relations
//if(!UpdateSupplierPrice(supplier, supplier_code, supplier_price))
//	return false;
	
	double dsupplier_price = 9999999;
	try
	{
		dsupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
		if(dsupplier_price <= 0)
			return true;
	}
	catch(Exception e) 
	{
		return true;
	}

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
//		return UpdateSkipTable(supplier, supplier_code, supplier_price, rrp, stock, eta, details);
//Thread.Sleep(1);
	}

	//add to tmp table
//	AddToTempTable(supplier, supplier_code, supplier_price, rrp, stock, eta, details, bIsNew);

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

	total_items++;
	return bRet;
}

Boolean GetTopsysCode(string supplier, string supplier_code, string supplier_name, string supplier_price, 
	string rrp, string brand, string cat,  string s_cat, string ss_cat, ref Boolean skip, ref Boolean bIsNew)
{
	Boolean found = false;
	DataRow dr;
	if(dracr.Length > code_index)
	{
		dr = dracr[code_index];
		string csupplier_code = dr["supplier_code"].ToString();
		Trim(ref csupplier_code);
		Trim(ref supplier_code);
DEBUG("sc = "+ csupplier_code, "c = "+ supplier_code);
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
		code_index++;
		old_items++;
	}
	else
	{
		if(!AddNewCodeToTempTable(supplier, supplier_code, supplier_name, supplier_price, rrp, brand, cat, s_cat, ss_cat))
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
/*		if(g_bRetailVersion)
		{
			m_scDump.Append(" IF NOT EXISTS (SELECT q.qty FROM stock_qty q JOIN code_relations c ON q.code = c.code ");
			m_scDump.Append(" WHERE c.supplier = '");
			m_scDump.Append(target_supplier.ToUpper());
			m_scDump.Append("' AND c.supplier_code = '");
			m_scDump.Append(supplier_code);
			m_scDump.Append("') ");
			m_scDump.Append(" BEGIN ");
		}
		m_scDump.Append(" DELETE FROM product WHERE supplier='");
		m_scDump.Append(target_supplier);
		m_scDump.Append("' AND supplier_code='");
		m_scDump.Append(supplier_code);
		m_scDump.Append("' DELETE FROM product_skip WHERE id='");
		m_scDump.Append(target_supplier);
		m_scDump.Append(supplier_code);
		m_scDump.Append("' ");
		if(g_bRetailVersion)
			m_scDump.Append(" END ");
*/
		//store code to dump
		if(m_sDumpP != "")
			m_sDumpP += ",";
		m_sDumpP += "'" + dracr[code_index]["supplier_code"].ToString() + "'";
		if(m_sDumpK != "")
			m_sDumpK += ",";
		m_sDumpK += "'" + target_supplier + dracr[code_index]["supplier_code"].ToString() + "'";

//		sc += DumpCurrentCode(dracr[code_index]["supplier_code"].ToString());
//		if(!DumpCurrentCode(dracr[code_index]["supplier_code"].ToString()))//delete discontinued products from our database as well
//		{
//			Response.Write("<font color=red>Error dumpping deleted code. program teminated.</font><br>\r\n");
//			Response.Flush();
//			return false;
//		}
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

bool AddNewCodeToTempTable(string supplier, string supplier_code, string supplier_name,
		string supplier_price, string rrp, string brand, string cat, string s_cat, string ss_cat)
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
//		DEBUG("Error, price incorrect : " + supplier_price + " , code=" + next_code,  " , reset to 99999 and skip AddNewCodeToTempTable())");
//		Response.Write("p");
		return true;
	}

	new_items++;
	double drate = GetDefaultPriceRate(dSupplier_price);
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

//DEBUG("supplier_price=", supplier_price);
//	if(supplier_price == null || supplier_price == "" || !TSIsDigit(supplier_price))
//	{
//		DEBUG("<br>Error, price incorrect : ", supplier_price + " , reset to 0");
//		supplier_price = "0";
//	}

	//insert code_relations_new
	string sc = "INSERT INTO code_relations_new (id, supplier, supplier_code, supplier_price, code, name, brand, cat, s_cat, ss_cat, skip, hot, rate)";
	sc += "VALUES('";
	sc += id;
	sc += "', '";
	sc += supplier;
	sc += "', '";
	sc += supplier_code;
	sc += "', ";
	sc += dSupplier_price;
	sc += ", ";
	sc += next_code++;;
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
	sc += "', 0, 0, ";
	sc += drate;
//	sc += GetDefaultPriceRate(double.Parse(supplier_price, NumberStyles.Currency, null));
	sc += ")";

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

void AddToTempTable(string supplier, string supplier_code, string supplier_price, 
	string rrp, string stock, string eta, string details, Boolean bIsNew)
{
	string sTableName = "product_raw";
	if(bIsNew)
		sTableName = "product_new";

	DataRow dr;

	if(supplier_price == "" || supplier_price == "0" || supplier_price == "0.00")
	{
//		DEBUG("<br>Error, zero or empty supplier price found : " + supplier_price + " , M_PN=" + supplier_code,  " , reset to 99999 and skip (AddToTempTable())");
//		supplier_price = "0";
		return; //drop it
	}
	
	double dSupplier_price = 99999;
	try
	{
		dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	}
	catch(Exception e) 
	{
		DEBUG("<br>Error, price incorrect : " + supplier_price + " , M_PN=" + supplier_code,  " , reset to 99999 and skip (AddToTempTable())");
//		Response.Write("p");
		return;
	}

	string id = supplier;
	id += supplier_code;

//	double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	
	dr = dst.Tables[sTableName].NewRow();

	if(!IsInteger(stock))
	{
//		DEBUG("<br>WARNING, stock data incorrect, reset to 0. In the data file, id=" + id + " stock=", stock);
		Response.Write("s");
		stock = "0";
//		return; 
	}

	dr["id"] = id;
	dr["stock"]	= int.Parse(stock);
	dr["eta"] = eta;
	dr["supplier_price"] = dSupplier_price;
	dr["details"] = details;
	
	if(bIsNew)
	{
		dr["price"] = CalculateRetailPrice(dSupplier_price, GetDefaultPriceRate(dSupplier_price));
//DEBUG("price=", dr["price"].ToString);
		if(dr["price"].ToString() == "99999")
		{
			DEBUG("<br>Error calculating retail price incorrect : " + dSupplier_price.ToString("c") + " , M_PN=" + supplier_code,  " , reset to 99999 and skip (AddToTempTable())");
			return;
		}
		if(rrp != "")
		{
			try
			{
				double drrp = double.Parse(rrp, NumberStyles.Currency, null);
				if(drrp != 0)
					dr["price"] = drrp;
//DEBUG("rrp=", rrp);
			}
			catch(Exception e)
			{
				DEBUG("error rrp:", rrp);
//				dr["price"]	= rrp;
			}
		}
	}

	dst.Tables[sTableName].Rows.Add(dr);
}

Boolean SaveTempTable(string tableName)
{
	Response.Write("Saving temperary table ");
	Response.Write(tableName);
	Response.Write("..........");
	Response.Flush();

	string sc = "SELECT * FROM ";
	sc += tableName;
	try
	{
		SqlDataAdapter custDA = new SqlDataAdapter(sc, myConnection);
		SqlCommandBuilder custCB = new SqlCommandBuilder(custDA);
		
		myConnection.Open();
		custDA.Update(dst, tableName);
		myConnection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("done<br>\r\n");
	Response.Flush();
	return true;
}

Boolean AddNewCodeToDatabase()
{
	Response.Write("Saving new code to temp table, this may take a few minutes please wait......");
	Response.Flush();

	string sc = "SELECT * FROM code_relations_new ";
	try
	{
		SqlDataAdapter custDA = new SqlDataAdapter(sc, myConnection);
		SqlCommandBuilder custCB = new SqlCommandBuilder(custDA);
		
		myConnection.Open();
		custDA.Update(dst, "new_code");
		myConnection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("done<br>\r\n");
	Response.Flush();
	return true;
}

Boolean AddToTempRawTable(string tableName, string supplier_code, string brand, string supplier_name, string supplier_price, 
	string RRP, string stock, string eta, string cat, string s_cat, string ss_cat, string details)
{
	details = DropHTMLCode(details);
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

	try
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
//DEBUG("drate="+drate.ToString(), " cost="+dcost.ToString() + " , rrp="+drrp.ToString());
	}
	catch(Exception e)
	{
	}
	
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

	dst.Tables[tableName].Rows.Add(myDataRow);
	return true;
}

string DropHTMLCode(string details)
{
	char[] buf = details.ToCharArray();
	char[] br = new char[details.Length];
	
	for(int i=0; i<details.Length; i++)
	{
		if(buf[i] == '<')
			br[i] = '[';
		else if(buf[i] == '>')
			br[i] = ']';
		else
			br[i] = buf[i];
	}
	return (new string(br));
}

Boolean CreateTempRawTable(string tableName)
{
	// Create a new DataTable.
	System.Data.DataTable myDataTable = new DataTable(tableName);

	// Declare variables for DataColumn and DataRow objects.
	DataColumn myDataColumn;

	string[] cn = new string[12];

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

	// Create new DataColumn, set DataType, ColumnName and add to DataTable.    
	for(int i=0; i<=11; i++)
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

	int rows = dra.Length;
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
	int items = 0;	
	int items_got = total_items;
	
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
		if(dra[i]["supplier_code"].ToString() == "")
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
//		bRet = ProcessDataRow(supplier, supplier_code, supplier_name, brand, cat, s_cat, ss_cat, stock, eta, supplier_price, price, details);
		bRet = ProcessDataRow(supplier, dra[i]["supplier_code"].ToString(), 
			dra[i]["supplier_name"].ToString(), dra[i]["brand"].ToString(), 
			dra[i]["cat"].ToString(), dra[i]["s_cat"].ToString(), dra[i]["ss_cat"].ToString(), 
			dra[i]["stock"].ToString(), dra[i]["eta"].ToString(), dra[i]["supplier_price"].ToString(), 
			dra[i]["supplier_price"].ToString(), dra[i]["details"].ToString());
		if(!bRet)
			break;
		MonitorProcess(300);
		items++;
	}
DEBUG("dump n=", m_sDumpP);
	if(m_sDumpP != "")
	{
		//prepare dumpping flags
		string sc = " UPDATE product SET real_stock = 1 WHERE code IN (SELECT code FROM stock_qty) ";
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

		StringBuilder sb = new StringBuilder();
		sb.Append(" DELETE FROM product WHERE supplier='");
		sb.Append(target_supplier);
		sb.Append("' AND real_stock=0 AND supplier_code IN (");
		sb.Append(m_sDumpP);
		sb.Append(") DELETE FROM product_skip WHERE real_stock=0 AND id IN (");
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

Boolean	PrepareTablesForAnalyze()
{
	Response.Write("Cacheing product table......");
	Response.Flush();
	if(!CacheProductTableForAnalyze())
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

Boolean CollectAllForSaleItems()
{
	Response.Write("Collecting all for sale items.......");
	Response.Flush();
	string sc = "SELECT c.code, c.id, c.brand, c.name, c.cat, c.s_cat, c.ss_cat, c.supplier, c.supplier_code, ";
	sc += "p.stock, p.eta, p.supplier_price, c.rate, c.hot FROM code_relations c JOIN product_raw p ON c.id=p.id ";
	sc += "WHERE c.skip=0 ";
	sc += "ORDER BY c.code, p.supplier_price, p.stock DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "product_all");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("done<br>\r\n");
	Response.Flush();
	return true;
}

Boolean AnalyzeData()
{
	if(!GetCheapestItem())
		return false;
	if(!CheckUpdates())
		return false;
	return true;
}

Boolean GetCheapestItem()
{
	int rows = dst.Tables["product_all"].Rows.Count;
	DataRow dr;
	string code = "";
	string code_old = "";
	string stock = "";
	int nStock = 0;
	int nStock_old = 0;
	int i = 0;
	Response.Write("Calculating price<font color=green> ...");
	Response.Flush();
	for(i=0; i<rows; i++)
	{
		MonitorProcess(300);
//Thread.Sleep(1);
		dr = dst.Tables["product_all"].Rows[i];
		code = dr["code"].ToString();
		stock = dr["stock"].ToString();
		if(stock == "")
			stock = "0";
		nStock = int.Parse(stock);
		if(code == code_old)	//next product start
		{
			if(nStock_old <= 0) //only interest if the cheapest supplier has no stock, otherwise continue
			{
				if(nStock > 0)
				{
					UpdateCurrent(dr);
					nStock_old = nStock;
				}
			}
		}
		else
		{
			if(!AddNextProduct(dr))
				return false;
			code_old = code;
			nStock_old = nStock;
		}
	}
	Response.Write(" ... </font>done<br>");
	Response.Flush();
	return true;
}

Boolean AddNextProduct(DataRow sdr)
{
//	DEBUG("Add one row, id=", sdr["id"].ToString());	
	
	string supplier_price = sdr["supplier_price"].ToString();
	string stock = sdr["stock"].ToString();
	double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	int nStock = int.Parse(stock);	

	DataRow dr = dst.Tables["product_temp"].NewRow();

	dr["code"]			= sdr["code"].ToString();
	dr["name"]			= sdr["name"].ToString();
	dr["brand"]			= sdr["brand"].ToString();
	dr["cat"]			= sdr["cat"].ToString();
	dr["s_cat"]			= sdr["s_cat"].ToString();
	dr["ss_cat"]		= sdr["ss_cat"].ToString();
	dr["price"]			= 0;
	dr["stock"]			= nStock;
	dr["eta"]			= sdr["eta"].ToString();
	dr["supplier"]		= sdr["supplier"].ToString();
	dr["supplier_code"]	= sdr["supplier_code"].ToString();
	dr["supplier_price"]	= dSupplier_price;
	dr["hot"]			= Boolean.Parse(sdr["hot"].ToString());
	dr["price_dropped"]	= 0;
	dr["price_age"]		= DateTime.Now;

	dst.Tables["product_temp"].Rows.Add(dr);
	return true;
}

Boolean UpdateCurrent(DataRow sdr)
{
//	DEBUG("updated one row, newid=", sdr["id"].ToString());
	dst.Tables["product_temp"].Rows.RemoveAt(dst.Tables["product_temp"].Rows.Count-1); //remove the last row (current)
	return AddNextProduct(sdr);
}

Boolean CheckUpdates()
{
	m_sTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm");
	//detect changes
	DataRow drp = null;	//search return from product table
	DataRow dr = null;	//rows in temp table
	
	string code = "";
	string stock = "";
	string eta = "";
	string supplier_price = "";

	string stock_current = "";
	string eta_current = "";
	string supplier_price_current = "";

	Response.Write("Check updates<font color=green> ...");
	Response.Flush();

	int rows = dst.Tables["product_temp"].Rows.Count;
	int i = 0;
//DEBUG("rows=", rows);
	for(i=0; i<rows; i++)
	{
		MonitorProcess(300);
		dr = dst.Tables["product_temp"].Rows[i];
		code = dr["code"].ToString();

		if(AUGetProduct(code, ref drp))
		{
			stock = dr["stock"].ToString();
			eta = dr["eta"].ToString();
			supplier_price = dr["supplier_price"].ToString();

			stock_current = drp["stock"].ToString();
			eta_current = drp["eta"].ToString();
			supplier_price_current = drp["supplier_price"].ToString();
				
			if(stock == stock_current && eta == eta_current && supplier_price == supplier_price_current)
			{
//DEBUG("continue, code=", code);
				continue;	//no changes
			}	
		}
		
		m_nUpdatedItems++;
		//either new or sth changed, add it to update table
		if(!WriteUpdate(dr, drp))
		{
Thread.Sleep(1);
			return false;
		}
	}
	Response.Write(" ... </font>done<br>");
	Response.Flush();
	return true;
}

Boolean AUGetProduct(string code, ref DataRow dr)
{
//DEBUG("GetProduct, code=", code);
	Boolean bRet = false;

	string sc = "code='";
	sc += code;
	sc += "'";

	DataRow[] dra = dst.Tables["product"].Select(sc);

	if(dra.Length > 0)
	{
		dr = dra[0];	
		bRet = true;
	}
	else
	{
		dr = null;
	}
	return bRet;
}

double GetProductPriceRate(int code)
{
	double dRate = m_dPrice_rate_settings;

	string sc = "code='";
	sc += code;
	sc += "'";

	DataRow[] dra = dst.Tables["product_all"].Select(sc);
	if(dra.Length > 0)
	{
		string rate = dra[0]["rate"].ToString();	
		dRate = double.Parse(rate);
	}
//DEBUG("GetProductPriceRate, rate=", dRate.ToString());
	return dRate;
}

Boolean WriteUpdate(DataRow dr, DataRow drp)
{
	double dPrice_rate = m_dPrice_rate_settings;
	if(drp != null)
		dPrice_rate = GetProductPriceRate(int.Parse(drp["code"].ToString()));

	string supplier_price = dr["supplier_price"].ToString();
	double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	double dPrice_current = CalculateRetailPrice(dSupplier_price, dPrice_rate);
//	dPrice_current = Math.Round(dPrice_current, 2);

	string supplier_price_current = "";
	string price_age = "";
	DateTime dNow = DateTime.Now;
	DateTime date_price_age;
	int nPrice_dropped = 0;
	
	bool bPriceChanged = false;
	string sc = "";

	if(drp != null) // item exists in main product table, do update
	{
		supplier_price_current = drp["supplier_price"].ToString();
		double dSupplier_price_current = double.Parse(supplier_price_current, NumberStyles.Currency, null);
		price_age = drp["price_age"].ToString();
//DEBUG("code=" + drp["code"].ToString(), " price_dropped=" + drp["price_dropped"].ToString());
		nPrice_dropped = int.Parse(drp["price_dropped"].ToString());
		if(dSupplier_price == dSupplier_price_current)
		{
			date_price_age = DateTime.Parse(price_age);
			TimeSpan ts = new TimeSpan(dNow.Ticks - date_price_age.Ticks);
			if(ts.Days > m_nPrice_age_settings)
			{
				nPrice_dropped = 0; //reset if price_age expired
			}
		}
		else
		{
			bPriceChanged = true;
			if(dSupplier_price > dSupplier_price_current)
			{
				nPrice_dropped = -1; //price raised
			}
			else
			{
				nPrice_dropped = 1;
			}
		}
	}

	if(drp != null)
	{
		if(!WriteUpdateLog(drp))
			return false;

		sc = "UPDATE product set ";
		sc += " stock=";
		sc += dr["stock"].ToString();
		sc += ", eta='";
		sc += dr["eta"].ToString();
		sc += "', price_dropped=";
		sc += nPrice_dropped;
		if(bPriceChanged)
		{
			sc += ", price=";
			sc += dPrice_current;
			sc += ", supplier_price=";
			sc += dSupplier_price;
			sc += ", price_age=";
			sc += "GETDATE()";
		}
		sc += " WHERE code=";
		sc += dr["code"].ToString();
	}
	else
	{
		sc = "INSERT INTO product (code, name, brand, cat, s_cat, ss_cat, ";
		sc += "price, stock, eta, supplier, supplier_code, supplier_price, hot, ";
		sc += "price_dropped, price_age) Values('";
		sc += dr["code"].ToString();
		sc += "', '";
		sc += dr["name"].ToString();
		sc += "', '";
		sc += dr["brand"].ToString();
		sc += "', '";
		sc += dr["cat"].ToString();
		sc += "', '";
		sc += dr["s_cat"].ToString();
		sc += "', '";
		sc += dr["ss_cat"].ToString();
		sc += "', ";
		sc += dPrice_current;
		sc += ", '";
		sc += dr["stock"].ToString();
		sc += "', '";
		sc += dr["eta"].ToString();
		sc += "', '";
		sc += dr["supplier"].ToString();
		sc += "', '";
		sc += dr["supplier_code"].ToString();
		sc += "', ";
		sc += dSupplier_price;
		sc += ", ";
		if(Boolean.Parse(dr["hot"].ToString()))
			sc += "1";
		else
			sc += "0";
		sc += ", ";
		sc += nPrice_dropped;
		sc += ", ";
		sc += "GETDATE()";
		sc += ")";

	}	

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

	//write update history
	if(!WriteUpdateLog(dr))
		return false;

	if(bPriceChanged)
	{
		if(!WritePriceHistory(dr["code"].ToString(), dPrice_current))
			return false;
		if(!AUUpdateSupplierPrice(dr["supplier"].ToString(), dr["supplier_code"].ToString(), supplier_price))
			return false;
	}
	return true;
}

bool AUUpdateSupplierPrice(string supplier, string supplier_code, string supplier_price)
{
	double dSupplier_price = double.Parse(supplier_price, NumberStyles.Currency, null);
	string sc = "UPDATE code_relations SET supplier_price=" + dSupplier_price;
	sc += ", foreign_supplier_price = " + dSupplier_price;
	sc += ", manual_cost_frd = " + dSupplier_price;
	sc += ", manual_cost_nzd = " + dSupplier_price;
	sc += " WHERE supplier='" + supplier + "' AND supplier_code='" + supplier_code + "'";
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

bool WriteUpdateLog(DataRow dr)
{
	string stock = "0";
	if(dr["stock"] != null && dr["stock"].ToString() != "")
		stock = dr["stock"].ToString();

	string sc = "INSERT INTO product_update_log (code, id, stock, eta, supplier_price, price_age, time_stamp) VALUES(";
	sc += dr["code"].ToString();
	sc += ", '";
	sc += dr["supplier"].ToString() + dr["supplier_code"].ToString();
	sc += "', ";
	sc += stock;
	sc += ", '";
	sc += dr["eta"].ToString();
	sc += "', ";
	sc += double.Parse(dr["supplier_price"].ToString(), NumberStyles.Currency, null);
	sc += ", '";
	sc += ((DateTime)dr["price_age"]).ToString("MM/dd/yyyy HH:mm");
	sc += "', '";
	sc += m_sTimeStamp;
	sc += "')";

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

//copy current main product table to memory, prepare for analyze
Boolean CacheProductTableForAnalyze()
{
	string sc = "SELECT code, supplier, supplier_code, stock, supplier_price, eta, price_age, price_dropped FROM product";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
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
			string supplier = Supplier.Value;
			if(new_supplier.Value != "")
				supplier = new_supplier.Value;
			string vpath = GetRootPath() + "/admin/data/" + supplier + "/";
			string strPath = Server.MapPath(vpath);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += strFileName;
			
//DEBUG("strPath=", strPath);
			//check old files, delete .gif or jpg (another type)if exists
//			string oldFile = strPath;
//			if(File.Exists(oldFile))
//				File.Delete(oldFile);

			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=autoupdate.aspx\">");
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
<td><b> Supplier : </b><select id=Supplier runat=server>
<option value=BB>BB</option>
<option value=CD>CD</option>
<option value=IM>IM</option>
<option value=OCNZ>OCNZ</option>
<option value=RN>RN</option>
<option value=TP>TP</option>
</select>
<input type=text id=new_supplier size=3 maxlength=4 runat=server>
<td> <asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</form>

<asp:Label id=LFooter runat=server />
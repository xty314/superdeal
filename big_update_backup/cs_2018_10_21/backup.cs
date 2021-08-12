<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>

<!-- #include file="kit_fun.cs" -->
<script runat=server>

DataSet ds = new DataSet();

string m_path = "";
string m_fileName = "";
string m_fileNameNE = ""; //without extension
string m_type = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;

	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];


	string strPath = Server.MapPath("backup/");
	string lname = Session["name"].ToString();
	int bpos = lname.IndexOf(" ");
	if(bpos > 0)
		lname = lname.Substring(0, bpos);
	lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	m_path = strPath + lname;

	if(m_type == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Online Backup</h3></center>");

		if(Directory.Exists(m_path))
		{
			Response.Write("<table align=center cellspacing=7 cellpadding=3 border=0 bordercolor#EEEEEE");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
			Response.Write("<tr><td colspan=4><b>Backup files ready to download</b></td></tr>");

			Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
			Response.Write("<td><b>FILE</b></td>");
			Response.Write("<td><b>SIZE</b></td>");
			Response.Write("<td><b>FILE DATE</b></td>");
			Response.Write("<td><b>DOWNLOAD</b></td>");
			Response.Write("</tr>");

			DirectoryInfo di = new DirectoryInfo(m_path);
			foreach (FileInfo f in di.GetFiles("*.*")) 
			{
				string s = f.FullName;
				string file = s.Substring(m_path.Length+1, s.Length-m_path.Length-1);
//				string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
				Response.Write("<tr><td><a href=backup/" + lname + "/" + file + ">" + file);
				Response.Write("</a></td>");
				Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
				Response.Write("<td>" + f.LastWriteTime.ToString("dd-MM-yyyy HH:mm") + "</td>");
				Response.Write("<td align=right><a href=backup/" + lname + "/" + file + " class=o>download");
				Response.Write("</a></td>");
				Response.Write("</tr>");
			}
			Response.Write("</table>");
		}
		return;
	}

	m_bShowProgress = true;
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h3>Online Backup</h3></center>");

	bool bRet = true;
	bRet = EmptyBackupFolder();
	bRet = ExportTables();
	
	if(bRet)
	{
		Response.Write("<br><h4>Zipping data files, please wait...");
		Response.Flush();
		bRet = ZipDir(m_path, m_sCompanyName + "_data_" + DateTime.Now.ToString("dd_MM_yy_HH_mm") + ".zip");
		Response.Write("done.</h4>\r\n");
	}

	if(bRet)
	{
		Response.Write("<br><h4>Backing up item images, please wait...");
		Response.Flush();
		bRet = ZipDir(Server.MapPath("../pi"), m_sCompanyName + "_img_" + DateTime.Now.ToString("dd_MM_yy_HH_mm") + ".zip");
		Response.Write("done.</h4><br><br>\r\n");
	}

	PrintAdminFooter();
	
	//clean up csv files
	if(Directory.Exists(m_path))
	{
		string[] files = Directory.GetFiles(m_path, "*.csv");
		for(int i=0; i<files.Length; i++)
		{
			if(File.Exists(files[i]))
				File.Delete(files[i]);
		}
	}

	if(bRet)
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=backup.aspx?t=done\">");
	}
}

bool EmptyBackupFolder()
{
	if(Directory.Exists(m_path))
		Directory.Delete(m_path, true);

	Directory.CreateDirectory(m_path);
	return true;
}

bool ExportTables()
{
	Response.Write("<br><h4>Exporting data, please wait...</h4>\r\n");
	Response.Flush();

	string[] t = new string[255];
	int i = 0;

	//all versions
	t[i++] = "auto_ftp";
	t[i++] = "bb_notify";
	t[i++] = "bb_post";
	t[i++] = "bb_topic";
	t[i++] = "branch";
	t[i++] = "brand_settings";
	t[i++] = "card";
	t[i++] = "card_access_class";
	t[i++] = "card_access_data";
	t[i++] = "card_access_menu";
	t[i++] = "cat_logo";
	t[i++] = "cat_seq";
	t[i++] = "code_relations";
	t[i++] = "color_name";
	t[i++] = "color_set";
	t[i++] = "dev_task";
	t[i++] = "dev_task_note";
	t[i++] = "enum";
	t[i++] = "kit";
	t[i++] = "kit_item";
	t[i++] = "menu_access_class";
	t[i++] = "menu_admin_access";
	t[i++] = "menu_admin_catalog";
	t[i++] = "menu_admin_id";
	t[i++] = "menu_admin_sub";
	t[i++] = "news";
	t[i++] = "notepad";
	t[i++] = "csv_format";
	t[i++] = "odbc_mapping";
	t[i++] = "order_item";
	t[i++] = "order_kit";
	t[i++] = "orders";
	t[i++] = "price_history";
	t[i++] = "product";
	t[i++] = "product_details";
	t[i++] = "product_skip";
	t[i++] = "q_cat";
	t[i++] = "q_flat";
	t[i++] = "q_mb";
	t[i++] = "q_mb_cpus";
	t[i++] = "q_ram";
	t[i++] = "q_sys";
	t[i++] = "q_video";
	t[i++] = "ship";
	t[i++] = "site_pages";
	t[i++] = "site_sub_pages";
	t[i++] = "specials";
	t[i++] = "specials_kit";
	t[i++] = "stock";
	t[i++] = "stock_adj_log";
	t[i++] = "stock_loss";
	t[i++] = "stock_qty";
	t[i++] = "web_log_analyze";

	if(!g_bOrderOnlyVersion)
	{
		t[i++] = "invoice";
		t[i++] = "invoice_freight";
		t[i++] = "sales";
		t[i++] = "sales_cost";
		t[i++] = "sales_kit";
		t[i++] = "account";
		t[i++] = "accrecon";
		t[i++] = "assets";
		t[i++] = "assets_item";
		t[i++] = "auto_expense";
		t[i++] = "direct_order";
		t[i++] = "expense";
		t[i++] = "expense_item";
		t[i++] = "payment";
		t[i++] = "po_number";
		t[i++] = "purchase";
		t[i++] = "purchase_item";
		t[i++] = "ra_statement";
		t[i++] = "repair";
		t[i++] = "repair_log";
		t[i++] = "return_sn";
		t[i++] = "rma";
		t[i++] = "sales_serial";
		t[i++] = "serial_trace";
		t[i++] = "settings";
		t[i++] = "support_log";
		t[i++] = "templates";
		t[i++] = "tran_deposit";
		t[i++] = "tran_deposit_id";
		t[i++] = "tran_detail";
		t[i++] = "tran_invoice";
		t[i++] = "trans";
		t[i++] = "used_catalog";
		t[i++] = "used_product";
	}

	int nTables = i;
	for(i=0; i<nTables; i++)
	{
		if(!WriteCSVFile(t[i]))
			return false;
	}

	Response.Write("<h4>Done.</h4>\r\n");
	Response.Flush();
	return true;
}

bool WriteCSVFile(string sTableName)
{
	Response.Write("Getting data from <b>" + sTableName + "</b> table ...");
	Response.Flush();

	//get data
	DataSet ds = new DataSet();
	string sc = " SELECT * FROM " + sTableName;
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
//		Response.Write("<br><center><h3>Export " + sTableName + " table failed.");
//		ShowExp(sc, e);
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

	//column data type
	for(i=0; i<cols; i++)
	{
		if(i > 0)
			sb.Append(",");
		sb.Append(dc[i].DataType.ToString().Replace("System.", ""));
	}
	sb.Append("\r\n");
	
	DataRow dr = null;

	for(i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		dr = ds.Tables[0].Rows[i];
		for(int j=0; j<cols; j++)
		{
			if(j > 0)
				sb.Append(",");
			string sValue = dr[j].ToString().Replace("\r\n", "@@eznz_return"); //encode line return in site_pages, kit...
			sValue = sValue.Replace("\r", "@@eznz_return"); //encode single return
			sValue = sValue.Replace("\n", "@@eznz_return"); //encode single return
			sValue = sValue.Replace("@@eznz_return", "\\r\\n");
			if(sTableName == "site_pages" || sTableName == "site_sub_pages")
				sValue = sValue.Replace("?/", "</"); //strange error
			sb.Append("\"" + EncodeDoubleQuote(sValue) + "\"");
		}
		sb.Append("\r\n");
		MonitorProcess(10);
	}

	string strPath = m_path + "\\" + sTableName + ".csv";

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

bool ZipOneFile(string FileToZip, string ZipedFile ,int CompressionLevel, int BlockSize)
{
	if(! System.IO.File.Exists(FileToZip)) 
	{
		throw new System.IO.FileNotFoundException("The specified file " + FileToZip + " could not be found. Zipping aborderd");
	}
	
	System.IO.FileStream StreamToZip = new System.IO.FileStream(FileToZip,System.IO.FileMode.Open , System.IO.FileAccess.Read);
	System.IO.FileStream ZipFile = System.IO.File.Create(ZipedFile);
	ZipOutputStream ZipStream = new ZipOutputStream(ZipFile);
	ZipEntry ZipEntry = new ZipEntry("ZippedFile");
	ZipStream.PutNextEntry(ZipEntry);
	ZipStream.SetLevel(CompressionLevel);
	byte[] buffer = new byte[BlockSize];
	System.Int32 size =StreamToZip.Read(buffer,0,buffer.Length);
	ZipStream.Write(buffer,0,size);
	try {
		while (size < StreamToZip.Length) {
			int sizeRead =StreamToZip.Read(buffer,0,buffer.Length);
			ZipStream.Write(buffer,0,sizeRead);
			size += sizeRead;
		}
	} catch(System.Exception ex){
		throw ex;
	}
	ZipStream.Finish();
	ZipStream.Close();
	StreamToZip.Close();
	return true;
}

bool ZipDir(string dirName, string zipFileName)
{
	string[] filenames = Directory.GetFiles(dirName);
	
	Crc32 crc = new Crc32();
	ZipOutputStream s = new ZipOutputStream(File.Create(m_path + "\\" + zipFileName));
	
	s.SetLevel(9); // 0 - store only to 9 - means best compression
	
	long maxLength = 2048000; //2mb file
	long len = 0;
	int files = 1;
	foreach (string file in filenames) 
	{
		if(s.Length >= maxLength)
		{
			s.Finish();
			s.Close();
			s = new ZipOutputStream(File.Create(m_path + "\\" + zipFileName.Replace(".zip", "") + "_" + files.ToString() + ".zip"));
			s.SetLevel(9); // 0 - store only to 9 - means best compression
			files++;
			len = 0;
		}
//		string file = Server.MapPath("./download/" + m_fileName);
		FileStream fs = File.OpenRead(file);
		byte[] buffer = new byte[fs.Length];
		fs.Read(buffer, 0, buffer.Length);
		ZipEntry entry = new ZipEntry(file);
		
		entry.DateTime = DateTime.Now;
		
		// set Size and the crc, because the information
		// about the size and crc should be stored in the header
		// if it is not set it is automatically written in the footer.
		// (in this case size == crc == -1 in the header)
		// Some ZIP programs have problems with zip files that don't store
		// the size and crc in the header.
		entry.Size = fs.Length;
		fs.Close();
		
		crc.Reset();
		crc.Update(buffer);
		
		entry.Crc  = crc.Value;
		
		s.PutNextEntry(entry);
		
		s.Write(buffer, 0, buffer.Length);
		len = buffer.Length; //total length
MonitorProcess(1);
	}
	
	s.Finish();
	s.Close();
	return true;
}

</script>

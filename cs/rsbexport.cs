<!-- #include file="page_index.cs" -->
<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>
<script runat=server>

string m_sorted = "DESC";
string m_path = "";
string m_export = "";
string m_invoice_num = "";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Request.QueryString["export"] != null)
		m_export = Request.QueryString["export"];

    if(p("invoice_num") != null && p("invoice_num") != "")
        m_invoice_num = p("invoice_num").Trim();

    string strPath = Server.MapPath("backup/");
	string lname = "RSB";
//	m_path = strPath + lname;
	m_path = strPath;
//export done 
	if(m_export == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Export Invoice Report Done</h3></center>");
//Response.Write(m_path);
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
//Response.Write(m_path +"<br>");
//Response.Write(s +"<br>");
//				string file = s.Substring(m_path.Length+1, s.Length-m_path.Length-1);
				string file = s.Substring(m_path.Length, s.Length-m_path.Length);
//Response.Write(file);
//				string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
				Response.Write("<tr><td><a href=backup/" + lname + "/" + file + ">" + file);
				Response.Write("</a></td>");
				Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
				Response.Write("<td>" + f.LastWriteTime.ToString("dd-MM-yyyy HH:mm") + "</td>");
//				Response.Write("<td align=right><a href=backup/" + lname + "/" + file + " class=o>download");
				Response.Write("<td align=right><a href=backup/" + file + " class=o>download");
				Response.Write("</a></td>");
				Response.Write("</tr>");
			}
			Response.Write("</table>");
			
			LFooter.Text = "<br><br><center><a href=rsbexport.aspx";
			LFooter.Text += " class=o>New Report </a>";
				
		}
		
		return;
	}

    if(Request.Form["cmd"] == null)
	{
	    PrintMainPage();
		LFooter.Text = m_sAdminFooter;
		return;
	}

    PrintAdminHeader();
	PrintAdminMenu();
	DoPaymentSummary();

	LFooter.Text = "<br><br><center><a href=rsbexport.aspx";
	LFooter.Text += " class=o>New Report </a>";
		
}

void PrintMainPage()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<form name=f action='rsbexport.aspx?r="+ DateTime.Now.ToOADate() +"");
	Response.Write("' method=post>");

	Response.Write("<br><center><h3>Export Invoice</h3>");
    Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
    Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	Response.Write("<b>Invoice# Input</b></td></tr>");
    Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=6>");
	
	
	Response.Write(" <b>Invoice# :&nbsp;</b>");
	Response.Write("<input type=text name=invoice_num size=25 >&nbsp;&nbsp;");
	Response.Write("<input type=submit name=cmd value='Export Invoice' " + Session["button_style"] + " onclick=\"return confirm('Export Invoice!!!');\">");

    Response.Write("</td></tr>");
    Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");

    Response.Write("</table>");
	Response.Write("</form>");
	LFooter.Text = m_sAdminFooter;
}
bool DoPaymentSummary()
{
	int n_rows = 0;
    ds.Clear();
	string sc = "";
    sc = " SET DATEFORMAT dmy SET ARITHABORT off SET ANSI_WARNINGS off";
    sc += " SELECT b.barcode,s.id, s.invoice_number, s.code, s.quantity, s.name, s.supplier_code, ISNULL(cr.level_price0*cr.level_rate1/100,0) AS original_price, Cast(round((1-(s.commit_price/cr.level_price0*cr.level_rate1/100))*100,0)as varchar(20)) + '%' AS discount_percent, s.commit_price, CONVERT(VARCHAR(10), i.commit_date, 105) AS invoice_date, i.cust_ponumber AS job_num ";
	sc += " FROM code_relations b JOIN ";
	sc += " sales s on b.code = s.code JOIN ";
    sc += " invoice i ON i.invoice_number = s.invoice_number ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = s.code ";
	sc += " WHERE 1=1 ";
    sc += " AND s.invoice_number = " + m_invoice_num;
	sc += " ORDER BY s.id DESC";
//DEBUG("sc=", sc);	
	try
	{  
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "report") <= 0)
        {
            Response.Write("<br><br><center><h3>Invoice# <font color=\"#FF0000\">" + m_invoice_num +"</font>&nbsp;Not Found!!!</h3></center>");
            LFooter.Text = "<br><br><center><a href=rsbexport.aspx";
			LFooter.Text += " class=o>New Report </a></center>";
            return false;
        }
    }
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
//return true;
    
    string job_num = "";
    if(ds.Tables["report"].Rows[0]["job_num"] != null && ds.Tables["report"].Rows[0]["job_num"].ToString() != "")
        job_num = ds.Tables["report"].Rows[0]["job_num"].ToString();
    job_num = job_num.Trim().Replace(" ", "_").Replace("\\", "_");
	
    if(Request.Form["cmd"] == "Export Invoice")
	{
		bool bRet = true;
		//bRet = EmptyBackupFolder();
        if(Directory.Exists(m_path))
		{
			string[] files = Directory.GetFiles(m_path, "*.zip");
			for(int i=0; i<files.Length; i++)
			{
				if(File.Exists(files[i]))
					File.Delete(files[i]);
			}
		}
        
//        bRet = WriteCSVFile(ds, "invoice_" + m_invoice_num + "_job_" + job_num);
        bRet = WriteCSVFile(ds, "invoice_" + m_invoice_num);
		
        if(bRet)
		{
			Response.Write("<br><h4>Zipping data files, please wait...");
			Response.Flush();
//			bRet = ZipDir(m_path, "invoice_" + m_invoice_num + "-job_" + job_num +".zip");
			bRet = ZipDir(m_path, "invoice_" + m_invoice_num + ".zip");
			Response.Write("done.</h4>\r\n");
		}
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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"]+ "?export=done\">");
			//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
			return true;
		}
		return true;
	}
	
    return true;
}

bool WriteCSVFile(DataSet ds, string csv_name)
{
	Response.Write("Getting data from <b> Invoice </b> table ...");
	Response.Flush();
	
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
			string sValue = dr[j].ToString().Replace("\r\n", "@@eznz_return"); //encode line return in site_pages, kit...
			sValue = sValue.Replace("\r", "@@eznz_return"); //encode single return
			sValue = sValue.Replace("\n", "@@eznz_return"); //encode single return
            sValue = sValue.Replace(",", " ");
			sValue = sValue.Replace("@@eznz_return", "\\r\\n");
            
			//if(sTableName == "site_pages" || sTableName == "site_sub_pages")
			//	sValue = sValue.Replace("?/", "</"); //strange error
			sb.Append("\"" + EncodeDoubleQuote(sValue) + "\"");
		}
		sb.Append("\r\n");
		MonitorProcess(10);
	}

	string strPath = m_path + "\\" + csv_name +".csv";

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
		string fn = System.IO.Path.GetFileName(file);
		ZipEntry entry = new ZipEntry(fn);
		
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
bool EmptyBackupFolder()
{
	if(Directory.Exists(m_path))
		Directory.Delete(m_path, true);

	Directory.CreateDirectory(m_path);
	return true;
}

</script>

<asp:Label id=LFooter runat=server/>
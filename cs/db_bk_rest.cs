<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@ Import Namespace="ASPNet_Drawing" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Checksums" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>

<!-- #include file="page_index.cs" -->
<!-- #include file="isdate.cs" -->
<script runat="server">
string m_selected = "1";  //selected value 

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_uri = "";
string m_SalesType = "Sales";
string m_invoice_number = "";
string m_branchID = "1";
string m_ReturnNewOrderID = "";
//----- m_querystring attribute ---//
//----- if m_querystring == ip ***** input faulty items
//----- if m_querystring == view ***** dealer, public site view ra list after apply ra
//----- if m_querystring == cr ******then create new ra number at admin site
//----- if m_querystring == all ****** show all created ra number on admin site
//---------------------------------------//
string m_path = "";
string m_fileName = "";
string m_destPath = "";
string lname = "";
string m_restoreFileName = "";
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;
	
	//getting server path name 
	string strPath = Server.MapPath("backup/");
//	lname = "db_"+Session["name"].ToString(); //"backup"; //
	lname = "db_backup";

	int bpos = lname.IndexOf(" ");
	if(bpos > 0)
		lname = lname.Substring(0, bpos);
	lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	m_path = strPath + lname;
//DEBUG("mspah=", m_path);

	//backup done 
	if(Request.QueryString["bk"] == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Backup Database Done</h3></center>");

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
			foreach (FileInfo f in di.GetFiles("*.zip")) 
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
	checkLogRstoreDB(true);
	if(Request.QueryString["restore"] == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Restore Database is Done</h3></center>");		
		return;
	}
	if(Request.Form["cmd"] == "Restore Database" )
	{
		bool bRet = true;
		string fullname = Request.Form["fullname"];
		string filename = Request.Form["filename"];
		Response.Write("<center><h3>Please Wait for few seconds...</h3></center>");
		bRet = doRestoreDB(fullname, filename);
		if(bRet)
			{			
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?retore=done \">");				
				return;
			}
		
	}
	if(Request.QueryString["restore"] == "1")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		PrintRestoreForm();
		//Form1.Visible = true;
		return;
	}
	PrintAdminHeader();
	PrintAdminMenu();
	GetQueryString();
	
	if(Request.Form["cmd"] == "Backup Database" )
	{
//DEBUG("cmd =", Request.Form["cmd"]);
		//if(doBackupDB())
		{
			bool bRet = true;
			
			bRet = CreateBackupFolder();			
			bRet = doBackupDB();
			if(bRet)
			{
				Response.Write("<br><h4>Zipping data files, please wait...");
				Response.Flush();
				//bRet = ZipDir(m_path, m_fileName + ".zip");
				bRet = ZipDir(m_path, "db_backup_" + DateTime.Now.ToString("dd_MM_yy_HH_mm") + ".zip");
				
				Response.Write("done.</h4>\r\n");
			}			
				//clean up zip files
		/*	if(Directory.Exists(m_path))
			{
				string[] files = Directory.GetFiles(m_path, "*.bak");
				for(int i=0; i<files.Length; i++)
				{					
					//File.Copy(files[i], m_destPath); 
						if(File.Exists(files[i]))
						File.Delete(files[i]);
				}
			}
			*/
			if(bRet)
			{			
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?bk=done \">");
				//Response.Redirect(""+ Request.ServerVariables["URL"]+ "?export=done");
				return;
			}
		}
	
	}

	PrintBackupForm();
	
	PrintAdminFooter();
}
void GetQueryString()
{
	if(Request.QueryString["pid"] != null && Request.QueryString["pid"] != "")
	{
		m_SalesType = "Purchase";
		m_invoice_number = Request.QueryString["pid"];
	}
	if(Request.QueryString["oid"] != null && Request.QueryString["oid"] != "")
	{
		m_SalesType = "Sales";
		m_invoice_number = Request.QueryString["oid"];
	}

	
}
void PrintBackupForm()
{
	Response.Write("<br><br><h4><center>Complete Backup Database</h4>");

	Response.Write("<br><br>");
	Response.Write("<table width=40% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<br><br><center>");
	
	Response.Write("<form name=f method=post>");
	Response.Write("<input type=submit name=cmd value='Backup Database' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"return confirm('Continue to backup database!!!'); \" ");
	Response.Write(">");
	
	Response.Write("</form>");
	Response.Write("</table>");
}

void PrintRestoreForm()
{
	Response.Write("<form name=f method=post>");
	Response.Write("<br><br><h4><center>Complete Restore Database</h4>");

	Response.Write("<br><br>");
	Response.Write("<table width=40% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<br><br><center>");
	
//Response.Write("<table align=center cellspacing=7 cellpadding=3 border=0 bordercolor#EEEEEE");
//			Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
			//Response.Write("<tr><td colspan=4><b>To Backup files to database, make sure you have checked the backup file's date!!!</b></td></tr>");
			Response.Write("<tr><td colspan=4><b>Backup Database's File!!!</b></td></tr>");

			Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
			Response.Write("<td><b>FILE</b></td>");
			Response.Write("<td><b>SIZE</b></td>");
			Response.Write("<td><b>FILE DATE</b></td>");
			//Response.Write("<td><b>DOWNLOAD</b></td>");
			Response.Write("</tr>");
			
			
			DirectoryInfo di = new DirectoryInfo(m_path);
			if(!di.Exists)
			{
				Response.Write("<tr><td colspan=4><H2>No File to Restore</h2></td></tr>");
				return;
			}
			int nFoundFile = 0;
			string fileName = "";
			foreach (FileInfo f in di.GetFiles("*.bak")) 
			{				
				string s = f.FullName;
				m_restoreFileName = s;
				
				string file = s.Substring(m_path.Length+1, s.Length-m_path.Length-1);
				fileName = file;
//				string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
				Response.Write("<tr><td>"+ file); //<a href=backup/" + lname + "/" + file + ">" + file);
				Response.Write("</a></td>");
				Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
				Response.Write("<td>" + f.LastWriteTime.ToString("dd-MM-yyyy HH:mm") + "</td>");
			//	Response.Write("<td align=right><a href=backup/" + lname + "/" + file + " class=o>download");
			//	Response.Write("</a></td>");
				Response.Write("</tr>");
				nFoundFile++;
			}			
			Response.Write("<input type=hidden name=fullname value='"+ m_restoreFileName +"'>");
			Response.Write("<input type=hidden name=filename value='"+ fileName +"'>");
		//	Response.Write("</table>");
	if(nFoundFile > 0)
	{
		Response.Write("<tr><td colspan=4 align=right><br><br><input type=submit name=cmd value='Restore Database' "+ Session["button_style"] +"");
		Response.Write(" onclick=\"return confirm('Warning!!! This cannot be undone. Continue to Restore database!!!'); \" ");
		Response.Write("></td></tr>");
	}		
	Response.Write("</table>");
	Response.Write("</form>");
}
bool doBackupDB()
{
	//m_fileName = m_sCompanyName+ "_"+ DateTime.Now.ToOADate()+".bak";
	m_fileName = m_sCompanyName + ".bak";
	string root = Server.MapPath("");
	string sc = " backup Database "+ m_sCompanyName;
	//sc += " To Disk = N'"+ root +"\\backup\\"+ m_sCompanyName+DateTime.Now.ToOADate()+".bak' ";
	sc += " To Disk = N'"+ m_path +"\\"+ m_fileName +"' ";
	sc += " With NOINIT, Name = N'"+ m_path +"\\"+ m_fileName +"', stats=10 ";	
	//sc += " With NOINIT, Name = N'"+ root +"\\backup\\"+ m_sCompanyName+DateTime.Now.ToOADate()+".bak', stats=10 ";	
//DEBUG("sc =", sc);
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

bool doRestoreDB(string fullName, string fileName)
{
	string sc = "SELECT saf.* FROM master.dbo.sysaltfiles saf join master.dbo.sysdatabases sd ON sd.dbid = saf.dbid where sd.name = '"+ m_sCompanyName +"' ";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "db_file");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
	{
		string db_mdf_file = "";
		string db_log_file = "";
		string db_mdf_name = "";
		string db_log_name = "";

		for(int i=0; i<rows;i++)
		{
			DataRow dr = dst.Tables["db_file"].Rows[i];
			if(i== 0)
			{
				db_mdf_file = dr["filename"].ToString();
				db_mdf_name = dr["name"].ToString();
			}
			if(i==1)
			{
				db_log_file = dr["filename"].ToString();
				db_log_name = dr["name"].ToString();
			}
		}
	
		m_fileName = m_sCompanyName + ".bak";
		string root = Server.MapPath("");
	/*	sc = " RESTORE DATABASE "+ m_sCompanyName +"";
		sc += " FROM DISK = N'"+ fullName +"' ";
		sc += " WITH MOVE '"+ db_mdf_name +"' TO '"+ db_mdf_file +"', ";
		sc += " MOVE '"+ db_log_name +"' TO '"+ db_log_file +"' ";
	*/		
	//	sc = " Alter Database "+ m_sCompanyName +"  SET SINGLE_USER With ROLLBACK IMMEDIATE ";
		sc = " RESTORE DATABASE ["+ m_sCompanyName +"] ";
		sc += " FROM DISK = N'"+ fullName +"' ";		
		sc += " WITH FILE = 1,  NOUNLOAD,  REPLACE,  STATS = 10 ";
	//	sc += " Alter Database "+ m_sCompanyName +"  SET MULTI_USER  ";
		//sc += " NORECOVERY; ";

	//DEBUG("sc =", sc);
	//return false;
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
	}
	return true;
}
void ShowProcessDone()
{
	m_ReturnNewOrderID = Request.QueryString["rid"];

	Response.Write("<center><br><h4>Copy Done...</h4>");
	Response.Write("<br><br><font size=4 color=red> New "+ m_SalesType +" Order ID: <a href='olist.aspx?kw="+ m_ReturnNewOrderID +"'>"+ m_ReturnNewOrderID +"</a></font>");
	
	//Response.Write("<script language=javascript>window.alert('Copy Done...'); window.location='"+ Request.ServerVariables["URL"] +"?cd=" + Session["ch_code"].ToString() + "';</script");
	//Response.Write(">");
	Response.Write("</center>");
}

// Processes click on our cmdSend button
void cmdSend_Click(object sender, System.EventArgs e)
{
	// Check to see if file was uploaded
	if( filMyFile.PostedFile != null )
	{
		// Get a reference to PostedFile object
		HttpPostedFile myFile = filMyFile.PostedFile;

		string ext = Path.GetExtension(myFile.FileName);
		ext = ext.ToLower();
		if(ext != ".jpg" && ext != ".gif")
		{
			Response.Write("<h3>ERROR Only .jpg, .gif File Allowed</h3>");
			return;
		}

		// Get size of uploaded file
		int nFileLen = myFile.ContentLength; 
//DEBUG("nFileLen=", nFileLen);
		if(nFileLen > 204800)
		{
			Response.Write("<h3>ERROR Max File Size(200 KB) Exceeded. ");
			Response.Write(Path.GetFileName(myFile.FileName) + " " + (int)nFileLen/1000 + " KB </h3>");
			return;
		}

		// make sure the size of the file is > 0
		if( nFileLen > 0 )
		{
			// Allocate a buffer for reading of the file
			byte[] myData = new byte[nFileLen];

			// Read uploaded file from the Stream
			myFile.InputStream.Read(myData, 0, nFileLen);

			// Create a name for the file to store
			string strFileName = Path.GetFileName(myFile.FileName);
			string strPath = Server.MapPath(".") + "\\pt\\" + 333;
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += "\\";
			string purePath = strPath;
			strPath += strFileName;
			
//DEBUG("pathname=", strPath);
			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);
		//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dev_task.aspx?id=" + m_id + "&nid=" + m_noteid + "&r=" + r + "\">");
		}
	}
	return;
}


void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}

bool CreateBackupFolder()
{	
	if(Directory.Exists(m_path))
		Directory.Delete(m_path, true);
	
	Directory.CreateDirectory(m_path);
//	DEBUG("s mapht=", m_path);	
	return true;
}

bool ZipDir(string dirName, string zipFileName)
{
	string[] filenames = Directory.GetFiles(dirName);
//DEBUG("dir =", dirName);	

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
		//	s = new ZipOutputStream(File.Create(zipFileName.Replace(".zip", "") + "_" + files.ToString() + ".zip"));
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


bool checkLogRstoreDB(bool bRefresh)
{	
	string sc = " SELECT top 1 * ";
	sc += " FROM db_restore_log ";	

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "restore_log");
	}
	catch(Exception e) 
	{
		string bdText = "";
		
		if((e.ToString()).IndexOf("Invalid object name 'db_restore_log'.") >=0 )
		{
			sc = @"
			CREATE TABLE [dbo].[db_restore_log](
				[id] [int] IDENTITY(1,1) NOT NULL,				
				[record_date] [datetime] NOT NULL CONSTRAINT [DF_db_restore_log_record_date]  DEFAULT (getdate()),
				[comments] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF_db_restore_log_comments]  DEFAULT (''),
				[record_by] [int] NOT NULL CONSTRAINT [DF_db_restore_log_record_by]  DEFAULT (0),												
				
			 CONSTRAINT [PK_db_restore_log] PRIMARY KEY CLUSTERED 
			(
				[id] ASC
			) ON [PRIMARY]
			) 		
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
				//ShowExp(sc, ee);
				return false;
			}
			if(bRefresh)
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"? \">");				
		}
	
		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<br>
<table align=center cellspacing=0 cellpadding=3 border=0 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>
<tr><td><font size=+1 color=red><b>Upload Restore Database File!!!</b></font><br>

<table><tr>
<td><input id="filMyFile" type="file" size=50 runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>

<br>
<asp:Label id=LOldPic runat=server/>
</td></tr></table>


</FORM>
<asp:Label id=LFooter runat=server/>

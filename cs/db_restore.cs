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

<script runat=server>
//////////////////////////////////////////////////////////////////////////////////////
//common functions for all sites
SqlConnection restoreConnection;  // = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);
SqlDataAdapter restoreAdapter;
SqlCommand restoreCommand;

string m_path = "";
string m_fileName = "";
string m_destPath = "";
string lname = "";
string m_restoreFileName = "";
DataSet dst = new DataSet();

protected void Page_Load(Object Src, EventArgs E) 
{	
	TS_PageLoad(); //do common things, LogVisit etc...
		
	if(!SecurityCheck("administrator"))
		return;
		//getting server path name 	
	string strPath = Server.MapPath("backup/");
//DEBUG("sdfdksl =", Session["name"].ToString());
	//lname = "db_"+ Session["name"].ToString(); //"backup"; //
	lname = "db_backup";
	
	int bpos = lname.IndexOf(" ");
	if(bpos > 0)
		lname = lname.Substring(0, bpos);
	lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	m_path = strPath + lname;
//DEBUG("<br>mspah=", m_path);

	PrintAdminHeader();
	PrintAdminMenu();	
	
	checkLogRstoreDB(true);

	if(Request.QueryString["restore"] != null && Request.QueryString["restore"] != "")
	{
		if(Request.QueryString["restore"].ToString() == "done")
		{
			//////log the restore db
			string loginName = Session["name"].ToString();
			if(doInsertDBRestoreLog("Restore Database by "+ loginName +""))
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?restore=complete \">");				
				return;
			}		
		}
		if(Request.QueryString["restore"].ToString() == "complete")
		{
			Response.Write("<br><center><h3>Restore Database is completed</h3>");	
			Response.Write("<br><a title='back to login' href='login.aspx'>Login</a></center>");
			return;
		}
	}

	if(Session["name"] == "" || Session["name"] == null)
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"2; URL=login.aspx \">");				
		return;
	}
	//string root = Server.MapPath("");

	if(Request.QueryString["type"] == "upload")
	{
		Form1.Visible = true;
	
		LTitle.Text = "Upload Backup Files(this is your database backup file)";
		//LSkip.Text = "<input type=button value=Skip onclick=window.location=('db_restore.aspx?t=x')>";
		return;
	}
	restoreConnection = new SqlConnection("Initial Catalog=master" + m_sDataSource + m_sSecurityString);

	if(Request.Form["cmd"] == "Restore Database" )
	{
		bool bRet = true;
		string fullname = Request.Form["fullname"];
		string filename = Request.Form["filename"];
		Response.Write("<center><h3>Please Wait for few seconds...</h3></center>");
		
		//close all connection to the database;
		closeAllConnection();
		bRet = doRestoreDB(fullname, filename);
		
		if(bRet)
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"4; URL="+ Request.ServerVariables["URL"] +"?restore=done \">");				
			return;
		}		
	}
	PrintRestoreForm();
	PrintAdminFooter();
}

void PrintRestoreForm()
{	

	Response.Write("<form name=f method=post>");
	Response.Write("<br><br><h2><center>Complete Restore Database</h2>");

	//Response.Write("<br><br>");
	Response.Write("<table width=40% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<br><br><center>");
	
//Response.Write("<table align=center cellspacing=7 cellpadding=3 border=0 bordercolor#EEEEEE");
//			Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
			//Response.Write("<tr><td colspan=4><b>Restore files to database. Make sure you check the Backup Date!!!</b></td></tr>");
		//	Response.Write("<tr><td colspan=4><b>To restore database, make sure you have checked the backup file's date</b></td></tr>");

			Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
			Response.Write("<td><b>FILE</b></td>");
			Response.Write("<td><b>SIZE</b></td>");
			Response.Write("<td><b>FILE DATE</b></td>");
			//Response.Write("<td><b>DOWNLOAD</b></td>");
			Response.Write("</tr>");
//DEBUG("msdf=", m_path);				
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
		Response.Write(" onclick=\"return confirm('************************\\r\\n                 Warning!!!\\r\\n************************\\r\\nThis cannot be undone after restoring.\\r\\n Continue to Restore database!!!'); \" ");
		Response.Write("></td></tr>");
	}		
	else
	{
		Response.Write("<tr><td colspan=4><H2>No File to Restore</h2></td></tr>");
		return;
	}
	Response.Write("<tr><td colspan=4>if you want to use your own backup database file, please copy the file to the server's directory: "+ m_path +"</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

bool doRestoreDB(string fullName, string fileName)
{
	string sc = "";
/*	string sc = "SELECT saf.* FROM master.dbo.sysaltfiles saf join master.dbo.sysdatabases sd ON sd.dbid = saf.dbid where sd.name = '"+ m_sCompanyName +"' ";
	int rows = 0;
	try
	{
		SqlDataAdapter restoreCommand = new SqlDataAdapter(sc, restoreConnection);
		rows = restoreCommand.Fill(dst, "db_file");
	}
	catch(Exception e) 
	{
		restoreShowExp(sc, e);
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
	
	*/
	m_fileName = m_sCompanyName + ".bak";		
	/*	sc = " RESTORE DATABASE "+ m_sCompanyName +"";
		sc += " FROM DISK = N'"+ fullName +"' ";
		sc += " WITH MOVE '"+ db_mdf_name +"' TO '"+ db_mdf_file +"', ";
		sc += " MOVE '"+ db_log_name +"' TO '"+ db_log_file +"' ";
	*/		
	//	sc = " Alter Database "+ m_sCompanyName +"  SET SINGLE_USER With ROLLBACK IMMEDIATE ";
	
		sc = "";
	/*	sc = " Alter DATABASE ["+ m_sCompanyName +"] set Offline ";
		try
		{
			restoreCommand = new SqlCommand(sc);
			restoreCommand.Connection = restoreConnection;
			restoreConnection.Open();
			restoreCommand.ExecuteNonQuery();
			restoreCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			restoreShowExp(sc, e);
			return false;
		}	
		*/
		sc = " USE master; ";
//		sc += " RESTORE FILELISTONLY  ";
//		sc += " FROM DISK = N'"+ fullName +"' ";
		sc += " RESTORE DATABASE ["+ m_sCompanyName +"] ";
		sc += " FROM DISK = N'"+ fullName +"' ";		
		sc += " WITH FILE = 1, NOUNLOAD,  REPLACE,  STATS=10 ";
		//sc += " NORECOVERY; ";
		//sc += " EXEC sp_dboption N'"+ m_sCompanyName +"', 'online', 'TRUE' ";
//Response.Write("sc1 =" + sc);
		try
		{
			restoreCommand = new SqlCommand(sc);
			restoreCommand.CommandTimeout = 120;
			restoreCommand.Connection = restoreConnection;
			restoreConnection.Open();
			restoreCommand.ExecuteNonQuery();
			restoreCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			restoreShowExp(sc, e);
			return false;
		}	
		sc = " Alter DATABASE ["+ m_sCompanyName +"] SET Online ";
		//sc = " EXEC sp_dboption N'"+ m_sCompanyName +"', 'online', 'TRUE' ";
		

//Response.Write("sc =" + sc);
	//return false;
		try
		{
			restoreCommand = new SqlCommand(sc);
			restoreCommand.Connection = restoreConnection;
			restoreConnection.Open();
			restoreCommand.ExecuteNonQuery();
			restoreCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			restoreShowExp(sc, e);
			return false;
		}		
		
//	}
	return true;
}

void restoreShowExp(string query, Exception e)
{
	Response.Write("Execute SQL Query Error.<br>\r\nQuery = ");
	Response.Write(query);
	Response.Write("<br>\r\n Error: ");
	Response.Write(e);
	Response.Write("<br>\r\n");
	string msg = "\r\n<font color=red><b>EXP</b></font><br>\r\n";
	msg += e.ToString();
	msg += "<br><br><font color=red><b>QUERY</b></font><br>\r\n";
	msg += query;
	msg += "<br><br>\r\n\r\n";
	msg += "ip : " + Session["ip"] + "<br>\r\n";
	msg += "login : " + Session["name"] + "<br>\r\n";
	msg += "email : " + Session["email"] + "<br>\r\n";
	msg += "url : " + Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "<br>\r\n";
//	AlertAdmin(msg);
}

void closeAllConnection()
{
		string sc = "USE [master]";
	sc += " SELECT sps.spid from master.dbo.sysprocesses sps join master.dbo.sysdatabases sdb ON sdb.dbid = sps.dbid where sdb.name= N'"+ m_sCompanyName +"' ";
//	sc += " EXEC sp_dboption N'"+ m_sCompanyName +"', 'offline', 'TRUE' ";
//	sc += " KILL (select sdb.spid from master.dbo.sysprocesses sps join master.dbo.sysdatabases sdb ON sdb.dbid = sps.dbid where sdb.name= N'"+ m_sCompanyName +"') ";
//DEBUG("sc = ", sc );
	int rows = 0;
	try
	{
		restoreAdapter = new SqlDataAdapter(sc, restoreConnection);		
		rows = restoreAdapter.Fill(dst, "ActiveProcess");		
	}
	catch(Exception e) 
	{
		restoreShowExp(sc, e);
		return;
	}
//DEBUG("rows = ", rows);
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["ActiveProcess"].Rows[i];
		string pid = dr[0].ToString();
		sc = " KILL "+ pid; 
//	DEBUG("sc =", sc);
		try
		{
			restoreCommand = new SqlCommand(sc);
			restoreCommand.Connection = restoreConnection;
			restoreConnection.Open();
			restoreCommand.ExecuteNonQuery();
			restoreCommand.Connection.Close();
		}
		catch(Exception e) 
		{
			Response.Write("<center><h3>Database is still in Used!!! Please wait until No One is Using Then Restoring the Database!!!</h3>");
			Response.Write("<a title='refresh the page' href='"+ Request.ServerVariables["URL"] +"'>Refresh</a></center>");
			return;			
		}	
	}
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
				ShowExp(sc, ee);
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
bool doInsertDBRestoreLog(string comments)
{	
	//switch back to the current database::::
	restoreConnection = new SqlConnection("Initial Catalog="+ m_sCompanyName + m_sDataSource + m_sSecurityString);
//DEBUG("restoreConnection-:", restoreConnection.ToString());
	string sc = " INSERT INTO db_restore_log ";
	sc += " (record_date, comments, record_by) ";
	sc += " VALUES( ";
	sc += " GETDATE() ";
	sc += ", '" + EncodeQuote(comments) + "' ";		
	sc += ", " + Session["card_id"].ToString();	
	sc += ") ";
	
/*	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myCommand.Connection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	*/
	try
	{
		restoreCommand = new SqlCommand(sc);
		restoreCommand.Connection = restoreConnection;
		restoreConnection.Open();
		restoreCommand.ExecuteNonQuery();
		restoreCommand.Connection.Close();
	}
	catch(Exception ee) 
	{
		ShowExp(sc, ee);
		return false;
	}
	
	return true;
}

bool ExtractAllZipFile(string sPath)
{
	if(Directory.Exists(sPath))
	{
		DirectoryInfo di = new DirectoryInfo(sPath);
		foreach (FileInfo f in di.GetFiles("*.zip")) 
		{
			string s = f.FullName;
			DoUnZipFile(m_path, s);
		}
	}
	return true;
}

bool DoUnZipFile(string dest_dir, string pathname)
{
	if(!File.Exists(pathname))
	{
		Response.Write("<br><center><h3>File " + pathname + " not exists");
		return false;
	}

	ZipInputStream s = new ZipInputStream(File.OpenRead(pathname));
	
	ZipEntry theEntry;
	Response.Write("<b>Extracting files, please wait.");
	while ((theEntry = s.GetNextEntry()) != null) 
	{
//		string directoryName = Path.GetDirectoryName(theEntry.Name);
		string fileName = Path.GetFileName(theEntry.Name);
		string dest_file = dest_dir + "\\" + fileName;
//		Response.Write(fileName + "<br>");
		MonitorProcess(1);
//DEBUG("dest_file=", dest_file);		
		
		if(fileName != String.Empty) 
		{
			FileStream streamWriter = File.Create(dest_file);
		
			//int size = 2048;
			int size = 204800;
			byte[] data = new byte[2048];
			while (true) 
			{
				size = s.Read(data, 0, data.Length);
				if(size > 0) 
					streamWriter.Write(data, 0, size);
				else 
					break;
			}
			streamWriter.Close();
		}
	}
	Response.Write("done</b><br>");

	s.Close();
	return true;
}

// Processes click on our cmdSend button
void cmdSend_Click(object sender, System.EventArgs e)
{
	DEBUG("fidsl =", filMyFile.PostedFile.ToString());
	if(filMyFile.PostedFile == null)
		return;

	HttpPostedFile myFile = filMyFile.PostedFile;
	int nFileLen = myFile.ContentLength; 

	if(nFileLen <= 0)
		return;

	string strFileName = Path.GetFileName(myFile.FileName);
	string vpath = m_path;

	if(!Directory.Exists(m_path))
		Directory.CreateDirectory(m_path);

	vpath += "\\";
	string purePath = vpath;
	string strPath = vpath + strFileName;

	FileStream streamWriter = File.Create(strPath);
		
	//int size = 2048;
	int size = 204800;
	byte[] data = new byte[2048];
	while(true) 
	{
		size = myFile.InputStream.Read(data, 0, data.Length);
		if(size > 0) 
			streamWriter.Write(data, 0, size);
		else 
			break;
	}
	streamWriter.Close();
	LUploaded.Text = ShowFolderContents();
}

string ShowFolderContents()
{
	StringBuilder sb = new StringBuilder();

	if(Directory.Exists(m_path))
	{
		sb.Append("<table align=center cellspacing=7 cellpadding=3 border=0 bordercolor#EEEEEE");
		sb.Append(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
		sb.Append("<tr><td colspan=4><b>Uploaded files</b></td></tr>");

		sb.Append("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
		sb.Append("<td><b>FILE</b></td>");
		sb.Append("<td><b>SIZE</b></td>");
		sb.Append("<td><b>FILE DATE</b></td>");
		sb.Append("</tr>");

		DirectoryInfo di = new DirectoryInfo(m_path);
		foreach (FileInfo f in di.GetFiles("*.*")) 
		{
			string s = f.FullName;
			string file = f.Name;
			sb.Append("<tr><td>" + file + "</td>");
			sb.Append("<td>" + (f.Length/1000).ToString() + "K</td>");
			sb.Append("<td>" + f.LastWriteTime.ToString("dd-MM-yyyy HH:mm") + "</td>");
			sb.Append("</tr>");
		}
		sb.Append("</table>");
	}
	return sb.ToString();
}
</script>


<br>
<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<center><h4>
<asp:Label id=LTitle runat=server/>
</h3>
<table><tr>
<td><b>zip file : </b> <input id="filMyFile" type="file" runat="server"></td><td></td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
<td><input type=button value=Skip onclick=window.location=('db_restore.aspx?t=x')></td>
</tr></table>
</form>
<asp:Label id=LSkip runat=server/>
<asp:Label id=LUploaded runat=server/>
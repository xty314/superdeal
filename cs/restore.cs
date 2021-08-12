
<%@ Import Namespace="ICSharpCode.SharpZipLib.BZip2" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>

<script runat=server>
//for common.cs
const int m_nFirstCode = 10001;
string m_sCompanyTitle = "";
bool m_bCheckLogin = true;
bool g_bRetailVersion = false;
bool g_bOrderOnlyVersion = false;
bool g_bUseSystemQuotation = true;
bool g_bSysQuoteAddHardwareLabourCharge = true;
bool g_bSysQuoteAddSoftwareLabourCharge = true;
string m_sSite = "admin";
//for common.cs

string m_type = "";
string m_path = "";

void Page_Load(Object Src, EventArgs E ) 
{
//	TS_PageLoad(); //do common things, LogVisit etc...
//	if(!SecurityCheck("normal"))
//		return;

	myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);

	string strPath = Server.MapPath("backup/");
	string lname = "master";
	if(Session["name"] != null)
	{
		lname = Session["name"].ToString();
		int bpos = lname.IndexOf(" ");
		if(bpos > 0)
			lname = lname.Substring(0, bpos);
		lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
	}
	m_path = strPath + lname + "\\restore";

	if(!Loggedin())
	{
		PrintLoginForm();
		return;
	}

	Response.Write(m_sHeader);

	if(Request.Form["cmd"] == "Restore")
	{
		if(DoRestoreTables())
		{
			if(RestoreImages())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=finish\">");
		}
		else
			Response.Write("<br><h4><font color=red>Restore failed.</font></h4>");
		return;
	}


	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];
	if(m_type == "upload")
	{	
		Form1.Visible = true;
		LTitle.Text = "Upload Backup Files(you can upload more than one file, click done to process)";
		LSkip.Text = "<input type=button value=Done onclick=window.location=('restore.aspx?t=x')>";
	}
/*	else if(m_type == "upload_image")
	{
		Form1.Visible = true;
		LTitle.Text = "Upload Item Image Backup File";
		LSkip.Text = "<input type=button value=Done onclick=window.location=('restore.aspx?t=x')>";
	}
*/
	else if(m_type == "x") //extract
	{
		if(ExtractAllZipFile(m_path))
		{
			PrintSelectionForm("*.csv");
		}
	}
	else if(m_type == "finish")
	{
		Session["EZNZ_RESTORE_SESSION"] = null;
		Response.Write("<br><h4>Restore finished.");
	}
}

bool DoRestoreTables()
{
	if(!Directory.Exists(m_path))
	{
		Response.Write("<br><center><h3>Error, restore directory not found, please upload file first.");
		Response.Write("<meta http-equiv=\"refresh\" content=\"10; URL=?t=upload\">");
		return false;
	}

	DirectoryInfo di = new DirectoryInfo(m_path);
	foreach (FileInfo f in di.GetFiles("*.csv")) 
	{
		string s = f.FullName;
		if(f.Length <= 0)
			continue;
		string file = f.Name;
		if(Request.Form["sel_" + file] != "on") 
			continue;
		string sTable = f.Name;
		int p = sTable.IndexOf('.');
		sTable = sTable.Substring(0, p);
		if(!RestoreOneTable(s, sTable))
			return false;
	}
	return true;
}

bool HasIdent(string table)
{
	string sc = " SET IDENTITY_INSERT " + table + " ON ";
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
		myCommand.Connection.Close();
		return false;
	}
	
	sc = " SET IDENTITY_INSERT " + table + " OFF ";
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
		myCommand.Connection.Close();
		ShowExp(sc, e);
		return true;
	}
	return true;
}

bool RestoreOneTable(string fileName, string sTable)
{
	int i = 0;
	int j = 0;

	Response.Write("<b>Restoring table " + sTable + ", please wait...");

	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);

	string sLine = r.ReadLine(); //column names line
	if(sLine == null || sLine == "")
	{
		Response.Write("<br><h3>Bad file, no column name defined");
		return false;
	}

	bool bHasIdent = HasIdent(sTable);
	
	string sc_header = ""; //SET IDENTITY_INSERT " + sTable + " ON ";
	string sc_tail = ")";//SET IDENTITY_INSERT " + sTable + " OFF ";
	if(bHasIdent)
	{
		sc_header += " SET IDENTITY_INSERT " + sTable + " ON ";
		sc_tail += " SET IDENTITY_INSERT " + sTable + " OFF ";
	}
	sc_header += " SET DATEFORMAT dmy INSERT INTO " + sTable + "(";
	{
		sc_header += sLine;
	}
	sc_header += ") VALUES(";
//DEBUG("sc_header=", sc_header);

	sLine = r.ReadLine(); //data type line
	if(sLine == null || sLine == "")
	{
		Response.Write("<br><h3>Bad file, no column data type defined");
		return false;
	}
	string[] data_type = new string[256];
	string sType = "";
	for(i=0; i<sLine.Length; i++)
	{
		if(sLine[i] == ',' || sLine[i] == '\r' || i == sLine.Length-1)
		{
			if(i == sLine.Length-1) //get last character
				sType += sLine[i];
			data_type[j] = sType;
			j++;
			sType = "";
		}
		else
			sType += sLine[i];
	}

	string sc = " DELETE " + sTable;
//DEBUG("sc=", sc);
//return true;
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

	bool bQuoteBegin = false;
	bool bQuoteEnd = false;
	sLine = r.ReadLine(); //begin data row
	while(sLine != null)
	{
		j = 0;
		string sValue = "";
		string s = "";
		for(i=0; i<sLine.Length; i++)
		{
			if(sLine[i] == '\"')
			{
				if(!bQuoteBegin)
				{
					bQuoteBegin = true;
					continue;
				}
				else
				{
					if(i < sLine.Length-1) //double quote?
					{
						if(sLine[i+1] == '\"')
						{
							sValue += "\"";
							i++;
							continue;
						}
					}
					bQuoteBegin = false;
					bQuoteEnd = true;
				}
			}
			else
			{
				if(bQuoteBegin)
					sValue += sLine[i];
			}

			if(bQuoteEnd)
			{
				bQuoteEnd = false;
				if(s != "")
					s += ",";
				if(sValue == "")
				{
					if(data_type[j] != "String")
						s += "null";
					else
						s += "''";
				}
				else
				{
					sValue = sValue.Replace("\\r\\n", "\r\n");
					switch(data_type[j])
					{
					case "String":
						s += "'" + EncodeQuote(sValue) + "'";
						break;
					case "DateTime":
						System.IFormatProvider format =	new System.Globalization.CultureInfo("en-NZ", false);
						s += "'" + DateTime.Parse(sValue, format, System.Globalization.DateTimeStyles.NoCurrentDateDefault).ToString("dd/MM/yyyy HH:mm:ss") + "'";
						break;
					case "Boolean":
						if(MyBooleanParse(sValue))
							s += "1";
						else
							s += "0";
						break;
					default:
						s += sValue;
						break;
					}
				}
				j++;
				sValue = "";
			}
		}
		MonitorProcess(10);
		sc = sc_header + s + sc_tail;
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
		sLine = r.ReadLine();
	}
	r.Close();
	Response.Write("done</b><br>");
	return true;
}

void PrintSelectionForm(string ext)
{
	Response.Write("<form name=form action=? method=post>");
	Response.Write("<table cellspacing=0 cellpadding=0 border=0 bordercolor#EEEEEE");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<tr><td colspan=4><b>" + ext + " files list, select to restore</b></td></tr>");

	Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
	Response.Write("<td><b>Table</b></td>");
	Response.Write("<td><b>Size</b></td>");
	Response.Write("<td><b>File Date</b></td>");
	Response.Write("<td><b>Select</b></td>");
	Response.Write("</tr>");

	DirectoryInfo di = new DirectoryInfo(m_path);
	foreach (FileInfo f in di.GetFiles(ext)) 
	{
		string s = f.FullName;
		if(f.Length <= 0)
			continue;
		string file = f.Name;
		Response.Write("<tr><td>" + file + "</td>");
		Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
		Response.Write("<td>" + f.LastWriteTime.ToString("dd-MM-yyyy HH:mm") + "</td>");
		Response.Write("<td align=right><input type=checkbox name=sel_" + file + " checked></td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=4 align=right>Select All");
	Response.Write("<input type=checkbox name=allbox onClick='CheckAll();' checked>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=4 align=right>");
	Response.Write("<input type=submit name=cmd value=Restore>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	PrintJavaFunctions();
}

bool RestoreImages()
{
	DirectoryInfo di = new DirectoryInfo(m_path);
	foreach (FileInfo f in di.GetFiles("*.csv")) 
	{
		File.Delete(f.FullName);
	}
	foreach (FileInfo f in di.GetFiles("*.zip")) 
	{
		File.Delete(f.FullName);
	}
	
	di = new DirectoryInfo(m_path);
	string name = "";
	string dest_path = Server.MapPath("../pi");
	foreach (FileInfo f in di.GetFiles("*.*")) 
	{
		string dest_file = dest_path + "\\" + f.Name;
		File.Copy(f.FullName, dest_file, true);
	}
	return true;
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.form.elements.length;i++) 
		{
			var e = document.form.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.form.allbox.checked;
		}
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
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
		
			int size = 2048;
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
		
	int size = 2048;
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

bool Loggedin()
{
	if(Session["EZNZ_RESTORE_SESSION"] != null)
		return true;
	string pass = "";
	if(Session["pass"] != null)
		pass = Session["pass"].ToString();
	if(Request.Form["pass"] != null)
		pass = Request.Form["pass"];
	pass = FormsAuthentication.HashPasswordForStoringInConfigFile(pass, "md5");
	if(pass == "410B6E86CA31315A55EF83F4686634C0")
	{
		if(Request.Form["pass"] != null)
			Session["pass"] = Request.Form["pass"];
		Session["EZNZ_RESTORE_SESSION"] = true;
		return true;
	}

	if(Directory.Exists(m_path)) //empty folder
	{
		try
		{
			Directory.Delete(m_path, true);
		}
		catch(Exception e1)
		{
			Response.Write("<br><h3><font color=red>Delete folder failed : " + e1.ToString() + "</font></h3>");
			return true;
		}
	}
	return false;
}

void PrintLoginForm()
{
	Response.Write(m_sHeader);
	Response.Write("<form name=f action=?t=upload method=post>");
	Response.Write("<br><table align=center>");
	Response.Write("<tr><td colspan=2><font size=+1>Restore Database</font></td></tr>");
//	Response.Write("<tr><td><b>Name : </b><td><input type=text name=name></td></tr>");
	Response.Write("<tr><td nowrap><b>Master Password : </b><td><input type=password size=50 name=pass></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=GO></td></tr>");

	Response.Write("<script");
	Response.Write(">document.f.pass.focus();</script");
	Response.Write(">");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");
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

string m_sHeader = @"
	<html><head>
	<title>EZNZ System Restore</title>

	<style type=text/css>

	td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:vardana;}
	body{font:10px Verdana;}
	a{color:#0033FF;text-decoration:none;color=#000000} 
	a:hover{text-decoration:underline;color=red}
	.m{TEXT-DECORATION:none;background-color:#EEEEEE;border: 1px solid #000000;}
	.x{FONT-WEIGHT:300;FONT-SIZE:8PT;TEXT-DECORATION:underline;FONT-FAMILY:verdana;COLOR:#0000ff;}
	.w{color:#FFFFFF;text-decoration:none} a.w:hover{color:#FF0000;text-decoration:none}
	.d{color:#000000;text-decoration:none} a.d:hover{color:#FF0000;text-decoration:none}
	.o{color:#0000FF;text-decoration:underline} a.o:hover{color:#FF0000;text-decoration:none}

	</style>
	</head>
	<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0>
";

</script>
<br>
<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<center><h4>
<asp:Label id=LTitle runat=server/>
</h3>
<table><tr>
<td><b>zip file : </b> <input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</form>
<asp:Label id=LSkip runat=server/>
<asp:Label id=LUploaded runat=server/>

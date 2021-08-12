<%@ Import Namespace="ICSharpCode.SharpZipLib.BZip2" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.Zip" %>
<%@ Import Namespace="ICSharpCode.SharpZipLib.GZip" %>
<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_path = "";
string m_sDestPath = "";
int m_nImage = 0;
int m_nInvalidFileName = 0;
int m_nInvalidMPN = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;
	
	m_path = Server.MapPath("./data/pi");
	m_sDestPath = Server.MapPath("../pi");

	PrintAdminHeader();
	PrintAdminMenu();
}
bool DoMoveImageFile(string sFileName, string sPath)
{
	string mpn = sFileName.Replace(".jpg", "");	
/*	string code = GetCodeFromMPN(mpn);
	if(code == "")
	{
		m_nInvalidMPN++;
		return true;
	}
*/ 
//	string sDest = m_sDestPath + "\\" + code + ".jpg";
	string sDest = m_sDestPath + "\\" + sFileName;
	try
	{
		if(File.Exists(sDest))
			File.Delete(sDest);
		File.Move(sPath, sDest);
		string purePath = Server.MapPath("../pi");
		if(Directory.Exists(purePath + "\\t"))
		{
			FileStream newFile = new FileStream(sDest, FileMode.Open);
			CreateThumbnail(sDest, purePath, newFile, 180, 180);
			newFile.Close();
		}
	}
	catch(Exception e)
	{
		Response.Write("error, " + e.ToString() + "<br>");
		return false;
	}
	m_nImage++;
	return true;
}
bool DoProcessFile(string pathname)
{
	if(!File.Exists(pathname))
	{
		Response.Write("<br><center><h3>File " + pathname + " not exists");
		return false;
	}
	string dest_dir = m_path;
	ZipInputStream s = new ZipInputStream(File.OpenRead(pathname));
	ZipEntry theEntry;
	Response.Write("<b>Extracting files, please wait.");
	while ((theEntry = s.GetNextEntry()) != null) 
	{
		string fileName = Path.GetFileName(theEntry.Name).ToLower();
		if(fileName.Length < 4)
		{
			m_nInvalidFileName++;
			continue;
		}
		if(fileName.Substring(fileName.Length - 4, 4) != ".jpg")
		{
			m_nInvalidFileName++;
			continue;
		}
		string dest_file = dest_dir + "\\" + fileName;
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
			if(!DoMoveImageFile(fileName, dest_file))
			{
				s.Close();
				return false;
			}
		}
	}
	s.Close();
	File.Delete(pathname);
	return true;
}
void cmdSend_Click(object sender, System.EventArgs e)
{
	if(filMyFile.PostedFile == null)
		return;

	HttpPostedFile myFile = filMyFile.PostedFile;
	int nFileLen = myFile.ContentLength; 
	if(nFileLen <= 0)
		return;

	string ext = Path.GetExtension(myFile.FileName);
	ext = ext.ToLower();
	if(ext != ".zip")
	{
		Response.Write("<h3>ERROR Only .zip File Allowed</h3>");
		return;
	}

	string strFileName = Path.GetFileName(myFile.FileName);
	string vpath = m_path;

	if(!Directory.Exists(m_path))
		Directory.CreateDirectory(m_path);

	vpath += "\\";
	string purePath = vpath;
	string strPath = vpath + strFileName;
//DEBUG("sp=", strPath);
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
	if(!DoProcessFile(strPath))
		return;
	Response.Write("<br>done<br>");
	Response.Write("Invalid File Name : " + m_nInvalidFileName + "<br>");
	Response.Write("Invalid MPN : " + m_nInvalidMPN + "<br>");
	Response.Write(m_nImage + " image successfully uploaded.<br>");
}
string GetCodeFromMPN(string mpn)
{
	if(ds.Tables["data"] != null)
		ds.Tables["data"].Clear();
	string sc = " SELECT code FROM code_relations WHERE LOWER(supplier_code) = '" + EncodeQuote(mpn) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "data") <= 0)
			return "";
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}
	return ds.Tables["data"].Rows[0]["code"].ToString();
}
</script>

<br><center><h4>Upload Product Image</h4>
<h5>Please use mpn.jpg format, zipped in one zip file</h5>
<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=true>
<table><tr>
<td><input id="filMyFile" type="file" runat="server" size=50></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</FORM>

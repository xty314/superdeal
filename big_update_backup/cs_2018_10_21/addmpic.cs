<%@ Import Namespace="System.Collections" %>
<%@ Import Namespace="System.Collections.NumericComparer" %>

<!-- #include file="kit_fun.cs" -->
<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations
string m_code = "";
string m_pic = "";
string m_name = "";
string m_fileName = "";

bool m_bKit = false;
string m_kit = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	InitKit();

	if(Request.QueryString["kit"] == "1")
	{	
		m_bKit = true;
		m_kit = "1";
	}

	GetQueryStrings();

	if(Request.QueryString["t"] == "da")
	{
		DoDelPic();
		
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?kit=" + m_kit + "&code=" + m_code + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	LFooter.Text = m_sAdminFooter;

	if(!m_bKit)
	{

		Response.Write("<img src=r.gif><a href=liveedit.aspx?code=" + m_code + " class=x>Edit Product</a> ");
		Response.Write(" <img src=r.gif><a href=ep.aspx?code=" + m_code + " class=x>Edit Specifications</a> ");
		Response.Write(" <img src=r.gif><a href="+ Request.ServerVariables["URL"] +"?code=" + m_code + " class=x>Edit Photo</a><br><h3> ");
	}

	if(m_code != null)
	{
		LTitle.Text = "<h3>Upload Image for ";
		if(m_bKit)
			LTitle.Text += m_sKitTerm + " # ";
		else
			LTitle.Text = "product code ";
		LTitle.Text += m_code;
		LTitle.Text += "</h3> - " + m_name + "<br><br>";
		GetOldData(); //display old photo if exists, otherwise insert new row, prepare to upload
		Form1.Visible = true;
	}
	else
	{
		LTitle.Text = "<font size=+4><b>Product Code Error</b></font><br>";
		LTitle.Text +="<br><b>no product code, please use proper links. eg: "+ Request.ServerVariables["URL"] +"?code=100408</b>";
	}
}

// Processes click on our cmdSend button
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
			string strFileName = m_code +"_temp";
			string sExt = Path.GetExtension(myFile.FileName);
			strFileName += sExt;
			m_fileName = strFileName;
			string vpath = GetRootPath();
			string multi_vpath = vpath;
			if(m_bKit)
				vpath += "/pk/";
			else
				vpath += "/pi/";				
			
			multi_vpath = vpath + m_code +"/";
			string strPath = Server.MapPath(vpath);		
			string multi_strPath = Server.MapPath(multi_vpath);
			string purePath = strPath;
			//save item picture in the item foler...			
			string sFolderPath = CreateItemImageFolder(multi_strPath);
			int nFileID = 1;
			
			getLastImageFromFolder(sFolderPath, ref nFileID);
			
			sFolderPath += m_code +"_"+ nFileID + sExt;

			strPath += strFileName;			

			//check old files, delete .gif or jpg (another type)if exists
			string sExtOld = ".gif";
			if(String.Compare(sExt, ".gif", true) == 0)
				sExtOld = ".jpg";
			string oldFile = purePath + m_code + sExtOld;
			if(File.Exists(oldFile))
				File.Delete(oldFile);

			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData, purePath, true);
			WriteToFile(sFolderPath, ref myData, purePath, false);
	
			//delete the temp image file		
			System.IO.File.Delete(strPath);
return;
			if(!m_bKit)
				WriteEditLog();

			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?kit=" + m_kit + "&code=");
			Response.Write(m_code + "&name=" + HttpUtility.UrlEncode(m_name));
			Response.Write("\">");
		}
	}
}

bool WriteEditLog()
{
	string sc = "INSERT INTO edit_log (code, filename, editor, clienthost, site, logtime, type) VALUES(";
	sc += m_code + ", '" + m_fileName + "', '" + Session["name"] + "', '" + Session["rip"];
	sc += "', '" + Session["site"] + "', GETDATE(), 'pic')";
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

// Writes file to current folder
void WriteToFile(string strPath, ref byte[] Buffer, string purePath, bool firstUpload)
{	
	// Create a file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(Buffer, 0, Buffer.Length);

	if(firstUpload)
		resideImage(strPath, purePath, newFile);

	// Close file
	newFile.Close();
	
}

Boolean GetOldData()
{
	string sPicFile = "";

	string vpath = GetRootPath();

	if(m_bKit)
		vpath += "/pk/"+ m_code +"/";
	else
		vpath += "/pi/"+ m_code +"/";
	
	string strPath = Server.MapPath(vpath);			

	if(m_bKit)
		sPicFile = GetKitImgSrc(m_code);		
	else
		sPicFile = GetProductImgSrc(m_code);
	
	
	LOldPic.Text = "<b>Old Image:</b> ";
	LOldPic.Text += "<table border=1 cellspacing=0 cellpadding=0>";
	LOldPic.Text += "<tr>";
		
	string sFolderPath = CreateItemImageFolder(strPath);

	if(Directory.Exists(sFolderPath))
	{		
		DirectoryInfo di = new DirectoryInfo(sFolderPath);
		foreach(FileInfo f in di.GetFiles("*.*"))
		{
			if(f.Name.IndexOf(".db") >= 0)
				continue;
			LOldPic.Text += "<td>" + f.Name;
			LOldPic.Text += "<br><img src=" + strPath + f.Name + ">";
			LOldPic.Text += "<br><a href="+ Request.ServerVariables["URL"] +"?kit=" + m_kit + "&code=" + m_code + "&t=da&file=" + HttpUtility.UrlEncode(vpath + f.Name);			
			LOldPic.Text += " class=o>DELETE</a>";
			LOldPic.Text += "</td>";

		}
		LOldPic.Text += "</tr></table>";
	}
	LOldPic.Text += "<b>Thumb Image:</b> ";
	LOldPic.Text += m_pic;
	LOldPic.Text += "<br><img src=" + sPicFile + ">";
	LOldPic.Text += "<br><a href="+ Request.ServerVariables["URL"] +"?kit=" + m_kit + "&code=" + m_code + "&t=da&file=" + HttpUtility.UrlEncode(sPicFile);
	LOldPic.Text += " class=o>DELETE</a>";
	
	return true;
}

void GetQueryStrings()
{
	m_code = Request.QueryString["code"];
	m_name = Request.QueryString["name"];
}

void PrintForm()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<form id=Form1 method=post runat=server enctype='multipart/form-data'>");
	sb.Append("<input id=filMyFile type=file runat=server>");
	sb.Append("</form>");
	Response.Write(sb.ToString());
}

void DoDelPic()
{
	string file = Server.MapPath(Request.QueryString["file"]);
	File.Delete(file);
}

string CreateItemImageFolder(string spath)
{
	if(!Directory.Exists(spath))
		Directory.CreateDirectory(spath);
	return spath;
}
string getLastImageFromFolder(string spath, ref int nFileLength)
{

	string sletter = "abcdefghijklmnpqrstuvwxyz";
	if(Directory.Exists(spath))
	{	
		DirectoryInfo di = new DirectoryInfo(spath);		
		
		FileInfo[] files = di.GetFiles("*.*");
		string [] f1 = Directory.GetFiles(spath, "*.*");
//		NumericComparer ns = new NumericComparer();
		Array.Sort(f1, ns); 
		//nFileLength = files.Length;	
		
		nFileLength = f1.Length;
		string templetter = "a";
		string f = files[files.Length-1].ToString();
	//	DEBUG("sdfds =", f1[f1.Length-2]);	
	
//		DEBUG("nFileLength", nFileLength);
		
	}
	
	return "";
}

void resideImage(string imageFileWithPath, string sPath, System.IO.FileStream newFile)
{
	//String[] files = Directory.GetFiles("c:\\images");
	//foreach (string imagefile in files)
	{
	
		FileInfo FileProps =new FileInfo(imageFileWithPath);
		string filename = FileProps.Name;
		string fileextension = FileProps.Extension;
		string [] Split = filename.Split(new Char [] {'.'}); //removes all character after and including (.)
		filename = Split[0].ToString();
		if (fileextension.ToLower() == ".jpg" || fileextension.ToLower() == ".gif" || fileextension.ToLower() == ".bmp")
		{
			System.Drawing.Image OriginalImage;
			OriginalImage = System.Drawing.Image.FromStream(newFile);
					//resize image value;
		float ratio;
int maxWidth = 60;
int maxHeight = 50;
	  // Get height and width of current image
        int width = (int)OriginalImage.Width;
        int height = (int)OriginalImage.Height;

        // Ratio and conversion for new size
        if (width > maxWidth)
        {
            ratio = (float)width / (float)maxWidth;
            width = (int)(width / ratio);
            height = (int)(height / ratio);
        }

        // Ratio and conversion for new size
        if (height > maxHeight)
        {
            ratio = (float)height / (float)maxHeight;
            height = (int)(height / ratio);
            width = (int)(width / ratio);
        }

			System.Drawing.Image thumbnailImage;
			thumbnailImage = OriginalImage.GetThumbnailImage(width, height, new System.Drawing.Image.GetThumbnailImageAbort(myCallBack), IntPtr.Zero);
			thumbnailImage.Save(sPath + m_code + ".jpg");			
			thumbnailImage.Dispose();
			newFile.Close();
		}
	}
}

bool myCallBack()
{
	return true;
}
</script>

<asp:label id=LTitle runat=server/>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<table><tr>
<td><input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>

<br>
<asp:Label id=LOldPic runat=server/>

</FORM>
<asp:Label id=LFooter runat=server/>
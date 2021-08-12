<!-- #include file="kit_fun.cs" -->
<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations
string m_code = "";
string m_pic = "";
string m_name = "";
string m_fileName = "";

int m_nCols = 3;
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
		DoDelPDF();
        Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=addpdf.aspx?kit=" + m_kit + "&code=" + m_code + "\">");
		return;
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h4>Upload PDF File for </h4></center>");
	LFooter.Text = m_sAdminFooter;
	
	/*if(!m_bKit)
	{
		Response.Write("<img src=r.gif> <a href=ep.aspx?code=");
		Response.Write(m_code);
		Response.Write(" target=_blank>Edit Specifications</a> ");

		Response.Write("<img src=r.gif> <a href=liveedit.aspx?code=");
		Response.Write(m_code);
		Response.Write(" target=_blank>Edit Product Details</a>");
	}
	*/
	if(m_code != null)
	{
		//LTitle.Text = "<h3>Upload PDF File for ";
		//LTitle.Text = "<br><center><h4>Upload PDF File for </h4></center>";
		if(m_bKit)
			LTitle.Text += m_sKitTerm + " # ";
		else
			LTitle.Text = "<b>Product Code: ";
		LTitle.Text += m_code;
		LTitle.Text += "</b><h4> - " + m_name + "</h4>";

		GetOldData(); //display old photo if exists, otherwise insert new row, prepare to upload
		Form1.Visible = true;
	}
	else
	{
		LTitle.Text = "<font size=+4><b>Product Code Error</b></font><br>";
		LTitle.Text +="<br><b>no product code, please use proper links. eg: addpdf.aspx?code=100408</b>";
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
			string strFileName = m_code;
			string sExt = Path.GetExtension(myFile.FileName);
			strFileName += sExt;
			m_fileName = strFileName;
			string vpath = GetRootPath();
			vpath += "../pdf/";
			/*if(m_bKit)
				vpath += "/pk/";
			else
				vpath += "/pi/";
				*/
			string strPath = Server.MapPath(vpath);
			string purePath = strPath;
			strPath += strFileName;
			
			//check old files, delete .gif or jpg (another type)if exists
			string sExtOld = ".pdf";
			if(String.Compare(sExt, ".pdf", true) == 0)
				sExtOld = ".pdf";
			string oldFile = purePath + m_code + sExtOld;
			if(File.Exists(oldFile))
				File.Delete(oldFile);

			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);

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
void WriteToFile(string strPath, ref byte[] Buffer)
{
	// Create a file
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	// Write data to the file
	newFile.Write(Buffer, 0, Buffer.Length);
	// Close file
	newFile.Close();
}

Boolean GetOldData()
{

/*
	string sPDFFile = "";
	//if(m_bKit)
	//	sPDFFile = GetKitImgSrc(m_code);
	//else
		sPDFFile = GetProductPDFSrc(m_code);
	//LOldPic.Text = "<tr><td colspan="+ m_nCols +">TESTING!!!!:::   " +m_pic+ "</b> ";
	LOldPic.Text = "<tr><td colspan="+ m_nCols +">Old PDF:</b> ";
	//LOldPic.Text += m_pic;
	//LOldPic.Text += "</td></tr><tr><td colspan="+ m_nCols +"><img src=" + sPDFFile + "></td></tr>";
	LOldPic.Text += "</td></tr><tr><td colspan="+ m_nCols +"><a title='File name is " + sPDFFile + "' href='" + sPDFFile + "' class=o target=blank>" + sPDFFile + "</a></td></tr>";
	LOldPic.Text += "<tr><td colspan="+ m_nCols +">";
	if(sPDFFile != "Not Exist")
	{
		LOldPic.Text += "<a href="+ Request.ServerVariables["URL"] +"?kit=" + m_kit + "&code=" + m_code + "&t=da&file=" + HttpUtility.UrlEncode(sPDFFile);
		LOldPic.Text += " class=o>DELETE</a> &nbsp;| &nbsp;"; //</td></tr>";
		
	}
  * */

    string sPDFFile = "";
    sPDFFile = "../pdf/" + m_code + ".pdf";

    LOldPic.Text = "<tr><td colspan="+ m_nCols +">Old PDF:</b> ";
	LOldPic.Text += "</td></tr><tr><td colspan="+ m_nCols +"><a title='File name is " + sPDFFile + "' href='" + sPDFFile + "' class=o target=blank>" + sPDFFile + "</a></td></tr>";
	LOldPic.Text += "<tr><td colspan="+ m_nCols +">";

	LOldPic.Text += "<a title='back to item description page' href='p.aspx?"+ m_code +"' ";
	LOldPic.Text += " class=o>BACK</a></td></tr>";
	/*sPDFFile = GetProductPDFSrc(m_code);
	LOldPic.Text = "<b>Old PDF:</b> ";
	LOldPic.Text += m_pic;
	LOldPic.Text += "<br><img src=" + sPDFFile + ">";
	LOldPic.Text += "<br><a href="+ Request.ServerVariables["URL"] +"?kit=" + m_kit + "&code=" + m_code + "&t=da&file=" + HttpUtility.UrlEncode(sPDFFile);
	LOldPic.Text += " class=o>DELETE</a>";
	*/
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

void DoDelPDF()
{
	string file = Server.MapPath(Request.QueryString["file"]);
	File.Delete(file);
}

</script>
<br>
<table width=70% align=center valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">
<tr><td colspan=3>
<asp:label id=LTitle runat=server/>
</td></tr>
<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<tr>
<td><input id="filMyFile" type="file" runat="server">
<asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr>
<tr><td colspan=3>
<asp:Label id=LOldPic runat=server/>
</td></tr>
</FORM>

</table>

<asp:Label id=LFooter runat=server/>


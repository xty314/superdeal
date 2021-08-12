
<script runat=server>

string[] sletter = new string[500];
string m_upload_file = "";
string[] sfirst = new string[500];

//vin code
string stmpfile = "\\tmp.vin";
string sonTopPic = "\\"+"top.vin"; //default on top file name top.gif

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(Request.QueryString["t"] == "da")
	{
		DoDelPic();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editimg.aspx?\"");
		if(Request.QueryString["sl"] != null && Request.QueryString["sl"] != "")
			Response.Write("sl="+ Request.QueryString["sl"] +"&");
		Response.Write("r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	GetAllFirstLetterFile();
	Form1.Visible = true;
	ShowImages();
	LFooter.Text = m_sAdminFooter;
	
	//vin code at 28-4-2004
	if(Request.QueryString["d"] == "clr")
	{
		DoSwapPic(Request.QueryString["path"],Request.QueryString["src"]);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editimg.aspx?\"");
		return;
	}
}

void GetAllFirstLetterFile()
{
//	abcdefrghijklmnopqrstuvwxyz
//	int n = 63;
//	for(int i=0; i<26; i++)
//	{
//		sletter[i] = DecimalToChar(n++).ToString();
//	DEBUG("sletter=", sletter[0]);
//	}
	sletter[10] = "A"; sletter[22] = "M";sletter[0] = "0";
	sletter[11] = "B"; sletter[23] = "N";sletter[1] = "1";
	sletter[12] = "C";sletter[24] = "O";sletter[2] = "2";
	sletter[13] = "D";sletter[25] = "P";sletter[3] = "3";
	sletter[14] = "E";sletter[26] = "Q";sletter[4] = "4";
	sletter[15] = "F";sletter[27] = "R";sletter[5] = "5";
	sletter[16] = "G";sletter[28] = "S";sletter[6] = "6";
	sletter[17] = "H";sletter[29] = "T";sletter[7] = "7";
	sletter[18] = "I";sletter[30] = "U";sletter[8] = "8";
	sletter[19] = "J";sletter[31] = "V";sletter[9] = "9";
	sletter[20] = "K";sletter[32] = "W";sletter[34] = "Y";
	sletter[21] = "L";sletter[33] = "X";sletter[35] = "Z";


	Response.Write("<form name=frm method=post>");
	
	Response.Write("<br><table width=70% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>");
	Response.Write("<tr><td>");
	Response.Write("<b>SEARCH FILE NAME:</b> <i>(don't need to put the Extension)</i> <input type=text name=file_search ><input type=submit name=cmd value='SEARCH' "+ Session["button_style"] +">");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<b>Sorted by:</b>&nbsp; ");
for(int i=0; i<36; i++)
		Response.Write("<a title='Display Only Start with  "+ sletter[i] +"' href='"+ Request.ServerVariables["URL"] +"?sl="+ sletter[i] +"' class=o><font size=2>"+ sletter[i].ToUpper() +"</font></a> &nbsp;");	
Response.Write("<a title='display ALL' href='"+ Request.ServerVariables["URL"] +"?sl=all' class=o><font size=2>ALL</a>");	
	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	
}

bool ShowImages()
{
	string imgPath = "../i";
	string strPath = Server.MapPath(imgPath);
//DEBUG("strpath",strPath);
	if(!Directory.Exists(strPath))
		return false;

	strPath += "\\";

	StringBuilder sb = new StringBuilder();
	sb.Append("<table border=0><tr>");
	DirectoryInfo di = new DirectoryInfo(strPath);
	int n = 0;
	string sq = "";
	if(Request.QueryString["sl"] != null && Request.QueryString["sl"] != "" && Request.QueryString["sl"] != "all")
		sq = Request.QueryString["sl"].ToString();
	if(Request.Form["cmd"] == "SEARCH" || Request.Form["file_search"] != null && Request.Form["file_search"] != "")
		sq = Request.Form["file_search"];
	if(Request.QueryString["fi"] != null && Request.QueryString["fi"] != "")
		sq = Request.QueryString["fi"].ToString();
//DEBUG("sq = ", sq);
	int i = 0;
	foreach (FileInfo f in di.GetFiles(""+ sq +"*.*")) 
	{
		string ext = f.Extension.ToLower();
		if(ext != ".jpg" && ext != ".gif")
			continue;
//DEBUG("f =", sletter[n].ToString());
		
		string s = f.FullName;

		string width = "";
		string height = "";
		try
		{
			System.Drawing.Image im = System.Drawing.Image.FromFile(s);
			width = im.Width.ToString();
			height = im.Height.ToString();
			im.Dispose();
		}
		catch(Exception e)
		{
		}
		string file = s.Substring(strPath.Length, s.Length-strPath.Length);
//DEBUG("file=", file);
		string imgsrc = imgPath + "/" + file;
//DEBUG("imgsrc",imgsrc);

//		if(n == 0 || n == 3 || n == 6 || n == 9)
		sb.Append("</tr><tr>");
		sb.Append("<td valign=bottom><table><tr><td colspan=2>");
		sb.Append("<a title='view Larger Image' href=\"javascript:image_window=window.open('");
		sb.Append(imgsrc);
		sb.Append("', 'image_window', 'width=450, height=500, scrollbars=no,resizable=yes '); image_window.focus()\" >");
		//sb.Append("<img width=50 src='" + imgsrc +"'");
		sb.Append("<img src='" + imgsrc +"' title=" + width + "");
//		if(im.Width > 250)
//			Response.Write(" width=250");
		sb.Append(" border=0></a></td></tr>");
		sb.Append("<tr><td>" + f.Name + "</td>");
		sb.Append("<td>" + width + "x" + height + " " + (f.Length/1000).ToString() + "K ");
		if(f.Length > 20480)
			sb.Append(" <font color=red> * big file * </font>");
		sb.Append("</td>");
		sb.Append("<td align=right><a href=editimg.aspx?t=da&file=" + HttpUtility.UrlEncode(file));
		sb.Append(" class=o>DELETE</a></td></tr>");
		sb.Append("<tr><td>&nbsp;</td></tr></table></td>");
		n++;
	}
	sb.Append("</tr></table>");
	
	sb.Append("<a href=editimg.aspx?r=" + DateTime.Now.ToOADate() + " class=o>Back to View</a>");
//	Response.Write(sb.ToString());
	LOldPic.Text = "<b>" + n.ToString() + " Photo</b>";
	LOldPic.Text += sb.ToString();
	
	if(n > 0)
		return true;
	return false; //no pic
}

//purpose: put the company pic on the top of upload pic
//Author: Vincent 
//Date: 27-4-2004
void DoOnTopPic(string getpath, string sourcef, string topf)
{
	if(File.Exists(sourcef))
	{
		//vin code for on top image
		Bitmap oCounter;
		Graphics oGraphics;

		//load input image
		Bitmap objImage = new Bitmap(sourcef);
				//DEBUG("1.gif size:width",objImage.Size.Width.ToString());
				//DEBUG("1.gif size:height",objImage.Size.Height.ToString());
		
		//set size of output image
		int pw = objImage.Size.Width;
		int ph = objImage.Size.Height;
		oCounter = new Bitmap(pw,ph);
		oGraphics = Graphics.FromImage(oCounter);
		oGraphics.DrawImage(objImage,0,0);
		
		//load on top image
		if(!File.Exists(topf))
		{
			Bitmap pic = new Bitmap(pw, ph);
			Graphics g = Graphics.FromImage(pic);
			int size = System.Convert.ToInt32(pw/20);
			Font myFont = new Font("Courier New", size, FontStyle.Bold);
			
			//get text from site setting 
			string s_text = "";
			if ((s_text = GetSiteSettings("company_name")) == "")
				s_text = "Eden Computer WWW.EZNZ.COM";
			
			g.DrawString(s_text, myFont, new SolidBrush(Color.FromArgb(90,10,10,10)), 0, 0);
			pic.Save(getpath + "\\new.vin"); 
 			
			objImage = new Bitmap(getpath + "\\new.vin");
			pic.Dispose();

		}
		else
			objImage = new Bitmap(topf);
				//DEBUG("2.gif size:width",objImage.Size.Width.ToString());
				//DEBUG("2.gif size:height",objImage.Size.Height.ToString());
		
		int p2w = objImage.Size.Width;
		int p2h = objImage.Size.Height;
		
		//draw image
		oGraphics.DrawImage(objImage,pw/4,ph/2);

		
		//export image
		try
		{
			oCounter.Save(getpath + stmpfile);
		}
		catch(Exception e)
		{
		}

		//dispose
		objImage.Dispose();
		oCounter.Dispose();
		oGraphics.Dispose();
		//end
		if (File.Exists(getpath + "\\new.vin"))    
			File.Delete(getpath + "\\new.vin");

//DEBUG("src",sourcef);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editimg.aspx?d=clr&path=" + getpath + "&src=" + sourcef + " \" ");
	}
}

//vin code at 28-4-2004
void DoSwapPic(string getpath, string sourcef)
{
//DEBUG("src",sourcef);
	if (File.Exists(sourcef))
		try
		{
			File.Delete(sourcef);
		}
		catch(Exception e)
		{
			
		}
	if (File.Exists(getpath + stmpfile) && sourcef != "")    
		File.Move(getpath + stmpfile, sourcef);
}

void DoDelPic()
{
	string file = Server.MapPath("../i/" + Request.QueryString["file"]);
//DEBUG("file=", file);
	File.Delete(file);
}

// Processes click on our cmdSend button
void cmdSend_Click(object sender, System.EventArgs e)
{
	string strPath="";
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
			m_upload_file = strFileName;
			m_upload_file = m_upload_file.Replace(".jpg", "");
			m_upload_file = m_upload_file.Replace(".gif", "");
//DEBUG(" sfie =", m_upload_file);
//return;
			strPath = Server.MapPath("../i");
//			if(!Directory.Exists(strPath))
//				Directory.CreateDirectory(strPath);
			strPath += "\\";
			string purePath = strPath;
			strPath += strFileName;

//DEBUG("pathname=", strPath);

			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);
	
			//vin code doOnTopPic
//			DoOnTopPic(purePath,purePath+"\\"+strFileName,purePath+sonTopPic);
			
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editimg.aspx?\"");
			//if(Request.QueryString["sl"] != null && Request.QueryString["sl"] != "")
			//	Response.Write("sl="+ Request.QueryString["sl"] +"&");
			if(m_upload_file != "")
				Response.Write("fi="+ HttpUtility.UrlEncode(m_upload_file) +"&");
			Response.Write("r=" + DateTime.Now.ToOADate() + "\">");
		}
	}

	return;
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


</script>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<table width=70% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>
<tr><td><font size=+1 color=red><b>Attach Images</b></font><br>

<table><tr>
<td><input id="filMyFile" type="file" size=50 runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>

<br>
<asp:Label id=LOldPic runat=server/>
</td></tr></table>


</FORM>
<asp:Label id=LFooter runat=server/>
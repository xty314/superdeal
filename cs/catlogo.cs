<script runat=server>

string s_cat = "";
string m_id = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	SecurityCheck("");

	RememberLastPage();
	
	if(Request.QueryString["s"] != null)
		s_cat = Request.QueryString["s"];

	if(Request.Form["cmd"] == "Update")
	{
		DoUpdateLink();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=catlogo.aspx?s=" + HttpUtility.UrlEncode(s_cat) + "&r=" + DateTime.Now.ToOADate() + "\">");
	}
	if(Request.QueryString["t"] == "ca")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Change Image</h3>");
		m_id = Request.QueryString["id"];
		Form1.Visible = true;
		BCancel.Text = "<input type=button value='Cancel' onclick=window.location=('catlogo.aspx?s=" + HttpUtility.UrlEncode(s_cat) + "') " + Session["button_style"] + ">";
		LFooter.Text = m_sAdminFooter;
		return;
	}
	else if(Request.QueryString["t"] == "da")
	{
		DoDelPic(Request.QueryString["file"]);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=catlogo.aspx?s=" + HttpUtility.UrlEncode(s_cat) + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(m_sCompanyName == "demo")// && Request.QueryString["np"] == null)
		PrintPatentInfomation("catlogo");

	GetCats();
	Form1.Visible = true;
	ShowImages();
	LFooter.Text = m_sAdminFooter;
}

bool GetCats()
{
	DataSet ds = new DataSet();
	string sc = " SELECT DISTINCT s_cat FROM catalog WHERE cat <> 'Brands' AND s_cat <> 'zzzOthers' ORDER BY s_cat ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "cats");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb = new StringBuilder();

	sb.Append("<select name=s onchange=\"window.location=('catlogo.aspx?s=' + this.options[this.selectedIndex].value)\" " + Session["scroll_style"] + ">");
	for(int i=0; i<ds.Tables["cats"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["cats"].Rows[i];
		string cat = dr["s_cat"].ToString();
		sb.Append("<option value='" + cat + "'");
		if(cat == s_cat)
			sb.Append(" selected");
		sb.Append(">" + cat + "</option>");

		if(s_cat == "" && i == 0)
			s_cat = cat;
	}
	sb.Append("</select>");

	LCats.Text = sb.ToString();
	return true;
}

bool ShowImages()
{
	if(s_cat == "")
		return true;

	DataSet ds = new DataSet();
	string sc = " SELECT * FROM cat_logo ";
	sc += " WHERE s_cat = '" + EncodeQuote(s_cat) + "' ";
	sc += " ORDER BY id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "cats");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<form action=catlogo.aspx?s=" + HttpUtility.UrlEncode(s_cat) + " method=post>");

	for(int i=0; i<ds.Tables["cats"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["cats"].Rows[i];
		string id = dr["id"].ToString();
		string pic_name = dr["pic_name"].ToString();
		string uri = dr["uri"].ToString();
		string title = dr["title"].ToString();
		string columns = dr["colspan"].ToString();
		string seq = dr["seq"].ToString();

		string imgPath = "/i";
		string strPath = Server.MapPath(imgPath);

		string iWidth = "0";
		string iHeight = "0";
		int nFileSize = 0;
		string s = strPath + "\\" + pic_name;
		if(File.Exists(s))
		{
			FileInfo f = new FileInfo(s);
			System.Drawing.Image im = System.Drawing.Image.FromFile(s);
			iWidth = im.Width.ToString();
			iHeight = im.Height.ToString();
			nFileSize = (int)f.Length;
			im.Dispose();
		}

		string file = pic_name;
//DEBUG("file=", file);
		string imgsrc = imgPath + "/" + file;
//		if(n == 0 || n == 3 || n == 6 || n == 9)
		sb.Append("<tr>");
		sb.Append("<td valign=bottom>");
		sb.Append("<table><tr><td colspan=2>");

			sb.Append("<table><tr><td>");
			sb.Append("<img src='" + imgsrc);
			sb.Append("' border=0></td>");
			sb.Append("<td valign=top>");

				sb.Append("<table>");
				sb.Append("<tr><td><b>File Name : </b></td><td>" + pic_name + "</td></tr>");
				if(nFileSize <= 0)
				{
					sb.Append("<tr><td colspan=2><font color=red><b>Bad file or file not exists</b></font></td></tr>");
				}
				else
				{
					sb.Append("<tr><td><b>File Size : </b></td><td>" + (nFileSize/1000).ToString() + "K ");
					if(nFileSize > 204800)
						sb.Append(" <font color=red> * big file * </font>");
					sb.Append("</td></tr>");
					sb.Append("<tr><td><b>Image Size : </b></td><td>" + iWidth + "x" + iHeight + "</td></tr>");
				}
				sb.Append("</table>");

			sb.Append("</td></tr>");
			sb.Append("</table>");
		
		sb.Append("</td></tr>");
		sb.Append("<tr><td>");

		sb.Append("<b>Title : </b>");
		sb.Append("<textarea cols=40 rows=3 name=title" + i + ">" + title + "</textarea>");
		sb.Append("</td><td><b>Link : </b>");
		sb.Append("<textarea cols=40 rows=3 name=uri" + i + ">" + uri + "</textarea>");
		sb.Append("<input type=hidden name=pic_name" + i + " value='" + pic_name + "'>");
		sb.Append("</td></tr>");

		sb.Append("<tr><td>");
		sb.Append("<b>Columns Needs : </b><input type=text size=3 name=columns" + i + " value='" + columns + "'> &nbsp&nbsp; ");
		sb.Append("<b>Sort Order : </b><input type=text size=3 name=seq" + i + " value='" + seq + "'>");
		sb.Append("</td><td nowrap align=right>");
		sb.Append("<input type=button onclick=window.location=('catlogo.aspx?t=ca&id=" + id);
		sb.Append("&s=" + HttpUtility.UrlEncode(s_cat));
		sb.Append("') value='Change Picture' " + Session["button_style"] + ">");
		sb.Append("<input type=submit name=cmd value=Update " + Session["button_style"] + ">");
		sb.Append("<input type=button onclick=window.location=('catlogo.aspx?t=da&file=" + HttpUtility.UrlEncode(file));
		sb.Append("&s=" + HttpUtility.UrlEncode(s_cat));
		sb.Append("') value=Delete " + Session["button_style"] + ">");
		sb.Append("</td></tr>");

		sb.Append("<tr><td>&nbsp;</td></tr>");
		sb.Append("</table>");
		sb.Append("</td></tr>");
	}
//	sb.Append("<tr><td colspan=2 align=right>");
//	sb.Append("<input type=submit name=cmd value=Update " + Session["button_style"] + ">");
//	sb.Append("</td></tr>");
	sb.Append("</form>");

	LOldPic.Text = sb.ToString();

	return true;
}

bool DoDelPic(string fileName)
{
	string file = Server.MapPath("/i/" + fileName);
//DEBUG("file=", file);
	File.Delete(file);

	string sc = " DELETE FROM cat_logo WHERE pic_name = '" + EncodeQuote(fileName) + "' ";
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
			string strPath = Server.MapPath("/i");
//			if(!Directory.Exists(strPath))
//				Directory.CreateDirectory(strPath);
			strPath += "\\";
			string purePath = strPath;
			strPath += strFileName;
			
//DEBUG("pathname=", strPath);

			// Write data into a file, overwrite if exists
			WriteToFile(strPath, ref myData);

			//insert record
			string sc = "";
			if(m_id != "") //do update
			{
				sc = " UPDATE cat_logo SET pic_name = '" + EncodeQuote(strFileName) + "' WHERE id=" + m_id;
			}
			else
			{
				sc = " INSERT INTO cat_logo (s_cat, pic_name) ";
				sc += " VALUES('" + EncodeQuote(s_cat) + "', '" + strFileName + "') ";
			}
//DEBUG("sc=", sc);
//Response.End();
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
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=catlogo.aspx?s=" + HttpUtility.UrlEncode(s_cat) + "&r=" + DateTime.Now.ToOADate() + "\">");
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

bool DoUpdateLink()
{
	int i = 0;
	int j = 0; //dead loop protection
	string sc = "";
	while(Request.Form["uri" + i] != null && j++<1000)
	{
		string uri = Request.Form["uri" + i];
		string colspan = Request.Form["columns" + i];
		string seq = Request.Form["seq" + i];
		string title = Request.Form["title" + i];
		string pic_name = Request.Form["pic_name" + i];
		i++;

		sc += " UPDATE cat_logo SET uri = '" + EncodeQuote(uri) + "' ";
		sc += ", title='" + EncodeQuote(title) + "' ";
		sc += ", colspan='" + EncodeQuote(colspan) + "' ";
		sc += ", seq='" + EncodeQuote(seq) + "' ";
		sc += " WHERE pic_name = '" + pic_name + "' ";
	}
//DEBUG("sc=", sc);
//Response.End();
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
</script>


<br>
<table width=70% align=center cellspacing=1 cellpadding=3 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>
<tr><td align=center><font size=+1 color=red><b>Logo/Image Manage</b></font><br><br>

	<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
	<table>
	<tr>
	<td><asp:Label id=LCats runat=server/></td>
	<td><input id="filMyFile" type="file" size=70 runat="server" style="font-size:8pt;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696"></td>
	<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload" style="font-size:8pt;font-weight:bold;background-color:#EEEEEE;color:#444444;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696"/></td>
	<td><asp:Label id=BCancel runat=server/></td>
	</tr>
	</FORM>
	</table>

<br>
</td></tr>

<tr><td>
	<asp:Label id=LOldPic runat=server/>
</td></tr>

</table>


<asp:Label id=LFooter runat=server/>
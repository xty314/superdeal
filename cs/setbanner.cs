<!-- #include file="kit_fun.cs" -->
<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	InitKit();

	if(Request.QueryString["t"] == "da")
	{
		DoDelPic();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=setbanner.aspx\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	GetOldData(); //display old photo if exists, otherwise insert new row, prepare to upload
	Form1.Visible = true;

	LFooter.Text = m_sAdminFooter;
}

void cmdSend_Click(object sender, System.EventArgs e)
{
	if( filMyFile.PostedFile != null )
	{
		HttpPostedFile myFile = filMyFile.PostedFile;
		int nFileLen = myFile.ContentLength; 

		if(nFileLen > 0)
		{
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);

			string vpath = "";
			vpath += "../pic/banner/images/";
			string strPath = Server.MapPath(vpath);

			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);

			strPath += System.IO.Path.GetFileName(myFile.FileName); //DateTime.Now.ToOADate().ToString() + ".jpg";
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?\">");
		}
	}
}

void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}

bool GetOldData()
{
	string sBannerPicFile = "";
	string path = "../pic/banner/images/";
	if(Directory.Exists(Server.MapPath(path)))
	{
		DirectoryInfo di = new DirectoryInfo(Server.MapPath(path));
		foreach(FileInfo f in di.GetFiles("*.*"))
		{
			if(f.Name.IndexOf(".db") >= 0)
				continue;
			sBannerPicFile = path + f.Name;
			LOldPic.Text += "<div style=\"text-align:center; margin-left:10px; margin-top:10px;\"><img style=\"width:960px;height:390px;\" src='" + sBannerPicFile + "'>";
			LOldPic.Text += "<br>" + f.Name;
			LOldPic.Text += "<br><a href=setbanner.aspx?t=da&file=" + HttpUtility.UrlEncode(sBannerPicFile);
			LOldPic.Text += " class=o>DELETE</a><hr style=\"width:960px;\"></div>";
		}
	}
	return true;
}

void DoDelPic()
{
	string file = Server.MapPath(Request.QueryString["file"]);
	File.Delete(file);
}

</script>

<asp:label id=LTitle runat=server/>
<center style="margin-bottom:10px;">
	<h2 style="margin-bottom:5px;"><b>Set Banner Picture</b></h2>
	<span>(Size : 960 x 390)</span>
</center>
<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<div><table align=center><tr>
<td><input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table></div><hr style="width:960px;">

<br>
<asp:Label id=LOldPic runat=server/>

</FORM>
<asp:Label id=LFooter runat=server/>
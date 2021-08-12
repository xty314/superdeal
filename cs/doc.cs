<script runat=server>

DataSet dst = new DataSet();
string m_id = "";
string m_kid = "";
string m_fileName = "";
string m_fileType = "";
string m_cat = "";

bool m_bSearching = false;

void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	if(Request.QueryString["showp"] == "1")
		Session["doc_expand_private"] = true;
	else if(Request.QueryString["showp"] == "0")
		Session["doc_expand_private"] = null;
	if(Request.QueryString["shows"] == "1")
		Session["doc_expand_shared"] = true;
	else if(Request.QueryString["shows"] == "0")
		Session["doc_expand_shared"] = null;

	string t = Request.QueryString["t"];
	if(t == "savecat")
	{
		if(SaveCatInfo())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?id=" + m_id + "\">");
		return;
	}
	else if(t == "delete")
	{
		if(DoDeleteDoc())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?" + "\">");
		return;
	}
	else if(t == "preview")
	{
		DoPreviewDoc();
		return;
	}
	else if(t == "download")
	{
		DoDownloadDoc();
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h3>Document Center</h3>");
	LFooter.Text = m_sAdminFooter;

	if(t == "cat" || t == "regroup")
	{
		PrintCatForm();
		return;
	}
	else if(t == "upload")
	{
		Form1.Visible = true;
		return;
	}
	else if(t == "update")
	{
		Form2.Visible = true;
		return;
	}
	else if(Request.Form["kw"] != null) //search
	{
		DoDocSearch();
		m_bSearching = true;
		PrintDocList();
		return;
	}
	else
	{
		GetAllDocs();
		PrintDocList();
		Form1.Visible = true;
		return;
	}
}

bool DoDeleteDoc()
{
	m_kid = Request.QueryString["kid"];
	if(m_kid == null || m_kid == "")
		return true;
	m_id = Request.QueryString["id"];
	if(m_id == null || m_id == "")
		return true;

	string sc = " UPDATE doc_data SET deleted=1, deleted_by=" + Session["card_id"].ToString() + " WHERE kid = " + m_kid;
	//delete doc name if all version deleted
	sc += " IF NOT EXISTS (SELECT * FROM doc_data WHERE id=" + m_id + ") ";
	sc += " BEGIN ";
	sc += " DELETE FROM doc_name WHERE id = " + m_id;
	sc += " END ";
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
	return true;
}

bool DoDocSearch()
{
	string kw = Request.Form["kw"];
	string sc = " SELECT n.*, d.kid, d.data, d.time_updated, c.name AS owner_name, c1.name AS name_locked_by ";
	sc += " FROM doc_name n ";
	sc += " JOIN doc_data d ON d.id = n.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = n.owner ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = n.locked_by ";
	sc += " WHERE n.name LIKE '%" + kw + "%' OR FREETEXT(d.data, '" + kw + "') ";
	sc += " ORDER BY n.private DESC, n.cat, n.name, d.time_updated DESC ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "docs");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetAllDocs()
{
	string sc = " SELECT n.*, d.kid, d.data, d.time_updated, c.name AS owner_name, c1.name AS name_locked_by ";
	sc += " FROM doc_name n ";
	sc += " JOIN doc_data d ON d.id = n.id ";
	sc += " LEFT OUTER JOIN card c ON c.id = n.owner ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = n.locked_by ";
	sc += " WHERE d.deleted = 0 AND (n.owner = " + Session["card_id"].ToString() + " OR n.private=0 ) ";
	sc += " ORDER BY n.private DESC, n.cat, n.name, d.time_updated DESC ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "docs");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintDocList()
{
	if(dst.Tables["docs"] == null)
		return;
	int nDocs = dst.Tables["docs"].Rows.Count;

	Response.Write("<table width=80% cellspacing=1 cellpadding=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<form id=frmsearch action=? method=post>");
	Response.Write("<tr><td colspan=5><input type=text name=kw value='" + Session["doc_search"] + "'>");
	Response.Write("<input type=submit name=cmd value=Search class=b>");
	if(m_bSearching)
		Response.Write("<input type=button onclick=window.location=('doc.aspx') class=b value='Back to List'>");
	Response.Write("</form></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td>&nbsp;</td><td>Group</td><td>Name</td><td>LastUpdated</td><td>Size</td><td>Locked</td><td>Download</td>");
	Response.Write("</tr>");

	bool bExpandPrivate = false;
	bool bExpandShared = false;
	if(Session["doc_expand_private"] != null)
		bExpandPrivate = true;
	if(Session["doc_expand_shared"] != null)
		bExpandShared = true;
	bool bPrivate = false;
	bool bBeginShared = false;
	bool bAlterColor = false;
//	string old_cat = "-1";
	string old_catid = "-1";
	string old_id = "-1";
	for(int i=0; i<nDocs; i++)
	{
		DataRow dr = dst.Tables["docs"].Rows[i];
		bPrivate = MyBooleanParse(dr["private"].ToString());

		if(!m_bSearching)
		{
			if(i==0 && bPrivate)
			{
				Response.Write("<tr><td colspan=6>");
				if(bExpandPrivate)
					Response.Write("<a href=?showp=0><font color=red><b>- ");
				else
					Response.Write("<a href=?showp=1><font color=red><b>+ ");
				Response.Write("Private Documents</b></font></a></td></tr>");
			}
			if(!bPrivate && !bBeginShared)
			{
				bBeginShared = true;
				Response.Write("<tr><td colspan=6>");
				if(bExpandShared)
					Response.Write("<a href=?shows=0><font color=green><b>- ");
				else
					Response.Write("<a href=?shows=1><font color=green><b>+ ");
				Response.Write("Shared Documents</b></font></a></td></tr>");
			}

			if(bPrivate && !bExpandPrivate)
				continue;

			if(!bPrivate && !bExpandShared)
				continue;
		}

		string id = dr["id"].ToString();
		string kid = dr["kid"].ToString();
		string cat = dr["cat"].ToString();
		string catid = "doc_exp_private_"; //private catid
		if(bBeginShared)
			catid = "doc_exp_shared_";
		catid += cat;

		if(Request.QueryString["showcat"] == id)
			Session[catid] = true;
		else if(Request.QueryString["hidecat"] == id)
			Session[catid] = null;

		if(!m_bSearching)
		{
			if(Session[catid] == null)
			{
				if(old_catid != catid)
				{
					Response.Write("<tr><td colspan=5><b> &nbsp; ");
					Response.Write("<a href=?showcat=" + id + ">+ " + cat);
					Response.Write("</b></a></td></tr>");
				}
				old_catid = catid;
				continue;
			}
			else
			{
				if(old_catid != catid)
				{
					Response.Write("<tr><td colspan=6><b> &nbsp; ");
					Response.Write("<a href=?hidecat=" + id + ">- " + cat);
					Response.Write("</b></a></td></tr>");
				}
				old_catid = catid;
			}
		}

		string name = dr["name"].ToString();
		string size = ((byte[])dr["data"]).Length / 1000 + "k";
		string last_updated = DateTime.Parse(dr["time_updated"].ToString()).ToString("dd-MM-yyyy");
		string owner = dr["owner_name"].ToString();
		bool bLocked = MyBooleanParse(dr["locked"].ToString());
		string time_locked = "";
		string locked_by = "";
		string locked_id = "";
		if(bLocked)
		{
			time_locked = DateTime.Parse(dr["time_locked"].ToString()).ToString("ddMMyyyy HH:mm");
			locked_by = dr["name_locked_by"].ToString();
			locked_id = dr["locked_by"].ToString();
		}

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

/*		Response.Write("<td> &nbsp; <a href=?t=expand&id=" + id + ">+</a></td>"); //expand

		if(old_cat == cat)
			Response.Write("<td>&nbsp;</td>");
		else
		{
			old_cat = cat;
			Response.Write("<td>" + cat + "</td>");
		}
*/
		string idsign = " &nbsp;&nbsp; ";
		bool bExpandID = false;
		if(Request.QueryString["id"] == id) //do expand
			bExpandID = true;
		if(i < nDocs-1)
		{
			if(id == dst.Tables["docs"].Rows[i+1]["id"].ToString())
			{
				if(bExpandID)
					idsign = "<a href=? title='Show Lastest Version Only'><b>-&nbsp;";
				else
					idsign = "<a href=?id=" + id + " title='Show All Versions'><b>+&nbsp;";
			}
		}
		if(id == old_id)
		{
			if(!bExpandID)
				continue;
		}
		old_id = id;

		//no expand link for search result
		if(m_bSearching)
		{
			idsign = " &nbsp;&nbsp; ";
			Response.Write("<td colspan=2>");
			if(bPrivate)
				Response.Write("Private->");
			else
				Response.Write("Shared->");
			Response.Write(cat + "</td>");
		}
		else
		{
			Response.Write("<td colspan=2>&nbsp;</td>");
		}

		Response.Write("<td>" + idsign + name + "</a></td>");
		Response.Write("<td>" + last_updated + "</td>");
		Response.Write("<td align=right>" + size + "</td>");
		if(!bLocked)
		{
			Response.Write("<td>&nbsp;</td>");
		}
		else
		{
			string strlock = "<font color=red>Locked By " + locked_by + " " + time_locked + "</font>";
			Response.Write("<td>" + strlock + "</td>");
		}

		Response.Write("<td><a href=?t=download&id=" + id + " class=o target=_blank>Download</a> ");
		Response.Write("<a href=?t=preview&id=" + id + " class=o target=_blank>Preview</a> ");
		Response.Write("<a href=?t=regroup&id=" + id + " class=o>Regroup</a> ");
		if(bLocked && locked_by == Session["card_id"].ToString())
			Response.Write("<a href=?t=unlock&id=" + id + " class=o>Unlock</a> ");
		else
		{
			Response.Write("<a href=?t=update&id=" + id + " class=o>Update</a> ");
			Response.Write("<a href=?t=delete&id=" + id + "&kid=" + kid + " class=o>Delete</a> ");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

//new document
void cmdSend_Click(object sender, System.EventArgs e)
{
	if(filMyFile.PostedFile != null)
	{
		HttpPostedFile myFile = filMyFile.PostedFile;
		int nFileLen = myFile.ContentLength; 
		if( nFileLen > 0 )
		{
			m_fileName = Path.GetFileName(myFile.FileName);
			m_fileName = m_fileName.ToLower();
			m_fileType = myFile.ContentType;
//DEBUG("type=", m_fileType);
//return;
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);
			if(SaveNewDocument(m_fileName, myData))
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
				Response.Write("URL=?t=cat&id=" + m_id + "&kid=" + m_kid);
				Response.Write("&fn=" + HttpUtility.UrlEncode(m_fileName) + "\">");
			}
		}
	}
}

bool SaveNewDocument(string fileName, byte[] buffer)
{
	//get new doc id
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO doc_name (name, owner) ";
	sc += " VALUES(N'" + EncodeQuote(fileName) + "', " + Session["card_id"].ToString();
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('doc_name') AS id "; 
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "id") == 1)
		{
			m_id = dst.Tables["id"].Rows[0]["id"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//save data
	sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO doc_data (id, who_updated, fileType) ";
	sc += " VALUES(" + m_id + ", " + Session["card_id"].ToString() + ", '" + EncodeQuote(m_fileType) + "' ";
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('doc_data') AS kid "; 
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "kid") == 1)
		{
			m_kid = dst.Tables["kid"].Rows[0]["kid"].ToString();
			sc = " UPDATE doc_data SET data=@data WHERE kid=" + m_kid;
			try
			{
				myCommand = new SqlCommand(sc);
				SqlParameter param0 = new SqlParameter("@data", SqlDbType.Image);
				param0.Value = buffer;
				myCommand.Parameters.Add(param0);
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
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintCatForm()
{
	m_id = Request.QueryString["id"];
	m_kid = Request.QueryString["kid"];
	m_fileName = Request.QueryString["fn"];
	if(m_id != "")
	{
		if(!GetOneDoc())
			return false;
	}
	DataRow dr = dst.Tables["preview"].Rows[0];
	if(m_fileName == null || m_fileName == "")
		m_fileName = dr["name"].ToString();
	else
		Response.Write("<h4>File Uploaded</h4>");
	m_cat = dr["cat"].ToString();
	bool bPrivate = false;
	if(MyBooleanParse(dr["private"].ToString()))
		bPrivate = true;

	Response.Write("<font color=red size=+1>Document Property : </font><br>");
	Response.Write("<form action=?t=savecat&id=" + m_id + "&kid=" + m_kid + " method=post>");
	Response.Write("<table border=1 cellspacing=3 cellpadding=0 bordercolor=#888888 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><b>File Name : </b> &nbsp; </td><td>" + m_fileName + "</td></tr>");
	Response.Write("<tr><td><b>Group : </b> &nbsp; </td><td>");
	if(!PrintCatSelection())
		return false;
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Private : </b> &nbsp; </td><td><input type=checkbox name=private");
	if(bPrivate)
		Response.Write(" checked");
	Response.Write("> <b>Yes</b></td></tr>");
//	Response.Write("<tr><td valign=top><b>Summary : </b></td>");
//	Response.Write("<td><textarea name=summary rows=5 cols=50></textarea></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Save class=b></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool SaveCatInfo()
{
	m_id = Request.QueryString["id"];
//	m_kid = Request.QueryString["kid"];

	string cat = Request.Form["cat"];
	if(Request.Form["cat_new"] != "")
		cat = Request.Form["cat_new"];
//	string summary = Request.Form["summary"];
	string sbprivate = "0";
	if(Request.Form["private"] == "on")
		sbprivate = "1";

	string sc = " UPDATE doc_name SET cat='" + EncodeQuote(cat) + "', private=" + sbprivate + " WHERE id=" + m_id;
//	sc += " UPDATE doc_data SET summary='" + EncodeQuote(summary) + "' WHERE kid=" + m_kid;
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
	return true;
}

bool PrintCatSelection()
{
	m_id = Request.QueryString["id"];
	if(Request.QueryString["kid"] != null && Request.QueryString["kid"] != "")
		m_kid = Request.QueryString["kid"];

	string sc = " SELECT DISTINCT cat FROM doc_name ORDER BY cat";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		myCommand1.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<select name=cat>");
	for(int i=0; i<dst.Tables["cat"].Rows.Count; i++)
	{
		string cat = dst.Tables["cat"].Rows[i]["cat"].ToString();
		if(cat == "")
			continue;
		Response.Write("<option value='" + cat + "'");
		if(cat == m_cat)
			Response.Write(" selected");
		Response.Write(">" + cat + "</option>");
	}
	Response.Write("</select><input type=text size=10 name=cat_new>");
	return true;
}

bool GetOneDoc()
{
	if(m_id == "")
	{
		m_id = Request.QueryString["id"];
		if(m_id == null || m_id == "")
			return false;
	}

	string sc = " SELECT top 1 n.name, n.cat, n.private, d.summary, d.data, d.fileType ";
	sc += " FROM doc_name n JOIN doc_data d ON d.id = n.id ";
	sc += " WHERE n.id = " + m_id;
	sc += " ORDER BY d.kid DESC ";
	if(m_kid != null && m_kid != "")
	{
		sc = " SELECT n.name, n.cat, n.private, d.summary, d.data, d.fileType ";
		sc += " FROM doc_data d JOIN doc_name n ON n.id = d.id ";
		sc += " WHERE d.kid = " + m_kid;
	}
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "preview") <= 0)
		{
			PrintAdminHeader();
			Response.Write("<h3>No Record Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoPreviewDoc()
{
	if(!GetOneDoc())
		return false;
	
	DataRow dr = dst.Tables["preview"].Rows[0];
	string name = dr["name"].ToString();
	m_fileType = dr["fileType"].ToString();
	if(m_fileType.IndexOf("text") >= 0)
	{
		string sout = "";
		Encoding enc = Encoding.GetEncoding("iso-8859-1");
		char[] buffer = enc.GetChars((byte[])dr["data"]);
		for(int i=0; i<buffer.Length; i++)
		{
			if(i > 10240)
				break;
			if(buffer[i] == '\r')
			{
				sout += "<br>";
				continue;
			}
//			if(buffer[i] < 32 || buffer[i] > 126) //none readable
//				continue;
			sout += buffer[i];
		}
		Response.Write("<br><center><h3>" + name + "</h3></center>");
		Response.Write(sout);
	}
	else
	{
		Response.Clear();
		Response.AddHeader("Content-Type",dr["fileType"].ToString());
		Response.BinaryWrite((byte[])dr["data"]);
	}

	return true;
}

bool DoDownloadDoc()
{
	m_id = Request.QueryString["id"];
	if(Request.QueryString["kid"] != null && Request.QueryString["kid"] != "")
		m_kid = Request.QueryString["kid"];

	string sc = " SELECT top 1 n.name, d.data, d.fileType ";
	sc += " FROM doc_name n JOIN doc_data d ON d.id = n.id ";
	sc += " WHERE n.id = " + m_id;
	sc += " ORDER BY d.kid DESC ";
	if(m_kid != "")
	{
		sc = " SELECT n.name, d.data, d.fileType ";
		sc += " FROM doc_data d JOIN doc_name n ON n.id = d.id ";
		sc += " WHERE d.kid = " + m_kid;
	}
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "preview") <= 0)
		{
			PrintAdminHeader();
			Response.Write("<h3>No Record Found</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	DataRow dr = dst.Tables["preview"].Rows[0];
	string name = dr["name"].ToString();
	byte[] buffer = (byte[])dr["data"];
	
	string strPath = Server.MapPath(".") + "\\temp\\" + Session["name"].ToString();
	if(Directory.Exists(strPath))
		Directory.Delete(strPath, true);
	Directory.CreateDirectory(strPath);

	strPath += "\\" + name;

	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(buffer, 0, buffer.Length);
	newFile.Close();

	string link = "temp/" + Session["name"].ToString() + "/" + name;
	Response.Write("<a href='" + link + "' class=o>" + name + "</a>");

	return true;
}

//update document
void cmdSend_Click2(object sender, System.EventArgs e)
{
	if(filMyFile2.PostedFile != null)
	{
		HttpPostedFile myFile = filMyFile2.PostedFile;
		int nFileLen = myFile.ContentLength; 
		if( nFileLen > 0 )
		{
			m_fileName = Path.GetFileName(myFile.FileName);
			m_fileName = m_fileName.ToLower();
			m_fileType = myFile.ContentType;
//DEBUG("type=", m_fileType);
//return;
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);
			if(SaveNewVersion(m_fileName, myData))
			{
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; ");
				Response.Write("URL=?id=" + m_id + "&kid=" + m_kid);
				Response.Write("&fn=" + HttpUtility.UrlEncode(m_fileName) + "\">");
			}
		}
	}
}

bool SaveNewVersion(string fileName, byte[] buffer)
{
	string m_id = Request.QueryString["id"];
	if(m_id == null || m_id == "")
	{
		Response.Write("<br><center><h3>Error, no ID</h3></center>");
		return false;
	}

	//save data
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO doc_data (id, who_updated, fileType) ";
	sc += " VALUES(";
	sc += m_id + ", " + Session["card_id"].ToString() + ", '" + EncodeQuote(m_fileType) + "' ";
	sc += ") ";
	sc += " SELECT IDENT_CURRENT('doc_data') AS kid "; 
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "kid") == 1)
		{
			m_kid = dst.Tables["kid"].Rows[0]["kid"].ToString();
			sc = " UPDATE doc_data SET data=@data WHERE kid=" + m_kid;
			try
			{
				myCommand = new SqlCommand(sc);
				SqlParameter param0 = new SqlParameter("@data", SqlDbType.Image);
				param0.Value = buffer;
				myCommand.Parameters.Add(param0);
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
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}


</script>

<asp:label id=LTitle runat=server/>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<h4>Upload New File</h4>
<table><tr>
<td><input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>

</FORM>

<form id="Form2" method="post" runat="server" enctype="multipart/form-data" visible=false>
<h4>Upload Updated File</h4>
<table><tr>
<td><input id="filMyFile2" type="file" runat="server"></td><td>&nbsp;</td>
<td><asp:button id="cmdSend2" runat="server" OnClick="cmdSend_Click2" Text="Upload"/></td>
</tr></table>

</FORM>

<asp:Label id=LFooter runat=server/>
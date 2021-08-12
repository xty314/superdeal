<script runat=server>

const string cols = "5";	//how many columns main table has, used to write colspan=
const string tableTitle = "Edit Sub Catalog";
const string thisurl = "escat.aspx";
const string sFieldName = "s_cat"; //for searching catalogs for select box

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	GetQueryStrings();

	string sc = "";
	if(brand != null)
		sc = "brand='" + brand + "' ORDER BY " + sFieldName;
	else if(cat != "")
		sc = "cat='" + cat + "' ORDER BY " + sFieldName;

	if(!ECATGetAllExistsValues(sFieldName, sc))
		return ;

	PrintAdminHeader();
	PrintAdminMenu();
	WriteHeaders();

	if(m_type == "update")
	{
		string update = Request.Form["update"];
		if(UpdateAllRows())
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);
			string s = "<br><br>done! wait a moment......... <br>\r\n";
			s += "<meta http-equiv=\"refresh\" content=\"1; URL=";
			s += WriteURLWithoutPageNumber();
			s += "&p=";
			s += page;
			s += "\"></body></html>";
			Response.Write(s);
		}
	}
	else
	{
		if(!DoSearch())
			return;
	
		MyDrawTable();
	}
	WriteFooter();
	PrintAdminFooter();
}

Boolean UpdateOneRow(string sRow)
{
	Boolean bRet = true;

	string code		= Request.Form["code"+sRow];
	string id		= Request.Form["id"+sRow];
	string s_cat	= Request.Form["s_cat"+sRow];
	if(Request.Form["s_cat_new"+sRow] != "")
		s_cat = Request.Form["s_cat_new"+sRow]; //new value entered

	Trim(ref s_cat);

	//update product (live update)
	StringBuilder sb = new StringBuilder();
	sb.Append("UPDATE product SET s_cat='");
	sb.Append(s_cat);
	sb.Append("' WHERE code=");
	sb.Append(code);
	try
	{
//DEBUG("sc=", sb.ToString());
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}
	
	sb.Remove(0, sb.Length);
	sb.Append("UPDATE code_relations SET s_cat='");
	sb.Append(s_cat);
	sb.Append("' WHERE code=");
	sb.Append(code);
	try
	{
		myCommand = new SqlCommand(sb.ToString());
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp(sb.ToString(), e);
		return false;
	}

	return bRet;
}


void DrawTableHeader()
{
	StringBuilder sb = new StringBuilder();;
	sb.Append("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("<td width=50>code</td>\r\n");
	sb.Append("<td>name (description)</td>\r\n");
	sb.Append("<td>Current</td>");
	sb.Append("<td>Enter New Value</td>");
	sb.Append("</tr>\r\n");
	
	Response.Write(sb.ToString());
	Response.Flush();
}

Boolean DrawRow(DataRow dr, int i, Boolean alterColor)
{
	string code = dr["code"].ToString();
	string id = dr["id"].ToString();
	string name = dr["name"].ToString();
	string brand = dr["brand"].ToString();
	string s_cat = dr["s_cat"].ToString();
	string index = i.ToString();

	if(!CheckCodeRelations(id, dr)) //if id is blank, then delete this from product table
		return false;

	StringBuilder sb = new StringBuilder();;
	
	sb.Append("<input type=hidden name=id");
	sb.Append(index);
	sb.Append(" value='");
	sb.Append(id);
	sb.Append("'>");

	sb.Append("<input type=hidden name=code");
	sb.Append(index);
	sb.Append(" value='");
	sb.Append(code);
	sb.Append("'>");

	sb.Append("<tr");
	if(alterColor)
		sb.Append(" bgcolor=#EEEEEE");
	sb.Append("><td>");
	sb.Append(code);
	sb.Append("</td><td>");
	sb.Append(name);
	sb.Append("</td>");

	sb.Append("<td><select name=s_cat");
	sb.Append(index);
	sb.Append(">");
	
	string str;
	for(int j=0; j<dsAEV.Tables[sFieldName].Rows.Count; j++)
	{
		str = dsAEV.Tables[sFieldName].Rows[j][0].ToString();
		sb.Append("<option value='");
		sb.Append(str);
		sb.Append("'");
		if(str == s_cat)
			sb.Append(" selected");
		sb.Append(">");
		sb.Append(str);
		sb.Append("</option>");
	}
	sb.Append("</select></td><td>");
	sb.Append("<input type=text size=10 name=s_cat_new");
	sb.Append(index);
	sb.Append(">");
	sb.Append("</td></tr>");

	Response.Write(sb.ToString());
	Response.Flush();
	return true;
}
</script>
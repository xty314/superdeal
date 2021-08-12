<script runat=server>

string m_page = "";
string m_cat = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["p"] != null)
		m_page = Request.QueryString["p"];
	
	if(Request.Form["cmd"] != null)
	{
		if(Request.Form["cmd"] == "Delete")
		{
			if(Request.Form["del_confirm"] != "on")
			{
				Response.Write("<br><br><center><h3>Please tick confirm box to delete</h3>");
				return;
			}
			SaveSitePage(Request.Form["page_id"], "", "", ""); //two blank field means delete
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editpage.aspx?r=" + DateTime.Now.ToOADate() + "\">");
		}
		else
		{
			string text = Request.Form["txt"];
//			text = text.Replace("nbsp", "&nbsp");
			string cat = Request.Form["cat"];
			if(Request.Form["cat_new"] != "")
				cat = Request.Form["cat_new"];
			if(!SaveSitePage(Request.Form["page_id"], EncodeQuote(Request.Form["page_name"]), EncodeQuote(text), EncodeQuote(cat)))
				return;
			TSRemoveCache(m_sCompanyName + m_sSite + "_" + m_sHeaderCacheName);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=editpage.aspx?p=" + HttpUtility.UrlEncode(Request.Form["page_name"]) + "\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br>");
	
	if(m_page == "")
	{
		DataSet ds = new DataSet();
		int rows = 0;
		string sc = "SELECT zcat=CASE cat WHEN '' THEN 'zzzOthers' ELSE cat END ";
		sc += ", name FROM site_pages ";
		sc += " WHERE name <> 'new_page' ";
		sc += " ORDER BY zcat, name";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(ds);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}

		Response.Write("<center><h4><b>Select Page To Edit</b></h4>");
		//Response.Write("<table align=center>");
		Response.Write("<table align=center valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		bool bAlt = true;
		string cat_old = "-1";
		bool bNewLine = false;
		for(int i=0; i<rows; i++)
		{
			bNewLine = !bNewLine;
			string p = ds.Tables[0].Rows[i]["name"].ToString();
			string cat = ds.Tables[0].Rows[i]["zcat"].ToString();
			if(cat != cat_old)
			{
				cat_old = cat;
				if(cat == "zzzOthers")
					cat = "Other";

				Response.Write("<tr><td colspan=2><b>" + cat + " Pages : </b></td></tr>");
				bNewLine = true;
			}
			//vin code
			string mcat = HttpUtility.UrlEncode(cat);
			string mname = HttpUtility.UrlEncode(p);

			//DEBUG("mod = ", Mod(i,2).ToString());
			if(bNewLine)
			{
				Response.Write("<tr");
				if(bAlt)
					Response.Write(" bgcolor='#EEEEEE' ");
				Response.Write(" >");
				Response.Write("<td>"+(i+1).ToString()+"</td><td><a href=siteoption.aspx?c=" + mcat +"&n=" + mname + " class=o>" + p + "</a>");			
			}
			else
			{
				bAlt = !bAlt;
				Response.Write("<td>"+(i+1).ToString()+"</td><td><a href=siteoption.aspx?c=" + mcat + "&n=" + mname + " class=o>" + p + "</a>");			
			}
		
			if(Session["email"].ToString().IndexOf("@eznz.com") >= 0)
				Response.Write(" <a href=editpage.aspx?p=" + HttpUtility.UrlEncode(p) + " title='Edit Default' class=o><font color=red>E</font></a>");
			Response.Write("</td>");
			if(!bNewLine)
				Response.Write("</tr>");

		}
		Response.Write("<tr><td colspan=4><br><a href=editpage.aspx?p=new_page class=o>Add New Page</a></td></tr>");
		Response.Write("</table>");
	}
	else
		PrintEditForm();
	LFooter.Text = m_sAdminFooter;
}

int Mod(int nStart, int nDivid)
{
	int nReturn = 0;
    nReturn = nStart-((nStart / nDivid) * nDivid);
	return nReturn;
}


void PrintEditForm()
{
	string id = "";
	string text = GetSitePageText(m_page, ref id, ref m_cat);
//	text = text.Replace("&nbsp", "nbsp");
	Response.Write("<form action=editpage.aspx?p=" + m_page + " method=post>");
	Response.Write("<input type=hidden name=page_id value=" + id + ">");
	Response.Write("<center><h3>Page Edit - <font color=red>" + m_page.ToUpper() + "</font></h3>");
	Response.Write("<table border=1>");
	Response.Write("<tr>");
//	Response.Write("<td><b>NAME &nbsp&nbsp;</b></td>");
	Response.Write("<td><b>Name : </b><input type=text name=page_name size=30 value=" + m_page + ">");
	Response.Write("<b>&nbsp&nbsp; Catalog : </b><select name=cat>");
	Response.Write(PrintSitePageCats(m_cat));
	Response.Write("<input type=text name=cat_new size=20></td>");
	Response.Write("</tr><tr>");
//	Response.Write("<td valign=top><b>TEXT</b></td>");
	Response.Write("<td><textarea name=txt rows=25 cols=110>");
	Response.Write(HttpUtility.HtmlEncode(text));
	Response.Write("</textarea>");
	Response.Write("<tr><td align=center>");
	Response.Write("<input type=submit name=cmd value=' Save '>");
	Response.Write("<input type=button value=Cancel onclick=window.location=('editpage.aspx')>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=checkbox name=del_confirm>Tick to confirm deletion <input type=submit name=cmd value=Delete>");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");
}

string PrintSitePageCats(string current_cat)
{
	StringBuilder sb = new StringBuilder();

	DataSet ds = new DataSet();
	string sc = " SELECT DISTINCT cat FROM site_pages WHERE cat <> '' ORDER BY cat ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "cat");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return "";
	}

	for(int i=0; i<ds.Tables["cat"].Rows.Count; i++)
	{
		string cat = ds.Tables["cat"].Rows[i]["cat"].ToString();
		sb.Append("<option value='" + cat + "'");
		if(cat == current_cat)
			sb.Append(" selected");
		sb.Append(">" + cat + "</option>");
	}
	return sb.ToString();
}

bool SaveSitePage(string id, string name, string text, string cat)
{
	string sc = "";
	if(name == "" && text == "" && id != "")
	{
		sc = " DELETE FROM site_pages WHERE id=" + id;
		sc += " DELETE FROM site_pages WHERE id = " + id;
	}
	else if(id == "")
	{
		sc = " IF NOT EXISTS (SELECT id FROM site_pages WHERE name = '" + name + "') ";
		sc += " BEGIN ";
		sc += " INSERT INTO site_pages (text, name, cat) ";
		sc += " VALUES('" + text + "', '" + name + "', '" + cat + "' ) ";
		sc += " END ";
		sc += " SELECT id FROM site_pages WHERE name = '" + name + "' ";
		try
		{
			DataSet dsid = new DataSet();
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dsid) <= 0)
			{
//				Response.Write("<h3>Error get Ident</h3>");
				return false;
			}
			id = dsid.Tables[0].Rows[0]["id"].ToString();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
//			return false;
		}
		sc = "";
	}
	else
	{
		sc = " UPDATE site_pages SET text='";
		sc += text;
		sc += "', name='"; 
		sc += name;
		sc += "', cat='";
		sc += cat;
		sc += "' WHERE id=" + id;
	}

	//refresh sub page defaults
	sc += " IF NOT EXISTS ( SELECT kid FROM site_sub_pages WHERE id = " + id + " AND description='Default') ";
	sc += " BEGIN ";
	sc += " INSERT INTO site_sub_pages (id, text, description, inuse) VALUES(";
	sc += id + ", '" + text + "', 'Default', 1) ";
	sc += " END ";
	sc += " ELSE ";
	sc += " BEGIN ";
	sc += " UPDATE site_sub_pages SET text = '" + text + "' ";
	sc += " WHERE id = " + id + " AND description='Default' ";
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
</script>

<asp:Label id=LFooter runat=server/>
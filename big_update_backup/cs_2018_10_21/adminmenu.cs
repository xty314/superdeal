<script runat=server>

string r = "r=";
string m_sAdminFooter = "</td></tr></table></td></tr><tr><td><a href=default.aspx?";
string m_sMenuTables = "";
bool m_bShowProgress = false;

DataSet dsAdminMenu = new DataSet();

void PrintAdminMenu()
{
	PrintMenuTables();
//	if(!m_bShowProgress)
//		Response.Write("<table cellpadding=0 cellspacing=0 align=center bgcolor=#FFFFFF width=96% height=95%><tr><td valign=top><table width=100% height=100%><tr><td valign=top>");
}

void PrintAdminHeader()
{
	r += DateTime.Now.ToOADate().ToString(); //random seeds to force IE no cache
//	m_sAdminFooter += r;
//	m_sAdminFooter += ">Main Page</a></td></tr></table><div align=right>&#169; EZNZ CORP. 2001-2002 All Right Reserved</body></html>";

	string sFooter = ReadSitePage("admin_footer");
	if(g_bPDA)
		sFooter = ReadSitePage("admin_footer_pda");
	m_sAdminFooter = ApplyColor(sFooter);
	string header = ReadSitePage("admin_page_header");
	if(g_bPDA)
		header = ReadSitePage("admin_page_header_pda");
	header = header.Replace("@@companyTitle", m_sCompanyTitle);
	Response.Write(ApplyColor(header));
}

string PrintAdminHeader(string sTitle)
{
	m_sAdminFooter = ApplyColor(ReadSitePage("admin_footer"));
	string header = ReadSitePage("admin_page_header");
	if(g_bPDA)
		header = ReadSitePage("admin_page_header_pda");
	header = header.Replace("@@companyTitle", sTitle + " - " + m_sCompanyTitle);
	return ApplyColor(header);
}

void PrintAdminFooter()
{
	Response.Write(m_sAdminFooter);
}

void PrintSearchForm()
{
	string kw = "";

	string kwCompare = "";
	if(Request.QueryString["kw"] != null && Request.QueryString["kw"] != "")
		kwCompare = Request.QueryString["kw"].ToString();
	if(Session["search_keyword"] != null)
	{	
		if(Session["search_keyword"].ToString() == kwCompare)
			kw = Session["search_keyword"].ToString();
		else
			kw = kwCompare;
	}			
	string frmSearch = ReadSitePage("admin_search");
	frmSearch = frmSearch.Replace("@@search_keyword", kw);
	Response.Write(frmSearch);
}

void PrintMenuTables()
{
	//get current page name for dynamic parameters
	string uri = Request.ServerVariables["URL"];
	uri = uri.Substring(0, uri.IndexOf(".aspx"));
	int i = uri.Length-1;
	for(; i>=0; i--)
	{
		if(uri[i] == '/')
			break;
	}
	uri = uri.Substring(i+1, uri.Length - i - 1);

	string sc = "SELECT * FROM menu_admin_catalog ";
	sc += " ORDER BY seq";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsAdminMenu, "cat") <= 0)
			return;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}

	string class_administrator = GetAccessClassID("Administrator");

	string s = ""; //catalog table
	string ss = ""; //sub menu table
	string u = ""; //uri
	string menu_name = "";
	string name = "";
	string cat = "";
	string m_class = Session[m_sCompanyName + "AccessLevel"].ToString();
	DataRow dr = null;

	string bgcolor = "#FFFFFF";//GetSiteSettings("main_menu_bg_color", "#666696");	
	
	s += "<table width=100% cellpadding=0 cellspacing=0>";
	s += "<tr>";
	for(i=0; i<dsAdminMenu.Tables["cat"].Rows.Count; i++)
	{
		dr = dsAdminMenu.Tables["cat"].Rows[i];
		name = dr["name"].ToString();
		cat = dr["id"].ToString();

		Trim(ref name);
		if(name == "") //seperator
			s += "\r\n<td width=30>&nbsp;</td>";
		
		sc = "SELECT i.uri, i.name ";
		sc += " FROM menu_admin_id i JOIN menu_admin_sub s ON i.id=s.menu ";
		if(m_class != class_administrator) //administrator has all access
			sc += " JOIN menu_admin_access a ON i.id=a.menu ";
		sc += " WHERE s.cat = " + cat;
		if(m_class != class_administrator)
			sc += " AND a.class=" + m_class;
		sc += " ORDER BY s.seq";
//DEBUG("sc=", sc);		
		bool bDisabled = false;
		if(dsAdminMenu.Tables["sub"] != null)
			dsAdminMenu.Tables["sub"].Clear();
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dsAdminMenu, "sub") <= 0)
			{
				bDisabled = true;
//				continue;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}

////put left image for the menu, using mleft.jpg
		s += "<td class=ml nowrap></td>";
		
		//sub menu table
		ss += "\r\n<table border=0 cellspacing=0 cellpadding=0 class=m id=" + name + "Menu width=156 style='position:absolute;top:0;left:0;z-index:100;visibility:hidden' onmouseout='hideMenu()'>";
		for(int j=0; j<dsAdminMenu.Tables["sub"].Rows.Count; j++)
		{
			u = dsAdminMenu.Tables["sub"].Rows[j]["uri"].ToString();
			menu_name = dsAdminMenu.Tables["sub"].Rows[j]["name"].ToString();
			ss += "\r\n<tr><td nowrap class=mc>&nbsp&nbsp;<a href=" + u;
			if(u.IndexOf("?") >= 0)
				ss += "&r=";
			else
				ss += "?r=";
			ss += DateTime.Now.ToOADate() + " class=d>" + Lang(menu_name) + "</a></td>";			
			ss += "</tr>";
		}
		ss += "</table>\r\n";		
		
		//catalog table
		s += "\r\n<td width=70px class=mc align=center nowrap id=m" + name + " onmouseover='setMenu(\"m" + name + "\", \"" + name + "Menu\")'>";
//		s += "\r\n<td width=60px class=mc align=center nowrap id=m" + name + ">";// onmouseover='setMenu(\"m" + name + "\", \"" + name + "Menu\")'>";
//		if(!bDisabled)
//			s += "<a href=default.aspx?ms="+ cat +" class=w><b>" + name + "</b>";
//		else
//			s += "<font color=#AAAAAA><b>" + name + "</b></font>";
		s += "<font color=#FFFFFF><b>" + name + "</b></font>";
		////put right image for the menu, using mright.jpg
		s += "</td><td class=mr nowrap>&nbsp;</td>";
	}

	s += "<td class=ml nowrap></td>";
	s += "<td class=mc nowrap align=right style='color:#aad300'><b>"+ Lang("Welcome") +" : <font color=#EEEEEE>" + Session["name"] + "&nbsp;</font></b></td>";
	////put right image for the menu, using mright.jpg
	s += "<td class=mr></td>";
	s += "</tr></table>";
	Response.Write(ss);
	Response.Write(s);
}
</script>



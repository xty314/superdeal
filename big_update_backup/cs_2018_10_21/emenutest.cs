<script runat=server>

string m_class = "1";
DataSet ds = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(Request.QueryString["c"] != null && Request.QueryString["c"] != "")
		m_class = Request.QueryString["c"];

	PrintAdminHeader();
	TestPrintAdminMenu();
	
	Response.Write("<br><br><center><h3>Test : <font color=red>Menu For " + GetAccessClassName(m_class) );

	if(!CheckAccess(m_class))
		Response.Write("<br>Access Denied");

	PrintAdminFooter();
}

void TestPrintAdminMenu()
{
	string r = "r=" + DateTime.Now.ToOADate();

	TestPrintMenuTables();

//	if(!m_bShowProgress)
		Response.Write("<table cellpadding=0 cellspacing=0 align=center bgcolor=#FFFFFF width=96% height=95%><tr><td valign=top><table width=100% height=100%><tr><td valign=top>");
}

void TestPrintMenuTables()
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

	DataSet ds = new DataSet();
	string sc = "SELECT * FROM menu_admin_catalog ORDER BY seq";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "cat") <= 0)
			return;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
	}

	string class_administrator = GetAccessClassID("Administrator");

	string s = ""; //catalog table
	string ss = ""; //sub menu table
	string u = ""; //uri
	string menu_name = "";
	string name = "";
	string cat = "";
	DataRow dr = null;

	s += "<table cellpadding=0 cellspacing=0 BORDER=0 BGCOLOR=#666696>";
	s += "<tr style='color:black;background-color:#666696;font-weight:bold;'>";

	for(i=0; i<ds.Tables["cat"].Rows.Count; i++)
	{
		dr = ds.Tables["cat"].Rows[i];
		name = dr["name"].ToString();
		cat = dr["id"].ToString();

		Trim(ref name);
		if(name == "") //seperator
			s += "\r\n<td width=30>&nbsp;</td>";

		sc = "SELECT i.uri, i.name ";
		sc += " FROM menu_admin_id i JOIN menu_admin_sub s ON i.id=s.menu ";
		if(m_class != class_administrator) //administrator has all access
			sc += " JOIN menu_admin_access a ON i.id=a.menu ";
		sc += " WHERE s.cat=" + cat;
		if(m_class != class_administrator)
			sc += " AND a.class=" + m_class;
		sc += " ORDER BY s.seq";
//DEBUG("sc=", sc);
		
		if(ds.Tables["sub"] != null)
			ds.Tables["sub"].Clear();
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "sub") <= 0)
				continue;
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}

		//sub menu table
		ss += "\r\n<table border=0 class=m id=" + name + "Menu width=156 style='position:absolute;top:0;left:0;z-index:100;visibility:hidden' onmouseout='hideMenu()'>";
		for(int j=0; j<ds.Tables["sub"].Rows.Count; j++)
		{
			u = ds.Tables["sub"].Rows[j]["uri"].ToString();
			if(u.IndexOf("@@help_page") >= 0)
				u = u.Replace("@@help_page", uri);
			else if(u.IndexOf("@@card_id") >= 0)
				u = u.Replace("@@card_id", Session["card_id"].ToString());

			menu_name = ds.Tables["sub"].Rows[j]["name"].ToString();
			ss += "\r\n<tr><td>&nbsp&nbsp;<a href=" + u;
			if(u.IndexOf("?") >= 0)
				ss += "&r=";
			else
				ss += "?r=";
			ss += DateTime.Now.ToOADate() + " class=d>" + menu_name + "</a></td></tr>";
		}
		ss += "</table>\r\n";

		//catalog table
		s += "\r\n<td id=m" + name + " onmouseover='setMenu(\"m" + name + "\", \"" + name + "Menu\")'><a href=default.aspx class=w>&nbsp&nbsp&nbsp&nbsp;" + name + "</td>";
		s += "\r\n<td width=15>&nbsp;</td>";
	}

	s += "</tr></table>";
//	smenu = smenu.Replace("@@help_page", uri);
//	smenu = smenu.Replace("@@rnd", r);
//	smenu = smenu.Replace("@@card_id", Session["card_id"].ToString());
	
	Response.Write(s);
	Response.Write(ss);
}
</script>



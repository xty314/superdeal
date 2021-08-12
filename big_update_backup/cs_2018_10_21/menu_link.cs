<script runat=server>

//string m_id = "";

//DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
bool m_bShowPic = false;
int m_nSetImageBreak = 5;
/*void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];


	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetAllCatalog())
		return;

	PrintBody();

	PrintAdminFooter();
}
*/

bool DoMove()
{
	string id = Request.QueryString["id"];
	string seq = Request.QueryString["seq"];
	
	if(id == null || seq == null || id == "" || seq == "")
	{
		Response.Write("Error, no id or seq, please follow a proper link");
		return false;
	}
	string sc = " UPDATE menu_admin_catalog SET seq=" + seq + " WHERE id=" + id;
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



bool GetAllCatalog()
{
	int rows = 0;
	
//	m_bShowPic = GetSiteSettings("
	string sc = "SELECT m.id, m.name AS cat, mi.name, mi.uri, mi.menu_link1, mi.menu_link2, mi.menu_link3  ";
	sc += " FROM menu_admin_catalog m ";
	sc += " JOIN menu_admin_id mi ON m.id = mi.cat ";
	sc += " INNER JOIN ";
	sc += " menu_admin_sub ms ON ms.menu = mi.id ";
	sc += " WHERE 1=1 ";
	if(g_bDemo && g_bOrderOnlyVersion)
		sc += " AND m.orderonly=1 ";
	if(Request.QueryString["ms"] != null && Request.QueryString["ms"] != "")
		sc += " AND m.name = '"+ Request.QueryString["ms"] +"' ";
//	sc += " GROUP BY mi.name ";
	sc += " AND m.name <> '' ";
	sc += " ORDER BY ms.seq ";
//	sc += " ORDER BY mi.name ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	PrintBody();
	return true;
}

void PrintBody()
{
	Response.Write("<br><center><h3>"+ Request.QueryString["s"] +" Menu</font></h3>");
	Response.Write("<form name=f action=emenucat.aspx method=post>");
	Response.Write("<table width=60% align=center valign=center cellspacing=5 cellpadding=9 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
//	Response.Write("<tr><td colspan=5>");
//	bSelectMenuCat();
//	Response.Write("</td></tr>");
/*	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th align=left>MENU</th>");
//	Response.Write("<th align=left>SEQUENCE</th>");
	Response.Write("<th>NAME</th>");
	Response.Write("<th align=right>URI</th>");
	Response.Write("<th align=right>Seq1</th>");
	Response.Write("<th align=right>Seq2</th>");
	Response.Write("<th align=right>Seq3</th>");
	Response.Write("</tr>");
*/
	string id = "";
	string name = "";
	double seq = 0; //current sequence
	double sequ1 = 0; //previous previous menu's sequence
	double sequ = 0; //previous menu's sequence
	double seqd = 0; //next menu's sequence
	double seqd1 = 0; //next next menu's sequence
	double seqn = 0; //new sequence number (calculated)
	DataRow dr = null;
	int rows = ds.Tables["cat"].Rows.Count;
	bool bAlterColor = false;
	string old_name = "File";
	string menu_link1 = "";
	string menu_link2 = "";
	string menu_link3 = "";

	int nCt = 0;

	bool bBreakCol = false;
	for(int i=0; i<rows; i++)
	{
		dr = ds.Tables["cat"].Rows[i];
		id = dr["id"].ToString();
		name = dr["name"].ToString();
		string uri = dr["uri"].ToString();
		string cat = dr["cat"].ToString();
		menu_link1 = dr["menu_link1"].ToString();
		menu_link2 = dr["menu_link2"].ToString();
		menu_link3 = dr["menu_link3"].ToString();
		if(i== 0 || cat != old_name )
		{
			Response.Write("<tr");
			if(bAlterColor)
				Response.Write(" bgcolor=#EEEEEE");
			bAlterColor = !bAlterColor;
			Response.Write(">");
			nCt = 0;
		}
		
		Trim(ref name);
		if(name == "")
			name = "<font color=green><i>&nbsp&nbsp; (seperator)</i></font>";
		
//		if(m_bShowPic)
		{
			if(m_nSetImageBreak == nCt)
			{
				Response.Write("</tr>");
				nCt = 0;
			}
			Response.Write("<td align=center> &nbsp;&nbsp;");
			Response.Write("<a title='"+ name +"' href='"+ uri +"' class=c>");
			Response.Write("<img size=25 height=35 width=30 border=0 src='/mi/"+ i +".gif'> <br><font size=2>"+ name +"</a></td>");

		
		}
	/*	else
		{
		Response.Write("<td>");
			if(cat != old_name)
		Response.Write(cat);
		Response.Write("</td>");
		Response.Write("<td>");
			Response.Write(name);
		Response.Write("</td>");
		Response.Write("<td align=right>");
		Response.Write(uri);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write("<input type=text name=seq1 value="+ menu_link1 +">");
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write("<input type=text name=seq2 value="+ menu_link2 +">");
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write("<input type=text name=seq3 value="+ menu_link3 +">");
		Response.Write("</td>");
		
		}
		*/
		nCt++;
//		if(cat != old_name)
	//		Response.Write("</tr><tr>");
		old_name = cat;
	}


	Response.Write("</table>");
	Response.Write("</form>");
}

bool bSelectMenuCat()
{
	string sc = " SELECT id, name FROM menu_admin_catalog ";
		sc += " WHERE 1=1 ";
	if(g_bDemo && g_bOrderOnlyVersion)
		sc += " AND orderonly=1 ";
	sc += " AND (name IS NOT NULL AND name <> '') ";
	sc += " ORDER BY seq ";
//DEBUG("sc=", sc);
int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "all");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	Response.Write("<select name=sel_cat>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["all"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		
		Response.Write("<option name="+ id +">"+ name +"</option>");
	}
	Response.Write("</select>");
	return true;
}

</script>

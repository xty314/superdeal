<!-- #include file="kit_fun.cs" -->
<!-- #include file="menu.cs" -->

<script runat=server>

DataSet ds = new DataSet();

int m_kits = 0;
int m_nKitsPerRow = 1;
int m_nItemsPerKit = 0;

string m_kitDetails = ""; //print below items

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	InitKit();
	
	GetPrintableKits();
	DoPrintKit();
}

bool GetPrintableKits()
{
	string sc = " SELECT id, name, details, s_cat, price ";
	sc += " FROM kit WHERE inactive = 0 ";
	sc += " ORDER BY s_cat, name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_kits = myAdapter.Fill(ds, "kits");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoPrintKit()
{
	string tp = ReadSitePage("package_print_template");
	tp = TemplateParseCommand(tp);

	string tp_kit = GetRowTemplate(ref tp, "kitrow");
	string tp_row = GetRowTemplate(ref tp_kit, "rowitem");

	string t = tp;
	string s_cat = "";

	int kit_printed = 0;
	string template_kitrow = "";

	string startPage = "0";
	if(Request.QueryString["p"] != null)
		startPage = Request.QueryString["p"];
	int i = MyIntParse(startPage);
	int end = i + m_nKitsPerRow;

	for(; i<m_kits; i++)
	{
		if(i >= end)
			break;
		DataRow dr = ds.Tables["kits"].Rows[i];
		s_cat = dr["s_cat"].ToString();

		string kit_id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string price = dr["price"].ToString();
		m_kitDetails = dr["details"].ToString();
		Trim(ref m_kitDetails);

		string s1 = tp_kit;
		s1 = s1.Replace("@@kit_name", name);
		s1 = s1.Replace("@@kit_price", price);

		string template_rowitem = PrintKitItems(tp_row, kit_id, ref s1);

		s1 = s1.Replace("@@template_rowitem", template_rowitem);
		template_kitrow += s1;
	}
	t = t.Replace("@@template_kitrow", template_kitrow);
	t = t.Replace("@@s_cat", s_cat);
	t = t.Replace("@@kit_date", DateTime.Now.ToString("dd-MMM-yy"));
	
	Response.Write(t);
	return true;
}

string PrintKitItems(string tp, string kit_id, ref string kit_tp)
{
	StringBuilder sb = new StringBuilder();

	DataSet dsi = new DataSet();
	string sc = " SELECT c.name, c.s_cat ";
	sc += " FROM kit_item k JOIN code_relations c ON c.code=k.code ";
	sc += " WHERE k.id = " + kit_id;
	sc += " ORDER BY k.seq ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsi);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	int rows = dsi.Tables[0].Rows.Count;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dsi.Tables[0].Rows[i];
		string name = dr["name"].ToString();
		string s_cat = dr["s_cat"].ToString();
		s_cat = s_cat.ToLower();
		if(s_cat.IndexOf("cpu") >= 0
			|| s_cat.IndexOf("memory") >= 0
			|| s_cat.IndexOf("hard disk") >= 0
			|| s_cat.IndexOf("monitor") >= 0
			|| s_cat.IndexOf("software") >= 0
			|| s_cat.IndexOf("cdrom") >= 0
			)
		{
			name = "<b>" + name + "</b>";
		}

		sb.Append(tp.Replace("@@item_name", name));
	}

	//get desc lines
	int descLines = 0;
	string s = "";
	bool bRead = ReadLine(m_kitDetails, descLines, ref s);
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		descLines++;
		bRead = ReadLine(m_kitDetails, descLines, ref s);
	}
//	if(descLines > 0)
//		descLines-=1;
//DEBUG("rows="+rows.ToString(), ", descLines="+descLines.ToString());
//DEBUG("breaks=", m_nItemsPerKit-rows-descLines);
	for(int j=rows; j<m_nItemsPerKit-descLines; j++)
	{
		sb.Append(tp.Replace("@@item_name", "&nbsp;"));
	}

	string notes = "";
	descLines = 0;
	s = "";
	bRead = ReadLine(m_kitDetails, descLines, ref s);
	protect = 999;
	while(bRead && protect-- > 0)
	{
		s = s.Replace("\r", "");
		s = s.Replace("\n", "");
		notes += tp.Replace("@@item_name", s);			
		descLines++;
		bRead = ReadLine(m_kitDetails, descLines, ref s);
	}

	kit_tp = kit_tp.Replace("@@notes", notes);
	return sb.ToString();
}

string TemplateParseCommand(string tp)
{
	StringBuilder sb = new StringBuilder();

	int line = 0;
	string sline = "";
	bool bRead = ReadLine(tp, line, ref sline);
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf("@@DEFINE") >= 0)
		{
			string snKits = GetDefineValue("KITS_PER_ROW", sline);
			if(snKits != "")
				m_nKitsPerRow = MyIntParse(snKits);
			string snItems = GetDefineValue("ITEMS_PER_KIT", sline);
			if(snItems != "")
				m_nItemsPerKit = MyIntParse(snItems);
		}
		else
		{
			sb.Append(sline);
		}
		line++;
		bRead = ReadLine(tp, line, ref sline);
	}
	return sb.ToString();
}

string GetDefineValue(string sDef, string sline)
{
	int p = sline.IndexOf(sDef);
	string sValue = "";
	if(p > 0)
	{
		p += sDef.Length + 1;
		for(; p<sline.Length; p++)
		{
			if(sline[p] == ' ' || sline[p] == '\r' || sline[p] == '\n')
				break;
			sValue += sline[p];
		}
	}
	return sValue;
}

</script>

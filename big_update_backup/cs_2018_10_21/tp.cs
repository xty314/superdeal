<!-- #include file="page_index.cs" -->
<script runat=server>
//////////////////////////////////////////////
// data grid template

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_action = "";
string m_sOrderBy = "";
string m_tpName = "card_list";
string m_table = "card";
string m_sc = "";
string m_sk = "";
string m_newID = "";
string m_title = "Card List";
int m_nTableWidth = 0;
int m_nPageSize = 0;
bool m_bAllowPaging = true;

bool m_bDescent = false; //order by xxx DESC

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("administrator"))
		return;

	if(Request.QueryString["t"] != null)
		m_action = Request.QueryString["t"];
	if(Request.QueryString["n"] != null)
		m_tpName = Request.QueryString["n"];
	if(Request.QueryString["sk"] != null)
		m_sk = Request.QueryString["sk"];

	if(Request.QueryString["ob"] != null)
		m_sOrderBy = Request.QueryString["ob"];
	
	if(Request.QueryString["desc"] == "1")
		m_bDescent = true;

	if(Request.Form["cmd"] == "Save")
	{
		if(DoSave())
		{
			string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
			if(Request.QueryString["t"] == "a")
				uri = "tp.aspx?n=" + m_tpName + "&t=e&sk=where+id+%3d+" + m_newID; 
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + uri + "\">");
		}
		return;
	}

	if(!GetTPName())
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>Error getting query string of " + m_tpName);
		return;
	}
	if(!GetData())
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h3>" + m_title + "</h3>");

	if(m_action == "e") //edit
	{
		m_title = "Edit " + m_title;
		PrintEditForm();
	}
	else if(m_action == "a") //add new
	{
		m_title = "New " + m_table + " Row ";
		PrintEditForm();
	}
	else if(!IsPostBack)
		BindGrid();

	if(m_table == "templates")
	{
		string uri1 = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"] + "&t=a";
		LFooter.Text = "<form action=" + uri1 + " method=post>";
		LFooter.Text += "<input type=submit name=cmd value='Add New' " + Session["button_style"] + ">";
		LFooter.Text += "</form>";
	}
	LFooter.Text += m_sAdminFooter;
}

bool GetTPName()
{
	string sc = " SELECT * FROM templates WHERE name = '" + m_tpName + "' ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "tp") > 0)
		{
			m_title = ds.Tables["tp"].Rows[0]["title"].ToString();
			m_table = ds.Tables["tp"].Rows[0]["table_name"].ToString();
			m_sc = ds.Tables["tp"].Rows[0]["sc"].ToString();
			int i = 1;
			string par = "@@p" + i.ToString();
			while(m_sc.IndexOf(par) >= 0)
			{
				if(i >= Request.QueryString.Count)
				{
					Response.Write("<br><center><h3><font color=white>Error, need more parameters");
					return false;
				}
				m_sc = m_sc.Replace(par, Request.QueryString[i]);
				i++;
				par = "@@p" + i.ToString();
			}

			m_bAllowPaging = bool.Parse(ds.Tables["tp"].Rows[0]["paging"].ToString());
			string pageSize = ds.Tables["tp"].Rows[0]["page_size"].ToString();
			if(pageSize != "")
				m_nPageSize = MyIntParse(pageSize);

			string tableWidth = ds.Tables["tp"].Rows[0]["table_width"].ToString();
			if(tableWidth != "")
				m_nTableWidth = MyIntParse(tableWidth);
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false; //didn't find
}

bool GetData()
{
	string sortDescent = m_bDescent?"&desc=0":"&desc=1";
	string r = sortDescent;
	string sc = m_sc;
	if(m_sk != "")
		sc += " " + m_sk;
	if(m_action == "e" || m_action == "a")
		sc = " SELECT TOP 1 * FROM " + m_table + " " + m_sk;
	//if(m_sOrderBy != "")
	//	sc += " ORDER BY '" + m_sOrderBy + "' ";
	if(m_bDescent)
		sc += " DESC";

	if(sc == "")
		return false;

//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "data");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintEditForm()
{
	if(ds.Tables["data"].Rows.Count < 0)
		return;

	string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
//	if(m_action == "a") //add new
//		uri += Request.ServerVariables["URL"] + "?" + 
	Response.Write("<form action=" + uri + " method=post>");

	if(m_action == "a") //add new
		Response.Write("<input type=hidden name=insert value=1>");

	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1 ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=Save " + Session["button_style"] + "></td></tr>");

	string testName = "";
	DataRow dr = null;
	if(ds.Tables["data"] != null && ds.Tables["data"].Rows.Count > 0)
		dr = ds.Tables["data"].Rows[0];
	DataColumnCollection dc = ds.Tables["data"].Columns;
	for(int i=0; i<dc.Count; i++)
	{
		string name = dc[i].ColumnName;
		string value = "";
		if(dr != null)
			value = dr[name].ToString();
		if(name == "name")
			testName = value;
		Response.Write("<input type=hidden name=cn" + i + " value='" + name + "'>");

		if(m_action == "a")
			value = ""; //for add new

		Response.Write("<tr><td valign=top><b>" + name + "</b></td><td>");
		if(name == "id")
			Response.Write("<input type=hidden name=id value=" + value + ">" + value);
		else if(name == "sc")
			Response.Write("<textarea rows=7 cols=100 name=txt" + i + ">" + value + "</textarea>");
		else if(name == "password")
		{
			Response.Write("<input type=password size=20 name=txt" + i + ">");
			if(m_action != "a")
				Response.Write("<font color=red>(You need to re-enter password b4 save)</font>");
		}
		else if(name == "supplier")
		{
			Response.Write(PrintSupplierOptions(value, ""));
		}
		else
			Response.Write("<textarea rows=3 cols=100 name=txt" + i + ">" + value + "</textarea>");
		Response.Write("</td></tr>");
	}
	Response.Write("<tr><td>");
	if(testName != "")
		Response.Write("<input type=button onclick=window.open('tp.aspx?n=" + testName + "') value=Test " + Session["button_style"] + ">");
	Response.Write("</td><td align=right>");
	Response.Write("<input type=submit name=cmd value=Save " + Session["button_style"] + ">");
	Response.Write("</td></tr>");
	Response.Write("</table>");

	Response.Write("<input type=hidden name=columns value=" + dc.Count + ">");
	Response.Write("<input type=hidden name=table_name value=" + m_table + ">");
	Response.Write("</form>");
}

bool DoSave()
{
	m_table = Request.Form["table_name"];
	int cols = MyIntParse(Request.Form["columns"]);
	int id = 0;
	if(m_action != "a")
		id = MyIntParse(Request.Form["id"]);

	string sc = " SET DATEFORMAT dmy ";
	if(Request.Form["insert"] == "1")
	{
		sc += " BEGIN TRANSACTION ";
		sc += " INSERT INTO " + m_table + "( ";
		for(int i=1; i<cols; i++) //skip id column
		{
			if(i > 1)
				sc += ", ";
			sc += Request.Form["cn" + i];
		}
		sc += ") VALUES ( " ;
		for(int i=1; i<cols; i++) //skip id column
		{
			if(i > 1)
				sc += ", ";
			string value = EncodeQuote(Request.Form["txt" + i]);
			if(Request.Form["cn" + i] == "supplier")
				value = Request.Form["supplier"];
			else if(value.ToLower() == "true")
				value = "1";
			else if(value.ToLower() == "false")
				value = "0";
			sc += " '" + value + "' ";
		}
		sc += ") SELECT IDENT_CURRENT('" + m_table + "') AS id ";
		sc += " COMMIT ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(ds, "id") != 1)
				return false;
			m_newID = ds.Tables["id"].Rows[0]["id"].ToString();
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	else
	{
		sc += " UPDATE " + m_table + " SET ";
		for(int i=1; i<cols; i++) //skip id column
		{
			if(i > 1)
				sc += ", ";
			string value = EncodeQuote(Request.Form["txt" + i]);
			if(Request.Form["cn" + i] == "supplier")
				value = Request.Form["supplier"];
			else if(value.ToLower() == "true")
				value = "1";
			else if(value.ToLower() == "false")
				value = "0";
			else if(Request.Form["cn" + i] == "password")
				value = FormsAuthentication.HashPasswordForStoringInConfigFile(value, "md5");
			sc += Request.Form["cn" + i] + " = '" + value + "' ";
		}
		sc += " WHERE id = " + id;
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
	}
	return true;	
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
/*	DataView source = new DataView(ds.Tables["data"]);
	if(m_nTableWidth > 0)
		MyDataGrid.Width = m_nTableWidth;
	if(m_nPageSize > 0)
		MyDataGrid.PageSize = m_nPageSize;

	MyDataGrid.AllowPaging = m_bAllowPaging;
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
*/
	Type t = null;
	Type tString = System.Type.GetType("System.String");
	Type tMoney = System.Type.GetType("System.Money");
	Type tDecimal = System.Type.GetType("System.Decimal");
	Type tBool = System.Type.GetType("System.Boolean");

	string tableName = "data";
	DataColumnCollection dc = ds.Tables[tableName].Columns;

	int i = 0;

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	m_cPI.PageSize = int.Parse(GetSiteSettings("set_tp_table_rows", "50", false));
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?n=" + m_tpName;
	if(m_sOrderBy != "")
		m_cPI.URI += "&ob=" + m_sOrderBy;
	if(m_bDescent)
		m_cPI.URI += "&desc=1";
	if(!m_bAllowPaging && m_nPageSize > 0)
		m_cPI.PageSize = m_nPageSize;

	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;

	string sPageIndex = m_cPI.Print();

	string tableWidth = "100%";
	if(m_nTableWidth > 0)
		tableWidth = m_nTableWidth.ToString();

	Response.Write("<table width=" + tableWidth + " align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");

	int cols = 0;
	for(int m=0; m<dc.Count; m++)
	{
		string cname = dc[m].ColumnName;
		t = dc[m].DataType;
//DEBUG("type=", t.ToString());
		if(cname == " " || cname == "seq")
		{
			cols++;
			continue;
		}
		string uri = "<a href=tp.aspx?n=" + m_tpName + "&ob=" + HttpUtility.UrlEncode(cname);
		if(!m_bDescent)
			uri += "&desc=1";
		uri += " class=o><font color=white><b>";
		Response.Write("<th>" + uri + cname + "</b></font></a></th>");
		cols++;
	}

	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}
	bool bAlterColor = false;

	if(!m_bAllowPaging)
		end = rows;

	for(; i < rows && i < end; i++)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + GetSiteSettings("table_row_bgcolor", "#EEEEEE"));
		bAlterColor = !bAlterColor;
		Response.Write(">");

		DataRow dr = ds.Tables[tableName].Rows[i];
		for(int j=0; j<cols; j++)
		{
			string cname = dc[j].ColumnName;
			if(cname == " " || cname == "seq")
				continue;
			string value = dr[j].ToString();
//			t = ds.Tables[tableName].Columns[j].DataType;
//			if(t == tDecimal && value != "")
//			{
//				value = MyDoubleParse(value).ToString("c");
//				Response.Write("<td align=right>" + value + "</td>");
//			}
//			else
				Response.Write("<td");
				if(j == cols - 1)
					Response.Write(" align=right");
				Response.Write(">" + value + "</td>");
		}
		Response.Write("</tr>");
	}
	if(m_bAllowPaging)
		Response.Write("<tr><td colspan=" + cols + ">" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	return;
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}
</script>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	style=fixed
	width=100%
	HorizontalAlign=center
	AllowPaging=True
	PageSize=40
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>
<!-- #include file="page_index.cs" -->
<!-- #include file="menu.cs" -->

<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="iTextSharp" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.html" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="iTextSharp.text.html.simpleparser" %>
<script runat=server>
//////////////////////////////////////////////
// data grid template

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string cat = "";
string s_cat = "";
string ss_cat = "";
string m_code = "";
string tableWidth = "91%";
string m_branchID = "1";
string m_brochureID = "";
string m_brochureTitle = "";
string m_uriString = "";
string m_EditBrochure = "";
double m_dGSTRate = 0.125;
int m_nItemsPerRow = 1;
int m_nItemRowsPerPage = 1;
int total = 0;
string m_action = "";
string m_type = "";
string m_cmd = "";

//string setStyleSheet = "<STYLE> P {page-break-before: always}</STYLE>"; 

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("administrator"))
		return;
	if(!MyBooleanParse(GetSiteSettings("Enable_Brochure_Feature", "0", true)))
	{		
		return;
	}

	if(Request.QueryString["t"] != null)
		m_action = Request.QueryString["t"];
	m_cmd = p("cmd");

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
	if(Request.QueryString["brochureid"] != null && Request.QueryString["brochureid"] != "")
		m_brochureID = Request.QueryString["brochureid"];
	if(Request.QueryString["title"] != null && Request.QueryString["title"] != "")
		m_brochureTitle = Request.QueryString["title"];
	if(Request.QueryString["ep"] != null && Request.QueryString["ep"] != "")
		m_EditBrochure = Request.QueryString["ep"];
	if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
		m_branchID = Request.QueryString["branch"];
	if(!TSIsDigit(m_branchID))
		m_branchID = "1";
	m_uriString = "&branch="+ m_branchID +"&title="+ HttpUtility.UrlEncode(m_brochureTitle) +"&brochureid="+ m_brochureID +"";
	//////// set no brochure id to reget the brochure id ///////
	
	if(Session[m_sCompanyName +"gst_rate"] != null)
		m_dGSTRate = 1+ MyDoubleParse(Session[m_sCompanyName +"gst_rate"].ToString());
	else
		m_dGSTRate = 1+ MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;

	if(Request.Form["cmd"] == "Add New Title")
	{		
		if(doAddBrochureTitle())
		{
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?brochureid="+ m_brochureID +"&branch="+ m_branchID +"&title="+ HttpUtility.UrlEncode(m_brochureTitle) +"");			
			Response.Write("\">");
			return;
		}		
		return;
	}
	if(Request.QueryString["print"] == "1" && Request.QueryString["price"] != null && Request.QueryString["price"] != "" && m_brochureID != "")
	{
		PrintBrochureLayout();
		Response.Write(PrintBrochureLayout());
		return;
	}
	if(m_action == "pdf")
	{
		DoCreatePdf();
		return;
	}
	if(m_cmd == Lang("Send"))
	{
		DoSendEmail();
		return;
	}
	/////insert new brochure title first
	if(Request.QueryString["add"] == "new")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		PrintBrochureTitleForm();
		//ShowEditTable();
		PrintAdminFooter();
		return;
	}			
	if(Request.QueryString["delall"] != null && Request.QueryString["delall"] != "")
	{
		if(DoDelete(Request.QueryString["delall"].ToString(), true))
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+ m_uriString +"\">");
		return;
	}
	if(Request.Form["cmd"] == "Save")
	{
		//Save the tempalte..
		doSaveTemplate();
	}
	if(m_EditBrochure != null && m_EditBrochure != "" && m_brochureID != null && m_brochureID != "")
	{
		//edit brochure Template
		PrintAdminHeader();
		PrintAdminMenu();		
		
		PrintEditForm();
		PrintAdminFooter();
		return;
	}
	//////get brochure id to insert item to brochure list////
	if(m_brochureID != null && m_brochureID != "" )
	{
		if(m_action == "bulk")
		{
			Trim(ref cat);
			Trim(ref s_cat);
			Trim(ref ss_cat);

			if(Request.Form["cmd"] == "Add All Pages")
			{
				if(DoBulkAddAll())
				{
					Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?t=bulk");
					Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
					Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
					Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
					Response.Write(m_uriString);
					Response.Write("\">");
					return;
				}
			}
			else if(Request.Form["cmd"] == "Add Selected")
			{
				if(DoBulkAddSelected())
				{
					Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?t=bulk");
					Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
					Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
					Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
					if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
						Response.Write("&p=" + Request.QueryString["p"]);
					if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
						Response.Write("&spb=" + Request.QueryString["spb"]);
					Response.Write(m_uriString);
					Response.Write("\">");
					return;
				}
			}
			PrintAdminHeader();
			PrintAdminMenu();
			PrintBulkForm();
			PrintAdminFooter();
			return;
		}
		else if(m_action == "del")
		{
			if(DoDelete())
				Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+ m_uriString +"\">");
			return;
		}
	/*	else if(m_action == "print1")
		{
			PrintPriceList(1);
			return;
		}
		else if(m_action == "print2")
		{
			PrintPriceList(2);
			return;
		}
	*/
		else if(Request.Form["code"] != null && Request.Form["code"] != "")
		{
			if(DoAddOne())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?code=" + Request.Form["code"] + m_uriString +"\">");
			return;
		}

		PrintAdminHeader();
		PrintAdminMenu();
		if(TSIsDigit(m_brochureID))
		{
			ShowEditTable();
			return;
		}
		else
		{			
			Response.Write("<center><h3><br>Invalid Brochure ID!!!</h3>");
			Response.Write("<br><br><a title='back to list' href='"+ Request.ServerVariables["URL"] +"' class=o>Back to Brochure List</a></center>");
			
			return;
		}
		PrintAdminFooter();
	}

	PrintAdminHeader();
	PrintAdminMenu();
	PrintBrochureList();
	PrintAdminFooter();
/*	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h3>Print Price List</h3>");
	ShowEditTable();
	PrintAdminFooter();
*/
}

bool GetBrochureTable()
{
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		m_code = Request.QueryString["code"];
	
	cat = EncodeQuote(cat.ToLower());
	s_cat = EncodeQuote(s_cat.ToLower());
	ss_cat = EncodeQuote(ss_cat.ToLower());
	string sc = " SELECT bi.kid, b.brochure_name, b.brochure_id, bi.code, c.supplier_code, p.name, p.cat, p.s_cat, p.ss_cat ";
	sc += " FROM brochure b ";
	sc += " JOIN brochure_item bi ON b.brochure_id = bi.brochure_id ";
	sc += " JOIN code_relations c ON c.code = bi.code JOIN product p ON p.code = bi.code ";
	sc += " WHERE 1=1 ";
	if(cat != "")
	{
		sc += " AND Lower(p.cat) = '" + cat + "' ";		
	}
	if(s_cat != "")
	{		
		sc += " AND Lower(p.s_cat) = '" + s_cat + "' ";
	}
	if(ss_cat != "")
	{		
		sc += " AND LOWER(p.ss_cat) = '" + ss_cat + "' ";
	}
	if(m_code != null && m_code != "")
	{		
		if(TSIsDigit(m_code))
			sc += " AND bi.code = " + m_code;
		else
			sc += " AND (b.supplier_code = '" + m_code +"' OR c.barcode = '"+ m_code +"' )" ;

	}
	sc += " AND b.brochure_id = "+ m_brochureID +" AND b.branch_id = "+ m_branchID +"";
sc += " ORDER BY bi.kid ";
//	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";
//DEBUG("sc =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "brochure");
	}
	catch(Exception e) 
	{
	string bdText = "";
		
		if((e.ToString()).IndexOf("Invalid object name 'brochure'.") >=0 )
		{
			sc = @"
				CREATE TABLE [dbo].[brochure](
				[brochure_id] [int] IDENTITY(1,1) NOT NULL,
				[brochure_name] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF_brochure_brochure_name]  DEFAULT (''),
				[record_date] [datetime] NOT NULL CONSTRAINT [DF_brochure_record_date]  DEFAULT (getdate()),
				[record_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF_brochure_record_name]  DEFAULT (''),
				[record_by] [int] NOT NULL CONSTRAINT [DF_brochure_record_by]  DEFAULT (0),
				[brochure_active] [bit] NOT NULL CONSTRAINT [DF_brochure_brochure_active]  DEFAULT (1),
				[branch_id] [int] NOT NULL CONSTRAINT [DF_brochure_branch_id]  DEFAULT (1),
				[brochure_template] [text] COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
				
				[items_per_row] [int] NOT NULL CONSTRAINT [DF_brochure_items_per_row]  DEFAULT (1),
				[total_rows_per_page] [int] NOT NULL CONSTRAINT [DF_brochure_total_rows_per_page]  DEFAULT (8),
			 CONSTRAINT [PK_brochure] PRIMARY KEY CLUSTERED 
			(
				[brochure_id] ASC
			) ON [PRIMARY]
			) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
		
				";

			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}
		if((e.ToString()).IndexOf("Invalid object name 'brochure_item'.") >=0 )
		{
			sc = @"
				CREATE TABLE [dbo].[brochure_item](
					[kid] [bigint] IDENTITY(1,1) NOT NULL,
					[brochure_id] [int] NOT NULL,
					[code] [int] NOT NULL,
			CONSTRAINT [PK_brochure_item] PRIMARY KEY CLUSTERED 
			(
				[kid] ASC
			) ON [PRIMARY]
				) ON [PRIMARY]

			";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetBrochureData()
{
	string sc = " SELECT p.code, p.name, p.cat, p.s_cat, p.ss_cat, p.price, c.level_rate1, c.level_rate2, c.level_rate3, c.level_rate4,c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9 ";
	sc += " FROM brochure b JOIN brochure_item bi ON bi.brochure_id = b.brochure_id ";
	sc += " JOIN code_relations c ON c.code = bi.code ";
	sc += " JOIN product p ON p.code = bi.code ";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";

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

bool GetBrochureList()
{
	string sc = " SELECT b.*, br.name AS branch_name , ISNULL((SELECT count(bi.code) AS total FROM brochure_item bi WHERE bi.brochure_id = b.brochure_id),0) AS total_item ";
	sc += " FROM brochure b JOIN branch br ON br.id = b.branch_id  ";
	sc += " ORDER BY b.brochure_id ";

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "brochure_list");
	}
	catch(Exception e) 
	{
		string bdText = ""; /*@"<table><tr>";
			bdText += "@@SETROWS items_rows_per_page 8";
			bdText += "@@SETITEMS items_per_row 2";
			bdText += @"<!-- BEGIN row_item -->
					<td>
					";
				bdText += "@@ITEM_NAME <br> ";
				bdText += "@@ITEM_PRICE ";
				bdText += "<br>@@ITEM_CODE<br><img src='@@ITEM_IMAGE_LINK'> ";
				bdText += @" </td>
					<!-- END row_item -->		
					</tr>
					</table>
		";*/
		if((e.ToString()).IndexOf("Invalid object name 'brochure'.") >=0 )
		{
			sc = @"
				CREATE TABLE [dbo].[brochure](
				[brochure_id] [int] IDENTITY(1,1) NOT NULL,
				[brochure_name] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF_brochure_brochure_name]  DEFAULT (''),
				[record_date] [datetime] NOT NULL CONSTRAINT [DF_brochure_record_date]  DEFAULT (getdate()),
				[record_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL CONSTRAINT [DF_brochure_record_name]  DEFAULT (''),
				[record_by] [int] NOT NULL CONSTRAINT [DF_brochure_record_by]  DEFAULT (0),
				[brochure_active] [bit] NOT NULL CONSTRAINT [DF_brochure_brochure_active]  DEFAULT (1),
				[branch_id] [int] NOT NULL CONSTRAINT [DF_brochure_branch_id]  DEFAULT (1),
				[brochure_template] [text] COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
				[items_per_row] [int] NOT NULL CONSTRAINT [DF_brochure_items_per_row]  DEFAULT (1),
				[total_rows_per_page] [int] NOT NULL CONSTRAINT [DF_brochure_total_rows_per_page]  DEFAULT (8),
			 CONSTRAINT [PK_brochure] PRIMARY KEY CLUSTERED 
			(
				[brochure_id] ASC
			) ON [PRIMARY]
			) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
		
				";

			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}
		if((e.ToString()).IndexOf("Invalid object name 'brochure_item'.") >=0 )
		{
			sc = @"
				CREATE TABLE [dbo].[brochure_item](
					[kid] [bigint] IDENTITY(1,1) NOT NULL,
					[brochure_id] [int] NOT NULL,
					[code] [int] NOT NULL,
			 CONSTRAINT [PK_brochure_item] PRIMARY KEY CLUSTERED 
			(
				[kid] ASC
			) ON [PRIMARY]
				) ON [PRIMARY]

			";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				ShowExp(sc, ee);
				return false;
			}
		}	return false;
	}

	return true;
}

void PrintBrochureList()
{
	if(!GetBrochureList())
		return;

	string tableName = "brochure_list";
	DataColumnCollection dc = ds.Tables[tableName].Columns;

	int cols = dc.Count;
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
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r="+ DateTime.Now.ToOADate() +"";
	m_cPI.URI += m_uriString;
	m_cPI.PageSize = 50;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<form action="+ Request.ServerVariables["URL"] +" method=post name=f>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Brochure List</b><font color=red><b>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" <b> - Branch : </b> ");
		PrintBranchNameOptions(m_branchID);
		//Response.Write("</td></tr>");
	}
	Response.Write(" <b>&nbsp;&nbsp;&nbsp;</b> ");
	
	Response.Write("</td><td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	//DEBUG("cols =", cols);
	Response.Write("<tr><td colspan=" + cols + "><br></td></tr>");
	Response.Write("<tr><td colspan=" + (cols-6) + ">" + sPageIndex + "</td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=button name='cmd' value='Add New Brochure' "+ Session["button_style"].ToString() +"");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&add=new');\"");
	Response.Write(">");
	//Response.Write("<input type=button name='Add New Brochure' "+ Session["button_style"].ToString() +">");
	//Response.Write("<input type=button name='Add New Brochure' "+ Session["button_style"].ToString() +">");
	Response.Write("</td>");
	Response.Write("</tr>");
	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");	
	Response.Write("<th>BRANCH</th>");
	Response.Write("<th>BROCHURE#</th>");
	
	Response.Write("<th>BROCHURE TITLE</th>");
	Response.Write("<th>RECORD DATE</th>");
	Response.Write("<th>RECORD NAME</th>");
	Response.Write("<th>TOTAL ITEMS</th>");
	Response.Write("<th>ACTION</th>");
//	Response.Write("<th>SAMPLE</th>");	
	Response.Write("</tr>");
	
	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + GetSiteSettings("table_row_bgcolor", "#EEEEEE"));
		bAlterColor = !bAlterColor;
		Response.Write(">");

		DataRow dr = ds.Tables[tableName].Rows[i];				
		string brochure_id = dr["brochure_id"].ToString();
		string title = dr["brochure_name"].ToString();
		string branch_id = dr["branch_id"].ToString();
		Response.Write("<td>");		
		Response.Write(dr["branch_name"].ToString() +"</td>");		
		Response.Write("<td>");
		Response.Write("<a title='add/edit items to this brochure' href='"+ Request.ServerVariables["URL"] +"?brochureid="+ brochure_id +"&title="+ HttpUtility.UrlEncode(title) +"&branch="+ branch_id+"' class=o>");
		Response.Write(dr["brochure_id"].ToString() +"</a></td>");
		Response.Write("<td>");
		Response.Write("<a title='add/edit items to this brochure' href='"+ Request.ServerVariables["URL"] +"?brochureid="+ brochure_id +"&title="+ HttpUtility.UrlEncode(title) +"&branch="+ branch_id+"' class=o>");
		Response.Write(dr["brochure_name"].ToString() +"</a></td>");
		Response.Write("<td>");
		Response.Write(dr["record_date"].ToString() +"</td>");
		Response.Write("<td>");
		Response.Write(dr["record_name"].ToString() +"</td>");
		Response.Write("<td>");
		Response.Write(dr["total_item"].ToString() +"</td>");
		Response.Write("<td align=right>");
		Response.Write("<input type=button value='PDF' class=b onclick=\"window.location=('"+ Request.QueryString["URL"] +"?t=pdf&brochureid="+ brochure_id +"&title="+ HttpUtility.UrlEncode(title) +"&branch="+ branch_id+"');\">");
		Response.Write("<input type=button value='EDIT' class=b onclick=\"window.location=('"+ Request.QueryString["URL"] +"?brochureid="+ brochure_id +"&title="+ HttpUtility.UrlEncode(title) +"&branch="+ branch_id+"');\">");
        Response.Write("<input type=button value='X' class=b onclick=\"if(confirm('Are you sure to delete all!!!')){window.location=('"+ Request.QueryString["URL"] +"?delall="+ brochure_id +"');}else{return false;}\"></td>");
//		Response.Write("<a href=liveedit.aspx?code=" + dr["code"].ToString() + " class=o target=_blank>EDIT</a> &nbsp; ");
//		Response.Write("<a href="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"&t=del&id=" + dr["id"].ToString() + " class=o>DEL</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=" + (cols-6) + ">" + sPageIndex + "</td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=button name='cmd' value='Add New Brochure' "+ Session["button_style"].ToString() +"");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&add=new');\"");
	Response.Write(">");
	//Response.Write("<input type=button name='Add New Brochure' "+ Session["button_style"].ToString() +">");
	//Response.Write("<input type=button name='Add New Brochure' "+ Session["button_style"].ToString() +">");
	Response.Write("</td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	
	return;
}
bool DoDelete()
{
    return DoDelete("", false);
}

bool DoDelete(string brochureID, bool bDeleteAll)
{

	string sc = "";
    if(bDeleteAll)
    {
        sc = " DELETE FROM brochure_item ";
        sc += " WHERE brochure_id = "+ brochureID;
        
        sc += " DELETE FrOM brochure WHERE brochure_id = "+ brochureID;
    
    }
    else
    {
           sc = " DELETE FROM brochure_item ";
        sc += " WHERE kid = " + Request.QueryString["kid"];
    }
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

bool DoAddOne()
{
//	if(GetProductDesc(Request.Form["code"]) == "")
	if(!bCheckExistedItem(Request.Form["code"]))
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, product not found. code = " + Request.Form["code"] + "</h3>");
		return false;
	}

	string sc = " IF NOT EXISTS (SELECT * FROM brochure_item WHERE ";

	sc += " code=" + Request.Form["code"] + ") ";
	sc += " INSERT INTO brochure_item (code, brochure_id) VALUES(" + Request.Form["code"] + ", "+ m_brochureID +") ";
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

/////////////////////////////////////////////////////////////////
void DoShowDealerLevel()
{
	int nDealerLevel = int.Parse(GetSiteSettings("dealer_levels", "6"));
	Response.Write("<select name=dealer_level>");
    Response.Write("<option value='price1' selected>Retail Price</optoin>");
	for(int i=1; i<=nDealerLevel; i++)
	{
		Response.Write("<option value="+ i +">Dealer Level: "+ i +"</option>");
	}
	Response.Write("</select>");

}
void ShowEditTable()
{
	string tableName = "brochure";
	if(ds.Tables[tableName] != null)
		ds.Tables[tableName].Clear();

	if(!GetBrochureTable())
		return;

	
	DataColumnCollection dc = ds.Tables[tableName].Columns;

	int cols = dc.Count;
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
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r="+ DateTime.Now.ToOADate() +"";
	m_cPI.URI += m_uriString;
	m_cPI.PageSize = 35;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;

	string sPageIndex = m_cPI.Print();

	Response.Write("<form action="+ Request.ServerVariables["URL"] + "?" +m_uriString +" method=post name=f>");
		Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Add/Edit Item for :: "+ m_brochureTitle.ToUpper() +"</b><font color=red><b>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" <b> Branch : </b> ");
		PrintBranchNameOptions(m_branchID);
		//Response.Write("</td></tr>");
	}
	Response.Write(" <b>&nbsp;&nbsp;&nbsp;</b> ");
	
	Response.Write("</td><td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan='"+ cols +"'><br></td></tr>");

	Response.Write("<tr><td colspan=5><b>Code/M_PN/Barcode : </b><input type=text name=code size=5>");
	Response.Write("<input type=submit name=cmd value=Add " + Session["button_style"] + ">");
//	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	//Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=button name=cmd value='Bulk Add' ");
	Response.Write(" onclick=window.location=('"+ Request.ServerVariables["URL"] +"?t=bulk");
	Response.Write("&brochureid="+ m_brochureID +"&title="+ HttpUtility.UrlEncode(m_brochureTitle) +"&branch="+ m_branchID +"");
	if(Request.QueryString["add"] != null)
		Response.Write("&add="+Request.QueryString["add"]);
	Response.Write("') " + Session["button_style"] + ">");
//	Response.Write("<i>(select from catalog)</i>");
	Response.Write("<input type=button name=cmd value='Back to Brochure List' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"');\">");
	
//	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<br>Select Existed Catagories: "); PrintCatsForDisplay();
	Response.Write("</td>");
	
	Response.Write("<td valign=top colspan=4 align=right>");
	
//	Response.Write("<input type=button onclick=window.open('editpage.aspx?p=brochure_template') value='Edit Template' " + Session["button_style"] + ">");
/*	Response.Write("<input type=button onclick=window.open('editpage.aspx?p=brochure_header') value='Edit Header' " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=window.open('editpage.aspx?p=brochure_body') value='Edit Body' " + Session["button_style"] + ">");
	Response.Write("<input type=button onclick=window.open('editpage.aspx?p=brochure_footer') value='Edit Footer' " + Session["button_style"] + ">");
*/
	Response.Write("<input type=button onclick=window.open('"+ Request.ServerVariables["URL"] +"?ep=brochure_template"+ m_uriString +"') value='Edit Template' " + Session["button_style"] + ">");
	//Response.Write("<input type=button onclick=window.open('"+ Request.ServerVariables["URL"] +"?ep=brochure_body"+ m_uriString +"') value='Edit Body' " + Session["button_style"] + ">");
	//Response.Write("<input type=button onclick=window.open('"+ Request.ServerVariables["URL"] +"?ep=brochure_footer"+ m_uriString +"') value='Edit Footer' " + Session["button_style"] + ">");

	Response.Write("&nbsp&nbsp&nbsp;Select Pricing Level:");
	DoShowDealerLevel();
	Response.Write("<input type=button onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?brochureid="+ m_brochureID +"&t=pdf&price='+ document.f.dealer_level.value);\" value='PDF' class=b>");
	//Response.Write("<input type=button onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?t=print1&dl='+ document.f.dealer_level.value)\" value='Print Page 1' " + Session["button_style"] + ">");
	//Response.Write("<input type=button onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?t=print2&dl='+ document.f.dealer_level.value)\" value='Print Page 2' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(int m=1; m<cols; m++)
		Response.Write("<th><b>" + dc[m].ColumnName.ToUpper() + "</b></th>");
	Response.Write("<th><b>ACTION</b></th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + GetSiteSettings("table_row_bgcolor", "#EEEEEE"));
		bAlterColor = !bAlterColor;
		Response.Write(">");

		DataRow dr = ds.Tables[tableName].Rows[i];
		string value = "";
		for(int j=1; j<cols; j++)
		{
			value = dr[j].ToString();
			Response.Write("<td>" + value + "</td>");
		}
		Response.Write("<td align=right>");
		Response.Write("<a href=liveedit.aspx?code=" + dr["code"].ToString() + " class=o target=_blank>EDIT</a> | ");
		Response.Write("<a href="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(cat)+ m_uriString +"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"&t=del&kid=" + dr["kid"].ToString() + " class=o>DEL</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=" + (cols-1) + ">" + sPageIndex + "</td><td align=right><input type=button value='Delete This Brochure' "+ Session["button_style"] +" onclick=\"if(confirm('Are you sure to delete all!!!')){window.location=('"+ Request.QueryString["URL"] +"?delall="+ m_brochureID +"');}else{return false;}\"></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");

	return;
}

bool PrintBulkForm()
{
	if(!GetProductList())
		return false;

	string tableName = "bulk";
	int i = 0;
	DataColumnCollection dc = ds.Tables[tableName].Columns;
	int cols = dc.Count;	
	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	m_cPI.TotalRows = rows;
	m_cPI.URI = "?t=bulk";
	m_cPI.URI += m_uriString;
	m_cPI.URI += "&cat=" + HttpUtility.UrlEncode(cat);
	m_cPI.URI += "&scat=" + HttpUtility.UrlEncode(s_cat);
	m_cPI.URI += "&sscat=" + HttpUtility.UrlEncode(ss_cat);

	m_cPI.PageSize = 20;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

//	Response.Write("<br><center><h3>Brochure Template - Bulk Add</h3>");

	Response.Write("<form action="+ Request.ServerVariables["URL"] +"?t=bulk");
	Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
		Response.Write("&p=" + Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
		Response.Write("&spb=" + Request.QueryString["spb"]);
	Response.Write("&branch="+ m_branchID +"&title="+ HttpUtility.UrlEncode(m_brochureTitle) +"&brochureid="+ m_brochureID +"");
	Response.Write(" method=post>");

	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Add Bulk Item for :: "+ m_brochureTitle.ToUpper() +"</b><font color=red><b>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" <b> Branch : </b> ");
		PrintBranchNameOptions(m_branchID);
		//Response.Write("</td></tr>");
	}
	Response.Write(" <b>&nbsp;&nbsp;&nbsp;</b> ");
	
	Response.Write("</td><td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=8><br></td></tr>");

	Response.Write("<tr><td colspan=4 nowrap><b>Select Catalog : </b>");
	PrintCats();
	Response.Write("</td>");

	Response.Write("<td colspan=4 align=right>");
	Response.Write("<b>Total </b><font color=red><b>" + total + "</b></font><b> items found</b>&nbsp&nbsp;");
	Response.Write("<input type=submit name=cmd value='Add Selected' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Add All Pages' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr align=left style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(int m=0; m<cols; m++)
		Response.Write("<th><b>" + dc[m].ColumnName.ToUpper() + "</b></th>");
	Response.Write("<th><b>Select</b></th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("<tr><td colspan=8 align=right>");
		Response.Write("<input type=button name=cmd value='Back to Brochure List' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"');\">");
		Response.Write("<input type=button value='Show Added Item' onclick=window.location=('"+ Request.ServerVariables["URL"] +"?"+ m_uriString +"') " + Session["button_style"] + ">");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		Response.Write("</form>");
		return true;
	}

	bool bAlterColor = false;

	Response.Write("<input type=hidden name=row_start value=" + i + ">");

	for(; i < rows && i < end; i++)
	{
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=" + GetSiteSettings("table_row_bgcolor", "#EEEEEE"));
		bAlterColor = !bAlterColor;
		Response.Write(">");

		DataRow dr = ds.Tables[tableName].Rows[i];
		string value = "";
		for(int j=0; j<cols; j++)
		{
			value = dr[j].ToString();
			Response.Write("<td>" + value + "</td>");
		}

		Response.Write("<input type=hidden name=code" + i + " value=" + dr["code"].ToString() + ">");
		Response.Write("<td align=right><input type=checkbox name=select" + i);
		Response.Write("></td>");
		Response.Write("</tr>");
	}
	
	Response.Write("<input type=hidden name=row_end value=" + i + ">");

	Response.Write("<tr><td colspan=" + cols+1 + ">" + sPageIndex + "</td></tr>");

	Response.Write("<tr><td colspan=7 align=right>");
	Response.Write("<input type=button value='Show Added Item' onclick=window.location=('"+ Request.ServerVariables["URL"] + "?"+ m_uriString +"') " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");	

	return true;
}

bool GetProductList()
{
	string sc = "";
	if(cat == "")
		cat = "just build the table, no row return please";

	sc = " SELECT DISTINCT p.code, p.supplier_code, p.cat, p.s_cat, p.ss_cat, p.name, s.qty AS quantity ";
	sc += " FROM product p ";
	sc += " JOIN stock_qty s ON s.code = p.code";
//	sc += " LEFT OUTER JOIN flare f ";
	sc += " WHERE p.cat = '" + cat + "' ";
	if(s_cat != "" && s_cat != "all")
		sc += " AND p.s_cat = '" + s_cat + "' ";
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND p.ss_cat = '" + ss_cat + "' ";
	sc += " AND s.branch_id ="+ m_branchID +"";
//	sc += " AND p.code NOT IN (SELECT bi.code FROM brochure_item bi JOIN brochure b ON b.brochure_id = bi.brochure_id WHERE b.branch_id = "+ m_branchID +") ";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";
//DEBUG("product listsc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		total = myAdapter.Fill(ds, "bulk");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintCats()
{
	int rows = 0;
	string sc = "SELECT DISTINCT p.cat ";
	sc += " FROM product p ";
	sc += " JOIN stock_qty s ON s.code = p.code ";
	sc += " AND s.branch_id = "+ m_branchID +"";
	sc += " ORDER BY p.cat";
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("<select name=cat ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
	Response.Write(m_uriString);
	Response.Write("&cat=' + escape(this.options[this.selectedIndex].value))\"");
	Response.Write(">");
	Response.Write("<option value='all'>Select One</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		sc = "SELECT DISTINCT p.s_cat ";
		sc += " FROM product p ";
		sc += " JOIN stock_qty s ON s.code = p.code ";
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " AND s.branch_id = "+ m_branchID +"";
		sc += " ORDER BY p.s_cat";
//	DEBUG("sc =", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
		Response.Write(m_uriString);
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}
		Response.Write("</select>");
	}

	
	if(s_cat != "")
	{
		sc = "SELECT DISTINCT ss_cat ";
		sc += " FROM product p ";
		sc += " JOIN stock_qty s ON s.code = p.code ";
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " AND p.s_cat = '"+ s_cat +"' ";
		sc += " AND s.branch_id = "+ m_branchID +"";
		sc += " ORDER BY p.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=ss_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
		Response.Write(m_uriString);
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");

		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}

	return true;
}

bool PrintCatsForDisplay()
{
	int rows = 0;
	string sc = "SELECT DISTINCT p.cat ";
	sc += " FROM product p JOIN brochure_item bi ON bi.code = p.code ";
	sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
	sc += " AND bi.brochure_id = "+ m_brochureID +"";
	sc += " AND b.branch_id = "+ m_branchID;
	sc += " ORDER BY p.cat";
//DEBUG("s c=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("<select name=cat ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?");
	Response.Write(m_uriString);
	Response.Write("&cat=' + escape(this.options[this.selectedIndex].value))\"");
	
	Response.Write(">");
	Response.Write("<option value='all'>All</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		sc = "SELECT DISTINCT p.s_cat ";
		sc += " FROM product p JOIN brochure_item bi ON bi.code = p.code ";
		sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
		sc += " AND bi.brochure_id = "+ m_brochureID +"";
		sc += " AND b.branch_id = "+ m_branchID;
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " ORDER BY p.s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?");
		Response.Write("cat=" + HttpUtility.UrlEncode(cat));
		Response.Write(m_uriString);
		Response.Write("&scat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}
		Response.Write("</select>");
	}

	
	if(s_cat != "")
	{
		sc = "SELECT DISTINCT p.ss_cat ";
		sc += " FROM product p JOIN brochure_item bi ON bi.code = p.code ";
		sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
		sc += " AND bi.brochure_id = "+ m_brochureID +"";
		sc += " AND b.branch_id = "+ m_branchID;
		sc += " WHERE p.cat = '"+ cat +"' ";
		sc += " AND p.s_cat = '"+ s_cat +"' ";
		sc += " ORDER BY p.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=ss_cat ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?");
		Response.Write("cat=" + HttpUtility.UrlEncode(cat));
		Response.Write(m_uriString);
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");

		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}
		Response.Write("</select>");
	}
	return true;
}

bool DoBulkAddAll()
{
	if(!GetProductList())
		return false;

	string sc = "";
	int rows = ds.Tables["bulk"].Rows.Count;
	for(int i=0; i<rows; i++)
	{
		sc += " INSERT INTO brochure_item (code, brochure_id) VALUES(" + ds.Tables["bulk"].Rows[i]["code"] + ", "+ m_brochureID +") ";
	}
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

bool DoBulkAddSelected()
{
	string sc = "";
	int row_start = MyIntParse(Request.Form["row_start"]);
	int row_end = MyIntParse(Request.Form["row_end"]);
	for(int i=row_start; i<row_end; i++)
	{
		if(Request.Form["select" + i] == "on")
			sc += " INSERT INTO brochure_item (code, brochure_id) VALUES(" + Request.Form["code" + i] + ", '"+ m_brochureID +"') ";
	}

	if(sc == "")
		return true;

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
void PrintBrochureTitleForm()
{
	tableWidth = "50%";
	Response.Write("<form action="+ Request.ServerVariables["URL"] +" method=post name=f>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Adding Brochure Title</b><font color=red><b>");
	if(Session["branch_support"] != null)
	{
		Response.Write(" <b> - Branch : </b> ");
		PrintBranchNameOptions(m_branchID);
		//Response.Write("</td></tr>");
	}
	Response.Write(" <b>&nbsp;&nbsp;&nbsp;</b> ");
	
	Response.Write("</td><td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><br></td><td></td></tr>");
	Response.Write("<tr align=right><th>Brochure Title: <font color=red>*</font></th><td align=left><input type=text name=title value='' size=70 maxlength=70></td></tr>");
	Response.Write("<tr align=right><td></td><td>");
	Response.Write("<input type=button name=cmd value='Back to Brochure List' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"');\">");
	Response.Write("<input type=reset name=cmd value='Clear Text' "+ Session["button_style"] +">");
	Response.Write("<input type=submit name=cmd value='Add New Title' "+ Session["button_style"] +" onclick=\"if(document.f.title.value==''){window.alert('Please Fill Up Brochure Title!!!'); return false;}\"></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}

bool doAddBrochureTitle()
{
	string title = Request.Form["title"];
	string branchID = Request.Form["branch"];
	if(!TSIsDigit(branchID))
		branchID = "1";
	m_branchID = branchID;
	string sc = " BEGIN TRANSACTION ";
	sc += " INSERT INTO brochure (brochure_name, record_date, record_name, record_by, branch_id) ";
	sc += " VALUES('"+ EncodeQuote(title) +"', GETDATE(), '"+ Session["name"].ToString() +"', '"+ Session["card_id"].ToString() +"', '"+ m_branchID +"')";
	sc += " SELECT IDENT_CURRENT('brochure') AS brochure_id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(ds, "brochure") == 1)
		{
			m_brochureID = ds.Tables["brochure"].Rows[0]["brochure_id"].ToString();
			m_brochureTitle = title;
//DEBUG("id=", m_topicid);	
		}
		else
		{
			Response.Write("<br><br><center><h3>Failed getting IDEN_CURRENT from brochure_ID");
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

bool bCheckExistedItem(string code)
{
	if(code == "" || code == null)
		return false;
	int rows = 0;
	string	sc = "SELECT top 1 p.code ";
	sc += " FROM product p JOIN code_relations c ON c.code=p.code ";		
	if(TSIsDigit(code))
	{
		sc += " WHERE p.code = "+ code +" OR p.supplier_code = '"+ code +"' or c.barcode = '" + code + "' ";
	}
	else
	{
		sc += " WHERE p.supplier_code = '"+ code +"' or c.barcode = '" + code + "' ";
	}
	sc += " ORDER BY p.s_cat";
//	DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "foundProduct");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
		return true;
	else
		return false;
}
string PrintBrochureLayout()
{
string setStyleSheet = "<STYLE> P {page-break-before: always}</STYLE>";
	string sBody = ReadSitePage("brochure_template");
//	string sBody = ReadSitePage("brochure_body");
	string sFooter = ReadSitePage("brochure_footer");
	string sHeader = ReadSitePage("brochure_header");
//	string sBody = "", sFooter = "", sHeader = "";
	string priceLevel = Request.QueryString["price"];
int rows =0;
    string sc = "SELECT ISNULL(sq.qty, 0) AS qty, p.eta, b.brochure_template, c.code, c.supplier_code, c.barcode, c.name";	
	if(TSIsDigit(priceLevel) && int.Parse(priceLevel) >0 && int.Parse(priceLevel) <10)
		sc += ", (c.manual_cost_nzd * c.rate * level_rate"+ priceLevel +") AS price1 ";
	else
		sc += ", (c.price1 / "+ m_dGSTRate +") AS price1";

    sc += " FROM brochure_item bi ";
    sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
    sc += " JOIN code_relations c ON c.code = bi.code ";
	sc += " JOIN product p ON p.code = c.code ";
	sc += " LEFT OUTER JOIN stock_qty sq ON sq.code = bi.code AND sq.branch_id = "+ Session["branch_id"].ToString() +" ";
    sc += " WHERE 1 = 1 ";
//    sc += " AND b.branch_id = " + m_branchID;
	sc += " AND bi.brochure_id = "+ m_brochureID;
    sc += " ORDER BY bi.kid ";
//DEBUG(" sbody =", sc);
    try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "brochureItem");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	if(rows > 0)
	{
//		sBody = dst.Tables["brochureItem"].Rows[0]["brochure_body"].ToString();
//		sFooter = dst.Tables["brochureItem"].Rows[0]["brochure_footer"].ToString();
//		sHeader = dst.Tables["brochureItem"].Rows[0]["brochure_header"].ToString();
		string sMyTemplate = dst.Tables["brochureItem"].Rows[0]["brochure_template"].ToString();
		if(sMyTemplate != "")
			sBody = sMyTemplate;
	}
	
////read header from template page
//string sTPHeader = TemplateParseCommand(sBody);
//DEBUG("slds =", sTPHeader);
//string readHeader = GetRowTemplate(ref sTPHeader, "brochure_header");
string readHeader = GetRowTemplate(ref sBody, "brochure_header");
sHeader = readHeader;
//Response.Write("Header =" + readHeader);
//return "";
////read footer from template page
//string sTPFooter = TemplateParseCommand(sBody);
//string readFooter = GetRowTemplate(ref sTPFooter, "brochure_footer");
string readFooter = GetRowTemplate(ref sBody, "brochure_footer");
sFooter = readFooter;
//Response.Write("footer =" + sFooter);
////read body from template page
string sTemplate = TemplateParseCommand(sBody);
string rowitem = GetRowTemplate(ref sTemplate, "row_item");
//DEBUG("reow =", rowitem);
//return "";

string sTmpSwap = rowitem;
StringBuilder sbRow = new StringBuilder();

//sbRow.Append("<html><header><style type=\"text/css\">\r\n");
//sbRow.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
sbRow.Append(sHeader);
//DEBUG(" each line =", sl);
int nRowsCounter = 0;
    for(int i=0; i<rows; i++)
    {			
        DataRow dr = dst.Tables["brochureItem"].Rows[i];
	
		double dPrice = MyDoubleParse(dr["price1"].ToString());
		string code = dr["code"].ToString();
		string sImgLink = "../pi/" + code + ".jpg";
		if(!File.Exists(Server.MapPath(sImgLink)))
			sImgLink = "";
		sTmpSwap = sTmpSwap.Replace("@@ITEM_CODE", code);
		sTmpSwap = sTmpSwap.Replace("@@ITEM_SUPPLIER_CODE", dr["supplier_code"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_NAME", dr["name"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_BARCODE", dr["barcode"].ToString());
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_IMAGE_LINK", GetProductImgSrc(dr["code"].ToString()));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_IMAGE_LINK", Server.MapPath(sImgLink));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_PRICE", dPrice.ToString("c"));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_GST_PRICE", (dPrice * m_dGSTRate).ToString("c"));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_ETA", dr["eta"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_QTY", dr["qty"].ToString());
		
		sbRow.Append(sTmpSwap);
		if(i>0 && ((i+1)%m_nItemsPerRow)== 0)
        {
			sbRow.Append("</tr><tr>");
            nRowsCounter++;
        }
		if(nRowsCounter > 0 && (nRowsCounter % m_nItemRowsPerPage) == 0)
		{
		//	DEBUG("nRowsCounter =", nRowsCounter + "m_nItemRowsPerPage =" + m_nItemRowsPerPage);
			//sbRow.Append(sFooter + "<P></P>" + sHeader);
			sbRow.Append(sFooter);
            if((rows - 1 ) > i)
                sbRow.Append("<P></P>" + sHeader);
            nRowsCounter = 0;
		}
		else if((rows - 1) == i)
        {
            sbRow.Append(sFooter);
        }
        else
			sbRow.Append(setStyleSheet );
		////cleanup row data...
		sTmpSwap = rowitem;    
    }
//	DEBUG("sd b=", sb.ToString());
//s	sTemplate = sTemplate.Replace("@@template_row_item", sTmpSwap);
    if(rows <0)
    {   
        rowitem = rowitem.Replace("@@ITEM_CODE", "");
        rowitem = rowitem.Replace("@@ITEM_SUPPLIER_CODE", "");
        rowitem = rowitem.Replace("@@ITEM_NAME", "");
        rowitem = rowitem.Replace("@@ITEM_BARCODE", "");
        rowitem = rowitem.Replace("@@ITEM_IMAGE_LINK", "");
        rowitem = rowitem.Replace("@@ITEM_PRICE", "");
		rowitem = rowitem.Replace("@@ITEM_ETA", "");
		rowitem = rowitem.Replace("@@ITEM_QTY", "");
		
        //sBody = sBody.Replace("@@ITEM_PRICE_WITH_GST", "");

    }	
	sTemplate = sTemplate.Replace("@@template_row_item", sbRow.ToString());
sTemplate = sTemplate.Replace("@@template_brochure_header", "");
sTemplate = sTemplate.Replace("@@template_brochure_footer", "");
 //   Response.Write(sTemplate);
	return sTemplate; // + sFooter;
}

string TemplateParseCommand(string tp)
{
	StringBuilder sb = new StringBuilder();

	int line = 0;
	string sline = "";
//DEBUG("tp =", tp);
	bool bRead = ReadLine(tp, line, ref sline);
//DEBUG("sline =",sline);
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf("@@SETITEMS") >= 0)
		{
			string snItems = GetDefineValue("items_per_row", sline);
			if(snItems != "")
				m_nItemsPerRow = MyIntParse(snItems);
//DEBUG("snItem -", snItems);
		}
		else if(sline.IndexOf("@@SETROWS") >= 0)
		{
			string snItemsRows = GetDefineValue("items_rows_per_page", sline);
			if(snItemsRows != "")
				m_nItemRowsPerPage = MyIntParse(snItemsRows);
//DEBUG("snItem -", snItemsRows);
		}
		else
		{
			sb.Append(sline);
		}
		line++;
		bRead = ReadLine(tp, line, ref sline);
	}
//DEBUG("sb =", sb.ToString());
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

void PrintEditForm()
{
	string	sc = "SELECT top 1 "+ m_EditBrochure +", brochure_id ";
	sc += " FROM brochure ";
	sc += " WHERE brochure_id = "+ m_brochureID +"";
	int rows = 0;
//	DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "getPage");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
string sp  = @" <!-- BEGIN brochure_header -->
<table><tr><td align=center> YOUR COMPANY HEADER</td></tr></table>
<table><tr>
<!-- END brochure_header -->

@@SETITEMS items_per_row 3
@@SETROWS items_rows_per_page 3
<!-- BEGIN row_item -->
<td>
<table><tr><td>@@ITEM_NAME</td></tr>
<tr><td>@@ITEM_CODE</td></tr>
<tr><td>@@ITEM_SUPPLIER_CODE</td></tr>
<tr><td>@@ITEM_BARCODE</td></tr>
<tr><td>@@ITEM_PRICE</td></tr>
<tr><td>@@ITEM_GST_PRICE</td></tr>
<tr><td>@@ITEM_ETA</td></tr>
<tr><td>@@ITEM_QTY</td></tr>
<tr><td><img width=90 height=68 border=0 src='@@ITEM_IMAGE_LINK'></td></tr>
</table>
</td>
<!-- END row_item -->


<!-- BEGIN brochure_footer -->
</tr></table>
<table><tr align=center><td> YOUR COMPANY FOOTER</td></tr></table>
<!-- END brochure_footer -->
";
	string id = "";
	string text = dst.Tables["getPage"].Rows[0][""+ m_EditBrochure +""].ToString();
//	text = text.Replace("&nbsp", "nbsp");
	Response.Write("<form action="+ Request.ServerVariables["URL"] +"?ep=" + m_EditBrochure + "&brochureid="+ m_brochureID +" method=post>");
	Response.Write("<input type=hidden name=brochure_id value=" + m_brochureID + ">");
	Response.Write("<input type=hidden name=brochure_name value=" + m_EditBrochure + ">");
	Response.Write("<center><h3>Edit Brochure - <font color=red>" + m_EditBrochure.ToUpper() + "</font></h3>");
	Response.Write("<table border=1>");
	Response.Write("<tr>");
//	Response.Write("<td valign=top><b>TEXT</b></td>");
    Response.Write("Samples How to Build a Template:<br><textarea name=sample rows=6 cols=110>");
    Response.Write(sp);
    Response.Write("</textarea>");
    Response.Write("</td></tr><tr>");
	Response.Write("<td><textarea name=txt rows=25 cols=110>");
	Response.Write(HttpUtility.HtmlEncode(text));
	Response.Write("</textarea>");
	Response.Write("<tr><td align=center>");
	Response.Write("<input type=submit name=cmd value='Save' "+ Session["button_style"] +">");
	Response.Write("<input type=reset value=Cancel "+ Session["button_style"] +">");	
	Response.Write("<input type=button value='Close This Window' onclick=\"window.close(); \" "+ Session["button_style"] +">");	
	Response.Write("</td></tr></table>");
	Response.Write("</form>");
}

bool doSaveTemplate()
{
	string name = Request.Form["brochure_name"];
	string name_value = Request.Form["txt"];

	string ID = Request.Form["brochure_id"];
	if(!TSIsDigit(ID))
		return false;
	
	string sc = " BEGIN TRANSACTION ";
	sc += " UPDATE brochure set " + name + " =  '" + EncodeQuote(name_value) + "' WHERE brochure_id = " + ID + " ";	
	sc += " COMMIT ";
//DEBUG(" dsfs =", sc);
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

bool DoCreatePdf()
{
	string st = PrintBrochureLayout();
	string sDate = DateTime.Now.ToString("ddMMyyyy");
	string sFN = "q_" + sDate;
	string sPath = "doc/" + sFN + ".pdf";
//	HtmToPdf(st, "doc/" + sFN + ".pdf");
//	FontFactory.Register("c:\\windows\\fonts\\simfang.ttf", "simfang");
	FontFactory.Register("c:\\windows\\fonts\\arial.ttf", "arial");
	StyleSheet style = new StyleSheet();
	style.LoadTagStyle("body", "face", "arial");
	style.LoadTagStyle("body", "encoding", "Identity-H");
	Document document = new Document(PageSize.A4);
	try
	{
//		List<IElement> ae = HTMLWorker.ParseToList(new StringReader(st), style);
		ArrayList ae = HTMLWorker.ParseToList(new StringReader(st), style);
		if(ae.Count <= 0)
		{
			ErrMsgAdmin("no product found");
			return false;
		}
		PdfWriter.GetInstance(document, new FileStream(Server.MapPath(sPath), FileMode.Create));
		document.Open();
		for (int i=0; i<ae.Count; i++)
		{
			document.Add((IElement)ae[i]);
		}
	}
	catch(DocumentException de) 
	{
		document.Close();
		Response.Write(de.Message);
	}
	catch(IOException ioe) 
	{
		document.Close();
		Response.Write(ioe.Message);
	}
	document.Close();


	string subject = Lang("Quote") + " " + sFN; 
	Response.Write("<br><center><h3>" + Lang("PDF") + " " + Lang("Created") + "</h3>");
	Response.Write("<a href=" + sPath + " target=_blank class=o><img src=i/pdf2.jpg border=0><br><b>" + Lang("Right click to save file") + "</b></a><br></center>");
//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + sfn + "\">");			
	Response.Write("<form name=f action=?a=email method=post>");
	Response.Write("<input type=hidden name=file_path value='" + sPath + "'>");
	Response.Write("<br>");
	Response.Write("<table align=center class=t border=1>");
	Response.Write("<tr><th colspan=2>" + Lang("Email") + " " + Lang("Quote") + "</th></tr>");
	Response.Write("<tr><td>" + Lang("Email") + " :&nbsp;</th><td><input type=text size=50 name=email value=''></td></tr>");
	Response.Write("<tr><td>" + Lang("Subject") + " :&nbsp;</th><td><input type=text name=subject size=50 value='" + subject + "'></td></tr>");
	Response.Write("<tr><td>" + Lang("Note") + " :&nbsp;</th><td><textarea name=body cols=40 rows=5></textarea></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit class=b name=cmd value='" + Lang("Send") + "' ");
	Response.Write(" onclick=\"if(document.f.email.value==''||document.f.email.value.indexOf('@')<=0)");
	Response.Write("{window.alert('" + Lang("Please enter a valid email address") + "');document.f.email.select();return false;}\">");
	Response.Write("</td></tr></table></form>");
	Response.Write("<script language=javascript>document.f.email.focus();</script");
	Response.Write(">");
//	PrintAdminFooter();
	return true;
}
bool DoSendEmail()
{
	MailMessage msgMail = new MailMessage();

	string afile = p("file_path");
	if(afile != "")
	{
		afile = Server.MapPath(afile);
		if(File.Exists(afile))
		{
			MailAttachment attach = new MailAttachment(afile);
			msgMail.Attachments.Add(attach);
		}
	}
	string sTo = p("email");
	string sFrom = GetSiteSettings("postmaster_email", "postmaster@cnlinden.com");
	if(sFrom == "")
		sFrom = "postmaster@cnlinden.com";
	msgMail.From = sFrom;
	msgMail.To = sTo;
	msgMail.Bcc = Session["email"].ToString();
	msgMail.Headers.Add("Reply-To", GetSiteSettings("sales_email", ""));
	msgMail.Subject = p("subject");
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = p("body");
	msgMail.Body = msgMail.Body.Replace("\r\n", "<br>\r\n");
//	msgMail.Body = msgMail.Body.Replace("<br>", "\r\n<br>");
	msgMail.Body += "\r\n<br>\r\n<br>";

	SmtpMail.Send(msgMail);

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h3>" + Lang("Done") + ", " + Lang("Email has send to") + " " + sTo + "</h3>");
	Response.Write("<input type=button value='" + Lang("New Quote") + "' class=b onclick=\"window.location=('?');\">");
	PrintAdminFooter();
	return true;
}
</script>


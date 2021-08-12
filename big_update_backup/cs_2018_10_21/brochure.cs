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

string m_cat = "";
string m_s_cat = "";
string m_ss_cat = "";
string m_code = "";
string tableWidth = "91%";
string m_branchID = "1";
string m_brochureID = "";
string m_brochureTitle = "";
string m_uriString = "";
string m_EditBrochure = "";
double m_dGSTRate = 0.15;
int m_nItemsPerRow = 1;
int m_nItemRowsPerPage = 1;
int total = 0;
string m_action = "";
string m_sPriceLevel = "price1";

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

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		m_s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		m_ss_cat = Request.QueryString["sscat"];
	Trim(ref m_cat);
	Trim(ref m_s_cat);
	Trim(ref m_ss_cat);

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

	if(Request.QueryString["price"] != null)
		m_sPriceLevel = Request.QueryString["price"];

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
	if(Request.QueryString["price"] != null && Request.QueryString["price"] != "" && m_brochureID != "")
	{
		if(Request.QueryString["print"] == "1")
		{
			PrintBrochureLayout();
			Response.Write(PrintBrochureLayout());
		}
		else if(Request.QueryString["pdf"] == "1")
		{
			if(m_cat != "")
				CreatePDFFileBrochure();
			else
				CreatePDFFileStock();
		}
		return;
	}
	if(Request.QueryString["add"] == "new")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		PrintBrochureTitleForm();
		PrintAdminFooter();
		return;
	}			
	if(Request.QueryString["delall"] != null && Request.QueryString["delall"] != "")
	{
		if(DoDelete(Request.QueryString["delall"].ToString(), true))
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(m_cat)+"&scat="+HttpUtility.UrlEncode(m_s_cat)+"&sscat="+HttpUtility.UrlEncode(m_ss_cat)+ m_uriString +"\">");
		return;
	}
	if(Request.QueryString["delallitem"] != null && Request.QueryString["delallitem"] != "")
	{
		if(DoDelete(Request.QueryString["delallitem"].ToString(), true, true))
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(m_cat)+"&scat="+HttpUtility.UrlEncode(m_s_cat)+"&sscat="+HttpUtility.UrlEncode(m_ss_cat)+ m_uriString +"\">");
		return;
	}
	if(Request.Form["cmd"] == "Save")
	{
		doSaveTemplate();
	}
	if(m_EditBrochure != null && m_EditBrochure != "" && m_brochureID != null && m_brochureID != "")
	{
		PrintAdminHeader();
		PrintAdminMenu();		
		PrintEditForm();
		PrintAdminFooter();
		return;
	}
	if(m_brochureID != null && m_brochureID != "" )
	{
		if(m_action == "bulk")
		{
			if(Request.Form["cmd"] == "Add All Pages")
			{
				if(DoBulkAddAll())
				{
					Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?t=bulk");
					Response.Write("&cat=" + HttpUtility.UrlEncode(m_cat));
					Response.Write("&scat=" + HttpUtility.UrlEncode(m_s_cat));
					Response.Write("&sscat=" + HttpUtility.UrlEncode(m_ss_cat));
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
					Response.Write("&cat=" + HttpUtility.UrlEncode(m_cat));
					Response.Write("&scat=" + HttpUtility.UrlEncode(m_s_cat));
					Response.Write("&sscat=" + HttpUtility.UrlEncode(m_ss_cat));
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
				Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(m_cat)+"&scat="+HttpUtility.UrlEncode(m_s_cat)+"&sscat="+HttpUtility.UrlEncode(m_ss_cat)+ m_uriString +"\">");
			return;
		}
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
}
bool GetBrochureTable()
{
	string cat = EncodeQuote(m_cat.ToLower());
	string s_cat = EncodeQuote(m_s_cat.ToLower());
	string ss_cat = EncodeQuote(m_ss_cat.ToLower());
	string sc = " SELECT bi.kid, b.brochure_name, b.brochure_id, bi.code, c.supplier_code, c.name, c.cat, c.s_cat, c.ss_cat ";
	sc += " FROM brochure b ";
	sc += " JOIN brochure_item bi ON b.brochure_id = bi.brochure_id ";
	sc += " JOIN code_relations c ON c.code = bi.code ";
//	sc += " JOIN product p ON p.code = bi.code ";
	sc += " WHERE 1=1 ";
	if(cat != "")
	{
		sc += " AND Lower(c.cat) = '" + cat + "' ";		
	}
	if(s_cat != "")
	{		
		sc += " AND Lower(c.s_cat) = '" + s_cat + "' ";
	}
	if(ss_cat != "")
	{		
		sc += " AND LOWER(c.ss_cat) = '" + ss_cat + "' ";
	}
	if(m_code != null && m_code != "")
	{		
		if(TSIsDigit(m_code))
			sc += " AND bi.code = " + m_code;
		else
			sc += " AND (b.supplier_code = '" + m_code + "' OR c.barcode = '" + m_code + "' )" ;
	}
	sc += " AND b.brochure_id = " + m_brochureID + " AND b.branch_id = " + m_branchID + "";
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
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetBrochureData()
{
	string sc = " SELECT c.code, c.name, c.cat, c.s_cat, c.ss_cat, c.price1 AS price, c.level_rate1, c.level_rate2, c.level_rate3, c.level_rate4,c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9 ";
	sc += " FROM brochure b JOIN brochure_item bi ON bi.brochure_id = b.brochure_id ";
	sc += " JOIN code_relations c ON c.code = bi.code ";
//	sc += " JOIN product p ON p.code = bi.code ";
	sc += " ORDER BY c.cat, c.s_cat, c.ss_cat, c.name ";
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
		ShowExp(sc, e);
		return false;
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
	Response.Write("<input type=button name='cmd' value='Add New Brochure' class=b ");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&add=new');\">");
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
		Response.Write("<input type=button value='EDIT' class=b onclick=\"window.location=('"+ Request.QueryString["URL"] +"?brochureid="+ brochure_id +"&title="+ HttpUtility.UrlEncode(title) +"&branch="+ branch_id+"');\">");
		Response.Write("<input type=button value='X' class=b onclick=\"if(confirm('Are you sure to delete all!!!')){window.location=('"+ Request.QueryString["URL"] +"?delall="+ brochure_id +"');}else{return false;}\"></td>");
//		Response.Write("<a href=liveedit.aspx?code=" + dr["code"].ToString() + " class=o target=_blank>EDIT</a> &nbsp; ");
//		Response.Write("<a href="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"&t=del&id=" + dr["id"].ToString() + " class=o>DEL</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=" + (cols-6) + ">" + sPageIndex + "</td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=button name='cmd' value='Add New Brochure' class=b ");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&add=new');\">");
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
	return DoDelete(brochureID, bDeleteAll, false); ///2nd false is delete brochure and brochure items
}
bool DoDelete(string brochureID, bool bDeleteAll, bool bDeleteAllItemOnly)
{
	string sc = "";
	if(bDeleteAll)
	{
		sc = " DELETE FROM brochure_item ";
		sc += " WHERE brochure_id = "+ brochureID;
		if(!bDeleteAllItemOnly)
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
	if(!bCheckExistedItem(Request.Form["code"]))
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>Error, product not found. code = " + Request.Form["code"] + "</h3>");
		return false;
	}
	string sc = " IF NOT EXISTS (SELECT * FROM brochure_item WHERE ";
	sc += " code = " + Request.Form["code"] + ") ";
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
	Response.Write("<option value='price2'>Dealer Price</option>");
//	for(int i=1; i<=nDealerLevel; i++)
//	{
//		Response.Write("<option value="+ i +">Dealer Level: "+ i +"</option>");
//	}
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
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width=100% valign=center bgcolor=white border=0><tr>");
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
	Response.Write("<input type=submit name=cmd value=Add class=b>");
	Response.Write("<input type=button name=cmd value='Bulk Add' ");
	Response.Write(" onclick=window.location=('"+ Request.ServerVariables["URL"] +"?t=bulk");
	Response.Write("&brochureid="+ m_brochureID +"&title="+ HttpUtility.UrlEncode(m_brochureTitle) +"&branch="+ m_branchID +"");
	if(Request.QueryString["add"] != null)
		Response.Write("&add="+Request.QueryString["add"]);
	Response.Write("') class=b>");
	Response.Write("<input type=button name=cmd value='Brochure List' class=b onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"');\">");
	
	Response.Write("<br>Select Existed Catagories: "); PrintCatsForDisplay();
	Response.Write("</td>");
	Response.Write("<td valign=top colspan=4 align=right nowrap>");
	Response.Write("<input type=button onclick=window.open('"+ Request.ServerVariables["URL"] +"?ep=brochure_template"+ m_uriString +"') value='Edit Template' class=b>");
	Response.Write("&nbsp&nbsp&nbsp;Select Pricing Level:");
	
	DoShowDealerLevel();
	
	Response.Write("<input type=button onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?brochureid="+ m_brochureID +"&pdf=1&price='+ document.f.dealer_level.value);\" value='PDF' class=b>");
	Response.Write("<input type=button onclick=\"window.open('"+ Request.ServerVariables["URL"] +"?brochureid="+ m_brochureID +"&print=1&price='+ document.f.dealer_level.value);\" value='Print' class=b>");
	Response.Write("</td></tr>");

	Response.Write("<tr align=left style=\"color:white;background-color:#336699;font-weight:bold;\">");
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
		Response.Write("<a href="+ Request.ServerVariables["URL"] +"?cat="+HttpUtility.UrlEncode(m_cat)+ m_uriString +"&scat="+HttpUtility.UrlEncode(m_s_cat)+"&sscat="+HttpUtility.UrlEncode(m_ss_cat)+"&t=del&kid=" + dr["kid"].ToString() + " class=o>DEL</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=" + (cols-1) + ">" + sPageIndex + "</td><td align=right>");
	Response.Write("<input type=button value='Delete All Items in This Brochure' "+ Session["button_style"] +" onclick=\"if(confirm('Are you sure to delete all items!!!')){window.location=('"+ Request.QueryString["URL"] +"?delallitem="+ m_brochureID +"');}else{return false;} \">");
	Response.Write("<input type=button value='Delete This Brochure' "+ Session["button_style"] +" onclick=\"if(confirm('Are you sure to delete all!!!')){window.location=('"+ Request.QueryString["URL"] +"?delall="+ m_brochureID +"');}else{return false;}\"></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
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
	m_cPI.URI += "&cat=" + HttpUtility.UrlEncode(m_cat);
	m_cPI.URI += "&scat=" + HttpUtility.UrlEncode(m_s_cat);
	m_cPI.URI += "&sscat=" + HttpUtility.UrlEncode(m_ss_cat);

	m_cPI.PageSize = 20;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

//	Response.Write("<br><center><h3>Brochure Template - Bulk Add</h3>");

	Response.Write("<form action="+ Request.ServerVariables["URL"] +"?t=bulk");
	Response.Write("&cat=" + HttpUtility.UrlEncode(m_cat));
	Response.Write("&scat=" + HttpUtility.UrlEncode(m_s_cat));
	Response.Write("&sscat=" + HttpUtility.UrlEncode(m_ss_cat));
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
	Response.Write("<input type=button onclick=\"window.open('?cat=" + HttpUtility.UrlEncode(m_cat));
	Response.Write("&scat=" + HttpUtility.UrlEncode(m_s_cat) + "&sscat=" + HttpUtility.UrlEncode(m_ss_cat));
//	Response.Write("&brochureid=" + m_brochureID + "&pdf=1&price=' + document.f.dealer_level.value);\" value='PDF' class=b>");
	Response.Write("&brochureid=" + m_brochureID + "&pdf=1&price=price2');\" value='PDF' class=b>");
	Response.Write("</td>");

	Response.Write("<td colspan=4 align=right>");
	Response.Write("<b>Total </b><font color=red><b>" + total + "</b></font><b> items found</b>&nbsp&nbsp;");
	Response.Write("<input type=submit name=cmd value='Add Selected' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Add All Pages' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr align=left style=\"color:white;background-color:#336696;font-weight:bold;\">");
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

	Response.Write("<tr><td colspan=8 align=right>");
	Response.Write("<input type=button value='Show Added Item' onclick=window.location=('"+ Request.ServerVariables["URL"] + "?"+ m_uriString +"') class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");	
	return true;
}
bool GetProductList()
{
	string sc = "";
	sc = " SELECT DISTINCT p.code, p.supplier_code, p.cat, p.s_cat, p.ss_cat, p.name, s.qty AS quantity ";
	sc += " FROM product p ";
	sc += " JOIN code_relations c ON c.code = p.code ";
	sc += " JOIN stock_qty s ON s.code = p.code";
	sc += " WHERE 1 = 1 ";
	if(m_cat == "new_arrival_ticked")
	{
		sc += " AND c.new_item = 1 ";
	}
	else
	{
		sc += " AND p.cat = '" + EncodeQuote(m_cat) + "' ";
		if(m_s_cat != "" && m_s_cat != "all")
			sc += " AND p.s_cat = '" + EncodeQuote(m_s_cat) + "' ";
		if(m_ss_cat != "" && m_ss_cat != "all")
			sc += " AND p.ss_cat = '" + EncodeQuote(m_ss_cat) + "' ";
	}
	sc += " AND s.branch_id ="+ m_branchID +"";
	sc += " ORDER BY p.cat, p.s_cat, p.ss_cat, p.name ";
//DEBUG("sc=", sc);	
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
	string sc = "SELECT DISTINCT c.cat ";
	sc += " FROM code_relations c ";
	sc += " JOIN stock_qty s ON s.code = c.code ";
	sc += " AND s.branch_id = "+ m_branchID +"";
	sc += " ORDER BY c.cat";
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
	Response.Write("<option value='new_arrival_ticked'>Item ticked as New Arrival</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(m_cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(m_cat != "")
	{
		sc = "SELECT DISTINCT c.s_cat ";
		sc += " FROM code_relations c ";
		sc += " JOIN stock_qty s ON s.code = c.code ";
		sc += " WHERE c.cat = '" + EncodeQuote(m_cat) + "' ";
		sc += " AND s.branch_id = " + m_branchID + "";
		sc += " ORDER BY c.s_cat ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
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
		Response.Write("&cat=" + HttpUtility.UrlEncode(m_cat));
		Response.Write("&scat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(m_s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}
		Response.Write("</select>");
	}
	
	if(m_s_cat != "")
	{
		sc = "SELECT DISTINCT c.ss_cat ";
		sc += " FROM code_relations c ";
		sc += " JOIN stock_qty s ON s.code = c.code ";
		sc += " WHERE c.cat = '" + EncodeQuote(m_cat) +"' ";
		sc += " AND c.s_cat = '"+ EncodeQuote(m_s_cat) +"' ";
		sc += " AND s.branch_id = "+ m_branchID +"";
		sc += " ORDER BY c.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
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
		Response.Write("&cat=" + HttpUtility.UrlEncode(m_cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(m_s_cat));
		Response.Write("&sscat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");

		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(m_ss_cat == s)
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
	string sc = "SELECT DISTINCT c.cat ";
	sc += " FROM code_relations c JOIN brochure_item bi ON bi.code = c.code ";
	sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
	sc += " AND bi.brochure_id = "+ m_brochureID +"";
	sc += " AND b.branch_id = "+ m_branchID;
	sc += " ORDER BY c.cat";
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
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(m_cat == s)
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(m_cat != "")
	{
		sc = "SELECT DISTINCT p.s_cat ";
		sc += " FROM code_relations p JOIN brochure_item bi ON bi.code = p.code ";
		sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
		sc += " AND bi.brochure_id = " + m_brochureID + "";
		sc += " AND b.branch_id = " + m_branchID;
		sc += " WHERE p.cat = '" + EncodeQuote(m_cat) + "' ";
		sc += " ORDER BY p.s_cat ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
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
		Response.Write("cat=" + HttpUtility.UrlEncode(m_cat));
		Response.Write(m_uriString);
		Response.Write("&scat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(m_s_cat == s)
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
			
		}
		Response.Write("</select>");
	}

	
	if(m_s_cat != "")
	{
		sc = "SELECT DISTINCT p.ss_cat ";
		sc += " FROM code_relations p JOIN brochure_item bi ON bi.code = p.code ";
		sc += " JOIN brochure b ON b.brochure_id = bi.brochure_id ";
		sc += " AND bi.brochure_id = " + m_brochureID + "";
		sc += " AND b.branch_id = " + m_branchID;
		sc += " WHERE p.cat = '" + EncodeQuote(m_cat) + "' ";
		sc += " AND p.s_cat = '" + EncodeQuote(m_s_cat) + "' ";
		sc += " ORDER BY p.ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
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
		Response.Write("cat=" + HttpUtility.UrlEncode(m_cat));
		Response.Write(m_uriString);
		Response.Write("&scat=" + HttpUtility.UrlEncode(m_s_cat));
		Response.Write("&sscat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");

		Response.Write("<option value='all'>All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(m_ss_cat == s)
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
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "foundProduct");
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
bool GetBrochureItem()
{
	if(dst.Tables["brochureItem"] != null)
		dst.Tables["brochureItem"].Clear();

	string sc = "SELECT c.cat, c.s_cat, ISNULL(sq.qty, 0) AS qty, p.eta, b.brochure_template, c.code, c.supplier_code, c.barcode, c.name";	
	sc += ", (c.price1 / "+ m_dGSTRate +") AS price1 ";
	sc += ", (c.price2 / "+ m_dGSTRate +") AS price2, c.manual_cost_nzd * c.rate + nzd_freight AS bottom_price, c.level_rate1, c.level_price0  ";
//	sc += ", c.inner_pack, c.outer_pack ";
	sc += " FROM brochure_item bi JOIN brochure b ON b.brochure_id = bi.brochure_id ";
	sc += " JOIN code_relations c ON c.code = bi.code ";
	sc += " JOIN product p ON p.code = c.code ";
	sc += " LEFT OUTER JOIN stock_qty sq ON sq.code = bi.code AND sq.branch_id = "+ Session["branch_id"].ToString() +" ";
	sc += " WHERE b.branch_id = "+ m_branchID;
	sc += " AND bi.brochure_id = "+ m_brochureID;
	sc += " AND b.brochure_id = "+ m_brochureID;
	sc += " AND c.is_service = 0 ";
	sc += " AND c.skip = 0 ";
	sc += " ORDER BY bi.kid ";
	
	if(m_cat != "" )
	{
		sc = "SELECT c.cat, c.s_cat, ISNULL(sq.qty, 0) AS qty, '' AS eta, '' AS brochure_template, c.code, c.supplier_code, c.barcode, c.name";	
		sc += ", (c.price1 / "+ m_dGSTRate +") AS price1 ";
		sc += ", (c.price2 / "+ m_dGSTRate +") AS price2 ";
//		sc += ", c.inner_pack, c.outer_pack ";
		sc += " FROM code_relations c ";
		//sc += " JOIN product p ON p.code = c.code ";
		sc += " LEFT OUTER JOIN stock_qty sq ON sq.code = c.code AND sq.branch_id = "+ Session["branch_id"].ToString() +" ";
		sc += " WHERE 1 = 1 AND c.cat <> 'ServiceItem' ";
		sc += " AND LOWER(c.cat) NOT LIKE '%invisible%' ";
		sc += " AND c.is_service = 0 ";
		sc += " AND c.skip = 0 ";
		if(m_cat == "new_arrival_ticked")
		{
			sc += " AND c.new_item = 1 ";
		}
		else
		{
			if(m_cat != "all")
				sc += " AND c.cat = '" + EncodeQuote(m_cat) + "' ";
			if(m_s_cat != "all" && m_s_cat != "")
				sc += " AND c.s_cat = '" + EncodeQuote(m_s_cat) + "' ";
			if(m_ss_cat != "all" && m_ss_cat != "")
				sc += " AND c.ss_cat = '" + EncodeQuote(m_ss_cat) + "' ";
		}
		sc += " ORDER BY c.cat, c.s_cat, c.ss_cat, c.name ";
		//sc += " ORDER BY c.code";
	}
//DEBUG("sc=", sc);    
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "brochureItem");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}
string PrintBrochureLayout()
{
	string setStyleSheet = "<STYLE> P {page-break-before: always}</STYLE>";
	string sBody = "", sFooter = "", sHeader = "";
	sBody = ReadSitePage("brochure");
	if(!GetBrochureItem())
		return "";
	int rows = dst.Tables["brochureItem"].Rows.Count;
//	if(rows > 0)
//	{
//		sBody = dst.Tables["brochureItem"].Rows[0]["brochure_template"].ToString();
//	}
	
	string readHeader = GetRowTemplate(ref sBody, "brochure_header");
	sHeader = readHeader;

	if(rows > 0)
	{
		sBody = sBody.Replace("@@CAT", dst.Tables["brochureItem"].Rows[0]["cat"].ToString());
		sBody = sBody.Replace("@@SCAT", dst.Tables["brochureItem"].Rows[0]["s_cat"].ToString());
		readHeader = readHeader.Replace("@@CAT", dst.Tables["brochureItem"].Rows[0]["cat"].ToString());
		readHeader = readHeader.Replace("@@SCAT", dst.Tables["brochureItem"].Rows[0]["s_cat"].ToString());    
		sHeader = sHeader.Replace("@@CAT", dst.Tables["brochureItem"].Rows[0]["cat"].ToString());
		sHeader = sHeader.Replace("@@SCAT", dst.Tables["brochureItem"].Rows[0]["s_cat"].ToString());    
	}
	else
	{
		sBody = sBody.Replace("@@CAT", "");
		sBody = sBody.Replace("@@SCAT", "");
		readHeader = readHeader.Replace("@@CAT", "");
		readHeader = readHeader.Replace("@@SCAT", "");    
		sHeader = sHeader.Replace("@@CAT", "");
		sHeader = sHeader.Replace("@@SCAT", "");  

	}
	string readFooter = GetRowTemplate(ref sBody, "brochure_footer");
	sFooter = readFooter;
	string sTemplate = TemplateParseCommand(sBody);
	string rowitem = GetRowTemplate(ref sTemplate, "row_item");

	string sTmpSwap = rowitem;
	StringBuilder sbRow = new StringBuilder();

	sbRow.Append(sHeader);
	int nRowsCounter = 0;
	int nNameLen = MyIntParse(GetSiteSettings("broucher_item_name_length", "12"));
	if(nNameLen <= 0)
		nNameLen = 4;
	for(int i=0; i<rows; i++)
	{			
		DataRow dr = dst.Tables["brochureItem"].Rows[i];
	
//		double dBottomPrice = MyDoubleParse(dr["bottom_price"].ToString());        
//		double dRate = MyDoubleParse(dr["level_rate1"].ToString());
//		double dPrice = dBottomPrice * dRate;
		double dPrice = MyDoubleParse(dr["level_price0"].ToString());
//		if(m_sPriceLevel == "price2")
//			dPrice = MyDoubleParse(dr["price2"].ToString());       
		string sImagePath = GetProductImgSrc(dr["code"].ToString()).ToLower();
		string item_name = dr["name"].ToString();
		if(item_name.Length > nNameLen)
			item_name = item_name.Substring(0, nNameLen);

		string supplier_code = dr["supplier_code"].ToString();
		string simg = Server.MapPath("../pi/" + supplier_code + ".jpg");
		if(!File.Exists(simg))
			continue;

//		sImagePath = sImagePath.Replace("pi/", "pi/t/"); 
		sTmpSwap = sTmpSwap.Replace("@@ITEM_CODE", dr["code"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_SUPPLIER_CODE", dr["supplier_code"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_NAME", item_name);
		sTmpSwap = sTmpSwap.Replace("@@ITEM_BARCODE", dr["barcode"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_IMAGE_LINK", simg);
		sTmpSwap = sTmpSwap.Replace("@@ITEM_PRICE", dPrice.ToString("c"));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_GST_PRICE", (dPrice * m_dGSTRate).ToString("c"));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_ETA", dr["eta"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_QTY", dr["qty"].ToString());		
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_INNER", dr["inner_pack"].ToString());
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_OUTER", dr["outer_pack"].ToString());

		sbRow.Append(sTmpSwap);
		if(i>0 && ((i+1)%m_nItemsPerRow)== 0)
		{
			sbRow.Append("</tr><tr>");
			nRowsCounter++;
		}
		if(nRowsCounter > 0 && (nRowsCounter % m_nItemRowsPerPage) == 0)
		{
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
		sTmpSwap = rowitem;    
	}
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
		rowitem = rowitem.Replace("@@ITEM_INNER", "");
		rowitem = rowitem.Replace("@@ITEM_OUTER", "");
		rowitem = rowitem.Replace("@@CAT", "");
		rowitem = rowitem.Replace("@@SCAT", "");
		//sBody = sBody.Replace("@@ITEM_PRICE_WITH_GST", "");

	}	
	sTemplate = sTemplate.Replace("@@template_row_item", sbRow.ToString());
	sTemplate = sTemplate.Replace("@@template_brochure_header", "");
	sTemplate = sTemplate.Replace("@@template_brochure_footer", "");
	return sTemplate; // + sFooter;
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
		if(sline.IndexOf("@@SETITEMS") >= 0)
		{
			string snItems = GetDefineValue("items_per_row", sline);
			if(snItems != "")
				m_nItemsPerRow = MyIntParse(snItems);
		}
		else if(sline.IndexOf("@@SETROWS") >= 0)
		{
			string snItemsRows = GetDefineValue("items_rows_per_page", sline);
			if(snItemsRows != "")
				m_nItemRowsPerPage = MyIntParse(snItemsRows);
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
void PrintEditForm()
{
	string	sc = "SELECT top 1 "+ m_EditBrochure +", brochure_id ";
	sc += " FROM brochure ";
	sc += " WHERE brochure_id = "+ m_brochureID +"";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "getPage");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
string sp  = @" <!-- BEGIN brochure_header -->
<table><tr><td align=center> YOUR COMPANY HEADER</td></tr></table>
<tr><td>@@CAT</td><td>@@SCAT</td></tr>
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
<tr><td>@@ITEM_INNER</td></tr>
<tr><td>@@ITEM_OUTER</td></tr>
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
	Response.Write("<input type=submit name=cmd value='Save' class=b>");
	Response.Write("<input type=reset value=Cancel class=b>");	
	Response.Write("<input type=button value='Close This Window' onclick=\"window.close(); \" class=b>");	
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
	sc += " UPDATE brochure SET " + name + " = '" + EncodeQuote(name_value) + "' WHERE brochure_id = " + ID + " ";	
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
bool CreatePDFFileStock()
{
	string st = PrintBrochureLayout();
	string sFN = "temp/SD_B_" + m_brochureID + "_" + DateTime.Now.ToString("yy_MM_dd") + ".pdf";
	string sPath = sFN;
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
		for (int j=0; j<ae.Count; j++)
		{
			document.Add((IElement)ae[j]);
		}
	}
	catch(DocumentException de) 
	{
		document.Close();
		Response.Write(de.Message);
		return false;
	}
	catch(IOException ioe) 
	{
		document.Close();
		Response.Write(ioe.Message);
		return false;
	}
	document.Close();
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + sFN + "\">");			
	return true;
}
bool CreatePDFFileStockOLD()
{
	if(!GetBrochureItem())
		return false;
	int nRows = dst.Tables["brochureItem"].Rows.Count;
	if(nRows <= 0)
	{
		Response.Write("<br><center><h3>There is no item in this brochure");
		return false;
	}
	
	DataRow dr = dst.Tables["brochureItem"].Rows[0];
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	
	string sfn = "temp/SD_B_" + m_brochureID + "_" + DateTime.Now.ToString("yy_MM_dd") + ".pdf";
	Document document = new Document(PageSize.A4);
	try 
	{
		PdfWriter.GetInstance(document, new FileStream(Server.MapPath(sfn), FileMode.Create));
//		document.AddTitle("ACQ Development Ltd. Brochure");
//		document.AddHeader("name", "title");
		document.Open();
//		iTextSharp.text.Font ften = FontFactory.GetFont(FontFactory.HELVETICA, 10);
		iTextSharp.text.Font ften = new iTextSharp.text.Font(iTextSharp.text.Font.HELVETICA, 10, iTextSharp.text.Font.NORMAL);
		iTextSharp.text.Font fbold = new iTextSharp.text.Font(iTextSharp.text.Font.HELVETICA, 12, iTextSharp.text.Font.BOLD);
		
		iTextSharp.text.Table aTable = new iTextSharp.text.Table(3);
		aTable.SetWidths(new int[]{47, 6, 47});
//		aTable.AutoFillEmptyCells = true;
		aTable.Border = 0;
		aTable.Width = 100f;
		aTable.BorderWidth = 1;
		aTable.BackgroundColor = new iTextSharp.text.Color(250, 250, 250);
		aTable.BorderColor = new iTextSharp.text.Color(0, 190, 255);
		aTable.Padding = 1;
		aTable.Spacing = 2;
		aTable.DefaultCell.Border = 0;
		aTable.DefaultCell.BorderColor = new iTextSharp.text.Color(100, 255, 255);

		iTextSharp.text.Cell cell = new iTextSharp.text.Cell(new Chunk(cat + " - " + s_cat, fbold));
		cell.Colspan = 3;
		cell.HorizontalAlignment = Element.ALIGN_CENTER;
		aTable.AddCell(cell);

		string code = "";
		string supplier_code = "";
		string simg = "";
		string name = "";
		string mpn = "";
		string moq = "";
		string inner = "";
		string outer = "";
		double dPrice = 0;
		int m = 0;
		for(int i=0; i<nRows; i++)
		{			
			dr = dst.Tables["brochureItem"].Rows[i];
			code = dr["code"].ToString();
			supplier_code = dr["supplier_code"].ToString();
//			simg = Server.MapPath("../pi/t/" + code + ".jpg");
			simg = Server.MapPath("../pi/" + supplier_code + ".jpg");
			if(!File.Exists(simg))
				continue;
			name = dr["name"].ToString();
//			inner = dr["inner_pack"].ToString();
//			outer = dr["outer_pack"].ToString();
			name += "[" + inner + "/" + outer + "]";
			mpn = dr["supplier_code"].ToString();
			dPrice = MyDoubleParse(dr["price1"].ToString());
			if(m_sPriceLevel == "price2")
				dPrice = MyDoubleParse(dr["price2"].ToString());
			
			iTextSharp.text.Table iTable = new iTextSharp.text.Table(2);
			iTable.Width = 98f;
			iTable.Border = iTextSharp.text.Rectangle.BOX;
			iTable.BorderWidth = 1;
			iTable.DefaultCell.Border = 1;
			iTable.AddCell(new iTextSharp.text.Cell(new Chunk("Code : ", ften)));
			cell = new iTextSharp.text.Cell(new Chunk(code, ften));
			cell.HorizontalAlignment = Element.ALIGN_RIGHT;
			iTable.AddCell(cell);
			
			iTable.AddCell(new iTextSharp.text.Cell(new Chunk("M_PN : ", ften)));
			cell = new iTextSharp.text.Cell(new Chunk(mpn, ften));
			cell.HorizontalAlignment = Element.ALIGN_RIGHT;
			iTable.AddCell(cell);

			iTable.AddCell(new iTextSharp.text.Cell(new Chunk("Minimum Qty : ", ften)));
			cell = new iTextSharp.text.Cell(new Chunk(moq, ften));
			cell.HorizontalAlignment = Element.ALIGN_RIGHT;
			iTable.AddCell(cell);

			iTable.AddCell(new iTextSharp.text.Cell(new Chunk("Price : ", ften)));
			cell = new iTextSharp.text.Cell(new Chunk(dPrice.ToString("C"), ften));
			cell.HorizontalAlignment = Element.ALIGN_RIGHT;
			iTable.AddCell(cell);
						
			iTextSharp.text.Table pTable = new iTextSharp.text.Table(2);
			pTable.Width = 98f;
			pTable.SetWidths(new int[]{40, 60});
			pTable.BackgroundColor = new iTextSharp.text.Color(255, 255, 0);
			pTable.AutoFillEmptyCells = false;
			pTable.Padding = 0;
			pTable.Spacing = 0;
			pTable.DefaultCell.Border = 0;
//			pTable.Border = iTextSharp.text.Rectangle.BOX;
			pTable.Border = 1;

			cell = new iTextSharp.text.Cell(new Chunk("Description : ", ften));
			cell.Width = 40f;
			pTable.AddCell(cell);
			cell = new iTextSharp.text.Cell(new Chunk(name, ften));
			cell.Width = 60f;
			cell.HorizontalAlignment = Element.ALIGN_RIGHT; 
			pTable.AddCell(cell);

			iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(simg);
//			img.ScaleAbsolute(110, 110);
			iTextSharp.text.Cell img_cell = new iTextSharp.text.Cell(img);
			pTable.AddCell(img_cell);
			cell = new iTextSharp.text.Cell(iTable);
			pTable.AddCell(cell);
//			pTable.InsertTable(iTable);
			
			cell = new iTextSharp.text.Cell(pTable);
			cell.Border = 1;
			cell.BorderWidth = 1;
//			cell.Border = iTextSharp.text.Rectangle.BOX;
			cell.BorderColor =  new iTextSharp.text.Color(100, 100, 255);
			aTable.AddCell(cell);
			if(i%2 == 0)
			{
				cell = new iTextSharp.text.Cell(" ");
				cell.Width = 10;
				aTable.AddCell(cell);
			}
			m++;
			if(m >= 8)
			{
				m = 0;
				document.Add(aTable);
				document.NewPage();
				aTable = new iTextSharp.text.Table(3);
				aTable.SetWidths(new int[]{47, 6, 47});
				aTable.Border = 0;
				aTable.Width = 100f;
				aTable.BorderWidth = 1;
				aTable.BackgroundColor = new iTextSharp.text.Color(250, 250, 250);
				aTable.Padding = 1;
				aTable.Spacing = 2;
				aTable.DefaultCell.Border = 0;
			}
		}
		document.Add(aTable);                      
	}
	catch(DocumentException de) 
	{
		Response.Write(de.Message);
	}
	catch(IOException ioe) 
	{
		Response.Write(ioe.Message);
	}
	document.Close();
	
//	Response.Write("<br><center><h4>Done, click to download</h4>");
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + sfn + "\">");			
	return true;
}
bool CreatePDFFileBrochure()
{
	if(!GetBrochureItem())
		return false;
	int nRows = dst.Tables["brochureItem"].Rows.Count;
	if(nRows <= 0)
	{
		Response.Write("<br><center><h3>There is no item in this brochure");
		return false;
	}
	
	DataRow dr = dst.Tables["brochureItem"].Rows[0];
	string cat = dr["cat"].ToString();
	string s_cat = dr["s_cat"].ToString();
	
	string sfn = "temp/SD_B_" + m_brochureID + "_" + DateTime.Now.ToString("yy_MM_dd") + ".pdf";
	if(m_cat != "")
		sfn = "temp/SD_C_" + m_cat.Replace("*", "").Replace("/", "").Replace("\\", "").Replace(" ", "_") + "_" + DateTime.Now.ToString("yy_MM_dd") + ".pdf";
	Document document = new Document(PageSize.A4);
	try 
	{
		iTextSharp.text.Font ften = new iTextSharp.text.Font(iTextSharp.text.Font.HELVETICA, 10, iTextSharp.text.Font.NORMAL);
		iTextSharp.text.Font fbold = new iTextSharp.text.Font(iTextSharp.text.Font.HELVETICA, 12, iTextSharp.text.Font.BOLD);

		PdfWriter.GetInstance(document, new FileStream(Server.MapPath(sfn), FileMode.Create));
//		HeaderFooter header = new HeaderFooter(new Phrase("this is a header"), false);
//		HeaderFooter header = new HeaderFooter(new Phrase(hTable), false);
//		document.Header = header;
		document.Open();
		
		iTextSharp.text.Table aTable = new iTextSharp.text.Table(5);
//		aTable.TableFitsPage = true;
		aTable.SetWidths(new int[]{32, 1, 32, 1, 32});
		aTable.AutoFillEmptyCells = false;
		aTable.Border = 0;
		aTable.Width = 100f;
		aTable.Padding = 1;
		aTable.Spacing = 2;
		aTable.DefaultCell.Border = 0;

		iTextSharp.text.Cell cell = new iTextSharp.text.Cell(new Chunk("", ften));
//		cell = new iTextSharp.text.Cell(new Chunk(cat + " - " + s_cat, fbold));
		cell = new iTextSharp.text.Cell(new Chunk(" ", fbold));
		cell.Colspan = 2;
		aTable.AddCell(cell);
	
		cell = new iTextSharp.text.Cell(new Chunk("www.acqshopping.co.nz", ften));
		cell.Colspan = 2;
		cell.HorizontalAlignment = Element.ALIGN_CENTER; 
		aTable.AddCell(cell);
		
		string spath = Server.MapPath("../i/logoacq.jpg");
		if(File.Exists(spath))
		{
			iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(spath);
			img.ScaleAbsolute(60, 30);
			iTextSharp.text.Cell img_cell = new iTextSharp.text.Cell(img);
			img_cell.HorizontalAlignment = Element.ALIGN_RIGHT; 
			aTable.AddCell(img_cell);
		}
		else
		{
			aTable.AddCell("");
		}
/*		if(m_cat == "")
		{
			cell = new iTextSharp.text.Cell(new Chunk(cat + " - " + s_cat, fbold));
			cell.Colspan = 2;
			aTable.AddCell(cell);
		
			cell = new iTextSharp.text.Cell(new Chunk("www.acqshopping.com", ften));
			cell.Colspan = 2;
			cell.HorizontalAlignment = Element.ALIGN_CENTER; 
			aTable.AddCell(cell);
			
			string spath = Server.MapPath("../i/logoacq.jpg");
			if(File.Exists(spath))
			{
				iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(spath);
				img.ScaleAbsolute(60, 30);
				iTextSharp.text.Cell img_cell = new iTextSharp.text.Cell(img);
				img_cell.HorizontalAlignment = Element.ALIGN_RIGHT; 
				aTable.AddCell(img_cell);
			}
			else
			{
				aTable.AddCell("");
			}
		}
*/		
		int m = 0;
		string code = "";
		string simg = "";
		string name = "";
		string mpn = "";
		string moq = "";
		string inner = "";
		string outer = "";
		string str_cat = dst.Tables["brochureItem"].Rows[0]["cat"].ToString();
		string str_s_cat = dst.Tables["brochureItem"].Rows[0]["s_cat"].ToString();
		string str_cat_old = str_cat;
		string str_s_cat_old = str_s_cat;
		string stitle = "";
		double dPrice = 0;
		int nChapters = 1;
		int nSections = 1;
		iTextSharp.text.Chapter chapter1 = new iTextSharp.text.Chapter(new iTextSharp.text.Paragraph(str_cat, fbold), nChapters++);
		iTextSharp.text.Section section1 = chapter1.AddSection(new iTextSharp.text.Paragraph(str_s_cat, fbold), 2);
		for(int i=0; i<nRows; i++)
		{			
			dr = dst.Tables["brochureItem"].Rows[i];
			code = dr["code"].ToString();
			//if(double.Parse(code) > 110584)
				//break;
			simg = Server.MapPath("../pi/" + code + ".jpg");
			name = dr["name"].ToString();
//			inner = dr["inner_pack"].ToString();
//			outer = dr["outer_pack"].ToString();
//			name += "[" + inner + "/" + outer + "]";
			mpn = dr["supplier_code"].ToString();
			dPrice = MyDoubleParse(dr["price2"].ToString());
			if(m_sPriceLevel == "price1")
				dPrice = MyDoubleParse(dr["price1"].ToString());        
			str_cat = dr["cat"].ToString();
			str_s_cat = dr["s_cat"].ToString();
			if(m_cat != "" && m > 0 && (str_s_cat_old != str_s_cat || str_cat_old != str_cat))
			{
				section1.Add(aTable);
				section1.NewPage();
				aTable = new iTextSharp.text.Table(5);
				aTable.AutoFillEmptyCells = true;
				aTable.SetWidths(new int[]{32, 1, 32, 1, 32});
				aTable.Border = 0;
				aTable.Width = 100f;
				aTable.Padding = 1;
				aTable.Spacing = 2;
				aTable.DefaultCell.Border = 0;
				m = 0;
				if(str_cat_old == str_cat && str_s_cat_old != str_s_cat)
				{
					section1 = chapter1.AddSection(new iTextSharp.text.Paragraph(str_s_cat, fbold), 2);
					cell = new iTextSharp.text.Cell(new Chunk(" ", fbold));
					cell.Colspan = 2;
					aTable.AddCell(cell);
				
					cell = new iTextSharp.text.Cell(new Chunk("www.acqshopping.co.nz", ften));
					cell.Colspan = 2;
					cell.HorizontalAlignment = Element.ALIGN_CENTER; 
					aTable.AddCell(cell);
					
					string sspath = Server.MapPath("../i/logoacq.jpg");
					if(File.Exists(spath))
					{
						iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(sspath);
						img.ScaleAbsolute(60, 30);
						iTextSharp.text.Cell img_cell = new iTextSharp.text.Cell(img);
						img_cell.HorizontalAlignment = Element.ALIGN_RIGHT; 
						aTable.AddCell(img_cell);
					}
					else
					{
						aTable.AddCell("");
					}
				}
			}
			else if(i > 0 && m == 0) //print title on each page
			{
//				cell = new iTextSharp.text.Cell(new Chunk(str_cat + " - " + str_s_cat, fbold));
//				cell.Colspan = 5;
//				aTable.AddCell(cell);

				cell = new iTextSharp.text.Cell(new Chunk(cat + " - " + s_cat, fbold));
				cell.Colspan = 2;
				aTable.AddCell(cell);
			
				cell = new iTextSharp.text.Cell(new Chunk("www.acqshopping.co.nz", ften));
				cell.Colspan = 2;
				cell.HorizontalAlignment = Element.ALIGN_CENTER; 
				aTable.AddCell(cell);
				
				string sspath = Server.MapPath("../i/logoacq.jpg");
				if(File.Exists(spath))
				{
					iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(sspath);
					img.ScaleAbsolute(60, 30);
					iTextSharp.text.Cell img_cell = new iTextSharp.text.Cell(img);
					img_cell.HorizontalAlignment = Element.ALIGN_RIGHT; 
					aTable.AddCell(img_cell);
				}
				else
				{
					aTable.AddCell("");
				}
			}
			if(m_cat != "" && str_cat_old != str_cat)
			{
				document.Add(chapter1);
				chapter1 = new iTextSharp.text.Chapter(new iTextSharp.text.Paragraph(str_cat, fbold), nChapters++);
				nSections = 1;
				section1 = chapter1.AddSection(new iTextSharp.text.Paragraph(str_cat + " - " + str_s_cat, fbold), 2);
				str_s_cat_old = "-1";
				aTable = new iTextSharp.text.Table(5);
				aTable.AutoFillEmptyCells = true;
				aTable.SetWidths(new int[]{32, 1, 32, 1, 32});
				aTable.Border = 0;
				aTable.Width = 100f;
				aTable.Padding = 1;
				aTable.Spacing = 2;
				aTable.DefaultCell.Border = 0;
				str_s_cat_old = str_s_cat;
				str_cat_old = str_cat;
				m = 0;
			}

			iTextSharp.text.Table pTable = new iTextSharp.text.Table(2);
			pTable.Width = 98f;
//			pTable.DefaultCell.Border = 1;
			pTable.DefaultCell.BorderColor = new iTextSharp.text.Color(230, 230, 230);

			if(File.Exists(simg))
			{
				iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(simg);
				img.ScaleAbsolute(90, 90);
				iTextSharp.text.Cell img_cell = new iTextSharp.text.Cell(img);
				img_cell.Colspan = 2;
				img_cell.HorizontalAlignment = Element.ALIGN_CENTER; 
				pTable.AddCell(img_cell);
			}
			else
			{
				cell = new iTextSharp.text.Cell(" ");
				cell.Colspan = 2;
				pTable.AddCell(cell);
			}
			
			cell = new iTextSharp.text.Cell(new Chunk(name, ften));
			cell.Colspan = 2;
			cell.HorizontalAlignment = Element.ALIGN_CENTER; 
			pTable.AddCell(cell);

			cell = new iTextSharp.text.Cell(new Chunk(mpn, ften));
			cell.HorizontalAlignment = Element.ALIGN_CENTER;
			pTable.AddCell(cell);
			
			cell = new iTextSharp.text.Cell(new Chunk(dPrice.ToString("C"), ften));
			cell.HorizontalAlignment = Element.ALIGN_CENTER;
			pTable.AddCell(cell);

			cell = new iTextSharp.text.Cell(pTable);
			aTable.AddCell(cell);
			m++;
			if(m%3 != 0)
			{
				aTable.AddCell("");
			}
			if(m >= 12)
			{
				if(m_cat != "")
				{
					section1.Add(aTable);
					section1.NewPage();
				}
				else
				{
					document.Add(aTable);
					document.NewPage();
				}
				aTable = new iTextSharp.text.Table(5);
				aTable.AutoFillEmptyCells = true;
				aTable.SetWidths(new int[]{32, 1, 32, 1, 32});
				aTable.Border = 0;
				aTable.Width = 100f;
				aTable.Padding = 1;
				aTable.Spacing = 2;
				aTable.DefaultCell.Border = 0;
				m = 0;
			}
			str_cat_old = str_cat;
			str_s_cat_old = str_s_cat;
		}
		if(m_cat != "")
		{
			section1.Add(aTable);
			document.Add(chapter1);
		}
		else
		{
			document.Add(aTable);
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
	
	Response.Write("<br><center><h4>Done, <a href=" + sfn + ">click to download</a></h4>");
//	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + sfn + "\">");			
	return true;
}
</script>
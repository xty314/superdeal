<!-- #include file="page_index.cs" -->
<script runat=server>
//////////////////////////////////////////////
// data grid template

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataSet promods=new DataSet();
string cat = "";
string s_cat = "";
string ss_cat = "";
string m_scode = "";
int total = 0;
int labels = 2;
string m_branchID = "1";

string m_action = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("administrator"))
		return;

	if(Request.QueryString["t"] != null)
		m_action = Request.QueryString["t"];

	// if(Request.QueryString["branch"] != null)
	// 	m_branchID = Request.QueryString["branch"];
	// if(Session["branch_support"] != null)
	// {
	
	// 	m_branchID = pgs("branch");
	// }
	if(m_branchID == null || m_branchID == "")
	{
		m_branchID = Session["login_branch_id"].ToString();
	}
	
	if(Request.QueryString["c"] != "" && Request.QueryString["c"] != null)
	{
	//    if(checkbarcode(Request.QueryString["c"].ToString()))
	  //  {
		if(doAddItem(Request.QueryString["c"].ToString()))
		{
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx? \">");
			return;
		}

        //     }
	}

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
	if(m_action == "bulk")
	{
		Trim(ref cat);
		Trim(ref s_cat);
		Trim(ref ss_cat);

		if(Request.Form["cmd"] == "Add All Pages")
		{
			if(DoBulkAddAll())
			{
				Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?t=bulk");
				Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
				Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
				Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
				Response.Write("\">");
				return;
			}
		}
		else if(Request.Form["cmd"] == "Add Selected")
		{
			if(DoBulkAddSelected())
			{
				Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?t=bulk");
				Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
				Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
				Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
				if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
					Response.Write("&p=" + Request.QueryString["p"]);
				if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
					Response.Write("&spb=" + Request.QueryString["spb"]);
	            if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
                    Response.Write("&branch=" + Request.QueryString["branch"]);
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
		if(DoDelete(false))
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?branch="+m_branchID+"&cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"&spb=1");
			if(Request.QueryString["p"] != null)
				Response.Write("&p="+Request.QueryString["p"]);
			Response.Write("\">");
		return;
	}
    	else if(m_action == "delall")
	{
		if(DoDelete(true))
			Response.Write("<meta  http-equiv=\"refresh\" content=\"0; URL=flare.aspx?branch="+m_branchID+"&cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&sscat="+HttpUtility.UrlEncode(ss_cat)+"\">");
		return;
	}
	else if(m_action == "print1")
	{
		PrintNewLayout2x10();
		// PrintPriceList(1, 2);
		return;
	}
	else if(m_action == "print2")
	{
		PrintPriceList(2, 0);
		return;
	}
	else if(m_action == "print3")
	{	
		PrintPriceList(1, 1);
		return;
	}
	else if(m_action == "print4")
	{	
		PrintNewLayout3x10();
		// PrintPriceList3(1, 2);
		return;
	}
	/***********************/
	else if(m_action == "print5")
	{	
		PrintPriceListA6(1, 2);
		return;
	}
	else if(m_action == "print6")
	{	
		 PrintNewLayout3x10_CN();
		return;
	}
	else if(m_action == "print7")
	{	
		 PrintNewLayout2x4sc();
		return;
	}
	else if(m_action == "print8")
	{	
		 PrintNewLayoutA4();
		return;
	}
	else if(m_action == "print9")
	{	
		 PrintNewLayoutA5();
		return;
	}
		else if(m_action == "print10")
	{	
		 PrintNewLayout2x10();
		return;
	}
	else if(m_action == "print11")
	{	
		 PrintNewLayout2x4multi();
		return;
	}
	else if(m_action == "print12")
	{	
		 PrintNewLayoutA4multi();
		return;
	}
		else if(m_action == "print13")
	{	
		 PrintNewLayoutA5multi();
		return;
	}
		else if(m_action == "print14")
	{	
		 PrintNewLayoutA4RTC();
		return;
	}
	
	/***********************/

	else if(Request.Form["code"] != null && Request.Form["code"] != "")
	{
		if(DoAddOne())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=flare.aspx?code=" + m_scode + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><center><h3>Print Price List</h3>");
	ShowEditTable();
	PrintAdminFooter();
}

bool GetFlareTable()
{

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];

//	string sc = " SELECT f.id, f.code, p.supplier_code, p.name, p.name_cn, c.price1 as price, p.cat, p.s_cat, p.ss_cat ";
//	string sc = " SELECT f.id, f.code, c.barcode, p.name, p.name_cn, c.price1 as price, p.cat, p.s_cat, p.ss_cat ";
	string sc = " ";

	sc = " SELECT distinct f.id, f.code,  c.name,   c.cat,c.supplier_code,c.barcode, ";
	sc+=" c.level_price0 AS price ";
	sc += " FROM flare f  ";
	sc += " join code_relations c on c.code = f.code ";
	sc +="Where 1=1";


	if(cat != "")
	{
		sc += " AND p.cat = N'" + cat + "' ";
	}
	if(s_cat != "")
	{
	
			sc += " AND ";
		sc += " p.s_cat = N'" + s_cat + "' ";
	}
	if(ss_cat != "")
	{
	
			sc += " AND ";
		sc += " p.ss_cat = N'" + ss_cat + "' ";
	}
	if(Request.QueryString["code"] != null)
	{
	
			sc += " AND ";
		sc += " f.code = " + Request.QueryString["code"];
	}
 
//	sc += " ORDER BY barcode, p.cat, p.s_cat, p.ss_cat, p.name ";
    sc += " ORDER BY f.id desc";
	

//  	DEBUG("sc=",sc);
// return false;


	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "flare");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetFlareData()
{
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];

	string sc = "";


	sc = " SELECT c.code, c.supplier_code as supplier_code1,c.barcode as supplier_code,  c.name, c.cat, c.s_cat, c.ss_cat ";
	sc += ", c.level_rate1, c.level_rate2, c.level_rate3, c.level_rate4,c.level_rate5, c.level_rate6, c.level_rate7, c.level_rate8, c.level_rate9 ";
	sc += ", c.name_cn, c.is_special,c.special_price, c.special_price_end_date, c.price4 ";
// sc+=", ISNULL(cb.price1,c.price1) AS price";
	sc+=",c.level_price0 AS price";
	sc += " FROM flare f JOIN code_relations c ON c.code = f.code ";
    sc += " WHERE 1 = 1 ";


	if(cat != "")
	{
		sc += " AND c.cat = N'" + cat + "' ";
	}
	if(s_cat != "")
	{
		sc += " AND c.s_cat = N'" + s_cat + "' ";
	}
	if(ss_cat != "")
	{
		sc += " AND c.ss_cat = N'" + ss_cat + "' ";
	}


	sc += " ORDER BY c.cat, c.s_cat, c.ss_cat, c.name ";
// DEBUG("sc=",sc);
// return false;
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



bool GetPromoFlareData()
{

	string myBranchId=m_branchID;


	string sc = "";


	sc = "select distinct c.core_range,c.code,c.price1 as price,c.price4,c.special_price_end_date,c.is_special,p.volumn_discount_qty,p.volumn_discount_price_total,c.name,";
	sc += " isnull((select top 1 barcode from barcode where item_code=c.code and convert(varchar,barcode)!=convert(varchar,c.code)),c.code) as barcode ";
	sc += "from flare f  left join code_relations c on c.code=f.code  ";
	sc += "left join promotion_group pg on pg.item_code=c.code ";
    sc += "left join promotion_list p on p.promo_id=pg.promo_id ";
	sc += "left join barcode b on b.item_code=c.code ";
	sc += "join promotion_branch pb on pb.promo_id=p.promo_id and pb.branch_id=f.branch_id ";
	
	sc += "where 1=1 and p.promo_type=6  and b.barcode=isnull((select top 1 barcode from barcode where item_code=c.code and convert(varchar,barcode)!=convert(varchar,c.code)),c.code)";

// DEBUG("sc=",sc);
// return false;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(promods, "data");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DoDelete(bool bDeleteAll)
{
/*	
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
*/
    //m_branchID = Request.QueryString["branch"];
	string sc = " DELETE FROM flare ";
    sc += " WHERE 1=1 ";
    if(!bDeleteAll)
	    sc += " AND id = " + Request.QueryString["id"];
    // else if(m_branchID != null && m_branchID != "")
    //     sc += " AND branch_id = '"+m_branchID+"'";
//DEBUG("sc",sc);
//return false;
/*
	else
	{
		bool bWhereAdded = false;
		if(cat != "")
		{
			sc += " WHERE p.cat = N'" + cat + "' ";
			bWhereAdded = true;
		}
		if(s_cat != "")
		{
			if(!bWhereAdded)
				sc += " WHERE ";
			else 
				sc += " AND ";
			sc += " p.s_cat = N'" + s_cat + "' ";
		}
		if(ss_cat != "")
		{
			if(!bWhereAdded)
				sc += " WHERE ";
			else 
				sc += " AND ";
			sc += " p.ss_cat = N'" + ss_cat + "' ";
		}
 
	}
 *  */
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
	 if(ds.Tables["checkCode"] != null)
        ds.Tables["checkCode"].Clear();

    string s_code = Request.Form["code"];
    
    if(s_code == null || s_code == "")
        return false;
    string sc = " SELECT TOP 1 *  FROM code_relations c  ";
    sc += " WHERE supplier_code =N '"+ EncodeQuote(s_code) +"' OR barcode = '"+ EncodeQuote(s_code) + "' ";
   // if(TSIsDigit(s_code) && s_code.Length <= 12)
   //     sc += " OR code = "+ s_code;	

//DEBUG("sc=",sc);
//return false;
    int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "checkCode");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
    if(rows == 1)
{
        s_code = ds.Tables["checkCode"].Rows[0]["code"].ToString();
	return false;
}

    /*******************************/
	if(rows == 0)
		Response.Write("jksdlfjsjdfl");
	/*****************************/    
    else
    { 
        PrintAdminHeader();
        PrintAdminMenu();
        Response.Write("<br><br><center><h3>Error, product not found. code = " + s_code + "</h3>");
        return false;
    }
    m_scode = s_code;
	sc = " IF NOT EXISTS (SELECT * FROM flare WHERE code=" + s_code + ") ";
	sc += " INSERT INTO flare (code,branch_id) VALUES(" + s_code + ","+m_branchID+") ";
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
	for(int i=1; i<=nDealerLevel; i++)
	{
		Response.Write("<option value="+ i +">Dealer Level: "+ i +"</option>");
	}
	Response.Write("</select>");

}
void ShowEditTable()
{
	if(!GetFlareTable())
		return;

	string tableName = "flare";
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
	m_cPI.URI = "?";
	m_cPI.PageSize = 30;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	Response.Write("<body onload='document.all.addcode.focus();'>");
	Response.Write("<form action=flare.aspx method=post name=f>");
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
    
    Response.Write("<tr><td>");
    Response.Write("<b>Branch : </b>");
//DEBUG("m_branchID",m_branchID);
	PrintBranchNameOptions(m_branchID, "flare.aspx?branch=", true);
    Response.Write("</td>");
	Response.Write("<td colspan=2 width=400><b>Code : ");//</b><input type=text name=code size=5>");
	Response.Write("<input type=text size=16 name=addcode onKeyDown=\"if(event.keyCode ==13){document.all.add.focus();}\">");
	Response.Write("<input type=button name=add value=Add onclick=\"window.location=('?c='+ document.all.addcode.value )\">");
	Response.Write("</td><td align=right>");
	//Response.Write("<input type=button onclick=window.open('editpage.aspx?p=flare_header') value='Edit Header' " + Session["button_style"] + ">");
//	Response.Write("<input type=submit name=cmd value=Add " + Session["_style"] + ">");
//	Response.Write("<input type=submit name=cmd value=Search " + Session["button_style"] + ">");
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=button name=cmd value='Bulk Add' ");
	Response.Write(" onclick=window.location=('flare.aspx?t=bulk') " + Session["button_style"] + ">");
//	Response.Write("<i>(select from catalog)</i>");

	
	Response.Write("&nbsp&nbsp&nbsp&nbsp&nbsp&nbsp;");
	PrintCatsForDisplay();
	Response.Write("</td>");
	
	Response.Write("<td colspan=3 align=right>");
	/*
	Response.Write("&nbsp&nbsp&nbsp;Select Dealer Level:");
	DoShowDealerLevel();*/
	//Response.Write("<input type=button onclick=\"window.open('flare.aspx?t=print1&dl=1&branch="+m_branchID+"&cat="+cat+"&scat="+s_cat+"&sscat="+ss_cat+"' )\" value='Print 2 Col' " + Session["button_style"] + ">");
	/*******************/
	Response.Write("<input type=button onclick=\"window.open('flare.aspx?t=print4&dl=1&branch="+m_branchID+"&cat="+cat+"&scat="+s_cat+"&sscat="+ss_cat+"' )\" value='Print Label' " + Session["button_style"] + ">");



	/********************/
	// Response.Write("<input type=button onclick=\"window.open('flare.aspx?t=print5&dl=1&branch="+m_branchID+"&cat="+cat+"&scat="+s_cat+"&sscat="+ss_cat+"' )\" value='Print A6' " + Session["button_style"] + ">");
	/********************/
	// Response.Write("<input type=button onclick=\"window.open('flare.aspx?t=print3&dl=1&branch="+m_branchID+"&cat="+cat+"&scat="+s_cat+"&sscat="+ss_cat+"')\" value='Print Single Column' " + Session["button_style"] + ">");
    Response.Write("<input type=button onclick=\"if(confirm('Are you sure to delete all items')){window.location=('flare.aspx?t=delall&branch="+m_branchID+"');}else{ return false; } \" value='Delete All' " + Session["button_style"] + ">");
	
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(int m=1; m<cols; m++)
	{
		if(dc[m].ColumnName == "code")
			continue;
		Response.Write("<th width=15%><b>" + dc[m].ColumnName + "</b></th>");
	}
	Response.Write("<th><b>Action</b></th>");
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
			if(dc[j].ColumnName== "code")
				continue;
			value = dr[j].ToString();
			Response.Write("<td width=15%>" + value + "</td>");
		}
		Response.Write("<td align=left>");
		Response.Write("<a href=liveedit.aspx?code=" + dr["code"].ToString() + " class=o target=_blank>EDIT</a> &nbsp; ");
		Response.Write("<a href=flare.aspx?cat="+HttpUtility.UrlEncode(cat)+"&scat="+HttpUtility.UrlEncode(s_cat)+"&");
		if(Request.QueryString["p"] != null)
			Response.Write("p="+ Request.QueryString["p"]);
		Response.Write("&sscat="+HttpUtility.UrlEncode(ss_cat)+"&t=del&branch="+m_branchID+"&id=" + dr["id"].ToString() + " class=o>DEL</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=" + cols + ">" + sPageIndex + "</td></tr>");
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
	m_cPI.URI += "&cat=" + HttpUtility.UrlEncode(cat);
	m_cPI.URI += "&scat=" + HttpUtility.UrlEncode(s_cat);
	m_cPI.URI += "&sscat=" + HttpUtility.UrlEncode(ss_cat);

	m_cPI.PageSize = 20;
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	Response.Write("<br><center><h3>Price List Templates - Bulk Add</h3>");

	Response.Write("<form action=flare.aspx?t=bulk");
	Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
		Response.Write("&p=" + Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null && Request.QueryString["spb"] != "")
		Response.Write("&spb=" + Request.QueryString["spb"]);
	Response.Write(" method=post>");

	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=4 nowrap><b>Select Catalog : </b>");
	PrintCats();
	Response.Write("</td>");

	Response.Write("<td colspan=3 align=right>");
	Response.Write("<b>Total </b><font color=red><b>" + total + "</b></font><b> items found</b>&nbsp&nbsp;");
	Response.Write("<input type=submit name=cmd value='Add Selected' " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value='Add All Pages' " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(int m=0; m<cols; m++)
		Response.Write("<th><b>" + dc[m].ColumnName + "</b></th>");
	Response.Write("<th><b>Select</b></th>");
	Response.Write("</tr>");

	if(rows <= 0)
	{
		Response.Write("<tr><td colspan=6 align=right>");
		Response.Write("<input type=button value='Show Template' onclick=window.location=('flare.aspx') " + Session["button_style"] + ">");
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

	Response.Write("<tr><td colspan=" + cols + ">" + sPageIndex + "</td></tr>");

	Response.Write("<tr><td colspan=6 align=right>");
	Response.Write("<input type=button value='Show Template' onclick=window.location=('flare.aspx') " + Session["button_style"] + ">");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");	

	return true;
}


bool Checkbarcode(string code)
{
	int rows =0;
//	string barcode = p("barcode");
	if(dst.Tables["barcode"] != null)
		dst.Tables["barcode"].Clear();
	string sc =" SELECT * from barcode where code= " + code+ "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "barcode");
		if(rows > 0)
			return true;
		else
		{
			Response.Write("Sorry, New Product!!");
			return false;
		}
			
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//	return true;	
	
}


bool GetProductList()
{
	string sc = "";
	if(cat == "")
		cat = "just build the table, no row return please";

	sc = " SELECT p.code, p.cat, p.s_cat, p.ss_cat, p.name,p.name_cn ";
	sc += " FROM product p ";
//	sc += " JOIN stock_qty s ON s.code = p.code";
//	sc += " LEFT OUTER JOIN flare f ";
	sc += " WHERE p.cat = N'" + cat + "' ";
	if(s_cat != "" && s_cat != "all")
		sc += " AND p.s_cat = N'" + s_cat + "' ";
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND p.ss_cat = N'" + ss_cat + "' ";
	sc += " AND p.code NOT IN (SELECT code FROM flare) ";
	
//DEBUG("sc=",sc);
//return false;
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
	sc += " ORDER BY p.cat";
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
	Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
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
		sc += " WHERE p.cat = N'"+ cat +"' ";
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?t=bulk");
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
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
		sc += " WHERE p.cat = N'"+ cat +"' ";
		sc += " AND p.s_cat = N'"+ s_cat +"' ";
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
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value)\"");
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
	sc += " FROM product p JOIN flare f ON f.code = p.code ";
	sc += " ORDER BY p.cat";
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
	Response.Write("cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value=''>All</option>");
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
		sc += " FROM product p JOIN flare f ON f.code = p.code ";
		sc += " WHERE p.cat = N'"+ cat +"' ";
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
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
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
		sc += " FROM product p JOIN flare f ON f.code = p.code ";
		sc += " WHERE p.cat = N'"+ cat +"' ";
		sc += " AND p.s_cat = N'"+ s_cat +"' ";
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
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value)\"");
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
	string myBranchId=m_branchID;
	if(myBranchId==null||myBranchId==""){
		myBranchId="1";
	}
	for(int i=0; i<rows; i++)
	{
		sc += " INSERT INTO flare (code,branch_id) VALUES(" + ds.Tables["bulk"].Rows[i]["code"] +","+myBranchId+ ") ";
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
		string myBranchId=m_branchID;
	if(myBranchId==null||myBranchId==""){
		myBranchId="1";
	}
	for(int i=row_start; i<row_end; i++)
	{
		if(Request.Form["select" + i] == "on")
			sc += " INSERT INTO flare (code ,branch_id) VALUES(" + Request.Form["code" + i] +"," +myBranchId+") ";
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
bool PrintPriceListA6(int page, int col)
{
	if(!GetFlareData())
		return false;

	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));

	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string supplier_code = "";
    string country_of_origin = "";
	DateTime special_price_end_date ;	
	string is_special = "";
	string special_price = "";
	string sped = "";
	if(col != 0);
		labels = col;
	int nItemsPerPage = MyIntParse(GetSiteSettings("flare_items_per_page", "20"));

	string st = "<table width=\"100%\" align=center valign=center cellspacing=1 cellpadding=3 border=0 bordercolor=#000000 >";
	string sth = "<table width=\"100%\" align=center valign=center cellspacing=1 cellpadding=3 border=0 bordercolor=#FF0000 >";

	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		if(i % nItemsPerPage == 0)
		{
			if(i > 0)
			{
				sb.Append("</td></tr></table>");
				sb.Append("<p style = page-break-before:always>");
			}
			sb.Append(st);
			sb.Append("<tr><td valign=top width=50%>");
			sb.Append(sth);
			sb.Append("<tr>");	
							
		}	
			
		code = dr["code"].ToString();
        supplier_code = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		is_special = dr["is_special"].ToString();
		special_price = dr["special_price"].ToString();

		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
//		special_price_end_date = DateTime.Parse(dr["special_price_end_date"].ToString());
		special_price_end_date = DateTime.Parse(sped);

//		special_price_end_date = DateTime.Parse(dr["special_price_end_date"].ToString());
		if(name=="" || name==null)
		{
			name="&nbsp;";
		}
		if(name_cn=="" || name_cn == null)
		{
			name_cn="&nbsp;";
		}
		DateTime today = DateTime.Now;
		int name_Length = name.Length;
		if(name_Length > 12)
			name_Length = 12;
		name = name.Substring(0, name_Length);

		int name_cn_Length = name_cn.Length;
		if(name_cn_Length >20)
			name_cn_Length = 20;
		name_cn = name_cn.Substring(0, name_cn_Length);
		
		
		price = dr["price"].ToString();
//		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
//			price = dr["special_price"].ToString();
		if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}
        country_of_origin = dr["country_of_origin"].ToString();		

		if(i % labels == 0)
		{
			sb.Append("</tr><tr>");
		}
		/*
		sb.Append("<td width=50% valign=top>");
		sb.Append("<table width=100% border=0 bordercolor=#0000FF style='font-family:Verdana;font-size:18pt;border-color:#EEEEEE;border-width:1px;border-style:Dashed;border-collapse:collapse;fixed'>");

        
        	sb.Append("<tr><td colspan=\"2\" style=font-size:16pt;><B>" + name + "</B></td></tr>");
		sb.Append("<tr><td colspan=\"2\" style=font-size:17pt;><B>" + name_cn + "</B></td></tr>");
		sb.Append("<tr><td style=font-size:9pt;><B>" + supplier_code + "</B></td>");
		sb.Append("<td style=font-size:18pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		sb.Append("</table>");
		sb.Append("</td>");
		*/
		
		sb.Append("<td width=50% valign=top>");
		sb.Append("<table width=100% border=0 bordercolor=#0000FF style='font-family:Verdana;font-size:18pt;border-color:#000000;border-width:1px;border-style:Dashed;border-collapse:collapse;fixed'>");
	    if(name.Length <= 20)
            sb.Append("<tr><td height=30 colspan=\"2\" style=font-size:30pt;height=30><B>" + name + "</B></td></tr>");
        else
            sb.Append("<tr><td height=30 colspan=\"2\" style=font-size:30pt;height=30 ><B>" + name + "</B></td></tr>");
		
		sb.Append("<tr><td height=15 colspane=2></td></tr>");

		sb.Append("<tr><td colspan=\"2\">");
		sb.Append("<table width=100% >");
//		sb.Append("<tr><td colspan=\"2\" style=font-size:14pt; ><B>" + name_cn + "</B></td><td align=right valign=bottom style=font-size:27pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		if(name_cn.Length <= 20)
            sb.Append("<tr><td height=25 colspan=\"2\" style=font-size:23pt;height=30><B>" + name_cn + "</B></td></tr>");
        else
            sb.Append("<tr><td height=25 colspan=\"2\" style=font-size:23pt;height=30><B>" + name_cn + "</B></td></tr>");
        sb.Append("<tr><td colspan=\"2\" style=font-size:9pt;height:15px >" + country_of_origin + "</td></tr>");

        sb.Append("<tr><td colspan=\"2\" align=center valign=bottom style=font-size:89pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");

	    sb.Append("<tr><td height=35></td></tr>");
        sb.Append("<tr><td style=font-size:25pt; ><B>" + supplier_code + "</B></td></tr>");
		sb.Append("</table>");
		sb.Append("</td><tr>");
		
		/*
		sb.Append("<tr><td colspan=\"2\" style=font-size:18pt; ><B>" + name_cn + "</B></td></tr>");
		sb.Append("<tr><td style=font-size:9pt; ><B>" + supplier_code + "</B></td>");
		sb.Append("<td align=right style=font-size:20pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		*/
		sb.Append("</table>");
		sb.Append("</td>");
		//DEBUG("i" , (i %2).ToString());
		///DEBUG("ROWS ", rows.ToString());
		if( i ==(rows-1) && i % 2== 0)
			sb.Append("<td width=50% style='abcd'>&nbsp;</td>");
		
	}
/*

	int start = 0;
	int nRowsPerPage = 10;
	for(int i=0; i<ds.Tables["data"].Rows.Count; i += nRowsPerPage * 2)
	{	
		sb.Append("<table width=700 align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 class=t>");
		sb.Append("<tr><td valign=top>");
		sb.Append(PrintHalfPage(start, nRowsPerPage));
		start += nRowsPerPage;
		sb.Append("</td><td valign=top>");
		sb.Append(PrintHalfPage(start, nRowsPerPage));
		start += nRowsPerPage;
		sb.Append("</td></tr>");
		sb.Append("</table>");
	}
*/
	Response.Write(sb.ToString());
	return true;
}

bool PrintPriceList3(int page, int col)
{
	if(!GetFlareData())
		return false;

	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "11"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));

	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string supplier_code = "";
	string sped = "";
    string country_of_origin = "";
    string country = "";
	DateTime special_price_end_date ;	

	if(col != 0);
		labels = col;
	int nItemsPerPage = 10000; //MyIntParse(GetSiteSettings("flare_items_per_page", "33"));

	string st = "<table width=\"100%\" align=center valign=center cellspacing=1 cellpadding=5 border=0 bordercolor=#000000 >";
	string sth = "<table width=\"100%\" align=center valign=center cellspacing=1 cellpadding=5 border=0 bordercolor=#FF0000 >";

	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		if(i % nItemsPerPage == 0)
		{
			if(i > 0)
			{
				sb.Append("</td></tr></table>");
				sb.Append("<p style = page-break-before:always>");
			}
			sb.Append(st);
			sb.Append("<tr><td valign=top width=30%>");
			sb.Append(sth);
			sb.Append("<tr>");	
							
		}	
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
        	supplier_code = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();

		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
//		special_price_end_date = DateTime.Parse(dr["special_price_end_date"].ToString());
		special_price_end_date = DateTime.Parse(sped);
		
		
		int name_Length = name.Length;
//		if(name_Length > 16)
//			name_Length = 16;
//		name = name.Substring(0, name_Length);

		int name_cn_Length = name_cn.Length;
//		if(name_cn_Length > 20)
//			name_cn_Length = 20;
//		name_cn = name_cn.Substring(0, name_cn_Length);
		
		price = dr["price"].ToString();
//		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
//			price = dr["special_price"].ToString();
		if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}
        country_of_origin = dr["country_of_origin"].ToString();
        country = dr["country"].ToString();

		if(i % 3 == 0)
		{
			sb.Append("</tr><tr>");
		}
		/*
		sb.Append("<td width=50% valign=top>");
		sb.Append("<table width=100% border=0 bordercolor=#0000FF style='font-family:Verdana;font-size:18pt;border-color:#EEEEEE;border-width:1px;border-style:Dashed;border-collapse:collapse;fixed'>");

        
        sb.Append("<tr><td colspan=\"2\" style=font-size:16pt;><B>" + name + "</B></td></tr>");
		sb.Append("<tr><td colspan=\"2\" style=font-size:17pt;><B>" + name_cn + "</B></td></tr>");
		sb.Append("<tr><td style=font-size:9pt;><B>" + supplier_code + "</B></td>");
		sb.Append("<td style=font-size:18pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		sb.Append("</table>");
		sb.Append("</td>");
		*/
		
		sb.Append("<td width=33% valign=top>");
		sb.Append("<table width=100% border=0 bordercolor=#0000FF style='font-family:Verdana;font-size:18pt;border-color:#000000;border-width:1px;border-style:Dashed;border-collapse:collapse;fixed'>");
		if(name.Length <= 16)
            //sb.Append("<tr><td colspan=\"2\" style=font-size:15pt;height:27px><B>" + name + "</B></td></tr>");
            sb.Append("<tr><td colspan=\"2\" style=font-size:10pt;height:27px><B>" + name + "</B></td></tr>");
		else if (name.Length >16 && name.Length <23)
            //sb.Append("<tr><td colspan=\"2\" style=font-size:12pt;height:27px><B>" + name + "</B></td></tr>");
            sb.Append("<tr><td colspan=\"2\" style=font-size:10pt;height:27px><B>" + name + "</B></td></tr>");
		else if (name.Length >=23)
           	sb.Append("<tr><td  colspan=\"2\" style=font-size:10pt;height:27px><B>" + name + "</B></td></tr>");
	//	sb.Append("<tr><td colspan=\"2\" style=font-size:16pt;><B>" + name + "</B></td></tr>");
		sb.Append("<tr><td colspan=\"2\">");
		sb.Append("<table width=100% >");
		if(name_cn.Length <= 20)
			sb.Append("<tr><td colspan=\"2\" style=font-size:9pt;height:15px >" + name_cn + "</td><td align=right valign=bottom style=font-size:30pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		else
			sb.Append("<tr><td colspan=\"2\" style=font-size:9pt;height:15px >" + name_cn + "</td><td align=right valign=bottom style=font-size:30pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		sb.Append("<tr><td height=10.5></td></tr>");
		sb.Append("<tr><td style=font-size:11pt;height:24px ><B>" + code + "</B></td>");
        sb.Append("<td align=right colspan=\"2\" style=\"font-size:11pt;height:15px; ");

        if(country.IndexOf("NZ-") != -1 || country.IndexOf("New Zealand") != -1)
            sb.Append("color:#008000\"");
        else if(country_of_origin == "International_Regions")
             sb.Append("color:#000000\"");
        else
            sb.Append("color:#000000\"");
        sb.Append("><B>" + country + "</B></td>");
        sb.Append("</tr>");
		sb.Append("</table>");
		sb.Append("</td><tr>");
		
		/*
		sb.Append("<tr><td colspan=\"2\" style=font-size:18pt; ><B>" + name_cn + "</B></td></tr>");
		sb.Append("<tr><td style=font-size:9pt; ><B>" + supplier_code + "</B></td>");
		sb.Append("<td align=right style=font-size:20pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		*/
		sb.Append("</table>");
		sb.Append("</td>");
		//DEBUG("i" , (i %2).ToString());
		///DEBUG("ROWS ", rows.ToString());
		if( i ==(rows-1) && i % 3== 0)
	//	if( i % 3== 0)
			sb.Append("<td width=33% style='abcd'>&nbsp;</td>");
		
	}
/*

	int start = 0;
	int nRowsPerPage = 10;
	for(int i=0; i<ds.Tables["data"].Rows.Count; i += nRowsPerPage * 2)
	{	
		sb.Append("<table width=700 align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 class=t>");
		sb.Append("<tr><td valign=top>");
		sb.Append(PrintHalfPage(start, nRowsPerPage));
		start += nRowsPerPage;
		sb.Append("</td><td valign=top>");
		sb.Append(PrintHalfPage(start, nRowsPerPage));
		start += nRowsPerPage;
		sb.Append("</td></tr>");
		sb.Append("</table>");
	}
*/
	Response.Write(sb.ToString());
	return true;
}
bool PrintNewLayout2x4sc()
{
	
	if(!GetFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
    string country_of_origin = "";
	string core_range="";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;

	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		if(i % 8 == 0)
		{
			st += "<div class='a4Paper'>";
		}
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
		supplier = dr["supplier"].ToString();
       	supplier_code = dr["supplier_code1"].ToString();
		barcode = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		core_range=dr["core_range"].ToString();
		price = dr["price"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
	
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_sc glabel_2x4'>";
		if(core_range=="False"){
			st += " <div class='wrap'><span class='clearance'>CLEARANCE</span></div>";
		}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
		st += "<div class='code'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='30' jsbarcode-width='1' jsbarcode-fontsize='12'> ";
		st +="</svg>";
		st += "</div>";
		st += " <div class='rtccode'>RTC CODE:"+code+"</div> ";
		st += " <div class='price'><div class='container'>"+MyMoneyParse(price).ToString("c")+"</div></div>";
		st += "</div>";
		
		if((i+1) % 8 == 0)
		{
			st += "</div>";
			
		}
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}
bool PrintNewLayout2x4multi()
{
	
	if(!GetPromoFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(promods.Tables[tableName] != null)
		rows = promods.Tables[tableName].Rows.Count;
	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string dis_price = "";
	string dis_qty = "";
	string sped = "";
    string country_of_origin = "";
	string core_range="";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;

	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<promods.Tables["data"].Rows.Count; i++)
	{


		DataRow dr = promods.Tables["data"].Rows[i];
		if(i % 16 == 0)
		{
			st += "<div class='a4Paper'>";
		}
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
	
		barcode = dr["barcode"].ToString();
		name = dr["name"].ToString();
		price=dr["price"].ToString();
		dis_qty = dr["volumn_discount_qty"].ToString();
		dis_price = dr["volumn_discount_price_total"].ToString();
	
	

		sped = dr["special_price_end_date"].ToString();
		core_range=dr["core_range"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_sc glabel_2x4'>";
		if(core_range=="False"){
			st += " <div class='wrap'><span class='clearance'>CLEARANCE</span></div>";
		}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
		st += "<div class='code'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='30' jsbarcode-width='1' jsbarcode-fontsize='12'> ";
		st +="</svg>";
		st += "</div>";
		st += " <div class='rtccode'>RTC CODE:"+code+"</div> ";
		st += " <div class='unitPrice'>"+MyMoneyParse(price).ToString("c")+"EA</div> ";
		st += " <div class='mutiprice'>"+dis_qty+"<p class='small'>&nbsp;&nbsp;for&nbsp;&nbsp; </p>"+MyMoneyParse(dis_price).ToString("c")+"</div>";
		st += "</div>";
		
		if((i+1) % 8 == 0)
		{
			st += "</div>";
			
		}
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}

bool PrintNewLayoutA4RTC()
{
	
	if(!GetFlareData())
		return false;

		
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
		string core_range="";
    bool bCentral = false;

	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
	
			st += "<div class='single_a4Paper'>";
		
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
		supplier = dr["supplier"].ToString();
       	supplier_code = dr["supplier_code1"].ToString();
		barcode = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		core_range=dr["core_range"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_sc glabel_a4'>";
	if(core_range=="False"){
		st+=" <div class='rtc'>REDUCED TO CLEAR</div>";
	}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
		 st += " <div class='price'><div class='container bigfont'>"+MyMoneyParse(price).ToString("c")+"</div></div>";
		st += "<div class='bottom'><div class='code'><div class='container'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='50' jsbarcode-width='2' jsbarcode-fontsize='20'> ";
		st +="</svg>";
		st += "</div></div>";
		st += " <div class='rtccode'><div class='container'>RTC CODE:"+code+"</div></div></div> ";
		
		st += "</div>";
		
		
			st += "</div>";
			
		
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}





bool PrintNewLayoutA4()
{
	
	if(!GetFlareData())
		return false;

		
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
		string core_range="";
    bool bCentral = false;

	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
	
			st += "<div class='single_a4Paper'>";
		
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
		supplier = dr["supplier"].ToString();
       	supplier_code = dr["supplier_code1"].ToString();
		barcode = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		core_range=dr["core_range"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_sc glabel_a4'>";
	if(core_range=="False"){
		st+=" <div class='clearance'>CLEARANCE</div>";
	}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
		 st += " <div class='price'><div class='container bigfont'>"+MyMoneyParse(price).ToString("c")+"</div></div>";
		st += "<div class='bottom'><div class='code'><div class='container'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='50' jsbarcode-width='2' jsbarcode-fontsize='20'> ";
		st +="</svg>";
		st += "</div></div>";
		st += " <div class='rtccode'><div class='container'>RTC CODE:"+code+"</div></div></div> ";
		
		st += "</div>";
		
		
			st += "</div>";
			
		
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}
bool PrintNewLayoutA4multi()
{
	
	if(!GetPromoFlareData())
		return false;

		
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(promods.Tables[tableName] != null)
		rows = promods.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
		string core_range="";
    bool bCentral = false;
string dis_price="";
string dis_qty="";
	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<promods.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = promods.Tables["data"].Rows[i];
	
			st += "<div class='single_a4Paper'>";
		
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
	    
		barcode = dr["barcode"].ToString();
		name = dr["name"].ToString();
	dis_qty = dr["volumn_discount_qty"].ToString();
		dis_price = dr["volumn_discount_price_total"].ToString();
		sped = dr["special_price_end_date"].ToString();
		core_range=dr["core_range"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_sc glabel_a4'>";
	if(core_range=="False"){
		st+=" <div class='clearance'>CLEARANCE</div>";
	}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
		 st += " <div class='price'><div class='container'>"+dis_qty+"<p class='small'>&nbsp;&nbsp;&nbsp;for&nbsp;&nbsp;&nbsp;</p>"+MyMoneyParse(dis_price).ToString("c")+"</div></div>";
		 st += " <div class='unitPrice'><div class='container ea '>"+MyMoneyParse(price).ToString("c")+"</div></div>";
		st += "<div class='bottom'><div class='code'><div class='container'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='50' jsbarcode-width='2' jsbarcode-fontsize='20'> ";
		st +="</svg>";
		st += "</div></div>";
		st += " <div class='rtccode'><div class='container'>RTC CODE:"+code+"</div></div></div> ";
		
		st += "</div>";
		
		
			st += "</div>";
			
		
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}
bool PrintNewLayoutA5()
{
	
	if(!GetFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
	
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;
  string core_range="";
	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	

	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
	
			st += "<div class='a5Paper'>";
		
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
		supplier = dr["supplier"].ToString();
       	supplier_code = dr["supplier_code1"].ToString();
		barcode = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	core_range=dr["core_range"].ToString();

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_a5'>";
	if(core_range=="False"){
		st+=" <div class='clearance'>CLEARANCE</div>";
	}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
		 st += " <div class='price'><div class='container bigfont'>"+MyMoneyParse(price).ToString("c")+"</div></div>";
		st += "<div class='bottom'><div class='code'><div class='container'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='50' jsbarcode-width='2' jsbarcode-fontsize='20'> ";
		st +="</svg>";
		st += "</div></div>";
		st += " <div class='rtccode'><div class='container'>RTC CODE: "+code+"</div></div></div> ";
		
		st += "</div>";
		
		
			st += "</div>";
			
		
	}
 
	Response.Write(st);
	
	return true;
	
}
bool PrintNewLayoutA5multi()
{
	
	if(!GetPromoFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(promods.Tables[tableName] != null)
		rows = promods.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string dis_price="";
string dis_qty="";
	string sped = "";
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;
  string core_range="";
	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	

	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<promods.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = promods.Tables["data"].Rows[i];
	
			st += "<div class='a5Paper'>";
		
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();

		barcode = dr["barcode"].ToString();
		name = dr["name"].ToString();
	
			dis_qty = dr["volumn_discount_qty"].ToString();
		dis_price = dr["volumn_discount_price_total"].ToString();
		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	core_range=dr["core_range"].ToString();

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_a5'>";
	if(core_range=="False"){
		st+=" <div class='clearance'>CLEARANCE</div>";
	}
 		st += "<div class='name'><div class='container'>"+name+"</div></div>";
	 st += " <div class='multiprice'><div class='container '>"+dis_qty+"<p class='small'>&nbsp;&nbsp;&nbsp;for&nbsp;&nbsp;&nbsp;</p>"+MyMoneyParse(dis_price).ToString("c")+"</div></div>";
		 st += " <div class='unitPrice'><div class='container ea '>"+MyMoneyParse(price).ToString("c")+"</div></div>";		st += "<div class='bottom'><div class='code'><div class='container'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='50' jsbarcode-width='2' jsbarcode-fontsize='20'> ";
		st +="</svg>";
		st += "</div></div>";
		st += " <div class='rtccode'><div class='container'>RTC CODE: "+code+"</div></div></div> ";
		
		st += "</div>";
		
		
			st += "</div>";
			
		
	}
 
	Response.Write(st);
	
	return true;
	
}
bool PrintNewLayout2x10()
{
	
	if(!GetPromoFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(promods.Tables[tableName] != null)
		rows = promods.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string dis_qty = "";
	string dis_price = "";
    string country_of_origin = "";
	string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;
string sped="";
	int nItemsPerPage = 10000; 
	

	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<promods.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = promods.Tables["data"].Rows[i];
		if(i % 20 == 0)
		{
			st += "<div class='a4Paper'>";
		}
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
	
		barcode = dr["barcode"].ToString();
		name = dr["name"].ToString();
		price=dr["price"].ToString();
		dis_qty = dr["volumn_discount_qty"].ToString();
		dis_price = dr["volumn_discount_price_total"].ToString();
	if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
	
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}
	

	

	
		
		// price = dr["price"].ToString();
		// if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
		// 	price = dr["special_price"].ToString();
		// else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		// {
		// 	if(MyDoubleParse(dr["price4"].ToString()) != 0 )
		// 		price = dr["price4"].ToString();
		// }

		st += "<div class='glabel_2x10'>";
 		st += "<div class='name'>"+name+"</div>";
		st += "<div class='code'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
		//    st += " jsbarcode-value='1234567891234' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='20' jsbarcode-width='1' jsbarcode-fontsize='12'> ";
		st +="</svg>";
		st += "</div>";
		st += " <div class='rtccode'>RTC CODE: "+code+"</div> ";

		st += " <div class='unitPrice'>"+MyMoneyParse(price).ToString("c")+"EA</div> ";
		st += " <span class='price '>"+dis_qty+"<p class='small'> &nbsp;&nbsp;for&nbsp;&nbsp;</p>"+MyMoneyParse(dis_price).ToString("c")+"</span>";
		// st += " <span class='price '>"+dis_qty+"<p class='small'> &nbsp;for&nbsp;</p>"+MyMoneyParse("99").ToString("c")+"</span>";

		st += "</div>";
		
		if((i+1) % 20 == 0)
		{
			st += "</div>";
			
		}
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}
bool PrintNewLayout3x10_CN()
{
	if(!GetFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;

	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		if(i % 30 == 0)
		{
			st += "<div class='a4Paper'>";
		}
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
		supplier = dr["supplier"].ToString();
       	supplier_code = dr["supplier_code1"].ToString();
		barcode = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel_yans3x10'>";
		st += " <div class='label_yans3x10_border'>";
 		st += "<div class='label_yans3x10_name'>"+name+"</div>";
		st += " <div class='label_yans3x10_cnname'>"+name_cn+"</div>";
		st += "<div class='label_yans3x10_code'>";
		st += "<svg class='barcode' jsbarcode-format='CODE128' ";
        st += " jsbarcode-value='"+barcode+"' jsbarcode-textmargin='-2' ";
        st += " jsbarcode-fontoptions='bold' jsbarcode-margin='0' jsbarcode-height='20' jsbarcode-width='1' jsbarcode-fontsize='12'> ";
		st +="</svg>";
		st += "</div>";
		//st += " <div class='label_itemcode'>plu: "+code+"</div> ";
		//st += " <div class='label_barcodeNum'>"+barcode+"</div> ";
		st += " <div class='label_yans3x10_suppliername'>"+supplier+" ("+supplier_code+")</div> ";
		st += " <div class='label_yans3x10_price'>"+MyMoneyParse(price).ToString("c")+"</div>";
		st += "</div></div>";
		
		if((i+1) % 30 == 0)
		{
			st += "</div>";
			
		}
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}

bool PrintNewLayout3x10()
{
	if(!GetFlareData())
		return false;
	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));
	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string barcode = "";
	string supplier_code = "";
	string supplier = "";
	string sped = "";
    string country_of_origin = "";
	    string country = "";
	DateTime special_price_end_date ;	
	    string central_warehouse = "";
    bool bCentral = false;

	int nItemsPerPage = 10000; 
	
	string st = "";
//	st += "<button class='btn btn-primary' type='button' id='printLabel'>Print Label</button>";
	
	// st += "  <div id='printContainer'></div>";
	st += "<div  id='printArea'>";
	
//  st += "<div class='a4Paper'>";
	string ist = "";
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		if(i % 8 == 0)
		{
			st += "<div class='a4Paper'>";
		}
		
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
		// supplier = dr["supplier"].ToString();
       	supplier_code = dr["supplier_code1"].ToString();
		barcode = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
		special_price_end_date = DateTime.Parse(sped);
	

		int name_Length = name.Length;
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}

		st += "<div class='glabel'>";
		st+= "<img class='label_logo' src='../pic/total_logo.jpg' >";
		st+= "<img class='label_pic' src='../pi/"+supplier_code+".jpg' >";
 		st += "<div class='label_name'>"+name+"</div>";

		st += " <div class='label_supplier_code'> "+supplier_code+"</div> ";
		st += " <div class='label_price'>"+MyMoneyParse(price).ToString("c")+"</div>";
		st += "</div>";
		
		if((i+1) % 8 == 0)
		{
			st += "</div>";
			
		}
	}
    st += "</div>";
	Response.Write(st);
	
	return true;
	
}







bool PrintPriceList(int page, int col)
{
	if(!GetFlareData())
		return false;

	int page_rows = MyIntParse(GetSiteSettings("flare_rows_per_page", "50"));
	string tableName = "data";

	int rows = 0;
	if(ds.Tables[tableName] != null)
		rows = ds.Tables[tableName].Rows.Count;

	StringBuilder sb = new StringBuilder();
	sb.Append(ReadSitePage("admin_page_header"));

	string code = "";
	string name = "";
	string name_cn = "";
	string price = "";
    string supplier_code = "";
	string sped = "";
    string country_of_origin = "";
	DateTime special_price_end_date ;	

	if(col != 0);
		labels = col;
	int nItemsPerPage = 10000; //MyIntParse(GetSiteSettings("flare_items_per_page", "20"));

	string st = "<table width=\"100%\" align=center valign=center cellspacing=1 cellpadding=3 border=0 bordercolor=#000000 >";
	string sth = "<table width=\"100%\" align=center valign=center cellspacing=1 cellpadding=3 border=0 bordercolor=#FF0000 >";

DEBUG("rows=", ds.Tables["data"].Rows.Count);
	for(int i=0; i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];
		if(i % nItemsPerPage == 0)
		{
			if(i > 0)
			{
				sb.Append("</td></tr></table>");
				sb.Append("<p style = page-break-before:always>");
			}
			sb.Append(st);
			sb.Append("<tr><td valign=top width=50%>");
			sb.Append(sth);
			sb.Append("<tr>");	
							
		}	
			
		DateTime today = DateTime.Now;
		code = dr["code"].ToString();
       		supplier_code = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		sped = dr["special_price_end_date"].ToString();
		if(sped==null || sped=="")
			sped = "1900-01-01 00:00:00.000";
//		special_price_end_date = DateTime.Parse(dr["special_price_end_date"].ToString());
		special_price_end_date = DateTime.Parse(sped);


		int name_Length = name.Length;
//		if(name_Length > 15)
//			name_Length = 15; 
//		name = name.Substring(0, name_Length);
		if(name==null || name=="")
			name="&nbsp;";

		int name_cn_Length = name_cn.Length;
//		if(name_cn_Length > 20)
//			name_cn_Length = 20; 
//		name_cn = name_cn.Substring(0, name_cn_Length);
		if(name_cn==null || name_cn=="")
			name_cn="&nbsp;";
		
		price = dr["price"].ToString();
//		if(dr["is_special"].ToString() == "True")
		if(dr["is_special"].ToString() == "True" && DateTime.Compare(today, special_price_end_date) < 0)
			price = dr["special_price"].ToString();
		else if(dr["price4"].ToString() != "" && dr["price4"].ToString() != null)
		{
			if(MyDoubleParse(dr["price4"].ToString()) != 0 )
				price = dr["price4"].ToString();
		}
        country_of_origin = dr["country_of_origin"].ToString();
		
		if(i % labels == 0)
		{
			sb.Append("</tr><tr>");
		}
		/*
		sb.Append("<td width=50% valign=top>");
		sb.Append("<table width=100% border=0 bordercolor=#0000FF style='font-family:Verdana;font-size:18pt;border-color:#EEEEEE;border-width:1px;border-style:Dashed;border-collapse:collapse;fixed'>");

        
        sb.Append("<tr><td colspan=\"2\" style=font-size:16pt;><B>" + name + "</B></td></tr>");
		sb.Append("<tr><td colspan=\"2\" style=font-size:17pt;><B>" + name_cn + "</B></td></tr>");
		sb.Append("<tr><td style=font-size:9pt;><B>" + supplier_code + "</B></td>");
		sb.Append("<td style=font-size:18pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		sb.Append("</table>");
		sb.Append("</td>");
		*/
		
		sb.Append("<td width=50% valign=top>");
		sb.Append("<table width=100% border=0 bordercolor=#0000FF style='font-family:Verdana;font-size:18pt;border-color:#000000;border-width:1px;border-style:Dashed;border-collapse:collapse;fixed'>");
	    if(name.Length <= 15)
            sb.Append("<tr><td  colspan=\"2\" style=font-size:17pt;height:30px><B>" + name + "</B></td></tr>");
        else
            sb.Append("<tr><td  colspan=\"2\" style=font-size:13pt;height:30px><B>" + name + "</B></td></tr>");

		sb.Append("<tr><td colspan=\"2\">");
		sb.Append("<table width=100% >");
//		sb.Append("<tr><td colspan=\"2\" style=font-size:14pt; ><B>" + name_cn + "</B></td><td align=right valign=bottom style=font-size:27pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		if(name_cn.Length <= 20)
            sb.Append("<tr><td  colspan=\"2\" style=font-size:14pt;height:28><B>" + name_cn + "</B></td></tr>");
		else
			sb.Append("<tr><td  colspan=\"2\" style=font-size:9pt;height:28><B>" + name_cn + "</B></td></tr>");
        sb.Append("<tr><td colspan=\"2\" style=font-size:9pt;height:15px >" + country_of_origin + "</td></tr>");

		sb.Append("<tr><td colspan=\"2\" align=center valign=bottom style=font-size:49pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		sb.Append("<tr><td height=25></td></tr>");
        sb.Append("<tr><td style=font-size:15pt; ><B>" + supplier_code + "</B></td></tr>");
		sb.Append("</table>");
		sb.Append("</td><tr>");
		
		/*
		sb.Append("<tr><td colspan=\"2\" style=font-size:18pt; ><B>" + name_cn + "</B></td></tr>");
		sb.Append("<tr><td style=font-size:9pt; ><B>" + supplier_code + "</B></td>");
		sb.Append("<td align=right style=font-size:20pt;><B>" + MyMoneyParse(price).ToString("c") + "</B></td></tr>");
		*/
		sb.Append("</table>");
		sb.Append("</td>");
		//DEBUG("i" , (i %2).ToString());
		///DEBUG("ROWS ", rows.ToString());
		if( i ==(rows-1) && i % 2== 0)
			sb.Append("<td width=50% style='abcd'>&nbsp;</td>");
		
	}
/*

	int start = 0;
	int nRowsPerPage = 10;
	for(int i=0; i<ds.Tables["data"].Rows.Count; i += nRowsPerPage * 2)
	{	
		sb.Append("<table width=700 align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 class=t>");
		sb.Append("<tr><td valign=top>");
		sb.Append(PrintHalfPage(start, nRowsPerPage));
		start += nRowsPerPage;
		sb.Append("</td><td valign=top>");
		sb.Append(PrintHalfPage(start, nRowsPerPage));
		start += nRowsPerPage;
		sb.Append("</td></tr>");
		sb.Append("</table>");
	}
*/
	Response.Write(sb.ToString());
	return true;
}



bool doAddItem(string code)
{
	
	//  string sc = " INSERT INTO flare (code) select c.code FROM code_relations c ";
	// sc += " WHERE c.supplier_code=N'"+code+"' ";
	// sc += " OR c.code = (SELECT b.item_code FROM barcode b WHERE b.barcode = '"+code+"')";
string myBranchId=m_branchID;
if(m_branchID==null||m_branchID=="")
{
myBranchId="1";
}
    // string sc = "if EXISTS(SELECT b.item_code FROM barcode b WHERE b.barcode = '"+code+"') ";
	  string sc = "";
	sc +=" INSERT INTO flare (code ) values( (select c.code FROM code_relations c ";
	sc += " WHERE c.supplier_code=N'"+code+"' ";
	sc += " ))";
	// sc +=" OR c.code="+ code;
		// DEBUG("sc=",sc);
		// return false;
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
string PrintHalfPage(int start, int rows)
{	
	string s_cat = "";
	string code = "";
    string supplier_code = "";
	string name = "";
	string name_cn = "";
	string price = "";
	int count = 0;
	
	string th1 = "<table width=50% align=left valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#0000EE class=t>";
	
	StringBuilder sb1 = new StringBuilder();
	sb1.Append(th1);

	int end = start + rows;
	for(int i=start; i<end && i<ds.Tables["data"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["data"].Rows[i];

		s_cat = dr["s_cat"].ToString();
		Trim(ref s_cat);
		code = dr["code"].ToString();
        supplier_code = dr["supplier_code"].ToString();
		name = dr["name"].ToString();
		name_cn = dr["name_cn"].ToString();
		price = dr["price"].ToString();
		if(dr["is_special"].ToString() == "1")
			price = dr["special_price"].ToString();
		sb1.Append("<tr>");
		sb1.Append("<td width=100%>");
		sb1.Append("<table width=100%>");
		sb1.Append("<tr align=right>" + name + "&nbsp&nbsp;</tr>");
		sb1.Append("<tr align=right>" + name_cn + "</tr>");
		//sb1.Append("<tr align=right>" + code + "</tr>");
        sb1.Append("<tr align=right>" + supplier_code + "</tr>");
		sb1.Append("<tr align=right>" + price + "</tr>");
		sb1.Append("</table>");
		sb1.Append("</td>");
		sb1.Append("</tr>");
	}
	sb1.Append("</table>");
	return sb1.ToString();
}

</script>

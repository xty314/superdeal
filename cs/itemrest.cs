<!-- #include file="page_index.cs" -->

<script runat="server">

/**********************************************************
restore item to product table, as items are no longer 
selling. 
purpose: for credit or resell item ##20-04-04
**********************************************************/

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_search = "";
string m_command = "";
string m_querystring = "";
string m_invoice = "";

string m_cat = "";
string m_scat = "";
string m_sscat = "";

bool bHide = true;
bool m_bIsFound = false;  //flag for searching sn on the database
bool m_bIsFirst = true;
bool m_bVerified = false; // verified for public site


void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	InitializeData();
	GetAllQueryString();
	
	if(Request.QueryString["t"] == "done")
	{
		Response.Write("<br><center><h4>RESTORE ITEM DONE!!!</h4>");
		Response.Write("<br><h5><a title='Back to Restore Item' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"' class=o>Back to Restore Item</a>");
		Response.Write("<br><br><h5><a title='go to order list' href='olist.aspx?o=11&r="+ DateTime.Now.ToOADate() +"' class=o>Go to Order List</a>");
		return;
	}
	if(m_command.ToLower() == "restore item")
	{
		if(DoInsertItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?t=done&r="+ DateTime.Now.ToOADate() +" \">");
			return;
		}
	}
	
	if(!DoSearchItem())
		return;


}


void GetAllQueryString()
{
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		m_command = Request.Form["cmd"];
	if(Request.Form["search"] != null && Request.Form["search"] != "")
		m_search = Request.Form["search"];
	if(Request.QueryString["i"] != null && Request.QueryString["i"] != "")
		m_invoice = Request.QueryString["i"];
	if(Request.QueryString["inv"] != null && Request.QueryString["inv"] != "")
		m_invoice = Request.QueryString["inv"];
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "" && Request.QueryString["cat"] != "all")
		m_cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "" && Request.QueryString["scat"] != "all")
		m_scat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "" && Request.QueryString["sscat"] != "all")
		m_sscat = Request.QueryString["sscat"];
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_search = Request.QueryString["id"];
//DEBUG(" cat = ", m_cat );
//DEBUG(" scat = ", m_scat );
//DEBUG(" sscat = ", m_sscat );
}



bool DoInsertItem()
{
	
	string code = Request.Form["code"];
	string id = Request.Form["id"];
	if(id != null && id != "" && code != null && code != "")
	{
		//notes = "\r\n" + notes +"\r\n";
		string sc = "";
		sc += " IF NOT EXISTS ( select code FROM product WHERE code = "+ code +" ) ";
		sc += " INSERT INTO product ";
		sc += " SELECT code, name, brand, cat, s_cat, ss_cat, hot, (manual_cost_frd + nzd_freight) * rate, 0, ''";
		sc += ", supplier, supplier_code, supplier_price, 0, 0, 0, 0, 0 ";
		sc += " FROM code_relations ";
		sc += " WHERE id = '"+ id +"' ";
		sc += " AND code = "+ code;
	//DEBUG("sc = ", sc);
		try
		{
			myCommand = new SqlCommand(sc);
			myCommand.Connection = myConnection;
			myConnection.Open();
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


bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT RTRIM(LTRIM(cat)) AS cat FROM product p  ORDER BY RTRIM(LTRIM(cat))";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows <= 0)
		return true;
	Response.Write("Catalog Select: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "");
	Response.Write("&cat='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");

//DEBUG("mcat = ", m_cat);
	string cat_scode = "";
	string cat_fcode = "";
	string scat_scode = "";
	string scat_fcode = "";
	string sscat_scode = "";
	string sscat_fcode = "";
		
	for(int i=0; i<rows; i++)
	{
		
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		
		if(m_cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(m_cat != null && m_cat != "" && m_cat != "all")
	{
		sc = "SELECT DISTINCT RTRIM(LTRIM(s_cat)) AS s_cat FROM product  WHERE cat = '"+ m_cat +"' ";
		sc += " ORDER BY RTRIM(LTRIM(s_cat))";
//DEBUG("sc = ", sc);
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
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&");

		Response.Write("cat="+ HttpUtility.UrlEncode(m_cat) +"&scat='+ escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");

		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			//DEBUG(" s = ", s);
//DEBUG(" scat = ", s_cat);
			if(m_scat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
		}

		Response.Write("</select>");
	}
	if(m_scat != null && m_scat != ""  && m_scat != "all")
	{
		sc = "SELECT DISTINCT RTRIM(LTRIM(ss_cat)) AS ss_cat FROM product p WHERE cat = '"+ m_cat +"' ";
		sc += " AND s_cat = '"+ m_scat +"' ";
		sc += " ORDER BY RTRIM(LTRIM(ss_cat)) ";
//DEBUG("sc = ", sc);
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
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&");

		Response.Write("cat="+ HttpUtility.UrlEncode(m_cat) +"&scat="+ HttpUtility.UrlEncode(m_scat) +"&sscat='+ escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");

		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(m_sscat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}
	return true;
}

bool DoSearchItem()
{
	int rows = 0;
//	if(m_search != null && m_search != "")
//	{

	string sc = " SELECT * FROM code_relations ";
	sc += " WHERE 1 = 1 ";
	if(m_search != null && m_search != "")
	{
		if(TSIsDigit(m_search))
			sc += " AND code = '"+ m_search +"' ";
		else
			sc += " AND name LIKE '%"+ EncodeQuote(m_search) +"%' OR id LIKE '%"+ EncodeQuote(m_search) +"%' OR supplier_code LIKE '%"+ EncodeQuote(m_search) +"%'";
	}
	if(m_cat != "" && m_cat != null && m_cat != "all")
		sc += " AND cat = '"+ m_cat +"' ";
	if(m_scat != "" && m_scat != null && m_scat != "all")
		sc += " AND s_cat = '"+ m_scat +"' ";
	if(m_sscat != "" && m_sscat != null && m_sscat != "all")
		sc += " AND ss_cat = '"+ m_sscat +"' ";
//DEBUG("sc = ", sc );
	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "code_relations");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;

	m_cPI.URI = "?r="+ DateTime.Now.ToOADate();
	if(m_cat != null && m_cat != "" && m_cat != "all")
		m_cPI.URI += "&cat="+ HttpUtility.UrlEncode(m_cat) +"";
	if(m_scat != null && m_scat != "" && m_scat != "all")
		m_cPI.URI += "&scat="+ HttpUtility.UrlEncode(m_scat) +"";
	if(m_sscat != null && m_sscat != "" && m_sscat != "all")
		m_cPI.URI += "&sscat="+ HttpUtility.UrlEncode(m_sscat) +"";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	Response.Write("<h4><center>Restore Discontinued Items</center></h4>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=0 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");
	DoItemOption();
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Search Item: <input type=text name=search value='"+ Request.Form["search"] +"'>");
	Response.Write("<input type=submit name=cmd value=' Search ' "+ Session["button_style"] +">");
	Response.Write("</td></tr>");
	Response.Write("<script language=javascript>document.frm.search.focus();document.frm.search.select();</script");
	Response.Write(">\r\n");
	Response.Write("</table>");
	Response.Write("</form>");
	if(rows == 1)
	{
		ShowFoundItem();
		return false;
	}
//	Response.Write("<hr size=0 color=gray width=60%>");
	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=10>"+ sPageIndex +"</td></tr>");
	Response.Write("<tr align=left bgcolor=#EEEFF3><th>ID</th><th>CODE</th><th>SUPP_CODE</th><th>BRAND</th><th>CAT</th>");
	Response.Write("<th>S_CAT</th><th>SS_CAT</th><th>NAME</th><th>SUPPLIER</th><th>SUPP_PRICE</th></tr>");
	bool bAlter = false;
	//for(int i=0; i<rows; i++)
	for(; i<rows && i<end; i++)
	{
		
		DataRow dr = dst.Tables["code_relations"].Rows[i];
		string id = dr["id"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string brand = dr["brand"].ToString();
		string cat = dr["cat"].ToString();
		string s_cat = dr["s_cat"].ToString();
		string ss_cat = dr["ss_cat"].ToString();
		string name = dr["name"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		
		Response.Write("<tr ");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(" >");
		bAlter = !bAlter;
		Response.Write("<td>"+ id +"</td>");
		Response.Write("<td>"+ code +"</td>");
		Response.Write("<td>"+ supplier_code +"</td>");
		Response.Write("<td>"+ brand +"</td>");
		Response.Write("<td>"+ cat +"</td>");
		Response.Write("<td>"+ s_cat +"</td>");
		Response.Write("<td>"+ ss_cat +"</td>");
		Response.Write("<td><a title='select this item' href='"+ Request.ServerVariables["URL"] +"?id="+ code +"&r="+ DateTime.Now.ToOADate() +"' class=o>"+ name +"</a></td>");
		Response.Write("<td>"+ supplier +"</td>");
		Response.Write("<td>"+ supplier_price +"</td>");
		
		Response.Write("</tr>");

	}
	Response.Write("</table><br><br>");
	return true;
}

void ShowFoundItem()
{
	string salign = "right";
	Response.Write("<form name=f method=post>");
	Response.Write("<table width=40% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th colspan=2  bgcolor=#EEEFF3 >ITEM FOUND</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >ID</td><td>"+ dst.Tables["code_relations"].Rows[0]["id"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >SUPPLIER</td><td>"+ dst.Tables["code_relations"].Rows[0]["supplier"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >SUPPLER_CODE</td><td>"+ dst.Tables["code_relations"].Rows[0]["supplier_code"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >CODE</td><td>"+ dst.Tables["code_relations"].Rows[0]["code"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >BRAND</td><td>"+ dst.Tables["code_relations"].Rows[0]["brand"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >CAT</td><td>"+ dst.Tables["code_relations"].Rows[0]["cat"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >S_CAT</td><td>"+ dst.Tables["code_relations"].Rows[0]["s_cat"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >SS_CAT</td><td>"+ dst.Tables["code_relations"].Rows[0]["ss_cat"].ToString() +"</td></tr>");
	Response.Write("<tr><td align="+ salign +" bgcolor=#EEEFF3 >NAME</td><td>"+ dst.Tables["code_relations"].Rows[0]["name"].ToString() +"</td></tr>");
	Response.Write("<tr  bgcolor=#EEEFF3 align=right><td colspan=2><input type=submit name=cmd value='RESTORE ITEM' "+ Session["button_style"] +" onclick=\"if(!confirm('RESTORE ITEM....')){return false;}\"> </td></tr>");
	Response.Write("</table>");
	Response.Write("<input type=hidden name=id value='"+ dst.Tables["code_relations"].Rows[0]["id"].ToString() +"'>");
	Response.Write("<input type=hidden name=code value='"+ dst.Tables["code_relations"].Rows[0]["code"].ToString() +"'>");
	Response.Write("</form>");
}





</script>
<asp:Label id=LFooter runat=server/>

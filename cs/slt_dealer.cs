<!-- #include file="page_index.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table
string m_id = "";
string m_name = "";
string m_sBranch = "";
string m_last_search = "";
string cat = "";
string s_cat = "";
string ss_cat = "";
string sSystem = "";
string sOption = "";
string ra_id = "";
string ra_code = "";

int m_RowsReturn = 0;
int m_SerialReturn = 0;

//current edit products
string m_sn = "";
string m_product_code = "";
string m_cost = "";
string m_purchase_date = "";
string m_branch_id = "";
string m_po_number = "";
string m_supplier = "";
string m_supplier_code = "";
string m_status = "";
string m_snQuery= "";
string m_prodQuery = "";
string m_search = "";
string m_sort = "";
bool m_bDesc = false;

string m_last_url = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;
	
	if(Request.QueryString["sort"] != null)
		m_sort = Request.QueryString["sort"];
	if(Request.QueryString["desc"] == "1")
		m_bDesc = true;
		
	if(Request.QueryString["uri"] != null)
	{
		Session["last_uri"] = Request.QueryString["uri"];
	}
	
	if(Request.QueryString["did"] != null && Request.QueryString["did"] != "")
	{
		//DEBUG("uri= ", Session["last_uri"].ToString());
		Session["slt_dealer_id"] = Request.QueryString["did"];
		//Session["slt_dealer_name"] = Request.QueryString["name"];
		//if(Session["slt_dealer_name"] == "" && Session["slt_dealer_name"] == null)
			Session["slt_dealer_name"] = Request.QueryString["cp"];
		Response.Write("<script language=javascript");
		Response.Write("> window.location=('"+ Session["last_uri"] +"')\r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		
		return;
	}
	
	PrintAdminHeader();
	PrintAdminMenu();
	
	Response.Write("<br><h3><center>Dealer List</h3></center>");

	GetSearch();

	if(Request.Form["cmd"] == "Search Dealer" || Request.Form["txtSearch"] != null)
	{
		if(Request.Form["txtSearch"] != "" && Request.Form["txtSearch"] != null)
			m_search = Request.Form["txtSearch"].ToString();
	}
	if(!GetDealer())
		return;
	BindStockQty();
	LFooter.Text = m_sAdminFooter;
}

bool GetDealer()
{
	string sc = " SELECT * ";
	sc += " FROM card ";
	sc += " WHERE type = 2 "; //2 for dealer
	if(m_search != "")
	{
		if(TSIsDigit(m_search))
			sc += " AND id = "+ m_search;
		else
		{
			m_search = "%" + m_search + "%";
			sc += " AND name LIKE '" + m_search +"' OR company LIKE '"+ m_search +"' ";
			sc += " OR email LIKE '" + m_search +"' OR phone LIKE '"+ m_search +"' ";
			sc += " OR trading_name LIKE '" + m_search +"' OR fax LIKE '"+ m_search +"' ";
		}
	}
//DEBUG("sc=", sc);
	if(m_sort != "")
	{
		sc += " ORDER BY " + Request.QueryString["sort"];
		if(m_bDesc)
			sc += " DESC ";
	}
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "dealer");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}


void GetSearch()
{
	Response.Write("<script language=javascript>");
	Response.Write("<!-- hide from old browser");
	string s = @"
		function checkform()
		{
			//if(document.frmSearchProduct.txtSearch.value !='' || document.frmSearchProduct.txtSearchSN.value != ''){
			if(document.frmSearchProduct.txtSearch.value == '' && document.frmSearchProduct.txtSearchSN.value == ''){

				window.alert('Please Input Product Code or Serial number for search!! ');
				document.frmSearchProduct.txtSearch.focus();
				//document.frmSearchProduct.cmdUpdate.disabled=false;
				return false;
			}
			if(!IsNumberic(document.frmSearchProduct.txtSearch.value)){
				//window.alert('Please Enter Number Only!!');
				document.frmSearchProduct.txtSearch.focus();
				document.frmSearchProduct.txtSearch.select();
				return false;
			}
			return true;			
		}
		function queryitem()
		{
				if(document.frmSearchProduct.txtQuery.value !='')
					document.frmSearchProduct.cmdQuery.disabled=false;
				else
					document.frmSearchProduct.cmdQuery.disabled=true;
		}
		
		
		function IsNumberic(sText)
		{
		   var ValidChars = '0123456789';
		   var IsNumber=true;
		   var Char;
		   for (i = 0; i < sText.length && IsNumber == true; i++) 
		   { 
			  Char = sText.charAt(i); 
			  if (ValidChars.indexOf(Char) == -1) 
						 IsNumber = false;
		   }
		   return IsNumber;
   		 }
	";
	Response.Write("//-->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<form name=frmSearchProduct method=post action="+ Request.ServerVariables["URL"] +">");
	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<tr><td>Search Dealer:</td><td><input type=text name=txtSearch ></td>");
	
	//Response.Write("<td><input type=submit name=cmd value='Search Product' "+Session["button_style"] +" onclick='return checkform();'></td></tr>");
	Response.Write("<td><input type=submit name=cmd value='Search Dealer' "+Session["button_style"] +" ></td></tr>");
	
	//Response.Write("<tr><td>Branch :</td><td>");
	//PrintBranchNameOptionsWithOnChange();
	Response.Write("</td></tr>");

	Response.Write("\r\n<script");
	Response.Write(">\r\n document.frmSearchProduct.txtSearch.focus()\r\n</script");
	Response.Write(">\r\n ");
	
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	//Response.Write("</table>");
	Response.Write("</form>");
}

void BindStockQty()
{

	Response.Write("<form method=post name=frm>");
	string uri = "?cat=" + HttpUtility.UrlEncode(cat) + "&s_cat=" + HttpUtility.UrlEncode(s_cat) +"&ss_cat=" + HttpUtility.UrlEncode(ss_cat);
	Response.Write("<table align=center width=90% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<br><hr size=1 color=black>");
	
		//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	
	int rows = dst.Tables["dealer"].Rows.Count;
	if(rows == 0)
	{
		Response.Write("<script Language=javascript");
		Response.Write(">\r\n");
		Response.Write("window.alert('Nothing Found!!')\r\n");
		Response.Write("</script");
		Response.Write(">\r\n ");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"\">");
		return;
	}
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 20;
	
	m_cPI.URI = "?r="+ DateTime.Now.ToString("ddMMyyyyhhmmss") +"&sort="+ m_sort +"";
	if(!m_bDesc)
		m_cPI.URI +="&desc=1";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	
	Response.Write("<tr><td colspan=2>");
	Response.Write(sPageIndex);
	Response.Write("</td><td align=right colspan=5>");
	
	Response.Write("</td></tr>");
	
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th><a href=" + uri + "&sort=id");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by id' class=o>ID</a></th>");

	Response.Write("<th><a href=" + uri + "&sort=name");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by name' class=o>Name</a></th>");

	Response.Write("<th><a href=" + uri + "&sort=company");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by company' class=o>Company</a></th>");

	Response.Write("<th><a href=" + uri + "&sort=email");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by email' class=o>Email</a></th>");

	Response.Write("<th><a href=" + uri + "&sort=phone");
	if(!m_bDesc)
		Response.Write("&desc=1");
	Response.Write(" title='Click to sort by phone' class=o>Phone</a></th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>");
	
	bool bAlt = true;

	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["dealer"].Rows[i];
		string name = dr["name"].ToString();
		string company = dr["company"].ToString();
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string email = dr["email"].ToString();
		string phone = dr["phone"].ToString();
		//string name = dr["phone"].ToString();
		//string name = dr["phone"].ToString();

		Response.Write("<tr");
		if(alterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		alterColor = !alterColor;
		Response.Write("<td align=center><b>"+id+"</b></td></td>");
		Response.Write("<td>"+name+"</td>");
		Response.Write("<td align=center>");
		Response.Write("" + company + "</font></td>");
		Response.Write("<td>"+email+"</td>");
		Response.Write("<td>"+phone+"</td>");
		Response.Write("<td align=right><input type=button name=button value='Select This' "+ Session["button_style"] +"");
		Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?did="+ id +"&name="+ HttpUtility.UrlEncode(name) +"&cp="+ HttpUtility.UrlEncode(company) +"')\" ");
		Response.Write(" ></td>");
		
		Response.Write("</tr>");
	}
	
	Response.Write("</table>");
	Response.Write("</form>");
}

</script>

<asp:Label id=LFooter runat=server/>




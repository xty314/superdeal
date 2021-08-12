<!-- #include file="page_index.cs" -->

<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_branchID = "1";
bool m_bAdminLogin = false;
string tableWidth = "97%";
bool m_bReceivedAll = false;
string m_sPurchaseID = "";
string m_searchKW = "";
string m_sPurchaseLinkID = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	m_bAdminLogin = SecurityCheck("administrator", false); //no prompt
	
	m_branchID = Session["branch_id"].ToString();
	if(Request.QueryString["pid"] != null)
		m_sPurchaseID = Request.QueryString["pid"].ToString();
	if(m_bAdminLogin && Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
		m_branchID = Request.QueryString["branch"];

    if(m_sPurchaseID != "" && m_sPurchaseID != "all")
        GetPurchaseID();

	if(Request.Form["cmd"] == "Receive")
	{
		if(DoReceive())
		{
			PrintAdminHeader();
			Response.Write("<br><center><h5>Done. Please wait a second .. </h5>");
            if(m_sPurchaseLinkID != "")
                Response.Write("<meta meta http-equiv=\"refresh\" content=\"3; URL=purchase.aspx?n=" + m_sPurchaseLinkID + "\">");
            else
			    Response.Write("<meta meta http-equiv=\"refresh\" content=\"3; URL=dlist.aspx?branch=" + m_branchID + "\">");
			return;
		}
	}
	else if(Request.Form["cmd"] == "Receive All")
	{
		m_bReceivedAll = true;
		if(DoReceive())
		{
			PrintAdminHeader();
			Response.Write("<br><center><h5>Done. Please wait a second .. </h5>");
			Response.Write("<meta meta http-equiv=\"refresh\" content=\"3; URL=dlist.aspx?branch=" + m_branchID + "\">");
			return;
		}
	}
	if(Request.Form["cmd"] == "Search" || (Request.Form["search"] != null && Request.Form["search"] != ""))
		m_searchKW = Request.Form["search"];
	PrintAdminHeader();
	PrintAdminMenu();

	if(!DoSearch())//m_itype))
		return;

	if(!IsPostBack)
	{
		BindGrid();
	}
	PrintAdminFooter();
}

bool DoSearch()//int m_itype)
{
	string sword = " WHERE ";
	int rows = 0;
	string sc = " SELECT c.supplier_code, c.barcode, p.id AS pid, p.po_number, p.inv_number, d.*, pi.price AS cost, p.exchange_rate, p.freight FROM dispatch d ";
	sc += " JOIN purchase_item pi ON pi.kid = d.id ";
	sc += " JOIN purchase p ON p.id = pi.id ";
	sc += " JOIN code_relations c ON c.code = pi.code ";
	sc += " WHERE 1=1 ";
	if(m_sPurchaseID != "all" && m_sPurchaseID != "")
		sc += " AND p.po_number = " + m_sPurchaseID;
	sc += " AND d.received = 0 ";
	if(m_branchID != "all")
		sc += " AND d.branch = " + m_branchID;
	if(m_searchKW != null && m_searchKW != "")
	{
		if(TSIsDigit(m_searchKW))
		{
			if(m_searchKW.Length >= 0 && m_searchKW.Length <= 12)
			{
				sc += " AND (pi.code = "+ m_searchKW +" OR pi.supplier_code = '"+ m_searchKW +"' OR c.barcode = '"+ m_searchKW +"' ) ";
			}
			else
				sc += " AND (pi.supplier_code = '"+ m_searchKW +"' OR c.barcode = '" + m_searchKW +"' ) ";
		}
		else
		{
			sc += " AND (pi.supplier_code = '"+ m_searchKW +"' OR c.barcode = '" + m_searchKW +"' ) ";
		}
	}
    sc +=" AND p.status <> 4 ";
	sc += " ORDER BY p.po_number ";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "pr");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = ds.Tables["pr"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?branch=" + m_branchID;
	m_cPI.PageSize = 35;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	
	string sPageIndex = m_cPI.Print();

	int ncol = 8;
	
	Response.Write("<form name=f action=?branch=" + m_branchID + "");	
	if(m_sPurchaseID != null && m_sPurchaseID != "")
		Response.Write("&pid="+ m_sPurchaseID);
	Response.Write(" method=post>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Dispatch List</b><font color=red><b>");
	if(Session["branch_support"] != null)
	{
		if(m_bAdminLogin)
		{
			Response.Write(" &nbsp;&nbsp; <b>Branch : </b>");
			PrintBranchNameOptionsWithOnChange();	
		}
	}	
	Response.Write(" <b>&nbsp;&nbsp;&nbsp;Purchase# :</b> ");
	getPurchaseIDOption();
		Response.Write("</td><td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan='"+ ncol +"'>");
	///search function herer..
	Response.Write("<b>Search Code/M_NP/Barcode# :</b> ");
	Response.Write("<input type=text name=search ><input type=submit name=cmd value='Search' "+ Session["button_style"] +">");
	Response.Write("<input type=button value='Show All' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?branch="+ m_branchID +"&pid="+ m_sPurchaseID +"');\">");
	Response.Write("<br></td></tr>");

/*	Response.Write("<br><center><h3>Dispatch List</h3>");

	if(m_bAdminLogin)
	{
		Response.Write("<b>Branch : </b>");
		PrintBranchNameOptionsWithOnChange();	
	}


	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
*/
	Response.Write("<tr><td colspan='"+ ncol +"'>"+ sPageIndex +"</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th align=center width='10%'>PURCHASE#</th>\r\n");
	Response.Write("<th align=center width='10%'>SUPPLIER INV#</th>\r\n");
	Response.Write("<th align=center width='10%'>CODE</th>\r\n");
	Response.Write("<th align=center width='10%'>M_PN</th>\r\n");
	Response.Write("<th align=center width='10%'>BARCODE</th>\r\n");
	Response.Write("<th width='30%'>DESCRIPTION</th>\r\n");
	Response.Write("<th width='10%' align=right>ORDERED QTY</th>\r\n");
	Response.Write("<th width='10%' align=right>RECEIVED QTY</th>\r\n");
	Response.Write("<th width='10%' align=right></th>\r\n");
//	Response.Write("<th width='10%' align=right>&nbsp;</th>\r\n");
//	Response.Write("<th width=100>&nbsp;</th>\r\n");
	Response.Write("</tr>\r\n");

	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}
	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = ds.Tables["pr"].Rows[i];
		string kid = dr["kid"].ToString();
		string barcode = dr["barcode"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string pid = dr["pid"].ToString();
		string code = dr["code"].ToString();
		string ponumber = dr["po_number"].ToString();
		string supplier_inv = dr["inv_number"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;		
		Response.Write("<td align=center><a title='link to purchase' href='purchase.aspx?t=pp&n="+ pid +"' class=o target=_new>" + ponumber + "</a></td>");
		Response.Write("<td align=center>" + supplier_inv + "</td>");
		Response.Write("<td align=center>" + code + "</td>");
		Response.Write("<td align=center>" + supplier_code + "</td>");
		Response.Write("<td align=center>" + barcode + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center>" + qty + " ");		
		Response.Write("<td align=right>");//<a href=dlist.aspx?t=receive&id=" + kid + " class=o>Receive</a></td>");
		
		Response.Write("<input type=hidden name=qty" + kid + " value=" + qty + ">");	
		
		//Response.Write("<input type=text name=rq" + kid + " value='"+ qty +"' size=5 style=text-align:right onclick='this.select();'>");
		Response.Write("<input type=text name=rq" + kid + " value='"+ qty +"' size=5 style=text-align:right onclick='this.select();'>");
		
		Response.Write("</td>");
		Response.Write("<input type=hidden name=hdKID value="+ kid +">");
		Response.Write("<td><input type=checkbox name='chk"+ kid +"' checked onclick='fillData(eval(\"document.f.qty"+ kid +".value\"), "+ kid +" , this.checked);' ></td>");
		Response.Write("</tr>");
	}

	if(m_branchID != "all")
	{
	/*	Response.Write("<tr><td colspan='"+ (ncol + 1) +"' align=right><i>Receive All</i>");
		Response.Write("<input type=checkbox name=allbox onclick=\"CheckAll();\">");
		Response.Write("</td></tr>");
*/
		Response.Write("<tr><td colspan='"+ (ncol-1) +"'>" + sPageIndex + "</td><td colspan=2 align=right>");
//		Response.Write("<input type=submit name=cmd value='Receive All' onclick=\"if(!confirm('This will receive All Stock QTY')){return false;}\"  "+ Session["button_style"] +">");
		Response.Write("<input type=submit name=cmd value=Receive onclick=\"if(!confirm('Continue to receive stock...')){return false;}\" "+ Session["button_style"] +">");
		Response.Write("</td></tr>");
	}
//	Response.Write("<tr><td '"+ ncol +"'>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	
	PrintJavaFunctions();
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{		
		for (var i=0;i<document.f.elements.length;i++) 
		{			
			var e = document.f.elements[i];			
			if((e.name != 'allbox') && (e.type=='checkbox'))
			{
				e.checked = document.f.allbox.checked;			
				var ID = document.f.hdKID[i].value;
				var QTY = eval('document.f.qty'+ ID +'.value');
			//	window.alert('ID =' +ID +' qty =' + QTY);
				fillData(QTY,ID, e.checked);			
			}
			
		}
	}	
	function fillData(iQTY, ID, bClicked)
	{		
		if(bClicked)
			eval('document.f.rq'+ ID +'.value = iQTY');		
		else
			eval('document.f.rq'+ ID +'.value = 0');	
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
}

bool PrintBranchNameOptionsWithOnChange()
{
	DataSet dsBranch = new DataSet();
	int rows = 0;
	string sc = "SELECT id, name FROM branch WHERE 1=1 ";
	if(Session["branch_support"] != null)
	{
//	if(Session[m_sCompanyName + "AccessLevel"].ToString() != "10")
	if(!bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
		m_branchID = Session["branch_id"].ToString();
		if(m_branchID != "")
		{
			if(TSIsDigit(m_branchID))
				sc += " AND id ="+ m_branchID +" ";
		}
	}
	}
	sc += " AND activated = 1 ";
	sc += " ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsBranch, "branch");
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=branch");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?pid="+ m_sPurchaseID +"&branch=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
//if(Session[m_sCompanyName + "AccessLevel"].ToString() == "10")
	if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
{
	Response.Write("<option value='all'");
	Response.Write(">All Branches</option>");
}
	for(int i=0; i<rows; i++)
	{
		string name = dsBranch.Tables["branch"].Rows[i]["name"].ToString();
		string id = dsBranch.Tables["branch"].Rows[i]["id"].ToString();
		Response.Write("<option value='" + id + "' ");
		if(id == m_branchID)
			Response.Write("selected");
		Response.Write(">" + name + "</option>");
	}

	if(rows == 0)
		Response.Write("<option value=1>Branch 1</option>");
	Response.Write("</select>");
	return true;
}

bool DoReceive()
{
	if(!DoSearch())
		return false;

	string sc = "";
	for(int i=0; i<ds.Tables["pr"].Rows.Count; i++)
	{
		DataRow dr = ds.Tables["pr"].Rows[i];
		string branch = dr["branch"].ToString();
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string kid = dr["kid"].ToString();
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
		string ponumber = dr["po_number"].ToString();
		string cost = dr["cost"].ToString();
		string exchange_rate = dr["exchange_rate"].ToString();
		string freight = dr["freight"].ToString();	
		int rq = MyIntParse(Request.Form["rq" + kid]);
		//check box for indetify which to recevied stock
		string chkBox = Request.Form["chk" + kid];
		
		if(m_bReceivedAll)
			rq = MyIntParse(qty);
		int nq = MyIntParse(qty);
		if(rq > nq)
			rq = nq;
//DEBUG("qty =", qty);
		if(rq == 0)
			continue;

		int remain_qty = nq - rq;
if(chkBox == "on")
{
		sc += " BEGIN TRANSACTION ";
		if(remain_qty != 0) //add a new record as remain qty for further receiving
		{
			sc += " INSERT INTO dispatch (branch, id, code, name, qty, record_date, received) VALUES(" + branch;
			sc += ", " + id + ", " + code + ", '" + EncodeQuote(name) + "', " + remain_qty + ", GETDATE(), 0) ";
		}
		sc += " UPDATE dispatch SET qty = " + rq + ", received=1, date_received = GETDATE() ";
		sc += ", staff_received = " + Session["card_id"].ToString() + " WHERE kid = " + kid + " AND code ="+code;


		if(nq > 0 && rq > 0)//update average cost only on positive qty
		{
			double dAverageCost = ((MyDoubleParse(cost) / MyDoubleParse(exchange_rate)) * rq) + MyDoubleParse(freight);
			sc += " IF((SELECT SUM(qty) FROM stock_qty WHERE code = "+ code +" ) <= 0 )";
			sc += " BEGIN ";
			//sc += " UPDATE code_relations SET average_cost = "+ dAverageCost +"";		
			sc += " UPDATE code_relations SET average_cost ="+ cost +" ";// Update Average cost equal purchase price if the qty less then zero 14/02/2008
			sc += " WHERE code = "+ code +"";
			sc += " END ";
			sc += " ELSE IF ((SELECT ISNULL(SUM(qty),0) + "+ rq +" FROM stock_qty WHERE code = "+ code +") >= 1 )";	
			sc += " BEGIN ";
			sc += " UPDATE code_relations SET ";
			sc += " average_cost = ((average_cost * ISNULL((SELECT SUM(qty) FROM stock_qty WHERE code = "+ code + "), 0) + ("+ dAverageCost +"))"; //("+ cost +" * "+ qty +")) ";
			sc += " / (ISNULL((SELECT SUM(qty) FROM stock_qty WHERE code = "+ code + " ), 0) + "+ rq +"))";
			sc += " WHERE code = "+ code +"";
			sc += " UPDATE stock_qty SET supplier_price = "+ cost +", average_cost = ((average_cost * ISNULL((SELECT SUM(qty) FROM stock_qty WHERE code = "+ code + "), 0) + ("+ dAverageCost +"))"; //("+ cost +" * "+ qty +"))
		    sc += " / (ISNULL((SELECT SUM(qty) FROM stock_qty WHERE code = "+ code + " ), 0) + "+ rq +"))  WHERE code = "+ code +" AND branch_id = "+ m_branchID +"";
			sc += " END ";
			sc += " ELSE ";
			sc += " BEGIN ";
			sc += " UPDATE code_relations SET ";
			sc += " average_cost = "+ cost +" ";		
			sc += " WHERE code = "+ code +"";
			sc += " UPDATE stock_qty SET supplier_price = "+ dAverageCost +", average_cost = "+ dAverageCost +""; //"+ cost +" ";
			sc += " WHERE code = "+ code +" AND branch_id = "+ m_branchID +"";
			sc += " END ";
		}
		//sc += " IF (SELECT qty FROM dispatch WHERE code = "+ code +") = 0 ";
		//sc += " UPDATE dispatch SET date_received = GETDATE(), staff_received = "+ Session["card_id"].ToString() +" WHERE qty = 0 AND code = "+ code +"";
		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code = " + code;
		sc += " AND branch_id = " + m_branchID + ")";
		sc += " INSERT INTO stock_qty (qty, code, branch_id) ";
		sc += " VALUES ('";
		sc += rq + "', ";
		sc += code + ", ";
		sc += m_branchID + ") ";
		sc += " ELSE UPDATE stock_qty SET qty = qty + " + rq;		
		sc += " WHERE code = " + code + " AND branch_id = " + m_branchID;
		//add to qty to product table also (this is obsolete, but keep it here. DW 14.05.2007)
		sc += " UPDATE product SET stock = stock + " + rq;
		sc += " WHERE code = "+ code +"";
		sc += " UPDATE purchase SET date_received = GETDATE() ";
		if(Session["branch_support"] == null)
		{
			if(remain_qty != 0 )
			 sc += ", status = 7 ";
			else if(rq  == 0)
			 sc += ", status = 6 ";
			else
			 sc += ", status =2 ";
		}
		else
		{
		  if(remain_qty != 0 )
			 sc += ", status = 7 ";
			else if(rq  == 0)
			 sc += ", status = 6 ";
			else
			 sc += ", status =2 ";
		 }
		
		sc += " WHERE po_number ="+ponumber;
		sc += " COMMIT ";
		
	}
}
//DEBUG("sc =", sc);
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

bool getPurchaseIDOption()
{
	DataSet dsPurchase = new DataSet();
	int rows = 0;
	string sc = " SELECT DISTINCT p.po_number ";
	sc += " FROM dispatch d ";
	sc += " JOIN purchase_item pi ON pi.kid = d.id ";
	sc += " JOIN purchase p ON p.id = pi.id ";	
	sc += " JOIN code_relations c ON c.code = pi.code ";
	sc += " WHERE d.received = 0 ";
	sc += " AND p.status  != 4 ";
	
	if(m_branchID != "all" )
		sc += " AND d.branch = " + m_branchID;
	sc += " ORDER BY p.po_number ";
//DEBUG("sc =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsPurchase, "purchaseList");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	Response.Write("<select name=purchaseID");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?branch="+ m_branchID +"&pid=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");

	Response.Write("<option value='all'");
	Response.Write(">All Purchase Orders</option>");
	for(int i=0; i<rows; i++)
	{
		string po_number = dsPurchase.Tables["purchaseList"].Rows[i]["po_number"].ToString();
	//	string id = dsPurchase.Tables["purchaseList"].Rows[i]["id"].ToString();
		Response.Write("<option value='" + po_number + "' ");
		if(po_number == m_sPurchaseID)
			Response.Write("selected");
		Response.Write(">" + po_number + "</option>");
	}	
	Response.Write("</select>");
	return true;
}

bool GetPurchaseID()
{
   
    if(ds.Tables["linkID"] != null)
		ds.Tables["linkID"].Clear();
    string sc = " SELECT id FROM purchase WHERE po_number = '" + m_sPurchaseID + "'";
    try
    {
        myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "linkID") == 1)
            m_sPurchaseLinkID = ds.Tables["linkID"].Rows[0]["id"].ToString();
    }
    catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
    return true;
}
</script>

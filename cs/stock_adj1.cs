<!-- #include file="fifo_f.cs" -->

<script runat=server>
DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table

//
int m_page = 1;
int m_nPageSize = 30;
int m_nQtyReturn = 0;
int m_nIndexCount = 10;

bool b_Allbranches = false;

string cat = "";
string s_cat = "";
string ss_cat = "";
string m_branchid = "1";		//branch id, 21/03/03 herman
string m_id = "";
string m_qty = "";  //1=bigger than 0, 2=less than 0 and 0=0
string tableWidth = "97%";

bool m_bBarcode = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(Request.QueryString["t"] == "transfer")
	{
		if(Request.Form["cmd"] == Lang("Transfer"))
		{
			if(DoStockTransfer())
			{
				PrintAdminHeader();
				Response.Write("<br><center><h4>" + Lang("Stock transfered, please wait a second") + "</h4>");
				Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=stock_adj.aspx?t=transfer&code=" + Request.QueryString["code"] + "\">");
				return;
			}
		}
		PrintTransferForm();
		return;
	}

	if(Session["branch_support"] != null)
	{
		if(Request.QueryString["b"] != null && Request.QueryString["b"] !="")
		{
			m_branchid = Request.QueryString["b"];
			if(m_branchid == "all")
				b_Allbranches = true;
		}
		else if(Session["branch_id"] != null)
		{
			m_branchid = Session["branch_id"].ToString();
		}
	}
	string bar = GetSiteSettings("use_barcode", "false", true);
	
	if(MyBooleanParse(bar))
		m_bBarcode = MyBooleanParse(bar);
	else
		m_bBarcode = false;
	
	if(Request.QueryString["page"] != null)
	{
		if(IsInteger(Request.QueryString["page"]))
			m_page = int.Parse(Request.QueryString["page"]);
	}

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];

	if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "")
		m_qty = Request.QueryString["qty"];
	
	Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);

	if(Request.QueryString["allocode"] != null && Request.QueryString["allocode"] != "")
	{
		string code = Request.QueryString["allocode"].ToString();
		if(!doCleanAlloCode(code))
			return;
	}

	if(Request.Form["cmd"] == Lang("Update Adjustment") || Request.Form["txtQty"] != null)
	{
		if(DoUpdate())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stock_adj.aspx");
			Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
			Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
			Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
			Response.Write("&page=" + m_page + " \">");
			if(m_qty != "")
				Response.Write("&qty="+ m_qty +"");
			return;
		}
	}
	
	PrintAdminHeader();
	PrintAdminMenu();

	if(!GetStockQty())
		return;
		
	BindStockQty();	
	LFooter.Text = m_sAdminFooter;
}

// clean allocated stock in product table by set to 0 : tee?8-4-3
// need recalculate each branch's allocated (in stock_qty table). darcy 08-03-2004
bool doCleanAlloCode(string code)
{
	double allocated = 0;
	string sc = "";

	//update allocated total
	//branches
	int nBranch = 1;
	sc = " SELECT id FROM branch ORDER BY id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		nBranch = myCommand.Fill(dst, "nbranch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	double dTotalAllocated = 0;
	for(int i=0; i<nBranch; i++)
	{
		string branchid = dst.Tables["nbranch"].Rows[i]["id"].ToString();

		allocated = 0;
		if(dst.Tables["get_allocated"] != null)
			dst.Tables["get_allocated"].Clear();

		string s = " SELECT SUM(i.quantity) AS allocated ";
		s += " FROM order_item i JOIN orders o ON o.id = i.id ";
		s += " WHERE i.code = " + code;
		s += " AND o.status IN(1, 4, 5) "; 
		s += " AND o.branch = " + m_branchid;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(s, myConnection);
			myCommand.Fill(dst, "get_allocated");
			string ret = dst.Tables["get_allocated"].Rows[0]["allocated"].ToString();
			if(ret != "")
				allocated = MyDoubleParse(ret);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		dTotalAllocated += allocated;
		
		//stock_qty for each branch's allocated and real stock
		sc += " UPDATE stock_qty ";
		sc += " SET allocated_stock = " + allocated;
		sc += " WHERE code = " + code + " AND branch_id = " + m_branchid + " ";
	}

	//allocated total in product table
	sc += " UPDATE product ";
	sc += " SET allocated_stock = " + dTotalAllocated;
	sc += " WHERE code = "+ code +" ";

	if(sc != "")
	{
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

bool DoUpdate()
{
	if(Request.Form["branch"] != null)
		m_branchid = Request.Form["branch"];
	if(m_branchid == null || m_branchid == "" || m_branchid == "all")
		m_branchid = "1";

	int i = (m_page-1) * m_nPageSize;
	string code = Request.Form["code"+i];
	
	while(code != null)
	{
		if(!UpdateOneRow(i.ToString()))
			return false;
		i++;
		code = Request.Form["code"+i];
	}
	return true;
}

Boolean UpdateOneRow(string sRow)
{
	Boolean bRet = true;

	string code		= Request.Form["code"+sRow];
	string qty_new = Request.Form["txtQty"+sRow];
	string qty_old = Request.Form["qty_old"+sRow];
	string branch = Request.Form["branch_id"+sRow];
	string location = Request.Form["stock_location"+sRow];
	if(branch == null || branch == "")
		branch = "1";
	if(qty_new == null || qty_new == "")
		qty_new = "0";
	

	string note = Request.Form["note"];

	int adj = MyIntParse(qty_new) - MyIntParse(qty_old);
	string sc = "BEGIN TRANSACTION ";
	if(qty_new != qty_old)
	{
		sc += " INSERT INTO stock_adj_log (staff, code, qty, branch_id, note) VALUES(" + Session["card_id"].ToString();
		sc += ", " + code + ", " + adj + ", " + branch + ", '" + EncodeQuote(note) + "') "; 
		sc += " IF NOT EXISTS(SELECT * FROM stock_qty WHERE code=" + code + " AND branch_id = " + branch + ") ";
		sc += " INSERT INTO stock_qty (code, qty, branch_id) VALUES(" + code + ", " + qty_new + ", " + branch + ") ";
		sc += " ELSE ";
		sc += " UPDATE stock_qty SET qty=" + qty_new + " WHERE code=" + code + " AND branch_id = " + branch;
		sc += " UPDATE code_relations SET stock_location ='"+ location+"' WHERE code ='"+code+"'";
		//
		//
	
	}
	else
		sc += " UPDATE code_relations SET stock_location ='"+ location+"' WHERE code ='"+ code+"'";

	sc += " SELECT IDENT_CURRENT('stock_adj_log') AS id ";
	sc += " COMMIT ";
	if(sc == "")
		return true;
	DataSet dsid = new DataSet();
	string new_id = "";

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsid, "id") != 1)
		{
			Response.Write("<br><center><h3>Error getting new record ID");
			return false;
		}
		new_id = dsid.Tables["id"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(qty_new == qty_old)// && allocated == allocated_old)
		return true;
	fifo_RecordStockLoss(code, m_branchid, (0 - adj), new_id);
	return true;
}

bool GetStockQty()
{
	string kw = "";
	if(Request.Form["kw"] != null)
	{
		kw = Request.Form["kw"];
		Session["stock_adj_search"] = kw;
	}
	bool bIsInt = false;
	try
	{
		string sTemp = int.Parse(kw).ToString();
		bIsInt = true;
	}
	catch(Exception e)
	{
	}
	kw = EncodeQuote(kw);

	bool bWhereAdded = false;
	string sc = "SELECT DISTINCT c.barcode, p.code, p.supplier, p.supplier_code, p.name, c.stock_location ";
	sc += ", sq.qty AS stock, sq.allocated_stock, sq.branch_id ";
	sc += ", l.code AS adjusted ";
if(Session["branch_support"] != null)
	sc += " , b.name AS branch_name ";
else 
	sc += " , '' AS branch_name ";
	sc += " FROM product p JOIN code_relations c ON c.code = p.code ";
	sc += " LEFT OUTER JOIN barcode ba ON ba.item_code = c.code";
	sc += " LEFT OUTER JOIN stock_adj_log l ON l.code=p.code ";
	sc += " LEFT OUTER JOIN stock_qty sq ON p.code=sq.code";
	if(Session["branch_support"] != null)
		sc += " JOIN branch b ON b.id = sq.branch_id AND b.activated =1 ";
	
	sc += " WHERE 1=1 ";
	if(cat != "" && cat != "all")
		sc += " AND p.cat = N'"+ cat +"' ";
	if(s_cat != "" && s_cat != "all")
		sc += " AND p.s_cat = N'"+ s_cat +"' ";
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND p.ss_cat = N'"+ ss_cat +"' ";
	if(kw != "")
	{		
		//if(TSIsDigit(kw))
		if(bIsInt)
		{
			sc += " AND (c.barcode LIKE N'%" + kw + "%' OR c.supplier_code LIKE N'%"+ kw +"%'";
			sc += " OR ba.barcode = '" + kw + "'";
			if(kw.Length < 9)
				 sc += " OR c.code LIKE N'%"+ kw +"%' ";
			sc += " ) ";
		}
		else
			sc += " AND (c.supplier_code LIKE N'%"+ kw +"%' OR c.barcode LIKE N'%" + kw + "%' OR ba.barcode LIKE '%"+ kw + "%' OR c.name LIKE '%" + kw+"%')";
	}

	if(Session["branch_support"] != null)
	{
		if(!b_Allbranches)
		{
			sc += " AND sq.branch_id=" + m_branchid;
		}
	}
//	else
//		sc += " AND sq.branch_id = 1 ";
	if(m_qty != "")
	{		
		if(TSIsDigit(m_qty))
		{
			sc += " AND ";
			if(g_bRetailVersion)
			{
				if(m_qty == "0")
					sc += " sq.qty = 0 ";
				if(m_qty == "1")
					sc += " sq.qty > 0 ";
				if(m_qty == "2")
					sc += " sq.qty < 0 ";				
			}
			else
			{
				if(m_qty == "0")
					sc += " p.stock = 0 ";
				if(m_qty == "1")
					sc += " p.stock > 0 ";
				if(m_qty == "2")
					sc += " p.stock < 0 ";				
			}
		}
	}
	sc += " ORDER BY c.barcode ";	
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nQtyReturn = myAdapter.Fill(dst, "stock_qty");

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
				myCommand.CommandTimeout = 60*60*60;
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
	return true;

}

void BindStockQty()
{
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	const string jav = @"
	function checkform() 
	{	
		if(document.frmQtyAdjust.txtQty.value==''){
			window.alert('Please enter Invoice Number');
			document.frmQtyAdjust.txtQty.focus();
			return false;
		}
		
		if (!IsNumberic(document.frmQtyAdjust.txtQty.value)) { 
			  window.alert('Please enter only numbers in the invoice number field'); 
			  document.frmQtyAdjust.txtQty.focus();
			  return false; 
		}

	}	
	
	function IsNumberic(sText)
	{
	   var ValidChars = '0123456789-';
	   var IsNumber=true;
	   var Char;
 	   for (i = 0; i < sText.length && IsNumber == true; i++) 
	   { 
		  Char = sText.charAt(i); 
		  if (ValidChars.indexOf(Char) == -1) 
		  {
			 IsNumber = false;
		  }
	   }
		   return IsNumber;
	}

	";
	Response.Write("--> "); 
	Response.Write(jav);
	Response.Write("</script");
	Response.Write("> ");
	Response.Write("<form name=frmQtyAdjust method=post action='stock_adj.aspx?update=success");
	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
		Response.Write("&b="+ Request.QueryString["b"].ToString());
	
	//Response.Write("&b=all");
	if(cat != "")
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	if(s_cat != "")
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	if(ss_cat != "")
		Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	if(m_qty != "")
		Response.Write("&qty="+ m_qty +"");
	Response.Write("&page=" + m_page);
	Response.Write("' >");
	
	Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>" + Lang("Stock Taking and Adjustment") + "</font></font>");
	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
	
	Response.Write("<table border=1 align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=11><br></td></tr>");

	//Response.Write("<center><h4>" + Lang("Stock Taking and Adjustment") + "</h4>");
	//Response.Write("<table width=98% cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	//Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><td colspan=11><table border=0 cellpadding=0 cellspacing=0 width=100%><tr><td>");
	Response.Write("<b>" + Lang("Barcode/Code/MNP Search") + " : </b></td><td><input type=text name=kw value='" + Session["stock_adj_search"] + "'>");
	Response.Write("<input type=submit name=cmd value='" + Lang("Search") + "'  "+ Session["button_style"] +">");
	Response.Write("</td></tr>");
	
	
	//branch option
	Response.Write("<tr>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<td><b>" + Lang("Branch") + " :</b></td><td align=left>");
		PrintBranchNameOptionsWithOnChange();
		if(Session["branch_support"] != null)
			Response.Write(" <input type=button  value='" + Lang("Print Product List") + "'  "+ Session["button_style"] +" onclick=\"window.open('stocktake.aspx?pr=3&br=" + m_branchid + "')\"> ");
		Response.Write("</td>");
	}
	else
		Response.Write("<td colspan=2>&nbsp;</td>");
	
	Response.Write("<td align=right colspan=5>");
	string suri = Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"";
	if(cat != "")
		suri += "&cat=" + HttpUtility.UrlEncode(cat) +"";
	if(s_cat != "")
		suri += "&scat=" + HttpUtility.UrlEncode(s_cat) +"";
	if(ss_cat != "")
		suri += "&sscat=" + HttpUtility.UrlEncode(ss_cat) +"";
	Response.Write(Lang("Filter With") + ": <input type=button title=' Filter with QTY Less than 0 ' value=' < ' "+ Session["button_style"] +" onclick=\"window.location=('"+ suri +"&qty=2");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write("');\" >");
	Response.Write("<input type=button title=' Filter with QTY = 0 ' value=' 0 ' "+ Session["button_style"] +" onclick=\"window.location=('"+ suri +"&qty=0");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write("');\" >");
	Response.Write("<input type=button title=' Filter with QTY Greater than 0 ' value=' > ' "+ Session["button_style"] +" onclick=\"window.location=('"+ suri +"&qty=1");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write("');\" >");	
	Response.Write("<input type=button title=' Show ALL QTY ' value=' All ' "+ Session["button_style"] +" onclick=\"window.location=('"+ suri +"");
	if(m_branchid != "")
		Response.Write("&b="+ m_branchid +"");
	Response.Write("');\" >");
	Response.Write("&nbsp;&nbsp;|&nbsp;&nbsp;");

	if(!DoItemOption())
		return;
	Response.Write("</tr>");

	Response.Write("</table></td></tr>");

	Response.Write("<tr bgcolor=#8BB7DD>");
	Response.Write("<th align=center>" + Lang("Code") + "#</th>");
	Response.Write("<th align=center>" + Lang("Barcode") + "#</th>");
	Response.Write("<th align=center nowrap>" + Lang("M_PN/Stock Trace") + "</th>");
	Response.Write("<th align=left>" + Lang("Item Description") + "</th>");
	if(Session["branch_support"] != null)
	{
		Response.Write("<th>" + Lang("Branch") + "</th>");
	}
	else
		Response.Write("<th>&nbsp;</th>");
	Response.Write("<th colspan=2>" + Lang("QTY") + "</th>");
	Response.Write("<th align=right>" + Lang("NEW QTY") + "</th> "); //<td align=center>Adjustment</td></tr>");
	Response.Write("<th>" + Lang("Location") + "</th>");
	//if(m_bBarcode)
	//	Response.Write("<th>" + Lang("Print Barcode") + "</th>");
	Response.Write("</tr>");

	bool bAlt = true;
	string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (m_page-1) * m_nPageSize;
	for(int i=startPage; i<dst.Tables["stock_qty"].Rows.Count; i++)
	{
		//Response.Write("<form name=frmQtyAdjust method=post action='stock_adj.aspx?update=success'>");
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["stock_qty"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
			break;
	}
		
	PrintPageIndex();
	
	Response.Write("</table>");
	Response.Write("</form></center>");
}

bool PrintBranchNameOptionsWithOnChange()	//Herman: 21/03/03
{
	DataSet dsBranch = new DataSet();
	string sBranchID = "1";
	int rows = 0;
	if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
	{
		sBranchID = Request.QueryString["b"];
		if(sBranchID != "all")
			Session["branch_id"] = MyIntParse(sBranchID); //Session["branch_id"] is integer
	}
	else if(Session["branch_id"] != null)
	{
		sBranchID = Session["branch_id"].ToString();
	}

	//do search
	string sc = "SELECT id, name FROM branch WHERE activated = 1 ORDER BY id";
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
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate());
	if(cat != "")
		Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
	if(s_cat != "")
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
	if(ss_cat != "")
		Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
	if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "" && TSIsDigit(Request.QueryString["qty"].ToString()))
		Response.Write("&qty="+ Request.QueryString["qty"].ToString());
//	if(Request.QueryString["page"] != null && Request.QueryString["page"] != "" && TSIsDigit(Request.QueryString["page"].ToString()))
//		Response.Write("&page="+ Request.QueryString["page"].ToString());
	Response.Write("&b=' + this.options[this.selectedIndex].value)\"");

	Response.Write(">");
	for(int i=0; i<rows; i++)
	{
		string bname = dsBranch.Tables["branch"].Rows[i]["name"].ToString();
		int bid = int.Parse(dsBranch.Tables["branch"].Rows[i]["id"].ToString());
		Response.Write("<option value='" + bid + "' ");
		if(IsInteger(sBranchID))
		{
			if(bid == int.Parse(sBranchID))
				Response.Write("selected");
		}
		Response.Write(">" + bname + "</option>");
	}
	Response.Write("<option value='all'");
	if(!IsInteger(m_branchid))
	{
		Response.Write("selected");
		b_Allbranches = true;	
	}
	Response.Write(">" + Lang("All Branches") + "</option>");

	if(rows == 0)
		Response.Write("<option value=1>" + Lang("Main Branch") + "</option>");
	Response.Write("</select>");
	return true;
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT cat FROM product ";
	sc += " ORDER BY cat";
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
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?r="+ DateTime.Now.ToOADate() +"");
	Response.Write("&b="+ m_branchid);
	if(m_qty != "")
		Response.Write("&qty="+ m_qty +"");
	Response.Write("&cat=' + escape(this.options[this.selectedIndex].value))\"");
	
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
		else
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
	
		sc = "SELECT DISTINCT s_cat FROM product ";
		sc += " WHERE cat = N'" + cat + "' ";
		sc += " ORDER BY s_cat";
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
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&b="+ m_branchid);
		if(m_qty != "")
		Response.Write("&qty="+ m_qty +"");
		Response.Write("&scat=' + escape(this.options[this.selectedIndex].value))\"");
		Response.Write(">");
		Response.Write("<option value='all'>" + Lang("Show All") + "</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
			
		}

		Response.Write("</select>");
	}
	
	if(s_cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM product ";
		sc += " WHERE cat = N'" + cat + "' ";
		sc += " AND s_cat = N'" + s_cat + "' ";
		sc += " ORDER BY ss_cat";
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
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&b="+ m_branchid);
		if(m_qty != "")
		Response.Write("&qty="+ m_qty +"");
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + escape(this.options[this.selectedIndex].value)) \"");
		
		Response.Write(">");
		Response.Write("<option value='all'>" + Lang("Show All") + "</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
		}
		Response.Write("</select>");
	}
	return true;
}

bool DrawRow(DataRow dr, int i, bool alterColor)
{
	string adjusted = dr["adjusted"].ToString(); //if not blank then this product has been adjusted stock
	
	string s_qty = dr["stock"].ToString();
	string code = dr["code"].ToString();
	string barcode = dr["barcode"].ToString();
	string supplier = dr["supplier"].ToString();
	string supplier_code = dr["supplier_code"].ToString();
	string s_productdesc = dr["name"].ToString();
	string location = dr["stock_location"].ToString();
	string swap = "";
	if(s_productdesc.Length >=60)
	{
		for(int j=0; j<60; j++)
			swap += s_productdesc[j].ToString();
	}
	else
		swap = s_productdesc;

	s_productdesc = swap;

	Response.Write("<tr");
	if(alterColor)
		Response.Write(" bgcolor=#EEEEEE");
	Response.Write(">");

	Response.Write("<td align= nowrap>");
	Response.Write("<table border=0><tr><td width=80%><a title='" + Lang("click here to view Sales Ref") + ":' href='salesref.aspx?code=" + code +"' class=o target=_new>");
	Response.Write(code);
	Response.Write("</a> </td><td>");
	
	Response.Write("<input type=button title='" + Lang("View Sales History") + "' onclick=\"javascript:viewsales_window=window.open('viewsales.aspx?");
	Response.Write("code=" + code + "','','width=500,height=500');\" value='S' " + Session["button_style"] + ">");
	Response.Write("</td>\r\n");
	Response.Write("</tr></table></td>");
	Response.Write("<td align= nowrap>");
	Response.Write(barcode);
	Response.Write("</td>");
	Response.Write("<td align=center><b>" + supplier_code + "</b></td>");
	Response.Write("<td> "+ StripHTMLtags(s_productdesc) +"</td>");
	
	if(Session["branch_support"] != null)
	{
		string s_branchName = dr["branch_name"].ToString();
		Response.Write("<td align=center>" + s_branchName + "</td>");	
		Response.Write("<input type=hidden name=branch_id" + i + " value='" + dr["branch_id"].ToString() + "'>");
	}
	else
		Response.Write("<td>&nbsp;</td>");
	Response.Write("<input type=hidden name=code" + i + " value='" + code + "'>");
	Response.Write("<td nowrap>");
	Response.Write("<b><font color=");
	if(MyDoubleParse(s_qty) == 0)
		Response.Write("Green>");
	else if(MyDoubleParse(s_qty) < 0)
		Response.Write("Red>");
	else if(MyDoubleParse(s_qty) > 0)
		Response.Write("Black>");

	Response.Write(s_qty);
	Response.Write(" </td><td><a href=stocktrace.aspx?p=0&c=" + code + " class=o>" + Lang("Trace") + "</a> ");
	Response.Write("<input type=button title='" + Lang("View Purchase & Sales Total") + "' ");
	Response.Write(" onclick=\"javascript:viewsales_window=window.open('stock_ana.aspx?");
	Response.Write("c=" + code + "','','width=500,height=300');\" value='A' " + Session["button_style"] + ">");
	Response.Write("<input type=button value=T title='" + Lang("Transfer") + "' onclick=window.location=('stock_adj.aspx?t=transfer&code=" + code + "')  "+ Session["button_style"] +">");
	
	if(adjusted != "")
	{
		Response.Write(" <a href=vd.aspx?t=adjlog&code=" + code + " class=o target=_blank>" + Lang("Adj. Log") + "</a>");
	}
	Response.Write("</font></b></td>");
	Response.Write("<td align=right>");
	Response.Write("<input type=text name=txtQty" + i + " size=5 style=text-align:right; value='"+s_qty+"' ></td>");
	Response.Write("<input type=hidden name=qty_old" + i + " value='"+s_qty+"' >");

/*	if(m_bBarcode)
	{
		Response.Write("<th>");
		int nQty = int.Parse(GetSiteSettings("barcode_qty", "30"));

		Response.Write("<select name=qty"+ i +">");
		for(int j=1; j<=nQty; j++)
			Response.Write("<option value="+ j +">"+ j +"</option>");
		Response.Write("</select>" );
		string bc_uri = "barcode.aspx?code="+ code +"&qty="; //document.frmQtyAdjust.qty"+ i +".value;		
		Response.Write("<input type=button title='" + Lang("Print Barcode") + "' value='BC'  "+ Session["button_style"] +" onclick=\"window.location=('"+ bc_uri +"'+ document.frmQtyAdjust.qty"+ i +".value);\">");
		Response.Write("</th>");
	}
	*/
	Response.Write("<td align=center>" + EditItemLocation(code, i) +"</td>");
	Response.Write("</tr>");
	return true;
}

void PrintPageIndex()
{
	Response.Write("<tr><td colspan=2>Page: ");
	int pages = dst.Tables["stock_qty"].Rows.Count / m_nPageSize + 1;
	int start = 1; 
	if(m_page >= 2)
		start = m_page - 1;
	int end = pages;
	if(end - start > m_nIndexCount)
		end = start + m_nIndexCount;
	int i = start;
	if(i > 2)
	{
		Response.Write("<a href=stock_adj.aspx?page=" + (start - 1).ToString());		
		if(cat != "")
			Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		if(s_cat != "")
			Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		if(ss_cat != "")
			Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
		if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "" && TSIsDigit(Request.QueryString["qty"].ToString()))
			Response.Write("&qty="+ Request.QueryString["qty"].ToString());
		if(Request.QueryString["b"] != null && Request.QueryString["b"] != "" )
			Response.Write("&b="+ Request.QueryString["b"].ToString());
		Response.Write(">...</a> ");
	}

	for(; i<=end; i++)
	{
		if(i != m_page)
		{
			Response.Write("<a href=stock_adj.aspx?page=");
			Response.Write(i.ToString());
			
			if(cat != "")
				Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
			if(s_cat != "")
				Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
			if(ss_cat != "")
				Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
			if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "" && TSIsDigit(Request.QueryString["qty"].ToString()))
				Response.Write("&qty="+ Request.QueryString["qty"].ToString());
			if(Request.QueryString["b"] != null && Request.QueryString["b"] != "")
			Response.Write("&b="+ Request.QueryString["b"].ToString());
			Response.Write(">");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	if(end < pages)
	{
		Response.Write("<a href=stock_adj.aspx?page=" + i.ToString());		
		if(cat != "")
			Response.Write("&cat=" + HttpUtility.UrlEncode(cat));
		if(s_cat != "")
			Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		if(ss_cat != "")
			Response.Write("&sscat=" + HttpUtility.UrlEncode(ss_cat));
		if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "" && TSIsDigit(Request.QueryString["qty"].ToString()))
			Response.Write("&qty="+ Request.QueryString["qty"].ToString());
		if(Request.QueryString["b"] != null && Request.QueryString["b"] != "" )
			Response.Write("&b="+ Request.QueryString["b"].ToString());
		Response.Write(">...</a>");
	}

	Response.Write("</td>");
	Response.Write("<td colspan=7 align=right>");
	Response.Write("<b>" + Lang("Adjustment Note") + " : </b><input type=text name=note> ");
	Response.Write("<input type=submit " + Session["button_style"] + " name=cmd value='" + Lang("Update Adjustment") + "' >");
	Response.Write("</td>");
	Response.Write("</tr>");
}

bool PrintTransferForm()
{
	PrintAdminHeader();
	PrintAdminMenu();

	string code = Request.QueryString["code"];
	if(code == null || code == "")
	{
		MsgDie("Error, No Item Code");
		return false;
	}

	//get qty
	int rows = 0;
	string sc = " SELECT q.code, q.branch_id, q.qty, c.name, b.name AS branch_name, c.barcode ";
	sc += " FROM stock_qty q JOIN code_relations c ON c.code = q.code ";
	sc += " LEFT OUTER JOIN branch b ON b.id = q.branch_id ";
	sc += " AND activated = 1 ";
	sc += " WHERE q.code = " + code;
	sc += " ORDER BY q.branch_id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "transfer");
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

	Response.Write("<br><center><h3>" + Lang("Stock Transfer") + "</h3>");

	if(rows <= 0)
	{

		MsgDie("No stock found in any branch, please do purchase first.");
		return true;
	}

	string barcode = dst.Tables["transfer"].Rows[0]["barcode"].ToString();

	Response.Write("<form name=f action=stock_adj.aspx?t=transfer&code=" + code + " method=post>");
	Response.Write("<table cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	string name = dst.Tables["transfer"].Rows[0]["name"].ToString();
	Response.Write("<tr><td colspan=2><b>" + Lang("Code") + " : </b>" + code + "</b></td></tr>");
	Response.Write("<tr><td colspan=2><b>" + Lang("Barcode") + " : </b>" + barcode + "</b></td></tr>");
	Response.Write("<tr><td colspan=2><b>" + Lang("Description") + " : </b>" + name + "</b></td></tr>");
	Response.Write("<tr><td colspan=2><b>" + Lang("Current Stock") + " : </b></td></tr>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["transfer"].Rows[i];
		string branch = dr["branch_name"].ToString();
		string qty = dr["qty"].ToString();
		Response.Write("<tr><td>" + branch + "</td><td><b>" + qty + "</b></td></tr>");
	}

	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td colspan=2 align=center><h5>" + Lang("Transfer") + " </h5></td></tr>");
	Response.Write("<tr><td align=right><b>" + Lang("QTY to transfer") + " : </b></td><td><input type=text name=qty value=1></td></tr>");
	Response.Write("<tr><td align=right><b>" + Lang("From Branch") + " : </b></td><td><select name=branch_from>" + SPrintBranchOptions("1") + "</select></td></tr>");
	Response.Write("<tr><td align=right><b>" + Lang("To Branch") + " : </b></td><td><select name=branch_to>" + SPrintBranchOptions("2") + "</select></td></tr>");
	Response.Write("<tr><td colspan=2 align=center><input type=submit name=cmd value='" + Lang("Transfer") + "'  "+ Session["button_style"] +"></td></tr>");
	Response.Write("</table></form>");
	return true;
}

string SPrintBranchOptions(string current_id)
{
	int rows = 0;
	string s = "";
	string sc = " SELECT id, name FROM branch WHERE activated = 1 ORDER BY id ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "branch");
	}
	catch(Exception e1) 
	{
		if(e1.ToString().IndexOf("Invalid column name 'activated'") >= 0)
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
		ShowExp(sc, e1);
		return "";
	}

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["branch"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		s += "<option value=" + id;
		if(id == current_id)
			s += " selected";
		s += ">" + name + "</option>";
	}
	return s;	
}

bool DoStockTransfer()
{
	string code = Request.QueryString["code"];
	string from_branch = Request.Form["branch_from"];
	string to_branch = Request.Form["branch_to"];
	string from_branch_name = GetBranchName(from_branch);
	string to_branch_name = GetBranchName(to_branch);
	string qty = Request.Form["qty"];
	int nQty = MyIntParse(qty);

	string sc = "";
	sc += " IF NOT EXISTS";
	sc += " (SELECT * FROM stock_qty WHERE code=" + code + " AND branch_id = " + from_branch + ") ";
	sc += " INSERT INTO stock_qty (code, qty, branch_id) VALUES(" + code + ", " + (0 - nQty).ToString() + ", " + from_branch + ") ";
	sc += " ELSE ";
	sc += " UPDATE stock_qty SET qty = qty - " + qty + " WHERE code = " + code + " AND branch_id = " + from_branch;
	
	sc += " INSERT INTO stock_adj_log (staff, code, qty, branch_id, note) VALUES(" + Session["card_id"].ToString();
	sc += ", " + code + ", " + (0 - nQty).ToString() + ", " + from_branch + ", 'transfered to " + to_branch_name + " branch') "; 

	sc += " IF NOT EXISTS";
	sc += " (SELECT * FROM stock_qty WHERE code=" + code + " AND branch_id = " + to_branch + ") ";
	sc += " INSERT INTO stock_qty (code, qty, branch_id) VALUES(" + code + ", " + nQty.ToString() + ", " + to_branch + ") ";
	sc += " ELSE ";
	sc += " UPDATE stock_qty SET qty = qty + " + qty + " WHERE code = " + code + " AND branch_id = " + to_branch;

	sc += " INSERT INTO stock_adj_log (staff, code, qty, branch_id, note) VALUES(" + Session["card_id"].ToString();
	sc += ", " + code + ", " + qty + ", " + to_branch + ", 'transfered from " + from_branch_name + " branch') "; 

//DEBUG("sc=", sc);
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
	return true;
}
/*
string GetBranchName(string id)
{
	if(dst.Tables["branch_name"] != null)
		dst.Tables["branch_name"].Clear();

	string sc = " SELECT name FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "branch_name") == 1)
			return dst.Tables["branch_name"].Rows[0]["name"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}
*/

</script>

<asp:Label id=LFooter runat=server/>

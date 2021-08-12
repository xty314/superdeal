<!-- #include file="page_index.cs" -->
<script runat=server>
DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table

DataTable dtst = null;

string m_ssid = "";
string m_id = "";
string m_sRequestNote = "";
string m_sHeadOfficeBranchID = "1";
string m_sRequestDate = "";
string m_sRequestBranch = "";
string m_sFromEmail = "";
bool m_bFinished = false;
bool m_bIsHeadOffice = false;
bool m_bAllowAllBranchDoStockTransfer = true;
string cat = "";
string cat_old = "";
string tableWidth = "97%";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(Session["branch_support"] == null)
	{

		PrintAdminHeader();
		PrintAdminMenu();
		PrintAdminFooter();
		Response.Write("<br><center><h4>Sorry no branch support</h4>");
		Response.Write("<br><a title='back to main menu' href='' class=o>Back to Main Menu</a></center>");
		return;
	}

    if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
    if(Request.QueryString["ocat"] != null && Request.QueryString["ocat"] != "")
		cat_old = Request.QueryString["ocat"];
	
	m_bAllowAllBranchDoStockTransfer = MyBooleanParse(GetSiteSettings("allow_all_branch_do_stock_transfer", "1"));
	m_sHeadOfficeBranchID = GetSiteSettings("head_office_branch_id", "1");
	if(Session["login_branch_id"] != null)
	{
		if(Session["login_branch_id"].ToString() == m_sHeadOfficeBranchID)
			m_bIsHeadOffice = true;
	}

	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
	{
		m_ssid = Request.QueryString["ssid"];
	}
	else
	{
		m_ssid = DateTime.Now.ToOADate().ToString(); //assign new Sales Session ID for this sales
		string par = "?ssid=" + m_ssid;
		if(Request.QueryString.Count > 0)
			par = "?" + Request.ServerVariables["QUERY_STRING"] + "&ssid=" + m_ssid;
		Response.Redirect("stktran.aspx" + par);
		return;
	}

	CheckTransferTable();

    
    if(Request.QueryString["new"] == "1")
    {
        Session["stktran" + m_ssid] = null;
        CheckTransferTable();
    }
        
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];
	if((Request.Form["barcode"] != null && Request.Form["barcode"] != "") || cat != "")
	{
		if(DoItemSearch())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&ocat=" + HttpUtility.UrlEncode(cat) +"&ssid=" + m_ssid + "\">");
			return;
		}
		else
		{
			Response.Write("<script>window.alert('Product not found');</script");
			Response.Write(">");
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&ssid=" + m_ssid + "\">");
			return;
		}
		
	}

	if(m_id != "")
	{
		Session["stktran" + m_ssid] = null; //prepare new transfer
		CheckTransferTable();
		if(!DoRestoreRequest(m_id))
			return;
	}
    if(Request.QueryString["t"] == "del")
    {
        if(DoDelTransfer())
        {
            
			Response.Write("<br><br><br><center><h1><font color=red><b>Transfer deleted successfully!</b></font></h1></center>");
            Response.Write("<meta http-equiv=\"refresh\" content=\"2; URL=stktran.aspx?t=list&showall=1\">");
        }
        return;
    }
	if(Request.QueryString["t"] == "list")
	{
		PrintRequestList();
		return;
	}
	else if(Request.QueryString["t"] == "done")
	{
		Session["stktran" + m_ssid] = null; //prepare new transfer		
		CheckTransferTable();
		PrintAdminHeader();
		PrintAdminMenu();

		Response.Write("<br><center><h4>Stock transfered</h4>");
		Response.Write("<h5><a href=stktran.aspx class=o>New Transfer</a>");
		Response.Write("<h4><a href=stktran.aspx?id=" + m_id + "&t=print class=o target=new>Print</a></h4>");
		PrintAdminFooter();
		return;
	}
	else if(Request.QueryString["t"] == "request_done")
	{		
		Session["stktran" + m_ssid] = null; //prepare new transfer		
		CheckTransferTable();
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h4>Stock Transfered Request done.</h4>");
		Response.Write("<h4><a href=stktran.aspx class=o>New Request</h4></a></h4>");
		Response.Write("<h4><a href=stktran.aspx?id=" + m_id + "&t=print class=o target=new>Print</a></h4>");
		return;
	}
	else if(Request.QueryString["t"] == "print")
	{
		DoPrintRequest(false);
		return;
	}

	string cmd = "";
	if(Request.Form["cmd"] != null)
		cmd = Request.Form["cmd"];
	if(cmd == "Set")
	{
		if(DoSetValues())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?ssid=" + m_ssid + "");
			if(m_id != "")
				Response.Write("&id="+ m_id);
			Response.Write(" \">");
		}
		return;
	}
	else if(cmd == "X")
	{
		if(DoDelRow())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?ssid=" + m_ssid + "");
			if(m_id != "")
				Response.Write("&id="+ m_id);
			Response.Write(" \">");
		}		
		return;
	}
	else if(cmd == "Transfer")
	{
		if(DoSetValues())
		{
			if(m_id == "")
			{
				if(!DoRequestTransfer())
					return;
			}
			if(DoTransfer())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&t=done&ssid=" + m_ssid + "\">");
		}
		return;
	}
	else if(cmd == "Request")
	{
		if(DoSetValues())
		{			
			if(DoRequestTransfer())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&t=request_done&ssid=" + m_ssid + "\">");
		}
		return;
	}
	else if(cmd == "Delete")
	{
		if(DoDeleteRequest())
		{
			Session["stktran" + m_ssid] = null; //prepare new transfer
			PrintAdminHeader();
			PrintAdminMenu();
			Response.Write("<br><center><h4>Done, request deleted.</h4>");
			Response.Write("<a href=stktran.aspx?t=list class=o>List Requests</a>");
		}
		return;
	}
	else if(cmd == "Email" || (Request.Form["email"] != null && Request.Form["email"] != ""))
	{
		if(DoPrintRequest(true))
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&ssid=" + m_ssid + "\">");
		return;
	}

	PrintTransferForm();
}

bool CheckTransferTable()
{
	if(Session["stktran" + m_ssid] != null)
	{
		dtst = (DataTable)Session["stktran" + m_ssid];
		return false;
	}
	dtst = new DataTable();
	dtst.Columns.Add(new DataColumn("code", typeof(String)));
	dtst.Columns.Add(new DataColumn("barcode", typeof(String)));
	dtst.Columns.Add(new DataColumn("name", typeof(String)));
	dtst.Columns.Add(new DataColumn("from", typeof(String)));
	dtst.Columns.Add(new DataColumn("to", typeof(String)));
	dtst.Columns.Add(new DataColumn("qty", typeof(String)));
	dtst.Columns.Add(new DataColumn("price", typeof(String)));
	dtst.Columns.Add(new DataColumn("kid", typeof(String)));
    dtst.Columns.Add(new DataColumn("cat", typeof(String)));
	Session["stktran" + m_ssid] = dtst;
	return true;
}

bool PrintTransferForm()
{
	PrintAdminHeader();
	PrintAdminMenu();

/*	Response.Write("<br><center><h4>Stock Transfer");
	if(!m_bIsHeadOffice && !m_bAllowAllBranchDoStockTransfer)
		Response.Write(" Request");
	if(m_id != "")
		Response.Write(" #" + m_id);
	if(m_bFinished)
		Response.Write(" <font color=red>(finished)</font>");
	Response.Write("</h4>");
	*/
	Response.Write("<form name=f action=?id=" + m_id + "&ssid=" + m_ssid + "");		
	Response.Write(" method=post>");
		Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Stock Transfer</b><font color=red><b>");
	if(!m_bIsHeadOffice && !m_bAllowAllBranchDoStockTransfer)
		Response.Write(" Request");
	if(m_id != "")
		Response.Write(" #" + m_id);
	if(m_bFinished)
		Response.Write(" <font color=red>(finished)</font>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	
    
 

	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=7><b>" + Lang("Catalog Select") + " :</b>&nbsp;&nbsp;");
    if(!doCatSearch())
        return false;
    Response.Write("<br></td></tr>");
//	Response.Write("<a href=stktran.aspx?t=list class=o>List Requests</a>");

	Response.Write("<input type=hidden name=del_row value=''>");
	Response.Write("<input type=hidden name=del_kid value=''>");

//	Response.Write("<table width=90% align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
//	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	/*Response.Write("<tr>\r\n");
	Response.Write("<td colspan=3 align=right>FROM: <select name=branch_from0>" +  SPrintBranchOptions(Request.Form["branch_from0"], "from") + "</select></td>");
	Response.Write("<td colspan=3 > TO: <select name=branch_to0>" +  SPrintBranchOptions(Request.Form["branch_to0"], "to") + "</select></td>");
	Response.Write("</tr>");
	*/
	Response.Write("<tr align=left style=\"color:white;background-color:#3a6ea5;font-weight:bold;\">\r\n");
	Response.Write("<th width=150>ITEM CODE</th>\r\n");
	Response.Write("<th width=150>PARTS#</th>\r\n");
	Response.Write("<th>DESCRIPTION</th>\r\n");
    Response.Write("<th>Cat</th>\r\n");
	Response.Write("<th>SELLING PRICE</th>\r\n");
	Response.Write("<th width=100>FROM</th>\r\n");
	Response.Write("<th width=100>TO</th>\r\n");
	Response.Write("<th width=100 align=center>Qty</th>\r\n");
	Response.Write("<td>&nbsp;</td>");
	Response.Write("</tr>\r\n");

	bool bAlterColor = false;
    int nRows = dtst.Rows.Count;
    //paging class
	/*
    PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	m_cPI.TotalRows = nRows;
	bool bQAdded = false;
	m_cPI.URI = "?id=" + m_id + "&ssid=" + m_ssid + "";
	m_cPI.PageSize = 30;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	if(end > nRows)
		end = nRows;
	string sPageIndex = m_cPI.Print();
     */
    
	
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = dtst.Rows[i];
		string kid = dr["kid"].ToString();
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		string from = dr["from"].ToString();
		string to = dr["to"].ToString();
		string qty = dr["qty"].ToString();
		string price = dr["price"].ToString();
        string catlog = dr["cat"].ToString();
        
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;
		Response.Write("<td>" + code + "</td>");
		Response.Write("<td>" + barcode + "</td>");
		Response.Write("<td>" + name + "</td>");
        Response.Write("<td>" + catlog + "</td>");
		Response.Write("<td>" + double.Parse(price).ToString("c") + "</td>");
		Response.Write("<td><select name=branch_from" + i + " ");
		if(i>0)
			Response.Write(" disabled ");
		Response.Write(">" +  SPrintBranchOptions(from, "from") + "</select></td>");
		Response.Write("<td><select name=branch_to" + i + " ");
		if(i>0)
			Response.Write(" disabled ");
		Response.Write(">" +  SPrintBranchOptions(to, "to") + "</select></td>");
		Response.Write("<td align=right><input type=text class=sr size=3 name=qty" + i + " value='" + qty + "'></td>");
		Response.Write("<td align=right>");
		Response.Write("<input type=hidden name='kid"+ i +"' value="+ kid +">");
		if(!m_bFinished)
		{
			Response.Write("<input type=submit name=cmd value=Set "+ Session["button_style"] +" >"); //onclick='document.f.update_row.value=" + i +"; document.f.kid.value=" + kid +";' >");
			Response.Write("<input type=submit name=cmd value=X "+ Session["button_style"] +" onclick=\"document.f.del_kid.value='" + kid + "';document.f.del_row.value='" + i + "'; \" > ");
		}
		Response.Write("</td>");
		Response.Write("</tr>");
	}
   // Response.Write("<tr><td colspan=8>" + sPageIndex + "</td></tr>");
	if(!m_bFinished)
	{
//		if(m_id == "")
			Response.Write("<tr><td><input type=text size=10 class=s name=barcode><input type=submit name=cmd value=Scan "+ Session["button_style"] +"></td><td colspan=5>&nbsp;</td></tr>");
	}
	Response.Write("<tr bgcolor=#EEEEE ><td colspan=9 align=right>");
	if(m_id != "") //restored
		Response.Write("<br><b>Request Note : </b>" + m_sRequestNote.Replace("\r\n", "<br>\r\n") + "</td></tr>");
	else
	{
		Response.Write("<table><tr><td><b>Request Note:</b></td></tr>");
		Response.Write("<tr><td><textarea name=request_note cols=55 rows=5>" + m_sRequestNote + "</textarea></td></tr>");
		Response.Write("</table>");
	}
	Response.Write("</td></tr>");
/*	if(m_bIsHeadOffice || m_bAllowAllBranchDoStockTransfer)
	{
		Response.Write("<tr><td colspan=6 align=right>");
		Response.Write("<table><tr><td><b>Transfer Note:</b></td></tr><tr><td><textarea name=finished_note cols=55 rows=5></textarea></td></tr></table>");
		Response.Write("</td></tr>");
	}
*/
	Response.Write("<tr bgcolor=#EEEEE ><td colspan=9 align=right>");	
	if(m_id != "")
	{
		Response.Write("Email to : <input type=text name=email value='" + Request.Form["email"] + "'><input type=submit name=cmd value=Email "+ Session["button_style"] +">");
		Response.Write("<input type=button onclick=\"window.open('stktran.aspx?id=" + m_id + "&t=print&ssid="+ m_ssid +"');\" value='Print/View Transfer' "+ Session["button_style"] +">");	
	}
	Response.Write("<input type=button value='New Transfer' "+ Session["button_style"] +" onclick=window.location=('stktran.aspx');>");
	Response.Write("<input type=button value='Stock Transfer List' "+ Session["button_style"] +" onclick=window.location=('stktran.aspx?t=list');>");
	if(m_bIsHeadOffice || m_bAllowAllBranchDoStockTransfer)
	{
		if(m_id == "")
			Response.Write("<input type=submit name=cmd value=Request "+ Session["button_style"] +">");
		if(!m_bFinished)
		{
			Response.Write("<input type=submit name=cmd value=Transfer onclick=\"return confirm('Are you SURE want to Transfer???');\" "+ Session["button_style"] +">");
			Response.Write("<input type=checkbox name=confirm_delete>Confirm Delete ");
			Response.Write("<input type=submit name=cmd value=Delete "+ Session["button_style"] +" ");
			Response.Write("onclick=\"if(!document.f.confirm_delete.checked){window.alert('Please tick Confirm Delete');return false;}\">");
		}
		
		
	}
	else
	{
		if(m_id != "")
		{
			if(!m_bFinished)
			{
				Response.Write("<input type=checkbox name=confirm_delete>Confirm Delete ");
				Response.Write("<input type=submit name=cmd value=Delete "+ Session["button_style"] +" ");
				Response.Write("onclick=\"if(!document.f.confirm_delete.checked){window.alert('Please tick Confirm Delete');return false;}\">");
			}
			Response.Write("<input type=button value='Printable Copy' "+ Session["button_style"] +" onclick=window.open('stktran.aspx?id=" + m_id + "&t=print&ssid=" + m_ssid + "');>");
		}
		else
			Response.Write("<input type=submit name=cmd value=Request "+ Session["button_style"] +">");
	}
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("<script>document.f.barcode.focus();</script");
	Response.Write(">");
	PrintAdminFooter();
	return true;
}

string SPrintBranchOptions(string current_id, string type)
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
		if(!m_bIsHeadOffice && !m_bAllowAllBranchDoStockTransfer)
		{
			if(type == "from")
			{
				if(id != m_sHeadOfficeBranchID)
					continue;
			}
			else if(type == "to")
				if(id != Session["login_branch_id"].ToString())
					continue;
		}
		s += "<option value=" + id;
		if(id == current_id)
			s += " selected";
		s += ">" + name + "</option>";
	}
	return s;	
}

bool DoRequestTransfer()
{
	int nRows = dtst.Rows.Count;
	if(nRows <= 0)
	{
		Response.Write("<h4>Nothing to request</h4>");
		return false;
	}
	//check qty
	for(int i=0; i<nRows; i++)
	{
		string qty = dtst.Rows[i]["qty"].ToString();
		try
		{
			int nQty = int.Parse(qty);
		}
		catch(Exception e)
		{
			Response.Write("<br><center><h4>Error, " + qty + " is not a valid Number.");
			return false;
		}
	}

	string from_id = dtst.Rows[0]["from"].ToString();
	string to_id = dtst.Rows[0]["to"].ToString();
	string note = Request.Form["request_note"];

	//fixed branch stock transfer
//	fixBranchTransfer();

	string sc = "  ";	
	sc = " BEGIN TRANSACTION ";
	sc += " INSERT INTO stock_transfer_request (from_branch_id, to_branch_id, staff_request, request_note) ";
	sc += " VALUES(" + from_id + ", " + to_id + ", " + Session["card_id"].ToString() + ", '" + EncodeQuote(note) + "') ";
	sc += " SELECT IDENT_CURRENT('stock_transfer_request') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "id") == 1)
		{
			m_id = dst.Tables["id"].Rows[0]["id"].ToString();
		}
		else
		{
			Response.Write("<br><br><center><h3>Record Request failed, error getting new id</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = dtst.Rows[i];
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
	//	DEBUG("from =", dr["from"].ToString());
	//	DEBUG("to =", dr["to"].ToString());
		sc += " INSERT INTO stock_transfer_request_item (id, code, qty) VALUES(" + m_id + ", " + code + ", " + qty + ")";
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
bool DoDelTransfer()
{
    if(m_id == "")
	{
		Response.Write("<h4>Transfer ID Error</h4>");
		return false;
	}
    int nRows = dtst.Rows.Count;
    for(int i=0; i<nRows; i++)
	{
		DataRow dr = dtst.Rows[i];
		string code = dr["code"].ToString();
		string to = dr["from"].ToString();
		string from = dr["to"].ToString();
		string qty = dr["qty"].ToString();		
		if(!DoStockTransfer(code, from, to, qty))
			return false;
	}
    string sc = " DELETE FROM stock_transfer_request WHERE id = " + m_id;
	sc += " DELETE FROM stock_transfer_request_item WHERE id = " + m_id;
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




bool DoTransfer()
{
	//fixed branch stock transfer
//	fixBranchTransfer();

	int nRows = dtst.Rows.Count;
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = dtst.Rows[i];
		string code = dr["code"].ToString();
		string from = dr["from"].ToString();
		string to = dr["to"].ToString();
		string qty = dr["qty"].ToString();		
		if(!DoStockTransfer(code, from, to, qty))
			return false;
	}
	if(m_id == "")
		return true;
	string sc = " UPDATE stock_transfer_request SET done = 1, staff_finished = " + Session["card_id"].ToString();
	sc += ", date_finished = GETDATE() ";
	sc += " WHERE id = " + m_id;
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

bool DoStockTransfer(string code, string from_branch, string to_branch, string qty)
{
	string from_branch_name = GetBranchName(from_branch);
	string to_branch_name = GetBranchName(to_branch);
	int nQty = MyIntParse(qty);

	string sc = " BEGIN TRANSACTION ";
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
	sc += " COMMIT ";

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

/*string GetBranchName(string id)
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
}*/

bool DoItemSearch()
{
	int rows = 0;
    string barcode = Request.Form["barcode"];
    if(barcode == null)
        barcode = "";
	if(barcode == "" && (cat == "all" || cat == ""))
		return false;
    string sc = "";
    if(barcode.ToLower() =="all")
    {
	    sc = " SELECT c.* FROM code_relations c ";
	    sc += " WHERE 1=1 ORDER BY c.name ";
        
    }
    else if(cat != "all" && cat !="" && barcode == "")
    {
        sc = " SELECT c.* FROM code_relations c ";
	    sc += " WHERE 1=1 AND c.cat = N'" + cat + "' ORDER BY c.name ";
    }
    else
    {
        sc = " SELECT c.* FROM code_relations c JOIN product p ON p.code = c.code ";
	    sc += " WHERE c.barcode LIKE '%" + EncodeQuote(barcode) + "%' ";
        sc += " OR c.supplier_code LIKE N'%" + EncodeQuote(barcode) + "%' ";
        sc += " OR upper(c.name) LIKE upper(N'%" + EncodeQuote(barcode) + "%') ";
	    if(TSIsDigit(barcode))
		    sc += " OR c.code = " + barcode;
    }
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "item_search");
        if(rows <= 0)
		{
			return false;
		}
        
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
    for(int i=0; i<rows; i++)
    {
	    DataRow dr = dst.Tables["item_search"].Rows[i];
	    barcode = dr["barcode"].ToString();
	    string code = dr["code"].ToString();
	    string name = dr["name"].ToString();
	    double dPrice = double.Parse(dr["price1"].ToString());
        dPrice = dPrice/1.15;
        string price = dPrice.ToString();
        string supplier_code = dr["supplier_code"].ToString();
        string catlog = dr["cat"].ToString();

	    AddToTransTable(code, supplier_code, name, "0", "0", "1", price, "", catlog);
    }
    return true;
}

bool AddToTransTable(string code, string barcode, string name, string from, string to, string qty, string price, string kid, string catlog)
{
	DataRow dr = dtst.NewRow();
	dr["code"] = code;
	dr["barcode"] = barcode;
	dr["name"] = name;
	dr["from"] = from;
	dr["to"] = to;
	dr["qty"] = qty;
	dr["price"] = price;
	dr["kid"] = kid;
    dr["cat"] = catlog;
	dtst.Rows.Add(dr);
//DEBUG("add to table, code=", code);

	if(m_id != "" && Request.Form["barcode"] != null && Request.Form["barcode"] != "") //add item
	{
		string sc = " INSERT INTO stock_transfer_request_item (id, code, qty) VALUES(" + m_id + ", " + code + ", " + qty + ")";
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

bool DoSetValues()
{
	int nRows = dtst.Rows.Count;
	for(int i=nRows-1; i>=0; i--)
	{
		string qty = Request.Form["qty" + i];
		int quantity = MyIntParse(qty);
		if(m_id != "")
		{
			string sc = " UPDATE stock_transfer_request_item SET qty = "+ Request.Form["qty" + i] +" WHERE kid = " + Request.Form["kid" + i].ToString() +" AND id = "+ m_id;	
			sc += " UPDATE stock_transfer_request SET from_branch_id = "+ Request.Form["branch_from0"] +", to_branch_id = "+ Request.Form["branch_to0"] +" WHERE id = "+ m_id;	
//DEBUG(" sc update =", sc);
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
		if(quantity <= 0)
		{
			dtst.Rows.RemoveAt(i);
		}
		else
		{
//	DEBUG("qtyasn =", quantity.ToString() );
//  DEBUG("i =", Request.Form["branch_to"+i].ToString());
			dtst.Rows[i].BeginEdit();
			//if(i>0)
			{
				dtst.Rows[i]["from"] = Request.Form["branch_from0"];			
				dtst.Rows[i]["to"] = Request.Form["branch_to0"];
			}
			/*else
			{
				dtst.Rows[i]["from"] = Request.Form["branch_from"+i];			
				dtst.Rows[i]["to"] = Request.Form["branch_to"+i];
			}*/
			
			dtst.Rows[i]["qty"] = quantity.ToString();
			dtst.Rows[i].EndEdit();		
		}
	}
	dtst.AcceptChanges();

	return true;
}

bool DoDelRow()
{
//DEBUG("del rwo =", Request.Form["del_row"].ToString());
//return false;
	int nRow = MyIntParse(Request.Form["del_row"]);
	dtst.Rows.RemoveAt(nRow);
	dtst.AcceptChanges();

	if(m_id != "")
	{
		string sc = " DELETE FROM stock_transfer_request_item WHERE kid = " + Request.Form["del_kid"].ToString() +" AND id = "+ m_id;	
//DEBUG(" sc Delete =", sc);
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

bool PrintRequestList()
{
	string sc = " SELECT DISTINCT r.id, r.done, c.name AS staff, b1.name AS from_branch_name, b2.name AS to_branch_name, r.date_request, r.request_note ";
//	sc += ", cr.name AS item_name, cr.barcode, ri.code, ri.qty ";
	sc += " FROM stock_transfer_request r ";
	sc += " JOIN stock_transfer_request_item ri ON ri.id = r.id ";
//	sc += " JOIN stock_transfer_request_item ri ON ri.id = r.id ";
	sc += " LEFT OUTER JOIN branch b1 ON b1.id = r.from_branch_id ";
	sc += " LEFT OUTER JOIN branch b2 ON b2.id = r.to_branch_id ";
	sc += " LEFT OUTER JOIN card c ON c.id = r.staff_request ";
//	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = ri.code ";
	sc += " WHERE 1=1 ";
	if(!m_bIsHeadOffice)
		sc += " AND (r.to_branch_id = " + Session["login_branch_id"].ToString() + " OR r.from_branch_id = " + Session["login_branch_id"].ToString() + ") ";
	if(Request.QueryString["showall"] != "1")
		sc += " AND r.done = 0 ";
	sc += " ORDER BY r.id DESC ";
//	sc += " ORDER BY r.to_branch_id, r.id ";
//	sc += " ri.kid ";
//DEBUG("sc=", sc);
	int rows = 0;
	PrintAdminHeader();
	PrintAdminMenu();
//	Response.Write("<br><center><h4>Stork Transfer Request List</h4>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Stork Transfer Request List</b><font color=red><b>");	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	

	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4 ><br></td></tr>");
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "list");
/*		if(rows <= 0)
		{
			Response.Write("<br><br><h4>No Request.</h4>");
			return false;
		}
	*/
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

/*	Response.Write("<table width=55% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
*/
	Response.Write("<tr><td colspan=4 align=center>");

	if(Request.QueryString["showall"] == "1")
	{
		//Response.Write("<a href=stktran.aspx?t=list class=o>Show Unfinished Only</a>");
		Response.Write("<input type=button value='Show Unfinished Only' "+ Session["button_style"] +" onclick=window.location=('stktran.aspx?t=list');>");
	}
	else
	{
		//Response.Write("<a href=stktran.aspx?t=list&showall=1 class=o>Show All(include finished)</a>");
		Response.Write("<input type=button value='Show All(include finished)' "+ Session["button_style"] +" onclick=window.location=('stktran.aspx?t=list&showall=1');>");
	}
	//Response.Write(" &nbsp&nbsp; <a href=stktran.aspx class=o>New Request</a>");
	Response.Write("<input type=button value='New Request' "+ Session["button_style"] +" onclick=window.location=('stktran.aspx');>");
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:black;background-color:#b0ebeb;font-weight:bold;\">\r\n");
	Response.Write("<th>ID</th><th nowrap>BRANCH REQUESTED</th><th nowrap>DATE REQUESTED</th>");
	Response.Write("<th>FINISHED</th>");
	Response.Write("</tr>");

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = MyIntParse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	m_cPI.TotalRows = rows;
	bool bQAdded = false;
	m_cPI.URI = "?t=list&ssid=" + m_ssid;
	if(Request.QueryString["showall"] == "1")
		m_cPI.URI += "&showall=1";
	m_cPI.PageSize = 30;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	if(end > rows)
		end = rows;
	string sPageIndex = m_cPI.Print();

	bool bColor = false;
	for(; i<end; i++)
	{
		DataRow dr = dst.Tables["list"].Rows[i];
		string id = dr["id"].ToString();
		string branch = dr["to_branch_name"].ToString();
		string rdate = DateTime.Parse(dr["date_request"].ToString()).ToString("dd/MM/yyyy");
		bool bDone = MyBooleanParse(dr["done"].ToString());
		string sDone = "Yes";
		if(!bDone)
			sDone = "No";

		Response.Write("<tr");
		if(bColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bColor = !bColor;
		Response.Write("<td><a href=stktran.aspx?id=" + id + " class=o>" + id + "</a></td>");		
		Response.Write("<td>" + branch + "</td>");
		Response.Write("<td>" + rdate + "</td>");
		Response.Write("<td>" + sDone);
		if(!bDone)
			Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a href=stktran.aspx?id=" + id + " class=o>open</a>");
        else
        {
            if(int.Parse(Session["employee_access_level"].ToString()) >= 10)
            {
                Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                Response.Write("<input type=button value='DEL' "+ Session["button_style"] +" ");
                Response.Write("onclick=\"if (confirm('Are you sure you want to delete the transfer?')) window.location.href='stktran.aspx?t=del&id=" + id + "';\">"); 
            } 
        }
//			Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<input type=button value=Open "+ Session["button_style"] +" onclick=window.location='stktran.aspx?id=" + id + "'>");
		Response.Write(" | <a href='stktran.aspx?id=" + id + "&t=print&ssid="+ m_ssid +"' class=o target=_new>Print/View Transfer</a></td>");
		Response.Write("</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=4>" + sPageIndex + "</td></tr>");
	Response.Write("</table>");
	PrintAdminFooter();
	return true;
}

bool DoRestoreRequest(string id)
{
	string sc = " SELECT r.id, r.done, c.name AS staff, b1.name AS from_branch_name, b2.name AS to_branch_name, b1.email AS from_branch_email ";
	sc += ", r.from_branch_id, r.to_branch_id, r.date_request, r.request_note ";
	sc += ", cr.name AS item_name, cr.barcode, ri.code, ri.qty, cr.price1/1.15 AS price1 , ri.kid, cr.cat AS catlog, cr.supplier_code ";
	sc += " FROM stock_transfer_request r ";
	sc += " JOIN stock_transfer_request_item ri ON ri.id = r.id ";
	sc += " LEFT OUTER JOIN branch b1 ON b1.id = r.from_branch_id ";
	sc += " LEFT OUTER JOIN branch b2 ON b2.id = r.to_branch_id ";
	sc += " LEFT OUTER JOIN card c ON c.id = r.staff_request ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = ri.code ";
	sc += " WHERE r.id = " + id;
//	if(Request.QueryString["showall"] != "1")
//		sc += " AND r.done = 0 ";
	sc += " ORDER BY r.to_branch_id, r.id ";
	sc += ", ri.kid ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "request") <= 0)
		{
			Response.Write("<br><br><h4>No Item Found.</h4>");
			return false;
		}
	}
	catch(Exception e) 
	{
		myConnection.Close();
		string err = e.ToString();
		if(err.IndexOf("Invalid column name 'from_branch_id'")>=0)
		{
			string scc = " alter table stock_transfer_request_item add from_branch_id [int] not null default(1), to_branch_id [int] not null default(1) ";
			try
			{
				myCommand = new SqlCommand(scc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				//ShowExp(sc, e);
				return false;
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&t=request_done&ssid=" + m_ssid + "\">");
			return false;
		}
		if(err.IndexOf("Invalid column name 'email'")>=0)
		{
			string scc = " alter table branch add email [varchar](255) not null default('') ";
			try
			{
				myCommand = new SqlCommand(scc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				//ShowExp(sc, e);
				return false;
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&t=request_done&ssid=" + m_ssid + "\">");
			return false;
		}
		//ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<dst.Tables["request"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["request"].Rows[i];
		if(i == 0)
		{
			m_sRequestNote = dr["request_note"].ToString();
			m_sRequestDate = DateTime.Parse(dr["date_request"].ToString()).ToString("dd/MM/yyyy");
			m_sRequestBranch = dr["to_branch_name"].ToString();
			m_sFromEmail = dr["from_branch_email"].ToString();
			m_bFinished = MyBooleanParse(dr["done"].ToString());
		}
		string price = dr["price1"].ToString();
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["item_name"].ToString();
		string from = dr["from_branch_id"].ToString();
		string to = dr["to_branch_id"].ToString();
		string qty = dr["qty"].ToString();
        string catlog = dr["catlog"].ToString();
        string supplier_code = dr["supplier_code"].ToString();		
		AddToTransTable(code, supplier_code, name, from, to, qty, price, dr["kid"].ToString(), catlog);
	}
	return true;
}

bool DoDeleteRequest()
{
	if(m_id == "")
	{
		Response.Write("<h4>Error No ID</h4>");
		return false;
	}
	string sc = " DELETE FROM stock_transfer_request WHERE id = " + m_id;
	sc += " DELETE FROM stock_transfer_request_item WHERE id = " + m_id;
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

bool DoPrintRequest(bool bEmail)
{
	StringBuilder sb = new StringBuilder();
	if(bEmail)
	{
		string s = ReadSitePage("page_header");
		int pos = s.ToLower().IndexOf("<body");
		if(pos > 0)
			s = s.Substring(0, pos);
		sb.Append(s);
		sb.Append("<body marginwidth=0 marginheight=0 topmargin=1 leftmargin=0 text=black link=black vlink=black alink=black>");
	}

	sb.Append("<table width=70% align=center>");
	sb.Append("<tr><td>");
	sb.Append("<h1>" + m_sCompanyName + "</h1>");
	sb.Append("</td></tr><tr><td align=center>");

	sb.Append("<font size=+1><b>Stock Transfer");
	if(!m_bFinished)
		sb.Append(" Request");
	else
		sb.Append(" Notice");
	sb.Append("</b></font>");

	sb.Append("</td></tr>");
	sb.Append("<tr><td align=right>");
	sb.Append("<table>");
	sb.Append("<tr><td><b>Date : </td><td>" + m_sRequestDate + "</b></td></tr>");
	sb.Append("<tr><td><b>Request Branch : </td><td>" + m_sRequestBranch + "</b></td></tr>");
	sb.Append("</table>");
	sb.Append("</td></tr>");

	sb.Append("<tr><td align=center><br><br><br>");

	sb.Append("<table width=100% align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	sb.Append("<tr align=left>\r\n");
	sb.Append("<th width=150>ITEM CODE</th>\r\n");
	sb.Append("<th width=150>PARTS#</th>\r\n");
	sb.Append("<th width=25%>DESCRIPTION</th>\r\n");
    sb.Append("<th>CATLOG</th>\r\n");
	sb.Append("<th width=100>SELLING PRICE</th>\r\n");
	sb.Append("<th width=100>FROM</th>\r\n");
	sb.Append("<th width=100>TO</th>\r\n");
	sb.Append("<th width=100 align=center>Qty</th>\r\n");
	sb.Append("<th width=100 align=center></th>\r\n");
	sb.Append("<th width=100 align=center>Signature</th>\r\n");
	sb.Append("</tr>\r\n");

	sb.Append("<tr><td colspan=10><hr></td></tr>");

	int nRows = dtst.Rows.Count;
	for(int i=0; i<nRows; i++)
	{
		DataRow dr = dtst.Rows[i];
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		string from = dr["from"].ToString();
		string to = dr["to"].ToString();
		string qty = dr["qty"].ToString();
		string price = dr["price"].ToString();
        string catlog = dr["cat"].ToString();

		sb.Append("<tr align=left>");
		sb.Append("<td>" + code + "</td>");
		sb.Append("<td>" + barcode + "</td>");
		sb.Append("<td>" + name + "</td>");
        sb.Append("<td>" + catlog + "</td>");
		sb.Append("<td width=100>" + double.Parse(price).ToString("c") + "</td>");
		sb.Append("<td>" +  GetBranchName(from) + "</td>");
		sb.Append("<td>" +  GetBranchName(to) + "</td>");
		sb.Append("<td align=center>" + qty + "</td>");
		sb.Append("<td align=center><input type=checkbox readonly></td>");
		sb.Append("<td align=center></td>");
		sb.Append("</tr>");
	}

	sb.Append("<tr><td colspan=10><br><br><b>Request Note : </b><br><br>" + m_sRequestNote.Replace("\r\n", "<br>\r\n") + "</td></tr>");
	sb.Append("</td></tr>");
	sb.Append("<tr><td colspan=10 align=right>");
	sb.Append("</td></tr>");
	sb.Append("</table>");

	sb.Append("</td></tr></table>");

	if(!bEmail)
	{
		PrintAdminHeader();
		Response.Write(sb.ToString());
		return true;
	}

	m_sFromEmail = Request.Form["email"];
	if(m_sFromEmail == "")
		return false;
	//DEBUG("sb =", sb.ToString());
	MailMessage msgMail = new MailMessage();
	msgMail.To = m_sFromEmail;
	msgMail.From = Session["email"].ToString();
	msgMail.Subject = "Stock Transfer Request by " + m_sRequestBranch;
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = sb.ToString();
	SmtpMail.Send(msgMail);	
	return true;
}

bool fixBranchTransfer()
{
	string sc = " SELECT top 1 from_branch_id, to_branch_id FROM stock_transfer_request_item ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		
	}
	catch(Exception e) 
	{
		string err = e.ToString();
		if(err.IndexOf("Invalid column name 'from_branch_id'")>=0)
		{
			string scc = " alter table stock_transfer_request_item add from_branch_id [int] not null default(1), to_branch_id [int] not null default(1) ";
			try
			{
				myCommand = new SqlCommand(scc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception ee) 
			{
				//ShowExp(sc, e);
				return false;
			}
			//Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=stktran.aspx?id=" + m_id + "&t=request_done&ssid=" + m_ssid + "\">");
			//return false;
		}
		return true;
	}
	return true;
}
bool doCatSearch()
{
    int rows = 0;
	string sc = "SELECT DISTINCT cat FROM catalog WHERE cat <> 'Brands' ";
	sc += " ORDER BY cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
//DEBUG("rows=", sc);
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
	Response.Write("?ssid=" + m_ssid + "");
    Response.Write("&new=1");
	Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	//if(Request.QueryString["cat"] != null)
		//cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat_old == s)
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
		else
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");

	}

	Response.Write("</select>");
	
	
	
	return true;
}
</script>

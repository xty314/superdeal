<!-- #include file="page_index.cs" -->

<script runat=server>
DataSet dst = new DataSet(); 
string m_type = "";
string m_search = "";
string m_code = "";
//int mn_borrow_id = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	
	if(!SecurityCheck("technician"))
		return;

	if(Request.Form["cmd"] == "Cancel")
	{
		int rows = 0;
		if(Session["slt_count"] != null)
		{
			rows = (int)Session["slt_count"];
			for(int i=1; i<=rows; i++)
			{
				Session["slt_count"] = null;
				Session["slt_code"+ i] = null;
			}
		}
	}	
	if(Request.Form["cmd"] == " SEARCH " || Request.Form["search"] != "" && Request.Form["search"] != null)
		m_search = Request.Form["search"];
	//print list
	if(Request.QueryString["pr"] == "lt")
	{
		if(!GetBorrowItem())
			return;
		return;
	}

	// ap = approve items, 1= list
	if(Request.QueryString["lt"] != null && Request.QueryString["lt"] != "")
		m_type = Request.QueryString["lt"];

	InitializeData();
	if(Request.Form["cmd"] == "SEARCH ITEM" || Request.Form["sValue"] != "" && Request.Form["sValue"] != null)
	{
		if(!DoAddItemToSession())
			return;
	}
	if(Request.Form["cmd"] == "Approve")
	{
		if(DoUpdateApprove())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?lt=ap&r=" + DateTime.Now.ToOADate() +"' \">");
			return;
		}
	}
	if(Request.Form["cmd"] == "Return Borrowed Item")
	{
		if(DoUpdateReturn())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?lt=rt&r=" + DateTime.Now.ToOADate() +"' \">");
			return;
		}
	}
	if(Request.QueryString["b"] == "done")
	{
		Response.Write("<br><br><center><h4><a title='print current borrow list' href=\"javascript:list_window=window.open('"+ Request.ServerVariables["URL"] +"?pr=lt&bid="+ Session["bid"] +"', ''); list_window.focus();\" class=o>Print the Borrow List</h5></a>");
		Response.Write("<h4><a title='to borrow list' href='"+ Request.ServerVariables["URL"] +"?lt=ap&r="+ DateTime.Now.ToOADate() +"' class=o>Go to Borrow List</h5></a>");
		Response.Write("<h4><a title='new borrow' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"' class=o>Make New Borrow</h5></a>");
		return;
	}
	if(Request.Form["cmd"] == "Borrow Items From Stock")
	{
		if(DoInsertBorrowItems())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() +"&b=done\">");
			return;
		}
		
	}	
	//if(m_type == "bl" || m_type == "ap" || m_type == "rt")
	if(m_type != null && m_type != "")
	{
		if(!GetBorrowItem())
			return;
		return;
	}
	if(Request.QueryString["dl"] != "" && Request.QueryString["dl"] != null)
	{
		if(!DoDeleteRows())
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}
	DisplayAllProduct();

}

bool DoUpdateReturn()
{
	string rows = Request.Form["rows"];
	for(int i=0; i<int.Parse(rows); i++)
	{
		string rqty = Request.Form["rqty"+ i];
		if(rqty != "" && rqty !=null) // && int.Parse(rqty) > 0 )
		{
			string actual_rqty = Request.Form["actual_rqty"+ i];
			int nreturn_qty_left = int.Parse(rqty) - int.Parse(actual_rqty);
	//		DEBUG("nreturn_qty_left +'", nreturn_qty_left);
			if(nreturn_qty_left > 0)
			{
				int nborrow_qty = int.Parse(Request.Form["borrow_qty"+ i].ToString());
				nborrow_qty -= int.Parse(actual_rqty);

				string id = Request.Form["id"+ i];
				string sc = " UPDATE stock_borrow SET return_qty = "+ rqty +", return_date = GETDATE() WHERE id = "+ id;
				//if(g_bRetailVersion)
					sc += " UPDATE stock_qty SET qty = qty + "+ rqty +" WHERE code = (SELECT code FROM stock_borrow WHERE id =" + id +")";
				//else
				//	sc += " UPDATE product SET stock = stock + "+ rqty +" WHERE code = (SELECT code FROM stock_borrow WHERE id =" + id +")";
	
	//		return false;
//DEBUG("nbonborrow_qty = ", nborrow_qty);
//DEBUG("nreturn_qty_left =", nreturn_qty_left);
				if(nborrow_qty == nreturn_qty_left)
					sc += " UPDATE stock_borrow SET return_complete = 1 WHERE id = "+ id;
	//		DEBUG("sc =", sc);	
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
            /////cleanup those dead qty....
            else
            {
                string id = Request.Form["id"+ i];
                string sc = " UPDATE stock_borrow SET return_complete = 1 WHERE (approved_qty - replace_qty - return_qty) = 0 AND id="+ id +" ";
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
		}
        else
        {
            Response.Write("\r\n<script language='javascript'> ");
	        Response.Write("\r\n window.alert('Sorry the quantity of return item is either complete or insufficient, please check the return Quantity!!!'); \r\n");
	        Response.Write("</script");
            Response.Write(">\r\n");    
        }

	}
	return true;
}

bool DoUpdateApprove()
{
	string rows = Request.Form["rows"];
	for(int i=0; i<int.Parse(rows); i++)
	{
		string approved = Request.Form["check"+ i];
		string app_qty = Request.Form["rqty"+ i];
		if(approved == "on" && int.Parse(app_qty) > 0)
		{
			
			string id = Request.Form["id"+ i];
//DEBUG("apptaqyt = ", app_qty);
			string sc = " UPDATE stock_borrow SET approved = 1, approved_qty = "+ app_qty +", approved_by = "+ Session["card_id"] +" WHERE id = "+ id;
			if(g_bRetailVersion)
				sc += " UPDATE stock_qty SET qty = qty - (SELECT qty FROM stock_borrow WHERE id = "+ id +") WHERE code = (SELECT code FROM stock_borrow WHERE id =" + id +")";
			else
				sc += " UPDATE product SET stock = stock - (SELECT qty FROM stock_borrow WHERE id = "+ id +") WHERE code = (SELECT code FROM stock_borrow WHERE id =" + id +")";
	//	DEBUG("sc =", sc);
	//	return false;
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
	}
	return true;
}

bool DoDeleteRows()
{
	string code = "";
//DEBUG("coutn = ", Session["slt_count"].ToString());
	if(Session["slt_code"+ Request.QueryString["dl"]] != null)
	{
		Session["slt_code"+ Request.QueryString["dl"]] = null;
		int noldrows = (int)Session["slt_count"]; //old rows
		//int nrows = (int)Session["slt_count"] - 1; //new rows		
		int nrows = 0;
		if(Session["slt_code"+ Request.QueryString["dl"]] == null)
			nrows = int.Parse(Request.QueryString["dl"].ToString());
	
		for(int i=1; i<=noldrows; i++)
		{
			if(Session["slt_code"+ i] != null)
				Session["slt_code"+ i] = Session["slt_code"+ i];
		}
	}
	
	return true;
}

bool CheckValidSN(string sn)
{
	bool bValid = false;
	if(sn != null && sn != "")
	{
		string sc = "SELECT TOP 1 sq.code FROM stock s JOIN stock_qty sq ON sq.code = s.product_code ";
		sc += " WHERE sq.qty > 0 ";
		sc += " AND (s.sn = '"+ sn +"'"; 
		if(TSIsDigit(sn))
			sc += " OR sq.code = '"+ sn +"' ";
		
		sc += " ) ";
			
//	DEBUG("sc =", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "valid");
				
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
		if(dst.Tables["valid"].Rows.Count > 0)
		{
			m_code = dst.Tables["valid"].Rows[0]["code"].ToString();
			bValid = true;
		}
	}
	return bValid;
}

int GenerateNextBorrowID()
{
	int nBorrowID = 1200;
	int nValueCHK = 0;
	string sc = " SELECT borrow_id FROM stock_borrow WHERE borrow_id IS NOT NULL ORDER BY id DESC ";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "borrowID") >= 1)
		{
			nValueCHK = int.Parse(dst.Tables["borrowID"].Rows[0]["borrow_id"].ToString());
			if(nValueCHK >= nBorrowID)
				nBorrowID = nValueCHK + 1;
		}
		else
			nBorrowID += 1;
			
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return 0;
	}
	return nBorrowID;
}
bool DoAddItemToSession()
{
	string code = "";
	if(Request.Form["sValue"] != "" && Request.Form["sValue"] != null)
		code = Request.Form["sValue"].ToString();
	bool bAccept = false;
	bAccept = CheckValidSN(code);
	
	if(bAccept)
	{
		code = m_code;
		int nSession = 0;
		bool IsDuplicate = false;
		if(code != "")
		{
//DEBUG("code = ", code);
			if(Session["slt_count"] == null)
			{
				Session["slt_count"] = 1;
				Session["slt_code1"] = code;
				nSession = (int)Session["slt_count"];
		//DEBUG("code = ", Session["slt_code1"].ToString());
			}
			else
			{
				nSession = (int)Session["slt_count"] + 1;		
				for(int i=1; i<=nSession; i++)
				{	
					if(Session["slt_code"+ i] != null)
					{
						if(code == Session["slt_code"+ i].ToString())
							IsDuplicate = true;
					}
				}
				if(!IsDuplicate)
				{
					Session["slt_count"] = nSession;
					Session["slt_code"+ nSession] = code;
			//		DEBUG("code = ", Session["slt_code"+ nSession].ToString());	
					//for(int i=1; i<=nSession; i++)
					//	DEBUG("code = ", Session["slt_code"+ i].ToString());	
				}
				
		//		DEBUG("nSession=", nSession);
			}
		}
	}
	return true;
}

void DisplayAllProduct()
{
	Response.Write("<br>");
	Response.Write("<form name=frm method=post >");
	Response.Write("<table width=94% align=center cellspacing=0 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th colspan=2 align=left><h4>BORROW ITEMS LIST</th></tr>");
	//Response.Write("<tr><td colspan=2><input type=submit value='Date :' "+ Session["button_style"] +"> "+ DateTime.Now.ToString("dd-MM-yyyy") +" </td></tr>");
	Response.Write("<tr><td colspan=2>Date : "+ DateTime.Now.ToString("dd-MM-yyyy") +" </td></tr>");
	Response.Write("<tr><td colspan=2>Requested by : "+ Session["name"].ToString().ToUpper() +" </td></tr>");
//	Response.Write("<tr><td colspan=2><input type=submit name=cmd value='Requested by :' "+ Session["button_style"] +"> "+ Session["name"].ToString().ToUpper() +" </td></tr>");
	Response.Write("<tr><td colspan=2>");
	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	bool bAlter = false;
	Response.Write("<tr bgcolor=#EEEEE><th>CODE</td><th>SUPP_CODE</td><th align=left>PRO_DESC</td><th>QTY</td><td>&nbsp;</td></tr>");
	DataRow dr;
	int nCount = 0;			
	if(Session["slt_count"] != null)
	{
//DEBUG("slt_count = ", Session["slt_count"].ToString());
		for(int i=1; i<=(int)Session["slt_count"]; i++)
		{
			if(Session["slt_code"+ i] != null)
			{
				Response.Write("<tr");
				if(!bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
				Response.Write(">");
				bAlter = !bAlter;
				
//		DEBUG("code = ", Session["slt_code"+ i].ToString());
				dr = GetProduct(Session["slt_code"+ i].ToString());
				if(dr != null)
				{
					string code = dr["code"].ToString();
					string supp_code = dr["supplier_code"].ToString();
					string name = dr["name"].ToString();
					string qty = dr["stock"].ToString();
					name = StripHTMLtags(name);
					//hidden value
					Response.Write("<input type=hidden name=code"+ i +" value='"+ code +"'>");
					Response.Write("<input type=hidden name=supp_code"+ i +" value='"+ supp_code +"'>");
					Response.Write("<input type=hidden name=name"+ i +" value='"+ name +"'>");

					Response.Write("<td>"+ code +"</td>");
					Response.Write("<td>"+ supp_code +"</td>");
					Response.Write("<td width=50%>"+ name +"</td>");
					Response.Write("<td><select name=qty"+ i +">");
					for(int j=1; j<4; j++)
					{
						Response.Write("<option value="+ j +" ");
						if(qty == j.ToString())
							Response.Write(" selected ");
						Response.Write(">"+ j +"</option>");
					}
					Response.Write("</select></td>");
					Response.Write("<td><a title='delete this item' href='"+ Request.ServerVariables["URL"] +"?dl="+ i +"'><font color=red><b>X</b></font></a></td>");
				}
				Response.Write("</tr>");
			}
			
		}
		Response.Write("<input type=hidden name=tsrows value="+ (int)Session["slt_count"] +">");
		Response.Write("<input type=hidden name=tfrows value="+ (int)Session["slt_count"] +">");
	}

	Response.Write("<tr><td colspan=5><input type=text name=sValue value=''><input type=submit name=cmd value='SEARCH ITEM' "+ Session["button_style"] +"> ");
		Response.Write("\r\n<script");
	Response.Write(">\r\ndocument.frm.sValue.focus();\r\n</script");
	Response.Write(">\r\n");
	
	//Response.Write("<tr><td colspan=5><input type=button value='Select Product From Catalog' "+ Session["button_style"] +" ");
	Response.Write("<br><input type=button value='Select Product From Catalog' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\"window.location=('slt_item.aspx?br=1')\">");
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left colspan=3 valign=TOP>&nbsp;&nbsp; REASON: <textarea name=reason rows=5 cols=60></textarea></td>");
	//Response.Write("<tr><td colspan=3><input type=button value='Select Product From Catalog' "+ Session["button_style"] +" ");
	//Response.Write(" onclick=\"window.location=('slt_item.aspx?br=1')\">");
	Response.Write("<td align=right  valign=bottom colspan=2>");
	Response.Write("<input type=button name=cmd value='Borrow List' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?lt=bl')\" ");
	Response.Write(">");
	Response.Write("<input type=submit name=cmd value='Cancel' "+ Session["button_style"] +">");
	if(Session["slt_count"] != null)
	{
	Response.Write("<input type=submit name=cmd value='Borrow Items From Stock' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"if(!confirm('Processing Borrowing...')){return false;}\" ");
	Response.Write(">");
	}
	Response.Write("</td></tr>");
	
	Response.Write("</table></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
}


bool DoInsertBorrowItems()
{
		
	string qty = "";
	string code = "";
	string tfrows = Request.Form["tfrows"];
	//string name = "";
	string supp_code = "";
	string reason = Request.Form["reason"];
	int nBID = 0;
	if(tfrows != null & tfrows != "")
	{
		nBID = GenerateNextBorrowID();
		Session["bid"] = nBID;
//	DEBUG("nbid = ", nBID);
		for(int i=1; i<=int.Parse(tfrows); i++)
		{
			qty = Request.Form["qty"+ i];
			code = Request.Form["code"+ i];
			supp_code = Request.Form["supp_code"+ i];
			//name = Request.Form["name"+ i];
			//DEBUG("code = ", code);
			if(code != "" && code != null)
			{
			string sc = " SET DATEFORMAT dmy ";
			sc += " INSERT INTO stock_borrow (borrow_id, code, supplier_code, borrower_id, borrow_date, borrow_reason, qty ";
			sc += "  ) ";
			sc += " VALUES( "+ nBID + ", "+ code +", '"+ supp_code +"', "+ Session["card_id"] +", GETDATE(), CONVERT(VARCHAR(512),'"+ reason +"'), "+ qty +" ";

			sc += " ) ";
	///		DEBUG("sc = ", sc);
	//		return false;
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
		}
		
		//clean up all the session values
		for(int i=1; i<=int.Parse(tfrows); i++)
		{
			Session["slt_code"+ i] = null;
			Session["slt_count"] = null;
		}
	}
	return true;
}


DataRow GetProduct(string code)
{
	//if(code == "" && code == null)
	//	return;
	DataSet dscsn = new DataSet();
	string sc = " SELECT TOP 1 c.code, c.supplier_code, c.name ";
	if(g_bRetailVersion)
		sc += ", sq.qty AS stock ";
	else
		sc += ", p.stock ";
	
	sc += " FROM code_relations c JOIN product p ON c.code = p.code ";
	sc += " LEFT OUTER JOIN stock s ON s.product_code = c.code ";
	if(g_bRetailVersion)
		sc += " JOIN stock_qty sq ON sq.code = p.code ";
	sc += " WHERE 1 = 1 ";
	if(TSIsDigit(code))
		sc += " AND c.code = "+ code +" OR s.sn = '"+ code +"' ";
	else
		sc += " AND s.sn = '" + code +"' ";
//DEBUG("sc = ", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dscsn) == 1)
			return dscsn.Tables[0].Rows[0];
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
	}
	return null;

}

bool GetBorrowItem()
{
    if(dst.Tables["borrow"] != null)
        dst.Tables["borrow"].Clear();

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT DISTINCT ISNULL(b.replace_qty,0) AS replace_qty, b.borrow_id, c.name AS borrower, c2.name AS authorize, cr.name AS name, b.* ";
	sc += " , ISNULL((b.qty - b.return_qty - b.replace_qty),0) AS bl_qty ";
	sc += " FROM stock_borrow b JOIN card c ON c.id = b.borrower_id ";
	sc += " JOIN code_relations cr ON cr.code = b.code AND cr.supplier_code = b.supplier_code ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = b.approved_by ";
	sc += " WHERE 1 = 1 ";
	if(Request.QueryString["rp"] == null)
	{
		//if(m_type != "ap" && m_type != "rt")
		if(m_type == "" && m_type == null)
			sc += " AND b.borrower_id = "+ Session["card_id"];
	}
	if(Request.QueryString["bid"] !=null && Request.QueryString["bid"] != "")
		sc += " AND b.borrow_id = "+ Request.QueryString["bid"];

	if(m_type == "ap")
			sc += " AND b.approved = 0 ";
	if(m_type == "rt")
			sc += " AND b.approved = 1 AND (b.return_complete = 0 OR (b.qty - b.return_qty - b.replace_qty) > 0)";
	if(m_type == "bk")
		sc += " AND b.return_complete = 1 AND b.approved = 1 ";
	if(m_search != "")
    {
		if(TSIsDigit(m_search) && m_search.Length < 13)
			sc += " AND (b.borrow_id = "+ m_search +" OR b.code = "+ m_search +" OR b.supplier_code = '"+ m_search +"' )"; 
        else
            sc += " AND (b.supplier_code = '"+ EncodeQuote(m_search) +"' OR b.name = '"+ EncodeQuote(m_search) +"' ) "; 
    }
//	sc += " AND (b.qty - b.return_qty - ISNULL(b.replace_qty,0)) > 0 "; 
	sc += " ORDER BY b.id desc ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "borrow");
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	if(Request.QueryString["lt"] == "ap")
	{
		if(!SecurityCheck("stockman"))
			return false;
	}

	Response.Write(BorrowList());
	return true;
}

string BorrowList()
{

	StringBuilder sb = new StringBuilder();
	DataRow dr;
		
	sb.Append("<br><center><h4>");
	if(Request.QueryString["lt"] == "ap" )
		sb.Append("APPROVE");
	if(m_type == "rt" )
		sb.Append("RETURN");
	if(m_type == "bk" )
		sb.Append("COMPLETE RETURNED ");
	sb.Append(" BORROW LIST</h4>");
	sb.Append("</center>");
	sb.Append("<form name=frm method=post >");	
	int cols = 12;
	//if(m_type == "ap" || m_type == "rt")
	if(m_type != "")
		cols = 13;
	if(Request.QueryString["pr"] == "lt")
		sb.Append("<body onload=window.print()>");
	
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(dst.Tables["borrow"] != null)
		rows = dst.Tables["borrow"].Rows.Count;
	m_cPI.TotalRows = rows;
	
	m_cPI.PageSize = 35;
	if(Request.QueryString["rp"] == "all")
		m_cPI.PageSize = rows;
	
	m_cPI.URI = "?lt="+ m_type;
	
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
		
	sb.Append("<table align=center width=98% cellspacing=0 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	//if(m_type == "ap" || m_type == "rt" )
	if(m_type != "" )
	{
		sb.Append("<tr><td colspan="+ cols +">SEARCH BORROW LIST (by Code#, Supplier Code, Borrow# or Description): <input type=text name=search value="+ Request.Form["search"] +"><input type=submit name=cmd value=' SEARCH '"+ Session["button_style"] +">");
		sb.Append("<input type=button value='SHOW ALL' "+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?lt=ap&r="+ DateTime.Now.ToOADate() +"')\"");
		sb.Append("</td></tr>");
	}
	sb.Append("<tr><td colspan="+ cols +">Requested by: "+ Session["name"] +"</td></tr>");
	sb.Append("<tr><td colspan="+ cols +">Request Date: "+ DateTime.Now.ToString("dd.MMM.yyyy") +"</td></tr>");
	sb.Append("<tr><th colspan="+ cols +" align=left><hr size=1 width=100%></th></tr>");
	sb.Append("<tr><td colspan=7>"+ sPageIndex +"</td>");
	if(Request.QueryString["pr"] != "lt")
	{
	    sb.Append("<td align=right colspan=6>Select Options: <select name=br_option onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?lt='+ document.frm.br_option.value)\" >");
	    sb.Append("<option value='rt'");
	    if(m_type == "rt")
		    sb.Append(" selected ");
	    sb.Append(">Returning List</option>");
	    sb.Append("<option value='ap'");
	    if(m_type == "ap")
		    sb.Append(" selected ");
	    sb.Append(">Waiting for Approve List</option>");
	    sb.Append("<option value='bk'");
	    if(m_type == "bk")
		    sb.Append(" selected ");
	    sb.Append(">Returned Complete List</option>");
	    sb.Append("</select>");
	}
	sb.Append("</tr>");
	sb.Append("<tr bgcolor=#EEEEE>");
	sb.Append("<th align=left >BORROW#</th>");
	if(g_bRetailVersion)
		sb.Append("<th align=left >CODE#</th>");

	sb.Append("<th align=left >SUPPLIER CODE#</th>");
	sb.Append("<th align=left>DESCRIPTION</th>");
	sb.Append("<th align=left>QTY</th>");
	//sb.Append("<th align=left>RETURN_QTY</th>");
	sb.Append("<th align=left>BORROWER</th>");
	sb.Append("<th align=left>BORROW_DATE</th>");
	sb.Append("<th align=left>APPR_BY</th>");
	sb.Append("<th align=left>APPROVED_QTY</th>");
	sb.Append("<th align=left>RETURNED_QTY</th>");
	sb.Append("<th align=left>REPLACED_QTY</th>");
	if(m_type == "ap" || m_type == "rt")
	{
		//if(!SecurityCheck("stockman"))
		//	return false;
		sb.Append("<th align=left>APPROVED</th>");
		sb.Append("<th align=left>APP/RET QTY</th>");
	}
	else
        sb.Append("<th colspan=2>&nbsp;</th>");
//	sb.Append("<th align=left>ACTION</th>");
	sb.Append("</tr>");
	sb.Append("<tr><th colspan="+ cols +" align=left><hr size=1 width=100%></th></tr>");
	bool bAlter = false;
	string reason = "";
	//for(int i=0; i<dst.Tables["borrow"].Rows.Count; i++)
	for(; i<rows && i<end; i++)
	{
		dr = dst.Tables["borrow"].Rows[i];
		string bl_qty = dr["bl_qty"].ToString();
		string bid = dr["borrow_id"].ToString();
		string id = dr["id"].ToString();
		string code = dr["code"].ToString();
		string qty = dr["qty"].ToString();
		string r_qty = dr["return_qty"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string borrower = dr["borrower"].ToString();
		string approved = dr["approved"].ToString();
		string authorize = dr["authorize"].ToString();
		string return_date = dr["return_date"].ToString();
		string borrow_date = dr["borrow_date"].ToString();
		string replace_qty = dr["replace_qty"].ToString();
//DEBUG("reps =", replace_qty);
		string app_qty = dr["approved_qty"].ToString();
		reason = dr["borrow_reason"].ToString();
		bool bcomplete = bool.Parse(dr["return_complete"].ToString());
		sb.Append("<tr");
		if(bAlter)
			sb.Append(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		sb.Append(">");
		//sb.Append("<td><a title='Print this borrow item' href='"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&bid="+ bid +"&pr=lt' class=o target=new>"+ bid +"</a></td>");
		sb.Append("<td><a title='Print this borrow item' href=\"javascript:list_window=window.open('"+ Request.ServerVariables["URL"] +"?pr=lt&bid="+ bid +"&rp=1', '', 'resizable=1, scrollbars=1'); list_window.focus();\" class=o>"+ bid +"</a></td>");
		
		//hidden value
		sb.Append("<input type=hidden name=actual_rqty"+ i +" value='"+ r_qty +"'>");
		sb.Append("<input type=hidden name=borrow_qty"+ i +" value='"+ qty +"'>");
		if(g_bRetailVersion)
		sb.Append("<td>"+ code +"</td>");
		sb.Append("<td>"+ supplier_code +"</td>");
		sb.Append("<td><a title='view product details' href=\"javascript:product_window=window.open('p.aspx?"+ code +"', '', 'resizable=1, scrollbars=1'); product_window.focus()\" class=o>"+ name +"</a></td>");
		sb.Append("<td>"+ qty +"</td>");
		
		sb.Append("<td>"+ borrower +"</td>");
		sb.Append("<td>"+ (DateTime.Parse(borrow_date)).ToString("dd-MM-yyyy") +"</td>");
		sb.Append("<td>"+ authorize +"</td>");
		sb.Append("<td align=center>"+ app_qty +"</td>");
		sb.Append("<td align=center>"+ r_qty +"</td>");
	//	sb.Append("<td>"+ returned +"</td>");
		sb.Append("<td align=center>"+ replace_qty +"</td>");
		if(Request.QueryString["pr"] != "lt")
		{
		sb.Append("<td><input type=checkbox name=check"+i+" ");
		if(approved == "1")
			sb.Append(" checked disabled ");
		
		sb.Append(">");
		sb.Append("</td>");
		
		sb.Append("<td>");
	//	DEBUG("blqyt =", bl_qty);
//		if(approved == "1" )
		{

			sb.Append("<select name=rqty"+ i +" ");
			if(bcomplete)
				sb.Append(" disabled ");
			sb.Append(">");
						
			for(int j=0; j<=int.Parse(bl_qty); j++)
			{
				sb.Append("<option value=");
				if(int.Parse(bl_qty) > 0) 
					sb.Append("'"+ j +"'");
				else
					sb.Append("'"+ bl_qty +"'");
				//sb.Append("<option value='"+ j +"'");
				if(int.Parse(r_qty) == j)
					sb.Append(" selected ");
				
				sb.Append(" >"+ j +"</option>");
			}
			//sb.Append("</select><input type=submit name=cmd value='RETURN' "+ Session["button_style"] +"");
			sb.Append("</select>");
			
		}
		
		sb.Append("</td>");
//		sb.Append("<td>&nbsp;&nbsp;<a title='Insert SN#' href='"+ Request.ServerVariables["URL"] +"?sn="+ id +"' class=o>SN</a>");
		}
		
		//Response.Write("<td><a title='input sn' href='"+ Request.ServerVariables["URL"] +"' class=o>SN</a></td>");
		sb.Append("</tr>");
		sb.Append("<input type=hidden name=id"+ i +" value="+ id +">");
	}
	sb.Append("<input type=hidden name=rows value='"+ dst.Tables["borrow"].Rows.Count +"'>");
	if(Request.QueryString["pr"] != "lt")
	{
		sb.Append("<tr><th colspan="+ cols +" align=right><br>");
		if(m_type == "rt")
		{
			sb.Append("<input type=button value='Go To Approval List' "+ Session["button_style"] +" ");
			sb.Append(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?lt=ap&r="+ DateTime.Now.ToOADate() +"')\">");
		}
		if(m_type == "ap")
		{
			sb.Append("<input type=button value='Go To Return List' "+ Session["button_style"] +" ");
			sb.Append(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?lt=rt&r="+ DateTime.Now.ToOADate() +"')\">");
		}
		sb.Append("<input type=button value='New Borrow' "+ Session["button_style"] +" ");
		sb.Append(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");
		sb.Append("<input type=button value='Print All Borrow List' "+ Session["button_style"] +" ");
		sb.Append(" onclick=\"javascript:list_window=window.open('"+ Request.ServerVariables["URL"] +"?pr=lt&rp=all&r="+ DateTime.Now.ToOADate() +"', '', 'resizable=1,scrollbars=1'); list_window.focus();\" ></td></tr>");
	}
	if(m_type == "ap")
	{
		//sb.Append("<tr><th colspan="+ cols +" align=right>CHECK ALL : <input type=checkbox name=allbox value='Select All' onclick='CheckAll();'>&nbsp;&nbsp;&nbsp;");
		sb.Append("&nbsp;&nbsp;CHECK ALL : <input type=checkbox name=allbox value='Select All' onclick='CheckAll();'>&nbsp;&nbsp;&nbsp;");
		sb.Append("<input type=submit name=cmd value='Approve' "+ Session["button_style"] +" Onclick=\"if(!confirm('Process Approve Borrowing...Stock Quantity will be updated')){return false;}\">");
		sb.Append("</td></tr>");
	}
	else if(m_type == "rt")
	{
		sb.Append("<input type=submit name=cmd value='Return Borrowed Item' "+ Session["button_style"] +" Onclick=\"if(!confirm('Process Return Items...Stock Quantity will be updated')){return false;}\">");
		sb.Append("</td></tr>");
	}
	else
	{
		sb.Append("<tr><td><br><br>");
//		sb.Append("Authorize By: </td></tr>");
	}
//	sb.Append("<tr><th colspan="+ cols +" ><br>"+ reason +" </td></tr>");
	sb.Append("</table>");
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.frm.elements.length;i++) 
		{
			var e = document.frm.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox') )
			{
				e.checked = document.frm.allbox.checked;
			}
		}
	}
	";
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	return sb.ToString();
	

}

</script>

<asp:Label id=LFooter runat=server/>
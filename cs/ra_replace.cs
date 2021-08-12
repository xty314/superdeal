<!-- #include file="page_index.cs" -->

<script runat=server>

string m_search = "";
string m_cust_query = "";
string m_supp_query = "";
string m_sdFrom = "";
string m_sdTo = "";
string m_id = "";
string m_command = "";
string m_rma_id = "";
string m_supplier_id = "";
string m_del_id = "";

//------current item to be processed
string mc_sn = "";
string mc_code = "";
string mc_supplier_code = "";
string mc_supplier_id = "";
string mc_customer_id = "";
string mc_po_id = "";
string mc_id = "";
string mc_stock = "";
string mc_name = "";
string mc_repair_id = "";
string mc_status = "";
string mc_condition = "";
string mc_old_sn = "";

string m_rp_code = "";
string m_rp_supplier_code = "";
string m_rp_name = "";
string m_rp_sn = "";

bool m_bFoundSN = false;
bool m_bValid = false;
bool m_bfrom_rma_stock = false;
bool m_bReplaced = false;
string[] m_EachMonth = new string[13];

bool m_bNoStockBorrow = false;

DataSet dst = new DataSet();
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
		//monthly name
	m_EachMonth[0] = "JAN";
	m_EachMonth[1] = "FEB";
	m_EachMonth[2] = "MAR";
	m_EachMonth[3] = "APR";
	m_EachMonth[4] = "MAY";
	m_EachMonth[5] = "JUN";
	m_EachMonth[6] = "JUL";
	m_EachMonth[7] = "AUG";
	m_EachMonth[8] = "SEP";
	m_EachMonth[9] = "OCT";
	m_EachMonth[10] = "NOV";
	m_EachMonth[11] = "DEC";
	//----
	
	m_bNoStockBorrow = MyBooleanParse(GetSiteSettings("no_stock_borrow_action", "false", true));
	GetQueryString();
	InitializeData();
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		if(DoDelLastReplace())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "");
			Response.Write("&id="+ Request.QueryString["del"] +"&ra=rp");
			Response.Write("\">");
			return;
		}
	}
	if(m_command.ToLower() == "search item" || Request.Form["rp_sn"] != null && Request.Form["rp_sn"] != ""
		|| Request.Form["rp_code"] != null && Request.Form["rp_code"] != ""
		|| Request.Form["rp_supplier_code"] != null && Request.Form["rp_supplier_code"] != "")
	{
		if(!CheckStockBorrowSN())
			return;
	}
	else
	{
		    Session["ss_rpsn"] = null;
			Session["ss_rpcode"] = null;
			Session["ss_rpsupplier_code"] = null;
			Session["ss_rpname"] = null;
	}
	if(m_command.ToLower() == "replace item")
	{
		if(!m_bNoStockBorrow)
		{
			if(!CheckValidateStock())
			{
				Response.Write("<br><center><h4><font color=red>The Replaced Item is Either Invalid or You Do Not Have Item to Replace the Faulty Item</font>");
				Response.Write("<h4>Please Go to Stock Borrow, here is the link <a title='borrow stock' href='stk_borrow.aspx?r="+ DateTime.Now.ToOADate() +"' class=o>Stock Borrow</a>");
				Response.Write("<h4>Or Go to your Borrow List, here is the link <a title='borrow stock list' href='stk_borrow.aspx?lt=bl&r="+ DateTime.Now.ToOADate() +"' class=o>Stock Borrow List</a>");
				Response.Write("<h4>Or Go to Back to the Replace Process, here is the link <a title='ra replacement' href=\"javascript:window.history.back()\" class=o><< Back</a>");
				return;
			}
		}
		if(DoReplaceItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "");
			Response.Write("&id="+ m_id +"&ra=rp");
			Response.Write("\">");
			return;
		}
	}
	GetRepairDetails();


}

bool DoDelLastReplace()
{
	string sc = " UPDATE stock SET status = 2 WHERE sn = (SELECT DISTINCT sn FROM ra_replaced WHERE ra_id = "+ Request.QueryString["del"] +") ";
	sc += AddSerialLogString(""+ Request.QueryString["sn"] +"", "Delete Replaced Item: "+ Request.QueryString["nm"] , "", "", ""+Request.QueryString["del"]+"", "");
	sc += "DELETE FROM ra_replaced WHERE ra_id = "+ Request.QueryString["del"];
	sc += " UPDATE repair SET status = 3, replaced = 0 ";
	sc += " WHERE id = "+ Request.QueryString["del"];
	sc += AddRepairLogString("Delete Replaced Item", "", "",""+ Request.QueryString["del"] +"","", "");
	sc += " UPDATE stock_borrow SET replace_qty = replace_qty - 1 WHERE code  = "+ Request.QueryString["code"] +" AND approved = 1 AND borrower_id = "+ Session["card_id"] +"";
	sc += " DELETE FROM rma_stock WHERE repair_id = "+ Request.QueryString["del"] +" AND code = "+ Request.QueryString["code"] +" ";
//	DEBUG(" sc +", sc);
//	return false;
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

bool DoReplaceItem()
{
    string rp_code = Request.Form["rp_code"];
    string rp_supplier_code = Request.Form["rp_supplier_code"];
    string rp_sn = Request.Form["rp_sn"];

	string sc = " BEGIN TRANSACTION SET DATEFORMAT dmy ";
	sc += " INSERT INTO ra_replaced (ra_id, code, sn, replace, description, rs_date, note, qty, staff, invoice_number, supp_code )";
	sc += " VALUES("+ Request.Form["hide_id"] +", "+ Request.Form["rp_code"] +", '"+ Request.Form["rp_sn"] +"', 1 ";
	sc += " , '"+ EncodeQuote(StripHTMLtags(Request.Form["rp_prod_desc"])) +"', GETDATE(), '"+ EncodeQuote(StripHTMLtags(Request.Form["rp_note"])) +"' ";
	sc += " , 1 , "+ Session["card_id"] +", '"+ Request.Form["hide_invoice"] +"', '"+ Request.Form["rp_supplier_code"] +"' ";
	sc += " )";
	
	sc += " UPDATE repair SET status = 4, replaced = 1 ";
	sc += " , note = '"+ EncodeQuote(StripHTMLtags(Request.Form["rp_note"])) +"' ";
	sc += " WHERE id = '"+ Request.Form["hide_id"] +"'";
	sc += AddSerialLogString(""+ Request.Form["rp_sn"] +"", "RA Replacement with: "+ Request.Form["rp_prod_desc"] , "", ""+ Request.Form["hide_invocie"]+"", ""+Request.Form["hide_ra_id"]+"", "");
	sc += AddRepairLogString("Replace Item with new sn:"+ Request.Form["rp_sn"]+ ", "+ Request.Form["rp_prod_desc"] +" ", ""+ Request.Form["hide_invoice"] +"", ""+ Request.Form["rp_code"] +"",  ""+ Request.Form["hide_ra_id"] +"", ""+ Request.Form["old_sn"] +"","");
	if(Request.Form["rp_sn"] != null && Request.Form["rp_sn"] != "")
		sc += " UPDATE stock SET status = 1 WHERE sn = '"+ Request.Form["rp_sn"] +"' ";

	if(Request.Form["old_sn"] != null && Request.Form["old_sn"] != "")
	{
		sc += " UPDATE sales_serial ";
		sc += " SET sn = '"+ Request.Form["rp_sn"] +"' ";
		sc += " , code = '"+ Request.Form["rp_code"] +"' ";
		sc += " WHERE id = (SELECT TOP 1 id FROM sales_serial WHERE sn = '"+ Request.Form["old_sn"] +"' )";

		sc += " UPDATE stock SET status = 4 WHERE sn = '" + Request.Form["old_sn"] +"' ";
	}
	
	//update borrow item stock
	if(Session["slt_borrow_id"] != null && Session["slt_borrow_id"] != "")
    {
	    sc += " UPDATE stock_borrow SET replace_qty = replace_qty + 1 WHERE borrower_id = "+ Session["card_id"] +" AND code = "+ Request.Form["rp_code"] +" AND borrow_id = "+ Session["slt_borrow_id"] +" ";
        sc += AddSerialLogString(""+ Request.Form["rp_sn"] +"", "Item from RMA Stock replaced to customer: "+ Request.Form["rp_prod_desc"] +": RMA Stock id : "+ Session["slt_rborrow_id"], "", "", "", "");
		sc += AddRepairLogString("RMA stock item replaced to customer sn:"+ Request.Form["rp_sn"]+ " , "+ Request.Form["rp_prod_desc"] +": rma_stock id = "+ Session["slt_rborrow_id"] +"", "", ""+ Request.Form["rp_code"] +"",  "", ""+ Request.Form["old_sn"] +"","");
		sc += " UPDATE rma_stock SET item_location=8 WHERE id = "+ Session["slt_rborrow_id"] +"";	
    }
    else
    {
        if(rp_sn != "" || rp_code != "" || rp_supplier_code != "")
        {
            sc += " UPDATE stock_borrow SET replace_qty = replace_qty + 1 WHERE borrower_id = "+ Session["card_id"] +" ";
            if(rp_code != "" && TSIsDigit(rp_code) && rp_code.Length < 13)
                sc += " AND code = "+ rp_code +" "; //AND borrow_id = "+ Session["slt_borrow_id"] +" ";
            if(rp_supplier_code != "")
                sc += " AND supplier_code = '"+ rp_supplier_code +"' "; 
            if(rp_sn != "")
                sc += " AND sn = '"+ EncodeQuote(rp_sn) +"' ";
        }
    }
	//set rma_stock item is replaced
	if(m_bfrom_rma_stock)
		sc += " UPDATE rma_stock SET item_location=3 WHERE sn ='"+ Request.Form["rp_sn"] +"'";		
	
	sc += " INSERT INTO rma_stock (repair_id, supp_rma_id, code, supplier_code, prod_name, stock_date, stock_by, sn , status, item_location) ";
	sc += " VALUES( '"+ Request.Form["hide_id"] +"', ''";
	if(Request.Form["old_code"] != null && Request.Form["old_code"] != "")
		if(TSIsDigit(Request.Form["old_code"].ToString()))
			sc += " , "+ Request.Form["old_code"] +"";
		else
			sc += " , "+ Request.Form["rp_code"] +"";
	else
		sc += " , "+ Request.Form["rp_code"] +"";
	if(Request.Form["old_supp_code"] != null && Request.Form["old_supp_code"] != "")
		sc += ", '"+ EncodeQuote(StripHTMLtags(Request.Form["old_supp_code"])) +"' ";
	else
		sc += ", '"+ EncodeQuote(StripHTMLtags(Request.Form["rp_supplier_code"])) +"' ";
	sc += " , '"+ Request.Form["old_name"] +"'";
	sc += " , GETDATE(), "+ Session["card_id"] +" ";
	sc += " , '"+ Request.Form["old_sn"] +"' ";
	sc += " , 2 ";
	sc += " , 5 ";
	sc += "  )";
    sc += " COMMIT ";
//DEBUG(" sc + ", sc);
//return false;
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
	SessionValueCleanup();
	return true;
}


void SessionValueCleanup()
{
	Session["ss_rpcode"] = null;
	Session["ss_rpsupplier_code"] = null;
	Session["ss_rpname"] = null;
	Session["ss_rpsn"] = null;
	Session["slt_code"] = null;
	Session["slt_supplier_code"] = null;
	Session["slt_name"] = null;
}
bool GetRepairDetails()
{
	if(m_id == "" || m_id == null)
	{
		Response.Write("<center><h4>No Record Found!!</h4>");
		Response.Write("<a title='back to repair' href='techr.aspx?r="+ DateTime.Now.ToOADate() +"&s=2&op=0' class=o><< Back To Repair</a>"); 
		return false;
	}
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT  r.replaced, r.id, r.ra_number,r.serial_number, r.invoice_number, r.note, r.fault_desc, r.repair_date, r.repair_finish_date, r.technician, r.code, r.supplier_code, r.supplier_id ";
	sc += ", r.prod_desc, c.name, c.company, c.address1, c.address2, c.city, c.phone, c.fax, c.email ";
	sc += ", e.name AS status, r.customer_id, r.for_supp_ra ";
	sc += " FROM repair r LEFT OUTER JOIN card c ON c.id = r.customer_id ";
	sc += " LEFT OUTER JOIN enum e ON e.id = r.status AND e.class='rma_status' ";
	
	sc += " WHERE r.id = "+ m_id +" ";

//DEBUG("sc = ", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "ra_details");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	sc = " SELECT  rp.* , c.name  ";
	sc += " FROM ra_replaced rp LEFT OUTER JOIN card c ON c.id = rp.staff ";
	sc += " WHERE rp.ra_id = "+ m_id +" ";

//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "ra_replaced");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}


	if(rows <=0 )
	{
		Response.Write("<br><center><h4><font color=red>NO RMA for this Item</h4></font>");
		Response.Write("<a title='back to repair task' href='techr.aspx?s=2&op=0&r="+ DateTime.Now.ToOADate() +"&id="+ m_id +"' class=o><< BACK</a>");
		return false;
	}

	Response.Write("<form name=frm method=post>");
	Response.Write("<center><h4>Replace Faulty Item</h4>");
	Response.Write("<table width=96% align=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	string sn = dst.Tables["ra_details"].Rows[0]["serial_number"].ToString();
	string code = dst.Tables["ra_details"].Rows[0]["code"].ToString();
	string supplier_code = dst.Tables["ra_details"].Rows[0]["supplier_code"].ToString();
	string supplier_id = dst.Tables["ra_details"].Rows[0]["supplier_id"].ToString();
	string customer_id = dst.Tables["ra_details"].Rows[0]["customer_id"].ToString();
//	string po_id = dst.Tables["ra_details"].Rows[0]["po_id"].ToString();
	string id = dst.Tables["ra_details"].Rows[0]["id"].ToString();
	string for_stock = dst.Tables["ra_details"].Rows[0]["for_supp_ra"].ToString();
	string name = dst.Tables["ra_details"].Rows[0]["name"].ToString();
	if(!g_bRetailVersion)
		name = dst.Tables["ra_details"].Rows[0]["company"].ToString();

	string status = dst.Tables["ra_details"].Rows[0]["status"].ToString();
	
	string addr1 = dst.Tables["ra_details"].Rows[0]["address1"].ToString();
	string addr2 = dst.Tables["ra_details"].Rows[0]["address2"].ToString();
	string city = dst.Tables["ra_details"].Rows[0]["city"].ToString();
	string phone = dst.Tables["ra_details"].Rows[0]["phone"].ToString();
	string fax = dst.Tables["ra_details"].Rows[0]["fax"].ToString();
	string email = dst.Tables["ra_details"].Rows[0]["email"].ToString();
	string fault = dst.Tables["ra_details"].Rows[0]["fault_desc"].ToString();
	string prod_desc = dst.Tables["ra_details"].Rows[0]["prod_desc"].ToString();
	string repair_note = dst.Tables["ra_details"].Rows[0]["note"].ToString();
	string repair_date = dst.Tables["ra_details"].Rows[0]["repair_date"].ToString();
	string invoice_number = dst.Tables["ra_details"].Rows[0]["invoice_number"].ToString();
	string ra_number = dst.Tables["ra_details"].Rows[0]["ra_number"].ToString();

	//hide value
	Response.Write("<input type=hidden name=hide_id value='"+ id +"'> ");
    Response.Write("<input type=hidden name=hide_ra_id value='"+ ra_number +"'> ");
	Response.Write("<input type=hidden name=old_sn value='"+ sn +"'> ");
	Response.Write("<input type=hidden name=old_code value='"+ code +"'> ");
	Response.Write("<input type=hidden name=old_supp_code value='"+ supplier_code +"'> ");
	Response.Write("<input type=hidden name=old_name value='"+ prod_desc +"'> ");
	Response.Write("<input type=hidden name=hide_invoice value='"+ invoice_number +"'> ");
	
	Response.Write("<tr><td>");
	Response.Write("<table align=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr align=left><th>CUSTOMER RMA#: </td><th><a title='view rma form' href='repair.aspx?print=form&ty=1&r="+ DateTime.Now.ToOADate() +"&ra="+ HttpUtility.UrlEncode(ra_number) +"' class=o target=new>"+ ra_number +"</a></td></tr>");
	Response.Write("<tr><td>Customer Details:</td><td>");
	Response.Write("<a title='view details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
	Response.Write("id=" + customer_id + "','', ' width=350,height=350'); viewcard_window.focus()\" class=o >");
	Response.Write(""+ name +"</a></td></tr>");
	Response.Write("<tr><td>Address:</td><td>"+ addr1 +"</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>"+ addr2 +"</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>"+ city +"</td></tr>");
	Response.Write("<tr><td>Phone:</td><td>"+ phone +"</td></tr>");
	Response.Write("<tr><td>Fax:</td><td>"+ fax +"</td></tr>");
	Response.Write("<tr><td>Email:</td><td>"+ email +"</td></tr>");
	
	Response.Write("</table>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>");
	Response.Write("<table width=75% align=center cellspacing=2 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td align=left colspan=6><b>Faulty Product Details:</td></tr>");
	Response.Write("<tr align=left bgcolor=#DDD844><th>STATUS</th><th>SN#</th><th>PROD_CODE</th><th>SUPP_CODE</th><th>PROD_DESC</th><th>FAULT_DESC</th><th>REPAIR_DESC</th></tr>");
	Response.Write("<tr><td>"+ status.ToUpper() +"</td><td>"+ sn.ToUpper() +"</td><td>"+ code +"</td>");
	Response.Write("<td>"+ supplier_code +"</td>");
	Response.Write("<td><a title='view product details' href='p.aspx?"+ code +"' class=o target=new>"+ prod_desc +"</a></td>");
	Response.Write("<td><textarea rows=5 cols=30>"+ fault +"</textarea></td>");
	Response.Write("<td><textarea rows=5 cols=30>"+ repair_note +"</textarea></td>");
	
	if(Session["slt_code"] != null && Session["slt_code"] != "")
	{
		m_rp_code = Session["slt_code"].ToString();
		m_rp_supplier_code = Session["slt_supplier_code"].ToString();
		if(Session["slt_name"] != null && Session["slt_name"] != "")
			m_rp_name = Session["slt_name"].ToString();
		if(!m_bFoundSN)
		{
		Session["ss_rpcode"] = Session["slt_code"];
		Session["ss_rpsupplier_code"] = Session["slt_supplier_code"];
		Session["ss_rpname"] = Session["slt_name"];
		}
	}

Session["ss_rpsn"] = Request.Form["rp_sn"];
	if(m_rp_name != "")
		m_rp_name = StripHTMLtags(m_rp_name);
	if(Session["ss_rpname"] != "" && Session["ss_rpname"] != null)
		Session["ss_rpname"] = StripHTMLtags(Session["ss_rpname"].ToString());
//	if(Session["ss_rpsn"] != null && Session["ss_rpsn"] != "")
//	{
//		m_rp_sn = Session["ss_rpsn"].ToString();
//		m_rp_code = Session["ss_rpcode"].ToString();
//		m_rp_supplier_code = Session["ss_rpsupplier_code"].ToString();
//		m_rp_name = Session["ss_rpname"].ToString();
//	}

	string uri = Request.ServerVariables["URL"] +"?id="+ id +"&ra=rp";
	Response.Write("</tr>");	
			
	Response.Write("</table>");
	Response.Write("</td></tr>");
	if(dst.Tables["ra_replaced"].Rows.Count >0)
	{
		//display replaced item
		Response.Write("<tr><td>");
		Response.Write("<table align=center cellspacing=1 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><th align=left colspan=7>Replaced Item Details:</td></tr>");
		Response.Write("<tr align=left bgcolor=#DDD844><th>SN#</th><th>PROD_CODE#</th><th>SUPP_CODE#</th><th>PROD_DESC#</th><th>STAFF#</th><th>REPLACE DATE#</th><th>NOTE#</th>");
		Response.Write("<th>ACTION</th></tr>");
				
		Response.Write("<tr><td>"+ (dst.Tables["ra_replaced"].Rows[0]["sn"].ToString()).ToUpper() +"</td>");
		Response.Write("<td>"+ dst.Tables["ra_replaced"].Rows[0]["code"] +"</td>");
		Response.Write("<td>"+ dst.Tables["ra_replaced"].Rows[0]["supp_code"] +"</td>");
		Response.Write("<td>"+ dst.Tables["ra_replaced"].Rows[0]["description"] +"</td>");
		Response.Write("<td>"+ dst.Tables["ra_replaced"].Rows[0]["name"] +"</td>");
		Response.Write("<td>"+ dst.Tables["ra_replaced"].Rows[0]["rs_date"] +"</td>");
		Response.Write("<td>"+ dst.Tables["ra_replaced"].Rows[0]["note"] +"</td>");
		Response.Write("<td><a title='delete replaced item' href='"+ Request.ServerVariables["URL"] +"?del="+ m_id +"");
		Response.Write("&sn="+ HttpUtility.UrlEncode(dst.Tables["ra_replaced"].Rows[0]["sn"].ToString()) +"");
		Response.Write("&nm="+ HttpUtility.UrlEncode(dst.Tables["ra_replaced"].Rows[0]["description"].ToString()) +""); 
		Response.Write("&code="+ dst.Tables["ra_replaced"].Rows[0]["code"] +"");
		Response.Write("' class=o><font color=red><b>X</a></b>");
		Response.Write("</td>");
		Response.Write("</tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");
		
		Response.Write("<tr align=center><td colspan=7><br>");
		Response.Write("<input type=button value='<< Back to Repair'"+ Session["button_style"] +" onclick=\"window.location=('techr.aspx?s=2&op=0&r="+ DateTime.Now.ToOADate() +"&id="+ id +"')\">");
		Response.Write("<input type=button value='Send Faulty Item To Supplier >> '"+ Session["button_style"] +" onclick=\"window.location=('ra_supplier.aspx?r="+ DateTime.Now.ToOADate() +"&id="+ id +"')\">");
		Response.Write("<input type=button value='Input Shipping Ticket >> '"+ Session["button_style"] +" onclick=\"window.location=('ra_freight.aspx?r="+ DateTime.Now.ToOADate() +"&rid="+ HttpUtility.UrlEncode(ra_number) +"&ty=1')\">");
		Response.Write("</td></tr><br><br>");
		Response.Write("</table>");
		Response.Write("</form><br><br><br><br><br><br>");
		return false;
	}
	//process replace item
	Response.Write("<tr><td colspan=2>");
	Response.Write("<table  align=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th align=left colspan=2>Process Replace Item Details:</td></tr>");
	Response.Write("<tr><td>SN#</td><td><input type=text size=40 name=rp_sn value='"+ Session["ss_rpsn"] +"'>");
	Response.Write("<input type=submit name=cmd value='SEARCH ITEM' "+ Session["button_style"] +" >");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><font color=red>PROD_CODE#</td><td><input size=30 type=text name=rp_code value='"+ Session["ss_rpcode"] +"'>");
	Response.Write("<a title='Get From Borrow Stock' href='slt_item.aspx?r="+ DateTime.Now.ToOADate() +"&uri="+ HttpUtility.UrlEncode(uri) +"' class=o>...</a>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><font color=red>SUPP_CODE#</td><td><input size=50 type=text name=rp_supplier_code value='"+ Session["ss_rpsupplier_code"] +"'></td></tr>");
	Response.Write("<tr><td><font color=red>PRODUCT DESC#</td><td><input size=85 type=text name=rp_prod_desc value='"+ Session["ss_rpname"] +"'></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>REPLACE NOTE#</td><td><textarea rows=5 cols=60 name=rp_note>"+ repair_note +"</textarea></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=2 align=right>");

	Response.Write("<input type=button value='<< Back to Repair'"+ Session["button_style"] +" onclick=\"window.location=('techr.aspx?s=2&op=0&r="+ DateTime.Now.ToOADate() +"&id="+ id +"')\">");
	//if(dst.Tables["ra_details"].Rows[0]["replaced"].ToString() != "1")
	//Response.Write("<input type=submit name=cmd value='Replace Item'"+ Session["button_style"] +" onclick=\"if(document.frm.rp_code.value=='' || document.frm.rp_supplier_code.value=='' ||document.frm.rp_prod_desc.value==''){window.alert('Please Enter All Required Fields'); return false;}if(!confirm('Process Replacement...')){return false;}\">");
	Response.Write("<input type=submit name=cmd value='Replace Item'"+ Session["button_style"] +" onclick=\"return checkvalid()\">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	
	Response.Write("<script language=javascript>");
	Response.Write("<!-- hide old browser");
	string sjava = @"
		function checkvalid()
	{
		var bAllow = true;
		if(document.frm.rp_code.value=='' || document.frm.rp_supplier_code.value=='' || document.frm.rp_prod_desc.value=='')
		{
			window.alert('Please Enter All Required Fields');
			bAllow = false;
		}
		if(!IsNum(document.frm.rp_code.value))
		{
			window.alert('Invalid Produc Code, Only Numberic');
			document.frm.rp_code.focus();
			document.frm.rp_code.select();
			bAllow = false;
		}
		if(!bAllow)
			return false;
		if(bAllow){
			if(!confirm('Process Replacement...'))
				return false;
			else
				return true;
		}
			
	}
	function IsNum(sText)
	{
	   var ValidChars = '0123456789';
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
	Response.Write("-->");
	Response.Write(sjava);
	Response.Write("</script");
	Response.Write(">");
Response.Write("</form><br><br><br><br><br><br>");
	return true;
}

bool CheckValidateStock()
{
	bool bValid = false;
	string sc = "";
	
	if(TSIsDigit(Request.Form["rp_code"].ToString()))
	{	
		sc = " SELECT DISTINCT code FROM stock_borrow ";
		sc += " WHERE code = "+ Request.Form["rp_code"];
		sc += " AND approved = 1 ";
		sc += " AND (qty - return_qty - ISNULL(replace_qty,0) ) > 0 ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(dst, "stock_valid") >=1)
				bValid = true;
			
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}

		if(dst.Tables["stock_valid"].Rows.Count <=0 )
		{
			sc = " SELECT DISTINCT code FROM rma_stock ";
			sc += " WHERE code = "+ Request.Form["rp_code"];
		
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				if(myAdapter.Fill(dst, "stock_valid") >=1)
				{
					bValid = true;
					m_bfrom_rma_stock = true;
				}
				
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
			
		}
	}
	m_bValid = bValid;
	return bValid;
}
bool CheckStockBorrowSN()
{
	string rp_sn = Request.Form["rp_sn"];
	string rp_code = Request.Form["rp_code"];
	string rp_supplier_code = Request.Form["rp_supplier_code"];
	string sc = "";
	if(m_bNoStockBorrow)
	{
		if(rp_sn != "" || rp_code != "" || rp_supplier_code != "")
		{
			sc = " SELECT DISTINCT s.sn, c.code, c.supplier_code, c.name ";
			//sc += " FROM stock s  ";
			//sc += " JOIN purchase_item pi ON pi.id = s.purchase_order_id AND pi.supplier_code = s.supplier_code ";
            sc += " FROM code_relations c LEFT OUTER JOIN stock s ON s.product_code = c.code ";
			sc += " WHERE 1=1 "; //s.status = 2 ";
			if(rp_sn != "")
				sc += " AND s.sn = '"+ rp_sn +"' ";
			if(rp_code != "")
           		if(TSIsDigit(rp_code) && rp_code.Length <13)
					sc += " AND c.code = "+ rp_code +" ";
			if(rp_supplier_code != "")
				sc += " AND c.supplier_code = '"+ rp_supplier_code +"' ";
		//DEBUG("sc =", sc);
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				myAdapter.Fill(dst, "SNchk");

			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
		}
	}
	//if(Request.Form["rp_sn"] != null && Request.Form["rp_sn"] != "" )
	else if(rp_sn != "" || rp_code != "" || rp_supplier_code != "")
	{
		sc = " SET DATEFORMAT dmy ";
		sc += " SELECT DISTINCT r.sn, r.code, r.supplier_code, r.prod_name AS name ";
		sc += " FROM rma_stock r "; //JOIN stock s ON r.sn = s.sn ";
		//sc += " JOIN purchase_item pi ON pi.id = s.purchase_order_id ";
		//sc += " JOIN stock_borrow sb ON sb.code = pi.code AND sb.supplier_code = pi.supplier_code ";
		//sc += " WHERE r.sn = '"+ Request.Form["rp_sn"] +"' ";
		sc += " WHERE 1=1 ";
		if(rp_sn != "")
		sc += " AND r.sn = '"+ rp_sn +"' ";
		if(rp_code != "")
			if(TSIsDigit(rp_code) && rp_code.Length <13)
				sc += " AND r.code = "+ rp_code +" ";
		if(rp_supplier_code != "")
			sc += " AND r.supplier_code = '"+ rp_supplier_code +"' ";
			
//DEBUG("sc serach= ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "SNchk");
			
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		//Define it's from rma stock
		if(dst.Tables["SNchk"].Rows.Count > 0)
			m_bfrom_rma_stock = true;

		if(dst.Tables["SNchk"].Rows.Count <=0 )
		{
			sc = " SELECT DISTINCT s.sn, sb.code, sb.supplier_code, c.name ";
			//sc += " FROM stock s  ";
			//sc += " JOIN purchase_item pi ON pi.id = s.purchase_order_id AND pi.supplier_code = s.supplier_code ";
           sc += " FROM code_relations c  ";
			sc += " JOIN stock_borrow sb ON sb.code = c.code AND sb.supplier_code = c.supplier_code ";
            sc += " LEFT OUTER JOIN stock s ON s.product_code = c.code ";
			sc += " WHERE sb.approved =1 ";
			//sc += " AND s.status = 2 ";			
			sc += " AND sb.qty - sb.return_qty - sb.replace_qty > 0 ";
            if(rp_sn != "")
        		sc += " AND s.sn = '"+ rp_sn +"' ";
		    if(rp_code != "")
			    if(TSIsDigit(rp_code) && rp_code.Length <13)
				    sc += " AND c.code = "+ rp_code +" ";
		    if(rp_supplier_code != "")
			    sc += " AND c.supplier_code = '"+ rp_supplier_code +"' ";
		//	sc += " WHERE s.sn = '"+ Request.Form["rp_sn"] +"'";
//DEBUG("sc =", sc);
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				myAdapter.Fill(dst, "SNchk");
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}
		}
		
	}
	if(dst.Tables["SNchk"] != null)
	{
		if(dst.Tables["SNchk"].Rows.Count >=1)
		{
			Session["ss_rpsn"] = dst.Tables["SNchk"].Rows[0]["sn"].ToString();
			Session["ss_rpcode"] = dst.Tables["SNchk"].Rows[0]["code"].ToString();
			Session["ss_rpsupplier_code"] = dst.Tables["SNchk"].Rows[0]["supplier_code"].ToString();
			Session["ss_rpname"] = dst.Tables["SNchk"].Rows[0]["name"].ToString();
			m_bFoundSN = true;
			return true;
		}
	}

	return true;
}

void GetQueryString()
{
	if(Request.QueryString["id"] != "")
		m_id = Request.QueryString["id"];
	if(Request.Form["cmd"] != "" && Request.Form["cmd"] != null)
		m_command = Request.Form["cmd"];
	if(Request.QueryString["sid"] != "")
		m_supplier_id = Request.QueryString["sid"];
	if(Request.QueryString["rmaid"] != "")
		m_rma_id = Request.QueryString["rmaid"];
	if(Request.QueryString["del"] != "")
		m_del_id = Request.QueryString["del"];
	

}
	
</script>
<asp:Label id=LFooter runat=server/>
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

bool m_bReplaced = false;
string[] m_EachMonth = new string[13];

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
	
	GetQueryString();
	if(!SecurityCheck("technician"))
		return;

	//clear session data from last selected data
	if(Request.QueryString["rmaid"] == null || Request.QueryString["rmaid"] == "")
	{
		Session["slt_code"] = null;
		Session["slt_supplier_code"] = null;
		Session["slt_name"] = null;
	}
	if(m_del_id == "1")
	{
		if(DoDeleteReplaced())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "?id="+ m_id +"");
			Response.Write("&rmaid="+ m_rma_id +"&sid="+ m_supplier_id +"\">");	
			
			return;
		}
	}
	if(m_command == "UPDATE to STOCK")
	{
		if(DoProcessItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "?id="+ m_id +"");
			Response.Write("&rmaid="+ m_rma_id +"&sid="+ m_supplier_id +"\">");	
			
			return;
		}
	}
	if(m_command == "PROCESS...")
	{
		if(DoProcessItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + Request.ServerVariables["URL"] + "?id="+ m_id +"");
			Response.Write("&rmaid="+ m_rma_id +"&sid="+ m_supplier_id +"\">");	
		
		return;
		}
	}
	
	InitializeData();
	if(!DoQuerySupplierRMA())
		return;

}


bool DoDeleteReplaced()
{
	string rp_sn = Request.QueryString["rp_sn"];
	string rp_id = Request.QueryString["rp_id"];
	string ra_id = Request.QueryString["id"];
	string code = Request.QueryString["code"];
	//string rma_id = Request.QueryString["rmaid"];
	string sc = " SET DATEFORMAT dmy ";
	sc += " DELETE FROM return_sn WHERE id = "+ rp_id +"";
	sc += " DELETE FROM stock WHERE sn = '"+ rp_sn +"' ";
	sc += " UPDATE rma SET check_status = 2 WHERE id = "+ m_rma_id +"";
	if(g_bRetailVersion)
		sc += " UPDATE stock_qty SET qty = qty - 1 WHERE code = "+ code;
	else
		sc += " UPDATE product SET stock = stock - 1 WHERE code = "+ code;

	sc += AddSerialLogString(rp_sn, "Delete while item return from supplier", "", "", "", Request.QueryString["rmaid"]);
//DEBUG(" sc + ", sc);
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

bool DoProcessItem()
{
	string old_sn = "";
	old_sn = Request.Form["old_sn"];
	mc_sn = Request.Form["new_sn"];
	mc_code = Request.Form["code"];
	mc_supplier_code = Request.Form["supplier_code"];
	mc_supplier_id = Request.Form["supplier"];
	mc_customer_id = Request.Form["customer_id"];
	mc_po_id = Request.Form["po_id"];
	mc_id = Request.Form["hid_id"];
	mc_repair_id = Request.Form["repair_id"];
	string update_stock = ""; //2 = for customer
	update_stock = Request.Form["update_stock"];
	string ignor = Request.Form["ignor"];
	string status = Request.Form["status"];
	
	string action = "";
	if(status == "1")
		action = "Update Replaced Item From Suppplier";
	if(status == "2")
		action = "Faulty Item Repaired By Supplier";
	if(status == "3")
		action = "Credit for Faulty Item";
	if(status == "4")
		action = "Faulty Item is Out of Warranty";

	if(m_command == "UPDATE to STOCK")
	{
	//	update_stock = Request.Form["update_stock"];
		old_sn = Request.Form["update_old_sn"];
		status = Request.Form["update_status"];
		mc_sn = Request.Form["old_sn"];
	}
//DEBUG("odl=sn = ", old_sn);
//DEBUG("odl=sn = ", mc_sn);
//DEBUG("updat_tsotkc = ", update_stock);
//DEBUG("staus = ", status);
//DEBUG("mc_id = ", mc_id);
//DEBUG("code = ", mc_code);
//DEBUG("suppcode = ", mc_supplier_code);
	
	string sc = "SET DATEFORMAT dmy ";
//DEBUG("mcoman d=", m_command);
	if(m_command == "PROCESS...")
	{
		sc += " INSERT INTO return_sn (replaced_sn, old_sn, code, supplier_code, replaced_date, staff, action_desc, ra_id, condition, repair_id, status) ";
		sc += " VALUES('"+ mc_sn +"', '"+ old_sn +"', "+ mc_code +", '"+ mc_supplier_code +"', GETDATE(), "+ Session["card_id"] +", '"+ action +"' ";
		sc += ", "+ mc_id +" "; 
		if(update_stock == "on" )
			sc += ", 2 ";
		else
			sc += " , 1 ";
		sc += ", '"+ mc_repair_id +"', '"+ status +"') ";
		if(mc_repair_id != "" && mc_repair_id != "0")
		{
			if(update_stock == "2")
			{
				//update repair job to set to finish job
				sc += " UPDATE repair SET note = note + '"+ action +"', status = 5 ";
				sc += " WHERE id = "+ mc_repair_id +"";
			}
		}
		
		sc += " UPDATE rma SET check_status = 3 , return_date = GETDATE(), p_code = '"+ mc_code+"', supplier_code = '"+ mc_supplier_code +"' WHERE id = "+ mc_id +"";
		
		if(status != "3" && m_command == "PROCESS...")
		{
			sc += " INSERT INTO rma_stock (repair_id, supp_rma_id, stock_date, stock_by ";
			sc += " , code, supplier_code, prod_name, sn , status, item_location)";
			sc += " VALUES('"+ mc_repair_id +"','"+ mc_id +"', GETDATE(), "+ Session["card_id"] +", "+ mc_code +" ";
			sc += ", '"+ mc_supplier_code +"', '"+ StripHTMLtags(EncodeQuote(Request.Form["name"].ToString())) +"'";
			if(status == "1")
				sc += ", '"+ EncodeQuote(mc_sn) +"' ";
			else
				sc += ", '"+ EncodeQuote(old_sn) +"' ";
			sc += ", 1";
			sc += ", "+ status;
			sc += " ) ";
			sc += AddSerialLogString(mc_sn, "Put into RMA Stock: "+ StripHTMLtags(EncodeQuote(Request.Form["name"].ToString())) +"", "", "", "", mc_id);
					
		}
//DEBUG("sc +", sc);
	}
//DEBUG("stock = ", update_stock);
	if(update_stock == "on" )
	{
		if(status == "2")
		{
			sc += " UPDATE stock SET status = 2 ";
			sc += " WHERE sn = '"+ old_sn +"' ";
		}
		if(status == "1")
		{
			if(mc_sn != "" && mc_sn != null)
			{
				sc += " IF EXISTS (SELECT sn FROM stock WHERE sn = '"+ old_sn +"' ) ";
				sc += " INSERT INTO stock (product_code, cost, branch_id, purchase_date, po_number, purchase_order_id, supplier, supplier_code ";
				sc += " ,  prod_desc, warranty, inv_number, sn, status, update_time";
				sc += " )";
				sc += " (SELECT product_code, cost, branch_id, purchase_date, po_number, purchase_order_id, supplier, supplier_code ";
				sc += " , prod_desc, warranty, inv_number, '"+ mc_sn +"', 2, GETDATE() FROM stock ";
				sc += " WHERE sn = '"+ old_sn +"')";
				
				sc += " INSERT INTO serial_trace (sn, logtime, staff, po_id, action_desc, invoice_number, dealer_rma_id, supplier_rma_id ";
				sc += " )";
				sc += " (SELECT '"+ mc_sn +"', GETDATE(), "+ Session["card_id"] +", purchase_order_id, '"+ action +"','','', "+ mc_id+" FROM stock  ";
				sc += " WHERE sn = '"+ old_sn +"' )";
			}

			//change item status to be rma 
			sc += " UPDATE stock SET status = 4 WHERE sn = '"+ old_sn +"' ";
	//DEBUG(" sc += ", sc);

	  //----------update stock quantity --------------//
			if(g_bRetailVersion)
			{
				sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code = "+ mc_code +" ) ";
				sc += " INSERT INTO stock_qty (code, qty, branch_id) ";
				sc += " VALUES("+ mc_code +", 1, 1) ";
				sc += " ELSE UPDATE stock_qty SET qty = qty + 1 ";
				sc += " WHERE code = "+ mc_code +" ";
			}
			else
			{
				sc += " UPDATE product SET stock = stock + 1 ";
				sc += " WHERE code = "+ mc_code +" AND supplier_code = '"+ mc_supplier_code +"'";
			}
			sc += AddSerialLogString(mc_sn, "Update Stock Qty: "+ StripHTMLtags(EncodeQuote(Request.Form["name"].ToString())) +"", "", "", "", mc_id);
		}

		if(m_command == "UPDATE to STOCK")
		{
			sc += " UPDATE return_sn SET condition = 2 WHERE id = "+ Request.QueryString["return_id"];
			sc += " DELETE FROM rma_stock WHERE supp_rma_id = "+ Request.QueryString["return_id"];
			//sc += AddSerialLogString(mc_sn, "Change Return Faulty Item: "+ StripHTMLtags(EncodeQuote(Request.Form["name"].ToString())) +"", "", "", "", Request.QueryString["return_id"]);
		}
		//DEBUG("sc = ", sc);
	}

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

	Session["slt_code"] = null;
	Session["slt_supplier_code"] = null;
	Session["slt_name"] = null;

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

bool DoQuerySupplierRMA()
{
	if(!TSIsDigit(m_id))
	{
		Response.Write("<script language=javascript>window.alert('INVALID RMA ID, Please Try Again!!'); window.history.go(-1);</script");
		Response.Write(">");
		return false;
	}

	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT r.*, c.name AS prod_desc FROM rma r LEFT OUTER JOIN code_relations c ON c.code = r.p_code AND c.supplier_code = r.supplier_code ";
	sc += " WHERE 1=1 ";
	if(m_id != "" && m_id != null)
		sc += " AND ra_id = "+ m_id;
	sc += " AND check_status = 2 ";
	
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "supplier_rma");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//	sc = " SET DATEFORMAT dmy ";
	sc = " SELECT cs.name AS description, r.id, r.supplier_id, r.ra_id, rs.condition, rs.id AS rs_id, rs.ra_id AS rs_rma_id, rs.old_sn, rs.replaced_sn, rs.replaced_date, rs.code, rs.supplier_code ";
	sc += ", rs.action_desc, e.name AS estatus ";
	sc += " FROM rma r INNER JOIN return_sn rs ON rs.ra_id = r.id LEFT OUTER JOIN enum e ON e.id = rs.status AND e.class='rma_return_status' ";
	sc += " JOIN code_relations cs ON cs.code = rs.code AND rs.supplier_code = cs.supplier_code ";
	sc += " WHERE 1=1 ";
	if(m_id != "" && m_id != null)
		sc += " AND r.ra_id = "+ m_id;
	sc += " AND r.check_status = 3 ";
	
//DEBUG("sc =", sc);

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "returned_sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG(" sc = ", sc);

	int ncols = 8;
	string uri = Request.ServerVariables["URL"]+"?";
	
/*	if(mc_id != "")
			uri += "rmaid="+ mc_id +"&";
		if(m_id != "")
			uri += "id="+ m_id +"&";
*/
	Response.Write("<form name=frm method=post>");
	if(rows > 0)
	{
		//paging class
		PageIndex m_cPI = new PageIndex(); //page index class
		if(Request.QueryString["p"] != null)
			m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
		if(Request.QueryString["spb"] != null)
			m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
			
		m_cPI.TotalRows = rows;
		m_cPI.PageSize = 25;
		m_cPI.URI = "?";
		if(m_sdFrom != "")
			m_cPI.URI += "frm="+ m_sdFrom+"&to="+ m_sdTo+"";
		int i = m_cPI.GetStartRow();
		int end = i + m_cPI.PageSize;
		string sPageIndex = m_cPI.Print();
		
		Response.Write("<center><h4>PROCESSING RETURNED ITEM from SUPPLIER</center></h4>");
		Response.Write("<table width=98% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan="+ ncols +">"+ sPageIndex +"</th></tr>");
		Response.Write("<tr align=left bgcolor=#B1E6FE><th>RA#</th><th>SUPP_RA#</th><th>SN#</th><th>CODE</th><th>SUPPLIER_CODE</th><th>SUPPLIER_ID</th><th>CUSTOMER_ID</th>");
		Response.Write("<th>PURCHASE#</th></tr>");
		
		bool bAlter = false;
		
		for(; i<rows && i<end; i++)
		{
			DataRow dr = dst.Tables["supplier_rma"].Rows[i];
			string id = dr["id"].ToString();
			string supp_ra = dr["supp_rmano"].ToString();
			string ra = dr["ra_id"].ToString();
			string sn = dr["serial_number"].ToString();
			string code = dr["p_code"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			//string desc = dr["prod_desc"].ToString();
			string supplier_id = dr["supplier_id"].ToString();
			string fault = dr["fault_desc"].ToString();
			string repair_jobno = dr["repair_jobno"].ToString();
			string po_id = dr["po_id"].ToString();
			string customer_id = dr["customer_id"].ToString();
			string supplier_rma_no = dr["supp_rmano"].ToString();
			
			mc_name = dr["product_desc"].ToString();
			if(dr["prod_desc"].ToString() != null && dr["prod_desc"].ToString() != "")
				mc_name = dr["prod_desc"].ToString();
			mc_id = id;
			mc_sn = sn;
			mc_code = code;
			mc_supplier_code = supplier_code;
			mc_supplier_id = supplier_id;
			mc_customer_id = customer_id;
			mc_po_id = po_id;	
			mc_stock = dr["stock_check"].ToString();
			
			Response.Write("<tr");
			if(Request.QueryString["rmaid"] != "" && Request.QueryString["rmaid"] != null)
			{
				if(Request.QueryString["rmaid"] == id)
					Response.Write(" bgcolor=#FCEFA4 ");
			}
			else
			{
			if(i+1 == rows)
				Response.Write(" bgcolor=#FCEFA4 ");
			}
					
			Response.Write(">");
			Response.Write("<input type=hidden name=id"+ i +" value="+ id +">");
			Response.Write("<td>");
			Response.Write("<a title='Select To Processing' href='"+ uri +"id="+ m_id +"");
			
			if(Request.QueryString["sid"] != "")
				Response.Write("&sid="+ Request.QueryString["sid"]+"");
			Response.Write("&rmaid="+ id +"' class=o>");
			
			Response.Write(ra);
			Response.Write("</a>");
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(supp_ra);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write("<a title='Select To Processing' href='"+ uri +"id="+ m_id +"");
			if(Request.QueryString["sid"] != "")
				Response.Write("&sid="+ Request.QueryString["sid"] +"");
			Response.Write("&rmaid="+ id +"' class=o>");
			
			Response.Write(sn.ToUpper());
			Response.Write("</a>");
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(code);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(supplier_code);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write("<a title='Select To Processing' href='"+ uri +"id="+ m_id +"");
			if(Request.QueryString["sid"] != "")
				Response.Write("&sid="+ Request.QueryString["sid"] +"");
			Response.Write("&rmaid="+ id +"' class=o>");
			
			Response.Write(supplier_id);
			Response.Write("</a>");
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(customer_id);
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write(po_id);
			Response.Write("</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");
	}
	
	

	Response.Write("<table  align=center cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");

	string rid = mc_id;

	if(Request.QueryString["rmaid"] != "" && Request.QueryString["rmaid"] != null)
		mc_id = Request.QueryString["rmaid"];

	if(mc_id != "")
	{
		if(!DoDisplayCurrentItem(mc_id))
			return false;
		//if(!m_bReplaced)
		{
		//if(rid != "")
		if(mc_id != "")
			uri += "rmaid="+ mc_id +"&";
		if(m_id != "")
			uri += "id="+ m_id +"&";
			
		//Response.Write("<tr><td colspan="+ ncols +">");
		string salign = "left";
		Response.Write("<br>");
		if(Session["slt_code"] != null)
		{
			mc_code = Session["slt_code"].ToString();
			mc_supplier_code = Session["slt_supplier_code"].ToString();
			mc_name = Session["slt_name"].ToString();
		}
		
		Response.Write("<tr><td colspan=3><hr size=1></td></tr>");
		Response.Write("<tr><td colspan=3><font size=2><b>YOU ARE PROCESSING...    SN# <font color=green>"+ mc_sn.ToUpper() +"</font><br></td></tr>");
		Response.Write("<tr><td colspan=3><hr size=1></td></tr>");
		Response.Write("<tr><th align="+ salign +">CODE#</th><td>&nbsp;</td><td><input type=text name=code value='"+ mc_code +"'>");
		if(mc_condition != "1")
			Response.Write(" <a title='get product code, supplier code ...' href='slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"' class=o>Find</a>");
		Response.Write("</td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align="+ salign +">SUPPLIER CODE#</th><td>&nbsp;</td><td><input type=text name=supplier_code value='"+ mc_supplier_code +"'>");
		if(mc_condition != "1")
			Response.Write(" <a title='get product code, supplier code ...' href='slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"' class=o>Find</a>");
		Response.Write("</td>");
		Response.Write("<tr><th align="+ salign +">PRODUCT DESC#</th><td>&nbsp;</td><td><input  size=50%  type=text name=name value='"+ mc_name +"'>");
		Response.Write("</td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align="+ salign +">SUPPLIER#</th><td>&nbsp;</td><td>");
		if(Request.QueryString["sid"] != "" && Request.QueryString["sid"] != null)
			mc_supplier_id = Request.QueryString["sid"];

		uri += "sid=";
		Response.Write(PrintSupplierOptions(mc_supplier_id, uri));
		//Response.Write("<input type=text name=code value='"+ mc_supplier_id +"'></td>");
		Response.Write("</tr>");
		Response.Write("<input type=hidden name=repair_id value="+ mc_repair_id +">");
		Response.Write("<input type=hidden name=hid_id value="+ mc_id +">");
		Response.Write("<tr><th align="+ salign +">CUSTOMER#</th><td>&nbsp;</td><td><input type=text readonly name=customer_id value='"+ mc_customer_id +"'></td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align="+ salign +">PURCHASE ID#</th><td>&nbsp;</td><td><input type=text name=po_id value='"+ mc_po_id +"'></td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align="+ salign +"> ");
		if(mc_condition != "1")
			Response.Write("OLD ");
		Response.Write("SN#</th><td>&nbsp;</td><td><input size=50% type=text name=old_sn readonly value='"+ mc_sn +"'></td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align="+ salign +">STATUS#</th><td>&nbsp;</td><td>");
			
		Response.Write("<select name=status onchange=\"if(document.frm.status.value=='3' || document.frm.status.value=='4'){document.frm.update_stock.checked = false;}\" ");
		if(mc_condition == "1")
			Response.Write(" disabled ");
		Response.Write(">");

		//if(mc_stock == "1")
		{	
			Response.Write("<option value='3' ");
			if(mc_status == "3")
				Response.Write(" selected ");
			Response.Write(">CREDITED</option>");
			Response.Write("<option value='4' ");
			if(mc_status == "4")
				Response.Write(" selected ");
			Response.Write(">Out Of Warranty</option>");
		}
		Response.Write("<option value='2' ");
		if(mc_status == "2")
				Response.Write(" selected ");
		Response.Write(">REPAIRED</option>");
		Response.Write("<option value='1' ");
		if(mc_status == "1")
				Response.Write(" selected ");
		Response.Write(">REPLACED</option>");	
		Response.Write("</select>");
		if(mc_stock == "1")
			Response.Write(" &nbsp;&nbsp;&nbsp;<input type=checkbox name=update_stock onclick=\"if(document.frm.status.value=='3' || document.frm.status.value=='4'){document.frm.update_stock.checked = false;}\"><b>Tick to UPDATE to STOCK, Otherwise LEAVE at RMA STOCK"); //<input type=submit name=cmd value='UPDATE to STOCK' "+ Session["button_style"] +">");
		else
			Response.Write("<input type=hidden name=update_stock value='2'>"); 
		Response.Write("</td>");
		//Response.Write("<input type=text name=status value='"+ mc_sn +"'></td>");
		Response.Write("</tr>");
		if(mc_condition != "1")
		{
		Response.Write("<tr><th align="+ salign +">NEW SN#</th><td>&nbsp;</td><td><input size=30% type=text name=new_sn value='"+ Request.Form["new_sn"] +"'>");
		Response.Write(" &nbsp;<input type=checkbox name=ignor ><b>ignor input SN#");
		Response.Write("</td>");
		Response.Write("</tr>");
		}
		else
		{
			Response.Write("<input type=hidden name=new_sn value='update_stock'>");
		}
		Response.Write("<tr align=right><td colspan=3 align="+ salign +">");
		if(mc_stock == "1")
			Response.Write("This Item is For STOCK");
		else
			Response.Write("This Item is For CUSTOMER <a title='click me to view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?id="+ mc_customer_id +"','', 'width=350,height=350, resizable=1'); viewcard_window.focus();\" class=o>VIEW CUSTOMER</a>");
		Response.Write(" &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
		if(mc_condition == "1")
		{
			if(Request.QueryString["return_id"] != null && Request.QueryString["return_id"] != "")
				Response.Write("<input type=submit name=cmd value='UPDATE to STOCK' "+ Session["button_style"] +" onclick='return jCheckEmpty();'>");
		
		}
		else if(mc_condition != "2")
		{
			Response.Write("<input type=submit name=cmd value='PROCESS...' "+ Session["button_style"] +" onclick='return jCheckEmpty();'>");
			Response.Write("<input type=button name=cmd value='Cancel' "+ Session["button_style"] +" onclick=\"window.location=('supp_rma.aspx?st=2&rma=rd&r="+ DateTime.Now.ToOADate() +"')\">");
		}
		Response.Write(" </td></tr>");
		Response.Write("<tr><td colspan=3><hr size=1></td></tr>");
		
		//Response.Write("</td></tr>");
		
		vJscript();
		}
	}
	Response.Write("</table>");
	if(!DoDisplayProcessedItem(uri))
		return false;

	if(mc_condition == "1")
	{
		Response.Write("<input type=hidden name=update_old_sn value='"+ mc_old_sn +"'>");
		Response.Write("<input type=hidden name=update_status value='"+ mc_status +"'>");
		//Response.Write("<input type=hidden name=update_stock value='"+ mc_stock +"'>");
	}

	Response.Write("</form><br><br><br><br><p>");

	return true;
}

bool DoDisplayProcessedItem(string uri)
{
	int ncols = 8;
	
	Response.Write("<table width=98% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan="+ ncols +"><h4><b><font size=3> ");
	Response.Write("PROCESSED ITEMS</b></center></td></tr>");
	Response.Write("<tr align=left bgcolor=#B1E6FE><th>RA#</th><th>OLD_SN#</th><th>REPLACED SN#</th><th>CODE</th><th>SUPPLIER_CODE</th><th>DESC</th><th>ACTION DESC</th><th>REPLACED DATE</th>");
	Response.Write("<th>CONDITION</th><th>&nbsp;</th></tr>");
	bool bAlter = false;
	
	for(int i=0; i<dst.Tables["returned_sn"].Rows.Count; i++)
	{

		DataRow dr = dst.Tables["returned_sn"].Rows[i];
		string id = dr["id"].ToString();
		string ra = dr["ra_id"].ToString();
		string rma_id = dr["rs_rma_id"].ToString();
		string replaced_id = dr["rs_id"].ToString();
		string old_sn = dr["old_sn"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string action = dr["action_desc"].ToString();
		string replaced_date = dr["replaced_date"].ToString();
		string replaced_sn = dr["replaced_sn"].ToString();
		string status = dr["estatus"].ToString();
		string supplier_id = dr["supplier_id"].ToString();	
		string condition = dr["condition"].ToString();
		string desc = dr["description"].ToString();
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#E1F4F7 ");
		
		bAlter = !bAlter;
		Response.Write(">");
		Response.Write("<td><a title='view supplier rma' href='supp_rma.aspx?rma=rd&spp="+supplier_id+"&rid="+ ra +"&st=3&p=1&spb=1' class=o>"+ ra +"</a> ");
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(old_sn);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(replaced_sn);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(code);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(supplier_code);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(desc);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(action.ToUpper());
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(replaced_date);
		Response.Write("</td>");
		Response.Write("<td>");
		string stext = "";

		if(condition == "1")
			stext = "IN RMA STOCK";
		if(condition == "2")
			stext = "ALREADY UPDATED IN STOCK";

		if(condition == "1")
		{
			Response.Write("<a title='Transfer back to stock' href='"+ Request.ServerVariables["URL"] +"?return_id="+ replaced_id +"");
			Response.Write("&rmaid="+ id +"&id="+ ra +"");
			Response.Write("' class=o> ");
		}

		Response.Write(stext);
		
		Response.Write("</a>");
		//Response.Write(status.ToUpper());
		Response.Write("</td>");
		Response.Write("<td><a title='delete this replaced item' href='"+ Request.ServerVariables["URL"] +"?id="+ m_id +"&rmaid="+ id +"&rp_id="+ replaced_id +"&del=1&rp_sn="+ HttpUtility.UrlEncode(replaced_sn) +"&code="+ code +"' class=o> ");
		Response.Write("<font color=red><b>X</b></a></td>");

		Response.Write("</tr>");
	}
	
	Response.Write("</table>");

	return true;
}

void vJscript()
{
	Response.Write("<script language=javascript>");
	Response.Write("<!---hide from old browser");
	string s = @"		
		function jCheckEmpty()
		{			
			var bPass = true;
			if(document.frm.code.value == '')
				bPass = false;
			if(!isDigit(document.frm.code.value))
			{
				window.alert('Please make sure Product Code is Numberic');
				return false;
				bPass = false;
			}
			
			if(document.frm.new_sn.value == '' && !document.frm.ignor.checked && document.frm.status.value =='1')
					bPass = false;
			
			//if(document.frm.sn.value == '')
			//	bPass = false;
			if(document.frm.supplier_code.value == '')
				bPass = false;
	
			if(document.frm.supplier.value == '')
				bPass = false;
			if(!bPass)
			{
				window.alert('Please Fill Up ALL Fields');
				document.frm.supplier_code.focus();
				return false;
			}
			
			return true;
		}
		function isDigit(sText)
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
	Response.Write("--->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
}

bool DoDisplayCurrentItem(string id)
{
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT *, rs.supplier_code AS supplier_code1, rs.status AS rs_status ";
	sc += " FROM rma r LEFT OUTER JOIN return_sn rs ON rs.ra_id = r.id ";
	sc += " WHERE 1=1 ";
//	sc += " AND status = 2 ";
	sc += " AND r.id = "+ id;
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "current_rma");
		if(rows == 1)
		{
			string old_sn = "";
			old_sn = dst.Tables["current_rma"].Rows[0]["old_sn"].ToString(); 
			mc_sn = dst.Tables["current_rma"].Rows[0]["serial_number"].ToString();
			if(old_sn == mc_sn)
				mc_sn = dst.Tables["current_rma"].Rows[0]["replaced_sn"].ToString();
			mc_old_sn = old_sn;
			mc_code = dst.Tables["current_rma"].Rows[0]["p_code"].ToString();
			if(mc_code == "")
				mc_code = dst.Tables["current_rma"].Rows[0]["code"].ToString();
			mc_supplier_code = dst.Tables["current_rma"].Rows[0]["supplier_code1"].ToString();
			if(mc_supplier_code == "")
				mc_supplier_code = dst.Tables["current_rma"].Rows[0]["supplier_code"].ToString();
			mc_supplier_id = dst.Tables["current_rma"].Rows[0]["supplier_id"].ToString();
			mc_customer_id = dst.Tables["current_rma"].Rows[0]["customer_id"].ToString();
			mc_po_id = dst.Tables["current_rma"].Rows[0]["po_id"].ToString();
			mc_id = dst.Tables["current_rma"].Rows[0]["id"].ToString();
			mc_stock = dst.Tables["current_rma"].Rows[0]["stock_check"].ToString();
			mc_repair_id = dst.Tables["current_rma"].Rows[0]["repair_jobno"].ToString();
			mc_status = dst.Tables["current_rma"].Rows[0]["rs_status"].ToString();
			mc_condition = dst.Tables["current_rma"].Rows[0]["condition"].ToString();

			return true;
		}
		if(rows <= 0 )
		{
			m_bReplaced = true;
			return m_bReplaced;
		}
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	return true;

}

</script>
<asp:Label id=LFooter runat=server/>
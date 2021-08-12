<!-- #include file="page_index.cs" -->
<!-- #include file="isdate.cs" -->

<script runat="server">
int m_selected = 1;  //selected value 

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_uri = "";
string m_sEmail = "";

bool bHide = true;
bool bAutoSearch = false;

string m_jobid = "";
string m_supplier_id = "";
string m_status = "";
string recorded = "";
string m_rp_status = "1";

string m_current_sn = "";
string m_current_code = "";
string m_current_inv = "";
string m_current_inv_date = "";
string m_current_desc = "";
string m_current_fault = "";
string m_current_supplier_id = "";
string m_current_supplier_code = "";
string m_po_number = "";
string m_purchase_order_id = "";
string m_last_supplier_id = "";

string m_querystring = "";

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	InitializeData();
	DoQueryStringData();
	Response.Write("<br>");
	
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		if(DoDeleteSessionItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "");
			Response.Write("\">");
			return;
		}
		
	}
	if(Request.QueryString["delivery"] == "done")
	{
		Response.Write("<center><br><a title='Back to RMA Processing' href='supp_rma.aspx?r="+ DateTime.Now.ToOADate() +"&rma=rd&st=2&rid="+ Request.QueryString["rid"] +"' class=o><h4><< Back to Supplier RMA Process</h4></a>");
		Response.Write("<br><a title='print RMA basic form' href=\"javascript:ra_form_window=window.open('ra_form1.aspx?r="+ DateTime.Now.ToOADate() +"&print=form&ra="+ Request.QueryString["rid"] +"&sid="+ Request.QueryString["sid"] +"', '','');  ra_form_window.focus()\" class=o><h4>Print Basic RMA FORM</h4></a>");
		Response.Write("<br><a title='print RMA form' href=\"javascript:ra_form_window=window.open('ra_form.aspx?r="+ DateTime.Now.ToOADate() +"&print=form&ra="+ Request.QueryString["rid"] +"&sid="+ Request.QueryString["sid"] +"', '','');  ra_form_window.focus()\" class=o><h4>Print RMA FORM</h4></a>");
		return;
	}
	if(Request.QueryString["deliveryid"] != null && Request.QueryString["deliveryid"] != "")
	{
		if(DoUpdateItemStatus())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "&delivery=done&rid="+ Request.QueryString["deliveryid"] +"&sid="+ Request.QueryString["sid"] +"'");
			Response.Write("\">");
			return;
		}
	}
	if(Request.Form["cmd"] == "Cancel")
	{
		CleanSessionData();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?slt="+ m_selected +" \">");
		return;
	}
	if(Request.QueryString["del"] != "" && Request.QueryString["del"] != null)
	{
		if(!doDeleteItem())
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?rma=rd&spp="+ Request.QueryString["spp"] +"&rid="+ Request.QueryString["rid"] +" \">");
		return;
	}
	
	if(Request.Form["cmd"] == "Update Supplier")
	{
		if(!doUpdateSupplier())
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?cp=all \">");
	}
	if((Request.Form["cmd"] == "Record for Supplier RA") || (Request.Form["cmd"] == "Insert to Supplier RMA" ))
	{
		if(Request.Form["cmd"] == "Insert to Supplier RMA")
			m_selected = int.Parse(Request.Form["hide_trows"].ToString());
		
		if(doInsertSupplierRA())
		{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?rma=rd&st=1 \">");
		return;
		}
	}
	if(Request.Form["cmd"] == "Update All Data" && m_status == "1" )
	{
		if(!doUpdateItems())
			return;
		Response.Write("<script language=javascript>window.location=('" + Request.ServerVariables["URL"] + "?lst="+ m_selected +"");
		Response.Write("&rma="+ recorded +"&spp="+ m_supplier_id +"&rid="+ m_jobid +"&st="+ m_status +"'); </script");
		Response.Write(">");	
		return;
		
	}
	if(Request.Form["cmd"] == "Update Inside Data" && m_jobid != null && m_jobid != "")
	{
		if(!doUpdateSubItems())
			return;
	}
	if(Request.Form["cmd"] == "Replace/Finish" && m_status == "2")
	{
		if(!doReplaceItems())
			return;
	}
	if(Request.Form["cmd"] == "Check SN#" || Request.Form["sn"] != null && Request.Form["sn"] != "")
	{
		if(!CheckFaultyItem())
			return;
//	DEBUG("mscurresn =", m_current_sn);
	}
	if(Request.Form["cmd"] == "Update S/N")
	{
		string uri = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
		if(!DoUpdateReplacedSN())
			return;
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=" + uri +" \">");
		return;
	}

	if(Request.Form["cmd"] == "Apply Supplier RA#")
	{
		if(doInsertSupplierRA())
		{	
			SessionCleanUp();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"]+"?rma=rd&spp="+ m_last_supplier_id +"&st=1&r=" + DateTime.Now.ToOADate() + "");
			Response.Write("\">");
			return;
		}
	}

	if(Request.Form["cmd"] == "Add Faulty Item")
	{
		DoAddFaultyItemToSession();
		//clean current inputted items
		m_current_sn = "";
		m_current_code = "";
		m_current_inv = "";
		m_current_inv_date = "";
		m_current_desc = "";
		m_current_fault = "";
		m_current_supplier_id = "";
		m_current_supplier_code = "";
		
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL='"+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "");
		Response.Write("\">");
		return;
	}

	if(Request.QueryString["cs"+ Request.QueryString["c"]] != "all" && Request.QueryString["cs"+ Request.QueryString["c"]] != "" && (Request.QueryString["supplier"] == "all" || Request.QueryString["supplier"] != null && Request.QueryString["supplier"] != "")
		|| (Request.Form["sp_search"] != null || Request.Form["cmd"] == "Search Supplier") && (Request.Form["cmd"] != "Search") && (Request.Form["cmd"] != "Update All Data")
		&& (Request.Form["cmd"] != "Replace/Finish") && (Request.Form["cmd"] != "Update Inside Data")
//		|| Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
	)
	{
		string supplier_id = "";
		if(Request.QueryString["supplier"] != null && Request.QueryString["supplier"] != "")
			supplier_id = Request.QueryString["supplier"];
		if(Request.Form["sp_search"] != null && Request.Form["sp_search"] != "")
			supplier_id = Request.Form["sp_search"].ToString();
		if(getSupplier(supplier_id, false))
				return;
			
	}
	else
	{
		if(Request.QueryString["rma"] == "rd") //|| (Request.QueryString["rid"] != "" && Request.QueryString["rid"] != null)
			DisplayRMA();
		else
		{
			if((Request.QueryString["cs"+ Request.QueryString["c"]+""] != "" && Request.QueryString["cs"+ Request.QueryString["c"]] != null )
				|| (Request.Form["customer_search"] != null || Request.Form["cmd"] == "Search Customer"))
			{
				if(!getCustomer())
					return;
			}
			else if(Request.QueryString["cp"] != "" && Request.QueryString["cp"] != null)
			{
				if(!getAllFaultyReplacedItem())
					return;
			}
			else
				GetFaultyItems();
		}
	}

}

bool DoUpdateItemStatus()
{
	string sc = " UPDATE rma SET check_status = 2, sent_date = GETDATE() WHERE ra_id = "+ Request.QueryString["deliveryid"];
//DEBUG("s c= ", sc);
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
		return false ;
	}
	
	return true;
}

void CleanSessionData()
{
	for(int i=0; i<m_selected; i++)
	{
		Session["sn"+i.ToString()] = null;
		Session["purchase_date"+i.ToString()] = null;
		Session["desc"+i.ToString()]  = null;
		Session["supplier_invoice"+i.ToString()]  = null;
		Session["code"+i.ToString()]  = null;
		Session["supplier_id"+i.ToString()]  = null;
		Session["supplier_code"+i.ToString()]  = null;
		Session["po_id"+i.ToString()] = null;
		Session["customer_id"+i.ToString()] = null;
		Session["customer_name"+i.ToString()] = null;
	}
	Session["supplier_id"] = null;
	Session["supplier_name"] = null;

}

void DoQueryStringData()
{
	if(Request.QueryString["slt"] != null && Request.QueryString["slt"] != "")
		m_selected = int.Parse(Request.QueryString["slt"]);
	if(Request.QueryString["rma"] != null && Request.QueryString["rma"] != "")
		recorded = Request.QueryString["rma"];
	
	if(Request.QueryString["spp"] != null && Request.QueryString["spp"] != "")
		m_supplier_id = Request.QueryString["spp"].ToString();
	if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
		m_jobid = Request.QueryString["rid"].ToString();
	if(Request.QueryString["st"] != null && Request.QueryString["st"] != "")
		m_status = Request.QueryString["st"].ToString();

}

bool DoUpdateReplacedSN()
{
	string new_sn = "";
	string old_sn = "";
	int nRows = int.Parse(Request.Form["hide_subrows"].ToString());
	for(int i=0; i<nRows; i++)
	{
		new_sn = Request.Form["rp_sn"+i.ToString()];
		old_sn = Request.Form["hide_rp_sn"+i.ToString()];
		string id = Request.Form["hide_rs_id"+i.ToString()];
		//DEBUG("old sn =", old_sn);
		//	DEBUG("new sn =", new_sn);
		//	DEBUG("rs id =", id);
		if(old_sn != new_sn)
		{

			string sc = " SET DATEFORMAT dmy ";
			sc += " UPDATE return_sn ";
			sc += " SET replaced_sn = '" + new_sn +"' ";
			sc += " WHERE id = "+ id;
			sc += " UPDATE stock SET sn = '"+ new_sn +"' , status = 2 WHERE sn = '"+ old_sn +"' ";
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
				return false ;
			}
		}
	}
	return true;
}

bool doUpdateSupplier()
{
	string rows = Request.Form["hide_trows"];
	//DEBUG(" row s= ", rows);
	string sc = "";
	for(int i=0; i<int.Parse(rows); i++)
	{
		string ra_number = Request.Form["ra_number"+i.ToString()];
		string supplier_id = Request.Form["supplier"+i.ToString()];
		//DEBUG("supplier_id =", supplier_id);
		if(supplier_id != null && supplier_id != "")
		{
			sc += " UPDATE repair SET supplier_id = '"+ supplier_id +"'";
			sc += " WHERE ra_number = "+ ra_number +" ";
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
				return false ;
			}
		}
	}
	return true;
}

bool doReplaceItems()
{
	string sc = "";
	string replaced_sn = "";
	string old_sn = "";
	string code = "";
	string supplier_code = "";
	string id = "";
	string note = "";
	string status = "";
	string supplier_rma = "";
	string ra_id = "";
	int nCt_rows = int.Parse(Request.Form["hide_subrows"].ToString());

	for(int i=0; i<nCt_rows; i++)
	{
		if(Request.Form["rp_status"+i.ToString()] != "" && Request.Form["rp_status"+i.ToString()] != null)
			status = Request.Form["rp_status"+i.ToString()].ToString();
		if(status == "1")
			replaced_sn = EncodeQuote(Request.Form["rp_sn"+i.ToString()].ToString());
		if(Request.Form["reason"+i.ToString()] != null && Request.Form["reason"+i.ToString()] != "")
			note = EncodeQuote(Request.Form["reason"+i.ToString()].ToString());
		if(Request.Form["supplier_code"+i.ToString()] != "" && Request.Form["supplier_code"+i.ToString()] != "")
			supplier_code = Request.Form["supplier_code"+i.ToString()].ToString();
		if(Request.Form["hide_supplier_rma"+i.ToString()] != null && Request.Form["hide_supplier_rma"+i.ToString()] != "")
			supplier_rma = Request.Form["hide_supplier_rma"+i.ToString()].ToString();

		old_sn = Request.Form["sn"+i.ToString()].ToString();
		ra_id = Request.Form["hide_ra_id"+i.ToString()];
		code = Request.Form["hide_code"+i.ToString()];
		id = Request.Form["hide_rpid"+i.ToString()].ToString();

		bool b_AllowReplaced = false;
		if(status == "1")
		{
			if(replaced_sn != "")
				b_AllowReplaced = true;
			note = "Replaced from Supplier, note: " + note;
		}
		else
			b_AllowReplaced = true;
		if(status == "2")
			note = "Repaired by Supplier, note: " + note;
		if(status == "3")
			note = "Out of Warranty, note: " + note;
		if(status == "4")
			note = "Credit by Supplier, note: " + note;

		if(b_AllowReplaced)
		{
		sc = "SET DATEFORMAT dmy ";
		sc += " INSERT INTO return_sn ( old_sn";
		if(status == "1")
			sc += " ,replaced_sn ";
		sc += ", action_desc, replaced_date, staff, code, supplier_code, ra_id)";
		sc += " VALUES( '"+ old_sn +"'";
		if(status == "1")
			sc += " ,'"+ replaced_sn +"'";
		sc += ", '"+ note +"', GETDATE(),  "+Session["card_id"]+" ";
		if(TSIsDigit(code))
			sc += ", '"+ code +"'";
		else 
			sc += ", 0";
		sc += ", '"+ supplier_code +"', '"+ ra_id +"' ";
		sc += " )";
		
		if(status == "1")
			sc += AddSerialLogString(replaced_sn, "Replaced from Supplier", "", "", "", ra_id);
		if(status == "2")
			sc += AddSerialLogString(replaced_sn, "Repaired from Supplier", "", "", "", ra_id);
		if(status == "3")
			sc += AddSerialLogString(replaced_sn, "Out of Warranty from Supplier", "", "", "", ra_id);
		if(status == "4")
			sc += AddSerialLogString(replaced_sn, "Credit from Supplier", "", "", "", ra_id);

		/*if(status == "1" || status == "2" && (supplier_code != null && supplier_code != ""))
		{
			if(g_bRetailVersion)
			{
				if(supplier_code != "" && supplier_code != null)
				{
					sc += " UPDATE stock_qty SET qty = qty + 1 ";
					sc += " WHERE code = ( SELECT TOP 1 code FROM product WHERE supplier_code = '"+ supplier_code +"') ";
					if(code != "" && code != null)
						sc += " OR code = code ";
				}
			}
			else
			{
				if(supplier_code != "" && supplier_code != null)
				{
					sc += " UPDATE product SET stock = stock + 1 ";
					sc += " WHERE supplier_code = '"+ supplier_code +"' ";
					if(code != "" && code != null)
					{
						if(TSIsDigit(code))
							sc += " OR code = "+ code +" ";
					}
				}
			}
			sc += " UPDATE stock SET sn = '"+ replaced_sn +"', status = 2 WHERE sn = '"+ old_sn +"' ";
			//sc += " UPDATE serial_trace SET sn = '"+ replaced_sn +"' WHERE sn = '"+ old_sn +"' ";
		}
		*/
		sc += " UPDATE rma SET return_date = GETDATE(), status = "+ status +", check_status= 3";
		sc += " WHERE id = "+ id;
		note = "";
		
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
			return false ;
		}
		}
	}

	return true;
}

bool getAllFaultyReplacedItem()
{
	string cp = "";
	if(Request.QueryString["cp"] != null && Request.QueryString["cp"] != "")
		cp = Request.QueryString["cp"];
	string sc = "";
	//get all faulty items from repairs -----
	if(cp != "" && cp != "all")
	{
		sc = "SELECT DISTINCT ";
		sc += " r.id AS ra_id, r.serial_number, r.ra_number, r.code, r.supplier_id, s.prod_desc, pi.name AS prod_desc2, pi.code AS code2, r.prod_desc AS prod_desc3, r.fault_desc, r.supplier_code,  p.inv_number ";
		sc += " , p.date_invoiced, c.company, s.purchase_order_id AS po_id ";
		sc += " FROM repair r LEFT OUTER JOIN stock s ON s.sn = r.serial_number LEFT OUTER JOIN ";
		sc += " purchase p ON s.purchase_order_id = p.id LEFT OUTER JOIN ";
		sc += " purchase_item pi ON pi.id = p.id AND pi.id = s.purchase_order_id ";
		sc += " AND pi.code = s.product_code ";
		//sc += " OR r.code = pi.code LEFT OUTER JOIN ";
		sc += " LEFT OUTER JOIN ";
		sc += " card c ON c.id = r.supplier_id ";
		sc += " WHERE (r.replaced = 1)";
		sc += " AND (r.for_supp_ra = 0) ";
		if(cp != "" && cp.ToUpper() != "UNKNOWN")
			sc += " AND c.company = '"+ cp +"' ";
	}
	else
	{
		sc = "SELECT DISTINCT ISNULL(c.company, 'UNKNOWN') AS company ";
		sc += " FROM repair r LEFT OUTER JOIN ";
		sc += " card c ON c.id = r.supplier_id ";
		sc += " WHERE (r.replaced = 1)";
		sc += " AND (r.for_supp_ra = 0) "; //OR (r.for_supp_ra = null)";
	}	
	
	int rows =0;
	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "list_fault");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	Response.Write("<form name=frm method=post>");
	Response.Write("<table width=90% align=center cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	bool blsCompany = false;
	if(rows > 0)
	{
		
		if(cp != "" && cp != "all")
		{
			Response.Write("<tr bgcolor=#eeeee><td colspan=9 align=center><font size=3><b>Update Faulty Item(s) From Repair</b></td></tr>");
			Response.Write("<tr bgcolor=#EEAD99><td>&nbsp;</td><td>REPAIR#</td><td>SUPP_CODE</td>");
			Response.Write("<td>SUPP_INV#</td><td>SUPP_INV_DATE</td><td>SN#</td><td>SUPPLIER</td><td>SELECT</td><td>DEL</td></tr>");
		}
		else
		{
			Response.Write("<tr bgcolor=#eeeee><td colspan=7 align=center><font size=3><b>There is New Faulty Item(s) From Repair</b></td></tr>");
			//Response.Write("<tr><td>Catagorized by Company Name</td></tr>");
		}
		string stitle = "Show All Product for ";
		string uri = Request.ServerVariables["URL"] +"?cp=";
		bool bAlter = false;
		
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["list_fault"].Rows[i];
			Response.Write("<tr ");
			if(bAlter)
				Response.Write(" bgcolor=#EEEDDD ");
			Response.Write(">");
			
			if(cp != "" && cp != "all")
			{
				bAlter = !bAlter;
				string sn =	dr["serial_number"].ToString();
				string ra_number =	dr["ra_number"].ToString(); 
				string code =	dr["code"].ToString(); 
				if(code == "")
					code =	dr["code2"].ToString(); 	
				string supplier_id =	dr["supplier_id"].ToString(); 
				string prod_desc =	dr["prod_desc"].ToString(); 
				if(prod_desc == "")
					prod_desc =	dr["prod_desc2"].ToString(); 	
				if(prod_desc == "")
					prod_desc =	dr["prod_desc3"].ToString(); 
				//if(prod_desc != "")
					prod_desc = StripHTMLtags(prod_desc);
				string fault =	dr["fault_desc"].ToString(); 
				string supplier_code =	dr["supplier_code"].ToString(); 
				string po_id =	dr["po_id"].ToString(); 
				string inv_number =	dr["inv_number"].ToString(); 
				string date_invoiced =	dr["date_invoiced"].ToString();
				string company = dr["company"].ToString();
				string ra_id = dr["ra_id"].ToString();
				Response.Write("<input type=hidden name='hide_ra_id"+i.ToString()+"' value="+ ra_id +"></td>");
				Response.Write("<td>"+ (i+1) +"</td><td><input size=6% type=text name='ra_number"+i.ToString()+"' value="+ ra_number +"></td>");
				//Response.Write("<td><input type=text name='fault"+i.ToString()+"' value="+ fault +"></td>");
				Response.Write("<input type=hidden name='hide_code"+i.ToString()+"' value="+ code +">");
				Response.Write("<input type=hidden name='hide_poid"+i.ToString()+"' value="+ po_id +">");
				Response.Write("<td><input size=10% type=text name='supplier_code"+i.ToString()+"' value="+ supplier_code +">");
				Response.Write("</td>");
				//Response.Write("<td><input type=text name='prod_desc"+i.ToString()+"' value="+  prod_desc +"></td>");
				Response.Write("<td><input size=10% type=text name='supplier_invoice"+i.ToString()+"' value="+ inv_number +"></td>");
				Response.Write("<td><input size=12% type=text name='purchase_date"+i.ToString()+"' value="+ date_invoiced +">");
				Response.Write("<input type=button value='...' "+ Session["button_style"] +" ");
				Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.purchase_date"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\"></td>");
				Response.Write("</td>");
				Response.Write("<td><input type=text name='sn"+i.ToString()+"' value="+ sn +"></td>");
				Response.Write("<td><select name='supplier"+ i.ToString() +"'>");
				Response.Write(""+ GetCardValue("3", supplier_id) +"</select></td>");
				Response.Write("<td><input type=checkbox name='check"+ i +"' ");
				Response.Write(" onclick=\"if(this.checked){document.frm.del"+ i +".checked=false;}\"");
				Response.Write(" ></td>");
				Response.Write("<td><input type=checkbox name='del"+ i +"'");
				Response.Write(" onclick=\"if(this.checkfed){document.frm.check"+i+".checked=false;}\"");
				Response.Write("></td>");
				Response.Write("</tr>");
				Response.Write("<tr><td>&nbsp;</td><th align=left>Product Description: </td><td colspan=2><input type=text size=40% name='desc"+i.ToString()+"' value='"+ StripHTMLtags(EncodeQuote(prod_desc)) +"' ></td>");
				Response.Write("<th align=left>Fault Description: </td><td colspan=4><textarea name='fault"+i.ToString()+"'>"+ StripHTMLtags(EncodeQuote(fault)) +"</textarea></td>");
				//Response.Write("<th align=left>Fault Description: </td><td colspan=2><input type=text size=40% name='fault"+i.ToString()+"' value="+ fault +" ></td>");
				//Response.Write("<tr><th colspan=2>Product Description: </td><td colspan=4><input type=text name='desc"+i.ToString()+"' value="+  prod_desc +"></td></tr>");
			//Response.Write("<tr><th colspan=2>Fault Description: </td><td colspan=4><input type=text name='fault"+i.ToString()+"' value="+ fault +"></td>");
				Response.Write("</tr>");
				Response.Write("<tr></tr>");
			}
			else
			{
				string company = dr["company"].ToString();
				Response.Write("<td colspan=7>");
				Response.Write("<table width=70% align=center cellspacing=2 cellpadding=3 border=1 bordercolor=gray bgcolor=white");
				Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
				Response.Write("<tr>");
				Response.Write("<td width=35%><a title='"+ stitle + company +"' href='"+ uri + HttpUtility.UrlEncode(company) +"' class=o>"+ company);
				Response.Write("</a></td><td>Faulty Items for "+company+"");
				Response.Write("</td></tr>");
				Response.Write("</table></td>");
				Response.Write("</tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");
				Response.Write("<tr></tr>");

				blsCompany = true;
			}
			
		}
		Response.Write("<input type=hidden name=hide_trows value='"+ rows +"'>");
		if(!blsCompany)
		{
			if(cp != "UNKNOWN")
				Response.Write("</tr><td align=right colspan=9><input type=submit name=cmd value='Insert to Supplier RMA' "+ Session["button_style"] +"></td></tr>");
			else
				Response.Write("</tr><td align=right colspan=9><input type=submit name=cmd value='Update Supplier' "+ Session["button_style"] +"></td></tr>");
			Response.Write("<tr></tr>");
		}
		Response.Write("</form>");
	}
	string uri_link = ""+ Request.ServerVariables["URL"] +"";
	Response.Write("<tr><td colspan=9>");
	Response.Write("<table width=70% align=center cellspacing=2 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr></tr>");
	Response.Write("<tr bgcolor=><td><font size=3><b>RMA Task List</b></font></td></tr>");
	Response.Write("<tr><td><a title='New RMA Record' href='"+ uri_link+"' class=o>New Supplier RMA</a></td>");
	Response.Write("<td>Select to Enter New RA Products</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td><a title='Processing RMA Record' href='"+ uri_link +"?rma=rd&ssp=all&st=1' class=o>Processing RMA</a></td>");
	Response.Write("<td>Select to Edit Faulty Products</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td><a title='Sent to Supplier RMA Record' href='"+ uri_link +"?rma=rd&ssp=all&st=2' class=o>Waiting 4 Returned</a></td>");
	Response.Write("<td>Select to Update/Edit Returned Products</td>");
	Response.Write("</tr>");
	Response.Write("<tr><td><a title='Finish/Returned RMA Record' href='"+ uri_link +"?rma=rd&ssp=all&st=3' class=o>Returned RMA</a></td>");
	Response.Write("<td>Select to View all RA JOB</td>");
	Response.Write("</tr>");
	Response.Write("</table>");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	return true;
}
bool doUpdateSubItems()
{
	string sn = "";
	string supplier_id = "";
	string supplier_code = "";
	string desc = "";
	string supplier_invoice = "";
	string purchase_date = "";
	string fault = "";
	string rma_id = ""; 
	int	ncount = int.Parse(Request.Form["hide_subrows"].ToString());
	string sc = "";
	for(int i=0; i<ncount; i++)
	{
		string id = Request.Form["hide_rpid"+i.ToString()];

		if(Request.Form["sn"+i.ToString()] != "" || Request.Form["sn"+i.ToString()] != null)
			sn = EncodeQuote(Request.Form["sn"+i.ToString()].ToString());
		if(Request.Form["supplier_id"+i.ToString()] != "" || Request.Form["supplier_id"+i.ToString()] != null)
			supplier_id = Request.Form["supplier_id"+i.ToString()].ToString();
		if(Request.Form["supplier_code"+i.ToString()] != null || Request.Form["supplier_code"+i.ToString()] != "")
			supplier_code = Request.Form["supplier_code"+i.ToString()].ToString();
		if(Request.Form["desc"+i.ToString()] != null || Request.Form["desc"+i.ToString()] != "")
			desc = EncodeQuote(Request.Form["desc"+i.ToString()].ToString());
		if(Request.Form["p_date"+i.ToString()] != null || Request.Form["p_date"+i.ToString()] != "")
			purchase_date = Request.Form["p_date"+i.ToString()];
		if(Request.Form["fault"+i.ToString()] != null || Request.Form["fault"+i.ToString()] != "")
			fault = EncodeQuote(Request.Form["fault"+i.ToString()].ToString());
		if(Request.Form["p_invoice"+i.ToString()] != null || Request.Form["p_invoice"+i.ToString()] != "")
			supplier_invoice = Request.Form["p_invoice"+i.ToString()];	
		sc = " SET DATEFORMAT dmy ";
		sc += "UPDATE rma ";
		sc += " SET serial_number = '"+ sn +"', supplier_id = '"+ supplier_id +"' ";
		sc += ", supplier_code = '"+ supplier_code +"', product_desc = '"+ desc +"' ";
		sc += ", purchase_date = '"+ purchase_date +"', fault_desc = '"+ fault +"' ";
		sc += ", invoice_number = '"+ supplier_invoice +"'  ";
		sc += " WHERE id = "+ id +"";
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
			return false ;
		}
	}

	return true;
}
bool doUpdateItems()
{
	string supplier_rma = "";
	string ticket = "";
	string sent = "";
	string rma_id = ""; string tech_email = "";
	int	ncount = int.Parse(Request.Form["hide_rows"].ToString());
	string sc = "";

	for(int i=0; i<ncount; i++)
	{
		if(Request.Form["supplier_rma"+i.ToString()] != "" || Request.Form["supplier_rma"+i.ToString()] != null)
			supplier_rma = Request.Form["supplier_rma"+i.ToString()].ToString();
//		if(Request.Form["ticket"+i.ToString()] != "" || Request.Form["ticket"+i.ToString()] != null)
//			ticket = Request.Form["ticket"+i.ToString()].ToString();
		if(Request.Form["hide_rmaid"+i.ToString()] != null || Request.Form["hide_rmaid"+i.ToString()] != "")
			rma_id = Request.Form["hide_rmaid"+i.ToString()].ToString();
		if(Request.Form["sent"+i.ToString()] != null || Request.Form["sent"+i.ToString()] != "")
			sent = Request.Form["sent"+i.ToString()];
		if(Request.Form["tech_email"+i.ToString()] != null && Request.Form["tech_email"+i.ToString()] != "")
			tech_email = Request.Form["tech_email"+i.ToString()];

		//DEBUG(" supplier = ", m_supplier_id);
		sc = "UPDATE rma ";
		sc += " SET ra_id = "+ rma_id +"";
		//if(ticket != "")
			sc += ", pack_no = '"+ ticket +"' ";
		sc += ", sent_date = GETDATE() ";
		//if(supplier_rma != "")
		sc += ", supp_rmano = '"+ supplier_rma +"'";
//		if(sent == "on")
//			sc += ", check_status = 2 ";
		//sc += ", sent_date = GETDATE() ";
		sc += " WHERE ra_id = '"+ rma_id +"'";
		if(m_supplier_id != "all" &&  m_supplier_id != null && m_supplier_id != "" )
		{
			if(Session["tech_email"].ToString() != tech_email)
			{
				sc += " UPDATE card SET tech_email = '"+ tech_email +"' ";
				sc += " WHERE id = "+ m_supplier_id +"";
			}
		}
				
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

bool doDeleteItem()
{
	string del_job = "";
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
		del_job = Request.QueryString["del"];
	if(del_job != null && del_job != "")
	{
		string sc = "DELETE FROM rma ";
		sc += " WHERE id = "+ del_job;
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
int GetNextRAnumber()
{
	int rNumber = 1000;
	
	if(dst.Tables["insertrma"] != null)
		dst.Tables["insertrma"].Clear();

//DEBUG("rnumber = ", rNumber);
	string sc = "SELECT TOP 1 ra_id FROM rma WHERE ra_id IS NOT NULL ORDER BY ra_id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "insertrma") == 1)
		{
			if(dst.Tables["insertrma"].Rows[0]["ra_id"].ToString() != null && dst.Tables["insertrma"].Rows[0]["ra_id"].ToString() != "")
				rNumber = int.Parse(dst.Tables["insertrma"].Rows[0]["ra_id"].ToString()) + 1;
			else
				rNumber = rNumber + 1;
		}
		Session["rma_id"] = rNumber;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return 0;
	}

	return rNumber;
}

bool GetSelectedJob(string rma_id)
{
	if(TSIsDigit(rma_id))
	{
		string sc = "SELECT DISTINCT  c3.name AS c3customer, c3.trading_name AS c3trading_name, r.customer_id, r.stock_check, r.po_id, r.serial_number, r.ra_id, r.repair_date, r.supplier_id, c2.company ";
		sc += " , c.name AS technician, r.invoice_number, r.pack_no ";
		sc += ",  r.id, r.product_desc, r.fault_desc, r.supp_rmano, CONVERT(varchar(12),r.purchase_date,13) AS purchase_date, r.return_date ";
		sc += ", r.supplier_code, r.email_supplier, r.status, r.p_code ";
		if(m_status == "3")
			sc += " , rs.replaced_sn, rs.action_desc, rs.replaced_date, rs.staff, rs.id AS rs_id ";
		sc += "FROM rma r  LEFT OUTER JOIN card c ON c.id = r.technician ";
		sc += " LEFT OUTER JOIN card c2 ON c2.id = r.supplier_id ";
		sc += " LEFT OUTER JOIN card c3 ON c3.id = r.customer_id ";
		if(m_status == "3")
			sc += " JOIN return_sn rs ON rs.ra_id = r.id";
		sc += " WHERE 1=1 ";
		sc += " AND r.ra_id = "+ rma_id +"";
		if(m_status != "all" && m_status != "")
		{
			sc += " AND r.check_status = "+ m_status;
			if(m_supplier_id != "" && m_supplier_id != null && m_supplier_id != "all")
				sc += " AND r.supplier_id = "+ m_supplier_id;
			//if(m_status == "3")
			//	sc += "
		}
			
		sc += " ORDER BY repair_date ASC";
//	DEBUG("sc = ", sc);
		int rows = 0;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "slt_rma");
		
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
	}
	return true;
}

void DisplayRMA()
{
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string s = @"
	function chkSearch() 
	{	
		var search = document.frm.search;
		if(search.value == '')
		{
			search.focus();
			search.select();
			return false;
		}
		return true;
	}
	";
	Response.Write("-->"); 
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> \r\n");
	
	if(!getRMAItems())
		return;
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = 0;
	rows = dst.Tables["list_rma"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;

	m_cPI.URI = "?rma="+ recorded +"&spp="+m_supplier_id+"&st="+ Request.QueryString["st"] +"&rid="+m_jobid+"";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	string search = "";
	if(Request.Form["search"] != null && Request.Form["search"] != "")
		search = Request.Form["search"];
	Response.Write("<form method=post name=frm>");
	Response.Write("<table width=99% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=2><input type=text name=search value='"+search+"'><input type=submit name=cmd  value='Search' "+ Session["button_style"] +" ></td>");
	Response.Write("\r\n<script");
	Response.Write(">\r\n document.frm.search.focus()\r\n</script");
	Response.Write(">\r\n ");
	Response.Write("<td align=right colspan=5>");
	Response.Write("<a title='list rma tasks' href='"+ Request.ServerVariables["URL"] +"?cp=all' class=o><b>All Tasks</b></a>|&nbsp;");
	Response.Write("<a title='new supplier rma' href='"+ Request.ServerVariables["URL"] +"' class=o><b>New RMA</b></a>|&nbsp;&nbsp;");
	if(!DoOptionSupplier())
		return;
	Response.Write("&nbsp;|&nbsp;Select Status:<select name=sltstatus ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?rma="+recorded+"&spp="+ m_supplier_id +"&st='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>All</option>");
	Response.Write("<option value='1'");
	if(m_status == "1")
		Response.Write(" selected ");
	Response.Write(">Recorded</option>");
	Response.Write("<option value='2'");
	if(m_status == "2")
		Response.Write(" selected ");
	Response.Write(">Sent</option>");
	Response.Write("<option value='3'");
	if(m_status == "3")
		Response.Write(" selected ");
	Response.Write(">Returned</option>");
	Response.Write("</select>");
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td colspan=4>"+ sPageIndex +"");
	if(m_status == "1") // && Request.QueryString["spp"] != null && Request.QueryString["spp"] != "" && Request.QueryString["spp"] != "all")
		Response.Write("<td colspan=3 align=right><input type=submit name=cmd value='Update All Data' "+ Session["button_style"] +">");
	else
		Response.Write("<td colspan=3 align=right>&nbsp;");
	Response.Write("</td></tr>");
	Response.Write("<tr></tr>");
	Response.Write("<tr align=left bgcolor=#DDEEAA><th>RMA#</th><th>RMA DATE</th>");
	string supplier_support_email_supp_rma = GetSiteSettings("supplier_support_email_supp_rma");
	if(m_status == "1" && supplier_support_email_supp_rma =="1"){
		Response.Write("<th>SUPPLIER SUPPORT EMAIL</th>");
		}
	else if(supplier_support_email_supp_rma =="1")
		Response.Write("<th>&nbsp;</th>");
	
	Response.Write("<th>SUPP RMA#</th>");
	Response.Write("<th>STATUS</th>");
	//<th>DELIVERY TICKET</th>");
	Response.Write("<th>");
	if(m_status == "3")
		Response.Write("RETURNED");
	else 
		Response.Write("SENT?");
	Response.Write("</th>");
	Response.Write("<th>ACTION</th>");
	Response.Write("</tr>");
	DataRow dr;
	bool bAlt = true;
	string uri = "";
	int hide_rows = 0;
	string supplier_id = "";
	string tech_email = "";
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["list_rma"].Rows[i];
		string rma_id = dr["ra_id"].ToString();
		string technician = dr["technician"].ToString();
		string repair_date = dr["repair_date"].ToString();
		tech_email = dr["tech_email"].ToString();
		string email_sent = dr["email_supplier"].ToString();
		string return_date = dr["return_date"].ToString();
		supplier_id = dr["supplier_id"].ToString();
		if(return_date != "")
			return_date = DateTime.Parse(return_date).ToString("dd-MMM-yy");
		string sent_date = dr["sent_date"].ToString();
		if(sent_date != "")
			sent_date = DateTime.Parse(sent_date).ToString("dd-MMM-yy");
		int sent = int.Parse(dr["check_status"].ToString());
		string supplier_rma = dr["supp_rmano"].ToString();
//		string ticket = dr["pack_no"].ToString();
		
		if(supplier_rma == "" && Request.Form["supplier_rma"+i.ToString()] != "" || Request.Form["supplier_rma"+i.ToString()] != null)
			supplier_rma = Request.Form["supplier_rma"+i.ToString()];
//		if(ticket == "" && Request.Form["ticket"+i.ToString()] != "" || Request.Form["ticket"+i.ToString()] != null)
//			ticket = Request.Form["ticket"+i.ToString()];

		uri = "?rma="+recorded+"&spp="+ m_supplier_id +"&st="+m_status+"&rid="+rma_id+"&p="+m_cPI.CurrentPage+"&spb="+m_cPI.StartPageButton+"";
		if(Request.QueryString["rid"] == rma_id  )
			uri = "?rma="+recorded+"&spp="+ m_supplier_id +"&st="+m_status+"&p="+m_cPI.CurrentPage+"&spb="+m_cPI.StartPageButton+"";
		uri = ""+Request.ServerVariables["URL"] + uri +"";
		Response.Write("<tr ");
		if(!bAlt)
			Response.Write(" bgcolor=#EEEEEE");
		bAlt = !bAlt;
		Response.Write(">");

		Response.Write("<td><a title='View/Process Job' href='"+ uri +"' class=o>"+ rma_id+"</a></td>");
		Response.Write("<td>"+ DateTime.Parse(repair_date).ToString("dd-MMM-yy") +"</td>");
		if(m_status == "1" && supplier_support_email_supp_rma =="1"){
		Response.Write("<th align=left>");
		//if(email_sent == "1")
		//	Response.Write("<font color=purple><b>*Email SENT </font></b>&nbsp;");
		//Response.Write("<a title='Email to Supplier' href='ra_form.aspx?print=form&ra="+rma_id+"&confirm=1&email="+tech_email+"&srma="+ supplier_rma+"' target=blank class=o><font color=red><b>EM</font></b></a>");
	
		
			Response.Write("&nbsp;<input type=text name='tech_email"+i.ToString()+"' value='"+tech_email+"'>");
			
		//Response.Write("&nbsp;<a title='View/Print RMA normal Form for Supplier' href='ra_form1.aspx?sid="+ supplier_id +"&print=form&ra="+rma_id+"&srma="+ supplier_rma+"' target=blank class=o><font color=green><b>PRF1</font></b></a>");
		Response.Write("&nbsp;<a title='View/Print RMA Form for Supplier' href='ra_form.aspx?sid="+ supplier_id +"&print=form&ra="+rma_id+"&srma="+ supplier_rma+"' target=blank class=o><font color=green><b>PRF</font></b></a>");
		Response.Write("&nbsp;<a title='Email to Supplier' href='ra_form.aspx?sid="+ supplier_id +"&ra="+rma_id+"&confirm=1&email="+tech_email+"&srma="+ supplier_rma+"' target=blank class=o><font color=red><b>EMAIL</font></b></a>");
		
		if(email_sent == "1")
			Response.Write("&nbsp;<font color=purple><b>*SENT</font></b>");

		Response.Write("&nbsp;&nbsp;&nbsp;<a title='View/Process Job' href='"+ uri +"' class=o><b>EDIT</b></a>");
		Response.Write("</td>");
		}
		Response.Write("<td><input type=text name='supplier_rma"+i.ToString()+"' value='"+ supplier_rma +"'>");
		//Response.Write("&nbsp;<a title='View/Process Job' href='"+ uri +"' class=o>"+ supplier_rma +"</a>");
		//Response.Write("&nbsp;<a title='View/Process Job' href='"+ uri +"' class=o><b>EDIT</b></a>");
		Response.Write("</td>");
//		Response.Write("<td><input type=text name='ticket"+i.ToString()+"' value='"+ ticket +"'></td>");
		//if(m_status != "3")

		Response.Write("<td>");	
		if(sent == 1)
			Response.Write("Product Recorded");
		if(sent == 2)
			Response.Write("Product Sent to Supplier");
		if(sent == 3)
			Response.Write("Returned From Supplier");
		Response.Write("</td>");
	
		if(m_status == "1" || m_status == "all" || m_status == "")
		{
			//Response.Write("<th><input type=checkbox name='sent"+i.ToString()+"' ");
		//	if(sent == 2)
		//		Response.Write(" checked disabled ");
		//	Response.Write("></td>");
			Response.Write("<td>&nbsp;</td>");
		}
		else if(m_status == "3")
			Response.Write("<td>"+return_date+"</td>");
		else if(m_status == "2")
			Response.Write("<td>"+sent_date+"</td>");
		
		Response.Write("<td>");
	
		Response.Write("<a title='View/Process Job' href='"+ uri +"' class=o><font color=red>EDIT</font></a>&nbsp;&nbsp;");
		Response.Write("<a title='Input Freight Ticket Number' href='ra_freight.aspx?rid="+ rma_id +"&ty=2' class=o>");
		Response.Write("FTK</a> ");
		if(sent == 1)
		{
			Response.Write("<a title='Sent Item to Supplier without Freight Ticket' href='"+ Request.ServerVariables["URL"] +"?rma=rd&r="+ DateTime.Now.ToOADate() +"&deliveryid="+ rma_id +"&sid="+ supplier_id +"' ");
			Response.Write(" onclick=\"if(!confirm('Are you sure want to Sent Product to Supplier Without Freight Ticket???')){return false;}\" ");
			Response.Write("class=o>");
			Response.Write("Delivery</a> ");
		}
		
		Response.Write("<a title='process return items' href='ra_process.aspx?id="+ rma_id +"' class=o>");
		if(m_status == "2")
			Response.Write("PRO");
		else if(m_status == "3")
			Response.Write("VIEW");
		Response.Write("</a>");

		Response.Write("</td>");
			
		//hide rows data
		Response.Write("<input type=hidden name='hide_rmaid"+i.ToString()+"' value='"+ rma_id +"'>");
		Response.Write("</tr>");
		
		if(Request.QueryString["rid"] == rma_id)
		{		
			if(!GetSelectedJob(rma_id))
				return;
			
			Response.Write("<tr><td colspan=7>");
			Response.Write("<table width=98% align=center cellspacing=1 cellpadding=2 border=0 bordercolor=#E3E3E3 bgcolor=white");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
			int nCt = dst.Tables["slt_rma"].Rows.Count;
			nCt -= 1;
			for(int ii=0; ii<dst.Tables["slt_rma"].Rows.Count; ii++)
			{
				dr = dst.Tables["slt_rma"].Rows[ii];
				string status = dr["status"].ToString();
				string code = dr["p_code"].ToString();
				string supplier_code = dr["supplier_code"].ToString();
				string desc = dr["product_desc"].ToString();
				string sn = dr["serial_number"].ToString();
				string purchase_invoice = dr["invoice_number"].ToString();
				string p_date = dr["purchase_date"].ToString();
				string fault = dr["fault_desc"].ToString();
				fault = StripHTMLtags(fault);
				desc = StripHTMLtags(desc);
				string po_id = dr["po_id"].ToString();
				//string supplier_id = dr["supplier_id"].ToString();
				string id = dr["id"].ToString();
				string supplier = dr["company"].ToString();
				string customer_id = dr["customer_id"].ToString();
				string customer = dr["c3customer"].ToString();
				if(customer == "")
					customer = dr["c3trading_name"].ToString();
				string check_stock = dr["stock_check"].ToString();
				string replaced_sn = "";
				string reason = "";
				string rs_id = "";
				//DEBUG(" code =", code);
				if(m_status == "3")
				{
					replaced_sn = dr["replaced_sn"].ToString();
					reason = dr["action_desc"].ToString();
					rs_id = dr["rs_id"].ToString();
				}
				Response.Write("<tr bgcolor=#E3E3E3><th colspan=2>SUPPLIER</th><th align=left>S/N</th>");
				Response.Write("<th align=left>CODE");
				Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;SUPP_CODE</th><th align=left>PROD_DESC</th>");
				//Response.Write("<th align=left>CODE</th>");
				//Response.Write("<th align=left>SUPP_CODE</th><th align=left>PROD_DESC</th>");
				Response.Write("<tr ");
				//if(!bAlt)
				//	Response.Write(" bgcolor=#EEEEEE");
				//bAlt = !bAlt;
				Response.Write(">");
				Response.Write("<th rowspan=3 valign=top align=left>"+(ii+1)+"</td>");
				Response.Write("<td>");
				Response.Write("<a title='delete this job' href='"+Request.ServerVariables["URL"]+"?rma="+recorded+"&spp="+ m_supplier_id+"&rid="+m_jobid+"&del="+id+"' class=o ><font color=red><b>X</b></font></a>");
				Response.Write("&nbsp;<select name='supplier_id"+ii.ToString()+"'>");
				Response.Write(""+ GetCardValue("3", supplier_id) +"</select></td>");
				Response.Write("<td><input type=text name='sn"+ ii.ToString() +"' value='"+sn+"'></td>");
				Response.Write("<td>");
				Response.Write("<input type=text name='code"+ ii.ToString() +"' value='"+code+"'>");
				//Response.Write("</td>");
				//Response.Write("<td>");
				Response.Write("<input type=text name='supplier_code"+ ii.ToString() +"' value='"+supplier_code+"'></td>");
				Response.Write("<td><input type=text size=45%  name='desc"+ ii.ToString() +"' value='"+ desc +"'></td>");
				Response.Write("</tr>");
				Response.Write("<tr bgcolor=><th align=left>SUPP_INV#</th><th align=left>PURCHASE DATE</th><th colspan=2 align=left>FAULT</th></tr>");
				Response.Write("<tr>");
				Response.Write("<td><input type=text name='p_invoice"+ ii.ToString() +"' value='"+purchase_invoice+"'></td>");
	//	DEBUG("pdate = ", p_date);
				Response.Write("<td><input type=text name='p_date"+ ii.ToString() +"' value=");
				if(p_date != "" && p_date != null)
				{
					if(DateTime.Parse(p_date).ToString("dd-MM-yyyy") == "01-01-1900")
						Response.Write("''");
					else
						Response.Write("'"+p_date+"' ");
				}
				Response.Write(">");
				Response.Write("<input type=button value='...' "+ Session["button_style"] +" ");
				Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.p_date"+ii.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\"></td>");
				Response.Write("</td>");
				Response.Write("<td colspan=2><input type=text size=80% name='fault"+ ii.ToString() +"' value='"+ fault +"'> </td>");
			
				Response.Write("</tr>");
				Response.Write("<tr><td colspan=5><hr size=0 color=#C0C0C0></td></tr>");
				Response.Write("<input type=hidden name='hide_code"+ii.ToString()+"' value='"+ code +"'>");
				Response.Write("<input type=hidden name='hide_supplier_rma"+ii.ToString()+"' value='"+ supplier_rma +"'>");
				//Response.Write("<input type=hidden name='hide_suppler_rma"+ii.ToString()+"' value='"+ supplier_rma +"'>");
				Response.Write("<input type=hidden name='hide_ra_id"+ii.ToString()+"' value='"+ rma_id +"'>");
				Response.Write("<input type=hidden name='hide_rpid"+ii.ToString()+"' value='"+ id +"'>");
			}
			Response.Write("<input type=hidden name='hide_subrows' value='"+ dst.Tables["slt_rma"].Rows.Count +"'>");
			
			if(m_status == "1")
				Response.Write("<tr><td colspan=5 align=right><input type=submit name=cmd value='Update Inside Data' "+ Session["button_style"] +"></td></tr>");
			Response.Write("</table>");
			Response.Write("</td></tr>");
		}
		hide_rows = i+1;
	}
	Session["tech_email"] = tech_email;
	Response.Write("<input type=hidden name='hide_rows' value='"+ hide_rows +"'>");
	//Response.Write("<tr></tr>");
	//Response.Write("<tr></tr>");
//	if(m_status == "1")
if(m_status == "1") // && Request.QueryString["spp"] != null && Request.QueryString["spp"] != "" && Request.QueryString["spp"] != "all")
	{
		Response.Write("<tr><td colspan=7 align=right>");
		//Response.Write("<input type=reset value='Clear Data' "+ Session["button_style"] +">");
		Response.Write("<input type=submit name=cmd value='Update All Data' "+ Session["button_style"] +"></td></tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");
}

bool DoOptionSupplier()
{
	string sc = " SELECT DISTINCT c.id, c.company ";
	sc += "FROM card c JOIN rma r ON r.supplier_id = c.id ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "supplier");
	
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	if(rows <= 0)
		return true;
	Response.Write("Select Supplier: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?rma="+recorded+"&st="+ m_status +"&spp='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["supplier"].Rows[i];
		string id = dr["id"].ToString();
		string s = dr["company"].ToString();
		if(m_supplier_id == id)
			Response.Write("<option value='"+id+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+id+"'>" +s+ "");

	}
	Response.Write("</select>");
	
	return true;
}
bool getRMAItems()
{
	string sc = "SELECT DISTINCT r.ra_id, CONVERT(varchar(12),r.repair_date,13) AS repair_date, r.supplier_id, c2.company ";
	sc += " , c.name AS technician, r.email_supplier ";
	{
		sc += ",  r.check_status, r.pack_no, r.supp_rmano, c2.tech_email, CONVERT(varchar(12), r.return_date, 13) AS return_date ";
		sc += " , CONVERT(varchar(12), sent_date, 13) AS sent_date ";
	}
	sc += "FROM rma r  LEFT OUTER JOIN card c ON c.id = r.technician ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = r.supplier_id ";
	sc += " WHERE 1=1 ";
	if(m_supplier_id != "" && m_supplier_id != "all")
		sc += " AND r.supplier_id = "+ m_supplier_id;
	if(m_status != "" && m_status != "all")
		sc += " AND r.check_status = "+ m_status;
	if(Request.Form["cmd"] == "Search")
	{
		string search = Request.Form["search"];
		if(TSIsDigit(search))
			sc += " AND r.ra_id = "+ search +" OR r.supp_rmano = '"+ search +"'";
		else if(search != null && search != "")
		{
			sc += " AND r.supp_rmano LIKE '%" + search +"%' ";
			//sc += " OR r.repair_date LIKE '%"+ search +"%' ";
		}
		else
			sc += " ";
	}
	if(Request.QueryString["id"] != "" && Request.QueryString["id"] != null)
		if(TSIsDigit(Request.QueryString["id"].ToString()))
			sc += " AND r.id = "+ Request.QueryString["id"];
	if(Request.QueryString["rid"] != "" && Request.QueryString["rid"] != null)
		sc += " AND r.ra_id = "+ Request.QueryString["rid"] +"";
	//sc += " ORDER BY repair_date DESC";
	sc += " ORDER BY r.ra_id DESC ";
//DEBUG("sc =", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "list_rma");
	
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

bool doInsertSupplierRA()
{
	string customer_id = "";
	
	string ra_number = GetNextRAnumber().ToString();
	if(ra_number == "")
		return false;
	if(Session["ss_count"] == null || Session["ss_count"] == "")
		return false;

	string sc = "";
	string stock_check = "1";
	for(int i=1; i<=(int)Session["ss_count"]; i++)
	{
		if(Session["ss_desc"+ i.ToString()] != null && Session["ss_desc"+ i.ToString()] != "" && Session["ss_fault"+ i.ToString()] != null && Session["ss_fault"+ i.ToString()] != "")
		{
			sc = "SET DATEFORMAT dmy ";
			sc += "INSERT INTO rma (po_id, serial_number, ra_id, status, fault_desc, repair_date ";
			sc += ", technician, invoice_number, product_desc, purchase_date, p_code, supplier_code, supplier_id ";
		//	sc += " , repair_jobno ";
			if(Session["ss_customer_id"+ i] != null && Session["ss_customer_id"+ i] != "")
			{
				stock_check = "2";
				sc += " , customer_id";
			}
			sc += " , stock_check ";
			sc += " , check_status ";
			sc += ") ";
			
			sc += " VALUES('"+ Session["ss_purchase_order_id"+ i] +"', '"+ Session["ss_sn"+ i.ToString()] +"', '"+ ra_number +"' ";
			sc += ", 0 , '"+ StripHTMLtags(EncodeQuote(Session["ss_fault"+ i.ToString()].ToString())) +"', GETDATE() ";
			sc += ", '"+ Session["card_id"] +"', '"+ Session["ss_inv"+ i.ToString()] +"' ";
			sc += ", '"+ StripHTMLtags(EncodeQuote(Session["ss_desc"+ i.ToString()].ToString()).ToString()) +"' ";
			if(Session["ss_inv_date"+ i.ToString()] != "" && Session["ss_inv_date"+ i.ToString()] != null)
			{
				if(IsDate(Session["ss_inv_date"+ i.ToString()]))
					sc += " ,'"+ DateTime.Parse((Session["ss_inv_date"+ i.ToString()]).ToString()).ToString("dd-MM-yyyy") +"'";
				else
					sc += " , '' ";
			}
			else
				sc += " , '' ";
			sc += ", '"+ Session["ss_code"+ i.ToString()] +"' ";
			sc += ",'"+ Session["ss_supplier_code"+ i.ToString()] +"','"+ Session["supplier_id"] +"' ";
			
			if(Session["ss_customer_id"+ i] != null && Session["ss_customer_id"+ i] != "")
				sc += ", '"+ Session["ss_customer_id"+ i] +"' ";

			sc += " , '"+ stock_check +"' ";
			sc += " , 1  ";
			sc += ")";
			
			//update item status, set to rma status
			if(Session["ss_sn"+ i.ToString()] != null && Session["ss_sn"+ i.ToString()] != "")
			{
				sc += " UPDATE stock SET status = 4 WHERE sn = '"+ Session["ss_sn"+ i.ToString()] +"' ";
				sc += AddSerialLogString(""+ Session["ss_sn"+ i.ToString()] +"", "Item Request for Supplier RMA: "+ StripHTMLtags(EncodeQuote(Session["ss_desc"+ i.ToString()].ToString())) +"", ""+ Session["ss_purchase_order_id"+ i.ToString()] +"", "", "", ra_number);
			}
//DEBUG(" sc = ", sc);
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

		}
	}

	return true;
}


bool getSupplier(string supplier_id, bool bAutoSearch)
{

	if(Request.QueryString["ccl"] == "true")
	{
		Session["customer_id"+ Request.QueryString["c"]] = null;
		Session["customer_name"+ Request.QueryString["c"]] = null;
	}
	if(Request.QueryString["supplier"] != "all" && Request.QueryString["supplier"] != "" && Request.QueryString["supplier"] != null)
		supplier_id = Request.QueryString["supplier"].ToString();
	string sc = "SELECT TOP 60 id, company, name, address1, address2, city, phone, email, fax  FROM card ";
	sc += " WHERE type = 3";
	if(supplier_id != "" && supplier_id != null && supplier_id != "all")
	{
		if(TSIsDigit(supplier_id))
			sc += " AND id = "+ supplier_id +"";
		else
		{
			sc += " AND phone LIKE '%"+ supplier_id +"%' OR company LIKE '%"+ supplier_id +"%' ";
			sc += " OR name LIKE '%"+ supplier_id +"%' ";
			sc += " OR email LIKE '%"+ supplier_id +"%' OR trading_name LIKE '%"+ supplier_id +"%' ";
		}
	}
//DEBUG("sc - ", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "supplier");
	
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	if(rows == 1)
	{
		Session["supplier_name"] = dst.Tables["supplier"].Rows[0]["company"].ToString();
		Session["supplier_id"] = dst.Tables["supplier"].Rows[0]["id"].ToString();
		Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"]+"?slt="+ m_selected +"')</script");
		Response.Write(">");
		return false;
	}	
//DEBUG("rows = ", rows);
	
	bool bAlter = true;
	if(rows > 1 && !bAutoSearch)
	{
		Response.Write("<form method=post name=frm>");
		Response.Write("<table width=96% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=gray bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><th align=left colspan=5>Search Supplier By: ID, Phone, Name, and etc...<input type=text name=sp_search value='"+ Request.Form["sp_search"] +"'>");
		Response.Write("<input type=submit name=cmd value='Search Supplier'"+Session["button_style"]+">");
		//Response.Write("<input type=submit name=cmd value='Cancel'"+Session["button_style"]+">");
		Response.Write("</td></tr>");
		Response.Write("\r\n<script");
		Response.Write(">\r\ndocument.frm.sp_search.focus();\r\ndocument.frm.sp_search.select();\r\n</script");
		Response.Write(">\r\n");
		Response.Write("<tr></tr>");
		Response.Write("<tr></tr>");
		Response.Write("<tr bgcolor=#B3DEE><th>ID</th><th>Name</th><th>Company</th><th>Phone</th><th>Email</th></tr>");
		for(int i=0; i<dst.Tables["supplier"].Rows.Count; i++)
		{
			DataRow dr = dst.Tables["supplier"].Rows[i];
			string id = dr["id"].ToString();
			string name = dr["name"].ToString();
			string company = dr["company"].ToString();
			string addr1 = dr["address1"].ToString();
			string addr2 = dr["address2"].ToString();
			string city = dr["city"].ToString();
			string phone = dr["phone"].ToString();
			string fax = dr["fax"].ToString();
			string email = dr["email"].ToString();

			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
			bAlter = !bAlter;
			string sLink = ""+Request.ServerVariables["URL"]+"?slt="+ m_selected +"&supplier="+id;
			Response.Write(">");
			Response.Write("<td><a title='select this supplier' href='"+sLink+"' class=o>"+ id +"</a></td>");
			Response.Write("<td>"+ name +"</td>");
			Response.Write("<td><a title='select this supplier' href='"+sLink+"' class=o>"+ company.ToUpper() +"</td>");
			Response.Write("<td><a title='select this supplier' href='"+sLink+"' class=o>"+ phone +"</td>");
			Response.Write("<td><a title='select this supplier' href='"+sLink+"' class=o>"+ email +"</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");
		Response.Write("</form>");
	}
	

	return true;
}

bool getCustomer()
{
	string count_id = Request.QueryString["c"];
	string customer_id = "";
	if(Request.QueryString["cs"+ count_id] != "all" && Request.QueryString["cs"+ count_id] != "" && Request.QueryString["cs"+ count_id] != null)
		customer_id = Request.QueryString["cs"+ count_id].ToString();
	if(Request.Form["customer_search"] != null)
		customer_id = Request.Form["customer_search"].ToString();
	customer_id = EncodeQuote(customer_id);
//DEBUG("customer =", customer_id);
	string sc = "SELECT TOP 60 id, company, name, address1, address2, city, phone, email, fax  FROM card ";
	//sc += " WHERE type <> 3";
	sc += " WHERE 1=1 ";
	if(customer_id != "" && customer_id != null && customer_id != "all")
	{
		if(TSIsDigit(customer_id))
		{
			sc += " AND id = "+ customer_id +"";
			//sc += " OR phone LIKE '%"+ customer_id +"' ";
		}
		else
		{
			sc += " AND phone LIKE '%"+ customer_id +"' OR company LIKE '%"+ customer_id +"' ";
			sc += " OR name LIKE '%"+ customer_id +"' OR fax LIKE '%"+ customer_id +"' ";
			sc += " OR email LIKE '%"+ customer_id +"' OR trading_name LIKE '%"+ customer_id +"' ";
		}
	}
//DEBUG(" sc = ", sc);
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "customer");
	
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	//DEBUG("rows = ", rows);
	if(rows == 1)
	{
		Session["customer_name"+ count_id +""] = dst.Tables["customer"].Rows[0]["name"].ToString();
		if(Session["customer_name"+ count_id +""] == null || Session["customer_name"+ count_id +""] == "")
			Session["customer_name"+ count_id +""] = dst.Tables["customer"].Rows[0]["company"].ToString();
		Session["customer_id"+ count_id +""] = dst.Tables["customer"].Rows[0]["id"].ToString();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+Request.ServerVariables["URL"]+"?slt="+m_selected+" \">");
		return true;
	}	
	Response.Write("<form method=post name=frm>");
	Response.Write("<table width=95% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><th align=left colspan=5>Search Customer By: ID, Phone, Name, and etc...<input type=text name=customer_search value=''>");
	Response.Write("<input type=submit name=cmd value='Search Customer'"+Session["button_style"]+">");
	Response.Write("<input type=button name=cmd value='Add New Customer'"+Session["button_style"]+"");
	Response.Write(" onclick=\"window.open('ecard.aspx?n=customer&a=new')\">");
	Response.Write("<input type=button name=cmd value='Cancel'"+Session["button_style"]+"");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"]+"?slt="+ m_selected +"&c="+ Request.QueryString["c"] +"&supplier="+ Session["supplier_id"]+"&ccl=true')\">");
	Response.Write("</td></tr>");
	Response.Write("\r\n<script");
	Response.Write(">\r\ndocument.frm.customer_search.focus();\r\ndocument.frm.customer_search.select();\r\n</script");
	Response.Write(">\r\n");
	Response.Write("<tr></tr>");
	Response.Write("<tr></tr>");
	Response.Write("<tr bgcolor=#E3E3E3><th>ID</th><th>Name</th><th>Company</th><th>Phone</th><th>Email</th></tr>");
	bool bAlter = true;
	if(rows > 1)
	{
		for(int i=0; i<dst.Tables["customer"].Rows.Count; i++)
		{
			DataRow dr = dst.Tables["customer"].Rows[i];
			string id = dr["id"].ToString();
			string name = dr["name"].ToString();
			string company = dr["company"].ToString();
			string addr1 = dr["address1"].ToString();
			string addr2 = dr["address2"].ToString();
			string city = dr["city"].ToString();
			string phone = dr["phone"].ToString();
			string fax = dr["fax"].ToString();
			string email = dr["email"].ToString();

			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#FFFFFA ");
			bAlter = !bAlter;
			string sLink = ""+Request.ServerVariables["URL"]+"?slt="+ m_selected +"&cs"+ count_id +"="+id+"&c="+ count_id;
			Response.Write(">");
			Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ id +"</a></td>");
			Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ name +"</td>");
			Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ company +"</td>");
			Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ phone +"</td>");
			Response.Write("<td><a title='select this custoemr' href='"+sLink+"' class=o>"+ email +"</td>");
		
			Response.Write("</tr>");
		}
	}
	Response.Write("</table>");
	Response.Write("</form>");
	
	return true;
}


bool GetFaultyItems()
{
	
	Response.Write("<br><center><h4>SUPPLIER RA APPLICATION </h4>");
	Response.Write("<form name=frm method=post>");
//	if(m_sSite.ToLower() == "admin")
//	{
		Response.Write("<h5><center>Select Supplier: <select name=slt_supplier ");
		Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?slt="+ m_selected +"&supplier=all')\">"); //'+this.options[this.selectedIndex].value)\">");
		if(Session["supplier_id"] != "" && Session["supplier_id"] != null)
			Response.Write("<option value='"+ Session["supplier_id"] +"' selected>"+ Session["supplier_name"] +"</option>");
		Response.Write("<option value=''>ALL</option>");
		Response.Write("</select>");
		Response.Write("&nbsp; | &nbsp;<a title='view details' ");
		Response.Write(" href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["supplier_id"] + "', '',");
		Response.Write("'width=350, height=350'); viewcard_window.focus()\" class=o><font color=Green>");
		Response.Write(""+ Session["supplier_name"] +"</a></font> </h5>");
		
//	}
	Response.Write("<table align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td>");
	Response.Write("<table align=center  cellspacing=0 cellpadding=0 border=0 bordercolor=#CCCCCC ");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	//--**************--- hidden value ---********************----
	Response.Write("<input type=hidden name=supplier_inv value='"+ m_po_number +"'>");
	Response.Write("<input type=hidden name=purchase_order_id value='"+ m_purchase_order_id +"'>");
	//--**************--- end hidden value ---********************----

	Response.Write("<tr><th align=left >Is Faulty Item For Customer:");
	Response.Write("</th><td>");
	Response.Write("<select name=for_customer ");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?cs=all')\""); //'+this.options[this.selectedIndex].value)\">");
	Response.Write(" >");
	if(Session["cusotomer_id"] != "" && Session["customer_id"] != null)
		Response.Write("<option value='"+ Session["customer_id"] +"'>"+ Session["customer_name"] +"</option>");
	else
	Response.Write("<option value='all'>ALL</option>");
	Response.Write("</select>");
	Response.Write(" <a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["customer_id"] + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>"+ Session["customer_name"] +"</a>");
//	if(Session["cusotomer_id"] != "" && Session["customer_id"] != null)
//		Response.Write("&nbsp;&nbsp;| &nbsp;Repair# <i>(optional)</i><input type=text name=repair_id >");
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left>SN#:");
	Response.Write("</th><td><input size=42 type=text name=sn value='"+ EncodeQuote(m_current_sn) +"'>");
	Response.Write("<script language=javascript1.2>document.frm.sn.focus();</script");
	Response.Write(">");
	Response.Write("<input type=submit name=cmd value='Check SN#'"+ Session["button_style"] +"> ");
	Response.Write("</td></tr>");
	Response.Write("<tr><th align=left >PURCHASE INVOICE#:");
	Response.Write("</th><td><input type=text size=42 name=inv value='"+ m_po_number +"'></td></tr>");
	//Response.Write("</th><td><input type=text size=42 name=inv value='"+ m_current_inv +"'></td></tr>");
	Response.Write("<tr><th align=left >PURCHASE INV_DATE:");
	if(m_current_inv_date != null && m_current_inv_date != "")
		if(IsDate(m_current_inv_date))
			m_current_inv_date = (DateTime.Parse(m_current_inv_date)).ToString("dd-MM-yyyy");
	Response.Write("</th><td><input type=text size=42 name=inv_date ");
//		Response.Write(" onfocus=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.inv_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
	Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.inv_date','calendar_window','width=190,height=230');calendar_window.focus()\" ");
	Response.Write(" value='"+ m_current_inv_date +"' ></td></tr>");
	Response.Write("<tr><th align=left>PRODUCT CODE#:");
	Response.Write("</th><td><input type=text name=code size=42 value='"+ m_current_code +"'></td></tr>");
	Response.Write("<tr><th align=left>MANUFACTURER CODE#:");
	Response.Write("</th><td><input type=text name=supplier_code size=42 value='"+ m_current_supplier_code +"'></td></tr>");
	
	
	Response.Write("<tr><th align=left><font color=red>PRODUCT DESCRIPTION:");
	Response.Write("</th><td><textarea name=desc rows=6 cols=79 >"+ StripHTMLtags(EncodeQuote(m_current_desc)) +"</textarea></td></tr>");
	Response.Write("<tr><th align=left><font color=red>FAULT DESCRIPTION:");
	Response.Write("</th><td><textarea name=fault rows=6 cols=79 >"+ StripHTMLtags(EncodeQuote(m_current_fault)) +"</textarea></td></tr>");
	Response.Write("<tr><th colspan=2 align=center>");
	//Response.Write("<input type=button  value='<< Back to Created RA# List'"+ Session["button_style"] +" ");
	//Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"&ra=all');\"> ");
	Response.Write("<input type=submit name=cmd value='Add Faulty Item'"+ Session["button_style"] +" ");
	Response.Write(" onclick=\"if(document.frm.fault.value=='' || document.frm.desc.value=='' || document.frm.slt_supplier.value==''){window.alert('Please Select Supplier, and Fill Product Descriptions and Fault Descriptions'); return false;}\" ");
	Response.Write("> ");
	Response.Write("</td></tr>");
	
	Response.Write("</table>");
	Response.Write("</td></tr>");
//	Response.Write("</table>");	

	if(Session["ss_count"] != null && Session["ss_count"] != "") //(int)Session["ss_count"] > 0)
	{
		bool bAlter = false;
		
		Response.Write("<tr><td>");
		Response.Write("<br><table width=100% align=center cellspacing=0 cellpadding=3 border=1 bordercolor=#CCCCCC ");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><th align=left colspan=4>CURRENT ADDED FAULTY ITEM(S)</td>");
		Response.Write("<td colspan=5 align=right><input type=submit name=cmd value='Apply Supplier RA#' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"if(!confirm('Processing Supplier RMA... ')){return false;};\" ");
		Response.Write(">");
		Response.Write("</tr>");
		Response.Write("<tr bgcolor=#DED123 align=left><th>SN#</th><th>PURCHASE INVOICE#</th><th>PURCHASE INV_DATE</th><th>CODE#</th><th>M_P_CODE#</th><th>PRO_DESC</th><th>FAULT_DESC</th><th>FOR CUSTOMER</th><th>ACTION</th></tr>");

		for(int i=1; i<=(int)Session["ss_count"]; i++)
		{
			if(Session["ss_desc"+ i.ToString()] != null && Session["ss_desc"+ i.ToString()] != "")
			{
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#E3E3E3 ");
			Response.Write(">");
			bAlter = !bAlter;
			
			Response.Write("<td>"+ Session["ss_sn"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_supplier_inv"+ i.ToString()] +"</td>");
			//Response.Write("<td>"+ Session["ss_inv"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_inv_date"+ i.ToString()] +"</td>");
			
			Response.Write("<td>"+ Session["ss_code"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_supplier_code"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_desc"+ i.ToString()] +"</td>");
			Response.Write("<td>"+ Session["ss_fault"+ i.ToString()] +"</td>");
			Response.Write("<td>");
			Response.Write(" <a href=\"javascript:viewcard_window=window.open('viewcard.aspx?id=" + Session["ss_customer_id"+ i.ToString()] + "', '', 'width=350, height=350'); viewcard_window.focus()\" class=o>"+ Session["ss_customer_id"+ i.ToString()] +"</a>");
			Response.Write("</td>");
			//Response.Write("<td>"+ Session["ss_fault"+ i.ToString()] +"</td>");
			Response.Write("<th align=left><a title='delete this item' href='"+ Request.ServerVariables["URL"]+"?r="+DateTime.Now.ToOADate() +"");
			if(m_querystring != "")
				Response.Write("&ra="+ m_querystring +"");
			if(Request.QueryString["rid"] != null && Request.QueryString["rid"] != "")
				Response.Write("&rid="+ Request.QueryString["rid"] +"");
			if(Request.QueryString["ifra"] != null && Request.QueryString["ifra"] != "")
				Response.Write("&ifra="+ Request.QueryString["ifra"]);
			Response.Write("&del="+i+"' class=o><font color=red>X</a></font></td>");
			Response.Write("</tr>");
			}
		}
		Response.Write("</table>");
		Response.Write("</td></tr>");
	}
	Response.Write("</table></form>");
//	Response.Write("</form>");
	Response.Write("<br><br><br><br>");
	return true;
}

bool DoAddFaultyItemToSession()
{
	string sn = Request.Form["sn"];
	string code = Request.Form["code"];
	string inv = Request.Form["inv"];
	string inv_date = Request.Form["inv_date"];
	string desc = Request.Form["desc"];
	string fault = Request.Form["fault"];
//	string supplier_id = Request.Form["slt_supplier"];
	string supplier_code = Request.Form["supplier_code"];
	string purchase_order_id = Request.Form["purchase_order_id"];
	string customer_id = Request.Form["for_customer"];
	string supplier_inv = Request.Form["supplier_inv"];
//	string repair_id = Request.Form["repair_id"];

	int nSession = 0;
	bool IsDuplicate = false;
//DEBUG(" code = ", code);	
	int nCt = 0;
	int nlimit = 0;
	if(m_sSite.ToLower() == "admin")
		if(Session["ss_count"] != null && Session["ss_count"] != "")
			if((int)Session["ss_count"] > int.Parse(GetSiteSettings("ra_qty_limit", "10")))
				return false;
	if(desc != null && desc != "" && fault != null && fault != "")
	{
		if(Session["ss_count"] == null)
		{
			Session["ss_count"] = 1;
			Session["ss_sn1"] = sn;
			Session["ss_code1"] = code;
			Session["ss_inv1"] = inv;
			Session["ss_inv_date1"] = inv_date;
			Session["ss_desc1"] = desc;
			Session["ss_fault1"] = fault;
		//	Session["ss_supplier_id1"] = supplier_id;
			Session["ss_supplier_code1"] = supplier_code;
			Session["ss_purchase_order_id1"] = purchase_order_id;
			Session["ss_supplier_inv1"] = supplier_inv;
			//Session["ss_repair1"] = repair_id;
			
			if(customer_id != "" && customer_id != "" && customer_id != "all")
				Session["ss_customer_id1"] = customer_id;

			nSession = (int)Session["ss_count"];
//DEBUG("value = ", Session["ss_count"].ToString());
		}
		else
		{
			nSession = (int)Session["ss_count"] + 1;		
			for(int i=1; i<=nSession; i++)
			{	
				if(Session["ss_sn"+ i] != null && Session["ss_sn"+ i] != "")
				{
					if(sn == Session["ss_sn"+ i].ToString())
						IsDuplicate = true;
				}
				
			}
			if(!IsDuplicate)
			{
				Session["ss_count"] = nSession;
				Session["ss_sn"+ nSession] = sn;
				Session["ss_code"+ nSession] = code;
				Session["ss_inv"+ nSession] = inv;
				Session["ss_inv_date"+ nSession] = inv_date;
				Session["ss_desc"+ nSession] = desc;
				Session["ss_fault"+ nSession] = fault;
			//	Session["ss_supplier_id"+ nSession] = supplier_id;
				Session["ss_supplier_code"+ nSession] = supplier_code;
				Session["ss_purchase_order_id"+ nSession] = purchase_order_id;
				if(customer_id != "" && customer_id != "" && customer_id != "all")
				{
					Session["ss_customer_id"+ nSession] = customer_id;
			//		Session["ss_repair_id"+ nSession] = repair_id;
				}
				Session["ss_supplier_inv"+ nSession] = supplier_inv;
				

				Session["customer_id"] = null;//clean selected customer
				Session["customer_name"] = null;//clean selected customer
			}
			else
			{
				Response.Write("<script language=javascript1.2>window.alert('Duplicate SN#, Please Try Again')</script");
				Response.Write(">");
			}
		}
	}
	
	return IsDuplicate;
}

bool CheckFaultyItem()
{
//	DEBUG("sn = ", Request.Form["sn"].ToString());
	DataRow dr;
	dr = CheckSN(Request.Form["sn"]);
	if(dr != null)
	{
		m_current_sn = dr["sn"].ToString();
		m_current_inv_date = dr["date_received"].ToString();
		m_current_desc = dr["name"].ToString();
		m_current_inv = dr["po_number"].ToString();
		m_current_code = dr["code"].ToString();
//		m_current_supplier_id = dr["supplier_id"].ToString();
		m_current_supplier_code = dr["supplier_code"].ToString();
		m_purchase_order_id = dr["purchase_order_id"].ToString();
		m_po_number = dr["inv_number"].ToString(); //supplier invoice number
		if(Session["supplier_id"] == null || Session["supplier_id"] == "")
		{
			Session["supplier_id"] = dr["supplier_id"].ToString();
			Session["supplier_name"] = dr["supplier_name"].ToString();
		}
	}
	else
	{
		m_current_sn = Request.Form["sn"];
		m_current_inv_date = Request.Form["inv_date"];
		m_current_desc = Request.Form["desc"];
		m_current_inv = Request.Form["inv"];
		m_current_code = Request.Form["code"];
//		m_current_supplier_id = dr["supplier_id"].ToString();
		m_current_supplier_code = Request.Form["supplier_code"];
		m_current_fault = Request.Form["fault"];	
		
	}
	return true;
}

DataRow CheckSN(string sn)
{
	DataSet dscsn = new DataSet();
	
	sn = sn.ToUpper();
	if(sn != null && sn != "")
	{
	string sc = " SELECT s.sn, p.inv_number, p.id AS purchase_order_id, p.date_received, p.supplier_id, c2.company AS supplier_name ";
	sc += " , pi.code, pi.supplier_code, pi.name, p.po_number ";
	sc += " FROM stock s JOIN purchase p ON p.id = s.purchase_order_id ";
	sc += " JOIN purchase_item pi ON pi.id = p.id AND pi.id = s.purchase_order_id AND pi.code = s.product_code ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = p.supplier_id ";
	sc += " WHERE 1=1 ";
	sc += " AND UPPER(RTRIM(s.sn)) = '"+ sn +"'  ";
	sc += " ORDER BY p.date_received DESC ";
	
//DEBUG("sc=", sc);		
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dscsn) > 0)
			return dscsn.Tables[0].Rows[0];
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
	}
	}
	return null;
}

bool DoDeleteSessionItem()
{
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		Session["ss_sn"+ Request.QueryString["del"]] = null;
		Session["ss_code"+ Request.QueryString["del"]] = null;
		Session["ss_desc"+ Request.QueryString["del"]] = null;
		Session["ss_fault"+ Request.QueryString["del"]] = null;
		Session["ss_inv"+ Request.QueryString["del"]] = null;
		Session["ss_inv_date"+ Request.QueryString["del"]] = null;
	//	Session["ss_supplier_id"+ Request.QueryString["del"]] = null;
		Session["ss_supplier_code"+ Request.QueryString["del"]] = null;
		Session["ss_purchase_order_id"+ Request.QueryString["del"]] = null;
		Session["ss_customer_id"+ Request.QueryString["del"]] = null;
//		Session["ss_repair_id"+ Request.QueryString["del"]] = null;
	}
	int nCt = 0;
	int nNumber = 0;
	if(Session["ss_count"] != null && Session["ss_count"] != "")
	{	
		nCt = (int)Session["ss_count"];
		for(int i=1; i<=nCt; i++)
		{
			if(Session["ss_desc"+ i] != null && Session["ss_desc"+ i] != "" && Session["ss_fault"+ i] != null && Session["ss_fault"+ i] != "")
			{
				nNumber++;
//DEBUG("nnumber = ",nNumber);
				Session["ss_sn"+ nNumber] = Session["ss_sn"+ i];
				Session["ss_code"+ nNumber] = Session["ss_code"+ i];
				Session["ss_desc"+ nNumber] = Session["ss_desc"+ i];
				Session["ss_fault"+ nNumber] = Session["ss_fault"+ i];
				Session["ss_inv"+ nNumber] = Session["ss_inv"+ i];
				Session["ss_inv_date"+ nNumber] = Session["ss_inv_date"+ i];
		//		Session["ss_supplier_id"+ nNumber] = Session["ss_supplier_id"+ i];
				Session["ss_supplier_code"+ nNumber] = Session["ss_supplier_code"+ i];
				Session["ss_customer_id"+ nNumber] = Session["ss_customer_id"+ i];
				Session["ss_purchase_order_id"+ nNumber] = Session["ss_purchase_order_id"+ i];
				//Session["ss_repair_id"+ nNumber] = Session["ss_repair_id"+ i];
			}
		}

		if(nNumber >= 1)
			Session["ss_count"] = nNumber;
		else
			Session["ss_count"] = null;
	}
	
	return true;
	
}

void SessionCleanUp()
{
	
	if(Request.Form["cmd"] == "Apply Supplier RA#")
	{
		if(Session["ss_count"] != null && Session["ss_count"] != "")
		{
			for(int i=1; i<=(int)Session["ss_count"]; i++)
			{
				Session["ss_sn"+i] = null;
				Session["ss_sn"+ i] = null;
				Session["ss_code"+ i] = null;
				Session["ss_inv"+ i] = null;
				Session["ss_inv_date"+ i] = null;
				Session["ss_desc"+ i] = null;
				Session["ss_fault"+ i] = null;
		//		Session["ss_supplier_id"+ i] = null;
				Session["ss_supplier_code"+ i] = null;
				//Session["ss_repair_id"+ i] = null;
			}
		}
		Session["ss_count"] = null;
		
		//store supplier_id
		m_last_supplier_id = Session["supplier_id"].ToString();

		Session["supplier_id"] = null;
		Session["supplier_name"] = null;
	}

}

</script>
<asp:Label id=LFooter runat=server/>



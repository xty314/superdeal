<script runat="server">


DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_RMANO = "";
string m_sFault = "";			
string m_SuppEmail = "";
string m_supplier = "";
string header = "";
int m_nPageSize = 15;
int m_page = 1;

int m_ncheck =0; // check to add more sn# to session;
int m_nCheckSNReturn = 0;

string m_sort = "";
bool m_bDesc = false;

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
	//adding header for each RMA page
	header = ReadSitePage("rma_header");
	InitializeData();
	if(Request.QueryString["sp"] != null)
	{
		if(IsInteger(Request.QueryString["sp"]))
			m_page = int.Parse(Request.QueryString["sp"]);
	}
	//RememberLastPage();
	if(Request.Form["txtSN"] == null)
		Session["rma_sns"] = null; //clear for new search
	int rNumber = 0;
	if(!GetNextRMANO(ref rNumber))
			return;
	m_RMANO = rNumber.ToString();
	
	if(Request.Form["sltSupp"] != null)
		m_supplier = Request.Form["sltSupp"].ToString();

	if(Request.Form["cmd"] == "Send Faulty Parts")
	{
		if(!doUpdateSend())
			return;
	}

	if(Request.Form["cmd"] == "Update Record")
	{
		if(!updateRMARecord())
			return;
	}
	if(Request.Form["cmd"] == "Update to RA List")
	{
		if(!doInsertToRMA(m_RMANO))
			return;
		Response.Redirect("rma.aspx");
	}
	if(Request.Form["cmd"] == "Delete Record")
	{
		//Response.Write("deleted arleady");
		if(!doDeleteRecord())
			return;
	}
	if(Request.Form["cmd"] == "Send??" )
	{
		//Response.Write("send arleady");
		if(!autoSendMail())
			return;
	}
	if(Request.QueryString["deljob"] != null)
	{
		if(!doDeleteRA())
			return;
	}
	if(Request.Form["cmd"] == "Print Record" && (Request.QueryString["data"] != null && Request.QueryString["rma"] != null 
		&& Request.QueryString["rows"] != null))
	{
		
		if(!PrintNiceRMAForm())
			return;
	}
	if(Request.Form["cmd"] == "Finish / Replace Products")
	{
		if(!insertToStrok())
			return;
	}
	if(Request.Form["cmd"] == "Record")
	{
		if(!insertRMAStock())
			return;
		Response.Redirect("rma.aspx?task=edit&rma_nav=1");
	}
	if((Request.QueryString["data"] == "recorded" || Request.QueryString["cmd"] == "RMA# Search") && Request.Form["cmd"] != "Print Record") 
	{
		if(!printRMARecord())
			return;
	}
	else
	{
		if(Request.QueryString["supp"] != null && Request.QueryString["supp"] != "")
		{
			if(!listFaultProduct())
				return;
		}
		else
		{
			string s_id = "";
			if(Request.QueryString["new"] != "rma_new")
			{
				if(Request.QueryString["task"] != null)
				{
					if(Request.QueryString["search_code"] == "1")
					{
						if(!displayAllProduct())
							return;
					}
					else
					{
						if(!DisplayRMAList())
							return;
						if(Request.QueryString["rma_nav"] != "2")
							BindRMAList();
							//BindRMAListGrid();
						else
							BindReturnList();
					}
				}
				else
				{
					if(Request.QueryString["data"] != "recorded")
					{
						if(!getFaultyProduct())
							return;
						sortbySupplier();
					}
				}
			}
			else
			{
			
				if(Request.Form["txtSN"] != null)
				{
					if(!checkSerialNO())
						return;
				}
				getItems();
				
			}
		}
	}

}

bool doUpdateSend()
{
	int nrows = int.Parse(Request.Form["hidrow"].ToString());
	for(int i=0; i< nrows; i++)
	{
		string hidid = Request.Form["hidid"+ i.ToString()].ToString();
		string delivery = Request.Form["del"+ i.ToString()].ToString();
		string supp_ra = Request.Form["supp_ra"+ i.ToString()].ToString();
		string check = "";
		if(Request.Form["sel"+ i.ToString()] != null)
			check = Request.Form["sel"+ i.ToString()].ToString();
		//DEBUG(" check = ", check);
		if(check == "on")
		{
			string sc = " UPDATE rma ";
			sc += " SET check_status = 2 ";
			if(supp_ra != "")
				sc += " , supp_rmano = '"+ supp_ra +"' ";
			if(delivery != "")
				sc += " , pack_no = '"+ delivery +"' ";
			sc += " WHERE ra_id = "+ hidid +" ";
			string note = "Sent to Supplier with ticket#: "+ delivery +" and Supplier RA# : "+ supp_ra +" ";

			sc += AddRepairLogString(note, "", "", "", "", hidid);
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
bool displayAllProduct()
{
	string search = "";
	if(Request.Form["search"] != "" && Request.Form["search"] != null)
		search = Request.Form["search"].ToString();
	bool isNum = true;
	int ptr = 0;
	while (ptr < search.Length)
	{
		if (!char.IsDigit(search, ptr++))
		{
			isNum = false;
			break;
		}
	}
	string q_supp = "", q_suppcode = "", q_rma = "", q_selected = "";
	if(Request.QueryString["rma"] != null && Request.QueryString["rma"] != "")
		q_rma = Request.QueryString["rma"];
	if(Request.QueryString["sup"] != null && Request.QueryString["sup"] != "")
		q_supp = Request.QueryString["sup"];
	if(Request.QueryString["sup_code"] != null && Request.QueryString["sup_code"] != "")
		q_suppcode = Request.QueryString["sup_code"];
	if(Request.QueryString["selected"] != null && Request.QueryString["selected"] != "")
		q_selected = Request.QueryString["selected"];
//DEBUG("q_supp = ", q_supp);
	string sc = " SELECT DISTINCT TOP 100 product_code AS code, prod_desc AS name, supplier, supplier_code FROM stock ";
	if(search != "" && search != null)
	{
		if(isNum)
			sc += " WHERE product_code = "+ search +" ";
		else
			sc += "  WHERE prod_desc like '%"+search+"% '  OR supplier_code like '%"+search+"%' "; 
	}
	else
	{
		if(q_supp != "")
			sc += " WHERE supplier = '"+q_supp+"' ";
		else if(q_suppcode != "")
			sc += " WHERE supplier_code = '"+q_suppcode+"' ";
		else if(q_supp != "" && q_suppcode != "")
			sc += " WHERE supplier = '"+q_supp+"' AND supplier_code = '"+q_suppcode+"' ";
	}
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "allProduct"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0 )
	{
		
		sc = " SELECT DISTINCT TOP 100 code, name, brand, stock, supplier, supplier_code, supplier_price FROM product ";
		
		if(search != "")
		{
			//DEBUG("saerch = ", search);
			if(isNum)
				sc += " WHERE code = "+ search +" ";
			else
				sc += "  WHERE name like '%"+search+"% '  "; 
		}
	
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "allProduct"); 
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	//DEBUG("s= ", s);
	string search_code = Request.QueryString["search_code"];
	string q_nav = Request.QueryString["rma_nav"];
	Response.Write("<br>");
	Response.Write("<form name=frm method=post action='rma.aspx?search_code="+search_code+"&task=return&rma_nav="+q_nav+"&rma="+q_rma+"&sup="+q_supp+"&sup_code="+q_suppcode+"&selected="+q_selected+"' >");
	Response.Write("<center><h5>Search Result for all Products</h5></center>");
	Response.Write("<table align=center width=100% cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=4><input type=text name='search' value=''><input type=submit value='Search Product' "+ Session["button_style"] +"></td></tr>");
	Response.Write("<tr><td colspan=4>Top 100 rows returned</td></tr>");
	Response.Write("<tr bgcolor=#e3e3e3><th>Product#</th><th>Description</th><th>Supplier</th><th>Supplier Code</th></tr>");
	bool bChange = true;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["allProduct"].Rows[i];
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		//string brand = dr["brand"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		//string stock = dr["stock"].ToString();
		if(!bChange)
		{
			Response.Write("<tr bgcolor=#E3E3E3>");
			bChange = true;
		}
		else
		{
			Response.Write("<tr>");
			bChange = false;
		}
		Response.Write("<td><a title='select product' href='rma.aspx?task=return&rma_nav=2&rma="+q_rma+"&code="+code+"&selected="+q_selected+"'><font color=blue><b>"+code+"</b></font></a></td>");
		//Response.Write("<td><a title='select product' href='rma.aspx?"+s+"&code"+q_selected+"="+code+"'><font color=blue><b>"+code+"</b></font></a></td>");
		Response.Write("<td><a title='select product' href='rma.aspx?task=return&rma_nav=2&rma="+q_rma+"&code="+code+"&selected="+q_selected+"'><font color=blue><b>"+name+"</b></font></a></td>");
		Response.Write("<td>"+supplier+"</td>");
		Response.Write("<td><a title='select product' href='rma.aspx?task=return&rma_nav=2&rma="+q_rma+"&code="+code+"&selected="+q_selected+"'><font color=blue><b>"+supplier_code+"</b></font></a></td>");
		Response.Write("</tr>");
	}
	
	Response.Write("</table></form>");
	
	return true;
}

bool doDeleteRA()
{
	string delJob = Request.QueryString["deljob"].ToString();
	string sc = " DELETE FROM rma WHERE id = "+delJob+"";
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

bool insertToStrok()
{
	DataRow dr;
	string rma = "";
	int rows = 0;
	if(Request.QueryString["rma"] != null)
		rma = Request.QueryString["rma"].ToString();
	if(Request.QueryString["rows"] != null)
		rows = int.Parse(Request.QueryString["rows"].ToString());
	for(int i=0; i<rows; i++)
	{
		string hidsn = "", hidid = "";
		
		if(Request.Form["hidid"+i.ToString()] != null && Request.Form["hidid"+i.ToString()] != "")
			hidid = Request.Form["hidid"+i.ToString()].ToString();
		if(Request.Form["hidsn"+i.ToString()] != null && Request.Form["hidsn"+i.ToString()] != "")
			hidsn = Request.Form["hidsn"+i.ToString()].ToString();

		string sn = "", pdate = "", pcode = "", inv = "", supplier = "";
		string supp_code = "", pdesc = "", hidsup_ra = "", r_status = "";
		if(Request.Form["r_radio"+i.ToString()] != null && Request.Form["r_radio"+i.ToString()] != "")
			r_status = Request.Form["r_radio"+i.ToString()];
	//DEBUG("r_status = ", r_status);
		if(Request.Form["sn"+i.ToString()] != null && Request.Form["sn"+i.ToString()] != "")
			sn = Request.Form["sn"+i.ToString()].ToString();
		if(Request.Form["pdate"+i.ToString()] != null && Request.Form["pdate"+i.ToString()] != "")
			pdate = Request.Form["pdate"+i.ToString()].ToString();

		if(Request.Form["pcode"+i.ToString()] != null && Request.Form["pcode"+i.ToString()] != "")
			pcode = Request.Form["pcode"+i.ToString()].ToString();
		if(Request.Form["supplier"+i.ToString()] != null && Request.Form["supplier"+i.ToString()] != "")
			supplier = Request.Form["supplier"+i.ToString()].ToString();

		if(Request.Form["supp_code"+i.ToString()] != null && Request.Form["supp_code"+i.ToString()] != "")
			supp_code = Request.Form["supp_code"+i.ToString()].ToString();
		if(Request.Form["pdesc"+i.ToString()] != null && Request.Form["pdesc"+i.ToString()] != "")
			pdesc = Request.Form["pdesc"+i.ToString()].ToString();

		if(Request.Form["inv"+i.ToString()] != null && Request.Form["inv"+i.ToString()] != "")
			inv = Request.Form["inv"+i.ToString()].ToString();
		if(Request.Form["hidsup"] != null && Request.Form["hidsup"] != "")
			hidsup_ra = Request.Form["hidsup"].ToString();
		int nrow =0;
		string stock = Request.Form["stock"+i.ToString()].ToString();
		//DEBUG(" pcode = ", pcode);
		if(r_status == "1" || r_status == "4")
		{
			if(pcode != "" && stock == "1")
			{
				string sc = " SELECT product_code, purchase_order_id, supplier_code, supplier, purchase_date, prod_desc ";
				sc += " FROM stock  WHERE product_code = "+pcode+" ";
				//DEBUG(" sn = ", sn);
				//DEBUG(" sn = ", pcode);
			
				try
				{
					myAdapter = new SqlDataAdapter(sc, myConnection);
					nrow = myAdapter.Fill(dst, "getAllDetail"); 
				}
				catch(Exception e) 
				{
					ShowExp(sc, e);
					return false;
				}
				
				if(nrow > 0)
				{
					dr = dst.Tables["getAllDetail"].Rows[0];
					supplier = dr["supplier"].ToString();
					string purchase_order_id = dr["purchase_order_id"].ToString();
					if(purchase_order_id == null || purchase_order_id == "")
						purchase_order_id = inv;
					//DEBUG("purchase = ", purchase_order_id);
					//DEBUG("purchase = ", inv);
					supp_code = dr["supplier_code"].ToString();
					string product_code = dr["product_code"].ToString();
					string purchase_date = dr["purchase_date"].ToString();
					string prod_desc = dr["prod_desc"].ToString();
					//DEBUG("purchase = ", purchase_date);
					sc = " SET DATEFORMAT dmy ";
					sc += "INSERT INTO stock (supplier, product_code, branch_id, supplier_code, status, ";
					sc += " purchase_order_id, prod_desc, purchase_date) ";
					sc += "VALUES('"+supplier+"', '"+product_code+"', 1, '"+supp_code+"', 2, ";
					sc += " '"+purchase_order_id+"', '"+prod_desc+"', GETDATE())";
					//sc += " '"+purchase_order_id+"', '"+prod_desc+"', '"+purchase_date+"')";
					string note = ""+ product_code +" desc: "+ prod_desc +" sn# "+ sn +"";
					
					//add serial trace log 
					if(r_status == "1" && sn != "")
					{
						sc += AddSerialLogString("Items returned with Replacement Code: "+hidsn, "Fault Items returned and Replaced with new Items", "", "", "", hidsup_ra);
						sc += AddRepairLogString(note, inv, pcode, "", sn, rma);
					}
					if(r_status == "4" && sn != "")
					{
						sc += AddSerialLogString(hidsn, "Fault Items returned and Product Repaired", "", "", "", hidsup_ra);
						sc += AddRepairLogString("Item returned with repaired: "+note, inv, pcode, "", sn, rma);
					}
				
					if(g_bRetailVersion && stock == "1")
					{
						//---------------retail version-----------
						sc = "UPDATE stock_qty  SET qty = qty + 1";
						sc += " WHERE code = "+product_code+" ";
						//----------------end --------
					}
					else
					{					//------wholesale version---------
						if(stock == "1")
						{
							sc = "UPDATE product  SET stock = stock + 1";
							sc += " WHERE code = "+product_code+" ";
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
					if(!updateRMAS(hidid, r_status))
						return false;
									
				}
			}
			else if(r_status == "2" || r_status == "3")
			{
				
				Response.Write("<center><font color=red><h5>Please Enter a valid Product code To Process this Replacement</h5></font>");
				Response.Write("<input type=button value='<<back' "+ Session["button_style"] +" onclick=\"window.history.back()\"></center>");
				return false;
			}
		}
		else
		{
			//update status	
			if(!updateRMAS(hidid, r_status))
				return false;
		}
		if(stock == "0")
		{
			string sc = "UPDATE rma SET check_status = 3 ";
			if(r_status != "" && r_status != null)
				sc += " , status = "+ r_status+ "";
			sc += " WHERE id = "+hidid+"";
			sc += AddRepairLogString("Item process on: "+DateTime.Now.ToString()+"", "", "", "", "", hidid);
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

bool updateRMAS(string hidid, string status)
{
	string sc = "UPDATE rma SET check_status = 3 ";
	if(status != null && status != "")
		sc += " , status = "+ status+ "";
	sc += " WHERE id = "+hidid+"";
	if(status == "3")
		sc += AddRepairLogString("Faulty item is credit: ", "", "", "", "", hidid);
	if(status == "1")
		sc += AddRepairLogString("Item out of Warranty: ", "", "", "", "", hidid);
	
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
bool BindReturnList()
{
	Response.Write("<br>");
	Response.Write("<table align=center width=100% cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	if(dst.Tables["rmalist"].Rows.Count > 0)
	{	
		Response.Write("<tr><td valign=left width=25%>");
		Response.Write("<table cellspacing=1 cellpadding=2 border=1 bordercolor=black bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#E3E3E3><th>No#</th><th>RA#</th><th>RA Date</th><th>Supp RA#</th></tr>");
		 
		for(int i=0; i<dst.Tables["rmalist"].Rows.Count; i++)
		{
			DataRow dr = dst.Tables["rmalist"].Rows[i];
			string ra_id = dr["RMA NO"].ToString();
			string ra_date = dr["RMA Date"].ToString();
			string sup_rma = dr["supp_rmano"].ToString();
			string supplier = dst.Tables["rmalist"].Rows[i]["supplier"].ToString();

			Response.Write("<tr><td align=center bgcolor=#E3E3E3>"+(i+1).ToString()+"</td><td width=15%><a href='rma.aspx?task=return&rma_nav=2&rma="+ra_id+"&supplier="+ supplier +"'><font color=blue><b>"+ra_id+"</font></b></a>");
			Response.Write("<br></td><th>"+ra_date+"<br>");
			Response.Write("&nbsp;<input type=button value='View Log' "+ Session["button_style"] +" alt='view repair log' ");
			Response.Write(" onclick=\"javascript:repair_log_window=window.open('repair_log.aspx?ra_job="+ra_id+"', '', '')\" >");
			Response.Write("</th><th><a href='rma.aspx?task=return&rma_nav=2&rma="+ra_id+"&supplier="+ supplier +"'><font color=blue><b>"+sup_rma+"</font></b></a><br>");
			Response.Write("<input type=button value='View Status' "+ Session["button_style"] +" ");
			Response.Write(" onclick=\"javascript:view_ra_window=window.open('view_ra.aspx?ra=supplier&id="+ ra_id +"','', 'width=650,height=450')\" >");
			Response.Write("</th></tr>");
			Response.Write("<input type=hidden name=hidsup value='"+sup_rma+"'>");
		}
		Response.Write("</table>");
		Response.Write("</td><td valign=left>");
		Response.Write("<table width=100% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		
		if(Request.QueryString["rma"] != null)
		{	
			if(!getReturnProduct())
				return false;
			string rma = Request.QueryString["rma"].ToString();
			int nRows = dst.Tables["getReturn"].Rows.Count;
			Response.Write("<form name=frm method=post action='rma.aspx?task=return&rma_nav=2&rma="+rma+"&rows="+nRows.ToString()+"'>");
			//Response.Write("<tr bgcolor=#E3E3E3><th>JOB#</th><th>Supplier</th><th>Supp Code</th><th>Product Code</th>");
			//Response.Write("<th>Prod Desc</th><th>Inv#</th><th>Purchase Date</th><th>SN#</th></tr>");
			Response.Write("<tr><th colspan=5 bgcolor=#E3E3E3>Insert New Returned Product to Stock</th></tr>");
			for(int i=0; i<dst.Tables["getReturn"].Rows.Count; i++)
			{
				DataRow dr = dst.Tables["getReturn"].Rows[i];
				string supplier = dr["supplier"].ToString();
				string stock = dr["stock_check"].ToString();
				string pcode = dr["p_code"].ToString();
				string purchase_date = dr["purchase_date"].ToString();
				string inv = dr["invoice_number"].ToString();
				string warranty = dr["warranty"].ToString();
				string pdesc = dr["name"].ToString();
				string sn = dr["serial_number"].ToString();
				string fault = dr["fault_desc"].ToString();
				string repair = dr["repair_jobno"].ToString();
				string id = dr["id"].ToString();
				
						
				Response.Write("<tr><th bgcolor=#E3E3E3 rowspan=5 width=3%>Item# "+(i+1).ToString()+"</th><td bgcolor=#E3E3E3>Supplier Name:<td><input type=text name='supplier"+i.ToString()+"' value='"+supplier+"'></td>");
				Response.Write("<td bgcolor=#E3E3E3>Purchase Date</td><td><input type=text name='pdate"+i.ToString()+"' value='"+purchase_date+"'>");
				Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.pdate"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\">");
				Response.Write("<font color=blue><b>...</font></b>");
				Response.Write("</td></tr>");
				Response.Write("<tr><td bgcolor=#E3E3E3>Supplier Code</td><td><input type=text name='supp_code"+i.ToString()+"' value='"+pcode+"'></td>");
				Response.Write("<td bgcolor=#E3E3E3>INV#</td><td><input type=text name='inv"+i.ToString()+"' value='"+inv+"'></td></tr>");
				Response.Write("<tr><td bgcolor=#E3E3E3>Old SN#</td><td>"+sn+"</td>");
				Response.Write("<td bgcolor=#E3E3E3>Description</td><td><input type=text name='pdesc"+i.ToString()+"' value='"+pdesc+"'></td></tr>");
								
				if(stock == "1")
				{
					Response.Write("<tr><td bgcolor=#E3E3E3><b>Return Product Status</b></td><td colspan=2><input type=radio name='r_radio"+i.ToString()+"' value='1' checked>Replace");
					Response.Write("<input type=radio name='r_radio"+i.ToString()+"' value='2'>Credit<input type=radio name='r_radio"+i.ToString()+"' value='3'>Out of Warranty");
					Response.Write("<input type=radio name='r_radio"+i.ToString()+"' value='4'>Repaired</td>");
					Response.Write("<th><font color=red>Product is For Stock</td></tr>");
					string selected = "", code = "";
					if(Request.QueryString["selected"] != null)
						selected = Request.QueryString["selected"].ToString();
					if(Request.QueryString["code"] != null)
						code = Request.QueryString["code"].ToString();
					if(selected == i.ToString())						
						Response.Write("<tr><td bgcolor=#E3E3E3><b><font color=red>Input Product Code#</font></b></td><td><input type=text name='pcode"+i.ToString()+"' value='"+code+"'>");
					else
						Response.Write("<tr><td bgcolor=#E3E3E3><b><font color=red>Input Product Code#</font></b></td><td><input type=text name='pcode"+i.ToString()+"' value=''>");
					//Response.Write("<input type=button value='...' "+Session["button_style"]+" Onclick=\"window.location=('rma.aspx?search_code=1&sup"+i.ToString()+"="+supplier+"&sup_code"+i.ToString()+"="+pcode+"&selected="+i.ToString()+"&"+s_query+"')\"></td>");
					Response.Write("<input type=button value='...' "+Session["button_style"]+" Onclick=\"window.location=('rma.aspx?search_code=1&task=return&rma_nav=2&rma="+rma+"&sup="+supplier+"&sup_code="+pcode+"&selected="+i.ToString()+"')\"></td>");
					Response.Write("<td bgcolor=#E3E3E3><b>Input New SN#:</b></td><td><input type=text name='sn"+i.ToString()+"' value=''></td></tr>");
					
					Response.Write("\n\r<input type=hidden name='hidsn"+i.ToString()+"' value='"+sn+"'>");
					Response.Write("<tr><th colspan=5 bgcolor=#E3E3E3>&nbsp;</th></tr>");
					
				}
				else
				{
					Response.Write("<tr>");
					Response.Write("<th colspan=4><font color=red>Item is for Customer: <a title='Click to view Customer detail' href='techrma.aspx?job="+ repair +"' class=o target=_blank>"+ repair +"</a></td></tr>");
				}
				
				Response.Write("<input type=hidden name='hidid"+i.ToString()+"' value='"+id+"'>\r\n");
				Response.Write("<input type=hidden name=stock"+i.ToString()+" value="+ stock +">");
			}
			Response.Write("<tr><td colspan=5 align=right><input type=submit name=cmd value='Finish / Replace Products' "+Session["button_style"]+"></td></tr>");
		}
		Response.Write("</table>");
		Response.Write("</tr>");
	}
	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}

bool getReturnProduct()
{
	string s_no = ""; 
	if(Request.QueryString["rma"] !=null)
		s_no = Request.QueryString["rma"];
		
	string sc = "SELECT r.id, r.check_status, r.repair_jobno, r.supplier, r.pack_no, r.authorize, r.supp_rmano, r.warranty, r.stock_check, r.p_code, CONVERT(varchar(12),r.repair_date,13) as repair_date ";
	sc += " , r.ra_id, r.invoice_number, CONVERT(varchar(12),r.purchase_date, 13) as purchase_date, r.fault_desc, ";
	sc += " r.serial_number, CONVERT(varchar(50), r.product_desc) AS name ";
	sc += "FROM rma r "; //inner join repair rp on r.serial_number=rp.serial_number ";
	sc += " WHERE r.ra_id = "+s_no+" ";
	sc += " AND r.check_status = 2 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "getReturn"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

//--------insert ra faulty product to rma table and send email to supplier---------
bool doInsertToRMA(string s_rmano)
{
	//update repair table to set faulty product is deactivated
	string sn = "", inv = "", pdate = "", supplier_code = "", desc = "", fault = "", id = "";
	string supplier = "", technician = "";
	int tatoljob = 0;
	string ss_supp = "";
	if(Request.QueryString["count"] != null && Request.QueryString["count"] != "")
		tatoljob = int.Parse(Request.QueryString["count"].ToString());
	string supp_rma = "", person="", pack="", update_date ="";
	if(Request.Form["ra"] != null && Request.Form["ra"] != "")
		supp_rma = Request.Form["ra"].ToString();
	if(Request.Form["ticket"] != null && Request.Form["ticket"] != "")
		pack = Request.Form["ticket"].ToString();
	if(Request.Form["person"] != null && Request.Form["person"] != "")
		person = Request.Form["person"].ToString();
	update_date = Request.Form["date"].ToString();
	technician = Session["email"].ToString();
	if(Request.QueryString["supp"] != null && Request.QueryString["supp"] != "")
		ss_supp = Request.QueryString["supp"].ToString();
//DEBUG("ss_supp = ", ss_supp);
	if(ss_supp == "UNKNOWN")
	{
		for(int i=0; i<tatoljob; i++)
		{
			if(Request.Form["fault"+i.ToString()] != null && Request.Form["fault"+i.ToString()] != "")
				fault = Request.Form["fault"+i.ToString()].ToString();
			if(Request.Form["sn"+i.ToString()] != null && Request.Form["sn"+i.ToString()] != "")
				sn = Request.Form["sn"+i.ToString()].ToString();
						
			if(Request.Form["supplier"+i.ToString()] != null && Request.Form["supplier"+i.ToString()] != "")
				supplier = Request.Form["supplier"+i.ToString()].ToString();
			if(Request.Form["id"+i.ToString()] != null && Request.Form["id"+i.ToString()] != "")
				id = Request.Form["id"+i.ToString()].ToString();
		//DEBUG("id = ", id);
			string sc = " UPDATE repair ";
			sc += " SET serial_number = '"+sn+"' , note = '"+fault+"', supplier = '"+supplier+"' ";
			sc += " WHERE id = "+id+"";
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
	else
	{
		for(int i=0; i<tatoljob; i++)
		{
			if(Request.Form["fault"+i.ToString()] != null && Request.Form["fault"+i.ToString()] != "")
				fault = msgEncode(Request.Form["fault"+i.ToString()].ToString());
			if(Request.Form["sn"+i.ToString()] != null && Request.Form["sn"+i.ToString()] != "")
				sn = Request.Form["sn"+i.ToString()].ToString();
		
			if(Request.Form["inv"+i.ToString()] != "")
				inv = Request.Form["inv"+i.ToString()].ToString();
			else
				inv = "";
			if(Request.Form["pdate"+i.ToString()] != "")
				pdate = Request.Form["pdate"+i.ToString()].ToString();
			else 
				pdate = "";
			if(Request.Form["supplier_code"+i.ToString()] != null && Request.Form["supplier"+i.ToString()] != "")
				supplier_code = msgEncode(Request.Form["supplier_code"+i.ToString()].ToString());
			else
				supplier_code = "";
			if(Request.Form["desc"+i.ToString()] != null && Request.Form["desc"+i.ToString()] != "")
				desc = msgEncode(Request.Form["desc"+i.ToString()].ToString());
			
			if(Request.QueryString["supp"] != "UNKNOWN")
				supplier = Request.QueryString["supp"].ToString();
			else if(Request.Form["supplier"+i.ToString()] != null && Request.Form["supplier"+i.ToString()] != "")
				supplier = Request.Form["supplier"+i.ToString()].ToString();
			if(Request.Form["id"+i.ToString()] != null && Request.Form["id"+i.ToString()] != "")
				id = Request.Form["id"+i.ToString()].ToString();
			string sc = "SELECT serial_number FROM rma WHERE serial_number = '"+sn+"'";
			try
			{
				myAdapter = new SqlDataAdapter(sc, myConnection);
				myAdapter.Fill(dst, "chkSN"); 
			}
			catch(Exception e) 
			{
				ShowExp(sc, e);
				return false;
			}

			if(dst.Tables["chkSN"].Rows.Count <=0)
			{
				sc = "SET DATEFORMAT dmy ";
				sc += "INSERT INTO rma (ra_id, serial_number, fault_desc, repair_date, technician ";
				sc += " , invoice_number, pack_no, supp_rmano, supplier, product_desc, purchase_date, ";
				sc += " stock_check, authorize, p_code, check_status, repair_jobno)";
				sc += " VALUES("+s_rmano+"";
				sc += " ,'"+sn+"','"+fault+"', '"+update_date+"', '"+technician+"' ";
				sc += " , '"+inv+"', '"+pack+"', '"+supp_rma+"', '"+supplier+"', '"+desc+"', '"+pdate+"'";
				sc += " , 1, '"+person+"', '"+supplier_code+"', 1, "+id+" )";
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
				sc = " UPDATE repair ";
				sc += " SET accepted = 1 ";
				sc += " WHERE id = "+id+"";
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
			else
				Response.Write("<center><font color=red><h5>SN# is already Exist</h5></font></center>");
		}
	}
	
	return true;
}
//--------END HERE--------------------------
//-------- arrange all fault product -------------
bool listFaultProduct()
{
	DataRow dr;
	string s_getSupp = (Request.QueryString["supp"].ToString()).ToUpper();
	string sc = " SELECT id, serial_number, invoice_number, customer_id,  note, code, supplier_code";
	sc += " ,CONVERT(varchar(50),prod_desc)  AS Description , supplier";
	sc += " FROM repair ";
	sc += " WHERE for_supp_ra = 1 ";
	sc += " AND accepted = 0 ";
	sc += " AND supplier = '"+s_getSupp+"' ";
	
	Response.Write("<br>");
	Response.Write("<table width=80% align=center cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "listFault");
		Response.Write("<form name=frm method=post action='rma.aspx?count="+rows+"&supp="+s_getSupp+"'>");
		if(rows > 0)
		{			
			if(s_getSupp == "UNKNOWN")
			{
				Response.Write("<tr ><th colspan=7><font size=2> Re-allocate Faulty Product for Supplier</font></th></tr>");
				Response.Write("<tr><th>Date:</th><td colspan=6>"+DateTime.Now.ToString("dd/MM/yyyy"));
				
				Response.Write("</tr>");
				Response.Write("<tr><td colspan=7>&nbsp;</td></tr>");
				
				Response.Write("<tr bgcolor=#E3E3E3><th width=5% align=left>NO#</th><th>SN#</th><th>SUPP_INV#</th><th>Purchase Date</th><th>Supplier Code</th><th>Description</th>");
				Response.Write("<th>Supplier</th></tr>");
				if(!getSupplierDetails(s_getSupp))
						return false;
				for(int i=0; i<rows; i++)
				{	
					dr = dst.Tables["listFault"].Rows[i];
					string sn = dr["serial_number"].ToString();
					//string inv = dr["invoice_number"].ToString();
					string fault = dr["note"].ToString();
					string code = dr["code"].ToString();
					string supplier_code = dr["supplier_code"].ToString();
					string desc = dr["Description"].ToString();
					//string supplier = dr["supplier"].ToString();
					string id = dr["id"].ToString();
					Response.Write("<tr><td size=5%>"+(i+1).ToString()+"</td>");
					Response.Write("<td><input type=text name=sn"+i+" value="+sn+"></td>");
					Response.Write("<td><input type=text name=inv"+i+" value='' ></td>");
					Response.Write("<td><input type=text name=pdate"+i+" value=''>");
					Response.Write("<input type=button value='...' "+ Session["button_style"] +" ");
					Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.date','calendar_window','width=190,height=230');calendar_window.focus()\"></td>");
					Response.Write("<td><input type=text name=supplier_code"+i+" value='"+supplier_code+"'></td>");
					Response.Write("<td><input type=text name=desc"+i+" value='"+desc+"'></td>");
					Response.Write("<td><select name=supplier"+i+">");
					for(int j=0; j<dst.Tables["cardSupplier"].Rows.Count; j++)
					{
						dr = dst.Tables["cardSupplier"].Rows[j];
						string supplier = dr["short_name"].ToString();
						string company = dr["company"].ToString();
						string supp_id = dr["id"].ToString();
				//DEBUG("supplier =", supplier);
						Response.Write("<option value='"+supplier+"'>"+company.ToUpper()+"</option>");
					}
					Response.Write("</select>");
					//	<input type=text name=supplier"+i+" value="+supplier+"></td></tr>");
					Response.Write("<input type=hidden name=id"+i+" value="+id+">");
					Response.Write("<tr><th colspan=2>Fault Description</th><td colspan=5><input type=text size=50% name=fault"+i+" value='"+fault+"'>&nbsp;&nbsp;</td></tr>");
						
				}
				Response.Write("<tr><td colspan=8 align=right><input type=submit name=cmd value='Update to RA List'"+Session["button_style"]+"></td></tr>");
				
			}
			else
			{
				if(!getSupplierDetails(s_getSupp))
					return false;
				string email = "";
				if(dst.Tables["cardSupplier"].Rows.Count > 0)
				{
					dr = dst.Tables["cardSupplier"].Rows[0];
					string name = dr["name"].ToString();
					email = dr["email"].ToString();
					string company = dr["company"].ToString();
					string address1 = dr["address1"].ToString();
					string address2 = dr["address2"].ToString();
					string phone = dr["phone"].ToString();
					string fax = dr["fax"].ToString();
					Response.Write("<tr bgcolor=#E3E3E3><th colspan=7><font size=2> Insert Faulty Products to RMA List </font></th></tr>");
					Response.Write("<tr><td colspan=7>");
					Response.Write("<table width=80% align=center cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
					Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
					Response.Write("<tr><th align=left colspan=4>Supplier Details</th></tr>");
					Response.Write("<tr><td>"+name+"</td><td>&nbsp;</td><th align=right>RA#(from Supplier)</th><td><input type=text name=ra></td></tr>");
					Response.Write("<tr><td>"+company+"</td><td>&nbsp;</td><th align=right>Packslip Ticket#</th><td><input type=text name=ticket></td></tr>");
					Response.Write("<tr><td>"+address1+"</td><td>&nbsp;</td><th align=right>Contact Person</th><td><input type=text name=person></td></tr>");
					Response.Write("<tr><td>"+address2+"</td><td>&nbsp;</td><th align=right>Date</th>");
					Response.Write("<td><input type=text name=date value='"+DateTime.Now.ToString("dd/MM/yyyy")+"'>");
					//Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.date','calendar_window','width=190,height=230');calendar_window.focus()\">");
					//Response.Write("<font color=blue><b>...</font></b>");
					Response.Write("</td></tr>");
					Response.Write("<tr><td>"+email+"</td><td>&nbsp;</td><td></td><td></td></tr>");
					Response.Write("<tr><td>ph: "+phone+"</td><td>fax: "+fax+"</td><td>&nbsp;</td></tr>");
										
					Response.Write("<tr><td>&nbsp;</td><td>&nbsp;</td><td></td><td></td></tr>");
					Response.Write("</table>");
					Response.Write("</td></tr>");
				}
				Response.Write("<tr bgcolor=#E3E3E3><td width=5% align=left>NO#</td><th>SN#</th><th>SUPP_INV#</th><th>Purchase Date</th><th>Supplier Code</th><th>Description</th>");
				for(int i=0; i<rows; i++)
				{					
					//Response.Write("<th>&nbsp;</th></tr>");
					Response.Write("</tr>");
					dr = dst.Tables["listFault"].Rows[i];
					string sn = dr["serial_number"].ToString();
					string inv = dr["invoice_number"].ToString();
					string fault = dr["note"].ToString();
					string code = dr["code"].ToString();
					string supplier_code = dr["supplier_code"].ToString();
					string desc = dr["Description"].ToString();
					string supplier = dr["supplier"].ToString();
					string id = dr["id"].ToString();
					
					Response.Write("<tr><td size=2%>"+(i+1).ToString()+"</td>");
					Response.Write("<td><input type=text name=sn"+i+" value="+sn+"></td>");
					//Response.Write("<td><input type=text name=inv"+i+" value="+inv+"></td>");
					Response.Write("<td><input type=text name=inv"+i+" value='' ></td>");
					Response.Write("<td><input type=text name=pdate"+i+" value=''>");
					//Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.pdate"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\">");
					//Response.Write("<font color=blue><b>...</font></b></td>");
					Response.Write("<input type=button value='...' "+ Session["button_style"] +" ");
					Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.date','calendar_window','width=190,height=230');calendar_window.focus()\"></td>");
						//Response.Write("<td><input type=text name=code"+i+" value='"+code+"'></td>");
					Response.Write("<td><input type=text name=supplier_code"+i+" value='"+supplier_code+"'></td>");
					Response.Write("<td><input type=text name=desc"+i+" value='"+desc+"'></td></tr>");
					//Response.Write("<td><input type=text name=supplier"+i+" value="+supplier+"></td></tr>");
					Response.Write("<input type=hidden name=id"+i+" value="+id+">");
					Response.Write("<tr><th colspan=2>Fault Description</th><td colspan=4><input size=50% type=text name=fault"+i+" value='"+fault+"'></td></tr>");
				}
				Response.Write("<tr><td colspan=7 align=right><input type=submit name=cmd value='Update to RA List'"+Session["button_style"]+"></td></tr>");
			}
		}
	}
	catch(Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}
bool getSupplierDetails(string supp)
{
	string sc = " SELECT DISTINCT id, name, email, CONVERT(varchar(10),company) as company, address1, address2, phone, fax, short_name";
	sc += " FROM card ";
	if(supp != "UNKNOWN")
		sc += " WHERE short_name = '"+ supp +"' ";
	else
		sc += " WHERE type = 3 ";
	try
	{	
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "cardSupplier");		
	}
	catch(Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}
//-------------------------start get faulty RA list -------------------------------------
bool getFaultyProduct()
{
	string sc = " SELECT id, serial_number, invoice_number, customer_id,  note AS 'Fault Desc', code, supplier_code";
	sc += " ,CONVERT(varchar(60),prod_desc) AS 'Description' , supplier";
	sc += " FROM repair ";
	sc += " WHERE for_supp_ra = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "getRAList");
		if(rows > 0)
		{
			//update supplier to repair table 
			for(int i=0; i<rows; i++)
			{
				DataRow dr = dst.Tables["getRAList"].Rows[i];
				string supplier = dr["supplier"].ToString();
				string sn = dr["serial_number"].ToString();
				string code = dr["code"].ToString();
				string supplier_code = dr["supplier_code"].ToString();
				string inv = dr["invoice_number"].ToString();
				string id = dr["id"].ToString();
			//DEBUG("sn +=-", sn);
			//DEBUG("inv +=-", inv);
				if(supplier.ToUpper() == "UNKNOWN" || (supplier == null || supplier == ""))
				{
					if((sn == "" && sn != null) || (inv == "" && inv == null))
					{
						sc = " UPDATE repair SET supplier = 'Unknown' ";
						sc += " WHERE id = "+id+" ";
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
					else
					{
						if((sn != null && sn != ""))
						{
							sc = " SELECT supplier FROM stock ";
							sc += " WHERE sn = '"+sn+"' ";
							try
							{
								myAdapter = new SqlDataAdapter(sc, myConnection);
								myAdapter.Fill(dst, "getSupplier");
								//no supplier found on stock table check next sales_serial table
								if(dst.Tables["getSupplier"].Rows.Count < 0)
								{
									sc = "SELECT invoice_number FROM sales_serial ";
									sc += " WHERE sn = '"+sn+"' ";
									try
									{
										myAdapter = new SqlDataAdapter(sc, myConnection);
										myAdapter.Fill(dst, "getSupplier");
										if(dst.Tables["getSupplier"].Rows.Count > 0)
										{
											dr = dst.Tables[0].Rows[0];
											string ss_inv = dr["invoice_number"].ToString();
											sc = " SELECT supplier FROM sales ";
											sc += " WHERE invoice_number = "+ss_inv+"";
											try
											{
												myAdapter = new SqlDataAdapter(sc, myConnection);
												myAdapter.Fill(dst, "getSupplier");
												dr = dst.Tables["getSupplier"].Rows[0];
												string ss_supp = dr["supplier"].ToString();
											//DEBUG("ss_upp = ", ss_supp);
												sc = " UPDATE repair SET supplier = '"+ss_supp+"' ";
												sc += " WHERE id = "+id+" ";
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
											catch(Exception e) 
											{
												ShowExp(sc, e);
												return false;
											}
										}
										else
										{
											sc = " UPDATE repair SET supplier = 'Unknown' ";
											sc += " WHERE id = "+id+" ";
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
									catch(Exception e) 
									{
										ShowExp(sc, e);
										return false;
									}
								}
								else
								{
									if(dst.Tables["getSupplier"].Rows.Count > 0)
									{
										dr = dst.Tables["getSupplier"].Rows[0];
										string supp = dr["supplier"].ToString();
									//DEBUG("supp = ", supp);
										sc = " UPDATE repair SET supplier = '"+supp+"' ";
										sc += " WHERE id = "+id+" ";
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
							}
							catch(Exception e) 
							{
								ShowExp(sc, e);
								return false;
							}
						}
						//inv_number is null, so update supplier to repair table
						if(inv != "" && inv != null)
						{	
							sc = " SELECT supplier FROM sales ";
							sc += " WHERE invoice_number = "+ inv +"";
							try
							{
								myAdapter = new SqlDataAdapter(sc, myConnection);
								myAdapter.Fill(dst, "getSupplier");
								if(dst.Tables["getSupplier"].Rows.Count > 0)
								{
									dr = dst.Tables["getSupplier"].Rows[0];
									string s_supp = dr["supplier"].ToString();
									sc = " UPDATE repair SET supplier = '"+s_supp+"' ";
									sc += " WHERE id = "+id+" ";
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
								else
								{
									sc = " UPDATE repair SET supplier = 'Unknown' ";
									sc += " WHERE id = "+id+" ";
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
							catch(Exception e)
							{
								ShowExp(sc, e);
								return false;
							}
						}
					}
				}
			}
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool sortbySupplier()
{
	string sc = " SELECT distinct supplier ";
	sc += " FROM repair ";
	sc += " WHERE for_supp_ra = 1 ";
	sc += " AND accepted = 0 ";
	sc += " ORDER BY supplier DESC";
	//sc += " ORDER BY 'Purchase Date' ASC";
	Response.Write("<form name=frm method=post>");
	Response.Write("<table valign=top align=center width=50% cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int rows = myAdapter.Fill(dst, "Supplier");
		if(rows > 0)
		{		
			Response.Write("<br>");
			Response.Write("<center><h5><u>There are Faulty Products waiting to Process</u></h5></center>");
			Response.Write("<br>");
			Response.Write("<tr><th colspan=4 align=left bgcolor=#E3E3E3>Group Faulty Products by Supplier Name</th></tr>");
			Response.Write("<tr><td colspan=4><hr size=1 color=black></td></tr>");
			for(int i=0; i<rows; i++)
			{
				DataRow dr = dst.Tables["Supplier"].Rows[i];
				string supplier = (dr["supplier"].ToString()).ToUpper();
				Response.Write("<tr><td>"+(i+1).ToString()+".</td><td><a title='group by "+supplier+"' href='rma.aspx?supp="+supplier+"'><font color=blue><b>"+supplier+"</b></font></a></td>");
				Response.Write("<td colspan=2>Select by "+supplier+"</td></tr>");
			}
		}
		else
		{
			Response.Write("<br>");
			Response.Write("<center><h5><font color=red><b>CURRENTLY THERE IS NO FAULTY PRODUCTS from Repairing</b></font></h5></center>");
		}
		Response.Write("<tr><td colspan=4><hr size=1 color=black></td></tr>");
		Response.Write("<tr><th colspan=4 align=left bgcolor=#E3E3E3>RA Task</th></tr>");
		Response.Write("<tr><td colspan=4><hr size=1 color=black></td></tr>");
		//Response.Write("<tr><td>&nbsp;</td><td>&nbsp;</td><td>No of Item</td><td>&nbsp;</td></tr>");
		//Response.Write("<tr><td colspan=2><a title='New RA Products' href='rma.aspx?task=new'>\n\r");
		Response.Write("<tr><td colspan=2><a title='New RA Products' href='rma.aspx?new=rma_new'>\n\r");
		Response.Write("<font color=blue><b>New RA</b></font></a></td>");
		/*Response.Write("<td><select name=item>");
		for(int j=1; j<11; j++)
			Response.Write("<option value='"+j+"'>"+j.ToString()+"</option>");
		Response.Write("</select>");
		*/
		Response.Write("<td>Select to Enter New RA Products</td></tr>");
		Response.Write("<tr><td colspan=2><a title='Edit Faulty Product in RA Stock' href='rma.aspx?task=edit&rma_nav=1'><font color=blue><b>Waiting 4 RMA</b></font></a></td>");
		Response.Write("<td>Select to Edit Faulty Products</td></tr>");
		Response.Write("<tr><td colspan=2><a title='Edit Return Product from Supplier' href='rma.aspx?task=return&rma_nav=2'><font color=blue><b>Waiting 4 Return</b></font></a></td>");
		Response.Write("<td>Select to Update/Edit Returned Products</td></tr>");
		Response.Write("<tr><td colspan=2><a title='Check or Edit Finished Product' href='rma.aspx?task=done&rma_nav=3'><font color=blue><b>RMA Finish</b></font></a></td>");
		Response.Write("<td>Select to View all RA JOB</td></tr>");

	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("</table>");
	Response.Write("</form>");
	return true;
}
//----------------------------End UPDATE all faulty product with supplier---------------------------------------
//-----------------start group faulty product by supplier---------------------

bool doDeleteRecord()
{
	string rma_no = "";
	if(Request.QueryString["rma"] != null && Request.QueryString["rma"] != "")
	{
		rma_no = Request.QueryString["rma"].ToString();
	
		string sc = "DELETE FROM rma ";
		sc += " WHERE ra_id = "+ rma_no + "";
		//add trace for rma task
		sc += AddRepairLogString("Delected RMA# "+rma_no+"","","","","",rma_no);
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

bool PrintNiceRMAForm()
{
	if(!getRMANO())
		return true;
	Response.Write("<form name=frm>");
	Response.Write("<html>");
	Response.Write("<body onload='window.print()'>");
	//Response.Write("<form name=frmPrint methord=post onload='window.print()'>");
	if(dst.Tables["getRMA"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["getRMA"].Rows[0];
		string s_id = dr["ra_id"].ToString();
		string s_supprma = dr["supp_rmano"].ToString();
		string s_pack = dr["pack_no"].ToString();
		string s_authorize = dr["authorize"].ToString();
		string s_rdate = dr["repair_date"].ToString();
		string s_supplier =  dr["supplier"].ToString();
		string s_company = dr["trading_name"].ToString();
		string s_addr1 = dr["address1"].ToString();
		string s_addr2 = dr["address2"].ToString();
		string s_city = dr["city"].ToString();
		string s_phone = dr["phone"].ToString();
		string s_fax = dr["fax"].ToString();
		string s_email = dr["email"].ToString();
		string s_tech = dr["technician"].ToString();
		string trading_name = s_company + "<br>" + s_addr1 +"<br>"+ s_addr2 +"<br>"+s_city+"<br>"+s_phone+"<br>"+s_fax+"<br>"+ s_email;
		//tech_email = s_email;
	//RMA Number from Supplier 
		Response.Write("<br>");
		Response.Write("<table valign=top align=center width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td>");
		Response.Write("<table valign=top align=center width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td>"+header+"</td><tr></table></td>");
		Response.Write("<td>");
		Response.Write("<table valign=top align=center width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td align=center><h5>Return Material Authorization -RMA- </h5></td></tr>");
		Response.Write("<tr><td align=center><h5>RMA# "+s_supprma+"</h5></td></tr>");
		Response.Write("</table></td></tr>");
		Response.Write("</table>");
		Response.Write("<hr size=1 width=100% color=black>");
			
		Response.Write("<table width=100% valign=center cellspacing=2 cellpadding=1 border=1 bordercolor=black bgcolor=#E3E3E3");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		
		Response.Write("<td width=40% valign=top>");
		Response.Write("<table valign=top cellspacing=1 cellpadding=0 border=0 bordercolor=white bgcolor=#E3E3E3");
		Response.Write(" style=\"font-family:Verdana;font-size:12pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td>");
		Response.Write("<b>SHIP TO:</b> </td><td>&nbsp;&nbsp;"+s_supplier+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;<b>"+s_company.ToUpper()+"</b></td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;"+s_addr1+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;"+s_addr2+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;"+s_city+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;"+s_phone+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;"+s_fax+"</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;&nbsp;"+s_email+"</td></tr>");
		Response.Write("</table>");
		Response.Write("</td>");
		Response.Write("<td valign=middle align=center>");
		Response.Write("<table valign=middle cellspacing=1 cellpadding=1 border=0 bordercolor=white bgcolor=#E3E3E3");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><th colspan=2></th></tr>");
		Response.Write("<tr><td>Own RMA NO:</td><td>"+s_id+"</td></tr>");
		Response.Write("<tr><td>Supplier RMA NO:</td><td>"+s_supprma+"</td></tr>");
		Response.Write("<tr><td>Date:</td><td>"+s_rdate+"");
		Response.Write("</td></tr>");
		Response.Write("<tr><td>Supplier Authorise by:</td><td>"+s_authorize+"</td></tr>");
		Response.Write("<tr><td>Packing Slip NO:</td><td>"+s_pack+"</td></tr>");
		Response.Write("<tr><td>");
	
		Response.Write("</td></tr>");
		Response.Write("</table>");
	}
		
	Response.Write("</td>");
	Response.Write("</tr>");
	Response.Write("<tr bgcolor=white ><td colspan=7>-RMA Items- ");
	Response.Write("<table align width=100% cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#E2E2E2>");
	Response.Write("<th width=3%>NO#</th><th align=left>SN#</th><th align=left>Description</th><th align=left>Product Code</th><th align=left>Purchase Date</th> ");
	Response.Write("<th align=left>INV#</th></tr>");
	bool bTrColor = true;
	for(int i=0; i<dst.Tables["getRMA"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["getRMA"].Rows[i];
		string supplier = dr["supplier"].ToString();
		string stock = dr["stock_check"].ToString();
		string pcode = dr["p_code"].ToString();
		string purchase_date = dr["purchase_date"].ToString();
		string inv = dr["invoice_number"].ToString();
		string warranty = dr["warranty"].ToString();
		string pdesc = dr["name"].ToString();
		string sn = dr["serial_number"].ToString();
		string fault = dr["fault_desc"].ToString();
		
		if(bTrColor)
		{
			Response.Write("<tr>");
			bTrColor = false;
		}
		else
		{
			Response.Write("<tr bgcolor=#E6E6E6>");
			bTrColor = true;
		}
		Response.Write("<th>"+(i+1).ToString()+"</th>");
		Response.Write("<td width=12%>"+sn+"</td>");
		Response.Write("<td width=20%>"+pdesc+"</td>");
		if(pcode == "no code" || pcode == "no product code")
			Response.Write("<td width=7%>&nbsp;</td>");
		else
			Response.Write("<td width=7%>"+pcode+"</td>");
		string invalid_date = "01 Jan 1900";
		
		if(purchase_date.IndexOf(invalid_date) == 0)
			Response.Write("<td width=7%>&nbsp;</td>");
		else
			Response.Write("<td width=7%>"+purchase_date+"</td>");
		if(inv == "0")
			Response.Write("<td width=5%>&nbsp;</td>");
		else
			Response.Write("<td width=5%>"+inv+"</td>");
		Response.Write("</tr>");
		//Response.Write("<tr><th align=right>Fault Description:</th><td colspan=4><input size=100% type=text value='"+fault+"'></td>");
		Response.Write("<tr><th></th><th>Fault Description:</th><td colspan=4><input size=80% type=text value='"+fault+"'></td>");
		Response.Write("</tr>");
	
	}
	if(dst.Tables["getRMA"].Rows.Count <= 7)
	{
		int iCt = dst.Tables["getRMA"].Rows.Count;
		if(iCt <= 5)
			iCt += 33;
		else if( iCt > 5 && iCt <= 13)
			iCt += 27;
		else if( iCt > 13 && iCt <=20)
			iCt += 18;
		else
			iCt += 12;
		for(int i=0; i<iCt; i++)
			Response.Write("<tr><td>&nbsp;</td></tr>");
	}
	Response.Write("</table>");
	Response.Write("</td>");
	Response.Write("</tr>");
	
	Response.Write("<tr><td colspan=2 align=right valign=bottom> ");
	Response.Write("<table width=100% valign=bottomleft cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=#E3E3E3");
	Response.Write(" style=\"font-family:Verdana;font-size:12pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>"+header+"</td>");

	Response.Write("</table>");
	//Response.Write("<td>&nbsp;</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	Response.Write("</html>");
	Response.Write("</body>");
	return true;
}

bool updateRMARecord()
{
	//Response.Write("you right");
	int nRows = 0;
	if(Request.QueryString["rows"] != null)
		nRows = int.Parse(Request.QueryString["rows"].ToString());
	for(int i=0; i<nRows; i++)
	{
		string rmano = "";
		string supp_rma = "";
		string pack = "";
		string authorize = "";
		string date = "";
		string sn = "";
		string inv = "";
		string pdesc = "";
		string pcode = "";
		string supp = "";
		string pdate = "";
		string warranty = ""; string chkStatus = "";
		string fault = "";	string repair = "";
		string stock = "";	string hidesn = "";
		if(Request.Form["txtOwnRMA"] != null && Request.Form["txtOwnRMA"] != "")
			rmano = Request.Form["txtOwnRMA"].ToString();

		if(Request.Form["txtRMANO"] != null && Request.Form["txtRMANO"] != "")
			supp_rma = Request.Form["txtRMANO"].ToString();
		else
			supp_rma = "0";
		if(Request.Form["txtDate"] != null && Request.Form["txtDate"] != "")
			date = Request.Form["txtDate"].ToString();
		if(Request.Form["txtAuthorise"] != null && Request.Form["txtAuthorise"] != "")
			authorize = Request.Form["txtAuthorise"].ToString();
		else
			authorize = "no authorize person";

		if(Request.Form["txtPack"] != null && Request.Form["txtPack"] != "")
			pack = Request.Form["txtPack"].ToString();
		else
			pack = "0";
		
		if(Request.Form["txtpdesc"+i.ToString()] != null && Request.Form["txtpdesc"+i.ToString()] != "")
			pdesc = Request.Form["txtpdesc"+i.ToString()].ToString();
		else
			pdesc = "no description";
		if(Request.Form["optSupp"] != null && Request.Form["optSupp"] != "")
			supp = Request.Form["optSupp"].ToString();
		
		if(Request.Form["txtpcode"+i.ToString()] != null &&  Request.Form["txtpcode"+i.ToString()] != "")
			pcode = Request.Form["txtpcode"+i.ToString()].ToString();
		else
			pcode = "0";
		if(Request.Form["txtpdate"+i.ToString()] != null && Request.Form["txtpdate"+i.ToString()] != "")
			pdate = Request.Form["txtpdate"+i.ToString()].ToString();
		else
			pdate = DateTime.Now.ToString("dd/MM/yyyy");

		if(Request.Form["txtwarranty"+i.ToString()] != null &&Request.Form["txtwarranty"+i.ToString()] != "" )
			warranty = Request.Form["txtwarranty"+i.ToString()].ToString();
		else 
			warranty = "1";
		if(Request.Form["txtinv"+i.ToString()] != null && Request.Form["txtinv"+i.ToString()] != "")
			inv = Request.Form["txtinv"+i.ToString()].ToString();
		else 
			inv = "0";
		if(Request.Form["chkstock"+i.ToString()] == "1")
			stock = "1";
		else
			stock = "0";
		if(Request.Form["txtrepair"+i.ToString()] != null && Request.Form["txtrepair"+i.ToString()] != "")
			repair = Request.Form["txtrepair"+i.ToString()].ToString();

		if(Request.Form["txtfault"+i.ToString()] != null)
			fault = Request.Form["txtfault"+i.ToString()].ToString();
		if(Request.Form["hidesn"+i.ToString()] != null)
			hidesn = Request.Form["hidesn"+i.ToString()].ToString();
	
		if(Request.Form["chkStatus"] == "2")
			chkStatus = Request.Form["chkStatus"].ToString();
		else
		{
			if(Request.Form["status"+i.ToString()] != null)
				chkStatus = Request.Form["status"+i.ToString()].ToString();
			else
				chkStatus = "1";
		}
		string sc = " UPDATE rma SET ";
		sc += " supp_rmano = '"+supp_rma+"', ";
		sc += " pack_no = '"+pack+"', ";
		sc += " authorize = '"+authorize+"', ";
		sc += " product_desc = '"+pdesc+"', ";
		sc += " supplier = '"+supp+"', ";
		sc += " p_code = '"+pcode+"', ";
		sc += " purchase_date = '"+pdate+"', ";
		sc += " invoice_number = '"+inv+"', ";
		sc += " warranty = "+warranty+", ";
		sc += " fault_desc = '"+fault+"', ";
		sc += " stock_check = "+stock+" ,";
		sc += " repair_jobno = '"+repair+"', ";
		sc += " check_status = "+chkStatus+" ";
		sc += " WHERE ra_id = "+rmano+" ";
		sc += " AND serial_number = '"+hidesn+"'";
		repair = repair.Replace("r", "");
		if(hidesn != "" && chkStatus == "2")
		{
			sc += AddSerialLogString(hidesn, "Faulty Item sent to Supplier with, delivery slip#: "+pack, inv, "", repair, supp_rma);
			sc += AddRepairLogString("Update Faulty Product and check sent to Supplier with, delivery slip#: "+pack+"",inv, pcode, repair, hidesn,rmano);
		}
		else
			sc += AddRepairLogString("Update Change to Faulty Product: ",inv, pcode, repair, hidesn,rmano);
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

bool printRMARecord()
{
	if(dst.Tables["getRMA"] != null)
		dst.Tables["getRMA"].Clear();

	if(!getRMANO())
		return false;
	string s_rma = "";
	if(Request.QueryString["rma"] != null)
		s_rma = Request.QueryString["rma"].ToString();
	
	int nCt = dst.Tables["getRMA"].Rows.Count;

//DEBUG("rows coutn = ", nCt);
	Response.Write("<form name=frmUpdate method=post action='rma.aspx?rma="+s_rma+"&data=recorded&rows="+nCt+"'>");
	if(nCt <= 0)
	{
		Response.Write("<script language=javascript> <!--");
		const string invalid = @"
			window.alert('NO JOBs, PLEASE TRY AGAIN');
			window.location=('rma.aspx?');
		";
		Response.Write("-->");
		Response.Write(invalid);
		Response.Write("</script");
		Response.Write(">");
		return false;
	}
	else
	{
	//DEBUG("how many = ", (dst.Tables["getRMA"].Rows.Count).ToString());
		string tech_email = "";
		if(dst.Tables["getRMA"].Rows.Count > 0)
		{
			DataRow dr = dst.Tables["getRMA"].Rows[0];
			string s_id = dr["ra_id"].ToString();
			string s_supprma = dr["supp_rmano"].ToString();
			string s_pack = dr["pack_no"].ToString();
			string s_authorize = dr["authorize"].ToString();
			string s_rdate = dr["repair_date"].ToString();
			string name = dr["name"].ToString();
			string s_supplier =  dr["supplier"].ToString();
			string s_company = dr["trading_name"].ToString();
			string s_addr1 = dr["address1"].ToString();
			string s_addr2 = dr["address2"].ToString();
			string s_city = dr["city"].ToString();
			string s_phone = dr["phone"].ToString();
			string s_fax = dr["fax"].ToString();
			string s_email = dr["email"].ToString();
			string s_chkstatus = dr["check_status"].ToString();
			tech_email = s_email;
			string trading_name = s_company + "<br>" + s_addr1 +"<br>"+ s_addr2 +"<br>"+s_city+"<br>"+s_phone+"<br>"+s_fax+"<br>"+ s_email;
	
			Response.Write("<br>");
			Response.Write("<table valign=center width=100% cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
			Response.Write(" style=\"font-family:Verdana;font-size:5pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td>");
			Response.Write("<table valign=top align=center width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td>"+header+"</td><tr></table></td>");
			Response.Write("<td>");
			Response.Write("<table valign=top align=center width=100% cellspacing=1 cellpadding=1 border=0 bordercolor=black bgcolor=white");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td align=center><h5>Return Material Authorization -RMA- </h5></td></tr>");
			Response.Write("<tr><td align=center><h5>RMA# "+s_supprma+"</h5></td></tr>");
			Response.Write("</table></td></tr>");
			Response.Write("</table>");
				
			Response.Write("<br>");
			Response.Write("<table valign=center width=100% cellspacing=0 cellpadding=1 border=1 bordercolor=black bgcolor=#E3E3E3");
			Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

			Response.Write("<tr><td>");
			Response.Write("<table valign=top cellspacing=0 cellpadding=1 border=0 bordercolor=white bgcolor=#E3E3E3");
			Response.Write(" style=\"font-family:Verdana;font-size:7pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><th colspan=2></th></tr>");
			Response.Write("<tr><td> Own RMA NO:</td><td><input type=text name=txtOwnRMA value='"+s_id+"'></td></tr>");
			Response.Write("<tr><td> Supplier RMA NO:</td><td><input type=text name=txtRMANO value='"+s_supprma+"'></td></tr>");
			Response.Write("<tr><td> Date:</td><td><input type=text name=txtDate value='"+s_rdate+"'>");
			Response.Write("<input type=button value='...' "+ Session["button_style"] +"");
			Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frmUpdate.txtDate','calendar_window','width=190,height=230');calendar_window.focus()\">");
			//Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frmUpdate.txtDate','calendar_window','width=190,height=230');calendar_window.focus()\">");
			//Response.Write("<font color=blue><b>...</font></b>");
			Response.Write("</td></tr>");
			Response.Write("<tr><td> Authorise by:</td><td><input type=text name=txtAuthorise value='"+s_authorize+"'></td></tr>");
			Response.Write("<tr><td>Packing Slip NO:</td><td><input type=text name=txtPack value='"+s_pack+"'></td>");
			if(s_chkstatus == "2")
				Response.Write("<td><input type=checkbox value=2 name=chkStatus checked><b>Item has been delivered to supplier</b></td>");
			else
				Response.Write("<td><input type=checkbox value=2 name=chkStatus><b>Tick, for items to send to Supplier</b></td>");

			Response.Write("</tr>");
			Response.Write("</table>");
			Response.Write("</td>");
			Response.Write("<td valign=top>");
			Response.Write("<table valign=top cellspacing=0 cellpadding=0 border=0 bordercolor=white bgcolor=#E3E3E3");
			Response.Write(" style=\"font-family:Verdana;font-size:7pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
			Response.Write("<tr><td>");
			Response.Write("<b>Ship to:</b> </td><td>");
			if(!getSupplierName())
				return false;
			Response.Write("<select name=optSupp ");
			Response.Write(" onchange=\"window.location=('"+Request.ServerVariables["URL"]+"?rma="+s_id+"&data=recorded&supplier='+this.options[this.selectedIndex].value)\" >"); 
			if(Request.QueryString["supplier"] != null)
				s_supplier=Request.QueryString["supplier"].ToString();
			for(int i=0; i<dst.Tables["card"].Rows.Count; i++)
			{
				dr = dst.Tables["card"].Rows[i];
				string s_short = dr["Short Name"].ToString();
				string supplier = dr["Supplier"].ToString();
				name = dr["name"].ToString();
				if(s_supplier.ToUpper() == s_short.ToUpper())
				{
					Response.Write("<option value='"+s_short+"' selected>"+supplier+name+"</option>");	
					name = dr["name"].ToString();
					s_supplier =  dr["supplier"].ToString();
					s_company = dr["company"].ToString();
					s_addr1 = dr["address1"].ToString();
					s_addr2 = dr["address2"].ToString();
					//s_city = dr["city"].ToString();
					s_phone = dr["phone"].ToString();
					s_fax = dr["fax"].ToString();
					s_email = dr["email"].ToString();
				}
				else
					Response.Write("<option value='"+s_short+"'>"+supplier+name+"</option>");
			}
			Response.Write("</select></td>");
			
			Response.Write("</tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_company.ToUpper()+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_addr1+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_addr2+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_city+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_phone+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_fax+"</td></tr>");
			Response.Write("<tr><td>&nbsp;</td><td>"+s_email+"</td></tr>");
			Response.Write("</table>");
		}
		
		Response.Write("</td>");
		Response.Write("</tr>");
		Response.Write("<tr bgcolor=white><td colspan=7>-RMA Items- ");
		Response.Write("<table align width=100% cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:7pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#E3E3E3>");
		Response.Write("<th align=left>Serial Number</th><th align=left>Description</th><th align=left>Product Code</th><th align=left>Purchase Date</th> ");
		Response.Write("<th align=left>Invoice Number</th><th>for Stock??</th><th>Repair#</th> </tr>"); //<th>Fault Description </th></tr>");
		for(int i=0; i<dst.Tables["getRMA"].Rows.Count; i++)
		{
			DataRow dr = dst.Tables["getRMA"].Rows[i];
			string supplier = dr["supplier"].ToString();
			string stock = dr["stock_check"].ToString();
			string pcode = dr["p_code"].ToString();
			string purchase_date = dr["purchase_date"].ToString();
			string inv = dr["invoice_number"].ToString();
			string warranty = dr["warranty"].ToString();
			string pdesc = dr["name"].ToString();
			string sn = dr["serial_number"].ToString();
			string fault = dr["fault_desc"].ToString();
			string repair = dr["repair_jobno"].ToString();
			string status = dr["check_status"].ToString();
			string id = dr["id"].ToString();
			Response.Write("<tr>");
		
			Response.Write("<input type=hidden name=hidesn"+i.ToString()+" value='"+sn+"'>");
			Response.Write("<td width=12%><input size=10% type=text name=txtsn"+i.ToString()+" value='"+sn+"'></td>");
			Response.Write("<td width=20%><input size=20% type=text name=txtpdesc"+i.ToString()+" value='"+pdesc+"'></td>");
			Response.Write("<input type=hidden name=txtsupp"+i.ToString()+" value='"+supplier+"'>");
			Response.Write("<td width=7%><input size=7% type=text name=txtpcode"+i.ToString()+" value='"+pcode+"'></td>");
			Response.Write("<td width=7%><input size=7% type=text name=txtpdate"+i.ToString()+" value='"+purchase_date+"'>");
			Response.Write("<input type=button value='...' "+ Session["button_style"] +"");
			Response.Write(" onclick=\"javascript:calendar_window=window.open('calendar.aspx?formname=frmUpdate.txtpdate"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\">");
			//Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frmUpdate.txtpdate"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\">");
			//Response.Write("<font color=blue><b>...</font></b>");
			Response.Write("<input type=hidden name=txtwarranty"+i.ToString()+" value='"+warranty+"'>");
			Response.Write("<td width=5%><input size=5% type=text name=txtinv"+i.ToString()+" value='"+inv+"'></td>");
						
			if(stock == "1")
				Response.Write("<td width=3%><input size=2% type=checkbox name=chkstock"+i.ToString()+" value='"+stock+"' checked></td>");
			else
				Response.Write("<td width=3%><input size=2% type=checkbox name=chkstock"+i.ToString()+" value='"+stock+"'></td>");
			
			Response.Write("<td width=5%><input size=5% type=text name=txtrepair"+i.ToString()+" value='"+repair+"'></td>");
			Response.Write("</tr>");
			//Response.Write("<td><input type=text name=txtfault"+i.ToString()+" value='"+fault+"'></td>");
			Response.Write("<tr><th align=right>Fault Description</th><td colspan=4><input size=70% type=text name=txtfault"+i.ToString()+" value='"+fault+"'>");
			Response.Write("&nbsp;<a href='rma.aspx?deljob="+id+"&rma="+s_rma+"&data=recorded'><font color=red><b>Del Record</b></font></a></td>");
			Response.Write("<input type=hidden name='hidid"+i.ToString()+"' value="+id+">");
			if(status == "3")
					Response.Write("<th colspan=4>Select RA Status: <select name='status"+i.ToString()+"'><option value=1>Not Send Yet<option value=2>Return<option value=3 selected>DONE</select></th>");
			Response.Write("</tr>");
		
		}
		Response.Write("</table>");
		Response.Write("</td>");
		Response.Write("</tr>");
		Response.Write("<tr><td colspan=1 align=right> ");
	
		Response.Write("</td>");
		Response.Write("<td colspan=2 valign=top>");
		Response.Write("<table width=100% valign=center cellspacing=1 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=#E3E3E3");
		Response.Write(" style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan align=right>");
		Response.Write("<input type=button onclick=window.location=('rma.aspx') value='RA Task'"+Session["button_style"]+">");
		Response.Write("<input type=button name=new "+Session["button_style"]+" value='New RMA' onclick=window.location=('"+Request.ServerVariables["URL"]+"?new=rma_new')");

		Response.Write("><input type=submit name=cmd value='Update Record'"+Session["button_style"]+"></td></tr>");
		Response.Write("<tr><td align=right><input type=submit name=cmd value='Delete Record'"+Session["button_style"]+">");
		Response.Write("<input type=submit name=cmd value='Print Record'"+Session["button_style"]+"></td></tr>");
		tech_email = EmailChange(tech_email);
		//if(Request.Form["cmd"] == "send??")
		Response.Write("<tr><td align=right>Email to Supplier:<input type=text name=txtEmail value='"+tech_email+"'><input type=submit name=cmd value='Send??'"+Session["button_style"]+">");
		Response.Write("</table>");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		Response.Write("</form>");
	}
	return true;

}

bool DisplayRMAList()
{
	string s_status = "", s_supplier = "";
	string sc = " SELECT repair_jobno, return_date, status,check_status, pack_no,";
	sc += " technician,  authorize, supplier, supp_rmano, ra_id 'RMA No', CONVERT(varchar(12),repair_date,13) AS 'RMA Date' FROM rma";

	//string sc = " SELECT repair_jobno, return_date, status, stock_check, check_status, serial_number, invoice_number, fault_desc, ";
	//sc += " technician, pack_no, authorize, product_desc, p_code, supplier, supp_rmano, ra_id 'RMA No', CONVERT(varchar(12),repair_date,13) AS 'RMA Date' FROM rma";
	
	//string sc = " SELECT id 'RMANo', invoice_number, status, technician, pack_no, fault_desc, CONVERT(varchar(12),repair_date, 13) AS 'RMA Date'  FROM rma ";
	//sc += " INNER JOIN enum e ON e.id = rma.status";
	//sc += " WHERE e.class = 'rma_status' ";
	//sc += " WHERE check_status = 1 ";

	if(Request.QueryString["rma_nav"] != null && Request.QueryString["rma_nav"] != "all")
	{
		s_status = Request.QueryString["rma_nav"].ToString();
		sc += " WHERE check_status = "+s_status+" ";
		if(Request.QueryString["supplier"] != null && Request.QueryString["supplier"] != "all")
		{
			s_supplier = Request.QueryString["supplier"].ToString();
			sc += " AND supplier = '"+s_supplier+"'";
		}
		if(s_status == "3")
		{
			sc += " AND (repair_date BETWEEN DATEADD(month, -2, repair_date) AND GETDATE()) ";
			
		}
	}
	sc += " GROUP BY repair_jobno, return_date, status,check_status, pack_no,";
	sc += " technician,  authorize, supplier, supp_rmano, ra_id ,repair_date";
	sc += " ORDER BY ra_id DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "rmalist");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	const string jav = @"
	
	function checkform() 
	{	
		if(document.frmSearch.txtSearch.value==''){
		window.alert('NO Data in the field');
		document.frmSearch.txtSearch.focus();
		document.frmSearch.txtSearch.select();
		return false;
		}
		if(!IsNumberic(document.frmSearch.txtSearch.value)){
			window.alert('Number Only for RMA number');
			document.frmSearch.txtSearch.focus();
			document.frmSearch.txtSearch.select();
			return false;
		}
		return true;
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
	Response.Write("--> "); 
	Response.Write(jav);
	Response.Write("</script");
	Response.Write("> ");
	
	Response.Write("<form name=frmSearch method=post action=rma.aspx?search=true&data=recorded onsubmit='return checkform()'>");
	Response.Write("<table align width=100% valign=center cellspacing=1 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><input type=text name=txtSearch value='' 'if(window.event.keyCode==13){checkform()}'><input type=submit name=cmd value='RMA# Search' "+ Session["button_style"] + " onclick='return checkform()'>");
	Response.Write("</td>");
	Response.Write("<script language=javascript>\r\n");
	Response.Write("<!--- hide old browser\r\n");
	Response.Write("document.frmSearch.txtSearch.focus()\r\n");
	Response.Write("</script\r\n");
	Response.Write(">");
	Response.Write("<td align=right><img src=r.gif><a href=rma.aspx class=o>RA Task</a>&nbsp;&nbsp; ");
	Response.Write("<img src=r.gif><a href=rma.aspx?task=all&rma_nav=all  class=o>All</a> ");
	Response.Write("&nbsp;&nbsp;&nbsp;<img src=r.gif>");
	Response.Write("<a href=rma.aspx?new=rma_new class=o>New RMA</a> ");
	Response.Write("&nbsp;&nbsp;&nbsp;<img src=r.gif>");
	Response.Write("<a href=rma.aspx?task=edit&rma_nav=1  class=o>Waiting 4 Process</a> ");
	Response.Write("&nbsp;&nbsp;&nbsp;<img src=r.gif>");
	Response.Write("<a href=rma.aspx?task=return&rma_nav=2 class=o>Waiting 4 Return Items</a>");
	Response.Write("&nbsp;&nbsp;&nbsp;<img src=r.gif><a href=rma.aspx?task=done&rma_nav=3  class=o>RMA Finish</a></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=5><hr size=1 color=black width=100%></td></tr>");
	string s_task = "", s_nav = "";
	if(Request.QueryString["task"] != null)
		s_task = (Request.QueryString["task"].ToString()).ToUpper();
	if(Request.QueryString["rma_nav"] != null)
		s_nav = Request.QueryString["rma_nav"].ToString();
	if(!GetSupplier())
		return false;
	Response.Write("<tr><th align=left>---"+s_task+" RA Products---</th><th align=right>Select Supplier Catalog: &nbsp;<select name=selSupp ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?task="+s_task+"&rma_nav="+Request.QueryString["rma_nav"]+"&supplier='+this.options[this.selectedIndex].value )\"");
	Response.Write(">");
	Response.Write("<option value='all'>ALL Supplier");
	string s_check = "";
	if(Request.QueryString["supplier"] != null)
		s_check = Request.QueryString["supplier"].ToString();
	for(int i=0; i<dst.Tables["suppList"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["suppList"].Rows[i];
		string s_name = dr["name"].ToString();
		string s_supp = dr["trading_name"].ToString();
		string s_id = dr["supplier"].ToString();
		
		if(s_check == s_id)
			Response.Write("<option value='"+s_id+"' selected>"+ s_name+"</option>");
		else
			Response.Write("<option value='"+s_id+"' >"+ s_name+"</option>");
		
	}
	Response.Write("</th></tr>");
	Response.Write("</form>");
	return true;
	
}

bool GetSupplier()
{
	string s_nav = "";
	if(Request.QueryString["rma_nav"] != null)
		s_nav = Request.QueryString["rma_nav"].ToString();
	string sc = "SELECT DISTINCT c.trading_name, c.name, r.supplier";
	sc += " FROM rma r INNER JOIN card c ON c.short_name = r.supplier";
	sc += " WHERE c.type = 3";
	if((s_nav != "" && s_nav != null) && s_nav != "all")
		sc += " AND check_status = "+s_nav+" ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "suppList");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool getRMANO()
{
	string s_no = ""; 
	if(Request.QueryString["rma"] !=null)
		s_no = Request.QueryString["rma"];
	else if(Request.Form["txtSearch"] != null)
		s_no = Request.Form["txtSearch"].ToString();
	
//DEBUG("s_no= ", s_no);
	string sc = "SELECT r.id, r.check_status, r.repair_jobno, r.supplier, r.pack_no, r.authorize, r.supp_rmano, r.warranty, r.stock_check, r.p_code, CONVERT(varchar(12),r.repair_date,13) as repair_date ";
	sc += " , r.ra_id, r.invoice_number, CONVERT(varchar(12),r.purchase_date, 13) as purchase_date, r.fault_desc, ";
	sc += " r.serial_number, CONVERT(varchar(50), r.product_desc) AS name ";
	sc += ", c.name AS Supplier, c.company, c.trading_name, c.address1, c.address2, c.phone, c.city, c.country, c.fax, c.email, r.technician ";
	sc += "FROM rma r INNER JOIN ";
    sc += " card c ON c.short_name = r.supplier ";
	sc += " WHERE r.ra_id = "+s_no+" ";
	sc += " AND c.type = 3 ";
	//sc += " GROUP BY r.check_status, r.repair_jobno, r.supplier, r.pack_no, r.authorize, r.supp_rmano, r.warranty, r.stock_check, r.p_code, r.repair_date, ";
	//sc += " r.id, r.invoice_number, r.purchase_date, r.fault_desc, ";
	//sc += " r.serial_number, r.product_desc";
	//sc += ", c.name, c.company, c.address1, c.address2, c.phone, c.city, c.country, c.fax, c.email, r.technician";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "getRMA"); 
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool autoSendMail()
{
	if(!getRMANO())
		return false;
	string s_email = "";
	if(Request.Form["txtEmail"] != null)
		s_email = Request.Form["txtEmail"].ToString();

	MailMessage msgMail = new MailMessage();
	//msgMail.To = "tee@eznz.com";
	msgMail.To = s_email;
	//DEBUG("semail = ", s_email);
	//msgMail.To = 
	string today = DateTime.Now.ToString("dd/MM/yyyy");
	//msgMail.From = "richard@edencomputer.co.nz";
	msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Subject = "Return Material Authorization RMA Request";
	msgMail.Body = "<html><style type=\"text/css\">\r\n";
	msgMail.Body += "td{FONT-WEIGHT:300;FONT-SIZE:7PT;FONT-FAMILY:verdana;}\r\n";
	msgMail.Body += "body{FONT-WEIGHT:300;FONT-SIZE:7PT;FONT-FAMILY:verdana;}</style>\r\n<head><h5>";
	msgMail.Body += "</head><body>";
	msgMail.Body += "<table border=0>";
	string s_supplier = "";
	//string s = (dst.Tables["getRMA"].Rows.Count).ToString();
//DEBUG(" s = ", s);
	if(dst.Tables["getRMA"].Rows.Count>0)
	{
		DataRow dr = dst.Tables["getRMA"].Rows[0];
		s_supplier = dr["Supplier"].ToString();
		string s_company = dr["trading_name"].ToString();
		string s_addr1 = dr["address1"].ToString();
		string s_addr2 = dr["address2"].ToString();
		string s_city = dr["city"].ToString();
		string s_country = dr["country"].ToString();
		string s_phone = dr["phone"].ToString();
		string s_fax = dr["fax"].ToString();
		string s_supp_rma = dr["supp_rmano"].ToString();

		msgMail.Body += "<tr><td colspan=4><table>";
		msgMail.Body += "<tr><td>"+s_company+"</td></tr>\r\n";
		msgMail.Body += "<tr><td>"+s_supplier+"</td></tr>\r\n";
		msgMail.Body += "<tr><td>"+s_addr1+"</td></tr>\r\n";
		msgMail.Body += "<tr><td>"+s_addr2+"</td></tr>\r\n";
		msgMail.Body += "<tr><td>"+s_city+"</td></tr>\r\n";
		msgMail.Body += "<tr><td>"+s_country+"</td>";
		msgMail.Body += "<td>"+s_phone+"</td>";
		msgMail.Body += "<td>"+s_fax+"</td></tr>\r\n";
		msgMail.Body += "</table><br>";
		msgMail.Body += "<tr><td>"+today+"</td></tr>\r\n";
		msgMail.Body += "<tr><td colspan=4><b>RE: Return Material Authorization -RMA-</b> RMA#"+s_supp_rma+"</td></tr>\r\n";
		//msgMail.Body += "<tr><td colspan=4><b>RE: Return Material Authorization -RMA-</b> RMA#"+s_supp_rma+"</td></tr>\r\n";
	}
	msgMail.Body += "<tr><td colspan=7>&nbsp;</td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=7>Dear Supplier </td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=7>&nbsp;</td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=7>The item(s) below are for warranty repair:</td></tr>\r\n";
	
	msgMail.Body += "<tr><td colspan=7><hr size=1 color=black></td></tr>\r\n";
	//msgMail.Body += "<tr><td colspan=7>Fault Items Below</td></tr>");
	msgMail.Body += "<tr bgcolor=#E3E3E3><td width=5%>&nbsp;</td><td>Invoice No</td><td>P_Code</td><td>Serial No</td><td>Product Description</td><td>Fault Description</td><td>Purchase Date</td>";
	msgMail.Body += "</tr>\r\n";
	bool bCheck = true;
	string s_technician = "";
	for(int i=0; i<dst.Tables["getRMA"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["getRMA"].Rows[i];
		string s_id = dr["ra_id"].ToString();
		string s_sn = dr["serial_number"].ToString();
		string s_inv = dr["invoice_number"].ToString();
		string s_fault = dr["fault_desc"].ToString();
		string s_purdate = dr["purchase_date"].ToString();
		string s_desc = dr["name"].ToString();
		string s_pcode = dr["p_code"].ToString();
		s_technician = dr["technician"].ToString();
		if(bCheck)
		{
			msgMail.Body += "<tr>";
			bCheck = false;
		}
		else
		{
			msgMail.Body += "<tr bgcolor=#EEEEEE>";
			bCheck = true;
		}
		msgMail.Body += "<td width=5%>"+(i+1).ToString()+"</td><td>"+s_inv+"</td><td>"+s_pcode+"</td><td>"+s_sn+"</td><td>"+s_desc+"</td><td>"+s_fault+"</td><td>"+s_purdate+"</td></tr>\r\n";
	}
	
	msgMail.Body += ""; 
	//msgMail.Body += "<br>***************************************************<br>";
	msgMail.Body += "<br><tr><td colspan=7><hr size=1 color=black> </td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=7>Please feel free to contact me, for any further queries.</td></tr>\r\n";
	msgMail.Body += "<br><tr><td colspan=4>&nbsp; </td></tr>\r\n";
	msgMail.Body += "<br><tr><td>Kind regards </td></tr>\r\n";
	msgMail.Body += "<tr><td>&nbsp;</td></tr>\r\n";
	msgMail.Body += "<tr><td>&nbsp;</td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=3>"+s_technician+" </td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=3>Eden Technician </td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=5>&nbsp;</td></tr>\r\n";
	msgMail.Body += "<tr><td colspan=5>\r\n";
	msgMail.Body += "<tr><td>&nbsp;</td></tr>\r\n";
	msgMail.Body += "<table border=0 cellspacing=0 cellpadding=0>\r\n";
	msgMail.Body += "<tr><td>"+header+"</td></tr>";

	msgMail.Body += "</table>";
	msgMail.Body += "</td></tr>";
	msgMail.Body += "</table>";
	msgMail.Body += "</body>";
	msgMail.Body += "</html>";	
	SmtpMail.Send(msgMail);
	//cc to yourself
	string login_email = "";
	if(TS_UserLoggedIn())
		login_email = Session["email"].ToString();
	//msgMail.To = "tee@eznz.com"; //m_sTecheMail;
	msgMail.To = login_email; //m_sTecheMail;
	SmtpMail.Send(msgMail);
	
	return true;
}


bool GetNextRMANO(ref int rNumber)
{
	if(dst.Tables["insertrma"] != null)
		dst.Tables["insertrma"].Clear();

	rNumber = 3000;

	string sc = "SELECT TOP 1 ra_id FROM rma ORDER BY ra_id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		//int i = myAdapter.Fill(dst,"insertrma");
		if(myAdapter.Fill(dst, "insertrma") == 1)
			rNumber = int.Parse(dst.Tables["insertrma"].Rows[0]["ra_id"].ToString()) + 1;

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

//copy from bb.c to decode the message with special char
string msgEncode(string s)
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += "\'\'"; //double it for SQL query
		else if(s[i] == '<')
			ss += '[';
		else if(s[i] == '>')
			ss += ']';
		else if(s[i] == '*')
			ss += '-';
		else if(s[i] == '.')
			ss += ' ';
		else if(s[i] == '~')
			ss += ' ';
		else if(s[i] == '`')
			ss += ' ';
		else
			ss += s[i];
	}
	return ss;
}

bool updateStockStatus(string s_sn)
{
	string sc = "UPDATE stock SET status = 4 ";
	sc += " WHERE sn = '"+s_sn+"'";
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


bool insertRMAStock()
{
	int rows = 0;
	int sns = 0; 
 
	if(Session["rma_sns"] != null)
		sns = (int)Session["rma_sns"];
	
	for(int i=1; i<=sns; i++)
	{
		string sPCode = "";
		string sPONumber = "";
		string sSupplier = ""; string s_sn = "";
		string sPDate = ""; 
		string rma_sn = "";
		rma_sn = Session["rma_sn" + i.ToString()].ToString();
		if(rma_sn == "" && rma_sn == null)
			rma_sn = "no serial#";
//DEBUG("rma_sn", rma_sn);
		
		string sc = "SELECT serial_number FROM rma";
		sc += " WHERE serial_number = '" + rma_sn + "'";
//DEBUG("rma_session = ", rma_session);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "RMAItem");
			
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
		
		if(rows <= 0 ) // && Request.Form["chkRecord"] == "on") //(m_sFault != null && m_sFault != ""))
		{
			string supplier = "";
			string purchase_date = "";
			string po_number = "";	string repair_no = "";
			string warranty = "";	string fault = "";
			string pdesc = "";  string pcode = "";
			int stock_check = 0; string supp_code = "";
			if(Request.Form["repair" + i.ToString()] != null && Request.Form["repair" + i.ToString()] != "")
				repair_no = Request.Form["repair" + i.ToString()].ToString();
			//else
			//	repair_no = "0";
			
			if(Request.Form["fault" + i.ToString()] != null && Request.Form["fault" + i.ToString()] != "")
				fault = msgEncode(Request.Form["fault" + i.ToString()].ToString());
			//else
			//	fault = "no fault description";
			if(Request.Form["purchase_date" + i.ToString()] != null && Request.Form["purchase_date" + i.ToString()] != "")
				purchase_date = Request.Form["purchase_date" + i.ToString()];
			else
				purchase_date = "";
//DEBUG("purchase date = ", purchase_date);
			if(Request.Form["po_number" + i.ToString()] != null && Request.Form["po_number" + i.ToString()] != "")
				po_number = Request.Form["po_number" + i.ToString()];
			//else
			//	po_number = "0";
			if(Request.Form["warranty" + i.ToString()] != null && Request.Form["warranty" + i.ToString()] != "")
				warranty = Request.Form["warranty" + i.ToString()];
			else
				warranty = "1";

			if(Request.Form["pdesc" + i.ToString()] != null && Request.Form["pdesc" + i.ToString()] != "")
				pdesc = msgEncode(Request.Form["pdesc" + i.ToString()]);
			//else
			//	pdesc = "no description";

			if(Request.Form["supplier" + i.ToString()] != null && Request.Form["supplier" + i.ToString()] != "")
				supplier = Request.Form["supplier" + i.ToString()];
			//else
			//	supplier = "no";

			if(Request.Form["pcode" + i.ToString()] != null && Request.Form["pcode" + i.ToString()] != "")
				pcode = msgEncode(Request.Form["pcode" + i.ToString()]);
			//else
			//	pcode = "no code";

			if(Request.Form["check" + i.ToString()] == "on")
				stock_check = 1;
			else
				stock_check = 0;
			
			string s_logname = "";
			if(Session["name"] != null)
				s_logname = Session["name"].ToString();
			else 
				s_logname = "Security Alert";
			if(!updateStockStatus(rma_sn))
				return false;
		
			sc = " set DATEFORMAT dmy ";
			sc += " INSERT INTO rma ( repair_jobno, p_code, stock_check,warranty, product_desc, purchase_date,supplier,";
			sc += " serial_number, status, invoice_number, fault_desc, repair_date, ra_id, technician, check_status )";
			sc += " VALUES('"+repair_no+"', '"+pcode+"', "+stock_check+","+warranty+",'"+pdesc+"','"+purchase_date+"','"+supplier+"', ";
			sc += "'"+rma_sn+"', 4, '"+po_number+"','"+fault+"',";
			sc += " GETDATE(), "+m_RMANO+", '"+s_logname+"', 1)";
					
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
		else
		{
			Response.Write("<script language=javascript");
			Response.Write(">");
			Response.Write("if(confirm('S/N already in RMA, Please try another one')){");
			Response.Write("window.location=('rma.aspx?new=rma_new');}");
			Response.Write("</script");
			Response.Write(">");
		}
	}
	
	return true;

}


void getItems()
{
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string jav = @"
			
	function checkemail()
	{
		if(document.frmSN.txtEmail.value==''){
			window.alert('Please enter email address of supplier');
			document.frmSN.txtEmail.focus();
			document.frmSN.txtEmail.select();
			return false;
		}
		return true;
	}
	";
	Response.Write("--> "); 
	Response.Write(jav);
	Response.Write("</script");
	Response.Write("> ");
	string sid = "";
	if(Request.QueryString["id"] != null)
	{
		sid = Request.QueryString["id"].ToString();
		//Response.Write("<form name=frmSN method=post action='rma.aspx?rma="+m_RMANO+"&m="+m_nCheckSNReturn+"&id="+sid+"&new=rma_new'>");
		Response.Write("<form name=frmSN method=post action='rma.aspx?rma="+m_RMANO+"&m="+m_nCheckSNReturn+"&new=rma_new'>");
	}
	else
		Response.Write("<form name=frmSN method=post action='rma.aspx?rma="+m_RMANO+"&m="+m_nCheckSNReturn+"&new=rma_new'>");
		//RMA Number from Supplier 
	Response.Write("</table>");
	Response.Write("<tr><td align=center>");
	Response.Write("<table width=100% align=center cellspacing=0 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:3pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>"+header+"</td></tr>");
	Response.Write("</table></td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<table valign=top width=100% cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:5pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#E3E3E3><td valign=top colspan=2>");

	Response.Write("<table border=0 >");
	Response.Write("<table cellspacing=1 cellpadding=1 border=0 bordercolor=black ");
	Response.Write(" style=\"font-family:Verdana;font-size:5pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=2></td></tr>");
	Response.Write("<tr><td> Own RMA NO:</td><td><input type=text name=txtOwnRMA value='"+m_RMANO+"'></td></tr>");
	Response.Write("<tr><td> Supplier RMA NO:</td><td><input type=text name=txtRMANO value=''></td></tr>");
	Response.Write("<tr><td> Date:</td><td><input type=text name=txtDate value='"+DateTime.Now.ToString("dd/MMM/yyyy")+"'></td></tr>");
	Response.Write("<tr><td> Authorise by:</td><td><input type=text name=txtAuthorise value=''></td></tr>");
	Response.Write("<tr><td>Packing Slip NO:</td><td><input type=text name=txtPack value=''></td></tr>");
	Response.Write("</table>");
	Response.Write("</td>");
	Response.Write("<td valign=top>");
	Response.Write("<table valign=top cellspacing=1 cellpadding=1 border=0 bordercolor=black ");
	Response.Write(" style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=2>");
//DEBUG("sid = ", sid);
	string s_id = "";
	string s_short = "";
	string s_supplier = "";
	string s_company = "";
	string s_addr1 = "";
	string s_addr2 = "";
	string s_city = "";
	string s_phone = "";
	string s_fax = "";
	string s_email = "";
	string s_supp = "";
	if(!getSupplierName())
		return;
	//string q_rma = "", q_m = "";
	//if(Request.QueryString["rma"] != null && Request.QueryString["rma"] != "")
	//	q_rma = Request.QueryString["rma"].ToString();
	//if(Request.QueryString["m"] != null && Request.QueryString["m"] != "")
	//	q_m = Request.QueryString["m"].ToString();
	Response.Write("Select Supplier: <select name=sltSupp ");
	Response.Write(" onchange=\"window.location=('"+Request.ServerVariables["URL"]+"?new=rma_new&supplier='+this.options[this.selectedIndex].value)\" >"); 
	string name = "";
	if(Request.QueryString["supplier"] != null)
		m_supplier = Request.QueryString["supplier"].ToString();
	for(int i=0; i<dst.Tables["card"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["card"].Rows[i];
		s_short = dr["Short Name"].ToString();
		s_supplier = dr["Supplier"].ToString();
		name = dr["name"].ToString();
		s_id = dr["id"].ToString();
		if(m_supplier == s_short)
		{
			Response.Write("<option value='"+s_short+"' selected>"+s_supplier+name+"</option>");
			//Response.Write("<option value='"+s_id+"' selected>"+s_supplier+name+"</option>");
			s_id = dr["id"].ToString();
			s_short = dr["Short Name"].ToString();
			s_supp = dr["Supplier"].ToString();
			s_addr1 = dr["address1"].ToString();
			s_addr2 = dr["address2"].ToString();
			s_city = dr["address3"].ToString();
			s_phone = dr["phone"].ToString();
			s_email = dr["email"].ToString();
			s_fax = dr["fax"].ToString();
//DEBUG("s_supplier = ", s_supplier);
		}
		else
			Response.Write("<option value='"+s_short+"'>"+s_supplier+name+"</option>");
	}
	Response.Write("</select>");
	Response.Write("</td></tr>");
	
	Response.Write("<tr><td >Ship To:</td><td><b>"+s_supp.ToUpper()+"</b></td> ");
	Response.Write("<tr><td>&nbsp;</td><td>"+s_company+"</td>");
	Response.Write("<tr><td>&nbsp;</td><td>"+s_addr1+"</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>"+s_addr2+"</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>"+s_city+"</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td>"+s_phone+"</td></tr>");
	Response.Write("</tr>");
	Response.Write("</table></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td colspan=3>-RMA Items- ");
	Response.Write("<table align width=100% valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:7pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#E3E3E3>");
	Response.Write("<th>Serial Number</th><th>Description</th><th>Product Code</th><th align=left>Purchase Date</th>");
	Response.Write("<th>Invoice Number</th><th>for Stock??</th><th>Repair#</th> </tr>"); //<th>Fault Description </th></tr>");

	int sns = 0;
	
	if(Session["rma_sns"] != null)
		sns = (int)Session["rma_sns"];
	string sn = "";
	string supplier = "";
	string price = "";
	string status = "";
	string product_code = "";
	string purchase_date = "";
	string po_number = "";
	string warranty = "";
	string pdesc = "";
	string pcode = "";
	string fault = "";
	string stock = "";
	string repair = "";
	for(int i=1; i<=sns; i++)
	{
		sn = Session["rma_sn" + i.ToString()].ToString();
		if(Request.Form["sn"+i.ToString()] != null)
		{
			sn = Request.Form["sn"+i.ToString()];
			//price = Request.Form["price" + i.ToString()];
			status = Request.Form["status" + i.ToString()];
			//product_code = Request.Form["product_code" + i.ToString()];
			purchase_date = Request.Form["purchase_date" + i.ToString()];
			po_number = Request.Form["po_number" + i.ToString()];
			warranty = Request.Form["warranty" + i.ToString()];
			pdesc = Request.Form["pdesc" + i.ToString()];
			supplier = Request.Form["supplier" + i.ToString()];
			pcode = Request.Form["pcode"+i.ToString()];
			fault = Request.Form["fault"+i.ToString()];
			stock = Request.Form["check"+i.ToString()];
			repair = Request.Form["repair"+i.ToString()];
		}
		else
		{
			DataRow dr = CheckSN(sn);
			if(dr == null)
			{
				purchase_date = Request.Form["txtPDate"];
				po_number = Request.Form["txtPONumber"];
				supplier = m_supplier;
				pcode = Request.Form["txtPCode"];
				pdesc = Request.Form["txtPdesc"];
				fault = Request.Form["txtFault"];
				stock = Request.Form["txtCheck"];
				repair = Request.Form["txtRepairNo"];
			}
			else
			{
				supplier = dr["supplier"].ToString();
				price = dr["cost"].ToString();
				status = dr["status"].ToString();
				purchase_date = dr["purchase_date"].ToString();
				po_number = dr["purchase_order_id"].ToString();
				warranty = dr["warranty"].ToString();
				pdesc = dr["name"].ToString();
				pcode = dr["product_code"].ToString();
				//repair = dr["repair_jobno"].ToString();
			}
		}
		
		Response.Write("<tr><td><input  type=hidden name=sn1" + i.ToString() + " value='");
		Response.Write(sn + "'><input size=10% type=text name=sn" + i.ToString() +" value=" + sn + "></td>");
		Response.Write("<td><input size=25% type=text name=pdesc" + i.ToString() + " value='" + pdesc + "'></td>");
		Response.Write("<input type=hidden name=supplier" + i.ToString() + " value='" + supplier + "'>");
		Response.Write("<td><input size=8% type=text name=pcode" + i.ToString() + " value='"+pcode+"'></td>");
		Response.Write("<td><input size=8% type=text name=purchase_date" + i.ToString() + " value='" + purchase_date + "'>");
		Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frmSN.purchase_date"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\">");
		Response.Write("<font color=blue><b>...</font></b>");
		Response.Write("</td>");
		Response.Write("<input size=5% type=hidden name=warranty" + i.ToString() + " value='" + warranty + "'>");
		Response.Write("<td><input size=8% type=text name=po_number" + i.ToString() + " value='" + po_number + "'></td>");
		if(stock == "on")
			Response.Write("<td><input type=checkbox name=check" + i.ToString() + " checked></td>");
		else
			Response.Write("<td><input type=checkbox name=check" + i.ToString() + "></td>");
		Response.Write("<td><input type=text name=repair" + i.ToString() +" value='"+ repair+ "'></td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align=right>Fault Description</th><td colspan=5><input size=100% type=text name=fault" + i.ToString() + " value='" + fault + "'></td></tr>");
	}
	
	//if(Request.Form["chkRecord"] != "on")
	{
		Response.Write("<tr><td width=8%>");
		Response.Write("<input size=8% type=text name=txtSN > <script");
		Response.Write(">\r\ndocument.frmSN.txtSN.focus();\r\n</script");
		Response.Write(">\r\n</td>");
		
		Response.Write("<td width=15%><input size=25% type=text name=txtPDesc value=''> </td>");
		Response.Write("<td width=7%><input size=8% type=text name=txtPCode value=''> </td>");
		Response.Write("<td width=5%><input size=8% type=text name=txtPDate value=''>");
		Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frmSN.txtPDate','calendar_window','width=190,height=230');calendar_window.focus()\">");
		Response.Write("<font color=blue><b>...</font></b>");
		Response.Write("</td>");
		//Response.Write("<td><input type=text name=txtPDate value=''></td>");
		Response.Write("<td width=10%><input size=8% type=text name=txtPONumber value=''> </td>");
		Response.Write("<td width=5%><input size=5% type=checkbox name=txtCheck > </td>");
		Response.Write("<td width=5%><input size=5% type=text name=txtRepairNo> </td>");
		Response.Write("</tr>");
		Response.Write("<tr><th align=right>Fault Description</th><td colspan=5><input size=100% type=text name=txtFault></td></tr>");
	}

	Response.Write("</table>");
	Response.Write("</td></tr>");
	//Response.Write("</form>");

	Response.Write("<tr>");
	Response.Write("<td colspan=2></td>");
	Response.Write("<td valign=top> ");
	Response.Write("<table width=100% valign=center cellspacing=1 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:5pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
//DEBUG("m_sFault= ", m_sFault);

	Response.Write("<tr><td align=right><input type=button value='Waiting 4 RMA' "+ Session["button_style"] +"");
	Response.Write(" Onclick=\"window.location=('rma.aspx?task=edit&rma_nav=1')\">");
	Response.Write("<input type=button value='Waiting 4 Return' "+ Session["button_style"] +"");
	Response.Write(" Onclick=\"window.location=('rma.aspx?task=edit&rma_nav=2')\">");
	Response.Write("<input type=button value='Finish RMA' "+ Session["button_style"] +"");
	Response.Write(" Onclick=\"window.location=('rma.aspx?task=edit&rma_nav=3')\">");
	Response.Write("<input type=submit name=cmd value='Update' "+ Session["button_style"] +"></td></tr>");
	Response.Write("<tr><td align=right>");
	Response.Write("<input type=button value='New RMA Form'"+Session["button_style"]+" onclick=\"window.location=('rma.aspx?new=rma_new')\">");
	Response.Write("<input type=submit name=cmd value='Record' "+Session["button_style"]+"");
	Response.Write(" onclick=\"return confirm('are you Sure want to do this')\" >");
	
	Response.Write("</td>"); 
	Response.Write("</tr>");
	Response.Write("<tr><td align=right>*note:Hit Enter Key every single record has entered");
	Response.Write("</td></tr>");

	if(Request.Form["cmd"] == "Record" )
	//if(Request.Form["cmd"] == "Record" && (Request.Form["chkRecord"] == "on"))
	{
		s_email = EmailChange(s_email);
		Response.Write("<tr><td align=right>Email to Supplier:<input type=text name=txtEmail value='"+s_email+"'><input type=submit name=cmd value='Send??'"+Session["button_style"]+" onClick='return checkemail()'>");
	}
	Response.Write("</table>");
	Response.Write("</td></tr> ");
	Response.Write("</table>");
	Response.Write("</form>");
}

string EmailChange(string sEmail)
{
	string bforeAt = "";
	string tech = "tech";
	bforeAt = sEmail;
	int nCount = 0;
	for(int i=0; i<bforeAt.Length; i++)
	{
		if(bforeAt[i].ToString() == "@")
		{
			tech += bforeAt[i].ToString();
			nCount = i;
		}
	}
	for(int i=nCount+1; i<bforeAt.Length; i++)
		tech += bforeAt[i].ToString();
	
	return tech;
}

DataRow CheckSN(string sn)
{
	DataSet dscsn = new DataSet();

	string sc = " SELECT s.purchase_order_id, s.id, s.sn, s.product_code, CONVERT(varchar(12),s.purchase_date, 13) AS purchase_date, s.cost, ";
	sc += " CONVERT(varchar(40),s.prod_desc) AS name, s.supplier, s.warranty, s.status ";
	sc += " FROM stock s";
	sc += " WHERE sn='" + sn + "'";
//DEBUG("sc=", sc);		
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

bool checkSerialNO()
{
	string s_sn = "";
	bool bCheckDuplicate = false;
	int sns = 0;
	if(Session["rma_sns"] != null)
		sns = (int)Session["rma_sns"];

	if(Request.Form["txtSN"] != null && Request.Form["txtSN"] != "")
	{
		s_sn = Request.Form["txtSN"].ToString();
//DEBUG("s_sn = ", s_sn);
		string sc = " SELECT serial_number FROM rma ";
		sc += " WHERE serial_number ='"+ s_sn +"'";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "checkSNinRMA");
			
			if(dst.Tables["checkSNinRMA"].Rows.Count <= 0)
			{
				sc = " SELECT s.id, s.sn, s.product_code, CONVERT(varchar(12),s.purchase_date, 13) AS purchase_date, s.cost, ";
				sc += " CONVERT(varchar(40),s.prod_desc) AS name, s.supplier, s.warranty, s.status ";
				sc += " FROM stock s";
				sc += " WHERE sn in(";
				for(int i=1; i<=sns; i++)
				{
			//DEBUG("sns="+sns.ToString(), " i="+i.ToString());
					sc += "'" + Session["rma_sn" + i.ToString()].ToString() + "', ";
				}
				sc += "'" + s_sn + "')";	
				try
				{
					myAdapter = new SqlDataAdapter(sc, myConnection);
					m_nCheckSNReturn = myAdapter.Fill(dst, "checkSN");
		//DEBUG("m_nchecksnreturn = ", m_nCheckSNReturn);
				}
				catch (Exception e)
				{
					ShowExp(sc,e);
					return false;
				}
				if(Request.QueryString["m"] != null)
					m_ncheck = int.Parse(Request.QueryString["m"].ToString());
		//DEBUG("m_ncheck = ", m_ncheck);
				if(m_nCheckSNReturn > 0 && m_nCheckSNReturn > m_ncheck) //found the new one
				{
					//store this sn to session object
					if(Session["rma_sns"] == null)
					{
						Session["rma_sns"] = 1;
						Session["rma_sn1"] = s_sn;
					}
					else
					{
						sns = (int)Session["rma_sns"] + 1;
						Session["rma_sns"] = sns;
						Session["rma_sn" + sns.ToString()] = s_sn;
					}
				}
				else 
				{
		//DEBUG("m_ncheck = ", m_ncheck);
					bool bCheck = true;
					for(int i=1; i<=sns; i++)
					{
						if(Session["rma_sn" + i.ToString()].ToString() == s_sn)
						{
							//Response.Write("<tr><td colspan=7><h4>SN# <font color=red>" + s_sn + " ALREADY EXIST!!!</font></h4></td></tr>");		
							bCheck = false;
							bCheckDuplicate = true;
						}
						else
							bCheckDuplicate = false;
					}
					string s_fault = "";
					if(Request.Form["txtFault"] != null)
						s_fault = Request.Form["txtFault"].ToString();
					if(bCheck && (s_fault == "" && s_fault == null))
						Response.Write("<tr><td colspan=4><h4>SN# <font color=red>" + s_sn + " NOT FOUND!!!</font></h4>");

					Response.Write("</td></tr>");
				}	
			}
			else
			{
				Response.Write("<tr><td align=center colspan=4><h4>SN# <font color=red>" + s_sn + " ALEARDY UNDER RMA PROCESSING!!!</font></h4></td></tr>");
				Response.Write("<tr><td  align=center colspan=4><input type=button value='back' "+Session["button_style"]+" onclick=\"window.history.back()\">");
				return false;
			}
				
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
	}
	//keep item couldn't find serial no# in the database
	if(!bCheckDuplicate)
	{
		if(((Request.Form["txtPDate"] != null && Request.Form["txtPDate"] != "") 
			||(Request.Form["txtPONumber"] != null && Request.Form["txtPONumber"] != "")
			||(Request.Form["txtPCode"] != null && Request.Form["txtPCode"] != "")
			||(Request.Form["txtFault"] != null && Request.Form["txtFault"] != "")
			||(Request.Form["txtPDesc"] != null && Request.Form["txtPDesc"] != "") 
				&& dst.Tables["checkSNinRMA"].Rows.Count <= 0 && m_nCheckSNReturn <=0 && !bCheckDuplicate))
		{
			string s_date = Request.Form["txtPDate"].ToString();
			string s_ponumber = Request.Form["txtPONumber"].ToString();
	//DEBUG("s_ponumber = ", s_ponumber);
			//DEBUG("sns +=", sns);
			//DEBUG("s_sn = ", s_sn);
			if(sns == 0)
			{
				if(Session["rma_sns"] == null)
				{
					Session["rma_sns"] = 1;
					Session["rma_sn1"] = s_sn;
				}
				else
				{
					sns = (int)Session["rma_sns"] + 1;
					Session["rma_sns"] = sns;
					Session["rma_sn" + sns.ToString()] = s_sn;
				}
			}
			else if (Session["rma_sn"] + (sns - 1).ToString() != s_sn)
			{
				if(Session["rma_sns"] == null)
				{
					Session["rma_sns"] = 1;
					Session["rma_sn1"] = s_sn;
				}
				else
				{
					sns = (int)Session["rma_sns"] + 1;
					Session["rma_sns"] = sns;
					Session["rma_sn" + sns.ToString()] = s_sn;
				}
			}
	//DEBUG("sn = ", s_sn);
		}
	}
	return true;
}


bool getSupplierName()
{
	string sc = " SELECT DISTINCT id, name, short_name 'Short Name', trading_name 'Supplier', company 'Company' ";
	sc += ", address1, address2, address3, phone, fax, email FROM card ";
	sc += " WHERE type = 3 ";
	string sid = "";
	if(Request.QueryString["id"] != null && Request.QueryString["id"] != "")
	{
		sid = Request.QueryString["id"].ToString();
		sc += " AND id = "+sid+"";
	}
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "card");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

bool BindRMAList()
{
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

	Response.Write("<form name=frm method=post >");
	Response.Write("<table align width=100% valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#E3DF5E><th>ID</th><th>Supplier</th><th>RMA DATE</th>");
	Response.Write("<th>RMA Staff</th><th>Repair#</th><th>&nbsp;</th><th>Supplier RA#</th><th>COURIER TICKET#</th><th>SENT??</th></tr>");
	bool bChange = true;
	int rows = dst.Tables["rmalist"].Rows.Count;
	if(Request.QueryString["rma_nav"] != "3")
	{
		Response.Write("<tr><td colspan=9 align=right><input type=submit name=cmd ");
		Response.Write(" onclick=\"return confirm('are you SURE want to process this');\" value='Send Faulty Parts' "+ Session["button_style"] +"></td></tr>");
	}
	Response.Write("<tr><th colspan=2><font color=red>Total RMA: "+ rows +"</font></th><th colspan=7 align=right>Select All: <input type=checkbox name=allbox value='Select All' onclick='CheckAll();'></th></tr>");
	
	//string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (m_page-1) * m_nPageSize;
	for(int i=startPage; i<dst.Tables["rmalist"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["rmalist"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRowSearch(dr, i, alterColor))
			break;
	}
		
	PrintPageIndexSearch();

	Response.Write("<input type=hidden name=hidrow value="+ rows +">");
	//Response.Write("<tr><td colspan=8 align=center>Select All: <input type=checkbox name=allbox value='Select All' onclick=CheckAll();></td></tr>");
	if(Request.QueryString["rma_nav"] != "3")
	{
		Response.Write("<tr><td colspan=9 align=right><input type=submit name=cmd ");
		Response.Write(" onclick=\"return confirm('are you SURE want to process this');\" value='Send Faulty Parts' "+ Session["button_style"] +"></td></tr>");
	}
	Response.Write("</table></form>");

	return true;
}


bool DrawRowSearch(DataRow dr, int i, bool alterColor)
{
		string id = dr["RMA NO"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_id = dr["supp_rmano"].ToString();
		string rma_date = dr["RMA Date"].ToString();
		string sent = dr["check_status"].ToString();
		string technician = dr["technician"].ToString();
		string pack = dr["pack_no"].ToString();
		string status = dr["status"].ToString();
		string check = dr["check_status"].ToString();
		string return_date = dr["return_date"].ToString();
		string repair = dr["repair_jobno"].ToString();
		//string id = dr["RMA NO"].ToString();
		//string id = dr["RMA NO"].ToString();
		if(alterColor)
		{
			Response.Write("<tr>");
			alterColor = false;
		}
		else
		{
			Response.Write("<tr bgcolor=#D3FCCC>");
			alterColor = true;
		}
		
		Response.Write("<th><a title='click to ProCess' href='rma.aspx?rma="+id+"&data=recorded' class=o>"+id+"</a></th>");
		Response.Write("<th>"+ supplier +"</th><th><a title='click to ProCess' href='rma.aspx?rma="+id+"&data=recorded' class=o>"+ rma_date +"</a></th>");
		Response.Write("<td align=center>"+ technician +"</td>");
		Response.Write("<th><a title='Print Replace statement' href='ra_slip.aspx?job="+ repair +"&ticket=' target=_new class=o>"+ repair +"</th>");
		Response.Write("<th><input type=button value='View Status' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"javascript:view_ra_window=window.open('view_ra.aspx?ra=supplier&id="+ id +"','', '')\" >");
		Response.Write("&nbsp;<input type=button value='View Log' "+ Session["button_style"] +" alt='view repair log' ");
		Response.Write(" onclick=\"javascript:repair_log_window=window.open('repair_log.aspx?ra_job="+id+"', '', '')\" ></th>");
		Response.Write("<th><input size=10% type=text name='supp_ra"+ i.ToString() +"' value="+ supplier_id +"></th>");
		if(sent == "1")
		{
			Response.Write("<td align=center><input size=12% type=text name=del"+ i.ToString() +" value="+ pack +"></td>");
			Response.Write("<td align=right><input size=5% type=checkbox name='sel"+ i.ToString() +"'></td>");
		}
		else if(sent == "2")
		{
			Response.Write("<th>"+pack+"</th><th><font color=red><b>Sent Already</b></font></th>");
		}
		else if(sent == "3")
		{
			Response.Write("<th>"+pack+"</th><th><font color=red><b>Returned on:"+ return_date +" </b></font></th>");
		}
		
		//Response.Write("</td>");
		Response.Write("</tr>");

		Response.Write("<input type=hidden name=hidid"+ i.ToString() +" value="+ id +">");
	
	return true;
}

void PrintPageIndexSearch()
{
	string task = Request.QueryString["task"].ToString();
	string rma = Request.QueryString["rma_nav"].ToString();
	Response.Write("<tr><td colspan=9>Page: ");
	
	int pages = dst.Tables["rmalist"].Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != m_page)
		{
			Response.Write("<a href='rma.aspx?task="+ task +"&rma_nav="+rma+"&sp=");
			Response.Write(i.ToString());
			Response.Write("'>");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	Response.Write("</td></tr>");
}

</script>

<asp:Label id=LFooter runat=server/>



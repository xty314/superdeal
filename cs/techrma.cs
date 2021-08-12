<script runat=server>

DataSet dst = new DataSet();

int m_nPageSize = 15;
int m_page = 1;

string m_rmano = "";
string m_nInvoice = "";
//current value
string m_status = "";
string m_jobno = "";
string m_repair_desc = "";
string m_charge = "";
string m_charge_detail = "";
string m_fdate = "";
string m_techname = "";
string m_customerEmail = "";
string m_customerName = "";
string header = "";
string condition = "";
int nNext = 0;


void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("technician"))
		return;
	
	string s_jobno = "";
	PrintAdminHeader();
	PrintAdminMenu();
	header = ReadSitePage("repair_header");
	
	if(Request.QueryString["p"] != null)
	{
		if(IsInteger(Request.QueryString["p"]))
			m_page = int.Parse(Request.QueryString["p"]);
	}
	condition = ReadSitePage("repair_condition");
	
	if(Request.Form["cmd"] == "Replace with New Product" )
	{
		if(!doRAReplacement())
			return;
	}
	if(Request.QueryString["deljob"] != "" && Request.QueryString["deljob"] != null)
	{
		string s_delete = Request.QueryString["deljob"];
		if(!deleteRepairJob(s_delete))
			return;
	}
	if(Request.Form["update"] != null ||(Request.Form["cmd"] == "Search Product Code" 
		|| Request.QueryString["s"] == "1" && Request.Form["search"] != null))
	{
		if(!updateRAJob())
			return;
	}
	if(Request.Form["cmd"] == "Authorize" || Request.Form["cmd"] == "Receive Product")// ||Request.Form["cmd"] == "NO!!")
	{
		if(!updateStatus())
			return;
	}
	if(Request.Form["cmd"] == "Update Status" || Request.Form["cmd"] == "Update Repair Job")
	{
		m_jobno = Request.QueryString["job"].ToString();
		m_repair_desc = Request.Form["txtfault"].ToString();
		m_charge = Request.Form["txtTotal"].ToString();
		m_charge_detail = Request.Form["txtDetail1"].ToString()+" $"+ Request.Form["txtPrice1"].ToString()+"<br>";
		m_charge_detail	+=Request.Form["txtDetail2"].ToString()+" $"+ Request.Form["txtPrice2"].ToString()+"<br>";
		m_charge_detail	+=Request.Form["txtDetail3"].ToString()+" $"+ Request.Form["txtPrice3"].ToString()+"<br>";
		m_charge_detail	+=Request.Form["txtDetail4"].ToString()+" $"+ Request.Form["txtPrice4"].ToString()+"<br>";
 		m_charge_detail	+=Request.Form["txtDetail5"].ToString()+" $"+ Request.Form["txtPrice5"].ToString();

		m_status = Request.Form["Status"].ToString();
		m_fdate = Request.Form["txtdate"].ToString();
		m_customerEmail = Request.Form["customer_email"];
		m_customerName = Request.Form["customer_name"];

		if(!updateRepairJob()) 
			return;
	}
	
	if(Request.QueryString["jobno"] != null)
	{
		m_jobno = Request.QueryString["jobno"];
	
		if(Request.QueryString["print"] == "y")
			PrintRepairForm();
		else
		{
			//DEBUG("stustat= ", m_jobno);
			if(!GetRepairForm(m_jobno))
				return;	

			DisplayRepairJob(m_jobno);
		}
	}
	else if((Request.QueryString["job"] != null && Request.QueryString["inv"] != null)
		|| (Request.QueryString["job"] != null && Request.QueryString["sn"] != null))
	{
		if(!SNReplacement())
			return;
	}
	else
	{
		if(Request.Form["cmd"] == "Search Repair" || Request.Form["txtSearch"] != null)
		{
			if(Request.Form["txtSearch"] != null)
				m_jobno = Request.Form["txtSearch"].ToString();
			
			if(!SearchValideJob(m_jobno))
				return;
			//if(!GetRepairForm(m_jobno))
			//	return;	

			//DisplayRepairJob(m_jobno);
		}
		else
		{
			if(!searchCustomerID())
				return;
			BindGrid();
			customerIDGrid();
		}
	}	
	LFooter.Text = m_sAdminFooter;	
}

bool doRAReplacement()
{
//DEBUG("replace = ", Request.Form["hidReplace"].ToString());
	if(Request.Form["hidReplace"] != "1" || (Request.Form["hidReplace"] == "" && Request.Form["hidReplace"] == null))
	{
		int rNumber = 0;
		string s_job = Request.QueryString["job"].ToString();
		string s_pcode = Request.Form["newPCode"].ToString();
		string s_newSN = Request.Form["newSN"].ToString();
		
		string slt = Request.Form["slt"].ToString();
		string s_note = "";
		s_note = s_note + ": Replace with Product#: " + s_pcode + " SN#: " + s_newSN;
		string q_inv = "", q_sn = "";
		if(Request.QueryString["sn"] != null)
			q_sn = Request.QueryString["sn"].ToString();
		if(Request.QueryString["inv"] != null)
			q_inv = Request.QueryString["inv"].ToString();
		string q_card = Request.QueryString["cardid"].ToString();
		
		//DEBUG("m_nInvoice = ", m_nInvoice);
		//if(q_inv != "" && q_inv != null)
		//{
		//	if(!replaceRA(q_sn, q_inv, s_newSN, s_pcode))
		//		return false;
		//}
		//else
		//{
		//	if(!createINV(s_newSN, s_pcode, ref rNumber))
		//		return false;
		//}
		/*string sc = "UPDATE repair ";
		sc += " SET for_supp_ra = 1 ";
		sc += " , note = '"+ msgEncode(s_note) +"' ";
		sc += " , status = 5 ";
		if(m_nInvoice != null && m_nInvoice != "")
			sc += " ,invoice_number = "+ m_nInvoice + " ";
		sc += " WHERE id = "+ s_job +" ";

		// fail item
		if(slt == "0")
			sc += AddSerialLogString(q_sn, "Product is Faulty, Replace with new Item Product# "+s_pcode, "", "", s_job, "");
					//pass item
		if(slt == "1")
			sc += AddSerialLogString(q_sn, "Product is Working Fine!! Still Replace with new Item: Product# "+s_pcode, "", "", s_job, "");
			//-------------------- add log to repair log  tee: 9-4-03
		sc += AddRepairLogString(msgEncode(s_note), m_nInvoice, s_pcode, s_job, s_newSN,"");
		//----------------------------------
		if(g_bRetailVersion)
		{//update qty for retial version
			sc = " UPDATE stock_qty ";
			sc += " SET qty = ( qty - (1)) ";
			sc += " WHERE code = "+ s_pcode + " ";
		}
		else
		{
		//update qty for whole sale version
			sc = " UPDATE product ";
			sc += " SET stock = (stock - (1)) ";
			sc += " WHERE code = "+ s_pcode + " ";
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
		}*/
	}
	else
		Response.Write(" <center><h5>Unable to replace Product, Please check Product Code </h5></center>");	
	return true;
}

bool updateRAJob()
{
	int rNumber = 0;
	string s_sr = "";
	if(Request.Form["search"] != null)
		s_sr = Request.Form["search"].ToString();

	int nRows =0;
	string s_note = "", s_job = "", s_result = "";
	if(Request.QueryString["job"] != null && Request.QueryString["job"] != "")
		s_job = Request.QueryString["job"].ToString();
	if(Request.Form["comment"] != null && Request.Form["comment"] != "")
		s_note = Request.Form["comment"].ToString();
	
	string s_pcode = "", s_newSN = "", s_inv = "", slip = "";
	if(Request.Form["newPCode"] != null && Request.Form["newPCode"] != "")
		s_pcode = Request.Form["newPCode"].ToString();
	if(Request.Form["newSN"] != null && Request.Form["newSN"] != "" && Request.Form["newSN"] != "Enter SN# Here")
		s_newSN =Request.Form["newSN"].ToString();
	if(Request.Form["soldinv"] != null && Request.Form["soldinv"] != "")
		s_inv = Request.Form["soldinv"].ToString();
	if(Request.Form["slip"] != null && Request.Form["slip"] != "")
		slip = Request.Form["slip"].ToString();
//DEBUG("s_pcode = ", s_pcode);
//DEBUG("s_newSN = ", s_newSN);

	string sc = "";
	bool isNum = true;
	int ptr = 0;
	while (ptr < s_sr.Length)
	{
		if (!char.IsDigit(s_sr, ptr++))
		{
			isNum = false;
			break;
		}
	}
	while (ptr < s_pcode.Length)
	{
		if (!char.IsDigit(s_pcode, ptr++))
		{
			isNum = false;
			break;
		}
	}
	if(s_newSN != "" && s_newSN != "Enter SN# Here")
	{
		sc = " SELECT Distinct TOP 100 sn, product_code AS code, supplier, supplier_code, CONVERT(varchar(50),prod_desc) AS name FROM stock ";
		sc += " WHERE sn = '"+s_newSN+"' OR sn like '%"+s_newSN+"'";
		sc += " OR prod_desc like '%"+s_newSN+"%' ";
		sc += " ORDER BY product_code ASC ";
	}
	else
	{
		sc = " SELECT Distinct TOP 100 product_code AS code, supplier, supplier_code, CONVERT(varchar(50),prod_desc) AS name FROM stock ";
		if(isNum)
		{	
			if(s_pcode != "")
				sc += " WHERE product_code = "+ s_pcode +"";
		}
		if(s_sr != "")
		{
			if(isNum)
				sc += " WHERE product_code = "+ s_sr +" ";
			else
			{
				sc += " WHERE sn like '%"+s_sr+"%' ";
				sc += " OR prod_desc like '%"+ s_sr +"%' ";
				sc += " ORDER BY product_code ASC ";
			}
		}	
	}
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(dst,"chkSN");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	string q_inv = "", q_sn = "";
	if(Request.QueryString["sn"] != null)
		q_sn = Request.QueryString["sn"].ToString();
	if(Request.QueryString["inv"] != null)
		q_inv = Request.QueryString["inv"].ToString();
	string q_card = Request.QueryString["cardid"].ToString();
	//insert faulty product and replacement
	
	//else
	{
		Response.Write("<form name=frm method=post action='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(q_sn)+"&inv="+q_inv+"&s=1&cardid="+q_card+"'>");
		Response.Write("<table width=80% align=center cellspacing=0 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=4><input type=text name=search> <input type=submit name=cmd value='Search Product Code' "+ Session["button_style"]+"</td></tr>");
		Response.Write("<script");
		Response.Write(">\r\ndocument.frm.search.focus();\r\n</script");
		Response.Write(">\r\n");
		if(s_newSN != "" && s_newSN != "Enter SN# Here")
			Response.Write("<tr bgcolor=#E3e3e3><th>Product#</th><th>SN#</th><th>Supplier</th><th>Supplier Code</th><th>Description</th></tr>");
		else
			Response.Write("<tr bgcolor=#E3e3e3><th>Product#</th><th>Supplier</th><th>Supplier Code</th><th>Description</th></tr>");
			
		if(nRows > 0)
		{
			//Response.Write("<center><font color=Red><h5>Correct Product Code: "+s_pcode+" OR SN#: "+s_newSN+"</h5></font></center>");
			for(int i=0; i<nRows; i++)
			{
				string sn = "";
				DataRow dr = dst.Tables["chkSN"].Rows[i];
				if(s_newSN != "" && s_newSN != "Enter SN# Here")
					sn = dr["sn"].ToString();
				string code = dr["code"].ToString();
				string supplier = dr["supplier"].ToString();
				string supplier_code = dr["supplier_code"].ToString();
				string name = dr["name"].ToString();
				Response.Write("<tr>");
				if(s_newSN != "")
				{
					Response.Write("<td><a title='select Product Code' href='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(sn)+"&inv="+q_inv+"&cardid="+q_card+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&s_sn="+HttpUtility.UrlEncode(sn)+"&selected=1'><font color=blue><b>"+code+"</b></font></a></td>");
					Response.Write("<td><a title='select Serial Number' href='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(q_sn)+"&inv="+q_inv+"&cardid="+q_card+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&s_sn="+HttpUtility.UrlEncode(sn)+"&selected=1'><font color=blue><b>"+sn+"</b></font></a></td>");
					Response.Write("<td>"+supplier+"</td>");
					Response.Write("<td><a title='select Serial Number' href='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(q_sn)+"&inv="+q_inv+"&cardid="+q_card+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&s_sn="+HttpUtility.UrlEncode(sn)+"&selected=1'><font color=blue><b>"+supplier_code+"</b></font></a></td>");
					Response.Write("<td><a title='select Product Code' href='techrma.aspx?job="+s_job+"&sn="+q_sn+"&inv="+q_inv+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&s_sn="+HttpUtility.UrlEncode(sn)+"&selected=1'><font color=blue><b>"+name+"</b></font></a></td>");
				}
				else
				{
					Response.Write("<td><a title='select Product Code' href='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(sn)+"&inv="+q_inv+"&cardid="+q_card+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&selected=1'><font color=blue><b>"+code+"</b></font></a></td>");
					Response.Write("<td><a title='select Product Code' href='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(sn)+"&inv="+q_inv+"&cardid="+q_card+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&selected=1'><font color=blue><b>"+supplier+"</b></font></a></td>");
					Response.Write("<td><a title='select Product Code' href='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(sn)+"&inv="+q_inv+"&cardid="+q_card+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&selected=1'><font color=blue><b>"+supplier_code+"</b></font></a></td>");
					Response.Write("<td><a title='select Product Code' href='techrma.aspx?job="+s_job+"&sn="+q_sn+"&inv="+q_inv+"&name="+HttpUtility.UrlEncode(name)+"&code="+code+"&cardid="+q_card+"&selected=1'><font color=blue><b>"+name+"</b></font></a></td>");
				}
				Response.Write("</tr>");
			}
	}
	Response.Write("</table>");
	Response.Write("</form>");
	}
	return true;
}

bool createINV(string new_SN, string new_Pcode, ref int rNumber)
{
	if(dst.Tables["insertINV"] != null)
		dst.Tables["insertINV"].Clear();

	//rNumber = 10000;
	DataRow dr;
	string sc = "SELECT TOP 1 invoice_number FROM invoice ORDER BY invoice_number DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "insertINV") == 1)
			rNumber = int.Parse(dst.Tables["insertINV"].Rows[0]["invoice_number"].ToString()) + 1;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	//if(new_Pcode != "" || new_SN != "")
	//{
		sc = " SELECT product_code, sn, cost, purchase_order_id, supplier, supplier_code, status, prod_desc ";
		sc += " FROM stock  WHERE status = 2";
		if(new_SN != "" && new_SN != null)
			sc += " AND sn = '"+ new_SN +"' ";
		else if(new_Pcode != "" && new_Pcode != null)
			sc += " AND product_code = '"+ new_Pcode +"' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst,"getProduct");
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
		if(dst.Tables["getProduct"].Rows.Count > 0)
		{
			dr = dst.Tables["getProduct"].Rows[0];
			string sn = "";
			string name = dr["prod_desc"].ToString();
			string supplier = dr["supplier"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string code = dr["product_code"].ToString();
			m_nInvoice = rNumber.ToString();
			string card_id = Request.QueryString["cardid"].ToString();
			if(new_SN != "")
				sn = dr["sn"].ToString();
			string cost = dr["cost"].ToString();
			sc = " SET DATEFORMAT dmy ";
			sc += " INSERT INTO orders (number, card_id, invoice_number, status, record_date, sales, sales_note) ";
			sc += " VALUES (";
			sc += " 0 , ";
			sc += " "+ card_id + ","+ rNumber +", 4, GETDATE(), "+ Session["card_id"].ToString() +", 'Product Replacement from RA department SN#:"+ new_SN +" and Product Code:"+ new_Pcode +"') ";

			sc += " SET DATEFORMAT dmy ";
			sc += "INSERT INTO sales (invoice_number, code, supplier, supplier_code, supplier_price, name, quantity, commit_price ";
			if(new_SN != "")
				sc += " ,sn ";
			sc += " )";
			sc += " VALUES( "+rNumber+", "+ new_Pcode+", '"+supplier+"',  '"+supplier_code+"', "+cost+",'"+name+"', 1, "+cost+" ";
			if(new_SN != "")
				sc += ", '"+new_SN+"' ";
			sc += " )";
			
			if(new_SN != "")
			{
				sc += " INSERT INTO sales_serial (code, sn, invoice_number) ";
				sc += " VALUES ("+code+", '"+new_SN+"', "+rNumber+" )";
			}	

			sc += " INSERT INTO invoice ( tax, total, branch, type,  price, commit_date, card_id, sales, sales_note) ";
			sc += " VALUES(0, 0, 1, 4, 0, GetDate(), "+card_id+", "+Session["card_id"]+", 'Product Replacement from RA department SN#:"+ new_SN +" and Product Code:"+ new_Pcode +"')";
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
			Response.Write("<center><h5>Sorry!!! The product your chosen is currently not available, Please choose other one</h5></center>");
		}
//DEBUG("m_nInvoice _= ", m_nInvoice);
	//}
	//Response.Write("bybe");
	return true;
}

bool replaceRA(string sold_sn, string s_inv, string new_sn, string new_code)
{
	string sc = "";
	DataRow dr;
	int rows = 0; int rNumber = 0;
	if(sold_sn != "")
	{
		sc = " SELECT invoice_number, code  FROM sales_serial ";
		sc += " WHERE sn = '"+ sold_sn +"' ";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst,"getInv");
		}
		catch (Exception e)
		{
			ShowExp(sc,e);
			return false;
		}
	}
	string card_id = Request.QueryString["cardid"].ToString();
	string sold_inv = "", sold_code = "";
	if(rows == 1)
	{
		dr = dst.Tables["getInv"].Rows[0];
		s_inv = dr["invoice_number"].ToString();
		sold_code = dr["code"].ToString();
	}
	//DEBUG("sold code from sn = ", sold_code);
	//get Previous Invoice Detail
	sc = " SELECT invoice_number, quantity, name, supplier, supplier_code, serial_number, commit_price, ";
	sc += " supplier_price, status, ticket, ";
	sc += " note, ship_date, processed_by, part, id ";
	sc += " FROM sales ";
	sc += " WHERE invoice_number = "+ s_inv +" "; 
	//if( != "")
	//	sc += " WHERE invoice_number = "+ sold_inv+" ";//AND code = "+sold_code+"";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		int i = myAdapter.Fill(dst,"getInvDetails");
//DEBUG(" i = ", i);
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	//insert replacement product to invoice table
	if(dst.Tables["getInvDetails"].Rows.Count > 0)
	{
		dr = dst.Tables["getInvDetails"].Rows[0];
		string name = dr["name"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		//string commite_price = dr["commite_price"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		//string shipby = dr["shipby"].ToString();
		string ticket = dr["ticket"].ToString();
		string process_by = "";
		string invoice_number = dr["invoice_number"].ToString();
		m_nInvoice = invoice_number;
		//DEBUG("m_nInvoice _= ", m_nInvoice);
		//insert to sales_serial table
		if(sold_sn != "")
		{
			sc += "INSERT INTO sales_serial (sn, code, invoice_number) ";
			sc += " VALUES('"+new_sn+"' ,"+new_code+",  "+s_inv+") ";
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
		//if((s_inv != "") || (sold_sn != "" && s_inv != ""))
		//{
			if(Session["id"] != null)
				process_by = Session["id"].ToString();
			else
				process_by = "0";
		
			//sc = " SET DATEFORMAT dmy ";
			//sc += " INSERT INTO orders (number, card_id, invoice_number, status, record_date, sales, sales_note) ";
			//sc += " VALUES ( ";
			//sc += " 0 , ";
			//sc += ""+ card_id + ","+ s_inv +", 4, GETDATE(), "+ Session["name"].ToString() +", 'RA Replacement') ";
			
			sc += " SET DATEFORMAT dmy ";
			sc += " INSERT INTO sales (invoice_number, quantity, name, supplier ";
			sc += " , code ,supplier_code, commit_price, supplier_price, status, ";
			sc += "  note, ship_date, processed_by) ";
			sc += " VALUES("+s_inv+", 1, '"+name+"', '"+supplier+"', "+new_code+",'"+supplier_code+"', ";
			sc += "  0, "+supplier_price+", 3, 'RA Replacement with Product#:"+new_code+"', ";
			sc += " GetDATE(), "+process_by+") ";
			
			//update new serial number is sold
			if(new_sn != "" && new_sn != null)
				sc += " UPDATE stock SET status = 1 WHERE sn = '"+new_sn+"' ";		
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
		//}
	}	
	else
	{
		createINV(new_sn, new_code, ref rNumber);
	}
	//	Response.Write("<center><h5><font color=red><b>No RMA Replacement, Check Invoice Number: '"+s_inv +"'!!!</b></font></h5></center>");
	return true;
}

bool updateStatus()
{
	string sClick = Request.Form["cmd"].ToString();

	int row = int.Parse(Request.Form["hidrow"]);
	for(int i=0; i<row; i++)
	{
		string check = "", comment = "", id = "";
		if(Request.Form["check"+i.ToString()] != null)
			check = Request.Form["check"+i.ToString()].ToString();
		if(Request.Form["hidid"+i.ToString()] != null)
			id = Request.Form["hidid"+i.ToString()].ToString();
		if(sClick == "Authorize")
		{
			if(Request.Form["comment"+i.ToString()] != null)
				comment = Request.Form["comment"+i.ToString()].ToString();
		}
		comment = msgEncode(comment);
	//DEBUG("check = ", check);
		if(check != "")
		{
			if(sClick == "Authorize")
			{	
				string sc = " UPDATE repair ";
				if(check == "1")
					sc += " SET status = 2 ";
				if(check == "0")
					sc += " SET status = 7 ";
					sc += " , note = '"+ comment +"' ";
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
			if(sClick == "Receive Product")
			{
				string sc = " UPDATE repair ";
				if(check == "1")
					sc += " SET status = 3 ";
				if(check == "0")
					sc += " SET status = 7 ";
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
	return true;
}

bool SearchValideJob(string s_job)
{
	bool isNum = true;
	int ptr = 0;
	while (ptr < s_job.Length)
	{
		if (!char.IsDigit(s_job, ptr++))
		{
			isNum = false;
			break;
		}
	}
	string sc = " SELECT r.id, r.repair_date, c.name FROM repair r INNER JOIN card c ON c.id = r.customer_id";
	if(isNum)
		sc += " WHERE r.id = " + s_job;
	else
		sc += " WHERE c.name like '%"+s_job+"%' ";
	int rows_return = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows_return = myAdapter.Fill(dst, "List");
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	if(rows_return > 0)
	{
		Response.Write("<br><br>");
		Response.Write("<center><h5>Please Select Customer or Job# to Process</h5></center>");
		Response.Write("<table width=100% align=center cellspacing=3 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#EEEEEE><th>JOB#</th><th>Repair Date</th><th>Customer Name</th></tr>");
		bool bChange = true;
		for(int i=0; i<rows_return; i++)
		{
			DataRow dr = dst.Tables["List"].Rows[i];
			string id = dr["id"].ToString();
			string r_date = dr["repair_date"].ToString();
			string name = dr["name"].ToString();
			if(!bChange)
			{
				Response.Write("<tr bgcolor=#EEEEEE>");
				bChange = false;
			}
			else
			{
				Response.Write("<tr>");
				bChange = true;
			}
			Response.Write("<td><a href='techrma.aspx?jobno="+id+"' title='Select Repair ID'><font color=blue><b>"+id+"</b></font></a></td>");
			Response.Write("<td><a href='techrma.aspx?jobno="+id+"' title='Select Repair Date'><font color=blue><b>"+r_date+"</b></font></a></td>");
			Response.Write("<td><a href='techrma.aspx?jobno="+id+"' title='Select Customer Name'><font color=blue><b>"+name+"</b></font></a></td>");
			Response.Write("</tr>");
		}
		return true;
	}
	else
	{
		Response.Write("<script language=javascript> <!--");
		const string invalid = @"
			window.alert('NO JOB FOUND');
			window.location=('techrma.aspx');
		";
		Response.Write("-->");
		Response.Write(invalid);
		Response.Write("</script");
		Response.Write(">");
		
		return false;
		
	}
}
//Product Replacement on 10/FEB/03 tee
bool SNReplacement()
{
	if(dst.Tables["customerid"] != null)
		dst.Tables["customerid"].Clear();

	if(!searchCustomerID())
		return false;

	Response.Write("<script language=javascript>");
	Response.Write("<!---hide from old browser");
	string s = @"		
		function chkSN()
		{			
			//if(document.frm.slip.value == ''){
			//	window.alert('Please Input Delivery ticket Number');
			//	document.frm.slip.focus();
			//	document.frm.slip.select();
			//	return false;
			//}
			//else{
				window.location.href = ('ra_slip.aspx?i='+document.frm.hidinv.value+'&po='+document.frm.slip.value+'&pcode='+document.frm.newPCode.value+'&sn='+document.frm.newSN.value+'' target=_blank);
			//	return true;
			//}
		}
		function chkcode()
		{
			if(document.frm.newPCode.value == '')
			{
				window.alert('Please enter your Product Code');
				document.frm.newPCode.focus();
				document.frm.newPCode.select();
				return false;
			}
			else
			{
				window.confirm('Are you SURE want to do this???');
				return true;
			}
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
	Response.Write("--->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	
	string s_job = "";
	
	Response.Write("<center >");
	string result = "", s_slip = "", spcode = "";
	if(Request.QueryString["result"] != null && Request.QueryString["result"] != "")
		result = Request.QueryString["result"].ToString();	
	if(Request.QueryString["job"] != null && Request.QueryString["job"] != "")
		s_job = Request.QueryString["job"].ToString();
	//if(Request.QueryString["sn"] != null && Request.QueryString["sn"] != "")
	//	s_sn = Request.QueryString["sn"].ToString();
	//if(Request.QueryString["inv"] != null && Request.QueryString["inv"] != "")
	//	s_inv = Request.QueryString["inv"].ToString();
	string s_inv = dst.Tables["customerid"].Rows[0]["invoice_number"].ToString();
	string s_sn = dst.Tables["customerid"].Rows[0]["Serial Number"].ToString();
	string s_note = dst.Tables["customerid"].Rows[0]["note"].ToString();
	string card_id = dst.Tables["customerid"].Rows[0]["customer_id"].ToString();
	if(Request.Form["slip"] != null && Request.Form["slip"] != "")
		s_slip = Request.Form["slip"].ToString();
	if(Request.Form["newPCode"] != null && Request.Form["newPCode"] != "")
		spcode =  Request.Form["newPCode"].ToString();
	
	if(Request.QueryString["selected"] == "1")
		Response.Write("<form name=frm action='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(s_sn)+"&inv="+s_inv+"&cardid="+card_id+"&selected=1' method=post>");
	else
		Response.Write("<form name=frm action='techrma.aspx?job="+s_job+"&sn="+HttpUtility.UrlEncode(s_sn)+"&inv="+s_inv+"&cardid="+card_id+"' method=post>");
	
	Response.Write("<input type=hidden name=hidinv value="+m_nInvoice+">");

	Response.Write("<br>");
	Response.Write("<h5>Process Product Replacement&nbsp;<h6>Job#: "+s_job+"</h6></h5>");
	
	if(dst.Tables["customerid"].Rows[0]["for_supp_ra"].ToString() == "1")
	{
		Response.Write("<h5> This Product has already been replace, please check the repair log</h5>");
		Response.Write("<input type=button value='View Repair Log' "+ Session["button_style"] +" alt='view repair log' ");
		Response.Write(" onclick=\"window.location=('repair_log.aspx?job="+s_job+"')\" target=new>");
		Response.Write("&nbsp;&nbsp;<input type=button value='View Invoice' "+ Session["button_style"] +" alt='view invoice log' ");
		Response.Write(" onclick=\"window.location=('invoice.aspx?"+s_inv+"')\" target=new>");
			
	}
	else
	{
		Response.Write("<input type=hidden name=hidReplace value='"+ dst.Tables["customerid"].Rows[0]["for_supp_ra"].ToString() +"'>");
		Response.Write("<table align=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td colspan=2><input type=button onclick=\"javascript:viewcard_window=window.open('view_ra.aspx?");
		Response.Write("job="+ s_job +"','','width=640,height=480')\" value='View Customer Detail & Description' " + Session["button_style"] + ">");
		Response.Write("<input type=submit name='update' value='"+ DateTime.Now.ToString() +"' "+Session["button_style"]+"></td></tr>");	
		Response.Write("<tr><th bgcolor=#E3E3E3>Sold INV#</th><td>");
		Response.Write("<a title='Click to check invoice details' href='invoice.aspx?"+s_inv+"'><font color=blue target=_blank><b>"+s_inv+"</b></font></a></td></tr>");
		Response.Write("<tr><th  bgcolor=#E3E3E3>Sold SN# </th><td>");
		Response.Write("<a title='Click to check serial# details' href='snsearch.aspx?sn=" + HttpUtility.UrlEncode(s_sn)+"' target=_blank><font color=blue><b>"+s_sn+"</b></font></a></td></tr>");
		Response.Write("</tr>");
		string s_comment = "";
		if(Request.Form["comment"] != null && Request.Form["comment"] != "")
			s_comment = Request.Form["comment"].ToString();
		if(Request.QueryString["code"] != null)
			spcode = Request.QueryString["code"].ToString();
		if(s_comment != "")
			Response.Write("<tr><th bgcolor=#E3E3E3>Fault Description</th><td><textarea rows=4 name=comment value='"+s_comment+"' >"+s_comment+"</textarea></td></tr>");
		else
			Response.Write("<tr><th bgcolor=#E3E3E3>Fault Description</th><td><textarea rows=4 name=comment value='"+s_note+"'>"+s_note+"</textarea></td></tr>");
		Response.Write("<tr><td>&nbsp;</td></tr>");
		string selected = "";
		if(Request.Form["slt"] != null)
			selected = Request.Form["slt"].ToString();
		if(selected == "0")
			Response.Write("<tr bgcolor=#e3e3e3><th>Tested Result: </th><td><select name=slt><option value=0 selected>Fail<option value=1>Pass</select></td></tr>");
		else
			Response.Write("<tr bgcolor=#e3e3e3><th>Tested Result: </th><td><select name=slt><option value=0 >Fail<option value=1 selected>Pass</select></td></tr>");
		
		Response.Write("<tr><th bgcolor=#E3E3E3><font color=red>Enter Product# For Replacement</font></th><td><input type=text name=newPCode value='"+spcode+"' >&nbsp;</td></tr>");
		
		string s_txtSN = "";
		if(Request.QueryString["s_sn"] != null)
			s_txtSN = Request.QueryString["s_sn"].ToString();
		if(Request.Form["newSN"] != null )
			s_txtSN = Request.Form["newSN"].ToString();
		//{	
		//	Response.Write("<tr><th bgcolor=#E3E3E3>New SN#</th><td><input type=text name=newSN value='"+s_txtSN+"'></td></tr>");	
		//}
		//else
			Response.Write("<tr><th bgcolor=#E3E3E3>New SN#</th><td><input type=text name=newSN value='"+s_txtSN+"'></td></tr>");
		string name = "";
		if(Request.QueryString["name"] != null)
			name = Request.QueryString["name"].ToString();
		if(Request.Form["desc"] != null)
			name = Request.Form["desc"].ToString();
		Response.Write("<tr><th bgcolor=#E3E3E3>Product Description</th><td><input type=text name=desc value='"+name+"'></td></tr>");
		Response.Write("<tr><td align=right colspan=2><input type=submit name=cmd ");
		Response.Write(" onclick=\"return confirm('Are you SURE want to Continue???');\" ");
		Response.Write(" value='Replace with New Product'"+Session["button_style"]+"></td></tr>");
	//DEBUG("m_nINvoice = ", m_nInvoice);
		if(Request.QueryString["selected"] == "1" && Request.Form["cmd"] == "Replace with New Product")
		{
			if(s_slip != "")
				Response.Write("<tr><th bgcolor=#E3E3E3>Delivery Ticket#</th><td><input type=text  name=slip value='"+s_slip+"'></td></tr>");
			else
				Response.Write("<tr><th bgcolor=#E3E3E3>Delivery Ticket#</th><td><input type=text name=slip value=''></td></tr>");
			Response.Write("<tr><td align=right colspan=2><input type=button name=cmd ");
			Response.Write("  onclick='return chkSN();' ");
			Response.Write(" value='Print Replacement Slip' " + Session["button_style"] + " >\n\r");
		}	
		Response.Write("</table>");
			

		Response.Write("</form>");
	}
	return true;
}

bool getSNDetails(string s_sn)
{
	string sc = " SELECT sn, product_code, CONVERT(varchar(12), purchase_date, 13) AS purchae_date, status, prod_desc ";
	sc += " FROM stock ";
	sc += " WHERE sn = '"+ s_sn +"'" ;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"GetSN");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}
bool getInvDetails(string s_inv)
{
	//string sc = " SELECT * ";
	string sc = " SELECT i.invoice_number, CONVERT(varchar(12), i.commit_date,13) AS commit_date,  i.price, s.serial_number ";
	sc += " FROM invoice i INNER JOIN sales s ON s.invoice_number = i.invoice_number ";
	sc += " WHERE i.invoice_number = "+ s_inv +"";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"GetINV");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}
//----- Product Replacement END here

bool PrintRepairForm()
{
	string s_job = "";
	if(Request.QueryString["jobno"] != null)
		s_job = Request.QueryString["jobno"].ToString();
	
	if(!GetRepairForm(s_job))
		return false;
	Response.Write("<html>");
	Response.Write("<body onload='window.print()'>");
		
	if(dst.Tables["jobform"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["jobform"].Rows[0];
		string sn = dr["serial_number"].ToString();
		string cumstomer_id = dr["customer_id"].ToString();
		string cumstomer_email = dr["email"].ToString();
		string cumstomer_name = dr["trading_name"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string note = dr["note"].ToString();
		string name = dr["name"].ToString();
		string company = dr["company"].ToString();
		string addr1 = dr["address1"].ToString();
		string addr2 = dr["address2"].ToString();
		string city = dr["address3"].ToString();
		string phone = dr["phone"].ToString();
		string email = dr["email"].ToString();
		string status = dr["status"].ToString();
		string fault_desc = dr["fault_desc"].ToString();
		string repair_date = dr["repair_date"].ToString();
		string finish_date = dr["repair_finish_date"].ToString();
		string charge_detail = dr["charge_detail"].ToString();
		string charge = dr["charge"].ToString();
		string technician = dr["technician"].ToString();
		string prod_desc = dr["prod_desc"].ToString();
	
		Response.Write("<form name=frmPrint>");
		//Response.Write("</td><td align=right valign=bottom>");
		Response.Write("<table width=100% valign=center cellspacing=1 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:5pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td><table><tr><td>"+header+"</td></tr></table></td>");
		Response.Write("<td align=center colspan=2><h5><u>Job Number:</u> "+s_job+"</h5></td></tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");
		Response.Write("<tr bgcolor=red><td colspan=5>&nbsp;</td></tr>");
		Response.Write("<tr><td colspan=5>");
		Response.Write("<table width=100% align valign=center cellspacing=1 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#E3E3E3><th align=left>Repair Date:</th><td>"+repair_date+"</td>");
		Response.Write("<th align=left>Address:</th><td>&nbsp;</td><td>&nbsp;</td></tr>");
		Response.Write("<tr><th align=left>Name:</th><td>"+name+"</td><td>"+addr1+"</td></tr>");
		Response.Write("<tr><th align=left>Company:</th><td>"+company+"</td><td>"+addr2+"</td></tr>");
		Response.Write("<tr><th align=left>Phone:</th><td>"+phone+"</td><td>"+city+"</td></tr>");
		Response.Write("<tr><th align=left>Email:</th><td>"+email+"</td><td>&nbsp;</td><td>&nbsp;</td></tr>");
		Response.Write("<tr bgcolor=#E3E3E3><td>Warrantee Repair??? </td><td>Complete System or Parts</td><td>Purchase Date</td><td>Warranty Date</td><td>&nbsp;</td></tr>");
		string commit_date = ""; string warranty_date = "";
		if(invoice != null && invoice != "")
		{
			if(!checkWarranty(invoice, sn))
				return false;

			if(dst.Tables["CheckInvoice"].Rows.Count > 0)
			{
				dr = dst.Tables["CheckInvoice"].Rows[0];
				commit_date = dr["commit_date"].ToString();
				warranty_date = dr["warranty"].ToString();
			}
		}
		if(sn == "complete_system")
			Response.Write("<tr><td>&nbsp;</td><td>Complete System</td><td>"+commit_date+"</td><td>"+warranty_date+"</td></tr>");
		else
			Response.Write("<tr><td>&nbsp;</td><td>Part with SN#:"+sn+"</td><td>"+commit_date+"</td><td>"+warranty_date+"</td></tr>");
			
		Response.Write("<tr><td>Fault Description:</td></tr>");
		Response.Write("<tr><td colspan=5><textarea rows=5 cols=80%>"+fault_desc+"</textarea></td></tr>");
		Response.Write("<tr bgcolor=#E3E3E3><td>Goods Received:</td><td colspan=4>"+prod_desc+"</td></tr>");
		if(status == "3")
			Response.Write("<tr><td>Repair Status:</td><td>YES!! it's done</td></tr>");
		Response.Write("<tr bgcolor=#E3E3E3><th align=left colspan=5>Repair Description</th></tr>");
		Response.Write("<tr><td colspan=5><textarea cols=80% rows=5>"+note+"</textarea></td></tr>");
		Response.Write("<tr bgcolor=#E3E3E3><th colspan=5 align=left>Repair Charges:</th></tr>");
		Response.Write("<tr><td>"+charge_detail+"</td></tr>");
		Response.Write("<tr><td colspan=2>Total Charge:</td><td colspan=3 >$"+charge+"</td></tr>");
		
		Response.Write("<tr bgcolor=#E3E3E3><th colspan=3 align=right>Repair Updated Date:</th>");
		//finish_date = DateTime(finish_date).ToString("dd/MMM/yyyy");
		Response.Write("<td>"+finish_date+"</td><td>&nbsp;</td></tr>");

		Response.Write("<tr><td colspan=5><hr size=1 solid></td>");
	//	Response.Write("<tr><td></td></tr>");

		Response.Write("<tr><td colspan=3>");
		Response.Write("<table width=100% align valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:7pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td><b>Conditions of Repair:</b></td></tr>");
		Response.Write("<tr><td>"+condition+"</td></tr>");
		Response.Write("</table> ");
		Response.Write("</td></tr>");
		
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;</td><td align=right>("+technician+")</td></tr>");
		Response.Write("<tr><td>&nbsp;</td><td>&nbsp;</td><td align=right>(Technician Signature)</td><td align=right>(Customer Signature)</td></tr>");
		Response.Write("</table>");
		Response.Write("</td></tr>");

		Response.Write("</table>");
	}
	Response.Write("</form>");
	Response.Write("</body>");
	Response.Write("</html>");

	return true;

}

bool checkWarranty(string invoice, string sn)
{
	string sc = " SELECT invoice_number, CONVERT(varchar(12),commit_date,13) AS commit_date ";
	if(sn == "complete_system")
		sc += " ,CONVERT(varchar(12), DATEADD(year, 2, commit_date), 13) AS warranty ";
	else
		sc += " ,CONVERT(varchar(12), DATEADD(year, 1, commit_date), 13) AS warranty ";
	sc += "FROM invoice ";
	
		sc += " WHERE invoice_number = "+invoice+"";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"CheckInvoice");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	
	return true;

}

bool searchCustomerID()
{
	string sc = "SELECT TOP 80 r.for_supp_ra, r.customer_id, r.note, r.invoice_number, r.purchase_date, c.trading_name 'Customer', r.id 'Job NO', r.serial_number 'Serial Number', CONVERT(varchar(50), r.fault_desc) AS 'Fault Description' , ";
	sc += " CONVERT(varchar(12), r.repair_date, 13) AS 'Repair Date', CONVERT(varchar(12), r.repair_finish_date, 13) AS 'Update Date' ";
	sc += " , e.name AS Status , r.status AS ra_status";
	sc += " FROM repair r LEFT OUTER JOIN enum e ON (e.id=r.status AND e.class='rma_status') LEFT OUTER JOIN card c ON c.id=r.customer_id ";

	if(Request.QueryString["tech_nav"] == "1")
		sc += "WHERE r.status = 1";
	else if(Request.QueryString["tech_nav"] == "2")
		sc += "WHERE r.status = 2";
	else if(Request.QueryString["tech_nav"] == "3")
		sc += "WHERE r.status = 3";
	else if(Request.QueryString["tech_nav"] == "4")
		sc += "WHERE r.status = 4";
	else if(Request.QueryString["tech_nav"] == "all" )
		sc += "";
	else if(Request.QueryString["tech_nav"] == "5")
		sc += " WHERE r.status = 5 ";		
	else if(Request.QueryString["tech_nav"] == "6")
		sc += " WHERE r.status = 6 ";		
	else if(Request.QueryString["tech_nav"] == "7")
		sc += " WHERE r.status = 7 ";		
	else
	{
		if(Request.QueryString["job"] != null)
			sc += " WHERE r.id = "+ Request.QueryString["job"].ToString() + " ";
		else
			sc += " WHERE r.status = 1 ";
	}
	if(Request.QueryString["tech_nav"] == "all")
		sc += " WHERE (r.repair_date BETWEEN DATEADD(month, -3, GETDATE()) AND GETDATE())";
	else
		sc += " AND (r.repair_date BETWEEN DATEADD(month, -3, GETDATE()) AND GETDATE())";
	
		
	sc += " ORDER BY repair_date ASC";	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"customerid");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	return true;
}

bool GetRepairForm(string sJobno)
{
	bool isNum = true;
	int ptr = 0;
	while (ptr < sJobno.Length)
	{
		if (!char.IsDigit(sJobno, ptr++))
		{
			isNum = false;
			break;
		}
	}
	string sc = "SELECT r.serial_number, r.invoice_number, CONVERT(varchar(12), r.repair_date, 13) AS repair_date, ";
	sc += " CONVERT(varchar(12), r.repair_finish_date,13) as repair_finish_date, c.name ";
	sc += ", c.company, c.trading_name, c.address1, c.address2, c.address3 ";
	sc += ", c.phone, c.email, r.customer_id, r.status, r.note, r.fault_desc, r.prod_desc, r.charge_detail, ";
	sc += " r.charge, r.technician, r.code, r.supplier_code, r.purchase_date, r.motherboard, r.ram, r.cpu, r.vga, r.os, r.other ";
	//sc += " r.charge, r.technician, r.code, r.supplier_code, r.purchase_date, r.motherboard, r.ram, r.cpu, r.vga, r.os, r.other ";
	sc += " FROM repair r JOIN card c ON c.id=r.customer_id ";
	if(isNum)
		sc += " WHERE r.id = '"+sJobno+"'";
	else
		sc += " WHERE c.name like '%"+sJobno+"' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst,"jobform");
	}
	catch(Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

void DisplayRepairJob(string sJobno)
{
	Response.Write("<br><center><h5>RA Repair DETAILS</h5>");
	Response.Write("<h5><u>Job Number:</u> "+sJobno+"</h5></center>");
	Response.Write("<script language=javascript>");
	Response.Write("<!---hide from old browser");
	string s = @"
		function printform(){
			window.print();
		}
		function typeNumber(number,precision) {
		  towrite = number + ' ';
		  if (precision > 0) {
			while (towrite.length < precision + 2) towrite = '0' + towrite;
			towrite = towrite.substring(0,towrite.length-precision-1) +
						'.' +
					towrite.substring(towrite.length-precision-1,towrite.length-1);
		  }
		  return towrite;
		}
		function calprice(){

			var price=0,price1=0,price2=0,price3=0, price4=0,price5=0; 
			var total=0; var subtotal=0; var gst=0;//document.frmRepairUpdate.txtTotal.value;
			//price=parseInt(document.frmRepairUpdate.txtPrice.value);
			price1=parseInt(document.frmRepairUpdate.txtPrice1.value);
			price2=parseInt(document.frmRepairUpdate.txtPrice2.value);
			price3=parseInt(document.frmRepairUpdate.txtPrice3.value);
			price4=parseInt(document.frmRepairUpdate.txtPrice4.value);
			price5=parseInt(document.frmRepairUpdate.txtPrice5.value);
		
			total=price5+price1+price2+price3+price4;
//			gst=total*0.125;
			double dGST = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;		//Modified by NEO
			gst = total * dGST;						//Modified by NEO
			subtotal=total+gst;
			
			document.frmRepairUpdate.txtGST.value=gst;
			//window.alert(price);
			document.frmRepairUpdate.txtTotal.value=subtotal; //typeNumber(total,0);
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
				 {
				 IsNumber = false;
				 }
		   }
		   return IsNumber;
   	    }
		function check(){
			if(document.frmRepairUpdate.note.value == '')
			{
				window.alert('Please comment on this product');
				document.frmRepairUpdate.note.focus();
				document.frmRepairUpdate.note.select();
				return false;
			}
			if(confirm('NO Warranty!! Are you SURE??'))
				return true;
			else
				return false;
		}
		function checkReceived(){
			if(confirm('Right Product!! Are you SURE??'))
				return true;
			else
				return false;
		}
	";
	Response.Write("--->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	if(dst.Tables["jobform"].Rows.Count <= 0 )
		return;
	if(Request.QueryString["rma_nav"] != null)
		nNext = int.Parse(Request.QueryString["rma_nav"].ToString());

	//Response.Write("<form name=frmRepairUpdate method=post action='techrma.aspx?jobno="+m_jobno+"&rma_nav="+nNext+"'>");
	Response.Write("<form name=frmRepairUpdate method=post action='techrma.aspx?job="+m_jobno+"&tech_nav="+nNext+"'>");
	Response.Write("<table align=center cellspacing=2 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	string s_sn = "", s_inv = "", s_jobno = "", s_status = "";
	string s_note = "", s_name = "", s_company = "", s_addr1 = "";
	string s_addr2 = "";	string s_city = "";	string s_phone = "";	string s_email = "";
	string s_fault = "";	string s_repair_date = "";	string s_fdate = "";
	string s_charge = "";	string s_charge_desc = "";	string s_prodesc = "";
	string code = "";	string supplier_code = "";	string purchase_date = "";
	string mb = "";	string ram = "";	string cpu = "";
	string vga = "";	string os = "";	string other = "", card_id ="";

	if(dst.Tables["jobform"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["jobform"].Rows[0];
		s_sn = dr["serial_number"].ToString();
		s_inv = dr["invoice_number"].ToString();
		s_jobno = dr["customer_id"].ToString();
		s_status = dr["status"].ToString();
		s_note = dr["note"].ToString();
		s_name = dr["name"].ToString();
		s_company = dr["trading_name"].ToString();
		s_addr1 = dr["address1"].ToString();
		s_addr2 = dr["address2"].ToString();
		s_city = dr["address3"].ToString();
		s_phone = dr["phone"].ToString();
		s_email = dr["email"].ToString();
		s_fault = dr["fault_desc"].ToString();
		s_repair_date = dr["repair_date"].ToString();
		s_fdate = dr["repair_finish_date"].ToString();
		s_charge = dr["charge"].ToString();
		s_charge_desc = dr["charge_detail"].ToString();
		s_prodesc = dr["prod_desc"].ToString();
		code = dr["code"].ToString();
		supplier_code = dr["supplier_code"].ToString();
		mb = dr["motherboard"].ToString();
		ram = dr["ram"].ToString();
		cpu = dr["cpu"].ToString();
		vga = dr["vga"].ToString();
		os = dr["os"].ToString();
		other = dr["other"].ToString();
		card_id = dr["customer_id"].ToString();
	}
	Response.Write("<td><input type=button onclick=\"javascript:viewcard_window=window.open('view_ra.aspx?");
	Response.Write("job="+ sJobno +"','','width=640,height=480')\" value='View Customer Detail & Description' " + Session["button_style"] + "></td>");	
	Response.Write("<th align=left>Repair Date:</b> "+DateTime.Parse(s_repair_date).ToString("dd/MM/yyyy")+"</th>");

	Response.Write("<tr bgcolor=#E3E3E3><th colspan=3 align=left>Repair Description:</th></tr>");
	Response.Write("<tr><td colspan=3><textarea name=txtfault rows=5% cols=70%>"+s_note+"</textarea></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3><th>Goods Received:</th><td colspan=2>"+s_prodesc+"</td></tr>");
	Response.Write("<tr><th  bgcolor=#E3E3E3>Repair Status:</th>");
	//Response.Write("<tr><td>"+s_status+"</td></tr>");
	Response.Write("<td><select name=status>");
	Response.Write(GetEnumOptions("rma_status", s_status));

	Response.Write("<tr bgcolor=#E3E3E3><th colspan=3 align=left>Repair Charges:</th></tr>");
	Response.Write("<tr><td colspan=2 align=center> Details:</td><td align=right>Price:</td></tr>");

	Response.Write("<tr><td colspan=2>1. <input type=text name=txtDetail1 size=70%></td><td align=right><input type=text size=10% value=0 name=txtPrice1 onchange='calprice();'></td></tr>");
	Response.Write("<tr><td colspan=2>2. <input type=text name=txtDetail2 size=70%></td><td align=right><input type=text size=10% value=0 name=txtPrice2 onchange='calprice();'></td></tr>");
	Response.Write("<tr><td colspan=2>3. <input type=text name=txtDetail3 size=70%></td><td align=right><input type=text size=10% value=0 name=txtPrice3 onchange='calprice();'></td></tr>");
	Response.Write("<tr><td colspan=2>4. <input type=text name=txtDetail4 size=70%></td><td align=right><input type=text size=10% value=0 name=txtPrice4 onchange='calprice();'></td></tr>");
	Response.Write("<tr><td colspan=2>5. <input type=text name=txtDetail5 size=70%></td><td align=right><input type=text size=10% value=0 name=txtPrice5 onchange='calprice();'></td></tr>");

	Response.Write("<tr><td align=right colspan=3><b>GST:</b> <input type=text name=txtGST onChange='calprice();' value=0></td></tr>");
	Response.Write("<tr><td align=right colspan=3><b>Total:</b> <input type=text value=0 name=txtTotal onChange='calprice();'></td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr bgcolor=#E3E3E3><th>Repair Updated Date:</th>");
	string sdate= DateTime.Now.ToString("dd/MM/yyyy");
	Response.Write("<td><input type=text name=txtdate value='"+sdate+"'></td><td>&nbsp;</td></tr>");

	Response.Write("<tr><td colspan=3><hr size=1 solid></td>");
//	Response.Write("<tr><td></td></tr>");

	Response.Write("<tr><td colspan=2><table border=0 bordercolor=black cellspacing=0 style=\"border-width:0px;border-style:Solid; ");
	Response.Write(" border-collapse:collapse;fixed\">");
	//Response.Write("<tr><td><b>Conditions of Repair:</b></td></tr>");
	Response.Write("</table> ");
	Response.Write("</td></tr>");
	
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	s_note = HttpUtility.UrlEncode(s_note);
	Response.Write("<tr><td>&nbsp;</td><td align=right>("+Session["name"].ToString()+")</td></tr>");
	Response.Write("<tr><td>&nbsp;</td><td align=right>(Technician Signature)</td><td align=right>(Customer Signature)</td></tr>");
	Response.Write("<tr><td colspan=3 align=right>");

	//Response.Write("<input type=button name=send ");
	//Response.Write(" OnClick=\"window.location=('rma.aspx?new=rma_new&job="+ sJobno +"&sn="+ s_sn +"&inv="+ s_inv +"&cardid="+ card_id +"')\" ");
	//Response.Write(" value='Send to Supplier' " + Session["button_style"] + ">\r\n");
	
	Response.Write("<input type=button name=replace value='Hardware Replacement' " + Session["button_style"] + " ");
	//Response.Write(" OnClick=\"window.location=('techrma.aspx?job="+ sJobno +"&note=" + HttpUtility.UrlEncode(s_note) +"&sn="+ s_sn +"&inv="+ s_inv +"&cardid="+ card_id +"')\" ");
	Response.Write(" OnClick=\"window.location=('techrma.aspx?job="+ sJobno +"&sn="+ s_sn +"&inv="+ s_inv +"&cardid="+ card_id +"')\" ");
	Response.Write("  >\r\n");

	//Response.Write("<tr><td colspan=3 align=right><input type=button name=print value='Printable Form' "+ Session["button_style"]+" OnClick=\"window.location=");
	Response.Write("<input type=button name=print value='Printable Form' "+ Session["button_style"]+" OnClick=\"window.location=");
	Response.Write("('techrma.aspx?jobno="+sJobno+"&print=y')\">");
	Response.Write("<input type=submit name=cmd value='Update Repair Job' "+Session["button_style"]+"></td></tr>");

	Response.Write("</table>");
	
	Response.Write("</form>");
	Response.Write("</body>");

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
//		else if(s[i] == '\n')
//			ss += s[i] + "<br>";
		else
			ss += s[i];
	}
	return ss;
}

//DW
string GetRMASN(string rma_id)
{
	if(dst.Tables["getrmasn"] != null)
		dst.Tables["getrmasn"].Clear();

	string sc = " SELECT serial_number FROM repair WHERE id = " + rma_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst,"getrmasn") == 1)
			return dst.Tables["getrmasn"].Rows[0]["serial_number"].ToString();
	}
	catch (Exception e)
	{
		ShowExp(sc, e);
	}
	return "";
}

bool updateRepairJob() 
{
	m_repair_desc = msgEncode(m_repair_desc);
	string re_desc = m_repair_desc;
	if(m_repair_desc.Length > 511)
	{
		for(int i=0; i<511; i++)
			m_repair_desc += re_desc[i].ToString();
	}
	string sc = "set DATEFORMAT dmy ";
	sc += " UPDATE repair SET note='"+m_repair_desc+"', ";
	sc += "status = '"+m_status+"', ";
	if(m_status == GetEnumID("rma_status", "Ready to Pick up"))
		sc += " repair_finish_date='" + m_fdate + "', ";
	sc += " charge="+m_charge+",";
	sc += "charge_detail='"+m_charge_detail+"', technician = '"+Session["name"].ToString()+"'";
	sc += " WHERE id = '"+m_jobno+"'";

	////////////////////////////////////////////////////////////////////////////////////////////////
	//serial trace DW
	string sn = GetRMASN(m_jobno);
	if(m_status == GetEnumID("rma_status", "Product Received"))
	{
		sc += " UPDATE stock SET status = " + GetEnumID("stock_status", "rma") + " WHERE sn = '" + sn + "' ";
		sc += AddSerialLogString(sn, "Received to Repair", "", "", m_jobno, "");
	}
	else if(m_status == GetEnumID("rma_status", "Pick up Already") )
		sc += AddSerialLogString(sn, "Repaired, return to customer", "", "", m_jobno, "");
	//end of serial trace DW
	/////////////////////////////////////////////////////////////////////////////////////////////////
	
	sc += AddRepairLogString(m_repair_desc, "", "", m_jobno, sn, "");
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

	if(m_status == GetEnumID("rma_status", "Authorized"))
	{
		MailMessage msgMail = new MailMessage();
		
		msgMail.To = m_customerEmail;
		msgMail.From = GetSiteSettings("service_email", "alert@eznz.com");
		msgMail.Subject = "RA# " + m_jobno + " Authorized";
	//	msgMail.BodyFormat = MailFormat.Html;
		msgMail.Body = "Dear " + m_customerName + ":\r\n\r\n";
		msgMail.Body += "Your RA Application has been authorized, please send faulty product to us\r\n";
		msgMail.Body += "You can view details or trace status on http://" + Request.ServerVariables["SERVER_NAME"] + GetRootPath() + "/rmastatus.aspx\r\n\r\n";
		msgMail.Body += "Regards.\r\n\r\n";
		msgMail.Body += m_sCompanyTitle + "\r\n";
		msgMail.Body += DateTime.Now.ToString("MMM.dd.yyyy");
		SmtpMail.Send(msgMail);
	}
	return true;
}	

bool deleteRepairJob(string repair_id)
{
	
	string sc = " UPDATE repair SET ";
	sc += " status = 7 ";
	sc += " WHERE id = '"+ repair_id +"'";
	if(Request.QueryString["tech_nav"] == "7")
	{
		sc += "DELETE FROM repair ";
		sc += "WHERE id = '"+ repair_id +"'";
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
	return true;

}

void BindGrid()
{
	Response.Write("<script language=javascript");
	Response.Write(">");
	Response.Write("<!-- ");
	const string s = @"
		function chkValid()
		{
			var s_char = document.frmList.txtSearch;
			if(s_char.value==''){
				window.alert('Please Enter Repair Job Number');
				s_char.focus();
				return false;
			}
			return true;
		}

		function validateform()
		{
			var s_char = document.frmList.txtSearch;
			if(s_char.value==''){
				window.alert('Please Enter Repair Job Number');
				s_char.focus();
				return false;
			}
			if(!IsNumberic(s_char.value)){
				window.alert('Please Enter Number Only!!');
				s_char.focus();
				s_char.select();
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
			
	Response.Write(" -->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
//DEBUG(" m jobno = ", m_jobno);
	Response.Write("<script language='JavaScript' src='cssjs/d_pick.js'>");
		//Script by Denis Gritcyuk: tspicker@yahoo.com
		//Submitted to JavaScript Kit (http://javascriptkit.com)
		//Visit http://javascriptkit.com for this script
	Response.Write("</script");
	Response.Write(">");
	
	//Response.Write("<form name=frmList method=post action='techrma.aspx'  onsubmit='return validateform()' >");
	Response.Write("<form name=frmList method=post action='techrma.aspx'>");

	Response.Write("<table width=100% cellspacing=0 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#3287C2;font-weight:bold;\">\r\n");
	Response.Write("<td><font size=2>Repairing Jobs Lists</font></td>");
	Response.Write("<td><input type=text name=txtSearch>");
	Response.Write("<input type=submit name=cmd value='Search Repair' onclick='return chkValid()'></td></tr>");
	
	Response.Write("<tr><td colspan=6 align=right><img src=r.gif><a title='Click to view all jobs' href=techrma.aspx?tech_nav=all><font color=blue>ALL</a> &nbsp;");
	Response.Write("<img src=r.gif><a title='Click to go to Waiting for Authorization' href=techrma.aspx?tech_nav=1><font color=blue>1.Waiting for Authorization</a> &nbsp;");
	Response.Write("<img src=r.gif><a title='Click to go to Authorized' href=techrma.aspx?tech_nav=2><font color=blue>2.Authorized</a> &nbsp;");
	Response.Write("<img src=r.gif><a title='Click to go to Product Received' href=techrma.aspx?tech_nav=3><font color=blue>3.Product Received</a> &nbsp;");
	Response.Write("<img src=r.gif><a title='Click to go to Repairing' href=techrma.aspx?tech_nav=4><font color=blue>4.Repairing</a> &nbsp; ");
	Response.Write("<img src=r.gif><a title='Click to go to Ready To Pickup' href=techrma.aspx?tech_nav=5><font color=blue>5.Ready To Pickup</a> &nbsp;");
	Response.Write("<img src=r.gif><a title='Click to go to Pick up Already' href=techrma.aspx?tech_nav=6><font color=blue>6.Already Pickup</font></a> &nbsp;"); //</td>");

	Response.Write("&nbsp;");
	Response.Write("<img src=r.gif><a title='Click to go to deleted jobs' href=techrma.aspx?tech_nav=7><font color=blue>7.Trash</a> &nbsp;</td>");
	Response.Write("</tr>");
	Response.Write("\r\n<script");
	Response.Write(">\r\ndocument.frmList.txtSearch.focus();\r\n</script");
	Response.Write(">\r\n");
	//Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><th bgcolor=#EEEEEE colspan=5><font size=+1>-"+ShowRAStatus()+"-</font></th></tr>");
	Response.Write("</table></form>");
	
}
string ShowRAStatus()
{
	string s_show = "";
	if(Request.QueryString["tech_nav"] == "1")
		s_show = "Waiting for Authorisation Records";
	else if(Request.QueryString["tech_nav"] == "2")
		s_show = "Authorised Records";
	else if(Request.QueryString["tech_nav"] == "3")
		s_show = "Product Received & Repairing Records";
	else if(Request.QueryString["tech_nav"] == "4")
		s_show = "Repairing Records";
	else if(Request.QueryString["tech_nav"] == "5")
		s_show = "Ready To Pickup Records";
	else if(Request.QueryString["tech_nav"] == "6")
		s_show = "Pickup Already Records";
	else if(Request.QueryString["tech_nav"] == "7")
		s_show = "Trash Records";
	else if(Request.QueryString["tech_nav"] == "all")
		s_show = "ALL Records";
	else
		s_show = "Waiting for Authorisation";

	return s_show;
}

void customerIDGrid()
{
	string tech_nav = ""; 
	if(Request.QueryString["tech_nav"] != null)
		tech_nav = Request.QueryString["tech_nav"].ToString();
	if(tech_nav != "")
		Response.Write("<form name=frm method=post action='techrma.aspx?tech_nav="+tech_nav+"'>");
	else
		Response.Write("<form name=frm method=post action='techrma.aspx'>");
	Response.Write("<table width=100% cellspacing=1 cellpadding=2 border=1 bordercolor=#E3E3E3 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#E3E3E3>");
	if(tech_nav == "1")
		Response.Write("<th>Authorize??</th>");	//
	else if(tech_nav == "2")
		Response.Write("<th>Got Products??</th>");
	else 
		Response.Write("<th>&nbsp;</th>");
			
	Response.Write("<th>Customer</th><th colspan=2>JOB#</th><th>INV#</th><th>SN#</th>");
	//Response.Write("<tr bgcolor=#E3E3E3><th>Authorize??</th><th>Customer</th><th>JOB#</th><th>SN#</th>");
	Response.Write("<th>Fault Desc</th><th>Purchase Date</th><th>Repire Date</th><th>Update Date</th><th>&nbsp;</th><th>&nbsp;</th></tr>");
	
	string status = "";
	/*bool bChange = true;
	int rows = 0;
	for(int i=0; i<dst.Tables["customerid"].Rows.Count; i++)
	{
		rows = dst.Tables["customerid"].Rows.Count;
		DataRow dr = dst.Tables["customerid"].Rows[i];
		
		string id = dr["Job No"].ToString();
		string name = dr["Customer"].ToString();
		string repair_date = dr["Repair Date"].ToString();
		string fault = dr["Fault Description"].ToString();
		string sn = dr["Serial Number"].ToString();
		string up_date = dr["Update Date"].ToString();
		status = dr["Status"].ToString();
		string inv = dr["invoice_number"].ToString();
		string p_date = dr["purchase_date"].ToString();
		string note = dr["note"].ToString();
		string card_id = dr["customer_id"].ToString();
		string for_supp_ra = dr["for_supp_ra"].ToString();
				//string rs_status = dr["ra_status"].ToString();
//DEBUG("rs_status = ", rs_status);
		int ra_status = 0;
		if(dr["ra_status"].ToString() != null && dr["ra_status"].ToString() != "")
			ra_status = int.Parse(dr["ra_status"].ToString());
		if(!bChange)
		{
			Response.Write("<tr bgcolor=#EEEEEE>");
			bChange = true;
		}
		else
		{
			Response.Write("<tr>");
			bChange = false;
		}
		if(status.ToUpper() == "WAITING FOR AUTHORIZATION")
		{
			Response.Write("<td><input type=radio name=check"+i.ToString()+" value=1>Yes");
			Response.Write("<input type=radio name=check"+i.ToString()+" value=0>No<input type=text name=comment"+i.ToString()+" value=''></td>");
		}
		else if(status.ToUpper() == "AUTHORIZED")
		{
			Response.Write("<td align=center><input type=radio name=check"+i.ToString()+" value=1>Yes");
			Response.Write("<input type=radio name=check"+i.ToString()+" value=0>No</td>");
			
		}
		else if(status.ToUpper() == "PRODUCT RECEIVED" || status.ToUpper() == "REPAIRING")
		{
			Response.Write("<td align=center><a title='Delete this job' href='techrma.aspx?"+Request.ServerVariables["QUERY_STRING"]+"&deljob="+id+"' onclick=\"return window.confirm('are you SURE want to DELETE this job#');\"><font color=red><b>Del</b></font></a>&nbsp;&nbsp;&nbsp;");
			Response.Write("<a title='Repair this job' href='techrma.aspx?jobno="+id+"&rma_nav="+ra_status+"'><font color=blue><b>Process!!</b></font></a></td>");
		}
		else
		{
			Response.Write("<td align=center><a title='Delete this job' href='techrma.aspx?"+Request.ServerVariables["QUERY_STRING"]+"&deljob="+id+"' onclick=\"return window.confirm('are you SURE want to PERMANENTLY Delete this job#');\"><font color=red><b>Del</b></font></a>&nbsp;&nbsp;&nbsp;");
			Response.Write("<a title='Repair this job' href='techrma.aspx?jobno="+id+"'><font color=blue><b>Process!!</b></font></a></td>");
		}
		Response.Write("<td>"+name+"</td>");
		Response.Write("<td>"+id+"</td>");
		Response.Write("<td><a title='view repair log' href='repair_log.aspx?job="+id+"' class=o target=target=_blank>Rp.Log</a></td>");
		Response.Write("<th><a title='view invoic number' href='invoice.aspx?"+ inv +"' class=o target=_blank>"+ inv +"</a></th>");
		Response.Write("<th><a title='check SN#' href='snsearch.aspx?sn="+ sn +"' class=o target=_blank>"+ sn +"</a></th>");
		Response.Write("<td>"+fault+"</td>");
		Response.Write("<td>"+p_date+"</td>");
		Response.Write("<td>"+repair_date+"</td>");
		Response.Write("<td>"+up_date+"</td>");
		Response.Write("<td><input type=button onclick=\"javascript:viewcard_window=window.open('view_ra.aspx?");
		Response.Write("job=" + id + "','','width=640,height=480');\" value='View Status' " + Session["button_style"] + "></td>");
		if(ra_status >2 && ra_status <7)
		{
			Response.Write("<td><input type=button name=cmd ");
			//Response.Write(" onclick=\"window.location=('techrma.aspx?job="+id+"&note=" + HttpUtility.UrlEncode("+note+")+"&sn="+sn+"&inv="+inv+"&cardid="+card_id+"')\"\n\r");
			Response.Write(" onclick=\"window.location=('techrma.aspx?job="+id+"&sn="+sn+"&inv="+inv+"&cardid="+card_id+"')\"\n\r");
			Response.Write(" value='Replace' " + Session["button_style"] + ">\r\n");
			Response.Write("</td>");
		}
		else
			Response.Write("<td>&nbsp;</td>");
		Response.Write("<input type=hidden name=hidid"+i.ToString()+" value="+id+">");
		
		Response.Write("</tr>");
	}
	Response.Write("<input type=hidden name=hidrow value="+rows+">");
	if(status.ToUpper() == "WAITING FOR AUTHORIZATION")
		Response.Write("<tr><td colspan=12><input type=submit name=cmd value='Authorize' "+Session["button_style"]+"></td></tr>");
	else if(status.ToUpper() == "AUTHORIZED")
		Response.Write("<tr><td colspan=12><input type=submit name=cmd value='Receive Product' "+Session["button_style"]+"></td></tr>");
	
	Response.Write("</form>");
	*/
	bool bAlt = true;

	//string s = "";
	DataRow dr;
	Boolean alterColor = true;
	int startPage = (m_page-1) * m_nPageSize;
//DEBUG("p=", startPage);
	for(int i=startPage; i<dst.Tables["customerid"].Rows.Count; i++)
	{
		if(i-startPage >= m_nPageSize)
			break;
		dr = dst.Tables["customerid"].Rows[i];
		alterColor = !alterColor;
		if(!DrawRow(dr, i, alterColor))
			break;
	}
	int rows = dst.Tables["customerid"].Rows.Count;
	Response.Write("<input type=hidden name=hidrow value="+rows+">");
	PrintPageIndex(status);
	Response.Write("</table>");
	Response.Write("</form>");
}

bool DrawRow(DataRow dr, int i, bool alterColor)
{
		
	string id = dr["Job No"].ToString();
	string name = dr["Customer"].ToString();
	string repair_date = dr["Repair Date"].ToString();
	string fault = dr["Fault Description"].ToString();
	string sn = dr["Serial Number"].ToString();
	string up_date = dr["Update Date"].ToString();
	string status = dr["Status"].ToString();
	string inv = dr["invoice_number"].ToString();
	string p_date = dr["purchase_date"].ToString();
	string note = dr["note"].ToString();
	string card_id = dr["customer_id"].ToString();
	string for_supp_ra = dr["for_supp_ra"].ToString();
			//string rs_status = dr["ra_status"].ToString();
//DEBUG("rs_status = ", rs_status);
	int ra_status = 0;
	if(dr["ra_status"].ToString() != null && dr["ra_status"].ToString() != "")
		ra_status = int.Parse(dr["ra_status"].ToString());
	if(!alterColor)
	{
		Response.Write("<tr bgcolor=#EEEEEE>");
		alterColor = true;
	}
	else
	{
		Response.Write("<tr>");
		alterColor = false;
	}
	if(status.ToUpper() == "WAITING FOR AUTHORIZATION")
	{
		Response.Write("<td><input type=radio name=check"+i.ToString()+" value=1>Yes");
		Response.Write("<input type=radio name=check"+i.ToString()+" value=0>No<input type=text name=comment"+i.ToString()+" value=''></td>");
	}
	else if(status.ToUpper() == "AUTHORIZED")
	{
		Response.Write("<td align=center><input type=radio name=check"+i.ToString()+" value=1>Yes");
		Response.Write("<input type=radio name=check"+i.ToString()+" value=0>No</td>");
		
	}
	else if(status.ToUpper() == "PRODUCT RECEIVED" || status.ToUpper() == "REPAIRING")
	{
		Response.Write("<td align=center><a title='Delete this job' href='techrma.aspx?"+Request.ServerVariables["QUERY_STRING"]+"&deljob="+id+"' onclick=\"return window.confirm('are you SURE want to DELETE this job#');\"><font color=red><b>Del</b></font></a>&nbsp;&nbsp;&nbsp;");
		Response.Write("<a title='Repair this job' href='techrma.aspx?jobno="+id+"&rma_nav="+ra_status+"'><font color=blue><b>Process!!</b></font></a></td>");
	}
	else
	{
		Response.Write("<td align=center><a title='Delete this job' href='techrma.aspx?"+Request.ServerVariables["QUERY_STRING"]+"&deljob="+id+"' onclick=\"return window.confirm('are you SURE want to PERMANENTLY Delete this job#');\"><font color=red><b>Del</b></font></a>&nbsp;&nbsp;&nbsp;");
		Response.Write("<a title='Repair this job' href='techrma.aspx?jobno="+id+"'><font color=blue><b>Process!!</b></font></a></td>");
	}
	Response.Write("<td>"+name+"</td>");
	Response.Write("<td>"+id+"</td>");
	Response.Write("<td><a title='view repair log' href='repair_log.aspx?job="+id+"' class=o target=target=_blank>Rp.Log</a></td>");
	Response.Write("<th><a title='view invoic number' href='invoice.aspx?"+ inv +"' class=o target=_blank>"+ inv +"</a></th>");
	Response.Write("<th><a title='check SN#' href='snsearch.aspx?sn="+ sn +"' class=o target=_blank>"+ sn +"</a></th>");
	Response.Write("<td>"+fault+"</td>");
	Response.Write("<td>"+p_date+"</td>");
	Response.Write("<td>"+repair_date+"</td>");
	Response.Write("<td>"+up_date+"</td>");
	Response.Write("<td><input type=button onclick=\"javascript:viewcard_window=window.open('view_ra.aspx?");
	Response.Write("job=" + id + "','','width=640,height=480');\" value='View Status' " + Session["button_style"] + "></td>");
	if(ra_status >2 && ra_status <7)
	{
		Response.Write("<td><input type=button name=cmd ");
		//Response.Write(" onclick=\"window.location=('techrma.aspx?job="+id+"&note=" + HttpUtility.UrlEncode("+note+")+"&sn="+sn+"&inv="+inv+"&cardid="+card_id+"')\"\n\r");
		Response.Write(" onclick=\"window.location=('techrma.aspx?job="+id+"&sn="+sn+"&inv="+inv+"&cardid="+card_id+"')\"\n\r");
		Response.Write(" value='Replace' " + Session["button_style"] + ">\r\n");
		Response.Write("</td>");
	}
	else
		Response.Write("<td>&nbsp;</td>");
	Response.Write("<input type=hidden name=hidid"+i.ToString()+" value="+id+">");
	
	Response.Write("</tr>");

	return true;
}

void PrintPageIndex(string status)
{
	string tech_nav = "";
	if(Request.QueryString["tech_nav"] != null && Request.QueryString["tech_nav"] != "")
		tech_nav = Request.QueryString["tech_nav"].ToString();
	Response.Write("<tr><td colspan=12>Page: ");
	int pages = dst.Tables["customerid"].Rows.Count / m_nPageSize + 1;
	for(int i=1; i<=pages; i++)
	{
		if(i != m_page)
		{
			Response.Write("<a href=techrma.aspx?tech_nav="+ tech_nav +"&p=");
			Response.Write(i.ToString());
			Response.Write(">");
			Response.Write(i.ToString());
			Response.Write("</a> ");
		}
		else
		{
			Response.Write("<font color=red><b>" + i.ToString() + "</b></font> ");
		}
	}
	Response.Write("</td></tr>");
	if(status.ToUpper() == "WAITING FOR AUTHORIZATION")
		Response.Write("<tr><td colspan=12><input type=submit name=cmd value='Authorize' "+Session["button_style"]+"></td></tr>");
	else if(status.ToUpper() == "AUTHORIZED")
		Response.Write("<tr><td colspan=12><input type=submit name=cmd value='Receive Product' "+Session["button_style"]+"></td></tr>");
}

void customerIDGrid1()
{
	DataView source = new DataView(dst.Tables["customerid"]);
	//String.Format("c", source.Tables["currentserial"].Rows["Cost"]);
	MyDataGrid.DataSource = source;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	customerIDGrid1();
}

</script>
<html>
<body>
<form>

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#E3E3E3
	CellPadding=2
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=6pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=25
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=left
    OnPageIndexChanged=MyDataGrid_Page
	>
	<HeaderStyle BackColor=#E3E3E3 ForeColor=black Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
	
	<Columns name=job >
		<asp:HyperLinkColumn
			 HeaderText=""
			 DataNavigateUrlField='Job NO'
			 DataNavigateUrlFormatString="techrma.aspx?deljob={0}"
			 Text=DEL
			 />
	</Columns>
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=""
			 DataNavigateUrlField='Job NO'
			 DataNavigateUrlFormatString="techrma.aspx?jobno={0}"
			 Text=PROCESS!!
			 />
	</Columns>
	
</asp:DataGrid>

</form>
<asp:Label id=LFooter runat=server/>
</body>
</html>
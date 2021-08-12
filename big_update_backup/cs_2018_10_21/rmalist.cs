<!-- #include file="page_index.cs" -->

<script runat="server">

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

int m_nPageSize = 15;
int m_page = 1;
int m_nColspan = 7;

string m_rmano = "";
string m_search = "";
string m_sort = "";
string m_command = "";
string mbgcol = "";

bool m_bIsOneCheck = false;
bool m_bDesc = false;

void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;
	
	if(Request.QueryString["ra"] != "" && Request.QueryString["ra"] != null)
		m_rmano = Request.QueryString["ra"];
	if(Request.Form["cmd"] == "Search" || (Request.Form["search"] != null && Request.Form["search"] != ""))
		m_search = Request.Form["search"];
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		m_command = Request.Form["cmd"].ToString();

	InitializeData();
	mbgcol = GetSiteSettings("table_row_bgcolor", "#EEEEEE");
	if(Request.QueryString["ra"] != "" && Request.QueryString["ra"] != null)
	{
		if(m_command == "Transfer Back to Stock")
		{
			if(!doTransfer())
				return;
		}
		if(m_command == "Send to Supplier")
		{
			if(!doInsertSupplier())
				return;
		}
		if(!TransferBackStock())
			return;
		
		return;
	}
	
	//clear all sn# sessions
	if(Session["sns"] != null)
	{
		int n = (int)Session["sns"];
		for(int ii=0; ii<n; ii++)
			Session["sn"+ ii] = null;
	}
	//--------

	BindFaultyItems();
}

bool doInsertSupplier()
{
	int nRMA = GetNextRMANO();
	
	int nRows = int.Parse(Request.Form["rows"].ToString());
	for(int i=0; i<nRows; i++)
	{
		if(Request.Form["check"+ i] == "on")
		{
			m_bIsOneCheck = true;
			string sn = Request.Form["sn"+ i].ToString();
			string repair_id = Request.Form["id"+ i].ToString();
			string code = Request.Form["code"+ i].ToString();
			string supplier_code = msgEncode(Request.Form["supp_code"+ i].ToString());
			string name = msgEncode(Request.Form["name"+ i].ToString());
			string fault = msgEncode(Request.Form["fault"+ i].ToString());
			//if(Request.Form["invoice"+ i] != null && Request.Form["invoice"+ i] != "")
			string supplier_id = "";
			if(Request.Form["supplier_id"+ i] != null)
				supplier_id = Request.Form["supplier_id"+ i];
			//DEBUG("repair_id = ", repair_id);
			if(supplier_id != null && code != "")
			{
		//		DEBUG("supplier_id =", supplier_id);
				string sc = "SET DATEFORMAT dmy ";
				sc += " INSERT INTO rma (serial_number, technician, ra_id, fault_desc, repair_date ";
				sc += ", p_code, product_desc, supplier_code, supplier_id, stock_check ";
				sc += ", check_status, purchase_date ";
				sc += " )";
				sc += " VALUES( '"+ sn +"', '"+ Session["card_id"] +"', "+ nRMA +", '"+ fault +"' ";
				sc += " , GETDATE(), '"+ code +"',  '"+ name +"', '"+ supplier_code +"', "+ supplier_id +", 1";
				sc += ", 1, '1/01/1900' )";
				
				//update repair
				sc += " UPDATE repair SET code = '"+ code +"', supplier_code = '"+ supplier_code +"'";
				sc += ", prod_desc = '" + name +"' ";
				sc += ", fault_desc = '"+ fault +"' ";
				sc += ", supplier_id = "+ supplier_id +"";
				sc += ", status = 6 , for_supp_ra = 1";
				sc += " WHERE id = "+ repair_id +" ";
				
				sc += AddSerialLogString(sn, "insert for supplier rma", "", "", repair_id, "");
				sc += AddRepairLogString("insert for supplier rma ", "", code, repair_id, sn, "");
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
	
	if(m_bIsOneCheck)
	{
		Response.Write("<script language=javascript");
		Response.Write("> window.location=('supp_rma.aspx?rma=rd&spp=&st=1&rid="+ nRMA +"')\r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		return false;
	}
	return true;
}



bool doTransfer()
{
	int nRows = int.Parse(Request.Form["rows"].ToString());
	string sCode = "";
	for(int i=0; i<nRows; i++)
	{
		if(Request.Form["check"+ i] == "on")
		{
			m_bIsOneCheck = true;
			string sn = Request.Form["sn"+ i].ToString();
			string repair_id = Request.Form["id"+ i].ToString();
			string code = Request.Form["code"+ i].ToString();
			string supplier_code = msgEncode(Request.Form["supp_code"+ i].ToString());
			string name = msgEncode(Request.Form["name"+ i].ToString());
			string invoice_number = "";
			sCode = code;
			if(Request.Form["invoice"+ i] != null && Request.Form["invoice"+ i] != "")
				invoice_number = Request.Form["invoice"+ i].ToString();
			if(code != "" && supplier_code != "")
			{	
			//	DEBUG("repair_id = ", repair_id);
			/*	string sc = "SET DATEFORMAT dmy ";
				sc += " INSERT INTO return_sn (old_sn, staff, replaced_date, condition, repair_id ";
				sc += ", code, supplier_code, action_desc )";
				sc += " VALUES( '"+ sn +"', '"+ Session["card_id"] +"', GETDATE(), 2, "+ repair_id;
				sc += " , '"+ code +"', '"+ supplier_code +"', 'Transfered Back to Stock')";

				//update repair
				sc += " UPDATE repair SET code = '"+ code +"', supplier_code = '"+ supplier_code +"'";
				sc += ", prod_desc = '" + name +"' ";
				sc += " WHERE id = "+ repair_id +" ";

				//update stock qty
				if(g_bRetailVersion)
				{
					sc += " UPDATE stock_qty SET qty = qty + 1 ";
					sc += " WHERE code = "+ code +" ";
				}
				else
				{
					sc += " UPDATE product SET stock = stock + 1 ";
					sc += " WHERE code = "+ code +" AND supplier_code = "+ supplier_code +"";
				}
				sc += AddSerialLogString(sn, "transfer item back to stock", "", "", repair_id, "");
				sc += AddRepairLogString("transfer item back to stock", "", code, repair_id, sn, "");
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
			*/	
			}
		}

	}
//DEBUG(" m_bIsOneCheck = ", m_bIsOneCheck.ToString());
/*	if(m_bIsOneCheck)
	{
		Response.Write("<script language=javascript");
		//Response.Write("> window.location=('stocktrace.aspx?p=0&c="+ sCode +"')\r\n");
		Response.Write("> window.location=('rma_rp.aspx')\r\n");
		Response.Write("</script\r\n");
		Response.Write(">\r\n");
		return false;
	}
	*/
	
	return true;
}

bool GetFaultyItems()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT * ";
	sc += ", (SELECT COUNT(*) FROM repair WHERE replaced = 1) AS total ";
	sc += " FROM repair ";
	sc += " WHERE ";
	sc += " replaced = 1 ";
	sc += " AND status < 7 ";
	//if(Request.QueryString["ss"] == "1")
		sc += " AND (for_supp_ra = 0 OR for_supp_ra IS NULL) ";
	if(m_search != "")
	{
		sc += " AND ";
		if(TSIsDigit(m_search))
		{
			sc += " serial_number = '"+ m_search +"'"; // OR code = "+ m_search;
			sc += " OR ra_number = '"+ m_search +"' ";
		}
		else
		{
			sc += " serial_number = '"+ m_search +"' OR prod_desc LIKE '%"+ m_search +"%' ";
		}
	}
	if(m_rmano != "")
		sc += " AND ra_number = '"+ m_rmano +"'";
	sc += " AND id NOT IN (SELECT repair_id FROM return_sn WHERE repair_id IS NOT NULL ";
	sc += " ) ";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "faults");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	return true;
}

void BindFaultyItems()
{
	if(!GetFaultyItems())
		return;

	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	int rows = 0;
	rows = dst.Tables["faults"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 25;
	m_cPI.URI = "?";
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();
	int nTotal = 0;
	if(rows > 0)
		nTotal = int.Parse(dst.Tables["faults"].Rows[0]["total"].ToString());
	
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center width=96% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan="+ m_nColspan +">");
	
	Response.Write("<table align=center width=100% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor="+ mbgcol +"");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");
	Response.Write("SEARCH: <input type=text name=search value='"+ m_search +"'>");
	Response.Write("<input type=submit name=cmd value='Search' "+ Session["button_style"] +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>"+ sPageIndex +"</td>");
	/*Response.Write("<td align=right><select name=slt_type >");
	Response.Write("<option value='all'>All");
	Response.Write("<option value='tr'>Transfered");
	Response.Write("<option value='rp'>Faulty");
	Response.Write("</select>");
	Response.Write("</td>");
	*/
	Response.Write("</tr>");
	Response.Write("<tr><td><font size=1><b>Total Items: "+ nTotal +"</b></font></td>");
	//Response.Write("<td><font size=1><b>Total Transfered Items: "+ nTotal +"</b></font></td></tr>");
	Response.Write("</table></td></tr>");
	Response.Write("<tr bgcolor=#AAEEDD><th align=left>RA#</td><th align=left>SN#</td><th align=left>CODE</td><th align=left>SUP_CODE</td><th>DESC</td><th>NOTE</td><th></td></tr>");
	DataRow dr;
	bool bAlt = true;
	string uri = ""+ Request.ServerVariables["URL"]+"";
	for(; i < rows && i < end; i++)
	{
		dr = dst.Tables["faults"].Rows[i];
		//string id = dr["id"].ToString();
		string rma_no = dr["ra_number"].ToString();
		string sn = dr["serial_number"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string prod_desc = dr["prod_desc"].ToString();
		string supplier_id = dr["supplier_id"].ToString();
		string note = dr["note"].ToString();
		string fault_desc = dr["fault_desc"].ToString();
		
		Response.Write("<tr ");
		if(!bAlt)
			Response.Write(" bgcolor=#EEEEEE");
		bAlt = !bAlt;
		Response.Write(">");
		Response.Write("<td>");
		Response.Write(rma_no);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(sn);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(code);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(supplier_code);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(prod_desc);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(note);
		Response.Write("</td>");
		Response.Write("<td><a title='Process items' href='"+ uri +"?ss=2&ra="+ rma_no +"' class=o>TF<a>");
		Response.Write(" <a title='Send To Supplier' href='"+ uri +"?ss=1&ra="+ rma_no +"' class=o>SS<a>");
		//Response.Write(" <a title='Transfer Back to Stock' href='' class=o>TF<a>");
		Response.Write("</td>");
	
	}

	Response.Write("</table>");
	Response.Write("</form>");

}

bool TransferBackStock()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.frm.elements.length;i++) 
		{
			var e = document.frm.elements[i];
			if((e.name != 'allbox') && (e.type=='checkbox'))
				e.checked = document.frm.allbox.checked;
		}
	}
	";
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");
	if(!GetFaultyItems())
		return false;
	
	string[] sTitle = new string[3];
	sTitle[1] = "Send Faulty Items To Supplier";
	sTitle[2] = "Transfer Replaced Items Back to Stock";
	int nss = 1;
	if(Request.QueryString["ss"] != "" && Request.QueryString["ss"] != null)
		nss = int.Parse(Request.QueryString["ss"].ToString());
	
	if(nss == 1)
		m_nColspan= 8;
	else
		m_nColspan= 7;
	//Response.Write("<center><h4><b> FAULTY ITEMS </b></h4></center>");
	Response.Write("<center><h4><b> "+ sTitle[nss] +" </b></h4></center>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center width=96% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan="+ m_nColspan +">");
	Response.Write("<table align=center width=100% cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor="+ mbgcol +"");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td>");
	
	int nRows = dst.Tables["faults"].Rows.Count;
	Response.Write("<tr><td><font size=1><b>Total Items: "+ nRows +"</b></font></td></tr>");
	Response.Write("</table></td></tr>");
	Response.Write("<tr bgcolor=#AAEEDD><th align=left>RA#</td><th align=left>CODE</td><th align=left>SUP_CODE</td><th>PRO_DESC</td><th align=left>SN#</td>");
	if(nss == 1)
	{
		Response.Write("<th>FAULT_DESC</textarea></td>");
		Response.Write("<th>SUPPLIER</td>");
	}
	Response.Write("<td></td></tr>");
	DataRow dr;
	bool bAlt = true;
	string uri = ""+ Request.ServerVariables["URL"]+"";
	
	Response.Write("<input type=hidden name=rows value='"+ nRows +"'>");
	Session["sns"] = dst.Tables["faults"].Rows.Count;
	
	for(int i=0;  i < dst.Tables["faults"].Rows.Count; i++)
	{
		dr = dst.Tables["faults"].Rows[i];
		string id = dr["id"].ToString();
		string rma_no = dr["ra_number"].ToString();
		string sn = dr["serial_number"].ToString();
		string code = dr["code"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string prod_desc = dr["prod_desc"].ToString();
		string supplier_id = dr["supplier_id"].ToString();
		string note = dr["note"].ToString();
		string fault_desc = dr["fault_desc"].ToString();
		string invoice = dr["invoice_number"].ToString();
		
		string slt_code = "";
		if(Session["slt_code"+ id] != null && Session["slt_code"+ id] != "")
			slt_code = Session["slt_code"+ id].ToString();
	//DEBUG("code = ", slt_code );
		
		if(Request.Form["sn"+ i] != null && Request.Form["sn"+ i] != "")
		{
			Session["sn"+ i] = Request.Form["sn"+ i];
		}
		if(Session["sn"+ i] != null)
		sn = Session["sn"+ i].ToString();
		dr = GetProductDetails(slt_code);
		if(dr != null)
		{
			code = dr["code"].ToString();
			supplier_code = dr["supplier_code"].ToString();
			prod_desc = dr["name"].ToString();
		}

		Response.Write("<tr ");
		if(!bAlt)
			Response.Write(" bgcolor=#EEEEEE");
		bAlt = !bAlt;
		Response.Write(">");
		Response.Write("<input type=hidden name='id"+ i +"' value='"+ id +"'>");
		Response.Write("<input type=hidden name='invoice"+ i +"' value='"+ invoice +"'>");
		Response.Write("<td>");
		Response.Write(rma_no);
		Response.Write("</td>");
			Response.Write("<td><input type=text size=6% name='code"+ i +"' value=");
		Response.Write(code);
		Response.Write("><a title='Get Product Code, Supplier Code, Description' href='slt_item.aspx?uri="+ uri +"?ra="+ rma_no +"&id="+ id +"&ss="+ nss +"'>...</a>");
		Response.Write("</td>");
		Response.Write("<td><input type=text size=10% name='supp_code"+ i +"' value=");
		Response.Write(supplier_code);
		Response.Write("></td>");
		Response.Write("<td><input type=text size=");
		if(nss == 1)
			Response.Write(" 30% ");
		else
			Response.Write(" 55% ");
		Response.Write(" name='name"+ i +"' value='");
		Response.Write(prod_desc);
		Response.Write("'></td>");
		Response.Write("<td><input type=text size=15% name='sn"+ i +"' value='");
		Response.Write(sn);
		Response.Write("'></td>");
		if(nss == 1)
		{
			Response.Write("<td><textarea name='fault"+ i +"' value=" + fault_desc +">"+ fault_desc +"</textarea></td>");
			Response.Write("<td width=10%>");
			GetSupplier(supplier_id, i);
			Response.Write("</td>");
		}
		Response.Write("<td><input type=checkbox name='check"+ i +"'> ");
		
		Response.Write("</td>");
	
	}
	Response.Write("<tr><td colspan=" + m_nColspan + " align=right><b>Select All </b>");
	Response.Write("<input type=checkbox name=allbox value='Select All' onClick='CheckAll();'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=4>#NB: Please Fill ALL Fields Before Transfer Back/Send to Supplier</td>");
	Response.Write("<th colspan="+ (m_nColspan-3).ToString() +" align=right>");
	Response.Write("<input type=button name=cmd value='Cancel/Back'"+ Session["button_style"] +" ");
	Response.Write(" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"');\" ");
	Response.Write(">");
	
	if(nss == 2)
		Response.Write("<input type=submit name=cmd value='Transfer Back to Stock'"+ Session["button_style"] +">");
	if(nss == 1)
		Response.Write("<input type=submit name=cmd value='Send to Supplier'"+ Session["button_style"] +">");
	if(m_bIsOneCheck)
	{
		Response.Write("<input type=button value='Report'"+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('rma_rp.aspx?d="+ DateTime.Now.ToString("dd-MM-yyyy")+"')\" ");
		Response.Write(">");
	}
	Response.Write("</td></tr>");
	Response.Write("</table>");
	
	Response.Write("</form>");
	
	return true;
}

void GetSupplier(string supplier_id, int nRow)
{
	int rows = 0;
	string sc = "SELECT id, trading_name, name, short_name FROM card WHERE type=3 ORDER BY trading_name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "supplier");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
	Response.Write("<select name='supplier_id"+ nRow +"'> ");
	
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["supplier"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["trading_name"].ToString();
		if(name == "")
			name = dr["name"].ToString();
		
		Response.Write("<option size=10% value=" + id +"");
		
		if(supplier_id == id)
			Response.Write(" selected ");
		Response.Write(">" + name +"</option>");
	}
	Response.Write("</select>");
	
}


DataRow GetProductDetails(string code)
{
	DataSet dscsn = new DataSet();

	string sc = " SELECT code, supplier_code, name ";
	sc += " FROM code_relations ";
	sc += " WHERE code='" + code + "'";
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


int GetNextRMANO()
{
	int rNumber = 1000;
	
	if(dst.Tables["insertrma"] != null)
		dst.Tables["insertrma"].Clear();

//DEBUG("rnumber = ", rNumber);
	string sc = "SELECT TOP 1 ra_id FROM rma ORDER BY ra_id DESC";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "insertrma") == 1)
		{
			if(dst.Tables["insertrma"].Rows[0]["ra_id"].ToString() != null && dst.Tables["insertrma"].Rows[0]["ra_id"].ToString() != "")
				rNumber = int.Parse(dst.Tables["insertrma"].Rows[0]["ra_id"].ToString()) + 1;
			else
				rNumber = rNumber + 1;
			return rNumber;
		}
		//Session["rma_id"] = rNumber;
		Session["rma_id"] = rNumber;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		//return false;
		return 1;
	}

	return rNumber;
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



</script>

<asp:Label id=LFooter runat=server/>



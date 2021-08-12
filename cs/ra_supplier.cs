<!-- #include file="page_index.cs" -->
<!-- #include file="isdate.cs" -->
<script runat=server>

string m_search = "";
string m_cust_query = "";
string m_supp_query = "";
string m_sdFrom = "";
string m_sdTo = "";
string m_rma_id = "";
string m_supplier_id = "";
string m_customer_id = "";
string m_command_type = "";
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
	
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		m_command_type = Request.Form["cmd"].ToString();

	GetQueryString();
	
	if(!SecurityCheck("technician"))
		return;
	InitializeData();
	
	
	if(!DoQueryRMA())
		return;
	if(m_command_type == "SEND to SUPPLIER")
	{
		if(DoInsertToSupplierRMA())
		{

		Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?id="+ m_rma_id +"");
		if(Request.QueryString["sid"] != "")
			Response.Write("&sid="+ Request.QueryString["sid"] +"");
		if(Request.QueryString["mid"] != "")
			Response.Write("&mid="+ Request.QueryString["mid"] +"");
		Response.Write("')");
		Response.Write("</script");
		Response.Write(">");

		return;
		}
	}

}

void GetQueryString()
{
	if(Request.QueryString["id"] != "" && Request.QueryString["id"] != null)
		m_rma_id = Request.QueryString["id"].ToString();
	if(Request.QueryString["sid"] != "" && Request.QueryString["sid"] != null)
		m_supplier_id = Request.QueryString["sid"];
	if(Request.QueryString["mid"] != "" && Request.QueryString["mid"] != null)
		m_customer_id = Request.QueryString["mid"];
}

bool DoCheckSN(string sn)
{	
	bool bfound = false;
	if(sn == "")
		return bfound;
	
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT p.id, ISNULL(p.date_invoiced, s.update_time) AS date_invoiced, p.inv_number, p.supplier_id, pi.code, pi.supplier_code, pi.name ";
	sc += " FROM stock s JOIN purchase p ON p.id = s.purchase_order_id ";
	sc += " JOIN purchase_item pi ON pi.id = p.id ";
	sc += " WHERE s.sn = '"+ sn +"' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "found") > 0)
		{	
			Session["purchase_date"] = dst.Tables["found"].Rows[0]["date_invoiced"].ToString();
			Session["purchase_invoice"] = dst.Tables["found"].Rows[0]["inv_number"].ToString();
			Session["purchase_id"] = dst.Tables["found"].Rows[0]["id"].ToString();
			Session["supplier_id"] = dst.Tables["found"].Rows[0]["supplier_id"].ToString();
			Session["slt_code"] = dst.Tables["found"].Rows[0]["code"].ToString();
			Session["slt_supplier_code"] = dst.Tables["found"].Rows[0]["supplier_code"].ToString();
			Session["slt_name"] = dst.Tables["found"].Rows[0]["name"].ToString();

			bfound = true;
			return bfound;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return bfound;
}

bool DoInsertToSupplierRMA()
{
	string id = Request.Form["id"];
	string sn = Request.Form["sn"];
	string code = Request.Form["code"];
	string supplier_code = Request.Form["supplier_code"];
	string desc = Request.Form["desc"];
	string fault = EncodeQuote(Request.Form["fault"].ToString());
	string customer_id = Request.Form["customer_id"];
	string supplier_id = Request.Form["supplier"];
	string ra_number = Request.Form["ra_number"];
	string status = Request.Form["status"];
//DEBUG("id = ",id);
//return false;
	int n_supplier_ra = GetNextSupplierRA_ID();
	bool bfound = DoCheckSN(sn);

	string sc = " ";
	string snote = "Product has sent to supplier On "+ DateTime.Now.ToString() +"\r\n";
	
	if(Session["purchase_date"] != null && Session["purchase_date"] != "")
	{
		if(IsDate(Session["purchase_date"].ToString()))
			Session["purchase_date"] = DateTime.Parse(Session["purchase_date"].ToString()).ToString("dd-MM-yyyy");
		else
			Session["purchase_date"] = DateTime.Now.ToString("dd-MM-yyyy");
	}
	//delete rma stock first until stock return from supplier
	sc += " DELETE FROM rma_stock WHERE repair_id = "+ id +" ";
	
	sc += " SET DATEFORMAT dmy INSERT INTO rma (status";
	if(bfound)
		sc += " , invoice_number , po_id";
	
	sc += ", purchase_date,  ra_id, customer_id, repair_jobno, supplier_id, p_code, supplier_code ";
	sc += " , fault_desc, product_desc, serial_number, technician, repair_date, stock_check, check_status ";
	sc += " ) ";
	sc += " VALUES(4 ";
	if(bfound)
		sc += ", '"+ Session["purchase_invoice"] +"', "+ Session["purchase_id"] +", '"+ Session["purchase_date"] +"' ";
	else
		sc += " , GETDATE() ";
	sc += ",  "+ n_supplier_ra +", "+ customer_id +", "+ id +", "+ supplier_id +" ";
	sc += ", "+ code +", '"+ supplier_code +"', CONVERT(VARCHAR(512),'"+ fault +"'), '"+ desc +"' ";
	sc += ", '"+ sn +"', "+ Session["card_id"] +", GETDATE(), "+ status +", 1 ";
	sc += " ) ";
	
	sc += " SET DATEFORMAT dmy UPDATE repair SET code = "+ code +", supplier_code = '"+ supplier_code +"' ";
	sc += " , prod_desc = '"+ desc +"', for_supp_ra = 1 ";
	sc += " , note = '"+ snote +"' + note ";
	sc += " WHERE id = "+ id;

	sc += AddRepairLogString("Item Sent to Supplier:"+ desc +"", "", ""+ code +"",  ""+ id +"", ""+ sn +"", "");
//DEBUG("sc +", sc);
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
	
	Session["supplier_id"] = null;
	Session["purchase_date"] = null;
	Session["purchase_invoice"] = null;
	Session["purchase_id"] = null;
	Session["slt_code"] = null;
	Session["slt_supplier_code"] = null;
	Session["slt_name"] = null;

	return true;
}

bool DoQueryRMA()
{
	if(!TSIsDigit(m_rma_id))
	{
		Response.Write("<script language=javascript>window.alert('INVALID RMA ID, Please Try Again!!'); window.history.go(-1);</script");
		Response.Write(">");
		return false;
	}
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT * FROM repair ";
	sc += " WHERE id = "+ m_rma_id;
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "repair");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	int ncols = 2;
	string scolor = "#FFFFF";
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
		
		Response.Write("<form name=frm method=post>");

		Response.Write("<center><h4>SEND ITEM to SUPPLIER</center></h4>");
		Response.Write("<table align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
			//	Response.Write("<tr><td colspan="+ ncols +">"+ sPageIndex +"</th></tr>");
		bool bAlter = false;
		//for(; i<rows && i<end; i++)
		//{
			DataRow dr = dst.Tables["repair"].Rows[0];
			string id = dr["id"].ToString();
			string ra = dr["ra_number"].ToString();
			string sn = dr["serial_number"].ToString();
			string code = dr["code"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			string desc = dr["prod_desc"].ToString();
			string supplier_id = dr["supplier_id"].ToString();
			string fault = dr["fault_desc"].ToString();
			string customer_id = dr["customer_id"].ToString();
			//bool bsend_to_supplier = bool.Parse(dr["for_supp_ra"].ToString());
			string send_to_supplier = dr["for_supp_ra"].ToString();
			
			if(Session["slt_code"] != null && Session["slt_code"] != "")
				code = Session["slt_code"].ToString();
			if(Session["slt_supplier_code"] != null && Session["slt_supplier_code"] != "")
				supplier_code = Session["slt_supplier_code"].ToString();
			if(Session["slt_name"] != null && Session["slt_name"] != "")
				desc = Session["slt_name"].ToString();
			
			if(m_supplier_id != "" && m_supplier_id != null) 
				supplier_id = m_supplier_id;
			if(m_customer_id != "" && m_customer_id != null) 
				customer_id = m_customer_id;
			
			if(DoCheckSN(sn))
			{
				code = Session["slt_code"].ToString();
				supplier_code = Session["slt_supplier_code"].ToString();	
				desc = Session["slt_name"].ToString();	
				supplier_id = Session["supplier_id"].ToString();	
				
			}
		
			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor="+ scolor +" ");
			bAlter = !bAlter;
			Response.Write(">");
			Response.Write("<input type=hidden name=id value="+ id +">");
			Response.Write("<td  bgcolor="+ scolor +" >CUSTOMER RMA#</td><td>");
			Response.Write("<input type=text readonly name=ra_number value=");
			Response.Write(ra);
			Response.Write(">");
			Response.Write("</td>");
			Response.Write("</tr><tr>");
			Response.Write("<td bgcolor="+ scolor +" >SN#</td><td>");
			Response.Write("<input type=text name=sn value=");
			Response.Write(sn);
			Response.Write(">");
			Response.Write("<input type=checkbox name=ignor ><b>Ignor Input SN#");
			Response.Write("</td>");
			Response.Write("</tr><tr>");
			Response.Write("<td bgcolor="+ scolor +" >PRODUCT CODE</td><td>");
			Response.Write("<input type=text name=code value=");
			Response.Write(code);
			Response.Write("> ");
			string uri = Request.ServerVariables["URL"] + "?" +Request.ServerVariables["QUERY_STRING"];
			Response.Write(" <a href='slt_item.aspx?uri="+ uri +"' class=o>...</a>");
			Response.Write("</td>");
			Response.Write("</tr><tr>");
			Response.Write("<td bgcolor="+ scolor +" >SUPPLIER CODE</td><td>");
			Response.Write("<input type=text name=supplier_code value=");
			Response.Write(supplier_code);
			Response.Write(">");
			Response.Write("</td>");
			Response.Write("</tr><tr>");
			Response.Write("<td bgcolor="+ scolor +" >PRODUCT DESCRITPION</td><td>");
			Response.Write("<input type=text size=50% name=desc value='");
			Response.Write(desc);
			Response.Write("'>");
			Response.Write("</td>");
			Response.Write("</tr><tr>");
			Response.Write("<td bgcolor="+ scolor +" >SUPPLIER</td><td>");
			Response.Write(PrintSupplierOptions(supplier_id, Request.ServerVariables["URL"] +"?id="+Request.QueryString["id"]+"&mid="+ Request.QueryString["mid"] +"&sid="));
			Response.Write(" &nbsp;<a title='view supplier details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + customer_id + "','', ' width=350,height=350'); viewcard_window.focus()\" class=o >V</a>");
			Response.Write("</td>");
			Response.Write("</tr><tr>");
			Response.Write("<td bgcolor="+ scolor +" >CUSTOMER</td><td>");
			//Response.Write(PrintCustomerOptions(customer_id, Request.ServerVariables["URL"] +"?id="+Request.QueryString["id"]+"&sid="+ Request.QueryString["sid"] +"&mid="));
			Response.Write("<input type=text name=customer_id readonly value=");
			Response.Write(customer_id);
			Response.Write(">");
			Response.Write(" &nbsp;<a title='view customer details' href=\"javascript:viewcard_window=window.open('viewcard.aspx?");
			Response.Write("id=" + customer_id + "','', ' width=350,height=350'); viewcard_window.focus()\" class=o >V</a>");
			Response.Write("</td>");
			Response.Write("</tr>");
			Response.Write("<tr><td bgcolor="+ scolor +">ITEM FOR</td><td><select name=status><option value='2'>For Customer</option>");
			Response.Write("<option value=1>For Stock</option></td></tr>");
			Response.Write("<tr>");
			Response.Write("<td bgcolor="+ scolor +" >FAULT DESCRIPTION</td><td>");
			//Response.Write("<input type=text name=fault value=");
			Response.Write("<textarea rows=7 cols=50 name=fault value="+ fault +">");
			Response.Write(fault);
			Response.Write("</textarea>");
			Response.Write("</td>");
			Response.Write("</tr>");
		//}
			if(send_to_supplier == null && send_to_supplier == "")
				send_to_supplier = "0";
//DEBUG("send to supplier = ", send_to_supplier);

			//if(!bool.Parse(send_to_supplier))
			Response.Write("<tr align=right><td colspan="+ ncols +">");
		Response.Write("<input type=button value='<< Back to Repair' "+ Session["button_style"] +" ");
		Response.Write(" onclick=\"window.location=('techr.aspx?s=2&op=3&id="+ id +"')\" >");
		if(send_to_supplier.ToLower() == "false" || send_to_supplier == "0")
		{
		
//		Response.Write("<tr align=right><td colspan="+ ncols +">");
		Response.Write("<input type=submit name=cmd value='SEND to SUPPLIER' "+ Session["button_style"] +" ");
		Response.Write(" onclick='return jCheckEmpty()' ");
		Response.Write("></td></tr>");
		}
		else
		{
//			Response.Write("<tr align=right><td colspan="+ ncols +">");
			Response.Write("<b><font color=red>*ALREADY SENT TO SUPPLIER</font></b> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
			Response.Write("<input type=button value='GO TO SUPPLIER RMA PROCESSING' "+ Session["button_style"] +" ");
			Response.Write(" onclick=\"window.location=('supp_rma.aspx?rma=rd&st=1&spp="+supplier_id+"')\" > ");
			//Response.Write("<input type=button  value='   BACK   '"+ Session["button_style"] +" onclick=\"window.history.back()\" > ");
			Response.Write("</td></tr>");
		}
	}
	Response.Write("</table>");
	
	/*Response.Write("<table align=center cellspacing=1 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"font-family:Verdana;font-size:6pt; \"><td colspan="+ ncols +">*Please fill up Product Code, Supplier_Code and Product Description if Item is Incorrect</th></tr>");
	Response.Write("</table>");
	*/
		Response.Write("<script language=javascript>");
	Response.Write("<!---hide from old browser");
	string s = @"		
		function jCheckEmpty()
		{			
			var bPass = true;
			if(document.frm.ra_number.value == '')
				bPass = false;
			if(document.frm.sn.value == '' && !document.frm.ignor.checked)
				bPass = false;
			if(document.frm.code.value == '')
				bPass = false;
			if(document.frm.supplier_code.value == '')
				bPass = false;
			if(document.frm.fault.value == '')
				bPass = false;
			if(document.frm.desc.value == '')
				bPass = false;
			if(document.frm.supplier.value == '')
				bPass = false;
			if(!bPass)
			{
				window.alert('Please Fill Up ALL Fields');
				document.frm.supplier_code.focus();
				return false;
			}
			else
			{
				if(!confirm('Processing the request...'))
				{
					return false;
				}
			}
			return true;
		}
		
	";
	Response.Write("--->");
	Response.Write(s);
	Response.Write("</script");
	Response.Write(">");

	Response.Write("</form><br><br>");

	
	return true;
}

</script>
<asp:Label id=LFooter runat=server/>
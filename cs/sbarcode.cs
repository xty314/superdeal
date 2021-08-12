<!-- #include file="fifo_f.cs" -->

<script runat=server>
DataSet dst = new DataSet();	//for creating Temp tables templated on an existing sql table

string m_code = "";
bool m_bBarcode = false;

int m_rows = 0;

string m_querystring = "";

void Page_Load(Object Src, EventArgs E ) 
{
	
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("stock"))
		return;

	if(Request.QueryString["pcode"] != null && Request.QueryString["pcode"] != null)
		m_querystring = Request.QueryString["pcode"];

	PrintAdminHeader();
	PrintAdminMenu();
/*	if(Request.QueryString["done"] == "1")
	{
		Response.Write("<center><br><h4>Transfer Done...");
		Response.Write("<br><br><a title='check purchase' href='purchase.aspx?t=pp&n="+ m_poID +"' class=o>View Purchase</a> ");
		Response.Write(" <br><br><a title='back to rma stock' href='ra_stock.aspx?id="+ m_trsid +"' class=o>Back to RMA Stock</a> ");
		return;
	}
*/
	if(Request.QueryString["del"] != null && Request.QueryString["del"] != "")
	{
		if(DoDeleteDuplicateItem())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "\">");
			return;
		}
	}
	
	if(Request.Form["cmd"] == "Search Barcode" || Request.Form["search"] != null && Request.QueryString["barcode"] != null && Request.QueryString["barcode"] != "" )
	{
		if(!DoBarcodeSearch())
			return;
	}
	else
		DoBarcodeSearch();
			
				
	vBindBarcode();	
	LFooter.Text = m_sAdminFooter;
}
bool DoDeleteDuplicateItem()
{
	string code = Request.QueryString["del"];
	if(code == "")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "\">");
		return false;
	}
	string sc = " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code = "+ code +") DELETE FROM code_relations c WHERE c.code = "+ code +" ";
//DEBUG("sc =", sc);
//return false;
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
void vBindBarcode()
{

	Response.Write("<center><h4>Search BARCODE</h>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table align=center cellspacing=2 cellpadding=3 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");

	if((Request.QueryString["barcode"] == "" || Request.QueryString["barcode"] == null))
	{
		if(dst.Tables["duplicate_bar"].Rows.Count > 0  )
		{
			Response.Write("<tr align=left bgcolor=#EEEEE>");
			Response.Write("<th>BARCODE#</th><th>DUPLICATE_QTY</th><th>&nbsp;</th>");
			Response.Write("</tr>");
			bool alterColor = false;
			for(int i=0; i<dst.Tables["duplicate_bar"].Rows.Count; i++)
			{
				DataRow dr = dst.Tables["duplicate_bar"].Rows[i];
				string barcode = dr["barcode"].ToString();
				string qty = dr["qty"].ToString();	
				
					
				Response.Write("<tr");
				if(alterColor)
					Response.Write(" bgcolor=#EEEEEE ");
				alterColor = !alterColor;
				Response.Write(">");
				//if(status == "1")
				Response.Write("<td>"+ barcode.ToUpper() +"</td>");
				Response.Write("<td>"+ qty +"</td>");
		//		Response.Write("<td>"+ branch_name +"</td>");
				if(barcode != "")
				Response.Write("<td><a title='search barcode' href='sbarcode.aspx?barcode="+  HttpUtility.UrlEncode(barcode) +"&r="+ DateTime.Now.ToOADate() +"' class=o>Search</a></td>");
				Response.Write("</tr>");

			}
			return;
		}
	}
	Response.Write("<tr align=left bgcolor=#EEEEE>");
	Response.Write("<td colspan=6>SEARCH :<input type=text name=search value="+ Request.Form["search"] +">");
	Response.Write("<input type=submit name=cmd value='Search Barcode' "+ Session["button_style"] +">");
	Response.Write("<input type=button name=cmd value='All Barcode' "+ Session["button_style"] +" onclick=\"window.location=('sbarcode.aspx?r="+ DateTime.Now.ToOADate() +"')\">");
	Response.Write("</td></tr>");

	if(m_rows > 0)
	{
		Response.Write("<tr align=left bgcolor=#EEEEE>");
		Response.Write("<th>BARCODE#</th><th>CODE#</th><th>SUPP_CODE#</th><th>DESCRIPTION</th><th>STOCK_QTY</th>");
		//Response.Write("<th>BRANCH</th>");
		Response.Write("<th>&nbsp;</th>");
		Response.Write("</tr>");
		bool alterColor = false;
		for(int i=0; i<dst.Tables["barcode"].Rows.Count; i++)
		{
			DataRow dr = dst.Tables["barcode"].Rows[i];
			string code = dr["code"].ToString();
			string qty = dr["qty"].ToString();
			string barcode = dr["barcode"].ToString();
			string name = dr["name"].ToString();
			string supplier_code = dr["supplier_code"].ToString();
			
			Response.Write("<tr");
			if(alterColor)
				Response.Write(" bgcolor=#EEEEEE ");
			alterColor = !alterColor;
			Response.Write(">");
			//if(status == "1")
			Response.Write("<td>"+ barcode.ToUpper() +"</td>");
			Response.Write("<td>"+ code +"</td>");
			Response.Write("<td>"+ supplier_code +"</td>");
			Response.Write("<td>"+ name +"</td>");
			Response.Write("<td>"+ qty +"</td>");
	//		Response.Write("<td>"+ branch_name +"</td>");
			
			Response.Write("<td>");
			Response.Write("<a title='delete product' href='sbarcode.aspx?del="+ HttpUtility.UrlEncode(code) +"&r="+ DateTime.Now.ToOADate() +"' onclick=\"if(!confirm('Yeah..baby..do..me!!!')){return false;}\"  class=o><font color=red>X</font></a> ");
			Response.Write("<a title='edit product' href='liveedit.aspx?code="+ HttpUtility.UrlEncode(code) +"&r="+ DateTime.Now.ToOADate() +"' class=o target=blank>Edit</a></td>");
		
			Response.Write("</tr>");

		}
	}
	Response.Write("</table></form>");
}
bool DoBarcodeSearch()
{
	string sc = "";
	if(Request.QueryString["barcode"] == null)
	{
	sc = " SELECT count(cr.barcode) AS qty, barcode FROM code_relations cr group by barcode ";
	sc += " Having count(barcode) > 1 ";
//	sc += " WHERE barcode not null ";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "duplicate_bar");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	}
	if((Request.Form["search"] != null && Request.Form["cmd"] == "Search Barcode") || Request.QueryString["barcode"] != null && Request.QueryString["barcode"] != "")
	{
		string keyword = Request.Form["search"];
		if(Request.QueryString["barcode"] != null)
			keyword = Request.QueryString["barcode"];
		sc = "SELECT cr.*, (SELECT sum(isnull(qty,0)) FROM stock_qty WHERE code = cr.code ) AS qty ";
		sc += " FROM code_relations cr "; //LEFT OUTER JOIN stock_qty s ON s.code = cr.code ";
		//sc += " JOIN branch b ON b.id = s.branch_id ";
		sc += " WHERE 1=1 ";
		sc += " AND cr.barcode = '" + EncodeQuote(keyword) +"' ";
//	DEBUG("sssc = ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			m_rows = myAdapter.Fill(dst, "barcode");
	//DEBUG("rows=", rows);
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}
	return true;
}
</script>

<asp:Label id=LFooter runat=server/>

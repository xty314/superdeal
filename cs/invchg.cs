<!-- #include file="isdate.cs" -->

<script runat=server>

DataSet dst = new DataSet();
DataTable dtAssets = new DataTable();

string m_id = "";
bool m_bRecorded = false;

string m_branch = "";
string m_branch_name = "";
string m_agent_id = "";
string m_sales = "";
string m_invoice_date = "";
string m_agent_name = "";
string m_note = "";
string m_invoice_number = "";
string m_freight = "";
string m_tax = "";
string m_customer_gst = "";
double m_dsales_total = 0;
double d_dicount = 0;
string d_dicount_old = "";
string m_customer_id = "";


void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("accountant"))
		return;

	if(Request.QueryString["back"] != null && Request.QueryString["back"] != "")
	{
		if(DoUnLockInvoice())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?keyword="+ Request.QueryString["back"] +"\">");
		}
		return;
	}

	/*if(Request.Form["cmd"] == Lang("Set"))
	{
		if(DoUpdateRecord())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=invchg.aspx?i="+ m_invoice_number +"&ds="+ d_dicount_old +"\">");
		}
		return;
	}
     */
	
	if(Request.Form["cmd"] == "Update Changes")
	{
		if(DoUpdateRecord())
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=invchg.aspx?t=done&n="+ m_invoice_number +"\">");
		}
		return;
	}
	if(Request.QueryString["t"] == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<center><h4>Update done...");
		Response.Write("<br><br><a title='back' href='invchg.aspx?i="+ Request.QueryString["n"] +"' class=o>Back</a>");
		Response.Write("<br><br><a title='back to order list' href='olist.aspx?r="+ DateTime.Now.ToOADate() +"' class=o>Back to Order List</a>");
			PrintAdminFooter();
		return;
	}
	if(!RqQueryString())
		return;
	PrintAdminHeader();
	PrintAdminMenu();
	
	DoQueryInvoice();

	PrintAdminFooter();

}
bool RqQueryString()
{
	if(Request.QueryString["i"] != null && Request.QueryString["i"] != "")
		m_invoice_number = Request.QueryString["i"];
	if(m_invoice_number == "" || m_invoice_number == null)
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=olist.aspx?o=14\">");
		return false;
	}
	if(!TSIsDigit(m_invoice_number))
		return false;

	if(!DoLockInvoice())
		return false;
	return true;
}


void UpdateAllFields()
{
	if(Session["branch_support"] != null)
		Session["assets_branch"] = Request.Form["branch"];
	Session["assets_customer"] = Request.Form["customer"];
	Session["assets_from_account"] = Request.Form["from_account"];
	Session["assets_to_account"] = Request.Form["to_account"];
	Session["assets_payment_type"] = Request.Form["payment_type"];
	Session["assets_payment_ref"] = Request.Form["payment_ref"];
	Session["assets_payment_date"] = Request.Form["payment_date"];
	Session["assets_note"] = Request.Form["note"];
}

bool CheckAssetsTable()
{
	if(Session["AssetsTable"] == null) 
	{
		dtAssets.Columns.Add(new DataColumn("name", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("invoice_number", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("invoice_date", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("tax", typeof(String)));
		dtAssets.Columns.Add(new DataColumn("total", typeof(String)));
		Session["AssetsTable"] = dtAssets;
		return false;
	}
	else
	{
		dtAssets = (DataTable)Session["AssetsTable"];
	}
	return true;
}


bool DoQueryInvoice()
{
	double dSubTotal = 0;
    

	if(dst.Tables["invoice"] != null)
		dst.Tables["invoice"].Clear();
	if(dst.Tables["sales_item"] != null)
		dst.Tables["sales_item"].Clear();
	string sc = " SELECT i.*, c.name AS agent_name, c.id AS agent_id, b.name AS branch_name, o.sales AS salesId ";
	sc += " FROM invoice i Left outer JOIN card c ON c.id = i.agent ";
	sc += " JOIN orders o ON o.invoice_number = i.invoice_number "; 
	sc += " JOIN branch b ON b.id = i.branch ";

	sc += " WHERE i.invoice_number =" + m_invoice_number;
//DEBUG("s c= ", sc);
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "invoice");
		if(rows == 1)
		{
			m_sales = dst.Tables["invoice"].Rows[0]["salesId"].ToString();
            m_customer_id = dst.Tables["invoice"].Rows[0]["card_id"].ToString();
			m_invoice_number = dst.Tables["invoice"].Rows[0]["invoice_number"].ToString();
			m_invoice_date = DateTime.Parse(dst.Tables["invoice"].Rows[0]["commit_date"].ToString()).ToString("dd-MM-yyyy");
			m_branch_name = dst.Tables["invoice"].Rows[0]["branch_name"].ToString();
		//	m_invoice_number = dst.Tables["invoice"].[0]["invoice_number"].ToString();
			m_agent_name = dst.Tables["invoice"].Rows[0]["agent_name"].ToString();
			m_agent_id = dst.Tables["invoice"].Rows[0]["agent_id"].ToString();
			m_dsales_total = MyDoubleParse(dst.Tables["invoice"].Rows[0]["total"].ToString());
			m_branch = dst.Tables["invoice"].Rows[0]["branch"].ToString();
			m_freight = dst.Tables["invoice"].Rows[0]["freight"].ToString();
			m_tax =  dst.Tables["invoice"].Rows[0]["tax"].ToString();
			m_customer_gst = dst.Tables["invoice"].Rows[0]["customer_gst"].ToString();
            dSubTotal = double.Parse(dst.Tables["invoice"].Rows[0]["price"].ToString());
			
		}
		else
		{
			Response.Write("Invalid Invoice number!!! closing now!!!");
			Response.Write("<script language=javascript>window.close();</script");
			Response.Write(">");
			return false;
		}
	
	}
	catch(Exception e) 
	{
		ShowExp("Error!!! Invalid Invoice", e);
		return false;
	}
    
    rows = 0;
	sc = " SELECT s.*, Round(s.commit_price, 2) AS sprice, c.barcode ";
	sc += " FROM sales s JOIN code_relations c ON c.code = s.code ";
	
	sc += " WHERE s.invoice_number = "+ m_invoice_number;
	sc += " ORDER BY s.id ";
//DEBUG("sc = ",sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "sales_item");
	}

	catch(Exception e) 
	{
		ShowExp("Error!!! Invalid Invoice", e);
		return false;
	}

	Response.Write(sJavafunction());
    Response.Write("<form name=f method=post>");
    Response.Write("<center><br><h3>INVOICE# "+ m_invoice_number +"</h3></br></center>");
    Response.Write("<table align=center width=80% cellspacing=0 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
    Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
    Response.Write("<tr >"); //style=\"color:white;background-color:#66696;font-weight:bold;\">");
    Response.Write("<td colspan=10>");
    Response.Write("<table width=100% cellspacing=0 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=#EEEEE");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
 
	Response.Write("<tr><th align=left width=25%>SALES :</td><td width=38%>"+ DisplayCardOptions(m_sales, "", true) +"</td>");
    
    Response.Write("<th align=right width=25%>BRANCH :</td><td align=right width=12%>");
    PrintBranchNameOptions(m_branch);
	Response.Write("</td></tr>");
//DEBUG("b= ", m_branch);
    //Agent and branch old
    Response.Write("<input type=hidden name=branch_old value='" + m_branch + "'>");
    Response.Write("<input type=hidden name=supplier value='" + m_agent_id + "'>");
	Response.Write("<tr ><th align=left width=25%>INVOICE DATE :</td><td width=38%>");
    Response.Write("<input type=text name=date value="+ m_invoice_date +" ");
    Response.Write(" onclick=\"displayDatePicker(this.name);\"></td>");
    Response.Write("<th align=right width=25%>Customer :</td><td align=right width=12%>");
//	Response.Write("<select name=agent>");
	//Response.Write(PrintSupplierOptions(m_agent_id, "", "agent"));
    Response.Write(DisplayCardOptions(m_customer_id, "", false));
 
//	Response.Write("</select>");
	Response.Write("</td></tr>");
	//Response.Write(""+ m_agent_name +"</td></tr>");
	Response.Write("</table>");
	Response.Write("</tr>");
	Response.Write("<tr style=\"color:white;background-color:#349ede;font-weight:bold;\"><th align=left>CODE</th><th align=left>M_PN</th><th align=left>BARCODE</th><th>DESCRIPTION</th><th>QTY</th><th align=right>PRICE</th><th align=right>TOTAL</th></tr>");
	bool bAlter = false;
	Response.Write("<input type=hidden name=ntrow value="+ rows +">");
	Response.Write("<input type=hidden name=invoice_number value="+ m_invoice_number +">");
	string keyEnter = "onKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"";
	double d_sub_total = 0; 
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["sales_item"].Rows[i];
		string barcode = dr["barcode"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["quantity"].ToString();
        string sDiscount = dr["discount_percent"].ToString();
        double sales_price = MyDoubleParse(dr["sprice"].ToString());
        double priceOld = sales_price;
        sales_price = sales_price * (1-MyDoubleParse(sDiscount)/100);
//		sales_price *= 1.125;
		double dtotal = MyDoubleParse(qty) * sales_price;
		d_sub_total += dtotal;
	//	m_dsales_total += dtotal;
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		bAlter = !bAlter;
		Response.Write(">");
		Response.Write("<td>");
		Response.Write(code);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(dr["supplier_code"].ToString());
		Response.Write("</td>");

		Response.Write("<td>");
		Response.Write(barcode);
		Response.Write("</td>");
		Response.Write("<td>");
		Response.Write(name);
		Response.Write("</td>");
        Response.Write("<input type=hidden name=oldQty"+ i +" value="+ qty +">");
		Response.Write("<td align=center>");
		//Response.Write(qty);
        Response.Write("<input  style='text-align: right' size=5 name=nqty"+ i +" value="+ qty +" ");
        Response.Write(" onchange=\" calsalesprice(document.f.price"+ i +".value, this.value, document.f.nrow"+ i +".value, ");
        if(sDiscount != "0")
            Response.Write("document.f.dis"+ i +".value) \" "+ keyEnter +">");
        else
            Response.Write("document.f.oldDis"+ i +".value) \" "+ keyEnter +">");
		Response.Write("</td>");
		Response.Write("<td align=right>");
        if(sDiscount != "0")
        {
            Response.Write("<font color='#FF0000'>DIS:&nbsp;<input size=3 style='text-align: right' type=text name=dis"+ i +" value="+ sDiscount +" ");
            Response.Write(" onchange=\" calsalesprice(document.f.price"+ i +".value, document.f.nqty"+ i +".value, document.f.nrow"+ i +".value, this.value) \" "+ keyEnter +">%</font>&nbsp;&nbsp;");
        }
        Response.Write("<input type=hidden name=oldDis"+ i +" value="+ sDiscount +">");
		Response.Write("<input type=hidden name=hcode"+ i +" value="+ code +">");
		Response.Write("<input type=hidden name=hprice"+ i +" value="+ sales_price.ToString() +">");
        Response.Write("<input type=hidden name=oldPrice"+ i +" value="+ priceOld.ToString() +">");
		//Response.Write("<input type=hidden name=nqty"+ i +" value="+ qty +">");
		Response.Write("<input type=hidden name=nrow"+ i +" value="+ i +">");
		Response.Write("<input style='text-align: right; background-color:#FFCC99' type=text readonly name=price"+ i +" value="+ priceOld +" ");
        Response.Write(" onchange=\" calsalesprice(this.value, document.f.nqty"+ i +".value, document.f.nrow"+ i +".value, document.f.dis"+ i +".value) \" "+ keyEnter +" ");
        Response.Write(">");
		Response.Write("</td>");
		Response.Write("<td align=right style=\"width:150px\"><b>");
		Response.Write("<input width=15px style='text-align: right; background-color:#FFCC99' type=text readonly name=total"+ i +"  value="+ dtotal.ToString() +" "+ keyEnter +">");
		//Response.Write(dtotal.ToString("c"));
		Response.Write("</b></td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr align=right><th colspan=6><b>SUB TOTAL :</b></th>");
    Response.Write("<td style=\"width:150px\"> <input width=15px style='text-align: right;  background-color:#FFCC99' type=text name=subtotal readonly=readonly value='"+ dSubTotal.ToString("c") +"'></td></tr>");
	//Response.Write("<td style=\"width:150px\" align=right ><b>" + dSubTotal.ToString("c") + "</b></td></tr>");
    Response.Write("<tr align=right><th colspan=6><b>Freight :</b></th><td style=\"width:150px\"><input style='text-align: right' type=text name=freight value='"+ Math.Round(MyDoubleParse(m_freight), 2).ToString() +"'></td></tr>");
	Response.Write("<tr align=right><th colspan=6><b>Tax :</b></th>");
    //Response.Write("<td style=\"width:150px\" ><input style='text-align: right; width:75px' type=text name=tax_show readonly=readonly value='"+ Math.Round(MyDoubleParse(m_tax), 2).ToString() +"'>&nbsp;");
    Response.Write("<td style=\"width:150px\" align=right ><b>" + Math.Round(MyDoubleParse(m_tax), 2).ToString("c"));
    /*Response.Write("<select name=tax_option><option value=0 ");
	if(m_customer_gst == "0")
		Response.Write(" selected ");
	Response.Write(">0</option><option value=125 ");
	if(m_customer_gst == "0.125")
		Response.Write(" selected ");
	Response.Write(">12.5%</option><option value=15 ");
	if(m_customer_gst == "0.15")
		Response.Write(" selected ");
	Response.Write(">15%</option></select></td></tr>");
     */
    Response.Write("</b></td></tr>");
	
	/*if(g("ds") != "0" && g("ds") != "")
		d_dicount = MyDoubleParse(g("ds"));
	else
		d_dicount = (1- (m_dsales_total/((d_sub_total+MyDoubleParse(m_freight))*(1+MyDoubleParse(m_customer_gst)))))*100;
     */
	//DEBUG("m_customer_gst=", m_customer_gst);
	//DEBUG("d_dicount=", d_dicount);
		
	//Response.Write("<tr align=right><th colspan=6><b>Discount(%) :</b></th><td style=\"width:150px\"><input style='text-align: right; width:93px' tsype=text name=discount value='"+ Math.Round(d_dicount, 2).ToString() +"'><input type=submit name=cmd style='width:50px' value='" + Lang("Set") + "'></td></tr>");
	
	Response.Write("<tr align=right><th colspan=6><b>TOTAL(Incl.GST) :</b></th>");
    //Response.Write("<td style=\"width:150px\"><input style='text-align: right' type=text name=total readonly=readonly value='"+ Math.Round(m_dsales_total, 2).ToString() +"'></td></tr>");
	Response.Write("<td style=\"width:150px\" align=right ><b>" + Math.Round(m_dsales_total, 2).ToString("c") + "</b></td></tr>");
	
	
	

	//Response.Write("<tr align=right><td colspan=7><input type=button value='Apply Sub Total to items'  "+ Session["button_style"] +" onclick='CalcDiscount();'>");
	Response.Write("<tr align=right><td colspan=7>");
	Response.Write("<input type=button name=cmd value='Back to Order list' "+ Session["button_style"] +" ");
    Response.Write("onclick=\"window.location=('invchg.aspx?back="+ m_invoice_number +"') \">");
	Response.Write("<input type=reset name=cmd value='Clear Change' "+ Session["button_style"] +">");
	Response.Write("<input type=submit name=cmd value='Update Changes' "+ Session["button_style"] +" ");
    //Response.Write("onclick=\"if(document.f.subtotal.value=='' || document.f.subtotal.value=='NaN'){window.alert('invalid price'); return false;} else {return confirm('Processing...');} \"></td></tr>");
	
    Response.Write("onclick=\"if(!window.confirm('");
    Response.Write(Lang("Are you sure to update this invoice?") + "')){return false;}else{return confirm('Processing...');}\"></td></tr>");
    Response.Write("</table>");
	Response.Write("<input type=hidden name=subtotal_org value='" + m_dsales_total + "'>");
    Response.Write("<input type=hidden name=realSubtotal value='" + d_sub_total + "'>");
	Response.Write("</form>");
	
	return true;
}
string sJavafunction()
{
	string s = " <script language=javascript> \r\n";
	s += " function caltotal(trow) { \r\n";
	s += " var dstotal = 0; ";	
	
	s += " for(var i=0; i<Number(trow); i++) {\r\n";
	s += "  eval( \"dstotal += Number(document.f.total\"+ i +\".value)\"); ";
	s += " } ";
	s += " document.f.subtotal.value = dstotal.toFixed(2); ";
	s += " }\r\n";
	s += " function calsalesprice(salesprice, nqty, nrow, dis) \r\n";
	s += " { \r\n";
	s += " var price = Number(salesprice) * Number(nqty)*(100 - Number(dis))/100; \r\n ";

	s += " eval( \"document.f.total\"+ nrow +\".value = price.toFixed(2) \"); \r\n";
	s += " var trow = document.f.ntrow.value; ";

	s += " caltotal(trow); \r\n";
	s += " } \r\n";

	s += @"
function CalcDiscount()
{
	var dOrgTotal = Number(document.f.subtotal_org.value);
	var sTotal = document.f.subtotal.value;
	sTotal = sTotal.replace('$', '');
	sTotal = sTotal.replace(',', '');
	var dTotal = Number(sTotal);
	var dd = dTotal / dOrgTotal;
	var nRow = Number(document.f.ntrow.value);
	var dFinalTotal = 0;
	for(var i=0; i<nRow; i++)
	{
		var op = eval('Number(document.f.hprice' + i + '.value)');
		var qty = eval('Number(document.f.nqty' + i + '.value)');
		var np = op * dd;
		var total = np * qty;
		dFinalTotal += total;
		eval('document.f.price' + i + '.value=np.toFixed(2)');
		eval('document.f.total' + i + '.value=total.toFixed(2)');
	}
	document.f.subtotal.value = dFinalTotal.toFixed(2);
}
	";
	s += " </script";
	s += " > \r\n";
	return s;
}
bool DoUnLockInvoice()
{
	string sc = "UPDATE orders set locked_by = null, time_locked=null WHERE invoice_number = "+ Request.QueryString["back"];
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

bool DoLockInvoice()
{
	string sc = "UPDATE orders set locked_by = "+ Session["card_id"] +", time_locked=getdate() WHERE invoice_number = "+ m_invoice_number;
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

bool DoUpdateRecord()
{
	
    if(dst.Tables["orderNum"] != null)
		dst.Tables["orderNum"].Clear();
    m_invoice_number = Request.Form["invoice_number"];
    if(m_invoice_number == "" || m_invoice_number == null)
        return false;
    string sc = "";
    sc = " SELECT number FROM orders WHERE invoice_number=" +  m_invoice_number;
    try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "orderNum") != 1)
            return false;
    }
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
    
    string orderNum = dst.Tables["orderNum"].Rows[0]["number"].ToString();
    string branchOld = Request.Form["branch_old"];
    m_branch = Request.Form["branch"];
    int rows = 0;
	rows = int.Parse(Request.Form["ntrow"].ToString());
    for(int i=0; i<rows; i++)
    {
        string sCode = Request.Form["hcode" + i];
        string sQty = Request.Form["nqty" + i];
        string sPrice = Request.Form["price" + i];
        string sOldQty = Request.Form["oldQty" + i];
        string sOldDis = Request.Form["oldDis" + i];
        string sRealDis = Request.Form["dis" + i];
        int realQty = int.Parse(sQty) - int.Parse(sOldQty);
        sc = "Begin Transaction ";
        sc += " UPDATE order_item SET quantity = '" + sQty + "'"; //, commit_price = '" + sPrice + "' 
        if(sOldDis != "0")
            sc += ", discount_percent = '" + sRealDis +"'";
        sc += " WHERE id=" + orderNum + " AND code = " + sCode;
        sc += " UPDATE sales SET quantity = '" + sQty + "'";//, commit_price = '" + sPrice + "' 
        if(sOldDis != "0")
            sc += ", discount_percent = '" + sRealDis +"'";
        sc += " WHERE invoice_number=" + m_invoice_number + " AND code = " + sCode;
        if(branchOld == m_branch)
            sc += " UPDATE stock_qty SET qty = qty - " + realQty + " WHERE code = " + sCode + " AND branch_id = " + m_branch;
        else
        {
            sc += " UPDATE stock_qty SET qty = qty - " + sQty + " WHERE code = " + sCode + " AND branch_id = " + m_branch; 
            sc += " UPDATE stock_qty SET qty = qty + " + sOldQty + " WHERE code = " + sCode + " AND branch_id = " + branchOld; 
        }
        sc += " COMMIT ";
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
	string agent = Request.Form["supplier"];
    m_sales = Request.Form["employee"];
    string salesName = "Sales";
    if(dst.Tables["salesname"] != null)
	    dst.Tables["salesname"].Clear();
    sc = " SELECT name FROM card where id='" + m_sales +"'";
    try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "salesname") != 1)
            return false;
        else
            salesName = dst.Tables["salesname"].Rows[0]["name"].ToString();
    }
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
    
    m_invoice_date = Request.Form["date"];
    m_customer_id = Request.Form["customer"];
  //  DEBUG("name= ", m_customer_id);
	if(agent == "")
		agent = "0";
	//string tprice = Request.Form["subtotal"];
	//string newdate = Request.Form["newdate"];
	//string d_discount = Request.Form["discount"];
	//d_dicount_old = d_discount;
	//string customer_gst = Request.Form["tax_option"];
    string customer_gst = "0.15";
	string sub_total = Request.Form["subtotal"];
	string freight = Request.Form["freight"];
	//customer_gst = customer_gst.Replace("%", "");
	sub_total = sub_total.Replace("$","");
	sub_total = sub_total.Replace(",","");
  	//tprice = tprice.Replace("$","");
	//tprice = tprice.Replace(",","");
	/*if(customer_gst=="125")
	{
		customer_gst = (MyDoubleParse(customer_gst)/1000).ToString();
		//tax = ((MyDoubleParse(sub_total)+ MyDoubleParse(freight))*MyDoubleParse(customer_gst)).ToString();
	}
	else
	{
		customer_gst = (MyDoubleParse(customer_gst)/100).ToString();
		//tax = ((MyDoubleParse(sub_total)+ MyDoubleParse(freight))*MyDoubleParse(customer_gst)).ToString();
	}
     */
	//DEBUG("TAX=", tax);
	//DEBUG("d_discount=", d_discount);
	//string tax = (MyDoubleParse(tprice) / 7.66667).ToString() ;
	//string price = (MyDoubleParse(tprice) - MyDoubleParse(tax)).ToString();
	string price_total = ((MyDoubleParse(sub_total) + MyDoubleParse(freight))*1.15).ToString();
    string tax = ((MyDoubleParse(sub_total) + MyDoubleParse(freight))*0.15).ToString();
	//price_total = (MyDoubleParse(price_total)*(1-MyDoubleParse(d_discount)/100)).ToString();
	//tax = (MyDoubleParse(price_total)*MyDoubleParse(customer_gst)).ToString();
	//tax = ((MyDoubleParse(price_total)/(1+MyDoubleParse(customer_gst)))*MyDoubleParse(customer_gst)).ToString();
	//string invSubTotal = (MyDoubleParse(sub_total)*(1-MyDoubleParse(d_discount)/100)).ToString();
    
	
	//DEBUG("price_total=", price_total);

    sc = "Begin Transaction ";
    sc += " SET DATEFORMAT dmy UPDATE invoice set card_id =" + m_customer_id +", agent ="+ agent +", price = "+ sub_total +", commit_date='"+m_invoice_date+"'";
    sc +=", branch = " + m_branch + ", sales = '" + salesName + "', tax = "+ tax +", freight = " + freight + ",  total = "+ price_total +" where invoice_number = "+ m_invoice_number;
    sc += " UPDATE orders set card_id =" + m_customer_id +", branch = " + m_branch + ", sales = '" + m_sales +"', agent = "+ agent + "WHERE invoice_number = "+ m_invoice_number +" ";
/*for (int i=0; i<rows; i++)
{
	code = Request.Form["hcode"+ i];
	commit_price = Request.Form["price"+ i];
	commit_price = (MyDoubleParse(commit_price) - (MyDoubleParse(commit_price) / 7.66667 )).ToString();
	sc += " UPDATE sales set commit_price = "+ commit_price +" where invoice_number = "+ m_invoice_number +" and code = "+ code;
	sc += " UPDATE order_item set commit_price = "+ commit_price +" where id = (select id from orders where invoice_number = "+ m_invoice_number +") and code = "+ code;
}*/
    sc += " UPDATE orders SET locked_by = null, time_locked = null WHERE invoice_number = "+ m_invoice_number;
    sc += " commit ";
//DEBUG("sc = ",sc);

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
string DisplayCardOptions(string current_id, string branch_id, bool employee)
{
	DataSet dssup = new DataSet();
	//string type_customer = GetEnumID("card_type", "supplier");
	int rows = 0;
	string sc = "SELECT id, trading_name, name, email, company ";
	sc += " FROM card ";
	sc += " WHERE ";
    if(employee)
        sc += " type=4 ";
    else
        sc += "(type = 1 OR type = 2) ";
    if(branch_id != "")
        sc += " AND our_branch = "+ branch_id;
	sc += " ORDER BY company";
//DEBUG("sc= ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dssup, "customer");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "\r\n<select ";
    if(employee)
        s += " name=employee ";
    else
        s += " name=customer ";
	s += ">";
	for(int i=0; i<rows; i++)
	{
		string id = dssup.Tables["customer"].Rows[i]["id"].ToString();
		string name = dssup.Tables["customer"].Rows[i]["company"].ToString();
		if(name == "")
			name = dssup.Tables["customer"].Rows[i]["trading_name"].ToString();
		if(name == "" || employee)
			name = dssup.Tables["customer"].Rows[i]["name"].ToString();
		s += "<option value=" + dssup.Tables["customer"].Rows[i]["id"].ToString();
		if(current_id == id)
			s += " selected";
		s += ">" + name + "</option>\r\n";
	}
	s += "\r\n</select>";
	return s;
}

</script>

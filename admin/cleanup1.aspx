<!-- #include file="config.cs" -->

<script runat=server>
bool m_bOrder = false;
protected void Page_Load(Object Src, EventArgs E ) 
{

	TS_PageLoad(); //do common things, LogVisit etc...
	if(!TS_UserLoggedIn() || Session["email"] == null)
	{
		Response.Write("<h3>Error, please <a href=login.aspx>login</a> first</h3>");
		return;
	}
	if(!SecurityCheck("administrator"))
		return;
//DEBUG("sesiosnemail = ", Session["email"].ToString());
if(Session["email"].ToString().IndexOf("@eznz.com") <= 0)
//	if(Session["email"].ToString() != "tee@eznz.com" && Session["email"].ToString() != "darcy@eznz.com" && Session["email"].ToString() != "neo@eznz.com"  && Session["email"].ToString() != "jerry@eznz.com" && Session["email"].ToString() != "jerry@edencomputer.com")
	{
		return;
	}


	string sc = "";
	bool bdelaccount = false;
	bool bdelcard = false;
	bool bdelall = false;
	bool bdelproduct = false;
	if(Request.QueryString["done"] == "1")
	{
	
		PrintAdminHeader();
	PrintAdminMenu();
		Response.Write("<center><h4>done...");
		Response.Write("<br><br><a href='"+ Request.ServerVariables["URL"] +"' title='new'>Clean More</a></h4></center>");

	PrintAdminFooter();
		return;
	}
	
	if(Request.QueryString["cl"] == "all" || Request.QueryString["cl"] == "account" || Request.QueryString["cl"] == "card" || Request.QueryString["cl"] == "product")
	{	
		if(Request.QueryString["cl"] == "all")
			bdelall = true;
		if(Request.QueryString["cl"] == "account")
			bdelaccount = true;
		if(Request.QueryString["cl"] == "card")
			bdelcard = true;
		if(Request.QueryString["cl"] == "product")
			bdelproduct = true;
		if(bdelaccount || bdelall)
		{
		//	sc += " delete from acc_equity where 1=1 ";
		//	sc += " delete from acc_refund where 1=1";
		//	sc += " delete from acc_refund_sub where 1=1";
			sc += " update account set balance = 0, opening_balance=0 where 1=1";
		//	sc += " delete from account_adjust_log where 1=1";
		//	sc += " delete from accrecon where 1=1";
		//	sc += " delete from assets where 1=1";
		//	sc += " delete from assets_item where 1=1";
		//	sc += " delete from assets_payment where 1=1";
			sc += " delete from auto_expense where 1=1";
			sc += " delete from credit where 1=1";
			sc += " update card set balance=0, trans_total=0 where 1=1";
		//	sc += " delete from custom_tax where 1=1";
		//	sc += " delete from custom_tax_log where 1=1";
		//	sc += " delete from custom_tax_sub where 1=1";
			sc += " delete from expense where 1=1";
			sc += " delete from expense_item where 1=1";
			sc += " delete from invoice where 1=1";
			sc += " delete from invoice_freight where 1=1";
			sc += " delete from invoice_note where 1=1";
			sc += " delete from order_item where 1=1";
			sc += " delete from order_kit where 1=1";
			sc += " delete from orders where 1=1";
			sc += " delete from purchase where 1=1";
			sc += " delete from purchase_item where 1=1";
			sc += " delete from ra_freight where 1=1";
			sc += " delete from ra_replaced where 1=1";
			sc += " delete from ra_statement where 1=1";
			sc += " delete from repair where 1=1";
			sc += " delete from repair_log where 1=1";
			sc += " delete from return_sn where 1=1";
			sc += " delete from rma where 1=1";
			sc += " delete from rma_stock where 1=1";
			sc += " delete from sales where 1=1";
			sc += " delete from sales_cost where 1=1";
			sc += " delete from sales_serial where 1=1";
			sc += " delete from serial_trace where 1=1";
			sc += " delete from support_log where 1=1";
			sc += " delete from stock where 1=1";
			sc += " delete from stock_adj_log where 1=1";
			sc += " delete from stock_borrow where 1=1";
			sc += " delete from stock_borrow_return where 1=1";
			sc += " delete from stock_borrow_sn where 1=1";
			sc += " delete from stock_cost where 1=1";
			sc += " delete from stock_loss where 1=1";
			sc += " delete from stock_qty where 1=1";
			sc += " delete from tran_deposit where 1=1";
			sc += " delete from tran_deposit_id where 1=1";
			sc += " delete from tran_detail where 1=1";
			sc += " delete from tran_invoice where 1=1";
			sc += " delete from trans where 1=1";
			sc += " delete from trans_other where 1=1";
			sc += " DBCC CHECKIDENT('auto_expense', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('credit', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('expense', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('expense_item', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('invoice', RESEED, 10000) ";
			sc += " DBCC CHECKIDENT('invoice_freight', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('invoice_note', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('orders', RESEED, 1000) ";
			sc += " DBCC CHECKIDENT('order_item', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('order_kit', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('purchase', RESEED, 5000) ";
			sc += " DBCC CHECKIDENT('purchase_item', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('ra_freight', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('ra_replaced', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('ra_statement', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('repair', RESEED, 4000) ";
			sc += " DBCC CHECKIDENT('repair_log', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('sales', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('sales_cost', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('sales_serial', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('serial_trace', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock_adj_log', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock_borrow', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock_borrow_return', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock_borrow_sn', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock_qty', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('stock_loss', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('tran_deposit', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('tran_deposit_id', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('tran_detail', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('tran_invoice', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('trans', RESEED, 0) ";
			sc += " DBCC CHECKIDENT('trans_other', RESEED, 0) ";
		}
//	--clean product account --
		if( bdelall|| bdelproduct)
		{
			sc += " delete FROM product where 1=1 ";
			sc += " delete from code_relations where 1=1 ";
			sc += " delete from product_skip where 1=1 ";
			sc += " delete from product_details where 1=1 ";
			sc += " delete from product_new where 1=1 ";
			sc += " delete from product_raw where 1=1 ";
			sc += " delete from product_update_log where 1=1 ";
			sc += " delete from catalog where 1=1 ";
			sc += " delete from catalog_popular where 1=1 ";
			sc += " delete from catalog_temp where 1=1 ";
			sc += " delete from cat_seq where 1=1 ";
			sc += " delete from cat_cross where 1=1 ";
			sc += " delete from cat_cross_kit where 1=1 ";
		}
//	-- clean up card list --
		if( bdelall || bdelcard)
		{
			sc += " delete from card where (type <> 4) AND id <> 0 "; // type <> 4 and type <> 5  and type <> 3";
		}
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
			return;
		}
		
//		DEBUG("sc = ", sc);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?done=1 \">");
		return;
	}
	PrintAdminHeader();
	PrintAdminMenu();
	vShowPage();
	PrintAdminFooter();
	
	return;
}

void vShowPage()
{
	Response.Write("<br><center><h4>Database Clean-up, do with CARE!!!</h4>");
	Response.Write("<form name=f method=post>");
	Response.Write("<br><table border=1><tr><td>");
	Response.Write("<input type=button value='Clear All Sales/Purchase Orders' onclick=\"if(confirm('Are you Sure!!!')){window.location=('"+ Request.ServerVariables["URL"] +"?cl=account');}else{return false;}\">");
//	Response.Write("</td></tr><tr><td>");
	Response.Write("<input type=button value='Clear All products' onclick=\"if(confirm('Are you Sure!!!')){window.location=('"+ Request.ServerVariables["URL"] +"?cl=product');}else{return false;}\">");
//	Response.Write("</td></tr><tr><td>");
	Response.Write("<input type=button value='Clear All card list, except employee and suppliers' onclick=\"if(confirm('Are you Sure!!!')){window.location=('"+ Request.ServerVariables["URL"] +"?cl=card');}else{return false;}\">");
//	Response.Write("</td></tr><tr><td>");
	Response.Write("<input type=button value='Clear All' onclick=\"if(confirm('Are you Sure!!!')){window.location=('"+ Request.ServerVariables["URL"] +"?cl=all');}else{return false;}\">");
//	Response.Write("</td></tr><tr><td>");
	Response.Write("<br></td></tr></table><br>");
	Response.Write("</form>");
}
</script>

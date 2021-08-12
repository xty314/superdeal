<!-- #include file="config.cs" -->

<script runat=server>
bool m_bOrder = false;
protected void Page_Load(Object Src, EventArgs E ) 
{

	TS_PageLoad(); //do common things, LogVisit etc...
	if(Session["email"] == null)
	//if(!TS_UserLoggedIn() || Session["email"] == null)
	{
		Response.Write("<h3>Error, please <a href=login.aspx>login</a> first</h3>");
		return;
	}
	if(!SecurityCheck("administrator"))
		return;

//	if(Session["email"].ToString() != "tee@eznz.com" && Session["email"].ToString() != "darcy@eznz.com" && Session["email"].ToString() != "neo@eznz.com"  && Session["email"].ToString() != "jerry@eznz.com" && Session["email"].ToString() != "jerry@edencomputer.com" || Session["email"].ToString().IndexOf("@eznz.com") <=0 )
//	if(Session["email"].ToString().IndexOf("@eznz.com") <=0 )
	{
//		return;
	}

	string sc = "";
	bool bdelaccount = false;
	bool bdelcard = false;
	bool bdelall = false;
	bool bdelproduct = false;
	bool bcleansupplier = false;
	if(Request.QueryString["done"] == "1")
	{
	
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<center><h4>done...");
		Response.Write("<br><br><a href='"+ Request.ServerVariables["URL"] +"' title='new'>Clean More</a></h4></center>");

	PrintAdminFooter();
		return;
	}
	
	if(Request.QueryString["cl"] == "all" || Request.QueryString["cl"] == "account" || Request.QueryString["cl"] == "card" || Request.QueryString["cl"] == "product"  || Request.QueryString["cl"] == "supplier")
	{	
		if(Request.QueryString["cl"] == "all")
			bdelall = true;
		if(Request.QueryString["cl"] == "account")
			bdelaccount = true;
		if(Request.QueryString["cl"] == "card")
			bdelcard = true;
		if(Request.QueryString["cl"] == "product")
			bdelproduct = true;
		if(Request.QueryString["cl"] == "supplier")
			bcleansupplier = true;
		if(bdelaccount || bdelall)
		{
		/*	sc += " delete from acc_equity where 1=1 ";
			sc += " delete from acc_refund where 1=1";
			sc += " delete from acc_refund_sub where 1=1";
*/
			sc += " update account set balance = 0, opening_balance=0 where 1=1";
//			sc += " delete from account_adjust_log where 1=1";
//			sc += " delete from accrecon where 1=1";
		//	sc += " delete from assets where 1=1";
		//	sc += " delete from assets_item where 1=1";
		//	sc += " delete from assets_payment where 1=1";
		//	sc += " delete from auto_expense where 1=1";
			sc += " delete from credit where 1=1";
			sc += " update card set balance=0, trans_total=0 where 1=1";
			sc += " delete from cat_cross where 1=1";
			sc += " delete from cat_cross_kit where 1=1";
			sc += " delete from custom_tax where 1=1";
			sc += " delete from custom_tax_log where 1=1";
			sc += " delete from custom_tax_sub where 1=1";
			sc += " delete from dispatch where 1=1";
			sc += " delete from expense where 1=1";
			sc += " delete from expense_item where 1=1";
			sc += " delete from flare where 1=1";
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
			sc += " delete from sales_kit where 1=1";
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
			sc += " delete from shelf where 1=1 ";
		//	sc += " delete from avg_cost_log where 1=1";

			sc += " delete from tran_deposit where 1=1";
			sc += " delete from tran_deposit_id where 1=1";
			sc += " delete from tran_detail where 1=1";
			sc += " delete from tran_invoice where 1=1";
			sc += " delete from trans where 1=1";
			sc += " delete from trans_other where 1=1";
			
		//	sc += " dbcc checkident (id, reseed, 1) ";
			sc += " dbcc checkident (credit, reseed, 1) ";
			sc += " dbcc checkident (shelf, reseed, 1) ";
		//	sc += " dbcc checkident (card, reseed, 4000)";
			sc += " dbcc checkident (custom_tax, reseed, 1)";
			sc += " dbcc checkident (custom_tax_log, reseed, 1)";
			sc += " dbcc checkident (custom_tax_sub, reseed, 1)";
			sc += " dbcc checkident (dispatch, reseed, 1)";
			sc += " dbcc checkident (expense, reseed, 1)";
			sc += " dbcc checkident (expense_item, reseed, 1)";
			sc += " dbcc checkident (flare, reseed, 1)";
			sc += " dbcc checkident (invoice, reseed, 10000)";
			sc += " dbcc checkident (invoice_freight, reseed, 1)";
			sc += " dbcc checkident (invoice_note, reseed, 1)";
			sc += " dbcc checkident (order_item, reseed, 1)";
			sc += " dbcc checkident (order_kit, reseed, 1)";
			sc += " dbcc checkident (orders, reseed, 2000)";
			sc += " dbcc checkident (purchase, reseed, 1)";
			sc += " dbcc checkident (purchase_item, reseed, 1)";
			sc += " dbcc checkident (ra_freight, reseed, 1)";
			sc += " dbcc checkident (ra_replaced, reseed, 1)";
			sc += " dbcc checkident (ra_statement, reseed, 1)";
			sc += " dbcc checkident (repair, reseed, 1)";;
			sc += " dbcc checkident (repair_log, reseed, 1)";
			sc += " dbcc checkident (return_sn, reseed, 1)";
			sc += " dbcc checkident (rma, reseed, 1)";
			sc += " dbcc checkident (rma_stock, reseed, 1)";
			sc += " dbcc checkident (sales, reseed, 1)";
			sc += " dbcc checkident (sales_cost, reseed, 1)";
			sc += " dbcc checkident (sales_kit, reseed, 1)";
			sc += " dbcc checkident (sales_serial, reseed, 1)";
			sc += " dbcc checkident (serial_trace, reseed, 1)";
			sc += " dbcc checkident (support_log, reseed, 1)";
			sc += " dbcc checkident (stock, reseed, 1)";
			sc += " dbcc checkident (stock_adj_log, reseed, 1)";
			sc += " dbcc checkident (stock_borrow, reseed, 1)";
			sc += " dbcc checkident (stock_borrow_sn, reseed, 1)";
			sc += " dbcc checkident (stock_cost, reseed, 1)";
			sc += " dbcc checkident (stock_loss, reseed, 1)";
			sc += " dbcc checkident (stock_qty, reseed, 1)";

			sc += " dbcc checkident (tran_deposit, reseed, 1)";
			sc += " dbcc checkident (tran_deposit_id, reseed, 1)";
			sc += " dbcc checkident (tran_detail, reseed, 1)";
			sc += " dbcc checkident (tran_invoice, reseed, 1)";
			sc += " dbcc checkident (trans, reseed, 1)";
			sc += " dbcc checkident (trans_other, reseed, 1)";	
			//sc += " delete from branch where id <> 1 ";
			//sc += " dbcc checkident (branch, reseed, 1)";	
			sc += " delete from currency where id not in(1,2) ";
			sc += " dbcc checkident (currency, reseed, 2)";	

		}
		if(bcleansupplier)
		{
			sc = " SET DATEFORMAT dmy ";
			sc += " UPDATE card SET balance = c1.balance - p.total_amount FROM purchase p JOIN card c1 ON c1.id = p.supplier_id where p.payment_status = 1 AND p.date_invoiced <='31/3/2005' ";
			sc += " UPDATE purchase set amount_paid = total_amount, payment_status = 2 ";
			sc += " WHERE payment_status = 1 AND date_invoiced <= '31/3/2005'  ";
			
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
			sc += " delete from kit where 1=1 ";
			//sc += " delete from kit_var where 1=1 ";
			//sc += " delete from kit_cache where 1=1 ";
			sc += " delete from kit_item where 1=1 ";
			sc += " delete from catalog where 1=1 ";
			sc += " delete from catalog_popular where 1=1 ";
			sc += " delete from catalog_temp where 1=1 ";
			sc += " delete from cat_seq where 1=1 ";
			sc += " delete from cat_cross where 1=1 ";
			sc += " delete from cat_cross_kit where 1=1 ";
			sc += " delete from stock_qty WHERE 1=1";
			sc += " delete from stock_adj_log WHERE 1=1";
			sc += " delete from stock_loss WHERE 1=1";
			sc += " delete from stock_borrow_sn WHERE 1=1";
			sc += " delete from stock_borrow WHERE 1=1";
			sc += " delete from stock_borrow_return WHERE 1=1";
			sc += " delete from stock_transfer_request WHERE 1=1";
			sc += " delete from stock_transfer_request_item WHERE 1=1";
			sc += " delete from specials_kit where 1=1";
			sc += " delete from specials where 1=1";
			sc += " delete from stock_qty where 1=1";
			sc += " delete from q_cat where 1=1";
			sc += " delete from q_flat where 1=1";
			sc += " delete from q_ram where 1=1";
			sc += " delete from q_sys where 1=1";
			sc += " delete from repair where 1=1";
			sc += " delete from repair_log where 1=1";
			sc += " delete from return_sn where 1=1";
			sc += " delete from rma where 1=1";
			sc += " delete from rma_stock where 1=1";
			sc += " delete from supplier_code_changed_log where 1=1";
			sc += " delete from dispatch where 1=1";	
			
			sc += " dbcc checkident (supplier_code_changed_log, reseed, 1)";
			sc += " dbcc checkident (dispatch, reseed, 1)";					

			sc += " UPDATE account SET opening_balance = 0 WHERE (class1 *1000) + (class2 * 100) + (class3 * 10) + (class4) = 1121 ";
			sc += " UPDATE account SET opening_balance = 0 WHERE (class1 *1000) + (class2 * 100) + (class3 * 10) + (class4) = 2111 ";

			sc += " dbcc checkident (account, reseed, 1)";
		}
		sc += " delete from doc_data WHERE 1=1 ";
		sc += " delete from doc_name WHERE 1=1 ";
		sc += " dbcc checkident (doc_name, reseed, 1) ";
		sc += " dbcc checkident (doc_data, reseed, 1) ";
		sc += " delete from dev_task WHERE 1=1 ";
		sc += " delete from dev_task_note WHERE 1=1 ";
		sc += " dbcc checkident (dev_task, reseed, 1) ";
		sc += " dbcc checkident (dev_task_note, reseed, 1) ";
//	-- clean up card list --
		if( bdelall || bdelcard)
		{
			sc += " delete from card where (type = 1 OR type = 2) AND (id <> 0 AND id <> 1) "; // type <> 4 and type <> 5  and type <> 3";
			sc += " delete from tourist where 1=1";			
			//sc += " dbcc checkident (card, reseed, 4000)";
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
		
//	DEBUG("sc = ", sc);
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
//	Response.Write("<input type=button value='Update Supplier' onclick=\"if(confirm('Are you Sure!!!')){window.location=('"+ Request.ServerVariables["URL"] +"?cl=supplier');}else{return false;}\">");
//	Response.Write("</td></tr><tr><td>");
	Response.Write("<br></td></tr></table><br>");
	Response.Write("</form>");
}
</script>

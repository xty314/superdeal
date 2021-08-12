<script runat=server>
string BuildInvoice(string sInvoiceNumber)
{
	DataSet dsi = new DataSet();
	string sc = "SELECT i.*, s.code, s.quantity, s.name as item_name, s.commit_price, ";
	sc += "s.status, s.shipby, s.ship_date, s.ticket, s.processed_by, s.system AS bSystem, i.system AS iSystem ";
	sc += "FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
	sc += "WHERE i.invoice_number=";
	sc += sInvoiceNumber;
//	sc += " AND email='" + email + "'";
	 
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	if(rows <= 0)
		return "<h3>Invoice# " + sInvoiceNumber + " Not Found</h3>";

	DataRow dr = dsi.Tables[0].Rows[0];

	if(dr["email"].ToString() != Session["email"].ToString() && !SecurityCheck("manager"))
		return "<h3>ACCESS DENIED</h3>";
	
	string sPaymentType = GetEnumValue("payment_method", dr["payment_type"].ToString());
//DEBUG("PaymentType = ", sPaymentType);

	string sDate = dr["commit_date"].ToString();
	DateTime tDate = DateTime.Parse(sDate);

	StringBuilder sb = new StringBuilder();

	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	sb.Append("<head><center><h2>");

	string status = dr["status"].ToString();
	if(status == "Payment Confirmed" || status == "Deliveried")
		sb.Append("Tax Invoice");
	else
		sb.Append("Order / Quotations");
DEBUG("Status = ", status);

	sb.Append("</h2></center></head>\r\n");
	sb.Append("<body>\r\n");

	sb.Append("<table width=100%><tr><td align=right>\r\n");

	sb.Append("<table width=100% border=0 cellpadding=0 cellspacing=0>\r\n");
	sb.Append("<tr><td>\r\n");

	sb.Append("<table border=1 bordercolor=black cellspacing=10 style=\"border-width:1px;border-style:Solid;border-collapse:collapse;fixed\"><tr><td>");
	sb.Append("<table border=0 cellpadding=0 cellspacing=0>\r\n");
	sb.Append("<tr><td colspan=2><font size=+1><b>EDEN COMPUTERS LTD</b></font></td></tr>\r\n");
	sb.Append("<tr><td colspan=2><b>www.edencomputer.co.nz &nbsp;&nbsp;&nbsp;sales@edencomputer.co.nz</b></td></tr>\r\n");
	sb.Append("<tr><td colspan=2 align=center>P.O.Box 8018, Symonds St</td></tr>\r\n");
	sb.Append("<tr><td>Mt Eden Head Office</td><td>City Branch</td></tr>\r\n");
	sb.Append("<tr><td>UNIT 6, 19 EDWIN ST &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td><td>Cnr Victoria & High St</td></tr>\r\n");
	sb.Append("<tr><td>MT EDEN</td><td>Auckland City</td></tr>\r\n");
	sb.Append("<tr><td>AUCKLAND</td><td>AUCKLAND</td></tr>\r\n");
	sb.Append("<tr><td>Tel: 09-6388188</td><td>Tel: 09-3070220</td></tr>\r\n");
	sb.Append("<tr><td>Fax: 09-6388778</td><td>Fax: 09-3073220</td></tr>\r\n");
	sb.Append("</table>\r\n");
	sb.Append("</td></tr></table>");
	
	sb.Append("</td><td align=right valign=bottom>\r\n");

	sb.Append("<table border=0 cellpadding=0 cellspacing=0><tr><td><b>GST#: </b><td align=right>72-443-950</td></tr>\r\n");
	sb.Append("<tr><td><b>");
	if(status == "Payment Confirmed" || status == "Deliveried")
		sb.Append("Invoice#: ");
	else
		sb.Append("Order Number: ");

	sb.Append("</b></td><td align=right>");
	sb.Append(sInvoiceNumber);
	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td><b>P.O.Number: </b></td><td>&nbsp;</td></tr>");
	sb.Append("<tr><td><b>Date: </b></td><td align=right>");
	sb.Append(tDate.ToString("dd/MM/yyyy"));
	sb.Append("</td></tr>\r\n");
	sb.Append("</table>\r\n");

	sb.Append("</td></tr></table>\r\n");

	sb.Append("</td></tr>\r\n");
	sb.Append("<tr><td height=1 bgcolor=red>&nbsp;</td></tr>\r\n");
	sb.Append("</table>\r\n");

	//start bill & ship to
	sb.Append("<table width=70%><tr><td>");
	//bill to
	sb.Append("<table><tr><td>");
	sb.Append("<b>Bill To: 1</b><br>\r\n");

	string sCompany = "";
	string sAddr = "";
	string sContact = "";

	if(dr["Address1B"].ToString() != "")
	{
		sAddr = dr["Address1B"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address2B"].ToString();
		sAddr += "<br>";
		sAddr += dr["CityB"].ToString();
	}
	else
	{
		sAddr = dr["Address1"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address2"].ToString();
		sAddr += "<br>";
		sAddr += dr["City"].ToString();
	}

	if(dr["CompanyB"].ToString() != "")
		sCompany = dr["CompanyB"].ToString();
	else
		sCompany = dr["Company"].ToString();

	if(sCompany == "")
	{
		if(dr["NameB"].ToString() != "")
			sCompany = dr["NameB"].ToString();
		else
			sCompany = dr["Name"].ToString();
	}
	else //if we have company name, then put contact person name here as well
	{
		if(dr["NameB"].ToString() != "")
			sContact = dr["NameB"].ToString();
		else
			sContact = dr["Name"].ToString();
	}

	sb.Append(sCompany);
	sb.Append("<br>\r\n");
	sb.Append(sAddr);
	sb.Append("<br>\r\n");
	sb.Append(dr["Email"].ToString());
	sb.Append("<br>\r\n");
//	sb.Append(dr["Phone"].ToString());
//	sb.Append("<br>\r\n");
//	if(sContact != "")
//	{
//		sb.Append(sContact);
//		sb.Append("<br>\r\n");
//	}

	sb.Append("</td></tr></table></td><td align=right><table><tr><td>");

	//ship to 
	sb.Append("<b>Ship To:</b><br>\r\n");

	if(dr["Address1"].ToString() != "")
	{
		sAddr = dr["Address1"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address2"].ToString();
		sAddr += "<br>";
		sAddr += dr["City"].ToString();
	}
	else
	{
		sAddr = dr["Address1B"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address2B"].ToString();
		sAddr += "<br>";
		sAddr += dr["CityB"].ToString();
	}

	if(dr["Company"].ToString() != "")
		sCompany = dr["Company"].ToString();
	else
		sCompany = dr["CompanyB"].ToString();

	if(sCompany == "")
	{
		if(dr["Name"].ToString() != "")
			sCompany = dr["Name"].ToString();
		else
			sCompany = dr["NameB"].ToString();
	}
	else //if we have company name, then put contact person name here as well
	{
		if(dr["Name"].ToString() != "")
			sContact = dr["Name"].ToString();
		else
			sContact = dr["NameB"].ToString();
	}

	sb.Append(sCompany);
	sb.Append("<br>\r\n");
	sb.Append(sAddr);
	sb.Append("<br>\r\n");
	sb.Append(dr["Email"].ToString());
	sb.Append("<br>\r\n");
	sb.Append("</td></tr></table>");
	//end of ship to

	sb.Append("</td></tr></table>");
	//end of bill and shipto table

	sb.Append(BuildItemTable(dsi.Tables[0]));

	sb.Append("<br>");

	//payment form
	sb.Append("Your form of payment is ");
	sb.Append("<table cellpadding=0 cellspacing=0>");

	if(status == "Payment Confirmed" || status == "Deliveried")
	{
		sb.Append("<tr><td><b>" + sPaymentType + "</b> - Payment confirmed.</td></tr>");
		if(status == "Deliveried")
		{
//			sb.Append("<tr><td>&nbsp;</td></tr>");
			sb.Append("<tr><td><b>Package Deliverid</b></td></tr>");
			sb.Append("<tr><td><table cellpadding=0 cellspacing=0>");
			sb.Append("<tr><td>Time Deliveried &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</td><td>");
			sb.Append(DateTime.Parse(dr["ship_date"].ToString()).ToString("f"));
			sb.Append("</td><tr><tr><td>Deliveried by </td><td>");
			sb.Append(dr["shipby"].ToString());
			sb.Append("</td></tr><tr><td>Ticket Number </td><td>");
			sb.Append(dr["ticket"].ToString());
			sb.Append("</td></tr><tr><td>Processed by </td><td>");
			sb.Append(dr["processed_by"].ToString());
			sb.Append("</td></tr></table></td></tr>");
		}
	}
	else
	{
		sb.Append("<tr><td colspan=2 bgcolor=#EEEEEE><b>" + sPaymentType + "</b></td></tr>");
//		sb.Append("<tr><td>&nbsp;</td></tr>");
		if(sPaymentType == "BankDeposit")
		{
			sb.Append("<tr><td colspan=2>Please deposit money into:</td></tr>");
			sb.Append("<tr><td>Bank: </td><td>ASB BANK</td></tr>");
			sb.Append("<tr><td>Branch: </td><td>Mt Eden Branch</td></tr>");
			sb.Append("<tr><td>Account Name: </td><td>EDEN COMPUTERS LIMITED</td></tr>");
			sb.Append("<tr><td>Account Number: </td><td>12-3048-0348683-00</td></tr>");
			sb.Append("<tr><td>&nbsp;</td></tr>");
			sb.Append("<tr><td colspan=2>");
			sb.Append("To ensure the fastest possible processing of your order, please use your order number as a reference word, on the deposit slip/internet transfer. Your order number and our bank account details will email to you once you finished placing the order.");
			sb.Append("</td></tr>");
		}
		else if(sPaymentType == "Cheque")
		{
			sb.Append("<tr><td colspan=2>Please send cheque to:</td></tr>");
			sb.Append("<tr><td>Eden Computers Ltd. P.O.Box 8018 Symonds St Auckland</td></tr>");
//			sb.Append("<tr><td>P.O.Box 8018 Symonds St Auckland</td></tr>");
//			sb.Append("<tr><td>Symonds St</td></tr>");
//			sb.Append("<tr><td>Auckland</td></tr>");
			sb.Append("<tr><td>&nbsp;</td></tr>");
			sb.Append("<tr><td colspan=2>");
			sb.Append("To ensure the fastest possible processing of your order, please use your order number as a reference word on the cheque. Your order number will email to you once you finished placing the order.");
			sb.Append("</td></tr>");
		}
	}
	sb.Append("</table>");

//	sb.Append("<br><br><br>");
	
	//conditions
	sb.Append("<table border=1 bordercolor=black cellspacing=10 style=\"border-width:1px;border-style:Solid;border-collapse:collapse;fixed\"><tr><td>");
	sb.Append("<table border=0 cellpadding=0 cellspacing=0>\r\n");
	sb.Append("<tr><td><b>Conditions of Sales:</b></td></tr>\r\n");
	sb.Append("<tr><td>1. All parts come with one year RTB warranty.</td></tr>\r\n");
	sb.Append("<tr><td>2. All computer systems carry 2 years Hardware & 5 years Labour RTB warranty.</td></tr>\r\n");
	sb.Append("<tr><td>&nbsp;&nbsp;&nbsp;Keyboard, Mouse, Speaker, Printer, Scanner come with one year warranty.</td></tr>\r\n");
	sb.Append("<tr><td>3. Original purchase invoice must be presented with warranty claims.</td></tr>\r\n");
	sb.Append("<tr><td>4. The above goods belong to EDEN COMPUTERS LTD until paid in full.</td></tr>\r\n");
	sb.Append("<tr><td>5. The warranty period is longer if so provided by manufacturer.</td></tr>\r\n");
	sb.Append("<tr><td>6. Removal of labels, tampering or similar unauthorised use, voids the warranty.</td></tr>\r\n");
	sb.Append("<tr><td>7. All goods returned are subject to a minimum 20% restocking fee.</td></tr>\r\n");
	sb.Append("<tr><td>8. Software problems are not covered by warranty.</td></tr>\r\n");
	sb.Append("</td></tr></table></td></tr></table>\r\n");

//	sb.Append("<div align=right>EDEN COMPUTERS LIMITED</div><hr width=30% align=right>\r\n");
//	sb.Append("<br><br>E.O.F.\r\n");
	sb.Append("</body></html>");

	return sb.ToString();
}

string BuildItemTable(DataTable dt)
{
	bool bSystem = bool.Parse(dt.Rows[0]["iSystem"].ToString());

	StringBuilder sb = new StringBuilder();

	sb.Append("<br>\r\n\r\n");
	sb.Append("<table cellpadding=0 cellspacing=0 width=100%");
	if(bSystem)
		sb.Append(" bgcolor=yellow><tr><td><b>SYSTEM</b></td></tr><tr>");
	else
	{
		sb.Append(">\r\n");
		sb.Append("<tr style=\"color:black;background-color:#EEEEEE;font-weight:bold;\">");
	}
	sb.Append("<td width=70>PART#</td>\r\n");
	sb.Append("<td>DESCRIPTION</td>\r\n");
	sb.Append("<td width=70 align=right>PRICE</td>\r\n");
	sb.Append("<td width=40 align=right>QTY</td>\r\n");
	sb.Append("<td width=70 align=right>AMOUNT</td></tr>\r\n");
	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	//build up list
	int i = 0;
	int j = 0;
	for(i=0; i<dt.Rows.Count; i++)
	{
		if(bSystem)
		{
			bool bSys = bool.Parse(dt.Rows[i]["bsystem"].ToString());
//			DEBUG("bSys=" + dt.Rows[i]["bsystem"].ToString(), " name=" + dt.Rows[i]["item_name"].ToString());
			if(!bSys)
				continue;
		}
		sb.Append(InvoicePrintOneRow(dt.Rows[i]));
		j++;
	}
//DEBUG("rows="+dt.Rows.Count, " j="+j);
	if(bSystem && j < dt.Rows.Count)
	{
		sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");
		sb.Append("</table><br>");
		sb.Append("<table cellpadding=0 cellspacing=0 width=100%>\r\n");
		sb.Append("<tr><td colspan=4><b>OTHER PARTS</b></td></tr>");
		sb.Append("<tr style=\"color:black;background-color:#FFFFFF;font-weight:bold;\">");
		sb.Append("<td width=70>PART#</td>\r\n");
		sb.Append("<td>DESCRIPTION</td>\r\n");
		sb.Append("<td width=70 align=right>PRICE</td>\r\n");
		sb.Append("<td width=40 align=right>QTY</td>\r\n");
		sb.Append("<td width=70 align=right>AMOUNT</td></tr>\r\n");

		sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

		//build up list
		for(i=0; i<dt.Rows.Count; i++)
		{
			if(bool.Parse(dt.Rows[i]["bsystem"].ToString()))
				continue;
			sb.Append(InvoicePrintOneRow(dt.Rows[i]));
		}
	}

	double dTotal = double.Parse(dt.Rows[0]["price"].ToString(), NumberStyles.Currency, null);
	double dTAX = double.Parse(dt.Rows[0]["GST"].ToString(), NumberStyles.Currency, null);
	double dAmount = double.Parse(dt.Rows[0]["total"].ToString(), NumberStyles.Currency, null);

	sb.Append("<tr><td colspan=5>&nbsp;</td></tr>\r\n");
	sb.Append("<tr><td colspan=5><hr></td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>Sub-Total:</b></td><td align=right>");
	sb.Append(dTotal.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>TAX:</b></td><td align=right>");
	sb.Append(dTAX.ToString("c"));
	sb.Append("</td></tr>\r\n");

	//shipping info
	sb.Append("<tr><td colspan=4 align=right valign=top nowrap>");
	sb.Append("<b>SHIPPING BY ");
	sb.Append(dt.Rows[0]["shipby"].ToString());
	sb.Append("</b></td><td align=right>");
	string shipping_fee = dt.Rows[0]["shipping_fee"].ToString();
	if(shipping_fee == "5")
		sb.Append("$5.00");
	else if(shipping_fee == "10")
		sb.Append("$10.00");
	else
		sb.Append("$0.00");
	sb.Append("</b></font></td></tr>");


	sb.Append("<tr><td>&nbsp;</td><td>&nbsp;</td><td colspan=2 align=right><b>TOTAL:</b></td><td align=right>");
	sb.Append(dAmount.ToString("c"));
	sb.Append("</td></tr></table>\r\n");

	return sb.ToString();
}

string InvoicePrintOneRow(DataRow dr)
{
	double dPrice = double.Parse(dr["commit_price"].ToString(), NumberStyles.Currency, null);
	int quantity = int.Parse(dr["quantity"].ToString());
	double dTotal = dPrice * quantity;

	StringBuilder sb = new StringBuilder();

	sb.Append("<tr><td>");
	sb.Append(dr["code"].ToString());
	sb.Append("</td><td>");
	sb.Append(dr["item_name"].ToString());
	sb.Append("</td><td align=right>");
	sb.Append(dr["commit_price"].ToString());
	sb.Append("</td><td align=right>");
	sb.Append(dr["quantity"].ToString());
	sb.Append("</td><td align=right>");
	sb.Append(dTotal.ToString("c"));
	sb.Append("</td></tr>\r\n");

	return sb.ToString();
}
</script>

<script runat=server>

double m_dOrderTotal = 0;
bool m_bDoSN = false;
DataSet dsi = new DataSet();
DataSet dstt = new DataSet();

string m_sInvType = "";

bool m_bShowPayment = true;
double dNormalTotal =0;
double m_invoice_total = 0;
double rrpTotal = 0;
double netTotal = 0;
int m_nItems = 0;
int m_nRowsToBottom = 25;

string m_account_number = "";
string m_account_term = "";
string m_account_name = "";

string m_inv_sNumber = "";
string m_itemCols = "12"; //item table columns

string m_shippingAddress = "";
string m_billingAddress = "";
string stylesheet = "<STYLE> H1 {page-break-before: always}</STYLE>"; 
bool m_bDisplayServiceItemWithNoPrice = false;
int m_nTotalInvoiceRows = 20;
string m_branchHeader = "";
string m_branchFooter = "";
bool m_bDoDisplaySNonInvoice = false;
int m_nItemCounter = 0;
bool m_bPrintHeaderInEachPage = true;
bool m_bPrintFooterInEachPage = true;
bool m_bEnablePrintOverDueStatement = true;
string m_credit_termID = "";

string InvoicePrintHeader(string sType, string sNumber, string sDate)
{
	return InvoicePrintHeader(sType, "", sNumber, sDate, "");
}

string InvoicePrintHeader(string sType, string sSales, string sNumber, string sDate)
{
	return InvoicePrintHeader(sType, sSales, sNumber, sDate, "");
}

string InvoicePrintHeader(string sType, string sSales, string sNumber, string sDate, string sPO_number)
{
	return InvoicePrintHeader(sType, sSales, sNumber, sDate, sPO_number, "", "");
}

string InvoicePrintHeader(string sType, string sSales, string sNumber, string sDate, string sPO_number, string card_id, string card_name)
{
	return InvoicePrintHeader(sType, sSales, sNumber, sDate, sPO_number, card_id, card_name, "");
}

string InvoicePrintHeader(string sType, string sSales, string sNumber, string sDate, string sPO_number, string card_id, string card_name, string supplier_invoice)
{
	string Gcompanyname = GetSiteSettings("company_name");
	string header = ReadSitePage("invoice_header");
	       header = header.Replace("@@companyname", Gcompanyname);
	try
	{
		m_bDisplayServiceItemWithNoPrice = MyBooleanParse(GetSiteSettings("set_invoice_no_pricing_for_service_item", "0", true));
	}
	catch (Exception e)
	{
	}
	
	m_sInvType = sType;

	m_bDoSN = (sType == "invoice");

	string title = sType.ToUpper(); 

	if(m_bOrder)
		title = "PURCHASE ";
	if(sType == "invoice")
		title = "TAX INVOICE";
	if(sType.ToLower() == "quote")
		title = "QUOTE";
	if(sType.ToLower() == "order")
		title = "SALES ORDER";

	header = header.Replace("@@title", title);

	string sTickets = "";
	string agent_name = GetAgentName(sNumber);
	//header = header.Replace("@@ticket", sTickets);

	StringBuilder sb = new StringBuilder();
	if(!m_bOrder)
	{
		sType = sType.ToUpper();
		sb.Append("<tr><td align=right><b>");
		sb.Append(sType + " Number : &nbsp;");
		sb.Append("</b></td><td>" + sNumber + "</td></tr>");

		if(sSales != "")
		{
			sb.Append("<tr><td align=right><b>Sales : &nbsp;</b></td><td>");
			sb.Append(sSales);
			sb.Append("</td></tr>");
		}
		sb.Append("<tr><td align=right><b>P.O.Number : &nbsp;</b></td><td>");
		sb.Append(sPO_number);
		sb.Append("</td></tr>");

			header = header.Replace("@@PONUMBER", sPO_number);
		header = header.Replace("@@SALES", sSales);
		header = header.Replace("@@ORDERTYPE", sType);
		header = header.Replace("@@ORDERNUMBER", sNumber);		
	}
	else
	{
		header = header.Replace("@@PONUMBER", sNumber);
		header = header.Replace("@@SALES", "");
		header = header.Replace("@@ORDERTYPE", "Supplier");
		header = header.Replace("@@ORDERNUMBER", supplier_invoice);
		
		sb.Append("<tr><td align=right><b>");
		sb.Append("P.O. Number : &nbsp;");
		sb.Append("</b></td><td>");
		sb.Append(sNumber);
		sb.Append("</td></tr>");

		sb.Append("<tr><td align=right><b>");
		sb.Append("Supplier INV# : &nbsp;");
		sb.Append("</b></td><td>");
		sb.Append(supplier_invoice);
		sb.Append("</td></tr>");
	}

	if(m_account_number == "")
	{
		m_account_number = card_id;
		if(Request.QueryString["t"] == "order")
			m_account_number = sSales;
		m_account_name = card_name;
	}
	if(title.IndexOf("PURCHASE") >= 0)
		m_account_number = m_account_name;
	if(title.IndexOf("SALES ORDER") >= 0) // CH 9-2-2011 UPDATED AS CUSTOME REQUIRED, NO HEADER TITLE ON SALES ORDER
		m_branchHeader = "";
    m_branchHeader = ReadBranchHeader(m_branchHeader);
	header = header.Replace("@@BRANCH_HEADER", m_branchHeader); 
	header = header.Replace("@@BRANCH_FOOTER", m_branchFooter); 
	header = header.Replace("@@agent_name", agent_name);
	header = header.Replace("@@account_number", m_account_number);
	header = header.Replace("@@account_term", m_account_term);
	header = header.Replace("@@invoice_number", sb.ToString());
	header = header.Replace("@@date", sDate);
	header = header.Replace("@@shipping_address", m_shippingAddress);
	header = header.Replace("@@billing_address", m_billingAddress);
	return header;
}

string GetAgentName(string sinvoice)
{
	string agent = "";
	string sc = "SELECT c.name AS agent ";
		   sc += " FROM invoice i left outer join card c ON c.id = i.agent ";
		   sc += " WHERE invoice_number ="+ sinvoice;
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dstt, "agent") > 0)
			agent = dstt.Tables["agent"].Rows[0][0].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return agent;
}
bool DoGetTicketNo(string sInvNo)
{
	string sc = "SELECT ship_name, ship_desc, price , ticket";
		   sc += " FROM invoice_freight WHERE invoice_number=" + sInvNo;
//		   sc += " GROUP BY ship_name, ship_desc, price, ticket";
//DEBUG("sc = ", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dstt, "tickets");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

string InvoicePrintShip(DataRow dr, string shipto)
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<table width=75% border=0><tr><td width=" + GetSiteSettings("envelope_left_margin", "10") + ">&nbsp;</td>");
	sb.Append("<td valign=top>");

	//bill to
	sb.Append("<table border=0><tr><td>");
	sb.Append("<b><font size=2>Bill To:</b><br>\r\n");
	if(dr == null)
	{
		sb.Append("<font size=2>Cash Sales<br><br>");
		sb.Append("</td></tr></table>\r\n");
		sb.Append("</td></tr></table>\r\n");
		m_billingAddress = sb.ToString();
		return sb.ToString();
	}
	
	string sCompany = "";
	string sAddr = "";
	string sContact = "";

	if(dr["postal1"].ToString() != "")
	{
		sAddr = "<font size=2>"+ dr["postal1"].ToString();
		sAddr += "<br>";
		sAddr += dr["postal2"].ToString();
		sAddr += "<br>";
		sAddr += dr["postal3"].ToString();
	}
	else
	{
		sAddr = "<font size=2>"+ dr["Address1"].ToString();
		sAddr += "<br>";
		sAddr += dr["Address2"].ToString();
		sAddr += "<br>";
		sAddr += dr["address3"].ToString();
	}

	if(dr["trading_name"].ToString() != null && dr["trading_name"].ToString() != "")
		sCompany = "<font size=2>"+ dr["trading_name"].ToString() + "<br>\r\n";

	if(sCompany == "" || sCompany == null)
	{		
//		sCompany += "<font size=2>"+ dr["company"].ToString() + "<br>\r\n";		
		sCompany += "<font size=2>"+ dr["Name"].ToString()  + "</font><br>\r\n";	
	}
	else //if we have company name, then put contact person name here as well
	{
//		if(String.Compare(dr["trading_name"].ToString(), dr["company"].ToString(), true) != 0)
//			sCompany += "<font size=2>"+ dr["company"].ToString() + "<br>\r\n";		
		sContact += "<font size=2>"+ dr["Name"].ToString()  + "<br>\r\n";
		//sCompany += "<font size=2>"+ dr["Name"].ToString()  + "<br>\r\n";
	}	
	m_billingAddress += "<span style=\"font:bold 15px arial; color:#000000\">"+ sCompany + "</span>";
	m_billingAddress += sAddr + "<br>";
	
	sb.Append(sCompany);
	sb.Append("<br>");
	sb.Append(sAddr);
	sb.Append("<br>");
//	sb.Append(dr["Email"].ToString());
//	sb.Append("<br>\r\n");
	if(dr["phone"].ToString() != null && dr["phone"].ToString() != "")
	{
		sb.Append("Ph : "+dr["phone"].ToString());
		m_billingAddress += "Ph: "+ dr["phone"].ToString() + "<br>";
	}
	if(dr["fax"].ToString() != null && dr["fax"].ToString() != "")
	{
		sb.Append("Fax: " + dr["fax"].ToString());
		m_billingAddress += "Fax: "+ dr["fax"].ToString() + "<br>";
	}

	sb.Append("</td></tr></table></td><td valign=top align=right><table border=0><tr><td>");

	//ship to, if "not pick up  --- 1"
	if(dr["shipping_method"].ToString() != "1")
	{
		sb.Append("<b><font size=2>Ship To:</font></b><br>\r\n");
		if(bool.Parse(dr["special_shipto"].ToString()))
		{
			sb.Append("<font size=2>" + dr["shipto"].ToString().Replace("\r\n", "\r\n<br>") + "</font>");
			m_shippingAddress = "<font size=2>" + dr["shipto"].ToString().Replace("\r\n", "\r\n<br>") + "</font>";
		}
		else
		{
			if(dr["Address1"].ToString() != "")
			{
				sAddr = dr["Address1"].ToString();
				sAddr += "<br>";
				sAddr += dr["Address2"].ToString();
				sAddr += "<br>";
				sAddr += dr["address3"].ToString();
			}
			else
			{
				sAddr = dr["postal1"].ToString();
				sAddr += "<br>";
				sAddr += dr["postal2"].ToString();
				sAddr += "<br>";
				sAddr += dr["postal3"].ToString();
			}

			sb.Append("<font size=2>" + sCompany + "</font>");
			sb.Append("<br>\r\n");
			sb.Append("<font size=2>" + sAddr + "</font>");
			sb.Append("<br>\r\n");
	//		sb.Append(dr["Email"].ToString());
	//		sb.Append("<br>\r\n");
			m_shippingAddress += sCompany + "<br>";
			m_shippingAddress += sAddr + "<br>";
			if(dr["phone"].ToString() != null && dr["phone"].ToString() != "")
			{
				sb.Append("<font size=2>Ph: " + dr["phone"].ToString() + "</font>");
				sb.Append("<br>\r\n");
				m_shippingAddress += "Ph: "+ dr["phone"].ToString()  + "<br>";
			}
			if(dr["fax"].ToString() != null && dr["fax"].ToString() != "")
			{
				sb.Append("<font size=2>Ph: " + dr["fax"].ToString() + "</font>");
				sb.Append("<br>\r\n");
				m_shippingAddress += "Fax: "+ dr["fax"].ToString() + "<br>";
			}
			if(dr["name"].ToString() != "")
			{
				sb.Append("<font size=2>ATTN : " + dr["name"].ToString() + "</font>");
				sb.Append("<br>\r\n");
				m_shippingAddress += "ATTN: " + dr["name"].ToString() + "<br>";
			}
						
		}
	}
	
	sb.Append("</td></tr></table>\r\n");
	//end of ship to

	sb.Append("</td></tr></table>\r\n");
	return sb.ToString();
}

string InvoicePrintBottom()
{
	return InvoicePrintBottom("");
}

string InvoicePrintBottom(String sComment)
{
	StringBuilder sb = new StringBuilder();
	string bottom = ReadSitePage("invoice_footer");
	// Note/Comment table
	/*if(sComment != "" && sComment != null)
	{
		sb.Append("<br>  Comment :<br>");
		sb.Append(sComment.Replace("\r\n", "\r\n<br>"));
	}
	
	bottom = bottom.Replace("@@comment", sb.ToString());
	*/
	bottom = bottom.Replace("@@comment", sComment);
	bottom = bottom.Replace("@@BRANCH_FOOTER", m_branchFooter);
	//for(int i=m_nRowsToBottom; i>m_nItems; i--)
	//	sb.Append("<br>");
	
	return bottom;
}

string check_IsNumber(string s_text)
{
	bool isNum = true;
	if(s_text == null)
		return "false";
	int ptr = 0;
	while (ptr < s_text.Length)
	{
		if (!char.IsDigit(s_text, ptr++))
		{
			isNum = false;
			break;
		}
	}
	return isNum.ToString();
}

string BuildInvoice(string sInvoiceNumber)
{
	m_inv_sNumber = sInvoiceNumber;
	string m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);
	m_bDoDisplaySNonInvoice = MyBooleanParse(GetSiteSettings("set_serial_number_display_on_invoice", "0", true));
	m_nTotalInvoiceRows = int.Parse(GetSiteSettings("set_total_invoice_item_rows_for_invoice_printout", "20"));
	m_bEnablePrintOverDueStatement = MyBooleanParse(GetSiteSettings("enable_print_overdue_statement", "0", true));
	string CheckDigit = check_IsNumber(sInvoiceNumber);
	if(CheckDigit == "False")
		return "<h3>Invoice# " + sInvoiceNumber + " Not Found</h3>";

	DataSet dsi = new DataSet();
	string sc = "SELECT s.discount_percent, i.customer_gst, b.branch_header, b.branch_footer, i.invoice_number, i.type, i.payment_type, i.commit_date, i.price ";
	sc += ", i.tax, i.total, i.sales, i.cust_ponumber, cr.stock_location, ";
	sc += " i.freight, i.sales_note, c.*, s.code, s.supplier_code, s.quantity, s.name as item_name, cr.brand, cr.price1, ISNULL(cr.level_price0*cr.level_rate1/100,0) AS rrp, i.branch ";
	sc += ", s.commit_price, s.normal_price, i.type, i.special_shipto, i.shipto, shipping_method, ";
	sc += " s.status AS si_status, s.note, s.shipby, s.ship_date, s.ticket, s.processed_by ";
	sc += ", s.system AS bSystem, i.system AS iSystem, ISNULL(cr.is_service,0) AS is_service, i.no_individual_price AS iNoIndividual ";
	sc += ", s.kit, s.krid, k.kit_id, k.qty AS kit_qty, k.name AS kit_name, k.commit_price AS kit_price, s.pack ";	
	sc += ", '' AS shelf ";
	sc += " FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number LEFT OUTER JOIN card c ON c.id=i.card_id ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = s.code ";	
	sc += " LEFT OUTER JOIN sales_kit k ON k.invoice_number=i.invoice_number AND k.krid = s.krid ";
	sc += " LEFT OUTER JOIN branch b ON b.id = i.branch ";
	sc += " WHERE i.invoice_number=";
	sc += sInvoiceNumber;
	if(!SecurityCheck("sales", false))
		sc += " AND i.card_id=" + Session["card_id"];
	//sc += " ORDER BY s.pack ";
	sc += " ORDER BY s.supplier_code ";
//DEBUG("sc = ", sc);
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
	m_account_number = dr["id"].ToString();
	m_account_term = dr["credit_term"].ToString();
	m_credit_termID = m_account_term;

	if(m_account_term != "")
		m_account_term = GetEnumValue("credit_terms", m_account_term);

	m_account_name = dr["trading_name"].ToString();
	m_branchHeader = dr["branch_header"].ToString();
	m_branchFooter = dr["branch_footer"].ToString();
	
	string sPaymentType = GetEnumValue("payment_method", dr["payment_type"].ToString());
	string sDate = dr["commit_date"].ToString();
	DateTime tDate = DateTime.Parse(sDate);
	string status = dr["si_status"].ToString();
	string sType = GetEnumValue("receipt_type", dr["type"].ToString());
	if(status == "Back Order")
		sType = status;

	string sales = dr["sales"].ToString();
	string po_number = dr["cust_ponumber"].ToString();
     
	StringBuilder sb = new StringBuilder();

	sb.Append("<html><header><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	Response.Write(stylesheet +"</header>");
	string th1 = "<table width=100% align=center valign=center cellspacing=1 cellpadding=1 border=1 bordercolor=#EEEEEE bgcolor=white";
	th1 += " style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">";
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	InvoicePrintShip(dr, "");	
	sb.Append(InvoicePrintHeader(sType, sales, sInvoiceNumber, tDate.ToString("dd/MM/yyyy"), po_number));
	sb.Append("<hr>");
	sb.Append(BuildItemTable(dsi.Tables[0], false, sb.ToString(),InvoicePrintBottom(dr["sales_note"].ToString()) ));
	sb.Append("</body></html>");
	return sb.ToString();
}

string GetError(string sInvoiceNumber)
{
	int rows = 0;
	DataSet dserr = new DataSet();
	string sc = "SELECT trans_failed_reason, debug_info FROM invoice WHERE invoice_number=" + sInvoiceNumber;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dserr);
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	if(rows < 0)
		return "";
	string failed_reason = dserr.Tables[0].Rows[0]["trans_failed_reason"].ToString();
	string debug_info = dserr.Tables[0].Rows[0]["debug_info"].ToString();
	string sRet = "<br><b>failed Reason : </b><br>" + failed_reason;
	if(m_sSite == "admin")
		sRet += "<br><br><b>Debug Info : </b><br>" + debug_info;
	return sRet;
}
string BuildItemTable(DataTable dt, bool bOrder)
{
	return BuildItemTable(dt, bOrder, "", "");
}
string BuildItemTable(DataTable dt, bool bOrder, string sHeader, string sFooter)
{
	string m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);

	bool bSystem = false;
	bool bIndividual_Price = false;
	bSystem = bool.Parse(dt.Rows[0]["iSystem"].ToString());
	bIndividual_Price = bool.Parse(dt.Rows[0]["iNoIndividual"].ToString()); //tee for display price when no individual price is untick
	
	StringBuilder sb = new StringBuilder();

	sb.Append("<br>\r\n\r\n");
	sb.Append("<table cellpadding=1 cellspacing=1 border=0 bordercolorlight=#EEEEE width=100%");
	sb.Append(" style=\"font-family:Verdana;font-size:7pt;border-width:0px;border-style:solid;border-collapse:collapse;fixed\" ");
	if(bSystem)
		sb.Append(" bgcolor=#FFFFEE><tr><td><b>SYSTEM</b></td></tr><tr>");
	else
	{
		sb.Append(">\r\n");
		sb.Append("<tr style=\"color:black;background-color:#EEEEEE;font-weight:bold;\">");
	}
	sb.Append("<td width=1%>&nbsp;</td>");
	//sb.Append("<td width=6%><b>Box</b></td>\r\n");
    sb.Append("<td width=6%></td>\r\n");
	if(bOrder)
	{
//		sb.Append("<td width=6%><b>EXH</b></td>\r\n");
//		sb.Append("<td width=6%><b>STO </b></td>\r\n");
        sb.Append("<td width=6%><b>Location</b></td>\r\n");
	}
	sb.Append("<td width=8%><b>Code</td>\r\n");
	if(bOrder)
		sb.Append("<td width=12%><b>Picture</b></td>\r\n");
	sb.Append("<td width=25%><b>DESCRIPTION</td>\r\n");
	if(bIndividual_Price)
	{		
		sb.Append("<td width=8% align=right>&nbsp;</td>\r\n");
        sb.Append("<td width=6% align=right>&nbsp;</td>\r\n");
        sb.Append("<td width=8% align=right>&nbsp;</td>\r\n");
		//sb.Append("<td width=8% align=right><b>DISCOUNT(%)</td>\r\n");
		sb.Append("<td width=6% align=right><b>QTY</td>\r\n");
		sb.Append("<td width=8% align=right>&nbsp;</td></tr>\r\n");
	}
	else
	{   
		sb.Append("<td width=8% align=right><b>RRP</td>\r\n");
		sb.Append("<td width=6% align=right><b>DISCOUNT</td>\r\n");
        sb.Append("<td width=8% align=right><b>NET PRICE</td>\r\n");
		sb.Append("<td width=6% align=right><b>QTY</td>\r\n");
		sb.Append("<td width=8% align=right><b>AMOUNT</td></tr>\r\n");
	}

	sb.Append("<tr><td colspan=" + m_itemCols + "><hr></td></tr>\r\n");
	string itemTitle = sb.ToString();
	int i = 0;
	int j = 0;
	string kit_id = "";
	string kit_id_old = "";
	int nCountRows = 0;
    double realSubTotal = 0;
	for(i=0; i<dt.Rows.Count; i++)
	{	
		m_nItemCounter++;

		bool bService = bool.Parse(dt.Rows[i]["is_service"].ToString());
		if(!m_bDisplayServiceItemWithNoPrice)
		{
			if(bService)
				continue; //put them to the end
		}
		if(MyBooleanParse(dt.Rows[i]["kit"].ToString()))
		{
			kit_id = dt.Rows[i]["kit_id"].ToString();
			string kit_name = dt.Rows[i]["kit_name"].ToString();
			string kit_qty = dt.Rows[i]["kit_qty"].ToString();
			double dKitPrice = MyDoubleParse(dt.Rows[i]["kit_price"].ToString());
			double dKitTotal = dKitPrice * MyDoubleParse(kit_qty);
			if(kit_id != kit_id_old)
			{
				sb.Append("<tr bgcolor=aliceblue><td></td>");
				sb.Append("<td nowrap>" + m_sKitTerm + "#" + kit_id + " &nbsp&nbsp;</td>");
				sb.Append("<td>&nbsp;</td>"); //m_pn
				sb.Append("<td>" + kit_name + "</td>");
				sb.Append("<td>&nbsp;</td>");
				sb.Append("<td align=right>" + dKitPrice.ToString("c") + "</td>");
				sb.Append("<td width=8% align=right></td>\r\n");
				sb.Append("<td align=right>" + kit_qty + "</td>");
				sb.Append("<td align=right>" + dKitTotal.ToString("c") + "</td></tr>");
				kit_id_old = kit_id;
			}
			sb.Append(InvoicePrintOneRow(dt.Rows[i], bSystem, bIndividual_Price, true, bOrder));
		}
		else
		{
			sb.Append(InvoicePrintOneRow(dt.Rows[i], bSystem, bIndividual_Price, bOrder));
		}
		j++;
        string realSaleQty = dt.Rows[i]["quantity"].ToString();
        string realSaleDiscount = dt.Rows[i]["discount_percent"].ToString();
        double realSalePrice = MyDoubleParse(dt.Rows[i]["commit_price"].ToString());
        realSalePrice = realSalePrice * (1-MyDoubleParse(realSaleDiscount)/100); 
        realSubTotal += MyDoubleParse(realSaleQty) * realSalePrice;
    }

	if(!m_bDisplayServiceItemWithNoPrice)
	{
		for(i=0; i<dt.Rows.Count; i++)
		{
			if(bSystem)
			{
				bool bSys = bool.Parse(dt.Rows[i]["bsystem"].ToString());
				if(!bSys)
					continue;
			}
			bool bService = bool.Parse(dt.Rows[i]["is_service"].ToString());
		
			if(!bService)
				continue; //put them to the end
			sb.Append(InvoicePrintOneRow(dt.Rows[i], bSystem, bIndividual_Price, bOrder));
		}
	}

	m_nItems = j; 
	double dTotal = 0;
	double dTAX = 0;
	double dAmount = 0;
	double dNormalAmount =0;
	double dSaveAmount = 0;
	double dDiscountpercent = 0;
	double dGST = 0;
	double dFreight = MyDoubleParse(dt.Rows[0]["freight"].ToString());

	if(bOrder)
	{
		dTotal = m_dOrderTotal;		
		dGST = MyDoubleParse(dt.Rows[0]["customer_gst"].ToString());	
		
		if(dt.Rows[0]["quote_total"].ToString() != "" && Math.Round(MyDoubleParse(dt.Rows[0]["quote_total"].ToString()),0) != 0)
			dTotal = double.Parse(dt.Rows[0]["quote_total"].ToString(), NumberStyles.Currency, null);	
		dTAX = (dTotal + dFreight) * dGST;							//Modified by NEO
		dTAX = Math.Round(dTAX, 2);
		dAmount = dTotal + dFreight + dTAX;
		dSaveAmount = dNormalTotal - dTotal;
	    dDiscountpercent = (dSaveAmount/(dAmount+dSaveAmount))*100;
	    dDiscountpercent  = Math.Round(dDiscountpercent,2);
	}
	else
	{
		dTotal = double.Parse(dt.Rows[0]["price"].ToString(), NumberStyles.Currency, null);
		dTAX = double.Parse(dt.Rows[0]["tax"].ToString(), NumberStyles.Currency, null);
		dAmount = double.Parse(dt.Rows[0]["total"].ToString(), NumberStyles.Currency, null);
		dGST = MyDoubleParse(dt.Rows[0]["customer_gst"].ToString());
		dSaveAmount = dNormalTotal - dTotal;
		//dSaveAmount = ((dNormalTotal+dFreight)*(1+dGST)) - dAmount;  //Change by Sean 5/Sep/2011
		dDiscountpercent = (dSaveAmount/(dAmount+dSaveAmount))*100;
		dDiscountpercent  = Math.Round(dDiscountpercent,2);
	}	
	
	sb.Append("<tr><td colspan=" + m_itemCols + ">&nbsp;</td></tr>\r\n");

	if(m_sInvType == "invoice")// && dFreight > 0)
	{
		sb.Append(PrintFreightTicket(ref nCountRows));
	}

	//sub-total
	sb.Append("<tr><td height=1 bgcolor=#CCCCCC colspan=" + m_itemCols + " ></td></tr>");
	sb.Append("<tr><td colspan=" + m_itemCols + ">&nbsp;</td></tr>\r\n");
	if(!bOrder)
    {
		if(bIndividual_Price)
            m_itemCols = (MyIntParse(m_itemCols) - 2).ToString();
        else
            m_itemCols = (MyIntParse(m_itemCols) - 2).ToString();
    }
    /*if(dTotal < realSubTotal)
    {
        sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right wrap ><b>Payment Discount :</b></td><td colspan=2  align=right>");
	    sb.Append(Math.Round((1-dTotal/realSubTotal)*100, 2).ToString());
	    sb.Append("%</td></tr>\r\n");
    }*/
	sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right wrap ><b>RRP Total :</b></td><td colspan=2  align=right>");
	sb.Append(rrpTotal.ToString("c"));
	sb.Append("</td></tr>\r\n");

	
	//DEBUG("dNormalTotal = ", dNormalTotal);
	//DEBUG("dTotal = ", dTotal);
	//DEBUG("dSaveAmount = ", dSaveAmount);
	//if(dSaveAmount != 0)
	//{
		//sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right valign=top  wrap>");
		//sb.Append("<b>Discount Percent :</b></td><td colspan=2  align=right>" + dDiscountpercent + "%</font></td></tr>");
       
		
		sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right valign=top  wrap>");
		sb.Append("<b>SAVE :</b></td><td colspan=2  align=right>" + (rrpTotal-netTotal).ToString("c") + "</font></td></tr>");

        sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right valign=top  wrap>");
		sb.Append("<b>Net Total :</b></td><td colspan=2  align=right>" + netTotal.ToString("c") + "</font></td></tr>");
	//}

    sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right valign=top  wrap>");
	sb.Append("<b>Freight :</b></td><td colspan=2  align=right>" + dFreight.ToString("c") + "</font></td></tr>");
	
	sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right wrap><b>GST :</b></td><td colspan=2  align=right>");
	sb.Append(dTAX.ToString("c"));
	sb.Append("</td></tr>\r\n");

	sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + " align=right wrap><b>Total Amount :</b></td><td colspan=2 align=right>");
	sb.Append(dAmount.ToString("c") );
	sb.Append("</td></tr>");
	if(m_bShowPayment)
	{
		if(bOrder && bool.Parse(dt.Rows[0]["credit_paid"].ToString()))
        {
            sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + ">");
		    sb.Append("<font color=green><b>Credit Card Paid</b></font>");
		    sb.Append("</td></tr>");
        }
        else
        {
            sb.Append("<tr><td colspan=" + (MyIntParse(m_itemCols) - 2).ToString() + ">");
		    sb.Append(ShowPayment());
		    sb.Append("</td></tr>");
        }
	}
	sb.Append("</td></tr></table>\r\n");	
	if(i == (dt.Rows.Count) )
	{		
		sb.Append("<tr><td colspan=5>");
/*		for(int m=0; m<((m_nTotalInvoiceRows - nCountRows)); m++)
		{
			sb.Append("<br>");
		} */
        sb.Append("<br><br><br>");
		sb.Append("</td></tr>"); 

		if(m_bEnablePrintOverDueStatement && m_account_term != "" && !bOrder && m_sSite.ToLower() == "admin")
		{
			//for Cash On Delivery only
			if(m_credit_termID == "3" || String.Compare(m_account_term.ToLower(), "c.o.d", true) == 0 || String.Compare(m_account_term.ToLower(), "cash on delivery", true) == 0 || String.Compare(m_account_term.ToLower(), "c.o. d", true) == 0 )
				sb.Append(BuildOverDueInvoice(m_account_number, m_credit_termID, m_account_term));
		}
		sb.Append(""+ sFooter +"");	
	}
	return sb.ToString();
}

string ShowPayment()
{
	DataSet dspay = new DataSet();
	string sc = " SELECT t.amount, i.amount_applied, d.*, c.name AS staff, e.name AS payment ";
	sc += " FROM tran_invoice i JOIN trans t ON t.id = i.tran_id ";
	sc += " JOIN tran_detail d ON d.id = i.tran_id ";
	sc += " LEFT OUTER JOIN card c ON c.id = d.staff_id ";
	sc += " LEFT OUTER JOIN enum e ON (e.id = d.payment_method AND e.class='payment_method') ";
	sc += " WHERE i.invoice_number = '" + m_inv_sNumber + "' ";
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dspay, "payment");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	if(rows <= 0)
		return "<font color=red><b>UNPAID</b></font>";

	double dAppliedTotal = 0;

	StringBuilder sb = new StringBuilder();

	sb.Append("<table cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><td colspan=10><font size=+1>Payment Information</font></td></tr>");
	sb.Append("<tr style=\"color:black;background-color:#EEEEEE;font-weight:bold;\">");
	sb.Append("<th>Date &nbsp; </th>");
	sb.Append("<th>Type &nbsp; </th>");
	sb.Append("<th>Total &nbsp; </th>");
	sb.Append("<th>Ref &nbsp; </th>");
	sb.Append("<th>PaidBy &nbsp; </th>");
	sb.Append("<th>Bank &nbsp; </th>");
	sb.Append("<th>Branch &nbsp; </th>");
	sb.Append("<th>Accountant &nbsp; </th>");
	sb.Append("<th>Note &nbsp; </th>");
	sb.Append("<th>Applied &nbsp; </th>");
	sb.Append("</tr>");

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspay.Tables["payment"].Rows[i];
		string payment_method = dr["payment"].ToString().ToUpper();
		string payment_date = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy HH:mm");
		double dApplied = MyDoubleParse(dr["amount_applied"].ToString());
		string staff = dr["staff"].ToString();
		string payment_ref = dr["payment_ref"].ToString();
		string paid_by = dr["paid_by"].ToString();
		string bank = dr["bank"].ToString();
		string branch = dr["branch"].ToString();
		double dAmount = MyDoubleParse(dr["amount"].ToString());
		string note = dr["note"].ToString();
		
		dAppliedTotal += dApplied;
		sb.Append("<tr>");
		sb.Append("<td>" + payment_date + "</td>");
		sb.Append("<td>" + payment_method + "</td>");
		sb.Append("<td>" + dAmount.ToString("c") + "</td>");
		sb.Append("<td>" + payment_ref + "</td>");
		sb.Append("<td>" + paid_by + "</td>");
		sb.Append("<td>" + bank + "</td>");
		sb.Append("<td>" + branch + "</td>");
		sb.Append("<td>" + staff + "</td>");
		sb.Append("<td>" + note + "</td>");
		sb.Append("<td align=right>" + dApplied.ToString("c") + "</td>");
		sb.Append("</tr>");
	}
	sb.Append("<tr><td colspan=9 align=right><b>Total Applied : </b></td>");
	sb.Append("<td align=right><b>" + dAppliedTotal.ToString("c") + "<b></td></tr>");

	double dLeft = m_invoice_total - dAppliedTotal;

	sb.Append("<tr><td colspan=9 align=right><b>Amount Owed : </b></td>");
	sb.Append("<td align=right><font color=red><b>" + dLeft.ToString("c") + "<b></font></td></tr>");
	
	sb.Append("</table>");

	return sb.ToString();
}

string PrintFreightTicket(ref int nRowCount)
{
	StringBuilder sb = new StringBuilder();
	double ship_dtotal = 0;
	if(!DoGetTicketNo(m_inv_sNumber))
		return sb.ToString();
	else
	{
		int tickets = dstt.Tables["tickets"].Rows.Count;
		if(tickets <= 0)
			return "";

		sb.Append("<tr><td colspan="+ m_itemCols +" align=right>\r\n");

		DataRow drt = null;
		//List shipping tickets;
		for(int f=0; f<tickets; f++)
		{
			nRowCount++;
			drt = dstt.Tables["tickets"].Rows[f];
			sb.Append("<tr><td>&nbsp;</td>");
			sb.Append("<td colspan=1>" + drt["ticket"].ToString() + "</td>");
			sb.Append("<td colspan=2>" + drt["ship_name"].ToString() + " - ");// + "</td>");
			sb.Append(drt["ship_desc"].ToString() + "</td>");
			sb.Append("<td colspan=2 align=right>" + MyMoneyParse(drt["price"].ToString()).ToString("c") + "</td></tr>");
			ship_dtotal += MyMoneyParse(drt["price"].ToString());
		}
		sb.Append("</td></tr>");
		sb.Append("<tr><td colspan=6><hr></td></tr>\r\n");
	}
	return sb.ToString();
}
string InvoicePrintOneRow(DataRow dr, bool bSystem, bool bIndividual_Price, bool bOrder)
{
	return InvoicePrintOneRow(dr, bSystem, bIndividual_Price, false, bOrder);
}
string InvoicePrintOneRow(DataRow dr, bool bSystem, bool bIndividual_Price, bool bKit, bool bOrder)
{
	string code =dr["code"].ToString();
	string stock_locaiton = dr["stock_location"].ToString();
	string src = GetProductImgSrc(code);
	src = src.Replace("na.gif", "0.gif");
	string branch =GetBranchName(dr["branch"].ToString());
	double dNormalPrice = double.Parse(dr["normal_price"].ToString(), NumberStyles.Currency, null); 
    double rrp_price = double.Parse(dr["rrp"].ToString(), NumberStyles.Currency, null);
    rrp_price = Math.Round(rrp_price, 2);
//	dNormalPrice = Math.Round((dNormalPrice/1.15),2);	 // Exclu G.S.T  
	double dPrice = double.Parse(dr["commit_price"].ToString(), NumberStyles.Currency, null);
    dPrice = Math.Round(dPrice, 2);
	double quantity = MyDoubleParse(dr["quantity"].ToString());
	double dDiscountPercent = double.Parse(dr["discount_percent"].ToString());
	dDiscountPercent /= 100;
    double totalDiscountP = dPrice*(1-dDiscountPercent)/rrp_price;
    totalDiscountP = Math.Round(totalDiscountP, 2);
	if (dPrice >= dNormalPrice)
		dNormalPrice = dPrice;   // if selling more the normal price, selling price will be display as normal price
    double  dNormalSubTotal = dNormalPrice * quantity;
    rrpTotal += rrp_price * quantity;
	
	double dTotal = (dPrice * (1-dDiscountPercent)) * quantity ; // Exclu G.S.T
	dTotal = Math.Round(dTotal, 2);

    netTotal += dTotal;
	string shelf = dr["shelf"].ToString();

	m_dOrderTotal += (double.Parse(dr["commit_price"].ToString()) * (1-dDiscountPercent)) * quantity; //dTotal;

	StringBuilder sb = new StringBuilder();

	sb.Append("<tr");
	if(bKit)
		sb.Append(" bgcolor=aliceblue");
	if((m_nItemCounter % 2)==0)
		sb.Append(" bgcolor=#EEEEE ");
	
	sb.Append(">");	
	bool bService = bool.Parse(dr["is_service"].ToString());
	sb.Append("<td valign='top'></td>");
	if(bService)
	{
		sb.Append("<td>&nbsp;</td>");
		sb.Append("<td>&nbsp;</td>");
		sb.Append("<td>" + dr["item_name"].ToString());
	}
	else if(bKit)
	{
		sb.Append("<td>&nbsp;</td>");
		sb.Append("<td>&nbsp;</td>");
		sb.Append("<td>x" + dr["quantity"].ToString() + " " + dr["code"].ToString() + " " + dr["item_name"].ToString());
	}
	else
	{
		//sb.Append("<td valign=top>" + dr["pack"].ToString() +"</td>");
        sb.Append("<td valign=top></td>");
		if(bOrder)
		{
			sb.Append("<td valign=top>" + stock_locaiton + "</td>");
			//sb.Append("<td valign=top>" + GetItemLocation(dr["code"].ToString()) + "</td>");
		}
		sb.Append("<td valign=top>" + dr["supplier_code"].ToString() + "</td>");
		if(bOrder)
			sb.Append("<td valign=top><img width=50 src='" + src + "' border=0></td>");
		sb.Append("<td valign=top>" + dr["item_name"].ToString());
        if(dDiscountPercent == 0)
            sb.Append(" *");
        sb.Append("</td>");
	}
    sb.Append("<td align=right valign=top>");
    if((bIndividual_Price) || bKit)
	{
		sb.Append("&nbsp;");
	}
    else
    {
        if(m_bDisplayServiceItemWithNoPrice)
		{
			if(bool.Parse(dr["is_service"].ToString()) && double.Parse(dr["commit_price"].ToString()) == 0)
			    sb.Append("&nbsp;");	
			else
			    sb.Append(rrp_price.ToString("c"));	
		}
        else
            sb.Append(rrp_price.ToString("c"));
    }
    sb.Append("</td>");
    sb.Append("<td align=right valign=top>");
    if((bIndividual_Price) || bKit)
	{
		sb.Append("&nbsp;");
	}
    else
    {
        //if(dDiscountPercent == 0)
            //sb.Append("&nbsp;");
        //else
        //{
            if(m_bDisplayServiceItemWithNoPrice)
		    {
			    if(bool.Parse(dr["is_service"].ToString()) && double.Parse(dr["commit_price"].ToString()) == 0)
			        sb.Append("&nbsp;");	
			    else
			        sb.Append((Math.Round((1-totalDiscountP)*100,2)).ToString() + "%");	
		    }
            else
                sb.Append((Math.Round((1-totalDiscountP)*100,2)).ToString() + "%");
        //}
    }
    sb.Append("</td>");
    
	sb.Append("<td align=right valign=top>");
	if((bIndividual_Price) || bKit)
	{
		sb.Append("&nbsp;");
	}
	else
	{
		if(m_bDisplayServiceItemWithNoPrice)
		{
			if(bool.Parse(dr["is_service"].ToString()) && double.Parse(dr["commit_price"].ToString()) == 0)
			{
				sb.Append("&nbsp;");	
			}
			else
			{ // if discount not equal null
				if(dDiscountPercent > 0)
				{
					double Isellingprice = double.Parse(dr["commit_price"].ToString());
				    Isellingprice = Isellingprice * (1- dDiscountPercent);
			        Isellingprice = Math.Round(Isellingprice, 2);
					sb.Append("$"+Isellingprice);
			    }
			    else
			    {
					sb.Append(MyDoubleParse(dr["commit_price"].ToString()).ToString("c"));	
				} // if disount qual null
			}
		}
		else
		{ // if discount not equal null
			if(dDiscountPercent > 0)	
			{
				double Isellingprice = double.Parse(dr["commit_price"].ToString());
				Isellingprice = Isellingprice * (1- dDiscountPercent);
			    Isellingprice = Math.Round(Isellingprice, 2);
				sb.Append("$"+Isellingprice);
		    }	
			else
			{   
		  	    double dcprice = MyDoubleParse(dr["commit_price"].ToString());
		 	   dcprice = Math.Round(dcprice, 2);
			  sb.Append(dcprice.ToString("c"));
			}
		}
	}
	sb.Append("</td>");
	if(!bKit)
	{
		if(m_bDisplayServiceItemWithNoPrice)
		{
			if(bool.Parse(dr["is_service"].ToString()) && double.Parse(dr["commit_price"].ToString()) == 0)
				sb.Append("<td>&nbsp;</td>");					
			else
				sb.Append("<td align=right valign=top>" + dr["quantity"].ToString() + "</td>");
		}
		else
			sb.Append("<td align=right valign=top>" + dr["quantity"].ToString() + "</td>");
	}
	else
		sb.Append("<td>&nbsp;</td>");
	sb.Append("<td align=right valign=top>");
	if((bIndividual_Price) || bKit)
	{
		sb.Append("&nbsp;");
	}
	else
	{
		if(m_bDisplayServiceItemWithNoPrice)
		{
			if(bool.Parse(dr["is_service"].ToString()) && double.Parse(dr["commit_price"].ToString()) == 0)
				sb.Append("&nbsp;");					
			else
				sb.Append(dTotal.ToString("c"));
		}
		else
				sb.Append(dTotal.ToString("c"));
	}
	dNormalTotal += dNormalSubTotal;
	sb.Append("</td></tr>\r\n");
	if(m_bDoSN && !bKit)
	{
		DataSet dst = new DataSet();
		
		string inv_No = m_inv_sNumber;//Request.QueryString[0];
		string sc = "SELECT sn FROM sales_serial WHERE invoice_number=" + inv_No;
			sc += " AND code=" + dr["code"].ToString();		
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst,"serials");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return "";
		}	
		string sn = "";
		for(int i=0; i < dst.Tables["serials"].Rows.Count; i++)
		{
			sn += dst.Tables["serials"].Rows[i]["sn"].ToString() + "; ";
		}
		if(m_bDoDisplaySNonInvoice)
		{
			if(sn.Length > 0)  //do not display SN# 7-4-03
			{
				sb.Append("<tr><td>S/N# :</td><td>");
				sb.Append(sn);
				sb.Append("</td></tr>\r\n");
			}
		}
	}
	return sb.ToString();
}

string BuildOrderMail(string sOrderNumber)
{
	string m_sKitTerm = GetSiteSettings("package_bundle_kit_name", "Kit", true);
	m_nTotalInvoiceRows = int.Parse(GetSiteSettings("set_total_invoice_item_rows_for_invoice_printout", "20"));

	m_inv_sNumber = sOrderNumber;

	DataSet dsi = new DataSet();
	
	string sc = "SELECT  i.discount_percent, o.customer_gst, b.branch_header,  b.branch_footer, c2.name AS sales, o.system AS iSystem ";
	sc += ", o.no_individual_price AS iNoIndividual, o.paid AS Credit_Paid, o.*, c.*, i.code, cr.stock_location ";
	sc += ", i.supplier_code, i.item_name, i.quantity, i.commit_price, ISNULL(cr.is_service,0) AS is_service, i.system AS bsystem, i.kit, i.krid, i.pack ";
	sc += ", k.kit_id, k.qty AS kit_qty, k.name AS kit_name, k.commit_price AS kit_price, cr.brand, ISNULL(cr.level_price0*cr.level_rate1/100, 0) AS rrp ";
	sc += ", (SELECT TOP 1 s.name FROM shelf_item si Join shelf s ON s.id = si.shelf_id WHERE si.code = i.code ORDER BY si.qty DESC) AS shelf ";
//	if(m_sInvType == "invoice")
//		sc +=", sa.normal_price ";	
//	else
//		sc += ", (cr.price1 / 1.15) as normal_price ";
	sc += ", 0 as normal_price ";
	sc += " FROM order_item i JOIN orders o ON i.id=o.id ";
	if(m_sInvType == "invoice")
		sc += " LEFT OUTER JOIN sales sa ON sa.code = i.code";
	sc += " LEFT OUTER JOIN order_kit k ON k.id=o.id AND k.krid=i.krid ";
	sc += " LEFT OUTER JOIN card c ON c.id=o.card_id ";
	sc += " LEFT OUTER JOIN card c2 ON c2.id = o.sales ";
	sc += " LEFT OUTER JOIN code_relations cr ON cr.code = i.code ";
	sc += " LEFT OUTER JOIN branch b ON b.id = o.branch ";
	sc += " WHERE o.id = " + sOrderNumber;
	sc += " ORDER BY i.supplier_code ";
//	sc += " ORDER BY cr.stock_location, shelf, i.supplier_code ";
//DEBUG("sc=", sc);
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
		return "<h3>Order# " + sOrderNumber + " Not Found</h3>";

	DataRow dr = dsi.Tables[0].Rows[0];

	string sDate = dr["record_date"].ToString();
	DateTime tDate = DateTime.Parse(sDate);
	m_branchHeader = dr["branch_header"].ToString();
	m_branchFooter = dr["branch_footer"].ToString();

	m_account_number = dr["card_id"].ToString();
	m_account_term = dr["credit_term"].ToString();
	if(m_account_term != "")
		m_account_term = GetEnumValue("credit_terms", m_account_term);
	m_account_name = dr["trading_name"].ToString();

	string sales = dr["sales"].ToString();
	string po_number = dr["po_number"].ToString();
	string sType = GetEnumValue("receipt_type", dr["type"].ToString());
	StringBuilder sb = new StringBuilder();

	sb.Append("<html><style type=\"text/css\">\r\n");
	sb.Append("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}\r\n");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>\r\n");
	
	InvoicePrintShip(dr, "");		

	//print header
	sb.Append(InvoicePrintHeader(sType, sales, sOrderNumber, tDate.ToString("dd/MM/yyyy"), po_number));

	//print shippment address
	string shipto = "";
	if(bool.Parse(dr["special_shipto"].ToString()))
	{
		shipto = dr["shipto"].ToString();
		shipto = shipto.Replace("\r\n", "<br>\r\n");
	}
//	sb.Append(InvoicePrintShip(dr, ""));
//	Response.Write(InvoicePrintBottom(dr["sales_note"].ToString()));
	string footer = InvoicePrintBottom(dr["sales_note"].ToString());
	//print invoiced items
	sb.Append(BuildItemTable(dsi.Tables[0], true, sb.ToString(), footer ));

/*	sb.Append("<br>");

	//conditions table
	sb.Append(InvoicePrintBottom(dr["sales_note"].ToString()));
*/
	sb.Append("</body></html>");

	return sb.ToString();
}

string BuildOverDueInvoice(string customerID, string terms_id, string credit_terms)
{
	double[] dSubBalance = new double[4];	
	double dCreditTotal = 0;

	StringBuilder sb = new StringBuilder();	
	sb.Append("<table width='90%'  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=gray bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:solid;border-collapse:collapse;fixed\">");
	sb.Append("<tr><th bgcolor=#EECC00 colspan=6 align=left>OVERDUE SUMMARY</th></tr>");
	sb.Append("<tr width=100% bgcolor=#EEEEE ><td align=center><b> Current</b></td>\r\n");	
	if(terms_id == "4")
		sb.Append("<td align=center><b>7 Days</b></td>\r\n");
	else if(terms_id == "5")
		sb.Append("<td align=center><b>14 Days</b></td>\r\n");
	else
		sb.Append("<td align=center><b>30 Days</b></td>\r\n");	
	if(terms_id == "4")
		sb.Append("<td align=center><b>14 Days</b></td>\r\n");
	else if(terms_id == "5")
		sb.Append("<td align=center><b>30 Days</b></td>\r\n");	
	else
		sb.Append("<td align=center><b>60 Days</b></td>\r\n");
	
		if(terms_id == "4")
		sb.Append("<td align=center><b>30 Days+</b></td>\r\n");
	else if(terms_id == "5")
		sb.Append("<td align=center><b>60 Days+</b></td>\r\n");
	
	else
		sb.Append("<td align=center><b>90 Days+</b></td>\r\n");
	sb.Append("<td align=center><b>Credits Left</b></td><th align=right>Amount Due</td></tr>\r\n");
	int nStoreID = 0;
	for(int i = 0; i<4; i++)
	{		
		GetInvoiceSubBalance(i, customerID, ref dSubBalance[nStoreID], terms_id);		
		nStoreID++;			
	}
	sb.Append("<tr><td align=center><b>" + dSubBalance[0].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[1].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[2].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dSubBalance[3].ToString("c") + "</b></td>\r\n");
	sb.Append("<td align=center><b>" + dCreditTotal.ToString("c") + "</b></td>\r\n");

	double d_SumTotal = dSubBalance[0] + dSubBalance[1] + dSubBalance[2] + dSubBalance[3];
	d_SumTotal -= dCreditTotal;

	sb.Append("<td align=right><b>" + d_SumTotal.ToString("c") + "</b></td>\r\n");
	
	sb.Append("</tr></table>\r\n");
	
	return sb.ToString();
}

bool GetInvoiceSubBalance(int i, string sCardID, ref double dtotal, string credit_term_id)
{
	if(dsi.Tables["balance"] != null)
		dsi.Tables["balance"].Clear();
	int rows = 0;
	string sc= "";	

	if(!chckCardType(ref sCardID))
	{
		sc= " SELECT SUM(total - amount_paid) AS sub_total FROM invoice WHERE card_id = " + sCardID;
		if(credit_term_id == "4")  // ** 7days
		{			
			if(i == 0)
				sc += "	AND (DATEDIFF(day, commit_date, GETDATE()) >= 0 AND DATEDIFF(day, commit_date, GETDATE()) < 7)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 7 AND DATEDIFF(day, commit_date, GETDATE()) < 15)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 15 AND DATEDIFF(day, commit_date, GETDATE()) < 22)";
			else if(i == 3)
				sc += " AND DATEDIFF(day, commit_date, GETDATE()) >= 22";
			else
			{
				dtotal = 0.00;
				return true;
			}

		}
		else if(credit_term_id == "5") // ** 14days
		{
			if(i == 0)
				sc += "	AND (DATEDIFF(day, commit_date, GETDATE()) >= 0 AND DATEDIFF(day, commit_date, GETDATE()) < 14)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 14 AND DATEDIFF(day, commit_date, GETDATE()) < 29)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, commit_date, GETDATE()) >= 29 AND DATEDIFF(day, commit_date, GETDATE()) < 59 )";
			else if(i == 3)
				sc += " AND DATEDIFF(day, commit_date, GETDATE()) >= 59";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		else // ** the rest 30days
		{
			if(i == 0)
				sc += "	AND DATEDIFF(month, commit_date, GETDATE()) = 0 ";
			else if(i == 1)
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 1 ";
			else if(i == 2)
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) = 2";
			else if(i == 3)
				sc += " AND DATEDIFF(month, commit_date, GETDATE()) >= 3";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
	
		sc += " AND paid = 0 ";
//		DEBUG("s c= ", sc);
	}
	else
	{
		sc = " SELECT SUM(total_amount - amount_paid) AS sub_total FROM purchase WHERE supplier_id = " + sCardID;
		if(credit_term_id == "4")  // ** 7days
		{
			if(i == 0)
				sc += "	AND (DATEDIFF(day, date_received, GETDATE()) >= 0 AND DATEDIFF(day, date_received, GETDATE()) < 7)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 7 AND DATEDIFF(day, date_received, GETDATE()) < 15)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 15 AND DATEDIFF(day, date_received, GETDATE()) < 22)";
			else if(i == 3)
				sc += " AND DATEDIFF(day, date_received, GETDATE()) >= 22";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
			else if(credit_term_id == "5") // ** 14days
		{
			if(i == 0)
				sc += "	AND (DATEDIFF(day, date_received, GETDATE()) >= 0 AND DATEDIFF(day, date_received, GETDATE()) < 14)";
			else if(i == 1)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 14 AND DATEDIFF(day, date_received, GETDATE()) < 29)";
			else if(i == 2)
				sc += " AND (DATEDIFF(day, date_received, GETDATE()) >= 29 AND DATEDIFF(day, date_received, GETDATE()) < 59 )";
			else if(i == 3)
				sc += " AND DATEDIFF(day, date_received, GETDATE()) >= 59";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		else // ** the rest 30days
		{
			if(i == 0)
				sc += "	AND DATEDIFF(month, date_received, GETDATE()) = 0 ";
			else if(i == 1)
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 1 ";
			else if(i == 2)
				sc += " AND DATEDIFF(month, date_received, GETDATE()) = 2";
			else if(i == 3)
				sc += " AND DATEDIFF(month, date_received, GETDATE()) >= 3";
			else
			{
				dtotal = 0.00;
				return true;
			}
		}
		sc += " AND amount_paid = 0 ";
	
//		sc += " AND date_received is NOT NULL ";
//DEBUG("sc = ", sc);
	}

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsi, "balance");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(dsi.Tables["balance"].Rows[0]["sub_total"].ToString() == "" || dsi.Tables["balance"].Rows[0]["sub_total"].ToString() == null)
		dtotal = 0;
	else
		dtotal = double.Parse(dsi.Tables["balance"].Rows[0]["sub_total"].ToString());

	return true;
}
bool chckCardType(ref string cardID)
{
	string stype = "1";
	bool bIsSupplier = false;
	if(dsi.Tables["ctype"] != null)
		dsi.Tables["ctype"].Clear();
	int rows = 0;
	string sc= " SELECT * FROM card WHERE id = " + cardID;
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsi, "ctype");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(rows == 1)
	{
		stype = dsi.Tables["ctype"].Rows[0]["type"].ToString();
		if(GetEnumID("card_type", "supplier") == stype)
			bIsSupplier = true;
	}

	return bIsSupplier;
}
</script>

<!-- #include file="kit_fun.cs" -->
<!-- #include file="card_function.cs" -->
<!-- #include file="fifo_f.cs" -->

<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
const int m_cols = 7;	//how many columns main table has, used to write colspan=
string m_tableTitle = "";
string m_invoiceNumber = "";
string m_comment = "";	//Sales Comment in Invoice Table;
string m_custpo = "";
string m_salesNote = "";
string m_branchID = "1";
string m_branchName = "";

string m_branchAddress = "";
string m_branchPhone = "";
string m_sSalesType = ""; //as string, m_quoteType is receipt_type ID
string m_salesName = "";
string m_nShippingMethod = "1";
string m_specialShipto = "0";
string m_specialShiptoAddr = ""; //special
string m_pickupTime = "";
string m_orderID = "";
string m_orderNumber = "";
string m_sales = "";
string m_customerID = "0";
string m_invoiceDate = "";
string m_memberName = "";
string m_memberPoints = "0";

string m_orderStatus = "1"; //Being Processed
string m_url = "";

double m_dFreight = 0;
double m_dInvoiceTotal = 0;
string m_discount = "0";
int m_nSearchReturn = 0;
int m_nNoOfReceiptPrintOut = 1;

bool m_bNoDoubleQTY = true;
bool b_create = false;
bool m_bCreditReturn = false;
bool m_bOrderCreated = false;
bool m_bFixedPrices = false;
bool m_bRecordSales = true;
bool m_bEnableMembership = true;
bool m_bEnblePrintOutSN = false;
bool m_bEnbleTouchScreenButton = false;
bool m_bEnbleAgentSwitch = false;
string m_sTotalTouchScreenItem = "40";

string m_branchGSTNum = "";
string m_branchPosHeader ="";
string m_branchPosFooter ="";
string m_jdic = ""; //JScript Dictionary object
string m_sAgentCache = ""; //agent cache
string m_sProductCache = ""; //product cache
string m_sProductTouchScreenCache = "";
string m_sMemberCache = "";
string m_sSalesBarcodeCache = "";
string miscellaneous_code = "";
string opentilt = "";
double m_gst = 1.15;
double m_dPointRate = 0.05;

double m_dCash = 0; //for kicking cash draw

//bool m_bDebug = true;
bool m_bDebug = false;
bool m_bUpdateCache = true;
bool m_bTrustedSite = true;
string m_shta = ""; //hta file content for offline invoicing
string m_sPaymentForm = "";
string m_sPaymentFormOnlineCache = "";
string m_sReceiptHeader = "";
string m_sReceiptFooter = "";
string m_sReceiptKickout = "";
string m_sReceiptPort = "";
string m_sReceiptPrinterObject = "";
string m_sMember = "";
string m_sServer = "";
string m_sAgent = ""; //agent

bool m_bDisplay = false;
string m_sDisplayPort = "COM1";
string m_sMaxDiscountPercentage = "0";
string m_max_item_name = "24";
string m_max_line_number = "42";
string m_max_space_between = "20";
string m_qpos_next_invoice_number = "10001";  //invoice number
bool m_bDisplayCurrencyExchange = false;
string m_qpos_cent_roundup = "5";
string m_setCulture = "en-GB";
bool m_bEnbleTouchScreenPicture = false;
int m_nTotalHoldItems = 5;
bool m_bEnableGSTCost = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;
	
	//check trusted site
	if(Request.QueryString["nts"] != null) //no trusted site, disable hta functions
	{
		Session["qpos_no_offline"] = true;
		m_bTrustedSite = false;
	}

	// Sets the CurrentCulture property to G.B. English.
	m_setCulture = GetSiteSettings("set_culture_date_format_for_system", "en-GB", true);

	m_max_item_name = GetSiteSettings("QPOS_Max_Item_Name_Length", "24");
	m_max_line_number = GetSiteSettings("QPOS_Max_Line_Number", "42");
	m_max_space_between = GetSiteSettings("QPOS_Max_Space_Between", "20");
	m_sMaxDiscountPercentage = GetSiteSettings("QPOS_MAX_DISCOUNT_PERCENTAGE", "10");
	m_sMaxDiscountPercentage = (MyDoubleParse(m_sMaxDiscountPercentage) / 100).ToString();
	m_bRecordSales = MyBooleanParse(GetSiteSettings("QPOS_RECORD_SALES_PERSON", "1"));
	m_bEnableMembership = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_MEMBERSHIP_POINTS", "1"));
	m_dPointRate = MyDoubleParse(GetSiteSettings("QPOS_MEMBERSHIP_POINT_RATE", "0.05"));
	m_bDisplay = MyBooleanParse(GetSiteSettings("POS_HAS_DISPLAY_UNIT", "0"));
	m_bEnblePrintOutSN = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_SN_PRINTOUT", "0"));
	m_bEnbleTouchScreenButton = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_TOUCH_SCREEN_BUTTON", "0", true));
	m_bEnbleTouchScreenPicture = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_TOUCH_SCREEN_WITH_PICTURE", "0", true));
// no gst for the qpos
	m_bEnableGSTCost = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_NO_GST", "0", true, "This is a setting to turn off the GST for the qpos module"));

	m_nTotalHoldItems = int.Parse(GetSiteSettings("QPOS_SET_TOTAL_HOLD_ORDERS_IN_QPOS", "5", true));
	m_sTotalTouchScreenItem = GetSiteSettings("QPOS_TOTAL_TOUCH_SCREEN_ITEMS", "40");
	m_qpos_cent_roundup = GetSiteSettings("SET_QPOS_CENT_ROUNDUP_BY_VALUE", "10");
	m_qpos_next_invoice_number = GetSiteSettings("qpos_next_invoice_number", "10001");
	m_bEnbleAgentSwitch = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_AGENT_SALES", "0", true));
	if(m_qpos_next_invoice_number != "")
	{
	Response.Write("<form name=f1 ><input type=hidden name=next_inv value="+ m_qpos_next_invoice_number +"></form>");
	Response.Write("<script language=javascript> ");
	string s = @" 
		var fn = 'c:/qpos/qposni.txt'; 
	var inv = Number(document.f1.next_inv.value);
	fso = new ActiveXObject('Scripting.FileSystemObject'); 	
		fso.DeleteFile(fn);
		tf = fso.OpenTextFile(fn , 8, 1, -1);
		tf.Write(inv);
		tf.Close();	
		";
		Response.Write(s);
		Response.Write(" </script ");
		Response.Write(">");
	}

	if(!TSIsDigit(m_sTotalTouchScreenItem))
		m_sTotalTouchScreenItem = "40";
	m_bDisplayCurrencyExchange = MyBooleanParse(GetSiteSettings("QPOS_ENABLE_DISPLAY_CURRENCY_EXCHANGE", "0", true));

	if(!m_bTrustedSite)
		m_bDisplay = false; //force disable
	if(m_bDisplay)
		m_sDisplayPort = GetSiteSettings("POS_DISPLAY_PORT", "LPT1");

	string server = Request.ServerVariables["SERVER_NAME"];
	if(server == "localhost")
		m_bDebug = true; //no maximize, no move etc..
	m_sServer = "http://" + server;

	miscellaneous_code = GetSiteSettings("set_miscellaneous_code", "4000", false);
	opentilt = GetSiteSettings("set_open_tilt_code", "1000", false);
	m_bNoDoubleQTY = MyBooleanParse(GetSiteSettings("qpos_no_double_qty", "1", false));
	m_nNoOfReceiptPrintOut = int.Parse(GetSiteSettings("total_no_of_receipt_printout", "1", false));

	m_gst = MyDoubleParse(GetSiteSettings("gst_rate_percent", "1.125")) / 100;
	if(m_gst < 1)
		m_gst = 1 + m_gst;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		m_bFixedPrices = true;

	if(Session["login_branch_id"] != null)
		m_branchID = Session["login_branch_id"].ToString();
	
	m_branchName = GetBranchName(m_branchID);

	if(Request.QueryString["updatecache"] != null) //write hta file failed for some reason
	{
		Session["qpos_hta_update_needed"] = true;
	}

	string t = "";
	if(Request.QueryString["t"] != null)
		t = Request.QueryString["t"];
	if(Request.QueryString["oi"] == null & t == "")
	{
		if(m_bTrustedSite)
		{
			CheckOfflineInvoices();
			return;
		}
	}
	else
	{
		if(t == "new")
		{
			Session["leave_offline_invoice"] = 1;
		}
		else if(t == "load")
		{
			PrintAdminHeader();
			LoadOfflineInvoices();
			return;
		}
		else if(t == "process")
		{
			PrintAdminHeader();
			ProcessOfflineInvoices(false);
			return;
		}
	}

	if(Request.QueryString["t"] == "new")
		EmptyCart();
	else if(Request.QueryString["t"] == "pr")
	{
		PrintAdminHeader();
		PrintReceipt();
		return;
	}
	else if(Request.QueryString["t"] == "plr")
	{
		if(Session["qpos_last_invoice_number"] == null)
		{
			PrintAdminHeader();
			Response.Write("<br><center><h3>Sorry you haven't done any invoice in this session</h3>");
			return;
		}
		m_invoiceNumber = Session["qpos_last_invoice_number"].ToString();
		if(DoPrintReceipt(false, false))
		{
			Response.Write("<script language.javascript>window.close();<");
			Response.Write(">");
		}
		return;
	}
	else if(Request.QueryString["t"] == "end")
	{
		ProcessOfflineInvoices(true);
		return;
	}
	else if(Request.QueryString["t"] == "chl") //create hta link
	{
		PrintAdminHeader();
		DoCreateShortcut();
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpos.aspx\">");
		return;
	}

	bool bMassInterface = false;
	if(Request.Form["rows"] == null)
		bMassInterface = true;

	if(Session["qpos_cache_updated"] != null)
		m_bUpdateCache = false;

	if(bMassInterface)
	{
		if(Session["qpos_hta_update_needed"] == null && m_bTrustedSite)
			CheckOfflineHTAFile();

		if(Request.QueryString["t"] == "fu")
			m_bUpdateCache = true;

		//main output, choose interface to use
		if(m_bTrustedSite)
		{
			if(Session["qpos_hta_update_needed"] == null && !m_bUpdateCache)
			{
				PrintTrustedMassInterface();
			}
		}
		else
		{
			if(m_bEnbleTouchScreenButton)
			{
				if(!BuildTouchScreenButtonCache())
					return;
			}
			if(!BuildProductCache())
				return;
			if(!BuildAgentCache())
				return;
			if(!BuildMemberCache())
				return;
			if(m_bRecordSales)
			{
				if(!BuildSalesBarcodeCache())
					return;
			}
			PrintMassInterface();
		}

		//check updates
		if(m_bTrustedSite)
		{
			if(Session["qpos_hta_update_needed"] != null || m_bUpdateCache)
			{
				PrintAdminHeader();
				Response.Write("<br><center><h4>Updating cache, please wait ... </h4>");
		
				if(!BuildProductCache())
					return;
				if(!BuildAgentCache())
					return;
				if(!BuildMemberCache())
					return;
				if(m_bEnbleTouchScreenButton)
				{
					if(!BuildTouchScreenButtonCache())
						return;
				}
				if(m_bRecordSales)
				{
					if(!BuildSalesBarcodeCache())
						return;
				}
				PrintMassInterface();
				UpdateOfflineHTAFile();
				Session["qpos_hta_update_needed"] = null; //regardless write falied or not
				Session["qpos_cache_updated"] = 1;
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpos.aspx\">");
				return;
			}
		}
		return;
	}

	if(Request.Form["confirm_checkout"] == "0") //force going back on unknow error
	{
//		Response.Write("Error");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpos.aspx?t=new\">");
		return;
	}
}

bool PrintMassInterface()
{
	DoPrintReceipt(true, true); //get printing characters
	
	StringBuilder sb = new StringBuilder();
	
	string sheader = ReadSitePage("qpos_header");
	sheader = sheader.Replace("@@company_title", m_sCompanyTitle);
	sb.Append(sheader);
	
	if(m_bTrustedSite)
	{
		sb.Append("\r\n<object classid=\"clsid:8A5E02AF-CC58-4EA8-9CB5-E9B7AC3A707B\" ");
		sb.Append(" CODEBASE=\"ezprint.dll#version=2,0,0,2\" width=1 height=1 style='visibility:hidden' ");
		sb.Append(" id=\"EzPrint\">\r\n");
//		sb.Append("<param name=\"m_sFontSize\" value=\"8\">\r\n");
		sb.Append("</object>\r\n");
	}	
	sb.Append(PrintMJava());
	sb.Append(m_sProductCache);
	sb.Append(m_sAgentCache);  //get agent cache
	sb.Append(m_sMemberCache);
	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");	
	sb.Append("<form name=f action=?t=end");
	if(!m_bTrustedSite)
		sb.Append("&nts=1");
	sb.Append(" method=post onKeyDown=\"return on_form_keydown();\">\r\n");
	sb.Append("<input type=hidden name=printer_port value='" + m_sReceiptPort + "'>");
	sb.Append("<input type=hidden name=display_port value='" + m_sDisplayPort + "'>");
	sb.Append("<input type=hidden name=branch value=" + m_branchID + ">");
	sb.Append("<input type=hidden name=branch_name value='" + m_branchName + "'>");
	//add round cent for cash sales only
	sb.Append("<input type=hidden name=round_cent value='"+ m_qpos_cent_roundup +"'>");
	sb.Append("<input type=hidden name=invoice_total_backup value=''>");
	
	//***** get latest invoice number 
	sb.Append("<input type=hidden name=hd_invoice_number value='"+ m_qpos_next_invoice_number +"' >");

	sb.Append(m_sSalesBarcodeCache);
	//java memory
	sb.Append("<input type=hidden name=rows value=0>");
	sb.Append("<input type=hidden name=focus_field value=2>");
	sb.Append("<input type=hidden name=last_barcode>");
	sb.Append("<input type=hidden name=last_code>");
	sb.Append("<input type=hidden name=last_is_package value=0>");

	sb.Append("<input type=hidden name=confirm_checkout value=0>");

	sb.Append("<input type=hidden name=max_item_name value='" + m_max_item_name + "'>");
	sb.Append("<input type=hidden name=max_line_number value='" + m_max_line_number + "'>");
	sb.Append("<input type=hidden name=max_space_between value='" + m_max_space_between + "'>");
	
	if(m_bEnableGSTCost)
		sb.Append("<input type=hidden name=set_no_gst value='1'>");
	else
		sb.Append("<input type=hidden name=set_no_gst value='0'>");
	sb.Append("<table width=100% border=0 cellspacing=0 cellpadding=0 >\r\n"); 
	//sb.Append(" bordercolorlight=#44444 bordercolordark=#AAAAAA bgcolor=white ");
	//sb.Append(" style=\"font-family:Verdana;font-size:8pt;fixed\" >\r\n");

	sb.Append("<tr >");
	sb.Append("<td width=75% valign=top >\r\n");
	// ============================================ Header table ======================================
	sb.Append("<table width=100% border=0 cellspacing=0 cellpadding=0 bordercolorlight=#44444 bordercolordark=#AAAAAA bgcolor=#336699 ");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;fixed\" >\r\n");

	//header
	sb.Append("<tr bgcolor=#6699CC ><td  align=left>");
//	sb.Append("<table width=100% border=0 cellspacing=0 cellpadding=3 bordercolor=red style=\"background-image:url(file:///C:/qpos/btBack.gif)\" >\r\n");
	sb.Append("<table  border=0 cellspacing=0 cellpadding=3 bordercolor=red   >\r\n");
	sb.Append("<tr align=left  bgcolor=#6699CC >");
	sb.Append("<td  align=center width=100>");
	
//DEBUG("msserver = ", m_sServer);

	sb.Append("<img border=0 src=\"c:/qpos/eznz.gif\">"); //src='" + m_sServer + "c:/qpos/newlogo2.jpg'>");
	
sb.Append("</td><td width=60 align=center>");
    
	//sb.Append("<input type=button onclick=window.location='qpos.aspx?t=fu' name=pos_title value='P.O.S' >");
//	sb.Append("<input type=image src=\"/i/reload.jpg\" onclick=window.location='qpos.aspx?t=fu' Title='Refresh' >");
		sb.Append("<input type=button onclick=window.location='qpos.aspx?t=fu' name=pos_title value='&nbsp;' style=\"border-style:none; border-left:none; height=50px ;width:50px;background:url('c:/qpos/reload.jpg')\" Title='Update Cache'>");
	sb.Append("</td>\r\n<td width=60 align=center><input type=button onclick=\"event.keyCode=90;return on_form_keydown();\" style=\"border-style:none; border-left:none; background:url('c:/qpos/opendraw.jpg'); height=50px; width:50px\" Title='Open Cash Draw'></td>");
	sb.Append("<td width=60 align=center><input type=button onclick=\"event.keyCode=79;return on_form_keydown();\"  style=\"border-style:none; border-left:none; background:url('c:/qpos/orderlist.jpg'); height=50px; width:50px\" Title='Order List'>");
	sb.Append("<td width=60 align=center><input type=button onclick=\"event.keyCode=72;return on_form_keydown();\"  style=\"border-style:none; border-left:none; background:url('c:/qpos/neworder.jpg'); height=50px; width:50px\"Title='New Order'>");
	sb.Append("<td width=60 align=center><input type=button onclick=\"event.keyCode=85;return on_form_keydown();\"  style=\"border-style:none; border-left:none; background:url('c:/qpos/print.jpg'); height=50px; width:50px\" Title='Print Last Reciept'>");
		sb.Append("<td width=60 align=center><input type=button onclick=\"window.open('doc/index.html')\"  style=\"border-style:none; border-left:none; background:url('c:/qpos/help.jpg'); height=50px; width:50px\" Title='Help / Document Center'>");
	sb.Append("</td></tr>");
	sb.Append("</table>");
	sb.Append("</td>");
	sb.Append("<td align=center  >");
	sb.Append("<table border=0 cellspacing=0 cellpadding=0 >");
	sb.Append("<tr  bgcolor=#6699CC ><td align=left>");
	
	//******Display Branch Name ********//
//	if(m_branchName != "")
//		sb.Append("<b><font size=2 color='yellow'>" +m_branchName +" Branch &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b></font> ");	
	//******Display Branch Name ********//

	if(m_bEnbleTouchScreenButton)
		sb.Append("<input type=hidden name=touchscreen value='1'>\r\n");
	else
		sb.Append("<input type=hidden name=touchscreen value='0'>\r\n");
	if(m_bEnableMembership)
	{
		sb.Append("<input type=hidden name=member value='0'>\r\n");
		sb.Append("<b><font color='white'>Membership : </b>");
		sb.Append("</td><td>");
		sb.Append("<input type=text name=member_code size=10 value='' onKeyDown=\"if(event.keyCode==13){CheckMemberCode();}\" onfocus=\"sh(this);\" onblur=\"rh(this);\" >");
		sb.Append("<input type=hidden name=member_points_current value=0>");
		sb.Append("<input type=hidden name=member_point_rate value=" + m_dPointRate + ">");
		sb.Append("<font color='white'> - <input type=text size=10 style='border:0;background-color:#EEEEEE;background-image:url(file:///C:/qpos/btBack.gif);' readonly=true name=member_name value=''>");
	}
	else
	{
		sb.Append("<input type=hidden name=member value='0'>\r\n");
		sb.Append("<input type=hidden name=member_code value='0'>");// onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
		sb.Append("<input type=hidden name=member_points_current value=0>");
		sb.Append("<input type=hidden name=member_point_rate value=" + m_dPointRate + ">");
		sb.Append("<input type=hidden name=member_name value=''>");
	}
	
	sb.Append("<input type=hidden size=1 style=text-align:right name=member_discount value=0 >");
	sb.Append("</td></tr>");
	sb.Append("<tr bgcolor=#6699CC ><td align=left>");
	if(m_bRecordSales)
	{
		sb.Append("<b><font color='white'>Sales : </b>");
		sb.Append("<input type=hidden name=sales value=''>\r\n");
		sb.Append("</td><td>");
		sb.Append("<input type=password size=12 name=sales_barcode onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
		sb.Append(" onKeyDown=\"if(event.keyCode==13){if(CheckSalesBarcode()){document.f.cmdgo.focus();}}\">");
	}
	else
	{
		sb.Append("<input type=hidden name=sales value='"+ Session["card_id"].ToString() +"'>\r\n");
		sb.Append("<input type=hidden name=sales_barcode value=0>");
	}
	
	sb.Append(" <font color='white'>");
	if(m_bRecordSales)
	{
		sb.Append("- ");
		sb.Append("<input type=text size=10 readonly=true style='border:0;background-color:#EEEEEE;background-image:url(file:///C:/qpos/btBack.gif);' name=sales_name value=''>");
		//sb.Append(m_branchName);
		//sb.Append(" - ");
		//sb.Append("<input type=text size=10 readonly=true style='border:0;background-color:#EEEEEE' name=sales_name value=''>");
	}
	else
	{
		sb.Append("<input type=hidden name=sales_name value='"+ Session["card_id"].ToString() +"'>");
/*		sb.Append("<b><font color='white'>Branch : </b>");
		sb.Append("</td><td><font color='yellow'>");
		sb.Append(m_branchName + " &nbsp; ");
		*/
	}
	if(m_bEnbleAgentSwitch)
	{	
		sb.Append("<tr  bgcolor=#6699CC ><td align=right>");
		sb.Append("<b><font color='white'>Agent : </b>");
		sb.Append("</td><td>");
		sb.Append("<input type=hidden name=agent value=''>\r\n");
		sb.Append("<input type=text size=10 name=agent_code onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
		sb.Append(" onKeyDown=\"if(event.keyCode==13){if(CheckAgentCode()){document.f.agent_name.focus(); event.keyCode=9; return false;}else{event.keyCode=9;}}\" >  - ");
		
		sb.Append("<input type=text size=10 style='border:0;background-color:#EEEEEE;background-image:url(file:///C:/qpos/btBack.gif);' readonly=true name=agent_name value=''>");
		sb.Append("</td></tr> ");
		sb.Append("<input type=hidden name=agent_discount value=0> ");
		/*sb.Append("<tr><td>");
		sb.Append("<b><font color='white'>Discount : </b>");
		sb.Append("</td><td>");
		sb.Append("<input size=1 style=text-align:right name=agent_discount value=0 ");
		sb.Append(" onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
		sb.Append(" onKeyDown=\"if(event.keyCode==13){document.f.s.focus();event.keyCode=38;}\"><font color='white'>");
		sb.Append("%");
		sb.Append("</td>");
		*/

	}
	else
	{
		sb.Append("<input type=hidden name=agent value=0><input type=hidden name=agent_discount value=0><input type=hidden name=agent_name value=''><input type=hidden name=agent_code value=0>");
	}
	sb.Append("</td><td width=10></td></tr>");
	sb.Append("</table>");
    sb.Append("</td></tr>\r\n");
	
sb.Append("<tr bgcolor=#6699CC ><td height=10 width=366 align=left><img src=\"c:/qpos/lbottom.gif\" width=10 height=10 border=0></td> <td width=100% align=right><img src=\"c:/qpos/rbottom.gif\" width=10 height=10 border=0></td></tr>");	
	sb.Append("</table>\r\n");
//*** last Column of Header  ********//
	sb.Append("<td  bgcolor='#333333' align=center width=25% >");
		//******Display Branch Name ********//
	if(m_branchName != "")
		sb.Append("<marquee scrolldelay=100 alt='moving....'><b><font size=4 color='yellow'>" +m_branchName +" Branch &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</font></b> ");	
	//******Display Branch Name ********//

	sb.Append("</marquee></td></tr>");
	//end of header
	//=============================== extention row======================
/*	sb.Append("<tr bgcolor=#6699CC ><td height=10 width=10 align=left><img src=\"c:/qpos/lbottom.gif\" width=10 height=10 border=0></td><td colspan=4 width=90%></td><td height=10 width=10 align=right  ><img src=\"c:/qpos/rbottom.gif\" width=10 height=10 border=0></td></tr>");*/
	
	
	sb.Append("<tr ><td  height=5 bgcolor=#336699 ></td><td bgcolor=#000000 height=5 style=\"border-top:1px #F0FC00 dashed\"></TD></tr>");
	sb.Append(" <tr ><td ><table width=100% cellpadding=0 cellspacing=0 border=0>");
	sb.Append("<tr ><td height=10 width=10 align=left ><img src=\"c:/qpos/ltop2.gif\" width=10 height=10 border=0></td><td width=100% bgcolor=#66CCFF ></td><td height=10 width=10 align=right  bgcolor=#336699><img src=\"c:/qpos/rtop2.gif\" width=10 height=10 border=0></td></table></td><td width =25% bgcolor=#000000 ></td></tr>");
	//sb.Append("</table></td></tr>");
	sb.Append("<tr width=75% >");
	sb.Append("<td bgcolor=#66CCFF align=left >");
    sb.Append("<table width=100% cellspacing=0 cellpadding=0 border=0>");
	sb.Append("<tr> <td>");
	string keyEnter = "onfocus=\"sh(this);\" onblur=\"rh(this);\" ";
//	keyEnter += " onKeyDown = \"if(event.keyCode==13 && document.f.s.value!='' ";
//	keyEnter += "&& document.f.s.value!='.' && document.f.s.value.indexOf('+') < 0 && document.f.s.value.indexOf('-') < 0 ";
//	keyEnter += "&& document.f.s.value.indexOf('*') < 0 && document.f.s.value.indexOf('/') < 0){event.keyCode=9;}\"";

	sb.Append("<table width= border=0 cellspacing=1 cellpadding=2 bgcolor=#66CCFF  >");
	//sb.Append("<tr bgcolor=#EEEEEE>");
	sb.Append("<tr align=center>");
	sb.Append("<td width=160 ><font color='white'><b>BARCODE:</b> </td>\r\n");
	sb.Append("<td width=50><font color='white'><b>QTY</b> </td>\r\n");
	sb.Append("<td width=110 nowrap><font color='white'><b>SELLING PRICE $</b> </td></tr>");
   /* sb.Append("<td width=100%></td></tr>");*/
	sb.Append("<tr>");
	//sb.Append("<tr bgcolor=#61AABC>");
	sb.Append("<td nowrap><input type=text name=s " + keyEnter + "></td>\r\n");
	sb.Append("<td nowrap><input type=text maxlength=4 size=3 name=md_qty "+ keyEnter +"></td>\r\n");
	sb.Append("<td nowrap>");
	sb.Append("<input type=text name=md_dollar size=10 onfocus=\"sh(this);\" onblur=\"rh(this);\" onKeyDown=\"if(event.keyCode==13 && document.f.s.value!='.' && document.f.s.value.substring(0,1)!='*'){if(document.f.member_name.value!='' &&this.value==''){window.alert('Please enter price!');document.f.md_qty.focus();}event.keyCode=9;}\">\r\n");
	sb.Append("<input type=submit name=cmdgo value='  GO!  '  onfocus=\"sh(this);\" onblur=\"rh(this);\" onclick=\"if(onscan(true)){return false;}\">\r\n");
	sb.Append("</tr></table>");
	sb.Append("</td><td>");
	//sb.Append("</td><td >&nbsp;</td></tr></table>");
	//sb.Append("<td  align=right  bgcolor=#66CCFF>");
	sb.Append("<table  border=0 cellspacing=0 cellpadding=0 ><tr><td >");
	sb.Append("<input type=hidden name=total_hold_orders value="+ m_nTotalHoldItems +">");
	if(m_bTrustedSite)
	{	
		string styleFont = "style=\"font-size:12pt;border:0; background-image:url(file:///C:/qpos/btBack.gif)\"";		
		if(m_bEnbleTouchScreenButton)
			styleFont  = "style=\"font-size:14pt;border:0; background-image:url(file:///C:/qpos/btBack.gif)\"";
		//sb.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
		sb.Append("<input type=button value=' Hold '  title='Click to hold current order' onclick=\"hold_current()\"  "+ styleFont +">");
		for(int nh=1; nh<=m_nTotalHoldItems; nh++)
		{
			//sb.Append(" <input type=button name=hold" + nh + " value=" + nh + " onclick=\"get_hold(" + nh + ");\" class=b ");
			//sb.Append(" title=\"Click to restore order " + nh + "\" ");
			//sb.Append(" style=\"visibility:hidden;border:0;font-size:24pt;font-weight:bold;background-color:#EEEEEE;\">");
			sb.Append(" <input type=button name=hold" + nh + " value=' " + nh + " ' onclick=\"get_hold(" + nh + ");\"  ");
			sb.Append(" title=\"Click to restore order " + nh + "\" ");			 
			sb.Append("  "+ styleFont +">");
		//	sb.Append(" style=\"visibility:hidden;border:1;font-size:12pt;font-weight:bold;background-color:#EEEEEE;\" "+ styleFont +">");
		}		
//sb.Append("&nbsp;<input type=image src=\"/i/delete.jpg\"   title='Click to delete current order' onclick=\"del_current(false)\" ");		
sb.Append("&nbsp;<input type=button  value=' &nbsp; ' class=b  title='Click to delete current order' onclick=\"del_current(false)\" style=\"border-style:none; border-left:none; background:url('c:/qpos/delete.jpg'); height:35px; width:35px\"");		
		sb.Append(" "+ styleFont +" >");
	}
	sb.Append("&nbsp;</td>");
	
	sb.Append("</tr></table> ");
	sb.Append("</td></tr></table>");
  
	//hidden value
	sb.Append("<input type=hidden name=miscel_code value='"+ miscellaneous_code +"'>\r\n");
	sb.Append("<input type=hidden name=open_tilt_code value='"+ opentilt +"'>\r\n");
	sb.Append("<input type=hidden name=gst_rate value='"+ m_gst +"'>\r\n");
	
	sb.Append("<script language=javascript>");
	sb.Append("document.f.s.focus();</script");
	sb.Append(">\r\n");
	
	sb.Append("</td><td width=25% bgcolor=#333333 style=\"font:bold 25px Arial; color:yellow; text-align:center\" valign=top>");
	/*sb.Append("<table width=98% cellpadding=0 cellspacing=0 border=0>");
	sb.Append("<tr bgcolor=#6699CC><td width=10 height=10><img src=\"/i/ltop.gif\" width=10 height=10 border=0></td><td width=98%></td>");
	sb.Append("<td width=10 height=10><img src=\"/i/rtop.gif\" width=10 height=10 border=0></td>");
	sb.Append("</td></tr>"); 
	sb.Append("</table>");
	*/
	
	
    sb.Append("<u>Current Item</u></td></tr>\r\n");

	sb.Append("<tr><td width=75% >");
	sb.Append("<table width=100% border=1 cellspacing=0 cellpadding=1 bordercolor='#CCCCCC' bgcolor='#ABC2CB'");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	if(m_bEnbleTouchScreenButton)
	{
		sb.Append("<tr align=center><td colspan=7>"+ m_sProductTouchScreenCache +"</td></tr>");		
	}
	sb.Append("<tr height='30' color='white' bgcolor='#6699CC' align=center><th align=><font color='white'>CODE</font></th><th ><font color='white'>DESCRIPTION</th><th><font color='white'>NORMAL PRICE</th><th><font color='white'>QTY</th>");
	sb.Append("<th><font color='white'>PRICE (inc GST)</th>  <th><font color='white'>DISCOUNT(%)</th><th><font color='white'>SUB-TOTAL</th></tr>\r\n");
	int nTotalRows = 50;
	int n = MyIntParse(GetSiteSettings("QPOS_MAX_ITEMS_PER_ORDER", "50"));
	if(n > 50)
		nTotalRows = n;
	string setStyle = "style='font-size:12pt; color=#FF0000 ' ";	
	string fontSize = "3";
	if(m_bEnbleTouchScreenButton) 
	{//reset display rows to only 20
		nTotalRows = 40;
		setStyle = "style='font-size:16pt; border=0;font-family: broadway; color=gray; background-image: url(file:///C:/qpos/btBack.gif)' ";		
		fontSize = "2";
	}
	for(int i=0; i<nTotalRows; i++)
	{
        string rowColor = "style='background-color:#ABC2CB '";
		sb.Append("<tr wrap");
        if((i%2) == 0){
            sb.Append(" bgcolor='#FFFFFF' "); rowColor = "style='background-color:#FFFFFF' ";	 }
        sb.Append("><td><input type=hidden name=rc" + i + "><input name=rb" + i + " size=13	 class=f readonly " + rowColor + "></td>");
		sb.Append("<td wrap><input size=20 name=rd" + i + " class=f readonly "+ rowColor + "></td>");
		sb.Append("<td wrap><input size=5 name=np" + i + " class=f readonly "+ rowColor + "></td>");
		sb.Append("<input type=hidden name=dp" + i + ">");
		
		sb.Append("<td wrap><input size=2 "+ rowColor +" name=rq" + i + " class=f onclick=this.select(); onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"ctd(" + i + ");\"></td>");
		sb.Append("<td wrap><input size=7 "+ rowColor +" name=rp" + i + " class=f onclick=this.select(); onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"cp(" + i + ");\"></td>");
		sb.Append("<td wrap><input size=5 "+ rowColor +" name=md" + i + " class=f onclick=this.select(); onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"cd(" + i + ");\"></td>");
//		sb.Append("<td><input size=10 name=st" + i + " class=f onchange=\"cs(" + i + ");\"><input type=hidden name=np" + i + ">");
		sb.Append("<td nowrap valign=middle width=10%><input size=9 "+ rowColor +" name=st" + i + " class=f onclick=this.select(); onfocus=\"sh(this);\" onblur=\"rh(this);\" onchange=\"cs(" + i + ");\" style=\"position: relative; top: 0\">");		
		sb.Append("<input type=button name=del" + i + " value=' X ' onclick=\"del(" + i + ")\" class=b style='visibility:hidden'>");
		sb.Append("<input type=hidden name=ro" + i + ">");
		sb.Append("</td>");
		sb.Append("</tr>\r\n");
	}
//	sb.Append("</table></td></tr>");
//	sb.Append("</table>");
//	sb.Append("</td>");

	sb.Append("</table></td>");

	
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//right hand side table
	sb.Append("<td width=25% valign=top bgcolor=#333333 algin=center>\r\n");
	sb.Append("<table width=96% border=0 cellspacing=0 cellpadding=0 bgcolor=#6699CC");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;fixed\">\r\n");
	sb.Append("<tr><td colspan=2 valign=top align=center>");
    sb.Append("<table width='100%' border='0' cellpadding='0' cellspacing='0' bordercolor='#CCCCCC' > ");
	/*sb.Append("<tr bgcolor=#6699CC><td height=10 align=left><img src=\"/i/ltop.gif\" width=10 height=10 border=0></td><td width=98%></td><td height=10 align=left><img src=\"/i/rtop.gif\" width=10 height=10 border=0></td></tr>");
  //  sb.Append("<tr><td height='45' colspan='3' align='center' bgcolor='#6699CC' class='style10'><font size=3 color=white><b>Current Item</b></font></span></td></tr>");*/
     sb.Append("<tr>");
    sb.Append("<td colspan=3 align='center' bgcolor='#333333' valign='top' >");
    sb.Append("<input type=text size=12 name=code class=f  style='font-weight:bold;text-align:center;font-size:13pt;background-color:#333333;color:yellow;'>\r\n");
	sb.Append("<br /></span><span class='style20'><textarea name=desc rows=1 cols=30 class=f style='overflow:hidden;font-size:12pt;background-color:#333333;color:Yellow;text-align:center;'></textarea>\r\n");
	sb.Append("<br /><span class='style20'><font color='white'>QTY <input type=text name=qty size=8 class=f style='font-weight:bold;text-align:center;font-size:14pt;background-color:#333333;color:yellow;'>\r\n");
	sb.Append("<br /><span class='style20'><font color='white'>SUB-TOTAL &nbsp;&nbsp;<input type=text name=price size=8 class=f style='font-weight:bold;text-align:center;font-size:14pt;background-color:#333333;color:yellow;'></td></tr>\r\n");
	//sb.Append("<tr><td><textarea name=sspace rows=2 cols=10 class=f style='overflow:hidden;background-color:#EEEEEE'></textarea></td></tr>\r\n");
    sb.Append("</table>");
	sb.Append("<tr><td nowrap colspan=2 align=center>");
    sb.Append("<table width='98%' border='0' cellpadding='0' cellspacing='0' bordercolor='#CCCCCC'> ");
    sb.Append("<tr><td align='center' bgcolor='#FFFFFF' height='1' style=\"font:bold 16px arial; color:#FFFFFF\"></td></tr><tr bordercolor='#003366'>");
    sb.Append("<td height='130' align='center' bgcolor='#333333' style=\"font:bold 25px Arial; color:yellow\"><u>Total</u><br><br><input type=text name=total_display size=14 value=0 class=f style='font-weight:bold;text-align:center;font-size:30pt;background-color:#333333;color:yellow;'\">");
	sb.Append("<br /><span style=\"font:13px Arial; color:#FFFFFF\">inc. GST:</span><input type=text name=total_without_gst ");	
	sb.Append(" size=8 class=f style='font-weight:bold;text-align:center;font-size:16pt;background-color:#333333;color:yellow;'>  </span></td></tr></table>");

 //   sb.Append("<font size=+2><b>Total :</b></font></td></tr><tr><td colspan=2 align=left>");
//	sb.Append("<input type=text name=total_display size=12 value=0 class=f style='font-weight:bold;text-align:right;font-size:18pt;background-color:#EEEEEE'\">");
//	sb.Append("<input type=text name=total size=5 value=0 style='font-weight:bold;text-align:right;font-size:10pt' onfocus=\"sh(this);\" onblur=\"rh(this);\">");
//	sb.Append("<input type=button name=button_ok value=OK onfocus=\"sh(this);\" onblur=\"rh(this);\" onclick=\"CalcTotalDiscount();\" class=b>");
	sb.Append("</td></tr>\r\n");
	
//	sb.Append("<tr><td colspan=2><br></td></tr>\r\n");
    //sb.Append("<tr><td colspan=2><textarea name=sspace rows=1 cols=10 class=f style='overflow:hidden;background-color:#EEEEEE'></textarea></td></tr>\r\n");

	if(m_bDisplayCurrencyExchange)
		sb.Append(BuildCurrencyExchange());
	else
	{
		sb.Append("<input type=hidden name=exRate value=1>");
		sb.Append("<input type=hidden name=currency value=0>");
		sb.Append("<input type=hidden name=totalfCurrency value=''>");
		sb.Append("<input type=hidden name=totalReceived value=''>");
		sb.Append("<input type=hidden name=totalExchangeChanged value=''>");		
		sb.Append("<input type=hidden name=totalChangedNZD value=''>");
		sb.Append("<input type=hidden name=moreCurrency value=''>");
		sb.Append("<input type=hidden name=moreMoney value='0'>");
		sb.Append("<input type=hidden name=moreExRate value='`'>");
	}
	sb.Append("<input type=hidden name=payByFCurrency value='0'>");
	//******foreign currency exchange End here **********
//sb.Append("<tr><td colspan=1><textarea name=sspace rows=1 cols=10 class=f style='overflow:hidden;background-color:#EEEEEE'></textarea></td></tr>\r\n");
//***** payments layout here *****//
    sb.Append("<input type=hidden name=pm value='cash'>\r\n");

	sb.Append("<input type=hidden name=payment_total value=0>\r\n");
	sb.Append("<input type=hidden name=payment_cash value=>\r\n");
	sb.Append("<input type=hidden name=payment_eftpos value=>\r\n");
	sb.Append("<input type=hidden name=payment_cheque value=>\r\n");
	sb.Append("<input type=hidden name=payment_cc value=>\r\n");
	sb.Append("<input type=hidden name=payment_bankcard value=>\r\n");
	sb.Append("<input type=hidden name=cash_out value=>\r\n");
	sb.Append("<input type=hidden name=pmt>");
	sb.Append("<tr><td colspan=2>");
    sb.Append("<table border='0' cellpadding='0' cellspacing='0'  bgcolor:#333333  >");
    sb.Append("<tr><td height='1' colspan='2' align='center' bgcolor=#FFFFFF ></td> ");
    sb.Append("</tr>");
	sb.Append("<tr><td colspan=2 align=center bgcolor=#333333 height=40 valign=middle style=\"font:bold 25px arial; color:yellow\"><u>Payments</u></td></tr>");
    sb.Append("<input type=hidden name=total value=''>");
	
    sb.Append("<input type=hidden name=tcashin >\r\n");
	sb.Append("<tr><td height='40' width='150' align=right valign=middle style=\"font:20px arial; color:yellow; background:#333333; border-top:1px yellow solid; border-left:1px yellow solid\" >CASH&nbsp;&nbsp;</td>\r\n");
	sb.Append("<td align=right bgcolor='#333333' width='80%' style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid\"><input type=text name=cashin size=5 class=f style='background-color:#333333;text-align:right;font-weight:bold;font-size:20'");
	sb.Append(" onfocus=\"if(document.f.payByFCurrency.value != '0'){document.f.cashin.value=document.f.payByFCurrency.value;}else{this.value=getRoundUpPrice(this.name);} sh(this); cleanUpEnteredValue(this.name, this.value, getRoundUpPrice(this.name)); document.f.cashin.value=this.value; this.focus(); this.select();\" onblur=\"rh(this); \" ");
	sb.Append(" onchange=\"document.f.payment_cash.value=this.value;\" ");
	sb.Append(" onKeyDown=\"document.f.payment_cash.value=this.value; if(event.keyCode==38){document.f.chequein.focus(); return false;}else if(event.keyCode==40){ document.f.eftposin.focus();return false;}");
	sb.Append(" else if(event.keyCode==13){document.f.cashin.value=this.value; if(this.value==''){event.keyCode=9;}else{document.f.cmd.focus();}}\"");	
	sb.Append("></td></tr>\r\n");

    sb.Append("<tr><td height='40' width='150' align=right valign=middle style=\"font:20px arial; color:yellow; background:#333333; border-top:1px yellow solid; border-left:1px yellow solid\" >EFTPOS&nbsp;&nbsp;</td>\r\n");
	sb.Append("<td align=right bgcolor='#333333' width='80%' style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid\"><input type=text name=eftposin size=5 class=f style='background-color:#333333;text-align:right;font-weight:bold;font-size:20'");
	sb.Append(" onfocus=\"this.value=getRoundUpPrice(this.name); sh(this); cleanUpEnteredValue(this.name, this.value, getRoundUpPrice(this.name)); document.f.eftposin.value=this.value; this.focus(); this.select();\" onblur=\"rh(this); \" ");
	sb.Append(" onchange=\" document.f.payment_eftpos.value=this.value; \" ");
	sb.Append(" onKeyDown=\"document.f.payment_eftpos.value=this.value; if(event.keyCode==38){document.f.cashin.focus();return false;}else if(event.keyCode==40){document.f.cashoutin.focus();return false;}");
	sb.Append(" else if(event.keyCode==13){if(this.value==''){event.keyCode=9;}else{document.f.cmd.focus();}}\"");
	sb.Append("></td></tr>\r\n");

	sb.Append("<tr><td height='40' width='150' align=right valign=middle style=\"font:20px arial; color:yellow; background:#333333; border-top:1px yellow solid; border-left:1px yellow solid\" >CASH OUT &nbsp</td>\r\n");
	sb.Append("<td align=right bgcolor='#333333' width='80%' style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid\"><input type=text name=cashoutin size=5 class=f style='background-color:#333333;text-align:right;font-weight:bold;font-size:20'");
	sb.Append(" onfocus=\" cleanUpEnteredValue(this.name, this.value, getRoundUpPrice(this.name)); sh(this);this.select(); \" onblur=\"rh(this);\" ");
	sb.Append(" onchange=\"document.f.cash_out.value=this.value;\" ");
	sb.Append(" onKeyDown=\"if(event.keyCode==38){document.f.eftposin.focus();return false;}else if(event.keyCode==40){document.f.ccin.focus();return false;}");
	sb.Append(" else if(event.keyCode==13){if(this.value==''){event.keyCode=9;}else{document.f.cmd.focus();}}\"");
	sb.Append("></td></tr>\r\n");

	sb.Append("<tr><td height='40' width='150' align=right valign=middle style=\"font:bold 15px arial; color:yellow; background:#333333; border-top:1px yellow solid; border-left:1px yellow solid\" >CREDIT CARD&nbsp;&nbsp</td>\r\n");
	sb.Append("<td align=right bgcolor='#333333' width='80%' style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid\"><input type=text name=ccin size=5 class=f style='background-color:#333333;text-align:right;font-weight:bold;font-size:20'");
	sb.Append(" onfocus=\"this.value=autoFillUpValue(this.name, this.value, getRoundUpPrice(this.name)); sh(this); cleanUpEnteredValue(this.name, this.value, getRoundUpPrice(this.name)); this.focus(); this.select();\" onblur=\"rh(this);\" ");
	sb.Append(" onchange=\"document.f.payment_cc.value=this.value;\" ");
	sb.Append(" onKeyDown=\" document.f.payment_cc.value=this.value; if(event.keyCode==38){document.f.cashoutin.focus();return false;}else if(event.keyCode==40){document.f.chequein.focus();return false;}");
	sb.Append(" else if(event.keyCode==13){if(this.value==''){event.keyCode=9;}else{document.f.cmd.focus();}}\"");
	sb.Append("></td></tr>\r\n");

	sb.Append("<tr><td height='40' width='150' align=right valign=middle style=\"font:20px arial; color:yellow; background:#333333; border-top:1px yellow solid; border-left:1px yellow solid\" >CHEQUE&nbsp;&nbsp;</td>\r\n");
	sb.Append("<td align=right bgcolor='#333333' width='80%' style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid\"><input type=text name=chequein size=5 class=f style='background-color:#333333;text-align:right;font-weight:bold;font-size:20'");
	//sb.Append(" onfocus=\"this.value=getRoundUpPrice(this.name); sh(this);  cleanUpEnteredValue(this.name, this.value, getRoundUpPrice(this.name));  this.focus(); this.select();\" onblur=\"rh(this);\" ");
	sb.Append(" onfocus=\"this.value=autoFillUpValue(this.name, this.value, getRoundUpPrice(this.name)); sh(this); cleanUpEnteredValue(this.name, this.value, getRoundUpPrice(this.name)); this.focus(); this.select();\" onblur=\"rh(this);\" ");
	sb.Append(" onchange=\"document.f.payment_cheque.value=this.value;\" ");
	sb.Append(" onKeyDown=\" document.f.payment_cheque.value=this.value; if(event.keyCode==38){document.f.ccin.focus();return false;}else if(event.keyCode==40){document.f.cashin.focus();return false;}");
	sb.Append(" else if(event.keyCode==13){if(this.value==''){document.f.cashin.focus;return false;}else{document.f.cmd.focus();}}\"");
	sb.Append("></td></tr>\r\n");

	sb.Append("<tr><td height='50' width='150' align=right valign=middle style=\"font:20px arial; color:yellow; background:#333333; border-top:1px yellow solid; border-left:1px yellow solid\" ><input type=text size=7 class=f name=tcashchange value='CHANGE' style='font-size:22;font-weight:bold;text-align:right;background:#333333; color:66CCCC'></td>");
	sb.Append("<td align=right bgcolor='#333333' width='80%' style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid\"><input type=text name=cashchange size=8 class=f style='background-color:yellow;text-align:right;font-weight:bold;font:bold 40px arial black;color:red'></td></tr>\r\n");
	sb.Append("<tr bgcolor=#333333><td colspan=2 align=center style=\"border-top:1px yellow solid; border-right:1px yellow solid; border-left:1px yellow solid; border-bottom:1px yellow solid \">");
	//*** stop multiple enter invoice
	sb.Append("<input type=hidden name=refreshPage value=''>");
	sb.Append("<input type=hidden name=inv>");
	sb.Append("<input type=submit style='border:0px; padding: 8px;font-size:16pt;border-style:ridge; color:yellow; background-color:#333333 '");
	sb.Append("name='cmd'  value=' CHECK-OUT '  class=b onclick=\"if(paymentok()){if(window.confirm('Confirm to checkout?') && document.f.refreshPage.value==''){return WriteOrder();}else{clear_payment(); cleanUpEnteredValue(); return false;}}else{return false;}\">");

	sb.Append("</td></tr>\r\n");

	sb.Append("</table>");
	
	sb.Append("</tr></table>");
	sb.Append("</td>");
	string sj = PrintPaymentJava(true);
	sb.Append(sj);

	sb.Append("</td></tr>");
	//end payments

	sb.Append("</table>\r\n");
	sb.Append("</td>");
//end right side
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	sb.Append("</tr></table>");
	if(!m_bTrustedSite)
	{
		string sbcn = sb.ToString();
//		sbcn = sbcn.Replace("else{LoadPaymentForm();return false;}", ""); //not for online
		Response.Write(sbcn);
		return true;
	}
	//all hta functions followed

	sb.Append("<script language=javascript>\r\n");
//	sb.Append("<!-- ");

//	string s = " var scart = '" + m_sReceiptPrinterObject + "';\r\n";
	string s = "";
	s += @"
var hh = new Array();
refresh_holds();
var total_holds = hh.length;
var current_number = total_holds + 1;
show_hold_list();

function check_special_printer_port()
{
	var sport = '';
	fn = 'c:/qpos/p_port.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
	{
		tf = fso.OpenTextFile(fn, 1, false, -1); 
		try
		{
			sport = tf.ReadAll();
		}
		catch(err)
		{
		}
		tf.Close(); 
	}
	if(sport != '')
		document.f.printer_port.value = sport;
}
check_special_printer_port();
function refresh_holds()
{
	hh = new Array();
	var forders = 'c:/qpos/orders.txt'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(!fso.FileExists(forders))
	{
		var tf = fso.OpenTextFile(forders , 8, 1, -1);
		tf.Write('');
		tf.Close();
	}
	else
	{
		var tf = fso.OpenTextFile(forders, 1, false, -1); 
		while(!tf.AtEndOfStream)
		{
			var sline = tf.ReadLine();
			if(sline != '')
			{
//				window.alert(sline);
				eval('hh.push([' + sline + '])');
			}
		}
		tf.Close();
	}
}
function show_hold_list()
{
	var total_hold_orders = document.f.total_hold_orders.value;
	for(var m=1; m<=Number(total_hold_orders); m++)
	{
		if(m > total_holds+1)
";
s += "			eval(\"document.f.hold\" + m + \".style.visibility='hidden'\"); ";
s += "		else ";
s += "			eval(\"document.f.hold\" + m + \".style.visibility='visible'\"); ";
s += "		if(m == current_number) ";
s += "			eval(\"document.f.hold\" + m + \".style.color='#F5362E'\"); ";
s += "		else ";
s += "			eval(\"document.f.hold\" + m + \".style.color='black'\"); ";
s += @"
	}
}
function get_hold(nNumber)
{
	if(nNumber > hh.length+1)
		return;
	else if(nNumber > hh.length)
	{
		current_number = nNumber;
		clear_form_cart();
		show_hold_list();
		return;
	}
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
			break;
		";
s += "		eval(\"document.f.rc\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rb\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rd\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.ro\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.dp\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rp\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.rq\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.md\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.np\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.st\" + n + \".value = ''\"); \r\n";
s += "		eval(\"document.f.del\" + n + \".style.visibility='hidden'\"); \r\n";
s += @"
	}
	var cart = hh[nNumber-1];
	var gh_code = '';
	var gh_barcode = '';
	var gh_sdesc = '';
	var gh_sgprice = '';
	var gh_sinb = '';
	for(var n=0; n<cart.length; n++)
	{
		gh_code = cart[n][0];
		gh_barcode = cart[n][1];
		gh_sdesc = cart[n][2];
		gh_sprice = cart[n][4];
";
s += "		eval(\"document.f.rc\" + n + \".value = cart[n][0]\"); \r\n ";
s += "		eval(\"document.f.rb\" + n + \".value = cart[n][1]\"); \r\n ";
s += "		eval(\"document.f.rd\" + n + \".value = cart[n][2]\"); \r\n ";
s += "		eval(\"document.f.ro\" + n + \".value = cart[n][3]\"); \r\n ";
s += "		eval(\"document.f.dp\" + n + \".value = cart[n][4]\"); \r\n ";
s += "		eval(\"document.f.rp\" + n + \".value = cart[n][5]\"); \r\n ";
s += "		eval(\"document.f.rq\" + n + \".value = cart[n][6]\"); \r\n ";
s += "		eval(\"document.f.md\" + n + \".value = cart[n][7]\"); \r\n ";
s += "		eval(\"document.f.np\" + n + \".value = cart[n][8]\"); \r\n ";
s += "		eval(\"document.f.st\" + n + \".value = cart[n][9]\"); \r\n ";
s += "		eval(\"document.f.del\" + n + \".style.visibility='visible'\"); \r\n ";
s += @"
	}
	document.f.code.value = gh_code;
	document.f.desc.value = gh_sdesc;
	document.f.price.value = gh_sgprice;
	document.f.last_barcode.value = gh_barcode;
	document.f.last_code.value = gh_code;
	document.f.rows.value = cart.length;

	ct();
	document.f.s.value='';
	document.f.md_qty.value='';
	document.f.md_dollar.value='';
	document.f.s.focus();
	current_number = nNumber;
	show_hold_list();

}
function hold_current()
{
	var total_hold_orders = document.f.total_hold_orders.value;
	if(total_holds >= Number(total_hold_orders))
	{
		window.alert('Sorry I can hold no more orders.');
		return;
	}
	if(!window.confirm('Are you sure to hold current order?'))
		return;
	var cart = '';
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
		{
			if(n == 0)
				return;
			else
				break;
		}
		var rb = eval('document.f.rb' + n + '.value');
		var rd = eval('document.f.rd' + n + '.value');		
		var ro = eval('document.f.ro' + n + '.value');
		var dp = eval('document.f.dp' + n + '.value');
		var rp = eval('document.f.rp' + n + '.value');
		var rq = eval('document.f.rq' + n + '.value');
		var md = eval('document.f.md' + n + '.value');
		var np = eval('document.f.np' + n + '.value');
		var st = eval('document.f.st' + n + '.value');
		if(n > 0)
			cart += ',';
";
s += "		cart += '[' + rc + ',' + rb + ',\"' + rd + '\",' + ro + ',' + dp + ',' + rp + ',' + rq + ',' + md + ',' + np + ',' + st + ']'; ";
s += @"
	}
	eval('hh.push([' + cart + '])');
	total_holds++;
	current_number = total_holds + 1;
	flush_holds();
	show_hold_list();
	clear_form_cart();
}
function clear_form_cart()
{
	for(var n=0; n<50; n++)
	{
		var rc = eval('document.f.rc' + n + '.value');
		if(rc == '')
			break;
		";
s += "		eval(\"document.f.rc\" + n + \".value = ''\");";
s += "		eval(\"document.f.rb\" + n + \".value = ''\");";
s += "		eval(\"document.f.rd\" + n + \".value = ''\");";
s += "		eval(\"document.f.ro\" + n + \".value = ''\");";
s += "		eval(\"document.f.dp\" + n + \".value = ''\");";
s += "		eval(\"document.f.rp\" + n + \".value = ''\");";
s += "		eval(\"document.f.rq\" + n + \".value = ''\");";
s += "		eval(\"document.f.md\" + n + \".value = ''\");";
s += "		eval(\"document.f.np\" + n + \".value = ''\");";
s += "		eval(\"document.f.st\" + n + \".value = ''\");";
s += "		eval(\"document.f.del\" + n + \".style.visibility='hidden'\");";
s += @"
	}
	document.f.code.value = '';
	document.f.desc.value = '';
	document.f.price.value = '';
	document.f.last_barcode.value = '';
	document.f.last_code.value = '';
	document.f.rows.value = 0;

	ct();
	document.f.s.value='';
	document.f.md_qty.value='';
	document.f.md_dollar.value='';
	document.f.s.focus();
}
function flush_holds()
{
	var forders = 'c:/qpos/orders.txt'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(forders))
		fso.DeleteFile(forders);
	fso = new ActiveXObject('Scripting.FileSystemObject');
	var tf = fso.OpenTextFile(forders , 8, 1, -1);
	for(var n=0; n<hh.length; n++)
	{
		var sline = '';
		var cart = hh[n];
		for(var m=0; m<cart.length; m++)
		{
			if(m > 0)
				sline += ',';
			sline += '[';
			for(var t=0; t<cart[m].length; t++)
			{
				if(t > 0)
					sline += ',';
				if(t == 2)
";
s += "					sline += '\"' + cart[m][t] + '\"'; ";
s += @"
				else
					sline += cart[m][t];
			}
			sline += ']';
		}
		tf.WriteLine(sline);
	}
	tf.Close();
	refresh_holds();
}
function del_current(bCheckingout)
{
//	window.alert(' rows=' + document.f.rows.value);
//	window.alert('current_number=' + current_number + ' total_holds=' + total_holds);
	if(current_number > total_holds)
		return;
	if(document.f.rows.value == '0')
		return;
	if(!bCheckingout)
	{
		if(!window.confirm('Are you sure to delete current order?'))
			return;
	}
	hh.splice(current_number - 1, 1);
	flush_holds();
	if(bCheckingout)
		return;
	total_holds--;
	if(current_number > total_holds+1)
		current_number = total_holds + 1;
	clear_form_cart();
	show_hold_list();
}
function reload_offline_app()
{
	var fn = 'c:/qpos/qpos.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false, -1); 
	var s = tf.ReadAll(); 
	tf.Close(); 
	document.close();
	document.write(s);
	return true;
}

///***** currency exchange display function START HERE *****
function calExCurrency(rates, moreRates)
{	
	document.f.exRate.value=rates;
	document.f.moreExRate.value = moreRates;
	document.f.totalfCurrency.value=Number(eval(document.f.total_display.value)*eval(rates)).toFixed(2);
	var totalReceived = document.f.totalReceived.value;	
	if(totalReceived == null || totalReceived == ' ' || totalReceived == '' || totalReceived == 'NaN')
		totalReceived = '0';	
	document.f.totalExchangeChanged.value=Number(eval(totalReceived) - eval(document.f.totalfCurrency.value)).toFixed(2);
	
	//window.alert('rece ='+ totalReceived +' rates= ' + rates);
	var moreMoney = document.f.moreMoney.value; 
	var moreExRate = document.f.moreExRate.value;
	if(moreMoney == null || moreMoney == ' ' || moreMoney == '' || moreMoney == 'NaN')
		moreMoney = '0';	

	if(moreExRate == null || moreExRate == ' ' || moreExRate == '' || moreExRate == 'NaN')
		moreExRate = '1';	
//	window.alert('calType =' + calType);
	var moreChange;
//	if(calType == 'true' || calType){
		moreChange = Number(eval(moreMoney)/eval(moreExRate)).toFixed(2)
//	}
	document.f.totalChangedNZD.value=Number((eval(document.f.totalExchangeChanged.value) / eval(document.f.currency.value)) + eval(moreChange)).toFixed(2);	
	document.f.cashin.value = Number(eval(moreChange) + (eval(totalReceived)/eval(rates))).toFixed(2);
	document.f.payByFCurrency.value = Number(eval(moreChange) + (eval(totalReceived)/eval(rates))).toFixed(2);
}

///***** currency exchange display function END HERE *****

		";
	sb.Append(s);
	sb.Append("</script");
	sb.Append(">\r\n ");

	string sbc = sb.ToString();
	m_shta += sbc.Replace("shelp = shelp.replace(re, '\\r\\n');", ""); //not for offline
//	sbc = sbc.Replace("else{LoadPaymentForm();return false;}", ""); //not for online
//	Response.Write(sbc);
//	Response.Write("<a href=qpos.aspx?t=chl class=o>Download Offline Tools(Create shortcut on desktop)</a>");
	return true;
}

bool BuildSalesBarcodeCache()
{
	int rows = 0;
	DataSet dsbc = new DataSet();
	string sc = " SELECT id, name, barcode FROM card WHERE type=4 AND barcode IS NOT NULL AND barcode <> '' ORDER BY name ";
//DEBUG("sc = ",sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsbc);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	StringBuilder sb = new StringBuilder();

	for(int i=0; i<rows; i++)
	{
		DataRow dr = dsbc.Tables[0].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		string barcode = dr["barcode"].ToString();
		if(barcode == "")
			continue;

		sb.Append("\r\n<input type=hidden name='sbb" + barcode + "' value=" + id + ">");
		sb.Append("\r\n<input type=hidden name='sbn" + id + "' value='" + name + "'>");
	}
	m_sSalesBarcodeCache = sb.ToString();
	return true;
}

bool BuildMemberCache()
{
	if(dst.Tables["member"] != null)
		dst.Tables["member"].Clear();

	string sc = " SELECT id, name, phone, points FROM card WHERE type = 6 AND phone <> '' ORDER BY name";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "member");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<script language=javascript>function CheckMemberCode(){ \r\n");

	sb.Append(" var i = new Array(); \r\n");
	int n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<dst.Tables["member"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["member"].Rows[i];
		string id = dr["id"].ToString();
		string phone = dr["phone"].ToString();
		string srow = "i[\"" + phone + "\"]=\"" + id + "\";";
		sb.Append(srow);
		n += srow.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}	
	sb.Append(" var n = new Array(); \r\n");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<dst.Tables["member"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["member"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		name = name.Replace("\"", "`");
		string srow = "n[" + id + "]=\"" + name + "\";";
		sb.Append(srow);
		n += srow.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(" var p = new Array(); \r\n");
	
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<dst.Tables["member"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["member"].Rows[i];
		string id = dr["id"].ToString();
		string points = dr["points"].ToString();				
		string srow = "p[" + id + "]=\"" + points + "\";";			
		sb.Append(srow);
		n += srow.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append("\r\n");		
	sb.Append(" var phone = document.f.member_code.value; \r\n");
	sb.Append(" if(i[phone] != null){var id = i[phone]; document.f.member_code.value=phone; document.f.member.value = id; document.f.member_name.value = n[id]; document.f.member_points_current.value = p[id]; } \r\n");
//	sb.Append(" window.alert(p[id]); ");
	sb.Append("} </script");
	sb.Append(">");

	m_sMemberCache = sb.ToString();
	return true;
}

bool BuildProductCache()
{
	int rows = 0;
	DataSet dspc = new DataSet();
	string sc = " SELECT DISTINCT code, name, RTRIM(LTRIM(barcode)) AS barcode ";
//	if(m_bFixedPrices)
		sc += ", price1 ";
		if(m_bEnableGSTCost)
			sc += "/"+ m_gst +" ";
		sc += " AS price, ISNULL(special_price, price2) AS price2, qpos_qty_break AS qty_break1 "; //GST inclusive price
	sc += ", supplier_code ";
//	else
//		sc += ", supplier_price * rate * level_rate1 AS price ";
//	sc += ", RTRIM(LTRIM(package_barcode1)) AS package_barcode1, package_qty1, package_price1 ";
//	sc += ", RTRIM(LTRIM(package_barcode2)) AS package_barcode2, package_qty2, package_price2 ";
//	sc += ", RTRIM(LTRIM(package_barcode3)) AS package_barcode3, package_qty3, package_price3 ";
	sc += " FROM code_relations ";
//	sc += " WHERE code = 5196 ";
//	sc += " where price1 <> 0 ";
	sc += " WHERE skip = 0 ";
	sc += " ORDER BY code ";
//DEBUG(" sc-", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dspc, "item");
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid column name 'qpos_qty_break'") >= 0)
		{
			myConnection.Close(); //close it first

			string ssc = @"
				
			ALTER TABLE [dbo].[code_relations] ADD [qpos_qty_break] [int] DEFAULT(0) not null										
		
			";
	//	DEBUG("ssc = ", ssc);
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
			//	ShowExp(sc, er);
				return false;
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +" \">");		
		}
		ShowExp(sc, e);
		return false;
	}

	int nMax = 1000;
	StringBuilder sb = new StringBuilder();
	sb.Append("<script language=javascript>\r\n");
	sb.Append("function searchitem(type, index, barcode){\r\n");
	sb.Append(" var b = new Array(");
	int n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["item"].Rows[i];
		string name = dr["name"].ToString();
//	DEBUG(" name = ", name);
		Trim(ref name);
		if(name == "")
			continue;
		string barcode = dr["barcode"].ToString().ToLower();
		if(barcode == null || barcode == "")
			barcode = dr["code"].ToString().ToLower();
		barcode = barcode.Replace("(", " ");
		barcode = barcode.Replace(")", " ");

		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + barcode + "\"");
		n += 2 + barcode.Length + 3;
		if(n > nMax)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var c = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["item"].Rows[i];
		string name = dr["name"].ToString();
//DEBUG(" name2 = ", name);
		Trim(ref name);
		if(name == "")
			continue;
		string code = dr["code"].ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
//		sb.Append("\"" + code + "\"");
		sb.Append(code);
		n += code.Length + 3;
		if(n > nMax)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var p = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["item"].Rows[i];
		string name = dr["name"].ToString();
//DEBUG(" name3 = ", name);
		Trim(ref name);
		if(name == "")
			continue;
//		string price = Math.Round(MyDoubleParse(dr["price"].ToString()), 2).ToString();
		string price = dr["price"].ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + price + "\"");
//		sb.Append(price);
		n += price.Length + 3;
		if(n > nMax)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var p2 = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["item"].Rows[i];
		string name = dr["name"].ToString();
//DEBUG(" name 4= ", name);
		Trim(ref name);
		if(name == "")
			continue;
//		string price = Math.Round(MyDoubleParse(dr["price2"].ToString()), 2).ToString();
		string price = dr["price2"].ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + price + "\"");
		n += price.Length + 3;
		if(n > nMax)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var q = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["item"].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string qty = dr["qty_break1"].ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + qty + "\"");
		n += qty.Length + 3;
		if(n > nMax)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var n = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["item"].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;

//		if(name.Length > 24)
//			name = name.Substring(0, 24);
		name = name.Replace("\"", "");
		name = name.Replace("\r\n", " ");
		name = name.Replace("'", "");
		name = name.Replace("(", " ");
		name = name.Replace(")", " ");
	/*	name = name.Replace("&", " ");
		name = name.Replace("!", " ");
		name = name.Replace("@", " ");
		name = name.Replace("#", " ");		
		name = name.Replace("%", " ");
		name = name.Replace("^", " ");
		name = name.Replace("~", " ");
		name = name.Replace("`", " ");
		name = name.Replace("}", " ");
		name = name.Replace("{", " ");
		name = name.Replace("]", " ");
		name = name.Replace("[", " ");
		*/		
		//DEBUG(" decode name 5= ", name);
		//name = HttpUtility.UrlDecode(name);
	//	DEBUG(" encode name 5= ", name);
		string supplier_code = dr["supplier_code"].ToString();		
		string code = dr["code"].ToString();

		if(i > 0)
		{
			sb.Append(",");
			n += 1;
		}
//		if((supplier_code.ToUpper()).IndexOf("G-") >= 0)
//			sb.Append("\"" + supplier_code + " - " + name + "\"");
//		else
//		sb.Append("\"" + code + " " + name + "\"");
		sb.Append("\"" + name + "\"");
		n += 2 + name.Length + 3;
		if(n > nMax)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");

	sb.Append(" if(barcode != ''){\r\n");
	sb.Append(" for(var i=0; i<b.length; i++){if(b[i] == barcode)return i} \r\n");
	sb.Append(" return -1; ");
	sb.Append("} \r\n");
	sb.Append(" else if(type == 'code'){return c[index];} \r\n");
	sb.Append(" else if(type == 'price'){return p[index];} \r\n");
	sb.Append(" else if(type == 'price2'){if(p2[index] > 0){return p2[index];}else{return p[index];}} \r\n");
	sb.Append(" else if(type == 'name'){return n[index];} \r\n");
	sb.Append(" else if(type == 'qty'){return q[index];} \r\n");

	sb.Append("}\r\n");
	sb.Append("</script");
	sb.Append(">\r\n");

	//package barcodes
	sc = " SELECT name, RTRIM(LTRIM(barcode)) AS barcode ";
	sc += ", RTRIM(LTRIM(package_barcode1)) AS package_barcode, package_qty1 AS package_qty, package_price1 AS package_price ";
	sc += " FROM code_relations WHERE package_barcode1 IS NOT NULL AND package_barcode1 <> '' AND package_qty1 <> 0 AND package_price1 <> 0 ";
	sc += " ORDER BY package_barcode1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dspc, "package");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = " SELECT name, RTRIM(LTRIM(barcode)) AS barcode ";
	sc += ", RTRIM(LTRIM(package_barcode2)) AS package_barcode, package_qty2 AS package_qty, package_price2 AS package_price ";
	sc += " FROM code_relations WHERE package_barcode2 IS NOT NULL AND package_barcode2 <> '' AND package_qty2 <> 0 AND package_price2 <> 0 ";
	sc += " ORDER BY package_barcode2 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows += myAdapter.Fill(dspc, "package");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = " SELECT name, RTRIM(LTRIM(barcode)) AS barcode ";
	sc += ", RTRIM(LTRIM(package_barcode3)) AS package_barcode, package_qty3 AS package_qty, package_price3 AS package_price ";
	sc += " FROM code_relations WHERE package_barcode3 IS NOT NULL AND package_barcode3 <> '' AND package_qty3 <> 0 AND package_price3 <> 0 ";
	sc += " ORDER BY package_barcode3 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows += myAdapter.Fill(dspc, "package");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sb.Append("<script language=javascript>\r\n");
	sb.Append("function searchpackage(barcode){\r\n");
	sb.Append(" var pb = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["package"].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string package_barcode = dr["package_barcode"].ToString();
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + package_barcode + "\"");
		n += 2 + package_barcode.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var pbib = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["package"].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string barcode = dr["barcode"].ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + barcode + "\"");
		n += barcode.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var pq = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["package"].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string package_qty = dr["package_qty"].ToString().ToLower();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + package_qty + "\"");
		n += package_qty.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" var pp = new Array(");
	n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["package"].Rows[i];
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		string price = Math.Round(MyDoubleParse(dr["package_price"].ToString()), 4).ToString();
		
		if(i > 0)
		{
			sb.Append(",");
			n++;
		}
		sb.Append("\"" + price + "\"");
		n += price.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}
	sb.Append(")\r\n");
	sb.Append(" if(barcode != ''){\r\n");
	sb.Append(" for(var i=0; i<pb.length; i++){if(pb[i] == barcode){ \r\n");
	sb.Append(" document.f.s.value = pbib[i]; document.f.md_qty.value = pq[i]; document.f.md_dollar.value = 't' + pp[i];\r\n");
	sb.Append(" document.f.last_barcode.value='package'; document.f.last_is_package.value=1; onscan(false); return true;\r\n");
	sb.Append("}} \r\n");
	sb.Append(" document.f.last_is_package.value=0; return false; ");
	sb.Append("} \r\n");

	sb.Append("}\r\n");
	sb.Append("</script");
	sb.Append(">\r\n");

	m_sProductCache = sb.ToString();
//	Session["pos_product_cache"] = m_sProductCache;
	return true;
}

string PrintMJava()
{

	string s = "<script language=javascript>\r\n";

	if(!m_bDebug)
	{
		s += "self.moveTo(-4,-4);\r\n";
		s += "self.resizeTo((screen.availWidth+8),(screen.availHeight+8));\r\n";
	}

	ASCIIEncoding encoding = new ASCIIEncoding( ); 
	byte[] select_display = {0x1b, 0x3d, 0x00, 0x02};
	byte[] select_printer = {0x1b, 0x3d, 0x00, 0x01};
	byte[] clear_screen = {0x00, 0x0c};
	byte[] move_home = {0x1f, 0x24, 0x01, 0x01};
	byte[] move_left_most = {0x08, 0x08, 0x08, 0x08, 0x08};
	byte[] init_printer = {0x1b, 0x40};
	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
//	byte[] kick = {0x1b, 0x70, 0x00, 0x19, 0x7f, 0x00};//, 0x0a, 0x0};//new char[6];
	string ssdisplay = encoding.GetString(select_display);
	string ssprinter = encoding.GetString(select_printer);
	string sclear = encoding.GetString(clear_screen);	
	string smovehome = encoding.GetString(move_home);
	string smoveleftmost = encoding.GetString(move_left_most);
    string kickout = encoding.GetString(kick);// + "\\\\r\\\\n";
    string sinit = encoding.GetString(init_printer);

	if(m_bDisplay)
	{
//		s += " var s_display = 'Welcome to " + m_sCompanyTitle + "'; \r\n";
		s += " var s_display = '" + m_sCompanyTitle + "'; \r\n";
	}

	s += " var ssprinter = '" + ssprinter + "'; \r\n";
	s += @"
var shelp = 'key : function\\r\\n';
shelp += 'a : Search Member\\r\\n';
shelp += 'b : Search Agent\\r\\n';
shelp += 's : Sales Price Reference\\r\\n';
shelp += 'o : Order List\\r\\n';
shelp += 'c : Focus Barcode Field/Calculator\\r\\n';
shelp += 'h : Hold Current Order\\r\\n';
shelp += 'd : Delete Current Order\\r\\n';
shelp += 'g : Print Agent Summary\\r\\n';
shelp += 'u : Print Last Receipt\\r\\n';
shelp += 'z : Open Cash Draw\\r\\n';
shelp += '. : Delete Last Item\\r\\n';
shelp += 'm : Enter Member ID\\r\\n';
shelp += 'n : New Order\\r\\n';
shelp += '* : Set Last Qty to \\r\\n';
shelp += 'q : Get OnHold Order 1 \\r\\n';
shelp += 'w : Get OnHold Order 2 \\r\\n';
shelp += 'e : Get OnHold Order 3 \\r\\n';
shelp += 'r : Get OnHold Order 4 \\r\\n';

	function autoFillUpValue(fieldName, enterValue, totalPrice)
	{
		var returnValue;

		if(fieldName == 'cashin')
		{			
			document.f.cashin.value=enterValue;
			document.f.payment_cash.value=enterValue;				
		}
		
		if(fieldName == 'ccin' || fieldName == 'chequein')
		{				
			if((totalPrice - document.f.cashin.value) > 0)
			{				
				returnValue = totalPrice - document.f.cashin.value;
			}	
			else if((totalPrice - document.f.cashin.value) == 0)
			{				
				returnValue = totalPrice;
				document.f.cashin.value = '';				
			}
			else
				returnValue = totalPrice;		
		
		}		
	return returnValue.toFixed(2);		

	}
	function cleanUpEnteredValue(fieldName, enterValue, totalPrice)
	{		
		if(fieldName == 'cashin')
		{			
			document.f.cashin.value=enterValue;
			document.f.payment_cash.value=enterValue;			
			document.f.eftposin.value='';
			document.f.ccin.value='';
			document.f.chequein.value='';
			document.f.cashoutin.value='';				
			document.f.payment_eftpos.value='';
			document.f.payment_cc.value='';
			document.f.payment_cheque.value='';			
		}
		if(fieldName == 'eftposin')
		{			
			document.f.eftposin.value=enterValue;
			document.f.payment_eftpos.value=enterValue;
			document.f.ccin.value='';
			document.f.chequein.value='';			
			//if((totalPrice - document.f.cashin.value) == 0)
			if(((totalPrice - document.f.cashin.value) < (Number(document.f.round_cent.value)/10) || (totalPrice - document.f.cashin.value) == 0))
			{				
				document.f.cashin.value='';			
				document.f.payment_cash.value='';
			}
			else if((totalPrice - document.f.cashin.value) > 0)
			{
				document.f.eftposin.value = (totalPrice - document.f.cashin.value).toFixed(2);
			}
			else
				document.f.eftposin.value = totalPrice;
			document.f.payment_cc.value='';
			document.f.payment_cheque.value='';			
		}
		if(fieldName == 'cashoutin')
		{			
			document.f.ccin.value='';
			document.f.chequein.value='';
			document.f.cashoutin.value='';
			//document.f.cashin.value='';
			//document.f.payment_cash.value='';			
			document.f.payment_cc.value='';
			document.f.payment_cheque.value='';
		}
		if(fieldName == 'ccin')
		{
			document.f.ccin.value=enterValue;
			document.f.payment_cc.value=enterValue;
			//document.f.cashin.value='';
			document.f.eftposin.value='';
			document.f.cashoutin.value='';
			document.f.chequein.value='';	
			document.f.payment_eftpos.value='';
			//document.f.payment_cash.value='';
			document.f.payment_cheque.value='';			
		}
		if(fieldName == 'chequein')
		{
			document.f.chequein.value=enterValue;
			document.f.payment_cheque.value=enterValue;
			document.f.ccin.value='';
			document.f.eftposin.value='';
			document.f.cashoutin.value='';
			document.f.payment_eftpos.value='';
			document.f.payment_cc.value='';	
		}		
		if(fieldName == '')
		{
			document.f.payment_eftpos.value='';
			document.f.payment_cc.value='';
			document.f.payment_cheque.value='';
			document.f.payment_cash.value='';
			document.f.cashin.value=enterValue;
			document.f.eftposin.value=enterValue;
			document.f.cashoutin.value=enterValue;
			document.f.chequein.value=enterValue;
			document.f.ccin.value=enterValue;
		}
	
	}
	function sh(field)
	{
		field.style.backgroundColor='#44ffff';
	}
	function rh(field)
	{
		//field.style.backgroundColor='#ffffff';
		field.style.backgroundColor='#ABC2CB';
	}
	function print_last_receipt()
	{
		var slreceipt = '';
		fn = 'c:/qpos/receipt.txt';
		fso = new ActiveXObject('Scripting.FileSystemObject'); 
		if(fso.FileExists(fn))
		{
			tf = fso.OpenTextFile(fn, 1, false, -1); 
			slreceipt = tf.ReadAll();
			tf.Close(); 
		}
		if(slreceipt != '')
		{
//			document.EzPrint.SetPrinterName('GP-5850II');
			document.EzPrint.SetPrinterName('Receipt');
			document.EzPrint.SetFontName('Verdana');
			document.EzPrint.SetFontSize(8);
			document.EzPrint.PrintString(slreceipt);
			document.EzPrint.Cut();
			document.EzPrint.Kick();
		}
		else
			window.open('qpos.aspx?t=plr');
	}
	function kick_cashdraw()
	{
		document.EzPrint.Kick();
	}
	function focus_last_row()
	{
		if(document.f.rows.value == '0')
			return;
		var nf_current = Number(document.f.focus_field.value) + 1;
		if(nf_current > 3)
			nf_current = 0;
		document.f.focus_field.value = nf_current;
		var field_to_focus = 'rp';
		if(nf_current == 1)
			field_to_focus = 'rq';
		else if(nf_current == 2)
			field_to_focus = 'md';
		else if(nf_current == 3)
			field_to_focus = 'st';
		field_to_focus += (Number(document.f.rows.value) - 1);
";
s += "		eval('document.f.' + field_to_focus + '.focus();'); ";
s += "		eval('document.f.' + field_to_focus + '.select();'); ";
s += @"
	}
	function set_special_printer_port()
	{
		var sport = '';
		fn = 'c:/qpos/p_port.txt';
		fso = new ActiveXObject('Scripting.FileSystemObject'); 
		if(fso.FileExists(fn))
		{
			tf = fso.OpenTextFile(fn, 1, false, -1); 
			try
			{
				sport = tf.ReadAll();
			}
			catch(err)
			{
			}
			tf.Close(); 
		}
		
		if(window.confirm('Click Cancel to set local print port.'))
			return;
		sport = window.prompt('Special Printer Port?', sport);
		if(sport == null)
			return;
		if(sport == '' && sport == document.f.printer_port.value)
			return;
		if(fso.FileExists(fn))
			fso.DeleteFile(fn);
		tf = fso.OpenTextFile(fn , 8, 1, -1);
		tf.Write(sport);
		tf.Close();
	}
	function do_item_search()
	{
		var sbarcode = '';
		sbarcode = document.EzPrint.SearchItem();
		if(sbarcode != '')
		{
			document.f.s.value = sbarcode;
			onscan(true);
		}
	}
	function on_form_keydown()
	{
//		window.alert(event.keyCode);
		var scode = document.f.s.value;
		if(scode.substring(0,1) == '=')
			return true;
		var k = event.keyCode;
		switch(k)
		{
		case 66:
			//window.open('inputsn.aspx?inv=');
			window.open('olist.aspx?o=10');
			break;
		case 73:
			do_item_search();
			break;
		case 77:
			document.f.member_code.focus();
			document.f.member_code.select();
			break;
		case 85:
			print_last_receipt();
			break;
		case 88:
			set_special_printer_port();
			break;
		case 90:
			kick_cashdraw();
			break;
		case 76:
			focus_last_row();
			break;
		case 79:
			window.open('olist.aspx?o=14');
			break;
		case 80:
			document.f.cashin.focus();
			break;
		case 83:
			window.open('salesref.aspx?code=' + document.f.last_code.value);
			break;
		case 65:
			window.open('card.aspx?type=6');
			break;
		case 66:
			window.open('card.aspx?type=2');
			break;
		case 67:
			document.f.s.focus();
			document.f.s.value = '';
			break;
		case 71:
			window.open('ragent.aspx');
			break;
		case 72:
			hold_current();
			break;
		case 68:
			del_current(false);
			break;
		case 78:
			if(!window.confirm('Are you sure to remove all items and open a new sales?\\r\\nClick Cancel to do so.')){reload_offline_app()};
			break;
		case 191:
			re = /\\r\\n/g;
			shelp = shelp.replace(re, '\r\n');
			window.alert(shelp);
			break;
		case 81:
			get_hold(1);
			break;
		case 87:
			get_hold(2);
			break;
		case 69:
			get_hold(3);
			break;
		case 82:
			get_hold(4);
			break;
		default:
			return true;
			break;
		}
		return false;
	}
	function CheckSalesBarcode()
	{
		var barcode = document.f.sales_barcode.value;
		if(barcode == '')return;
		";
s += "	if(eval(\"document.f.sbb\" + barcode) == null || + eval(\"document.f.sbb\" + barcode) == 'undefined') \r\n";
s += "{window.alert('Sales Not Found.');document.f.sales_barcode.select();document.f.sales_barcode.focus();return;}  \r\n";
s += "	eval(\"document.f.sales.value = document.f.sbb\" + barcode + \".value\");  \r\n";
s += " var id = document.f.sales.value; \r\n";
s += " if(id == 'undefined') return;\r\n ";
s += "	eval(\"document.f.sales_name.value = document.f.sbn\" + id + \".value\");  \r\n";
s += @"
	}
	function IsNumberic(sText)
	{
		var ValidChars = '-0123456789.';
		var IsNumber=true;
		var Char;
		for (i = 0; i < sText.length && IsNumber == true; i++) 
		{ 
			Char = sText.charAt(i); 
			if(ValidChars.indexOf(Char) == -1) 
				IsNumber = false;
		}
		return IsNumber;
	}
	function check_all_numbers()
	{
		var rows = Number(document.f.rows.value);
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += " var sqty = eval(\"document.f.rq\" + j + \".value\"); \r\n";
	s += " if(!IsNumberic(sqty)){window.alert('Error, invalid QTY, ' + sqty + ' is not a number!');eval(\"document.f.rq\" + j + \".focus()\");eval(\"document.f.rq\" + j + \".select()\");return false}; \r\n";
	s += " var sprice = eval(\"document.f.dp\" + j + \".value\"); \r\n";
	s += " if(!IsNumberic(sprice)){window.alert('Error, invalid Price, ' + sprice + ' is not a number!');eval(\"document.f.dp\" + j + \".focus()\");eval(\"document.f.dp\" + j + \".select()\");return false}; \r\n";
	s += @"
		}
		return true;
	}
	function onscan(bSearchPackage)
	{
		var sin = document.f.s.value;
		if(sin.substring(0, 1) == '=')
		{
			sin = sin.substring(1, sin.length);
			document.f.s.value = sin;
		}	
		if(sin.substring(0, 1) != '*')
		{
			if(sin.indexOf('+') > 0 || sin.indexOf('-') > 0 || sin.indexOf('*') > 0 || sin.indexOf('/') > 0)
			{
				var calc = eval(sin);
				window.alert(sin + ' = ' + calc.toFixed(4));
				document.f.s.focus();
				document.f.s.select();
				return true;
			}
		}
		if(document.f.s.value == '')
		{
			if(!check_all_numbers()){return true;}
";
if(m_bRecordSales)
{
	s += @"
			if(document.f.sales.value == '' || document.f.sales.value == 'undefined')
			{
				window.alert('Please scan sales barcode.');
				document.f.sales_barcode.select();
				document.f.sales_barcode.focus();
				return true;
			}
		";
}
s += @"
			if(document.f.confirm_checkout.value == '1')
			{
				return true; //no double invoicing, already confirmed, checking out
			}
			if(document.f.rows.value == '0')
			{
				document.f.s.focus();
				return true;
			}
			if(document.f.cashchange.value == '')
			{
";
	if(m_bDisplay)
	{
		s += " var s_d_desc = 'Total : '; \r\n";
		s += " var s_d_price = document.f.total_display.value; \r\n";		
		s += " var nspaces = 35 - s_d_desc.length - s_d_price.length; \r\n";
		s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
		s += " var s_display = s_d_desc + s_d_price; \r\n";
	}

s += @"		
				document.f.cashin.focus();
				return true;
			}
			if(!paymentok())
			{
//				document.f.cashin.focus();
				return true;
			}

			var bconfirm = window.confirm('Confirm to checkout?');
			document.f.s.focus();
			if(bconfirm)
			{
//				document.f.confirm_checkout.value = 1;
//				del_current(true);
				writeorder();
			}
			return !bconfirm;
		}
		var i = Number(document.f.rows.value);
";
	
	s += @"
		var sinb = document.f.s.value;
		var nqty = 1;
		
		var md_q = document.f.md_qty.value;
		var md_d = document.f.md_dollar.value;
		var md_ad = document.f.member_discount.value;
		var md_ag = document.f.agent_discount.value;
		var gst_rate = document.f.gst_rate.value;

		if(md_q.length >4)
			document.f.md_qty.value = '';
		if(md_d.length >5)
			document.f.md_dollar.value = '';
		var b_total = false;
		if(md_d != '' && md_d.substring(0, 1) == 't') //total price
		{
			b_total = true;
			md_d = md_d.substring(1, md_d.length);
		}
		if(md_q == '')
			md_q = 1;
		if(!IsNumberic(md_q))
		{
			//window.alert('Warning!!! Wrong Discounted Percentage ' + document.f.md_qty.value);
			window.alert('Warning!!! Invalid QTY ' + document.f.md_qty.value);
			document.f.md_qty.value = '';
			md_q = '';
		}
		if(!IsNumberic(md_d))
		{
			//window.alert('Warning!!! Wrong Discounted Price:' + document.f.md_dollar.value);
			window.alert('Warning!!! Invalid Selling Price:' + document.f.md_dollar.value);
			document.f.md_dollar.value = '';
			md_d = '';
		}
		if(!IsNumberic(md_ad))
		{
			window.alert('Warning!!! Wrong Member Discounted Percentage ' + document.f.member_discount.value);
			document.f.member_discount.value = 0;
			md_ad = 0;
		}
		if(!IsNumberic(md_ag))
		{
			window.alert('Warning!!! Wrong Agent Discounted Percentage ' + document.f.agent_discount.value);
			document.f.agent_discount.value = 0;
			md_ad = 0;
		}
		if(sin.substring(0, 1) == '*')
		{
			nqty = Number(sin.substring(1));
			if(i > 0)
				i -= 1;
		";
	s += "	eval(\"document.f.rq\" + i + \".value = nqty\");  \r\n";
	s += @"
			ctd(i);
			document.f.s.value='';
			document.f.s.focus();
			return true;
		}
		else if(sin.substring(0, 1) == '.' || sin.substring(0, 1) == '/')
		{
			document.f.md_dollar.value='';
			document.f.md_qty.value='';
			removelastone();
			return true;
		}
		var barcode = sin;
		";

	s += "	var qty = eval(\"document.f.rq\" + i + \".value\"); \r\n";
	s += @"	
		if(qty == ''){qty = '1'};
		if(b_total)
		{
			md_d = (Number(md_d) / Number(md_q)).toFixed(4);
		}
		";
	//double up for the duplicate items

if(!m_bNoDoubleQTY)
{
	s += "	if(i>0 && sin == document.f.last_barcode.value && document.f.last_is_package.value != '1') \r\n";
	s += "	{		\r\n";
//	s += " if(document.f.last_is_package.value == '1'){searchpackage(sin);return true;}\r\n";
	s += "		i--; \r\n";
	s += " var lastQTY = eval(\"Number(document.f.rq\" + i + \".value);\") \r\n";	
	s += "		qty = eval(\"Number(document.f.rq\" + i + \".value) + 1;\") \r\n";
	s += "		sprice = eval(\"Number(document.f.dp\" + i + \".value);\") \r\n";	
	s += "		eval(\"document.f.rq\" + i + \".value = qty\"); \r\n";	
	s += "		ct(); \r\n";
	s += "		document.f.qty.value = qty; \r\n";
	//*** change to have the discount qty price  ****///
	s += " var index = searchitem('', 0, sin);\r\n";
	s += " var sqty = searchitem('qty', index, ''); \r\n";
	s += " var sprice = searchitem('price', index, ''); \r\n";	
	s += " if(Number(lastQTY) <= Number(qty)){sprice = searchitem('price2', index, '');} \r\n";
	//**** end here ****///	
	s += " eval(\"document.f.dp\" + i + \".value=Number(sprice).toFixed(2);\"); \r\n";	
	s += " eval(\"document.f.rp\" + i + \".value=Number(sprice).toFixed(2);\"); \r\n";		
	s += " eval(\"document.f.st\" + i + \".value=(Number(sprice) * qty).toFixed(2);\"); \r\n";	
	s += " document.f.price.value = (Number(sprice) * qty).toFixed(2); \r\n";	
	s += " document.f.total_display.value = (Number(sprice) * qty).toFixed(2); \r\n";	
	s += " document.f.totalfCurrency.value = (Number(sprice) * qty).toFixed(2); \r\n";		
	s += "		ct(); \r\n";
	s += "		document.f.s.value=''; document.f.md_qty.value='';document.f.md_dollar.value=''; \r\n";
	s += "		document.f.s.focus(); \r\n";
	s += "		return true; \r\n";
	s += "	}		\r\n"; 		


}
	s += " if(bSearchPackage){if(searchpackage(barcode)){return true;}}\r\n";
	s += " var index = searchitem('', 0, barcode);\r\n";
	s += " if(index == -1){sin = 'badbarcode'; \r\n";
	s += " window.alert('Not Found');";
	s += "	ct();";
	s += "	document.f.s.value='';";
	s += "	document.f.md_qty.value='';";
	s += "	document.f.md_dollar.value='';";
	s += "	document.f.s.focus();";
	s += "	return true;";
	s += "	} else {sin = searchitem('code', index, ''); \r\n";
	s += "	var sdesc = searchitem('name', index, ''); \r\n";
	s += "	var sqty = searchitem('qty', index, ''); \r\n";
	s += "	var sprice = Number(searchitem('price', index, '')); \r\n";
//s += "window.alert(sprice);";
//	s += "	window.alert(md_d);if(md_d == '' && Number(document.f.md_qty.value) >= Number(sqty)){sprice = searchitem('price2', index, '');} \r\n";
	s += " var normal_price = sprice; \r\n";
	s += " var sgprice = sprice; \r\n";
	s += " if(md_d > 0){ ";
	s += "  gst_rate = 1; ";
	s += " } ";
	s += " if( (md_d != '' && md_d > 0 ) || document.f.md_dollar.value == '0') ";
	s += "  sgprice = (md_d * gst_rate).toFixed(4); \r\n";
	s += " else ";
	s += " sgprice = sprice.toFixed(4); \r\n ";
	s += "	normal_price = sgprice; \r\n"; //GST inclusive
	s += " var md = '0'; ";
	s += " md = (sgprice  * (md_ad/100)).toFixed(0); ";
	//member discount price
	s += "if(md_ad > 0) {";
	s += " md = document.f.member_discount.value; ";
	s += " sgprice = (sgprice * (1 - md_ad/100)).toFixed(2); ";
	s += " } ";
	//agent discount price....
	s += "if(md_ag > 0) {";
	s += " md = document.f.agent_discount.value; ";
	s += " sgprice = (sgprice * (1 - md_ag/100)).toFixed(2); ";
	s += " } ";

	s += " var srd_total = sgprice * qty;\r\n";
	s += "	eval(\"document.f.rc\" + i + \".value = sin\"); \r\n";
	s += "	eval(\"document.f.rb\" + i + \".value = barcode\"); \r\n";	
	//s += " sdesc = unescape(sdesc); s(sdesc); ";
	s += "	eval(\"document.f.rd\" + i + \".value = sdesc\"); \r\n";
	s += "	eval(\"document.f.ro\" + i + \".value = sprice\"); \r\n";  //GST exclusive price
	s += "	eval(\"document.f.dp\" + i + \".value = sgprice\"); \r\n"; //GST Inclusive price
	s += "	eval(\"document.f.rp\" + i + \".value = Number(sgprice).toFixed(2)\"); \r\n"; //GST Inclusive price
	
	s += "	eval(\"document.f.np\" + i + \".value = Number(normal_price).toFixed(2)\"); \r\n"; //GST Inclusive price
//	s += "	eval(\"document.f.rq\" + i + \".value = qty\"); \r\n";
	s += "	eval(\"document.f.rq\" + i + \".value = md_q\"); \r\n";
	s += "	eval(\"document.f.md\" + i + \".value = md\"); \r\n";
//	s += "	eval(\"document.f.st\" + i + \".value = srd_total\"); \r\n";
	s += "	eval(\"document.f.del\" + i + \".style.visibility='visible'\"); \r\n";
	s += "	document.f.code.value = sin; \r\n";
	s += "	document.f.desc.value = sdesc; \r\n";
	s += "	document.f.qty.value = md_q; \r\n";
	s += "	document.f.price.value = (Number(sgprice) * Number(md_q)).toFixed(2); \r\n";
	s += "	document.f.last_barcode.value = sinb; \r\n";
//	s += " if(document.f.last_is_package == '1'){document.f.last_barcode.value='';}\r\n";
	s += "	document.f.last_code.value = sin; \r\n";
	s += "	document.f.focus_field.value = 2; \r\n";
		
	if(m_bDisplay)
	{
		s += " var s_d_desc = sdesc; \r\n";
		s += " if(s_d_desc.length > 30){s_d_desc = s_d_desc.substring(0, 30);} \r\n";
		s += " var s_d_price = '$' + Number(sgprice).toFixed(2); \r\n";
		s += " var nspaces = 40 - s_d_desc.length; \r\n";
		s += " nspaces -= s_d_price.length; \r\n";
		s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
		s += " var s_display = ''; \r\n";
		s += " s_display += s_d_desc; \r\n";
		s += " s_display += s_d_price; \r\n";
	}

	s += " } ";
	
	s += @"

		document.f.rows.value = i + 1;
	
		ct();
		document.f.s.value='';
		document.f.md_qty.value='';
		document.f.md_dollar.value='';
		document.f.s.focus();
		return true;
	}
	";

	s += @"

	function removelastone()
	{
		document.f.code.value = '';
		document.f.desc.value = '';
		document.f.price.value = '';

		var i = Number(document.f.rows.value) - 1;
		if(i < 0)
		{
			document.f.s.value='';
			document.f.s.focus();
			return;
		}
	";
	s += "	eval(\"document.f.rc\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rb\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rd\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.ro\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.dp\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rp\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.rq\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.md\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.np\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.st\" + i + \".value = ''\"); ";
	s += "	eval(\"document.f.del\" + i + \".style.visibility='hidden'\"); ";
	s += @"
		document.f.rows.value = i;
		ct();
		document.f.s.value='';
		document.f.s.focus();

		";

	s += " if(i > 0)\r\n{document.f.last_code.value = eval(\"document.f.rc\" + (i-1) + \".value\");document.f.last_barcode.value = eval(\"document.f.rb\" + (i-1) + \".value\");} \r\n";

	s += @"
	}

	function cp(row)
	{
		";
	s += " var p = Number(eval(\"document.f.rp\" + row + \".value\")); \r\n";
	s += " eval(\"document.f.dp\" + row + \".value='\" + p + \"'\"); \r\n";
	s += " var np = Number(eval(\"document.f.np\" + row + \".value\")); \r\n";
//s += "window.alert('np='+np);";
	s += " var discount = (np-p)/np; \r\n";
	s += " var sd = discount * 100; \r\n";	
	s += " if( discount > " + m_sMaxDiscountPercentage + "){window.alert('Error, Max Discount(" + MyDoubleParse(m_sMaxDiscountPercentage).ToString("p") + ") reached.'); \r\n";
	s += " eval(\"document.f.rp\" + row + \".value=document.f.np\" + row + \".value\"); return;}\r\n";
	s += " else{";
	s += " eval(\"document.f.md\" + row + \".value='\" + sd.toFixed(0) + \"'\"); \r\n";
	s += "} \r\n";

	s += @"
		ct();
	}
	function cd(row)
	{
		";
	s += " var np = Number(eval(\"document.f.np\" + row + \".value\")); \r\n";
	s += " var d = eval(\"document.f.md\" + row + \".value\"); \r\n";
	s += " d = d.replace('%', ''); \r\n";
	s += " d = d.replace('$', ''); \r\n";
	s += " var nd = Number(d)/100; \r\n";
	s += " var p = np * (1 - nd ); \r\n";
//	s += " var p = np - nd; \r\n";
	s += " if( nd > " + m_sMaxDiscountPercentage + "){window.alert('Error, Max Discount(" + MyDoubleParse(m_sMaxDiscountPercentage).ToString("p") + ") reached.'); \r\n";
	s += " eval(\"document.f.md\" + row + \".value='0'\"); return;}\r\n";
	s += " else{";
	s += " eval(\"document.f.dp\" + row + \".value='\" + p + \"'\");";
	s += " eval(\"document.f.rp\" + row + \".value='\" + p.toFixed(2) + \"'\");";	
	s += "} \r\n";

	s += @"
		ct();
	}
	function cs(row)
	{
		";
	s += " var q = Number(eval(\"document.f.rq\" + row + \".value\")); \r\n";
	s += " var np = Number(eval(\"document.f.np\" + row + \".value\")); \r\n";
	s += " var st = Number(eval(\"document.f.st\" + row + \".value\")); \r\n";
	s += " var p = (st/q); \r\n";
	s += " var d = (np - p)/np * 100; \r\n";	
	s += " if( d > " + m_sMaxDiscountPercentage + "){window.alert('Error, Max Discount(" + MyDoubleParse(m_sMaxDiscountPercentage).ToString("p") + ") reached.'); \r\n";
	s += " var rp = Number(eval(\"document.f.rp\" + row + \".value\")); \r\n";	
	s += " eval(\"document.f.st\" + row + \".value='\" + (rp * q).toFixed(2) + \"'\"); \r\n";
	s += " return false; ";
	//s += " eval(\"document.f.dp\" + row + \".value='\" + p + \"'\"); \r\n";
	s += " } ";	
	s += " else { ";
	s += " eval(\"document.f.dp\" + row + \".value='\" + p + \"'\"); \r\n";
	s += " eval(\"document.f.rp\" + row + \".value='\" + p.toFixed(2) + \"'\"); \r\n";
	s += " eval(\"document.f.md\" + row + \".value='\" + d.toFixed(2) + \"'\"); \r\n";
	
	s += "} \r\n";
	s += @"
		ct();
	}

	function del(row)
	{
		var rows = Number(document.f.rows.value);
		for(var i=row; i<=rows; i++)
		{
			";
	s += " var j = i + 1; \r\n";
	s += " var c = eval(\"document.f.rc\" + j + \".value\"); \r\n";
	s += " var b = eval(\"document.f.rb\" + j + \".value\"); \r\n";
	s += " var d = eval(\"document.f.rd\" + j + \".value\"); \r\n";
	s += " var q = eval(\"document.f.rq\" + j + \".value\"); \r\n";
	s += " var md = eval(\"document.f.md\" + j + \".value\"); \r\n";
	s += " var dp = eval(\"document.f.dp\" + j + \".value\"); \r\n";
	s += " var p = eval(\"document.f.rp\" + j + \".value\"); \r\n";
	s += " var o = eval(\"document.f.ro\" + j + \".value\"); \r\n";
	s += " var np = eval(\"document.f.np\" + j + \".value\"); \r\n";
	s += " if(i+1 == rows){ \r\n";
		s += " eval(\"document.f.rc\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rb\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rd\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rq\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.md\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.dp\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.rp\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.ro\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.np\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.st\" + i + \".value=''\"); \r\n";
		s += " eval(\"document.f.del\" + i + \".style.visibility='hidden'\"); \r\n";
	s += "}else{ \r\n";
		s += " eval(\"document.f.rc\" + i + \".value=c\"); \r\n";
		s += " eval(\"document.f.rb\" + i + \".value=b\"); \r\n";
		s += " eval(\"document.f.rd\" + i + \".value=d\"); \r\n";
		s += " eval(\"document.f.rq\" + i + \".value=q\"); \r\n";
		s += " eval(\"document.f.md\" + i + \".value=md\"); \r\n";
		s += " eval(\"document.f.dp\" + i + \".value=dp\"); \r\n";
		s += " eval(\"document.f.rp\" + i + \".value=p\"); \r\n";
		s += " eval(\"document.f.ro\" + i + \".value=o\"); \r\n";
		s += " eval(\"document.f.np\" + i + \".value=np\"); \r\n";
	s += "} \r\n";
	s += @"
		}
		document.f.rows.value = rows - 1;
		document.f.last_barcode.value = '';
		document.f.last_code.value = '';
		document.f.s.focus();
		ct();
	}

	function ctd(j)
	{
	";
	s += " var barcode = eval(\"document.f.rb\" + j + \".value\"); \r\n";

	s += " var qty = Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";	
	s += " var index = searchitem('', 0, barcode);\r\n";	
	s += " var sqty = searchitem('qty', index, ''); \r\n";	
	s += " var sprice = searchitem('price', index, ''); \r\n";	
	s += " if(qty >= Number(sqty)){sprice = searchitem('price2', index, '');} \r\n";
	s += " eval(\"document.f.dp\" + j + \".value=sprice;\");\r\n";
	s += " eval(\"document.f.rp\" + j + \".value=Number(sprice).toFixed(2);\");\r\n";
	s += " eval(\"document.f.st\" + j + \".value = (sprice * qty).toFixed(2)\");  \r\n";
	s += "	document.f.qty.value = qty; \r\n";
	s += "	document.f.price.value = (Number(sprice) * Number(qty)).toFixed(2); \r\n";
	s += " ct(); \r\n";
	s += @"
	}

	function ct()
	{
		var dtotal = 0;
		var rows = Number(document.f.rows.value);
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += "	eval(\"document.f.st\" + j + \".value = (Number(document.f.dp\" + j + \".value) * Number(document.f.rq\" + j + \".value)).toFixed(2)\");  \r\n";
	s += " dtotal += Number(eval(\"document.f.dp\" + j + \".value\")) * Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += @"
		}
		document.f.total.value = dtotal.toFixed(2);
		document.f.total_display.value = dtotal.toFixed(2);	
document.f.invoice_total_backup.value = dtotal.toFixed(2); //backup the invoice total
		if(document.f.set_no_gst.value==0)
			document.f.total_without_gst.value = (dtotal - (dtotal / Number(document.f.gst_rate.value))).toFixed(2);
		else
			document.f.total_without_gst.value = (0).toFixed(2);
		//*** currency exchange
		document.f.totalfCurrency.value = (eval(document.f.total_display.value) * eval(document.f.exRate.value)).toFixed(2) 
	}

	function CalcTotalDiscount()
	{
		var dtotal = 0;
		var dototal = 0;
		var rows = Number(document.f.rows.value);
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += " dototal += Number(eval(\"document.f.np\" + j + \".value\")) * Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += "};\r\n";
	s += " var new_total = Number(document.f.total.value); \r\n";
	s += " if( (dototal - new_total) / dototal > " + m_sMaxDiscountPercentage + "){window.alert('Error, Max Discount reached.'); \r\n";
	s += " ct(); return;}\r\n";
	s += @"
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += " dtotal += Number(eval(\"document.f.dp\" + j + \".value\")) * Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += @"
		}
		var ddiscount = (dtotal - Number(document.f.total.value));
		dtotal = 0;
		for(var j=0; j<rows; j++)
		{
	";
	s += " if(eval(\"document.f.rc\" + j + \".value\") == ''){break;} \r\n";
	s += " var np = Number(eval(\"document.f.np\" + j + \".value\")); \r\n";
	s += " var p = Number(eval(\"document.f.dp\" + j + \".value\")); \r\n";
	s += " var qty = Number(eval(\"document.f.rq\" + j + \".value\")); \r\n";
	s += " ddiscount /= qty; \r\n";
	s += " var new_p = p - ddiscount; \r\n";
	s += " eval(\"document.f.md\" + j + \".value = ((np-new_p)/np * 100).toFixed(2);\"); \r\n";
//	s += " eval(\"document.f.md\" + j + \".value = (np-new_p).toFixed(2);\"); \r\n";
	s += "eval(\"document.f.dp\" + j + \".value = new_p;\"); \r\n";
	s += "eval(\"document.f.rp\" + j + \".value = new_p.toFixed(2);\"); \r\n";
	s += " break; \r\n";
	s += @"
		}
		ct();
	}

	function getRoundUpPrice(paymentType)
	{
		var roundUpCent = document.f.round_cent.value;
		var roundedPrice = document.f.total.value;
		
		var lastDigit;
		if(paymentType == 'cashin')
		{
			if(roundUpCent == 10){ ////round up for 10 cents when last digit is higher than 0.06
				lastDigit = roundedPrice.substring(roundedPrice.length-1, roundedPrice.length);
				if(Number(lastDigit) > 5)
				{
					if(Number(roundedPrice) < 0)
						roundedPrice = Number(roundedPrice) + (0-(Number(roundUpCent) /100) - (Number(lastDigit)/ 100));
					else
						roundedPrice = Number(roundedPrice) + ((Number(roundUpCent) /100) - (Number(lastDigit)/ 100));
				}
				else
				{
					if(Number(roundedPrice) < 0)
						roundedPrice = Number(roundedPrice) - (0-Number(lastDigit) /100 );				
					else
						roundedPrice = Number(roundedPrice) - (Number(lastDigit) /100 );				
				}
			}
			else { ///round up only for 5 cents
				lastDigit = roundedPrice.substring(roundedPrice.length-1, roundedPrice.length);
				if(Number(lastDigit) > 5)
				{
					if(Number(roundedPrice) < 0)
						roundedPrice = Number(roundedPrice) + (0-Number(roundUpCent)/100 - (Number(lastDigit)/100));
					else
						roundedPrice = Number(roundedPrice) + (Number(roundUpCent)/100 - (Number(lastDigit)/100));
				}
				else if(Number(lastDigit) < 5)
				{
					if(Number(roundedPrice) < 0)
						roundedPrice = Number(roundedPrice) - (0-Number(lastDigit)/100);
					else
						roundedPrice = Number(roundedPrice) - (Number(lastDigit)/100);
				}
			}

			roundedPrice = Number(roundedPrice).toFixed(2);
			document.f.total_display.value=roundedPrice;
			document.f.total.value=roundedPrice;
		}
		else
		{
			document.f.total.value=document.f.invoice_total_backup.value; //restore default total for other payment
			document.f.total_display.value=document.f.invoice_total_backup.value; //restore default total for other payment			
			roundedPrice = document.f.total.value;
		}	
						
		return roundedPrice;
	}
	
	";

	s += "</script";
	s += ">";

	return s;
}

string PrintPaymentJava(bool bhta)
{
	string s = "<script language=javascript>\r\n";

	ASCIIEncoding encoding = new ASCIIEncoding( ); 
	byte[] select_display = {0x1b, 0x3d, 0x00, 0x02};
	byte[] select_printer = {0x1b, 0x3d, 0x00, 0x01};
	byte[] clear_screen = {0x00, 0x0c};
	byte[] move_home = {0x1f, 0x24, 0x01, 0x01};
	byte[] move_left_most = {0x08, 0x08, 0x08, 0x08, 0x08};
	string ssdisplay = encoding.GetString(select_display);
	string ssprinter = encoding.GetString(select_printer);
	string sclear = encoding.GetString(clear_screen);	
	string smovehome = encoding.GetString(move_home);
	string smoveleftmost = encoding.GetString(move_left_most);
	if(m_bDisplay && !bhta)
	{
		s += " var s_d_desc = 'Total : '; \r\n";
		s += " var s_d_price = inv_total; \r\n";
//s += "window.alert(inv_total);";
		s += " var nspaces = 35 - s_d_desc.length - s_d_price.length; \r\n";
		s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
		s += " var s_display = s_d_desc + s_d_price; \r\n";
	}
	if(bhta)
	{
		s += @"
function formatCurrency(num) 
{
	num = num.toString().replace(/\$|\,/g,'');
	if(isNaN(num))
		num = '0';
	sign = (num == (num = Math.abs(num)));
	num = Math.floor(num*100+0.50000000001);
	cents = num%100;
	num = Math.floor(num/100).toString();
	if(cents<10)
		cents = '0' + cents;
	for(var i = 0; i < Math.floor((num.length-(1+i))/3); i++)
		num = num.substring(0,num.length-(4*i+3))+','+
	num.substring(num.length-(4*i+3));
	return (((sign)?'':'-') + '$' + num + '.' + cents);
}

function GetNextInvNumber()
{
	var inv = 10001;
//	var inv = document.f.hd_invoice_number.value;	
	var fn = 'c:/qpos/qposni.txt'; 
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(!fso.FileExists(fn))
	{
		fso = new ActiveXObject('Scripting.FileSystemObject');
		tf = fso.OpenTextFile(fn , 8, 1, -1);
		tf.Write(inv);
		tf.Close();
		return inv;
	}	
	inv = Number(inv);
	tf = fso.OpenTextFile(fn, 1, false, -1); 
	inv = Number(tf.ReadAll());
	tf.Close(); 
	fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -1);
	tf.Write(inv+1);
	tf.Close();	
		
	return inv;
}
function clear_payment()
{
	document.f.cashin.value = '';
	document.f.eftposin.value = '';
	document.f.cashoutin.value = '';
	document.f.ccin.value = '';
	document.f.chequein.value = '';
	document.f.cashchange.value = '';

	document.f.payment_cash.value = '';
	document.f.payment_eftpos.value = '';
	document.f.payment_cc.value = '';
	document.f.payment_cheque.value = '';
	document.f.cash_out.value = '';

	document.f.cashin.focus();
}
function WriteOrder()
{
	//set refresh page is on....
	document.f.refreshPage.value = '1';

	var dCash = Number(document.f.payment_cash.value);
	var dEftpos = Number(document.f.payment_eftpos.value);
	var dCreditCard = Number(document.f.payment_cc.value);
	var dBankcard = Number(document.f.payment_bankcard.value);
	var dCheque = Number(document.f.payment_cheque.value);

	var inv_num = GetNextInvNumber();
	var invTotal = Number(document.f.total.value);
	var sInvTotal = formatCurrency(invTotal);
	var taxTotal =invTotal - (invTotal/Number(document.f.gst_rate.value));
	var branch = document.f.branch.value;
	var branch_name = document.f.branch_name.value;
	var sales = document.f.sales.value;
	var sales_name = document.f.sales_name.value;
	var member = document.f.member.value;
	var agent = document.f.agent.value;
	var max_item_name = document.f.max_item_name.value;
	var max_line_number = document.f.max_line_number.value;
	var max_space_between = document.f.max_space_between.value;
	
	if(member == '')
		member = '0';
	var d = new Date();
	var sd = d.getDate() + '/' + (d.getMonth()+1) + '/' + d.getFullYear() + ' ' + d.getHours() + ':' + d.getMinutes() + ':' + d.getSeconds();
	var s = 'invoice begin,' + inv_num + ',' + sd + ',' + branch + ',' + sales + ',' + member + ',' + agent + ',' + invTotal + ',' + dCash + ',' + dEftpos + ',' + dCreditCard + ',' + dBankcard + ',' + dCheque + '\\r\\n';
	var nrows = Number(document.f.rows.value);
	var sp = '';
	var space_before = max_space_between;
	for(var i=0; i<=nrows; i++)
	{
		var code = eval('document.f.rc' + i + '.value');
		if(code == '')
			break;
		var name = eval('document.f.rd' + i + '.value');
		if(name.length > max_line_number)
			name = name.substring(0, max_line_number);
		if(name.length > max_item_name)
			name = name.substring(0, max_item_name);		
		var barcode = eval('document.f.rb' + i + '.value');
		var qty = eval('document.f.rq' + i + '.value');
		var price = eval('document.f.dp' + i + '.value');
		s += code + ',' + price + ',' + qty + '\\r\\n';

		price = formatCurrency(Number(eval('document.f.dp' + i + '.value')) * Number(qty));

		sp += name + '\\r\\n';

		var slcode = '       ' + barcode;

		var len = (max_line_number/2) - (qty.length + 1 + price.length) ; //12;// - slcode.length;
		space_before = len;
		for(var n=0; n<len; n++)
			sp += ' ';
		len = qty.length + 1 + price.length;
		len = max_line_number - space_before - len;
		sp += 'x' + qty;
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += price + '\\r\\n';
	}

	var si = nrows + ' Items        ';
	si += 'TOTAL';
	sp += si;
	si += sInvTotal;
	var len = max_line_number - si.length;
	
	for(var n=0; n<len; n++)
		sp += ' ';
	sp += sInvTotal + '\\r\\n';

	var pm = document.f.pm.value;
	pm = pm.toUpperCase();
	//len = 20 - pm.length;
	len = space_before;	
	if(document.f.cashin.value != '')
	{
		for(n=0; n<len; n++)
			sp += ' ';
		sp += pm;
		var cashin = formatCurrency(Number(document.f.cashin.value));
		var cashchange = formatCurrency(Number(document.f.cashchange.value));
//		cashchange = cashchange.toFixed(2);
		//len = 20 - cashin.length;
		len = max_line_number - (space_before + pm.length + cashin.length);
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += cashin + '\\r\\n';
		if(Number(document.f.payment_eftpos.value) == 0)
		{
			for(var n=0; n<space_before; n++)
				sp += ' ';
			sp += 'CHANGE';
			var changed = 'CHANGE';
			len = max_line_number - (space_before + changed.length + cashchange.length);
			for(var n=0; n<len; n++)
				sp += ' ';
			sp += cashchange + '\\r\\n';
		}
	}
	if(Number(document.f.payment_eftpos.value) != 0)
	{
		var pmt = 'EFTPOS TOTAL';
		if(Number(document.f.cash_out.value) > 0)
		{
			var cashout = formatCurrency(Number(document.f.cash_out.value));
			for(var n=0; n<space_before; n++)
				sp += ' ';
			sp += 'CASH OUT';
			var cashOUT = 'CASH OUT';
			len = max_line_number - (space_before + cashOUT.length + cashout.length);
			for(var n=0; n<len; n++)
				sp += ' ';
			sp += cashout + '\\r\\n';
		}
		//len = max_line_number - pmt.length;
		len = space_before;
		for(n=0; n<len; n++)
			sp += ' ';
		sp += pmt;
		var cashchange = formatCurrency(Number(document.f.cashchange.value));
//		cashchange = cashchange.toFixed(2);
		len = max_line_number - (space_before + pmt.length + cashchange.length);
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += cashchange + '\\r\\n';
	}
	if(Number(document.f.payment_cc.value) != 0)
	{
		pm = 'Credit Card';
		//len = max_line_number - pm.length;
		len = space_before;
		for(n=0; n<len; n++)
			sp += ' ';
		sp += pm;
		var sdCC = formatCurrency(Number(document.f.payment_cc.value));
//		sdCC = sdCC.toFixed(2);
		len = max_line_number - (space_before + pm.length +sdCC.length);
		for(var n=0; n<len; n++)
			sp += ' ';
		sp += sdCC + '\\r\\n';
	}

//print tax total here
	var taxpm = 'Tax';
		//len = max_line_number - pm.length;
		len = space_before;
		for(n=0; n<len; n++)
			sp += ' ';
		sp += taxpm;
		var sdTax = formatCurrency(taxTotal);
		len = max_line_number - (space_before + taxpm.length +sdTax.length);
		for(var n=0; n<len; n++)
			sp += ' ';
		if(document.f.set_no_gst.value==0)
			sp += '0\\r\\n';
		else
			sp += sdTax + '\\r\\n';
	sp += '\\r\\n';
		";
s += " var sReceipt = \"" + m_sReceiptHeader + "\";\\r\\n";
s += " sReceipt += sp;\\r\\n";
s += " sReceipt += \"" + m_sReceiptFooter.Replace("\r\n", "\\\r\\\n") + "\"\\r\\n";
s += " var s_kickout = \"" + m_sReceiptKickout + "\"\\r\\n";

s += @"
	s += 'invoice end\\r\\n';
//	s += sReceipt;
	document.f.inv.value = s;
	try 
	{
		var fso, tf;
		var fn = 'c:/qpos/qposinv.csv';
		fso = new ActiveXObject('Scripting.FileSystemObject');
		tf = fso.OpenTextFile(fn , 8, 1, -1);
		tf.Write(s);
		tf.Close();
	}
	catch(err)
	{
		var strErr = 'Error:';
		strErr += '\\r\\nNumber:' + err.number;
		strErr += '\\r\\nDescription:' + err.description;
//		window.alert(strErr);
		return false;
	}
	sReceipt = sReceipt.replace('@@sales', sales_name);
	sReceipt = sReceipt.replace('@@date', sd);
	sReceipt = sReceipt.replace('@@time', '');
	sReceipt = sReceipt.replace('@@inv_num', inv_num);
	var points_msg = '';
	
	if(document.f.member.value != '0')
	{
		var points = (Number(document.f.total.value) * Number(document.f.member_point_rate.value)).toFixed(0);		
		var s1 = 'Membership ID : ' + document.f.member_code.value + ' Name : ' + document.f.member_name.value;
		var s2 = 'Points this INV : ' + points;
		var s3 = 'Total Points : ' + (Number(document.f.member_points_current.value) + Number(points));

		//var len = 42 - s1.length;
		var len = max_line_number - s1.length;
		for(var n=0; n<len; n++)
			points_msg += ' ';
		points_msg += s1 + '\\r\\n';
		//len = 42 - s2.length;
		len = max_line_number - s2.length;
		for(var n=0; n<len; n++)
			points_msg += ' ';
		points_msg += s2 + '\\r\\n';
		//len = 42 - s3.length;
		len = max_line_number - s3.length;
		for(var n=0; n<len; n++)
			points_msg += ' ';
		points_msg += s3 + '\\r\\n';		
	}
	
	sReceipt = sReceipt.replace('@@member_points_msg', points_msg);

//	document.write(sReceipt);

	fn = 'c:/qpos/receipt.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
		fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -1);
	tf.Write(sReceipt);
	tf.Close();

	del_current(true);
";
	s += " for(var n=0; n<" + m_nNoOfReceiptPrintOut + ";n++)\r\n";
	s += @"
	{
		document.EzPrint.SetPrinterName('Receipt');
		document.EzPrint.SetFontName('Verdana');
		document.EzPrint.SetFontSize(8);
		document.EzPrint.PrintString(sReceipt);
		document.EzPrint.Cut();
		document.EzPrint.Kick();
	}
	if(document.URL.indexOf('qpos.hta') > 0)
	{
		reload_offline_app();
		return false;
	}
	return true;
}
	function reload_offline_app()
	{
		var fn = 'c:/qpos/qpos.hta'; 
		var fso = new ActiveXObject('Scripting.FileSystemObject'); 
		var tf = fso.OpenTextFile(fn, 1, false, -1); 
		var s = tf.ReadAll(); 
		tf.Close(); 
		document.close();
		document.write(s);
		return true;
	}
		";

	}

	s += @"
	function CalcPaymentTotal()
	{
		var total = 0;
		total += Number(document.f.payment_cash.value);
		total += Number(document.f.payment_eftpos.value);
		//		total -= Number(document.f.cash_out.value);
		total += Number(document.f.payment_cheque.value);
		total += Number(document.f.payment_cc.value);
		total += Number(document.f.payment_bankcard.value);
		
		document.f.payment_total.value = Math.round(total * 100) / 100;
		var invoice_total = Number(document.f.total.value);
		var res = (invoice_total - total).toFixed(2);
		return res;
	}
	function MsgNotEnough(cashin)
	{
		var payment_total = Number(document.f.payment_total.value);
		var invoice_total = Number(document.f.total.value);
		var res = Math.round( (invoice_total - payment_total) * 100) / 100;
		var p_cash = Number(document.f.payment_cash.value);
		var p_eftpos = Number(document.f.payment_eftpos.value);
		var p_cheque = Number(document.f.payment_cheque.value);
		var p_cc = Number(document.f.payment_cc.value);
		var p_bankcard = Number(document.f.payment_bankcard.value);
		var msg = 'Payment Not OK.\\r\\n\\r\\n';
		msg += 'Invoice Total : $' + invoice_total + '\\r\\n';
		msg += 'Paid Total : $' + payment_total + '\\r\\n';
		msg += '------------------------------------\\r\\n';
		if(p_cash != 0)
			msg += 'Cash : $' + p_cash + '\\r\\n';
		if(p_eftpos != 0)
			msg += 'EFTPOS : $' + p_eftpos + '\\r\\n';
		if(p_cheque != 0)
			msg += 'Cheque : $' + p_cheque + '\\r\\n';
		if(p_cc != 0)
			msg += 'CREDIT Card : $' + p_cc + '\\r\\n';
		msg += '------------------------------------\\r\\n';
		if(res > 0)
			msg += 'Shortage : $' + res + '\\r\\n';
		else
			msg += 'OverCharge : $' + (0 - res) + '\\r\\n';
		window.alert(msg);
		document.f.cashin.focus();
		shownextform();
	}
	function MsgConfirmCheckout()
	{
		var bconfirm = window.confirm('Confirm Payment OK?');
		if(!bconfirm)
		{
			document.f.cashin.value = '';
			document.f.cashchange.value = '';
			document.f.cashin.focus();
		}
		return bconfirm;
	}
	function shownextform()
	{return;
		document.f.cashin.focus();
		if(document.f.pm.value == 'cash')
			showeftposform();
		else if(document.f.pm.value == 'eftpos')
			showcreditcardform();
		else if(document.f.pm.value == 'credit card')
			showcashform();
	}
	function delete_order()
	{
		if(document.URL.indexOf('qpos.hta') > 0)
		{
			var fn = 'c:/qpos/qpos.hta'; 
			var fso = new ActiveXObject('Scripting.FileSystemObject'); 
			var tf = fso.OpenTextFile(fn, 1, false, -1); 
			var s = tf.ReadAll(); 
			tf.Close(); 
			document.close();
			document.write(s);
		}
		else
";
s += "	window.location='qpos.aspx?t=new';\r\n";
s += @"
	}
	function paymentok()
	{	
		document.f.cashin.value = document.f.payment_cash.value;
		document.f.eftposin.value = document.f.payment_eftpos.value; 
		document.f.ccin.value = document.f.payment_cc.value;
		document.f.chequein.value = document.f.payment_cheque.value;
		if(document.f.rows.value == '0')
		{
			window.alert('Nothing to checkout.');
			return false;
		}
		if(document.f.cash_out.value != '' && Number(document.f.cash_out.value) != 0)
		{
			if(document.f.cashin.value != '' && Number(document.f.cashin.value) != 0)
			{
				window.alert('Sorry, I cannot take both cash in and out values.');
				return false;
			}
			document.f.payment_cash.value = 0 - Number(document.f.cash_out.value);
			document.f.payment_eftpos.value = Number(document.f.eftposin.value) + Number(document.f.cash_out.value);
		}
		else
		{
			document.f.cash_out.value = '';
		}
		var dbalance = CalcPaymentTotal();

		if(dbalance == 'NaN')
		{			
			MsgNotEnough();
			return false;
		}				
		if(dbalance > 0)
		{			
			MsgNotEnough();
			clear_payment();
			return false;
		}
		else if(dbalance < 0 && (document.f.cashin.value == '' || document.f.eftposin.value != '' || document.f.ccin.value != ''))		
		{		
			MsgNotEnough();
			return false;
		}
		
		var eftpos_total = (Number(document.f.payment_eftpos.value));// + Number(document.f.cash_out.value)).toFixed(2);
		var cg = Number(document.f.payment_total.value) - Number(document.f.total.value);
		if(document.f.payment_eftpos.value != '')
		{
			document.f.tcashchange.value = 'Eftpos Total';
			document.f.cashchange.value = eftpos_total;
		}
		else
		{	
			document.f.tcashchange.value = 'CHANGED';
			document.f.cashchange.value = cg.toFixed(2);
		}
		if(document.f.cash_out.value == '' && document.f.cashin.value != '' && document.f.cashchange.value != '') //more than enough cash received
		{
			if(Number(document.f.cashin.value) > Number(document.f.total.value))
				document.f.payment_cash.value = document.f.total.value; //set to total
		}
";
if(m_bDisplay)
{
//	s += " var s_d_desc = 'Change : '; \r\n";
	s += " var s_d_desc = document.f.tcashchange.value + ' : '; \r\n";
	s += " var s_d_price = '$' + document.f.cashchange.value; \r\n";
	s += " var nspaces = 40 - s_d_desc.length - s_d_price.length; \r\n";
	s += " for(var isp=0; isp<nspaces; isp++){s_d_desc += ' ';} \r\n";
	s += " var s_display = s_d_desc + s_d_price; \r\n";
}
s += @"
		return true;
	}
	";

	s += "</script";
	s += ">";
	return s;
}

string BuildReceiptBody()
{
	if(m_invoiceNumber == null || m_invoiceNumber == "")
		return "";
	m_memberPoints = "0";
	int rows = 0;
	string sc = " SELECT i.total, s.code, s.name, s.quantity, s.commit_price * "+ m_gst +" AS price, i.sales ";
	sc += ", c.barcode, cd.points, cd.name AS memberName, cd.phone  ";	
	sc += " FROM invoice i JOIN sales s ON s.invoice_number=i.invoice_number ";
	sc += " JOIN card cd ON cd.id = i.card_id ";	
	sc += " LEFT OUTER JOIN code_relations c ON c.code = s.code ";
	sc += " WHERE i.invoice_number = " + m_invoiceNumber;
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "receipt");
		if(rows <= 0)
		{
//			Response.Write("<br><br><center><h3>ERROR, Order Not Found</h3>");
			return "Error, Invoice #" + m_invoiceNumber + " Not Found or no item found.";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "SQL Error";
	}
DataRow[] drArray = null;

	if(m_bEnblePrintOutSN)
	{
		sc = " SELECT sn, ss.code  ";
		sc += " FROM invoice i JOIN sales s ON s.invoice_number=i.invoice_number ";
		sc += " JOIN sales_serial ss ON ss.invoice_number = i.invoice_number AND ss.code = s.code ";		
		sc += " WHERE i.invoice_number = " + m_invoiceNumber;
		sc += " AND (ss.sn <> '' OR ss.sn is not null) ";
//	DEBUG("sc =", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			myAdapter.Fill(dst, "serialNo");			
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return "SQL Error";
		}
	
	}

	int n = 0;
	int len = 0;
	m_dInvoiceTotal = Math.Round(MyDoubleParse(dst.Tables["receipt"].Rows[0]["total"].ToString()), 2);
	m_memberName = dst.Tables["receipt"].Rows[0]["memberName"].ToString();
	m_memberPoints = dst.Tables["receipt"].Rows[0]["points"].ToString();
	m_salesName = dst.Tables["receipt"].Rows[0]["sales"].ToString();
	string stotal = m_dInvoiceTotal.ToString("c");
	string s = "";
	int space_before = 10;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["receipt"].Rows[i];
		string code = dr["code"].ToString();
		string sn = "";
		if(m_bEnblePrintOutSN)
		{
			drArray = dst.Tables["serialNo"].Select(" code = "+ code +"","");
			for(int j=0; j<drArray.Length; j++)
			{
				//if(j>0)
				//	sn += " ";
				//if(j==0)
				sn += "SN (" + (j+1) + "): ";
				sn += drArray[j]["sn"].ToString();
				sn += "\\r\\n";
			}
		}
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		if(name.Length > int.Parse(m_max_item_name))
			name = name.Substring(0, int.Parse(m_max_item_name));
		name = name.Replace("\"", "");
		name = name.Replace("\r\n", " ");
		name = name.Replace("'", "");
		string qty = dr["quantity"].ToString();
		string price = Math.Round(MyDoubleParse(dr["price"].ToString()) * MyIntParse(qty), 2).ToString("c");

		s += name + "\\r\\n";
				
		string slcode = "       " + barcode;
//		s += slcode;
		//len = 20;// - slcode.Length;
		len = (int.Parse(m_max_line_number)/2) - (qty.Length + 1 + price.Length) ; //12;// - slcode.length;
		space_before = len;
		for(n=0; n<len; n++)
			s += ' ';
		len = qty.Length + 1 + price.Length;
//		len = 22 - len;

		len = int.Parse(m_max_line_number) - space_before - len;
		s += "x" + qty;
		for(n=0; n<len; n++)
			s += ' ';
		s += price + "\\r\\n";

		if(m_bEnblePrintOutSN)
			s += sn + "\\r\\n";

//		s += "      " + code + "          x" + qty + "      " + price + "\r\n";
	}

	string si = rows + " Items        TOTAL";
	s += si;
	si += stotal;
//	len = 42 - si.Length;
	len = int.Parse(m_max_line_number) - si.Length;
	for(n=0; n<len; n++)
		s += ' ';
	s += stotal + "\\r\\n";

	string pm = "";
	if(Request.Form["pm"] != null)
		pm = Request.Form["pm"].ToString().ToUpper(); //payment method

	double dCash = MyDoubleParse(Request.Form["payment_cash"]);
	double dEftpos = MyDoubleParse(Request.Form["payment_eftpos"]);
	double dCashOut = MyDoubleParse(Request.Form["cash_out"]);
	double dCC = MyDoubleParse(Request.Form["payment_cc"]);

//	len = 20 - pm.Length;
	len = space_before;
//	if(pm == "CASH")
	if(MyDoubleParse(Request.Form["cashin"]) != 0)
	{
		for(n=0; n<len; n++)
			s += ' ';
		s += pm;
		string cashin = MyDoubleParse(Request.Form["cashin"].ToString()).ToString("c"); 
		string cashchange = MyDoubleParse(Request.Form["cashchange"].ToString()).ToString("c");
		
		//len = 22 - cashin.Length;
		len = int.Parse(m_max_line_number) - (space_before + pm.Length + cashin.Length);
		for(n=0; n<space_before; n++)
			s += ' ';
		s += cashin + "\\r\\n";
		if(dEftpos == 0)
		{
			for(n=0; n<14; n++)
				s += ' ';
			s += "CHANGE";
			string changed = "CHANGE";
			//len = 22 - cashchange.Length;
			len = int.Parse(m_max_line_number) - (space_before + changed.Length + cashchange.Length);
			for(n=0; n<len; n++)
				s += ' ';
			s += cashchange + "\\r\\n";
		}
	}
//	else if(pm == "EFTPOS") //print cashout value if not 0
	if(dEftpos != 0)
	{
		string pmt = "EFTPOS TOTAL";
		string cashout = Request.Form["cash_out"].ToString();
		if(MyMoneyParse(cashout) > 0)
		{
			string pmco = "CASH OUT";
			//len = 20 - pmco.Length;			
			
			for(n=0; n<space_before; n++)
				s += ' ';
			s += pmco;
			cashout = MyMoneyParse(cashout).ToString("c");
			len = int.Parse(m_max_line_number) - (space_before + pmco.Length + cashout.Length);
			//len = 22 - cashout.Length;
			for(n=0; n<len; n++)
				s += ' ';
			s += cashout + "\\r\\n";
		}
		else if(MyMoneyParse(cashout) < 0)
		{
			string pmco = "CASH";
			//len = 20 - pmco.Length;
			for(n=0; n<space_before; n++)
				s += ' ';
			s += pmco;
			cashout = (0 - MyMoneyParse(cashout)).ToString("c");
			len = int.Parse(m_max_line_number) - (space_before + pmco.Length + cashout.Length);
			//len = 22 - cashout.Length;
			for(n=0; n<len; n++)
				s += ' ';
			s += cashout + "\\r\\n";
			pmt = "EFTPOS";
		}
		//len = 20 - pmt.Length;
		for(n=0; n<space_before; n++)
			s += ' ';
		s += pmt;
		string cashchange = MyDoubleParse(Request.Form["cashchange"].ToString()).ToString("c");
		
		//len = 22 - cashchange.Length;
		len = int.Parse(m_max_line_number) - (space_before + pmt.Length + cashchange.Length);
		for(n=0; n<len; n++)
			s += ' ';
		s += cashchange + "\\r\\n";
	}
//	else
	if(dCC != 0)
	{
		pm = "Credit Card";
		//len = 20 - pm.Length;
		for(n=0; n<space_before; n++)
			s += ' ';
		s += pm;
		//len = 22 - (dCC.ToString("c")).Length;
		len = int.Parse(m_max_line_number) - (space_before +  (dCC.ToString()).Length);
		
		for(n=0; n<len; n++)
			s += ' ';
		s += dCC.ToString("c") + "\\r\\n";
	}
	s += "\\r\\n";

	if(m_memberPoints != "0" && m_memberName.ToLower().IndexOf("cash sales") < 0 )
	{		
		string st1 = "";
		st1 += "Membership ID : " + dst.Tables["receipt"].Rows[0]["phone"].ToString() + " Name : " + m_memberName +"\\r\\n";
		st1 += "Points this INV : " +  (int)(m_dInvoiceTotal) +"\\r\\n";
		st1 += "Total Points : " + m_memberPoints +"\\r\\n";
		m_memberPoints = st1;		
	}
	else
		m_memberPoints = "0";

	return s;
}

bool DoReceiveOnePayment(string pm, double dAmount)
{
	if(dAmount == 0)
		return true;

	string payment_method = GetEnumID("payment_method", pm);
	string sAmount = dAmount.ToString();

	//do transaction
	SqlCommand myCommand = new SqlCommand("eznz_payment", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;

	myCommand.Parameters.Add("@shop_branch", SqlDbType.Int).Value = m_branchID;
	myCommand.Parameters.Add("@Amount", SqlDbType.Money).Value = sAmount;
	myCommand.Parameters.Add("@paid_by", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@bank", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@branch", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@nDest", SqlDbType.Int).Value = "1116";
	myCommand.Parameters.Add("@amount_for_card_balance", SqlDbType.Money).Value = 0;
	myCommand.Parameters.Add("@staff_id", SqlDbType.Int).Value = Session["card_id"].ToString();
	myCommand.Parameters.Add("@card_id", SqlDbType.Int).Value = "0"; //cash sales
	myCommand.Parameters.Add("@payment_method", SqlDbType.Int).Value = payment_method;
	myCommand.Parameters.Add("@invoice_number", SqlDbType.VarChar).Value = m_invoiceNumber;
	myCommand.Parameters.Add("@payment_ref", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@note", SqlDbType.VarChar).Value = "";
	myCommand.Parameters.Add("@finance", SqlDbType.Money).Value = "0";
	myCommand.Parameters.Add("@credit", SqlDbType.Money).Value = "0";
	myCommand.Parameters.Add("@bRefund", SqlDbType.Bit).Value = 0;
	myCommand.Parameters.Add("@amountList", SqlDbType.VarChar).Value = sAmount;
	myCommand.Parameters.Add("@return_tran_id", SqlDbType.Int).Direction = ParameterDirection.Output;

	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
//		ShowExp("DoCustomerPayment", e);
//		return false;
		myConnection.Close();
		AlertAdmin("DoCustomerPayment, e = " + e.ToString());
		return true;
	}
//	string m_tranid = myCommand.Parameters["@return_tran_id"].Value.ToString();
	return true;
}

double MyMoneyParseNoWarning(string s)
{
	Trim(ref s);
	if(s == null || s == "")
		return 0;
	if(s == "NaN")
		return 0;

	double d = 0;
	try
	{
		d = double.Parse(s, NumberStyles.Currency, null);
	}
	catch(Exception e)
	{
//		ShowParseException(s);
	}
	return d;
}

bool DoReceivePayment()
{
	if(Request.Form["invoice_number"] != null)
		m_invoiceNumber = Request.Form["invoice_number"];

	//IE refresh protection, fix double pay record
	if(Session["qpos_last_invoice_paid"] != null)
	{
		if(Session["qpos_last_invoice_paid"].ToString() == m_invoiceNumber)
			return true;
	}

	bool bMultiPayments = false;
	m_dCash = MyMoneyParseNoWarning(Request.Form["payment_cash"]);
	double dEftpos = MyMoneyParseNoWarning(Request.Form["payment_eftpos"]);
	double dCreditCard = MyMoneyParseNoWarning(Request.Form["payment_cc"]);
	double dBankcard = MyMoneyParseNoWarning(Request.Form["payment_bankcard"]);
	double dCheque = MyMoneyParseNoWarning(Request.Form["payment_cheque"]);

	if(!DoReceiveOnePayment("cash", m_dCash))
		return false;
	if(!DoReceiveOnePayment("eftpos", dEftpos))
		return false;
	if(!DoReceiveOnePayment("bank card", dBankcard))
		return false;
	if(!DoReceiveOnePayment("cheque", dCheque))
		return false;
	if(!DoReceiveOnePayment("credit card", dCreditCard))
		return false;

	Session["qpos_last_invoice_paid"] = m_invoiceNumber;
	return true;
}

bool PrintReceipt()
{
	if(Request.QueryString["i"] == null || Request.QueryString["i"] == "")
	{
		MsgDie("Error, no invoice number");
		return false;
	}
	m_invoiceNumber = Request.QueryString["i"];
	if(DoPrintReceipt(false, false))
	{
		Response.Write("<script language.javascript>window.close();<");
		Response.Write(">");
	}
	return true;
}

bool DoPrintReceipt(bool bhta, bool bkick)
{	
	string sReceiptBody = "";
	if(!bhta)
		sReceiptBody = BuildReceiptBody(); //get m_dInvoiceTotal first before record payment
	//print receipt
	byte[] bf = {0x1b, 0x21, 0x20, 0x0};//new char[4];
	byte[] sf = {0x1b, 0x21, 0x02, 0x0};//new char[4];
	byte[] cut = {0x1d, 0x56, 0x01, 0x00};//new char[4];
	byte[] kick = {0x1b, 0x70, 0x30, 0x7f};//, 0x0a, 0x0};//new char[6];
	byte[] init_printer = {0x1b, 0x40};

	ASCIIEncoding encoding = new ASCIIEncoding( );
    string bigfont = encoding.GetString(bf);	
    string smallfont = encoding.GetString(sf);
    string scut = encoding.GetString(cut);
    string kickout = encoding.GetString(kick);
    string sinit = encoding.GetString(init_printer);

	string header = ReadSitePage("pos_receipt_header");
	header = header.Replace("@@company_title", m_sCompanyTitle);
	header = header.Replace("@@branch_name", m_branchName);
	header = header.Replace("@@branch_address", m_branchAddress);
	header = header.Replace("@@branch_phone", m_branchPhone);
	header = header.Replace("@@branch_pos", m_branchPosHeader);
	
	header = header.Replace("@@branch_gstnum", m_branchGSTNum);
//	DataRow drc = GetCardData(m_sales);
	if(!bhta)
		header = header.Replace("@@sales", m_salesName);

	string footer = ReadSitePage("pos_receipt_footer");
	footer = footer.Replace("@@branch_pos_footer", m_branchPosFooter);
	string sbody = sReceiptBody;
//DEBUG("sbody = ", sbody);
	string sdate = DateTime.Now.ToString("dd/MM/yyyy");
	string stime = DateTime.Now.ToString("HH:mm");

	if(bhta)
		header = header.Replace("\r\n", "\\\\r\\\\n");
	else
		header = header.Replace("\r\n", "\\r\\n");
	header = header.Replace("[/b]", smallfont);
	header = header.Replace("[b]", bigfont);
	header = header.Replace("[cut]", scut.ToString());
	if(!bhta)
	{
		header = header.Replace("@@member_points_msg", "");
		header = header.Replace("@@date", sdate);
		header = header.Replace("@@time", stime);
		header = header.Replace("@@inv_num", m_invoiceNumber);
		if(m_memberPoints != "0" && m_memberPoints != "")
			header = header.Replace("@@member_points_msg", m_memberPoints);	
	}

	if(bhta)
		footer = footer.Replace("\r\n", "\\\\r\\\\n");
	else
		footer = footer.Replace("\r\n", "\\r\\n");
	footer = footer.Replace("[/b]", smallfont);
	footer = footer.Replace("[b]", bigfont);
	footer = footer.Replace("[cut]", scut.ToString());

	if(!bhta)
	{
		footer = footer.Replace("@@member_points_msg", "");
		footer = footer.Replace("@@date", sdate);
		footer = footer.Replace("@@time", stime);
		footer = footer.Replace("@@inv_num", m_invoiceNumber);
		if(m_memberPoints != "0" && m_memberPoints != "")
			footer = footer.Replace("@@member_points_msg", m_memberPoints);			
	}

	header = sinit + header;
	m_sReceiptHeader = header;
	m_sReceiptFooter = footer + scut;
	m_sReceiptKickout = kickout;

//DEBUG("header =", header);
	string sprint = header + sbody + footer + scut;
	string sprint_nokick = sprint;
//	if(m_dCash > 0)
//		sprint +=  kickout;// + "\\r\\n";
	StringBuilder sb = new StringBuilder();

	//EzPrint ActiveX Control
	sb.Append("\r\n<object classid=\"clsid:8A5E02AF-CC58-4EA8-9CB5-E9B7AC3A707B\" ");
	sb.Append(" CODEBASE=\"ezprint.dll#version=2,0,0,2\" width=1 height=1 style='visibility:hidden' ");
	sb.Append(" id=\"EzPrint\">\r\n");
	sb.Append("</object>\r\n");

	m_sReceiptPrinterObject = sb.ToString();
	m_sReceiptPrinterObject = m_sReceiptPrinterObject.Replace("\"", "\\\"");
	m_sReceiptPrinterObject = m_sReceiptPrinterObject.Replace("\r\n", "");
	m_sReceiptPort = GetSiteSettings("receipt_printer_port", "LPT1");

	if(bhta)
		return true;

	string s = "";
	s = "\r\n<script language=javascript>\r\n";
	
	s += " var printer_port = '" + m_sReceiptPort + "';\r\n";
	s += @"
	var sport = '';
	fn = 'c:/qpos/p_port.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
	{
		tf = fso.OpenTextFile(fn, 1, false, -1); 
		
		try
		{
			sport = tf.ReadAll();
		}
		catch(err)
		{
		}
		tf.Close(); 
	}
	if(sport != '')
		printer_port = sport;

	fn = 'c:/qpos/receipt.txt';
	fso = new ActiveXObject('Scripting.FileSystemObject'); 
	if(fso.FileExists(fn))
		fso.DeleteFile(fn);
	tf = fso.OpenTextFile(fn , 8, 1, -1);
	";
	s += "tf.Write('" + sprint_nokick + "'); \r\n";
	s += "tf.Close();\r\n";
	s += "document.EzPrint.SetPrinterName('Receipt'); ";
	s += "document.EzPrint.SetFontName('Verdana'); ";
	s += "document.EzPrint.SetFontSize(8); ";
	s += "document.EzPrint.PrintString('" + sprint + "');";
	s += "document.EzPrint.Cut();";
	s += "document.EzPrint.Kick();";
	s += "</script";
	s += ">";
//	DEBUG("s = ", s);
for(int i=0; i<m_nNoOfReceiptPrintOut; i++)
{
	sb.Append(s);
}
	
//	Response.Write(sprint); //test
	if(Request.QueryString["t"] != "pr")
	{
		sb.Append("<meta http-equiv=\"refresh\" content=\"0; URL=qpos.aspx?t=new\">");
//		sb.Append("<script>window.close();</script");
//		sb.Append(">");
	}
	else
	{
//		Response.Write(sprint); //test
//		return false;
		sb.Append("<script language=javascript>window.close();</script");
		sb.Append(">");
	}
	Response.Write(sb.ToString());
	return true;
}

/*
string GetBranchName(string id)
{
	if(dst.Tables["branch_name"] != null)
		dst.Tables["branch_name"].Clear();

	string sc = " SELECT * FROM branch WHERE id = " + id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "branch_name") == 1)
		{
			DataRow dr = dst.Tables["branch_name"].Rows[0];
			m_branchAddress = dr["address1"].ToString();	
			m_branchPhone = dr["phone"].ToString();
			m_branchPosHeader = dr["branch_pos_reciept_header"].ToString();
			m_branchPosFooter = dr["branch_pos_reciept_footer"].ToString();
			m_branchGSTNum = dr["tax_num"].ToString();

//	DEBUG("m_branchAddress +", m_branchAddress);
			return dr["name"].ToString();
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	return "";
}

*/

bool BuildAgentCache()
{
	string agentID = GetEnumID("card_type", "agent");
	if(agentID == "" || agentID == null)
		agentID = "2";
	string sc = " SELECT id, name, barcode FROM card WHERE type = "+ agentID +"  AND barcode <> '' ORDER BY barcode, id, name";
///DEBUG("agent =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "agent");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	StringBuilder sb = new StringBuilder();
/*	sb.Append("<select name=tagent><option name=0></option>");

	for(int i=0; i<dst.Tables["agent"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["agent"].Rows[i];
		string id = dr["id"].ToString();
		string trading_name = dr["trading_name"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
//		if(trading_name.Length > 10)
//			trading_name = trading_name.Substring(0, 10);
		
		sb.Append("<option value=" + id + ">" + name + " - " + trading_name + "</option>");
	}
	sb.Append("</select>");
*/
	sb.Append("<script language=javascript>function CheckAgentCode(){ \r\n");
	sb.Append(" var a = new Array(); \r\n");
	
	int n = 20; //max 1024 characters each line for hta file
	for(int i=0; i<dst.Tables["agent"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["agent"].Rows[i];
		string id = dr["id"].ToString();
		string barcode = dr["barcode"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		if(!TSIsDigit(barcode))
			continue;
		name = name.Replace("\"", "`");
//		string srow = "a[" + id + "]=\"" + name + "\";";
		string srow = "a[" + barcode + "]=\"" + name + "\";";
		sb.Append(srow);
		n += srow.Length;
		if(n > 1000)
		{
			sb.Append("\r\n");
			n = 0;
		}
	}

	sb.Append("\r\n");
	sb.Append(" var id = Number(document.f.agent_code.value); \r\n");
//	sb.Append(" if(id == 0){document.f.button_ok.focus();return;} \r\n");
	sb.Append(" if(a[id] != null){document.f.agent.value = id; document.f.agent_name.value = a[id];return true;}else{return false;} \r\n");
	sb.Append("} </script");
	sb.Append(">");

	m_sAgentCache = sb.ToString();
	return true;
}

string PrintSalesOptions()
{
	string sc = " SELECT id, trading_name, name FROM card WHERE type = 4 ";
	if(Session["branch_support"] != null)
		sc += " AND our_branch = " + m_branchID;
	sc += " ORDER BY name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "sales");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Response.End();
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<select name=sales><option name=0>Please Select</option>");

	for(int i=0; i<dst.Tables["sales"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["sales"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["name"].ToString();
		Trim(ref name);
		if(name == "")
			continue;
		
		sb.Append("<option value=" + id + ">" + name + "</option>");
	}
	sb.Append("</select>");
	return sb.ToString();
}

bool CheckOfflineHTAFile()
{
	string s = "<script language=javascript>\r\n";
	s += @"
try 
{
	var fn = 'c:/qpos/qpos.hta';
	var fso = new ActiveXObject('Scripting.FileSystemObject');
	if(!fso.FileExists(fn))
		window.location='qpos.aspx?updatecache=1';
}
catch(err)
{
	var strErr = 'checkofflinehtafile Error:';
	strErr += '\nNumber:' + err.number;
	strErr += '\nDescription:' + err.description;
//	window.alert(strErr);
	window.location = 'qpos.aspx?nts=1';
}
</script";
	s += ">";
	Response.Write(s);
	return true;
}

//offline HTA file functions
bool UpdateOfflineHTAFile()
{
	string st = m_shta.Replace("'", "\\'");
	st = st.Replace("\r\n", "\\r\\n");
	st = st.Replace("script", "@@script@@");
	st = st.Replace("function", "@@function@@");
	string s = "\r\n\r\n\r\n<script language=javascript>\r\n";
	s += "var shta = '" + st + "';";
	
	s += @"
		re = /@@script@@/g;
		shta = shta.replace(re, 'script');
		re = /@@function@@/g;
		shta = shta.replace(re, 'function');
		try 
		{
			var fso, tf;
			var pn = 'c:/qpos';
			var fn = 'c:/qpos/qpos.hta';
			fso = new ActiveXObject('Scripting.FileSystemObject');
			if(!fso.FolderExists(pn))
				fso.CreateFolder(pn);
			if(fso.FileExists(fn))
				fso.DeleteFile(fn);
			tf = fso.OpenTextFile(fn , 8, 1, -1);
		";
	s += "		tf.Write(shta)";
	s += @"
			tf.Close();
		}
		catch(err)
		{
			var strErr = 'write qpos.hta Error:';
			strErr += '\nNumber:' + err.number;
			strErr += '\nDescription:' + err.description;
			window.alert(strErr);
		}
		</script
		";
	s += ">";

	Response.Write(s);
	return true;
}

bool DoCreateShortcut()
{
//	WshShell shell = new WshShell();
//	IWshShortcut link = (IWshShortcut)shell.CreateShortcut("POS - offline");
//	link.TargetPath = "c:\\qpos.hta";
//	link.Save();

	string s = @"
<script language=javascript>
var strResult;

//****** download and create images to local
//*** background image *****//
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/qposbk.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/qposbk.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write qposbk.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/*try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/btBack.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/btBack.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write btBack.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
*/

//// load eznz logo
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/eznz.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/eznz.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write eznz.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}

/////load reload picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/reload.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/reload.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load delete picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/delete.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/delete.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load opendraw picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/opendraw.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/opendraw.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load Help picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/help.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/help.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load orderlist picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/orderlist.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/orderlist.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load neworder picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/neworder.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/neworder.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load neworder picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/print.jpg';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/print.jpg';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write reload.jpg Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load conner picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/ltop2.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/ltop2.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write lbottom.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load conner picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/rtop2.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/rtop2.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write rtop2.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load conner picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/rbottom.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/rbottom.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write rbottom.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
/////load conner picture...
try
{
	var strURL = '";
s += m_sServer;
	s += @"/i/lbottom.gif';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/lbottom.gif';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;
	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write lbottom.gif Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}
try
{
	var strURL = '";
s += m_sServer;
s += @"/i/eznzicon.exe';
	var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
	var temp = WinHttpReq.Open('GET', strURL, false);
	WinHttpReq.Send();
	strResult = WinHttpReq.ResponseBody;
}
catch (objError)
{
	strResult = objError;
	strResult += 'WinHTTP returned error: ' + (objError.number & 0xFFFF).toString();
	strResult += objError.description;
}
try 
{
	var fn = 'c:/qpos/eznzicon.exe';
	var adTypeBinary = 1;
	var adSaveCreateOverWrite = 2;

	var BinaryStream = new ActiveXObject('ADODB.Stream');
	BinaryStream.Type = adTypeBinary;
	BinaryStream.Open();
	BinaryStream.Write(strResult);
	BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
}
catch(err)
{
	var strErr = 'write eznzicon.exe Error:';
	strErr += '\r\nNumber:' + err.number;
	strErr += '\r\nDescription:' + err.description;
//	window.alert(strErr);
}

Shell = new ActiveXObject('WScript.Shell');
DesktopPath = Shell.SpecialFolders('Desktop');
link = Shell.CreateShortcut(DesktopPath + '\\P.O.S..lnk');
link.Arguments = '';
link.Description = 'POS Offline';
link.HotKey = 'CTRL+ALT+SHIFT+P';
link.IconLocation = 'c:\\qpos\\eznzicon.exe,0';
link.TargetPath = 'c:\\qpos\\qpos.hta';
link.WindowStyle = 3;
link.WorkingDirectory = 'c:\\qpos';
link.Save();
</script";
	s += ">";
	Response.Write(s);
	return true;
}

bool CheckOfflineInvoices()
{
	string s = @"
<script language=javascript>
var pn = 'c:/qpos';
var fn = 'c:/qpos/qposinv.csv'; 
fso = new ActiveXObject('Scripting.FileSystemObject'); 
if(!fso.FolderExists(pn))
	fso.CreateFolder(pn);
if(fso.FileExists(fn))
	window.location='qpos.aspx?oi=1&t=load';
else
	window.location='qpos.aspx?t=new';
</script";
	s += ">";
	Response.Write(s);
	return true;
}

bool LoadOfflineInvoices()
{
	string s = @"
<script language=javascript>
var fn = 'c:/qpos/qposinv.csv'; 
fso = new ActiveXObject('Scripting.FileSystemObject'); 
if(fso.FileExists(fn))
{
	var tf = fso.OpenTextFile(fn, 1, false, -1); 
	var s = tf.ReadAll(); 
	re = /\'/g;
	s = s.replace(re, '\\\'');
	tf.Close(); 
	document.write('<center><br><br><h4>You have unprocessed offline invoices, do you want to upload them now?</h4>');
	document.write('<form name=f action=qpos.aspx?oi=1&t=process method=post>');";
	s += "document.write('<input type=hidden name=inv value=\"' + s + '\">');";
	s += "document.write('<input type=submit name=cmd value=\"Upload Now\" class=b>');";
	s += "document.write('<input type=button value=\"Later\" class=b onclick=window.location=\"qpos.aspx?oi=1&t=new\">');";
	s += @"
	document.write('</form>');
	document.f.cmd.focus();
}
</script";
	s += ">";
	Response.Write(s);
	return true;
}

bool CreateOrder(string branch_id, string card_id, string po_number, string special_shipto, string shipto, 
				 string shipping_method, string pickup_time, string contact, string sales_id, string sales_note, 
				 ref string order_number)
{
	string reason = "";
	bool bStopOrdering = false;//IsStopOrdering(card_id, ref reason);
	if(bStopOrdering)
	{
		if(reason == "")
			reason = "No reason given.";
		Response.Write("<br><br><center><h3>This account has been disabled to place order</h3><br>");
		Response.Write("<h4><font color=red>" + reason + "<font color=red></h4><br>");
		Response.Write("<h4><a href=ecard.aspx?id=" + card_id + " class=o>Edit Account</a></h4>");
		Response.Write("<br><br><br><br><br><br><br>");
		return false;
	}

//	if(!CheckBottomPrice())
//		return false;

	//	string agent = Request.Form["tagent"];
	string agent = m_sAgent;
	if(Request.Form["agent"] != null)
		agent = Request.Form["agent"];
	if(agent == null || agent == "")
		agent = "0";

	DataSet dsco = new DataSet();
	string sc = "BEGIN TRANSACTION ";
	sc += " INSERT INTO orders (number, card_id, po_number, freight) VALUES(0, " + card_id + ", '";
	sc += po_number + "', 0 ";
	sc += ") SELECT IDENT_CURRENT('orders') AS id";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dsco, "id") == 1)
		{
			m_orderID = dsco.Tables["id"].Rows[0]["id"].ToString();
			m_orderNumber = m_orderID; //new order, same
			//assign ordernumber same as id
			sc = "UPDATE orders SET number=" + m_orderNumber + ", branch=" + branch_id + ", sales_note='" + sales_note + "' ";
			if(special_shipto == "1")
				sc += ", special_shipto=1, shipto='" + shipto + "' ";
			sc += ", contact='" + contact + "' ";
			sales_id = sales_id.Replace("NaN", "");
			if(sales_id != "")
			{			
				sc += ", sales = '" + sales_id + "' ";
			}
			else
				sc += ", sales = '" + Session["card_id"].ToString() +"'"; 
			sc += ", unchecked = 0 ";
			sc += ", sales_manager = (SELECT TOP 1 ISNULL(sales,'') FROM card WHERE id ='"+ card_id +"' AND sales IS NOT NULL) ";
			if(agent != null && agent != "")
				sc += ", agent = ISNULL((SELECT TOP 1 id FROM card WHERE barcode = '" + agent +"' ), '"+ agent +"') "; // agent enable....
			//sc += ", agent = '" + agent +"' "; // agent enable....
			sc += ", shipping_method=1";// + shipping_method;
			sc += ", pick_up_time='" + EncodeQuote(pickup_time) + "' ";
			sc += " WHERE id=" + order_number;
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
		else
		{
			Response.Write("<br><br><center><h3>Create Order failed, error getting new order number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	if(!WriteOrderItems(m_orderID))
		return false;
	return true;
}

bool CreateInvoice(string id)
{
	DataRow dr = null;
	double dPrice = 0;
	double dFreight = 0;
	double dTax = 0;
	double dTotal = 0;
	int rows = 0;
	if(dst.Tables["invoice"] != null)
		dst.Tables["invoice"].Clear();
	string sc = "SELECT * FROM orders WHERE id=" + id;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "invoice");
		if(rows != 1)
		{
			Response.Write("<br><br><center><h3>Error creating invoice, id=" + id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	dr = dst.Tables["invoice"].Rows[0];
	string card_id = dr["card_id"].ToString();
	string po_number = dr["po_number"].ToString();
	string m_shippingMethod = dr["shipping_method"].ToString();
	string m_pickupTime = dr["pick_up_time"].ToString();

	string agent = dr["agent"].ToString(); //agent enable
	string sales = dr["sales"].ToString();
	if(sales != "")
		sales = TSGetUserNameByID(sales);

	dFreight = Math.Round(MyDoubleParse(dst.Tables["invoice"].Rows[0]["freight"].ToString()), 4);

	if(dst.Tables["item"] != null)
		dst.Tables["item"].Clear();
	sc = "SELECT * FROM order_item WHERE id=" + id;
//DEBUG(" sc order=", sc);
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "item");
		if(rows <= 0)
		{
			Response.Write("<br><br><center><h3>Error getting order items, id=" + id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		double dp = MyDoubleParse(dr["commit_price"].ToString());
		dp = Math.Round(dp, 4);
//DEBUG("dp price= ",dp.ToString());
		double qty = MyDoubleParse(dr["quantity"].ToString());
//DEBUG("QTYT= ",qty);
		dPrice += dp * qty;
//DEBUG("dprice dp*qty= ",dPrice.ToString());
		dPrice = Math.Round(dPrice, 4);
//DEBUG("dprice round= ",dPrice.ToString());
	}
	dTax = (dPrice + dFreight) * GetGstRate(card_id);
	dTax = Math.Round(dTax, 4);
	dTotal = (dPrice + dFreight) * (1 + GetGstRate(card_id));
//DEBUG("dTotal no round= ", dTotal);
	dTotal = Math.Round(dTotal, 2);
//DEBUG("dTotal round= ", dTotal);
	m_dInvoiceTotal = dTotal;
	int nPoints = (int)(Math.Round(dTotal * m_dPointRate,0));
//DEBUG("m_dInvoiceTotal invocie = ", m_dInvoiceTotal);
	dr = dst.Tables["invoice"].Rows[0];
	string special_shipto = "0";
	if(bool.Parse(dr["special_shipto"].ToString()))
		special_shipto = "1";
	
	string receipt_type = GetEnumID("receipt_type", "invoice");
	if(m_bCreditReturn)
		receipt_type = "6";//GetEnumID("receipt_type", "credit note");
//DEBUG("invoice_date = ", m_invoiceDate);
	string sbSystem = "0";
	if(MyBooleanParse(dr["system"].ToString()))
		sbSystem = "1";
	string type = Request.Form["pm"];	

	if(dst.Tables["invoice_id"] != null)
		dst.Tables["invoice_id"].Clear();

	sc = " SET DATEFORMAT dmy ";
	sc += " BEGIN TRANSACTION ";
	sc += "INSERT INTO invoice (branch, type, card_id, price, tax, total, amount_paid, paid, commit_date, special_shipto, shipto ";
	sc += ", freight, cust_ponumber, shipping_method, pick_up_time, sales, sales_note, agent)";
	sc += " VALUES("+ m_branchID +", " + receipt_type + ", " + card_id + ", " + dPrice;
//	sc += ", " + dTax + ", " + dTotal + ", " + dTotal + ", 1, GETDATE(), ";
	sc += ", " + dTax + ", ROUND(" + dTotal + ",2), ROUND(" + dTotal + ",2), 1 ";
	if(m_invoiceDate != "")
		sc += ", '"+ m_invoiceDate +"', ";	
	else
		sc += ", GETDATE() , ";	
	sc += special_shipto + ", '" + EncodeQuote(dr["shipto"].ToString()) + "', " + dFreight + ", '" + po_number + "', ";
	sc += m_shippingMethod + ", '" + EncodeQuote(m_pickupTime) + "', '" + EncodeQuote(sales) + "', '";
	sc += EncodeQuote(dr["sales_note"].ToString()) + "' ";
	sc += ", '" + agent +"' ";
	sc += " )";
	sc += " SELECT IDENT_CURRENT('invoice') AS id";
	sc += " COMMIT ";
//DEBUG("sc =", sc);
//return false;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "invoice_id") == 1)
		{
			m_invoiceNumber = dst.Tables["invoice_id"].Rows[0]["id"].ToString();
//DEBUG(" m_invoiceNumber = ", m_invoiceNumber);
			m_qpos_next_invoice_number = m_invoiceNumber;
//DEBUG(" m_qpos_next_invoice_number = ", m_qpos_next_invoice_number);
		}
		else
		{
			Response.Write("<br><br><center><h3>Error get new invoice number</h3>");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	//update order to record invoice number
//	sc = " IF EXISTS(SELECT id FROM invoice WHERE id = " + m_invoiceNumber +" ) ";
//	sc += " BEGIN UPDATE invoice SET invoice_number = (SELECT invoice.id FROM invoice WHERE id = " + m_invoiceNumber +") WHERE  id = " + m_invoiceNumber +" END ";
	//sc += " ELSE ";
//	sc += " UPDATE invoice SET invoice_number = (SELECT invoice.id FROM invoice WHERE id = " + m_invoiceNumber +") WHERE  id = " + m_invoiceNumber +" ";
	sc = " UPDATE invoice SET invoice_number = id WHERE id = " + m_invoiceNumber +" ";
	sc += " UPDATE settings SET value = '"+ (int.Parse(m_invoiceNumber) + 1).ToString() +"' WHERE name = 'qpos_next_invoice_number' ";	
	sc += " SET DATEFORMAT dmy UPDATE orders SET invoice_number = " + m_invoiceNumber + ", status=3 ";
	if(m_invoiceDate != "")
		sc += " , record_date = '"+ m_invoiceDate +"' ";
	sc += " WHERE id=" + id; //status 3 = shipped
	sc += " UPDATE card SET points = points + " + nPoints + " WHERE id = " + card_id;
//DEBUG("sc +", sc);
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

	bool bHasKit = false;

	//write price history
	for(int i=0; i<dst.Tables["item"].Rows.Count; i++)
	{
		dr = dst.Tables["item"].Rows[i];
		string commit_price = dr["commit_price"].ToString();
		string quantity = dr["quantity"].ToString();
		string code = dr["code"].ToString();
		string name = dr["item_name"].ToString();
		string kit = dr["kit"].ToString();
		string krid = dr["krid"].ToString();
		string supplier = dr["supplier"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string supplier_price = dr["supplier_price"].ToString();
		double dNormalPrice = dGetNormalPrice(code);
		if(dNormalPrice == 0)
			dNormalPrice = MyDoubleParse(commit_price);

		sbSystem = "0";
		if(bool.Parse(dr["system"].ToString()))
			sbSystem = "1";

		string sKit = "0";
		if(MyBooleanParse(kit))
		{
			sKit = "1";
			bHasKit = true;
		}
		if(krid == "")
			krid = "null";

		sc = "INSERT INTO sales (invoice_number, code, name, quantity, commit_price, supplier, supplier_code, supplier_price, system, kit, krid ";
		sc += ", normal_price ";
		sc += " )";
		sc += " VALUES(" + m_invoiceNumber + ", " + code + ", '" + EncodeQuote(name) + "', " + quantity + ", " + commit_price + ", ";
		sc += "'" + supplier + "', '" + supplier_code + "', " + supplier_price + ", " + sbSystem + ", " + sKit + ", " + krid + "";
		sc += ", "+ dNormalPrice +" ";
		sc += " )";
//DEBUG("sc=", sc);
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
		
		double dQty = MyDoubleParse(quantity);

		//disable this call, we use last cost to calculate profit report for POS system
		//		fifo_sales_update_cost(m_invoiceNumber, code, commit_price, m_branchID, dQty);

		//update stock qty
		UpdateStockQty(dQty, code, m_branchID);
//		fifo_checkAC200Item(m_invoiceNumber, code, supplier_code, commit_price); //for unknow item
	}

	if(bHasKit)
	{
		if(!RecordKitToInvoice(id, m_invoiceNumber))
			return true;
	}

	Session["qpos_last_invoice_number"] = m_invoiceNumber;

	UpdateCardAverage(card_id, dPrice, MyIntParse(DateTime.Now.ToString("MM")));
	UpdateCardBalance(card_id, dTotal);

	return true;
}

double dGetNormalPrice(string code)
{
	if(dst.Tables["normal_price"] != null)
		dst.Tables["normal_price"].Clear();
	double dprice = 0;
	string sc = " SELECT DISTINCT ISNULL(";
	sc += "price1 / "+ m_gst +" "; //GST exclusive price
	sc += ",0) AS n_price FROM code_relations WHERE code = "+ code +" ";
	string nprice = "0";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "normal_price") == 1)
			 nprice= dst.Tables["normal_price"].Rows[0]["n_price"].ToString();
	}
	catch(Exception e) 
	{
//		ShowExp(sc, e);
		return 999;
	}
	dprice = MyDoubleParse(nprice);
	return dprice;
}

bool UpdateStockQty(double qty, string id, string branch_id)
{
	string sc = "";
	sc = " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code=" + id;
	sc += " AND branch_id = " + branch_id;
	sc += ")";
	sc += " INSERT INTO stock_qty (code, branch_id, qty, supplier_price) ";
	sc += " VALUES (" + id + ", " + branch_id + ", " + (0 - qty).ToString() + ", " + GetSupPrice(id) + ")"; 
	sc += " ELSE Update stock_qty SET ";
	sc += "qty = qty - " + qty + ", allocated_stock = allocated_stock - " + qty;
	sc += " WHERE code=" + id + " AND branch_id = " + branch_id;

	if(!g_bRetailVersion)
	{
		sc += " UPDATE product SET stock = stock - " + qty + ", allocated_stock = allocated_stock - " + qty;
		sc += " WHERE code=" + id;
	}
	else //retail version only update allocated stock in product table
	{
		sc += " UPDATE product SET allocated_stock = allocated_stock - " + qty;
		sc += " WHERE code=" + id;
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
		return false;
	}
	return true;
}

bool DoCreateOrder(bool bSystem, string sCustPONumber, string sSalesNote)
{
	string contact = "";
	m_sales = Request.Form["sales"];
//	m_customerID = Request.Form["member_code"];
	return CreateOrder(m_branchID, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), ref m_orderID);
}

bool WriteOrderItems(string order_id)
{
	CheckShoppingCart();
if(dtCart.Rows.Count <= 0)
{
	Response.Write("<script language=javascript> window.alert('Cart Empty');</script");
	Response.Write(">");
return false;
}

//DEBUG("cart rows = ", dtCart.Rows.Count);
//print_t(dtCart);	
//return false;
	for(int i=0; i<dtCart.Rows.Count; i++)
	{
		DataRow dr = dtCart.Rows[i];
		if(dr["site"].ToString() != m_sCompanyName)
			continue;

		string kit = dr["kit"].ToString();
		double dPrice = Math.Round(MyMoneyParseNoWarning(dr["salesPrice"].ToString()), 4);
//DEBUG("cart price=", dPrice);
		string name = EncodeQuote(dr["name"].ToString());
		
		if(name.Length > 255)
			name = name.Substring(0, 255);

		if(kit == "1")
		{
			RecordKitToOrder(order_id, dr["code"].ToString(), name, dr["quantity"].ToString(), dPrice, m_branchID);
			continue;
		}
		
		string sc = "INSERT INTO order_item (id, code, quantity, item_name, supplier, supplier_code, supplier_price ";
		sc += ", commit_price ";
//		sc += ", normal_price ";
		sc += " ) VALUES(" + order_id + ", " + dr["code"].ToString() + ", ";
		sc += dr["quantity"].ToString() + ", '" + name + "', '" + dr["supplier"].ToString();
		sc += "', '" + dr["supplier_code"].ToString() + "', " + Math.Round(MyMoneyParseNoWarning(dr["supplierPrice"].ToString()), 4);
		sc += ", " + dPrice + " ";
//		sc += ","+ dNormalPrice +" ";
		sc += ") ";
		
		sc += " UPDATE stock_qty SET allocated_stock = allocated_stock + " + dr["quantity"].ToString();
		sc += " WHERE code = " + dr["code"].ToString();
		sc += " AND branch_id = " + m_branchID;
		
		sc += " UPDATE product SET allocated_stock=allocated_stock+" + dr["quantity"].ToString();
		sc += " WHERE code=" + dr["code"].ToString() + " ";
//DEBUG("sc order_item = ", sc);
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
	return true;
}

//	return CreateOrder(m_branchID, m_customerID, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
//		m_pickupTime, contact, m_sales,  EncodeQuote(sSalesNote), ref m_orderID);
bool ProcessOfflineInvoices(bool bSilence)
{
	Session[m_sCompanyName + "_ordering"] = null;
	Session[m_sCompanyName + "_salestype"] = "qpos";

	CheckShoppingCart();
	string s = "";
	s = Request.Form["inv"];
	string line = "";
	string ref_num = "";
	string inv_total = "";
	string sdate = "";
	string sCustPONumber = "";
	string contact = "";
	double dCash = 0;
	double dEftpos = 0;
	double dCreditCard = 0;
	double dBankcard = 0;
	double dCheque = 0;
	if(!bSilence)
	{
		Response.Write("<h4>Processing, please wait...</h4>");
		Response.Flush();
	}
	
	DateTime dTmp; 			
	string sDateFormat = "en-US";

	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\r') //one line fully read
		{
//DEBUG("line=", line);
			char[] cb = line.ToCharArray();
			int pos = 0;
			string tag = CSVNextColumn(cb, ref pos);
			Trim(ref tag);
//DEBUG("tag=", tag);
			if(tag == "invoice begin")
			{
//DEBUG("begin", "");
				ref_num = CSVNextColumn(cb, ref pos);
				
				sdate = CSVNextColumn(cb, ref pos);
				m_branchID = CSVNextColumn(cb, ref pos);
				m_sales = CSVNextColumn(cb, ref pos);
				m_sMember = CSVNextColumn(cb, ref pos);			
				m_sAgent = CSVNextColumn(cb, ref pos);// get agent 
				sCustPONumber = ref_num +" branch:"+ m_branchID;
				try
				{					
					Thread.CurrentThread.CurrentCulture = new CultureInfo(m_setCulture);	
					dTmp = DateTime.Parse(sdate);
					m_invoiceDate = dTmp.ToString();
					sdate = m_invoiceDate;	
//DEBUG("dtmp1 +", dTmp.ToString());					
				}
				catch (Exception er)
				{
					try
					{
						if(m_setCulture == sDateFormat)
							sDateFormat = "en-GB";						
						Thread.CurrentThread.CurrentCulture = new CultureInfo(sDateFormat);
						dTmp = DateTime.Parse(sdate);	
						Thread.CurrentThread.CurrentCulture = new CultureInfo(m_setCulture);										
						m_invoiceDate = dTmp.ToString();
						sdate = m_invoiceDate;
					}
					catch (Exception ec)
					{
						m_invoiceDate = "";						
					}
				}						
//DEBUG("sdate1 +", sdate);

				if(m_sales == "undefined")
					m_sales = "0"; //use cash sales
				inv_total = CSVNextColumn(cb, ref pos);
//				m_salesNote = "offline invoice ref #" + ref_num + " date:" + sdate;
				m_salesNote = "offline invoice ref #" + ref_num + " branch:" + m_branchID + " total:" + inv_total + " date:" + sdate;
				if(bSilence)
					m_salesNote = "";
				dCash = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dEftpos = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dCreditCard = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dBankcard = MyMoneyParse(CSVNextColumn(cb, ref pos));
				dCheque = MyMoneyParse(CSVNextColumn(cb, ref pos));
				EmptyCart();
			}
			else if(tag == "invoice end")
			{
//DEBUG("end", "");
				if(!bSilence)
					Response.Write("reference #" + ref_num + ", ");
				if(!bSilence && AlreadyUploaded(m_salesNote)) //use sales_note to avoid double uploads (ONLY FOR OFFLINE)
				{
					Response.Write("already uploaded, skip<br>");
					line = "";
					continue;
				}
				
				bool bRet = CreateOrder(m_branchID, m_sMember, sCustPONumber, m_specialShipto, m_specialShiptoAddr, m_nShippingMethod, 
					m_pickupTime, contact, m_sales, EncodeQuote(m_salesNote), ref m_orderID);
				if(!bRet)
				{
					Response.Write("<h4>Error Create Order</h4>");
					return false;
				}
				if(!bSilence)
					Response.Write("order created #" + m_orderID + ", ");
				if(!CreateInvoice(m_orderID))
				{
					Response.Write("<h4>Error Create Invoice</h4>");
					return false;
				}
				if(!bSilence)
					Response.Write("invoice created #" + m_invoiceNumber + ", ");
				if(!DoReceiveOnePayment("cash", dCash))
					return false;
				if(!DoReceiveOnePayment("eftpos", dEftpos))
					return false;
				if(!DoReceiveOnePayment("bank card", dBankcard))
					return false;
				if(!DoReceiveOnePayment("cheque", dCheque))
					return false;
				if(!DoReceiveOnePayment("credit card", dCreditCard))
					return false;
				if(!bSilence)
					Response.Write("payment recorded<br>");
			}
			else //items, build cart
			{
//DEBUG("tag =", tag);
				string code = tag;
				double dPrice = MyMoneyParse(CSVNextColumn(cb, ref pos));
//DEBUG("price inv = ", dPrice.ToString());
				int qty = MyIntParse(CSVNextColumn(cb, ref pos));
				dPrice /= m_gst;
				dPrice = Math.Round(dPrice, 4);
//DEBUG("code cart = ", code);
//DEBUG("qty cart= ", qty);
//DEBUG("price cart = ", dPrice.ToString());
				if(!AddToCart(code, qty.ToString(), dPrice.ToString()))
				{
					Response.Write(tag + " " + code + " " +qty.ToString() +" " +dPrice.ToString() + "<br>");
					Response.Write("add to cart error<br>");
					return false;
				}
			}
			line = "";
		}
		else if(s[i] == '\n')
		{
		}
		else
			line += s[i];
	}

	s = @"
<script language=javascript>
var fn = 'c:/qpos/qposinv.csv'; 
fso = new ActiveXObject('Scripting.FileSystemObject'); 
if(fso.FileExists(fn))
	fso.DeleteFile(fn);
</script";
	s += ">";
	Response.Write(s);
	if(!bSilence)
	{
		Response.Write("<h4>done! please wait a second...</h4>");
		Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=qpos.aspx?t=new\">");
		return true;
	}
	else
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=qpos.aspx?t=new\">");
	}
	return true;
}

bool AlreadyUploaded(string note)
{
	string sc = " SELECT invoice_number FROM invoice WHERE sales_note LIKE '" + EncodeQuote(note) + "' ";
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		if(myCommand1.Fill(dst, "item") > 0) //found, already uploaded
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return true; //return true to not upload this invoice
	}
	return false; //ok, upload this invoice
}

string CSVNextColumn(char[] cb, ref int pos)
{
	if(pos >= cb.Length)
		return "";

	char[] cbr = new char[cb.Length];
	int i = 0;

	if(cb[pos] == '\"')
	{
		while(true)
		{
			pos++;
			if(pos == cb.Length)
				break;
			if(cb[pos] == '\"')
			{
				pos++;
				if(pos >= cb.Length)
					break;
				if(cb[pos] == '\"')
				{
					cbr[i++] = '\"';
					continue;
				}
				else if(cb[pos] != ',')
				{
					Response.Write("<br><font color=red>Error</font>. CSV file corrupt, comma not followed quote. Line=");
					Response.Write(new string(cb));
					Response.Write("<br>\r\n");
					break;
				}
				else
				{
					pos++;
					break;
				}
			}
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
		}
	}
	else
	{
		while(cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
			pos++;
			if(pos == cb.Length)
				break;
		}
		pos++;
	}
	return new string(cbr, 0, i);
}

bool PrintTrustedMassInterface()
{
	string s = @"
<script language=javascript>
	var fn = 'c:/qpos/qpos.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false, -1); 
	var sh = tf.ReadAll(); 
	tf.Close(); 
	document.close();
	";
//s += "	sh = sh.replace('reload_offline_app()', 'window.location=(\"?t=new\")')\r\n";
s += @"
	document.write(sh);
</script";
	s += ">";
//Response.Write("trsuted interface test<br>");
	Response.Write(s);
	Response.Write("<a href=qpos.aspx?t=chl class=o>Download Offline Tools(Create shortcut on desktop)</a> &nbsp; ");
	Response.Write("<a href=qpos.aspx?t=fu&r=" + DateTime.Now.ToOADate() + " class=o>Update Cache</a> &nbsp; ");
	return true;
}

bool PrintTrustedPaymentForm()
{
	m_invoiceNumber = Request.QueryString["i"];
	m_dInvoiceTotal = 0;
	if(Request.QueryString["total"] != null)
		m_dInvoiceTotal = MyDoubleParse(Request.QueryString["total"].ToString());

	string s = @"
<script language=javascript>
	var fn = 'c:/qpos/qpospayo.hta'; 
	var fso = new ActiveXObject('Scripting.FileSystemObject'); 
	var tf = fso.OpenTextFile(fn, 1, false, -1); 
	var sh = tf.ReadAll(); 
	tf.Close(); 
	document.close();
";
	s += " 	re = /replace_with_invoice_number/g; ";
	s += "	sh = sh.replace(re, '" + m_invoiceNumber + "');";
	s += "	document.write(sh);";
	s += "document.f.total1.value = '" + m_dInvoiceTotal.ToString("c") + "';\r\n";
	s += "document.f.total.value = '" + m_dInvoiceTotal + "';\r\n";
	s += "</script";
	s += ">";
//Response.Write("trsuted interface test<br>");
	Response.Write(s);
	return true;
}

bool SendToDebug(string s)
{
	MailMessage msgMail = new MailMessage();
	msgMail.From = "alert1@eznz.com";
	msgMail.To = "alert1@eznz.com";
	msgMail.Subject = "debug info";
//	msgMail.BodyFormat = MailFormat.Html;
	msgMail.Body = s;
	SmtpMail.Send(msgMail);
	return true;
}

bool BuildTouchScreenButtonCache()
{
	int rows = 0;
	DataSet dspc = new DataSet();
	if(dspc.Tables["itemButton"] != null)
		dspc.Tables["itemButton"].Clear();

	string sc = " SELECT DISTINCT TOP "+ m_sTotalTouchScreenItem +" c.code, c.name, RTRIM(LTRIM(c.barcode)) AS barcode ";	
	if(m_bFixedPrices)
		sc += ", c.price1 AS price, c.price2, c.qpos_qty_break AS qty_break1 "; //GST inclusive price
	sc += " FROM code_relations c JOIN specials s ON s.code = c.code ";
	sc += " WHERE c.skip = 0 ";
	
	sc += " ORDER BY c.code DESC ";
//DEBUG(" sc-", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dspc, "itemButton");
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid column name 'qpos_qty_break'") >= 0)
		{
			myConnection.Close(); //close it first

			string ssc = @"
				
			ALTER TABLE [dbo].[code_relations] ADD [qpos_qty_break] [int] DEFAULT(0) not null										
		
			";
	//	DEBUG("ssc = ", ssc);
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
			//	ShowExp(sc, er);
				return false;
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +" \">");		
		}
		ShowExp(sc, e);
		return false;
	}
 
	int nMax = 1000;
	StringBuilder sb = new StringBuilder();
	sb.Append("<table width=100% border=1 cellspacing=0 cellpadding=2 bordercolor=black bgcolor=black");
	sb.Append(" style=\"font-family:Verdana;font-size:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">\r\n");
	int nLetters = 17;
	string s = "";
	
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dspc.Tables["itemButton"].Rows[i];
		string barcode = dr["barcode"].ToString();
		string price = dr["price"].ToString();	
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		if(name.Length > nLetters)
			name = name.Substring(0,nLetters-1);
		string sSpace = "";	
		if(name.Length < nLetters)
		{			
			for(int j=name.Length; j<nLetters; j++)
				sSpace += " ";

			name = "<font color=green>"+ name + "<font color=green>"+sSpace ;
		}		
		name = name.ToUpper();
		if(i==0 || (i%4) == 0)
			sb.Append("<tr align=center>");	
		/////////////////////////// 
		//// download special item picture to local//when enable Tourch screen picture is enable/////
		if(m_bEnbleTouchScreenPicture)
		{	
			s += @"
			<script language=javascript>
			
			try
			{
				var strURL = '";
				s += m_sServer +"/"+ m_sCompanyName;
				s += "/pi/"+ code +".gif";
			s += @"';
				
				var WinHttpReq = new ActiveXObject('WinHttp.WinHttpRequest.5.1');
				var temp = WinHttpReq.Open('GET', strURL, false);
				WinHttpReq.Send();
				strResult = WinHttpReq.ResponseBody;
			}
			catch (objError)
			{
				strResult = objError;
				strResult += 'WinHTTP returned error: Downloading Special Picture Errors: ' + (objError.number & 0xFFFF).toString();
				strResult += objError.description;
			}
			try 
			{
				var fn = 'c:/qpos/";
				s += code +".gif";
				s += @"';				
				var adTypeBinary = 1;
				var adSaveCreateOverWrite = 2;

				var BinaryStream = new ActiveXObject('ADODB.Stream');
				BinaryStream.Type = adTypeBinary;
				BinaryStream.Open();
				BinaryStream.Write(strResult);
				BinaryStream.SaveToFile(fn, adSaveCreateOverWrite);
			}
			catch(err)
			{
				var strErr = 'write speical item .gif Error:';
				strErr += '\r\nNumber:' + err.number;
				strErr += '\r\nDescription:' + err.description;
				//window.alert(strErr);
			}
			</script"; 
			s += ">";
			
			Response.Write(s);									
			
			//////////////////////////end here/////////////////
			///////////
		}		
		
		//sb.Append("<td><button style='font-size:11pt;border-style: none; width=168px; height=60px; background-image: url(file:///C:/qpos/btBack.gif)' name=\""+ barcode +"\" class=b onfocus=\"sh(this);\" onblur=\"rh(this);\" ");		
		if(m_bEnbleTouchScreenPicture)
			sb.Append("<td><button style='font-size:11pt;border-style: none; width=186px; height=120px; background-image: url(file:///C:/qpos/"+ code +".gif)' name=\""+ barcode +"\" class=b onfocus=\"sh(this);\" onblur=\"rh(this);\" ");			
		else
			sb.Append("<td><button style='font-size:11pt;border-style: none; width=168px; height=60px; background-image: url(file:///C:/qpos/btBack.gif)' name=\""+ barcode +"\" class=b onfocus=\"sh(this);\" onblur=\"rh(this);\" ");
		
		sb.Append(" onclick=\" document.f.s.value=this.name; if(onscan(true)){return false;}\">"+ name +"<br>"+ double.Parse(price).ToString("c") +"</button></td>\r\n");			
	}
	
	sb.Append("</table>");
	m_sProductTouchScreenCache = sb.ToString();
	return true;
}

string BuildCurrencyExchange()
{
	int rows = 0;
	DataSet dspc = new DataSet();
	string sc = " SELECT currency_name, rates ";	
	sc += " FROM currency ";
	sc += " ORDER BY id ";
//DEBUG(" sc-currency", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dspc, "currencyExchange");
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid object name 'currency'") >= 0)
		{
			myConnection.Close(); //close it first

			string ssc = @"
				
			CREATE TABLE [dbo].[currency](
			[id] [int] IDENTITY(1,1) NOT NULL,
			[currency_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[rates] [float] NOT NULL CONSTRAINT [rates]  DEFAULT (1),
			[insert_by] [int] NOT NULL,
			[insert_date] [datetime] NOT NULL CONSTRAINT [insert_date]  DEFAULT (getdate()),
			[comments] [varchar](4068) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
			) ON [PRIMARY]

		
			";
	//	DEBUG("ssc = ", ssc);
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
			//	ShowExp(sc, er);
				return "";
			}
			try
			{
				
				string sqlString = " INSERT INTO currency (currency_name, rates, insert_by, insert_date) VALUES('NZD', 1, 0, GETDATE() ) ";
				myCommand = new SqlCommand(sqlString);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch
			{
				return "";
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +" \">");		
		}
		ShowExp(sc, e);
		return "";
	}
 
	int nMax = 1000;
	string stextReturn = "";
	string setStyle = "style=\"background-color:#EEEEEE\"";
	//****** Exchange Currency Rate Here ***************//
//	stextReturn += "<table width=100% border=1 cellspacing=0 cellpadding=2 bordercolor=#DDDDDD bgcolor=white";
//	stextReturn += " style=\"font-family:Verdana;font-sie:6pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">\r\n";
	string defaultCurrencyName = GetSiteSettings("default_currency_name", "NZD");
	string defaultCurrencyRate = GetSiteSettings("currency_rate_"+ defaultCurrencyName.ToLower(), "1");
	stextReturn += "\r\n";
	stextReturn += "<tr><td><table width=100% border=0 cellspacing=1 cellpadding=1 bordercolorlight=#44444 bordercolordark=#AAAAAA bgcolor=#057FCA";
	stextReturn += "style=\"font-family:Verdana;font-size:8pt;fixed\">\r\n";
	stextReturn += "<tr><td height='45' colspan='3' align='center' bgcolor='#057FCA' class='style10'><font size=3 color=white><b>Exchange Currency</b></font></span></td></tr><tr bordercolor='#003366'>";
//	stextReturn += "<tr><td colspan=2>";
 //   stextReturn += "<table width='98%' border='1' cellpadding='0' cellspacing='0' bordercolor='#CCCCCC' > ";
	stextReturn += "\r\n<tr><td><select name=currency onchange=\"javascript:calExCurrency(this.options[selectedIndex].value, document.f.moreExRate.value); \">";
	if(rows == 0)
	{
		stextReturn += "<option value=1>"+ defaultCurrencyName +"</option>";
	}
	else
	{
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dspc.Tables["currencyExchange"].Rows[i];
			string currency_name = dr["currency_name"].ToString();
			string rates = dr["rates"].ToString();	
			currency_name = currency_name.ToUpper();
					
			stextReturn += "<option value="+ rates +"";
			if(currency_name.ToLower() == defaultCurrencyName.ToLower())
			{
				stextReturn += " seleced ";
				defaultCurrencyRate = rates;
			}
			stextReturn += ">"+ currency_name +"</option>";
		}
	}
	stextReturn += "</select>";
	stextReturn += "<input type=text size=6 name=exRate readonly "+ setStyle +" value='"+ defaultCurrencyRate +"' onchange=\"javascript:calExCurrency(this.value, document.f.moreExRate.value);\"></td>";
	
	stextReturn += "<td><b>TOTAL :</b></td><td><input readonly "+ setStyle +" type=text name=totalfCurrency size=16></td></tr>";
	stextReturn += "<tr><td></td><td colspan=1><b>RECEIVE :</b></td><td><input type=text name=totalReceived size=16 onchange=\"javascript:calExCurrency(document.f.exRate.value, document.f.moreExRate.value);\"></td></tr>";
	stextReturn += "<tr><td></td><td colspan=1><b><font color='red'>CHANGE :</font></b></td><td><input readonly  "+ setStyle +" type=text name=totalExchangeChanged size=16></td></tr>";
	
	stextReturn += "\r\n<tr><td colspan=1><select name=moreCurrency onchange=\"javascript:calExCurrency(document.f.exRate.value, this.options[selectedIndex].value); \">";
	if(rows == 0)
	{
		stextReturn += "<option value=1>"+ defaultCurrencyName +"</option>";
	}
	else
	{
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dspc.Tables["currencyExchange"].Rows[i];
			string currency_name = dr["currency_name"].ToString();
			string rates = dr["rates"].ToString();	
			currency_name = currency_name.ToUpper();
					
			stextReturn += "<option value="+ rates +"";
			if(currency_name.ToLower() == defaultCurrencyName.ToLower())
			{
				stextReturn += " seleced ";
				defaultCurrencyRate = rates;
			}
			stextReturn += ">"+ currency_name +"</option>";
		}
	}
	stextReturn += "</select>";
	stextReturn += "<input type=text size=6 name=moreExRate readonly  "+ setStyle +" value='"+ defaultCurrencyRate +"' ></td>";
	stextReturn += "\r\n<td colspan=1><b>RECEIVE MORE :</b></td><td>";
	stextReturn += "<input type=text size=9 name=moreMoney onchange=\"javascript:calExCurrency(document.f.exRate.value, document.f.moreExRate.value);\"></td></tr>";
	stextReturn += "<tr><td></td><td colspan=1><b><font color='red'>CHANGE in "+ defaultCurrencyName.ToUpper() +" :</b></font></td><td><input readonly type=text name=totalChangedNZD size=9></td></tr>";
	stextReturn += "</table></td></tr>";
//	stextReturn += "</table>";
	//*********** End Here ***************//
	return stextReturn;
}

</script>

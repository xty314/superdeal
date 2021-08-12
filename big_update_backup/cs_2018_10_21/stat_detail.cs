<!-- #include file="stat_detail_fun.cs" -->
<!-- #include file="page_index.cs" -->
<script runat=server>

string m_periodindex = "1";
string m_Tbalance = "0";
string m_type = "";
string m_datePeriod = "";
string m_dateSql = "";
string m_code = "";
DataRow[] m_dra = null;
string[] m_EachMonth = new string[16];
string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
string fromdate ="";
string todate = "";
string rangdate ="";
int m_nPeriod = 0;
bool m_bPickTime = false;
string m_branchID = "";
//double m_dCardBalance = 0;
double m_dUnPaidTotal = 0;
string m_directory = "0";
string tableWidth = "97%";

void SPage_Load()
{
	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
		m_custID = Request.QueryString["ci"];

	if(m_sSite != "admin")
		m_custID = Session["card_id"].ToString(); //customer can only view his own statment, of course
	
	if(Request.QueryString["dir"] != null && Request.QueryString["dir"] != "")
		m_directory = Request.QueryString["dir"];

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

	int i = 0;
	
	int day = 1;
	int month = DateTime.Now.Month;
	int year = DateTime.Now.Year;
    m_type = Request.QueryString["t"];
 		
	

	if(Request.Form["period"] != null)
		m_nPeriod = MyIntParse(Request.Form["period"]);
	if(Request.QueryString["period"] != null)
	    m_nPeriod = MyIntParse(Request.QueryString["period"]); 
	if(Request.Form["code"] != null)
		m_code = Request.Form["code"];
	if(Request.Form["t"] != null)
		m_type = Request.Form["t"];

	if(Request.Form["Datepicker1_day"] != null)
	{
		m_sdFrom = Request.Form["Datepicker1_day"] + "-" + Request.Form["Datepicker1_month"] + "-" + Request.Form["Datepicker1_year"];
		m_sdTo = Request.Form["Datepicker2_day"] + "-" + Request.Form["Datepicker2_month"] + "-" + Request.Form["Datepicker2_year"];

		Session["report_date_from"] = m_sdFrom;
		Session["report_date_to"] = m_sdTo;
	}
	if(Request.Form["pick_month1"] != null)
	{
		m_smFrom = Request.Form["pick_month1"];
		m_smTo = Request.Form["pick_month2"];
		m_syFrom = Request.Form["pick_year1"];
		m_syTo = Request.Form["pick_year2"];
	
	}
	if(Request.QueryString["frm"] != null)
	{
		m_sdFrom = Request.QueryString["frm"].ToString();
		m_sdTo = Request.QueryString["to"].ToString();
	}
	if(Request.QueryString["pr"] != null)
		m_nPeriod = int.Parse(Request.QueryString["pr"].ToString());

	Session["report_period"] = m_nPeriod;
	
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "Today";
		break;
	case 1:
		m_datePeriod = "Yestoday";
		break;
	case 6:
		m_datePeriod = "The Day Before Yesterday";
		break;
		
	case 2:
		m_datePeriod = "This Week";
		break;
	case 3:
		m_datePeriod = "This Month";
		break;
	case 4:
		m_datePeriod = "From <font color=green>" + m_sdFrom + "</font>";
		m_datePeriod += " To <font color=red>" + m_sdTo + "</font>";
		break;
	case 5:
		m_datePeriod = "From <font color=green>" + m_EachMonth[int.Parse(m_smFrom)-1] + "-"+ m_syFrom +"</font>";
		m_datePeriod += " To <font color=red>" + m_EachMonth[int.Parse(m_smTo)-1] + "-"+ m_syTo +"</font>";
		break;
	default:
		break;
	}
  string m_ShowFullReport = Request.Form["period"];

	if(m_timeOpt != Request.QueryString["p"])
		m_timeOpt = Request.QueryString["p"];
	
	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
	{
		m_timeOpt = "4";
	
		if(!GetSelectedCust(m_custID))
			return;
		if(!GetInvRecords(m_custID))
			return;

		Response.Write(PrintStatmentDetails());
		return;
	}
	else if(Request.QueryString["fpaid"] == "1")
	{
		
	        if(Request.QueryString["d"] == null || Request.QueryString["d"] =="" )
		      PrintMainPage();
			else
			{
				if(!GetFullyPaidInvoices())
				return;
				BindFullyPaid();
			}
		
		return;
	}


	PrintStatHeader();
 //   PrintMainPage();

	if(m_custID != "")
	{
		PrintDaysToPay();
		PrintCustStats();
		PrintBalDetails();
/*		if(m_dUnPaidTotal != m_dCardBalance)
		{
			if(Session["email"] != null && m_sSite == "admin")
			{
				if(Session["email"].ToString() == "darcy@eznz.com")
				{
					Response.Write("<a href=statement.aspx?ci=" + m_custID + "&rb=1 class=o>Reset Balance</a>");
				}
			}
		}
*/
	}

	if(Request.QueryString["rb"] == "1")
	{
		if(Session["email"] != null && m_sSite == "admin")
		{
			if(Session["email"].ToString() == "darcy@eznz.com")
			{
				DoResetBalance();
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=statement.aspx?ci=" + m_custID + "\">");
			}
		}
	}
}



void PrintMainPage()
{

	Response.Write("<form name=f action='stat_detail.aspx?");
    Response.Write("ci="+Request.QueryString["ci"]+"&fpaid="+Request.QueryString["fpaid"]+"&f=0&d=0");
	
	Response.Write("' method=post>");

	Response.Write("<br><center><h3>Select Report</h3>");
	if(Session["branch_support"] != null)
	{

		{
			Response.Write("<b>Branch : </b>");
			PrintBranchNameOptions(m_branchID, "", true);
		}

	}

	Response.Write("<table align=center cellspacing=1 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\"><td colspan=6>");
	

	string uri = Request.ServerVariables["URL"].ToString();
	Response.Write("<tr><td colspan=6>&nbsp;</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Select Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>Today</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Yestoday</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=6>The Day Before Yesterday</td></tr>");
	Response.Write("<input type=hidden name=dateSelected value=1 >");
	/////allow only authorized user to access...
	if(bGetAllowAccessID(Session[m_sCompanyName + "AccessLevel"].ToString()))
	{
		Response.Write("<tr><td colspan=6><input type=radio name=period value=2>This Week</td></tr>");
		Response.Write("<tr><td colspan=6><input type=radio name=period value=3>This Month</td></tr>");
		Response.Write("<tr><td colspan=6><input type=radio name=period value=4>Select Date Range</td></tr>");
	
		int i = 1;
		datePicker(); //call date picker function
		Response.Write("<tr><td><b> &nbsp; From Date </b>");
		
		string s_day = DateTime.Now.ToString("dd");
		string s_month = DateTime.Now.ToString("MM");
		string s_year = DateTime.Now.ToString("yyyy");
		Response.Write("<select name='Datepicker1_day' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
		for(int d=1; d<32; d++)
		{
			Response.Write("<option value="+ d +">"+d+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='Datepicker1_month' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			if(int.Parse(s_month) == m)
				Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
			else
				Response.Write("<option value="+m+">"+txtMonth+"</option>");
		}
		
		Response.Write("</select>");
		Response.Write("<select name='Datepicker1_year' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
		for(int y=2000; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");

		Response.Write("<input type=hidden name='Datepicker1'>");
		Response.Write("<input type=hidden name='Datepicker1_conv'>");
		Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker1',1)");
		Response.Write("</script ");
		Response.Write(">");

		Response.Write("<td> &nbsp; TO: ");
		Response.Write("<select name='Datepicker2_day' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
		for(int d=1; d<32; d++)
		{
			if(int.Parse(s_day) == d)
				Response.Write("<option value="+ d +" selected>"+d+"</option>");
			else
				Response.Write("<option value="+ d +">"+d+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<select name='Datepicker2_month' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			if(int.Parse(s_month) == m)
				Response.Write("<option value="+m+" selected>"+txtMonth+"</option>");
			else
				Response.Write("<option value="+m+">"+txtMonth+"</option>");
		}
		
		Response.Write("</select>");
		Response.Write("<select name='Datepicker2_year' onChange=\"document.f.period[5].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
		for(int y=2000; y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");
		Response.Write("<input type=hidden name='Datepicker2'>");
		Response.Write("<input type=hidden name='Datepicker2_conv'>");
		Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[0].Datepicker2',1)");
		Response.Write("</script ");
		Response.Write(">");
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td align=right colspan=6><input type=submit name=cmd value='View Report' " + Session["button_style"] + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");

}

void PrintStatHeader()
{
	string uri = Request.ServerVariables["URL"] + "?r="+ DateTime.Now.ToOADate() +"";
//	Response.Write("<br><br><center><h3><font color=#495C77 face='Tahoma' size=4><b>STATEMENT</b></font></h3>");
	Response.Write("<form id=search name=search action=" + uri + " method=post>");
		Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Statement</b><font color=red><b>");	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	

	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><td colspan=10 valign=bottom>");

	if(m_sSite == "admin")
	{
		
		Response.Write("<table align=center><tr><td><b>Customer :</b></td><td>");
		Response.Write("<input type=editbox size=20 name=ckw></td><td>");

		Response.Write("<script");
		Response.Write(">");
		Response.Write("document.search.ckw.focus();");
		Response.Write("</script");
		Response.Write(">");
/*string supid = Request.QueryString["sup_id"];
if(supid == null || supid == "")
supid = "1";
*/
		Response.Write("<input type=submit name=cmd value=Search "+ Session["button_style"] +">" );
		Response.Write("<input type=button name=cmd value='Cancel' "+ Session["button_style"] +"");
		Response.Write(" onClick=window.location=('statement.aspx?r=" + DateTime.Now.ToOADate() + "')>");
		Response.Write("</td>");
	/*	Response.Write("<td><select name=card_type onchange=\"window.location=('"+ uri +"&sup_id=' + this.options[this.selectedIndex].value)\" >");
		Response.Write("<option value=''>All");
	Response.Write(GetEnumOptions("card_type", supid , false, true));
	Response.Write("</select></td>");
	*/
	//	string uri = Request.ServerVariables["URL"] + Request.ServerVaraibles["QUERY_STRING"];
		Response.Write("<td><a title='click to list all suppliers' href='statement.aspx?&sup_id=3&r=" + DateTime.Now.ToOADate() + "' class=o>Suppliers</a> |</td>");
		Response.Write("<td><a title='click to list all dealers' href='statement.aspx?&sup_id=2&r=" + DateTime.Now.ToOADate() + "' class=o>Dealers</a> |</td>");
		Response.Write("<td><a title='click to list all customers' href='statement.aspx?&sup_id=1&r=" + DateTime.Now.ToOADate() + "' class=o>Customers</a> |</td>");
		Response.Write("<td>");
		Response.Write("Directory/Group:");
		Response.Write("<select name=directory onchange=\"window.location=('" + uri + "&sup_id="+ Request.QueryString["sup_id"] +"&dir='+this.options[this.selectedIndex].value)\">");
		Response.Write("<option value=0>All</option>");
		Response.Write(GetEnumOptions("card_dir", m_directory));
		Response.Write("</select>");
		Response.Write("</td>");
//		Response.Write("<td><a title='click to list all Others' href='statement.aspx?&sup_id=5&r=" + DateTime.Now.ToOADate() + "' class=o>Others</a></td>");
		Response.Write("</tr></table></form>\r\n");
		Response.Write("</td></tr>");
		if(Request.QueryString["ci"] == null || Request.QueryString["ci"] == "")
		{
			if(!DoCustomerSearchAndList())
				return;
		}
	}
}

void PrintCustStats()
{
	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#7D93B5;font-weight:bold;font-face:Tahoma;\">");
	Response.Write("<td width=120>Account-ID:</td><td width=120>Customer Name:</td><td width=120>Credit Terms:</td>");
	Response.Write("<td width=200>PH:</td><td>");
	Response.Write("Company Name: </td></tr>");
//	Response.Write("Total Balance: </td></tr>\r\n");
	Session["custname"] =dst.Tables["cust_gen"].Rows[0]["trading_name"].ToString();
	if(!GetSelectedCust(m_custID))
		return;
	m_scredit_terms_id = dst.Tables["cust_gen"].Rows[0]["terms_id"].ToString();
	m_scredit_terms = dst.Tables["cust_gen"].Rows[0]["credit_terms"].ToString();
	Response.Write("<tr><td><b>"+ m_custID+"</td><td><b>" + dst.Tables["cust_gen"].Rows[0]["name"].ToString() + "</b></td><td>");
	Response.Write("<b>" + m_scredit_terms + "</b></td><td>");
	Response.Write(dst.Tables["cust_gen"].Rows[0]["phone"].ToString());
	if(dst.Tables["cust_gen"].Rows[0]["phone"].ToString() != "")
		Response.Write("; Fax: " + dst.Tables["cust_gen"].Rows[0]["phone"].ToString());
	Response.Write("</td><td>");
	Response.Write(dst.Tables["cust_gen"].Rows[0]["trading_name"].ToString() + "</td>");
	Response.Write("</tr>");

//	m_dCardBalance = double.Parse(dst.Tables["cust_gen"].Rows[0]["balance"].ToString());
//	Response.Write(m_dCardBalance.ToString("c") + "</td></tr>\r\n");

	//Customer Address
/*
	string s_address = dst.Tables["cust_gen"].Rows[0]["nameB"].ToString();
	if(dst.Tables["cust_gen"].Rows[0]["companyB"].ToString() != "")
		s_address += "<br>" + dst.Tables["cust_gen"].Rows[0]["companyB"].ToString();
	if(dst.Tables["cust_gen"].Rows[0]["address1B"].ToString() != "")
		s_address += "<br>" + dst.Tables["cust_gen"].Rows[0]["address1B"].ToString();
	if(dst.Tables["cust_gen"].Rows[0]["address2B"].ToString() != "")
		s_address += "<br>" + dst.Tables["cust_gen"].Rows[0]["address2B"].ToString();
	if(dst.Tables["cust_gen"].Rows[0]["cityB"].ToString() != "")
		s_address += "<br>" + dst.Tables["cust_gen"].Rows[0]["cityB"].ToString();
	if(dst.Tables["cust_gen"].Rows[0]["countryB"].ToString() != "")
		s_address += "  " + dst.Tables["cust_gen"].Rows[0]["countryB"].ToString();
*/
    string s_address = "";
	if(dst.Tables["cust_gen"].Rows[0]["postal1"].ToString() != "")
	{
	  if(dst.Tables["cust_gen"].Rows[0]["postal1"].ToString() != "")
	      s_address += dst.Tables["cust_gen"].Rows[0]["postal1"].ToString();
	  if(dst.Tables["cust_gen"].Rows[0]["postal2"].ToString() != "")
	      s_address +="<br>"+ dst.Tables["cust_gen"].Rows[0]["postal2"].ToString();
	  if(dst.Tables["cust_gen"].Rows[0]["postal3"].ToString() != "")
 		  s_address +="<br>"+ dst.Tables["cust_gen"].Rows[0]["postal3"].ToString();
   	 }
	 else
	{
	  if(dst.Tables["cust_gen"].Rows[0]["address1"].ToString() != "")
	      s_address += dst.Tables["cust_gen"].Rows[0]["address1"].ToString();
	  if(dst.Tables["cust_gen"].Rows[0]["address2"].ToString() != "")
	      s_address +="<br>"+ dst.Tables["cust_gen"].Rows[0]["address2"].ToString();
	  if(dst.Tables["cust_gen"].Rows[0]["address3"].ToString() != "")
 		  s_address +="<br>"+ dst.Tables["cust_gen"].Rows[0]["address3"].ToString();
	 }
   	  if(dst.Tables["cust_gen"].Rows[0]["city"].ToString() != "")
	  	 s_address +="<br>"+ dst.Tables["cust_gen"].Rows[0]["city"].ToString();

	
	if(s_address != "")
	{
		Response.Write("<tr><td colspan=5><hr></td></tr><tr bgcolor=#CCCCCC><td colspan=2><b>Billing Address:</b></td></tr>");
		Response.Write("<tr><td colspan=5>" + s_address + "</td></tr>");
	}
	Response.Write("<tr><td colspan=5><hr></td></tr></table>\r\n");

	//Option table
	string s_checked1 = "";
	string s_checked2 = "";
	string s_checked3 = "";
	string s_checked4 = "";
	string s_checked5 = "";
	string s_checked6 = "";
	string s_colbgc1 = "";
	string s_colbgc2 = "";
	string s_colbgc3 = "";
	string s_colbgc4 = "";
	string s_colbgc5 = "";
	string s_colbgc6 = "";

	if(m_timeOpt == "0")
	{
		s_checked1 = " checked";
		s_colbgc1 = " bgcolor=A5F2C0";
	}
	else if(m_timeOpt =="1")
	{
		s_checked2 = " checked";
		s_colbgc2 = " bgcolor=A5F2C0";
	}	
	else if(m_timeOpt =="2")
	{
		s_checked3 = " checked";	
		s_colbgc3 = " bgcolor=A5F2C0";
	}
	else if(m_timeOpt =="3")
	{
		s_checked4 = " checked";	
		s_colbgc4 = " bgcolor=A5F2C0";
	}
	else if(m_timeOpt =="4")
	{
		s_checked5 = " checked";	
		s_colbgc5 = " bgcolor=A5F2C0";
	}
	else
	{
		s_checked6 = " checked";
		s_colbgc6 = " bgcolor=A5F2C0";
	}
	string s_url = Request.ServerVariables["URL"] + "?ci=" + Request.QueryString["ci"];
	
	Response.Write("<form name=timeOptfrm action=" + s_url + "&p=" + m_timeOpt + "method=post>");
//	Response.Write("<table border=1 cellspacing=0>");
	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=10><font color=#495C77><b>To: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + "</b></font></td></tr>");
	Response.Write("<tr><td bgcolor=#7D93B5><font face=tahoma color=white size=2><b>Days:&nbsp;&nbsp;&nbsp;</b></font></td>");
	
	Response.Write("<td" + s_colbgc1 + ">&nbsp;<input type=radio name=period value='0'" + s_checked1);
	Response.Write(" onclick=\"window.location=('"+s_url+"&p='+document.timeOptfrm.period[0].value)\"");
	Response.Write(">");
	Response.Write("&nbsp;<b>Current &nbsp; </b></td>\r\n");
	
	Response.Write("<td" + s_colbgc2 + ">&nbsp;<input type=radio name=period value='1'" + s_checked2);
	Response.Write(" onclick=\"window.location=('"+s_url+"&p='+document.timeOptfrm.period[1].value)\"");
	Response.Write(">");
	//Response.Write(">&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	if(m_scredit_terms_id == "4")
		Response.Write("&nbsp;<b>7 Days &nbsp; </b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		Response.Write("&nbsp;<b>14 Days &nbsp; </b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	Response.Write("&nbsp;<b>14 Days &nbsp; </b></td>\r\n");
	else
		Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	
	Response.Write("<td" + s_colbgc3 + ">&nbsp;<input type=radio name=period value='2'" + s_checked3);
	Response.Write(" onclick=\"window.location=('"+s_url+"&p='+document.timeOptfrm.period[2].value)\"");
	Response.Write(">");
	//Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	if(m_scredit_terms_id == "4")
		Response.Write("&nbsp;<b>14 Days &nbsp; </b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	else
		Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	
	
	Response.Write("<td" + s_colbgc4 + ">&nbsp;<input type=radio name=period value='3'" + s_checked4);
	Response.Write(" onclick=\"window.location=('"+s_url+"&p='+document.timeOptfrm.period[3].value)\"");
	Response.Write(">");
	//Response.Write("&nbsp;<b>90 Days &nbsp; </b></td>\r\n");
	if(m_scredit_terms_id == "4")
		Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	else
		Response.Write("&nbsp;<b>90 Days &nbsp; </b></td>\r\n");
	
	
	Response.Write("<td" + s_colbgc5 + ">&nbsp;&nbsp;&nbsp;&nbsp;<input type=radio name=period value='4'" + s_checked5);
	Response.Write(" onclick=\"window.location=('"+s_url+"&p='+document.timeOptfrm.period[4].value)\"");
	Response.Write(">");
	Response.Write("&nbsp;<b>All&nbsp;&nbsp;&nbsp;</b></td>\r\n");
	Response.Write("<td><b>&nbsp&nbsp; Credits &nbsp&nbsp;</b></td>");
	Response.Write("<td><b>&nbsp&nbsp; Total_Due &nbsp&nbsp;</b></td>");

	Response.Write("</tr>");

//	Response.Write("<td" + s_colbgc6 + ">&nbsp;&nbsp;&nbsp;&nbsp;<input type=radio name=period value='5'" + s_checked6);
//	Response.Write(" onclick=\"window.location=('"+s_url+"&p='+document.timeOptfrm.period[5].value)\"");
//	Response.Write(">");
//	Response.Write("&nbsp;<b>To Be Billed/Emailed&nbsp;&nbsp;&nbsp;</b></td></tr>\r\n");
	
	//DateTime d_date1 = new DateTime(); //DateTime.Parse("dd-mm-yyyy"); DateTime.Now;
	double[] dSubBalance = new double[5];
	//sub-"total balance"
	for(int i = 0; i<5; i++)
	{
		if(!GetSubBalance(i, m_custID, ref dSubBalance[i]))
			return;		
//DEBUG("i = ", i);
//DEBUG("dSubBalance = ", dSubBalance[i].ToString());
	}		

	double dRowTotal = dSubBalance[0] + dSubBalance[1] + dSubBalance[2] + dSubBalance[3];
	m_dUnPaidTotal = dRowTotal;
	double dCreditTotal = GetTotalCredit(m_custID);
	double dTotalDue = dRowTotal - dCreditTotal;

	Response.Write("<tr><td bgcolor=#7D93B5><font face=tahoma color=white size=2><b>Sub Balance:&nbsp;</b></font></td>");
	Response.Write("<td align=center" + s_colbgc1 + ">" + dSubBalance[0].ToString("c") + "</td>");
	Response.Write("<td align=center" + s_colbgc2 + ">" + dSubBalance[1].ToString("c") + "</td>");
	Response.Write("<td align=center" + s_colbgc3 + ">" + dSubBalance[2].ToString("c") + "</td>");
	Response.Write("<td align=center" + s_colbgc4 + ">" + dSubBalance[3].ToString("c") + "</td>");
	Response.Write("<td align=right" + s_colbgc5 + ">&nbsp;&nbsp;" + dRowTotal.ToString("c") + "</td>");
	Response.Write("<td align=right" + s_colbgc5 + ">&nbsp;&nbsp;" + dCreditTotal.ToString("c") + "</td>");
	Response.Write("<td align=right" + s_colbgc5 + ">&nbsp;&nbsp;" + dTotalDue.ToString("c") + "</td>");
//	Response.Write("<td align=center" + s_colbgc6 + ">" + dSubBalance[5].ToString("c") + "</td>"); //to be billed
	Response.Write("</tr></table></form>\r\n");
}

void PrintBalDetails()
{
//	if(Request.QueryString["p"] != null && Request.QueryString["p"] != "")
	{	
		if(!GetInvRecords(m_custID))
			return;
	}
	string sTimeLbl = "";
	
if(m_scredit_terms_id == "4")
{
	if(m_timeOpt == "0")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;Current</b></font>";
	if(m_timeOpt == "1")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 7 Days</b></font>";
	if(m_timeOpt == "2")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 14 Days</b></font>";
	if(m_timeOpt == "3")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 30 Days</b></font>";
	if(m_timeOpt == "4")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp; All</b></font>";
}
else if(m_scredit_terms_id == "5")
{
	if(m_timeOpt == "0")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;Current</b></font>";
	if(m_timeOpt == "1")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 14 Days</b></font>";
	if(m_timeOpt == "2")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 30 Days</b></font>";
	if(m_timeOpt == "3")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 60 Days</b></font>";
	if(m_timeOpt == "4")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp; All</b></font>";
}
else
{
	if(m_timeOpt == "0")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;Current</b></font>";
	if(m_timeOpt == "1")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 30 Days</b></font>";
	if(m_timeOpt == "2")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 60 Days</b></font>";
	if(m_timeOpt == "3")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp;in 90 Days</b></font>";
	if(m_timeOpt == "4")
		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp; All</b></font>";
}
//	if(m_timeOpt == "5")
//		sTimeLbl = "<font color=blue><b>&nbsp;&nbsp; To Be Billed/Emailed </b><i>( >=30 days)</i></font>";

	if(dst.Tables["invoice_rec"].Rows.Count == 0)
	{
		Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;<font color=#495C77><b>0</b></font><b>&nbsp;Records found:</b>" + sTimeLbl);
		if(m_sSite == "admin")
		{
			Response.Write("&nbsp;&nbsp;<input type=button value='Fully Paid Invoices' " + Session["button_style"]);
			Response.Write(" onclick=window.location=('stat_detail.aspx?ci=" + m_custID + "&fpaid=1&r=" + DateTime.Now.ToOADate() + "')>");
			Response.Write("&nbsp;&nbsp;<input type=button value='Back to Statement' " + Session["button_style"]);
			Response.Write(" onclick=window.location=('statement.aspx?r=" + DateTime.Now.ToOADate() + "')>");
		}
		return;
	}
	
	Response.Write("<table width='"+ tableWidth +"' align=center><tr><td colspan=6><font color=red><b>" + dst.Tables["invoice_rec"].Rows.Count.ToString());
	Response.Write("</b></font><b>&nbsp;Records found:</b>" + sTimeLbl);
	string s_vdUrl = Request.ServerVariables["URL"] + "?" + Request.ServerVariables["QUERY_STRING"];
	Response.Write("&nbsp;&nbsp;&nbsp;<input type=button " + Session["button_style"] + " onclick=window.open('"+ s_vdUrl + "&t=vd') value=' Printable Copy '>");

	if(m_sSite == "admin")
	{
		Response.Write("&nbsp;&nbsp;<input type=button " + Session["button_style"]);
		Response.Write(" onclick=window.location=('status.aspx?ci=" + m_custID + "&r=" + DateTime.Now.ToOADate() + "') value=' Order Trace '>");

		Response.Write("&nbsp;&nbsp;<input type=button " + Session["button_style"]);
		Response.Write(" onclick=window.location=('broadmail.aspx?ci=" + m_custID + "&t=m&r=" + DateTime.Now.ToOADate() + "') value=' e-mail Statement '>");

		Response.Write("&nbsp;&nbsp;<input type=button " + Session["button_style"]);
		Response.Write(" onclick=window.location=('stat_detail.aspx?ci=" + m_custID + "&fpaid=1&r=" + DateTime.Now.ToOADate() + "') value='Fully Paid Invoices'>");
		Response.Write("&nbsp;&nbsp;<input type=button value='Back to Statement' " + Session["button_style"]);
		Response.Write(" onclick=window.location=('statement.aspx?r=" + DateTime.Now.ToOADate() + "')>");
	}
	Response.Write("</td></tr>\r\n");
	Response.Write("<tr><td colspan=6><hr></td></tr>");
	//Row headers
	Response.Write("<tr bgcolor=#CCCCCC><td width=15% align=center><b>Date</b></td>");
	Response.Write("<td width=15% align=center><b>Invoice No.</b></td>");
	Response.Write("<td width=15% align=center><b>PO No.</b></td>");
	Response.Write("<td width=15% align=center><b>Charge</b></td>");
	Response.Write("<td width=15% align=center><b>Amount Paid</b></td>");
	Response.Write("<td width=20% align=center><b>Balance</b></td></tr>");
	//Print Rows
	double dCharge = 0;
	
	for(int i = 0; i < dst.Tables["invoice_rec"].Rows.Count; i++)
	{
		Response.Write("<tr>");
		DataRow dr = dst.Tables["invoice_rec"].Rows[i];
		Response.Write("<td align=center>" + DateTime.Parse(dr["commit_date"].ToString()).ToString("dd/MM/yy") + "</td>");
		Response.Write("<td align=center><a href=");
		if(!bCardType(ref m_custID))
			Response.Write("invoice.aspx?n=" + dr["invoice_number"].ToString() + "");
		else
			Response.Write("purchase.aspx?t=pp&n=" + dr["pid"].ToString() + "");
		Response.Write(" target=_blank>"); 
		string inv_number = dr["invoice_number"].ToString();
		if(inv_number == "0")
			inv_number = "";
		//Response.Write(dr["invoice_number"].ToString() + "</a></td>");
		Response.Write(inv_number + "</a></td>");
		Response.Write("<td align=center>" + dr["cust_ponumber"].ToString() + "</td>");
		//total chargeable amount "ship + price"(GST excl.) 
		dCharge = double.Parse(dr["total"].ToString());
		Response.Write("<td align=right>" + dCharge.ToString("c") + "</td>");
		
		if(double.Parse(dr["amount_paid"].ToString()) > 0)
			Response.Write("<td align=center>" + double.Parse(dr["amount_paid"].ToString()).ToString("c") + "</td>");
		else
			Response.Write("<td align=center>&nbsp;</td>");

		Response.Write("<td align=right>" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "</td>");
	}
	Response.Write("</tr>");
	Response.Write("</table>");
}

bool DoCustomerSearchAndList()
{
	string uri = Request.ServerVariables["URL"] + "?";	// + Request.ServerVariables["QUERY_STRING"];
	int rows = 0;
	string kw = "'%" + EncodeQuote(Request.Form["ckw"]) + "%'";
	if(Request.Form["ckw"] == null || Request.Form["ckw"] == "")
		kw = "'%%'";
	string card_type = Request.QueryString["sup_id"];

	//string sc = "SELECT *, '" + uri + "' + '&ci=' + LTRIM(STR(id)) + '&p=0' AS uri FROM card ";
	string sc = "SELECT e1.name AS directory, c1.name AS sales, e.name AS credit_term, c.*, '" + uri + "' + '&ci=' + LTRIM(STR(c.id)) + '&p=0' AS uri ";
	sc += ", 'tranlist.aspx?cid=' + LTRIM(STR(c.id)) AS uritl "; 
	sc += " ,'ecard.aspx?id=' + LTRIM(STR(c.id)) + '' AS ecard ";

	sc += ", ";
	if(card_type == "3")
	{
		sc += " ISNULL((SELECT ISNULL(SUM(total_amount - amount_paid),0) FROM purchase WHERE supplier_id = c.id AND type = 4), 0) ";		
	}
	else
	{
	sc += " ISNULL((SELECT SUM(total - amount_paid) ";
	sc += " FROM invoice ";
	sc += " WHERE card_id = c.id), (SELECT ISNULL(SUM(total_amount - amount_paid),0) FROM purchase WHERE supplier_id = c.id)) ";
	sc += " - ";
	sc += " ISNULL((SELECT SUM(amount - amount_applied) ";
	sc += " FROM credit ";
	sc += " WHERE card_id = c.id), 0) ";
	}
	sc += " AS total_balance ";
	sc += ", ISNULL((SELECT SUM(amount-amount_applied) FROM credit WHERE card_id = c.id),0) AS total_credit ";

	sc += "FROM card c JOIN enum e ON e.id = c.credit_term";
	sc += " JOIN enum e1 ON e1.id = c.directory AND e1.class='card_dir' ";
	sc += " LEFT OUTER JOIN card c1 ON c1.id = c.sales ";
	sc += " WHERE c.main_card_id is null AND (";
	if(IsInteger(Request.Form["ckw"]))
		sc += " c.id=" + Request.Form["ckw"];
	else
		sc += " c.name LIKE " + kw + " OR c.email LIKE " + kw + " OR c.company LIKE " + kw;
	//sc += ") ORDER BY c.balance DESC";
	sc += ") ";
	sc += " AND e.class = 'credit_terms' ";
	if(Request.QueryString["sup_id"] != null && Request.QueryString["sup_id"] != "")
		sc += " AND c.type = "+ Request.QueryString["sup_id"] +"";
	else
		sc += " AND c.type <> 3 ";
	if(m_directory != "0")
		sc += " AND c.directory = "+ m_directory;
	sc += " ORDER BY total_balance DESC ";
//sDEBUG("PRINT sc=", sc);
	try
	{
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "card");
		if(rows == 1)
		{
			string search_id = dst.Tables["card"].Rows[0]["id"].ToString();
			Trim(ref search_id);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=statement.aspx?p=0&ci=" + search_id + "\">");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	BindGrid();

	return true;
}

void BindGrid()
{
	DataView source = new DataView(dst.Tables["card"]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_PageA(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

bool GetFullyPaidInvoices()
{
	string sc = " ";
	string sort = Request.QueryString["f"];
	string paidStatus = "0";
	if(sort == "2")
	     paidStatus = "0";
	else if(sort == "1")
	     paidStatus ="1";
		 
    switch(m_nPeriod)
	{
	case 0:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";
		break;
	case 6:
		m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 2 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(week, i.commit_date, GETDATE()) = 0 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}
//DEBUG("bCardType ",bCardType(ref m_custID).ToString());	 
	if(!bCardType(ref m_custID))
	{
		sc = " SET DATEFORMAT dmy SELECT DISTINCT '0' AS pid, i.invoice_number, i.commit_date, i.amount_paid ";
		sc += ", i.cust_ponumber, d.trans_date, d.payment_method, d.payment_ref ";
		sc += ", i.total - amount_paid AS rest";
		sc += ", i.total AS invTotal";
		sc += ", c.trading_name as custname";
		sc += " FROM invoice i JOIN tran_detail d ON d.invoice_number LIKE '%' + CONVERT(varchar(50), i.invoice_number) + '%'";
		sc += " JOIN card c ON c.id = i.card_id";
		sc += " WHERE 1=1 ";
		if(sort != "0")
		sc += " AND i.paid = "+paidStatus;
		sc += " AND i.card_id = " + m_custID;
		if(Request.Form["dateSelected"] =="1")
		sc +=   m_dateSql;
		sc += " ORDER BY i.commit_date DESC";
	}
	else
	{
		sc = " SET DATEFORMAT dmy  SELECT DISTINCT i.id AS pid, i.po_number AS invoice_number, i.date_invoiced AS commit_date, ti.amount_applied AS amount_paid, i.po_number AS cust_ponumber, d.trans_date ";
		sc += ", i.total AS invTotal";
		sc += ", i.total_amount - amount_paid AS rest";
		sc += ", c.trading_name as custname";
		sc += " FROM purchase i "; //INNER JOIN ";
	//	sc += " tran_detail d ON d.invoice_number LIKE '%' + CONVERT(varchar(50), i.id) + '%' ";
		sc += " JOIN tran_invoice ti ON ti.invoice_number = i.id ";
		sc += " JOIN tran_detail d ON d.id = ti.tran_id ";
		sc += " JOIN card c ON c.id = i.supplier_id";
		sc += " WHERE (i.supplier_id = "+ m_custID +" AND i.date_invoiced is not null)  ";
		sc += " AND ti.purchase = 1";
		sc += " ORDER BY i.date_invoiced DESC"; 
	
	}
 
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst, "invoice");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void BindFullyPaid()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(dst.Tables["invoice"] != null)
		rows = dst.Tables["invoice"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?fpaid=1&ci=" + m_custID;
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	m_cPI.PageSize = 50;
	string sPageIndex = m_cPI.Print();

	string company = TSGetUserCompanyByID(m_custID);
	double dTotalPaid = 0;
	double dTotalUnpaid =0;
	double dTotalInv = 0;
	if(Request.QueryString["from"] != "" && Request.QueryString["from"] != null )
	    fromdate = Request.QueryString["from"].ToString();
	if(Request.QueryString["tod"] != "" &&  Request.QueryString["tod"] != null)
	    todate =  Request.QueryString["tod"].ToString();
	if( Request.QueryString["from"] != "" && Request.QueryString["tod"] != "" &&  Request.QueryString["from"] != null)
	    rangdate = "From "+ fromdate +" to  "+ todate;
	if(m_nPeriod != 4)
	    rangdate = m_datePeriod;

/*	Response.Write("<br><center><h3>FULLY PAID INVOICES - <font color=red>" + company + "</font></h3>");

	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
*/
   
    if(Request.QueryString["p"] !="1")
	{
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3>FULLY PAID INVOICES - <font color=red>" + company + "</font>");	
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write(" <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");
	}
	else
	{
	Response.Write(ReadSitePage("statement_header_full"));	
	Response.Write("<div align=center><table width="+tableWidth +" cellspacing=0  cellpadding=0 border=0>");
	Response.Write("<tr><td colspan=8 align=center style=\"font:bold 15px arial\"><b>Statement</b></td></tr>");
	Response.Write("<tr><td><b>Company name:</b> "+ dst.Tables["invoice"].Rows[0]["custname"].ToString()+"</td><td colspan=7 align=right><b>Date:</b>"+ rangdate+ "</td></tr>");
	Response.Write("</table></table>");
	}
	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6><br></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<th width=10%>Date</th>");
	Response.Write("<th width=14%>Invoice #</th>");
	//if(Session["po_number"] != "");
	//Response.Write("<th>PO #</th>");

	if(!bCardType(ref m_custID))
	Response.Write("<th width=14%>Sales Amount</th>");
	Response.Write("<th width=14%>Amount Paid</th>");
	if(!bCardType(ref m_custID))
	Response.Write("<th width=14%>Unpaid</th>");
	Response.Write("<th width=15%>Date Paid</th>");
	Response.Write("<th width=10%>Payment</th>");
	Response.Write("<th width=20%>Reference</th>");
	Response.Write("</tr>");
   string print = "";
   string printwidth;
   {
	if(Request.QueryString["p"] == "1")
	         print= "class=print";
			 printwidth ="width=60%";
			 
	}
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	bool bAlterColor = false;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = dst.Tables["invoice"].Rows[i];
		string idate = DateTime.Parse(dr["commit_date"].ToString()).ToString("dd-MM-yyyy");
		string invoice_number = dr["invoice_number"].ToString();
		string po_number = dr["cust_ponumber"].ToString();
		string amount_paid = dr["amount_paid"].ToString();
		string invTotal = dr["invTotal"].ToString();
		string unpaid = dr["rest"].ToString();
		string date_paid = DateTime.Parse(dr["trans_date"].ToString()).ToString("dd-MM-yyyy");
		string payment = GetEnumValue("payment_method", dr["payment_method"].ToString());
		string reference = dr["payment_ref"].ToString();
		string pid = dr["pid"].ToString();
        Session["po_number"] = po_number;
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td >" + idate + "</td>");
		Response.Write("<td ><a href=");
		if(!bCardType(ref m_custID))
			Response.Write("invoice.aspx?" + invoice_number + "");
		else
			Response.Write("purchase.aspx?t=pp&n=" + pid + "");
		Response.Write(" class=o target=blank>" + invoice_number + "</a></td>");
		//if(po_number !="")
		//Response.Write("<td >" + po_number + "</td>");
		if(!bCardType(ref m_custID))
		Response.Write("<td >" + MyDoubleParse(invTotal).ToString("c") + "</td>");
		Response.Write("<td >" + MyDoubleParse(amount_paid).ToString("c") + "</td>");
		if(!bCardType(ref m_custID))
		Response.Write("<td >" + MyDoubleParse(unpaid).ToString("c") + "</td>");
		Response.Write("<td >" + date_paid + "</td>");
		Response.Write("<td >" + payment + "</td>");
		Response.Write("<td  >" + reference + "</td>");
		Response.Write("</tr>");
		dTotalPaid += MyDoubleParse(amount_paid);
		dTotalUnpaid +=  MyDoubleParse(unpaid);
		dTotalInv += MyDoubleParse(invTotal);
		
	}
	   
	   Response.Write("<tr><td colspan=7 align=right ><b>Sales Amount :</td><td>" + dTotalInv.ToString("c")+"</b></td></tr>");
	   Response.Write("<tr><td colspan=7 align=right><b>Total Paid : </td><td> " + dTotalPaid.ToString("c")+"</b></td></tr>");
	   Response.Write("<tr><td colspan=7 align=right><b>Total Unpaid :</td><td>" + dTotalUnpaid.ToString("c")+"</b></td></tr>");
	if(Request.QueryString ["p"] !="1")
	{   
	Response.Write("<tr><td colspan=3 "+print+">" + sPageIndex + "</td><td colspan=4 align=right "+print+"><input type=button "+ Session["button_style"] +" value='Show Full Unpaid' onclick=\"window.location=('stat_detail.aspx?ci="+m_custID+"&fpaid=1&f=2&d=0');\"><input type=button "+ Session["button_style"] +" value='Show Full paid' onclick=\"window.location=('stat_detail.aspx?ci="+m_custID+"&fpaid=1&f=1&d=0');\"><input type=button "+ Session["button_style"] +" value='Show Full Statment' onclick=\"window.location=('stat_detail.aspx?ci="+m_custID+"&fpaid=1&f=0&d=0');\"><input type=button "+ Session["button_style"] +" value='Print Version' onclick=\"window.location=('stat_detail.aspx?ci="+m_custID+"&fpaid=1&f="+Request.QueryString["f"]+"&d=0&p=1&period="+m_nPeriod+"&from="+m_sdFrom+"&tod="+m_sdTo+"');\"><input type=button "+ Session["button_style"] +" value='Back to Statement' onclick=\"window.location=('statement.aspx?');\"></td></tr>");
     }
	Response.Write("</table>");
}

bool DoResetBalance()
{
	if(m_custID == null || m_custID == "")
		return false;

	string sc = " UPDATE card SET balance = " + m_dUnPaidTotal + " WHERE id = " + m_custID;
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

bool PrintDaysToPay()
{
	string pday12 = "0";
	string pday24 = "0";

	string sc = " SET DATEFORMAT dmy  SELECT AVG(DATEDIFF(day, i.commit_date, d.trans_date)) AS pday ";
	sc += " FROM invoice i JOIN tran_invoice t ON t.invoice_number = i.invoice_number ";
	sc += " JOIN tran_detail d ON d.id = t.tran_id ";
	sc += " WHERE i.card_id = " + m_custID;
	sc += " AND DATEDIFF(month, i.commit_date, GETDATE()) <= 12 ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "12pday") ==  1)
			pday12 = dst.Tables["12pday"].Rows[0]["pday"].ToString();
		if(pday12 != "")
			pday12 += " days";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	sc = " SET DATEFORMAT dmy  SELECT AVG(DATEDIFF(day, i.commit_date, d.trans_date)) AS pday ";
	sc += " FROM invoice i JOIN tran_invoice t ON t.invoice_number = i.invoice_number ";
	sc += " JOIN tran_detail d ON d.id = t.tran_id ";
	sc += " WHERE i.card_id = " + m_custID;
	sc += " AND DATEDIFF(month, i.commit_date, GETDATE()) <= 24 ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "24pday") ==  1)
			pday24 = dst.Tables["24pday"].Rows[0]["pday"].ToString();
		if(pday24 != "")
			pday24 += " days";
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(m_sSite == "admin")
	{
		//Response.Write("<br><table width=75% cellspacing=1 cellpadding=2>");
		Response.Write("<br><table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td><b>Days to pay</b></td>");
		Response.Write("<td><font color=red><b>Last 12 months : " + pday12 + "</b></font></td>");
		Response.Write("<td><font color=orange><b>Last 24 months : " + pday24 + "</b></font>");
//		Response.Write("<td align=right><input type=button value='Transaction List' class=b onclick=window.open('tranlist.aspx?cid=" + m_custID + "')></td>");
		Response.Write("</td></tr></table>");
	}
	return true;
}


</script>

<form runat=server >

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=false
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=97% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	AllowSorting=True
	PageSize=20
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_PageA
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=ID
			 DataNavigateUrlField=ecard
			 DataNavigateUrlFormatString="{0}"
			 SortExpression="ecard"
			 DataTextField=id/>
		<asp:HyperLinkColumn
			 HeaderText=Select
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=name/>
		<asp:HyperLinkColumn
			 HeaderText="Trading Name"
			 DataNavigateUrlField=uri
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=trading_name/>
		<asp:HyperLinkColumn
			 HeaderText=Company
			 DataNavigateUrlField=uritl
			 DataNavigateUrlFormatString="{0}"
			 DataTextField=company/>
		<asp:BoundColumn HeaderText="Sales Person" DataField=sales DataFormatString="{0}"/>		
		<asp:BoundColumn HeaderText="Credit Term" DataField=credit_term DataFormatString="{0}"/>
		<asp:BoundColumn HeaderText="Credit Limit" DataField=credit_limit DataFormatString="{0:c}"/>
		<asp:BoundColumn HeaderText="Total Credit" DataField=total_credit DataFormatString="{0:c}"/>
		<asp:BoundColumn HeaderText=Balance DataField=total_balance DataFormatString="{0:c}"/>
		
	</Columns>
</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>

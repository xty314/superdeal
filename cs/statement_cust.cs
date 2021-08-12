<!-- #include file="stat_fun_cust.cs" -->
<script runat=server>

string m_periodindex = "1";
string m_Tbalance = "0";

//double m_dCardBalance = 0;
double m_dUnPaidTotal = 0;
string m_directory = "0";


void SPage_Load()
{
	if(Request.QueryString["ci"] != null && Request.QueryString["ci"] != "")
		m_custID = Request.QueryString["ci"];

	if(m_sSite != "admin")
		m_custID = Session["card_id"].ToString(); //customer can only view his own statment, of course
	
}



string  PrintCustStatsCardList(string sCid)
{
	if(Request.QueryString["ci"] != null)
		m_custID = Request.QueryString["ci"];
	else
		m_custID = "0";
    m_custID = sCid;
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
	
	StringBuilder sb = new StringBuilder();

//	Response.Write("<table border=1 cellspacing=0>");
	Response.Write("<table width=100%  align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td bgcolor=#7D93B5><font face=tahoma color=white size=2><b>Days:&nbsp;&nbsp;&nbsp;</b></font></td>");
	
	Response.Write("<td" + s_colbgc1 + ">");
	
	Response.Write("&nbsp;<b>Current &nbsp; </b></td>\r\n");
	
	Response.Write("<td" + s_colbgc2 + ">");
	
	//Response.Write(">&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	if(m_scredit_terms_id == "4")
		Response.Write("&nbsp;<b>7 Days &nbsp; </b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		Response.Write("&nbsp;<b>14 Days &nbsp; </b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	Response.Write("&nbsp;<b>14 Days &nbsp; </b></td>\r\n");
	else
		Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	
	Response.Write("<td" + s_colbgc3 + ">");

	//Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	if(m_scredit_terms_id == "4")
		Response.Write("&nbsp;<b>14 Days &nbsp; </b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	else
		Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	
	
	Response.Write("<td" + s_colbgc4 + ">");
	

	//Response.Write("&nbsp;<b>90 Days &nbsp; </b></td>\r\n");
	if(m_scredit_terms_id == "4")
		Response.Write("&nbsp;<b>30 Days &nbsp; </b></td>\r\n");
	else if(m_scredit_terms_id == "5")
		Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	//else if(m_scredit_terms_id == "7")
	//	Response.Write("&nbsp;<b>60 Days &nbsp; </b></td>\r\n");
	else
		Response.Write("&nbsp;<b>90 Days &nbsp; </b></td>\r\n");
	
	
	Response.Write("<td" + s_colbgc5 + ">");

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
			return "";		
//DEBUG("i = ", i);
//DEBUG("dSubBalance = ", dSubBalance[i].ToString());
	}		

	double dRowTotal = dSubBalance[0] + dSubBalance[1] + dSubBalance[2] + dSubBalance[3];
	m_dUnPaidTotal = dRowTotal;
	double dCreditTotal = GetTotalCredit(m_custID);
	double dTotalDue = dRowTotal - dCreditTotal;

	sb.Append("<tr><td bgcolor=#7D93B5><font face=tahoma color=white size=2><b>Sub Balance:&nbsp;</b></font></td>");
	sb.Append("<td align=center" + s_colbgc1 + ">" + dSubBalance[0].ToString("c") + "</td>");
	sb.Append("<td align=center" + s_colbgc2 + ">" + dSubBalance[1].ToString("c") + "</td>");
	sb.Append("<td align=center" + s_colbgc3 + ">" + dSubBalance[2].ToString("c") + "</td>");
	sb.Append("<td align=center" + s_colbgc4 + ">" + dSubBalance[3].ToString("c") + "</td>");
	sb.Append("<td align=right" + s_colbgc5 + ">&nbsp;&nbsp;" + dRowTotal.ToString("c") + "</td>");
	sb.Append("<td align=right" + s_colbgc5 + ">&nbsp;&nbsp;" + dCreditTotal.ToString("c") + "</td>");
	sb.Append("<td align=right" + s_colbgc5 + ">&nbsp;&nbsp;" + dTotalDue.ToString("c") + "</td>");
//	sb.Append("<td align=center" + s_colbgc6 + ">" + dSubBalance[5].ToString("c") + "</td>"); //to be billed
	sb.Append("</tr></table>\r\n");
	return sb.ToString();
}
</script>

<asp:Label id=LFooter runat=server/>

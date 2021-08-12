<script runat=server>

string m_sSalesToday = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;

	if(!GetSalesToday())
		return;

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><h3>SALES</h3></center>");
	Response.Write("<table width=90% align=center border=1 cellpadding=9 cellspacing=1><tr><td>");
	
	Response.Write("<table align=center border=0 cellpadding=9 cellspacing=1><tr>");
	Response.Write("<td><a href=c.aspx><img src=/i/shopsale.jpg><br><b>SALES</b></a></td>");
	Response.Write("<td><a href=q.aspx><img src=/i/quotation.jpg ><br><b>Quotation</b></a></td>");
	Response.Write("<td><a href=order.aspx><img src=/i/orderinvoice.jpg><br><b>Order/Invoice</b></a></td>");
	Response.Write("</tr></table>");

	Response.Write("</td></tr>");
	Response.Write("<tr><td>&nbsp;</td></tr>");

//	Response.Write("<tr><td align=center><font size=+1><b>Today's Sales Summary</b></font></td></tr>");
//	Response.Write("<tr><td>&nbsp;</td></tr>");

//	Response.Write("<tr><td>");
//	Response.Write("<b>Total Sales till " + DateTime.Now.ToString("HH:mm MMM.dd.yyyy") + " : </b>");
//	Response.Write("<font color=red size=+1><b>" + m_sSalesToday + "</b></font>");
//	Response.Write("</td></tr>");

	Response.Write("<tr><td>");

	////////////////////////////////////////////////////////////////////////////////////
	//supplier link table
	Response.Write("<center><h3>Supplier Links</h3>");
	
	Response.Write("<table cellspacing=1 cellpadding=10 border=1 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=\"font-family:Verdana;font-size:8pt;fixed\">");
	Response.Write("<tr>");

	Response.Write("<form NAME=Logon action=http://www.techlink.co.nz/techlink/forms/login.asp method=post target=_blank>");
	Response.Write("<td width=120 align=center>");
	Response.Write("<input type=hidden name=cust_id value=5934admin>");
	Response.Write("<input type=hidden name=PIN value=669881>");
	Response.Write("<input type=submit name=OK value='TechLink'>");
	Response.Write("</td>");	
	Response.Write("</form>");

	Response.Write("<form NAME=frmLogin action=http://shop.renaissance.co.nz/Login.ASP method=post target=_blank>");
	Response.Write("<td width=120 align=center>");
	Response.Write("<input TYPE=HIDDEN NAME=prmMode VALUE=CheckLogin>");
	Response.Write("<input type=hidden name=Password Value=20nhzx>");
	Response.Write("<input type=hidden name=LoginID Value=REN3250>");
	Response.Write("<input name=LoginMode type=hidden value=Validate>");
	Response.Write("<input type=submit name=OK value='Renaissance'>");
	Response.Write("</td>");	
	Response.Write("</form>");

	Response.Write("<form NAME=login action=http://www.bbfnz.co.nz/cair/cair.asp method=post target=_blank>");
	Response.Write("<td width=120 align=center>");
	Response.Write("<input type=hidden name=login value=auto>");
	Response.Write("<input type=hidden name=uid Value=EDEN>");
	Response.Write("<input type=hidden name=pwd Value=EDEN88>");
	Response.Write("<input type=submit name=OK value='BBF Comp.'>");
	Response.Write("</td>");	
	Response.Write("</form>");

	Response.Write("</tr></table>");	
	//end of supplier link table
	////////////////////////////////////////////////////////////////////////////////////

	PrintAdminFooter();
}

bool GetSalesToday()
{
	double dTotal = 320;

	string sc = "SELECT i.invoice_number as Invoice#, i.commit_date as date ";
	sc += "FROM sales s JOIN invoice i ON s.invoice_number=i.invoice_number ";
	m_sSalesToday = dTotal.ToString("c");
	return true;
}
</script>

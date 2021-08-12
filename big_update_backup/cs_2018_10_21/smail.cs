<!-- #include file="resolver.cs" -->
<!-- #include file="statement.cs" -->
<%@ Import Namespace="iTextSharp" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.html" %>
<%@ Import Namespace="iTextSharp.text.xml" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>

<script runat=server>

bool bSortAscend = false;	//list records order by balance in ASC or DESC

DataSet dsc = new DataSet();
string[] m_EachMonth = new string[13];
string m_kw = "";

void SmailPage_Load()
{
		//monthly name
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


	if(Request.QueryString["reset"] == "1")
		Session["smail_already_sent"] = null;

	if(Request.QueryString["dir"] != null && Request.QueryString["dir"] != "")
		m_directory = Request.QueryString["dir"];
	
    if(Request.Form["kw"] != null && Request.Form["kw"] != "")
        m_kw = Request.Form["kw"];

    if(Request.Form["cmd"] != null && Request.Form["cmd"] == " Send Statement ")
	{
		DoMailStatement();
		Response.Write("<center><h5>You have succesfully sent statement notice to customers</h5>");
		Response.Write("<input type=button value='Send Others' "+ Session["button_style"] +" onclick=window.location=('smail.aspx?reset=1')></center>");
	//	return;
	}
    else if(Request.Form["cmd"] == "Send PDF")
	{
		DoPrintStatementPdf(false, true);
        Response.Write("<center><h5>You have succesfully sent statement notice to customers</h5>");
		Response.Write("<input type=button value='Send Others' "+ Session["button_style"] +" onclick=window.location=('smail.aspx?reset=1')></center>");
	//	return;
	}
	else if(Request.Form["cmd"] == " Print Statement ")
	{
		string stylesheet = "<STYLE TYPE='text/css'> P.breakhere {page-break-before: always}</STYLE>"; 
		
		Response.Write(stylesheet);
		Response.Write("<body onload=window.print()>");
		DoPrintStatement(false);
		Response.Write("</body>");
	}
	else if(Request.Form["cmd"] == " Print All Statement ")
	{
		string stylesheet = "<STYLE TYPE='text/css'> P.breakhere {page-break-before: always}</STYLE>"; 
		
		Response.Write(stylesheet);
		Response.Write("<body onload=window.print()>");
		DoPrintStatement(true);
		Response.Write("</body>");
	}
    else if(Request.Form["cmd"] == "Print PDF Statement")
    {
       	string stylesheet = "<STYLE TYPE='text/css'> P.breakhere {page-break-before: always}</STYLE>"; 
		
		Response.Write(stylesheet);
		Response.Write("<body onload=window.print()>");
		DoPrintStatementPdf(false, false);
		Response.Write("</body>");
    }
	else
	{
		PrintAdminMenu();
		if(!GetStatementList())
			return;

		MyDrawTable();
		PrintAdminFooter();
	}

	return;
}

bool GetStatementList()
{
	string sc = " SELECT c.id, c.trading_name, c.name, c.city, ";
 //   sc += " SUM(i.total) AS balance "; //c.balance ";
    sc += " ISNULL((SELECT SUM(total - amount_paid) FROM invoice WHERE card_id = c.id AND paid = 0), 0) ";
    sc += " - ISNULL((SELECT SUM(amount - amount_applied) FROM credit ";
    sc += " WHERE card_id = c.id), 0) as balance ";
	sc += ", c.email, c.credit_limit, e.name AS credit_terms ";
	sc += " FROM invoice i LEFT OUTER JOIN card c ON c.id = i.card_id ";
	//sc += " FROM card c JOIN enum e ON e.id = c.credit_term ";
	sc += " JOIN enum e ON e.id = c.credit_term ";
	sc += " WHERE e.class = 'credit_terms' ";
	//sc += " AND c.balance > 0 ";
	sc += " AND ROUND(i.total,2) - ROUND(i.amount_paid,2) <> 0 and paid = 0 ";
    if(m_kw != "")
    {
        sc += " AND (UPPER(c.name) LIKE UPPER(N'%" + EncodeQuote(m_kw) + "%')";
        sc += " OR UPPER(c.short_name) LIKE UPPER(N'%" + EncodeQuote(m_kw) + "%')";
        sc += " OR UPPER(c.trading_name) LIKE UPPER(N'%" + EncodeQuote(m_kw) + "%') )";
    }
	if(Session["smail_already_sent"] != null)
		sc += " AND c.id NOT IN(" + Session["smail_already_sent"].ToString() + ") ";
	if((Request.QueryString["type"] != null && Request.QueryString["type"] != "") && Request.QueryString["type"] != "all")
		sc += " AND c.type = "+ Request.QueryString["type"].ToString() + "";
	if(Request.QueryString["type"] == "all")
		sc += "";
	if(m_directory != "0")
		sc += " AND c.directory = "+ m_directory;
	if((Request.Form["type"] != null && Request.Form["type"] != "") && Request.Form["type"] != "all")
		sc += " AND c.type = "+ Request.Form["type"].ToString() + "";	
	if(Request.Form["directory"]!= null && Request.Form["directory"] != "" && Request.Form["directory"].ToString() != "0")
		sc += " AND c.directory = "+ Request.Form["directory"].ToString();
	sc += " GROUP BY c.id, c.trading_name, c.name, c.city, c.email, c.credit_limit, e.name ";
	if(bSortAscend)
		sc += " ORDER BY SUM(i.total) ";
//		sc += " ORDER BY c.balance";
	else
		sc += " ORDER BY SUM(i.total) DESC ";
//		sc += " ORDER BY c.balance DESC";
//DEBUG("cs =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dsc, "card");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void MyDrawTable()
{
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);
	string s_type = "";
	if(Request.QueryString["type"] != null && Request.QueryString["type"] != "")
		s_type = Request.QueryString["type"].ToString();
	int rows = 0;
	if(dsc.Tables["card"] != null)
		rows = dsc.Tables["card"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.PageSize = 20;
	m_cPI.URI = "?type="+ s_type +"&r=" + DateTime.Now.ToOADate();
	if(m_directory != null && m_directory != "")
		m_cPI.URI += "&dir="+ m_directory;
	if(Request.QueryString["sup_id"] != null && Request.QueryString["sup_id"] != "")
		m_cPI.URI += "&sup_id="+ Request.QueryString["sup_id"];
	int i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	//Header
	Response.Write("<br><h3><center>Mail Statement</center></h3>");
	
	Response.Write("<form name=f action=? method=post>");
	Response.Write("<input type=hidden name=type value="+ s_type +">");	
	Response.Write("<input type=hidden name=sup_id value="+ Request.QueryString["sup_id"] +">");	
	Response.Write("<table width=95% align=center valign=top cellspacing=1 cellpadding=0 border=1 bordercolor=#666696 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=8>");
	Response.Write(sPageIndex);
	Response.Write("&nbsp;<a title='click to list only suppliers' href='smail.aspx?reset=1&type=3' class=o>Suppliers</a> | ");
	Response.Write("<a title='click to list only dealers' href='smail.aspx?reset=1&type=2' class=o>Dealers</a> | ");
	Response.Write("<a title='click to list only customers' href='smail.aspx?reset=1&type=1' class=o>Customers</a> | ");
	Response.Write("<a title='click to list all' href='smail.aspx?reset=1&type=all' class=o>All</a> | ");
	Response.Write(" &nbsp;&nbsp;Directory/Group:");
	Response.Write("<select name=directory onchange=\"window.location=('" + Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +"");
	Response.Write("&type="+ Request.QueryString["type"] +"&p="+ m_cPI.CurrentPage +"&spb="+ m_cPI.StartPageButton + "&sup_id="+ Request.QueryString["sup_id"] +"&dir='+this.options[this.selectedIndex].value)\">");
	Response.Write("<option value=0>All</option>");
	Response.Write(GetEnumOptions("card_dir", m_directory));
	Response.Write("</select>");
    Response.Write(" &nbsp;&nbsp;Search:&nbsp;");
    Response.Write("<input type=text size=15 name='kw' >");
    Response.Write("&nbsp;<input type=submit value=' GO ' "+ Session["button_style"] +" >");
    //Response.Write(" onClick=window.location=('smail.aspx?reset=1')>");

	Response.Write("</td>");//<td colspan><input type=text name=search value=''><input type=submit name=cmd value='Search' "+ Session["button_style"] +">");
	Response.Write("</td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	
	Response.Write("<th>Company</th>");
	Response.Write("<th>Name</th>");
	Response.Write("<th>E-mail Address</th>");
//	Response.Write("<th>City</th>");
	Response.Write("<th>Credit Term</th>");
	Response.Write("<th>Credit Limit</th>");
	Response.Write("<th>Balance</th>");
	Response.Write("<th>&nbsp;&nbsp;&nbsp;</th></tr>");

	bool bAlterColor = false;
	int iRowPos = 0;
	for(; i < rows && i < end; i++)
	{
		DataRow dr = dsc.Tables["card"].Rows[i];
		//string id = dr["id"].ToString();
		string s_tradeName = dr["trading_name"].ToString();
		string s_name = dr["name"].ToString();
		string s_email = dr["email"].ToString();
//		string s_city = dr["city"].ToString();
		string s_balance = dr["balance"].ToString();
		string s_credit_limit = dr["credit_limit"].ToString();
		string s_credit_terms = dr["credit_terms"].ToString();
		
		CResolver rs = new CResolver();
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td>" + s_tradeName + "</td>");

		Response.Write("<td>" + s_name + "</td>");
		Response.Write("<td>" + s_email + "</td>");
//		Response.Write("<td>" + s_city + "</td>");
		Response.Write("<td>" + s_credit_terms + "</td>");
		Response.Write("<td align=right>" + double.Parse(s_credit_limit).ToString("c") + "</td>");
		Response.Write("<td align=right>" + double.Parse(s_balance).ToString("c")+ "</td>\r\n");
		string sTicked = "sTicked" + iRowPos.ToString();
		Response.Write("<td align=center><input type=checkbox name="+sTicked+" >");
		string sCardID = "sCardID" + iRowPos.ToString();
		Response.Write("<input type=hidden name="+sCardID+" value='" + dr["id"].ToString() + "'></td></tr>\r\n");
		iRowPos = iRowPos + 1;;
	}
	Response.Write("<tr>");
	Response.Write("<th colspan=6 align=right>CHECK ALL</td><td align=center><input type=checkbox name=allbox onclick='CheckAll();'>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=7 align=center> Select Period : ");
string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
//	string regYear = DateTime.Parse(Session[m_sCompanyName +"registered_date"].ToString()).ToString("yyyy");
	string regYear = "2006";
	//DEBUG("sdfre =", regYear);
	Response.Write("<select name='pickMonth'><option value=all>All</option>");
//	Response.Write("<select name='pickPeriod'><option value=all>All</option>");
	Response.Write("<option value=0>Current</option>");
	Response.Write("<option value=1>Last Month</option>");
	Response.Write("<option value=2>2 Months Ago</option>");
	Response.Write("<option value=3>Over 3 Months</option>");
    Response.Write("<option value=btm>Before This Month</option>");
/*		for(int m=1; m<13; m++)
		{
			string txtMonth = "";
			txtMonth = m_EachMonth[m-1];
			
			Response.Write("<option value="+m+"");
			if(int.Parse(s_month) == m)
				Response.Write(" selected ");
			Response.Write(">"+txtMonth+"</option>");
			//Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
			
		}
		*/
		Response.Write("</select>");
/*		Response.Write("<select name='pickYear'>");
		for(int y=int.Parse(regYear); y<int.Parse(s_year)+1; y++)
		{
			if(int.Parse(s_year) == y)
				Response.Write("<option value="+y+" selected>"+y+"</option>");
			else
				Response.Write("<option value="+y+">"+y+"</option>");
		}
		Response.Write("</select>");
*/
	Response.Write("</td></tr>");	
	Response.Write("<center>");
	Response.Write("<tr><td colspan=7 align=center>");
	Response.Write("<input type=submit " + Session["button_style"]);
	Response.Write(" name=cmd value=' Send Statement ' onclick=\"if(!confirm('Email Statement...')){return false;}\">");
	Response.Write("<input type=submit " + Session["button_style"]);
	Response.Write(" name=cmd value=' Print Statement '>");
	Response.Write("<input type=submit " + Session["button_style"]);
	Response.Write(" name=cmd value=' Print All Statement ' Onclick=\"return confirm('This will Print All Statements in this Category/Directory OR Card Type');\">");
	//Response.Write("</center>");
    Response.Write("<input type=submit " + Session["button_style"]);
	Response.Write(" name=cmd value='Print PDF Statement'>");
    Response.Write("<input type=submit " + Session["button_style"]);
	Response.Write(" name=cmd value='Send PDF' onclick=\"if(!confirm('Email PDF Statement...')){return false;}\">");
	Response.Write("</td></tr>");
Response.Write("</table><input type=hidden name=TotalRows value='" + iRowPos.ToString() + "'><br>");
	Response.Write("</from>");
	PrintJavaFunctions();
}

void PrintJavaFunctions()
{
	Response.Write("<script language=JavaScript");
	Response.Write(">");
	
	const string s = @"
	function CheckAll()
	{
		for (var i=0;i<document.f.elements.length;i++) 
		{
			var e = document.f.elements[i];
			if((e.name != 'check') && (e.type=='checkbox'))
				e.checked = document.f.allbox.checked;
		}
	}
	";
	Response.Write(s);

	Response.Write("</script");
	Response.Write(">");
}

void DoPrintStatement(bool bPrintAllWithType)
{	
	m_timeOpt = "4";
	string sCardID = "";
	string s_mailbody = "";
	int oldid = 1;
	
//	s_mailbody = "<STYLE TYPE='text/css'> P.breakhere  {page-break-before: always}</STYLE>";
//	s_mailbody = "<STYLE TYPE='text/css'> H2  {page-break-before: always}</STYLE>";
	int nCt = 0;
	bool bFirstPrint = false;
	string PrintPageBreak = "";
	if(bPrintAllWithType)
	{
		GetStatementList();
		for(int i=0; i<dsc.Tables["card"].Rows.Count; i++)
		{
			nCt++;
			sCardID = dsc.Tables["card"].Rows[i]["id"].ToString();
			m_custID = sCardID;
			GetSelectedCust(sCardID);
			GetInvRecords(sCardID);
						
			s_mailbody = PrintStatmentDetails();
			PrintPageBreak = "<P CLASS='breakhere'>";
			
			if(bFirstPrint)
				Response.Write(PrintPageBreak);	
			Response.Write(s_mailbody);
			bFirstPrint = true;
		}
	}
	else
	{
		for(int i=0; i < MyIntParse(Request.Form["TotalRows"].ToString()); i++)
		{
			string sRow = "sTicked" + i.ToString();
			string sRowCardID = "sCardID" + i.ToString();
		
			if(Request.Form[sRow] == "on")
			{ 
				nCt++;
				sCardID = Request.Form[sRowCardID];

				m_custID = sCardID;
				GetSelectedCust(sCardID);
				GetInvRecords(sCardID);
							
				s_mailbody = PrintStatmentDetails();
				PrintPageBreak = "<P CLASS='breakhere'>";
				
				if(bFirstPrint)
					Response.Write(PrintPageBreak);	
				Response.Write(s_mailbody);
				bFirstPrint = true;
			}
			
		
		}
	}
	return;

}

void DoMailStatement()
{
	m_timeOpt = "4";
	string sCardID = "";
	string s_mailbody = "";
	
	for(int i=0; i < MyIntParse(Request.Form["TotalRows"].ToString()); i++)
	{
		MailMessage msgMail = new MailMessage();

		string sRow = "sTicked" + i.ToString();
		string sRowCardID = "sCardID" + i.ToString();
	
		if(Request.Form[sRow] == "on")
		{ 
			sCardID = Request.Form[sRowCardID];

			m_custID = sCardID;
			GetSelectedCust(sCardID);
			GetInvRecords(sCardID);
            string mail_to = dst.Tables["cust_gen"].Rows[0]["ap_email"].ToString();
			if(mail_to == "")
				mail_to = dst.Tables["cust_gen"].Rows[0]["email"].ToString();

			s_mailbody = PrintStatmentDetails();
//DEBUG(" smailbody = ", s_mailbody);
			msgMail.From = m_sSalesEmail; //Session["email"].ToString();
			
			//sent to account payable email if exists
			
			msgMail.BodyFormat = MailFormat.Html;
			msgMail.To = mail_to;
//DEBUG("send", mail_to);
			msgMail.Subject = "Statement Notice.";
			msgMail.Body = s_mailbody;
			SmtpMail.Send(msgMail);
			
//Response.Write(s_mailbody);
		}
		
		//remember
		string s = "";
		if(Session["smail_already_sent"] != null)
			s = Session["smail_already_sent"].ToString() + ", " + Request.Form[sRowCardID];
		else 
			s = Request.Form[sRowCardID];
		Session["smail_already_sent"] = s;
	}
	return;
}
void DoPrintStatementPdf(bool bPrintAllWithType, bool bEmailPdf)
{	
	m_timeOpt = "4";
	string sCardID = "";
	string s_mailbody = "";
	int oldid = 1;
	int pagepdf = 1;
	int nCt = 0;
	bool bFirstPrint = false;
	string PrintPageBreak = "";
	Document document = new Document(PageSize.A4, 20,20,1, 1);
	string strHTMLpath = Server.MapPath("MyHTML.html");
	string strPDFpath = Server.MapPath("Statement.pdf");
    string mail_to = "";
    if (File.Exists(strPDFpath))
		{
			File.Delete(strPDFpath);
		}
//	try
//	{
		StringWriter sw = new StringWriter();
		sw.WriteLine(Environment.NewLine);
		sw.WriteLine(Environment.NewLine);
		sw.WriteLine(Environment.NewLine);
		sw.WriteLine(Environment.NewLine);
		HtmlTextWriter htw = new HtmlTextWriter(sw);
	    
        string txt = ReadSitePage("statement_pdf_logo");
		
        string statlisttitle = @"<br><table width=100% align=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white  >
		<tr bgcolor=#4080BF >
			<td width=15% align=center><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=center >Date</font></td>
			<td width=18% align=center><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=center >Invoice no.</font></td>
			<td width=17% align=center><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=center >Order no.</font></td>
			<td width=15% align=right><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >Charges</font></td>
			<td width=15% align=right><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=center >Payment</font></td>
			<td width=20% align=right><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >Total &nbsp;</font></td>
		</tr>";

		StreamWriter strWriter = new StreamWriter(strHTMLpath, false, Encoding.UTF8);
		strWriter.Write( htw.InnerWriter.ToString());
		
		if(bPrintAllWithType)
		{	
			GetStatementList();
			for(int i=0; i<dsc.Tables["card"].Rows.Count; i++)
			{
				nCt++;
				sCardID = dsc.Tables["card"].Rows[i]["id"].ToString();
				m_custID = sCardID;
				GetSelectedCust(sCardID);
				GetInvRecords(sCardID);
							
				s_mailbody = PrintStatmentDetails();
				PrintPageBreak = "<P CLASS='breakhere'>";
//				DEBUG("11111", "11111111111111111111111111111111111111");
				if(bFirstPrint)
				Response.Write(PrintPageBreak);	
				Response.Write(s_mailbody);
				//strWriter.Write(PrintPageBreak);
				//strWriter.Write(s_mailbody);
				bFirstPrint = true;
			}
		}
		else
		{
			for(int i=0; i < MyIntParse(Request.Form["TotalRows"].ToString()); i++)
			{
				string sRow = "sTicked" + i.ToString();
				string sRowCardID = "sCardID" + i.ToString();
			
				if(Request.Form[sRow] == "on")
				{ 
					nCt++;
					sCardID = Request.Form[sRowCardID];
	
					m_custID = sCardID;
					GetSelectedCust(sCardID);
					GetInvRecords(sCardID);

                    mail_to = dst.Tables["cust_gen"].Rows[0]["ap_email"].ToString();
			            if(mail_to == "")
				            mail_to = dst.Tables["cust_gen"].Rows[0]["email"].ToString();
					string custAdd = @"
						<table width=100% cellpadding=0 cellspacing=0 >
					  <tr border=1 border-color=#000000>
						<td align=left ><font face='Times New Roman, Times, serif' color=#000000 size=9px align=left >
							&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;@@compname</font>
						<font face='Times New Roman, Times, serif' color=#000000 size=8px align=left >
						<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;@@pobox 
						<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;@@suburb 
						<br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;@@city</font><br> 
						</td></tr></table>";
								
								
				    
					custAdd = custAdd.Replace("@@compname", GetSelectedCustByID(m_custID, "trading_name"));
					custAdd = custAdd.Replace("@@pobox", GetSelectedCustByID(m_custID, "postal1"));
					custAdd = custAdd.Replace("@@suburb", GetSelectedCustByID(m_custID, "postal2"));
					custAdd = custAdd.Replace("@@city",  GetSelectedCustByID(m_custID, "postal3"));	
					
					//strWriter.Write(GetSelectedCustByID(m_custID, "trading_name"));
					
				    string statHeader = ReadSitePage("statement_pdf_header");
  					double totalPage = double.Parse(dst.Tables["invoice_rec"].Rows.Count.ToString()) / 12;
					if(totalPage >= 1.5)
						totalPage = Math.Round(totalPage, 0);
			        else
						totalPage = 1;	
	   				statHeader = statHeader.Replace("@@Page", pagepdf.ToString() +" of "+ Math.Round(totalPage,0) );
					statHeader = statHeader.Replace("@@date",DateTime.Now.ToString("dd-MMM-yyyy"));
					
					//s_mailbody = PrintStatmentDetails();
					//PrintPageBreak = "<P CLASS='breakhere'>";
					
					//if(bFirstPrint)
						//Response.Write(PrintPageBreak);	
					//Response.Write(s_mailbody);
					
					//strWriter.Write(PrintPageBreak);
					strWriter.Write(txt);
					strWriter.Write(statHeader);
					strWriter.Write(custAdd);
					strWriter.Write(statlisttitle);
					double totalDue = 0;
					
					string statFooter = ReadSitePage("statement_pdf_footer");
                    statFooter = statFooter.Replace("@@pspdf3",  PrintStatmentDetailsPDF("3"));
                    statFooter = statFooter.Replace("@@pspdf2",  PrintStatmentDetailsPDF("2"));
                    statFooter = statFooter.Replace("@@pspdf1",  PrintStatmentDetailsPDF("1"));
                    statFooter = statFooter.Replace("@@pspdf0",  PrintStatmentDetailsPDF("0"));
                    statFooter = statFooter.Replace("@@pspdf_total",  PrintStatmentDetailsPDF("total_due"));
                    statFooter = statFooter.Replace("@@custNum",  m_custID);
                    statFooter = statFooter.Replace("@@custinfo",  GetSelectedCustByID(m_custID, "trading_name"));
					statFooter = statFooter.Replace("@@amountdue",  totalDue.ToString("c"));
					statFooter = statFooter.Replace("@@date",  DateTime.Now.ToString("dd-MMM-yyyy"));
					 
					for(int e = 0; e < dst.Tables["invoice_rec"].Rows.Count; e++)
					{
						//PrintStatmentDetails();
						DataRow dr = dst.Tables["invoice_rec"].Rows[e];
					    string item = @"<tr><td align=center><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >";
						item +=  DateTime.Parse(dr["commit_date"].ToString()).ToString("dd/MM/yy");
						item += @"</font></td>";
						string inv_number = dr["invoice_number"].ToString();
						if(inv_number == "0")
							inv_number = "";
						 item += @"<td align=center><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >";
						 item += inv_number;
						 item += @"</font></td>";
						 if(dr["cust_ponumber"].ToString() == "")
						 	item += "<td align=center>&nbsp;</td>";
						 else
						   item += "<td align=center><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >" + dr["cust_ponumber"].ToString() + "</font></td>";
						   item += "<td align=right><font face='Times New Roman, Times, serif' color=#000000 size=10px align=right>" + double.Parse(dr["total"].ToString()).ToString("c")  + "</font></td>";
						 if(dr["amount_paid"].ToString() == "0")
						    item += "<td align=right>&nbsp;</td>";
						 else
						 	item += "<td align=right><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >" + double.Parse(dr["amount_paid"].ToString()).ToString("c")  + "</font></td>";
						item += "<td align=right><font face='Times New Roman, Times, serif' color=#000000 size=10px align=right >" + double.Parse(dr["cur_bal"].ToString()).ToString("c") + "</font></td>";
						item += "</tr>";
						totalDue += double.Parse(dr["cur_bal"].ToString());
							strWriter.Write(item);
							
						if(e==14)
						{
							strWriter.Write("</table>");
							statFooter = statFooter.Replace("@@amountdue",  totalDue.ToString("c"));
							statFooter = statFooter.Replace("@@date",  DateTime.Now.ToString("dd-MM-yyyy"));
							//strWriter.Write("<table ><tr bgcolor=#4080BF ><td align=right><b><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >STATEMENT TOTAL:&nbsp;&nbsp;</font>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >"+totalDue.ToString("c")+"</FONT></td></tr></table>");
							//strWriter.Write(statFooter);
							
							strWriter.Write("<br><br><table cellpadding=0 cellspacing=0 border=0 width=100% ><tr><td align=left valign=middle >");
							strWriter.Write("<font face='Arial Black, Gadget, sans-serif' color=#000000 size=25px>STATEMENT</font></td><td>&nbsp;</td>");
	      				    strWriter.Write("<td align=right valign=bottom ><table cellpadding=0 cellspacing=0 border=1 border-color=#000000>");
							strWriter.Write("<tr><td bgcolor=#4080BF  height=10   valign=middle height=10px ><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >Page:</font>&nbsp;</td>");
							strWriter.Write("<td align=left><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >2 of "+totalPage+"</font></td>");
							strWriter.Write("<tr><td bgcolor=#4080BF  size=220 ><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >Statement Date:</font>&nbsp;</td>");
					        strWriter.Write("<td align=left><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >");
							strWriter.Write(System.DateTime.Now.ToString("dd-MMM-yyyy"));
							strWriter.Write("</font></td><tr></table></td></tr></table>");
							strWriter.Write(custAdd);
							strWriter.Write(statlisttitle);
							
							
						}
							
					}
					
					int sRows = dst.Tables["invoice_rec"].Rows.Count;
					int rest = 15 - sRows;
					if(rest > 0)
					{
						for(int t = 0; t < rest ; t++)
						{
							strWriter.Write("<tr><td colspan=6><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >&nbsp; </font></td></tr>");
						}
					}
					if(rest < 0)
					{
						int eRows = 20 + rest;
						for(int t = 0; t < eRows ; t++)
						{
						 strWriter.Write("<tr><td colspan=6><font face='Times New Roman, Times, serif' color=#000000 size=10px align=center >&nbsp; </font></td></tr>");
						} 
					}
					
					
					strWriter.Write("</table>");
					
					if(rest != 0)
					{
					    strWriter.Write("<table ><tr bgcolor=#4080BF ><td align=right><b><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >STATEMENT TOTAL:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b><font face='Times New Roman, Times, serif' color=#FFFFFF size=10px align=right >"+totalDue.ToString("c")+"</font></td></tr></table>");
						statFooter = statFooter.Replace("@@date",  DateTime.Now.ToString("dd-MM-yyyy"));
					
					strWriter.Write(statFooter);
					}
					//strWriter.Write(s_mailbody);
					bFirstPrint = true;
				
				
			      /*  if(bEmailPdf)
                    {
                        
                        strWriter.Write("");
		                strWriter.Close();
		                strWriter.Dispose();
		                iTextSharp.text.html.simpleparser.
		                StyleSheet mstyles = new iTextSharp.text.html.simpleparser.StyleSheet();
 
		                PdfWriter.GetInstance(document, new FileStream(strPDFpath, FileMode.Create));
		                document.Add(new Header(iTextSharp.text.html.Markup.HTML_ATTR_STYLESHEET, "Style.css"));
		                document.Open();
		                ArrayList mobjects;
	 
		                mobjects = iTextSharp.text.html.simpleparser.HTMLWorker.ParseToList(new StreamReader(strHTMLpath, Encoding.Default), mstyles);
		                for (int k = 0; k < mobjects.Count; k++)
		                {
			                document.Add((IElement)mobjects[k]);
			
		                }
                        document.Close();
                        Response.WriteFile(strPDFpath);
                        
                        MailMessage msgMail = new MailMessage();
                        msgMail.From = m_sSalesEmail; //Session["email"].ToString();
//DEBUG("m", mail_to);
			            //msgMail.BodyFormat = MailFormat.Html;
			            msgMail.To = mail_to;
			            msgMail.Subject = "ClipMan Statement Notice.";
			            msgMail.Body = "Please see the Attachment";
                        MailAttachment attachment = new MailAttachment(strPDFpath);
                        msgMail.Attachments.Add( attachment );
			            SmtpMail.Send(msgMail);
                        if (File.Exists(strPDFpath))
		                {
			                File.Delete(strPDFpath);
		                }
                        //strWriter = new StreamWriter(strHTMLpath, false, Encoding.UTF8);
		                //strWriter.Write( htw.InnerWriter.ToString());
                        
                    }*/
                }
			}
		}
//        if(bEmailPdf)
//           return;
       
		strWriter.Write("");
		strWriter.Close();
		strWriter.Dispose();
		iTextSharp.text.html.simpleparser.
		StyleSheet styles = new iTextSharp.text.html.simpleparser.StyleSheet();
 
		PdfWriter.GetInstance(document, new FileStream(strPDFpath, FileMode.Create));
		document.Add(new Header(iTextSharp.text.html.Markup.HTML_ATTR_STYLESHEET, "Style.css"));
		document.Open();
		ArrayList objects;
	 
		objects = iTextSharp.text.html.simpleparser.HTMLWorker.ParseToList(new StreamReader(strHTMLpath, Encoding.Default), styles);
		for (int k = 0; k < objects.Count; k++)
		{
			//DEBUG("ITEM ", objects[k].ToString());
			document.Add((IElement)objects[k]);
			//DEBUG("ITEM ", objects[k].ToString());
		}
		
//	}
//	catch (Exception ex)
//	{
		//throw ex;
//	}
//	finally
//	{
	   
        document.Close();
        if(bEmailPdf)
        {
            Response.WriteFile(strPDFpath);
            MailMessage msgMail = new MailMessage();
            msgMail.From = m_sSalesEmail; //Session["email"].ToString();

			msgMail.BodyFormat = MailFormat.Html;
			msgMail.To = mail_to;
			msgMail.Subject = "ClipMan Statement Notice.";
			msgMail.Body = "Please see the Attachment";
            MailAttachment attachment = new MailAttachment(strPDFpath);
            msgMail.Attachments.Add( attachment );
			SmtpMail.Send(msgMail);
//DEBUG("m", mail_to);
           return;
        }
        else
        {
		    Response.Write(strPDFpath);
		    Response.ClearContent();
		    Response.ClearHeaders();
		    Response.AddHeader("Content-Disposition", "attachment; filename=" + strPDFpath);
		    Response.ContentType = "application/octet-stream";
		    Response.WriteFile(strPDFpath);
		    Response.Flush();
		    Response.Close();
        }
		if (File.Exists(strPDFpath))
		{
			File.Delete(strPDFpath);
		}
 
//	}
	
	
	
	return;

}


</script>



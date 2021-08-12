<!-- #include file="page_index.cs" -->
<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_branchID = "1";
string m_dateSql = "";
string m_code = "";
string m_datePeriod = "";
string m_datechecked0 = "";
string m_datechecked1 = "";
string m_datechecked2 = "";
string m_datechecked3 = "";
string m_datechecked4 = "";
string Norecord = "";
string m_accountID = "";
string m_action = "";
string m_tranID = "";
string m_date = "";
string m_ref = "";
double m_dTotal = 0;
string[] m_EachMonth = new string[16];
string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";
string m_syFrom = "";
string m_syTo = "";
int m_nPeriod = 0;
bool m_bPrint = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	m_action = Request.QueryString["t"];

    
	if(Request.QueryString["id"] != null)
	{
		m_tranID = Request.QueryString["id"];
		if(m_action == "p")
			m_bPrint = true;
		PrintBankingSlip();
		return;
	}
	
	if(Request.QueryString["roll"] == "done")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<center><h4>"+ Lang("Roll Back Done") +"!");
		Response.Write("<br><br><a title='"+ Lang("go to banking") +"' href="+ Request.ServerVariables["URL"] +"?r="+ DateTime.Now.ToOADate() +" class=o>Back to Banking</a>");
		PrintAdminFooter();
		return;
	}
	if(Request.QueryString["rid"] != null)
	{
		if(DoRollBackDeposit())
		{
			Response.Write("<br><center><h4>"+ Lang("Please Wait for 1 Second to finish the Roll Back Transaction") +"...</center>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL="+ Request.ServerVariables["URL"] +"?roll=done\">");
			return;
		}
		
	}

	if(Request.Form["cmd"] == Lang("Deposit"))
	{
		if(!DoDeposit())
		{
			return;
		}
		return;
	}
	
	if(!IsPostBack)
	{
		string startDate = "";
		if(Session["banking_start_date"] == null || Session["banking_start_date"] == "")
		{
			
//			Calendar1.VisibleDate = DateTime.Now;
			startDate = DateTime.Now.ToString();
		
			Session["banking_start_date"] = startDate;

			DateTime dstart = DateTime.Parse(startDate);
			Session["banking_day"] = dstart.Day;
			Session["banking_month"] = dstart.Month;
			Session["banking_year"] = dstart.Year;
		}
		else
			startDate = Session["banking_start_date"].ToString();

		if(!DoSearch())
			return;
		BindGrid();
	}	

    if(Request.QueryString["print"] == "1") {
		BindGrid();
	}

	bool bPrint = false;
	if(Request.Form["cmd"] == Lang("Deposit"))
		bPrint = true;

	if(m_action != "p")
		LFooter.Text = m_sAdminFooter;
//	PrintAdminFooter();


}

Boolean DoSearch()
{
	string b = Request.Form["branch"];
	string startDate = DateTime.Now.ToString();
	if(Session["banking_start_date"] != null)
	startDate = Session["banking_start_date"].ToString();
	string endDate = DateTime.Parse(startDate).AddDays(1).ToString("dd-MM-yyyy");
	startDate = DateTime.Parse(startDate).ToString("dd-MM-yyyy");

	//////////////////////////////////////// Select Date ////////////////

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



	if(Request.Form["period"] != null)
	m_nPeriod = MyIntParse(Request.Form["period"]);
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


	string trandsSQL = "";
	string invSQL = "";
	switch(m_nPeriod)
	{
	case 0:
		//m_dateSql = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		m_dateSql = " AND DATEDIFF(day, d.trans_date, GETDATE()) = 0 ";
		trandsSQL = " AND DATEDIFF(day, d.trans_date, GETDATE()) = 0 ";
		invSQL = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		m_dateSql = " AND DATEDIFF(day, d.trans_date, GETDATE()) = 1 ";
		trandsSQL = " AND DATEDIFF(day, d.trans_date, GETDATE()) = 1 ";
		invSQL = " AND DATEDIFF(day, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		m_dateSql = " AND DATEDIFF(week, d.trans_date, GETDATE()) = 0 ";
		trandsSQL = " AND DATEDIFF(week, d.trans_date, GETDATE()) = 0 ";
		invSQL = " AND DATEDIFF(week, i.commit_date, GETDATE()) = 0 ";
		break;
	case 3:
		m_dateSql = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		trandsSQL = " AND DATEDIFF(month, d.trans_date, GETDATE()) = 0 ";
		invSQL = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 4:
		m_dateSql = " AND d.trans_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		trandsSQL = " AND d.trans_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		invSQL = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		break;
	default:
		break;
	}
////////////////////////////////////////////////////// Select Print End ////////////////////////////////////
    string v_print = Request.QueryString["print"];


	ds.Clear();

	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT t.id, d.trans_date, c.trading_name, d.payment_method, d.payment_ref, d.note, t.amount ";
	sc += ", c.id AS card_id, c.company as company, c.name as name, d.invoice_number, d.card_id as customer_id, br.name  AS bName ";
	sc += " FROM tran_detail d JOIN trans t ON t.id=d.id LEFT OUTER JOIN card c ON c.id=d.staff_id JOIN branch br on br.id=c.our_branch";
	sc += " WHERE t.dest = 1116 AND t.banked = 0 ";
    
	
    	if (b == null  )
	{	sc += " AND br.id ='"+	Session["branch_id"] +"' ";
	
	}
       else if (b != null && b !="0"){
		sc +=" AND br.id="+b;
	  } 
	

	//unbanked trans in undeposite
	if(Request.Form["period"] != "" && Request.Form["period"] != null){
	sc +=  m_dateSql;
	}
	sc += " ORDER BY d.payment_method ";
//DEBUG("bankging2 =", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "banking");
        
		if(rows ==0){
		Norecord = " Sorry, there is no record in that branch ";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
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

/////////////////////////////////////////////////////////////////
void BindGrid()
{

	string branch_id = Request.Form["branch"];
	string s_period = Request.Form["period"];
	string datepicker1_day = Request.Form["Datepicker1_day"];
	string datepicker1_month = Request.Form["Datepicker1_month"];
	string datepicker1_year = Request.Form["Datepicker1_year"];
	string datepicker2_day = Request.Form["Datepicker2_day"];
	string datepicker2_month = Request.Form["Datepicker2_month"];
	string datepicker2_year = Request.Form["Datepicker2_year"];
	string v_print = Request.QueryString["print"];
	StringBuilder sb = new StringBuilder();
	string cols ="";
	if(v_print =="1"){
	 cols = "6";
	}else{
	  cols ="7";
	}
	if(v_print !="1")
{
	PrintAdminHeader();
	PrintAdminMenu();
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();

		Session["branch_id"] = m_branchID;
	}
	

	
    string r_date = "";
	if(Request.Form["period"] != "" && Request.Form["period"] != null )
	 r_date = " Date: " + m_datePeriod ;
		//call checkbox java function
	PrintJavaFunctions();
    ///////
	if(Request.Form["period"] != null){
	  string m_datevalue = Request.Form["period"];
      if(m_datevalue == "1")
	  m_datechecked1 = "checked";
	  if(m_datevalue == "2")
	  m_datechecked2 = "checked";
	  if(m_datevalue == "3")
	  m_datechecked3 = "checked";
	  if(m_datevalue == "4")
	  m_datechecked4 = "checked";
	  if(m_datevalue == "0")
	  m_datechecked0 = "checked";
	}
/*	else {
	m_datechecked4 = "checked";
	}
*/
	
    Response.Write ("<div align=center><table width=100%>");
	Response.Write("<tr><td height=20>&nbsp;</td>");
	Response.Write("<tr><td colspan=" + cols + " align=center><font size=+1><b>"+ Lang("Bank Deposit") +"</b></font></td></tr>");
	Response.Write("<tr><td height=20>&nbsp;</td>");
	Response.Write("<tr><td align=center width=100% >");
	Response.Write("<form name=b action='banking.aspx?r="+ DateTime.Now.ToOADate() +"' method=post>");
	if(Session["branch_support"] != null)
	{
	
	Response.Write("<b>Branch:</b>");
	PrintBranchNameOptions(m_branchID, "", true);
	Response.Write("<input type=submit  value=GO  >");

	}
	Response.Write("</td></tr>");
	Response.Write("<tr><td align=center ><input type=radio name=period value=0 "+m_datechecked0+">Today");
	Response.Write("<input type=radio name=period value=1 "+m_datechecked1+">Yestoday");
	Response.Write("<input type=radio name=period value=2 "+m_datechecked2+">This Week");
	Response.Write("<input type=radio name=period value=3 "+m_datechecked3+">This Month");
	Response.Write("<input type=radio name=period value=4 "+m_datechecked4+">Select Date</td></tr>");
  

	/////

int e = 1;
	datePicker(); //call date picker function
	Response.Write("<tr><td colspan=2 align=center><b> &nbsp; From Date </b>");
	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<select name='Datepicker1_day' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");

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
	Response.Write("<select name='Datepicker1_year' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker1',1);\" style=''>");
	for(int y=2006; y<int.Parse(s_year)+1; y++)
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

    Response.Write ("<b>&nbsp;To:</b>");
		Response.Write("<select name='Datepicker2_day' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");

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
	Response.Write("<select name='Datepicker2_year' onChange=\"document.f.period[4].checked=true;tg_mm_setdays('document.forms[0].Datepicker2',1);\" style=''>");
	for(int y=2006; y<int.Parse(s_year)+1; y++)
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
	///
	Response.Write("</form>");
	Response.Write("<tr><td  align=center height=40 valign=middle><b>"+r_date+"</d></td></tr>");
    Response.Write("</table>");
	Response.Write("</div>");
    /////


} // Print view
    sb.Append("<link rel='stylesheet' href='../print.css' type='text/css' media='print' >"); 
	sb.Append("<table align=center valign=center cellspacing=0 cellpadding=1 border=0 bordercolor=#EEEEEE bgcolor=white");
	sb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	//title
	
  
	int i = 0;
	//Response.Write("<br><center><font size=+2>Sales Report &nbsp; </font> <b>");
	//paging class
	PageIndex m_cPI = new PageIndex(); //page index class
	m_cPI.PageSize = 50;
	if(Request.QueryString["p"] != null)
		m_cPI.CurrentPage = int.Parse(Request.QueryString["p"]);
	if(Request.QueryString["spb"] != null)
		m_cPI.StartPageButton = int.Parse(Request.QueryString["spb"]);

	int rows = 0;
	if(ds.Tables["banking"] != null)
		rows = ds.Tables["banking"].Rows.Count;
	m_cPI.TotalRows = rows;
	m_cPI.URI = "?r=" + DateTime.Now.ToOADate();
	i = m_cPI.GetStartRow();
	int end = i + m_cPI.PageSize;
	string sPageIndex = m_cPI.Print();

	if(v_print == "1"){
    sb.Append("<tr><td colspan=" + cols + " align=center><font size=+1><b>"+ Lang("Bank Deposit") +"</b></font></td></tr>");
    sb.Append ("<br><tr><td colspan="+cols+" align=left><b>Date: "+m_datePeriod+"</b></td></tr>");
	}
    
    sb.Append("<form name=f action=banking.aspx method=post>");
	sb.Append("<tr>");
	sb.Append("<th width=120 align=left>"+ Lang("Payment Method") +"</th>");
	if(Session["branch_support"] != null)
	{
	sb.Append("<th width=120 align=left>"+ Lang("Branch")+"</th>");
	}
	sb.Append("<th width=70 align=left>"+ Lang("Date") +"</th>");
	sb.Append("<th align=left>"+ Lang("Customer") +"</th>");
	sb.Append("<th align=left>"+ Lang("Invoice") +"#</th>");
	sb.Append("<th width=150 align=right>"+ Lang("Amount") +"</th>");
//	sb.Append("<th align=right>&nbsp;</th>");
	if(v_print !="1")
	sb.Append("<th align=right>"+ Lang("CHECK ALL") +"<input type=checkbox name=allbox onclick='CheckAll(); CalcTotal();'></th>");

	sb.Append("</tr>");
	sb.Append("<tr><td colspan=" + cols + "><hr></td></tr>");
    sb.Append("<tr><td colspan=" + cols + " align=center style=\"font:bold 13px arail; color: red \">" + Norecord +"</td></tr>");
	if(rows <= 0)
	{
		sb.Append("</table>");
		LTable.Text = sb.ToString();
		return;
	}
	double dSubTotal = 0;
	double dGrandTotal = 0;
	string payment_method_old = "-1";

	sb.Append("<input type=hidden name=rows value=" + rows + ">");

	bool bAlterColor = true;
//	for(; i < rows && i < end; i++)
	string pm = "";

	for(i=0; i < rows; i++)
	{
		DataRow dr = ds.Tables["banking"].Rows[i];
		string id = dr["id"].ToString();
     	string branch = dr["bName"].ToString();
		string date = dr["trans_date"].ToString();
		pm = dr["payment_method"].ToString();
		string note = dr["note"].ToString();
		string reference = dr["payment_ref"].ToString();
//		string invoice_number = dr["invoice_number"].ToString();
		string customer = dr["trading_name"].ToString();
		string company = dr["company"].ToString();
		string name = dr["name"].ToString();
		string invoice = dr["invoice_number"].ToString();
		string customer_id = dr["customer_id"].ToString();
	/*	if(customer == "")
			customer = company;
		if(customer == "")
			customer = name;
		if(customer == "")
			customer = dr["card_id"].ToString();
	*/
		if(customer_id == "0")
			customer = "Cash Sale";
		 else
			 customer = customer_id;


		string total = dr["amount"].ToString();

		DateTime dd = DateTime.Now; //DateTime.Parse(date);
		try
		{
			dd = DateTime.Parse(date);
		}
		catch(Exception ec)
		{
		}
		double dTotal = MyMoneyParse(total);

		string payment = GetEnumValue("payment_method", pm);
		if(payment == "" || payment == null)
			payment = "cash";

		if(pm != payment_method_old)
		{
			if(dSubTotal != 0)
			{   
				
				
				if(v_print != "1"){
				sb.Append("<tr><td colspan=" + (MyIntParse(cols) - 1).ToString() + " align=right><b>"+ Lang("UnDeposit Total") +" : </b></td>");
				sb.Append("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
				}else{
				sb.Append("<tr><td colspan=" + (MyIntParse(cols)).ToString() + " align=right><b>"+ Lang("UnDeposit Total") +" : </b>" + dSubTotal.ToString("c") + "</td></tr>");
			//print version
				}
				if(v_print != "1"){
				sb.Append("<tr><td colspan=" + (MyIntParse(cols) - 1).ToString() + " align=right><font color=red><b>"+ Lang("Deposit Total") +" : </font></b></td>");
				sb.Append("<td align=right><input type=text style=border:0;font-size:12;font-face:verdana;text-align:right readonly=true name=total" + payment_method_old + " value=$0></td></tr>");
			}
				dSubTotal = 0;
			// print version
			}

			sb.Append("<tr><td><font size=+1><b>" + payment[0].ToString().ToUpper() + payment.Substring(1, payment.Length-1) + "</b></font></td></tr>");
			bAlterColor = true;
		}
		payment_method_old = pm;

   
		sb.Append("<tr");
		if(bAlterColor)
			sb.Append(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		sb.Append(">");

		sb.Append("<input type=hidden name=id" + i.ToString() + " value=" + id + ">");
		sb.Append("<input type=hidden name=amount" + i.ToString() + " value=" + dTotal + ">");
		sb.Append("<input type=hidden name=type" + i.ToString() + " value=" + pm + ">");

		if(payment.ToLower() == "cheque")
			sb.Append("<td>" + dr["payment_ref"].ToString());
		else
			sb.Append("<td>&nbsp;");
		
		sb.Append(" " + note + " &nbsp&nbsp;");
		sb.Append("</td>");
		if(Session["branch_support"] != null)
		{
		sb.Append("<td>&nbsp;");
		sb.Append(" " +branch + " &nbsp&nbsp;");
	    sb.Append("</td>");
        } 
		sb.Append("<td>" + dd.ToString("dd-MM-yy") + "</td>");
//		sb.Append("<td><a href=invoice.aspx?" + invoice_number + "&r=" + DateTime.Now.ToOADate() + " class=o>" + dr["trading_name"].ToString() + "</a></td>");
		sb.Append("<td><a href=# onClick=\"window.open('viewcard.aspx?id= "+ customer + "','My','width=350, height=400')\" ");
		sb.Append(" >" + customer + "</a></td>");
		int nINCREASED = 12;
		string sinvoice = "";
		int nFound = 0;
		for(int k=0; k<invoice.Length; k++)
		{
			sinvoice += invoice[k].ToString();
	//		DEBUG("sinvoice = ", sinvoice);
			if(invoice[k].ToString() == ",")
				nFound++ ;
			
			if(nFound == nINCREASED)
			{
				nINCREASED += 12;
				sinvoice += "<br>";
			}
			
		}
		invoice = sinvoice;
		sb.Append("<td>" + invoice + "</td>");
		sb.Append("<td align=right>" + dTotal.ToString("c") + "</td>");
		if(v_print != "1") // print version
		sb.Append("<td align=right><input type=checkbox name=check" + i.ToString() + " onclick=CalcTotal(); ></td>");

		sb.Append("</tr>");

		dSubTotal += dTotal;
		dGrandTotal += dTotal;
	}

	string cols1 = (MyIntParse(cols) - 1).ToString();

	if(dSubTotal != 0)
	{   
		if(v_print =="1"){
     	cols1 = cols;
        sb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("UnDeposit Total") +":</b> " + dSubTotal.ToString("c") + "</td></tr>");
	}//print version
	else{
		sb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("UnDeposit Total") +":</b></td>");
		sb.Append("<td align=right>" + dSubTotal.ToString("c") + "</td></tr>");
	}
		if(v_print != "1"){
		sb.Append("<tr><td colspan=" + cols1 + " align=right><font color=red><b>"+ Lang("Deposit Total") +":</b></font></td>");
		sb.Append("<td align=right><input type=text style=border:0;font-size:12;font-face:verdana;text-align:right readonly=true name=total" + pm + " value=$0></td></tr>");
	}//print version
	

	}
	sb.Append("<tr><td>&nbsp;</td></tr>");
	if(v_print == "1"){
	cols1 = cols;
    sb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Grant UnDeposit Total") +":</b> <b>" + dGrandTotal.ToString("c") + "</b></td></tr>");
	sb.Append("<tr><td colspan=" + cols1 + " align=center><form action='banking.aspx?r="+ DateTime.Now.ToOADate() +" method=post>");
	sb.Append("<input type=hidden name=branch value='"+Session["branch_id"]+"'>");
	sb.Append("<input type=button class=print value='<< Back &nbsp;' onClick=\"window.location=('banking.aspx?r="+ DateTime.Now.ToOADate() +" ')\" >");
	sb.Append("<input class=print type=button value='&nbsp; &nbsp; Print &nbsp; &nbsp;' onClick=\"window.print()\"");
	sb.Append("></td></tr>");
	}//print version
	else{
	sb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Grant UnDeposit Total") +":</b></td>");
	sb.Append("<td align=right><b>" + dGrandTotal.ToString("c") + "</b></td>");
	sb.Append("</tr>");
	}
    
	if(v_print !="1"){
	sb.Append("<tr><td colspan=" + cols1 + " align=right><b><font color=red>"+ Lang("Grant Deposit Total") +":</b></font></td>");
	sb.Append("<td align=right><input type=text style=border:0;font-size:12;font-face:verdana;text-align:right readonly=true name=gtotal value=$0></td></tr>");
	sb.Append("</tr>");
    }// print version
    if(v_print != "1"){
	sb.Append("<tr><td colspan=" + cols + " align=right><br>");
	sb.Append("<b>"+Lang("To")+" : </b>" + PrintToAccountList());
	sb.Append("<b>"+Lang("Date")+" : </b><input type=text size=10 name=date value=" + DateTime.Now.ToString("dd-MM-yyyy") + ">&nbsp&nbsp;");
	sb.Append("<b>"+Lang("Ref")+"# : </b><input type=text size=10 name=ref>");
	sb.Append("<input type=submit name=cmd value="+ Lang("Deposit") +" " + Session["button_style"] + " ");
	sb.Append(" onclick=\"if(!confirm('"+ Lang("Processing Transaction") +"!!!')){return false;}\" ");
	sb.Append(">");
	sb.Append("</form>");
	sb.Append("<form action='banking.aspx?r="+ DateTime.Now.ToOADate() +"&print=1' name=print method=post>");
	sb.Append("<input type=hidden name=branch value="+branch_id+">");
	sb.Append("<input type=hidden name=period value="+s_period+">");
	sb.Append("<input type=hidden name=datepicker1_day value="+datepicker1_day+">");
	sb.Append("<input type=hidden name=datepicker1_month value="+datepicker1_month+">");
	sb.Append("<input type=hidden name=datepicker1_year value="+datepicker1_year+">");
	sb.Append("<input type=hidden name=datepicker2_day value="+datepicker2_day+">");
	sb.Append("<input type=hidden name=datepicker2_month value="+datepicker2_month+">");
	sb.Append("<input type=hidden name=datepicker2_year value="+datepicker2_year+">");
	if(s_period != null)
    sb.Append("<input type=submit value='Printable Version'>");
	sb.Append("</form>");
   
	} // print version
	
	sb.Append("</td></tr>");
	sb.Append("</table>");

	

	sb.Append("\r\n<script language=JavaScript");
	sb.Append(">\r\n");
	sb.Append("function CalcTotal()\r\n");
	sb.Append("{	var total = 0;\r\n");
	sb.Append("		var gtotal = 0;\r\n");
	sb.Append("		var type = document.f.type0.value;\r\n");
	sb.Append("		var type_old = type;\r\n");
	sb.Append("for(var i=0; i<" + rows + "; i++)\r\n");
	sb.Append("{		");
	sb.Append(" type = eval(\"document.f.type\" + i + \".value\");\r\n");
	sb.Append(" if(type != type_old)\r\n");
	sb.Append("	{			");
	sb.Append("		eval(\"document.f.total\" + type_old + \".value = '$' + total\");");
	sb.Append("		type_old = type; gtotal += total; total = 0; \r\n");
	sb.Append("	}			");
	sb.Append("	if(eval(\"document.f.check\" + i + \".checked\"))\r\n");
	sb.Append("		total += Number(eval(\"document.f.amount\" + i + \".value\"));\r\n");
	sb.Append(" total = Math.round(total * 100) / 100; \r\n");
	sb.Append("}			");
	sb.Append("	eval(\"document.f.total\" + type_old + \".value = '$' + total\");");
	sb.Append(" gtotal += total; ");
	sb.Append(" gtotal = Math.round(gtotal * 100) / 100;");
	sb.Append(" document.f.gtotal.value = '$' + gtotal;\r\n");
	sb.Append("}\r\n</script");
	sb.Append(">");


	LTable.Text = sb.ToString();
}

bool DoRollBackDeposit()
{
	string roll_id = Request.QueryString["rid"];
if(roll_id != "" && roll_id != null)
{
	if(TSIsDigit(roll_id))
	{
	string sc = "";
	sc += " SELECT * FROM tran_deposit_id WHERE id = "+ roll_id +" ";
	int rows =0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "roll_id");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " IF EXISTS (SELECT id FROM tran_deposit WHERE id = "+ roll_id +") ";
	sc += " UPDATE account SET balance = balance - (SELECT total FROM tran_deposit WHERE id = " + roll_id +") ";
	sc += " WHERE id = (SELECT account_id FROM tran_deposit WHERE id = "+ roll_id +") ";
	sc += " DELETE FROM tran_deposit WHERE id = "+ roll_id +" ";
	
//DEBUG("sc = ", sc);
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

	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["roll_id"].Rows[i];
		string tran_id = dr["tran_id"].ToString();
		string id = dr["id"].ToString();
		string kid = dr["kid"].ToString();
		
		sc = " UPDATE account SET balance = balance + (SELECT amount FROM trans WHERE id = " + tran_id +" ) ";
		sc += " WHERE CONVERT(varchar(5), class1) + CONVERT(varchar(5), class2) + CONVERT(varchar(5), class3) + CONVERT(varchar(5), class4) ";
		sc += " = 1116 "; //(SELECT dest FROM trans WHERE id = "+ tran_id +") ";
		sc += " UPDATE trans SET banked = 0 , trans_bank_id = '', dest = '1116' WHERE id = "+ tran_id +"";
		sc += " DELETE FROM tran_deposit_id WHERE tran_id = "+ tran_id +" AND kid = "+ kid +"";
//	DEBUG("sc1 = ", sc );
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
	}
}
	return true;
}

bool DoDeposit()
{
	m_date = Request.Form["date"];
	m_ref = Request.Form["ref"];

	string sc = "BEGIN TRANSACTION ";
	sc += " SET DATEFORMAT dmy ";
	sc += " INSERT INTO tran_deposit (total, deposit_date, ref, staff, account_id) ";
	sc += " VALUES(" + Request.Form["gtotal"] + ", '" + m_date + "', '" + EncodeQuote(m_ref) + "', " + Session["card_id"] + "";
	sc += " , "+ Request.Form["account_id"];
	sc += " ) ";
	sc += " SELECT IDENT_CURRENT('tran_deposit') AS id ";
	sc += " COMMIT ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "tranid") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting IDENT");
			return false;
		}
		m_tranID = ds.Tables["tranid"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = "";

	string account_class = GetAccountClass(Request.Form["account_id"]);
//DEBUG("account clas = ", account_class);
	int rows = MyIntParse(Request.Form["rows"]);
	for(int i=0; i<rows; i++)
	{
		if(Request.Form["check" + i] != "on")
			continue;
		string id = Request.Form["id" + i];
		sc += " INSERT INTO tran_deposit_id (id, tran_id) VALUES(" + m_tranID + ", " + id + ") ";	
		sc += " UPDATE trans SET banked = 1, dest=" + account_class + ", trans_bank_id = " + m_tranID;
		sc += " WHERE id = " + id + " ";
	}

	//update account balance
	sc += " UPDATE account SET balance = balance - " + Request.Form["gtotal"];
	sc += " WHERE class1=1 AND class2=1 AND class3=1 AND class4=6 ";
	sc += " UPDATE account SET balance = balance + " + Request.Form["gtotal"];
	sc += " WHERE id=" + Request.Form["account_id"];

	if(sc == "")
		return false;

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
//DEBUG("ID =", m_tranID);
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=banking.aspx?id=" + m_tranID + "\">");
//	PrintBankingSlip();
	return true;
}

bool GetTransaction(string id)
{
	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT t.id, d.trans_date, c.trading_name, d.payment_method, d.payment_ref, d.note, t.amount ";
	sc += ", c.company, c.name, d.bank, d.branch, d.paid_by ";
	sc += " FROM tran_detail d JOIN trans t ON t.id=d.id LEFT OUTER JOIN card c ON c.id=d.card_id ";
	sc += " WHERE t.id = " + id;// + " AND t.dest = 1116 "; //unbanked trans in undeposite
	sc += " ORDER BY d.payment_method ";
//DEBUG("banking = ", sc);

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		int rows = myCommand.Fill(ds, "banking");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintBankingSlip()
{
	int i = 0;
	int rows = 0;
	string sc = "SELECT a.name4 AS bank_name, i.tran_id, d.deposit_date ";
	sc += " FROM tran_deposit_id i JOIN tran_deposit d ON d.id=i.id ";
	sc += " JOIN trans t ON t.id = i.tran_id ";
	sc += " JOIN account a ON (a.class1 * 1000 + a.class2 * 100 + a.class3 * 10 + a.class4) = t.dest ";
	sc += " WHERE i.id = " + m_tranID;
//DEBUG("sc +", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds, "ids");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	rows = ds.Tables["ids"].Rows.Count;
	if(rows <= 0)
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>"+ Lang("Deposit Record Not Found") +"</h3>");
		return false;
	}

	string deposit_date = DateTime.Parse(ds.Tables["ids"].Rows[0]["deposit_date"].ToString()).ToString("dd-MM-yyyy");
	string bank_name = "Bank Name";
	for(i=0; i<rows; i++)
	{
		string id = ds.Tables["ids"].Rows[i]["tran_id"].ToString();
		bank_name = ds.Tables["ids"].Rows[i]["bank_name"].ToString();
		if(!GetTransaction(id))
			return false;
	}

	if(ds.Tables["banking"] != null)
		rows = ds.Tables["banking"].Rows.Count;

	string cols = "5";

	StringBuilder sb = new StringBuilder();

	sb.Append("<tr>");
	sb.Append("<th width=300 align=left><font color=#888888>"+ Lang("Cheque issued by") +"</font></th>");
	sb.Append("<th width=70 align=left><font color=#888888>"+ Lang("Bank") +"</font></th>");
	sb.Append("<th align=left><font color=#888888>"+ Lang("Branch") +"</font></th>");
	sb.Append("<th width=90 align=right><font color=#888888>"+ Lang("Ref/Chk") +"#</font></th>");
	sb.Append("<th width=90 align=right><font color=#888888>"+ Lang("Amount") +"</font></th>");
	sb.Append("</tr>");

	sb.Append("<tr><td colspan=" + cols + "><hr></td></tr>");

	double dSubTotal = 0;
	double dCashTotal = 0;
	double dChequeTotal = 0;
	double dGrandTotal = 0;
	
	string payment_method_old = "-1";

	string pm = "";
	for(i=0; i < rows; i++)
	{
		DataRow dr = ds.Tables["banking"].Rows[i];
		string id = dr["id"].ToString();
		string company = dr["company"].ToString();
		string paid_by = dr["paid_by"].ToString();
		if(paid_by != "")
			company = paid_by; //cheque paid by different company or title
		else if(company == "")
			company = dr["name"].ToString(); //use personal name

		string bank = dr["bank"].ToString();
		string branch = dr["branch"].ToString();
		string date = dr["trans_date"].ToString();
		pm = dr["payment_method"].ToString();
		string note = dr["note"].ToString();
		string reference = dr["payment_ref"].ToString();
		string customer = dr["trading_name"].ToString();
		string total = dr["amount"].ToString();

		double dTotal = MyMoneyParse(total);
//DEBUG("total=", total);
		string payment = GetEnumValue("payment_method", pm);
//DEBUG("payment = ", payment);
		if(pm != payment_method_old)
		{
			if(dSubTotal != 0)
			{
				if(payment_method_old == "1")
					dCashTotal = dSubTotal;
				else if(payment_method_old == "2")
					dChequeTotal = dSubTotal;
				dSubTotal = 0;
			}
		}
		payment_method_old = pm;

		dSubTotal += dTotal;
		dGrandTotal += dTotal;

		if(payment.ToLower() != "cheque")
			continue;

		sb.Append("<tr>");
		sb.Append("<td>" + company + "</td>");
		sb.Append("<td>" + bank + "</td>");
		sb.Append("<td>" + branch + "</td>");
		sb.Append("<td align=right>" + reference + "</td>");
		sb.Append("<td align=right>" + dTotal.ToString("c") + "</td>");
		sb.Append("</tr>");

	}
	if(dSubTotal != 0)
	{
		if(payment_method_old == "1")
			dCashTotal = dSubTotal;
		else if(payment_method_old == "2")
			dChequeTotal = dSubTotal;
		dSubTotal = 0;
	}

	sb.Append("<tr><td colspan=" + cols + "><hr></td></tr>");

	int cols1 = MyIntParse(cols) - 1;

	sb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Total") +" : &nbsp; </b></td>");
	sb.Append("<td align=right>" + dChequeTotal.ToString("c") + "</td></tr>");

	sb.Append("<tr><td>&nbsp;</td></tr>");
	sb.Append("<tr><td>&nbsp;</td></tr>");

	sb.Append("<tr><td colspan=" + cols1 + " align=right><font size=+1><b>"+ Lang("Deposit Total") +" : &nbsp; </b></font></td>");
	sb.Append("<td align=right><font size=+1><b>" + dGrandTotal.ToString("c") + "</b></font></td></tr>");


	//begin write out
	string header = ReadSitePage("deposit_header");
	header = header.Replace("@@BANK_NAME", bank_name); 

	StringBuilder sbb = new StringBuilder();

	sbb.Append("<table align=center valign=center cellspacing=0 cellpadding=0 border=0 bordercolor=#EEEEEE bgcolor=white");
	sbb.Append(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	sbb.Append("<tr><td colspan=" + cols + ">");
	sbb.Append(header);
	sbb.Append("</td></tr>");

	sbb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Date") +" : &nbsp; </b></td>");
//	sbb.Append("<td align=right>" + DateTime.Now.ToString("dd-MM-yyyy") + "</td></tr>");
	sbb.Append("<td align=right>" + deposit_date + "</td></tr>");

	sbb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Notes") +" : &nbsp; </b></td>");
	sbb.Append("<td align=right>" + dCashTotal.ToString("c") + "</td></tr>");

	sbb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Cheques") +" : &nbsp; </b></td>");
	sbb.Append("<td align=right>" + dChequeTotal.ToString("c") + "</td></tr>");

	sbb.Append("<tr><td colspan=" + cols1 + " align=right><b>"+ Lang("Total") +" : &nbsp; </b></td>");
	sbb.Append("<td align=right>" + dGrandTotal.ToString("c") + "</td></tr>");

	sbb.Append("<tr><td>&nbsp; <br> &nbsp;</td></tr>");

	if(dChequeTotal > 0)
	{
		sbb.Append("<tr><td colspan=" + cols + "><h4>"+ Lang("Particulars of cheques") +"</h4></td></tr>");
		sbb.Append(sb.ToString());
	}
	if(m_bPrint)
	{
		Response.Write("<html>");
		Response.Write("<head>");

		Response.Write("<style type=text/css>");
		Response.Write("td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:vardana;}");
		Response.Write("body{background:#FFFFFF;font:10px Verdana;}");
		Response.Write("</style></head>");

		Response.Write("<body onload='window.print()'>");
		Response.Write(sbb.ToString());
		Response.Write("</table>");
		Response.Write("</body>");
		Response.Write("</html>");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=banking.aspx?id=" + m_tranID + "\">");
	}
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write(sbb.ToString());
		Response.Write("<tr><td colspan=" + cols + " align=right><br>");
		Response.Write("<input type=button "+ Session["button_style"] +" value='"+ Lang("Roll Back Trans") +"' ");
		Response.Write(" onclick=\"if(!confirm('"+ Lang("Confirm Roll Back") +"!!!!')){return false;}else{ window.location=('"+ Request.ServerVariables["URL"] +"?rid="+ m_tranID +"' );} \"> ");
		Response.Write("<input type=button " + Session["button_style"]);
		Response.Write(" onclick=window.location=('banking.aspx?id=" + m_tranID + "&t=p') value='"+ Lang("Print") +"'>");
		Response.Write("</td></tr>");
		Response.Write("</table>");
		PrintAdminFooter();
	}
	return true;
}

string PrintToAccountList()
{
	int rows = 0;
	string sc = "SELECT * FROM account ORDER BY class1, class2, class3, class4";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, "account");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	StringBuilder sb = new StringBuilder();

	sb.Append("<select name=account_id>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["account"].Rows[i];
		string id = dr["id"].ToString();
//		string number = dr["class1"].ToString() + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		string disnumber = dr["class1"].ToString() + "-" + dr["class2"].ToString() + dr["class3"].ToString() + dr["class4"].ToString();
		sb.Append("<option value=" + id);
		if(id == m_accountID)
			sb.Append(" selected");
		sb.Append(">" + disnumber + " " + dr["name4"].ToString() + " " +dr["name1"].ToString()+ " $" +dr["balance"].ToString());		
	}
	sb.Append("</select>");
	return sb.ToString();
}

</script>

<table width=100%>
<tr><td><asp:Label id=LTable runat=server/></td></tr>

<tr><td>

</td></tr>

<tr><td>
<asp:Label id=LFooter runat=server/>
</td></tr>

</table>
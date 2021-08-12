<script runat=server>

DataSet ds = new DataSet();
StringBuilder sb = new StringBuilder();

string[] m_EachMonth = new string[16];

string m_sdFrom = "";
string m_sdTo = "";
string m_smFrom = "";
string m_smTo = "";

string m_datePeriod = "";
int m_nPeriod = 0;


void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("normal"))
		return;

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
		Session["report_month_from"] = m_smFrom;
		Session["report_month_to"] = m_smTo;
	}
	
	Session["report_period"] = m_nPeriod;
	switch(m_nPeriod)
	{
	case 0:
		m_datePeriod = "This Month";
		break;
	case 1:
		m_datePeriod = "Last Month";
		break;
	case 2:
		m_datePeriod = "Last Three Month";
		break;
	case 3:
		m_datePeriod = "From " + m_sdFrom;
		m_datePeriod += " To " + m_sdTo;
		break;
	case 4:
		m_datePeriod = "From " + m_smFrom;
		m_datePeriod += " To " + m_smTo;
		break;
	default:
		break;
	}

	PrintHeaderAndMenu();

	Response.Write("<br><center><h3>Download Invoices</h3>");

	if(Request.Form["cmd"] == "Export")
	{
		if(!DoQuery())
			return;

		string strPath = Server.MapPath("/download/") + "\\";
		string lname = Session["name"].ToString();
		int bpos = lname.IndexOf(" ");
		if(bpos > 0)
			lname = lname.Substring(0, bpos);
		lname = lname.Replace("/", "-"); //prevent slash in names, some client does this
		string fileName = lname + "_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + "_inv.csv";
		strPath += fileName;
		string url = "/download/" + fileName;

		Encoding enc = Encoding.GetEncoding("iso-8859-1");
		byte[] Buffer = enc.GetBytes(sb.ToString());

		FileStream newFile = new FileStream(strPath, FileMode.Create);
		newFile.Write(Buffer, 0, Buffer.Length);
		newFile.Close();
		Response.Write("<h4>File is ready to download</h4><br>");
		Response.Write("<a href='" + url + "' class=o>" + fileName + "</a><br><br><br><br><br><br><br>");
		Response.Write("<table width=100%?");
     	Response.Write("<tr><td>"+ReadSitePage("foot_menu")+"");
    	Response.Write("</td></tr></table>");
//		Response.Write(sb.ToString());
	}
	else
	{
		PrintDateSelectForm();
	}

	Response.Write("</body></html>");
//	PrintAdminFooter();
}

void PrintDateSelectForm()
{
	Response.Write("<form name=myform action=expinv.aspx method=post>");
	Response.Write("<table>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=6><b>Date Range</b></td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=0 checked>This Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=1>Last Month</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=2>Last Three Months</td></tr>");
	Response.Write("<tr><td colspan=6><input type=radio name=period value=3>Select Date Range</td></tr>");

	int i = 1;
	datePicker(); //call date picker function
	//from date
	Response.Write("<tr><td><b> &nbsp; From Date </b>");
	//DateTime dstep = DateTime.Parse("01/01/2003");
	//DateTime dend = DateTime.Now;

	string s_day = DateTime.Now.ToString("dd");
	string s_month = DateTime.Now.ToString("MM");
	string s_year = DateTime.Now.ToString("yyyy");
	Response.Write("<select name='Datepicker1_day' onChange=\"tg_mm_setdays('document.forms[document.forms.length-1].Datepicker1',1);\">");
	for(int d=1; d<32; d++)
	{
		//if(int.Parse(s_day) == d)
		//	Response.Write("<option value="+ d +" selected>"+d+"</option>");
		//else
		Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker1_month' onChange=\"tg_mm_setdays('document.forms[document.forms.length-1].Datepicker1',1);\" style=''>");

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
	Response.Write("<select name='Datepicker1_year' onChange=\"tg_mm_setdays('document.forms[document.forms.length-1].Datepicker1',1);\" style=''>");
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
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[document.forms.length-1].Datepicker1',1)");
	Response.Write("</script ");
	Response.Write(">");
//------ END first display date -----------

	//------ start second display date -----------
	Response.Write("<td> &nbsp; TO: ");
	Response.Write("<select name='Datepicker2_day' onChange=\"tg_mm_setdays('document.forms[document.forms.length-1].Datepicker2',1);\" style=''>");
	for(int d=1; d<32; d++)
	{
		if(int.Parse(s_day) == d)
			Response.Write("<option value="+ d +" selected>"+d+"</option>");
		else
			Response.Write("<option value="+ d +">"+d+"</option>");
	}
	Response.Write("</select>");
	Response.Write("<select name='Datepicker2_month' onChange=\"tg_mm_setdays('document.forms[document.forms.length-1].Datepicker2',1);\" style=''>");

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
	Response.Write("<select name='Datepicker2_year' onChange=\"tg_mm_setdays('document.forms[document.forms.length-1].Datepicker2',1);\" style=''>");
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
	Response.Write("<script language=javascpt> tg_mm_setdays('document.forms[document.forms.length-1].Datepicker2',1)");
	Response.Write("</script ");
	Response.Write(">");
	Response.Write("</td>");
	//Response.Write("</td><td>&nbsp;<input type=submit nam=cmd value='Search' "+Session["button_style"].ToString()+"></td>");
	Response.Write("</tr>");
//------ END second display date -----------
	//Response.Write("</td></tr>");
	Response.Write("<tr><td colspan=6 align=right>");
	Response.Write("<input type=submit name=cmd value=Export class=b>");
	Response.Write("</td></tr>");
	Response.Write("</table></center>");
	Response.Write("</form>");
	Response.Write("<table width=100%?");
	Response.Write("<tr><td>"+ReadSitePage("foot_menu")+"");
	Response.Write("</td></tr></table>");
}

bool DoQuery()
{
	string dateSql = "";
	switch(m_nPeriod)
	{
	case 0:
		dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 0 ";
		break;
	case 1:
		dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) = 1 ";
		break;
	case 2:
		dateSql = " AND DATEDIFF(month, i.commit_date, GETDATE()) >= 1 AND DATEDIFF(month, i.commit_date, GETDATE()) <= 3 ";
		break;
	case 3:
		dateSql = " AND i.commit_date BETWEEN '" + m_sdFrom + "' AND DATEADD(day, 1, '" + m_sdTo + "') ";
		//m_dateSql = " AND i.commit_date >= '" + m_sdFrom + "' AND i.commit_date <= '" + m_sdTo + " 23:59 "+"' ";
		break;
	default:
		break;
	}

	string table = "invoice";
	ds.Clear();

	string sc = "SET DATEFORMAT dmy ";
	sc += " SELECT i.commit_date, i.invoice_number, i.cust_ponumber, i.price, i.freight, i.tax, i.total ";
	sc += ", s.code, s.name, s.quantity, s.commit_price ";
	sc += " FROM invoice i JOIN sales s ON s.invoice_number=i.invoice_number ";
	sc += " WHERE i.card_id = " + Session["card_id"];
	sc += dateSql;
	sc += " ORDER BY i.commit_date ";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, table);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	int i = 0;
	
	sb.Append(m_sCompanyTitle + " Invoices for " + Session["trading_name"] + " (" + m_datePeriod + ")\r\n");
	sb.Append("\"Date\",\"INV#\",\"PO#\",\"ItemTotal\",\"Freight\",\"TAX\",\"TotalAmount\",\"ItemCode\",\"ItemDesc\",\"ItemQty\",\"ItemPrice\",\r\n"); 

	for(i=0; i<ds.Tables[table].Rows.Count; i++)
	{
		string sDate = DateTime.Parse(ds.Tables[table].Rows[i][0].ToString()).ToString("dd-MM-yyyy");
		sb.Append("\"" + sDate + "\", ");
		for(int j=1; j<ds.Tables[table].Columns.Count; j++)
		{
			string svalue = ds.Tables[table].Rows[i][j].ToString();
			svalue = svalue.Replace(",", " ");
			sb.Append("\"" + EncodeDoubleQuote(svalue) + "\",");
		}
		sb.Append("\r\n");
	}
	return true;
}

bool BuildResultSQL(string table)
{
	return true;
}
</script>

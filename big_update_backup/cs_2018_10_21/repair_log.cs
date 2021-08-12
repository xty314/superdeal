<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_JOBRA = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;
	InitializeData(); //init functions

	if(!getRepairLog())
		return;
	string jobno = "";
	if(Request.QueryString["job"] != null)
		jobno = Request.QueryString["job"].ToString();
	Response.Write("<br>");
	Response.Write("<h5><center>Repair Log for Repair#:"+ jobno +"</center></h5>");
	BindGrid();
	//LFooter.Text = m_sAdminFooter;
}

bool getRepairLog()
{
	int rows = 0;
	if(Request.QueryString["job"] != "" && Request.QueryString["job"] != null)
		m_JOBRA = Request.QueryString["job"].ToString();
	string sc = "SELECT r.sn AS SN#, CONVERT(varchar(100),r.action_desc) AS 'Action Desc',  r.log_time, '<a href=techr.aspx?id='+ r.repair_id +' class=o>'+ r.repair_id+'</a>' AS Repair#, c.name AS Staff";
	sc += " FROM repair_log r JOIN card c ON c.id = r.staff ";
	sc += " WHERE 1 = 1 ";
	if(m_JOBRA != "")
		sc += " AND r.repair_id = '"+ m_JOBRA +"' ";
	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		if(TSIsDigit(Request.QueryString["code"]))
			sc += " AND code = "+ Request.QueryString["code"];
	sc += " ORDER BY log_time DESC ";
//DEBUG("sc =", sc);	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "repair_log");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<center><h5><font color=Red>NO Repair Log on this JOB</h5></font></center>");
		return false;
	}

	/*for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["repair_log"].Rows[i];
		string id = dr["repair_id"].ToString();
		string name = dr["name"].ToString();
		//string company = dr["company"].ToString();
		//string email = dr["email"].ToString();
		//string trading_name = dr["trading_name"].ToString();
		//string contact = dr["contact"].ToString();
		//string phone = dr["phone"].ToString();
		//string addr1 = dr["address1"].ToString();
		//string addr2 = dr["address2"].ToString();
		//string city = dr["city"].ToString();
		
		string sn = dr["serial_number"].ToString();
		string fault = dr["action_desc"].ToString();
		string log_time = dr["log_time"].ToString();
		string contact = dr["contact"].ToString();
		string inv = dr["invoice_number"].ToString();
		string code = dr["code"].ToString();
		//Response.Write("<center><h5>Customer Details</h5></center>");
		Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#83CCF6 bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:11pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr><td valign=top >");
		Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#83CCF6 bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#6DE2A7><th colspan=2>Customer Details</th></tr>");
		Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Customer ID:</th><td> "+ id +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Name:</th><td> "+ name +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Trading Name:</th><td> "+ trading_name +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Company:</th><td> "+ company +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Address:</th><td> "+ addr1 +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>&nbsp;</th><td> "+ addr2 +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>&nbsp;</th><td> "+ city +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Email:</th><td> <a title='email to customer' href=mailto:"+ email +">"+ email +"</a></td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Phone:</th><td> "+ phone +"</td></tr>");
		//Response.Write("<tr><th align=right bgcolor=#83CCF6>Fax:</th><td> "+ fax +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Contact:</th><td> "+ contact +"</td></tr>");
		Response.Write("</table>");
		Response.Write("</td><td>&nbsp;</td>");
		Response.Write("<td valign=top>");
		Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#83CCF6 bgcolor=white");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
		Response.Write("<tr bgcolor=#6DE2A7><th colspan=2>Repair Details</th></tr>");
		Response.Write("<tr><td colspan=3><br size=1; color=black></td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Repair Status:</th><th> "+ status.ToUpper() +"</th></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>SN#:</th><td><a title='check SN#' href='snsearch.aspx?sn="+ sn +"' class=o target=_blank>"+ sn +"</a></td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>INV#:</th><td><a title='view invoic number' href='invoice.aspx?"+ inv +"' class=o target=_blank>"+ inv +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Product Des:</th><td>"+ prod_desc +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Environment:</th><td>&nbsp;</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>OS:</th><td>"+ os +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>RAM:</th><td>"+ ram +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>VGA:</th><td>"+ vga +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>MoBo:</th><td>"+ motherboard +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Other:</th><td>"+ other +"</td></tr>");
		Response.Write("<tr><th align=right bgcolor=#83CCF6>Fault Descriptions:</th><td> <textarea col=10 rows=5 readonly>"+ fault +"</textarea></td></tr>");
		
		Response.Write("</table>");
		Response.Write("</td></tr>");
		Response.Write("<tr><td colspan=3 align=right><input type=button value='    close   ' "+Session["button_style"]+" onclick='window.close()'>");
		Response.Write("</table>");
	}*/
	return true;
}
void BindGrid()
{
	DataView source = new DataView(dst.Tables[0]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}



</script>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"><html>
<head>
    <title>--- Repair Log ---</title> 
</head>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#E3E3E3
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=7pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=100
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#E3E3E3 ForeColor=black Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#E3E3E3/>

</asp:DataGrid>
</form>

<asp:Label id=LFooter runat=server/>
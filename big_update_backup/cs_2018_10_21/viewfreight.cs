
<script runat=server>
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	//InitializeData(); //init functions

	PrintAdminHeader();
	PrintAdminMenu();
	doCreateViewFreight();
		
	
//	LFooter.Text = m_sAdminFooter;
}

bool GetFreightCat()
{
	string sc = "SELECT DISTINCT name FROM ship ";
	sc += " ORDER BY name ";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "cat");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<select name=cat ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "");
	Response.Write("&c='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write(">");
	Response.Write("<option value=all>All</option>");
	for(int i=0; i<rows; ++i)
	{
		Response.Write("<option value='"+ dst.Tables["cat"].Rows[i]["name"].ToString() +"' ");
		if(Request.QueryString["c"] == dst.Tables["cat"].Rows[i]["name"].ToString())
			Response.Write(" selected ");
		Response.Write(">"+ dst.Tables["cat"].Rows[i]["name"].ToString() +"</option>");
	}

	return true;
}
bool doCreateViewFreight()
{

	int rows = 0;
	
	string sc = "SELECT * FROM ship ";
	if(Request.QueryString["c"] != null && Request.QueryString["c"] != "" && Request.QueryString["c"] != "all")
		sc += " WHERE name = '"+ Request.QueryString["c"] +"' ";
	sc += " ORDER BY name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "freight");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<br><center><h4>SHIPPING TICKET TALBE</center><br>");
	Response.Write("<table align=center cellspacing=1 cellpadding=3 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:12pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=8> SELECT COURIER COMPANY: ");
	GetFreightCat();
	Response.Write("</td></tr>");
	Response.Write("<tr bgcolor=#EEEEE><td></td><td>NAME</td><td>DESCRP</td><td>PRICE</td><td>PHONE</td><td>WEB</td><td>PREFIX</td><td>SUFFIX</td></tr>");
	bool bAlter = false;
	for(int i=0; i<rows; ++i)
	{
		DataRow dr = dst.Tables["freight"].Rows[i];
		string id = dr[0].ToString();
		string name = dr[1].ToString();
		string description = dr[2].ToString();
		string price = dr[3].ToString();
		string prefix = dr[4].ToString();
		string suffix = dr[5].ToString();
		string phone = dr[6].ToString();
		string web = dr[7].ToString();
						
		Response.Write("<tr");
		if(bAlter)
			Response.Write(" bgcolor=#EEEEEE ");
		Response.Write(">");
		bAlter = !bAlter;
		Response.Write("<td>"+ (i+1) +"</td>");
		Response.Write("<td>"+ name +"</td>");
		Response.Write("<td><b>"+ description +"</td>");
		Response.Write("<td>"+ MyDoubleParse(price).ToString("c") +"</td>");
		Response.Write("<td>"+ phone +"</td>");
		Response.Write("<td>"+ web +"</td>");
		Response.Write("<td>"+ prefix +"</td>");
		Response.Write("<td>"+ suffix +"</td>");
		Response.Write("</tr>");		
		
	}
	Response.Write("<tr><td colspan=8 align=right><a title='close this window' href='javascript:window.close();' class=o>close</a><br><br></td></tr>");
	Response.Write("</table>");
	return true;
}



</script>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"><html>
<head>
    <title>--- Customer Details ---</title> 
</head>

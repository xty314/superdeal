<script runat=server>
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_code = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;

	if(Request.QueryString["c"] == null || Request.QueryString["c"] == "")
	{
		Response.Write("<h4>Item code not found</h4>");
		return;
	}
	m_code = Request.QueryString["c"];

	DoAnalyzeStock();
}

bool DoAnalyzeStock()
{
	int rows = 0;
	string sc = " SELECT ";
	sc += " (SELECT SUM(i.qty) ";
	sc += " FROM purchase_item i JOIN purchase p ON p.id = i.id ";
	sc += " WHERE i.code = " + m_code;
	sc += " AND p.status in (3, 4)) AS received_purchase "; //received status
	sc += ", ";
	sc += " (SELECT SUM(i.qty) ";
	sc += " FROM purchase_item i JOIN purchase p ON p.id = i.id ";
	sc += " WHERE i.code = " + m_code;
	sc += " AND p.status IN(1,2)) AS on_order_purchase "; //received status
	sc += ", ";
	sc += " (SELECT SUM(quantity) FROM sales WHERE code = " + m_code + ") ";
	sc += " AS total_sales ";
	sc += ", name FROM code_relations WHERE code=" + m_code;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "stock");

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<center><h5><font color=Red>Item " + m_code + " not found.</h5></font></center>");
		return false;
	}

	DataRow dr = dst.Tables["stock"].Rows[0];
	string received_purchase = dr["received_purchase"].ToString();
	string on_order_purchase = dr["on_order_purchase"].ToString();
	string total_sales = dr["total_sales"].ToString();
	string name = dr["name"].ToString();

	Response.Write("<table align=center cellspacing=0 cellpadding=0 border=1 bordercolor=#83CCF6 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:11pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr bgcolor=#6DE2A7><th colspan=2>Purchase & Sales Total</th></tr>");
	Response.Write("<tr><td colspan=2>&nbsp;</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6 nowrap>Item Code:</th><td> " + m_code + "</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Name:</th><td> "+ name +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Stocked Purchase:</th><td> "+ received_purchase +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>On Order Purchase:</th><td> "+ on_order_purchase +"</td></tr>");
	Response.Write("<tr><td align=right bgcolor=#83CCF6>Sales:</th><td> "+ total_sales +"</td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=button value='    close   ' "+ Session["button_style"] +"  onclick='window.close();'>");
	Response.Write("</table>");
	return true;
}

</script>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"><html>
<head>
    <title>--- Purchase & Sales Total---</title> 
</head>

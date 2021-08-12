<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

string m_type = "";
string m_id = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;
	
	if(Request.Form["id"] != null)
	{
		if(DoUpdate())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=discount.aspx?r=" + DateTime.Now.ToOADate() + "\">");
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(!DoSearch())//m_itype))
		return;

	m_id = Request.QueryString["id"];
	PrintDiscountBody();
	LFooter.Text = m_sAdminFooter;
}

Boolean DoSearch()//int m_itype)
{
	int rows = 0;
	string factor = "level";
	string sc = "SELECT * FROM discount WHERE factor='" + factor + "' ORDER BY factor_id";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, factor);
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	factor = "qty";
	sc = "SELECT * FROM discount WHERE factor='" + factor + "' ORDER BY factor_id";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(ds, factor);
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void PrintDiscountBody()
{
	Response.Write("<br><center><h3>DISCOUNT SETTINGS</h3>");
	BindLevelTable();
	Response.Write("<br>");
	BindQtyTable();
}

/////////////////////////////////////////////////////////////////
void BindLevelTable()
{
	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=3><b>Customer Levels</b></td></tr>");

	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>ID&nbsp;</td>");
	Response.Write("<th>&nbsp;Level DESC&nbsp;</th>\r\n");
	Response.Write("<th>&nbsp;Margin Topup&nbsp;</th>\r\n");
	Response.Write("<th nowrap>&nbsp;Average Last 12 Months&nbsp;</th>\r\n");
	Response.Write("<th>&nbsp;Note&nbsp;</th>\r\n");
	Response.Write("<th>&nbsp;</th>\r\n");
	Response.Write("</tr>\r\n");

	int rows = ds.Tables["level"].Rows.Count;
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["level"].Rows[i];
		string id = dr["id"].ToString();
		string factor_id = dr["factor_id"].ToString();
		string level = dr["name"].ToString();
		string margin = dr["data1"].ToString();
		string average = dr["data2"].ToString();
		string note = dr["note"].ToString();
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");

		if(m_id == id)
		{
			Response.Write("<form action=discount.aspx method=post>");
			Response.Write("<input type=hidden name=id value=" + m_id + ">");
			Response.Write("<input type=hidden name=factor value=level>");
			Response.Write("<td><input type=text style='text-align:right' size=5 name=factor_id value='" + factor_id + "'></td>");
			Response.Write("<td><input type=text style='text-align:right' size=10 name=name value='" + level + "'></td>");
			Response.Write("<td><input type=text style='text-align:right' size=5 name=data1 value='" + margin + "'></td>");
			Response.Write("<td><input type=text style='text-align:right' size=15 name=data2 value='" + average + "'></td>");
			Response.Write("<td><input type=text name=note size=30 value='" + note + "'></td>");
			Response.Write("<td><input type=submit name=cmd value='OK'>");
			Response.Write("<input type=button onclick=window.location=('discount.aspx?r=" + DateTime.Now.ToOADate() + "') value='Cancel'></td>");
			Response.Write("</form>");
		}
		else
		{
			Response.Write("<td>" + factor_id + "</td>");
			Response.Write("<td>" + level + "</td>");
			Response.Write("<td>" + margin + "</td>");
			Response.Write("<td>" + average + "</td>");
			Response.Write("<td>" + note + "</td>");
			Response.Write("<td align=right><a href=discount.aspx?id=" + id + " class=o>EDIT</a></td>");
		}
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}


/////////////////////////////////////////////////////////////////
void BindQtyTable()
{
	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=3><b>Quantity Factor</b></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<th>ID&nbsp;</th>");
	Response.Write("<th>&nbsp;Quantity Break&nbsp;</th>\r\n");
	Response.Write("<th>&nbsp;Margin Topup&nbsp;</th>\r\n");
	Response.Write("<th width=50%>&nbsp;Note&nbsp;</th>\r\n");
	Response.Write("<th>&nbsp;</th>\r\n");
	Response.Write("</tr>\r\n");

	int rows = ds.Tables["qty"].Rows.Count;
	bool bAlterColor = false;
	for(int i=0; i<rows; i++)
	{
		DataRow dr = ds.Tables["qty"].Rows[i];
		string id = dr["id"].ToString();
		string factor_id = dr["factor_id"].ToString();
		string qty = dr["data2"].ToString();
		string margin = dr["data1"].ToString();
		string note = dr["note"].ToString();
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");

		if(m_id == id)
		{
			Response.Write("<form action=discount.aspx method=post>");
			Response.Write("<input type=hidden name=id value=" + m_id + ">");
			Response.Write("<input type=hidden name=factor value=qty>");
			Response.Write("<input type=hidden style='text-align:right'size=10 name=name value=''>");
			Response.Write("<td><input type=text style='text-align:right' size=5 name=factor_id value='" + factor_id + "'></td>");
			Response.Write("<td><input type=text style='text-align:right' size=15 name=data2 value='" + qty + "'></td>");
			Response.Write("<td><input type=text style='text-align:right' size=5 name=data1 value='" + margin + "'></td>");
			Response.Write("<td><input type=text name=note size=40 value='" + note + "'></td>");
			Response.Write("<td><input type=submit name=cmd value='OK'>");
			Response.Write("<input type=button onclick=window.location=('discount.aspx?r=" + DateTime.Now.ToOADate() + "') value='Cancel'></td>");
			Response.Write("</form>");
		}
		else
		{
			Response.Write("<td>" + factor_id + "</td>");
			Response.Write("<td>" + qty + "</td>");
			Response.Write("<td>" + margin + "</td>");
			Response.Write("<td>" + note + "</td>");
			Response.Write("<td align=right><a href=discount.aspx?id=" + id + " class=o>EDIT</a></td>");
		}
		Response.Write("</tr>");
	}
	Response.Write("");
	Response.Write("</table>");
}

bool DoUpdate()
{
	string id = Request.Form["id"];
	string factor = Request.Form["factor"];
	string factor_id = Request.Form["factor_id"];
	string name = Request.Form["name"];
	string data1 = Request.Form["data1"];
	string data2 = Request.Form["data2"];
	string note = Request.Form["note"];
	string sc = "UPDATE discount SET factor='" + factor + "', factor_id=" + factor_id + ", name='" + name;
	sc += "', data1=" + data1 + ", data2=" + data2 + ", note='" + EncodeQuote(note) + "' ";
	sc += " WHERE id=" + id;
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

</script>

<asp:Label id=LFooter runat=server/>
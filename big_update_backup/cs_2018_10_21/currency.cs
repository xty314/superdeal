<script runat=server>
DataSet dst = new DataSet();
DataSet dsi = new DataSet();

void Page_Load(Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("administrator"))
		return;

	string s_cmd = "";
	if(Request.Form["cmd"] != null)
	{
		s_cmd = Request.Form["cmd"];
		Trim(ref s_cmd);
	}

	if(s_cmd == "Add")
	{
		if(DoAddNewCurrency())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?" + "\">");
		return;
	}
	else if(s_cmd =="Modify")
	{
		if(UpdateCurrency())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=m&i=" + Request.Form["id"] + "\">");
		return;
	}
	else if(s_cmd =="Delete")
	{
		if(DeleteCurrency())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?" + "\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><hr3><center><b><font size=+1>Currency List</font></b></center></h3>");
	Response.Write("<table width=90%  align=center valign=top cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td width=60% align=center valign=top>");
	LoadExistingBranch();
	Response.Write("</td><td width=40% align=center valign=top>");

	PrintOneCurrency();

	Response.Write("</td></tr></table>");

	PrintAdminFooter();
}

bool DoAddNewCurrency()
{
	string id = "";
	string currency = Request.Form["currency"];
	string rates = Request.Form["rates"];
	string comments = Request.Form["comments"];
	
	if(currency.Length >= 254)
		currency = currency.Substring(0, 254);
	try
	{
		rates = (double.Parse(rates)).ToString();
	}
	catch(Exception e)
		{
		rates = "1";
		}
	string sc = " IF NOT EXISTS(SELECT * FROM currency WHERE currency_name='" + EncodeQuote(currency) + "') ";
	sc += " INSERT INTO currency (currency_name, rates, insert_by, insert_date, comments, gst_rate) ";
	sc += " VALUES( ";
	sc += "'" + EncodeQuote(currency) + "' ";
	sc += ", '" + EncodeQuote(rates) + "' ";
	sc += ", "+ Session["card_id"].ToString() +" ";
	sc += ", GETDATE() ";
	sc += ", '" + EncodeQuote(comments) + "' ";	
	sc += ", 0.125 ";	
	sc += ") ";
//DEBUG("sc =", sc);
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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

bool UpdateCurrency()
{
	string id = Request.Form["id"];
	string currency = Request.Form["currency"];
	string rates = Request.Form["rates"];
	string gst_rate = Request.Form["gst_rate"];
	string comments = Request.Form["comments"];
	if(currency.Length >= 255)
		currency = currency.Substring(0, 254);
	
	try
	{
		rates = (double.Parse(rates)).ToString();
	}
	catch(Exception e)
	{
		rates = "1";
	}
	try
	{
		gst_rate = (double.Parse(gst_rate) / 100).ToString();
	}
	catch(Exception e)
	{
		gst_rate = "0";
	}
	if(id == null || id == "")
	{
		Response.Write("<br><center><h3>Error, no ID</h3>");
		return false;
	}
	
	string sc = "UPDATE currency SET ";
	sc += " currency_name='" + EncodeQuote(currency) + "' ";
	sc += ", rates = '" + rates + "' ";
	sc += ", gst_rate = '"+ gst_rate +"' ";
	sc += ", comments = '" + EncodeQuote(comments) + "' ";	
	sc += " WHERE id = " + id;

	sc += " UPDATE settings SET value = "+ rates +" WHERE name = 'exchange_rate_"+ currency +"' ";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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

bool DeleteCurrency()
{
	string id = Request.Form["id"];
	if(id == null || id == "")
	{
		Response.Write("<br><center><H3>Error, no id");
		return false;
	}

	string sc = "DELETE FROM currency WHERE id = " + id;
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
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

void LoadExistingBranch()
{
	if(!GetExistingCurrency())
		return;
	
	Response.Write("<table width=100% valign=top cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<th width=5%>ID</th>");
	Response.Write("<th nowrap>Currency Name</th>");
	Response.Write("<th>Rate</th>");
	Response.Write("<th>Created By</th>");
	Response.Write("<th>Created Date</th>");
	Response.Write("<th>Comments</th>");
	Response.Write("<th>&nbsp;</th>");
	Response.Write("</tr>\r\n");

	for(int i=0; i<dst.Tables["currency"].Rows.Count;i++)
	{
		DataRow dr = dst.Tables["currency"].Rows[i];
		string id = dr["id"].ToString();
		string currency = dr["currency_name"].ToString();
		string rates = dr["rates"].ToString();
		string insert_by = dr["insert_by"].ToString();
		string insert_date = dr["insert_date"].ToString();
		string comments = dr["comments"].ToString();
		Response.Write("<tr>");
		Response.Write("<td>" + id + "</td>");
		Response.Write("<td>" + currency + "</td>");
		Response.Write("<td>" + rates + "</td>");
		Response.Write("<td>" + insert_by + "</td>");
		Response.Write("<td>" + insert_date + "</td>");
		Response.Write("<td>" + comments + "</td>");
		Response.Write("<td align=right><a href=?t=m&i=" + id + " class=o>Edit</a></td>");
		Response.Write("</tr>\r\n");
	}
	Response.Write("</table>");

	return;
}

void PrintOneCurrency()
{
	string id = "";
	string currency = "";
	string rates = "1";
	string comments = "";
	string insert_by = "";
	string insert_date = "";
	string gst_rate = "0";
	string s_TblName = "Add";

	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "")
	{
		s_TblName = "Modify";
		if(Request.QueryString["i"] != null && Request.QueryString["i"] != "")
		{
			if(!GetSelectedRow())
				return;
			DataRow dr = dsi.Tables["selected"].Rows[0];
			id = dr["id"].ToString();
			rates = dr["rates"].ToString();
			gst_rate = dr["gst_rate"].ToString();
			if(gst_rate == "" || gst_rate == null)
				gst_rate = "0";
			currency = dr["currency_name"].ToString();
			insert_by = dr["insert_by"].ToString();
			insert_date = dr["insert_date"].ToString();
			comments = dr["comments"].ToString();			
		}
	}
	Response.Write("<form name=frmAdd method=post action=currency.aspx>");
	Response.Write("<table width=95% valign=top align=right cellspacing=1 cellpadding=1 border=1 bordercolor=#000000 ");
	Response.Write(" style=\"border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#444444;font-weight:bold;\">");
	Response.Write("<td colspan=2 align=center><b>" + s_TblName + " Currency</b></td></tr>");

	Response.Write("<tr align=left><td width=30% align=left bgcolor=#DDDDDD><b>Currency:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=currency value='");
	Response.Write(currency + "'> </td></tr>\r\n");

	Response.Write("<script");
	Response.Write(">");
	Response.Write("document.frmAdd.name.focus();");
	Response.Write("</script");
	Response.Write(">");

	Response.Write("<tr align=left><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Rate:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=rates value='");
	Response.Write(rates + "'> </td></tr>\r\n");
	Response.Write("<tr align=left><td width=30% align=left bgcolor=#DDDDDD nowrap><b>GST Rate:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=10 name=gst_rate value='");
	Response.Write((double.Parse(gst_rate) * 100).ToString() + "'>%</td></tr>\r\n");
/*	Response.Write("<tr align=left><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Insert By:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=insert_by value='");
	Response.Write(insert_by + "'></td></tr>\r\n");
	Response.Write("<tr align=left><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Insert Date:</b></td>");
	Response.Write("<td width=70% align=center><input type=editbox size=30 name=insert_date value='");
	Response.Write(insert_date + "'></td></tr>\r\n");
*/
	Response.Write("<tr align=left><td width=30% align=left bgcolor=#DDDDDD nowrap><b>Comments:</b></td>");
	Response.Write("<td width=70% align=center><textarea rows=8 cols=45 name=comments>");
	Response.Write(comments + "</textarea></td></tr>\r\n");

	Response.Write("<tr align=left><td colspan=2 bgcolor=#FFFFFF align=center><br>");
	Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' " + s_TblName + " ' " + Session["button_style"] +">");

	//Response.Write("&nbsp;&nbsp;&nbsp");
	Response.Write("<input type=button style='font-size:8pt;font-weight:bold' name=clear value=' Cancel '");
	Response.Write(" " + Session["button_style"] +" OnClick=window.location=('currency.aspx')>");
	
	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "" && Request.QueryString["i"].ToString() != "1")
		Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' Delete ' " + Session["button_style"] +" onclick=\"if(!confirm('Are you sure to delete this currency!!!')){return false;}\">");
//	if(Request.QueryString["t"] == "m" && Request.QueryString["i"] != null && Request.QueryString["i"] != "" && Request.QueryString["i"].ToString() != "1")
	{
//		Response.Write("&nbsp;&nbsp;&nbsp");
//		Response.Write("<input type=submit style='font-size:8pt;font-weight:bold' name=cmd value=' Delete ' " + Session["button_style"] +" onclick=\"if(!window.confirm('Are you sure to do this')){return false;}\">");
		Response.Write("<input type=hidden name=id value='" + Request.QueryString["i"] + "'>");
	}
	
	Response.Write("</td></tr>");
	Response.Write("</table></form>");

	return;
}

bool GetSelectedRow()
{
	string sc = "SELECT gst_rate, * FROM currency WHERE id = " + Request.QueryString["i"];
	int rows = 0;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dsi, "selected");
	}
	catch(Exception e) 
	{
		if((e.ToString()).IndexOf("Invalid column name 'gst_rate'")>= 0)
		{
			sc = @" Alter table currency ADD gst_rate [float] NOT NULL Default (12.5) ";
				try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
				ShowExp(sc, er);
				return false;
			}
		}
		ShowExp(sc, e);
		return false;
	}

	if(rows != 1)
		return false;

	return true;
}

bool GetExistingCurrency()
{
	string sc = "SELECT c.name AS insert_by, y.* FROM currency y LEFT OUTER JOIN card c ON c.id = y.insert_by ORDER BY y.id";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "currency");
	}
	catch(Exception e) 
	{
		string err = e.ToString().ToLower();
		if(err.IndexOf("invalid object name 'currency'") >= 0)
		{
			myConnection.Close(); //close it first

			string ssc = @"
				
			CREATE TABLE [dbo].[currency](
			[id] [int] IDENTITY(1,1) NOT NULL,
			[currency_name] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[rates] [float] NOT NULL CONSTRAINT [rates]  DEFAULT (1),
			[gst_rate] [float] NOT NULL CONSTRAINT [rates]  DEFAULT (12.5),
			[insert_by] [int] NOT NULL,
			[insert_date] [datetime] NOT NULL CONSTRAINT [insert_date]  DEFAULT (getdate()),
			[comments] [varchar](4068) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
			) ON [PRIMARY]

			";
///		DEBUG("ssc = ", ssc);
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception er)
			{
				ShowExp(sc, er);
				return false;
			}
			try
			{
				
				string sqlString = " INSERT INTO currency (currency_name, rates, insert_by, insert_date, gst_rate) VALUES('NZD', 1, 0, GETDATE(), 0.125 ) ";
				myCommand = new SqlCommand(sqlString);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch
			{
				return false;
			}
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +" \">");		
		}
		
		ShowExp(sc, e);
		return false;
	}
	return true;
}

</script>
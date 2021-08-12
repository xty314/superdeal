<script runat=server>

string m_cardID = "";
string m_months = "0";
double m_dOpenBalance = 0;

DataSet dst = new DataSet();

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	if(Request.QueryString["cid"] != null)
		m_cardID = Request.QueryString["cid"];
	if(Request.QueryString["m"] != null)
		m_months = Request.QueryString["m"];

	PrintAdminHeader();
	PrintAdminMenu();

	if(DoTransSearch())
	{
		BindData();
	}

	PrintAdminFooter();
}

bool DoTransSearch()
{
	int rows = 0;

	//get opening balance
	string sc = " SELECT SUM(total - amount_paid) AS balance ";
	sc += " FROM invoice ";
	sc += " WHERE card_id = " + m_cardID + " AND paid = 0 ";
	sc += " AND DATEDIFF(month, commit_date, GETDATE()) > " + m_months;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "ob") == 1)
			m_dOpenBalance = MyDoubleParse(dst.Tables["ob"].Rows[0]["balance"].ToString());
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	sc = " SELECT 'IN' AS rtype, i.invoice_number AS number, o.po_number AS ref, i.total AS amount ";
	sc += ", (i.total - i.amount_paid) AS balance, i.commit_date AS record_date ";
	sc += " FROM invoice i JOIN orders o ON o.invoice_number = i.invoice_number";
	sc += " WHERE i.card_id = " + m_cardID;
	sc += " AND DATEDIFF(month, i.commit_date, GETDATE()) = " + m_months;
	sc += " ORDER BY i.commit_date DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "list");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	sc = " SELECT 'CR' AS rtype, t.id AS number, t.amount AS amount, d.trans_date AS record_date, '' AS ref, 0 AS balance ";
	sc += " FROM trans t JOIN tran_detail d ON d.id = t.id ";
	sc += " WHERE d.card_id = " + m_cardID;
	sc += " AND DATEDIFF(month, d.trans_date, GETDATE()) = " + m_months;
	sc += " ORDER BY d.trans_date DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "list");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

void BindData()
{
	DateTime dcur = DateTime.Now.AddMonths(0 - MyIntParse(m_months));
	DateTime dpre = dcur.AddMonths(-1);
	DateTime dnext = dcur.AddMonths(1);
	string scur = dcur.ToString("MMM-yyyy");
	string spre = dpre.ToString("MMM-yyyy");
	string snext = dnext.ToString("MMM-yyyy");
	string date_string = "<a href=tranlist.aspx?cid=" + m_cardID + "&m=" + (MyIntParse(m_months) + 1).ToString() + " class=o>" + spre + "</a>";
	date_string += "&nbsp;&nbsp;<b>" + scur + "</b>&nbsp;&nbsp;";
	date_string += "<a href=tranlist.aspx?cid=" + m_cardID + "&m=" + (MyIntParse(m_months) - 1).ToString() + " class=o>" + snext + "</a>";


	DataRow[] drs = dst.Tables["list"].Select("", "record_date");
	Response.Write("<br><center><h4>Transaction List</h4>");
	Response.Write("<table width=100% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	Response.Write("<tr><td colspan=8 align=center>" + date_string + "</td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#80db80;font-weight:bold;\">");
	Response.Write("<th>Date</th><th>Type</th><th>Our Reference</th><th>Your Reference</th><th>Debit</th><th>Credit</th><th>Balance</th><th>Cumulative</th>");
	Response.Write("</tr>");

	bool bAlterColor = false;
	double dTotal = m_dOpenBalance;
//	Response.Write("<tr><td colspan=7 align=right>Balance carried from last month</td><td align=right>" + dTotal.ToString("c") + "</td></tr>");
	for(int i=0; i<drs.Length; i++)
	{
		DataRow dr = drs[i];
		string date = DateTime.Parse(dr["record_date"].ToString()).ToString("dd-MM-yyyy");
		string type = dr["rtype"].ToString();
		string our_ref = dr["number"].ToString();
		string your_ref = dr["ref"].ToString();
		double dAmount = MyDoubleParse(dr["amount"].ToString());
		string debit = dAmount.ToString("c");
		string credit = "";
		if(type == "CR")
		{
			credit = debit;
			debit = "";
			Trim(ref our_ref);
			our_ref = "<a href=payhistory.aspx?t=p&id=" + our_ref + " target=new class=o>" + our_ref + "</a>";
		}
		double dBalance = MyDoubleParse(dr["balance"].ToString());
		dTotal += dBalance;
//		if(type == "CR")
//			dTotal -= dAmount;

		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");
		bAlterColor = !bAlterColor;

		Response.Write("<td>" + date + "</td>");
		Response.Write("<td>" + type + "</td>");
		Response.Write("<td>" + our_ref + "</td>");
		Response.Write("<td>" + your_ref + "</td>");
		Response.Write("<td align=right>" + debit + "</td>");
		Response.Write("<td align=right>" + credit + "</td>");
		Response.Write("<td align=right>" + dBalance.ToString("c") + "</td>");
		Response.Write("<td align=right>" + dTotal.ToString("c") + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("<tr><td colspan=7 align=right><b>Balance Due</b></td><td align=right>" + dTotal.ToString("c") + "</td></tr>");
	Response.Write("</table>");
}
</script>
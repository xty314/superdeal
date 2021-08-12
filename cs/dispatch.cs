<!-- #include file="purchase_function.cs" -->
<!-- #include file="fifo_f.cs" -->

<script runat="server">

DataSet dst = new DataSet();
string m_poID = "";
int m_nItems = 0;
int m_nBranches = 1;
string[] m_aBranchName = new String[64];
string[] m_aBranchID = new String[64];
int[] m_aQty = new int[64];
string m_type = "";
int m_nYourBranchID = 1;
string tableWidth = "97%";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;

	if(Session["branch_id"] != null && Session["branch_id"] != "")
		m_nYourBranchID = int.Parse(Session["branch_id"].ToString());
	m_poID = Request.QueryString["n"];
	if(m_poID == null || m_poID == "")
	{
		MsgDie("no id");
		return;
	}
	if(Request.QueryString["t"] != null)
		m_type = Request.QueryString["t"];

	if(!GetPurchaseItems())
		return;

	if(!BuildBranchIndex())
		return;

	if(Request.Form["cmd"] == "Dispatch")
	{
		if(DoDispatch())
		{
			Response.Write("<meta meta http-equiv=\"refresh\" content=\"0; URL=dispatch.aspx?t=done&n=" + m_poID + "\">");
			return;
		}
	}
	else if(Request.Form["cmd"] == "Update")
	{
		if(DoUpdateDispatch())
		{
			Response.Write("<meta meta http-equiv=\"refresh\" content=\"0; URL=dispatch.aspx?t=done&n=" + m_poID + "\">");
			return;
		}
	}
	PrintAdminHeader();
	PrintAdminMenu();

	if(Request.QueryString["t"] == "done")
	{
		Response.Write("<br><center><h5>Done. Please wait a second .. </h5>");
		Response.Write("<meta meta http-equiv=\"refresh\" content=\"3; URL=dispatch.aspx?n=" + m_poID + "\">");
		return;
	}
	else
		DrawDispatchTable();

	PrintAdminFooter();
}

bool GetPurchaseItems()
{
	string sc = " SELECT * FROM purchase_item WHERE id = " + m_poID;

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "items") <= 0)
		{
			MsgDie("no items found");
			return false;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool DrawDispatchTable()
{
/*	Response.Write("<center><h3>Dispatch Purchase <font color=red>#" + m_poID + "</font></h3>");
	Response.Write("<table align=center valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
*/
	int ncol = 4 + m_nBranches;
	Response.Write("<form name=f action=?n=" + m_poID + " method=post>");
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>Dispatch Purchase <font color=red>#" + m_poID + "</b><font color=red><b>");		
	Response.Write("</td><td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan='"+ ncol +"'><br></td></tr>");

	Response.Write("<tr><td colspan=4>&nbsp;</td><th colspan=" + m_nBranches + ">BRANCH DISPATCH</th></tr>");
	Response.Write("<tr bgcolor=#EEEEE>");
	Response.Write("<th>ITEM CODE</th>");
	Response.Write("<th align=left>SUPPLIER CODE</th>");
	Response.Write("<th width=35% align=left>DESCRIPTION</th>");
	Response.Write("<th width=55>QTY</th>");
	Response.Write("<input type=hidden name=hidTBranch value="+ m_nBranches +">");
	for(int i=0; i<m_nBranches; i++)
	{	
//		Response.Write("<th> ");
       int rows = dst.Tables["items"].Rows.Count ;
		
		Response.Write("<input type=hidden name=hidBranch value='"+ m_aBranchID[i] +"'>");
		//Response.Write("<th><input type=radio name='branch' onclick=\"fillAllRows("+ m_aBranchID[i]+", "+ dst.Tables["items"].Rows.Count +");\" >");
		Response.Write("<th><input type=radio name='branch' onclick=\"fillAllRows("+ m_aBranchID[i]+", "+rows+");\" >");
		Response.Write(" " + m_aBranchName[i] + " &nbsp; </th>");
	}
	Response.Write("</tr>");
	Response.Write("<script");
	Response.Write(">");
	string ss = @"
		function fillAllRows(branch, itemrows)
		{						
			var restBranch = new Array();
			for(n=0;n<Number(document.f.hidTBranch.value); n++)
			{
				restBranch[n] = document.f.hidBranch[n].value;
	
				for(i=0;i<itemrows; i++)
				{
					var hkid = document.f.hidKID[i].value + branch;	
					var hqty = document.f.hidQTY[i].value;					
					if(branch == restBranch[n])
					{
		";		
						ss += " eval(\"document.f.qty\" + hkid + \".value = hqty \"); ";				
						ss += @"	
					}	
					else
					{				
						hkid = document.f.hidKID[i].value + restBranch[n];	
				";
					ss += " eval(\"document.f.qty\" + hkid + \".value = 0 \"); ";
					ss += @" 
					}
				}
			}
		}
		";
	
	Response.Write(ss);
	Response.Write("</script");
	Response.Write(">");

	bool bDispatched = false;
	bool bAllDispatched = true;
	m_nItems = dst.Tables["items"].Rows.Count;
	int nTotalReceived = 0;
	int nTotalDispatched = 0;
	for(int i=0; i<dst.Tables["items"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["items"].Rows[i];
		string kid = dr["kid"].ToString();	
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();

		Response.Write("<input type=hidden name=hidKID value='"+ kid +"'>");
		Response.Write("<input type=hidden name=hidQTY value='"+ qty +"'>");

		bDispatched = MyBooleanParse(dr["dispatched"].ToString());
		string sdispatched = "0";
		if(bDispatched && m_type != "e")
			sdispatched = "1";
		else
			bAllDispatched = false;
	/*	Response.Write("<input type=hidden name=code" + i + " value=" + code + ">");
		Response.Write("<input type=hidden name=total" + i + " value=" + qty + ">");
		Response.Write("<input type=hidden name=dispatched" + i + " value=" + sdispatched + ">");
*/
		Response.Write("<tr><td align=center>" + code + "</td><td>" + dr["supplier_code"].ToString() + "</td><td>" + name + "</td><td align=center>" + qty + "</td>");
		for(int m=0; m<m_nBranches; m++)
		{
			int dqty = 0;

			//***** get dispatch and receive qty ****//
			string dispatched_qty = GetDispatchedQty(kid, m_aBranchID[m]);
			string received_qty = GetReceivedQty(kid, m_aBranchID[m]);				
			nTotalReceived += int.Parse(received_qty);
			nTotalDispatched += int.Parse(dispatched_qty);
			//***** get dispatch and receive qty END ****//

			string qid = kid + m_aBranchID[m];		
			if(Session["dispatch" + qid] != null && Session["dispatch" + qid] != "")
				dqty = MyIntParse(Session["dispatch" + qid].ToString());
			if(!bDispatched || m_type == "e")
			{
				if(m_type == "e")
					dqty = MyIntParse(GetDispatchedQty(kid, m_aBranchID[m]));
				Response.Write("<td align=center>");
					//set the value to auto fill the qty...
				if(m == (m_nYourBranchID - 1))
				{	
					string rc_qty = GetReceivedQty(kid, m_aBranchID[m]);
					if(int.Parse(rc_qty) > 0)
						dqty = int.Parse(qty) - int.Parse(GetReceivedQty(kid, m_aBranchID[m]));
					else if(m_type == "e")
						dqty = dqty;
					else
						dqty = int.Parse(qty);
				}				
			
				Response.Write("<input type=text style='text-align:right' name=qty" + qid + " size=1 value=" + dqty + " > Received: <b>"+ received_qty +" </b>");
				//onchange=\"jsCalStockQty(this.value, "+ qid +", "+ kid +", "+ m_nBranches +");\"
//				Response.Write("<input type=text style=text-align:right name="+ i +""+ m +" size=1 value=" + dqty + ">");
			}
			else
			{
				//string dispatched_qty = GetDispatchedQty(kid, m_aBranchID[m]);
				//string received_qty = GetReceivedQty(kid, m_aBranchID[m]);
				string sq = dispatched_qty;// + "/" + received_qty;
				string stitle = "dispatched " + dispatched_qty + " / received " + received_qty;
				//nTotalReceived += int.Parse(received_qty);
				//nTotalDispatched += int.Parse(dispatched_qty);
				if(sq == "0")
				{
					sq = "";
					stitle = "no dispatch";
				}
				else if(dispatched_qty == received_qty)
					sq = dispatched_qty + " (Received)";
				else
					sq = "<font color=red>" + dispatched_qty + "/"+ received_qty +"</font>";
				Response.Write("<td align=center title='" + stitle + "'>");
				Response.Write(sq);
				
				
//				string dispatched_qty = GetDispatchedQty(kid, m_aBranchID[m]);
//				Response.Write(dispatched_qty);
			}
			Response.Write("</td>");
			Response.Write("<input type=hidden name=qid" + i + m + " value=qty" + qid + ">");
		}		
		Response.Write("<input type=hidden name=code" + i + " value=" + code + ">");
		Response.Write("<input type=hidden name=total" + i + " value=" + (int.Parse(qty) - nTotalReceived) + ">");
		Response.Write("<input type=hidden name=dispatched" + i + " value=" + sdispatched + ">");

		Response.Write("</tr>");
	}
string js = @"

<script language='javascript' TYPE='text/javascript'>
function jsCalStockQty(enterValue, rowID, kID, totalBranch)
{
//	window.alert(totalBranch);
//
//	window.alert(enterValue +' = '+  rowID);
}
</script";
js += ">\r\n";

Response.Write(js);

//	Response.Write("</table>");
	Response.Write("<tr align=center ><td colspan='"+ ncol +"'>");
	if(!bAllDispatched && m_type != "e")
		Response.Write("<input type=submit name=cmd value=Dispatch  "+ Session["button_style"] +"  onclick=\"if(!CheckQty())return false;\">");
	else if(bAllDispatched)
	{
		Response.Write("<input type=button ");
		if(nTotalReceived > 0 )
			Response.Write(" disabled ");
		Response.Write("value=' &nbsp; Edit &nbsp; '  "+ Session["button_style"] +"  onclick=\"window.location=('dispatch.aspx?n=" + m_poID + "&t=e');\">");
	}
	else
	{
		Response.Write("<input type=submit name=cmd value=Update  "+ Session["button_style"] +"  onclick=\"if(!CheckQty())return false;\">");
		Response.Write("<input type=button value=Cancel  "+ Session["button_style"] +"  onclick=\"window.location='dispatch.aspx?n=" + m_poID + "'\">");
	}
	Response.Write("<input type=button onclick=window.location=('purchase.aspx?n=" + m_poID + "') value='Back to Purchase Order'  "+ Session["button_style"] +" >");
	Response.Write("</td></tr></table>");
	Response.Write("</form>");

	PrintJavaFunction();
	return true;
}

string GetReceivedQty(string kid, string branch_id)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT isnull(sum(qty), 0) AS qty  FROM dispatch WHERE id = " + kid + " AND branch = " + branch_id + " AND received = 1 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	return dsd.Tables[0].Rows[0]["qty"].ToString();
}

string GetDispatchedQty(string kid, string branch_id)
{
	DataSet dsd = new DataSet();
	string sc = " SELECT isnull(sum(qty), 0) AS qty FROM dispatch WHERE id = " + kid + " AND branch = " + branch_id;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dsd) <= 0)
		{
			return "0";
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "0";
	}
	return dsd.Tables[0].Rows[0]["qty"].ToString();
}

bool PrintJavaFunction()
{
	Response.Write("<script language=javascript> \r\n");
	
	Response.Write("function CheckQty() \r\n");
	Response.Write("{ \r\n");
		Response.Write("for(var i=0; i<" + m_nItems + "; i++) \r\n");
		Response.Write("{ \r\n");
			Response.Write("if(eval(\"document.f.dispatched\" + i + \".value\") == '1')continue;");
			Response.Write("var code = eval(\"document.f.code\" + i + \".value\"); \r\n");
			Response.Write("var total = eval(\"Number(document.f.total\" + i + \".value)\"); \r\n");
			Response.Write("var qty = 0; \r\n");
			Response.Write("for(var m=0; m<" + m_nBranches + "; m++) \r\n");
			Response.Write("{ \r\n");
				Response.Write("var qid = eval(\"document.f.qid\" + i + m + \".value\"); \r\n");
				Response.Write("qty += eval(\"Number(document.f.\" + qid + \".value)\"); \r\n");
			Response.Write("} \r\n");
			Response.Write("if(total != qty) \r\n");
			Response.Write("{ \r\n");
				Response.Write("window.alert('Error, dispatched qty not equals total qty on code : ' + code + '.'); \r\n");
				Response.Write("return false; \r\n");
			Response.Write("} \r\n");
		Response.Write("} \r\n");
		Response.Write("return true; \r\n");
	Response.Write("} \r\n");

	Response.Write("</script");
	Response.Write("> \r\n");
	return true;
}

bool BuildBranchIndex()
{
	DataSet dsb = new DataSet();

	string sc = "SELECT id, name FROM branch WHERE activated = 1 ORDER BY id";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_nBranches = myCommand.Fill(dsb, "branch");
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("Invalid column name 'activated'") >= 0)
		{
			sc = @"
				alter table branch ADD activated [bit] not null default(1) 
				";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e2) 
			{
				ShowExp(sc, e2);				
			}
		}
		ShowExp(sc, e);
		return false;
	}
	
	for(int i=0; i<m_nBranches; i++)
	{
		string name = dsb.Tables["branch"].Rows[i]["name"].ToString();
		string id = dsb.Tables["branch"].Rows[i]["id"].ToString();
		m_aBranchName[i] = name;
		m_aBranchID[i] = id;
	}
	return true;
}

bool DoDispatch()
{
	string sc = "";
	for(int i=0; i<dst.Tables["items"].Rows.Count; i++)
	{
		if(Request.Form["dispatched" + i] == "1")
			continue;
		DataRow dr = dst.Tables["items"].Rows[i];
		string kid = dr["kid"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		int nQty = MyIntParse(qty);

		for(int m=0; m<m_nBranches; m++)
		{
			string qid = "qty" + kid + m_aBranchID[m];
			m_aQty[m] = MyIntParse(Request.Form[qid]);
			Session["dispatch" + qid] = m_aQty[m];
			if(m_aQty[m] == 0)
				continue;
			sc += " INSERT INTO dispatch (id, branch, code, name, qty) ";
			sc += " VALUES(";
			sc += kid;
			sc += ", " + m_aBranchID[m];
			sc += ", " + code;
			sc += ", '" + EncodeQuote(name) + "' ";
			sc += ", " + m_aQty[m];
			sc += ") ";
			sc += " UPDATE purchase_item SET dispatched = 1 WHERE kid = " + kid;
		}
	}
	
	sc += " UPDATE purchase SET status = 2 WHERE id = " + m_poID;
//DEBUG("sc=", sc);
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

bool DoUpdateDispatch()
{
	string sc = "BEGIN TRANSACTION ";
	for(int i=0; i<dst.Tables["items"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["items"].Rows[i];
		string kid = dr["kid"].ToString();
		string code = dr["code"].ToString();
		string name = dr["name"].ToString();
		string qty = dr["qty"].ToString();
		int nQty = MyIntParse(qty);

		for(int m=0; m<m_nBranches; m++)
		{
			string qid = "qty" + kid + m_aBranchID[m];
			m_aQty[m] = MyIntParse(Request.Form[qid]);
			Session["dispatch" + qid] = m_aQty[m];
//			if(m_aQty[m] == 0)
//				continue;
			if(m_aQty[m] == 0)
			{
				sc += " DELETE FROM dispatch WHERE id = "+ kid +" AND received = 0 AND branch = "+ m_aBranchID[m] +"";
			}
			else
			{
				sc += " IF (SELECT qty FROM dispatch WHERE id = "+ kid +" AND branch = "+ m_aBranchID[m] +" AND received = 0) IS NULL";
				sc += " BEGIN ";
				sc += " INSERT INTO dispatch (id, branch, code, name, qty) ";
				sc += " VALUES( "+ kid +", "+ m_aBranchID[m] +", "+ code +", '"+ name +"', "+ m_aQty[m] +" ) ";
				sc += " END ";
				sc += " ELSE ";
				sc += " BEGIN ";
				sc += " UPDATE dispatch SET qty = " + m_aQty[m];
				sc += " WHERE id = " + kid + " AND branch = " + m_aBranchID[m];
				sc += " AND received = 0 ";
				sc += " END ";			
			}
//			sc += " UPDATE purchase_item SET dispatched = 1 WHERE kid = " + kid;
		}
	}
	sc += " COMMIT ";
//	sc += " UPDATE purchase SET status = 2 WHERE id = " + m_poID;
//DEBUG("sc=", sc);Response.End();
//return false;
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
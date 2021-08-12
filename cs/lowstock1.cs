<!-- #include file="page_index.cs" -->
<!-- #include file="purchase_function.cs" -->
<script runat=server>

string m_branchID = "";
string m_type = "";
string m_tableTitle = "Low Stock Item List";
string[] m_aBranchID = new string[16];
string[] m_aBranchName = new string[16];
int m_nBranches = 0;
string tableWidth = "97%";

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
	
	if(Session["branch_support"] != null)
	{
		if(Request.Form["branch"] != null)
			m_branchID = Request.Form["branch"];
		else if(Request.QueryString["branch"] != null && Request.QueryString["branch"] != "")
			m_branchID = Request.QueryString["branch"];
		else if(Session["branch_id"] != null)
			m_branchID = Session["branch_id"].ToString();
	}
	
	if(Request.QueryString["t"] == "p")
	{
		string code = Request.QueryString["c"];
		string qty = Request.QueryString["q"];
		DataRow drp = null;
		if(!GetProduct(code, ref drp))
			return;
		string supplier = drp["supplier"].ToString();
		string supplier_code = drp["supplier_code"].ToString();
		string foreign_supplier_price = drp["foreign_supplier_price"].ToString();
		AddToCart(code, supplier, supplier_code, qty, foreign_supplier_price);
		Session["purchase_need_update" + m_ssid] = true; //for update order first
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=purchase.aspx?ssid=" + m_ssid + "\">");
		return;
	}
	
	if(!GetData())
		return;
	if(!GetAllBranchNames())
		return;
	PrintList();
}

bool GetData()
{
	string  gCode = Request.QueryString["lowstockcode"];
	string  gSpecialItem = Request.QueryString["g"];
    string sc = " SELECT q.code,(sum(q.qty) - c.low_stock) AS low ";
	sc += " FROM stock_qty q JOIN code_relations c ON c.code = q.code ";
    //sc += "WHERE c.low_stock <> 0 AND q.qty = c.low_stock ";
	sc += " WHERE  c.low_stock <> 0 AND (SELECT sum(qty) FROM stock_qty WHERE code = c.code) < c.low_stock "; // Get All branches stock total compare with low stock warning CH 24.05.08
	if(gSpecialItem =="1")
	sc += " AND c.code='"+gCode+"'";

	if(Session["branch_support"] != null)
	{
	}
	else
	{
		sc += " AND q.branch_id = 1 ";
	}
	
	sc += " GROUP BY q.code, c.low_stock ";
	sc += " ORDER BY low DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "low");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	string codes = "";
	for(int i=0; i<ds.Tables["low"].Rows.Count; i++)
	{
		int nLow = MyIntParse(ds.Tables["low"].Rows[i]["low"].ToString());
		if(nLow >= 0)
			continue;
		if(codes != "")
			codes += ",";
		codes += ds.Tables["low"].Rows[i]["code"].ToString();
	}

	if(codes == "")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><center><h3>There is no low stock items</h3>");
		
		return false;
	}

	sc = " SELECT c.name, c.barcode, c.low_stock, q.code, q.branch_id, q.qty";
	sc += " FROM stock_qty q JOIN code_relations c ON c.code = q.code ";
	sc += " WHERE q.code IN (" + codes + ") ";
	if(Session["branch_support"] != null)
	{
	}
	else
	{
		sc += " AND q.branch_id = 1 ";
	}
	sc += " ORDER BY q.code, q.branch_id ";
//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "report");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetAllBranchNames()
{
	string sc = " SELECT * FROM branch WHERE activated = 1 ";
	if(Session["branch_support"] == null)
		sc += " AND id = 1 ";

	sc += " ORDER BY id ";
//DEBUG(" sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nBranches = myAdapter.Fill(ds, "branches");
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

	for(int i=0; i<m_nBranches && i<16; i++)
	{
		DataRow dr = ds.Tables["branches"].Rows[i];
		string bid = dr["id"].ToString();
		string bname = dr["name"].ToString();
		m_aBranchID[i] = bid;
		m_aBranchName[i] = bname;
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void PrintList()
{
	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>"+ m_tableTitle +"</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td><br></td></tr>");
	Response.Write("<tr style=\"color:white;background-color:#0080db;font-weight:bold;\">");
	Response.Write("<th>Barcode</th>");
	Response.Write("<th>Description</th>");
	if(Session["branch_support"] != null)
	{
	for(int i=0; i<m_nBranches; i++)
		Response.Write("<th nowrap>" + m_aBranchName[i] + "</th>");
	}
/*	else
	{
		Response.Write("<td></td>");
	}
	*/
	Response.Write("<th>Total</th>");
	Response.Write("<th nowrap>Warning Value</th>");
	Response.Write("</tr>");
	int rows = ds.Tables["report"].Rows.Count;
	if(rows <= 0)
	{
		Response.Write("</table>");
		return;
	}

	string code = "";
	string code_old = "";
	string barcode = "";
	string name = "";
	//string low_stock = "";
	int low_stock = 0;
	int w_stock = 0;
	int nTotalQty = 0;
	int[] nQty = new int[16];
	bool bAlterColor = false;
	for(int i=0; i<=rows; i++)
	{
		DataRow dr = null;
		string code_curr = "";

		if(i < rows)
		{
			dr = ds.Tables["report"].Rows[i];
			code_curr = dr["code"].ToString();
			
		}
		if(code_curr != code)
		{
			if(code != "") //begin print one row
			{
				Response.Write("<tr");
				if(bAlterColor)
					Response.Write(" bgcolor=#EEEEEE");
				bAlterColor = !bAlterColor;
				Response.Write(">");
				Response.Write("<td>");
				if(SecurityCheck("manager"))
					Response.Write("<a href=liveedit.aspx?code=" + code + " target=_blank>");
				Response.Write(barcode);
				if(SecurityCheck("manager"))
					Response.Write("</a>");	
				Response.Write("</td>");
				Response.Write("<td>" + name + "</td>");
				if(Session["branch_support"] != null)
				{
				for(int n=0; n<m_nBranches; n++)
				{				
					int qty = nQty[MyIntParse(m_aBranchID[n])];
					Response.Write("<td align=center>");
					if(qty != 0)
						Response.Write(qty);

					else
						Response.Write("0");
					Response.Write("</td>");
					nTotalQty += qty;

				}
				}
				Response.Write("<td align=center>" + nTotalQty + "</td>");
				Response.Write("<td align=center>" + low_stock);
				//int p_stock = (w_stock - nTotalQty);
				//Response.Write("&nbsp;<input type=button onclick =\"window.open('lowstock.aspx?t=p&c="+code+"&q=1&ssid=" + m_ssid + "')\" value='Pur'></td>");
				Response.Write("</td></tr>");
			}
			if(i >= rows)
				break;
			code = code_curr;
			barcode = dr["barcode"].ToString();
			name = dr["name"].ToString();
			low_stock = MyIntParse(dr["low_stock"].ToString());
			for(int m=0; m<16; m++)
				nQty[m] = 0;
			nTotalQty = 0;
		}
		//else
		{
			int branch_id = MyIntParse(dr["branch_id"].ToString());
	//	DEBUG("branch =", branch_id);
			nQty[branch_id] = MyIntParse(dr["qty"].ToString());
		}
	}

	Response.Write("</table>");
}

</script>

<script runat="server">

string m_modurl = ""; 
string m_msg = "";

bool DisplaySNinput()
{
	if(Session["sales_current_quote_number"] == null)
		return false;

	m_quoteNumber = Session["sales_current_quote_number"].ToString();

	if(!matchingItem())
		return false;
	
	if(!getItemSNs())
		return false;

	if(Request.QueryString["mod"] != null)
		m_modurl = "&mod=e";
	
//DEBUG("before postback", 0);
	if(Request.Form["sInputSN"] != null)//Request.Form["sn_cmd"] == " OK ")
	{	
		if(Request.QueryString["mod"] != null)
		{
			if(!DoUpdateSN(Request.Form["sInputSN"], Request.Form["ref_id"]))
			{
				return false;
			}
			else
			{
				if(!getItemSNs())
					return false;
			}

			if(!getItemSNs())
				return false;
		}
		else
		{
			if(Request.Form["sInputSN"] != "" && Request.Form["sInputSN"] != null)
			{ 
				if(int.Parse(dst.Tables["matching"].Rows[0]["quantity"].ToString()) > dst.Tables["item_sn"].Rows.Count)
				{
					if(DoinsertSN(Request.Form["sInputSN"]))
					{
						m_msg = " * SN was Added Successfully!";
						if(!getItemSNs())
							return false;
					}
				}
				else
				{
					m_msg = " * SN was not Inserted! All items have SN."; 
				}
			}
		}
	}

	PrintAdminHeader();
	PrintAdminMenu();
	PrintHeaderAndMenu();

	//list all items in the cart
	PrintListTable();	
	PrintInputTable();

	PrintSearchForm();
	LFooter.Text = m_sAdminFooter;

	return true;
}

void PrintListTable()
{
	//dtCart  --- variable of shopping cart 
	string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	dtCart = (DataTable)Session["ShoppingCart" + ssid];

	Response.Write("<h3><center><font size=+1><b>Sales<font> --- Enter Serial Number</b></center></h3>");
	Response.Write("<br><table width=100% border=1 cellspacing=3 cellpadding=2>\r\n");
	Response.Write("<tr bgcolor=E3E3E3 height=30><td width=13% align=center>");
	Response.Write("<b>Product ID</b></td><td width=70% align=center><b>Description</b></td>");
	Response.Write("<td width=12% align=center><b>Retail Price</b></td>");
	Response.Write("<td width=5% align=center><b>Quantity</b></td></tr>\r\n");

	string sbold_op = "";
	string sbold_cl = "";

	int rows = dtCart.Rows.Count;
	for(int i=0; i <= rows-1; i++)
	{

		if(i.ToString() == Request.QueryString["rid"])
		{
			sbold_op = "<b>";
			sbold_cl = "</b>";
		}
		else
		{
			sbold_op = "";
			sbold_cl = "";
		}

		Response.Write("<tr><td>" + dtCart.Rows[i]["code"].ToString() + "</td>");		
		Response.Write("<td><a href=pos.aspx?i=" +m_quoteNumber+ "&a=sn&rid=" + i + ">");
		Response.Write(sbold_op + dtCart.Rows[i]["name"].ToString() + sbold_cl + "</a></td>");
		Response.Write("</td><td align=right>" + double.Parse(dtCart.Rows[i]["salesPrice"].ToString()).ToString("c"));
		Response.Write("<td align=center>" + dtCart.Rows[i]["quantity"].ToString() + "</td></tr>");
	}
	Response.Write("</table>");
	return;
}

void PrintInputTable()
{
	//dtCart  --- variable of shopping cart 
	string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	dtCart = (DataTable)Session["ShoppingCart" + ssid];

	string s_rowNo = Request.QueryString["rid"];
	if(m_modurl != "")
		m_modurl += "&l=" + Request.QueryString["l"];
	Response.Write("<br><br><form action=pos.aspx?i="+ m_quoteNumber +"&a=sn&rid=");
	Response.Write(s_rowNo + m_modurl + " method=post>");
	Response.Write("<table width=60% align=center border=1 bordercolor=4D4C49 cellspacing=0 cellpadding=0>\r\n");
	Response.Write("<tr bgcolor=FFFFFF height=25><td><b>&nbsp;&nbsp;Product ID #</b></td><td><font color=red><b>");
	Response.Write(dtCart.Rows[int.Parse(s_rowNo)]["code"].ToString() + "</b></font></td></tr>");
	Response.Write("<tr bgcolor=E3E3E3 height=25><td colspan=2>&nbsp;&nbsp;<b>");
	Response.Write(dtCart.Rows[int.Parse(s_rowNo)]["name"].ToString() + "</b></td></tr>");

	Response.Write("<tr><td valign=top>");
	
	//table of listing
	Response.Write("<table cellspacing=2 border=1 width=100%>");
	Response.Write("<tr bgcolor=E3E3E3><td>No.</td><td align=center>Serial No.</td></tr>");

	string s_snbgclr_op = "";
	string s_snbgclr_cl = "";
	string s_newsn = "";

	//if item's S/N was deleted in updating,
	bool b_Current = true;
	if(Request.QueryString["l"] != null)
	{
		if(dst.Tables["item_sn"].Rows.Count < int.Parse(Request.QueryString["l"].ToString()))
			b_Current = false;
	}

	for(int i = 0; i <= int.Parse(dst.Tables["matching"].Rows[0]["quantity"].ToString())-1; i++)
	{

		if((i+1).ToString() == Request.QueryString["l"])
		{
			s_snbgclr_op = " bgcolor=FAF986><b>";
			s_snbgclr_cl = "</b>";
			if(b_Current)
				s_newsn = dst.Tables["item_sn"].Rows[i]["sn"].ToString();
		}
		else
		{
			s_snbgclr_op = ">";
			s_snbgclr_cl = "";
		}
		Response.Write("<tr><td align=left>"+ (i+1).ToString() +"</td>");
		Response.Write("<td align=right"); //+ dst.Tables["item_sn"].Rows[i]["sn"].ToString() + "</td></tr>");

//DEBUG("rows count = ", dst.Tables["item_sn"].Rows.Count);
		if(dst.Tables["item_sn"].Rows.Count > i)
		{
			Response.Write(s_snbgclr_op);
			Response.Write("<a href=pos.aspx?i=" +m_quoteNumber+ "&a=sn&rid=" + s_rowNo);
			Response.Write("&mod=e&l=" + (i+1) + ">");	
			Response.Write(dst.Tables["item_sn"].Rows[i]["sn"].ToString() + s_snbgclr_cl + "</a>");
			
			if((i+1).ToString() == Request.QueryString["l"])
			{	
				Response.Write("<input type=hidden name=ref_id value=");
				Response.Write(dst.Tables["item_sn"].Rows[i]["id"].ToString() + ">");
				//Response.Write(dst.Tables["item_sn"].Rows[i]["id"].ToString());	#####testing... 
			}

			Response.Write("</td></tr>");
		}
		else
			Response.Write(">N/A</td></tr>");
	}

	Response.Write("</table>");
	Response.Write("</td>");

	//table of operations
	Response.Write("<td valign=top><table cellspacing=2 border=1 width=100%>");

	if(Request.QueryString["mod"] == "e")
	{
		Response.Write("<tr bgcolor=E3E3E3><td align=center>Modify S/N:</td>");
		Response.Write("<td><a href=pos.aspx?i=" +m_quoteNumber+ "&a=sn&rid=" + s_rowNo);
		Response.Write(">Input</a></td></tr>");
		Response.Write("<tr><td><input type=text size=15 name=sInputSN value='" + s_newsn + "'>&nbsp;&nbsp;");
		Response.Write("<input type=submit name=sn_cmd value=' OK '></td></tr>");
	}
	else
	{
		Response.Write("<tr bgcolor=E3E3E3><td align=center>Input S/N:</td>");
		Response.Write("<td><a href=pos.aspx?i=" +m_quoteNumber+ "&a=sn&rid=" + s_rowNo);
		Response.Write("&mod=e&l=1>Edit</a></td></tr>");
		Response.Write("<tr><td><input type=text size=15 name=sInputSN value='" + s_newsn + "'>&nbsp;&nbsp;");
		Response.Write("<input type=submit name=sn_cmd value=' OK '></td></tr>");
	}
	//messages
	Response.Write("<tr><td rowspan=5 colspan=2><font color=red><b><br>&nbsp;" + m_msg + "</b><font></td></tr>");

	Response.Write("</table></td>");

	Response.Write("</tr></table>\r\n");
	Response.Write("</form>");

	//"back" button to back to the "edit" mode
	Response.Write("<table width=80%><tr align=right><td><button onclick=window.location=");
	Response.Write("('pos.aspx?i=" + m_quoteNumber + "')> Back </button></td></tr></table>");
}

bool matchingItem()
{
	string ssid = "";
	if(Request.QueryString["ssid"] != null && Request.QueryString["ssid"] != "")
		ssid = Request.QueryString["ssid"];

	dtCart = (DataTable)Session["ShoppingCart" + ssid];
	int s_row = 0;

	try
	{
		s_row = int.Parse(Request.QueryString["rid"]);
	}
	catch(Exception e)
	{
		ShowExp("", e);
		return false;
	}

	string item_id = dtCart.Rows[s_row]["code"].ToString();
	string item_SN = dtCart.Rows[s_row]["s_serialNo"].ToString();

	string sc = "SELECT invoice_number, code, quantity, serial_number FROM sales WHERE ";
		   sc+= "code = " + item_id + " AND (serial_number ";
		   if(item_SN == "")
			   sc += " is null OR serial_number = '" + item_SN + "' ";
		   else
			   sc += "='" + item_SN + "' ";
	       sc+= ") AND invoice_number = " + m_quoteNumber;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "matching");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

//DEBUG("matching sql = ", sc);
/*	if(item_sn !="")
	{
		if((Request.Form["sInputSN"] == "" || Request.Form["sInputSN"] == null))
		{
DEBUG("DoInsertSn sql = ", sc);
			if(!DoinsertSN(item_sn))
				return false;
		}
	}	*/

//DEBUG("rows = ", dst.Tables["matching"].Rows.Count);
//DEBUG("id = ", dst.Tables["matching"].Rows[0]["id"].ToString());

	return true;
}

bool getItemSNs()
{
	if(dst.Tables["item_sn"] != null)
		dst.Tables["item_sn"].Clear();
	string sc = "SELECT * FROM sales_serial WHERE invoice_number = ";
	sc += dst.Tables["matching"].Rows[0]["invoice_number"].ToString() + " AND code = ";
	sc += dst.Tables["matching"].Rows[0]["code"].ToString();
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "item_sn");
		//m_nSearchReturn = myAdapter.Fill(dst, "item_sn");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
//DEBUG("sql = ", sc);

	return true;
}

bool DoinsertSN(string SN)
{
	if(!CheckSN(SN))  //check is the sn in the table or not, if it's in, can not insert...
		return false;

	string sc = "INSERT INTO sales_serial (invoice_number, code, sn) VALUES (";
	       sc+= dst.Tables["matching"].Rows[0]["invoice_number"].ToString()+ ", ";
		   sc+= dst.Tables["matching"].Rows[0]["code"].ToString()+ ", '" + SN + "')";

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

bool CheckSN(string s_sn)
{
	string sc = "SELECT COUNT(*) AS quantity FROM sales_serial WHERE sn = '" + s_sn + "'";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "sncheck");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(int.Parse(dst.Tables["sncheck"].Rows[0]["quantity"].ToString()) > 0 )
	{
		m_msg = " * Operation failed! Duplicated SN, the SN is not valid!";
		m_msg += "<br><br><center>input S/N: " + s_sn + "</center>";
		return false;
	}
	return true;
}

bool DoUpdateSN(string s_sn_new, string s_refID)
{
	if(s_refID != null && s_refID != "")
	{	
		try
		{
			if(!IsInteger(s_sn_new))
				s_sn_new = "null";
		}
		catch(Exception intconvert)
		{
			ShowExp("", intconvert);
			return false;
		}

		string sc = "UPDATE sales_serial SET sn = " + s_sn_new + " WHERE id = " + s_refID;
		sc+= " DELETE FROM sales_serial WHERE id = " + s_refID + " AND sn is null";
	//DEBUG("sc=", sc);
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

		m_msg = " * Update Successfully Completed!";
		return true;
		}
	else
		m_msg = " * This item is not editable, Change to Input SN to Add new.";

	return true;

}


</script>
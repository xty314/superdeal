<script runat=server>

string m_code = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString[0] != null)
		m_code = Request.QueryString[0];

	if(Request.Form["cmd"] == "YES")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=dp.aspx?done\">");
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	if(m_code == "done")
		Response.Write("<br><center><h3>Product deleted</h3></center>");
	else
		PrintConfirmForm();
	PrintAdminFooter();
}

void PrintConfirmForm()
{
	if(GetProductStock(m_code) != "0")
	{
		Response.Write("<br><center><h3>Error, this item has stock on hand, cannot be deleted</h3>");
		return;;
	}
	string desc = GetProductDesc(m_code);
	Response.Write("<br><center><h3>Delete Product</h3></center>");
	Response.Write("<form action=dp.aspx?" + m_code + " method=post>");
//	Response.Write("<input type=hidden name=code value=" + m_code + ">");
	Response.Write("<table align=center>");
//	Response.Write("<tr><td>&nbsp;</td></tr>");
	Response.Write("<tr><td><font color=red size=+1><b>#" + m_code + " : " + desc + "</b></font></td></tr>");
	Response.Write("<tr><td><b>Are you sure you want to permanently delete this item?</b></td></tr>");
	Response.Write("<tr><td><b>This action cannot roll back</b></td></tr>");
	Response.Write("<tr><td align=right><input type=submit name=cmd value='YES'></td></tr>");
	Response.Write("</table></form>");
}

bool DoDelete()
{
	//bakcup product
/*	DataSet ds = new DataSet();
	string sc = "SELECT * FROM product WHERE code=" + m_code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "product") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(!BackupProduct(ds.Tables["product"].Rows[0]))
		return false;
*/
	
	//delete product table
	string sc = "DELETE FROM product_skip ";
	sc += " WHERE id = (SELECT id FROM code_relations WHERE code=" + m_code + ") ";
	sc += " DELETE FROM code_relations WHERE code=" + m_code;
	sc += " DELETE FROM product WHERE code=" + m_code;
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
</script>

<asp:Label id=LFooter runat=server/>
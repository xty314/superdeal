<script runat="server">

string m_pcode = "";
string m_pdesc = "";
string m_type = "";
string m_id = "";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table


void Page_Load(Object Src, EventArgs E ) 
{
	
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
	
	PrintAdminHeader();
	PrintAdminMenu();

	CreateProductCodeForm();
	if(Request.Form["cmd"] == "Update Product Code")
	{
		if(Request.QueryString["id"] != null)
			m_id = Request.QueryString["id"].ToString();

		m_pcode = Request.Form["txtPCode"].ToString();
		m_pdesc = Request.Form["txtPDesc"].ToString();
		m_type = Request.Form["txtPType"].ToString();
		if(!UpdateProductCode())
			return;
	}
	if(Request.QueryString["success"] == "d")
	{
		//if(Request.Form["txtPCode"] != null)
			m_pcode = Request.Form["txtProdCode"].ToString();
		//if(Request.Form["txtPDesc"] != null)
			m_pdesc = Request.Form["txtProdDesc"].ToString();
		//if(Request.Form["txtType"] != null)
			m_type = Request.Form["txtProdType"].ToString();
		if(!GetProductCode())
			return;


	}
	if(Request.QueryString["del"] != null)
	{
		if(!DeleteProductCode())
			return;
	}



	if(Request.QueryString["edit"] != null)
	{
		
		if(!GetEditProductCode())
			return;
		//DisplayEditProductCode();

	}
	else 
	{
		if(!GetProductCodeList())
			return;

		//BindProductGrid();		
		BindProductCodeGrid();
	}
	
	

	LFooter.Text = m_sAdminFooter;
//	PrintAdminFooter();
}
bool DeleteProductCode()
{
	string s_id = Request.QueryString["del"].ToString();

	string sc = "DELETE FROM product_code ";
	sc += " WHERE id = "+ s_id +"";
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;

}

bool UpdateProductCode()
{
	string s_id = Request.QueryString["id"].ToString();
	string sc = " UPDATE product_code "	;
	sc += " SET product_code = '"+m_pcode+"', product_desc = '"+m_pdesc+"', type = '"+m_type+"' ";
	sc += " WHERE id = "+s_id+"";
	
	try
	{
		myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
		
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	return true;
}

bool GetEditProductCode()
{
	m_id = Request.QueryString["edit"].ToString();

	string sc = " SELECT * ";
	sc += " FROM product_code ";
	sc += " WHERE id = "+m_id+"";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "editcode");
	}
	catch(Exception e) 
	{
		
		ShowExp(sc, e);
		return false;
	}
	string s_pcode = "";
	string s_pdesc = "";
	string s_type = "";
	//for(int i=0; i<dst.Tables["editcode"].Rows.Count; i++)
	if(dst.Tables["editcode"].Rows.Count == 1 )
	{
	
		DataRow dr = dst.Tables["editcode"].Rows[0];
			
		s_pcode = dr["product_code"].ToString();
		s_pdesc = dr["product_desc"].ToString();
		s_type = dr["type"].ToString();
	}

	Response.Write("<br><hr size=1 width=45% color=black align=left>");
	Response.Write("<form name=frmEditProductCode method=post action='createpcode.aspx?id="+m_id+"'> ");
	Response.Write("<table width=45% cellspacing=3 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td colspan=2 align=center><b>Edit Current Product Code</b></td></tr>");


	Response.Write("<tr><td align=right>Product Code</td>");
	Response.Write("<td><input type=text name=txtPCode value='"+s_pcode+"'></td></tr>");
	Response.Write("<tr><td align=right>Product Description</td>");
	Response.Write("<td><input type=text name=txtPDesc value='"+s_pdesc+"'></td></tr>");
	Response.Write("<tr><td align=right>Product Type</td>");
	
	//Response.Write("<tr><td bgcolor=red>&nbsp;</td>");

	Response.Write("<td><input type=text name=txtPType value='"+s_type+"'></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value='Update Product Code'></td></tr>");


	Response.Write("</table>");
	Response.Write("</form>");

	return true;

	
}

void CreateProductCodeForm()
{
	Response.Write("<script Language=javascript");
	Response.Write(">");
	Response.Write("<!-- hide from older browser");
	string s = @"
		function checkcode()
		{
		
		if(document.frmCreatePCode.txtProdCode.value=='') {
		window.alert ('Please Enter a Product Code!');
		document.frmCreatePCode.txtProdCode.focus();
		return false;
		}
		if(document.frmCreatePCode.txtProdType.value=='') {
		window.alert ('Please Enter a Product Type!');
		document.frmCreatePCode.txtProdType.focus();
		return false;
		}
		return true;
		}
		
	";
	
	Response.Write("--> "); 
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> ");

	Response.Write("<form name=frmCreatePCode method=post action='createpcode.aspx?success=d'>");
	//Response.Write("<font size=1><b>Create New Product Code </font></b>");
	Response.Write("<table width=45% cellspacing=3 cellpadding=2 border=0 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;\">");
//	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	

	Response.Write("<tr bgcolor=#E3E3E3><td colspan=2 align=center><b>Create New Product Code </b></td></tr>");
	Response.Write("<tr><td align=right>Enter Product Code :</td><td><input type=text name=txtProdCode ></td></tr>");
	Response.Write("<tr><td  align=right>Enter Type :</td><td><input type=text name=txtProdType ></td></tr>");
	Response.Write("<tr><td align=right>Enter Product Descrition :</td><td><input type=text name=txtProdDesc ></td></tr>");
	
	Response.Write("<tr><td>&nbsp;</td><td><input type=submit name=cmd value='Create Product Code' ");
	Response.Write(" OnClick='return checkcode();'></td></tr>");

	Response.Write("</table>");
	Response.Write("</form>");
	
}

bool GetProductCodeList()
{
	string sc = "SELECT * FROM product_code ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "codelist");
	}
	catch(Exception e) 
	{
		
		ShowExp(sc, e);
		return false;
	}

	return true;

}

void BindProductGrid()
{
	Response.Write("<table cellspacing=3 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td>Product Code</td>");
	Response.Write("<td>Product Description</td>");
	Response.Write("<td>Product Type</td>");
	Response.Write("<td bgcolor=red>&nbsp;</td></tr>");

	for(int i=0; i<dst.Tables["codelist"].Rows.Count; i++)
	{
		DataRow dr = dst.Tables["codelist"].Rows[i];
		
		string sprodcode = dr["product_code"].ToString();
		string sprod_desc = dr["product_desc"].ToString();
		string stype = dr["type"].ToString();
		string sid = dr["id"].ToString();

		Response.Write("<tr><td>"+sprodcode+"</td>");
		Response.Write("<td>"+sprod_desc+"</td>");
		Response.Write("<td>"+stype+"</td>");
		Response.Write("<td><a href='createpcode.aspx?edit="+sid+"'>edit</a></td></tr>");
		

	}
	Response.Write("</table>");
}

bool GetProductCode()
{
	string spcode = "";
	if(Request.Form["txtPCode"] != null)
		spcode = Request.Form["txtPCode"].ToString();

	string sc = "SELECT * FROM product_code ";
	sc += " WHERE product_code = '"+spcode+"'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "createcode");

		if(dst.Tables["createcode"].Rows.Count<=0)
		{
			string ssc = "INSERT INTO product_code (product_code,product_desc ,type ) ";
			ssc += " VALUES ('"+m_pcode+"','"+ m_pdesc+"','"+m_type+"' )";
			try
			{
				myCommand = new SqlCommand(ssc);
				myCommand.Connection = myConnection;
				myConnection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e) 
			{
				
				ShowExp(ssc, e);
				return false;
			}
		}
		else
		{
			
			Response.Write("<embed src=/wav/09.mp3 volume=100 hidden=true autostart=true>");
			Response.Write("The product number already exist ");
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	return true;

}



void BindProductCodeGrid()
{
	DataView source = new DataView(dst.Tables["codelist"]);
	//string path = Request.ServerVariables["URL"].ToString();
	MyDataGrid.DataSource = source;
	MyDataGrid.DataBind();

}
void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{

	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindProductCodeGrid();
	
}

</script>
<form runat=server> 
<br><hr size=1 align=left width=45%>
<b>  <br></b>

<asp:DataGrid id=MyDataGrid
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#E3E3E3
	CellPadding=2
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=45% 
	style=fixed
	HorizontalAlign=left
	AllowPaging=True
	PageSize=20
	PagerStyle-PageButtonCount=20
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<Columns>
		<asp:HyperLinkColumn
			 HeaderText="Edit Product Code"
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="createpcode.aspx?edit={0}"
			 Text=Edit
			 />
	</Columns>
		<Columns>
		<asp:HyperLinkColumn
			 HeaderText=""
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="createpcode.aspx?Del={0}"
			 Text=Delete
			 />
	</Columns>

	<HeaderStyle BackColor=#E3E3E3 ForeColor=black Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
</asp:DataGrid>



</form>
<asp:Label id=LFooter runat=server/>

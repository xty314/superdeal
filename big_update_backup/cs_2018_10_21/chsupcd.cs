<script runat=server>

DataSet dst = new DataSet();
DataTable dtExpense = new DataTable();

string m_id = "";
bool m_bRecorded = false;
bool m_bIsPaid = false;

string m_supplier_code = "";
string m_code = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
	if(!SecurityCheck("administrator"))
		return;
	
	if(Request.Form["cmd"] == Lang("Change"))
	{		
		if(Request.Form["new_supplier_code"] == null || Request.Form["new_supplier_code"] == "")
		{
			Response.Write("<h4>"+Lang("No Supplier Code")+"!!!");
			Response.Write("<br><a href="+ Request.ServerVariables["URL"] +"&e=new&cd="+ Request.QueryString["cd"] +" class=x> << Back</a>");
			
			return;
		}
		if(!checkDuplicateSupplierCode(Request.Form["new_supplier_code"]))
		{
			Response.Write("<h4>Duplicated Supplier Code!!! - "+ Request.Form["new_supplier_code"]);
			Response.Write("<br><a href="+ Request.ServerVariables["URL"] +"&e=new&cd="+ Request.QueryString["cd"] +" class=x> << Back</a>");
			return;
		}
		doAddtoSession();
		doCreateChangeCodeProcedure();
		if(doCreateChangeCodeProcedure())
		{
//			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?rd=next\">");
			if(doUpdateSupplierCode())
			{
				doDropChangeCodeProcedure();
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?rd=done\">");
				return;
			}
		}
		return;
	}
	
	if(Request.Form["cmd"] == Lang("Proceeding now..."))
	{
	//	doCreateChangeCodeProcedure();
		if(doUpdateSupplierCode())
		{
			doDropChangeCodeProcedure();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?rd=done\">");
			return;
		}
	}
	if(Request.QueryString["e"] == "new")
	{
		CleanSession();
	}
	if(Request.QueryString["rd"] == "next")
	{
//		PrintAdminHeader();
//		PrintAdminMenu();
		ContinueChangeSupplierCode();
//		PrintAdminFooter();
		return;
	}
	if(Request.QueryString["rd"] == "done")
	{
//		PrintAdminHeader();
//		PrintAdminMenu();
		ShowProcessDone();
//		PrintAdminFooter();
		return;
	}
//	PrintAdminHeader();
//	PrintAdminMenu();

	if(Request.QueryString["cd"] == null || Request.QueryString["cd"] == "") // || Request.QueryString["scd"] == null || Request.QueryString["scd"] == "")
	{
//		Response.Write("<script language=javascript>window.alert('Invalid Page!!!'); window.close();</script");
//		Response.Write(">");
		PrintAdminHeader();
		Response.Write("<center><br><br>");
		Response.Write("<form name=f action=chsupcd.aspx method=get><b>"+Lang("Product Code")+" : </b><input type=text name=cd><input type=submit value="+Lang("Continue")+"></form>");
		Response.Write("<script language=javascript>document.f.cd.focus();</script");
		Response.Write(">");
		return;
	}
	if(!TSIsDigit(Request.QueryString["cd"].ToString()))
	{
		Response.Write("<script language=javascript>window.alert('Invalid Product Code, must be a number'); </script");
		Response.Write(">");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=chsupcd.aspx\">");
		return;
	}

	GetDetailForms();
	
//	PrintAdminFooter();
}

void CleanSession()
{
	Session["ch_old_supplier_code"] = null;
	Session["ch_new_supplier_code"] = null;
	Session["ch_code"] = null;
	Session["ch_old_code"] = null;	
	Session["ch_new_supplier"] = null;
	Session["ch_old_supplier"] = null;
}
void ContinueChangeSupplierCode()
{
//	DEBUG("Sessions=", Session["ch_old_supplier_code"].ToString());
//	DEBUG("Sessions=", Session["ch_new_supplier_code"].ToString());
	Response.Write("<center><br><h4>");
	Response.Write(Lang("Continue Proceeding... "));
	Response.Write("<form name=f method=post>");
	Response.Write("<table width=60%  align=center valign=center cellspacing=0 cellpadding=4 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=2><input type=submit name=cmd value='"+Lang("Proceeding now...")+"' "+ Session["button_style"] +"");
//	Response.Write(" onclick=\" if(document.f.other_supplier.value.length >5){return false;} if(document.f.new_supplier_code.value==''){return false;}else if(!confirm('Confirm Changes!!! This Will Not Be UNDONE')){return false;};\" ");
	Response.Write(">");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
	
}

void ShowProcessDone()
{
	Response.Write("<center><br><h4>"+Lang("Changes Done..."));
	Response.Write("<script language=javascript>window.alert('Changes Done...'); window.location='chsupcd.aspx?cd=" + Session["ch_code"].ToString() + "';</script");
	Response.Write(">");
}

void GetDetailForms()
{

string sc = " SELECT DISTINCT c.*, sl.old_supplier_code FROM code_relations c ";
sc += " LEFT OUTER JOIN supplier_code_changed_log sl ON sl.old_code = c.code ";
sc += " WHERE code = "+ Request.QueryString["cd"] +"";
int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "ch_code");
		
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("Invalid object name 'supplier_code_changed_log'") >=0)
		{
			sc = @" CREATE TABLE [dbo].[supplier_code_changed_log](
			[id] [bigint] IDENTITY(1,1) NOT NULL,
			[old_supplier_code] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[changed_supplier_code] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[old_code] [int] NOT NULL,
			[changed_code] [int] NOT NULL,
			[old_supplier] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[changed_supplier] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
			[record_by] [int] NOT NULL,
			[record_date] [datetime] NOT NULL
			) ON [PRIMARY] 
		";
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
				return;
			}
		}

		ShowExp(sc, e);
		return;
	}
	if(rows <= 0)
	{
		Response.Write("<script language=javascript>window.alert('Product not found!');</script");
		Response.Write(">");
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=chsupcd.aspx\">");
		return;
	}

	Response.Write("<center><h4>"+Lang("Change Supplier Code")+"</h4>");
	Response.Write("Please do with care</center>");
	Response.Write("<form name=f method=post>");
	Response.Write("<table width=60%  align=center valign=center cellspacing=0 cellpadding=4 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<input type=hidden name=old_supplier_code value='" + dst.Tables["ch_code"].Rows[0]["supplier_code"].ToString() +"'>");
	Response.Write("<input type=hidden name=old_code value='" + dst.Tables["ch_code"].Rows[0]["code"].ToString() +"'>");
	Response.Write("<input type=hidden name=code value='" + dst.Tables["ch_code"].Rows[0]["code"].ToString() +"'>");
	Response.Write("<input type=hidden name=old_supplier value='" + dst.Tables["ch_code"].Rows[0]["supplier"].ToString() +"'>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("ID")+"#:</td><td>"+ dst.Tables["ch_code"].Rows[0]["id"].ToString() +"</td></tr>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("Product Code")+":</td><td>"+ dst.Tables["ch_code"].Rows[0]["code"].ToString() +"</td></tr>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("Current Supplier Code")+":</td><td>"+ dst.Tables["ch_code"].Rows[0]["supplier_code"].ToString() +"</td></tr>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("Product Description")+":</td><td>"+ dst.Tables["ch_code"].Rows[0]["name"].ToString() +"</td></tr>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("Supplier")+":</td><td>"+ dst.Tables["ch_code"].Rows[0]["supplier"].ToString() +"</td></tr>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("Select Supplier")+" :");
	Response.Write("</td><td>");
	GetSupplierShortName(dst.Tables["ch_code"].Rows[0]["supplier"].ToString());
	Response.Write("&nbsp;<input type=text name=other_supplier> (The program will take the default supplier to add to the system, so before changing the supplier code, please make sure you have select the correct SUPPLIER)</td></tr>");
	Response.Write("<tr>\r\n");
	Response.Write("<td>"+Lang("New Supplier Code")+":</td><td><input type=text size=60% name=new_supplier_code></td></tr>");
	Response.Write("<tr><td colspan=2 align=center>");
//	Response.Write("<input type=button value='"+Lang("Edit Product")+"' "+ Session["button_style"] +" onclick=\"window.location=('liveedit.aspx?code="+ Request.QueryString["cd"] +"')\" >");
	Response.Write("<input type=submit name=cmd value='"+Lang("Change")+"' "+ Session["button_style"] +"");
	Response.Write(" onclick=\" if(document.f.other_supplier.value.length >5){return false;} if(document.f.new_supplier_code.value==''){return false;}else if(!confirm('Are you really sure to change it??')){return false;};\" ");
	Response.Write(">");
//	Response.Write("<input type=button value='"+Lang("Cancel")+" ' "+ Session["button_style"] +" onclick=\"window.close();\" >");
	Response.Write("</td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");

}

bool GetSupplierShortName(string supplier)
{
	string sc = " SELECT c.short_name FROM card c ";
	sc += " JOIN enum e ON e.id = c.type AND e.class='card_type' AND e.name='supplier' ";
	sc += " WHERE c.type=3";
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "short");
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows <= 0)
	{
		Response.Write("<script language=javascript>window.alert('Invalid Page!!!'); window.close();</script");
		Response.Write(">");
		return false;
	}
	Response.Write("<select name=supplier>");
	for(int i=0; i<rows; ++i)
	{
		Response.Write("<option ");
		if(supplier == dst.Tables["short"].Rows[i]["short_name"].ToString())
			Response.Write(" selected ");
		Response.Write(" value='"+ dst.Tables["short"].Rows[i]["short_name"].ToString() +"'>"+ dst.Tables["short"].Rows[i]["short_name"].ToString() +"</option>");
	}
	
	Response.Write("</select>");
	return true;
}
bool doAddtoSession()
{
	string old_supplier_code = Request.Form["old_supplier_code"];
	string new_supplier_code = Request.Form["new_supplier_code"];
	string code = Request.Form["code"];
	string old_code = Request.Form["old_code"];
	string new_supplier = Request.Form["supplier"];
	string old_supplier = Request.Form["old_supplier"];
	if(Request.Form["other_supplier"] != null && Request.Form["other_supplier"] != "")
		new_supplier = Request.Form["other_supplier"];

	new_supplier = EncodeQuote(new_supplier);
	if(code == "" || new_supplier_code == "")
		return false;

	if(!TSIsDigit(code))
		return false;

	new_supplier_code = StripHTMLtags(EncodeQuote(new_supplier_code));
	old_supplier_code = StripHTMLtags(EncodeQuote(old_supplier_code));

	//****** add data to Session
	Session["ch_old_supplier_code"] = old_supplier_code;
	Session["ch_new_supplier_code"] = new_supplier_code.ToUpper();
	Session["ch_code"] = code;
	Session["ch_old_code"] = old_code;
	Session["ch_new_supplier"] = new_supplier.ToUpper();
	Session["ch_old_supplier"] = old_supplier;
		//****** end here ***********

	return true;
}
bool doUpdateSupplierCode()
{

/*	string old_supplier_code = Request.Form["old_supplier_code"];
	string new_supplier_code = Request.Form["new_supplier_code"];
	string code = Request.Form["code"];
	string new_supplier = Request.Form["supplier"];
	string old_supplier = Request.Form["old_supplier"];
	if(Request.Form["other_supplier"] != null && Request.Form["other_supplier"] != "")
		new_supplier = Request.Form["other_supplier"];

	new_supplier = EncodeQuote(new_supplier);
	if(code == "" || new_supplier_code == "")
		return false;

	if(!TSIsDigit(code))
		return false;

	new_supplier_code = StripHTMLtags(EncodeQuote(new_supplier_code));
	old_supplier_code = StripHTMLtags(EncodeQuote(old_supplier_code));

	//****** add data to Session
	Session["ch_old_supplier_code"] = old_supplier_code;
	Session["ch_new_supplier_code"] = new_supplier_code;
	Session["ch_code"] = code;
	Session["ch_new_supplier"] = new_supplier;
	Session["ch_old_supplier"] = old_supplier;

	//****** end here ***********
//DEBUG("code =", code);
//DEBUG("new code =", new_supplier_code);
//DEBUG("old code =", old_supplier_code);
	SqlCommand myCommand = new SqlCommand("ch_all_supplier_code", myConnection);
	myCommand.CommandType = CommandType.StoredProcedure;

	myCommand.Parameters.Add("@new_supplier_code", SqlDbType.VarChar).Value = new_supplier_code;
	myCommand.Parameters.Add("@old_supplier_code", SqlDbType.VarChar).Value = old_supplier_code;
	myCommand.Parameters.Add("@code", SqlDbType.Int).Value = code;
	myCommand.Parameters.Add("@old_supplier", SqlDbType.VarChar).Value = old_supplier;
	myCommand.Parameters.Add("@new_supplier", SqlDbType.VarChar).Value = new_supplier;
	myCommand.Parameters.Add("@return_status", SqlDbType.Int).Value = 0;
	try
	{
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception e) 
	{
		ShowExp("doUpdateSupplierCode", e);
		return false;
	}

	string return_status = myCommand.Parameters["@return_status"].Value.ToString();
	if(return_status != "0")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		Response.Write("<br><br><center><h3>"+Lang("Error")+", " + return_status);
		
		return false;
	}
	*/
	if(Session["ch_old_supplier_code"] != null)
	{
		string old_supplier_code = Session["ch_old_supplier_code"].ToString();
		string new_supplier_code = Session["ch_new_supplier_code"].ToString();
		string code = Session["ch_code"].ToString();
		string new_supplier = Session["ch_new_supplier"].ToString();
		string old_supplier = Session["ch_old_supplier"].ToString();
		
		string sc = " EXECUTE ch_all_supplier_code '"+ new_supplier_code.ToUpper() +"' ";
		sc += ", '"+ old_supplier_code +"', "+ code +", '"+ old_supplier +"', '"+ new_supplier.ToUpper() +"', '"+ Session["card_id"].ToString() +"', 0 ";
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			myCommand.Fill(dst);
			
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	
	}
	return true;

}
bool doCreateChangeCodeProcedure()
{
	string sc = "CREATE PROCEDURE ch_all_supplier_code  \r\n";
	sc += " @new_supplier_code varchar(255), \r\n";
	sc += " @old_supplier_code varchar(255), \r\n";	
	sc += " @code int, \r\n";
	sc += " @old_supplier varchar(100), \r\n";
	sc += " @new_supplier varchar(100), \r\n";	
	sc += " @card_id int, \r\n";
	sc += " @return_status int OUTPUT \r\n";
	sc += " AS \r\n";
	sc += " begin transaction \r\n";
//	sc += " BEGIN \r\n";
//	sc += " IF EXISTS (SELECT code FROM product WHERE code = @code) \r\n";
	sc += " BEGIN \r\n";
	sc += " \tIF NOT EXISTS(SELECT supplier_code FROM code_relations WHERE supplier_code = @new_supplier_code AND supplier = @new_supplier) ";
	sc += " \t BEGIN ";
	sc += " \tUPDATE code_relations set supplier_code = @new_supplier_code, supplier = @new_supplier, id = @new_supplier + @new_supplier_code WHERE code = @code AND supplier_code = @old_supplier_code \r\n";
	sc += " \tUPDATE product_skip set id = @new_supplier + @new_supplier_code WHERE id = @old_supplier + @old_supplier_code\r\n";
	sc += " \tUPDATE stock set supplier_code = @new_supplier_code WHERE supplier_code = @old_supplier_code AND product_code = @code\r\n";
	sc += " \tUPDATE product set supplier_code = @new_supplier_code, supplier = @new_supplier WHERE code = @code AND supplier_code = @old_supplier_code \r\n";
	sc += " \tUPDATE order_item set supplier_code = @new_supplier_code, supplier = @new_supplier WHERE code = @code AND supplier_code = @old_supplier_code \r\n";
	sc += " \tUPDATE sales set supplier_code = @new_supplier_code, supplier = @new_supplier WHERE code = @code AND supplier_code = @old_supplier_code \r\n";
	sc += " \tUPDATE purchase_item set supplier_code = @new_supplier_code WHERE code = @code AND supplier_code = @old_supplier_code \r\n";
	sc += " \tINSERT INTO supplier_code_changed_log (old_supplier_code, changed_supplier_code, old_code, changed_code, old_supplier, changed_supplier, record_by, record_date ) ";
	sc += " \t VALUES(@old_supplier_code, @new_supplier_code, @code, @code,@old_supplier, @new_supplier, @card_id, GETDATE() ) ";
	sc += " \t END ";
	sc += " END \r\n";
//	sc += " END \r\n";
	sc += " commit transaction \r\n";
	sc += " set @return_status = @code --done\r\n";
//DEBUG("sc = ", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst);
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("already an object named") >= 0)
			return true;
		ShowExp(sc, e);
		doDropChangeCodeProcedure();
		return false;
	}
	return true;
}

bool doDropChangeCodeProcedure()
{
	string sc = " DROP PROCEDURE ch_all_supplier_code ";	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(dst);
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

bool checkDuplicateSupplierCode(string supplier_code)
{
	string sc = " SELECT supplier_code, code FROM code_relations  ";	
	sc += " WHERE supplier_code = '"+ supplier_code +"' ";
//DEBUG("sc = ", sc);
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst);
		
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(rows > 0)
		return false;
	return true;
}




</script>

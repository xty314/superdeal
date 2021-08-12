<script runat=server>

string m_page = "";
string m_cat = "";
string m_template_type = "0";
string m_repair_finish_card_id = "";
string m_repair_card_id = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["p"] != null)
		m_page = Request.QueryString["p"];
	
	if(Request.Form["cmd"] != null)
	{
		if(Request.Form["cmd"] == "Delete")
		{
			if(Request.Form["del_confirm"] != "on")
			{
				Response.Write("<br><br><center><h3>Please tick confirm box to delete</h3>");
				return;
			}
			SaveRATemplate(Request.Form["page_id"], "", "", Request.QueryString["sid"]); //two blank field means delete
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?r=" + DateTime.Now.ToOADate() + "");
			if(Request.QueryString["or"] != null && Request.QueryString["or"] != "")
				Response.Write("&or=3");
			Response.Write("\">");
		}
		else
		{
			string text = Request.Form["txt"];
			text = text.Replace("nbsp", "&nbsp");
			string supplier_id = Request.Form["supplier"];
			if(Request.Form["other"] != null && Request.Form["other"] != "")
				supplier_id = Request.Form["other"];
			if(!SaveRATemplate(Request.Form["page_id"], EncodeQuote(Request.Form["page_name"]), EncodeQuote(text), EncodeQuote(supplier_id)))
				return;
			TSRemoveCache(m_sCompanyName + m_sSite + "_" + m_sHeaderCacheName);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL="+ Request.ServerVariables["URL"] +"?");
			if(Request.QueryString["or"] != null && Request.QueryString["or"] != "")
				Response.Write("or=3&");
			Response.Write("sid="+ Request.QueryString["sid"] +"\">");
		}
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br>");
	
	PrintEditForm();
	LFooter.Text = m_sAdminFooter;
}

bool SaveRATemplate(string id, string name, string text, string supplier_id)
{
	string sc = "";
	if(supplier_id != "")
	{
		if(name == "" && text == "" && id != "")
			sc = "DELETE FROM template_ra_form WHERE id=" + id;
		else if(id == "")
		{
			sc = "INSERT INTO template_ra_form (text_template, name, supplier_id, template_type	) ";
			sc += " VALUES('"+ text +"', '"+ name +"', '"+ supplier_id +"', "+ Request.Form["template_type"].ToString() +" ) ";
					
		}
		else
		{
			sc = "UPDATE template_ra_form SET text_template='";
			sc += text;
			sc += "', name='"; 
			sc += name;
			sc += "', supplier_id='";
			sc += supplier_id;
			sc += "'";
			sc += " , template_type = "+ Request.Form["template_type"].ToString() +"";
			sc += " WHERE id=" + id;
		}
//		DEBUG("sc =", sc);
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
	}
	return true;
}


string GetTemplateText(string supplier_id, ref string id)
{
	string s = "<!-- **********************************************************************\n";	
	s += "Please use the data attribute to fill up faulty items \n";
	s += "All attributes are: ";
	if(Request.QueryString["or"] != null && Request.QueryString["or"] != "")
	{
		s += "The Vairiables depending on how many you set on the edit site settings, ie: if you set 10 faulty items to be process ";
		s += " then you need 10 Variables to input to the template form ";
		s += "@@company,@@customer,@@ticket,@@addr1,@@addr2,@@city,@@phone,@@fax,@@email,@@ra_number,@@repair_date, @@technician0 \r\n";
		s += "@@replace_sn0,@@replace_sn1,@@replace_sn2,@@replace_sn3,@@replace_sn4 \r\n";
		s += "@@replace_inv0,@@replace_inv1,@@replace_inv2,@@replace_inv3,@@replace_inv4 \r\n";
		s += "@@replace_code0,@@replace_code1,@@replace_code2,@@replace_code3,@@replace_code4 \r\n";
		s += "@@replace_item_desc0,@@replace_item_desc1,@@replace_item_desc2,@@replace_item_desc3,@@replace_item_desc4 \r\n";
		s += "@@replace_date0,@@replace_date1,@@replace_date2,@@replace_date3,@@replace_date4 \r\n";
		s += "@@replace_supp_code0,@@replace_supp_code1,@@replace_supp_code2,@@replace_supp_code3,@@replace_supp_code4 \r\n";
		s += "@@replaced_qty0,@@replaced_qty1,@@replaced_qty2,@@replaced_qty3,@@replaced_qty4 \r\n";
		s += "@@repair_qty0,@@repair_qty1,@@repair_qty2,@@repair_qty3,@@repair_qty4 \r\n";
		s += "@@sn0,@@sn1,@@sn2,@@sn3,@@sn4 \r\n";
		s += "@@code0,@@code1,@@code2,@@code3,@@code4 \r\n";
		s += "@@invoice0,@@invoice1,@@invoice2,@@invoice3,@@invoice4 \r\n";
		s += "@@invoice_date0,@@invoice_date1,@@invoice_date2,@@invoice_date3,@@invoice_date4 \r\n";
		s += "@@fault_desc0,@@fault_desc1,@@fault_desc2,@@fault_desc3,@@fault_desc4 \r\n";
		s += "@@item_desc0,@@item_desc1,@@item_desc2,@@item_desc3,@@item_desc4 \r\n";
		s += "@@status0,@@status1,@@status2,@@status3,@@status4 \r\n";
		s += "@@supp_code0,@@supp_code1,@@supp_code2,@@supp_code3,@@supp_code4\r\n";
		s += "@@repair_note0,@@repair_note1,@@repair_note2,@@repair_note3,@@repair_note4\r\n";
		s += "@@authorize_date, @@received_date, @@ticket \r\n ";
		
	}
	else
	{
		s += "The Vairiables depending on how many you set on the edit site settings, ie: if you set 10 faulty items to be process ";
		s += " then you need 10 Variables to input to the template form ";
		s += " @total_item, @own_rma_no \r\n";
		s += " @invoice0, @invoice1, @invoice2, @invoice3, @invoice4 \r\n";
		s += "		@sn0, @sn1, @sn2, @sn3, @sn4 \n";
		s += "		@supplier_code0, @supplier_code1, @supplier_code2, @supplier_code3, @supplier_code4 \n";
		s += "		@fault0, @fault1, @fault2, @fault3, @fault4 \n";
		s += "		@desc0, @desc1, @desc2, @desc3, @desc4 \n";
		s += "		@repair_date0, @repair_date1, @repair_date2, @repair_date3, @repair_date4 \n";
		s += "		@pur_date0, @pur_date1, @pur_date2, @pur_date3, @pur_date4 \n";
		s += "		@supplier_rmano \n";
		s += "      @ticket \n\r";
	}
	s += " *****************************************************************************-----> ";
	if(supplier_id != "")
	{
		string sc = "SELECT id, text_template, template_type FROM template_ra_form ";
		sc += " WHERE 1=1 ";
		sc += " AND supplier_id='";
		sc += supplier_id;
		sc += "'"; 
		
		int rows = 0;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			DataSet ds = new DataSet();
			rows = myCommand.Fill(ds);
			if(rows > 0)
			{
				s = ds.Tables[0].Rows[0]["text_template"].ToString();
				id = ds.Tables[0].Rows[0]["id"].ToString();
				m_template_type = ds.Tables[0].Rows[0]["template_type"].ToString();
				//cat = ds.Tables[0].Rows[0]["cat"].ToString();
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
		}
	}
	else
		return s;
	
	return s;
}

void PrintEditForm()
{
	string id = "";
	string supplier_id = "";
	string uri = Request.ServerVariables["URL"] +"?sid=";
	if(Request.QueryString["sid"] != "" && Request.QueryString["sid"] != null)
		supplier_id = Request.QueryString["sid"];
	string text = GetTemplateText(supplier_id, ref id);
	text = text.Replace("&nbsp", "nbsp");
//	Response.Write("<form action="+ uri + supplier_id +" method=post>");
	Response.Write("<form name=frm method=post>");
	Response.Write("<input type=hidden name=page_id value=" + id + ">");
	Response.Write("<center><h3>  RA FORM TEMPLATE  </h3>");
	Response.Write("<table border=1>");
	Response.Write("<tr><td>");
	Response.Write("");
	Response.Write("</td></tr>");
	Response.Write("<tr>");
	//Response.Write("<td><b>Name : </b><input type=text name=page_name size=30 value=" + m_page + ">");
	Response.Write("<td>");
	Response.Write("<b>SELECT SUPPLIER : </b>");
	
	Response.Write(PrintSupplierOptions(supplier_id,uri));
	//Response.Write("<input type=text name=cat_new size=20></td>");
	uri = Request.ServerVariables["URL"] +"?or=3&sid=";
	Response.Write("&nbsp;&nbsp;&nbsp;| SELECT <b>"+ m_sCompanyName.ToUpper() +"</b> RMA FORM: ");
	Response.Write(PrintCompanyOptions(supplier_id,uri));
	//Response.Write("<input type=text name=cat_new size=20></td>");
	if(Request.QueryString["or"] != null && Request.QueryString["or"] != "")
	{
		Response.Write(" | SELECT TYPE :<select name=template_type><option value=1");
		if(m_template_type == "1")
			Response.Write(" selected ");
		Response.Write(">Repair</option><option value=2");
		if(m_template_type == "2")
			Response.Write(" selected ");
		Response.Write(">Finish</option></select>");
	}
	else
		Response.Write(" <input type=hidden name=template_type value=0>");
	Response.Write("</td>");
	Response.Write("</tr><tr>");
	Response.Write("<td><textarea name=txt rows=30 cols=150>");
	Response.Write(text);
	Response.Write("</textarea>");
	Response.Write("<tr><td align=center>");
	Response.Write("<input type=submit name=cmd value=' Save '>");
	Response.Write("<input type=button value=Cancel onclick=window.location=('"+ Request.ServerVariables["URL"] +"')>");
	Response.Write("&nbsp&nbsp&nbsp&nbsp;");
	Response.Write("<input type=checkbox name=del_confirm>Tick to confirm deletion <input type=submit name=cmd value=Delete>");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");
}

string CreateRepairCardID(int nType)
{
	string company = m_sCompanyName;
	string email = "";
	DataSet ds = new DataSet();
	if(nType == 1)
		company += " Repair Form";
	if(nType == 2)
		company += " Repair Finish Form";
	
	string sc = " IF NOT EXISTS (SELECT id FROM card WHERE company =";
	if(nType == 1)
		sc += "'company_id_for_repair_form' )";
	else
		sc += "'company_id_for_repair_finish_form' )";
	sc += " INSERT INTO card (company, name, trading_name, type, email) ";
	sc += " VALUES ('"+ company +"', '"+ company +"', '"+ company +"', 5, '"+ company +"' )";
	
//DEBUG("sc = ", sc);
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
		return "";
	}

	string card_id = "";
	sc = " SELECT top 1 id FROM card where company = '"+ company +"' ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(ds, "rma_form") == 1)
			 card_id = ds.Tables["rma_form"].Rows[0]["id"].ToString();
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	
	sc = " UPDATE settings SET value = "+ card_id +" WHERE 1=1 ";
	if(nType == 1)
		sc += " AND name = 'company_id_for_repair_form' ";
	if(nType == 2)
		sc += " AND name = 'company_id_for_repair_finish_form' ";
//DEBUG("sc = ", sc);
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
		return "";
	}
		
	return card_id;

}


string PrintCompanyOptions(string current_id, string uri)
{
	m_repair_card_id = GetSiteSettings("company_id_for_repair_form");
	m_repair_finish_card_id = GetSiteSettings("company_id_for_repair_finish_form");
	if(m_repair_card_id == "" || m_repair_card_id == null)
		m_repair_card_id = CreateRepairCardID(1);
	if(m_repair_finish_card_id == "" || m_repair_finish_card_id == null)
		m_repair_finish_card_id = CreateRepairCardID(2);

	DataSet dssup = new DataSet();
	string type_supplier = GetEnumID("card_type", "others");
	int rows = 0;
	string sc = " SELECT id, short_name, name, email, company ";
	sc += " FROM card WHERE type=" + type_supplier + " ";
	//sc += " WHERE 1 = 1";
	sc += " AND (id = "+ m_repair_card_id +" OR id = "+ m_repair_finish_card_id +") ";
	sc += " ORDER BY company";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dssup, "others");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	string s = "\r\n<select name=other";
	if(uri != "")
		s += " onchange=\"window.location=('" + uri + "'+this.options[this.selectedIndex].value)\"";
	s += "><option value=''>All Others</option>";
	for(int i=0; i<rows; i++)
	{
		string id = dssup.Tables["others"].Rows[i]["id"].ToString();
		string name = dssup.Tables["others"].Rows[i]["company"].ToString();
		if(name == "")
			name = dssup.Tables["others"].Rows[i]["name"].ToString();
		if(name == "")
			name = dssup.Tables["others"].Rows[i]["short_name"].ToString();
		s += "<option value=" + dssup.Tables["others"].Rows[i]["id"].ToString();
		if(current_id == id)
			s += " selected";
		s += ">" + name + "</option>\r\n";
	}
	s += "\r\n</select>";
	return s;
}

</script>

<asp:Label id=LFooter runat=server/>
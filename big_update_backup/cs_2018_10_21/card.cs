<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_cardType = "2";
string m_job = "";

string m_sOrderBy = "trading_name";
bool m_bDescent = false; //order by xxx DESC
bool m_bRestrict = false;
int m_members = 0;

string tableWidth ="97%";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	string sstrict = GetSiteSettings("secure_employee_card", "0");
	if(MyBooleanParse(sstrict))
		m_bRestrict = true;
	if(m_bRestrict)
	{
		//string al =Session[m_sCompanyName + "AccessLevel"].ToString();
		//string GetSecSetting = GetSiteSettings("SET_ALLOW_ACCESS_ID_FOR_CARD_AND_OTHER_SECURITIES");
	//	if(al == "10" || al == "8") // GetSecSetting)
	 //  int s_lAccess = int.Parse(GetSiteSettings("SET_ALLOW_ACCESS_ID_FOR_CARD_AND_OTHER_SECURITIES").ToString());
//DEBUG("Access ", s_lAccess);
	   if(int.Parse(Session[m_sCompanyName+"AccessLevel"].ToString()) >=8)
			m_bRestrict = false; //administrator or manager have access
	     
	}
          
	if(Request.QueryString["type"] != null)
		m_cardType = Request.QueryString["type"];

	if(m_bRestrict)
	{
		if(m_cardType != null && m_cardType != "" && MyIntParse(m_cardType) > 2)
		{
			Response.Write("<h3>ACCESS DENIED");
			return;
		}
	}

	if(Request.QueryString["j"] != null)
		m_job = Request.QueryString["j"];

	if(Request.QueryString["ob"] != null)
	{
		m_sOrderBy = Request.QueryString["ob"];
		Session["card_list_order_by"] = m_sOrderBy;
	}
	else if(Session["card_list_order_by"] != null)
		m_sOrderBy = Session["card_list_order_by"].ToString();
	
	if(Request.QueryString["desc"] == "1")
		m_bDescent = true;

	if(m_cardType == "-1")
	{
		if(!GetExtraLogins())
			return;
	}
	else if(m_cardType == "-2")
	{
		if(!GetBranchLogins())
			return;
	}
	else if(!DoSearch())
	{
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();
	
	string sType = "Card List -- <font color=red>";//ALL</font>";
	if(m_cardType == "-1")
		sType += "Extra Logins";
	else if(m_cardType != "")
		sType += GetEnumValue("card_type", m_cardType).ToUpper();
	else
		sType += "ALL";
	sType += "</font>";
	
/*	Response.Write("<br><center><h3>" + sType + "</h3></center>");
//	Response.Write("<br><img border=0 src='/i/cf.gif'>");
	Response.Write("<table width=100%>");
*/
	Response.Write("<br><table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=3><font size=+1><b>" + sType + "</b><font color=red><b>");
	Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");	


	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=3><br></td></tr>");
	Response.Write("<tr><td width=30%>");
	Response.Write("<b>Member in this class : </b>" + m_members.ToString() + "</td>");
	Response.Write("<td width=70% align=right>");
	
	DataSet dsEnum = new DataSet();
	string sc = "SELECT id, name FROM enum WHERE class='card_type' and id NOT IN (6,7)";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dsEnum, "enum");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return;
	}
	
	string s_href = "";
	//if(Request.QueryString["approved"] != null && Request.QueryString["approved"] == "0")
	//	s_href = "<a href=card.aspx?approved=0&type=";
	//else
		s_href = "<a href=card.aspx?type=";
	for(int i=0; i<dsEnum.Tables["enum"].Rows.Count; i++)
	{
		string id = dsEnum.Tables["enum"].Rows[i]["id"].ToString();
		string name = dsEnum.Tables["enum"].Rows[i]["name"].ToString().ToUpper();
		if(m_bRestrict)
		{
			if(name == "CUSTOMER" || name == "DEALER")
			{
				Response.Write("<img src=r.gif>" + s_href + id + ">" + name + "</a>&nbsp;&nbsp;&nbsp;");
			}
		}
		else
			Response.Write("<img src=r.gif>" + s_href + id + ">" + name + "</a>&nbsp;&nbsp;&nbsp;");
	}
//	Response.Write("<img src=r.gif><a href=card.aspx?type=-1>EXTRALOGIN</a>&nbsp;&nbsp;&nbsp;");
//	Response.Write("<img src=r.gif>");
//	Response.Write("<a href=card.aspx?type=-2>BRANCH LOGIN</a>&nbsp;&nbsp;&nbsp;");
	if(!m_bRestrict)
		Response.Write("<img src=r.gif>" + s_href + ">ALL</a>&nbsp;&nbsp;&nbsp;");

	Response.Write("</td></tr></table>");
	displayForm();
	
	if(!IsPostBack)
	{
		if(m_job == "payment")
			BindGridPayment();
		else
			BindGrid();
	}
	LFooter.Text = m_sAdminFooter;
	
}
void displayForm()
{
	string type = "";
	if(Request.QueryString["type"] != null && Request.QueryString["type"] != "")
		type = Request.QueryString["type"].ToString();

	Response.Write("<form name=form1 method=post action=card.aspx?type="+ type +" id=form1>");
	Response.Write("<table width="+ tableWidth +" align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");	
	Response.Write("<tr><td width=30%>");

	Response.Write("<input type=button value='Add New' "+ Session["button_style"] +"");
	switch (type)
	{
		case "1":
		   Response.Write(" onclick=\"javascript:ecard_window=window.open('ecard.aspx?n=customer&a=new', '', '');\">");
		    break;
		case "2":
		   Response.Write(" onclick=\"javascript:ecard_window=window.open('ecard.aspx?n=dealer&a=new', '', '');\">");
			break;
		case "3":
			Response.Write(" onclick=\"javascript:ecard_window=window.open('ecard.aspx?n=supplier&a=new', '', '');\">");
			break;
		case "4":
			Response.Write(" onclick=\"javascript:ecard_window=window.open('ecard.aspx?n=employee&a=new', '', '');\">");
			break;
		case "6":
			Response.Write(" onclick=\"javascript:ecard_window=window.open('ecard.aspx?t=member', '', '');\">");
			break;
		case "-2":
		   Response.Write(" onclick=\"javascript:ecard_window=window.open('ecard.aspx?n=branch_login&a=new', '', '');\">");
			break;
	}
	Response.Write("<input type=button value='Show ALL' "+ Session["button_style"] +"");
	Response.Write(" onclick=\"window.location=('card.aspx?');\">");
	Response.Write("<input type=hidden name=t value=search>");
	Response.Write("&nbsp;&nbsp;&nbsp;<select name='select_card'>");
	Response.Write("<option value='all'>All</option><option value='name'>Name</option><option value='id'>ID</option><option value='phone'>Phone</option>");
	Response.Write("<option value='email'>Email</option><option value='trading_name'>Trading Name</option><option value='company'>Company</option>");
	Response.Write("</select><input type=text name=keyword>"); 
	Response.Write("<input type=submit value=' Search '"+ Session["button_style"] +">");
	Response.Write("</form>");
}

bool CheckAccountType(string card_id)
{
	DataSet acDss = new DataSet();
	if(acDss.Tables["type"] != null)
		acDss.Tables["type"].Clear();

	bool bisValid = false;
	if(card_id == "")
		return false;
	if(!TSIsDigit(card_id))
		return false;

	string sc = " SELECT access_level FROM card WHERE id = '" + card_id +"' AND type = 4";
//DEBUG("sc =", sc);	
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(acDss, "type") == 1)
			if(MyIntParse(acDss.Tables["type"].Rows[0]["access_level"].ToString()) >= MyIntParse(GetSiteSettings("set_privilege_on_access_cardlist", "5", false)))
				bisValid = true;

	}
	catch(Exception e) 
	{
	//	ShowExp(sc, e);
		return false;
	}
	return bisValid;
}


Boolean DoSearch()
{
	string sortDescent = m_bDescent?"&desc=0":"&desc=1";	
	string r = sortDescent + "&r=" + DateTime.Now.ToOADate();
	string sc = "";
	
	if(m_job == "payment")
	{
		sc = "SELECT id, name, short_name, trading_name, email, phone, trans_total, balance, access_level, discount, note FROM card ";
		sc += " ORDER BY balance DESC, name, register_date";	 
	}
	else
	{
		//if(Request.QueryString["t"] == "search")
		if(Request.Form["keyword"] != null && Request.Form["keyword"] != "")
		{
			//string kw = Request.QueryString["keyword"];
			string kw = Request.Form["keyword"].ToString();
			//DEBUG(" kw = ", kw);
			string s_card = "";
			//if(Request.QueryString["select_card"] != null && Request.QueryString["select_card"] != "")
			//	s_card = Request.QueryString["select_card"].ToString();
			if(Request.Form["select_card"] != null && Request.Form["select_card"] != "")
				s_card = Request.Form["select_card"].ToString();
			//DEBUG("sc ard= ", s_card);
			bool isNum = true;
			int ptr = 0;
			while (ptr < kw.Length)
			{
				if (!char.IsDigit(kw, ptr++))
				{
					isNum = false;
					break;
				}
			}
			//DEBUG("isnum = ", isNum.ToString());
			sc = "SELECT DISTINCT barcode, id, name, trading_name, email, phone, access_level, note ";
		
			sc += ", '<a href=ecard.aspx?t=member&id=' + LTRIM(STR(id)) + ' class=o target=new>' + STR(points) + '</a>' AS '<a href=card.aspx?ob=points" + r + "><font color=white>Points</font></a>' ";	
		if(m_sSite.ToLower() == "admin" && CheckAccountType(Session["card_id"].ToString()))
				sc += ", '<a href=ecard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + " class=o>Edit</a>' AS Action ";
			//sc += ", '<a href=\"javascript:viewcard_window=window.open(''viewcard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=400, height=650, resizable=1''); viewcard_window.focus()\" class=o>View</a>' AS ACTION ";
			sc += ", '<a href=\"javascript:viewcard_window=window.open(''ecard.aspx?id='+LTRIM(STR(id)) + '&v=view&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=600, height=650, resizable=1,scrollbars=1''); viewcard_window.focus()\" class=o >View</a>' AS ACTION ";
			
			sc += " FROM card ";
			sc += " WHERE ( 1=1 ";
			if(kw != "")
			{
				kw = EncodeQuote(kw);
				if(s_card == "id")
				{
					if(isNum)
					{
						sc += " AND id = '" + kw + "' ";
						//sc += " OR phone LIKE '%"+ kw +"%' ";
					}
					else
						sc += " ";
				}
				if(s_card == "name")
					sc += " AND name LIKE '%" + kw + "%' ";
				if(s_card == "company")
					sc += " AND company LIKE '%" + kw + "%' ";
				if(s_card == "trading_name")
					sc += " AND trading_name LIKE '%" + kw + "%' ";
				if(s_card == "email")
					sc += " AND email LIKE '%" + kw + "%'";
				if(s_card == "phone")
					sc += " AND phone like '%"+ kw +"%' ";
				if(s_card == "all")
				{
					sc += "AND name LIKE '%" + kw + "%' OR company LIKE '%" + kw + "%' OR email LIKE '%" + kw + "%' OR phone like '%"+ kw +"%' OR trading_name LIKE '%" + kw + "%'";
					if(isNum)
					{
						sc += " OR id = '" + kw + "' ";
						sc += " OR phone LIKE '%"+ kw +"%' ";
					}
				}
				//sc += " AND REPLACE(phone, '-', '') LIKE '%"+ kw +"%' ";
			}
			sc += " ) AND type <> 0 ";
            if(Session["login_card_id"].ToString()!="9417" && Session["login_card_id"].ToString()!="1057")
                sc += " AND id<>'9417' AND id<>'1057' ";
			if(Session["branch_support"] != null)
			{
				if(!bSecurityAccess(Session["card_id"].ToString()))
				{
					sc += " AND our_branch = "+ Session["branch_id"].ToString();
				}					
			}
			if(m_bRestrict)
				sc += " AND type IN (1, 2) ";
			sc += " ORDER BY name";
			if(m_cardType == "0")//personal
			{
				sc = "SELECT id AS '<a href=card.aspx?ob=id&type=" + m_cardType + "><font color=white>ID</font></a>' ";
				sc += ", trading_name AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + "><font color=white>COMPANY</font></a>' ";
				sc += ", name AS '<a href=card.aspx?ob=name&type=" + m_cardType + "><font color=white>NAME</font></a>' ";
				sc += ", phone AS '<a href=card.aspx?ob=phone&type=" + m_cardType + "><font color=white>PHONE</font></a>' ";
				sc += ", fax AS '<a href=card.aspx?ob=fax&type=" + m_cardType + "><font color=white>FAX</font></a>' ";
				sc += ", address1 AS '<a href=card.aspx?ob=address1&type=" + m_cardType + "><font color=white>ADDRESS</font></a>' ";
				sc += ", address2 AS '<a href=card.aspx?ob=address2&type=" + m_cardType + "><font color=white>ADDRESS</font></a>' ";
				sc += ", address3 AS '<a href=card.aspx?ob=address3&type=" + m_cardType + "><font color=white>ADDRESS</font></a>' ";
				if(m_sSite.ToLower() == "admin" && CheckAccountType(Session["card_id"].ToString()))
					sc += ", '<a href=ecard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + " class=o>Edit</a>' AS ACTION ";
				//sc += ", '<a href=\"javascript:viewcard_window=window.open(''viewcard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=400, height=650, resizable=1''); viewcard_window.focus()\" class=o>View</a>' AS ACTION ";
				sc += ", '<a href=\"javascript:viewcard_window=window.open(''ecard.aspx?id='+LTRIM(STR(id)) + '&v=view&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=600, height=650, resizable=1,scrollbars=1''); viewcard_window.focus()\" class=o >View</a>' AS ACTION ";
				
				sc += " FROM card ";
				sc += " WHERE ( 1=1 ";
				if(kw != "")
				{
					kw = EncodeQuote(kw);
					if(s_card == "id")
					{
						if(isNum)
						{
							sc += " AND id = '" + kw + "' ";
							//sc += " OR phone LIKE '%"+ kw +"%' ";
						}
						else
							sc += " ";
					}
					if(s_card == "name")
						sc += " AND name LIKE '%" + kw + "%' ";
					if(s_card == "company")
						sc += " AND company LIKE '%" + kw + "%' ";
					if(s_card == "trading_name")
						sc += " AND trading_name LIKE '%" + kw + "%' ";
					if(s_card == "email")
						sc += " AND email LIKE '%" + kw + "%'";
					if(s_card == "phone")
						sc += " AND phone like '%"+ kw +"%' ";
					if(s_card == "all")
					{
						sc += "AND name LIKE '%" + kw + "%' OR company LIKE '%" + kw + "%' OR email LIKE '%" + kw + "%' OR phone like '%"+ kw +"%' OR trading_name LIKE '%" + kw + "%'";
						if(isNum)
						{
							sc += " OR id = '" + kw + "' ";
							sc += " OR phone LIKE '%"+ kw +"%' ";
						}
					}
					sc += ") AND type=0 AND personal_id = " + Session["card_id"].ToString(); //only display current users personal cards
					if(Session["branch_support"] != null)
					{
						if(!bSecurityAccess(Session["card_id"].ToString()))
						{
							sc += " AND our_branch = "+ Session["branch_id"].ToString();
						}					
					}
				}
			}
		}
		else
		{
			if(Request.QueryString["approved"] != null && Request.QueryString["approved"] == "0")
			{
				sc = "SELECT id, trading_name AS 'Legal Name', trading_name AS 'Trading Name', name AS 'Purchase Manager', ";
				sc += " email AS 'PM_Email', address1 AS 'Physical Addr', Phone, Fax, register_date AS 'Date', Approved, ";
				sc += " '<a href=ecard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + " class=o>Process</a>' AS Action ";
				sc += " FROM card WHERE approved=0 AND type=2 ";
				sc += " ORDER BY register_date DESC";	 
//				if(m_cardType != "")
//					sc += " AND type='" + m_cardType + "'";
			}
			else
			{
				string month = DateTime.Now.Month.ToString();
				string fname = "m" + month;
				
				sc = "SELECT c.barcode AS Barcode";
				sc +=", c.id AS '<a href=card.aspx?ob=id&type=" + m_cardType + r + "><font color=white>ACC#</font></a>' ";
				if(m_cardType != "6")
				{  
				   if(m_cardType =="4"){
					sc += ", c.trading_name AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + r + "><font color=white>Position </font></a>' ";
				sc += ", b.name AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + r + "><font color=white>Branch</font></a>' ";
					//sc += ", company AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + r + "><font color=white>Branch</font></a>' ";
					}else{
					sc += ", c.trading_name AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + r + "><font color=white>Trade Name </font></a>' ";
					sc += ", c.company AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + r + "><font color=white>Company</font></a>' ";}
				}
				if(m_cardType !="3"){
				sc += ", c.name AS '<a href=card.aspx?ob=name&type=" + m_cardType + r + "><font color=white>Name</font></a>' ";
				}
				if(m_cardType == "6")
				{
					sc += ", c.phone AS '<a href=card.aspx?ob=phone&type=" + m_cardType + r + "><font color=white>Phone</font></a>' ";
					sc += ", '<a href=ecard.aspx?t=member&id=' + LTRIM(STR(c.id)) + ' class=o target=new>' + STR(points) + '</a>' AS '<a href=card.aspx?ob=points" + r + "><font color=white>Points</font></a>' ";
				}
//				sc += ", email AS '<a href=card.aspx?ob=email" + r + "><font color=white>EMAIL</font></a>' ";
//				sc += ", phone AS '<a href=card.aspx?ob=phone" + r + "><font color=white>PHONE</font></a>' ";
//				sc += ", trans_total AS '<a href=card.aspx?ob=trans_total" + r + "><font color=white>TRANS_TOTAL</font></a>' ";
//				sc += ", ROUND(purchase_average, 2) AS '<a href=card.aspx?ob=purchase_average&type=" + m_cardType + r + "><font color=white>PUR_AVE</font></a>' ";
//				sc += ", ROUND(" + fname + ", 2) AS '<a href=card.aspx?ob=" + fname + "&type=" + m_cardType + r + "><font color=white>THIS MONTH</font></a>' ";
//				sc += ", ROUND(balance, 2) AS '<a href=card.aspx?ob=balance&type=" + m_cardType + r + "><font color=white>BALANCE</font></a>' ";
//				sc += ", credit_limit AS '<a href=card.aspx?ob=credit_limit&type=" + m_cardType + r + "><font color=white>CREDIT_LIMIT</font></a>' ";
				if(m_sSite.ToLower() == "admin" && CheckAccountType(Session["card_id"].ToString()))
				{
					sc += ", '<a href=ecard.aspx?id='+LTRIM(STR(c.id)) + '&r=" + DateTime.Now.ToOADate() + " class=o>Edit</a>' AS ACTION ";
				}
				//sc += ", '<a href=\"javascript:viewcard_window=window.open(''viewcard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=400, height=650, resizable=1''); viewcard_window.focus()\" class=o>View</a>' AS ACTION ";
				sc += ", '<a href=\"javascript:viewcard_window=window.open(''ecard.aspx?id='+LTRIM(STR(c.id)) + '&v=view&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=600, height=650, resizable=1,scrollbars=1''); viewcard_window.focus()\" class=o>View</a>' AS ACTION ";
				
				sc += " FROM card c JOIN branch b on c.our_branch =b.id   WHERE 1=1 AND c.main_card_id IS NULL ";
                if(Session["login_card_id"].ToString()!="9417" && Session["login_card_id"].ToString()!="1057")
                    sc += " AND c.id<>'9417' AND c.id<>'1057' ";
				if(Session["branch_support"] != null)
				{
				if(!bSecurityAccess(Session["card_id"].ToString()))
					//if(m_bRestrict)
				{
						sc += " AND our_branch = "+ Session["branch_id"].ToString();
					   
				}					
				}
				if(m_cardType != "")
				{
					sc += " AND type='" + m_cardType + "' ";
				}
				//sc += " ORDER BY " + m_sOrderBy;	
				if (m_cardType =="4"){
				 sc +="ORDER BY b.name";
				 }else{ 
				 sc +="ORDER BY c.id";
				 }
				if(m_bDescent)
					sc += " DESC";
					
				if(m_cardType == "0")//personal
				{
					sc = "SELECT id AS '<a href=card.aspx?ob=id&type=" + m_cardType + "><font color=white>ID</font></a>' ";
					sc += ", trading_name AS '<a href=card.aspx?ob=trading_name&type=" + m_cardType + "><font color=white>COMPANY</font></a>' ";
					sc += ", name AS '<a href=card.aspx?ob=name&type=" + m_cardType + "><font color=white>NAME</font></a>' ";
					sc += ", phone AS '<a href=card.aspx?ob=phone&type=" + m_cardType + "><font color=white>PHONE</font></a>' ";
					sc += ", fax AS '<a href=card.aspx?ob=fax&type=" + m_cardType + "><font color=white>FAX</font></a>' ";
					sc += ", address1 AS '<a href=card.aspx?ob=address1&type=" + m_cardType + "><font color=white>ADDRESS</font></a>' ";
					sc += ", address2 AS '<a href=card.aspx?ob=address2&type=" + m_cardType + "><font color=white>ADDRESS</font></a>' ";
					sc += ", address3 AS '<a href=card.aspx?ob=address3&type=" + m_cardType + "><font color=white>ADDRESS</font></a>' ";
					if(m_sSite.ToLower() == "admin" && CheckAccountType(Session["card_id"].ToString()))
						sc += ", '<a href=ecard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + " class=o>Edit</a>' AS ACTION ";
				
					//sc += ", '<a href=\"javascript:viewcard_window=window.open(''viewcard.aspx?id='+LTRIM(STR(id)) + '&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=400, height=650, resizable=1''); viewcard_window.focus()\" class=o>View</a>' AS ACTION ";
					sc += ", '<a href=\"javascript:viewcard_window=window.open(''ecard.aspx?id='+LTRIM(STR(id)) + '&v=view&r=" + DateTime.Now.ToOADate() + "'', '''' , ''width=600, height=650, resizable=1,scrollbars=1''); viewcard_window.focus()\" class=o>View</a>' AS ACTION ";

					sc += " FROM card WHERE 1=1 AND main_card_id IS NULL ";
					if(Session["branch_support"] != null)
					{
						if(!bSecurityAccess(Session["card_id"].ToString()))
						{
							sc += " AND our_branch = "+ Session["branch_id"].ToString();
						}					
					}
					sc += " AND type=0 ";
					sc += " AND personal_id = " + Session["card_id"].ToString(); //only display current users personal cards
					sc += " ORDER BY " + m_sOrderBy;	 
					if(m_bDescent)
						sc += " DESC";
				}
			}
		}
	}
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			m_members = myCommand.Fill(ds);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetExtraLogins()
{
	string sortDescent = m_bDescent?"&desc=0":"&desc=1";
	string r = sortDescent + "&type=0";
	string sc = "";

	string month = DateTime.Now.Month.ToString();
	string fname = "m" + month;
	sc = " SELECT c.id AS '<a href=card.aspx?ob=id" + r + "><font color=white>ACCOUNT</font></a>' ";
	sc += ", c1.trading_name AS '<a href=card.aspx?ob=trading_name" + r + "><font color=white>MainBranch</font></a>' ";
	sc += ", c.trading_name AS '<a href=card.aspx?ob=trading_name" + r + "><font color=white>TradingName</font></a>' ";
	sc += ", c.company AS '<a href=card.aspx?ob=company" + r + "><font color=white>Company</font></a>' ";
	sc += ", c.name AS '<a href=card.aspx?ob=name" + r + "><font color=white>Name</font></a>' ";
	sc += ", c.email AS '<a href=card.aspx?ob=email" + r + "><font color=white>Email</font></a>' ";
	sc += ", c.is_branch AS '<a href=card.aspx?ob=is_branch" + r + "><font color=white>Is Branch</font></a>' ";
	sc += ", '<a href=ecard.aspx?id=' + LTRIM(STR(c.id)) + ' target=_blank class=o>Edit</a>' AS ACTION ";
	sc += ", '<a href=viewcard.aspx?id=' + LTRIM(STR(c.id)) + ' target=_blank class=o>View</a>' AS ACTION ";
	sc += " FROM card c JOIN card c1 ON c1.id = c.main_card_id ";
	sc += " WHERE c.main_card_id IS NOT NULL ";
	sc += " ORDER BY c1." + m_sOrderBy;	 
	if(m_bDescent)
		sc += " DESC";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_members = myCommand.Fill(ds);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool GetBranchLogins()
{
	string sortDescent = m_bDescent?"&desc=0":"&desc=1";
	string r = sortDescent + "&type=-2";
	string sc = "";

	string month = DateTime.Now.Month.ToString();
	string fname = "m" + month;
	sc = " SELECT c.id AS '<a href=card.aspx?ob=id" + r + "><font color=white>"+Lang("Account")+"</font></a>' ";
	sc += ", c.name AS '<a href=card.aspx?ob=name" + r + "><font color=white>"+Lang("Name")+"</font></a>' ";
	sc += ", c.email AS '<a href=card.aspx?ob=email" + r + "><font color=white>"+Lang("Email")+"</font></a>' ";
	sc += ", c.phone AS '<a href=card.aspx?ob=phone" + r + "><font color=white>"+Lang("Phone")+"</font></a>' ";
	if(m_sSite.ToLower() == "admin" && CheckAccountType(Session["login_card_id"].ToString()))
	{
		sc += ", '<a href=ecard.aspx?id='+LTRIM(STR(c.id)) + '";
        sc += "&n=4&r=" + DateTime.Now.ToOADate() + " class=o><font color=#579ecb>'+ N'"+ Lang("Edit") +"</font></a>' AS "+Lang("ACTION")+" ";
	}
	sc += " FROM card c ";
	sc += " WHERE c.type = 4 AND c.is_branch = 1 ";
    if(m_sOrderBy != null && m_sOrderBy != "")
	{
		sc += " ORDER BY c." + m_sOrderBy;	 
		if(m_bDescent)
			sc += " DESC";
	}
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		m_members = myCommand.Fill(ds);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	DataView source = new DataView(ds.Tables[0]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

/////////////////////////////////////////////////////////////////
void BindGridPayment()
{
	DataView source = new DataView(ds.Tables[0]);
	MyDataGridPayment.DataSource = source ;
	MyDataGridPayment.DataBind();
}

void MyDataGridPayment_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGridPayment.CurrentPageIndex = e.NewPageIndex;
	BindGridPayment();
}
</script>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=50
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
</asp:DataGrid>

<asp:DataGrid id=MyDataGridPayment
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=50
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGridPayment_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
	<Columns>
		<asp:HyperLinkColumn
			 HeaderText=PAYMENT
			 DataNavigateUrlField=id
			 DataNavigateUrlFormatString="payment.aspx?ci={0}"
			 Text=PAYMENT
			 Target=_blank/>
	</Columns>

</asp:DataGrid>

</form>

<asp:Label id=LFooter runat=server/>
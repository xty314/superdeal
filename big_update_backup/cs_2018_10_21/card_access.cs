<script runat="server">

DataSet dsca = new DataSet();

string PrintPublicAccessOption(string current_id)
{
	int rows = 0;
	string sc = " SELECT * ";
	sc += " FROM card_access_class ";
	sc += " WHERE main_card_id = " + Session["card_id"];
	sc += " ORDER BY class_id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsca, "level");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	if(rows == 0) //add default
	{
		sc = " INSERT INTO card_access_class (main_card_id, class_id, class_name) VALUES(" + Session["card_id"] + ", 1, 'Manager') ";
		sc += " INSERT INTO card_access_class (main_card_id, class_id, class_name) VALUES(" + Session["card_id"] + ", 2, 'Branch Manager') ";
		sc += " INSERT INTO card_access_class (main_card_id, class_id, class_name) VALUES(" + Session["card_id"] + ", 3, 'Sales') ";
		sc += " INSERT INTO card_access_class (main_card_id, class_id, class_name) VALUES(" + Session["card_id"] + ", 4, 'Accountant') ";
		sc += " INSERT INTO card_access_class (main_card_id, class_id, class_name) VALUES(" + Session["card_id"] + ", 5, 'Technician') ";
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
			return "";
		}
	}
	
	sc = " SELECT * ";
	sc += " FROM card_access_class ";
	sc += " WHERE main_card_id = " + Session["card_id"];
	sc += " ORDER BY class_id ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsca, "level");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	StringBuilder sb = new StringBuilder();
	sb.Append("<select name=level>");
	int i = 0;
	if(Session["customer_access_level"].ToString() == "2") //branch manager logged in
		i = 1; //skip manager
	for(; i<rows; i++)
	{
		DataRow dr = dsca.Tables["level"].Rows[i];
		string id = dr["class_id"].ToString();
		string name = dr["class_name"].ToString();
		sb.Append("<option value=" + id);
		if(id == current_id)
			sb.Append(" selected");
		sb.Append(">" + name + "</option>");
	}
	return sb.ToString();
}

string PrintLoginBranchOption(string current_id)
{
	string main_card_id = Session["card_id"].ToString();
	if(Session["main_card_id"].ToString() != "")
		main_card_id = Session["main_card_id"].ToString();

	int rows = 0;
	string sc = " SELECT id, trading_name ";
	sc += " FROM card ";
	sc += " WHERE main_card_id = " + main_card_id + " AND is_branch=1 ";
	sc += " ORDER BY trading_name ";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dsca, "branch");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	StringBuilder sb = new StringBuilder();
	sb.Append("<select name=branch>");
	sb.Append("<option value=" + main_card_id + ">Main Branch</option>");
	int i = 0;
	for(; i<rows; i++)
	{
		DataRow dr = dsca.Tables["branch"].Rows[i];
		string id = dr["id"].ToString();
		string name = dr["trading_name"].ToString();
		sb.Append("<option value=" + id);
		if(id == current_id)
			sb.Append(" selected");
		sb.Append(">" + name + "</option>");
	}
	return sb.ToString();
}

string ApplyCustomerAccessLevel(string sin)
{
	//testing
//	Session["main_card_id"] = "9";
//	Session["customer_access_level"] = "3";
//	Session["main_card_id"] = "";

	//clean up
	Session["no_access_statement.aspx"] = null;
	Session["no_access_checkout.aspx"] = null;
	Session["no_access_c.aspx"] = null;
	Session["no_access_status.aspx"] = null;
	Session["no_access_repairform.aspx"] = null;
	Session["no_access_rmastatus.aspx"] = null;
	Session["no_access_repairform.aspx"] = null;
	Session["no_access_pl.aspx"] = null;
	Session["no_access_cart.aspx"] = null; 

	if(Session["main_card_id"] == null || Session["main_card_id"].ToString() == "")
		return sin;

	DataSet ds2 = new DataSet();
	int rows = 0;
	string sc = " SELECT m.menu ";
	sc += " FROM card_access_menu m JOIN card_access_data d ON m.id = d.no_access_menu_id ";
	sc += " WHERE d.main_card_id = " + Session["main_card_id"];
	sc += " AND d.class_id = " + Session["customer_access_level"];
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds2, "data");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	if(rows <= 0) //get default data
	{
		sc = " SELECT m.menu ";
		sc += " FROM card_access_menu m JOIN card_access_data_default d ON m.id = d.no_access_menu_id ";
		sc += " WHERE d.class_id = " + Session["customer_access_level"];
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(ds2, "data");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return "";
		}
	}

	//additional menu
	sc = " SELECT 'register.aspx?t=branch' AS 'menu'";
	sc += " UNION ";
	sc += " SELECT 'cart.aspx' AS 'menu'"; //price list
	sc += " UNION ";
	sc += " SELECT 'pl.aspx' AS 'menu'"; //price list
	if(MyIntParse(Session["customer_access_level"].ToString()) > 2)
	{
		sc += " UNION ";
		sc += " SELECT 'register.aspx?t=logins' AS 'menu'";
	}
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds2, "data");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}

	string s = sin;
	for(int i=0; i<ds2.Tables["data"].Rows.Count; i++)
	{
		string menu = ds2.Tables["data"].Rows[i]["menu"].ToString();
		Session["no_access_" + menu] = true;
//DEBUG("menu=", menu);
//DEBUG("no=no_access_"+menu, "true");
		int p = s.IndexOf(menu);
//DEBUG("p=", p);
		if(p >= 0)
		{
			int m = p;
			int n = p;
			while(m >= 5) //get menu row
			{
				m--;
				if(menu == "cart.aspx")
				{
					if(s[m] == '<' && s[m+1] == 'a')
						break;
				}
				else
				{
					if(s[m] == '\n' && s[m+1] == '<' && s[m+2] == 't' && s[m+3] == 'r' && s[m+4] == '>')
						break;
				}
			}
			while(n < s.Length)
			{
				n++;
				if(menu == "cart.aspx")
				{
					if(s[n-2] == 'a' && s[n-1] == '>' && s[n] == '&')
						break;
				}
				else
				{
					if(s[n-2] == 'r' && s[n-1] == '>' && s[n] == '\r')
						break;
				}
			}
			if(menu == "cart.aspx")
			{
				if(Session["no_access_c.aspx"] != null || Session["no_access_checkout.aspx"] != null) //if cant check out then remove View Cart
				{
					s = s.Substring(0, m) + s.Substring(n, s.Length - n); //remove this menu
				}
				else
					Session["no_access_cart.aspx"] = null; //enable view cart
			}
			else if(menu == "pl.aspx")
			{
				if(Session["no_access_c.aspx"] != null) //if cant see catalog then can download price list
				{
					s = s.Substring(0, m) + s.Substring(n, s.Length - n); //remove this menu
				}
				else
					Session["no_access_pl.aspx"] = null; //enable download price list
			}
			else if(menu == "c.aspx")
			{
				s = s.Replace("@@category", "");
			}
			else
				s = s.Substring(0, m) + s.Substring(n, s.Length - n); //remove this menu
		}
	}

	//for branch manager
	string u = "";
	//change update account to edit branch
	if(Session["main_card_id"] != null) //not manager logged in
	{
		if(Session["branch_card_id"].ToString() == Session["login_card_id"].ToString()) //branch manager logged in
		{
			u = "<tr><td>&nbsp&nbsp;<a href=register.aspx?t=branch&a=edit&id=";
			u += Session["login_card_id"] + " class=d>Update Branch Details</a></td></tr>";
		}
		string menu = "update my account"; //remove update account menu
		int p = s.ToLower().IndexOf(menu);
		if(p >= 0)
		{
			int m = p;
			int n = p;
			while(m >= 5) //get "update my account" menu row, prepare to remove it
			{
				m--;
				if(s[m] == '\n' && s[m+1] == '<' && s[m+2] == 't' && s[m+3] == 'r' && s[m+4] == '>')
					break;
			}
			while(n < s.Length)
			{
				n++;
				if(s[n-2] == 'r' && s[n-1] == '>' && s[n] == '\r')
					break;
			}
			s = s.Substring(0, m) + u + s.Substring(n, s.Length - n); //remove this menu
		}
	}
	return s;
}

string BlockSysQuote(string sMenu)
{
	string s = sMenu;
	string u = "";
	
	if(Session["card_id"] == null)
		return sMenu;

	DataRow dr = GetCardData(Session["card_id"].ToString());
	if(dr == null)
		return sMenu;

	bool bNoQuote = false;
	if(dr["type"].ToString() == "3") //supplier
		bNoQuote = true;
	if(bool.Parse(dr["no_sys_quote"].ToString()))
		bNoQuote = true;
	if(bNoQuote) 
	{
		Session["no_access_q.aspx"] = true;
		Session["no_access_quotation.aspx"] = true;
		Session["no_access_qcb.aspx"] = true;

		string menu = "quotation.aspx"; //remove update account menu
		int p = s.ToLower().IndexOf(menu);
		if(p >= 0)
		{
			int m = p;
			int n = p;
			while(m >= 5) //get menu row, prepare to remove it
			{
				m--;
				if(s[m] == '\n' && s[m+1] == '<' && s[m+2] == 't' && s[m+3] == 'd' && s[m+4] == '>')
//				if(s[m] == '<' && s[m+1] == 't' && s[m+2] == 'd' && s[m+3] == '>')
					break;
			}
			while(n < s.Length)
			{
				n++;
				if(s[n-2] == 'd' && s[n-1] == '>' && s[n] == '\r')
					break;
			}
			s = s.Substring(0, m) + s.Substring(n, s.Length - n); //remove this menu
		}
	}
	return s;
}

bool CanPlaceOrder(string card_id)
{
	DataRow drc = GetCardData(card_id);
	if(drc == null)
	{
		Response.Write("<br><center><h3>Account # " + card_id + " not found</h3>");
		return false; //not found?
	}

	string main_card_id = drc["main_card_id"].ToString();
	if(main_card_id == "")
		return true; //account manager

	string access_level = drc["customer_access_level"].ToString();

	DataSet ds2 = new DataSet();
	int rows = 0;
	string sc = " SELECT m.menu ";
	sc += " FROM card_access_menu m JOIN card_access_data d ON m.id = d.no_access_menu_id ";
	sc += " WHERE d.main_card_id = " + main_card_id;
	sc += " AND d.class_id = " + access_level;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(ds2, "data");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0) //get default data
	{
		sc = " SELECT m.menu ";
		sc += " FROM card_access_menu m JOIN card_access_data_default d ON m.id = d.no_access_menu_id ";
		sc += " WHERE d.class_id = " + access_level;
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(ds2, "data");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
	}

	for(int i=0; i<ds2.Tables["data"].Rows.Count; i++)
	{
		string menu = ds2.Tables["data"].Rows[i]["menu"].ToString();
		if(menu == "checkout.aspx")
			return false;
	}
	return true;
}
</script>

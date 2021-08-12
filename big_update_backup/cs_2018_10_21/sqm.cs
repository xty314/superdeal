<!-- #include file="kit_fun.cs" -->

<script runat=server>

DataSet ds = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_type = "mb";
string m_parent = "";
string m_cols = "3";
string m_set = null;
void Page_Load(Object Src, EventArgs E )
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["t"] != null && Request.QueryString["t"] != "")
		m_type = Request.QueryString["t"];

	if(!DoSearch())
		return;

	if(Request.QueryString["set"] != null)
	{
		m_set = Request.QueryString["set"];
	}
//DEBUG("m_set=", m_set);
	if(Request.QueryString["a"] == "del")
	{
		if(DoDelete())
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&r=" + DateTime.Now.ToOADate() + "\">");
		return;
	}

	if(Request.Form["cmd"] != null)
	{
		string cmd = Request.Form["cmd"];
		if(cmd == "Add")
		{
			if(DoAdd())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=" + m_type + "&set=" + m_set + "&r=" + DateTime.Now.ToOADate() + "\">");
			return;
		}
		else if(cmd == "Apply")
		{
			SetQCat(m_type, Request.Form["cat"]);
		}
	}

	TS_PageLoad(); //do common things, LogVisit etc...

	PrintAdminHeader();
	PrintAdminMenu();

	Response.Write("<br><center><font size=+1><b>System Quotation Manager - " + m_type + "</b></font></center><br>");
	//Response.Write("<br><img border=0 src='../../i/sqm.gif'> <b><font size=4><center>");
	
	//Response.Write("- " + m_type + " -</b></font></center>\r\n");

	BindGrid();
//DEBUG("m_parent=", m_parent);
//		if(m_parent == "")
		PrintAddNewField();
//	else
//		PrintAddNewFieldWithParent();
	LFooter.Text = m_sAdminFooter;
}

bool QExists(string code, string parent)
{
	for(int i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		if(parent != null)
		{
			if(dr["parent"].ToString() != parent)
				continue;
		}
		if(dr["code"].ToString() == code)
			return true;
	}
	return false;
}

bool DoSearch()
{
	if(ds != null)
		ds.Clear();
	string sc = "";
	m_parent = "";
	/*
	if(m_type == "cpu")
	{
		m_parent = "";
		sc = "SELECT DISTINCT q.parent AS code, p.name, p.price, p.manual_cost_p, p.supplier_price FROM q_mb q LEFT OUTER JOIN product p ON q.parent=p.code ORDER BY name";
	}
	else if(m_type == "mb")
	{
		m_parent = "cpu";
		sc = "SELECT DISTINCT q.code AS code, p.name, p.price , p.manual_cost_p FROM q_mb q LEFT OUTER JOIN product p ON q.code=p.code ORDER BY name";
	}
	else if(m_type == "ram" || m_type == "video")
	{
		m_parent = "mb";
		sc = "SELECT DISTINCT q.code AS code, p.name, p.price , p.manual_cost_p FROM q_" + m_type + " q LEFT OUTER JOIN product p ON q.code=p.code ORDER BY name";
	}
	else
		sc = "SELECT DISTINCT q." + m_type + " AS code, p.name, p.price , p.manual_cost_p FROM q_flat q LEFT OUTER JOIN product p ON q." + m_type + "=p.code WHERE q." + m_type + ">0 ORDER BY name";
		*//********************************* Modify 02/04/08 colin **********************************/
		if(m_type == "cpu")
	{
		m_parent = "";
		sc = "SELECT DISTINCT q.parent AS code, p.name, p.price,  p.supplier_price, c.rate, c.level_rate1, c.manual_cost_nzd FROM q_mb q LEFT OUTER JOIN product p ON q.parent=p.code JOIN code_relations c ON c.code = p.code ORDER BY p.name";
	}
	else if(m_type == "mb")
	{
		m_parent = "cpu";
		sc = "SELECT DISTINCT q.code AS code, p.name, p.price ,  c.rate, c.level_rate1, c.manual_cost_nzd FROM q_mb q LEFT OUTER JOIN product p ON q.code=p.code JOIN code_relations c ON c.code = p.code ORDER BY p.name";
	}
	else if(m_type == "ram" || m_type == "video")
	{
		m_parent = "mb";
		sc = "SELECT DISTINCT q.code AS code, p.name, p.price , c.rate, c.level_rate1, c.manual_cost_nzd FROM q_" + m_type + " q LEFT OUTER JOIN product p ON q.code=p.code JOIN code_relations c ON c.code = p.code ORDER BY p.name";
	}
	else
		sc = "SELECT DISTINCT q." + m_type + " AS code, p.name, p.price ,  c.rate, c.level_rate1 , c.manual_cost_nzd FROM q_flat q LEFT OUTER JOIN product p ON q." + m_type + "=p.code JOIN code_relations c ON c.code = p.code WHERE q." + m_type + ">0 ORDER BY p.name";
	
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, m_type);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}

bool PrintAddNewField()
{
	string keys = GetNewOptions(m_type);
//	if(keys == "error")
//		return false;
	StringBuilder sb = new StringBuilder();
	if(m_type == "cpu" || m_type == "mb" || m_type == "ram" || m_type == "video")
	{
		sb.Append("<form action=sqmi.aspx method=get>");
		sb.Append("<table width=100% align=center><tr><td>");
		sb.Append("<input type=hidden name=t value='" + m_type + "'>");
		sb.Append("<select name=set>");
		sb.Append(keys);
		sb.Append("</select> ");
		sb.Append("<input type=submit value='Add' " + Session["button_style"] + ">");
		sb.Append("</td></tr></table>");
	}
	else
	{
		sb.Append("<form action=sqm.aspx?t=" + m_type + " method=post>");
		sb.Append("<table width=100% align=center><tr><td>");
		sb.Append("<select name=code>");
		sb.Append(keys);
		sb.Append("</select> ");
		sb.Append("<input type=submit name=cmd value='Add' " + Session["button_style"] + ">");
		sb.Append("</td></tr></table>");
	}
	
	sb.Append("</form>");

	sb.Append("<form action=sqm.aspx?t=" + m_type + " method=post>");
	sb.Append("<b>Select String : </b><input type=text size=90 name=cat value=\"" + GetQCat(m_type) + "\">");
	sb.Append("<input type=submit name=cmd value='Apply' " + Session["button_style"] + ">");
	sb.Append("</form>");

	LAddNewButton.Text = sb.ToString();
	return true;
}
/*
bool PrintAddNewFieldWithParent()
{
	string keys = "";
//	keys = GetNewOptions(m_parent);
//	if(m_type == "cpu")
//		keys = GetKeyOptions(m_parent);

	string options = GetNewOptions(m_type);
	
	if(keys == "error")
		return false;
	if(options == "error")
		return false;
	
	StringBuilder sb = new StringBuilder();
	sb.Append("<form action=sqmi.aspx?t=" + m_type + " method=get>");
	sb.Append("<table><tr><td>");
//	sb.Append("<select name=set>");
//	sb.Append(keys);
//	sb.Append("</select></td></tr><tr><td>");
	sb.Append("<select name=set>");
	sb.Append(options);
	sb.Append("</select></td></tr><tr><td align=right>");
	sb.Append("<input type=submit value='Add'>");
	sb.Append("</td></tr></table></form>");
	LAddNewButton.Text = sb.ToString();
	return true;
}
*/
string GetNewOptions(string sKey)
{
	DataSet dso = new DataSet();
	int rows = 0;
	StringBuilder sb = new StringBuilder();
	
	string sc = GetNewOptionsKey(sKey);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dso);
	}
	catch(Exception e) 
	{
		string ee = e.ToString();
		int pos = ee.IndexOf("at System.");
		if(pos > 0)
			ee = ee.Substring(0, pos);
		Response.Write("<br><h5>SQL Query Error : </h5><font color=red>" + ee + "</font>");
//		ShowExp(sc, e);
		return "error";
	}
	if(rows <= 0)
		return "";

	bool bSelected = false;
	string code = "";
	string name = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dso.Tables[0].Rows[i];
		code = dr["code"].ToString();
//		if(m_parent == "")
		{
			if(QExists(code, null))
				continue;
		}
		sb.Append("<option value='" + code + "'");
		if(!bSelected)
		{
			if(m_set != null)
			{
				if(m_set == code)
				{
					sb.Append(" selected");
					bSelected = true;
				}
			}
			else
			{
				sb.Append(" selected");
				bSelected = true;
			}
		}
		name = dr["name"].ToString();
		if(name.Length > 100)
			name = name.Substring(0, 100);
	//	sb.Append(">" + name + " $" + dr["price"].ToString() + "</option>");
	   double b_rate = double.Parse(dr["rate"].ToString());
	   double level_r1 = double.Parse(dr["level_rate1"].ToString());
	   double level_pr1 = double.Parse(dr["manual_cost_nzd"].ToString());
	          b_rate = Math.Round(b_rate, 6);
			  level_r1 = Math.Round(level_r1, 6);
			  level_pr1 = Math.Round(level_pr1, 2);
		double sqm_lprice = b_rate * level_r1 * level_pr1;
		      sqm_lprice = Math.Round(sqm_lprice, 2);
			  
//DEBUG("RATE " , b_rate);
//DEBUG("level rate ", level_r1);
//DEBUG("level price ", level_pr1);   
	   
		sb.Append(">" + name + " $" + sqm_lprice + "</option>");
	}
	if(sKey == "video")
		sb.Append("<option value=-1>" + m_sNONE + "</option>");
	return sb.ToString();
}

bool DoAdd()
{
	string code = Request.Form["code"];
	string parent = Request.Form["parent"];
	m_set = parent;
	if(QExists(code, parent))
	{
//DEBUG("already exists, code="+code, " parent="+parent);
		return true;
	}
	if(!IsInteger(code))
	{
		Response.Write("<h3>Invalid Code</h3>");
		return false;
	}
	string sc = "";
	if(m_type == "mb" || m_type == "ram" || m_type == "video")
		sc = "INSERT INTO q_" + m_type + " (parent, code) VALUES(" + parent + ", " + code + ")";
	else
		sc = "INSERT INTO q_flat (" + m_type + ") VALUES(" + code + ")";
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

bool DoDelete()
{
	string code = Request.QueryString["c"];
//	string parent = Request.QueryString["p"];
	string sc = "";
	if(m_type == "cpu")
		sc = "DELETE FROM q_mb WHERE parent=" + code;
	else if(m_type == "mb")
		sc = "DELETE FROM q_mb WHERE code=" + code;
	else if(m_type == "ram" || m_type == "video")
		sc = "DELETE FROM q_" + m_type + " WHERE code=" + code;
	else
		sc = "DELETE FROM q_flat WHERE " + m_type + "=" + code;
//DEBUG("m_type="+m_type, " sc="+sc);
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
	
	if(m_type == "mb") //delete ram and video for this mb as well
	{
		sc = "DELETE FROM q_ram WHERE parent=" + code;
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
		
		sc = "DELETE FROM q_video WHERE parent=" + code;
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
	}
	return true;
}

/////////////////////////////////////////////////////////////////
void BindGrid()
{
	Response.Write("<table width=100% cellspacing=0 cellpadding=a bordercolor=#EEEEEE ");
	Response.Write("style=border-width:1px;font-family:Verdana;font-size:8pt;border-collapse:collapse;fixed>");
	
	//menu
	Response.Write("<tr><td colspan=" + m_cols + " align=right>");
	Response.Write("<a href=?t=cpu&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>CPU</a> ");
	Response.Write("<a href=?t=mb&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>MB</a> ");
	Response.Write("<a href=?t=ram&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>RAM</a> ");
	Response.Write("<a href=?t=video&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>VideoCard</a> ");
	Response.Write("<a href=?t=sound&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>SoundCard</a> ");
	Response.Write("<a href=?t=hd&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>HDD</a> ");
	Response.Write("<a href=?t=fd&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>FDD</a> ");
	Response.Write("<a href=?t=cd&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Opitcal Drives 1</a> ");
	Response.Write("<a href=?t=cdrw&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Opitcal Drives 2</a> ");
	Response.Write("<a href=?t=modem&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Modem</a> ");
	Response.Write("<a href=?t=monitor&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Monitor</a> ");
	Response.Write("<a href=?t=pccase&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Case</a> ");
	Response.Write("<a href=?t=kb&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Keyboard</a> ");
	Response.Write("<a href=?t=mouse&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Mouse</a> ");
	Response.Write("<a href=?t=speaker&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Speaker</a> ");
	Response.Write("<a href=?t=printer&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Printer</a> ");
	Response.Write("<a href=?t=scanner&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>Scanner</a> ");
	Response.Write("<a href=?t=nic&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>NIC</a> ");
	Response.Write("<a href=?t=os&r=" + DateTime.Now.ToOADate() + "><img src=i/reda1.gif border=0>OS</a> ");
	Response.Write("</td></tr>");

	//table
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	Response.Write("<td>DEL</td><td>EDIT</td>");
//	if(m_parent != "")
//		Response.Write("<td><b>" + m_parent + "</b></td>");
	Response.Write("<td><b>" + m_type + "</b></td><td align=right><b>Leve 1 Price</b></td></tr>");
	bool bAlterColor = false;
//	string set_old = "-1";
	for(int i=0; i<ds.Tables[0].Rows.Count; i++)
	{
		DataRow dr = ds.Tables[0].Rows[i];
		string name = dr["name"].ToString();
		string code = dr["code"].ToString();
//DEBUG("code="+code, " name="+name);
		if(name == null || name == "")
		{
			if(m_type != "video") //delete obsolete item
			{
				string sc = "";
				if(m_type == "cpu")
					sc = "DELETE FROM q_mb WHERE parent=" + code;// + " AND parent=" + m_parent;
				else if(m_type == "mb" || m_type == "ram")
					sc = "DELETE FROM q_" + m_type + " WHERE code=" + code;// + " AND parent=" + m_parent;
				else
					sc = "DELETE FROM q_flat WHERE " + m_type + "=" + code;
//DEBUG("do delete, sc=", sc);
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
					return;
				}
				continue;
			}
		}

//DEBUG("parent="+dr["parent"].ToString(), "code="+dr["code"].ToString());
		Response.Write("<tr");
		if(bAlterColor)
			Response.Write(" bgcolor=#EEEEEE");
		bAlterColor = !bAlterColor;
		Response.Write(">");
		Response.Write("<td><a href=sqm.aspx?a=del&t=" + m_type + "&c=" + dr["code"].ToString());
//		if(m_parent != "")
//			Response.Write("&p=" + dr["parent"].ToString());
		Response.Write("&r=" + DateTime.Now.ToOADate());
		Response.Write(">DEL</a></td>");

		Response.Write("<td><a href=liveedit.aspx?code=" + code + " target=_blank>Edit</a></td>");
/*		
		if(m_parent != "")
		{
			Response.Write("<td");
			string set = dr["parent"].ToString();
			string desc = GetProductDesc(set);
//DEBUG("code="+code, " desc="+desc);
			if(desc == "") //delete obsolete item
			{
				if(m_type == "mb" || m_type == "ram" || m_type == "video")
				{
					string sc = "DELETE FROM q_" + m_type + " WHERE code=" + code + " AND parent=" + set;
//DEBUG("do delete, sc=", sc);
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
						return;
					}
					desc = "NOT FOUND, record deleted";
				}
			}
			Response.Write(" title='" + desc + "'>");
			if(m_type == "mb")
			{	Response.Write("<a href=sqmi.aspx?t=");
				Response.Write("cpu");
			}
			else
				Response.Write(m_type);
			if(m_type == "mb")
				Response.Write("&set="+set+"&r="+DateTime.Now.ToOADate()+ " class=d target=_blank>");
			Response.Write(set + " ");
			if(desc.Length >= 30)
				Response.Write(desc.Substring(0, 30));
			else
				Response.Write(desc);
			if(m_type == "mb")
				Response.Write("</a>");
			Response.Write("</td>");
		}
*/      
        double price1 = double.Parse(dr["price"].ToString()); 
		double manual_price = double.Parse(dr["manual_cost_nzd"].ToString());
		double rate = double.Parse(dr["rate"].ToString());
		       rate = Math.Round(rate,6);
		double level_rate1 = double.Parse(dr["level_rate1"].ToString());
		       level_rate1 = Math.Round(level_rate1, 6);
		   
		double level_price1 = manual_price * rate * level_rate1 ;
		       level_price1 = Math.Round(level_price1, 2);
		
//DEBUG("rate ", rate);
//DEBUG ("level_rate ", level_rate1);
//DEBUG ("level_price1 ", level_price1);


		if(name == null || name == "")
		{
			Response.Write("<td>" + m_sNONE + "</td><td> </td>");
		}
		else
		{
			Response.Write("<td>");
			if(m_type == "cpu")
				Response.Write("<a href=sqmi.aspx?t=cpu&set="+code+"&r="+DateTime.Now.ToOADate()+ " class=d target=_blank>");
			if(m_type == "mb")
				Response.Write("<a href=sqmi.aspx?t=mb&set="+code+"&r="+DateTime.Now.ToOADate()+ " class=d target=_blank>");
			else if(m_type == "ram" || m_type == "video")
				Response.Write("<a href=sqmi.aspx?t=" + m_type + "&set="+code+"&r="+DateTime.Now.ToOADate()+ " class=d target=_blank>");
			Response.Write(code + " " + name + "</a></td>");
//			Response.Write("<td><a href=liveedit.aspx?code=" + code + " class=d target=_blank>" + code + " " + name + "</a></td>");
			//Response.Write("<td align=right>" + double.Parse(dr["price"].ToString()).ToString("c") + "</td>");
			Response.Write ("<td align=right>" + level_price1.ToString("c")+ "</td>");
			
		}
		Response.Write("</tr>");
	}
	Response.Write("</table>");
}

</script>

<asp:Label id=LAddNewButton runat=server/>
<asp:Label id=LFooter runat=server/>
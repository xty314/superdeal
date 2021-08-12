<script runat=server>

DataSet dst = new DataSet();

string m_action;
string m_show;
string m_q;
string m_c;
string m_s;
string m_ss;
string cat = "";
string s_cat = "";
string ss_cat = "";
string tableWidth = "97%";
bool m_edit = false;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...

	if(!SecurityCheck("manager"))
		return;

	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		cat = Request.QueryString["cat"];
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		s_cat = Request.QueryString["scat"];
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		ss_cat = Request.QueryString["sscat"];
    Trim(ref cat);
	Trim(ref s_cat);
	Trim(ref ss_cat);
    
    PrintAdminHeader();
	PrintAdminMenu();
	GetQueryStrings();


    if(Request.Form["cmd"] == "Save")
    {
        string cValue = Request.Form["cv"];
        string cType = "1";
        if(Request.Form["cType"] == "Percentage")
            cType = "2";
        if(!DoUpadteCommition(cType, cValue))
        {
            Response.Write("<br><br><center><h3>Commision updating failed!</h3></center>");
            return;
        }
    }
    if(m_action == "e") //edit
	{
		m_edit = true;
        DrawTableHeader();
        	
//        Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=ecom.aspx \">");
//    DEBUG("cat =", cat);
//    DEBUG("s_cat =", s_cat);
//    DEBUG("ss_cat =", ss_cat);
        
	}
    else
	{
		if(GetTable())
		{
			DrawTableHeader();
			
		}
        else
            Response.Write("<br><br><center><h3>There is no <b>Catalog</b> to edit</h3></center>");  
//    DEBUG("cat =", cat);
//    DEBUG("s_cat =", s_cat);
//    DEBUG("ss_cat =", ss_cat);        
			
	}

	PrintAdminFooter();
}

void GetQueryStrings()
{
	m_action = Request.QueryString["a"];
	m_show = Request.QueryString["sh"];
	m_q = Request.QueryString["q"];
	m_c = Request.QueryString["c"];
	m_s = Request.QueryString["s"];
	m_ss = Request.QueryString["ss"];
	
}
Boolean GetTable()
{
	if( dst.Tables["catalog"] != null)
        dst.Tables["catalog"].Clear();
    string sc = "SELECT * FROM catalog WHERE cat <> 'Brands'";
    if(cat != "" && cat != "all")
		sc += " AND cat = N'"+ cat +"' ";
	if(s_cat != "" && s_cat != "all")
		sc += " AND s_cat = N'"+ s_cat +"' ";
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND ss_cat = N'"+ ss_cat +"' ";
    sc += " ORDER BY cat, s_cat, ss_cat ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if( myAdapter.Fill(dst, "catalog") < 1)
            return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}



void DrawTableHeader()
{
	Response.Write("<br><br>");
    Response.Write("<form name=f action='?a=s&cat=" + HttpUtility.UrlEncode(cat) + "&scat=" + HttpUtility.UrlEncode(s_cat) + "&sscat=" + HttpUtility.UrlEncode(ss_cat) + "' method=post>");
    Response.Write("<table align=center cellspacing=0 cellpadding=0 width="+ tableWidth +" valign=center bgcolor=white border=0><tr>");
	Response.Write("<td width='17' height='30' id='top-header1'>&nbsp;</td>");
	Response.Write("<td height='30' class='pageName' id='top-header2' ><font size=+1>" + Lang("Commision Edit") + "</font>");
    Response.Write("<td width='76' id='top-header3'>&nbsp;</td>");
	Response.Write("  <td  height='30' id='top-header4'>&nbsp;</td>");
	Response.Write("<td width='40' id='top-header5'>&nbsp;</td>");
	Response.Write("</tr></table>");

    Response.Write("<table border=1 align=center width='"+ tableWidth +"'");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6><br></td></tr>");
    Response.Write("<tr><td colspan=6><b>" + Lang("Catalog Select") + " :</b>&nbsp;&nbsp;");
    if(!doCatSearch())
        return;
    Response.Write("</td></tr>");
    Response.Write("<tr bgcolor=#8BB7DD>");
    Response.Write("<th align=center>" + Lang("Catalog") + "</th>");
    Response.Write("<th align=center>" + Lang("S_Cat") + "</th>");
    Response.Write("<th align=center>" + Lang("Ss_Cat") + "</th>");
    Response.Write("<th align=center>" + Lang("Commision Type") + "</th>");
    Response.Write("<th align=center>" + Lang("Commision Rate") + "</th>");
    Response.Write("<th align=center>" + Lang("Edit") + "</th>");
    Response.Write("</tr>");
    if(m_edit)
    {
        if(!DoEdit())
            return; 
    }
    else
    {
        if(!showTable())
            return;
    }
    Response.Write("</table></form>");
}
bool showTable()
{
    if( !GetTable())
        return false;
    int nRows = dst.Tables["catalog"].Rows.Count;
    for(int i=0; i<nRows; i++)
    {
        DataRow dr = dst.Tables["catalog"].Rows[i];
        Response.Write("<tr>");
        Response.Write("<td>" + dr["cat"].ToString() + "</td>");
        Response.Write("<td>" + dr["s_cat"].ToString() + "</td>");
        Response.Write("<td>" + dr["ss_cat"].ToString() + "</td>");
        if( dr["c_type"].ToString() == "1")
        {
            Response.Write("<td align=center>Pieces</td>");
            Response.Write("<td align=center>$" + dr["c_rate"].ToString() + "</td>");
        }
        else
        {
             Response.Write("<td align=center>Percentage</td>");
             Response.Write("<td align=center>" + dr["c_rate"].ToString() +"%</td>");
        }
        Response.Write("<td align=center><a href=\"ecom.aspx?a=e");
        if( dr["cat"].ToString()!= "" && dr["cat"].ToString() != null)
            Response.Write("&cat=" + HttpUtility.UrlEncode(dr["cat"].ToString()));
        if( dr["s_cat"].ToString()!= "" && dr["s_cat"].ToString() != null)
            Response.Write("&scat=" + HttpUtility.UrlEncode(dr["s_cat"].ToString()));
        if( dr["ss_cat"].ToString()!= "" && dr["ss_cat"].ToString() != null)
            Response.Write("&sscat=" + HttpUtility.UrlEncode(dr["ss_cat"].ToString()));
        Response.Write("\">Edit</a></td>");
        
        Response.Write("</tr>");
        
    }
    return true;
    
}

bool doCatSearch()
{
    int rows = 0;
	string sc = "SELECT DISTINCT cat FROM catalog WHERE cat <> 'Brands' ";
	sc += " ORDER BY cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
//DEBUG("rows=", rows);
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;
	Response.Write("<select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
	Response.Write("?r="+ DateTime.Now.ToOADate() +"");
	Response.Write("&cat=' + this.options[this.selectedIndex].value)\"");
	Response.Write(">");
	Response.Write("<option value='all'>Show All</option>");
	if(Request.QueryString["cat"] != null)
		cat = Request.QueryString["cat"].ToString();
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		Trim(ref s);
		if(cat == s)
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
		else
			Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");

	}

	Response.Write("</select>");
	
	if(cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
	    sc = "SELECT DISTINCT s_cat FROM catalog ";
		sc += " WHERE cat <> 'Brands' AND cat = N'" + cat + "' ";
		sc += " ORDER BY s_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=' + this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>" + Lang("Show All") + "</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			Trim(ref s);
			if(s_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
			
		}

		Response.Write("</select>");
	}
	
	if(s_cat != "")
	{
		cat = Request.QueryString["cat"].ToString();
		sc = "SELECT DISTINCT ss_cat FROM catalog ";
		sc += " WHERE cat <> 'Brands' AND cat = N'" + cat + "' ";
		sc += " AND s_cat = N'" + s_cat + "' ";
		sc += " ORDER BY ss_cat";
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}
		
		if(rows <= 0)
			return true;
		Response.Write("<select name=s ");
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"]);
		Response.Write("?cat=" + HttpUtility.UrlEncode(cat));
		Response.Write("&scat=" + HttpUtility.UrlEncode(s_cat));
		Response.Write("&sscat=' + this.options[this.selectedIndex].value) \"");
		Response.Write(">");
		Response.Write("<option value='all'>" + Lang("Show All") + "</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			Trim(ref s);
			if(ss_cat == s)
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "' selected>" + s + "</option>");
			else
				Response.Write("<option value='" + HttpUtility.UrlEncode(s) + "'>" + s + "</option>");
		}
		Response.Write("</select>");
	}
	return true;
}

bool DoEdit()
{
    if( !GetTable())
       return false;
    int nRows = dst.Tables["catalog"].Rows.Count;
    for(int i=0; i<nRows; i++)
    {
        string sc = @"
                      function changeType()
                      {
                          var txt = document.getElementById('cType').value; 
                          if( txt == 'Percentage')
                          {
                               document.getElementById('sMoney').style.color='#FFFFFF';
                               document.getElementById('sPer').style.color='#000'; 
                           }
                          else
                           { 
                               document.getElementById('sPer').style.color='#FFFFFF';
                               document.getElementById('sMoney').style.color='#000';
                            }       
                      }
                    ";
        DataRow dr = dst.Tables["catalog"].Rows[i];
        Response.Write("<tr>");
        Response.Write("<td>" + dr["cat"].ToString() + "</td>");
        Response.Write("<td>" + dr["s_cat"].ToString() + "</td>");
        Response.Write("<td>" + dr["ss_cat"].ToString() + "</td>");
        if( dr["c_type"].ToString() == "1")
        {
            Response.Write("<td align=center>");
            Response.Write("<select id=cType name=cType onChange=\"changeType()\"><option value='Pieces' selected>Pieces</option>");
            Response.Write("<option value='Percentage'>Percentage</option></select></td>");
            Response.Write("<td align=center><font id=sMoney>$&nbsp;</font><input type=text name='cv' style=\"width:50px\" value='" + dr["c_rate"].ToString() + "'><font id=sPer color=white>&nbsp;%</font></td>");
        }
        else
        {
             Response.Write("<td align=center>");
             Response.Write("<select id=cType name=cType onChange=\"changeType()\"><option value='Pieces'>Pieces</option>");
             Response.Write("<option value='Percentage' selected>Percentage</option></select></td>");
             Response.Write("<td align=center><font id=sMoney color=white>$&nbsp;</font><input type=text name='cv' style=\"width:50px\" value='" + dr["c_rate"].ToString() + "'><font id=sPer>&nbsp;%</font></td>");
        }
       /* Response.Write("<td align=center><a href=\"ecom.aspx?a=s");
        if( dr["cat"].ToString()!= "" && dr["cat"].ToString() != null)
            Response.Write("&cat=" + HttpUtility.UrlEncode(dr["cat"].ToString()));
        if( dr["s_cat"].ToString()!= "" && dr["s_cat"].ToString() != null)
            Response.Write("&scat=" + HttpUtility.UrlEncode(dr["s_cat"].ToString()));
        if( dr["ss_cat"].ToString()!= "" && dr["ss_cat"].ToString() != null)
            Response.Write("&sscat=" + HttpUtility.UrlEncode(dr["ss_cat"].ToString()));
        Response.Write("&ct=" + Request.Form["cType"] + "&cv=" + Request.Form["cv"]);
        Response.Write("\">Save</a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a href=\"ecom.aspx\"><font color=red>Cancel</font></a></td>");*/
        
       Response.Write("<td align=center><input type=submit name=cmd value='Save'>");
       Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<a href=\"ecom.aspx\"><font color=red>Cancel</font></a></td>");
        
        
        Response.Write("</tr>");
        Response.Write("<script language=javascript>");
        Response.Write(sc);
        Response.Write("</script");
        Response.Write(">");
    }
    
    return true;
}

bool DoUpadteCommition(string cType, string cValue)
{
    string sc = " UPDATE catalog SET c_type='" + cType + "'";
    if( cValue != null && cValue != "")
        sc += ", c_rate=" + cValue.Trim();
    else
        sc += ", c_rate=0 ";
    sc += " WHERE cat <> 'Brands'";
    if(cat != "" && cat != "all")
		sc += " AND cat = N'"+ cat +"' ";
	if(s_cat != "" && s_cat != "all")
		sc += " AND s_cat = N'"+ s_cat +"' ";
	if(ss_cat != "" && ss_cat != "all")
		sc += " AND ss_cat = N'"+ ss_cat +"' "; 
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


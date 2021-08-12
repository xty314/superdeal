<!-- #include file="kit_fun.cs" -->
<script runat=server>

string m_old_pwd = "";

DataSet dst = new DataSet();

void SPage_Load()//Object Src, EventArgs E ) 
{
	if(Request.QueryString["t"] != null && Request.QueryString["t"] == "c")
	{
		Response.Write("<center>");
		if(!GetUserOldPwd())
		{
			Response.Write("<br><br><br><h3><b>&nbsp;&nbsp;Error! Wrong Old Password!</b></h3>");
		}
		else if(!CheckNewPwd())
		{
			Response.Write("<br><br><br><h3><b>&nbsp;&nbsp;Invalid New Password!</b></h3>");
		}
		else if(UpdatePwd())
		{
			Response.Write("<br><br><br><h3><b>&nbsp;&nbsp;New password saved!</b></h3>");
			Response.Write("<input type=button onclick=window.location=('default.aspx') value='Home'>");
		}
		else
		{
			Response.Write("<br><br><br><b>&nbsp;&nbsp;Updating Password Error, Please ");
			Response.Write("<a href=setpwd.aspx?r=" + DateTime.Now.ToOADate() + ">");
			Response.Write("<font color=blue>Try Again!</font></a></b>");
		}
	}
	else
	{
		MyPwdTalbe();
	}
}

bool UpdatePwd()
{
	string newpwd = Request.Form["new_pwd"];
	newpwd = FormsAuthentication.HashPasswordForStoringInConfigFile(newpwd, "md5");
	
	string sc = "UPDATE card SET password='" + newpwd + "' WHERE email='" + Session["email"].ToString() + "'";
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


bool CheckNewPwd()
{
	string s_pwd1 = Request.Form["new_pwd"];
	string s_pwd2 = Request.Form["confirm_pwd"];

	return s_pwd1 == s_pwd2;
}

bool GetUserOldPwd()
{
	string s_login = Session["email"].ToString();
	string s_oldpwd = "";
	if(Request.Form["old_pwd"] != null)
		s_oldpwd = Request.Form["old_pwd"];
	else
		return false;

	s_oldpwd = FormsAuthentication.HashPasswordForStoringInConfigFile(s_oldpwd, "md5");
	int rows = 0;

	string sc = "SELECT password FROM card WHERE email='" + s_login + "'";
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst, "pwd");

		if(rows == 1 && dst.Tables["pwd"].Rows[0]["password"].ToString() == s_oldpwd)
		{
			return true;
		}
		else
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	return true;
}

void MyPwdTalbe()
{
	Response.Write("<br><br><br><center><h2>Change Password</b></center><br>");
	Response.Write("<form action=setpwd.aspx?t=c method=post>");
	Response.Write("<table width=35% align=center cellspacing=0 cellpadding=1 border=1 bgcolor=#FFFFFF");
	Response.Write(" bordercolorlight=#888888 bordercolor=#FFFFFF style=\"font-family:Verdana;font-size:8pt;fixed\">\r\n");
	Response.Write("<tr><td align=right style='border:none'>Your Old Password:</td>");
	Response.Write("<td style='border:none'><input class=form-control type=password name=old_pwd value=''></td></tr>\r\n");
	Response.Write("<tr><td align=right style='border:none'>New Password:</td>");
	Response.Write("<td style='border:none'><input class=form-control type=password name=new_pwd value=''></td></tr>\r\n");
	Response.Write("<tr><td align=right style='border:none'>Type New Password again:</td>");
	Response.Write("<td style='border:none'><input class=form-control type=password name=confirm_pwd value=''></td></tr>\r\n");

	Response.Write("<td colspan=2 align=center style='border:none'><br><input class='btn btn-success' type=submit value=Change> ");
	Response.Write("<button class='btn btn-danger' onclick=window.location=('Default.aspx?r=" + DateTime.Now.ToOADate() +"')>");
	Response.Write("Cancel</button><br></td></tr>\r\n");

	Response.Write("</table>");

	return;
}

</script>

<asp:Label id=LFooter runat=server/>
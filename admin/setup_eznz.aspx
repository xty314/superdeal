<%@Language=C# Debug="true" %>
<%@Import Namespace="System.Web.Caching" %>
<%@Import Namespace="System.Data" %>
<%@Import Namespace="System.Data.SqlClient" %>
<%@Import Namespace="System.IO" %>
<%@Import Namespace="System.Globalization" %>
<%@Import Namespace="System.Web.Mail" %>
<%@Import Namespace="System.Threading" %>

<script runat=server>

bool m_bDebug = false;
//bool m_bDebug = true;

string m_bs = "style=\"font-size:8pt;font-weight:bold;background-color:#EEEEEE;color:#444444;border-left:1px solid #C0C0C0;border-right:1px solid #666696;border-top: 1px solid #C0C0C0;border-bottom:1px solid #666696\"";

string m_constr = "";

string m_ip = "localhost";
string m_sDataSource = ";data source=localhost;";
//string m_sSecurityString = "Integrated Security=SSPI;";
//string m_ip = "192.168.1.4";
//string m_sDataSource = ";data source=192.168.1.4;";
string m_sSecurityString = "User id=eznz;Password=9seqxtf7;Integrated Security=false;";

string m_sCompanyName = "demo";		//site identifer, used for cache, sql etc, highest priority

SqlConnection myConnection;// = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);
SqlDataAdapter myAdapter;
SqlCommand myCommand;

DataSet ds = new DataSet();

protected void Page_Load(Object Src, EventArgs E ) 
{
	if(!Loggedin())
	{
		PrintLoginForm();
		return;
	}

	if(Session["sql_server_name"] != null)
		m_sDataSource = ";data source=" + Session["sql_server_name"].ToString() + ";";
	if(Session["sql_db_name"] != null)
		m_sCompanyName = Session["sql_db_name"].ToString();
	if(Session["sql_security_string"] != null)
		m_sSecurityString = Session["sql_security_string"].ToString();

	myConnection = new SqlConnection("Initial Catalog=" + m_sCompanyName + m_sDataSource + m_sSecurityString);

	if(Request.QueryString["t"] == "logo")
	{
		Response.Write(m_sHeader);
		Response.Write("<br><center><h3>EZNZ BUSINESS.NET 3.0 SETUP</h3>");
		FormImg.Visible = true;
		return;
	}
	else if(Request.QueryString["t"] == "config")
	{
		PrintConfigForm();
		return;
	}
	else if(Request.QueryString["t"] == "done")
	{
		Response.Write(m_sHeader);
		Response.Write("<br><table align=center valign=center height=97% width=97% bgcolor=white><tr><td valign=top>");
		Response.Write("<br><center><h3>EZNZ BUSINESS.NET 3.0 SETUP</h3>");
//		string url = Request.ServerVariables["SERVER_NAME"] + "/" + m_sCompanyName + "/admin";
		string url = Request.ServerVariables["SERVER_NAME"] + "/admin";
		Response.Write("<br><a href=http://" + url + " class=o>Login to New Site(admin)</a>");
		Response.Write("</td></tr></table>");
		return;
	}

	if(Request.Form["step"] != null && Request.Form["step"] != "")
	{
		string step = Request.Form["step"];
		switch (step)
		{
		case "setup_db":
			if(m_bDebug)
			{
				Session["sql_db_name"] = Request.Form["name"];
				Session["sql_server_name"] = Request.Form["server"];
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=config\">");
				break;
			}
//			if(DoCopyCSFiles())
			if(ModifySqlString())
			{
				if(DoSetupDB())
				{
					if(DoCopyData())
					{
						Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=?t=config\">");
						return;
					}
				}
			}
			break;
		case "config":
			if(DoSaveConfig())
				Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=logo\">");
			break;
		default:
			break;
		}
	}

	//gether sql info
	if(Session["sql_server_name"] == null || Session["sql_db_name"] == null)
	{
		if(Request.QueryString["adv"] == "1")
			PrintDBInfoFormAdvanced();
		else
			PrintDBInfoForm();
		return;
	}
}

bool Loggedin()
{
	string pass = "";
	if(Session["pass"] != null)
		pass = Session["pass"].ToString();
	if(Request.Form["pass"] != null)
		pass = Request.Form["pass"];
	pass = FormsAuthentication.HashPasswordForStoringInConfigFile(pass, "md5");
	if(pass == "410B6E86CA31315A55EF83F4686634C0")
	{
		if(Request.Form["pass"] != null)
			Session["pass"] = Request.Form["pass"];
		return true;
	}
	return false;
}

void PrintLoginForm()
{
	PrintAdminHeader("login");

	Response.Write("<tr><td colspan=2><font size=+1>Step 1 : Login</font></td></tr>");
//	Response.Write("<tr><td><b>Name : </b><td><input type=text name=name></td></tr>");
	Response.Write("<tr><td nowrap><b>Password : </b><td><input type=password size=50 name=pass></td></tr>");
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=GO " + m_bs + "></td></tr>");

	Response.Write("<script");
	Response.Write(">document.f.pass.focus();</script");
	Response.Write(">");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	PrintAdminFooter();
}

string GetMasterDBPath()
{
	string constr = "Initial Catalog=master" + m_sDataSource + m_sSecurityString;
//DEBUG("constr=", constr);
	string sc = " SELECT filename FROM sysdatabases WHERE name='master' ";
	DataSet dsf = new DataSet();
	myConnection = new SqlConnection(constr);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsf) > 0)
		{
			string s = dsf.Tables[0].Rows[0]["filename"].ToString();
			int i = s.Length-1;
			for(; i>=0; i--)
			{
				if(s[i] == '\\')
					break;
			}
			s = s.Substring(0, i+1);
			return s;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return "";
	}
	return "";
}

bool DoOrderOnlyChanges()
{
	string sc = "";
	//modify admin default page
//	string sc = " UPDATE site_pages SET text='" + EncodeQuote(m_sOrderOnlyAdminDefault) + "' ";
//	sc += " WHERE name='admin_default' ";

	//delete full version menus
	sc = " DELETE FROM menu_admin_catalog WHERE name IN ";
	sc += " ('Sales', 'Stock', 'Account', 'Service', 'Report', 'Purchase') ";

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

	//change orderlist file
	string path = Server.MapPath("/") + Session["sql_db_name"].ToString();
	string ofile = path + "\\cs\\olist.cs";
	string srcfile = Server.MapPath("..") + "\\cs\\olist_orderonly.cs";
	File.Delete(ofile);
	File.Copy(srcfile, ofile);

	return true;
}

bool DoSaveConfig()
{
	string sc = " UPDATE settings SET value = '"; 
	sc += EncodeQuote(Request.Form["company_name"]) + "' ";
	sc += " WHERE name='company_name' ";
	
	sc += " UPDATE settings SET value = '";
	sc += EncodeQuote(Request.Form["sales_email"]) + "' ";
	sc += " WHERE name='sales_email' ";

	bool bRetail = false;
	bool bOrderOnly = true;
	if(Request.Form["version"] == "retail")
		bRetail = true;
	if(Request.Form["orderonly"] != "on")
		bOrderOnly = false;
	if(bRetail)
		sc += " UPDATE settings SET value=1 WHERE name='system_retail_version' ";
	if(!bOrderOnly)
		sc += " UPDATE settings SET value=0 WHERE name='system_orderonly_version' ";

	if(bOrderOnly)
		DoOrderOnlyChanges();

	if(Request.Form["admin_email"] != "")
	{
		sc += " IF NOT EXISTS(SELECT * FROM card WHERE email='" + EncodeQuote(Request.Form["admin_email"]) + "') ";
		sc += " INSERT INTO card (name, email, company, trading_name, password, type, access_level) ";
		sc += " VALUES('" + EncodeQuote(m_sCompanyName) + " admin' ";
		sc += ", '" + EncodeQuote(Request.Form["admin_email"]) + "' ";
		sc += ", '" + EncodeQuote(Request.Form["company_name"]) + "' ";
		sc += ", '" + EncodeQuote(Request.Form["company_name"]) + "' ";
		sc += ", '" + FormsAuthentication.HashPasswordForStoringInConfigFile(Request.Form["admin_pass"], "md5") + "' ";
		sc += ", 4, 10) ";
	}

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

void PrintConfigForm()
{
	string dbpath = GetMasterDBPath();
	PrintAdminHeader("config");

	Response.Write("<tr><td colspan=2><font size=+1>Step 3 : Information</td></tr>");

	Response.Write("<tr><td nowrap><b>Company Title(Full Name)</b></td>");
	Response.Write("<td><input type=text name=company_name ></td></tr>");

	Response.Write("<tr><td nowrap><b>Sales Email</b></td>");
	Response.Write("<td><input type=text name=sales_email ></td></tr>");

	Response.Write("<tr><td nowrap><b>System Version : </b></td>");
	Response.Write("<td>");
	Response.Write("<input type=radio name=version value=wholesale checked> Wholesale");
	Response.Write("<input type=radio name=version value=retail> Retail");
	Response.Write("</td></tr>");

	Response.Write("<tr><td nowrap><b>Order Only</b></td>");
	Response.Write("<td><input type=checkbox name=orderonly checked> Yes</td></tr>");

	Response.Write("<tr><td nowrap><b>Admin Login Email</b></td>");
	Response.Write("<td><input type=text name=admin_email></td></tr>");
	
	Response.Write("<tr><td nowrap><b>Password</b></td>");
	Response.Write("<td><input type=password name=admin_pass></td></tr>");
	
//	Response.Write("<tr><td nowrap><b></b></td>");
//	Response.Write("<td><input type=text name= ></td></tr>");
	
	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=GO " + m_bs + "></td></tr>");

	Response.Write("</table>");
	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	Response.Write("<script");
	Response.Write(">document.f.company_name.focus();</script");
	Response.Write(">");
	PrintAdminFooter();
}

void PrintDBInfoForm()
{
	string dbpath = GetMasterDBPath();
	PrintAdminHeader("setup_db");

	Response.Write("<tr><td colspan=2><font size=+1>Step 2 : Create SQL Database</td></tr>");
	Response.Write("<input type=hidden size=30 name=server value=" + m_ip + ">");
	Response.Write("<tr><td nowrap><b>Company Short Name/ID <br>(Database name, no space): </b></td>");
	Response.Write("<td><input type=text size=10 name=name onchange='document.f.pathname.value=document.f.path.value+document.f.name.value+\".mdf\";'></td></tr>");
	Response.Write("<input type=hidden name=path onchange='document.f.pathname.value=document.f.path.value+document.f.name.value+\".mdf\";' ");
	Response.Write(" value='" + dbpath + "'>");
	Response.Write("<input type=hidden name=pathname value='" + dbpath + "'>");

	Response.Write("<input type=hidden name=dbuser value=eznz>");
	Response.Write("<input type=hidden name=dbpass value=9seqxtf7>");

	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=GO " + m_bs + "></td></tr>");

	Response.Write("<tr><td colspan=2 align=center><a href=?adv=1 class=o>Advanced Options</a></td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	Response.Write("<script");
	Response.Write(">document.f.name.focus();</script");
	Response.Write(">");
	PrintAdminFooter();
}

void PrintDBInfoFormAdvanced()
{
	string dbpath = GetMasterDBPath();
	PrintAdminHeader("setup_db");

	Response.Write("<tr><td colspan=2><font size=+1>Step 2 : Create SQL Database</td></tr>");
	Response.Write("<tr><td nowrap><b>Server Name/IP : </b></td>");
	Response.Write("<td><input type=text size=30 name=server value=" + m_ip + "></td></tr>");
	Response.Write("<tr><td nowrap><b>Database Name : </b></td>");
	Response.Write("<td><input type=text size=50 name=name onchange='document.f.pathname.value=document.f.path.value+document.f.name.value+\".mdf\";'></td></tr>");
	Response.Write("<tr><td nowrap><b>DB Path : </b></td>");
	Response.Write("<td><input type=text size=50 name=path onchange='document.f.pathname.value=document.f.path.value+document.f.name.value+\".mdf\";' ");
	Response.Write(" value='" + dbpath + "'></td></tr>");

	Response.Write("<tr><td nowrap><b>DB Path Name : </b></td>");
	Response.Write("<td><input type=text size=70 style='border=0;bgcolor=white' name=pathname value='" + dbpath + "'>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><b>user : </b></td><td><input type=text name=dbuser></td></tr>");
	Response.Write("<tr><td><b>pass : </b></td><td><input type=password name=dbpass></td></tr>");

	Response.Write("<tr><td colspan=2 align=right><input type=submit name=cmd value=GO " + m_bs + "></td></tr>");

	Response.Write("<tr><td colspan=2 align=center><a href=? class=o>Use Default Settings</a></td></tr>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
	Response.Write("</form>");
	Response.Write("<script");
	Response.Write(">document.f.name.focus();</script");
	Response.Write(">");
	PrintAdminFooter();
}

bool DBExists(string name)
{
	string constr = "Initial Catalog=master" + m_sDataSource + m_sSecurityString;
	string sc = " SELECT name FROM sysdatabases WHERE name='" + name + "' ";
	DataSet dsf = new DataSet();
	myConnection = new SqlConnection(constr);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dsf) > 0)
		{
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return false;
}

void CopyDirectory(string Src,string Dst) 
{ 
	if(Dst[Dst.Length-1]!=Path.DirectorySeparatorChar) 
		Dst += Path.DirectorySeparatorChar; 
	
	if(!Directory.Exists(Dst)) 
		Directory.CreateDirectory(Dst); 
	String[] Files = Directory.GetFileSystemEntries(Src); 
	foreach(string Element in Files) 
	{
		// Sub directories 
		if(Directory.Exists(Element)) 
			CopyDirectory(Element,Dst+Path.GetFileName(Element)); 
		else // Files in directory 
		{
			try
			{
				File.Copy(Element,Dst+Path.GetFileName(Element),true); 
			}
			catch(Exception e)
			{
			}
		}
	} 
}
 
bool DoCopyCSFiles()
{
	string root = Server.MapPath(".");
	string src = root;
	int i = src.Length - 2;
	for(;i>=0; i--)
	{
		if(src[i] == '\\')
			break;
	}
	src = src.Substring(0, i);

	string name = Request.Form["name"];
	string dest = Server.MapPath("/") + name;
//	Session["dest_root_path"] = dest;

	CopyDirectory(src, dest);
/*
	//create company root directory
	if(!Directory.Exists(dest))
		Directory.CreateDirectory(dest);

	string[] dirs = Directory.GetDirectories(src);
	string path = "";
	string dir = "";
	foreach(string sd in dirs)
	{
		i = sd.Length - 1;
		for(; i>=0; i--)
		{
			if(sd[i] == '\\')
				break;
		}
		path = sd.Substring(i, sd.Length - i);

		//create company sub directory
		if(!Directory.Exists(dest+path))
			Directory.CreateDirectory(dest+path);

		DirectoryInfo di = new DirectoryInfo(sd);
		DoCopyDirectory(di, dest+path);
	}
*/
	if(!ModifySqlString())
		return false;
	return true;
}

bool ModifySqlString()
{
	//modify sqlstring.cs
	string server = Request.Form["server"];
	string name = Request.Form["name"];
	string dest = Server.MapPath("../cs/sqlstring.cs");
	string ssfile = dest;// + "\\sqlstring.cs";

	FileStream fs = new FileStream(ssfile, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string text = "";
	for(int i=0; i<300; i++)
	{
		if(line == null)
			break;
		text += "\r\n";
		text += line;
		line = r.ReadLine();
	}
	fs.Close();

	File.Delete(ssfile);
	text = text.Replace(m_ip, server);
	text = text.Replace("darcy@eznz.com", "alert@eznz.com");
	text = text.Replace("wanfang", name);

	Encoding enc = Encoding.GetEncoding("iso-8859-1");
	byte[] Buffer = enc.GetBytes(text);

	FileStream newFile = null;
	try
	{
		newFile = new FileStream(ssfile, FileMode.Create);
		newFile.Write(Buffer, 0, Buffer.Length);
	}
	catch(Exception e)
	{
		Response.Write("<h3>Modify sqlstring.cs failed.</h3>" + e);
		return false;
	}
	newFile.Close();
	return true;
}

bool DoSetupDB()
{
	PrintAdminHeader("setup_db");

	Response.Write("</td></tr></table>");

	if(Request.Form["server"] == null || Request.Form["server"] == "")
		return false;

	string server = Request.Form["server"];
	string name = Request.Form["name"];
	string path = Request.Form["path"];
	string dbuser = Request.Form["dbuser"];
	string dbpass = Request.Form["dbpass"];

	if(name.IndexOf(' ') >=0
		|| name.IndexOf('/') >= 0
		|| name.IndexOf('\\') >= 0
		|| name.IndexOf('.') >= 0
		|| name.IndexOf('\'') >= 0
		|| name.IndexOf('"') >= 0
		|| name.IndexOf(';') >= 0
		|| name.IndexOf(':') >= 0
		|| name.IndexOf('~') >= 0
		|| name.IndexOf('|') >= 0
		|| name.IndexOf('?') >= 0
		|| name.IndexOf(',') >= 0
		|| name.IndexOf('(') >= 0
		|| name.IndexOf(')') >= 0
		|| name.IndexOf('[') >= 0
		|| name.IndexOf(']') >= 0
		|| name.IndexOf('>') >= 0
		|| name.IndexOf('<') >= 0
		)
	{
		Response.Write("<center><h3><font color=red>Error, Name/ID invalid, please use letters and numbers only");
		Response.End();
		return false;
	}

	if(DBExists(name))
	{
//		Response.Write("<center><h3><font color=red>Error, " + name + " already Exists, please use another Name/ID</h3>");
//		Response.End();
		return true;
	}

	Session["sql_server_name"] = server;
	Session["sql_db_name"] = name;

	if(Session["sql_server_name"] != null)
		m_sDataSource = ";data source=" + Session["sql_server_name"].ToString() + ";";
	if(Session["sql_db_name"] != null)
		m_sCompanyName = Session["sql_db_name"].ToString();

	if(dbuser != "")
	{
		m_sSecurityString = "User id=" + dbuser + ";Password=" + dbpass + ";Integrated Security=false;";
		Session["sql_security_string"] = m_sSecurityString;
	}
	m_constr = "Initial Catalog=master" + m_sDataSource + m_sSecurityString;
	
	if(path[path.Length - 1] != '\\')
		path += "\\";

	string pathname = path + name;

	Response.Write("<br>Creating database, please wait ... ");
	Response.Flush();

	string fileName = Server.MapPath(".") + "\\dbstruct.sql";
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string sc = "";
	bool bCreated = false;
	bool bCreatingProc = false;
	while(line != null)
	{
		if(line.ToLower().IndexOf("use [") == 0)
		{
			bCreated = true;
			m_constr = "Initial Catalog=" + name + m_sDataSource + m_sSecurityString;
			sc = "";
			line = r.ReadLine();
			continue;
		}
		sc += "\r\n";
		sc += line;

		line = r.ReadLine();
		if(line == null)
			break;
		if(!bCreated)
		{
			if(line.ToLower() == "go")
			{
Response.Write(".");
Response.Flush();
				sc = sc.Replace("e:\\sql\\MSSQL\\Data\\wanfang", pathname);
				sc = sc.Replace("wanfang", name);
				
				if(!ExecuteSQL(sc))
				{
					r.Close();
					Session["sql_server_name"] = null;
					return false;
				}
				sc = "";
				line = r.ReadLine();
			}
		}
		else if(bCreatingProc)
		{
			if(line.ToLower() == "go")
			{
Response.Write(".");
Response.Flush();
				if(!ExecuteSQL(sc))
				{
					r.Close();
					Session["sql_server_name"] = null;
					return false;
				}
				sc = "";
				line = r.ReadLine();
			}
		}
		else
		{
			if(line.ToUpper().IndexOf("CREATE PROCEDURE ") == 0)
			{
Response.Write(".");
Response.Flush();
				//create table before stored procudures, as CREATE PROCEDURE must be the first in a batch
				sc = sc.Replace("e:\\sql\\MSSQL\\Data\\wanfang", pathname);
				sc = sc.Replace("wanfang", name);
				sc = sc.Replace("GO", "");
				if(!ExecuteSQL(sc))
				{
					r.Close();
					Session["sql_server_name"] = null;
					return false;
				}
				bCreatingProc = true;
				sc = "";
			}
		}
	}
	r.Close();
	Response.Write(" done !");
	Response.Flush();

	return true;
}

bool DoCopyData()
{
	PrintAdminHeader("copy_data");
	Response.Write("<tr><td><br><center><h4>Table Created, Initializing data .....");
	Response.Flush();

	string fileName = Server.MapPath(".") + "\\dbdata.sql";
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string sc = "";
/*	while(line != null)
	{
		sc = line;
		sc = sc.Replace("Quay to NZ", m_sCompanyName);
		sc = sc.Replace("Wan Fang", m_sCompanyName);
		sc = sc.Replace("WanFang", m_sCompanyName);
		sc = sc.Replace("wanfang", m_sCompanyName);
		sc = sc.Replace("a.m.", "");
		sc = sc.Replace("p.m.", "");

		if(!ExecuteSQL(sc))
		{
			r.Close();
			return false;
		}
		line = r.ReadLine();
	}
	Response.Write(" Done!");
*/
	while(line != null)
	{
		sc += "\r\n";
		sc += line;
		line = r.ReadLine();
	}

	sc = sc.Replace("Quay to NZ", m_sCompanyName);
	sc = sc.Replace("Quay NZ to", m_sCompanyName);
	sc = sc.Replace("Souvenirs", "");
	sc = sc.Replace("Wan Fang", m_sCompanyName);
	sc = sc.Replace("WanFang", m_sCompanyName);
	sc = sc.Replace("wanfang", m_sCompanyName);
	sc = sc.Replace("a.m.", "");
	sc = sc.Replace("p.m.", "");

	if(ExecuteSQL(sc))
		Response.Write(" Done!");
	else
	{
		r.Close();
		return false;
	}

	r.Close();
	Response.Write("</h4></td></tr>");
	PrintAdminFooter();
	return true;
}

bool ExecuteSQL(string sc)
{
	if(sc == null || sc == "")
		return false;
//DEBUG("m_constr=", m_constr);
	myConnection = new SqlConnection(m_constr);
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
		string s = e.ToString();
		if(s.IndexOf("already exists") >= 0 || s.IndexOf("already an object named") >= 0)
			return true;
		Response.Write("<h3>Error</h3><b>sc=</b>" + sc + "<br><br><font color=red>" + e + "</font><br>" + Environment.StackTrace + "<br><br>");
		Response.Write("<h4>constr=" + m_constr);
		return false;
	}
	return true;
}

void PrintAdminHeader(string step)
{
	Response.Write(m_sHeader);

	Response.Write("<form name=f action=setup_eznz.aspx method=post>");
	Response.Write("<input type=hidden name=step value=" + step + ">");
	Response.Write("<br><table align=center valign=center height=97% width=97% bgcolor=white><tr><td valign=top>");

	Response.Write("<br><center><h3>EZNZ BUSINESS.NET 3.0 SETUP</h3>");

	Response.Write("<table cellspacing=9 cellpadding=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

}

void PrintAdminFooter()
{
//	Response.Write("</td></tr></table>");
//	Response.Write("</form>");

	Response.Write("</body></html>");
}
void DEBUG(string msg, string value)
{
	string sd = "";
	sd += "<font color=red>";
	sd += msg;
	sd += "</font>";
	sd += value;
	sd += "<br>\r\n";
	Response.Write(sd);
	Response.Flush();
}

void DEBUG(string msg, int value)
{
	string sd = "";
	sd += "<font color=red>";
	sd += msg;
	sd += "</font>";
	sd += value.ToString();
	sd += "<br>\r\n";
	Response.Write(sd);
	Response.Flush();
}

void ShowExp(string query, Exception e)
{
	Response.Write("Execute SQL Query Error.<br>\r\nQuery = ");
	Response.Write(query);
	Response.Write("<br>\r\n Error: ");
	Response.Write(e);
	Response.Write("<br>\r\n");
/*	string msg = "\r\n<font color=red><b>EXP</b></font><br>\r\n";
	msg += e.ToString();
	msg += "<br><br><font color=red><b>QUERY</b></font><br>\r\n";
	msg += query;
	msg += "<br><br>\r\n\r\n";
	msg += "ip : " + Session["ip"] + "<br>\r\n";
	msg += "login : " + Session["name"] + "<br>\r\n";
	msg += "email : " + Session["email"] + "<br>\r\n";
	msg += "url : " + Request.ServerVariables["URL"] + "<br>\r\n";
	AlertAdmin(msg);
*/
	Response.End(); //no more out put
}

string EncodeQuote(string s) //double single quote for sql statements
{
	if(s == null)
		return null;
	string ss = "";
	for(int i=0; i<s.Length; i++)
	{
		if(s[i] == '\'')
			ss += '\''; //double it for SQL query
		ss += s[i];
	}
	return ss;
}

void cmdSend_Click(object sender, System.EventArgs e)
{
	// Check to see if file was uploaded
	if( filMyFile.PostedFile != null )
	{
		// Get a reference to PostedFile object
		HttpPostedFile myFile = filMyFile.PostedFile;

		string ext = Path.GetExtension(myFile.FileName);
		ext = ext.ToLower();
		if(ext != ".jpg" && ext != ".gif")
		{
			Response.Write("<h3>ERROR Only .jpg, .gif File Allowed</h3>");
			return;
		}

		int nFileLen = myFile.ContentLength; 
		if(nFileLen > 204800)
		{
			Response.Write("<h3>ERROR Max File Size(200 KB) Exceeded. ");
			Response.Write(Path.GetFileName(myFile.FileName) + " " + (int)nFileLen/1000 + " KB </h3>");
			return;
		}

		if( nFileLen > 0 )
		{
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);

			string strFileName = Path.GetFileName(myFile.FileName);
			string strPath = Server.MapPath("/" + m_sCompanyName + "/i");
			strPath += "\\";
			strPath += "new-logo.gif";//strFileName;
//DEBUG("pathname=", strPath);
			// Write data into a file, overwrite if exists
			FileStream newFile = new FileStream(strPath, FileMode.Create);
			newFile.Write(myData, 0, myData.Length);
			newFile.Close();
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=setup_eznz.aspx?t=done\">");
		}
	}
	return;
}

string m_sHeader = @"
	<html><head>
	<title>EZNZ Setup</title>

	<style type=text/css>

	td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:vardana;}
	body{font:10px Verdana;}
	a{color:#0033FF;text-decoration:none;color=#000000} 
	a:hover{text-decoration:underline;color=red}
	.m{TEXT-DECORATION:none;background-color:#EEEEEE;border: 1px solid #000000;}
	.x{FONT-WEIGHT:300;FONT-SIZE:8PT;TEXT-DECORATION:underline;FONT-FAMILY:verdana;COLOR:#0000ff;}
	.w{color:#FFFFFF;text-decoration:none} a.w:hover{color:#FF0000;text-decoration:none}
	.d{color:#000000;text-decoration:none} a.d:hover{color:#FF0000;text-decoration:none}
	.o{color:#0000FF;text-decoration:underline} a.o:hover{color:#FF0000;text-decoration:none}

	</style>
	</head>
	<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0>
";


</script>
<form id="FormImg" method="post" runat="server" enctype="multipart/form-data" visible=false>
<br>
<table align=center cellspacing=1 cellpadding=3 border=0 bgcolor=#FFFFFF bordercolorlight=#44444 bordercolordark=#AAAAAA style=font-family:Verdana;font-size:8pt;fixed>
<tr><td><font size=+1><b>Step 4 : Upload Company Logo</b></font><br>

<table><tr>
<td><input id="filMyFile" type="file" size=50 runat="server"></td><td>&nbsp;</td></tr>
<tr><td>
<asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/>
<input type=button value=Finish onclick=window.location=('setup_eznz.aspx?t=done')>
</tr>
</table>

<br>
<asp:Label id=LOldPic runat=server/>
</td></tr></table>


</FORM>
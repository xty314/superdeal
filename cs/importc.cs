<script runat=server>

const string date_format = "MMM-dd HH:mm";
string m_file = "";

//for configuration
int aNameCount = 64;
string[] aName = new string[64];
string[] aValue = new string[64];
string[] aColumn = new string[64];

bool m_bTestFormat = false;
//int debug_del = 0; //for debug only

DataSet ds = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dsc = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations

int m_nMapOptions = 0;
string[] m_aMapOption = new String[64];

int m_nSkipLine = 0;
StringBuilder sbTest = new StringBuilder();
string m_last_scat = "";
string m_last_sscat = "";

int	m_nExistsItem = 0;
int m_nNewItem = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	if(!GetColumns())
		return;

	if(Request.QueryString["file"] != null)
		m_file = Request.QueryString["file"];

	if(Request.Form["cmd"] != null)
	{
		if(!DoSaveConfiguration())
		{
			Response.Write("<br><br><center><h3>Error Save Configuration</h3>");
			return;
		}

		if(Request.Form["cmd"] == "Test")
		{
			PrintAdminHeader();
			PrintAdminMenu();
			m_bTestFormat = true;
			m_bShowProgress = true;
			CheckSCVFormat();
			DoTest();
			PrintAnalyzePage();
		}
		else
		{
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=importc.aspx?t=analyze&file=" + HttpUtility.UrlEncode(m_file) + "\">");
		}
		return;
	}

	string type = Request.QueryString["t"];
	if(type == "process")
	{
		CheckSCVFormat();
		if(DoProcessFile())
		{
		}
		return;
	}
	else if(type == "delete")
	{
		string root = GetRootPath() + "data/card";
		root = Server.MapPath(root);
		string pathname = root + "\\" +  m_file;
		if(File.Exists(pathname))
			File.Delete(pathname);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=importc.aspx\">");
		return;
	}
	else if(type == "analyze")
	{
		PrintAdminHeader();
		PrintAdminMenu();
		CheckSCVFormat();
		PrintAnalyzePage();
		return;
	}
	else
	{
		PrintAdminHeader();
		PrintAdminMenu();
		PrintAdminFooter();
		string root = GetRootPath() + "data/card";
		root = Server.MapPath(root);
		if(!Directory.Exists(root))
			Directory.CreateDirectory(root);

		Response.Write("<br><center><h3>Import Card</h3>");
		Response.Write("<table align=center cellspacing=1 cellpadding=7 border=1 bordercolor#EEEEEE");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
		Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
		Response.Write("<td><b>FILE</b></td>");
		Response.Write("<td><b>SIZE</b></td>");
		Response.Write("<td><b>FILE DATE</b></td>");
		Response.Write("<td><b>ACTION</b></td>");
		Response.Write("</tr>");

		string[] dirs = Directory.GetDirectories(root);
		string path = "";
		string dir = "";
		DirectoryInfo di = new DirectoryInfo(root);
		foreach (FileInfo f in di.GetFiles("*.csv")) 
		{
			string s = f.FullName;
			string file = f.Name;//s.Substring(path.Length+1, s.Length-path.Length-1);
			string file_id = file + "_" + (f.Length/1000).ToString() + "K_" + f.LastWriteTime.ToString(date_format);
			Response.Write("<tr><td><a href=importc.aspx?supplier=" + dir + "&file=");
			Response.Write(HttpUtility.UrlEncode(file));
			Response.Write(">");
			Response.Write(file);
			Response.Write("</a></td>");
			Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
			Response.Write("<td>" + f.LastWriteTime.ToString(date_format) + "</td>");

			Response.Write("<td>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('importc.aspx?t=process&file=" + HttpUtility.UrlEncode(file) + "') value=Process>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('importc.aspx?t=delete&file=" + HttpUtility.UrlEncode(file) + "') value=Delete>");
			string sp = dir.ToUpper();
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('importc.aspx?t=analyze&file=" + HttpUtility.UrlEncode(file) + "') value='Analyze'>");
			Response.Write("</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");
		Form1.Visible = true;
		return;
	}
}

bool GetColumns()
{
	string sc = " SELECT TOP 1 * FROM import_card_format ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "columns");
	}
	catch(Exception e) 
	{
		PrintAdminHeader();
		PrintAdminMenu();
		ShowExp(sc, e);
		return false;
	}

	DataColumnCollection dc = ds.Tables["columns"].Columns;
	aNameCount = dc.Count - 2; //ignore id, file_name
	int m = 0;
	for(int i=0; i<dc.Count; i++)
	{
		string name = dc[i].ColumnName;
		if(name == "id" || name == "file_name")
			continue;
		aName[m++] = name;
	}

	for(int i=0; i<aNameCount; i++)
		aValue[i] = "0";

	return true;
}

bool CheckSCVFormat()
{
	string sc = "SELECT * FROM import_card_format WHERE file_name = '" + m_file + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "csv") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(dst.Tables["csv"].Rows.Count > 0)
	{
		DataRow dr = dst.Tables["csv"].Rows[0];
		for(int i=0; i<aNameCount; i++)
		{
			string s = dr[aName[i]].ToString();
			if(s == "")
				s = "0"; //this column (aColumn[0]) is set to blank
			string nstr = dr[aName[i]].ToString();
			Trim(ref nstr);
			aValue[i] = nstr;
			if(aName[i] == "lines_to_skip")
				m_nSkipLine = MyIntParse(nstr);
		}
	}
	return true;	
}

string CSVNextColumn(char[] cb, ref int pos)
{
	if(pos >= cb.Length)
		return "";

	char[] cbr = new char[cb.Length];
	int i = 0;

	if(cb[pos] == '\"')
	{
		while(true)
		{
			pos++;
			if(pos == cb.Length)
				break;
			if(cb[pos] == '\"')
			{
				pos++;
				if(pos >= cb.Length)
					break;
				if(cb[pos] == '\"')
				{
					cbr[i++] = '\"';
					continue;
				}
				else if(cb[pos] != ',')
				{
					Response.Write("<br><font color=red>Error</font>. CSV file corrupt, comma not followed quote. Line=");
					Response.Write(new string(cb));
					Response.Write("<br>\r\n");
					break;
				}
				else
				{
					pos++;
					break;
				}
			}
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
		}
	}
	else
	{
		while(cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
			if(cb[pos] == '\'')
				cbr[i++] = '\'';
			pos++;
			if(pos == cb.Length)
				break;
		}
		pos++;
	}
	return new string(cbr, 0, i);
}

bool GetSampleLine(string sLine)
{
	m_aMapOption[0] = "<option value=''></option>";
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 1;
	for(i=1; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		m_aMapOption[i] = CSVNextColumn(cb, ref pos);
	}
	m_nMapOptions = i;
	return true;
}

string BuildMapOptions(int current)
{
	StringBuilder sb = new StringBuilder();
	sb.Append(m_aMapOption[0]);

	for(int i=1; i<m_nMapOptions; i++)
	{
		sb.Append("<option value=" + i.ToString());
		if(current == i)
			sb.Append(" selected");
		sb.Append(">" + m_aMapOption[i] + "</option>");
	}
	return sb.ToString();
}

bool PrintAnalyzePage()
{
	//read a few lines of the file
	string root = GetRootPath() + "data/card/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
//	string text = "";
	bool bGetSample = true;
	for(int i=0; i<30; i++)
	{
		if(line == null)
			break;
		if(bGetSample)
		{
			bGetSample = false;
			GetSampleLine(line);
			break;
		}
//		text += "\r\n";
//		text += line;
		line = r.ReadLine();
	}

	Response.Write("<br><center><font size=3><b>File Format Configuration</b></font>");
	Response.Write("<table width=90%>");
	Response.Write("<tr><td><b>" + m_file + "</b></td></tr>");

//	Response.Write("<tr><td><textarea name=text wrap=off cols=110 rows=10>");
//	Response.Write(text);
//	Response.Write("</textarea></td></tr>");

	//print data
	Response.Write("<tr><td>");

	Response.Write("<table valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");

	//column number
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	for(int i=1; i<=m_nMapOptions; i++)
	{
		Response.Write("<th>" + i.ToString() + "</th>");
	}
	Response.Write("</tr>");

	//column name
	Response.Write("<tr style=\"background-color:#EEEEEE;font-weight:bold;\">\r\n");
	for(int i=1; i<m_nMapOptions; i++)
	{
		Response.Write("<th>" + m_aMapOption[i] + "</th>");
	}
	Response.Write("</tr>");

	//data
//	r.BaseStream.Seek(0, SeekOrigin.Begin);
	line = r.ReadLine();
	for(int i=0; i<5; i++)
	{
		if(line == null)
			break;
		Response.Write("<tr>");
		char[] cb = line.ToCharArray();
		int pos = 0;
		for(int m=1; m<64; m++)
		{
			if(pos >= cb.Length)
				break;
			Response.Write("<td>" + CSVNextColumn(cb, ref pos) + "</td>");
		}
		Response.Write("</tr>");
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();

	Response.Write("</table>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td><br><b>Column Mapping</b>");

	//configuration table
	Response.Write("<table valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=50>Name</td>\r\n");
	Response.Write("<td width=50>MapTo</td>\r\n");
	Response.Write("</tr>\r\n");

	Response.Write("<form action=importc.aspx?file=" + HttpUtility.UrlEncode(m_file) + " method=post>");

	for(int i=0; i<aNameCount; i++)
	{
		Response.Write("<tr><td><b>" + aName[i].ToUpper() + "</b></td><td>");
		if(aName[i] == "card_type")
		{
			Response.Write("<select name=card_type>");
			Response.Write("<option value=1>Customer</option>");
			Response.Write("<option value=2 ");
			if(aValue[i] == "2")
				Response.Write(" selected");
			Response.Write(">Dealer</option>");
			Response.Write("<option value=3 ");
			if(aValue[i] == "3")
				Response.Write(" selected");
			Response.Write(">Supplier</option>");
			Response.Write("<option value=4 ");
			if(aValue[i] == "4")
				Response.Write(" selected");
			Response.Write(">Employee</option>");
			Response.Write("<option value=5 ");
			if(aValue[i] == "5")
				Response.Write(" selected");
			Response.Write(">Others</option>");
			Response.Write("</select>");
		}
		else if(aName[i] == "lines_to_skip")
		{
			Response.Write("<input type=text size=10 name=" + aName[i] + " value='");
			Response.Write(aValue[i]);
			Response.Write("'>");
		}
		else
		{
			Response.Write("<select name=" + aName[i] + ">");
			Response.Write(BuildMapOptions( MyIntParse(aValue[i]) ));
			Response.Write("</select>");
		}
		Response.Write("</td></tr>");
	}

	Response.Write("<tr><td colspan=2 align=right>");
	Response.Write("<input type=submit name=cmd value=Test " + Session["button_style"] + ">");
	Response.Write("<input type=submit name=cmd value=Save " + Session["button_style"] + ">");
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('importc.aspx?r=" + DateTime.Now.ToOADate() + "') value=OK>");
	Response.Write("</td></tr>");
	Response.Write("</form>");
	Response.Write("</table>");

	Response.Write("</td></tr></table>");
	Response.Write("</center>");
	return true;
}

/////////////////////////////////////////////////////////////////
void cmdSend_Click(object sender, System.EventArgs e)
{
	if(filMyFile.PostedFile != null)
	{
		HttpPostedFile myFile = filMyFile.PostedFile;
		int nFileLen = myFile.ContentLength; 
		if( nFileLen > 0 )
		{
			byte[] myData = new byte[nFileLen];
			myFile.InputStream.Read(myData, 0, nFileLen);
			string strFileName = Path.GetFileName(myFile.FileName);
			string sExt = Path.GetExtension(myFile.FileName);
			if(sExt.ToLower() != ".csv")
			{
				Response.Write("<h3>Error, " + strFileName + " is not a .csv file</h3>");
				return;
			}
			string m_fileName = strFileName;
			string vpath = GetRootPath() + "data/card/";
			string strPath = Server.MapPath(vpath);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += strFileName;
			
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=importc.aspx\">");
		}
	}
}

void WriteToFile(string strPath, ref byte[] Buffer)
{
	FileStream newFile = new FileStream(strPath, FileMode.Create);
	newFile.Write(Buffer, 0, Buffer.Length);
	newFile.Close();
}

bool DoSaveConfiguration()
{
	string sc = "IF NOT EXISTS (SELECT id FROM import_card_format WHERE file_name = '" + m_file + "') ";
	sc += " INSERT INTO import_card_format (file_name";
	for(int i=0; i<aNameCount; i++)
	{
		sc += "," + aName[i];
	}
	sc += ") VALUES('" + m_file + "'";
	for(int i=0; i<aNameCount-1; i++)
	{
		sc += ",'" + Request.Form[aName[i]] + "' ";
	}
	sc += ", '" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += ") ELSE UPDATE import_card_format SET ";
	for(int i=0; i<aNameCount-1; i++)
	{
		sc += aName[i] + "='" + Request.Form[aName[i]] + "', ";
	}
	sc += aName[aNameCount-1] + "='" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += " WHERE file_name = '" + m_file + "' ";
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

bool DoTest()
{
	//read a few lines of the file
	string root = GetRootPath() + "data/card/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string text = "";

	int i = 1;
	for(; i<10; i++)
	{
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		if(line == null)
			break;
		text += "\r\n";
		text += line;
		if(!ProcessLine(line))
			break;
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();

	Response.Write("<table width=100% cellspacing=0 cellpadding=0 bordercolor=#EEEEEE bgcolor=white border=1");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">");
	for(i=0; i<aNameCount; i++)
	{
		if(aName[i] == "lines_to_skip")
			continue;
		if(aValue[i] == "" || aValue[i] == "0")
			continue;
		Response.Write("<th>" + aName[i] + "</th>");
	}
	Response.Write("</tr>");
	Response.Write(sbTest.ToString());
	Response.Write("</table>");
	return true;
}

bool DoAddCard()
{
	string email = "";
	string name = "";
	string lastname = "";
	string short_name = "";
	string trading_name = "";
	string company = "";
	string address1 = "";
	string address2 = "";
	string address3 = "";
	string city = "";
	string country = "";
	string phone = "";
	string fax = "";
	string contact = "";
	string nameB = "";
	string companyB = "";
	string address1B = "";
	string address2B = "";
	string cityB = "";
	string countryB = "";
	string postal1 = "";
	string postal2 = "";
	string postal3 = "";
	string card_type = "1";
	string dealer_level = "1";
	string credit_term = "0";
	string credit_limit= "1";
	string sales = "";
	string open_balance = "0";
	
	if(Session["import_card_email"] != null)
	{
		email = Session["import_card_email"].ToString();
		Trim(ref email);
		Session["import_card_email"] = null;
	}
	if(Session["import_card_name"] != null)
	{
		name = Session["import_card_name"].ToString();
		Trim(ref name);
		Session["import_card_name"] = null;
	}
	if(Session["import_card_last_name"] != null)
	{
		name = Session["import_card_last_name"].ToString();
		Trim(ref lastname);
		Session["import_card_last_name"] = null;
	}
	if(Session["import_card_short_name"] != null)
	{
		short_name = Session["import_card_short_name"].ToString();
		Trim(ref short_name);
		Session["import_card_short_name"] = null;
	}
	if(Session["import_card_trading_name"] != null)
	{
		trading_name = Session["import_card_trading_name"].ToString();
		Trim(ref trading_name);
		Session["import_card_trading_name"] = null;
	}
	if(Session["import_card_company"] != null)
	{
		company = Session["import_card_company"].ToString();
		Trim(ref company);
		Session["import_card_company"] = null;
	}
	if(Session["import_card_address1"] != null)
	{
		address1 = Session["import_card_address1"].ToString();
		Trim(ref address1);
		Session["import_card_address1"] = null;
	}
	if(Session["import_card_address2"] != null)
	{
		address2 = Session["import_card_address2"].ToString();
		Trim(ref address2);
		Session["import_card_address2"] = null;
	}
	if(Session["import_card_address3"] != null)
	{
		address3 = Session["import_card_address3"].ToString();
		Trim(ref address3);
		Session["import_card_address3"] = null;
	}
	if(Session["import_card_city"] != null)
	{
		city = Session["import_card_city"].ToString();
		Trim(ref city);
		Session["import_card_city"] = null;
	}
	if(Session["import_card_country"] != null)
	{
		country = Session["import_card_country"].ToString();
		Trim(ref country);
		Session["import_card_country"] = null;
	}
	if(Session["import_card_phone"] != null)
	{
		phone = Session["import_card_phone"].ToString();
		Trim(ref phone);
		Session["import_card_phone"] = null;
	}
	if(Session["import_card_fax"] != null)
	{
		fax = Session["import_card_fax"].ToString();
		Trim(ref fax);
		Session["import_card_fax"] = null;
	}
	if(Session["import_card_contact"] != null)
	{
		contact = Session["import_card_contact"].ToString();
		Trim(ref contact);
		Session["import_card_contact"] = null;
	}
	if(Session["import_card_nameB"] != null)
	{
		nameB = Session["import_card_nameB"].ToString();
		Trim(ref nameB);
		Session["import_card_nameB"] = null;
	}
	if(Session["import_card_companyB"] != null)
	{
		companyB = Session["import_card_companyB"].ToString();
		Trim(ref companyB);
		Session["import_card_companyB"] = null;
	}
	if(Session["import_card_address1B"] != null)
	{
		address1B = Session["import_card_address1B"].ToString();
		Trim(ref address1B);
		Session["import_card_address1B"] = null;
	}
	if(Session["import_card_address2B"] != null)
	{
		address2B = Session["import_card_address2B"].ToString();
		Trim(ref address2B);
		Session["import_card_address2B"] = null;
	}
	if(Session["import_card_cityB"] != null)
	{
		cityB = Session["import_card_cityB"].ToString();
		Trim(ref cityB);
		Session["import_card_cityB"] = null;
	}
	if(Session["import_card_countryB"] != null)
	{
		countryB = Session["import_card_countryB"].ToString();
		Trim(ref countryB);
		Session["import_card_countryB"] = null;
	}
	if(Session["import_card_postal1"] != null)
	{
		postal1 = Session["import_card_postal1"].ToString();
		Trim(ref postal1);
		Session["import_card_postal1"] = null;
	}
	if(Session["import_card_postal2"] != null)
	{
		postal2 = Session["import_card_postal2"].ToString();
		Trim(ref postal2);
		Session["import_card_postal2"] = null;
	}
	if(Session["import_card_postal3"] != null)
	{
		postal3 = Session["import_card_postal3"].ToString();
		Trim(ref postal3);
		Session["import_card_postal3"] = null;
	}
	if(Session["import_card_card_type"] != null)
	{
		card_type = Session["import_card_card_type"].ToString();
		Trim(ref card_type);
		Session["import_card_card_type"] = null;
	}
	if(Session["import_card_dealer_level"] != null)
	{
		dealer_level = Session["import_card_dealer_level"].ToString();
		Trim(ref dealer_level);
		Session["import_card_dealer_level"] = null;
	}
	if(Session["import_card_credit_term"] != null)
	{
		credit_term = Session["import_card_credit_term"].ToString();
		Trim(ref credit_term);
		Session["import_card_credit_term"] = null;
	}
	if(Session["import_card_credit_limit"] != null)
	{
		credit_limit = Session["import_card_credit_limit"].ToString();
		Trim(ref card_type);
		Session["import_card_credit_limit"] = null;
	}
	if(Session["import_card_sales"] != null)
	{
		sales = Session["import_card_sales"].ToString();
		Trim(ref sales);
		Session["import_card_sales"] = null;
	}
	if(Session["import_card_open_balance"] != null)
	{
		open_balance = Session["import_card_open_balance"].ToString();
		Trim(ref open_balance);
		Session["import_card_open_balance"] = null;
	}

	if(email == "")
		email = phone;//DateTime.Now.ToOADate().ToString();
	if(CardExists(email, name, company, trading_name, contact, address1, address2, card_type))
	{
		m_nExistsItem++;
		return true;
	}
	if(lastname != "")
		name = name + " " + lastname;
	string sc = "BEGIN TRANSACTION IF NOT EXISTS (SELECT email FROM card WHERE email = '"+ EncodeQuote(email) +"') ";
	sc += " INSERT INTO card (email, name, short_name, trading_name, company, address1, address2, address3, city ";
	sc += ", country, phone, fax, contact, nameB, companyB, address1B, address2B, cityB, countryB ";
	sc += ", postal1, postal2, postal3, type, dealer_level, credit_term";
	if(credit_limit != "")
	{
		if(TSIsDigit(credit_limit))
			sc += ", credit_limit";
	}
	if(sales != "")
	{
		if(TSIsDigit(sales))
			sc += ", sales";
	}
	sc += ", password) VALUES( ";
	sc += " '" + EncodeQuote(email) + "' ";
	sc += ", N'" + EncodeQuote(name) + "'";
	sc += ", N'" + EncodeQuote(short_name) + "' ";
	sc += ", N'" + EncodeQuote(trading_name) + "' ";
	sc += ", N'" + EncodeQuote(company) + "' ";
	sc += ", N'" + EncodeQuote(address1) + "' ";
	sc += ", N'" + EncodeQuote(address2) + "' ";
	sc += ", N'" + EncodeQuote(address3) + "' ";
	sc += ", N'" + EncodeQuote(city) + "' ";
	sc += ", N'" + EncodeQuote(country) + "' ";
	sc += ", N'" + EncodeQuote(phone) + "' ";
	sc += ", N'" + EncodeQuote(fax) + "' ";
	sc += ", N'" + EncodeQuote(contact) + "' ";
	sc += ", N'" + EncodeQuote(nameB) + "' ";
	sc += ", N'" + EncodeQuote(companyB) + "' ";
	sc += ", N'" + EncodeQuote(address1B) + "' ";
	sc += ", N'" + EncodeQuote(address2B) + "' ";
	sc += ", N'" + EncodeQuote(cityB) + "' ";
	sc += ", N'" + EncodeQuote(countryB) + "' ";
	sc += ", N'" + EncodeQuote(postal1) + "' ";
	sc += ", N'" + EncodeQuote(postal2) + "' ";
	sc += ", N'" + EncodeQuote(postal3) + "' ";
	sc += ", " + card_type;
	if(TSIsDigit(dealer_level))
		sc += ", " + dealer_level;
	else
		sc += ", 1 ";
	if(TSIsDigit(credit_term))
			sc += ", " + credit_term;
	else
	{
		credit_term = credit_term.ToLower();
		string scterm = "0";
		if(credit_term == "cash only")
			scterm = "1";
		else if(credit_term == "pay in advance" || credit_term == "pay in")
			scterm = "2";
		else if(credit_term == "c.o.d" || credit_term == "cash on delivery")
			scterm = "3";
		else if(credit_term == "7days" || credit_term == "7 days" || credit_term == "7day")
			scterm = "4";
		else if(credit_term == "14days" || credit_term == "14 days" || credit_term == "14day")
			scterm = "5";
		else if(credit_term == "30days" || credit_term == "30 days" || credit_term == "30day")
			scterm = "6";
		else if(credit_term == "20th of the month" || credit_term == "20th" || credit_term == "20th month")
			scterm = "7";
		sc += ", "+ scterm;
	}
	if(credit_limit != "")
	{
		if(TSIsDigit(credit_limit))
			sc += ", " + credit_limit +"";
	}
	if(sales != "")
	{
		if(TSIsDigit(sales))
			sc += ", '"+ sales +"' ";
		else
			sc += ", '' ";
	}
	sc += ", '"+ FormsAuthentication.HashPasswordForStoringInConfigFile(name, "md5") +"' ";
	sc += ") ";
	sc += " COMMIT ";
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
	m_nNewItem++;
	return true;
}

bool CardExists(string email, string name, string company, string trading_name, string contact, string address1, string address2, string card_type)
{
	string sc = " SELECT id FROM card WHERE email = '" + EncodeQuote(email) + "' ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "exists") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Response.End();
		return false;
	}

	//check others
	sc = " SELECT id FROM card WHERE ";
	sc += " name = '" + EncodeQuote(name) + "' ";
	sc += " AND company = '" + EncodeQuote(company) + "' ";
	sc += " AND trading_name = '" + EncodeQuote(trading_name) + "' ";
	sc += " AND contact = '" + EncodeQuote(contact) + "' ";
	sc += " AND address1 = '" + EncodeQuote(address1) + "' ";
	sc += " AND address2 = '" + EncodeQuote(address2) + "' ";
	sc += " AND type = " + card_type;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "exists") > 0)
			return true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Response.End();
		return false;
	}
	return false;
}

bool ProcessLine(string sLine)
{
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 1;
	for(i=1; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		aColumn[i] = CSVNextColumn(cb, ref pos);
	}
	if(m_bTestFormat)
		sbTest.Append("<tr>");
	for(i=0; i<aNameCount; i++)
	{
		if(aName[i] == "lines_to_skip")
			continue;
		if(aValue[i] == "" || aValue[i] == "0")
			continue;
		string v = "";
		int n = MyIntParse(aValue[i]);
		if(n >= 0)
			v = aColumn[n];
		if(aName[i] == "card_type")
			v = aValue[i];
		if(m_bTestFormat)
			sbTest.Append("<td>" + v + "</td>");
		else
		{
			Session["import_card_" + aName[i]] = v;
		}
	}
	if(m_bTestFormat)
		sbTest.Append("</tr>");
	else
	{
		if(!DoAddCard())
			return false;
	}
	return true;
}

bool DoProcessFile()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = GetRootPath() + "data/card/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	Response.Write("done.<br>");
	Response.Write("Processing, please wait...");
	
	string line = r.ReadLine();
	string text = "";

	bool bRet = true;
	int i = 0;
	while(line != null)
	{
		i++;
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		text += "\r\n";
		text += line;
		if(!ProcessLine(line))
		{
			bRet = false;
			break;
		}
		MonitorProcess(50);
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();
	Response.Write("done.<br>");
	Response.Write("<h5>Total lines processed : <b>" + (i - m_nSkipLine).ToString() + "</b><br>");
	Response.Write("Exists/Duplicates Cards : <b>" + m_nExistsItem + "</b><br>");
	Response.Write("New Cards(Imported) : <b>" + m_nNewItem + "</b></h5>");
	Response.Write(" <a href=default.aspx class=o>Done</a></h4>");
	return bRet;
}
</script>

<form id="Form1" method="post" runat="server" enctype="multipart/form-data" visible=false>
<br>
<table align=center>
<tr><td colspan=4 align=center><font size=+1><b>Upload File</b><br>&nbsp;</td></tr>
<tr><td><b> File : </b><input id="filMyFile" type="file" runat="server"></td><td>&nbsp;</td>
<td> <asp:button id="cmdSend" runat="server" OnClick="cmdSend_Click" Text="Upload"/></td>
</tr></table>
</form>

<br><br><br><br><br>
<h5><a href=import.aspx class=o>Import Product</a></h5>

<script runat=server>

const string date_format = "MMM-dd HH:mm";
string m_file = "";

//for configuration
int aNameCount = 64;
string[] aName = new string[64];
string[] aValue = new string[64];
string[] aColumn = new string[64];
bool m_bUPDATEREGULARLY = true;
bool m_bTestFormat = false;
//int debug_del = 0; //for debug only

DataSet ds = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dsc = new DataSet();	//DataSet cache for code_relations and product_drop
DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations

int m_nMapOptions = 0;
string[] m_aMapOption = new String[64];

int m_nSkipLine = 0;
string m_ProductUpdate = "";
StringBuilder sbTest = new StringBuilder();
string m_subCatSeperator = "";
string m_last_scat = "";
string m_last_sscat = "";

int m_nUpdatedItem = 0;
int	m_nNoCodeItem = 0;
int	m_nExistsItem = 0;
int m_nExistsBarcode = 0;
int	m_nNoPriceItem = 0;
int m_nStockErrorItem = 0;
int m_nNewItem = 0;

string m_pn_last = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("administrator"))
		return;

	if(!GetColumns())
		return;

	if(Request.QueryString["file"] != null)
		m_file = Request.QueryString["file"];
	m_bUPDATEREGULARLY = MyBooleanParse(GetSiteSettings("item_price_update_regularly", "1", false));
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
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=import.aspx?t=analyze&file=" + HttpUtility.UrlEncode(m_file) + "\">");
		}
		return;
	}

	string action = Request.QueryString["a"];
	string type = Request.QueryString["t"];
	
	if(action == "pb")
	{
		DoProcessBarcodeSupplier();
		return;
	}
	else if(action == "pp")
	{
		DoProcessProduct();
		return;
	}
	else if(action == "moq")
	{
		DoProcessMOQ();
		return;
	}
	else if(action == "book1")
	{
		DoProcessBook1();
		return;
	}
	
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
		string root = "data/item";
		root = Server.MapPath(root);
		string pathname = root + "\\" +  m_file;
		if(File.Exists(pathname))
			File.Delete(pathname);
		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=import.aspx\">");
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
		string root = "data/item";
		root = Server.MapPath(root);
		if(!Directory.Exists(root))
			Directory.CreateDirectory(root);

		Response.Write("<br><center><h3>Import Product</h3>");
		Response.Write("<table align=center cellspacing=1 cellpadding=7 border=1 bordercolor#EEEEEE");
		Response.Write(" style=\"font-family:Verdana;font-size:8pt;fixed\">");
		Response.Write("<tr style=\"color:#000000;background-color:#CCCCCC;font-weight:bold;\">\r\n");
		Response.Write("<td><b>FILE</b></td>");
		Response.Write("<td><b>SUPPLIER</b></td>");
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
			Response.Write("<tr><td><a href=import.aspx?supplier=" + dir + "&file=");
			Response.Write(HttpUtility.UrlEncode(file));
			Response.Write(">");
			Response.Write(file);
			Response.Write("</a></td><td>"+ dir + "</td>");
			Response.Write("<td>" + (f.Length/1000).ToString() + "K</td>");
			Response.Write("<td>" + f.LastWriteTime.ToString(date_format) + "</td>");

			Response.Write("<td>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('import.aspx?t=process&file=" + HttpUtility.UrlEncode(file) + "') value=Process>");
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('import.aspx?t=delete&file=" + HttpUtility.UrlEncode(file) + "') value=Delete>");
			string sp = dir.ToUpper();
			Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('import.aspx?t=analyze&file=" + HttpUtility.UrlEncode(file) + "') value='Analyze'>");
			Response.Write("</td>");
			Response.Write("</tr>");
		}
		Response.Write("</table>");
		Response.Write("<a href=?a=pb>Process BarcodeSupplier</a> ");
		Response.Write("<a href=?a=pp>Process Product</a> ");
		Response.Write("<a href=?a=pc>Process Customer</a>");
		Form1.Visible = true;
		return;
	}
}

bool GetColumns()
{
	string sc = " SELECT TOP 1 * FROM import_item_format ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(ds, "columns");
	}
	catch(Exception e) 
	{
		PrintAdminHeader();
		PrintAdminMenu();
		
		if(e.ToString().IndexOf("Invalid column name 'branch'")>=0)
		{
			sc = " ALTER TABLE import_item_format ADD branch [varchar](25), product_update [varchar](25) ";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e1) 
			{
				ShowExp(sc, e1);
				return false;
			}
		}

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

void PrintMainForm()
{
	PrintAdminHeader();
	PrintAdminMenu();
	PrintAdminFooter();
	Response.Write("<br><br><center><h4>Import Data</h4>");
	Response.Write("<h5><a href=import.aspx?t=item class=o>Import Product</a><h5>");
	Response.Write("<h5><a href=import.aspx?t=customer class=o>Import Customer</a><h5>");
	Response.Write("<h5><a href=import.aspx?t=supplier class=o>Import Supplier</a><h5>");
}

bool CheckSCVFormat()
{
	string sc = "SELECT branch, product_update, * FROM import_item_format WHERE file_name = '" + m_file + "'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "csv") <= 0)
			return false;
	}
	catch(Exception e) 
	{
		if(e.ToString().IndexOf("Invalid column name 'branch'")>=0)
		{
			sc = " ALTER TABLE import_item_format ADD branch [varchar](25), product_update [varchar](25) ";
			try
			{
				myCommand = new SqlCommand(sc);
				myCommand.Connection = myConnection;
				myCommand.Connection.Open();
				myCommand.ExecuteNonQuery();
				myCommand.Connection.Close();
			}
			catch(Exception e1) 
			{
				ShowExp(sc, e1);
				return false;
			}
		}
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
			else if(aName[i] == "product_update")
				m_ProductUpdate = nstr;
			else if(aName[i] == "sub_cat_seperator")
				m_subCatSeperator = nstr;
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
//			if(cb[pos] == '\'')
//				cbr[i++] = '\'';
		}
	}
	else
	{
		while(cb[pos] != ',')
		{
			cbr[i++] = cb[pos];
//			if(cb[pos] == '\'')
//				cbr[i++] = '\'';
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
	string root = "data/item/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	
	string line = r.ReadLine();
	string text = "";
	bool bGetSample = true;
	for(int i=0; i<30; i++)
	{
		if(line == null)
			break;
		if(bGetSample)
		{
			bGetSample = false;
			GetSampleLine(line);
		}
		text += "\r\n";
		text += line;
		line = r.ReadLine();
	}
	r.Close();
	fs.Close();

	Response.Write("<br><center><font size=3><b>File Format Configuration</b></font>");
	Response.Write("<table width=90%>");
	Response.Write("<tr><td><b>" + m_file + "</b></td></tr>");

	Response.Write("<tr><td><textarea name=text wrap=off cols=110 rows=10>");
	Response.Write(text);
	Response.Write("</textarea></td></tr>");

	Response.Write("<tr><td><br><b>Column Mapping</b>");

	//configuration table
	Response.Write("<table valign=center cellspacing=0 cellpadding=0 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	Response.Write("<td width=50>Name</td>\r\n");
	Response.Write("<td width=50>MapTo</td>\r\n");
	Response.Write("</tr>\r\n");

	Response.Write("<form action=import.aspx?file=" + HttpUtility.UrlEncode(m_file) + " method=post>");

	for(int i=0; i<aNameCount; i++)
	{
		string sName = aName[i].ToUpper();
		if(sName == "PRICE1")
			sName = "POS Price inc GST";
		else if(sName == "PRICE2")
			sName = "POS Disc Price inc GST";
		else if(sName == "PRICE3")
			sName = "Whole Sale Price ext GST";
		Response.Write("<tr><td><b>" + sName + "");
		if(aName[i] == "product_update")
			Response.Write("?");
		Response.Write("</b></td><td>");
		if(aName[i] == "sub_cat_seperator")
		{
			Response.Write("<input type=text size=10 name=" + aName[i] + " value='");
			if(aValue[i] != "0")
				Response.Write(aValue[i]);
			Response.Write("'>ie:'/','-' if main cat field has these characters for sub categories");
		}
		else if(aName[i] == "lines_to_skip")
		{
			Response.Write("<input type=text size=10 name=" + aName[i] + " value='");
			Response.Write(aValue[i]);
			Response.Write("'>");
		}
		else if(aName[i] == "product_update")
		{
			Response.Write("<input type=checkbox name=" + aName[i] + " ");	
			if(aValue[i] == "on")
				Response.Write(" checked ");
			Response.Write("> <font color=red><i><b>(Check this box will update stock to Specify BRANCH ONLY, if no branch select, it will go to First Branch)</i>");
		}
		else if(aName[i] == "branch")
		{
			PrintBranchNameOptions(MyIntParse(aValue[i]).ToString());
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
	Response.Write("<input type=button " + Session["button_style"] + " onclick=window.location=('import.aspx?r=" + DateTime.Now.ToOADate() + "') value=OK>");
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
			string vpath = "data/item/";
			string strPath = Server.MapPath(vpath);
			if(!Directory.Exists(strPath))
				Directory.CreateDirectory(strPath);
			strPath += strFileName;
			
			WriteToFile(strPath, ref myData);
			Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=import.aspx\">");
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
	string sc = "IF NOT EXISTS (SELECT id FROM import_item_format WHERE file_name = '" + m_file + "') ";
	sc += " INSERT INTO import_item_format (file_name";
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
	sc += ") ELSE UPDATE import_item_format SET ";
	for(int i=0; i<aNameCount-1; i++)
	{
		sc += aName[i] + "='" + Request.Form[aName[i]] + "', ";
	}
	sc += aName[aNameCount-1] + "='" + EncodeQuote(Request.Form[aName[aNameCount-1]]) + "' ";
	sc += " WHERE file_name = '" + m_file + "' ";

	if(Request.Form["sub_cat_seperator"] != "")
		sc += " UPDATE import_item_format SET s_cat = '-1', ss_cat = '-1' WHERE file_name = '" + m_file + "' ";
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
	string root = "data/item/";
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
		if(aName[i] == "sub_cat_seperator" || aName[i] == "lines_to_skip" || aName[i] == "product_update")
			continue;
		if(aValue[i] == "" || aValue[i] == "0")
		{
//			if(aName[i] != "s_cat" && aName[i] != "ss_cat")
				continue;
		}
		Response.Write("<th>" + aName[i] + "</th>");
	}
	Response.Write("</tr>");
	Response.Write(sbTest.ToString());
	Response.Write("</table>");
	return true;
}

bool DoAddItem()
{
	string supplier = "";
	string supplier_code = "";
	string id = "";
	string ref_code = "";
	string name = "";
	string brand = "";
	string cat = "";
	string s_cat = "";
	string ss_cat = "";
	string barcode = "";
	string average_cost = "";
	string supplier_price = "";
	string manual_cost_frd = "";
	string manual_cost_nzd = "";
	string stock = "";
	string eta = "";	
	string location = "";
	string expire_date = "";
	string price1 = "";
	string price2 = "";
	string price3 = "";
	string price4 = "";
	string price5 = "";
	string price6 = "";
	string branch = "";
	string inner_pack = "";
	string outer_pack = "";
	string moq = "";
	
	string spec = "";
	string update_regular = "1";

	if(Session["import_item_supplier"] != null)
	{
		supplier = Session["import_item_supplier"].ToString();
		Session["import_item_supplier"] = null;
	}
	if(Session["import_item_m_pn"] != null)
	{
		supplier_code = Session["import_item_m_pn"].ToString();
		Session["import_item_m_pn"] = null;
	}
	if(Session["import_item_ref_code"] != null)
	{
		ref_code = Session["import_item_ref_code"].ToString();
		Session["import_item_ref_code"] = null;
	}
	if(Session["import_item_description"] != null)
	{
		name = Session["import_item_description"].ToString();
		Session["import_item_description"] = null;
	}
	if(Session["import_item_brand"] != null)
	{
		brand = Session["import_item_brand"].ToString();
		Session["import_item_brand"] = null;
	}
	if(Session["import_item_cat"] != null)
	{
		cat = Session["import_item_cat"].ToString();
		Session["import_item_cat"] = null;
	}
	if(Session["import_item_s_cat"] != null)
	{
		s_cat = Session["import_item_s_cat"].ToString();
		Session["import_item_s_cat"] = null;
	}
	if(Session["import_item_ss_cat"] != null)
	{
		ss_cat = Session["import_item_ss_cat"].ToString();
		Session["import_item_ss_cat"] = null;
	}
	if(Session["import_item_barcode"] != null)
	{
		barcode = Session["import_item_barcode"].ToString();
		Session["import_item_barcode"] = null;
	}
	if(Session["import_item_average_cost"] != null)
	{
		average_cost = Session["import_item_average_cost"].ToString();
		Session["import_item_average_cost"] = null;
	}
	if(Session["import_item_last_cost"] != null)
	{
		supplier_price = Session["import_item_last_cost"].ToString();
		Session["import_item_last_cost"] = null;
	}
	if(Session["import_item_stock"] != null)
	{
		stock = Session["import_item_stock"].ToString();
		Session["import_item_stock"] = null;
	}
	if(Session["import_item_eta"] != null)
	{
		eta = Session["import_item_eta"].ToString();
		Session["import_item_eta"] = null;
	}
	if(Session["import_item_location"] != null)
	{
		location = Session["import_item_location"].ToString();
		Session["import_item_location"] = null;
	}
	if(Session["import_item_expire_date"] != null)
	{
		expire_date = Session["import_item_expire_date"].ToString();
		Session["import_item_expire_date"] = null;
	}
	if(Session["import_item_price1"] != null)
	{
		price1 = Session["import_item_price1"].ToString();
		Session["import_item_price1"] = null;
	}
	if(Session["import_item_price2"] != null)
	{
		price2 = Session["import_item_price2"].ToString();
		Session["import_item_price2"] = null;
	}
	if(Session["import_item_price3"] != null)
	{
		price3 = Session["import_item_price3"].ToString();
		Session["import_item_price3"] = null;
	}
	if(Session["import_item_price4"] != null)
	{
		price4 = Session["import_item_price4"].ToString();
		Session["import_item_price4"] = null;
	}
	if(Session["import_item_price5"] != null)
	{
		price5 = Session["import_item_price5"].ToString();
		Session["import_item_price5"] = null;
	}
	if(Session["import_item_price6"] != null)
	{
		price6 = Session["import_item_price6"].ToString();
		Session["import_item_price6"] = null;
	}
	if(Session["import_item_spec"] != null)
	{
		spec = Session["import_item_spec"].ToString();
		Session["import_item_spec"] = null;
	}
//select branch where to update..
	if(Session["import_item_branch"] != null)
	{
		branch = Session["import_item_branch"].ToString();
		Session["import_item_branch"] = null;
	}
	if(Session["import_item_inner_pack"] != null)
	{
		inner_pack = Session["import_item_inner_pack"].ToString();
		Session["import_item_inner_pack"] = null;
	}
	if(Session["import_item_outer_pack"] != null)
	{
		outer_pack = Session["import_item_outer_pack"].ToString();
		Session["import_item_outer_pack"] = null;
	}
	if(Session["import_item_moq"] != null)
	{
		moq = Session["import_item_moq"].ToString();
		Session["import_item_moq"] = null;
	}

	Trim(ref supplier);
	Trim(ref supplier_code);
	Trim(ref ref_code);
	Trim(ref barcode);
		
	bool bExistingItem = false;
	//DEBUG("update procuc=", m_ProductUpdate);
	//if(barcode == "")
	//	barcode = supplier_code;
	
	id = supplier + supplier_code;
	//to prevent error on import to system.
	if(supplier_code == "" || supplier_code == null)
	{
		supplier_code = ref_code;
		id += ref_code;
	}

	if(id == "")
	{
		m_nNoCodeItem++;
		return true; //skip this one
	}
	if(barcode != "")
	{
		if(BarcodeExists(barcode))
		{
			m_nExistsBarcode++;
			if(m_ProductUpdate.ToLower() != "on")
				return true;
		}
	}
//DEBUG("id=", id);
	string existedCode = "";
	if(ItemExists(id, ref existedCode))
	{
		m_nExistsItem++;
		bExistingItem = true;
		if(m_ProductUpdate.ToLower() != "on") //filter with product update or not...if it's update then just do the update
			return true;
	}
//DEBUG("	existedCode =", existedCode);
//DEBUG("supplier_price =", supplier_price);
//return true;
	double dsupplier_price = MyCurrencyPrice(supplier_price);
	double daverage_cost = MyCurrencyPrice(average_cost);
//	if(dsupplier_price == 0)
//		dsupplier_price = dprice1;
	if(dsupplier_price == 0)
		dsupplier_price = daverage_cost;
//	if(dsupplier_price == 0)
//	{
//		m_nNoPriceItem++;
//		return true; //skip this one
//	}
	
	manual_cost_frd = dsupplier_price.ToString();
	manual_cost_nzd = manual_cost_frd;
	manual_cost_frd = manual_cost_frd.Replace("$", "");
	manual_cost_frd = manual_cost_frd.Replace(",", "");
	manual_cost_nzd = manual_cost_nzd.Replace("$", "");
	manual_cost_nzd = manual_cost_nzd.Replace(",", "");
	if(average_cost == "" || average_cost == null)
		average_cost = "0";
	if(supplier_price == "" || supplier_price == null)
		supplier_price = "0";

	supplier_price = supplier_price.Replace("$", "");
	supplier_price = supplier_price.Replace(",", "");
	average_cost = average_cost.Replace("$", "");
	average_cost = average_cost.Replace(",", "");
    
	if(manual_cost_frd == "0")
	{
		manual_cost_frd = "1";
		manual_cost_nzd = manual_cost_frd;
	}
	
	double dStock = 0;
	Trim(ref stock);
	if(stock != "")
	{
		try
		{
			dStock = double.Parse(stock);
		}
		catch(Exception e) 
		{
			m_nStockErrorItem++;
			return true;
		}
	}
	stock = ((int)dStock).ToString();
	eta = eta;
	price2 = MyCurrencyPrice(price2).ToString();
	price3 = MyCurrencyPrice(price3).ToString();
	price4 = MyCurrencyPrice(price4).ToString();
	price5 = MyCurrencyPrice(price5).ToString();
	price6 = MyCurrencyPrice(price6).ToString();
	if(price1 == "")
		price1 = "0";
	price1 = price1.Replace("$", "");
	price1 = price1.Replace(",", "");
	price1 = price1.Replace(" ", "");

    if(price3 == "")
		price3 = "0";
	price3 = price3.Replace("$", "");
	price3 = price3.Replace(",", "");
	price3 = price3.Replace(" ", "");

	double dprice1 = 0;
    double dprice3 = 0;
	Trim(ref price1);
    Trim(ref price3);
//DEBUG("rpie =", price1);
	if(price1 != "")
	{
		try
		{
			dprice1 = MyCurrencyPrice(price1);
		}
		catch(Exception e) 
		{
			m_nNoPriceItem++;
			return true;
		}
	}
    dprice3 = MyCurrencyPrice(price3);
	//price1 = Math.Round(dprice1 * 1.125, 2).ToString();
	price1 = Math.Round(dprice1, 2).ToString();

    price3 = Math.Round(dprice3, 2).ToString();
	int nCode = GetNextCode();
	if(nCode <= 0)
	{
		Response.Write("Error generating code for new product");
		return false;
	}

	if(cat == "")
		cat = "Product";
	double costnzd = 1;
	if(MyCurrencyPrice(manual_cost_nzd) != 0)
		costnzd = MyCurrencyPrice(manual_cost_nzd);
	double[] level_rate = new double[10];

	for(int i=1; i<=9; i++)
	{
		level_rate[i] = double.Parse(GetSiteSettings("set_import_level_rate"+i, "1.1", false));
		try
		{
			level_rate[i] = level_rate[i];
		}	
		catch(Exception e)
		{
			level_rate[i] = 2;
		}
	}
	//double drate = 1 + Math.Round((MyDoubleParse(price1) - costnzd) / costnzd, 4); 
	double drate = 1.1;
	try
	{
		drate = double.Parse(GetSiteSettings("default_bottom_rate", "1.1", false));
	}
	catch(Exception e)
	{
	}
	
	inner_pack = MyDoubleParse(inner_pack).ToString();
	outer_pack = MyDoubleParse(outer_pack).ToString();
	moq = MyDoubleParse(moq).ToString();
	
	string sc = " BEGIN TRANSACTION ";
	if(m_ProductUpdate.ToLower() == "on" && branch != null && branch != "" && TSIsDigit(branch))
	{
		//update only item name, price, cost 
		if(bExistingItem)
		{
			if(existedCode != "" && existedCode != null)
			{				
				sc += " IF (SELECT SUM(qty) FROM stock_qty WHERE code = "+ existedCode +" ) <= 0 ";
				sc += " BEGIN ";
				sc += " UPDATE product SET name = N'" + EncodeQuote(name) + "', supplier_price = " + supplier_price;
				sc += ", stock = " + stock +", eta = '" + EncodeQuote(eta) + "' WHERE code = "+ existedCode +" ";						
				sc += " UPDATE code_relations SET name = N'" + EncodeQuote(name) + "', price1 = "+ price1 +" , supplier_price = " + supplier_price;
				sc += ", inner_pack = " + inner_pack + ", weight = " + outer_pack + ", moq = " + moq + " ";
				sc += ", level_price0 ='"+ MyDoubleParse(price3) +"'";
				sc += " WHERE code = "+ existedCode + " ";
				sc += " END ";
				sc += " ELSE ";
				sc += " BEGIN ";
				sc += " UPDATE product SET name = N'" + EncodeQuote(name) + "'";
				sc += ", stock = " + stock +", eta = '" + EncodeQuote(eta) + "' WHERE code = "+ existedCode +" ";						
				sc += " UPDATE code_relations SET name = N'" + EncodeQuote(name) + "', inner_pack = " + inner_pack + ", weight = " + outer_pack + ", moq = " + moq + " ";
				sc += " WHERE code = "+ existedCode +" ";
				sc += " END ";
				sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code = "+ existedCode + " AND branch_id = " + branch + ") ";
				sc += " INSERT INTO stock_qty (code, qty, supplier_price, average_cost, branch_id) VALUES(" + nCode.ToString() + ", " + stock + ", " + supplier_price + ", " + supplier_price + ", "+ branch +") ";
				sc += " ELSE UPDATE stock_qty SET qty = " + stock + ", supplier_price = "+ supplier_price +" WHERE code = "+ existedCode +" AND branch_id = "+ branch +" ";

				if(spec != "" && spec != null)
				{
					sc += " IF NOT EXISTS (SELECT code FROM product_details WHERE code = "+ nCode.ToString() +") ";
					sc += " BEGIN  INSERT INTO product_details (code, spec ) VALUES("+ nCode.ToString() +", '"+ EncodeQuote(spec) +"') END ";
					sc += " ELSE ";
					sc += " BEGIN  UPDATE product_details SET spec = '"+ EncodeQuote(spec) +"' WHERE code = "+ nCode.ToString() +"  END ";
				}
//DEBUG("sc=", sc);
			}
		}
		else
		{
			sc += " INSERT INTO code_relations (code, id, supplier, supplier_code, ref_code, name, brand, cat, s_cat, ss_cat ";
			sc += ", barcode, average_cost, supplier_price, manual_cost_frd, manual_cost_nzd, stock_location, expire_date ";
			sc += ", price1, price2, price3, price4, price5, price6, rate, inner_pack, weight, moq , level_rate1, level_rate2, level_rate3, level_rate4";
			sc += ", level_rate5, level_rate6, level_rate7, level_rate8, level_rate9,level_price0, level_price1, level_price2, level_price3, level_price4";
			sc += ", level_price5, level_price6, level_price7, level_price8, level_price9";
			sc += "  ) VALUES(" + nCode.ToString();
			sc += ", '" + EncodeQuote(id) + "' ";
			sc += ", N'" + EncodeQuote(supplier) + "' ";
			sc += ", '" + EncodeQuote(supplier_code) + "' ";
			sc += ", '" + EncodeQuote(ref_code) + "' ";
			sc += ", N'" + EncodeQuote(name) + "' ";
			sc += ", '" + EncodeQuote(brand) + "' ";
			sc += ", N'" + EncodeQuote(cat) + "' ";
			sc += ", N'" + EncodeQuote(s_cat) + "' ";
			sc += ", N'" + EncodeQuote(ss_cat) + "' ";
			sc += ", '" + EncodeQuote(barcode) + "' ";
			sc += ", " + average_cost;
			sc += ", " + supplier_price;
			sc += ", " + manual_cost_frd;
			sc += ", " + manual_cost_nzd;
			sc += ", '" + EncodeQuote(location) + "' ";
			sc += ", '" + EncodeQuote(expire_date) + "' ";
			sc += ", " + price1;
			sc += ", " + price2;
			sc += ", " + price3;
			sc += ", " + price4;
			sc += ", " + price5;
			sc += ", " + price6;
			sc += ", "+ drate +"";
			sc += ", " + inner_pack;
			sc += ", " + outer_pack;
			sc += ", " + moq;
			sc += ", '"+  GetDealerLevelRate("1") +"'";
			sc += ", '"+  GetDealerLevelRate("2") +"'";
			sc += ", '"+  GetDealerLevelRate("3") +"'";
			sc += ", '"+  GetDealerLevelRate("4") +"'";
			sc += ", '"+  GetDealerLevelRate("5") +"'";
			sc += ", '"+  GetDealerLevelRate("6") +"'";
			sc += ", '"+  GetDealerLevelRate("7") +"'";
			sc += ", '"+  GetDealerLevelRate("8") +"'";
			sc += ", '"+  GetDealerLevelRate("9") +"'";
			sc += ", '"+ price3 +"'";
			sc += ", '"+  GetDealerLevelRate("1") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("2") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("3") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("4") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("5") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("6") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("7") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("8") * MyDoubleParse(price3) +"'";
			sc += ", '"+  GetDealerLevelRate("9") * MyDoubleParse(price3) +"'";
			sc += ") ";
			
			sc += " INSERT INTO barcode (item_code, barcode, item_qty, carton_qty, carton_barcode, box_qty, package_price, supplier_code) ";
			sc += " VALUES ('"+ nCode.ToString() +"', '"+ EncodeQuote(barcode)+"',1,0,1,'',1,'0','"+ EncodeQuote(supplier_code)+"')";
			
			sc += " INSERT INTO product (code, supplier, supplier_code, name, brand, cat, s_cat, ss_cat, supplier_price ";
			sc += ", price, stock, eta) VALUES(" + nCode.ToString();
			sc += ", '" + EncodeQuote(supplier) + "' ";
			sc += ", '" + EncodeQuote(supplier_code) + "' ";
			sc += ", N'" + EncodeQuote(name) + "' ";
			sc += ", N'" + EncodeQuote(brand) + "' ";
			sc += ", N'" + EncodeQuote(cat) + "' ";
			sc += ", N'" + EncodeQuote(s_cat) + "' ";
			sc += ", N'" + EncodeQuote(ss_cat) + "' ";
			sc += ", " + supplier_price;
			sc += ", " + price1;
			sc += ", " + stock;
			sc += ", '" + EncodeQuote(eta) + "') ";	
			if(spec != "" && spec != null)
			{
				sc += " IF NOT EXISTS (SELECT code FROM product_details WHERE code = "+ nCode.ToString() +") ";
				sc += " BEGIN  INSERT INTO product_details (code, spec ) VALUES("+ nCode.ToString() +", N'"+ EncodeQuote(spec) +"') END ";
				sc += " ELSE ";
				sc += " BEGIN  UPDATE product_details SET spec = N'"+ EncodeQuote(spec) +"' WHERE code = "+ nCode.ToString() +"  END ";
			}
			sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code = "+ nCode.ToString() + " AND branch_id = " + branch + ") ";
			sc += " INSERT INTO stock_qty (code, qty, supplier_price, average_cost, branch_id) VALUES(" + nCode.ToString() + ", " + stock + ", " + supplier_price + ", " + supplier_price + ", "+ branch +") ";
			sc += " ELSE UPDATE stock_qty SET qty = " + stock + ", supplier_price = "+ supplier_price +", average_cost = "+ supplier_price +" WHERE code = "+ nCode.ToString() + " AND branch_id = " + branch + " ";
		}
	}
	else
	{
		sc += " INSERT INTO code_relations (code, id, supplier, supplier_code, ref_code, name, brand, cat, s_cat, ss_cat ";
		sc += ", barcode, average_cost, supplier_price, manual_cost_frd, manual_cost_nzd, stock_location, expire_date ";
		sc += ", price1, price2, price3, price4, price5, price6, rate, inner_pack, weight, moq, level_rate1, level_rate2, level_rate3, level_rate4";
		sc += ", level_rate5, level_rate6, level_rate7, level_rate8, level_rate9, level_price0, level_price1, level_price2, level_price3, level_price4";
		sc += ", level_price5, level_price6, level_price7, level_price8, level_price9";
		sc += "  ) VALUES(" + nCode.ToString();
		sc += ", '" + EncodeQuote(id) + "' ";
		sc += ", N'" + EncodeQuote(supplier) + "' ";
		sc += ", '" + EncodeQuote(supplier_code) + "' ";
		sc += ", '" + EncodeQuote(ref_code) + "' ";
		sc += ", N'" + EncodeQuote(name) + "' ";
		sc += ", N'" + EncodeQuote(brand) + "' ";
		sc += ", N'" + EncodeQuote(cat) + "' ";
		sc += ", N'" + EncodeQuote(s_cat) + "' ";
		sc += ", N'" + EncodeQuote(ss_cat) + "' ";
		sc += ", '" + EncodeQuote(barcode) + "' ";
		sc += ", " + average_cost;
		sc += ", " + supplier_price;
		sc += ", " + manual_cost_frd;
		sc += ", " + manual_cost_nzd;
		sc += ", '" + EncodeQuote(location) + "' ";
		sc += ", '" + EncodeQuote(expire_date) + "' ";
		sc += ", " + price1;
		sc += ", " + price2;
		sc += ", " + price3;
		sc += ", " + price4;
		sc += ", " + price5;
		sc += ", " + price6;
		sc += ", "+ drate +"";
		sc += ", " + inner_pack;
		sc += ", " + outer_pack;
		sc += ", " + moq;
		sc += ", '"+  GetDealerLevelRate("1") +"'";
		sc += ", '"+  GetDealerLevelRate("2") +"'";
		sc += ", '"+  GetDealerLevelRate("3") +"'";
		sc += ", '"+  GetDealerLevelRate("4") +"'";
		sc += ", '"+  GetDealerLevelRate("5") +"'";
		sc += ", '"+  GetDealerLevelRate("6") +"'";
		sc += ", '"+  GetDealerLevelRate("7") +"'";
		sc += ", '"+  GetDealerLevelRate("8") +"'";
		sc += ", '"+  GetDealerLevelRate("9") +"'";
		sc += ", '"+ price3 +"'";
		sc += ", '"+  GetDealerLevelRate("1") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("2") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("3") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("4") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("5") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("6") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("7") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("8") * MyDoubleParse(price3) +"'";
		sc += ", '"+  GetDealerLevelRate("9") * MyDoubleParse(price3) +"'";
		sc += ") ";

		sc += " INSERT INTO barcode (item_code, barcode, item_qty, carton_qty, carton_barcode, box_qty, package_price, supplier_code) ";
		sc += " VALUES ('"+ nCode.ToString() +"', '"+ EncodeQuote(barcode)+"',1,0,'',1,'0','"+ EncodeQuote(supplier_code)+"')";

		sc += " INSERT INTO product (code, supplier, supplier_code, name, brand, cat, s_cat, ss_cat, supplier_price ";
		sc += ", price, stock, eta) VALUES(" + nCode.ToString();
		sc += ", N'" + EncodeQuote(supplier) + "' ";
		sc += ", '" + EncodeQuote(supplier_code) + "' ";
		sc += ", N'" + EncodeQuote(name) + "' ";
		sc += ", '" + EncodeQuote(brand) + "' ";
		sc += ", N'" + EncodeQuote(cat) + "' ";
		sc += ", N'" + EncodeQuote(s_cat) + "' ";
		sc += ", N'" + EncodeQuote(ss_cat) + "' ";
		sc += ", " + supplier_price;
		sc += ", " + price1;
		sc += ", " + stock;
		sc += ", '" + EncodeQuote(eta) + "') ";

		if(spec != "" && spec != null)
		{
			sc += " IF NOT EXISTS (SELECT code FROM product_details WHERE code = "+ nCode.ToString() +") ";
			sc += " BEGIN  INSERT INTO product_details (code, spec ) VALUES("+ nCode.ToString() +", N'"+ EncodeQuote(spec) +"') END ";
			sc += " ELSE ";
			sc += " BEGIN  UPDATE product_details SET spec = N'"+ EncodeQuote(spec) +"' WHERE code = "+ nCode.ToString() +"  END ";
		}

		sc += " IF NOT EXISTS (SELECT code FROM stock_qty WHERE code = "+ nCode.ToString() +") ";
		sc += " INSERT INTO stock_qty (code, qty, supplier_price, average_cost) VALUES(" + nCode.ToString() + ", " + stock + ", " + supplier_price + ", " + supplier_price + ") ";
		sc += " ELSE UPDATE stock_qty SET qty = qty + "+ stock +", supplier_price = (supplier_price + "+ supplier_price +") / 2, average_cost = (supplier_price + "+ supplier_price +") / 2 WHERE code = "+ nCode.ToString() +" ";
		if(!m_bUPDATEREGULARLY)
		{
			sc += " UPDATE account SET opening_balance = opening_balance + "+ supplier_price +" WHERE (class1 *1000) + (class2 * 100) + (class3 * 10) + (class4) = 1121 ";
			sc += " UPDATE account SET opening_balance = opening_balance + "+ supplier_price +" WHERE (class1 *1000) + (class2 * 100) + (class3 * 10) + (class4) = 2111 ";
		}
	}
	sc += " COMMIT ";
//DEBUG("sc=", sc);
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
	if(bExistingItem)
		m_nUpdatedItem++;
	else
		m_nNewItem++;
	return true;
}

bool ItemExists(string id, ref string code)
{
	if(ds.Tables["exists"] != null)
		ds.Tables["exists"].Clear();
	string sc = " SELECT code FROM code_relations WHERE id = '" + EncodeQuote(id) + "' ";
//DEBUG("sc =", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "exists") > 0)
		{
			code = ds.Tables["exists"].Rows[0]["code"].ToString();
			return true;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		Response.End();
		return false;
	}
	return false;
}

bool BarcodeExists(string barcode)
{
	string sc = " SELECT code FROM code_relations WHERE barcode LIKE '" + EncodeQuote(barcode) + "' ";
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
//		DEBUG("aColumn=", aColumn[i].ToString());
	}
	if(m_bTestFormat)
		sbTest.Append("<tr>");
	for(i=0; i<aNameCount; i++)
	{
		if(aName[i] == "sub_cat_seperator" || aName[i] == "lines_to_skip" || aName[i] == "product_update")
			continue;			
		if(aValue[i] == "" || aValue[i] == "0")
			continue;		
		string v = "";
		int n = MyIntParse(aValue[i]);
		if(n >= 0)
		{
			if(aName[i] == "branch")
				v = n.ToString();
			else
				v = aColumn[n];
		}
		else
		{
			if(aName[i] == "s_cat")
				v = m_last_scat;
			else if(aName[i] == "ss_cat")
				v = m_last_sscat;
		}
		if(m_subCatSeperator != "" && aName[i] == "cat")
			v = GetSubCat(v);
		if(m_bTestFormat)
			sbTest.Append("<td>" + v + "</td>");
		else
		{
			Session["import_item_" + aName[i]] = v;
		}
	}
	if(m_bTestFormat)
		sbTest.Append("</tr>");
	else
	{
		if(!DoAddItem())
			return false;
	}
	return true;
}

string GetSubCat(string cat)
{
	m_last_scat = "";
	m_last_sscat = "";

	if(cat == null || cat == "")
		return cat;
	string sp = m_subCatSeperator;
	int p1 = cat.IndexOf(sp);
	if(p1 <= 0)
		return cat;
	int p2 = cat.IndexOf(sp, p1 + 1);
	if(p2 <= 0)
		p2 = cat.Length;
	
	string c = cat.Substring(0, p1);
	m_last_scat = cat.Substring(p1 + 1, p2 - p1 - 1);
	if(p2 != cat.Length)
		m_last_sscat = cat.Substring(p2 + 1, cat.Length - p2 - 1);
	return c;
}

bool DoProcessFile()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = "data/item/";
	root = Server.MapPath(root);
	string fileName = root + m_file;
//DEBUG("fileName = ", fileName);
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
//if(i > 10)
//	break;
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
		MonitorProcess(300);
		line = r.ReadLine();
	}
//DEBUG("i = ", i);
	r.Close();
	fs.Close();
	Response.Write("done.<br>");
	Response.Write("<h5>Total lines processed : <b>" + (i - m_nSkipLine - 1).ToString() + "</b><br>");
	Response.Write("Existing Items : <b>" + m_nExistsItem + "</b><br>");
	Response.Write("Existing Barcode : <b>" + m_nExistsBarcode + "</b><br>");
	Response.Write("No Code Items(skipped) : <b>" + m_nNoCodeItem + "</b><br>");
	Response.Write("Price Error Items(skipped) : <b>" + m_nNoPriceItem + "</b><br>");
	Response.Write("Stock Error Items(skipped) : <b>" + m_nStockErrorItem + "</b><br>");
	Response.Write("New Items(Imported) : <b>" + m_nNewItem + "</b><br>");
	Response.Write("Updated Items : <b>" + m_nUpdatedItem + "</b></h5>");	
	Response.Write("<h4><a href=ec.aspx class=o>Update Category</a> &nbsp;&nbsp;");
	Response.Write(" <a href=default.aspx class=o>Done</a></h4>");
	return bRet;
}

bool DoProcessBarcodeSupplier()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = "data/item/barcodeandsupply.csv";
	root = Server.MapPath(root);
	string fileName = root + m_file;
//DEBUG("fileName = ", fileName);
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	Response.Write("done.<br>");
	Response.Write("Processing, please wait...");
	
	string line = r.ReadLine();
	line = r.ReadLine(); //skip one
	string text = "";

	bool bRet = true;
	int i = 0;
	while(line != null)
	{
//if(i > 10)
//	break;
		i++;
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		text += "\r\n";
		text += line;
		if(!ProcessLinePB(line))
		{
			bRet = false;
			break;
		}
		MonitorProcess(300);
		line = r.ReadLine();
	}
//DEBUG("i = ", i);
	r.Close();
	fs.Close();
	Response.Write("done.<br>");
	
	return true;
}
bool ProcessLinePB(string sLine)
{
//Barcode,M-PN,Chinese Name,BOX Qty,Supply,Phone,Bank,Bank Address,Bank Name,Account No
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 0;
	for(i=0; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		aColumn[i] = CSVNextColumn(cb, ref pos);
//DEBUG("aColumn=", aColumn[i].ToString());
	}
	
	string barcode = aColumn[0];
	string m_pn = aColumn[1];
	Trim(ref barcode);
	Trim(ref m_pn);
	if(barcode == "")
		return true;
	if(m_pn == m_pn_last)
		return true;
	m_pn_last = m_pn;
	string name_cn = aColumn[2];
	string boxed_qty = aColumn[3];
	string supplier = aColumn[4];
	string phone = aColumn[5];
	string bank = aColumn[6] + " " + aColumn[7] + " " + aColumn[8] + " " + aColumn[9];
	Trim(ref bank);
//DEBUG("barcode=" + barcode + ",m_pn=" + m_pn + ",name_cn=" + name_cn + ",boxed_qty=" + boxed_qty + ",supplier=" + supplier + ",phone=" + phone + ", bank=", bank);	
	
//	string sc = " IF EXISTS(SELECT code FROM code_relations WHERE supplier_code = N'" + EncodeQuote(m_pn) + "') ";
	string sc = " UPDATE code_relations SET name_cn = N'" + EncodeQuote(name_cn) + "' ";
	sc += ", barcode = N'" + EncodeQuote(barcode) + "' ";
	sc += ", boxed_qty = N'" + EncodeQuote(boxed_qty) + "' ";
	sc += " WHERE supplier_code = N'" + EncodeQuote(m_pn) + "' ";
	
	sc += " IF NOT EXISTS(SELECT id FROM card WHERE trading_name = N'" + EncodeQuote(supplier) + "') ";
	sc += " INSERT INTO card (email, type, name, company, trading_name, short_name, phone, note) VALUES(";
	sc += " N'" + EncodeQuote(supplier) + "', 3 ";
	sc += ", N'" + EncodeQuote(supplier) + "' ";
	sc += ", N'" + EncodeQuote(supplier) + "' ";
	sc += ", N'" + EncodeQuote(supplier) + "' ";
	sc += ", N'" + EncodeQuote(supplier) + "' ";
	sc += ", N'" + EncodeQuote(phone) + "' ";
	sc += ", N'" + EncodeQuote(bank) + "' ";
	sc += ") ";
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

bool DoProcessProduct()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = "data/item/product.csv";
	root = Server.MapPath(root);
	string fileName = root + m_file;
//DEBUG("fileName = ", fileName);
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	Response.Write("done.<br>");
	Response.Write("Processing, please wait...");
	
	string line = r.ReadLine();
	line = r.ReadLine(); //skip one
	string text = "";

	bool bRet = true;
	int i = 0;
	while(line != null)
	{
//if(i > 10)
//	break;
		i++;
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		text += "\r\n";
		text += line;
		if(!ProcessLinePP(line))
		{
			bRet = false;
			break;
		}
		MonitorProcess(300);
		line = r.ReadLine();
	}
//DEBUG("i = ", i);
	r.Close();
	fs.Close();
	Response.Write("done.<br>");
	
	return true;
}
bool ProcessLinePP(string sLine)
{
//Barcode,M-PN,Chinese Name,BOX Qty,Supply,Phone,Bank,Bank Address,Bank Name,Account No
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 0;
	for(i=0; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		aColumn[i] = CSVNextColumn(cb, ref pos);
//DEBUG("aColumn=", aColumn[i].ToString());
	}
	
	string id = aColumn[0];
	string m_pn = aColumn[1];
	Trim(ref m_pn);
	if(m_pn == m_pn_last)
		return true;
	m_pn_last = m_pn;
	double dPrice = MyMoneyParse(aColumn[2]);
	dPrice /= 1.125;
	string pic = aColumn[3];
	string stock_location = aColumn[4];
	
	string sc = " UPDATE code_relations SET ref_code = N'" + EncodeQuote(pic) + "' ";
	sc += ", stock_location = N'" + EncodeQuote(stock_location) + "' ";
	sc += ", price2 = price1 ";
	sc += ", price1 = " + dPrice.ToString();
	sc += " WHERE supplier_code = N'" + EncodeQuote(m_pn) + "' ";
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

bool DoProcessMOQ()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = "data/item/moq.csv";
	root = Server.MapPath(root);
	string fileName = root + m_file;
//DEBUG("fileName = ", fileName);
	FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
	StreamReader r = new StreamReader(fs);
	r.BaseStream.Seek(0, SeekOrigin.Begin);
	Response.Write("done.<br>");
	Response.Write("Processing, please wait...");
	
	string line = r.ReadLine();
	line = r.ReadLine(); //skip one
	string text = "";

	bool bRet = true;
	int i = 0;
	while(line != null)
	{
//if(i > 10)
//	break;
		i++;
		if(i == m_nSkipLine)
		{
			line = r.ReadLine();
			continue;
		}
		text += "\r\n";
		text += line;
		if(!ProcessLineMOQ(line))
		{
			bRet = false;
			break;
		}
		MonitorProcess(300);
		line = r.ReadLine();
	}
//DEBUG("i = ", i);
	r.Close();
	fs.Close();
	Response.Write("done.<br>");
	
	return true;
}
bool ProcessLineMOQ(string sLine)
{
//Barcode,M-PN,Chinese Name,BOX Qty,Supply,Phone,Bank,Bank Address,Bank Name,Account No
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 0;
	for(i=0; i<64; i++)
	{
		if(pos >= cb.Length)
			break;
		aColumn[i] = CSVNextColumn(cb, ref pos);
//DEBUG("aColumn=", aColumn[i].ToString());
	}
	
	string barcode = aColumn[0];
	string m_pn = aColumn[1];
	Trim(ref m_pn);
	if(m_pn == m_pn_last)
		return true;
	m_pn_last = m_pn;
	int nOuter = MyIntParseNoWarning(aColumn[2]);
	int nInner = MyIntParseNoWarning(aColumn[3]);
	int nMoq = MyIntParseNoWarning(aColumn[4]);
	
	string sc = " UPDATE code_relations SET barcode = N'" + EncodeQuote(barcode) + "' ";
	sc += ", weight = " + nOuter + " ";
	sc += ", inner_pack = " + nInner + " ";
	sc += ", moq = " + nMoq;
	sc += " WHERE supplier_code = N'" + EncodeQuote(m_pn) + "' ";
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
int MyIntParseNoWarning(string s)
{
	int n = 0;
	try
	{
		n = int.Parse(s);
	}
	catch(Exception e)
	{
	}
	return n;
}
bool DoProcessBook1()
{
	PrintAdminHeader();
	Response.Write("Opening file....");
	string root = "data/item/book1.csv";
	root = Server.MapPath(root);
	string fileName = root + m_file;
//DEBUG("fileName = ", fileName);
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
//if(i > 10)
//	break;
		i++;
		if(!ProcessLineBook1(line))
		{
			bRet = false;
			break;
		}
		MonitorProcess(300);
		line = r.ReadLine();
	}
//DEBUG("i = ", i);
	r.Close();
	fs.Close();
	Response.Write("done.<br>");
	
	return true;
}
bool ProcessLineBook1(string sLine)
{
	char[] cb = sLine.ToCharArray();
	int pos = 0;
	int i = 0;
	string m_pn = CSVNextColumn(cb, ref pos);
	string price1 = CSVNextColumn(cb, ref pos);
	string barcode = CSVNextColumn(cb, ref pos);
	
	Trim(ref barcode);
	Trim(ref m_pn);
	if(m_pn == m_pn_last)
		return true;
	m_pn_last = m_pn;
	double dPrice = MyMoneyParse(price1);
	
	string sc = " UPDATE code_relations SET barcode = N'" + EncodeQuote(barcode) + "' ";
	sc += ", price1 = " + dPrice.ToString();
	sc += " WHERE supplier_code = N'" + EncodeQuote(m_pn) + "' ";
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
double GetDealerLevelRate(string i)
{
	if(dst.Tables["rate"] != null)
		dst.Tables["rate"].Clear();
	string sc = "SELECT ISNULL(value, 1) AS value FROM settings WHERE name ='set_import_level_rate"+i+"'";
	//DEBUG("sc=", sc);
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "rate") <= 0)
			return 1;
	}
	catch(Exception ex)
	{
		ShowExp(sc, ex);
		myConnection.Close();
		return 1;
	}
	if(dst.Tables["rate"].Rows.Count == 1)
	{
		return MyDoubleParse(dst.Tables["rate"].Rows[0]["value"].ToString());
	}
	return 1;
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
<h5><a href=importc.aspx class=o>Import Customer</a></h5>

<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="iTextSharp" %>
<%@ Import Namespace="iTextSharp.text" %>
<%@ Import Namespace="iTextSharp.text.html" %>
<%@ Import Namespace="iTextSharp.text.pdf" %>
<%@ Import Namespace="iTextSharp.text.html.simpleparser" %>

<script runat=server>

string ebrand;
string ecat;
string es_cat;
string ess_cat;

string brand;
string cat;
string s_cat;
string ss_cat;
string m_kw = "";

int m_iTotalItems = 0;

string m_type = "";
string m_cmd = "";
string m_sWithStock = "";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

int m_nItemsPerRow = 1;
int m_nItemRowsPerPage = 1;
int total = 0;

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	m_type = g("t");
	m_cmd = p("cmd");
	brand = g("b");
	cat = g("c");
	s_cat = g("s");
	ss_cat = g("ss");
	m_kw = p("search");
	if(m_kw == "")
		m_kw = g("search");
	if(p("with_stock") == "1")
		m_sWithStock = "checked";

	ebrand = HttpUtility.UrlEncode(brand);
	ecat = HttpUtility.UrlEncode(cat);
	es_cat = HttpUtility.UrlEncode(s_cat);
	ess_cat = HttpUtility.UrlEncode(ss_cat);

	if(!DoSearch())
		return;

	if(m_cmd == "Print")
	{
		CreatePDFFile();
		return;
	}

	PrintAdminHeader();
	PrintAdminMenu();

	if(m_type == "done")
	{
		string fn = g("fn");
		Response.Write("<center><h4>PDF Catalgo</h4>");
		Response.Write("<br>click to download : <a href=" + fn + " class=o>" + fn + "</a><br><br>");
		Response.Write("<input type=button class=b value='New Catalog' onclick=\"window.location='?';\">");
		return;
	}
	MyDrawTable();
	PrintAdminFooter();
}
Boolean DoSearch()
{
	if(dst.Tables["product"] != null)
		dst.Tables["product"].Clear();
	string sc = " SELECT ";
	sc += " * FROM (SELECT ";
	if(m_kw == "" && cat == "")
		sc += " TOP 20 ";
	sc += " c.id, c.name_cn, c.price1, c.level_price0, c.code, c.brand, c.name, c.cat, c.s_cat, c.ss_cat, c.average_cost, c.supplier_code, c.barcode ";
	sc += ", (SELECT SUM(qty) FROM stock_qty WHERE code = c.code) AS stock, c.supplier_price, c.rate ";
	sc += " FROM code_relations c  ";
	sc += " LEFT OUTER JOIN barcode b ON c.code = b.item_code";
	sc += " WHERE 1 = 1 ";
	if(brand != "")
		sc += " AND c.brand = N'" + brand + "'";
	else if(cat != "")
		sc += "  AND c.cat = N'" + cat + "'";
	if(s_cat != "")
		sc += " AND c.s_cat= N'" + s_cat + "'";
	if(ss_cat != "")
		sc += " AND c.ss_cat= N'" + ss_cat + "'";
	if(m_kw != "")
	{
		string kw = EncodeQuote(m_kw);
		if(TSIsDigit(m_kw))
			sc += " AND (c.code = " + kw +" OR b.barcode = '" + kw + "' OR c.supplier_code = N'" + kw + "') ";
		else
			sc += " AND (b.barcode = '" + kw + "' OR c.supplier_code = N'"+ kw + "' OR LOWER(c.name) LIKE N'%" + kw.ToLower() + "%' OR LOWER(c.name_cn) LIKE N'%" + kw.ToLower() + "%' ) ";
	}
	sc += ") AS dt ";
	sc += " WHERE 1 = 1 ";
	if(m_sWithStock != "")
		sc += " AND stock > 0 ";
	sc += " ORDER BY cat, supplier_code ";
//DEBUG("sc=", sc);	
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_iTotalItems = myAdapter.Fill(dst, "product");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	return true;
}
bool MyDrawTable()
{
	Response.Write("<form action=?b=" + ebrand + "&c=" + ecat + "&s=" + es_cat + "&ss=" + ess_cat + " method=post>\r\n");
	Response.Write("<center><h4>PDF Catalog</h4>");

	Response.Write("<table width=98% align=center valign=center cellspacing=0 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr><td colspan=6>");
	PrintCatalogSelection();
	Response.Write("&nbsp;Search:<input size=10 name=search value='" + m_kw + "'>");
	Response.Write("&nbsp;Search:<input type=checkbox name=with_stock value=1 " + m_sWithStock + ">Qty>0 &nbsp;");
	Response.Write("<input type=submit name=cmd value='GO' class=b>");
	Response.Write("<input type=submit name=cmd value='Print' class=b>");
	Response.Write("</td></tr>");

	StringBuilder sb = new StringBuilder();;
	sb.Append("<tr style=\"color:white;background-color:#666696;font-weight:bold;\">\r\n");
	sb.Append("<td width=50>CODE</td>");
	sb.Append("<td width=50>BARCODE</td>");
	sb.Append("<td>CAT</td>");
	sb.Append("<td>DESCRIPTION</td>");
	sb.Append("<td>PRICE</td>");
	sb.Append("<td>STOCK</td>");
	sb.Append("</tr>\r\n");
	
	Response.Write(sb.ToString());

	DataRow dr;
	for(int i=0; i<dst.Tables["product"].Rows.Count; i++)
	{
		dr = dst.Tables["product"].Rows[i];
		string id = dr["id"].ToString();
		string mpn = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		string otherName = dr["name_cn"].ToString();
		string brand = dr["brand"].ToString();
		string cat = dr["cat"].ToString();
		string stock = dr["stock"].ToString();
		string code = dr["code"].ToString();
		string barcode = dr["barcode"].ToString();
		double dPrice = double.Parse(dr["level_price0"].ToString());

		Response.Write("<tr");
		if(i%2 != 0)
			Response.Write(" bgcolor=#EEEEEE");
		Response.Write(">");

		Response.Write("<td>" + mpn + "</td>");
		Response.Write("<td>" + barcode + "</td>");
		Response.Write("<td>" + cat + "</td>");
		Response.Write("<td>" + name + "</td>");
		Response.Write("<td align=center>" + dPrice.ToString("c") + "</td>");
		Response.Write("<td>" + stock + "</td>");
		Response.Write("</tr>");
	}
	Response.Write("</td></tr></table></form>\r\n");
	return true;
}
bool PrintCatalogSelection()
{
	int rows = 0;
	string sc = " SELECT DISTINCT cat FROM code_relations ";
	sc += " ORDER BY cat";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		rows = myAdapter.Fill(dst, "cat");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}

	if(rows <= 0)
		return true;

	Response.Write("&nbsp; <b>Select Category :</b> <select name=c");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?c='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write("><option value=''>Show All</option>");
	string s = "";
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["cat"].Rows[i];
		s = dr["cat"].ToString();
		Response.Write("<option value='" + s + "'");
		string lcat = cat;
		if(cat != null)
			lcat = cat.ToLower();
		if(s.ToLower() == lcat)
			Response.Write(" selected");
		Response.Write(">" + s + "</option>");
	}
	Response.Write("</select>\r\n");

	//sub catalog
	sc = "SELECT DISTINCT s_cat FROM code_relations WHERE cat= N'" + cat + "' ";
	sc += " ORDER BY s_cat";
//DEBUG("sc=", sc);
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

	Response.Write("<select name=s");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?c=" + HttpUtility.UrlEncode(cat) + "&r=" + DateTime.Now.ToOADate() + "&s='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write("><option value=''>Show All</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["s_cat"].Rows[i];
		s = dr["s_cat"].ToString();
		Response.Write("<option value='" + s + "'");
		string ls_cat = s_cat;
		if(s_cat != null)
			ls_cat = s_cat.ToLower();
		if(s.ToLower() == ls_cat)
			Response.Write(" selected");
		Response.Write(">" + s + "</option>");
	}
	Response.Write("</select>\r\n");
	
	//ss_cat
	sc = "SELECT DISTINCT ss_cat FROM code_relations WHERE cat= N'" + cat + "' AND s_cat=N'" + s_cat;
	sc += "' ORDER BY ss_cat";
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

	Response.Write("<select name=ss");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?c=" + HttpUtility.UrlEncode(cat) + "&s=" + HttpUtility.UrlEncode(s_cat) + "&r=" + DateTime.Now.ToOADate() + "&ss='+ escape(this.options[this.selectedIndex].value))\"");
	Response.Write("><option value=''>Show All</option>");
	for(int i=0; i<rows; i++)
	{
		DataRow dr = dst.Tables["ss_cat"].Rows[i];
		s = dr["ss_cat"].ToString();
		Response.Write("<option value='" + s + "'");
		string lss_cat = ss_cat;
		if(ss_cat != null)
			lss_cat = ss_cat.ToLower();
		if(s.ToLower() == lss_cat)
			Response.Write(" selected");
		Response.Write(">" + s + "</option>");
	}
	Response.Write("</select>\r\n");
	return true;
}
bool CreatePDFFile()
{
	string st = PrintBrochureLayout();
	if(st == "")
	{
		ErrMsgAdmin("no product found");
		return false;
	}
	string sFN = "temp/catalog_" + DateTime.Now.ToString("dd_MM_yyyy_HHmmss") + ".pdf";
	string sPath = sFN;
	FontFactory.Register("c:\\windows\\fonts\\arial.ttf", "arial");
	StyleSheet style = new StyleSheet();
	style.LoadTagStyle("body", "face", "arial");
	style.LoadTagStyle("body", "encoding", "Identity-H");
	Document document = new Document(PageSize.A4);
//DEBUG("<br><br>st=", st);		
//return false;
	try
	{
//		List<IElement> ae = HTMLWorker.ParseToList(new StringReader(st), style);
		ArrayList ae = HTMLWorker.ParseToList(new StringReader(st), style);
		if(ae.Count <= 0)
		{
			ErrMsgAdmin("no product found");
			return false;
		}
		PdfWriter.GetInstance(document, new FileStream(Server.MapPath(sPath), FileMode.Create));
		document.Open();
		for (int j=0; j<ae.Count; j++)
		{
			document.Add((IElement)ae[j]);
//			iTextSharp.text.pdf.PdfPTable p = (iTextSharp.text.pdf.PdfPTable)ae[j];
			IElement ele = (IElement)ae[j];
			if(ele.GetType().ToString() == "iTextSharp.text.pdf.PdfPTable")
			{
				iTextSharp.text.pdf.PdfPTable p = (iTextSharp.text.pdf.PdfPTable)ae[j];
				if(p.Rows.Count == 0)
					document.NewPage();
			}
		}
	}
	catch(DocumentException de) 
	{
		document.Close();
		Response.Write(de.Message);
		return false;
	}
	catch(IOException ioe) 
	{
		document.Close();
		Response.Write(ioe.Message);
		return false;
	}
	document.Close();
	Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=?t=done&fn=" + sFN + "\">");			
	return true;
}
string PrintBrochureLayout()
{
//	string setStyleSheet = "<STYLE> P {page-break-before: always}</STYLE>";
	string setStyleSheet = "<p style=\"page-break-after:always;\"></p>";
	string sBody = "", sFooter = "", sHeader = "";
	sBody = ReadSitePage("brochure");

	int rows = dst.Tables["product"].Rows.Count;
	if(rows <= 0)
		return "";
	
	string readHeader = GetRowTemplate(ref sBody, "brochure_header");
	sHeader = readHeader;

	if(rows > 0)
	{
		sBody = sBody.Replace("@@CAT", cat);
		sBody = sBody.Replace("@@SCAT", s_cat);
		readHeader = readHeader.Replace("@@CAT", ss_cat);
		readHeader = readHeader.Replace("@@SCAT", dst.Tables["product"].Rows[0]["s_cat"].ToString());    
		sHeader = sHeader.Replace("@@CAT", dst.Tables["product"].Rows[0]["cat"].ToString());
		sHeader = sHeader.Replace("@@SCAT", dst.Tables["product"].Rows[0]["s_cat"].ToString());    
	}
	else
	{
		sBody = sBody.Replace("@@CAT", "");
		sBody = sBody.Replace("@@SCAT", "");
		readHeader = readHeader.Replace("@@CAT", "");
		readHeader = readHeader.Replace("@@SCAT", "");    
		sHeader = sHeader.Replace("@@CAT", "");
		sHeader = sHeader.Replace("@@SCAT", "");  
	}
	string readFooter = GetRowTemplate(ref sBody, "brochure_footer");
	sFooter = readFooter;
	string sTemplate = TemplateParseCommand(sBody);
	string rowitem = GetRowTemplate(ref sTemplate, "row_item");
//DEBUG("rowitem=", rowitem);
	string sTmpSwap = rowitem;
	StringBuilder sbRow = new StringBuilder();

	sbRow.Append(sHeader);
	int nRowsCounter = 0;
	int nNameLen = MyIntParse(GetSiteSettings("broucher_item_name_length", "12"));
	if(nNameLen <= 0)
		nNameLen = 4;
	int m = 0;
	int nRowsAdd = 0;
	for(int i=0; i<rows; i++)
	{			
		DataRow dr = dst.Tables["product"].Rows[i];
//		double dBottomPrice = MyDoubleParse(dr["bottom_price"].ToString());        
//		double dRate = MyDoubleParse(dr["level_rate1"].ToString());
//		double dPrice = dBottomPrice * dRate;
		double dPrice = MyDoubleParse(dr["level_price0"].ToString());
//		if(dPrice <= 0)
//			continue;
//		if(m_sPriceLevel == "price2")
//			dPrice = MyDoubleParse(dr["price2"].ToString());       
//		string sImagePath = GetProductImgSrc(dr["code"].ToString()).ToLower();
		string item_name = dr["name"].ToString();
//		if(item_name.Length > nNameLen)
//			item_name = item_name.Substring(0, nNameLen);
		string supplier_code = dr["supplier_code"].ToString();
		string simg = Server.MapPath("../pi/" + supplier_code + ".jpg");
//		string simg = Server.MapPath("../pi/1.jpg");
		if(!CheckThumbnail(simg))
			continue;
		simg = simg.Replace("\\pi", "\\pi\\t");

//		sImagePath = sImagePath.Replace("pi/", "pi/t/"); 
		sTmpSwap = sTmpSwap.Replace("@@ITEM_CODE", dr["code"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_SUPPLIER_CODE", dr["supplier_code"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_NAME", item_name);
		sTmpSwap = sTmpSwap.Replace("@@ITEM_BARCODE", dr["barcode"].ToString());
		sTmpSwap = sTmpSwap.Replace("@@ITEM_IMAGE_LINK", simg);
		sTmpSwap = sTmpSwap.Replace("@@ITEM_PRICE", dPrice.ToString("c"));
		sTmpSwap = sTmpSwap.Replace("@@ITEM_GST_PRICE", (dPrice * 1.15).ToString("c"));
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_ETA", dr["eta"].ToString());
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_QTY", dr["qty"].ToString());		
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_INNER", dr["inner_pack"].ToString());
//		sTmpSwap = sTmpSwap.Replace("@@ITEM_OUTER", dr["outer_pack"].ToString());

		sbRow.Append(sTmpSwap);
		if(m>0 && ((m+1)%m_nItemsPerRow)== 0 && i<rows-1)
		{
			sbRow.Append("</tr><tr>");
			nRowsCounter++;
			nRowsAdd++;
		}
		if(nRowsCounter > 0 && (nRowsCounter % m_nItemRowsPerPage) == 0)
		{
			sbRow.Append(sFooter);
//			sbRow.Append(setStyleSheet ); //page break
			sbRow.Append("<table></table>"); //no rows in the table, indicate a page break
			if((rows - 1 ) > i)
				sbRow.Append("<P></P>" + sHeader);
			nRowsCounter = 0;
		}
		else if((rows - 1) == i)
		{
//			sbRow.Append(sFooter);
		}
		else
		{
//			sbRow.Append(setStyleSheet );
		}
		sTmpSwap = rowitem;    
		m++;
	}
	if(m <= 0)
		return "";
	
	int nAddon = m_nItemsPerRow - (m - m_nItemsPerRow * nRowsAdd);
	for(int i=0; i<nAddon; i++)
	{
		sbRow.Append("<td>&nbsp;</td>");
	}
		
	sbRow.Append(sFooter);
	if(rows < 0)
	{   
		rowitem = rowitem.Replace("@@ITEM_CODE", "");
		rowitem = rowitem.Replace("@@ITEM_SUPPLIER_CODE", "");
		rowitem = rowitem.Replace("@@ITEM_NAME", "");
		rowitem = rowitem.Replace("@@ITEM_BARCODE", "");
		rowitem = rowitem.Replace("@@ITEM_IMAGE_LINK", "");
		rowitem = rowitem.Replace("@@ITEM_PRICE", "");
		rowitem = rowitem.Replace("@@ITEM_ETA", "");
		rowitem = rowitem.Replace("@@ITEM_QTY", "");
		rowitem = rowitem.Replace("@@ITEM_INNER", "");
		rowitem = rowitem.Replace("@@ITEM_OUTER", "");
		rowitem = rowitem.Replace("@@CAT", "");
		rowitem = rowitem.Replace("@@SCAT", "");
		//sBody = sBody.Replace("@@ITEM_PRICE_WITH_GST", "");

	}	
	sTemplate = sTemplate.Replace("@@template_row_item", sbRow.ToString());
	sTemplate = sTemplate.Replace("@@template_brochure_header", "");
	sTemplate = sTemplate.Replace("@@template_brochure_footer", "");
	return sTemplate; // + sFooter;
}
string TemplateParseCommand(string tp)
{
	StringBuilder sb = new StringBuilder();

	int line = 0;
	string sline = "";
	bool bRead = ReadLine(tp, line, ref sline);
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf("@@SETITEMS") >= 0)
		{
			string snItems = GetDefineValue("items_per_row", sline);
			if(snItems != "")
				m_nItemsPerRow = MyIntParse(snItems);
		}
		else if(sline.IndexOf("@@SETROWS") >= 0)
		{
			string snItemsRows = GetDefineValue("items_rows_per_page", sline);
			if(snItemsRows != "")
				m_nItemRowsPerPage = MyIntParse(snItemsRows);
		}
		else
		{
			sb.Append(sline);
		}
		line++;
		bRead = ReadLine(tp, line, ref sline);
	}
	return sb.ToString();
}
string GetDefineValue(string sDef, string sline)
{
	int p = sline.IndexOf(sDef);
	string sValue = "";
	if(p > 0)
	{
		p += sDef.Length + 1;
		for(; p<sline.Length; p++)
		{
			if(sline[p] == ' ' || sline[p] == '\r' || sline[p] == '\n')
				break;
			sValue += sline[p];
		}
	}
	return sValue;
}
string GetRowTemplate(ref string tp, string sid)
{
	StringBuilder sb = new StringBuilder(); //for return
	StringBuilder sb1 = new StringBuilder();

	string begin = "<!-- BEGIN " + sid + " -->";
	string end = "<!-- END " + sid + " -->";
	int line = 0;
	string sline = "";
	bool bRead = ReadLine(tp, line, ref sline);
	bool bBegan = false;
	int protect = 999;
	while(bRead && protect-- > 0)
	{
		if(sline.IndexOf(begin) >= 0)
		{
			bBegan = true;
			sb1.Append("@@template_" + sid);

			//skip this line
			line++; 
			bRead = ReadLine(tp, line, ref sline);
		}
		if(sline.IndexOf(end) >= 0)
			bBegan = false;
		else if(bBegan)
			sb.Append(sline);
		else
			sb1.Append(sline);
		line++;
		bRead = ReadLine(tp, line, ref sline);
	}
	tp = sb1.ToString(); //replace template with @@template_[sid]
	return sb.ToString().Replace("\n", "").Replace("\r", "");
}
bool ReadLine(string s, int n, ref string sline)
{
	StringBuilder sb = new StringBuilder();
	int lines = 0;
	int i = 0;
	for(i=0; i<s.Length && lines <= n; i++)
	{
		if(s[i] == '\r')
			lines++;
		else if(s[i] == '\n')
			continue;

		if(n == lines)
			sb.Append(s[i]);
		if(lines > n)
			break;
	}
	sline = sb.ToString();

	if(sb.ToString() == "" && i == s.Length)
		return false;
	return true;
}
bool CheckThumbnail(string strPath)
{
	if(!File.Exists(strPath))
		return false;
	string thumbPath = strPath.Replace("\\pi", "\\pi\\t");
	if(File.Exists(thumbPath))
	{
//		return true;
		File.Delete(thumbPath);
	}
	string root = Server.MapPath("../pi");
	FileStream fFile = new FileStream(strPath, FileMode.Open);
	try
	{
		CreateThumbnail(strPath, root, fFile, 320, 320);
	}
	catch(Exception e)
	{
		fFile.Close();
		ShowExp("", e);
		return false;
	}
	fFile.Close();
	return true;
}
bool MakeThumbnail()
{
	string root = Server.MapPath("../pi");
	DirectoryInfo di = new DirectoryInfo(root);
	foreach (FileInfo f in di.GetFiles("*.*")) 
	{
		string file = f.Name.ToLower();
		if(file.IndexOf(".jpg") < 0 && file.IndexOf(".gif") < 0)
			continue;
		string strPath = f.FullName;
		FileStream fFile = new FileStream(strPath, FileMode.Open);
		try
		{
			CreateThumbnail(strPath, root, fFile, 320, 320);
		}
		catch(Exception e)
		{
			fFile.Close();
			ShowExp("", e);
			return false;
		}
		fFile.Close();
	}
	return true;
}
</script>
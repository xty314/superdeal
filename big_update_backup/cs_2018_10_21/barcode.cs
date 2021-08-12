<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Drawing2D" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<%@ Import Namespace="System.Drawing.Text" %>
<%@Import Namespace="ASPNet_Drawing" %>
<!-- #include file="page_index.cs" -->

<script runat=server>

string m_cat = "";
string m_scat = "";
string m_sscat = "";

string m_scode = "";
string m_fcode = "";
string m_command = "";
string m_qty = "1";

string m_code = "";

string m_cols = "1";

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck(""))
		return;
		
	DoQueryStringValue();
	//Response.Write("all done");	
//	if(!getAllProductCode())
//		return;

	if(m_code != "" && TSIsDigit(m_code))
	{
		if(!getAllProductCode())
			return;
		return;
	}
	if(m_command == "Print Barcode")
	{
		if(!getAllProductCode())
			return;
		return;
	}
	InitializeData();
	
    ShowInputForm();  
	
}


bool getAllProductCode()
{
	bool bFixedPrices = false;
	if(MyBooleanParse(GetSiteSettings("use_fixed_level_prices", "0", true)))
		bFixedPrices = true;

	string stype = "";
	if(Request.Form["rtype"] != null && Request.Form["rtype"] != "")
		stype = Request.Form["rtype"].ToString();
	if(stype == "")
		stype = "2";
	int nm_qty = 0;
	if(Request.Form["hid_mqty"] != null && Request.Form["hid_mqty"] != "")
		nm_qty = int.Parse(Request.Form["hid_mqty"].ToString());
	if(Request.QueryString["qty"] != "" && Request.QueryString["qty"] != null)
		nm_qty = int.Parse(Request.QueryString["qty"].ToString());

	string sc = "";
		//sc += "SELECT p.code, p.name, p.price * c.level1_rate AS price FROM product p JOIN stock_qty sq ON sq.code = p.code ";
	sc += "SELECT p.code, CONVERT(varchar(200),p.name) AS name, p.price * c.level_rate1 AS price, c.barcode ";
	if(bFixedPrices)
		sc += ", c.price1 ";
	sc += " FROM product p ";
//	if(g_bRetailVersion)
//			sc += " JOIN stock_qty sq ON sq.code = p.code ";

	sc += " JOIN code_relations c ON c.code = p.code "; 
	sc += "  WHERE 1=1 ";
	if(stype == "1")  // 1 is for catalog only
	{
		if(m_cat != "" && m_cat != "all")
			sc += " AND p.cat = '"+ m_cat +"' ";
		if(m_scat != "" && m_scat != "all")
			sc += " AND p.s_cat = '"+ m_scat +"' ";
		if(m_sscat != "" && m_sscat != "all")
			sc += " AND p.ss_cat = '"+ m_sscat +"' ";
	}
	if(stype == "2")
	{
	//DEBUG("mscode = ", m_scode);
	//DEBUG("mfcode = ", m_fcode);
		if(m_code != "")
			m_scode = m_code;
		if(m_scode != "" && m_fcode != "")
		{
			if(TSIsDigit(m_scode) && TSIsDigit(m_fcode))
				sc += " AND c.code BETWEEN "+ m_scode +" AND "+ m_fcode +" ";
		}
		if(m_scode != "" && m_fcode == "")
		{
			if(TSIsDigit(m_scode))
				sc += " AND p.code = "+ m_scode +" ";
		}
	}
bool bNoCode = false;
	if(stype == "3")
	{
		bool bAND = false;
		string code = "";
		for(int i=1; i<=nm_qty; i++)
		{
			code = Request.Form["m_code"+ i];
			if(code != "")
			{
				if(TSIsDigit(code))
				{
					if(!bAND)
					{
						sc += " AND ";
						bAND = true;
					}
					else
						sc += " OR ";
				
					sc += " p.code ="+ code;
				}
				bNoCode = false;
			}
			else
				bNoCode = true;
		
		}
	}
//DEBUG("sc = ", sc);
//
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		rows = myCommand.Fill(dst,"code");
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
Session["slt_code"] = null;
	//----------user private font to generate code39 barcode-------------------
	//string sFontPath = Server.MapPath("./bar/BARCODE39.TTF");
	string sfontName = GetSiteSettings("barcode_fontname", "barcode39.ttf", true);
	string sFontPath = Server.MapPath("./bar/"+ sfontName +"");
//DEBUG("sfontpath =", sFontPath);
	// Create a private font collection
	PrivateFontCollection pfc = new PrivateFontCollection();

	// Load in the temporary barcode font
	pfc.AddFontFile(""+ sFontPath +"");

	// Select the font family to use
	
	FontFamily usefont = null;
	try
	{
		usefont = new FontFamily("39 tall",pfc);
	}
	catch(Exception e)
	{
		usefont = new FontFamily("code 39",pfc);
	}
	//-------------end of using private font---------------------
	if(bNoCode)
		rows = 0;
	if(rows <= 0)
	{
		Response.Write("<script language=javascript>");
		//Response.Write("window.alert('This Item is not Found in Stock'); window.location=('"+ Request.ServerVariables["URL"] +"');\r\n");
		Response.Write("window.alert('This Item is not Found in Stock'); window.location=;\r\n");
		Response.Write("</script");
		Response.Write(">");
		return false;
	}
	if(rows > 0)
	{
		//---delete all other files first --------
		string path = Server.MapPath("./bar/");
	//DEBUG(" path =", path);
		string[] files = Directory.GetFiles(path,"*.gif");
		int count = files.Length;
		
		for(int i=0; i<count; i++)
		{
			File.Delete(files[i]);
		}
	//---------------end of delete-----
		
		int n_ImgWidth = int.Parse(GetSiteSettings("barcode_width", "650"));
		int n_ImgHeight = int.Parse(GetSiteSettings("barcode_height", "460"));
		string swidth = GetSiteSettings("barcode_width_percent", "203");
		string sheight = GetSiteSettings("barcode_height_percent", "100");
		int nFontSize = 16;
		int nBarcodeFS = 42;
		int nXtra1 = 10, nXtra2 = 20, nXtra3 = 60;
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["code"].Rows[i];
			string code = dr["code"].ToString();
			string barcode = dr["barcode"].ToString();
			string name = dr["name"].ToString();
			string price = dr["price"].ToString();
			if(bFixedPrices)
				price = dr["price1"].ToString();
			double dPrice = Math.Round(MyDoubleParse(price), 2);
			double GSTRate = MyDoubleParse(GetSiteSettings("gst_rate_percent", "12.5")) / 100;
//			DEBUG("gstrate = ", GSTRate.ToString());
			double dGST = dPrice * GSTRate;
			double dPriceWithGST = Math.Round((dPrice + dGST),2);
			
			name = StripHTMLtags(name);
			Bitmap b = new Bitmap(n_ImgWidth,n_ImgHeight,PixelFormat.Format32bppRgb);
			Graphics g = Graphics.FromImage(b);
			SolidBrush sb = new SolidBrush(Color.Black);
			SolidBrush sb2 = new SolidBrush(Color.Orange);
			//Font f = new Font(bfCode39,30);
			//Font myCode39 = new Font(""+ sFontPath +"", 30);
			if(barcode.Length < 7)
				nBarcodeFS = 42;
			Font myCode39 = new Font(usefont, nBarcodeFS);
			
			
			Font f2 = new Font("Verdana",12);
			Font f3 = new Font("Verdana", nFontSize,FontStyle.Bold);			
			Font f4 = new Font("Verdana", 18,FontStyle.Bold);
			
			g.FillRectangle(new SolidBrush(Color.White),0,0,n_ImgWidth,n_ImgHeight);
				
			g.DrawString(""+ name +"",f4,sb, 6 , ((n_ImgHeight/2)/4)+ nXtra1);							
			g.DrawString(""+ code +"", f4,sb,10, ((n_ImgHeight/2)/2)+ nXtra1);
//			g.DrawString("*"+ code +"*",myCode39,sb,(n_ImgWidth/2)-(n_ImgWidth/3),(n_ImgHeight/2)-30);
			//g.DrawString(""+ m_sCompanyName.ToUpper()+ code +"",f2,sb,(n_ImgWidth/2)-(n_ImgWidth/4),(n_ImgHeight/2)+45);
			
			if(GetSiteSettings("barcode_print_pirce", "true", false) == "true")
			{
//				g.DrawString(""+ dPrice.ToString("c") +"(exc GST)",f4,sb,5,(n_ImgHeight/2)+60);
				g.DrawString(""+ dPriceWithGST.ToString("c") +"",f4,sb,(n_ImgWidth/2)-nXtra1, ((n_ImgHeight/2)/2) + nXtra1);
			}
			g.DrawString("*"+ barcode +"*", myCode39, sb, 10,(n_ImgHeight/2)+nXtra2);
			g.DrawString("*"+ barcode +"*", f3, sb, (n_ImgWidth/4)-20, (n_ImgHeight/2)+nXtra3);

			//Response.ContentType = "image/jpeg";
		    //b.Save(Response.OutputStream, ImageFormat.Jpeg);
			//b.Save(Server.MapPath("./bar") +"\\"+ code +".bmp",ImageFormat.Bmp);
			b.Save(Server.MapPath("./bar") +"\\"+ code +".gif",ImageFormat.Gif);
			//b.Save(Server.MapPath("./bar") +"\\"+ code +".jpg",ImageFormat.Jpeg);
			g.Dispose();

			if(!DoPrintBarcodeImage(code, i, rows, swidth, sheight))
				return false;
		}

	}

	return true;

}

bool DoPrintBarcodeImage(string code, int scol, int fcol, string swidth, string sheight)
{
//	DEBUG("mcosl =", m_cols);
	string dest_path = Server.MapPath("./bar");

	DirectoryInfo di = new DirectoryInfo(dest_path);
//DEBUG("sfile = ", dest_path);
//DEBUG(" scol  = ", scol);
//DEBUG(" fcol  = ", fcol);
//	Response.Write("jpg format");
/*	foreach (FileInfo f in di.GetFiles("*.jpg")) 
	{
		string sfile = f.Name.ToString();
		sfile = sfile.Replace(".jpg", "");
//DEBUG("sfile = ", sfile);
		if(code == sfile)
		{
			//string dest_file = dest_path + "\\" + f.Name;
			string dest_file = "./bar/" + f.Name;
			
			Response.Write("&nbsp;&nbsp;<img width=30% height=8% src='"+ dest_file +"'>");
		}
	}
	Response.Write("<br>bit map format");


	foreach (FileInfo f in di.GetFiles("*.bmp")) 
	{
		string sfile = f.Name.ToString();
		sfile = sfile.Replace(".bmp", "");
//DEBUG("sfile = ", sfile);
		if(code == sfile)
		{
			//string dest_file = dest_path + "\\" + f.Name;
			string dest_file = "./bar/" + f.Name;
			
			Response.Write("&nbsp;&nbsp;<img width=30% height=8% src='"+ dest_file +"'>");
		}
		
	}
		
	Response.Write("<br>gif format");
*/
	string tbRowWidth = GetSiteSettings("barcode_table_row_width", ""+swidth +"");
	string tbRowHeight = GetSiteSettings("barcode_table_row_height", ""+ sheight +"");
	Response.Write("<table cellspacing=0 cellpadding=0 border=0 >");
	foreach (FileInfo f in di.GetFiles("*.gif")) 
	{
		string sfile = f.Name.ToString();
		sfile = sfile.Replace(".gif", "");
//DEBUG("sfile = ", sfile);
		int nSpace = int.Parse(GetSiteSettings("barcode_bt_space", "6"));
		int nRowCount = 0;
		for(int i=0; i<int.Parse(m_qty); i++)
		{
			if(code == sfile)
			{
				//string dest_file = dest_path + "\\" + f.Name;
				string dest_file = "./bar/" + f.Name;
				//Response.Write("");
				
				if((i % 3) == 0)
				{
					nRowCount++;
					Response.Write("<tr align=center>");
				}
							
				//Response.Write("<tr><td><img width='"+ swidth +"' height='"+ sheight +"' src='"+ dest_file +"'></td></tr>");
				Response.Write("<td align=center width='"+ tbRowWidth +"' height='"+ tbRowHeight +"'>");
				for(int j=0; j<nSpace; j++)
				{
					Response.Write("&nbsp;");
				}				
				Response.Write("<img border=0 width='"+ swidth +"' height='"+ sheight +"' src='"+ dest_file +"'>");
				Response.Write("</td>");				
				if((i % 3) == 2 && i > 0 )
					Response.Write("</tr>");
								
				
			}
			
		}
	}
	Response.Write("</table>");
	return true;
}

void DoQueryStringValue()
{
	if(Request.QueryString["cat"] != null && Request.QueryString["cat"] != "")
		m_cat = Request.QueryString["cat"].ToString();
	if(Request.QueryString["scat"] != null && Request.QueryString["scat"] != "")
		m_scat = Request.QueryString["scat"].ToString();
	if(Request.QueryString["sscat"] != null && Request.QueryString["sscat"] != "")
		m_sscat = Request.QueryString["sscat"].ToString();
	if(Request.Form["cmd"] != null)
		m_command = Request.Form["cmd"].ToString();
	if(Request.Form["scode"] != null && Request.Form["scode"] != "")
		m_scode = Request.Form["scode"].ToString();
	if(Request.Form["fcode"] != null && Request.Form["fcode"] != "")
		m_fcode = Request.Form["fcode"].ToString();
	if(Request.Form["slt_print"] != null && Request.Form["slt_print"] != "")
		m_cols = Request.Form["slt_print"].ToString();
	if(Request.Form["qty"] != null && Request.Form["qty"] != "")
		m_qty = Request.Form["qty"].ToString();

	if(Request.QueryString["code"] != null && Request.QueryString["code"] != "")
		m_code = Request.QueryString["code"].ToString();

	if(Request.QueryString["qty"] != null && Request.QueryString["qty"] != "")
		m_qty = Request.QueryString["qty"].ToString();
}

bool DoItemOption()
{
	int rows = 0;
	string sc = "SELECT DISTINCT RTRIM(LTRIM(cat)) AS cat FROM product p  ORDER BY RTRIM(LTRIM(cat))";
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
	Response.Write("Catalog Select: <select name=s ");
	Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "");

	Response.Write("&cat='+this.options[this.selectedIndex].value)\"");
	Response.Write(">");
//	Response.Write("<option value='all'>Show All</option>");

//DEBUG("mcat = ", m_cat);
	string cat_scode = "";
	string cat_fcode = "";
	string scat_scode = "";
	string scat_fcode = "";
	string sscat_scode = "";
	string sscat_fcode = "";
		
	for(int i=0; i<rows; i++)
	{
		
		DataRow dr = dst.Tables["cat"].Rows[i];
		string s = dr["cat"].ToString();
		
		if(m_cat.ToUpper() == s.ToUpper())
			Response.Write("<option value='"+s+"' selected>" +s+ "");
		else
			Response.Write("<option value='"+s+"'>" +s+ "");

	}

	Response.Write("</select>");
	
	if(m_cat != null && m_cat != "" && m_cat != "all")
	{
		sc = "SELECT DISTINCT RTRIM(LTRIM(s_cat)) AS s_cat FROM product  WHERE cat = '"+ m_cat +"' ";
		sc += " ORDER BY RTRIM(LTRIM(s_cat))";
//DEBUG("sc = ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "s_cat");
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&");

		Response.Write("cat="+ m_cat +"&scat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");
		Response.Write("<option value='all'>Show All</option>");

		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["s_cat"].Rows[i];
			string s = dr["s_cat"].ToString();
			//DEBUG(" s = ", s);
//DEBUG(" scat = ", s_cat);
			if(m_scat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>" +s+ "");
			else
				Response.Write("<option value='"+s+"'>" +s+ "");
		}

		Response.Write("</select>");
	}
	if(m_scat != null && m_scat != ""  && m_scat != "all")
	{
		sc = "SELECT DISTINCT RTRIM(LTRIM(ss_cat)) AS ss_cat FROM product p WHERE cat = '"+ m_cat +"' ";
		sc += " AND s_cat = '"+ m_scat +"' ";
		sc += " ORDER BY RTRIM(LTRIM(ss_cat)) ";
//DEBUG("sc = ", sc);
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			rows = myAdapter.Fill(dst, "ss_cat");
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
		Response.Write(" onchange=\"window.location=('" + Request.ServerVariables["URL"] + "?r=" + DateTime.Now.ToOADate() + "&");

		Response.Write("cat="+ m_cat+"&scat="+ m_scat +"&sscat='+this.options[this.selectedIndex].value)\"");
		Response.Write(">");

		Response.Write("<option value='all'>Show All</option>");
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["ss_cat"].Rows[i];
			string s = dr["ss_cat"].ToString();
			
			if(m_sscat.ToUpper() == s.ToUpper())
				Response.Write("<option value='"+s+"' selected>"+s+"");
			else
				Response.Write("<option value='"+s+"'>"+s+"");
		}

		Response.Write("</select>");
	}
	return true;
}

void ShowInputForm()
{
	string uri = ""+ Request.ServerVariables["URL"] +"?";
	Response.Write("<br><form name=frm method=post>");
	Response.Write("<center><h4>DISPLAY QUERY BARCODE</center></h4>");
	Response.Write("<table align=center width= valign=center cellspacing=2 cellpadding=4 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	
	string scode = "";
	if(Session["slt_code"] != null && Session["slt_code"] != "")
		m_scode = Session["slt_code"].ToString();

	Response.Write("<td colspan=1>");
	Response.Write(" <input type=radio name=rtype value=1 checked></td><td colspan=1>");
	DoItemOption();
	//Response.Write(" &nbsp;&nbsp;Select Columns: <select name=slt_print><option value=1>One Col</option><option value=2>Two Cols</option><option value=3>Three Cols</option></select>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>");
//	Response.Write(" Sequence Product Code: <input type=checkbox name=sequence >");
	Response.Write(" <input type=radio name=rtype value=2 >");
	Response.Write("</td><td>Start Product Code: &nbsp;<input type=text name=scode value="+ m_scode +"> ");
	Response.Write(" <a title='get product code, supplier code ...' href='slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"' class=o>Find</a>");

	Response.Write(" &nbsp;&nbsp;<br>Finish Product Code: <input type=text name=fcode value="+ m_fcode +">");
//	Response.Write(" <a title='get product code, supplier code ...' href='slt_item.aspx?uri="+ HttpUtility.UrlEncode(uri) +"' class=o>Find</a> &nbsp;&nbsp;&nbsp;");
	
	int nQty = int.Parse(GetSiteSettings("barcode_qty", "30"));
//	Response.Write("&nbsp;|&nbsp; SELECT QTY: <select name=qty>");
//	for(int i=1; i<=nQty; i++)
//		Response.Write("<option value="+ i +">"+ i +"</option>");
//	Response.Write("</select> &nbsp;&nbsp;" );
//	Response.Write("<input type=submit name=cmd value='Print Barcode' "+ Session["button_style"] +"></td>");
	Response.Write("</tr>");
	
	Response.Write("<tr><td colspan=2><b>SELECT MULTIPLE PRODUCT CODE: ");
	int n_mqty = 1;
	if(Request.QueryString["mq"] != null && Request.QueryString["mq"] != "")
		n_mqty = int.Parse(Request.QueryString["mq"].ToString());
	Response.Write("<input type=hidden name=hid_mqty value="+ n_mqty +">");
	Response.Write("&nbsp;&nbsp; <select name=m_qty onchange=\"window.location=('"+ Request.ServerVariables["URL"] +"?mq='+ this.options[this.selectedIndex].value ) \">");
	for(int i=1; i<=nQty; i++)
	{
		Response.Write("<option value="+ i +" ");
		if(n_mqty == i)
			Response.Write(" selected ");
		Response.Write(">"+ i +"</option>");
	}
	Response.Write("</select> &nbsp;&nbsp;" );
	Response.Write("</td></tr>");
	Response.Write("<tr><td><input type=radio name=rtype value=3 checked> ");
//	Response.Write("<tr><td>Multi Product Code: <input type=checkbox name=multi > ");
	Response.Write("</td><td> ");
	for(int i=1; i<=n_mqty; i++)
		Response.Write("PRODUCT CODE "+ i +" : &nbsp;<input type=text name=m_code"+ i +"><br>");
	Response.Write("</td></tr>");
	Response.Write("</td></tr>");

	Response.Write("<tr><td align=center colspan=2>");
	Response.Write("&nbsp;&nbsp; <b>SELECT PRINTOUT QTY: <select name=qty>");
	for(int i=1; i<=nQty; i++)
		Response.Write("<option value="+ i +">"+ i +"</option>");
	Response.Write("</select> &nbsp;&nbsp;" );
	Response.Write("<input type=submit name=cmd value='Print Barcode' "+ Session["button_style"] +"></td>");
	Response.Write("</tr>");

	Response.Write("</table>");
	Response.Write("</form><br>");
	

}

</script>


<asp:Label id=LFooter runat=server/>
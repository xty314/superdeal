<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
DataRow[] dracr;	//for sorting code_relations
//string m_type = "";
string m_action = "";
string m_code = "";
string m_highlight = "";
string m_spec = "";
string m_manufacture = "";
string m_pic = "";
string m_rev = "";
string m_warranty = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("editor"))
		return;

	GetQueryStrings();
	PrintAdminHeader();
	PrintAdminMenu();

	if(m_action == "save")
	{
		if(DoSave())
		{
			TSRemoveCache(m_sCompanyName + "_" + m_sHeaderCacheName);

			string s = "<center><h2>done...</h2>";//<br><a href=editp.aspx?t=new class=d>Add another one</a>";
			s += "<h4><br><a href='liveedit.aspx?code="+ m_code +"' class=o> << Back to Edit Product Page</a>";
			s += "<h4><br><a href='ep.aspx?code="+ m_code +"' class=o> << Back to Edit Product Specification Page</a>";
			s += "<br>";
		/*	s += "<a href= ";
			if(Session["LastPage"] != null)
			{
				s += Session["LastPage"];
				s += ">Go back where you came from: ";
				s += Session["LastPage"];
				s += "</a>";
			}
			else
			{
				s += "default.aspx";
				s += ">Home</a>";
			}
			*/
			Response.Write(s);
		}
		return;
	}

	if(!GetOldData())
		return;
	PrintForm();
	PrintAdminFooter();
}

Boolean GetOldData()
{
	string sc = "SELECT * FROM product_details WHERE code=";
	sc += m_code;
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(dst, "ret") <= 0)
		{
			return InsertNew();
		}
		else
		{
			m_highlight = dst.Tables["ret"].Rows[0]["highlight"].ToString();
			m_highlight = m_highlight;
			m_spec = dst.Tables["ret"].Rows[0]["spec"].ToString();
			m_spec = m_spec;
			m_manufacture = dst.Tables["ret"].Rows[0]["manufacture"].ToString();
			m_pic = dst.Tables["ret"].Rows[0]["pic"].ToString();
			m_rev = dst.Tables["ret"].Rows[0]["rev"].ToString();
			m_warranty = dst.Tables["ret"].Rows[0]["warranty"].ToString();
			m_warranty = m_warranty;
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	
	return true;
}

Boolean InsertNew()
{
	string sc = "INSERT INTO product_details (code, highlight, manufacture, spec, pic, rev, warranty) VALUES(";
	sc += m_code;
	sc += ", '', '', '', '', '', '')";

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

void GetQueryStrings()
{
//	m_type = Request.QueryString["t"];
	m_action = Request.QueryString["a"];
	m_code = Request.QueryString["code"];
}

void PrintForm()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<img src=r.gif><a href=liveedit.aspx?code=" + m_code + " class=x>Edit Product</a> ");
	sb.Append(" <img src=r.gif><a href=ep.aspx?code=" + m_code + " class=x>Edit Specifications</a> ");
	sb.Append(" <img src=r.gif><a href=addpic.aspx?code=" + m_code + " class=x>Edit Photo</a> ");

	sb.Append("<form action=ep.aspx?a=save&code=" + m_code);
//	sb.Append(m_type);
	sb.Append(" method=POST>");
	sb.Append("<table cellpadding=3>");
	sb.Append("<tr><td colspan=2><font size=+1><b>");
	sb.Append("Product Details of ");
	sb.Append(m_code);
	sb.Append("</b></font>&nbsp;&nbsp;&nbsp;<a href=addpic.aspx?code=" + m_code + "><font size=+1><b>Edit Photo</b></font></a></td></tr>");
//	sb.Append("<tr><td><b>Photo</b> (link)</td></tr>");
//	sb.Append("<tr><td><textarea name=pic cols=90 rows=1>");
//	sb.Append(m_pic);
//	sb.Append("</textarea></td></tr>");

	sb.Append("<tr><td><b>High Light</b> (text/html)</td></tr>");
	sb.Append("<tr><td><textarea name=highlight cols=90 rows=7>");
	sb.Append(m_highlight);
	sb.Append("</textarea></td></tr>");

	sb.Append("<tr><td><b>Manufacturer's Website</b></td></tr>");
	sb.Append("<tr><td><textarea name=manufacture cols=90 rows=1>");
	sb.Append(m_manufacture);
	sb.Append("</textarea></td></tr>");

	sb.Append("<tr><td><b>Spec</b> (text/html)</td></tr>");
	sb.Append("<tr><td><textarea name=spec cols=90 rows=7>");
	sb.Append(m_spec);
	sb.Append("</textarea></td></tr>");

	sb.Append("<tr><td><b>Reviews</b> (text/html)</td></tr>");
	sb.Append("<tr><td><textarea name=rev cols=90 rows=7>");
	sb.Append(m_rev);
	sb.Append("</textarea></td></tr>");

	sb.Append("<tr><td><b>Warranty</b> (text/html)</td></tr>");
	sb.Append("<tr><td><textarea name=warranty cols=90 rows=3>");
	sb.Append(m_warranty);
	sb.Append("</textarea></td></tr>");

	sb.Append("<input type=hidden name=code value=");
	sb.Append(m_code);
	sb.Append(">");
	sb.Append("<tr><td align=right><input type=submit value=' &nbsp; Save &nbsp; ' "+ Session["button_style"] +">");
	//sb.Append("<input type=button value=' Back to Edit Product Page ' onclick=\"window.location=('liveedit.aspx?code="+ m_code +"');\" "+ Session["button_style"] +">");
	sb.Append("</td></tr></table></form>");
	Response.Write(sb.ToString());
}

Boolean DoSave()
{
	if(Request.Form["code"] == null || Request.Form["code"] == "")
	{
		Response.Write("<h3>No Item Code Provided, Cannot Update</h3>");
		return false;
	}
	string shl = EncodeQuote(Request.Form["highlight"]);
	string sman = EncodeQuote(Request.Form["manufacture"]);
	string sspec = EncodeQuote(Request.Form["spec"]);
	string srev = EncodeQuote(Request.Form["rev"]);
	string swarr = EncodeQuote(Request.Form["warranty"]);

	if(srev.Length >= 1024)
		srev = srev.Substring(0, 1023);
/*
	int len = sman.Length + srev.Length + swarr.Length;
	int lenLeft = 8000 - len;
	if(shl.Length > lenLeft)
	{
		if(lenLeft > 0)
			shl = shl.Substring(0, lenLeft);
		else
		{
			Response.Write("<h3>Too many reviews, cannot store that much</h3>");
			return false;
		}
	}
	len += shl.Length;
	lenLeft = 8000 - len;
	if(sspec.Length > lenLeft)
	{
		if(lenLeft > 0)
		{
			Response.Write("<h3>spec's too long, tail's truncated, check out.</h3>");
			sspec = sspec.Substring(0, lenLeft);
		}
		else
		{
			Response.Write("<h3>Too many highlights, cannot store that much</h3>");
			return false;
		}
	}
*/
	string sc = "UPDATE product_details SET highlight=N'";
	sc += shl;
	sc += "', manufacture=N'";
	sc += sman;
	sc += "', spec=N'";
	sc += sspec;
	sc += "', rev=N'";
	sc += srev;
	sc += "', warranty=N'";
	sc += swarr;
	sc += "' WHERE code=";
	sc += Request.Form["code"];
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

void WriteHeaders()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("<html><style type=\"text/css\">td{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}");
	sb.Append("body{FONT-WEIGHT:300;FONT-SIZE:8PT;FONT-FAMILY:verdana;}</style>");
	sb.Append("<body marginwidth=0 marginheight=0 topmargin=0 leftmargin=0>\r\n");

	Response.Write(sb.ToString());
	Response.Flush();
}

void WriteFooter()
{
	StringBuilder sb = new StringBuilder();
	sb.Append("</body></html>");
	Response.Write(sb.ToString());
}
</script>

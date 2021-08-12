<script runat=server>

DataSet dst = new DataSet();	//for creating Temp talbes templated on an existing sql table
string m_uri = "";
string m_ncardID = "0";
string m_customerName = "";

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("sales"))
		return;
	if(Request.QueryString["uri"] != null)
	{
		m_uri = Request.QueryString["uri"];
		Session["returnURI"] = m_uri;
	}
	
	if(Request.QueryString["luri"] != null && Request.QueryString["luri"] != "")
	{
		Session["last_uri_card"] = Request.QueryString["luri"];
//DEBUG("lasr uri =", Session["last_uri_card"].ToString());
	}
//	InitializeData();
	if(Request.Form["cmd"] == "Add Customer")
	{
		if(AddNewEZCard())
		{
			vReloadPage();
			return;
		}
//		Response.Write("<script language=javascript>window.close();</script");
//		Response.Write(">");
	}
	vEZCardForm();

}

void vReloadPage()
{
/*	string strjscript = "<script language=javascript>";
//    strjscript += "window.opener." + HttpContext.Current.Request.QueryString["formname"];
  //  strjscript += ".value = '" + m_ncardID + "';";
	//strjscript += " window.opener.top.location.href="+ Session["last_uri_card"].ToString();
//	strjscript += " var vLinkURL = "+ Session["last_uri_card"].ToString();
//	strjscript += " var vLinkURL = http://help.com ";
//	strjscript += " window.opener.top.location.href=vLinkURL; ";
	strjscript += " window.opener.parent.location.herf='http://www.edenonline.co.nz'; ";
	strjscript += " window.location.href='http://www.edenonline.co.nz'; return false; ";
//	strjscript += "window.opener.focus(); ";
	strjscript += "window.close();";
    strjscript += "</script" + ">"; 
	Response.Write(strjscript);
*/
	string squerystringADD = "";
	if(Session["last_uri_card"] != null)
	{
		squerystringADD = Session["last_uri_card"].ToString();
		if(Session["last_uri_card"].ToString().IndexOf("pos.aspx") >=0 || Session["last_uri_card"].ToString().IndexOf("pos_retail.aspx") >=0 || Session["last_uri_card"].ToString().IndexOf("support") >=0 || Session["last_uri_card"].ToString().IndexOf("q.aspx") >=0)
			squerystringADD += "&ci="+ m_ncardID;	
	}
	Response.Write("<body onload=\"javascript:window.opener.top.location.href='"+ squerystringADD +"'\">");
	Response.Write("<script language=javascript");
	Response.Write(">\r\n");
	Response.Write(" window.close(); \r\n");
	Response.Write("</script");
	Response.Write(">");
	
	//clean up last uri session
	Session["last_uri_card"] = null;
	
}
void vEZCardForm()
{

	vJVScript();
	Response.Write("<BODY bgcolor='#FFFFFF' onload=\"frameFit();\" topmargin='0' marginheight='0' leftmargin='0' marginwidth='0'>");
	Response.Write("<center><form name=frm method=post >");
//	Response.Write("<br><center><h4>ADD NEW CUSTOMER");
	string keyEnter = "OnKeyDown=\"if(event.keyCode==13) event.keyCode=9;\"";		
	//Response.Write("<br><br><table background='/i/top.gif' cellspacing=2 cellpadding=2 >");
	Response.Write("<br><br><table aling=center cellspacing=0 cellpadding=4 border=0 bordercolor=#CCCCCC bgcolor=#EEEEE");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr align=center><td colspan=2><h4>ADD NEW CUSTOMER</td></tr>");
	Response.Write("<tr><td>Name:</td>");
	Response.Write("<td><input type=text name=name size=33% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>Company:</td>");
	Response.Write("<td><input type=text name=company size=33% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>Address:</td>");
	Response.Write("<td><input type=text name=address1 size=35% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>&nbsp;</td>");
	Response.Write("<td><input type=text name=address2 size=35% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>&nbsp;</td>");
	Response.Write("<td><input type=text name=address3 size=35% "+ keyEnter +"></td>");
	Response.Write("</tr>");
//	Response.Write("<tr><td>City:</td>");
	Response.Write("<td>");	
	string scountry = "New Zealand";
	if(DoGetCountry(scountry))
	{
		Response.Write("<select name=city "+ keyEnter +">");
		DoGetCountry(scountry);
	//	Response.Write(countyName);
		//Response.Write("<input type=text name=city></td>");
		Response.Write("</select>");
	}
/*	else
	{
		Response.Write("<input type=text name=city value='Auckland' "+ keyEnter +">");
	}
*/
	Response.Write("<input type=hidden name=city value=''>");
	Response.Write("</tr>");
	Response.Write("<tr><td>Country:</td>");
	Response.Write("<td><input type=text name=country readonly value='New Zealand' "+ keyEnter +"></td>");
	Response.Write("</tr>");
	
	Response.Write("<tr><td>Phone:</td>");
	Response.Write("<td><input type=text name=phone size=20% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>Fasimile:</td>");
	Response.Write("<td><input type=text name=fax size=20% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr><td>Email:</td>");
	Response.Write("<td><input type=text name=email size=40% "+ keyEnter +"></td>");
	Response.Write("</tr>");
	Response.Write("<tr align=center><td colspan=2><input type=submit name=cmd value='Add Customer' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\"if(document.frm.name.value==''){ window.alert('Please Enter a Customer Name:');document.frm.name.focus();return false;}\">");
	Response.Write("<input type=button value='Close' "+ Session["button_style"] +" ");
	Response.Write(" onclick=\"window.close();\"></td></tr>");
	Response.Write("</table></form>");

}

void vJVScript()
{

	Response.Write("<script language='javascript'");
	Response.Write(">");
/*	Response.Write("var temp=self.location.href.split('?'); ");
	Response.Write("var picUrl = (temp.length>1)?temp[1]:'';  ");
	*/
	Response.Write("var NS = (navigator.appName=='Netscape')?true:false; ");

	Response.Write("function frameFit() { ");
	Response.Write("iWidth = (NS)?window.innerWidth:document.body.clientWidth; ");
	Response.Write("iHeight = (NS)?window.innerHeight:document.body.clientHeight; ");
	//Response.Write("iWidth = document.images[0].width - iWidth; ");
	//Response.Write("iHeight = document.images[0].height - iHeight; ");
	
	Response.Write("iWidth = 380 - iWidth; ");
	Response.Write("iHeight = 470 - iHeight; ");
	Response.Write(" window.resizeBy(iWidth, iHeight-1); ");
	Response.Write(" self.focus();");
	Response.Write("};");
	Response.Write("</script");
	Response.Write(">");


}


bool AddNewEZCard()
{
	string name = Request.Form["name"];
	string phone = Request.Form["phone"];
	string fax = Request.Form["fax"];
	string email = Request.Form["email"];
	string address1 = Request.Form["address1"];
	string address2 = Request.Form["address2"];
	string address3 = Request.Form["address3"];
	string city = Request.Form["city"];
	string country = Request.Form["country"];
	string company = Request.Form["company"];
	string sc = "";
//	if(email == "")
//		email = name;
	int rows = 0;
	if(email != "")
	{
		sc = " SELECT id, name, email FROM card WHERE email = '"+ email +"' ";
//	DEBUG("sc = ", sc);
		try
		{
			SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
			rows = myCommand.Fill(dst, "foundID");
			if(rows > 0)
			{
				m_ncardID = dst.Tables["foundID"].Rows[0]["id"].ToString();
//			DEBUG("mncard =", m_ncardID);
				Session["slt_customer"] = dst.Tables["foundID"].Rows[0]["id"].ToString();
				Session["slt_name"] = dst.Tables["foundID"].Rows[0]["name"].ToString();
				Response.Write("<script language=javascript>");
				Response.Write(" window.alert('Existing Customer...This Customer Will be Selected!!!'); ");
				Response.Write("</script");
				Response.Write(">");
				return true;
			}
		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}	
	}
	if(rows == 0)
	{
	sc = " BEGIN TRANSACTION ";
	sc += "INSERT INTO card (company, type, name, phone, fax, email, address1, address2,address3, city, country ";
	sc += " , contact, address1B, address2B, cityB, countryB, postal1, postal2, postal3 ";
	if(Session["branch_support"] != null)
		sc += " , our_branch ";
	sc += ") ";
	sc += " SELECT top 1 '"+ EncodeQuote(company) +"', '1', '"+ EncodeQuote(name) +"', '" + EncodeQuote(phone) +"', '"+ EncodeQuote(fax) +"' ";
	if(email == "")
		sc += " , id + 1 "; 
	else
		sc += " , '"+ EncodeQuote(email) +"'";
	sc += " , '"+ EncodeQuote(address1) +"' ";
	sc += " , '" + EncodeQuote(address2) +"', '" + EncodeQuote(address3) +"', '" + EncodeQuote(city) +"', '" + EncodeQuote(country) +"' ";
	sc += " , '"+ EncodeQuote(name) +"' ";
	sc += " , '" + EncodeQuote(address1) +"', '" + EncodeQuote(address2) +"', '" + EncodeQuote(address3) +"', '" + EncodeQuote(country) +"' ";
	sc += " , '"+ EncodeQuote(address1) +"' ";
	sc += " , '" + EncodeQuote(address2) +"', '" + EncodeQuote(address3) +"' ";
	if(Session["branch_support"] != null)
		sc += " , "+ Session["branch_id"].ToString() +" ";
	sc += " FROM card order by id desc  ";
	sc += " SELECT IDENT_CURRENT('card') AS id ";
	sc += " COMMIT ";
//DEBUG("sc = ", sc);

	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		if(myCommand.Fill(dst, "card") != 1)
		{
			Response.Write("<br><br><center><h3>Error getting card id IDENT");
			return false;
		}
		m_ncardID = dst.Tables["card"].Rows[0]["id"].ToString();
		sc = " SELECT * FROM card WHERE id = "+ m_ncardID;
		try
		{
			myCommand = new SqlDataAdapter(sc, myConnection);
			if(myCommand.Fill(dst, "cardID")==1)
			{
				m_ncardID = dst.Tables["cardID"].Rows[0]["id"].ToString();
			//	m_customerName = dst.Tables["cardID"].Rows[0]["name"].ToString();
				Session["slt_customer"] = dst.Tables["cardID"].Rows[0]["id"].ToString();
				Session["slt_name"] = dst.Tables["cardID"].Rows[0]["name"].ToString();
			}

		}
		catch(Exception e) 
		{
			ShowExp(sc, e);
			return false;
		}	

	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	}
	return true;
}


</script>

<asp:Label id=LFooter runat=server/>
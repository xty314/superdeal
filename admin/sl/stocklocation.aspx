<!-- #include file="../config.cs" -->
<!-- #include file="/cs/page_index.cs" -->
<!DOCTYPE HTML>
<html>
<head>
<meta name="mobile-web-app-capable" content="yes">
<title>Stock Location Search</title>


<!-- Custom Theme files -->
<link href="css/style.css" rel="stylesheet" type="text/css" media="all"/>
<link rel="stylesheet" href="css/index-dark.css">
<!-- Custom Theme files -->
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" /> 
<meta name="keywords" content="Flat Search Box Responsive, Login form web template, Sign up Web Templates, Flat Web Templates, Login signup Responsive web template, Smartphone Compatible web template, free webdesigns for Nokia, Samsung, LG, SonyErricsson, Motorola web design" />
<!--Google Fonts-->
<link href='http://fonts.googleapis.com/css?family=Open+Sans:300italic,400italic,600italic,700italic,800italic,400,300,600,700,800' rel='stylesheet' type='text/css'>
<!--Google Fonts-->
</head>
<body>
<!--search start here-->
	   <%  PrintMainForm();%>
<!--search end here-->	
<div id="main"></div>
<div class="copyright">
	 <p>2020 &copy Stock Location Search rights reserved  by  <a >  Gpos Ltd </a></p>
</div>
<script src="js/index.js"></script> 
	<script>
	
		const keyboard = new aKeyboard.keyboard({
            el: '#main',
            style: {},
            fixedBottomCenter: true
        })
			keyboard.inputOn('#searchTB', 'value');
		     keyboard.onclick('Enter', function() {
             document.getElementById("searchBtn").click();
        })
		function searchTBfocus(){
    		keyboard.inputOn('#searchTB', 'value');
			     keyboard.onclick('Enter', function() {
             document.getElementById("searchBtn").click();
        })
		}
		function stockTBfocus(){
			
    		keyboard.inputOn('#stockTB', 'value');
			     keyboard.onclick('Enter', function() {
             document.getElementById("saveBtn").click();
        })
		}
	</script>
</body>
</html>














<script runat=server>
DataSet ds = new DataSet();
string m_type = "";	//query type &t=
string m_action = "";	//query action &a=
string m_cmd = "";		//post button value, name=cmd
string m_kw = "";
string prompt="Processing....";
void Page_Load(Object Src, EventArgs E ) 
{
	m_bCheckLogin = false;
	m_bDealerArea = false;
	TS_PageLoad(); //do common things, LogVisit etc...   
	m_type = g("t");
	m_action = g("a");
	m_kw = p("kw");

	if(m_kw == "")
		prompt="Please type barcode into the above textbox.";
		m_kw = g("kw");
	m_cmd = p("cmd");
	
	if(m_cmd == "Save")
	{
		if(DoUpdateData())
		{
			// PrintAdminHeader();
			Response.Write("<br><br><br><center><h3>Stock Location saved, please wait a moment.<h3></center>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?\">");
		}
		return; //if it's a form post then do nothing else, quit here
	}
	if(m_cmd == "SCAN")
	{
		Response.Write("<meta http-equiv=\"refresh\" content=\"1; URL=?kw="+p("kw")+"\">");
		return; 
	}

}

bool PrintMainForm()
{
	<%-- Response.Write("<center><h4>Stock Location</h4></center>"); --%>
	Response.Write("<form name=f action=? method=post><div class='search'><div class='s-bar'>");
	<%-- Response.Write("<input type=text value='" + m_kw + "' name=kw class=b onfocus=\"this.value = '';\" onblur=\"if (this.value == '') {this.value = 'Please scan barcode';}\" onkeydown=\"if(event.key=='Enter' || window.event.keyCode == 13){window.location='?kw='+this.value;return false;}\"  onchange=\"window.location='?kw='+this.value;\">"); --%>
	Response.Write("<input type=text value='" + m_kw + "' name=kw onfocus=searchTBfocus()  id=searchTB class=b autocomplete='off' placeholder='Pleasc scan barcode'  onkeydown=\"if(event.key=='Enter' || window.event.keyCode == 13){window.location='?kw='+this.value;return false;}\"  onchange=\"window.location='?kw='+this.value;\">");
	Response.Write("<input type=submit name=cmd class=b value='SCAN' id=searchBtn></div></div>");
	Response.Write("<div class='result-table'>");

	int nRows = 0;
	string sc = " SELECT c.code, c.supplier_code, c.name, c.level_price0 AS price, c.stock_location,s.qty ";
	sc += " FROM code_relations c ";
	sc += "LEFT OUTER JOIN stock_qty s ON s.code=c.code";
	sc += " WHERE 1 = 1 ";
	if (m_kw == ""){
		sc += " AND 1 = 2 ";

	}
	else{
		sc += " AND (c.barcode = '" + EncodeQuote(m_kw) + "' OR c.code = (SELECT TOP 1 item_code FROM barcode WHERE barcode = '" + EncodeQuote(m_kw) + "') ";
		sc += " OR c.supplier_code ='"+EncodeQuote(m_kw)+"' )";

	}
	sc += " ORDER BY c.code ";

	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		nRows = myAdapter.Fill(ds, "data");
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	if (nRows > 0)
	{
		DataRow dr = ds.Tables["data"].Rows[0];
		string code = dr["code"].ToString();
		string qty=dr["qty"].ToString();
		string supplier_code = dr["supplier_code"].ToString();
		string name = dr["name"].ToString();
		double dPrice = MyDoubleParse(dr["price"].ToString());
		string stock_location = dr["stock_location"].ToString();
		Response.Write("<input type=hidden name=code value='" + code + "'><tr>");
		Response.Write("<div class='container'>");
		Response.Write("<div class='images' style='background:url(/pi/"+supplier_code+".jpg)no-repeat; background-size:100% 100%;'>");
		<%-- Response.Write("<img src='/pi/"+supplier_code+".jpg' />"); --%>
		Response.Write("</div>");
		Response.Write("<div class='product'>");
		Response.Write("<h1>Name:"+name+"</h1>");
		Response.Write("<h1>Code:"+code+"</h1>");
		Response.Write("<h1>Supplier Code:"+supplier_code+"</h1>");
		Response.Write("<h1>Price:"+dPrice.ToString("c")+"</h1>");
		Response.Write("<h1>Stock qty:"+qty+"</h1>");
		Response.Write("<h1>Stock Location</h1>");
		Response.Write("<div class='buttons'>");
		Response.Write("<input type='text' name=stock_location id='stockTB' onfocus=stockTBfocus()  autocomplete='off' value="+ stock_location+">");
		Response.Write("<input type=submit name=cmd id=saveBtn class='add' value='Save'>");
		Response.Write("</div>");
		Response.Write("</div>");
		Response.Write("</div>");

	}
	else
	{
		if(g("kw")!=""){
			prompt="Can not found product #"+g("kw");
		}
		Response.Write("<div class='container'>");
		Response.Write("<div class='prompt'>");
		Response.Write(prompt);
		Response.Write("</div>");
		Response.Write("</div>");
	}
	Response.Write("</div></form><script>document.f.kw.select();</script");
		Response.Write(">");
	return true;
}
bool DoUpdateData()
{
	string code = p("code");
	string stock_location = p("stock_location");
	
	if(code == "" || code == "0")
	{
		ErrMsgAdmin("Invalid Code");
		return false;
	}
	
	string sc = "";
	sc += " UPDATE code_relations SET stock_location = N'" + EncodeQuote(stock_location) + "' ";
	sc += " WHERE code = " + code;
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


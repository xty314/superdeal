<script runat=server>

DataSet ds = new DataSet();
int rows = 0;

protected void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	RememberLastPage();

	string cmd = Request.Form["cmd"];
	if(cmd != null)
	{
		if(!TS_UserLoggedIn())
			Response.Redirect("login.aspx");
	}
	if(cmd == "Record")
	{
		if(!AddReferer())
			return;
	}
	PrintHeaderAndMenu();

	LFooter.Text = m_sFooter;
}

bool AddReferer()
{
	string email = Request.Form["semail"];
	Trim(ref email);
	bool bInUse = false;
	if(email == "")
	{
		PrintHeaderAndMenu();
		Response.Write("<br><br><center><h3>Error, email address can not be blank</h3>");
		return false;
	}
	string sc = "SELECT COUNT(*) FROM card c CROSS JOIN referer_secret r WHERE c.email='" + email + "' OR r.email='" + email + "'";
//DEBUG("sc=", sc);
	try
	{
		SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
		myCommand.Fill(ds, "ref");
		if(ds.Tables["ref"].Rows[0][0].ToString() != "0")
			bInUse = true;
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	if(bInUse)
	{
		PrintHeaderAndMenu();
		Response.Write("<br><br><center><h3>Sorry, this email is in use");
		BindGrid();
		return false;
	}

	sc = "INSERT INTO referer_secret (card_id, email) VALUES(" + Session["card_id"].ToString() + ", '" + email + "')";
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
	Response.Write("<br><br><center><h3>Referer Email Saved.</h3>");
	Response.Write("<input type=button onclick=history.go(-1) value=' << Back '>");
	Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=ds.aspx\">");
	return true;
}

void BindGrid()
{
	DataView source = new DataView(ds.Tables[0]);
	MyDataGrid.DataSource = source ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Page(object sender, DataGridPageChangedEventArgs e) 
{
	MyDataGrid.CurrentPageIndex = e.NewPageIndex;
	BindGrid();
}

</script>

<form runat=server>
<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=true
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=#EEEEEE
	CellPadding=1
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	style=fixed
	HorizontalAlign=center
	AllowPaging=True
	PageSize=20
	PagerStyle-PageButtonCount=10
	PagerStyle-Mode=NumericPages
	PagerStyle-HorizontalAlign=Left
    OnPageIndexChanged=MyDataGrid_Page
	>

	<HeaderStyle BackColor=#666696 ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=#EEEEEE/>
    
</asp:DataGrid>
</form>



<br><center><h3>Affiliate & Referer</h3>
<br>

<table width=70%><tr><td>

<a name=affiliate></a><font size=+1><b>Affilicate Program <font color=red>20 cents one click, no limit</font></b></font>
<br><br>
It's simple, put a link any where you want, include your account id in query string(URI) eg:
<br>
http://www.edenonline.co.nz/p.aspx?100025&ai=2
<br>
it's the last parameter "&ai=2", 2 is account id, you can go to <a href=status.aspx class=o>status page</a> to see your account id.

<ul>
<b>We will check : </b>
<li>The referer URL, must be a valid web page, not any kind of script.
<li>The client IP, must from within New Zealand, so don't bother to make links on none NZ focussed sites.
<li>Repeatly click throughs from a same IP, or same ISP will not be credit. 
<li>Credit over $100 must be audited before can be claimed.
<li>Money can be claimed only if you can supply invoice with valid GST number.
</ul>

<br><br>

<a name=referer></a><font size=+1><b>Tell A Friend to earn <font color=red>5 dollars credit</font></b></font>
<br><br>

<form action=ds.aspx method=post>

<br><b>Options</b>
<br>1. put your friend's email address into your referer list, then tell him about our site, you get 5 dollars once anybody bought anything uses this email address. (no email restriction on this option)
<br>Your Friend's Email : <input type=text name=semail size=20><input type=submit name=cmd value=Record>
<br>
<br>2. Email your friend an invitation from our site(the email will include a random refer string). You get 5 dollars once he registered.(email restriction apply to this option)
<br><table border=1>
<tr><td><b>subject : </b></td><td><input type=text size=65 name=subject></td></tr>
<tr><td valign=top><b>text : </b></td><td><textarea name=text rows=5 cols=50></textarea></td></tr>
<tr><td><b>Email : </b></td><td><input type=text name=femail></td></tr>
<tr><td colspan=2 align=right><input type=submit name=cmd value=Send></td></tr>
</table>

<br>Ask your good friend to register on our site, enter your account id as referer number, we will send him an email include a confirm link to validate your credit. You get 5 dollars once he finished the confirmation. (email restriction apply to this option)

</form>

We reserve the right to audit your logs, the affiliate program is designed to promote this web site, any kind of defraud will cause your account canceled and all credit withheld.



</td></tr></table>

<asp:Label id=LFooter runat=server/>
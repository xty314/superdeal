<script runat="server">

protected void Page_Load(Object Src, EventArgs E ) 
{
	
	//TS_PageLoad(); //do common things, LogVisit etc...
		
	//Calendar1_SelectionChanged();
	//Calendar1_DayRender();
}

void Calendar1_SelectionChanged(Object sender, System.EventArgs e)
{
    string strjscript = "<script language='javascript'>";
    strjscript += "window.opener." + HttpContext.Current.Request.QueryString["formname"];
    strjscript += ".value = '" + Calendar1.SelectedDate.ToString("d") + "';window.close();";
    strjscript += "</script" + ">"; //Don't ask, tool bug.
    Literal1.Text = strjscript;
}


void Calendar1_DayRender(Object source, DayRenderEventArgs e)
{
   if (e.Day.Date.ToString("d") == DateTime.Now.ToString("d"))
    {
        e.Cell.BackColor = System.Drawing.Color.Orange;
    }
    
   	//Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.pdate"+i.ToString()+"','calendar_window','width=190,height=230');calendar_window.focus()\">");
	//Response.Write("<font color=blue><b>...</font></b>");
}

</script>

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"><html>
<head>
    <title>---PICK A DATE---</title> 
</head>
<body leftmargin="0" topmargin="0">
    <form name=frm runat="server">
		<asp:Calendar id="Calendar1" runat="server" 
		OnSelectionChanged="Calendar1_SelectionChanged" 
		OnDayRender="Calendar1_DayRender" 
		Font-Size="10pt"
		ShowGridLines="true"
        BackColor="beige"
        ForeColor="darkblue"
	    SelectedDayStyle-BackColor="black"
        SelectedDayStyle-ForeColor="red"
        SelectedDayStyle-Font-Bold="true"
		showtitle="true" 
		DayNameFormat="short" 
		SelectionMode="Day" 
		TitleStyle-BackColor="#2E79CD"
        TitleStyle-ForeColor="white"
        TitleStyle-Font-Bold="true"
        NextPrevStyle-BackColor="darkblue"
        NextPrevStyle-ForeColor="#4BA4DD"
        DayHeaderStyle-BackColor="#82DD4B"
        DayHeaderStyle-ForeColor="white"
        DayHeaderStyle-Font-Bold="true"
        OtherMonthDayStyle-BackColor="white"
        OtherMonthDayStyle-ForeColor="#82DD4B"
		Height="25" Width="35">
             
            <TitleStyle backcolor="#2E79CD" forecolor="black" />
            <NextPrevStyle backcolor="#2E79CD" forecolor="black" />
            <OtherMonthDayStyle forecolor="#F49D57" />
        </asp:Calendar>
        <asp:Literal id="Literal1" runat="server"></asp:Literal>
    </form>
</body>
</html>

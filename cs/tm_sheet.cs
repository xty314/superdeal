
<script runat=server>

DataSet dst = new DataSet();

bool m_bStarted = false;
bool m_bFinished = false;
bool m_bChkWork = false;

int m_nReturnRows = 0;
int m_nRowsReturn = 0;

string m_smFrom = "";
string m_Start_id = "";
string[] m_EachDay = new string[7];
string[] m_EachMonth = new string[13];
string[] m_Employee = new string[50];
string[] m_days = new string[13];
string m_server_ip = "";
string m_user_ip = "";

string[] m_name = new string[255];
string[] m_wk_date = new string[255];
string[] m_start_time = new string[255];
string[] m_finish_time = new string[255];

string m_type = "1";
void Page_Load (Object Src, EventArgs E)
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("technician"))
		return;
	m_EachDay[0] = "MONDAY";
	m_EachDay[1] = "TUESDAY";
	m_EachDay[2] = "WEDNESDAY";
	m_EachDay[3] = "THURSDAY";
	m_EachDay[4] = "FRIDAY";
	m_EachDay[5] = "SATURDAY";
	m_EachDay[6] = "SUNDAY";
	
	//monthly name
	m_EachMonth[0] = "JAN";
	m_EachMonth[1] = "FEB";
	m_EachMonth[2] = "MAR";
	m_EachMonth[3] = "APR";
	m_EachMonth[4] = "MAY";
	m_EachMonth[5] = "JUN";
	m_EachMonth[6] = "JUL";
	m_EachMonth[7] = "AUG";
	m_EachMonth[8] = "SEP";
	m_EachMonth[9] = "OCT";
	m_EachMonth[10] = "NOV";
	m_EachMonth[11] = "DEC";
	//----
	
	//monthly name
	m_days[1] = "31";
	if(DateTime.IsLeapYear(DateTime.Now.Year))
		m_days[2] = "29";
	else
		m_days[2] = "28";
	m_days[3] = "31";
	m_days[4] = "30";
	m_days[5] = "31";
	m_days[6] = "30";
	m_days[7] = "31";
	m_days[8] = "31";
	m_days[9] = "30";
	m_days[10] = "31";
	m_days[11] = "30";
	m_days[12] = "31";
	//----


	RememberLastPage();
	string uip = Request.ServerVariables["REMOTE_ADDR"];

	string sip = GetSiteSettings("local_ip", "127.0.0.1"); 
//	DEBUG("localt=", Request.ServerVariables["LOCAL_ADDR"].ToString());
//	DEBUG("uip = ", uip);
//	DEBUG("sip =", sip);
//	DEBUG("day ", DateTime.IsLeapYear(DateTime.Now.Year).ToString());
	for(int i=0; i<3; i++)
	{
		m_user_ip += uip[i].ToString();
		m_server_ip += sip[i].ToString();
	}

	if(Request.Form["cmd"] == "SHOW TIME")
	{
		m_type = Request.Form["type"];
		if(m_type == "1")
			m_smFrom = DateTime.Now.Month.ToString();
		else
			m_smFrom = Request.Form["pick_month"];
		if(!getTimeSheet())
			return;
		DisplayTimeSheet();
		return;
	}

	if(Request.Form["cmd"] == "Start Work")
	{
		if(m_user_ip != m_server_ip)
		{
			Response.Write("<script language=javascript>window.alert('YOU MUST USE LOCAL IP ADDRESS TO START TIME STAMP'); window.history.back();</script");
			Response.Write(">");
			return;
		}

		if(!doInsertStartTime())
			return;
		Response.Write("<script language=javascript>window.close()</script");
		Response.Write(">\r\n");
	}
	if(Request.Form["cmd"] == "Lets Get Out Here")
	{
		if(m_user_ip != m_server_ip)
		{
			Response.Write("<script language=javascript>window.alert('YOU MUST USE LOCAL IP ADDRESS TO FINISH TIME STAMP'); window.history.back();</script");
			Response.Write(">");
			return;
		}
		if(!doUpdateFinishTime())
			return;
		Response.Write("<script language=javascript>window.close()</script");
		Response.Write(">\r\n");
	}
	if(Request.Form["cmd"] == "Book A Day Off")
	{
		if(!doMakeADayOff())
			return;
		Response.Write("<script language=javascript>window.close()</script");
		Response.Write(">\r\n");
	}
/*	if(Request.QueryString["s"] == "view")
	{
		InitializeData();
		DisplayTimeSheet();
		return;
	}
*/	
	InitializeData();
	ShowWorkTime();
	/*if(DateTime.Now.ToString("HH:mm") == "17:30")
	{
		if(!doUpdateFinishTime())
			return;
	}*/
	
	return;
		
}


bool doMakeADayOff()
{	
	string sc = " SET DATEFORMAT dmy ";
	sc += " INSERT INTO dayoff (staff_id, dayoff, reason, update_date) ";
	sc += " VALUES( "+ Session["card_id"] +", '"+ Request.Form["dayoff"] +"' ";
	sc += ", CONVERT(VARCHAR(512), '"+ Request.Form["reason"] +"'), GETDATE() ";
	sc += ")";
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

bool doInsertStartTime()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " INSERT INTO wk_tm_sheet (staff_id, work_date, start_time, start_check, finish_check) ";
	sc += " VALUES( '"+ Session["card_id"] +"', '"+ Request.Form["work_date"] +"', GETDATE(), 1 , 0)";
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

bool doUpdateFinishTime()
{

	string sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE wk_tm_sheet ";
	sc += " SET finish_time = GETDATE() ";
	sc += " , finish_check = 1 ";
	sc += " WHERE id = "+ Request.Form["start_id"] +" ";
//DEBUG("sc = ", sc);
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

bool ShowStartTime()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT * FROM wk_tm_sheet ";
	sc += " WHERE staff_id = "+ Session["card_id"] +" ";
	sc += " AND work_date = '"+ DateTime.Now.ToString("dd-MM-yy") +"'";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nRowsReturn = myAdapter.Fill(dst,"wk_yet");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	if(m_nRowsReturn > 0)
		m_bStarted = true;

	m_bChkWork = m_bStarted;

	return m_bChkWork;
}

bool ShowFinishTime()
{
	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT * FROM wk_tm_sheet ";
	sc += " WHERE staff_id = "+ Session["card_id"] +" ";
	sc += " AND work_date = '"+ DateTime.Now.ToString("dd-MM-yy") +"'";
	sc += " AND start_check = 1 ";
//	sc += " AND finish_check = 0 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nRowsReturn = myAdapter.Fill(dst, "wk_done");
		//if(myAdapter.Fill(dst, "wk_done") == 1)
		if(m_nRowsReturn == 1)
		{
			m_Start_id = dst.Tables["wk_done"].Rows[0]["id"].ToString();
			//DEBUG("m_statid = ", m_Start_id);
			//m_Start_id = dst.Tables["wk_done"].Rows[0]["work_date"].ToString();
		}
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}
	if(m_nRowsReturn == 1)
		m_bFinished = true;
	
//DEBUG("m_bFinish=", m_bFinished.ToString());
	m_bChkWork = m_bFinished;

	return m_bChkWork;
}

bool getAllEmployee()
{
	string sc = " SELECT id, name FROM card ";
	sc += " WHERE type = 4 ";
	sc += " ORDER BY name ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		myAdapter.Fill(dst, "employee");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	return true;
	
}

bool getTimeSheet()
{
	string sql_qry = "";
	if(m_type == "1")
		sql_qry = " AND DATEDIFF(month, wk.work_date, GETDATE()) = 0 ";
	if(m_type == "2")
		sql_qry = " AND Month(wk.work_date) = "+ m_smFrom +" ";
		//sql_qry = " AND wk.work_date BETWEEN '"+ m_sdate +"' AND '"+ m_fdate +" 23:59"+"'";

	string sc = " SET DATEFORMAT dmy ";
	sc += " SELECT wk.*, c.name FROM wk_tm_sheet wk JOIN card c  ";
	sc += " ON c.id = wk.staff_id ";
	sc += " WHERE 1=1 ";

	sc += sql_qry;

	sc += " ORDER BY wk.work_date ";
//DEBUG("sc = ", sc );
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		m_nReturnRows = myAdapter.Fill(dst, "wk_sheet");
	}
	catch (Exception e)
	{
		ShowExp(sc,e);
		return false;
	}

	return true;
	
}
void DisplayTimeSheet()
{
	Response.Write("<br><form name=frm method=post action='"+ Request.ServerVariables["URL"] +"?s=view' encrypted>");
	//Response.Write("Today's Date: "+ DateTime.Now.ToString() +"</center>");
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=2 border=0 bordercolor=black bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
/*	Response.Write("<tr><td>Select By: &nbsp;");
	
	Response.Write("<select name='pick_month'>");
	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		txtMonth = m_EachMonth[m-1];
		Response.Write("<option value="+m+"");
		if(int.Parse(DateTime.Now.ToString("MM")) == m)
			Response.Write(" selected ");
		Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
	}
	Response.Write("<option value='current_week'>Current Week</option>");
	Response.Write("</select><input type=submit name=cmd value='Show Time' "+ Session["button_style"] +"></tr>");
	Response.Write("</select>");
*/
	if(!getAllEmployee())
			return;
	//Response.Write("<form name=frm method=post>");
	Response.Write("<tr><td colspan=7><input type=button value='Print TimeSheet' "+ Session["button_style"] +" Onclick='window.print(); window.close();'></td></tr>");
	Response.Write("<tr><td colspan=7>");
	Response.Write("<table width=100% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:0px;border-style:Solid;border-collapse:collapse;fixed\">");
	int emp_row = dst.Tables["employee"].Rows.Count;
	Response.Write("<tr align=center bgcolor=#EEE222><td>&nbsp;</td>");
	
	for(int i=0; i<emp_row; i++)
	{
		DataRow dr = dst.Tables["employee"].Rows[i];
		string name = dr["name"].ToString();
		string id = dr["id"].ToString();
		m_Employee[i] = name;
		Response.Write("<td colspan=2><b>"+ m_Employee[i].ToUpper() +"</td>");
		
	}

	Response.Write("</tr>");
	Response.Write("<tr align=center><td>&nbsp;</td>");
	for(int i=0; i<emp_row; i++)
		Response.Write("<td width=>IN</td><td width=>OUT</td>");
	Response.Write("</tr>");
	bool bAlter = true;
	double dTotalHours = 0;

	for(int i=0; i< m_nReturnRows; i++)
	{
		DataRow dr = dst.Tables["wk_sheet"].Rows[i];
		string wk_date = dr["work_date"].ToString();
		wk_date = DateTime.Parse(wk_date).ToString("dd");

	//	string name = dr["name"].ToString();
		m_name[int.Parse(wk_date)] = dr["name"].ToString();
		m_start_time[int.Parse(wk_date)] = dr["start_time"].ToString();
		m_finish_time[int.Parse(wk_date)] = dr["finish_time"].ToString();
		//string start_time = dr["start_time"].ToString();
		//string finish_time = dr["finish_time"].ToString();
		//string note = dr["note"].ToString();
		//string temp_day = DateTime.Parse(wk_date).ToString("dd");
//	DEBUG("tmepdate = ", temp_day);
		if(m_start_time[int.Parse(wk_date)] != "" && m_start_time[int.Parse(wk_date)] != null)
			m_start_time[int.Parse(wk_date)] = DateTime.Parse(m_start_time[int.Parse(wk_date)]).ToString("HH:mm:ss");
		if(m_finish_time[int.Parse(wk_date)] != "" && m_finish_time[int.Parse(wk_date)] != null)
			m_finish_time[int.Parse(wk_date)] = DateTime.Parse(m_finish_time[int.Parse(wk_date)]).ToString("HH:mm:ss");
		//string hours = (DateTime.Parse(finish_time) - DateTime.Parse(start_time)).ToString(); 
		//string hours = finish_time;
		//if(int.Parse(temp_day) == i)
//			Response.Write("<td>"+ start_time +"</td><td>"+ finish_time +"</td>");
		
	}
			
	for(int i=1; i<=int.Parse(m_days[int.Parse(m_smFrom)]); i++)
//	for(int i=0; i<m_nReturnRows; i++)
	{
			
		Response.Write("<tr bgcolor= ");
		if(bAlter)
			Response.Write(" #EEEEEE");
		bAlter = !bAlter;
			Response.Write(">");
		string sday = i + "-" + m_smFrom +"-"+ DateTime.Now.Year.ToString();
		Response.Write("<td align=center>"+ sday +"</td>");
			
			
		for(int j=0; j<emp_row; j++)
		{
			if(m_name[i] == m_Employee[j])
				Response.Write("<td>"+ m_start_time[i] +"</td><td>"+ m_finish_time[i] +"</td>");
			else
				Response.Write("<td>&nbsp;</td><td>&nbsp;</td>");
		}
	
		Response.Write("</tr>");

	}
	
	Response.Write("</table>");
	Response.Write("</td></tr>");
	Response.Write("</table></form>");
}
void ShowWorkTime()
{
	ShowStartTime();

	Response.Write("<form name=frm method=post><br>");

	Response.Write("<table width=80% align=center cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<input type=hidden name='work_date' value='"+ DateTime.Now.ToString("dd-MM-yy") +"'>");
	Response.Write("<input type=hidden name=bstarted value='"+ m_bStarted.ToString() +"'>");
	Response.Write("<input type=hidden name=bfinished value='"+ m_bFinished.ToString() +"'>");
	
	Response.Write("<tr align=center><td colspan=2><font size=+1>Welcome to Employees Time Stamp Section<br></td></tr>");

	Response.Write("<tr align=center><td colspan=2>Today's Date: "+ DateTime.Now.ToString("dd-MM-yy") +"</td></tr>");
	
	if(TS_UserLoggedIn())
	{
		string greeting = "";
		if(int.Parse(DateTime.Now.ToString("HH")) > 0 && int.Parse(DateTime.Now.ToString("HH")) < 12)
			greeting = "GOOD MORNING";
		else if(int.Parse(DateTime.Now.ToString("HH")) > 12 && int.Parse(DateTime.Now.ToString("HH")) < 18)
			greeting = "GOOD AFTERNOON";
		else
			greeting = "GOOD EVENING";
		Response.Write("<tr bgcolor=#DDDEEE><td colspan=2><b>"+ greeting +" :&nbsp;&nbsp;"+ Session["name"] +"</td></tr>");
	}
	if(!m_bStarted)
	{
		Response.Write("<tr><td>Start Time:</td><td><input type=text  readonly name=start > </td></tr>");
		Response.Write("<tr><td></td><td><input type=submit name=cmd value='Start Work' "+ Session["button_style"] +"></td></tr>");
	}

	if(m_bFinished)
		Response.Write("<center><font color=Green>-You Already Signed Out-</font><br><br>");

	if(m_bStarted && !m_bFinished)
	{
		ShowFinishTime();
		string s_time = dst.Tables["wk_yet"].Rows[0]["start_time"].ToString();
		
		Response.Write("<tr><td>Start Time:</td><td><input type=text readonly value="+ DateTime.Parse(s_time).ToString("HH:mm:ss ") +" ></td></tr>");
		Response.Write("<tr><td>Finish Time:</td><td><input type=text readonly name=finish ></td></tr>");
		Response.Write("<tr><td></td><td><input type=submit name=cmd value='Lets Get Out Here'"+ Session["button_style"] +"></td></tr>");
		Response.Write("<input type=hidden name='start_id' value='"+ m_Start_id +"'>");
	}
	Response.Write("</table><br>");
	
/*	Response.Write("<table width=80% align=center cellspacing=1 cellpadding=2 border=0 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	Response.Write("<tr ><td colspan=2><b>BOOK A Day Off </b></td></tr>");
	Response.Write("<tr><td width=5%>Date:</td><td><input type=text name=dayoff>");
	Response.Write(" <a title='Select A Date' ");
	Response.Write(" href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.dayoff','calendar_window','width=190,height=230');calendar_window.focus()\">");
	Response.Write("...</a>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td>Reason:</td><td><textarea cols=35 rows=7 name=reason></textarea></td></tr>");
	Response.Write("<tr><td></td><td><input type=submit name=cmd value='Book A Day Off'"+ Session["button_style"] +" onclick='return chkReason();'></td></tr>");
	Response.Write("</table>");
	Response.Write("</form>");
*/
	Response.Write("<table width=80% align=center cellspacing=0 cellpadding=2 border=1 bordercolor=gray bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px;border-style:Solid;border-collapse:collapse;fixed\">");
	
	Response.Write("<tr><td>");
	Response.Write("<input type=radio name=type checked value=1>");
	Response.Write("</td><td>Show This Month's Time Sheet");
	Response.Write("</td></tr>");

	Response.Write("<tr><td>");
	Response.Write("<input type=radio name=type value=2></td><td>");
	Response.Write("Select By: &nbsp;");
	Response.Write("<select name='pick_month'>");
	for(int m=1; m<13; m++)
	{
		string txtMonth = "";
		txtMonth = m_EachMonth[m-1];
		Response.Write("<option value="+ m +"");
		if(int.Parse(DateTime.Now.ToString("MM")) == m)
			Response.Write(" selected ");
		Response.Write(">"+txtMonth+"-"+DateTime.Now.ToString("yy")+"</option>");
	}
	
	//Response.Write("</select><input type=submit name=cmd value='Show Time' "+ Session["button_style"] +"></tr>");
	Response.Write("</select>&nbsp;&nbsp;<input type=submit name=cmd value='SHOW TIME' "+ Session["button_style"] +"> ");
	Response.Write("</td></tr>");

	Response.Write("</table>");
	
	Response.Write("<script language=javascript>");
	Response.Write("<!-- ");
//	Response.Write(" /*By George Chiang (JK's JavaScript tutorial) ");
//	Response.Write(" http://javascriptkit.com ");
//	Response.Write(" Credit must stay intact for use*/ ");
	string s = @"
	function chkReason()
	{
		if(document.frm.dayoff.value == '')
		{
			document.frm.dayoff.select();
			document.frm.dayoff.focus();
			return false;
		}
		if(document.frm.reason.value == '' )
		{
			document.frm.reason.select();
			document.frm.reason.focus();
			return false;
		}
		return true;
	}
	function show(){
	var Digital=new Date();
	var hours=Digital.getHours();
	var minutes=Digital.getMinutes();
	var seconds=Digital.getSeconds();
	var dn='AM';
	if (hours>=12){
	dn='PM';
	//hours=hours-12;
	}
	if (hours==0)
	hours=12;
	if (minutes<=9)
	minutes='0'+minutes;
	if (seconds<=9)
	seconds='0'+seconds;
	//window.alert(document.frm.bstarted.value);
	if(document.frm.bstarted.value == 'False'){
	document.frm.start.value=hours+':'+minutes+':' +seconds+' '+dn;
	setTimeout('show()',1000);
	}
	if(document.frm.bstarted.value == 'True'){
	document.frm.finish.value=hours+':'+minutes+':' +seconds+' '+dn;
	setTimeout('show()',1000);
	}
	}
	show();

		";
	Response.Write(s);
	Response.Write("</script");
	Response.Write("> ");
}
</script>
<asp:Label id=LFooter runat=server/>
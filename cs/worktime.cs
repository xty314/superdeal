<script runat=server>

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	
	if(Request.Form["barcode"] != null)
	{
		DoRecordTimeStamp();
		return;
	}
	if(Request.Form["cmd"] != null)
	{
		string cmd = Request.Form["cmd"];
//		Response.Write("<meta http-equiv=\"refresh\" content=\"0; URL=worktime.aspx\">");
	}
	else
	{
		string type = Request.QueryString["t"];
		PrintScanForm();
	}
}

void PrintScanForm()
{
	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<center><h1>" + m_sCompanyName + " Staff Checkin</h1>");
	Response.Write("<form name=f action=worktime.aspx method=post>");
	Response.Write("<table border=0>");
	Response.Write("<tr><td><b>Please scan your employment card</b></td></tr>");
	Response.Write("<tr><td><input type=text name=barcode><input type=submit name=cmd value=Record class=b></td></tr>");
	Response.Write("</table></form>");
	Response.Write("<script language=javascript>document.f.barcode.focus();</script");
	Response.Write(">");
}

bool CheckAllowIPOK()
{
	int i = 0;
	int j = 0;
	string[] abip = new string[1024];
	string oneip = "";

	if(Session["staff_checkin_allow_ip"] == null)
	{
		string allow_ip = GetSiteSettings("staff_checkin_allow_ip", "");
		for(i=0; i<allow_ip.Length; i++)
		{
			if(allow_ip[i] == ' ' || allow_ip[i] == ',' || allow_ip[i] == ';')
			{
				Trim(ref oneip);
				if(oneip != "")
				{
					abip[j++] = oneip;
					oneip = "";
				}
			}
			else
			{
				oneip += allow_ip[i];
			}
		}
		if(oneip != "") //the last one
		{
			abip[j++] = oneip;
			oneip = "";
		}

		Session["staff_checkin_allow_ip"] = abip;
	}
	else
	{
		abip = (string[])Session["staff_checkin_allow_ip"];
	}
	string ip = "";
	if(Session["ip"] != null)
		ip = Session["ip"].ToString();
	if(ip == "")
		return true;
	for(i=0; i<abip.Length; i++)
	{
		oneip = abip[i];
		if(oneip == null)
			break;

//DEBUG("oneip=", oneip);
//DEBUG("ip=", ip);
		if(ip.IndexOf(oneip) == 0)// || ip == "127.0.0.1")
			return true;
	}
	return false;
}

bool DoRecordTimeStamp()
{
	if(!CheckAllowIPOK())
	{
		PrintAdminHeader();
		PrintAdminMenu();

		Response.Write("<br><br><center><h4>Checkin denied from your IP:" + Session["ip"] + "</h4>");
		return false;
	}

	DataSet ds = new DataSet();
	String barcode = Request.Form["barcode"];
	string sc = " SELECT id, name FROM card WHERE barcode = '" + barcode + "' AND type = 4 ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "barcode") <= 0)
		{
			PrintAdminHeader();
			Response.Write("<br><br><br><center><h4>Invalid barcode, please scan only Employment Card.</h4>");
			Response.Write("<input type=button value=Back onclick=window.location='worktime.aspx' class=b>");
			return false;
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}
	string card_id = ds.Tables["barcode"].Rows[0]["id"].ToString();

	//get last record of today
	bool bCheckin = true;
	bool bLastIsCheckin = false;
	string lastCheckinTime = "";
	string lastRecordID = "";
	sc = " SELECT TOP 1 * FROM work_time WHERE card_id = " + card_id + " AND DATEDIFF(day, record_time, GETDATE()) = 0 ORDER BY record_time DESC ";
	try
	{
		myAdapter = new SqlDataAdapter(sc, myConnection);
		if(myAdapter.Fill(ds, "last") == 1)
		{
			DataRow dr = ds.Tables["last"].Rows[0];
			bLastIsCheckin = MyBooleanParse(dr["is_checkin"].ToString());
			if(bLastIsCheckin)
			{
				bCheckin = false; //this time is a checkout
				lastCheckinTime = dr["record_time"].ToString();
				lastRecordID = dr["id"].ToString();
			}
		}
	}
	catch(Exception e)
	{
		ShowExp(sc, e);
		return false;
	}

	if(!bLastIsCheckin)
	{
		sc = " INSERT INTO work_time (card_id, is_checkin) VALUES(" + card_id + ", 1)";
	}
	else
	{
		double dMinutes = 0;
		double dHours = 0;
		sc = " SELECT DATEDIFF(minute, record_time, getdate()) AS minutes FROM work_time WHERE id = " + lastRecordID;
		try
		{
			myAdapter = new SqlDataAdapter(sc, myConnection);
			if(myAdapter.Fill(ds, "hours") == 1)
			{
				DataRow dr = ds.Tables["hours"].Rows[0];
				dMinutes = MyDoubleParse(dr["minutes"].ToString());
				dHours = Math.Round(dMinutes / 60, 2);
			}
		}
		catch(Exception e)
		{
			ShowExp(sc, e);
			return false;
		}
		sc = " INSERT INTO work_time (card_id, hours, is_checkin) VALUES(" + card_id + ", " + dHours + ", 0)";
	}
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

	PrintAdminHeader();
	PrintAdminMenu();
	Response.Write("<br><br><center><h2>");
	string name = TSGetUserNameByID(card_id);
	string nick = "";
	for(int i=0; i<name.Length; i++)
	{
		if(name[i] == ' ')
			break;
		nick += name[i];
	}
	if(bCheckin)
		Response.Write(nick + " just checked in at " + DateTime.Now.ToString());
	else
		Response.Write(nick + " just checked out at " + DateTime.Now.ToString());
	Response.Write("</h4>");
	Response.Write("<form name=fok action=default.aspx method=post><input type=button name=bok value=' OK ' class=b onclick=window.location='default.aspx'>");
	Response.Write("<script language=javascript>document.fok.bok.focus();</script");
	Response.Write("></form>");

	return true;
}
</script>

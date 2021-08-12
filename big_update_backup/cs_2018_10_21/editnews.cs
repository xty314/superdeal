<script runat=server>

DataSet dst = new DataSet();
string m_command = "";

string m_id = "";
string m_sdate = "";
string m_subject = "";
string m_news = "";

DataRow[] m_dra = null; 

void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;

	PrintAdminHeader();
	PrintAdminMenu();
//	if(!IsPostBack)
//		BindGrid();
	if(Request.Form["cmd"] != null && Request.Form["cmd"] != "")
		m_command = Request.Form["cmd"];
//DEBUG("m_command = ", m_command);
	
	if(m_command == "Add News")
	{
		if(DoInsertNews())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"?nid="+ Request.QueryString["nid"] +"');</script");
			Response.Write(">\r\n");
			return;
		}
	}
	if(m_command == "Update News")
	{
		if(!DoUpdateNews())
			return;
	}
	if(Request.QueryString["delid"] != null && Request.QueryString["delid"] != "")
	{
		if(DoDeleteNews())
		{
			Response.Write("<script language=javascript>window.location=('"+ Request.ServerVariables["URL"] +"');</script");
			Response.Write(">\r\n");
			return;
		}
	}
	if(!DoQueryNews())
		return;
	ShowNews();
	LFooter.Text = m_sAdminFooter;
}

void ShowNews()
{
	if(m_sdate != "")
		m_sdate = DateTime.Parse(m_sdate).ToString("dd-MM-yy");
	Response.Write("<form name=frm method=post>");
	Response.Write("<table width=95% border=0 align=center>");
	Response.Write("<tr>");
	Response.Write("<td><table>");
//	Response.Write("<tr><td><b>Date : </b></td><td><asp:TextBox id="TextBoxDate" Text="" Columns=80 runat="server"/></td></tr>");
//	Response.Write("<tr><td valign=top><b>Text : </b></td><td><asp:TextBox id="TextBoxText" Text="" Columns=100 TextMode=MultiLine Rows=20 runat="server"/></td></tr>");
//	Response.Write("<tr><td><b>Subject : </b></td><td><asp:TextBox id="TextBoxSubject" Text="" Columns=80 runat="server"/></td></tr>");
	Response.Write("<tr><td><b>Date : </b></td><td><input type=text name=s_date value='"+ m_sdate +"'>");
	Response.Write("<a href=\"javascript:calendar_window=window.open('calendar.aspx?formname=frm.s_date','calendar_window','width=190,height=230, resizable=1');calendar_window.focus()\" class=o>...</a>");
	Response.Write("</td></tr>");
	Response.Write("<tr><td><b>Subject : </b></td><td><input type=text size=40% name=subject value='"+ m_subject +"'></td></tr>");
	Response.Write("<tr><td valign=top><b>Text : </b></td><td><textarea name=news rows=25 cols=100 value="+ m_news +">"+ m_news +"</textarea></td></tr>");
	Response.Write("<input type=hidden name=h_nid value="+ m_id +">");
	Response.Write("</table>");

	Response.Write("</td>");

	Response.Write("<td align=right valign=bottom>");
	
	if(Request.QueryString["nid"] != null && Request.QueryString["nid"] != "")
	{
		Response.Write("<input type=button name=cmd value=");
		Response.Write("'New' ");
		Response.Write(""+ Session["button_style"] +" onclick=\"window.location=('"+ Request.ServerVariables["URL"] +"')\">");

		Response.Write("<input type=submit name=cmd value=");
		Response.Write("'Update News' ");
		Response.Write(""+ Session["button_style"] +">");
	}
	else
	{
		Response.Write("<input type=submit name=cmd value=");
		Response.Write("'Add News' ");
		Response.Write(""+ Session["button_style"] +">");
	}
	//Response.Write("<asp:Button id="button1" text="&nbsp;&nbsp;&nbsp;Add News&nbsp;&nbsp;&nbsp;" CommandArgument="arg" OnClick="CommandBtn_Click" runat="server"/>");
	Response.Write("</td>");

	Response.Write("</tr>");
	Response.Write("</table>");

	Response.Write("</form><br> ");

}

bool DoUpdateNews()
{
	string id = Request.Form["h_nid"];
	string subject = Request.Form["subject"];
	string text = Request.Form["news"];
	string sdate = Request.Form["s_date"];
	if(sdate == "" || sdate == null)
		sdate = DateTime.Now.ToString("");
//DEBUG("id = ", id);
//DEBUG("subject = ", subject);
//DEBUG("stext = ", text);
//DEBUG("sdate = ", sdate);

	string sc = " SET DATEFORMAT dmy ";
	sc += " UPDATE news SET date = '"+ sdate +"' ";
	sc += ", subject = CONVERT(varchar(50),'"+ EncodeQuote(subject) +"') ";
	sc += ", text= '"+ EncodeQuote(text) +"' ";
	sc += " WHERE id = "+ id +"";
//DEBUG("sc = ", sc);

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

bool DoDeleteNews()
{
	string id = Request.QueryString["delid"];
	
	if(TSIsDigit(id))
	{
//DEBUG("sdate = ", sdate);
	string sc = " SET DATEFORMAT dmy ";
	sc += " DELETE news WHERE id = "+ id;
//DEBUG("sc = ", sc);
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
	}
	return true;
}

bool DoQueryNews()
{
	string sc = " SELECT * FROM news ";
	
	int rows = 0;
	try
	{
		SqlDataAdapter myCommand1 = new SqlDataAdapter(sc, myConnection);
		rows = myCommand1.Fill(dst, "news");
		if(rows <= 0)
		{
	//		Response.Write("<br><br><center><h3>Error getting order items, id=" + m_id + ", rows return:" + rows + "</h3>");
		}
	}
	catch(Exception e) 
	{
		ShowExp(sc, e);
		return false;
	}
	Response.Write("<br><center><h4>NEWS</center></h4>");
	Response.Write("<table width=95% align=center cellspacing=1 cellpadding=2 border=1 bordercolor=#EEEEEE bgcolor=white");
	Response.Write(" style=\"font-family:Verdana;font-size:8pt;border-width:1px; \">"); //border-style:Solid;border-collapse:collapse;fixed\">");
	bool bAlter = false;

	if(rows > 0)
	{
		if(Request.QueryString["nid"] != null && Request.QueryString["nid"] != "")
		{
		m_dra = dst.Tables["news"].Select("id="+ Request.QueryString["nid"] +"", "");
		int nLength = m_dra.Length;
		if(nLength == 1)
		{
			m_id = m_dra[0]["id"].ToString();
			m_sdate = m_dra[0]["date"].ToString();
			m_subject = m_dra[0]["subject"].ToString();
			m_news = m_dra[0]["text"].ToString();
		}
		}
//		DEBUG("mid =", m_id);
//		DEBUG("mdate =", m_sdate);
//		DEBUG("msubject =", m_subject);
//		DEBUG("mnews =", m_news);
		Response.Write("<tr align=left bgcolor=#b3ADE><th>DATE</td><th>SUBJECT</td><th>NEWS</td><th>ACTION</td></tr>");
		
		for(int i=0; i<rows; i++)
		{
			DataRow dr = dst.Tables["news"].Rows[i];
			string sdate = dr["date"].ToString();
			string id = dr["id"].ToString();
			string subject = dr["subject"].ToString();
			string news = dr["text"].ToString();	

			Response.Write("<tr");
			if(bAlter)
				Response.Write(" bgcolor=#EEEEEE ");
			bAlter = !bAlter;
			Response.Write(">");
			Response.Write("<td>"+ sdate +"");
			Response.Write("</td>");
			Response.Write("<td>"+ subject +"");
			Response.Write("</td>");
			Response.Write("<td width=50%><textarea cols=60 rows=1>"+ news +"</textarea>");
			Response.Write("</td>");
			Response.Write("<td>");
			Response.Write("<a title='Edit News' href='"+ Request.ServerVariables["URL"] +"?nid="+ id +"' class=o>EDIT</a>&nbsp;&nbsp; ");
			Response.Write("<a title='DELETE News' href='"+ Request.ServerVariables["URL"] +"?delid="+ id +"' class=o><font color=red><b>X</a>");
			Response.Write("</td>");
			Response.Write("</tr>");
		}
	}

	Response.Write("</table>");
	return true;
}
bool DoInsertNews()
{
	string subject = Request.Form["subject"];
	string text = Request.Form["news"];
	string sdate = Request.Form["s_date"];
	if(sdate == "" || sdate == null)
		sdate = DateTime.Now.ToString("dd-MM-yyyy");
//DEBUG("sdate = ", sdate);
	string sc = " SET DATEFORMAT dmy ";
	sc += " INSERT INTO news (date, subject, text) VALUES (";
	sc += "'"+ sdate +"' ";
	sc += ", CONVERT(varchar(50),'";
	sc += EncodeQuote(subject);
	sc += "'), '";
	sc += EncodeQuote(text);
	sc += "')";
//DEBUG("sc = ", sc);
//DEBUG("id = ", id);
//DEBUG("subject = ", subject);
//DEBUG("stext = ", text);
//DEBUG("sdate = ", sdate);
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
/*
void BindGrid()
{
//	SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
	string sc = "SELECT * FROM news ORDER BY date DESC";
	SqlDataAdapter myCommand = new SqlDataAdapter(sc, myConnection);
	DataSet ds = new DataSet();
	myCommand.Fill(ds);
	DataView dv = new DataView(ds.Tables[0]);
	MyDataGrid.DataSource = dv ;
	MyDataGrid.DataBind();
}

void MyDataGrid_Edit(object sender, DataGridCommandEventArgs e)
{
	MyDataGrid.EditItemIndex = e.Item.ItemIndex;
	BindGrid();
}

void MyDataGrid_Cancel(Object sender, DataGridCommandEventArgs e) 
{
	MyDataGrid.EditItemIndex = -1;
	BindGrid();
}
 
void MyDataGrid_Delete(Object sender, DataGridCommandEventArgs e) 
{
	string sid = e.Item.Cells[0].Text;
	int id = int.Parse(sid);
//	SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
//	SqlConnection myConnection = new SqlConnection("Initial Catalog=topsys;Data Source=localhost;Integrated Security=SSPI;");
	string sc = "DELETE news ";
	sc += "WHERE id=";
	sc += id;

	try
	{
		SqlCommand myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception exp) 
	{
		ShowExp(sc, exp);
		return;
	}
	
	MyDataGrid.EditItemIndex = -1;
	BindGrid();
}
 
void MyDataGrid_Update(Object sender, DataGridCommandEventArgs e) 
{
	TextBox dateText = (TextBox)e.Item.Cells[1].Controls[0];
	TextBox subjectText = (TextBox)e.Item.Cells[2].Controls[0];
	TextBox newsText = (TextBox)e.Item.Cells[3].Controls[0];

	string subject = subjectText.Text;
	string news = newsText.Text;
	string sdate = dateText.Text;
	if(sdate == "" || sdate == null)
		sdate = DateTime.Now.ToString("dd-MM-yyyy");
	string sid = e.Item.Cells[0].Text;
	sdate = DateTime.Parse(sdate).ToString("dd-MM-yyyy");

	int id = int.Parse(sid);
	//string sc = "UPDATE card SET show_news=1 UPDATE news SET date=GETDATE(), ";
	string sc = " SET DATEFORMAT dmy UPDATE news SET date='"+ sdate +"', ";
	sc += "subject='";
	sc += subject;
	sc += "', text='";
	sc += EncodeQuote(news);
	sc += "' ";
	sc += "WHERE id=";
	sc += id;

	try
	{
		SqlCommand myCommand = new SqlCommand(sc);
		myCommand.Connection = myConnection;
		myConnection.Open();
		myCommand.ExecuteNonQuery();
		myCommand.Connection.Close();
	}
	catch(Exception exp) 
	{
		ShowExp(sc, exp);
	}

	MyDataGrid.EditItemIndex = -1;
	BindGrid();
}

void CommandBtn_Click(Object sender, EventArgs e)
{
	string subject = TextBoxSubject.Text;
	string text = TextBoxText.Text;
	string sdate = TextBoxDate.Text;
	if(sdate == "" || sdate == null)
		sdate = DateTime.Now.ToString("dd-MM-yyyy");
//DEBUG("sdate = ", sdate);
	string sc = " SET DATEFORMAT dmy ";
	sc += " INSERT INTO news (date, subject, text) VALUES (";
	sc += "'"+ sdate +"' ";
	sc += ", '";
	sc += subject;
	sc += "', '";
	sc += EncodeQuote(text);
	sc += "')";
//DEBUG("sc = ", sc);
//return;
	try
	{
//		String	myConnection = "Initial Catalog=topsys;Data Source=localhost;Integrated Security=SSPI;";
//		SqlConnection myConnection = new SqlConnection("Initial Catalog=eznz" + m_sDataSource + m_sSecurityString);
//		SqlConnection mySqlClientSrvConn = new SqlConnection(myConnection);
		SqlCommand mySqlCommand = new SqlCommand(sc);
		mySqlCommand.Connection = myConnection;
		myConnection.Open();
		mySqlCommand.ExecuteNonQuery();
		mySqlCommand.Connection.Close();
	}
	catch(Exception exp) 
	{
		ShowExp(sc, exp);
	}
	TextBoxSubject.Text = "";
	TextBoxText.Text = "";
	Response.Write("Please Wait ......<br><meta http-equiv=\"refresh\" content=\"0; URL=editnews.aspx\"");
	

//	<asp:BoundColumn HeaderText=Date DataField=date ReadOnly=true>
//			<HeaderStyle Width=150px/>
//		</asp:BoundColumn>

//	BindGrid();
}
*/

/*
<form runat=server>

<asp:DataGrid id=MyDataGrid 
	runat=server 
	AutoGenerateColumns=false
	BackColor=White 
	BorderWidth=1px 
	BorderStyle=Solid 
	BorderColor=Tan
	CellPadding=2 
	CellSpacing=0
	Font-Name=Verdana 
	Font-Size=8pt 
	width=100% 
	HorizontalAlign=center
	OnEditCommand=MyDataGrid_Edit 
	OnCancelCommand=MyDataGrid_Cancel
	OnUpdateCommand=MyDataGrid_Update
	OnDeleteCommand=MyDataGrid_Delete>

	<Columns>
		<asp:BoundColumn HeaderText=ID DataField=id ReadOnly=true>
			<HeaderStyle Width=50px/>
		</asp:BoundColumn>

		<asp:BoundColumn HeaderText=Date DataField=date/>
		<asp:BoundColumn HeaderText=Subject DataField=subject/>
		<asp:BoundColumn HeaderText=News DataField=text/>
		<asp:EditCommandColumn CancelText="NG"
			UpdateText="OK"
			ItemStyle-Wrap=false
			HeaderStyle-Wrap=false
			HeaderText=Edit 
			EditText=Edit>

			<HeaderStyle Width=50px/>
		</asp:EditCommandColumn>

		<asp:ButtonColumn
			HeaderText=""
			ButtonType="LinkButton"
			Text=" DEL "
			CommandName="Delete">

			<HeaderStyle Width=50px/>

		</asp:ButtonColumn>

	</Columns>

	<HeaderStyle BackColor=DarkRed ForeColor=White Font-Bold=true/>
	<ItemStyle ForeColor=DarkSlateBlue/>
	<AlternatingItemstyle BackColor=Beige/>
</asp:DataGrid>


<table width=100%>
<tr>

	<td><table>
	<tr><td><b>Date : </b></td><td><asp:TextBox id="TextBoxDate" Text="" Columns=80 runat="server"/></td></tr>

	<tr><td><b>Subject : </b></td><td><asp:TextBox id="TextBoxSubject" Text="" Columns=80 runat="server"/></td></tr>
	<tr><td valign=top><b>Text : </b></td><td><asp:TextBox id="TextBoxText" Text="" Columns=100 TextMode=MultiLine Rows=20 runat="server"/></td></tr>
	</table>

	</td>

	<td align=right valign=bottom>
		<asp:Button id="button1" text="&nbsp;&nbsp;&nbsp;Add News&nbsp;&nbsp;&nbsp;" CommandArgument="arg" OnClick="CommandBtn_Click" runat="server"/>
	</td>

</tr>
</table>

</form>
*/
</script>



<asp:Label id=LFooter runat=server/>

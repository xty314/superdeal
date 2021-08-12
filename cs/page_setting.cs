<script runat=server>


void Page_Load(Object Src, EventArgs E ) 
{
	TS_PageLoad(); //do common things, LogVisit etc...
	if(!SecurityCheck("manager"))
		return;
	PrintAdminHeader();
	PrintAdminMenu();
    PrintEditForm();
	
	}

void PrintEditForm()
{

   string level = Session["employee_access_level"].ToString();
    if(level != "10"){
	 Response.Write("<div align=center>");
			Response.Write("<h3>ACCESS DENIED</h3>");
			Response.Write ("<Br> You will be redirected to home page in 3 seconds");
			Response.Write ("</div>");
			Response.Write("<meta http-equiv=\"refresh\" content=\"3; URL=default.aspx\">");
	 }else{
	string lsm = ReadSitePage("page_setting");
	Response.Write (lsm);
	
}
	}
	
</script>

